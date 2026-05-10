using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;
using Precept.LanguageServer.Handlers;

namespace Precept.LanguageServer;

internal static class LanguageServerComposition
{
    internal static LanguageServerOptions ConfigurePreceptLanguageServer(this LanguageServerOptions options)
    {
        return options
            .WithServices(services => services.AddSingleton<DocumentStore>())
            .WithHandler<TextDocumentSyncHandler>()
            .WithHandler<SemanticTokensHandler>()
            .WithHandler<CompletionHandler>()
            .WithHandler<SignatureHelpHandler>()
            .WithHandler<HoverHandler>()
            .WithHandler<DefinitionHandler>()
            .WithHandler<ReferencesHandler>()
            .WithHandler<DocumentHighlightHandler>()
            .WithHandler<RenameHandler>()
            .WithHandler<DocumentSymbolHandler>()
            .WithHandler<WorkspaceSymbolHandler>()
            .WithHandler<SelectionRangeHandler>()
            .WithHandler<FoldingRangeHandler>()
            .WithHandler<CodeActionHandler>();
    }
}
