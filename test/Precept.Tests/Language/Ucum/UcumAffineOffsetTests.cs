using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumAffineOffsetTests
{
    [Fact]
    public void Cel_Atom_HasAffineOffset()
    {
        UcumAtomCatalog.All["Cel"].AffineOffset.Should().Be(273.15m);
    }

    [Fact]
    public void DegF_Atom_HasAffineOffset()
    {
        UcumAtomCatalog.All["[degF]"].AffineOffset.Should().Be(459.67m);
    }

    [Fact]
    public void K_Atom_HasNoAffineOffset()
    {
        UcumAtomCatalog.All["K"].AffineOffset.Should().BeNull();
    }

    [Fact]
    public void Kg_Atom_HasNoAffineOffset()
    {
        UcumAtomCatalog.All["kg"].AffineOffset.Should().BeNull();
    }
}
