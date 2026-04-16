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
        result.Vocabulary.GrammarKeywords.Should().NotBeEmpty();
        result.Vocabulary.TypeKeywords.Should().NotBeEmpty();
        result.Vocabulary.LiteralKeywords.Should().NotBeEmpty();
        result.Vocabulary.Operators.Should().NotBeEmpty();
    }

    [Fact]
    public void VocabularyDoesNotExposeLegacyConstraintKeywords()
    {
        var result = LanguageTool.Run();

        result.Vocabulary.DeclarationKeywords.Should().NotContain("invariant");
        result.Vocabulary.GrammarKeywords.Should().NotContain("assert");
        result.Vocabulary.ControlKeywords.Should().NotContain("assert");
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
            [TokenCategory.Grammar] = result.Vocabulary.GrammarKeywords,
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

        result.Constraints.Should().HaveCount(DiagnosticCatalog.Constraints.Count);
    }

    [Fact]
    public void ConstraintsIncludeSharedTypeCheckingRange()
    {
        var result = LanguageTool.Run();

        result.Constraints.Select(constraint => constraint.Id)
            .Should().Contain(["C38", "C39", "C40", "C41", "C42", "C43"]);
    }

    [Fact]
    public void ConstraintsIncludeBuiltInFunctionTypeCheckingRange()
    {
        var result = LanguageTool.Run();

        result.Constraints.Select(constraint => constraint.Id)
            .Should().Contain(["C71", "C72", "C73", "C74", "C75", "C76", "C77"]);
    }

    [Fact]
    public void ExpressionScopesHasSixEntries()
    {
        var result = LanguageTool.Run();

        result.ExpressionScopes.Should().HaveCount(6);
    }

    [Fact]
    public void FirePipelineHasSevenStages()
    {
        var result = LanguageTool.Run();

        result.FirePipeline.Should().HaveCount(7);
    }

    [Fact]
    public void OutcomeKindsHasSixEntries()
    {
        var result = LanguageTool.Run();

        result.OutcomeKinds.Should().HaveCount(6);
    }

    [Fact]
    public void LogicalOperatorsAreKeywordForms()
    {
        var result = LanguageTool.Run();
        var symbols = result.Vocabulary.Operators.Select(o => o.Symbol).ToList();

        symbols.Should().Contain("and", "logical AND should be keyword 'and', not '&&'");
        symbols.Should().Contain("or", "logical OR should be keyword 'or', not '||'");
        symbols.Should().Contain("not", "logical NOT should be keyword 'not', not '!'");
        symbols.Should().NotContain("&&", "symbolic && must not appear in the operator inventory");
        symbols.Should().NotContain("||", "symbolic || must not appear in the operator inventory");
        symbols.Should().NotContain("!", "symbolic ! must not appear in the operator inventory");
    }

    [Fact]
    public void TypeKeywordsIncludeIntegerDecimalChoice()
    {
        var result = LanguageTool.Run();

        result.Vocabulary.TypeKeywords.Should().Contain("integer");
        result.Vocabulary.TypeKeywords.Should().Contain("decimal");
        result.Vocabulary.TypeKeywords.Should().Contain("choice");
    }

    [Fact]
    public void ConstraintKeywordsIncludeMaxplacesAndOrdered()
    {
        var result = LanguageTool.Run();

        result.Vocabulary.ConstraintKeywords.Should().Contain("maxplaces");
        result.Vocabulary.ConstraintKeywords.Should().Contain("ordered");
    }

    [Fact]
    public void ConstructsIncludeRoundFunction()
    {
        var result = LanguageTool.Run();

        result.Constructs.Should().Contain(c => c.Description.Contains("built-in function"),
            "function-call construct must be registered in the construct catalog");
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

    // ════════════════════════════════════════════════════════════════════
    // Conditional expressions (issue #9)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ControlKeywordsIncludeConditionalExpressionTokens()
    {
        var result = LanguageTool.Run();

        result.Vocabulary.ControlKeywords.Should().Contain("if");
        result.Vocabulary.ControlKeywords.Should().Contain("then");
        result.Vocabulary.ControlKeywords.Should().Contain("else");
    }

    [Fact]
    public void ConstraintsIncludeConditionalExpressionRange()
    {
        var result = LanguageTool.Run();

        result.Constraints.Select(c => c.Id)
            .Should().Contain(["C78", "C79"]);
    }
}
