using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;
using Precept.LanguageServer.Handlers;

namespace Precept.LanguageServer;

internal static class Program
{
    public static async Task Main()
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
        {
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithServices(services =>
                {
                    services.AddSingleton<DocumentStore>();
                })
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<SemanticTokensHandler>()
                .WithHandler<CompletionHandler>()
                .WithHandler<HoverHandler>()
                .WithHandler<DefinitionHandler>()
                .WithHandler<DocumentSymbolHandler>()
                .WithHandler<FoldingRangeHandler>()
                .WithHandler<CodeActionHandler>();
        });

        await server.WaitForExit;
    }
}
