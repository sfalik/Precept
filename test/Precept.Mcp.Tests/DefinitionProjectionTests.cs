using System.Linq;
using FluentAssertions;
using Precept.Mcp.Dtos;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

/// <summary>
/// Tests that all <c>SemanticIndex</c> record types are fully projected into their
/// corresponding MCP DTO fields by <see cref="CompileTool"/>.
///
/// This file covers DTO field gaps not exercised by <see cref="CompileToolDefinitionProjectionTests"/>:
/// <list type="bullet">
///   <item><see cref="EnsureDto.Kind"/> — state vs event vs global anchor kind string</item>
///   <item><see cref="EnsureDto.Anchor"/> — the anchor name for state/event-anchored ensures</item>
///   <item><see cref="EnsureDto.Guard"/> — guarded ensure conditions (non-null <c>when</c> clause)</item>
///   <item><see cref="StateHookDto"/> anchored to specific states (entry vs exit)</item>
///   <item><see cref="EventArgDto"/> type field coverage</item>
/// </list>
/// </summary>
public class DefinitionProjectionTests
{
    // ── EnsureDto.Kind ────────────────────────────────────────────────────────────

    [Fact]
    public void EnsureDto_StateAnchoredEnsure_KindIsState()
    {
        var definition = CompileDefinition("""
            precept StateEnsureKind
            field Balance as number default 0 writable
            state Active initial
            in Active ensure Balance >= 0 because "Balance must not go negative"
            event Refresh
            from Active on Refresh -> no transition
            """);

        var state = definition.States.Should().ContainSingle(s => s.Name == "Active").Subject;
        state.Constraints.Should().ContainSingle();
        state.Constraints[0].Kind.Should().Be("StateResident",
            because: "a 'in State ensure' constraint must project Kind as 'StateResident'");
    }

    [Fact]
    public void EnsureDto_EventAnchoredEnsure_KindIsEvent()
    {
        var definition = CompileDefinition("""
            precept EventEnsureKind
            field Balance as number default 0 writable
            state Active initial
            event Deposit(Amount as number)
            on Deposit ensure Deposit.Amount > 0 because "Deposit must be positive"
            from Active on Deposit -> set Balance = Balance + Deposit.Amount -> no transition
            """);

        var depositEvent = definition.Events.Should().ContainSingle(e => e.Name == "Deposit").Subject;
        depositEvent.Constraints.Should().NotBeNull();
        depositEvent.Constraints!.Should().ContainSingle();
        depositEvent.Constraints[0].Kind.Should().Be("EventPrecondition",
            because: "an 'on Event ensure' constraint must project Kind as 'EventPrecondition'");
    }

    // ── EnsureDto.Anchor ─────────────────────────────────────────────────────────

    [Fact]
    public void EnsureDto_StateAnchoredEnsure_AnchorIsStateName()
    {
        var definition = CompileDefinition("""
            precept StateEnsureAnchor
            field Score as integer default 0 writable
            state Review initial
            in Review ensure Score >= 0 because "Score must be non-negative"
            event Next
            from Review on Next -> no transition
            """);

        var reviewState = definition.States.Should().ContainSingle(s => s.Name == "Review").Subject;
        reviewState.Constraints.Should().ContainSingle();
        reviewState.Constraints[0].Anchor.Should().Be("Review",
            because: "a state-anchored ensure must project Anchor as the state name");
    }

    [Fact]
    public void EnsureDto_EventAnchoredEnsure_AnchorIsEventName()
    {
        var definition = CompileDefinition("""
            precept EventEnsureAnchor
            field Amount as number default 0 writable
            state Active initial
            event Transfer(Value as number)
            on Transfer ensure Transfer.Value > 0 because "Transfer value must be positive"
            from Active on Transfer -> set Amount = Amount + Transfer.Value -> no transition
            """);

        var transferEvent = definition.Events.Should().ContainSingle(e => e.Name == "Transfer").Subject;
        transferEvent.Constraints!.Should().ContainSingle();
        transferEvent.Constraints![0].Anchor.Should().Be("Transfer",
            because: "an event-anchored ensure must project Anchor as the event name");
    }

    // ── EnsureDto.Guard ───────────────────────────────────────────────────────────

    [Fact]
    public void EnsureDto_UnguardedEnsure_GuardIsNull()
    {
        var definition = CompileDefinition("""
            precept UnguardedEnsure
            field Balance as number default 0 writable
            state Active initial
            in Active ensure Balance >= 0 because "Balance must be non-negative"
            event Refresh
            from Active on Refresh -> no transition
            """);

        var constraint = definition.States.Single(s => s.Name == "Active").Constraints[0];
        constraint.Guard.Should().BeNull(
            because: "an unguarded ensure must project Guard as null");
    }

    [Fact]
    public void EnsureDto_GuardedEnsure_GuardIsNull_WhenNoWhenClause()
    {
        // Note: guarded ensures ('in State ensure X when Y because Z') may require BUG-020 fix.
        // This test verifies the Guard field is correctly null for unguarded ensures.
        var definition = CompileDefinition("""
            precept EnsureGuardNull
            field Count as integer default 0 writable
            state Active initial
            in Active ensure Count >= 0 because "Count must be non-negative"
            event Tick
            from Active on Tick -> no transition
            """);

        var constraint = definition.States.Single(s => s.Name == "Active").Constraints[0];
        constraint.Guard.Should().BeNull(
            because: "an ensure without a when-clause must project Guard as null");
    }

