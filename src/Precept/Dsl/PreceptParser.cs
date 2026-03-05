using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Precept;

public static class PreceptParser
{
    private static readonly Regex MachineRegex = new("^machine\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new("^state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s+(?<initial>initial))?$", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("^event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex TypedEventRegex = new("^event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*\\((?<arg>[A-Za-z_][A-Za-z0-9_<>., ]*)\\)$", RegexOptions.Compiled);
    private static readonly Regex EventArgFieldRegex = new(
        "^(?<type>string|number|boolean|null)(?<nullable>\\?)?\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*(?<default>.+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DataFieldRegex = new(
        "^(?<type>string|number|boolean|null)(?<nullable>\\?)?\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*(?<default>.+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CollectionFieldRegex = new(
        "^(?<kind>set|queue|stack)<(?<inner>number|string|boolean)>\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TransitionRegex = new(
        "^transition\\s+(?<from>[A-Za-z_][A-Za-z0-9_]*)\\s*->\\s*(?<to>[A-Za-z_][A-Za-z0-9_]*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)(?:\\s+set\\s+(?<setKey>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<setExpr>.+))?$",
        RegexOptions.Compiled);
    private static readonly Regex FromOnRegex = new(
        "^from\\s+(?<from>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)(?:\\s+when\\s+(?<when>.+))?$",
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
    private static readonly Regex AddRegex = new(
        "^add\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s+(?<expr>.+)$",
        RegexOptions.Compiled);
    private static readonly Regex RemoveRegex = new(
        "^remove\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s+(?<expr>.+)$",
        RegexOptions.Compiled);
    private static readonly Regex EnqueueRegex = new(
        "^enqueue\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s+(?<expr>.+)$",
        RegexOptions.Compiled);
    private static readonly Regex DequeueRegex = new(
        "^dequeue\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)(?:\\s+into\\s+(?<into>[A-Za-z_][A-Za-z0-9_]*))?$",
        RegexOptions.Compiled);
    private static readonly Regex PushRegex = new(
        "^push\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s+(?<expr>.+)$",
        RegexOptions.Compiled);
    private static readonly Regex PopRegex = new(
        "^pop\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)(?:\\s+into\\s+(?<into>[A-Za-z_][A-Za-z0-9_]*))?$",
        RegexOptions.Compiled);
    private static readonly Regex ClearRegex = new(
        "^clear\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)$",
        RegexOptions.Compiled);
    private static readonly Regex SimpleTransitionRegex = new(
        "^transition\\s+(?<to>[A-Za-z_][A-Za-z0-9_]*)$",
        RegexOptions.Compiled);
    private static readonly Regex RejectRegex = new("^reject\\s+(?<reason>.+)$", RegexOptions.Compiled);
    private static readonly Regex RuleRegex = new(
        "^rule\\s+(?<expr>.+?)\\s+\"(?<reason>[^\"]+)\"\\s*$",
        RegexOptions.Compiled);

    public static DslWorkflowModel Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("DSL input is empty.");

        string? name = null;
        DslState? initialState = null;
        var states = new List<DslState>();
        var events = new List<DslEvent>();
        var transitions = new List<DslTransition>();
        var dataFields = new List<DslField>();
        var collectionFields = new List<DslCollectionField>();
        var topLevelRules = new List<DslRule>();
        var seenFromOnPairs = new HashSet<(string State, string Event)>();

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int? firstContentLineNumber = null;
        var lastStateLineNumber = 1;
        int i = 0;
        while (i < lines.Length)
        {
            var raw = lines[i];
            string line = StripInlineComment(raw.Trim());
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                i++;
                continue;
            }

            firstContentLineNumber ??= i + 1;

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
                if (states.Any(s => string.Equals(s.Name, stateName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate state '{stateName}'.");

                lastStateLineNumber = i + 1;
                var dslState = ParseStateDeclaration(lines, ref i, stateName);
                states.Add(dslState);

                if (stateMatch.Groups["initial"].Success)
                {
                    if (initialState is not null)
                        throw new InvalidOperationException($"Line {i}: duplicate initial state marker. '{initialState.Name}' is already marked initial.");

                    initialState = dslState;
                }

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

            var collectionFieldMatch = CollectionFieldRegex.Match(line);
            if (collectionFieldMatch.Success)
            {
                var fieldName = collectionFieldMatch.Groups["name"].Value;
                if (dataFields.Any(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate data field '{fieldName}'.");
                if (collectionFields.Any(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate collection field '{fieldName}'.");

                var collectionKind = collectionFieldMatch.Groups["kind"].Value.ToLowerInvariant() switch
                {
                    "set" => DslCollectionKind.Set,
                    "queue" => DslCollectionKind.Queue,
                    "stack" => DslCollectionKind.Stack,
                    _ => throw new InvalidOperationException($"Line {i + 1}: unknown collection kind '{collectionFieldMatch.Groups["kind"].Value}'.")
                };
                var innerType = ParseScalarType(collectionFieldMatch.Groups["inner"].Value);

                var collectionFieldRules = ParseFieldRules(lines, ref i, fieldName, isCollection: true);
                collectionFields.Add(new DslCollectionField(fieldName, collectionKind, innerType, collectionFieldRules.Count > 0 ? collectionFieldRules : null));
                continue;
            }

            var dataFieldMatch = DataFieldRegex.Match(line);
            if (dataFieldMatch.Success)
            {
                var fieldName = dataFieldMatch.Groups["name"].Value;
                if (dataFields.Any(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate data field '{fieldName}'.");
                if (collectionFields.Any(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Line {i + 1}: duplicate data field '{fieldName}'.");

                var fieldType = ParseScalarType(dataFieldMatch.Groups["type"].Value);
                var isNullable = dataFieldMatch.Groups["nullable"].Success;
                var hasDefaultValue = dataFieldMatch.Groups["default"].Success;

                if (!isNullable && !hasDefaultValue)
                    throw new InvalidOperationException($"Line {i + 1}: non-nullable field '{fieldName}' requires a default value.");

                var defaultValue = hasDefaultValue
                    ? ParseFieldDefaultLiteral(dataFieldMatch.Groups["default"].Value.Trim(), fieldType, isNullable, fieldName, i + 1)
                    : null;

                var fieldRules = ParseFieldRules(lines, ref i, fieldName, isCollection: false);

                dataFields.Add(new DslField(
                    fieldName,
                    fieldType,
                    isNullable,
                    hasDefaultValue,
                    defaultValue,
                    fieldRules.Count > 0 ? fieldRules : null));

                continue;
            }

            // Top-level rule (appears after referenced fields, before from/on blocks)
            var topLevelRuleMatch = RuleRegex.Match(line);
            if (topLevelRuleMatch.Success)
            {
                var ruleExprText = topLevelRuleMatch.Groups["expr"].Value.Trim();
                var ruleReason = topLevelRuleMatch.Groups["reason"].Value;
                var exprStartCol = raw.IndexOf(ruleExprText, StringComparison.Ordinal);
                if (exprStartCol < 0) exprStartCol = line.IndexOf(ruleExprText, StringComparison.Ordinal);
                var exprEndCol = exprStartCol + ruleExprText.Length;
                var reasonStartCol = raw.LastIndexOf('"') - ruleReason.Length;
                if (reasonStartCol < 0) reasonStartCol = exprEndCol;
                var reasonEndCol = reasonStartCol + ruleReason.Length;

                // Validate no forward references — only fields already declared are allowed
                var declaredNames = new HashSet<string>(dataFields.Select(f => f.Name).Concat(collectionFields.Select(f => f.Name)), StringComparer.Ordinal);
                ValidateRuleScope(ruleExprText, i + 1, declaredNames, allowedIdentifiers: null, scopeDescription: "top-level rule");

                DslExpression ruleExpr;
                try { ruleExpr = DslExpressionParser.Parse(ruleExprText); }
                catch (InvalidOperationException ex)
                { throw new InvalidOperationException($"Line {i + 1}: invalid rule expression '{ruleExprText}'. {ex.Message}"); }

                topLevelRules.Add(new DslRule(ruleExprText, ruleExpr, ruleReason, i + 1, exprStartCol, exprEndCol, reasonStartCol, reasonEndCol));
                i++;
                continue;
            }

            var fromOnMatch = FromOnRegex.Match(line);
            if (fromOnMatch.Success)
            {
                // Enforce uniqueness of (state, event) pairs before delegating to ParseFromOnBlock.
                var fromToken = fromOnMatch.Groups["from"].Value.Trim();
                var onEvent = fromOnMatch.Groups["event"].Value.Trim();
                var sourceStatesForCheck = fromToken.Equals("any", StringComparison.Ordinal)
                    ? states.Select(s => s.Name).ToList()
                    : fromToken.Split(',').Select(s => s.Trim()).ToList();
                foreach (var st in sourceStatesForCheck)
                {
                    if (!seenFromOnPairs.Add((st, onEvent)))
                        throw new InvalidOperationException(
                            $"Line {i + 1}: duplicate 'from {st} on {onEvent}' block. Each state+event combination must be handled in exactly one block.");
                }

                ParseFromOnBlock(
                    lines,
                    ref i,
                    fromOnMatch,
                    states,
                    transitions,
                    collectionFields,
                    dataFields);
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
            throw new InvalidOperationException($"Line {firstContentLineNumber ?? 1}: Missing 'machine <Name>' declaration.");

        if (states.Count == 0)
            throw new InvalidOperationException($"Line {firstContentLineNumber ?? 1}: At least one state must be declared.");

        if (initialState is null)
            throw new InvalidOperationException($"Line {lastStateLineNumber}: Exactly one state must be marked initial. Use 'state <Name> initial'.");

        ValidateReferences(states, events, transitions, dataFields, collectionFields);

        return new DslWorkflowModel(name, states, initialState, events, transitions, dataFields, collectionFields,
            topLevelRules.Count > 0 ? topLevelRules : null);
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
        var args = new List<DslEventArg>();
        var eventRules = new List<DslRule>();

        index++;
        while (index < lines.Length)
        {
            var raw = lines[index];
            var line = StripInlineComment(raw.Trim());
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var indent = GetIndentation(raw);
            if (indent <= headerIndent)
                break;

            // Event-level rules: indented under event, scope is event args only
            var eventRuleMatch = RuleRegex.Match(line);
            if (eventRuleMatch.Success)
            {
                var ruleExprText = eventRuleMatch.Groups["expr"].Value.Trim();
                var ruleReason = eventRuleMatch.Groups["reason"].Value;
                var argNames = new HashSet<string>(args.Select(a => a.Name), StringComparer.Ordinal);
                // Event rules may only reference event arg identifiers (prefixed or bare)
                // Event rules may only reference event arg identifiers (prefixed or bare)
                ValidateEventRuleScope(ruleExprText, index + 1, argNames, eventName);

                DslExpression ruleExpr;
                try { ruleExpr = DslExpressionParser.Parse(ruleExprText); }
                catch (InvalidOperationException ex)
                { throw new InvalidOperationException($"Line {index + 1}: invalid rule expression '{ruleExprText}'. {ex.Message}"); }

                var exprStartCol = raw.IndexOf(ruleExprText, StringComparison.Ordinal);
                if (exprStartCol < 0) exprStartCol = line.IndexOf(ruleExprText, StringComparison.Ordinal);
                var exprEndCol = exprStartCol + ruleExprText.Length;
                var reasonStartCol = exprEndCol;
                var reasonEndCol = reasonStartCol + ruleReason.Length;
                eventRules.Add(new DslRule(ruleExprText, ruleExpr, ruleReason, index + 1, exprStartCol, exprEndCol, reasonStartCol, reasonEndCol));
                index++;
                continue;
            }

            var fieldMatch = EventArgFieldRegex.Match(line);
            if (!fieldMatch.Success)
                throw new InvalidOperationException($"Line {index + 1}: invalid argument declaration '{line}'. Expected '<string|number|boolean|null>[?] <Name> [= <Default>]'.");

            var fieldName = fieldMatch.Groups["name"].Value;
            if (args.Any(a => string.Equals(a.Name, fieldName, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Line {index + 1}: duplicate argument '{fieldName}' for event '{eventName}'.");

            var argType = ParseScalarType(fieldMatch.Groups["type"].Value);
            var argIsNullable = fieldMatch.Groups["nullable"].Success;
            var argHasDefault = fieldMatch.Groups["default"].Success;
            var argDefaultValue = argHasDefault
                ? ParseFieldDefaultLiteral(fieldMatch.Groups["default"].Value.Trim(), argType, argIsNullable, fieldName, index + 1)
                : null;

            args.Add(new DslEventArg(
                fieldName,
                argType,
                argIsNullable,
                argHasDefault,
                argDefaultValue));
            index++;
        }

        events.Add(new DslEvent(eventName, args, eventRules.Count > 0 ? eventRules : null));
    }

    /// <summary>
    /// Advances <paramref name="index"/> past the state header line and collects any indented
    /// <c>rule</c> lines that follow. Non-rule indented content causes a parse error.
    /// </summary>
    private static DslState ParseStateDeclaration(
        string[] lines,
        ref int index,
        string stateName)
    {
        var headerIndent = GetIndentation(lines[index]);
        index++;

        var rules = new List<DslRule>();
        while (index < lines.Length)
        {
            var raw = lines[index];
            var line = StripInlineComment(raw.Trim());
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var indent = GetIndentation(raw);
            if (indent <= headerIndent)
                break;

            var ruleMatch = RuleRegex.Match(line);
            if (!ruleMatch.Success)
            {
                // Not a rule line — stop consuming state body; outer parser will handle this line.
                break;
            }

            var ruleExprText = ruleMatch.Groups["expr"].Value.Trim();
            var ruleReason = ruleMatch.Groups["reason"].Value;
            var exprStartCol = raw.IndexOf(ruleExprText, StringComparison.Ordinal);
            if (exprStartCol < 0) exprStartCol = line.IndexOf(ruleExprText, StringComparison.Ordinal);
            var exprEndCol = exprStartCol + ruleExprText.Length;
            var reasonStartCol = exprEndCol;
            var reasonEndCol = reasonStartCol + ruleReason.Length;

            DslExpression ruleExpr;
            try { ruleExpr = DslExpressionParser.Parse(ruleExprText); }
            catch (InvalidOperationException ex)
            { throw new InvalidOperationException($"Line {index + 1}: invalid rule expression '{ruleExprText}'. {ex.Message}"); }

            rules.Add(new DslRule(ruleExprText, ruleExpr, ruleReason, index + 1, exprStartCol, exprEndCol, reasonStartCol, reasonEndCol));
            index++;
        }

        return new DslState(stateName, rules.Count > 0 ? rules : null);
    }

    /// <summary>
    /// Reads rule lines indented under a field declaration (scalar or collection).
    /// Validates field rule scope restriction (only the owning field is allowed).
    /// Leaves <paramref name="index"/> pointing at the next non-rule line.
    /// </summary>
    private static List<DslRule> ParseFieldRules(string[] lines, ref int index, string owningField, bool isCollection)
    {
        var rules = new List<DslRule>();
        var headerIndent = GetIndentation(lines[index]);
        index++; // advance past the field line

        while (index < lines.Length)
        {
            var raw = lines[index];
            var line = StripInlineComment(raw.Trim());
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var indent = GetIndentation(raw);
            if (indent <= headerIndent)
                break;

            var ruleMatch = RuleRegex.Match(line);
            if (!ruleMatch.Success)
                throw new InvalidOperationException($"Line {index + 1}: only 'rule' statements are allowed indented under a field declaration.");

            var ruleExprText = ruleMatch.Groups["expr"].Value.Trim();
            var ruleReason = ruleMatch.Groups["reason"].Value;

            // Scope restriction: only the declaring field, its dotted properties, and literals
            var allowed = new HashSet<string>(StringComparer.Ordinal) { owningField };
            ValidateRuleScope(ruleExprText, index + 1, allowed, allowedIdentifiers: null, scopeDescription: null, fieldRuleOwner: owningField);

            DslExpression ruleExpr;
            try { ruleExpr = DslExpressionParser.Parse(ruleExprText); }
            catch (InvalidOperationException ex)
            { throw new InvalidOperationException($"Line {index + 1}: invalid rule expression '{ruleExprText}'. {ex.Message}"); }

            var exprStartCol = raw.IndexOf(ruleExprText, StringComparison.Ordinal);
            if (exprStartCol < 0) exprStartCol = line.IndexOf(ruleExprText, StringComparison.Ordinal);
            var exprEndCol = exprStartCol + ruleExprText.Length;
            var reasonStartCol = exprEndCol;
            var reasonEndCol = reasonStartCol + ruleReason.Length;

            rules.Add(new DslRule(ruleExprText, ruleExpr, ruleReason, index + 1, exprStartCol, exprEndCol, reasonStartCol, reasonEndCol));
            index++;
        }

        return rules;
    }

    /// <summary>
    /// Validates that a rule expression only references identifiers in <paramref name="allowedSet"/>
    /// (and their dotted members) plus literals.
    /// When <paramref name="fieldRuleOwner"/> is set, any identifier other than the owner (or its dotted properties)
    /// is rejected with the field-rule-specific message.
    /// </summary>
    private static void ValidateRuleScope(
        string expressionText,
        int lineNumber,
        HashSet<string> allowedSet,
        HashSet<string>? allowedIdentifiers,
        string? scopeDescription,
        string? fieldRuleOwner = null)
    {
        DslExpression parsed;
        try { parsed = DslExpressionParser.Parse(expressionText); }
        catch { return; } // parse errors are reported separately

        ValidateRuleScopeNode(parsed, lineNumber, allowedSet, allowedIdentifiers, scopeDescription, fieldRuleOwner);
    }

    private static void ValidateRuleScopeNode(
        DslExpression expr,
        int lineNumber,
        HashSet<string> allowedSet,
        HashSet<string>? allowedIdentifiers,
        string? scopeDescription,
        string? fieldRuleOwner)
    {
        switch (expr)
        {
            case DslIdentifierExpression id:
                // The base identifier name is always the lookup key (member is a property of the base)
                var baseName = id.Name;
                if (allowedSet.Contains(baseName, StringComparer.Ordinal))
                    return;
                if (allowedIdentifiers is not null && allowedIdentifiers.Contains(baseName, StringComparer.Ordinal))
                    return;
                if (fieldRuleOwner is not null)
                    throw new InvalidOperationException($"Line {lineNumber}: field rule may only reference its own field '{fieldRuleOwner}'; use a top-level rule for cross-field constraints. (offending identifier: '{baseName}')");
                if (scopeDescription is not null)
                    throw new InvalidOperationException($"Line {lineNumber}: {scopeDescription} references undeclared field '{baseName}'.");
                break;
            case DslLiteralExpression:
                return;
            case DslUnaryExpression unary:
                ValidateRuleScopeNode(unary.Operand, lineNumber, allowedSet, allowedIdentifiers, scopeDescription, fieldRuleOwner);
                break;
            case DslBinaryExpression binary:
                ValidateRuleScopeNode(binary.Left, lineNumber, allowedSet, allowedIdentifiers, scopeDescription, fieldRuleOwner);
                ValidateRuleScopeNode(binary.Right, lineNumber, allowedSet, allowedIdentifiers, scopeDescription, fieldRuleOwner);
                break;
            case DslParenthesizedExpression paren:
                ValidateRuleScopeNode(paren.Inner, lineNumber, allowedSet, allowedIdentifiers, scopeDescription, fieldRuleOwner);
                break;
        }
    }

    /// <summary>
    /// Validates that an event rule only references event arg identifiers (bare or prefixed with EventName.).
    /// </summary>
    private static void ValidateEventRuleScope(
        string expressionText,
        int lineNumber,
        HashSet<string> argNames,
        string eventName)
    {
        DslExpression parsed;
        try { parsed = DslExpressionParser.Parse(expressionText); }
        catch { return; }

        ValidateEventRuleScopeNode(parsed, lineNumber, argNames, eventName);
    }

    private static void ValidateEventRuleScopeNode(
        DslExpression expr,
        int lineNumber,
        HashSet<string> argNames,
        string eventName)
    {
        switch (expr)
        {
            case DslIdentifierExpression id:
                // Accept: EventName.ArgName  (Name=eventName, Member=argName)
                if (string.Equals(id.Name, eventName, StringComparison.Ordinal) && id.Member is not null)
                {
                    if (!argNames.Contains(id.Member, StringComparer.Ordinal))
                        throw new InvalidOperationException(
                            $"Line {lineNumber}: event rule may only reference event argument identifiers; '{id.Name}.{id.Member}' is not a declared argument for event '{eventName}'.");
                    return;
                }
                // Accept: bare ArgName  (Name=argName, no member)
                if (id.Member is null && argNames.Contains(id.Name, StringComparer.Ordinal))
                    return;
                // Accept: ArgName.property  (Name=argName, Member=some property)
                if (id.Member is not null && argNames.Contains(id.Name, StringComparer.Ordinal))
                    return;
                // Otherwise: reject
                var displayName = id.Member is not null ? $"{id.Name}.{id.Member}" : id.Name;
                throw new InvalidOperationException(
                    $"Line {lineNumber}: event rule may only reference event argument identifiers; '{displayName}' is not a declared argument for event '{eventName}'.");
            case DslLiteralExpression:
                return;
            case DslUnaryExpression unary:
                ValidateEventRuleScopeNode(unary.Operand, lineNumber, argNames, eventName);
                break;
            case DslBinaryExpression binary:
                ValidateEventRuleScopeNode(binary.Left, lineNumber, argNames, eventName);
                ValidateEventRuleScopeNode(binary.Right, lineNumber, argNames, eventName);
                break;
            case DslParenthesizedExpression paren:
                ValidateEventRuleScopeNode(paren.Inner, lineNumber, argNames, eventName);
                break;
        }
    }

    /// <summary>Collects all identifier base names referenced in an expression (not dotted members).</summary>
    private static void CollectIdentifiers(DslExpression expression, out HashSet<string> names)
    {
        names = new HashSet<string>(StringComparer.Ordinal);
        CollectIdentifiersInto(expression, names);
    }

    private static void CollectIdentifiersInto(DslExpression expression, HashSet<string> names)
    {
        switch (expression)
        {
            case DslIdentifierExpression id:
                names.Add(id.Name);
                break;
            case DslLiteralExpression:
                break;
            case DslUnaryExpression unary:
                CollectIdentifiersInto(unary.Operand, names);
                break;
            case DslBinaryExpression binary:
                CollectIdentifiersInto(binary.Left, names);
                CollectIdentifiersInto(binary.Right, names);
                break;
            case DslParenthesizedExpression paren:
                CollectIdentifiersInto(paren.Inner, names);
                break;
        }
    }

    private static void ParseFromOnBlock(
        string[] lines,
        ref int index,
        Match fromOnMatch,
        IReadOnlyList<DslState> declaredStates,
        ICollection<DslTransition> transitions,
        IReadOnlyList<DslCollectionField> collectionFields,
        IReadOnlyList<DslField> dataFields)
    {
        var headerRaw = lines[index];
        var headerIndent = GetIndentation(headerRaw);
        var headerLineNumber = index + 1;
        var fromToken = fromOnMatch.Groups["from"].Value.Trim();
        var eventName = fromOnMatch.Groups["event"].Value.Trim();
        var whenText = fromOnMatch.Groups["when"].Success ? fromOnMatch.Groups["when"].Value.Trim() : null;

        var sourceStates = fromToken.Equals("any", StringComparison.Ordinal)
            ? declaredStates.Select(s => s.Name).ToList()
            : ParseIdentifierList(fromToken, index + 1, "state");

        if (sourceStates.Count == 0)
            throw new InvalidOperationException($"Line {index + 1}: 'from any' requires states to be declared first.");

        // Parse optional 'when' predicate
        DslExpression? whenAst = null;
        if (whenText is not null)
        {
            try { whenAst = DslExpressionParser.Parse(whenText); }
            catch (InvalidOperationException ex)
            { throw new InvalidOperationException($"Line {index + 1}: invalid 'when' expression '{whenText}'. {ex.Message}"); }
        }

        var clauses = new List<DslClause>();
        var pendingSets = new List<DslSetAssignment>();
        var pendingMutations = new List<DslCollectionMutation>();
        bool reachedOutcome = false;
        bool hasIfChain = false;
        bool hasElseBranch = false;

        index++;
        while (index < lines.Length)
        {
            var raw = lines[index];
            var line = StripInlineComment(raw.Trim());
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var indent = GetIndentation(raw);
            if (indent <= headerIndent)
                break;

            if (reachedOutcome)
                throw new InvalidOperationException($"Line {index + 1}: no statements are allowed after an outcome statement in a from/on block.");

            var ifMatch = IfRegex.Match(line);
            if (ifMatch.Success)
            {
                if (ifMatch.Groups["reason"].Success)
                    throw new InvalidOperationException($"Line {index + 1}: 'reason' is not allowed on 'if' branches. Provide a reason only on 'reject' statements.");

                if (hasIfChain)
                    throw new InvalidOperationException($"Line {index + 1}: use 'else if' to continue an existing if-chain.");

                hasIfChain = true;
                var clauseSourceLine = index + 1;
                var clause = ParseGuardedClause(lines, ref index, indent, ifMatch.Groups["guard"].Value.Trim(), clauseSourceLine, collectionFields, dataFields);
                clauses.Add(clause);
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

                var clauseSourceLine = index + 1;
                var clause = ParseGuardedClause(lines, ref index, indent, elseIfMatch.Groups["guard"].Value.Trim(), clauseSourceLine, collectionFields, dataFields);
                clauses.Add(clause);
                continue;
            }

            if (ElseRegex.IsMatch(line))
            {
                if (!hasIfChain)
                    throw new InvalidOperationException($"Line {index + 1}: 'else' requires a preceding 'if'.");

                if (hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: duplicate 'else' is not allowed.");

                hasElseBranch = true;
                var clauseSourceLine = index + 1;
                var clause = ParseElseClause(lines, ref index, indent, clauseSourceLine, collectionFields, dataFields);
                clauses.Add(clause);
                reachedOutcome = true;
                continue;
            }

            var setAtBlock = SetRegex.Match(line);
            if (setAtBlock.Success)
            {
                if (hasIfChain && !hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: block-level statement after an 'if' chain requires 'else'. Add 'else' before the fallback.");
                pendingSets.Add(ParseSetAssignment(setAtBlock, index + 1));
                index++;
                continue;
            }

            var mutationResult = TryParseCollectionMutation(line, index + 1, collectionFields, dataFields);
            if (mutationResult is not null)
            {
                if (hasIfChain && !hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: block-level statement after an 'if' chain requires 'else'. Add 'else' before the fallback.");
                pendingMutations.Add(mutationResult);
                index++;
                continue;
            }

            var simpleTransitionAtBlock = SimpleTransitionRegex.Match(line);
            if (simpleTransitionAtBlock.Success)
            {
                if (hasIfChain && !hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: block-level statement after an 'if' chain requires 'else'. Add 'else' before the fallback.");
                var targetState = simpleTransitionAtBlock.Groups["to"].Value.Trim();
                var outcomeLineNumber = index + 1;
                clauses.Add(new DslClause(
                    new DslStateTransition(targetState),
                    pendingSets.Count > 0 ? pendingSets.ToArray() : Array.Empty<DslSetAssignment>(),
                    outcomeLineNumber,
                    null,
                    null,
                    pendingMutations.Count > 0 ? pendingMutations.ToArray() : null));
                pendingSets.Clear();
                pendingMutations.Clear();
                reachedOutcome = true;
                index++;
                continue;
            }

            if (line.Equals("no transition", StringComparison.Ordinal))
            {
                if (hasIfChain && !hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: block-level statement after an 'if' chain requires 'else'. Add 'else' before the fallback.");
                var outcomeLineNumber = index + 1;
                clauses.Add(new DslClause(
                    new DslNoTransition(),
                    pendingSets.Count > 0 ? pendingSets.ToArray() : Array.Empty<DslSetAssignment>(),
                    outcomeLineNumber,
                    null,
                    null,
                    pendingMutations.Count > 0 ? pendingMutations.ToArray() : null));
                pendingSets.Clear();
                pendingMutations.Clear();
                reachedOutcome = true;
                index++;
                continue;
            }

            var rejectMatch = RejectRegex.Match(line);
            if (rejectMatch.Success)
            {
                if (hasIfChain && !hasElseBranch)
                    throw new InvalidOperationException($"Line {index + 1}: block-level statement after an 'if' chain requires 'else'. Add 'else' before the fallback.");
                var reason = Unquote(rejectMatch.Groups["reason"].Value.Trim());
                var outcomeLineNumber = index + 1;
                clauses.Add(new DslClause(
                    new DslRejection(reason),
                    Array.Empty<DslSetAssignment>(),
                    outcomeLineNumber,
                    null,
                    null,
                    null));
                reachedOutcome = true;
                index++;
                continue;
            }

            throw new InvalidOperationException($"Line {index + 1}: unrecognized statement '{line}' inside from/on block.");
        }

        if (pendingSets.Count > 0 || pendingMutations.Count > 0)
            throw new InvalidOperationException($"Line {index}: set requires a following transition.");

        if (!reachedOutcome)
            throw new InvalidOperationException($"Line {index}: from/on block must end with an outcome statement: transition <State>, reject <reason>, or no transition.");

        var clauseList = (IReadOnlyList<DslClause>)clauses.AsReadOnly();

        foreach (var sourceState in sourceStates)
        {
            transitions.Add(new DslTransition(
                sourceState,
                eventName,
                clauseList,
                headerLineNumber,
                whenText,
                whenAst));
        }
    }

    private static DslClause ParseGuardedClause(
        string[] lines,
        ref int index,
        int branchHeaderIndent,
        string guardExpression,
        int clauseSourceLine,
        IReadOnlyList<DslCollectionField> collectionFields,
        IReadOnlyList<DslField> dataFields)
    {
        var branchSets = new List<DslSetAssignment>();
        var branchMutations = new List<DslCollectionMutation>();
        DslClauseOutcome? outcome = null;
        bool reachedOutcome = false;

        index++;
        while (index < lines.Length)
        {
            var nestedRaw = lines[index];
            var nestedLine = StripInlineComment(nestedRaw.Trim());
            if (string.IsNullOrWhiteSpace(nestedLine) || nestedLine.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var nestedIndent = GetIndentation(nestedRaw);
            if (nestedIndent <= branchHeaderIndent)
                break;

            if (reachedOutcome)
                throw new InvalidOperationException($"Line {index + 1}: no statements are allowed after an outcome statement in an if-branch.");

            var setMatch = SetRegex.Match(nestedLine);
            if (setMatch.Success)
            {
                branchSets.Add(ParseSetAssignment(setMatch, index + 1));
                index++;
                continue;
            }

            var mutationResult = TryParseCollectionMutation(nestedLine, index + 1, collectionFields, dataFields);
            if (mutationResult is not null)
            {
                branchMutations.Add(mutationResult);
                index++;
                continue;
            }

            var transitionMatch = SimpleTransitionRegex.Match(nestedLine);
            if (transitionMatch.Success)
            {
                if (outcome is not null)
                    throw new InvalidOperationException($"Line {index + 1}: only one outcome statement is allowed in an if-branch.");

                outcome = new DslStateTransition(transitionMatch.Groups["to"].Value.Trim());
                reachedOutcome = true;
                index++;
                continue;
            }

            if (nestedLine.Equals("no transition", StringComparison.Ordinal))
            {
                if (outcome is not null)
                    throw new InvalidOperationException($"Line {index + 1}: only one outcome statement is allowed in an if-branch.");

                outcome = new DslNoTransition();
                reachedOutcome = true;
                index++;
                continue;
            }

            if (RejectRegex.IsMatch(nestedLine))
                throw new InvalidOperationException($"Line {index + 1}: if/else if branches support 'transition <State>' or 'no transition'; use else or a block outcome statement for reject.");

            throw new InvalidOperationException($"Line {index + 1}: expected 'set <Key> = <Expr>', collection mutation, 'transition <State>', or 'no transition' inside if-branch.");
        }

        if (outcome is null)
            throw new InvalidOperationException($"Line {index}: if/else if branch requires 'transition <State>' or 'no transition'.");

        DslExpression? guardAst = null;
        try { guardAst = DslExpressionParser.Parse(guardExpression); }
        catch { /* parse errors reported separately in analyzer */ }

        return new DslClause(
            outcome,
            branchSets.Count > 0 ? (IReadOnlyList<DslSetAssignment>)branchSets.ToArray() : Array.Empty<DslSetAssignment>(),
            clauseSourceLine,
            guardExpression,
            guardAst,
            branchMutations.Count > 0 ? branchMutations.ToArray() : null);
    }

    private static DslClause ParseElseClause(
        string[] lines,
        ref int index,
        int branchHeaderIndent,
        int clauseSourceLine,
        IReadOnlyList<DslCollectionField> collectionFields,
        IReadOnlyList<DslField> dataFields)
    {
        var branchSets = new List<DslSetAssignment>();
        var branchMutations = new List<DslCollectionMutation>();
        DslClauseOutcome? outcome = null;
        bool reachedOutcome = false;

        index++;
        while (index < lines.Length)
        {
            var nestedRaw = lines[index];
            var nestedLine = StripInlineComment(nestedRaw.Trim());
            if (string.IsNullOrWhiteSpace(nestedLine) || nestedLine.StartsWith("#", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var nestedIndent = GetIndentation(nestedRaw);
            if (nestedIndent <= branchHeaderIndent)
                break;

            if (reachedOutcome)
                throw new InvalidOperationException($"Line {index + 1}: no statements are allowed after an outcome statement in an else-branch.");

            var setMatch = SetRegex.Match(nestedLine);
            if (setMatch.Success)
            {
                branchSets.Add(ParseSetAssignment(setMatch, index + 1));
                index++;
                continue;
            }

            var mutationResult = TryParseCollectionMutation(nestedLine, index + 1, collectionFields, dataFields);
            if (mutationResult is not null)
            {
                branchMutations.Add(mutationResult);
                index++;
                continue;
            }

            var transitionMatch = SimpleTransitionRegex.Match(nestedLine);
            if (transitionMatch.Success)
            {
                outcome = new DslStateTransition(transitionMatch.Groups["to"].Value.Trim());
                reachedOutcome = true;
                index++;
                continue;
            }

            if (nestedLine.Equals("no transition", StringComparison.Ordinal))
            {
                outcome = new DslNoTransition();
                reachedOutcome = true;
                index++;
                continue;
            }

            var rejectMatch = RejectRegex.Match(nestedLine);
            if (rejectMatch.Success)
            {
                outcome = new DslRejection(Unquote(rejectMatch.Groups["reason"].Value.Trim()));
                reachedOutcome = true;
                index++;
                continue;
            }

            throw new InvalidOperationException($"Line {index + 1}: expected an outcome statement inside else-branch.");
        }

        if (!reachedOutcome)
            throw new InvalidOperationException($"Line {index}: else branch requires an outcome statement.");

        return new DslClause(
            outcome!,
            branchSets.Count > 0 ? (IReadOnlyList<DslSetAssignment>)branchSets.ToArray() : Array.Empty<DslSetAssignment>(),
            clauseSourceLine,
            null,
            null,
            branchMutations.Count > 0 ? branchMutations.ToArray() : null);
    }

    private static DslCollectionMutation? TryParseCollectionMutation(
        string line,
        int lineNumber,
        IReadOnlyList<DslCollectionField> collectionFields,
        IReadOnlyList<DslField> dataFields)
    {
        // Try each mutation verb
        var addMatch = AddRegex.Match(line);
        if (addMatch.Success)
            return BuildMutation(DslCollectionMutationVerb.Add, addMatch.Groups["field"].Value, addMatch.Groups["expr"].Value.Trim(), lineNumber, collectionFields, DslCollectionKind.Set);

        var removeMatch = RemoveRegex.Match(line);
        if (removeMatch.Success)
            return BuildMutation(DslCollectionMutationVerb.Remove, removeMatch.Groups["field"].Value, removeMatch.Groups["expr"].Value.Trim(), lineNumber, collectionFields, DslCollectionKind.Set);

        var enqueueMatch = EnqueueRegex.Match(line);
        if (enqueueMatch.Success)
            return BuildMutation(DslCollectionMutationVerb.Enqueue, enqueueMatch.Groups["field"].Value, enqueueMatch.Groups["expr"].Value.Trim(), lineNumber, collectionFields, DslCollectionKind.Queue);

        var dequeueMatch = DequeueRegex.Match(line);
        if (dequeueMatch.Success)
        {
            string? intoField = dequeueMatch.Groups["into"].Success ? dequeueMatch.Groups["into"].Value : null;
            return BuildMutationNoExpr(DslCollectionMutationVerb.Dequeue, dequeueMatch.Groups["field"].Value, lineNumber, collectionFields, DslCollectionKind.Queue, dataFields, intoField);
        }

        var pushMatch = PushRegex.Match(line);
        if (pushMatch.Success)
            return BuildMutation(DslCollectionMutationVerb.Push, pushMatch.Groups["field"].Value, pushMatch.Groups["expr"].Value.Trim(), lineNumber, collectionFields, DslCollectionKind.Stack);

        var popMatch = PopRegex.Match(line);
        if (popMatch.Success)
        {
            string? intoField = popMatch.Groups["into"].Success ? popMatch.Groups["into"].Value : null;
            return BuildMutationNoExpr(DslCollectionMutationVerb.Pop, popMatch.Groups["field"].Value, lineNumber, collectionFields, DslCollectionKind.Stack, dataFields, intoField);
        }

        var clearMatch = ClearRegex.Match(line);
        if (clearMatch.Success)
        {
            var fieldName = clearMatch.Groups["field"].Value;
            var field = collectionFields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal));
            if (field is null)
                throw new InvalidOperationException($"Line {lineNumber}: 'clear' targets unknown collection field '{fieldName}'.");

            return new DslCollectionMutation(DslCollectionMutationVerb.Clear, fieldName, null, null);
        }

        return null;
    }

    private static DslCollectionMutation BuildMutation(
        DslCollectionMutationVerb verb,
        string fieldName,
        string expressionText,
        int lineNumber,
        IReadOnlyList<DslCollectionField> collectionFields,
        DslCollectionKind requiredKind)
    {
        var field = collectionFields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal));
        if (field is null)
            throw new InvalidOperationException($"Line {lineNumber}: '{verb.ToString().ToLowerInvariant()}' targets unknown collection field '{fieldName}'.");

        if (field.CollectionKind != requiredKind)
            throw new InvalidOperationException($"Line {lineNumber}: '{verb.ToString().ToLowerInvariant()}' is not valid on {field.CollectionKind.ToString().ToLowerInvariant()}<{field.InnerType.ToString().ToLowerInvariant()}> field '{fieldName}'.");

        try
        {
            var expression = DslExpressionParser.Parse(expressionText);
            return new DslCollectionMutation(verb, fieldName, expressionText, expression);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Line {lineNumber}: invalid expression '{expressionText}'. {ex.Message}");
        }
    }

    private static DslCollectionMutation BuildMutationNoExpr(
        DslCollectionMutationVerb verb,
        string fieldName,
        int lineNumber,
        IReadOnlyList<DslCollectionField> collectionFields,
        DslCollectionKind requiredKind,
        IReadOnlyList<DslField> dataFields,
        string? intoFieldName = null)
    {
        var field = collectionFields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal));
        if (field is null)
            throw new InvalidOperationException($"Line {lineNumber}: '{verb.ToString().ToLowerInvariant()}' targets unknown collection field '{fieldName}'.");

        if (field.CollectionKind != requiredKind)
            throw new InvalidOperationException($"Line {lineNumber}: '{verb.ToString().ToLowerInvariant()}' is not valid on {field.CollectionKind.ToString().ToLowerInvariant()}<{field.InnerType.ToString().ToLowerInvariant()}> field '{fieldName}'.");

        if (intoFieldName is not null)
        {
            var targetField = dataFields.FirstOrDefault(f => string.Equals(f.Name, intoFieldName, StringComparison.Ordinal));
            if (targetField is null)
            {
                // Check if it's a collection field (not allowed)
                var isCollection = collectionFields.Any(f => string.Equals(f.Name, intoFieldName, StringComparison.Ordinal));
                if (isCollection)
                    throw new InvalidOperationException($"Line {lineNumber}: 'into' target '{intoFieldName}' is a collection field. The target must be a scalar data field.");

                throw new InvalidOperationException($"Line {lineNumber}: 'into' target '{intoFieldName}' is not a declared data field.");
            }

            // Validate type compatibility: the scalar field's type must match the collection's inner type
            if (targetField.Type != field.InnerType)
                throw new InvalidOperationException($"Line {lineNumber}: type mismatch — 'into' target '{intoFieldName}' is {targetField.Type.ToString().ToLowerInvariant()}{(targetField.IsNullable ? "?" : "")} but collection '{fieldName}' has inner type {field.InnerType.ToString().ToLowerInvariant()}.");
        }

        return new DslCollectionMutation(verb, fieldName, null, null, intoFieldName);
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

    private static object? ParseFieldDefaultLiteral(string text, DslScalarType type, bool isNullable, string fieldName, int lineNumber)
    {
        DslExpression parsedExpression;
        try
        {
            parsedExpression = DslExpressionParser.Parse(text);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Line {lineNumber}: invalid default value for field '{fieldName}'. {ex.Message}");
        }

        if (parsedExpression is not DslLiteralExpression literal)
            throw new InvalidOperationException($"Line {lineNumber}: default value for field '{fieldName}' must be a literal.");

        if (!IsValidDefaultLiteral(type, isNullable, literal.Value))
            throw new InvalidOperationException($"Line {lineNumber}: default value for field '{fieldName}' does not match declared type.");

        return literal.Value;
    }

    private static bool IsValidDefaultLiteral(DslScalarType type, bool isNullable, object? value)
    {
        if (value is null)
            return isNullable;

        return type switch
        {
            DslScalarType.String => value is string,
            DslScalarType.Boolean => value is bool,
            DslScalarType.Number => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal,
            DslScalarType.Null => false,
            _ => false
        };
    }

    private static string NormalizeDataAssignmentKey(string setKey)
        => setKey;

    /// <summary>
    /// Strips an inline comment from an already-trimmed line.
    /// A <c>#</c> character outside a double-quoted string starts a comment;
    /// everything from that point to the end of the line is discarded.
    /// The result is right-trimmed so trailing whitespace before the <c>#</c> is removed.
    /// </summary>
    private static string StripInlineComment(string line)
    {
        bool inString = false;
        for (int ci = 0; ci < line.Length; ci++)
        {
            char c = line[ci];
            if (c == '"')
                inString = !inString;
            else if (c == '#' && !inString)
                return line.Substring(0, ci).TrimEnd();
        }
        return line;
    }

    private static DslSetAssignment ParseSetAssignment(Match setMatch, int lineNumber)
    {
        var key = NormalizeDataAssignmentKey(setMatch.Groups["setKey"].Value.Trim());
        var expressionText = setMatch.Groups["setExpr"].Value.Trim();

        try
        {
            var expression = DslExpressionParser.Parse(expressionText);
            return new DslSetAssignment(key, expressionText, expression, lineNumber);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Line {lineNumber}: invalid set expression '{expressionText}'. {ex.Message}");
        }
    }

    private static void ValidateReferences(
        IReadOnlyCollection<DslState> states,
        IReadOnlyCollection<DslEvent> events,
        IReadOnlyCollection<DslTransition> transitions,
        IReadOnlyCollection<DslField> dataFields,
        IReadOnlyCollection<DslCollectionField> collectionFields)
    {
        var stateSet = new HashSet<string>(states.Select(s => s.Name), StringComparer.Ordinal);
        var eventSet = new HashSet<string>(events.Select(e => e.Name), StringComparer.Ordinal);

        foreach (var transition in transitions)
        {
            var tPrefix = transition.SourceLine > 0 ? $"Line {transition.SourceLine}: " : string.Empty;

            if (!stateSet.Contains(transition.FromState))
                throw new InvalidOperationException($"{tPrefix}Transition references unknown source state '{transition.FromState}'.");

            if (!eventSet.Contains(transition.EventName))
                throw new InvalidOperationException($"{tPrefix}Transition references unknown event '{transition.EventName}'.");

            foreach (var clause in transition.Clauses)
            {
                var cPrefix = clause.SourceLine > 0 ? $"Line {clause.SourceLine}: " : tPrefix;

                if (clause.Outcome is DslStateTransition st)
                {
                    if (!stateSet.Contains(st.TargetState))
                        throw new InvalidOperationException($"{cPrefix}Transition references unknown target state '{st.TargetState}'.");
                }

                if (clause.Outcome is DslRejection rej && string.IsNullOrWhiteSpace(rej.Reason))
                    throw new InvalidOperationException($"{cPrefix}Reject outcome requires a reason.");

                foreach (var assignment in clause.SetAssignments)
                {
                    if (dataFields.Count == 0)
                        continue;

                    var aPrefix = assignment.SourceLine > 0 ? $"Line {assignment.SourceLine}: " : cPrefix;
                    var knownDataField = dataFields.Any(f => string.Equals(f.Name, assignment.Key, StringComparison.Ordinal));
                    if (!knownDataField)
                        throw new InvalidOperationException($"{aPrefix}Transition '{transition.FromState}' on '{transition.EventName}' assigns unknown data field '{assignment.Key}'.");
                }
            }
        }
    }
}
