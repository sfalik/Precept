using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumCatalogTests
{
    [Fact]
    public void Parse_And_IsValid_DelegateToParser()
    {
        UcumCatalog.Parse("kg.m/s^2").IsValid.Should().BeTrue();
        UcumCatalog.IsValid("kg.m/s^2").Should().BeTrue();
        UcumCatalog.IsValid("parsecs").Should().BeFalse();
    }

    [Fact]
    public void BrowseTier1_ReturnsRepresentativeAtoms()
    {
        UcumCatalog.BrowseTier1().Should().Contain(atom => atom.Code == "kg");
        UcumCatalog.BrowseTier1().Should().Contain(atom => atom.Code == "[degF]");
    }

    [Fact]
    public void LookupAtom_ReturnsKnownAtom()
    {
        UcumCatalog.LookupAtom("N")!.Vector.Should().Be(new DimensionVector(1, 1, -2, 0, 0, 0, 0));
    }
}
