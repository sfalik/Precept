using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Runtime tests for the new Precept language constructs:
/// invariant/because, state asserts (in/to/from), event asserts (on),
/// state actions (to/from ->), transition rows, and edit blocks.
/// All DSL snippets use the new syntax (field/as, invariant/because, -> pipeline).
/// </summary>
public class NewSyntaxRuntimeTests
{
    // ════════════════════════════════════════════════════════════════════
    // INVARIANT — always checked post-commit
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Invariant_Satisfied_TransitionAccepted()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must be non-negative"
            state Active initial
            state Done
            event Spend with Amount as number
            from Active on Spend -> set Balance = Balance - Spend.Amount -> transition Done
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });
        var result = wf.Fire(inst, "Spend", new Dictionary<string, object?> { ["Amount"] = 50.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.CurrentState.Should().Be("Done");
        result.UpdatedInstance.InstanceData["Balance"].Should().Be(50.0);
    }

    [Fact]
    public void Invariant_Violated_TransitionRejected()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must be non-negative"
            state Active initial
            state Done
            event Spend with Amount as number
            from Active on Spend -> set Balance = Balance - Spend.Amount -> transition Done
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 100.0 });
        var result = wf.Fire(inst, "Spend", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Balance must be non-negative");
    }

    [Fact]
    public void Invariant_Violated_AtCompileTime_Throws()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must be non-negative"
            state Active initial
            event Reset
            from Active on Reset -> set Balance = -5 -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Balance must be non-negative*");
    }

    [Fact]
    public void Invariant_LiteralSetViolation_AtCompileTime_Throws()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must be non-negative"
            state Active initial
            event Reset
            from Active on Reset -> set Balance = -1 -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Balance must be non-negative*");
    }

    [Fact]
    public void Invariant_Multiple_AllChecked()
    {
        const string dsl = """
            precept Test
            field X as number default 10
            field Y as number default 10
            invariant X >= 0 because "X non-negative"
            invariant Y >= 0 because "Y non-negative"
            state Active initial
            event Adjust with Dx as number, Dy as number
            from Active on Adjust -> set X = X + Adjust.Dx -> set Y = Y + Adjust.Dy -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["X"] = 10.0, ["Y"] = 10.0 });
        var result = wf.Fire(inst, "Adjust", new Dictionary<string, object?> { ["Dx"] = -20.0, ["Dy"] = -20.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Invariant_CheckedOnNoTransition()
    {
        const string dsl = """
            precept Test
            field Counter as number default 5
            invariant Counter >= 0 because "Counter non-negative"
            state Active initial
            event Reduce
            from Active on Reduce -> set Counter = Counter - 10 -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["Counter"] = 5.0 });
        var result = wf.Fire(inst, "Reduce");

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Counter non-negative");
    }

    // ════════════════════════════════════════════════════════════════════
    // STATE ASSERT — in/to/from prepositions
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateAssert_In_CheckedWhileResiding()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            in Active assert Score > 0 because "Score positive while active"
            event Adjust
            from Active on Adjust -> set Score = Score - 20 -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["Score"] = 10.0 });
        var result = wf.Fire(inst, "Adjust");

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Score positive while active");
    }

    [Fact]
    public void StateAssert_To_CheckedOnEntry()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Draft initial
            state Published
            to Published assert Score >= 10 because "Need enough score to publish"
            event Publish
            from Draft on Publish -> transition Published
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Draft", new Dictionary<string, object?> { ["Score"] = 5.0 });
        var result = wf.Fire(inst, "Publish");

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Need enough score to publish");
    }

    [Fact]
    public void StateAssert_To_SatisfiedOnEntry()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Draft initial
            state Published
            to Published assert Score >= 10 because "Need enough score to publish"
            event Publish
            from Draft on Publish -> transition Published
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Draft", new Dictionary<string, object?> { ["Score"] = 15.0 });
        var result = wf.Fire(inst, "Publish");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.CurrentState.Should().Be("Published");
    }

    [Fact]
    public void StateAssert_From_CheckedOnExit()
    {
        const string dsl = """
            precept Test
            field ClearedForExit as boolean default false
            state Active initial
            state Done
            from Active assert ClearedForExit == true because "Must be cleared before leaving Active"
            event Finish
            from Active on Finish -> transition Done
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["ClearedForExit"] = false });
        var result = wf.Fire(inst, "Finish");

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Must be cleared before leaving Active");
    }

    [Fact]
    public void StateAssert_SelfTransition_ChecksTo()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            to Active assert Score > 0 because "Score must be positive to enter Active"
            event Penalize with Amount as number
            from Active on Penalize -> set Score = Score - Penalize.Amount -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["Score"] = 10.0 });
        var result = wf.Fire(inst, "Penalize", new Dictionary<string, object?> { ["Amount"] = 15.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Score must be positive to enter Active");
    }

    [Fact]
    public void StateAssert_Any_ExpandsToAllStates()
    {
        const string dsl = """
            precept Test
            field Health as number default 100
            state Alive initial
            state Dead
            in any assert Health >= 0 because "Health never negative"
            event TakeDamage with Amount as number
            from Alive on TakeDamage when Health - TakeDamage.Amount > 0 -> set Health = Health - TakeDamage.Amount -> transition Alive
            from Alive on TakeDamage -> set Health = 0 -> transition Dead
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Alive", new Dictionary<string, object?> { ["Health"] = 100.0 });

        // Take 50 damage -> still alive
        var r1 = wf.Fire(inst, "TakeDamage", new Dictionary<string, object?> { ["Amount"] = 50.0 });
        r1.Outcome.Should().Be(TransitionOutcome.Transition);
        r1.UpdatedInstance!.InstanceData["Health"].Should().Be(50.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // EVENT ASSERT — checked pre-transition (args only)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventAssert_Violated_TransitionRejected()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Payment must be positive"
            from Active on Pay -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");
        var result = wf.Fire(inst, "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Payment must be positive");
    }

    [Fact]
    public void EventAssert_Satisfied_TransitionAccepted()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Payment must be positive"
            from Active on Pay -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");
        var result = wf.Fire(inst, "Pay", new Dictionary<string, object?> { ["Amount"] = 10.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void EventAssert_InspectWithoutArgs_Skipped()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Payment must be positive"
            from Active on Pay -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");
        var result = wf.Inspect(inst, "Pay");

        // Inspect without args should not evaluate event asserts
        (result.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════
    // STATE ACTIONS — to/from <State> -> <mutations>
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateAction_EntryAction_ExecutedOnTransition()
    {
        const string dsl = """
            precept Test
            field EntryCount as number default 0
            state A initial
            state B
            to B -> set EntryCount = EntryCount + 1
            event Go
            from A on Go -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["EntryCount"] = 0.0 });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["EntryCount"].Should().Be(1.0);
    }

    [Fact]
    public void StateAction_ExitAction_ExecutedOnTransition()
    {
        const string dsl = """
            precept Test
            field ExitCount as number default 0
            state A initial
            state B
            from A -> set ExitCount = ExitCount + 1
            event Go
            from A on Go -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["ExitCount"] = 0.0 });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["ExitCount"].Should().Be(1.0);
    }

    [Fact]
    public void StateAction_Pipeline_ExitThenRowThenEntry()
    {
        // Verifies execution order: exit actions → row mutations → entry actions
        const string dsl = """
            precept Test
            field Log as number default 0
            state A initial
            state B
            from A -> set Log = 1
            to B -> set Log = Log + 10
            event Go
            from A on Go -> set Log = Log + 100 -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Log"] = 0.0 });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        // Exit sets Log=1, Row adds 100 → Log=101, Entry adds 10 → Log=111
        result.UpdatedInstance!.InstanceData["Log"].Should().Be(111.0);
    }

    [Fact]
    public void StateAction_NotExecutedOnNoTransition()
    {
        const string dsl = """
            precept Test
            field Counter as number default 0
            state A initial
            from A -> set Counter = Counter + 1
            to A -> set Counter = Counter + 10
            event Ping
            from A on Ping -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Counter"] = 0.0 });
        var result = wf.Fire(inst, "Ping");

        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
        // No transition means no exit/entry actions
        result.UpdatedInstance!.InstanceData["Counter"].Should().Be(0.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // TRANSITION ROWS — from <State> on <Event> [when ...] -> ... -> outcome
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionRow_SimpleTransition_Accepted()
    {
        const string dsl = """
            precept Test
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A");
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.CurrentState.Should().Be("B");
    }

    [Fact]
    public void TransitionRow_NoTransition_AcceptedInPlace()
    {
        const string dsl = """
            precept Test
            field Counter as number default 0
            state A initial
            event Ping
            from A on Ping -> set Counter = Counter + 1 -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Counter"] = 0.0 });
        var result = wf.Fire(inst, "Ping");

        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
        result.UpdatedInstance!.InstanceData["Counter"].Should().Be(1.0);
    }

    [Fact]
    public void TransitionRow_Reject_Rejected()
    {
        const string dsl = """
            precept Test
            state A initial
            event Bad
            from A on Bad -> reject "Not allowed"
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A");
        var result = wf.Fire(inst, "Bad");

        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Not allowed");
    }

    [Fact]
    public void TransitionRow_WithWhenGuard_FirstMatchWins()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Low initial
            state Medium
            state High
            event Evaluate
            from Low on Evaluate when Score >= 20 -> transition High
            from Low on Evaluate when Score >= 10 -> transition Medium
            from Low on Evaluate -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Score 10 → Medium (first matching guard)
        var inst = wf.CreateInstance("Low", new Dictionary<string, object?> { ["Score"] = 10.0 });
        var result = wf.Fire(inst, "Evaluate");
        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.CurrentState.Should().Be("Medium");

        // Score 25 → High
        inst = wf.CreateInstance("Low", new Dictionary<string, object?> { ["Score"] = 25.0 });
        result = wf.Fire(inst, "Evaluate");
        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.CurrentState.Should().Be("High");

        // Score 5 → no transition (fallback)
        inst = wf.CreateInstance("Low", new Dictionary<string, object?> { ["Score"] = 5.0 });
        result = wf.Fire(inst, "Evaluate");
        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    [Fact]
    public void TransitionRow_WhenGuard_AllFail_NotApplicable()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state A initial
            state B
            event Go
            from A on Go when Score > 100 -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Score"] = 5.0 });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Unmatched);
    }

    [Fact]
    public void TransitionRow_WithSetAndMutations()
    {
        const string dsl = """
            precept Test
            field Total as number default 0
            field Items as set of string
            state Cart initial
            state Checkout
            event AddAndCheckout with Item as string, Price as number
            from Cart on AddAndCheckout -> set Total = Total + AddAndCheckout.Price -> add Items AddAndCheckout.Item -> transition Checkout
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Cart", new Dictionary<string, object?> { ["Total"] = 0.0 });
        var result = wf.Fire(inst, "AddAndCheckout", new Dictionary<string, object?> { ["Item"] = "Widget", ["Price"] = 9.99 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Total"].Should().Be(9.99);
    }

    [Fact]
    public void TransitionRow_MultiState_ExpandsCorrectly()
    {
        const string dsl = """
            precept Test
            state A initial
            state B
            state C
            event Reset
            from A, B on Reset -> transition C
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var res1 = wf.Fire(wf.CreateInstance("A"), "Reset");
        res1.Outcome.Should().Be(TransitionOutcome.Transition);
        res1.UpdatedInstance!.CurrentState.Should().Be("C");

        var res2 = wf.Fire(wf.CreateInstance("B"), "Reset");
        res2.Outcome.Should().Be(TransitionOutcome.Transition);
        res2.UpdatedInstance!.CurrentState.Should().Be("C");
    }

    [Fact]
    public void TransitionRow_Any_MatchesAllStates()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state A initial
            state B
            event Reset
            event Go
            from A on Go -> transition B
            from any on Reset -> set X = 0 -> transition A
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Fire from B
        var inst = wf.CreateInstance("B", new Dictionary<string, object?> { ["X"] = 42.0 });
        var result = wf.Fire(inst, "Reset");
        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.CurrentState.Should().Be("A");
        result.UpdatedInstance.InstanceData["X"].Should().Be(0.0);
    }

    [Fact]
    public void TransitionRow_UndefinedEvent_NotDefined()
    {
        const string dsl = """
            precept Test
            state A initial
            event Go
            from A on Go -> transition A
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A");
        var result = wf.Fire(inst, "Unknown");

        result.Outcome.Should().Be(TransitionOutcome.Undefined);
    }

    // ════════════════════════════════════════════════════════════════════
    // FULL PIPELINE — combining all constructs
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void FullPipeline_EventAssert_ThenActions_ThenInvariant()
    {
        const string dsl = """
            precept OrderSystem
            field Total as number default 0
            field OrderCount as number default 0
            invariant Total >= 0 because "Total cannot be negative"
            state Open initial
            state Closed
            event Purchase with Amount as number
            on Purchase assert Amount > 0 because "Amount must be positive"
            to Closed -> set OrderCount = OrderCount + 1
            from Open on Purchase -> set Total = Total + Purchase.Amount -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open", new Dictionary<string, object?> { ["Total"] = 0.0, ["OrderCount"] = 0.0 });

        // Negative amount blocked by event assert
        var r1 = wf.Fire(inst, "Purchase", new Dictionary<string, object?> { ["Amount"] = -10.0 });
        r1.Outcome.Should().Be(TransitionOutcome.Rejected);
        r1.Violations.Should().ContainSingle().Which.Message.Should().Contain("Amount must be positive");

        // Valid purchase -> exit actions → row mutations → entry actions → validation
        var r2 = wf.Fire(inst, "Purchase", new Dictionary<string, object?> { ["Amount"] = 50.0 });
        r2.Outcome.Should().Be(TransitionOutcome.Transition);
        r2.UpdatedInstance!.CurrentState.Should().Be("Closed");
        r2.UpdatedInstance.InstanceData["Total"].Should().Be(50.0);
        r2.UpdatedInstance.InstanceData["OrderCount"].Should().Be(1.0);
    }

    [Fact]
    public void FullPipeline_InspectShowsAllOutcomes()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Done
            event Finish
            event Retry
            to Done assert Score >= 20 because "Need 20 to finish"
            from Active on Finish -> transition Done
            from Active on Retry -> set Score = Score + 5 -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active", new Dictionary<string, object?> { ["Score"] = 10.0 });

        // Inspect all — Finish should be rejected (Score < 20), Retry accepted
        var inspection = wf.Inspect(inst);
        inspection.Events.Should().HaveCount(2);

        var finishResult = inspection.Events.First(e => e.EventName == "Finish");
        finishResult.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);

        var retryResult = inspection.Events.First(e => e.EventName == "Retry");
        (retryResult.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════
    // EDIT BLOCK — model parsing only (runtime Update API is deferred)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void EditBlock_ParsedAndAvailableOnModel()
    {
        const string dsl = """
            precept Test
            field Name as string default "unnamed"
            field Score as number default 0
            state Active initial
            state Locked
            in Active edit Name, Score
            event Lock
            from Active on Lock -> transition Locked
            """;

        var model = PreceptParser.Parse(dsl);
        model.EditBlocks.Should().HaveCount(1);
        model.EditBlocks![0].State.Should().Be("Active");
        model.EditBlocks![0].FieldNames.Should().BeEquivalentTo("Name", "Score");

        // Compiles without error (edit blocks don't affect runtime yet)
        var wf = PreceptCompiler.Compile(model);
        wf.Should().NotBeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // COLLECTION MUTATIONS in new syntax
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionRow_CollectionAdd_Works()
    {
        const string dsl = """
            precept Test
            field Tags as set of string
            state A initial
            event Tag with Value as string
            from A on Tag -> add Tags Tag.Value -> transition A
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A");
        var result = wf.Fire(inst, "Tag", new Dictionary<string, object?> { ["Value"] = "important" });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void TransitionRow_CollectionEnqueueDequeue_Works()
    {
        const string dsl = """
            precept Test
            field Queue as queue of string
            field Last as string nullable
            state A initial
            event Enq with Item as string
            event Deq
            from A on Enq -> enqueue Queue Enq.Item -> no transition
            from A on Deq -> dequeue Queue into Last -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A");

        var r1 = wf.Fire(inst, "Enq", new Dictionary<string, object?> { ["Item"] = "first" });
        r1.Outcome.Should().Be(TransitionOutcome.NoTransition);

        var r2 = wf.Fire(r1.UpdatedInstance!, "Enq", new Dictionary<string, object?> { ["Item"] = "second" });
        r2.Outcome.Should().Be(TransitionOutcome.NoTransition);

        var r3 = wf.Fire(r2.UpdatedInstance!, "Deq");
        r3.Outcome.Should().Be(TransitionOutcome.NoTransition);
        r3.UpdatedInstance!.InstanceData["Last"].Should().Be("first");
    }

    [Fact]
    public void StateAction_WithCollectionMutation_Works()
    {
        const string dsl = """
            precept Test
            field Log as queue of string
            state A initial
            state B
            to B -> enqueue Log "arrived"
            event Go
            from A on Go -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A");
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.CurrentState.Should().Be("B");
    }

    // ════════════════════════════════════════════════════════════════════
    // EXPRESSIONS — verified through new syntax
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expression_ArithmeticInSet()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state A initial
            event Compute with Val as number
            from A on Compute -> set X = (Compute.Val + 1) * 2 -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["X"] = 0.0 });
        var result = wf.Fire(inst, "Compute", new Dictionary<string, object?> { ["Val"] = 4.0 });

        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
        result.UpdatedInstance!.InstanceData["X"].Should().Be(10.0); // (4+1)*2 = 10
    }

    [Fact]
    public void Expression_StringConcatenationInSet()
    {
        const string dsl = """
            precept Test
            field Msg as string default "hello"
            state A initial
            event Append with Suffix as string
            from A on Append -> set Msg = Msg + Append.Suffix -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Msg"] = "hello" });
        var result = wf.Fire(inst, "Append", new Dictionary<string, object?> { ["Suffix"] = " world" });

        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
        result.UpdatedInstance!.InstanceData["Msg"].Should().Be("hello world");
    }

    [Fact]
    public void Expression_BooleanLogicInGuard()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number default 0
            state A initial
            state B
            event Go
            from A on Go when X > 0 and Y > 0 -> transition B
            from A on Go -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?> { ["X"] = 5.0, ["Y"] = 5.0 });
        wf.Fire(inst1, "Go").Outcome.Should().Be(TransitionOutcome.Transition);

        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?> { ["X"] = 5.0, ["Y"] = 0.0 });
        wf.Fire(inst2, "Go").Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    [Fact]
    public void Expression_NullCheckInGuard()
    {
        const string dsl = """
            precept Test
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name != null -> transition B
            from A on Go -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = "Alice" });
        wf.Fire(inst1, "Go").Outcome.Should().Be(TransitionOutcome.Transition);

        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = null });
        wf.Fire(inst2, "Go").Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    [Fact]
    public void Expression_ContainsInGuard()
    {
        const string dsl = """
            precept Test
            field Allowed as set of string
            state A initial
            state B
            event Go with Role as string
            from A on Go when Allowed contains Go.Role -> transition B
            from A on Go -> reject "Not authorized"
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A");

        // Without adding to set, "admin" is not contained
        var r1 = wf.Fire(inst, "Go", new Dictionary<string, object?> { ["Role"] = "admin" });
        r1.Outcome.Should().Be(TransitionOutcome.Rejected);
    }

    // ════════════════════════════════════════════════════════════════════
    // COERCE — CoerceEventArguments type conversions (Category B)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void CoerceEventArguments_StringToNumber_IsConvertedToDouble()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Deposit with Amount as number
            from Active on Deposit -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var coerced = wf.CoerceEventArguments("Deposit", new Dictionary<string, object?> { ["Amount"] = "42.5" });

        coerced.Should().NotBeNull();
        coerced!["Amount"].Should().Be(42.5);
    }

    [Fact]
    public void CoerceEventArguments_IntToNumber_IsConvertedToDouble()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Deposit with Amount as number
            from Active on Deposit -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var coerced = wf.CoerceEventArguments("Deposit", new Dictionary<string, object?> { ["Amount"] = 100 });

        coerced.Should().NotBeNull();
        coerced!["Amount"].Should().Be(100.0).And.BeOfType<double>();
    }

    [Fact]
    public void CoerceEventArguments_StringBoolean_TrueAndFalse_AreConverted()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Toggle with Flag as boolean
            from Active on Toggle -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var coercedTrue = wf.CoerceEventArguments("Toggle", new Dictionary<string, object?> { ["Flag"] = "true" });
        coercedTrue!["Flag"].Should().Be(true);

        var coercedFalse = wf.CoerceEventArguments("Toggle", new Dictionary<string, object?> { ["Flag"] = "FALSE" });
        coercedFalse!["Flag"].Should().Be(false);
    }

    [Fact]
    public void CoerceEventArguments_NullValue_PassesThrough()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Submit with Note as string nullable
            from Active on Submit -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var coerced = wf.CoerceEventArguments("Submit", new Dictionary<string, object?> { ["Note"] = null });

        coerced.Should().NotBeNull();
        coerced!["Note"].Should().BeNull();
    }

    [Fact]
    public void CoerceEventArguments_StringNumber_ThenFire_EvaluatesAssert()
    {
        // End-to-end: string "50" coerced to 50.0 then used in event assert evaluation
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Must be positive"
            from Active on Pay -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");

        var rawArgs = new Dictionary<string, object?> { ["Amount"] = "50" };
        var coerced = wf.CoerceEventArguments("Pay", rawArgs)!;
        var result = wf.Fire(inst, "Pay", coerced);

        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }
}
