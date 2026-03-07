using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer;

internal sealed class PreceptHoverHandler : IHoverHandler
{
    private static readonly TextDocumentSelector Selector = TextDocumentSelector.ForPattern("**/*.precept");

    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        if (!PreceptTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(request.TextDocument.Uri, out var text))
            return Task.FromResult<Hover?>(null);

        var info = PreceptDocumentIntellisense.Analyze(text);
        return Task.FromResult(PreceptDocumentIntellisense.CreateHover(info, request.Position));
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = Selector
        };
}