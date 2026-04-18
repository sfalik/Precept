using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Full-pipeline tests for Gap 4 — bounded transitive closure in <see cref="RelationalGraph"/>:
/// <c>rule A &gt; B</c> + <c>rule B &gt; C</c> derives <c>A &gt; C</c> so
/// <c>Y / (A - C)</c> has its divisor safety proven.
///
/// <para>Each test compiles a complete precept via <see cref="PreceptTypeChecker.Check"/>
/// and asserts whether C93 (unproven divisor) or C76 (sqrt nonneg) is suppressed or emitted.</para>
///
/// Coverage:
/// <list type="bullet">
///   <item>2-step, 3-step, 4-step chains (at depth cap)</item>
///   <item>5-step chain — beyond depth cap of 4 → sound false-negative (C93 fires)</item>
///   <item>Strict/non-strict matrix: all 4 combinations &gt;·&gt;, &gt;·&gt;=, &gt;=·&gt;, &gt;=·&gt;=</item>
///   <item>Self-contradiction and self-loop — silent drop, no throw</item>
///   <item>Disconnected graph — no path between groups</item>
///   <item>Reverse-direction form — forward BFS cannot reach target</item>
///   <item>Mixed guard + global fact in same event context</item>
///   <item>Modulo operator with transitive divisor</item>
///   <item>Constant-offset compound expression layered on transitive proof</item>
///   <item>Computed field derived from transitively proven interval</item>
///   <item>3-step all-GTE → nonneg → sqrt safe (no C76)</item>
/// </list>
/// </summary>
/// <remarks>
/// Tests written against <c>docs/ProofEngineDesign.md § Gap 4</c>
/// and <c>temp/unified-proof-plan.md § Commit 5</c>.
/// BFS caps: max 64 facts, depth 4, 256 visited nodes.
/// Strict/non-strict matrix: &gt;·&gt;⇒&gt;, &gt;·&gt;=⇒&gt;, &gt;=·&gt;⇒&gt;, &gt;=·&gt;=⇒&gt;=.
/// </remarks>
public class ProofEngineTransitiveClosureTests
{
    // ── BFS depth: 2-step, 3-step, 4-step (at cap), 5-step (beyond cap) ──────

