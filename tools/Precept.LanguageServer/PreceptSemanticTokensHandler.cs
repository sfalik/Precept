using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Superpower.Model;

namespace Precept.LanguageServer;

internal sealed class PreceptSemanticTokensHandler : SemanticTokensHandlerBase
{
    internal readonly record struct ClassifiedSemanticToken(string Text, string Type, string? Modifier);

    private static readonly TextDocumentSelector Selector = new(new TextDocumentFilter
    {
        Pattern = "**/*.precept",
        Language = "precept"
    });

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
            "comment",
            "preceptKeywordSemantic",
            "preceptKeywordGrammar",
            "preceptState",
            "preceptEvent",
            "preceptFieldName",
            "preceptType",
            "preceptValue",
            "preceptMessage"
        ),
        TokenModifiers = new Container<SemanticTokenModifier>(
            "preceptConstrained"
        )
    };

    // Comments are stripped by the tokenizer, so we scan them from raw text.
    private static readonly Regex LineCommentRegex = new("#.*$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Tracks declaration context for comma-separated name lists.</summary>
    private enum DeclContext { None, State, Event, Field }

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
                TokenCategory.Control => "preceptKeywordSemantic",
                TokenCategory.Declaration => "preceptKeywordSemantic",
                TokenCategory.Action => "preceptKeywordSemantic",
                TokenCategory.Outcome => "preceptKeywordSemantic",
                TokenCategory.Grammar => "preceptKeywordGrammar",
                TokenCategory.Type => "preceptType",
                TokenCategory.Literal => "preceptValue",
                TokenCategory.Operator => "preceptKeywordGrammar",
                TokenCategory.Punctuation => member == PreceptToken.Arrow ? "preceptKeywordGrammar" : null,
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

        foreach (var (token, semanticType, modifier) in ClassifyTokens(tokens, text))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = token.Span.Position.Line - 1;
            var col = token.Span.Position.Column - 1;
            var len = token.Span.Length;

            if (semanticType != null)
                Push(builder, line, col, len, semanticType, modifier);
        }

        return Task.CompletedTask;
    }

    internal static IReadOnlyList<ClassifiedSemanticToken> GetClassifiedTokens(string text)
    {
        TokenList<PreceptToken> tokens;
        try
        {
            tokens = PreceptTokenizerBuilder.Instance.Tokenize(text);
        }
        catch
        {
            return [];
        }

        return ClassifyTokens(tokens, text)
            .Where(x => x.SemanticType != null)
            .Select(x => new ClassifiedSemanticToken(x.Token.ToStringValue(), x.SemanticType!, x.Modifier))
            .ToList();
    }

    private static IEnumerable<(Token<PreceptToken> Token, string? SemanticType, string? Modifier)> ClassifyTokens(
        TokenList<PreceptToken> tokens,
        string text)
    {
        var (constrainedStates, constrainedEvents, guardedFields) = BuildConstraintSets(text);

        PreceptToken? previousKind = null;
        var declContext = DeclContext.None;

        foreach (var token in tokens)
        {
            if (token.Kind == PreceptToken.State)
                declContext = DeclContext.State;
            else if (token.Kind == PreceptToken.Event)
                declContext = DeclContext.Event;
            else if (token.Kind == PreceptToken.Field)
                declContext = DeclContext.Field;
            else if (token.Kind == PreceptToken.NewLine)
                declContext = DeclContext.None;
            else if (token.Kind == PreceptToken.With)
                declContext = DeclContext.None;
            else if (token.Kind == PreceptToken.As)
                declContext = DeclContext.None;

            string? semanticType = null;
            string? modifier = null;

            if (SemanticTypeMap.TryGetValue(token.Kind, out var mapped))
            {
                semanticType = mapped;
            }
            else if (token.Kind == PreceptToken.Identifier)
            {
                semanticType = ClassifyIdentifier(previousKind, declContext);
                if (semanticType != null)
                {
                    var tokenText = token.ToStringValue();
                    if ((semanticType == "preceptState" && constrainedStates.Contains(tokenText)) ||
                        (semanticType == "preceptEvent" && constrainedEvents.Contains(tokenText)) ||
                        (semanticType == "preceptFieldName" && guardedFields.Contains(tokenText)))
                    {
                        modifier = "preceptConstrained";
                    }
                }
            }
            else if (token.Kind == PreceptToken.StringLiteral)
            {
                semanticType = previousKind is PreceptToken.Because or PreceptToken.Reject
                    ? "preceptMessage"
                    : "preceptValue";
            }
            else if (token.Kind == PreceptToken.NumberLiteral)
            {
                semanticType = "preceptValue";
            }

            yield return (token, semanticType, modifier);

            if (token.Kind != PreceptToken.NewLine)
                previousKind = token.Kind;
        }
    }

    /// <summary>
    /// Parses the document text and extracts constraint sets for italic modifier emission.
    /// Fails open — returns empty sets if the parse is incomplete or fails.
    /// </summary>
    internal static (HashSet<string> States, HashSet<string> Events, HashSet<string> Fields) BuildConstraintSets(string text)
    {
        try
        {
            var (definition, parseDiags) = PreceptParser.ParseWithDiagnostics(text);
            if (definition is null || parseDiags.Count > 0)
                return ([], [], []);
            return ExtractConstraintSets(definition);
        }
        catch
        {
            return ([], [], []);
        }
    }

    /// <summary>
    /// Extracts the three constraint sets from a fully parsed <see cref="PreceptDefinition"/>.
    /// </summary>
    internal static (HashSet<string> States, HashSet<string> Events, HashSet<string> Fields) ExtractConstraintSets(PreceptDefinition definition)
    {
        var states = new HashSet<string>(StringComparer.Ordinal);
        var events = new HashSet<string>(StringComparer.Ordinal);
        var fields = new HashSet<string>(StringComparer.Ordinal);

        if (definition.StateAsserts != null)
            foreach (var sa in definition.StateAsserts)
                states.Add(sa.State);

        if (definition.EventAsserts != null)
            foreach (var ea in definition.EventAsserts)
                events.Add(ea.EventName);

        if (definition.Invariants != null)
            foreach (var inv in definition.Invariants)
                CollectFieldNames(inv.Expression, fields);

        return (states, events, fields);
    }

    private static void CollectFieldNames(PreceptExpression expr, HashSet<string> fields)
    {
        switch (expr)
        {
            case PreceptIdentifierExpression { Member: null } id:
                fields.Add(id.Name);
                break;
            case PreceptBinaryExpression bin:
                CollectFieldNames(bin.Left, fields);
                CollectFieldNames(bin.Right, fields);
                break;
            case PreceptUnaryExpression unary:
                CollectFieldNames(unary.Operand, fields);
                break;
            case PreceptParenthesizedExpression paren:
                CollectFieldNames(paren.Inner, fields);
                break;
        }
    }

    /// <summary>
    /// Classifies an identifier token based on the preceding token:
    /// - After precept → "preceptMessage" (gold — the contract name)
    /// - After state/from/transition/in/to → "preceptState"
    /// - After event/on → "preceptEvent"
    /// - After field/set/add/remove/.../into → "preceptFieldName"
    /// - After dot → "preceptFieldName" (member access like Collection.count)
    /// - Otherwise → "preceptFieldName" (bare identifier in expression)
    /// </summary>
    private static string ClassifyIdentifier(PreceptToken? previousKind, DeclContext context) => previousKind switch
    {
        PreceptToken.Precept => "preceptMessage",
        PreceptToken.From => "preceptState",
        PreceptToken.Dot => "preceptFieldName",
        PreceptToken.Comma when context == DeclContext.State => "preceptState",
        PreceptToken.Comma when context == DeclContext.Event => "preceptEvent",
        PreceptToken.Comma when context == DeclContext.Field => "preceptFieldName",
        PreceptToken.Comma => "preceptFieldName",
        _ when previousKind.HasValue && StateContextTokens.Contains(previousKind.Value) => "preceptState",
        _ when previousKind.HasValue && EventContextTokens.Contains(previousKind.Value) => "preceptEvent",
        _ when previousKind.HasValue && FieldContextTokens.Contains(previousKind.Value) => "preceptFieldName",
        PreceptToken.Edit => "preceptFieldName",
        _ => "preceptFieldName" // default: bare identifier in expression position
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

    private static void Push(SemanticTokensBuilder builder, int line, int character, int length, string tokenType, string? modifier = null)
    {
        if (length <= 0) return;
        builder.Push(line, character, length, tokenType, modifier != null ? [modifier] : Array.Empty<string>());
    }
}
