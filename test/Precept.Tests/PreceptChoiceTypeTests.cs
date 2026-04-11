using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the choice type (Issue #25, Slice 7 — choice portion).
/// Covers: parser acceptance, type-checker validation (C62/C63/C64/C66),
/// runtime membership enforcement, and equality comparison.
/// </summary>
public class PreceptChoiceTypeTests
{
    // ════════════════════════════════════════════════════════════════════
    // PARSER — field declaration
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ChoiceField_BasicDeclaration_Succeeds()
    {
        const string dsl = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().ContainSingle();
        var f = model.Fields[0];
        f.Name.Should().Be("Status");
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.ChoiceValues.Should().BeEquivalentTo(["Draft", "Active", "Closed"],
            opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_ChoiceField_SingleValue_Succeeds()
    {
        const string dsl = """
            precept M
            field State as choice("Only") default "Only"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields[0].ChoiceValues.Should().ContainSingle().Which.Should().Be("Only");
    }

    [Fact]
    public void Parse_ChoiceField_WithDefault_Succeeds()
    {
        const string dsl = """
            precept M
            field Status as choice("A","B","C") default "A"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be("A");
        f.ChoiceValues.Should().BeEquivalentTo(["A", "B", "C"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_ChoiceField_WithOrdered_Succeeds()
    {
        const string dsl = """
            precept M
            field Priority as choice("Low","Medium","High") default "Low" ordered
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.IsOrdered.Should().BeTrue();
        f.ChoiceValues.Should().BeEquivalentTo(["Low", "Medium", "High"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_ChoiceField_WithOrderedAndDefault_Succeeds()
    {
        const string dsl = """
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.IsOrdered.Should().BeTrue();
        f.DefaultValue.Should().Be("Low");
    }

    [Fact]
    public void Parse_ChoiceField_Nullable_Succeeds()
    {
        const string dsl = """
            precept M
            field Category as choice("X","Y") nullable
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.IsNullable.Should().BeTrue();
        f.Type.Should().Be(PreceptScalarType.Choice);
    }

    [Fact]
    public void Parse_ChoiceEventArg_Succeeds()
    {
        const string dsl = """
            precept M
            state A initial
            event Assign with Level as choice("Low","High")
            """;

        var model = PreceptParser.Parse(dsl);

        model.Events.Should().ContainSingle();
        var arg = model.Events[0].Args[0];
        arg.Name.Should().Be("Level");
        arg.Type.Should().Be(PreceptScalarType.Choice);
        arg.ChoiceValues.Should().BeEquivalentTo(["Low", "High"], opts => opts.WithStrictOrdering());
    }

    // ════════════════════════════════════════════════════════════════════
    // TYPE CHECKER — C62 (empty set), C63 (duplicates), C64 (bad default), C66 (ordered on non-choice)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeCheck_ChoiceField_DefaultInSet_NoError()
    {
        const string dsl = """
            precept M
            field Status as choice("A","B") default "A"
            state A initial
            state B
            event Go
            from A on Go -> set Status = "B" -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id.StartsWith("C6"));
    }

    [Fact]
    public void TypeCheck_ChoiceField_DefaultNotInSet_EmitsC64()
    {
        // Default value "C" is not in the choice set {"A","B"} → C64
        const string dsl = """
            precept M
            field Status as choice("A","B") default "C"
            state A initial
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C64");
    }

    [Fact]
    public void TypeCheck_ChoiceField_DuplicateValues_EmitsC63()
    {
        // Construct model directly — duplicate values in choice set → C63
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Status", PreceptScalarType.Choice, false,
                ChoiceValues: ["A", "A", "B"])],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C63");
    }

    [Fact]
    public void TypeCheck_ChoiceField_EmptySet_EmitsC62()
    {
        // Empty choice set → C62
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Status", PreceptScalarType.Choice, false,
                ChoiceValues: [])],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C62");
    }

    [Fact]
    public void TypeCheck_OrderedOnChoiceField_NoError()
    {
        // ordered on a choice field is valid
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Priority", PreceptScalarType.Choice, false,
                ChoiceValues: ["Low", "High"], IsOrdered: true)],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id == "C66");
    }

    [Fact]
    public void TypeCheck_OrderedOnNonChoiceField_EmitsC66()
    {
        // ordered on a string field → C66
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Name", PreceptScalarType.String, false,
                IsOrdered: true)],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C66");
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — membership enforcement, equality comparison
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_Choice_AssignValidMember_Succeeds()
    {
        const string dsl = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state S initial
            event Activate with NewStatus as choice("Draft","Active","Closed")
            from S on Activate -> set Status = Activate.NewStatus -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Activate", new Dictionary<string, object?> { ["NewStatus"] = "Active" });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Status"].Should().Be("Active");
    }

    [Fact]
    public void Runtime_Choice_AssignInvalidMember_IsRejected()
    {
        // "Pending" is not in the choice set — assignment should be rejected
        const string dsl = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state S initial
            event Update with NewStatus as string
            from S on Update -> set Status = Update.NewStatus -> no transition
            """;

        // This DSL should compile (type checker doesn't flag runtime-only checking for event.arg → field mismatch in this case)
        // But when fired with an invalid value, TryValidateAssignedValue should reject it
        var result = PreceptParser.Parse(dsl);
        PreceptCompiler.Validate(result).Diagnostics.Should().NotContain(d => d.Constraint.Id == "C39");

        var engine = PreceptCompiler.Compile(result);
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Update", new Dictionary<string, object?> { ["NewStatus"] = "Pending" });

        // The assignment should fail at runtime — either Rejected or ConstraintFailure
        fired.Outcome.Should().NotBe(TransitionOutcome.NoTransition);
        fired.Outcome.Should().NotBe(TransitionOutcome.Transition);
    }

    [Fact]
    public void Runtime_Choice_DefaultValue_IsSetOnCreation()
    {
        const string dsl = """
            precept M
            field Status as choice("A","B","C") default "A"
            state S initial
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();

        inst.InstanceData["Status"].Should().Be("A");
    }

    [Fact]
    public void Runtime_Choice_EqualityComparison_Works()
    {
        const string dsl = """
            precept M
            field Status as choice("Open","Closed") default "Open"
            state S initial
            event Close
            from S on Close when Status == "Open" -> set Status = "Closed" -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Close");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Status"].Should().Be("Closed");
    }

    [Fact]
    public void Runtime_Choice_EventArg_ValidMember_Accepted()
    {
        const string dsl = """
            precept M
            state S initial
            event SetLevel with Level as choice("Low","Med","High")
            from S on SetLevel -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "SetLevel", new Dictionary<string, object?> { ["Level"] = "Med" });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    [Fact]
    public void Runtime_Choice_EventArg_InvalidMember_IsRejected()
    {
        const string dsl = """
            precept M
            state S initial
            event SetLevel with Level as choice("Low","Med","High")
            from S on SetLevel -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "SetLevel", new Dictionary<string, object?> { ["Level"] = "Critical" });

        // "Critical" is not in the choice set for the event arg → rejected
        fired.Outcome.Should().Be(TransitionOutcome.Rejected);
    }
}
