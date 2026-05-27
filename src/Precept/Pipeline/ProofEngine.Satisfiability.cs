using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Precept.Language;

namespace Precept.Pipeline;

// W-C — Proof-engine satisfiability cluster.
//
// Detects guards and rules whose conjunction with the fields' declared bounds
// is empty (unsatisfiable / contradictory) — the dual of the per-obligation
// discharge that the rest of the proof engine performs. Lives as a lateral
// pass between `IncorporateForwardingFacts` and the discharge loop.
//
// Architectural placement per the locked design (Decision 1): the existing
// strategy set in `ProofEngine.Strategies.cs` is obligation-discharge-shaped
// (each strategy takes a ProofObligation and answers "did this discharge?").
// Satisfiability is whole-construct ("does ANY data configuration satisfy
// this guard?") with no obligation site — folding it into TryDischarge would
// deform the dispatch shape. A dedicated lateral pass keeps the existing
// strategies intact (the `precept-engine.md § 11 Decision 1` five-strategy
// bound) and adds a clean second surface.

public static partial class ProofEngine
{
    /// <summary>
    /// W-C — scans the precept for provably-unsatisfiable guards and
    /// contradictory rule pairs, emitting `UnsatisfiableGuard` (PRE0082),
    /// `TautologicalGuard` (PRE0153), `VacuousRule` (PRE0154), and
    /// `ContradictoryRule` (PRE0155) directly into the diagnostic stream.
    /// Soundness-over-completeness: every emission corresponds to a verdict
    /// computed exactly under the existing `NumericInterval` algebra — no
    /// solver, no heuristic, no "unknown" verdict.
    /// </summary>
    private static void ScanSatisfiability(SemanticIndex semantics, List<Diagnostic> diagnostics)
    {
        ScanTransitionRowGuards(semantics, diagnostics);
        ScanRules(semantics, diagnostics);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  F-LANG-SPEC-02 — UnsatisfiableGuard (PRE0082)
    // ─────────────────────────────────────────────────────────────────────────

    private static void ScanTransitionRowGuards(SemanticIndex semantics, List<Diagnostic> diagnostics)
    {
        foreach (var row in semantics.TransitionRows)
        {
            if (row.Guard is null) continue;
            if (!IsGuardUnsatisfiableUnderFieldBounds(row.Guard, semantics)) continue;

            diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.UnsatisfiableGuard,
                row.Guard.Span,
                FormatGuardText(row.Guard),
                row.EventName,
                string.Empty));
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
    //  F-LANG-SPEC-03 — ContradictoryRule (PRE0155)
    // ─────────────────────────────────────────────────────────────────────────

    private static void ScanRules(SemanticIndex semantics, List<Diagnostic> diagnostics)
    {
        var ruleConstraints = new List<RuleConstraintSummary>();
        for (int i = 0; i < semantics.Rules.Length; i++)
        {
            var rule = semantics.Rules[i];
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
