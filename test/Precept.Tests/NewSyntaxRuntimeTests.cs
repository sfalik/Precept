using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Runtime tests for the new Precept language constructs:
/// rule/because, state ensures (in/to/from), event ensures (on),
/// state actions (to/from ->), transition rows, and edit blocks.
/// All DSL snippets use the new syntax (field/as, rule/because, -> pipeline).
/// </summary>
public class NewSyntaxRuntimeTests
{
    // ════════════════════════════════════════════════════════════════════
    // RULE — always checked post-commit
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Rule_Satisfied_TransitionAccepted()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must be non-negative"
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
    public void Rule_Violated_TransitionRejected()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must be non-negative"
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
    public void Rule_Violated_AtCompileTime_Throws()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must be non-negative"
            state Active initial
            event Reset
            from Active on Reset -> set Balance = -5 -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Balance must be non-negative*");
    }

    [Fact]
    public void Rule_LiteralSetViolation_AtCompileTime_Throws()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must be non-negative"
            state Active initial
            event Reset
            from Active on Reset -> set Balance = -1 -> transition Active
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Balance must be non-negative*");
    }

    [Fact]
    public void Rule_Multiple_AllChecked()
    {
        const string dsl = """
            precept Test
            field X as number default 10
            field Y as number default 10
            rule X >= 0 because "X non-negative"
            rule Y >= 0 because "Y non-negative"
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
    public void Rule_CheckedOnNoTransition()
    {
        const string dsl = """
            precept Test
            field Counter as number default 5
            rule Counter >= 0 because "Counter non-negative"
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
    // STATE ENSURE — in/to/from prepositions
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateEnsure_In_CheckedWhileResiding()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            in Active ensure Score > 0 because "Score positive while active"
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
    public void StateEnsure_To_CheckedOnEntry()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Draft initial
            state Published
            to Published ensure Score >= 10 because "Need enough score to publish"
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
    public void StateEnsure_To_SatisfiedOnEntry()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state Draft initial
            state Published
            to Published ensure Score >= 10 because "Need enough score to publish"
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
    public void StateEnsure_From_CheckedOnExit()
    {
        const string dsl = """
            precept Test
            field ClearedForExit as boolean default false
            state Active initial
            state Done
            from Active ensure ClearedForExit == true because "Must be cleared before leaving Active"
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
    public void StateEnsure_SelfTransition_ChecksTo()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            to Active ensure Score > 0 because "Score must be positive to enter Active"
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
    public void StateEnsure_Any_ExpandsToAllStates()
    {
        const string dsl = """
            precept Test
            field Health as number default 100
            state Alive initial
            state Dead
            in any ensure Health >= 0 because "Health never negative"
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
    // EVENT ENSURE — checked pre-transition (args only)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventEnsure_Violated_TransitionRejected()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Payment must be positive"
            from Active on Pay -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");
        var result = wf.Fire(inst, "Pay", new Dictionary<string, object?> { ["Amount"] = -5.0 });

        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Payment must be positive");
    }

    [Fact]
    public void EventEnsure_Satisfied_TransitionAccepted()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Payment must be positive"
            from Active on Pay -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");
        var result = wf.Fire(inst, "Pay", new Dictionary<string, object?> { ["Amount"] = 10.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void EventEnsure_InspectWithoutArgs_Skipped()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Payment must be positive"
            from Active on Pay -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");
        var result = wf.Inspect(inst, "Pay");

        // Inspect without args should not evaluate event ensures
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
    public void FullPipeline_EventEnsure_ThenActions_ThenRule()
    {
        const string dsl = """
            precept OrderSystem
            field Total as number default 0
            field OrderCount as number default 0
            rule Total >= 0 because "Total cannot be negative"
            state Open initial
            state Closed
            event Purchase with Amount as number
            on Purchase ensure Amount > 0 because "Amount must be positive"
            to Closed -> set OrderCount = OrderCount + 1
            from Open on Purchase -> set Total = Total + Purchase.Amount -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open", new Dictionary<string, object?> { ["Total"] = 0.0, ["OrderCount"] = 0.0 });

        // Negative amount blocked by event ensure
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
            to Done ensure Score >= 20 because "Need 20 to finish"
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
    public void CoerceEventArguments_StringNumber_ThenFire_EvaluatesEnsure()
    {
        // End-to-end: string "50" coerced to 50.0 then used in event ensure evaluation
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Must be positive"
            from Active on Pay -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Active");

        var rawArgs = new Dictionary<string, object?> { ["Amount"] = "50" };
        var coerced = wf.CoerceEventArguments("Pay", rawArgs)!;
        var result = wf.Fire(inst, "Pay", coerced);

        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    // ════════════════════════════════════════════════════════════════════
    // GUARDED RULES / ENSURES — Forms 1–3 when guards
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Fire_GuardedRule_GuardTrue_ViolationReported()
    {
        const string dsl = """
            precept Test
            field X as number default 200
            field Active as boolean default false
            state Open initial, Closed
            event Close with NewX as number
            rule X > 100 when Active because "X must be > 100 when active"
            from Open on Close -> set X = Close.NewX -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        // Start with Active=true, X=200 (satisfies invariant) — then fire setting X=0
        var inst = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 200.0, ["Active"] = true });
        var result = wf.Fire(inst, "Close", new Dictionary<string, object?> { ["NewX"] = 0.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().Contain(v => v.Message.Contains("X must be > 100"));
    }

    [Fact]
    public void Fire_GuardedRule_GuardFalse_NoViolation()
    {
        const string dsl = """
            precept Test
            field X as number default 200
            field Active as boolean default false
            state Open initial, Closed
            event Close with NewX as number
            rule X > 100 when Active because "X must be > 100 when active"
            from Open on Close -> set X = Close.NewX -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        // Active=false → guard is false → invariant skipped even though X becomes 0
        var inst = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 200.0, ["Active"] = false });
        var result = wf.Fire(inst, "Close", new Dictionary<string, object?> { ["NewX"] = 0.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Fire_GuardedStateEnsure_GuardTrue_ViolationReported()
    {
        const string dsl = """
            precept Test
            field X as number default 10
            field Active as boolean default false
            state Open initial, Closed
            event Reduce
            in Open ensure X > 0 when Active because "X must be positive when active"
            from Open on Reduce -> set X = X - 20 -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 10.0, ["Active"] = true });
        var result = wf.Fire(inst, "Reduce");

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().Contain(v => v.Message.Contains("X must be positive when active"));
    }

    [Fact]
    public void Fire_GuardedStateEnsure_GuardFalse_NoViolation()
    {
        const string dsl = """
            precept Test
            field X as number default 10
            field Active as boolean default false
            state Open initial, Closed
            event Reduce
            in Open ensure X > 0 when Active because "X must be positive when active"
            from Open on Reduce -> set X = X - 20 -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 10.0, ["Active"] = false });
        var result = wf.Fire(inst, "Reduce");

        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Fire_GuardedEventEnsure_GuardTrue_ViolationReported()
    {
        const string dsl = """
            precept Test
            state Open initial, Closed
            event Submit with Amount as number, Priority as number
            on Submit ensure Amount > 0 when Priority > 1 because "Amount required for high priority"
            from Open on Submit -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open");
        var result = wf.Fire(inst, "Submit", new Dictionary<string, object?> { ["Amount"] = 0.0, ["Priority"] = 5.0 });

        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        result.Violations.Should().Contain(v => v.Message.Contains("Amount required for high priority"));
    }

    [Fact]
    public void Fire_GuardedEventEnsure_GuardFalse_NoViolation()
    {
        const string dsl = """
            precept Test
            state Open initial, Closed
            event Submit with Amount as number, Priority as number
            on Submit ensure Amount > 0 when Priority > 1 because "Amount required for high priority"
            from Open on Submit -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open");
        var result = wf.Fire(inst, "Submit", new Dictionary<string, object?> { ["Amount"] = 0.0, ["Priority"] = 0.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Fire_MultipleGuardedRules_CollectAll()
    {
        const string dsl = """
            precept Test
            field X as number default 200
            field Y as number default 200
            field CheckX as boolean default false
            field CheckY as boolean default false
            state Open initial, Closed
            event Close with NewX as number, NewY as number
            rule X > 100 when CheckX because "X must be > 100"
            rule Y > 100 when CheckY because "Y must be > 100"
            from Open on Close -> set X = Close.NewX -> set Y = Close.NewY -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open", new Dictionary<string, object?>
        {
            ["X"] = 200.0, ["Y"] = 200.0,
            ["CheckX"] = true, ["CheckY"] = true
        });
        var result = wf.Fire(inst, "Close", new Dictionary<string, object?> { ["NewX"] = 0.0, ["NewY"] = 0.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Violations.Should().Contain(v => v.Message.Contains("X must be > 100"));
        result.Violations.Should().Contain(v => v.Message.Contains("Y must be > 100"));
    }

    [Fact]
    public void Fire_GuardedRule_WhenNot_SkipsWhenTrue()
    {
        const string dsl = """
            precept Test
            field X as number default 200
            field Active as boolean default true
            state Open initial, Closed
            event Close with NewX as number
            rule X > 100 when not Active because "X must be > 100 when inactive"
            from Open on Close -> set X = Close.NewX -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Active=true → guard (not Active) is false → invariant skipped
        var inst1 = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 200.0, ["Active"] = true });
        var r1 = wf.Fire(inst1, "Close", new Dictionary<string, object?> { ["NewX"] = 0.0 });
        r1.Outcome.Should().Be(TransitionOutcome.Transition);
        r1.Violations.Should().BeEmpty();

        // Active=false → guard (not Active) is true → invariant applies, X=0 fails
        var inst2 = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 200.0, ["Active"] = false });
        var r2 = wf.Fire(inst2, "Close", new Dictionary<string, object?> { ["NewX"] = 0.0 });
        r2.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        r2.Violations.Should().Contain(v => v.Message.Contains("X must be > 100 when inactive"));
    }

    // ════════════════════════════════════════════════════════════════════
    // GUARDED STATE/EVENT ASSERTS — when not
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Fire_GuardedStateEnsure_WhenNot_SkipsWhenTrue()
    {
        const string dsl = """
            precept Test
            field X as number default 10
            field Bypass as boolean default false
            state Open initial, Closed
            event Reduce
            in Open ensure X > 0 when not Bypass because "X must be positive unless bypassed"
            from Open on Reduce -> set X = X - 20 -> no transition
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Bypass=true → guard (not Bypass) is false → assert skipped, no violation
        var inst1 = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 10.0, ["Bypass"] = true });
        var r1 = wf.Fire(inst1, "Reduce");
        r1.Outcome.Should().Be(TransitionOutcome.NoTransition);
        r1.Violations.Should().BeEmpty();

        // Bypass=false → guard (not Bypass) is true → assert applies, X=-10 fails
        var inst2 = wf.CreateInstance("Open", new Dictionary<string, object?> { ["X"] = 10.0, ["Bypass"] = false });
        var r2 = wf.Fire(inst2, "Reduce");
        r2.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        r2.Violations.Should().Contain(v => v.Message.Contains("X must be positive unless bypassed"));
    }

    [Fact]
    public void Fire_GuardedEventEnsure_WhenNot_SkipsWhenTrue()
    {
        const string dsl = """
            precept Test
            state Open initial, Closed
            event Submit with Amount as number, IsDraft as boolean
            on Submit ensure Submit.Amount > 0 when not Submit.IsDraft because "Amount required for non-draft"
            from Open on Submit -> transition Closed
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("Open");

        // IsDraft=true → guard (not IsDraft) is false → assert skipped, Amount=0 allowed
        var r1 = wf.Fire(inst, "Submit", new Dictionary<string, object?> { ["Amount"] = 0.0, ["IsDraft"] = true });
        r1.Outcome.Should().Be(TransitionOutcome.Transition);
        r1.Violations.Should().BeEmpty();

        // IsDraft=false → guard (not IsDraft) is true → assert applies, Amount=0 fails
        var inst2 = wf.CreateInstance("Open");
        var r2 = wf.Fire(inst2, "Submit", new Dictionary<string, object?> { ["Amount"] = 0.0, ["IsDraft"] = false });
        r2.Outcome.Should().Be(TransitionOutcome.Rejected);
        r2.Violations.Should().Contain(v => v.Message.Contains("Amount required for non-draft"));
    }
}
