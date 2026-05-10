using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class WorkspaceSymbolHandlerTests
{
    private static readonly DocumentUri FirstUri = DocumentUri.FromFileSystemPath(@"C:\workspace-symbol-first.precept");
    private static readonly DocumentUri SecondUri = DocumentUri.FromFileSystemPath(@"C:\workspace-symbol-second.precept");

    [Fact]
    public async Task WorkspaceSymbol_Query_ReturnsMatchingSymbolsAcrossOpenDocuments()
    {
        var store = new DocumentStore();
        store.GetOrAdd(FirstUri).Update(Precept.Compiler.Compile("""
            precept FirstOrder
            field SharedCount as number
            state Pending initial
            """));
        store.GetOrAdd(SecondUri).Update(Precept.Compiler.Compile("""
            precept SecondOrder
            event SharedApproval
            state Complete initial terminal
            """));

        var handler = new WorkspaceSymbolHandler(store);

        var symbols = await handler.Handle(new WorkspaceSymbolParams
        {
            Query = "shared",
        }, CancellationToken.None);

        symbols.Should().HaveCount(2);
        symbols.Select(symbol => symbol.Name).Should().BeEquivalentTo(["SharedCount", "SharedApproval"]);
        symbols.Select(symbol => symbol.Location.Location!.Uri).Should().BeEquivalentTo([FirstUri, SecondUri]);
    }

    [Fact]
    public async Task WorkspaceSymbol_Result_CarriesDocumentUriAndSymbolKind()
    {
        var store = new DocumentStore();
        store.GetOrAdd(FirstUri).Update(Precept.Compiler.Compile("""
            precept Order
            event Activate
            """));

        var handler = new WorkspaceSymbolHandler(store);

        var symbols = await handler.Handle(new WorkspaceSymbolParams
        {
            Query = "activate",
        }, CancellationToken.None);

        var symbol = symbols.Should().ContainSingle().Subject;
        symbol.Name.Should().Be("Activate");
        symbol.Kind.Should().Be(SymbolKind.Function);
        symbol.Location.IsLocation.Should().BeTrue();
        symbol.Location.Location.Should().NotBeNull();
        symbol.Location.Location!.Uri.Should().Be(FirstUri);
        symbol.Location.Location.Range.Should().BeEquivalentTo(
            DiagnosticProjector.ToRange(
                Precept.Compiler.Compile("""
                    precept Order
                    event Activate
                    """).Semantics.Events.Single().NameSpan));
    }
}
