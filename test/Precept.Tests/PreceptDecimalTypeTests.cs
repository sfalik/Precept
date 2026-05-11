using System;
using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the decimal type (Issue #27, Slice 7 — decimal portion).
/// Covers: parser acceptance, maxplaces constraint parsing, type-checker coercion,
/// round() built-in function, runtime arithmetic, and constraint enforcement.
/// </summary>
public class PreceptDecimalTypeTests
{
    // ════════════════════════════════════════════════════════════════════
    // PARSER — field declaration
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_DecimalField_WithDefaultDouble_Succeeds()
    {
        const string dsl = """
            precept M
            field Rate as decimal default 3.14
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().ContainSingle();
        var f = model.Fields[0];
        f.Name.Should().Be("Rate");
        f.Type.Should().Be(PreceptScalarType.Decimal);
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(3.14);
    }

    [Fact]
    public void Parse_DecimalField_WithDefaultInteger_Succeeds()
    {
        const string dsl = """
            precept M
            field Price as decimal default 0
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Decimal);
        f.HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void Parse_DecimalField_Nullable_Succeeds()
    {
        const string dsl = """
            precept M
            field Tax as decimal nullable
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Decimal);
        f.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Parse_DecimalField_WithMaxplacesConstraint_Succeeds()
    {
        const string dsl = """
            precept M
            field Rate as decimal default 0.0 maxplaces 2
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Decimal);
        f.Constraints.Should().ContainSingle()
            .Which.Should().BeOfType<FieldConstraint.Maxplaces>()
            .Which.Places.Should().Be(2);
    }

    [Fact]
    public void Parse_DecimalField_WithNonnegativeConstraint_Succeeds()
    {
        const string dsl = """
            precept M
            field Amount as decimal default 0.0 nonnegative
            state A initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_DecimalField_WithMinMaxConstraints_Succeeds()
    {
        const string dsl = """
            precept M
            field Discount as decimal default 0.0 min 0 max 1
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Constraints.Should().HaveCount(2);
        f.Constraints.Should().Contain(c => c is FieldConstraint.Min);
        f.Constraints.Should().Contain(c => c is FieldConstraint.Max);
    }

    [Fact]
    public void Parse_SetOfDecimal_Succeeds()
    {
        const string dsl = """
            precept M
            field Rates as set of decimal
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().ContainSingle();
        var c = model.CollectionFields[0];
        c.CollectionKind.Should().Be(PreceptCollectionKind.Set);
        c.InnerType.Should().Be(PreceptScalarType.Decimal);
    }

    // ════════════════════════════════════════════════════════════════════
    // TYPE CHECKER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeCheck_DecimalField_AssignDecimalLiteral_NoError()
    {
        const string dsl = """
            precept M
            field Rate as decimal default 0.0
            state A initial
            state B
            event Go
            from A on Go -> set Rate = 3.14 -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id.StartsWith("C6"));
    }

    [Fact]
    public void TypeCheck_DecimalField_AssignIntegerLiteral_NoError()
    {
        // integer literal widens to decimal — no diagnostic
        const string dsl = """
            precept M
            field Amount as decimal default 0.0
            state A initial
            state B
            event Go
            from A on Go -> set Amount = 5 -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id == "C60");
    }

    [Fact]
    public void TypeCheck_RoundExpression_ReturnsDecimal_NoError()
    {
        const string dsl = """
            precept M
            field Rate as decimal default 0.0
            state A initial
            state B
            event Go
            from A on Go -> set Rate = round(Rate, 2) -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id.StartsWith("C6"));
    }

    [Fact]
    public void TypeCheck_MaxplacesOnIntegerField_EmitsC61()
    {
        // maxplaces is only valid for decimal fields — integer field with maxplaces → C61
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Count", PreceptScalarType.Integer, false, true, 0L,
                [new FieldConstraint.Maxplaces(2)])],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C61");
    }

    [Fact]
    public void TypeCheck_MaxplacesOnDecimalField_NoError()
    {
        // maxplaces is valid on decimal fields — no C61
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Rate", PreceptScalarType.Decimal, false, true, 0.0,
                [new FieldConstraint.Maxplaces(2)])],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id == "C61");
    }

    [Fact]
    public void TypeCheck_NumberToDecimalAssignment_EmitsC39WithRoundGuidance()
    {
        // number → decimal is not implicit — author must use round(expr, N) or change field type.
        const string dsl = """
            precept M
            field Price as decimal default 0.0
            field Rate as number default 0
            state A initial
            state B
            event Apply
            from A on Apply -> set Price = Rate -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        var c39 = result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C39").Which;
        c39.Message.Should().Contain("round(");
        c39.Message.Should().Contain("decimal");
    }

    [Fact]
    public void TypeCheck_DecimalToNumberAssignment_EmitsC39WithUnsupportedGuidance()
    {
        // decimal → number is intentionally unsupported — diagnostic should say so and suggest alternatives.
        const string dsl = """
            precept M
            field Total as number default 0
            field Tax as decimal default 0.0
            state A initial
            state B
            event Apply
            from A on Apply -> set Total = Tax -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        var c39 = result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C39").Which;
        c39.Message.Should().Contain("intentionally unsupported");
        c39.Message.Should().Contain("floor()");
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — decimal arithmetic and round()
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_Decimal_Addition_Succeeds()
    {
        const string dsl = """
            precept M
            field A as decimal default 1.5
            field B as decimal default 2.5
            field Sum as decimal default 0.0
            state S initial
            event Add
            from S on Add -> set Sum = A + B -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Add");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        var sum = fired.UpdatedInstance!.InstanceData["Sum"];
        sum.Should().NotBeNull();
        // 1.5 + 2.5 = 4.0; runtime compares as double via TryToNumber
        ((double)Convert.ToDouble(sum)).Should().BeApproximately(4.0, 0.0001);
    }

    [Fact]
    public void Runtime_Decimal_RoundFunction_RoundsToTwoPlaces()
    {
        const string dsl = """
            precept M
            field Rate as decimal default 0.0
            state S initial
            event Apply with Amount as number
            from S on Apply -> set Rate = round(Apply.Amount, 2) -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Apply", new Dictionary<string, object?> { ["Amount"] = 3.14159 });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        var result = fired.UpdatedInstance!.InstanceData["Rate"];
        // round(3.14159, 2) = 3.14
        ((double)Convert.ToDouble(result)).Should().BeApproximately(3.14, 0.0001);
    }

    [Fact]
    public void Runtime_Decimal_RoundFunction_UsesBankersRounding_HalfEvenDown()
    {
        // 2.5 rounds to 2 (nearest even) with MidpointRounding.ToEven
        const string dsl = """
            precept M
            field R as decimal default 0.0
            state S initial
            event Calc with Val as number
            from S on Calc -> set R = round(Calc.Val, 0) -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Calc", new Dictionary<string, object?> { ["Val"] = 2.5 });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        ((double)Convert.ToDouble(fired.UpdatedInstance!.InstanceData["R"])).Should().BeApproximately(2.0, 0.0001);
    }

    [Fact]
    public void Runtime_Decimal_RoundFunction_UsesBankersRounding_HalfEvenUp()
    {
        // 3.5 rounds to 4 (nearest even) with MidpointRounding.ToEven
        const string dsl = """
            precept M
            field R as decimal default 0.0
            state S initial
            event Calc with Val as number
            from S on Calc -> set R = round(Calc.Val, 0) -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Calc", new Dictionary<string, object?> { ["Val"] = 3.5 });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        ((double)Convert.ToDouble(fired.UpdatedInstance!.InstanceData["R"])).Should().BeApproximately(4.0, 0.0001);
    }

    [Fact]
    public void Runtime_Decimal_NonnegativeConstraint_EnforcedAtRuntime()
    {
        const string dsl = """
            precept M
            field Amount as decimal default 0.0 nonnegative
            state S initial
            event Set with Val as decimal
            from S on Set -> set Amount = Set.Val -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Val"] = -1.5 });

        fired.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact]
    public void Runtime_Decimal_MaxplacesConstraint_ViolationRejected()
    {
        const string dsl = """
            precept M
            field Rate as decimal default 0.0 maxplaces 2
            state S initial
            event Set with Val as decimal
            from S on Set -> set Rate = Set.Val -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        // 3.14159 has more than 2 decimal places → should fail the maxplaces constraint;
        // maxplaces is enforced at assignment time → TransitionOutcome.Rejected
        var fired = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Val"] = 3.14159 });

        fired.Outcome.Should().Be(TransitionOutcome.Rejected);
    }

    [Fact]
    public void Runtime_Decimal_MaxplacesConstraint_ConformingValueAccepted()
    {
        const string dsl = """
            precept M
            field Rate as decimal default 0.0 maxplaces 2
            state S initial
            event Set with Val as decimal
            from S on Set -> set Rate = Set.Val -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        // 3.14 has exactly 2 decimal places → should succeed
        var fired = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Val"] = 3.14 });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }
}
