using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using StateMachine.Dsl;
using Xunit;

namespace StateMachine.Tests;

/// <summary>
/// Tests for the declarative rules feature across all four rule positions:
/// field rules, top-level rules, state rules, and event rules.
/// </summary>
public class DslRulesTests
{
    // ========================================================================================
    // PARSING — model structure
    // ========================================================================================

    [Fact]
    public void Parse_FieldRule_AttachedToField()
    {
        const string dsl = """
            machine Test
            number Balance = 0
              rule Balance >= 0 "Must be non-negative"
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        var balance = machine.Fields.Single(f => f.Name == "Balance");
        balance.Rules.Should().ContainSingle();
        balance.Rules![0].ExpressionText.Should().Be("Balance >= 0");
        balance.Rules[0].Reason.Should().Be("Must be non-negative");
    }

    [Fact]
    public void Parse_FieldRule_MultipleRulesOnSameField()
    {
        const string dsl = """
            machine Test
            number Rating = 1
              rule Rating >= 1 "Too low"
              rule Rating <= 5 "Too high"
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        var rating = machine.Fields.Single(f => f.Name == "Rating");
        rating.Rules.Should().HaveCount(2);
        rating.Rules![0].Reason.Should().Be("Too low");
        rating.Rules[1].Reason.Should().Be("Too high");
    }

