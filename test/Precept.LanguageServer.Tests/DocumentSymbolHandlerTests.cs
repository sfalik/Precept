using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.LanguageServer.Handlers;
using Precept.Pipeline;
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

    private static DocumentSymbol GetDocumentSymbol(Precept.Pipeline.Compilation compilation, string name) =>
        GetDocumentSymbols(compilation).Single(symbol => symbol.Name == name);

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

    [Fact]
    public void BuildDocumentSymbols_FieldSelectionRange_UsesNameSpan()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var field = compilation.Semantics.Fields.Single(symbol => symbol.Name == "Quantity");
        var symbol = GetDocumentSymbol(compilation, "Quantity");

        symbol.SelectionRange.Should().BeEquivalentTo(DiagnosticProjector.ToRange(field.NameSpan));
        symbol.SelectionRange.Should().NotBeEquivalentTo(DiagnosticProjector.ToRange(field.Syntax.Span));
    }

    [Fact]
    public void BuildDocumentSymbols_StateSelectionRange_UsesNameSpan()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var state = compilation.Semantics.States.Single(symbol => symbol.Name == "Pending");
        var symbol = GetDocumentSymbol(compilation, "Pending");

        symbol.SelectionRange.Should().BeEquivalentTo(DiagnosticProjector.ToRange(state.NameSpan));
        symbol.SelectionRange.Should().NotBeEquivalentTo(DiagnosticProjector.ToRange(state.Syntax.Span));
    }

    [Fact]
    public void BuildDocumentSymbols_EventSelectionRange_UsesNameSpan()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var evt = compilation.Semantics.Events.Single(symbol => symbol.Name == "Activate");
        var symbol = GetDocumentSymbol(compilation, "Activate");

        symbol.SelectionRange.Should().BeEquivalentTo(DiagnosticProjector.ToRange(evt.NameSpan));
        symbol.SelectionRange.Should().NotBeEquivalentTo(DiagnosticProjector.ToRange(evt.Syntax.Span));
    }

    [Fact]
    public void BuildDocumentSymbols_PreceptSelectionRange_UsesHeaderIdentifierSpan()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var header = compilation.ConstructManifest.Constructs.Single(construct => construct.Meta.Kind == ConstructKind.PreceptHeader);
        var identifierSlot = header.GetRequiredSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList);
        var symbol = GetDocumentSymbol(compilation, "OrderItem");

        symbol.SelectionRange.Should().BeEquivalentTo(DiagnosticProjector.ToRange(identifierSlot.Span));
        symbol.SelectionRange.Should().NotBeEquivalentTo(DiagnosticProjector.ToRange(header.Span));
    }

    [Fact]
    public void BuildDocumentSymbols_AllSelectionRanges_AreContainedWithinRanges()
    {
        var compilation = Precept.Compiler.Compile(Source);

        foreach (var symbol in GetDocumentSymbols(compilation))
        {
            Contains(symbol.Range, symbol.SelectionRange).Should().BeTrue($"{symbol.Name} must satisfy the LSP selectionRange containment rule");
        }
    }

    [Fact]
    public void ExpandRangeToContainSelection_GrowsRangeWhenSelectionFallsOutside()
    {
        var range = new SourceSpan(Offset: 10, Length: 6, StartLine: 2, StartColumn: 5, EndLine: 2, EndColumn: 11);
        var selection = new SourceSpan(Offset: 8, Length: 10, StartLine: 2, StartColumn: 3, EndLine: 2, EndColumn: 13);

        var expanded = OutlineSymbolProjector.ExpandRangeToContainSelection(range, selection);

        expanded.Should().Be(new SourceSpan(Offset: 8, Length: 10, StartLine: 2, StartColumn: 3, EndLine: 2, EndColumn: 13));
    }

    private static bool Contains(Range range, Range selectionRange)
    {
        var startsAfter =
            selectionRange.Start.Line > range.Start.Line ||
            (selectionRange.Start.Line == range.Start.Line && selectionRange.Start.Character >= range.Start.Character);
        var endsBefore =
            selectionRange.End.Line < range.End.Line ||
            (selectionRange.End.Line == range.End.Line && selectionRange.End.Character <= range.End.Character);

        return startsAfter && endsBefore;
    }
}
