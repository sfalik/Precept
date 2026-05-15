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

            // Writable on event arg
            var declarationSite = isEventArg
                ? ValueModifierDeclarationSite.EventArgDeclaration
                : ValueModifierDeclarationSite.FieldDeclaration;
            if (!valueMeta.ApplicableDeclarationSites.HasFlag(declarationSite))
            {
                if (isEventArg && kind == ModifierKind.Writable)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.WritableOnEventArg, modifierSpan, declarationName));
                }
            }

            // Writable on computed field
            if (kind == ModifierKind.Writable && isComputed)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.ComputedFieldNotWritable, modifierSpan, declarationName));
            }
        }

        ValidateBoundQualifierRequirements(modifiers, resolvedType, declaredQualifiers, ctx);
        ValidateModifierBounds(modifiers, resolvedType, declaredQualifiers, span, ctx);
        ValidateModifierValues(modifiers, ctx);
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

    private static bool TryExtractTypedConstantMagnitude(object? parsedValue, out decimal magnitude)
    {
        if (parsedValue is decimal decimalValue)
        {
            magnitude = decimalValue;
            return true;
        }

        if (parsedValue is ITuple tuple
            && tuple.Length > 0
            && tuple[0] is decimal tupleMagnitude)
        {
            magnitude = tupleMagnitude;
            return true;
        }

        magnitude = default;
        return false;
    }

    private readonly record struct ExtractedBoundValue(
        decimal DeclaredMagnitude,
        decimal NormalizedMagnitude,
        ImmutableArray<DeclaredQualifierMeta> Qualifiers);

    /// <summary>
    /// PRE0035 — InvalidModifierValue: validate that modifiers with values carry valid values.
    /// For example, 'maxplaces' must be a non-negative integer.
    /// </summary>
    private static void ValidateModifierValues(ImmutableArray<ParsedModifier> modifiers, CheckContext ctx)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.Value is null or MissingExpression) continue;

            switch (modifier.Kind)
            {
                case ModifierKind.Maxplaces:
                {
                    // maxplaces must be a non-negative integer
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
