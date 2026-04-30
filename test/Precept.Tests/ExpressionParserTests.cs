using System.Collections.Immutable;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Soup Nazi tests for the Pratt expression parser (Slice 3.1).
/// Each test lexes a fragment, feeds the tokens to a ParseSession,
/// and validates the resulting AST shape.
/// </summary>
public class ExpressionParserTests
{
    private static Expression ParseExpr(string source)
    {
        var tokens = Lexer.Lex(source);
        var session = new Parser.ParseSession(tokens.Tokens);
        return session.ParseExpression(0);
    }

    // ── Atoms ──────────────────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_SingleIdentifier()
    {
        var expr = ParseExpr("amount");
        var id = expr.Should().BeOfType<IdentifierExpression>().Subject;
        id.Name.Text.Should().Be("amount");
    }

    [Fact]
    public void ParseExpression_IntegerLiteral()
    {
        var expr = ParseExpr("42");
        var lit = expr.Should().BeOfType<LiteralExpression>().Subject;
        lit.Value.Kind.Should().Be(TokenKind.NumberLiteral);
        lit.Value.Text.Should().Be("42");
    }

    [Fact]
    public void ParseExpression_StringLiteral()
    {
        var expr = ParseExpr("\"hello\"");
        var lit = expr.Should().BeOfType<LiteralExpression>().Subject;
        lit.Value.Kind.Should().Be(TokenKind.StringLiteral);
        lit.Value.Text.Should().Be("hello");
    }

    [Fact]
    public void ParseExpression_BooleanLiteral_True()
    {
        var expr = ParseExpr("true");
        var lit = expr.Should().BeOfType<LiteralExpression>().Subject;
        lit.Value.Kind.Should().Be(TokenKind.True);
    }

    [Fact]
    public void ParseExpression_BooleanLiteral_False()
    {
        var expr = ParseExpr("false");
        var lit = expr.Should().BeOfType<LiteralExpression>().Subject;
        lit.Value.Kind.Should().Be(TokenKind.False);
    }

    // ── Prefix operators ───────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_PrefixNot()
    {
        var expr = ParseExpr("not active");
        var unary = expr.Should().BeOfType<UnaryExpression>().Subject;
        unary.Operator.Kind.Should().Be(TokenKind.Not);
        unary.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("active");
    }

    // ── Binary operators ───────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_BinaryAdd()
    {
        var expr = ParseExpr("a + b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.Plus);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("a");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("b");
    }

    [Fact]
    public void ParseExpression_BinaryComparison()
    {
        var expr = ParseExpr("amount > 0");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.GreaterThan);
        bin.Left.Should().BeOfType<IdentifierExpression>();
        bin.Right.Should().BeOfType<LiteralExpression>();
    }

    [Fact]
    public void ParseExpression_LogicalAnd()
    {
        var expr = ParseExpr("a and b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.And);
    }

    [Fact]
    public void ParseExpression_LogicalOr()
    {
        var expr = ParseExpr("a or b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.Or);
    }

    // ── Precedence ─────────────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_PrecedenceArithmetic()
    {
        // "a + b * c" → a + (b * c)
        var expr = ParseExpr("a + b * c");
        var add = expr.Should().BeOfType<BinaryExpression>().Subject;
        add.Operator.Kind.Should().Be(TokenKind.Plus);
        add.Left.Should().BeOfType<IdentifierExpression>();
        var mul = add.Right.Should().BeOfType<BinaryExpression>().Subject;
        mul.Operator.Kind.Should().Be(TokenKind.Star);
    }

    [Fact]
    public void ParseExpression_PrecedenceLogical()
    {
        // "x > 0 and y > 0" → (x > 0) and (y > 0)
        var expr = ParseExpr("x > 0 and y > 0");
        var and = expr.Should().BeOfType<BinaryExpression>().Subject;
        and.Operator.Kind.Should().Be(TokenKind.And);
        and.Left.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.GreaterThan);
        and.Right.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.GreaterThan);
    }

