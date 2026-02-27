using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StateMachine.Dsl;

public static class StateMachineDslParser
{
    private static readonly Regex MachineRegex = new("^machine\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new("^state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("^event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex TypedEventRegex = new("^event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*\\((?<arg>[A-Za-z_][A-Za-z0-9_<>., ]*)\\)$", RegexOptions.Compiled);
    private static readonly Regex TransitionRegex = new(
        "^transition\\s+(?<from>[A-Za-z_][A-Za-z0-9_]*)\\s*->\\s*(?<to>[A-Za-z_][A-Za-z0-9_]*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)(?:\\s+when\\s+(?<guard>.*?))?(?:\\s+set\\s+(?<setKey>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<setExpr>.+))?$",
        RegexOptions.Compiled);

    public static DslMachine Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("DSL input is empty.");

        string? name = null;
        var states = new List<string>();
        var events = new List<DslEvent>();
        var transitions = new List<DslTransition>();

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var machineMatch = MachineRegex.Match(line);
            if (machineMatch.Success)
            {
                if (name != null)
                    throw new InvalidOperationException($"Line {i + 1}: machine already declared.");

                name = machineMatch.Groups["name"].Value;
                continue;
            }

            var stateMatch = StateRegex.Match(line);
            if (stateMatch.Success)
            {
                string stateName = stateMatch.Groups["name"].Value;
                if (states.Contains(stateName, StringComparer.Ordinal))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate state '{stateName}'.");

                states.Add(stateName);
                continue;
            }

            var eventMatch = EventRegex.Match(line);
            if (eventMatch.Success)
            {
                string eventName = eventMatch.Groups["name"].Value;

                if (events.Any(e => e.Name.Equals(eventName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate event '{eventName}'.");

                events.Add(new DslEvent(eventName));
                continue;
            }

            var typedEventMatch = TypedEventRegex.Match(line);
            if (typedEventMatch.Success)
            {
                var eventName = typedEventMatch.Groups["name"].Value;
                throw new InvalidOperationException($"Line {i + 1}: typed event arguments are deprecated. Use 'event {eventName}' and infer required keys from transition transforms.");
            }

            var transitionMatch = TransitionRegex.Match(line);
            if (transitionMatch.Success)
            {
                transitions.Add(new DslTransition(
                    transitionMatch.Groups["from"].Value,
                    transitionMatch.Groups["to"].Value,
                    transitionMatch.Groups["event"].Value,
                    transitionMatch.Groups["guard"].Success ? transitionMatch.Groups["guard"].Value.Trim() : null,
                    transitionMatch.Groups["setKey"].Success ? transitionMatch.Groups["setKey"].Value.Trim() : null,
                    transitionMatch.Groups["setExpr"].Success ? transitionMatch.Groups["setExpr"].Value.Trim() : null));
                continue;
            }

            throw new InvalidOperationException($"Line {i + 1}: unrecognized statement '{line}'.");
        }

        if (name == null)
            throw new InvalidOperationException("Missing 'machine <Name>' declaration.");

        if (states.Count == 0)
            throw new InvalidOperationException("At least one state must be declared.");

        if (events.Count == 0)
            throw new InvalidOperationException("At least one event must be declared.");

        ValidateReferences(states, events, transitions);

        return new DslMachine(name, states, events, transitions);
    }

    private static void ValidateReferences(
        IReadOnlyCollection<string> states,
        IReadOnlyCollection<DslEvent> events,
        IReadOnlyCollection<DslTransition> transitions)
    {
        var stateSet = new HashSet<string>(states, StringComparer.Ordinal);
        var eventSet = new HashSet<string>(events.Select(e => e.Name), StringComparer.Ordinal);

        foreach (var transition in transitions)
        {
            if (!stateSet.Contains(transition.FromState))
                throw new InvalidOperationException($"Transition references unknown source state '{transition.FromState}'.");

            if (!stateSet.Contains(transition.ToState))
                throw new InvalidOperationException($"Transition references unknown target state '{transition.ToState}'.");

            if (!eventSet.Contains(transition.EventName))
                throw new InvalidOperationException($"Transition references unknown event '{transition.EventName}'.");

            if (!string.IsNullOrWhiteSpace(transition.DataAssignmentExpression) &&
                transition.DataAssignmentExpression.StartsWith("data.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Transition '{transition.FromState} -> {transition.ToState}' on '{transition.EventName}' uses unsupported transform expression '{transition.DataAssignmentExpression}'. Use a bare event-argument key (for example, 'Reason') or a literal.");
            }

            if (!string.IsNullOrWhiteSpace(transition.DataAssignmentExpression) &&
                transition.DataAssignmentExpression.StartsWith("arg.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Transition '{transition.FromState} -> {transition.ToState}' on '{transition.EventName}' uses deprecated transform expression '{transition.DataAssignmentExpression}'. Use a bare event-argument key (for example, 'Reason').");
            }
        }
    }
}
