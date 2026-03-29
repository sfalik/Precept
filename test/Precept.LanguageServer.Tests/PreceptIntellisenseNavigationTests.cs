using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptIntellisenseNavigationTests
{
    [Fact]
    public void Hover_FieldReference_ShowsTypeAndDefault()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Go
            from A on Go when Bal$$ance >= 0 -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);

        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        hover!.Contents.ToString().Should().Contain("Balance");
        hover.Contents.ToString().Should().Contain("number");
        hover.Contents.ToString().Should().Contain("0");
    }

    [Fact]
    public void Hover_Keyword_ShowsConstructForm()
    {
        const string text = """
            precept M
            fi$$eld Balance as number default 0
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);

        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        hover!.Contents.ToString().Should().Contain("field <Name>[, <Name>, ...] as <Type> [nullable] [default <Value>]");
        hover.Contents.ToString().Should().Contain("Declares a scalar or collection data field");
    }

    [Fact]
    public void Definition_TransitionTarget_ResolvesToStateDeclaration()
    {
        const string text = """
            precept M
            state A initial
            state Closed
            event Go
            from A on Go -> transition Clo$$sed
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var uri = DocumentUri.From("file:///tmp/navigation.precept");

        var definitions = PreceptDocumentIntellisense.CreateDefinition(uri, info, position).ToArray();

        definitions.Should().HaveCount(1);
        definitions[0].IsLocation.Should().BeTrue();
        definitions[0].Location!.Range.Start.Line.Should().Be(2);
    }

    [Fact]
    public void DocumentSymbols_ReturnHierarchyForFieldsStatesAndEvents()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Go with Amount as number
            from A on Go -> no transition
            """;

        var info = PreceptDocumentIntellisense.Analyze(text);

        var symbols = PreceptDocumentIntellisense.CreateDocumentSymbols(info).ToArray();

        symbols.Should().HaveCount(1);
        symbols[0].IsDocumentSymbol.Should().BeTrue();
        var root = symbols[0].DocumentSymbol!;
        root.Name.Should().Be("M");
        root.Children.Should().NotBeNull();
        root.Children!.Select(static child => child.Name).Should().Contain(new[] { "Balance", "A", "Go" });
        root.Children!.First(child => child.Name == "Go").Children.Should().ContainSingle(child => child.Name == "Amount");
    }

    [Fact]
    public void Diagnostics_IncludeAuditWarnings()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            state DeadEnd
            state Unreachable
            event Used
            event Unused
            from A on Used -> transition DeadEnd
            from DeadEnd on Used -> reject "blocked"
            """;

        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);

        var diagnostics = analyzer.GetDiagnostics(uri).ToArray();

        diagnostics.Should().Contain(diagnostic => diagnostic.Message.Contains("Unreachable", StringComparison.Ordinal));
        diagnostics.Should().Contain(diagnostic => diagnostic.Message.Contains("never referenced", StringComparison.OrdinalIgnoreCase));
        diagnostics.Should().Contain(diagnostic => diagnostic.Message.Contains("no path forward", StringComparison.OrdinalIgnoreCase));
    }

    private static (string text, Position position) ExtractPosition(string textWithMarker)
    {
        var index = textWithMarker.IndexOf("$$", StringComparison.Ordinal);
        index.Should().BeGreaterThanOrEqualTo(0);

        var text = textWithMarker.Replace("$$", string.Empty, StringComparison.Ordinal);
        var prefix = textWithMarker[..index];
        var line = prefix.Count(static ch => ch == '\n');
        var lastNewLine = prefix.LastIndexOf('\n');
        var character = lastNewLine >= 0 ? prefix.Length - lastNewLine - 1 : prefix.Length;
        return (text, new Position(line, character));
    }
}