using System;
using FluentAssertions;
using Precept;
using Precept.Tests.Infrastructure;
using Xunit;

namespace Precept.Tests;

public class PreceptExpressionParserTests
{
    [Fact]
    public void Parse_SetExpression_NumberLiteral_IsLiteralExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("42");

        expression.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)expression).Value.Should().Be(42d);
    }

    [Fact]
    public void Parse_SetExpression_StringLiteral_IsLiteralExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("\"hello\"");

        expression.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)expression).Value.Should().Be("hello");
    }

    [Fact]
    public void Parse_SetExpression_Identifier_IsIdentifierExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("CarsWaiting");

        expression.Should().BeOfType<PreceptIdentifierExpression>();
        var identifier = (PreceptIdentifierExpression)expression;
        identifier.Name.Should().Be("CarsWaiting");
        identifier.Member.Should().BeNull();
    }

    [Fact]
    public void Parse_SetExpression_ScopedIdentifier_IsIdentifierExpressionWithMember()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("Emergency.Reason");

        expression.Should().BeOfType<PreceptIdentifierExpression>();
        var identifier = (PreceptIdentifierExpression)expression;
        identifier.Name.Should().Be("Emergency");
        identifier.Member.Should().Be("Reason");
    }

    [Fact]
    public void Parse_SetExpression_UnaryNot_IsUnaryExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("!IsEnabled");

        expression.Should().BeOfType<PreceptUnaryExpression>();
        var unary = (PreceptUnaryExpression)expression;
        unary.Operator.Should().Be("!");
        unary.Operand.Should().BeOfType<PreceptIdentifierExpression>();
    }

    [Fact]
    public void Parse_SetExpression_UnaryMinus_IsUnaryExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("-RetryCount");

        expression.Should().BeOfType<PreceptUnaryExpression>();
        var unary = (PreceptUnaryExpression)expression;
        unary.Operator.Should().Be("-");
        unary.Operand.Should().BeOfType<PreceptIdentifierExpression>();
    }

    [Fact]
    public void Parse_SetExpression_ArithmeticPrecedence_MultipliesBeforeAddition()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("A + B * 2");

        expression.Should().BeOfType<PreceptBinaryExpression>();
        var add = (PreceptBinaryExpression)expression;
        add.Operator.Should().Be("+");
        add.Left.Should().BeOfType<PreceptIdentifierExpression>();
        add.Right.Should().BeOfType<PreceptBinaryExpression>();

        var multiply = (PreceptBinaryExpression)add.Right;
        multiply.Operator.Should().Be("*");
    }

    [Fact]
    public void Parse_SetExpression_ParenthesesOverridePrecedence()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("(A + B) * 2");

        expression.Should().BeOfType<PreceptBinaryExpression>();
        var multiply = (PreceptBinaryExpression)expression;
        multiply.Operator.Should().Be("*");
        multiply.Left.Should().BeOfType<PreceptParenthesizedExpression>();

        var grouped = (PreceptParenthesizedExpression)multiply.Left;
        grouped.Inner.Should().BeOfType<PreceptBinaryExpression>();
        ((PreceptBinaryExpression)grouped.Inner).Operator.Should().Be("+");
    }

    [Fact]
    public void Parse_SetExpression_LogicalPrecedence_AndBeforeOr()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("A || B && C");

        expression.Should().BeOfType<PreceptBinaryExpression>();
        var or = (PreceptBinaryExpression)expression;
        or.Operator.Should().Be("||");
        or.Right.Should().BeOfType<PreceptBinaryExpression>();
        ((PreceptBinaryExpression)or.Right).Operator.Should().Be("&&");
    }

    [Fact]
    public void Parse_SetExpression_ModuloOperator_IsBinaryExpression()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression("RetryCount % 2");

        expression.Should().BeOfType<PreceptBinaryExpression>();
        ((PreceptBinaryExpression)expression).Operator.Should().Be("%");
    }

    [Fact]
    public void Parse_SetExpression_InvalidSingleEquals_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("A = 1");

        act.Should().Throw<InvalidOperationException>(because: "single '=' is not valid expression syntax");
    }

    [Fact]
    public void Parse_SetExpression_MissingScopedMember_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("Emergency.");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_SetExpression_UnterminatedString_ThrowsLineError()
    {
        var act = PreceptExpressionTestHelper.ParseFirstSetExpressionAction("\"oops");

        act.Should().Throw<InvalidOperationException>();
    }

}
