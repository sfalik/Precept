using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class SlotOrderingDriftTests
{
    private static readonly TokenKind[] ScopedLeadingTokens =
        [TokenKind.In, TokenKind.To, TokenKind.From, TokenKind.On];

    [Fact]
    public void ScopedConstruct_AnchorSlotIsAlwaysAtIndex0()
    {
        var scopedConstructs = Constructs.All
            .Where(m => m.Entries.Any(e => ScopedLeadingTokens.Contains(e.LeadingToken)))
            .ToList();

        scopedConstructs.Should().NotBeEmpty("some constructs use scoped leading tokens");

        foreach (var meta in scopedConstructs)
        {
            var firstSlotKind = meta.Slots[0].Kind;
            firstSlotKind.Should().BeOneOf(
                [ConstructSlotKind.StateTarget, ConstructSlotKind.EventTarget],
                $"{meta.Kind} uses In/To/From/On but Slots[0] is {firstSlotKind}");
        }
    }

    [Fact]
    public void ScopedConstruct_GuardSlotPositionMatchesExpectation()
    {
        Constructs.GetMeta(ConstructKind.TransitionRow).Slots[2].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "TransitionRow keeps its guard after StateTarget and EventTarget");
        Constructs.GetMeta(ConstructKind.StateEnsure).Slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "StateEnsure guard now lives in the slot list immediately after StateTarget");
        Constructs.GetMeta(ConstructKind.StateAction).Slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "StateAction guard now lives in the slot list immediately after StateTarget");
        Constructs.GetMeta(ConstructKind.EventEnsure).Slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "EventEnsure guard now lives in the slot list immediately after EventTarget");
        Constructs.GetMeta(ConstructKind.AccessMode).Slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "AccessMode guard moved ahead of the modify verb and field target");

        Constructs.GetMeta(ConstructKind.OmitDeclaration).Slots.Should().NotContain(s => s.Kind == ConstructSlotKind.GuardClause,
            "OmitDeclaration must never have a guard slot");
    }

    [Fact]
    public void OmitDeclaration_NeverHasGuardAtAnySlotPosition()
    {
        var omitSlots = Constructs.GetMeta(ConstructKind.OmitDeclaration).Slots;

        for (int i = 0; i < omitSlots.Count; i++)
        {
            omitSlots[i].Kind.Should().NotBe(ConstructSlotKind.GuardClause,
                $"OmitDeclaration.Slots[{i}] must not be GuardClause — omit is unconditional");
        }
    }

    [Fact]
    public void ScopedConstruct_OnlyRecognizedConstructsUseScopedLeadingTokens()
    {
        var scopedConstructs = Constructs.All
            .Where(m => m.Entries.Any(e => ScopedLeadingTokens.Contains(e.LeadingToken)))
            .Select(m => m.Kind)
            .ToHashSet();

        var expected = new[]
        {
            ConstructKind.TransitionRow,
            ConstructKind.TransitionRowReject,
            ConstructKind.StateEnsure,
            ConstructKind.AccessMode,
            ConstructKind.OmitDeclaration,
            ConstructKind.StateAction,
            ConstructKind.EventEnsure,
            ConstructKind.EventRow,
            ConstructKind.ConstructionRow,
            ConstructKind.ConstructionRowReject,
        };

        scopedConstructs.Should().BeEquivalentTo(expected,
            "only these constructs use In/To/From/On leading tokens");

        foreach (var kind in scopedConstructs)
        {
            var slotKind = Constructs.GetMeta(kind).Slots[0].Kind;
            slotKind.Should().BeOneOf(
                [ConstructSlotKind.StateTarget, ConstructSlotKind.EventTarget],
                $"{kind} should have StateTarget or EventTarget at Slots[0]");
        }
    }
}
