using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StateMachine.Dsl;

public static class StateMachineDslParser
{
    private static readonly Regex MachineRegex = new("^machine\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new("^state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex StatesRegex = new("^states\\s+(?<list>.+)$", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("^event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex EventsRegex = new("^events\\s+(?<list>.+)$", RegexOptions.Compiled);
    private static readonly Regex TypedEventRegex = new("^event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*\\((?<arg>[A-Za-z_][A-Za-z0-9_<>., ]*)\\)$", RegexOptions.Compiled);
    private static readonly Regex TransitionRegex = new(
        "^transition\\s+(?<from>[A-Za-z_][A-Za-z0-9_]*)\\s*->\\s*(?<to>[A-Za-z_][A-Za-z0-9_]*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)(?:\\s+set\\s+(?<setKey>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<setExpr>.+))?$",
        RegexOptions.Compiled);
    private static readonly Regex FromOnRegex = new(
        "^from\\s+(?<from>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)$",
        RegexOptions.Compiled);
    private static readonly Regex IfRegex = new(
        "^if\\s+(?<guard>.+?)(?:\\s+reason\\s+\"(?<reason>[^\"]+)\")?$",
        RegexOptions.Compiled);
    private static readonly Regex ElseIfRegex = new(
        "^else\\s+if\\s+(?<guard>.+?)(?:\\s+reason\\s+\"(?<reason>[^\"]+)\")?$",
        RegexOptions.Compiled);
    private static readonly Regex ElseRegex = new("^else$", RegexOptions.Compiled);
    private static readonly Regex TransformRegex = new(
        "^(?:transform|set)\\s+(?<setKey>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<setExpr>.+)$",
        RegexOptions.Compiled);
    private static readonly Regex SimpleTransitionRegex = new(
        "^transition\\s+(?<to>[A-Za-z_][A-Za-z0-9_]*)$",
        RegexOptions.Compiled);
    private static readonly Regex RejectRegex = new("^reject\\s+(?<reason>.+)$", RegexOptions.Compiled);

    public static DslMachine Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("DSL input is empty.");

        string? name = null;
        var states = new List<string>();
        var events = new List<DslEvent>();
        var transitions = new List<DslTransition>();
        var terminalRules = new List<DslTerminalRule>();

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int i = 0;
        while (i < lines.Length)
        {
            var raw = lines[i];
            string line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                i++;
                continue;
            }

            var machineMatch = MachineRegex.Match(line);
            if (machineMatch.Success)
            {
                if (name != null)
                    throw new InvalidOperationException($"Line {i + 1}: machine already declared.");

                name = machineMatch.Groups["name"].Value;
                i++;
                continue;
            }

            var statesMatch = StatesRegex.Match(line);
            if (statesMatch.Success)
            {
                foreach (var stateName in ParseIdentifierList(statesMatch.Groups["list"].Value, i + 1, "state"))
                {
                    if (states.Contains(stateName, StringComparer.Ordinal))
                        throw new InvalidOperationException($"Line {i + 1}: duplicate state '{stateName}'.");

                    states.Add(stateName);
                }

                i++;
                continue;
            }

            var stateMatch = StateRegex.Match(line);
            if (stateMatch.Success)
            {
                string stateName = stateMatch.Groups["name"].Value;
                if (states.Contains(stateName, StringComparer.Ordinal))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate state '{stateName}'.");

                states.Add(stateName);
                i++;
                continue;
            }

            var eventsMatch = EventsRegex.Match(line);
            if (eventsMatch.Success)
            {
                foreach (var eventName in ParseIdentifierList(eventsMatch.Groups["list"].Value, i + 1, "event"))
                {
                    if (events.Any(e => string.Equals(e.Name, eventName, StringComparison.Ordinal)))
                        throw new InvalidOperationException($"Line {i + 1}: duplicate event '{eventName}'.");

                    events.Add(new DslEvent(eventName));
                }

                i++;
                continue;
            }

            var eventMatch = EventRegex.Match(line);
            if (eventMatch.Success)
            {
                string eventName = eventMatch.Groups["name"].Value;

                if (events.Any(e => string.Equals(e.Name, eventName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate event '{eventName}'.");

                events.Add(new DslEvent(eventName));
                i++;
                continue;
            }

            var typedEventMatch = TypedEventRegex.Match(line);
            if (typedEventMatch.Success)
            {
                var eventName = typedEventMatch.Groups["name"].Value;
                throw new InvalidOperationException($"Line {i + 1}: typed event arguments are deprecated. Use 'event {eventName}' and infer required keys from transition transforms.");
            }

            var fromOnMatch = FromOnRegex.Match(line);
            if (fromOnMatch.Success)
            {
                ParseFromOnBlock(
                    lines,
                    ref i,
                    fromOnMatch,
                    states,
                    transitions,
                    terminalRules);
                continue;
            }

            var transitionMatch = TransitionRegex.Match(line);
            if (transitionMatch.Success)
            {
                transitions.Add(new DslTransition(
                    transitionMatch.Groups["from"].Value,
                    transitionMatch.Groups["to"].Value,
                    transitionMatch.Groups["event"].Value,
                    null,
                    transitionMatch.Groups["setKey"].Success ? transitionMatch.Groups["setKey"].Value.Trim() : null,
                    transitionMatch.Groups["setExpr"].Success ? transitionMatch.Groups["setExpr"].Value.Trim() : null));
                i++;
                continue;
            }

            if (line.StartsWith("transition", StringComparison.Ordinal) && line.Contains(" reason ", StringComparison.Ordinal))
                throw new InvalidOperationException($"Line {i + 1}: inline transition reasons are not supported. Use a block outcome statement, for example reject \"<message>\".");

            throw new InvalidOperationException($"Line {i + 1}: unrecognized statement '{line}'.");
        }

        if (name == null)
            throw new InvalidOperationException("Missing 'machine <Name>' declaration.");

        if (states.Count == 0)
            throw new InvalidOperationException("At least one state must be declared.");

        if (events.Count == 0)
            throw new InvalidOperationException("At least one event must be declared.");

        ValidateReferences(states, events, transitions, terminalRules);

        return new DslMachine(name, states, events, transitions, terminalRules);
    }

    private static void ParseFromOnBlock(
        string[] lines,
        ref int index,
        Match fromOnMatch,
        IReadOnlyList<string> declaredStates,
        ICollection<DslTransition> transitions,
        ICollection<DslTerminalRule> terminalRules)
    {
        var headerRaw = lines[index];
        var headerIndent = GetIndentation(headerRaw);
        var fromToken = fromOnMatch.Groups["from"].Value.Trim();
        var eventName = fromOnMatch.Groups["event"].Value.Trim();

        var sourceStates = fromToken.Equals("any", StringComparison.Ordinal)
            ? declaredStates.ToList()
            : ParseIdentifierList(fromToken, index + 1, "state");

        if (sourceStates.Count == 0)
            throw new InvalidOperationException($"Line {index + 1}: 'from any' requires states to be declared first.");

        var blockBranches = new List<DslTransition>();
        DslTerminalKind? terminalKind = null;
        string? terminalReason = null;
        bool hasUnconditionalTerminalTransition = false;
        bool reachedTerminalStatement = false;
        bool hasIfChain = false;
        bool hasElseBranch = false;

        string? pendingSetKey = null;
        string? pendingSetExpr = null;

        index++;
        while (index < lines.Length)
        {
            var raw = lines[index];
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var indent = GetIndentation(raw);
            if (indent <= headerIndent)
                break;

            if (reachedTerminalStatement)
                throw new InvalidOperationException($"Line {index + 1}: no statements are allowed after an outcome statement in a from/on block.");

            var ifMatch = IfRegex.Match(line);
            if (ifMatch.Success)
            {
                if (ifMatch.Groups["reason"].Success)
                    throw new InvalidOperationException($"Line {index + 1}: 'reason' is not allowed on 'if' branches. Provide a reason only on 'reject' statements.");

                if (hasIfChain)
                    throw new InvalidOperationException($"Line {index + 1}: use 'else if' to continue an existing if-chain.");

                hasIfChain = true;
                ParseGuardedTransitionBranch(
                    lines,
                    ref index,
                    indent,
                    sourceStates,
                    eventName,
                    blockBranches,
                    ifMatch.Groups["guard"].Value.Trim());
                continue;
            }

            var elseIfMatch = ElseIfRegex.Match(line);
            if (elseIfMatch.Success)
            {
                if (elseIfMatch.Groups["reason"].Success)
                    throw new InvalidOperationException($"Line {index + 1}: 'reason' is not allowed on 'else if' branches. Provide a reason only on 'reject' statements.");

                if (!hasIfChain)
                    throw new InvalidOperationException($"Line {index + 1}: 'else if' requires a preceding 'if'.");

                if (hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: 'else if' is not allowed after 'else'.");

                ParseGuardedTransitionBranch(
                    lines,
                    ref index,
                    indent,
                    sourceStates,
                    eventName,
                    blockBranches,
                    elseIfMatch.Groups["guard"].Value.Trim());
                continue;
            }

            if (ElseRegex.IsMatch(line))
            {
                if (!hasIfChain)
                    throw new InvalidOperationException($"Line {index + 1}: 'else' requires a preceding 'if'.");

                if (hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: duplicate 'else' is not allowed.");

                hasElseBranch = true;
                ParseOutcomeBranch(
                    lines,
                    ref index,
                    indent,
                    sourceStates,
                    eventName,
                    blockBranches,
                    ref hasUnconditionalTerminalTransition,
                    ref terminalKind,
                    ref terminalReason);

                reachedTerminalStatement = true;
                continue;
            }

            var transformAtBlock = TransformRegex.Match(line);
            if (transformAtBlock.Success)
            {
                pendingSetKey = transformAtBlock.Groups["setKey"].Value.Trim();
                pendingSetExpr = transformAtBlock.Groups["setExpr"].Value.Trim();
                index++;
                continue;
            }

            var simpleTransitionAtBlock = SimpleTransitionRegex.Match(line);
            if (simpleTransitionAtBlock.Success)
            {
                var targetState = simpleTransitionAtBlock.Groups["to"].Value.Trim();
                foreach (var sourceState in sourceStates)
                {
                    blockBranches.Add(new DslTransition(
                        sourceState,
                        targetState,
                        eventName,
                        null,
                        pendingSetKey,
                        pendingSetExpr));
                }

                pendingSetKey = null;
                pendingSetExpr = null;
                hasUnconditionalTerminalTransition = true;
                reachedTerminalStatement = true;
                index++;
                continue;
            }

            if (line.Equals("no transition", StringComparison.Ordinal))
            {
                terminalKind = DslTerminalKind.NoTransition;
                terminalReason = null;
                reachedTerminalStatement = true;
                index++;
                continue;
            }

            var rejectMatch = RejectRegex.Match(line);
            if (rejectMatch.Success)
            {
                terminalKind = DslTerminalKind.Reject;
                terminalReason = Unquote(rejectMatch.Groups["reason"].Value.Trim());
                reachedTerminalStatement = true;
                index++;
                continue;
            }

            throw new InvalidOperationException($"Line {index + 1}: unrecognized statement '{line}' inside from/on block.");
        }

        if (pendingSetKey is not null)
            throw new InvalidOperationException($"Line {index}: transform requires a following transition.");

        if (!hasUnconditionalTerminalTransition && terminalKind is null)
            throw new InvalidOperationException($"Line {index}: from/on block must end with an outcome statement: transition <State>, reject <reason>, or no transition.");

        if (terminalKind == DslTerminalKind.Reject && string.IsNullOrWhiteSpace(terminalReason))
            throw new InvalidOperationException($"Line {index}: reject requires a reason.");

        foreach (var branch in blockBranches)
            transitions.Add(branch);

        if (terminalKind is not null)
        {
            foreach (var sourceState in sourceStates)
                terminalRules.Add(new DslTerminalRule(sourceState, eventName, terminalKind.Value, terminalReason));
        }
    }

    private static void ParseGuardedTransitionBranch(
        string[] lines,
        ref int index,
        int branchHeaderIndent,
        IReadOnlyList<string> sourceStates,
        string eventName,
        ICollection<DslTransition> blockBranches,
        string guardExpression)
    {
        string? branchSetKey = null;
        string? branchSetExpr = null;
        string? branchTargetState = null;

        index++;
        while (index < lines.Length)
        {
            var nestedRaw = lines[index];
            var nestedLine = nestedRaw.Trim();
            if (string.IsNullOrWhiteSpace(nestedLine) || nestedLine.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var nestedIndent = GetIndentation(nestedRaw);
            if (nestedIndent <= branchHeaderIndent)
                break;

            var transformMatch = TransformRegex.Match(nestedLine);
            if (transformMatch.Success)
            {
                branchSetKey = transformMatch.Groups["setKey"].Value.Trim();
                branchSetExpr = transformMatch.Groups["setExpr"].Value.Trim();
                index++;
                continue;
            }

            var transitionMatch = SimpleTransitionRegex.Match(nestedLine);
            if (transitionMatch.Success)
            {
                if (branchTargetState is not null)
                    throw new InvalidOperationException($"Line {index + 1}: only one outcome statement is allowed in an if-branch.");

                branchTargetState = transitionMatch.Groups["to"].Value.Trim();
                index++;
                continue;
            }

            if (RejectRegex.IsMatch(nestedLine) || nestedLine.Equals("no transition", StringComparison.Ordinal))
                throw new InvalidOperationException($"Line {index + 1}: if/else if branches must resolve with 'transition <State>'; use else or a block outcome statement for reject/no transition.");

            throw new InvalidOperationException($"Line {index + 1}: expected 'transform <Key> = <Expr>' or 'transition <State>' inside if-branch.");
        }

        if (string.IsNullOrWhiteSpace(branchTargetState))
            throw new InvalidOperationException($"Line {index}: if/else if branch requires 'transition <State>'.");

        foreach (var sourceState in sourceStates)
        {
            blockBranches.Add(new DslTransition(
                sourceState,
                branchTargetState,
                eventName,
                guardExpression,
                branchSetKey,
                branchSetExpr));
        }
    }

    private static void ParseOutcomeBranch(
        string[] lines,
        ref int index,
        int branchHeaderIndent,
        IReadOnlyList<string> sourceStates,
        string eventName,
        ICollection<DslTransition> blockBranches,
        ref bool hasUnconditionalTerminalTransition,
        ref DslTerminalKind? terminalKind,
        ref string? terminalReason)
    {
        string? branchSetKey = null;
        string? branchSetExpr = null;
        string? branchTargetState = null;
        DslTerminalKind? branchTerminalKind = null;
        string? branchTerminalReason = null;
        bool branchReachedOutcome = false;

        index++;
        while (index < lines.Length)
        {
            var nestedRaw = lines[index];
            var nestedLine = nestedRaw.Trim();
            if (string.IsNullOrWhiteSpace(nestedLine) || nestedLine.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var nestedIndent = GetIndentation(nestedRaw);
            if (nestedIndent <= branchHeaderIndent)
                break;

            if (branchReachedOutcome)
                throw new InvalidOperationException($"Line {index + 1}: no statements are allowed after an outcome statement in an else-branch.");

            var transformMatch = TransformRegex.Match(nestedLine);
            if (transformMatch.Success)
            {
                branchSetKey = transformMatch.Groups["setKey"].Value.Trim();
                branchSetExpr = transformMatch.Groups["setExpr"].Value.Trim();
                index++;
                continue;
            }

            var transitionMatch = SimpleTransitionRegex.Match(nestedLine);
            if (transitionMatch.Success)
            {
                branchTargetState = transitionMatch.Groups["to"].Value.Trim();
                hasUnconditionalTerminalTransition = true;
                branchReachedOutcome = true;
                index++;
                continue;
            }

            if (nestedLine.Equals("no transition", StringComparison.Ordinal))
            {
                branchTerminalKind = DslTerminalKind.NoTransition;
                branchTerminalReason = null;
                branchReachedOutcome = true;
                index++;
                continue;
            }

            var rejectMatch = RejectRegex.Match(nestedLine);
            if (rejectMatch.Success)
            {
                branchTerminalKind = DslTerminalKind.Reject;
                branchTerminalReason = Unquote(rejectMatch.Groups["reason"].Value.Trim());
                branchReachedOutcome = true;
                index++;
                continue;
            }

            throw new InvalidOperationException($"Line {index + 1}: expected an outcome statement inside else-branch.");
        }

        if (!branchReachedOutcome)
            throw new InvalidOperationException($"Line {index}: else branch requires an outcome statement.");

        if (branchTargetState is not null)
        {
            foreach (var sourceState in sourceStates)
            {
                blockBranches.Add(new DslTransition(
                    sourceState,
                    branchTargetState,
                    eventName,
                    null,
                    branchSetKey,
                    branchSetExpr));
            }

            return;
        }

        terminalKind = branchTerminalKind;
        terminalReason = branchTerminalReason;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value.Substring(1, value.Length - 2);

        return value;
    }

    private static int GetIndentation(string rawLine)
    {
        int count = 0;
        foreach (char c in rawLine)
        {
            if (c == ' ')
            {
                count++;
                continue;
            }

            if (c == '\t')
            {
                count += 4;
                continue;
            }

            break;
        }

        return count;
    }

    private static List<string> ParseIdentifierList(string listText, int lineNumber, string kind)
    {
        var results = new List<string>();
        foreach (var rawToken in listText.Split(','))
        {
            var token = rawToken.Trim();
            if (string.IsNullOrWhiteSpace(token))
                continue;

            if (!Regex.IsMatch(token, "^[A-Za-z_][A-Za-z0-9_]*$"))
                throw new InvalidOperationException($"Line {lineNumber}: invalid {kind} identifier '{token}'.");

            results.Add(token);
        }

        return results;
    }

    private static void ValidateReferences(
        IReadOnlyCollection<string> states,
        IReadOnlyCollection<DslEvent> events,
        IReadOnlyCollection<DslTransition> transitions,
        IReadOnlyCollection<DslTerminalRule> terminalRules)
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

        var terminalKeySet = new HashSet<(string State, string Event)>(EqualityComparer<(string State, string Event)>.Default);
        foreach (var terminalRule in terminalRules)
        {
            if (!stateSet.Contains(terminalRule.FromState))
                throw new InvalidOperationException($"Terminal rule references unknown source state '{terminalRule.FromState}'.");

            if (!eventSet.Contains(terminalRule.EventName))
                throw new InvalidOperationException($"Terminal rule references unknown event '{terminalRule.EventName}'.");

            if (!terminalKeySet.Add((terminalRule.FromState, terminalRule.EventName)))
                throw new InvalidOperationException($"Duplicate outcome rule for state '{terminalRule.FromState}' and event '{terminalRule.EventName}'.");

            if (terminalRule.Kind == DslTerminalKind.Reject && string.IsNullOrWhiteSpace(terminalRule.Reason))
                throw new InvalidOperationException($"Terminal reject rule for state '{terminalRule.FromState}' and event '{terminalRule.EventName}' requires a reason.");
        }
    }
}
