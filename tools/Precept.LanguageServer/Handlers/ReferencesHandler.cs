using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer.Handlers;

internal sealed class ReferencesHandler : IReferencesHandler
{
    private readonly DocumentStore _store;

    public ReferencesHandler(DocumentStore store)
    {
        _store = store;
    }

    public ReferenceRegistrationOptions GetRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public void SetCapability(ReferenceCapability capability)
    {
    }

    public Task<LocationContainer?> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<LocationContainer?>(new LocationContainer());
        }

        return Task.FromResult<LocationContainer?>(HandleReferences(
            request.TextDocument.Uri,
            state.Current,
            request.Position,
            request.Context.IncludeDeclaration));
    }

    internal static LocationContainer HandleReferences(DocumentUri uri, Precept.Pipeline.Compilation compilation, Position position, bool includeDeclaration)
    {
        if (!SymbolNavigation.TryFindOccurrence(compilation, position, out var occurrence))
        {
            return new LocationContainer();
        }

        return new LocationContainer(SymbolNavigation
            .GetReferenceSpans(compilation.Semantics, occurrence, includeDeclaration)
            .Select(span => new Location
            {
                Uri = uri,
                Range = DiagnosticProjector.ToRange(span),
            }));
    }
}
