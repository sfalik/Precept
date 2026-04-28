using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class SlotOrderingDriftTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Pre-parsed injection anchor: slot[0] is always StateTarget or EventTarget
    //  for constructs using In/To/From/On leading tokens.
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly TokenKind[] InjectionLeadingTokens =
        [TokenKind.In, TokenKind.To, TokenKind.From, TokenKind.On];

    [Fact]
    public void PreParsedInjection_AnchorSlotIsAlwaysAtIndex0()
    {
        var injectionConstructs = Constructs.All
            .Where(m => m.Entries.Any(e => InjectionLeadingTokens.Contains(e.LeadingToken)))
            .ToList();

        injectionConstructs.Should().NotBeEmpty("some constructs use injection leading tokens");

        foreach (var meta in injectionConstructs)
        {
            var firstSlotKind = meta.Slots[0].Kind;
            firstSlotKind.Should().BeOneOf(
                [ConstructSlotKind.StateTarget, ConstructSlotKind.EventTarget],
                $"{meta.Kind} uses In/To/From/On but Slots[0] is {firstSlotKind}");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Guard slot position invariants
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PreParsedInjection_GuardSlotPositionMatchesExpectation()
    {
        // TransitionRow: GuardClause at index 2
        var trSlots = Constructs.GetMeta(ConstructKind.TransitionRow).Slots;
        trSlots[2].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "TransitionRow guard is at index 2 (after StateTarget, EventTarget)");

        // AccessMode: GuardClause at index 3 (post-field)
        var amSlots = Constructs.GetMeta(ConstructKind.AccessMode).Slots;
        amSlots[3].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "AccessMode guard is at index 3 (after StateTarget, FieldTarget, AccessModeKeyword)");

        // OmitDeclaration: NO GuardClause at any index
        var omitSlots = Constructs.GetMeta(ConstructKind.OmitDeclaration).Slots;
        omitSlots.Should().NotContain(s => s.Kind == ConstructSlotKind.GuardClause,
            "OmitDeclaration must NEVER have a guard slot");

        // StateEnsure: no GuardClause slot — guard is embedded in the ensure expression
        var seSlots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        seSlots.Should().NotContain(s => s.Kind == ConstructSlotKind.GuardClause,
            "StateEnsure embeds guard in the ensure expression, not as a standalone slot");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Dedicated regression anchor: OmitDeclaration NEVER has guard
    // ════════════════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════════════════
    //  Only recognized scoped constructs use the injection path
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PreParsedInjection_OnlyRecognizedConstructsUseInjectionPath()
    {
        var constructsWithInjection = Constructs.All
            .Where(m => m.Entries.Any(e => InjectionLeadingTokens.Contains(e.LeadingToken)))
            .Select(m => m.Kind)
            .ToHashSet();

        // These are the expected constructs that use In/To/From/On
        var expected = new[]
        {
            ConstructKind.TransitionRow,  // From
            ConstructKind.StateEnsure,    // In, To, From
            ConstructKind.AccessMode,     // In
            ConstructKind.OmitDeclaration,// In
            ConstructKind.StateAction,    // To, From
            ConstructKind.EventEnsure,    // On
            ConstructKind.EventHandler,   // On
        };

        constructsWithInjection.Should().BeEquivalentTo(expected,
            "only these constructs use In/To/From/On leading tokens");

        // Verify slot[0] for all of them is StateTarget or EventTarget
        foreach (var kind in constructsWithInjection)
        {
            var slotKind = Constructs.GetMeta(kind).Slots[0].Kind;
            slotKind.Should().BeOneOf(
                [ConstructSlotKind.StateTarget, ConstructSlotKind.EventTarget],
                $"{kind} should have StateTarget or EventTarget at Slots[0]");
        }
    }
}
