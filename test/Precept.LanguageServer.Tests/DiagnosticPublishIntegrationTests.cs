using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class DiagnosticPublishIntegrationTests
{
    [Fact]
    public async Task DidOpen_InvalidSource_PublishesDiagnostics()
    {
        await using var host = await LspTestHost.CreateAsync();
        var uri = DocumentUri.FromFileSystemPath(@"C:\Users\Shane.Falik\source\repos\precept-architecture\test\Precept.LanguageServer.Tests\invalid-diagnostics.precept");
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var publishedDiagnosticsTask = host.WhenPublishDiagnosticsAsync(uri, cancellationTokenSource.Token);

        host.Client.TextDocument.DidOpenTextDocument(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                LanguageId = "precept",
                Version = 1,
                Text = """
                    precept Broken
                    field Quantity as UnknownType
                    state Pending initial terminal
                    """,
            },
        });

        var publishedDiagnostics = await publishedDiagnosticsTask;

        publishedDiagnostics.Uri.Should().Be(uri);
        publishedDiagnostics.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DidClose_OpenDocument_PublishesEmptyDiagnosticsForUri()
    {
        await using var host = await LspTestHost.CreateAsync();
        var uri = DocumentUri.FromFileSystemPath(@"C:\Users\Shane.Falik\source\repos\precept-architecture\test\Precept.LanguageServer.Tests\close-diagnostics.precept");
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var initialDiagnosticsTask = host.WhenPublishDiagnosticsAsync(uri, cancellationTokenSource.Token);
        host.Client.TextDocument.DidOpenTextDocument(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                LanguageId = "precept",
                Version = 1,
                Text = """
                    precept Broken
                    field Quantity as UnknownType
                    state Pending initial terminal
                    """,
            },
        });

        (await initialDiagnosticsTask).Diagnostics.Should().NotBeEmpty();

        var closeDiagnosticsTask = host.WhenPublishDiagnosticsAsync(uri, cancellationTokenSource.Token);
        host.Client.TextDocument.DidCloseTextDocument(new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
        });

        var closedDiagnostics = await closeDiagnosticsTask;

        closedDiagnostics.Uri.Should().Be(uri);
        closedDiagnostics.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void DocumentStore_Remove_RemovesStoredState()
    {
        var store = new DocumentStore();
        var uri = DocumentUri.FromFileSystemPath(@"C:\document-store.precept");

        store.GetOrAdd(uri).Update(Compiler.Compile("precept Order"));
        store.Remove(uri);

        store.TryGet(uri, out _).Should().BeFalse();
    }

    [Fact]
    public async Task DocumentState_Update_ConcurrentUpdates_LeavesValidCurrentCompilation()
    {
        var state = new DocumentState();
        var compilations = Enumerable.Range(1, 64)
            .Select(index => Compiler.Compile($$"""
                precept Order{{index}}
                state Draft initial
                """))
            .ToArray();

        await Task.WhenAll(compilations.Select(compilation => Task.Run(() => state.Update(compilation))));

        state.Current.Should().NotBeNull();
        compilations.Should().Contain(state.Current!);
        state.Current!.Tokens.Should().NotBeNull();
    }

    [Fact]
    public async Task DidChange_OutOfOrderVersions_PublishesNewestDiagnosticsOnly()
    {
        await using var host = await LspTestHost.CreateAsync();
        var uri = DocumentUri.FromFileSystemPath(@"C:\Users\Shane.Falik\source\repos\precept-architecture\test\Precept.LanguageServer.Tests\out-of-order-diagnostics.precept");
        using var publishTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var initialDiagnosticsTask = host.WhenPublishDiagnosticsAsync(uri, publishTimeout.Token);
        host.Client.TextDocument.DidOpenTextDocument(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                LanguageId = "precept",
                Version = 1,
                Text = """
                    precept Order
                    field Quantity as number optional
                    state Draft initial terminal
                    """,
            },
        });

        (await initialDiagnosticsTask).Diagnostics.Should().BeEmpty();

        var newestDiagnosticsTask = host.WhenPublishDiagnosticsAsync(uri, publishTimeout.Token);
        host.Client.TextDocument.DidChangeTextDocument(CreateDidChangeParams(
            uri,
            version: 3,
            """
            precept Order
            field Quantity as UnknownType
            state Draft initial terminal
            """));

        var newestDiagnostics = await newestDiagnosticsTask;
        newestDiagnostics.Uri.Should().Be(uri);
        newestDiagnostics.Diagnostics.Should().NotBeEmpty();
        newestDiagnostics.Diagnostics.Should().Contain(diagnostic => diagnostic.Message.Contains("UnknownType", StringComparison.Ordinal));

        using var stalePublishTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var stalePublishTask = host.WhenPublishDiagnosticsAsync(uri, stalePublishTimeout.Token);
        host.Client.TextDocument.DidChangeTextDocument(CreateDidChangeParams(
            uri,
            version: 2,
            """
            precept Order
            field Quantity as number optional
            state Draft initial terminal
            """));

        Func<Task> awaitStalePublish = async () => await stalePublishTask;
        await awaitStalePublish.Should().ThrowAsync<TaskCanceledException>();

        var finalDiagnosticsTask = host.WhenPublishDiagnosticsAsync(uri, publishTimeout.Token);
        host.Client.TextDocument.DidChangeTextDocument(CreateDidChangeParams(
            uri,
            version: 4,
            """
            precept Order
            field Quantity as number optional
            state Draft initial terminal
            """));

        (await finalDiagnosticsTask).Diagnostics.Should().BeEmpty();
    }

    private static DidChangeTextDocumentParams CreateDidChangeParams(DocumentUri uri, int version, string text) =>
        new()
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = uri,
                Version = version,
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent
                {
                    Text = text,
                }),
        };
}
