using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class DefinitionHandler : IDefinitionHandler
{
    private readonly DocumentStore _store;

    public DefinitionHandler(DocumentStore store)
    {
        _store = store;
    }

    public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public void SetCapability(DefinitionCapability capability)
    {
    }

    public Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        return Task.FromResult<LocationOrLocationLinks?>(HandleDefinition(request.TextDocument.Uri, state.Current, request.Position));
    }

    internal static LocationOrLocationLinks HandleDefinition(DocumentUri uri, Compilation compilation, Position position)
    {
        return SymbolNavigation.TryFindOccurrence(compilation, position, out var occurrence)
            ? ToLocationLinks(uri, occurrence.DeclarationSpan)
            : new LocationOrLocationLinks();
    }

    private static LocationOrLocationLinks ToLocationLinks(DocumentUri uri, SourceSpan span) =>
        new(new LocationOrLocationLink(new Location
        {
            Uri = uri,
            Range = DiagnosticProjector.ToRange(span),
        }));
}
