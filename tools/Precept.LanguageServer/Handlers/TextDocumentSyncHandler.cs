using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Precept.LanguageServer.Handlers;

internal sealed class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private readonly DocumentStore _store;
    private readonly ILanguageServerFacade _facade;

    public TextDocumentSyncHandler(DocumentStore store, ILanguageServerFacade facade)
    {
        _store = store;
        _facade = facade;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            Change = Change,
        };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) =>
        new(uri, "precept");

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        RecompileAndPublish(request.TextDocument.Uri, request.TextDocument.Version, request.TextDocument.Text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var text = request.ContentChanges.FirstOrDefault()?.Text ?? string.Empty;
        RecompileAndPublish(request.TextDocument.Uri, request.TextDocument.Version, text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _store.Remove(request.TextDocument.Uri);
        PublishDiagnostics(request.TextDocument.Uri, []);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken) => Unit.Task;

    private void RecompileAndPublish(DocumentUri uri, int? version, string text)
    {
        var compilation = Precept.Compiler.Compile(text);
        var (enrichedDiagnostics, suggestions) = DiagnosticEnricher.Enrich(compilation);

        var state = _store.GetOrAdd(uri);
        if (version is null)
        {
            state.Update(compilation, suggestions);
            PublishDiagnostics(uri, enrichedDiagnostics);
            return;
        }

        if (state.TryUpdate(version.Value, compilation, suggestions))
        {
            PublishDiagnostics(uri, enrichedDiagnostics);
        }
    }

    private void PublishDiagnostics(DocumentUri uri, IReadOnlyList<Diagnostic> diagnostics)
    {
        _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = new Container<Diagnostic>(diagnostics),
        });
    }
}
