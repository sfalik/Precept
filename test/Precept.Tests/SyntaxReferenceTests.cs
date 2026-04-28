using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class SyntaxReferenceTests
{
    // ── All properties return non-empty values ──────────────────────────────────

    [Fact]
    public void GrammarModel_IsLineOriented()
    {
        SyntaxReference.GrammarModel.Should().Be("line-oriented");
    }

    [Fact]
    public void CommentSyntax_IsHashToEndOfLine()
    {
        SyntaxReference.CommentSyntax.Should().Contain("#");
    }

    [Fact]
    public void IdentifierRules_IsNonEmpty()
    {
        SyntaxReference.IdentifierRules.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void StringLiteralRules_MentionsDoubleQuoted()
    {
        SyntaxReference.StringLiteralRules.Should().Contain("Double-quoted");
    }

    [Fact]
    public void NumberLiteralRules_MentionsIntegers()
    {
        SyntaxReference.NumberLiteralRules.Should().Contain("Integers");
    }

    [Fact]
    public void WhitespaceRules_IsNonEmpty()
    {
        SyntaxReference.WhitespaceRules.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NullNarrowing_IsNonEmpty()
    {
        SyntaxReference.NullNarrowing.Should().NotBeNullOrEmpty();
    }

    // ── ConventionalOrder ───────────────────────────────────────────────────────

    [Fact]
    public void ConventionalOrder_HasTenEntries()
    {
        SyntaxReference.ConventionalOrder.Should().HaveCount(10);
    }

    [Fact]
    public void ConventionalOrder_StartsWithHeader()
    {
        SyntaxReference.ConventionalOrder[0].Should().Be("header");
    }

    [Fact]
    public void ConventionalOrder_EndsWithStateActions()
    {
        SyntaxReference.ConventionalOrder[^1].Should().Be("state actions");
    }

    [Fact]
    public void ConventionalOrder_HasUniqueEntries()
    {
        SyntaxReference.ConventionalOrder.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ConventionalOrder_FieldsBeforeStates()
    {
        var order = SyntaxReference.ConventionalOrder;
        var fieldsIdx = order.ToList().IndexOf("fields");
        var statesIdx = order.ToList().IndexOf("states");
        fieldsIdx.Should().BeLessThan(statesIdx);
    }

    [Fact]
    public void ConventionalOrder_RulesBeforeStates()
    {
        var order = SyntaxReference.ConventionalOrder;
        var rulesIdx = order.ToList().IndexOf("rules");
        var statesIdx = order.ToList().IndexOf("states");
        rulesIdx.Should().BeLessThan(statesIdx);
    }

    [Fact]
    public void ConventionalOrder_EventsBeforeTransitions()
    {
        var order = SyntaxReference.ConventionalOrder;
        var eventsIdx = order.ToList().IndexOf("events");
        var transitionsIdx = order.ToList().IndexOf("transitions");
        eventsIdx.Should().BeLessThan(transitionsIdx);
    }

    // X20 — all 10 section names are specific known values, in order
    [Fact]
    public void ConventionalOrder_ContainsAllKnownSections_InOrder()
    {
        var expected = new[]
        {
            "header", "fields", "rules", "states", "ensures",
            "accessModes", "events", "event ensures", "transitions", "state actions",
        };
        SyntaxReference.ConventionalOrder.Should().BeEquivalentTo(
            expected,
            options => options.WithStrictOrdering(),
            "all 10 section names must match exactly and be in canonical order");
    }

    // X18 ── Content quality tests ──────────────────────────────────────────────

    [Fact]
    public void TypedConstantRules_MentionsSingleQuotes()
    {
        SyntaxReference.TypedConstantRules.Should().Contain("'",
            "typed constants use single-quote delimiters");
    }

    [Fact]
    public void StringLiteralRules_MentionsInterpolationBrace()
    {
        SyntaxReference.StringLiteralRules.Should().Contain("{",
            "string literals support {expr} interpolation syntax");
    }

    // NullNarrowing was updated in N2 nit to use v2 'is set' syntax (not '!= null').
    [Fact]
    public void NullNarrowing_MentionsIsSet()
    {
        SyntaxReference.NullNarrowing.Should().Contain("is set",
            "v2 null narrowing uses 'is set' / 'is not set', not '!= null'");
    }
}
