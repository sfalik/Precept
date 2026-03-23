using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public class PreceptCollectionParsingTests
{
    [Fact]
    public void Parse_SetDeclaration_CorrectCollectionKindAndInnerType()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.CollectionFields.Should().ContainSingle();
        var field = machine.CollectionFields[0];
        field.Name.Should().Be("Floors");
        field.CollectionKind.Should().Be(PreceptCollectionKind.Set);
        field.InnerType.Should().Be(PreceptScalarType.Number);
    }

    [Fact]
    public void Parse_QueueDeclaration_CorrectCollectionKindAndInnerType()
    {
        const string dsl = """
            precept Sample
            field Messages as queue of string
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.CollectionFields.Should().ContainSingle();
        var field = machine.CollectionFields[0];
        field.Name.Should().Be("Messages");
        field.CollectionKind.Should().Be(PreceptCollectionKind.Queue);
        field.InnerType.Should().Be(PreceptScalarType.String);
    }

    [Fact]
    public void Parse_StackDeclaration_CorrectCollectionKindAndInnerType()
    {
        const string dsl = """
            precept Sample
            field Flags as stack of boolean
            state Idle initial
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.CollectionFields.Should().ContainSingle();
        var field = machine.CollectionFields[0];
        field.Name.Should().Be("Flags");
        field.CollectionKind.Should().Be(PreceptCollectionKind.Stack);
        field.InnerType.Should().Be(PreceptScalarType.Boolean);
    }

    [Fact]
    public void Parse_DuplicateCollectionName_Throws()
    {
        const string dsl = """
            precept Sample
            field Items as set of number
            field Items as set of number
            state Idle initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate*Items*");
    }

    [Fact]
    public void Parse_CollectionNameConflictsWithDataField_Throws()
    {
        const string dsl = """
            precept Sample
            field Items as number default 0
            field Items as set of number
            state Idle initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate*Items*");
    }

    [Fact]
    public void Parse_AddMutation_OnSet()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> add Floors 5 -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        var mut = row.CollectionMutations![0];
        mut.Verb.Should().Be(PreceptCollectionMutationVerb.Add);
        mut.TargetField.Should().Be("Floors");
        mut.ExpressionText.Should().Be("5");
    }

    [Fact]
    public void Parse_RemoveMutation_OnSet()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> remove Floors 5 -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        var mut = row.CollectionMutations![0];
        mut.Verb.Should().Be(PreceptCollectionMutationVerb.Remove);
        mut.TargetField.Should().Be("Floors");
    }

    [Fact]
    public void Parse_EnqueueMutation_OnQueue()
    {
        const string dsl = """
            precept Sample
            field Tasks as queue of string
            state Idle initial
            state Active
            event Go
            from Idle on Go -> enqueue Tasks "work" -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        var mut = row.CollectionMutations![0];
        mut.Verb.Should().Be(PreceptCollectionMutationVerb.Enqueue);
        mut.TargetField.Should().Be("Tasks");
    }

    [Fact]
    public void Parse_DequeueMutation_OnQueue()
    {
        const string dsl = """
            precept Sample
            field Tasks as queue of string
            state Idle initial
            state Active
            event Go
            from Idle on Go -> dequeue Tasks -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        var mut = row.CollectionMutations![0];
        mut.Verb.Should().Be(PreceptCollectionMutationVerb.Dequeue);
        mut.TargetField.Should().Be("Tasks");
        mut.ExpressionText.Should().BeNull();
    }

    [Fact]
    public void Parse_PushMutation_OnStack()
    {
        const string dsl = """
            precept Sample
            field History as stack of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> push History 42 -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        var mut = row.CollectionMutations![0];
        mut.Verb.Should().Be(PreceptCollectionMutationVerb.Push);
        mut.TargetField.Should().Be("History");
    }

    [Fact]
    public void Parse_PopMutation_OnStack()
    {
        const string dsl = """
            precept Sample
            field History as stack of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> pop History -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        var mut = row.CollectionMutations![0];
        mut.Verb.Should().Be(PreceptCollectionMutationVerb.Pop);
        mut.TargetField.Should().Be("History");
    }

    [Fact]
    public void Parse_ClearMutation_OnSet()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> clear Floors -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        var mut = row.CollectionMutations![0];
        mut.Verb.Should().Be(PreceptCollectionMutationVerb.Clear);
        mut.TargetField.Should().Be("Floors");
    }

    [Fact]
    public void Parse_EnqueueOnSet_Throws()
    {
        const string dsl = """
            precept Sample
            field Items as set of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> enqueue Items 1 -> transition Active
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*enqueue*set*Items*");
    }

    [Fact]
    public void Parse_PushOnQueue_Throws()
    {
        const string dsl = """
            precept Sample
            field Items as queue of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> push Items 1 -> transition Active
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*push*queue*Items*");
    }

    [Fact]
    public void Parse_AddOnQueue_Throws()
    {
        const string dsl = """
            precept Sample
            field Items as queue of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> add Items 1 -> transition Active
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*add*queue*Items*");
    }

    [Fact]
    public void Parse_MutationOnUnknownCollection_Throws()
    {
        const string dsl = """
            precept Sample
            state Idle initial
            state Active
            event Go
            from Idle on Go -> add Missing 1 -> transition Active
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unknown collection*Missing*");
    }

    [Fact]
    public void Parse_MultipleMutations_PreservesOrder()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> add Floors 3 -> add Floors 5 -> remove Floors 1 -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().HaveCount(3);
        row.CollectionMutations![0].Verb.Should().Be(PreceptCollectionMutationVerb.Add);
        row.CollectionMutations![0].ExpressionText.Should().Be("3");
        row.CollectionMutations![1].Verb.Should().Be(PreceptCollectionMutationVerb.Add);
        row.CollectionMutations![1].ExpressionText.Should().Be("5");
        row.CollectionMutations![2].Verb.Should().Be(PreceptCollectionMutationVerb.Remove);
        row.CollectionMutations![2].ExpressionText.Should().Be("1");
    }

    [Fact]
    public void Parse_MutationInIfBranch_WithNoTransition()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            field Count as number default 0
            state Idle initial
            state Active
            event Go
            from Idle on Go when Count > 0 -> add Floors 1 -> no transition
            from Idle on Go -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        machine.TransitionRows.Should().HaveCount(2);
        var noTransRow = machine.TransitionRows!.Single(r => r.Outcome is NoTransition);
        noTransRow.CollectionMutations.Should().ContainSingle();
        noTransRow.CollectionMutations![0].Verb.Should().Be(PreceptCollectionMutationVerb.Add);
    }

    [Fact]
    public void Parse_MutationWithExpressionReferringToDataField()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            field Target as number default 5
            state Idle initial
            state Active
            event Go
            from Idle on Go -> add Floors Target -> transition Active
            """;

        var machine = PreceptParser.Parse(dsl);

        var row = machine.TransitionRows!.Single(r => r.Outcome is StateTransition st && st.TargetState == "Active");
        row.CollectionMutations.Should().ContainSingle();
        row.CollectionMutations![0].ExpressionText.Should().Be("Target");
    }
}

public class PreceptCollectionRuntimeTests
{
    [Fact]
    public void Fire_AddToSet_CollectionContainsValue()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state Idle initial
            state Active
            event Go
            from Idle on Go -> add Floors 3 -> transition Active
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Go");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance.Should().NotBeNull();
        var collection = fire.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        collection.Should().NotBeNull();
        collection!.Count.Should().Be(1);
        collection.Contains(3d).Should().BeTrue();
    }

    [Fact]
    public void Fire_AddAndRemoveFromSet_CollectionReflectsBoth()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Step1
            event Step2
            from A on Step1 -> add Floors 3 -> add Floors 5 -> add Floors 7 -> transition B
            from B on Step2 -> remove Floors 5 -> transition C
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Step1");
        (fire1.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col1 = fire1.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col1!.Count.Should().Be(3);

        var fire2 = workflow.Fire(fire1.UpdatedInstance, "Step2");
        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col2 = fire2.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col2!.Count.Should().Be(2);
        col2.Contains(5d).Should().BeFalse();
        col2.Contains(3d).Should().BeTrue();
        col2.Contains(7d).Should().BeTrue();
    }

    [Fact]
    public void Fire_EnqueueAndDequeue_FIFO()
    {
        const string dsl = """
            precept Sample
            field Tasks as queue of string
            state A initial
            state B
            state C
            event Enq
            event Deq
            from A on Enq -> enqueue Tasks "first" -> enqueue Tasks "second" -> transition B
            from B on Deq -> dequeue Tasks -> transition C
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Enq");
        (fire1.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col1 = fire1.UpdatedInstance!.InstanceData["Tasks"] as List<object>;
        col1!.Count.Should().Be(2);

        var fire2 = workflow.Fire(fire1.UpdatedInstance, "Deq");
        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col2 = fire2.UpdatedInstance!.InstanceData["Tasks"] as List<object>;
        col2!.Count.Should().Be(1);
        // "first" was dequeued, "second" remains
        col2[0].Should().Be("second");
    }

    [Fact]
    public void Fire_PushAndPop_LIFO()
    {
        const string dsl = """
            precept Sample
            field History as stack of number
            state A initial
            state B
            state C
            event Push
            event Pop
            from A on Push -> push History 10 -> push History 20 -> transition B
            from B on Pop -> pop History -> transition C
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Push");
        (fire1.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col1 = fire1.UpdatedInstance!.InstanceData["History"] as List<object>;
        col1!.Count.Should().Be(2);
        col1[^1].Should().Be(20d);

        var fire2 = workflow.Fire(fire1.UpdatedInstance, "Pop");
        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col2 = fire2.UpdatedInstance!.InstanceData["History"] as List<object>;
        col2!.Count.Should().Be(1);
        col2[^1].Should().Be(10d);
    }

    [Fact]
    public void Fire_ClearCollection_EmptiesIt()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Add
            event Reset
            from A on Add -> add Floors 1 -> add Floors 2 -> transition B
            from B on Reset -> clear Floors -> transition C
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var col1 = fire1.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col1!.Count.Should().Be(2);

        var fire2 = workflow.Fire(fire1.UpdatedInstance, "Reset");
        var col2 = fire2.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col2!.Count.Should().Be(0);
    }

    [Fact]
    public void Fire_DequeueFromEmpty_RejectsTransition()
    {
        const string dsl = """
            precept Sample
            field Tasks as queue of string
            state A initial
            state B
            event Deq
            from A on Deq -> dequeue Tasks -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Deq");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        fire.Violations.Should().ContainSingle().Which.Message.Should().Contain("empty queue");
    }

    [Fact]
    public void Fire_PopFromEmpty_RejectsTransition()
    {
        const string dsl = """
            precept Sample
            field History as stack of number
            state A initial
            state B
            event Pop
            from A on Pop -> pop History -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Pop");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        fire.Violations.Should().ContainSingle().Which.Message.Should().Contain("empty stack");
    }

    [Fact]
    public void Guard_ContainsExpression_AllowsTransition()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Add
            event Check
            from A on Add -> add Floors 3 -> transition B
            from B on Check when Floors contains 3 -> transition C
            from B on Check -> reject "not found"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Check");

        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire2.NewState.Should().Be("C");
    }

    [Fact]
    public void Guard_ContainsExpression_BlocksTransition()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Add
            event Check
            from A on Add -> add Floors 3 -> transition B
            from B on Check when Floors contains 99 -> transition C
            from B on Check -> reject "not found"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Check");

        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
    }

    [Fact]
    public void Guard_CollectionCountProperty_Works()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Add
            event Check
            from A on Add -> add Floors 3 -> add Floors 5 -> transition B
            from B on Check when Floors.count > 1 -> transition C
            from B on Check -> reject "too few"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Check");

        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire2.NewState.Should().Be("C");
    }

    [Fact]
    public void Guard_CollectionMinProperty_Works()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Add
            event Check
            from A on Add -> add Floors 3 -> add Floors 7 -> add Floors 1 -> transition B
            from B on Check when Floors.min == 1 -> transition C
            from B on Check -> reject "wrong min"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Check");

        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire2.NewState.Should().Be("C");
    }

    [Fact]
    public void Guard_CollectionMaxProperty_Works()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Add
            event Check
            from A on Add -> add Floors 3 -> add Floors 7 -> add Floors 1 -> transition B
            from B on Check when Floors.max == 7 -> transition C
            from B on Check -> reject "wrong max"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Check");

        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire2.NewState.Should().Be("C");
    }

    [Fact]
    public void Guard_QueuePeekProperty_ShowsFront()
    {
        const string dsl = """
            precept Sample
            field Tasks as queue of string
            state A initial
            state B
            state C
            event Add
            event Check
            from A on Add -> enqueue Tasks "alpha" -> enqueue Tasks "beta" -> transition B
            from B on Check when Tasks.peek == "alpha" -> transition C
            from B on Check -> reject "wrong peek"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Check");

        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire2.NewState.Should().Be("C");
    }

    [Fact]
    public void Guard_StackPeekProperty_ShowsTop()
    {
        const string dsl = """
            precept Sample
            field History as stack of number
            state A initial
            state B
            state C
            event Add
            event Check
            from A on Add -> push History 10 -> push History 20 -> transition B
            from B on Check when History.peek == 20 -> transition C
            from B on Check -> reject "wrong peek"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Add");
        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Check");

        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire2.NewState.Should().Be("C");
    }

    [Fact]
    public void Fire_MutationWithExpression_EvaluatesExpression()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            field Target as number default 5
            state A initial
            state B
            event Go
            from A on Go -> add Floors Target -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Go");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col = fire.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col!.Contains(5d).Should().BeTrue();
    }

    [Fact]
    public void Fire_MutationWithEventArgExpression()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            event Request with Floor as number
            from A on Request -> add Floors Request.Floor -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Request", new Dictionary<string, object?> { ["Floor"] = 7 });

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col = fire.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col!.Contains(7d).Should().BeTrue();
    }

    [Fact]
    public void Fire_NoTransition_WithMutations_AppliesMutations()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            event Go
            from A on Go -> add Floors 3 -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Go");

        fire.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fire.UpdatedInstance.Should().NotBeNull();
        var col = fire.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col!.Count.Should().Be(1);
        col.Contains(3d).Should().BeTrue();
    }

    [Fact]
    public void Fire_SetWithMixedSetAssignmentsAndMutations()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go -> add Floors 3 -> set Count = Count + 1 -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Go");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Count"].Should().Be(1d);
        var col = fire.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col!.Contains(3d).Should().BeTrue();
    }

    [Fact]
    public void Fire_ReadYourWrites_MutationsVisibleInSubsequentGuards()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            state C
            event Go
            from A on Go -> add Floors 3 -> transition B
            from B on Go when Floors.count == 1 -> add Floors 5 -> transition C
            from B on Go -> reject "wrong count"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire1 = workflow.Fire(instance, "Go");
        (fire1.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();

        var fire2 = workflow.Fire(fire1.UpdatedInstance!, "Go");
        (fire2.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire2.NewState.Should().Be("C");
        var col = fire2.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col!.Count.Should().Be(2);
    }

    [Fact]
    public void CreateInstance_CollectionsInitializedEmpty()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            field Tasks as queue of string
            field Flags as stack of boolean
            state Idle initial
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var floors = instance.InstanceData["Floors"] as List<object>;
        var tasks = instance.InstanceData["Tasks"] as List<object>;
        var flags = instance.InstanceData["Flags"] as List<object>;

        floors.Should().NotBeNull();
        floors!.Count.Should().Be(0);

        tasks.Should().NotBeNull();
        tasks!.Count.Should().Be(0);

        flags.Should().NotBeNull();
        flags!.Count.Should().Be(0);
    }

    [Fact]
    public void Fire_AddDuplicateToSet_NoDuplication()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            event Go
            from A on Go -> add Floors 3 -> add Floors 3 -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Go");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col = fire.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col!.Count.Should().Be(1);
    }

    [Fact]
    public void Fire_SetSortedOrder_MinMaxCorrect()
    {
        const string dsl = """
            precept Sample
            field Floors as set of number
            state A initial
            state B
            event Go
            from A on Go -> add Floors 7 -> add Floors 1 -> add Floors 5 -> add Floors 3 -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Go");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        var col = fire.UpdatedInstance!.InstanceData["Floors"] as List<object>;
        col!.OfType<double>().DefaultIfEmpty(double.NaN).Min().Should().Be(1d);
        col.Max().Should().Be(7d);
        col.Count.Should().Be(4);
    }

    [Fact]
    public void Fire_AtomicRollback_FailedMutationDoesNotModifyOriginal()
    {
        const string dsl = """
            precept Sample
            field Tasks as queue of string
            state A initial
            state B
            event Go
            from A on Go -> enqueue Tasks "first" -> dequeue Tasks -> dequeue Tasks -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance();

        var fire = workflow.Fire(instance, "Go");

        // Second dequeue on empty queue should reject
        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        // Original instance should be unmodified
        var col = instance.InstanceData["Tasks"] as List<object>;
        col!.Count.Should().Be(0);
    }
}
