using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

public class GraphAnalyzerTests
{
    [Fact]
    public void Analyze_UnreachableState_EmitsDiagnosticAndReachabilityFacts()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Review
            state Approved terminal
            state Archived
            event Submit
            event Approve
            from Draft on Submit -> transition Review
            from Review on Approve -> transition Approved
            """);

        graph.ReachableStates.Should().BeEquivalentTo(["Draft", "Review", "Approved"]);
        graph.UnreachableStates.Should().BeEquivalentTo(["Archived"]);
        graph.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.UnreachableState));

        var archivedFact = graph.ProofFacts.OfType<ReachabilityFact>().Single(f => f.StateName == "Archived");
        archivedFact.IsReachable.Should().BeFalse();
        archivedFact.PathFromInitial.Should().BeNull();

        var approvedFact = graph.ProofFacts.OfType<ReachabilityFact>().Single(f => f.StateName == "Approved");
        approvedFact.IsReachable.Should().BeTrue();
        approvedFact.PathFromInitial.Should().Equal("Draft", "Review");
    }

    [Fact]
    public void Analyze_DeadEndState_EmitsWarningAndDeadEndFact()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Review
            state Stalled
            state Approved terminal
            event Submit
            event Approve
            event Stall
            from Draft on Submit -> transition Review
            from Review on Approve -> transition Approved
            from Review on Stall -> transition Stalled
            """);

        graph.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.DeadEndState) && d.Message.Contains("Stalled"));

        var deadEndFact = graph.ProofFacts.OfType<DeadEndStateFact>().Single();
        deadEndFact.DeadEndStates.Should().Equal("Stalled");
        deadEndFact.DeadEndCount.Should().Be(1);
    }

    [Fact]
    public void Analyze_EventCoverage_IsEventLevelAcrossGuardedRows()
    {
        var graph = Analyze("""
            precept Workflow
            field Count as number default 0
            state Draft initial
            state Active
            event Stay
            event OnlyDraft
            event Unused
            from Draft on Stay when Count >= 0 -> transition Active
            from Active on Stay when Count >= 0 -> transition Draft
            from Draft on OnlyDraft when Count >= 0 -> no transition
            """);

        var stayCoverage = graph.EventCoverage.Single(entry => entry.EventName == "Stay");
        stayCoverage.HandlingStates.Should().Equal("Draft", "Active");
        stayCoverage.NonHandlingReachableStates.Should().BeEmpty();

        var onlyDraftCoverage = graph.EventCoverage.Single(entry => entry.EventName == "OnlyDraft");
        onlyDraftCoverage.HandlingStates.Should().Equal("Draft");
        onlyDraftCoverage.NonHandlingReachableStates.Should().Equal("Active");

        var onlyDraftFact = graph.ProofFacts.OfType<EventCoverageFact>().Single(f => f.EventName == "OnlyDraft");
        onlyDraftFact.UnhandledReachableStates.Should().Equal("Active");
        graph.ProofFacts.OfType<EventCoverageFact>().Should().NotContain(f => f.EventName == "Stay");
        graph.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.UnhandledEvent) && d.Message.Contains("Unused"));
    }

    [Fact]
    public void Analyze_UnreachableTerminal_ProducesTerminalCompletenessFact()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Review
            state Approved terminal
            event Submit
            from Draft on Submit -> no transition
            """);

        var fact = graph.ProofFacts.OfType<TerminalCompletenessFact>().Single();
        fact.AllTerminalsReachable.Should().BeFalse();
        fact.UnreachableTerminals.Should().Equal("Approved");
    }

    [Fact]
    public void Analyze_ProducesDominanceAndProofForwardingFacts()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Review required
            state Approved terminal
            state Rejected terminal
            event Submit
            event Approve
            event Reject
            from Draft on Submit -> transition Review
            from Review on Approve -> transition Approved
            from Review on Reject -> transition Rejected
            """);

        graph.Dominance.Should().Contain(f => f.Dominator == "Review" && f.Dominated == "Approved" && f.Distance == 1);
        graph.Dominance.Should().Contain(f => f.Dominator == "Review" && f.Dominated == "Rejected" && f.Distance == 1);

        var dominanceFact = graph.ProofFacts.OfType<DominancePathFact>().Single();
        dominanceFact.RequiredState.Should().Be("Review");
        dominanceFact.DominatedTerminals.Should().Equal("Approved", "Rejected");

        graph.Events.Single(e => e.Name == "Submit").IsInitial.Should().BeTrue();
    }

    private static StateGraph Analyze(string source)
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(source);
        return GraphAnalyzer.Analyze(index);
    }
}
