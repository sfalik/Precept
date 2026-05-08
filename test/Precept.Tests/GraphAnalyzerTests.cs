using System.Collections.Generic;
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
        graph.ReachableStates.Union(graph.UnreachableStates).Should().BeEquivalentTo(graph.States.Select(state => state.Name));
        graph.ReachableStates.Intersect(graph.UnreachableStates).Should().BeEmpty();

        var unreachableDiagnostic = graph.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.UnreachableState));
        unreachableDiagnostic.Severity.Should().Be(Severity.Warning);

        var deadEndFact = graph.ProofFacts.OfType<DeadEndStateFact>().Single();
        deadEndFact.DeadEndStates.Should().NotContain("Archived");

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

        var deadEndDiagnostic = graph.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.DeadEndState) && d.Message.Contains("Stalled"));
        deadEndDiagnostic.Severity.Should().Be(Severity.Warning);

        var deadEndFact = graph.ProofFacts.OfType<DeadEndStateFact>().Single();
        deadEndFact.DeadEndStates.Should().Equal("Stalled");
        deadEndFact.DeadEndStates.Should().NotContain("Approved");
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
            from Draft on Stay when Count >= 0 -> transition Active
            from Active on Stay when Count >= 0 -> transition Draft
            """);

        var stayCoverage = graph.EventCoverage.Single(entry => entry.EventName == "Stay");
        stayCoverage.HandlingStates.Should().Equal("Draft", "Active");
        stayCoverage.NonHandlingReachableStates.Should().BeEmpty();
        graph.ProofFacts.OfType<EventCoverageFact>().Should().BeEmpty();
        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnhandledEvent));
    }

    [Fact]
    public void Analyze_EventWithNoHandlers_EmitsUnhandledEventDiagnostic()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Active
            event Move
            event Unused
            from Draft on Move -> transition Active
            from Active on Move -> no transition
            """);

        var unusedCoverage = graph.EventCoverage.Single(entry => entry.EventName == "Unused");
        unusedCoverage.HandlingStates.Should().BeEmpty();
        graph.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.UnhandledEvent) && d.Message.Contains("Unused"));
    }

    [Fact]
    public void Analyze_EventWithPartialCoverage_EmitsNoDiagnostic()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Active
            event Move
            event OnlyDraft
            from Draft on Move -> transition Active
            from Active on Move -> no transition
            from Draft on OnlyDraft -> no transition
            """);

        var onlyDraftCoverage = graph.EventCoverage.Single(entry => entry.EventName == "OnlyDraft");
        onlyDraftCoverage.HandlingStates.Should().Equal("Draft");
        onlyDraftCoverage.NonHandlingReachableStates.Should().Equal("Active");

        var onlyDraftFact = graph.ProofFacts.OfType<EventCoverageFact>().Single(f => f.EventName == "OnlyDraft");
        onlyDraftFact.UnhandledReachableStates.Should().Equal("Active");
        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnhandledEvent));
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
    public void Analyze_RejectOutcome_CreatesSelfEdge()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            event Submit
            from Draft on Submit -> reject "Denied"
            """);

        graph.Edges.Should().ContainSingle(edge =>
            edge.EventName == "Submit"
            && edge.Outcome == TransitionRowOutcome.Reject
            && edge.FromState == "Draft"
            && edge.ToState == "Draft");
    }

    [Fact]
    public void Analyze_NoTransitionOutcome_CreatesSelfEdge()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            event Stay
            from Draft on Stay -> no transition
            """);

        graph.Edges.Should().ContainSingle(edge =>
            edge.EventName == "Stay"
            && edge.Outcome == TransitionRowOutcome.NoTransition
            && edge.FromState == "Draft"
            && edge.ToState == "Draft");
    }

    [Fact]
    public void Analyze_RequiredState_ProducesDominanceFact()
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

    [Fact]
    public void Analyze_WildcardRow_ExpandsToAllStatesWithoutExplicitRow()
    {
        var (_, _, graph) = AnalyzeAllowingDiagnostics("""
            precept Workflow
            state Draft initial
            state Review
            state Approved terminal
            event Reset
            from any on Reset -> transition Draft
            from Review on Reset -> transition Draft
            """);

        var resetEdges = graph.Edges.Where(edge => edge.EventName == "Reset").ToList();

        resetEdges.Should().HaveCount(3);
        resetEdges.Should().Contain(edge => edge.FromState == "Draft" && edge.ToState == "Draft");
        resetEdges.Should().Contain(edge => edge.FromState == "Review" && edge.ToState == "Draft");
        resetEdges.Should().Contain(edge => edge.FromState == "Approved" && edge.ToState == "Draft");
    }

    [Fact]
    public void Analyze_WildcardRow_SuppressedWhenExplicitRowExists()
    {
        var (_, _, graph) = AnalyzeAllowingDiagnostics("""
            precept Workflow
            state Draft initial
            state Review
            state Approved terminal
            event Reset
            from any on Reset -> transition Draft
            from Review on Reset -> transition Draft
            """);

        var resetEdges = graph.Edges.Where(edge => edge.EventName == "Reset").ToList();

        resetEdges.Count(edge => edge.FromState == "Review").Should().Be(1);
    }

    [Fact]
    public void Analyze_NoInitialState_EmitsDiagnosticAndMarksAllUnreachable()
    {
        var (_, diagnostics, graph) = AnalyzeAllowingDiagnostics("""
            precept Workflow
            state Draft
            state Review
            state Approved terminal
            event Submit
            from Draft on Submit -> transition Review
            """);

        var noInitialDiagnostics = diagnostics
            .Concat(graph.Diagnostics)
            .Where(d => d.Code == nameof(DiagnosticCode.NoInitialState))
            .ToList();

        noInitialDiagnostics.Should().NotBeEmpty();
        noInitialDiagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
        graph.UnreachableStates.Should().BeEquivalentTo(["Draft", "Review", "Approved"]);
        graph.ReachableStates.Should().BeEmpty();
        graph.ProofFacts.OfType<TerminalCompletenessFact>().Should().ContainSingle();
        graph.ProofFacts.OfType<DeadEndStateFact>().Should().ContainSingle();
    }

    [Fact]
    public void Analyze_StatelessPrecept_ReturnsMinimalGraph()
    {
        var graph = Analyze("""
            precept Workflow
            event Submit
            """);

        graph.States.Should().BeEmpty();
        graph.Edges.Should().BeEmpty();
        graph.ReachableStates.Should().BeEmpty();
        graph.UnreachableStates.Should().BeEmpty();
        graph.ProofFacts.OfType<TerminalCompletenessFact>().Single().AllTerminalsReachable.Should().BeTrue();
        graph.ProofFacts.OfType<DeadEndStateFact>().Single().DeadEndCount.Should().Be(0);
        graph.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_TerminalStateWithOutgoingEdge_ProducesTerminalViolation()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Approved terminal
            event Approve
            event Reopen
            from Draft on Approve -> transition Approved
            from Approved on Reopen -> transition Draft
            """);

        graph.TerminalViolations.Should().NotBeEmpty();

        var violation = graph.TerminalViolations.Single();
        violation.StateName.Should().Be("Approved");
        violation.OutgoingEdges.Should().ContainSingle(edge => edge.EventName == "Reopen" && edge.ToState == "Draft");

        graph.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.TerminalStateHasOutgoingEdges));
    }

    [Fact]
    public void Analyze_IrreversibleStateWithBackEdge_ProducesBackEdgeViolation()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Processing irreversible
            state Approved terminal
            event Start
            event Complete
            event Reset
            from Draft on Start -> transition Processing
            from Processing on Complete -> transition Approved
            from Processing on Reset -> transition Draft
            """);

        graph.BackEdgeViolations.Should().NotBeEmpty();

        var violation = graph.BackEdgeViolations.Single();
        violation.StateName.Should().Be("Processing");
        violation.BackEdge.EventName.Should().Be("Reset");
        violation.BackEdge.ToState.Should().Be("Draft");

        graph.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.IrreversibleStateHasBackEdge));
    }

    [Fact]
    public void Analyze_AllTerminalsReachable_TerminalCompletenessFact_IsTrue()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Approved terminal
            event Approve
            from Draft on Approve -> transition Approved
            """);

        var fact = graph.ProofFacts.OfType<TerminalCompletenessFact>().Single();
        fact.AllTerminalsReachable.Should().BeTrue();
        fact.UnreachableTerminals.Should().BeEmpty();
        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DeadEndState));
    }

    [Fact]
    public void Analyze_CycleInGraph_AllCycleStatesReachable()
    {
        var graph = Analyze("""
            precept Workflow
            state A initial
            state B
            state C
            state D terminal
            event ToB
            event ToC
            event ToA
            event ToD
            from A on ToB -> transition B
            from B on ToC -> transition C
            from C on ToA -> transition A
            from B on ToD -> transition D
            """);

        graph.ReachableStates.Should().BeEquivalentTo(["A", "B", "C", "D"]);
        graph.UnreachableStates.Should().BeEmpty();
        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnreachableState));
        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DeadEndState));
        graph.ProofFacts.OfType<TerminalCompletenessFact>().Single().AllTerminalsReachable.Should().BeTrue();
    }

    [Fact]
    public void Analyze_SingleStateInitialAndTerminal_ReachableNotDeadEnd()
    {
        var graph = Analyze("""
            precept Workflow
            state Closed initial terminal
            """);

        graph.ReachableStates.Should().BeEquivalentTo(["Closed"]);
        graph.UnreachableStates.Should().BeEmpty();
        graph.ProofFacts.OfType<DeadEndStateFact>().Single().DeadEndStates.Should().BeEmpty();
        graph.ProofFacts.OfType<TerminalCompletenessFact>().Single().AllTerminalsReachable.Should().BeTrue();
        graph.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_DiamondGraph_MultiplePathsToSameState()
    {
        var graph = Analyze("""
            precept Workflow
            state Draft initial
            state Review
            state Expedite
            state Approved terminal
            event ReviewRoute
            event ExpediteRoute
            event ApproveFromReview
            event ApproveFromExpedite
            from Draft on ReviewRoute -> transition Review
            from Draft on ExpediteRoute -> transition Expedite
            from Review on ApproveFromReview -> transition Approved
            from Expedite on ApproveFromExpedite -> transition Approved
            """);

        graph.ReachableStates.Should().BeEquivalentTo(["Draft", "Review", "Expedite", "Approved"]);
        graph.UnreachableStates.Should().BeEmpty();
        graph.ProofFacts.OfType<DeadEndStateFact>().Single().DeadEndStates.Should().BeEmpty();
        graph.ProofFacts.OfType<TerminalCompletenessFact>().Single().AllTerminalsReachable.Should().BeTrue();
        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DeadEndState));
    }

    [Fact]
    public void Analyze_MultipleDeadEndStates_AllReported()
    {
        var graph = Analyze("""
            precept Workflow
            state A initial
            state B terminal
            state C
            state D
            event ToB
            event ToC
            event ToD
            from A on ToB -> transition B
            from A on ToC -> transition C
            from A on ToD -> transition D
            """);

        var deadEndFact = graph.ProofFacts.OfType<DeadEndStateFact>().Single();
        deadEndFact.DeadEndCount.Should().Be(2);
        deadEndFact.DeadEndStates.Should().BeEquivalentTo(["C", "D"]);

        var deadEndDiagnostics = graph.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.DeadEndState)).ToList();
        deadEndDiagnostics.Should().HaveCount(2);
        deadEndDiagnostics.Should().OnlyContain(d => d.Severity == Severity.Warning);
    }

    private static StateGraph Analyze(string source)
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(source);
        return GraphAnalyzer.Analyze(index);
    }

    private static (SemanticIndex Index, IReadOnlyList<Diagnostic> Diagnostics, StateGraph Graph) AnalyzeAllowingDiagnostics(string source)
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(source);
        return (index, diagnostics, GraphAnalyzer.Analyze(index));
    }
}
