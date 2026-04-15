using FluentAssertions;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptSemanticTokensClassificationTests
{
    [Fact]
    public void GetClassifiedTokens_Comment_IsPreceptComment()
    {
        const string dsl = "# force flashing red until explicitly cleared\nprecept M\nstate Active initial\nevent Go\nfrom Active on Go -> no transition\n";

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "# force flashing red until explicitly cleared" &&
            t.Type == "preceptComment");
    }

    [Fact]
    public void GetClassifiedTokens_BecauseString_IsPreceptMessage()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            rule Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Go
            from Active on Go -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "\"Balance must not go negative\"" &&
            t.Type == "preceptMessage");
    }

    [Fact]
    public void GetClassifiedTokens_RejectString_IsPreceptMessage()
    {
        const string dsl = """
            precept M
            state Active initial
            state Closed
            event Go
            from Active on Go -> reject "Cannot complete this transition"
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "\"Cannot complete this transition\"" &&
            t.Type == "preceptMessage");
    }

    [Fact]
    public void GetClassifiedTokens_DefaultString_IsPreceptValue()
    {
        const string dsl = """
            precept M
            field Note as string default "pending"
            state Active initial
            event Go
            from Active on Go -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "\"pending\"" &&
            t.Type == "preceptValue");
    }

    [Fact]
    public void GetClassifiedTokens_GrammarKeywords_ArePreceptKeywordSemantic()
    {
        const string dsl = """
            precept M
            field Note as string default "pending"
            state Active initial
            event Go with Reason as string
            from any on Go -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "default" &&
            t.Type == "preceptKeywordSemantic");

        tokens.Should().Contain(t =>
            t.Text == "initial" &&
            t.Type == "preceptKeywordSemantic");

        tokens.Should().Contain(t =>
            t.Text == "with" &&
            t.Type == "preceptKeywordSemantic");

        tokens.Should().Contain(t =>
            t.Text == "any" &&
            t.Type == "preceptKeywordSemantic");
    }

    [Fact]
    public void GetClassifiedTokens_ConstraintKeyword_IsPreceptKeywordGrammar()
    {
        const string dsl = """
            precept M
            field Count as number nonnegative
            state Active initial
            event Go
            from Active on Go -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "nonnegative" &&
            t.Type == "preceptKeywordGrammar");
    }

    [Fact]
    public void GetClassifiedTokens_PreceptName_IsPreceptMessage()
    {
        const string dsl = """
            precept ApartmentRentalApplication
            state Active initial
            event Go
            from Active on Go -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "ApartmentRentalApplication" &&
            t.Type == "preceptMessage");
    }

    [Fact]
    public void GetClassifiedTokens_ConstrainedState_GetsModifier()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            state Active initial
            state Closed
            in Active ensure Balance >= 0 because "ok"
            event Go
            from Active on Go -> transition Closed
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "Active" &&
            t.Type == "preceptState" &&
            t.Modifier == "preceptConstrained");
    }

    [Fact]
    public void GetClassifiedTokens_ConditionalKeywords_ArePreceptKeywordSemantic()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            field Label as string default ""
            state Active initial
            event Go with Amount as number
            from Active on Go -> set Label = if Amount > 0 then "positive" else "zero" -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "if" &&
            t.Type == "preceptKeywordSemantic");

        tokens.Should().Contain(t =>
            t.Text == "then" &&
            t.Type == "preceptKeywordSemantic");

        tokens.Should().Contain(t =>
            t.Text == "else" &&
            t.Type == "preceptKeywordSemantic");
    }
}