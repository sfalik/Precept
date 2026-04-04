using FluentAssertions;
using Precept;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptSemanticTokensConstraintTests
{
    [Fact]
    public void ExtractConstraintSets_StateInInAssertBlock_IsConstrained()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            state Active initial
            state Inactive
            in Active assert Balance >= 0 because "Balance must not go negative"
            event Go
            from Active on Go -> transition Inactive
            """;

        var (definition, _) = PreceptParser.ParseWithDiagnostics(dsl);
        var (states, _, _) = PreceptSemanticTokensHandler.ExtractConstraintSets(definition!);

        states.Should().Contain("Active");
        states.Should().NotContain("Inactive");
    }

    [Fact]
    public void ExtractConstraintSets_UnconstrainedState_NotInSet()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;

        var (definition, _) = PreceptParser.ParseWithDiagnostics(dsl);
        var (states, _, _) = PreceptSemanticTokensHandler.ExtractConstraintSets(definition!);

        states.Should().BeEmpty();
    }

    [Fact]
    public void ExtractConstraintSets_EventWithOnAssert_IsConstrained()
    {
        const string dsl = """
            precept M
            state A initial
            event Deposit with Amount as number
            on Deposit assert Amount > 0 because "Amount must be positive"
            from A on Deposit -> no transition
            """;

        var (definition, _) = PreceptParser.ParseWithDiagnostics(dsl);
        var (_, events, _) = PreceptSemanticTokensHandler.ExtractConstraintSets(definition!);

        events.Should().Contain("Deposit");
    }

    [Fact]
    public void ExtractConstraintSets_FieldInInvariant_IsGuarded()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            invariant Balance >= 0 because "Balance must not go negative"
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (definition, _) = PreceptParser.ParseWithDiagnostics(dsl);
        var (_, _, fields) = PreceptSemanticTokensHandler.ExtractConstraintSets(definition!);

        fields.Should().Contain("Balance");
    }

    [Fact]
    public void BuildConstraintSets_ParseError_ReturnsEmptySets()
    {
        const string badDsl = "this is not valid precept syntax !!!";

        var (states, events, fields) = PreceptSemanticTokensHandler.BuildConstraintSets(badDsl);

        states.Should().BeEmpty();
        events.Should().BeEmpty();
        fields.Should().BeEmpty();
    }
}
