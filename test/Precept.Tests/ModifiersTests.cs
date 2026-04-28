using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class ModifiersTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_ReturnsForEveryModifierKind()
    {
        foreach (var kind in Enum.GetValues<ModifierKind>())
        {
            var meta = Modifiers.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Description.Should().NotBeNullOrEmpty($"{kind} must have a description");
        }
    }

    [Fact]
    public void All_ContainsEveryKindExactlyOnce()
    {
        var expected = Enum.GetValues<ModifierKind>().ToHashSet();
        var actual = Modifiers.All.Select(m => m.Kind).ToHashSet();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void All_IsInDeclarationOrder()
    {
        var kinds = Modifiers.All.Select(m => (int)m.Kind).ToList();
        kinds.Should().BeInAscendingOrder();
    }

    // ── Count invariants ────────────────────────────────────────────────────────

    [Fact]
    public void Total_Count()
    {
        // 15 field + 7 state + 1 event + 3 access + 3 anchor = 29
        Modifiers.All.Should().HaveCount(29);
    }

    [Fact]
    public void FieldModifier_Count()
    {
        Modifiers.All.OfType<FieldModifierMeta>().Should().HaveCount(15);
    }

    [Fact]
    public void StateModifier_Count()
    {
        Modifiers.All.OfType<StateModifierMeta>().Should().HaveCount(7);
    }

    [Fact]
    public void EventModifier_Count()
    {
        Modifiers.All.OfType<EventModifierMeta>().Should().HaveCount(1);
    }

    [Fact]
    public void AccessModifier_Count()
    {
        Modifiers.All.OfType<AccessModifierMeta>().Should().HaveCount(3);
    }

    [Fact]
    public void AnchorModifier_Count()
    {
        Modifiers.All.OfType<AnchorModifierMeta>().Should().HaveCount(3);
    }

    // ── All entries reference valid TokenMeta instances ─────────────────────────

    [Fact]
    public void AllEntries_HaveValidTokenMeta()
    {
        foreach (var meta in Modifiers.All)
        {
            meta.Token.Should().NotBeNull($"{meta.Kind} must have a Token");
            meta.Token.Kind.Should().BeDefined($"{meta.Kind} references invalid TokenKind");
        }
    }

    // ── Field modifier applicability ────────────────────────────────────────────

    [Theory]
    [InlineData(ModifierKind.Nonnegative)]
    [InlineData(ModifierKind.Positive)]
    [InlineData(ModifierKind.Nonzero)]
    [InlineData(ModifierKind.Min)]
    [InlineData(ModifierKind.Max)]
    public void NumericModifiers_ApplyToIntegerDecimalNumber(ModifierKind kind)
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(kind);
        meta.ApplicableTo.Select(t => t.Kind).Should()
            .BeEquivalentTo([TypeKind.Integer, TypeKind.Decimal, TypeKind.Number]);
    }

    [Theory]
    [InlineData(ModifierKind.Notempty)]
    [InlineData(ModifierKind.Minlength)]
    [InlineData(ModifierKind.Maxlength)]
    public void StringModifiers_ApplyToStringOnly(ModifierKind kind)
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(kind);
        meta.ApplicableTo.Should().HaveCount(1);
        meta.ApplicableTo[0].Kind.Should().Be(TypeKind.String);
    }

    [Theory]
    [InlineData(ModifierKind.Mincount)]
    [InlineData(ModifierKind.Maxcount)]
    public void CollectionModifiers_ApplyToSetQueueStack(ModifierKind kind)
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(kind);
        meta.ApplicableTo.Select(t => t.Kind).Should()
            .BeEquivalentTo([TypeKind.Set, TypeKind.Queue, TypeKind.Stack]);
    }

    [Fact]
    public void Maxplaces_ApplyToDecimalOnly()
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(ModifierKind.Maxplaces);
        meta.ApplicableTo.Should().HaveCount(1);
        meta.ApplicableTo[0].Kind.Should().Be(TypeKind.Decimal);
    }

    [Fact]
    public void Ordered_ApplyToChoiceOnly()
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(ModifierKind.Ordered);
        meta.ApplicableTo.Should().HaveCount(1);
        meta.ApplicableTo[0].Kind.Should().Be(TypeKind.Choice);
    }

    [Fact]
    public void Optional_AppliesToAnyType()
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(ModifierKind.Optional);
        meta.ApplicableTo.Should().BeEmpty("empty = applies to all types");
    }

    [Fact]
    public void Default_AppliesToAnyType()
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(ModifierKind.Default);
        meta.ApplicableTo.Should().BeEmpty("empty = applies to all types");
    }

    // ── HasValue flag ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ModifierKind.Default)]
    [InlineData(ModifierKind.Min)]
    [InlineData(ModifierKind.Max)]
    [InlineData(ModifierKind.Minlength)]
    [InlineData(ModifierKind.Maxlength)]
    [InlineData(ModifierKind.Mincount)]
    [InlineData(ModifierKind.Maxcount)]
    [InlineData(ModifierKind.Maxplaces)]
    public void ValueCarrying_HasValueIsTrue(ModifierKind kind)
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(kind);
        meta.HasValue.Should().BeTrue($"{kind} carries a value argument");
    }

    [Theory]
    [InlineData(ModifierKind.Optional)]
    [InlineData(ModifierKind.Ordered)]
    [InlineData(ModifierKind.Nonnegative)]
    [InlineData(ModifierKind.Positive)]
    [InlineData(ModifierKind.Nonzero)]
    [InlineData(ModifierKind.Notempty)]
    public void FlagModifiers_HasValueIsFalse(ModifierKind kind)
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(kind);
        meta.HasValue.Should().BeFalse($"{kind} is a bare flag");
    }

    // ── Subsumption ─────────────────────────────────────────────────────────────

    [Fact]
    public void Positive_SubsumesNonnegativeAndNonzero()
    {
        var meta = (FieldModifierMeta)Modifiers.GetMeta(ModifierKind.Positive);
        meta.Subsumes.Should().BeEquivalentTo(
            [ModifierKind.Nonnegative, ModifierKind.Nonzero]);
    }

    [Fact]
    public void OnlyPositive_HasSubsumptions()
    {
        var withSubsumption = Modifiers.All
            .OfType<FieldModifierMeta>()
            .Where(m => m.Subsumes.Length > 0)
            .ToList();

        withSubsumption.Should().HaveCount(1);
        withSubsumption[0].Kind.Should().Be(ModifierKind.Positive);
    }

    // ── State modifiers ─────────────────────────────────────────────────────────

    [Fact]
    public void Terminal_DisallowsOutgoing()
    {
        var meta = (StateModifierMeta)Modifiers.GetMeta(ModifierKind.Terminal);
        meta.AllowsOutgoing.Should().BeFalse();
    }

    [Fact]
    public void Required_RequiresDominator()
    {
        var meta = (StateModifierMeta)Modifiers.GetMeta(ModifierKind.Required);
        meta.RequiresDominator.Should().BeTrue();
    }

    [Fact]
    public void Irreversible_PreventsBackEdge()
    {
        var meta = (StateModifierMeta)Modifiers.GetMeta(ModifierKind.Irreversible);
        meta.PreventsBackEdge.Should().BeTrue();
    }

    [Theory]
    [InlineData(ModifierKind.InitialState)]
    [InlineData(ModifierKind.Terminal)]
    [InlineData(ModifierKind.Required)]
    [InlineData(ModifierKind.Irreversible)]
    public void StructuralStateModifiers_AreStructuralCategory(ModifierKind kind)
    {
        Modifiers.GetMeta(kind).Category.Should().Be(ModifierCategory.Structural);
    }

    [Theory]
    [InlineData(ModifierKind.Success)]
    [InlineData(ModifierKind.Warning)]
    [InlineData(ModifierKind.Error)]
    public void SemanticStateModifiers_AreSemanticCategory(ModifierKind kind)
    {
        Modifiers.GetMeta(kind).Category.Should().Be(ModifierCategory.Semantic);
    }

    [Fact]
    public void InitialState_DefaultGraphFlags()
    {
        var meta = (StateModifierMeta)Modifiers.GetMeta(ModifierKind.InitialState);
        meta.AllowsOutgoing.Should().BeTrue();
        meta.RequiresDominator.Should().BeFalse();
        meta.PreventsBackEdge.Should().BeFalse();
    }

    // ── Event modifiers ─────────────────────────────────────────────────────────

    [Fact]
    public void InitialEvent_RequiresInitialEventCompatibilityAnalysis()
    {
        var meta = (EventModifierMeta)Modifiers.GetMeta(ModifierKind.InitialEvent);
        meta.RequiredAnalysis.Should().Be(GraphAnalysisKind.InitialEventCompatibility);
    }

    // ── Access modifiers ────────────────────────────────────────────────────────

    [Fact]
    public void Write_IsPresentAndWritable()
    {
        var meta = (AccessModifierMeta)Modifiers.GetMeta(ModifierKind.Write);
        meta.IsPresent.Should().BeTrue();
        meta.IsWritable.Should().BeTrue();
    }

    [Fact]
    public void Read_IsPresentNotWritable()
    {
        var meta = (AccessModifierMeta)Modifiers.GetMeta(ModifierKind.Read);
        meta.IsPresent.Should().BeTrue();
        meta.IsWritable.Should().BeFalse();
    }

    [Fact]
    public void Omit_NotPresentNotWritable()
    {
        var meta = (AccessModifierMeta)Modifiers.GetMeta(ModifierKind.Omit);
        meta.IsPresent.Should().BeFalse();
        meta.IsWritable.Should().BeFalse();
    }

    // ── Anchor modifiers ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ModifierKind.In, AnchorScope.InState)]
    [InlineData(ModifierKind.To, AnchorScope.OnEntry)]
    [InlineData(ModifierKind.From, AnchorScope.OnExit)]
    public void Anchors_HaveCorrectScope(ModifierKind kind, AnchorScope expectedScope)
    {
        var meta = (AnchorModifierMeta)Modifiers.GetMeta(kind);
        meta.Scope.Should().Be(expectedScope);
    }

    // ── initial keyword resolution ──────────────────────────────────────────────

    [Fact]
    public void InitialState_AndInitialEvent_ShareSameTokenMeta()
    {
        var stateMeta = Modifiers.GetMeta(ModifierKind.InitialState);
        var eventMeta = Modifiers.GetMeta(ModifierKind.InitialEvent);
        stateMeta.Token.Kind.Should().Be(TokenKind.Initial);
        eventMeta.Token.Kind.Should().Be(TokenKind.Initial);
    }

    [Fact]
    public void InitialState_IsStateModifierMeta()
    {
        Modifiers.GetMeta(ModifierKind.InitialState).Should().BeOfType<StateModifierMeta>();
    }

    [Fact]
    public void InitialEvent_IsEventModifierMeta()
    {
        Modifiers.GetMeta(ModifierKind.InitialEvent).Should().BeOfType<EventModifierMeta>();
    }

    // ── Category distribution ───────────────────────────────────────────────────

    [Fact]
    public void Category_StructuralCount()
    {
        // 15 field + 4 structural state + 1 event + 3 access + 3 anchor = 26
        Modifiers.All.Count(m => m.Category == ModifierCategory.Structural)
            .Should().Be(26);
    }

    [Fact]
    public void Category_SemanticCount()
    {
        // 3 semantic state modifiers: success, warning, error
        Modifiers.All.Count(m => m.Category == ModifierCategory.Semantic)
            .Should().Be(3);
    }

    [Fact]
    public void Category_NoSeverityYet()
    {
        // No severity modifiers in v2
        Modifiers.All.Count(m => m.Category == ModifierCategory.Severity)
            .Should().Be(0);
    }

    // ── All field modifiers are structural ──────────────────────────────────────

    [Fact]
    public void AllFieldModifiers_AreStructural()
    {
        Modifiers.All.OfType<FieldModifierMeta>()
            .All(m => m.Category == ModifierCategory.Structural)
            .Should().BeTrue("all field modifiers are compile-time structural constraints");
    }

    // ── Token is object reference to Tokens catalog ─────────────────────────────

    [Fact]
    public void AllEntries_TokenMatchesTokensCatalog()
    {
        foreach (var meta in Modifiers.All)
        {
            var expected = Tokens.GetMeta(meta.Token.Kind);
            meta.Token.Should().Be(expected,
                $"{meta.Kind} Token should match the Tokens catalog entry");
        }
    }

    // ── Anchor modifiers carry AnchorTarget ─────────────────────────────────────

    [Fact]
    public void AnchorModifiers_DefaultToEnsureTarget()
    {
        foreach (var anchor in Modifiers.All.OfType<AnchorModifierMeta>())
        {
            anchor.Target.Should().Be(AnchorTarget.Ensure,
                $"{anchor.Kind} should default to AnchorTarget.Ensure");
        }
    }

    [Fact]
    public void AnchorTarget_HasTwoValues()
    {
        Enum.GetValues<AnchorTarget>().Should().HaveCount(2);
    }

    // X15 ── Cross-catalog: ApplicableTo references valid TypeKinds ────────────

    [Fact]
    public void FieldModifiers_ApplicableTo_ReferencesOnlyValidTypeKinds()
    {
        // Empty ApplicableTo means "applies to all types" — skip cross-check for those.
        // Every non-empty TypeTarget.Kind must be findable via Types.GetMeta without throwing.
        foreach (var meta in Modifiers.All.OfType<FieldModifierMeta>())
        {
            foreach (var target in meta.ApplicableTo)
            {
                if (target.Kind.HasValue)
                {
                    var typeMeta = Types.GetMeta(target.Kind.Value);
                    typeMeta.Kind.Should().Be(target.Kind.Value,
                        $"{meta.Kind}.ApplicableTo references TypeKind.{target.Kind.Value} which is not in Types.All");
                }
            }
        }
    }
}
