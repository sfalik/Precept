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

    // ── Slice C: shape method body slot-driven tests ──────────────────────────────

    /// <summary>
    /// InsertAt (insert) shape: 'at' separator and index expression come from slot metadata.
    /// BUG-049: insert F value at index — slot-driven parse.
    /// </summary>
    [Fact]
    public void Insert_Field_Value_At_Index_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on AddStep -> insert Steps \"Walk\" at 0 -> transition Approved");

        var insert = action.Should().BeOfType<InsertAtAction>().Subject;
        insert.Kind.Should().Be(ActionKind.Insert);
        insert.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Steps",
                because: "target field precedes slot-driven 'at' separator");
        insert.Value.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("Walk");
        insert.Index.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("0");
    }

    /// <summary>
    /// CollectionValueBy (AppendBy) should be reachable from the primary 'append' token
    /// when the catalog-driven secondary separator follows the value expression.
    /// </summary>
    [Fact]
    public void Append_Field_Value_By_Key_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on LogStep -> append Steps LogStep.Value by LogStep.Key -> transition Approved");

        var append = action.Should().BeOfType<CollectionValueByAction>().Subject;
        append.Kind.Should().Be(ActionKind.AppendBy);
        append.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("Steps");
        append.Value.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("Value");
        append.OrderingKey.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("Key");
    }

    [Fact]
    public void Enqueue_Field_Value_By_Key_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on RankTask -> enqueue Queue RankTask.Value by RankTask.Priority -> transition Approved");

        var enqueue = action.Should().BeOfType<CollectionValueByAction>().Subject;
        enqueue.Kind.Should().Be(ActionKind.EnqueueBy);
        enqueue.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("Queue");
        enqueue.Value.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("Value");
        enqueue.OrderingKey.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("Priority");
    }

    /// <summary>
    /// CollectionInto (dequeue/pop) shape: optional 'into' separator comes from slot metadata.
    /// Verifies the slot-driven optional check when 'into' is present.
    /// </summary>
    [Fact]
    public void Dequeue_Field_Into_Target_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on Promote -> dequeue Queue into Captured -> transition Approved");

        var dequeue = action.Should().BeOfType<CollectionIntoAction>().Subject;
        dequeue.Kind.Should().Be(ActionKind.Dequeue);
        dequeue.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Queue",
                because: "target field precedes optional slot-driven 'into' separator");
        dequeue.IntoTarget.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Captured",
                because: "'into' separator is consumed from slot metadata; capture field follows");
    }

    /// <summary>
    /// CollectionInto (dequeue/pop) shape: optional 'into' slot may be absent.
    /// Verifies the slot-driven optional check when 'into' is absent.
    /// </summary>
    [Fact]
    public void Dequeue_Field_Without_Into_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on Promote -> dequeue Queue -> transition Approved");

        var dequeue = action.Should().BeOfType<CollectionIntoAction>().Subject;
        dequeue.Kind.Should().Be(ActionKind.Dequeue);
        dequeue.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Queue");
        dequeue.IntoTarget.Should().BeNull(
            because: "'into' is an optional slot; omitting it leaves IntoTarget null");
    }

    /// <summary>
    /// CollectionIntoBy (DequeueBy) should remain reachable from the primary 'dequeue' token
    /// when an optional trailing separator from the secondary shape follows the shared prefix.
    /// </summary>
    [Fact]
    public void Dequeue_Field_Into_Target_By_Key_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on Promote -> dequeue Queue into Captured by Priority -> transition Approved");

        var dequeue = action.Should().BeOfType<CollectionIntoByAction>().Subject;
        dequeue.Kind.Should().Be(ActionKind.DequeueBy);
        dequeue.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("Queue");
        dequeue.IntoTarget.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("Captured");
        dequeue.OrderingCapture.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("Priority");
    }

    [Fact]
    public void Remove_Field_At_Index_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on RemoveStep -> remove Steps at RemoveStep.Index -> transition Approved");

        var remove = action.Should().BeOfType<RemoveAtAction>().Subject;
        remove.Kind.Should().Be(ActionKind.RemoveAt);
        remove.Target.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("Steps");
        remove.Index.Should().BeOfType<MemberAccessExpression>().Which.MemberName.Should().Be("Index");
    }

    /// <summary>
    /// PutKeyValue (put) shape: '=' separator between key and value comes from slot metadata.
    /// </summary>
    [Fact]
    public void Put_Field_Key_Assign_Value_ParsesCorrectly()
    {
        var action = GetOnlyAction("from Draft on TagItem -> put Tags \"priority\" = 1 -> transition Approved");

        var put = action.Should().BeOfType<PutKeyValueAction>().Subject;
        put.Kind.Should().Be(ActionKind.Put);
        put.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Tags",
                because: "target field precedes positional key and slot-driven '=' separator");
        put.Key.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("priority");
        put.Value.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("1");
    }
}
