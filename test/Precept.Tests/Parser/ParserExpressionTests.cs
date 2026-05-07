using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests for the Slice 3 Pratt expression parser.
/// Validates that all expression-carrying slots produce real ParsedExpression DU values
/// instead of advance-past-tokens LiteralExpression(True) placeholders.
/// </summary>
public class ParserExpressionTests
{
    // ═══════════════════════════════════════════════════════════════════════════════
    //  Helper: lex + parse a precept source fragment
    // ═══════════════════════════════════════════════════════════════════════════════

    private static ConstructManifest Parse(string source)
    {
        var tokens = Lexer.Lex(source);
        return Pipeline.Parser.Parse(tokens);
    }

    private static ParsedExpression GetRuleExpression(ConstructManifest manifest, int constructIndex = 0)
    {
        var construct = manifest.Constructs
            .Where(c => c.Meta.Kind == ConstructKind.RuleDeclaration)
            .ElementAt(constructIndex);
        var slot = construct.Slots.OfType<RuleExpressionSlot>().First();
        return slot.Expression;
    }

    private static ParsedExpression GetGuardExpression(ConstructManifest manifest, int index = 0)
    {
        var slots = manifest.Constructs.AsEnumerable().SelectMany(c => c.Slots).OfType<GuardClauseSlot>().ToList();
        return slots[index].Expression;
    }

    private static ParsedExpression GetComputeExpression(ConstructManifest manifest, int index = 0)
    {
        var slots = manifest.Constructs.AsEnumerable().SelectMany(c => c.Slots).OfType<ComputeExpressionSlot>().ToList();
        return slots[index].Expression;
    }

    private static ParsedExpression GetEnsureExpression(ConstructManifest manifest, int index = 0)
    {
        var slots = manifest.Constructs.AsEnumerable().SelectMany(c => c.Slots).OfType<EnsureClauseSlot>().ToList();
        return slots[index].Expression;
    }

