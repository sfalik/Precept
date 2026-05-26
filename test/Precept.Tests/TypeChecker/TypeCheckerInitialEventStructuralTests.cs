using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerInitialEventStructuralTests
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
    public void PRE0092_AllowsInitialEventConstructionRow_OnMultiStatePrecept()
    {
        var precept = """
            precept LoanApplication
            field ApplicantName as string
            field RequestedAmount as money in 'USD'
            field CreditScore as integer
            field DecisionNote as string optional

            state Pending initial
            state UnderReview
            state Approved terminal
            state Declined terminal

            event Create(Applicant as string notempty, Amount as money in 'USD', Score as integer) initial
            on Create ensure Create.Amount > '0.00 USD' because "Loan amount must be positive"
            on Create ensure Create.Score >= 300 because "Minimum credit score is 300"

            on Create
                -> set ApplicantName = Create.Applicant
                -> set RequestedAmount = Create.Amount
                -> set CreditScore = Create.Score

            event Review
            event Approve
            event Decline(Note as string notempty)

            from Pending on Review
                -> transition UnderReview
            from UnderReview on Approve
                -> transition Approved
            from UnderReview on Decline
                -> set DecisionNote = Decline.Note
                -> transition Declined
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PRE0092_StillEmitted_ForNonInitialEventHandlerInStatefulPrecept()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial
            state Done terminal
            event Start initial
            event Ping
            event Finish
            on Start -> set Count = 1
            on Ping -> set Count = Count + 1
            from Draft on Finish -> transition Done
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Where(d => d.Code == nameof(DiagnosticCode.EventHandlerInStatefulPrecept))
            .Should().ContainSingle(d => d.Args.Contains("Ping"));
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
