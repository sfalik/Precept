using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Parser round-trip tests for interpolated typed constant expressions.
/// Verifies that ParseInterpolatedTypedConstant produces InterpolatedTypedConstantExpression
/// with correct segment counts, segment types, and hole expression parsing.
/// </summary>
public class InterpolatedTypedConstantTests
{
    /// <summary>
    /// Parse a compute expression from a field declaration with a typed constant on the RHS.
    /// </summary>
    private static ParsedExpression GetComputeExpression(string source)
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(source));
        manifest.Diagnostics.Should().BeEmpty();
        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        return field.GetRequiredSlot<ComputeExpressionSlot>(ConstructSlotKind.ComputeExpression).Expression;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Single hole
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SingleHole_WholeValue_ProducesThreeSegments()
    {
        // '{x}' → TextSegment("") + HoleSegment(x) + TextSegment("")
        var expression = GetComputeExpression("field q as quantity <- '{x}'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(3);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        interp.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("x");
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
    }

    [Fact]
    public void SingleHole_TrailingUnit_ProducesThreeSegments()
    {
        // '{x} kg' → TextSegment("") + HoleSegment(x) + TextSegment(" kg")
        var expression = GetComputeExpression("field q as quantity <- '{x} kg'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(3);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        interp.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("x");
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" kg");
    }

    [Fact]
    public void SingleHole_LeadingNumber_ProducesThreeSegments()
    {
        // '100 {x}' → TextSegment("100 ") + HoleSegment(x) + TextSegment("")
        var expression = GetComputeExpression("field q as quantity <- '100 {x}'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(3);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().Be("100 ");
        interp.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("x");
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Two holes
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TwoHoles_SpaceSeparated_ProducesFiveSegments()
    {
        // '{x} {y}' → Text("") + Hole(x) + Text(" ") + Hole(y) + Text("")
        var expression = GetComputeExpression("field q as quantity <- '{x} {y}'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(5);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        interp.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("x");
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" ");
        interp.Segments[3].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("y");
        interp.Segments[4].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
    }

    [Fact]
    public void TwoHoles_TrailingText_ProducesFiveSegments()
    {
        // '{x} {y}/each' → Text("") + Hole(x) + Text(" ") + Hole(y) + Text("/each")
        var expression = GetComputeExpression("field q as quantity <- '{x} {y}/each'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(5);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        interp.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("x");
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" ");
        interp.Segments[3].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("y");
        interp.Segments[4].Should().BeOfType<TextSegment>().Which.Text.Should().Be("/each");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Three holes
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ThreeHoles_SlashSeparated_ProducesSevenSegments()
    {
        // '{x} {y}/{z}' → Text("") + Hole(x) + Text(" ") + Hole(y) + Text("/") + Hole(z) + Text("")
        var expression = GetComputeExpression("field q as quantity <- '{x} {y}/{z}'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(7);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        interp.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("x");
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" ");
        interp.Segments[3].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("y");
        interp.Segments[4].Should().BeOfType<TextSegment>().Which.Text.Should().Be("/");
        interp.Segments[5].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("z");
        interp.Segments[6].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Compound temporal
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CompoundTemporal_TwoHolesWithPlusSeparator_ProducesFiveSegments()
    {
        // '{n} days + {m} hours' → Text("") + Hole(n) + Text(" days + ") + Hole(m) + Text(" hours")
        var expression = GetComputeExpression("field q as quantity <- '{n} days + {m} hours'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(5);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        interp.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("n");
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" days + ");
        interp.Segments[3].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("m");
        interp.Segments[4].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" hours");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Expressions in holes
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void HoleWithBinaryExpression_ParsesCorrectly()
    {
        // '{x + 1} kg' → Text("") + Hole(x + 1) + Text(" kg")
        var expression = GetComputeExpression("field q as quantity <- '{x + 1} kg'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(3);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        var hole = interp.Segments[1].Should().BeOfType<HoleSegment>().Subject;
        hole.Expression.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.Plus);
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" kg");
    }

    [Fact]
    public void HoleWithMemberAccess_ParsesCorrectly()
    {
        // '{a.b} USD' → Text("") + Hole(a.b) + Text(" USD")
        var expression = GetComputeExpression("field q as quantity <- '{a.b} USD'");

        var interp = expression.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        interp.Segments.Should().HaveCount(3);
        interp.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        var hole = interp.Segments[1].Should().BeOfType<HoleSegment>().Subject;
        hole.Expression.Should().BeOfType<MemberAccessExpression>();
        interp.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be(" USD");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ExpressionFormKind
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void InterpolatedTypedConstant_HasCorrectFormKind()
    {
        var expression = GetComputeExpression("field q as quantity <- '{x} kg'");

        expression.Kind.Should().Be(ExpressionFormKind.InterpolatedTypedConstant);
    }
}
