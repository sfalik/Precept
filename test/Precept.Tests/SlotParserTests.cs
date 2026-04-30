using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Soup Nazi tests for individual slot parsers (Slice 3.2).
/// Tests use the full Parser.Parse pipeline on minimal input.
/// </summary>
public class SlotParserTests
{
    private static SyntaxTree Parse(string source)
    {
        var tokens = Lexer.Lex(source);
        return Parser.Parse(tokens);
    }

    // ── IdentifierList ─────────────────────────────────────────────────────

    [Fact]
    public void ParseIdentifierList_SingleIdentifier()
    {
        var tree = Parse("field amount as decimal");
        tree.Declarations.Should().HaveCount(1);
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Names.Should().HaveCount(1);
        field.Names[0].Text.Should().Be("amount");
    }

    [Fact]
    public void ParseIdentifierList_MultipleCommaSeparated()
    {
        var tree = Parse("field name, description as string");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Names.Should().HaveCount(2);
        field.Names[0].Text.Should().Be("name");
        field.Names[1].Text.Should().Be("description");
    }

    [Fact]
    public void ParseIdentifierList_TrailingComma_ProducesDiagnostic()
    {
        var tree = Parse("field amount, as decimal");
        tree.Diagnostics.Should().NotBeEmpty("trailing comma before 'as' should produce a diagnostic");
    }

    // ── TypeExpression ─────────────────────────────────────────────────────

    [Fact]
    public void ParseTypeExpression_SimpleType()
    {
        var tree = Parse("field amount as string");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Type.Should().BeOfType<ScalarTypeRefNode>()
            .Which.TypeName.Kind.Should().Be(TokenKind.StringType);
    }

