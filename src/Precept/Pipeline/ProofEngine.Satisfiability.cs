using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Precept.Language;

namespace Precept.Pipeline;

// Proof-engine satisfiability scan.
//
// Detects guards and rules whose conjunction with the fields' declared bounds
// is empty (unsatisfiable / contradictory) — the dual of the per-obligation
// discharge that the rest of the proof engine performs. Lives as a lateral
// pass between `IncorporateForwardingFacts` and the discharge loop. See
// docs/compiler/proof-engine.md § Two-Pass Design (Pass 1.5).
//
// The existing strategy set in `ProofEngine.Strategies.cs` is obligation-
// discharge-shaped (each strategy takes a ProofObligation and answers "did
// this discharge?"). Satisfiability is whole-construct ("does ANY data
// configuration satisfy this guard?") with no obligation site — folding it
// into TryDischarge would deform the dispatch shape, so this scan emits
// diagnostics directly rather than flowing through the obligation channel.

public static partial class ProofEngine
{
    /// <summary>
    /// Scans the precept for provably-unsatisfiable guards and contradictory
    /// rule pairs, emitting `UnsatisfiableGuard` (PRE0082), `TautologicalGuard`
    /// (PRE0153), `VacuousRule` (PRE0154), and `ContradictoryRule` (PRE0155)
    /// directly into the diagnostic stream. Soundness-over-completeness
    /// (per `precept-language-spec.md § 0.6 Proof philosophy`): every emission
    /// corresponds to a verdict computed exactly under the existing
    /// `NumericInterval` algebra — no solver, no heuristic, no "unknown"
    /// verdict.
    /// </summary>
    private static void ScanSatisfiability(SemanticIndex semantics, List<Diagnostic> diagnostics, List<ProofForwardingFact> producedFacts)
    {
        ScanTransitionRowGuards(semantics, diagnostics, producedFacts);
        ScanRules(semantics, diagnostics);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TautologicalGuard / VacuousRule
    //
    //  Shared mechanic: a predicate is provably-true iff every leaf constraint
    //  is provably-true under the field's bounded interval. We sidestep
    //  expression negation (which would require synthesizing inverted
    //  TypedBinaryOp nodes) by checking each leaf directly against the field's
    //  interval: e.g. `field > V` is provably-true iff `min(field) > V`.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// True iff every leaf constraint of the predicate is provably-true under
    /// the field's declared bounds. Returns false for OR-disjunctive predicates
    /// (every branch must independently be tautological for the disjunction to
    /// be tautological — we conservatively decline on disjunctions).
    /// </summary>
    private static bool IsPredicateProvablyTrue(
        TypedExpression predicate,
        SemanticIndex semantics,
        Dictionary<string, NumericInterval>? extraNarrowing = null)
    {
        var branches = ExtractGuardBranches(predicate);
        if (branches.IsEmpty) return false;

        foreach (var branchConstraints in branches)
        {
            if (branchConstraints.IsEmpty) return false; // empty branch: nothing proven
            foreach (var gc in branchConstraints)
            {
                if (gc.IsPresenceCheck) return false; // out of scope for this check
                if (gc.Value is not { } value) return false;
                if (!IsConstraintProvablyTrue(gc.Field, gc.Comparison, value, semantics, extraNarrowing))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// True iff the constraint `field comparison value` holds for every value
    /// in the field's bounded interval (optionally further narrowed by
    /// <paramref name="extraNarrowing"/> — used for the rule's own `when`
    /// guard when checking VacuousRule).
    /// </summary>
    private static bool IsConstraintProvablyTrue(
        string fieldName,
        OperatorKind comparison,
        decimal value,
        SemanticIndex semantics,
        Dictionary<string, NumericInterval>? extraNarrowing)
    {
        var interval = ExtractFieldInterval(fieldName, semantics);
        if (extraNarrowing is not null && extraNarrowing.TryGetValue(fieldName, out var extra))
            interval = interval.IsUnbounded
                ? extra
                : interval.Intersect(extra);

        // An unbounded interval cannot guarantee any non-trivial constraint
        // (soundness over completeness — § 0.6 #1).
        if (interval.IsUnbounded) return false;
        if (interval.IsEmpty) return false;

        return comparison switch
        {
            OperatorKind.GreaterThan        => interval.Min >  value,
            OperatorKind.GreaterThanOrEqual => interval.Min >= value,
            OperatorKind.LessThan           => interval.Max <  value,
            OperatorKind.LessThanOrEqual    => interval.Max <= value,
            OperatorKind.Equals             => interval.Min == value && interval.Max == value,
            OperatorKind.NotEquals          => value < interval.Min || value > interval.Max,
            _ => false,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UnsatisfiableGuard (PRE0082)
    // ─────────────────────────────────────────────────────────────────────────

    private static void ScanTransitionRowGuards(SemanticIndex semantics, List<Diagnostic> diagnostics, List<ProofForwardingFact> producedFacts)
    {
        foreach (var row in semantics.TransitionRows)
        {
            if (row.Guard is null) continue;

            if (IsGuardUnsatisfiableUnderFieldBounds(row.Guard, semantics))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UnsatisfiableGuard,
                    row.Guard.Span,
                    FormatGuardText(row.Guard),
                    row.EventName,
                    string.Empty));

                // Produce a structured reachability fact for downstream
                // consumers (LS hover, MCP precept_proofs). Wildcard FromState
                // is represented as "*" for serialization stability.
                producedFacts.Add(new UnreachableRowFact(
                    FromState: row.FromState ?? "*",
                    EventName: row.EventName,
                    RowSpan: row.RowSpan));
                continue; // a guard can't be both unsatisfiable AND tautological
            }

            if (IsPredicateProvablyTrue(row.Guard, semantics))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.TautologicalGuard,
                    row.Guard.Span,
                    FormatGuardText(row.Guard)));
            }
        }
    }

