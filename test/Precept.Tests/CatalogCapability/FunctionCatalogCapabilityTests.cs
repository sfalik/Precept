using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class FunctionCatalogCapabilityTests
{
    [Fact]
    public void Abs_AllOverloads_ReturnNonnegative_True()
    {
        foreach (var overload in Functions.GetMeta(FunctionKind.Abs).Overloads)
        {
            CatalogCapabilityReflection.GetInstanceValue(overload, "ReturnNonnegative")
                .Should().Be(true);
        }
    }

    [Fact]
    public void Sqrt_Overload_ReturnNonnegative_False()
        => CatalogCapabilityReflection.GetInstanceValue(
                Functions.GetMeta(FunctionKind.Sqrt).Overloads.Single(), "ReturnNonnegative")
            .Should().Be(false);
}
