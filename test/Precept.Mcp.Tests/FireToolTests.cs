using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class FireToolTests
{
    private static string ReadSample(string fileName) =>
        File.ReadAllText(Path.Combine(TestPaths.SamplesDir, fileName));

    [Fact]
    public void SingleEvent_TransitionSucceeds()
    {
        var text = ReadSample("maintenance-work-order.precept");
        var data = new Dictionary<string, object?>
        {
            ["RequesterName"] = "Jordan",
            ["Location"] = "Plant 1",
            ["IssueSummary"] = "Leaking valve",
            ["AssignedTechnician"] = null,
            ["EstimatedHours"] = 0.0,
            ["ActualHours"] = 0.0,
            ["Urgent"] = false,
            ["PartsApproved"] = false,
            ["CompletionNote"] = null,
            ["CancellationReason"] = null
        };

        var result = FireTool.Fire(text, "Open", "Assign", data, new() { ["Technician"] = "alice", ["Estimate"] = 3.0 });

        result.Error.Should().BeNull();
        result.Event.Should().Be("Assign");
        result.Outcome.Should().Be("Transition");
        result.FromState.Should().Be("Open");
        result.ToState.Should().Be("Scheduled");
        result.Data!["AssignedTechnician"].Should().Be("alice");
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void UndefinedEvent_ReturnsUndefined()
    {
        var text = ReadSample("maintenance-work-order.precept");

        var result = FireTool.Fire(text, "Draft", @event: "StartWork");

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Undefined");
        result.Violations.Should().NotBeEmpty();
    }

    [Fact]
    public void ConstraintFailure_ReturnsStructuredViolations()
    {
        var text = """
            precept Test
            field Name as string nullable
            in Done assert Name != null because "Done requires a name"
            state Open initial
            state Done
            event Finish
            from Open on Finish -> transition Done
            """;

        var result = FireTool.Fire(text, "Open", @event: "Finish");

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("ConstraintFailure");
        result.Violations.Should().NotBeEmpty();
        result.Violations[0].Message.Should().Contain("Done requires a name");
        result.Violations[0].Source.Kind.Should().Be("state-assertion");
        result.Violations[0].Targets.Should().Contain(t => t.Kind == "field" && t.FieldName == "Name");
    }

    [Fact]
    public void CompileFailure_ReturnsErrorString()
    {
        var result = FireTool.Fire("not valid", "Open", @event: "Foo");

        result.Error.Should().Be("Compilation failed. Use precept_compile to diagnose and fix errors first.");
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void EchoesResolvedDataSnapshot()
    {
        var text = ReadSample("maintenance-work-order.precept");
        var data = new Dictionary<string, object?>
        {
            ["RequesterName"] = "Jordan",
            ["Location"] = "Plant 1",
            ["IssueSummary"] = "Leaking valve"
        };

        var result = FireTool.Fire(text, "Open", "Assign", data, new() { ["Technician"] = "alice", ["Estimate"] = 3.0 });

        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();
        result.Data!["RequesterName"].Should().Be("Jordan");
        result.Data.Should().ContainKey("EstimatedHours");
    }

    [Fact]
    public void Rejection_ReturnsRejectedOutcome()
    {
        var text = ReadSample("maintenance-work-order.precept");
        var data = new Dictionary<string, object?>
        {
            ["RequesterName"] = "Jordan",
            ["Location"] = "Plant 1",
            ["IssueSummary"] = "Leaking valve",
            ["AssignedTechnician"] = "alice",
            ["EstimatedHours"] = 3.0,
            ["ActualHours"] = 0.0,
            ["Urgent"] = true,
            ["PartsApproved"] = false,
            ["CompletionNote"] = null,
            ["CancellationReason"] = null
        };

        var result = FireTool.Fire(text, "Scheduled", "StartWork", data);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Rejected");
        result.Violations.Should().NotBeEmpty();
    }

    [Fact]
    public void NoTransition_ReturnsNoTransitionOutcome()
    {
        var text = ReadSample("maintenance-work-order.precept");
        var data = new Dictionary<string, object?>
        {
            ["RequesterName"] = "Jordan",
            ["Location"] = "Plant 1",
            ["IssueSummary"] = "Leaking valve",
            ["AssignedTechnician"] = "alice",
            ["EstimatedHours"] = 3.0,
            ["ActualHours"] = 0.0,
            ["Urgent"] = false,
            ["PartsApproved"] = false,
            ["CompletionNote"] = null,
            ["CancellationReason"] = null
        };

        var result = FireTool.Fire(text, "Scheduled", "ApproveParts", data);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("NoTransition");
        result.FromState.Should().Be("Scheduled");
        result.Violations.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════
    // Conditional expressions (issue #9)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConditionalSetAssignment_ThenBranch_ProducesCorrectValue()
    {
        var text = """
            precept Test
            field Urgent as boolean default true
            field Priority as number default 0
            state Open initial
            state Done
            event Finish
            from Open on Finish -> set Priority = if Urgent then 10 else 1 -> transition Done
            """;

        var data = new Dictionary<string, object?>
        {
            ["Urgent"] = true,
            ["Priority"] = 0.0
        };

        var result = FireTool.Fire(text, "Open", "Finish", data);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Transition");
        result.ToState.Should().Be("Done");
        result.Data!["Priority"].Should().Be(10.0);
    }

    [Fact]
    public void ConditionalSetAssignment_ElseBranch_ProducesCorrectValue()
    {
        var text = """
            precept Test
            field Urgent as boolean default false
            field Priority as number default 0
            state Open initial
            state Done
            event Finish
            from Open on Finish -> set Priority = if Urgent then 10 else 1 -> transition Done
            """;

        var data = new Dictionary<string, object?>
        {
            ["Urgent"] = false,
            ["Priority"] = 0.0
        };

        var result = FireTool.Fire(text, "Open", "Finish", data);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Transition");
        result.ToState.Should().Be("Done");
        result.Data!["Priority"].Should().Be(1.0);
    }
}
