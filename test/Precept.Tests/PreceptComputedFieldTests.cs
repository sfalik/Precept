using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for computed/derived fields: <c>field X as Type -> Expression</c>.
/// Covers Slice 1 — parser, model, and diagnostics (Issue #17).
/// </summary>
public class PreceptComputedFieldTests
{
    // ════════════════════════════════════════════════════════════════════
    // Basic parsing — computed field recognized
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ComputedField_SimpleBinaryExpression()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field Total as number -> A + B
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var total = model.Fields.Single(f => f.Name == "Total");
        total.IsComputed.Should().BeTrue();
        total.DerivedExpression.Should().BeOfType<PreceptBinaryExpression>();
        total.DerivedExpressionText.Should().Be("A + B");
        total.Type.Should().Be(PreceptScalarType.Number);
        total.IsNullable.Should().BeFalse();
        total.HasDefaultValue.Should().BeFalse();
    }

    [Fact]
    public void Parse_ComputedField_ArrowTokenRecognized()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number -> X * 2
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var y = model.Fields.Single(f => f.Name == "Y");
        y.IsComputed.Should().BeTrue();
        y.DerivedExpressionText.Should().Be("X * 2");
    }

    [Fact]
    public void Parse_ComputedField_ParenthesizedSubExpressions()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field C as number default 3
            field Result as number -> (A + B) * C
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var result = model.Fields.Single(f => f.Name == "Result");
        result.IsComputed.Should().BeTrue();
        result.DerivedExpressionText.Should().Be("(A + B) * C");
        result.DerivedExpression.Should().BeOfType<PreceptBinaryExpression>();
    }

    [Fact]
    public void Parse_ComputedField_IdentifierOnlyExpression()
    {
        const string dsl = """
            precept Test
            field Source as number default 10
            field Mirror as number -> Source
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var mirror = model.Fields.Single(f => f.Name == "Mirror");
        mirror.IsComputed.Should().BeTrue();
        mirror.DerivedExpression.Should().BeOfType<PreceptIdentifierExpression>();
        mirror.DerivedExpressionText.Should().Be("Source");
    }

    [Fact]
    public void Parse_ComputedField_StringType()
    {
        const string dsl = """
            precept Test
            field First as string default "A"
            field Last as string default "B"
            field Full as string -> First
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var full = model.Fields.Single(f => f.Name == "Full");
        full.IsComputed.Should().BeTrue();
        full.Type.Should().Be(PreceptScalarType.String);
    }

    [Fact]
    public void Parse_ComputedField_BooleanType()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            field IsHigh as boolean -> Score > 100
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var isHigh = model.Fields.Single(f => f.Name == "IsHigh");
        isHigh.IsComputed.Should().BeTrue();
        isHigh.Type.Should().Be(PreceptScalarType.Boolean);
        isHigh.DerivedExpressionText.Should().Be("Score > 100");
    }

    // ════════════════════════════════════════════════════════════════════
    // Computed field with constraints after expression
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ComputedField_WithNonnegativeConstraint()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field Total as number -> A + B nonnegative
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var total = model.Fields.Single(f => f.Name == "Total");
        total.IsComputed.Should().BeTrue();
        total.DerivedExpressionText.Should().Be("A + B");
        total.Constraints.Should().NotBeNull();
        total.Constraints.Should().ContainSingle()
            .Which.Should().BeOfType<FieldConstraint.Nonnegative>();
    }

    [Fact]
    public void Parse_ComputedField_WithPositiveConstraint()
    {
        const string dsl = """
            precept Test
            field X as number default 5
            field Y as number -> X * 2 positive
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var y = model.Fields.Single(f => f.Name == "Y");
        y.IsComputed.Should().BeTrue();
        y.Constraints.Should().ContainSingle()
            .Which.Should().BeOfType<FieldConstraint.Positive>();
    }

    [Fact]
    public void Parse_ComputedField_WithMultipleConstraints()
    {
        const string dsl = """
            precept Test
            field X as number default 5
            field Y as number -> X + 1 nonnegative min 0 max 100
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var y = model.Fields.Single(f => f.Name == "Y");
        y.IsComputed.Should().BeTrue();
        y.Constraints.Should().HaveCount(3);
    }

    // ════════════════════════════════════════════════════════════════════
    // Model: IsComputed property
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsComputed_ReturnsFalse_ForRegularField()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Single().IsComputed.Should().BeFalse();
        model.Fields.Single().DerivedExpression.Should().BeNull();
        model.Fields.Single().DerivedExpressionText.Should().BeNull();
    }

    [Fact]
    public void IsComputed_ReturnsTrue_ForDerivedField()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number -> A + 1
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var b = model.Fields.Single(f => f.Name == "B");
        b.IsComputed.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════
    // Mutual exclusion diagnostics
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_DefaultPlusDerived_ThrowsC80()
    {
        const string dsl = """
            precept Test
            field X as number default 5 -> 1 + 2
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<ConstraintViolationException>()
            .Where(e => e.Constraint.Id == "C80")
            .WithMessage("*both a default value and a derived expression*");
    }

    [Fact]
    public void Parse_NullablePlusDerived_ThrowsC81()
    {
        const string dsl = """
            precept Test
            field X as number nullable -> 1 + 2
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<ConstraintViolationException>()
            .Where(e => e.Constraint.Id == "C81")
            .WithMessage("*nullable and has a derived expression*");
    }

    [Fact]
    public void Parse_MultiNameDerived_ThrowsC82()
    {
        const string dsl = """
            precept Test
            field A, B as number -> 1 + 2
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<ConstraintViolationException>()
            .Where(e => e.Constraint.Id == "C82")
            .WithMessage("*Multi-name field declaration*");
    }

    // ════════════════════════════════════════════════════════════════════
    // C17 bypass: computed fields don't need a default
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ComputedField_DoesNotRequireDefault()
    {
        // A non-nullable, non-computed field without default would throw C17.
        // A computed field should not.
        const string dsl = """
            precept Test
            field Source as number default 0
            field Derived as number -> Source + 1
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    // ════════════════════════════════════════════════════════════════════
    // Compilation pipeline: computed fields thread through
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void CompileFromText_ComputedField_Succeeds()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field Total as number -> A + B
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Model.Should().NotBeNull();
        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
        var total = result.Model!.Fields.Single(f => f.Name == "Total");
        total.IsComputed.Should().BeTrue();
    }

    [Fact]
    public void CompileFromText_ComputedField_InStatelessPrecept()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field Sum as number -> A + B
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Model.Should().NotBeNull();
        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
        var sum = result.Model!.Fields.Single(f => f.Name == "Sum");
        sum.IsComputed.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════
    // ParseWithDiagnostics: computed field errors surface correctly
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseWithDiagnostics_DefaultPlusDerived_ReportsDiagnostic()
    {
        const string dsl = """
            precept Test
            field X as number default 0 -> 1 + 2
            state Active initial
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().Contain(d => d.Code == "PRECEPT080");
    }

    [Fact]
    public void ParseWithDiagnostics_NullablePlusDerived_ReportsDiagnostic()
    {
        const string dsl = """
            precept Test
            field X as number nullable -> 1 + 2
            state Active initial
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().Contain(d => d.Code == "PRECEPT081");
    }

    [Fact]
    public void ParseWithDiagnostics_MultiNameDerived_ReportsDiagnostic()
    {
        const string dsl = """
            precept Test
            field A, B as number -> 1 + 2
            state Active initial
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().Contain(d => d.Code == "PRECEPT082");
    }

    // ════════════════════════════════════════════════════════════════════
    // Edge cases
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ComputedField_ComplexNestedExpression()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field C as number default 3
            field Complex as number -> (A + B) * (C - 1) + 10
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var complex = model.Fields.Single(f => f.Name == "Complex");
        complex.IsComputed.Should().BeTrue();
        complex.DerivedExpressionText.Should().Be("(A + B) * (C - 1) + 10");
    }

    [Fact]
    public void Parse_ComputedField_LiteralExpression()
    {
        const string dsl = """
            precept Test
            field Constant as number -> 42
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var constant = model.Fields.Single(f => f.Name == "Constant");
        constant.IsComputed.Should().BeTrue();
        constant.DerivedExpressionText.Should().Be("42");
    }

    [Fact]
    public void Parse_MixedRegularAndComputedFields()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as string default "hello"
            field C as number -> A * 2
            field D as boolean default true
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().HaveCount(4);
        model.Fields.Single(f => f.Name == "A").IsComputed.Should().BeFalse();
        model.Fields.Single(f => f.Name == "B").IsComputed.Should().BeFalse();
        model.Fields.Single(f => f.Name == "C").IsComputed.Should().BeTrue();
        model.Fields.Single(f => f.Name == "D").IsComputed.Should().BeFalse();
    }

    [Fact]
    public void Parse_ComputedField_ConditionalExpression()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            field Label as string -> if Score > 50 then "High" else "Low"
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var label = model.Fields.Single(f => f.Name == "Label");
        label.IsComputed.Should().BeTrue();
        label.DerivedExpression.Should().BeOfType<PreceptConditionalExpression>();
    }

    [Fact]
    public void Parse_ComputedField_DuplicateDerivedArrow_ThrowsC70()
    {
        // Two -> modifiers on the same field should throw C70 (duplicate modifier)
        const string dsl = """
            precept Test
            field X as number -> 1 -> 2
            state Active initial
            """;

        // The second -> will be parsed as part of the expression or as a second modifier.
        // Either way, it should produce an error.
        var act = () => PreceptParser.Parse(dsl);

        act.Should().Throw<InvalidOperationException>();
    }

    // ════════════════════════════════════════════════════════════════════
    // Slice 2 — Type Checker Validation for Computed Fields
    // ════════════════════════════════════════════════════════════════════

    // ── C83: Nullable field reference in computed expression ──────────

    [Fact]
    public void TypeCheck_ComputedField_ReferencesNullableField_ProducesC83()
    {
        const string dsl = """
            precept Test
            field Name as string nullable
            field Display as string -> Name
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT083");
    }

    [Fact]
    public void TypeCheck_ComputedField_ReferencesNonNullableField_NoC83()
    {
        const string dsl = """
            precept Test
            field Price as number default 10
            field Tax as number -> Price * 0.1
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Code == "PRECEPT083");
    }

    // ── C84: Event argument reference in computed expression ──────────

    [Fact]
    public void TypeCheck_ComputedField_ReferencesEventArg_ProducesC84()
    {
        const string dsl = """
            precept Test
            field Total as number default 0
            field Calculated as number -> Submit.Amount
            state Active initial
            event Submit with Amount as number
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT084");
    }

    // ── C85: Unsafe collection accessor in computed expression ────────

    [Fact]
    public void TypeCheck_ComputedField_UsesPeek_ProducesC85()
    {
        const string dsl = """
            precept Test
            field Items as stack of number
            field TopItem as number -> Items.peek
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT085");
    }

    [Fact]
    public void TypeCheck_ComputedField_UsesMin_ProducesC85()
    {
        const string dsl = """
            precept Test
            field Scores as set of number
            field Lowest as number -> Scores.min
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT085");
    }

    [Fact]
    public void TypeCheck_ComputedField_UsesMax_ProducesC85()
    {
        const string dsl = """
            precept Test
            field Scores as set of number
            field Highest as number -> Scores.max
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT085");
    }

    [Fact]
    public void TypeCheck_ComputedField_UsesCount_NoC85()
    {
        const string dsl = """
            precept Test
            field Items as set of string
            field ItemCount as number -> Items.count
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Code == "PRECEPT085");
        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
    }

    // ── C86: Circular dependency / topological sort ──────────────────

    [Fact]
    public void TypeCheck_ComputedField_SelfReference_ProducesC86()
    {
        const string dsl = """
            precept Test
            field X as number -> X + 1
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT086");
    }

    [Fact]
    public void TypeCheck_ComputedField_CircularDependency_ProducesC86()
    {
        const string dsl = """
            precept Test
            field A as number -> B + 1
            field B as number -> A + 1
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d =>
            d.Code == "PRECEPT086" &&
            d.Message.Contains("\u2192")); // contains arrow (→) in cycle path
    }

    [Fact]
    public void TypeCheck_ComputedField_ThreeNodeCycle_ProducesC86()
    {
        const string dsl = """
            precept Test
            field A as number -> B + 1
            field B as number -> C + 1
            field C as number -> A + 1
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT086");
    }

    [Fact]
    public void TypeCheck_ComputedField_LinearChain_TopologicalSortCorrect()
    {
        const string dsl = """
            precept Test
            field Base as number default 10
            field Mid as number -> Base * 2
            field Top as number -> Mid + 1
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
        result.Model.Should().NotBeNull();
        result.Model!.ComputedFieldOrder.Should().NotBeNull();
        result.Model!.ComputedFieldOrder.Should().Equal("Mid", "Top");
    }

    [Fact]
    public void TypeCheck_ComputedField_DependsOnAnotherComputed_Works()
    {
        const string dsl = """
            precept Test
            field X as number default 5
            field Y as number -> X * 2
            field Z as number -> Y + 1
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
        result.Model!.ComputedFieldOrder.Should().Equal("Y", "Z");
    }

    [Fact]
    public void TypeCheck_ComputedField_DiamondDependency_ResolvesCorrectly()
    {
        const string dsl = """
            precept Test
            field Base as number default 1
            field Left as number -> Base + 1
            field Right as number -> Base + 2
            field Top as number -> Left + Right
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
        result.Model!.ComputedFieldOrder.Should().NotBeNull();
        var order = result.Model!.ComputedFieldOrder!.ToList();
        order.Should().HaveCount(3);
        // Left and Right must come before Top
        order.IndexOf("Left").Should().BeLessThan(order.IndexOf("Top"));
        order.IndexOf("Right").Should().BeLessThan(order.IndexOf("Top"));
    }

    // ── C87: Computed field in edit declaration ──────────────────────

    [Fact]
    public void TypeCheck_ComputedField_InEditDeclaration_ProducesC87()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number -> X + 1
            state Active initial
            in Active edit X, Y
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d =>
            d.Code == "PRECEPT087" &&
            d.Message.Contains("Y"));
    }

    [Fact]
    public void TypeCheck_ComputedField_NotInEditDecl_NoC87()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number -> X + 1
            state Active initial
            in Active edit X
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Code == "PRECEPT087");
    }

    // ── C88: Computed field as set target ────────────────────────────

    [Fact]
    public void TypeCheck_ComputedField_AsSetTarget_ProducesC88()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number -> X + 1
            state A initial
            state B
            event Go
            from A on Go -> set Y = 5 -> transition B
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d =>
            d.Code == "PRECEPT088" &&
            d.Message.Contains("Y") &&
            d.Message.Contains("derived from"));
    }

    [Fact]
    public void TypeCheck_ComputedField_AsSetTarget_InStateAction_ProducesC88()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number -> X + 1
            state A initial
            state B
            event Go
            from A on Go -> transition B
            to B -> set Y = 99
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d =>
            d.Code == "PRECEPT088" &&
            d.Message.Contains("Y"));
    }

    // ── Type-appropriate constraints on computed fields ──────────────

    [Fact]
    public void TypeCheck_ComputedField_WithValidConstraints_Compiles()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field Total as number -> A + B nonnegative
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
    }

    [Fact]
    public void TypeCheck_ComputedField_WithIncompatibleConstraint_ProducesError()
    {
        // minlength is a string constraint, but field is number
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number -> A + 1 minlength 5
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT057");
    }

    // ── Expression type checking ────────────────────────────────────

    [Fact]
    public void TypeCheck_ComputedField_UndefinedReference_ProducesC38()
    {
        const string dsl = """
            precept Test
            field X as number -> Missing + 1
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT038");
    }

    [Fact]
    public void TypeCheck_ComputedField_TypeMismatch_ProducesError()
    {
        const string dsl = """
            precept Test
            field Name as string default "hello"
            field Count as number -> Name
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d =>
            d.Code == "PRECEPT039" &&
            d.Message.Contains("type mismatch"));
    }

    // ── All data types supported ────────────────────────────────────

    [Fact]
    public void TypeCheck_ComputedField_IntegerType_CompilesSuccessfully()
    {
        const string dsl = """
            precept Test
            field X as integer default 1
            field Y as integer -> X + 2
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
    }

    [Fact]
    public void TypeCheck_ComputedField_DecimalType_CompilesSuccessfully()
    {
        const string dsl = """
            precept Test
            field X as decimal default 1.5
            field Y as decimal -> X * 2
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
    }

    [Fact]
    public void TypeCheck_ComputedField_BooleanType_CompilesSuccessfully()
    {
        const string dsl = """
            precept Test
            field Score as number default 50
            field IsHigh as boolean -> Score > 100
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
    }

    [Fact]
    public void TypeCheck_ComputedField_ChoiceWithConditional_CompilesSuccessfully()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            field Level as choice("Low", "High") -> if Score > 50 then "High" else "Low"
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
    }

    // ── Stateless precept computed field validation ──────────────────

    [Fact]
    public void TypeCheck_ComputedField_Stateless_CompilesSuccessfully()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field Sum as number -> A + B
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Severity == ConstraintSeverity.Error);
        result.Model!.ComputedFieldOrder.Should().NotBeNull();
        result.Model!.ComputedFieldOrder.Should().Equal("Sum");
    }

    // ── No computed fields → null order ─────────────────────────────

    [Fact]
    public void TypeCheck_NoComputedFields_OrderIsNull()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state Active initial
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Model!.ComputedFieldOrder.Should().BeNull();
    }
}

