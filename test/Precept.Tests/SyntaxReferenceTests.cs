using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
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

    [Fact]
    public void CommonPatterns_ComputedField_UsesBackArrowSyntax()
    {
        var computedField = SyntaxReference.CommonPatterns.Single(pattern => pattern.Name == "Computed field");

        computedField.DslSnippet.Should().Contain("<-");
        computedField.DslSnippet.Should().NotContain("->");
    }

    // ── Generic: catalog snippets compile per their declared expectations ───────
    //
    // The contract is declared on each catalog entry via metadata:
    //
    //   CommonPattern.IsFragment  — true: snippet is a documentation fragment
    //     (no enclosing `precept Name` header); not testable standalone.
    //   AntiPattern.IsFragment    — same for BadSnippet + GoodSnippet pair.
    //   AntiPattern.BadCompilesClean — true (default): design anti-pattern; bad
    //     snippet compiles clean but represents bad design. false: compiler-
    //     enforced anti-pattern; bad snippet is expected to produce errors.
    //
    // GoodSnippet always compiles clean for non-fragment AntiPatterns —
    // it is the recommended alternative.

    public static IEnumerable<object[]> SnippetsThatMustCompileClean()
    {
        foreach (var p in SyntaxReference.CommonPatterns.Where(p => !p.IsFragment))
            yield return new object[] { $"CommonPattern: {p.Name}", p.DslSnippet };

        foreach (var p in SyntaxReference.AntiPatterns.Where(p => !p.IsFragment))
        {
            if (p.BadCompilesClean)
                yield return new object[] { $"AntiPattern (Bad, design-only): {p.Name}", p.BadSnippet };
            yield return new object[] { $"AntiPattern (Good): {p.Name}", p.GoodSnippet };
        }
    }

    public static IEnumerable<object[]> SnippetsThatMustHaveErrors()
    {
        foreach (var p in SyntaxReference.AntiPatterns.Where(p => !p.IsFragment && !p.BadCompilesClean))
            yield return new object[] { $"AntiPattern (Bad, compiler-caught): {p.Name}", p.BadSnippet };
    }

    [Theory]
    [MemberData(nameof(SnippetsThatMustCompileClean))]
    public void CatalogSnippet_CompilesClean(string label, string snippet)
    {
        var compilation = Compiler.Compile(snippet);

        var diagnostics = compilation.Diagnostics.Select(d => $"{d.Code}: {d.Message}").ToList();
        compilation.HasErrors.Should().BeFalse(
            $"snippet '{label}' should compile without errors. " +
            $"Diagnostics: [{string.Join(" | ", diagnostics)}]");
    }

    [Theory]
    [MemberData(nameof(SnippetsThatMustHaveErrors))]
    public void CatalogSnippet_HasErrors(string label, string snippet)
    {
        var compilation = Compiler.Compile(snippet);

        compilation.HasErrors.Should().BeTrue(
            $"snippet '{label}' is a compiler-caught anti-pattern and should produce " +
            $"diagnostics — the compiler IS the enforcement.");
    }

    [Fact]
    public void AntiPatterns_SentinelDefaults_RecommendOmitAndTransitionSet()
    {
        var sentinelDefaults = SyntaxReference.AntiPatterns.Single(pattern => pattern.Name == "Sentinel defaults for not-yet-meaningful fields");
        var badCompilation = Compiler.Compile(sentinelDefaults.BadSnippet);
        var goodCompilation = Compiler.Compile(sentinelDefaults.GoodSnippet);

        sentinelDefaults.Description.Should().Contain("absent in earlier states");
        sentinelDefaults.GoodSnippet.Should().Contain("omit ApprovedAmount");
        sentinelDefaults.GoodSnippet.Should().Contain("set ApprovedAmount = Approve.Amount");
        sentinelDefaults.WhyItFails.Should().Contain("transition into a non-omitted state");
        sentinelDefaults.WhyItFails.Should().Contain("`set Field = ...`");
        sentinelDefaults.WhyItFails.Should().NotContain("MustSetOmitToNonOmit");
        sentinelDefaults.WhyItFails.Should().NotContain("D132");
        badCompilation.HasErrors.Should().BeFalse();
        badCompilation.Diagnostics.Should().BeEmpty();
        goodCompilation.HasErrors.Should().BeFalse();
        goodCompilation.Diagnostics.Should().BeEmpty();
    }
}
