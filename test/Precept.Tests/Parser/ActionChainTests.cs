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
    /// CollectionValueBy (AppendBy/EnqueueBy) shape: slot metadata carries 'by' as a required separator.
    /// BUG-048: CollectionValueBy reads 'by' from slot metadata, not hardcoded TokenKind.By.
    /// AppendBy is a secondary action (unreachable via direct dispatch); this test verifies
    /// the catalog slot structure that ParseCollectionValueByAction now reads from.
    /// </summary>
    [Fact]
    public void Append_Field_Value_By_Key_ParsesCorrectly()
    {
        var shapeMeta = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy);

        shapeMeta.Slots.Should().HaveCount(3,
            because: "CollectionValueBy has Target, Value, and OrderingKey slots");
        var orderingKeySlot = shapeMeta.Slots[2];
        orderingKeySlot.Role.Should().Be(ActionSlotRole.OrderingKey,
            because: "third slot is the ordering key introduced by 'by'");
        orderingKeySlot.PrecedingSeparator.Should().Be(TokenKind.By,
            because: "ParseCollectionValueByAction reads 'by' from slot metadata, not from a hardcoded TokenKind.By");
        orderingKeySlot.IsOptional.Should().BeFalse(
            because: "the 'by key' slot is required in CollectionValueBy");
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
    /// CollectionIntoBy (DequeueBy) shape: both 'into' and 'by' optional slots come from slot metadata.
    /// BUG-021: 'by' consumed correctly from catalog — slot-driven optional check.
    /// DequeueBy is a secondary action (unreachable via direct dispatch); this test verifies
    /// the catalog slot structure that ParseCollectionIntoByAction now reads from.
    /// </summary>
    [Fact]
    public void Dequeue_Field_Into_Target_By_Key_ParsesCorrectly()
    {
        var shapeMeta = Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy);

        shapeMeta.Slots.Should().HaveCount(3,
            because: "CollectionIntoBy has Target, IntoTarget, and OrderingCapture slots");

        var intoSlot = shapeMeta.Slots[1];
        intoSlot.Role.Should().Be(ActionSlotRole.IntoTarget);
        intoSlot.PrecedingSeparator.Should().Be(TokenKind.Into,
            because: "ParseCollectionIntoByAction reads 'into' from slot metadata, not hardcoded");
        intoSlot.IsOptional.Should().BeTrue();

        var orderingCaptureSlot = shapeMeta.Slots[2];
        orderingCaptureSlot.Role.Should().Be(ActionSlotRole.OrderingCapture);
        orderingCaptureSlot.PrecedingSeparator.Should().Be(TokenKind.By,
            because: "ParseCollectionIntoByAction reads 'by' from slot metadata, not hardcoded TokenKind.By");
        orderingCaptureSlot.IsOptional.Should().BeTrue(
            because: "both 'into' and 'by' are optional slots in CollectionIntoBy");
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
