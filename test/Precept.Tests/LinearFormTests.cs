using FluentAssertions;
using Precept.Tests.Infrastructure;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Unit tests for <see cref="LinearForm"/> — the canonical normalizer used to key the
/// relational fact store in the unified proof engine.
/// Covers TryNormalize, algebra methods (Add/Subtract/Negate/ScaleByConstant),
/// depth-bound termination, commutative equality, and property tests
/// (associativity, commutativity, distributivity).
/// </summary>
/// <remarks>
/// Tests are written against the spec in <c>docs/ProofEngineDesign.md § LinearForm Normalization</c>
/// and <c>temp/unified-proof-plan.md § Implementation Manifest</c>.
/// George's <c>LinearForm.cs</c> is implemented in tandem; tests compile and pass once it lands.
/// <para>
/// Expression AST nodes are obtained by parsing mini-precepts via
/// <see cref="PreceptExpressionTestHelper.ParseFirstSetExpression"/>.
/// The parser does not type-check, so undeclared identifiers (A, B, C …) are
/// parsed as <see cref="PreceptIdentifierExpression"/> nodes — valid LinearForm terms.
/// </para>
/// </remarks>
public class LinearFormTests
{
    // ── Convenience ──────────────────────────────────────────────────────────

    private static PreceptExpression ParseExpr(string text) =>
        PreceptExpressionTestHelper.ParseFirstSetExpression(text);

    private static LinearForm? Normalize(string text) =>
        LinearForm.TryNormalize(ParseExpr(text));

    // Terms dictionary keys are plain strings (field names).
    private static readonly Rational R0 = new Rational(0, 1);
    private static readonly Rational R1 = new Rational(1, 1);
    private static readonly Rational RNeg1 = new Rational(-1, 1);
    private static readonly Rational R2 = new Rational(2, 1);
    private static readonly Rational R3 = new Rational(3, 1);
    private static readonly Rational R1Over2 = new Rational(1, 2);
    private static readonly Rational R1Over3 = new Rational(1, 3);

    // ── Literal normalization ─────────────────────────────────────────────────

    [Fact]
    public void Normalize_IntegerLiteral_ConstantOnlyForm()
    {
        var lf = Normalize("5");

        lf.Should().NotBeNull();
        lf!.Terms.Should().BeEmpty();
        lf.Constant.Should().Be(new Rational(5, 1));
    }

    [Fact]
    public void Normalize_NegativeLiteralConstant()
    {
        // Unary - on a literal: LinearForm.Negate(Constant=7) → Constant=-7
        var lf = Normalize("-7");

        lf.Should().NotBeNull();
        lf!.Terms.Should().BeEmpty();
        lf.Constant.Should().Be(new Rational(-7, 1));
    }

    [Fact]
    public void Normalize_DecimalLiteral_RationalCoefficient()
    {
        // 0.5 literal → Constant = Rational(1, 2) via decimal intermediary
        var lf = Normalize("0.5");

        lf.Should().NotBeNull();
        lf!.Terms.Should().BeEmpty();
        lf.Constant.Should().Be(R1Over2);
    }

    [Fact]
    public void Normalize_ConstantAddition_FoldsToConstant()
    {
        // 3 + 5 → no terms, Constant = 8
        var lf = Normalize("3 + 5");

        lf.Should().NotBeNull();
        lf!.Terms.Should().BeEmpty();
        lf.Constant.Should().Be(new Rational(8, 1));
    }

    // ── Identifier normalization ──────────────────────────────────────────────