    [Fact]
    public void Check_Transitive_TwoStep_AGtB_BGtC_NoC93()
    {
        // A > B and B > C → A > C by 2-hop BFS.
        // BFS from C: edge C → B (fact B > C), edge B → A (fact A > B). posVar A reached.
        // Combined strictness: GT · GT = GT → (0, +∞). KnowsNonzero(A-C) = true.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field C as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Transitive_ThreeStep_AGtB_BGtC_CGtD_NoC93()
    {
        // 3-hop chain: A > B > C > D → A > D transitively.
        // BFS from D → C (depth 1) → B (depth 2) → A (depth 3, finds posVar). No C93.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 7
            field C as number default 4
            field D as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            rule C > D because "C exceeds D"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - D) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Transitive_FourStep_AtDepthCap_NoC93()
    {
        // 4-hop chain: A > B > C > D > E → A > E. Depth used: 3 (processed at depth 3 < 4).
        // At depth 3, B is processed and A (posVar) is found → proof succeeds at the cap boundary.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 8
            field C as number default 6
            field D as number default 4
            field E as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            rule C > D because "C exceeds D"
            rule D > E because "D exceeds E"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - E) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Transitive_FiveStep_BeyondDepthCap_EmitsC93()
    {
        // 5-hop chain: A > B > C > D > E > F → would prove A > F, but requires 5 hops.
        // At depth 4 the BFS dequeues B with depth == MaxDepth (4) and skips it via 'continue'.
        // A (posVar) is never reached → transitive proof fails → C93 fires.
        // This is a sound false-negative: the engine is conservative, not wrong.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 8
            field C as number default 6
            field D as number default 4
            field E as number default 2
            field F as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            rule C > D because "C exceeds D"
            rule D > E because "D exceeds E"
            rule E > F because "E exceeds F"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - F) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "5-hop chain exceeds the BFS depth cap of 4; this is a sound false-negative");
    }

    // ── Strict/non-strict combination matrix ─────────────────────────────────

    [Fact]
    public void Check_Transitive_MixedGtGte_StrictResult_NoC93()
    {
        // A > B (strict) and B >= C (non-strict).
        // CombineStrictness(GT, GTE) = GT (one strict edge makes the path strict).
        // A - C ∈ (0, +∞) → ExcludesZero = true → no C93.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field C as number default 1
            field Y as number default 1
            rule A > B because "A strictly exceeds B"
            rule B >= C because "B not below C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Transitive_MixedGteGt_StrictResult_NoC93()
    {
        // A >= B (non-strict) and B > C (strict).
        // CombineStrictness(GTE, GT) = GT (the strict edge on the second hop).
        // A - C ∈ (0, +∞) → ExcludesZero = true → no C93.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field C as number default 1
            field Y as number default 1
            rule A >= B because "A not below B"
            rule B > C because "B strictly exceeds C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Transitive_AllGte_GteResult_EmitsC93OnDivisor()
    {
        // A >= B and B >= C → A >= C (non-strict only).
        // CombineStrictness(GTE, GTE) = GTE → A - C ∈ [0, +∞).
        // [0, +∞).ExcludesZero = false (lower=0 inclusive) → C93 fires (A could equal C).
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field C as number default 1
            field Y as number default 1
            rule A >= B because "A not below B"
            rule B >= C because "B not below C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "GTE+GTE transitivity only proves A >= C, not A > C; A - C can be zero");
    }

    [Fact]
    public void Check_Transitive_AllGte_SqrtSafe_NoC76()
    {
        // A >= B and B >= C → A >= C → A - C ∈ [0, +∞). IsNonnegative = true.
        // sqrt(A - C): KnowsNonneg(A - C) = true → C76 suppressed.
        // (C93 on the divisor test and C76 on the sqrt test are independent proofs of
        // the same fact: GTE+GTE proves nonneg but not nonzero.)
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 3
            field C as number default 1
            field Y as number default 0
            rule A >= B because "A not below B"
            rule B >= C because "B not below C"
            state Open initial
            event Go
            from Open on Go -> set Y = sqrt(A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    // ── Self-contradiction and self-loop ─────────────────────────────────────

    [Fact]
    public void Check_Transitive_SelfContradiction_DirectFact_NoThrow_NoC93()
    {
        // rule A > B and rule B > A are contradictory, but the type checker must not throw.
        // The direct fact {A:1, B:-1} GT is stored and found immediately (no BFS needed).
        // BFS cycle: if triggered, visited-set prevents infinite loop.
        // Y / (A - B): direct lookup finds (0, +∞) → KnowsNonzero = true → no C93, no throw.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > A because "contradictory rule"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty(
            "direct fact A > B proves A - B nonzero regardless of the contradiction");
    }

    [Fact]
    public void Check_Transitive_SelfLoop_NoProofForOtherPair_EmitsC93()
    {
        // rule A > A: LinearForm(A) - LinearForm(A) = {} with constant 0.
        // Stored as a degenerate fact with empty terms — never matched by a useful query.
        // No rule relating A and B exists. Y / (A - B): no proof. C93 fires.
        // The self-loop fact must NOT cause an exception or fabricate a proof.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field Y as number default 1
            rule A > A because "self-referential rule"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "rule A > A stores a degenerate empty-terms fact that proves nothing about A - B");
    }

    // ── Disconnected and reverse-direction cases ──────────────────────────────

    [Fact]
    public void Check_Transitive_DisconnectedGraph_NoPath_EmitsC93()
    {
        // rule A > B and rule C > D form two disconnected components.
        // No path from D to A → RelationalGraph.Query({A:1, D:-1}) finds no edges from D.
        // C93 fires.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 1
            field C as number default 8
            field D as number default 2
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule C > D because "C exceeds D"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - D) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "A and D are in disconnected components of the relational graph");
    }

    [Fact]
    public void Check_Transitive_ReverseDirectionForm_NoForwardPath_EmitsC93()
    {
        // rule B > A and rule C > B: stored facts are {B:1,A:-1} GT and {C:1,B:-1} GT.
        // These give edges A → B and B → C (forward direction: smaller → larger).
        // Query {A:1, C:-1} (A - C): posVar=A, negVar=C. BFS starts from C.
        // Edges from C: fact {C:1,B:-1} has fNeg=B ≠ C; fact {B:1,A:-1} has fNeg=A ≠ C.
        // No edges from C → BFS terminates → Unknown → C93 fires.
        //
        // Note: C > B > A mathematically means A - C < 0 (nonzero), but the forward BFS
        // only proves posVar > negVar, not posVar < negVar. This is a sound false-negative.
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 3
            field C as number default 5
            field Y as number default 1
            rule B > A because "B exceeds A"
            rule C > B because "C exceeds B"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C93",
            "forward BFS from C cannot reach A given the edge directions; sound false-negative");
    }

    // ── Mixed guard + global fact ─────────────────────────────────────────────

    [Fact]
    public void Check_Transitive_MixedGuardAndGlobalFact_NoC93()
    {
        // Global: rule B > C stores {B:1,C:-1} GT.
        // Guard: when A > B adds {A:1,B:-1} GT to event context.
        // Both facts exist in the same event context during the transition.
        // BFS for {A:1,C:-1}: C → B (global fact) → A (guard fact). posVar reached. No C93.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 5
            field C as number default 1
            field Y as number default 1
            rule B > C because "B exceeds C"
            state Open initial
            event Go
            from Open on Go when A > B -> set Y = Y / (A - C) -> no transition
            from Open on Go -> reject "below threshold"
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Modulo and compound expressions with transitive divisors ─────────────

    [Fact]
    public void Check_Transitive_ModuloOperator_TwoStep_NoC93()
    {
        // A > B > C → A - C ∈ (0, +∞) via transitive BFS.
        // X % (A - C): same divisor-nonzero check as division.
        // KnowsNonzero(A - C) = true → C93 suppressed.
        const string dsl = """
            precept Test
            field A as number default 7
            field B as number default 4
            field C as number default 1
            field X as number default 100
            field Y as number default 0
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            state Open initial
            event Go
            from Open on Go -> set Y = X % (A - C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_Transitive_ConstantOffsetOnTransitiveExpr_NoC93()
    {
        // A > B > C → IntervalOf(A - C) = (0, +∞) via transitive BFS (recursive call from binary +).
        // IntervalOf((A - C) + 1) = (0, +∞) + [1, 1] = (1, +∞). IsPositive = true. No C93.
        // The constant offset benefits from recursive IntervalOf on the sub-expression A - C.
        const string dsl = """
            precept Test
            field A as number default 7
            field B as number default 4
            field C as number default 1
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            state Open initial
            event Go
            from Open on Go -> set Y = Y / ((A - C) + 1) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Transitive proof feeds computed field (Gap 3 + Gap 4 combination) ────

    [Fact]
    public void Check_Transitive_ThreeStep_FeedsComputedField_NoC93()
    {
        // A > B > C: IntervalOf(A - C) = (0, +∞) via 2-hop BFS.
        // set D = A - C: ApplyAssignmentNarrowing stores $positive:D.
        // Y / D: identifier check finds $positive:D → no C93.
        // Tests that Gap 3 (forward inference at assignment) correctly captures
        // the interval derived by Gap 4 (transitive closure).
        const string dsl = """
            precept Test
            field A as number default 7
            field B as number default 4
            field C as number default 1
            field D as number default 0
            field Y as number default 1
            rule A > B because "A exceeds B"
            rule B > C because "B exceeds C"
            state Open initial
            event Go
            from Open on Go -> set D = A - C -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // ── Three-step all-GTE: sqrt safety ──────────────────────────────────────

    [Fact]
    public void Check_Transitive_ThreeStep_AllGte_SqrtSafe_NoC76()
    {
        // 3-hop all-GTE: A >= B >= C >= D → A - D ∈ [0, +∞) (nonneg).
        // BFS from D: {C:1,D:-1} GTE → C, combined GTE. {B:1,C:-1} GTE → B, combined GTE.
        // {A:1,B:-1} GTE → A = posVar. Return [0, +∞). IsNonneg = true.
        // sqrt(A - D): KnowsNonneg = true → C76 suppressed.
        const string dsl = """
            precept Test
            field A as number default 10
            field B as number default 7
            field C as number default 4
            field D as number default 1
            field Y as number default 0
            rule A >= B because "A not below B"
            rule B >= C because "B not below C"
            rule C >= D because "C not below D"
            state Open initial
            event Go
            from Open on Go -> set Y = sqrt(A - D) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static TypeCheckResult Check(string dsl) =>
        PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
}
