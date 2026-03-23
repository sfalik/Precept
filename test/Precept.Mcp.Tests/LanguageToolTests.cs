using FluentAssertions;
using Precept;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class LanguageToolTests
{
    [Fact]
    public void VocabularyContainsAllCategories()
    {
        var result = LanguageTool.Run();

        result.Vocabulary.ControlKeywords.Should().NotBeEmpty();
        result.Vocabulary.ActionKeywords.Should().NotBeEmpty();
        result.Vocabulary.DeclarationKeywords.Should().NotBeEmpty();
        result.Vocabulary.TypeKeywords.Should().NotBeEmpty();
        result.Vocabulary.LiteralKeywords.Should().NotBeEmpty();
        result.Vocabulary.Operators.Should().NotBeEmpty();
    }

    [Fact]
    public void EveryKeywordInTokenEnumAppearsInAllMatchingVocabularyLists()
    {
        var result = LanguageTool.Run();

        // Map each keyword category to its vocabulary list
        var listByCategory = new Dictionary<TokenCategory, IReadOnlyList<string>>
        {
            [TokenCategory.Control] = result.Vocabulary.ControlKeywords,
            [TokenCategory.Declaration] = result.Vocabulary.DeclarationKeywords,
            [TokenCategory.Action] = result.Vocabulary.ActionKeywords,
            [TokenCategory.Outcome] = result.Vocabulary.OutcomeKeywords,
            [TokenCategory.Type] = result.Vocabulary.TypeKeywords,
            [TokenCategory.Literal] = result.Vocabulary.LiteralKeywords,
        };

        var failures = new List<string>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            var sym = PreceptTokenMeta.GetSymbol(token);
            if (sym is null) continue;

            // Check every category the token carries, not just the primary one
            foreach (var category in PreceptTokenMeta.GetCategories(token))
            {
                if (!listByCategory.TryGetValue(category, out var list)) continue;
                if (!list.Contains(sym))
                    failures.Add($"{token} ({sym}): missing from {category} vocabulary list");
            }
        }

        failures.Should().BeEmpty(
            "every token must appear in the vocabulary list for each of its categories");
    }

    [Fact]
    public void ConstructsMatchCatalogCount()
    {
        var result = LanguageTool.Run();

        result.Constructs.Should().HaveCount(ConstructCatalog.Constructs.Count);
    }

    [Fact]
    public void ConstraintsMatchCatalogCount()
    {
        var result = LanguageTool.Run();

        result.Constraints.Should().HaveCount(ConstraintCatalog.Constraints.Count);
    }

    [Fact]
    public void ConstraintsIncludeSharedTypeCheckingRange()
    {
        var result = LanguageTool.Run();

        result.Constraints.Select(constraint => constraint.Id)
            .Should().Contain(["C38", "C39", "C40", "C41", "C42", "C43"]);
    }

    [Fact]
    public void ExpressionScopesHasFiveEntries()
    {
        var result = LanguageTool.Run();

        result.ExpressionScopes.Should().HaveCount(5);
    }

    [Fact]
    public void FirePipelineHasSixStages()
    {
        var result = LanguageTool.Run();

        result.FirePipeline.Should().HaveCount(6);
    }

    [Fact]
    public void OutcomeKindsHasSixEntries()
    {
        var result = LanguageTool.Run();

        result.OutcomeKinds.Should().HaveCount(6);
    }

    [Fact]
    public void ResultIsSerializable()
    {
        var result = LanguageTool.Run();

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        json.Should().NotBeNullOrEmpty();

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<LanguageResult>(json);
        deserialized.Should().NotBeNull();
        deserialized!.Vocabulary.ControlKeywords.Should().HaveCount(result.Vocabulary.ControlKeywords.Count);
    }
}
