using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Full-pipeline tests for Gap 3 — forward inference at assignment:
/// <c>set Net = Gross - Tax</c> computes an interval for <c>Net</c> that subsequent
/// expressions in the same transition row can use for divisor and sqrt safety proofs.
///
/// <para>Each test compiles a complete precept via <see cref="PreceptTypeChecker.Check"/>
/// and asserts whether C93 (unproven divisor) or C76 (sqrt nonneg) is suppressed or emitted.</para>
///
/// Coverage:
/// <list type="bullet">
///   <item>Core gap: <c>set Net = Gross - Tax</c> with <c>rule Gross &gt; Tax</c> → <c>Y / Net</c> safe</item>
///   <item>Multi-step chain with negative constant offset → still C93</item>
///   <item>Reassignment invalidates derived interval</item>
///   <item>Literal RHS (positive, zero, negative) → correct sign markers</item>
///   <item>Identifier copy preserves proof</item>
///   <item>abs() opacity — nonneg but not nonzero</item>
///   <item>Guard-narrowing derives interval within the same event</item>
///   <item>Cross-event isolation (soundness anchor): derived interval does NOT cross event boundaries</item>
///   <item>Sqrt on computed nonneg field</item>
///   <item>Modulo operator with computed divisor</item>
///   <item>Re-derivation after reassignment</item>
///   <item>Sequential chain with constant offset</item>
/// </list>
/// </summary>
/// <remarks>
/// Tests written against <c>docs/ProofEngineDesign.md § Gap 3</c>
/// and <c>temp/unified-proof-plan.md § Commit 4</c>.
/// All tests pass once George's Commit 4 lands (forward inference at assignment via <c>IntervalOf(rhs)</c>).
/// </remarks>
public class ProofEngineComputedFieldTests
{
    // ── Core Gap 3: set Net = compound expr, then use Net as divisor ──────────

    [Fact]
    public void Check_ComputedField_SetNetGrossMinusTax_DivisorNet_WithGtRule_NoC93()
    {
        // Core Gap 3 scenario: rule Gross > Tax proves Net = Gross - Tax is positive.
        // ApplyAssignmentNarrowing calls IntervalOf(Gross - Tax) which returns (0, +∞)
        // via the relational fact {Gross:1, Tax:-1} GT. Net gets $positive:Net.
        // Y / Net: identifier check finds $positive:Net → C93 suppressed.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field Y as number default 1
            rule Gross > Tax because "gross exceeds tax"
            state Open initial
            event Go
            from Open on Go -> set Net = Gross - Tax -> set Y = Y / Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_ComputedField_SetAFromPositivePair_SetBMinus1_DivisorB_C93()
    {
        // Multi-step chain with negative constant offset.
        // rule Price > 0 and rule Qty > 0 make both fields positive.
        // set A = Price + Qty → A ∈ (0, +∞) → A is positive.
        // set B = A - 1 → B ∈ (-1, +∞) — includes zero (e.g. Price=0.5, Qty=0.5 → A=1 → B=0).
        // Wait: Price > 0 and Qty > 0 means Price, Qty ≥ tiny positive. A could be 1 → B = 0.
        // IntervalOf(A - 1) = (0, +∞) - [1, 1] = (-1, +∞). Does NOT exclude zero.
        // Z / B → compound divisor, KnowsNonzero → false → C93 fires.
        const string dsl = """
            precept Test
            field Price as number default 5
            field Qty as number default 2
            field A as number default 0
            field B as number default 0
            field Z as number default 1
            rule Price > 0 because "price must be positive"
            rule Qty > 0 because "qty must be positive"
            state Open initial
            event Go
            from Open on Go -> set A = Price + Qty -> set B = A - 1 -> set Z = Z / B -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "A-1 can reach zero even when A is strictly positive (A could equal 1)");
    }

    [Fact]
    public void Check_ComputedField_ReassignmentKillsDerivedInterval_C93()
    {
        // rule Gross > Tax proves Net = Gross - Tax is positive (first assignment).
        // Reassigning Net = Amount (where Amount has no proof) kills the positive marker.
        // Kill loop in ApplyAssignmentNarrowing removes $positive:Net, $nonneg:Net, $nonzero:Net.
        // Y / Net: identifier check finds no nonzero proof → C93 fires.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field Amount as number default 5
            field Y as number default 1
            rule Gross > Tax because "gross exceeds tax"
            state Open initial
            event Go
            from Open on Go -> set Net = Gross - Tax -> set Net = Amount -> set Y = Y / Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "reassigning Net to Amount (no proof) kills the positive marker derived from Gross - Tax");
    }

    // ── Literal RHS: sign markers injected correctly ──────────────────────────

    [Fact]
    public void Check_ComputedField_LiteralPositiveRhs_NoC93()
    {
        // set X = 5: literal 5 is positive. ApplyAssignmentNarrowing stores $positive:X,
        // $nonneg:X, $nonzero:X, and a point interval [5, 5] (ExcludesZero = true).
        // Y / X: identifier check finds $positive:X → C93 suppressed.
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set X = 5 -> set Y = Y / X -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_ComputedField_LiteralZeroRhs_C92()
    {
        // set X = 0: literal 0 produces interval [0,0] — provably zero.
        // Y / X: AssessDivisorSafety finds [0,0] → Contradiction → C92.
        const string dsl = """
            precept Test
            field X as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set X = 0 -> set Y = Y / X -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C92",
            "X = 0 is provably zero — contradiction, not just obligation");
    }

