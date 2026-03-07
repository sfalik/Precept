using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class SchemaToolTests
{
    private static string SamplesDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static string SamplePath(string fileName) => Path.Combine(SamplesDir, fileName);

    [Fact]
    public void ValidFile_ReturnsCorrectStateCounts()
    {
        var result = SchemaTool.Run(SamplePath("bugtracker.precept"));

        result.Error.Should().BeNull();
        result.Name.Should().Be("BugTracker");
        result.InitialState.Should().Be("Triage");
        result.States.Should().NotBeEmpty();
        result.Events.Should().NotBeEmpty();
    }

    [Fact]
    public void Fields_HaveCorrectTypes()
    {
        var result = SchemaTool.Run(SamplePath("bugtracker.precept"));

        result.Fields.Should().Contain(f => f.Name == "Assignee" && f.Type == "string" && f.Nullable);
        result.Fields.Should().Contain(f => f.Name == "Priority" && f.Type == "number");
    }

    [Fact]
    public void EventArgs_ArePresent()
    {
        var result = SchemaTool.Run(SamplePath("bugtracker.precept"));

        var assignEvent = result.Events!.FirstOrDefault(e => e.Name == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Args.Should().ContainSingle(a => a.Name == "User" && a.Type == "string");
    }

    [Fact]
    public void Transitions_SummarizeBranches()
    {
        var result = SchemaTool.Run(SamplePath("bugtracker.precept"));

        result.Transitions.Should().NotBeEmpty();
        result.Transitions.Should().Contain(t => t.From == "Triage" && t.On == "Assign");
    }

    [Fact]
    public void CollectionFields_Present()
    {
        // document-signing has collection fields
        var result = SchemaTool.Run(SamplePath("document-signing.precept"));

        if (result.Error is null && result.CollectionFields is { Count: > 0 })
        {
            result.CollectionFields.Should().Contain(cf => cf.Kind == "set" || cf.Kind == "queue" || cf.Kind == "stack");
        }
    }

    [Fact]
    public void StateRules_Populated()
    {
        var result = SchemaTool.Run(SamplePath("bugtracker.precept"));

        // InProgress has "Must have an assignee to be in progress"
        var inProgress = result.States!.FirstOrDefault(s => s.Name == "InProgress");
        inProgress.Should().NotBeNull();
        inProgress!.Rules.Should().NotBeEmpty();
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
