using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class UpdateToolTests
{
    private static string ReadSample(string fileName) =>
        File.ReadAllText(Path.Combine(TestPaths.SamplesDir, fileName));

    [Fact]
    public void SuccessfulEdit_ReturnsUpdateOutcome()
    {
        var text = ReadSample("maintenance-work-order.precept");
        // Draft state has: in Draft edit Location, IssueSummary, Urgent
        var data = new Dictionary<string, object?>
        {
            ["RequesterName"] = null,
            ["Location"] = "Plant 1",
            ["IssueSummary"] = "Old summary",
            ["AssignedTechnician"] = null,
            ["EstimatedHours"] = 0.0,
            ["ActualHours"] = 0.0,
            ["Urgent"] = false,
            ["PartsApproved"] = false,
            ["CompletionNote"] = null,
            ["CancellationReason"] = null
        };
        var fields = new Dictionary<string, object?>
        {
            ["Location"] = "Plant 2",
            ["IssueSummary"] = "Updated summary"
        };

        var result = UpdateTool.Update(text, "Draft", data, fields);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Update");
        result.Data.Should().NotBeNull();
        result.Data!["Location"].Should().Be("Plant 2");
        result.Data!["IssueSummary"].Should().Be("Updated summary");
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void UneditableField_ReturnsUneditableFieldOutcome()
    {
        var text = ReadSample("maintenance-work-order.precept");
        // Draft state only allows editing Location, IssueSummary, Urgent
        var fields = new Dictionary<string, object?>
        {
            ["AssignedTechnician"] = "alice"
        };

        var result = UpdateTool.Update(text, "Draft", null, fields);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("UneditableField");
    }

    [Fact]
    public void CompileFailure_ReturnsErrorString()
    {
        var result = UpdateTool.Update("not valid", "Open", null,
            new Dictionary<string, object?> { ["X"] = 1 });

        result.Error.Should().Be("Compilation failed. Use precept_compile to diagnose and fix errors first.");
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void EchoesResolvedDataSnapshot()
    {
        var text = ReadSample("maintenance-work-order.precept");
        var data = new Dictionary<string, object?>
        {
            ["RequesterName"] = null,
            ["Location"] = "Plant 1",
            ["IssueSummary"] = "Leaking valve"
        };
        var fields = new Dictionary<string, object?>
        {
            ["Urgent"] = true
        };

        var result = UpdateTool.Update(text, "Draft", data, fields);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("Update");
        result.Data.Should().NotBeNull();
        result.Data!["Urgent"].Should().Be(true);
        // Defaults should be present
        result.Data.Should().ContainKey("EstimatedHours");
    }

    [Fact]
    public void ConstraintFailure_ReturnsStructuredViolations()
    {
        var text = """
            precept Test
            field Priority as number default 5
            invariant Priority >= 1 because "Priority must be positive"
            state Open initial
            in Open edit Priority
            """;

        var result = UpdateTool.Update(text, "Open", null,
            new Dictionary<string, object?> { ["Priority"] = 0.0 });

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("ConstraintFailure");
        result.Violations.Should().NotBeEmpty();
        result.Violations[0].Message.Should().Contain("Priority must be positive");
    }

    [Fact]
    public void NoEditDeclarations_ReturnsUneditableField()
    {
        var text = ReadSample("maintenance-work-order.precept");
        // Open state has no edit declarations
        var fields = new Dictionary<string, object?>
        {
            ["Location"] = "New Location"
        };

        var result = UpdateTool.Update(text, "Open", null, fields);

        result.Error.Should().BeNull();
        result.Outcome.Should().Be("UneditableField");
    }
}
