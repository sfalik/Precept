using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer;

internal sealed class PreceptDocumentSymbolHandler : IDocumentSymbolHandler
{
    private static readonly TextDocumentSelector Selector = TextDocumentSelector.ForPattern("**/*.precept");

    public Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        if (!PreceptTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(request.TextDocument.Uri, out var text))
            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(new SymbolInformationOrDocumentSymbolContainer());

        var info = PreceptDocumentIntellisense.Analyze(text);
        return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(PreceptDocumentIntellisense.CreateDocumentSymbols(info));
    }

    public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = Selector,
            Label = "Precept"
        };
}