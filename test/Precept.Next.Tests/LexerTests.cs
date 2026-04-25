using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Next.Tests;

public class LexerTests
{
    [Fact]
    public void Lex_StringLiteral_ProducesDecodedTokenAndFullSpan()
    {
        var stream = Lexer.Lex("\"hello\"");

        stream.Tokens.Should().HaveCount(2);
        stream.Tokens[0].Should().Be(new Token(TokenKind.StringLiteral, "hello", 1, 1, 0, 7));
        stream.Tokens[1].Kind.Should().Be(TokenKind.EndOfSource);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_StringInterpolation_ProducesStartMiddleAndEndSegmentsWithSpans()
    {
        var stream = Lexer.Lex("\"a {B} c {D} e\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringStart, "a ", 1, 1, 0, 4),
            new Token(TokenKind.Identifier, "B", 1, 5, 4, 1),
            new Token(TokenKind.StringMiddle, " c ", 1, 6, 5, 5),
            new Token(TokenKind.Identifier, "D", 1, 11, 10, 1),
            new Token(TokenKind.StringEnd, " e", 1, 12, 11, 4),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 16, 15, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_EmptyStringInterpolation_ProducesEmptyBoundaryTokensWithFullSpans()
    {
        var stream = Lexer.Lex("\"{}\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringStart, string.Empty, 1, 1, 0, 2),
            new Token(TokenKind.StringEnd, string.Empty, 1, 3, 2, 2),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 5, 4, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_StringInterpolation_CanContainNestedStringLiteralInsideExpression()
    {
        var stream = Lexer.Lex("\"a {\"x\"} b\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringStart, "a ", 1, 1, 0, 4),
            new Token(TokenKind.StringLiteral, "x", 1, 5, 4, 3),
            new Token(TokenKind.StringEnd, " b", 1, 8, 7, 4),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 12, 11, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_StringLiteral_DecodesSupportedEscapesIntoTokenText()
    {
        var stream = Lexer.Lex("\"a\\\"b\\\\c\\nd\\te{{f}}\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, "a\"b\\c\nd\te{f}", 1, 1, 0, 20),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 21, 20, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_LongStringLiteral_ResizesContentBufferAndPreservesText()
    {
        var text = new string('a', 200);
        var stream = Lexer.Lex($"\"{text}\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, text, 1, 1, 0, 202),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 203, 202, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_TypedConstant_ProducesDecodedTokenAndFullSpan()
    {
        var stream = Lexer.Lex("'2026-06-01'");

        stream.Tokens.Should().HaveCount(2);
        stream.Tokens[0].Should().Be(new Token(TokenKind.TypedConstant, "2026-06-01", 1, 1, 0, 12));
        stream.Tokens[1].Kind.Should().Be(TokenKind.EndOfSource);
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_TypedConstantInterpolation_ProducesStartAndEndSegmentsWithSpans()
    {
        var stream = Lexer.Lex("'{GraceDays} days'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstantStart, string.Empty, 1, 1, 0, 2),
            new Token(TokenKind.Identifier, "GraceDays", 1, 3, 2, 9),
            new Token(TokenKind.TypedConstantEnd, " days", 1, 12, 11, 7),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 19, 18, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_TypedConstant_DecodesSupportedEscapesIntoTokenText()
    {
        var stream = Lexer.Lex("'a\\'b\\\\c{{d}}'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "a'b\\c{d}", 1, 1, 0, 14),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 15, 14, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_NumericForms_ProduceNumberLiteralTokens()
    {
        var stream = Lexer.Lex("1 2.5 3e+4 6E-2");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.NumberLiteral, "1", 1, 1, 0, 1),
            new Token(TokenKind.NumberLiteral, "2.5", 1, 3, 2, 3),
            new Token(TokenKind.NumberLiteral, "3e+4", 1, 7, 6, 4),
            new Token(TokenKind.NumberLiteral, "6E-2", 1, 12, 11, 4),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 16, 15, 0));

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
            new Token(TokenKind.Field, "field", 1, 1, 0, 5),
            new Token(TokenKind.Identifier, "A", 1, 7, 6, 1),
            new Token(TokenKind.Comment, "# note", 1, 9, 8, 6),
            new Token(TokenKind.NewLine, string.Empty, 1, 15, 14, 2),
            new Token(TokenKind.State, "state", 2, 1, 16, 5),
            new Token(TokenKind.EndOfSource, string.Empty, 2, 6, 21, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_CommentAtEndOfSource_ProducesCommentThenEndOfSource()
    {
        var stream = Lexer.Lex("field A # note");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Field, "field", 1, 1, 0, 5),
            new Token(TokenKind.Identifier, "A", 1, 7, 6, 1),
            new Token(TokenKind.Comment, "# note", 1, 9, 8, 6),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 15, 14, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_CrOnlyNewLine_ProducesSingleCharacterNewLineToken()
    {
        var stream = Lexer.Lex("field\rstate");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.Field, "field", 1, 1, 0, 5),
            new Token(TokenKind.NewLine, string.Empty, 1, 6, 5, 1),
            new Token(TokenKind.State, "state", 2, 1, 6, 5),
            new Token(TokenKind.EndOfSource, string.Empty, 2, 6, 11, 0));

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
            new Token(TokenKind.Identifier, "Alpha_1", 1, 1, 0, 7),
            new Token(TokenKind.Identifier, "Beta2", 1, 9, 8, 5),
            new Token(TokenKind.Identifier, "set_value", 1, 15, 14, 9),
            new Token(TokenKind.Identifier, "notemptyValue", 1, 25, 24, 13),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 38, 37, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_LeadingUnderscore_IsNotAValidIdentifierStart()
    {
        var stream = Lexer.Lex("_hidden");

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.InvalidCharacter));
        stream.Tokens.Should().Equal(
            new Token(TokenKind.Identifier, "hidden", 1, 2, 1, 6),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 8, 7, 0));
    }

    [Fact]
    public void Lex_SourceTooLarge_ReturnsOnlyEndOfSourceAndSecurityDiagnostic()
    {
        var stream = Lexer.Lex(new string('a', 65_537));

        stream.Tokens.Should().Equal(new Token(TokenKind.EndOfSource, string.Empty, 1, 1, 0, 0));
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
            new Token(TokenKind.Identifier, source, 1, 1, 0, 65_536),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 65_537, 65_536, 0));

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
            new Token(TokenKind.EndOfSource, string.Empty, 1, 2, 1, 0));
    }

    [Fact]
    public void Lex_InvalidFormatCharacter_FormatsDiagnosticWithUnicodeEscape()
    {
        var stream = Lexer.Lex("\u200B");

        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.InvalidCharacter));
        stream.Diagnostics[0].Message.Should().Contain("\\u200B");
        stream.Tokens.Should().Equal(
            new Token(TokenKind.EndOfSource, string.Empty, 1, 2, 1, 0));
    }

    [Fact]
    public void Lex_UnrecognizedStringEscapeInsideLiteral_SkipsEscapedCharacterAndContinues()
    {
        var stream = Lexer.Lex("\"a\\qb\"");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, "ab", 1, 1, 0, 6),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 7, 6, 0));

        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.UnrecognizedStringEscape));
        stream.Diagnostics[0].Message.Should().Contain("q");
    }

