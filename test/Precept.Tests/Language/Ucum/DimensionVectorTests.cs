using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class DimensionVectorTests
{
    [Fact]
    public void Multiply_And_Divide_ComposeExpectedVectors()
    {
        var mass = new DimensionVector(0, 1, 0, 0, 0, 0, 0);
        var acceleration = new DimensionVector(1, 0, -2, 0, 0, 0, 0);
        var force = mass.Multiply(acceleration);

        force.Should().Be(new DimensionVector(1, 1, -2, 0, 0, 0, 0));
        force.Divide(mass).Should().Be(acceleration);
    }

    [Fact]
    public void Pow_ComputesExpectedVector()
    {
        new DimensionVector(1, 0, 0, 0, 0, 0, 0).Pow(3)
            .Should().Be(new DimensionVector(3, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void IsDimensionless_UsesZeroVector()
    {
        DimensionVector.None.IsDimensionless.Should().BeTrue();
        new DimensionVector(0, 0, -1, 0, 0, 0, 0).IsDimensionless.Should().BeFalse();
    }
}
