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

    // ── Case 3: write all on stateless precept ──────────────────────────────

    [Fact]
    public void Case3_WriteAll_LexesCorrectly()
    {
        var src = "precept TestWriteAll\nfield Amount as money\nwrite all";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty();
        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        kinds.Should().ContainInOrder(
            TokenKind.Precept, TokenKind.Identifier, TokenKind.NewLine,
            TokenKind.Field, TokenKind.Identifier, TokenKind.As, TokenKind.MoneyType, TokenKind.NewLine,
            TokenKind.Write, TokenKind.All,
            TokenKind.EndOfSource);
    }

    [Fact]
    public void Case3_WriteAll_CompileThrowsNotImplemented()
    {
        var src = "precept TestWriteAll\nfield Amount as money\nwrite all";
        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }

    // ── Case 4: in Draft write Amount (stateful) ────────────────────────────

    [Fact]
    public void Case4_InStateWrite_LexesCorrectly()
    {
        var src =
            "precept TestStateful\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft write Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var stream = Lexer.Lex(src);
        stream.Diagnostics.Should().BeEmpty();

        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        // Key tokens: In, Identifier(Draft), Write, Identifier(Amount)
        kinds.Should().Contain(TokenKind.In);
        kinds.Should().Contain(TokenKind.Write);
        // No Writable modifier token — this is the access-mode write
        kinds.Should().NotContain(TokenKind.Writable);
    }

    [Fact]
    public void Case4_InStateWrite_CompileThrowsNotImplemented()
    {
        var src =
            "precept TestStateful\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "in Draft write Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }

    // ── Case 5: write Amount at root level (old eliminated syntax) ──────────

    [Fact]
    public void Case5_RootLevelWrite_LexesCorrectly()
    {
        var src =
            "precept TestOldSyntax\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "write Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var stream = Lexer.Lex(src);
        // Lexer is context-free — it does not reject root-level write; Parser would.
        stream.Diagnostics.Should().BeEmpty();

        var kinds = stream.Tokens.Select(t => t.Kind).ToArray();
        output.WriteLine("Tokens: " + string.Join(", ", kinds));

        // write lexes as Write token; Amount as Identifier
        var writeIdx = Array.IndexOf(kinds, TokenKind.Write);
        writeIdx.Should().BeGreaterThan(-1);
        kinds[writeIdx + 1].Should().Be(TokenKind.Identifier);
    }

    [Fact]
    public void Case5_RootLevelWrite_CompileThrowsNotImplemented()
    {
        var src =
            "precept TestOldSyntax\n" +
            "field Amount as money\n" +
            "state Draft initial, Approved terminal success\n" +
            "write Amount\n" +
            "from Draft on Approve -> transition Approved\n" +
            "event Approve";

        var act = () => Compiler.Compile(src);
        act.Should().Throw<NotImplementedException>();
    }
}
