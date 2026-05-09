using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumCatalogTests
{
    [Fact]
    public void Parse_And_IsValid_RemainAvailableThroughCompatibilityShim()
    {
        UcumCatalog.Parse("kg.m/s^2").IsValid.Should().BeTrue();
        UcumCatalog.IsValid("kg.m/s^2").Should().BeTrue();
        UcumCatalog.IsValid("parsecs").Should().BeFalse();
    }

    [Fact]
    public void CompatibilityShim_ExposesFullAtomCatalog()
    {
        UcumCatalog.All.Should().BeSameAs(UcumAtomCatalog.All);

        var nonTier1Code = UcumAtomCatalog.All.Keys.First(code => !UcumAtomCatalog.BrowseTier1().Any(atom => atom.Code == code));
        UcumCatalog.LookupAtom(nonTier1Code).Should().Be(UcumAtomCatalog.All[nonTier1Code]);
    }

    [Fact]
    public void BrowseTier1_ReturnsCuratedTier1EntriesInDeclaredOrder()
    {
        var tier1 = UcumAtomCatalog.BrowseTier1();
        var derivedSpeed = UcumParser.Parse("km/h").Unit!;

        tier1.Should().HaveCount(150);
        tier1.Select(atom => atom.Code).Take(8).Should().Equal("m", "dm", "cm", "mm", "km", "um", "nm", "Ao");
        tier1.Should().Contain(atom => atom.Code == "km/h");
        tier1.Should().Contain(atom => atom.Code == "m2");
        tier1.Should().Contain(atom => atom.Code == "[degF]");
        tier1.Should().NotContain(atom => atom.Code == "s");
        tier1.Should().NotContain(atom => atom.Code == "mol");
        tier1.Single(atom => atom.Code == "[degF]").Should().BeSameAs(UcumAtomCatalog.All["[degF]"]);
        tier1.Single(atom => atom.Code == "km/h").Vector.Should().Be(derivedSpeed.Vector);
        tier1.Single(atom => atom.Code == "km/h").Scale.Should().Be(derivedSpeed.Scale);
    }
}
