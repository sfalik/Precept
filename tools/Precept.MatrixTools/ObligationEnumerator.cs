using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.MatrixTools;

/// <summary>Base of the obligation-enumeration DU: a usable spec or an explicit skip.</summary>
public abstract record ObligationEntry(string Label);

/// <summary>
/// A rule-shaped obligation the enumerator could not translate into a
/// <see cref="ObligationSpec"/>. Skips are records, not silences — every
/// desugaring modifier outside the calculator's current scope surfaces here.
/// </summary>
public sealed record ObligationSkipped(string Label, string Reason) : ObligationEntry(Label);

/// <summary>
/// Enumerates the obligations a precept carries: explicit <c>rule</c> statements
/// plus modifier-desugared rules per field. Desugaring is catalog-derived — the
/// Modifiers catalog's <c>DesugarsToRule</c> flag and <c>ProofSatisfactions</c>
/// metadata (projection, comparison operator, bound source) determine the rule
/// shape; this class never hardcodes per-modifier semantics.
/// </summary>
public static class ObligationEnumerator
{
    /// <summary>
    /// Explicit rules followed by modifier-desugared field rules, in declaration
    /// order. Every field modifier whose catalog entry says it desugars to a rule
    /// yields exactly one entry: an <see cref="ObligationSpec"/> when the numeric
    /// self-value shape applies, otherwise an <see cref="ObligationSkipped"/>
    /// naming why (accessor-projected bounds, cross-field bounds, or a
    /// satisfaction shape the calculator does not model yet).
    /// </summary>
    public static ImmutableArray<ObligationEntry> Enumerate(SemanticIndex semantics)
    {
        var entries = ImmutableArray.CreateBuilder<ObligationEntry>();

        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            var rule = semantics.Rules[i];
            entries.Add(new ObligationSpec($"rule[{i}]", rule.Condition, rule.Guard));
        }

        foreach (var field in semantics.Fields)
        foreach (var modifierKind in field.Modifiers)
        {
            var meta = Modifiers.GetMeta(modifierKind);
            if (!meta.DesugarsToRule)
                continue; // not rule-sugar (optional, editable, default, …) — nothing to enumerate

            var label = $"{field.Name}:{meta.Token.Text ?? modifierKind.ToString().ToLowerInvariant()}";
            entries.Add(DesugarModifier(field, modifierKind, meta, label));
        }

        return entries.ToImmutable();
    }

    private static ObligationEntry DesugarModifier(
        TypedField field, ModifierKind modifierKind, ModifierMeta meta, string label)
    {
        if (meta is not ValueModifierMeta valueMeta)
            return new ObligationSkipped(label,
                "desugars to a rule but is not a value modifier; no satisfaction metadata to derive from");

        if (valueMeta.ProofSatisfactions.Length == 0)
            return new ObligationSkipped(label,
                "desugars to a rule but its catalog entry carries no proof-satisfaction "
                + "metadata to derive the rule shape from (e.g. maxplaces)");

        foreach (var satisfaction in valueMeta.ProofSatisfactions)
        {
            if (satisfaction is not ProofSatisfaction.Numeric numeric)
                continue;

            if (numeric.Projection is not SatisfactionProjection.SelfValue)
                return new ObligationSkipped(label,
                    "accessor-projected bound (length/count); outside the numeric "
                    + "self-value scope of the single-write calculator");

            decimal? bound = numeric.Bound switch
            {
                NumericBoundSource.Constant constant => constant.Value,
                // DeclarationValue: the modifier's declared value slot on the field.
                NumericBoundSource.DeclarationValue => modifierKind switch
                {
                    ModifierKind.Min => field.DeclaredMin,
                    ModifierKind.Max => field.DeclaredMax,
                    _ => null,
                },
                _ => null,
            };
            if (bound is null)
                return new ObligationSkipped(label,
                    "bound is not a literal declared value (cross-field or unsupported "
                    + "bound source); not representable as a constant-bound rule");

            return new ObligationSpec(
                label,
                DesugaredCondition(field, numeric.Comparison, bound.Value),
                // Modifiers on an optional field constrain the value only when one
                // is present: the desugared rule activates on `Field is set`.
                ActivationCondition: field.IsOptional ? PresenceActivation(field) : null);
        }

        return new ObligationSkipped(label,
            "no numeric proof-satisfaction on the catalog entry; satisfaction "
            + "shape not modeled by the calculator");
    }

    /// <summary>
    /// Synthesizes the desugared rule condition <c>Field ⋈ bound</c> as a typed
    /// expression. The operation kind is looked up from the Operations catalog by
    /// operator identity — the canonicalizer only reads the catalog's
    /// <c>OperatorKind</c> back off it, so any member with the right operator serves.
    /// </summary>
    private static TypedExpression DesugaredCondition(TypedField field, OperatorKind comparison, decimal bound)
    {
        var operation = Operations.All
            .OfType<BinaryOperationMeta>()
            .First(m => m.Op == comparison
                && m.Lhs.Kind is TypeKind.Decimal or TypeKind.Integer or TypeKind.Number);

        return new TypedBinaryOp(
            ResultType: TypeKind.Boolean,
            ResolvedOp: operation.Kind,
            Left: FieldRef(field),
            Right: new TypedLiteral(TypeKind.Decimal, bound, field.NameSpan),
            ResultQualifier: null,
            ProofRequirements: ImmutableArray<ProofRequirement>.Empty,
            Span: field.NameSpan);
    }

    /// <summary>The activation condition <c>Field is set</c> for optional-field modifier rules.</summary>
    private static TypedExpression PresenceActivation(TypedField field) =>
        new TypedPostfixOp(FieldRef(field), IsNegated: false, field.NameSpan);

    private static TypedFieldRef FieldRef(TypedField field) =>
        new(field.ResolvedType, field.Name, IsCaseInsensitive: false,
            DeclaredQualifiers: null, Span: field.NameSpan);
}
