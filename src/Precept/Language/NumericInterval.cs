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

    /// <summary>
    /// Explicit empty interval. Distinct from <see cref="IsEmpty"/> (which
    /// detects emptiness via <c>Max &lt; Min</c>) — this is the canonical
    /// empty value the satisfiability scan returns for an inhabited-set check
    /// that fails.
    /// </summary>
    public static NumericInterval Empty { get; } = new(0m, -1m);

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

    /// <summary>
    /// Interval intersection. Used by the satisfiability scan to detect
    /// contradictory rule pairs: when two rules' per-field intervals intersect
    /// to an empty interval, no valid configuration can satisfy both. Returns
    /// <see cref="Empty"/> when the intervals are disjoint.
    /// </summary>
    public NumericInterval Intersect(NumericInterval other)
    {
        if (IsUnbounded) return other;
        if (other.IsUnbounded) return this;
        var lo = Math.Max(Min, other.Min);
        var hi = Math.Min(Max, other.Max);
        return hi < lo ? Empty : new(lo, hi);
    }

    /// <summary>
    /// Sound set difference used by cross-row interval composition. When the
    /// difference would split this interval into two disjoint pieces
    /// (e.g. <c>[0,10] \ [3,5] = [0,3) ∪ (5,10]</c>), falls back to <c>this</c>
    /// unchanged — sound but less precise. The contiguous case
    /// (e.g. <c>[0,10] \ [5,∞) = [0,5)</c>) is the common one for reject-row
    /// guards on the upper end and produces a single tightened interval.
    /// </summary>
    public NumericInterval Difference(NumericInterval other)
    {
        if (IsEmpty || other.IsEmpty) return this;
        if (other.IsUnbounded) return Empty;
        if (IsUnbounded) return this; // can't precisely difference an unbounded interval

        // other entirely outside this: difference is unchanged
        if (other.Max < Min || other.Min > Max) return this;

        // other entirely contains this: difference is empty
        if (other.Min <= Min && other.Max >= Max) return Empty;

        // other clips off the right end: difference is [Min, other.Min)
        // (we approximate the open boundary by leaving the bound inclusive —
        // the scan uses interval *emptiness* as the verdict, so the off-by-one
        // on closed/open is conservative)
        if (other.Min > Min && other.Max >= Max)
            return new(Min, Math.Min(Max, other.Min));

        // other clips off the left end: difference is (other.Max, Max]
        if (other.Max < Max && other.Min <= Min)
            return new(Math.Max(Min, other.Max), Max);

        // other is in the middle — non-contiguous difference. Fall back
        // to the original interval (sound, less precise).
        return this;
    }

    public override string ToString() =>
        IsUnbounded ? "[−∞ .. +∞]" :
        IsEmpty ? "[empty]" :
        $"[{Min} .. {Max}]";
}
