using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Soundness invariant tests for the Precept proof engine.
///
/// <para>
/// A proof engine is <b>sound</b> if it never suppresses C93 for a divisor that
/// genuinely CAN be zero. Equivalently: whenever the engine claims an interval
/// <c>ExcludesZero == true</c>, the concrete value in that interval must indeed
/// be nonzero for all inputs satisfying the declared rules/constraints.
/// </para>
///
/// <para>
/// These tests verify specific boundary conditions in the interval arithmetic and
/// relational fact lookup pipeline. Each test compiles a precept and asserts the
/// exact expected C93 outcome. An incorrect assertion means the engine either:
/// <list type="bullet">
///   <item>Suppressed C93 incorrectly (unsound — false negative, dangerous)</item>
///   <item>Emitted C93 incorrectly (incomplete — false positive, conservative but noisy)</item>
/// </list>
/// </para>
///
/// Coverage:
/// <list type="bullet">
///   <item>Positive vs. nonneg field constraints — only positive excludes zero</item>
///   <item>GT vs. GTE rules — only GT proves A - B nonzero</item>
///   <item>Literal divisors — positive literal safe, zero literal unsafe</item>
///   <item>Multiplication of two positives — product is positive</item>
///   <item>Multiplication of positive × nonneg — product is nonneg, not provably nonzero</item>
///   <item>abs() opacity — nonneg but not nonzero</item>
///   <item>Sum of two positives — sum is positive</item>
///   <item>Sum of two nonneg — sum is nonneg, not provably nonzero</item>
///   <item>Three-step transitive GT chain — transitively proved positive</item>
///   <item>Negative constant offset kills proof</item>
///   <item>Nonneg plus positive constant becomes positive</item>
///   <item>Sequential assignment chain: nonneg → add constant → positive</item>
///   <item>Reassignment kills a previously derived positive proof</item>
/// </list>
/// </summary>
/// <remarks>
/// Written against <c>docs/ProofEngineDesign.md § Soundness Guarantee</c> and
/// <c>src/Precept/Dsl/NumericInterval.cs</c> transfer rules. These tests act as
/// the soundness regression gate: a new optimization passes only if all these
/// tests continue to emit the expected outcome.
/// </remarks>
public class ProofEngineSoundnessInvariantTests
{
    // ── Positive vs. nonneg field constraints ─────────────────────────────────

