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
    public void Parse_SetExpression_BooleanLiteral_IsLiteralExpression(string input, bool expected)
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression(input);

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be(expected);
    }

    [Fact]
    public void Parse_SetExpression_NullLiteral_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("null");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().BeNull();
    }

    [Fact]
    public void Parse_SetExpression_NumberWithExponent_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("1.5e2");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be(150d);
    }

    [Fact]
    public void Parse_SetExpression_StringWithEscapes_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("\"line1\\nline2\"");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be("line1\nline2");
    }

    [Fact]
    public void Parse_SetExpression_ComparisonChain_RespectsPrecedence()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("A > B == C");

        expression.Should().BeOfType<DslBinaryExpression>();
        var equals = (DslBinaryExpression)expression;
        equals.Operator.Should().Be("==");
        equals.Left.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)equals.Left).Operator.Should().Be(">");
    }

    [Fact]
    public void Parse_SetExpression_SingleAmpersand_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("A & B");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*unexpected character '&'*");
    }

    [Fact]
    public void Parse_SetExpression_SinglePipe_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("A | B");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*unexpected character '|'*");
    }

    [Fact]
    public void Parse_SetExpression_UnexpectedCharacter_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("A @ B");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*unexpected character '@'*");
    }

    [Fact]
    public void Parse_SetExpression_MissingRightParenthesis_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("(A + 1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*expected ')'*");
    }

    [Fact]
    public void Parse_SetExpression_UnexpectedTrailingToken_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("A + 1 extra");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*expected '<end>'*found 'extra'*");
    }
}
