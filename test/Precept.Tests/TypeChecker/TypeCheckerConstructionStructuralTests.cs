using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerConstructionStructuralTests
{
    [Fact]
    public void PRE0092_AllowsInitialEventOnStatefulPrecept()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start(InputCount as integer) initial
            on Start -> set Count = InputCount
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PRE0145_InitialEventInTransitionRow_Emitted()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial
            state Done terminal
            event Start initial
            on Start -> set Count = 1
            from Draft on Start -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InitialEventInTransitionRow);
    }

    [Fact]
    public void PRE0146_ZeroConstructionRows_Emitted()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ZeroConstructionRows);
    }

    [Fact]
    public void PRE0146_ZeroConstructionRows_NotEmitted()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start initial
            on Start -> set Count = 1
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PRE0147_MultipleInitialEvents_Emitted()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start initial
            event Seed initial
            on Start -> set Count = 1
            on Seed -> set Count = 2
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.MultipleInitialEvents);
    }

    [Fact]
    public void PRE0147_MultipleInitialEvents_NotEmitted_SameEvent()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start(InputCount as integer) initial
            on Start when InputCount > 0 -> set Count = InputCount
            on Start when InputCount <= 0 -> set Count = 0
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PRE0147_MultipleInitialEvents_NotEmitted_SingleEvent()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start initial
            on Start -> set Count = 1
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void ConstructionRow_AllowsGuard_NoError()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Start(InputCount as integer) initial
            on Start when InputCount > 0 -> set Count = InputCount
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
