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
    private static readonly Regex ContractFieldRegex = new(
        "^(?<type>string|number|boolean|null)(?<nullable>\\?)?\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
    private static readonly Regex SetRegex = new(
        "^set\\s+(?<setKey>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<setExpr>.+)$",
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
        var dataFields = new List<DslFieldContract>();

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

            var eventMatch = EventRegex.Match(line);
            if (eventMatch.Success)
            {
                string eventName = eventMatch.Groups["name"].Value;

                ParseEventDeclaration(lines, ref i, eventName, events);
                continue;
            }

            if (line.StartsWith("states ", StringComparison.Ordinal))
                throw new InvalidOperationException($"Line {i + 1}: 'states' declaration is not supported. Declare each state with 'state <Name>'.");

            if (line.StartsWith("events ", StringComparison.Ordinal))
                throw new InvalidOperationException($"Line {i + 1}: 'events' declaration is not supported. Declare each event with 'event <Name>'.");

            var typedEventMatch = TypedEventRegex.Match(line);
            if (typedEventMatch.Success)
            {
                var eventName = typedEventMatch.Groups["name"].Value;
                throw new InvalidOperationException($"Line {i + 1}: inline typed event arguments are not supported. Use 'event {eventName}' with optional indented argument declarations.");
            }

            var dataFieldMatch = ContractFieldRegex.Match(line);
            if (dataFieldMatch.Success)
            {
                var fieldName = dataFieldMatch.Groups["name"].Value;
                if (dataFields.Any(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate data field '{fieldName}'.");

                dataFields.Add(new DslFieldContract(
                    fieldName,
                    ParseScalarType(dataFieldMatch.Groups["type"].Value),
                    dataFieldMatch.Groups["nullable"].Success));

                i++;
                continue;
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
                throw new InvalidOperationException($"Line {i + 1}: inline 'transition <From> -> <To> on <Event>' declarations are not supported. Use a 'from <State> on <Event>' block instead.");

            if (line.StartsWith("transition", StringComparison.Ordinal) && line.Contains(" reason ", StringComparison.Ordinal))
                throw new InvalidOperationException($"Line {i + 1}: inline transition declarations are not supported. Use a 'from <State> on <Event>' block and place reasons on 'reject' statements.");

            throw new InvalidOperationException($"Line {i + 1}: unrecognized statement '{line}'.");
        }

        if (name == null)
            throw new InvalidOperationException("Missing 'machine <Name>' declaration.");

        if (states.Count == 0)
            throw new InvalidOperationException("At least one state must be declared.");

        if (events.Count == 0)
            throw new InvalidOperationException("At least one event must be declared.");

        ValidateReferences(states, events, transitions, terminalRules, dataFields);

        return new DslMachine(name, states, events, transitions, terminalRules, dataFields);
    }

    private static void ParseEventDeclaration(
        string[] lines,
        ref int index,
        string eventName,
        ICollection<DslEvent> events)
    {
        if (events.Any(e => string.Equals(e.Name, eventName, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Line {index + 1}: duplicate event '{eventName}'.");

        var headerIndent = GetIndentation(lines[index]);
        var args = new List<DslFieldContract>();

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

            var fieldMatch = ContractFieldRegex.Match(line);
            if (!fieldMatch.Success)
                throw new InvalidOperationException($"Line {index + 1}: invalid argument declaration '{line}'. Expected '<string|number|boolean|null>[?] <Name>'.");

            var fieldName = fieldMatch.Groups["name"].Value;
            if (args.Any(a => string.Equals(a.Name, fieldName, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Line {index + 1}: duplicate argument '{fieldName}' for event '{eventName}'.");

            args.Add(new DslFieldContract(
                fieldName,
                ParseScalarType(fieldMatch.Groups["type"].Value),
                fieldMatch.Groups["nullable"].Success));
            index++;
        }

        events.Add(new DslEvent(eventName, args));
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
        var blockTerminalRules = new List<DslTerminalRule>();
        var branchOrder = 0;
        bool reachedTerminalStatement = false;
        bool hasIfChain = false;
        bool hasElseBranch = false;

        var pendingSets = new List<DslSetAssignment>();

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
                    blockTerminalRules,
                    ref branchOrder,
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
                    blockTerminalRules,
                    ref branchOrder,
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
                    blockTerminalRules,
                    ref branchOrder);

                reachedTerminalStatement = true;
                continue;
            }

            var setAtBlock = SetRegex.Match(line);
            if (setAtBlock.Success)
            {
                pendingSets.Add(ParseSetAssignment(setAtBlock, index + 1));
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
                        pendingSets.ToArray(),
                        branchOrder));
                }
                branchOrder++;

                pendingSets.Clear();
                reachedTerminalStatement = true;
                index++;
                continue;
            }

            if (line.Equals("no transition", StringComparison.Ordinal))
            {
                foreach (var sourceState in sourceStates)
                    blockTerminalRules.Add(new DslTerminalRule(sourceState, eventName, DslTerminalKind.NoTransition, null, null, branchOrder));

                branchOrder++;
                reachedTerminalStatement = true;
                index++;
                continue;
            }

            var rejectMatch = RejectRegex.Match(line);
            if (rejectMatch.Success)
            {
                var reason = Unquote(rejectMatch.Groups["reason"].Value.Trim());
                foreach (var sourceState in sourceStates)
                    blockTerminalRules.Add(new DslTerminalRule(sourceState, eventName, DslTerminalKind.Reject, reason, null, branchOrder));

                branchOrder++;
                reachedTerminalStatement = true;
                index++;
                continue;
            }

            throw new InvalidOperationException($"Line {index + 1}: unrecognized statement '{line}' inside from/on block.");
        }

        if (pendingSets.Count > 0)
            throw new InvalidOperationException($"Line {index}: set requires a following transition.");

        if (!reachedTerminalStatement)
            throw new InvalidOperationException($"Line {index}: from/on block must end with an outcome statement: transition <State>, reject <reason>, or no transition.");

        foreach (var branch in blockBranches)
            transitions.Add(branch);

        foreach (var terminalRule in blockTerminalRules)
            terminalRules.Add(terminalRule);
    }

    private static void ParseGuardedTransitionBranch(
        string[] lines,
        ref int index,
        int branchHeaderIndent,
        IReadOnlyList<string> sourceStates,
        string eventName,
        ICollection<DslTransition> blockBranches,
        ICollection<DslTerminalRule> blockTerminalRules,
        ref int branchOrder,
        string guardExpression)
    {
        var branchSets = new List<DslSetAssignment>();
        string? branchTargetState = null;
        bool hasNoTransitionOutcome = false;

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

            var setMatch = SetRegex.Match(nestedLine);
            if (setMatch.Success)
            {
                branchSets.Add(ParseSetAssignment(setMatch, index + 1));
                index++;
                continue;
            }

            var transitionMatch = SimpleTransitionRegex.Match(nestedLine);
            if (transitionMatch.Success)
            {
                if (branchTargetState is not null || hasNoTransitionOutcome)
                    throw new InvalidOperationException($"Line {index + 1}: only one outcome statement is allowed in an if-branch.");

                branchTargetState = transitionMatch.Groups["to"].Value.Trim();
                index++;
                continue;
            }

            if (nestedLine.Equals("no transition", StringComparison.Ordinal))
            {
                if (branchTargetState is not null || hasNoTransitionOutcome)
                    throw new InvalidOperationException($"Line {index + 1}: only one outcome statement is allowed in an if-branch.");

                hasNoTransitionOutcome = true;
                index++;
                continue;
            }

            if (RejectRegex.IsMatch(nestedLine))
                throw new InvalidOperationException($"Line {index + 1}: if/else if branches support 'transition <State>' or 'no transition'; use else or a block outcome statement for reject.");

            throw new InvalidOperationException($"Line {index + 1}: expected 'set <Key> = <Expr>', 'transition <State>', or 'no transition' inside if-branch.");
        }

        if (string.IsNullOrWhiteSpace(branchTargetState) && !hasNoTransitionOutcome)
            throw new InvalidOperationException($"Line {index}: if/else if branch requires 'transition <State>' or 'no transition'.");

        if (hasNoTransitionOutcome && branchSets.Count > 0)
            throw new InvalidOperationException($"Line {index}: set requires a transition outcome and cannot be used with 'no transition'.");

        if (hasNoTransitionOutcome)
        {
            foreach (var sourceState in sourceStates)
                blockTerminalRules.Add(new DslTerminalRule(sourceState, eventName, DslTerminalKind.NoTransition, null, guardExpression, branchOrder));

            branchOrder++;
            return;
        }

        foreach (var sourceState in sourceStates)
        {
            blockBranches.Add(new DslTransition(
                sourceState,
                branchTargetState!,
                eventName,
                guardExpression,
                branchSets.ToArray(),
                branchOrder));
        }

        branchOrder++;
    }

    private static void ParseOutcomeBranch(
        string[] lines,
        ref int index,
        int branchHeaderIndent,
        IReadOnlyList<string> sourceStates,
        string eventName,
        ICollection<DslTransition> blockBranches,
        ICollection<DslTerminalRule> blockTerminalRules,
        ref int branchOrder)
    {
        var branchSets = new List<DslSetAssignment>();
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

            var setMatch = SetRegex.Match(nestedLine);
            if (setMatch.Success)
            {
                branchSets.Add(ParseSetAssignment(setMatch, index + 1));
                index++;
                continue;
            }

            var transitionMatch = SimpleTransitionRegex.Match(nestedLine);
            if (transitionMatch.Success)
            {
                branchTargetState = transitionMatch.Groups["to"].Value.Trim();
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
                    branchSets.ToArray(),
                    branchOrder));
            }

            branchOrder++;

            return;
        }

        foreach (var sourceState in sourceStates)
            blockTerminalRules.Add(new DslTerminalRule(sourceState, eventName, branchTerminalKind!.Value, branchTerminalReason, null, branchOrder));

        branchOrder++;
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

    private static DslScalarType ParseScalarType(string token)
        => token.Trim().ToLowerInvariant() switch
        {
            "string" => DslScalarType.String,
            "number" => DslScalarType.Number,
            "boolean" => DslScalarType.Boolean,
            "null" => DslScalarType.Null,
            _ => throw new InvalidOperationException($"Unsupported scalar type '{token}'.")
        };

    private static string NormalizeDataAssignmentKey(string setKey)
        => setKey;

    private static DslSetAssignment ParseSetAssignment(Match setMatch, int lineNumber)
    {
        var key = NormalizeDataAssignmentKey(setMatch.Groups["setKey"].Value.Trim());
        var expressionText = setMatch.Groups["setExpr"].Value.Trim();

        try
        {
            var expression = DslExpressionParser.Parse(expressionText);
            return new DslSetAssignment(key, expressionText, expression);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Line {lineNumber}: invalid set expression '{expressionText}'. {ex.Message}");
        }
    }

    private static void ValidateReferences(
        IReadOnlyCollection<string> states,
        IReadOnlyCollection<DslEvent> events,
        IReadOnlyCollection<DslTransition> transitions,
        IReadOnlyCollection<DslTerminalRule> terminalRules,
        IReadOnlyCollection<DslFieldContract> dataFields)
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

            foreach (var assignment in transition.SetAssignments)
            {
                if (dataFields.Count == 0)
                    continue;

                var knownDataField = dataFields.Any(f => string.Equals(f.Name, assignment.Key, StringComparison.Ordinal));
                if (!knownDataField)
                    throw new InvalidOperationException($"Transition '{transition.FromState} -> {transition.ToState}' on '{transition.EventName}' assigns unknown data field '{assignment.Key}'.");
            }
        }

        var terminalGroups = terminalRules.GroupBy(rule => (rule.FromState, rule.EventName));
        foreach (var terminalGroup in terminalGroups)
        {
            var unguardedCount = 0;
            foreach (var terminalRule in terminalGroup)
            {
            if (!stateSet.Contains(terminalRule.FromState))
                throw new InvalidOperationException($"Terminal rule references unknown source state '{terminalRule.FromState}'.");

            if (!eventSet.Contains(terminalRule.EventName))
                throw new InvalidOperationException($"Terminal rule references unknown event '{terminalRule.EventName}'.");

            if (string.IsNullOrWhiteSpace(terminalRule.GuardExpression))
                unguardedCount++;

            if (terminalRule.Kind == DslTerminalKind.Reject && string.IsNullOrWhiteSpace(terminalRule.Reason))
                throw new InvalidOperationException($"Terminal reject rule for state '{terminalRule.FromState}' and event '{terminalRule.EventName}' requires a reason.");

                if (terminalRule.Order < 0)
                    throw new InvalidOperationException($"Terminal rule for state '{terminalRule.FromState}' and event '{terminalRule.EventName}' has invalid order '{terminalRule.Order}'.");
            }

            if (unguardedCount > 1)
                throw new InvalidOperationException($"Duplicate unguarded outcome rule for state '{terminalGroup.Key.FromState}' and event '{terminalGroup.Key.EventName}'.");
        }
    }
}