    [Fact]
    public void Soundness_PositiveConstraint_ExcludesZero_NoC93()
    {
        // Invariant: if field A has 'positive' constraint then IntervalOf(A) = (0, +∞).
        // ExcludesZero = true (lower == 0 exclusive). Y / A → no C93.
        // If this test breaks (C93 fires), the engine no longer recognises the positive
        // field constraint as a divisor-safety proof.
        const string dsl = """
            precept Test
            field A as number default 1 positive
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / A -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "positive constraint makes IntervalOf(A) = (0, +∞), which ExcludesZero");
    }

    [Fact]
    public void Soundness_NonnegConstraint_IncludesZero_EmitsC93()
    {
        // Invariant: if field A has 'nonnegative' constraint then IntervalOf(A) = [0, +∞).
        // ExcludesZero = false (lower == 0 inclusive). Y / A → C93.
        // A nonneg field can be zero (e.g., default 0 is valid). The engine must not
        // suppress C93 here — that would be unsound.
        const string dsl = """
            precept Test
            field A as number default 0 nonnegative
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / A -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "nonneg constraint makes IntervalOf(A) = [0, +∞); lower is 0 inclusive, so ExcludesZero = false");
    }

    // ── GT vs. GTE rules: only GT proves subtraction nonzero ──────────────────

    [Fact]
    public void Soundness_SubtractionWithGt_ExcludesZero_NoC93()
    {
        // Invariant: rule A > B stores {A:1,B:-1} GT. IntervalOf(A - B) = (0, +∞).
        // ExcludesZero = true. Y / (A - B) → no C93.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A strictly exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "GT rule makes IntervalOf(A-B) = (0, +∞), which ExcludesZero");
    }

    [Fact]
    public void Soundness_SubtractionWithGte_IncludesZero_EmitsC93()
    {
        // Invariant: rule A >= B stores {A:1,B:-1} GTE. IntervalOf(A - B) = [0, +∞).
        // ExcludesZero = false (A could equal B, making A - B = 0). Y / (A - B) → C93.
        // Suppressing C93 here would be unsound — the engine cannot prove A > B from A >= B.
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

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "GTE rule makes IntervalOf(A-B) = [0, +∞); A could equal B so ExcludesZero = false");
    }

    // ── Literal divisors ──────────────────────────────────────────────────────

    [Fact]
    public void Soundness_LiteralPositiveDivisor_ExcludesZero_NoC93()
    {
        // Invariant: literal 5 → IntervalOf(5) = [5, 5]. Lower = 5 > 0 → ExcludesZero.
        // Y / 5 is always safe. If C93 fires here the engine's literal handling is broken.
        const string dsl = """
            precept Test
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / 5 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "literal 5 produces a [5,5] singleton interval, which ExcludesZero (lower > 0)");
    }

    [Fact]
    public void Soundness_LiteralZeroDivisor_EmitsC92()
    {
        // Invariant: literal 0 → IntervalOf(0) = [0, 0]. ExcludesZero = false.
        // Y / 0 is always unsafe. The engine fires C92 (division by zero, literal 0),
        // which is a stronger diagnostic than C93 (unproven divisor). Both indicate
        // the divisor is provably unsafe; C92 fires when the divisor is exactly zero.
        const string dsl = """
            precept Test
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / 0 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C92",
            "literal 0 divisor fires C92 (division by zero), the stronger literal-specific diagnostic");
    }

    // ── Multiplication transfer rules ─────────────────────────────────────────

    [Fact]
    public void Soundness_ProductOfTwoPositives_ExcludesZero_NoC93()
    {
        // Invariant: A ∈ (0,+∞), B ∈ (0,+∞).
        // Multiply: both positive branch → result = (0·0, +∞·+∞) = (0, +∞). ExcludesZero.
        // Y / (A * B) → no C93. Both A and B must be nonzero for this to hold.
        const string dsl = """
            precept Test
            field A as number default 1 positive
            field B as number default 2 positive
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A * B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "product of two positive intervals (0,+∞)×(0,+∞) = (0,+∞) which ExcludesZero");
    }

    [Fact]
    public void Soundness_ProductNonnegTimesNonneg_IsNonnegOnly_EmitsC93()
    {
        // Invariant: A ∈ [0,+∞), B ∈ [0,+∞).
        // Multiply both-nonneg branch: lower = 0*0 = 0, LowerInclusive = true && true = true.
        // Result = [0, +∞). ExcludesZero = false (both A and B could simultaneously be 0).
        // Y / (A * B) → C93.
        const string dsl = """
            precept Test
            field A as number default 0 nonnegative
            field B as number default 0 nonnegative
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A * B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "product of [0,+∞)×[0,+∞) = [0,+∞); lower=0 inclusive, ExcludesZero=false");
    }

    // ── Absolute value opacity ────────────────────────────────────────────────

    [Fact]
    public void Soundness_AbsoluteValue_IsNonnegOnly_EmitsC93()
    {
        // Invariant: abs(A) with A ∈ (-∞, +∞) → Abs(Unknown) = [0, +∞). IsNonneg = true.
        // But ExcludesZero = false (abs(0) = 0). Y / abs(A) → C93.
        // abs() is nonneg-safe but not nonzero-safe: the engine must not confuse the two.
        const string dsl = """
            precept Test
            field A as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / abs(A) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "abs(A) is nonneg only: lower=0 inclusive, ExcludesZero=false (abs(0)=0)");
    }

    // ── Addition transfer rules ───────────────────────────────────────────────

    [Fact]
    public void Soundness_PositivePlusPositive_IsPositive_NoC93()
    {
        // Invariant: A ∈ (0,+∞), B ∈ (0,+∞).
        // Add: lower = 0+0 = 0 (exclusive ∧ exclusive = exclusive). Upper = +∞.
        // Result = (0, +∞). ExcludesZero = true. Y / (A + B) → no C93.
        const string dsl = """
            precept Test
            field A as number default 1 positive
            field B as number default 1 positive
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A + B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "sum of (0,+∞) + (0,+∞) = (0,+∞) which ExcludesZero");
    }

    [Fact]
    public void Soundness_NonnegPlusNonneg_IsNonnegOnly_EmitsC93()
    {
        // Invariant: A ∈ [0,+∞), B ∈ [0,+∞).
        // Add: lower = 0+0 = 0 (inclusive ∧ inclusive = inclusive). Result = [0, +∞).
        // ExcludesZero = false (both A and B could be 0). Y / (A + B) → C93.
        const string dsl = """
            precept Test
            field A as number default 0 nonnegative
            field B as number default 0 nonnegative
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A + B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "sum of [0,+∞) + [0,+∞) = [0,+∞); lower=0 inclusive, ExcludesZero=false");
    }

    // ── Transitive chain soundness ────────────────────────────────────────────

    [Fact]
    public void Soundness_TransitiveChain_ThreeStep_ExcludesZero_NoC93()
    {
        // Invariant: rule A > B and rule B > C transitively derive A > C via BFS.
        // IntervalOf(A - C) = (0, +∞). ExcludesZero = true. Y / (A - C) → no C93.
        // This validates the soundness of the BFS transitive closure: any BFS path
        // that reaches posVar must produce a strictly positive interval for the query.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 5
            field C as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "two-hop BFS transitively derives A > C from A > B and B > C; (A-C) ∈ (0,+∞)");
    }

    // ── Constant offset: correct direction vs. wrong direction ───────────────

    [Fact]
    public void Soundness_NegativeOffsetKillsProof_EmitsC93()
    {
        // Invariant: rule A > B → IntervalOf(A - B) = (0, +∞).
        // Subtract 1: IntervalOf((A - B) - 1) = (0,+∞) - [1,1] = (-1, +∞).
        // Lower = -1 (exclusive). -1 < 0, and 0 is in (-1, +∞). ExcludesZero = false.
        // Y / (A - B - 1) → C93. Example: A=2, B=1 → A-B-1 = 0. Must not suppress C93.
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

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "negative offset shifts (0,+∞) to (-1,+∞); zero is still reachable (A=2, B=1)");
    }

    [Fact]
    public void Soundness_NonnegPlusPositiveConstant_IsPositive_NoC93()
    {
        // Invariant: A ∈ [0,+∞) (nonneg). IntervalOf(A + 1) = [0,+∞) + [1,1] = [1, +∞).
        // Lower = 1 (inclusive). IsPositive = true (lower > 0). ExcludesZero = true.
        // Y / (A + 1) → no C93. A nonneg plus a positive constant is always positive.
        const string dsl = """
            precept Test
            field A as number default 0 nonnegative
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A + 1) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "[0,+∞) + [1,1] = [1,+∞); lower=1 inclusive, IsPositive=true, ExcludesZero=true");
    }

    // ── Sequential assignment chain soundness ────────────────────────────────

    [Fact]
    public void Soundness_AssignmentChain_NonnegPlusConstant_NoC93()
    {
        // Invariant: rule A >= B → IntervalOf(A - B) = [0, +∞). D = A - B is nonneg.
        // set E = D + 1 → IntervalOf(D + 1) = [0,+∞) + [1,1] = [1, +∞). IsPositive.
        // Y / E → no C93. The assignment chain correctly propagates nonneg → positive.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field D as number default 0
            field E as number default 0
            field Y as number default 1
            rule A >= B because "A not below B"
            state Open initial
            event Go
            from Open on Go -> set D = A - B -> set E = D + 1 -> set Y = Y / E -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "D=A-B∈[0,+∞) (GTE rule); E=D+1∈[1,+∞); IsPositive=true; Y/E is safe");
    }

    // ── Reassignment kills proof soundness ────────────────────────────────────

    [Fact]
    public void Soundness_ReassignmentKillsPositiveProof_EmitsC93()
    {
        // Invariant: set Net = Gross - Tax → rule Gross > Tax makes Net positive.
        // set Net = Amount (no proof for Amount) → positive marker on Net is cleared.
        // The kill is required for soundness: without it, subsequent reassignment to an
        // unknown value would allow Net to be zero while the engine claims it's nonzero.
        // Y / Net after reassignment → C93.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field Amount as number default 3
            field Y as number default 1
            rule Gross > Tax because "gross exceeds tax"
            state Open initial
            event Go
            from Open on Go -> set Net = Gross - Tax -> set Net = Amount -> set Y = Y / Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "set Net = Amount (no proof) kills $positive:Net; engine must not retain stale interval");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
}