    [Fact]
    public void Lex_UnrecognizedTypedConstantEscapeInsideLiteral_SkipsEscapedCharacterAndContinues()
    {
        var stream = Lexer.Lex("'a\\qb'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "ab", 1, 1, 0, 6),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 7, 6, 0));

        stream.Diagnostics.Should().ContainSingle();
        stream.Diagnostics[0].Code.Should().Be(nameof(DiagnosticCode.UnrecognizedTypedConstantEscape));
        stream.Diagnostics[0].Message.Should().Contain("q");
    }

    [Fact]
    public void Lex_UnterminatedStringLiteralAtEndOfSource_EmitsLiteralTokenAndDiagnostic()
    {
        var stream = Lexer.Lex("\"abc");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.StringLiteral, "abc", 1, 1, 0, 4),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 5, 4, 0));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.UnterminatedStringLiteral));
    }

    [Fact]
    public void Lex_UnterminatedTypedConstantAtEndOfSource_EmitsLiteralTokenAndDiagnostic()
    {
        var stream = Lexer.Lex("'abc");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "abc", 1, 1, 0, 4),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 5, 4, 0));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_TypedConstantInterpolation_ProducesMiddleSegmentsForMultipleInterpolations()
    {
        var stream = Lexer.Lex("'a {B} c {D} e'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstantStart, "a ", 1, 1, 0, 4),
            new Token(TokenKind.Identifier, "B", 1, 5, 4, 1),
            new Token(TokenKind.TypedConstantMiddle, " c ", 1, 6, 5, 5),
            new Token(TokenKind.Identifier, "D", 1, 11, 10, 1),
            new Token(TokenKind.TypedConstantEnd, " e", 1, 12, 11, 4),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 16, 15, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_NumberLookahead_DoesNotConsumeDotOrIncompleteExponent()
    {
        var stream = Lexer.Lex("1.foo 2e+ Bar");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.NumberLiteral, "1", 1, 1, 0, 1),
            new Token(TokenKind.Dot, ".", 1, 2, 1, 1),
            new Token(TokenKind.Identifier, "foo", 1, 3, 2, 3),
            new Token(TokenKind.NumberLiteral, "2", 1, 7, 6, 1),
            new Token(TokenKind.Identifier, "e", 1, 8, 7, 1),
            new Token(TokenKind.Plus, "+", 1, 9, 8, 1),
            new Token(TokenKind.Identifier, "Bar", 1, 11, 10, 3),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 14, 13, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_UnrecognizedStringEscapeBeforeNewLine_DoesNotSkipUnterminatedLiteralRecovery()
    {
        var stream = Lexer.Lex("\"\\\n");

        stream.Tokens.Should().HaveCount(3);
        stream.Tokens[0].Should().Be(new Token(TokenKind.StringLiteral, string.Empty, 1, 1, 0, 2));
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
        stream.Tokens[0].Should().Be(new Token(TokenKind.TypedConstant, string.Empty, 1, 1, 0, 2));
        stream.Tokens[1].Should().Be(new Token(TokenKind.EndOfSource, string.Empty, 1, 3, 2, 0));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(
            nameof(DiagnosticCode.UnrecognizedTypedConstantEscape),
            nameof(DiagnosticCode.UnterminatedTypedConstant));
    }

    [Fact]
    public void Lex_UnrecognizedTypedConstantEscapeBeforeNewLine_DoesNotSkipUnterminatedLiteralRecovery()
    {
        var stream = Lexer.Lex("'\\\n");

        stream.Tokens.Should().HaveCount(3);
        stream.Tokens[0].Should().Be(new Token(TokenKind.TypedConstant, string.Empty, 1, 1, 0, 2));
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
            new Token(TokenKind.StringLiteral, "a}b", 1, 1, 0, 5),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 6, 5, 0));

        stream.Diagnostics.Select(d => d.Code).Should().Equal(nameof(DiagnosticCode.UnescapedBraceInLiteral));
    }

    [Fact]
    public void Lex_LoneBraceInsideTypedConstant_PreservesTextAndReportsDiagnostic()
    {
        var stream = Lexer.Lex("'a}b'");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.TypedConstant, "a}b", 1, 1, 0, 5),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 6, 5, 0));

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
            new Token(TokenKind.EndOfSource, string.Empty, 1, 2, 1, 0));

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
            new Token(TokenKind.CaseInsensitiveEquals, "~=", 1, 1, 0, 2),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 3, 2, 0));

        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_BangTilde_ProducesCaseInsensitiveNotEqualsToken()
    {
        var stream = Lexer.Lex("!~");

        stream.Tokens.Should().Equal(
            new Token(TokenKind.CaseInsensitiveNotEquals, "!~", 1, 1, 0, 2),
            new Token(TokenKind.EndOfSource, string.Empty, 1, 3, 2, 0));

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
}