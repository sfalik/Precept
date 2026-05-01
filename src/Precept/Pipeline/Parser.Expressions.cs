using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline.SyntaxNodes;

namespace Precept.Pipeline;

public static partial class Parser
{
    internal ref partial struct ParseSession
    {
        /// <summary>
        /// Like <see cref="Expect(TokenKind)"/> for an identifier, but also accepts
        /// keyword tokens listed in <see cref="KeywordsValidAsMemberName"/> so that
        /// <c>.min</c> and <c>.max</c> parse as member accesses rather than syntax errors.
        /// </summary>
        private Token ExpectIdentifierOrKeywordAsMemberName()
        {
            var cur = Current();
            if (cur.Kind == TokenKind.Identifier || KeywordsValidAsMemberName.Contains(cur.Kind))
                return Advance();
            _diagnostics.Add(Diagnostics.Create(DiagnosticCode.ExpectedToken, cur.Span, TokenKind.Identifier, cur.Text));
            return new Token(TokenKind.Identifier, string.Empty, cur.Span);
        }

        // ── Expression parser (Pratt) ─────────────────────────────────────────

        [HandlesForm(ExpressionFormKind.MemberAccess)]
        [HandlesForm(ExpressionFormKind.BinaryOperation)]
        [HandlesForm(ExpressionFormKind.MethodCall)]
        [HandlesForm(ExpressionFormKind.PostfixOperation)]
        internal Expression ParseExpression(int minPrecedence)
        {
            var left = ParseAtom();

            while (true)
            {
                var current = Current();

                // Natural termination: boundary tokens or end-of-source
                if (ExpressionBoundaryTokens.Contains(current.Kind))
                    break;

                // Member access (dot) — highest binary precedence
                if (current.Kind == TokenKind.Dot)
                {
                    if (minPrecedence > 80) break;
                    Advance(); // consume '.'
                    // GAP-C fix: keywords valid as member names (e.g. .min, .max)
                    var member = ExpectIdentifierOrKeywordAsMemberName();
                    left = new MemberAccessExpression(
                        SourceSpan.Covering(left.Span, member.Span), left, member);
                    continue;
                }

                // is set / is not set — postfix null-check (binding power 60, non-associative)
                // Precedence 60 — matches Operators.GetMeta(OperatorKind.IsSet).Precedence. Spec §2.1.
                if (current.Kind == TokenKind.Is)
                {
                    if (minPrecedence > 60) break;
                    Advance(); // consume 'is'
                    if (Current().Kind == TokenKind.Not)
                    {
                        Advance(); // consume 'not'
                        var setTok = Expect(TokenKind.Set);
                        left = new IsNotSetExpression(SourceSpan.Covering(left.Span, setTok.Span), left);
                    }
                    else
                    {
                        var setTok = Expect(TokenKind.Set);
                        left = new IsSetExpression(SourceSpan.Covering(left.Span, setTok.Span), left);
                    }
                    continue;
                }

                // Method call — '(' following a MemberAccessExpression (binding power 90)
                // Precedence 90 — tighter than dot-access (80) per spec §2.1.
                if (current.Kind == TokenKind.LeftParen)
                {
                    if (left is MemberAccessExpression memberAccess)
                    {
                        if (minPrecedence > 90) break;
                        Advance(); // consume '('
                        var args = ImmutableArray.CreateBuilder<Expression>();
                        if (Current().Kind != TokenKind.RightParen)
                        {
                            do
                            {
                                args.Add(ParseExpression(0));
                            }
                            while (Match(TokenKind.Comma));
                        }
                        var closeParen = Expect(TokenKind.RightParen);
                        left = new MethodCallExpression(
                            SourceSpan.Covering(left.Span, closeParen.Span),
                            memberAccess.Object,
                            memberAccess.Member.Text!,
                            args.ToImmutable());
                        continue;
                    }
                    // unreachable: identifiers resolve as FunctionCall in ParseAtom
                    break;
                }

                // Binary operator — check precedence table
                if (!OperatorPrecedence.TryGetValue(current.Kind, out var opInfo))
                    break;

                if (opInfo.Precedence < minPrecedence)
                    break;

                // Non-associative operators (comparisons) — detect chaining
                var meta = Operators.ByToken.GetValueOrDefault((current.Kind, Arity.Binary));
                if (meta?.Associativity == Associativity.NonAssociative)
                {
                    if (left is BinaryExpression prevBin)
                    {
                        var prevMeta = Operators.ByToken.GetValueOrDefault((prevBin.Operator.Kind, Arity.Binary));
                        if (prevMeta?.Associativity == Associativity.NonAssociative)
                        {
                            EmitDiagnostic(DiagnosticCode.NonAssociativeComparison, current.Span,
                                "use 'and' to combine comparisons");
                            break;
                        }
                    }
                }

                var opToken = Advance();
                int nextMinPrec = opInfo.RightAssociative ? opInfo.Precedence : opInfo.Precedence + 1;
                var right = ParseExpression(nextMinPrec);
                left = new BinaryExpression(
                    SourceSpan.Covering(left.Span, right.Span), left, opToken, right);
            }

            return left;
        }

        [HandlesForm(ExpressionFormKind.Literal)]
        [HandlesForm(ExpressionFormKind.Identifier)]
        [HandlesForm(ExpressionFormKind.Grouped)]
        [HandlesForm(ExpressionFormKind.UnaryOperation)]
        [HandlesForm(ExpressionFormKind.Conditional)]
        [HandlesForm(ExpressionFormKind.FunctionCall)]
        [HandlesForm(ExpressionFormKind.ListLiteral)]
        private Expression ParseAtom()
        {
            var current = Current();

            switch (current.Kind)
            {
                case TokenKind.NumberLiteral:
                case TokenKind.StringLiteral:
                    return new LiteralExpression(current.Span, Advance());

                case TokenKind.True:
                case TokenKind.False:
                    return new LiteralExpression(current.Span, Advance());

                // Interpolated string: StringStart expr (StringMiddle expr)* StringEnd
                case TokenKind.StringStart:
                    return ParseInterpolatedString();

                // Typed constant literal: 'USD', '2026-04-15'
                case TokenKind.TypedConstant:
                    return new TypedConstantExpression(current.Span, Advance());

                // Interpolated typed constant: 'Hello {name}'
                case TokenKind.TypedConstantStart:
                    return ParseInterpolatedTypedConstant();

                case TokenKind.Identifier:
                // GAP-C fix: min/max are keywords but can also appear as function names
                // in expression position: min(a, b) or max(a, b).
                case TokenKind.Min:
                case TokenKind.Max:
                {
                    var name = Advance();
                    // Function call: name(args...)
                    if (Current().Kind == TokenKind.LeftParen)
                    {
                        Advance(); // consume '('
                        var args = ImmutableArray.CreateBuilder<Expression>();
                        if (Current().Kind != TokenKind.RightParen)
                        {
                            do
                            {
                                args.Add(ParseExpression(0));
                            }
                            while (Match(TokenKind.Comma));
                        }
                        var closeParen = Expect(TokenKind.RightParen);
                        return new CallExpression(
                            SourceSpan.Covering(name.Span, closeParen.Span), name, args.ToImmutable());
                    }
                    return new IdentifierExpression(name.Span, name);
                }

                case TokenKind.Not:
                {
                    var op = Advance();
                    var operand = ParseExpression(25); // not precedence
                    return new UnaryExpression(
                        SourceSpan.Covering(op.Span, operand.Span), op, operand);
                }

                case TokenKind.Minus:
                {
                    var op = Advance();
                    var operand = ParseExpression(65); // negate precedence
                    // Constant-fold: -<NumberLiteral> → signed LiteralExpression
                    if (operand is LiteralExpression { Value.Kind: TokenKind.NumberLiteral } lit)
                    {
                        var negText = lit.Value.Text!.StartsWith('-')
                            ? lit.Value.Text[1..]       // --1 → "1"
                            : "-" + lit.Value.Text;     // -1  → "-1"
                        var span = SourceSpan.Covering(op.Span, lit.Span);
                        return new LiteralExpression(span, new Token(TokenKind.NumberLiteral, negText, span));
                    }
                    return new UnaryExpression(
                        SourceSpan.Covering(op.Span, operand.Span), op, operand);
                }

                case TokenKind.LeftParen:
                {
                    var open = Advance();
                    var inner = ParseExpression(0);
                    var close = Expect(TokenKind.RightParen);
                    return new ParenthesizedExpression(
                        SourceSpan.Covering(open.Span, close.Span), inner);
                }

                case TokenKind.If:
                {
                    var ifToken = Advance();
                    var condition = ParseExpression(0);
                    Expect(TokenKind.Then);
                    var whenTrue = ParseExpression(0);
                    Expect(TokenKind.Else);
                    var whenFalse = ParseExpression(0);
                    return new ConditionalExpression(
                        SourceSpan.Covering(ifToken.Span, whenFalse.Span),
                        condition, whenTrue, whenFalse);
                }

                case TokenKind.LeftBracket:
                    return ParseListLiteral();

                default:
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, current.Span, "expression", current.Text);
                    return new IdentifierExpression(current.Span,
                        new Token(TokenKind.Identifier, string.Empty, current.Span));
            }
        }

