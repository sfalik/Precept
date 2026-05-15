namespace Precept.Language;

/// <summary>
/// A closed numeric interval [Min, Max] used for compile-time overflow proofs.
/// </summary>
public readonly struct NumericInterval
{
    public decimal Min { get; }
    public decimal Max { get; }
    public bool IsUnbounded { get; }
    public bool IsEmpty => Max < Min;

    public NumericInterval(decimal min, decimal max, bool isUnbounded = false)
    {
        Min = min;
        Max = max;
        IsUnbounded = isUnbounded;
    }

    public static NumericInterval Unbounded { get; } =
        new(decimal.MinValue, decimal.MaxValue, isUnbounded: true);

    public static NumericInterval Point(decimal v) => new(v, v);

    // Sentinel-safe helpers: decimal.MinValue / decimal.MaxValue represent ±∞.
    // We cannot perform normal arithmetic on them without overflowing, so we
    // saturate at the sentinel boundaries instead.
    private static bool IsSentinel(decimal v) => v == decimal.MinValue || v == decimal.MaxValue;

    private static decimal SentinelAdd(decimal a, decimal b)
    {
        if (a == decimal.MinValue || b == decimal.MinValue) return decimal.MinValue;
        if (a == decimal.MaxValue || b == decimal.MaxValue) return decimal.MaxValue;
        return a + b;
    }

    private static decimal SentinelSubtractLower(decimal minL, decimal maxR)
    {
        // lower bound of subtraction: l.Min - r.Max  (goes most negative when r is largest)
        if (minL == decimal.MinValue || maxR == decimal.MaxValue) return decimal.MinValue;
        return minL - maxR;
    }

    private static decimal SentinelSubtractUpper(decimal maxL, decimal minR)
    {
        // upper bound of subtraction: l.Max - r.Min  (goes most positive when r is smallest)
        if (maxL == decimal.MaxValue || minR == decimal.MinValue) return decimal.MaxValue;
        return maxL - minR;
    }

    private static bool HasSentinelBound(NumericInterval v) =>
        IsSentinel(v.Min) || IsSentinel(v.Max);

    private static decimal ScaleBound(decimal bound, decimal factor)
    {
        if (bound == decimal.MinValue)
            return factor < 0m ? decimal.MaxValue : decimal.MinValue;

        if (bound == decimal.MaxValue)
            return factor < 0m ? decimal.MinValue : decimal.MaxValue;

        return bound * factor;
    }

    public NumericInterval Add(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        return new(SentinelAdd(Min, other.Min), SentinelAdd(Max, other.Max));
    }

    public NumericInterval Subtract(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        return new(SentinelSubtractLower(Min, other.Max), SentinelSubtractUpper(Max, other.Min));
    }

    public NumericInterval Multiply(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        // If any bound is a sentinel (±∞), the product may be ±∞ — return Unbounded safely.
        if (HasSentinelBound(this) || HasSentinelBound(other)) return Unbounded;
        var corners = new[] { Min * other.Min, Min * other.Max, Max * other.Min, Max * other.Max };
        return new(corners.Min(), corners.Max());
    }

    public NumericInterval Divide(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        if (other.Min <= 0m && other.Max >= 0m) return Unbounded;
        // If any bound is a sentinel, conservatively return Unbounded.
        if (HasSentinelBound(this) || HasSentinelBound(other)) return Unbounded;
        var corners = new[] { Min / other.Min, Min / other.Max, Max / other.Min, Max / other.Max };
        return new(corners.Min(), corners.Max());
    }

    public NumericInterval Negate()
    {
        if (IsUnbounded) return Unbounded;
        return new(-Max, -Min);
    }

    public NumericInterval Scale(decimal factor)
    {
        if (IsUnbounded) return Unbounded;

        var scaledMin = ScaleBound(Min, factor);
        var scaledMax = ScaleBound(Max, factor);
        return new(Math.Min(scaledMin, scaledMax), Math.Max(scaledMin, scaledMax));
    }

    public NumericInterval Shift(decimal offset)
    {
        if (IsUnbounded) return Unbounded;
        return new(SentinelAdd(Min, offset), SentinelAdd(Max, offset));
    }

    public bool Contains(NumericInterval other)
    {
        if (other.IsEmpty) return true;
        return other.Min >= Min && other.Max <= Max;
    }

    public NumericInterval Union(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        return new(Math.Min(Min, other.Min), Math.Max(Max, other.Max));
    }

    public override string ToString() =>
        IsUnbounded ? "[−∞ .. +∞]" :
        IsEmpty ? "[empty]" :
        $"[{Min} .. {Max}]";
}
