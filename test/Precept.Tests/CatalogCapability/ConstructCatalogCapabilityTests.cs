using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class ConstructCatalogCapabilityTests
{
    [Fact]
    public void StateEnsure_SupportsPreVerbWhenGuard_True()
        => CatalogCapabilityReflection.GetInstanceValue(
                Constructs.GetMeta(ConstructKind.StateEnsure), "SupportsPreVerbWhenGuard")
            .Should().Be(true);

    [Fact]
    public void StateAction_SupportsPreVerbWhenGuard_True()
        => CatalogCapabilityReflection.GetInstanceValue(
                Constructs.GetMeta(ConstructKind.StateAction), "SupportsPreVerbWhenGuard")
            .Should().Be(true);

    [Fact]
    public void EventEnsure_SupportsPreVerbWhenGuard_True()
        => CatalogCapabilityReflection.GetInstanceValue(
                Constructs.GetMeta(ConstructKind.EventEnsure), "SupportsPreVerbWhenGuard")
            .Should().Be(true);

    [Fact]
    public void EventHandler_SupportsPostActionEnsure_True()
        => CatalogCapabilityReflection.GetInstanceValue(
                Constructs.GetMeta(ConstructKind.EventHandler), "SupportsPostActionEnsure")
            .Should().Be(true);
}
