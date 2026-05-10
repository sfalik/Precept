using FluentAssertions;
using Precept.Mcp.Dtos;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class CompileToolDefinitionProjectionTests
{
    [Fact]
    public void Compile_StateHooks_AreProjectedIntoDefinition()
    {
        var definition = CompileDefinition("""
            precept Hooked
            field Status as string default "created" writable
            state Draft initial
            state Active
            event Activate
            from Draft on Activate -> transition Active
            to Active
                -> set Status = "active"
            from Active
                -> set Status = "leaving"
            """);

        definition.StateHooks.Should().HaveCount(2);
        definition.StateHooks.Should().ContainEquivalentOf(new StateHookDto("Active", "entry", ["set Status = \"active\""]));
        definition.StateHooks.Should().ContainEquivalentOf(new StateHookDto("Active", "exit", ["set Status = \"leaving\""]));
    }

    [Fact]
    public void Compile_StatelessEventHandlers_AreProjectedAsRows()
    {
        var definition = CompileDefinition("""
            precept StatelessHandlers
            field Counter as integer default 0 writable
            event Tick
            on Tick
                -> set Counter = Counter + 1
            """);

        var tick = definition.Events.Should().ContainSingle().Subject;
        tick.Rows.Should().ContainSingle();
        tick.Rows[0].FromStates.Should().Equal("*");
        tick.Rows[0].Actions.Should().Equal("set Counter = Counter + 1");
        tick.Rows[0].Outcome.Should().Be("no transition");
    }

    [Fact]
    public void Compile_GuardedRules_ProjectWhenClause()
    {
        var definition = CompileDefinition("""
            precept GuardedRule
            field Amount as number default 0 writable
            field IsPremium as boolean default false writable
            state Active initial
            event Refresh
            from Active on Refresh -> no transition
            rule Amount >= 1000 when IsPremium because "Premium accounts require minimum balance"
            """);

        var rule = definition.Rules.Should().ContainSingle().Subject;
        rule.Expression.Should().Be("Amount >= 1000");
        rule.When.Should().Be("IsPremium");
    }

    [Fact]
    public void Compile_CaseInsensitiveStringFields_RenderTildeStringType()
    {
        var definition = CompileDefinition("""
            precept CaseInsensitiveField
            field Email as ~string default "" writable
            """);

        definition.Fields.Should().ContainSingle().Which.TypeName.Should().Be("~string");
    }

    [Fact]
    public void Compile_CollectionFields_RenderElementAndKeyTypes()
    {
        var definition = CompileDefinition("""
            precept CollectionTypes
            field Tags as set of string
            field Audit as log of string by instant
            field Scores as lookup of string to number
            """);

        definition.Fields.Should().Contain(field => field.Name == "Tags" && field.TypeName == "set of string");
        definition.Fields.Should().Contain(field => field.Name == "Audit" && field.TypeName == "log of string by instant");
        definition.Fields.Should().Contain(field => field.Name == "Scores" && field.TypeName == "lookup of string to number");
    }

    [Fact]
    public void Compile_EventEnsures_AreProjectedUnderEvents()
    {
        var definition = CompileDefinition("""
            precept EventEnsure
            field Balance as number default 0 writable
            state Active initial
            event Deposit(Amount as number)
            on Deposit ensure Deposit.Amount > 0 because "Deposit must be positive"
            from Active on Deposit
                -> set Balance = Balance + Deposit.Amount
                -> no transition
            """);

        var deposit = definition.Events.Should().ContainSingle().Subject;
        deposit.Constraints.Should().NotBeNull();
        deposit.Constraints!.Should().ContainSingle();
        deposit.Constraints[0].Expression.Should().Be("Deposit.Amount > 0");
        deposit.Constraints[0].Because.Should().Be("Deposit must be positive");
    }

    [Fact]
    public void Compile_BecauseMessages_OmitKeywordAndQuotes()
    {
        var definition = CompileDefinition("""
            precept BecauseMessages
            field Balance as number default 0 writable
            state Active initial
            in Active ensure Balance >= 0 because "Balance must be non-negative"
            event Refresh
            from Active on Refresh -> no transition
            rule Balance >= 0 because "Rule message"
            """);

        definition.Rules.Should().ContainSingle().Which.Because.Should().Be("Rule message");
        definition.States.Should().ContainSingle().Which.Constraints.Should().ContainSingle();
        definition.States[0].Constraints[0].Because.Should().Be("Balance must be non-negative");
    }

    [Fact]
    public void Compile_OmitDeclarations_ProjectOmittedFieldsPerState()
    {
        var definition = CompileDefinition("""
            precept OmitFields
            field Amount as number default 0
            field InternalCode as string default ""
            state Draft initial
            state Active
            in Active omit InternalCode
            event Activate
            from Draft on Activate -> transition Active
            """);

        var active = definition.States.Single(state => state.Name == "Active");
        active.OmittedFields.Should().Equal("InternalCode");
    }

    [Fact]
    public void Compile_TransitionRows_ProjectOutcomeAndRejectMessage()
    {
        var definition = CompileDefinition("""
            precept TransitionOutcomes
            field Balance as number default 0 writable
            state Active initial
            event Withdraw(Amount as number)
            from Active on Withdraw when Withdraw.Amount <= Balance
                -> set Balance = Balance - Withdraw.Amount
                -> no transition
            from Active on Withdraw
                -> reject "Insufficient funds"
            """);

        var withdraw = definition.Events.Should().ContainSingle().Subject;
        withdraw.Rows.Should().HaveCount(2);
        withdraw.Rows.Should().Contain(row => row.Outcome == "no transition" && row.RejectMessage == null);
        withdraw.Rows.Should().Contain(row => row.Outcome == "reject" && row.RejectMessage == "Insufficient funds");
    }

    [Fact]
    public void Compile_EventArgs_ProjectOptionality()
    {
        var definition = CompileDefinition("""
            precept OptionalArgs
            field Name as string default "" writable
            state Active initial
            event Update(NewName as string, Alias as string optional)
            from Active on Update
                -> set Name = Update.NewName
                -> no transition
            """);

        var update = definition.Events.Should().ContainSingle().Subject;
        update.Args.Should().Contain(arg => arg.Name == "Alias" && arg.IsOptional);
    }

    [Fact]
    public void Compile_StateAccessModes_ProjectPerStateOverrides()
    {
        var definition = CompileDefinition("""
            precept AccessModes
            field Name as string writable
            field Amount as number
            state Draft initial
            state Active
            in Draft modify Amount editable
            in Active modify Name readonly
            event Activate
            from Draft on Activate -> transition Active
            """);

        var draft = definition.States.Single(state => state.Name == "Draft");
        var active = definition.States.Single(state => state.Name == "Active");

        draft.AccessModes.Should().ContainSingle(mode => mode.FieldName == "Amount" && mode.Mode == "editable");
        active.AccessModes.Should().ContainSingle(mode => mode.FieldName == "Name" && mode.Mode == "readonly");
    }

    [Fact]
    public void Compile_ChoiceFields_ProjectElementTypeAndValues()
    {
        var definition = CompileDefinition("""
            precept ChoiceFields
            field Status as choice of string("Open", "Closed") default "Open" writable
            field Level as choice of integer(1, 2, 3) default 1 writable
            """);

        var status = definition.Fields.Single(field => field.Name == "Status");
        var level = definition.Fields.Single(field => field.Name == "Level");

        status.ChoiceElementType.Should().Be("string");
        status.ChoiceValues.Should().Equal("Open", "Closed");
        level.ChoiceElementType.Should().Be("integer");
        level.ChoiceValues.Should().Equal("1", "2", "3");
    }

    [Fact]
    public void Compile_ValuedModifiers_RenderBoundValues()
    {
        var definition = CompileDefinition("""
            precept ModifierValues
            field Amount as integer min 0 max 100 default 1 writable
            """);

        var field = definition.Fields.Should().ContainSingle().Subject;
        field.Modifiers.Should().Contain("min 0");
        field.Modifiers.Should().Contain("max 100");
        field.Modifiers.Should().Contain("default");
    }

    [Fact]
    public void Compile_StringDefaults_StripDslQuotes()
    {
        var definition = CompileDefinition("""
            precept StringDefaults
            field Greeting as string default "hello" writable
            field Status as choice of string("Open", "Closed") default "Open" writable
            """);

        definition.Fields.Single(field => field.Name == "Greeting").DefaultExpression.Should().Be("hello");
        definition.Fields.Single(field => field.Name == "Status").DefaultExpression.Should().Be("Open");
    }

    private static PreceptDefinitionDto CompileDefinition(string source)
    {
        var result = CompileTool.Compile(source);
        result.HasErrors.Should().BeFalse();
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Severity == "Error");
        result.Definition.Should().NotBeNull();
        return result.Definition!;
    }
}
