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
                .ConfigurePreceptLanguageServer();
        });

        SemanticTokensHandler.SendColorNotification(server);
        await server.WaitForExit;
    }
}
