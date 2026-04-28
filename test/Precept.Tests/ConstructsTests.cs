using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class ConstructsTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_ReturnsForEveryConstructKind()
    {
        foreach (var kind in Enum.GetValues<ConstructKind>())
        {
            var meta = Constructs.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Name.Should().NotBeNullOrEmpty($"{kind} must have a name");
            meta.Description.Should().NotBeNullOrEmpty($"{kind} must have a description");
            meta.UsageExample.Should().NotBeNullOrEmpty($"{kind} must have an example");
        }
    }

    [Fact]
    public void All_ContainsEveryKindExactlyOnce()
    {
        var expected = Enum.GetValues<ConstructKind>().ToHashSet();
        var actual = Constructs.All.Select(c => c.Kind).ToHashSet();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void All_IsInDeclarationOrder()
    {
        var kinds = Constructs.All.Select(c => (int)c.Kind).ToList();
        kinds.Should().BeInAscendingOrder();
    }

    // ── Count invariant ─────────────────────────────────────────────────────────

    [Fact]
    public void Total_Count()
    {
        Constructs.All.Should().HaveCount(12);
    }

    // ── Top-level constructs (AllowedIn is empty) ───────────────────────────────

    [Theory]
    [InlineData(ConstructKind.PreceptHeader)]
    [InlineData(ConstructKind.FieldDeclaration)]
    [InlineData(ConstructKind.StateDeclaration)]
    [InlineData(ConstructKind.EventDeclaration)]
    [InlineData(ConstructKind.RuleDeclaration)]
    [InlineData(ConstructKind.TransitionRow)]
    [InlineData(ConstructKind.AccessMode)]
    [InlineData(ConstructKind.OmitDeclaration)]
    [InlineData(ConstructKind.EventHandler)]
    public void TopLevelConstructs_HaveEmptyAllowedIn(ConstructKind kind)
    {
        Constructs.GetMeta(kind).AllowedIn.Should().BeEmpty(
            $"{kind} is a top-level construct (AllowedIn = [])");
    }

    // ── Nested constructs ───────────────────────────────────────────────────────

    [Fact]
    public void StateEnsure_AllowedInStateDeclaration()
    {
        Constructs.GetMeta(ConstructKind.StateEnsure)
            .AllowedIn.Should().BeEquivalentTo([ConstructKind.StateDeclaration]);
    }

    [Fact]
    public void AccessMode_IsTopLevel()
    {
        Constructs.GetMeta(ConstructKind.AccessMode)
            .AllowedIn.Should().BeEmpty("AccessMode is a top-level construct — the 'in State' form appears at precept body level");
    }

    [Fact]
    public void OmitDeclaration_IsTopLevel()
    {
        Constructs.GetMeta(ConstructKind.OmitDeclaration)
            .AllowedIn.Should().BeEmpty("OmitDeclaration is a top-level construct — 'in State omit Field' appears at precept body level");
    }

    [Fact]
    public void StateAction_AllowedInStateDeclaration()
    {
        Constructs.GetMeta(ConstructKind.StateAction)
            .AllowedIn.Should().BeEquivalentTo([ConstructKind.StateDeclaration]);
    }

    [Fact]
    public void EventEnsure_AllowedInEventDeclaration()
    {
        Constructs.GetMeta(ConstructKind.EventEnsure)
            .AllowedIn.Should().BeEquivalentTo([ConstructKind.EventDeclaration]);
    }

    // ── Nesting counts ──────────────────────────────────────────────────────────

    [Fact]
    public void TopLevel_Count()
    {
        Constructs.All.Count(c => c.AllowedIn.Length == 0).Should().Be(9);
    }

    [Fact]
    public void Nested_Count()
    {
        Constructs.All.Count(c => c.AllowedIn.Length > 0).Should().Be(3);
    }

    [Fact]
    public void NestedInState_Count()
    {
        Constructs.All.Count(c => c.AllowedIn.Contains(ConstructKind.StateDeclaration))
            .Should().Be(2, "StateEnsure, StateAction");
    }

    [Fact]
    public void NestedInEvent_Count()
    {
        Constructs.All.Count(c => c.AllowedIn.Contains(ConstructKind.EventDeclaration))
            .Should().Be(1, "EventEnsure only");
    }

    // ── AllowedIn references are valid ConstructKinds ───────────────────────────

    [Fact]
    public void AllowedIn_ReferencesValidConstructKinds()
    {
        var validKinds = new HashSet<ConstructKind>(Enum.GetValues<ConstructKind>());
        foreach (var meta in Constructs.All)
        {
            foreach (var parent in meta.AllowedIn)
            {
                validKinds.Should().Contain(parent,
                    $"{meta.Kind}.AllowedIn references invalid ConstructKind {parent}");
            }
        }
    }

    // ── No construct is nested inside itself ────────────────────────────────────

    [Fact]
    public void NoConstruct_AllowedInSelf()
    {
        foreach (var meta in Constructs.All)
        {
            meta.AllowedIn.Should().NotContain(meta.Kind,
                $"{meta.Kind} cannot be nested inside itself");
        }
    }

    // ── Names are unique ────────────────────────────────────────────────────────

    [Fact]
    public void Names_AreUnique()
    {
        var names = Constructs.All.Select(c => c.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
    }

    // M9 ── ConstructMeta.Slots ──────────────────────────────────────────────

    [Fact]
    public void AllConstructs_HaveSlots()
    {
        foreach (var meta in Constructs.All)
        {
            meta.Slots.Should().NotBeEmpty(
                $"{meta.Kind} must have at least one slot");
        }
    }

    [Theory]
    [InlineData(ConstructKind.FieldDeclaration)]
    [InlineData(ConstructKind.StateDeclaration)]
    [InlineData(ConstructKind.EventDeclaration)]
    [InlineData(ConstructKind.TransitionRow)]
    public void KeyConstructs_HaveMinimumSlotCount(ConstructKind kind)
    {
        Constructs.GetMeta(kind).Slots.Should().HaveCountGreaterThanOrEqualTo(2,
            $"{kind} is a complex construct and needs at least 2 slots");
    }

    [Fact]
    public void AccessMode_HasGuardClauseAsOptional()
    {
        var slots = Constructs.GetMeta(ConstructKind.AccessMode).Slots;

        var guardSlot = slots.Should().ContainSingle(s => s.Kind == ConstructSlotKind.GuardClause).Subject;
        guardSlot.IsRequired.Should().BeFalse("guards are optional on access modes");
        slots.Last().Kind.Should().Be(ConstructSlotKind.GuardClause,
            "guard clause must be the final slot in the access mode sequence");
    }

    [Fact]
    public void AccessMode_SlotOrder_StateTarget_FieldTarget_AccessModeKeyword_Guard()
    {
        var slots = Constructs.GetMeta(ConstructKind.AccessMode).Slots.ToArray();
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget,       "first slot is state target");
        slots[1].Kind.Should().Be(ConstructSlotKind.FieldTarget,        "second slot is field target (after consumed 'modify' verb)");
        slots[2].Kind.Should().Be(ConstructSlotKind.AccessModeKeyword,  "third slot is the readonly|editable adjective");
        slots[3].Kind.Should().Be(ConstructSlotKind.GuardClause,        "fourth slot is optional guard");
    }

    [Fact]
    public void OmitDeclaration_HasNoGuardClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.OmitDeclaration).Slots;
        slots.Should().NotContain(s => s.Kind == ConstructSlotKind.GuardClause,
            "omit is unconditional — no when clause");
        slots.Should().HaveCount(2, "omit has exactly StateTarget + FieldTarget");
    }

    [Fact]
    public void TransitionRow_HasGuardClauseAndActionChainAsOptional()
    {
        var slots = Constructs.GetMeta(ConstructKind.TransitionRow).Slots;

        var guardSlot = slots.Should().ContainSingle(s => s.Kind == ConstructSlotKind.GuardClause).Subject;
        guardSlot.IsRequired.Should().BeFalse("guards are optional in transition rows");

        var actionSlot = slots.Should().ContainSingle(s => s.Kind == ConstructSlotKind.ActionChain).Subject;
        actionSlot.IsRequired.Should().BeFalse("action chains are optional in transition rows");
    }

    [Fact]
    public void TransitionRow_HasRequiredOutcomeSlot()
    {
        var slots = Constructs.GetMeta(ConstructKind.TransitionRow).Slots;
        var outcomeSlot = slots.Should().ContainSingle(s => s.Kind == ConstructSlotKind.Outcome).Subject;
        outcomeSlot.IsRequired.Should().BeTrue("every transition row must specify an outcome");
    }

    // ── LeadingToken regression anchors ────────────────────────────────────────

    [Theory]
    [InlineData(ConstructKind.PreceptHeader,    TokenKind.Precept)]
    [InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]
    [InlineData(ConstructKind.StateDeclaration, TokenKind.State)]
    [InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]
    [InlineData(ConstructKind.RuleDeclaration,  TokenKind.Rule)]
    [InlineData(ConstructKind.TransitionRow,    TokenKind.From)]
    [InlineData(ConstructKind.StateEnsure,      TokenKind.In)]
    [InlineData(ConstructKind.AccessMode,       TokenKind.In)]
    [InlineData(ConstructKind.OmitDeclaration,  TokenKind.In)]
    [InlineData(ConstructKind.StateAction,      TokenKind.To)]
    [InlineData(ConstructKind.EventEnsure,      TokenKind.On)]
    public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)
    {
        Constructs.GetMeta(kind).PrimaryLeadingToken.Should().Be(expectedToken,
            $"{kind} dispatch must begin with {expectedToken}");
    }

    // ── Slice 1.2: PrimaryLeadingToken bridge ──────────────────────────────────

    [Theory]
    [InlineData(ConstructKind.PreceptHeader,    TokenKind.Precept)]
    [InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]
    [InlineData(ConstructKind.StateDeclaration, TokenKind.State)]
    [InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]
    [InlineData(ConstructKind.RuleDeclaration,  TokenKind.Rule)]
    [InlineData(ConstructKind.TransitionRow,    TokenKind.From)]
    [InlineData(ConstructKind.StateEnsure,      TokenKind.In)]
    [InlineData(ConstructKind.AccessMode,       TokenKind.In)]
    [InlineData(ConstructKind.OmitDeclaration,  TokenKind.In)]
    [InlineData(ConstructKind.StateAction,      TokenKind.To)]
    [InlineData(ConstructKind.EventEnsure,      TokenKind.On)]
    [InlineData(ConstructKind.EventHandler,     TokenKind.On)]
    public void PrimaryLeadingToken_MatchesExpectedToken(ConstructKind kind, TokenKind expectedToken)
    {
        Constructs.GetMeta(kind).PrimaryLeadingToken.Should().Be(expectedToken);
    }

    [Fact]
    public void Entries_IsNotEmpty_ForAllConstructs()
    {
        foreach (var meta in Constructs.All)
        {
            meta.Entries.Should().NotBeEmpty($"{meta.Kind} must have at least one DisambiguationEntry");
        }
    }

    // ── Slice 1.4: Entries, slots, disambiguation ──────────────────────────────

    [Fact]
    public void AllConstructsHaveAtLeastOneEntry()
    {
        foreach (var meta in Constructs.All)
        {
            meta.Entries.Length.Should().BeGreaterThan(0,
                $"{meta.Kind} must have at least one entry");
        }
    }

    [Fact]
    public void LeadingTokenSlot_OnlyUsedWhenLeadingTokenIsAlsoSlotContent()
    {
        foreach (var meta in Constructs.All)
        {
            foreach (var entry in meta.Entries)
            {
                if (entry.LeadingTokenSlot is { } slotKind)
                {
                    meta.Slots.Should().Contain(s => s.Kind == slotKind,
                        $"{meta.Kind} entry for {entry.LeadingToken} has LeadingTokenSlot={slotKind} but no matching slot");
                }
            }
        }
    }

    [Fact]
    public void RuleDeclaration_HasRuleExpressionSlot()
    {
        var slots = Constructs.GetMeta(ConstructKind.RuleDeclaration).Slots;
        slots.Should().HaveCount(3, "RuleDeclaration: [RuleExpression, GuardClause(opt), BecauseClause]");
        slots[0].Kind.Should().Be(ConstructSlotKind.RuleExpression);
        slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause);
        slots[1].IsRequired.Should().BeFalse();
        slots[2].Kind.Should().Be(ConstructSlotKind.BecauseClause);
    }

    [Fact]
    public void AccessMode_HasCorrectSlotSequence()
    {
        var slots = Constructs.GetMeta(ConstructKind.AccessMode).Slots;
        slots.Should().HaveCount(4, "AccessMode: [StateTarget, FieldTarget, AccessModeKeyword, GuardClause(opt)]");
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget);
        slots[1].Kind.Should().Be(ConstructSlotKind.FieldTarget);
        slots[2].Kind.Should().Be(ConstructSlotKind.AccessModeKeyword);
        slots[3].Kind.Should().Be(ConstructSlotKind.GuardClause);
        slots[3].IsRequired.Should().BeFalse();
    }

    [Fact]
    public void OmitDeclaration_HasCorrectSlotSequence()
    {
        var slots = Constructs.GetMeta(ConstructKind.OmitDeclaration).Slots;
        slots.Should().HaveCount(2, "OmitDeclaration: [StateTarget, FieldTarget]");
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget);
        slots[1].Kind.Should().Be(ConstructSlotKind.FieldTarget);
    }

    [Fact]
    public void OmitDeclaration_HasNoGuardSlot()
    {
        Constructs.GetMeta(ConstructKind.OmitDeclaration).Slots
            .Should().NotContain(s => s.Kind == ConstructSlotKind.GuardClause,
                "omit is unconditional — OmitDeclaration must not have a guard slot");
    }

    [Theory]
    [InlineData(ConstructKind.StateEnsure,      3)]
    [InlineData(ConstructKind.AccessMode,        1)]
    [InlineData(ConstructKind.OmitDeclaration,   1)]
    [InlineData(ConstructKind.StateAction,       2)]
    [InlineData(ConstructKind.EventEnsure,       1)]
    [InlineData(ConstructKind.EventHandler,      1)]
    [InlineData(ConstructKind.TransitionRow,     1)]
    public void DisambiguatedConstructs_HaveCorrectEntryCount(ConstructKind kind, int expectedCount)
    {
        Constructs.GetMeta(kind).Entries.Length.Should().Be(expectedCount,
            $"{kind} should have {expectedCount} disambiguation entries");
    }

    [Fact]
    public void AccessMode_DisambiguationTokens_ContainModifyOnly()
    {
        var entries = Constructs.GetMeta(ConstructKind.AccessMode).Entries;
        var inEntry = entries.Should().ContainSingle(e => e.LeadingToken == TokenKind.In).Subject;
        inEntry.DisambiguationTokens.Should().NotBeNull();
        inEntry.DisambiguationTokens!.Value.Should().BeEquivalentTo([TokenKind.Modify],
            "AccessMode disambiguates with Modify only — Omit belongs to OmitDeclaration");
    }

    [Fact]
    public void OmitDeclaration_DisambiguationTokens_ContainOmitOnly()
    {
        var entries = Constructs.GetMeta(ConstructKind.OmitDeclaration).Entries;
        var inEntry = entries.Should().ContainSingle(e => e.LeadingToken == TokenKind.In).Subject;
        inEntry.DisambiguationTokens.Should().NotBeNull();
        inEntry.DisambiguationTokens!.Value.Should().BeEquivalentTo([TokenKind.Omit],
            "OmitDeclaration disambiguates with Omit only — Modify belongs to AccessMode");
    }

    [Fact]
    public void AllConstructs_UsageExample_IsNotNullOrEmpty()
    {
        foreach (var meta in Constructs.All)
        {
            meta.UsageExample.Should().NotBeNullOrEmpty(
                $"{meta.Kind} must have a usage example");
        }
    }

    // ── Slice 1.5: Derived indexes ─────────────────────────────────────────────

    [Fact]
    public void EveryLeadingTokenMapsToAtLeastOneConstruct()
    {
        var allLeadingTokens = Constructs.All
            .SelectMany(m => m.Entries)
            .Select(e => e.LeadingToken)
            .Distinct();

        foreach (var token in allLeadingTokens)
        {
            Constructs.ByLeadingToken.Should().ContainKey(token,
                $"token {token} appears as a leading token but is missing from ByLeadingToken index");
        }
    }

    [Fact]
    public void LeadingTokens_ContainsAllExpectedTokens()
    {
        var expected = new[]
        {
            TokenKind.Field, TokenKind.State, TokenKind.Event, TokenKind.Rule,
            TokenKind.From, TokenKind.In, TokenKind.To, TokenKind.On, TokenKind.Precept
        };

        foreach (var token in expected)
        {
            Constructs.LeadingTokens.Should().Contain(token, $"{token} should be a leading token");
        }
    }

    [Fact]
    public void LeadingTokens_DoesNotContainRetiredTokens()
    {
        // Write was retired from access mode context in B4
        Constructs.LeadingTokens.Should().NotContain(TokenKind.Writable,
            "Writable is not a leading token for any construct");
    }

    [Theory]
    [InlineData(TokenKind.In,   3)]
    [InlineData(TokenKind.To,   2)]
    [InlineData(TokenKind.From, 3)]
    [InlineData(TokenKind.On,   2)]
    public void SharedLeadingTokens_HaveCorrectCandidateCount(TokenKind token, int expectedCount)
    {
        Constructs.ByLeadingToken[token].Length.Should().Be(expectedCount,
            $"token {token} should map to {expectedCount} construct candidates");
    }

    [Theory]
    [InlineData(TokenKind.Precept)]
    [InlineData(TokenKind.Field)]
    [InlineData(TokenKind.State)]
    [InlineData(TokenKind.Event)]
    [InlineData(TokenKind.Rule)]
    public void UniqueLeadingTokens_HaveSingleCandidate(TokenKind token)
    {
        Constructs.ByLeadingToken[token].Length.Should().Be(1,
            $"token {token} should uniquely identify a single construct");
    }

    [Fact]
    public void LeadingTokens_Count_IsCorrect()
    {
        Constructs.LeadingTokens.Count.Should().Be(9,
            "9 distinct leading tokens: Precept, Field, State, Event, Rule, From, In, To, On");
    }
}
