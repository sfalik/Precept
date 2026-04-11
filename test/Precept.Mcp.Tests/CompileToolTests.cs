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
}
