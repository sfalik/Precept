using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
using Precept.Language;
using Xunit;
using LspDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

namespace Precept.LanguageServer.Tests;

public class DiagnosticProjectorTests
{
    [Fact]
    public void Project_ErrorDiagnostic_MapsToLspError()
    {
        var compilation = Compiler.Compile(ErrorSource);

        var projected = DiagnosticProjector.Project(compilation);

        projected.Should().Contain(d => d.Severity == LspDiagnosticSeverity.Error);
    }

    [Fact]
    public void Project_WarningDiagnostic_MapsToLspWarning()
    {
        var compilation = Compiler.Compile(WarningSource);

        var projected = DiagnosticProjector.Project(compilation);

        projected.Should().Contain(d => d.Severity == LspDiagnosticSeverity.Warning);
    }

    [Fact]
    public void Project_Diagnostic_SpanMappedToLspRange()
    {
        var compilation = Compiler.Compile(ErrorSource);
        var expected = compilation.Diagnostics.First(d => d.Severity == Severity.Error);

        var projected = DiagnosticProjector.Project(compilation);
        var actual = projected.First(d => d.Severity == LspDiagnosticSeverity.Error);

        actual.Range.Start.Line.Should().Be(expected.Span.StartLine - 1);
        actual.Range.Start.Character.Should().Be(expected.Span.StartColumn - 1);
        actual.Range.End.Line.Should().Be(expected.Span.EndLine - 1);
        actual.Range.End.Character.Should().Be(expected.Span.EndColumn - 1);
    }

    [Fact]
    public void Project_Diagnostic_SourceIsPreceptString()
    {
        var compilation = Compiler.Compile(WarningSource);

        var projected = DiagnosticProjector.Project(compilation);

        projected.Should().NotBeEmpty();
        projected.Should().OnlyContain(d => d.Source == "precept");
    }

    [Fact]
    public void Project_EmptyDiagnostics_ReturnsEmptyList()
    {
        var compilation = Compiler.Compile(ValidSource);

        compilation.Diagnostics.Should().BeEmpty();
        DiagnosticProjector.Project(compilation).Should().BeEmpty();
    }

    private const string ValidSource = """
        precept OrderItem
        field Quantity as number
        field Price as number
        state Pending initial terminal
        """;

    private const string ErrorSource = """
        precept Broken
        field Quantity as UnknownType
        state Pending initial terminal
        """;

    private const string WarningSource = """
        precept Widget
        field Count as integer positive nonnegative
        state Open initial terminal
        """;
}
