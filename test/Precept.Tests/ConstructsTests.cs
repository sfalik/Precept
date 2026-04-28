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
        Constructs.All.Should().HaveCount(11);
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
            .AllowedIn.Should().BeEmpty("AccessMode is a top-level construct — both root-level write and state-scoped 'in' forms appear at precept body level");
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
        Constructs.All.Count(c => c.AllowedIn.Length == 0).Should().Be(8);
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
    [InlineData(ConstructKind.AccessMode,       TokenKind.Write)]
    [InlineData(ConstructKind.StateAction,      TokenKind.To)]
    [InlineData(ConstructKind.EventEnsure,      TokenKind.On)]
    public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)
    {
        Constructs.GetMeta(kind).LeadingToken.Should().Be(expectedToken,
            $"{kind} dispatch must begin with {expectedToken}");
    }
}
