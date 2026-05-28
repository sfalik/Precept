using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// W-C — the proof engine's satisfiability scan flags guards whose
/// conjunction with the fields' declared bounds is empty, and rule pairs
/// whose per-field interval intersections are empty.
/// </summary>
public class SatisfiabilityScanTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void UnsatisfiableGuard_EmitsPRE0082_OnContradictoryGuard()
    {
        // F-LANG-SPEC-02 — `X > 100 and X < 50` narrows X to an empty interval.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when X > 100 and X < 50 -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "the guard conjunction is empty under any X value");
    }

    [Fact]
    public void UnsatisfiableGuard_DisjunctiveBranchSatisfiable_DoesNotEmit()
    {
        // F-LANG-SPEC-02 § disjunctive branch handling — `(X > 100 and X < 50) or (X == 7)`
        // has at least one satisfiable branch, so no PRE0082.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when (X > 100 and X < 50) or X == 7 -> transition Done
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "the second disjunct (X == 7) is satisfiable, so the whole disjunction is too");
    }

    [Fact]
    public void UnsatisfiableGuard_AgainstMinMaxFieldModifier_EmitsPRE0082()
    {
        // F-LANG-SPEC-02 § field-modifier composition — `field X min 5 max 100`
        // narrows X to [5, 100]; the guard `when X < 5` is unsatisfiable.
        // (The strict-comparison modifiers like `positive` aren't picked up by
        // the existing GetFieldBounds — that helper handles >= / <= bounds.)
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 min 5 max 100 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when X < 5 -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "min 5 narrows X to [5, 100]; X < 5 falls outside that interval");
    }

    [Fact]
    public void SatisfiableGuard_DoesNotEmitPRE0082()
    {
        // Soundness baseline: a satisfiable guard does not emit.
        var ledger = Prove("""
            precept Repro
            field X as integer default 5 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when X > 0 -> transition Done
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableGuard),
            because: "X > 0 admits many values from the integer domain");
    }

    [Fact]
    public void ContradictoryRule_EmitsPRE0155_OnDisjointRuleIntervals()
    {
        // F-LANG-SPEC-03 — `rule X > 10; rule X <= 5` have empty intersection.
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 editable
            rule X > 10 because "X must exceed 10"
            rule X <= 5 because "X must be at most 5"
            state Open initial
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "X > 10 and X <= 5 have empty intersection — no valid X satisfies both");
    }

    [Fact]
    public void NonContradictoryRulePair_DoesNotEmitPRE0155()
    {
        // Soundness baseline: overlapping rule intervals do not emit.
        var ledger = Prove("""
            precept Repro
            field X as integer default 5 editable
            rule X > 0 because "positive"
            rule X <= 100 because "bounded"
            state Open initial
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "X > 0 and X <= 100 intersect on (0, 100]");
    }

    [Fact]
    public void ContradictoryRule_DifferentFields_DoesNotEmitPRE0155()
    {
        // F-LANG-SPEC-03 scope-cut: only fires when rules share a field.
        var ledger = Prove("""
            precept Repro
            field A as integer default 0 editable
            field B as integer default 0 editable
            rule A > 10 because "A is large"
            rule B < 5 because "B is small"
            state Open initial
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "A and B are independent fields");
    }

    [Fact]
    public void TautologicalGuard_EmitsPRE0153_OnRedundantBound()
    {
        // F-LANG-SPEC-05 — `field X min 5 max 100; when X >= 5` adds no
        // constraint beyond the field's existing modifier.
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 min 5 max 100 editable
            state Open initial
            state Done terminal
            event Advance
            from Open on Advance when X >= 5 -> transition Done
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.TautologicalGuard),
            because: "X >= 5 is always true under the min-5 field bound");
    }

    [Fact]
    public void TautologicalGuard_DoesNotEmit_WhenGuardActuallyNarrows()
    {
        // Soundness baseline: `X > 50` does narrow X within [5, 100], so
        // the guard is not tautological.
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 min 5 max 100 editable
            state Open initial
            state Done terminal
            event Advance
            from Open on Advance when X > 50 -> transition Done
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.TautologicalGuard),
            because: "X > 50 actually narrows X from [5, 100] to (50, 100]");
    }

    [Fact]
    public void VacuousRule_EmitsPRE0154_OnAlwaysTruePredicate()
    {
        // F-LANG-SPEC-04 — `field X min 5; rule X >= 5` is always true.
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 min 5 max 100 editable
            rule X >= 5 because "X must be at least 5"
            state Open initial
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.VacuousRule),
            because: "the rule predicate is already guaranteed by the field's min 5 modifier");
    }

    [Fact]
    public void VacuousRule_DoesNotEmit_OnConstrainingPredicate()
    {
        // Soundness baseline: a rule that actually narrows the field
        // beyond its modifiers does not emit.
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 min 5 max 100 editable
            rule X > 50 because "X must exceed 50"
            state Open initial
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.VacuousRule),
            because: "the rule predicate X > 50 is not implied by min 5");
    }

    [Fact]
    public void UnsatisfiableGuard_AlsoProducesUnreachableRowFact()
    {
        // F-LANG-SPEC-12 — emitting UnsatisfiableGuard also produces an
        // UnreachableRowFact in the proof ledger, surfacing the structured
        // verdict for downstream consumers (LS hover, MCP precept_proofs).
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 editable
            state Open initial
            state Done terminal
            event Submit
            from Open on Submit when X > 100 and X < 50 -> transition Done
            """);

        ledger.ProducedFacts.OfType<UnreachableRowFact>().Should().ContainSingle(
            f => f.EventName == "Submit" && f.FromState == "Open",
            because: "the unsatisfiable guard produces a structured reachability verdict");
    }

    [Fact]
    public void TautologicalGuard_DoesNotEmit_OnUnboundedField()
    {
        // F-LANG-SPEC-05 scope-cut (soundness over completeness): when the
        // field has no declared bounds, every comparison is "cannot decide,"
        // not "tautological."
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 editable
            state Open initial
            state Done terminal
            event Advance
            from Open on Advance when X >= 0 -> transition Done
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.TautologicalGuard),
            because: "without declared bounds, X >= 0 is not provably-true");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  UnsatisfiableRule (PRE0159) — pre-pass that fires before ContradictoryRule
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UnsatisfiableRule_EmitsPRE0159_OnSelfImpossibleRule()
    {
        // The rule `X >= 10` is impossible because X is declared `max 4`. The
        // intersection of [0, 4] (from field bounds) and [10, ∞) (from the rule)
        // is empty AND non-Unbounded, satisfying the soundness gate.
        var ledger = Prove("""
            precept LoanRenewalCap
            field RenewalCount as integer default 0 nonnegative max 4 editable
            rule RenewalCount >= 10 because "Renewal cannot exceed cap"
            state Open initial
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableRule),
            because: "RenewalCount has declared bounds [0, 4]; the rule requires >= 10, which is impossible");
    }

    [Fact]
    public void UnsatisfiableRule_AttributesToOffender_NotInnocentPartner()
    {
        // The attribution-fix test: when one rule is self-unsat and another is fine,
        // PRE0159 fires on the offender; ContradictoryRule does NOT fire on the partner.
        var ledger = Prove("""
            precept LoanRenewalCap
            field RenewalCount as integer default 0 nonnegative max 4 editable
            rule RenewalCount >= 10 because "Offender — impossible"
            rule RenewalCount <= 50 because "Innocent — trivially satisfiable"
            state Open initial
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableRule),
            because: "the first rule is self-unsat and should be attributed correctly");
        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "the second rule is excluded from the pair sweep because the first is self-unsat");
    }

    [Fact]
    public void ContradictoryRule_StillFiresOnGenuinePairConflict()
    {
        // Regression guard: when both rules are individually satisfiable but
        // their conjunction is empty, PRE0155 still fires (PRE0159 does NOT).
        var ledger = Prove("""
            precept Repro
            field X as integer default 7 editable
            rule X > 10 because "X must exceed 10"
            rule X <= 5 because "X must be at most 5"
            state Open initial
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ContradictoryRule),
            because: "both rules are individually satisfiable on an unbounded integer; the pair has empty intersection");
        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableRule),
            because: "neither rule is self-unsatisfiable in isolation — X is unbounded, so each rule admits values");
    }

    [Fact]
    public void UnsatisfiableRule_DoesNotFire_OnUnboundedField()
    {
        // Soundness-over-completeness gate: a rule that would intersect with an
        // unbounded field to [100, ∞) is NOT empty, so PRE0159 does NOT fire.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 editable
            rule X >= 100 because "X must reach the threshold"
            state Open initial
            """);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableRule),
            because: "X has no declared max, so the rule X >= 100 is satisfiable for large enough X");
    }

    [Fact]
    public void UnsatisfiableRule_ComposesWithWhenGuard()
    {
        // The rule's own `when` guard is folded into the per-field interval
        // before the predicate. `when X <= 5` narrows X to [0, 5]; then
        // `X >= 10` (predicate) intersects to empty → PRE0159 fires.
        var ledger = Prove("""
            precept Repro
            field X as integer default 0 nonnegative max 100 editable
            rule X >= 10 when X <= 5 because "Conjunction is impossible"
            state Open initial
            """);

        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnsatisfiableRule),
            because: "the rule's `when` guard composes with its predicate; the conjunction X <= 5 AND X >= 10 is empty");
    }
}
