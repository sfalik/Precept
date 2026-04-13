using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for guarded root-level edit declarations —
/// <c>edit &lt;fields&gt; when &lt;guard&gt;</c> in stateless precepts.
/// Validates parsing, type checking, and compilation of the new form.
/// Runtime tests (Update, Inspect) are in the runtime slice.
/// </summary>
public class GuardedRootEditTests
{
    private static (PreceptEngine engine, PreceptInstance instance) CompileAndCreate(
        string dsl, Dictionary<string, object?>? data = null)
    {
        var def = PreceptParser.Parse(dsl);
        var engine = PreceptCompiler.Compile(def);
        var instance = engine.CreateInstance(data);
        return (engine, instance);
    }

    // ════════════════════════════════════════════════════════════════════
    // Parsing: guarded root edit parses
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_GuardedRootEdit_Succeeds()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when Active
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(1);
        var eb = model.EditBlocks![0];
        eb.State.Should().BeNull();
        eb.FieldNames.Should().Contain("Priority");
        eb.WhenText.Should().Be("Active");
        eb.WhenGuard.Should().NotBeNull();
    }

    [Fact]
    public void Parse_GuardedRootEditAll_Succeeds()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as string default ""
            field Active as boolean default true
            edit all when Active
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(1);
        var eb = model.EditBlocks![0];
        eb.State.Should().BeNull();
        eb.FieldNames.Should().Contain("all");
        eb.WhenText.Should().Be("Active");
        eb.WhenGuard.Should().NotBeNull();
    }

    [Fact]
    public void Parse_UnguardedRootEdit_StillWorks()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            edit Priority
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(1);
        var eb = model.EditBlocks![0];
        eb.State.Should().BeNull();
        eb.FieldNames.Should().Contain("Priority");
        eb.WhenText.Should().BeNull();
        eb.WhenGuard.Should().BeNull();
    }

    [Fact]
    public void Parse_GuardedRootEdit_CompoundGuard()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field A as boolean default true
            field B as boolean default true
            edit X when A and B
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(1);
        var eb = model.EditBlocks![0];
        eb.WhenText.Should().Be("A and B");
        eb.WhenGuard.Should().NotBeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // Compile: guard stored in PreceptEditBlock
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Compile_GuardedRootEdit_Succeeds()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when Active
            """;

        var def = PreceptParser.Parse(dsl);
        var engine = PreceptCompiler.Compile(def);

        engine.Should().NotBeNull();
        engine.IsStateless.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════
    // Type checker: C69 — out-of-scope guard reference
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Compile_GuardedRootEdit_C69_OutOfScopeRef()
    {
        // Guard references an identifier not in field scope
        const string dsl = """
            precept Test
            field Priority as number default 1
            edit Priority when NonExistent
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*PRECEPT038*");
    }

    [Fact]
    public void Compile_GuardedRootEdit_BooleanTypeRequired()
    {
        // Guard must evaluate to boolean — number field is rejected
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Score as number default 5
            edit Priority when Score
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*boolean*");
    }

    // ════════════════════════════════════════════════════════════════════
    // Type checker: C87 — computed field in guarded edit target
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Compile_GuardedRootEdit_C87_ComputedFieldRejected()
    {
        const string dsl = """
            precept Test
            field X as number default 1
            field Active as boolean default true
            field Doubled as number -> X * 2
            edit Doubled when Active
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d =>
            d.Code == "PRECEPT087" &&
            d.Message.Contains("Doubled"));
    }

    // ════════════════════════════════════════════════════════════════════
    // C55: guarded root edit with states = compile error
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Compile_GuardedRootEdit_C55_WithStates()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            state Open initial, Closed
            event Close
            edit Priority when Active
            from Open on Close -> transition Closed
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*PRECEPT055*");
    }

    // ════════════════════════════════════════════════════════════════════
    // Nullable guard rejected (PRECEPT046)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Compile_GuardedRootEdit_NullableGuard_Rejected()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field MaybeActive as boolean nullable
            edit Priority when MaybeActive
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*PRECEPT046*");
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime — Update: guard true / guard false
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedRootEdit_GuardTrue_FieldEditable()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = true });

        var result = engine.Update(instance, p => p.Set("Priority", 5.0));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.InstanceData["Priority"].Should().Be(5.0);
    }

    [Fact]
    public void Update_GuardedRootEdit_GuardFalse_FieldNotEditable()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = false });

        var result = engine.Update(instance, p => p.Set("Priority", 5.0));

        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
        result.UpdatedInstance.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime — Update: edit all when guard
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedRootEditAll_GuardTrue_AllFieldsEditable()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as string default ""
            field Active as boolean default true
            edit all when Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["Y"] = "", ["Active"] = true });

        engine.Update(instance, p => p.Set("X", 5.0)).Outcome.Should().Be(UpdateOutcome.Update);
        engine.Update(instance, p => p.Set("Y", "hello")).Outcome.Should().Be(UpdateOutcome.Update);
    }

    [Fact]
    public void Update_GuardedRootEditAll_GuardFalse_NoFieldsEditable()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as string default ""
            field Active as boolean default true
            edit all when Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["Y"] = "", ["Active"] = false });

        engine.Update(instance, p => p.Set("X", 5.0)).Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime — Update: additive union of unconditional + guarded
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedRootEdit_AdditiveUnion()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number default 0
            field Active as boolean default true
            edit X
            edit Y when Active
            """;

        // Active=true → both X and Y editable
        var (engine1, inst1) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["Y"] = 0.0, ["Active"] = true });

        engine1.Update(inst1, p => p.Set("X", 1.0)).Outcome.Should().Be(UpdateOutcome.Update);
        engine1.Update(inst1, p => p.Set("Y", 2.0)).Outcome.Should().Be(UpdateOutcome.Update);

        // Active=false → only X editable, Y not
        var (engine2, inst2) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["Y"] = 0.0, ["Active"] = false });

        engine2.Update(inst2, p => p.Set("X", 1.0)).Outcome.Should().Be(UpdateOutcome.Update);
        engine2.Update(inst2, p => p.Set("Y", 2.0)).Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime — Update: fail-closed
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedRootEdit_WhenNot_GuardTrue_FieldNotEditable()
    {
        // "when not Active" → Active=true means guard is false → field not editable
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when not Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = true });

        var result = engine.Update(instance, p => p.Set("Priority", 5.0));
        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    [Fact]
    public void Update_GuardedRootEdit_WhenNot_GuardFalse_FieldEditable()
    {
        // "when not Active" → Active=false means guard is true → field editable
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when not Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = false });

        var result = engine.Update(instance, p => p.Set("Priority", 5.0));
        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.InstanceData["Priority"].Should().Be(5.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime — Update: compound guard (and/or)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedRootEdit_CompoundGuard_And()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field A as boolean default true
            field B as boolean default true
            edit X when A and B
            """;

        // Both true → editable
        var (engine1, inst1) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["A"] = true, ["B"] = true });
        engine1.Update(inst1, p => p.Set("X", 10.0)).Outcome.Should().Be(UpdateOutcome.Update);

        // One false → not editable
        var (engine2, inst2) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["A"] = true, ["B"] = false });
        engine2.Update(inst2, p => p.Set("X", 10.0)).Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    [Fact]
    public void Update_GuardedRootEdit_CompoundGuard_Or()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field A as boolean default true
            field B as boolean default true
            edit X when A or B
            """;

        // Both false → not editable
        var (engine1, inst1) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["A"] = false, ["B"] = false });
        engine1.Update(inst1, p => p.Set("X", 10.0)).Outcome.Should().Be(UpdateOutcome.UneditableField);

        // One true → editable
        var (engine2, inst2) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["X"] = 0.0, ["A"] = false, ["B"] = true });
        engine2.Update(inst2, p => p.Set("X", 10.0)).Outcome.Should().Be(UpdateOutcome.Update);
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime — Inspect: editable fields reflect guard
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Inspect_GuardedRootEdit_GuardTrue_FieldInEditableList()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = true });

        var result = engine.Inspect(instance);

        result.EditableFields.Should().NotBeNull();
        result.EditableFields!.Select(f => f.FieldName).Should().Contain("Priority");
    }

    [Fact]
    public void Inspect_GuardedRootEdit_GuardFalse_FieldNotInEditableList()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Priority when Active
            """;

        var (engine, instance) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = false });

        var result = engine.Inspect(instance);

        // EditableFields should either be null/empty or not contain Priority
        if (result.EditableFields is not null)
            result.EditableFields.Select(f => f.FieldName).Should().NotContain("Priority");
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime — Inspect patch: guard flip changes editable set
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Inspect_Patch_GuardedRootEdit_FlipGuard()
    {
        // Only guarded edit — no unconditional edit for Priority
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default true
            edit Active
            edit Priority when Active
            """;

        // Active=true → Priority IS in editable set (guard passes)
        var (engineTrue, instTrue) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = true });

        var trueResult = engineTrue.Inspect(instTrue);
        trueResult.EditableFields.Should().NotBeNull();
        trueResult.EditableFields!.Select(f => f.FieldName).Should().Contain("Priority",
            "guard is true so Priority should be editable");
        trueResult.EditableFields!.Select(f => f.FieldName).Should().Contain("Active");

        // Active=false → Priority NOT in editable set (guard fails)
        var (engineFalse, instFalse) = CompileAndCreate(dsl,
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = false });

        var falseResult = engineFalse.Inspect(instFalse);
        falseResult.EditableFields.Should().NotBeNull();
        falseResult.EditableFields!.Select(f => f.FieldName).Should().Contain("Active");
        falseResult.EditableFields!.Select(f => f.FieldName).Should().NotContain("Priority",
            "guard is false so Priority should not be editable");

        // Patch: set Priority on false-guard instance — Priority is not editable, expect violation
        var patchResult = engineFalse.Inspect(instFalse, p => p.Set("Priority", 5.0));
        patchResult.EditableFields.Should().NotBeNull();
        // Priority is not in editable set, so the patch should show a violation
        var priorityField = patchResult.EditableFields!.FirstOrDefault(f => f.FieldName == "Priority");
        // Priority may not appear at all (not editable) — that's correct behavior
        if (priorityField is not null)
            priorityField.Violation.Should().NotBeNull("Priority is not editable when guard is false");
    }
}