    [Fact]
    public void ParseTypeExpression_CollectionType()
    {
        var tree = Parse("field tags as set of string");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Type.Should().BeOfType<CollectionTypeRefNode>()
            .Which.ElementType.Kind.Should().Be(TokenKind.StringType);
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_StringElement()
    {
        var tree = Parse("field status as choice of string(\"A\", \"B\")");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var choice = field.Type.Should().BeOfType<ChoiceTypeRefNode>().Subject;
        choice.ElementType!.Value.Kind.Should().Be(TokenKind.StringType);
        choice.Options.Should().HaveCount(2);
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_IntegerElement()
    {
        var tree = Parse("field code as choice of integer(0, 404, 500)");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var choice = field.Type.Should().BeOfType<ChoiceTypeRefNode>().Subject;
        choice.ElementType!.Value.Kind.Should().Be(TokenKind.IntegerType);
        choice.Options.Should().HaveCount(3);
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_DecimalElement()
    {
        var tree = Parse("field rate as choice of decimal(0.0, 0.05)");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var choice = field.Type.Should().BeOfType<ChoiceTypeRefNode>().Subject;
        choice.ElementType!.Value.Kind.Should().Be(TokenKind.DecimalType);
        choice.Options.Should().HaveCount(2);
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_NumberElement()
    {
        var tree = Parse("field score as choice of number(1.5, 2.5)");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var choice = field.Type.Should().BeOfType<ChoiceTypeRefNode>().Subject;
        choice.ElementType!.Value.Kind.Should().Be(TokenKind.NumberType);
        choice.Options.Should().HaveCount(2);
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_BooleanElement()
    {
        var tree = Parse("field flag as choice of boolean(true, false)");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var choice = field.Type.Should().BeOfType<ChoiceTypeRefNode>().Subject;
        choice.ElementType!.Value.Kind.Should().Be(TokenKind.BooleanType);
        choice.Options.Should().HaveCount(2);
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_NegativeNumericValues()
    {
        var tree = Parse("field temp as choice of integer(-10, 0, 10)");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var choice = field.Type.Should().BeOfType<ChoiceTypeRefNode>().Subject;
        choice.Options.Should().HaveCount(3);
        choice.Options[0].Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("-10");
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_BareChoice_ProducesDiagnostic()
    {
        // choice(...) without 'of T' must produce ChoiceMissingElementType
        var tree = Parse("field status as choice(\"A\", \"B\")");
        tree.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ChoiceMissingElementType));
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_UnknownElementType_ProducesDiagnostic()
    {
        var tree = Parse("field status as choice of uuid(\"A\")");
        tree.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseTypeExpression_ChoiceType_WrongLiteralKind_ProducesDiagnostic()
    {
        // integer element type but string literal value
        var tree = Parse("field code as choice of integer(\"not-an-int\")");
        tree.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ChoiceElementTypeMismatch));
    }

    [Fact]
    public void ParseTypeExpression_MissingAs_ProducesDiagnostic()
    {
        var tree = Parse("field amount decimal");
        tree.Diagnostics.Should().NotBeEmpty("missing 'as' keyword should produce a diagnostic");
    }

    // ── ModifierList ───────────────────────────────────────────────────────

    [Fact]
    public void ParseModifierList_SingleModifier()
    {
        var tree = Parse("field amount as decimal nonnegative");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Modifiers.Should().HaveCount(1);
        field.Modifiers[0].Should().BeOfType<FlagModifierNode>()
            .Which.Keyword.Kind.Should().Be(TokenKind.Nonnegative);
    }

    [Fact]
    public void ParseModifierList_MultipleModifiers()
    {
        var tree = Parse("field amount as decimal nonnegative positive");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Modifiers.Should().HaveCount(2);
    }

    [Fact]
    public void ParseModifierList_NoModifiers_ReturnsNull()
    {
        var tree = Parse("field amount as decimal");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Modifiers.Should().BeEmpty();
    }

    // ── StateModifierList ──────────────────────────────────────────────────

    [Fact]
    public void ParseStateModifierList_Terminal()
    {
        var tree = Parse("state Approved terminal");
        var state = tree.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        state.Entries.Should().HaveCount(1);
        state.Entries[0].Modifiers.Should().Contain(t => t.Kind == TokenKind.Terminal);
    }

    [Fact]
    public void ParseStateModifierList_Initial()
    {
        var tree = Parse("state Draft initial");
        var state = tree.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        state.Entries[0].Modifiers.Should().Contain(t => t.Kind == TokenKind.Initial);
    }

    [Fact]
    public void ParseStateModifierList_NoModifiers_ReturnsNull()
    {
        var tree = Parse("state Draft");
        var state = tree.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        state.Entries[0].Modifiers.Should().BeEmpty();
    }

    // ── ArgumentList ───────────────────────────────────────────────────────

    [Fact]
    public void ParseArgumentList_SingleArg()
    {
        var tree = Parse("event Approve(Amount as decimal)");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Arguments.Should().HaveCount(1);
        evt.Arguments[0].Name.Text.Should().Be("Amount");
    }

    [Fact]
    public void ParseArgumentList_MultipleArgs()
    {
        var tree = Parse("event Submit(Name as string, Amount as decimal)");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Arguments.Should().HaveCount(2);
    }

    [Fact]
    public void ParseArgumentList_MissingAs_ProducesDiagnostic()
    {
        var tree = Parse("event Submit(Name decimal)");
        tree.Diagnostics.Should().NotBeEmpty("missing 'as' should produce a diagnostic");
    }

    [Fact]
    public void ParseArgumentList_NoWith_ReturnsEmpty()
    {
        var tree = Parse("event Submit");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Arguments.Should().BeEmpty();
    }

    // ── ComputeExpression ──────────────────────────────────────────────────

    [Fact]
    public void ParseComputeExpression_SimpleExpression()
    {
        var tree = Parse("field total as decimal -> principal + interest");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.ComputedExpression.Should().NotBeNull();
        field.ComputedExpression.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.Plus);
    }

    [Fact]
    public void ParseComputeExpression_NoArrow_ReturnsNull()
    {
        var tree = Parse("field amount as decimal");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.ComputedExpression.Should().BeNull();
    }

    // ── RuleExpression ─────────────────────────────────────────────────────

    [Fact]
    public void ParseRuleExpression_DirectExpression()
    {
        var tree = Parse("rule amount > 0 because \"must be positive\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Condition.Should().BeOfType<BinaryExpression>()
            .Which.Operator.Kind.Should().Be(TokenKind.GreaterThan);
    }

    // ── GuardClause ────────────────────────────────────────────────────────

    [Fact]
    public void ParseGuardClause_WhenPresent()
    {
        var tree = Parse("rule amount > 0 when Active because \"msg\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Guard.Should().NotBeNull();
        rule.Guard.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Text.Should().Be("Active");
    }

    [Fact]
    public void ParseGuardClause_WhenAbsent_ReturnsNull()
    {
        var tree = Parse("rule amount > 0 because \"msg\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Guard.Should().BeNull();
    }

    // ── BecauseClause ──────────────────────────────────────────────────────

    [Fact]
    public void ParseBecauseClause_StringLiteral()
    {
        var tree = Parse("rule amount > 0 because \"must be positive\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Message.Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("must be positive");
    }

    [Fact]
    public void ParseBecauseClause_MissingBecause_ProducesDiagnostic()
    {
        var tree = Parse("rule amount > 0");
        tree.Diagnostics.Should().NotBeEmpty("missing 'because' should produce a diagnostic");
    }
}
