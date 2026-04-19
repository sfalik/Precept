using System.Linq;
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
    public void GetClassifiedTokens_PreceptName_IsPreceptName()
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
            t.Type == "preceptName");
    }

    [Fact]
    public void GetClassifiedTokens_RuleOperators_AreStandardOperators()
    {
        const string dsl = """
            precept InvoiceLineItem
            field UnitPrice as number default 0
            field Quantity as number default 1
            field DiscountPercent as number default 0
            rule Quantity >= 1 because "Quantity must be at least 1"
            rule UnitPrice * Quantity - DiscountPercent + 1 <= 100 because "Math should stay numeric"
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == ">=" &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == "*" &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == "-" &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == "+" &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == "<=" &&
            t.Type == "operator");
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

    [Fact]
    public void GetClassifiedTokens_RuleEnsureKeywords_ArePreceptKeywordSemantic()
    {
        const string dsl = """
            precept M
            field Balance as number default 100
            rule Balance >= 0 because "Must be positive"
            state Active initial
            state Done
            in Done ensure Balance > 0 because "Done needs balance"
            event Go
            from Active on Go -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "rule" &&
            t.Type == "preceptKeywordSemantic");

        tokens.Should().Contain(t =>
            t.Text == "ensure" &&
            t.Type == "preceptKeywordSemantic");
    }

    [Fact]
    public void GetClassifiedTokens_StateEditFields_ArePreceptFieldNames_IncludingCommaTail()
    {
        const string dsl = """
            precept M
            field ScheduledDay as number default 0
            field ScheduledMinute as number default 0
            state Scheduled initial
            in Scheduled edit ScheduledDay, ScheduledMinute
            event Save
            from Scheduled on Save -> no transition
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Count(t => t.Text == "ScheduledDay" && t.Type == "preceptFieldName").Should().Be(2);
        tokens.Count(t => t.Text == "ScheduledMinute" && t.Type == "preceptFieldName").Should().Be(2);
    }

    [Fact]
    public void GetClassifiedTokens_Punctuation_IsOperator()
    {
        const string dsl = """
            precept M
            field Priority as choice("Low","High") default "Low"
            field Tags as set of string
            state Active initial
            event Go
            from Active on Go -> no transition
            rule Tags.count >= 0 because "always true"
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "(" &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == ")" &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == "," &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == "->" &&
            t.Type == "operator");

        tokens.Should().Contain(t =>
            t.Text == "." &&
            t.Type == "operator");
    }
}