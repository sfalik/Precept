using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class DimensionCatalogTests
{
    [Fact]
    public void AllAliases_ContainExpectedEntries()
    {
        DimensionCatalog.AllNames.Should().Contain(["length", "mass", "temperature", "volume", "area", "speed", "energy", "pressure", "force", "count"]);
        DimensionCatalog.AllNames.Should().NotContain("time");
    }

    [Fact]
    public void GetByName_ReturnsExpectedVector()
    {
        DimensionCatalog.GetByName("force").Vector.Should().Be(new DimensionVector(1, 1, -2, 0, 0, 0, 0));
        DimensionCatalog.GetByName("count").Vector.IsDimensionless.Should().BeTrue();
    }

    [Fact]
    public void TryGetAlias_FindsKnownVector()
    {
        DimensionCatalog.TryGetAlias(new DimensionVector(1, 0, -1, 0, 0, 0, 0), out var alias).Should().BeTrue();
        alias!.Name.Should().Be("speed");
    }
}
