using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class AuditToolTests
{
    private static string SamplesDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static string SamplePath(string fileName) => Path.Combine(SamplesDir, fileName);

    [Fact]
    public void FullyConnectedGraph_NoIssues()
    {
        var result = AuditTool.Run(SamplePath("bugtracker.precept"));

        result.Error.Should().BeNull();
        result.AllStates.Should().NotBeEmpty();
        result.ReachableStates.Should().NotBeEmpty();
        result.UnreachableStates.Should().BeEmpty();
    }

    [Fact]
    public void TerminalState_Detected()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                precept Test
                state Open initial
                state Closed
                event Close
                from Open on Close -> transition Closed
                """);
            var result = AuditTool.Run(tempFile);

            result.Error.Should().BeNull();
            // Closed has no outgoing transitions
            result.TerminalStates.Should().Contain("Closed");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void UnreachableState_Detected()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                precept Test
                state Open initial
                state Closed
                state Orphan
                event Close
                from Open on Close -> transition Closed
                """);
            var result = AuditTool.Run(tempFile);

            result.UnreachableStates.Should().Contain("Orphan");
            result.Warnings.Should().Contain(w => w.Contains("Unreachable"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OrphanedEvent_Detected()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                precept Test
                state Open initial
                state Closed
                event Close
                event NeverUsed
                from Open on Close -> transition Closed
                """);
            var result = AuditTool.Run(tempFile);

            result.OrphanedEvents.Should().Contain("NeverUsed");
            result.Warnings.Should().Contain(w => w.Contains("Orphaned"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void DeadEnd_Detected()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
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
                """);
            var result = AuditTool.Run(tempFile);

            result.DeadEndStates.Should().Contain("DeadEnd");
            result.Warnings.Should().Contain(w => w.Contains("Dead-end"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void MissingFile_ReturnsError()
    {
        var result = AuditTool.Run(@"C:\nonexistent\audit.precept");
        result.Error.Should().Contain("File not found");
    }
}