    [Fact]
    public void Parse_TopLevelRule_AttachedToMachine()
    {
        const string dsl = """
            machine Test
            number Quantity = 0
            number Total = 0
            rule Quantity >= 0 "Quantity must be non-negative"
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.TopLevelRules.Should().ContainSingle();
        machine.TopLevelRules![0].ExpressionText.Should().Be("Quantity >= 0");
        machine.TopLevelRules[0].Reason.Should().Be("Quantity must be non-negative");
    }

    [Fact]
    public void Parse_TopLevelRule_MultipleRules()
    {
        const string dsl = """
            machine Test
            number Balance = 100
            number Limit = 1000
            rule Balance >= 0 "Balance must not go negative"
            rule Balance <= Limit "Balance must not exceed limit"
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.TopLevelRules.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_StateRule_AttachedToState()
    {
        const string dsl = """
            machine Test
            number AmountPaid = 0
            state Idle initial
            state Paid
              rule AmountPaid > 0 "Must have paid"
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.States.Should().Contain(s => s.Name == "Paid" && s.Rules != null);
        machine.States.Single(s => s.Name == "Paid").Rules.Should().ContainSingle();
        machine.States.Single(s => s.Name == "Paid").Rules![0].ExpressionText.Should().Be("AmountPaid > 0");
        machine.States.Single(s => s.Name == "Paid").Rules![0].Reason.Should().Be("Must have paid");
    }

    [Fact]
    public void Parse_StateRule_MultipleStatesWithRules()
    {
        const string dsl = """
            machine Test
            number Score = 0
            state A initial
              rule Score >= 0 "Non-negative in A"
            state B
              rule Score >= 10 "Must be 10 in B"
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.States.Count(s => s.Rules != null && s.Rules.Count > 0).Should().Be(2);
        machine.States.Single(s => s.Name == "A").Rules![0].Reason.Should().Be("Non-negative in A");
        machine.States.Single(s => s.Name == "B").Rules![0].Reason.Should().Be("Must be 10 in B");
    }

    [Fact]
    public void Parse_EventRule_AttachedToEvent()
    {
        const string dsl = """
            machine Test
            state Idle initial
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        var payEvent = machine.Events.Single(e => e.Name == "Pay");
        payEvent.Rules.Should().ContainSingle();
        payEvent.Rules![0].ExpressionText.Should().Be("Amount > 0");
        payEvent.Rules[0].Reason.Should().Be("Amount must be positive");
    }

    [Fact]
    public void Parse_EventRule_ReferencingPrefixedArgName()
    {
        const string dsl = """
            machine Test
            state Idle initial
            event Pay
              number Amount
              rule Pay.Amount > 0 "Amount must be positive"
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        var payEvent = machine.Events.Single(e => e.Name == "Pay");
        payEvent.Rules.Should().ContainSingle();
        payEvent.Rules![0].ExpressionText.Should().Be("Pay.Amount > 0");
    }

    [Fact]
    public void Parse_CollectionFieldRule_AttachedToCollection()
    {
        const string dsl = """
            machine Test
            set<string> Approvers
              rule Approvers.count >= 1 "Need at least one approver"
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        var approvers = machine.CollectionFields.Single(f => f.Name == "Approvers");
        approvers.Rules.Should().ContainSingle();
        approvers.Rules![0].ExpressionText.Should().Be("Approvers.count >= 1");
        approvers.Rules[0].Reason.Should().Be("Need at least one approver");
    }

    [Fact]
    public void Parse_FieldRule_SourceLineIsCorrect()
    {
        const string dsl = """
            machine Test
            number Balance = 0
              rule Balance >= 0 "Must be non-negative"
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        var rule = machine.Fields.Single(f => f.Name == "Balance").Rules![0];
        rule.SourceLine.Should().Be(3);
    }

    // ========================================================================================
    // PARSING — scope restrictions
    // ========================================================================================

    [Fact]
    public void Parse_FieldRule_ReferencingAnotherField_Throws()
    {
        const string dsl = """
            machine Test
            number Balance = 100
            number Limit = 1000
              rule Balance <= Limit "Cannot exceed limit"
            state Idle initial
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*field rule*own field*");
    }

    [Fact]
    public void Parse_FieldRule_ReferencingOwnField_IsAllowed()
    {
        const string dsl = """
            machine Test
            number Balance = 0
              rule Balance >= 0 "Must be non-negative"
            state Idle initial
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_FieldRule_DottedPropertyOfOwnField_IsAllowed()
    {
        const string dsl = """
            machine Test
            set<string> Tags
              rule Tags.count <= 10 "Too many tags"
            state Idle initial
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_EventRule_ReferencingInstanceDataField_Throws()
    {
        const string dsl = """
            machine Test
            number Balance = 100
            state Idle initial
            event Pay
              number Amount
              rule Amount <= Balance "Cannot exceed balance"
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*event rule*event argument identifiers*");
    }

    [Fact]
    public void Parse_EventRule_ReferencingOnlyEventArgs_IsAllowed()
    {
        const string dsl = """
            machine Test
            state Idle initial
            event Pay
              number Amount
              number Discount
              rule Amount > Discount "Amount must exceed discount"
            """;

        var act = () => StateMachineDslParser.Parse(dsl);

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
            machine Test
            number Balance = 0
              rule Balance >= 10 "Balance must be at least 10"
            state Idle initial
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*compile-time rule violation*Balance must be at least 10*");
    }

    [Fact]
    public void Compile_FieldRule_DefaultValueSatisfiesRule_Succeeds()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Must be non-negative"
            state Idle initial
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_TopLevelRule_DefaultValueViolatesRule_Throws()
    {
        const string dsl = """
            machine Test
            number Quantity = 0
            number UnitPrice = 10
            number TotalPrice = 999
            rule Quantity * UnitPrice == TotalPrice "Price must be consistent"
            state Idle initial
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*compile-time rule violation*Price must be consistent*");
    }

    [Fact]
    public void Compile_TopLevelRule_DefaultValuesSatisfyRule_Succeeds()
    {
        const string dsl = """
            machine Test
            number Balance = 100
            number Limit = 1000
            rule Balance <= Limit "Cannot exceed limit"
            state Idle initial
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_InitialStateRule_ViolatedByDefaultData_Throws()
    {
        const string dsl = """
            machine Test
            number AmountPaid = 0
            state Paid initial
              rule AmountPaid > 0 "Must have paid"
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*compile-time rule violation*Must have paid*initial state*");
    }

    [Fact]
    public void Compile_NonInitialStateRule_NotCheckedAtCompileTime_Succeeds()
    {
        const string dsl = """
            machine Test
            number AmountPaid = 0
            state Idle initial
            state Paid
              rule AmountPaid > 0 "Must have paid"
            event Pay
            from Idle on Pay
                transition Paid
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_CollectionRule_ViolatedAtCreation_Throws()
    {
        const string dsl = """
            machine Test
            set<string> Approvers
              rule Approvers.count >= 1 "Need at least one approver"
            state Idle initial
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*compile-time rule violation*Need at least one approver*");
    }

    [Fact]
    public void Compile_CollectionRule_SatisfiedAtCreation_Succeeds()
    {
        const string dsl = """
            machine Test
            set<string> Tags
              rule Tags.count <= 10 "Too many tags"
            state Idle initial
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_EventRule_DefaultArgValueViolatesRule_Throws()
    {
        const string dsl = """
            machine Test
            state Idle initial
            event Submit
              number Priority = 0
              rule Priority > 0 "Priority must be positive"
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*compile-time rule violation*Priority must be positive*");
    }

    [Fact]
    public void Compile_EventRule_AllArgsHaveDefaultsAndPass_Succeeds()
    {
        const string dsl = """
            machine Test
            state Idle initial
            event Submit
              number Priority = 1
              rule Priority > 0 "Priority must be positive"
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_EventRule_RequiredArgNotDefaulted_SkipsCompileTimeCheck_Succeeds()
    {
        const string dsl = """
            machine Test
            state Idle initial
            event Submit
              number Priority
              rule Priority > 0 "Priority must be positive"
            """;

        // Cannot check at compile time because Priority has no default
        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().NotThrow();
    }

    [Fact]
    public void Compile_LiteralSetAssignment_ViolatesFieldRule_Throws()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Must be non-negative"
            state Active initial
            event Reset
            from Active on Reset
              set Balance = -1
              transition Active
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*literal assignment*violates rule*Must be non-negative*");
    }

    [Fact]
    public void Compile_LiteralSetAssignment_SatisfiesFieldRule_Succeeds()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Must be non-negative"
            state Active initial
            event Adjust
            from Active on Adjust
              set Balance = 50
              transition Active
            """;

        var act = () => DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        act.Should().NotThrow();
    }

    // ========================================================================================
    // RUNTIME — event rules (checked before guard, fire path)
    // ========================================================================================

    [Fact]
    public void Fire_EventRule_Violated_IsBlocked()
    {
        const string dsl = """
            machine Test
            state Idle initial
            state Done
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              transition Done
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Outcome.Should().Be(DslOutcomeKind.Rejected);
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_EventRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            machine Test
            state Idle initial
            state Done
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              transition Done
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.NewState.Should().Be("Done");
    }

    [Fact]
    public void Fire_EventRule_ViolatedBeforeGuardIsEvaluated_IsBlocked()
    {
        // Even when the guard would reject, event rules are reported first
        const string dsl = """
            machine Test
            number Balance = 0
            state Idle initial
            state Done
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              if Balance > 1000
                transition Done
              else
                reject "Not enough balance"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?> { ["Balance"] = 0.0 });

        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_EventRule_MultipleViolations_AllReported()
    {
        const string dsl = """
            machine Test
            state Idle initial
            state Done
            event Transfer
              number Amount
              number Fee
              rule Amount > 0 "Amount must be positive"
              rule Fee >= 0 "Fee must be non-negative"
            from Idle on Transfer
              transition Done
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Fire(instance, "Transfer", new Dictionary<string, object?>
        {
            ["Amount"] = -100.0,
            ["Fee"] = -5.0
        });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
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
            machine Test
            number CreditScore = 0
            state Apply initial
            state UnderReview
            event Submit
              number CreditScore
                rule CreditScore >= 300 "Credit score must be at least 300"
            from Apply on Submit
              transition UnderReview
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Apply", new Dictionary<string, object?> { ["CreditScore"] = 0.0 });

        // Arg value 500 satisfies the rule even though the machine field is 0
        var passing = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 500.0 });
        (passing.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();

        // Arg value 100 violates the rule
        var failing = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 100.0 });
        (failing.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        failing.Reasons.Should().ContainSingle(r => r.Contains("Credit score must be at least 300", StringComparison.Ordinal));
    }

    // ========================================================================================
    // RUNTIME — field rules (checked after set execution)
    // ========================================================================================

    [Fact]
    public void Fire_FieldRule_ViolatedAfterSetExecution_IsBlocked()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state Active initial
            event Debit
              number Amount
            from Active on Debit
              set Balance = Balance - Debit.Amount
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Outcome.Should().Be(DslOutcomeKind.Rejected);
        result.Reasons.Should().ContainSingle(r => r.Contains("Balance must not go negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_FieldRule_Satisfied_CommitsAndAccepts()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state Active initial
            event Debit
              number Amount
            from Active on Debit
              set Balance = Balance - Debit.Amount
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 30.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().Be(70.0);
    }

    [Fact]
    public void Fire_FieldRule_Violated_SetMutationsRolledBack()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            number TransactionCount = 0
            state Active initial
            event Debit
              number Amount
            from Active on Debit
              set Balance = Balance - Debit.Amount
              set TransactionCount = TransactionCount + 1
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?>
        {
            ["Balance"] = 100.0,
            ["TransactionCount"] = 0.0
        });

        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        // UpdatedInstance is null on rejection meaning original data is unchanged
        result.UpdatedInstance.Should().BeNull();
    }

    [Fact]
    public void Fire_FieldRule_MultipleViolations_AllReported()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            number Quantity = 5
              rule Quantity >= 0 "Quantity must be non-negative"
            state Active initial
            event BadEvent
              number BalanceAdjust
              number QuantityAdjust
            from Active on BadEvent
              set Balance = Balance + BadEvent.BalanceAdjust
              set Quantity = Quantity + BadEvent.QuantityAdjust
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
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

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
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
            machine Test
            number Quantity = 5
            number UnitPrice = 10
            number TotalPrice = 50
            rule Quantity * UnitPrice == TotalPrice "Price must be consistent"
            state Active initial
            event AdjustQuantity
              number NewQty
            from Active on AdjustQuantity
              set Quantity = AdjustQuantity.NewQty
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?>
        {
            ["Quantity"] = 5.0,
            ["UnitPrice"] = 10.0,
            ["TotalPrice"] = 50.0
        });

