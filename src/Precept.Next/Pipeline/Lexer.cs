using System.Collections.Immutable;
using System.Text;

namespace Precept.Pipeline;

public static class Lexer
{
    private const int MaxSourceLength = 65_536;
    private const int MaxModeStackDepth = 8;

    public static TokenStream Lex(string source)
    {
        if (source.Length > MaxSourceLength)
        {
            return new TokenStream(
                ImmutableArray.Create(new Token(TokenKind.EndOfSource, "", 1, 1, 0, 0)),
                ImmutableArray.Create(Diagnostics.Create(
                    DiagnosticCode.InputTooLarge,
                    new SourceRange(1, 1, 1, 1))));
        }

        var scanner = new Scanner(source);
        scanner.ScanAll();
        return scanner.Build();
    }

    private static bool IsLetter(char c) =>
        (uint)((c | 0x20) - 'a') <= 'z' - 'a';

    private static bool IsDigit(char c) =>
        (uint)(c - '0') <= 9;

    private static bool IsWordChar(char c) =>
        IsLetter(c) || IsDigit(c) || c == '_';

    private enum LexerMode
    {
        Normal,
        String,
        TypedConstant,
        Interpolation,
    }

    private struct Scanner
    {
        private readonly string _source;
        private int _offset;
        private int _line;
        private int _column;
        private readonly ImmutableArray<Token>.Builder _tokens;
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;
        private readonly Stack<LexerMode> _modeStack;

        public Scanner(string source)
        {
            _source = source;
            _offset = 0;
            _line = 1;
            _column = 1;
            _tokens = ImmutableArray.CreateBuilder<Token>();
            _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            _modeStack = new Stack<LexerMode>();
            _modeStack.Push(LexerMode.Normal);
        }

        private bool IsAtEnd => _offset >= _source.Length;

        private char Current => _source[_offset];

        private char PeekNext =>
            _offset + 1 < _source.Length ? _source[_offset + 1] : '\0';

        private LexerMode CurrentMode => _modeStack.Peek();

        private void PushMode(LexerMode mode) => _modeStack.Push(mode);

        private void PopMode()
        {
            if (_modeStack.Count > 1)
                _modeStack.Pop();
        }

        private void Advance()
        {
            _offset++;
            _column++;
        }

        public void ScanAll()
        {
            while (!IsAtEnd)
            {
                switch (CurrentMode)
                {
                    case LexerMode.String:
                        ScanStringContent();
                        break;
                    case LexerMode.TypedConstant:
                        ScanTypedConstantContent();
                        break;
                    default: // Normal or Interpolation
                        ScanToken();
                        break;
                }
            }
        }

        public TokenStream Build()
        {
            _tokens.Add(new Token(TokenKind.EndOfSource, "", _line, _column, _offset, 0));
            return new TokenStream(_tokens.ToImmutable(), _diagnostics.ToImmutable());
        }

