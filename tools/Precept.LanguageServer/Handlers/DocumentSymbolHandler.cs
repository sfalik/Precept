using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class DocumentSymbolHandler : IDocumentSymbolHandler
{
    private readonly DocumentStore _store;

    public DocumentSymbolHandler(DocumentStore store)
    {
        _store = store;
    }

    public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
        };

    public void SetCapability(DocumentSymbolCapability capability)
    {
    }

    public Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(new SymbolInformationOrDocumentSymbolContainer());
        }

        return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(BuildDocumentSymbols(state.Current));
    }

    internal static SymbolInformationOrDocumentSymbolContainer BuildDocumentSymbols(Compilation compilation) =>
        OutlineSymbolProjector.BuildDocumentSymbols(compilation);
}
