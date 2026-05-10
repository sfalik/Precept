using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class Track2PhaseAParserTests
{
    private static ParsedExpression GetRuleExpression(string source)
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));
        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        return ((RuleExpressionSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.RuleExpression)).Expression;
    }

    [Fact]
    public void MinKeywordFollowedByParen_ParsesAsFunctionCall()
    {
        var expr = GetRuleExpression("""
            precept Widget
            field Amount as number
            rule min(Amount, 10) > 0 because "msg"
            """);

        var comparison = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        comparison.Left.Should().BeOfType<FunctionCallExpression>()
            .Which.FunctionName.Should().Be("min");
    }

    [Fact]
    public void CurrencyKeywordMemberAccess_ParsesAsMemberAccess()
    {
        var expr = GetRuleExpression("""
            precept Widget
            field Amount as money in 'USD'
            rule Amount.currency == 'USD' because "msg"
            """);

        var comparison = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        comparison.Left.Should().BeOfType<MemberAccessExpression>()
            .Which.MemberTokenKind.Should().Be(TokenKind.CurrencyType);
    }

    [Fact]
    public void AtKeywordMethodCall_ParsesAsMethodCall()
    {
        var expr = GetRuleExpression("""
            precept Widget
            field Items as list of integer
            rule Items.at(1) > 0 because "msg"
            """);

        var comparison = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        comparison.Left.Should().BeOfType<MethodCallExpression>()
            .Which.MemberTokenKind.Should().Be(TokenKind.At);
    }
}
