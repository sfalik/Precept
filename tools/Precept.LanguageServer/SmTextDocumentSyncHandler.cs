using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Precept.LanguageServer;

internal sealed class SmTextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    internal static readonly SmDslAnalyzer SharedAnalyzer = new();

    private static readonly TextDocumentSelector Selector = TextDocumentSelector.ForPattern("**/*.sm");

    private readonly ILanguageServerFacade _router;

    public SmTextDocumentSyncHandler(ILanguageServerFacade router)
    {
        _router = router;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        => new(uri, "sm");

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        SharedAnalyzer.SetDocumentText(request.TextDocument.Uri, request.TextDocument.Text);
        PublishDiagnostics(request.TextDocument.Uri);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var text = request.ContentChanges.LastOrDefault()?.Text ?? string.Empty;
        SharedAnalyzer.SetDocumentText(request.TextDocument.Uri, text);
        PublishDiagnostics(request.TextDocument.Uri);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        SharedAnalyzer.RemoveDocument(request.TextDocument.Uri);
        _router.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = request.TextDocument.Uri,
            Diagnostics = new Container<Diagnostic>()
        });
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (request.Text is not null)
            SharedAnalyzer.SetDocumentText(request.TextDocument.Uri, request.Text);

        PublishDiagnostics(request.TextDocument.Uri);
        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = Selector,
            Change = TextDocumentSyncKind.Full,
            Save = new BooleanOr<SaveOptions>(new SaveOptions { IncludeText = true })
        };

    private void PublishDiagnostics(DocumentUri uri)
    {
        _router.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = new Container<Diagnostic>(SharedAnalyzer.GetDiagnostics(uri))
        });
    }
}
