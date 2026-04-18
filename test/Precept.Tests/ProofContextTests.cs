using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Wrapper-contract tests for <see cref="ProofContext"/>.
/// Verifies that ProofContext correctly delegates to the underlying proof infrastructure
/// for all query methods and preserves copy-on-write semantics for mutation methods.
/// </summary>
public class ProofContextTests
{
    // ── Symbols property ─────────────────────────────────────────────────────

    [Fact]
    public void Symbols_ExposesUnderlyingDictionary()
    {
        var dict = new Dictionary<string, StaticValueKind>
        {
            ["Price"] = StaticValueKind.Number,
            ["Amount"] = StaticValueKind.Number,
        };
        var ctx = new ProofContext(dict);

        ctx.Symbols.Should().BeEquivalentTo(dict);
    }

    // ── IntervalOf ───────────────────────────────────────────────────────────

    [Fact]
    public void IntervalOf_FieldWithPositiveFlag_ReturnsPositiveInterval()
    {
        var ctx = new ProofContext(
            new Dictionary<string, StaticValueKind>(),
            new Dictionary<LinearForm, RelationalFact>(),
            new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal),
            new Dictionary<string, NumericFlags>(System.StringComparer.Ordinal)
            {
                ["Price"] = NumericFlags.Positive | NumericFlags.Nonnegative | NumericFlags.Nonzero,
            },
            new Dictionary<LinearForm, NumericInterval>());
        var expr = PreceptParser.ParseExpression("Price");

        var interval = ctx.IntervalOf(expr);

        interval.IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IntervalOf_LiteralExpression_ReturnsSingleton()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("42");

        var interval = ctx.IntervalOf(expr);

