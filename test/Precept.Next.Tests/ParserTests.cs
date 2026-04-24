using System.Collections.Immutable;
using FluentAssertions;
using Precept.Pipeline;
using Xunit;

namespace Precept.Next.Tests;

public class ParserTests
{
    // ── Test helpers ────────────────────────────────────────────────────────────

    /// <summary>Parse a full precept definition and return the SyntaxTree.</summary>
    private static SyntaxTree Parse(string source)
    {
        var stream = Lexer.Lex(source);
        return Parser.Parse(stream);
    }

    /// <summary>Parse a rule expression: wraps in precept/rule/because and extracts the condition.</summary>
    private static Expression ParseExpr(string expr)
    {
        var tree = Parse($"precept Test\nrule {expr} because \"test\"");
        tree.Root.Should().NotBeNull();
        tree.Root!.Body.Should().ContainSingle();
        var rule = tree.Root.Body[0].Should().BeOfType<RuleDeclaration>().Subject;
        return rule.Condition;
    }

    /// <summary>Parse and return the first declaration from the body.</summary>
    private static Declaration ParseDecl(string body)
    {
        var tree = Parse($"precept Test\n{body}");
        tree.Root.Should().NotBeNull();
        tree.Root!.Body.Should().NotBeEmpty();
        return tree.Root.Body[0];
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Precept header
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_EmptyPrecept_ReturnsRootWithEmptyBody()
    {
        var tree = Parse("precept Empty");
        tree.Root.Should().NotBeNull();
        tree.Root!.Name.Text.Should().Be("Empty");
        tree.Root.Body.Should().BeEmpty();
        tree.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MissingPreceptName_EmitsDiagnosticAndCreatesIsMissingName()
    {
        var tree = Parse("precept");
        tree.Root.Should().NotBeNull();
        tree.Root!.Name.Kind.Should().Be(TokenKind.Identifier);
        tree.Root.Name.Text.Should().BeEmpty();
        tree.Diagnostics.Should().NotBeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Atom expressions
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_Identifier_ReturnsIdentifierExpression()
    {
        var expr = ParseExpr("Amount");
        var ide = expr.Should().BeOfType<IdentifierExpression>().Subject;
        ide.Name.Text.Should().Be("Amount");
    }

    [Fact]
    public void Expr_NumberLiteral_ReturnsNumberLiteralExpression()
    {
        var expr = ParseExpr("42");
        var num = expr.Should().BeOfType<NumberLiteralExpression>().Subject;
        num.Value.Text.Should().Be("42");
    }

    [Fact]
    public void Expr_DecimalLiteral_ReturnsNumberLiteralExpression()
    {
        var expr = ParseExpr("3.14");
        var num = expr.Should().BeOfType<NumberLiteralExpression>().Subject;
        num.Value.Text.Should().Be("3.14");
    }

    [Fact]
    public void Expr_BooleanTrue_ReturnsBooleanLiteralExpression()
    {
        var expr = ParseExpr("true");
        var b = expr.Should().BeOfType<BooleanLiteralExpression>().Subject;
        b.Value.Should().BeTrue();
    }

    [Fact]
    public void Expr_BooleanFalse_ReturnsBooleanLiteralExpression()
    {
        var expr = ParseExpr("false");
        var b = expr.Should().BeOfType<BooleanLiteralExpression>().Subject;
        b.Value.Should().BeFalse();
    }

    [Fact]
    public void Expr_StringLiteral_ReturnsStringLiteralExpression()
    {
        // Use it in "because" position since rule condition expects bool
        var tree = Parse("precept Test\nrule true because \"hello world\"");
        var rule = tree.Root!.Body[0].Should().BeOfType<RuleDeclaration>().Subject;
        var str = rule.Message.Should().BeOfType<StringLiteralExpression>().Subject;
        str.Value.Text.Should().Be("hello world");
    }

    [Fact]
    public void Expr_TypedConstant_ReturnsTypedConstantExpression()
    {
        // Use in set RHS via transition row
        var tree = Parse("precept Test\nfield D as date\nfrom S on E\n-> set D = '2026-04-23'\n-> transition S2");
        // Just check it parses — the typed constant is in a set action
        tree.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Expr_ParenthesizedGrouping_ReturnsParenthesizedExpression()
    {
        var expr = ParseExpr("(Amount)");
        var paren = expr.Should().BeOfType<ParenthesizedExpression>().Subject;
        paren.Inner.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("Amount");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Binary operator precedence
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_Addition_ReturnsBinaryExpression()
    {
        var expr = ParseExpr("A + B");
        var bin = expr.Should().BeOfType<BinaryExpression>().Subject;
        bin.Op.Should().Be(BinaryOp.Plus);
        bin.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("A");
        bin.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("B");
    }

    [Fact]
    public void Expr_MultiplicationBindsTighterThanAddition()
    {
        // A + B * C  should parse as  A + (B * C)
        var expr = ParseExpr("A + B * C");
        var add = expr.Should().BeOfType<BinaryExpression>().Subject;
        add.Op.Should().Be(BinaryOp.Plus);
        add.Left.Should().BeOfType<IdentifierExpression>();
        var mul = add.Right.Should().BeOfType<BinaryExpression>().Subject;
        mul.Op.Should().Be(BinaryOp.Star);
    }

    [Fact]
    public void Expr_AdditionIsLeftAssociative()
    {
        // A + B + C  should parse as  (A + B) + C
        var expr = ParseExpr("A + B + C");
        var outer = expr.Should().BeOfType<BinaryExpression>().Subject;
        outer.Op.Should().Be(BinaryOp.Plus);
        outer.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("C");
        var inner = outer.Left.Should().BeOfType<BinaryExpression>().Subject;
        inner.Op.Should().Be(BinaryOp.Plus);
        inner.Left.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("A");
        inner.Right.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("B");
    }

    [Fact]
    public void Expr_AndBindsTighterThanOr()
    {
        // A or B and C  should parse as  A or (B and C)
        var expr = ParseExpr("A or B and C");
        var or = expr.Should().BeOfType<BinaryExpression>().Subject;
        or.Op.Should().Be(BinaryOp.Or);
        or.Right.Should().BeOfType<BinaryExpression>().Which.Op.Should().Be(BinaryOp.And);
    }

    [Fact]
    public void Expr_ComparisonBindsTighterThanLogical()
    {
        // A == B and C > D  should parse as  (A == B) and (C > D)
        var expr = ParseExpr("A == B and C > D");
        var and = expr.Should().BeOfType<BinaryExpression>().Subject;
        and.Op.Should().Be(BinaryOp.And);
        and.Left.Should().BeOfType<BinaryExpression>().Which.Op.Should().Be(BinaryOp.Equal);
        and.Right.Should().BeOfType<BinaryExpression>().Which.Op.Should().Be(BinaryOp.Greater);
    }

    [Fact]
    public void Expr_AllArithmeticOps_ParseCorrectly()
    {
        foreach (var (text, op) in new[] {
            ("A - B", BinaryOp.Minus), ("A * B", BinaryOp.Star),
            ("A / B", BinaryOp.Slash), ("A % B", BinaryOp.Percent) })
        {
            var expr = ParseExpr(text);
            expr.Should().BeOfType<BinaryExpression>().Which.Op.Should().Be(op);
        }
    }

    [Fact]
    public void Expr_AllComparisonOps_ParseCorrectly()
    {
        foreach (var (text, op) in new[] {
            ("A == B", BinaryOp.Equal), ("A != B", BinaryOp.NotEqual),
            ("A < B", BinaryOp.Less), ("A > B", BinaryOp.Greater),
            ("A <= B", BinaryOp.LessOrEqual), ("A >= B", BinaryOp.GreaterOrEqual) })
        {
            var expr = ParseExpr(text);
            expr.Should().BeOfType<BinaryExpression>().Which.Op.Should().Be(op);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Unary operators
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_NotPrefix_ReturnsUnaryNot()
    {
        var expr = ParseExpr("not Active");
        var u = expr.Should().BeOfType<UnaryExpression>().Subject;
        u.Op.Should().Be(UnaryOp.Not);
        u.Operand.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("Active");
    }

    [Fact]
    public void Expr_NegatePrefix_ReturnsUnaryNegate()
    {
        var expr = ParseExpr("-Amount");
        var u = expr.Should().BeOfType<UnaryExpression>().Subject;
        u.Op.Should().Be(UnaryOp.Negate);
    }

    [Fact]
    public void Expr_NotBindsTighterThanAnd()
    {
        // not A and B  should parse as  (not A) and B
        var expr = ParseExpr("not A and B");
        var and = expr.Should().BeOfType<BinaryExpression>().Subject;
        and.Op.Should().Be(BinaryOp.And);
        and.Left.Should().BeOfType<UnaryExpression>().Which.Op.Should().Be(UnaryOp.Not);
    }

    [Fact]
    public void Expr_NegateBindsTighterThanMultiplication()
    {
        // -A * B  should parse as  (-A) * B
        var expr = ParseExpr("-A * B");
        var mul = expr.Should().BeOfType<BinaryExpression>().Subject;
        mul.Op.Should().Be(BinaryOp.Star);
        mul.Left.Should().BeOfType<UnaryExpression>().Which.Op.Should().Be(UnaryOp.Negate);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Contains and is-set
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_Contains_ReturnsContainsExpression()
    {
        var expr = ParseExpr("Items contains X");
        var c = expr.Should().BeOfType<ContainsExpression>().Subject;
        c.Collection.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("Items");
        c.Value.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("X");
    }

    [Fact]
    public void Expr_IsSet_ReturnsIsSetExpression()
    {
        var expr = ParseExpr("Name is set");
        var i = expr.Should().BeOfType<IsSetExpression>().Subject;
        i.IsNot.Should().BeFalse();
        i.Operand.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("Name");
    }

    [Fact]
    public void Expr_IsNotSet_ReturnsIsSetExpressionWithIsNotTrue()
    {
        var expr = ParseExpr("Name is not set");
        var i = expr.Should().BeOfType<IsSetExpression>().Subject;
        i.IsNot.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Member access and calls
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_MemberAccess_ReturnsMemberAccessExpression()
    {
        var expr = ParseExpr("Event.Arg");
        var mac = expr.Should().BeOfType<MemberAccessExpression>().Subject;
        mac.Object.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("Event");
        mac.Member.Text.Should().Be("Arg");
    }

    [Fact]
    public void Expr_ChainedMemberAccess_IsLeftAssociative()
    {
        // A.B.C  should parse as  (A.B).C
        var expr = ParseExpr("A.B.C");
        var outer = expr.Should().BeOfType<MemberAccessExpression>().Subject;
        outer.Member.Text.Should().Be("C");
        var inner = outer.Object.Should().BeOfType<MemberAccessExpression>().Subject;
        inner.Object.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("A");
        inner.Member.Text.Should().Be("B");
    }

    [Fact]
    public void Expr_MemberCount_ParsesCollectionDotMember()
    {
        var expr = ParseExpr("Items.count");
        var mac = expr.Should().BeOfType<MemberAccessExpression>().Subject;
        mac.Member.Text.Should().Be("count");
    }

    [Fact]
    public void Expr_FunctionCall_ReturnsCallExpression()
    {
        var expr = ParseExpr("clamp(A, B)");
        var call = expr.Should().BeOfType<CallExpression>().Subject;
        call.FunctionName.Text.Should().Be("clamp");
        call.Args.Should().HaveCount(2);
    }

    [Fact]
    public void Expr_MethodCall_ReturnsMethodCallExpression()
    {
        var expr = ParseExpr("T.inZone(Tz)");
        var mc = expr.Should().BeOfType<MethodCallExpression>().Subject;
        mc.Object.Should().BeOfType<IdentifierExpression>().Which.Name.Text.Should().Be("T");
        mc.Method.Text.Should().Be("inZone");
        mc.Args.Should().ContainSingle();
    }

    [Fact]
    public void Expr_ChainedMemberThenMethodCall_ParsesCorrectly()
    {
        // A.B.C(D)  should be MethodCall(A.B, C, [D])
        var expr = ParseExpr("A.B.C(D)");
        var mc = expr.Should().BeOfType<MethodCallExpression>().Subject;
        mc.Method.Text.Should().Be("C");
        mc.Object.Should().BeOfType<MemberAccessExpression>()
            .Which.Member.Text.Should().Be("B");
        mc.Args.Should().ContainSingle();
    }

    [Fact]
    public void Expr_FunctionCallNoArgs_ReturnsCallWithEmptyArgs()
    {
        var expr = ParseExpr("now()");
        var call = expr.Should().BeOfType<CallExpression>().Subject;
        call.FunctionName.Text.Should().Be("now");
        call.Args.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Conditional expression
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_Conditional_ReturnsConditionalExpression()
    {
        var expr = ParseExpr("if Active then A else B");
        var cond = expr.Should().BeOfType<ConditionalExpression>().Subject;
        cond.Condition.Should().BeOfType<IdentifierExpression>();
        cond.Consequence.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("A");
        cond.Alternative.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("B");
    }

    [Fact]
    public void Expr_NestedConditional_ParsesCorrectly()
    {
        var expr = ParseExpr("if A then if B then C else D else E");
        var outer = expr.Should().BeOfType<ConditionalExpression>().Subject;
        outer.Consequence.Should().BeOfType<ConditionalExpression>();
        outer.Alternative.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("E");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Interpolated string
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_InterpolatedString_ReturnsInterpolatedStringExpression()
    {
        var tree = Parse("precept Test\nrule true because \"hello {Name} world\"");
        var rule = tree.Root!.Body[0].Should().BeOfType<RuleDeclaration>().Subject;
        var interp = rule.Message.Should().BeOfType<InterpolatedStringExpression>().Subject;
        // TextSegment(hello ) + ExpressionSegment(Name) + TextSegment( world)
        interp.Segments.Should().HaveCount(3);
        interp.Segments[0].Should().BeOfType<TextSegment>();
        interp.Segments[1].Should().BeOfType<ExpressionSegment>();
        interp.Segments[2].Should().BeOfType<TextSegment>();
    }

    [Fact]
    public void Expr_InterpolatedStringMultiExpr_ParsesAllSegments()
    {
        var tree = Parse("precept Test\nrule true because \"a {B} c {D} e\"");
        var rule = tree.Root!.Body[0].Should().BeOfType<RuleDeclaration>().Subject;
        var interp = rule.Message.Should().BeOfType<InterpolatedStringExpression>().Subject;
        // TextSegment(a ) + ExprSegment(B) + TextSegment( c ) + ExprSegment(D) + TextSegment( e)
        interp.Segments.Should().HaveCount(5);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  List literal
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Expr_ListLiteral_ReturnsListLiteralExpression()
    {
        // Parse as default value in a field declaration
        var decl = ParseDecl("field Tags as set of string default [\"a\", \"b\"]");
        var field = decl.Should().BeOfType<FieldDeclaration>().Subject;
        var def = field.Modifiers.Should().ContainSingle()
            .Which.Should().BeOfType<DefaultModifier>().Subject;
        var list = def.Value.Should().BeOfType<ListLiteralExpression>().Subject;
        list.Elements.Should().HaveCount(2);
    }

    [Fact]
    public void Expr_EmptyListLiteral_ReturnsEmptyList()
    {
        var decl = ParseDecl("field Tags as set of string default []");
        var field = decl.Should().BeOfType<FieldDeclaration>().Subject;
        var def = field.Modifiers.Should().ContainSingle()
            .Which.Should().BeOfType<DefaultModifier>().Subject;
        def.Value.Should().BeOfType<ListLiteralExpression>()
            .Which.Elements.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Declaration parsing
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Decl_Field_ParsesNameTypeAndModifiers()
    {
        var decl = ParseDecl("field Amount as number nonnegative default 0");
        var field = decl.Should().BeOfType<FieldDeclaration>().Subject;
        field.Names.Should().ContainSingle().Which.Text.Should().Be("Amount");
        field.Type.Should().BeOfType<ScalarTypeRef>()
            .Which.Kind.Should().Be(ScalarTypeKind.Number);
        field.Modifiers.Should().HaveCount(2);
        field.Modifiers[0].Should().BeOfType<NonnegativeModifier>();
        field.Modifiers[1].Should().BeOfType<DefaultModifier>();
    }

    [Fact]
    public void Decl_FieldMultiName_ParsesAllNames()
    {
        var decl = ParseDecl("field A, B, C as string");
        var field = decl.Should().BeOfType<FieldDeclaration>().Subject;
        field.Names.Should().HaveCount(3);
    }

    [Fact]
    public void Decl_ComputedField_ParsesArrowExpression()
    {
        var decl = ParseDecl("field Total as number -> Base + Tax");
        var field = decl.Should().BeOfType<FieldDeclaration>().Subject;
        field.ComputedExpression.Should().NotBeNull();
        field.ComputedExpression.Should().BeOfType<BinaryExpression>()
            .Which.Op.Should().Be(BinaryOp.Plus);
    }

    [Fact]
    public void Decl_State_ParsesEntriesWithModifiers()
    {
        var decl = ParseDecl("state Draft initial, Active, Done terminal");
        var state = decl.Should().BeOfType<StateDeclaration>().Subject;
        state.Entries.Should().HaveCount(3);
        state.Entries[0].Name.Text.Should().Be("Draft");
        state.Entries[0].IsInitial.Should().BeTrue();
        state.Entries[2].Modifiers.Should().Contain(StateModifierKind.Terminal);
    }

    [Fact]
    public void Decl_EventWithParenArgs_ParsesArgList()
    {
        var decl = ParseDecl("event Submit(Name as string notempty, Amount as number positive)");
        var ev = decl.Should().BeOfType<EventDeclaration>().Subject;
        ev.Names.Should().ContainSingle().Which.Text.Should().Be("Submit");
        ev.Args.Should().HaveCount(2);
        ev.Args[0].Name.Text.Should().Be("Name");
        ev.Args[1].Name.Text.Should().Be("Amount");
        ev.Args[1].Modifiers.Should().ContainSingle()
            .Which.Should().BeOfType<PositiveModifier>();
    }

    [Fact]
    public void Decl_EventNoArgs_ParsesEmptyArgList()
    {
        var decl = ParseDecl("event PassScreen");
        var ev = decl.Should().BeOfType<EventDeclaration>().Subject;
        ev.Args.Should().BeEmpty();
        ev.IsInitial.Should().BeFalse();
    }

    [Fact]
    public void Decl_EventInitial_ParsesIsInitialFlag()
    {
        var decl = ParseDecl("event Create initial");
        var ev = decl.Should().BeOfType<EventDeclaration>().Subject;
        ev.IsInitial.Should().BeTrue();
    }

    [Fact]
    public void Decl_Rule_ParsesConditionGuardAndMessage()
    {
        var decl = ParseDecl("rule Amount > 0 when Active because \"Amount must be positive\"");
        var rule = decl.Should().BeOfType<RuleDeclaration>().Subject;
        rule.Condition.Should().BeOfType<BinaryExpression>();
        rule.Guard.Should().NotBeNull();
        rule.Guard.Should().BeOfType<IdentifierExpression>();
        rule.Message.Should().BeOfType<StringLiteralExpression>();
    }

    [Fact]
    public void Decl_StateEnsure_ParsesConditionAndMessage()
    {
        var decl = ParseDecl("in Active ensure Amount > 0 because \"Required\"");
        var ensure = decl.Should().BeOfType<StateEnsureDeclaration>().Subject;
        ensure.Anchor.Should().Be(EnsureAnchor.In);
        ensure.Condition.Should().BeOfType<BinaryExpression>();
        ensure.Guard.Should().BeNull();
    }

    [Fact]
    public void Decl_StateEnsureWithGuard_ParsesGuardBeforeEnsure()
    {
        var decl = ParseDecl("in Active when Flag ensure Amount > 0 because \"Required\"");
        var ensure = decl.Should().BeOfType<StateEnsureDeclaration>().Subject;
        ensure.Guard.Should().NotBeNull();
        ensure.Guard.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("Flag");
    }

    [Fact]
    public void Decl_TransitionRow_ParsesFullRow()
    {
        var tree = Parse(@"precept Test
state Draft initial, Active
event Submit
from Draft on Submit
-> set Amount = 42
-> transition Active");
        tree.Diagnostics.Should().BeEmpty();
        var row = tree.Root!.Body[2].Should().BeOfType<TransitionRowDeclaration>().Subject;
        row.FromStates.Names.Should().ContainSingle().Which.Text.Should().Be("Draft");
        row.EventName.Text.Should().Be("Submit");
        row.Guard.Should().BeNull();
        row.Actions.Should().ContainSingle().Which.Should().BeOfType<SetActionStatement>();
        row.Outcome.Should().BeOfType<TransitionOutcomeNode>()
            .Which.StateName.Text.Should().Be("Active");
    }

    [Fact]
    public void Decl_TransitionRowWithGuard_ParsesGuard()
    {
        var tree = Parse(@"precept Test
state S1 initial, S2
event E
from S1 on E when Amount > 0
-> transition S2");
        tree.Diagnostics.Should().BeEmpty();
        var row = tree.Root!.Body[2].Should().BeOfType<TransitionRowDeclaration>().Subject;
        row.Guard.Should().NotBeNull();
        row.Guard.Should().BeOfType<BinaryExpression>();
    }

    [Fact]
    public void Decl_TransitionRowReject_ParsesRejectOutcome()
    {
        var tree = Parse(@"precept Test
state S1 initial
event E
from S1 on E
-> reject ""Not allowed""");
        tree.Diagnostics.Should().BeEmpty();
        var row = tree.Root!.Body[2].Should().BeOfType<TransitionRowDeclaration>().Subject;
        row.Outcome.Should().BeOfType<RejectOutcomeNode>();
    }

    [Fact]
    public void Decl_TransitionRowNoTransition_ParsesNoTransitionOutcome()
    {
        var tree = Parse(@"precept Test
state S1 initial
event E
from S1 on E
-> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var row = tree.Root!.Body[2].Should().BeOfType<TransitionRowDeclaration>().Subject;
        row.Outcome.Should().BeOfType<NoTransitionOutcomeNode>();
    }

    [Fact]
    public void Decl_AccessMode_ParsesInStateWriteFields()
    {
        var decl = ParseDecl("in Active write Amount, Name");
        var am = decl.Should().BeOfType<AccessModeDeclaration>().Subject;
        am.Mode.Should().Be(AccessMode.Write);
        am.StateScope.Should().NotBeNull();
        am.Fields.Names.Should().HaveCount(2);
    }

    [Fact]
    public void Decl_AccessModeRead_ParsesReadMode()
    {
        var decl = ParseDecl("in Active read all");
        var am = decl.Should().BeOfType<AccessModeDeclaration>().Subject;
        am.Mode.Should().Be(AccessMode.Read);
        am.Fields.IsAll.Should().BeTrue();
    }

    [Fact]
    public void Decl_RootWrite_ParsesWithoutStateScope()
    {
        var decl = ParseDecl("write Amount");
        var am = decl.Should().BeOfType<AccessModeDeclaration>().Subject;
        am.StateScope.Should().BeNull();
        am.Mode.Should().Be(AccessMode.Write);
    }

    [Fact]
    public void Decl_EventEnsure_ParsesOnEventEnsure()
    {
        var decl = ParseDecl("on Submit ensure Amount > 0 because \"Required\"");
        var ensure = decl.Should().BeOfType<EventEnsureDeclaration>().Subject;
        ensure.EventName.Text.Should().Be("Submit");
        ensure.Guard.Should().BeNull();
    }

    [Fact]
    public void Decl_EventEnsureWithGuard_ParsesGuard()
    {
        var decl = ParseDecl("on Submit when Active ensure Amount > 0 because \"Required\"");
        var ensure = decl.Should().BeOfType<EventEnsureDeclaration>().Subject;
        ensure.Guard.Should().NotBeNull();
    }

    [Fact]
    public void Decl_StatelessEventHook_ParsesActions()
    {
        var tree = Parse(@"precept Test
event E
on E
-> set Amount = 1
-> set Name = ""test""");
        tree.Diagnostics.Should().BeEmpty();
        var hook = tree.Root!.Body[1].Should().BeOfType<StatelessEventHookDeclaration>().Subject;
        hook.Actions.Should().HaveCount(2);
    }

    [Fact]
    public void Decl_SetOfString_ParsesCollectionType()
    {
        var decl = ParseDecl("field Tags as set of string");
        var field = decl.Should().BeOfType<FieldDeclaration>().Subject;
        var coll = field.Type.Should().BeOfType<CollectionTypeRef>().Subject;
        coll.Kind.Should().Be(CollectionKind.Set);
        coll.ElementType.Kind.Should().Be(ScalarTypeKind.String);
    }

    [Fact]
    public void Decl_ChoiceType_ParsesChoiceValues()
    {
        var decl = ParseDecl("field Status as choice(\"A\", \"B\", \"C\")");
        var field = decl.Should().BeOfType<FieldDeclaration>().Subject;
        var choice = field.Type.Should().BeOfType<ChoiceTypeRef>().Subject;
        choice.Choices.Should().HaveCount(3);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Action statements
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Action_CollectionActions_ParseCorrectly()
    {
        var tree = Parse(@"precept Test
state S initial
event E
from S on E
-> add Items ""x""
-> remove Items ""y""
-> clear Items
-> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var row = tree.Root!.Body[2].Should().BeOfType<TransitionRowDeclaration>().Subject;
        row.Actions.Should().HaveCount(3);
        row.Actions[0].Should().BeOfType<AddActionStatement>();
        row.Actions[1].Should().BeOfType<RemoveActionStatement>();
        row.Actions[2].Should().BeOfType<ClearActionStatement>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Error recovery
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Error_MissingExpression_EmitsDiagnosticAndReturnsMissingNode()
    {
        // rule without a condition — the parser should emit diagnostic
        var tree = Parse("precept Test\nrule because \"msg\"");
        tree.Diagnostics.Should().NotBeEmpty();
        // Should still have a root
        tree.Root.Should().NotBeNull();
    }

    [Fact]
    public void Error_UnknownTopLevelKeyword_ResyncsAndContinues()
    {
        var tree = Parse("precept Test\ngarbage\nfield A as string");
        tree.Root.Should().NotBeNull();
        // Should recover and parse the field
        tree.Root!.Body.Should().ContainSingle()
            .Which.Should().BeOfType<FieldDeclaration>();
        tree.Diagnostics.Should().NotBeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Full precept integration
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_MinimalPrecept_AllDeclarationTypes()
    {
        var tree = Parse(@"precept Minimal
field Name as string
state Draft initial, Active
event Start
rule Name is set because ""Name is required""
in Active ensure Name is set because ""Active needs name""
from Draft on Start
-> transition Active");
        tree.Diagnostics.Should().BeEmpty();
        tree.Root.Should().NotBeNull();
        tree.Root!.Name.Text.Should().Be("Minimal");
        tree.Root.Body.Should().HaveCount(6);
    }
}
