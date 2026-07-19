using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// One test region per adopted normal-form rule (positive and negative cases),
/// plus the documented non-rules — equivalences the normal form deliberately
/// does NOT admit. See NORMAL-FORM-DRAFT.md.
/// </summary>
public class NormalFormTests
{
    // ── N2: comparison-direction flip ────────────────────────────────────────

    [Fact]
    public void Flip_LeEquivalentToGe() => Px.Eq("A <= B", "B >= A").Should().BeTrue();

    [Fact]
    public void Flip_LtEquivalentToGt() => Px.Eq("A < B", "B > A").Should().BeTrue();

    [Fact]
    public void Flip_StrictnessIsPreserved() => Px.Eq("A <= B", "A < B").Should().BeFalse();

    [Fact]
    public void Flip_DirectionMatters() => Px.Eq("A <= B", "B <= A").Should().BeFalse();

    // ── N3: operand commutation for commutative operators ────────────────────

    [Fact]
    public void Commute_Addition() => Px.Eq("A + B <= C", "B + A <= C").Should().BeTrue();

    [Fact]
    public void Commute_Multiplication() => Px.Eq("A * B <= C", "B * A <= C").Should().BeTrue();

    [Fact]
    public void Commute_Equality() => Px.Eq("A == B", "B == A").Should().BeTrue();

    [Fact]
    public void Commute_Inequality() => Px.Eq("A != B", "B != A").Should().BeTrue();

    [Fact]
    public void Commute_And() => Px.Eq("A <= B and B <= C", "B <= C and A <= B").Should().BeTrue();

    [Fact]
    public void Commute_Or() => Px.Eq("A <= B or B <= C", "B <= C or A <= B").Should().BeTrue();

    [Fact]
    public void Commute_SubtractionIsNotCommutative() => Px.Eq("A - B <= C", "B - A <= C").Should().BeFalse();

    [Fact]
    public void Commute_DivisionIsNotCommutative() => Px.Eq("A / B <= C", "B / A <= C").Should().BeFalse();

    // ── N4: associativity flattening ─────────────────────────────────────────

    [Fact]
    public void Assoc_AdditionChains() => Px.Eq("(A + B) + C <= 10", "A + (B + C) <= 10").Should().BeTrue();

    [Fact]
    public void Assoc_AndChains() =>
        Px.Eq("(A <= 1 and B <= 2) and C <= 3", "A <= 1 and (B <= 2 and C <= 3)").Should().BeTrue();

    // ── N5: numeric literals compare by value ────────────────────────────────

    [Fact]
    public void Literal_IntegerAndDecimalSpellings() => Px.Eq("A <= 500", "A <= 500.0").Should().BeTrue();

    [Fact]
    public void Literal_DifferentValuesDiffer() => Px.Eq("A <= 500", "A <= 500.5").Should().BeFalse();

    // ── N6: subtraction is addition of the negation; arithmetic double negation ──

    [Fact]
    public void Neg_SubtractionEqualsAddedNegation() => Px.Eq("A - B <= C", "A + (-B) <= C").Should().BeTrue();

    [Fact]
    public void Neg_DoubleArithmeticNegationCancels() => Px.Eq("-(-A) <= B", "A <= B").Should().BeTrue();

    // ── N7: logical double negation ──────────────────────────────────────────

    [Fact]
    public void Not_DoubleNegationCancels() => Px.Eq("not (not (A <= B))", "A <= B").Should().BeTrue();

    // ── N8: duplicate conjuncts/disjuncts collapse ───────────────────────────

    [Fact]
    public void Idem_DuplicateConjunctCollapses() => Px.Eq("A <= B and A <= B", "A <= B").Should().BeTrue();

    [Fact]
    public void Idem_DuplicateDisjunctCollapses() => Px.Eq("A <= B or A <= B", "A <= B").Should().BeTrue();

    // ── N9: constant folding (FLAGGED open item — see NORMAL-FORM-DRAFT.md) ──

    [Fact]
    public void Fold_LiteralAddition() => Px.Eq("A <= 500 + 500", "A <= 1000").Should().BeTrue();

    [Fact]
    public void Fold_LiteralMultiplication() => Px.Eq("A <= 4 * 250", "A <= 1000").Should().BeTrue();

    [Fact]
    public void Fold_AdditiveZeroDrops() => Px.Eq("A + 0 <= 10", "A <= 10").Should().BeTrue();

    [Fact]
    public void Fold_MultiplicativeOneDrops() => Px.Eq("A * 1 <= 10", "A <= 10").Should().BeTrue();

    [Fact]
    public void Fold_DivisionDoesNotFold() => Px.Eq("A <= 10 / 2", "A <= 5").Should().BeFalse();

    // ── N10: quantifier bindings are alpha-renamed ───────────────────────────

    [Fact]
    public void Alpha_BindingNameDoesNotMatter()
    {
        const string decls = "field S as set of integer";
        WpCalculator.AreNormalFormEqual(
            Px.Rule(decls, "no x in S (x > 100)"),
            Px.Rule(decls, "no y in S (y > 100)")).Should().BeTrue();
    }

    // ── Non-rules: sound implications that are NOT normal-form equalities ────

    [Fact]
    public void NonRule_ImplicationIsNotEquality() =>
        Px.Eq("A <= 500 and B <= 500", "A + B <= 1000").Should().BeFalse();

    [Fact]
    public void NonRule_NoTermMovementAcrossComparison() =>
        Px.Eq("A - B <= 10", "A <= 10 + B").Should().BeFalse();

    [Fact]
    public void NonRule_NoDistribution() =>
        Px.Eq("A * (B + C) <= 10", "A * B + A * C <= 10").Should().BeFalse();

    [Fact]
    public void NonRule_NoDeMorgan() =>
        Px.Eq("not (A <= 10 and B <= 10)", "not (A <= 10) or not (B <= 10)").Should().BeFalse();

    [Fact]
    public void NonRule_NoAlgebraicCancellation() =>
        Px.Eq("A + B - B <= 10", "A <= 10").Should().BeFalse();

    [Fact]
    public void NonRule_NoNegationDistributionOverSums() =>
        Px.Eq("-(A + B) <= 10", "-A - B <= 10").Should().BeFalse();

    [Fact]
    public void NonRule_NotIsNotPushedThroughComparisons() =>
        Px.Eq("not (A <= B)", "B < A").Should().BeFalse();
}
