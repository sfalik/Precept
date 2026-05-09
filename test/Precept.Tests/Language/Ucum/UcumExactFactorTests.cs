using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumExactFactorTests
{
    [Fact]
    public void Multiply_And_Divide_AreExact()
    {
        var factor = UcumExactFactor.FromInt(5)
            .Divide(UcumExactFactor.FromInt(9))
            .Multiply(UcumExactFactor.FromInt(9));

        factor.Should().Be(UcumExactFactor.FromInt(5));
    }

    [Fact]
    public void Parse_And_Multiply_NormalizePowersOfTen()
    {
        var factor = UcumExactFactor.Parse("1e-3").Multiply(UcumExactFactor.Parse("1e3"));

        factor.Should().Be(UcumExactFactor.One);
    }

    [Fact]
    public void Pow_AppliesIntegerExponents()
    {
        UcumExactFactor.Parse("1e-3").Pow(2).Should().Be(UcumExactFactor.Parse("1e-6"));
    }
}
