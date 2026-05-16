using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerConstructionFieldStateTests
{
    [Fact]
    public void D94_SingleTarget_Success()
    {
        var precept = """
            precept Widget
            field Total as integer
            state Draft initial terminal
            event Start(Amount as integer) initial
            on Start initial -> set Total = Amount
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D94_SingleTarget_Failure()
    {
        var diagnostic = AssertSingleD94("""
            precept Widget
            field Total as integer
            field Note as string optional
            state Draft initial terminal
            event Start(Amount as integer, IsLarge as boolean) initial
            on Start initial when IsLarge -> set Total = Amount
            on Start initial -> set Note = "small"
            """);

        diagnostic.Message.Should().Be("Initial event 'Start' does not assign required field(s): Total");
    }

    [Fact]
    public void PRE0148_ConstructionGuardReadsField_Emitted()
    {
        var diagnostic = AssertSingleD148("""
            precept Widget
            field Total as integer optional
            state Draft initial terminal
            event Start(Amount as integer) initial
            on Start initial when Total > 0 -> set Total = Amount
            """);

        diagnostic.Message.Should().Be("Construction guard on initial event 'Start' reads field 'Total' before the entity exists — only event payload values are available here");
    }

    [Fact]
    public void PRE0148_ConstructionGuardReadsField_NotEmitted()
    {
        var precept = """
            precept Widget
            field Total as integer optional
            state Draft initial terminal
            event Start(Amount as integer) initial
            on Start initial when Amount > 0 -> set Total = Amount
            """;

        AssertNoD148(precept);
    }

    [Fact]
    public void PRE0148_ConstructionGuardReadsField_NotEmitted_NoGuard()
    {
        var precept = """
            precept Widget
            field Total as integer optional
            state Draft initial terminal
            event Start(Amount as integer) initial
            on Start initial -> set Total = Amount
            """;

        AssertNoD148(precept);
    }

    [Fact]
    public void PRE0148_NotEmitted_TransitionRow()
    {
        var precept = """
            precept Widget
            field Total as integer default 0
            state Idle initial terminal
            event Pause
            from Idle on Pause when Total > 0 -> no transition
            """;

        AssertNoD148(precept);
    }

    private static Diagnostic AssertSingleD94(string precept)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        var matches = diagnostics.Where(d => d.Code == DiagnosticCode.InitialEventMissingAssignments.ToString()).ToArray();
        matches.Should().ContainSingle();
        return matches[0];
    }

    private static void AssertNoD148(string precept)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        diagnostics.Where(d => d.Code == DiagnosticCode.ConstructionGuardReadsUninitializedField.ToString()).Should().BeEmpty();
        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
    }

    private static Diagnostic AssertSingleD148(string precept)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);
        var matches = diagnostics.Where(d => d.Code == DiagnosticCode.ConstructionGuardReadsUninitializedField.ToString()).ToArray();
        matches.Should().ContainSingle();
        return matches[0];
    }
}