        interval.Lower.Should().Be(42);
        interval.Upper.Should().Be(42);
        interval.LowerInclusive.Should().BeTrue();
        interval.UpperInclusive.Should().BeTrue();
    }

    // ── KnowsNonzero ─────────────────────────────────────────────────────────

    [Fact]
    public void KnowsNonzero_FieldWithGtRule_ReturnsTrue()
    {
        // A > B via WithRule → A - B > 0 (strictly positive, hence nonzero)
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));
        var expr = PreceptParser.ParseExpression("A - B");

        ctx.KnowsNonzero(expr).Should().BeTrue();
    }

    [Fact]
    public void KnowsNonzero_UnknownField_ReturnsFalse()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("X");

        ctx.KnowsNonzero(expr).Should().BeFalse();
    }

    // ── KnowsNonnegative ─────────────────────────────────────────────────────

    [Fact]
    public void KnowsNonnegative_FieldWithNonnegFlag_ReturnsTrue()
    {
        var ctx = new ProofContext(
            new Dictionary<string, StaticValueKind>(),
            new Dictionary<LinearForm, RelationalFact>(),
            new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal),
            new Dictionary<string, NumericFlags>(System.StringComparer.Ordinal)
            {
                ["Amount"] = NumericFlags.Nonnegative,
            },
            new Dictionary<LinearForm, NumericInterval>());
        var expr = PreceptParser.ParseExpression("Amount");

        ctx.KnowsNonnegative(expr).Should().BeTrue();
    }

    [Fact]
    public void KnowsNonnegative_UnconstrainedField_ReturnsFalse()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("X");

        ctx.KnowsNonnegative(expr).Should().BeFalse();
    }

    // ── SignOf ────────────────────────────────────────────────────────────────

    [Fact]
    public void SignOf_PositiveField_ReturnsPositive()
    {
        var ctx = new ProofContext(
            new Dictionary<string, StaticValueKind>(),
            new Dictionary<LinearForm, RelationalFact>(),
            new Dictionary<string, NumericInterval>(System.StringComparer.Ordinal),
            new Dictionary<string, NumericFlags>(System.StringComparer.Ordinal)
            {
                ["Rate"] = NumericFlags.Positive | NumericFlags.Nonnegative | NumericFlags.Nonzero,
            },
            new Dictionary<LinearForm, NumericInterval>());
        var expr = PreceptParser.ParseExpression("Rate");

        ctx.SignOf(expr).Should().Be(ProofSign.Positive);
    }

    [Fact]
    public void SignOf_UnknownField_ReturnsUnknown()
    {
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());
        var expr = PreceptParser.ParseExpression("X");

        ctx.SignOf(expr).Should().Be(ProofSign.Unknown);
    }

    // ── WithNarrowing ─────────────────────────────────────────────────────────

    [Fact]
    public void WithNarrowing_ReturnsNewContext_DoesNotMutateOriginal()
    {
        // Narrowing "Price > 0" (assumeTrue) should produce a new context with
        // Positive flag for Price — the original empty context must remain unchanged.
        var original = new ProofContext(new Dictionary<string, StaticValueKind>());
        var condition = PreceptParser.ParseExpression("Price > 0");

        var narrowed = original.WithNarrowing(condition, assumeTrue: true);

        narrowed.Should().NotBeSameAs(original);
        narrowed.Flags.Should().ContainKey("Price");
        ((narrowed.Flags["Price"] & NumericFlags.Positive) != 0).Should().BeTrue();
        original.Flags.Should().BeEmpty();
    }

    // ── Commit 3: C-Nano regression cases (via unified LinearForm path) ──────
    //
    // These tests port the C-Nano subtraction-proof scenarios verbatim.
    // In the unified engine (Commit 3) they exercise WithRule + IntervalOf
    // rather than the legacy $gt: marker convention.
    // Depends on: George's WithRule(lhs, RelationKind, rhs) and RelationKind enum.

    [Fact]
    public void IntervalOf_SimpleSubtraction_AMinusB_WithGtRule_IsPositive()
    {
        // C-Nano primary case: rule A > B → A - B > 0 (strictly positive, excludes zero).
        // WithRule stores a RelationalFact keyed by LinearForm(A) - LinearForm(B).
        // IntervalOf(A - B) looks up that key and intersects with (0,+∞).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("A - B"));

        interval.IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IntervalOf_SimpleSubtraction_BMinusA_WithGtAB_IsNegative()
    {
        // Reversed subtraction: A > B → B - A < 0 (excludes zero).
        // The engine must handle the negated LinearForm (key = -1·A + 1·B).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("B - A"));

        interval.ExcludesZero.Should().BeTrue();
        interval.Upper.Should().BeLessThanOrEqualTo(0);
    }

    [Fact]
    public void KnowsNonzero_Subtraction_WithGtRule_ReturnsTrue()
    {
        // C-Nano primary use case verbatim: WithRule(A > B) → KnowsNonzero(A - B) == true.
        // This is the exact scenario the C-Nano patch addressed; the unified engine must pass it.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        ctx.KnowsNonzero(PreceptParser.ParseExpression("A - B")).Should().BeTrue();
    }

    [Fact]
    public void IntervalOf_Subtraction_WithGteRule_IsNonneg()
    {
        // A >= B → A - B >= 0 (nonneg). The interval should start at 0 (inclusive) but
        // NOT be strictly positive — zero is included when A == B.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThanOrEqual,
                PreceptParser.ParseExpression("B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("A - B"));

        interval.IsNonnegative.Should().BeTrue();
        interval.IsPositive.Should().BeFalse("gte only proves nonneg; the divisor could equal zero when A == B");
    }

    [Fact]
    public void KnowsNonzero_Subtraction_WithGteRule_ReturnsFalse()
    {
        // Soundness boundary: A >= B proves nonneg but NOT nonzero.
        // A == B is a valid concrete value, so the engine MUST NOT claim nonzero.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThanOrEqual,
                PreceptParser.ParseExpression("B"));

        ctx.KnowsNonzero(PreceptParser.ParseExpression("A - B")).Should().BeFalse();
    }

    // ── Commit 3: Compound expression tests (Gap 1) ───────────────────────────
    //
    // Gap 1: compound subtraction operands ((A+1)-B, A-(B+C), Total-Tax-Fee)
    // were previously unhandled. LinearForm normalization closes this gap.

    [Fact]
    public void IntervalOf_CompoundSubtraction_APlusOneMinusB_WithGtRule_IsPositive()
    {
        // Gap 1: (A+1) - B with rule A > B.
        // A > B → A - B > 0. (A+1) - B = (A - B) + 1 ≥ 1 > 0.
        // LinearForm((A+1)-B) = +1·A + (-1)·B + 1. Stored key = +1·A + (-1)·B.
        // Constant difference = +1, so the divisor is ≥ 1 > 0.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("(A + 1) - B"));

        interval.IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IntervalOf_ThreeTermSubtraction_TotalMinusTaxMinusFee_WithGtTotalRule()
    {
        // Gap 1 multi-term: Total - Tax - Fee with rule Total > Tax + Fee.
        // LinearForm(Total - Tax - Fee) = +1·Total + (-1)·Tax + (-1)·Fee.
        // WithRule(Total, GT, Tax+Fee) stores key = LinearForm(Total) - LinearForm(Tax+Fee)
        //   = +1·Total + (-1)·Tax + (-1)·Fee — exact match, no constant difference.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("Total"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("Tax + Fee"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("Total - Tax - Fee"));

        interval.IsPositive.Should().BeTrue();
    }

    // ── Commit 3: Sum-on-RHS relational fact (Gap 2) ──────────────────────────
    //
    // Gap 2: rule Total > Tax + Fee (sum on RHS) was not previously injectable.
    // WithRule now normalizes both sides, keying on LinearForm(LHS) - LinearForm(RHS).

    [Fact]
    public void IntervalOf_SumOnRhs_TotalMinusTaxMinusFee_WithGtSumRule_IsPositive()
    {
        // Gap 2: rule Total > Tax + Fee stored directly via WithRule.
        // KnowsNonzero bridges Gap 1+2: divisor Total - Tax - Fee normalizes
        // to the same LinearForm key as LHS - RHS of the rule.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("Total"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("Tax + Fee"));

        ctx.KnowsNonzero(PreceptParser.ParseExpression("Total - Tax - Fee")).Should().BeTrue();
    }

    // ── Commit 3: Scalar-multiple normalization ───────────────────────────────
    //
    // Before relational lookup, IntervalOf GCD-normalizes the queried LinearForm.
    // 3·A + (-3)·B → GCD=3 → 1·A + (-1)·B. Direct match with the A > B stored fact.

    [Fact]
    public void IntervalOf_ScalarMultiple_3AMinus3B_WithGtAB_IsPositive()
    {
        // 3*A - 3*B with rule A > B.
        // LinearForm: {A: 3, B: -3}. GCD = 3. GCD-normalized: {A: 1, B: -1}.
        // Stored key (from WithRule A > B): {A: 1, B: -1}. Direct match after normalization.
        // Scale factor = 3 (positive) → sign preserved → interval is (0,+∞).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("3 * A - 3 * B"));

        interval.IsPositive.Should().BeTrue();
    }

    // ── Commit 3: Copy-on-write and mutation semantics ────────────────────────

    [Fact]
    public void WithGuard_NarrowsCorrectly()
    {
        // WithGuard(A > B, branch=true) should narrow context so IntervalOf(A - B) is positive.
        // Depends on George renaming WithNarrowing → WithGuard (or adding WithGuard as an alias).
        var empty = new ProofContext(new Dictionary<string, StaticValueKind>());
        var condition = PreceptParser.ParseExpression("A > B");

        var narrowed = empty.WithGuard(condition, branch: true);

        narrowed.Should().NotBeSameAs(empty);
        narrowed.IntervalOf(PreceptParser.ParseExpression("A - B")).IsPositive.Should().BeTrue();
    }

    [Fact]
    public void WithRule_StoresRelationalFact()
    {
        // WithRule must store a relational fact that IntervalOf can look up via LinearForm.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("Price"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("Cost"));

        ctx.IntervalOf(PreceptParser.ParseExpression("Price - Cost")).IsPositive.Should().BeTrue();
    }

    // ── Additional tests ──────────────────────────────────────────────────────

    [Fact]
    public void WithRule_ReturnsNewContext_DoesNotMutateOriginal()
    {
        // WithRule must be copy-on-write — the original context must remain unchanged.
        var original = new ProofContext(new Dictionary<string, StaticValueKind>());

        var narrowed = original.WithRule(
            PreceptParser.ParseExpression("A"),
            RelationKind.GreaterThan,
            PreceptParser.ParseExpression("B"));

        narrowed.Should().NotBeSameAs(original);
        // original has no relational fact for A - B — result should NOT be positive:
        original.IntervalOf(PreceptParser.ParseExpression("A - B")).IsPositive.Should().BeFalse();
        // narrowed does have the fact:
        narrowed.IntervalOf(PreceptParser.ParseExpression("A - B")).IsPositive.Should().BeTrue();
    }

    [Fact]
    public void KnowsNonnegative_WithGteRule_AMinusB_ReturnsTrue()
    {
        // A >= B → A - B >= 0 → KnowsNonnegative returns true.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThanOrEqual,
                PreceptParser.ParseExpression("B"));

        ctx.KnowsNonnegative(PreceptParser.ParseExpression("A - B")).Should().BeTrue();
    }

    [Fact]
    public void SignOf_WithGtRule_AMinusB_ReturnsPositive()
    {
        // A > B → SignOf(A - B) = Positive (the strongest classification).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        ctx.SignOf(PreceptParser.ParseExpression("A - B")).Should().Be(ProofSign.Positive);
    }

    [Fact]
    public void IntervalOf_AMinusSumBC_WithGtSumRule_IsPositive()
    {
        // Gap 1+2 combined: A - (B + C) with rule A > B + C.
        // WithRule(A, GT, B+C): key = LinearForm(A) - LinearForm(B+C) = +1·A + (-1)·B + (-1)·C.
        // Divisor A - (B + C) normalizes to the same key → direct match.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B + C"));

        ctx.IntervalOf(PreceptParser.ParseExpression("A - (B + C)")).IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IntervalOf_SelfSubtraction_AMinusA_LinearFormGivesZero()
    {
        // A - A normalizes to LinearForm with empty terms and constant 0.
        // The engine should return the singleton interval [0, 0].
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("A - A"));

        interval.Lower.Should().Be(0);
        interval.Upper.Should().Be(0);
    }

    [Fact]
    public void KnowsNonzero_LiteralPositive_ReturnsTrue()
    {
        // Literal 5 → interval [5, 5]. Lower = 5 > 0 → ExcludesZero = true.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());

        ctx.KnowsNonzero(PreceptParser.ParseExpression("5")).Should().BeTrue();
    }

    [Fact]
    public void IntervalOf_ConstantPositive_IsPositive()
    {
        // Literal 42 → singleton interval [42, 42] → IsPositive = true.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("42"));

        interval.IsPositive.Should().BeTrue();
    }

    [Fact]
    public void WithRule_NonNormalizableLhs_DoesNotThrow()
    {
        // abs(X) is not normalizable to a LinearForm.
        // WithRule must fail gracefully (store nothing) rather than throw.
        // Querying IntervalOf(abs(X) - B) should return a non-positive result — not throw.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>());

        var act = () => ctx.WithRule(
            PreceptParser.ParseExpression("abs(X)"),
            RelationKind.GreaterThan,
            PreceptParser.ParseExpression("B"));

        act.Should().NotThrow();

        var narrowed = act();
        // Non-normalizable LHS: no relational fact stored. C93 would fire here.
        narrowed.KnowsNonzero(PreceptParser.ParseExpression("abs(X) - B")).Should().BeFalse();
    }

    [Fact]
    public void IntervalOf_ScalarHalf_HalfAMinusHalfB_WithGtAB_IsPositive()
    {
        // 0.5*A - 0.5*B with rule A > B.
        // LinearForm: {A: Rational(1,2), B: Rational(-1,2)}.
        // GCD of coefficient absolute values = Rational(1,2).
        // GCD-normalized: {A: 1, B: -1}. Matches stored key from rule A > B.
        // Scale factor = 1/2 (positive) → sign preserved → interval is (0,+∞).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        ctx.IntervalOf(PreceptParser.ParseExpression("0.5 * A - 0.5 * B")).IsPositive.Should().BeTrue();
    }

    [Fact]
    public void WithGuard_GteCondition_NarrowsToNonneg()
    {
        // WithGuard(A >= B, branch=true) narrows context so IntervalOf(A - B) is nonneg.
        var empty = new ProofContext(new Dictionary<string, StaticValueKind>());
        var condition = PreceptParser.ParseExpression("A >= B");

        var narrowed = empty.WithGuard(condition, branch: true);

        narrowed.IntervalOf(PreceptParser.ParseExpression("A - B")).IsNonnegative.Should().BeTrue();
    }

    [Fact]
    public void KnowsNonzero_Compound_APlusOneMinusB_WithGtAB_ReturnsTrue()
    {
        // Gap 1: (A+1) - B with rule A > B should be provably nonzero.
        // A > B → A - B > 0 → (A - B) + 1 ≥ 1 > 0 → ExcludesZero = true.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("B"));

        ctx.KnowsNonzero(PreceptParser.ParseExpression("(A + 1) - B")).Should().BeTrue();
    }

    [Fact]
    public void SignOf_WithGteRule_AMinusB_ReturnsNonneg()
    {
        // A >= B → SignOf(A - B) = Nonneg (not Positive, since A == B is possible).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThanOrEqual,
                PreceptParser.ParseExpression("B"));

        ctx.SignOf(PreceptParser.ParseExpression("A - B")).Should().Be(ProofSign.Nonneg);
    }

    // ── Step 10: W1 inclusivity fix + W2 GCD normalization ────────────────────

    [Fact]
    public void IntervalOf_GteRule_AMinusB_LowerIsInclusive()
    {
        // W1 regression: rule A >= B → IntervalOf(A - B) = [0, +∞).
        // ConstantOffsetScan with c=0 must return LowerInclusive = true.
        // Before the fix, it returned (0, +∞) — LowerInclusive = false.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThanOrEqual,
                PreceptParser.ParseExpression("B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("A - B"));

        interval.Lower.Should().Be(0);
        interval.LowerInclusive.Should().BeTrue("A >= B means A - B >= 0, so [0, +∞) not (0, +∞)");
    }

    [Fact]
    public void IntervalOf_ScaledGtRule_2AMinus2B_ProvesAMinusB_Safe()
    {
        // W2 regression: rule 2*A > 2*B stores key 2A-2B. Without GCD normalization
        // at storage, looking up A-B (which IS GCD-normalized at lookup) would miss.
        // With GCD normalization at storage, 2A-2B → A-B, and IntervalOf(A - B) = (0, +∞).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("2 * A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("2 * B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("A - B"));

        interval.ExcludesZero.Should().BeTrue("GCD normalization should make 2A-2B → A-B at storage, matching the lookup");
        interval.IsPositive.Should().BeTrue();
    }

    // ── Frank B1: W1/W2 tier-specific regression pins ─────────────────────────

    [Fact]
    public void IntervalOf_ConstantOffsetScan_GteZeroOffset_LowerIsInclusive()
    {
        // W1 regression pin: ConstantOffsetScan with c=1 and >= must return inclusive lower bound.
        // Stores A >= B + 1, queries A - B. Offset c = 0 - (-1) = 1 >= 0 → fires ConstantOffsetScan.
        // Before W1 fix, c == 0 case with >= would produce (0, +inf) instead of [0, +inf).
        // This test forces tier-4 path (not tier-1 direct match).
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("A"),
                RelationKind.GreaterThanOrEqual,
                PreceptParser.ParseExpression("B + 1"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("A - B"));

        interval.Lower.Should().Be(1.0);
        interval.LowerInclusive.Should().BeTrue("W1 fix: >= with non-negative offset must be inclusive");
        interval.IsPositive.Should().BeTrue("A >= B+1 means A-B >= 1, which is positive");
    }

    [Fact]
    public void IntervalOf_StorageSideGcd_ScaledRule_2AGreaterThan2B_ProvesAMinusB()
    {
        // W2 regression pin: WithRule must GCD-normalize at storage time.
        // Stores rule 2*A > 2*B. Without W2 fix, stored key is {A:2, B:-2}.
        // With W2 fix, stored key is GCD-normalized to {A:1, B:-1}.
        // Query: IntervalOf(A - B) → LinearForm {A:1, B:-1} → direct match.
        var ctx = new ProofContext(new Dictionary<string, StaticValueKind>())
            .WithRule(
                PreceptParser.ParseExpression("2 * A"),
                RelationKind.GreaterThan,
                PreceptParser.ParseExpression("2 * B"));

        var interval = ctx.IntervalOf(PreceptParser.ParseExpression("A - B"));

        interval.IsPositive.Should().BeTrue("2A > 2B normalizes to A > B at storage time");
    }
}
