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

        var result = InspectTool.Run(SamplePath("maintenance-work-order.precept"), "Open", data, eventArgs);

        result.Error.Should().BeNull();
        result.CurrentState.Should().Be("Open");

        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Outcome.Should().Be("Accepted");
        assignEvent.ResultState.Should().Be("Scheduled");
    }

    [Fact]
    public void EventNotDefined_ReturnsNotDefined()
    {
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

        var result = InspectTool.Run(SamplePath("maintenance-work-order.precept"), "InProgress", data);

        var approvePartsEvent = result.Events.FirstOrDefault(e => e.Event == "ApproveParts");
        approvePartsEvent.Should().NotBeNull();
        approvePartsEvent!.Outcome.Should().Be("NotDefined");
    }

    [Fact]
    public void EventWithRequiredArgsMissing_ReturnsRequiresArgs()
    {
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

        var result = InspectTool.Run(SamplePath("maintenance-work-order.precept"), "Open", data);

        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.RequiresArgs.Should().BeTrue();
        assignEvent.RequiredArgs.Should().Contain(a => a.Name == "Technician");
        assignEvent.RequiredArgs.Should().Contain(a => a.Name == "Estimate");
    }

    [Fact]
    public void EventWithArgsSupplied_EvaluatedWithThoseArgs()
    {
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

        var result = InspectTool.Run(SamplePath("maintenance-work-order.precept"), "Open", data, eventArgs);

        var assignEvent = result.Events.FirstOrDefault(e => e.Event == "Assign");
        assignEvent.Should().NotBeNull();
        assignEvent!.Outcome.Should().Be("Accepted");
        assignEvent.ResultState.Should().Be("Scheduled");
    }

    [Fact]
    public void ResultOrdering_ActionableFirst()
    {
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

        var result = InspectTool.Run(SamplePath("maintenance-work-order.precept"), "InProgress", data);

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
