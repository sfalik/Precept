using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumAtomCatalogTests
{
    [Fact]
    public void All_LoadsExpectedAtoms()
    {
        UcumAtomCatalog.All.Count.Should().BeGreaterThanOrEqualTo(300);
        UcumAtomCatalog.All.Should().ContainKey("kg");
        UcumAtomCatalog.All.Should().ContainKey("m");
        UcumAtomCatalog.All.Should().ContainKey("s");
        UcumAtomCatalog.All.Should().ContainKey("mol");
        UcumAtomCatalog.All.Should().ContainKey("[degF]");
    }

    [Fact]
    public void All_ComputesExpectedVectors()
    {
        UcumAtomCatalog.All["kg"].Vector.Should().Be(new DimensionVector(0, 1, 0, 0, 0, 0, 0));
        UcumAtomCatalog.All["N"].Vector.Should().Be(new DimensionVector(1, 1, -2, 0, 0, 0, 0));
        UcumAtomCatalog.All["[degF]"].Vector.Should().Be(new DimensionVector(0, 0, 0, 0, 1, 0, 0));
    }

    [Fact]
    public void All_ComputesExpectedPrefixabilityAndScale()
    {
        UcumAtomCatalog.All["g"].Prefixable.Should().BeTrue();
        UcumAtomCatalog.All["kg"].Prefixable.Should().BeFalse();
        UcumAtomCatalog.All["[degF]"].Scale.Should().Be(
            UcumExactFactor.FromInt(5).Divide(UcumExactFactor.FromInt(9)));
    }
}
