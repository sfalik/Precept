using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class RunToolTests
{
    private static string SamplesDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static string SamplePath(string fileName) => Path.Combine(SamplesDir, fileName);

    [Fact]
    public void HappyPath_AllStepsAccepted()
    {
        var steps = new List<RunStep>
        {
            new("Assign", new() { ["User"] = "alice" }),
            new("StartWork")
        };

        var result = RunTool.Run(SamplePath("bugtracker.precept"), steps: steps);

        result.Error.Should().BeNull();
        result.AbortedAt.Should().BeNull();
        result.Steps.Should().HaveCount(2);
        result.FinalState.Should().Be("InProgress");
    }

    [Fact]
    public void StepRejected_AbortsEarly()
    {
        // Try StartWork from Triage without assigning first — no transition defined
        var steps = new List<RunStep>
        {
            new("StartWork")
        };

        var result = RunTool.Run(SamplePath("bugtracker.precept"), steps: steps);

        result.Error.Should().BeNull();
        result.AbortedAt.Should().Be(1);
        result.Steps.Should().HaveCount(1);
        result.Steps[0].Outcome.Should().Be("NotDefined");
    }

    [Fact]
    public void EmptySteps_ReturnsInitialState()
    {
        var result = RunTool.Run(SamplePath("bugtracker.precept"), steps: []);

        result.Error.Should().BeNull();
        result.AbortedAt.Should().BeNull();
        result.FinalState.Should().Be("Triage");
        result.Steps.Should().BeEmpty();
    }

    [Fact]
    public void StepsWithEventArgs_PassedCorrectly()
    {
        var steps = new List<RunStep>
        {
            new("Prioritize", new() { ["Level"] = 5.0 })
        };

        var result = RunTool.Run(SamplePath("bugtracker.precept"), steps: steps);

        result.Error.Should().BeNull();
        result.AbortedAt.Should().BeNull();
        result.Steps.Should().HaveCount(1);
        result.Steps[0].Outcome.Should().Be("AcceptedInPlace");
        result.FinalData!["Priority"].Should().Be(5.0);
    }

    [Fact]
    public void InvalidFile_ErrorBeforeExecution()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "not valid");
            var result = RunTool.Run(tempFile, steps: [new("Foo")]);

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
        var result = RunTool.Run(@"C:\nonexistent\run.precept");
        result.Error.Should().Contain("File not found");
    }
}
