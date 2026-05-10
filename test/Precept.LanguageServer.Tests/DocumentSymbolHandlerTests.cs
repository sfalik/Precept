using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class DocumentSymbolHandlerTests
{
    private static DocumentSymbol[] GetDocumentSymbols(Precept.Pipeline.Compilation compilation) =>
        DocumentSymbolHandler.BuildDocumentSymbols(compilation)
            .Select(symbol =>
            {
                symbol.IsDocumentSymbol.Should().BeTrue();
                symbol.DocumentSymbol.Should().NotBeNull();
                return symbol.DocumentSymbol!;
            })
            .ToArray();

    private const string Source = """
precept OrderItem
field Quantity as number
state Pending initial
event Activate
""";

    [Fact]
    public void BuildDocumentSymbols_ReturnsSymbolForEachOutlineNode()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var symbols = GetDocumentSymbols(compilation);

        symbols.Should().HaveCount(4);
        symbols.Select(symbol => symbol.Name).Should().Equal("OrderItem", "Quantity", "Pending", "Activate");
    }

    [Fact]
    public void BuildDocumentSymbols_FieldIsProperty()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var field = GetDocumentSymbols(compilation)
            .Single(symbol => symbol.Name == "Quantity");

        field.Kind.Should().Be(SymbolKind.Property);
    }

    [Fact]
    public void BuildDocumentSymbols_StateIsEnum()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var state = GetDocumentSymbols(compilation)
            .Single(symbol => symbol.Name == "Pending");

        state.Kind.Should().Be(SymbolKind.Enum);
    }

    [Fact]
    public void BuildDocumentSymbols_EventIsFunction()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var evt = GetDocumentSymbols(compilation)
            .Single(symbol => symbol.Name == "Activate");

        evt.Kind.Should().Be(SymbolKind.Function);
    }
}
