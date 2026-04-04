using FluentAssertions;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptSemanticTokensClassificationTests
{
    [Fact]
    public void GetClassifiedTokens_BecauseString_IsPreceptMessage()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            invariant Balance >= 0 because "Balance must not go negative"
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
            in Active assert Balance >= 0 because "ok"
            event Go
            from Active on Go -> transition Closed
            """;

        var tokens = PreceptSemanticTokensHandler.GetClassifiedTokens(dsl);

        tokens.Should().Contain(t =>
            t.Text == "Active" &&
            t.Type == "preceptState" &&
            t.Modifier == "preceptConstrained");
    }
}