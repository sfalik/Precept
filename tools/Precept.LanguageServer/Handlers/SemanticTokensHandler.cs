using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Compilation = Precept.Pipeline.Compilation;
using TokenKind = Precept.Language.TokenKind;
using TokensCatalog = Precept.Language.Tokens;

namespace Precept.LanguageServer.Handlers;

internal sealed class SemanticTokensHandler : SemanticTokensHandlerBase
{
    private static readonly SemanticTokensLegend Legend = BuildLegend();

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

        foreach (var token in ProjectLexicalTokens(compilation))
        {
            builder.Push(token.Line, token.Character, token.Length, token.TokenType);
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
            if (meta.SemanticTokenType is null)
            {
                continue;
            }

            projected.Add(new LexicalSemanticToken(
                token.Kind,
                token.Span.StartLine - 1,
                token.Span.StartColumn - 1,
                token.Span.Length,
                meta.SemanticTokenType));
        }

        return projected.ToImmutable();
    }

    private static SemanticTokensLegend BuildLegend() =>
        new()
        {
            TokenTypes = new Container<SemanticTokenType>(
                TokensCatalog.All
                    .Select(static meta => meta.SemanticTokenType)
                    .Where(static type => type is not null)
                    .Distinct()
                    .Select(static type => new SemanticTokenType(type!))
                    .ToArray()),
            TokenModifiers = new Container<SemanticTokenModifier>(),
        };

    internal readonly record struct LexicalSemanticToken(
        TokenKind Kind,
        int Line,
        int Character,
        int Length,
        string TokenType);
}
