using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Soup Nazi tests for top-level dispatch and full declaration parsing (Slice 3.3).
/// </summary>
public class ParserTests
{
    private static SyntaxTree Parse(string source)
    {
        var tokens = Lexer.Lex(source);
        return Parser.Parse(tokens);
    }

    // ── Multi-declaration integration ──────────────────────────────────────

    [Fact]
    public void Parse_MultipleDeclarations_ProducesCorrectNodeCount()
    {
        var tree = Parse("""
            precept X
            field amount as decimal
            state Draft initial, Submitted
            event Submit
            rule amount > 0 because "msg"
            """);

        tree.Header.Should().NotBeNull();
        tree.Header!.Name.Text.Should().Be("X");
        tree.Declarations.Should().HaveCount(4); // field, state, event, rule
    }

    // ── FieldDeclaration ───────────────────────────────────────────────────

    [Fact]
    public void Parse_FieldDeclaration_Simple()
    {
        var tree = Parse("field amount as decimal");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Names.Should().HaveCount(1);
        field.Names[0].Text.Should().Be("amount");
        field.Type.Should().BeOfType<ScalarTypeRefNode>();
    }

    [Fact]
    public void Parse_FieldDeclaration_MultipleNames()
    {
        var tree = Parse("field name, description as string");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Names.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FieldDeclaration_WithModifiers()
    {
        var tree = Parse("field amount as money nonnegative positive");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Modifiers.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FieldDeclaration_WithComputedExpr()
    {
        var tree = Parse("field total as money -> principal + interest");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.ComputedExpression.Should().NotBeNull();
        field.ComputedExpression.Should().BeOfType<BinaryExpression>();
    }

    // ── StateDeclaration ───────────────────────────────────────────────────

    [Fact]
    public void Parse_StateDeclaration_Simple()
    {
        var tree = Parse("state Draft initial");
        var state = tree.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        state.Entries.Should().HaveCount(1);
        state.Entries[0].Name.Text.Should().Be("Draft");
        state.Entries[0].Modifiers.Should().Contain(t => t.Kind == TokenKind.Initial);
    }

    [Fact]
    public void Parse_StateDeclaration_MultipleStates()
    {
        var tree = Parse("state Draft initial, Submitted, Approved terminal success");
        var state = tree.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        state.Entries.Should().HaveCount(3);
        state.Entries[0].Name.Text.Should().Be("Draft");
        state.Entries[1].Name.Text.Should().Be("Submitted");
        state.Entries[2].Name.Text.Should().Be("Approved");
        state.Entries[2].Modifiers.Should().HaveCount(2);
    }

    // ── EventDeclaration ───────────────────────────────────────────────────

    [Fact]
    public void Parse_EventDeclaration_Simple()
    {
        var tree = Parse("event Submit");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Names.Should().HaveCount(1);
        evt.Names[0].Text.Should().Be("Submit");
        evt.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EventDeclaration_WithArgs()
    {
        var tree = Parse("event Approve(Amount as decimal)");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Arguments.Should().HaveCount(1);
        evt.Arguments[0].Name.Text.Should().Be("Amount");
    }

    [Fact]
    public void Parse_EventDeclaration_Initial()
    {
        var tree = Parse("event Create initial");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.IsInitial.Should().BeTrue();
    }

    // ── RuleDeclaration ────────────────────────────────────────────────────

    [Fact]
    public void Parse_RuleDeclaration_Simple()
    {
        var tree = Parse("rule amount > 0 because \"must be positive\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Condition.Should().BeOfType<BinaryExpression>();
        rule.Guard.Should().BeNull();
        rule.Message.Should().BeOfType<LiteralExpression>();
    }

    [Fact]
    public void Parse_RuleDeclaration_WithGuard()
    {
        var tree = Parse("rule amount > 0 when Active because \"msg\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Guard.Should().NotBeNull();
        rule.Guard.Should().BeOfType<IdentifierExpression>();
    }

    // ── PreceptHeader ──────────────────────────────────────────────────────

    [Fact]
    public void Parse_PreceptHeader_Valid()
    {
        var tree = Parse("precept InsuranceClaim");
        tree.Header.Should().NotBeNull();
        tree.Header!.Name.Text.Should().Be("InsuranceClaim");
    }

    // ── Error recovery ─────────────────────────────────────────────────────

    [Fact]
    public void Parse_UnknownLeadingToken_ProducesDiagnosticAndSyncs()
    {
        var tree = Parse("badtoken\nfield amount as decimal");
        // "badtoken" lexes as an Identifier, which is not a construct leading token,
        // so it produces a diagnostic and syncs to the next declaration.
        tree.Diagnostics.Should().NotBeEmpty();
        tree.Declarations.Should().HaveCount(1);
        tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>();
    }

    // ── Diagnostics are clean for well-formed input ────────────────────────

    [Fact]
    public void Parse_WellFormedInput_NoDiagnostics()
    {
        var tree = Parse("""
            precept Loan
            field amount as decimal nonnegative
            state Draft initial, Approved terminal
            event Submit
            rule amount > 0 because "Amount must be positive"
            """);
        tree.Diagnostics.Should().BeEmpty();
    }
}