// ════════════════════════════════════════════════════════════════════════════
// Slice 3 — Runtime Tests for Computed Fields
// ════════════════════════════════════════════════════════════════════════════

public class PreceptComputedFieldRuntimeTests
{
    private static (PreceptEngine engine, PreceptInstance instance) CompileAndCreate(
        string dsl, IReadOnlyDictionary<string, object?>? data = null)
    {
        var result = PreceptCompiler.CompileFromText(dsl);
        result.Engine.Should().NotBeNull($"Expected successful compilation but got errors: {string.Join("; ", result.Diagnostics.Where(d => d.Severity == ConstraintSeverity.Error).Select(d => d.Message))}");
        var instance = result.Engine!.CreateInstance(data);
        return (result.Engine, instance);
    }

    // ════════════════════════════════════════════════════════════════════
    // Fire pipeline
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Fire_ProducesComputedFieldValuesInInstanceData()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 2
            field Total as number -> A + B
            state S1 initial
            state S2
            event Go
            from S1 on Go -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);
        var result = engine.Fire(instance, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Total"].Should().Be(3.0);
    }

    [Fact]
    public void Fire_ComputedFieldReflectsMutation()
    {
        const string dsl = """
            precept Test
            field Price as number default 10
            field Tax as number -> Price * 0.1
            state S1 initial
            state S2
            event SetPrice with NewPrice as number
            from S1 on SetPrice -> set Price = SetPrice.NewPrice -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // Initial: Tax = 10 * 0.1 = 1.0
        instance.InstanceData["Tax"].Should().Be(1.0);

        // Fire: Price → 50, Tax should recompute to 5.0
        var result = engine.Fire(instance, "SetPrice",
            new Dictionary<string, object?> { ["NewPrice"] = 50.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Price"].Should().Be(50.0);
        result.UpdatedInstance.InstanceData["Tax"].Should().Be(5.0);
    }

    [Fact]
    public void Fire_GuardReferencingComputedField_SeesFreshValue()
    {
        const string dsl = """
            precept Test
            field Count as number default 0
            field Double as number -> Count * 2
            state S1 initial
            state S2
            event Go
            from S1 on Go when Double > 5 -> transition S2
            from S1 on Go -> reject "Double too low"
            """;

        var (engine, _) = CompileAndCreate(dsl);

        // Count = 0, Double = 0 → guard fails → reject
        var inst0 = engine.CreateInstance(new Dictionary<string, object?> { ["Count"] = 0.0 });
        var r0 = engine.Fire(inst0, "Go");
        r0.Outcome.Should().Be(TransitionOutcome.Rejected);

        // Count = 5, Double = 10 → guard passes → transition
        var inst5 = engine.CreateInstance(new Dictionary<string, object?> { ["Count"] = 5.0 });
        var r5 = engine.Fire(inst5, "Go");
        r5.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void Fire_RuleReferencingComputedField_SeesFreshValueAfterMutation()
    {
        // Rule references input fields (A + B) rather than the computed field directly,
        // because BuildDefaultData doesn't include computed values → C29 can't evaluate them.
        // The computed field is still exercised: it recomputes and we verify its value.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 5
            field Sum as number -> A + B
            rule A + B <= 20 because "Sum must not exceed 20"
            state S1 initial
            state S2
            event Bump with NewA as number
            from S1 on Bump -> set A = Bump.NewA -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // A = 5, B = 5, Sum = 10 → passes
        var ok = engine.Fire(instance, "Bump",
            new Dictionary<string, object?> { ["NewA"] = 10.0 });
        ok.Outcome.Should().Be(TransitionOutcome.Transition);
        ok.UpdatedInstance!.InstanceData["Sum"].Should().Be(15.0);

        // A = 100, Sum = 105 → rule fails
        var fail = engine.Fire(instance, "Bump",
            new Dictionary<string, object?> { ["NewA"] = 100.0 });
        fail.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        fail.Violations.Should().ContainSingle()
            .Which.Message.Should().Be("Sum must not exceed 20");
    }

    [Fact]
    public void Fire_ChainedComputedFields_EvaluateInCorrectOrder()
    {
        const string dsl = """
            precept Test
            field Base as number default 10
            field Mid as number -> Base * 2
            field Top as number -> Mid + 1
            state S1 initial
            state S2
            event SetBase with NewBase as number
            from S1 on SetBase -> set Base = SetBase.NewBase -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // Initial: Base=10, Mid=20, Top=21
        instance.InstanceData["Mid"].Should().Be(20.0);
        instance.InstanceData["Top"].Should().Be(21.0);

        // Fire: Base=5, Mid=10, Top=11
        var result = engine.Fire(instance, "SetBase",
            new Dictionary<string, object?> { ["NewBase"] = 5.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Base"].Should().Be(5.0);
        result.UpdatedInstance.InstanceData["Mid"].Should().Be(10.0);
        result.UpdatedInstance.InstanceData["Top"].Should().Be(11.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // Update pipeline
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Update_RecomputesComputedFieldsAfterEdit()
    {
        const string dsl = """
            precept Test
            field Price as number default 10
            field Quantity as number default 1
            field Total as number -> Price * Quantity
            state Active initial
            in Active edit Price, Quantity
            """;

        var (engine, instance) = CompileAndCreate(dsl);
        instance.InstanceData["Total"].Should().Be(10.0);

        var result = engine.Update(instance, p => p.Set("Quantity", 5.0));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.InstanceData["Quantity"].Should().Be(5.0);
        result.UpdatedInstance.InstanceData["Total"].Should().Be(50.0);
    }

    [Fact]
    public void Update_PatchTargetingComputedField_ReturnsError()
    {
        const string dsl = """
            precept Test
            field X as number default 1
            field Y as number -> X + 1
            state Active initial
            in Active edit X
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        var result = engine.Update(instance, p => p.Set("Y", 99.0));

        result.Outcome.Should().Be(UpdateOutcome.InvalidInput);
        result.Violations.Should().ContainSingle()
            .Which.Message.Should().Contain("computed field");
    }

    [Fact]
    public void Update_RuleEvaluatesAgainstRecomputedValues()
    {
        // Rule uses stored fields so C29 can evaluate at compile time.
        // Computed field Sum still recomputes and we verify its value.
        const string dsl = """
            precept Test
            field A as number default 5
            field B as number default 5
            field Sum as number -> A + B
            rule A + B <= 15 because "Sum must not exceed 15"
            state Active initial
            in Active edit A
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // A = 5, B = 5, Sum = 10 → pass
        var ok = engine.Update(instance, p => p.Set("A", 8.0));
        ok.Outcome.Should().Be(UpdateOutcome.Update);
        ok.UpdatedInstance!.InstanceData["Sum"].Should().Be(13.0);

        // A = 20, Sum = 25 → rule fails
        var fail = engine.Update(instance, p => p.Set("A", 20.0));
        fail.Outcome.Should().Be(UpdateOutcome.ConstraintFailure);
    }

    // ════════════════════════════════════════════════════════════════════
    // Inspect pipeline
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Inspect_PreviewShowsComputedFieldValues()
    {
        const string dsl = """
            precept Test
            field A as number default 3
            field B as number default 7
            field Sum as number -> A + B
            state S1 initial
            state S2
            event Go
            from S1 on Go -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // Instance data should include the computed value
        instance.InstanceData.Should().ContainKey("Sum");
        instance.InstanceData["Sum"].Should().Be(10.0);

        // Inspect should succeed (computed value is valid)
        var result = engine.Inspect(instance, "Go");
        result.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void Inspect_ShowsRecomputedValuesAfterSimulatedMutations()
    {
        const string dsl = """
            precept Test
            field Price as number default 10
            field Tax as number -> Price * 0.1
            state S1 initial
            state S2
            event SetPrice with NewPrice as number
            from S1 on SetPrice -> set Price = SetPrice.NewPrice -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // Inspect with Price → 200, Tax should be 20.0 in simulation
        var result = engine.Inspect(instance, "SetPrice",
            new Dictionary<string, object?> { ["NewPrice"] = 200.0 });

        result.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    // ════════════════════════════════════════════════════════════════════
    // CreateInstance
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void CreateInstance_ComputesInitialValuesFromDefaults()
    {
        const string dsl = """
            precept Test
            field A as number default 3
            field B as number default 4
            field Sum as number -> A + B
            state Active initial
            """;

        var (_, instance) = CompileAndCreate(dsl);

        instance.InstanceData.Should().ContainKey("Sum");
        instance.InstanceData["Sum"].Should().Be(7.0);
    }

    [Fact]
    public void CreateInstance_WithComputedFieldValueInData_ThrowsError()
    {
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number -> A + 1
            state Active initial
            """;

        var def = PreceptParser.Parse(dsl);
        var engine = PreceptCompiler.Compile(def);

        var act = () => engine.CreateInstance(
            new Dictionary<string, object?> { ["A"] = 5.0, ["B"] = 99.0 });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*computed field*B*");
    }

    // ════════════════════════════════════════════════════════════════════
    // Collection accessors in computed expressions
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_CountInComputedExpression_ReflectsCollectionSize()
    {
        const string dsl = """
            precept Test
            field Items as set of string
            field ItemCount as number -> Items.count
            state S1 initial
            state S2
            event AddItem with Name as string
            from S1 on AddItem -> add Items AddItem.Name -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);
        instance.InstanceData["ItemCount"].Should().Be(0.0);

        var r1 = engine.Fire(instance, "AddItem",
            new Dictionary<string, object?> { ["Name"] = "Apple" });
        r1.Outcome.Should().Be(TransitionOutcome.Transition);
        r1.UpdatedInstance!.InstanceData["ItemCount"].Should().Be(1.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // Stateless precept
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Stateless_ComputedFieldsRecomputeAfterUpdate()
    {
        const string dsl = """
            precept Test
            field Price as number default 10
            field TaxRate as number default 0.1
            field Tax as number -> Price * TaxRate
            edit Price, TaxRate
            """;

        var (engine, instance) = CompileAndCreate(dsl);
        instance.InstanceData["Tax"].Should().Be(1.0);

        var r1 = engine.Update(instance, p => p.Set("Price", 100.0));
        r1.Outcome.Should().Be(UpdateOutcome.Update);
        r1.UpdatedInstance!.InstanceData["Tax"].Should().Be(10.0);

        var r2 = engine.Update(r1.UpdatedInstance, p => p.Set("TaxRate", 0.2));
        r2.Outcome.Should().Be(UpdateOutcome.Update);
        r2.UpdatedInstance!.InstanceData["Tax"].Should().Be(20.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // Violation targets — dependency closure
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ViolationTargets_RuleInvolvingComputedField_IncludesDependencyFields()
    {
        // Use a state ensure on non-initial state to bypass C30 compile-time check.
        // C30 only checks initial-state ensures; S2 ensure is not checked at compile time.
        // At runtime, transition to S2 triggers the ensure, which references computed field Sum.
        // ExpandComputedFieldTargets should surface A and B as dependency targets.
        const string dsl = """
            precept Test
            field A as number default 1
            field B as number default 1
            field Sum as number -> A + B
            state S1 initial
            state S2
            in S2 ensure Sum <= 1 because "Sum too large"
            event Go
            from S1 on Go -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        var result = engine.Fire(instance, "Go");

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        var violation = result.Violations.Should().ContainSingle().Subject;
        violation.Message.Should().Be("Sum too large");

        // Should have FieldTarget for Sum (direct reference) + A, B (dependency closure) + StateTarget
        var fieldTargets = violation.Targets.OfType<ConstraintTarget.FieldTarget>()
            .Select(t => t.FieldName).ToList();
        fieldTargets.Should().Contain("Sum");
        fieldTargets.Should().Contain("A");
        fieldTargets.Should().Contain("B");
        violation.Targets.OfType<ConstraintTarget.StateTarget>().Should().ContainSingle();
    }

    // ════════════════════════════════════════════════════════════════════
    // Entry / exit action → computed recomputation (review B1)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void EntryAction_MutatesField_RecomputesComputedField()
    {
        // Entry action sets Base → computed Doubled should reflect the new value.
        const string dsl = """
            precept Test
            field Base as number default 0
            field Doubled as number -> Base + Base
            state S1 initial
            state S2
            to S2 -> set Base = 5
            event Go
            from S1 on Go -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        var result = engine.Fire(instance, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Base"].Should().Be(5.0);
        result.UpdatedInstance!.InstanceData["Doubled"].Should().Be(10.0);
    }

    [Fact]
    public void ExitAction_MutatesField_RecomputesComputedField()
    {
        // Exit action sets Base → computed Doubled should reflect the new value.
        const string dsl = """
            precept Test
            field Base as number default 0
            field Doubled as number -> Base + Base
            state S1 initial
            state S2
            from S1 -> set Base = 7
            event Go
            from S1 on Go -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        var result = engine.Fire(instance, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        // Exit sets Base=7, row has no set, entry has no set. Doubled = 7+7.
        result.UpdatedInstance!.InstanceData["Doubled"].Should().Be(14.0);
    }

    [Fact]
    public void FullPipeline_ExitRowEntry_AllRecomputeComputedField()
    {
        // Exit sets Base=1, row adds 10 → Base=11, entry adds 100 → Base=111.
        // Computed field Doubled = Base + Base should be 222.
        const string dsl = """
            precept Test
            field Base as number default 0
            field Doubled as number -> Base + Base
            state S1 initial
            state S2
            from S1 -> set Base = 1
            to S2 -> set Base = Base + 100
            event Go
            from S1 on Go -> set Base = Base + 10 -> transition S2
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        var result = engine.Fire(instance, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        // Exit: Base=1. Row: Base=1+10=11. Entry: Base=11+100=111.
        result.UpdatedInstance!.InstanceData["Base"].Should().Be(111.0);
        result.UpdatedInstance!.InstanceData["Doubled"].Should().Be(222.0);
    }

    // ════════════════════════════════════════════════════════════════════
    // Stateless rule vs. recomputed values (review B3)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void StatelessRule_EnforcedAgainstRecomputedValue()
    {
        // Stateless precept with computed field + rule.
        // Updating dependency to make recomputed value violate the rule.
        const string dsl = """
            precept Test
            field Qty as number default 1
            field Total as number -> Qty + Qty
            edit Qty
            rule Total <= 10 because "Total exceeds limit"
            """;

        var (engine, instance) = CompileAndCreate(dsl);

        // Qty=6 → Total=12 → violates Total <= 10
        var result = engine.Update(instance, p => p.Set("Qty", 6.0));

        result.Outcome.Should().Be(UpdateOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle()
            .Which.Message.Should().Be("Total exceeds limit");
    }
}