        private InterpolatedStringExpression ParseInterpolatedString()
        {
            var parts = ImmutableArray.CreateBuilder<InterpolationPart>();
            var startToken = Advance(); // consume StringStart
            parts.Add(new TextInterpolationPart(startToken.Span, startToken));

            while (Current().Kind != TokenKind.StringEnd && !IsAtEnd())
            {
                // Parse expression hole
                var expr = ParseExpression(0);
                parts.Add(new ExpressionInterpolationPart(expr.Span, expr));

                // Parse middle text segment if present
                if (Current().Kind == TokenKind.StringMiddle)
                {
                    var mid = Advance();
                    parts.Add(new TextInterpolationPart(mid.Span, mid));
                }
            }

            var endToken = Current().Kind == TokenKind.StringEnd ? Advance() : Current();
            parts.Add(new TextInterpolationPart(endToken.Span, endToken));

            return new InterpolatedStringExpression(
                SourceSpan.Covering(startToken.Span, endToken.Span), parts.ToImmutable());
        }

        private InterpolatedTypedConstantExpression ParseInterpolatedTypedConstant()
        {
            var parts = ImmutableArray.CreateBuilder<InterpolationPart>();
            var startToken = Advance(); // consume TypedConstantStart
            parts.Add(new TextInterpolationPart(startToken.Span, startToken));

            while (Current().Kind != TokenKind.TypedConstantEnd && !IsAtEnd())
            {
                // Parse expression hole
                var expr = ParseExpression(0);
                parts.Add(new ExpressionInterpolationPart(expr.Span, expr));

                // Parse middle text segment if present
                if (Current().Kind == TokenKind.TypedConstantMiddle)
                {
                    var mid = Advance();
                    parts.Add(new TextInterpolationPart(mid.Span, mid));
                }
            }

            var endToken = Current().Kind == TokenKind.TypedConstantEnd ? Advance() : Current();
            parts.Add(new TextInterpolationPart(endToken.Span, endToken));

            return new InterpolatedTypedConstantExpression(
                SourceSpan.Covering(startToken.Span, endToken.Span), parts.ToImmutable());
        }

        private ListLiteralExpression ParseListLiteral()
        {
            var openBracket = Advance(); // consume '['
            var elements = ImmutableArray.CreateBuilder<Expression>();

            if (Current().Kind != TokenKind.RightBracket)
            {
                do
                {
                    if (Current().Kind == TokenKind.RightBracket) break; // trailing comma
                    elements.Add(ParseExpression(0));
                }
                while (Match(TokenKind.Comma));
            }

            var closeBracket = Expect(TokenKind.RightBracket);
            return new ListLiteralExpression(
                SourceSpan.Covering(openBracket.Span, closeBracket.Span),
                elements.ToImmutable());
        }
    }
}
