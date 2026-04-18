using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for C94 (assignment outside constraint) and C95 (contradictory rule) diagnostics
/// introduced in step 12a of the proof engine work.
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
}
