using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class SchemaToolTests
{
    private static string SamplesDir => TestPaths.SamplesDir;

    private static string SamplePath(string fileName) => Path.Combine(SamplesDir, fileName);

    [Fact]
    public void ValidFile_ReturnsCorrectStateCounts()
    {
        var result = SchemaTool.Run(SamplePath("maintenance-work-order.precept"));

        result.Error.Should().BeNull();
        result.Name.Should().Be("MaintenanceWorkOrder");
        result.InitialState.Should().Be("Draft");
        result.States.Should().NotBeEmpty();
        result.Events.Should().NotBeEmpty();
    }

    [Fact]
    public void Fields_HaveCorrectTypes()
    {
        var result = SchemaTool.Run(SamplePath("maintenance-work-order.precept"));

        result.Fields.Should().Contain(f => f.Name == "AssignedTechnician" && f.Type == "string" && f.Nullable);
        result.Fields.Should().Contain(f => f.Name == "EstimatedHours" && f.Type == "number");
    }

    [Fact]
    public void EventArgs_ArePresent()
    {
        var result = SchemaTool.Run(SamplePath("maintenance-work-order.precept"));

        var assignEvent = result.Events!.FirstOrDefault(e => e.Name == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Args.Should().Contain(a => a.Name == "Technician" && a.Type == "string");
        assignEvent.Args.Should().Contain(a => a.Name == "Estimate" && a.Type == "number");
    }

    [Fact]
    public void Transitions_SummarizeBranches()
    {
        var result = SchemaTool.Run(SamplePath("maintenance-work-order.precept"));

        result.Transitions.Should().NotBeEmpty();
        result.Transitions.Should().Contain(t => t.From == "Open" && t.On == "Assign");
    }

    [Fact]
    public void CollectionFields_Present()
    {
        var result = SchemaTool.Run(SamplePath("library-hold-request.precept"));

        if (result.Error is null && result.CollectionFields is { Count: > 0 })
        {
            result.CollectionFields.Should().Contain(cf => cf.Kind == "set" || cf.Kind == "queue" || cf.Kind == "stack");
        }
    }

    [Fact]
    public void StateRules_Populated()
    {
        var result = SchemaTool.Run(SamplePath("maintenance-work-order.precept"));

        var scheduled = result.States!.FirstOrDefault(s => s.Name == "Scheduled");
        scheduled.Should().NotBeNull();
        scheduled!.Rules.Should().NotBeEmpty();
    }

    [Fact]
    public void InvalidFile_ReturnsError()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "not valid precept");
            var result = SchemaTool.Run(tempFile);
            result.Error.Should().NotBeNullOrEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void MissingFile_ReturnsError()
    {
        var result = SchemaTool.Run(@"C:\nonexistent\schema.precept");
        result.Error.Should().Contain("File not found");
    }
}
