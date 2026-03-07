using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptAnalyzerCompletionTests
{
    [Fact]
    public void Completions_InvariantScope_ExcludesEventArgs()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Deposit with Amount as number
            invariant Bal$$
            from A on Deposit -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Balance");
        completions.Should().NotContain("Deposit.Amount");
        completions.Should().NotContain("Amount");
    }

    [Fact]
    public void Completions_EventAssertScope_IncludesBareArgsAndExcludesFields()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Deposit with Amount as number
            on Deposit assert $$
            from A on Deposit -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Amount");
        completions.Should().Contain("Deposit.Amount");
        completions.Should().NotContain("Balance");
    }

    [Fact]
    public void Completions_EventDeclaration_DoesNotSuggestInitial()
    {
        const string text = """
            precept M
            state A initial
            event Deposit $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("with");
        completions.Should().NotContain("initial");
    }

    [Fact]
    public void Completions_FromStateClause_SuggestsOnAssertAndArrow()
    {
        const string text = """
            precept M
            state A initial
            state B
            event Go
            from A $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("on");
        completions.Should().Contain("assert");
        completions.Should().Contain("->");
    }

    [Fact]
    public void Completions_CollectionMembers_AreFilteredByCollectionKind()
    {
        const string text = """
            precept M
            field Floors as set of number
            field Queue as queue of number
            state A initial
            event Go
            from A on Go when Floors.$$ -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var setMembers = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        setMembers.Should().Contain("Floors.count");
        setMembers.Should().Contain("Floors.min");
        setMembers.Should().Contain("Floors.max");
        setMembers.Should().NotContain("Floors.peek");

        const string queueText = """
            precept M
            field Queue as queue of number
            state A initial
            event Go
            from A on Go when Queue.$$ -> no transition
            """;

        var (queueCode, queuePosition) = ExtractPosition(queueText);
        var queueMembers = AnalyzeCompletions(queueCode, queuePosition).Select(static item => item.Label).ToArray();
        queueMembers.Should().Contain("Queue.count");
        queueMembers.Should().Contain("Queue.peek");
        queueMembers.Should().NotContain("Queue.min");
        queueMembers.Should().NotContain("Queue.max");
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