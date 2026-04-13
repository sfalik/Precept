using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public class PreceptEditTests
{
    // ─── Shared DSL for most tests ───

    private const string WorkOrderDsl = """
        precept WorkOrder

        field Description as string default ""
        field Notes as string nullable
        field Priority as number default 3
        field AssignedTo as string default ""
        field ResolutionSummary as string nullable

        invariant Priority >= 1 and Priority <= 5 because "Priority must be between 1 and 5"

        state Open initial
        state InProgress
        state Resolved
        state Closed

        in Resolved assert ResolutionSummary != null because "Resolution requires a summary"

        event Assign with Technician as string
        event StartWork
        event Resolve with Summary as string
        event Close

        from Open on Assign -> set AssignedTo = Assign.Technician -> transition InProgress
        from InProgress on Resolve -> set ResolutionSummary = Resolve.Summary -> transition Resolved
        from Resolved on Close -> transition Closed

        in any edit Notes, Priority
        in Open, InProgress edit Description
        in Open, InProgress edit AssignedTo
        in Resolved edit ResolutionSummary
        """;

    private static (PreceptEngine engine, PreceptInstance instance) CompileAndCreate(string dsl)
    {
        var def = PreceptParser.Parse(dsl);
        var engine = PreceptCompiler.Compile(def);
        var instance = engine.CreateInstance();
        return (engine, instance);
    }

    // ─── Parsing: edit blocks are parsed correctly ───

    [Fact]
    public void Parse_EditBlocks_SingleState()
    {
        const string dsl = """
            precept T
            field Notes as string nullable
            state Open initial
            in Open edit Notes
            """;

        var def = PreceptParser.Parse(dsl);
        def.EditBlocks.Should().NotBeNull();
        def.EditBlocks!.Count.Should().Be(1);
        def.EditBlocks[0].State.Should().Be("Open");
        def.EditBlocks[0].FieldNames.Should().ContainSingle().Which.Should().Be("Notes");
    }

    [Fact]
    public void Parse_EditBlocks_MultiState()
    {
        const string dsl = """
            precept T
            field Notes as string nullable
            state Open initial
            state InProgress
            in Open, InProgress edit Notes
            """;

        var def = PreceptParser.Parse(dsl);
        def.EditBlocks.Should().NotBeNull();
        def.EditBlocks!.Count.Should().Be(2);
        def.EditBlocks.Select(b => b.State).Should().Contain("Open").And.Contain("InProgress");
    }

    [Fact]
    public void Parse_EditBlocks_InAny()
    {
        const string dsl = """
            precept T
            field Notes as string nullable
            state Open initial
            state Closed
            in any edit Notes
            """;

        var def = PreceptParser.Parse(dsl);
        def.EditBlocks.Should().NotBeNull();
        // 'any' expands to one block per declared state
        def.EditBlocks!.Count.Should().Be(2);
        def.EditBlocks.Select(b => b.State).Should().Contain("Open").And.Contain("Closed");
    }

    [Fact]
    public void Parse_EditBlocks_MultipleFields()
    {
        const string dsl = """
            precept T
            field Notes as string nullable
            field Priority as number default 1
            state Open initial
            in Open edit Notes, Priority
            """;

        var def = PreceptParser.Parse(dsl);
        def.EditBlocks.Should().NotBeNull();
        def.EditBlocks!.Count.Should().Be(1);
        def.EditBlocks[0].FieldNames.Should().BeEquivalentTo(new[] { "Notes", "Priority" });
    }

    // ─── Update: basic scalar editing ───

    [Fact]
    public void Update_ScalarField_Updated()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Update(instance, p => p.Set("Notes", "hello"));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance.Should().NotBeNull();
        result.UpdatedInstance!.InstanceData["Notes"].Should().Be("hello");
        result.UpdatedInstance.CurrentState.Should().Be("Open");
    }

    [Fact]
    public void Update_MultipleScalarFields_Updated()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Update(instance, p => p
            .Set("Notes", "caller rang back")
            .Set("Priority", 1.0));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.InstanceData["Notes"].Should().Be("caller rang back");
        result.UpdatedInstance.InstanceData["Priority"].Should().Be(1.0);
    }

    [Fact]
    public void Update_NullableField_SetToNull_Updated()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // First set Notes, then clear it
        var r1 = engine.Update(instance, p => p.Set("Notes", "some text"));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        var r2 = engine.Update(r1.UpdatedInstance!, p => p.Set("Notes", null));
        r2.Outcome.Should().Be(UpdateOutcome.Update);
        r2.UpdatedInstance!.InstanceData["Notes"].Should().BeNull();
    }

    // ─── Update: editability enforcement ───

    [Fact]
    public void Update_NotEditableInCurrentState_NotAllowed()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // ResolutionSummary is only editable in Resolved, not Open
        var result = engine.Update(instance, p => p.Set("ResolutionSummary", "fixed"));

        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("ResolutionSummary").And.Contain("Open");
        result.UpdatedInstance.Should().BeNull();
    }

    [Fact]
    public void Update_EditableInOneState_NotEditableInAnother()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Description is editable in Open
        var r1 = engine.Update(instance, p => p.Set("Description", "new desc"));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        // Move to Resolved
        var r2 = engine.Fire(r1.UpdatedInstance!, "Assign", new Dictionary<string, object?> { ["Technician"] = "Alice" });
        r2.Outcome.Should().Be(TransitionOutcome.Transition);

        var r3 = engine.Fire(r2.UpdatedInstance!, "Resolve", new Dictionary<string, object?> { ["Summary"] = "done" });
        r3.Outcome.Should().Be(TransitionOutcome.Transition);

        // Description is NOT editable in Resolved
        var r4 = engine.Update(r3.UpdatedInstance!, p => p.Set("Description", "late change"));
        r4.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    [Fact]
    public void Update_InAnyEdit_EditableInAllStates()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Notes is editable in any state — test in Open
        var r1 = engine.Update(instance, p => p.Set("Notes", "open note"));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        // Move to Resolved
        var r2 = engine.Fire(r1.UpdatedInstance!, "Assign", new Dictionary<string, object?> { ["Technician"] = "Bob" });
        var r3 = engine.Fire(r2.UpdatedInstance!, "Resolve", new Dictionary<string, object?> { ["Summary"] = "done" });

        // Notes is still editable in Resolved
        var r4 = engine.Update(r3.UpdatedInstance!, p => p.Set("Notes", "resolved note"));
        r4.Outcome.Should().Be(UpdateOutcome.Update);
        r4.UpdatedInstance!.InstanceData["Notes"].Should().Be("resolved note");
    }

    [Fact]
    public void Update_NoEditBlocks_AllFieldsNotAllowed()
    {
        const string dsl = """
            precept Minimal
            field Notes as string nullable
            state Idle initial
            """;
        var (engine, instance) = CompileAndCreate(dsl);

        var result = engine.Update(instance, p => p.Set("Notes", "test"));
        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    // ─── Update: invariant enforcement ───

    [Fact]
    public void Update_InvariantViolation_Blocked()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Priority must be 1-5
        var result = engine.Update(instance, p => p.Set("Priority", 99.0));

        result.Outcome.Should().Be(UpdateOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Priority must be between 1 and 5");
        result.UpdatedInstance.Should().BeNull();
    }

    [Fact]
    public void Update_InvariantPasses_Updated()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Update(instance, p => p.Set("Priority", 5.0));
        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.InstanceData["Priority"].Should().Be(5.0);
    }

    // ─── Update: state assert enforcement ───

    [Fact]
    public void Update_StateAssertViolation_Blocked()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Move to Resolved (with summary set by event)
        var r1 = engine.Fire(instance, "Assign", new Dictionary<string, object?> { ["Technician"] = "A" });
        var r2 = engine.Fire(r1.UpdatedInstance!, "Resolve", new Dictionary<string, object?> { ["Summary"] = "done" });

        // In Resolved, ResolutionSummary is editable. Setting it to null violates the assert.
        var r3 = engine.Update(r2.UpdatedInstance!, p => p.Set("ResolutionSummary", null));
        r3.Outcome.Should().Be(UpdateOutcome.ConstraintFailure);
        r3.Violations.Should().ContainSingle().Which.Message.Should().Contain("Resolution requires a summary");
    }

    // ─── Update: type validation ───

    [Fact]
    public void Update_TypeMismatch_Invalid()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Priority is a number, not a string
        var result = engine.Update(instance, p => p.Set("Priority", "not-a-number"));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
    }

    [Fact]
    public void Update_UnknownField_Invalid()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Unknown field
        var result = engine.Update(instance, p => p.Set("Nonexistent", "value"));
        // Field is not in editableFields, so NotAllowed
        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    [Fact]
    public void Update_NullOnNonNullable_Invalid()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Priority is non-nullable number
        var result = engine.Update(instance, p => p.Set("Priority", null));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
    }

    // ─── Update: patch conflict detection ───

    [Fact]
    public void Update_DuplicateSet_Invalid()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Update(instance, p => p
            .Set("Notes", "first")
            .Set("Notes", "second"));

        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Duplicate Set");
    }

    [Fact]
    public void Update_EmptyPatch_Invalid()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Update(instance, p => { });
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("empty");
    }

    // ─── Update: collection operations ───

    private const string CollectionDsl = """
        precept CollectionTest

        field Tags as set of string
        field Queue as queue of number
        field Stack as stack of string

        state Active initial
        state Done

        in any edit Tags, Queue, Stack
        """;

    [Fact]
    public void Update_SetCollection_Add()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var result = engine.Update(instance, p => p.Add("Tags", "urgent"));
        result.Outcome.Should().Be(UpdateOutcome.Update);

        var tags = result.UpdatedInstance!.InstanceData["Tags"] as IEnumerable<object>;
        tags.Should().NotBeNull();
        tags.Should().Contain("urgent");
    }

    [Fact]
    public void Update_SetCollection_Remove()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var r1 = engine.Update(instance, p => p.Add("Tags", "urgent"));
        var r2 = engine.Update(r1.UpdatedInstance!, p => p.Remove("Tags", "urgent"));

        r2.Outcome.Should().Be(UpdateOutcome.Update);
        var tags = r2.UpdatedInstance!.InstanceData["Tags"] as IEnumerable<object>;
        tags.Should().NotContain("urgent");
    }

    [Fact]
    public void Update_QueueCollection_EnqueueDequeue()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var r1 = engine.Update(instance, p => p
            .Enqueue("Queue", 1.0)
            .Enqueue("Queue", 2.0));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        var queue = (r1.UpdatedInstance!.InstanceData["Queue"] as IEnumerable<object>)!.ToList();
        queue.Should().HaveCount(2);

        var r2 = engine.Update(r1.UpdatedInstance!, p => p.Dequeue("Queue"));
        r2.Outcome.Should().Be(UpdateOutcome.Update);

        var queue2 = (r2.UpdatedInstance!.InstanceData["Queue"] as IEnumerable<object>)!.ToList();
        queue2.Should().HaveCount(1);
    }

    [Fact]
    public void Update_StackCollection_PushPop()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var r1 = engine.Update(instance, p => p
            .Push("Stack", "a")
            .Push("Stack", "b"));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        var stack = (r1.UpdatedInstance!.InstanceData["Stack"] as IEnumerable<object>)!.ToList();
        stack.Should().HaveCount(2);

        var r2 = engine.Update(r1.UpdatedInstance!, p => p.Pop("Stack"));
        r2.Outcome.Should().Be(UpdateOutcome.Update);

        var stack2 = (r2.UpdatedInstance!.InstanceData["Stack"] as IEnumerable<object>)!.ToList();
        stack2.Should().HaveCount(1);
    }

    [Fact]
    public void Update_Collection_Replace()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var r1 = engine.Update(instance, p => p.Add("Tags", "old"));
        var r2 = engine.Update(r1.UpdatedInstance!, p => p.Replace("Tags", new object[] { "new1", "new2" }));

        r2.Outcome.Should().Be(UpdateOutcome.Update);
        var tags = (r2.UpdatedInstance!.InstanceData["Tags"] as IEnumerable<object>)!.ToList();
        tags.Should().BeEquivalentTo(new[] { "new1", "new2" });
    }

    [Fact]
    public void Update_Collection_Clear()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var r1 = engine.Update(instance, p => p
            .Add("Tags", "a")
            .Add("Tags", "b"));
        var r2 = engine.Update(r1.UpdatedInstance!, p => p.Clear("Tags"));

        r2.Outcome.Should().Be(UpdateOutcome.Update);
        var tags = (r2.UpdatedInstance!.InstanceData["Tags"] as IEnumerable<object>)!.ToList();
        tags.Should().BeEmpty();
    }

    [Fact]
    public void Update_SetOnCollectionField_Invalid()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var result = engine.Update(instance, p => p.Set("Tags", "wrong"));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Replace");
    }

    [Fact]
    public void Update_GranularOpOnScalarField_Invalid()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Update(instance, p => p.Add("Notes", "item"));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
    }

    [Fact]
    public void Update_ReplaceWithGranularOp_Invalid()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var result = engine.Update(instance, p => p
            .Replace("Tags", new object[] { "a" })
            .Add("Tags", "b"));

        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Cannot combine Replace");
    }

    [Fact]
    public void Update_DequeueEmptyQueue_Invalid()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var result = engine.Update(instance, p => p.Dequeue("Queue"));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("empty");
    }

    [Fact]
    public void Update_PopEmptyStack_Invalid()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var result = engine.Update(instance, p => p.Pop("Stack"));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("empty");
    }

    // ─── Update: atomicity ───

    [Fact]
    public void Update_AtomicRollback_OnInvariantViolation()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Set priority to valid first
        var r1 = engine.Update(instance, p => p.Set("Priority", 2.0));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        // Now try to set priority AND notes — priority is invalid, atomically rolled back
        var r2 = engine.Update(r1.UpdatedInstance!, p => p
            .Set("Notes", "should not persist")
            .Set("Priority", 99.0));
        r2.Outcome.Should().Be(UpdateOutcome.ConstraintFailure);

        // Instance should be unchanged
        r1.UpdatedInstance!.InstanceData["Priority"].Should().Be(2.0);
        r1.UpdatedInstance.InstanceData["Notes"].Should().BeNull(); // still null
    }

    // ─── Update: state does not change ───

    [Fact]
    public void Update_DoesNotChangeState()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Update(instance, p => p.Set("Notes", "data edit"));
        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.CurrentState.Should().Be("Open");
    }

    // ─── Update: coexistence with events ───

    [Fact]
    public void Update_CoexistsWithEvents()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Edit Notes via Update
        var r1 = engine.Update(instance, p => p.Set("Notes", "initial note"));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        // Fire event to transition
        var r2 = engine.Fire(r1.UpdatedInstance!, "Assign", new Dictionary<string, object?> { ["Technician"] = "Alice" });
        r2.Outcome.Should().Be(TransitionOutcome.Transition);

        // Notes survives the transition
        r2.UpdatedInstance!.InstanceData["Notes"].Should().Be("initial note");
        r2.UpdatedInstance.CurrentState.Should().Be("InProgress");

        // Can still edit Notes in new state
        var r3 = engine.Update(r2.UpdatedInstance!, p => p.Set("Notes", "updated after assign"));
        r3.Outcome.Should().Be(UpdateOutcome.Update);
        r3.UpdatedInstance!.InstanceData["Notes"].Should().Be("updated after assign");
    }

    // ─── Inspect: EditableFields in aggregate inspect ───

    [Fact]
    public void Inspect_EditableFields_PopulatedPerState()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Inspect(instance);

        result.EditableFields.Should().NotBeNull();
        var fieldNames = result.EditableFields!.Select(f => f.FieldName).ToList();

        // In Open: Notes + Priority (any) + Description + AssignedTo
        fieldNames.Should().Contain("Notes");
        fieldNames.Should().Contain("Priority");
        fieldNames.Should().Contain("Description");
        fieldNames.Should().Contain("AssignedTo");
        fieldNames.Should().NotContain("ResolutionSummary");
    }

    [Fact]
    public void Inspect_EditableFields_ChangesPerState()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Move to Resolved
        var r1 = engine.Fire(instance, "Assign", new Dictionary<string, object?> { ["Technician"] = "A" });
        var r2 = engine.Fire(r1.UpdatedInstance!, "Resolve", new Dictionary<string, object?> { ["Summary"] = "done" });

        var result = engine.Inspect(r2.UpdatedInstance!);

        result.EditableFields.Should().NotBeNull();
        var fieldNames = result.EditableFields!.Select(f => f.FieldName).ToList();

        // In Resolved: Notes + Priority (any) + ResolutionSummary
        fieldNames.Should().Contain("Notes");
        fieldNames.Should().Contain("Priority");
        fieldNames.Should().Contain("ResolutionSummary");
        fieldNames.Should().NotContain("Description");
        fieldNames.Should().NotContain("AssignedTo");
    }

    [Fact]
    public void Inspect_EditableFieldInfo_HasCorrectMetadata()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Inspect(instance);
        var notesInfo = result.EditableFields!.First(f => f.FieldName == "Notes");
        notesInfo.FieldType.Should().Be("string");
        notesInfo.IsNullable.Should().BeTrue();
        notesInfo.CurrentValue.Should().BeNull();
        notesInfo.Violation.Should().BeNull();

        var priorityInfo = result.EditableFields!.First(f => f.FieldName == "Priority");
        priorityInfo.FieldType.Should().Be("number");
        priorityInfo.IsNullable.Should().BeFalse();
        priorityInfo.CurrentValue.Should().Be(3.0);
    }

    [Fact]
    public void Inspect_EditableFieldInfo_CollectionType()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var result = engine.Inspect(instance);
        result.EditableFields.Should().NotBeNull();

        var tagsInfo = result.EditableFields!.First(f => f.FieldName == "Tags");
        tagsInfo.FieldType.Should().Be("set<string>");
        tagsInfo.IsNullable.Should().BeFalse();

        var queueInfo = result.EditableFields!.First(f => f.FieldName == "Queue");
        queueInfo.FieldType.Should().Be("queue<number>");

        var stackInfo = result.EditableFields!.First(f => f.FieldName == "Stack");
        stackInfo.FieldType.Should().Be("stack<string>");
    }

    [Fact]
    public void Inspect_NoEditBlocks_EditableFieldsNull()
    {
        const string dsl = """
            precept Minimal
            field Notes as string nullable
            state Idle initial
            """;
        var (engine, instance) = CompileAndCreate(dsl);

        var result = engine.Inspect(instance);
        result.EditableFields.Should().BeNull();
    }

    // ─── Inspect with patch: hypothetical validation ───

    [Fact]
    public void Inspect_WithPatch_ViolationReflected()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Inspect(instance, p => p.Set("Priority", 99.0));

        result.EditableFields.Should().NotBeNull();
        var priorityInfo = result.EditableFields!.First(f => f.FieldName == "Priority");
        priorityInfo.Violation.Should().NotBeNull();
        priorityInfo.Violation.Should().Contain("Priority must be between 1 and 5");
    }

    [Fact]
    public void Inspect_WithPatch_NoViolation_NoViolationSet()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        var result = engine.Inspect(instance, p => p.Set("Priority", 2.0));

        result.EditableFields.Should().NotBeNull();
        var priorityInfo = result.EditableFields!.First(f => f.FieldName == "Priority");
        priorityInfo.Violation.Should().BeNull();
    }

    [Fact]
    public void Inspect_WithPatch_InstanceUnchanged()
    {
        var (engine, instance) = CompileAndCreate(WorkOrderDsl);

        // Even though patch is valid, inspect doesn't commit
        engine.Inspect(instance, p => p.Set("Notes", "hypothetical"));

        instance.InstanceData["Notes"].Should().BeNull();
    }

    // ─── Additive semantics ───

    [Fact]
    public void EditBlocks_Additive_UnionAcrossDeclarations()
    {
        const string dsl = """
            precept T
            field A as string nullable
            field B as string nullable
            field C as string nullable
            state Open initial
            in Open edit A
            in Open edit B
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // Both A and B should be editable
        var r1 = engine.Update(instance, p => p.Set("A", "yes"));
        r1.Outcome.Should().Be(UpdateOutcome.Update);

        var r2 = engine.Update(instance, p => p.Set("B", "yes"));
        r2.Outcome.Should().Be(UpdateOutcome.Update);

        // C is not editable
        var r3 = engine.Update(instance, p => p.Set("C", "no"));
        r3.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    [Fact]
    public void EditBlocks_Additive_AnyPlusSpecific()
    {
        const string dsl = """
            precept T
            field A as string nullable
            field B as string nullable
            state Open initial
            state Closed
            in any edit A
            in Open edit B
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // In Open: A + B editable
        var inspectOpen = engine.Inspect(instance);
        inspectOpen.EditableFields!.Select(f => f.FieldName).Should().Contain("A").And.Contain("B");
    }

    // ─── GetEditableFieldNames (internal) ───

    [Fact]
    public void GetEditableFieldNames_ReturnsCorrectSet()
    {
        var (engine, _) = CompileAndCreate(WorkOrderDsl);

        var openFields = engine.GetEditableFieldNames("Open");
        openFields.Should().Contain("Notes");
        openFields.Should().Contain("Priority");
        openFields.Should().Contain("Description");
        openFields.Should().Contain("AssignedTo");
        openFields.Should().NotContain("ResolutionSummary");

        var resolvedFields = engine.GetEditableFieldNames("Resolved");
        resolvedFields.Should().Contain("Notes");
        resolvedFields.Should().Contain("Priority");
        resolvedFields.Should().Contain("ResolutionSummary");
        resolvedFields.Should().NotContain("Description");
    }

    [Fact]
    public void GetEditableFieldNames_NoEditBlocks_Empty()
    {
        const string dsl = """
            precept Minimal
            field Notes as string nullable
            state Idle initial
            """;
        var def = PreceptParser.Parse(dsl);
        var engine = PreceptCompiler.Compile(def);

        var fields = engine.GetEditableFieldNames("Idle");
        fields.Should().BeEmpty();
    }

    // ─── Collection element type validation ───

    [Fact]
    public void Update_CollectionElement_WrongType_Invalid()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        // Tags is set<string>, adding a number should fail
        var result = engine.Update(instance, p => p.Add("Tags", 42.0));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
    }

    [Fact]
    public void Update_Replace_WrongElementType_Invalid()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        // Tags is set<string> — replacing with numbers should fail
        var result = engine.Update(instance, p => p.Replace("Tags", new object[] { 1.0, 2.0 }));
        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
    }

    // ─── Multiple granular ops on same collection allowed ───

    [Fact]
    public void Update_MultipleGranularOps_SameCollection_Allowed()
    {
        var (engine, instance) = CompileAndCreate(CollectionDsl);

        var result = engine.Update(instance, p => p
            .Add("Tags", "a")
            .Add("Tags", "b")
            .Add("Tags", "c"));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        var tags = (result.UpdatedInstance!.InstanceData["Tags"] as IEnumerable<object>)!.ToList();
        tags.Should().HaveCount(3);
    }
}
