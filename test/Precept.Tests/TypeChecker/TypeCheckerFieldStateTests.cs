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

    [Fact]
    public void D131_SetAction_TargetFieldOmitInTargetState_Fires()
    {
        var diagnostic = AssertSingleD131("""
            precept Widget
            field F as number default 0
            state Draft initial
            state Review
            event E
            in Review omit F
            from Draft on E -> set F = 1 -> transition Review
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in target state 'Review'; this transition cannot set it");
    }

    [Fact]
    public void D131_SetAction_TargetFieldNotOmitInTargetState_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            state Review
            event E
            from Draft on E -> set F = 1 -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D131_SetAction_NoTransitionOutcome_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            state Review
            event E
            in Review omit F
            from Draft on E -> set F = 1 -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D131_SetAction_RejectOutcome_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            state Review
            event E
            in Review omit F
            from Draft on E -> set F = 1 -> reject "reason"
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D131_SetAction_OmitInFromStateNotTargetState_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E -> set F = 1 -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D131_OnEntryHook_SetOmitField_Fires()
    {
        var diagnostic = AssertSingleD131("""
            precept Widget
            field F as number default 0
            state Draft initial
            in Draft omit F
            to Draft -> set F = 1
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in target state 'Draft'; this transition cannot set it");
    }

    [Fact]
    public void D131_OnExitHook_SetOmitField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as number default 0
            state Draft initial
            in Draft omit F
            from Draft -> set F = 1
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D131_SelfLoop_SetOmitField_Fires()
    {
        var diagnostic = AssertSingleD131("""
            precept Widget
            field F as number default 0
            state S initial
            event E
            in S omit F
            from S on E -> set F = 1 -> transition S
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in target state 'S'; this transition cannot set it");
    }

    [Fact]
    public void D131_WildcardFromState_SetOmitInTarget_Fires()
    {
        var diagnostic = AssertSingleD131("""
            precept Widget
            field F as number default 0
            state Draft initial
            state Review
            event E
            in Review omit F
            from any on E -> set F = 1 -> transition Review
            """);

        diagnostic.Message.Should().Be("Field 'F' is omitted in target state 'Review'; this transition cannot set it");
    }

    [Fact]
    public void D132_OmitToNonOmit_RequiredField_NoSet_Fires()
    {
        var diagnostic = AssertSingleD132("""
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E -> transition Review
            """);

        diagnostic.Message.Should().Be("Required field 'F' is omitted in 'Draft' but present in 'Review'; add `set F = ...` to this transition");
    }

    [Fact]
    public void D132_OmitToNonOmit_RequiredField_WithSet_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E -> set F = 1 -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_OmitToNonOmit_OptionalField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer optional
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_OmitToNonOmit_DefaultField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer default 0
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_OmitToNonOmit_ComputedField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field G as integer default 0
            field F as integer <- G + 1
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_BothStatesOmit_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            in Draft omit F
            in Review omit F
            from Draft on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_NeitherStateOmit_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            from Draft on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_WildcardFromState_OmitInOneState_Fires()
    {
        var diagnostic = AssertSingleD132("""
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            in Draft omit F
            from any on E -> transition Review
            """);

        diagnostic.Message.Should().Be("Required field 'F' is omitted in 'Draft' but present in 'Review'; add `set F = ...` to this transition");
    }

    [Fact]
    public void D132_WildcardFromState_OmitInAllStates_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            in any omit F
            from any on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_SelfLoop_OmitInState_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer
            state S initial
            event E
            in S omit F
            from S on E -> transition S
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_AddActionOnCollection_Exempt_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Tags as set of string
            state Draft initial
            state Review
            event E
            in Draft omit Tags
            from Draft on E -> add Tags "x" -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_StatelessPrecept_Inapplicable_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field F as integer
            event Start initial
            on Start -> set F = 1
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_CollectionField_OmitToNonOmit_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Items as set of string
            state Draft initial
            state Review
            event E
            in Draft omit Items
            from Draft on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_ListField_OmitToNonOmit_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Entries as list of string
            state Draft initial
            state Review
            event E
            in Draft omit Entries
            from Draft on E -> transition Review
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void D132_DoesNotSuppressD130()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field F as integer
            state Draft initial
            state Review
            event E
            in Draft omit F
            from Draft on E when F > 0 -> transition Review
            """);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().BeEquivalentTo(
            [
                nameof(DiagnosticCode.OmittedFieldReadInState),
                nameof(DiagnosticCode.RequiredFieldUnassignedOnEntry)
            ]);
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

    private static Diagnostic AssertSingleD131(string preceptText)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(preceptText);
        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().OnlyContain(code => code == nameof(DiagnosticCode.OmittedFieldSetInTargetState));

        var d131 = diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.OmittedFieldSetInTargetState))
            .ToArray();

        d131.Should().ContainSingle();
        return d131[0];
    }

    private static Diagnostic AssertSingleD132(string preceptText)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check(preceptText);
        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().OnlyContain(code => code == nameof(DiagnosticCode.RequiredFieldUnassignedOnEntry));

        var d132 = diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.RequiredFieldUnassignedOnEntry))
            .ToArray();

        d132.Should().ContainSingle();
        return d132[0];
    }
}
