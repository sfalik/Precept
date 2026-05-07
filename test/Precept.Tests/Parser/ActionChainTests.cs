using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class ActionChainTests
{
    private static ParsedConstruct ParseTransitionRow(string source)
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(source));
        manifest.Diagnostics.Should().BeEmpty();
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.TransitionRow);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
    }

    private static ParsedAction GetOnlyAction(string source)
    {
        var row = ParseTransitionRow(source);
        var actionSlot = row.GetRequiredSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
        actionSlot.Actions.Should().ContainSingle();
        return actionSlot.Actions[0];
    }

    [Fact]
    public void AddAction_PreservesKindTargetAndValue()
    {
        var action = GetOnlyAction("from Draft on AddFloor -> add RequestedFloors AddFloor.Floor -> transition Approved");

        var add = action.Should().BeOfType<CollectionValueAction>().Subject;
        add.Kind.Should().Be(ActionKind.Add);
        add.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("RequestedFloors");
        add.Value.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("Floor");
    }

    [Fact]
    public void RemoveAction_PreservesKindTargetAndValue()
    {
        var action = GetOnlyAction("from Draft on RemoveFloor -> remove RequestedFloors RemoveFloor.Floor -> transition Approved");

        var remove = action.Should().BeOfType<CollectionValueAction>().Subject;
        remove.Kind.Should().Be(ActionKind.Remove);
        remove.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("RequestedFloors");
        remove.Value.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("Floor");
    }

    [Fact]
    public void EnqueueAction_PreservesKindTargetAndValue()
    {
        var action = GetOnlyAction("from Draft on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> transition Approved");

        var enqueue = action.Should().BeOfType<CollectionValueAction>().Subject;
        enqueue.Kind.Should().Be(ActionKind.Enqueue);
        enqueue.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("AgentQueue");
        enqueue.Value.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("AgentName");
    }

    [Fact]
    public void DequeueAction_PreservesKindTargetAndIntoCapture()
    {
        var action = GetOnlyAction("from Draft on Promote -> dequeue HoldQueue into PromotedPatron -> transition Approved");

        var dequeue = action.Should().BeOfType<CollectionIntoAction>().Subject;
        dequeue.Kind.Should().Be(ActionKind.Dequeue);
        dequeue.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("HoldQueue");
        dequeue.IntoTarget.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("PromotedPatron");
    }

    [Fact]
    public void PushAction_PreservesKindTargetAndValue()
    {
        var action = GetOnlyAction("from Draft on LogRepairStep -> push RepairSteps LogRepairStep.StepName -> transition Approved");

        var push = action.Should().BeOfType<CollectionValueAction>().Subject;
        push.Kind.Should().Be(ActionKind.Push);
        push.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("RepairSteps");
        push.Value.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("StepName");
    }

    [Fact]
    public void PopAction_PreservesKindTargetAndIntoCapture()
    {
        var action = GetOnlyAction("from Draft on Undo -> pop RepairSteps into LastReversedStep -> transition Approved");

        var pop = action.Should().BeOfType<CollectionIntoAction>().Subject;
        pop.Kind.Should().Be(ActionKind.Pop);
        pop.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("RepairSteps");
        pop.IntoTarget.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("LastReversedStep");
    }

    [Fact]
    public void ClearAction_PreservesKindAndTarget()
    {
        var action = GetOnlyAction("from Draft on Reset -> clear RepairSteps -> transition Approved");

        var clear = action.Should().BeOfType<FieldOnlyAction>().Subject;
        clear.Kind.Should().Be(ActionKind.Clear);
        clear.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("RepairSteps");
    }
}
