using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class Track2PhaseAConstructCatalogTests
{
    [Theory]
    [InlineData(ConstructKind.StateEnsure, 1)]
    [InlineData(ConstructKind.StateAction, 1)]
    [InlineData(ConstructKind.EventEnsure, 1)]
    [InlineData(ConstructKind.AccessMode, 1)]
    public void GuardedConstruct_DeclaresGuardClauseAtExpectedSlotIndex(ConstructKind kind, int slotIndex)
        => Constructs.GetMeta(kind).Slots[slotIndex].Kind.Should().Be(ConstructSlotKind.GuardClause);

    [Fact]
    public void EventHandler_SupportsPostActionEnsure_True()
        => Constructs.GetMeta(ConstructKind.EventHandler).SupportsPostActionEnsure.Should().BeTrue();
}
