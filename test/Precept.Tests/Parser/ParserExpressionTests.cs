using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests for the Slice 3 Pratt expression parser.
///
/// Tests are RED-E — they describe the expression AST shapes that
/// <c>Parser.Parse</c> must produce once Slice 3 is implemented. Every test
/// method that exercises expression-slot content is marked with
/// <c>// RED-E: Slice 3 — expression parsing</c> in its body.
///
/// All 13 <see cref="ExpressionFormKind"/> members are covered across:
///   §0  Lexer smoke tests (GREEN)
///   §1  Literals
///   §2  Identifiers
///   §3  Binary operations
///   §4  Unary operations
///   §5  Grouped expressions
///   §6  Member access
///   §7  Method calls
///   §8  Function calls
///   §9  Conditional expressions
///   §10 List literals
///   §11 Postfix operations (is set / is not set)
///   §12 Quantifiers (each / any / no)
///   §13 CI function calls (~startsWith / ~endsWith)
///   §14 Expression slot plumbing across construct kinds
///   §15 Termination: RuleExpression stops before `when`
///   §16 ExpressionFormKind catalog coverage (GREEN)
/// </summary>
public class ParserExpressionTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Private harness helpers — parse and extract one expression-bearing slot.
    //  All use kind-based slot lookup (never index-based).
    // ════════════════════════════════════════════════════════════════════════════

    private static ParsedExpression GetRuleExpression(string source)
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));
        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        var slot = (RuleExpressionSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.RuleExpression);
        return slot.Expression;
    }

    private static ParsedExpression GetRuleGuard(string source)
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));
        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        var slot = (GuardClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.GuardClause);
        return slot.Expression;
    }

    private static ParsedExpression GetCompute(string source)
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));
        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var slot = (ComputeExpressionSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.ComputeExpression);
        return slot.Expression;
    }

    private static ParsedExpression GetEnsure(string source)
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));
        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.StateEnsure);
        var slot = (EnsureClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.EnsureClause);
        return slot.Expression;
    }

    private static ParsedExpression GetTransitionGuard(string source)
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));
        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var slot = (GuardClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.GuardClause);
        return slot.Expression;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §0. Lexer smoke tests — GREEN
    //  Confirm every DSL snippet used in RED-E tests lexes without errors.
    //  Failures here indicate a bad source string, not a missing parser feature.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("rule 0 because \"msg\"")]
    [InlineData("rule true because \"msg\"")]
    [InlineData("rule false because \"msg\"")]
    [InlineData("rule amount > 0 because \"msg\"")]
    [InlineData("rule a + b * c because \"msg\"")]
    [InlineData("rule a + b + c because \"msg\"")]
    [InlineData("rule a or b and c because \"msg\"")]
    [InlineData("rule a - b because \"msg\"")]
    [InlineData("rule a * b because \"msg\"")]
    [InlineData("rule a / b because \"msg\"")]
    [InlineData("rule a % b because \"msg\"")]
    [InlineData("rule a < b because \"msg\"")]
    [InlineData("rule a <= b because \"msg\"")]
    [InlineData("rule a >= b because \"msg\"")]
    [InlineData("rule a == b because \"msg\"")]
    [InlineData("rule a != b because \"msg\"")]
    [InlineData("rule a and b because \"msg\"")]
    [InlineData("rule a or b because \"msg\"")]
    [InlineData("rule a ~= \"test\" because \"ci\"")]
    [InlineData("rule not valid because \"msg\"")]
    [InlineData("rule (a + b) * c because \"msg\"")]
    [InlineData("rule (a or b) and c because \"msg\"")]
    [InlineData("rule loan.amount > 0 because \"msg\"")]
    [InlineData("rule loan.amount.round(2) > 0 because \"msg\"")]
    [InlineData("rule round(2.5) > 0 because \"msg\"")]
    [InlineData("rule (if amount > 0 then amount else 0) > 0 because \"msg\"")]
    [InlineData("rule amount is set because \"msg\"")]
    [InlineData("rule amount is not set because \"msg\"")]
    [InlineData("rule amount is set and total > 0 because \"msg\"")]
    [InlineData("rule each item in items (item > 0) because \"msg\"")]
    [InlineData("rule any item in items (item > 0) because \"msg\"")]
    [InlineData("rule ~startsWith(name, \"A\") because \"msg\"")]
    [InlineData("rule ~endsWith(name, \"Z\") because \"msg\"")]
    [InlineData("rule amount > 0 when active because \"msg\"")]
    [InlineData("field label as string <- \"hello\"")]
    [InlineData("field val as number <- -amount")]
    [InlineData("field tax as number <- subtotal * 0.1")]
    [InlineData("field tags as number <- [1, 2, 3]")]
    [InlineData("field tags as number <- []")]
    [InlineData("field dt as date <- '2026-01-01'")]
    [InlineData("from Draft on Submit when amount > 0 -> transition Approved")]
    [InlineData("in Draft ensure amount > 0 because \"msg\"")]
    [InlineData("in Draft when active ensure amount > 0 because \"msg\"")]
    public void ExpressionSource_LexesWithoutErrors(string source)
    {
        // GREEN — every source string used in RED-E tests must lex cleanly.
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; lex errors indicate a bad test source string");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §1. Literals — ExpressionFormKind.Literal
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Literal_NumberZero_InRuleExpression_ProducesLiteralExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule 0 because \"msg\"");

        expr.Should().BeOfType<LiteralExpression>();
        var lit = (LiteralExpression)expr;
        lit.LiteralKind.Should().Be(TokenKind.NumberLiteral, "bare '0' is a NumberLiteral token");
        lit.Text.Should().Be("0");
    }

    [Fact]
    public void Literal_BooleanTrue_InRuleExpression_ProducesLiteralExpression_WithTrueKind()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule true because \"msg\"");

        expr.Should().BeOfType<LiteralExpression>();
        ((LiteralExpression)expr).LiteralKind.Should().Be(TokenKind.True,
            "'true' keyword produces a TokenKind.True literal");
    }

    [Fact]
    public void Literal_BooleanFalse_InRuleExpression_ProducesLiteralExpression_WithFalseKind()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule false because \"msg\"");

        expr.Should().BeOfType<LiteralExpression>();
        ((LiteralExpression)expr).LiteralKind.Should().Be(TokenKind.False,
            "'false' keyword produces a TokenKind.False literal");
    }

    [Fact]
    public void Literal_StringLiteral_InComputeExpression_ProducesLiteralExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetCompute("field label as string <- \"hello\"");

        expr.Should().BeOfType<LiteralExpression>();
        var lit = (LiteralExpression)expr;
        lit.LiteralKind.Should().Be(TokenKind.StringLiteral,
            "double-quoted string without interpolation is a StringLiteral token");
        lit.Text.Should().Be("hello");
    }

    [Fact]
    public void Literal_TypedConstant_InComputeExpression_ProducesLiteralExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetCompute("field dt as date <- '2026-01-01'");

        expr.Should().BeOfType<LiteralExpression>();
        var lit = (LiteralExpression)expr;
        lit.LiteralKind.Should().Be(TokenKind.TypedConstant,
            "single-quoted constant without interpolation is a TypedConstant token");
        lit.Text.Should().Be("2026-01-01");
    }

    [Fact]
    public void Literal_NumberInBinaryRhs_TextMatchesSource()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule x == 42 because \"num\"");

        var bin = (BinaryOperationExpression)expr;
        bin.Right.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("42");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. Identifiers — ExpressionFormKind.Identifier
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Identifier_LeftOperandOfComparison_ProducesIdentifierExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule amount > 0 because \"msg\"");

        expr.Should().BeOfType<BinaryOperationExpression>();
        var bin = (BinaryOperationExpression)expr;
        bin.Left.Should().BeOfType<IdentifierExpression>();
        ((IdentifierExpression)bin.Left).Name.Should().Be("amount",
            "the identifier on the left of '>' must carry its source name");
    }

    [Fact]
    public void Identifier_RightOperandOfBinaryOp_NameIsPreserved()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule a > b because \"msg\"");

        var bin = (BinaryOperationExpression)expr;
        bin.Right.Should().BeOfType<IdentifierExpression>();
        ((IdentifierExpression)bin.Right).Name.Should().Be("b");
    }

    [Fact]
    public void Identifier_StandaloneInRuleExpression_ProducesIdentifierExpression()
    {
        // RED-E: Slice 3 — expression parsing
        // A bare identifier is a valid boolean-position expression; the type checker
        // enforces boolean, but the parser must accept and produce IdentifierExpression.
        var expr = GetRuleExpression("rule active because \"must be active\"");

        expr.Should().BeOfType<IdentifierExpression>();
        ((IdentifierExpression)expr).Name.Should().Be("active");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. Binary Operations — ExpressionFormKind.BinaryOperation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BinaryOp_Precedence_MultiplicationBindsTighterThanAddition()
    {
        // RED-E: Slice 3 — expression parsing
        // a + b * c  ->  a + (b * c) — * has precedence 60, + has precedence 50
        var expr = GetRuleExpression("rule a + b * c because \"msg\"");

        var plus = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        plus.Operator.Should().Be(TokenKind.Plus, "top-level op is + because * binds more tightly");
        plus.Left.Should().BeOfType<IdentifierExpression>();
        var times = plus.Right.Should().BeOfType<BinaryOperationExpression>().Subject;
        times.Operator.Should().Be(TokenKind.Star);
        ((IdentifierExpression)times.Left).Name.Should().Be("b");
        ((IdentifierExpression)times.Right).Name.Should().Be("c");
    }

    [Fact]
    public void BinaryOp_LeftAssociativity_AdditionGroupsLeftToRight()
    {
        // RED-E: Slice 3 — expression parsing
        // a + b + c  ->  (a + b) + c  — left-to-right grouping
        var expr = GetRuleExpression("rule a + b + c because \"msg\"");

        var outer = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        outer.Operator.Should().Be(TokenKind.Plus);
        var inner = outer.Left.Should().BeOfType<BinaryOperationExpression>().Subject;
        inner.Operator.Should().Be(TokenKind.Plus);
        ((IdentifierExpression)inner.Left).Name.Should().Be("a");
        ((IdentifierExpression)inner.Right).Name.Should().Be("b");
        ((IdentifierExpression)outer.Right).Name.Should().Be("c");
    }

    [Fact]
    public void BinaryOp_AndOrPrecedence_AndBindsTighterThanOr()
    {
        // RED-E: Slice 3 — expression parsing
        // a or b and c  ->  a or (b and c)  — and (prec 20) > or (prec 10)
        var expr = GetRuleExpression("rule a or b and c because \"msg\"");

        var or = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        or.Operator.Should().Be(TokenKind.Or, "top-level op is 'or'; 'and' binds more tightly");
        or.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("a");
        or.Right.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.And);
    }

    [Fact]
    public void BinaryOp_GreaterThan_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a > b because \"msg\""))
            .Operator.Should().Be(TokenKind.GreaterThan);
    }

    [Fact]
    public void BinaryOp_LessThan_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a < b because \"msg\""))
            .Operator.Should().Be(TokenKind.LessThan);
    }

    [Fact]
    public void BinaryOp_LessThanOrEqual_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a <= b because \"msg\""))
            .Operator.Should().Be(TokenKind.LessThanOrEqual);
    }

    [Fact]
    public void BinaryOp_GreaterThanOrEqual_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a >= b because \"msg\""))
            .Operator.Should().Be(TokenKind.GreaterThanOrEqual);
    }

    [Fact]
    public void BinaryOp_DoubleEquals_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a == b because \"msg\""))
            .Operator.Should().Be(TokenKind.DoubleEquals);
    }

    [Fact]
    public void BinaryOp_NotEquals_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a != b because \"msg\""))
            .Operator.Should().Be(TokenKind.NotEquals);
    }

    [Fact]
    public void BinaryOp_Plus_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a + b because \"msg\""))
            .Operator.Should().Be(TokenKind.Plus);
    }

    [Fact]
    public void BinaryOp_Minus_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a - b because \"msg\""))
            .Operator.Should().Be(TokenKind.Minus);
    }

    [Fact]
    public void BinaryOp_Star_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a * b because \"msg\""))
            .Operator.Should().Be(TokenKind.Star);
    }

    [Fact]
    public void BinaryOp_Slash_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a / b because \"msg\""))
            .Operator.Should().Be(TokenKind.Slash);
    }

    [Fact]
    public void BinaryOp_Percent_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a % b because \"msg\""))
            .Operator.Should().Be(TokenKind.Percent);
    }

    [Fact]
    public void BinaryOp_And_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a and b because \"msg\""))
            .Operator.Should().Be(TokenKind.And);
    }

    [Fact]
    public void BinaryOp_Or_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a or b because \"msg\""))
            .Operator.Should().Be(TokenKind.Or);
    }

    [Fact]
    public void BinaryOp_CaseInsensitiveEquals_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule a ~= \"test\" because \"ci\""))
            .Operator.Should().Be(TokenKind.CaseInsensitiveEquals);
    }

    [Fact]
    public void BinaryOp_Contains_ProducesCorrectToken()
    {
        // RED-E: Slice 3 — expression parsing
        ((BinaryOperationExpression)GetRuleExpression("rule tags contains \"x\" because \"msg\""))
            .Operator.Should().Be(TokenKind.Contains);
    }

    [Fact]
    public void BinaryOp_ArithmeticPrecedence_MulRhsOfAdd_IsNestedCorrectly()
    {
        // RED-E: Slice 3 — expression parsing
        // rule a + b * c > 0  ->  top: >(+(a, *(b,c)), 0)
        var expr = GetRuleExpression("rule a + b * c > 0 because \"arith\"");

        var gt = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        gt.Operator.Should().Be(TokenKind.GreaterThan);
        var add = gt.Left.Should().BeOfType<BinaryOperationExpression>().Subject;
        add.Operator.Should().Be(TokenKind.Plus);
        add.Right.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.Star);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §4. Unary Operations — ExpressionFormKind.UnaryOperation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Unary_Not_InRuleExpression_ProducesUnaryOperationExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule not valid because \"msg\"");

        var unary = expr.Should().BeOfType<UnaryOperationExpression>().Subject;
        unary.Operator.Should().Be(TokenKind.Not,
            "prefix 'not' produces UnaryOperationExpression with TokenKind.Not");
        unary.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("valid");
    }

    [Fact]
    public void Unary_LogicalNot_OperandIsPreserved()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule not done because \"not done\"");

        var unary = expr.Should().BeOfType<UnaryOperationExpression>().Subject;
        unary.Operator.Should().Be(TokenKind.Not);
        unary.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("done");
    }

    [Fact]
    public void Unary_Negate_BeforeIdentifier_ProducesMinusOperator()
    {
        // RED-E: Slice 3 — expression parsing
        // Unary minus before an identifier must produce UnaryOperationExpression.
        // Per spec §1.3, only '-' followed by a NumberLiteral is constant-folded;
        // '-identifier' remains a runtime negate.
        var expr = GetCompute("field val as number <- -amount");

        var unary = expr.Should().BeOfType<UnaryOperationExpression>().Subject;
        unary.Operator.Should().Be(TokenKind.Minus,
            "unary minus before an identifier produces TokenKind.Minus");
        unary.Operand.Should().BeOfType<IdentifierExpression>();
    }

    [Fact]
    public void Unary_Negate_InBinaryContext_IsLeftOperandOfGreaterThan()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule -x > 0 because \"neg\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<UnaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.Minus);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §5. Grouped Expressions — ExpressionFormKind.Grouped
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Grouped_ParenthesizedAddition_BecomesLeftOperandOfMultiplication()
    {
        // RED-E: Slice 3 — expression parsing
        // (a + b) * c  ->  top is *(GroupedExpr(+(a,b)), c)
        var expr = GetRuleExpression("rule (a + b) * c because \"msg\"");

        var times = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        times.Operator.Should().Be(TokenKind.Star,
            "parentheses override precedence: * is the top-level operator");
        times.Left.Should().BeOfType<GroupedExpression>();
    }

    [Fact]
    public void Grouped_InnerExpression_IsAdditionOfTwoIdentifiers()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule (a + b) * c because \"msg\"");

        var times = (BinaryOperationExpression)expr;
        var group = (GroupedExpression)times.Left;
        var inner = group.Inner.Should().BeOfType<BinaryOperationExpression>().Subject;
        inner.Operator.Should().Be(TokenKind.Plus);
        ((IdentifierExpression)inner.Left).Name.Should().Be("a");
        ((IdentifierExpression)inner.Right).Name.Should().Be("b");
    }

    [Fact]
    public void Grouped_OrInsideParens_BecomesLeftOperandOfAnd()
    {
        // RED-E: Slice 3 — expression parsing
        // (a or b) and c — parentheses override the lower precedence of 'or'
        var expr = GetRuleExpression("rule (a or b) and c because \"grouped\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.And,
            "top-level is 'and'; the grouped '(a or b)' is its left operand");
        bin.Left.Should().BeOfType<GroupedExpression>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §6. Member Access — ExpressionFormKind.MemberAccess
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MemberAccess_DotAccess_ProducesMemberAccessExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule loan.amount > 0 because \"msg\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<MemberAccessExpression>(
            "loan.amount on the left of '>' must parse as MemberAccessExpression");
    }

    [Fact]
    public void MemberAccess_TargetAndMemberName_ArePreserved()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule loan.amount > 0 because \"msg\"");

        var member = (MemberAccessExpression)((BinaryOperationExpression)expr).Left;
        member.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("loan");
        member.MemberName.Should().Be("amount");
    }

    [Fact]
    public void MemberAccess_MemberTokenKind_IsIdentifier()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule loan.amount > 0 because \"msg\"");

        var member = (MemberAccessExpression)((BinaryOperationExpression)expr).Left;
        member.MemberTokenKind.Should().Be(TokenKind.Identifier,
            "a user-defined member name is an Identifier token");
    }

    [Fact]
    public void MemberAccess_DotMinOnCollection_MemberName_IsPreserved()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule items.min == \"a\" because \"min\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<MemberAccessExpression>()
            .Which.MemberName.Should().Be("min");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §7. Method Calls — ExpressionFormKind.MethodCall
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MethodCall_DotMethodWithArgs_ProducesMethodCallExpression()
    {
        // RED-E: Slice 3 — expression parsing
        // loan.amount.round(2) -> MethodCallExpression
        var expr = GetRuleExpression("rule loan.amount.round(2) > 0 because \"msg\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<MethodCallExpression>(
            "target.method(args) must produce MethodCallExpression");
    }

    [Fact]
    public void MethodCall_MethodName_IsPreserved()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule loan.amount.round(2) > 0 because \"msg\"");

        var call = (MethodCallExpression)((BinaryOperationExpression)expr).Left;
        call.MethodName.Should().Be("round");
    }

    [Fact]
    public void MethodCall_Target_IsTheMemberAccessReceiverBeforeTheMethodName()
    {
        // RED-E: Slice 3 — expression parsing
        // loan.amount.round(2) — Target is loan.amount; MethodName is round
        var expr = GetRuleExpression("rule loan.amount.round(2) > 0 because \"msg\"");

        var call = (MethodCallExpression)((BinaryOperationExpression)expr).Left;
        var receiver = call.Target.Should().BeOfType<MemberAccessExpression>().Subject;
        ((IdentifierExpression)receiver.Target).Name.Should().Be("loan");
        receiver.MemberName.Should().Be("amount");
    }

    [Fact]
    public void MethodCall_Arguments_ContainExactlyOneNumberLiteralArg()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule loan.amount.round(2) > 0 because \"msg\"");

        var call = (MethodCallExpression)((BinaryOperationExpression)expr).Left;
        call.Arguments.Should().ContainSingle("round(2) has exactly one argument");
        call.Arguments[0].Should().BeOfType<LiteralExpression>()
            .Which.LiteralKind.Should().Be(TokenKind.NumberLiteral);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §8. Function Calls — ExpressionFormKind.FunctionCall
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FunctionCall_BareIdentifierCallSyntax_ProducesFunctionCallExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule round(2.5) > 0 because \"msg\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<FunctionCallExpression>(
            "round(2.5) with bare identifier callee must produce FunctionCallExpression");
    }

    [Fact]
    public void FunctionCall_FunctionName_IsPreserved()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule round(2.5) > 0 because \"msg\"");

        var call = (FunctionCallExpression)((BinaryOperationExpression)expr).Left;
        call.FunctionName.Should().Be("round");
    }

    [Fact]
    public void FunctionCall_Arguments_ContainSingleNumberLiteralArg()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule round(2.5) > 0 because \"msg\"");

        var call = (FunctionCallExpression)((BinaryOperationExpression)expr).Left;
        call.Arguments.Should().ContainSingle("round(2.5) has exactly one argument");
        call.Arguments[0].Should().BeOfType<LiteralExpression>()
            .Which.LiteralKind.Should().Be(TokenKind.NumberLiteral);
    }

    [Fact]
    public void FunctionCall_Approximate_FunctionNamePreserved()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule approximate(x) == 0 because \"zero\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<FunctionCallExpression>()
            .Which.FunctionName.Should().Be("approximate");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §9. Conditional Expressions — ExpressionFormKind.Conditional
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Conditional_StandaloneIfThenElse_ProducesConditionalExpression()
    {
        // RED-E: Slice 3 — expression parsing
        // rule if x > 0 then true else false — top-level is ConditionalExpression
        var expr = GetRuleExpression("rule if x > 0 then true else false because \"cond\"");

        expr.Should().BeOfType<ConditionalExpression>();
    }

    [Fact]
    public void Conditional_ConditionBranch_IsABinaryComparison()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule if x > 0 then true else false because \"cond\"");

        ((ConditionalExpression)expr).Condition
            .Should().BeOfType<BinaryOperationExpression>("condition is 'x > 0'");
    }

    [Fact]
    public void Conditional_ThenElseBranches_AreLiteralExpressions()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule if x > 0 then true else false because \"cond\"");

        var cond = (ConditionalExpression)expr;
        cond.ThenBranch.Should().BeOfType<LiteralExpression>("then-branch is 'true'");
        cond.ElseBranch.Should().BeOfType<LiteralExpression>("else-branch is 'false'");
    }

    [Fact]
    public void Conditional_GroupedIfThenElse_OuterIsGreaterThan_InnerIsConditional()
    {
        // RED-E: Slice 3 — expression parsing
        // (if amount > 0 then amount else 0) > 0  ->  top: >(GroupedExpr(Conditional), 0)
        var expr = GetRuleExpression("rule (if amount > 0 then amount else 0) > 0 because \"msg\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<GroupedExpression>()
            .Which.Inner.Should().BeOfType<ConditionalExpression>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §10. List Literals — ExpressionFormKind.ListLiteral
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ListLiteral_ThreeElementList_InComputeExpression_ProducesListLiteralExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetCompute("field tags as number <- [1, 2, 3]");

        expr.Should().BeOfType<ListLiteralExpression>();
        ((ListLiteralExpression)expr).Elements.Should().HaveCount(3,
            "the list [1, 2, 3] has three elements");
    }

    [Fact]
    public void ListLiteral_AllElements_AreLiteralExpressions()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetCompute("field tags as number <- [1, 2, 3]");

        var list = (ListLiteralExpression)expr;
        list.Elements.Should().AllSatisfy(e => e.Should().BeOfType<LiteralExpression>(),
            "each element of [1, 2, 3] is a NumberLiteral");
    }

    [Fact]
    public void ListLiteral_Empty_ProducesListLiteralExpressionWithNoElements()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetCompute("field tags as number <- []");

        expr.Should().BeOfType<ListLiteralExpression>();
        ((ListLiteralExpression)expr).Elements.Should().BeEmpty("[] is an empty list literal");
    }

    [Fact]
    public void ListLiteral_AsRightOperandOfContains_TwoStringElements()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule tags contains [\"a\", \"b\"] because \"list\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Right.Should().BeOfType<ListLiteralExpression>()
            .Which.Elements.Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §11. Postfix Operations — ExpressionFormKind.PostfixOperation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Postfix_IsSet_ProducesPostfixOperationExpression_WithIsNegatedFalse()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule amount is set because \"msg\"");

        var postfix = expr.Should().BeOfType<PostfixOperationExpression>().Subject;
        postfix.IsNegated.Should().BeFalse("'is set' is the non-negated presence check");
        ((IdentifierExpression)postfix.Operand).Name.Should().Be("amount");
    }

    [Fact]
    public void Postfix_IsNotSet_ProducesPostfixOperationExpression_WithIsNegatedTrue()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule amount is not set because \"msg\"");

        var postfix = expr.Should().BeOfType<PostfixOperationExpression>().Subject;
        postfix.IsNegated.Should().BeTrue("'is not set' is the negated absence check");
        postfix.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("amount");
    }

    [Fact]
    public void Postfix_IsNotFollowedBySet_DoesNotLoopInfinitely_IsStoppedByLedCheck()
    {
        // Regression for Frank's BLOCKED finding: "field is not valid" must NOT cause an
        // infinite loop. GetLedBindingPower must return (-1,-1) for `is not <non-set>`.
        // The expression parser should stop at `is` (treating it as a termination signal),
        // leaving `is not valid` unconsumed — returning `field` as an IdentifierExpression.
        // The construct itself will produce a diagnostic, but must not hang.
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex("rule field is not valid because \"msg\""));
        // Should complete without hanging; diagnostics may be present but no infinite loop.
        manifest.Should().NotBeNull("parse must complete — no infinite loop on 'is not <non-set>'");
    }

    [Fact]
    public void Postfix_IsSetHasHigherPrecedenceThanAnd_IsLeftOperandOfAnd()
    {
        // RED-E: Slice 3 — expression parsing
        // 'amount is set and total > 0'
        // is-set (prec 60) > and (prec 20), so top is 'and', left is 'amount is set'
        var expr = GetRuleExpression("rule amount is set and total > 0 because \"msg\"");

        var and = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        and.Operator.Should().Be(TokenKind.And);
        and.Left.Should().BeOfType<PostfixOperationExpression>(
            "'amount is set' binds tighter than 'and' and must be the left child");
        ((PostfixOperationExpression)and.Left).IsNegated.Should().BeFalse();
    }

    [Fact]
    public void Postfix_IsSet_Operand_IsIdentifierExpression_WithCorrectName()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule name is set because \"needed\"");

        var postfix = expr.Should().BeOfType<PostfixOperationExpression>().Subject;
        postfix.Operand.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("name");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §12. Quantifiers — ExpressionFormKind.Quantifier
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Quantifier_Each_ProducesQuantifierExpression_WithEachToken()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule each item in items (item > 0) because \"msg\"");

        var quant = expr.Should().BeOfType<QuantifierExpression>().Subject;
        quant.QuantifierToken.Should().Be(TokenKind.Each,
            "'each' quantifier keyword must be stored as TokenKind.Each");
        quant.BindingName.Should().Be("item", "the binding variable name must be preserved");
    }

    [Fact]
    public void Quantifier_Each_CollectionIsIdentifier_PredicateIsBinaryOp()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule each item in items (item > 0) because \"msg\"");

        var quant = (QuantifierExpression)expr;
        quant.Collection.Should().BeOfType<IdentifierExpression>(
            "the collection ref 'items' is a bare field name — IdentifierExpression");
        ((IdentifierExpression)quant.Collection).Name.Should().Be("items");
        quant.Predicate.Should().BeOfType<BinaryOperationExpression>(
            "the predicate 'item > 0' is a BinaryOperationExpression");
    }

    [Fact]
    public void Quantifier_Any_ProducesQuantifierExpression_WithAnyToken()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule any item in items (item > 0) because \"msg\"");

        expr.Should().BeOfType<QuantifierExpression>();
        ((QuantifierExpression)expr).QuantifierToken.Should().Be(TokenKind.Any);
    }

    [Fact]
    public void Quantifier_No_ProducesQuantifierExpression_WithNoToken()
    {
        // RED-E: Slice 3 — expression parsing
        // 'no item in items (...)' is disambiguated as a quantifier by lookahead:
        // Identifier followed by 'in' followed by CollectionRef followed by '(' -> quantifier.
        var expr = GetRuleExpression("rule no item in items (item > 0) because \"msg\"");

        expr.Should().BeOfType<QuantifierExpression>();
        ((QuantifierExpression)expr).QuantifierToken.Should().Be(TokenKind.No);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §13. CI Function Calls — ExpressionFormKind.CIFunctionCall
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CIFunctionCall_TildeStartsWith_ProducesCIFunctionCallExpression_NotFunctionCallExpression()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule ~startsWith(name, \"A\") because \"msg\"");

        expr.Should().BeOfType<CIFunctionCallExpression>(
            "'~startsWith(...)' must produce CIFunctionCallExpression, not FunctionCallExpression");
    }

    [Fact]
    public void CIFunctionCall_StartsWith_FunctionName_IsStartsWith()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule ~startsWith(name, \"A\") because \"msg\"");

        ((CIFunctionCallExpression)expr).FunctionName.Should().Be("startsWith");
    }

    [Fact]
    public void CIFunctionCall_StartsWith_HasExactlyTwoArguments()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule ~startsWith(name, \"A\") because \"msg\"");

        var ci = (CIFunctionCallExpression)expr;
        ci.Arguments.Should().HaveCount(2,
            "~startsWith requires exactly two arguments: subject and prefix");
        ci.Arguments[0].Should().BeOfType<IdentifierExpression>("first arg is the subject 'name'");
        ci.Arguments[1].Should().BeOfType<LiteralExpression>("second arg is the prefix string");
    }

    [Fact]
    public void CIFunctionCall_TildeEndsWith_FunctionName_IsEndsWith()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule ~endsWith(name, \"Z\") because \"msg\"");

        expr.Should().BeOfType<CIFunctionCallExpression>();
        ((CIFunctionCallExpression)expr).FunctionName.Should().Be("endsWith");
    }

    [Fact]
    public void CIFunctionCall_EndsWith_HasExactlyTwoArguments()
    {
        // RED-E: Slice 3 — expression parsing
        var expr = GetRuleExpression("rule ~endsWith(name, \"Z\") because \"msg\"");

        ((CIFunctionCallExpression)expr).Arguments.Should().HaveCount(2,
            "~endsWith requires exactly two arguments: subject and suffix");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §14. Expression slot plumbing
    //  Verifies expression-bearing slots on different construct kinds are
    //  populated and carry correctly-shaped expression trees.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SlotPlumbing_TransitionRow_GuardClauseSlot_IsPresentAndContainsBinaryOp()
    {
        // RED-E: Slice 3 — expression parsing
        const string source = "from Draft on Submit when amount > 0 -> transition Approved";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.GuardClause,
            "'when amount > 0' on a TransitionRow must materialize a GuardClause slot");

        var guard = (GuardClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.GuardClause);
        guard.Expression.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.GreaterThan);
    }

    [Fact]
    public void SlotPlumbing_FieldDeclaration_ComputeExpressionSlot_IsPresentAndContainsBinaryOp()
    {
        // RED-E: Slice 3 — expression parsing
        const string source = "field tax as number <- subtotal * 0.1";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.ComputeExpression,
            "FieldDeclaration '<- subtotal * 0.1' must materialize a ComputeExpression slot");

        var compute = (ComputeExpressionSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.ComputeExpression);
        compute.Expression.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.Star);
    }

    [Fact]
    public void SlotPlumbing_StateEnsure_EnsureClauseSlot_IsPresentAndContainsBinaryOp()
    {
        // RED-E: Slice 3 — expression parsing
        const string source = "in Draft ensure amount > 0 because \"msg\"";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.StateEnsure);
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.EnsureClause,
            "StateEnsure must materialize an EnsureClause slot");

        var ensure = (EnsureClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.EnsureClause);
        ensure.Expression.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.GreaterThan);
    }

    [Fact]
    public void SlotPlumbing_StateEnsure_WithGuard_MaterializesDistinctGuardAndEnsureSlots()
    {
        const string source = "in Draft when active ensure amount > 0 because \"msg\"";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.StateEnsure);
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.GuardClause,
            "StateEnsure now carries an explicit GuardClause slot in the catalog");
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.EnsureClause,
            "EnsureClause slot must remain present after the guard slot is added");

        var guard = (GuardClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.GuardClause);
        guard.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("active");
    }

    [Fact]
    public void SlotPlumbing_TransitionGuard_CanContainComplexLogicalExpression()
    {
        // RED-E: Slice 3 — expression parsing
        const string source = "from Draft on Submit when x > 0 and y -> transition Approved";
        var guardExpr = GetTransitionGuard(source);

        guardExpr.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.And);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §15. Termination: RuleExpression stops before `when`
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Termination_RuleExpression_DoesNotConsumeWhenKeyword()
    {
        // RED-E: Slice 3 — expression parsing
        // rule amount > 0 when active because "msg"
        // RuleExpression must be 'amount > 0', not extend to include 'active'
        var expr = GetRuleExpression("rule amount > 0 when active because \"msg\"");

        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.GreaterThan,
            "RuleExpression must stop before 'when'");
        bin.Right.Should().BeOfType<LiteralExpression>()
            .Which.LiteralKind.Should().Be(TokenKind.NumberLiteral,
                "right operand is '0', not the guard identifier");
    }

    [Fact]
    public void Termination_GuardClause_IsDistinctSlotFromRuleExpression()
    {
        // RED-E: Slice 3 — expression parsing
        const string source = "rule amount > 0 when active because \"msg\"";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.RuleExpression,
            "RuleExpression slot must be present");
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.GuardClause,
            "GuardClause slot must be materialized for 'when active'");

        var guard = (GuardClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.GuardClause);
        guard.Expression.Should().BeOfType<IdentifierExpression>("guard is the bare identifier 'active'");
        ((IdentifierExpression)guard.Expression).Name.Should().Be("active");
    }

    [Fact]
    public void Termination_BecauseClauseSlot_ContainsCorrectMessage()
    {
        // RED-E: Slice 3 — expression parsing
        const string source = "rule amount > 0 because \"Amount must be positive\"";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        construct.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.BecauseClause,
            "BecauseClause slot must be materialized");

        var because = (BecauseClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.BecauseClause);
        because.Message.Should().Be("Amount must be positive");
    }

    [Fact]
    public void Termination_NoPlaceholder_NonTrivialRuleIsNotLiteralTrue()
    {
        // RED-E: Slice 3 — expression parsing
        // A compound rule expression must NOT degrade to the stub LiteralExpression(True, "true").
        var expr = GetRuleExpression("rule x > 0 because \"pos\"");

        expr.Should().BeOfType<BinaryOperationExpression>(
            "a compound rule expression must produce a BinaryOperationExpression, not a True placeholder");
    }

    [Fact]
    public void Termination_GuardClause_DoesNotConsumeOutcomeArrow()
    {
        const string source = "from Draft on Submit when amount > 0 -> transition Approved";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var guard = (GuardClauseSlot)row.Slots.Single(s => s.Kind == ConstructSlotKind.GuardClause);
        guard.Expression.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.GreaterThan);

        var outcome = (OutcomeSlot)row.Slots.Single(s => s.Kind == ConstructSlotKind.Outcome);
        outcome.Outcome.Should().BeOfType<TransitionOutcome>()
            .Which.StateName.Should().Be("Approved");
    }

    [Fact]
    public void Termination_EnsureClause_DoesNotConsumeBecauseKeyword()
    {
        const string source = "in Draft ensure amount > 0 because \"msg\"";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        var construct = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.StateEnsure);
        var ensure = (EnsureClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.EnsureClause);
        ensure.Expression.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.GreaterThan);

        var because = (BecauseClauseSlot)construct.Slots.Single(s => s.Kind == ConstructSlotKind.BecauseClause);
        because.Message.Should().Be("msg");
    }

    [Fact]
    public void Termination_ComputeExpression_StopsAtNextConstructBoundary()
    {
        const string source = "field total as number <- subtotal\nrule total > 0 because \"msg\"";
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

        manifest.Diagnostics.Should().BeEmpty();
        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.RuleDeclaration);

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var compute = (ComputeExpressionSlot)field.Slots.Single(s => s.Kind == ConstructSlotKind.ComputeExpression);
        compute.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("subtotal");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §16. Negative / recovery coverage
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Negative_IncompleteBinaryExpression_EmitsExpectedToken_AndUsesPlaceholderRightOperand()
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex("rule a + because \"msg\""));

        manifest.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedToken));

        var rule = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        var expression = rule.GetRequiredSlot<RuleExpressionSlot>(ConstructSlotKind.RuleExpression).Expression;
        var binary = expression.Should().BeOfType<BinaryOperationExpression>().Subject;

        binary.Operator.Should().Be(TokenKind.Plus);
        binary.Right.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("true",
                "the parser currently recovers missing right operands with its expression placeholder literal");
    }

    [Fact]
    public void Negative_UnclosedParen_EmitsExpectedToken_AndPreservesGroupedExpression()
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex("field amount as number <- (a + b"));

        manifest.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedToken));

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var expression = field.GetRequiredSlot<ComputeExpressionSlot>(ConstructSlotKind.ComputeExpression).Expression;
        var grouped = expression.Should().BeOfType<GroupedExpression>().Subject;

        grouped.Inner.Should().BeOfType<BinaryOperationExpression>().Which.Operator.Should().Be(TokenKind.Plus);
    }

    [Fact]
    public void Negative_EmptyGuardClause_EmitsExpectedToken_AndUsesMissingExpressionSentinel()
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex("from Draft on Submit when -> transition Approved"));

        manifest.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedToken));

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.GetRequiredSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause).Expression.Should().BeOfType<MissingExpression>();
    }

    [Fact]
    public void Negative_UnknownFunctionName_StillProducesFunctionCallExpression()
    {
        var expression = GetRuleExpression("rule mystery(amount) because \"msg\"");

        expression.Should().BeOfType<FunctionCallExpression>()
            .Which.FunctionName.Should().Be("mystery");
    }

    [Fact]
    public void Negative_MissingBecauseClause_EmitsExpectedToken_AndMaterializesEmptyBecauseClauseSlot()
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex("rule amount > 0"));

        manifest.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedToken));

        var rule = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        rule.GetRequiredSlot<BecauseClauseSlot>(ConstructSlotKind.BecauseClause).Message.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §17. ExpressionFormKind catalog coverage — GREEN
    //  Pure catalog tests — no parser invoked. Must stay green.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ExpressionFormKind.Literal,          ExpressionCategory.Atom,       false)]
    [InlineData(ExpressionFormKind.Identifier,       ExpressionCategory.Atom,       false)]
    [InlineData(ExpressionFormKind.Grouped,          ExpressionCategory.Atom,       false)]
    [InlineData(ExpressionFormKind.BinaryOperation,  ExpressionCategory.Composite,  true)]
    [InlineData(ExpressionFormKind.UnaryOperation,   ExpressionCategory.Composite,  false)]
    [InlineData(ExpressionFormKind.MemberAccess,     ExpressionCategory.Composite,  true)]
    [InlineData(ExpressionFormKind.Conditional,      ExpressionCategory.Composite,  false)]
    [InlineData(ExpressionFormKind.FunctionCall,     ExpressionCategory.Invocation, false)]
    [InlineData(ExpressionFormKind.MethodCall,       ExpressionCategory.Invocation, true)]
    [InlineData(ExpressionFormKind.ListLiteral,      ExpressionCategory.Collection, false)]
    [InlineData(ExpressionFormKind.PostfixOperation, ExpressionCategory.Composite,  true)]
    [InlineData(ExpressionFormKind.Quantifier,       ExpressionCategory.Composite,  false)]
    [InlineData(ExpressionFormKind.CIFunctionCall,   ExpressionCategory.Invocation, false)]
    public void ExpressionFormCatalog_AllMembers_HaveCorrectCategoryAndDenotation(
        ExpressionFormKind kind, ExpressionCategory expectedCategory, bool expectedIsLed)
    {
        // GREEN — catalog metadata only; no parser involved.
        var meta = ExpressionForms.GetMeta(kind);
        meta.Category.Should().Be(expectedCategory,
            $"{kind} must belong to ExpressionCategory.{expectedCategory}");
        meta.IsLeftDenotation.Should().Be(expectedIsLed,
            expectedIsLed
                ? $"{kind} is a left-denotation (led) form that extends an existing left operand"
                : $"{kind} is a null-denotation (nud) form that starts a new expression");
    }

    [Fact]
    public void ExpressionFormCatalog_All_HasExactly14Members()
    {
        // GREEN — the expression form enum must have exactly 14 members.
        ExpressionForms.All.Should().HaveCount(14,
            "there are exactly 14 ExpressionFormKind members in the Precept expression grammar");
    }
}