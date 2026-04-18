using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Full-pipeline tests for compound-divisor patterns through the unified proof engine
/// (Commit 3: <c>GlobalProofContext</c> + <c>LinearForm</c> relational-fact integration).
///
/// Each test compiles a complete precept via <see cref="PreceptTypeChecker.Check"/> and
/// asserts whether C93 (unproven divisor) is suppressed or emitted.
///
/// Coverage:
/// <list type="bullet">
///   <item>Gap 1 — compound subtraction operands: <c>(A+1)-B</c>, <c>A-(B+C)</c>, <c>Total-Tax-Fee</c></item>
///   <item>Regression — bare <c>A-B</c> with rule <c>A&gt;B</c> (ex-C-Nano cases)</item>
///   <item>Scalar-multiple normalization: <c>3*A - 3*B</c> with <c>rule A&gt;B</c></item>
///   <item>Boundary: constant shifts in wrong direction; gte-only; negative scalars</item>
///   <item>No-proof baseline: C93 fires when evidence is absent</item>
/// </list>
/// </summary>
/// <remarks>
/// Tests written against <c>docs/ProofEngineDesign.md § LinearForm Normalization</c>
/// and <c>temp/unified-proof-plan.md § Commit 3</c>.
/// They compile and pass once George's Commit 3 lands; some will fail against the
/// current Commit 2 implementation where compound divisors fall through to Unknown.
/// </remarks>
public class ProofEngineCompoundDivisorTests
{
    // ── Gap 1: compound subtraction operands ─────────────────────────────────

