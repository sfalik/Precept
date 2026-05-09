using System;
using System.Linq;
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
    public void BrowseTier1_ReturnsCuratedTier1Entries()
    {
        var tier1 = UcumCatalog.BrowseTier1();

        UcumCatalog.All.Should().ContainKeys("kg", "[degF]");
        tier1.Should().OnlyContain(atom => UcumCatalog.All.ContainsKey(atom.Code));
        tier1.Should().HaveCount(150);
        tier1.Should().Contain(atom => atom.Code == "kg");
        tier1.Should().Contain(atom => atom.Code == "dm");
        tier1.Should().Contain(atom => atom.Code == "km/h");
        tier1.Should().Contain(atom => atom.Code == "[degF]");
        tier1.Should().NotContain(atom => atom.Code == "s");
        tier1.Should().NotContain(atom => atom.Code == "mol");
    }

    [Fact]
    public void LookupAtom_RemainsBackedByFullAtomCatalog()
    {
        var nonTier1Code = UcumAtomCatalog.All.Keys.First(code => !UcumCatalog.All.ContainsKey(code));

        UcumCatalog.LookupAtom(nonTier1Code).Should().Be(UcumAtomCatalog.All[nonTier1Code]);
    }
}
