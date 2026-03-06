using System;
using System.Collections.Generic;
using System.Globalization;

namespace Precept;

internal static class PreceptExpressionParser
{
    public static PreceptExpression Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new InvalidOperationException("expression is empty.");

        var parser = new Parser(expression);
        var result = parser.ParseExpression();
        parser.Expect(TokenKind.End);
        return result;
    }

    private sealed class Parser
    {
        private readonly Lexer _lexer;
        private Token _current;
        private Token _previous;

        internal Parser(string source)
        {
            _lexer = new Lexer(source);
            _current = _lexer.NextToken();
            _previous = new Token(TokenKind.End, string.Empty, null);
        }

        internal PreceptExpression ParseExpression() => ParseOr();

        private PreceptExpression ParseOr()
        {
            var left = ParseAnd();
            while (Match(TokenKind.OrOr))
            {
                var op = Previous().Text;
                var right = ParseAnd();
                left = new PreceptBinaryExpression(op, left, right);
            }

            return left;
        }

        private PreceptExpression ParseAnd()
        {
            var left = ParseEquality();
            while (Match(TokenKind.AndAnd))
            {
                var op = Previous().Text;
                var right = ParseEquality();
                left = new PreceptBinaryExpression(op, left, right);
            }

            return left;
        }

        private PreceptExpression ParseEquality()
        {
            var left = ParseComparison();
            while (Match(TokenKind.EqualEqual) || Match(TokenKind.BangEqual))
            {
                var op = Previous().Text;
                var right = ParseComparison();
                left = new PreceptBinaryExpression(op, left, right);
            }

            return left;
        }

        private PreceptExpression ParseComparison()
        {
            var left = ParseTerm();
            while (Match(TokenKind.Greater) || Match(TokenKind.GreaterEqual) || Match(TokenKind.Less) || Match(TokenKind.LessEqual) || Match(TokenKind.Contains))
            {
                var op = Previous().Text;
                var right = ParseTerm();
                left = new PreceptBinaryExpression(op, left, right);
            }

            return left;
        }

        private PreceptExpression ParseTerm()
        {
            var left = ParseFactor();
            while (Match(TokenKind.Plus) || Match(TokenKind.Minus))
            {
                var op = Previous().Text;
                var right = ParseFactor();
                left = new PreceptBinaryExpression(op, left, right);
            }

            return left;
        }

        private PreceptExpression ParseFactor()
        {
            var left = ParseUnary();
            while (Match(TokenKind.Star) || Match(TokenKind.Slash) || Match(TokenKind.Percent))
            {
                var op = Previous().Text;
                var right = ParseUnary();
                left = new PreceptBinaryExpression(op, left, right);
            }

            return left;
        }

        private PreceptExpression ParseUnary()
        {
            if (Match(TokenKind.Bang) || Match(TokenKind.Minus))
            {
                var op = Previous().Text;
                var operand = ParseUnary();
                return new PreceptUnaryExpression(op, operand);
            }

            return ParsePrimary();
        }

        private PreceptExpression ParsePrimary()
        {
            if (Match(TokenKind.Number))
            {
                if (!double.TryParse(Previous().Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    throw Error("invalid number literal.");

                return new PreceptLiteralExpression(number);
            }

            if (Match(TokenKind.String))
                return new PreceptLiteralExpression(Previous().Value);

            if (Match(TokenKind.True))
                return new PreceptLiteralExpression(true);

            if (Match(TokenKind.False))
                return new PreceptLiteralExpression(false);

            if (Match(TokenKind.Null))
                return new PreceptLiteralExpression(null);

            if (Match(TokenKind.Identifier))
            {
                var identifier = Previous().Text;
                if (Match(TokenKind.Dot))
                {
                    if (!Match(TokenKind.Identifier))
                        throw Error("expected identifier after '.'.");

                    var member = Previous().Text;

                    // Support chained dot access for collection properties like CollectionField.count
                    // The first identifier.member is treated as a PreceptIdentifierExpression
                    return new PreceptIdentifierExpression(identifier, member);
                }

                return new PreceptIdentifierExpression(identifier);
            }

            if (Match(TokenKind.LeftParen))
            {
                var inner = ParseExpression();
                Expect(TokenKind.RightParen);
                return new PreceptParenthesizedExpression(inner);
            }

            throw Error($"unexpected token '{_current.Text}'.");
        }

        internal void Expect(TokenKind kind)
        {
            if (_current.Kind != kind)
                throw Error($"expected '{TokenText(kind)}' but found '{_current.Text}'.");

            _previous = _current;
            _current = _lexer.NextToken();
        }

        private bool Match(TokenKind kind)
        {
            if (_current.Kind != kind)
                return false;

            _previous = _current;
            _current = _lexer.NextToken();
            return true;
        }

        private Token Previous() => _previous;

        private InvalidOperationException Error(string message)
            => new($"Expression parse error: {message}");

        private static string TokenText(TokenKind kind)
            => kind switch
            {
                TokenKind.End => "<end>",
                TokenKind.LeftParen => "(",
                TokenKind.RightParen => ")",
                TokenKind.Dot => ".",
                TokenKind.Plus => "+",
                TokenKind.Minus => "-",
                TokenKind.Star => "*",
                TokenKind.Slash => "/",
                TokenKind.Percent => "%",
                TokenKind.Bang => "!",
                TokenKind.BangEqual => "!=",
                TokenKind.EqualEqual => "==",
                TokenKind.Greater => ">",
                TokenKind.GreaterEqual => ">=",
                TokenKind.Less => "<",
                TokenKind.LessEqual => "<=",
                TokenKind.AndAnd => "&&",
                TokenKind.OrOr => "||",
                _ => kind.ToString()
            };
    }

    private sealed class Lexer
    {
        private readonly string _source;
        private int _index;

        internal Lexer(string source)
        {
            _source = source;
        }

        internal Token NextToken()
        {
            SkipWhitespace();
            if (_index >= _source.Length)
                return new Token(TokenKind.End, string.Empty, null);

            var c = _source[_index];

            if (char.IsDigit(c))
                return ReadNumber();

            if (c == '"' || c == '\'')
                return ReadString(c);

            if (char.IsLetter(c) || c == '_')
                return ReadIdentifierOrKeyword();

            _index++;
            return c switch
            {
                '(' => new Token(TokenKind.LeftParen, "(", null),
                ')' => new Token(TokenKind.RightParen, ")", null),
                '.' => new Token(TokenKind.Dot, ".", null),
                '+' => new Token(TokenKind.Plus, "+", null),
                '-' => new Token(TokenKind.Minus, "-", null),
                '*' => new Token(TokenKind.Star, "*", null),
                '/' => new Token(TokenKind.Slash, "/", null),
                '%' => new Token(TokenKind.Percent, "%", null),
                '!' => Peek('=')
                    ? new Token(TokenKind.BangEqual, "!=", null)
                    : new Token(TokenKind.Bang, "!", null),
                '=' => Peek('=')
                    ? new Token(TokenKind.EqualEqual, "==", null)
                    : throw new InvalidOperationException("Expression parse error: unexpected '='; did you mean '=='?"),
                '>' => Peek('=')
                    ? new Token(TokenKind.GreaterEqual, ">=", null)
                    : new Token(TokenKind.Greater, ">", null),
                '<' => Peek('=')
                    ? new Token(TokenKind.LessEqual, "<=", null)
                    : new Token(TokenKind.Less, "<", null),
                '&' when Peek('&') => new Token(TokenKind.AndAnd, "&&", null),
                '|' when Peek('|') => new Token(TokenKind.OrOr, "||", null),
                _ => throw new InvalidOperationException($"Expression parse error: unexpected character '{c}'.")
            };
        }

        private bool Peek(char expected)
        {
            if (_index >= _source.Length || _source[_index] != expected)
                return false;

            _index++;
            return true;
        }

        private Token ReadNumber()
        {
            var start = _index;
            while (_index < _source.Length && char.IsDigit(_source[_index]))
                _index++;

            if (_index < _source.Length && _source[_index] == '.')
            {
                _index++;
                while (_index < _source.Length && char.IsDigit(_source[_index]))
                    _index++;
            }

            if (_index < _source.Length && (_source[_index] == 'e' || _source[_index] == 'E'))
            {
                _index++;
                if (_index < _source.Length && (_source[_index] == '+' || _source[_index] == '-'))
                    _index++;

                while (_index < _source.Length && char.IsDigit(_source[_index]))
                    _index++;
            }

            var text = _source[start.._index];
            return new Token(TokenKind.Number, text, null);
        }

        private Token ReadString(char quote)
        {
            _index++;
            var chars = new List<char>();

            while (_index < _source.Length)
            {
                var c = _source[_index++];
                if (c == quote)
                    return new Token(TokenKind.String, quote + new string(chars.ToArray()) + quote, new string(chars.ToArray()));

                if (c == '\\' && _index < _source.Length)
                {
                    var escaped = _source[_index++];
                    chars.Add(escaped switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '\\' => '\\',
                        '\'' => '\'',
                        '"' => '"',
                        _ => escaped
                    });
                    continue;
                }

                chars.Add(c);
            }

            throw new InvalidOperationException("Expression parse error: unterminated string literal.");
        }

        private Token ReadIdentifierOrKeyword()
        {
            var start = _index;
            _index++;
            while (_index < _source.Length && (char.IsLetterOrDigit(_source[_index]) || _source[_index] == '_'))
                _index++;

            var text = _source[start.._index];
            var kind = text switch
            {
                "true" => TokenKind.True,
                "false" => TokenKind.False,
                "null" => TokenKind.Null,
                "contains" => TokenKind.Contains,
                _ => TokenKind.Identifier
            };

            return new Token(kind, text, null);
        }

        private void SkipWhitespace()
        {
            while (_index < _source.Length && char.IsWhiteSpace(_source[_index]))
                _index++;
        }
    }

    private sealed record Token(TokenKind Kind, string Text, object? Value);

    private enum TokenKind
    {
        End,
        Identifier,
        Number,
        String,
        True,
        False,
        Null,
        Contains,
        LeftParen,
        RightParen,
        Dot,
        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        Bang,
        BangEqual,
        EqualEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
        AndAnd,
        OrOr
    }
}
