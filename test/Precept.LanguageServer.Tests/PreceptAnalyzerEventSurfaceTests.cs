using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptAnalyzerEventSurfaceTests
{
    [Fact]
    public void Diagnostics_RejectOnlyPair_ProducesWarning()
    {
        const string text = """
            precept M
            state Pending initial
            event Deposit
            from Pending on Deposit -> reject "not open"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Any(diagnostic =>
            diagnostic.Message.Contains("ends in reject", StringComparison.Ordinal))
            .Should().BeTrue();
    }

    [Fact]
    public void Diagnostics_RejectOnlyPair_WhenEventSucceedsElsewhere_ProducesWarning()
    {
        const string text = """
            precept M
            state Pending initial
            state Active
            event Open
            event Deposit
            from Pending on Open -> transition Active
            from Pending on Deposit -> reject "not open"
            from Active on Deposit -> no transition
            """;

        var diagnostics = Analyze(text);

        diagnostics.Any(diagnostic =>
            diagnostic.Message.Contains("ends in reject", StringComparison.Ordinal))
            .Should().BeTrue();
    }

    [Fact]
    public void Diagnostics_EventNeverSucceedsAnywhere_ProducesWarning()
    {
        const string text = """
            precept M
            state A initial
            state B
            event Move
            event Stop
            from A on Move -> transition B
            from A on Stop -> reject "blocked"
            from B on Stop -> reject "blocked"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Any(diagnostic =>
            diagnostic.Message.Contains("can never succeed from any reachable state", StringComparison.Ordinal))
            .Should().BeTrue();
    }

    [Fact]
    public void Diagnostics_OrphanedEvent_IsWarningSeverity()
    {
        const string text = """
            precept M
            state A initial
            event Go
            event Unused
            from A on Go -> no transition
            """;

        var diagnostics = Analyze(text);
        var orphaned = diagnostics.First(diagnostic =>
            diagnostic.Message.Contains("never referenced", StringComparison.OrdinalIgnoreCase));

        orphaned.Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    private static Diagnostic[] Analyze(string text)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetDiagnostics(uri).ToArray();
    }
}
