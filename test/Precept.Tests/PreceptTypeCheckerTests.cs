using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

public class PreceptTypeCheckerTests
{
    [Fact]
    public void Check_UnknownIdentifier_ProducesC38()
    {
        const string dsl = """
            precept M
            field Total as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set Total = Missing -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C38");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT038");
        result.Diagnostics[0].Message.Should().Contain("unknown identifier 'Missing'");
    }

    [Fact]
    public void Check_NullableAssignmentWithoutGuard_ProducesC42()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C42");
        result.Diagnostics[0].Message.Should().Contain("set target 'Value' type mismatch");
    }

    [Fact]
    public void Check_NullableAssignmentWithGuard_NarrowsSuccessfully()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount != null -> set Value = RetryCount -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_TypeContext_CapturesScopedSymbolsForGuardedTransition()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount != null -> set Value = RetryCount -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.TypeContext.Scopes.Should().NotBeEmpty();
        var exactActionScope = result.TypeContext.Scopes.Single(scope =>
            scope.Line == 7 &&
            scope.ScopeKind == "transition-actions" &&
            scope.StateContext == "A" &&
            scope.EventName == "Go");
        exactActionScope.Symbols.Should().ContainKey("RetryCount");
        exactActionScope.Symbols["RetryCount"].Should().Be(StaticValueKind.Number);
    }

    [Fact]
    public void Validate_ReturnsAllTypeDiagnosticsWithoutThrowing()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field Name as string default ""
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> set Name = Missing -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var result = PreceptCompiler.Validate(model);

        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics.Select(d => d.Constraint.Id).Should().BeEquivalentTo(["C42", "C38"]);
        result.TypeContext.Expressions.Should().NotBeEmpty();
    }

    [Fact]
    public void Check_ContainsRhsTypeMismatch_ProducesC41()
    {
        const string dsl = """
            precept M
            field RequestedFloors as set of number
            state Draft initial
            event RemoveFloor with Floor as string
            from Draft on RemoveFloor when RequestedFloors contains RemoveFloor.Floor -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C41");
        result.Diagnostics[0].Message.Should().Contain("operator 'contains' requires RHS of type number");
    }

    [Fact]
    public void Check_PopIntoWrongTargetType_ProducesC43()
    {
        const string dsl = """
            precept M
            field LastStep as number default 0
            field RepairSteps as stack of string
            state InRepair initial
            event Undo
            from InRepair on Undo when RepairSteps.count > 0 -> pop RepairSteps into LastStep -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C43");
        result.Diagnostics[0].Message.Should().Contain("cannot assign string to target 'LastStep' of type number");
    }

    [Fact]
    public void Check_FromAny_UsesPerStateExpansionAndStateEnsureNarrowing()
    {
        const string dsl = """
            precept M
            field AssignedTechnician as string nullable
            field TechnicianName as string default ""
            state Scheduled initial
            state Open
            in Scheduled ensure AssignedTechnician != null because "scheduled work must have a technician"
            event Snapshot
            from any on Snapshot -> set TechnicianName = AssignedTechnician -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C42");
        result.Diagnostics[0].StateContext.Should().Be("Open");
    }

