using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Unit tests for <see cref="NumericInterval"/> arithmetic transfer rules and predicates.
/// Part of the non-SMT proof engine (Slice 12).
/// </summary>
public class NumericIntervalTests
{
    // ── Addition ─────────────────────────────────────────────────────────────

    [Fact]
    public void Add_BothPositive_ReturnsPositive()
    {
        var result = NumericInterval.Add(NumericInterval.Positive, NumericInterval.Positive);
        result.ExcludesZero.Should().BeTrue();
        result.IsNonnegative.Should().BeTrue();
        result.Lower.Should().Be(0);
        result.LowerInclusive.Should().BeFalse();
    }

    // ── Subtraction ──────────────────────────────────────────────────────────

    [Fact]
    public void Subtract_SameInterval_ContainsZero()
    {
        var x = new NumericInterval(1, true, 10, true);
        var result = NumericInterval.Subtract(x, x);
        // [1,10] - [1,10] = [-9, 9] — includes zero
        result.ExcludesZero.Should().BeFalse();
        result.Lower.Should().Be(-9);
        result.Upper.Should().Be(9);
    }

    // ── Multiplication ───────────────────────────────────────────────────────

    [Fact]
    public void Multiply_BothPositive_ReturnsPositive()
    {
        var result = NumericInterval.Multiply(NumericInterval.Positive, NumericInterval.Positive);
        result.ExcludesZero.Should().BeTrue();
        result.Lower.Should().Be(0);
        result.LowerInclusive.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 5, true, false,  0, 5, true, false,  false)]  // [0,5) × [0,5) = [0, 25) — lower = 0 inclusive, includes 0
    [InlineData(-5, 0, true, false,  -5, 0, true, false,  true)]  // [-5,0) × [-5,0) — both strictly neg, product is (0,25] — excludes zero
    [InlineData(-3, -1, true, true,  2, 5, true, true,  true)]    // neg × pos = neg result [-15,-2], entirely negative → excludes zero
    [InlineData(1, 3, true, true,  -5, -1, true, true,  true)]    // pos × neg = neg result [-15,-1], entirely negative → excludes zero
    [InlineData(0, 5, true, true,  -2, 3, true, true,  false)]    // nonneg × mixed: can be 0 (when left = 0)
    [InlineData(-2, 3, true, true,  0, 5, true, true,  false)]    // mixed × nonneg: can be 0 (when right = 0)
    public void Multiply_MixedSigns_FourCorner(
        double al, double au, bool ali, bool aui,
        double bl, double bu, bool bli, bool bui,
        bool expectedExcludesZero)
    {
        var a = new NumericInterval(al, ali, au, aui);
        var b = new NumericInterval(bl, bli, bu, bui);
        var result = NumericInterval.Multiply(a, b);
        result.ExcludesZero.Should().Be(expectedExcludesZero);
    }

    // ── Division ─────────────────────────────────────────────────────────────

    [Fact]
    public void Divide_DivisorExcludesZero()
    {
        var num = new NumericInterval(10, true, 100, true);
        var den = new NumericInterval(2, true, 5, true);
        var result = NumericInterval.Divide(num, den);
        result.ExcludesZero.Should().BeTrue();
        result.Lower.Should().BeApproximately(2.0, 1e-10);
        result.Upper.Should().BeApproximately(50.0, 1e-10);
    }

    [Fact]
    public void Divide_DivisorContainsZero_ReturnsUnknown()
    {
        var num = new NumericInterval(1, true, 10, true);
        var den = new NumericInterval(-1, true, 1, true);
        var result = NumericInterval.Divide(num, den);
        result.IsUnknown.Should().BeTrue();
    }

    // ── Abs ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Abs_Nonneg_Identity()
    {
        var result = NumericInterval.Abs(NumericInterval.Nonneg);
        result.IsNonnegative.Should().BeTrue();
        result.Lower.Should().Be(0);
        result.LowerInclusive.Should().BeTrue();
    }

    [Fact]
    public void Abs_Mixed_ReturnsNonneg()
    {
        var mixed = new NumericInterval(-5, true, 3, true);
        var result = NumericInterval.Abs(mixed);
        result.Lower.Should().Be(0);
        result.LowerInclusive.Should().BeTrue();
        result.Upper.Should().Be(5);
        result.IsNonnegative.Should().BeTrue();
    }

    // ── Min / Max ─────────────────────────────────────────────────────────────

    [Fact]
    public void Min_BothPositive_Positive()
    {
        var result = NumericInterval.Min(NumericInterval.Positive, NumericInterval.Positive);
        result.IsNonnegative.Should().BeTrue();
        result.ExcludesZero.Should().BeTrue();
    }

    [Fact]
    public void Max_EitherPositive_Positive()
    {
        var result = NumericInterval.Max(NumericInterval.Unknown, NumericInterval.Positive);
        result.ExcludesZero.Should().BeTrue();
        result.IsNonnegative.Should().BeTrue();
    }

    // ── Clamp ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clamp_WithBounds()
    {
        var x = NumericInterval.Unknown;
        var lo = new NumericInterval(5, true, 5, true);
        var hi = new NumericInterval(100, true, 100, true);
        var result = NumericInterval.Clamp(x, lo, hi);
        result.Lower.Should().Be(5);
        result.Upper.Should().Be(100);
        result.ExcludesZero.Should().BeTrue();
    }

    // ── Hull ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Hull_PositiveAndZero_Nonneg()
    {
        var result = NumericInterval.Hull(NumericInterval.Positive, NumericInterval.Zero);
        result.IsNonnegative.Should().BeTrue();
        result.ExcludesZero.Should().BeFalse(); // hull includes [0,0]
        result.Lower.Should().Be(0);
        result.LowerInclusive.Should().BeTrue(); // Zero has LowerInclusive=true, Hull uses || for equal bounds
    }

    // ── ExcludesZero predicates ───────────────────────────────────────────────

    [Fact]
    public void ExcludesZero_Positive_True()
    {
        NumericInterval.Positive.ExcludesZero.Should().BeTrue();
    }

    [Fact]
    public void ExcludesZero_Nonneg_False()
    {
        NumericInterval.Nonneg.ExcludesZero.Should().BeFalse();
    }

    [Fact]
    public void ExcludesZero_StrictlyNegative_True()
    {
        var neg = new NumericInterval(double.NegativeInfinity, false, 0, false);
        neg.ExcludesZero.Should().BeTrue();
    }

    [Fact]
    public void ExcludesZero_OpenUpperAtZero_True()
    {
        var neg = new NumericInterval(double.NegativeInfinity, false, 0, false);
        neg.ExcludesZero.Should().BeTrue();
    }

    [Fact]
    public void ExcludesZero_BoundedPositive_True()
    {
        var bounded = new NumericInterval(5, true, 100, true);
        bounded.ExcludesZero.Should().BeTrue();
    }

}
