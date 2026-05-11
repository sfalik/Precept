using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Mcp.Dtos;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

/// <summary>
/// Tests that <see cref="OutcomeMeta.SerializedKind"/> is correctly projected into
/// <see cref="TransitionRowDto.Outcome"/> by <see cref="CompileTool"/>.
///
/// The MCP serialization layer must use catalog-derived <c>SerializedKind</c> values
/// (from <see cref="Outcomes.GetMeta"/>) rather than hardcoded strings. This test file
/// locks the contract at both the catalog level and the round-trip projection level.
///
/// Root causes addressed:
///   BUG-032: 'reject' outcomes not in MCP rows — <c>TransitionRowDto</c> lacked Outcome/RejectMessage.
///   BUG-036: 'no transition'/'reject' indistinguishable — Outcome field required to tell them apart.
/// </summary>
public class OutcomeKindProjectionTests
{
    // ── Catalog: SerializedKind values are stable strings ────────────────────────

    [Fact]
    public void OutcomeCatalog_Transition_SerializedKind_IsTransition()
    {
        Outcomes.GetMeta(OutcomeKind.Transition).SerializedKind.Should().Be("transition",
            because: "the MCP output contract requires the serialized form 'transition' for transition outcomes");
    }

    [Fact]
    public void OutcomeCatalog_NoTransition_SerializedKind_IsNoTransition()
    {
        Outcomes.GetMeta(OutcomeKind.NoTransition).SerializedKind.Should().Be("no transition",
            because: "the MCP output contract requires 'no transition' to distinguish from reject");
    }

    [Fact]
    public void OutcomeCatalog_Reject_SerializedKind_IsReject()
    {
        Outcomes.GetMeta(OutcomeKind.Reject).SerializedKind.Should().Be("reject",
            because: "the MCP output contract requires 'reject' for reject outcomes");
    }

    [Fact]
    public void OutcomeCatalog_AllMembers_HaveNonEmptySerializedKind()
    {
        foreach (var meta in Outcomes.All)
        {
            meta.SerializedKind.Should().NotBeNullOrWhiteSpace(
                because: $"OutcomeKind.{meta.Kind} must have a non-empty SerializedKind for MCP serialization");
        }
    }

    [Fact]
    public void OutcomeCatalog_AllMembers_SerializedKinds_AreDistinct()
    {
        var kinds = Outcomes.All.Select(meta => meta.SerializedKind).ToList();
        kinds.Should().OnlyHaveUniqueItems(
            because: "each OutcomeKind must have a unique SerializedKind string to allow round-trip deserialization");
    }

    // ── Round-trip: CompileTool projects SerializedKind into TransitionRowDto ────

    [Fact]
    public void CompileTool_TransitionOutcome_RowDto_HasOutcomeTransition()
    {
        var definition = CompileDefinition("""
            precept OutcomeTransition
            state Draft initial
            state Approved terminal
            event Approve
            from Draft on Approve -> transition Approved
            """);

        var row = definition.Events.Should().ContainSingle().Subject
            .Rows.Should().ContainSingle().Subject;

        row.Outcome.Should().Be("transition",
            because: "a transition row must project SerializedKind 'transition' from the Outcomes catalog");
        row.ToState.Should().Be("Approved");
    }

    [Fact]
    public void CompileTool_NoTransitionOutcome_RowDto_HasOutcomeNoTransition()
    {
        var definition = CompileDefinition("""
            precept OutcomeNoTransition
            field Counter as integer default 0 writable
            state Active initial
            event Tick
            from Active on Tick
                -> set Counter = Counter + 1
                -> no transition
            """);

        var row = definition.Events.Should().ContainSingle().Subject
            .Rows.Should().ContainSingle().Subject;

        row.Outcome.Should().Be("no transition",
            because: "a no-transition row must project SerializedKind 'no transition' from the catalog");
        row.ToState.Should().BeNull(because: "no-transition rows have no target state");
    }

