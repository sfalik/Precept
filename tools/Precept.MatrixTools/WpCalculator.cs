using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.MatrixTools;

// ════════════════════════════════════════════════════════════════════════════
//  WpCalculator — mechanized weakest-precondition computation for the
//  obligation-discharge matrix.
//
//  Scope: single-write plans over the loop-free, function-free surface.
//  WP(set F = E, R) = R[F := E] (backward substitution through one assignment).
//  Conditional rules (`when C`): WP = C[F:=E] implies R[F:=E].
//  Establishment: WP over the default configuration — defaults substituted for
//  every field the initial write plan does not cover.
//
//  Multi-write plans return WpNotSupported: whether a multi-write plan's
//  obligation decomposes per write (and how guard facts transport through
//  earlier writes) is an open owner decision — sequential composition is
//  deliberately not implemented until that decision lands.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>An obligation to push a WP through: an explicit rule or a modifier-desugared rule.</summary>
public sealed record ObligationSpec(
    string Label,
    TypedExpression Condition,
    TypedExpression? ActivationCondition) : ObligationEntry(Label);

/// <summary>Result of a WP computation.</summary>
public abstract record WpResult;

/// <summary>A computed weakest precondition in normal form.</summary>
public sealed record WpComputed(CanonExpr Wp) : WpResult;

/// <summary>The calculator declines the plan; <see cref="Reason"/> says why.</summary>
public sealed record WpNotSupported(string Reason) : WpResult;

public static class WpCalculator
{
    /// <summary>
    /// Weakest precondition of the obligation through a transition/event row's
    /// write plan (preservation): unwritten field reads denote the pre-state.
    /// </summary>
    public static WpResult ComputePreservationWp(IReadOnlyList<TypedAction> writePlan, ObligationSpec obligation)
    {
        var (write, unsupported) = ValidatePlan(writePlan);
        if (unsupported is not null)
            return unsupported;

        var substitution = Substitution.Empty;
        if (write is not null)
        {
            substitution = substitution.With(
                write.FieldName,
                Canonicalizer.Canonicalize(write.InputExpression, Substitution.Empty));
        }

        return new WpComputed(BuildWp(obligation, substitution));
    }

    /// <summary>
    /// Establishment WP: the obligation over the default configuration, with the
    /// initial write plan's single write (if any) substituted and defaults
    /// substituted for every uncovered field. Fields with neither write nor
    /// default substitute the unset marker.
    /// </summary>
    public static WpResult ComputeEstablishmentWp(
        SemanticIndex semantics,
        IReadOnlyList<TypedAction> initialWritePlan,
        ObligationSpec obligation)
    {
        var (write, unsupported) = ValidatePlan(initialWritePlan);
        if (unsupported is not null)
            return unsupported;

        // Pass 1 — the default configuration. Computed fields are out of scope
        // for now and stay symbolic (pre-state reads).
        var defaults = Substitution.Empty;
        foreach (var field in semantics.Fields)
        {
            if (field.IsComputed)
                continue;
            defaults = defaults.With(
                field.Name,
                field.DefaultExpression is not null
                    ? Canonicalizer.Canonicalize(field.DefaultExpression, Substitution.Empty)
                    : new CanonUnset());
        }

        // Pass 2 — the initial write overrides its field's default; its RHS reads
        // the default configuration.
        var substitution = defaults;
        if (write is not null)
        {
            substitution = substitution.With(
                write.FieldName,
                Canonicalizer.Canonicalize(write.InputExpression, defaults));
        }

        return new WpComputed(BuildWp(obligation, substitution));
    }

    /// <summary>Canonicalizes a typed expression (no substitution) to its normal form.</summary>
    public static CanonExpr Canonicalize(TypedExpression expression) =>
        Canonicalizer.Canonicalize(expression, Substitution.Empty);

    /// <summary>True iff the two expressions are normal-form-equal.</summary>
    public static bool AreNormalFormEqual(TypedExpression left, TypedExpression right) =>
        Canonicalize(left).Key == Canonicalize(right).Key;

    /// <summary>True iff the guard, as a whole condition, is normal-form-equal to the WP.</summary>
    public static bool GuardMatchesWp(TypedExpression guard, CanonExpr wp) =>
        Canonicalize(guard).Key == wp.Key;

