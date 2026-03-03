using OmniSharp.Extensions.LanguageServer.Server;

namespace StateMachine.Dsl.LanguageServer;

internal static class Program
{
    public static async Task Main()
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
        {
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithHandler<SmTextDocumentSyncHandler>()
                .WithHandler<SmCompletionHandler>()
                .WithHandler<SmSemanticTokensHandler>()
                .WithHandler<SmPreviewHandler>();
        });

        await server.WaitForExit;
    }
}