    [Fact]
    public void Normalize_SingleIdentifier_SingleTermForm()
    {
        // Bare identifier A → {A: 1}, Constant = 0
        var lf = Normalize("A");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["A"].Should().Be(R1);
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_UnaryNegation_NegativeCoefficient()
    {
        // -A → {A: -1}, Constant = 0
        var lf = Normalize("-A");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["A"].Should().Be(RNeg1);
        lf.Constant.Should().Be(R0);
    }

    // ── Binary +/- normalization ──────────────────────────────────────────────

    [Fact]
    public void Normalize_BinaryAddition_TwoTerms()
    {
        // A + B → {A:1, B:1}, Constant=0
        var lf = Normalize("A + B");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(2);
        lf.Terms["A"].Should().Be(R1);
        lf.Terms["B"].Should().Be(R1);
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_BinarySubtraction_SubtractedCoefficient()
    {
        // A - B → {A:1, B:-1}, Constant=0
        var lf = Normalize("A - B");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(2);
        lf.Terms["A"].Should().Be(R1);
        lf.Terms["B"].Should().Be(RNeg1);
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_ThreeTerms_AllCombined()
    {
        // A + B - C → {A:1, B:1, C:-1}, Constant=0
        var lf = Normalize("A + B - C");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(3);
        lf.Terms["A"].Should().Be(R1);
        lf.Terms["B"].Should().Be(R1);
        lf.Terms["C"].Should().Be(RNeg1);
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_CommutativeExpression_SameLinearForm()
    {
        // A + B - C  ≡  -C + B + A : same Terms (sorted dict) and Constant → equal
        var lf1 = Normalize("A + B - C");
        var lf2 = Normalize("-C + B + A");

        lf1.Should().NotBeNull();
        lf2.Should().NotBeNull();
        lf1!.Should().Be(lf2);
    }

    [Fact]
    public void Normalize_IdentifierPlusConstant_WithOffset()
    {
        // A + 3 → {A:1}, Constant=3
        var lf = Normalize("A + 3");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["A"].Should().Be(R1);
        lf.Constant.Should().Be(R3);
    }

    // ── Scalar multiplication / division ─────────────────────────────────────

    [Fact]
    public void Normalize_ConstantTimesField_ScalesCoefficient()
    {
        // 3 * A → {A: 3}, Constant=0
        var lf = Normalize("3 * A");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["A"].Should().Be(R3);
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_DecimalScalar_RationalCoefficient()
    {
        // 0.5 * A → {A: Rational(1,2)}, Constant=0
        var lf = Normalize("0.5 * A");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["A"].Should().Be(R1Over2);
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_FieldDividedByConstant_RationalCoefficient()
    {
        // A / 2 → {A: Rational(1,2)}, Constant=0
        var lf = Normalize("A / 2");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["A"].Should().Be(R1Over2);
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_FieldTimesNegativeConstant_NegativeCoefficient()
    {
        // A * -3 → {A: -3}, Constant=0  (coefficient sign follows constant)
        var lf = Normalize("A * -3");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["A"].Should().Be(new Rational(-3, 1));
        lf.Constant.Should().Be(R0);
    }

    // ── Parentheses ───────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_Parentheses_DoNotConsumeDepthBudget()
    {
        // 8 layers of parentheses around a single binary op should still normalize.
        // Parens do not decrement the depth budget.
        var lf = Normalize("((((((((A + B))))))))");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(2);
        lf.Terms["A"].Should().Be(R1);
        lf.Terms["B"].Should().Be(R1);
    }

    // ── Zero-coefficient cancellation ─────────────────────────────────────────

    [Fact]
    public void Normalize_SelfSubtraction_ZeroCancellation()
    {
        // A - A → zero coefficient dropped → empty Terms, Constant=0
        var lf = Normalize("A - A");

        lf.Should().NotBeNull();
        lf!.Terms.Should().BeEmpty();
        lf.Constant.Should().Be(R0);
    }

    [Fact]
    public void Normalize_PartialCancellation_RemainingTermKept()
    {
        // A + B - A → only B:1 survives
        var lf = Normalize("A + B - A");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(1);
        lf.Terms["B"].Should().Be(R1);
        lf.Constant.Should().Be(R0);
    }

    // ── Non-normalizable input ────────────────────────────────────────────────

    [Fact]
    public void Normalize_FunctionCall_ReturnsNull()
    {
        // Function calls are opaque to LinearForm
        var lf = Normalize("abs(A)");

        lf.Should().BeNull();
    }

    [Fact]
    public void Normalize_ProductOfTwoIdentifiers_ReturnsNull()
    {
        // A * B — multiplication of two non-constants is non-linear
        var lf = Normalize("A * B");

        lf.Should().BeNull();
    }

    [Fact]
    public void Normalize_VariableDivisor_ReturnsNull()
    {
        // A / B — division by a variable is non-linear
        var lf = Normalize("A / B");

        lf.Should().BeNull();
    }

    // ── Depth-bound termination ───────────────────────────────────────────────

    [Fact]
    public void Normalize_DepthBound_ExceedsLimit_ReturnsNull()
    {
        // 10 terms = 9 binary ops (left-associative).
        // Depth budget = 8; on the 9th binary op the budget is exhausted → null.
        const string deepExpr = "A + B + C + D + E + F + G + H + I + J";

        var lf = Normalize(deepExpr);

        lf.Should().BeNull("depth budget of 8 is exhausted after the 9th binary operation");
    }

    [Fact]
    public void Normalize_DepthBound_Parens_DoNotCountAgainstBudget()
    {
        // Same 8 binary ops as the budget limit, but each operand is doubly-parenthesized.
        // Since parens don't decrement the budget, the result equals the non-parenthesized form.
        const string withParens    = "((A)) + ((B)) + ((C)) + ((D)) + ((E)) + ((F)) + ((G)) + ((H)) + ((I))";
        const string withoutParens = "A + B + C + D + E + F + G + H + I";

        var lfParens    = Normalize(withParens);
        var lfNoParens  = Normalize(withoutParens);

        lfParens.Should().NotBeNull();
        lfNoParens.Should().NotBeNull();
        lfParens!.Should().Be(lfNoParens);
    }

    // ── Algebra methods ───────────────────────────────────────────────────────

    [Fact]
    public void Algebra_Add_MergesTermsAndConstants()
    {
        // {A:1, const:2} + {B:3, const:1} = {A:1, B:3, const:3}
        var a = Normalize("A + 2")!;
        var b = Normalize("3 * B + 1")!;

        var result = a.Add(b);

        result.Terms["A"].Should().Be(R1);
        result.Terms["B"].Should().Be(R3);
        result.Constant.Should().Be(R3);
    }

    [Fact]
    public void Algebra_Subtract_NegatesAndMerges()
    {
        // {A:1, B:1} - {A:1, B:2} = {B:-1}  (A cancels, B becomes -1)
        var a = Normalize("A + B")!;
        var b = Normalize("A + 2 * B")!;

        var result = a.Subtract(b);

        result.Terms.Should().HaveCount(1);
        result.Terms["B"].Should().Be(RNeg1);
        result.Constant.Should().Be(R0);
    }

    [Fact]
    public void Algebra_Negate_FlipsAllCoefficientsAndConstant()
    {
        // Negate({A:1, B:-2, const:3}) = {A:-1, B:2, const:-3}
        var form = Normalize("A - 2 * B + 3")!;

        var result = form.Negate();

        result.Terms["A"].Should().Be(RNeg1);
        result.Terms["B"].Should().Be(R2);
        result.Constant.Should().Be(new Rational(-3, 1));
    }

    [Fact]
    public void Algebra_ScaleByConstant_MultipliesAll()
    {
        // ScaleByConstant({A:1, B:1, const:2}, 3) = {A:3, B:3, const:6}
        var form = Normalize("A + B + 2")!;

        var result = form.ScaleByConstant(R3);

        result.Terms["A"].Should().Be(R3);
        result.Terms["B"].Should().Be(R3);
        result.Constant.Should().Be(new Rational(6, 1));
    }

    [Fact]
    public void Algebra_AddYieldingZeroCoefficient_TermDropped()
    {
        // {A:1} + {A:-1} = {}  — zero-coefficient term is dropped
        var a = Normalize("A")!;
        var b = Normalize("-A")!;

        var result = a.Add(b);

        result.Terms.Should().BeEmpty();
        result.Constant.Should().Be(R0);
    }

    // ── Property tests ────────────────────────────────────────────────────────

    [Fact]
    public void Property_Associativity_AddIsAssociative()
    {
        // (A+B)+C  ==  A+(B+C)
        var a = Normalize("A")!;
        var b = Normalize("B")!;
        var c = Normalize("C")!;

        var leftGrouped  = a.Add(b).Add(c);
        var rightGrouped = a.Add(b.Add(c));

        leftGrouped.Should().Be(rightGrouped);
    }

    [Fact]
    public void Property_Commutativity_AddIsCommutative()
    {
        // A+B  ==  B+A
        var a = Normalize("A")!;
        var b = Normalize("B")!;

        a.Add(b).Should().Be(b.Add(a));
    }

    [Fact]
    public void Property_Distributivity_ScaleOverAdd()
    {
        // 3 * (A + B)  ==  3*A + 3*B
        var a = Normalize("A")!;
        var b = Normalize("B")!;
        var sum = a.Add(b);

        var scaleSum   = sum.ScaleByConstant(R3);
        var sumScaled  = a.ScaleByConstant(R3).Add(b.ScaleByConstant(R3));

        scaleSum.Should().Be(sumScaled);
    }

    [Fact]
    public void Property_NegateNegate_IsIdentity()
    {
        var form = Normalize("A + 2 * B - 1")!;

        form.Negate().Negate().Should().Be(form);
    }

    [Fact]
    public void Property_SubtractSelf_IsZero()
    {
        var form = Normalize("A + B - C + 5")!;
        var result = form.Subtract(form);

        result.Terms.Should().BeEmpty();
        result.Constant.Should().Be(R0);
    }

    // ── Constant-only forms ───────────────────────────────────────────────────

    [Fact]
    public void ConstantOnly_LiteralSum_NoTerms()
    {
        // 10 + 5 - 3 → empty Terms, Constant=12
        var lf = Normalize("10 + 5 - 3");

        lf.Should().NotBeNull();
        lf!.Terms.Should().BeEmpty();
        lf.Constant.Should().Be(new Rational(12, 1));
    }

    [Fact]
    public void ConstantOnly_LiteralDifference_CorrectConstant()
    {
        // 100 - 37 → Constant=63
        var lf = Normalize("100 - 37");

        lf.Should().NotBeNull();
        lf!.Terms.Should().BeEmpty();
        lf.Constant.Should().Be(new Rational(63, 1));
    }

    // ── Stress / large-coefficient tests ─────────────────────────────────────

    [Fact]
    public void Stress_LargeCoefficients_NormalizeWithoutOverflow()
    {
        // 100000 * A - 100000 * B: coefficients 100000 and -100000 are within long range.
        // Verifies no overflow or exception during normalization.
        var lf = Normalize("100000 * A - 100000 * B");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(2);
        lf.Terms["A"].Should().Be(new Rational(100000, 1));
        lf.Terms["B"].Should().Be(new Rational(-100000, 1));
    }

    [Fact]
    public void Stress_MaxValueGcd_ScaleByConstantWithinRange()
    {
        // ScaleByConstant with a large-but-valid coefficient should not throw.
        // long.MaxValue / 2 fits in long; GCD computation on this scale must not overflow.
        var form = Normalize("A")!;
        var largeCoeff = new Rational(long.MaxValue / 4, 1);

        var act = () => _ = form.ScaleByConstant(largeCoeff);

        act.Should().NotThrow();
        var result = form.ScaleByConstant(largeCoeff);
        result.Terms["A"].Should().Be(largeCoeff);
    }

    // ── Equality / dictionary key semantics ──────────────────────────────────

    [Fact]
    public void Equality_SameFormDifferentOrder_AreEqual()
    {
        // LinearForm equality is order-independent (ImmutableSortedDictionary).
        // B + A  and  A + B should produce the same LinearForm.
        var lf1 = Normalize("B + A");
        var lf2 = Normalize("A + B");

        lf1.Should().NotBeNull();
        lf2.Should().NotBeNull();
        lf1!.Should().Be(lf2);
        lf1.GetHashCode().Should().Be(lf2!.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentForms_AreNotEqual()
    {
        var lf1 = Normalize("A + B");   // {A:1, B:1}
        var lf2 = Normalize("A - B");   // {A:1, B:-1}

        lf1.Should().NotBeNull();
        lf2.Should().NotBeNull();
        lf1!.Should().NotBe(lf2);
    }

    // ── Mixed normalization ───────────────────────────────────────────────────

    [Fact]
    public void Normalize_MixedSignsWithConstant_CorrectForm()
    {
        // 2 * A - B + 3 → {A:2, B:-1}, Constant=3
        var lf = Normalize("2 * A - B + 3");

        lf.Should().NotBeNull();
        lf!.Terms.Should().HaveCount(2);
        lf.Terms["A"].Should().Be(R2);
        lf.Terms["B"].Should().Be(RNeg1);
        lf.Constant.Should().Be(R3);
    }
}
