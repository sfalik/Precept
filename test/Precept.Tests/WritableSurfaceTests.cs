using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;
using Xunit.Abstractions;

namespace Precept.Tests;

/// <summary>
/// Compile-surface tests for the <c>writable</c> modifier feature.
/// Documents exact behavior given that Parser and TypeChecker are NotImplementedException stubs.
/// </summary>
public class WritableSurfaceTests(ITestOutputHelper output)
{
    // ── Case 1: field Amount as money writable ──────────────────────────────

    [Fact]
    public void Case1_WritableModifier_LexesCorrectly()
    {
        var src = "precept TestWritable\nfield Amount as money writable";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty();
        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        kinds.Should().ContainInOrder(
            TokenKind.Precept,
            TokenKind.Identifier,
            TokenKind.NewLine,
            TokenKind.Field,
            TokenKind.Identifier,
            TokenKind.As,
            TokenKind.MoneyType,
            TokenKind.Writable,
            TokenKind.EndOfSource);
    }

    [Fact]
    public void Case1_WritableModifier_CompileThrowsNotImplemented()
    {
        var src = "precept TestWritable\nfield Amount as money writable";
        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }

    // ── Case 2: field Amount as money (no writable) ─────────────────────────

    [Fact]
    public void Case2_ReadOnlyField_LexesCorrectly()
    {
        var src = "precept TestReadOnly\nfield Amount as money";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty();
        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        kinds.Should().ContainInOrder(
            TokenKind.Precept,
            TokenKind.Identifier,
            TokenKind.NewLine,
            TokenKind.Field,
            TokenKind.Identifier,
            TokenKind.As,
            TokenKind.MoneyType,
            TokenKind.EndOfSource);
        kinds.Should().NotContain(TokenKind.Writable);
    }

    [Fact]
    public void Case2_ReadOnlyField_CompileThrowsNotImplemented()
    {
        var src = "precept TestReadOnly\nfield Amount as money";
        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }

    // ── Case 3: in Draft modify Amount editable (new access mode vocab B4) ───

    [Fact]
    public void Case3_ModifyEditable_LexesCorrectly()
    {
        // write all retired in B4 (2026-04-28). New surface: in State modify Field editable.
        var src =
            "precept TestModify\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft modify Amount editable\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty();
        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        kinds.Should().ContainInOrder(TokenKind.In, TokenKind.Identifier, TokenKind.Modify, TokenKind.Identifier, TokenKind.Editable);
    }

    [Fact]
    public void Case3_ModifyEditable_CompileThrowsNotImplemented()
    {
        var src =
            "precept TestModify\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft modify Amount editable\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";
        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }

    // ── Case 4: in Draft modify Amount readonly (read-only access mode) ──────

    [Fact]
    public void Case4_InStateModifyReadonly_LexesCorrectly()
    {
        var src =
            "precept TestStateful\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft modify Amount readonly\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var stream = Lexer.Lex(src);
        stream.Diagnostics.Should().BeEmpty();

        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        // Key tokens: In, Identifier(Draft), Modify, Identifier(Amount), Readonly
        kinds.Should().Contain(TokenKind.In);
        kinds.Should().Contain(TokenKind.Modify);
        kinds.Should().Contain(TokenKind.Readonly);
        // No Writable modifier token — this is the access-mode modify, not field-level writable
        kinds.Should().NotContain(TokenKind.Writable);
    }

    [Fact]
    public void Case4_InStateModifyReadonly_CompileThrowsNotImplemented()
    {
        var src =
            "precept TestStateful\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft modify Amount readonly\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }

    // ── Case 5: in Draft omit Amount (structural exclusion) ──────────────────

    [Fact]
    public void Case5_InStateOmit_LexesCorrectly()
    {
        // write at root level was old eliminated syntax. Now testing omit — structural exclusion.
        var src =
            "precept TestOmit\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft omit Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var stream = Lexer.Lex(src);
        // Lexer is context-free — it does not reject semantically invalid forms; Parser would.
        stream.Diagnostics.Should().BeEmpty();

        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        // in Draft omit Amount → In, Identifier, Omit, Identifier
        kinds.Should().Contain(TokenKind.In);
        kinds.Should().Contain(TokenKind.Omit);
    }

    [Fact]
    public void Case5_InStateOmit_CompileThrowsNotImplemented()
    {
        var src =
            "precept TestOmit\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft omit Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }
}
