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

    // ════════════════════════════════════════════════════════════════════
    // Slice 9c: when-guarded constraint sets
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractConstraintSets_WhenGuardedInvariant_FieldStillConstrained()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            field Active as boolean default true
            invariant Balance >= 0 when Active because "Balance must not go negative when active"
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (definition, _) = PreceptParser.ParseWithDiagnostics(dsl);
        var (_, _, fields) = PreceptSemanticTokensHandler.ExtractConstraintSets(definition!);

        fields.Should().Contain("Balance",
            "a when-guarded invariant still constrains its target field for semantic token purposes");
    }

    [Fact]
    public void ExtractConstraintSets_WhenGuardedStateAssert_StateStillConstrained()
    {
        const string dsl = """
            precept M
            field Balance as number default 0
            field Active as boolean default true
            state Open initial
            state Closed
            in Open assert Balance >= 0 when Active because "Balance guard"
            event Go
            from Open on Go -> transition Closed
            """;

        var (definition, _) = PreceptParser.ParseWithDiagnostics(dsl);
        var (states, _, _) = PreceptSemanticTokensHandler.ExtractConstraintSets(definition!);

        states.Should().Contain("Open",
            "a when-guarded state assert still constrains its anchor state for semantic token purposes");
    }

    [Fact]
    public void ExtractConstraintSets_WhenGuardedEventAssert_EventStillConstrained()
    {
        const string dsl = """
            precept M
            state A initial
            event Submit with Amount as number, Priority as number
            on Submit assert Amount > 0 when Priority > 1 because "Amount required for high priority"
            from A on Submit -> no transition
            """;

        var (definition, _) = PreceptParser.ParseWithDiagnostics(dsl);
        var (_, events, _) = PreceptSemanticTokensHandler.ExtractConstraintSets(definition!);

        events.Should().Contain("Submit",
            "a when-guarded event assert still constrains its event for semantic token purposes");
    }
}
