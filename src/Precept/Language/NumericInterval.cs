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

    public NumericInterval Add(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        return new(Min + other.Min, Max + other.Max);
    }

    public NumericInterval Subtract(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        return new(Min - other.Max, Max - other.Min); // note: swapped
    }

    public NumericInterval Multiply(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        var corners = new[] { Min * other.Min, Min * other.Max, Max * other.Min, Max * other.Max };
        return new(corners.Min(), corners.Max());
    }

    public NumericInterval Divide(NumericInterval other)
    {
        if (IsUnbounded || other.IsUnbounded) return Unbounded;
        if (other.Min <= 0m && other.Max >= 0m) return Unbounded;
        var corners = new[] { Min / other.Min, Min / other.Max, Max / other.Min, Max / other.Max };
        return new(corners.Min(), corners.Max());
    }

    public NumericInterval Negate()
    {
        if (IsUnbounded) return Unbounded;
        return new(-Max, -Min);
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