    /// <summary>
    /// True iff every branch of the guard, narrowed against the field bounds,
    /// produces an empty interval on at least one field. Uses the existing
    /// `ExtractGuardBranches` infrastructure so AND/OR structure is honoured.
    /// </summary>
    private static bool IsGuardUnsatisfiableUnderFieldBounds(
        TypedExpression guard,
        SemanticIndex semantics)
    {
        var branches = ExtractGuardBranches(guard);
        if (branches.IsEmpty) return false;

        foreach (var branchConstraints in branches)
        {
            if (!BranchHasEmptyFieldInterval(branchConstraints, semantics))
                return false; // at least one branch is satisfiable
        }
        return true;
    }

    private static bool BranchHasEmptyFieldInterval(
        ImmutableArray<GuardConstraint> branchConstraints,
        SemanticIndex semantics)
    {
        var byField = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);

        foreach (var gc in branchConstraints)
        {
            if (gc.IsPresenceCheck) continue;
            if (gc.Value is not { } value) continue;

            var current = byField.TryGetValue(gc.Field, out var existing)
                ? existing
                : ExtractFieldInterval(gc.Field, semantics);

            var clipped = NarrowByConstraint(current, gc.Comparison, value);
            if (clipped.IsEmpty) return true;
            byField[gc.Field] = clipped;
        }

