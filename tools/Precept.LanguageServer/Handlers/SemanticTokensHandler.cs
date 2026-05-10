using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Precept.Language;
using Compilation = Precept.Pipeline.Compilation;
using SemanticTokenTypesCatalog = Precept.Language.SemanticTokenTypes;
using TokenKind = Precept.Language.TokenKind;
using TokensCatalog = Precept.Language.Tokens;

namespace Precept.LanguageServer.Handlers;

internal sealed class SemanticTokensHandler : SemanticTokensHandlerBase
{
    private static readonly SemanticTokensLegend Legend = BuildLegend();
    internal const string SemanticTokenColorsNotificationName = "precept/semanticTokenColors";

    private readonly ConcurrentDictionary<DocumentUri, SemanticTokensDocument> _documents = new();
    private readonly DocumentStore _store;

    public SemanticTokensHandler(DocumentStore store)
    {
        _store = store;
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            Legend = Legend,
            Full = new SemanticTokensCapabilityRequestFull
            {
                Delta = true,
            },
            Range = new SemanticTokensCapabilityRequestRange(),
        };

    protected override Task Tokenize(
        SemanticTokensBuilder builder,
        ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        if (!_store.TryGet(identifier.TextDocument.Uri, out var state) || state.Current is not { } compilation)
        {
            return Task.CompletedTask;
        }

        var lexicalTokens = ProjectLexicalTokens(compilation);
        var identifierTokens = ProjectIdentifierTokens(compilation.Semantics);

        foreach (var projected in lexicalTokens
                     .Select(static token => (Token: token, OverlayOrder: 0))
                     .Concat(identifierTokens.Select(static token => (Token: token, OverlayOrder: 1)))
                     .OrderBy(static entry => entry.Token.Line)
                     .ThenBy(static entry => entry.Token.Character)
                     .ThenBy(static entry => entry.OverlayOrder)
                     .ThenBy(static entry => entry.Token.Length))
        {
            builder.Push(
                projected.Token.Line,
                projected.Token.Character,
                projected.Token.Length,
                projected.Token.TokenType);
        }

        return Task.CompletedTask;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params,
        CancellationToken cancellationToken) =>
        Task.FromResult(_documents.GetOrAdd(@params.TextDocument.Uri, static _ => new SemanticTokensDocument(Legend)));

    internal static ImmutableArray<LexicalSemanticToken> ProjectLexicalTokens(Compilation compilation)
    {
        var projected = ImmutableArray.CreateBuilder<LexicalSemanticToken>();

        foreach (var token in compilation.Tokens.Tokens)
        {
            var meta = TokensCatalog.GetMeta(token.Kind);
            if (!meta.VisualCategory.HasValue)
            {
                continue;
            }

            // Contextual reclassification: 'set' emits the type custom token in type-expression position.
            // SetType.VisualCategory is intentionally null (parser-synthesized; never in the lexer stream),
            // so we derive the type token from the catalog rather than the synthetic token kind.
            var effectiveTokenType = token.Kind == TokenKind.Set && SlotContextResolver.IsSetInTypePosition(compilation, token)
                ? SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.Type).CustomType
                : SemanticTokenTypesCatalog.GetMeta(meta.VisualCategory.Value).CustomType;

            projected.Add(new LexicalSemanticToken(
                token.Kind,
                token.Span.StartLine - 1,
                token.Span.StartColumn - 1,
                token.Span.Length,
                effectiveTokenType));
        }

        return projected.ToImmutable();
    }

    internal static ImmutableArray<LexicalSemanticToken> ProjectIdentifierTokens(Precept.Pipeline.SemanticIndex index)
    {
        var projected = ImmutableArray.CreateBuilder<LexicalSemanticToken>();

        foreach (var field in index.Fields)
        {
            var ns = field.NameSpan;
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ns.StartLine - 1,
                ns.StartColumn - 1,
                field.Name.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.FieldName).CustomType));
        }

        foreach (var state in index.States)
        {
            var ns = state.NameSpan;
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ns.StartLine - 1,
                ns.StartColumn - 1,
                state.Name.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.State).CustomType));
        }

        foreach (var evt in index.Events)
        {
            var ns = evt.NameSpan;
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ns.StartLine - 1,
                ns.StartColumn - 1,
                evt.Name.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.Event).CustomType));

            foreach (var arg in evt.Args)
            {
                projected.Add(new LexicalSemanticToken(
                    TokenKind.Identifier,
                    arg.Span.StartLine - 1,
                    arg.Span.StartColumn - 1,
                    arg.Name.Length,
                    SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.ArgName).CustomType));
            }
        }

        foreach (var fr in index.FieldReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                fr.Site.StartLine - 1,
                fr.Site.StartColumn - 1,
                fr.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.FieldName).CustomType));
        }

        foreach (var sr in index.StateReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                sr.Site.StartLine - 1,
                sr.Site.StartColumn - 1,
                sr.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.State).CustomType));
        }

        foreach (var er in index.EventReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                er.Site.StartLine - 1,
                er.Site.StartColumn - 1,
                er.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.Event).CustomType));
        }

        foreach (var ar in index.ArgReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ar.Site.StartLine - 1,
                ar.Site.StartColumn - 1,
                ar.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.ArgName).CustomType));
        }

        return projected.ToImmutable();
    }

    internal static void AddIdentifierTokens(SemanticTokensBuilder builder, Precept.Pipeline.SemanticIndex index)
    {
        foreach (var token in ProjectIdentifierTokens(index))
        {
            builder.Push(token.Line, token.Character, token.Length, token.TokenType);
        }
    }

    internal static SemanticTokensLegend BuildLegend()
    {
        return new SemanticTokensLegend
        {
            TokenTypes = new Container<SemanticTokenType>(
                SemanticTokenTypesCatalog.All
                    .Select(static m => new SemanticTokenType(m.CustomType))
                    .ToArray()),
            TokenModifiers = new Container<SemanticTokenModifier>(
                new SemanticTokenModifier("preceptConstrained")),
        };
    }

    internal static ImmutableArray<SemanticTokenColorRule> BuildColorNotificationPayload() =>
        SemanticTokenTypesCatalog.All
            .Select(static meta => new SemanticTokenColorRule(
                meta.CustomType,
                meta.ForegroundHex,
                meta.Bold,
                meta.Italic))
            .ToImmutableArray();

    internal static void SendColorNotification(OmniSharp.Extensions.LanguageServer.Server.LanguageServer server) =>
        SendColorNotification((method, payload) => server.SendNotification(method, payload));

    internal static void SendColorNotification(Action<string, SemanticTokenColorRule[]> sendNotification) =>
        sendNotification(SemanticTokenColorsNotificationName, BuildColorNotificationPayload().ToArray());

    internal readonly record struct LexicalSemanticToken(
        TokenKind Kind,
        int Line,
        int Character,
        int Length,
        string TokenType);

    internal readonly record struct SemanticTokenColorRule(
        [property: JsonPropertyName("tokenType")] string TokenType,
        [property: JsonPropertyName("hexColor")] string HexColor,
        [property: JsonPropertyName("bold")] bool Bold,
        [property: JsonPropertyName("italic")] bool Italic);
}
