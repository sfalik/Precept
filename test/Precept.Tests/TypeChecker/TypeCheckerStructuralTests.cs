using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 6 — Structural Validation.
/// Covers IsSet/IsNotSet postfix op resolution on optional fields (IsSetOnNonOptional),
/// computed-field cycle detection via three-color DFS (CircularComputedField),
/// choice domain validation (EmptyChoice, DuplicateChoiceValue),
/// and forward-reference belt-and-suspenders (DefaultForwardReference).
/// </summary>
public class TypeCheckerStructuralTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Category 1: IsSet / IsNotSet on optional fields
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalField_IsSet_InGuard_ResolvesToBoolean_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string optional
            state Draft initial
            state Done
            event Submit
            from Draft on Submit when Name is set -> transition Done
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("optional field 'is set' in guard should resolve cleanly");
    }

    [Fact]
    public void OptionalField_IsNotSet_InGuard_ResolvesToBoolean_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string optional
            state Draft initial
            state Done
            event Submit
            from Draft on Submit when Name is not set -> transition Done
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("optional field 'is not set' in guard should resolve cleanly");
    }

    [Fact]
    public void NonOptionalField_IsSet_InGuard_EmitsIsSetOnNonOptional()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            state Done
            event Submit
            from Draft on Submit when Count is set -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.IsSetOnNonOptional);
    }

    [Fact]
    public void NonOptionalField_IsNotSet_InGuard_EmitsIsSetOnNonOptional()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            state Done
            event Submit
            from Draft on Submit when Count is not set -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.IsSetOnNonOptional);
    }

    [Fact]
    public void IsSet_OnErrorExpression_ReturnsErrorExpression_NoSecondDiagnostic()
    {
        // Reference an undeclared field in a guard — identifier resolution produces
        // TypedErrorExpression. PostfixOp D13 propagation should return TypedErrorExpression
        // without emitting a second IsSetOnNonOptional diagnostic.
        var precept = """
            precept Widget
            state Draft initial
            state Done
            event Submit
            from Draft on Submit when BogusField is set -> transition Done
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Select(d => d.Code)
            .Should().Contain(nameof(DiagnosticCode.UndeclaredField),
                "the undeclared identifier should be caught")
            .And.NotContain(nameof(DiagnosticCode.IsSetOnNonOptional),
                "D13 error propagation should suppress the second diagnostic");
    }

    [Fact]
    public void OptionalField_IsSet_InGuardAlone_ResolvesCleanly()
    {
        // IsSet as sole guard expression — no compound operator involved.
        var precept = """
            precept Widget
            field Name as string optional
            field Count as number default 0
            state Draft initial
            state Done
            event Submit
            from Draft on Submit when Name is set -> transition Done
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("'is set' as sole guard should resolve cleanly");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 2: Computed-field cycle detection
    // ════════════════════════════════════════════════════════════════════════
    //
    // NOTE: ComputedDeps is currently empty because computed expression resolution
    // is not yet wired (Slice 2+ deferred). These tests document the expected behavior
    // once computed expressions populate ComputedDeps. Until then, cycle detection
    // and forward-ref checks are no-ops (correct behavior per george-slice-6-done.md).

    [Fact]
    public void TwoFields_NoCycle_NoDiagnostic()
    {
        // Two independent fields — no computed deps → no cycle.
        var precept = """
            precept Widget
            field A as number default 0
            field B as number default 0
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.CircularComputedField))
            .Should().BeEmpty("independent fields have no cycle");
    }

    [Fact]
    public void DiamondDependency_NoCycle_NoDiagnostic()
    {
        // Diamond: A→B, A→C, B→D, C→D — this is NOT a cycle, just shared deps.
        // Currently ComputedDeps is empty so this is a no-op; documents expected behavior.
        var precept = """
            precept Widget
            field D as number default 0
            field B as number default 0
            field C as number default 0
            field A as number default 0
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.CircularComputedField))
            .Should().BeEmpty("diamond dependency is not a cycle");
    }

    [Fact]
    public void NoPrecept_NoComputedDeps_CycleDetection_IsNoOp()
    {
        // Minimal precept with no computed fields — ValidateStructural runs but
        // ComputedDeps is empty, so DFS never fires.
        var precept = """
            precept Widget
            field X as number default 0
            state Idle initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.CircularComputedField))
            .Should().BeEmpty("no computed fields means no cycles to detect");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 3: Choice domain validation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ChoiceField_ValidDistinctValues_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Priority as choice of string("Low","Medium","High") default "Low"
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("valid choice domain with distinct values should be clean");
    }

    [Fact]
    public void ChoiceField_SingleValue_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active") default "Active"
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.EmptyChoice))
            .Should().BeEmpty("single-value choice is valid");
        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.DuplicateChoiceValue))
            .Should().BeEmpty("single-value choice has no duplicates");
    }

    [Fact]
    public void ChoiceField_EmptyDomain_EmitsEmptyChoice()
    {
        var precept = """
            precept Widget
            field Status as choice of string() default "x"
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.EmptyChoice);
    }

    [Fact]
    public void ChoiceField_DuplicateValues_EmitsDuplicateChoiceValue()
    {
        var precept = """
            precept Widget
            field Priority as choice of string("Low","High","Low") default "Low"
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DuplicateChoiceValue);
    }

    [Fact]
    public void ChoiceField_MultipleDuplicates_EmitsDuplicateChoiceValueForEach()
    {
        var precept = """
            precept Widget
            field Priority as choice of string("A","B","A","B") default "A"
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.DuplicateChoiceValue))
            .Should().HaveCountGreaterThanOrEqualTo(2,
                "each duplicate occurrence should emit its own diagnostic");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 4: Forward-reference belt-and-suspenders
    // ════════════════════════════════════════════════════════════════════════
    //
    // Forward-reference validation in ValidateStructural reads ComputedDeps.
    // With computed/default expression wiring in place, default-expression dependencies
    // now contribute forward-reference diagnostics here as well.

    [Fact]
    public void FieldDeclaredBeforeDependency_NoDiagnostic()
    {
        // Standard declaration order — no forward reference.
        var precept = """
            precept Widget
            field Base as number default 0
            field Tax as number default 0
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference))
            .Should().BeEmpty("fields declared in order should not trigger forward-ref");
    }

    [Fact]
    public void ForwardReference_InDefaultExpression_EmitsForwardRefDiagnostic()
    {
        // D8: field Total references field SubTotal declared after Total.
        // With default-expression dependency wiring in place, ValidateStructural
        // should surface the forward-reference diagnostic.
        var precept = """
            precept Widget
            field Total as number default SubTotal
            field SubTotal as number default 0
            state Open initial
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference),
                "computed/default dependencies now participate in forward-reference validation");
    }

    [Fact]
    public void NoFields_NoForwardRefDiagnostic()
    {
        var precept = """
            precept Widget
            state Open initial
            event Ping
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference))
            .Should().BeEmpty("no fields means no forward references");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category: PRE0092 — EventHandlerInStatefulPrecept
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventHandler_InStatefulPrecept_EmitsEventHandlerInStatefulPrecept()
    {
        var precept = """
            precept Widget
            field Name as string
            state Draft initial
            state Done
            event Rename
            on Rename -> set Name = Rename.Name
            from Draft on Rename -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.EventHandlerInStatefulPrecept);
    }

    [Fact]
    public void EventHandler_InStatelessPrecept_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            event Rename
            on Rename -> set Name = Rename.Name
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.EventHandlerInStatefulPrecept))
            .Should().BeEmpty("event handlers are valid in stateless precepts");
    }

    [Fact]
    public void EventHandler_MultipleHandlers_InStatefulPrecept_EmitsForEach()
    {
        var precept = """
            precept Widget
            field Name as string
            field Count as number
            state Draft initial
            state Done
            event Rename
            event Increment
            on Rename -> set Name = Rename.Name
            on Increment -> set Count = Count + 1
            from Draft on Rename -> transition Done
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.EventHandlerInStatefulPrecept))
            .Should().HaveCount(2, "each event handler in a stateful precept should emit PRE0092");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 6: Choice value validation (PRE0086, PRE0087, PRE0089)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ChoiceField_LiteralNotInSet_EmitsChoiceLiteralNotInSet()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active", "Done") default "Active"
            state Open initial
            state Closed
            event Close
            from Open on Close when Status == "Pending" -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ChoiceLiteralNotInSet);
    }

    [Fact]
    public void ChoiceField_ValidLiteral_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active", "Done") default "Active"
            state Open initial
            state Closed
            event Close
            from Open on Close when Status == "Active" -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void ChoiceField_LiteralCaseMismatch_EmitsChoiceLiteralNotInSet()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active", "Done") default "Active"
            state Open initial
            state Closed
            event Close
            from Open on Close when Status == "active" -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ChoiceLiteralNotInSet);
    }

    [Fact]
    public void ChoiceArg_ValueOutsideFieldSet_EmitsChoiceArgOutsideFieldSet()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active", "Done") default "Active"
            state Open initial
            state Closed
            event Update(NewStatus as choice of string("Active", "Pending"))
            from Open on Update
                -> set Status = Update.NewStatus
                -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ChoiceArgOutsideFieldSet);
    }

    [Fact]
    public void ChoiceArg_ValuesSubsetOfFieldSet_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active", "Done") default "Active"
            state Open initial
            state Closed
            event Update(NewStatus as choice of string("Active", "Done"))
            from Open on Update
                -> set Status = Update.NewStatus
                -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void ChoiceArg_RankConflictsWithField_EmitsChoiceRankConflict()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active", "Done", "Archived") default "Active"
            state Open initial
            state Closed
            event Update(NewStatus as choice of string("Done", "Active"))
            from Open on Update
                -> set Status = Update.NewStatus
                -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ChoiceRankConflict);
    }

    [Fact]
    public void ChoiceArg_RankMatchesField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Active", "Done", "Archived") default "Active"
            state Open initial
            state Closed
            event Update(NewStatus as choice of string("Active", "Done"))
            from Open on Update
                -> set Status = Update.NewStatus
                -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: ComputedFieldWithDefault (PRE0039)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputedField_WithDefaultExpression_EmitsComputedFieldWithDefault()
    {
        var precept = """
            precept Widget
            field Price as decimal default 10
            field Tax as decimal default 0 <- Price * 0.1
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ComputedFieldWithDefault);
    }

    [Fact]
    public void ComputedField_WithoutDefault_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Price as decimal default 10
            field Tax as decimal <- Price * 0.1
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: DuplicateArgName (PRE0027)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Event_DuplicateArgName_EmitsDuplicateArgName()
    {
        var precept = """
            precept Widget
            field Name as string default "x"
            state Open initial
            state Done
            event Submit(Name as string, Name as string)
            from Open on Submit -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DuplicateArgName);
    }

    [Fact]
    public void Event_UniqueArgNames_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string default "x"
            state Open initial
            state Done
            event Submit(FirstName as string, LastName as string)
            from Open on Submit -> set Name = Submit.FirstName -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: NonChoiceAssignedToChoice / ValueNotInChoiceSet (PRE0085)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ChoiceField_AssignNonChoiceValue_EmitsValueNotInChoiceSet()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Open", "Closed") default "Open"
            state Active initial
            state Done
            event Update(NewStatus as string)
            from Active on Update -> set Status = Update.NewStatus -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.NonChoiceAssignedToChoice);
    }

    [Fact]
    public void ChoiceField_AssignChoiceValue_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Status as choice of string("Open", "Closed") default "Open"
            state Active initial
            state Done
            event Update(NewStatus as choice of string("Open", "Closed"))
            from Active on Update -> set Status = Update.NewStatus -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
