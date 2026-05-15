using System;
using System.Reflection;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class TypedConstantNormalizerTests
{
    [Fact]
    public void NormalizeQuantity_UsesKnownUcumScaleFactors()
    {
        var grams = ParseUnit("g");
        var centimeters = ParseUnit("cm");

        TypedConstantNormalizer.NormalizeQuantity(1m, grams).Should().Be(0.001m);
        TypedConstantNormalizer.NormalizeQuantity(2m, centimeters).Should().Be(0.02m);
        TypedConstantNormalizer.NormalizeQuantity(3m, ParseUnit("kg")).Should().Be(3m);
    }

    [Fact]
    public void NormalizePrice_UsesInverseDenominatorScale()
    {
        var grams = ParseUnit("g");

        TypedConstantNormalizer.NormalizePrice(1m, grams).Should().Be(1000m);
    }

    [Fact]
    public void NormalizeQuantity_NullUnit_LeavesMagnitudeUnchanged()
    {
        TypedConstantNormalizer.NormalizeQuantity(12.5m, null).Should().Be(12.5m);
    }

    [Fact]
    public void DenormalizeQuantity_ReversesNormalization()
    {
        var grams = ParseUnit("g");

        TypedConstantNormalizer.DenormalizeQuantity(0.001m, grams).Should().Be(1m);
    }

    [Fact]
    public void ApplyFactor_UsesExactFactorParts()
    {
        var factor = UcumExactFactor.Parse("1e-3");

        TypedConstantNormalizer.ApplyFactor(500m, factor).Should().Be(0.5m);
    }

    [Fact]
    public void NumericIntervalScale_PreservesSentinelBounds()
    {
        var lowerUnbounded = new NumericInterval(decimal.MinValue, 5m).Scale(2m);
        var upperUnbounded = new NumericInterval(-3m, decimal.MaxValue).Scale(2m);
        var flipped = new NumericInterval(decimal.MinValue, 5m).Scale(-2m);

        lowerUnbounded.Min.Should().Be(decimal.MinValue);
        lowerUnbounded.Max.Should().Be(10m);
        upperUnbounded.Min.Should().Be(-6m);
        upperUnbounded.Max.Should().Be(decimal.MaxValue);
        flipped.Min.Should().Be(-10m);
        flipped.Max.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void NormalizeQuantity_Celsius_AppliesAffineOffset()
    {
        TypedConstantNormalizer.NormalizeQuantity(20m, ParseUnit("Cel")).Should().Be(293.15m);
    }

    [Fact]
    public void NormalizeQuantity_Fahrenheit_AppliesAffineScaleAndOffset()
    {
        TypedConstantNormalizer.NormalizeQuantity(68m, ParseUnit("[degF]")).Should().Be(293.15m);
    }

    [Fact]
    public void NormalizeQuantity_Reaumur_AppliesAffineScaleAndOffset()
    {
        TypedConstantNormalizer.NormalizeQuantity(16m, ParseUnit("[degRe]")).Should().Be(293.15m);
    }

    [Fact]
    public void NormalizeQuantity_Rankine_LinearOnly()
    {
        TypedConstantNormalizer.NormalizeQuantity(527.67m, ParseUnit("[degR]")).Should().Be(293.15m);
    }

    [Fact]
    public void NormalizeQuantity_Kelvin_IdentityScale()
    {
        TypedConstantNormalizer.NormalizeQuantity(293.15m, ParseUnit("K")).Should().Be(293.15m);
    }

    [Fact]
    public void NormalizeQuantity_CelsiusNegative_CorrectOffset()
    {
        TypedConstantNormalizer.NormalizeQuantity(-40m, ParseUnit("Cel")).Should().Be(233.15m);
    }

    [Fact]
    public void NormalizeQuantity_FahrenheitNegative_CorrectOffset()
    {
        TypedConstantNormalizer.NormalizeQuantity(-40m, ParseUnit("[degF]")).Should().Be(233.15m);
    }

    [Fact]
    public void DenormalizeQuantity_Celsius_Roundtrip()
    {
        var normalized = TypedConstantNormalizer.NormalizeQuantity(20m, ParseUnit("Cel"));

        TypedConstantNormalizer.DenormalizeQuantity(normalized, ParseUnit("Cel")).Should().Be(20m);
    }

    [Fact]
    public void DenormalizeQuantity_Fahrenheit_Roundtrip()
    {
        var normalized = TypedConstantNormalizer.NormalizeQuantity(68m, ParseUnit("[degF]"));

        TypedConstantNormalizer.DenormalizeQuantity(normalized, ParseUnit("[degF]")).Should().Be(68m);
    }

    [Fact]
    public void UcumAtom_Celsius_HasAffineOffset()
    {
        GetAffineOffset(UcumAtomCatalog.All["Cel"]).Should().Be(273.15m);
    }

    [Fact]
    public void UcumAtom_Fahrenheit_HasAffineOffset()
    {
        GetAffineOffset(UcumAtomCatalog.All["[degF]"]).Should().Be(459.67m);
    }

    [Fact]
    public void UcumAtom_Kelvin_NoAffineOffset()
    {
        GetAffineOffset(UcumAtomCatalog.All["K"]).Should().BeNull();
    }

    [Fact]
    public void UcumParsedUnit_CelCompound_NoAffineOffset()
    {
        var parsed = UcumParser.Parse("Cel/min");

        parsed.IsValid.Should().BeTrue();
        GetAffineOffset(parsed.Unit!).Should().BeNull();
    }

    [Fact]
    public void UcumParsedUnit_CelStandalone_HasAffineOffset()
    {
        var parsed = UcumParser.Parse("Cel");

        parsed.IsValid.Should().BeTrue();
        GetAffineOffset(parsed.Unit!).Should().Be(273.15m);
    }

    [Fact]
    public void UcumAtom_dB_NoAffineOffset()
    {
        var parsed = UcumParser.Parse("dB");

        parsed.IsValid.Should().BeTrue();
        GetAffineOffset(parsed.Unit!).Should().BeNull();
    }

    [Fact]
    public void UcumAtom_pH_NoAffineOffset()
    {
        GetAffineOffset(UcumAtomCatalog.All["[pH]"]).Should().BeNull();
    }

    private static decimal? GetAffineOffset(object subject)
    {
        var property = subject.GetType().GetProperty("AffineOffset", BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"{subject.GetType().Name} must expose AffineOffset for Slice 37");

        return property!.GetValue(subject) switch
        {
            null => null,
            decimal value => value,
            _ => throw new InvalidOperationException($"Unexpected AffineOffset payload type '{property.PropertyType}'.")
        };
    }

    private static UcumParsedUnit ParseUnit(string text)
    {
        var result = UcumParser.Parse(text);
        result.IsValid.Should().BeTrue($"'{text}' should parse as a valid UCUM unit");
        return result.Unit!;
    }
}
