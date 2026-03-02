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

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*\\s+on\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*$", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(beforeCursor, "\\s+on\\s+", RegexOptions.IgnoreCase))
        {
            var stateItems = BuildItems(states.Append("any"), CompletionItemKind.EnumMember);
            return stateItems.Concat(KeywordItems.Where(item => item.Label == "on")).ToArray();
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*transition\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildItems(states, CompletionItemKind.EnumMember);

        if (Regex.IsMatch(beforeCursor, "^\\s*inspect\\s+[^\\n]*$", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(beforeCursor, "^\\s*fire\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        return KeywordItems;
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
}
