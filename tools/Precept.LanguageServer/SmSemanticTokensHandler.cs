using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer;

internal sealed class SmSemanticTokensHandler : SemanticTokensHandlerBase
{
    private static readonly TextDocumentSelector Selector = TextDocumentSelector.ForPattern("**/*.sm");

    private static readonly SemanticTokensLegend Legend = new()
    {
        TokenTypes = new Container<SemanticTokenType>(
            "keyword",
            "type",
            "function",
            "variable",
            "number",
            "string",
            "operator",
            "comment"
        ),
        TokenModifiers = new Container<SemanticTokenModifier>()
    };

    private static readonly Regex LineCommentRegex = new("#.*$", RegexOptions.Compiled);
    private static readonly Regex StringRegex = new("\"(?:\\\\.|[^\"])*\"|'(?:\\\\.|[^'])*'", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new("\\b\\d+(?:\\.\\d+)?\\b", RegexOptions.Compiled);
    private static readonly Regex OperatorRegex = new("==|!=|>=|<=|&&|\\|\\||[+\\-*/%]|>|<|=|!", RegexOptions.Compiled);

    private static readonly Regex MachineDeclRegex = new("^(\\s*)precept\\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex StateDeclRegex = new("^(\\s*)state\\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex EventDeclRegex = new("^(\\s*)event\\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex FromOnRegex = new("^(\\s*)from\\s+(any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex TransitionRegex = new("^(\\s*)transition\\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex SetRegex = new("^(\\s*)set\\s+([A-Za-z_][A-Za-z0-9_]*)\\s*=", RegexOptions.Compiled);
    private static readonly Regex TypeDeclRegex = new("^(\\s*)(string|number|boolean|null)\\??\\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Dotted event arg reference: EventName.ArgName in expression positions
    private static readonly Regex EventArgRefRegex = new("\\b([A-Za-z_][A-Za-z0-9_]*)(\\.)([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    // Variable identifiers in expression lines (after set =, if/else if, rule keyword, or collection mutation value)
    private static readonly Regex ExpressionLineRegex = new("^\\s*(?:if|else\\s+if|set\\s+[A-Za-z_][A-Za-z0-9_]*\\s*=|rule|(?:add|remove|push|enqueue)\\s+[A-Za-z_][A-Za-z0-9_]*|from\\s+(?:any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+[A-Za-z_][A-Za-z0-9_]*\\s+when)\\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IdentifierInExprRegex = new("\\b([A-Za-z_][A-Za-z0-9_]*)\\b", RegexOptions.Compiled);

    private static readonly string[] KeywordTokens =
    [
        "precept", "state", "initial", "event", "from", "on", "if", "else",
        "transition", "set", "reject", "rule", "reason", "no", "any",
        "true", "false", "null", "string", "number", "boolean",
        "add", "remove", "enqueue", "dequeue", "push", "pop", "clear",
        "contains", "queue", "stack", "into", "above", "below", "when"
    ];

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = Selector,
            Legend = Legend,
            Range = true,
            Full = true
        };

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        => Task.FromResult(new SemanticTokensDocument(Legend));

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        if (!SmTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(@params.TextDocument.Uri, out var text))
            return Task.CompletedTask;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TokenizeLine(builder, lines[lineIndex], lineIndex);
        }

        return Task.CompletedTask;
    }

    private static void TokenizeLine(SemanticTokensBuilder builder, string line, int lineIndex)
    {
        foreach (Match match in LineCommentRegex.Matches(line))
            Push(builder, lineIndex, match.Index, match.Length, "comment");

        foreach (Match match in StringRegex.Matches(line))
            Push(builder, lineIndex, match.Index, match.Length, "string");

        foreach (Match match in NumberRegex.Matches(line))
            Push(builder, lineIndex, match.Index, match.Length, "number");

        foreach (Match match in OperatorRegex.Matches(line))
            Push(builder, lineIndex, match.Index, match.Length, "operator");

        HighlightKeywords(builder, line, lineIndex);
        HighlightNamedSymbols(builder, line, lineIndex);
    }

    private static void HighlightKeywords(SemanticTokensBuilder builder, string line, int lineIndex)
    {
        foreach (var keyword in KeywordTokens)
        {
            foreach (Match match in Regex.Matches(line, $"\\b{Regex.Escape(keyword)}\\b", RegexOptions.IgnoreCase))
                Push(builder, lineIndex, match.Index, match.Length, "keyword");
        }
    }

    private static void HighlightNamedSymbols(SemanticTokensBuilder builder, string line, int lineIndex)
    {
        var machineDecl = MachineDeclRegex.Match(line);
        if (machineDecl.Success)
            Push(builder, lineIndex, machineDecl.Groups[2].Index, machineDecl.Groups[2].Length, "type");

        var stateDecl = StateDeclRegex.Match(line);
        if (stateDecl.Success)
            Push(builder, lineIndex, stateDecl.Groups[2].Index, stateDecl.Groups[2].Length, "type");

        var eventDecl = EventDeclRegex.Match(line);
        if (eventDecl.Success)
            Push(builder, lineIndex, eventDecl.Groups[2].Index, eventDecl.Groups[2].Length, "function");

        var fromOn = FromOnRegex.Match(line);
        if (fromOn.Success)
        {
            Push(builder, lineIndex, fromOn.Groups[3].Index, fromOn.Groups[3].Length, "function");

            var statesGroup = fromOn.Groups[2];
            foreach (Match state in Regex.Matches(statesGroup.Value, "[A-Za-z_][A-Za-z0-9_]*"))
            {
                Push(builder, lineIndex, statesGroup.Index + state.Index, state.Length, "type");
            }
        }

        var transition = TransitionRegex.Match(line);
        if (transition.Success)
            Push(builder, lineIndex, transition.Groups[2].Index, transition.Groups[2].Length, "type");

        var set = SetRegex.Match(line);
        if (set.Success)
            Push(builder, lineIndex, set.Groups[2].Index, set.Groups[2].Length, "variable");

        var typeDecl = TypeDeclRegex.Match(line);
        if (typeDecl.Success)
            Push(builder, lineIndex, typeDecl.Groups[3].Index, typeDecl.Groups[3].Length, "variable");

        // Highlight EventName.ArgName dotted references in expression positions
        foreach (Match m in EventArgRefRegex.Matches(line))
        {
            Push(builder, lineIndex, m.Groups[1].Index, m.Groups[1].Length, "function");
            Push(builder, lineIndex, m.Groups[3].Index, m.Groups[3].Length, "variable");
        }

        // Highlight bare identifier references on expression lines (if/else if guards, set RHS, rule exprs)
        if (ExpressionLineRegex.IsMatch(line))
        {
            // Find where the expression body starts (after the first keyword prefix)
            var prefixMatch = ExpressionLineRegex.Match(line);
            var exprStart = prefixMatch.Index + prefixMatch.Length;
            foreach (Match m in IdentifierInExprRegex.Matches(line, exprStart))
            {
                // Skip tokens already colored as keywords
                if (KeywordTokens.Any(k => string.Equals(k, m.Value, StringComparison.OrdinalIgnoreCase)))
                    continue;
                Push(builder, lineIndex, m.Index, m.Length, "variable");
            }
        }
    }

    private static void Push(SemanticTokensBuilder builder, int line, int character, int length, string tokenType)
    {
        if (length <= 0)
            return;

        builder.Push(line, character, length, tokenType, Array.Empty<string>());
    }
}
