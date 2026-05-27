using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    /// <summary>Validate modifier applicability, conflicts, and subsumption for all fields and states.</summary>
    private static void ValidateModifiers(CheckContext ctx)
    {
        foreach (var field in ctx.Fields)
        {
            if (field.ResolvedType == TypeKind.Error) continue;
            ValidateValueModifiers(
                field.Syntax.GetSlot<ModifierListSlot>(ConstructSlotKind.ModifierList)?.Modifiers ?? ImmutableArray<ParsedModifier>.Empty,
                field.ResolvedType,
                field.ImpliedModifiers,
                field.DeclaredQualifiers,
                field.IsComputed,
                field.Syntax.Span,
                field.Name,
                isEventArg: false,
                ctx);
        }

        foreach (var evt in ctx.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.ResolvedType == TypeKind.Error) continue;
                ValidateValueModifiers(
                    arg.Modifiers.Select(kind => new ParsedModifier(kind, null, arg.Span)).ToImmutableArray(),
                    arg.ResolvedType,
                    ImmutableArray<ModifierKind>.Empty,
                    arg.DeclaredQualifiers,
                    isComputed: false,
                    arg.Span,
                    arg.Name,
                    isEventArg: true,
                    ctx);
            }
        }
    }

    /// <summary>
    /// Catalog-driven modifier validation for a single field or event arg declaration.
    /// Checks applicability, duplicates, mutual exclusivity, subsumption, redundancy with
    /// implied modifiers, and writable restrictions.
    /// </summary>
    private static void ValidateValueModifiers(
        ImmutableArray<ParsedModifier> modifiers,
        TypeKind resolvedType,
        ImmutableArray<ModifierKind> impliedModifiers,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        bool isComputed,
        SourceSpan span,
        string declarationName,
        bool isEventArg,
        CheckContext ctx)
    {
        var seen = new HashSet<ModifierKind>();

        for (int i = 0; i < modifiers.Length; i++)
        {
            var modifier = modifiers[i];
            var kind = modifier.Kind;
            var meta = Modifiers.GetMeta(kind);
            var modifierSpan = modifier.Span;

            // Duplicate check
            if (!seen.Add(kind))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.DuplicateModifier, modifierSpan, meta.Token.Text));
                continue;
            }

            // Only ValueModifierMeta modifiers are valid on fields/args
            if (meta is not ValueModifierMeta valueMeta)
                continue;

            // Applicability: empty ApplicableTo = any type; otherwise check membership.
            // Skip applicability check if the modifier is already implied by the type —
            // the redundancy check below will emit RedundantModifier instead.
            if (valueMeta.ApplicableTo.Length > 0 &&
                !impliedModifiers.Contains(kind) &&
                !IsTypeApplicable(valueMeta.ApplicableTo, resolvedType, modifiers.Select(m => m.Kind).ToImmutableArray()))
            {
                var typeName = Types.GetMeta(resolvedType).DisplayName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.InvalidModifierForType, modifierSpan, meta.Token.Text, typeName));
            }

            // Mutual exclusivity / subsumption
            foreach (var conflict in meta.MutuallyExclusiveWith)
            {
                if (!seen.Contains(conflict))
                    continue;

                var conflictMeta = Modifiers.GetMeta(conflict);
                if (conflictMeta is ValueModifierMeta conflictValue)
                {
                    if (conflictValue.Subsumes.Contains(kind))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.RedundantModifier, modifierSpan,
                                meta.Token.Text, conflictMeta.Token.Text));
                        continue;
                    }

                    if (valueMeta.Subsumes.Contains(conflict))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.RedundantModifier, modifierSpan,
                                conflictMeta.Token.Text, meta.Token.Text));
                        continue;
                    }
                }

                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.ConflictingModifiers, modifierSpan,
                        meta.Token.Text, conflictMeta.Token.Text));
            }

            // Subsumption: if another explicit modifier already subsumes this one
            foreach (var other in seen)
            {
                if (other == kind || meta.MutuallyExclusiveWith.Contains(other)) continue;
                var otherMeta = Modifiers.GetMeta(other);
                if (otherMeta is ValueModifierMeta otherValue && otherValue.Subsumes.Contains(kind))
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.RedundantModifier, modifierSpan,
                            meta.Token.Text, otherMeta.Token.Text));
                }
            }

            // Redundancy with implied modifiers (type already implies this modifier)
            if (impliedModifiers.Contains(kind))
            {
                var typeName = Types.GetMeta(resolvedType).DisplayName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.RedundantModifier, modifierSpan,
                        meta.Token.Text, typeName));
            }

            // Declaration-site applicability for value modifiers.
            var declarationSite = isEventArg
                ? ValueModifierDeclarationSite.EventArgDeclaration
                : ValueModifierDeclarationSite.FieldDeclaration;
            if (!valueMeta.ApplicableDeclarationSites.HasFlag(declarationSite))
            {
                // No value modifier today reaches this branch with an emit — the
                // retired `writable` modifier used to. Defensive guard left for
                // future declaration-site-restricted value modifiers.
            }
        }

        // Validate access-modifier rules separately — they live in a different
        // DU subtype (AccessModifierMeta) so the value-modifier loop above
        // skips them. `editable` at field declaration is the unified
        // replacement for the retired `writable` value modifier.
        foreach (var mod in modifiers)
        {
            if (Modifiers.GetMeta(mod.Kind) is not AccessModifierMeta) continue;

            // `editable` on a computed field is a contradiction — computed fields
            // are derived, not directly written.
            if (mod.Kind == ModifierKind.Write && isComputed)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.ComputedFieldNotWritable, mod.Span, declarationName));
            }
        }

        ValidateBoundQualifierRequirements(modifiers, resolvedType, declaredQualifiers, ctx);
        ValidateModifierBounds(modifiers, resolvedType, declaredQualifiers, span, ctx);
        ValidateModifierValues(modifiers, resolvedType, declaredQualifiers, ctx);
        ValidateBoundQualifierCompatibility(modifiers, resolvedType, declaredQualifiers, ctx);
    }

    private static void ValidateBoundQualifierCompatibility(
        ImmutableArray<ParsedModifier> modifiers,
        TypeKind resolvedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        CheckContext ctx)
    {
        if (modifiers.IsDefaultOrEmpty || resolvedType == TypeKind.Error)
            return;

        var typeMeta = Types.GetMeta(resolvedType);
        if (typeMeta.RequiredBoundQualifierAxes.Count == 0)
            return;

        // Only run per-bound checks when the field carries the required qualifier axes.
        // When the field lacks the qualifier, ValidateBoundQualifierRequirements already emits
        // BoundsRequireQualifier for the whole declaration — don't double-diagnose.
        var fieldQualifiersByAxis = declaredQualifiers
            .Where(q => typeMeta.RequiredBoundQualifierAxes.Contains(q.Axis))
            .ToDictionary(q => q.Axis);

        if (fieldQualifiersByAxis.Count == 0)
            return;

        var requiredPrepositions = typeMeta.QualifierShape?.Slots
            .Where(slot => typeMeta.RequiredBoundQualifierAxes.Contains(slot.Axis))
            .Select(slot => Tokens.GetMeta(slot.Preposition).Text ?? slot.Preposition.ToString().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        foreach (var modifier in modifiers)
        {
            if (modifier.Kind != ModifierKind.Min && modifier.Kind != ModifierKind.Max)
                continue;

            var boundValue = TryGetComparableModifierValue(modifier.Value, resolvedType, declaredQualifiers);
            if (boundValue is null)
                continue;

            var boundQualifiers = boundValue.Value.Qualifiers;
            if (boundQualifiers.IsDefaultOrEmpty)
            {
                if (ShouldEmitCountDimensionBoundsAmbiguous(modifier.Value, resolvedType, declaredQualifiers))
                {
                    ctx.Diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.CountDimensionBoundsAmbiguous,
                        modifier.Span));
                    continue;
                }

                if (ShouldAllowUnitQualifiedQuantityBareNumericBound(modifier.Value, resolvedType, declaredQualifiers))
                    continue;

                // Plain numeric bound on a field that requires a qualifier — the bound must
                // specify its qualifier so the comparison is unambiguous.
                var modifierMeta = Modifiers.GetMeta(modifier.Kind);
                ctx.Diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.BoundsRequireQualifier,
                    modifier.Span,
                    modifierMeta.Token.Text ?? modifier.Kind.ToString().ToLowerInvariant(),
                    typeMeta.DisplayName,
                    FormatRequiredQualifierLabel(requiredPrepositions)));
                continue;
            }

            // Both field and bound carry qualifiers — compare values on matching axes.
            foreach (var boundQualifier in boundQualifiers)
            {
                if (!fieldQualifiersByAxis.TryGetValue(boundQualifier.Axis, out var fieldQualifier))
                    continue;

                if (!QualifierValuesMatch(fieldQualifier, boundQualifier))
                {
                    ctx.Diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.BoundsQualifierMismatch,
                        modifier.Span,
                        GetQualifierDisplayValue(boundQualifier),
                        GetQualifierDisplayValue(fieldQualifier)));
                }
            }
        }
    }

    private static bool ShouldAllowUnitQualifiedQuantityBareNumericBound(
        ParsedExpression? expression,
        TypeKind resolvedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers) =>
        resolvedType == TypeKind.Quantity
        && expression is not null
        && IsBareNumericLiteral(expression)
        && declaredQualifiers.Any(q => q is DeclaredQualifierMeta.Unit);

    private static bool ShouldEmitCountDimensionBoundsAmbiguous(
        ParsedExpression? expression,
        TypeKind resolvedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers)
    {
        if (resolvedType != TypeKind.Quantity || expression is null || !IsBareNumericLiteral(expression))
            return false;

        if (declaredQualifiers.Any(q => q is DeclaredQualifierMeta.Unit))
            return false;

        foreach (var dimension in declaredQualifiers.OfType<DeclaredQualifierMeta.Dimension>())
        {
            if (IsCountDimension(dimension.DimensionName))
                return true;
        }

        return false;
    }

    private static bool IsCountDimension(string dimensionName)
    {
        if (DimensionCatalog.All.TryGetValue(dimensionName, out var alias))
            return alias.Vector.Equals(DimensionVector.None);

        return string.Equals(dimensionName, "count", StringComparison.OrdinalIgnoreCase);
    }

    private static bool QualifierValuesMatch(DeclaredQualifierMeta a, DeclaredQualifierMeta b) =>
        (a, b) switch
        {
            (DeclaredQualifierMeta.Currency ca, DeclaredQualifierMeta.Currency cb)
                => string.Equals(ca.CurrencyCode, cb.CurrencyCode, StringComparison.Ordinal),
            (DeclaredQualifierMeta.Unit ua, DeclaredQualifierMeta.Unit ub)
                => string.Equals(ua.UnitCode, ub.UnitCode, StringComparison.Ordinal),
            _ => true,
        };

    private static string GetQualifierDisplayValue(DeclaredQualifierMeta q) => q switch
    {
        DeclaredQualifierMeta.Currency c => c.CurrencyCode,
        DeclaredQualifierMeta.Unit u => u.UnitCode,
        _ => q.Axis.ToString(),
    };

    private static void ValidateBoundQualifierRequirements(
        ImmutableArray<ParsedModifier> modifiers,
        TypeKind resolvedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        CheckContext ctx)
    {
        if (modifiers.IsDefaultOrEmpty || resolvedType == TypeKind.Error)
            return;

        bool hasMin = false;
        bool hasMax = false;
        bool hasBoundModifier = false;
        var boundSpan = default(SourceSpan);

        foreach (var modifier in modifiers)
        {
            switch (modifier.Kind)
            {
                case ModifierKind.Min:
                    hasMin = true;
                    if (!hasBoundModifier)
                    {
                        hasBoundModifier = true;
                        boundSpan = modifier.Span;
                    }
                    break;
                case ModifierKind.Max:
                    hasMax = true;
                    if (!hasBoundModifier)
                    {
                        hasBoundModifier = true;
                        boundSpan = modifier.Span;
                    }
                    break;
            }
        }

        if (!hasBoundModifier)
            return;

        var typeMeta = Types.GetMeta(resolvedType);
        if (typeMeta.RequiredBoundQualifierAxes.Count == 0)
            return;

        if (declaredQualifiers.Any(q => typeMeta.RequiredBoundQualifierAxes.Contains(q.Axis)))
            return;

        var requiredPrepositions = typeMeta.QualifierShape?.Slots
            .Where(slot => typeMeta.RequiredBoundQualifierAxes.Contains(slot.Axis))
            .Select(slot => Tokens.GetMeta(slot.Preposition).Text ?? slot.Preposition.ToString().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        var boundLabel = hasMin && hasMax
            ? "min/max"
            : hasMin
                ? "min"
                : "max";

        ctx.Diagnostics.Add(Diagnostics.Create(
            DiagnosticCode.BoundsRequireQualifier,
            boundSpan,
            boundLabel,
            typeMeta.DisplayName,
            FormatRequiredQualifierLabel(requiredPrepositions)));
    }

    private static string FormatRequiredQualifierLabel(IReadOnlyList<string> qualifiers)
    {
        if (qualifiers.Count == 0)
            return "the required qualifier";

        if (qualifiers.Count == 1)
            return $"'{qualifiers[0]}'";

        if (qualifiers.Count == 2)
            return $"'{qualifiers[0]}' or '{qualifiers[1]}'";

        var head = string.Join(", ", qualifiers.Take(qualifiers.Count - 1).Select(q => $"'{q}'"));
        return $"{head}, or '{qualifiers[^1]}'";
    }

    private static void ValidateModifierBounds(
        ImmutableArray<ParsedModifier> modifiers,
        TypeKind resolvedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        SourceSpan span,
        CheckContext ctx)
    {
        if (modifiers.IsDefaultOrEmpty)
            return;

        var byKind = modifiers
            .GroupBy(modifier => modifier.Kind)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var modifier in modifiers)
        {
            var meta = Modifiers.GetMeta(modifier.Kind) as ValueModifierMeta;
            if (meta?.BoundCounterpart is null || !IsLowerBound(meta))
                continue;

            if (!byKind.TryGetValue(meta.BoundCounterpart.Value, out var counterpart))
                continue;

            var lowerValue = TryGetComparableModifierValue(modifier.Value, resolvedType, declaredQualifiers);
            var upperValue = TryGetComparableModifierValue(counterpart.Value, resolvedType, declaredQualifiers);
            if (lowerValue is null || upperValue is null || lowerValue.Value.NormalizedMagnitude <= upperValue.Value.NormalizedMagnitude)
                continue;

            var counterpartMeta = (ValueModifierMeta)Modifiers.GetMeta(meta.BoundCounterpart.Value);
            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.InvalidModifierBounds,
                    span,
                    meta.Token.Text ?? modifier.Kind.ToString(),
                    lowerValue.Value.DeclaredMagnitude.ToString(CultureInfo.InvariantCulture),
                    counterpartMeta.Token.Text ?? counterpart.Kind.ToString(),
                    upperValue.Value.DeclaredMagnitude.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static bool IsLowerBound(ValueModifierMeta meta)
        => meta.ProofSatisfactions
            .OfType<ProofSatisfaction.Numeric>()
            .Select(proof => proof.Comparison)
            .FirstOrDefault() is OperatorKind.GreaterThan or OperatorKind.GreaterThanOrEqual;

    private static ExtractedBoundValue? TryGetComparableModifierValue(
        ParsedExpression? expr,
        TypeKind expectedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers) => expr switch
    {
        LiteralExpression { LiteralKind: TokenKind.NumberLiteral, Text: var text }
            when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            => new(value, value, ImmutableArray<DeclaredQualifierMeta>.Empty),
        LiteralExpression { LiteralKind: TokenKind.TypedConstant, Text: var text }
            => TryGetComparableTypedConstantValue(text, expectedType, declaredQualifiers),
        UnaryOperationExpression
        {
            Operator: TokenKind.Minus,
            Operand: LiteralExpression { LiteralKind: TokenKind.NumberLiteral, Text: var text }
        }
            when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            => new(-value, -value, ImmutableArray<DeclaredQualifierMeta>.Empty),
        _ => null,
    };

    private static ExtractedBoundValue? TryGetComparableTypedConstantValue(
        string rawText,
        TypeKind expectedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers)
    {
        if (expectedType == TypeKind.Error)
            return null;

        var contentValidation = Types.GetMeta(expectedType).ContentValidation;
        if (contentValidation is null)
            return null;

        var typedConstantContext = declaredQualifiers.IsDefaultOrEmpty
            ? null
            : new TypedConstantContext(DeclaredQualifiers: declaredQualifiers);
        var parseResult = TypedConstantValidation.Validate(contentValidation, rawText, expectedType, typedConstantContext);
        if (!parseResult.IsValid || !TryExtractTypedConstantMagnitude(parseResult.Value, out var magnitude))
            return null;

        var declaredMagnitude = magnitude;
        var normalizedMagnitude = expectedType switch
        {
            TypeKind.Quantity when parseResult.Value is ValueTuple<decimal, UcumParsedUnit?> (_, var unit) =>
                TypedConstantNormalizer.NormalizeQuantity(declaredMagnitude, unit),
            TypeKind.Price when parseResult.Value is ValueTuple<decimal, object?, UcumParsedUnit?> (_, _, var denominatorUnit) =>
                TypedConstantNormalizer.NormalizePrice(declaredMagnitude, denominatorUnit),
            _ => declaredMagnitude,
        };

        var qualifiers = ExtractQualifiersFromParsedValue(expectedType, parseResult.Value)
            ?? ImmutableArray<DeclaredQualifierMeta>.Empty;
        return new ExtractedBoundValue(declaredMagnitude, normalizedMagnitude, qualifiers);
    }

    private static bool TryExtractTypedConstantMagnitude(object? parsedValue, out decimal magnitude) =>
        TypedExpressionMagnitude.TryGetMagnitudeFromParsedValue(parsedValue, out magnitude);

    /// <summary>
    /// PRE0079 — OutOfRange: a field or event-arg default value violates a
    /// structural numeric modifier declared or implied on the same declaration
    /// ('nonnegative', 'positive', 'nonzero', 'min', 'max').
    /// </summary>
    private static void ValidateDefaultAgainstNumericModifiers(
        TypedExpression resolvedDefault,
        TypeKind targetType,
        ImmutableArray<ModifierKind> modifiers,
        ImmutableArray<ModifierKind> impliedModifiers,
        decimal? normalizedDeclaredMin,
        decimal? normalizedDeclaredMax,
        string name,
        SourceSpan span,
        CheckContext ctx)
    {
        if (!TypedExpressionMagnitude.TryGetStaticMagnitude(resolvedDefault, out var rawMagnitude))
            return;

        var comparableMagnitude = NormalizeMagnitudeForComparison(rawMagnitude, resolvedDefault, targetType);
        var displayValue = resolvedDefault is TypedTypedConstant ttc
            ? "'" + ttc.RawText + "'"
            : rawMagnitude.ToString(CultureInfo.InvariantCulture);

        foreach (var modKind in modifiers)
        {
            if (TryReportNumericViolation(modKind, comparableMagnitude, normalizedDeclaredMin, normalizedDeclaredMax,
                                          name, displayValue, span, ctx))
                return;
        }

        if (!impliedModifiers.IsDefaultOrEmpty)
        {
            foreach (var modKind in impliedModifiers)
            {
                if (TryReportNumericViolation(modKind, comparableMagnitude, normalizedDeclaredMin, normalizedDeclaredMax,
                                              name, displayValue, span, ctx))
                    return;
            }
        }
    }

    private static bool TryReportNumericViolation(
        ModifierKind modKind,
        decimal magnitude,
        decimal? normalizedDeclaredMin,
        decimal? normalizedDeclaredMax,
        string name,
        string displayValue,
        SourceSpan span,
        CheckContext ctx)
    {
        if (Modifiers.GetMeta(modKind) is not ValueModifierMeta meta || meta.ProofSatisfactions is null)
            return false;

        foreach (var satisfaction in meta.ProofSatisfactions)
        {
            if (satisfaction is not ProofSatisfaction.Numeric numeric) continue;
            if (numeric.Projection is not SatisfactionProjection.SelfValue) continue;

            decimal? bound = numeric.Bound switch
            {
                NumericBoundSource.Constant c => c.Value,
                NumericBoundSource.DeclarationValue => modKind switch
                {
                    ModifierKind.Min => normalizedDeclaredMin,
                    ModifierKind.Max => normalizedDeclaredMax,
                    _ => null,
                },
                _ => null,
            };
            if (bound is null) continue;

            var satisfied = numeric.Comparison switch
            {
                OperatorKind.GreaterThanOrEqual => magnitude >= bound.Value,
                OperatorKind.GreaterThan        => magnitude >  bound.Value,
                OperatorKind.LessThanOrEqual    => magnitude <= bound.Value,
                OperatorKind.LessThan           => magnitude <  bound.Value,
                OperatorKind.NotEquals          => magnitude != bound.Value,
                OperatorKind.Equals             => magnitude == bound.Value,
                _ => true,
            };

            if (!satisfied)
            {
                var modifierLabel = meta.Token.Text ?? modKind.ToString();
                ctx.Diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.OutOfRange,
                    span, name, displayValue, modifierLabel));
                return true;
            }
        }

        return false;
    }

    private static decimal NormalizeMagnitudeForComparison(
        decimal rawMagnitude,
        TypedExpression resolved,
        TypeKind targetType)
    {
        if (resolved is not TypedTypedConstant ttc) return rawMagnitude;

        return ttc.ParsedValue switch
        {
            ValueTuple<decimal, UcumParsedUnit?> (_, var unit) when targetType == TypeKind.Quantity =>
                TypedConstantNormalizer.NormalizeQuantity(rawMagnitude, unit),
            ValueTuple<decimal, object?, UcumParsedUnit?> (_, _, var denominatorUnit) when targetType == TypeKind.Price =>
                TypedConstantNormalizer.NormalizePrice(rawMagnitude, denominatorUnit),
            _ => rawMagnitude,
        };
    }

    private readonly record struct ExtractedBoundValue(
        decimal DeclaredMagnitude,
        decimal NormalizedMagnitude,
        ImmutableArray<DeclaredQualifierMeta> Qualifiers);

    /// <summary>
    /// PRE0035 — InvalidModifierValue: validate that modifiers with values carry valid values.
    /// For example, 'maxplaces' must be a non-negative integer.
    /// </summary>
    private static void ValidateModifierValues(
        ImmutableArray<ParsedModifier> modifiers,
        TypeKind resolvedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        CheckContext ctx)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.Value is null or MissingExpression) continue;

            switch (modifier.Kind)
            {
                case ModifierKind.Maxplaces:
                {
                    // maxplaces accepts: (a) a non-negative integer literal, OR
                    // (b) a contextual `currency.minorUnit` accessor when the field
                    //     carries a static currency qualifier.
                    if (modifier.Value is LiteralExpression lit && lit.LiteralKind == TokenKind.NumberLiteral)
                    {
                        if (lit.Text.Contains('.') ||
                            (long.TryParse(lit.Text, CultureInfo.InvariantCulture, out var val) && val < 0))
                        {
                            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                                modifier.Span, "maxplaces", "a non-negative integer"));
                        }
                    }
                    else if (modifier.Value is UnaryOperationExpression { Operator: TokenKind.Minus })
                    {
                        ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                            modifier.Span, "maxplaces", "a non-negative integer"));
                    }
                    else if (TryRecognizeContextualCurrencyAccessor(modifier.Value, out var accessorName))
                    {
                        ValidateMaxplacesCurrencyAccessor(modifier, accessorName, resolvedType, declaredQualifiers, ctx);
                    }
                    else
                    {
                        ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                            modifier.Span, "maxplaces", "a non-negative integer or 'currency.minorUnit'"));
                    }
                    break;
                }
                case ModifierKind.Maxlength:
                case ModifierKind.Minlength:
                {
                    // maxlength/minlength must be a non-negative integer
                    var modName = modifier.Kind == ModifierKind.Maxlength ? "maxlength" : "minlength";
                    if (modifier.Value is LiteralExpression lenLit && lenLit.LiteralKind == TokenKind.NumberLiteral)
                    {
                        if (lenLit.Text.Contains('.') ||
                            (long.TryParse(lenLit.Text, CultureInfo.InvariantCulture, out var lenVal) && lenVal < 0))
                        {
                            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                                modifier.Span, modName, "a non-negative integer"));
                        }
                    }
                    else if (modifier.Value is UnaryOperationExpression { Operator: TokenKind.Minus })
                    {
                        ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                            modifier.Span, modName, "a non-negative integer"));
                    }
                    break;
                }
                case ModifierKind.Maxcount:
                case ModifierKind.Mincount:
                {
                    // maxcount/mincount must be a non-negative integer
                    var cntName = modifier.Kind == ModifierKind.Maxcount ? "maxcount" : "mincount";
                    if (modifier.Value is LiteralExpression cntLit && cntLit.LiteralKind == TokenKind.NumberLiteral)
                    {
                        if (cntLit.Text.Contains('.') ||
                            (long.TryParse(cntLit.Text, CultureInfo.InvariantCulture, out var cntVal) && cntVal < 0))
                        {
                            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                                modifier.Span, cntName, "a non-negative integer"));
                        }
                    }
                    else if (modifier.Value is UnaryOperationExpression { Operator: TokenKind.Minus })
                    {
                        ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                            modifier.Span, cntName, "a non-negative integer"));
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Recognizes the contextual `<receiver>.<member>` shape used in modifier-value
    /// position. For `currency.minorUnit`, returns true with
    /// <paramref name="accessorName"/> = "minorUnit". Receiver must be a bare
    /// identifier "currency"; the broader `UseInModifierValueContext` whitelist
    /// on <see cref="FixedReturnAccessor"/> gates which member names are accepted.
    /// </summary>
    private static bool TryRecognizeContextualCurrencyAccessor(ParsedExpression? value, out string accessorName)
    {
        accessorName = string.Empty;
        if (value is MemberAccessExpression { Target: IdentifierExpression { Name: "currency" }, MemberName: var name })
        {
            accessorName = name;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Validates the contextual `maxplaces currency.&lt;accessor&gt;` form:
    /// (a) field must carry a currency qualifier (DeclaredQualifierMeta.Currency);
    /// (b) qualifier must be a static literal — not interpolated from another field;
    /// (c) accessor must be on the modifier-value-context whitelist (via the catalog's
    ///     `UseInModifierValueContext` flag on FixedReturnAccessor).
    /// </summary>
    private static void ValidateMaxplacesCurrencyAccessor(
        ParsedModifier modifier,
        string accessorName,
        TypeKind resolvedType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        CheckContext ctx)
    {
        // (a) Field must carry a currency qualifier. Money/Price/ExchangeRate are
        // the qualifier-bearing types that produce a DeclaredQualifierMeta.Currency
        // via the type checker's qualifier-extraction path.
        var currencyQualifier = declaredQualifiers.OfType<DeclaredQualifierMeta.Currency>().FirstOrDefault();
        if (currencyQualifier is null)
        {
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                modifier.Span, "maxplaces",
                $"'currency.{accessorName}' on a field without a currency qualifier"));
            return;
        }

        // (b) Currency qualifier must be a static literal — SourceFieldName=null means
        // the qualifier was a literal token ('USD'), not an interpolated field reference.
        if (currencyQualifier.SourceFieldName is not null)
        {
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.MaxplacesCurrencyQualifierNotStatic,
                modifier.Span, "maxplaces"));
            return;
        }

        // (c) Accessor must be on the modifier-value-context whitelist. The Currency
        // type-meta's accessor inventory carries `UseInModifierValueContext: true` only
        // on `minorUnit`; other accessors (numericCode, name, symbol) parse cleanly
        // but are not whitelisted in this position.
        var currencyMeta = Types.GetMeta(TypeKind.Currency);
        var accessor = currencyMeta.Accessors
            .OfType<FixedReturnAccessor>()
            .FirstOrDefault(a => a.Name == accessorName);
        if (accessor is null || !accessor.UseInModifierValueContext)
        {
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidModifierValue,
                modifier.Span, "maxplaces",
                $"'currency.{accessorName}' — only `currency.minorUnit` is recognized here (currency.minorUnit returns the ISO 4217 minor-unit count for the field's currency)"));
            return;
        }

        // All three gates pass — the form is structurally valid. The actual catalog
        // resolution (turning currency.minorUnit into the integer count for the
        // declared currency code) is performed by downstream consumers via
        // `TryResolveMaxplacesValue`.
    }

    /// <summary>
    /// Resolves a `maxplaces` modifier value to its integer count, accepting both the
    /// literal-integer form and the contextual `currency.minorUnit` form.
    /// Returns false if the value shape isn't recognized or the contextual form's
    /// currency qualifier can't be resolved. Callers should already have called
    /// <see cref="ValidateModifierValues"/> to surface diagnostics; this helper is the
    /// resolution step downstream of validation.
    /// </summary>
    internal static bool TryResolveMaxplacesValue(
        ParsedExpression? value,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        out int maxplaces)
    {
        if (value is LiteralExpression { LiteralKind: TokenKind.NumberLiteral, Text: var litText }
            && int.TryParse(litText, CultureInfo.InvariantCulture, out maxplaces)
            && maxplaces >= 0)
        {
            return true;
        }

        if (TryRecognizeContextualCurrencyAccessor(value, out var accessorName)
            && accessorName == "minorUnit")
        {
            var currencyQualifier = declaredQualifiers.OfType<DeclaredQualifierMeta.Currency>().FirstOrDefault();
            if (currencyQualifier is not null
                && currencyQualifier.SourceFieldName is null
                && CurrencyCatalog.TryGet(currencyQualifier.CurrencyCode, out var entry))
            {
                maxplaces = entry.MinorUnit;
                return true;
            }
        }

        maxplaces = -1;
        return false;
    }

    /// <summary>
    /// Check whether a resolved type matches any entry in a modifier's ApplicableTo array.
    /// Handles both simple <see cref="TypeTarget"/> and <see cref="ModifiedTypeTarget"/> entries.
    /// </summary>
    private static bool IsTypeApplicable(TypeTarget[] applicableTo, TypeKind resolvedType, ImmutableArray<ModifierKind> modifiers)
    {
        foreach (var target in applicableTo)
        {
            // Kind == null means "any type" within the target
            if (target.Kind is null || target.Kind == resolvedType)
            {
                if (target is ModifiedTypeTarget modified)
                {
                    // All required modifiers must be present
                    if (modified.RequiredModifiers.All(m => modifiers.Contains(m)))
                        return true;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }
}
