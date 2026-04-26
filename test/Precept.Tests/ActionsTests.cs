using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class ActionsTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_ReturnsForEveryActionKind()
    {
        foreach (var kind in Enum.GetValues<ActionKind>())
        {
            var meta = Actions.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Description.Should().NotBeNullOrEmpty($"{kind} must have a description");
        }
    }

    [Fact]
    public void All_ContainsEveryKindExactlyOnce()
    {
        var expected = Enum.GetValues<ActionKind>().ToHashSet();
        var actual = Actions.All.Select(a => a.Kind).ToHashSet();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void All_IsInDeclarationOrder()
    {
        var kinds = Actions.All.Select(a => (int)a.Kind).ToList();
        kinds.Should().BeInAscendingOrder();
    }

    // ── Count invariant ─────────────────────────────────────────────────────────

    [Fact]
    public void Total_Count()
    {
        Actions.All.Should().HaveCount(8);
    }

    // ── All entries have valid TokenMeta instances ─────────────────────────────

    [Fact]
    public void AllEntries_HaveValidTokenMeta()
    {
        foreach (var meta in Actions.All)
        {
            meta.Token.Should().NotBeNull($"{meta.Kind} must have a Token");
            meta.Token.Kind.Should().BeDefined($"{meta.Kind} references invalid TokenKind");
        }
    }

    // ── ValueRequired flag ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(ActionKind.Set)]
    [InlineData(ActionKind.Add)]
    [InlineData(ActionKind.Remove)]
    [InlineData(ActionKind.Enqueue)]
    [InlineData(ActionKind.Push)]
    public void ValueRequired_Actions(ActionKind kind)
    {
        Actions.GetMeta(kind).ValueRequired.Should().BeTrue($"{kind} requires a value expression");
    }

    [Theory]
    [InlineData(ActionKind.Dequeue)]
    [InlineData(ActionKind.Pop)]
    [InlineData(ActionKind.Clear)]
    public void NoValueRequired_Actions(ActionKind kind)
    {
        Actions.GetMeta(kind).ValueRequired.Should().BeFalse($"{kind} takes no value");
    }

    // ── IntoSupported flag ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(ActionKind.Dequeue)]
    [InlineData(ActionKind.Pop)]
    public void IntoSupported_Actions(ActionKind kind)
    {
        Actions.GetMeta(kind).IntoSupported.Should().BeTrue($"{kind} supports 'into' clause");
    }

    [Fact]
    public void OnlyDequeueAndPop_SupportInto()
    {
        var withInto = Actions.All.Where(a => a.IntoSupported).ToList();
        withInto.Should().HaveCount(2);
        withInto.Select(a => a.Kind).Should()
            .BeEquivalentTo([ActionKind.Dequeue, ActionKind.Pop]);
    }

    // ── Mutually exclusive: ValueRequired and IntoSupported ─────────────────────

    [Fact]
    public void ValueRequired_And_IntoSupported_NeverBothTrue()
    {
        foreach (var meta in Actions.All)
        {
            (meta.ValueRequired && meta.IntoSupported).Should().BeFalse(
                $"{meta.Kind} cannot both require a value and support 'into'");
        }
    }

    // ── Applicability: Set actions ──────────────────────────────────────────────

    [Theory]
    [InlineData(ActionKind.Add)]
    [InlineData(ActionKind.Remove)]
    public void SetCollectionActions_ApplyToSetOnly(ActionKind kind)
    {
        var meta = Actions.GetMeta(kind);
        meta.ApplicableTo.Should().HaveCount(1);
        meta.ApplicableTo[0].Kind.Should().Be(TypeKind.Set);
    }

    // ── Applicability: Queue actions ────────────────────────────────────────────

    [Theory]
    [InlineData(ActionKind.Enqueue)]
    [InlineData(ActionKind.Dequeue)]
    public void QueueActions_ApplyToQueueOnly(ActionKind kind)
    {
        var meta = Actions.GetMeta(kind);
        meta.ApplicableTo.Should().HaveCount(1);
        meta.ApplicableTo[0].Kind.Should().Be(TypeKind.Queue);
    }

    // ── Applicability: Stack actions ────────────────────────────────────────────

    [Theory]
    [InlineData(ActionKind.Push)]
    [InlineData(ActionKind.Pop)]
    public void StackActions_ApplyToStackOnly(ActionKind kind)
    {
        var meta = Actions.GetMeta(kind);
        meta.ApplicableTo.Should().HaveCount(1);
        meta.ApplicableTo[0].Kind.Should().Be(TypeKind.Stack);
    }

    // ── Applicability: set (scalar) ─────────────────────────────────────────────

    [Fact]
    public void Set_AppliesToAnyType()
    {
        var meta = Actions.GetMeta(ActionKind.Set);
        meta.ApplicableTo.Should().BeEmpty("empty = caller validates target type");
    }

    // ── Applicability: clear ────────────────────────────────────────────────────

    [Fact]
    public void Clear_AppliesToCollectionsAndOptional()
    {
        var meta = Actions.GetMeta(ActionKind.Clear);
        meta.ApplicableTo.Should().HaveCount(4);

        var typeTargets = meta.ApplicableTo.OfType<TypeTarget>()
            .Where(t => t is not ModifiedTypeTarget)
            .Select(t => t.Kind)
            .ToList();
        typeTargets.Should().BeEquivalentTo([TypeKind.Set, TypeKind.Queue, TypeKind.Stack]);
    }

    [Fact]
    public void Clear_IncludesModifiedTypeTarget_ForOptional()
    {
        var meta = Actions.GetMeta(ActionKind.Clear);
        var modified = meta.ApplicableTo.OfType<ModifiedTypeTarget>().ToList();
        modified.Should().HaveCount(1);
        modified[0].TypeKindOrNull.Should().BeNull("null = any type");
        modified[0].RequiredModifiers.Should().BeEquivalentTo([ModifierKind.Optional]);
    }

    // ── Token mapping correctness ───────────────────────────────────────────────

    [Theory]
    [InlineData(ActionKind.Set, TokenKind.Set)]
    [InlineData(ActionKind.Add, TokenKind.Add)]
    [InlineData(ActionKind.Remove, TokenKind.Remove)]
    [InlineData(ActionKind.Enqueue, TokenKind.Enqueue)]
    [InlineData(ActionKind.Dequeue, TokenKind.Dequeue)]
    [InlineData(ActionKind.Push, TokenKind.Push)]
    [InlineData(ActionKind.Pop, TokenKind.Pop)]
    [InlineData(ActionKind.Clear, TokenKind.Clear)]
    public void TokenMapping_IsCorrect(ActionKind kind, TokenKind expectedToken)
    {
        Actions.GetMeta(kind).Token.Kind.Should().Be(expectedToken);
    }

    // ── Grouping invariants ─────────────────────────────────────────────────────

    [Fact]
    public void FiveActions_RequireValue()
    {
        Actions.All.Count(a => a.ValueRequired).Should().Be(5);
    }

    [Fact]
    public void TwoActions_SupportInto()
    {
        Actions.All.Count(a => a.IntoSupported).Should().Be(2);
    }

    [Fact]
    public void OneAction_HasNoValueAndNoInto()
    {
        // clear: no value, no into
        var neither = Actions.All
            .Where(a => !a.ValueRequired && !a.IntoSupported)
            .ToList();
        neither.Should().HaveCount(1);
        neither[0].Kind.Should().Be(ActionKind.Clear);
    }

    // ── Token is object reference to Tokens catalog ─────────────────────────────

    [Fact]
    public void AllEntries_TokenMatchesTokensCatalog()
    {
        foreach (var meta in Actions.All)
        {
            var expected = Tokens.GetMeta(meta.Token.Kind);
            meta.Token.Should().Be(expected,
                $"{meta.Kind} Token should be the same object instance from Tokens catalog");
        }
    }

    // ── AllowedIn — all actions currently allowed in EventDeclaration ────────────

    [Fact]
    public void AllActions_AllowedInEventDeclaration()
    {
        foreach (var meta in Actions.All)
        {
            meta.AllowedIn.Should().Contain(ConstructKind.EventDeclaration,
                $"{meta.Kind} should be allowed in event declarations");
        }
    }

    [Fact]
    public void AllActions_AllowedIn_IsNotEmpty()
    {
        foreach (var meta in Actions.All)
        {
            meta.AllowedIn.Should().NotBeEmpty(
                $"{meta.Kind} must declare where it is allowed");
        }
    }

    // ── ProofRequirements — default empty ───────────────────────────────────────

    [Fact]
    public void AllActions_ProofRequirements_DefaultEmpty()
    {
        foreach (var meta in Actions.All)
        {
            meta.ProofRequirements.Should().BeEmpty(
                $"{meta.Kind} has no proof requirements by default");
        }
    }
}
