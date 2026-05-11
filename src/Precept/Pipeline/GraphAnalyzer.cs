using System.Collections.Immutable;
using System.Linq;
using Precept.Language;

namespace Precept.Pipeline;

// CATALOG-DRIVEN IMPLEMENTATION GUIDE
//
// Before hardcoding state or event modifier semantics into graph algorithms,
// check catalogs:
//
//   State modifier structural semantics → StateModifierMeta.AllowsOutgoing,
//                                         RequiresDominator, PreventsBackEdge
//   Event modifier graph requirements  → EventModifierMeta.RequiredAnalysis
//                                         (e.g. GraphAnalysisKind.InitialEventCompatibility)
//   Modifier metadata lookup           → Modifiers.GetMeta(kind)
//
// Graph algorithms (reachability, dominator trees, SCCs) are generic machinery
// and stay hand-written. Only the *meaning* of modifiers is catalog-driven.
//
// See: docs/language/catalog-system.md § GraphAnalyzer-catalog integration pattern

public static class GraphAnalyzer
{
    public static StateGraph Analyze(SemanticIndex semantics)
    {
        var stateFlags = semantics.States
            .ToDictionary(state => state.Name, GetStateFlags, StringComparer.Ordinal);

        if (semantics.States.IsEmpty)
        {
            var statelessTerminalFact = new TerminalCompletenessFact(
                AllTerminalsReachable: true,
                UnreachableTerminals: ImmutableArray<string>.Empty);
            var statelessDeadEndFact = new DeadEndStateFact(
                DeadEndStates: ImmutableArray<string>.Empty,
                DeadEndCount: 0);
            var statelessCoverage = semantics.Events
                .Select(evt => new EventCoverageEntry(
                    evt.Name,
                    ImmutableArray<string>.Empty,
                    ImmutableArray<string>.Empty))
                .ToImmutableArray();

            return new StateGraph(
                States: ImmutableArray<GraphState>.Empty,
                Events: semantics.Events
                    .Select(evt => new GraphEvent(evt.Name, false, ImmutableArray<string>.Empty))
                    .ToImmutableArray(),
                Edges: ImmutableArray<GraphEdge>.Empty,
                ReachableStates: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
                UnreachableStates: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
                Dominance: ImmutableArray<DominanceFact>.Empty,
                TerminalViolations: ImmutableArray<TerminalOutgoingViolation>.Empty,
                BackEdgeViolations: ImmutableArray<IrreversibleBackEdgeViolation>.Empty,
                EventCoverage: statelessCoverage,
                ProofFacts: [statelessTerminalFact, statelessDeadEndFact],
                Diagnostics: ImmutableArray<Diagnostic>.Empty);
        }

        var edges = BuildEdges(semantics);
        var edgeIndex = edges.ToLookup(e => e.EventName, e => e.FromState, StringComparer.Ordinal);
        var adjacency = BuildAdjacency(semantics.States.Select(state => state.Name), edges, useReverseEdges: false);
        var reverseAdjacency = BuildAdjacency(semantics.States.Select(state => state.Name), edges, useReverseEdges: true);
        var declarationOrder = semantics.States
            .Select((state, index) => (state.Name, index))
            .ToDictionary(x => x.Name, x => x.index, StringComparer.Ordinal);

        var initialState = semantics.States.FirstOrDefault(state => stateFlags[state.Name].IsInitial);
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        ReachabilityResult reachability;
        if (initialState is null)
        {
            if (!HasDiagnostic(semantics.Diagnostics, DiagnosticCode.NoInitialState))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.NoInitialState,
                    semantics.States[0].NameSpan));
            }

            reachability = ReachabilityResult.AllUnreachable(semantics.States.Select(state => state.Name));
        }
        else
        {
            reachability = ComputeReachability(initialState.Name, semantics.States, adjacency);

            var unreachableStateSet = reachability.UnreachableOrdered.ToImmutableHashSet(StringComparer.Ordinal);
            foreach (var state in semantics.States.Where(state => unreachableStateSet.Contains(state.Name)))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UnreachableState,
                    state.NameSpan,
                    state.Name,
                    initialState.Name));
            }
        }

        var terminalStates = semantics.States
            .Where(state => stateFlags[state.Name].IsTerminal)
            .Select(state => state.Name)
            .ToImmutableArray();
        var terminalStateSet = terminalStates.ToImmutableHashSet(StringComparer.Ordinal);

        // Message A: structural sinks — reachable, non-terminal states with zero outgoing transitions.
        // Fires regardless of whether any terminal states are declared.
        var structuralSinkNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in semantics.States)
        {
            if (reachability.Reachable.Contains(state.Name)
                && !stateFlags[state.Name].IsTerminal
                && adjacency[state.Name].IsEmpty)
            {
                structuralSinkNames.Add(state.Name);
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.StructuralSinkState,
                    state.NameSpan,
                    state.Name));
            }
        }

        // Message B: no-path-to-terminal dead-ends, gated on at least one terminal state existing.
        // Structural sinks are excluded here — they are already reported by Message A.
        ImmutableArray<string> deadEndStates;
        if (terminalStates.Length > 0)
        {
            deadEndStates = ComputeDeadEnds(semantics.States, reachability.Reachable, terminalStateSet, reverseAdjacency);
            foreach (var deadEndState in semantics.States.Where(s => deadEndStates.Contains(s.Name) && !structuralSinkNames.Contains(s.Name)))
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.DeadEndState,
                    deadEndState.NameSpan,
                    deadEndState.Name));
            }
        }
        else
        {
            // No terminal states declared: BFS dead-end detection is vacuous (every state
            // would be flagged). Use only structural sinks so the ProofEngine still has
            // the right suppression set without false dead-end claims.
            deadEndStates = structuralSinkNames.ToImmutableArray();
        }

        var dominanceResult = initialState is null
            ? DominanceResult.Empty
            : ComputeDominance(initialState.Name, semantics.States, reachability.Reachable, reverseAdjacency, declarationOrder);

        var eventCoverage = BuildEventCoverage(semantics, reachability.Reachable, edgeIndex);
        foreach (var evt in semantics.Events)
        {
            if (semantics.States.Length > 0 && !eventCoverage.HandlingStatesByEvent[evt.Name].Any())
            {
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UnhandledEvent,
                    evt.NameSpan,
                    evt.Name));
            }
        }

        var graphStates = semantics.States
            .Select(state => new GraphState(
                Name: state.Name,
                IsInitial: stateFlags[state.Name].IsInitial,
                IsTerminal: stateFlags[state.Name].IsTerminal,
                IsRequired: stateFlags[state.Name].IsRequired,
                IsIrreversible: stateFlags[state.Name].IsIrreversible,
                IsReachable: reachability.Reachable.Contains(state.Name)))
            .ToImmutableArray();

        // Event modifier graph analysis dispatch
        // Mirrors the StateModifierMeta dispatch in GetStateFlags(). TypedEvent currently
        // carries IsInitial rather than a general modifier array, so derive the active
        // modifier set from the event surface before dispatching through catalog metadata.
        foreach (var evt in semantics.Events)
        {
            ImmutableArray<ModifierKind> eventModifiers = evt.IsInitial
                ? [ModifierKind.InitialEvent]
                : ImmutableArray<ModifierKind>.Empty;

            foreach (var modifier in eventModifiers)
            {
                if (Modifiers.GetMeta(modifier) is EventModifierMeta eventMeta
                    && eventMeta.RequiredAnalysis != GraphAnalysisKind.None)
                {
                    switch (eventMeta.RequiredAnalysis)
                    {
                        case GraphAnalysisKind.InitialEventCompatibility:
                            // Currently handled by the initialState logic above.
                            // When this analysis needs explicit dispatch, add it here.
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Unhandled GraphAnalysisKind: {eventMeta.RequiredAnalysis} for modifier {modifier}");
                    }
                }
            }
        }

        var graphEvents = semantics.Events
            .Select(evt => new GraphEvent(
                Name: evt.Name,
                IsInitial: initialState is not null && edgeIndex[evt.Name].Contains(initialState.Name),
                HandledInStates: eventCoverage.HandlingStatesByEvent[evt.Name]))
            .ToImmutableArray();

        var terminalViolations = BuildTerminalViolations(semantics.States, stateFlags, adjacency);
        var backEdgeViolations = BuildBackEdgeViolations(semantics.States, stateFlags, adjacency, reachability.Parents);

        foreach (var violation in terminalViolations)
        {
            var relatedSpans = CollectEdgeSpans(semantics, violation.OutgoingEdges);
            diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.TerminalStateHasOutgoingEdges,
                semantics.StatesByName[violation.StateName].NameSpan,
                violation.StateName) with { RelatedSpans = relatedSpans });
        }

        foreach (var violation in backEdgeViolations)
        {
            var relatedSpans = CollectEdgeSpans(semantics, [violation.BackEdge]);
            diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.IrreversibleStateHasBackEdge,
                semantics.StatesByName[violation.StateName].NameSpan,
                violation.StateName) with { RelatedSpans = relatedSpans });
        }

        var unreachableTerminals = semantics.States
            .Where(state => stateFlags[state.Name].IsTerminal && !reachability.Reachable.Contains(state.Name))
            .Select(state => state.Name)
            .ToImmutableArray();
        var terminalCompletenessFact = new TerminalCompletenessFact(
            AllTerminalsReachable: terminalStates.Length > 0 && unreachableTerminals.IsEmpty,
            UnreachableTerminals: unreachableTerminals);
        var deadEndStateFact = new DeadEndStateFact(
            DeadEndStates: deadEndStates,
            DeadEndCount: deadEndStates.Length);

        var proofFacts = ImmutableArray.CreateBuilder<ProofForwardingFact>();
        foreach (var state in semantics.States)
        {
            proofFacts.Add(new ReachabilityFact(
                StateName: state.Name,
                IsReachable: reachability.Reachable.Contains(state.Name),
                PathFromInitial: reachability.Paths.TryGetValue(state.Name, out var path) ? path : null));
        }

        foreach (var state in semantics.States.Where(state => stateFlags[state.Name].IsRequired))
        {
            var dominatedTerminals = terminalStates
                .Where(terminal => dominanceResult.Dominators.TryGetValue(terminal, out var dominators) && dominators.Contains(state.Name))
                .ToImmutableArray();
            proofFacts.Add(new DominancePathFact(state.Name, dominatedTerminals));

            if (dominatedTerminals.IsEmpty)
            {
                var undominatedTerminalSpans = terminalStates
                    .Where(terminal => !dominatedTerminals.Contains(terminal))
                    .Select(terminal => new RelatedSpan(semantics.StatesByName[terminal].NameSpan, $"terminal state '{terminal}' not dominated"))
                    .ToImmutableArray();
                diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.RequiredStateDoesNotDominateTerminal,
                    semantics.StatesByName[state.Name].NameSpan,
                    state.Name) with { RelatedSpans = undominatedTerminalSpans });
            }
        }

        foreach (var coverage in eventCoverage.Entries.Where(entry => !entry.NonHandlingReachableStates.IsEmpty))
        {
            proofFacts.Add(new EventCoverageFact(coverage.EventName, coverage.NonHandlingReachableStates));
        }

        proofFacts.Add(terminalCompletenessFact);
        proofFacts.Add(deadEndStateFact);

        return new StateGraph(
            States: graphStates,
            Events: graphEvents,
            Edges: edges,
            ReachableStates: reachability.Reachable.ToImmutableHashSet(StringComparer.Ordinal),
            UnreachableStates: reachability.UnreachableOrdered.ToImmutableHashSet(StringComparer.Ordinal),
            Dominance: dominanceResult.Facts,
            TerminalViolations: terminalViolations,
            BackEdgeViolations: backEdgeViolations,
            EventCoverage: eventCoverage.Entries,
            ProofFacts: proofFacts.ToImmutable(),
            Diagnostics: diagnostics.ToImmutable());
    }

    private static ImmutableArray<GraphEdge> BuildEdges(SemanticIndex semantics)
    {
        var explicitStateEvents = semantics.TransitionRows
            .Where(row => row.FromState is not null
                && semantics.StatesByName.ContainsKey(row.FromState)
                && semantics.EventsByName.ContainsKey(row.EventName))
            .Select(row => (row.FromState!, row.EventName))
            .ToHashSet();

        var edges = ImmutableArray.CreateBuilder<GraphEdge>();
        foreach (var row in semantics.TransitionRows)
        {
            if (!semantics.EventsByName.ContainsKey(row.EventName))
            {
                continue;
            }

            if (row.FromState is not null)
            {
                TryAddEdge(semantics, row, row.FromState, edges);
                continue;
            }

            foreach (var state in semantics.States)
            {
                if (explicitStateEvents.Contains((state.Name, row.EventName)))
                {
                    continue;
                }

                TryAddEdge(semantics, row, state.Name, edges);
            }
        }

        return edges.ToImmutable();
    }

    private static void TryAddEdge(
        SemanticIndex semantics,
        TypedTransitionRow row,
        string fromState,
        ImmutableArray<GraphEdge>.Builder edges)
    {
        if (!semantics.StatesByName.ContainsKey(fromState))
        {
            return;
        }

        string? toState = row.Outcome switch
        {
            TransitionRowOutcome.Transition when row.TargetState is not null && semantics.StatesByName.ContainsKey(row.TargetState)
                => row.TargetState,
            TransitionRowOutcome.NoTransition or TransitionRowOutcome.Reject
                => fromState,
            _ => null,
        };

        if (toState is null)
        {
            return;
        }

        edges.Add(new GraphEdge(
            FromState: fromState,
            EventName: row.EventName,
            ToState: toState,
            HasGuard: row.Guard is not null,
            Outcome: row.Outcome));
    }

    private static Dictionary<string, ImmutableArray<GraphEdge>> BuildAdjacency(
        IEnumerable<string> stateNames,
        ImmutableArray<GraphEdge> edges,
        bool useReverseEdges)
    {
        var builders = stateNames.ToDictionary(
            stateName => stateName,
            _ => ImmutableArray.CreateBuilder<GraphEdge>(),
            StringComparer.Ordinal);

        foreach (var edge in edges)
        {
            var key = useReverseEdges ? edge.ToState : edge.FromState;
            builders[key].Add(edge);
        }

        return builders.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToImmutable(),
            StringComparer.Ordinal);
    }

    private static ReachabilityResult ComputeReachability(
        string initialStateName,
        ImmutableArray<TypedState> states,
        Dictionary<string, ImmutableArray<GraphEdge>> adjacency)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { initialStateName };
        var queue = new Queue<string>();
        queue.Enqueue(initialStateName);

        var orderedReachable = ImmutableArray.CreateBuilder<string>();
        orderedReachable.Add(initialStateName);

        var paths = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal)
        {
            [initialStateName] = ImmutableArray<string>.Empty,
        };
        var parents = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [initialStateName] = null,
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in adjacency[current])
            {
                if (!visited.Add(edge.ToState))
                {
                    continue;
                }

                queue.Enqueue(edge.ToState);
                orderedReachable.Add(edge.ToState);
                parents[edge.ToState] = current;
                paths[edge.ToState] = paths[current].Add(current);
            }
        }

        var unreachableOrdered = states
            .Select(state => state.Name)
            .Where(stateName => !visited.Contains(stateName))
            .ToImmutableArray();

        return new ReachabilityResult(
            Reachable: visited,
            ReachableOrdered: orderedReachable.ToImmutable(),
            UnreachableOrdered: unreachableOrdered,
            Paths: paths,
            Parents: parents);
    }

    private static ImmutableArray<string> ComputeDeadEnds(
        ImmutableArray<TypedState> states,
        HashSet<string> reachableStates,
        ImmutableHashSet<string> terminalStates,
        Dictionary<string, ImmutableArray<GraphEdge>> reverseAdjacency)
    {
        var reverseVisited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();

        foreach (var terminalState in terminalStates)
        {
            if (reverseVisited.Add(terminalState))
            {
                queue.Enqueue(terminalState);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in reverseAdjacency[current])
            {
                if (reverseVisited.Add(edge.FromState))
                {
                    queue.Enqueue(edge.FromState);
                }
            }
        }

        return states
            .Select(state => state.Name)
            .Where(stateName => reachableStates.Contains(stateName)
                && !terminalStates.Contains(stateName)
                && !reverseVisited.Contains(stateName))
            .ToImmutableArray();
    }

    private static DominanceResult ComputeDominance(
        string initialStateName,
        ImmutableArray<TypedState> states,
        HashSet<string> reachableStates,
        Dictionary<string, ImmutableArray<GraphEdge>> reverseAdjacency,
        Dictionary<string, int> declarationOrder)
    {
        var reachableOrdered = states
            .Select(state => state.Name)
            .Where(reachableStates.Contains)
            .ToImmutableArray();

        var dominators = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var stateName in reachableOrdered)
        {
            dominators[stateName] = stateName == initialStateName
                ? new HashSet<string>(StringComparer.Ordinal) { initialStateName }
                : new HashSet<string>(reachableOrdered, StringComparer.Ordinal);
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var stateName in reachableOrdered.Where(name => name != initialStateName))
            {
                var predecessors = reverseAdjacency[stateName]
                    .Select(edge => edge.FromState)
                    .Where(reachableStates.Contains)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var newDominators = predecessors.Count == 0
                    ? new HashSet<string>(StringComparer.Ordinal)
                    : new HashSet<string>(dominators[predecessors[0]], StringComparer.Ordinal);

                foreach (var predecessor in predecessors.Skip(1))
                {
                    newDominators.IntersectWith(dominators[predecessor]);
                }

                newDominators.Add(stateName);
                if (!newDominators.SetEquals(dominators[stateName]))
                {
                    dominators[stateName] = newDominators;
                    changed = true;
                }
            }
        }

        var immediateDominators = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [initialStateName] = null,
        };

        foreach (var stateName in reachableOrdered.Where(name => name != initialStateName))
        {
            var idom = dominators[stateName]
                .Where(dominator => dominator != stateName)
                .OrderByDescending(dominator => dominators[dominator].Count)
                .ThenBy(dominator => declarationOrder[dominator])
                .FirstOrDefault();
            immediateDominators[stateName] = idom;
        }

        var facts = ImmutableArray.CreateBuilder<DominanceFact>();
        foreach (var stateName in reachableOrdered)
        {
            var distance = 1;
            var current = immediateDominators[stateName];
            while (current is not null)
            {
                facts.Add(new DominanceFact(current, stateName, distance));
                current = immediateDominators.TryGetValue(current, out var parent) ? parent : null;
                distance++;
            }
        }

        return new DominanceResult(dominators, facts.ToImmutable());
    }

    private static EventCoverageResult BuildEventCoverage(
        SemanticIndex semantics,
        HashSet<string> reachableStates,
        ILookup<string, string> edgeIndex)
    {
        var entries = ImmutableArray.CreateBuilder<EventCoverageEntry>();
        var handlingStatesByEvent = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);

        foreach (var evt in semantics.Events)
        {
            var eventFromStates = edgeIndex[evt.Name].ToHashSet(StringComparer.Ordinal);
            var handlingStates = semantics.States
                .Select(state => state.Name)
                .Where(stateName => eventFromStates.Contains(stateName))
                .ToImmutableArray();
            var handlingStateSet = handlingStates.ToImmutableHashSet(StringComparer.Ordinal);
            var nonHandlingReachableStates = semantics.States
                .Select(state => state.Name)
                .Where(stateName => reachableStates.Contains(stateName) && !handlingStateSet.Contains(stateName))
                .ToImmutableArray();

            handlingStatesByEvent[evt.Name] = handlingStates;
            entries.Add(new EventCoverageEntry(evt.Name, handlingStates, nonHandlingReachableStates));
        }

        return new EventCoverageResult(entries.ToImmutable(), handlingStatesByEvent);
    }

    private static ImmutableArray<TerminalOutgoingViolation> BuildTerminalViolations(
        ImmutableArray<TypedState> states,
        Dictionary<string, StateFlags> stateFlags,
        Dictionary<string, ImmutableArray<GraphEdge>> adjacency)
    {
        var violations = ImmutableArray.CreateBuilder<TerminalOutgoingViolation>();
        foreach (var state in states.Where(state => stateFlags[state.Name].IsTerminal))
        {
            var outgoing = adjacency[state.Name]
                .Where(edge => edge.ToState != edge.FromState)
                .ToImmutableArray();
            if (!outgoing.IsEmpty)
            {
                violations.Add(new TerminalOutgoingViolation(state.Name, outgoing));
            }
        }

        return violations.ToImmutable();
    }

    private static ImmutableArray<IrreversibleBackEdgeViolation> BuildBackEdgeViolations(
        ImmutableArray<TypedState> states,
        Dictionary<string, StateFlags> stateFlags,
        Dictionary<string, ImmutableArray<GraphEdge>> adjacency,
        Dictionary<string, string?> parents)
    {
        var violations = ImmutableArray.CreateBuilder<IrreversibleBackEdgeViolation>();
        foreach (var state in states.Where(state => stateFlags[state.Name].IsIrreversible))
        {
            foreach (var edge in adjacency[state.Name])
            {
                if (IsBfsAncestor(edge.ToState, state.Name, parents))
                {
                    violations.Add(new IrreversibleBackEdgeViolation(state.Name, edge));
                }
            }
        }

        return violations.ToImmutable();
    }

    private static bool IsBfsAncestor(
        string candidateAncestor,
        string stateName,
        Dictionary<string, string?> parents)
    {
        if (!parents.TryGetValue(stateName, out var current))
        {
            return false;
        }

        while (current is not null)
        {
            if (current == candidateAncestor)
            {
                return true;
            }

            current = parents.TryGetValue(current, out var parent) ? parent : null;
        }

        return false;
    }

    private static StateFlags GetStateFlags(TypedState state)
    {
        var isTerminal = false;
        var isRequired = false;
        var isIrreversible = false;

        foreach (var modifier in state.Modifiers)
        {
            if (Modifiers.GetMeta(modifier) is not StateModifierMeta stateMeta)
            {
                continue;
            }

            isTerminal |= !stateMeta.AllowsOutgoing;
            isRequired |= stateMeta.RequiresDominator;
            isIrreversible |= stateMeta.PreventsBackEdge;
        }

        return new StateFlags(
            // IsInitial identifies the BFS entry point — a topological role, not a structural
            // constraint. Unlike terminal/required/irreversible, it is not derived from
            // StateModifierMeta because no catalog flag governs traversal entry; the initial
            // modifier's meaning is "this is where graph traversal starts."
            IsInitial: state.Modifiers.Contains(ModifierKind.InitialState),
            IsTerminal: isTerminal,
            IsRequired: isRequired,
            IsIrreversible: isIrreversible);
    }

    private readonly record struct StateFlags(
        bool IsInitial,
        bool IsTerminal,
        bool IsRequired,
        bool IsIrreversible);

    private sealed record ReachabilityResult(
        HashSet<string> Reachable,
        ImmutableArray<string> ReachableOrdered,
        ImmutableArray<string> UnreachableOrdered,
        Dictionary<string, ImmutableArray<string>> Paths,
        Dictionary<string, string?> Parents)
    {
        public static ReachabilityResult AllUnreachable(IEnumerable<string> stateNames) =>
            new(
                Reachable: new HashSet<string>(StringComparer.Ordinal),
                ReachableOrdered: ImmutableArray<string>.Empty,
                UnreachableOrdered: stateNames.ToImmutableArray(),
                Paths: new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal),
                Parents: new Dictionary<string, string?>(StringComparer.Ordinal));
    }

    private sealed record DominanceResult(
        Dictionary<string, HashSet<string>> Dominators,
        ImmutableArray<DominanceFact> Facts)
    {
        public static DominanceResult Empty { get; } = new(
            Dominators: new Dictionary<string, HashSet<string>>(StringComparer.Ordinal),
            Facts: ImmutableArray<DominanceFact>.Empty);
    }

    private sealed record EventCoverageResult(
        ImmutableArray<EventCoverageEntry> Entries,
        Dictionary<string, ImmutableArray<string>> HandlingStatesByEvent);

    /// <summary>
    /// Collects <see cref="RelatedSpan"/> entries for structural violation edges.
    /// Uses event and target-state <see cref="TypedState.NameSpan"/>/<see cref="TypedEvent.NameSpan"/>
    /// because <see cref="TypedTransitionRow.Syntax"/> is blocked by PRECEPT0024 outside the TypeChecker.
    /// </summary>
    private static ImmutableArray<RelatedSpan> CollectEdgeSpans(
        SemanticIndex semantics,
        ImmutableArray<GraphEdge> violatingEdges)
    {
        var spans = ImmutableArray.CreateBuilder<RelatedSpan>();

        foreach (var edge in violatingEdges)
        {
            if (semantics.EventsByName.TryGetValue(edge.EventName, out var evt))
            {
                spans.Add(new RelatedSpan(evt.NameSpan, $"event '{edge.EventName}' causes transition to '{edge.ToState}'"));
            }
        }

        return spans.ToImmutable();
    }

    /// <summary>
    /// Typed cross-stage diagnostic lookup — avoids fragile string literals by deriving
    /// the code name from the <see cref="DiagnosticCode"/> enum value, matching the
    /// <c>nameof(DiagnosticCode.X)</c> convention used by <see cref="Diagnostics.Create"/>.
    /// </summary>
    private static bool HasDiagnostic(ImmutableArray<Diagnostic> diagnostics, DiagnosticCode code)
    {
        var codeString = code.ToString();
        return diagnostics.Any(d => d.Code == codeString);
    }
}

