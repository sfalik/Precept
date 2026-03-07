using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class InspectToolTests
{
    private static string SamplesDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static string SamplePath(string fileName) => Path.Combine(SamplesDir, fileName);

    [Fact]
    public void EventThatTransitions_ReturnsAccepted()
    {
        var data = new Dictionary<string, object?>
        {
            ["Assignee"] = "alice",
            ["Priority"] = 3.0,
            ["BlockReason"] = null,
            ["Resolution"] = null
        };
        var eventArgs = new Dictionary<string, Dictionary<string, object?>>
        {
            ["Block"] = new() { ["Reason"] = "Waiting on infra" }
        };

        var result = InspectTool.Run(SamplePath("bugtracker.precept"), "InProgress", data, eventArgs);

        result.Error.Should().BeNull();
        result.CurrentState.Should().Be("InProgress");

        var blockEvent = result.Events.FirstOrDefault(e => e.Event == "Block");
        blockEvent.Should().NotBeNull();
        blockEvent!.Outcome.Should().Be("Accepted");
        blockEvent.ResultState.Should().Be("Blocked");
    }

    [Fact]
    public void EventNotDefined_ReturnsNotDefined()
    {
        var data = new Dictionary<string, object?>
        {
            ["Assignee"] = "alice",
            ["Priority"] = 3.0,
            ["BlockReason"] = null,
            ["Resolution"] = null
        };

        var result = InspectTool.Run(SamplePath("bugtracker.precept"), "InProgress", data);

        var approveEvent = result.Events.FirstOrDefault(e => e.Event == "Approve");
        approveEvent.Should().NotBeNull();
        approveEvent!.Outcome.Should().Be("NotDefined");
    }

    [Fact]
    public void EventWithRequiredArgsMissing_ReturnsRequiresArgs()
    {
        var data = new Dictionary<string, object?>
        {
            ["Assignee"] = null,
            ["Priority"] = 3.0,
            ["BlockReason"] = null,
            ["Resolution"] = null
        };

        var result = InspectTool.Run(SamplePath("bugtracker.precept"), "Triage", data);

        // Assign requires "User" arg which wasn't supplied
        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.RequiresArgs.Should().BeTrue();
        assignEvent.RequiredArgs.Should().ContainSingle(a => a.Name == "User");
    }

    [Fact]
    public void EventWithArgsSupplied_EvaluatedWithThoseArgs()
    {
        var data = new Dictionary<string, object?>
        {
            ["Assignee"] = null,
            ["Priority"] = 3.0,
            ["BlockReason"] = null,
            ["Resolution"] = null
        };
        var eventArgs = new Dictionary<string, Dictionary<string, object?>>
        {
            ["Assign"] = new() { ["User"] = "alice" }
        };

        var result = InspectTool.Run(SamplePath("bugtracker.precept"), "Triage", data, eventArgs);

        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Outcome.Should().Be("Accepted");
        assignEvent.ResultState.Should().Be("Open");
    }

    [Fact]
    public void ResultOrdering_ActionableFirst()
    {
        var data = new Dictionary<string, object?>
        {
            ["Assignee"] = "alice",
            ["Priority"] = 3.0,
            ["BlockReason"] = null,
            ["Resolution"] = null
        };

        var result = InspectTool.Run(SamplePath("bugtracker.precept"), "InProgress", data);

        // First events should be actionable (Accepted/AcceptedInPlace), then not-defined/etc., then requiresArgs
        var events = result.Events.ToList();
        var firstActionable = events.FindIndex(e =>
            e.Outcome == "Accepted" || e.Outcome == "AcceptedInPlace");
        var firstUnavailable = events.FindIndex(e =>
            e.Outcome == "NotDefined" || e.Outcome == "NotApplicable" || e.Outcome == "Rejected");
        var firstRequiresArgs = events.FindIndex(e => e.RequiresArgs == true);

        if (firstActionable >= 0 && firstUnavailable >= 0)
            firstActionable.Should().BeLessThan(firstUnavailable);
        if (firstActionable >= 0 && firstRequiresArgs >= 0)
            firstActionable.Should().BeLessThan(firstRequiresArgs);
    }

    [Fact]
    public void MissingFile_ReturnsError()
    {
        var result = InspectTool.Run(@"C:\nonexistent\inspect.precept", "Open");
        result.Error.Should().Contain("File not found");
    }
}
