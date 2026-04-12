using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Runtime tests for Form 4: conditional (guarded) edit blocks —
/// <c>in &lt;State&gt; when &lt;guard&gt; edit &lt;fields&gt;</c>.
/// Validates guard true → field editable, guard false → field not editable,
/// fail-closed semantics, union with unconditional blocks, inspect integration,
/// <c>in any when</c> expansion, state filtering, and data-driven guard flips.
/// </summary>
public class GuardedEditTests
{
    private static (PreceptEngine engine, PreceptInstance instance) CompileAndCreate(
        string dsl, string? state = null, Dictionary<string, object?>? data = null)
    {
        var def = PreceptParser.Parse(dsl);
        var engine = PreceptCompiler.Compile(def);
        var instance = state is not null
            ? engine.CreateInstance(state, data)
            : engine.CreateInstance(data);
        return (engine, instance);
    }

    // ════════════════════════════════════════════════════════════════════
    // Update: guard true / guard false
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedEdit_GuardTrue_FieldEditable()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default false
            state Open initial, Closed
            event Close
            in Open when Active edit Priority
            from Open on Close -> transition Closed
            """;

        var (engine, instance) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = true });

        var result = engine.Update(instance, p => p.Set("Priority", 5.0));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.InstanceData["Priority"].Should().Be(5.0);
    }

    [Fact]
    public void Update_GuardedEdit_GuardFalse_FieldNotEditable()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default false
            state Open initial, Closed
            event Close
            in Open when Active edit Priority
            from Open on Close -> transition Closed
            """;

