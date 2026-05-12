using System;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class StateGraph(
    ImmutableArray<GraphState> States,
    ImmutableArray<GraphEvent> Events,
    ImmutableArray<GraphEdge> Edges,
    ImmutableArray<EdgeProofStatus> EdgeProofStatuses,
    ImmutableHashSet<string> ReachableStates,
    ImmutableHashSet<string> UnreachableStates,
    ImmutableArray<DominanceFact> Dominance,
    ImmutableArray<TerminalOutgoingViolation> TerminalViolations,
    ImmutableArray<IrreversibleBackEdgeViolation> BackEdgeViolations,
    ImmutableArray<EventCoverageEntry> EventCoverage,
    ImmutableArray<ProofForwardingFact> ProofFacts,
    ImmutableArray<Diagnostic> Diagnostics
)
{
    public static StateGraph Empty { get; } = new(
        States: ImmutableArray<GraphState>.Empty,
        Events: ImmutableArray<GraphEvent>.Empty,
        Edges: ImmutableArray<GraphEdge>.Empty,
        EdgeProofStatuses: ImmutableArray<EdgeProofStatus>.Empty,
        ReachableStates: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
        UnreachableStates: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
        Dominance: ImmutableArray<DominanceFact>.Empty,
        TerminalViolations: ImmutableArray<TerminalOutgoingViolation>.Empty,
        BackEdgeViolations: ImmutableArray<IrreversibleBackEdgeViolation>.Empty,
        EventCoverage: ImmutableArray<EventCoverageEntry>.Empty,
        ProofFacts: ImmutableArray<ProofForwardingFact>.Empty,
        Diagnostics: ImmutableArray<Diagnostic>.Empty);
}

public sealed record GraphState(
    string Name,
    bool IsInitial,
    bool IsTerminal,
    bool IsRequired,
    bool IsIrreversible,
    bool IsReachable
);

public sealed record GraphEvent(
    string Name,
    bool IsInitial,
    ImmutableArray<string> HandledInStates
);

public sealed record GraphEdge(
    string FromState,
    string EventName,
    string ToState,
    bool HasGuard,
    TransitionRowOutcome Outcome
);

public sealed record EdgeProofStatus(
    string FromState,
    string EventName,
    string ToState,
    bool IsProven,
    ImmutableArray<string> UnresolvedObligationSummaries
);

public sealed record DominanceFact(
    string Dominator,
    string Dominated,
    int Distance
);

public sealed record TerminalOutgoingViolation(
    string StateName,
    ImmutableArray<GraphEdge> OutgoingEdges
);

public sealed record IrreversibleBackEdgeViolation(
    string StateName,
    GraphEdge BackEdge
);

public sealed record EventCoverageEntry(
    string EventName,
    ImmutableArray<string> HandlingStates,
    ImmutableArray<string> NonHandlingReachableStates
);

public abstract record ProofForwardingFact;

public sealed record ReachabilityFact(
    string StateName,
    bool IsReachable,
    ImmutableArray<string>? PathFromInitial
) : ProofForwardingFact;

public sealed record DominancePathFact(
    string RequiredState,
    ImmutableArray<string> DominatedTerminals
) : ProofForwardingFact;

public sealed record EventCoverageFact(
    string EventName,
    ImmutableArray<string> UnhandledReachableStates
) : ProofForwardingFact;

public sealed record TerminalCompletenessFact(
    bool AllTerminalsReachable,
    ImmutableArray<string> UnreachableTerminals
) : ProofForwardingFact;

public sealed record DeadEndStateFact(
    ImmutableArray<string> DeadEndStates,
    int DeadEndCount
) : ProofForwardingFact;
