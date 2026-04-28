using OmniSharp.Extensions.LanguageServer.Server;

namespace Precept.LanguageServer;

internal static class Program
{
    public static async Task Main()
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
        {
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput());
        });

        await server.WaitForExit;
    }
}
