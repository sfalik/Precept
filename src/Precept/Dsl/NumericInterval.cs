using System;
using System.Globalization;

namespace Precept;

/// <summary>
/// An immutable closed/open interval over <c>double</c> used by the non-SMT proof engine
/// (Layers 2–5) to track the range of values that a numeric expression can produce.
/// </summary>
/// <remarks>
/// <para>Named constants: <see cref="Unknown"/> = (-∞, +∞), <see cref="Positive"/> = (0, +∞),
/// <see cref="Nonneg"/> = [0, +∞), <see cref="Zero"/> = [0, 0].</para>
/// <para>All arithmetic uses IEEE 754 <c>double</c>. Overflow saturates to ±∞ (sound — an interval
/// containing ±∞ is a valid over-approximation). NaN inputs produce Unknown-equivalent intervals.</para>
/// </remarks>
internal readonly record struct NumericInterval(
    double Lower, bool LowerInclusive,
    double Upper, bool UpperInclusive)
{
    // ── Named intervals ──────────────────────────────────────────────────────

    /// <summary>No information: (-∞, +∞).</summary>
    public static readonly NumericInterval Unknown =
        new(double.NegativeInfinity, false, double.PositiveInfinity, false);

    /// <summary>Strictly positive: (0, +∞).</summary>
    public static readonly NumericInterval Positive =
        new(0, false, double.PositiveInfinity, false);

    /// <summary>Non-negative: [0, +∞).</summary>
    public static readonly NumericInterval Nonneg =
        new(0, true, double.PositiveInfinity, false);

    /// <summary>Exactly zero: [0, 0].</summary>
    public static readonly NumericInterval Zero =
        new(0, true, 0, true);

    // ── Key predicates ───────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when the interval provably does not contain zero.
    /// This is the primary predicate for C93 (divisor safety) suppression.
    /// </summary>
    public bool ExcludesZero =>
        Lower > 0 ||
        Upper < 0 ||
        (Lower == 0 && !LowerInclusive) ||
        (Upper == 0 && !UpperInclusive);

    /// <summary>
    /// <c>true</c> when all values in the interval are ≥ 0 (lower bound is at or above 0).
    /// This is the primary predicate for C76 (sqrt safety) suppression.
    /// </summary>
    public bool IsNonnegative => Lower >= 0;

    /// <summary><c>true</c> when all values in the interval are strictly > 0.</summary>
    public bool IsPositive => Lower > 0 || (Lower == 0 && !LowerInclusive);

    /// <summary><c>true</c> when this is the Unknown interval (full real line).</summary>
    public bool IsUnknown => double.IsNegativeInfinity(Lower) && double.IsPositiveInfinity(Upper);

    // ── Transfer rules ───────────────────────────────────────────────────────

    /// <summary>[a,b] + [c,d] = [a+c, b+d]</summary>
    public static NumericInterval Add(NumericInterval a, NumericInterval b) =>
        new(a.Lower + b.Lower, a.LowerInclusive && b.LowerInclusive,
            a.Upper + b.Upper, a.UpperInclusive && b.UpperInclusive);

    /// <summary>[a,b] - [c,d] = [a-d, b-c]</summary>
    public static NumericInterval Subtract(NumericInterval a, NumericInterval b) =>
        new(a.Lower - b.Upper, a.LowerInclusive && b.UpperInclusive,
            a.Upper - b.Lower, a.UpperInclusive && b.LowerInclusive);

    /// <summary>
    /// Sign-case decomposition to avoid <c>0 × ∞ = NaN</c>.
    /// Naive four-corner multiplication produces NaN when an endpoint is zero and the
    /// other is ±∞. The 9-case decomposition below guarantees all four products involve
    /// finite nonzero factors in the only case that still uses min/max (both mixed).
    /// </summary>
    public static NumericInterval Multiply(NumericInterval a, NumericInterval b)
    {
        double al = a.Lower, au = a.Upper;
        double bl = b.Lower, bu = b.Upper;
        bool ali = a.LowerInclusive, aui = a.UpperInclusive;
        bool bli = b.LowerInclusive, bui = b.UpperInclusive;

        // Both positive
        if (al >= 0 && bl >= 0)
            return new(al * bl, ali && bli, au * bu, aui && bui);

        // Both negative
        if (au <= 0 && bu <= 0)
            return new(au * bu, aui && bui, al * bl, ali && bli);

        // Left positive, right negative
        if (al >= 0 && bu <= 0)
            return new(au * bl, aui && bli, al * bu, ali && bui);

        // Left negative, right positive
        if (au <= 0 && bl >= 0)
            return new(al * bu, ali && bui, au * bl, aui && bli);

        // Left positive, right mixed
        if (al >= 0 && bl < 0 && bu > 0)
            return new(au * bl, aui && bli, au * bu, aui && bui);

        // Left negative, right mixed
        if (au <= 0 && bl < 0 && bu > 0)
            return new(al * bu, ali && bui, al * bl, ali && bli);

        // Left mixed, right positive
        if (al < 0 && au > 0 && bl >= 0)
            return new(al * bu, ali && bui, au * bu, aui && bui);

        // Left mixed, right negative
        if (al < 0 && au > 0 && bu <= 0)
            return new(au * bl, aui && bli, al * bl, ali && bli);

        // Both mixed: all four products involve finite nonzero factors
        var p1 = al * bu; var p2 = au * bl; var p3 = al * bl; var p4 = au * bu;
        return new(Math.Min(p1, p2), ali && bui || aui && bli,
                   Math.Max(p3, p4), ali && bli || aui && bui);
    }

    /// <summary>
    /// [a,b] / [c,d] when [c,d] excludes zero; otherwise <see cref="Unknown"/>.
    /// </summary>
    public static NumericInterval Divide(NumericInterval a, NumericInterval b)
    {
        if (!b.ExcludesZero)
            return Unknown;

        // Invert b: 1/[c,d] = [1/d, 1/c] (reversed because c and d are same sign)
        var bInv = new NumericInterval(1.0 / b.Upper, b.UpperInclusive,
                                       1.0 / b.Lower, b.LowerInclusive);
        return Multiply(a, bInv);
    }

    /// <summary>Negate: [-b, -a] with flipped inclusivity.</summary>
    public static NumericInterval Negate(NumericInterval a) =>
        new(-a.Upper, a.UpperInclusive, -a.Lower, a.LowerInclusive);

    /// <summary>
    /// |[a,b]|: identity when both nonneg; negate when both nonpositive;
    /// [0, max(|a|,|b|)] when mixed (includes zero).
    /// </summary>
    public static NumericInterval Abs(NumericInterval a)
    {
        if (a.Lower >= 0) return a;                      // both nonneg — identity
        if (a.Upper <= 0) return Negate(a);              // both nonpositive — negate
        // mixed: lower becomes 0, upper is max of absolute values
        var upper = Math.Max(Math.Abs(a.Lower), Math.Abs(a.Upper));
        return new(0, true, upper, true);
    }

    /// <summary>min([a,b], [c,d]) = [min(a,c), min(b,d)]</summary>
    public static NumericInterval Min(NumericInterval a, NumericInterval b) =>
        new(Math.Min(a.Lower, b.Lower), a.Lower <= b.Lower ? a.LowerInclusive : b.LowerInclusive,
            Math.Min(a.Upper, b.Upper), a.Upper <= b.Upper ? a.UpperInclusive : b.UpperInclusive);

    /// <summary>max([a,b], [c,d]) = [max(a,c), max(b,d)]</summary>
    public static NumericInterval Max(NumericInterval a, NumericInterval b) =>
        new(Math.Max(a.Lower, b.Lower), a.Lower >= b.Lower ? a.LowerInclusive : b.LowerInclusive,
            Math.Max(a.Upper, b.Upper), a.Upper >= b.Upper ? a.UpperInclusive : b.UpperInclusive);

    /// <summary>clamp(x, lo, hi) = [max(x.Lower, lo.Lower), min(x.Upper, hi.Upper)]</summary>
    public static NumericInterval Clamp(NumericInterval x, NumericInterval lo, NumericInterval hi)
    {
        var lower = Math.Max(x.Lower, lo.Lower);
        var lowerInc = lower == x.Lower ? x.LowerInclusive : lo.LowerInclusive;
        var upper = Math.Min(x.Upper, hi.Upper);
        var upperInc = upper == x.Upper ? x.UpperInclusive : hi.UpperInclusive;
        return new(lower, lowerInc, upper, upperInc);
    }

    /// <summary>
    /// Hull (join for conditional expression synthesis): smallest interval enclosing both.
    /// When both lower bounds are equal, <c>LowerInclusive = a.LowerInclusive || b.LowerInclusive</c>
    /// (and likewise for upper).
    /// </summary>
    public static NumericInterval Hull(NumericInterval a, NumericInterval b)
    {
        double lower = Math.Min(a.Lower, b.Lower);
        bool lowerInc;
        if (a.Lower == b.Lower)
            lowerInc = a.LowerInclusive || b.LowerInclusive;
        else
            lowerInc = a.Lower < b.Lower ? a.LowerInclusive : b.LowerInclusive;

        double upper = Math.Max(a.Upper, b.Upper);
        bool upperInc;
        if (a.Upper == b.Upper)
            upperInc = a.UpperInclusive || b.UpperInclusive;
        else
            upperInc = a.Upper > b.Upper ? a.UpperInclusive : b.UpperInclusive;

        return new(lower, lowerInc, upper, upperInc);
    }

    // ── Interval marker serialization ────────────────────────────────────────

    /// <summary>
    /// Serializes this interval into the <c>$ival:{key}:{lower}:{lowerInc}:{upper}:{upperInc}</c>
    /// marker key format used by the symbol table. All numeric values use InvariantCulture.
    /// </summary>
    public string ToMarkerKey(string fieldKey) =>
        string.Create(CultureInfo.InvariantCulture,
            $"$ival:{fieldKey}:{Lower}:{(LowerInclusive ? "true" : "false")}:{Upper}:{(UpperInclusive ? "true" : "false")}");

    /// <summary>
    /// Parses an interval from a marker key produced by <see cref="ToMarkerKey"/>.
    /// Returns <see cref="Unknown"/> if the key is malformed.
    /// </summary>
    public static bool TryParseMarkerKey(string markerKey, out NumericInterval result)
    {
        result = Unknown;
        // format: $ival:{key}:{lower}:{lowerInc}:{upper}:{upperInc}
        var parts = markerKey.Split(':');
        if (parts.Length < 6) return false;
        // parts[0] = "$ival", parts[1] = field key (may be dotted), parts[2] = lower,
        // parts[3] = lowerInc, parts[4] = upper, parts[5] = upperInc
        // Parse from the end to handle dotted field keys with embedded dots
        if (!double.TryParse(parts[parts.Length - 4], NumberStyles.Float, CultureInfo.InvariantCulture, out var lower)) return false;
        if (!double.TryParse(parts[parts.Length - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var upper)) return false;
        var lowerInc = parts[parts.Length - 3] == "true";
        var upperInc = parts[parts.Length - 1] == "true";
        result = new(lower, lowerInc, upper, upperInc);
        return true;
    }
}
