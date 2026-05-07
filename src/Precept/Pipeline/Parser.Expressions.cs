using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class Parser
{
    private sealed partial class ParserState
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        //  Pratt Expression Parser
        //
        //  Standard nud/led algorithm driven by binding powers from Operators catalog.
        //  Termination predicates are passed per-slot to stop at context-sensitive
        //  boundaries (when, because, ->, construct leading tokens, etc.).
        // ═══════════════════════════════════════════════════════════════════════════════

        private ParsedExpression ParseExpression(int minBp, Func<bool> terminates)
        {
            var left = ParseNud(terminates);

            while (!terminates() && !IsAtEnd)
            {
                var (ledBp, _) = GetLedBindingPower(Peek().Kind);
                if (ledBp < 0 || ledBp < minBp)
                    break;

                left = ParseLed(left, terminates);
            }

            return left;
        }

        // ── Null denotation (nud) ───────────────────────────────────────────────

        private ParsedExpression ParseNud(Func<bool> terminates)
        {
            var token = Peek();
            switch (token.Kind)
            {
                // ── Literals ────────────────────────────────────────────
                case TokenKind.NumberLiteral:
                case TokenKind.StringLiteral:
                case TokenKind.True:
                case TokenKind.False:
                case TokenKind.TypedConstant:
                    return ParseLiteral();

                // ── Interpolated strings ────────────────────────────────
                case TokenKind.StringStart:
                    return ParseInterpolatedString();

                case TokenKind.TypedConstantStart:
                    return ParseInterpolatedTypedConstant();

                // ── Identifier or FunctionCall ──────────────────────────
                case TokenKind.Identifier:
                    return ParseIdentifierOrFunctionCall(terminates);

                // ── Grouped expression ──────────────────────────────────
                case TokenKind.LeftParen:
                    return ParseGrouped(terminates);

                // ── List literal ────────────────────────────────────────
                case TokenKind.LeftBracket:
                    return ParseListLiteral(terminates);

                // ── Prefix unary: not ───────────────────────────────────
                case TokenKind.Not:
                case TokenKind.Minus:
                    return ParseUnaryOperation(terminates);

                // ── Conditional: if ... then ... else ... ───────────────
                case TokenKind.If:
                    return ParseConditional(terminates);

                // ── Quantifier: each/any/no ─────────────────────────────
                case TokenKind.Each:
                case TokenKind.Any:
                case TokenKind.No:
                    return ParseQuantifier(terminates);

                // ── CI function call: ~functionName(...) ─────────────────
                case TokenKind.Tilde:
                    return ParseCIFunctionCall(terminates);

                default:
                    // Error recovery — emit a diagnostic and produce a placeholder
                    _diagnostics.Add(Language.Diagnostics.Create(
                        DiagnosticCode.ExpectedToken, token.Span, "expression", token.Text));
                    Advance();
                    return new LiteralExpression(TokenKind.True, "true", token.Span);
            }
        }

        // ── Left denotation (led) ───────────────────────────────────────────────

        private ParsedExpression ParseLed(ParsedExpression left, Func<bool> terminates)
        {
            var token = Peek();
            switch (token.Kind)
            {
                // ── Member access / method call ─────────────────────────
                case TokenKind.Dot:
                    return ParseMemberAccessOrMethodCall(left, terminates);

                // ── Postfix: is set / is not set ────────────────────────
                case TokenKind.Is:
                    return ParsePostfixIs(left);

                // ── Binary infix operators ──────────────────────────────
                default:
                    return ParseBinaryInfix(left, terminates);
            }
        }

        [HandlesCatalogMember(ExpressionFormKind.Literal)]
        private ParsedExpression ParseLiteral()
        {
            var token = Advance();
            return new LiteralExpression(token.Kind, token.Text, token.Span);
        }

        [HandlesCatalogMember(ExpressionFormKind.UnaryOperation)]
        private ParsedExpression ParseUnaryOperation(Func<bool> terminates)
        {
            var opToken = Advance();
            var meta = Operators.ByToken[(opToken.Kind, Arity.Unary)];
            var operand = ParseExpression(meta.Precedence, terminates);
            return new UnaryOperationExpression(
                opToken.Kind, operand,
                SourceSpan.Covering(opToken.Span, operand.Span));
        }

        // ── Binding power query for led position ────────────────────────────────

        /// <summary>
        /// Returns (leftBp, rightBp) for a token in led position.
        /// Returns (-1, -1) if the token cannot appear in led position.
        /// </summary>
        private (int LeftBp, int RightBp) GetLedBindingPower(TokenKind kind)
        {
            // Dot (member access) has highest led binding power
            if (kind == TokenKind.Dot)
                return (80, 81);

            // Is (postfix: is set / is not set) — requires full token-sequence check.
            // peek1 == Not is only valid when peek2 == Set; anything else (e.g. "is not valid")
            // must return (-1,-1) to stop the led loop — otherwise ParsePostfixIs bails without
            // consuming tokens and the loop spins forever.
            if (kind == TokenKind.Is)
            {
                var peek1 = Peek(1).Kind;
                if (peek1 == TokenKind.Set ||
                    (peek1 == TokenKind.Not && Peek(2).Kind == TokenKind.Set))
                {
                    var isMeta = Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set);
                    return isMeta != null ? (isMeta.Precedence, int.MaxValue) : (-1, -1);
                }
                return (-1, -1);
            }

            // Single-token binary operators
            if (Operators.ByToken.TryGetValue((kind, Arity.Binary), out var meta))
            {
                var leftBp = meta.Precedence;
                var rightBp = meta.Associativity switch
                {
                    Associativity.Left => meta.Precedence + 1,
                    Associativity.Right => meta.Precedence,
                    Associativity.NonAssociative => int.MaxValue,
                    _ => meta.Precedence + 1,
                };
                return (leftBp, rightBp);
            }

            return (-1, -1);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        //  Expression form sub-parsers
        // ═══════════════════════════════════════════════════════════════════════════════

        [HandlesCatalogMember(ExpressionFormKind.Identifier)]
        [HandlesCatalogMember(ExpressionFormKind.FunctionCall)]
        private ParsedExpression ParseIdentifierOrFunctionCall(Func<bool> terminates)
        {
            var idToken = Advance();
            // Only promote to FunctionCall if '(' is not a termination signal in the outer
            // context (e.g. quantifier collection stops before the predicate parenthesis).
            if (Peek().Kind == TokenKind.LeftParen && !terminates())
            {
                // Function call: name(args)
                Advance(); // consume '('
                var args = ParseArgumentExpressions();
                var closeParen = Expect(TokenKind.RightParen);
                return new FunctionCallExpression(
                    idToken.Text, args,
                    SourceSpan.Covering(idToken.Span, closeParen.Span));
            }
            return new IdentifierExpression(idToken.Text, idToken.Span);
        }

        [HandlesCatalogMember(ExpressionFormKind.Grouped)]
        private ParsedExpression ParseGrouped(Func<bool> terminates)
        {
            var openParen = Advance(); // consume '('
            var inner = ParseExpression(0, () => Peek().Kind == TokenKind.RightParen || terminates());
            var closeParen = Expect(TokenKind.RightParen);
            return new GroupedExpression(
                inner, SourceSpan.Covering(openParen.Span, closeParen.Span));
        }

        [HandlesCatalogMember(ExpressionFormKind.ListLiteral)]
        private ParsedExpression ParseListLiteral(Func<bool> terminates)
        {
            var openBracket = Advance(); // consume '['
            var elements = ImmutableArray.CreateBuilder<ParsedExpression>();

            while (Peek().Kind != TokenKind.RightBracket && !IsAtEnd && !terminates())
            {
                elements.Add(ParseExpression(0,
                    () => Peek().Kind == TokenKind.Comma
                       || Peek().Kind == TokenKind.RightBracket
                       || terminates()));

                if (Peek().Kind == TokenKind.Comma)
                    Advance();
                else
                    break;
            }

            var closeBracket = Expect(TokenKind.RightBracket);
            return new ListLiteralExpression(
                elements.ToImmutable(),
                SourceSpan.Covering(openBracket.Span, closeBracket.Span));
        }

        [HandlesCatalogMember(ExpressionFormKind.Conditional)]
        private ParsedExpression ParseConditional(Func<bool> terminates)
        {
            var ifToken = Advance(); // consume 'if'
            var condition = ParseExpression(0,
                () => Peek().Kind == TokenKind.Then || terminates());
            Expect(TokenKind.Then);
            var thenBranch = ParseExpression(0,
                () => Peek().Kind == TokenKind.Else || terminates());
            Expect(TokenKind.Else);
            var elseBranch = ParseExpression(0, terminates);
            return new ConditionalExpression(
                condition, thenBranch, elseBranch,
                SourceSpan.Covering(ifToken.Span, elseBranch.Span));
        }

        [HandlesCatalogMember(ExpressionFormKind.Quantifier)]
        private ParsedExpression ParseQuantifier(Func<bool> terminates)
        {
            var quantToken = Advance(); // consume each/any/no
            var bindingToken = Expect(TokenKind.Identifier);
            Expect(TokenKind.In);
            var collection = ParseExpression(0,
                () => Peek().Kind == TokenKind.LeftParen || terminates());
            Expect(TokenKind.LeftParen);
            var predicate = ParseExpression(0,
                () => Peek().Kind == TokenKind.RightParen || terminates());
            var closeParen = Expect(TokenKind.RightParen);
            return new QuantifierExpression(
                quantToken.Kind, bindingToken.Text, collection, predicate,
                SourceSpan.Covering(quantToken.Span, closeParen.Span));
        }

        [HandlesCatalogMember(ExpressionFormKind.CIFunctionCall)]
        private ParsedExpression ParseCIFunctionCall(Func<bool> terminates)
        {
            var tildeToken = Advance(); // consume '~'
            var nameToken = Expect(TokenKind.Identifier);
            Expect(TokenKind.LeftParen);
            var args = ParseArgumentExpressions();
            var closeParen = Expect(TokenKind.RightParen);
            return new CIFunctionCallExpression(
                nameToken.Text, args,
                SourceSpan.Covering(tildeToken.Span, closeParen.Span));
        }

        [HandlesCatalogMember(ExpressionFormKind.MemberAccess)]
        [HandlesCatalogMember(ExpressionFormKind.MethodCall)]
        private ParsedExpression ParseMemberAccessOrMethodCall(ParsedExpression left, Func<bool> terminates)
        {
            Advance(); // consume '.'
            var memberToken = Peek();

            // Accept Identifier or IsValidAsMemberName tokens (min, max, countof, peekby)
            if (memberToken.Kind == TokenKind.Identifier || IsMemberNameToken(memberToken.Kind))
            {
                Advance();
                if (Peek().Kind == TokenKind.LeftParen)
                {
                    // Method call: target.method(args)
                    Advance(); // consume '('
                    var args = ParseArgumentExpressions();
                    var closeParen = Expect(TokenKind.RightParen);
                    return new MethodCallExpression(
                        left, memberToken.Kind, memberToken.Text,
                        args, SourceSpan.Covering(left.Span, closeParen.Span));
                }
                return new MemberAccessExpression(
                    left, memberToken.Kind, memberToken.Text,
                    SourceSpan.Covering(left.Span, memberToken.Span));
            }

            // Error: expected member name after dot
            _diagnostics.Add(Language.Diagnostics.Create(
                DiagnosticCode.ExpectedToken, memberToken.Span, "member name", memberToken.Text));
            return new MemberAccessExpression(
                left, TokenKind.Identifier, "",
                SourceSpan.Covering(left.Span, memberToken.Span));
        }

        private static bool IsMemberNameToken(TokenKind kind)
        {
            var meta = Tokens.GetMeta(kind);
            return meta.IsValidAsMemberName;
        }

        [HandlesCatalogMember(ExpressionFormKind.PostfixOperation)]
        private ParsedExpression ParsePostfixIs(ParsedExpression left)
        {
            // Peek ahead: is set / is not set
            var peek1 = Peek(1);
            if (peek1.Kind == TokenKind.Set)
            {
                Advance(); // consume 'is'
                var setToken = Advance(); // consume 'set'
                return new PostfixOperationExpression(
                    left, false,
                    SourceSpan.Covering(left.Span, setToken.Span));
            }

            if (peek1.Kind == TokenKind.Not)
            {
                var peek2 = Peek(2);
                if (peek2.Kind == TokenKind.Set)
                {
                    Advance(); // consume 'is'
                    Advance(); // consume 'not'
                    var setToken = Advance(); // consume 'set'
                    return new PostfixOperationExpression(
                        left, true,
                        SourceSpan.Covering(left.Span, setToken.Span));
                }
            }

            // 'is' without 'set' — not a postfix op, stop the led loop
            return left;
        }

        [HandlesCatalogMember(ExpressionFormKind.BinaryOperation)]
        private ParsedExpression ParseBinaryInfix(ParsedExpression left, Func<bool> terminates)
        {
            var opToken = Peek();
            if (!Operators.ByToken.TryGetValue((opToken.Kind, Arity.Binary), out var meta))
            {
                // Not a recognized binary operator — stop
                return left;
            }

            Advance(); // consume operator token
            var (_, rightBp) = GetLedBindingPower(opToken.Kind);
            var right = ParseExpression(rightBp, terminates);
            return new BinaryOperationExpression(
                left, opToken.Kind, right,
                SourceSpan.Covering(left.Span, right.Span));
        }

        // ── Interpolated string handling ────────────────────────────────────────

        [HandlesCatalogMember(ExpressionFormKind.Literal)]
        private ParsedExpression ParseInterpolatedString()
        {
            var startToken = Advance(); // consume StringStart
            var lastSpan = startToken.Span;

            // Advance through interpolation segments until StringEnd
            while (!IsAtEnd && Peek().Kind != TokenKind.StringEnd)
            {
                lastSpan = Advance().Span;
            }

            if (Peek().Kind == TokenKind.StringEnd)
                lastSpan = Advance().Span;

            return new LiteralExpression(
                TokenKind.StringStart, startToken.Text,
                SourceSpan.Covering(startToken.Span, lastSpan));
        }

        [HandlesCatalogMember(ExpressionFormKind.Literal)]
        private ParsedExpression ParseInterpolatedTypedConstant()
        {
            var startToken = Advance(); // consume TypedConstantStart
            var lastSpan = startToken.Span;

            while (!IsAtEnd && Peek().Kind != TokenKind.TypedConstantEnd)
            {
                lastSpan = Advance().Span;
            }

            if (Peek().Kind == TokenKind.TypedConstantEnd)
                lastSpan = Advance().Span;

            return new LiteralExpression(
                TokenKind.TypedConstantStart, startToken.Text,
                SourceSpan.Covering(startToken.Span, lastSpan));
        }

        // ── Shared helpers ──────────────────────────────────────────────────────

        private ImmutableArray<ParsedExpression> ParseArgumentExpressions()
        {
            var args = ImmutableArray.CreateBuilder<ParsedExpression>();

            while (Peek().Kind != TokenKind.RightParen && !IsAtEnd)
            {
                args.Add(ParseExpression(0,
                    () => Peek().Kind == TokenKind.Comma
                       || Peek().Kind == TokenKind.RightParen));

                if (Peek().Kind == TokenKind.Comma)
                    Advance();
                else
                    break;
            }

            return args.ToImmutable();
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        //  Slot-level entry points — called from Parser.cs slot dispatch
        // ═══════════════════════════════════════════════════════════════════════════════

        private SlotValue ParseRuleExpression(ConstructSlot slot)
        {
            if (IsAtConstructBoundary() && !slot.IsRequired)
                return MakeSentinel(slot);

            var startSpan = Peek().Span;
            var expr = ParseExpression(0,
                () => Peek().Kind == TokenKind.When
                   || Peek().Kind == TokenKind.Because
                   || IsAtConstructBoundary());

            if (expr.Span == SourceSpan.Missing && !slot.IsRequired)
                return MakeSentinel(slot);

            return new RuleExpressionSlot(expr, expr.Span);
        }

        private SlotValue ParseGuardClause(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.When)
                return MakeSentinel(slot);

            var whenToken = Advance(); // consume 'when'

            if (IsAtEnd || IsAtConstructBoundary())
            {
                var emptyExpr = new LiteralExpression(TokenKind.True, "true", whenToken.Span);
                return new GuardClauseSlot(emptyExpr, whenToken.Span);
            }

            var expr = ParseExpression(0,
                () => Peek().Kind == TokenKind.Because
                   || Peek().Kind == TokenKind.Arrow
                   || IsAtConstructBoundary());

            return new GuardClauseSlot(expr,
                SourceSpan.Covering(whenToken.Span, expr.Span));
        }

        private SlotValue ParseComputeExpression(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.BackArrow)
                return MakeSentinel(slot);

            var arrowToken = Advance(); // consume '<-'
            var nextToken = Peek();

            if (!ExpressionStartTokens.Contains(nextToken.Kind))
            {
                _diagnostics.Add(Language.Diagnostics.Create(
                    DiagnosticCode.ExpectedToken, nextToken.Span, "expression", nextToken.Text));

                while (!IsAtEnd && !IsAtConstructBoundary())
                    Advance();

                return MakeSentinel(slot);
            }

            var expr = ParseExpression(0, () => IsAtConstructBoundary());

            return new ComputeExpressionSlot(expr,
                SourceSpan.Covering(arrowToken.Span, expr.Span));
        }

        private SlotValue ParseEnsureClause(ConstructSlot slot)
        {
            SourceSpan startRef;

            if (Peek().Kind == TokenKind.Ensure)
            {
                startRef = Advance().Span;
            }
            else if (slot.IsRequired)
            {
                startRef = Peek().Span;
            }
            else
            {
                return MakeSentinel(slot);
            }

            if (IsAtEnd || (IsAtConstructBoundary() && Peek().Kind != TokenKind.Identifier))
            {
                var emptyExpr = new LiteralExpression(TokenKind.True, "true", startRef);
                return new EnsureClauseSlot(emptyExpr, startRef);
            }

            var expr = ParseExpression(0,
                () => Peek().Kind == TokenKind.Because
                   || IsAtConstructBoundary());

            return new EnsureClauseSlot(expr,
                SourceSpan.Covering(startRef, expr.Span));
        }

        private SlotValue ParseOutcome(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.Arrow)
                return MakeSentinel(slot);

            var arrowToken = Advance(); // consume '->'
            var outcomeToken = Peek();

            switch (outcomeToken.Kind)
            {
                case TokenKind.Transition:
                {
                    // -> transition StateName
                    var transToken = Advance();
                    var stateToken = Peek();
                    if (stateToken.Kind == TokenKind.Identifier)
                    {
                        Advance();
                        var span = SourceSpan.Covering(arrowToken.Span, stateToken.Span);
                        return new OutcomeSlot(new TransitionOutcome(stateToken.Text, span), span);
                    }
                    // transition without state name — malformed
                    var malSpan = SourceSpan.Covering(arrowToken.Span, transToken.Span);
                    return new OutcomeSlot(new MalformedOutcome(malSpan), malSpan);
                }

                case TokenKind.No:
                {
                    // -> no transition
                    var noToken = Advance();
                    if (Peek().Kind == TokenKind.Transition)
                    {
                        var transToken = Advance();
                        var span = SourceSpan.Covering(arrowToken.Span, transToken.Span);
                        return new OutcomeSlot(new NoTransitionOutcome(span), span);
                    }
                    // 'no' without 'transition' — malformed
                    var malSpan = SourceSpan.Covering(arrowToken.Span, noToken.Span);
                    return new OutcomeSlot(new MalformedOutcome(malSpan), malSpan);
                }

                case TokenKind.Reject:
                {
                    // -> reject "reason"
                    var rejectToken = Advance();
                    if (Peek().Kind == TokenKind.StringLiteral)
                    {
                        var reasonToken = Advance();
                        var span = SourceSpan.Covering(arrowToken.Span, reasonToken.Span);
                        return new OutcomeSlot(new RejectOutcome(reasonToken.Text, span), span);
                    }
                    // reject without reason — malformed
                    var malSpan = SourceSpan.Covering(arrowToken.Span, rejectToken.Span);
                    return new OutcomeSlot(new MalformedOutcome(malSpan), malSpan);
                }

                default:
                {
                    // Unexpected token after arrow — malformed outcome
                    var span = arrowToken.Span;
                    return new OutcomeSlot(new MalformedOutcome(span), span);
                }
            }
        }
    }
}
