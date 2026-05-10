using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class OperatorCatalogCapabilityTests
{
    [Fact]
    public void Or_StaticResultType_IsBoolean()
        => CatalogCapabilityReflection.GetInstanceValue(Operators.GetMeta(OperatorKind.Or), "StaticResultType")
            .Should().Be(TypeKind.Boolean);

    [Fact]
    public void And_StaticResultType_IsBoolean()
        => CatalogCapabilityReflection.GetInstanceValue(Operators.GetMeta(OperatorKind.And), "StaticResultType")
            .Should().Be(TypeKind.Boolean);

    [Fact]
    public void Not_StaticResultType_IsBoolean()
        => CatalogCapabilityReflection.GetInstanceValue(Operators.GetMeta(OperatorKind.Not), "StaticResultType")
            .Should().Be(TypeKind.Boolean);

    [Fact]
    public void Contains_StaticResultType_IsBoolean()
        => CatalogCapabilityReflection.GetInstanceValue(Operators.GetMeta(OperatorKind.Contains), "StaticResultType")
            .Should().Be(TypeKind.Boolean);

    [Fact]
    public void IsSet_StaticResultType_IsBoolean()
        => CatalogCapabilityReflection.GetInstanceValue(Operators.GetMeta(OperatorKind.IsSet), "StaticResultType")
            .Should().Be(TypeKind.Boolean);

    [Fact]
    public void LookupAccess_ResultTypePolicy_IsLookupValueType()
        => CatalogCapabilityReflection.GetInstanceValue(Operators.GetMeta(OperatorKind.LookupAccess), "ResultTypePolicy")
            .Should().NotBeNull()
            .And.Subject.ToString().Should().Be("LookupValueType");

    [Fact]
    public void Plus_StaticResultType_IsNull()
        => CatalogCapabilityReflection.GetInstanceValue(Operators.GetMeta(OperatorKind.Plus), "StaticResultType")
            .Should().BeNull();
}
