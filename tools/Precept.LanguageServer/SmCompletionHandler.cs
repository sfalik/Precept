using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer;

internal sealed class SmCompletionHandler : CompletionHandlerBase
{
    private static readonly TextDocumentSelector Selector = TextDocumentSelector.ForPattern("**/*.sm");

    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var items = SmTextDocumentSyncHandler.SharedAnalyzer.GetCompletions(request.TextDocument.Uri, request.Position)
            .ToArray();

        return Task.FromResult(new CompletionList(items, isIncomplete: false));
    }

    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        => Task.FromResult(request);

    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = Selector,
            ResolveProvider = false,
            TriggerCharacters = new Container<string>(" ", ".")
        };
}
