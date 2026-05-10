using System.Collections.Generic;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class ConstructCatalogCapabilityTests
{
    [Theory]
    [InlineData(ConstructKind.StateEnsure, 1)]
    [InlineData(ConstructKind.StateAction, 1)]
    [InlineData(ConstructKind.EventEnsure, 1)]
    [InlineData(ConstructKind.AccessMode, 1)]
    public void GuardedConstruct_DeclaresGuardClauseAtExpectedSlotIndex(ConstructKind kind, int slotIndex)
    {
        var slotsValue = CatalogCapabilityReflection.GetInstanceValue(Constructs.GetMeta(kind), nameof(ConstructMeta.Slots));
        slotsValue.Should().BeAssignableTo<IReadOnlyList<ConstructSlot>>();

        var slots = (IReadOnlyList<ConstructSlot>)slotsValue!;
        slots[slotIndex].Kind.Should().Be(ConstructSlotKind.GuardClause);
    }

    [Fact]
    public void EventHandler_SupportsPostActionEnsure_True()
        => CatalogCapabilityReflection.GetInstanceValue(
                Constructs.GetMeta(ConstructKind.EventHandler), "SupportsPostActionEnsure")
            .Should().Be(true);
}