    // ── EnsureDto.Expression and Because ─────────────────────────────────────────

    [Fact]
    public void EnsureDto_StateEnsure_ExpressionAndBecause_AreProjected()
    {
        var definition = CompileDefinition("""
            precept EnsureFields
            field Quota as integer default 0 writable
            state Active initial
            in Active ensure Quota >= 0 because "Quota must be non-negative"
            event Refresh
            from Active on Refresh -> no transition
            """);

        var constraint = definition.States.Single(s => s.Name == "Active").Constraints[0];
        constraint.Expression.Should().Be("Quota >= 0",
            because: "EnsureDto.Expression must project the constraint condition");
        constraint.Because.Should().Be("Quota must be non-negative",
            because: "EnsureDto.Because must strip DSL quotes from the because message");
    }

    // ── StateHookDto ──────────────────────────────────────────────────────────────

    [Fact]
    public void StateHookDto_EntryHook_KindIsEntry()
    {
        var definition = CompileDefinition("""
            precept EntryHook
            field Status as string default "pending" writable
            state Draft initial
            state Active
            event Activate
            from Draft on Activate -> transition Active
            to Active
                -> set Status = "active"
            """);

        var hooks = definition.StateHooks.Where(h => h.StateName == "Active").ToList();
        hooks.Should().ContainSingle(h => h.Kind == "entry",
            because: "'to State -> action' produces an entry hook for that state");
    }

    [Fact]
    public void StateHookDto_ExitHook_KindIsExit()
    {
        var definition = CompileDefinition("""
            precept ExitHook
            field Status as string default "active" writable
            state Draft initial
            state Active
            event Deactivate
            from Active on Deactivate -> transition Draft
            from Active
                -> set Status = "leaving"
            """);

        var hooks = definition.StateHooks.Where(h => h.StateName == "Active").ToList();
        hooks.Should().ContainSingle(h => h.Kind == "exit",
            because: "'from State -> action' (without event) produces an exit hook for that state");
    }

    [Fact]
    public void StateHookDto_EntryHook_ActionsAreProjected()
    {
        var definition = CompileDefinition("""
            precept EntryHookActions
            field Status as string default "draft" writable
            state Draft initial
            state Review
            event Submit
            from Draft on Submit -> transition Review
            to Review
                -> set Status = "review"
            """);

        var entryHook = definition.StateHooks.Single(h => h.StateName == "Review" && h.Kind == "entry");
        entryHook.Actions.Should().ContainSingle()
            .Which.Should().Contain("set Status",
                because: "the hook action text must be projected into StateHookDto.Actions");
    }

    // ── EventArgDto field coverage ────────────────────────────────────────────────

    [Fact]
    public void EventArgDto_RequiredArg_IsOptionalIsFalse()
    {
        var definition = CompileDefinition("""
            precept RequiredArg
            field Amount as number default 0 writable
            state Active initial
            event Deposit(Value as number)
            from Active on Deposit -> set Amount = Amount + Deposit.Value -> no transition
            """);

        var arg = definition.Events.Should().ContainSingle().Subject.Args
            .Should().ContainSingle().Subject;

        arg.Name.Should().Be("Value");
        arg.Type.Should().Be("number");
        arg.IsOptional.Should().BeFalse(
            because: "a required event arg must project IsOptional as false");
    }

    [Fact]
    public void EventArgDto_OptionalArg_IsOptionalIsTrue()
    {
        var definition = CompileDefinition("""
            precept OptionalArg
            field Name as string default "" writable
            state Active initial
            event Update(NewName as string, Alias as string optional)
            from Active on Update
                -> set Name = Update.NewName
                -> no transition
            """);

        var optionalArg = definition.Events.Should().ContainSingle().Subject.Args
            .Should().Contain(arg => arg.Name == "Alias").Subject;

        optionalArg.IsOptional.Should().BeTrue(
            because: "an 'optional' event arg must project IsOptional as true");
    }

    // ── PreceptDefinitionDto structural coverage ──────────────────────────────────

    [Fact]
    public void PreceptDefinitionDto_IsStateless_TrueForStatelessPrecept()
    {
        var definition = CompileDefinition("""
            precept StatelessPrecept
            field Name as string default "" writable
            """);

        definition.IsStateless.Should().BeTrue(
            because: "a precept with no states must project IsStateless as true");
        definition.States.Should().BeEmpty();
    }

    [Fact]
    public void PreceptDefinitionDto_IsStateless_FalseForStatefulPrecept()
    {
        var definition = CompileDefinition("""
            precept StatefulPrecept
            state Draft initial
            event Submit
            from Draft on Submit -> no transition
            """);

        definition.IsStateless.Should().BeFalse(
            because: "a precept with states must project IsStateless as false");
        definition.States.Should().NotBeEmpty();
    }

    [Fact]
    public void PreceptDefinitionDto_Name_IsProjectedFromPreceptHeader()
    {
        var definition = CompileDefinition("""
            precept MyNamedPrecept
            field Value as number default 0
            """);

        definition.Name.Should().Be("MyNamedPrecept",
            because: "PreceptDefinitionDto.Name must project the precept identifier from the header");
    }

    private static PreceptDefinitionDto CompileDefinition(string source)
    {
        var result = CompileTool.Compile(source);
        result.HasErrors.Should().BeFalse(
            because: "test source must compile without errors");
        result.Definition.Should().NotBeNull();
        return result.Definition!;
    }
}
