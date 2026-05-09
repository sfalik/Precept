using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Precept.Language;

public readonly record struct UcumExactFactor
{
    private static readonly Regex NumericPattern =
        new(@"^[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?$", RegexOptions.Compiled);

    public static UcumExactFactor One => new(BigInteger.One, BigInteger.One, 0);

    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }
    public int Base10Exponent { get; }

    public UcumExactFactor(BigInteger numerator, BigInteger denominator, int base10Exponent)
    {
        if (denominator.IsZero)
            throw new DivideByZeroException();

        if (numerator.IsZero)
        {
            Numerator = BigInteger.Zero;
            Denominator = BigInteger.One;
            Base10Exponent = 0;
            return;
        }

        if (denominator.Sign < 0)
        {
            numerator = BigInteger.Negate(numerator);
            denominator = BigInteger.Negate(denominator);
        }

        var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        numerator /= gcd;
        denominator /= gcd;

        while ((numerator % 10) == 0)
        {
            numerator /= 10;
            base10Exponent++;
        }

        while ((denominator % 10) == 0)
        {
            denominator /= 10;
            base10Exponent--;
        }

        Numerator = numerator;
        Denominator = denominator;
        Base10Exponent = base10Exponent;
    }

    public static UcumExactFactor FromInt(int value) => new(new BigInteger(value), BigInteger.One, 0);

    public static UcumExactFactor Parse(string text)
    {
        var trimmed = text.Trim();
        if (!NumericPattern.IsMatch(trimmed))
            throw new FormatException($"'{text}' is not a valid UCUM exact factor.");

        var exponentIndex = trimmed.IndexOfAny(['e', 'E']);
        var mantissa = exponentIndex >= 0 ? trimmed[..exponentIndex] : trimmed;
        var explicitExponent = exponentIndex >= 0
            ? int.Parse(trimmed[(exponentIndex + 1)..], CultureInfo.InvariantCulture)
            : 0;

        var negative = mantissa.StartsWith("-", StringComparison.Ordinal);
        if (negative || mantissa.StartsWith("+", StringComparison.Ordinal))
            mantissa = mantissa[1..];

        var decimalIndex = mantissa.IndexOf('.');
        var decimalPlaces = decimalIndex >= 0 ? mantissa.Length - decimalIndex - 1 : 0;
        var digits = decimalIndex >= 0 ? mantissa.Remove(decimalIndex, 1) : mantissa;
        var numerator = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        if (negative)
            numerator = BigInteger.Negate(numerator);

        return new UcumExactFactor(numerator, BigInteger.One, explicitExponent - decimalPlaces);
    }

    public UcumExactFactor Multiply(UcumExactFactor other) => new(
        Numerator * other.Numerator,
        Denominator * other.Denominator,
        Base10Exponent + other.Base10Exponent);

    public UcumExactFactor Divide(UcumExactFactor other) => new(
        Numerator * other.Denominator,
        Denominator * other.Numerator,
        Base10Exponent - other.Base10Exponent);

    public UcumExactFactor Pow(int exponent)
    {
        if (exponent == 0)
            return One;

        if (exponent < 0)
            return new UcumExactFactor(
                BigInteger.Pow(Denominator, -exponent),
                BigInteger.Pow(Numerator, -exponent),
                -Base10Exponent * -exponent);

        return new UcumExactFactor(
            BigInteger.Pow(Numerator, exponent),
            BigInteger.Pow(Denominator, exponent),
            Base10Exponent * exponent);
    }
}