        return false;
    }

    /// <summary>
    /// Apply a single constraint (comparison + literal value) to an interval,
    /// returning the tightened interval. Used by the satisfiability scan when
    /// composing guard-derived constraints with field-modifier bounds.
    /// </summary>
    private static NumericInterval NarrowByConstraint(NumericInterval current, OperatorKind comparison, decimal value)
    {
        if (current.IsUnbounded)
            current = new NumericInterval(decimal.MinValue, decimal.MaxValue);

        return comparison switch
        {
            OperatorKind.GreaterThan        => current.Intersect(new NumericInterval(value + 1m, decimal.MaxValue)),
            OperatorKind.GreaterThanOrEqual => current.Intersect(new NumericInterval(value,       decimal.MaxValue)),
            OperatorKind.LessThan           => current.Intersect(new NumericInterval(decimal.MinValue, value - 1m)),
            OperatorKind.LessThanOrEqual   => current.Intersect(new NumericInterval(decimal.MinValue, value)),
            OperatorKind.Equals             => current.Intersect(NumericInterval.Point(value)),
            OperatorKind.NotEquals          => current, // point-exclusion isn't representable in a closed interval — conservative
            _ => current,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ContradictoryRule (PRE0155)
    // ─────────────────────────────────────────────────────────────────────────

    private static void ScanRules(SemanticIndex semantics, List<Diagnostic> diagnostics)
    {
        var ruleConstraints = new List<RuleConstraintSummary>();
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            var rule = semantics.Rules[i];

            // VacuousRule: rule predicate is provably-true under the fields'
            // bounds (and the rule's own `when` guard, if any).
            Dictionary<string, NumericInterval>? extraNarrowing = null;
            if (rule.Guard is not null)
            {
                extraNarrowing = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);
                foreach (var gc in ExtractGuardConstraints(rule.Guard))
                {
                    if (gc.IsPresenceCheck) continue;
                    if (gc.Value is not { } v) continue;
                    var current = extraNarrowing.TryGetValue(gc.Field, out var existing)
                        ? existing
                        : new NumericInterval(decimal.MinValue, decimal.MaxValue);
                    extraNarrowing[gc.Field] = NarrowByConstraint(current, gc.Comparison, v);
                }
            }

            if (IsPredicateProvablyTrue(rule.Condition, semantics, extraNarrowing))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.VacuousRule,
                    rule.Condition.Span,
                    FormatGuardText(rule.Condition)));
            }

            var perField = SummariseRuleConstraints(rule.Condition);
            ruleConstraints.Add(new RuleConstraintSummary(i, rule, perField));
        }

        for (int j = 1; j < ruleConstraints.Count; j++)
        {
            var current = ruleConstraints[j];
            for (int k = 0; k < j; k++)
            {
                var prior = ruleConstraints[k];
                foreach (var (field, currentInterval) in current.PerField)
                {
                    if (!prior.PerField.TryGetValue(field, out var priorInterval)) continue;

                    var combined = currentInterval.Intersect(priorInterval);
                    if (!combined.IsEmpty) continue;

                    // Scope-cut (Decision § 0.6 #1, soundness over completeness):
                    // skip if either operand is Unbounded — verdict is "cannot
                    // decide," not "contradicts."
                    if (currentInterval.IsUnbounded || priorInterval.IsUnbounded) continue;

                    diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.ContradictoryRule,
                        current.Rule.Condition.Span,
                        FormatGuardText(current.Rule.Condition),
                        field));
                    goto NextRule; // one contradiction per rule is enough
                }
            }
            NextRule: ;
        }
    }

    /// <summary>
    /// Maps a rule's predicate expression to a per-field interval the rule
    /// imposes. Returns empty when the predicate has OR-disjunction (per the
    /// design's scope-cut — only AND-conjoined-leaf shapes are handled).
    /// </summary>
    private static Dictionary<string, NumericInterval> SummariseRuleConstraints(TypedExpression predicate)
    {
        var result = new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal);
        var constraints = ExtractGuardConstraints(predicate);
        foreach (var gc in constraints)
        {
            if (gc.IsPresenceCheck) continue;
            if (gc.Value is not { } value) continue;

            var current = result.TryGetValue(gc.Field, out var existing)
                ? existing
                : NumericInterval.Unbounded;

            // Use a non-sentinel base when intersecting (Unbounded ∩ X always = X).
            if (current.IsUnbounded)
                current = new NumericInterval(decimal.MinValue, decimal.MaxValue);

            result[gc.Field] = NarrowByConstraint(current, gc.Comparison, value);
        }
        return result;
    }

    private sealed record RuleConstraintSummary(
        int RuleIndex,
        TypedRule Rule,
        Dictionary<string, NumericInterval> PerField);

    private static string FormatGuardText(TypedExpression expr) => expr.ToString() ?? "<guard>";
}
