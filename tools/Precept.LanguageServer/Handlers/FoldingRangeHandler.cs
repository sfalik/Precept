using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class FoldingRangeHandler : IFoldingRangeHandler
{
    private readonly DocumentStore _store;

    public FoldingRangeHandler(DocumentStore store)
    {
        _store = store;
    }

    public FoldingRangeRegistrationOptions GetRegistrationOptions(FoldingRangeCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public Task<Container<FoldingRange>?> Handle(FoldingRangeRequestParam request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
            return Task.FromResult<Container<FoldingRange>?>(null);

        var ranges = GetFoldingRanges(state.Current);
        return Task.FromResult<Container<FoldingRange>?>(new Container<FoldingRange>(ranges));
    }

    internal static FoldingRange[] GetFoldingRanges(Compilation compilation) =>
        compilation.ConstructManifest.Constructs
            .Where(construct => construct.Span.EndLine > construct.Span.StartLine)
            .Select(construct => new FoldingRange
            {
                StartLine = construct.Span.StartLine - 1,
                EndLine = construct.Span.EndLine - 1,
                Kind = FoldingRangeKind.Region,
            })
            .ToArray();
}
