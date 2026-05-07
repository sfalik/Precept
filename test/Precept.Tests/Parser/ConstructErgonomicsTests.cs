using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests for ParsedConstruct ergonomics helpers (GetSlot, GetRequiredSlot, HasSlot, IsComplete)
/// and ConstructManifest.ByKind index.
/// </summary>
public class ConstructErgonomicsTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  §1. GetSlot helpers
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetSlot_ByKind_ReturnsMatchingSlot()
    {
        var tokens = Lexer.Lex("field amount as decimal");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var typeSlot = field.GetSlot(ConstructSlotKind.TypeExpression);

        typeSlot.Should().NotBeNull();
        typeSlot.Should().BeOfType<TypeExpressionSlot>();
    }

    [Fact]
    public void GetSlot_Generic_ReturnsCastSlot()
    {
        var tokens = Lexer.Lex("field amount as decimal");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var typeSlot = field.GetSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        typeSlot.Should().NotBeNull();
        typeSlot!.TypeRef.Should().BeOfType<SimpleTypeReference>();
    }

    [Fact]
    public void GetSlot_MissingKind_ReturnsNull()
    {
        var tokens = Lexer.Lex("field amount as decimal");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var outcome = field.GetSlot(ConstructSlotKind.Outcome);

        outcome.Should().BeNull();
    }

    [Fact]
    public void GetRequiredSlot_Missing_Throws()
    {
        var tokens = Lexer.Lex("field amount as decimal");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var act = () => field.GetRequiredSlot<OutcomeSlot>(ConstructSlotKind.Outcome);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Outcome*missing*");
    }

    [Fact]
    public void HasSlot_ExistingSlot_ReturnsTrue()
    {
        var tokens = Lexer.Lex("field amount as decimal");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);

        field.HasSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression).Should().BeTrue();
    }

    [Fact]
    public void HasSlot_MissingSlot_ReturnsFalse()
    {
        var tokens = Lexer.Lex("field amount as decimal");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);

        field.HasSlot<OutcomeSlot>(ConstructSlotKind.Outcome).Should().BeFalse();
    }

    [Fact]
    public void GetSlots_ByType_ReturnsAllMatchingSlots()
    {
        var tokens = Lexer.Lex("field amount as decimal nonnegative");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var modifierSlots = field.GetSlots<ModifierListSlot>().ToList();

        modifierSlots.Should().HaveCount(1);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. IsComplete property
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsComplete_FullyParsedConstruct_ReturnsTrue()
    {
        var tokens = Lexer.Lex("field amount as decimal");
        var manifest = Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);

        field.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void IsComplete_ConstructWithMalformedOutcome_ReturnsFalse()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> unknown");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);

        row.IsComplete.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. ConstructManifest.ByKind index
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ByKind_ReturnsCorrectConstructsByKind()
    {
        var tokens = Lexer.Lex(@"
            precept Test
            field a as string
            field b as decimal
            state Draft, Submitted
        ");
        var manifest = Pipeline.Parser.Parse(tokens);

        manifest.ByKind[ConstructKind.FieldDeclaration].Should().HaveCount(2);
        manifest.ByKind[ConstructKind.StateDeclaration].Should().HaveCount(1);
        manifest.ByKind[ConstructKind.PreceptHeader].Should().HaveCount(1);
    }

    [Fact]
    public void ByKind_MissingKind_ReturnsEmpty()
    {
        var tokens = Lexer.Lex("precept Test");
        var manifest = Pipeline.Parser.Parse(tokens);

        manifest.ByKind[ConstructKind.TransitionRow].Should().BeEmpty();
    }

    [Fact]
    public void ByKind_IsCached()
    {
        var tokens = Lexer.Lex("precept Test");
        var manifest = Pipeline.Parser.Parse(tokens);

        var lookup1 = manifest.ByKind;
        var lookup2 = manifest.ByKind;

        ReferenceEquals(lookup1, lookup2).Should().BeTrue();
    }
}
