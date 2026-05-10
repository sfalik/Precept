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
        Actions.All.Should().HaveCount(15);
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
    [InlineData(ActionKind.Append)]
    [InlineData(ActionKind.AppendBy)]
    [InlineData(ActionKind.Insert)]
    [InlineData(ActionKind.Put)]
    [InlineData(ActionKind.EnqueueBy)]
    public void ValueRequired_Actions(ActionKind kind)
    {
        Actions.GetMeta(kind).ValueRequired.Should().BeTrue($"{kind} requires a value expression");
    }

    [Theory]
    [InlineData(ActionKind.Dequeue)]
    [InlineData(ActionKind.Pop)]
    [InlineData(ActionKind.Clear)]
    [InlineData(ActionKind.RemoveAt)]
    [InlineData(ActionKind.DequeueBy)]
    public void NoValueRequired_Actions(ActionKind kind)
    {
        Actions.GetMeta(kind).ValueRequired.Should().BeFalse($"{kind} takes no value");
    }

    // ── Into slot support (derived from ActionShapeMeta) ────────────────────────

    private static bool HasIntoSlot(ActionMeta meta) =>
        Actions.GetShapeMeta(meta.SyntaxShape).Slots.Any(s => s.Role == ActionSlotRole.IntoTarget);

    [Theory]
    [InlineData(ActionKind.Dequeue)]
    [InlineData(ActionKind.Pop)]
    [InlineData(ActionKind.DequeueBy)]
    public void IntoSlot_PresentForIntoCapturingActions(ActionKind kind)
    {
        HasIntoSlot(Actions.GetMeta(kind)).Should().BeTrue($"{kind} supports 'into' clause via slot metadata");
    }

    [Fact]
    public void OnlyDequeuePopAndDequeueBy_HaveIntoSlot()
    {
        var withInto = Actions.All.Where(HasIntoSlot).ToList();
        withInto.Should().HaveCount(3);
        withInto.Select(a => a.Kind).Should()
            .BeEquivalentTo([ActionKind.Dequeue, ActionKind.Pop, ActionKind.DequeueBy]);
    }

    // ── Mutually exclusive: ValueRequired and into slot ─────────────────────────

    [Fact]
    public void ValueRequired_And_IntoSlot_NeverBothTrue()
    {
        foreach (var meta in Actions.All)
        {
            (meta.ValueRequired && HasIntoSlot(meta)).Should().BeFalse(
                $"{meta.Kind} cannot both require a value and support 'into'");
        }
    }

    // ── Applicability: Set actions ──────────────────────────────────────────────

    [Fact]
    public void Add_AppliesToSetAndBag()
    {
        var meta = Actions.GetMeta(ActionKind.Add);
        meta.ApplicableTo.Should().HaveCount(2);
        meta.ApplicableTo.Select(t => t.Kind).Should().BeEquivalentTo([TypeKind.Set, TypeKind.Bag]);
    }

    [Fact]
    public void Remove_AppliesToSetBagListAndLookup()
    {
        var meta = Actions.GetMeta(ActionKind.Remove);
        meta.ApplicableTo.Should().HaveCount(4);
        meta.ApplicableTo.Select(t => t.Kind).Should()
            .BeEquivalentTo([TypeKind.Set, TypeKind.Bag, TypeKind.List, TypeKind.Lookup]);
    }

    // ── Applicability: Queue actions ────────────────────────────────────────────

    [Fact]
    public void Enqueue_AppliesToQueueOnly()
    {
        var meta = Actions.GetMeta(ActionKind.Enqueue);
        meta.ApplicableTo.Should().HaveCount(1);
        meta.ApplicableTo[0].Kind.Should().Be(TypeKind.Queue);
    }

    [Fact]
    public void Dequeue_AppliesToQueueAndQueueBy()
    {
        var meta = Actions.GetMeta(ActionKind.Dequeue);
        meta.ApplicableTo.Should().HaveCount(2);
        meta.ApplicableTo.Select(t => t.Kind).Should()
            .BeEquivalentTo([TypeKind.Queue, TypeKind.QueueBy]);
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
        meta.ApplicableTo.Should().HaveCount(7);

        var typeTargets = meta.ApplicableTo.OfType<TypeTarget>()
            .Where(t => t is not ModifiedTypeTarget)
            .Select(t => t.Kind)
            .ToList();
        typeTargets.Should().BeEquivalentTo(
            [TypeKind.Set, TypeKind.Queue, TypeKind.Stack, TypeKind.Bag, TypeKind.List, TypeKind.QueueBy]);
    }

    [Fact]
    public void Clear_IncludesModifiedTypeTarget_ForOptional()
    {
        var meta = Actions.GetMeta(ActionKind.Clear);
        var modified = meta.ApplicableTo.OfType<ModifiedTypeTarget>().ToList();
        modified.Should().HaveCount(1);
        modified[0].Kind.Should().BeNull("null = any type");
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
    public void TenActions_RequireValue()
    {
        Actions.All.Count(a => a.ValueRequired).Should().Be(10);
    }

    [Fact]
    public void ThreeActions_SupportInto()
    {
        Actions.All.Count(HasIntoSlot).Should().Be(3);
    }

    [Fact]
    public void TwoActions_HasNoValueAndNoInto()
    {
        // clear + removeAt: no value, no into
        var neither = Actions.All
            .Where(a => !a.ValueRequired && !HasIntoSlot(a))
            .ToList();
        neither.Should().HaveCount(2);
        neither.Select(a => a.Kind).Should().BeEquivalentTo([ActionKind.Clear, ActionKind.RemoveAt]);
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

    // ── AllowedIn — all actions allowed in EventDeclaration, StateAction, and TransitionRow ──

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
    public void AllActions_AllowedInStateAction()
    {
        foreach (var meta in Actions.All)
        {
            meta.AllowedIn.Should().Contain(ConstructKind.StateAction,
                $"{meta.Kind} should be allowed in state action hooks");
        }
    }

    [Fact]
    public void AllActions_AllowedInTransitionRow()
    {
        foreach (var meta in Actions.All)
        {
            meta.AllowedIn.Should().Contain(ConstructKind.TransitionRow,
                $"{meta.Kind} should be allowed in transition rows");
        }
    }

    [Fact]
    public void AllActions_AllowedIn_HasThreeContexts()
    {
        foreach (var meta in Actions.All)
        {
            meta.AllowedIn.Should().HaveCount(3,
                $"{meta.Kind} should be allowed in exactly 3 construct contexts");
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
        // Dequeue, Pop, Insert, RemoveAt, and DequeueBy carry non-empty proof requirements; all others default to empty
        var actionsWithRequirements = new HashSet<ActionKind>
        {
            ActionKind.Dequeue, ActionKind.Pop,
            ActionKind.Insert, ActionKind.RemoveAt, ActionKind.DequeueBy,
        };
        foreach (var meta in Actions.All.Where(a => !actionsWithRequirements.Contains(a.Kind)))
        {
            meta.ProofRequirements.Should().BeEmpty(
                $"{meta.Kind} has no proof requirements by default");
        }
    }

    // M4 ── Mutating collection action proof requirements ─────────────────────

    [Theory]
    [InlineData(ActionKind.Dequeue)]
    [InlineData(ActionKind.Pop)]
    public void MutatingCollectionActions_RequireNonEmptyCollection(ActionKind kind)
    {
        var meta = Actions.GetMeta(kind);
        meta.ProofRequirements.Should().HaveCount(1,
            $"{kind} requires the collection to be non-empty");
        var req = meta.ProofRequirements[0].Should().BeOfType<NumericProofRequirement>().Subject;
        req.Comparison.Should().Be(OperatorKind.GreaterThan,
            $"{kind} requires count > 0");
        req.Threshold.Should().Be(0);
    }

    [Fact]
    public void NonMutatingActions_HaveNoProofRequirements()
    {
        var nonMutating = new[]
        {
            ActionKind.Set, ActionKind.Add, ActionKind.Remove,
            ActionKind.Enqueue, ActionKind.Push, ActionKind.Clear,
            ActionKind.Append, ActionKind.AppendBy, ActionKind.Put, ActionKind.EnqueueBy,
        };
        foreach (var kind in nonMutating)
        {
            Actions.GetMeta(kind).ProofRequirements.Should().BeEmpty(
                $"{kind} does not mutate a collection in a way requiring non-empty proof");
        }
    }

    // ── ActionSyntaxShape ────────────────────────────────────────────────────────

    [Fact]
    public void Actions_ActionSyntaxShape_AllMembersAreNonZero()
    {
        var shapes = Enum.GetValues<ActionSyntaxShape>();
        shapes.Should().AllSatisfy(s => ((int)s).Should().BeGreaterThan(0,
            because: "default(ActionSyntaxShape) must not match any named shape"));
    }

    [Fact]
    public void Actions_ByTokenKind_ContainsAllPrimaryActionKinds()
    {
        // ByTokenKind only includes primary actions (PrimaryActionKind == null).
        // Secondary kinds (AppendBy, RemoveAt, EnqueueBy, DequeueBy) share tokens with primaries.
        var primaryActions = Actions.All.Where(m => m.PrimaryActionKind == null).ToList();
        Actions.ByTokenKind.Should().HaveCount(primaryActions.Count);
        foreach (var meta in primaryActions)
        {
            Actions.ByTokenKind.Should().ContainKey(meta.Token.Kind,
                $"ByTokenKind must contain entry for primary action {meta.Kind}");
            Actions.ByTokenKind[meta.Token.Kind].Kind.Should().Be(meta.Kind);
        }
    }
}