        private void ScanToken()
        {
            var c = Current;

            // ── Whitespace (space, tab): consume silently ──────────
            if (c is ' ' or '\t')
            {
                Advance();
                while (!IsAtEnd && Current is ' ' or '\t')
                    Advance();
                return;
            }

            // ── Newlines ───────────────────────────────────────────
            if (c == '\n')
            {
                EmitNewLine(1);
                return;
            }
            if (c == '\r')
            {
                int len = PeekNext == '\n' ? 2 : 1;
                EmitNewLine(len);
                return;
            }

            // ── Comment ────────────────────────────────────────────
            if (c == '#')
            {
                ScanComment();
                return;
            }

            // ── Closing brace in Interpolation mode ────────────────
            if (c == '}' && CurrentMode == LexerMode.Interpolation)
            {
                Advance(); // consume '}'
                PopMode(); // back to String or TypedConstant
                return;
            }

            // ── Letter → keyword or identifier ─────────────────────
            if (IsLetter(c))
            {
                ScanWord();
                return;
            }

            // ── Digit → NumberLiteral ──────────────────────────────
            if (IsDigit(c))
            {
                ScanNumber();
                return;
            }

            // ── String literal ─────────────────────────────────────
            if (c == '"')
            {
                BeginString();
                return;
            }

            // ── Typed constant ─────────────────────────────────────
            if (c == '\'')
            {
                BeginTypedConstant();
                return;
            }

            // ── Operators (multi-char first) ───────────────────────
            if (TryScanOperator())
                return;

            // ── Punctuation ────────────────────────────────────────
            if (TryScanPunctuation())
                return;

            // ── Invalid character ──────────────────────────────────
            _diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.InvalidCharacter,
                new SourceRange(_line, _column, _line, _column),
                c));
            Advance();
        }

        // ── String / Typed-constant scanning ───────────────────────

        private void BeginString()
        {
            Advance(); // consume opening "
            PushMode(LexerMode.String);
        }

        private void BeginTypedConstant()
        {
            Advance(); // consume opening '
            PushMode(LexerMode.TypedConstant);
        }

        /// <summary>
        /// Scans content inside a <c>"..."</c> string. Called repeatedly from ScanAll
        /// while in String mode. Emits StringLiteral (no interpolation) or
        /// StringStart/StringMiddle/StringEnd (with interpolation).
        /// </summary>
        private void ScanStringContent()
        {
            int startLine = _line, startCol = _column, startOff = _offset;
            bool hadInterpolation = false;
            int segmentIndex = 0; // how many segments already emitted for this string
            var content = new StringBuilder();

            // Check if we're resuming after an interpolation (we'll have already
            // emitted StringStart or StringMiddle for a prior segment).
            // We can detect this: if the previous token is an expression token
            // and the mode is String, we're after a } that just popped Interpolation.
            // Actually, segmentIndex tracking: count how many StringStart/StringMiddle
            // tokens have been emitted for the current string by scanning backwards.
            segmentIndex = CountPriorStringSegments();
            if (segmentIndex > 0)
                hadInterpolation = true;

            while (!IsAtEnd)
            {
                var c = Current;

                // ── End of line → unterminated ──────────────────────
                if (c == '\n' || c == '\r')
                {
                    EmitStringSegment(content.ToString(), startLine, startCol, startOff,
                        hadInterpolation, segmentIndex, isFinal: false);
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UnterminatedStringLiteral,
                        new SourceRange(startLine, startCol, _line, _column)));
                    PopMode();
                    return;
                }

                // ── Closing " ──────────────────────────────────────
                if (c == '"')
                {
                    Advance(); // consume closing "
                    EmitStringSegment(content.ToString(), startLine, startCol, startOff,
                        hadInterpolation, segmentIndex, isFinal: true);
                    PopMode();
                    return;
                }

                // ── Escape: \" ─────────────────────────────────────
                if (c == '\\' && !IsAtEnd && PeekNext == '"')
                {
                    content.Append('"');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \\ ─────────────────────────────────────
                if (c == '\\' && PeekNext == '\\')
                {
                    content.Append('\\');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \n ─────────────────────────────────────
                if (c == '\\' && PeekNext == 'n')
                {
                    content.Append('\n');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \t ─────────────────────────────────────
                if (c == '\\' && PeekNext == 't')
                {
                    content.Append('\t');
                    Advance(); Advance();
                    continue;
                }

                // ── Escaped brace {{ → literal { ───────────────────
                if (c == '{' && PeekNext == '{')
                {
                    content.Append('{');
                    Advance(); Advance();
                    continue;
                }

                // ── Escaped brace }} → literal } ───────────────────
                if (c == '}' && PeekNext == '}')
                {
                    content.Append('}');
                    Advance(); Advance();
                    continue;
                }

                // ── Interpolation start { ──────────────────────────
                if (c == '{')
                {
                    Advance(); // consume '{'
                    hadInterpolation = true;
                    var kind = segmentIndex == 0 ? TokenKind.StringStart : TokenKind.StringMiddle;
                    _tokens.Add(new Token(kind, content.ToString(), startLine, startCol, startOff, _offset - startOff));
                    segmentIndex++;

                    if (_modeStack.Count >= MaxModeStackDepth)
                    {
                        _diagnostics.Add(Diagnostics.Create(
                            DiagnosticCode.UnterminatedInterpolation,
                            new SourceRange(_line, _column, _line, _column)));
                        RecoverFromUnterminatedInterpolation();
                        return;
                    }
                    PushMode(LexerMode.Interpolation);
                    return;
                }

                // ── Regular character ──────────────────────────────
                content.Append(c);
                Advance();
            }

            // End of source without closing " → unterminated
            EmitStringSegment(content.ToString(), startLine, startCol, startOff,
                hadInterpolation, segmentIndex, isFinal: false);
            _diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.UnterminatedStringLiteral,
                new SourceRange(startLine, startCol, _line, _column)));
            PopMode();
        }

        private void EmitStringSegment(string text, int startLine, int startCol, int startOff,
            bool hadInterpolation, int segmentIndex, bool isFinal)
        {
            int length = _offset - startOff;
            if (isFinal)
            {
                var kind = hadInterpolation ? TokenKind.StringEnd : TokenKind.StringLiteral;
                _tokens.Add(new Token(kind, text, startLine, startCol, startOff, length));
            }
            else
            {
                // Unterminated — emit what we have
                var kind = hadInterpolation
                    ? (segmentIndex == 0 ? TokenKind.StringStart : TokenKind.StringMiddle)
                    : TokenKind.StringLiteral;
                _tokens.Add(new Token(kind, text, startLine, startCol, startOff, length));
            }
        }

        /// <summary>
        /// Scans content inside a <c>'...'</c> typed constant. Called repeatedly from ScanAll
        /// while in TypedConstant mode. Emits TypedConstant (no interpolation) or
        /// TypedConstantStart/TypedConstantMiddle/TypedConstantEnd (with interpolation).
        /// </summary>
        private void ScanTypedConstantContent()
        {
            int startLine = _line, startCol = _column, startOff = _offset;
            bool hadInterpolation = false;
            int segmentIndex = 0;
            var content = new StringBuilder();

            segmentIndex = CountPriorTypedConstantSegments();
            if (segmentIndex > 0)
                hadInterpolation = true;

            while (!IsAtEnd)
            {
                var c = Current;

                // ── End of line → unterminated ──────────────────────
                if (c == '\n' || c == '\r')
                {
                    EmitTypedConstantSegment(content.ToString(), startLine, startCol, startOff,
                        hadInterpolation, segmentIndex, isFinal: false);
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UnterminatedTypedConstant,
                        new SourceRange(startLine, startCol, _line, _column)));
                    PopMode();
                    return;
                }

                // ── Closing ' ──────────────────────────────────────
                if (c == '\'')
                {
                    Advance(); // consume closing '
                    EmitTypedConstantSegment(content.ToString(), startLine, startCol, startOff,
                        hadInterpolation, segmentIndex, isFinal: true);
                    PopMode();
                    return;
                }

                // ── Escape: \' ─────────────────────────────────────
                if (c == '\\' && PeekNext == '\'')
                {
                    content.Append('\'');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \\ ─────────────────────────────────────
                if (c == '\\' && PeekNext == '\\')
                {
                    content.Append('\\');
                    Advance(); Advance();
                    continue;
                }

                // ── Escaped brace {{ → literal { ───────────────────
                if (c == '{' && PeekNext == '{')
                {
                    content.Append('{');
                    Advance(); Advance();
                    continue;
                }

                // ── Escaped brace }} → literal } ───────────────────
                if (c == '}' && PeekNext == '}')
                {
                    content.Append('}');
                    Advance(); Advance();
                    continue;
                }

                // ── Interpolation start { ──────────────────────────
                if (c == '{')
                {
                    Advance(); // consume '{'
                    hadInterpolation = true;
                    var kind = segmentIndex == 0 ? TokenKind.TypedConstantStart : TokenKind.TypedConstantMiddle;
                    _tokens.Add(new Token(kind, content.ToString(), startLine, startCol, startOff, _offset - startOff));
                    segmentIndex++;

                    if (_modeStack.Count >= MaxModeStackDepth)
                    {
                        _diagnostics.Add(Diagnostics.Create(
                            DiagnosticCode.UnterminatedInterpolation,
                            new SourceRange(_line, _column, _line, _column)));
                        RecoverFromUnterminatedInterpolation();
                        return;
                    }
                    PushMode(LexerMode.Interpolation);
                    return;
                }

                // ── Regular character ──────────────────────────────
                content.Append(c);
                Advance();
            }

            // End of source without closing ' → unterminated
            EmitTypedConstantSegment(content.ToString(), startLine, startCol, startOff,
                hadInterpolation, segmentIndex, isFinal: false);
            _diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.UnterminatedTypedConstant,
                new SourceRange(startLine, startCol, _line, _column)));
            PopMode();
        }

        private void EmitTypedConstantSegment(string text, int startLine, int startCol, int startOff,
            bool hadInterpolation, int segmentIndex, bool isFinal)
        {
            int length = _offset - startOff;
            if (isFinal)
            {
                var kind = hadInterpolation ? TokenKind.TypedConstantEnd : TokenKind.TypedConstant;
                _tokens.Add(new Token(kind, text, startLine, startCol, startOff, length));
            }
            else
            {
                var kind = hadInterpolation
                    ? (segmentIndex == 0 ? TokenKind.TypedConstantStart : TokenKind.TypedConstantMiddle)
                    : TokenKind.TypedConstant;
                _tokens.Add(new Token(kind, text, startLine, startCol, startOff, length));
            }
        }

        /// <summary>
        /// Counts how many StringStart/StringMiddle tokens trail the token list
        /// for the current string (scanning backwards past expression tokens).
        /// Used to determine the segment index when resuming after interpolation.
        /// </summary>
        private int CountPriorStringSegments()
        {
            int count = 0;
            for (int i = _tokens.Count - 1; i >= 0; i--)
            {
                var k = _tokens[i].Kind;
                if (k == TokenKind.StringStart || k == TokenKind.StringMiddle)
                {
                    count++;
                    break; // only need to know if we had at least one
                }
                // Skip expression tokens (identifiers, operators, etc.)
                // but stop if we hit a StringLiteral/StringEnd/TypedConstant* or structural token
                // that can't be part of our current interpolated string.
                if (k == TokenKind.StringLiteral || k == TokenKind.StringEnd
                    || k == TokenKind.TypedConstant || k == TokenKind.TypedConstantEnd
                    || k == TokenKind.NewLine || k == TokenKind.Comment
                    || k == TokenKind.EndOfSource)
                    break;
            }
            return count;
        }

        /// <summary>
        /// Counts how many TypedConstantStart/TypedConstantMiddle tokens trail the token list
        /// for the current typed constant.
        /// </summary>
        private int CountPriorTypedConstantSegments()
        {
            int count = 0;
            for (int i = _tokens.Count - 1; i >= 0; i--)
            {
                var k = _tokens[i].Kind;
                if (k == TokenKind.TypedConstantStart || k == TokenKind.TypedConstantMiddle)
                {
                    count++;
                    break;
                }
                if (k == TokenKind.TypedConstant || k == TokenKind.TypedConstantEnd
                    || k == TokenKind.StringLiteral || k == TokenKind.StringEnd
                    || k == TokenKind.NewLine || k == TokenKind.Comment
                    || k == TokenKind.EndOfSource)
                    break;
            }
            return count;
        }

        /// <summary>
        /// Recovery for unterminated interpolation when max depth is exceeded.
        /// Scan forward for a <c>}</c> at depth 0; if none found before end of line,
        /// resume in the enclosing literal mode.
        /// </summary>
        private void RecoverFromUnterminatedInterpolation()
        {
            while (!IsAtEnd && Current != '\n' && Current != '\r')
            {
                if (Current == '}')
                {
                    Advance();
                    return; // stay in current enclosing mode (String/TypedConstant)
                }
                Advance();
            }
            // Hit end of line — pop back to enclosing literal mode (already there since we didn't push)
        }

        // ── Helpers ────────────────────────────────────────────────

        private void EmitNewLine(int length)
        {
            int startLine = _line, startCol = _column, startOff = _offset;
            for (int i = 0; i < length; i++)
                Advance();
            _tokens.Add(new Token(TokenKind.NewLine, "", startLine, startCol, startOff, length));
            _line++;
            _column = 1;
        }

        private void ScanComment()
        {
            int startLine = _line, startCol = _column, startOff = _offset;
            while (!IsAtEnd && Current != '\n' && Current != '\r')
                Advance();
            int len = _offset - startOff;
            _tokens.Add(new Token(TokenKind.Comment, _source.Substring(startOff, len), startLine, startCol, startOff, len));
        }

        private void ScanWord()
        {
            int startLine = _line, startCol = _column, startOff = _offset;
            Advance();
            while (!IsAtEnd && IsWordChar(Current))
                Advance();
            int len = _offset - startOff;
            var text = _source.Substring(startOff, len);
            var kind = Tokens.Keywords.TryGetValue(text, out var kw) ? kw : TokenKind.Identifier;
            _tokens.Add(new Token(kind, text, startLine, startCol, startOff, len));
        }

        private void ScanNumber()
        {
            int startLine = _line, startCol = _column, startOff = _offset;

            // Integer part
            while (!IsAtEnd && IsDigit(Current))
                Advance();

            // Decimal part: '.' only if followed by a digit
            if (!IsAtEnd && Current == '.' && _offset + 1 < _source.Length && IsDigit(_source[_offset + 1]))
            {
                Advance(); // '.'
                while (!IsAtEnd && IsDigit(Current))
                    Advance();
            }

            // Exponent part: 'e'/'E' only if digits follow (with optional sign)
            if (!IsAtEnd && (Current == 'e' || Current == 'E'))
            {
                int peek = _offset + 1;
                if (peek < _source.Length && (_source[peek] == '+' || _source[peek] == '-'))
                    peek++;
                if (peek < _source.Length && IsDigit(_source[peek]))
                {
                    Advance(); // 'e'/'E'
                    if (!IsAtEnd && (Current == '+' || Current == '-'))
                        Advance();
                    while (!IsAtEnd && IsDigit(Current))
                        Advance();
                }
            }

            int len = _offset - startOff;
            _tokens.Add(new Token(TokenKind.NumberLiteral, _source.Substring(startOff, len), startLine, startCol, startOff, len));
        }

        private bool TryScanOperator()
        {
            var c = Current;
            char next = PeekNext;
            int startLine = _line, startCol = _column, startOff = _offset;

            // Multi-char operators (scan order per §1.5)
            if (c == '-' && next == '>')
                return EmitOperator(TokenKind.Arrow, "->", startLine, startCol, startOff);
            if (c == '=' && next == '=')
                return EmitOperator(TokenKind.DoubleEquals, "==", startLine, startCol, startOff);
            if (c == '!' && next == '=')
                return EmitOperator(TokenKind.NotEquals, "!=", startLine, startCol, startOff);
            if (c == '>' && next == '=')
                return EmitOperator(TokenKind.GreaterThanOrEqual, ">=", startLine, startCol, startOff);
            if (c == '<' && next == '=')
                return EmitOperator(TokenKind.LessThanOrEqual, "<=", startLine, startCol, startOff);

            // Single-char operators
            var singleKind = c switch
            {
                '=' => TokenKind.Assign,
                '>' => TokenKind.GreaterThan,
                '<' => TokenKind.LessThan,
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '%' => TokenKind.Percent,
                _ => (TokenKind?)null,
            };

            if (singleKind is { } sk)
            {
                Advance();
                _tokens.Add(new Token(sk, c.ToString(), startLine, startCol, startOff, 1));
                return true;
            }

            return false;
        }

        private bool EmitOperator(TokenKind kind, string text, int line, int col, int offset)
        {
            Advance();
            Advance();
            _tokens.Add(new Token(kind, text, line, col, offset, 2));
            return true;
        }

        private bool TryScanPunctuation()
        {
            var c = Current;

            var kind = c switch
            {
                '.' => TokenKind.Dot,
                ',' => TokenKind.Comma,
                '(' => TokenKind.LeftParen,
                ')' => TokenKind.RightParen,
                '[' => TokenKind.LeftBracket,
                ']' => TokenKind.RightBracket,
                _ => (TokenKind?)null,
            };

            if (kind is { } k)
            {
                int startLine = _line, startCol = _column, startOff = _offset;
                Advance();
                _tokens.Add(new Token(k, c.ToString(), startLine, startCol, startOff, 1));
                return true;
            }

            return false;
        }
    }
}
