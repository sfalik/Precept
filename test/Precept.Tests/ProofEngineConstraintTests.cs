using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for C94–C98 proof-backed diagnostics:
/// C94 (assignment outside constraint), C95 (contradictory rule),
/// C96 (vacuous rule), C97 (dead guard), C98 (tautological guard).
/// </summary>
public class ProofEngineConstraintTests
{
    // ── C94: assignment value outside field constraint interval ───────────────

    [Fact]
    public void Check_C94_AssignmentOutsideConstraint_EmitsC94()
    {
        const string dsl = """
            precept Test
            field Score as number default 50 max 100
            state Open initial
            event Go
            from Open on Go -> set Score = 150 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C94");
    }

    [Fact]
    public void Check_C94_AssignmentWithinConstraint_NoC94()
    {
        const string dsl = """
            precept Test
            field Score as number default 50 max 100
            state Open initial
            event Go
            from Open on Go -> set Score = 50 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C94").Should().BeEmpty();
    }

    [Fact]
    public void Check_C94_AssignmentAtBoundary_NoC94()
    {
        const string dsl = """
            precept Test
            field Score as number default 50 max 100
            state Open initial
            event Go
            from Open on Go -> set Score = 100 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C94").Should().BeEmpty();
    }

    [Fact]
    public void Check_C94_AssignmentBelowMin_EmitsC94()
    {
        const string dsl = """
            precept Test
            field Score as number default 10 min 5
            state Open initial
            event Go
            from Open on Go -> set Score = 2 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C94");
    }

    // ── C95: contradictory rule ──────────────────────────────────────────────

    [Fact]
    public void Check_C95_ContradictoryRule_EmitsC95()
    {
        const string dsl = """
            precept Test
            field X as number default 10 min 10
            state Open initial
            rule X < 5 because "contradicts min 10"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C95");
    }

    [Fact]
    public void Check_C95_SatisfiableRule_NoC95()
    {
        const string dsl = """
            precept Test
            field X as number default 15 min 10
            state Open initial
            rule X < 50 because "satisfiable with min 10"
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C95").Should().BeEmpty();
    }

