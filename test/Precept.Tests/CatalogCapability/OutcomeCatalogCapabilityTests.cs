using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class OutcomeCatalogCapabilityTests
{
    [Fact]
    public void Transition_SerializedKind_IsTransition()
        => CatalogCapabilityReflection.GetInstanceValue(Outcomes.GetMeta(OutcomeKind.Transition), "SerializedKind")
            .Should().Be("transition");

    [Fact]
    public void NoTransition_SerializedKind_IsNoTransition()
        => CatalogCapabilityReflection.GetInstanceValue(Outcomes.GetMeta(OutcomeKind.NoTransition), "SerializedKind")
            .Should().Be("no transition");

    [Fact]
    public void Reject_SerializedKind_IsReject()
        => CatalogCapabilityReflection.GetInstanceValue(Outcomes.GetMeta(OutcomeKind.Reject), "SerializedKind")
            .Should().Be("reject");

    [Fact]
    public void AllOutcomes_SerializedKind_Distinct()
    {
        var serializedKinds = Outcomes.All
            .Select(meta => CatalogCapabilityReflection.GetInstanceValue(meta, "SerializedKind"))
            .ToList();

        serializedKinds.Should().OnlyHaveUniqueItems();
    }
}
