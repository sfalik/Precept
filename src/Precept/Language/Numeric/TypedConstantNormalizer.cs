using System.Globalization;

namespace Precept.Language;

public static class TypedConstantNormalizer
{
    public static decimal NormalizeQuantity(decimal magnitude, UcumParsedUnit? unit) =>
        unit is null ? magnitude : ApplyFactor(magnitude, unit.Scale);

    public static decimal NormalizePrice(decimal magnitude, UcumParsedUnit? denominatorUnit)
    {
        if (denominatorUnit is null)
            return magnitude;

        var factor = FactorToDecimal(denominatorUnit.Scale);
        return magnitude / factor;
    }

    public static decimal DenormalizeQuantity(decimal normalizedMagnitude, UcumParsedUnit? unit)
    {
        if (unit is null)
            return normalizedMagnitude;

        var factor = FactorToDecimal(unit.Scale);
        return normalizedMagnitude / factor;
    }

    public static decimal ApplyFactor(decimal magnitude, UcumExactFactor factor) =>
        magnitude * FactorToDecimal(factor);

    private static decimal FactorToDecimal(UcumExactFactor factor)
    {
        var numerator = decimal.Parse(factor.Numerator.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        var denominator = decimal.Parse(factor.Denominator.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        var value = numerator / denominator;

        if (factor.Base10Exponent > 0)
        {
            for (var i = 0; i < factor.Base10Exponent; i++)
                value *= 10m;
            return value;
        }

        for (var i = 0; i > factor.Base10Exponent; i--)
            value /= 10m;

        return value;
    }
}
