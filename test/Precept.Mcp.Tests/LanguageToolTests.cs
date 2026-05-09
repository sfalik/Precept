using FluentAssertions;
using Precept.Language;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class LanguageToolTests
{
    [Fact]
    public void Language_TokensContainAllActionKeywordsAndTypeKeywords()
    {
        var result = LanguageTool.Language();
        var tokenTexts = result.Tokens
            .Select(token => token.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
            .ToArray();

        var actionKeywords = Actions.All
            .Select(action => action.Token.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct()
            .Cast<string>();

        var typeKeywords = Types.All
            .Select(type => type.Token?.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct()
            .Cast<string>();

        tokenTexts.Should().Contain(actionKeywords);
        tokenTexts.Should().Contain(typeKeywords);
    }

    [Fact]
    public void Language_OperatorsMirrorOperatorCatalog()
    {
        var result = LanguageTool.Language();

        result.Operators.Should().HaveCount(Operators.All.Count);

        var isSet = result.Operators.Should().ContainSingle(op => op.Kind == OperatorKind.IsSet.ToString()).Subject;
        isSet.Tokens.Should().Equal("is", "set");
    }

    [Fact]
    public void Language_FunctionsMirrorFunctionCatalog()
    {
        var result = LanguageTool.Language();

        result.Functions.Should().HaveCount(Functions.All.Count);
        result.Functions.Should().Contain(function => function.Name == "abs");
        result.Functions.Should().Contain(function => function.Name == "now");
    }

    [Fact]
    public void Language_FirePipelineIsPopulatedInExecutionOrder()
    {
        var result = LanguageTool.Language();

        result.FirePipeline.Should().Equal(
            "RowMatching",
            "GuardEvaluation",
            "PreconditionCheck",
            "MutationApplication",
            "InvariantCheck",
            "StateEnsuresCheck",
            "EventEnsuresCheck");
    }

    [Fact]
    public void Language_TokenCountExceedsReasonableFloor()
    {
        var result = LanguageTool.Language();

        result.Tokens.Should().HaveCountGreaterThan(80);
    }
}