        var (engine, instance) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = false });

        var result = engine.Update(instance, p => p.Set("Priority", 5.0));

        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
        result.UpdatedInstance.Should().BeNull();
    }

    [Fact]
    public void Update_GuardedEdit_NullableGuard_CompileRejects()
    {
        // PRECEPT046: edit guard must be strictly boolean, not boolean|null.
        // The type checker prevents runtime evaluation errors by rejecting nullable guards.
        const string dsl = """
            precept Test
            field Priority as number default 1
            field NullableFlag as boolean nullable
            state Open initial, Closed
            event Close
            in Open when NullableFlag edit Priority
            from Open on Close -> transition Closed
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().Throw<InvalidOperationException>().WithMessage("*PRECEPT046*");
    }

    // ════════════════════════════════════════════════════════════════════
    // Union semantics: unconditional + guarded
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_UnconditionalAndGuardedEdit_Union()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number default 0
            field Active as boolean default false
            state Open initial, Closed
            event Close
            in Open edit X
            in Open when Active edit Y
            from Open on Close -> transition Closed
            """;

        // Active=true → both X and Y editable
        var (engine, inst1) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["X"] = 0.0, ["Y"] = 0.0, ["Active"] = true });

        engine.Update(inst1, p => p.Set("X", 1.0)).Outcome.Should().Be(UpdateOutcome.Update);
        engine.Update(inst1, p => p.Set("Y", 2.0)).Outcome.Should().Be(UpdateOutcome.Update);

        // Active=false → only X editable, Y not
        var (engine2, inst2) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["X"] = 0.0, ["Y"] = 0.0, ["Active"] = false });

        engine2.Update(inst2, p => p.Set("X", 1.0)).Outcome.Should().Be(UpdateOutcome.Update);
        engine2.Update(inst2, p => p.Set("Y", 2.0)).Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    // ════════════════════════════════════════════════════════════════════
    // Inspect: editable fields list reflects guard evaluation
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Inspect_GuardedEdit_GuardTrue_FieldInEditableList()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default false
            state Open initial, Closed
            event Close
            in Open when Active edit Priority
            from Open on Close -> transition Closed
            """;

        var (engine, instance) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = true });

        var result = engine.Inspect(instance);

        result.EditableFields.Should().NotBeNull();
        result.EditableFields!.Select(f => f.FieldName).Should().Contain("Priority");
    }

    [Fact]
    public void Inspect_GuardedEdit_GuardFalse_FieldNotInEditableList()
    {
        const string dsl = """
            precept Test
            field Priority as number default 1
            field Active as boolean default false
            state Open initial, Closed
            event Close
            in Open when Active edit Priority
            from Open on Close -> transition Closed
            """;

        var (engine, instance) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["Priority"] = 1.0, ["Active"] = false });

        var result = engine.Inspect(instance);

        result.EditableFields.Should().NotBeNull();
        result.EditableFields!.Select(f => f.FieldName).Should().NotContain("Priority");
    }

    // ════════════════════════════════════════════════════════════════════
    // in any when — expansion to all states
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedEdit_InAnyWhen_ExpandsToAllStates()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state A initial, B
            event Go
            in any when Active edit X
            from A on Go -> transition B
            """;

        // State A, Active=true → X editable
        var (engine, instA) = CompileAndCreate(dsl, "A",
            new Dictionary<string, object?> { ["X"] = 0.0, ["Active"] = true });
        engine.Update(instA, p => p.Set("X", 10.0)).Outcome.Should().Be(UpdateOutcome.Update);

        // State B, Active=true → X editable
        var instB = engine.CreateInstance("B", new Dictionary<string, object?> { ["X"] = 0.0, ["Active"] = true });
        engine.Update(instB, p => p.Set("X", 20.0)).Outcome.Should().Be(UpdateOutcome.Update);

        // State A, Active=false → X not editable
        var instAFalse = engine.CreateInstance("A", new Dictionary<string, object?> { ["X"] = 0.0, ["Active"] = false });
        engine.Update(instAFalse, p => p.Set("X", 10.0)).Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    // ════════════════════════════════════════════════════════════════════
    // State filtering: wrong state → not editable even if guard true
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedEdit_StateFilter_OnlyMatchingState()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state Open initial, Closed
            event Close
            in Open when Active edit X
            from Open on Close -> transition Closed
            """;

        // Current state is Closed, Active=true → X NOT editable (guard is for Open only)
        var (engine, _) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["X"] = 0.0, ["Active"] = true });
        var closedInst = engine.CreateInstance("Closed",
            new Dictionary<string, object?> { ["X"] = 0.0, ["Active"] = true });

        var result = engine.Update(closedInst, p => p.Set("X", 5.0));
        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    // ════════════════════════════════════════════════════════════════════
    // Data-driven guard flip: guard changes via event
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedEdit_DataDrivenGuardFlip()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state Open initial
            event Activate
            in Open when Active edit X
            from Open on Activate -> set Active = true -> no transition
            """;

        // Start with Active=false → X not editable
        var (engine, instance) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["X"] = 0.0, ["Active"] = false });
        engine.Update(instance, p => p.Set("X", 10.0)).Outcome.Should().Be(UpdateOutcome.UneditableField);

        // Fire Activate → sets Active=true
        var fireResult = engine.Fire(instance, "Activate");
        fireResult.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fireResult.UpdatedInstance!.InstanceData["Active"].Should().Be(true);

        // Now X should be editable
        var updateResult = engine.Update(fireResult.UpdatedInstance!, p => p.Set("X", 10.0));
        updateResult.Outcome.Should().Be(UpdateOutcome.Update);
        updateResult.UpdatedInstance!.InstanceData["X"].Should().Be(10.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // when not: negative guard on edit blocks
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_GuardedEdit_WhenNot_NegativeGuard()
    {
        const string dsl = """
            precept Test
            field Notes as string default "initial"
            field IsLocked as boolean default false
            state Open initial, Closed
            event Close
            in Open when not IsLocked edit Notes
            from Open on Close -> transition Closed
            """;

        // IsLocked=false → guard (not IsLocked) is true → Notes editable
        var (engine, inst1) = CompileAndCreate(dsl, "Open",
            new Dictionary<string, object?> { ["Notes"] = "initial", ["IsLocked"] = false });
        var r1 = engine.Update(inst1, p => p.Set("Notes", "updated"));
        r1.Outcome.Should().Be(UpdateOutcome.Update);
        r1.UpdatedInstance!.InstanceData["Notes"].Should().Be("updated");

        // IsLocked=true → guard (not IsLocked) is false → Notes not editable
        var inst2 = engine.CreateInstance("Open",
            new Dictionary<string, object?> { ["Notes"] = "initial", ["IsLocked"] = true });
        var r2 = engine.Update(inst2, p => p.Set("Notes", "blocked"));
        r2.Outcome.Should().Be(UpdateOutcome.UneditableField);
        r2.UpdatedInstance.Should().BeNull();
    }
}
