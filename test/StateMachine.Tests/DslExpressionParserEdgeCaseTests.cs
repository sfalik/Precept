using System;
using FluentAssertions;
using StateMachine.Dsl;
using StateMachine.Tests.Infrastructure;
using Xunit;

namespace StateMachine.Tests;

public class DslExpressionParserEdgeCaseTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Parse_TransformExpression_BooleanLiteral_IsLiteralExpression(string input, bool expected)
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression(input);

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be(expected);
    }

    [Fact]
    public void Parse_TransformExpression_NullLiteral_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("null");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().BeNull();
    }

    [Fact]
    public void Parse_TransformExpression_NumberWithExponent_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("1.5e2");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be(150d);
    }

    [Fact]
    public void Parse_TransformExpression_StringWithEscapes_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("\"line1\\nline2\"");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be("line1\nline2");
    }

    [Fact]
    public void Parse_TransformExpression_ComparisonChain_RespectsPrecedence()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("A > B == C");

        expression.Should().BeOfType<DslBinaryExpression>();
        var equals = (DslBinaryExpression)expression;
        equals.Operator.Should().Be("==");
        equals.Left.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)equals.Left).Operator.Should().Be(">");
    }

    [Fact]
    public void Parse_TransformExpression_SingleAmpersand_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("A & B");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*unexpected character '&'*");
    }

    [Fact]
    public void Parse_TransformExpression_SinglePipe_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("A | B");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*unexpected character '|'*");
    }

    [Fact]
    public void Parse_TransformExpression_UnexpectedCharacter_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("A @ B");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*unexpected character '@'*");
    }

    [Fact]
    public void Parse_TransformExpression_MissingRightParenthesis_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("(A + 1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*expected ')'*");
    }

    [Fact]
    public void Parse_TransformExpression_UnexpectedTrailingToken_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("A + 1 extra");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*expected '<end>'*found 'extra'*");
    }
}
