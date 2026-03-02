using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StateMachine.Dsl;

namespace StateMachine.Dsl.LanguageServer;

internal sealed class SmDslAnalyzer
{
    private static readonly Regex LineErrorRegex = new("^Line\\s+(?<line>\\d+)\\s*:\\s*(?<message>.+)$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new("^\\s*state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("^\\s*event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex DataFieldRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventArgRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FromOnRegex = new("^\\s*from\\s+(?<from>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventMemberPrefixRegex = new("(?<event>[A-Za-z_][A-Za-z0-9_]*)\\.$", RegexOptions.Compiled);

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

        try
        {
            var machine = StateMachineDslParser.Parse(text);
            DslWorkflowCompiler.Compile(machine);
            return Array.Empty<Diagnostic>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
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
        var eventArgs = CollectEventArgs(lines);
        var currentEvent = FindCurrentEventName(lines, (int)position.Line);

        var eventMemberPrefixMatch = EventMemberPrefixRegex.Match(beforeCursor);
        if (eventMemberPrefixMatch.Success)
        {
            var eventName = eventMemberPrefixMatch.Groups["event"].Value;
            if (eventArgs.TryGetValue(eventName, out var argsForEvent))
                return BuildItems(argsForEvent, CompletionItemKind.Field);
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*(?:if|else\\s+if)\\s+[^\\n]*$", RegexOptions.IgnoreCase))
        {
            var guardItems = BuildGuardCompletions(dataFields, currentEvent, eventArgs);
            return DistinctAndSort(guardItems.Concat(GuardSnippetItems));
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*transform\\s+[^=\\n]*$", RegexOptions.IgnoreCase) &&
            !beforeCursor.Contains('=', StringComparison.Ordinal))
            return DistinctAndSort(BuildItems(dataFields, CompletionItemKind.Field).Concat(TransformSnippetItems));

        if (Regex.IsMatch(beforeCursor, "^\\s*transform\\s+[A-Za-z_][A-Za-z0-9_]*\\s*=\\s*[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildExpressionCompletions(dataFields, currentEvent, eventArgs);

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*\\s+on(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*$", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(beforeCursor, "\\s+on\\s+", RegexOptions.IgnoreCase))
        {
            var stateItems = BuildItems(states.Append("any"), CompletionItemKind.EnumMember);
            return stateItems.Concat(KeywordItems.Where(item => item.Label == "on")).ToArray();
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*transition(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return DistinctAndSort(BuildItems(states, CompletionItemKind.EnumMember).Concat(TransitionSnippetItems));

        if (Regex.IsMatch(beforeCursor, "^\\s*inspect\\s+[^\\n]*$", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(beforeCursor, "^\\s*fire\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        return DistinctAndSort(KeywordItems.Concat(GlobalSnippetItems));
    }

    private static IReadOnlyList<CompletionItem> BuildGuardCompletions(
        IReadOnlyList<string> dataFields,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(GuardOperatorItems);
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
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
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
        new CompletionItem { Label = "machine", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "state", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "event", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "from", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "on", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "if", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "else if", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "else", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "transition", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "transform", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "reject", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "no transition", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "string", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "number", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "boolean", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "null", Kind = CompletionItemKind.Keyword }
    ];

    private static readonly IReadOnlyList<CompletionItem> GuardOperatorItems =
    [
        new CompletionItem { Label = "==", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "!=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = ">", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = ">=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "<", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "<=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "&&", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "||", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "!", Kind = CompletionItemKind.Operator }
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

    private static readonly IReadOnlyList<CompletionItem> TransformSnippetItems =
    [
        SnippetItem("transform assignment", "transform ${1:Field} = ${2:value}", "Data assignment")
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
}
