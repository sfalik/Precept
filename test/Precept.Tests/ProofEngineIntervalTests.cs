using System.Reflection;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

// ════════════════════════════════════════════════════════════════════════════════
//  Slice 1 — NumericInterval struct unit tests
//
//  Design reference: docs/Working/interval-proof-engine-design.md
//    §2.1 Interval Semantics
//    §8.2 Slice 1 — Catalog Foundation + NumericInterval struct
//    §9.1 Section A — NumericInterval struct unit tests (~50 tests)
//
//  ⚠️  These tests reference Precept.Pipeline.NumericInterval which is created
//  by George in Slice 1 (src/Precept/Pipeline/ProofEngine.Intervals.cs).
//  This file is intentionally RED until that implementation ships.
//  Completion gate: dotnet build clean + all tests pass → Slice 1 done.
// ════════════════════════════════════════════════════════════════════════════════

public class ProofEngineIntervalTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Struct construction and special values
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Constructor_SetsMinAndMax()
    {
        var interval = new NumericInterval(10m, 50m);

        interval.Min.Should().Be(10m);
        interval.Max.Should().Be(50m);
    }

    [Fact]
    public void NumericInterval_Unbounded_SpansDecimalMinMax()
    {
        // § 2.1 Unbounded = [decimal.MinValue, decimal.MaxValue]
        NumericInterval.Unbounded.Min.Should().Be(decimal.MinValue);
        NumericInterval.Unbounded.Max.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void NumericInterval_Unbounded_IsUnbounded_ReturnsTrue()
    {
        NumericInterval.Unbounded.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_BoundedInterval_IsUnbounded_ReturnsFalse()
    {
        var interval = new NumericInterval(0m, 100m);

        interval.IsUnbounded.Should().BeFalse();
    }

    [Fact]
    public void NumericInterval_IsEmpty_TrueWhenMaxLessThanMin()
    {
        // § 2.1 Empty = [max, min] where max < min
        var empty = new NumericInterval(10m, 5m);

        empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_IsEmpty_FalseForNormalInterval()
    {
        var interval = new NumericInterval(0m, 100m);

        interval.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void NumericInterval_IsEmpty_FalseForPointInterval()
    {
        var point = new NumericInterval(42m, 42m);

        point.IsEmpty.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Point interval (literal values — §2.2 TypedLiteral → [value, value])
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_PointInterval_MinEqualsMax()
    {
        var point = new NumericInterval(42m, 42m);

        point.Min.Should().Be(42m);
        point.Max.Should().Be(42m);
    }

    [Fact]
    public void NumericInterval_PointInterval_ContainsItself()
    {
        var point = new NumericInterval(42m, 42m);

        point.Contains(point).Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_PointInterval_IsNotUnbounded()
    {
        var point = new NumericInterval(42m, 42m);

        point.IsUnbounded.Should().BeFalse();
    }

    [Fact]
    public void NumericInterval_PointInterval_IsNotEmpty()
    {
        var point = new NumericInterval(42m, 42m);

        point.IsEmpty.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Add — § 2.1: [a,b] + [c,d] = [a+c, b+d]
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Add_PositivePositive_ReturnsSum()
    {
        // [1, 5] + [2, 3] = [3, 8]
        var a = new NumericInterval(1m, 5m);
        var b = new NumericInterval(2m, 3m);

        var result = a.Add(b);

        result.Min.Should().Be(3m);
        result.Max.Should().Be(8m);
    }

    [Fact]
    public void NumericInterval_Add_PositiveNegative_ReturnsCorrectBounds()
    {
        // [1, 5] + [-3, -1] = [-2, 4]
        var a = new NumericInterval(1m, 5m);
        var b = new NumericInterval(-3m, -1m);

        var result = a.Add(b);

        result.Min.Should().Be(-2m);
        result.Max.Should().Be(4m);
    }

    [Fact]
    public void NumericInterval_Add_NegativeNegative_ReturnsNegativeSum()
    {
        // [-5, -1] + [-3, -2] = [-8, -3]
        var a = new NumericInterval(-5m, -1m);
        var b = new NumericInterval(-3m, -2m);

        var result = a.Add(b);

        result.Min.Should().Be(-8m);
        result.Max.Should().Be(-3m);
    }

    [Fact]
    public void NumericInterval_Add_WithZeroInterval_PreservesOperand()
    {
        // [3, 7] + [0, 0] = [3, 7]
        var a = new NumericInterval(3m, 7m);
        var zero = new NumericInterval(0m, 0m);

        var result = a.Add(zero);

        result.Min.Should().Be(3m);
        result.Max.Should().Be(7m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Subtract — § 2.1: [a,b] - [c,d] = [a-d, b-c]  (note swap)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Subtract_SwapsMaxMin()
    {
        // [10, 20] - [3, 5] = [10-5, 20-3] = [5, 17]
        var a = new NumericInterval(10m, 20m);
        var b = new NumericInterval(3m, 5m);

        var result = a.Subtract(b);

        result.Min.Should().Be(5m);
        result.Max.Should().Be(17m);
    }

    [Fact]
    public void NumericInterval_Subtract_PositiveFromPositive_CanGoNegative()
    {
        // [0, 100] - [0, 200] = [0-200, 100-0] = [-200, 100]
        var balance = new NumericInterval(0m, 100m);
        var amount = new NumericInterval(0m, 200m);

        var result = balance.Subtract(amount);

        result.Min.Should().Be(-200m);
        result.Max.Should().Be(100m);
    }

    [Fact]
    public void NumericInterval_Subtract_SameInterval_ContainsZero()
    {
        // [a, b] - [a, b] = [a-b, b-a]
        // [10, 20] - [10, 20] = [-10, 10]
        var a = new NumericInterval(10m, 20m);

        var result = a.Subtract(a);

        result.Min.Should().Be(-10m);
        result.Max.Should().Be(10m);
    }

    [Fact]
    public void NumericInterval_Subtract_NegativeFromNegative_ReturnsCorrectBounds()
    {
        // [-5, -1] - [-3, -2] = [-5-(-2), -1-(-3)] = [-3, 2]
        var a = new NumericInterval(-5m, -1m);
        var b = new NumericInterval(-3m, -2m);

        var result = a.Subtract(b);

        result.Min.Should().Be(-3m);
        result.Max.Should().Be(2m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Multiply — § 2.1: 4-corner case
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Multiply_BothPositive_ReturnsProductBounds()
    {
        // [1, 3] × [2, 4] = [2, 12]
        var a = new NumericInterval(1m, 3m);
        var b = new NumericInterval(2m, 4m);

        var result = a.Multiply(b);

        result.Min.Should().Be(2m);
        result.Max.Should().Be(12m);
    }

    [Fact]
    public void NumericInterval_Multiply_PositiveTimesNegative_ReturnsSwappedBounds()
    {
        // [1, 3] × [-4, -2] → corners: -4,-2,-12,-6 → [-12, -2]
        var a = new NumericInterval(1m, 3m);
        var b = new NumericInterval(-4m, -2m);

        var result = a.Multiply(b);

        result.Min.Should().Be(-12m);
        result.Max.Should().Be(-2m);
    }

    [Fact]
    public void NumericInterval_Multiply_BothNegative_ReturnsPositiveBounds()
    {
        // [-3, -1] × [-4, -2] → corners: {(-3)×(-4)=12, (-3)×(-2)=6, (-1)×(-4)=4, (-1)×(-2)=2}
        var a = new NumericInterval(-3m, -1m);
        var b = new NumericInterval(-4m, -2m);

        var result = a.Multiply(b);

        result.Min.Should().Be(2m);
        result.Max.Should().Be(12m);
    }

    [Fact]
    public void NumericInterval_Multiply_MixedSign_UsesAllFourCorners()
    {
        // [-1, 2] × [3, 4] → corners: -3,-4,6,8 → [-4, 8]
        var a = new NumericInterval(-1m, 2m);
        var b = new NumericInterval(3m, 4m);

        var result = a.Multiply(b);

        result.Min.Should().Be(-4m);
        result.Max.Should().Be(8m);
    }

    [Fact]
    public void NumericInterval_Multiply_ByZeroPointInterval_ReturnsZero()
    {
        // [5, 10] × [0, 0] = [0, 0]
        var a = new NumericInterval(5m, 10m);
        var zero = new NumericInterval(0m, 0m);

        var result = a.Multiply(zero);

        result.Min.Should().Be(0m);
        result.Max.Should().Be(0m);
    }

    [Fact]
    public void NumericInterval_Multiply_TwoMixedSignIntervals_UsesAllFourCorners()
    {
        // [-2, 3] × [-4, 5] → corners: 8,-10,-12,15 → [-12, 15]
        var a = new NumericInterval(-2m, 3m);
        var b = new NumericInterval(-4m, 5m);

        var result = a.Multiply(b);

        result.Min.Should().Be(-12m);
        result.Max.Should().Be(15m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Divide — § 2.1: unbounded if divisor contains 0; else 4-corner
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Divide_AllPositive_ReturnsCorrectBounds()
    {
        // [4, 8] ÷ [2, 4] → corners: 2,4,1,2 → [1, 4]
        var a = new NumericInterval(4m, 8m);
        var b = new NumericInterval(2m, 4m);

        var result = a.Divide(b);

        result.Min.Should().Be(1m);
        result.Max.Should().Be(4m);
    }

    [Fact]
    public void NumericInterval_Divide_DivisorContainsZero_ReturnsUnbounded()
    {
        // § 2.1 division by interval containing zero → unbounded
        var a = new NumericInterval(1m, 10m);
        var divisorWithZero = new NumericInterval(-1m, 1m);

        var result = a.Divide(divisorWithZero);

        result.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Divide_DivisorIsExactlyZero_ReturnsUnbounded()
    {
        // [1, 10] ÷ [0, 0] → divisor IS zero → unbounded
        var a = new NumericInterval(1m, 10m);
        var zero = new NumericInterval(0m, 0m);

        var result = a.Divide(zero);

        result.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Divide_DivisorStraddlesZero_ReturnsUnbounded()
    {
        // [5, 10] ÷ [-2, 3] → divisor straddles zero → unbounded
        var a = new NumericInterval(5m, 10m);
        var straddlesZero = new NumericInterval(-2m, 3m);

        var result = a.Divide(straddlesZero);

        result.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Divide_AllNegativeDivisor_ReturnsCorrectBounds()
    {
        // [4, 8] ÷ [-4, -2] → corners: -2,-1,-4,-2 → [-4, -1]
        var a = new NumericInterval(4m, 8m);
        var b = new NumericInterval(-4m, -2m);

        var result = a.Divide(b);

        result.Min.Should().Be(-4m);
        result.Max.Should().Be(-1m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Negate — § 5.6: negate([a,b]) = [-b, -a]
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Negate_PositiveBounds_ReturnsNegatedAndSwapped()
    {
        // negate([3, 7]) = [-7, -3]
        var a = new NumericInterval(3m, 7m);

        var result = a.Negate();

        result.Min.Should().Be(-7m);
        result.Max.Should().Be(-3m);
    }

    [Fact]
    public void NumericInterval_Negate_NegativeBounds_ReturnsPositive()
    {
        // negate([-5, -1]) = [1, 5]
        var a = new NumericInterval(-5m, -1m);

        var result = a.Negate();

        result.Min.Should().Be(1m);
        result.Max.Should().Be(5m);
    }

    [Fact]
    public void NumericInterval_Negate_MixedSign_SwapsBounds()
    {
        // negate([-2, 4]) = [-4, 2]
        var a = new NumericInterval(-2m, 4m);

        var result = a.Negate();

        result.Min.Should().Be(-4m);
        result.Max.Should().Be(2m);
    }

    [Fact]
    public void NumericInterval_Negate_PointInterval_ReturnsNegatedPoint()
    {
        // negate([5, 5]) = [-5, -5]
        var a = new NumericInterval(5m, 5m);

        var result = a.Negate();

        result.Min.Should().Be(-5m);
        result.Max.Should().Be(-5m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Contains — bounds.Contains(result): is result fully within bounds?
    //  § 2.3: R ⊆ B iff R.Min >= B.Min && R.Max <= B.Max
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Contains_ProperSubset_ReturnsTrue()
    {
        // [0, 100].Contains([10, 50]) → true
        var bounds = new NumericInterval(0m, 100m);
        var result = new NumericInterval(10m, 50m);

        bounds.Contains(result).Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Contains_EqualInterval_ReturnsTrue()
    {
        // [0, 100].Contains([0, 100]) → true (boundary inclusivity)
        var bounds = new NumericInterval(0m, 100m);

        bounds.Contains(bounds).Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Contains_ExceedsMax_ReturnsFalse()
    {
        // [0, 100].Contains([10, 150]) → false (150 > 100)
        var bounds = new NumericInterval(0m, 100m);
        var result = new NumericInterval(10m, 150m);

        bounds.Contains(result).Should().BeFalse();
    }

    [Fact]
    public void NumericInterval_Contains_BelowMin_ReturnsFalse()
    {
        // [0, 100].Contains([-10, 50]) → false (-10 < 0)
        var bounds = new NumericInterval(0m, 100m);
        var result = new NumericInterval(-10m, 50m);

        bounds.Contains(result).Should().BeFalse();
    }

    [Fact]
    public void NumericInterval_Contains_ResultAtExactBoundary_ReturnsTrue()
    {
        // [0, 100].Contains([0, 100]) — boundary values are inclusive
        var bounds = new NumericInterval(0m, 100m);
        var exactEdge = new NumericInterval(0m, 100m);

        bounds.Contains(exactEdge).Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Empty_ContainsAnything_ReturnsTrue()
    {
        // § 2.1: "Empty intervals trivially satisfy containment"
        // bounds.Contains(emptyResult) = true — no value in empty interval can violate bounds
        var bounds = new NumericInterval(0m, 100m);
        var emptyResult = new NumericInterval(50m, 10m); // max < min → empty

        bounds.Contains(emptyResult).Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Union — for TypedConditional then/else branches (§ 2.2)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Union_TwoDisjointIntervals_SpansBoth()
    {
        // [1, 3] ∪ [7, 9] = [1, 9]
        var a = new NumericInterval(1m, 3m);
        var b = new NumericInterval(7m, 9m);

        var result = a.Union(b);

        result.Min.Should().Be(1m);
        result.Max.Should().Be(9m);
    }

    [Fact]
    public void NumericInterval_Union_OverlappingIntervals_SpansBoth()
    {
        // [1, 5] ∪ [3, 9] = [1, 9]
        var a = new NumericInterval(1m, 5m);
        var b = new NumericInterval(3m, 9m);

        var result = a.Union(b);

        result.Min.Should().Be(1m);
        result.Max.Should().Be(9m);
    }

    [Fact]
    public void NumericInterval_Union_WithSelf_ReturnsSelf()
    {
        // [3, 7] ∪ [3, 7] = [3, 7]
        var a = new NumericInterval(3m, 7m);

        var result = a.Union(a);

        result.Min.Should().Be(3m);
        result.Max.Should().Be(7m);
    }

    [Fact]
    public void NumericInterval_Union_WithUnbounded_ReturnsUnbounded()
    {
        // [3, 7] ∪ Unbounded = Unbounded
        var bounded = new NumericInterval(3m, 7m);

        var result = bounded.Union(NumericInterval.Unbounded);

        result.IsUnbounded.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Unbounded propagation — § 2.2 "Critical rule: unbounded propagates"
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Add()
    {
        // Unbounded + bounded → Unbounded
        var bounded = new NumericInterval(0m, 100m);

        var result = NumericInterval.Unbounded.Add(bounded);

        result.IsUnbounded.Should().BeTrue(
            "any operation on an unbounded operand produces unbounded result — §2.2");
    }

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Add_RightOperand()
    {
        // bounded + Unbounded → Unbounded
        var bounded = new NumericInterval(0m, 100m);

        var result = bounded.Add(NumericInterval.Unbounded);

        result.IsUnbounded.Should().BeTrue(
            "unbounded propagates regardless of operand position — §2.2");
    }

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Subtract()
    {
        var bounded = new NumericInterval(0m, 100m);

        var result = NumericInterval.Unbounded.Subtract(bounded);

        result.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Multiply()
    {
        var bounded = new NumericInterval(0m, 100m);

        var result = NumericInterval.Unbounded.Multiply(bounded);

        result.IsUnbounded.Should().BeTrue(
            "any operation on an unbounded operand produces unbounded result — §2.2");
    }

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Multiply_RightOperand()
    {
        var bounded = new NumericInterval(0m, 100m);

        var result = bounded.Multiply(NumericInterval.Unbounded);

        result.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Divide_Dividend()
    {
        var bounded = new NumericInterval(1m, 10m);

        var result = NumericInterval.Unbounded.Divide(bounded);

        result.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Divide_Divisor()
    {
        // bounded ÷ Unbounded → Unbounded (note: divisor may contain zero)
        var bounded = new NumericInterval(1m, 10m);

        var result = bounded.Divide(NumericInterval.Unbounded);

        result.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void NumericInterval_Unbounded_PropagatesThrough_Negate()
    {
        // § 5.6 negate(Unbounded) must stay unbounded
        var result = NumericInterval.Unbounded.Negate();

        result.IsUnbounded.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 37 — affine interval shift coverage
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Shift_PositiveOffset_BothBoundsShifted()
    {
        var shifted = InvokeShift(new NumericInterval(10m, 20m), 273.15m);

        shifted.Min.Should().Be(283.15m);
        shifted.Max.Should().Be(293.15m);
    }

    [Fact]
    public void Shift_NegativeValues_Shifted()
    {
        var shifted = InvokeShift(new NumericInterval(-40m, 100m), 273.15m);

        shifted.Min.Should().Be(233.15m);
        shifted.Max.Should().Be(373.15m);
    }

    [Fact]
    public void Shift_Unbounded_ReturnsUnbounded()
    {
        var shifted = InvokeShift(NumericInterval.Unbounded, 273.15m);

        shifted.IsUnbounded.Should().BeTrue();
    }

    [Fact]
    public void Shift_ZeroOffset_IdentityBehavior()
    {
        var shifted = InvokeShift(new NumericInterval(10m, 20m), 0m);

        shifted.Min.Should().Be(10m);
        shifted.Max.Should().Be(20m);
    }

    private static NumericInterval InvokeShift(NumericInterval interval, decimal offset)
    {
        var method = typeof(NumericInterval).GetMethod(
            name: "Shift",
            bindingAttr: BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(decimal)],
            modifiers: null);

        method.Should().NotBeNull("Slice 37 adds NumericInterval.Shift(decimal) for affine normalization");

        var result = method!.Invoke(interval, [offset]);
        result.Should().BeOfType<NumericInterval>();
        return (NumericInterval)result!;
    }
}