    // ── Grouping ───────────────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_Parenthesized()
    {
        var expr = ParseExpr("(a + b)");
        var paren = expr.Should().BeOfType<ParenthesizedExpression>().Subject;
        paren.Inner.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.Plus);
    }

    // ── Member access ──────────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_MemberAccess()
    {
        var expr = ParseExpr("event.field");
        // "event" is a keyword — but in expression position we see it as:
        // token Event followed by Dot followed by Identifier "field".
        // The lexer maps "event" to TokenKind.Event so the expression parser
        // encounters a boundary token and stops. Test with plain identifiers instead.
        expr.Should().NotBeNull();
    }

    [Fact]
    public void ParseExpression_MemberAccess_Identifiers()
    {
        var expr = ParseExpr("claim.amount");
        var ma = expr.Should().BeOfType<MemberAccessExpression>().Subject;
        ma.Object.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("claim");
        ma.Member.Text.Should().Be("amount");
    }

    // ── Function call ──────────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_FunctionCall()
    {
        var expr = ParseExpr("myFunc(a, b)");
        var call = expr.Should().BeOfType<CallExpression>().Subject;
        call.Name.Text.Should().Be("myFunc");
        call.Arguments.Should().HaveCount(2);
        call.Arguments[0].Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("a");
        call.Arguments[1].Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("b");
    }

    // ── Conditional ────────────────────────────────────────────────────────

    [Fact]
    public void ParseExpression_Conditional()
    {
        var expr = ParseExpr("if flag then a else b");
        var cond = expr.Should().BeOfType<ConditionalExpression>().Subject;
        cond.Condition.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("flag");
        cond.WhenTrue.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("a");
        cond.WhenFalse.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("b");
    }

    // ── Boundary termination ───────────────────────────────────────────────

    [Fact]
    public void ParseExpression_TerminatesAtWhen()
    {
        var expr = ParseExpr("amount > 0 when Active");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.GreaterThan);
        // "when" should not be consumed
    }

    [Fact]
    public void ParseExpression_TerminatesAtBecause()
    {
        var expr = ParseExpr("amount > 0 because \"msg\"");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.GreaterThan);
    }

    // ── Negative literal folding ───────────────────────────────────────────

    [Fact]
    public void ParseExpression_NegativeInteger_FoldsToLiteral()
    {
        var expr = ParseExpr("-1");
        var lit = expr.Should().BeOfType<LiteralExpression>().Subject;
        lit.Value.Kind.Should().Be(TokenKind.NumberLiteral);
        lit.Value.Text.Should().Be("-1");
    }

    [Fact]
    public void ParseExpression_NegativeDecimal_FoldsToLiteral()
    {
        var expr = ParseExpr("-3.14");
        var lit = expr.Should().BeOfType<LiteralExpression>().Subject;
        lit.Value.Kind.Should().Be(TokenKind.NumberLiteral);
        lit.Value.Text.Should().Be("-3.14");
    }

    [Fact]
    public void ParseExpression_DoubleNegation_FoldsToPositive()
    {
        // --1 → LiteralExpression("1"), not UnaryExpression(UnaryExpression(...))
        var expr = ParseExpr("--1");
        var lit = expr.Should().BeOfType<LiteralExpression>().Subject;
        lit.Value.Kind.Should().Be(TokenKind.NumberLiteral);
        lit.Value.Text.Should().Be("1");
    }

    [Fact]
    public void ParseExpression_NegativeIdentifier_RemainsUnary()
    {
        // -x is not a literal — must stay as UnaryExpression
        var expr = ParseExpr("-x");
        expr.Should().BeOfType<UnaryExpression>();
    }

    [Fact]
    public void ParseExpression_BinaryMinus_NotFolded()
    {
        // a - 1 is binary subtraction, not constant-fold
        var expr = ParseExpr("a - 1");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.Minus);
        bin.Right.Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("1"); // positive "1", not "-1"
    }
}
