using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumPrefixCatalogTests
{
    [Fact]
    public void All_LoadsTwentySiPrefixes()
    {
        UcumPrefixCatalog.All.Count.Should().Be(20);
        UcumPrefixCatalog.All["k"].Factor.Should().Be(UcumExactFactor.Parse("1e3"));
        UcumPrefixCatalog.All["m"].Factor.Should().Be(UcumExactFactor.Parse("1e-3"));
    }

    [Fact]
    public void LongestPrefixMatch_ResolvesMetricPrefix()
    {
        UcumPrefixCatalog.LongestPrefixMatch("mg")!.Code.Should().Be("m");
        UcumPrefixCatalog.LongestPrefixMatch("kg")!.Code.Should().Be("k");
    }
}
