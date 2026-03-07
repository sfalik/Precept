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
    public void EveryKeywordInTokenEnumAppears()
    {
        var result = LanguageTool.Run();
        var allKeywords = result.Vocabulary.ControlKeywords
            .Concat(result.Vocabulary.ActionKeywords)
            .Concat(result.Vocabulary.DeclarationKeywords)
            .Concat(result.Vocabulary.OutcomeKeywords)
            .Concat(result.Vocabulary.TypeKeywords)
            .Concat(result.Vocabulary.LiteralKeywords)
            .ToHashSet(StringComparer.Ordinal);

        // Count tokens with keyword-like categories
        var expectedCategories = new[]
        {
            TokenCategory.Control, TokenCategory.Declaration,
            TokenCategory.Action, TokenCategory.Outcome,
            TokenCategory.Type, TokenCategory.Literal
        };

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            var cat = PreceptTokenMeta.GetCategory(token);
            var sym = PreceptTokenMeta.GetSymbol(token);
            if (cat is not null && expectedCategories.Contains(cat.Value) && sym is not null)
            {
                allKeywords.Should().Contain(sym, $"token {token} with symbol '{sym}' should be in vocabulary");
            }
        }
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
    public void OutcomeKindsHasFiveEntries()
    {
        var result = LanguageTool.Run();

        result.OutcomeKinds.Should().HaveCount(5);
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
