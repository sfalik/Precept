using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests.Construction;

/// <summary>
/// Slice 6 tests: graph analyzer construction-row awareness.
/// AlwaysRejecting is promoted to Error severity for all-reject construction paths.
/// </summary>
public class GraphAnalyzerConstructionTests
{
    [Fact]
    public void AlwaysRejecting_ConstructionRow_IsError()
    {
        // A single ConstructionRowReject with a guard means the only construction
        // path always rejects — the precept can never be created.
        var (_, _, graph) = AnalyzeAllowingDiagnostics("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start initial
            on Start initial when false -> reject "never"
            """);

        var d1 = graph.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.AlwaysRejecting))
            .ToList();

        d1.Should().ContainSingle();
        d1.Single().Severity.Should().Be(Severity.Error);
        d1.Single().Message.Should().Contain("Start");
    }

    [Fact]
    public void AlwaysRejecting_ConstructionRow_NotEmitted()
    {
        // A guarded success construction row provides a valid creation path.
        var (_, _, graph) = AnalyzeAllowingDiagnostics("""
            precept Widget
            field Count as integer
            state Draft initial terminal
            event Start(InputCount as integer) initial
            on Start initial when InputCount > 0 -> set Count = InputCount
            """);

        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.AlwaysRejecting));
    }

    [Fact]
    public void AlwaysRejecting_TransitionRow_IsWarning()
    {
        // Regression: transition rows that always reject still emit Warning, not Error.
        var (_, _, graph) = AnalyzeAllowingDiagnostics("""
            precept Workflow
            state Idle initial
            state Done terminal
            event Pause
            event Complete
            from Idle on Complete -> transition Done
            from Idle on Pause -> reject "not allowed"
            """);

        var d1 = graph.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.AlwaysRejecting))
            .ToList();

        d1.Should().ContainSingle();
        d1.Single().Severity.Should().Be(Severity.Warning);
        d1.Single().Message.Should().Contain("Pause");
    }

    [Fact]
    public void ConstructionRow_IncludedInReachability()
    {
        // A precept with a construction row must not produce spurious UnreachableState diagnostics.
        var (_, _, graph) = AnalyzeAllowingDiagnostics("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start initial
            on Start initial -> set Count = 1
            """);

        graph.ReachableStates.Should().Contain("Draft");
        graph.UnreachableStates.Should().BeEmpty();
        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnreachableState));
    }

    private static (SemanticIndex Index, IReadOnlyList<Diagnostic> Diagnostics, StateGraph Graph) AnalyzeAllowingDiagnostics(string source)
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(source);
        return (index, diagnostics, Pipeline.GraphAnalyzer.Analyze(index));
    }
}