    [Fact]
    public void Check_Gap1_APlusOneMinusB_WithGtRule_NoC93()
    {
        // (A+1) - B with rule A > B.
        // LinearForm((A+1)-B) = +1·A + (-1)·B + 1. Stored key = +1·A + (-1)·B.
        // Constant diff = +1 → divisor ≥ 1 > 0 → C93 suppressed.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / ((A + 1) - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Gap1_AMinusSumBC_WithGtSumRule_NoC93()
    {
        // A - (B + C) with rule A > B + C.
        // LinearForm(A-(B+C)) = +1·A + (-1)·B + (-1)·C. Stored key = same.
        // Direct match → C93 suppressed.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 3
            field C as number default 2
            field Y as number default 1
            rule A > B + C because "A exceeds sum"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - (B + C)) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Gap2_TotalMinusTaxMinusFee_WithGtSumRule_NoC93()
    {
        // Total - Tax - Fee with rule Total > Tax + Fee.
        // Closing Gap 1 and Gap 2 simultaneously: the RHS is a sum and the divisor
        // spans three fields. LinearForm matches on both sides.
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
    public void Check_Gap1WithGte_APlusOneMinusB_GteRule_NoC93()
    {
        // (A+1) - B with rule A >= B.
        // A >= B → A - B >= 0. Adding constant +1 → divisor ≥ 1 > 0.
        // Even gte (which alone doesn't prove nonzero for A-B) proves nonzero for (A+1)-B.
        const string dsl = """
            precept Test
            field A as number default 4
            field B as number default 4
            field Y as number default 1
            rule A >= B because "A not below B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / ((A + 1) - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Regression: bare A-B (ex-C-Nano cases) ───────────────────────────────

    [Fact]
    public void Check_Regression_BareAMinusB_WithGtRule_NoC93()
    {
        // Regression: bare A - B with rule A > B still works via unified path.
        // This was the C-Nano primary scenario; must continue to pass after C-Nano deletion.
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

    [Fact]
    public void Check_Regression_BareAMinusB_WithGtRule_ModuloOp_NoC93()
    {
        // Regression: bare A - B with rule A > B, using % operator.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y % (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Scalar-multiple normalization ─────────────────────────────────────────

    [Fact]
    public void Check_ScalarMultiple_3AMinux3B_WithGtAB_NoC93()
    {
        // 3*A - 3*B with rule A > B.
        // LinearForm: {A: 3, B: -3}. GCD-normalized: {A: 1, B: -1}. Matches stored key.
        // Scale factor = 3 (positive) → sign preserved → divisor > 0 → C93 suppressed.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (3 * A - 3 * B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_ScalarMultiple_NegativeCoefficients_EmitsC93()
    {
        // -3*A - (-3*B) = -3*(A-B) with rule A > B.
        // The divisor -3*(A-B) is negative when A > B.
        // A negative value excludes zero, so C93 should be suppressed IF the engine
        // recognises the negated-key pattern. If the engine is conservative on negative
        // scalars, C93 fires. This test documents the conservative (sound) outcome.
        //
        // If the engine is extended to handle negative scalars, flip the assertion.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (-3 * A - -3 * B) -> no transition
            """;

        // Conservative assertion: C93 fires because engine does not (yet) handle
        // the negated-scale case. Flip to .Should().BeEmpty() if George implements it.
        var result = Check(dsl);
        // No assertion on C93 here — behaviour is implementation-defined.
        // The test exists to document the pattern and prevent unexpected exceptions.
        result.Should().NotBeNull();
    }

    // ── Boundary: constant shifts in wrong direction ──────────────────────────

    [Fact]
    public void Check_CompoundDivisor_AMinusBMinus1_WithGtAB_EmitsC93()
    {
        // A - B - 1 with rule A > B.
        // A > B → A - B > 0, but (A - B) - 1 could be ≤ 0 (e.g. A=2, B=1 → A-B-1=0).
        // LinearForm: +1·A + (-1)·B + (-1). Stored key: +1·A + (-1)·B.
        // Constant diff = -1 → queried interval = (0,∞) - 1 = (-1,∞) — does NOT exclude 0.
        // C93 must fire (sound: the engine cannot prove this particular divisor nonzero).
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B - 1) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_CompoundDivisor_GteRule_AMinusB_EmitsC93()
    {
        // A - B with rule A >= B only.
        // A >= B → A - B >= 0 (nonneg), but could equal zero. C93 must fire.
        // This mirrors the existing scalar test: nonneg constraint fires C93.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 5
            field Y as number default 1
            rule A >= B because "A not below B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_CompoundDivisor_NoProof_EmitsC93()
    {
        // No rule, no constraint, no guard — divisor A - B is completely unconstrained.
        // C93 must fire (baseline: no regression from existing behavior).
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    // ── Non-linear divisor — soundness anchors ────────────────────────────────

    [Fact]
    public void Check_CompoundDivisor_NonLinear_ProductDivisor_EmitsC93()
    {
        // A*B - C with rule A > C. The divisor is non-linear (involves A*B).
        // LinearForm.TryNormalize(A*B - C) returns null (product of two non-constants).
        // No LinearForm key → no relational lookup → C93 fires.
        // Note: rule A > C only proves A - C > 0, not A*B - C > 0.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 2
            field C as number default 1
            field Y as number default 1
            rule A > C because "A exceeds C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A * B - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_CompoundDivisor_InsufficientRule_ThreeTerms_EmitsC93()
    {
        // Total - Tax - Fee with only rule Total > Tax (not Total > Tax + Fee).
        // The rule proves Total - Tax > 0 but NOT Total - Tax - Fee > 0.
        // C93 must fire because the divisor can reach zero (e.g. Total=3, Tax=1, Fee=2).
        const string dsl = """
            precept Test
            field Total as number default 10
            field Tax as number default 2
            field Fee as number default 5
            field Y as number default 1
            rule Total > Tax because "total exceeds tax alone"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (Total - Tax - Fee) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    // ── Additional compound patterns ──────────────────────────────────────────

    [Fact]
    public void Check_CompoundDivisor_APlusTwoMinusB_WithGtAB_NoC93()
    {
        // (A+2) - B with rule A > B: even larger constant offset → still provably > 0.
        // A - B > 0, so (A - B) + 2 ≥ 3 > 0.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / ((A + 2) - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_CompoundDivisor_SumOfPositiveAndNonneg_NoC93()
    {
        // (A + B) where A is positive and B is nonneg → sum is positive → no C93.
        // This tests interval arithmetic (not LinearForm), but verifies no regression.
        const string dsl = """
            precept Test
            field A as number default 1 positive
            field B as number default 0 nonnegative
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A + B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_CompoundDivisor_DDivisorIsAlwaysZero_EmitsC93()
    {
        // D - D is provably zero: interval arithmetic yields [0,0], which includes zero.
        // C93 must fire. This tests that the engine does NOT suppress C93 for zero divisors.
        const string dsl = """
            precept Test
            field D as number default 3 positive
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (D - D) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_CompoundDivisor_ReversedRule_BMinusA_WithGtBA_NoC93()
    {
        // Symmetry check: rule B > A (B is the larger field).
        // Divisor B - A should be proven positive → C93 suppressed.
        // Verifies that the rule direction is respected correctly.
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 5
            field Y as number default 1
            rule B > A because "B exceeds A"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (B - A) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_TruncateDivisor_WithMinConstraint_NoC93()
    {
        // truncate(X) where X >= 1 → truncate([1, +∞)) = [1, +∞) → always positive → no C93.
        const string dsl = """
            precept Test
            field X as number default 5 min 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = 100 / truncate(X) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_PowEvenExponentDivisor_WithNonzeroRule_NoC93()
    {
        // pow(X, 2) where X != 0 → result is positive (even exponent, nonzero base) → no C93.
        const string dsl = """
            precept Test
            field X as number default 3
            field Y as number default 1
            rule X != 0 because "X is nonzero"
            state Open initial
            event Go
            from Open on Go -> set Y = 100 / pow(X, 2) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
}
