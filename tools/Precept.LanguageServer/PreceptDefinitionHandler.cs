using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer;

internal sealed class PreceptDefinitionHandler : IDefinitionHandler
{
    private static readonly TextDocumentSelector Selector = new(new TextDocumentFilter
    {
        Pattern = "**/*.precept",
        Language = "precept"
    });

    public Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        if (!PreceptTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(request.TextDocument.Uri, out var text))
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());

        var info = PreceptDocumentIntellisense.Analyze(text);
        return Task.FromResult<LocationOrLocationLinks?>(PreceptDocumentIntellisense.CreateDefinition(request.TextDocument.Uri, info, request.Position));
    }

    public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = Selector
        };
}