    private static ParsedExpression GetOutcomeExpression(ConstructManifest manifest, int index = 0)
    {
        var slots = manifest.Constructs.AsEnumerable().SelectMany(c => c.Slots).OfType<OutcomeSlot>().ToList();
        return slots[index].Expression;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  §1. Rule Expressions
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RuleExpression_SimpleBooleanLiteral()
    {
        var result = Parse("precept Test\nrule true because \"always\"");
        var expr = GetRuleExpression(result);
        expr.Should().BeOfType<LiteralExpression>()
            .Which.LiteralKind.Should().Be(TokenKind.True);
    }

    [Fact]
    public void RuleExpression_Identifier()
    {
        var result = Parse("precept Test\nrule active because \"must be active\"");
        var expr = GetRuleExpression(result);
        expr.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("active");
    }

    [Fact]
    public void RuleExpression_BinaryComparison()
    {
        var result = Parse("precept Test\nfield amount as number\nrule amount > 0 because \"positive\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.GreaterThan);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("amount");
        bin.Right.Should().BeOfType<LiteralExpression>().Which.Text.Should().Be("0");
    }

    [Fact]
    public void RuleExpression_LogicalAnd()
    {
        var result = Parse("precept Test\nfield x as boolean\nfield y as boolean\nrule x and y because \"both\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.And);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("x");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("y");
    }

    [Fact]
    public void RuleExpression_UnaryNot()
    {
        var result = Parse("precept Test\nfield done as boolean\nrule not done because \"not done\"");
        var expr = GetRuleExpression(result);
        var unary = expr.Should().BeOfType<UnaryOperationExpression>().Subject;
        unary.Operator.Should().Be(TokenKind.Not);
        unary.Operand.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("done");
    }

    [Fact]
    public void RuleExpression_Precedence_AndBindsTighterThanOr()
    {
        // "a or b and c" should parse as "a or (b and c)"
        var result = Parse("precept Test\nfield a as boolean\nfield b as boolean\nfield c as boolean\nrule a or b and c because \"prec\"");
        var expr = GetRuleExpression(result);
        var outer = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        outer.Operator.Should().Be(TokenKind.Or);
        outer.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("a");
        var inner = outer.Right.Should().BeOfType<BinaryOperationExpression>().Subject;
        inner.Operator.Should().Be(TokenKind.And);
    }

    [Fact]
    public void RuleExpression_PostfixIsSet()
    {
        var result = Parse("precept Test\nfield name as string optional\nrule name is set because \"needed\"");
        var expr = GetRuleExpression(result);
        var postfix = expr.Should().BeOfType<PostfixOperationExpression>().Subject;
        postfix.IsNegated.Should().BeFalse();
        postfix.Operand.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("name");
    }

    [Fact]
    public void RuleExpression_PostfixIsNotSet()
    {
        var result = Parse("precept Test\nfield name as string optional\nrule name is not set because \"absent\"");
        var expr = GetRuleExpression(result);
        var postfix = expr.Should().BeOfType<PostfixOperationExpression>().Subject;
        postfix.IsNegated.Should().BeTrue();
        postfix.Operand.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("name");
    }

    [Fact]
    public void RuleExpression_FunctionCall()
    {
        var result = Parse("precept Test\nfield x as number\nrule approximate(x) == 0 because \"zero\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<FunctionCallExpression>()
            .Which.FunctionName.Should().Be("approximate");
    }

    [Fact]
    public void RuleExpression_GroupedExpression()
    {
        var result = Parse("precept Test\nfield a as boolean\nfield b as boolean\nfield c as boolean\nrule (a or b) and c because \"grouped\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.And);
        bin.Left.Should().BeOfType<GroupedExpression>();
    }

    [Fact]
    public void RuleExpression_MemberAccess()
    {
        var result = Parse("precept Test\nfield items as set of string\nrule items.min == \"a\" because \"min\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<MemberAccessExpression>()
            .Which.MemberName.Should().Be("min");
    }

    [Fact]
    public void RuleExpression_ConditionalExpression()
    {
        var result = Parse("precept Test\nfield x as number\nrule if x > 0 then true else false because \"cond\"");
        var expr = GetRuleExpression(result);
        expr.Should().BeOfType<ConditionalExpression>();
    }

    [Fact]
    public void RuleExpression_UnaryNegate()
    {
        var result = Parse("precept Test\nfield x as number\nrule -x > 0 because \"neg\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Left.Should().BeOfType<UnaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.Minus);
    }

    [Fact]
    public void RuleExpression_ListLiteral()
    {
        var result = Parse("precept Test\nfield tags as set of string\nrule tags contains [\"a\", \"b\"] because \"list\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Right.Should().BeOfType<ListLiteralExpression>()
            .Which.Elements.Should().HaveCount(2);
    }

    [Fact]
    public void RuleExpression_StringLiteral()
    {
        var result = Parse("precept Test\nfield name as string\nrule name == \"hello\" because \"str\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Right.Should().BeOfType<LiteralExpression>()
            .Which.LiteralKind.Should().Be(TokenKind.StringLiteral);
    }

    [Fact]
    public void RuleExpression_NumberLiteral()
    {
        var result = Parse("precept Test\nfield x as number\nrule x == 42 because \"num\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Right.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("42");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  §2. Guard Clause Expressions
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GuardClause_SimpleComparison()
    {
        var result = Parse("precept Test\nfield x as number\nstate A, B\nevent Submit\nfrom A on Submit when x > 0 -> transition B");
        var expr = GetGuardExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.GreaterThan);
    }

    [Fact]
    public void GuardClause_ComplexLogical()
    {
        var result = Parse("precept Test\nfield x as number\nfield y as boolean\nstate A, B\nevent Submit\nfrom A on Submit when x > 0 and y -> transition B");
        var expr = GetGuardExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.And);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  §3. Compute Expressions
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputeExpression_ArithmeticBinary()
    {
        var result = Parse("precept Test\nfield x as number\nfield y as number\nfield total as number -> x + y");
        var expr = GetComputeExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.Plus);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("x");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("y");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  §4. Ensure Clause Expressions
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EnsureClause_SimpleComparison()
    {
        var result = Parse("precept Test\nfield x as number\nstate Active\nin Active ensure x > 0 because \"positive\"");
        var expr = GetEnsureExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.GreaterThan);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  §5. Outcome Expressions
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Outcome_Transition()
    {
        var result = Parse("precept Test\nstate A, B\nevent Submit\nfrom A on Submit -> transition B");
        var expr = GetOutcomeExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.Transition);
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Should().Be("B");
    }

    [Fact]
    public void Outcome_NoTransition()
    {
        var result = Parse("precept Test\nstate A\nevent Ping\nfrom A on Ping -> no transition");
        var expr = GetOutcomeExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.No);
    }

    [Fact]
    public void Outcome_Reject()
    {
        var result = Parse("precept Test\nstate A\nevent Bad\nfrom A on Bad -> reject \"not allowed\"");
        var expr = GetOutcomeExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.Reject);
        bin.Right.Should().BeOfType<LiteralExpression>().Which.Text.Should().Be("not allowed");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  §6. No Placeholders Remain
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NoPlaceholders_RuleExpressionIsReal()
    {
        // A non-trivial rule should NOT produce LiteralExpression(True, "true")
        var result = Parse("precept Test\nfield x as number\nrule x > 0 because \"pos\"");
        var expr = GetRuleExpression(result);
        // Should be a BinaryOperationExpression, not a placeholder
        expr.Should().NotBeEquivalentTo(
            new LiteralExpression(TokenKind.True, "true", expr.Span));
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  §7. Edge Cases
    // ═══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expression_CaseInsensitiveEquals()
    {
        var result = Parse("precept Test\nfield name as string\nrule name ~= \"test\" because \"ci\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.CaseInsensitiveEquals);
    }

    [Fact]
    public void Expression_ContainsOperator()
    {
        var result = Parse("precept Test\nfield tags as set of string\nrule tags contains \"important\" because \"has tag\"");
        var expr = GetRuleExpression(result);
        var bin = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        bin.Operator.Should().Be(TokenKind.Contains);
    }

    [Fact]
    public void Expression_ArithmeticPrecedence()
    {
        // "a + b * c" should parse as "a + (b * c)"
        var result = Parse("precept Test\nfield a as number\nfield b as number\nfield c as number\nrule a + b * c > 0 because \"arith\"");
        var expr = GetRuleExpression(result);
        var outer = expr.Should().BeOfType<BinaryOperationExpression>().Subject;
        outer.Operator.Should().Be(TokenKind.GreaterThan);
        var addExpr = outer.Left.Should().BeOfType<BinaryOperationExpression>().Subject;
        addExpr.Operator.Should().Be(TokenKind.Plus);
        addExpr.Right.Should().BeOfType<BinaryOperationExpression>()
            .Which.Operator.Should().Be(TokenKind.Star);
    }
}
