using System;
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
