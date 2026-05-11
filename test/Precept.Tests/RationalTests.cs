using System;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Unit tests for <see cref="Rational"/> — exact long/long arithmetic used as
/// <see cref="LinearForm"/> coefficients in the unified proof engine.
/// Covers GCD normalization, sign canonicalization, INumber&lt;T&gt; arithmetic,
/// comparison operators, decimal literal round-trip, and overflow safety.
/// </summary>
/// <remarks>
/// These tests are written against the spec in <c>docs/ProofEngineDesign.md § Rational Type</c>
/// and <c>temp/unified-proof-plan.md § Implementation Manifest</c>.
/// George's <c>Rational.cs</c> is implemented in tandem; tests compile and pass once it lands.
/// </remarks>
public class RationalTests
{
    // ── Construction & GCD normalization ─────────────────────────────────────

    [Fact]
    public void Construction_GcdNormalization_SixOverFour_ThreeOverTwo()
    {
        var r = new Rational(6, 4);

        r.Numerator.Should().Be(3);
        r.Denominator.Should().Be(2);
    }

    [Fact]
    public void Construction_ZeroDenominator_ThrowsDivideByZeroException()
    {
        var act = () => _ = new Rational(1, 0);

        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void Construction_ZeroNumerator_NormalizesToZeroOverOne()
    {
        var r = new Rational(0, 7);

        r.Numerator.Should().Be(0);
        r.Denominator.Should().Be(1);
    }

    [Fact]
    public void Construction_NegativeNumerator_CanonicalSign()
    {
        // GCD(4,8)=4; -4/4=-1, 8/4=2
        var r = new Rational(-4, 8);

        r.Numerator.Should().Be(-1);
        r.Denominator.Should().Be(2);
    }

    [Fact]
    public void Construction_NegativeDenominator_DenominatorAlwaysPositive()
    {
        // Denominator must be positive; negate both: N=-4, D=8; GCD(4,8)=4 → N=-1, D=2
        var r = new Rational(4, -8);

        r.Numerator.Should().Be(-1);
        r.Denominator.Should().Be(2);
    }

    [Fact]
    public void Construction_BothNegative_NormalizesPositive()
    {
        // Both negative → negate both; N=4, D=8; GCD=4 → N=1, D=2
        var r = new Rational(-4, -8);

        r.Numerator.Should().Be(1);
        r.Denominator.Should().Be(2);
    }

    [Theory]
    [InlineData(12, 8, 3, 2)]
    [InlineData(100, 25, 4, 1)]
    [InlineData(7, 7, 1, 1)]
    [InlineData(-9, 6, -3, 2)]
    [InlineData(1, 1, 1, 1)]
    public void GcdNormalization_Theory_CanonicalForm(long n, long d, long expectedN, long expectedD)
    {
        var r = new Rational(n, d);

        r.Numerator.Should().Be(expectedN);
        r.Denominator.Should().Be(expectedD);
    }

    // ── Equality & hash code ──────────────────────────────────────────────────

    [Fact]
    public void Equality_SameValue_DifferentForm_AreEqual()
    {
        var a = new Rational(3, 6);   // normalizes to 1/2
        var b = new Rational(1, 2);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_EqualRationals_HaveEqualHashCodes()
    {
        var a = new Rational(6, 4);   // normalizes to 3/2
        var b = new Rational(3, 2);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // ── Arithmetic operators ──────────────────────────────────────────────────

    [Fact]
    public void Addition_ProducesCorrectResult()
    {
        var result = new Rational(1, 3) + new Rational(1, 6);

        result.Should().Be(new Rational(1, 2));
    }

    [Fact]
    public void Subtraction_ProducesCorrectResult()
    {
        var result = new Rational(2, 3) - new Rational(1, 6);

        result.Should().Be(new Rational(1, 2));
    }

    [Fact]
    public void Multiplication_ProducesCorrectResult()
    {
        var result = new Rational(2, 3) * new Rational(3, 4);

        result.Should().Be(new Rational(1, 2));
    }

    [Fact]
    public void Division_ProducesCorrectResult()
    {
        // (2/3) / (4/3) = (2/3) * (3/4) = 1/2
        var result = new Rational(2, 3) / new Rational(4, 3);

        result.Should().Be(new Rational(1, 2));
    }

    [Fact]
    public void DivisionByRationalZero_ThrowsDivideByZeroException()
    {
        var act = () => _ = new Rational(1, 1) / new Rational(0, 1);

        act.Should().Throw<DivideByZeroException>();
    }

    // ── Decimal literal ───────────────────────────────────────────────────────

    [Fact]
    public void DecimalLiteral_PointOne_Parses_ToOneOverTen()
    {
        var r = Rational.FromDecimalLiteral("0.1");

        r.Should().Be(new Rational(1, 10));
    }

    [Fact]
    public void DecimalLiteralRoundTrip_PointOne_AsDouble()
    {
        // No explicit (double) cast operator; compute via Numerator/Denominator.
        var r = Rational.FromDecimalLiteral("0.1");
        var asDouble = (double)r.Numerator / r.Denominator;

        asDouble.Should().Be(0.1d);
    }

    // ── Comparison operators ──────────────────────────────────────────────────

    [Fact]
    public void Comparison_LessThanGreaterThan()
    {
        var half = new Rational(1, 2);
        var twoThirds = new Rational(2, 3);

        (half < twoThirds).Should().BeTrue();
        (twoThirds > half).Should().BeTrue();
        (half > twoThirds).Should().BeFalse();
    }

    [Fact]
    public void Comparison_AllOrderingOperators()
    {
        var a = new Rational(1, 3);
        var b = new Rational(1, 3);   // equal
        var c = new Rational(1, 2);   // greater

        (a <= b).Should().BeTrue();
        (a >= b).Should().BeTrue();
        (a < c).Should().BeTrue();
        (c > a).Should().BeTrue();
        (a <= c).Should().BeTrue();
        (c >= a).Should().BeTrue();
    }

    // ── Overflow / checked arithmetic ─────────────────────────────────────────

    [Fact]
    public void LongMinValue_Negation_Overflow_Throws()
    {
        // Rational(long.MinValue, -1): normalizing denominator to positive requires
        // negating both parts; -long.MinValue overflows checked long arithmetic.
        var act = () => _ = new Rational(long.MinValue, -1);

        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void MultiplicationOverflow_Checked_Throws()
    {
        // Cross-GCD pre-reduction: gcd(a.N, b.D)=gcd(large, 1)=1, gcd(b.N, a.D)=gcd(2,1)=1.
        // No reduction possible. checked(large * 2) overflows.
        var a = new Rational(long.MaxValue / 2 + 1, 1);
        var b = new Rational(2, 1);

        var act = () => _ = a * b;

        act.Should().Throw<OverflowException>();
    }

    // ── Modulo, increment/decrement, ToString ──────────────────────────────────

    [Fact]
    public void Modulo_Operator_ProducesCorrectRemainder()
    {
        // (7/3) % (2/3) = 7/3 - 2/3 * truncate((7/3)/(2/3)) = 7/3 - 2/3*3 = 7/3 - 2 = 1/3
        var result = new Rational(7, 3) % new Rational(2, 3);

        result.Should().Be(new Rational(1, 3));
    }

    [Fact]
    public void IncrementDecrement_Operators_WorkCorrectly()
    {
        var r = new Rational(1, 2);
        r++;

        r.Should().Be(new Rational(3, 2));   // 1/2 + 1 = 3/2

        r--;

        r.Should().Be(new Rational(1, 2));   // 3/2 - 1 = 1/2
    }

    [Fact]
    public void ToString_Format_NumeratorOverDenominator()
    {
        new Rational(3, 4).ToString().Should().Be("3/4");
        new Rational(-1, 2).ToString().Should().Be("-1/2");
        new Rational(2, 1).ToString().Should().Be("2");   // denominator=1 → no slash
    }
}
