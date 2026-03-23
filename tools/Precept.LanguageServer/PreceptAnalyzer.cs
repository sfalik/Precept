using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;

namespace Precept.LanguageServer;

internal sealed class PreceptAnalyzer
{
    internal const string RejectOnlyPairDiagnosticCode = "PA1001";
    internal const string RejectOnlyPairSupportedElsewhereDiagnosticCode = "PA1002";
    internal const string EventNeverSucceedsDiagnosticCode = "PA1003";

    private static readonly Regex LineErrorRegex = new("^Line\\s+(?<line>\\d+)\\s*:\\s*(?<message>.+)$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new("^\\s*state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("^\\s*event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex DataFieldRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*.+)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventArgRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*.+)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FromOnRegex = new("^\\s*from\\s+(?<from>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventMemberPrefixRegex = new("(?<event>[A-Za-z_][A-Za-z0-9_]*)\\.$", RegexOptions.Compiled);
    private static readonly Regex CollectionDeclRegex = new("^\\s*(?:set|queue|stack)<(?:number|string|boolean)>\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SetAssignmentExpressionRegex = new("(?:^|->)\\s*set\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*[^\\n]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CollectionMutationExpressionRegex = new("(?:^|->)\\s*(?:add|remove|push|enqueue)\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s+[^\\n]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── New-syntax regex patterns ──────────────────────────────────────
    // Match `field Name as string|number|boolean` (scalar fields in new syntax)
    private static readonly Regex NewFieldDeclRegex = new("^\\s*field\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+(?:string|number|boolean)(?:\\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Match `field Name as set|queue|stack of type` (collection fields in new syntax)
    private static readonly Regex NewCollectionFieldRegex = new("^\\s*field\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+(?:set|queue|stack)\\s+of\\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Match `event Name with ...` (inline event args in new syntax)
    private static readonly Regex NewEventWithArgsRegex = new("^\\s*event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+with\\s+(?<args>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


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

        // Use ParseWithDiagnostics for structured error reporting with position info
        var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);

        if (parseDiags.Count > 0)
        {
            return parseDiags.Select(d =>
            {
                var lineIndex = Math.Max(0, d.Line - 1);
                var lineLength = lineIndex < lines.Length ? lines[lineIndex].Length : 1;
                return new Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = d.Message,
                    Source = "precept",
                    Code = d.Code is not null ? new DiagnosticCode(d.Code) : default,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        new Position(lineIndex, d.Column),
                        new Position(lineIndex, Math.Max(d.Column + 1, lineLength)))
                };
            }).ToArray();
        }

        if (model is null)
            return Array.Empty<Diagnostic>();

        try
        {
            var typeDiagnostics = PreceptTypeChecker.Check(model);
            if (typeDiagnostics.HasErrors)
                return typeDiagnostics.Diagnostics.Select(MapTypeDiagnostic).ToArray();

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

        var info = PreceptDocumentIntellisense.Analyze(text);
        var lines = info.Lines;
        if (position.Line >= lines.Length)
            return KeywordItems;

        var lineText = lines[(int)position.Line];
        var beforeCursor = position.Character <= lineText.Length
            ? lineText[..(int)position.Character]
            : lineText;

        var states = info.States;
        var events = info.Events;
        var dataFields = info.Fields;
        var collectionFields = info.CollectionFields;
        var eventArgs = info.EventArgs;
        var collectionKinds = info.CollectionKinds;
        var currentEvent = FindCurrentEventName(lines, (int)position.Line);

        // Collection member prefix: e.g. "Floors." → suggest .count, .min, .max, .peek
        var collectionMemberPrefixMatch = Regex.Match(beforeCursor, "(?<col>[A-Za-z_][A-Za-z0-9_]*)\\.$");
        if (collectionMemberPrefixMatch.Success)
        {
            var colName = collectionMemberPrefixMatch.Groups["col"].Value;
            if (collectionFields.Contains(colName, StringComparer.Ordinal))
                return BuildCollectionMemberItems(colName, collectionKinds.TryGetValue(colName, out var kind) ? kind : null);
        }

        var eventMemberPrefixMatch = EventMemberPrefixRegex.Match(beforeCursor);
        if (eventMemberPrefixMatch.Success)
        {
            var eventName = eventMemberPrefixMatch.Groups["event"].Value;
            if (eventArgs.TryGetValue(eventName, out var argsForEvent))
                return BuildItems(argsForEvent, CompletionItemKind.Field);
        }

        // reject: offer a string snippet
        if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*reject(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return [SnippetItem("reject reason", "reject \"${1:Reason}\"", "Rejection reason")];

        // Mutation verb lines: suggest collection field names after add/remove/enqueue/dequeue/push/pop/clear
        if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*(?:add|remove|enqueue|dequeue|push|pop|clear)(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
        {
            // After "dequeue <Field> into" or "pop <Field> into", suggest scalar data fields filtered by collection inner type
            var intoMatch = Regex.Match(beforeCursor, "(?:^|->)\\s*(?:dequeue|pop)\\s+(?<col>[A-Za-z_][A-Za-z0-9_]*)\\s+into(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase);
            if (intoMatch.Success)
                return TryBuildTypedDequeuePopIntoCompletions(intoMatch.Groups["col"].Value, dataFields, info.CollectionInnerTypes, info.FieldTypeKinds)
                    ?? BuildItems(dataFields, CompletionItemKind.Field);

            // After "dequeue <Field>" or "pop <Field>", suggest "into" keyword and collection fields
            if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*(?:dequeue|pop)\\s+[A-Za-z_][A-Za-z0-9_]*(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            {
                var items = new List<CompletionItem>(BuildItems(collectionFields, CompletionItemKind.Field));
                items.Add(new CompletionItem { Label = "into", Kind = CompletionItemKind.Keyword });
                return items.ToArray();
            }

            // After "add|remove|push|enqueue <Field> ", suggest expression (the value to add/push/remove)
            if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*(?:add|remove|push|enqueue)\\s+[A-Za-z_][A-Za-z0-9_]*\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            {
                var typedItems = TryBuildTypedCollectionMutationExpressionCompletions(
                    text,
                    lines,
                    (int)position.Line,
                    beforeCursor,
                    currentEvent,
                    eventArgs,
                    collectionKinds);
                return typedItems ?? BuildExpressionCompletions(dataFields, currentEvent, eventArgs, collectionKinds);
            }

            return BuildItems(collectionFields, CompletionItemKind.Field);
        }

        if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*set\\s+[^=\\n]*$", RegexOptions.IgnoreCase) &&
            !beforeCursor.Contains('=', StringComparison.Ordinal))
            return DistinctAndSort(BuildItems(dataFields, CompletionItemKind.Field).Concat(SetSnippetItems));

        if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*set\\s+[A-Za-z_][A-Za-z0-9_]*\\s*=\\s*[^\\n]*$", RegexOptions.IgnoreCase))
        {
            var typedItems = TryBuildTypedSetExpressionCompletions(
                text,
                lines,
                (int)position.Line,
                beforeCursor,
                currentEvent,
                eventArgs,
                collectionKinds);
            return typedItems ?? BuildExpressionCompletions(dataFields, currentEvent, eventArgs, collectionKinds);
        }

        // ── New-syntax: arrow pipeline ──
        // After "-> transition " or start-of-line "transition " → suggest state names
        if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*transition\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildItems(states, CompletionItemKind.EnumMember);

        // After "-> no " → suggest "transition"
        if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*no\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "transition", Kind = CompletionItemKind.Keyword }];

        // After "-> " → suggest action/outcome keywords
        // Must be checked before the broader "from … on …" regex which would swallow "->"
        if (beforeCursor.TrimEnd().EndsWith("->", StringComparison.Ordinal) ||
            Regex.IsMatch(beforeCursor, "->\\s+$"))
            return ArrowItems;

        // After a completed guarded transition expression, suggest the action/outcome pipeline.
        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+\\S+\\s+on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+when\\s+.+\\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [ArrowPipelineItem];

        // After "from <State> on <Event> when ", suggest expression completions
        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+\\S+\\s+on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+when\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildGuardCompletions(dataFields, collectionFields, collectionKinds, currentEvent, eventArgs);

        // After "from <State> on <Event> " (trailing space, nothing else), suggest "when" and "->"
        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+\\S+\\s+on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return
            [
                new CompletionItem { Label = "when", Kind = CompletionItemKind.Keyword },
                new CompletionItem { Label = "->", Kind = CompletionItemKind.Operator, Detail = "action/outcome pipeline" }
            ];

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*\\s+on(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase) &&
            !beforeCursor.Contains("->", StringComparison.Ordinal))
            return BuildItems(events, CompletionItemKind.Event);

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*$", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(beforeCursor, "\\s+on\\s+", RegexOptions.IgnoreCase))
        {
            var items = new List<CompletionItem>(BuildItems(states.Append("any"), CompletionItemKind.EnumMember));
            items.AddRange(KeywordItems.Where(item => item.Label == "on"));

            if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+(?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*(?:any|[A-Za-z_][A-Za-z0-9_]*)\\s+$", RegexOptions.IgnoreCase))
            {
                items.Add(new CompletionItem { Label = "assert", Kind = CompletionItemKind.Keyword });
                items.Add(new CompletionItem { Label = "->", Kind = CompletionItemKind.Operator, Detail = "state exit actions" });
            }

            return DistinctAndSort(items);
        }

        // ── New-syntax: field declarations ──
        // After "field Name as set|queue|stack of " → suggest scalar types
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+as\s+(?:set|queue|stack)\s+of\s+\w*$", RegexOptions.IgnoreCase))
            return ScalarTypeItems;

        // After "field Name as set|queue|stack " → suggest "of"
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+as\s+(?:set|queue|stack)\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "of", Kind = CompletionItemKind.Keyword }];

        // After "field Name as Type nullable " → suggest "default"
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+as\s+(?:string|number|boolean)\s+nullable\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "default", Kind = CompletionItemKind.Keyword, Detail = "Default value" }];

        // After "field Name as Type " (scalar type completed) → suggest "nullable", "default"
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+as\s+(?:string|number|boolean)\s+$", RegexOptions.IgnoreCase))
            return
            [
                new CompletionItem { Label = "nullable", Kind = CompletionItemKind.Keyword, Detail = "Makes field nullable" },
                new CompletionItem { Label = "default", Kind = CompletionItemKind.Keyword, Detail = "Default value" }
            ];

        // After "field Name as " (typing type) → suggest type keywords
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+as\s+\w*$", RegexOptions.IgnoreCase))
            return TypeItems;

        // After "field Name " → suggest "as"
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "as", Kind = CompletionItemKind.Keyword }];

        // ── New-syntax: invariant/assert expressions ──
        // After a completed invariant expression, suggest the required reason clause.
        if (Regex.IsMatch(beforeCursor, "^\\s*invariant\\s+.+\\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [BecauseItem];

        // After "invariant " → suggest expression completions (field names, operators)
        if (Regex.IsMatch(beforeCursor, "^\\s*invariant\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After a completed event assert expression, suggest the required reason clause.
        if (Regex.IsMatch(beforeCursor, "^\\s*on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+assert\\s+.+\\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [BecauseItem];

        // After "on EventName assert " → suggest expression completions (event args)
        if (Regex.IsMatch(beforeCursor, "^\\s*on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+assert\\s+[^\\n]*$", RegexOptions.IgnoreCase))
        {
            var eventName = Regex.Match(beforeCursor, "^\\s*on\\s+(?<evt>[A-Za-z_][A-Za-z0-9_]*)").Groups["evt"].Value;
            return BuildEventAssertCompletions(eventName, eventArgs);
        }

        // After "on EventName " → suggest "assert"
        if (Regex.IsMatch(beforeCursor, "^\\s*on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "assert", Kind = CompletionItemKind.Keyword }];

        // After "in/to StateName " → suggest assert, ->, edit (in only)
        if (Regex.IsMatch(beforeCursor, "^\\s*in\\s+[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*\\s+$", RegexOptions.IgnoreCase))
            return
            [
                new CompletionItem { Label = "assert", Kind = CompletionItemKind.Keyword },
                new CompletionItem { Label = "edit", Kind = CompletionItemKind.Keyword },
                new CompletionItem { Label = "->", Kind = CompletionItemKind.Operator, Detail = "action chain" }
            ];

        if (Regex.IsMatch(beforeCursor, "^\\s*to\\s+[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*\\s+$", RegexOptions.IgnoreCase))
            return
            [
                new CompletionItem { Label = "assert", Kind = CompletionItemKind.Keyword },
                new CompletionItem { Label = "->", Kind = CompletionItemKind.Operator, Detail = "action chain" }
            ];

        // After "in/to " (typing state name) → suggest state names
        // Only matches while still in the state-target portion (identifiers/commas), not after a keyword like edit/assert
        if (Regex.IsMatch(beforeCursor, "^\\s*(?:in|to)\\s+(?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*(?:[A-Za-z_][A-Za-z0-9_]*)?$", RegexOptions.IgnoreCase))
            return BuildItems(states.Append("any"), CompletionItemKind.EnumMember);

        // After a completed state assert expression, suggest the required reason clause.
        if (Regex.IsMatch(beforeCursor, "^\\s*(?:in|to|from)\\s+[^\\n]*\\s+assert\\s+.+\\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [BecauseItem];

        // After "in/to/from State assert " → expression completions
        if (Regex.IsMatch(beforeCursor, "^\\s*(?:in|to|from)\\s+[^\\n]*\\s+assert\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After "because " → suggest string template
        if (Regex.IsMatch(beforeCursor, "\\bbecause\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return [SnippetItem("because reason", "because \"${1:Reason}\"", "Constraint reason")];

        // After "in State edit " → suggest field names
        if (Regex.IsMatch(beforeCursor, "^\\s*in\\s+[^\\n]*\\s+edit\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return DistinctAndSort(
                BuildItems(dataFields, CompletionItemKind.Field)
                    .Concat(BuildItems(collectionFields, CompletionItemKind.Field)));

        // After "event Name with Arg as Type [nullable] [default Value] " → suggest delimiter / modifiers
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+(?:(?:[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)(?:\\s+nullable)?(?:\\s+default\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null))?)\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)\\s+nullable\\s+$", RegexOptions.IgnoreCase))
            return [DefaultItem, CommaItem];

        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+(?:(?:[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)(?:\\s+nullable)?(?:\\s+default\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null))?)\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)\\s+default\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null)\\s*$", RegexOptions.IgnoreCase))
            return [CommaItem];

        // After a comma in an event arg list, the user is starting the next arg name.
        // Avoid unrelated global keyword fallback in this position.
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+.*\\,\\s*$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();

        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+(?:(?:[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)(?:\\s+nullable)?(?:\\s+default\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null))?)\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)\\s+$", RegexOptions.IgnoreCase))
            return [NullableItem, DefaultItem, CommaItem];

        // After "event Name with ArgName as " → suggest type keywords
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+(?:(?:[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)(?:\\s+nullable)?(?:\\s+default\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null))?)\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+\\w*$", RegexOptions.IgnoreCase))
            return ScalarTypeItems;

        // After "event Name with ArgName " → suggest "as"
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+(?:(?:[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean)(?:\\s+nullable)?(?:\\s+default\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null))?)\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return [AsItem];

        // After "event Name with " → user is typing arg name, no suggestions
        // After "event Name " → suggest "with"
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return [WithItem];

        // After "of " → suggest scalar types (for collection type declarations)
        if (Regex.IsMatch(beforeCursor, "\\bof\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return ScalarTypeItems;

        // After "state <Name> ", suggest the "initial" keyword
        if (Regex.IsMatch(beforeCursor, "^\\s*state\\s+[A-Za-z_][A-Za-z0-9_]*\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return [InitialItem];

        return DistinctAndSort(KeywordItems.Concat(GlobalSnippetItems));
    }

    internal IReadOnlyList<RejectOnlyStateEventIssue> GetRejectOnlyStateEventIssues(DocumentUri uri)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return Array.Empty<RejectOnlyStateEventIssue>();

        var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
        if (parseDiags.Count > 0 || model is null)
            return Array.Empty<RejectOnlyStateEventIssue>();

        try
        {
            PreceptCompiler.Compile(model);
        }
        catch (Exception)
        {
            return Array.Empty<RejectOnlyStateEventIssue>();
        }

        return AnalyzeRejectOnlyStateEventIssues(model, text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
    }

    internal IReadOnlyList<OrphanedEventIssue> GetOrphanedEventIssues(DocumentUri uri)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return Array.Empty<OrphanedEventIssue>();

        var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
        if (parseDiags.Count > 0 || model is null)
            return Array.Empty<OrphanedEventIssue>();

        try
        {
            PreceptCompiler.Compile(model);
        }
        catch (Exception)
        {
            return Array.Empty<OrphanedEventIssue>();
        }

        return AnalyzeOrphanedEventIssues(model, text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
    }

    private static IReadOnlyList<CompletionItem> BuildGuardCompletions(
        IReadOnlyList<string> dataFields,
        IReadOnlyList<string> collectionFields,
        IReadOnlyDictionary<string, PreceptCollectionKind> collectionKinds,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(BuildItems(collectionFields, CompletionItemKind.Variable));
        items.AddRange(BuildCollectionScopeItems(collectionKinds));
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
        IReadOnlyDictionary<string, PreceptCollectionKind>? collectionKinds = null)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);

        // Collection field members are valid in set-RHS expressions (e.g. MySet.count)
        if (collectionKinds is not null)
            items.AddRange(BuildCollectionScopeItems(collectionKinds));

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

    private static IReadOnlyList<CompletionItem>? TryBuildTypedSetExpressionCompletions(
        string text,
        string[] lines,
        int lineIndex,
        string beforeCursor,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs,
        IReadOnlyDictionary<string, PreceptCollectionKind> collectionKinds)
    {
        if (!TryGetLastSetAssignmentTarget(beforeCursor, out var targetField))
            return null;

        if (lineIndex < 0 || lineIndex >= lines.Length)
            return null;

        var rowMatch = FromOnRegex.Match(lines[lineIndex]);
        if (!rowMatch.Success)
            return null;

        var fromState = rowMatch.Groups["from"].Value;
        if (string.Equals(fromState, "any", StringComparison.OrdinalIgnoreCase) || fromState.Contains(',', StringComparison.Ordinal))
            return null;

        var eventName = rowMatch.Groups["event"].Value;
        var sanitizedText = BuildCompletionParseText(text, lines, lineIndex, beforeCursor, "0");
        var (model, parseDiagnostics) = PreceptParser.ParseWithDiagnostics(sanitizedText);
        if (model is null || parseDiagnostics.Count > 0)
            return null;

        var target = model.Fields.FirstOrDefault(field => string.Equals(field.Name, targetField, StringComparison.Ordinal));
        if (target is null)
            return null;

        var typeCheck = PreceptTypeChecker.Check(model);
        var scope = typeCheck.TypeContext.Scopes.LastOrDefault(scope =>
                        scope.Line == lineIndex + 1 &&
                        scope.ScopeKind == "transition-actions" &&
                        string.Equals(scope.StateContext, fromState, StringComparison.Ordinal) &&
                        string.Equals(scope.EventName, eventName, StringComparison.Ordinal))
                    ?? typeCheck.TypeContext.FindBestScope(lineIndex + 1, fromState, eventName);
        if (scope is null)
            return null;

        var expectedKind = PreceptTypeChecker.MapFieldContractKind(target);
        var baseline = BuildExpressionCompletions(
            model.Fields.Select(static field => field.Name).ToArray(),
            currentEvent,
            eventArgs,
            collectionKinds);
        var dataFieldNames = model.Fields.Select(static field => field.Name).ToHashSet(StringComparer.Ordinal);

        return DistinctAndSort(baseline.Where(item =>
            item.Kind is CompletionItemKind.Operator or CompletionItemKind.Snippet ||
            IsTypedExpressionCompletionItem(item, scope.Symbols, expectedKind, dataFieldNames, eventName, collectionKinds)));
    }

    private static IReadOnlyList<CompletionItem>? TryBuildTypedCollectionMutationExpressionCompletions(
        string text,
        string[] lines,
        int lineIndex,
        string beforeCursor,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs,
        IReadOnlyDictionary<string, PreceptCollectionKind> collectionKinds)
    {
        if (!TryGetLastCollectionMutationTarget(beforeCursor, out var targetField))
            return null;

        if (lineIndex < 0 || lineIndex >= lines.Length)
            return null;

        var rowMatch = FromOnRegex.Match(lines[lineIndex]);
        if (!rowMatch.Success)
            return null;

        var fromState = rowMatch.Groups["from"].Value;
        if (string.Equals(fromState, "any", StringComparison.OrdinalIgnoreCase) || fromState.Contains(',', StringComparison.Ordinal))
            return null;

        var eventName = rowMatch.Groups["event"].Value;
        var sanitizedText = BuildCompletionParseText(text, lines, lineIndex, beforeCursor, "0");
        var (model, parseDiagnostics) = PreceptParser.ParseWithDiagnostics(sanitizedText);
        if (model is null || parseDiagnostics.Count > 0)
            return null;

        var target = model.CollectionFields.FirstOrDefault(field => string.Equals(field.Name, targetField, StringComparison.Ordinal));
        if (target is null)
            return null;

        var typeCheck = PreceptTypeChecker.Check(model);
        var scope = typeCheck.TypeContext.Scopes.LastOrDefault(scope =>
                        scope.Line == lineIndex + 1 &&
                        scope.ScopeKind == "transition-actions" &&
                        string.Equals(scope.StateContext, fromState, StringComparison.Ordinal) &&
                        string.Equals(scope.EventName, eventName, StringComparison.Ordinal))
                    ?? typeCheck.TypeContext.FindBestScope(lineIndex + 1, fromState, eventName);
        if (scope is null)
            return null;

        var expectedKind = PreceptTypeChecker.MapScalarType(target.InnerType);
        var baseline = BuildExpressionCompletions(
            model.Fields.Select(static field => field.Name).ToArray(),
            currentEvent,
            eventArgs,
            collectionKinds);
        var dataFieldNames = model.Fields.Select(static field => field.Name).ToHashSet(StringComparer.Ordinal);

        return DistinctAndSort(baseline.Where(item =>
            item.Kind is CompletionItemKind.Operator or CompletionItemKind.Snippet ||
            IsTypedExpressionCompletionItem(item, scope.Symbols, expectedKind, dataFieldNames, eventName, collectionKinds)));
    }

    private static IReadOnlyList<CompletionItem>? TryBuildTypedDequeuePopIntoCompletions(
        string collectionFieldName,
        IReadOnlyList<string> dataFields,
        IReadOnlyDictionary<string, PreceptScalarType> collectionInnerTypes,
        IReadOnlyDictionary<string, StaticValueKind> fieldTypeKinds)
    {
        if (!collectionInnerTypes.TryGetValue(collectionFieldName, out var innerType))
            return null;

        var innerKind = PreceptTypeChecker.MapScalarType(innerType);
        if (innerKind == StaticValueKind.None)
            return null;

        var compatible = dataFields
            .Where(name => fieldTypeKinds.TryGetValue(name, out var fieldKind)
                && PreceptTypeChecker.IsAssignableKind(innerKind, fieldKind))
            .ToArray();

        return BuildItems(compatible, CompletionItemKind.Field);
    }

    private static string BuildCompletionParseText(
        string originalText,
        string[] lines,
        int lineIndex,
        string beforeCursor,
        string placeholder)
    {
        if (lineIndex < 0 || lineIndex >= lines.Length)
            return originalText;

        var updatedLines = (string[])lines.Clone();
        var currentLine = updatedLines[lineIndex];
        if (beforeCursor.Length > currentLine.Length)
            return originalText;

        updatedLines[lineIndex] = beforeCursor + placeholder + currentLine[beforeCursor.Length..];
        var newline = originalText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return string.Join(newline, updatedLines);
    }

    private static bool IsTypedExpressionCompletionItem(
        CompletionItem item,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        StaticValueKind expectedKind,
        IReadOnlySet<string> dataFieldNames,
        string eventName,
        IReadOnlyDictionary<string, PreceptCollectionKind> collectionKinds)
    {
        if (item.Kind == CompletionItemKind.Constant)
            return PreceptTypeChecker.TryGetLiteralKind(item.Label, out var literalKind) && PreceptTypeChecker.IsAssignableKind(literalKind, expectedKind);

        if (item.Kind == CompletionItemKind.Module)
            return true;

        if (dataFieldNames.Contains(item.Label) && symbols.TryGetValue(item.Label, out var fieldKind))
            return PreceptTypeChecker.IsAssignableKind(fieldKind, expectedKind);

        if (item.Label.StartsWith(eventName + ".", StringComparison.Ordinal) && symbols.TryGetValue(item.Label, out var eventArgKind))
            return PreceptTypeChecker.IsAssignableKind(eventArgKind, expectedKind);

        if (TryGetCollectionName(item.Label, out var collectionName) && collectionKinds.ContainsKey(collectionName) && symbols.TryGetValue(item.Label, out var collectionMemberKind))
            return PreceptTypeChecker.IsAssignableKind(collectionMemberKind, expectedKind);

        return false;
    }

    private static bool TryGetLastSetAssignmentTarget(string beforeCursor, out string fieldName)
    {
        var matches = SetAssignmentExpressionRegex.Matches(beforeCursor);
        if (matches.Count > 0)
        {
            fieldName = matches[^1].Groups["field"].Value;
            return true;
        }

        fieldName = string.Empty;
        return false;
    }

    private static bool TryGetLastCollectionMutationTarget(string beforeCursor, out string fieldName)
    {
        var matches = CollectionMutationExpressionRegex.Matches(beforeCursor);
        if (matches.Count > 0)
        {
            fieldName = matches[^1].Groups["field"].Value;
            return true;
        }

        fieldName = string.Empty;
        return false;
    }

    private static bool TryGetCollectionName(string label, out string collectionName)
    {
        var dotIndex = label.IndexOf('.');
        if (dotIndex > 0)
        {
            collectionName = label[..dotIndex];
            return true;
        }

        collectionName = string.Empty;
        return false;
    }

    private static IReadOnlyList<CompletionItem> BuildDataExpressionCompletions(
        IReadOnlyList<string> dataFields,
        IReadOnlyDictionary<string, PreceptCollectionKind> collectionKinds)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(BuildItems(collectionKinds.Keys, CompletionItemKind.Variable));
        items.AddRange(BuildCollectionScopeItems(collectionKinds));
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);
        return DistinctAndSort(items);
    }

    private static IReadOnlyList<CompletionItem> BuildEventAssertCompletions(
        string eventName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs)
    {
        var items = new List<CompletionItem>();
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);

        if (eventArgs.TryGetValue(eventName, out var argsForEvent))
        {
            items.AddRange(BuildItems(argsForEvent, CompletionItemKind.Variable));
            items.Add(new CompletionItem { Label = eventName + ".", Kind = CompletionItemKind.Module });
            items.AddRange(BuildItems(argsForEvent.Select(arg => eventName + "." + arg), CompletionItemKind.Field));
        }

        return DistinctAndSort(items);
    }

    private static IReadOnlyList<CompletionItem> BuildCollectionScopeItems(
        IReadOnlyDictionary<string, PreceptCollectionKind> collectionKinds)
    {
        var items = new List<CompletionItem>();
        foreach (var pair in collectionKinds)
        {
            items.Add(new CompletionItem { Label = pair.Key + ".", Kind = CompletionItemKind.Module });
            items.AddRange(BuildCollectionMemberItems(pair.Key, pair.Value));
        }

        return items;
    }

    private static IReadOnlyList<CompletionItem> BuildCollectionMemberItems(string collectionName, PreceptCollectionKind? kind)
    {
        var items = new List<CompletionItem>
        {
            new() { Label = collectionName + ".count", Kind = CompletionItemKind.Property, Detail = "Number of elements" }
        };

        if (kind is null or PreceptCollectionKind.Set)
        {
            items.Add(new CompletionItem { Label = collectionName + ".min", Kind = CompletionItemKind.Property, Detail = "Minimum element" });
            items.Add(new CompletionItem { Label = collectionName + ".max", Kind = CompletionItemKind.Property, Detail = "Maximum element" });
        }

        if (kind is null or PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
            items.Add(new CompletionItem { Label = collectionName + ".peek", Kind = CompletionItemKind.Property, Detail = "Front/top element" });

        return items;
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

            // New syntax: field Name as string|number|boolean (scalar only, not collection)
            var newFieldMatch = NewFieldDeclRegex.Match(raw);
            if (newFieldMatch.Success)
            {
                fields.Add(newFieldMatch.Groups["name"].Value);
                continue;
            }

            // Old syntax: string? Name [= value]
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

            // New syntax: event Name with ArgName as type, ArgName2 as type
            var newEventWithMatch = NewEventWithArgsRegex.Match(raw);
            if (newEventWithMatch.Success)
            {
                FlushCurrentEvent();
                currentEvent = newEventWithMatch.Groups["name"].Value;
                currentArgs = new List<string>();
                inEvent = true;

                // Parse inline args: "Count as number, Reason as string"
                var argsText = newEventWithMatch.Groups["args"].Value;
                foreach (Match argMatch in Regex.Matches(argsText, "\\b(?<argName>[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+", RegexOptions.IgnoreCase))
                    currentArgs.Add(argMatch.Groups["argName"].Value);

                FlushCurrentEvent();
                inEvent = false;
                continue;
            }

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

            var argMatch2 = EventArgRegex.Match(raw);
            if (argMatch2.Success)
                currentArgs.Add(argMatch2.Groups["name"].Value);
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

    /// <summary>
    /// Collects collection field names from both old syntax (set&lt;T&gt; Name)
    /// and new syntax (field Name as set of T).
    /// </summary>
    private static IReadOnlyList<string> CollectAllCollectionFields(string text, string[] lines)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        // Old syntax: set<T> Name, queue<T> Name, stack<T> Name
        foreach (var name in CollectIdentifiers(text, CollectionDeclRegex))
            names.Add(name);

        // New syntax: field Name as set|queue|stack of T
        foreach (var line in lines)
        {
            var match = NewCollectionFieldRegex.Match(line);
            if (match.Success)
                names.Add(match.Groups["name"].Value);
        }

        return names.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<CompletionItem> DistinctAndSort(IEnumerable<CompletionItem> items)
        => items
            .GroupBy(item => item.Label, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(item => item.Label, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<Diagnostic> GetSemanticDiagnostics(PreceptDefinition model, string[] lines)
    {
        var diagnostics = new List<Diagnostic>();

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
                Source = "precept",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(machineLine, 0),
                    new Position(machineLine, Math.Max(1, lineLength)))
            });
        }

        AddEntryAssertDiagnostics(model, lines, diagnostics);
        AddEventSurfaceDiagnostics(model, lines, diagnostics);
        AddAuditDiagnostics(model, lines, diagnostics);

        return diagnostics;
    }

    private static Diagnostic MapTypeDiagnostic(PreceptTypeDiagnostic diagnostic)
    {
        var lineIndex = Math.Max(0, diagnostic.Line - 1);
        var message = string.IsNullOrWhiteSpace(diagnostic.StateContext)
            ? diagnostic.Message
            : $"{diagnostic.Message} [state {diagnostic.StateContext}]";

        var severity = diagnostic.Constraint.Severity == ConstraintSeverity.Warning
            ? DiagnosticSeverity.Warning
            : DiagnosticSeverity.Error;

        return new Diagnostic
        {
            Severity = severity,
            Message = message,
            Source = "precept",
            Code = new DiagnosticCode(diagnostic.DiagnosticCode),
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(lineIndex, Math.Max(0, diagnostic.Column)),
                new Position(lineIndex, Math.Max(1, diagnostic.Column + 1)))
        };
    }

    private static void AddEventSurfaceDiagnostics(PreceptDefinition model, string[] lines, List<Diagnostic> diagnostics)
    {
        var rejectOnlyIssues = AnalyzeRejectOnlyStateEventIssues(model, lines);
        foreach (var issue in rejectOnlyIssues)
        {
            var code = issue.EventSucceedsElsewhere
                ? RejectOnlyPairSupportedElsewhereDiagnosticCode
                : RejectOnlyPairDiagnosticCode;

            var message = issue.EventSucceedsElsewhere
                ? $"Event '{issue.EventName}' has successful behavior in other states, but in state '{issue.StateName}' it is defined only as rejection. If '{issue.EventName}' is unsupported in '{issue.StateName}', remove all 'from {issue.StateName} on {issue.EventName}' rows so the outcome is NotDefined."
                : $"All rows for event '{issue.EventName}' in state '{issue.StateName}' reject. This makes the event part of the state's contract surface while ensuring it can never succeed. Remove the pair if the event should be unavailable here.";

            diagnostics.Add(CreateDiagnostic(lines, issue.FirstLineIndex, DiagnosticSeverity.Warning, message, code));
        }

        var transitionRows = model.TransitionRows ?? Array.Empty<PreceptTransitionRow>();
        var rowsByEvent = transitionRows
            .GroupBy(row => row.EventName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var evt in model.Events)
        {
            if (!rowsByEvent.TryGetValue(evt.Name, out var rows) || rows.Length == 0)
                continue;

            if (rows.Any(static row => row.Outcome is not Rejection))
                continue;

            var lineIndex = FindEventLine(lines, evt.Name);
            diagnostics.Add(CreateDiagnostic(
                lines,
                lineIndex,
                DiagnosticSeverity.Warning,
                $"Event '{evt.Name}' is declared and referenced in transition rows, but it never succeeds in any state. Remove the event, remove the reject-only rows, or add a successful transition/no-transition path.",
                EventNeverSucceedsDiagnosticCode));
        }
    }

    private static IReadOnlyList<RejectOnlyStateEventIssue> AnalyzeRejectOnlyStateEventIssues(PreceptDefinition model, string[] lines)
    {
        var transitionRows = model.TransitionRows ?? Array.Empty<PreceptTransitionRow>();
        var eventHasSuccessfulPath = transitionRows
            .GroupBy(row => row.EventName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Any(static row => row.Outcome is not Rejection),
                StringComparer.Ordinal);

        return transitionRows
            .GroupBy(row => (row.FromState, row.EventName))
            .Select(group =>
            {
                var rows = group.OrderBy(static row => row.SourceLine).ToArray();
                if (rows.Length == 0 || rows.Any(static row => row.Outcome is not Rejection))
                    return null;

                var firstLineIndex = FindTransitionPairLine(lines, group.Key.FromState, group.Key.EventName);
                return new RejectOnlyStateEventIssue(
                    group.Key.FromState,
                    group.Key.EventName,
                    firstLineIndex,
                    eventHasSuccessfulPath.TryGetValue(group.Key.EventName, out var succeedsElsewhere) && succeedsElsewhere);
            })
            .Where(static issue => issue is not null)
            .Cast<RejectOnlyStateEventIssue>()
            .ToArray();
    }

    private static void AddAuditDiagnostics(PreceptDefinition model, string[] lines, List<Diagnostic> diagnostics)
    {
        var allStates = model.States.Select(static state => state.Name).ToList();
        var transitionRows = model.TransitionRows ?? Array.Empty<PreceptTransitionRow>();
        var graph = allStates.ToDictionary(static state => state, static _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        var referencedEvents = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in transitionRows)
        {
            referencedEvents.Add(row.EventName);

            if (row.Outcome is not StateTransition transition)
                continue;

            if (string.Equals(row.FromState, "any", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var state in allStates)
                    graph[state].Add(transition.TargetState);
            }
            else if (graph.TryGetValue(row.FromState, out var neighbors))
            {
                neighbors.Add(transition.TargetState);
            }
        }

        var reachable = new HashSet<string>(StringComparer.Ordinal) { model.InitialState.Name };
        var queue = new Queue<string>();
        queue.Enqueue(model.InitialState.Name);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in graph[current])
            {
                if (reachable.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        foreach (var state in allStates.Where(state => !reachable.Contains(state)))
        {
            var lineIndex = FindStateLine(lines, state);
            diagnostics.Add(CreateDiagnostic(lines, lineIndex, DiagnosticSeverity.Warning, $"State '{state}' is unreachable from the initial state."));
        }

        foreach (var state in allStates)
        {
            var outgoingRows = transitionRows.Where(row =>
                string.Equals(row.FromState, state, StringComparison.Ordinal) ||
                string.Equals(row.FromState, "any", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (outgoingRows.Length == 0 || outgoingRows.Any(row => row.Outcome is StateTransition))
                continue;

            var lineIndex = FindStateLine(lines, state);
            diagnostics.Add(CreateDiagnostic(lines, lineIndex, DiagnosticSeverity.Hint, $"State '{state}' is a dead end: all outgoing rows reject or stay in place."));
        }

        foreach (var eventName in model.Events.Select(static evt => evt.Name).Where(eventName => !referencedEvents.Contains(eventName)))
        {
            var lineIndex = FindEventLine(lines, eventName);
            diagnostics.Add(CreateDiagnostic(lines, lineIndex, DiagnosticSeverity.Warning, $"Event '{eventName}' is orphaned: it is declared but never referenced in transition rows."));
        }
    }

    private static IReadOnlyList<OrphanedEventIssue> AnalyzeOrphanedEventIssues(PreceptDefinition model, string[] lines)
    {
        var referencedEvents = new HashSet<string>(
            (model.TransitionRows ?? Array.Empty<PreceptTransitionRow>()).Select(static row => row.EventName),
            StringComparer.Ordinal);

        return model.Events
            .Where(evt => !referencedEvents.Contains(evt.Name))
            .Select(evt => new OrphanedEventIssue(evt.Name, FindEventLine(lines, evt.Name)))
            .ToArray();
    }

    private static void AddEntryAssertDiagnostics(
        PreceptDefinition model,
        string[] lines,
        List<Diagnostic> diagnostics)
    {
        if (model.StateAsserts is not null)
        {
            var statesWithToAsserts = model.StateAsserts
                .Where(sa => sa.Anchor == AssertAnchor.To)
                .Select(sa => sa.State)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (statesWithToAsserts.Count > 0)
            {
                var targetedStates = new HashSet<string>(
                    (model.TransitionRows ?? Array.Empty<PreceptTransitionRow>())
                        .Select(r => r.Outcome)
                        .OfType<StateTransition>()
                        .Select(st => st.TargetState),
                    StringComparer.Ordinal);
                foreach (var stateName in statesWithToAsserts)
                {
                    if (!targetedStates.Contains(stateName))
                    {
                        var stateLineIdx = FindStateLine(lines, stateName);
                        var lineLen = stateLineIdx < lines.Length ? lines[stateLineIdx].Length : 1;
                        diagnostics.Add(new Diagnostic
                        {
                            Severity = DiagnosticSeverity.Warning,
                            Message = $"State '{stateName}' has entry asserts but no transition targets it — entry asserts are never checked.",
                            Source = "precept",
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                new Position(stateLineIdx, 0),
                                new Position(stateLineIdx, Math.Max(1, lineLen)))
                        });
                    }
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

    private static int FindEventLine(string[] lines, string eventName)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith($"event {eventName}", StringComparison.Ordinal))
                return i;
        }

        return 0;
    }

    private static int FindTransitionPairLine(string[] lines, string stateName, string eventName)
    {
        var prefix = $"from {stateName} on {eventName}";
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
                return i;
        }

        return 0;
    }

    private static Diagnostic CreateDiagnostic(string[] lines, int lineIndex, DiagnosticSeverity severity, string message, string? code = null)
    {
        var safeLineIndex = Math.Min(Math.Max(lineIndex, 0), Math.Max(lines.Length - 1, 0));
        var lineLength = safeLineIndex < lines.Length ? lines[safeLineIndex].Length : 1;
        var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
            new Position(safeLineIndex, 0),
            new Position(safeLineIndex, Math.Max(1, lineLength)));

        return code is null
            ? new Diagnostic
            {
                Severity = severity,
                Message = message,
                Source = "precept",
                Range = range
            }
            : new Diagnostic
            {
                Code = code,
                Severity = severity,
                Message = message,
                Source = "precept",
                Range = range
            };
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
                Source = "precept",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(lineIndex, 0),
                    new Position(lineIndex, Math.Max(1, lineLength)))
            };
        }

        return new Diagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = message,
            Source = "precept",
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

    private static readonly IReadOnlyList<CompletionItem> KeywordItems = BuildKeywordItems();

    /// <summary>
    /// Builds the fallback keyword completion list from <see cref="PreceptTokenMeta"/> attributes.
    /// Includes all word-tokens (alphabetic symbols) plus the composite "no transition" outcome.
    /// </summary>
    private static IReadOnlyList<CompletionItem> BuildKeywordItems()
    {
        var items = new List<CompletionItem>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            var category = PreceptTokenMeta.GetCategory(token);
            var symbol = PreceptTokenMeta.GetSymbol(token);
            var description = PreceptTokenMeta.GetDescription(token);

            if (category is null || symbol is null) continue;
            if (!symbol.All(char.IsLetter)) continue;
            if (category is TokenCategory.Structure or TokenCategory.Value) continue;

            items.Add(new CompletionItem
            {
                Label = symbol,
                Kind = CompletionItemKind.Keyword,
                Detail = description
            });
        }

        // Composite outcome: "no transition" is two tokens but one semantic unit
        items.Add(new CompletionItem
        {
            Label = "no transition",
            Kind = CompletionItemKind.Keyword,
            Detail = "Stay in current state"
        });

        return items;
    }

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
        SnippetItem("from/on row", "from ${1:any} on ${2:Event} -> ${3:transition ${4:State}}", "Transition row"),
        SnippetItem("from/on with when", "from ${1:any} on ${2:Event} when ${3:guard} -> ${4:transition ${5:State}}", "Guarded transition row"),
        SnippetItem("from/on with set", "from ${1:any} on ${2:Event} -> set ${3:Field} = ${4:value} -> transition ${5:State}", "Row with data assignment"),
        SnippetItem("field declaration", "field ${1:Name} as ${2:string}", "Field declaration"),
        SnippetItem("invariant", "invariant ${1:Expr} because \"${2:Reason}\"", "Global invariant"),
        SnippetItem("event with args", "event ${1:Name} with ${2:Arg} as ${3:string}", "Event with arguments")
    ];

    private static readonly IReadOnlyList<CompletionItem> SetSnippetItems =
    [
        SnippetItem("set assignment", "set ${1:Field} = ${2:value}", "Data assignment")
    ];

    // ── New-syntax item lists ──────────────────────────────────────────

    /// <summary>Scalar type keywords for positions after "as" / "of" / event arg type.</summary>
    private static readonly IReadOnlyList<CompletionItem> ScalarTypeItems =
    [
        new CompletionItem { Label = "string", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "number", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "boolean", Kind = CompletionItemKind.TypeParameter }
    ];

    /// <summary>All type keywords for positions after "field Name as" (scalar + collection).</summary>
    private static readonly IReadOnlyList<CompletionItem> TypeItems =
    [
        new CompletionItem { Label = "string", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "number", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "boolean", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "set", Kind = CompletionItemKind.TypeParameter, Detail = "Sorted unique set" },
        new CompletionItem { Label = "queue", Kind = CompletionItemKind.TypeParameter, Detail = "FIFO ordered" },
        new CompletionItem { Label = "stack", Kind = CompletionItemKind.TypeParameter, Detail = "LIFO ordered" }
    ];

    /// <summary>Action/outcome keywords available after "->" in flat transition rows.</summary>
    private static readonly IReadOnlyList<CompletionItem> ArrowItems =
    [
        new CompletionItem { Label = "set", Kind = CompletionItemKind.Keyword, Detail = "Data assignment" },
        new CompletionItem { Label = "transition", Kind = CompletionItemKind.Keyword, Detail = "Transition to state" },
        new CompletionItem { Label = "no transition", Kind = CompletionItemKind.Keyword, Detail = "Stay in current state" },
        new CompletionItem { Label = "reject", Kind = CompletionItemKind.Keyword, Detail = "Reject event" },
        new CompletionItem { Label = "add", Kind = CompletionItemKind.Keyword, Detail = "Add to set" },
        new CompletionItem { Label = "remove", Kind = CompletionItemKind.Keyword, Detail = "Remove from set" },
        new CompletionItem { Label = "enqueue", Kind = CompletionItemKind.Keyword, Detail = "Enqueue to queue" },
        new CompletionItem { Label = "dequeue", Kind = CompletionItemKind.Keyword, Detail = "Dequeue from queue" },
        new CompletionItem { Label = "push", Kind = CompletionItemKind.Keyword, Detail = "Push onto stack" },
        new CompletionItem { Label = "pop", Kind = CompletionItemKind.Keyword, Detail = "Pop from stack" },
        new CompletionItem { Label = "clear", Kind = CompletionItemKind.Keyword, Detail = "Clear collection" }
    ];

    private static readonly CompletionItem AsItem = new() { Label = "as", Kind = CompletionItemKind.Keyword };
    private static readonly CompletionItem WithItem = new() { Label = "with", Kind = CompletionItemKind.Keyword };
    private static readonly CompletionItem InitialItem = new() { Label = "initial", Kind = CompletionItemKind.Keyword };
    private static readonly CompletionItem NullableItem = new() { Label = "nullable", Kind = CompletionItemKind.Keyword, Detail = "Makes field nullable" };
    private static readonly CompletionItem DefaultItem = new() { Label = "default", Kind = CompletionItemKind.Keyword, Detail = "Default value" };
    private static readonly CompletionItem BecauseItem = new() { Label = "because", Kind = CompletionItemKind.Keyword, Detail = "Constraint reason" };
    private static readonly CompletionItem ArrowPipelineItem = new() { Label = "->", Kind = CompletionItemKind.Operator, Detail = "action/outcome pipeline" };
    private static readonly CompletionItem CommaItem = new() { Label = ",", Kind = CompletionItemKind.Operator, Detail = "Next event argument" };

    private static bool EndsWithCompletedExpression(string text)
        => Regex.IsMatch(text, "(?:[A-Za-z0-9_\\)\"]|true|false|null)\\s+$", RegexOptions.IgnoreCase);

    private static CompletionItem SnippetItem(string label, string snippet, string detail)
        => new()
        {
            Label = label,
            Kind = CompletionItemKind.Snippet,
            InsertText = snippet,
            InsertTextFormat = InsertTextFormat.Snippet,
            Detail = detail
        };

    internal sealed record RejectOnlyStateEventIssue(
        string StateName,
        string EventName,
        int FirstLineIndex,
        bool EventSucceedsElsewhere);

    internal sealed record OrphanedEventIssue(
        string EventName,
        int LineIndex);
}