    [Fact]
    public void Check_ComputedField_NegativeLiteralRhs_Nonzero_NoC93()
    {
        // set X = -5: negative literal. ApplyAssignmentNarrowing stores $nonzero:X
        // AND a point interval [-5, -5] (ExcludesZero = true, Lower=-5 < 0, Upper=-5 < 0).
        // ExtractIntervalFromMarkers finds the $ival: marker first (priority) → [-5, -5].
        // ExcludesZero: Upper=-5 < 0 → true. Y / X: compound divisor check, KnowsNonzero → true.
        // C93 suppressed.
        const string dsl = """
            precept Test
            field X as number default 1
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set X = 0 - 5 -> set Y = Y / X -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Identifier copy: proof propagates from source field ───────────────────

    [Fact]
    public void Check_ComputedField_IdentifierCopy_PositiveField_NoC93()
    {
        // field A declared positive (has $positive:A globally).
        // set B = A: identifier RHS → ApplyAssignmentNarrowing copies all markers:
        //   $positive:A → $positive:B, $nonneg:A → $nonneg:B, etc.
        // Y / B: identifier check finds $positive:B → C93 suppressed.
        const string dsl = """
            precept Test
            field A as number default 1 positive
            field B as number default 0
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set B = A -> set Y = Y / B -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Compound RHS: function call opacity ───────────────────────────────────

    [Fact]
    public void Check_ComputedField_AbsRhs_NonnegNotNonzero_C93()
    {
        // set X = abs(A): abs() maps Unknown → [0, +∞] (nonneg, includes zero).
        // ApplyAssignmentNarrowing: rhsInterval.IsNonnegative = true → stores $nonneg:X only.
        // Y / X: identifier check finds $nonneg:X but no $positive:X / $nonzero:X
        // → "nonneg but not nonzero" C93 fires.
        const string dsl = """
            precept Test
            field A as number default 3
            field X as number default 0
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set X = abs(A) -> set Y = Y / X -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "abs(A) is nonneg but can be zero when A = 0");
    }

    // ── Core Gap 3 variants ───────────────────────────────────────────────────

    [Fact]
    public void Check_ComputedField_DirectSubtraction_WithGtRule_NoC93()
    {
        // Baseline: rule A > B, set D = A - B, then Y / D.
        // IntervalOf(A - B) → relational fact {A:1, B:-1} GT → (0, +∞).
        // D gets $positive:D. Y / D: $positive:D → no C93.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field D as number default 0
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set D = A - B -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_ComputedField_GuardNarrowingDerivesInterval_NoC93()
    {
        // Guard when Gross > Tax establishes {Gross:1, Tax:-1} GT in event context.
        // set Net = Gross - Tax: IntervalOf returns (0, +∞) via guard fact.
        // Net gets $positive:Net. Y / Net → no C93.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go when Gross > Tax -> set Net = Gross - Tax -> set Y = Y / Net -> no transition
            from Open on Go -> reject "below threshold"
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Soundness anchor: cross-event isolation ───────────────────────────────

    [Fact]
    public void Check_ComputedField_CrossEventIsolation_SecondEventEmitsC93()
    {
        // SOUNDNESS ANCHOR: derived interval for Net from event SetNet
        // must NOT leak into event UseNet's proof context.
        //
        // Event SetNet: set Net = Gross - Tax (with rule Gross > Tax). Net gets $positive:Net
        //   inside SetNet's local context only.
        // Event UseNet: builds fresh context from global. Net has no global positive marker.
        //   Y / Net: identifier check → no $positive:Net / $nonzero:Net → C93 fires.
        //
        // If this test starts passing (C93 suppressed), that is a cross-event leakage BUG.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field Y as number default 1
            rule Gross > Tax because "gross exceeds tax"
            state Open initial
            event SetNet
            event UseNet
            from Open on SetNet -> set Net = Gross - Tax -> no transition
            from Open on UseNet -> set Y = Y / Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "proof derived in SetNet's row must not leak to UseNet's row — event isolation soundness");
    }

    // ── Sqrt on computed fields ───────────────────────────────────────────────

    [Fact]
    public void Check_ComputedField_SqrtOfDerivedNonneg_WithGteRule_NoC76()
    {
        // rule A >= B stores {A:1, B:-1} GTE → IntervalOf(A - B) = [0, +∞).
        // set D = A - B: D gets $nonneg:D and ival [0, +∞).
        // set Y = sqrt(D): KnowsNonneg(D) = IntervalOf(D).IsNonnegative = true (lower=0, inclusive).
        // C76 suppressed.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field D as number default 0
            field Y as number default 0
            rule A >= B because "A not below B"
            state Open initial
            event Go
            from Open on Go -> set D = A - B -> set Y = sqrt(D) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    // ── Modulo operator with computed divisor ─────────────────────────────────

    [Fact]
    public void Check_ComputedField_ModuloOperator_WithGtRule_NoC93()
    {
        // rule Gross > Tax: Net = Gross - Tax is positive. $positive:Net stored.
        // Amount % Net: same divisor-nonzero check as division.
        // Identifier check finds $positive:Net → C93 suppressed.
        const string dsl = """
            precept Test
            field Gross as number default 10
            field Tax as number default 2
            field Net as number default 0
            field Amount as number default 7
            field Y as number default 0
            rule Gross > Tax because "gross exceeds tax"
            state Open initial
            event Go
            from Open on Go -> set Net = Gross - Tax -> set Y = Amount % Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Reassignment: kill, then re-derive ────────────────────────────────────

    [Fact]
    public void Check_ComputedField_ReassignmentThenRederive_NoC93()
    {
        // rule A > B: set X = A - B (X > 0). set X = 0 (kill positive, store nonneg).
        // set X = A - B again: re-derives X > 0 because the relational fact {A:1, B:-1} is
        // still present (kill loop only removes facts mentioning X, not facts about A-B).
        // Y / X → $positive:X → no C93.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field X as number default 0
            field Y as number default 1
            rule A > B because "A exceeds B"
            state Open initial
            event Go
            from Open on Go -> set X = A - B -> set X = 0 -> set X = A - B -> set Y = Y / X -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Sum of positive fields ────────────────────────────────────────────────

    [Fact]
    public void Check_ComputedField_SumOfPositiveFields_NoC93()
    {
        // field A positive + field B positive: A ∈ (0, +∞), B ∈ (0, +∞).
        // set Sum = A + B: IntervalOf(A + B) = (0,+∞) + (0,+∞) = (0,+∞). Sum positive.
        // Y / Sum: identifier check finds $positive:Sum → no C93.
        const string dsl = """
            precept Test
            field A as number default 1 positive
            field B as number default 2 positive
            field Sum as number default 0
            field Y as number default 1
            state Open initial
            event Go
            from Open on Go -> set Sum = A + B -> set Y = Y / Sum -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Sequential chain: computed field used as building block ──────────────

    [Fact]
    public void Check_ComputedField_SequentialChainWithOffset_NoC93()
    {
        // rule P > Q: set A = P - Q → A ∈ (0, +∞).
        // set B = A + 1 → IntervalOf(A + 1) = (0,+∞) + [1,1] = (1, +∞). B positive.
        // Z / B: identifier check finds $positive:B → no C93.
        const string dsl = """
            precept Test
            field P as number default 5
            field Q as number default 1
            field A as number default 0
            field B as number default 0
            field Z as number default 1
            rule P > Q because "P exceeds Q"
            state Open initial
            event Go
            from Open on Go -> set A = P - Q -> set B = A + 1 -> set Z = Z / B -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Reassignment to nonneg (abs) kills positive proof ────────────────────

    [Fact]
    public void Check_ComputedField_ReassignToAbsKillsPositiveProof_C93()
    {
        // rule Gross > Tax: set Net = Gross - Tax → Net is positive ($positive:Net).
        // set Net = abs(Amount): kill loop clears $positive:Net.
        // abs(Amount) → nonneg but not nonzero → $nonneg:Net only.
        // Y / Net: identifier check finds $nonneg:Net, no $positive:Net / $nonzero:Net
        // → "nonneg but not nonzero" C93.
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
            from Open on Go -> set Net = Gross - Tax -> set Net = abs(Amount) -> set Y = Y / Net -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "abs(Amount) is nonneg-only; the positive proof from Gross-Tax was killed by reassignment");
    }

    // ── Derived nonneg + constant offset yields positive ─────────────────────

    [Fact]
    public void Check_ComputedField_NonnegPlusOne_BecomesPositive_NoC93()
    {
        // rule A >= B: set D = A - B → D ∈ [0, +∞) (nonneg).
        // set E = D + 1: IntervalOf(D + 1) = [0, +∞) + [1, 1] = [1, +∞). IsPositive = true.
        // Y / E: identifier check finds $positive:E → no C93.
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

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
}
