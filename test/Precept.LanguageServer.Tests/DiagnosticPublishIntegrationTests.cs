using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
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
}
