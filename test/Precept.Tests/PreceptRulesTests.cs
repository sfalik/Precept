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
            invariant Balance >= 0 because "Must be non-negative"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        var inv = machine.Invariants.Should().ContainSingle().Subject;
        inv.ExpressionText.Should().Be("Balance >= 0");
        inv.Reason.Should().Be("Must be non-negative");
    }

    [Fact]
    public void Parse_FieldRule_MultipleRulesOnSameField()
    {
        const string dsl = """
            precept Test
            field Rating as number default 1
            invariant Rating >= 1 because "Too low"
            invariant Rating <= 5 because "Too high"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Invariants.Should().HaveCount(2);
        machine.Invariants![0].Reason.Should().Be("Too low");
        machine.Invariants[1].Reason.Should().Be("Too high");
    }

    [Fact]
    public void Parse_TopLevelRule_AttachedToMachine()
    {
        const string dsl = """
            precept Test
            field Quantity as number default 0
            field Total as number default 0
            invariant Quantity >= 0 because "Quantity must be non-negative"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TopLevelRules.Should().ContainSingle();
        machine.TopLevelRules![0].ExpressionText.Should().Be("Quantity >= 0");
        machine.TopLevelRules[0].Reason.Should().Be("Quantity must be non-negative");
    }

    [Fact]
    public void Parse_TopLevelRule_MultipleRules()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            field Limit as number default 1000
            invariant Balance >= 0 because "Balance must not go negative"
            invariant Balance <= Limit because "Balance must not exceed limit"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TopLevelRules.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_StateRule_AttachedToState()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Idle initial
            state Paid
            in Paid assert AmountPaid > 0 because "Must have paid"
            """;

        var machine = PreceptParser.Parse(dsl);

        var assert = machine.StateAsserts.Should().ContainSingle().Subject;
        assert.State.Should().Be("Paid");
        assert.ExpressionText.Should().Be("AmountPaid > 0");
        assert.Reason.Should().Be("Must have paid");
    }

    [Fact]
    public void Parse_StateRule_MultipleStatesWithRules()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state A initial
            in A assert Score >= 0 because "Non-negative in A"
            state B
            in B assert Score >= 10 because "Must be 10 in B"
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.StateAsserts.Should().HaveCount(2);
        machine.StateAsserts!.Single(sa => sa.State == "A").Reason.Should().Be("Non-negative in A");
        machine.StateAsserts!.Single(sa => sa.State == "B").Reason.Should().Be("Must be 10 in B");
    }

    [Fact]
    public void Parse_EventRule_AttachedToEvent()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Amount must be positive"
            """;

        var machine = PreceptParser.Parse(dsl);

        var assert = machine.EventAsserts.Should().ContainSingle().Subject;
        assert.EventName.Should().Be("Pay");
        assert.ExpressionText.Should().Be("Amount > 0");
        assert.Reason.Should().Be("Amount must be positive");
    }

    [Fact]
    public void Parse_EventRule_ReferencingPrefixedArgName()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Pay with Amount as number
            on Pay assert Pay.Amount > 0 because "Amount must be positive"
            """;

        var machine = PreceptParser.Parse(dsl);

        var assert = machine.EventAsserts.Should().ContainSingle().Subject;
        assert.EventName.Should().Be("Pay");
        assert.ExpressionText.Should().Be("Pay.Amount > 0");
    }

    [Fact]
    public void Parse_CollectionFieldRule_AttachedToCollection()
    {
        const string dsl = """
            precept Test
            field Approvers as set of string
            invariant Approvers.count >= 1 because "Need at least one approver"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        var inv = machine.Invariants.Should().ContainSingle().Subject;
        inv.ExpressionText.Should().Be("Approvers.count >= 1");
        inv.Reason.Should().Be("Need at least one approver");
    }

    [Fact]
    public void Parse_FieldRule_SourceLineIsCorrect()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            invariant Balance >= 0 because "Must be non-negative"
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        var inv = machine.Invariants.Should().ContainSingle().Subject;
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
            invariant Balance <= Limit because "Cannot exceed limit"
            state Idle initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        // Invariants can reference any declared field — not scoped to a single field
        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_FieldRule_ReferencingOwnField_IsAllowed()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            invariant Balance >= 0 because "Must be non-negative"
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
            invariant Tags.count <= 10 because "Too many tags"
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
            on Pay assert Amount <= Balance because "Cannot exceed balance"
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
            on Pay assert Amount > Discount because "Amount must exceed discount"
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
            invariant Balance >= 10 because "Balance must be at least 10"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invariant violation*Balance must be at least 10*");
    }

    [Fact]
    public void Compile_FieldRule_DefaultValueSatisfiesRule_Succeeds()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Must be non-negative"
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
            invariant Quantity * UnitPrice == TotalPrice because "Price must be consistent"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invariant violation*Price must be consistent*");
    }

    [Fact]
    public void Compile_TopLevelRule_DefaultValuesSatisfyRule_Succeeds()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            field Limit as number default 1000
            invariant Balance <= Limit because "Cannot exceed limit"
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
            in Paid assert AmountPaid > 0 because "Must have paid"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*state assert violation*Must have paid*initial state*");
    }

    [Fact]
    public void Compile_NonInitialStateRule_NotCheckedAtCompileTime_Succeeds()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Idle initial
            state Paid
            in Paid assert AmountPaid > 0 because "Must have paid"
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
            invariant Approvers.count >= 1 because "Need at least one approver"
            state Idle initial
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invariant violation*Need at least one approver*");
    }

    [Fact]
    public void Compile_CollectionRule_SatisfiedAtCreation_Succeeds()
    {
        const string dsl = """
            precept Test
            field Tags as set of string
            invariant Tags.count <= 10 because "Too many tags"
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
            on Submit assert Priority > 0 because "Priority must be positive"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*event assert violation*Priority must be positive*");
    }

    [Fact]
    public void Compile_EventRule_AllArgsHaveDefaultsAndPass_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Submit with Priority as number default 1
            on Submit assert Priority > 0 because "Priority must be positive"
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
            on Submit assert Priority > 0 because "Priority must be positive"
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
            invariant Balance >= 0 because "Must be non-negative"
            state Active initial
            event Reset
            from Active on Reset -> set Balance = -1 -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*literal assignment*violates invariant*Must be non-negative*");
    }

    [Fact]
    public void Compile_LiteralSetAssignment_SatisfiesFieldRule_Succeeds()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Must be non-negative"
            state Active initial
            event Adjust
            from Active on Adjust -> set Balance = 50 -> transition Active
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
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Outcome.Should().Be(PreceptOutcomeKind.Rejected);
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_EventRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay when Balance > 1000 -> transition Done
            from Idle on Pay -> reject "Not enough balance"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?> { ["Balance"] = 0.0 });

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_EventRule_MultipleViolations_AllReported()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Transfer with Amount as number, Fee as number
            on Transfer assert Amount > 0 because "Amount must be positive"
            on Transfer assert Fee >= 0 because "Fee must be non-negative"
            from Idle on Transfer -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Transfer", new Dictionary<string, object?>
        {
            ["Amount"] = -100.0,
            ["Fee"] = -5.0
        });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().HaveCount(2);
        result.Reasons.Should().Contain(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
        result.Reasons.Should().Contain(r => r.Contains("Fee must be non-negative", StringComparison.Ordinal));
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
            on Submit assert CreditScore >= 300 because "Credit score must be at least 300"
            from Apply on Submit -> transition UnderReview
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Apply", new Dictionary<string, object?> { ["CreditScore"] = 0.0 });

        // Arg value 500 satisfies the rule even though the machine field is 0
        var passing = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 500.0 });
        (passing.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();

        // Arg value 100 violates the rule
        var failing = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 100.0 });
        (failing.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        failing.Reasons.Should().ContainSingle(r => r.Contains("Credit score must be at least 300", StringComparison.Ordinal));
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
            invariant Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Outcome.Should().Be(PreceptOutcomeKind.Rejected);
        result.Reasons.Should().ContainSingle(r => r.Contains("Balance must not go negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_FieldRule_Satisfied_CommitsAndAccepts()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 30.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().Be(70.0);
    }

    [Fact]
    public void Fire_FieldRule_Violated_SetMutationsRolledBack()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must not go negative"
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

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        // UpdatedInstance is null on rejection meaning original data is unchanged
        result.UpdatedInstance.Should().BeNull();
    }

    [Fact]
    public void Fire_FieldRule_MultipleViolations_AllReported()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must not go negative"
            field Quantity as number default 5
            invariant Quantity >= 0 because "Quantity must be non-negative"
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

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().HaveCount(2);
        result.Reasons.Should().Contain(r => r.Contains("Balance must not go negative", StringComparison.Ordinal));
        result.Reasons.Should().Contain(r => r.Contains("Quantity must be non-negative", StringComparison.Ordinal));
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
            invariant Quantity * UnitPrice == TotalPrice because "Price must be consistent"
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

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Price must be consistent", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_TopLevelRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            field Quantity as number default 5
            field UnitPrice as number default 10
            field TotalPrice as number default 50
            invariant Quantity * UnitPrice == TotalPrice because "Price must be consistent"
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

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            in Paid assert AmountPaid > 0 because "Must have paid something"
            event Checkout with Payment as number
            from Draft on Checkout -> set AmountPaid = Checkout.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Fire(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 0.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Must have paid something", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_StateRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Draft initial
            state Paid
            in Paid assert AmountPaid > 0 because "Must have paid something"
            event Checkout with Payment as number
            from Draft on Checkout -> set AmountPaid = Checkout.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Fire(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 100.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            in Active assert Score > 0 because "Score must be positive while active"
            event Penalize with Points as number
            from Active on Penalize -> set Score = Score - Penalize.Points -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Score"] = 10.0 });

        var result = workflow.Fire(instance, "Penalize", new Dictionary<string, object?> { ["Points"] = 15.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Score must be positive while active", StringComparison.Ordinal));
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
            to Active assert Score > 0 because "Score must be positive"
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
        (enterResult.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();

        var instance = enterResult.UpdatedInstance!;

        // Now fire a no-transition event that would violate the state rule if state rules were checked
        var result = workflow.Fire(instance, "AttemptFail", new Dictionary<string, object?> { ["Penalty"] = 10.0 });

        // no-transition doesn't check state rules — should be accepted (no-transition outcome)
        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.Outcome.Should().Be(PreceptOutcomeKind.AcceptedInPlace);
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
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay when Balance > 9999 -> transition Done
            from Idle on Pay -> reject "Not enough balance"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?> { ["Balance"] = 0.0 });

        // Both event rule AND guard would reject, but event rule is checked first
        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        // Should only have the event rule violation reason, not the guard rejection reason
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
        result.Reasons.Should().NotContain(r => r.Contains("Not enough balance", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_RulesOrder_FieldRulesCheckedAfterSets()
    {
        // Field rule should see the post-set value, not the pre-set value
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Must be non-negative"
            state Active initial
            event ZeroOut
            from Active on ZeroOut -> set Balance = 0 -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Setting to 0 satisfies Balance >= 0
        var result = workflow.Fire(instance, "ZeroOut");

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            invariant Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Debit of 50 keeps Balance = 50, rule 'Balance >= 0' passes
        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().Be(50.0);
    }

    [Fact]
    public void Fire_FieldRule_ViolatedDynamically_IsBlocked()
    {
        // Field rule violation through a dynamic expression (not caught at compile time)
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 50.0 });

        // Debiting 200 would make Balance = -150, violating the rule
        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Balance must not go negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_FieldRule_NullFieldWithNullCheck_IsAllowed()
    {
        // Field with nullable type set to null via dynamic expression.
        // Rule uses explicit null check: passes when Balance is null.
        const string dsl = """
            precept Test
            field Balance as number nullable
            invariant Balance == null || Balance >= 0 because "Balance must be null or non-negative"
            state Active initial
            event ClearBalance with NewBalance as number nullable
            from Active on ClearBalance -> set Balance = ClearBalance.NewBalance -> transition Active
            """;

        // compile must succeed: default value null satisfies 'null || null >= 0' = true
        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Setting Balance to null via nullable arg — 'Balance == null || Balance >= 0' passes
        var result = workflow.Fire(instance, "ClearBalance", new Dictionary<string, object?> { ["NewBalance"] = null });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            invariant Tags.count <= 2 because "Too many tags"
            state Active initial
            event AddTag with Tag as string
            from Active on AddTag -> add Tags AddTag.Tag -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");

        // Add first two tags
        var r1 = workflow.Fire(instance, "AddTag", new Dictionary<string, object?> { ["Tag"] = "a" });
        (r1.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
        var r2 = workflow.Fire(r1.UpdatedInstance!, "AddTag", new Dictionary<string, object?> { ["Tag"] = "b" });
        (r2.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();

        // Third tag should violate the rule
        var r3 = workflow.Fire(r2.UpdatedInstance!, "AddTag", new Dictionary<string, object?> { ["Tag"] = "c" });
        (r3.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        r3.Reasons.Should().ContainSingle(r => r.Contains("Too many tags", StringComparison.Ordinal));
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
            in Paid assert AmountPaid > 0 because "Must have paid to be in Paid"
            event Pay with Payment as number
            from any on Pay -> set AmountPaid = Pay.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instanceDraft = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });
        var instanceReview = workflow.CreateInstance("Review", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var resultFromDraft = workflow.Fire(instanceDraft, "Pay", new Dictionary<string, object?> { ["Payment"] = 0.0 });
        var resultFromReview = workflow.Fire(instanceReview, "Pay", new Dictionary<string, object?> { ["Payment"] = 50.0 });

        (resultFromDraft.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        resultFromDraft.Reasons.Should().ContainSingle(r => r.Contains("Must have paid to be in Paid", StringComparison.Ordinal));

        (resultFromReview.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Inspect(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Outcome.Should().Be(PreceptOutcomeKind.Rejected);
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_EventRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Inspect(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            on Submit assert CreditScore >= 300 because "Credit score must be at least 300"
            from Apply on Submit -> transition UnderReview
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Apply", new Dictionary<string, object?> { ["CreditScore"] = 0.0 });

        var passing = workflow.Inspect(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 500.0 });
        (passing.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();

        var failing = workflow.Inspect(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 100.0 });
        (failing.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        failing.Reasons.Should().ContainSingle(r => r.Contains("Credit score must be at least 300", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_FieldRule_SimulatedViolation_IsBlocked()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Inspect(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Balance must not go negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_StateRule_SimulatedViolation_IsBlocked()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Draft initial
            state Paid
            in Paid assert AmountPaid > 0 because "Must have paid"
            event Checkout with Payment as number
            from Draft on Checkout -> set AmountPaid = Checkout.Payment -> transition Paid
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Inspect(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 0.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Must have paid", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_WithoutEventArgs_ReturnsRequiredKeys()
    {
        const string dsl = """
            precept Test
            state Idle initial
            state Done
            event Pay with Amount as number, Note as string nullable, Fee as number default 5
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        // Calling inspect without args — should still return accepted (discovery) with RequiredEventArgumentKeys
        var result = workflow.Inspect(instance, "Pay");

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            on Pay assert Amount > 0 because "Amount must be positive"
            from Idle on Pay -> transition Done
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var result = workflow.Inspect(workflow.CreateInstance("Idle"), "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        (result.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    // ========================================================================================
    // MODEL — no-rules case produces null collections (not empty)
    // ========================================================================================

    [Fact]
    public void Parse_NoRules_MachineHasNullTopLevelRules()
    {
        const string dsl = """
            precept Test
            field Balance as number default 0
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TopLevelRules.Should().BeNull();
        machine.States.All(s => s.Rules == null).Should().BeTrue();
        machine.Fields[0].Rules.Should().BeNull();
    }

    [Fact]
    public void Parse_NoRules_EventHasNullRules()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Go with Value as number
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.Events[0].Rules.Should().BeNull();
    }
}


