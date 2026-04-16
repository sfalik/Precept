using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptAnalyzerRuleWarningTests
{
    [Fact]
    public void Diagnostics_ToStateEnsureWithoutIncomingTransition_ProducesWarning()
    {
        const string text = """
            precept M
            field Ready as boolean default true
            state Draft initial
            state Approved
            to Approved ensure Ready because "approved items must be ready"
            event Submit
            from Draft on Submit -> no transition
            """;

        var diagnostics = Analyze(text);

        var warning = diagnostics.Single(diagnostic =>
            diagnostic.Message.Contains("entry ensures are never checked", StringComparison.Ordinal));

        warning.Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    private static Diagnostic[] Analyze(string text)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetDiagnostics(uri).ToArray();
    }
}