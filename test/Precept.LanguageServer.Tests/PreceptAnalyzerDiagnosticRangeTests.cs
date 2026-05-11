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

    [Fact]
    public void Diagnostics_C78_ConditionalNonBooleanCondition_SquigglesCorrectLine()
    {
        // Line 0: precept Test
        // Line 1: field Score as number default 0
        // Line 2: field Label as string default ""
        // Line 3: state A initial
        // Line 4: state B
        // Line 5: event Go
        // Line 6: from A on Go -> set Label = if Score then "high" else "low" -> transition B
        const string text = """
            precept Test
            field Score as number default 0
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if Score then "high" else "low" -> transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Conditional expression condition must be a non-nullable boolean");
        diagnostics[0].Range.Start.Line.Should().Be(6);
    }

    [Fact]
    public void Diagnostics_C98_TautologicalGuard_SquigglesGuardExpression()
    {
        const string text = """
            precept Test
            field X as number default 15 min 10
            state A initial
            event Go
            from A on Go
                when X >= 0
                -> no transition
            """;

        var diagnostics = Analyze(text);
        var diagnostic = diagnostics.Single(d => d.Code?.String == "PRECEPT098");
        var lineText = text.Split('\n')[5];
        var expectedStart = lineText.IndexOf("X >= 0", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "X >= 0".Length;

        diagnostic.Range.Start.Line.Should().Be(5);
        diagnostic.Range.Start.Character.Should().Be(expectedStart);
        diagnostic.Range.End.Character.Should().Be(expectedEnd);
    }

    [Fact]
    public void Diagnostics_C92_ProvablyZeroDivisorIdentifier_SquigglesWholeIdentifier()
    {
        const string text = """
            precept Test
            field Amount as number default 100
            field Rate as number default 1
            field Quotient as number default 0
            state A initial
            event Go
            from A on Go -> set Rate = 0 -> set Quotient = Amount / Rate -> no transition
            """;

        var diagnostics = Analyze(text);
        var diagnostic = diagnostics.Single(d => d.Code?.String == "PRECEPT092");
        var lineText = text.Split('\n')[6];
        var expectedStart = lineText.LastIndexOf("Rate", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Rate".Length;

        diagnostic.Range.Start.Line.Should().Be(6);
        diagnostic.Range.Start.Character.Should().Be(expectedStart);
        diagnostic.Range.End.Character.Should().Be(expectedEnd);
    }

    [Fact]
    public void Diagnostics_C94_AssignmentConstraintViolation_SquigglesWholeExpression()
    {
        const string text = """
            precept Test
            field Score as number default 50 min 0 max 100
            field Boost as number default 200 min 200 max 500
            state Review initial
            event AddBoost
            from Review on AddBoost -> set Score = Score + Boost -> no transition
            """;

        var diagnostics = Analyze(text);
        var diagnostic = diagnostics.Single(d => d.Code?.String == "PRECEPT094");
        var lineText = text.Split('\n')[5];
        var expectedStart = lineText.IndexOf("Score + Boost", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Score + Boost".Length;

        diagnostic.Range.Start.Line.Should().Be(5);
        diagnostic.Range.Start.Character.Should().Be(expectedStart);
        diagnostic.Range.End.Character.Should().Be(expectedEnd);
    }

    [Fact]
    public void Diagnostics_C76_SqrtArgument_SquigglesWholeArgument()
    {
        const string text = """
            precept Test
            field Measurement as number default 0
            field Root as number default 0
            state Active initial
            event Compute
            from Active on Compute -> set Root = sqrt(Measurement) -> no transition
            """;

        var diagnostics = Analyze(text);
        var diagnostic = diagnostics.Single(d => d.Code?.String == "PRECEPT076");
        var lineText = text.Split('\n')[5];
        var expectedStart = lineText.IndexOf("Measurement", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Measurement".Length;

        diagnostic.Range.Start.Line.Should().Be(5);
        diagnostic.Range.Start.Character.Should().Be(expectedStart);
        diagnostic.Range.End.Character.Should().Be(expectedEnd);
    }

    private static Diagnostic[] Analyze(string text)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetDiagnostics(uri).ToArray();
    }
}
