using System.Collections.Immutable;
using System.Linq;
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

    // GAP-C fix: keywords min/max are valid as member names after '.'
    [Fact]
    public void ParseExpression_MemberAccess_KeywordMin_ParsesWithoutErrors()
    {
        // Parse a minimal precept that uses .min member access (GAP-C fix)
        const string source = """
            precept amounts
            field total as decimal -> items.min
            """;
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty(
            ".min member access should parse without errors (GAP-C fix)");
    }

    [Fact]
    public void ParseExpression_MemberAccess_KeywordMax_ParsesWithoutErrors()
    {
        // Parse a minimal precept that uses .max member access (GAP-C fix)
        const string source = """
            precept amounts
            field total as decimal -> items.max
            """;
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty(
            ".max member access should parse without errors (GAP-C fix)");
    }

    [Fact]
    public void ParseExpression_FunctionCall_KeywordMin_ParsesWithoutErrors()
    {
        // Parse a minimal precept that uses min() as a function call (GAP-C fix)
        const string source = """
            precept amounts
            field total as decimal -> min(a, b)
            """;
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty(
            "min(a, b) function call should parse without errors (GAP-C fix)");
    }

    [Fact]
    public void ParseExpression_FunctionCall_KeywordMax_ParsesWithoutErrors()
    {
        // Parse a minimal precept that uses max() as a function call (GAP-C fix)
        const string source = """
            precept amounts
            field total as decimal -> max(x, y, z)
            """;
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty(
            "max(x, y, z) function call should parse without errors (GAP-C fix)");
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

    // ── Typed constant literals ────────────────────────────────────────────

    [Fact]
    public void ParseExpression_TypedConstant_Simple()
    {
        var expr = ParseExpr("'USD'");
        var tc = expr.Should().BeOfType<TypedConstantExpression>().Subject;
        tc.Value.Kind.Should().Be(TokenKind.TypedConstant);
        tc.Value.Text.Should().Be("USD");
    }

    [Fact]
    public void ParseExpression_TypedConstant_Date()
    {
        var expr = ParseExpr("'2026-04-15'");
        var tc = expr.Should().BeOfType<TypedConstantExpression>().Subject;
        tc.Value.Kind.Should().Be(TokenKind.TypedConstant);
        tc.Value.Text.Should().Be("2026-04-15");
    }

    [Fact]
    public void ParseExpression_TypedConstant_Interpolated()
    {
        var expr = ParseExpr("'Hello {name}'");
        var itc = expr.Should().BeOfType<InterpolatedTypedConstantExpression>().Subject;
        itc.Parts.Should().HaveCountGreaterThan(1);
        itc.Parts.Should().ContainItemsAssignableTo<InterpolationPart>();
        itc.Parts.OfType<ExpressionInterpolationPart>().Should().HaveCount(1);
    }

    [Fact]
    public void ParseExpression_TypedConstant_InFieldDefault()
    {
        var tokens = Lexer.Lex("""field Amt as money default 'USD'""");
        var tree = Parser.Parse(tokens);
        tree.Diagnostics.Should().BeEmpty("typed constant in default should parse without errors");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var modifier = field.Modifiers[0].Should().BeOfType<ValueModifierNode>().Subject;
        modifier.Value.Should().BeOfType<TypedConstantExpression>();
    }

    // ── Comparison operators (Slice 8) ─────────────────────────────────────

    [Fact]
    public void ParseExpression_ComparisonLessThan()
    {
        var expr = ParseExpr("a < b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.LessThan);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("a");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("b");
    }

    [Fact]
    public void ParseExpression_ComparisonLessThanOrEqual()
    {
        var expr = ParseExpr("a <= b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.LessThanOrEqual);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("a");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("b");
    }

    [Fact]
    public void ParseExpression_ComparisonGreaterThanOrEqual()
    {
        var expr = ParseExpr("a >= b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.GreaterThanOrEqual);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("a");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("b");
    }

    [Fact]
    public void ParseExpression_ComparisonEquals()
    {
        var expr = ParseExpr("a == b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.DoubleEquals);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("a");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("b");
    }

    [Fact]
    public void ParseExpression_ComparisonNotEquals()
    {
        var expr = ParseExpr("a != b");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.NotEquals);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("a");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("b");
    }

    [Fact]
    public void ParseExpression_ComparisonNonAssociative_EmitsDiagnostic()
    {
        // a < b < c — the second < chains a NonAssociative operator; parser emits diagnostic and stops
        var tree = Parser.Parse(Lexer.Lex("""rule a < b < c because "msg" """));
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.NonAssociativeComparison));
    }

    // ── contains operator (Slice 9) ────────────────────────────────────────

    [Fact]
    public void ParseExpression_Contains()
    {
        var expr = ParseExpr("tags contains \"urgent\"");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Operator.Kind.Should().Be(TokenKind.Contains);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("tags");
        bin.Right.Should().BeOfType<LiteralExpression>()
            .Which.Value.Kind.Should().Be(TokenKind.StringLiteral);
    }

    [Fact]
    public void ParseExpression_Contains_Precedence()
    {
        // contains (prec 40) binds tighter than and (prec 20)
        // "tags contains "a" and x > 0" → and(contains(tags, "a"), >(x, 0))
        var expr = ParseExpr("tags contains \"a\" and x > 0");
        var and = expr.Should().BeOfType<BinaryExpression>().Subject;
        and.Operator.Kind.Should().Be(TokenKind.And);
        and.Left.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.Contains);
        and.Right.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.GreaterThan);
    }

    [Fact]
    public void ParseExpression_Contains_ChainedNonAssociative()
    {
        // tags contains "a" contains "b" — contains is NonAssociative; parser emits
        // NonAssociativeComparison diagnostic and stops after the first contains.
        var tree = Parser.Parse(Lexer.Lex("""rule tags contains "a" contains "b" because "msg" """));
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.NonAssociativeComparison),
            "chaining 'contains' should emit NonAssociativeComparison (Slice 18)");
    }

    // ── Interpolated strings (Slice 11) ───────────────────────────────────

    [Fact]
    public void ParseExpression_InterpolatedString_SingleHole()
    {
        // "Hello {name}" → 3 parts: TextInterpolationPart(StringStart), ExpressionInterpolationPart(name), TextInterpolationPart(StringEnd)
        var expr = ParseExpr("\"Hello {name}\"");
        var interp = expr.Should().BeOfType<InterpolatedStringExpression>().Subject;
        interp.Parts.Should().HaveCount(3);
        interp.Parts[0].Should().BeOfType<TextInterpolationPart>();
        interp.Parts[1].Should().BeOfType<ExpressionInterpolationPart>()
            .Which.Value.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("name");
        interp.Parts[2].Should().BeOfType<TextInterpolationPart>();
    }

    [Fact]
    public void ParseExpression_InterpolatedString_MultipleHoles()
    {
        // "{a} and {b}" → 5 parts: Text, Expr(a), Text, Expr(b), Text
        var expr = ParseExpr("\"{a} and {b}\"");
        var interp = expr.Should().BeOfType<InterpolatedStringExpression>().Subject;
        interp.Parts.Should().HaveCount(5);
        interp.Parts[0].Should().BeOfType<TextInterpolationPart>();
        interp.Parts[1].Should().BeOfType<ExpressionInterpolationPart>()
            .Which.Value.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("a");
        interp.Parts[2].Should().BeOfType<TextInterpolationPart>();
        interp.Parts[3].Should().BeOfType<ExpressionInterpolationPart>()
            .Which.Value.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("b");
        interp.Parts[4].Should().BeOfType<TextInterpolationPart>();
    }

    [Fact]
    public void ParseExpression_InterpolatedString_ExpressionInHole()
    {
        // "Total: {a + b}" → 3 parts, middle hole contains BinaryExpression(Plus)
        var expr = ParseExpr("\"Total: {a + b}\"");
        var interp = expr.Should().BeOfType<InterpolatedStringExpression>().Subject;
        interp.Parts.Should().HaveCount(3);
        interp.Parts[1].Should().BeOfType<ExpressionInterpolationPart>()
            .Which.Value.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.Plus);
    }

    // ── is set / is not set postfix operators (GAP-3) ──────────────────────

    [Fact]
    public void ParseExpression_IsSet()
    {
        var expr = ParseExpr("opt is set");
        var isSet = expr.Should().BeOfType<IsSetExpression>().Subject;
        isSet.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("opt");
    }

    [Fact]
    public void ParseExpression_IsNotSet()
    {
        var expr = ParseExpr("opt is not set");
        var isNotSet = expr.Should().BeOfType<IsNotSetExpression>().Subject;
        isNotSet.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("opt");
    }

    [Fact]
    public void ParseExpression_IsSet_InCondition()
    {
        // "opt is set and x > 0" → And(IsSet(opt), BinaryExpr(x > 0))
        var expr = ParseExpr("opt is set and x > 0");
        var and = expr.Should().BeOfType<BinaryExpression>().Subject;
        and.Operator.Kind.Should().Be(TokenKind.And);
        and.Left.Should().BeOfType<IsSetExpression>()
            .Which.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("opt");
    }

    [Fact]
    public void ParseExpression_IsNotSet_InCondition()
    {
        // "opt is not set or y < 5" → Or(IsNotSet(opt), BinaryExpr(y < 5))
        var expr = ParseExpr("opt is not set or y < 5");
        var or = expr.Should().BeOfType<BinaryExpression>().Subject;
        or.Operator.Kind.Should().Be(TokenKind.Or);
        or.Left.Should().BeOfType<IsNotSetExpression>()
            .Which.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("opt");
    }

    // M5
    [Fact]
    public void ParseExpression_IsSet_Chained_ParsesAsNestedIsSet()
    {
        // "x is set is set" — postfix operators have no binary chaining guard;
        // the parser silently produces IsSetExpression(IsSetExpression(x)).
        // This pins the defined behavior so regressions are caught.
        var expr = ParseExpr("x is set is set");
        var outer = expr.Should().BeOfType<IsSetExpression>().Subject;
        outer.Operand.Should().BeOfType<IsSetExpression>(
            "chaining 'is set is set' nests the inner IsSetExpression as the operand of the outer one");
    }

    // ── List literals (GAP-6, Slice 5) ────────────────────────────────────

    [Fact]
    public void ParseExpression_ListLiteral_Empty()
    {
        var expr = ParseExpr("[]");
        var list = expr.Should().BeOfType<ListLiteralExpression>().Subject;
        list.Elements.Should().BeEmpty();
    }

    [Fact]
    public void ParseExpression_ListLiteral_SingleElement()
    {
        var expr = ParseExpr("[1]");
        var list = expr.Should().BeOfType<ListLiteralExpression>().Subject;
        list.Elements.Should().HaveCount(1);
        list.Elements[0].Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("1");
    }

    [Fact]
    public void ParseExpression_ListLiteral_MultipleElements()
    {
        var expr = ParseExpr("[1, 2, 3]");
        var list = expr.Should().BeOfType<ListLiteralExpression>().Subject;
        list.Elements.Should().HaveCount(3);
    }

    [Fact]
    public void ParseExpression_ListLiteral_StringElements()
    {
        var expr = ParseExpr("[\"a\", \"b\"]");
        var list = expr.Should().BeOfType<ListLiteralExpression>().Subject;
        list.Elements.Should().HaveCount(2);
        list.Elements[0].Should().BeOfType<LiteralExpression>()
            .Which.Value.Kind.Should().Be(TokenKind.StringLiteral);
        list.Elements[1].Should().BeOfType<LiteralExpression>()
            .Which.Value.Kind.Should().Be(TokenKind.StringLiteral);
    }

    [Fact]
    public void ParseExpression_ListLiteral_NestedExpressions()
    {
        var expr = ParseExpr("[a + 1, b * 2]");
        var list = expr.Should().BeOfType<ListLiteralExpression>().Subject;
        list.Elements.Should().HaveCount(2);
        list.Elements[0].Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.Plus);
        list.Elements[1].Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.Star);
    }

    // ── Method calls (GAP-7, Slice 6) ─────────────────────────────────────

    [Fact]
    public void ParseExpression_MethodCall_NoArgs()
    {
        var expr = ParseExpr("obj.Method()");
        var call = expr.Should().BeOfType<MethodCallExpression>().Subject;
        call.MethodName.Should().Be("Method");
        call.Arguments.Should().BeEmpty();
        call.Receiver.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("obj");
    }

    [Fact]
    public void ParseExpression_MethodCall_SingleArg()
    {
        var expr = ParseExpr("obj.Method(x)");
        var call = expr.Should().BeOfType<MethodCallExpression>().Subject;
        call.MethodName.Should().Be("Method");
        call.Arguments.Should().HaveCount(1);
        call.Arguments[0].Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("x");
    }

    [Fact]
    public void ParseExpression_MethodCall_MultipleArgs()
    {
        var expr = ParseExpr("obj.Method(x, y, z)");
        var call = expr.Should().BeOfType<MethodCallExpression>().Subject;
        call.MethodName.Should().Be("Method");
        call.Arguments.Should().HaveCount(3);
    }

    [Fact]
    public void ParseExpression_MethodCall_ChainedAccess()
    {
        // a.b.Method(x) → receiver is MemberAccessExpression(a, b), method = "Method"
        var expr = ParseExpr("a.b.Method(x)");
        var call = expr.Should().BeOfType<MethodCallExpression>().Subject;
        call.MethodName.Should().Be("Method");
        call.Arguments.Should().HaveCount(1);
        call.Receiver.Should().BeOfType<MemberAccessExpression>()
            .Which.Member.Text.Should().Be("b");
    }
}
