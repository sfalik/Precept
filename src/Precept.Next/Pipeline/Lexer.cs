using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;

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

    /// <summary>
    /// Returns a display-safe string for <paramref name="c"/>.
    /// Printable characters are returned as-is; control and non-printable
    /// characters are formatted as <c>\uXXXX</c> so they are visible in
    /// diagnostic messages and IDE tooltips.
    /// </summary>
    private static string DisplayChar(char c) =>
        char.IsControl(c) || char.GetUnicodeCategory(c) is
            System.Globalization.UnicodeCategory.OtherNotAssigned or
            System.Globalization.UnicodeCategory.Format
            ? $"\\u{(int)c:X4}"
            : c.ToString();

    private enum LexerMode
    {
        Normal,
        String,
        TypedConstant,
        Interpolation,
    }

    private struct ModeState
    {
        public LexerMode Mode;
        public int SegmentIndex;
        // Span origin for the current segment — includes the opening delimiter (" or ')
        // on the first segment, and the closing } on subsequent segments.
        public int SegStartOffset;
        public int SegStartLine;
        public int SegStartColumn;
    }

    private struct Scanner
    {
        private readonly string _source;
        private int _offset;
        private int _line;
        private int _column;
        private readonly ImmutableArray<Token>.Builder _tokens;
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;
        private readonly ModeState[] _modeStack;
        private int _modeDepth;
        private readonly FrozenDictionary<string, TokenKind>.AlternateLookup<ReadOnlySpan<char>> _keywordLookup;
        private char[] _contentBuffer;
        private int _contentLength;

        public Scanner(string source)
        {
            _source = source;
            _offset = 0;
            _line = 1;
            _column = 1;
            _tokens = ImmutableArray.CreateBuilder<Token>();
            _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            _modeStack = new ModeState[MaxModeStackDepth];
            _modeStack[0] = new ModeState { Mode = LexerMode.Normal };
            _modeDepth = 1;
            _keywordLookup = Tokens.Keywords.GetAlternateLookup<ReadOnlySpan<char>>();
            _contentBuffer = new char[128];
            _contentLength = 0;
        }

        private void ResetContent() => _contentLength = 0;

        private void AppendContent(char c)
        {
            if (_contentLength == _contentBuffer.Length)
                Array.Resize(ref _contentBuffer, _contentBuffer.Length * 2);
            _contentBuffer[_contentLength++] = c;
        }

        private string ContentToString() =>
            new string(_contentBuffer.AsSpan(0, _contentLength));

        private bool IsAtEnd => _offset >= _source.Length;

        private char Current => _source[_offset];

        private char PeekNext =>
            _offset + 1 < _source.Length ? _source[_offset + 1] : '\0';

        private LexerMode CurrentMode => _modeStack[_modeDepth - 1].Mode;

        private void PushMode(LexerMode mode, int segStartOffset, int segStartLine, int segStartColumn)
        {
            // The mode stack alternates literal ↔ Interpolation. A literal can only
            // push Interpolation, and Interpolation can only push a literal. The depth
            // check in each literal scanner (>= MaxModeStackDepth) prevents overflow
            // before we get here, so this assert should never fire in production.
            Debug.Assert(_modeDepth < MaxModeStackDepth, "Mode stack overflow — alternating-parity invariant violated");
            _modeStack[_modeDepth++] = new ModeState
            {
                Mode = mode,
                SegStartOffset = segStartOffset,
                SegStartLine = segStartLine,
                SegStartColumn = segStartColumn,
            };
        }

        private void PopMode()
        {
            if (_modeDepth > 1)
                _modeDepth--;
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
            // Emit diagnostics for any modes still open at EOF (innermost first).
            // Normal (depth 0) is always present and needs no diagnostic.
            for (int i = _modeDepth - 1; i >= 1; i--)
            {
                ref var s = ref _modeStack[i];
                var range = new SourceRange(s.SegStartLine, s.SegStartColumn, _line, _column);
                var code = s.Mode switch
                {
                    LexerMode.Interpolation  => DiagnosticCode.UnterminatedInterpolation,
                    LexerMode.String         => DiagnosticCode.UnterminatedStringLiteral,
                    LexerMode.TypedConstant  => DiagnosticCode.UnterminatedTypedConstant,
                    _                        => DiagnosticCode.UnterminatedStringLiteral,
                };
                _diagnostics.Add(Diagnostics.Create(code, range));
            }

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
            // In Interpolation mode a newline terminates the expression (spec §1.8).
            // Emit the diagnostic, pop back to the enclosing literal mode, and leave
            // the newline unconsumed so that mode's content scanner fires its own
            // unterminated diagnostic on the same character.
            if ((c == '\n' || c == '\r') && CurrentMode == LexerMode.Interpolation)
            {
                ref var interp = ref _modeStack[_modeDepth - 1];
                _diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UnterminatedInterpolation,
                    new SourceRange(interp.SegStartLine, interp.SegStartColumn, _line, _column)));
                PopMode(); // back to String or TypedConstant — newline left unconsumed
                return;
            }
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
                // Capture } position before advancing so the next segment's span includes it.
                int closeOff = _offset, closeLine = _line, closeCol = _column;
                Advance(); // consume '}'
                PopMode(); // back to String or TypedConstant
                // Update enclosing mode's segment span origin to start at the '}'.
                ref var enclosing = ref _modeStack[_modeDepth - 1];
                enclosing.SegStartOffset = closeOff;
                enclosing.SegStartLine = closeLine;
                enclosing.SegStartColumn = closeCol;
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
                DisplayChar(c)));
            Advance();
        }

        // ── String / Typed-constant scanning ───────────────────────

        private void BeginString()
        {
            // Capture position of '"' before advancing so the token span includes it.
            int delimOff = _offset, delimLine = _line, delimCol = _column;
            Advance(); // consume opening "
            _modeStack[_modeDepth++] = new ModeState
            {
                Mode = LexerMode.String,
                SegStartOffset = delimOff,
                SegStartLine = delimLine,
                SegStartColumn = delimCol,
            };
        }

        private void BeginTypedConstant()
        {
            // Capture position of '\'' before advancing so the token span includes it.
            int delimOff = _offset, delimLine = _line, delimCol = _column;
            Advance(); // consume opening '
            _modeStack[_modeDepth++] = new ModeState
            {
                Mode = LexerMode.TypedConstant,
                SegStartOffset = delimOff,
                SegStartLine = delimLine,
                SegStartColumn = delimCol,
            };
        }

        /// <summary>
        /// Scans content inside a <c>"..."</c> string. Called repeatedly from ScanAll
        /// while in String mode. Emits StringLiteral (no interpolation) or
        /// StringStart/StringMiddle/StringEnd (with interpolation).
        /// </summary>
        private void ScanStringContent()
        {
            ref var state = ref _modeStack[_modeDepth - 1];
            int startOff = state.SegStartOffset, startLine = state.SegStartLine, startCol = state.SegStartColumn;
            int segmentIndex = state.SegmentIndex;
            bool hadInterpolation = segmentIndex > 0;
            ResetContent();

            while (!IsAtEnd)
            {
                var c = Current;

                // ── End of line → unterminated ──────────────────────
                if (c == '\n' || c == '\r')
                {
                    EmitStringSegment(ContentToString(), startLine, startCol, startOff,
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
                    EmitStringSegment(ContentToString(), startLine, startCol, startOff,
                        hadInterpolation, segmentIndex, isFinal: true);
                    PopMode();
                    return;
                }

                // ── Escape: \" ─────────────────────────────────────
                if (c == '\\' && PeekNext == '"')
                {
                    AppendContent('"');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \\ ─────────────────────────────────────
                if (c == '\\' && PeekNext == '\\')
                {
                    AppendContent('\\');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \n ─────────────────────────────────────
                if (c == '\\' && PeekNext == 'n')
                {
                    AppendContent('\n');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \t ─────────────────────────────────────
                if (c == '\\' && PeekNext == 't')
                {
                    AppendContent('\t');
                    Advance(); Advance();
                    continue;
                }

                // ── Unrecognized escape → diagnostic ───────────────
                if (c == '\\')
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UnrecognizedStringEscape,
                        new SourceRange(_line, _column, _line, _column),
                        DisplayChar(PeekNext)));
                    Advance(); Advance(); // skip \ and the unrecognized char
                    continue;
                }

                // ── Escaped brace {{ → literal { ───────────────────
                if (c == '{' && PeekNext == '{')
                {
                    AppendContent('{');
                    Advance(); Advance();
                    continue;
                }

                // ── Escaped brace }} → literal } ───────────────────
                if (c == '}' && PeekNext == '}')
                {
                    AppendContent('}');
                    Advance(); Advance();
                    continue;
                }

                // ── Lone } → diagnostic (use }} for a literal }) ─────
                if (c == '}')
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UnescapedBraceInLiteral,
                        new SourceRange(_line, _column, _line, _column)));
                    AppendContent(c); // preserve in segment text for recovery
                    Advance();
                    continue;
                }

                // ── Interpolation start { ──────────────────────────
                if (c == '{')
                {
                    int braceOff = _offset, braceLine = _line, braceCol = _column;
                    Advance(); // consume '{'
                    hadInterpolation = true;
                    var kind = segmentIndex == 0 ? TokenKind.StringStart : TokenKind.StringMiddle;
                    _tokens.Add(new Token(kind, ContentToString(), startLine, startCol, startOff, _offset - startOff));
                    _modeStack[_modeDepth - 1].SegmentIndex = segmentIndex + 1;

                    if (_modeDepth >= MaxModeStackDepth)
                    {
                        _diagnostics.Add(Diagnostics.Create(
                            DiagnosticCode.UnterminatedInterpolation,
                            new SourceRange(_line, _column, _line, _column)));
                        RecoverFromUnterminatedInterpolation();
                        return;
                    }
                    PushMode(LexerMode.Interpolation, braceOff, braceLine, braceCol);
                    return;
                }

                // ── Regular character ──────────────────────────────
                AppendContent(c);
                Advance();
            }

            // End of source without closing " → unterminated
            EmitStringSegment(ContentToString(), startLine, startCol, startOff,
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
            ref var state = ref _modeStack[_modeDepth - 1];
            int startOff = state.SegStartOffset, startLine = state.SegStartLine, startCol = state.SegStartColumn;
            int segmentIndex = state.SegmentIndex;
            bool hadInterpolation = segmentIndex > 0;
            ResetContent();

            while (!IsAtEnd)
            {
                var c = Current;

                // ── End of line → unterminated ──────────────────────
                if (c == '\n' || c == '\r')
                {
                    EmitTypedConstantSegment(ContentToString(), startLine, startCol, startOff,
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
                    EmitTypedConstantSegment(ContentToString(), startLine, startCol, startOff,
                        hadInterpolation, segmentIndex, isFinal: true);
                    PopMode();
                    return;
                }

                // ── Escape: \' ─────────────────────────────────────
                if (c == '\\' && PeekNext == '\'')
                {
                    AppendContent('\'');
                    Advance(); Advance();
                    continue;
                }

                // ── Escape: \\ ─────────────────────────────────────
                if (c == '\\' && PeekNext == '\\')
                {
                    AppendContent('\\');
                    Advance(); Advance();
                    continue;
                }

                // ── Unrecognized escape → diagnostic ───────────────
                // (\n and \t are not valid in typed constants)
                if (c == '\\')
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UnrecognizedTypedConstantEscape,
                        new SourceRange(_line, _column, _line, _column),
                        DisplayChar(PeekNext)));
                    Advance(); Advance(); // skip \ and the unrecognized char
                    continue;
                }

                // ── Escaped brace {{ → literal { ───────────────────
                if (c == '{' && PeekNext == '{')
                {
                    AppendContent('{');
                    Advance(); Advance();
                    continue;
                }

                // ── Escaped brace }} → literal } ───────────────────
                if (c == '}' && PeekNext == '}')
                {
                    AppendContent('}');
                    Advance(); Advance();
                    continue;
                }

                // ── Lone } → diagnostic (use }} for a literal }) ─────
                if (c == '}')
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UnescapedBraceInLiteral,
                        new SourceRange(_line, _column, _line, _column)));
                    AppendContent(c); // preserve in segment text for recovery
                    Advance();
                    continue;
                }

                // ── Interpolation start { ──────────────────────────
                if (c == '{')
                {
                    int braceOff = _offset, braceLine = _line, braceCol = _column;
                    Advance(); // consume '{'
                    hadInterpolation = true;
                    var kind = segmentIndex == 0 ? TokenKind.TypedConstantStart : TokenKind.TypedConstantMiddle;
                    _tokens.Add(new Token(kind, ContentToString(), startLine, startCol, startOff, _offset - startOff));
                    _modeStack[_modeDepth - 1].SegmentIndex = segmentIndex + 1;

                    if (_modeDepth >= MaxModeStackDepth)
                    {
                        _diagnostics.Add(Diagnostics.Create(
                            DiagnosticCode.UnterminatedInterpolation,
                            new SourceRange(_line, _column, _line, _column)));
                        RecoverFromUnterminatedInterpolation();
                        return;
                    }
                    PushMode(LexerMode.Interpolation, braceOff, braceLine, braceCol);
                    return;
                }

                // ── Regular character ──────────────────────────────
                AppendContent(c);
                Advance();
            }

            // End of source without closing ' → unterminated
            EmitTypedConstantSegment(ContentToString(), startLine, startCol, startOff,
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
                    return; // stay in enclosing literal mode — content scanning resumes
                }
                Advance();
            }
            // Hit end of line without finding } — pop the enclosing literal mode
            // so we don't re-enter string/typed-constant scanning and emit a
            // second unterminated diagnostic for the same line.
            PopMode();
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
            _tokens.Add(new Token(TokenKind.Comment, _source.AsSpan(startOff, len).ToString(), startLine, startCol, startOff, len));
        }

        private void ScanWord()
        {
            int startLine = _line, startCol = _column, startOff = _offset;
            Advance();
            while (!IsAtEnd && IsWordChar(Current))
                Advance();
            int len = _offset - startOff;
            var span = _source.AsSpan(startOff, len);
            var kind = _keywordLookup.TryGetValue(span, out var kw) ? kw : TokenKind.Identifier;
            _tokens.Add(new Token(kind, span.ToString(), startLine, startCol, startOff, len));
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
            _tokens.Add(new Token(TokenKind.NumberLiteral, _source.AsSpan(startOff, len).ToString(), startLine, startCol, startOff, len));
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
