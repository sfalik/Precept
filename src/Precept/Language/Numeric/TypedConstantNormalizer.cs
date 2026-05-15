using System;
using System.Globalization;
using System.Linq;

namespace Precept.Language;

public static class TypedConstantNormalizer
{
    public static decimal NormalizeQuantity(decimal magnitude, UcumParsedUnit? unit) =>
        unit is null
            ? magnitude
            : TryGetStaticAffineParams(unit, out var scale, out var offset)
                ? AffineToBase(magnitude, scale, offset)
                : magnitude;

    public static decimal NormalizePrice(decimal magnitude, UcumParsedUnit? denominatorUnit)
    {
        if (denominatorUnit is null)
            return magnitude;

        if (!TryGetStaticAffineParams(denominatorUnit, out var scale, out var offset))
            return magnitude;

        if (offset.HasValue)
            throw new InvalidOperationException("Price normalization does not support affine-offset units");

        return magnitude / scale;
    }

    public static decimal DenormalizeQuantity(decimal normalizedMagnitude, UcumParsedUnit? unit)
    {
        if (unit is null)
            return normalizedMagnitude;

        if (!TryGetStaticAffineParams(unit, out var scale, out var offset))
            return normalizedMagnitude;

        return AffineFromBase(normalizedMagnitude, scale, offset);
    }

    public static decimal ApplyFactor(decimal magnitude, UcumExactFactor factor) =>
        magnitude * FactorToDecimal(factor);

    public static decimal? TryGetStaticScalingFactor(UcumParsedUnit? unit)
    {
        if (unit is null)
            return null;

        if (unit.AffineOffset.HasValue
            || unit.SourceText.Contains('{', StringComparison.Ordinal)
            || unit.CanonicalCode.Contains('{', StringComparison.Ordinal)
            || unit.UsedAtoms.Any(atom => atom.AffineOffset.HasValue))
        {
            return null;
        }

        return FactorToDecimal(unit.Scale);
    }

    public static bool TryGetStaticAffineParams(UcumParsedUnit? unit, out decimal scale, out decimal? offset)
    {
        if (unit is null
            || unit.SourceText.Contains('{', StringComparison.Ordinal)
            || unit.CanonicalCode.Contains('{', StringComparison.Ordinal))
        {
            scale = default;
            offset = default;
            return false;
        }

        scale = FactorToDecimal(unit.Scale);
        offset = unit.AffineOffset;
        return true;
    }

    private static decimal AffineToBase(decimal value, decimal scale, decimal? offset)
    {
        var normalized = offset.HasValue ? (value + offset.Value) * scale : value * scale;
        return NormalizeResult(normalized);
    }

    private static decimal AffineFromBase(decimal value, decimal scale, decimal? offset)
    {
        var denormalized = offset.HasValue ? value / scale - offset.Value : value / scale;
        return NormalizeResult(denormalized);
    }

    private static decimal NormalizeResult(decimal value) =>
        decimal.Round(value, 24, MidpointRounding.ToEven);

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
