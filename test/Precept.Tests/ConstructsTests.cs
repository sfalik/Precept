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
            meta.Example.Should().NotBeNullOrEmpty($"{kind} must have an example");
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
    [InlineData(ConstructKind.StatelessHook)]
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
    public void AccessMode_AllowedInStateDeclaration()
    {
        Constructs.GetMeta(ConstructKind.AccessMode)
            .AllowedIn.Should().BeEquivalentTo([ConstructKind.StateDeclaration]);
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
        Constructs.All.Count(c => c.AllowedIn.Length == 0).Should().Be(7);
    }

    [Fact]
    public void Nested_Count()
    {
        Constructs.All.Count(c => c.AllowedIn.Length > 0).Should().Be(4);
    }

    [Fact]
    public void NestedInState_Count()
    {
        Constructs.All.Count(c => c.AllowedIn.Contains(ConstructKind.StateDeclaration))
            .Should().Be(3, "StateEnsure, AccessMode, StateAction");
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
}
