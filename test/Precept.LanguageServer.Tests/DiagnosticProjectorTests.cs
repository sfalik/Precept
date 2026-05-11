using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;
using LspDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using PreceptDiagnostic = Precept.Language.Diagnostic;

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
    public void Project_InfoDiagnostic_MapsToLspInformation()
    {
        var compilation = CreateCompilation(new PreceptDiagnostic(
            Severity.Info,
            DiagnosticStage.Type,
            "InfoCode",
            "Informational note",
            new SourceSpan(0, 5, 1, 1, 1, 6)));

        var projected = DiagnosticProjector.Project(compilation);

        projected.Should().ContainSingle();
        projected[0].Severity.Should().Be(LspDiagnosticSeverity.Information);
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
    public void Project_GraphWarningOnState_UsesStateNameTokenRange()
    {
        var compilation = Compiler.Compile("""
            precept Workflow
            state Draft initial
            state Rejected

            # comment
            event Reject
            from Draft on Reject -> transition Rejected
            """);

        var diagnostic = DiagnosticProjector.Project(compilation)
            .Single(entry => entry.Code?.String == nameof(Precept.Language.DiagnosticCode.StructuralSinkState)
                && entry.Message.Contains("Rejected"));

        diagnostic.Range.Should().BeEquivalentTo(new Range
        {
            Start = new Position(2, 6),
            End = new Position(2, 14),
        });
    }

    [Fact]
    public void Project_InvalidModifierForMoney_UsesModifierTokenRange()
    {
        var compilation = Compiler.Compile("""
            precept Quote
            field Amount as money in 'USD' nonnegative
            state Draft initial terminal
            """);

        var diagnostic = DiagnosticProjector.Project(compilation)
            .Single(entry => entry.Code?.String == nameof(Precept.Language.DiagnosticCode.InvalidModifierForType));

        diagnostic.Range.Should().BeEquivalentTo(new Range
        {
            Start = new Position(1, 31),
            End = new Position(1, 42),
        });
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
    public void Project_MixedErrorAndWarningDiagnostics_ProjectsBothWithPreceptSource()
    {
        var compilation = CreateCompilation(
            new PreceptDiagnostic(
                Severity.Warning,
                DiagnosticStage.Type,
                "WarningCode",
                "Warning message",
                new SourceSpan(5, 4, 2, 3, 2, 7)),
            new PreceptDiagnostic(
                Severity.Error,
                DiagnosticStage.Parse,
                "ErrorCode",
                "Error message",
                new SourceSpan(12, 3, 3, 5, 3, 8)));

        var projected = DiagnosticProjector.Project(compilation);

        projected.Should().HaveCount(2);
        projected.Should().Contain(d => d.Severity == LspDiagnosticSeverity.Warning);
        projected.Should().Contain(d => d.Severity == LspDiagnosticSeverity.Error);
        projected.Should().OnlyContain(d => d.Source == "precept");
    }

    [Fact]
    public void ToRange_MultiLineSpan_MapsToZeroBasedRange()
    {
        var span = new SourceSpan(Offset: 7, Length: 10, StartLine: 2, StartColumn: 4, EndLine: 4, EndColumn: 6);

        var range = DiagnosticProjector.ToRange(span);

        range.Start.Line.Should().Be(1);
        range.Start.Character.Should().Be(3);
        range.End.Line.Should().Be(3);
        range.End.Character.Should().Be(5);
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

    private static Compilation CreateCompilation(params PreceptDiagnostic[] diagnostics) => new(
        new TokenStream(ImmutableArray<Precept.Language.Token>.Empty, ImmutableArray<PreceptDiagnostic>.Empty),
        new ConstructManifest(ImmutableArray<ParsedConstruct>.Empty, ImmutableArray<PreceptDiagnostic>.Empty),
        SymbolTable.Empty,
        SemanticIndex.Empty,
        StateGraph.Empty,
        new ProofLedger(
            ImmutableArray<ProofObligation>.Empty,
            ImmutableArray<FaultSiteLink>.Empty,
            ImmutableArray<ConstraintInfluenceEntry>.Empty,
            ImmutableArray<InitialStateSatisfiabilityResult>.Empty,
            ImmutableArray<PreceptDiagnostic>.Empty),
        diagnostics.ToImmutableArray(),
        diagnostics.Any(diagnostic => diagnostic.Severity == Severity.Error));
}