    [Fact]
    public void Check_C95_RuleAtExactBoundary_NoC95()
    {
        // rule X >= 10 with min 10 — not contradictory, exactly at boundary
        const string dsl = """
            precept Test
            field X as number default 15 min 10
            state Open initial
            rule X >= 10 because "matches min constraint"
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C95").Should().BeEmpty();
    }

    [Fact]
    public void Check_C95_LiteralOnLeftSide_EmitsC95()
    {
        // "5 > X" with min 10 → X < 5, contradicts [10, +∞)
        const string dsl = """
            precept Test
            field X as number default 10 min 10
            state Open initial
            rule 5 > X because "literal on left, contradicts min 10"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C95");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));

    // ── C96: vacuous rule (always true given constraints) ────────────────────

    [Fact]
    public void Check_C96_VacuousRule_EmitsC96()
    {
        // rule X >= 0 is vacuous when field has min 1 (constraint is [1, 100] ⊂ [0, +∞))
        const string dsl = """
            precept Test
            field X as number default 5 min 1 max 100
            state Open initial
            rule X >= 0 because "vacuous — min 1 already guarantees >= 0"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C96");
    }

    [Fact]
    public void Check_C96_VacuousRule_PositiveConstraint_EmitsC96()
    {
        // rule X > 0 is vacuous when field has positive constraint (constraint is (0, +∞) ⊂ (0, +∞))
        const string dsl = """
            precept Test
            field X as number default 5 positive
            state Open initial
            rule X > 0 because "vacuous — positive already guarantees > 0"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C96");
    }

    [Fact]
    public void Check_C96_NonVacuousRule_NoC96()
    {
        // rule X > 0 is NOT vacuous when field has no positive/min constraint
        const string dsl = """
            precept Test
            field X as number default 5
            state Open initial
            rule X > 0 because "not vacuous — field has no constraint guaranteeing > 0"
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C96").Should().BeEmpty();
    }

    // ── C97: dead guard (always false given constraints) ─────────────────────

    [Fact]
    public void Check_C97_DeadGuard_EmitsC97()
    {
        // when X < 0 is always false when field has min 10 (constraint [10, +∞) disjoint from (-∞, 0))
        const string dsl = """
            precept Test
            field X as number default 15 min 10
            state A initial
            event Go
            from A on Go when X < 0 -> no transition
            from A on Go -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C97");
    }

    [Fact]
    public void Check_C97_DeadGuard_MaxViolation_EmitsC97()
    {
        // when X > 200 is always false when field has max 100 (constraint (-∞, 100] disjoint from (200, +∞))
        const string dsl = """
            precept Test
            field X as number default 50 max 100
            state A initial
            event Go
            from A on Go when X > 200 -> no transition
            from A on Go -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C97");
    }

    [Fact]
    public void Check_C97_SatisfiableGuard_NoC97()
    {
        // when X > 50 is NOT dead when field has min 10 max 100
        const string dsl = """
            precept Test
            field X as number default 15 min 10 max 100
            state A initial
            event Go
            from A on Go when X > 50 -> no transition
            from A on Go -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C97").Should().BeEmpty();
    }

    // ── C98: tautological guard (always true given constraints) ──────────────

    [Fact]
    public void Check_C98_TautologicalGuard_EmitsC98()
    {
        // when X >= 0 is always true when field has min 10 (constraint [10, +∞) ⊂ [0, +∞))
        const string dsl = """
            precept Test
            field X as number default 15 min 10
            state A initial
            event Go
            from A on Go when X >= 0 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C98");
    }

    [Fact]
    public void Check_C98_TautologicalGuard_PositiveField_EmitsC98()
    {
        // when X > 0 is always true when field has positive constraint
        const string dsl = """
            precept Test
            field X as number default 5 positive
            state A initial
            event Go
            from A on Go when X > 0 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C98");
    }

    [Fact]
    public void Check_C98_NonTautologicalGuard_NoC98()
    {
        // when X > 50 is NOT tautological when field has min 10 max 100
        const string dsl = """
            precept Test
            field X as number default 15 min 10 max 100
            state A initial
            event Go
            from A on Go when X > 50 -> no transition
            from A on Go -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C98").Should().BeEmpty();
    }

    // ── Conditional composition: else-branch negated guard narrowing ─────────

    [Fact]
    public void Check_ConditionalComposition_BothBranchesExcludeZero_NoC93()
    {
        // if A > B then A - B else 1 → then branch: A>B → A-B > 0; else branch: literal 1
        // Hull = (0,+∞) ∪ [1,1] = (0,+∞) — excludes zero → no C93
        const string dsl = """
            precept Test
            field A as number default 10 positive
            field B as number default 5 positive
            field Result as number default 1
            state Open initial
            event Go
            from Open on Go -> set Result = Result / (if A > B then A - B else 1) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_ConditionalComposition_ElseBranchIncludesZero_EmitsC93()
    {
        // if A > B then A - B else 0 → else branch is literal 0
        // Hull includes zero → C93
        const string dsl = """
            precept Test
            field A as number default 10 positive
            field B as number default 5 positive
            field Result as number default 1
            state Open initial
            event Go
            from Open on Go -> set Result = Result / (if A > B then A - B else 0) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_ConditionalComposition_ElseBranchNegatedNarrowing_NoC93()
    {
        // if A > B then A - B else B - A + 1
        // else context: A <= B (negated guard) → B - A >= 0 → B - A + 1 >= 1
        // Hull of (0,+∞) and [1,+∞) excludes zero → no C93
        const string dsl = """
            precept Test
            field A as number default 10 positive
            field B as number default 5 positive
            field Result as number default 1
            state Open initial
            event Go
            from Open on Go -> set Result = Result / (if A > B then A - B else B - A + 1) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Reachability sharpening: C97 dead guards sharpen C48/C50/C51 ────────

    [Fact]
    public void Validate_DeadGuardRow_SharpensC51_RejectOnly()
    {
        // State A has two rows for Go: one rejects, one transitions but has a dead guard
        // (when X < 0 with positive X). Without sharpening, the transition row would
        // prevent C51. With sharpening, C51 fires because the transition is unreachable.
        const string dsl = """
            precept Test
            field X as number default 10 positive
            state A initial
            state B
            event Go
            from A on Go when X < 0 -> transition B
            from A on Go -> reject "blocked"
            from B on Go -> no transition
            """;

        var result = Validate(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C51");
        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C97");
    }

    [Fact]
    public void Validate_DeadGuardRow_SharpensC48_Unreachable()
    {
        // State B is only reachable via a dead-guard transition.
        // With sharpening, B is reported as unreachable (C48).
        const string dsl = """
            precept Test
            field X as number default 10 positive
            state A initial
            state B
            event Go
            from A on Go when X < 0 -> transition B
            from A on Go -> no transition
            """;

        var result = Validate(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C48"
            && d.Message.Contains("B"));
        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C97");
    }

    // ── Private helpers (Validate) ──────────────────────────────────────────

    private static ValidationResult Validate(string dsl) =>
        PreceptCompiler.Validate(PreceptParser.Parse(dsl));
}
