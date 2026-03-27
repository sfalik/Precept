using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

internal sealed record RejectOnlyStateEventIssue(
    string StateName,
    string EventName,
    int SourceLine,
    bool EventSucceedsElsewhere);

internal sealed record OrphanedEventIssue(
    string EventName,
    int SourceLine);

internal sealed record AnalysisResult(
    IReadOnlyList<PreceptValidationDiagnostic> Diagnostics,
    IReadOnlyList<string> ReachableStates,
    IReadOnlyList<string> UnreachableStates,
    IReadOnlyList<string> TerminalStates,
    IReadOnlyList<string> DeadEndStates,
    IReadOnlyList<RejectOnlyStateEventIssue> RejectOnlyStateEventIssues,
    IReadOnlyList<OrphanedEventIssue> OrphanedEventIssues,
    IReadOnlyList<string> EventsThatNeverSucceed,
    bool IsEmptyPrecept);

internal static class PreceptAnalysis
{
    public static AnalysisResult Analyze(PreceptDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var diagnostics = new List<PreceptValidationDiagnostic>();
        var states = definition.States ?? Array.Empty<PreceptState>();
        var events = definition.Events ?? Array.Empty<PreceptEvent>();
        var transitionRows = definition.TransitionRows ?? Array.Empty<PreceptTransitionRow>();

        var allStateNames = states.Select(static state => state.Name).ToArray();
        var statesByName = states.ToDictionary(static state => state.Name, StringComparer.Ordinal);
        var graph = allStateNames.ToDictionary(static state => state, static _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        var rowsByPair = new Dictionary<(string State, string Event), List<PreceptTransitionRow>>();
        var statesWithOutgoing = new HashSet<string>(StringComparer.Ordinal);
        var referencedEvents = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in transitionRows)
        {
            referencedEvents.Add(row.EventName);

            foreach (var sourceState in ExpandSourceStates(row.FromState, allStateNames))
            {
                statesWithOutgoing.Add(sourceState);

                var key = (sourceState, row.EventName);
                if (!rowsByPair.TryGetValue(key, out var rows))
                {
                    rows = new List<PreceptTransitionRow>();
                    rowsByPair[key] = rows;
                }

                rows.Add(row);

                if (row.Outcome is StateTransition transition && graph.TryGetValue(sourceState, out var neighbors))
                    neighbors.Add(transition.TargetState);
            }
        }

        var reachable = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(definition.InitialState.Name) && graph.ContainsKey(definition.InitialState.Name))
        {
            var queue = new Queue<string>();
            queue.Enqueue(definition.InitialState.Name);
            reachable.Add(definition.InitialState.Name);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in graph[current])
                {
                    if (reachable.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
        }

        var unreachableStates = allStateNames.Where(state => !reachable.Contains(state)).ToArray();
        foreach (var stateName in unreachableStates)
        {
            var state = statesByName[stateName];
            // SYNC:CONSTRAINT:C48
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C48,
                DiagnosticCatalog.C48.FormatMessage(("State", stateName)),
                state.SourceLine));
        }

        var orphanedEvents = events
            .Where(evt => !referencedEvents.Contains(evt.Name))
            .Select(evt => new OrphanedEventIssue(evt.Name, evt.SourceLine))
            .ToArray();
        foreach (var orphanedEvent in orphanedEvents)
        {
            // SYNC:CONSTRAINT:C49
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C49,
                DiagnosticCatalog.C49.FormatMessage(("Event", orphanedEvent.EventName)),
                orphanedEvent.SourceLine));
        }

        var terminalStates = allStateNames.Where(state => !statesWithOutgoing.Contains(state)).ToArray();

        var deadEndStates = allStateNames
            .Where(state => !terminalStates.Contains(state, StringComparer.Ordinal))
            .Where(state => rowsByPair.Keys.Any(key => StringComparer.Ordinal.Equals(key.State, state)))
            .Where(state => rowsByPair
                .Where(pair => StringComparer.Ordinal.Equals(pair.Key.State, state))
                .SelectMany(pair => pair.Value)
                .All(static row => row.Outcome is NoTransition or Rejection))
            .ToArray();
        foreach (var stateName in deadEndStates)
        {
            var state = statesByName[stateName];
            // SYNC:CONSTRAINT:C50
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C50,
                DiagnosticCatalog.C50.FormatMessage(("State", stateName)),
                state.SourceLine));
        }

        var eventHasSuccessfulPath = rowsByPair
            .GroupBy(static pair => pair.Key.Event, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(static pair => pair.Value).Any(static row => row.Outcome is not Rejection),
                StringComparer.Ordinal);

        var rejectOnlyPairs = rowsByPair
            .Where(static pair => pair.Value.Count > 0)
            .Where(static pair => pair.Value.All(static row => row.Outcome is Rejection))
            .Select(pair =>
            {
                var firstRow = pair.Value.OrderBy(static row => row.SourceLine).First();
                return new RejectOnlyStateEventIssue(
                    pair.Key.State,
                    pair.Key.Event,
                    firstRow.SourceLine,
                    eventHasSuccessfulPath.TryGetValue(pair.Key.Event, out var succeedsElsewhere) && succeedsElsewhere);
            })
            .OrderBy(static issue => issue.SourceLine)
            .ToArray();
        foreach (var issue in rejectOnlyPairs)
        {
            // SYNC:CONSTRAINT:C51
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C51,
                DiagnosticCatalog.C51.FormatMessage(("State", issue.StateName), ("Event", issue.EventName)),
                issue.SourceLine));
        }

        var eventsThatNeverSucceed = events
            .Where(evt => referencedEvents.Contains(evt.Name))
            .Where(evt => reachable.All(state =>
            {
                var key = (state, evt.Name);
                return !rowsByPair.TryGetValue(key, out var rows) || rows.All(static row => row.Outcome is Rejection);
            }))
            .Select(static evt => evt.Name)
            .ToArray();
        foreach (var eventName in eventsThatNeverSucceed)
        {
            var evt = events.First(evt => evt.Name == eventName);
            // SYNC:CONSTRAINT:C52
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C52,
                DiagnosticCatalog.C52.FormatMessage(("Event", eventName)),
                evt.SourceLine));
        }

        var isEmptyPrecept = events.Count == 0;
        if (isEmptyPrecept)
        {
            // SYNC:CONSTRAINT:C53
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C53,
                DiagnosticCatalog.C53.FormatMessage(("Name", definition.Name)),
                definition.SourceLine));
        }

        return new AnalysisResult(
            diagnostics,
            reachable.OrderBy(static state => state, StringComparer.Ordinal).ToArray(),
            unreachableStates,
            terminalStates,
            deadEndStates,
            rejectOnlyPairs,
            orphanedEvents,
            eventsThatNeverSucceed,
            isEmptyPrecept);
    }

    private static IReadOnlyList<string> ExpandSourceStates(string fromState, IReadOnlyList<string> allStates)
    {
        if (string.Equals(fromState, "any", StringComparison.OrdinalIgnoreCase))
            return allStates;

        return [fromState];
    }
}