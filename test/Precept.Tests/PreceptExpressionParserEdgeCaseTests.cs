using System;
using FluentAssertions;
using Precept;
using Precept.Tests.Infrastructure;
using Xunit;

namespace Precept.Tests;

public class PreceptExpressionParserEdgeCaseTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Parse_SetExpression_BooleanLiteral_IsLiteralExpression(string input, bool expected)
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression(input);

        expression.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)expression).Value.Should().Be(expected);
    }

    [Fact]
    public void Parse_SetExpression_NullLiteral_IsLiteralExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("null");

        expression.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)expression).Value.Should().BeNull();
    }

    [Fact]
    public void Parse_SetExpression_NumberWithExponent_IsLiteralExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("1.5e2");

        expression.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)expression).Value.Should().Be(150d);
    }

    [Fact]
    public void Parse_SetExpression_StringWithEscapes_IsLiteralExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("\"line1\\nline2\"");

        expression.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)expression).Value.Should().Be("line1\nline2");
    }

    [Fact]
    public void Parse_SetExpression_ComparisonChain_RespectsPrecedence()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("A > B == C");

        expression.Should().BeOfType<PreceptBinaryExpression>();
        var equals = (PreceptBinaryExpression)expression;
        equals.Operator.Should().Be("==");
        equals.Left.Should().BeOfType<PreceptBinaryExpression>();
        ((PreceptBinaryExpression)equals.Left).Operator.Should().Be(">");
    }

    [Fact]
    public void Parse_SetExpression_SingleAmpersand_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("A & B");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_SetExpression_SinglePipe_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("A | B");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_SetExpression_UnexpectedCharacter_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("A @ B");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_SetExpression_MissingRightParenthesis_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("(A + 1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_SetExpression_UnexpectedTrailingToken_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("A + 1 extra");

        act.Should().Throw<InvalidOperationException>();
    }
}
