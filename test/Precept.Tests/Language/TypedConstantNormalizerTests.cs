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

    private static UcumParsedUnit ParseUnit(string text)
    {
        var result = UcumParser.Parse(text);
        result.IsValid.Should().BeTrue($"'{text}' should parse as a valid UCUM unit");
        return result.Unit!;
    }
}
