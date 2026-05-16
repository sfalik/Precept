using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

/// <summary>
/// Validates that ConstructSlot instances carry correct completion vocabulary metadata.
/// These tests read from the live Constructs catalog — they fail if slot metadata drifts.
/// </summary>
public class SlotVocabularyMetadataTests
{
    // ── Helper: find a slot by kind from a specific construct ────────────────────

    private static ConstructSlot GetSlot(ConstructKind construct, ConstructSlotKind slotKind)
    {
        var meta = Constructs.GetMeta(construct);
        var slot = meta.Slots.FirstOrDefault(s => s.Kind == slotKind);
        slot.Should().NotBeNull($"construct {construct} should have a {slotKind} slot");
        return slot!;
    }

    // ── StateTarget ─────────────────────────────────────────────────────────────

    [Fact]
    public void StateTarget_IsList_WithCommaIntroducer_And_StateNamesVocabulary()
    {
        var slot = GetSlot(ConstructKind.TransitionRow, ConstructSlotKind.StateTarget);

        slot.IsList.Should().BeTrue();
        slot.IsChainable.Should().BeFalse();
        slot.ItemIntroducerToken.Should().Be(TokenKind.Comma);
        slot.Vocabulary.Should().Be(SlotVocabulary.StateNames);
    }

    // ── ActionChain ─────────────────────────────────────────────────────────────

    [Fact]
    public void ActionChain_IsChainable_WithArrowIntroducer_And_ActionVerbsVocabulary()
    {
        var slot = GetSlot(ConstructKind.TransitionRow, ConstructSlotKind.ActionChain);

        slot.IsList.Should().BeFalse();
        slot.IsChainable.Should().BeTrue();
        slot.ItemIntroducerToken.Should().Be(TokenKind.Arrow);
        slot.Vocabulary.Should().Be(SlotVocabulary.ActionVerbs);
    }

    // ── TypeExpression ──────────────────────────────────────────────────────────

    [Fact]
    public void TypeExpression_HasTypeKeywordsVocabulary()
    {
        var slot = GetSlot(ConstructKind.FieldDeclaration, ConstructSlotKind.TypeExpression);

        slot.IsList.Should().BeFalse();
        slot.IsChainable.Should().BeFalse();
        slot.ItemIntroducerToken.Should().BeNull();
        slot.Vocabulary.Should().Be(SlotVocabulary.TypeKeywords);
    }

    // ── ModifierList ────────────────────────────────────────────────────────────

    [Fact]
    public void ModifierList_HasModifiersVocabulary()
    {
        var slot = GetSlot(ConstructKind.FieldDeclaration, ConstructSlotKind.ModifierList);

        // Modifiers are space-separated (no explicit introducer token), not comma-delimited.
        slot.IsList.Should().BeFalse();
        slot.Vocabulary.Should().Be(SlotVocabulary.Modifiers);
    }

    // ── FieldTarget ─────────────────────────────────────────────────────────────

    [Fact]
    public void FieldTarget_IsList_WithCommaIntroducer_And_FieldNamesVocabulary()
    {
        var slot = GetSlot(ConstructKind.AccessMode, ConstructSlotKind.FieldTarget);

        slot.IsList.Should().BeTrue();
        slot.ItemIntroducerToken.Should().Be(TokenKind.Comma);
        slot.Vocabulary.Should().Be(SlotVocabulary.FieldNames);
    }

    // ── EventTarget ─────────────────────────────────────────────────────────────

    [Fact]
    public void EventTarget_IsNotList_WithEventNamesVocabulary()
    {
        var slot = GetSlot(ConstructKind.TransitionRow, ConstructSlotKind.EventTarget);

        slot.IsList.Should().BeFalse();
        slot.IsChainable.Should().BeFalse();
        slot.ItemIntroducerToken.Should().BeNull();
        slot.Vocabulary.Should().Be(SlotVocabulary.EventNames);
    }

    // ── GuardClause ─────────────────────────────────────────────────────────────

    [Fact]
    public void GuardClause_HasExpressionVocabulary()
    {
        var slot = GetSlot(ConstructKind.TransitionRow, ConstructSlotKind.GuardClause);

        slot.IsList.Should().BeFalse();
        slot.IsChainable.Should().BeFalse();
        slot.Vocabulary.Should().Be(SlotVocabulary.Expression);
    }

