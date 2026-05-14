using System.Collections.Immutable;
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
        ValidateModifierBounds(modifiers, span, ctx);
    }

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

            var lowerValue = TryGetComparableModifierValue(modifier.Value);
            var upperValue = TryGetComparableModifierValue(counterpart.Value);
            if (lowerValue is null || upperValue is null || lowerValue <= upperValue)
                continue;

            var counterpartMeta = (ValueModifierMeta)Modifiers.GetMeta(meta.BoundCounterpart.Value);
            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.InvalidModifierBounds,
                    span,
                    meta.Token.Text ?? modifier.Kind.ToString(),
                    lowerValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    counterpartMeta.Token.Text ?? counterpart.Kind.ToString(),
                    upperValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    private static bool IsLowerBound(ValueModifierMeta meta)
        => meta.ProofSatisfactions
            .OfType<ProofSatisfaction.Numeric>()
            .Select(proof => proof.Comparison)
            .FirstOrDefault() is OperatorKind.GreaterThan or OperatorKind.GreaterThanOrEqual;

    private static decimal? TryGetComparableModifierValue(ParsedExpression? expr) => expr switch
    {
        LiteralExpression { LiteralKind: TokenKind.NumberLiteral, Text: var text }
            when decimal.TryParse(text, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var value)
            => value,
        UnaryOperationExpression
        {
            Operator: TokenKind.Minus,
            Operand: LiteralExpression { LiteralKind: TokenKind.NumberLiteral, Text: var text }
        }
            when decimal.TryParse(text, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var value)
            => -value,
        _ => null,
    };

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
