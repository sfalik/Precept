using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;

namespace Precept.LanguageServer;

internal sealed class PreceptAnalyzer
{
    private static readonly Regex LineErrorRegex = new("^Line\\s+(?<line>\\d+)\\s*:\\s*(?<message>.+)$", RegexOptions.Compiled);
    private static readonly Regex FromOnRegex = new("^\\s*from\\s+(?<from>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventMemberPrefixRegex = new("(?<event>[A-Za-z_][A-Za-z0-9_]*)\\.$", RegexOptions.Compiled);
    private static readonly Regex SetAssignmentExpressionRegex = new("(?:^|->)\\s*set\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*[^\\n]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CollectionMutationExpressionRegex = new("(?:^|->)\\s*(?:add|remove|push|enqueue)\\s+(?<field>[A-Za-z_][A-Za-z0-9_]*)\\s+[^\\n]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── Regex patterns for new-syntax declarations ──────────────────────
    // Match `field Name[, Name, ...] as string|number|boolean` (scalar fields)
    private static readonly Regex NewFieldDeclRegex = new("^\\s*field\\s+(?<names>(?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+(?:string|number|boolean|integer|decimal|choice)(?:\\s|\\(|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Match `field Name[, Name, ...] as set|queue|stack of type` (collection fields)
    private static readonly Regex NewCollectionFieldRegex = new("^\\s*field\\s+(?<names>(?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+(?:set|queue|stack)\\s+of\\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Match `event Name[, Name, ...] with ...` (inline event args)
    private static readonly Regex NewEventWithArgsRegex = new("^\\s*event\\s+(?<names>(?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)\\s+with\\s+(?<args>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


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

        var validation = PreceptCompiler.Validate(model);
        var diagnostics = validation.Diagnostics.Select(d => MapValidationDiagnostic(d, lines)).ToList();
        diagnostics.AddRange(GetSemanticDiagnostics(model, lines));
        return diagnostics.Count == 0 ? Array.Empty<Diagnostic>() : diagnostics;
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

        // ── Comment lines: suppress completions entirely ──
        if (beforeCursor.TrimStart().StartsWith('#'))
            return Array.Empty<CompletionItem>();

        // ── Pre-precept scope: before any precept declaration, only offer 'precept' ──
        var hasPreceptDecl = info.Lines.Any(static l => Regex.IsMatch(l, @"^\s*precept\s+", RegexOptions.IgnoreCase));
        if (!hasPreceptDecl)
        {
            // If the user is on a line starting with 'precept', they're naming it — suppress
            if (Regex.IsMatch(beforeCursor, @"^\s*precept\s+\S*$", RegexOptions.IgnoreCase))
                return Array.Empty<CompletionItem>();

            return
            [
                new CompletionItem { Label = "precept", Kind = CompletionItemKind.Keyword, Detail = "Top-level precept declaration" }
            ];
        }

        var states = info.States;
        var events = info.Events;
        var dataFields = info.Fields;
        var collectionFields = info.CollectionFields;
        var eventArgs = info.EventArgs;
        var collectionKinds = info.CollectionKinds;
        var currentEvent = FindCurrentEventName(lines, (int)position.Line);

        // Computed fields are excluded from set/edit target positions (they are read-only).
        // When the model is available, use it; otherwise fall back to regex detection
        // so computed field filtering works even on incomplete documents during editing.
        var computedFieldNames = info.Model?.Fields
            .Where(static f => f.IsComputed)
            .Select(static f => f.Name)
            .ToHashSet(StringComparer.Ordinal) ?? DetectComputedFieldsByRegex(lines);
        var editableDataFields = computedFieldNames.Count > 0
            ? dataFields.Where(f => !computedFieldNames.Contains(f)).ToArray()
            : dataFields;

        // ── Completion suppression: "inventing a name" positions ──
        // When the user is typing a new identifier name, suppress the default keyword list.
        if (Regex.IsMatch(beforeCursor, @"^\s*precept\s+\S*$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*\s*,\s*$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();
        if (Regex.IsMatch(beforeCursor, @"^\s*state\s+$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();
        if (Regex.IsMatch(beforeCursor, @"^\s*state\s+.*,\s*$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();
        if (Regex.IsMatch(beforeCursor, @"^\s*event\s+$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();
        if (Regex.IsMatch(beforeCursor, @"^\s*event\s+[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*\s*,\s*$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();

        // Collection member prefix: e.g. "Floors." → suggest .count, .min, .max, .peek
        var collectionMemberPrefixMatch = Regex.Match(beforeCursor, "(?<col>[A-Za-z_][A-Za-z0-9_]*)\\.$");
        if (collectionMemberPrefixMatch.Success)
        {
            var colName = collectionMemberPrefixMatch.Groups["col"].Value;
            if (collectionFields.Contains(colName, StringComparer.Ordinal))
                return BuildCollectionMemberItems(colName, collectionKinds.TryGetValue(colName, out var kind) ? kind : null);
            // String member prefix: e.g. "Notes." → suggest .length
            if (info.FieldTypeKinds.TryGetValue(colName, out var fieldKind) && (fieldKind & StaticValueKind.String) != 0)
                return BuildStringMemberItems(colName, isNullable: (fieldKind & StaticValueKind.Null) != 0);
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
                // If expression looks complete (ends with identifier/literal + space), offer -> for pipeline continuation.
                // Only check after the value portion has started — the regex requires at least one non-space token
                // past the field name to distinguish "add Field |" (needs value) from "add Field value |" (needs ->).
                var valueMatch = Regex.Match(beforeCursor, "(?:^|->)\\s*(?:add|remove|push|enqueue)\\s+[A-Za-z_][A-Za-z0-9_]*\\s+(?<value>\\S.*)$", RegexOptions.IgnoreCase);
                if (valueMatch.Success && EndsWithCompletedExpression(valueMatch.Groups["value"].Value))
                    return [ArrowPipelineItem];

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
            return DistinctAndSort(BuildItems(editableDataFields, CompletionItemKind.Field).Concat(SetSnippetItems));

        if (Regex.IsMatch(beforeCursor, "(?:^|->)\\s*set\\s+[A-Za-z_][A-Za-z0-9_]*\\s*=\\s*[^\\n]*$", RegexOptions.IgnoreCase))
        {
            // If expression looks complete (ends with identifier/literal + space), offer -> for pipeline continuation
            if (EndsWithCompletedExpression(beforeCursor))
                return [ArrowPipelineItem];

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
        // Exclude field declaration lines — those use "->" for derived expression syntax.
        if ((beforeCursor.TrimEnd().EndsWith("->", StringComparison.Ordinal) ||
            Regex.IsMatch(beforeCursor, "->\\s+$")) &&
            !Regex.IsMatch(beforeCursor, @"^\s*field\s+", RegexOptions.IgnoreCase))
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
                items.Add(new CompletionItem { Label = "ensure", Kind = CompletionItemKind.Keyword });                items.Add(new CompletionItem { Label = "->", Kind = CompletionItemKind.Operator, Detail = "state exit actions" });
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

        // ── Field modifier zone: any-order modifiers after type ──
        // Computed field derivation: after "field Name as type -> " → suggest field names and collection accessors
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+as\s+(?:string|number|boolean|integer|decimal|choice\([^)]*\))\s*->\s+[^\n]*$", RegexOptions.IgnoreCase))
            return BuildDerivedExpressionCompletions(dataFields, collectionFields, collectionKinds);

        // Unified handler for all field modifier completions (nullable, default, constraints, ordered).
        // Detects the field type, scans existing modifiers, offers remaining items.
        var fieldModifiers = TryGetFieldModifierCompletions(beforeCursor);
        if (fieldModifiers is not null)
            return fieldModifiers;

        // After "field Name as " (typing type) → suggest type keywords
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+[A-Za-z_]\w*\s+as\s+\w*$", RegexOptions.IgnoreCase))
            return TypeItems;

        // After "field Name[, Name, ...] " → suggest "as" and ","
        if (Regex.IsMatch(beforeCursor, @"^\s*field\s+(?:[A-Za-z_]\w*\s*(?:,\s*)?)*[A-Za-z_]\w*\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "as", Kind = CompletionItemKind.Keyword }, new CompletionItem { Label = ",", Kind = CompletionItemKind.Operator, Detail = "add another field name" }];

        // ── New-syntax: rule/ensure expressions ──
        // After "rule <expr> when <guard> " (completed guard) → suggest because
        if (Regex.IsMatch(beforeCursor, @"^\s*rule\s+.+\s+when\s+.+\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [BecauseItem];

        // After "rule <expr> when " (guard in progress) → suggest field names for guard expression
        if (Regex.IsMatch(beforeCursor, @"^\s*rule\s+.+\s+when\s+[^\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After a completed rule expression, suggest when or because.
        if (Regex.IsMatch(beforeCursor, "^\\s*rule\\s+.+\\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [WhenItem, BecauseItem];

        // After "rule " → suggest expression completions (field names, operators)
        if (Regex.IsMatch(beforeCursor, "^\\s*rule\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After "on Event ensure <expr> when <guard> " (completed guard) → suggest because
        if (Regex.IsMatch(beforeCursor, @"^\s*on\s+[A-Za-z_][A-Za-z0-9_]*\s+ensure\s+.+\s+when\s+.+\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [BecauseItem];

        // After "on Event ensure <expr> when " (guard in progress) → suggest event arg completions
        if (Regex.IsMatch(beforeCursor, @"^\s*on\s+[A-Za-z_][A-Za-z0-9_]*\s+ensure\s+.+\s+when\s+[^\n]*$", RegexOptions.IgnoreCase))
        {
            var eventName = Regex.Match(beforeCursor, @"^\s*on\s+(?<evt>[A-Za-z_][A-Za-z0-9_]*)").Groups["evt"].Value;
            return BuildEventEnsureCompletions(eventName, eventArgs);
        }

        // After a completed event ensure expression, suggest when or because.
        if (Regex.IsMatch(beforeCursor, "^\\s*on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+ensure\\s+.+\\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [WhenItem, BecauseItem];

        // After "on EventName ensure " → suggest expression completions (event args)
        if (Regex.IsMatch(beforeCursor, "^\\s*on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+ensure\\s+[^\\n]*$", RegexOptions.IgnoreCase))
        {
            var eventName = Regex.Match(beforeCursor, "^\\s*on\\s+(?<evt>[A-Za-z_][A-Za-z0-9_]*)").Groups["evt"].Value;
            return BuildEventEnsureCompletions(eventName, eventArgs);
        }

        // After "on EventName " → suggest "ensure"
        if (Regex.IsMatch(beforeCursor, "^\\s*on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = "ensure", Kind = CompletionItemKind.Keyword }];

        // After "on " (typing or choosing event name) → suggest event names
        if (Regex.IsMatch(beforeCursor, @"^\s*on\s+\w*$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        // After "in State when <guard> edit " → suggest field names (excludes computed fields)
        if (Regex.IsMatch(beforeCursor, @"^\s*in\s+[^\n]*\s+when\s+[^\n]+\s+edit\s+[^\n]*$", RegexOptions.IgnoreCase))
            return DistinctAndSort(
                new CompletionItem[] { new() { Label = "all", Kind = CompletionItemKind.Keyword } }
                    .Concat(BuildItems(editableDataFields, CompletionItemKind.Field))
                    .Concat(BuildItems(collectionFields, CompletionItemKind.Field)));

        // After "in State when <guard> " (completed guard) → suggest edit
        if (Regex.IsMatch(beforeCursor, @"^\s*in\s+[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*\s+when\s+.+\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [new CompletionItem { Label = "edit", Kind = CompletionItemKind.Keyword }];

        // After "in State when " (guard in progress) → suggest field names for guard expression
        if (Regex.IsMatch(beforeCursor, @"^\s*in\s+[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*\s+when\s+[^\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After "in/to StateName " → suggest ensure, ->, edit (in only), when (in only)
        if (Regex.IsMatch(beforeCursor, "^\\s*in\\s+[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*\\s+$", RegexOptions.IgnoreCase))
            return
            [
                new CompletionItem { Label = "ensure", Kind = CompletionItemKind.Keyword },
                new CompletionItem { Label = "edit", Kind = CompletionItemKind.Keyword },
                WhenItem,
                new CompletionItem { Label = "->", Kind = CompletionItemKind.Operator, Detail = "action chain" }
            ];

        if (Regex.IsMatch(beforeCursor, "^\\s*to\\s+[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*\\s+$", RegexOptions.IgnoreCase))
            return
            [
                new CompletionItem { Label = "ensure", Kind = CompletionItemKind.Keyword },
                new CompletionItem { Label = "->", Kind = CompletionItemKind.Operator, Detail = "action chain" }
            ];

        // After "in/to " (typing state name) → suggest state names
        // Only matches while still in the state-target portion (identifiers/commas), not after a keyword like edit/ensure
        if (Regex.IsMatch(beforeCursor, "^\\s*(?:in|to)\\s+(?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*(?:[A-Za-z_][A-Za-z0-9_]*)?$", RegexOptions.IgnoreCase))
            return BuildItems(states.Append("any"), CompletionItemKind.EnumMember);

        // After "in/to/from State ensure <expr> when <guard> " (completed guard) → suggest because
        if (Regex.IsMatch(beforeCursor, @"^\s*(?:in|to|from)\s+[^\n]*\s+ensure\s+.+\s+when\s+.+\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [BecauseItem];

        // After "in/to/from State ensure <expr> when " (guard in progress) → suggest field completions
        if (Regex.IsMatch(beforeCursor, @"^\s*(?:in|to|from)\s+[^\n]*\s+ensure\s+.+\s+when\s+[^\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After a completed state ensure expression, suggest when or because.
        if (Regex.IsMatch(beforeCursor, "^\\s*(?:in|to|from)\\s+[^\\n]*\\s+ensure\\s+.+\\s+$", RegexOptions.IgnoreCase) && EndsWithCompletedExpression(beforeCursor))
            return [WhenItem, BecauseItem];

        // After "in/to/from State ensure " → expression completions
        if (Regex.IsMatch(beforeCursor, "^\\s*(?:in|to|from)\\s+[^\\n]*\\s+ensure\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After "because " → suggest string template
        if (Regex.IsMatch(beforeCursor, "\\bbecause\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return [SnippetItem("because reason", "because \"${1:Reason}\"", "Constraint reason")];

        // After root "edit <fields> when <guard>" (guard in progress) → suggest field completions
        if (Regex.IsMatch(beforeCursor, @"^\s*edit\s+[^\n]+\s+when\s+[^\n]*$", RegexOptions.IgnoreCase))
            return BuildDataExpressionCompletions(dataFields, collectionKinds);

        // After root "edit <fields> " (completed field list) → suggest 'when' + more field names
        if (Regex.IsMatch(beforeCursor, @"^\s*edit\s+(?:all|[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*)\s+$", RegexOptions.IgnoreCase))
            return DistinctAndSort(
                new CompletionItem[] { WhenItem }
                    .Concat(BuildItems(editableDataFields, CompletionItemKind.Field))
                    .Concat(BuildItems(collectionFields, CompletionItemKind.Field)));

        // After root "edit " → suggest 'all' + field names (excludes computed fields)
        if (Regex.IsMatch(beforeCursor, "^\\s*edit\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return DistinctAndSort(
                new CompletionItem[] { new() { Label = "all", Kind = CompletionItemKind.Keyword } }
                    .Concat(BuildItems(editableDataFields, CompletionItemKind.Field))
                    .Concat(BuildItems(collectionFields, CompletionItemKind.Field)));

        // After "in State edit " → suggest 'all' + field names (excludes computed fields)
        if (Regex.IsMatch(beforeCursor, "^\\s*in\\s+[^\\n]*\\s+edit\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return DistinctAndSort(
                new CompletionItem[] { new() { Label = "all", Kind = CompletionItemKind.Keyword } }
                    .Concat(BuildItems(editableDataFields, CompletionItemKind.Field))
                    .Concat(BuildItems(collectionFields, CompletionItemKind.Field)));

        // ── Event arg modifier zone: any-order modifiers after type ──
        // Unified handler for event argument modifier completions (nullable, default, constraints, comma).
        var eventArgModifiers = TryGetEventArgModifierCompletions(beforeCursor);
        if (eventArgModifiers is not null)
            return eventArgModifiers;

        // After a comma in an event arg list, the user is starting the next arg name.
        // Avoid unrelated global keyword fallback in this position.
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+.*\\,\\s*$", RegexOptions.IgnoreCase))
            return Array.Empty<CompletionItem>();

        // After "event Name with ArgName as " → suggest type keywords
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+(?:(?:[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean|integer|decimal|choice\\([^)]*\\))(?:\\s+(?:nullable|nonnegative|positive|notempty|ordered|(?:(?:default|min|max|minlength|maxlength|mincount|maxcount|maxplaces)\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null|\\[\"[^\"]*\"(?:,\\s*\"[^\"]*\")*\\]))))*)\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+\\w*$", RegexOptions.IgnoreCase))
            return ScalarTypeItems;

        // After "event Name with ArgName " → suggest "as"
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+with\\s+(?:(?:[A-Za-z_][A-Za-z0-9_]*\\s+as\\s+(?:string|number|boolean|integer|decimal|choice\\([^)]*\\))(?:\\s+(?:nullable|nonnegative|positive|notempty|ordered|(?:(?:default|min|max|minlength|maxlength|mincount|maxcount|maxplaces)\\s+(?:\"[^\"\\n]*\"|-?\\d+(?:\\.\\d+)?|true|false|null|\\[\"[^\"]*\"(?:,\\s*\"[^\"]*\")*\\]))))*)\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return [AsItem];

        // After "event Name with " → user is typing arg name, no suggestions
        // After "event Name " → suggest "with"
        if (Regex.IsMatch(beforeCursor, "^\\s*event\\s+[A-Za-z_][A-Za-z0-9_]*\\s+$", RegexOptions.IgnoreCase))
            return [WithItem];

        // After "of " → suggest scalar types (for collection type declarations)
        if (Regex.IsMatch(beforeCursor, "\\bof\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return ScalarTypeItems;

        // After "state <Name>[, ...] " (trailing space after a name), suggest "initial" and ","
        if (Regex.IsMatch(beforeCursor, @"^\s*state\s+(?:[A-Za-z_][A-Za-z0-9_]*\s*(?:initial\s*)?(?:,\s*)?)*[A-Za-z_][A-Za-z0-9_]*\s+$", RegexOptions.IgnoreCase))
            return [InitialItem, new CompletionItem { Label = ",", Kind = CompletionItemKind.Operator, Detail = "add another state name" }];

        // After "state ... initial " (trailing space after initial), suggest ","
        if (Regex.IsMatch(beforeCursor, @"^\s*state\s+.*\binitial\s+$", RegexOptions.IgnoreCase))
            return [new CompletionItem { Label = ",", Kind = CompletionItemKind.Operator, Detail = "add another state name" }];

        return DistinctAndSort(TopLevelItems.Concat(GlobalSnippetItems));
    }

    internal IReadOnlyList<RejectOnlyStateEventIssue> GetRejectOnlyStateEventIssues(DocumentUri uri)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return Array.Empty<RejectOnlyStateEventIssue>();

        var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
        if (parseDiags.Count > 0 || model is null)
            return Array.Empty<RejectOnlyStateEventIssue>();

        var validation = PreceptCompiler.Validate(model);
        if (validation.HasErrors)
            return Array.Empty<RejectOnlyStateEventIssue>();

        return PreceptAnalysis.Analyze(model)
            .RejectOnlyStateEventIssues
            .Select(issue => new RejectOnlyStateEventIssue(
                issue.StateName,
                issue.EventName,
                Math.Max(0, issue.SourceLine - 1),
                issue.EventSucceedsElsewhere))
            .ToArray();
    }

    internal IReadOnlyList<OrphanedEventIssue> GetOrphanedEventIssues(DocumentUri uri)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return Array.Empty<OrphanedEventIssue>();

        var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
        if (parseDiags.Count > 0 || model is null)
            return Array.Empty<OrphanedEventIssue>();

        var validation = PreceptCompiler.Validate(model);
        if (validation.HasErrors)
            return Array.Empty<OrphanedEventIssue>();

        return PreceptAnalysis.Analyze(model)
            .OrphanedEventIssues
            .Select(issue => new OrphanedEventIssue(issue.EventName, Math.Max(0, issue.SourceLine - 1)))
            .ToArray();
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

        var filtered = baseline.Where(item =>
            item.Kind is CompletionItemKind.Operator or CompletionItemKind.Snippet ||
            IsTypedExpressionCompletionItem(item, scope.Symbols, expectedKind, dataFieldNames, eventName, collectionKinds));

        // For choice fields, also offer the declared member values as string literal completions
        if (target.Type == PreceptScalarType.Choice && target.ChoiceValues?.Count > 0)
        {
            var choiceLiterals = target.ChoiceValues.Select(static v =>
                new CompletionItem { Label = $"\"{v}\"", Kind = CompletionItemKind.EnumMember, Detail = "choice value" });
            return DistinctAndSort(filtered.Concat(choiceLiterals));
        }

        return DistinctAndSort(filtered);
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

        var filtered = baseline.Where(item =>
            item.Kind is CompletionItemKind.Operator or CompletionItemKind.Snippet ||
            IsTypedExpressionCompletionItem(item, scope.Symbols, expectedKind, dataFieldNames, eventName, collectionKinds));

        // For choice collections, also offer the declared member values as string literal completions
        if (target.InnerType == PreceptScalarType.Choice && target.ChoiceValues?.Count > 0)
        {
            var choiceLiterals = target.ChoiceValues.Select(static v =>
                new CompletionItem { Label = $"\"{v}\"", Kind = CompletionItemKind.EnumMember, Detail = "choice value" });
            return DistinctAndSort(filtered.Concat(choiceLiterals));
        }

        return DistinctAndSort(filtered);
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

    /// <summary>
    /// Expression completions for the derived field context (after <c>-></c> in a field declaration).
    /// Includes data fields and collection accessors but excludes event arguments (not in scope).
    /// </summary>
    private static IReadOnlyList<CompletionItem> BuildDerivedExpressionCompletions(
        IReadOnlyList<string> dataFields,
        IReadOnlyList<string> collectionFields,
        IReadOnlyDictionary<string, PreceptCollectionKind> collectionKinds)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(BuildItems(collectionFields, CompletionItemKind.Variable));
        items.AddRange(BuildCollectionScopeItems(collectionKinds));
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);
        return DistinctAndSort(items);
    }

    private static IReadOnlyList<CompletionItem> BuildEventEnsureCompletions(
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

    private static IReadOnlyList<CompletionItem> BuildStringMemberItems(string fieldName, bool isNullable)
    {
        var doc = isNullable
            ? "Returns the string's character count (UTF-16 code units). Use 'field != null and field.length ...' for nullable strings."
            : "Returns the string's character count (UTF-16 code units).";
        return
        [
            new CompletionItem
            {
                Label = fieldName + ".length",
                Kind = CompletionItemKind.Property,
                Detail = "number",
                Documentation = new StringOrMarkupContent(doc)
            }
        ];
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

    private static IReadOnlyList<Diagnostic> GetSemanticDiagnostics(PreceptDefinition model, string[] lines)
    {
        var diagnostics = new List<Diagnostic>();

        AddEntryAssertDiagnostics(model, lines, diagnostics);

        return diagnostics;
    }

    private static Diagnostic MapValidationDiagnostic(PreceptValidationDiagnostic diagnostic, string[] lines)
    {
        var lineIndex = Math.Max(0, diagnostic.Line - 1);
        var lineLength = lineIndex < lines.Length ? lines[lineIndex].Length : 1;
        var message = string.IsNullOrWhiteSpace(diagnostic.StateContext)
            ? diagnostic.Message
            : $"{diagnostic.Message} [state {diagnostic.StateContext}]";

        var severity = diagnostic.Constraint.Severity switch
        {
            ConstraintSeverity.Warning => DiagnosticSeverity.Warning,
            ConstraintSeverity.Hint => DiagnosticSeverity.Hint,
            _ => DiagnosticSeverity.Error
        };

        return new Diagnostic
        {
            Severity = severity,
            Message = message,
            Source = "precept",
            Code = new DiagnosticCode(diagnostic.DiagnosticCode),
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(lineIndex, Math.Max(0, diagnostic.Column)),
                new Position(lineIndex, Math.Max(diagnostic.Column + 1, lineLength)))
        };
    }

    private static void AddEntryAssertDiagnostics(
        PreceptDefinition model,
        string[] lines,
        List<Diagnostic> diagnostics)
    {
        if (model.StateEnsures is not null)
        {
            var statesWithToAsserts = model.StateEnsures
                .Where(sa => sa.Anchor == EnsureAnchor.To)
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
                            Message = $"State '{stateName}' has entry ensures but no transition targets it — entry ensures are never checked.",
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

    internal static readonly IReadOnlyList<CompletionItem> KeywordItems = BuildKeywordItems();

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

            // Exclude continuation-only keywords that are never valid standalone
            if (symbol is "then" or "else") continue;

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

    /// <summary>
    /// Top-level declaration keywords appropriate for blank lines / statement start.
    /// Excludes action keywords (set, add), outcome keywords (transition, reject),
    /// grammar modifiers (as, nullable, because), and constraint keywords that only
    /// make sense inside specific declaration contexts.
    /// </summary>
    private static readonly IReadOnlyList<CompletionItem> TopLevelItems = BuildTopLevelItems();

    private static IReadOnlyList<CompletionItem> BuildTopLevelItems()
    {
        var topLevelCategories = new HashSet<TokenCategory>
        {
            TokenCategory.Declaration,
            TokenCategory.Control
        };

        var items = new List<CompletionItem>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            var category = PreceptTokenMeta.GetCategory(token);
            var symbol = PreceptTokenMeta.GetSymbol(token);
            var description = PreceptTokenMeta.GetDescription(token);

            if (category is null || symbol is null) continue;
            if (!symbol.All(char.IsLetter)) continue;
            if (!topLevelCategories.Contains(category.Value)) continue;

            // Exclude expression-only control keywords from statement-start completions
            if (symbol is "if" or "then" or "else") continue;

            items.Add(new CompletionItem
            {
                Label = symbol,
                Kind = CompletionItemKind.Keyword,
                Detail = description
            });
        }

        return items;
    }

    internal static readonly IReadOnlyList<CompletionItem> ExpressionOperatorItems =
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
        new CompletionItem { Label = "and", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "or", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "not", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "contains", Kind = CompletionItemKind.Operator },
        // Numeric functions
        FunctionSnippetItem("abs(expr)", "abs(${1:expr})", "Absolute value"),
        FunctionSnippetItem("floor(expr)", "floor(${1:expr})", "Round down to nearest integer"),
        FunctionSnippetItem("ceil(expr)", "ceil(${1:expr})", "Round up to nearest integer"),
        FunctionSnippetItem("round(expr)", "round(${1:expr})", "Round to nearest integer"),
        FunctionSnippetItem("round(expr, N)", "round(${1:expr}, ${2:2})", "Round a decimal value to N places"),
        FunctionSnippetItem("truncate(expr)", "truncate(${1:expr})", "Truncate toward zero"),
        FunctionSnippetItem("min(a, b)", "min(${1:a}, ${2:b})", "Minimum of two or more values"),
        FunctionSnippetItem("max(a, b)", "max(${1:a}, ${2:b})", "Maximum of two or more values"),
        FunctionSnippetItem("pow(base, exp)", "pow(${1:base}, ${2:exp})", "Raise to integer power"),
        FunctionSnippetItem("sqrt(expr)", "sqrt(${1:expr})", "Square root (requires non-negative)"),
        FunctionSnippetItem("clamp(value, lo, hi)", "clamp(${1:value}, ${2:lo}, ${3:hi})", "Clamp value to range [lo, hi]"),
        // String functions
        FunctionSnippetItem("toLower(expr)", "toLower(${1:expr})", "Convert string to lowercase"),
        FunctionSnippetItem("toUpper(expr)", "toUpper(${1:expr})", "Convert string to uppercase"),
        FunctionSnippetItem("startsWith(str, prefix)", "startsWith(${1:str}, ${2:prefix})", "Check if string starts with prefix"),
        FunctionSnippetItem("endsWith(str, suffix)", "endsWith(${1:str}, ${2:suffix})", "Check if string ends with suffix"),
        FunctionSnippetItem("trim(expr)", "trim(${1:expr})", "Remove leading/trailing whitespace"),
        FunctionSnippetItem("left(str, count)", "left(${1:str}, ${2:count})", "First N characters (clamping)"),
        FunctionSnippetItem("right(str, count)", "right(${1:str}, ${2:count})", "Last N characters"),
        FunctionSnippetItem("mid(str, start, count)", "mid(${1:str}, ${2:start}, ${3:count})", "Substring from position (1-indexed)"),
        // Conditional expression
        SnippetItem("if ... then ... else", "if ${1:condition} then ${2:value} else ${3:value}", "Conditional expression")
    ];

    internal static readonly IReadOnlyList<CompletionItem> LiteralItems =
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
        SnippetItem("rule", "rule ${1:Expr} because \"${2:Reason}\"", "Global rule"),
        SnippetItem("event with args", "event ${1:Name} with ${2:Arg} as ${3:string}", "Event with arguments")
    ];

    private static readonly IReadOnlyList<CompletionItem> SetSnippetItems =
    [
        SnippetItem("set assignment", "set ${1:Field} = ${2:value}", "Data assignment")
    ];

    // ── New-syntax item lists ──────────────────────────────────────────

    /// <summary>Scalar type keywords for positions after "as" / "of" / event arg type.</summary>
    internal static readonly IReadOnlyList<CompletionItem> ScalarTypeItems =
    [
        new CompletionItem { Label = "string", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "number", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "boolean", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "integer", Kind = CompletionItemKind.TypeParameter, Detail = "Whole number (no decimal point)" },
        new CompletionItem { Label = "decimal", Kind = CompletionItemKind.TypeParameter, Detail = "Exact base-10 decimal number" },
        SnippetItem("choice(...)", "choice(\"${1:A}\", \"${2:B}\")", "Constrained value from a predefined set")
    ];

    /// <summary>All type keywords for positions after "field Name as" (scalar + collection).</summary>
    internal static readonly IReadOnlyList<CompletionItem> TypeItems =
    [
        new CompletionItem { Label = "string", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "number", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "boolean", Kind = CompletionItemKind.TypeParameter },
        new CompletionItem { Label = "integer", Kind = CompletionItemKind.TypeParameter, Detail = "Whole number (no decimal point)" },
        new CompletionItem { Label = "decimal", Kind = CompletionItemKind.TypeParameter, Detail = "Exact base-10 decimal number" },
        SnippetItem("choice(...)", "choice(\"${1:A}\", \"${2:B}\")", "Constrained value from a predefined set"),
        new CompletionItem { Label = "set", Kind = CompletionItemKind.TypeParameter, Detail = "Sorted unique set" },
        new CompletionItem { Label = "queue", Kind = CompletionItemKind.TypeParameter, Detail = "FIFO ordered" },
        new CompletionItem { Label = "stack", Kind = CompletionItemKind.TypeParameter, Detail = "LIFO ordered" }
    ];

    /// <summary>Action/outcome keywords available after "->" in flat transition rows.</summary>
    internal static readonly IReadOnlyList<CompletionItem> ArrowItems =
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
    private static readonly CompletionItem WhenItem = new() { Label = "when", Kind = CompletionItemKind.Keyword, Detail = "Conditional guard" };
    private static readonly CompletionItem ArrowPipelineItem = new() { Label = "->", Kind = CompletionItemKind.Operator, Detail = "action/outcome pipeline" };
    private static readonly CompletionItem DeriveItem = new() { Label = "->", Kind = CompletionItemKind.Operator, Detail = "computed field derivation" };
    private static readonly CompletionItem CommaItem = new() { Label = ",", Kind = CompletionItemKind.Operator, Detail = "Next event argument" };

    internal static readonly IReadOnlyList<CompletionItem> NumberConstraintItems =
    [
        new CompletionItem { Label = "nonnegative", Kind = CompletionItemKind.Keyword, Detail = "number constraint", Documentation = new StringOrMarkupContent("Field must be >= 0") },
        new CompletionItem { Label = "positive",    Kind = CompletionItemKind.Keyword, Detail = "number constraint", Documentation = new StringOrMarkupContent("Field must be > 0") },
        new CompletionItem { Label = "min",         Kind = CompletionItemKind.Keyword, Detail = "number constraint", Documentation = new StringOrMarkupContent("Field must be >= value") },
        new CompletionItem { Label = "max",         Kind = CompletionItemKind.Keyword, Detail = "number constraint", Documentation = new StringOrMarkupContent("Field must be <= value") },
    ];

    internal static readonly IReadOnlyList<CompletionItem> StringConstraintItems =
    [
        new CompletionItem { Label = "notempty",   Kind = CompletionItemKind.Keyword, Detail = "string constraint", Documentation = new StringOrMarkupContent("Field must not be empty") },
        new CompletionItem { Label = "minlength",  Kind = CompletionItemKind.Keyword, Detail = "string constraint", Documentation = new StringOrMarkupContent("Field must have at least N characters") },
        new CompletionItem { Label = "maxlength",  Kind = CompletionItemKind.Keyword, Detail = "string constraint", Documentation = new StringOrMarkupContent("Field must have at most N characters") },
    ];

    internal static readonly IReadOnlyList<CompletionItem> CollectionConstraintItems =
    [
        new CompletionItem { Label = "notempty",  Kind = CompletionItemKind.Keyword, Detail = "collection constraint", Documentation = new StringOrMarkupContent("Collection must not be empty") },
        new CompletionItem { Label = "mincount",  Kind = CompletionItemKind.Keyword, Detail = "collection constraint", Documentation = new StringOrMarkupContent("Collection must have at least N items") },
        new CompletionItem { Label = "maxcount",  Kind = CompletionItemKind.Keyword, Detail = "collection constraint", Documentation = new StringOrMarkupContent("Collection must have at most N items") },
    ];

    internal static readonly IReadOnlyList<CompletionItem> DecimalConstraintItems =
    [
        new CompletionItem { Label = "nonnegative", Kind = CompletionItemKind.Keyword, Detail = "decimal constraint", Documentation = new StringOrMarkupContent("Field must be >= 0") },
        new CompletionItem { Label = "positive",    Kind = CompletionItemKind.Keyword, Detail = "decimal constraint", Documentation = new StringOrMarkupContent("Field must be > 0") },
        new CompletionItem { Label = "min",         Kind = CompletionItemKind.Keyword, Detail = "decimal constraint", Documentation = new StringOrMarkupContent("Field must be >= value") },
        new CompletionItem { Label = "max",         Kind = CompletionItemKind.Keyword, Detail = "decimal constraint", Documentation = new StringOrMarkupContent("Field must be <= value") },
        SnippetItem("maxplaces N", "maxplaces ${1:2}", "Maximum decimal places"),
    ];

    internal static readonly IReadOnlyList<CompletionItem> ChoiceConstraintItems =
    [
        new CompletionItem { Label = "ordered", Kind = CompletionItemKind.Keyword, Detail = "choice constraint", Documentation = new StringOrMarkupContent("Enables ordinal comparison of choice values") },
    ];

    private static bool EndsWithCompletedExpression(string text)
        => Regex.IsMatch(text, "(?:[A-Za-z0-9_\\)\"]|true|false|null)\\s+$", RegexOptions.IgnoreCase);

    // ═══════════════════════════════════════════════════════════════════
    // Field/Arg Modifier Completion Helpers (any-order modifier support)
    // ═══════════════════════════════════════════════════════════════════

    // Matches a scalar field declaration in the modifier zone: field Name as <type> <rest...>
    private static readonly Regex FieldScalarModifierZoneRegex = new(
        @"^\s*field\s+(?:[A-Za-z_]\w*\s*,\s*)*[A-Za-z_]\w*\s+as\s+(?<type>number|string|boolean|integer|decimal|choice\([^)]*\))\s+(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches a collection field declaration in the modifier zone: field Name as set|queue|stack of <inner> <rest...>
    private static readonly Regex FieldCollectionModifierZoneRegex = new(
        @"^\s*field\s+(?:[A-Za-z_]\w*\s*,\s*)*[A-Za-z_]\w*\s+as\s+(?:set|queue|stack)\s+of\s+\w+\s+(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Detects if the cursor is in a field declaration modifier zone and returns the
    /// remaining modifiers appropriate for the field type. Returns null if not in modifier zone.
    /// </summary>
    private static IReadOnlyList<CompletionItem>? TryGetFieldModifierCompletions(string beforeCursor)
    {
        if (!beforeCursor.EndsWith(' '))
            return null;

        // Try scalar field
        var scalarMatch = FieldScalarModifierZoneRegex.Match(beforeCursor);
        if (scalarMatch.Success)
        {
            var rest = scalarMatch.Groups["rest"].Value;
            if (rest.Length > 0 && IsValueExpectingKeyword(rest.TrimEnd().Split(' ')[^1]))
                return null;
            return ComputeRemainingModifiers(scalarMatch.Groups["type"].Value, rest, isCollection: false, isEventArg: false);
        }

        // Try collection field
        var colMatch = FieldCollectionModifierZoneRegex.Match(beforeCursor);
        if (colMatch.Success)
        {
            var rest = colMatch.Groups["rest"].Value;
            if (rest.Length > 0 && IsValueExpectingKeyword(rest.TrimEnd().Split(' ')[^1]))
                return null;
            return ComputeRemainingModifiers("collection", rest, isCollection: true, isEventArg: false);
        }

        return null;
    }

    /// <summary>
    /// Detects if the cursor is in an event argument modifier zone and returns the
    /// remaining modifiers. Returns null if not in modifier zone.
    /// </summary>
    private static IReadOnlyList<CompletionItem>? TryGetEventArgModifierCompletions(string beforeCursor)
    {
        if (!beforeCursor.EndsWith(' '))
            return null;

        var eventMatch = NewEventWithArgsRegex.Match(beforeCursor);
        if (!eventMatch.Success)
            return null;

        var argsText = eventMatch.Groups["args"].Value;
        var lastArg = ExtractLastEventArg(argsText);

        var argTypeMatch = Regex.Match(lastArg,
            @"^[A-Za-z_][A-Za-z0-9_]*\s+as\s+(?<type>string|number|boolean|integer|decimal|choice\([^)]*\))\s+(?<rest>.*)$",
            RegexOptions.IgnoreCase);
        if (!argTypeMatch.Success)
            return null;

        var rest = argTypeMatch.Groups["rest"].Value;
        if (rest.Length > 0 && IsValueExpectingKeyword(rest.TrimEnd().Split(' ')[^1]))
            return null;

        return ComputeRemainingModifiers(argTypeMatch.Groups["type"].Value, rest, isCollection: false, isEventArg: true);
    }

    /// <summary>
    /// Extracts the last event argument text from a comma-delimited arg list,
    /// correctly handling commas inside choice(...) parentheses.
    /// </summary>
    private static string ExtractLastEventArg(string argsText)
    {
        int depth = 0;
        int lastComma = -1;
        for (int i = 0; i < argsText.Length; i++)
        {
            switch (argsText[i])
            {
                case '(': depth++; break;
                case ')': depth--; break;
                case ',' when depth == 0: lastComma = i; break;
            }
        }
        return lastComma >= 0 ? argsText[(lastComma + 1)..].TrimStart() : argsText.TrimStart();
    }

    private static bool IsValueExpectingKeyword(string word)
        => word.Equals("default", StringComparison.OrdinalIgnoreCase) ||
           word.Equals("min", StringComparison.OrdinalIgnoreCase) ||
           word.Equals("max", StringComparison.OrdinalIgnoreCase) ||
           word.Equals("minlength", StringComparison.OrdinalIgnoreCase) ||
           word.Equals("maxlength", StringComparison.OrdinalIgnoreCase) ||
           word.Equals("mincount", StringComparison.OrdinalIgnoreCase) ||
           word.Equals("maxcount", StringComparison.OrdinalIgnoreCase) ||
           word.Equals("maxplaces", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Computes the remaining modifier completions for a field or event argument,
    /// given the type and any modifiers already present in the text.
    /// </summary>
    private static IReadOnlyList<CompletionItem> ComputeRemainingModifiers(
        string typeStr, string existingModifierText, bool isCollection, bool isEventArg)
    {
        bool isChoice = typeStr.StartsWith("choice", StringComparison.OrdinalIgnoreCase);
        string normalizedType = isCollection ? "collection" : isChoice ? "choice" : typeStr.ToLowerInvariant();

        bool hasNullable = Regex.IsMatch(existingModifierText, @"\bnullable\b", RegexOptions.IgnoreCase);
        bool hasDefault = Regex.IsMatch(existingModifierText, @"\bdefault\b", RegexOptions.IgnoreCase);
        bool hasDerived = existingModifierText.Contains("->", StringComparison.Ordinal);

        var items = new List<CompletionItem>();

        if (!isCollection && !hasDerived)
        {
            if (!hasNullable) items.Add(NullableItem);
            if (!hasDefault) items.Add(DefaultItem);
        }

        // Offer derivation operator for scalar fields (not collections, not event args, not already derived)
        if (!isCollection && !isEventArg && !hasDerived && !hasDefault && !hasNullable)
            items.Add(DeriveItem);

        IReadOnlyList<CompletionItem> constraintPool = normalizedType switch
        {
            "number" or "integer" => NumberConstraintItems,
            "decimal" => DecimalConstraintItems,
            "string" => StringConstraintItems,
            "collection" => CollectionConstraintItems,
            "choice" => ChoiceConstraintItems,
            _ => Array.Empty<CompletionItem>()
        };

        foreach (var item in constraintPool)
        {
            var keyword = item.Label.Split(' ')[0];
            if (!Regex.IsMatch(existingModifierText, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase))
                items.Add(item);
        }

        if (isEventArg) items.Add(CommaItem);
        return items.Count > 0 ? items.ToArray() : Array.Empty<CompletionItem>();
    }

    private static CompletionItem SnippetItem(string label, string snippet, string detail)
        => new()
        {
            Label = label,
            Kind = CompletionItemKind.Snippet,
            InsertText = snippet,
            InsertTextFormat = InsertTextFormat.Snippet,
            Detail = detail
        };

    private static CompletionItem FunctionSnippetItem(string label, string snippet, string detail)
        => new()
        {
            Label = label,
            Kind = CompletionItemKind.Function,
            InsertText = snippet,
            InsertTextFormat = InsertTextFormat.Snippet,
            Detail = detail
        };

    /// <summary>
    /// Regex fallback for detecting computed field names when the parser model is unavailable
    /// (e.g. incomplete documents during editing). Scans for <c>field Name as Type -&gt;</c> patterns.
    /// </summary>
    private static HashSet<string> DetectComputedFieldsByRegex(string[] lines)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"^\s*field\s+([A-Za-z_]\w*)\s+as\s+\S+\s*->", RegexOptions.IgnoreCase);
            if (match.Success)
                result.Add(match.Groups[1].Value);
        }
        return result;
    }

    internal sealed record RejectOnlyStateEventIssue(
        string StateName,
        string EventName,
        int FirstLineIndex,
        bool EventSucceedsElsewhere);

    internal sealed record OrphanedEventIssue(
        string EventName,
        int LineIndex);
}