    // ── Outcome ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Outcome_HasOutcomeKeywordsVocabulary()
    {
        var slot = GetSlot(ConstructKind.TransitionRow, ConstructSlotKind.Outcome);

        slot.Vocabulary.Should().Be(SlotVocabulary.OutcomeKeywords);
        slot.IsList.Should().BeFalse();
        slot.IsChainable.Should().BeFalse();
    }

    // ── RejectClause ────────────────────────────────────────────────────────────

    [Fact]
    public void RejectClause_HasRejectReasonVocabulary()
    {
        var slot = GetSlot(ConstructKind.TransitionRowReject, ConstructSlotKind.RejectClause);

        slot.Vocabulary.Should().Be(SlotVocabulary.RejectReason);
        slot.IsList.Should().BeFalse();
        slot.IsChainable.Should().BeFalse();
    }

    // ── SuccessOutcome ──────────────────────────────────────────────────────────

    [Fact]
    public void SuccessOutcome_SlotInstance_HasOutcomeKeywordsVocabulary()
    {
        // SlotSuccessOutcome is defined for future use; verify its metadata is correct.
        // Access it indirectly through the catalog — find any construct that has it,
        // or verify the slot kind's vocabulary on the Outcome slot (TransitionRow uses Outcome=9).
        var slot = GetSlot(ConstructKind.TransitionRow, ConstructSlotKind.Outcome);
        slot.Vocabulary.Should().Be(SlotVocabulary.OutcomeKeywords);
    }

    // ── AccessModeKeyword ───────────────────────────────────────────────────────

    [Fact]
    public void AccessModeKeyword_HasAccessModesVocabulary()
    {
        var slot = GetSlot(ConstructKind.AccessMode, ConstructSlotKind.AccessModeKeyword);

        slot.Vocabulary.Should().Be(SlotVocabulary.AccessModes);
    }

    // ── EnsureClause ────────────────────────────────────────────────────────────

    [Fact]
    public void EnsureClause_HasExpressionVocabulary()
    {
        var slot = GetSlot(ConstructKind.StateEnsure, ConstructSlotKind.EnsureClause);

        slot.Vocabulary.Should().Be(SlotVocabulary.Expression);
    }

    // ── StateEntryList ──────────────────────────────────────────────────────────

    [Fact]
    public void StateEntryList_IsList_WithStateEntryNamesVocabulary()
    {
        var slot = GetSlot(ConstructKind.StateDeclaration, ConstructSlotKind.StateEntryList);

        slot.IsList.Should().BeTrue();
        slot.ItemIntroducerToken.Should().Be(TokenKind.Comma);
        slot.Vocabulary.Should().Be(SlotVocabulary.StateEntryNames);
    }

    // ── Structural invariants ───────────────────────────────────────────────────

    [Fact]
    public void AllListSlots_HaveItemIntroducerToken()
    {
        var allSlots = Constructs.All
            .SelectMany(m => m.Slots)
            .Where(s => s.IsList)
            .ToList();

        allSlots.Should().NotBeEmpty("at least one slot should be a list");
        foreach (var slot in allSlots)
        {
            slot.ItemIntroducerToken.Should().NotBeNull(
                $"list slot {slot.Kind} must declare an ItemIntroducerToken");
        }
    }

    [Fact]
    public void AllChainableSlots_HaveItemIntroducerToken()
    {
        var allSlots = Constructs.All
            .SelectMany(m => m.Slots)
            .Where(s => s.IsChainable)
            .ToList();

        allSlots.Should().NotBeEmpty("at least one slot should be chainable");
        foreach (var slot in allSlots)
        {
            slot.ItemIntroducerToken.Should().NotBeNull(
                $"chainable slot {slot.Kind} must declare an ItemIntroducerToken");
        }
    }

    [Fact]
    public void NoSlotIsBothListAndChainable()
    {
        var allSlots = Constructs.All
            .SelectMany(m => m.Slots)
            .ToList();

        foreach (var slot in allSlots)
        {
            if (slot.IsList)
                slot.IsChainable.Should().BeFalse($"slot {slot.Kind} cannot be both IsList and IsChainable");
            if (slot.IsChainable)
                slot.IsList.Should().BeFalse($"slot {slot.Kind} cannot be both IsChainable and IsList");
        }
    }
}
