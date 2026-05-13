using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerFieldStateTests
{
    [Fact]
    public void D130_TransitionGuard_ReadsOmitField_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            state Draft initial
            event E
            in Draft omit F
            from Draft on E when F > 0 -> no transition
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in state 'Draft' and cannot be read in this expression");
    }

    [Fact]
    public void D130_TransitionGuard_ReadsNonOmitField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            event E
            from Draft on E when F > 0 -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D130_TransitionGuard_OmitInDifferentState_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            state Pending
            event E
            in Pending omit F
            from Draft on E when F > 0 -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D130_TransitionGuard_WildcardFromState_ReadsOmitField_ListsAffectedStates()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            state Draft initial
            state Pending
            event E
            in Draft omit F
            from any on E when F > 0 -> no transition
            """);

        diagnostic.Message.Should().Contain("state 'Draft'");
    }

    [Fact]
    public void D130_TransitionGuard_WildcardFromState_OmitInMultipleStates_ListsAll()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            state Draft initial
            state Pending
            event E
            in Draft omit F
            in Pending omit F
            from any on E when F > 0 -> no transition
            """);

        diagnostic.Message.Should().Contain("Draft, Pending");
    }

    [Fact]
    public void D130_TransitionGuard_NoGuard_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            event E
            in Draft omit F
            from Draft on E -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D130_ActionRHS_ReadsOmitField_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            field G as number default 0
            state Draft initial
            event E
            in Draft omit G
            from Draft on E -> set F = G -> no transition
            """);

        diagnostic.Message.Should().Be("Field 'G' is omitted in state 'Draft' and cannot be read in this expression");
    }

    [Fact]
    public void D130_ActionRHS_ReadsEventArg_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer default 0
            state Draft initial
            event E(X as integer)
            from Draft on E -> set F = E.X -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D130_ActionRHS_ComplexExpression_ReadsOmitField_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            field G as number default 0
            state Draft initial
            event E
            in Draft omit G
            from Draft on E -> set F = G + 1 -> no transition
            """);

        diagnostic.Message.Should().Be("Field 'G' is omitted in state 'Draft' and cannot be read in this expression");
    }

    [Fact]
    public void D130_InStateEnsure_ReadsOmitField_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            state Draft initial
            in Draft omit F
            in Draft ensure F > 0 because "x"
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in state 'Draft' and cannot be read in this expression");
    }

    [Fact]
    public void D130_FromStateEnsure_ReadsOmitField_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            state Draft initial
            in Draft omit F
            from Draft ensure F > 0 because "x"
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in state 'Draft' and cannot be read in this expression");
    }

    [Fact]
    public void D130_ToStateEnsure_ReadsOmitField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            in Draft omit F
            to Draft ensure F > 0 because "x"
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D130_ToStateHook_GuardReadsOmitField_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            field G as number default 0
            state Draft initial
            in Draft omit F
            to Draft when F > 0 -> set G = 1
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in state 'Draft' and cannot be read in this expression");
    }

    [Fact]
    public void D130_FromStateHook_ActionRHSReadsOmitField_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            field G as number default 0
            state Draft initial
            in Draft omit F
            from Draft -> set G = F
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in state 'Draft' and cannot be read in this expression");
    }

    [Fact]
    public void D130_SelfLoop_OmitInState_Fires()
    {
        var diagnostic = AssertSingleD130("""
            precept Widget
            field F as number default 0
            state S initial
            event E
            in S omit F
            from S on E when F > 0 -> transition S
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in state 'S' and cannot be read in this expression");
    }

    private static Diagnostic AssertSingleD130(string preceptText)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(preceptText);
        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().OnlyContain(code => code == nameof(DiagnosticCode.OmittedFieldReadInState));

        var d130 = diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.OmittedFieldReadInState))
            .ToArray();

        d130.Should().ContainSingle();
        return d130[0];
    }
}
