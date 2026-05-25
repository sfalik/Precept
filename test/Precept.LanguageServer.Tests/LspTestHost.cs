using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Precept.LanguageServer.Tests;

/// <summary>
/// Spins up an in-process LS server + client pair for protocol-layer tests.
/// Dispose to shut down cleanly.
/// </summary>
public sealed class LspTestHost : IAsyncDisposable
{
    private readonly ILanguageServer _server;
    private readonly LanguageClient _client;
    private readonly ConcurrentDictionary<DocumentUri, TaskCompletionSource<PublishDiagnosticsParams>> _diagWaiters;

    private LspTestHost(
        ILanguageServer server,
        LanguageClient client,
        ConcurrentDictionary<DocumentUri, TaskCompletionSource<PublishDiagnosticsParams>> diagWaiters)
    {
        _server = server;
        _client = client;
        _diagWaiters = diagWaiters;
    }

    public ILanguageClient Client => _client;

    public ServerCapabilities ServerCapabilities => _client.ServerSettings.Capabilities;

    public Task<PublishDiagnosticsParams> WhenPublishDiagnosticsAsync(DocumentUri uri, CancellationToken cancellationToken = default)
    {
        var tcs = _diagWaiters.GetOrAdd(
            uri,
            static _ => new TaskCompletionSource<PublishDiagnosticsParams>(TaskCreationOptions.RunContinuationsAsynchronously));

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                if (_diagWaiters.TryRemove(uri, out var waiter))
                {
                    waiter.TrySetCanceled(cancellationToken);
                    return;
                }

                tcs.TrySetCanceled(cancellationToken);
            });
        }

        return tcs.Task;
    }

    public static async Task<LspTestHost> CreateAsync(CancellationToken cancellationToken = default)
    {
        var (serverInput, clientOutput) = CreatePipePair();
        var (clientInput, serverOutput) = CreatePipePair();

        // DocumentUri.Equals is case-sensitive on Linux (OrdinalIgnoreCase only on
        // Windows/macOS). The test contracts use Windows-style URIs like
        // "file:///C:/Users/..." which JSON-RPC serialization round-trips with a
        // lower-cased drive letter ("file:///c:/Users/..."). Use a case-insensitive
        // comparer so the waiter dictionary matches client-side URIs the way LSP
        // clients on Windows/macOS would.
        var diagWaiters = new ConcurrentDictionary<DocumentUri, TaskCompletionSource<PublishDiagnosticsParams>>(CaseInsensitiveDocumentUriComparer.Instance);

        var server = OmniSharp.Extensions.LanguageServer.Server.LanguageServer.PreInit(options =>
        {
            options
                .WithInput(serverInput)
                .WithOutput(serverOutput)
                .ConfigurePreceptLanguageServer();
        });

        var client = LanguageClient.PreInit(options =>
        {
            options
                .WithInput(clientInput)
                .WithOutput(clientOutput)
                .WithRootPath(AppContext.BaseDirectory)
                .OnPublishDiagnostics(@params =>
                {
                    if (diagWaiters.TryRemove(@params.Uri, out var waiter))
                    {
                        waiter.TrySetResult(@params);
                    }
                });
        });

        await Task.WhenAll(
            server.Initialize(cancellationToken),
            client.Initialize(cancellationToken));

        return new LspTestHost(server, client, diagWaiters);
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

    private sealed class CaseInsensitiveDocumentUriComparer : IEqualityComparer<DocumentUri>
    {
        public static CaseInsensitiveDocumentUriComparer Instance { get; } = new();

        public bool Equals(DocumentUri? x, DocumentUri? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return string.Equals(x.Scheme, y.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Authority, y.Authority, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Query, y.Query, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Fragment, y.Fragment, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(DocumentUri obj)
        {
            var c = StringComparer.OrdinalIgnoreCase;
            return HashCode.Combine(
                obj.Scheme is null ? 0 : c.GetHashCode(obj.Scheme),
                c.GetHashCode(obj.Authority),
                c.GetHashCode(obj.Path),
                c.GetHashCode(obj.Query),
                c.GetHashCode(obj.Fragment));
        }
    }
}
