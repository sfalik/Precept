using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public class LexerTests
{
    [Fact]
    public void Lex_StringLiteral_ProducesDecodedTokenAndFullSpan()
    {
        var stream = Lexer.Lex("\"hello\"");

        stream.Tokens.Should().HaveCount(2);
        stream.Tokens[0].Should().Be(new Token(TokenKind.StringLiteral, "hello", new SourceSpan(0, 7, 1, 1, 1, 8)));
        stream.Tokens[1].Kind.Should().Be(TokenKind.EndOfSource);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_StringInterpolation_ProducesStartMiddleAndEndSegmentsWithSpans()
    {
        var stream = Lexer.Lex("\"a {B} c {D} e\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringStart, "a ", new SourceSpan(0, 4, 1, 1, 1, 5)),
            new Token(TokenKind.Identifier, "B", new SourceSpan(4, 1, 1, 5, 1, 6)),
            new Token(TokenKind.StringMiddle, " c ", new SourceSpan(5, 5, 1, 6, 1, 11)),
            new Token(TokenKind.Identifier, "D", new SourceSpan(10, 1, 1, 11, 1, 12)),
            new Token(TokenKind.StringEnd, " e", new SourceSpan(11, 4, 1, 12, 1, 16)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(15, 0, 1, 16, 1, 16)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_EmptyStringInterpolation_ProducesEmptyBoundaryTokensWithFullSpans()
    {
        var stream = Lexer.Lex("\"{}\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringStart, string.Empty, new SourceSpan(0, 2, 1, 1, 1, 3)),
            new Token(TokenKind.StringEnd, string.Empty, new SourceSpan(2, 2, 1, 3, 1, 5)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(4, 0, 1, 5, 1, 5)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_StringInterpolation_CanContainNestedStringLiteralInsideExpression()
    {
        var stream = Lexer.Lex("\"a {\"x\"} b\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringStart, "a ", new SourceSpan(0, 4, 1, 1, 1, 5)),
            new Token(TokenKind.StringLiteral, "x", new SourceSpan(4, 3, 1, 5, 1, 8)),
            new Token(TokenKind.StringEnd, " b", new SourceSpan(7, 4, 1, 8, 1, 12)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(11, 0, 1, 12, 1, 12)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_StringLiteral_DecodesSupportedEscapesIntoTokenText()
    {
        var stream = Lexer.Lex("\"a\\\"b\\\\c\\nd\\te{{f}}\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, "a\"b\\c\nd\te{f}", new SourceSpan(0, 20, 1, 1, 1, 21)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(20, 0, 1, 21, 1, 21)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_LongStringLiteral_ResizesContentBufferAndPreservesText()
    {
        var text = new string('a', 200);
        var stream = Lexer.Lex($"\"{text}\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, text, new SourceSpan(0, 202, 1, 1, 1, 203)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(202, 0, 1, 203, 1, 203)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_TypedConstant_ProducesDecodedTokenAndFullSpan()
    {
        var stream = Lexer.Lex("'2026-06-01'");

        stream.Tokens.Should().HaveCount(2);
        stream.Tokens[0].Should().Be(new Token(TokenKind.TypedConstant, "2026-06-01", new SourceSpan(0, 12, 1, 1, 1, 13)));
        stream.Tokens[1].Kind.Should().Be(TokenKind.EndOfSource);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_TypedConstantInterpolation_ProducesStartAndEndSegmentsWithSpans()
    {
        var stream = Lexer.Lex("'{GraceDays} days'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstantStart, string.Empty, new SourceSpan(0, 2, 1, 1, 1, 3)),
            new Token(TokenKind.Identifier, "GraceDays", new SourceSpan(2, 9, 1, 3, 1, 12)),
            new Token(TokenKind.TypedConstantEnd, " days", new SourceSpan(11, 7, 1, 12, 1, 19)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(18, 0, 1, 19, 1, 19)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_TypedConstant_DecodesSupportedEscapesIntoTokenText()
    {
        var stream = Lexer.Lex("'a\\'b\\\\c{{d}}'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "a'b\\c{d}", new SourceSpan(0, 14, 1, 1, 1, 15)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(14, 0, 1, 15, 1, 15)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_NumericForms_ProduceNumberLiteralTokens()
    {
        var stream = Lexer.Lex("1 2.5 3e+4 6E-2");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.NumberLiteral, "1", new SourceSpan(0, 1, 1, 1, 1, 2)),
            new Token(TokenKind.NumberLiteral, "2.5", new SourceSpan(2, 3, 1, 3, 1, 6)),
            new Token(TokenKind.NumberLiteral, "3e+4", new SourceSpan(6, 4, 1, 7, 1, 11)),
            new Token(TokenKind.NumberLiteral, "6E-2", new SourceSpan(11, 4, 1, 12, 1, 16)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(15, 0, 1, 16, 1, 16)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_OperatorsAndPunctuation_ProduceExpectedTokenKindsInOrder()
    {
        var stream = Lexer.Lex("== != ~= !~ >= <= > < = + - * / % -> . , ( ) [ ]");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.DoubleEquals,
            TokenKind.NotEquals,
            TokenKind.CaseInsensitiveEquals,
            TokenKind.CaseInsensitiveNotEquals,
            TokenKind.GreaterThanOrEqual,
            TokenKind.LessThanOrEqual,
            TokenKind.GreaterThan,
            TokenKind.LessThan,
            TokenKind.Assign,
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.Slash,
            TokenKind.Percent,
            TokenKind.Arrow,
            TokenKind.Dot,
            TokenKind.Comma,
            TokenKind.LeftParen,
            TokenKind.RightParen,
            TokenKind.LeftBracket,
            TokenKind.RightBracket,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_CommentAndCrLfNewLine_PreserveCommentTextAndAdvanceLine()
    {
        var stream = Lexer.Lex("field A # note\r\nstate");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Field, "field", new SourceSpan(0, 5, 1, 1, 1, 6)),
            new Token(TokenKind.Identifier, "A", new SourceSpan(6, 1, 1, 7, 1, 8)),
            new Token(TokenKind.Comment, "# note", new SourceSpan(8, 6, 1, 9, 1, 15)),
            new Token(TokenKind.NewLine, string.Empty, new SourceSpan(14, 2, 1, 15, 1, 17)),
            new Token(TokenKind.State, "state", new SourceSpan(16, 5, 2, 1, 2, 6)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(21, 0, 2, 6, 2, 6)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_CommentAtEndOfSource_ProducesCommentThenEndOfSource()
    {
        var stream = Lexer.Lex("field A # note");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Field, "field", new SourceSpan(0, 5, 1, 1, 1, 6)),
            new Token(TokenKind.Identifier, "A", new SourceSpan(6, 1, 1, 7, 1, 8)),
            new Token(TokenKind.Comment, "# note", new SourceSpan(8, 6, 1, 9, 1, 15)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(14, 0, 1, 15, 1, 15)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_CrOnlyNewLine_ProducesSingleCharacterNewLineToken()
    {
        var stream = Lexer.Lex("field\rstate");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Field, "field", new SourceSpan(0, 5, 1, 1, 1, 6)),
            new Token(TokenKind.NewLine, string.Empty, new SourceSpan(5, 1, 1, 6, 1, 7)),
            new Token(TokenKind.State, "state", new SourceSpan(6, 5, 2, 1, 2, 6)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(11, 0, 2, 6, 2, 6)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_SetInTypePosition_StillEmitsSetAndNeverSetType()
    {
        var stream = Lexer.Lex("field Items as set of string");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.Field,
            TokenKind.Identifier,
            TokenKind.As,
            TokenKind.Set,
            TokenKind.Of,
            TokenKind.StringType,
            TokenKind.EndOfSource);

        stream.Tokens.Should().NotContain(token => token.Kind == TokenKind.SetType);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_KeywordCatalog_MapsSetToSetAndExcludesSetType()
    {
        Tokens.Keywords["set"].Should().Be(TokenKind.Set);
        Tokens.Keywords.Values.Should().NotContain(TokenKind.SetType);
    }

    [Fact]
    public void Lex_IdentifierMayContainDigitsAndUnderscoresAfterFirstCharacter()
    {
        var stream = Lexer.Lex("Alpha_1 Beta2 set_value notemptyValue");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Identifier, "Alpha_1", new SourceSpan(0, 7, 1, 1, 1, 8)),
            new Token(TokenKind.Identifier, "Beta2", new SourceSpan(8, 5, 1, 9, 1, 14)),
            new Token(TokenKind.Identifier, "set_value", new SourceSpan(14, 9, 1, 15, 1, 24)),
            new Token(TokenKind.Identifier, "notemptyValue", new SourceSpan(24, 13, 1, 25, 1, 38)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(37, 0, 1, 38, 1, 38)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_LeadingUnderscore_IsNotAValidIdentifierStart()
    {
        var stream = Lexer.Lex("_hidden");

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.InvalidCharacter));
        stream.Tokens.Should().Equal(
            new Token(TokenKind.Identifier, "hidden", new SourceSpan(1, 6, 1, 2, 1, 8)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(7, 0, 1, 8, 1, 8)));
    }

    [Fact]
    public void Lex_SourceTooLarge_ReturnsOnlyEndOfSourceAndSecurityDiagnostic()
    {
        var stream = Lexer.Lex(new string('a', 65_537));

        stream.Tokens.Should().Equal(new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(0, 0, 1, 1, 1, 1)));
        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.InputTooLarge));
        stream.Diagnostics[0].Stage.Should().Be(DiagnosticStage.Lex);
    }

    [Fact]
    public void Lex_SourceAtSecurityLimit_IsStillLexed()
    {
        var source = new string('a', 65_536);
        var stream = Lexer.Lex(source);

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Identifier, source, new SourceSpan(0, 65_536, 1, 1, 1, 65_537)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(65_536, 0, 1, 65_537, 1, 65_537)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_InvalidControlCharacter_FormatsDiagnosticWithUnicodeEscape()
    {
        var stream = Lexer.Lex("\u0001");

        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.InvalidCharacter));
        stream.Diagnostics[0].Message.Should().Contain("\\u0001");
        stream.Tokens.Should().Equal(
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(1, 0, 1, 2, 1, 2)));
    }

    [Fact]
    public void Lex_InvalidFormatCharacter_FormatsDiagnosticWithUnicodeEscape()
    {
        var stream = Lexer.Lex("\u200B");

        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.InvalidCharacter));
        stream.Diagnostics[0].Message.Should().Contain("\\u200B");
        stream.Tokens.Should().Equal(
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(1, 0, 1, 2, 1, 2)));
    }

    [Fact]
    public void Lex_UnrecognizedStringEscapeInsideLiteral_SkipsEscapedCharacterAndContinues()
    {
        var stream = Lexer.Lex("\"a\\qb\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, "ab", new SourceSpan(0, 6, 1, 1, 1, 7)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(6, 0, 1, 7, 1, 7)));

        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.UnrecognizedStringEscape));
        stream.Diagnostics[0].Message.Should().Contain("q");
    }

    [Fact]
    public void Lex_UnrecognizedTypedConstantEscapeInsideLiteral_SkipsEscapedCharacterAndContinues()
    {
        var stream = Lexer.Lex("'a\\qb'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "ab", new SourceSpan(0, 6, 1, 1, 1, 7)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(6, 0, 1, 7, 1, 7)));

        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.UnrecognizedTypedConstantEscape));
        stream.Diagnostics[0].Message.Should().Contain("q");
    }

    [Fact]
    public void Lex_UnterminatedStringLiteralAtEndOfSource_EmitsLiteralTokenAndDiagnostic()
    {
        var stream = Lexer.Lex("\"abc");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, "abc", new SourceSpan(0, 4, 1, 1, 1, 5)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(4, 0, 1, 5, 1, 5)));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.UnterminatedStringLiteral));
    }

    [Fact]
    public void Lex_UnterminatedTypedConstantAtEndOfSource_EmitsLiteralTokenAndDiagnostic()
    {
        var stream = Lexer.Lex("'abc");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "abc", new SourceSpan(0, 4, 1, 1, 1, 5)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(4, 0, 1, 5, 1, 5)));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_TypedConstantInterpolation_ProducesMiddleSegmentsForMultipleInterpolations()
    {
        var stream = Lexer.Lex("'a {B} c {D} e'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstantStart, "a ", new SourceSpan(0, 4, 1, 1, 1, 5)),
            new Token(TokenKind.Identifier, "B", new SourceSpan(4, 1, 1, 5, 1, 6)),
            new Token(TokenKind.TypedConstantMiddle, " c ", new SourceSpan(5, 5, 1, 6, 1, 11)),
            new Token(TokenKind.Identifier, "D", new SourceSpan(10, 1, 1, 11, 1, 12)),
            new Token(TokenKind.TypedConstantEnd, " e", new SourceSpan(11, 4, 1, 12, 1, 16)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(15, 0, 1, 16, 1, 16)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_NumberLookahead_DoesNotConsumeDotOrIncompleteExponent()
    {
        var stream = Lexer.Lex("1.foo 2e+ Bar");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.NumberLiteral, "1", new SourceSpan(0, 1, 1, 1, 1, 2)),
            new Token(TokenKind.Dot, ".", new SourceSpan(1, 1, 1, 2, 1, 3)),
            new Token(TokenKind.Identifier, "foo", new SourceSpan(2, 3, 1, 3, 1, 6)),
            new Token(TokenKind.NumberLiteral, "2", new SourceSpan(6, 1, 1, 7, 1, 8)),
            new Token(TokenKind.Identifier, "e", new SourceSpan(7, 1, 1, 8, 1, 9)),
            new Token(TokenKind.Plus, "+", new SourceSpan(8, 1, 1, 9, 1, 10)),
            new Token(TokenKind.Identifier, "Bar", new SourceSpan(10, 3, 1, 11, 1, 14)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(13, 0, 1, 14, 1, 14)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_UnrecognizedStringEscapeBeforeNewLine_DoesNotSkipUnterminatedLiteralRecovery()
    {
        var stream = Lexer.Lex("\"\\\n");

        stream.Tokens.Should().HaveCount(3);
        stream.Tokens[0].Should().Be(new Token(TokenKind.StringLiteral, string.Empty, new SourceSpan(0, 2, 1, 1, 1, 3)));
        stream.Tokens[1].Kind.Should().Be(TokenKind.NewLine);
        stream.Tokens[2].Kind.Should().Be(TokenKind.EndOfSource);

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnrecognizedStringEscape),
            nameof(DiagnosticCode.UnterminatedStringLiteral));
    }

    [Fact]
    public void Lex_NewLineInsideInterpolation_ReportsInterpolationAndOuterStringDiagnostics()
    {
        var stream = Lexer.Lex("\"a {Name\n");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.StringStart,
            TokenKind.Identifier,
            TokenKind.StringMiddle,
            TokenKind.NewLine,
            TokenKind.EndOfSource);

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnterminatedInterpolation),
            nameof(DiagnosticCode.UnterminatedStringLiteral));
    }

    [Fact]
    public void Lex_EndOfSourceInsideInterpolation_ReportsInterpolationAndOuterStringDiagnosticsFromBuild()
    {
        var stream = Lexer.Lex("\"a {Name");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.StringStart,
            TokenKind.Identifier,
            TokenKind.EndOfSource);

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnterminatedInterpolation),
            nameof(DiagnosticCode.UnterminatedStringLiteral));
    }

    [Fact]
    public void Lex_NewLineInsideTypedConstantInterpolation_ReportsInterpolationAndOuterTypedConstantDiagnostics()
    {
        var stream = Lexer.Lex("'a {Name\n");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.TypedConstantStart,
            TokenKind.Identifier,
            TokenKind.TypedConstantMiddle,
            TokenKind.NewLine,
            TokenKind.EndOfSource);

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnterminatedInterpolation),
            nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_EndOfSourceInsideTypedConstantInterpolation_ReportsInterpolationAndOuterTypedConstantDiagnosticsFromBuild()
    {
        var stream = Lexer.Lex("'a {Name");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.TypedConstantStart,
            TokenKind.Identifier,
            TokenKind.EndOfSource);

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnterminatedInterpolation),
            nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_MaxInterpolationDepth_WithClosingBraceBeforeNewLine_RecoversAndContinuesToLineEnd()
    {
        var source = "\"{" + "\"{" + "\"{" + "\"{x}\n";
        var stream = Lexer.Lex(source);

        stream.Tokens.Last().Kind.Should().Be(TokenKind.EndOfSource);
        stream.Tokens.Should().Contain(token => token.Kind == TokenKind.NewLine);
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedInterpolation));
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedStringLiteral));
    }

    [Fact]
    public void Lex_MaxInterpolationDepth_WithoutClosingBraceBeforeNewLine_PopsEnclosingLiteralAndRecovers()
    {
        var source = "\"{" + "\"{" + "\"{" + "\"{x\n";
        var stream = Lexer.Lex(source);

        stream.Tokens.Last().Kind.Should().Be(TokenKind.EndOfSource);
        stream.Tokens.Should().Contain(token => token.Kind == TokenKind.NewLine);
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedInterpolation));
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedStringLiteral));
    }

    [Fact]
    public void Lex_UnrecognizedTypedConstantEscapeAtEndOfSource_DoesNotSkipUnterminatedLiteralRecovery()
    {
        var stream = Lexer.Lex("'\\");

        stream.Tokens.Should().HaveCount(2);
        stream.Tokens[0].Should().Be(new Token(TokenKind.TypedConstant, string.Empty, new SourceSpan(0, 2, 1, 1, 1, 3)));
        stream.Tokens[1].Should().Be(new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(2, 0, 1, 3, 1, 3)));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnrecognizedTypedConstantEscape),
            nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_UnrecognizedTypedConstantEscapeBeforeNewLine_DoesNotSkipUnterminatedLiteralRecovery()
    {
        var stream = Lexer.Lex("'\\\n");

        stream.Tokens.Should().HaveCount(3);
        stream.Tokens[0].Should().Be(new Token(TokenKind.TypedConstant, string.Empty, new SourceSpan(0, 2, 1, 1, 1, 3)));
        stream.Tokens[1].Kind.Should().Be(TokenKind.NewLine);
        stream.Tokens[2].Kind.Should().Be(TokenKind.EndOfSource);

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnrecognizedTypedConstantEscape),
            nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_LoneBraceInsideStringLiteral_PreservesTextAndReportsDiagnostic()
    {
        var stream = Lexer.Lex("\"a}b\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, "a}b", new SourceSpan(0, 5, 1, 1, 1, 6)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(5, 0, 1, 6, 1, 6)));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.UnescapedBraceInLiteral));
    }

    [Fact]
    public void Lex_LoneBraceInsideTypedConstant_PreservesTextAndReportsDiagnostic()
    {
        var stream = Lexer.Lex("'a}b'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "a}b", new SourceSpan(0, 5, 1, 1, 1, 6)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(5, 0, 1, 6, 1, 6)));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.UnescapedBraceInLiteral));
    }

    [Fact]
    public void Lex_MaxInterpolationDepthInsideTypedConstant_WithClosingBraceBeforeNewLine_RecoversAndContinuesToLineEnd()
    {
        var source = "'{" + "'{" + "'{" + "'{x}\n";
        var stream = Lexer.Lex(source);

        stream.Tokens.Last().Kind.Should().Be(TokenKind.EndOfSource);
        stream.Tokens.Should().Contain(token => token.Kind == TokenKind.NewLine);
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedInterpolation));
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_MaxInterpolationDepthInsideTypedConstant_WithoutClosingBraceBeforeNewLine_PopsEnclosingLiteralAndRecovers()
    {
        var source = "'{" + "'{" + "'{" + "'{x\n";
        var stream = Lexer.Lex(source);

        stream.Tokens.Last().Kind.Should().Be(TokenKind.EndOfSource);
        stream.Tokens.Should().Contain(token => token.Kind == TokenKind.NewLine);
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedInterpolation));
        stream.Diagnostics.Select(d => d.Code).Should().Contain(nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_InvalidTopLevelCharacter_ReportsDiagnosticAndSkipsCharacter()
    {
        var stream = Lexer.Lex("@");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(1, 0, 1, 2, 1, 2)));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.InvalidCharacter));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Case-insensitive comparison operators
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Lex_TildeEquals_ProducesCaseInsensitiveEqualsToken()
    {
        var stream = Lexer.Lex("~=");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.CaseInsensitiveEquals, "~=", new SourceSpan(0, 2, 1, 1, 1, 3)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(2, 0, 1, 3, 1, 3)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_BangTilde_ProducesCaseInsensitiveNotEqualsToken()
    {
        var stream = Lexer.Lex("!~");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.CaseInsensitiveNotEquals, "!~", new SourceSpan(0, 2, 1, 1, 1, 3)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(2, 0, 1, 3, 1, 3)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_BangTildeBeforeBangEquals_ScanOrderIsCorrect()
    {
        var stream = Lexer.Lex("!~ !=");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.CaseInsensitiveNotEquals,
            TokenKind.NotEquals,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_TildeEqualsBeforeDoubleEquals_ScanOrderIsCorrect()
    {
        var stream = Lexer.Lex("~= ==");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.CaseInsensitiveEquals,
            TokenKind.DoubleEquals,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_CaseInsensitiveEqualsInExpressionContext_LexesCorrectly()
    {
        var stream = Lexer.Lex("Name ~= \"test\"");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.Identifier,
            TokenKind.CaseInsensitiveEquals,
            TokenKind.StringLiteral,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_CaseInsensitiveNotEqualsInExpressionContext_LexesCorrectly()
    {
        var stream = Lexer.Lex("Name !~ \"test\"");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.Identifier,
            TokenKind.CaseInsensitiveNotEquals,
            TokenKind.StringLiteral,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Tilde prefix (~) for case-insensitive collection inner types
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Lex_Tilde_EmittedForStandalonePrefix()
    {
        // ~ not followed by = must produce a standalone Tilde token
        var stream = Lexer.Lex("set of ~string");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.Set,
            TokenKind.Of,
            TokenKind.Tilde,
            TokenKind.StringType,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_Tilde_NotEmittedForCaseInsensitiveEquals()
    {
        // ~= must remain a single CaseInsensitiveEquals token — no standalone Tilde emitted
        var stream = Lexer.Lex("~=");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.CaseInsensitiveEquals,
            TokenKind.EndOfSource);

        stream.Tokens.Should().NotContain(token => token.Kind == TokenKind.Tilde);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_Tilde_NotEmittedForCaseInsensitiveNotEquals()
    {
        // !~ must remain a single CaseInsensitiveNotEquals token — Tilde is not part of it
        var stream = Lexer.Lex("!~");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.CaseInsensitiveNotEquals,
            TokenKind.EndOfSource);

        stream.Tokens.Should().NotContain(token => token.Kind == TokenKind.Tilde);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_Tilde_InQueueContext()
    {
        var stream = Lexer.Lex("queue of ~string");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.QueueType,
            TokenKind.Of,
            TokenKind.Tilde,
            TokenKind.StringType,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_Tilde_InStackContext()
    {
        var stream = Lexer.Lex("stack of ~string");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.StackType,
            TokenKind.Of,
            TokenKind.Tilde,
            TokenKind.StringType,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_Tilde_BeforeNonString()
    {
        // Lexer emits Tilde + IntegerType without a diagnostic.
        // The parser accepts this; the type checker (separate slice) rejects it.
        var stream = Lexer.Lex("set of ~integer");

        stream.Tokens.Select(token => token.Kind).Should().Equal(
            TokenKind.Set,
            TokenKind.Of,
            TokenKind.Tilde,
            TokenKind.IntegerType,
            TokenKind.EndOfSource);

        stream.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Operator and punctuation: individual token verification (B2)
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("->", "Arrow", "->", 2)]
    [InlineData("!~", "CaseInsensitiveNotEquals", "!~", 2)]
    [InlineData("~=", "CaseInsensitiveEquals", "~=", 2)]
    [InlineData("==", "DoubleEquals", "==", 2)]
    [InlineData("!=", "NotEquals", "!=", 2)]
    [InlineData(">=", "GreaterThanOrEqual", ">=", 2)]
    [InlineData("<=", "LessThanOrEqual", "<=", 2)]
    [InlineData("=", "Assign", "=", 1)]
    [InlineData(">", "GreaterThan", ">", 1)]
    [InlineData("<", "LessThan", "<", 1)]
    [InlineData("+", "Plus", "+", 1)]
    [InlineData("-", "Minus", "-", 1)]
    [InlineData("*", "Star", "*", 1)]
    [InlineData("/", "Slash", "/", 1)]
    [InlineData("%", "Percent", "%", 1)]
    [InlineData("~", "Tilde", "~", 1)]
    public void Lex_Operator_ProducesExpectedKindTextOffsetAndLength(string source, string kindName, string expectedText, int expectedLength)
    {
        var expectedKind = Enum.Parse<TokenKind>(kindName);
        var stream = Lexer.Lex(source);

        stream.Tokens[0].Should().Be(new Token(expectedKind, expectedText, new SourceSpan(0, expectedLength, 1, 1, 1, 1 + expectedLength)));
        stream.Tokens[1].Kind.Should().Be(TokenKind.EndOfSource);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData(".", "Dot")]
    [InlineData(",", "Comma")]
    [InlineData("(", "LeftParen")]
    [InlineData(")", "RightParen")]
    [InlineData("[", "LeftBracket")]
    [InlineData("]", "RightBracket")]
    public void Lex_Punctuation_ProducesExpectedKindTextAndOffset(string source, string kindName)
    {
        var expectedKind = Enum.Parse<TokenKind>(kindName);
        var stream = Lexer.Lex(source);

        stream.Tokens[0].Should().Be(new Token(expectedKind, source, new SourceSpan(0, 1, 1, 1, 1, 2)));
        stream.Tokens[1].Kind.Should().Be(TokenKind.EndOfSource);
        stream.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Maximal-munch boundary tests (G1)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Lex_Arrow_IsNotScannedAsMinusThenGreaterThan()
    {
        var stream = Lexer.Lex("->x");

        stream.Tokens.Select(t => t.Kind).Should().Equal(
            TokenKind.Arrow,
            TokenKind.Identifier,
            TokenKind.EndOfSource);
    }

    [Fact]
    public void Lex_GreaterThanOrEqual_IsNotScannedAsGreaterThanThenAssign()
    {
        var stream = Lexer.Lex(">=1");

        stream.Tokens.Select(t => t.Kind).Should().Equal(
            TokenKind.GreaterThanOrEqual,
            TokenKind.NumberLiteral,
            TokenKind.EndOfSource);
    }

    [Fact]
    public void Lex_LessThanOrEqual_IsNotScannedAsLessThanThenAssign()
    {
        var stream = Lexer.Lex("<=1");

        stream.Tokens.Select(t => t.Kind).Should().Equal(
            TokenKind.LessThanOrEqual,
            TokenKind.NumberLiteral,
            TokenKind.EndOfSource);
    }

    [Fact]
    public void Lex_DoubleEquals_IsNotScannedAsTwoAssigns()
    {
        var stream = Lexer.Lex("==1");

        stream.Tokens.Select(t => t.Kind).Should().Equal(
            TokenKind.DoubleEquals,
            TokenKind.NumberLiteral,
            TokenKind.EndOfSource);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Dual-use keyword tests (G2)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Lex_Min_AlwaysEmitsAsKeyword()
    {
        var stream = Lexer.Lex("min");

        stream.Tokens[0].Kind.Should().Be(TokenKind.Min);
        stream.Tokens[0].Text.Should().Be("min");
    }

    [Fact]
    public void Lex_Max_AlwaysEmitsAsKeyword()
    {
        var stream = Lexer.Lex("max");

        stream.Tokens[0].Kind.Should().Be(TokenKind.Max);
        stream.Tokens[0].Text.Should().Be("max");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Newline and whitespace edge cases (G4)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Lex_BareLineFeedNewLine_ProducesSingleCharacterNewLineToken()
    {
        var stream = Lexer.Lex("field\nstate");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Field, "field", new SourceSpan(0, 5, 1, 1, 1, 6)),
            new Token(TokenKind.NewLine, string.Empty, new SourceSpan(5, 1, 1, 6, 1, 7)),
            new Token(TokenKind.State, "state", new SourceSpan(6, 5, 2, 1, 2, 6)),
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(11, 0, 2, 6, 2, 6)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_EmptySource_ProducesOnlyEndOfSource()
    {
        var stream = Lexer.Lex("");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(0, 0, 1, 1, 1, 1)));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_WhitespaceOnlySource_ProducesOnlyEndOfSource()
    {
        var stream = Lexer.Lex("   \t  ");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.EndOfSource, string.Empty, new SourceSpan(6, 0, 1, 7, 1, 7)));

        stream.Diagnostics.Should().BeEmpty();
    }
}