    /// <summary>
    /// True iff every top-level conjunct (guard fact) of the WP appears among the
    /// guard's top-level conjuncts — the guard-set membership form of the match
    /// (a guard may carry additional facts beyond the WP).
    /// </summary>
    public static bool GuardFactsCoverWp(TypedExpression guard, CanonExpr wp)
    {
        if (wp is CanonBool { Value: true })
            return true; // a trivially-true WP needs no guard fact

        var guardFacts = ConjunctsOf(Canonicalize(guard))
            .Select(f => f.Key)
            .ToHashSet(StringComparer.Ordinal);
        return ConjunctsOf(wp).All(f => guardFacts.Contains(f.Key));
    }

    /// <summary>The top-level conjunct list (guard-fact set) of a canonical expression.</summary>
    public static ImmutableArray<CanonExpr> ConjunctsOf(CanonExpr expression) =>
        expression is CanonNary { Op: CanonNaryOp.And } and_
            ? and_.Operands
            : [expression];

    /// <summary>
    /// Extracts per-term bound facts from a guard: every conjunct of the shape
    /// term-compare-constant, normalized so the fact reads term ⋈ bound.
    /// These feed the interval-arithmetic derivation (which is the prover's job,
    /// not this calculator's) — extraction only.
    /// </summary>
    public static ImmutableArray<GuardBoundFact> ExtractBoundFacts(TypedExpression guard)
    {
        var facts = ImmutableArray.CreateBuilder<GuardBoundFact>();
        foreach (var conjunct in ConjunctsOf(Canonicalize(guard)))
        {
            if (conjunct is not CanonCompare compare)
                continue;

            switch (compare.Left, compare.Right)
            {
                case (CanonNumber, CanonNumber):
                    break; // ground comparisons fold before reaching here

                case (var term, CanonNumber bound):
                    facts.Add(new GuardBoundFact(term, RightBoundKind(compare.Op), bound.Value));
                    break;

                case (CanonNumber bound, var term):
                    facts.Add(new GuardBoundFact(term, LeftBoundKind(compare.Op), bound.Value));
                    break;
            }
        }
        return facts.ToImmutable();
    }

    private static BoundKind RightBoundKind(CanonCompareOp op) => op switch
    {
        CanonCompareOp.Lt => BoundKind.UpperExclusive,   // term <  c
        CanonCompareOp.Le => BoundKind.UpperInclusive,   // term <= c
        CanonCompareOp.Eq => BoundKind.Equal,
        _ => BoundKind.NotEqual,
    };

    private static BoundKind LeftBoundKind(CanonCompareOp op) => op switch
    {
        CanonCompareOp.Lt => BoundKind.LowerExclusive,   // c <  term
        CanonCompareOp.Le => BoundKind.LowerInclusive,   // c <= term
        CanonCompareOp.Eq => BoundKind.Equal,
        _ => BoundKind.NotEqual,
    };

    // ── Internals ────────────────────────────────────────────────────────────

    private static CanonExpr BuildWp(ObligationSpec obligation, Substitution substitution)
    {
        var body = Canonicalizer.Canonicalize(obligation.Condition, substitution);
        if (obligation.ActivationCondition is null)
            return body;

        // Conditional rule: WP = activation' implies body'. Never folded — the
        // vacuity semantics is discharge-contract content, not normalization.
        var activation = Canonicalizer.Canonicalize(obligation.ActivationCondition, substitution);
        return new CanonImplies(activation, body);
    }

    private static (TypedInputAction? Write, WpNotSupported? Error) ValidatePlan(
        IReadOnlyList<TypedAction> plan)
    {
        if (plan.Count == 0)
            return (null, null); // no-write plan: the obligation frame-preserves

        foreach (var action in plan)
        {
            if (action is not TypedInputAction { Kind: ActionKind.Set })
                return (null, new WpNotSupported(
                    $"plan contains a non-set action ({action.Kind} on {action.FieldName}); "
                    + "only scalar set writes are in the single-write calculator's scope"));
        }

        if (plan.Count > 1)
            return (null, new WpNotSupported(
                "multi-write plan: write-plan decomposition (per-write cells vs whole-plan "
                + "obligations, and guard-fact transport through prior writes) is an open "
                + "owner decision; sequential composition is deliberately not implemented"));

        return ((TypedInputAction)plan[0], null);
    }
}

/// <summary>How a bound fact constrains its term.</summary>
public enum BoundKind
{
    UpperInclusive,   // term <= c
    UpperExclusive,   // term <  c
    LowerInclusive,   // term >= c
    LowerExclusive,   // term >  c
    Equal,            // term == c
    NotEqual,         // term != c
}

/// <summary>A per-term bound fact extracted from a guard conjunct: term ⋈ constant.</summary>
public sealed record GuardBoundFact(CanonExpr Term, BoundKind Kind, decimal Bound);
