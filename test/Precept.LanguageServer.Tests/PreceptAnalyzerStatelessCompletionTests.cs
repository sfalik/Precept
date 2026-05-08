using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptAnalyzerStatelessCompletionTests
{
    [Fact]
    public void Completions_RootEditLine_SuggestsAll()
    {
        const string text = """
            precept Profile
            field Name as string default ""
            field Age as number default 0
            edit $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("all");
    }

    [Fact]
    public void Completions_RootEditLine_SuggestsFieldNames()
    {
        const string text = """
            precept Profile
            field Name as string default ""
            field Age as number default 0
            edit $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Name");
        completions.Should().Contain("Age");
    }

    [Fact]
    public void Completions_InStateEditLine_SuggestsAllAsFirstOption()
    {
        const string text = """
            precept T
            field Notes as string nullable
            state Open initial
            in Open edit $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("all");
        completions.Should().Contain("Notes");
    }

    private static CompletionItem[] AnalyzeCompletions(string text, Position position)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetCompletions(uri, position).ToArray();
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
