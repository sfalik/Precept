using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the integer type (Issue #29, Slice 7 — integer portion).
/// Covers: parser acceptance, type-checker coercion diagnostics (C60/C61),
/// runtime arithmetic semantics, field constraints, and collection fields.
/// </summary>
public class PreceptIntegerTypeTests
{
    // ════════════════════════════════════════════════════════════════════
    // PARSER — field declaration
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_IntegerField_WithDefault_Succeeds()
    {
        const string dsl = """
            precept M
            field Count as integer default 0
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().ContainSingle();
        var f = model.Fields[0];
        f.Name.Should().Be("Count");
        f.Type.Should().Be(PreceptScalarType.Integer);
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(0L);
    }

    [Fact]
    public void Parse_IntegerLiteral_ParsesAsLong()
    {
        // Whole-number literal in DSL → long, not double
        const string dsl = """
            precept M
            field Score as integer default 5
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.DefaultValue.Should().Be(5L);
        f.DefaultValue.Should().BeOfType<long>();
    }

    [Fact]
    public void Parse_DecimalPointLiteral_ParsesAsDouble()
    {
        // A literal with a decimal point is a double, not a long
        const string dsl = """
            precept M
            field Rate as number default 5.0
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.DefaultValue.Should().Be(5.0);
        f.DefaultValue.Should().BeOfType<double>();
    }

    [Fact]
    public void Parse_IntegerField_NullableWithDefault_Succeeds()
    {
        const string dsl = """
            precept M
            field Count as integer nullable
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Integer);
        f.IsNullable.Should().BeTrue();
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Parse_IntegerField_Large_ParsesAsLong()
    {
        // Confirm that large integer literals fit in long
        const string dsl = """
            precept M
            field BigNum as integer default 1000000000
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.DefaultValue.Should().Be(1000000000L);
        f.DefaultValue.Should().BeOfType<long>();
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSER — collection fields of integer
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_SetOfInteger_Succeeds()
    {
        const string dsl = """
            precept M
            field IDs as set of integer
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().ContainSingle();
        var c = model.CollectionFields[0];
        c.Name.Should().Be("IDs");
        c.CollectionKind.Should().Be(PreceptCollectionKind.Set);
        c.InnerType.Should().Be(PreceptScalarType.Integer);
    }

    [Fact]
    public void Parse_QueueOfInteger_Succeeds()
    {
        const string dsl = """
            precept M
            field Jobs as queue of integer
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().ContainSingle();
        var c = model.CollectionFields[0];
        c.CollectionKind.Should().Be(PreceptCollectionKind.Queue);
        c.InnerType.Should().Be(PreceptScalarType.Integer);
    }

    [Fact]
    public void Parse_StackOfInteger_Succeeds()
    {
        const string dsl = """
            precept M
            field History as stack of integer
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().ContainSingle();
        var c = model.CollectionFields[0];
        c.CollectionKind.Should().Be(PreceptCollectionKind.Stack);
        c.InnerType.Should().Be(PreceptScalarType.Integer);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSER — field constraints on integer fields
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_IntegerField_NonnegativeConstraint_Accepted()
    {
        const string dsl = """
            precept M
            field Count as integer default 0 nonnegative
            state A initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_IntegerField_PositiveConstraint_Accepted()
    {
        const string dsl = """
            precept M
            field Count as integer default 1 positive
            state A initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_IntegerField_MinMaxConstraint_Accepted()
    {
        const string dsl = """
            precept M
            field Score as integer default 5 min 0 max 100
            state A initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    // ════════════════════════════════════════════════════════════════════
    // TYPE CHECKER — coercion diagnostics (C60 / C61)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeCheck_IntegerField_AssignIntegerLiteral_NoError()
    {
        // Integer literal (5, stored as long) assigned to integer field — no diagnostic.
        // Use -> transition B to keep B reachable so structural warnings (C48/C50) don't fire.
        const string dsl = """
            precept M
            field Count as integer default 0
            state A initial
            state B
            event Go
            from A on Go -> set Count = 5 -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_IntegerField_AssignNumberLiteral_EmitsC60()
    {
        // Number literal (3.0, stored as double) assigned to integer field → narrowing → C60.
        // Use -> transition B so B is reachable (no C48), allowing ContainSingle to work.
        const string dsl = """
            precept M
            field Count as integer default 0
            state A initial
            state B
            event Go
            from A on Go -> set Count = 3.0 -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C60");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT060");
        result.Diagnostics[0].Message.Should().Contain("explicit conversion");
    }

    [Fact]
    public void TypeCheck_NumberField_AssignIntegerLiteral_NoError()
    {
        // integer widens to number — no diagnostic.
        // Use -> transition B to keep B reachable (no C48) and avoid C50 on A.
        const string dsl = """
            precept M
            field Value as number default 0
            field Count as integer default 0
            state A initial
            state B
            event Go
            from A on Go -> set Value = Count -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_IntegerField_AssignIntegerVariable_NoError()
    {
        // Integer field assigned from another integer field — same kind, no diagnostic.
        // Use two states: S initial → Done, so no C48 (Done reachable) and no C50 (no rows on Done).
        const string dsl = """
            precept M
            field A as integer default 3
            field B as integer default 0
            state S initial
            state Done
            event Copy
            from S on Copy -> set B = A -> transition Done
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_IntegerField_MaxplacesConstraint_EmitsC61()
    {
        // maxplaces is not a parseable DSL keyword for integer yet;
        // construct the model directly to test the type-checker rejection.
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Count", PreceptScalarType.Integer, false, true, 0L,
                [new FieldConstraint.Maxplaces(2)])],
            [], null);

        var result = PreceptCompiler.Validate(model);

        // C61 must be present; C53 (no events) may also appear for this minimal model.
        var c61 = result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C61").Which;
        c61.DiagnosticCode.Should().Be("PRECEPT061");
        c61.Message.Should().Contain("decimal fields");
    }

    [Theory]
    [InlineData("integer", "5", false)]     // integer literal to integer field → ok
    [InlineData("number", "5", false)]      // integer literal widens to number → ok
    [InlineData("integer", "5.0", true)]    // double literal to integer field → C60
    public void TypeCheck_AssignmentCoercion_Theory(string fieldType, string literal, bool expectsC60)
    {
        // Use -> transition B to keep B reachable and avoid structural warnings (C48/C50)
        // interfering with the ContainSingle / NotContain assertions.
        var dsl = $"""
            precept M
            field X as {fieldType} default 0
            state A initial
            state B
            event Go
            from A on Go -> set X = {literal} -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        if (expectsC60)
        {
            // C60 must be present; other diagnostics (e.g. C48) must not appear because B is reachable.
            result.Diagnostics.Should().ContainSingle()
                .Which.Constraint.Id.Should().Be("C60");
        }
        else
        {
            result.Diagnostics.Should().NotContain(d => d.Constraint.Id == "C60");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — integer arithmetic
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_Integer_Addition_ProducesLong()
    {
        const string dsl = """
            precept M
            field A as integer default 3
            field B as integer default 4
            field Result as integer default 0
            state S initial
            event Add
            from S on Add -> set Result = A + B -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Add");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Result"].Should().Be(7L);
        fired.UpdatedInstance.InstanceData["Result"].Should().BeOfType<long>();
    }

    [Fact]
    public void Runtime_Integer_Subtraction_ProducesLong()
    {
        const string dsl = """
            precept M
            field A as integer default 10
            field B as integer default 3
            field Result as integer default 0
            state S initial
            event Sub
            from S on Sub -> set Result = A - B -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Sub");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Result"].Should().Be(7L);
    }

    [Fact]
    public void Runtime_Integer_Multiplication_ProducesLong()
    {
        const string dsl = """
            precept M
            field A as integer default 6
            field B as integer default 7
            field Result as integer default 0
            state S initial
            event Mul
            from S on Mul -> set Result = A * B -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Mul");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Result"].Should().Be(42L);
    }

    [Fact]
    public void Runtime_Integer_Division_TruncatesNotFloors()
    {
        // 5 / 2 = 2 (truncation toward zero), not 2.5
        const string dsl = """
            precept M
            field A as integer default 5
            field B as integer default 2
            field Quotient as integer default 0
            state S initial
            event Divide
            from S on Divide -> set Quotient = A / B -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Divide");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Quotient"].Should().Be(2L);
        fired.UpdatedInstance.InstanceData["Quotient"].Should().BeOfType<long>();
    }

    [Fact]
    public void Runtime_Integer_NegativeDivision_TruncatesNotFloors()
    {
        // -5 / 2 = -2 (truncation toward zero), not -3 (floor)
        const string dsl = """
            precept M
            field A as integer default 0
            field Quotient as integer default 0
            state S initial
            event Setup
            event Divide
            from S on Setup -> set A = 5 -> no transition
            from S on Divide -> set Quotient = A / 2 -> no transition
            """;

        // Build with negative initial value via CreateInstance
        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance("S", new Dictionary<string, object?> { ["A"] = -5L, ["Quotient"] = 0L });
        var fired = engine.Fire(inst, "Divide");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Quotient"].Should().Be(-2L);
    }

    [Fact]
    public void Runtime_Integer_Modulo_ProducesLong()
    {
        // 7 % 3 = 1
        const string dsl = """
            precept M
            field A as integer default 7
            field B as integer default 3
            field Remainder as integer default 0
            state S initial
            event Mod
            from S on Mod -> set Remainder = A % B -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Mod");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Remainder"].Should().Be(1L);
    }

    [Fact]
    public void Runtime_Integer_Equality_ComparesAsNumbers()
    {
        // 7L == 7.0d must be true (numeric-aware equality)
        const string dsl = """
            precept M
            field Count as integer default 7
            field Flag as boolean default false
            state S initial
            state Done
            event Check
            from S on Check when Count == 7 -> set Flag = true -> transition Done
            from S on Check -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Check");

        fired.Outcome.Should().Be(TransitionOutcome.Transition);
        fired.UpdatedInstance!.InstanceData["Flag"].Should().Be(true);
    }

    [Fact]
    public void Runtime_Integer_WidenedToNumber_ArithmeticSucceeds()
    {
        // An integer field used in arithmetic with a number field → widening promotion
        const string dsl = """
            precept M
            field Count as integer default 3
            field Rate as number default 1.5
            field Total as number default 0
            state S initial
            event Calc
            from S on Calc -> set Total = Count * Rate -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Calc");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        // 3L * 1.5d — long × double falls through to TryToNumber → 4.5d
        fired.UpdatedInstance!.InstanceData["Total"].Should().Be(4.5);
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — integer constraints
    // ════════════════════════════════════════════════════════════════════

    // TODO: enable when George adds Integer branch to BuildScalarConstraintExpr in PreceptParser.cs.
    // Currently nonnegative/positive/min/max constraints on integer fields are accepted by the
    // parser and type checker but the desugar to invariant is silently skipped — the constraint
    // expression builder only handles Number and String types, not Integer.
    [Fact(Skip = "Integer constraint desugaring not yet implemented — BuildScalarConstraintExpr has no Integer branch")]
    public void Runtime_IntegerField_NonnegativeConstraint_EnforcedAtRuntime()
    {
        // nonnegative desugars to `invariant Count >= 0`
        const string dsl = """
            precept M
            field Count as integer default 0 nonnegative
            state S initial
            event Set with Value as integer
            from S on Set -> set Count = Set.Value -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Value"] = -1L });

        fired.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    // TODO: enable when George adds Integer branch to BuildScalarConstraintExpr in PreceptParser.cs.
    [Fact(Skip = "Integer constraint desugaring not yet implemented — BuildScalarConstraintExpr has no Integer branch")]
    public void Runtime_IntegerField_PositiveConstraint_EnforcedAtRuntime()
    {
        // positive desugars to `invariant Count > 0`; zero is not positive
        const string dsl = """
            precept M
            field Count as integer default 1 positive
            state S initial
            event Set with Value as integer
            from S on Set -> set Count = Set.Value -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Value"] = 0L });

        fired.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    // TODO: enable when George adds Integer branch to BuildScalarConstraintExpr in PreceptParser.cs.
    [Fact(Skip = "Integer constraint desugaring not yet implemented — BuildScalarConstraintExpr has no Integer branch")]
    public void Runtime_IntegerField_MinMaxConstraint_EnforcedAtRuntime()
    {
        const string dsl = """
            precept M
            field Score as integer default 5 min 1 max 10
            state S initial
            event Set with Value as integer
            from S on Set -> set Score = Set.Value -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();

        var belowMin = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Value"] = 0L });
        belowMin.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);

        var aboveMax = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Value"] = 11L });
        aboveMax.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);

        var inRange = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Value"] = 5L });
        inRange.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — set<integer> min/max accessors
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_SetOfInteger_MinMax_ReturnsCorrectValues()
    {
        // .min and .max on set<integer> should work and return long values
        const string dsl = """
            precept M
            field IDs as set of integer
            state A initial
            state B
            state C
            event Populate
            event CheckMin
            event CheckMax
            from A on Populate -> add IDs 1 -> add IDs 7 -> add IDs 3 -> transition B
            from B on CheckMin when IDs.min == 1 -> transition C
            from B on CheckMin -> reject "wrong min"
            from B on CheckMax when IDs.max == 7 -> transition C
            from B on CheckMax -> reject "wrong max"
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();

        var populated = engine.Fire(inst, "Populate");
        populated.Outcome.Should().Be(TransitionOutcome.Transition);

        var minCheck = engine.Fire(populated.UpdatedInstance!, "CheckMin");
        minCheck.Outcome.Should().Be(TransitionOutcome.Transition,
            because: "IDs.min should equal 1 (smallest integer in the set)");

        // Reset to B for max check
        var inst2 = engine.CreateInstance("B", populated.UpdatedInstance!.InstanceData);
        var maxCheck = engine.Fire(inst2, "CheckMax");
        maxCheck.Outcome.Should().Be(TransitionOutcome.Transition,
            because: "IDs.max should equal 7 (largest integer in the set)");
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — event args of integer type
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_EventArg_Integer_AcceptsLongValue()
    {
        const string dsl = """
            precept M
            field Count as integer default 0
            state S initial
            event Set with Value as integer
            from S on Set -> set Count = Set.Value -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Value"] = 42L });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Count"].Should().Be(42L);
    }

    [Fact]
    public void Runtime_EventArg_Integer_CoercesIntToLong()
    {
        // int (32-bit) passed as event arg should be coerced to long by the runtime
        const string dsl = """
            precept M
            field Count as integer default 0
            state S initial
            event Set with Value as integer
            from S on Set -> set Count = Set.Value -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Set", new Dictionary<string, object?> { ["Value"] = 10 }); // int, not long

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Count"].Should().Be(10L);
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — integer arithmetic in guards
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_Integer_Guard_ComparisonWithLiteralRoutes()
    {
        const string dsl = """
            precept M
            field Count as integer default 5
            state A initial
            state B
            event Check
            from A on Check when Count > 3 -> transition B
            from A on Check -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Check");

        // Count is 5L, guard is Count > 3; 5L > 3 should be true
        fired.Outcome.Should().Be(TransitionOutcome.Transition);
        fired.UpdatedInstance!.CurrentState.Should().Be("B");
    }
}
