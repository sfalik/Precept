using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer.Handlers;

internal sealed class DocumentHighlightHandler : IDocumentHighlightHandler
{
    private readonly DocumentStore _store;

    public DocumentHighlightHandler(DocumentStore store)
    {
        _store = store;
    }

    public DocumentHighlightRegistrationOptions GetRegistrationOptions(DocumentHighlightCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public void SetCapability(DocumentHighlightCapability capability)
    {
    }

    public Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<DocumentHighlightContainer?>(new DocumentHighlightContainer());
        }

        return Task.FromResult<DocumentHighlightContainer?>(HandleHighlights(state.Current, request.Position));
    }

    internal static DocumentHighlightContainer HandleHighlights(Precept.Pipeline.Compilation compilation, Position position)
    {
        if (!SymbolNavigation.TryFindOccurrence(compilation, position, out var occurrence))
        {
            return new DocumentHighlightContainer();
        }

        return new DocumentHighlightContainer(SymbolNavigation
            .GetReferenceSpans(compilation.Semantics, occurrence, includeDeclaration: true)
            .Select(span => new DocumentHighlight
            {
                Range = DiagnosticProjector.ToRange(span),
                Kind = DocumentHighlightKind.Text,
            }));
    }
}
