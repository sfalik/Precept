using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Precept.LanguageServer.Handlers;

namespace Precept.LanguageServer.Tests;

/// <summary>
/// Spins up an in-process LS server + client pair for protocol-layer tests.
/// Dispose to shut down cleanly.
/// </summary>
public sealed class LspTestHost : IAsyncDisposable
{
    private readonly ILanguageServer _server;
    private readonly ILanguageClient _client;

    private LspTestHost(ILanguageServer server, ILanguageClient client)
    {
        _server = server;
        _client = client;
    }

    public ILanguageClient Client => _client;

    public static async Task<LspTestHost> CreateAsync(CancellationToken cancellationToken = default)
    {
        var (serverInput, clientOutput) = CreatePipePair();
        var (clientInput, serverOutput) = CreatePipePair();

        var server = OmniSharp.Extensions.LanguageServer.Server.LanguageServer.PreInit(options =>
        {
            options
                .WithInput(serverInput)
                .WithOutput(serverOutput)
                .WithServices(services => services.AddSingleton<DocumentStore>())
                .WithHandler<TextDocumentSyncHandler>();
        });

        var client = LanguageClient.PreInit(options =>
        {
            options
                .WithInput(clientInput)
                .WithOutput(clientOutput)
                .WithRootPath(AppContext.BaseDirectory);
        });

        await Task.WhenAll(
            server.Initialize(cancellationToken),
            client.Initialize(cancellationToken));

        return new LspTestHost(server, client);
    }

    private static (PipeReader reader, PipeWriter writer) CreatePipePair()
    {
        var pipe = new Pipe();
        return (pipe.Reader, pipe.Writer);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.Shutdown();
        _client.Dispose();
        _server.Dispose();
    }
}
