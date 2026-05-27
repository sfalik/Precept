using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;
using Xunit.Abstractions;

namespace Precept.Tests;

/// <summary>
/// Compile-surface tests for the unified access-modifier vocabulary —
/// field-declaration `editable`, per-state `modify F editable | readonly | omit`.
/// </summary>
public class WritableSurfaceTests(ITestOutputHelper output)
{
    // ── Case 1: field Amount as money editable ──────────────────────────────

    [Fact]
    public void Case1_EditableFieldModifier_LexesCorrectly()
    {
        // F-LANG-GRAPH-04 Decision 5: the unified `editable` keyword stands in
        // for the retired `writable` value modifier at the field-declaration site.
        var src = "precept TestEditable\nfield Amount as money editable";
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
            TokenKind.Editable,
            TokenKind.EndOfSource);
    }

    [Fact]
    public void Case1_EditableFieldModifier_CompileSucceeds()
    {
        var src = "precept TestEditable\nfield Amount as money editable";
        var act = () => Compiler.Compile(src);
        act.Should().NotThrow();
    }

    // ── Case 2: field Amount as money (no editable) ─────────────────────────

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
    }

    [Fact]
    public void Case2_ReadOnlyField_CompileSucceeds()
    {
        var src = "precept TestReadOnly\nfield Amount as money";
        var act = () => Compiler.Compile(src);
        act.Should().NotThrow();
    }

    // ── Case 3: in Draft modify Amount editable (per-state access vocab) ─────

    [Fact]
    public void Case3_ModifyEditable_LexesCorrectly()
    {
        // Modify-editable surface: `in State modify Field editable`.
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
    public void Case3_ModifyEditable_CompileSucceeds()
    {
        var src =
            "precept TestModify\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft modify Amount editable\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";
        var act = () => Compiler.Compile(src);
        act.Should().NotThrow();
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

        kinds.Should().Contain(TokenKind.In);
        kinds.Should().Contain(TokenKind.Modify);
        kinds.Should().Contain(TokenKind.Readonly);
    }

    [Fact]
    public void Case4_InStateModifyReadonly_CompileSucceeds()
    {
        var src =
            "precept TestStateful\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft modify Amount readonly\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var act = () => Compiler.Compile(src);
        act.Should().NotThrow();
    }

    // ── Case 5: in Draft omit Amount (structural exclusion) ──────────────────

    [Fact]
    public void Case5_InStateOmit_LexesCorrectly()
    {
        var src =
            "precept TestOmit\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft omit Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var stream = Lexer.Lex(src);
        stream.Diagnostics.Should().BeEmpty();

        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        kinds.Should().Contain(TokenKind.In);
        kinds.Should().Contain(TokenKind.Omit);
    }

    [Fact]
    public void Case5_InStateOmit_CompileSucceeds()
    {
        var src =
            "precept TestOmit\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft omit Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var act = () => Compiler.Compile(src);
        act.Should().NotThrow();
    }
}
