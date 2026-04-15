using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the declarative rules feature across all four rule positions:
/// field rules, top-level rules, state rules, and event rules.
/// </summary>
public class PreceptRulesTests
{
    // ========================================================================================
    // PARSING — model structure
    // ========================================================================================

    [Fact]
    public void Parse_FieldRule_AttachedToField()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            rule Balance >= 0 because "Must be non-negative"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        var inv = machine.Rules.Should().ContainSingle().Subject;
        inv.ExpressionText.Should().Be("Balance >= 0");
        inv.Reason.Should().Be("Must be non-negative");
    }

    [Fact]
    public void Parse_FieldRule_MultipleRulesOnSameField()
    {
        const string dsl = """
            precept Test
            field Rating as number default 1
            rule Rating >= 1 because "Too low"
            rule Rating <= 5 because "Too high"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Rules.Should().HaveCount(2);
        machine.Rules![0].Reason.Should().Be("Too low");
        machine.Rules[1].Reason.Should().Be("Too high");
    }

    [Fact]
    public void Parse_StateRule_AttachedToState()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Idle initial
            state Paid
            in Paid ensure AmountPaid > 0 because "Must have paid"
            """;

        var machine = PreceptParser.Parse(dsl);

        var stateEnsure = machine.StateEnsures.Should().ContainSingle().Subject;
        stateEnsure.State.Should().Be("Paid");
        stateEnsure.ExpressionText.Should().Be("AmountPaid > 0");
        stateEnsure.Reason.Should().Be("Must have paid");
    }

    [Fact]
    public void Parse_StateRule_MultipleStatesWithRules()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state A initial
            in A ensure Score >= 0 because "Non-negative in A"
            state B
            in B ensure Score >= 10 because "Must be 10 in B"
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.StateEnsures.Should().HaveCount(2);
        machine.StateEnsures!.Single(sa => sa.State == "A").Reason.Should().Be("Non-negative in A");
        machine.StateEnsures!.Single(sa => sa.State == "B").Reason.Should().Be("Must be 10 in B");
    }

    [Fact]
    public void Parse_EventRule_AttachedToEvent()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            """;

        var machine = PreceptParser.Parse(dsl);

        var eventEnsure = machine.EventEnsures.Should().ContainSingle().Subject;
        eventEnsure.EventName.Should().Be("Pay");
        eventEnsure.ExpressionText.Should().Be("Amount > 0");
        eventEnsure.Reason.Should().Be("Amount must be positive");
    }

    [Fact]
    public void Parse_EventRule_ReferencingPrefixedArgName()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Pay with Amount as number
            on Pay ensure Pay.Amount > 0 because "Amount must be positive"
            """;

        var machine = PreceptParser.Parse(dsl);

        var eventEnsure = machine.EventEnsures.Should().ContainSingle().Subject;
        eventEnsure.EventName.Should().Be("Pay");
        eventEnsure.ExpressionText.Should().Be("Pay.Amount > 0");
    }

    [Fact]
    public void Parse_CollectionFieldRule_AttachedToCollection()
    {
        const string dsl = """
            precept Test
            field Approvers as set of string
            rule Approvers.count >= 1 because "Need at least one approver"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        var inv = machine.Rules.Should().ContainSingle().Subject;
        inv.ExpressionText.Should().Be("Approvers.count >= 1");
        inv.Reason.Should().Be("Need at least one approver");
    }

    [Fact]
    public void Parse_FieldRule_SourceLineIsCorrect()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            rule Balance >= 0 because "Must be non-negative"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        var inv = machine.Rules.Should().ContainSingle().Subject;
        inv.SourceLine.Should().Be(3);
    }

    // ========================================================================================
    // PARSING — scope restrictions
    // ========================================================================================

    [Fact]
    public void Parse_FieldRule_ReferencingAnotherField_Throws()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            field Limit as number default 1000
            rule Balance <= Limit because "Cannot exceed limit"
            state Idle initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        // Rules can reference any declared field — not scoped to a single field
        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_FieldRule_ReferencingOwnField_IsAllowed()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            rule Balance >= 0 because "Must be non-negative"
            state Idle initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_FieldRule_DottedPropertyOfOwnField_IsAllowed()
    {
        const string dsl = """
            precept Test
            field Tags as set of string
            rule Tags.count <= 10 because "Too many tags"
            state Idle initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_EventRule_ReferencingInstanceDataField_Throws()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            state Idle initial
            event Pay with Amount as number
            on Pay ensure Amount <= Balance because "Cannot exceed balance"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*event argument identifiers*");
    }

    [Fact]
    public void Parse_EventRule_ReferencingOnlyEventArgs_IsAllowed()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Pay with Amount as number, Discount as number
            on Pay ensure Amount > Discount because "Amount must exceed discount"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    // ========================================================================================
    // COMPILE-TIME VALIDATION — field default values
    // ========================================================================================

    [Fact]
    public void Compile_FieldRule_DefaultValueViolatesRule_Throws()
    {
        // Balance starts at 0, which violates rule >= 10
        const string dsl = """
            precept Test
            field Balance as number default 0
            rule Balance >= 10 because "Balance must be at least 10"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rule violation*Balance must be at least 10*");
    }

    [Fact]
    public void Compile_FieldRule_DefaultValueSatisfiesRule_Succeeds()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Must be non-negative"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_TopLevelRule_DefaultValueViolatesRule_Throws()
    {
        const string dsl = """
            precept Test
            field Quantity as number default 0
            field UnitPrice as number default 10
            field TotalPrice as number default 999
            rule Quantity * UnitPrice == TotalPrice because "Price must be consistent"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rule violation*Price must be consistent*");
    }

    [Fact]
    public void Compile_TopLevelRule_DefaultValuesSatisfyRule_Succeeds()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            field Limit as number default 1000
            rule Balance <= Limit because "Cannot exceed limit"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_InitialStateRule_ViolatedByDefaultData_Throws()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Paid initial
            in Paid ensure AmountPaid > 0 because "Must have paid"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*state ensure violation*Must have paid*initial state*");
    }

    [Fact]
    public void Compile_NonInitialStateRule_NotCheckedAtCompileTime_Succeeds()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Idle initial
            state Paid
            in Paid ensure AmountPaid > 0 because "Must have paid"
            event Pay
            from Idle on Pay -> transition Paid
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_CollectionRule_ViolatedAtCreation_Throws()
    {
        const string dsl = """
            precept Test
            field Approvers as set of string
            rule Approvers.count >= 1 because "Need at least one approver"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rule violation*Need at least one approver*");
    }

    [Fact]
    public void Compile_CollectionRule_SatisfiedAtCreation_Succeeds()
    {
        const string dsl = """
            precept Test
            field Tags as set of string
            rule Tags.count <= 10 because "Too many tags"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_EventRule_DefaultArgValueViolatesRule_Throws()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Submit with Priority as number default 0
            on Submit ensure Priority > 0 because "Priority must be positive"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*event ensure violation*Priority must be positive*");
    }

    [Fact]
    public void Compile_EventRule_AllArgsHaveDefaultsAndPass_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Submit with Priority as number default 1
            on Submit ensure Priority > 0 because "Priority must be positive"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_EventRule_RequiredArgNotDefaulted_SkipsCompileTimeCheck_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Submit with Priority as number
            on Submit ensure Priority > 0 because "Priority must be positive"
            """;

        // Cannot check at compile time because Priority has no default
        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_LiteralSetAssignment_ViolatesFieldRule_Throws()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Must be non-negative"
            state Active initial
            event Reset
            from Active on Reset -> set Balance = -1 -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*literal assignment*violates rule*Must be non-negative*");
    }

    [Fact]
    public void Compile_LiteralSetAssignment_SatisfiesFieldRule_Succeeds()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Must be non-negative"
            state Active initial
            event Adjust
            from Active on Adjust -> set Balance = 50 -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    // ========================================================================================
    // COMPILE-TIME — duplicate and subsumed state ensure detection
    // ========================================================================================

    [Fact]
    public void Compile_DuplicateStateEnsure_SamePrepositionStateExpression_Throws()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Idle initial
            state Active
            in Active ensure Score >= 0 because "first"
            in Active ensure Score >= 0 because "duplicate"
            event Go
            from Idle on Go -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate state ensure*in Active ensure*Score >= 0*");
    }

    [Fact]
    public void Compile_DuplicateStateEnsure_DifferentPreposition_Succeeds()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Idle initial
            state Active
            in Active ensure Score >= 0 because "in-scope"
            to Active ensure Score >= 0 because "entry-scope"
            event Go
            from Idle on Go -> transition Active
            """;

        // Not a duplicate (different prepositions) — but subsumption will catch this
        // This test verifies the duplicate check alone doesn't flag it
        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // This will throw for subsumption (C45), not duplicate (C44)
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Subsumed state ensure*");
    }

    [Fact]
    public void Compile_DuplicateStateEnsure_DifferentExpression_Succeeds()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            field Limit as number default 100
            state Idle initial
            state Active
            in Active ensure Score >= 0 because "score check"
            in Active ensure Limit > 0 because "limit check"
            event Go
            from Idle on Go -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_DuplicateStateEnsure_DifferentState_Succeeds()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Idle initial
            state Active
            state Done
            in Active ensure Score >= 0 because "active check"
            in Done ensure Score >= 0 because "done check"
            event Go
            from Idle on Go -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_SubsumedEnsure_ToRedundantWithIn_Throws()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Idle initial
            state Active
            in Active ensure Score >= 0 because "in covers entry and in-place"
            to Active ensure Score >= 0 because "to only covers entry — redundant"
            event Go
            from Idle on Go -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Subsumed state ensure*to Active ensure*Score >= 0*redundant*in Active ensure*");
    }

    [Fact]
    public void Compile_SubsumedEnsure_DifferentExpression_Succeeds()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            field Limit as number default 100
            state Idle initial
            state Active
            in Active ensure Score >= 0 because "score check"
            to Active ensure Limit > 0 because "limit check"
            event Go
            from Idle on Go -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_SubsumedEnsure_FromNotSubsumedByIn_Succeeds()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Idle initial
            state Active
            state Done
            in Active ensure Score >= 0 because "in-active check"
            from Active ensure Score >= 0 because "exit check is distinct"
            event Go
            from Idle on Go -> transition Active
            from Active on Go -> transition Done
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_SubsumedEnsure_ToWithoutMatchingIn_Succeeds()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Idle initial
            state Active
            to Active ensure Score >= 0 because "entry only"
            event Go
            from Idle on Go -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    // ========================================================================================
    // RUNTIME — event rules (checked before guard, fire path)
    // ========================================================================================

    [Fact]
    public void Fire_EventRule_Violated_IsBlocked()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Amount must be positive");
    }

    [Fact]
    public void Fire_EventRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.NewState.Should().Be("Done");
    }

    [Fact]
    public void Fire_EventRule_ViolatedBeforeGuardIsEvaluated_IsBlocked()
    {
        // Even when the guard would reject, event rules are reported first
        const string dsl = """
            precept Test
            field Balance as number default 0
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay when Balance > 1000 -> transition Done
            from Idle on Pay -> reject "Not enough balance"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?> { ["Balance"] = 0.0 });

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Amount must be positive");
    }

    [Fact]
    public void Fire_EventRule_MultipleViolations_AllReported()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Transfer with Amount as number, Fee as number
            on Transfer ensure Amount > 0 because "Amount must be positive"
            on Transfer ensure Fee >= 0 because "Fee must be non-negative"
            from Idle on Transfer -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Transfer", new Dictionary<string, object?>
        {
            ["Amount"] = -100.0,
            ["Fee"] = -5.0
        });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().HaveCount(2);
        result.Violations.Should().Contain(v => v.Message.Contains("Amount must be positive", StringComparison.OrdinalIgnoreCase));
        result.Violations.Should().Contain(v => v.Message.Contains("Fee must be non-negative", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Fire_EventRule_EventArgNameShadowsMachineField_ArgValueIsUsed()
    {
        // Regression: event arg has the same bare name as a machine field (e.g. CreditScore).
        // The event rule must evaluate against the supplied arg value, not the machine field value.
        // Previously the machine field (= 0) shadowed the arg (= 500) and the rule falsely failed.
        const string dsl = """
            precept Test
            field CreditScore as number default 0
            state Apply initial
            state UnderReview
            event Submit with CreditScore as number
            on Submit ensure CreditScore >= 300 because "Credit score must be at least 300"
            from Apply on Submit -> transition UnderReview
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Apply", new Dictionary<string, object?> { ["CreditScore"] = 0.0 });

        // Arg value 500 satisfies the rule even though the machine field is 0
        var passing = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 500.0 });
        (passing.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();

        // Arg value 100 violates the rule
        var failing = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 100.0 });
        (failing.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        failing.Violations.Should().ContainSingle().Which.Message.Should().Contain("Credit score must be at least 300");
    }

    // ========================================================================================
    // RUNTIME — field rules (checked after set execution)
    // ========================================================================================

    [Fact]
    public void Fire_FieldRule_ViolatedAfterSetExecution_IsBlocked()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Balance must not go negative");
    }

    [Fact]
    public void Fire_FieldRule_Satisfied_CommitsAndAccepts()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 30.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().Be(70.0);
    }

    [Fact]
    public void Fire_FieldRule_Violated_SetMutationsRolledBack()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            field TransactionCount as number default 0
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> set TransactionCount = TransactionCount + 1 -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?>
        {
            ["Balance"] = 100.0,
            ["TransactionCount"] = 0.0
        });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        // UpdatedInstance is null on rejection meaning original data is unchanged
        result.UpdatedInstance.Should().BeNull();
    }

    [Fact]
    public void Fire_FieldRule_MultipleViolations_AllReported()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            field Quantity as number default 5
            rule Quantity >= 0 because "Quantity must be non-negative"
            state Active initial
            event BadEvent with BalanceAdjust as number, QuantityAdjust as number
            from Active on BadEvent -> set Balance = Balance + BadEvent.BalanceAdjust -> set Quantity = Quantity + BadEvent.QuantityAdjust -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?>
        {
            ["Balance"] = 100.0,
            ["Quantity"] = 5.0
        });

        var result = workflow.Fire(instance, "BadEvent", new Dictionary<string, object?>
        {
            ["BalanceAdjust"] = -200.0,
            ["QuantityAdjust"] = -50.0
        });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().HaveCount(2);
        result.Violations.Should().Contain(v => v.Message.Contains("Balance must not go negative", StringComparison.OrdinalIgnoreCase));
        result.Violations.Should().Contain(v => v.Message.Contains("Quantity must be non-negative", StringComparison.OrdinalIgnoreCase));
    }

    // ========================================================================================
    // RUNTIME — top-level rules (checked after set execution)
    // ========================================================================================

    [Fact]
    public void Fire_TopLevelRule_ViolatedAfterSets_IsBlocked()
    {
        const string dsl = """
            precept Test
            field Quantity as number default 5
            field UnitPrice as number default 10
            field TotalPrice as number default 50
            rule Quantity * UnitPrice == TotalPrice because "Price must be consistent"
            state Active initial
            event AdjustQuantity with NewQty as number
            from Active on AdjustQuantity -> set Quantity = AdjustQuantity.NewQty -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?>
        {
            ["Quantity"] = 5.0,
            ["UnitPrice"] = 10.0,
            ["TotalPrice"] = 50.0
        });

        var result = workflow.Fire(instance, "AdjustQuantity", new Dictionary<string, object?> { ["NewQty"] = 7.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Price must be consistent");
    }

    [Fact]
    public void Fire_TopLevelRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            field Quantity as number default 5
            field UnitPrice as number default 10
            field TotalPrice as number default 50
            rule Quantity * UnitPrice == TotalPrice because "Price must be consistent"
            state Active initial
            event AdjustAll with NewQty as number, NewTotal as number
            from Active on AdjustAll -> set Quantity = AdjustAll.NewQty -> set TotalPrice = AdjustAll.NewTotal -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?>
        {
            ["Quantity"] = 5.0,
            ["UnitPrice"] = 10.0,
            ["TotalPrice"] = 50.0
        });

        var result = workflow.Fire(instance, "AdjustAll", new Dictionary<string, object?>
        {
            ["NewQty"] = 3.0,
            ["NewTotal"] = 30.0
        });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Quantity"].Should().Be(3.0);
        result.UpdatedInstance.InstanceData["TotalPrice"].Should().Be(30.0);
    }

    // ========================================================================================
    // RUNTIME — state rules (checked on entry, not on no-transition)
    // ========================================================================================

    [Fact]
    public void Fire_StateRule_ViolatedOnEntry_IsBlocked()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Draft initial
            state Paid
            in Paid ensure AmountPaid > 0 because "Must have paid something"
            event Checkout with Payment as number
            from Draft on Checkout -> set AmountPaid = Checkout.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Fire(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 0.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Must have paid something");
    }

    [Fact]
    public void Fire_StateRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Draft initial
            state Paid
            in Paid ensure AmountPaid > 0 because "Must have paid something"
            event Checkout with Payment as number
            from Draft on Checkout -> set AmountPaid = Checkout.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Fire(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 100.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.NewState.Should().Be("Paid");
    }

    [Fact]
    public void Fire_StateRule_SelfTransition_IsChecked()
    {
        // Self-transition means we are 'entering' the same state — state rules apply
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            in Active ensure Score > 0 because "Score must be positive while active"
            event Penalize with Points as number
            from Active on Penalize -> set Score = Score - Penalize.Points -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Score"] = 10.0 });

        var result = workflow.Fire(instance, "Penalize", new Dictionary<string, object?> { ["Points"] = 15.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Score must be positive while active");
    }

    [Fact]
    public void Fire_StateRule_NoTransition_IsNotChecked_CorrectDesign()
    {
        // State rules on a state are NOT checked when the outcome is 'no transition'.
        // We use a transition to a different state to set up, then verify no-transition doesn't check source state rules.
        const string dsl = """
            precept Test
            field Score as number default 5
            state Lobby initial
            state Active
            to Active ensure Score > 0 because "Score must be positive"
            event Enter
            event AttemptFail with Penalty as number
            from Lobby on Enter -> set Score = 5 -> transition Active
            from Active on AttemptFail -> set Score = Score - AttemptFail.Penalty -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        // Move to Active state first
        var enterResult = workflow.Fire(
            workflow.CreateInstance("Lobby", new Dictionary<string, object?> { ["Score"] = 5.0 }),
            "Enter");
        (enterResult.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();

        var instance = enterResult.UpdatedInstance!;

        // Now fire a no-transition event that would violate the state rule if state rules were checked
        var result = workflow.Fire(instance, "AttemptFail", new Dictionary<string, object?> { ["Penalty"] = 10.0 });

        // no-transition doesn't check state rules — should be accepted (no-transition outcome)
        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
        result.UpdatedInstance!.InstanceData["Score"].Should().Be(-5.0);
    }

    // ========================================================================================
    // RUNTIME — rules pipeline ordering
    // ========================================================================================

    [Fact]
    public void Fire_RulesOrder_EventRulesCheckedBeforeGuard()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay when Balance > 9999 -> transition Done
            from Idle on Pay -> reject "Not enough balance"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?> { ["Balance"] = 0.0 });

        // Both event rule AND guard would reject, but event rule is checked first
        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        // Should only have the event rule violation reason, not the guard rejection reason
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Amount must be positive");
        result.Violations.Should().NotContain(v => v.Message.Contains("Not enough balance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Fire_RulesOrder_FieldRulesCheckedAfterSets()
    {
        // Field rule should see the post-set value, not the pre-set value
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Must be non-negative"
            state Active initial
            event ZeroOut
            from Active on ZeroOut -> set Balance = 0 -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Setting to 0 satisfies Balance >= 0
        var result = workflow.Fire(instance, "ZeroOut");

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().Be(0.0);
    }

    // ========================================================================================
    // RUNTIME — null handling in rules
    // ========================================================================================

    [Fact]
    public void Fire_FieldRule_DynamicExpressionKeepsFieldValid_IsAccepted()
    {
        // A dynamic expression that keeps a field within its rule passes at runtime.
        // Compile time skips validation because the expression is not a constant.
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Debit of 50 keeps Balance = 50, rule 'Balance >= 0' passes
        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().Be(50.0);
    }

    [Fact]
    public void Fire_FieldRule_ViolatedDynamically_IsBlocked()
    {
        // Field rule violation through a dynamic expression (not caught at compile time)
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 50.0 });

        // Debiting 200 would make Balance = -150, violating the rule
        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Balance must not go negative");
    }

    [Fact]
    public void Fire_FieldRule_NullFieldWithNullCheck_IsAllowed()
    {
        // Field with nullable type set to null via dynamic expression.
        // Rule uses explicit null check: passes when Balance is null.
        const string dsl = """
            precept Test
            field Balance as number nullable
            rule Balance == null or Balance >= 0 because "Balance must be null or non-negative"
            state Active initial
            event ClearBalance with NewBalance as number nullable
            from Active on ClearBalance -> set Balance = ClearBalance.NewBalance -> transition Active
            """;

        // compile must succeed: default value null satisfies 'null or null >= 0' = true
        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Setting Balance to null via nullable arg — 'Balance == null or Balance >= 0' passes
        var result = workflow.Fire(instance, "ClearBalance", new Dictionary<string, object?> { ["NewBalance"] = null });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().BeNull();
    }

    // ========================================================================================
    // RUNTIME — collection rules
    // ========================================================================================

    [Fact]
    public void Fire_CollectionRule_ViolatedAfterMutation_IsBlocked()
    {
        // A rule requiring count <= 2, but mutation adds a 3rd item
        const string dsl = """
            precept Test
            field Tags as set of string
            rule Tags.count <= 2 because "Too many tags"
            state Active initial
            event AddTag with Tag as string
            from Active on AddTag -> add Tags AddTag.Tag -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");

        // Add first two tags
        var r1 = workflow.Fire(instance, "AddTag", new Dictionary<string, object?> { ["Tag"] = "a" });
        (r1.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var r2 = workflow.Fire(r1.UpdatedInstance!, "AddTag", new Dictionary<string, object?> { ["Tag"] = "b" });
        (r2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();

        // Third tag should violate the rule
        var r3 = workflow.Fire(r2.UpdatedInstance!, "AddTag", new Dictionary<string, object?> { ["Tag"] = "c" });
        (r3.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        r3.Violations.Should().ContainSingle().Which.Message.Should().Contain("Too many tags");
    }

    // ========================================================================================
    // RUNTIME — from-any with state rules
    // ========================================================================================

    [Fact]
    public void Fire_FromAny_StateRules_ApplyToTargetState()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Draft initial
            state Review
            state Paid
            in Paid ensure AmountPaid > 0 because "Must have paid to be in Paid"
            event Pay with Payment as number
            from any on Pay -> set AmountPaid = Pay.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instanceDraft = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });
        var instanceReview = workflow.CreateInstance("Review", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var resultFromDraft = workflow.Fire(instanceDraft, "Pay", new Dictionary<string, object?> { ["Payment"] = 0.0 });
        var resultFromReview = workflow.Fire(instanceReview, "Pay", new Dictionary<string, object?> { ["Payment"] = 50.0 });

        (resultFromDraft.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        resultFromDraft.Violations.Should().ContainSingle().Which.Message.Should().Contain("Must have paid to be in Paid");

        (resultFromReview.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        resultFromReview.NewState.Should().Be("Paid");
    }

    // ========================================================================================
    // INSPECT — rules during inspection
    // ========================================================================================

    [Fact]
    public void Inspect_EventRule_Violated_IsBlocked()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Inspect(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Amount must be positive");
    }

    [Fact]
    public void Inspect_EventRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Inspect(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.TargetState.Should().Be("Done");
    }

    [Fact]
    public void Inspect_EventRule_EventArgNameShadowsMachineField_ArgValueIsUsed()
    {
        // Regression: event arg has the same bare name as a machine field.
        // Inspect must evaluate the event rule against the arg value, not the machine field value.
        const string dsl = """
            precept Test
            field CreditScore as number default 0
            state Apply initial
            state UnderReview
            event Submit with CreditScore as number
            on Submit ensure CreditScore >= 300 because "Credit score must be at least 300"
            from Apply on Submit -> transition UnderReview
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Apply", new Dictionary<string, object?> { ["CreditScore"] = 0.0 });

        var passing = workflow.Inspect(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 500.0 });
        (passing.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();

        var failing = workflow.Inspect(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 100.0 });
        (failing.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        failing.Violations.Should().ContainSingle().Which.Message.Should().Contain("Credit score must be at least 300");
    }

    [Fact]
    public void Inspect_FieldRule_SimulatedViolation_IsBlocked()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Inspect(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Balance must not go negative");
    }

    [Fact]
    public void Inspect_StateRule_SimulatedViolation_IsBlocked()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Draft initial
            state Paid
            in Paid ensure AmountPaid > 0 because "Must have paid"
            event Checkout with Payment as number
            from Draft on Checkout -> set AmountPaid = Checkout.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Inspect(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 0.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Must have paid");
    }

    [Fact]
    public void Inspect_WithoutEventArgs_ReturnsRequiredKeys()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number, Note as string nullable, Fee as number default 5
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        // Calling inspect without args — should still return accepted (discovery) with RequiredEventArgumentKeys
        var result = workflow.Inspect(instance, "Pay");

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        result.RequiredEventArgumentKeys.Should().ContainSingle().Which.Should().Be("Amount");
    }

    // ========================================================================================
    // INSPECT — stateless overload with event rules
    // ========================================================================================

    [Fact]
    public void Inspect_Stateless_EventRule_Violated_IsBlocked()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var result = workflow.Inspect(workflow.CreateInstance("Idle"), "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Amount must be positive");
    }

    // ========================================================================================
    // MODEL — no-rules case produces null collections (not empty)
    // ========================================================================================

    [Fact]
    public void Parse_NoRules_MachineHasNullConstraints()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Rules.Should().BeNull();
        machine.StateEnsures.Should().BeNull();
        machine.EventEnsures.Should().BeNull();
    }

    [Fact]
    public void Parse_NoRules_EventEnsuresAreNull()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Go with Value as number
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.EventEnsures.Should().BeNull();
    }
}


