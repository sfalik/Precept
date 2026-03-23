using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class AuditTool
{
    [McpServerTool(Name = "precept_audit")]
    [Description("Graph analysis of a precept — reachability, dead ends, terminal states, orphaned events.")]
    public static AuditResult Run(
        [Description("Path to the .precept file")] string path)
    {
        if (!File.Exists(path))
            return AuditResult.WithError($"File not found: {path}");

        var text = File.ReadAllText(path);
        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(text);

        if (model is null || diagnostics.Count > 0)
        {
            return AuditResult.WithError(
                string.Join("; ", diagnostics.Select(d => d.Message)));
        }

        var allStates = model.States.Select(s => s.Name).ToList();
        var transitionRows = model.TransitionRows ?? [];

        // Build directed graph: edges from source state to target states
        var graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var state in allStates)
            graph[state] = [];

        // Track which events are referenced in transition rows
        var referencedEvents = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in transitionRows)
        {
            referencedEvents.Add(row.EventName);

            if (row.Outcome is StateTransition t)
            {
                // "any" means all states — expand
                if (string.Equals(row.FromState, "any", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var state in allStates)
                        graph[state].Add(t.TargetState);
                }
                else
                {
                    // FromState could be comma-separated (handled by parser as separate rows)
                    if (graph.ContainsKey(row.FromState))
                        graph[row.FromState].Add(t.TargetState);
                }
            }
        }

        // BFS from initial state
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(model.InitialState.Name);
        reachable.Add(model.InitialState.Name);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (graph.TryGetValue(current, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (reachable.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
        }

        var unreachable = allStates.Where(s => !reachable.Contains(s)).ToList();

        // Terminal states: states with zero outgoing transitions
        var statesWithOutgoing = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in transitionRows)
        {
            if (string.Equals(row.FromState, "any", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var s in allStates) statesWithOutgoing.Add(s);
            }
            else
            {
                statesWithOutgoing.Add(row.FromState);
            }
        }
        var terminal = allStates.Where(s => !statesWithOutgoing.Contains(s)).ToList();

        // Dead-end states: non-terminal states where all outgoing rows have NoTransition or Rejection only
        var deadEnds = new List<string>();
        foreach (var state in allStates)
        {
            if (terminal.Contains(state)) continue;

            var outgoingRows = transitionRows.Where(r =>
                string.Equals(r.FromState, state, StringComparison.Ordinal) ||
                string.Equals(r.FromState, "any", StringComparison.OrdinalIgnoreCase)).ToList();

            if (outgoingRows.Count > 0 && outgoingRows.All(r => r.Outcome is NoTransition or Rejection))
                deadEnds.Add(state);
        }

        // Orphaned events: declared but not referenced in any transition row
        var declaredEvents = model.Events.Select(e => e.Name).ToList();
        var orphaned = declaredEvents.Where(e => !referencedEvents.Contains(e)).ToList();

        // Build warnings
        var warnings = new List<string>();
        if (unreachable.Count > 0)
            warnings.Add($"Unreachable states: {string.Join(", ", unreachable)}");
        if (deadEnds.Count > 0)
            warnings.Add($"Dead-end states (all outcomes reject or no-transition): {string.Join(", ", deadEnds)}");
        if (orphaned.Count > 0)
            warnings.Add($"Orphaned events (declared but never referenced in transitions): {string.Join(", ", orphaned)}");

        return new AuditResult(
            allStates, reachable.ToList(), unreachable, terminal, deadEnds, orphaned, warnings, null);
    }
}

public sealed record AuditResult(
    IReadOnlyList<string> AllStates,
    IReadOnlyList<string> ReachableStates,
    IReadOnlyList<string> UnreachableStates,
    IReadOnlyList<string> TerminalStates,
    IReadOnlyList<string> DeadEndStates,
    IReadOnlyList<string> OrphanedEvents,
    IReadOnlyList<string> Warnings,
    string? Error)
{
    public static AuditResult WithError(string message) =>
        new([], [], [], [], [], [], [], message);
}
