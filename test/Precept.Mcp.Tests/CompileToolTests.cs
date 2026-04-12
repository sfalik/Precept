using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class CompileToolTests
{
    private static string ReadSample(string fileName) =>
        File.ReadAllText(Path.Combine(TestPaths.SamplesDir, fileName));

    [Fact]
    public void ValidDefinition_ReturnsFullSchemaWithZeroDiagnostics()
    {
        var text = ReadSample("maintenance-work-order.precept");

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Name.Should().Be("MaintenanceWorkOrder");
        result.InitialState.Should().Be("Draft");
        result.StateCount.Should().BeGreaterThan(0);
        result.EventCount.Should().BeGreaterThan(0);
        result.States.Should().NotBeEmpty();
        result.Fields.Should().NotBeEmpty();
        result.Events.Should().NotBeEmpty();
        result.Transitions.Should().NotBeEmpty();
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void ValidDefinition_FieldsHaveCorrectTypes()
    {
        var text = ReadSample("maintenance-work-order.precept");

        var result = CompileTool.Run(text);

        result.Fields.Should().Contain(f => f.Name == "AssignedTechnician" && f.Type == "string" && f.Nullable);
        result.Fields.Should().Contain(f => f.Name == "EstimatedHours" && f.Type == "number");
    }

    [Fact]
    public void ValidDefinition_EventArgsPresent()
    {
        var text = ReadSample("maintenance-work-order.precept");

        var result = CompileTool.Run(text);

        var assignEvent = result.Events!.FirstOrDefault(e => e.Name == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Args.Should().Contain(a => a.Name == "Technician" && a.Type == "string");
        assignEvent.Args.Should().Contain(a => a.Name == "Estimate" && a.Type == "number");
    }

    [Fact]
    public void ValidDefinition_TransitionsSummarizeBranches()
    {
        var text = ReadSample("maintenance-work-order.precept");

        var result = CompileTool.Run(text);

        result.Transitions.Should().Contain(t => t.From == "Open" && t.On == "Assign");
    }

    [Fact]
    public void ValidDefinition_StateRulesPopulated()
    {
        var text = ReadSample("maintenance-work-order.precept");

        var result = CompileTool.Run(text);

        var scheduled = result.States!.FirstOrDefault(s => s.Name == "Scheduled");
        scheduled.Should().NotBeNull();
        scheduled!.Rules.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidDefinition_CollectionFieldsPresent()
    {
        var text = ReadSample("library-hold-request.precept");

        var result = CompileTool.Run(text);

        if (result.Valid && result.CollectionFields is { Count: > 0 })
        {
            result.CollectionFields.Should().Contain(cf =>
                cf.Kind == "set" || cf.Kind == "queue" || cf.Kind == "stack");
        }
    }

    [Fact]
    public void TypeErrors_ReturnsPartialSchemaWithErrorDiagnostics()
    {
        var text = """
            precept Test
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeFalse();
        result.Name.Should().Be("Test");
        result.States.Should().NotBeNull();
        result.Fields.Should().NotBeNull();
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("PRECEPT042");
        result.Diagnostics[0].Severity.Should().Be("error");
        result.Diagnostics[0].Message.Should().Contain("set target 'Value' type mismatch");
    }

    [Fact]
    public void MultipleTypeErrors_ReturnAllDiagnostics()
    {
        var text = """
            precept Test
            field Value as number default 0
            field Name as string default ""
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> set Name = Missing -> transition B
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeFalse();
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics.Select(d => d.Code).Should().BeEquivalentTo(["PRECEPT042", "PRECEPT038"]);
    }

    [Fact]
    public void ParseFailure_ReturnsDiagnosticsOnlyNoSchema()
    {
        var text = "precept Test\nfield as number\n";

        var result = CompileTool.Run(text);

        result.Valid.Should().BeFalse();
        result.Name.Should().BeNull();
        result.States.Should().BeNull();
        result.Fields.Should().BeNull();
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void UnreachableState_WarningDiagnostic()
    {
        var text = """
            precept Test
            state A initial
            state B
            state Unreachable
            event Go
            from A on Go -> transition B
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("PRECEPT048");
        result.Diagnostics[0].Severity.Should().Be("warning");
    }

    [Fact]
    public void OrphanedEvent_WarningDiagnostic()
    {
        var text = """
            precept Test
            state A initial
            state B
            event Go
            event NeverUsed
            from A on Go -> transition B
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT049" && d.Severity == "warning");
    }

    [Fact]
    public void DeadEndState_HintDiagnostic()
    {
        var text = """
            precept Test
            state Open initial
            state DeadEnd
            state Closed
            event GoDeadEnd
            event TryEscape
            event Close
            from Open on GoDeadEnd -> transition DeadEnd
            from DeadEnd on TryEscape -> no transition
            from Open on Close -> transition Closed
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT050" && d.Severity == "warning");
    }

    [Fact]
    public void CompileTimeViolation_ReturnsInvalid()
    {
        var text = """
            precept Test
            field Priority as number default 0
            invariant Priority >= 1 because "Must be positive"
            state Open initial
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeFalse();
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void EmptyText_ReturnsInvalid()
    {
        var result = CompileTool.Run("");

        result.Valid.Should().BeFalse();
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void DiagnosticsIncludeColumnInfo()
    {
        var text = """
            precept Test
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var result = CompileTool.Run(text);

        // Column should be present (0-based)
        result.Diagnostics.Should().NotBeEmpty();
        result.Diagnostics[0].Line.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IntegerField_SerializesAsIntegerType()
    {
        var text = """
            precept Test
            field Count as integer default 0
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Fields.Should().Contain(f => f.Name == "Count" && f.Type == "integer");
    }

    [Fact]
    public void DecimalField_SerializesAsDecimalType()
    {
        var text = """
            precept Test
            field Amount as decimal default 0
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Fields.Should().Contain(f => f.Name == "Amount" && f.Type == "decimal");
    }

    [Fact]
    public void ChoiceField_SerializesChoiceValuesAndType()
    {
        var text = """
            precept Test
            field Status as choice("Pending","Active","Closed") default "Pending"
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        var statusField = result.Fields!.FirstOrDefault(f => f.Name == "Status");
        statusField.Should().NotBeNull();
        statusField!.Type.Should().Be("choice");
        statusField.ChoiceValues.Should().BeEquivalentTo(["Pending", "Active", "Closed"]);
    }

    [Fact]
    public void OrderedChoiceField_SerializesIsOrdered()
    {
        var text = """
            precept Test
            field Priority as choice("Low","Medium","High") default "Low" ordered
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        var priorityField = result.Fields!.FirstOrDefault(f => f.Name == "Priority");
        priorityField.Should().NotBeNull();
        priorityField!.IsOrdered.Should().BeTrue();
    }

    [Fact]
    public void NonOrderedChoiceField_IsOrderedIsNull()
    {
        var text = """
            precept Test
            field Status as choice("A","B") default "A"
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        var f = result.Fields!.FirstOrDefault(f => f.Name == "Status");
        f.Should().NotBeNull();
        f!.IsOrdered.Should().BeNull();
    }

    [Fact]
    public void MaxplacesConstraint_SerializesWithPlaces()
    {
        var text = """
            precept Test
            field Amount as decimal default 0 maxplaces 2
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        var amountField = result.Fields!.FirstOrDefault(f => f.Name == "Amount");
        amountField.Should().NotBeNull();
        amountField!.Constraints.Should().Contain("maxplaces 2");
    }

    [Fact]
    public void StringLengthInvariant_CompilesCleanly()
    {
        var text = """
            precept Test
            field Name as string default ""
            state A initial
            state B
            event Go
            invariant Name.length <= 100 because "Name must be \u2264100 characters"
            from A on Go -> transition B
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════
    // Slice 9c: when-guard DTO tests
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Compile_WhenGuardedInvariant_InvariantsArrayPopulated()
    {
        var text = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            invariant X >= 0 when Active because "X must be non-negative when active"
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Invariants.Should().NotBeNull();
        var guarded = result.Invariants!.FirstOrDefault(i => i.When is not null);
        guarded.Should().NotBeNull("a when-guarded invariant should have When populated");
        guarded!.When.Should().Contain("Active");
        guarded.Expression.Should().Contain("X >= 0");
    }

    [Fact]
    public void Compile_WhenGuardedStateAssert_StateAssertsArrayPopulated()
    {
        var text = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state Open initial
            in Open assert X >= 0 when Active because "X must be non-negative when active"
            event Go
            from Open on Go -> no transition
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.StateAsserts.Should().NotBeNull();
        var guarded = result.StateAsserts!.FirstOrDefault(sa => sa.When is not null);
        guarded.Should().NotBeNull("a when-guarded state assert should have When populated");
        guarded!.When.Should().Contain("Active");
        guarded.State.Should().Be("Open");
    }

    [Fact]
    public void Compile_WhenGuardedEventAssert_EventAssertsArrayPopulated()
    {
        var text = """
            precept Test
            field X as number default 0
            state A initial
            event Submit with Amount as number, Priority as number
            on Submit assert Amount > 0 when Priority > 1 because "Amount required for high priority"
            from A on Submit -> no transition
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.EventAsserts.Should().NotBeNull();
        var guarded = result.EventAsserts!.FirstOrDefault(ea => ea.When is not null);
        guarded.Should().NotBeNull("a when-guarded event assert should have When populated");
        guarded!.When.Should().Contain("Priority > 1");
        guarded.Event.Should().Be("Submit");
    }

    [Fact]
    public void Compile_WhenGuardedEditBlock_EditBlocksArrayPopulated()
    {
        var text = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state Open initial
            in Open when Active edit X
            event Go
            from Open on Go -> no transition
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.EditBlocks.Should().NotBeNull();
        var guarded = result.EditBlocks!.FirstOrDefault(eb => eb.When is not null);
        guarded.Should().NotBeNull("a when-guarded edit block should have When populated");
        guarded!.When.Should().Contain("Active");
        guarded.State.Should().Be("Open");
        guarded.Fields.Should().Contain("X");
    }

    [Fact]
    public void Compile_UnguardedInvariant_WhenIsNull()
    {
        var text = """
            precept Test
            field X as number default 1
            invariant X > 0 because "X must be positive"
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Invariants.Should().NotBeNull();
        var unguarded = result.Invariants!.FirstOrDefault(i => i.Expression.Contains("X > 0"));
        unguarded.Should().NotBeNull();
        unguarded!.When.Should().BeNull("an unguarded invariant should have When = null");
    }
}
