using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class InterpolationTests
{
    private static ParsedExpression GetComputeExpression(string source)
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(source));
        manifest.Diagnostics.Should().BeEmpty();
        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        return field.GetRequiredSlot<ComputeExpressionSlot>(ConstructSlotKind.ComputeExpression).Expression;
    }

    [Fact]
    public void InterpolatedString_WithIdentifierHole_ProducesTextAndHoleSegments()
    {
        var expression = GetComputeExpression("field msg as string <- \"Hello {name}\"");

        var interpolated = expression.Should().BeOfType<InterpolatedStringExpression>().Subject;
        interpolated.Segments.Should().HaveCount(3);
        interpolated.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().Be("Hello ");
        interpolated.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("name");
        interpolated.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
    }

    [Fact]
    public void InterpolatedString_WithBinaryExpressionHole_ProducesBinaryOperationInHole()
    {
        var expression = GetComputeExpression("field msg as string <- \"Total: {amount * 0.9}\"");

        var interpolated = expression.Should().BeOfType<InterpolatedStringExpression>().Subject;
        interpolated.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().Be("Total: ");
        interpolated.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<BinaryOperationExpression>().Which.Operator.Should().Be(TokenKind.Star);
    }

    [Fact]
    public void PlainQuotedString_WithoutHoles_RemainsLiteralExpression()
    {
        var expression = GetComputeExpression("field msg as string <- \"static string\"");

        expression.Should().BeOfType<LiteralExpression>();
        ((LiteralExpression)expression).LiteralKind.Should().Be(TokenKind.StringLiteral);
        ((LiteralExpression)expression).Text.Should().Be("static string");
    }
}