        var result = workflow.Fire(instance, "AdjustQuantity", new Dictionary<string, object?> { ["NewQty"] = 7.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Price must be consistent", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_TopLevelRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            machine Test
            number Quantity = 5
            number UnitPrice = 10
            number TotalPrice = 50
            rule Quantity * UnitPrice == TotalPrice "Price must be consistent"
            state Active initial
            event AdjustAll
              number NewQty
              number NewTotal
            from Active on AdjustAll
              set Quantity = AdjustAll.NewQty
              set TotalPrice = AdjustAll.NewTotal
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
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

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            machine Test
            number AmountPaid = 0
            state Draft initial
            state Paid
              rule AmountPaid > 0 "Must have paid something"
            event Checkout
              number Payment
            from Draft on Checkout
              set AmountPaid = Checkout.Payment
              transition Paid
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Fire(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 0.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Must have paid something", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_StateRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            machine Test
            number AmountPaid = 0
            state Draft initial
            state Paid
              rule AmountPaid > 0 "Must have paid something"
            event Checkout
              number Payment
            from Draft on Checkout
              set AmountPaid = Checkout.Payment
              transition Paid
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Fire(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 100.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.NewState.Should().Be("Paid");
    }

    [Fact]
    public void Fire_StateRule_SelfTransition_IsChecked()
    {
        // Self-transition means we are 'entering' the same state — state rules apply
        const string dsl = """
            machine Test
            number Score = 10
            state Active initial
              rule Score > 0 "Score must be positive while active"
            event Penalize
              number Points
            from Active on Penalize
              set Score = Score - Penalize.Points
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Score"] = 10.0 });

        var result = workflow.Fire(instance, "Penalize", new Dictionary<string, object?> { ["Points"] = 15.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Score must be positive while active", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_StateRule_NoTransition_IsNotChecked_CorrectDesign()
    {
        // State rules on a state are NOT checked when the outcome is 'no transition'.
        // We use a transition to a different state to set up, then verify no-transition doesn't check source state rules.
        const string dsl = """
            machine Test
            number Score = 5
            state Lobby initial
            state Active
              rule Score > 0 "Score must be positive"
            event Enter
            event AttemptFail
              number Penalty
            from Lobby on Enter
              set Score = 5
              transition Active
            from Active on AttemptFail
              set Score = Score - AttemptFail.Penalty
              no transition
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        // Move to Active state first
        var enterResult = workflow.Fire(
            workflow.CreateInstance("Lobby", new Dictionary<string, object?> { ["Score"] = 5.0 }),
            "Enter");
        (enterResult.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();

        var instance = enterResult.UpdatedInstance!;

        // Now fire a no-transition event that would violate the state rule if state rules were checked
        var result = workflow.Fire(instance, "AttemptFail", new Dictionary<string, object?> { ["Penalty"] = 10.0 });

        // no-transition doesn't check state rules — should be accepted (no-transition outcome)
        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.Outcome.Should().Be(DslOutcomeKind.AcceptedInPlace);
        result.UpdatedInstance!.InstanceData["Score"].Should().Be(-5.0);
    }

    // ========================================================================================
    // RUNTIME — rules pipeline ordering
    // ========================================================================================

    [Fact]
    public void Fire_RulesOrder_EventRulesCheckedBeforeGuard()
    {
        const string dsl = """
            machine Test
            number Balance = 0
            state Idle initial
            state Done
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              if Balance > 9999
                transition Done
              else
                reject "Not enough balance"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?> { ["Balance"] = 0.0 });

        // Both event rule AND guard would reject, but event rule is checked first
        var result = workflow.Fire(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        // Should only have the event rule violation reason, not the guard rejection reason
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
        result.Reasons.Should().NotContain(r => r.Contains("Not enough balance", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_RulesOrder_FieldRulesCheckedAfterSets()
    {
        // Field rule should see the post-set value, not the pre-set value
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Must be non-negative"
            state Active initial
            event ZeroOut
            from Active on ZeroOut
              set Balance = 0
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Setting to 0 satisfies Balance >= 0
        var result = workflow.Fire(instance, "ZeroOut");

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            machine Test
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state Active initial
            event Debit
              number Amount
            from Active on Debit
              set Balance = Balance - Debit.Amount
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Debit of 50 keeps Balance = 50, rule 'Balance >= 0' passes
        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Balance"].Should().Be(50.0);
    }

    [Fact]
    public void Fire_FieldRule_ViolatedDynamically_IsBlocked()
    {
        // Field rule violation through a dynamic expression (not caught at compile time)
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state Active initial
            event Debit
              number Amount
            from Active on Debit
              set Balance = Balance - Debit.Amount
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 50.0 });

        // Debiting 200 would make Balance = -150, violating the rule
        var result = workflow.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Balance must not go negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_FieldRule_NullFieldWithNullCheck_IsAllowed()
    {
        // Field with nullable type set to null via dynamic expression.
        // Rule uses explicit null check: passes when Balance is null.
        const string dsl = """
            machine Test
            number? Balance = 100
              rule Balance == null || Balance >= 0 "Balance must be null or non-negative"
            state Active initial
            event ClearBalance
              number? NewBalance
            from Active on ClearBalance
              set Balance = ClearBalance.NewBalance
              transition Active
            """;

        // compile must succeed: default value 100 satisfies 'null || 100 >= 0' = true
        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        // Setting Balance to null via nullable arg — 'Balance == null || Balance >= 0' passes
        var result = workflow.Fire(instance, "ClearBalance", new Dictionary<string, object?> { ["NewBalance"] = null });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
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
            machine Test
            set<string> Tags
              rule Tags.count <= 2 "Too many tags"
            state Active initial
            event AddTag
              string Tag
            from Active on AddTag
              add Tags AddTag.Tag
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");

        // Add first two tags
        var r1 = workflow.Fire(instance, "AddTag", new Dictionary<string, object?> { ["Tag"] = "a" });
        (r1.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        var r2 = workflow.Fire(r1.UpdatedInstance!, "AddTag", new Dictionary<string, object?> { ["Tag"] = "b" });
        (r2.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();

        // Third tag should violate the rule
        var r3 = workflow.Fire(r2.UpdatedInstance!, "AddTag", new Dictionary<string, object?> { ["Tag"] = "c" });
        (r3.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        r3.Reasons.Should().ContainSingle(r => r.Contains("Too many tags", StringComparison.Ordinal));
    }

    // ========================================================================================
    // RUNTIME — from-any with state rules
    // ========================================================================================

    [Fact]
    public void Fire_FromAny_StateRules_ApplyToTargetState()
    {
        const string dsl = """
            machine Test
            number AmountPaid = 0
            state Draft initial
            state Review
            state Paid
              rule AmountPaid > 0 "Must have paid to be in Paid"
            event Pay
              number Payment
            from any on Pay
              set AmountPaid = Pay.Payment
              transition Paid
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instanceDraft = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });
        var instanceReview = workflow.CreateInstance("Review", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var resultFromDraft = workflow.Fire(instanceDraft, "Pay", new Dictionary<string, object?> { ["Payment"] = 0.0 });
        var resultFromReview = workflow.Fire(instanceReview, "Pay", new Dictionary<string, object?> { ["Payment"] = 50.0 });

        (resultFromDraft.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        resultFromDraft.Reasons.Should().ContainSingle(r => r.Contains("Must have paid to be in Paid", StringComparison.Ordinal));

        (resultFromReview.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        resultFromReview.NewState.Should().Be("Paid");
    }

    // ========================================================================================
    // INSPECT — rules during inspection
    // ========================================================================================

    [Fact]
    public void Inspect_EventRule_Violated_IsBlocked()
    {
        const string dsl = """
            machine Test
            state Idle initial
            state Done
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              transition Done
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Inspect(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = -10.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Outcome.Should().Be(DslOutcomeKind.Rejected);
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_EventRule_Satisfied_IsAccepted()
    {
        const string dsl = """
            machine Test
            state Idle initial
            state Done
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              transition Done
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        var result = workflow.Inspect(instance, "Pay", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.TargetState.Should().Be("Done");
    }

    [Fact]
    public void Inspect_EventRule_EventArgNameShadowsMachineField_ArgValueIsUsed()
    {
        // Regression: event arg has the same bare name as a machine field.
        // Inspect must evaluate the event rule against the arg value, not the machine field value.
        const string dsl = """
            machine Test
            number CreditScore = 0
            state Apply initial
            state UnderReview
            event Submit
              number CreditScore
                rule CreditScore >= 300 "Credit score must be at least 300"
            from Apply on Submit
              transition UnderReview
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Apply", new Dictionary<string, object?> { ["CreditScore"] = 0.0 });

        var passing = workflow.Inspect(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 500.0 });
        (passing.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();

        var failing = workflow.Inspect(instance, "Submit", new Dictionary<string, object?> { ["CreditScore"] = 100.0 });
        (failing.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        failing.Reasons.Should().ContainSingle(r => r.Contains("Credit score must be at least 300", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_FieldRule_SimulatedViolation_IsBlocked()
    {
        const string dsl = """
            machine Test
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state Active initial
            event Debit
              number Amount
            from Active on Debit
              set Balance = Balance - Debit.Amount
              transition Active
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = workflow.Inspect(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Balance must not go negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_StateRule_SimulatedViolation_IsBlocked()
    {
        const string dsl = """
            machine Test
            number AmountPaid = 0
            state Draft initial
            state Paid
              rule AmountPaid > 0 "Must have paid"
            event Checkout
              number Payment
            from Draft on Checkout
              set AmountPaid = Checkout.Payment
              transition Paid
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft", new Dictionary<string, object?> { ["AmountPaid"] = 0.0 });

        var result = workflow.Inspect(instance, "Checkout", new Dictionary<string, object?> { ["Payment"] = 0.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Must have paid", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_WithoutEventArgs_ReturnsRequiredKeys()
    {
        const string dsl = """
            machine Test
            state Idle initial
            state Done
            event Pay
              number Amount
              string? Note
              number Fee = 5
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              transition Done
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("Idle", new Dictionary<string, object?>());

        // Calling inspect without args — should still return accepted (discovery) with RequiredEventArgumentKeys
        var result = workflow.Inspect(instance, "Pay");

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeTrue();
        result.RequiredEventArgumentKeys.Should().ContainSingle().Which.Should().Be("Amount");
    }

    // ========================================================================================
    // INSPECT — stateless overload with event rules
    // ========================================================================================

    [Fact]
    public void Inspect_Stateless_EventRule_Violated_IsBlocked()
    {
        const string dsl = """
            machine Test
            state Idle initial
            state Done
            event Pay
              number Amount
              rule Amount > 0 "Amount must be positive"
            from Idle on Pay
              transition Done
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));

        var result = workflow.Inspect("Idle", "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        (result.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace).Should().BeFalse();
        result.Reasons.Should().ContainSingle(r => r.Contains("Amount must be positive", StringComparison.Ordinal));
    }

    // ========================================================================================
    // MODEL — no-rules case produces null collections (not empty)
    // ========================================================================================

    [Fact]
    public void Parse_NoRules_MachineHasNullTopLevelRules()
    {
        const string dsl = """
            machine Test
            number Balance = 0
            state Idle initial
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.TopLevelRules.Should().BeNull();
        machine.States.All(s => s.Rules == null).Should().BeTrue();
        machine.Fields[0].Rules.Should().BeNull();
    }

    [Fact]
    public void Parse_NoRules_EventHasNullRules()
    {
        const string dsl = """
            machine Test
            state Idle initial
            event Go
              number Value
            """;

        var machine = StateMachineDslParser.Parse(dsl);

        machine.Events[0].Rules.Should().BeNull();
    }
}