    [Fact]
    public void Compile_TypeError_IsCompileBlockingWithStableCode()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PRECEPT042*");
    }

    [Fact]
    public void Check_StateAction_SetTypeMismatch_ProducesC39()
    {
        const string dsl = """
            precept M
            field Score as number default 0
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            to Closed -> set Score = "not a number"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C39");
        result.Diagnostics[0].Message.Should().Contain("set target 'Score'");
        result.Diagnostics[0].StateContext.Should().Be("Closed");
    }

    [Fact]
    public void Check_StateAction_NullFlowViolation_ProducesC42()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field MaybeNull as number nullable
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            to Closed -> set Value = MaybeNull
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C42");
        result.Diagnostics[0].StateContext.Should().Be("Closed");
    }

    [Fact]
    public void Check_StateAction_CollectionMutationTypeMismatch_ProducesC39()
    {
        const string dsl = """
            precept M
            field Log as queue of string
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            to Closed -> enqueue Log 42
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C39");
        result.Diagnostics[0].Message.Should().Contain("enqueue Log");
    }

    [Fact]
    public void Check_StateAction_WithStateEnsureNarrowing_NarrowsSuccessfully()
    {
        const string dsl = """
            precept M
            field Value as number default 0
            field MaybeNull as number nullable
            state Open initial
            state Closed
            in Closed ensure MaybeNull != null because "closed requires value"
            event Close
            from Open on Close -> transition Closed
            to Closed -> set Value = MaybeNull
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_StateAction_PopIntoWrongType_ProducesC43()
    {
        const string dsl = """
            precept M
            field LastItem as number default 0
            field Steps as stack of string
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            from Closed -> pop Steps into LastItem
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C43");
        result.Diagnostics[0].Message.Should().Contain("cannot assign string to target 'LastItem' of type number");
    }

    [Fact]
    public void Check_StateAction_ValidSetAndMutation_ProducesNoDiagnostics()
    {
        const string dsl = """
            precept M
            field Score as number default 0
            field Log as queue of string
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            to Closed -> set Score = Score + 1 -> enqueue Log "closed"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_StateAction_Scope_RegisteredAsStateAction()
    {
        const string dsl = """
            precept M
            field Score as number default 0
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            to Closed -> set Score = 1
            """;

        var result = Check(dsl);

        result.TypeContext.Scopes.Should().Contain(s => s.ScopeKind == "state-action" && s.StateContext == "Closed");
    }

    // ─── Phase D: Equality type-compatibility ───────────────────────
    [Fact]
    public void Check_SameTypeEquality_String_Passes()
    {
        const string dsl = """
            precept M
            field Name as string default ""
            state A initial
            state B
            event Go
            from A on Go when Name == "open" -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_SameTypeEquality_Number_Passes()
    {
        const string dsl = """
            precept M
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go when Count == 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_SameTypeEquality_Boolean_Passes()
    {
        const string dsl = """
            precept M
            field Active as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Active == false -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_CrossTypeEquality_StringVsNumber_ProducesC41()
    {
        const string dsl = """
            precept M
            field Name as string default ""
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go when Name == Count -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C41");
        result.Diagnostics[0].Message.Should().Contain("same type");
    }

    [Fact]
    public void Check_NullEquality_NonNullableStringField_ProducesC41()
    {
        const string dsl = """
            precept M
            field Name as string default ""
            state A initial
            state B
            event Go
            from A on Go when Name == null -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C41");
        result.Diagnostics[0].Message.Should().Contain("non-nullable");
    }

    [Fact]
    public void Check_NullEquality_NullableStringField_Passes()
    {
        const string dsl = """
            precept M
            field Tag as string nullable
            state A initial
            state B
            event Go
            from A on Go when Tag != null -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_NullableToNonNullableEquality_SameFamily_Passes()
    {
        const string dsl = """
            precept M
            field Best as string default ""
            field Tag as string nullable
            state A initial
            state B
            event Go
            from A on Go when Tag != Best -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    // ─── Phase E: Bare event arg in transition row ──────────────────
    [Fact]
    public void Check_BareEventArgInTransitionGuard_ProducesC38()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event Go with Count as number
            from A on Go when Count > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C38");
        result.Diagnostics[0].Message.Should().Contain("'Count'");
    }

    [Fact]
    public void Check_DottedEventArgInTransitionGuard_Passes()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event Go with Count as number
            from A on Go when Go.Count > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_DottedEventArgNullNarrowing_NarrowsSuccessfully()
    {
        const string dsl = """
            precept M
            field Total as number default 0
            state A initial
            state B
            event Go with Amount as number nullable
            from A on Go when Go.Amount != null -> set Total = Go.Amount -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_BareEventArgInTransitionSet_ProducesC38()
    {
        const string dsl = """
            precept M
            field Total as number default 0
            state A initial
            state B
            event Go with Amount as number
            from A on Go -> set Total = Amount -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C38");
        result.Diagnostics[0].Message.Should().Contain("'Amount'");
    }

    [Fact]
    public void Check_EventEnsureStillAcceptsBareArgName()
    {
        const string dsl = """
            precept M
            state A initial
            event Go with Count as number
            on Go ensure Count > 0 because "count must be positive"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    // ─── Phase F: Rule-position non-boolean → C46 ───────────────────
    [Fact]
    public void Check_NumericExpressionInGuard_ProducesC46()
    {
        const string dsl = """
            precept M
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go when Count -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C46");
        result.Diagnostics[0].Message.Should().Contain("when predicate");
    }

    [Fact]
    public void Check_StringExpressionInRule_ProducesC46()
    {
        const string dsl = """
            precept M
            field Name as string default ""
            state A initial
            rule Name because "non-boolean invariant"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C46");
        result.Diagnostics[0].Message.Should().Contain("rule");
    }

    [Fact]
    public void Check_BooleanExpressionInGuard_Passes_NoC46()
    {
        const string dsl = """
            precept M
            field Active as boolean default true
            state A initial
            state B
            event Go
            from A on Go when Active -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_NumberInStateEnsure_ProducesC46()
    {
        const string dsl = """
            precept M
            field Balance as number default 10
            state Open initial
            in Open ensure Balance because "non-boolean assert"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C46");
    }

    // ─── Phase G: Duplicate guard detection (C47) ───────────────────
    [Fact]
    public void Check_IdenticalGuardsOnSameStateEvent_ProducesC47()
    {
        const string dsl = """
            precept M
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go when Count > 0 -> transition B
            from A on Go when Count > 0 -> reject "duplicate"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C47");
        result.Diagnostics[0].Message.Should().Contain("Count > 0");
    }

    [Fact]
    public void Check_DifferentGuardsOnSameStateEvent_Passes()
    {
        const string dsl = """
            precept M
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go when Count > 0 -> transition B
            from A on Go when Count == 0 -> reject "zero count"
            from A on Go -> reject "fallback"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_IdenticalGuardNormalizedWhitespace_ProducesC47()
    {
        const string dsl = """
            precept M
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go when Count > 0 -> transition B
            from A on Go when Count  >  0 -> reject "duplicate with extra spaces"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C47");
    }

    private static TypeCheckResult Check(string dsl)
    {
        var model = PreceptParser.Parse(dsl);
        return PreceptTypeChecker.Check(model);
    }

    // ─── Issue #14 Slice 9: When-guard type checker tests ───────────

    [Fact]
    public void Check_Rule_WhenGuardFalse_AtDefaultData_NoPrecompileViolation()
    {
        // EC-3: A guarded rule whose guard is false at default data
        // should NOT produce a pre-compile violation.
        const string dsl = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state A initial
            rule X > 100 when Active because "X must be high when active"
            from A on Go -> no transition
            event Go
            """;

        // This should compile clean — the invariant body (X > 100) fails at defaults,
        // but the guard (Active == false at defaults) means it's skipped.
        var compiled = PreceptCompiler.CompileFromText(dsl);
        compiled.HasErrors.Should().BeFalse("guarded rule with false guard should not trigger pre-compile violation");
    }

    [Fact]
    public void TypeCheck_RuleGuard_ValidBooleanField_NoDiagnostic()
    {
        const string dsl = """
            precept M
            field X as number default 0
            field Active as boolean default false
            state A initial
            rule X >= 0 when Active because "guarded"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_RuleGuard_NonBooleanField_DiagnosticEmitted()
    {
        const string dsl = """
            precept M
            field X as number default 0
            field Count as number default 0
            state A initial
            rule X >= 0 when Count because "count is not boolean"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C46");
    }

    [Fact]
    public void TypeCheck_EventEnsureGuard_ValidArgReference_NoDiagnostic()
    {
        const string dsl = """
            precept M
            state A initial
            state B
            event Submit with Amount as number, Priority as number
            on Submit ensure Amount > 0 when Priority > 1 because "high priority needs amount"
            from A on Submit -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_C69_RuleGuard_EventArgReference_Emitted()
    {
        const string dsl = """
            precept M
            field X as number default 0
            state A initial
            state B
            event Go with Amount as number
            rule X >= 0 when Go.Amount > 0 because "bad guard"
            from A on Go -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C69");
    }

    [Fact]
    public void TypeCheck_C69_EventEnsureGuard_EntityFieldReference_Emitted()
    {
        const string dsl = """
            precept M
            field Total as number default 0
            state A initial
            state B
            event Submit with Amount as number
            on Submit ensure Amount > 0 when Total > 0 because "bad guard scope"
            from A on Submit -> transition B
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C69");
    }

    [Fact]
    public void TypeCheck_EditGuard_ValidFieldReference_NoDiagnostic()
    {
        const string dsl = """
            precept M
            field Priority as number default 0
            field Active as boolean default false
            state Open initial
            in Open when Active edit Priority
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_GuardedStateEnsure_ExcludedFromNarrowing()
    {
        // A guarded state ensure should NOT contribute to type narrowing.
        // Without the guard filter, the assert "MaybeNull != null" would narrow
        // MaybeNull to non-nullable, making `set Value = MaybeNull` pass.
        // With the filter, it remains nullable → C42.
        const string dsl = """
            precept M
            field Value as number default 0
            field MaybeNull as number nullable
            field Active as boolean default false
            state Open initial
            state Closed
            in Closed ensure MaybeNull != null when Active because "conditional guarantee"
            event Close
            from Open on Close -> transition Closed
            to Closed -> set Value = MaybeNull
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C42");
    }
}