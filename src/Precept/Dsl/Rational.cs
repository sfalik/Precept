using System;
using System.Globalization;
using System.Numerics;

namespace Precept;

/// <summary>
/// Exact rational arithmetic for <see cref="LinearForm"/> coefficients.
/// Always in canonical form: GCD-normalized, denominator strictly positive.
/// </summary>
/// <remarks>
/// Implements <see cref="INumber{T}"/> for .NET generic-math compatibility.
/// Uses <c>checked</c> long arithmetic throughout; <see cref="OverflowException"/>
/// propagates to callers (caught by <see cref="LinearForm.TryNormalize"/>).
/// </remarks>
internal readonly record struct Rational : INumber<Rational>, ISignedNumber<Rational>
{
    public long Numerator { get; }
    public long Denominator { get; }

    /// <summary>
    /// Constructs a GCD-normalized rational.  Denominator is always positive after construction.
    /// <c>0/n</c> normalizes to <c>0/1</c>.
    /// </summary>
    /// <exception cref="DivideByZeroException">When <paramref name="denominator"/> is 0.</exception>
    public Rational(long numerator, long denominator)
    {
        if (denominator == 0)
            throw new DivideByZeroException("Rational: denominator cannot be zero.");
        if (numerator == 0) { Numerator = 0; Denominator = 1; return; }
        // Sign normalization: denominator always positive.
        if (denominator < 0) { numerator = checked(-numerator); denominator = -denominator; }
        long g = Gcd(Math.Abs(numerator), denominator);
        Numerator = numerator / g;
        Denominator = denominator / g;
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    private static long Gcd(long a, long b)
    {
        while (b != 0) { long t = b; b = a % b; a = t; }
        return a;
    }

    /// <summary>
    /// Parses a decimal source literal (e.g. <c>"0.1"</c>) to an exact rational
    /// via a <see cref="decimal"/> intermediary.  <c>0.1 → Rational(1, 10)</c>.
    /// </summary>
    public static Rational FromDecimalLiteral(string s)
    {
        if (!decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
            throw new FormatException($"Cannot parse '{s}' as a decimal literal.");
        return FromDecimalValue(d);
    }

    /// <summary>
    /// Converts a finite <see cref="double"/> to an exact rational via
    /// <see cref="decimal"/> intermediary.
    /// </summary>
    /// <exception cref="OverflowException">When the value is non-finite or too large.</exception>
    public static Rational FromDouble(double d)
    {
        if (!double.IsFinite(d)) throw new OverflowException("Cannot convert non-finite double to Rational.");
        return FromDecimalValue((decimal)d);
    }

    internal static Rational FromDecimalValue(decimal d)
    {
        int[] bits = decimal.GetBits(d);
        int scale = (bits[3] >> 16) & 0xFF;
        bool negative = (bits[3] & unchecked((int)0x80000000)) != 0;
        if (bits[2] != 0)
            throw new OverflowException("Decimal value too large for Rational.");
        ulong mantissa = unchecked((ulong)(uint)bits[0]) | ((ulong)(uint)bits[1] << 32);
        if (mantissa > (ulong)long.MaxValue)
            throw new OverflowException("Decimal mantissa too large for Rational.");
        long numerator = negative ? -(long)mantissa : (long)mantissa;
        long denominator = 1;
        for (int i = 0; i < scale; i++) denominator = checked(denominator * 10);
        return new Rational(numerator, denominator);
    }

    // ── Named constants ───────────────────────────────────────────────────────

    public static Rational Zero => new(0, 1);
    public static Rational One => new(1, 1);
    public static Rational NegativeOne => new(-1, 1);
    public static int Radix => 10;
    public static Rational AdditiveIdentity => Zero;
    public static Rational MultiplicativeIdentity => One;

    // ── INumberBase<Rational> predicates ──────────────────────────────────────

    public static Rational Abs(Rational value) =>
        value.Numerator < 0 ? new Rational(checked(-value.Numerator), value.Denominator) : value;

    public static bool IsCanonical(Rational value)          => true;
    public static bool IsComplexNumber(Rational value)      => false;
    public static bool IsEvenInteger(Rational value)        => IsInteger(value) && value.Numerator % 2 == 0;
    public static bool IsFinite(Rational value)             => true;
    public static bool IsImaginaryNumber(Rational value)    => false;
    public static bool IsInfinity(Rational value)           => false;
    public static bool IsInteger(Rational value)            => value.Denominator == 1;
    public static bool IsNaN(Rational value)                => false;
    public static bool IsNegative(Rational value)           => value.Numerator < 0;
    public static bool IsNegativeInfinity(Rational value)   => false;
    public static bool IsNormal(Rational value)             => value.Numerator != 0;
    public static bool IsOddInteger(Rational value)         => IsInteger(value) && Math.Abs(value.Numerator) % 2 == 1;
    public static bool IsPositive(Rational value)           => value.Numerator > 0;
    public static bool IsPositiveInfinity(Rational value)   => false;
    public static bool IsRealNumber(Rational value)         => true;
    public static bool IsSubnormal(Rational value)          => false;
    public static bool IsZero(Rational value)               => value.Numerator == 0;

    public static Rational MaxMagnitude(Rational x, Rational y)       => Abs(x) >= Abs(y) ? x : y;
    public static Rational MaxMagnitudeNumber(Rational x, Rational y) => MaxMagnitude(x, y);
    public static Rational MinMagnitude(Rational x, Rational y)       => Abs(x) <= Abs(y) ? x : y;
    public static Rational MinMagnitudeNumber(Rational x, Rational y) => MinMagnitude(x, y);

    // ── Arithmetic operators ──────────────────────────────────────────────────

    public static Rational operator +(Rational a, Rational b)
    {
        // a/p + b/q = (a*q + b*p) / (p*q), reduced by gcd(p,q) first.
        long g = Gcd(a.Denominator, b.Denominator);
        long den = checked(a.Denominator / g * b.Denominator);
        long num = checked(a.Numerator * (b.Denominator / g) + b.Numerator * (a.Denominator / g));
        return new Rational(num, den);
    }

    public static Rational operator -(Rational a, Rational b) => a + (-b);

    public static Rational operator -(Rational value) =>
        new Rational(checked(-value.Numerator), value.Denominator);

    public static Rational operator +(Rational value) => value;

    public static Rational operator *(Rational a, Rational b)
    {
        // Cross-GCD pre-reduction: gcd(a.Num, b.Den) and gcd(b.Num, a.Den) before multiplying.
        long g1 = Gcd(Math.Abs(a.Numerator), b.Denominator);
        long g2 = Gcd(Math.Abs(b.Numerator), a.Denominator);
        long num = checked((a.Numerator / g1) * (b.Numerator / g2));
        long den = checked((a.Denominator / g2) * (b.Denominator / g1));
        return new Rational(num, den);
    }

    public static Rational operator /(Rational a, Rational b)
    {
        if (b.Numerator == 0) throw new DivideByZeroException("Division by zero Rational.");
        // a/p ÷ b/q = (a*q) / (p*b). Cross-GCD: gcd(a.Num, b.Num) and gcd(a.Den, b.Den).
        long g1 = Gcd(Math.Abs(a.Numerator), Math.Abs(b.Numerator));
        long g2 = Gcd(a.Denominator, b.Denominator);
        long num = checked((a.Numerator / g1) * (b.Denominator / g2));
        long den = checked((a.Denominator / g2) * (b.Numerator / g1));
        // den may be negative if b.Numerator is negative; constructor normalizes sign.
        return new Rational(num, den);
    }

    public static Rational operator %(Rational a, Rational b)
    {
        if (b.Numerator == 0) throw new DivideByZeroException("Modulo by zero Rational.");
        // Truncated remainder toward zero: a - trunc(a/b) * b.
        Rational div = a / b;
        long truncated = div.Numerator / div.Denominator; // C# integer division truncates toward zero.
        return a - new Rational(truncated, 1) * b;
    }

    public static Rational operator ++(Rational value) => value + One;
    public static Rational operator --(Rational value) => value - One;

    // ── Comparison operators ──────────────────────────────────────────────────

    public static bool operator <(Rational a, Rational b)  => a.CompareTo(b) < 0;
    public static bool operator <=(Rational a, Rational b) => a.CompareTo(b) <= 0;
    public static bool operator >(Rational a, Rational b)  => a.CompareTo(b) > 0;
    public static bool operator >=(Rational a, Rational b) => a.CompareTo(b) >= 0;

    // ── IComparable ───────────────────────────────────────────────────────────

    public int CompareTo(Rational other)
    {
        // a/p vs b/q: sign of (a*q - b*p). Use Int128 to avoid long overflow.
        var lhs = (Int128)Numerator * other.Denominator;
        var rhs = (Int128)other.Numerator * Denominator;
        return lhs.CompareTo(rhs);
    }

    public int CompareTo(object? obj) =>
        obj is Rational r ? CompareTo(r) : throw new ArgumentException("Object is not a Rational.");

    // ── IFormattable / ISpanFormattable ───────────────────────────────────────

    public override string ToString() =>
        Denominator == 1 ? Numerator.ToString() : $"{Numerator}/{Denominator}";

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public bool TryFormat(Span<char> destination, out int charsWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var s = ToString();
        if (s.TryCopyTo(destination)) { charsWritten = s.Length; return true; }
        charsWritten = 0;
        return false;
    }

    // ── IParsable<Rational> / ISpanParsable<Rational> ─────────────────────────

    public static Rational Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var r) ? r : throw new FormatException($"Cannot parse '{s}' as Rational.");

    public static bool TryParse(string? s, IFormatProvider? provider, out Rational result)
    {
        if (s is null) { result = default; return false; }
        return TryParse(s.AsSpan(), provider, out result);
    }

    public static Rational Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var r) ? r : throw new FormatException("Cannot parse as Rational.");

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Rational result)
    {
        int slash = s.IndexOf('/');
        if (slash >= 0)
        {
            if (long.TryParse(s[..slash], out long n) && long.TryParse(s[(slash + 1)..], out long d))
            {
                try { result = new Rational(n, d); return true; }
                catch { result = default; return false; }
            }
            result = default;
            return false;
        }
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dec))
        {
            try { result = FromDecimalValue(dec); return true; }
            catch { result = default; return false; }
        }
        result = default;
        return false;
    }

    // INumberBase<T> NumberStyles overloads (delegate to basic parse).
    public static Rational Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);
    public static Rational Parse(string s, NumberStyles style, IFormatProvider? provider) =>
        Parse(s, provider);
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider,
        out Rational result) => TryParse(s, provider, out result);
    public static bool TryParse(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? s,
        NumberStyles style, IFormatProvider? provider, out Rational result) =>
        TryParse(s, provider, out result);

    // ── TryConvert* (INumberBase<T> protected static) ────────────────────────

    static bool INumberBase<Rational>.TryConvertFromChecked<TOther>(TOther value, out Rational result) =>
        TryConvertFrom(value, out result);
    static bool INumberBase<Rational>.TryConvertFromSaturating<TOther>(TOther value, out Rational result) =>
        TryConvertFrom(value, out result);
    static bool INumberBase<Rational>.TryConvertFromTruncating<TOther>(TOther value, out Rational result) =>
        TryConvertFrom(value, out result);

    private static bool TryConvertFrom<TOther>(TOther value, out Rational result)
        where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(int))      { result = new Rational((int)(object)value, 1);  return true; }
        if (typeof(TOther) == typeof(long))     { result = new Rational((long)(object)value, 1); return true; }
        if (typeof(TOther) == typeof(Rational)) { result = (Rational)(object)value;              return true; }
        if (typeof(TOther) == typeof(double))
        {
            double d = (double)(object)value;
            if (!double.IsFinite(d)) { result = default; return false; }
            try { result = FromDecimalValue((decimal)d); return true; }
            catch { result = default; return false; }
        }
        if (typeof(TOther) == typeof(decimal))
        {
            try { result = FromDecimalValue((decimal)(object)value); return true; }
            catch { result = default; return false; }
        }
        result = default;
        return false;
    }

    static bool INumberBase<Rational>.TryConvertToChecked<TOther>(Rational value, out TOther result) =>
        TryConvertTo(value, out result);
    static bool INumberBase<Rational>.TryConvertToSaturating<TOther>(Rational value, out TOther result) =>
        TryConvertTo(value, out result);
    static bool INumberBase<Rational>.TryConvertToTruncating<TOther>(Rational value, out TOther result) =>
        TryConvertTo(value, out result);

    private static bool TryConvertTo<TOther>(Rational value, out TOther result)
        where TOther : INumberBase<TOther>
    {
        double d = (double)value.Numerator / value.Denominator;
        bool ok = TOther.TryConvertFromChecked(d, out TOther? converted);
        result = converted!;
        return ok;
    }
}
