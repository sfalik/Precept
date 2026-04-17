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
    public void Check_DivisorFromAny_PartialStateEnsure_C93WithContext()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            state B
            event Go
            event Switch
            in B ensure D > 0 because "D positive in B"
            from A on Switch -> transition B
            from any on Go -> set Y = Y / D -> no transition
            """;
        var result = Check(dsl);
        // Should warn for state A (no ensure) but not state B
        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().ContainSingle();
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
            rule Name because "non-boolean rule"
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
            in Open ensure Balance because "non-boolean ensure"
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

        // This should compile clean — the rule body (X > 100) fails at defaults,
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
        // Without the guard filter, the ensure "MaybeNull != null" would narrow
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

    // ─── Issue #106 Slice 1: Numeric comparison narrowing ───────────

    [Fact]
    public void Check_NumericNarrowing_FieldGreaterThanZero_SqrtNoC76()
    {
        const string dsl = """
            precept M
            field Rate as number default 0
            state A initial
            event Go
            from A on Go when Rate > 0 -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_NumericNarrowing_FieldGreaterEqualZero_SqrtNoC76()
    {
        const string dsl = """
            precept M
            field Rate as number default 0
            state A initial
            event Go
            from A on Go when Rate >= 0 -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_NumericNarrowing_ReversedZeroLessThanField_SqrtNoC76()
    {
        const string dsl = """
            precept M
            field Rate as number default 0
            state A initial
            event Go
            from A on Go when 0 < Rate -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_NumericNarrowing_ReversedZeroLessEqualField_SqrtNoC76()
    {
        const string dsl = """
            precept M
            field Rate as number default 0
            state A initial
            event Go
            from A on Go when 0 <= Rate -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_NumericNarrowing_FieldNotEqualZero_SqrtStillC76()
    {
        // != 0 proves nonzero but NOT nonneg, so sqrt should still emit C76
        const string dsl = """
            precept M
            field Rate as number default 1
            state A initial
            event Go
            from A on Go when Rate != 0 -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C76");
    }

    [Fact]
    public void Check_NumericNarrowing_FieldLessThanZero_SqrtStillC76()
    {
        // < 0 proves nonzero but NOT nonneg, so sqrt should still emit C76
        const string dsl = """
            precept M
            field Rate as number default -1
            state A initial
            event Go
            from A on Go when Rate < 0 -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C76");
    }

    // ─── Issue #106 Slice 2: Or-pattern null-guard decomposition ────

    [Fact]
    public void Check_OrPattern_NullOrPositive_SqrtNoC76()
    {
        const string dsl = """
            precept M
            field Rate as number nullable default null
            state A initial
            event Go
            from A on Go when Rate == null or Rate > 0 -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_OrPattern_PositiveOrNull_SqrtNoC76()
    {
        // Reversed ordering: numeric branch first, null-check second
        const string dsl = """
            precept M
            field Rate as number nullable default null
            state A initial
            event Go
            from A on Go when Rate > 0 or Rate == null -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_OrPattern_NullLiteralOnLeft_SqrtNoC76()
    {
        // null == Field instead of Field == null
        const string dsl = """
            precept M
            field Rate as number nullable default null
            state A initial
            event Go
            from A on Go when null == Rate or Rate > 0 -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_OrPattern_NullableNonnegativeConstraint_SqrtNoC76()
    {
        // nonnegative constraint on nullable field desugars to Rate == null or Rate >= 0
        const string dsl = """
            precept M
            field Rate as number nullable nonnegative default null
            state A initial
            event Go
            from A on Go when Rate != null -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_OrPattern_BothNullChecks_NoNumericProof()
    {
        // Both branches are null checks — no numeric proof to extract.
        // C77 fires (nullable argument) because the or-pattern doesn't narrow away null.
        const string dsl = """
            precept M
            field Rate as number nullable default null
            state A initial
            event Go
            from A on Go when Rate == null or Rate == null -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C76" || d.Constraint.Id == "C77");
    }

    [Fact]
    public void Check_OrPattern_CompoundAnd_SqrtNoC76()
    {
        // Compound: Field == null or (Field >= 0 and Field < 100)
        const string dsl = """
            precept M
            field Rate as number nullable default null
            state A initial
            event Go
            from A on Go when Rate == null or (Rate >= 0 and Rate < 100) -> set Rate = sqrt(Rate) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_OrPattern_CrossField_SqrtStillC76()
    {
        // Different fields in null-check vs numeric branch — no proof extracted.
        // C77 fires (nullable argument) because there's no null-guard for B.
        const string dsl = """
            precept M
            field A as number nullable default null
            field B as number nullable default null
            state S initial
            event Go
            from S on Go when A == null or B >= 0 -> set B = sqrt(B) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C76" || d.Constraint.Id == "C77");
    }

    // ─── Issue #106 Slice 3: Guarded rule exclusion from proof iteration ────

    [Fact]
    public void Check_GuardedRule_ExcludedFromProofIteration_SqrtStillC76()
    {
        // A guarded rule (rule D >= 0 when IsActive) should NOT inject $nonneg: unconditionally.
        // The fact D >= 0 only holds when IsActive is true — injecting it always would be unsound.
        // sqrt(D) should still produce C76 because no unconditional proof exists.
        const string dsl = """
            precept Test
            field D as number default 1
            field IsActive as boolean default true
            state Active initial
            state Done
            event Finish
            event Check
            from Active on Finish -> transition Done
            rule D >= 0 when IsActive because "D nonneg only when active"
            from Active on Check when sqrt(D) > 0 -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C76");
    }

    // ─── Issue #106 Slice 4: Event ensure narrowing with name translation ───────

    [Fact]
    public void Check_EventEnsureNonneg_ProvesSqrt_NoC76()
    {
        const string dsl = """
            precept Test
            field Result as number default 0
            event Submit with Days as number
            on Submit ensure Days >= 0 because "nonneg"
            state Draft initial
            from Draft on Submit -> set Result = sqrt(Submit.Days) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void Check_GuardedEventEnsure_ExcludedFromProof_SqrtC76()
    {
        const string dsl = """
            precept Test
            field Result as number default 0
            event Submit with Days as number
            on Submit ensure Days >= 0 when Days != null because "conditional nonneg"
            state Draft initial
            from Draft on Submit -> set Result = sqrt(Submit.Days) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C76");
    }

    [Fact]
    public void Check_EventArgPositiveConstraint_ProvesSqrt_NoC76()
    {
        const string dsl = """
            precept Test
            field Result as number default 0
            event Submit with Days as number positive
            state Draft initial
            from Draft on Submit -> set Result = sqrt(Submit.Days) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    // ─── Issue #106 Slice 5: Divisor safety diagnostics (C92/C93) ───────

    // A. Proof source × operator theory

    [Theory]
    [InlineData("positive", "/", false, null)]
    [InlineData("positive", "%", false, null)]
    [InlineData("nonnegative", "/", true, "nonnegative but not nonzero")]
    [InlineData("nonnegative", "%", true, "nonnegative but not nonzero")]
    [InlineData("min 1", "/", false, null)]
    [InlineData("min 1", "%", false, null)]
    [InlineData("min 0", "/", true, "nonnegative but not nonzero")]
    public void Check_DivisorProofSource_Theory(string constraint, string op, bool expectC93, string? messageFragment)
    {
        var dsl = $"""
            precept M
            field Y as number default 10
            field D as number default 1 {constraint}
            state A initial
            event Go
            from A on Go -> set Y = Y {op} D -> no transition
            """;

        var result = Check(dsl);

        var c93 = result.Diagnostics.Where(d => d.Constraint.Id == "C93").ToList();
        if (expectC93)
        {
            c93.Should().ContainSingle();
            if (messageFragment is not null)
                c93[0].Message.Should().Contain(messageFragment);
        }
        else
        {
            c93.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("rule D > 0 because \"pos\"", "/")]
    [InlineData("rule D != 0 because \"nonzero\"", "/")]
    public void Check_DivisorRuleProof_Clean(string rule, string op)
    {
        var dsl = $"""
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            event Go
            {rule}
            from A on Go -> set Y = Y {op} D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Theory]
    [InlineData("when D != 0", "/")]
    [InlineData("when D > 0", "/")]
    public void Check_DivisorGuardProof_Clean(string guard, string op)
    {
        var dsl = $"""
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            event Go
            from A on Go {guard} -> set Y = Y {op} D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorStateEnsureProof_Clean()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            state B
            event Go
            event Next
            in B ensure D > 0 because "D pos in B"
            from A on Go -> transition B
            from B on Next -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("%")]
    public void Check_DivisorNoProof_EmitsC93(string op)
    {
        var dsl = $"""
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            event Go
            from A on Go -> set Y = Y {op} D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C93");
    }

    // B. Field types

    [Theory]
    [InlineData("number")]
    [InlineData("integer")]
    [InlineData("decimal")]
    public void Check_DivisorFieldType_PositiveClean(string type)
    {
        var dsl = $"""
            precept M
            field Y as {type} default 10
            field D as {type} default 1 positive
            state A initial
            event Go
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // C. Literal divisors

    [Theory]
    [InlineData("Y / 0", true)]
    [InlineData("Y / 0.0", true)]
    [InlineData("Y % 0", true)]
    [InlineData("Y / 2", false)]
    [InlineData("Y / -1", false)]
    [InlineData("Y / 100", false)]
    public void Check_DivisorLiteral_Theory(string expression, bool expectC92)
    {
        var dsl = $"""
            precept M
            field Y as number default 10
            state A initial
            event Go
            from A on Go -> set Y = {expression} -> no transition
            """;

        var result = Check(dsl);

        if (expectC92)
            result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C92");
        else
            result.Diagnostics.Where(d => d.Constraint.Id == "C92").Should().BeEmpty();
    }

    // D. Compound expression divisors

    [Fact]
    public void Check_DivisorCompound_Addition_NoWarning()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            event Go
            from A on Go -> set Y = Y / (D + 1) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id is "C92" or "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorCompound_Multiplication_NoWarning()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1
            field C as number default 2
            state A initial
            event Go
            from A on Go -> set Y = Y / (D * C) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id is "C92" or "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorCompound_Subtraction_NoWarning()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            event Go
            from A on Go -> set Y = Y / (D - D) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id is "C92" or "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorCompound_AbsFunction_NoWarning()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1
            state A initial
            event Go
            from A on Go -> set Y = Y / abs(D) -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id is "C92" or "C93").Should().BeEmpty();
    }

    // E. Event arg divisors

    [Fact]
    public void Check_DivisorEventArg_EnsurePositive_NoC93()
    {
        const string dsl = """
            precept M
            field Result as number default 0
            event Calc with Divisor as number
            on Calc ensure Divisor > 0 because "pos"
            state A initial
            from A on Calc -> set Result = Result / Calc.Divisor -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorEventArg_NoProof_C93()
    {
        const string dsl = """
            precept M
            field Result as number default 0
            event Calc with Divisor as number
            state A initial
            from A on Calc -> set Result = Result / Calc.Divisor -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_DivisorEventArg_GuardNonzero_NoC93()
    {
        const string dsl = """
            precept M
            field Result as number default 0
            event Calc with Divisor as number
            state A initial
            from A on Calc when Calc.Divisor != 0 -> set Result = Result / Calc.Divisor -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorEventArg_PositiveConstraint_NoC93()
    {
        const string dsl = """
            precept M
            field Result as number default 0
            event Calc with Divisor as number positive
            state A initial
            from A on Calc -> set Result = Result / Calc.Divisor -> no transition
            """;
        var result = Check(dsl);
        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // F. Nullable fields

    [Fact]
    public void Check_DivisorNullable_NoGuard_C41()
    {
        // Nullable field without guard fails the numeric operand check (C41) before
        // the divisor check can fire. number? is not a numeric kind.
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number nullable default null
            state A initial
            event Go
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C41");
    }

    [Fact]
    public void Check_DivisorNullable_NullGuardOnly_C93Only()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number nullable default null
            state A initial
            event Go
            from A on Go when D != null -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C42").Should().BeEmpty();
        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_DivisorNullable_CompoundGuard_Clean()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number nullable default null
            state A initial
            event Go
            from A on Go when D != null and D > 0 -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id is "C42" or "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorNullable_PositiveConstraint_Clean()
    {
        // positive constraint on nullable desugars via or-pattern: D == null or D > 0
        // This proves both nonneg and nonzero, so no C93.
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number nullable positive default null
            state A initial
            event Go
            from A on Go when D != null -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // G. min 0 vs min 1 boundary

    [Fact]
    public void Check_DivisorMin0_C93()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 0 min 0
            state A initial
            event Go
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C93");
    }

    [Fact]
    public void Check_DivisorMin1_Clean()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1 min 1
            state A initial
            event Go
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // H. Context-aware C93 message

    [Fact]
    public void Check_DivisorNonnegative_ContextAwareMessage()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 0 nonnegative
            state A initial
            event Go
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        var c93 = result.Diagnostics.Where(d => d.Constraint.Id == "C93").ToList();
        c93.Should().ContainSingle();
        c93[0].Message.Should().Contain("nonnegative but not nonzero");
    }

    // I. Computed field

    [Fact]
    public void Check_DivisorComputedField_NoProof_C93()
    {
        const string dsl = """
            precept M
            field Amount as number default 100
            field Rate as number default 1
            field Ratio as number -> Amount / Rate
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C93");
    }

    // J. Intra-row mutation false-negative (known limitation)

    [Fact]
    public void Check_DivisorIntraRowMutation_KnownLimitation_NoC93()
    {
        // Known limitation: set Rate = 0 followed by set X = A / Rate in the same row
        // uses the pre-transition proof state, not the mutated value.
        // No C93 is emitted because the type checker doesn't track intra-row mutations.
        const string dsl = """
            precept M
            field A as number default 10
            field Rate as number default 1 positive
            field X as number default 0
            state S initial
            event Go
            from S on Go -> set Rate = 0 -> set X = A / Rate -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // K. Multiple/redundant proofs

    [Fact]
    public void Check_DivisorRedundantProofs_PositiveAndRule_Clean()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1 positive
            state A initial
            event Go
            rule D > 0 because "redundant"
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    [Fact]
    public void Check_DivisorComplementaryProofs_NonnegAndRule_Clean()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 1 nonnegative
            state A initial
            event Go
            rule D != 0 because "nonzero"
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        result.Diagnostics.Where(d => d.Constraint.Id == "C93").Should().BeEmpty();
    }

    // L. Severity

    [Fact]
    public void Check_C93_IsError_NotWarning()
    {
        const string dsl = """
            precept M
            field Y as number default 10
            field D as number default 0
            state A initial
            event Go
            from A on Go -> set Y = Y / D -> no transition
            """;

        var result = Check(dsl);

        var c93 = result.Diagnostics.Single(d => d.Constraint.Id == "C93");
        c93.Constraint.Severity.Should().Be(ConstraintSeverity.Error);
    }
}