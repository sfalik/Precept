using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.MatrixTools;

/// <summary>
/// Enumerates the obligations a precept carries: explicit <c>rule</c> statements
/// plus modifier-desugared rules per field. Desugaring is catalog-derived — the
/// Modifiers catalog's <c>DesugarsToRule</c> flag and <c>ProofSatisfactions</c>
/// metadata (projection, comparison operator, bound source) determine the rule
/// shape; this class never hardcodes per-modifier semantics. Accessor-projected
/// satisfactions (length/count bounds) are outside the numeric-value scope for
/// now and are skipped.
/// </summary>
public static class ObligationEnumerator
{
    /// <summary>Explicit rules followed by modifier-desugared field rules, in declaration order.</summary>
    public static ImmutableArray<ObligationSpec> Enumerate(SemanticIndex semantics)
    {
        var obligations = ImmutableArray.CreateBuilder<ObligationSpec>();

        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            var rule = semantics.Rules[i];
            obligations.Add(new ObligationSpec($"rule[{i}]", rule.Condition, rule.Guard));
        }

        foreach (var field in semantics.Fields)
        foreach (var modifierKind in field.Modifiers)
        {
            var meta = Modifiers.GetMeta(modifierKind);
            if (!meta.DesugarsToRule || meta is not ValueModifierMeta valueMeta)
                continue;

            foreach (var satisfaction in valueMeta.ProofSatisfactions)
            {
                if (satisfaction is not ProofSatisfaction.Numeric
                    {
                        Projection: SatisfactionProjection.SelfValue
                    } numeric)
                {
                    continue;
                }

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
                    continue;

                obligations.Add(new ObligationSpec(
                    $"{field.Name}:{meta.Token.Text ?? modifierKind.ToString().ToLowerInvariant()}",
                    DesugaredCondition(field, numeric.Comparison, bound.Value),
                    ActivationCondition: null));
            }
        }

        return obligations.ToImmutable();
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
            Left: new TypedFieldRef(
                field.ResolvedType, field.Name, IsCaseInsensitive: false,
                DeclaredQualifiers: null, Span: field.NameSpan),
            Right: new TypedLiteral(TypeKind.Decimal, bound, field.NameSpan),
            ResultQualifier: null,
            ProofRequirements: ImmutableArray<ProofRequirement>.Empty,
            Span: field.NameSpan);
    }
}
