using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Full-pipeline tests for <b>Gap 2</b> — rules where the RHS is a sum
/// (<c>rule Total &gt; Tax + Fee</c>) — through the unified proof engine (Commit 3).
///
/// <para>
/// Each test compiles a complete precept and asserts whether C93 is suppressed or emitted.
/// Before Commit 3, <c>TryApplyNumericComparisonNarrowing</c> only handled
/// <c>id OP id</c> and <c>id OP literal</c>; sum-on-RHS fell through.
/// After Commit 3, both sides of a rule are normalised to <see cref="LinearForm"/>
/// so any normalizable comparison stores a relational fact that <c>IntervalOf</c> can match.
/// </para>
///
/// Coverage:
/// <list type="bullet">
///   <item>Two-field sum-on-RHS (<c>rule Total &gt; Tax + Fee</c>)</item>
///   <item>Three-field sum-on-RHS</item>
///   <item>gte-on-RHS (nonneg, not nonzero → C93 fires)</item>
///   <item>Sum on LHS (<c>rule A + B &gt; C</c>)</item>
///   <item>Function-call summand — conservative fallback (C93 fires)</item>
///   <item>Guard-based sum-on-RHS (via <c>when</c> clause)</item>
///   <item>Insufficient rule that only covers part of the divisor</item>
///   <item>Regression: simple <c>A &gt; B</c> still works</item>
/// </list>
/// </summary>
public class ProofEngineSumOnRhsTests
{
    // ── Core Gap 2: sum on RHS ────────────────────────────────────────────────

    [Fact]
    public void Check_SumOnRhs_TotalMinusTaxFee_WithGtRule_NoC93()
    {
        // Basic Gap 2: rule Total > Tax + Fee.
        // LinearForm(Total - (Tax + Fee)) = +1·Total + (-1)·Tax + (-1)·Fee.
        // WithRule stores exactly that key → direct match → C93 suppressed.
        const string dsl = """
            precept Test
            field Total as number default 10
            field Tax as number default 2
            field Fee as number default 1
            field Y as number default 1
            rule Total > Tax + Fee because "total exceeds charges"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (Total - Tax - Fee) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_SumOnRhs_ThreeFieldSum_NoC93()
    {
        // Rule with three-field sum on RHS: rule Amount > Base + Tax + Surcharge.
        const string dsl = """
            precept Test
            field Amount as number default 20
            field Base as number default 10
            field Tax as number default 5
            field Surcharge as number default 2
            field Y as number default 1
            rule Amount > Base + Tax + Surcharge because "amount covers all charges"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (Amount - Base - Tax - Surcharge) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_SumOnRhs_GteRule_EmitsC93()
    {
        // rule Total >= Tax + Fee only proves nonneg, not nonzero.
        // Divisor Total - Tax - Fee could be zero. C93 must fire.
        const string dsl = """
            precept Test
            field Total as number default 5
            field Tax as number default 3
            field Fee as number default 2
            field Y as number default 1
            rule Total >= Tax + Fee because "total not below charges"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (Total - Tax - Fee) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_SumOnLhs_WithGtRule_NoC93()
    {
        // Sum on LHS: rule A + B > C. Divisor = A + B - C.
        // LinearForm(A + B - C) = +1·A + 1·B + (-1)·C.
        // Rule key (from WithRule(A+B, GT, C)) = same. Direct match → C93 suppressed.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field C as number default 1
            field Y as number default 1
            rule A + B > C because "sum exceeds C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A + B - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_SumOnRhs_WithConstantOnRhs_NoC93()
    {
        // rule A > B + 1. Divisor = A - B - 1.
        // LinearForm(A - B - 1) = +1·A + (-1)·B + (-1).
        // Rule key = LinearForm(A) - LinearForm(B + 1) = +1·A + (-1)·B + (-1). Direct match.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B + 1 because "A strictly more than B+1"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B - 1) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_SumOnRhs_ModuloOperator_NoC93()
    {
        // Same sum-on-RHS proof but with the % operator on the divisor.
        const string dsl = """
            precept Test
            field Total as number default 10
            field Tax as number default 2
            field Fee as number default 1
            field Y as number default 1
            rule Total > Tax + Fee because "total exceeds charges"
            state Open initial
            event Go
            from Open on Go -> set Y = Y % (Total - Tax - Fee) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_SumOnRhs_FunctionCallSummand_EmitsC93()
    {
        // rule Total > abs(Tax) + Fee — abs(Tax) is a function call, not normalizable.
        // LinearForm.TryNormalize(abs(Tax)) returns null. The rule falls back to conservative
        // handling → no LinearForm fact stored for the sum-on-RHS → C93 fires.
        const string dsl = """
            precept Test
            field Total as number default 10
            field Tax as number default 2
            field Fee as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go when Total > abs(Tax) + Fee -> set Y = Y / (Total - abs(Tax) - Fee) -> no transition
            from Open on Go -> reject "not proved"
            """;

        // Even with a guard, the divisor abs(Tax) component is non-normalizable.
        // The engine cannot prove the divisor nonzero → C93 fires.
        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_SumOnRhs_GuardBased_NoC93()
    {
        // Sum-on-RHS proof via guard rather than rule.
        // when Total > Tax + Fee narrows the EventProofContext with a relational fact.
        const string dsl = """
            precept Test
            field Total as number default 10
            field Tax as number default 2
            field Fee as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go when Total > Tax + Fee -> set Y = Y / (Total - Tax - Fee) -> no transition
            from Open on Go -> reject "below threshold"
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_SumOnRhs_InsufficientRule_MissingOneTerm_EmitsC93()
    {
        // rule Total > Tax (not Total > Tax + Fee).
        // The divisor is Total - Tax - Fee. Total > Tax only proves Total - Tax > 0,
        // not Total - Tax - Fee > 0. C93 must fire.
        const string dsl = """
            precept Test
            field Total as number default 10
            field Tax as number default 2
            field Fee as number default 8
            field Y as number default 1
            rule Total > Tax because "total exceeds tax"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (Total - Tax - Fee) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_SumOnRhs_BothSidesNonNormalizable_EmitsC93()
    {
        // rule abs(A) > abs(B) — neither side is a LinearForm.
        // No relational fact stored. Divisor abs(A) - abs(B) → C93 fires.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go when abs(A) > abs(B) -> set Y = Y / (A - B) -> no transition
            from Open on Go -> reject "no proof"
            """;

        // Guard provides no LinearForm-based fact for A - B, even though it mentions
        // abs(A) > abs(B). The divisor is plain A - B, which needs a direct proof.
        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_SumOnRhs_WithMixedSignSummands_NoC93()
    {
        // rule A > B - C (negative summand on RHS).
        // LinearForm(A) - LinearForm(B - C) = +1·A + (-1)·B + 1·C.
        // Divisor A - (B - C) = A - B + C → same LinearForm → C93 suppressed.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 5
            field C as number default 2
            field Y as number default 1
            rule A > B - C because "A exceeds net"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - (B - C)) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Regression_SimpleGtRule_StillWorks_NoC93()
    {
        // Regression: simple rule A > B (no sum on either side) still works
        // after Commit 3's rewrite of TryApplyNumericComparisonNarrowing.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
}
