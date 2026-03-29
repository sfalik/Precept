using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Superpower.Model;

namespace Precept.LanguageServer;

internal sealed class PreceptSemanticTokensHandler : SemanticTokensHandlerBase
{
    private static readonly TextDocumentSelector Selector = TextDocumentSelector.ForPattern("**/*.precept");

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

    // Comments are stripped by the tokenizer, so we scan them from raw text.
    private static readonly Regex LineCommentRegex = new("#.*$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Tracks declaration context for comma-separated name lists.</summary>
    private enum DeclContext { None, State, Event }

    // ── Attribute-driven semantic type map ────────────────────────────
    // Built once at startup from PreceptTokenMeta — adding a keyword to
    // the PreceptToken enum with [TokenCategory] automatically gives it
    // the correct semantic coloring with no change needed here.
    private static readonly Dictionary<PreceptToken, string> SemanticTypeMap = BuildSemanticTypeMap();

    // Tokens whose following identifier is a state name (colored as "type")
    private static readonly HashSet<PreceptToken> StateContextTokens =
    [
        PreceptToken.State,
        PreceptToken.Transition,
        PreceptToken.In,
        PreceptToken.To
    ];

    // Tokens whose following identifier is an event name (colored as "function")
    private static readonly HashSet<PreceptToken> EventContextTokens =
    [
        PreceptToken.Event,
        PreceptToken.On
    ];

    // Tokens whose following identifier is a field name (colored as "variable")
    private static readonly HashSet<PreceptToken> FieldContextTokens =
    [
        PreceptToken.Field,
        PreceptToken.Set,
        PreceptToken.Add,
        PreceptToken.Remove,
        PreceptToken.Enqueue,
        PreceptToken.Dequeue,
        PreceptToken.Push,
        PreceptToken.Pop,
        PreceptToken.Clear,
        PreceptToken.Into
    ];

    private static Dictionary<PreceptToken, string> BuildSemanticTypeMap()
    {
        var map = new Dictionary<PreceptToken, string>();
        foreach (var member in Enum.GetValues<PreceptToken>())
        {
            var category = PreceptTokenMeta.GetCategory(member);
            var semanticType = category switch
            {
                TokenCategory.Control => "keyword",
                TokenCategory.Declaration => "keyword",
                TokenCategory.Action => "keyword",
                TokenCategory.Outcome => "keyword",
                TokenCategory.Type => "type",
                TokenCategory.Literal => "keyword",  // true/false/null
                TokenCategory.Operator => "operator",
                TokenCategory.Punctuation => member == PreceptToken.Arrow ? "operator" : null,
                _ => null
            };
            if (semanticType != null)
                map[member] = semanticType;
        }

        return map;
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = Selector,
        Legend = Legend,
        Range = true,
        Full = true
    };

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        => Task.FromResult(new SemanticTokensDocument(Legend));

    protected override Task Tokenize(SemanticTokensBuilder builder,
        ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        if (!PreceptTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(@params.TextDocument.Uri, out var text))
            return Task.CompletedTask;

        // ── 1. Comments — scanned from raw text since the tokenizer strips them ──
        foreach (Match match in LineCommentRegex.Matches(text))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (line, col) = OffsetToLineCol(text, match.Index);
            Push(builder, line, col, match.Length, "comment");
        }

        // ── 2. Token stream — keywords, operators, identifiers, literals ──
        TokenList<PreceptToken> tokens;
        try
        {
            tokens = PreceptTokenizerBuilder.Instance.Tokenize(text);
        }
        catch
        {
            // Tokenization failed — comments already emitted, return gracefully
            return Task.CompletedTask;
        }

        PreceptToken? previousKind = null;
        var declContext = DeclContext.None;

        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Superpower positions are 1-based; LSP is 0-based
            var line = token.Span.Position.Line - 1;
            var col = token.Span.Position.Column - 1;
            var len = token.Span.Length;

            // Track declaration context for comma-separated name lists
            if (token.Kind == PreceptToken.State)
                declContext = DeclContext.State;
            else if (token.Kind == PreceptToken.Event)
                declContext = DeclContext.Event;
            else if (token.Kind == PreceptToken.NewLine)
                declContext = DeclContext.None;
            else if (token.Kind == PreceptToken.With)
                declContext = DeclContext.None;

            string? semanticType = null;

            // 2a. Fixed-category tokens (keywords, operators, types)
            if (SemanticTypeMap.TryGetValue(token.Kind, out var mapped))
            {
                semanticType = mapped;
            }
            // 2b. Identifiers — classified by preceding token context
            else if (token.Kind == PreceptToken.Identifier)
            {
                semanticType = ClassifyIdentifier(previousKind, declContext);
            }
            // 2c. String literals
            else if (token.Kind == PreceptToken.StringLiteral)
            {
                semanticType = "string";
            }
            // 2d. Number literals
            else if (token.Kind == PreceptToken.NumberLiteral)
            {
                semanticType = "number";
            }

            if (semanticType != null)
                Push(builder, line, col, len, semanticType);

            // Track previous non-trivial token for context
            if (token.Kind != PreceptToken.NewLine)
                previousKind = token.Kind;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Classifies an identifier token based on the preceding token:
    /// - After state/transition/in/to → "type" (state name)
    /// - After event/on → "function" (event name)
    /// - After field/set/add/remove/.../into → "variable" (field name)
    /// - After precept → "type" (precept name)
    /// - After from → "type" (state name; covers "from State on Event")
    /// - After dot → "variable" (member access like Collection.count)
    /// - Otherwise → "variable" (bare identifier in expression)
    /// </summary>
    private static string ClassifyIdentifier(PreceptToken? previousKind, DeclContext context) => previousKind switch
    {
        PreceptToken.Precept => "type",
        PreceptToken.From => "type",
        PreceptToken.Dot => "variable",
        PreceptToken.Comma when context == DeclContext.State => "type",
        PreceptToken.Comma when context == DeclContext.Event => "function",
        PreceptToken.Comma => "variable",
        _ when previousKind.HasValue && StateContextTokens.Contains(previousKind.Value) => "type",
        _ when previousKind.HasValue && EventContextTokens.Contains(previousKind.Value) => "function",
        _ when previousKind.HasValue && FieldContextTokens.Contains(previousKind.Value) => "variable",
        PreceptToken.Edit => "variable",
        _ => "variable" // default: bare identifier in expression position
    };

    /// <summary>Converts a character offset in the full text to (line, column), both 0-based.</summary>
    private static (int line, int col) OffsetToLineCol(string text, int offset)
    {
        var line = 0;
        var col = 0;
        for (var i = 0; i < offset && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                col = 0;
            }
            else if (text[i] != '\r')
            {
                col++;
            }
        }

        return (line, col);
    }

    private static void Push(SemanticTokensBuilder builder, int line, int character, int length, string tokenType)
    {
        if (length <= 0) return;
        builder.Push(line, character, length, tokenType, Array.Empty<string>());
    }
}
