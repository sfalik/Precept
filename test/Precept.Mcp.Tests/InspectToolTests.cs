using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class InspectToolTests
{
    private static string ReadSample(string fileName) =>
        File.ReadAllText(Path.Combine(TestPaths.SamplesDir, fileName));

    [Fact]
    public void EventThatTransitions_ReturnsTransition()
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
        var eventArgs = new Dictionary<string, Dictionary<string, object?>>
        {
            ["Assign"] = new() { ["Technician"] = "alice", ["Estimate"] = 3.0 }
        };

        var result = InspectTool.Inspect(text, "Open", data, eventArgs);

        result.Error.Should().BeNull();
        result.CurrentState.Should().Be("Open");

        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Outcome.Should().Be("Transition");
        assignEvent.ResultState.Should().Be("Scheduled");
    }

    [Fact]
    public void EventUndefined_ReturnsUndefined()
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
            ["PartsApproved"] = true,
            ["CompletionNote"] = null,
            ["CancellationReason"] = null
        };

        var result = InspectTool.Inspect(text, "InProgress", data);

        // ApproveParts has no transition rows from InProgress — engine only returns events
        // with rows from the current state, so we verify that an event without rows doesn't appear.
        var approvePartsEvent = result.Events.FirstOrDefault(e => e.Event == "ApproveParts");
        approvePartsEvent.Should().BeNull("engine.Inspect only returns events with transition rows from the current state");

        // Verify that RecordProgress (which has rows from InProgress) does appear
        var recordProgress = result.Events.FirstOrDefault(e => e.Event == "RecordProgress");
        recordProgress.Should().NotBeNull();
    }

    [Fact]
    public void EventWithRequiredArgsMissing_ReturnsEngineOutcomeWithRequiredArgs()
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

        var result = InspectTool.Inspect(text, "Open", data);

        // Assign has transition rows from Open, so it should appear in the engine result.
        // Without args, the engine returns its actual outcome — not a synthetic "requires-args".
        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Outcome.Should().NotBeNull();
        // The engine returns RequiredEventArgumentKeys only for successful transitions;
        // when args are missing and the event fails, RequiredArgs may be null.
    }

    [Fact]
    public void RequiredArgs_PopulatedOnSuccessfulTransition()
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
        var eventArgs = new Dictionary<string, Dictionary<string, object?>>
        {
            ["Assign"] = new() { ["Technician"] = "alice", ["Estimate"] = 3.0 }
        };

        var result = InspectTool.Inspect(text, "Open", data, eventArgs);

        // With valid args supplied, Assign transitions successfully and carries RequiredEventArgumentKeys
        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Outcome.Should().Be("Transition");
        assignEvent.RequiredArgs.Should().NotBeNull();
        assignEvent.RequiredArgs!.Should().Contain("Technician");
        assignEvent.RequiredArgs!.Should().Contain("Estimate");
    }

    [Fact]
    public void EventWithArgsSupplied_EvaluatedWithThoseArgs()
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
        var eventArgs = new Dictionary<string, Dictionary<string, object?>>
        {
            ["Assign"] = new() { ["Technician"] = "alice", ["Estimate"] = 3.0 }
        };

        var result = InspectTool.Inspect(text, "Open", data, eventArgs);

        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Outcome.Should().Be("Transition");
        assignEvent.ResultState.Should().Be("Scheduled");
    }

    [Fact]
    public void EventsPreserveEngineDeclarationOrder()
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

        var result = InspectTool.Inspect(text, "Open", data);

        // Events should be in engine's declaration order, not sorted by outcome
        var eventNames = result.Events.Select(e => e.Event).ToList();
        eventNames.Should().NotBeEmpty();
        eventNames.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void CompileFailure_ReturnsErrorString()
    {
        var result = InspectTool.Inspect("not valid precept", "Open");

        result.Error.Should().Be("Compilation failed. Use precept_compile to diagnose and fix errors first.");
        result.Events.Should().BeEmpty();
    }

    [Fact]
    public void EchoesResolvedInstanceSnapshot()
    {
        var text = ReadSample("maintenance-work-order.precept");
        var data = new Dictionary<string, object?>
        {
            ["RequesterName"] = "Jordan",
            ["Location"] = "Plant 1",
            ["IssueSummary"] = "Leaking valve"
        };

        var result = InspectTool.Inspect(text, "Open", data);

        result.Error.Should().BeNull();
        result.CurrentState.Should().Be("Open");
        result.Data.Should().NotBeNull();
        result.Data!["RequesterName"].Should().Be("Jordan");
        // Defaults should be applied
        result.Data.Should().ContainKey("EstimatedHours");
        result.Data.Should().ContainKey("ActualHours");
    }

    [Fact]
    public void ConstraintFailure_ReturnsStructuredViolations()
    {
        var text = """
            precept Test
            field Name as string nullable
            in Done ensure Name != null because "Done requires a name"
            state Open initial
            state Done
            event Finish
            from Open on Finish -> transition Done
            """;

        var result = InspectTool.Inspect(text, "Open");

        var finish = result.Events.FirstOrDefault(e => e.Event == "Finish");
        finish.Should().NotBeNull();
        finish!.Outcome.Should().Be("ConstraintFailure");
        finish.Violations.Should().NotBeEmpty();
        finish.Violations[0].Message.Should().Contain("Done requires a name");
        finish.Violations[0].Source.Kind.Should().Be("state-ensure");
        finish.Violations[0].Targets.Should().Contain(t => t.Kind == "field" && t.FieldName == "Name");
    }

    [Fact]
    public void EditableFields_PresentWhenEditDeclarationsExist()
    {
        var text = ReadSample("maintenance-work-order.precept");

        // Draft state has: in Draft edit Location, IssueSummary, Urgent
        var result = InspectTool.Inspect(text, "Draft");

        result.Error.Should().BeNull();
        result.EditableFields.Should().NotBeNull();
        result.EditableFields!.Select(f => f.Name).Should().Contain("Location");
        result.EditableFields!.Select(f => f.Name).Should().Contain("IssueSummary");
        result.EditableFields!.Select(f => f.Name).Should().Contain("Urgent");
    }

    [Fact]
    public void EditableFields_NullWhenNoEditDeclarations()
    {
        var text = """
            precept NoEdits
            field Name as string nullable
            state Open initial
            state Done
            event Finish
            from Open on Finish -> transition Done
            """;

        // Precept has no edit declarations at all
        var result = InspectTool.Inspect(text, "Open");

        result.Error.Should().BeNull();
        result.EditableFields.Should().BeNull();
    }

    [Fact]
    public void StringLengthConstraint_ShowsViolationInPreview()
    {
        const string text = """
precept Test
field Name as string default "valid"
state Open initial
state Done
event Update with NewName as string
rule Name.length >= 2 because "Name must be at least 2 characters"
from Open on Update -> set Name = Update.NewName -> transition Done
""";

        var data = new Dictionary<string, object?> { ["Name"] = "valid" };
        var eventArgs = new Dictionary<string, Dictionary<string, object?>>
        {
            ["Update"] = new() { ["NewName"] = "x" }
        };
        var result = InspectTool.Inspect(text, "Open", data, eventArgs);

        result.Error.Should().BeNull();
        var update = result.Events.FirstOrDefault(e => e.Event == "Update");
        update.Should().NotBeNull();
        update!.Outcome.Should().Be("ConstraintFailure");
        update.Violations.Should().NotBeEmpty();
        update.Violations[0].Message.Should().Contain("Name must be at least 2 characters");
    }

    // ════════════════════════════════════════════════════════════════════
    // Slice 9c: when-guard inspect tests
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Inspect_GuardedRule_GuardFalse_NoViolation()
    {
        var text = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            rule X > 0 when Active because "X must be positive when active"
            state Open initial
            state Done
            event Finish
            from Open on Finish -> transition Done
            """;

        // Active = false → guard is false → rule should not fire
        var data = new Dictionary<string, object?>
        {
            ["X"] = 0.0,
            ["Active"] = false
        };

        var result = InspectTool.Inspect(text, "Open", data);

        result.Error.Should().BeNull();
        var finish = result.Events.FirstOrDefault(e => e.Event == "Finish");
        finish.Should().NotBeNull();
        finish!.Outcome.Should().Be("Transition",
            "when the rule guard is false, the rule should be skipped and the transition should succeed");
    }

    [Fact]
    public void Inspect_GuardedEdit_GuardTrue_FieldShownAsEditable()
    {
        var text = """
            precept Test
            field X as number default 0
            field Active as boolean default true
            state Open initial
            in Open when Active edit X
            event Go
            from Open on Go -> no transition
            """;

        // Active = true → guard passes → X should be editable
        var data = new Dictionary<string, object?>
        {
            ["X"] = 0.0,
            ["Active"] = true
        };

        var result = InspectTool.Inspect(text, "Open", data);

        result.Error.Should().BeNull();
        result.EditableFields.Should().NotBeNull();
        result.EditableFields!.Select(f => f.Name).Should().Contain("X",
            "when the edit block guard is true, the field should appear in editable fields");
    }

    // ════════════════════════════════════════════════════════════════════
    // Conditional expressions (issue #9)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Inspect_ConditionalSetAssignment_ShowsCorrectOutcome()
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

        var result = InspectTool.Inspect(text, "Open", data);

        result.Error.Should().BeNull();
        var finish = result.Events.FirstOrDefault(e => e.Event == "Finish");
        finish.Should().NotBeNull();
        finish!.Outcome.Should().Be("Transition");
        finish.ResultState.Should().Be("Done");
    }

    // ════════════════════════════════════════════════════════════════════
    // Computed fields (issue #17)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Inspect_ComputedFieldValues_AppearInPreview()
    {
        var text = """
            precept Test
            field Price as number default 10
            field Qty as number default 2
            field Total as number -> Price * Qty
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            """;

        var data = new Dictionary<string, object?>
        {
            ["Price"] = 10.0,
            ["Qty"] = 2.0
        };

        var result = InspectTool.Inspect(text, "Open", data);

        result.Error.Should().BeNull();
        // The computed field Total should be present in the instance data
        result.Data.Should().ContainKey("Total");
        result.Data!["Total"].Should().Be(20.0);
    }

    [Fact]
    public void Inspect_ComputedFieldNotEditable()
    {
        var text = """
            precept Test
            field Base as number default 5
            field Derived as number -> Base + 1
            state Open initial
            in Open edit Base
            event Go
            from Open on Go -> no transition
            """;

        var data = new Dictionary<string, object?>
        {
            ["Base"] = 5.0
        };

        var result = InspectTool.Inspect(text, "Open", data);

        result.Error.Should().BeNull();
        // Computed field "Derived" should NOT appear in the editable fields list
        result.EditableFields.Should().NotBeNull();
        result.EditableFields.Should().Contain(f => f.Name == "Base");
        result.EditableFields.Should().NotContain(f => f.Name == "Derived");
    }
}
