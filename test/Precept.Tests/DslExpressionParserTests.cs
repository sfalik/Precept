using System;
using FluentAssertions;
using Precept;
using Precept.Tests.Infrastructure;
using Xunit;

namespace Precept.Tests;

public class DslExpressionParserTests
{
    [Fact]
    public void Parse_SetExpression_NumberLiteral_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("42");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be(42d);
    }

    [Fact]
    public void Parse_SetExpression_StringLiteral_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("\"hello\"");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be("hello");
    }

    [Fact]
    public void Parse_SetExpression_Identifier_IsIdentifierExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("CarsWaiting");

        expression.Should().BeOfType<DslIdentifierExpression>();
        var identifier = (DslIdentifierExpression)expression;
        identifier.Name.Should().Be("CarsWaiting");
        identifier.Member.Should().BeNull();
    }

    [Fact]
    public void Parse_SetExpression_ScopedIdentifier_IsIdentifierExpressionWithMember()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("Emergency.Reason");

        expression.Should().BeOfType<DslIdentifierExpression>();
        var identifier = (DslIdentifierExpression)expression;
        identifier.Name.Should().Be("Emergency");
        identifier.Member.Should().Be("Reason");
    }

    [Fact]
    public void Parse_SetExpression_UnaryNot_IsUnaryExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("!IsEnabled");

        expression.Should().BeOfType<DslUnaryExpression>();
        var unary = (DslUnaryExpression)expression;
        unary.Operator.Should().Be("!");
        unary.Operand.Should().BeOfType<DslIdentifierExpression>();
    }

    [Fact]
    public void Parse_SetExpression_UnaryMinus_IsUnaryExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("-RetryCount");

        expression.Should().BeOfType<DslUnaryExpression>();
        var unary = (DslUnaryExpression)expression;
        unary.Operator.Should().Be("-");
        unary.Operand.Should().BeOfType<DslIdentifierExpression>();
    }

    [Fact]
    public void Parse_SetExpression_ArithmeticPrecedence_MultipliesBeforeAddition()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("A + B * 2");

        expression.Should().BeOfType<DslBinaryExpression>();
        var add = (DslBinaryExpression)expression;
        add.Operator.Should().Be("+");
        add.Left.Should().BeOfType<DslIdentifierExpression>();
        add.Right.Should().BeOfType<DslBinaryExpression>();

        var multiply = (DslBinaryExpression)add.Right;
        multiply.Operator.Should().Be("*");
    }

    [Fact]
    public void Parse_SetExpression_ParenthesesOverridePrecedence()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("(A + B) * 2");

        expression.Should().BeOfType<DslBinaryExpression>();
        var multiply = (DslBinaryExpression)expression;
        multiply.Operator.Should().Be("*");
        multiply.Left.Should().BeOfType<DslParenthesizedExpression>();

        var grouped = (DslParenthesizedExpression)multiply.Left;
        grouped.Inner.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)grouped.Inner).Operator.Should().Be("+");
    }

    [Fact]
    public void Parse_SetExpression_LogicalPrecedence_AndBeforeOr()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("A || B && C");

        expression.Should().BeOfType<DslBinaryExpression>();
        var or = (DslBinaryExpression)expression;
        or.Operator.Should().Be("||");
        or.Right.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)or.Right).Operator.Should().Be("&&");
    }

    [Fact]
    public void Parse_SetExpression_ModuloOperator_IsBinaryExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstSetExpression("RetryCount % 2");

        expression.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)expression).Operator.Should().Be("%");
    }

    [Fact]
    public void Parse_SetExpression_InvalidSingleEquals_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("A = 1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*did you mean '=='*", because: "single '=' is not valid expression syntax");
    }

    [Fact]
    public void Parse_SetExpression_MissingScopedMember_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("Emergency.");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*expected identifier after '.'*");
    }

    [Fact]
    public void Parse_SetExpression_UnterminatedString_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstSetExpressionAction("\"oops");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid set expression*unterminated string literal*");
    }

}
