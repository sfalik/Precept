using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumParserTests
{
    [Theory]
    [InlineData("kg")]
    [InlineData("m")]
    [InlineData("s")]
    [InlineData("mol")]
    [InlineData("mg")]
    [InlineData("cm")]
    [InlineData("mmol")]
    [InlineData("kg.m")]
    [InlineData("m/s")]
    [InlineData("m/s^2")]
    [InlineData("kg.m/s^2")]
    [InlineData("mmol/(L.min)")]
    [InlineData("[degF]")]
    [InlineData("mm[Hg]")]
    [InlineData("J/s")]
    public void Parse_AcceptsValidExpressions(string text)
    {
        UcumParser.Parse(text).IsValid.Should().BeTrue(text);
    }

    [Fact]
    public void Parse_PreservesAnnotationsButExcludesThemFromCanonicalCode()
    {
        var result = UcumParser.Parse("{RBC}/uL");

        result.IsValid.Should().BeTrue();
        result.Unit!.Annotations.Should().ContainSingle().Which.Should().Be("RBC");
        result.Unit.CanonicalCode.Should().Be("1/uL");
    }

    [Fact]
    public void Parse_ComputesExpectedDimensionAliases()
    {
        UcumParser.Parse("kg.m/s^2").Unit!.PreferredDimensionAlias.Should().Be("force");
        UcumParser.Parse("m/s").Unit!.PreferredDimensionAlias.Should().Be("speed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("parsecs")]
    [InlineData("(")]
    [InlineData("m//s")]
    public void Parse_RejectsInvalidExpressions(string text)
    {
        UcumParser.Parse(text).IsValid.Should().BeFalse();
    }
}
