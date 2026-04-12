using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

/// <summary>
/// Regression tests for diagnostic squiggle line accuracy.
/// Guards against constraint violations being emitted on line 1 (the precept header)
/// instead of on the actual offending declaration. These tests assert Range.Start.Line
/// on the LSP Diagnostic objects returned by GetDiagnostics, exercising the full
/// ParseWithDiagnostics → GetDiagnostics → LSP Range pipeline.
/// </summary>
public class PreceptAnalyzerDiagnosticRangeTests
{
    [Fact]
    public void Diagnostics_C17_NonNullableFieldWithoutDefault_SquigglesFieldLine()
    {
        // Line 0: precept Task        (LSP lines are 0-based)
        // Line 1: field Title as string nullable
        // Line 2: field Description as string nullable
        // Line 3: field Blah as choice("A", "B")  ← should squiggle here
        const string text = """
            precept Task
            field Title as string nullable
            field Description as string nullable
            field Blah as choice("A", "B")
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Blah");
        diagnostics[0].Range.Start.Line.Should().Be(3);
    }

    [Fact]
    public void Diagnostics_C6_DuplicateField_SquigglesSecondDeclarationLine()
    {
        // Line 3 (0-based) is the duplicate field.
        const string text = """
            precept Test
            field A as number default 0
            state Open initial
            field A as string nullable
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Duplicate field");
        diagnostics[0].Range.Start.Line.Should().Be(3);
    }

    [Fact]
    public void Diagnostics_C7_DuplicateState_SquigglesSecondDeclarationLine()
    {
        // Line 2 (0-based) is the duplicate state.
        const string text = """
            precept Test
            state Active initial
            state Active
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Duplicate state");
        diagnostics[0].Range.Start.Line.Should().Be(2);
    }

    [Fact]
    public void Diagnostics_SquiggleDoesNotLandOnLine0ForNonHeaderError()
    {
        // Any semantic error on a non-first declaration must not land on line 0
        // (which would mean it's squiggling the precept header instead of the offending line).
        const string text = """
            precept Task
            field Title as string nullable
            field Score as number default 0
            field Blah as choice("A", "B")
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Range.Start.Line.Should().BeGreaterThan(0,
            "a field declaration error should squiggle the field line, not the precept header");
    }

    private static Diagnostic[] Analyze(string text)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetDiagnostics(uri).ToArray();
    }
}
