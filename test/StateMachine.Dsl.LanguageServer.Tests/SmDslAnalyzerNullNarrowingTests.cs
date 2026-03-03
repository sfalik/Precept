using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StateMachine.Dsl.LanguageServer;
using Xunit;

namespace StateMachine.Dsl.LanguageServer.Tests;

public class SmDslAnalyzerNullNarrowingTests
{
    [Fact]
    public void Diagnostics_Guard_NullCheckAndNumericComparison_UsesNarrowedType()
    {
        const string text = """
            machine M
            number? RetryCount
            state A
            state B
            event Go
            from A on Go
              if RetryCount != null && RetryCount > 0
                transition B
              else
                reject \"blocked\"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Guard_NullCheckOrNumericComparison_UsesNarrowedType()
    {
        const string text = """
            machine M
            number? RetryCount
            state A
            state B
            event Go
            from A on Go
              if RetryCount == null || RetryCount > 0
                transition B
              else
                reject \"blocked\"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Guard_NullableNumericWithoutNarrowing_FailsComparison()
    {
        const string text = """
            machine M
            number? RetryCount
            state A
            state B
            event Go
            from A on Go
              if RetryCount > 0
                transition B
              else
                reject \"blocked\"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("operator '>' requires numeric operands");
    }

    [Fact]
    public void Diagnostics_Set_NullableValueAssignedToNonNullableTarget_Fails()
    {
        const string text = """
            machine M
            number Value
            number? RetryCount
            state A
            state B
            event Go
            from A on Go
              set Value = RetryCount
              transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("set target 'Value' type mismatch");
    }

    [Fact]
    public void Diagnostics_Set_NullableValueNarrowedByGuard_AssignsCleanly()
    {
        const string text = """
            machine M
            number Value
            number? RetryCount
            state A
            state B
            event Go
            from A on Go
              if RetryCount != null
                set Value = RetryCount
                transition B
              else
                reject \"blocked\"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    private static Diagnostic[] Analyze(string text)
    {
        var analyzer = new SmDslAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.sm");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetDiagnostics(uri).ToArray();
    }
}
