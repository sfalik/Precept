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

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        await RecompileAndPublishAsync(request.TextDocument.Uri, request.TextDocument.Version, request.TextDocument.Text, cancellationToken)
            .ConfigureAwait(false);
        return Unit.Value;
    }

    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var text = request.ContentChanges.FirstOrDefault()?.Text ?? string.Empty;
        await RecompileAndPublishAsync(request.TextDocument.Uri, request.TextDocument.Version, text, cancellationToken)
            .ConfigureAwait(false);
        return Unit.Value;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _store.Remove(request.TextDocument.Uri);
        PublishDiagnostics(request.TextDocument.Uri, []);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken) => Unit.Task;

    // RecompileAndPublishAsync hops the CPU-bound compile to a thread-pool task so it
    // does not block the LSP dispatch thread. Holding the dispatch thread synchronously
    // prevents OmniSharp from pumping the wire writer, which deadlocks subsequent
    // `PublishDiagnostics` notifications (F-LS-02 / DiagnosticPublishIntegrationTests).
    private async Task RecompileAndPublishAsync(DocumentUri uri, int? version, string text, CancellationToken cancellationToken)
    {
        var (compilation, enrichedDiagnostics, suggestions) = await Task.Run(() =>
        {
            var c = Precept.Compiler.Compile(text);
            var (d, s) = DiagnosticEnricher.Enrich(c);
            return (c, d, s);
        }, cancellationToken).ConfigureAwait(false);

        var state = _store.GetOrAdd(uri);
        if (version is null)
        {
            state.Update(compilation, suggestions, text);
            PublishDiagnostics(uri, enrichedDiagnostics);
            return;
        }

        if (state.TryUpdate(version.Value, compilation, suggestions, text))
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