    [Fact]
    public void CompileTool_RejectOutcome_RowDto_HasOutcomeReject_AndRejectMessage()
    {
        var definition = CompileDefinition("""
            precept OutcomeReject
            field Balance as number default 0 writable
            state Active initial
            event Withdraw(Amount as number)
            from Active on Withdraw when Withdraw.Amount <= Balance
                -> set Balance = Balance - Withdraw.Amount
                -> no transition
            from Active on Withdraw
                -> reject "Insufficient funds"
            """);

        var withdrawEvent = definition.Events.Should().ContainSingle().Subject;
        var rejectRow = withdrawEvent.Rows.Should().Contain(row => row.Outcome == "reject").Subject;

        rejectRow.Outcome.Should().Be("reject",
            because: "a reject row must project SerializedKind 'reject' from the Outcomes catalog");
        rejectRow.RejectMessage.Should().Be("Insufficient funds",
            because: "the reject message must be extracted from the DSL literal (without quotes)");
    }

    [Fact]
    public void CompileTool_MultipleOutcomes_AllProjectCorrectly()
    {
        var definition = CompileDefinition("""
            precept MultiOutcome
            field Balance as number default 100 writable
            state Active initial
            state Closed terminal
            event Withdraw(Amount as number)
            from Active on Withdraw when Withdraw.Amount <= Balance
                -> set Balance = Balance - Withdraw.Amount
                -> no transition
            from Active on Withdraw when Balance == 0
                -> transition Closed
            from Active on Withdraw
                -> reject "Insufficient funds"
            """);

        var rows = definition.Events.Should().ContainSingle().Subject.Rows;
        rows.Should().HaveCount(3);
        rows.Should().ContainSingle(row => row.Outcome == "no transition");
        rows.Should().ContainSingle(row => row.Outcome == "transition");
        rows.Should().ContainSingle(row => row.Outcome == "reject");
    }

    [Fact]
    public void CompileTool_StatelessEventHandler_RowDto_HasOutcomeNoTransition()
    {
        // Stateless event handlers implicitly use NoTransition
        var definition = CompileDefinition("""
            precept StatelessHandler
            field Counter as integer default 0 writable
            event Tick
            on Tick
                -> set Counter = Counter + 1
            """);

        var row = definition.Events.Should().ContainSingle().Subject
            .Rows.Should().ContainSingle().Subject;

        row.Outcome.Should().Be("no transition",
            because: "stateless event handlers must project 'no transition' via Outcomes.GetMeta");
        row.FromStates.Should().Equal(new[] { "*" },
            because: "stateless handlers fire in any state, represented as '*'");
    }

    [Fact]
    public void CompileTool_WildcardTransitionRow_RowDto_FromStates_ContainsStar()
    {
        var definition = CompileDefinition("""
            precept WildcardRow
            field Flag as boolean default false writable
            state Draft initial
            state Done terminal
            event Toggle
            from any on Toggle -> set Flag = true -> no transition
            """);

        var row = definition.Events.Should().ContainSingle().Subject
            .Rows.Should().ContainSingle().Subject;

        row.FromStates.Should().Equal(new[] { "*" },
            because: "'from any' wildcard rows must project as '*' in FromStates");
    }

    // ── DTO structural coverage: TransitionRowDto fields ─────────────────────────

    [Fact]
    public void TransitionRowDto_GuardedRow_ProjectsGuardExpression()
    {
        var definition = CompileDefinition("""
            precept GuardedRow
            field Amount as number default 0 writable
            state Active initial
            event Process
            from Active on Process when Amount > 0 -> no transition
            """);

        var row = definition.Events.Should().ContainSingle().Subject
            .Rows.Should().ContainSingle().Subject;

        row.Guard.Should().Be("Amount > 0",
            because: "a guarded row must project the guard expression into TransitionRowDto.Guard");
    }

    [Fact]
    public void TransitionRowDto_UnguardedRow_GuardIsNull()
    {
        var definition = CompileDefinition("""
            precept UnguardedRow
            state Active initial
            event Reset
            from Active on Reset -> no transition
            """);

        var row = definition.Events.Should().ContainSingle().Subject
            .Rows.Should().ContainSingle().Subject;

        row.Guard.Should().BeNull(
            because: "an unguarded transition row must project Guard as null");
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
