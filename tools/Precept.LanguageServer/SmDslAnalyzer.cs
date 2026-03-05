using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;

namespace Precept.LanguageServer;

internal sealed class SmDslAnalyzer
{
    private static readonly Regex LineErrorRegex = new("^Line\\s+(?<line>\\d+)\\s*:\\s*(?<message>.+)$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new("^\\s*state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("^\\s*event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex DataFieldRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*.+)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventArgRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*.+)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FromOnRegex = new("^\\s*from\\s+(?<from>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventMemberPrefixRegex = new("(?<event>[A-Za-z_][A-Za-z0-9_]*)\\.$", RegexOptions.Compiled);
    private static readonly Regex SetLineRegex = new("^\\s*set\\s+(?<key>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<expr>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CollectionDeclRegex = new("^\\s*(?:set|queue|stack)<(?:number|string|boolean)>\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly MethodInfo? ExpressionParseMethod = typeof(DslWorkflowModel).Assembly
        .GetType("Precept.DslExpressionParser", throwOnError: false)
        ?.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

    private readonly ConcurrentDictionary<DocumentUri, string> _documents = new();

    public void SetDocumentText(DocumentUri uri, string text)
        => _documents[uri] = text;

    public void RemoveDocument(DocumentUri uri)
        => _documents.TryRemove(uri, out _);

    public bool TryGetDocumentText(DocumentUri uri, out string text)
        => _documents.TryGetValue(uri, out text!);

    public IReadOnlyList<Diagnostic> GetDiagnostics(DocumentUri uri)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return Array.Empty<Diagnostic>();

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        try
        {
            var model = PreceptParser.Parse(text);
            PreceptCompiler.Compile(model);

            var diagnostics = GetSemanticDiagnostics(model, lines);
            return diagnostics.Count == 0 ? Array.Empty<Diagnostic>() : diagnostics;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            var diagnostic = ToDiagnostic(ex.Message, lines);
            return new[] { diagnostic };
        }
    }

    public IReadOnlyList<CompletionItem> GetCompletions(DocumentUri uri, Position position)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return KeywordItems;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (position.Line >= lines.Length)
            return KeywordItems;

        var lineText = lines[(int)position.Line];
        var beforeCursor = position.Character <= lineText.Length
            ? lineText[..(int)position.Character]
            : lineText;

        var states = CollectIdentifiers(text, StateRegex);
        var events = CollectIdentifiers(text, EventRegex);
        var dataFields = CollectTopLevelDataFields(lines);
        var collectionFields = CollectIdentifiers(text, CollectionDeclRegex);
        var eventArgs = CollectEventArgs(lines);
        var currentEvent = FindCurrentEventName(lines, (int)position.Line);

        // Collection member prefix: e.g. "Floors." → suggest .count, .min, .max, .peek
        var collectionMemberPrefixMatch = Regex.Match(beforeCursor, "(?<col>[A-Za-z_][A-Za-z0-9_]*)\\.$");
        if (collectionMemberPrefixMatch.Success)
        {
            var colName = collectionMemberPrefixMatch.Groups["col"].Value;
            if (collectionFields.Contains(colName, StringComparer.Ordinal))
            {
                return new CompletionItem[]
                {
                    new() { Label = colName + ".count", Kind = CompletionItemKind.Property, Detail = "Number of elements" },
                    new() { Label = colName + ".min", Kind = CompletionItemKind.Property, Detail = "Minimum element (set only)" },
                    new() { Label = colName + ".max", Kind = CompletionItemKind.Property, Detail = "Maximum element (set only)" },
                    new() { Label = colName + ".peek", Kind = CompletionItemKind.Property, Detail = "Front/top element (queue/stack)" }
                };
            }
        }

        var eventMemberPrefixMatch = EventMemberPrefixRegex.Match(beforeCursor);
        if (eventMemberPrefixMatch.Success)
        {
            var eventName = eventMemberPrefixMatch.Groups["event"].Value;
            if (eventArgs.TryGetValue(eventName, out var argsForEvent))
                return BuildItems(argsForEvent, CompletionItemKind.Field);
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*(?:if|else\\s+if)\\s+[^\\n]*$", RegexOptions.IgnoreCase))
        {
            var guardItems = BuildGuardCompletions(dataFields, collectionFields, currentEvent, eventArgs);
            return DistinctAndSort(guardItems.Concat(GuardSnippetItems));
        }

        // rule expression: offer all in-scope identifiers + operators
        if (Regex.IsMatch(beforeCursor, "^\\s*rule\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return DistinctAndSort(BuildGuardCompletions(dataFields, collectionFields, currentEvent, eventArgs));

        // reject: offer a string snippet
        if (Regex.IsMatch(beforeCursor, "^\\s*reject(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return [SnippetItem("reject reason", "reject \"${1:Reason}\"", "Rejection reason")];

        // Mutation verb lines: suggest collection field names after add/remove/enqueue/dequeue/push/pop/clear
        if (Regex.IsMatch(beforeCursor, "^\\s*(?:add|remove|enqueue|dequeue|push|pop|clear)(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
        {
            // After "dequeue <Field> into" or "pop <Field> into", suggest scalar data fields
            if (Regex.IsMatch(beforeCursor, "^\\s*(?:dequeue|pop)\\s+[A-Za-z_][A-Za-z0-9_]*\\s+into(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
                return BuildItems(dataFields, CompletionItemKind.Field);

            // After "dequeue <Field>" or "pop <Field>", suggest "into" keyword and collection fields
            if (Regex.IsMatch(beforeCursor, "^\\s*(?:dequeue|pop)\\s+[A-Za-z_][A-Za-z0-9_]*(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            {
                var items = new List<CompletionItem>(BuildItems(collectionFields, CompletionItemKind.Field));
                items.Add(new CompletionItem { Label = "into", Kind = CompletionItemKind.Keyword });
                return items.ToArray();
            }

            // After "add|remove|push|enqueue <Field> ", suggest expression (the value to add/push/remove)
            if (Regex.IsMatch(beforeCursor, "^\\s*(?:add|remove|push|enqueue)\\s+[A-Za-z_][A-Za-z0-9_]*\\s+[^\\n]*$", RegexOptions.IgnoreCase))
                return BuildExpressionCompletions(dataFields, currentEvent, eventArgs);

            return BuildItems(collectionFields, CompletionItemKind.Field);
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*set\\s+[^=\\n]*$", RegexOptions.IgnoreCase) &&
            !beforeCursor.Contains('=', StringComparison.Ordinal))
            return DistinctAndSort(BuildItems(dataFields, CompletionItemKind.Field).Concat(SetSnippetItems));

        if (Regex.IsMatch(beforeCursor, "^\\s*set\\s+[A-Za-z_][A-Za-z0-9_]*\\s*=\\s*[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildExpressionCompletions(dataFields, currentEvent, eventArgs, collectionFields);

        // After "from <State> on <Event> when ", suggest expression completions
        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+\\S+\\s+on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+when\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildGuardCompletions(dataFields, collectionFields, currentEvent, eventArgs);

        // After "from <State> on <Event> " (trailing space, nothing else), suggest "when"
        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+\\S+\\s+on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "when", Kind = CompletionItemKind.Keyword }];

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*\\s+on(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*$", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(beforeCursor, "\\s+on\\s+", RegexOptions.IgnoreCase))
        {
            var stateItems = BuildItems(states.Append("any"), CompletionItemKind.EnumMember);
            return stateItems.Concat(KeywordItems.Where(item => item.Label == "on")).ToArray();
        }

        // After "state <Name> ", suggest the "initial" keyword
        if (Regex.IsMatch(beforeCursor, "^\\s*state\\s+[A-Za-z_][A-Za-z0-9_]*\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "initial", Kind = CompletionItemKind.Keyword }];

        if (Regex.IsMatch(beforeCursor, "^\\s*transition(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return DistinctAndSort(BuildItems(states, CompletionItemKind.EnumMember).Concat(TransitionSnippetItems));

        if (Regex.IsMatch(beforeCursor, "^\\s*inspect\\s+[^\\n]*$", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(beforeCursor, "^\\s*fire\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        return DistinctAndSort(KeywordItems.Concat(GlobalSnippetItems));
    }

    private static IReadOnlyList<CompletionItem> BuildGuardCompletions(
        IReadOnlyList<string> dataFields,
        IReadOnlyList<string> collectionFields,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(BuildItems(collectionFields, CompletionItemKind.Variable));
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);

        if (!string.IsNullOrWhiteSpace(currentEvent) && eventArgs.TryGetValue(currentEvent, out var argsForEvent) && argsForEvent.Count > 0)
        {
            items.Add(new CompletionItem
            {
                Label = currentEvent + ".",
                Kind = CompletionItemKind.Module
            });

            items.AddRange(BuildItems(argsForEvent.Select(arg => currentEvent + "." + arg), CompletionItemKind.Field));
        }

        return DistinctAndSort(items);
    }

    private static IReadOnlyList<CompletionItem> BuildExpressionCompletions(
        IReadOnlyList<string> dataFields,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs,
        IReadOnlyList<string>? collectionFields = null)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);

        // Collection field members are valid in set-RHS expressions (e.g. MySet.count)
        if (collectionFields is not null)
        {
            foreach (var col in collectionFields)
            {
                items.Add(new CompletionItem { Label = col + ".", Kind = CompletionItemKind.Module });
                items.Add(new CompletionItem { Label = col + ".count", Kind = CompletionItemKind.Property, Detail = "Number of elements" });
                items.Add(new CompletionItem { Label = col + ".min", Kind = CompletionItemKind.Property, Detail = "Minimum element (set only)" });
                items.Add(new CompletionItem { Label = col + ".max", Kind = CompletionItemKind.Property, Detail = "Maximum element (set only)" });
                items.Add(new CompletionItem { Label = col + ".peek", Kind = CompletionItemKind.Property, Detail = "Front/top element (queue/stack)" });
            }
        }

        if (!string.IsNullOrWhiteSpace(currentEvent) && eventArgs.TryGetValue(currentEvent, out var argsForEvent) && argsForEvent.Count > 0)
        {
            items.Add(new CompletionItem
            {
                Label = currentEvent + ".",
                Kind = CompletionItemKind.Module
            });

            items.AddRange(BuildItems(argsForEvent.Select(arg => currentEvent + "." + arg), CompletionItemKind.Field));
        }

        return DistinctAndSort(items);
    }

    private static IReadOnlyList<string> CollectTopLevelDataFields(string[] lines)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        var inEventArgs = false;
        var eventIndent = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                continue;

            var indent = raw.Length - raw.TrimStart().Length;

            if (Regex.IsMatch(trimmed, "^event\\s+[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.IgnoreCase))
            {
                inEventArgs = true;
                eventIndent = indent;
                continue;
            }

            if (inEventArgs && indent > eventIndent)
                continue;

            inEventArgs = false;

            var match = DataFieldRegex.Match(raw);
            if (match.Success)
                fields.Add(match.Groups["name"].Value);
        }

        return fields.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CollectEventArgs(string[] lines)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var currentEvent = string.Empty;
        var currentIndent = 0;
        var currentArgs = new List<string>();
        var inEvent = false;

        void FlushCurrentEvent()
        {
            if (inEvent && !string.IsNullOrWhiteSpace(currentEvent))
                result[currentEvent] = currentArgs.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                continue;

            var eventMatch = EventRegex.Match(raw);
            if (eventMatch.Success)
            {
                FlushCurrentEvent();
                currentEvent = eventMatch.Groups["name"].Value;
                currentIndent = raw.Length - raw.TrimStart().Length;
                currentArgs = new List<string>();
                inEvent = true;
                continue;
            }

            if (!inEvent)
                continue;

            var indent = raw.Length - raw.TrimStart().Length;
            if (indent <= currentIndent)
            {
                FlushCurrentEvent();
                inEvent = false;
                currentEvent = string.Empty;
                currentArgs = new List<string>();
                continue;
            }

            var argMatch = EventArgRegex.Match(raw);
            if (argMatch.Success)
                currentArgs.Add(argMatch.Groups["name"].Value);
        }

        FlushCurrentEvent();
        return result;
    }

    private static string? FindCurrentEventName(string[] lines, int lineIndex)
    {
        var start = Math.Min(Math.Max(lineIndex, 0), lines.Length - 1);
        for (var i = start; i >= 0; i--)
        {
            var match = FromOnRegex.Match(lines[i]);
            if (match.Success)
                return match.Groups["event"].Value;
        }

        return null;
    }

    private static IReadOnlyList<CompletionItem> DistinctAndSort(IEnumerable<CompletionItem> items)
        => items
            .GroupBy(item => item.Label, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(item => item.Label, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<Diagnostic> GetSemanticDiagnostics(DslWorkflowModel model, string[] lines)
    {
        var diagnostics = new List<Diagnostic>();
        var dataFieldKinds = model.Fields.ToDictionary(
            field => field.Name,
            MapFieldContractKind,
            StringComparer.Ordinal);

        var eventArgKinds = model.Events.ToDictionary(
            evt => evt.Name,
            evt => evt.Args.ToDictionary(
                arg => arg.Name,
                MapFieldContractKind,
                StringComparer.Ordinal),
            StringComparer.Ordinal);

        var collectionFieldMap = model.CollectionFields.ToDictionary(
            c => c.Name,
            c => c,
            StringComparer.Ordinal);

        // Process each transition (one per (FromState, EventName) pair), iterating clauses in order
        // so that prior-branch guard negations are accumulated for subsequent branches.
        foreach (var transition in model.Transitions)
        {
            var eventName = transition.EventName;
            var baseSymbols = BuildSymbolKinds(dataFieldKinds, eventArgKinds, eventName, model.CollectionFields);

            // Validate the transition-level `when` predicate if present
            if (!string.IsNullOrWhiteSpace(transition.Predicate))
            {
                var whenLine = Math.Max(0, transition.SourceLine - 1);
                ValidateExpression(
                    transition.Predicate!,
                    whenLine,
                    baseSymbols,
                    expectedKind: StaticValueKind.Boolean,
                    expectedLabel: "when predicate",
                    diagnostics,
                    lines);
            }

            // branchSymbols starts as baseSymbols narrowed by the 'when' predicate (if any):
            // within the block, 'when' is true, so identifiers can be narrowed accordingly.
            // The 'false' (NotApplicable) path has no code to validate — no narrowing needed there.
            IReadOnlyDictionary<string, StaticValueKind> branchSymbols = transition.PredicateAst is not null
                ? ApplyNarrowing(transition.PredicateAst, baseSymbols, assumeTrue: true)
                : baseSymbols;

            foreach (var clause in transition.Clauses)
            {
                var searchLine = Math.Max(0, clause.SourceLine - 1);
                var fallbackLine = Math.Max(0, clause.SourceLine - 1);

                // setSymbols is used for set-assignment and mutation validation within this branch.
                IReadOnlyDictionary<string, StaticValueKind> setSymbols = branchSymbols;

                if (!string.IsNullOrWhiteSpace(clause.Predicate))
                {
                    var guardLine = FindGuardLine(lines, ref searchLine, clause.Predicate!, fallbackLine);
                    ValidateExpression(
                        clause.Predicate!,
                        guardLine,
                        branchSymbols,
                        expectedKind: StaticValueKind.Boolean,
                        expectedLabel: "guard expression",
                        diagnostics,
                        lines);

                    if (TryParseExpression(clause.Predicate!, out var parsedGuard, out _))
                    {
                        // Within this branch the guard is true — narrow symbols for set/mutation validation.
                        setSymbols = ApplyNarrowing(parsedGuard!, branchSymbols, assumeTrue: true);
                        // For the next branch the guard was false — accumulate the negation.
                        branchSymbols = ApplyNarrowing(parsedGuard!, branchSymbols, assumeTrue: false);
                    }
                }

                foreach (var assignment in clause.SetAssignments)
                {
                    var assignmentFallback = assignment.SourceLine > 0 ? assignment.SourceLine - 1 : fallbackLine;
                    var assignmentLine = FindSetLine(lines, ref searchLine, assignment.Key, assignment.ExpressionText, assignmentFallback);
                    if (!dataFieldKinds.TryGetValue(assignment.Key, out var targetKind))
                        continue;

                    ValidateExpression(
                        assignment.ExpressionText,
                        assignmentLine,
                        setSymbols,
                        expectedKind: targetKind,
                        expectedLabel: $"set target '{assignment.Key}'",
                        diagnostics,
                        lines);
                }

                ValidateCollectionMutations(
                    clause.CollectionMutations,
                    setSymbols,
                    dataFieldKinds,
                    collectionFieldMap,
                    ref searchLine,
                    fallbackLine,
                    diagnostics,
                    lines);
            }
        }

        if (model.Events.Count == 0)
        {
            var machineLine = 0;
            for (var li = 0; li < lines.Length; li++)
            {
                if (lines[li].TrimStart().StartsWith("precept ", StringComparison.Ordinal))
                {
                    machineLine = li;
                    break;
                }
            }

            var lineLength = machineLine < lines.Length ? lines[machineLine].Length : 1;
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Hint,
                Message = "Precept has no events declared. It cannot respond to any input.",
                Source = "state-machine-dsl",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(machineLine, 0),
                    new Position(machineLine, Math.Max(1, lineLength)))
            });
        }

        // Validate rules
        ValidateRuleDiagnostics(model, lines, dataFieldKinds, eventArgKinds, collectionFieldMap, diagnostics);

        return diagnostics;
    }

    private static void ValidateRuleDiagnostics(
        DslWorkflowModel model,
        string[] lines,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        IReadOnlyDictionary<string, DslCollectionField> collectionFieldMap,
        List<Diagnostic> diagnostics)
    {
        // Symbols for data fields (field rules and top-level rules scope)
        var dataSymbols = new Dictionary<string, StaticValueKind>(dataFieldKinds, StringComparer.Ordinal);
        foreach (var col in model.CollectionFields)
        {
            dataSymbols[$"{col.Name}.count"] = StaticValueKind.Number;
        }

        // Field rules: validate expression against single-field scope
        foreach (var field in model.Fields)
        {
            if (field.Rules is null) continue;
            var fieldSymbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal)
            {
                [field.Name] = dataFieldKinds.TryGetValue(field.Name, out var fk) ? fk : StaticValueKind.None
            };
            foreach (var rule in field.Rules)
            {
                var lineIndex = Math.Max(0, rule.SourceLine - 1);
                ValidateExpression(rule.ExpressionText, lineIndex, fieldSymbols, StaticValueKind.Boolean, $"field rule on '{field.Name}'", diagnostics, lines);
            }
        }

        // Collection field rules: validate expression against single-collection scope
        foreach (var col in model.CollectionFields)
        {
            if (col.Rules is null) continue;
            var colSymbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal)
            {
                [$"{col.Name}.count"] = StaticValueKind.Number
            };
            foreach (var rule in col.Rules)
            {
                var lineIndex = Math.Max(0, rule.SourceLine - 1);
                ValidateExpression(rule.ExpressionText, lineIndex, colSymbols, StaticValueKind.Boolean, $"collection rule on '{col.Name}'", diagnostics, lines);
            }
        }

        // Top-level rules: validate against all data fields
        if (model.TopLevelRules is not null)
        {
            foreach (var rule in model.TopLevelRules)
            {
                var lineIndex = Math.Max(0, rule.SourceLine - 1);
                ValidateExpression(rule.ExpressionText, lineIndex, dataSymbols, StaticValueKind.Boolean, "top-level rule", diagnostics, lines);
            }
        }

        // State rules: validate against all data fields
        foreach (var state in model.States)
        {
            if (state.Rules is null) continue;
            foreach (var rule in state.Rules)
            {
                var lineIndex = Math.Max(0, rule.SourceLine - 1);
                ValidateExpression(rule.ExpressionText, lineIndex, dataSymbols, StaticValueKind.Boolean, $"state rule on '{state.Name}'", diagnostics, lines);
            }
        }

        // Event rules: validate against event arg scope only
        foreach (var evt in model.Events)
        {
            if (evt.Rules is null) continue;
            var evtSymbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
            foreach (var arg in evt.Args)
            {
                var argKind = arg.Type switch
                {
                    DslScalarType.String => StaticValueKind.String,
                    DslScalarType.Number => StaticValueKind.Number,
                    DslScalarType.Boolean => StaticValueKind.Boolean,
                    _ => StaticValueKind.None
                };
                if (arg.IsNullable) argKind |= StaticValueKind.Null;
                evtSymbols[$"{evt.Name}.{arg.Name}"] = argKind;
                evtSymbols[arg.Name] = argKind;
            }

            foreach (var rule in evt.Rules)
            {
                var lineIndex = Math.Max(0, rule.SourceLine - 1);
                ValidateExpression(rule.ExpressionText, lineIndex, evtSymbols, StaticValueKind.Boolean, $"event rule on '{evt.Name}'", diagnostics, lines);
            }
        }

        // Warn about states with entry rules that are never targeted by any transition
        var statesWithRules = model.States.Where(s => s.Rules is not null && s.Rules.Count > 0).ToList();
        if (statesWithRules.Count > 0)
        {
            var targetedStates = new HashSet<string>(
                model.Transitions
                    .SelectMany(t => t.Clauses)
                    .Select(c => c.Outcome)
                    .OfType<DslStateTransition>()
                    .Select(st => st.TargetState),
                StringComparer.Ordinal);
            foreach (var state in statesWithRules)
            {
                if (!targetedStates.Contains(state.Name))
                {
                    // Find the state line in the source
                    var stateLineIdx = FindStateLine(lines, state.Name);
                    var lineLen = stateLineIdx < lines.Length ? lines[stateLineIdx].Length : 1;
                    diagnostics.Add(new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Message = $"State '{state.Name}' has entry rules but no transition targets it — entry rules are never checked.",
                        Source = "state-machine-dsl",
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new Position(stateLineIdx, 0),
                            new Position(stateLineIdx, Math.Max(1, lineLen)))
                    });
                }
            }
        }
    }

    private static int FindStateLine(string[] lines, string stateName)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith($"state {stateName}", StringComparison.Ordinal))
                return i;
        }
        return 0;
    }

    private static Dictionary<string, StaticValueKind> BuildSymbolKinds(
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        string eventName,
        IReadOnlyList<DslCollectionField>? collectionFields = null)
    {
        var symbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
        foreach (var pair in dataFieldKinds)
            symbols[pair.Key] = pair.Value;

        if (eventArgKinds.TryGetValue(eventName, out var eventArgs))
        {
            foreach (var pair in eventArgs)
                symbols[$"{eventName}.{pair.Key}"] = pair.Value;
        }

        // Add collection property symbols
        if (collectionFields is not null)
        {
            foreach (var col in collectionFields)
            {
                var innerKind = col.InnerType switch
                {
                    DslScalarType.Number => StaticValueKind.Number,
                    DslScalarType.String => StaticValueKind.String,
                    DslScalarType.Boolean => StaticValueKind.Boolean,
                    _ => StaticValueKind.None
                };

                symbols[$"{col.Name}.count"] = StaticValueKind.Number;
                symbols[$"{col.Name}.min"] = innerKind;
                symbols[$"{col.Name}.max"] = innerKind;
                symbols[$"{col.Name}.peek"] = innerKind;
            }
        }

        return symbols;
    }

    private static void ValidateExpression(
        string expressionText,
        int lineIndex,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        StaticValueKind expectedKind,
        string expectedLabel,
        List<Diagnostic> diagnostics,
        string[] lines)
    {
        if (!TryParseExpression(expressionText, out var parsedExpression, out var parseError))
        {
            diagnostics.Add(CreateLineDiagnostic(lines, lineIndex, parseError));
            return;
        }

        if (!TryInferKind(parsedExpression!, symbols, out var actualKind, out var semanticError))
        {
            diagnostics.Add(CreateLineDiagnostic(lines, lineIndex, semanticError));
            return;
        }

        if (!IsAssignable(actualKind, expectedKind))
        {
            diagnostics.Add(CreateLineDiagnostic(
                lines,
                lineIndex,
                $"{expectedLabel} type mismatch: expected {FormatKinds(expectedKind)} but expression produces {FormatKinds(actualKind)}."));
        }
    }

    private static bool TryParseExpression(string expression, out DslExpression? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        if (ExpressionParseMethod is null)
        {
            error = "expression analyzer unavailable in language server.";
            return false;
        }

        try
        {
            parsed = ExpressionParseMethod.Invoke(null, [expression]) as DslExpression;
            if (parsed is null)
            {
                error = "expression parse failed.";
                return false;
            }

            return true;
        }
        catch (TargetInvocationException tie)
        {
            error = tie.InnerException?.Message ?? tie.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryInferKind(
        DslExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        out StaticValueKind kind,
        out string error)
    {
        kind = StaticValueKind.None;
        error = string.Empty;

        switch (expression)
        {
            case DslLiteralExpression literal:
                kind = MapLiteralKind(literal.Value);
                return true;

            case DslIdentifierExpression identifier:
            {
                var key = identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
                if (!symbols.TryGetValue(key, out var symbolKind))
                {
                    error = $"unknown identifier '{key}'.";
                    return false;
                }

                kind = symbolKind;
                return true;
            }

            case DslParenthesizedExpression parenthesized:
                return TryInferKind(parenthesized.Inner, symbols, out kind, out error);

            case DslUnaryExpression unary:
            {
                if (!TryInferKind(unary.Operand, symbols, out var operandKind, out error))
                    return false;

                if (unary.Operator == "!")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Boolean))
                    {
                        error = "operator '!' requires boolean operand.";
                        return false;
                    }

                    kind = StaticValueKind.Boolean;
                    return true;
                }

                if (unary.Operator == "-")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Number))
                    {
                        error = "unary '-' requires numeric operand.";
                        return false;
                    }

                    kind = StaticValueKind.Number;
                    return true;
                }

                error = $"unsupported unary operator '{unary.Operator}'.";
                return false;
            }

            case DslBinaryExpression binary:
            {
                switch (binary.Operator)
                {
                    case "&&":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Boolean))
                        {
                            error = "operator '&&' requires boolean operands.";
                            return false;
                        }

                        var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
                        if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(rightKind, StaticValueKind.Boolean))
                        {
                            error = "operator '&&' requires boolean operands.";
                            return false;
                        }

                        kind = StaticValueKind.Boolean;
                        return true;
                    }

                    case "||":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Boolean))
                        {
                            error = "operator '||' requires boolean operands.";
                            return false;
                        }

                        var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: false);
                        if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(rightKind, StaticValueKind.Boolean))
                        {
                            error = "operator '||' requires boolean operands.";
                            return false;
                        }

                        kind = StaticValueKind.Boolean;
                        return true;
                    }

                    case "+":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out var rightKind, out error))
                            return false;

                        var stringCandidate = IsExactly(leftKind, StaticValueKind.String) && IsExactly(rightKind, StaticValueKind.String);
                        var numberCandidate = IsExactly(leftKind, StaticValueKind.Number) && IsExactly(rightKind, StaticValueKind.Number);

                        if (stringCandidate && !numberCandidate)
                        {
                            kind = StaticValueKind.String;
                            return true;
                        }

                        if (numberCandidate && !stringCandidate)
                        {
                            kind = StaticValueKind.Number;
                            return true;
                        }

                        error = "operator '+' requires number+number or string+string.";
                        return false;
                    }

                    case "-":
                    case "*":
                    case "/":
                    case "%":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Number) || !IsExactly(rightKind, StaticValueKind.Number))
                        {
                            error = $"operator '{binary.Operator}' requires numeric operands.";
                            return false;
                        }

                        kind = StaticValueKind.Number;
                        return true;
                    }

                    case ">":
                    case ">=":
                    case "<":
                    case "<=":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Number) || !IsExactly(rightKind, StaticValueKind.Number))
                        {
                            error = $"operator '{binary.Operator}' requires numeric operands.";
                            return false;
                        }

                        kind = StaticValueKind.Boolean;
                        return true;
                    }

                    case "==":
                    case "!=":
                        if (!TryInferKind(binary.Left, symbols, out _, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out _, out error))
                            return false;

                        kind = StaticValueKind.Boolean;
                        return true;

                    case "contains":
                        // contains is always boolean — left side is a collection identifier, right is a value
                        kind = StaticValueKind.Boolean;
                        return true;

                    default:
                        error = $"unsupported binary operator '{binary.Operator}'.";
                        return false;
                }
            }

            default:
                error = "unsupported expression node.";
                return false;
        }
    }

    private static IReadOnlyDictionary<string, StaticValueKind> ApplyNarrowing(
        DslExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        bool assumeTrue)
    {
        expression = StripParentheses(expression);

        if (expression is DslUnaryExpression { Operator: "!" } unary)
            return ApplyNarrowing(unary.Operand, symbols, !assumeTrue);

        if (expression is DslBinaryExpression binary)
        {
            if (binary.Operator == "&&")
            {
                if (assumeTrue)
                {
                    var leftNarrowed = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
                    return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: true);
                }

                return symbols;
            }

            if (binary.Operator == "||")
            {
                if (!assumeTrue)
                {
                    var leftNarrowed = ApplyNarrowing(binary.Left, symbols, assumeTrue: false);
                    return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: false);
                }

                return symbols;
            }

            if (binary.Operator is "==" or "!=" &&
                TryApplyNullComparisonNarrowing(binary, symbols, assumeTrue, out var narrowed))
                return narrowed;
        }

        return symbols;
    }

    private static bool TryApplyNullComparisonNarrowing(
        DslBinaryExpression binary,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        bool assumeTrue,
        out IReadOnlyDictionary<string, StaticValueKind> narrowed)
    {
        narrowed = symbols;

        if (!TryGetIdentifierKey(binary.Left, out var leftKey) && !TryGetIdentifierKey(binary.Right, out leftKey))
            return false;

        var leftIsNull = IsNullLiteral(binary.Left);
        var rightIsNull = IsNullLiteral(binary.Right);
        if (!leftIsNull && !rightIsNull)
            return false;

        if (!symbols.TryGetValue(leftKey, out var existingKind))
            return false;

        var expectsNull = binary.Operator switch
        {
            "==" => assumeTrue,
            "!=" => !assumeTrue,
            _ => false
        };

        var updatedKind = expectsNull
            ? StaticValueKind.Null
            : (existingKind & ~StaticValueKind.Null);

        var updated = new Dictionary<string, StaticValueKind>(symbols, StringComparer.Ordinal)
        {
            [leftKey] = updatedKind
        };

        narrowed = updated;
        return true;
    }

    private static void ValidateCollectionMutations(
        IReadOnlyList<DslCollectionMutation>? mutations,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, DslCollectionField> collectionFieldMap,
        ref int searchLine,
        int fallbackLine,
        List<Diagnostic> diagnostics,
        string[] lines)
    {
        if (mutations is null || mutations.Count == 0)
            return;

        foreach (var mutation in mutations)
        {
            if (!collectionFieldMap.TryGetValue(mutation.TargetField, out var colField))
                continue;

            var innerKind = MapScalarTypeToKind(colField.InnerType);
            var verbLabel = mutation.Verb.ToString().ToLowerInvariant();
            var mutationLine = FindMutationLine(lines, ref searchLine, verbLabel, mutation.TargetField, fallbackLine);

            switch (mutation.Verb)
            {
                case DslCollectionMutationVerb.Add:
                case DslCollectionMutationVerb.Remove:
                case DslCollectionMutationVerb.Enqueue:
                case DslCollectionMutationVerb.Push:
                    if (!string.IsNullOrWhiteSpace(mutation.ExpressionText))
                    {
                        ValidateExpression(
                            mutation.ExpressionText!,
                            mutationLine,
                            symbols,
                            expectedKind: innerKind,
                            expectedLabel: $"'{verbLabel} {mutation.TargetField}' value",
                            diagnostics,
                            lines);
                    }

                    break;

                case DslCollectionMutationVerb.Dequeue:
                case DslCollectionMutationVerb.Pop:
                    if (!string.IsNullOrWhiteSpace(mutation.IntoField) &&
                        dataFieldKinds.TryGetValue(mutation.IntoField!, out var intoKind))
                    {
                        if (!IsAssignable(innerKind, intoKind))
                        {
                            diagnostics.Add(CreateLineDiagnostic(
                                lines,
                                mutationLine,
                                $"'{verbLabel} {mutation.TargetField} into {mutation.IntoField}': " +
                                $"cannot assign {FormatKinds(innerKind)} to target '{mutation.IntoField}' of type {FormatKinds(intoKind)}."));
                        }
                    }

                    break;

                case DslCollectionMutationVerb.Clear:
                    break;
            }
        }
    }

    private static StaticValueKind MapScalarTypeToKind(DslScalarType type) => type switch
    {
        DslScalarType.String => StaticValueKind.String,
        DslScalarType.Number => StaticValueKind.Number,
        DslScalarType.Boolean => StaticValueKind.Boolean,
        DslScalarType.Null => StaticValueKind.Null,
        _ => StaticValueKind.None
    };

    private static bool TryGetIdentifierKey(DslExpression expression, out string key)
    {
        var stripped = StripParentheses(expression);
        if (stripped is DslIdentifierExpression identifier)
        {
            key = identifier.Member is null
                ? identifier.Name
                : $"{identifier.Name}.{identifier.Member}";
            return true;
        }

        key = string.Empty;
        return false;
    }

    private static DslExpression StripParentheses(DslExpression expression)
    {
        while (expression is DslParenthesizedExpression parenthesized)
            expression = parenthesized.Inner;

        return expression;
    }

    private static bool IsNullLiteral(DslExpression expression)
        => StripParentheses(expression) is DslLiteralExpression { Value: null };

    private static bool IsExactly(StaticValueKind kind, StaticValueKind expected)
        => kind == expected;

    private static bool IsAssignable(StaticValueKind actual, StaticValueKind expected)
    {
        var actualNonNull = actual & ~StaticValueKind.Null;
        var expectedNonNull = expected & ~StaticValueKind.Null;

        if (!HasFlag(expected, StaticValueKind.Null) && HasFlag(actual, StaticValueKind.Null))
            return false;

        if ((actualNonNull & ~expectedNonNull) != StaticValueKind.None)
            return false;

        if (actual == StaticValueKind.Null)
            return HasFlag(expected, StaticValueKind.Null);

        return true;
    }

    private static int FindGuardLine(string[] lines, ref int searchLine, string guardExpression, int fallbackLine = 0)
    {
        for (var i = Math.Max(0, searchLine); i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("if ", StringComparison.Ordinal))
            {
                var candidate = trimmed[3..].Trim();
                if (Normalized(candidate) == Normalized(guardExpression))
                {
                    searchLine = i + 1;
                    return i;
                }
            }

            if (trimmed.StartsWith("else if ", StringComparison.Ordinal))
            {
                var candidate = trimmed[8..].Trim();
                if (Normalized(candidate) == Normalized(guardExpression))
                {
                    searchLine = i + 1;
                    return i;
                }
            }
        }

        return fallbackLine;
    }

    private static int FindMutationLine(string[] lines, ref int searchLine, string verb, string targetField, int fallbackLine = 0)
    {
        // Mutation lines have the form: <verb> <targetField> [<rest>]
        // e.g. "add Tags Value", "dequeue Names into LastRemoved", "clear Floors"
        var prefix = $"{verb} {targetField}";
        for (var i = Math.Max(0, searchLine); i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                (trimmed.Length == prefix.Length || char.IsWhiteSpace(trimmed[prefix.Length])))
            {
                searchLine = i + 1;
                return i;
            }
        }

        return fallbackLine;
    }

    private static int FindSetLine(string[] lines, ref int searchLine, string key, string expression, int fallbackLine = 0)
    {
        for (var i = Math.Max(0, searchLine); i < lines.Length; i++)
        {
            var match = SetLineRegex.Match(lines[i]);
            if (!match.Success)
                continue;

            var candidateKey = match.Groups["key"].Value;
            var candidateExpr = match.Groups["expr"].Value.Trim();
            if (candidateKey == key && Normalized(candidateExpr) == Normalized(expression))
            {
                searchLine = i + 1;
                return i;
            }
        }

        return fallbackLine;
    }

    private static string Normalized(string text)
        => Regex.Replace(text.Trim(), "\\s+", " ");

    private static Diagnostic CreateLineDiagnostic(string[] lines, int lineIndex, string message)
    {
        var safeLineIndex = Math.Min(Math.Max(lineIndex, 0), Math.Max(lines.Length - 1, 0));
        var lineLength = safeLineIndex < lines.Length ? lines[safeLineIndex].Length : 1;
        return new Diagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = message,
            Source = "state-machine-dsl",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(safeLineIndex, 0),
                new Position(safeLineIndex, Math.Max(1, lineLength)))
        };
    }

    private static StaticValueKind MapFieldContractKind(DslField field) => MapKind(field.Type, field.IsNullable);

    private static StaticValueKind MapFieldContractKind(DslEventArg arg) => MapKind(arg.Type, arg.IsNullable);

    private static StaticValueKind MapKind(DslScalarType type, bool isNullable)
    {
        var kind = type switch
        {
            DslScalarType.String => StaticValueKind.String,
            DslScalarType.Number => StaticValueKind.Number,
            DslScalarType.Boolean => StaticValueKind.Boolean,
            DslScalarType.Null => StaticValueKind.Null,
            _ => StaticValueKind.None
        };

        if (isNullable)
            kind |= StaticValueKind.Null;

        return kind;
    }

    private static StaticValueKind MapLiteralKind(object? value)
        => value switch
        {
            null => StaticValueKind.Null,
            string => StaticValueKind.String,
            bool => StaticValueKind.Boolean,
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => StaticValueKind.Number,
            _ => StaticValueKind.None
        };

    private static bool HasFlag(StaticValueKind kind, StaticValueKind flag)
        => (kind & flag) == flag;

    private static string FormatKinds(StaticValueKind kinds)
    {
        if (kinds == StaticValueKind.None)
            return "none";

        var labels = new List<string>();
        if (HasFlag(kinds, StaticValueKind.String)) labels.Add("string");
        if (HasFlag(kinds, StaticValueKind.Number)) labels.Add("number");
        if (HasFlag(kinds, StaticValueKind.Boolean)) labels.Add("boolean");
        if (HasFlag(kinds, StaticValueKind.Null)) labels.Add("null");
        return string.Join("|", labels);
    }

    private static IReadOnlyList<string> CollectIdentifiers(string text, Regex regex)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var match = regex.Match(line);
            if (match.Success)
                names.Add(match.Groups["name"].Value);
        }

        return names.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static Diagnostic ToDiagnostic(string message, string[] lines)
    {
        var match = LineErrorRegex.Match(message);
        if (match.Success && int.TryParse(match.Groups["line"].Value, out var lineNumber))
        {
            var lineIndex = Math.Max(0, lineNumber - 1);
            var lineLength = lineIndex < lines.Length ? lines[lineIndex].Length : 1;
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = match.Groups["message"].Value,
                Source = "state-machine-dsl",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(lineIndex, 0),
                    new Position(lineIndex, Math.Max(1, lineLength)))
            };
        }

        return new Diagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = message,
            Source = "state-machine-dsl",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 0),
                new Position(0, 1))
        };
    }

    private static IReadOnlyList<CompletionItem> BuildItems(IEnumerable<string> labels, CompletionItemKind kind)
        => labels
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .Select(label => new CompletionItem
            {
                Label = label,
                Kind = kind
            })
            .ToArray();

    private static readonly IReadOnlyList<CompletionItem> KeywordItems =
    [
        new CompletionItem { Label = "precept", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "state", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "initial", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "event", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "from", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "on", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "if", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "else if", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "else", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "transition", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "set", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "reject", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "no transition", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "string", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "number", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "boolean", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "null", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "set<number>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "set<string>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "set<boolean>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "queue<number>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "queue<string>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "queue<boolean>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "stack<number>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "stack<string>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "stack<boolean>", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "add", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "remove", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "enqueue", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "dequeue", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "push", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "pop", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "clear", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "contains", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "into", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "above", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "below", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "reason", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "rule", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "when", Kind = CompletionItemKind.Keyword }
    ];

    private static readonly IReadOnlyList<CompletionItem> ExpressionOperatorItems =
    [
        new CompletionItem { Label = "+", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "-", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "*", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "/", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "%", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "==", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "!=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = ">", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = ">=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "<", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "<=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "&&", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "||", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "!", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "contains", Kind = CompletionItemKind.Operator }
    ];

    private static readonly IReadOnlyList<CompletionItem> LiteralItems =
    [
        new CompletionItem { Label = "true", Kind = CompletionItemKind.Constant },
        new CompletionItem { Label = "false", Kind = CompletionItemKind.Constant },
        new CompletionItem { Label = "null", Kind = CompletionItemKind.Constant }
    ];

    private static readonly IReadOnlyList<CompletionItem> GlobalSnippetItems =
    [
        SnippetItem("from/on block", "from ${1:any} on ${2:Event}\n  ${3:transition ${4:State}}", "DSL block"),
        SnippetItem("if transition", "if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("else if transition", "else if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("else reject", "else\n  reject \"${1:Reason}\"", "Fallback branch")
    ];

    private static readonly IReadOnlyList<CompletionItem> GuardSnippetItems =
    [
        SnippetItem("if ... transition", "if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("if ... no transition", "if ${1:condition}\n  no transition", "Guard branch"),
        SnippetItem("else if ... transition", "else if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("else ... reject", "else\n  reject \"${1:Reason}\"", "Fallback branch")
    ];

    private static readonly IReadOnlyList<CompletionItem> SetSnippetItems =
    [
        SnippetItem("set assignment", "set ${1:Field} = ${2:value}", "Data assignment")
    ];

    private static readonly IReadOnlyList<CompletionItem> TransitionSnippetItems =
    [
        SnippetItem("transition state", "transition ${1:State}", "Transition"),
        SnippetItem("no transition", "no transition", "Terminal outcome"),
        SnippetItem("reject reason", "reject \"${1:Reason}\"", "Terminal outcome")
    ];

    private static CompletionItem SnippetItem(string label, string snippet, string detail)
        => new()
        {
            Label = label,
            Kind = CompletionItemKind.Snippet,
            InsertText = snippet,
            InsertTextFormat = InsertTextFormat.Snippet,
            Detail = detail
        };

    [Flags]
    private enum StaticValueKind
    {
        None = 0,
        String = 1,
        Number = 2,
        Boolean = 4,
        Null = 8
    }
}
