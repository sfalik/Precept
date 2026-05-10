using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class DiagnosticEnricherTests
{
    [Theory]
    [InlineData("Quantty", "Quantity")]
    [InlineData("Quantit", "Quantity")]
    public void Enrichment_UndeclaredField_SuggestsClosestMatch(string typo, string expectedSuggestion)
    {
        var source = $$"""
            precept OrderItem
            field Quantity as number default 0
            state Pending initial terminal
            rule {{typo}} >= 0 because "must be nonnegative"
            """;

        var (diagnostics, suggestions) = Enrich(source);

        diagnostics.Any(d =>
                d.Code is not null &&
                d.Code.Value.String == nameof(DiagnosticCode.UndeclaredField) &&
                d.Message.Contains($"did you mean '{expectedSuggestion}'?"))
            .Should().BeTrue();
        suggestions.Should().NotBeEmpty();
        suggestions.Keys.Should().OnlyContain(key => key.Code == DiagnosticCode.UndeclaredField);
        suggestions.Values.Should().Contain(new SuggestionInfo(expectedSuggestion, typo));
    }

    [Theory]
    [InlineData("Xyz")]
    [InlineData("WayTooFar")]
    public void Enrichment_NoMatchWithin3Edits_NoSuggestion(string typo)
    {
        var source = $$"""
            precept OrderItem
            field Quantity as number default 0
            state Pending initial terminal
            rule {{typo}} >= 0 because "must be nonnegative"
            """;

        var (diagnostics, suggestions) = Enrich(source);

        diagnostics.Any(d =>
                d.Code is not null &&
                d.Code.Value.String == nameof(DiagnosticCode.UndeclaredField) &&
                d.Message == $"Field '{typo}' is not declared")
            .Should().BeTrue();
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public void Enrichment_TiebreakAlphabetical()
    {
        const string source = """
            precept OrderItem
            field Bat as number default 0
            field Cat as number default 0
            state Pending initial terminal
            rule Dat >= 0 because "must be nonnegative"
            """;

        var (diagnostics, suggestions) = Enrich(source);

        diagnostics.Any(d =>
                d.Code is not null &&
                d.Code.Value.String == nameof(DiagnosticCode.UndeclaredField) &&
                d.Message.Contains("did you mean 'Bat'?"))
            .Should().BeTrue();
        suggestions.Should().NotBeEmpty();
        suggestions.Values.Should().Contain(new SuggestionInfo("Bat", "Dat"));
    }

    [Fact]
    public void Enrichment_EmptyPool_NoSuggestion()
    {
        const string source = """
            precept OrderItem
            state Pending initial terminal
            rule Missing >= 0 because "must be nonnegative"
            """;

        var (diagnostics, suggestions) = Enrich(source);

        diagnostics.Any(d =>
                d.Code is not null &&
                d.Code.Value.String == nameof(DiagnosticCode.UndeclaredField) &&
                d.Message == "Field 'Missing' is not declared")
            .Should().BeTrue();
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public void Enrichment_IdenticalName_NoSuggestion()
    {
        const string source = """
            precept OrderItem
            field Quantity as number default 0
            state Pending initial terminal
            rule MIN(Quantity, 0) >= 0 because "must be nonnegative"
            """;

        var (diagnostics, suggestions) = Enrich(source);

        diagnostics.Any(d =>
                d.Code is not null &&
                d.Code.Value.String == nameof(DiagnosticCode.UndeclaredFunction) &&
                d.Message == "'MIN' is not a recognized function")
            .Should().BeTrue();
        suggestions.Should().BeEmpty();
    }

    private static (IReadOnlyList<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic> Diagnostics, IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> Suggestions)
        Enrich(string source) => DiagnosticEnricher.Enrich(Compiler.Compile(source));
}
