using System;
using FluentAssertions;
using StateMachine.Dsl;
using StateMachine.Tests.Infrastructure;
using Xunit;

namespace StateMachine.Tests;

public class DslExpressionParserTests
{
    [Fact]
    public void Parse_TransformExpression_NumberLiteral_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("42");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be(42d);
    }

    [Fact]
    public void Parse_TransformExpression_StringLiteral_IsLiteralExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("\"hello\"");

        expression.Should().BeOfType<DslLiteralExpression>();
        ((DslLiteralExpression)expression).Value.Should().Be("hello");
    }

    [Fact]
    public void Parse_TransformExpression_Identifier_IsIdentifierExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("CarsWaiting");

        expression.Should().BeOfType<DslIdentifierExpression>();
        var identifier = (DslIdentifierExpression)expression;
        identifier.Name.Should().Be("CarsWaiting");
        identifier.Member.Should().BeNull();
    }

    [Fact]
    public void Parse_TransformExpression_ScopedIdentifier_IsIdentifierExpressionWithMember()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("Emergency.Reason");

        expression.Should().BeOfType<DslIdentifierExpression>();
        var identifier = (DslIdentifierExpression)expression;
        identifier.Name.Should().Be("Emergency");
        identifier.Member.Should().Be("Reason");
    }

    [Fact]
    public void Parse_TransformExpression_UnaryNot_IsUnaryExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("!IsEnabled");

        expression.Should().BeOfType<DslUnaryExpression>();
        var unary = (DslUnaryExpression)expression;
        unary.Operator.Should().Be("!");
        unary.Operand.Should().BeOfType<DslIdentifierExpression>();
    }

    [Fact]
    public void Parse_TransformExpression_UnaryMinus_IsUnaryExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("-RetryCount");

        expression.Should().BeOfType<DslUnaryExpression>();
        var unary = (DslUnaryExpression)expression;
        unary.Operator.Should().Be("-");
        unary.Operand.Should().BeOfType<DslIdentifierExpression>();
    }

    [Fact]
    public void Parse_TransformExpression_ArithmeticPrecedence_MultipliesBeforeAddition()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("A + B * 2");

        expression.Should().BeOfType<DslBinaryExpression>();
        var add = (DslBinaryExpression)expression;
        add.Operator.Should().Be("+");
        add.Left.Should().BeOfType<DslIdentifierExpression>();
        add.Right.Should().BeOfType<DslBinaryExpression>();

        var multiply = (DslBinaryExpression)add.Right;
        multiply.Operator.Should().Be("*");
    }

    [Fact]
    public void Parse_TransformExpression_ParenthesesOverridePrecedence()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("(A + B) * 2");

        expression.Should().BeOfType<DslBinaryExpression>();
        var multiply = (DslBinaryExpression)expression;
        multiply.Operator.Should().Be("*");
        multiply.Left.Should().BeOfType<DslParenthesizedExpression>();

        var grouped = (DslParenthesizedExpression)multiply.Left;
        grouped.Inner.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)grouped.Inner).Operator.Should().Be("+");
    }

    [Fact]
    public void Parse_TransformExpression_LogicalPrecedence_AndBeforeOr()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("A || B && C");

        expression.Should().BeOfType<DslBinaryExpression>();
        var or = (DslBinaryExpression)expression;
        or.Operator.Should().Be("||");
        or.Right.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)or.Right).Operator.Should().Be("&&");
    }

    [Fact]
    public void Parse_TransformExpression_ModuloOperator_IsBinaryExpression()
    {
        var expression = DslExpressionTestHelper.ParseFirstTransformExpression("RetryCount % 2");

        expression.Should().BeOfType<DslBinaryExpression>();
        ((DslBinaryExpression)expression).Operator.Should().Be("%");
    }

    [Fact]
    public void Parse_TransformExpression_InvalidSingleEquals_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("A = 1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*did you mean '=='*", because: "single '=' is not valid expression syntax");
    }

    [Fact]
    public void Parse_TransformExpression_MissingScopedMember_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("Emergency.");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*expected identifier after '.'*");
    }

    [Fact]
    public void Parse_TransformExpression_UnterminatedString_ThrowsLineError()
    {
        var act = DslExpressionTestHelper.ParseFirstTransformExpressionAction("\"oops");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transform expression*unterminated string literal*");
    }

}
