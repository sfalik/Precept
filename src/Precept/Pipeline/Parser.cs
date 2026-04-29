using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline.SyntaxNodes;

namespace Precept.Pipeline;

/// <summary>
/// Transforms a <see cref="TokenStream"/> into a <see cref="SyntaxTree"/>.
/// </summary>
/// <remarks>
/// The parser is a flat, keyword-dispatched recursive descent parser. The top-level
/// loop dispatches on a fixed set of leading tokens; each token routes to a dedicated
/// parse method that owns its grammar production. Scoped constructs beginning with
/// <c>In</c>, <c>To</c>, <c>From</c>, or <c>On</c> require one-token lookahead past
/// the anchor target to disambiguate the ConstructKind.
///
/// Vocabulary recognition sets (operators, types, modifiers, actions) are derived
/// from catalog metadata at startup and must not be duplicated as hardcoded lists here.
/// See <c>docs/language/catalog-system.md</c> § Pipeline Stage Impact for the
/// authoritative boundary between hand-written grammar and catalog-driven vocabulary.
/// </remarks>
public static class Parser
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Vocabulary FrozenDictionaries — derived from catalog metadata (Layer A)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Operator precedence and associativity, keyed by <see cref="TokenKind"/>.
    /// Derived from <see cref="Operators.All"/>. Binary operators only (unary
    /// handled by prefix binding power in the Pratt loop).
    /// </summary>
    internal static readonly FrozenDictionary<TokenKind, (int Precedence, bool RightAssociative)> OperatorPrecedence =
        Operators.All
            .Where(op => op.Arity == Arity.Binary)
            .ToFrozenDictionary(
                op => op.Token.Kind,
                op => (op.Precedence, op.Associativity == Associativity.Right));

    /// <summary>
    /// Token kinds that introduce a type in <c>as Type</c> position.
    /// Derived from <see cref="Types.ByToken"/>.
    /// </summary>
    internal static readonly FrozenSet<TokenKind> TypeKeywords =
        Types.ByToken.Keys.ToFrozenSet();

    /// <summary>
    /// Token kinds that are field-level modifiers (both flag and value-bearing).
    /// Derived from <see cref="Modifiers.All"/> where the modifier is a <see cref="FieldModifierMeta"/>.
    /// </summary>
    internal static readonly FrozenSet<TokenKind> ModifierKeywords =
        Modifiers.All
            .OfType<FieldModifierMeta>()
            .Select(m => m.Token.Kind)
            .ToFrozenSet();

    /// <summary>
    /// Token kinds that are state-level modifiers.
    /// Derived from <see cref="Modifiers.All"/> where the modifier is a <see cref="StateModifierMeta"/>.
    /// </summary>
    internal static readonly FrozenSet<TokenKind> StateModifierKeywords =
        Modifiers.All
            .OfType<StateModifierMeta>()
            .Select(m => m.Token.Kind)
            .ToFrozenSet();

    /// <summary>
    /// Token kinds that begin an action statement (<c>set</c>, <c>add</c>, etc.).
    /// Derived from <see cref="Actions.All"/>.
    /// </summary>
    internal static readonly FrozenSet<TokenKind> ActionKeywords =
        Actions.All
            .Select(a => a.Token.Kind)
            .ToFrozenSet();

    /// <summary>
    /// Tokens that always terminate expression parsing — declaration boundaries,
    /// clause introducers, and structural tokens.
    /// </summary>
    private static readonly FrozenSet<TokenKind> StructuralBoundaryTokens = new[]
    {
        TokenKind.When, TokenKind.Because, TokenKind.Arrow, TokenKind.Ensure,
        TokenKind.EndOfSource, TokenKind.NewLine,
    }.ToFrozenSet();

    internal static readonly FrozenSet<TokenKind> ExpressionBoundaryTokens =
        StructuralBoundaryTokens.Union(Constructs.LeadingTokens).ToFrozenSet();

    // ════════════════════════════════════════════════════════════════════════════
    //  Public entry point
    // ════════════════════════════════════════════════════════════════════════════

    public static SyntaxTree Parse(TokenStream tokens)
    {
        var session = new ParseSession(tokens.Tokens);
        return session.ParseAll();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ParseSession — mutable cursor state for a single parse pass
    // ════════════════════════════════════════════════════════════════════════════

    internal ref struct ParseSession
    {
        private readonly ImmutableArray<Token> _tokens;
        private int _position;
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;

        public ParseSession(ImmutableArray<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        }

        // ── Token navigation ──────────────────────────────────────────────────

        private Token Current() => _tokens[_position];

        private Token Peek(int offset)
        {
            var idx = _position + offset;
            return idx < _tokens.Length ? _tokens[idx] : _tokens[^1];
        }

        private Token Advance()
        {
            var token = _tokens[_position];
            if (_position < _tokens.Length - 1)
                _position++;
            return token;
        }

        private bool Match(TokenKind kind)
        {
            if (Current().Kind != kind) return false;
            Advance();
            return true;
        }

        private Token Expect(TokenKind kind)
        {
            if (Current().Kind == kind) return Advance();
            _diagnostics.Add(Diagnostics.Create(DiagnosticCode.ExpectedToken, Current().Span, kind, Current().Text));
            return new Token(kind, string.Empty, Current().Span);
        }

        private bool IsAtEnd() => Current().Kind == TokenKind.EndOfSource;

        private void SkipTrivia()
        {
            while (Current().Kind is TokenKind.NewLine or TokenKind.Comment)
                Advance();
        }

        private void EmitDiagnostic(DiagnosticCode code, SourceSpan span, params object?[] args)
        {
            _diagnostics.Add(Diagnostics.Create(code, span, args));
        }

        // ── Top-level dispatch loop ───────────────────────────────────────────

        internal SyntaxTree ParseAll()
        {
            PreceptHeaderNode? header = null;
            var declarations = ImmutableArray.CreateBuilder<Declaration>();

            SkipTrivia();

            // Parse optional precept header
            if (!IsAtEnd() && Current().Kind == TokenKind.Precept)
            {
                header = ParsePreceptHeaderDeclaration();
                SkipTrivia();
            }

            // Main dispatch loop
            while (!IsAtEnd())
            {
                SkipTrivia();
                if (IsAtEnd()) break;

                var token = Current();

                if (!Constructs.ByLeadingToken.TryGetValue(token.Kind, out var candidates))
                {
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, token.Span, "declaration keyword", token.Text);
                    SyncToNextDeclaration();
                    continue;
                }

                Declaration? decl = candidates is [var only] && only.Entry.DisambiguationTokens is null or { IsEmpty: true }
                    ? ParseDirectConstruct(only.Kind)
                    : DisambiguateAndParse(token);

                if (decl is not null)
                    declarations.Add(decl);
            }

            return new SyntaxTree(header, declarations.ToImmutable(), _diagnostics.ToImmutable());
        }


        private Declaration? ParseDirectConstruct(ConstructKind kind) => kind switch
        {
            ConstructKind.FieldDeclaration => ParseFieldDeclaration(),
            ConstructKind.StateDeclaration => ParseStateDeclaration(),
            ConstructKind.EventDeclaration => ParseEventDeclaration(),
            ConstructKind.RuleDeclaration  => ParseRuleDeclaration(),
            var k => throw new InvalidOperationException($"Unexpected direct construct: {k}"),
        };

        private Declaration? DisambiguateAndParse(Token token)
        {
            var leadingKind = token.Kind;
            Advance(); // consume 'in', 'to', 'from', or 'on'
            var start = token.Span;

            // Parse anchor target: state for in/to/from, event for on
            if (leadingKind == TokenKind.On)
            {
                var eventTarget = ParseEventTargetDirect();
                if (eventTarget is null)
                {
                    SyncToNextDeclaration();
                    return null;
                }

                var stashedGuard = TryParseStashedGuard();

                if (IsAtEnd())
                {
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "ensure or ->", Current().Text);
                    return null;
                }

#pragma warning disable CS8524 // unnamed enum values are unreachable — named arms cover all defined ConstructKind values
                return FindDisambiguatedConstruct(leadingKind, Current().Kind) switch
                {
                    ConstructKind.EventEnsure  => ParseEventEnsure(start, eventTarget.Value, stashedGuard),
                    ConstructKind.EventHandler => ParseEventHandlerWithGuardCheck(start, eventTarget.Value, stashedGuard),
                    null => EmitAmbiguityAndSync(Current()),
                    ConstructKind.PreceptHeader or ConstructKind.FieldDeclaration or
                    ConstructKind.StateDeclaration or ConstructKind.EventDeclaration or
                    ConstructKind.RuleDeclaration or ConstructKind.TransitionRow or
                    ConstructKind.StateEnsure or ConstructKind.AccessMode or
                    ConstructKind.OmitDeclaration or ConstructKind.StateAction
                        => throw new InvalidOperationException(
                            "Non-EventScoped ConstructKind reached EventScoped switch — check DisambiguationEntry.LeadingToken values."),
                };
#pragma warning restore CS8524
            }
            else
            {
                var stateTarget = ParseStateTargetDirect();
                if (stateTarget is null)
                {
                    SyncToNextDeclaration();
                    return null;
                }

                var stashedGuard = TryParseStashedGuard();

                if (IsAtEnd())
                {
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "disambiguation token", Current().Text);
                    return null;
                }

#pragma warning disable CS8524 // unnamed enum values are unreachable — named arms cover all defined ConstructKind values
                return FindDisambiguatedConstruct(leadingKind, Current().Kind) switch
                {
                    ConstructKind.AccessMode      => ParseAccessMode(start, stateTarget, stashedGuard),
                    ConstructKind.OmitDeclaration => ParseOmitDeclaration(start, stateTarget, stashedGuard),
                    ConstructKind.StateEnsure     => ParseStateEnsure(start, token, stateTarget, stashedGuard),
                    ConstructKind.StateAction     => ParseStateAction(start, token, stateTarget, stashedGuard),
                    ConstructKind.TransitionRow   => ParseTransitionRow(start, stateTarget, stashedGuard),
                    null => EmitAmbiguityAndSync(Current()),
                    ConstructKind.PreceptHeader or ConstructKind.FieldDeclaration or
                    ConstructKind.StateDeclaration or ConstructKind.EventDeclaration or
                    ConstructKind.RuleDeclaration or ConstructKind.EventEnsure or ConstructKind.EventHandler
                        => throw new InvalidOperationException(
                            "Non-StateScoped ConstructKind reached StateScoped switch — check DisambiguationEntry.LeadingToken values."),
                };
#pragma warning restore CS8524
            }
        }

        private static ConstructKind? FindDisambiguatedConstruct(TokenKind leadingKind, TokenKind disambToken)
        {
            if (!Constructs.ByLeadingToken.TryGetValue(leadingKind, out var candidates))
                return null;
            foreach (var (kind, entry) in candidates)
            {
                if (entry.DisambiguationTokens?.Contains(disambToken) == true)
                    return kind;
            }
            return null;
        }

        private Declaration? EmitAmbiguityAndSync(Token token)
        {
            EmitDiagnostic(DiagnosticCode.ExpectedToken, token.Span, "disambiguation token", token.Text);
            SyncToNextDeclaration();
            return null;
        }

        private Expression? TryParseStashedGuard()
        {
            if (Current().Kind != TokenKind.When) return null;
            Advance(); // consume 'when'
            return ParseExpression(0);
        }

        private StateTargetNode? ParseStateTargetDirect()
        {
            if (Current().Kind == TokenKind.Any)
            {
                var tok = Advance();
                return new StateTargetNode(tok.Span, tok, IsQuantifier: true);
            }
            if (Current().Kind == TokenKind.Identifier)
            {
                var tok = Advance();
                return new StateTargetNode(tok.Span, tok, IsQuantifier: false);
            }
            EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "state name", Current().Text);
            return null;
        }

        private Token? ParseEventTargetDirect()
        {
            if (Current().Kind == TokenKind.Identifier)
                return Advance();
            EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "event name", Current().Text);
            return null;
        }

        // ── in-scoped construct parsers ───────────────────────────────────────

        private AccessModeNode ParseAccessMode(SourceSpan start, StateTargetNode anchor, Expression? stashedGuard)
        {
            Advance(); // consume 'modify'
            var fields = ParseFieldTargetDirect();
            var mode = ParseAccessModeKeywordDirect();

            // Guard: stashed (pre-field) or post-field
            Expression? guard = stashedGuard;
            if (guard is null && Current().Kind == TokenKind.When)
            {
                Advance();
                guard = ParseExpression(0);
            }

            var lastSpan = guard?.Span ?? mode.Span;
            return new AccessModeNode(SourceSpan.Covering(start, lastSpan), anchor, fields, mode, guard);
        }

        private OmitDeclarationNode ParseOmitDeclaration(SourceSpan start, StateTargetNode anchor, Expression? stashedGuard)
        {
            Advance(); // consume 'omit'

            if (stashedGuard is not null)
                EmitDiagnostic(DiagnosticCode.OmitDoesNotSupportGuard, stashedGuard.Span);

            var fields = ParseFieldTargetDirect();

            // Post-field guard: consume and discard with diagnostic
            if (Current().Kind == TokenKind.When)
            {
                var whenSpan = Current().Span;
                Advance(); // consume 'when'
                ParseExpression(0); // consume and discard
                EmitDiagnostic(DiagnosticCode.OmitDoesNotSupportGuard, whenSpan);
            }

            return new OmitDeclarationNode(SourceSpan.Covering(start, fields.Span), anchor, fields);
        }

        private StateEnsureNode ParseStateEnsure(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
        {
            Advance(); // consume 'ensure'
            var condition = ParseExpression(0);
            var because = Expect(TokenKind.Because);
            var message = ParseExpression(0);

            return new StateEnsureNode(
                SourceSpan.Covering(start, message.Span),
                preposition, anchor, stashedGuard, condition, message);
        }

        // ── to-scoped construct parsers ───────────────────────────────────────

        private StateActionNode ParseStateAction(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
        {
            // First arrow is the disambiguation token
            Advance(); // consume '->'
            var first = ParseActionStatement();
            var actions = ImmutableArray.CreateBuilder<Statement>();
            actions.Add(first);

            SkipTrivia();
            while (Current().Kind == TokenKind.Arrow && !IsOutcomeAhead())
            {
                Advance(); // consume '->'
                actions.Add(ParseActionStatement());
                SkipTrivia();
            }

            var lastSpan = actions[^1].Span;
            return new StateActionNode(
                SourceSpan.Covering(start, lastSpan),
                preposition, anchor, stashedGuard, actions.ToImmutable());
        }

        // ── from-scoped construct parsers ──────────────────────────────────────

        private TransitionRowNode ParseTransitionRow(SourceSpan start, StateTargetNode fromState, Expression? stashedGuard)
        {
            Advance(); // consume 'on'
            var eventName = Expect(TokenKind.Identifier);

            // Guard handling: stashed pre-event guard → emit diagnostic, inject post-event
            Expression? guard;
            if (stashedGuard is not null)
            {
                EmitDiagnostic(DiagnosticCode.PreEventGuardNotAllowed, stashedGuard.Span);
                guard = stashedGuard;
            }
            else
            {
                guard = TryParseStashedGuard();
            }

            // Parse action chain: -> action -> action -> ... -> outcome
            var actions = ImmutableArray.CreateBuilder<Statement>();
            SkipTrivia();
            while (Current().Kind == TokenKind.Arrow && !IsOutcomeAhead())
            {
                Advance(); // consume '->'
                var action = TryParseActionStatementWithRecovery();
                if (action is not null)
                    actions.Add(action);
                SkipTrivia();
            }

            // Parse outcome
            OutcomeNode outcome;
            SkipTrivia();
            if (Current().Kind == TokenKind.Arrow)
            {
                Advance(); // consume '->'
                outcome = ParseOutcomeNode();
            }
            else
            {
                EmitDiagnostic(DiagnosticCode.ExpectedOutcome, Current().Span);
                outcome = new NoTransitionOutcomeNode(Current().Span);
            }

            var lastSpan = outcome.Span;
            return new TransitionRowNode(
                SourceSpan.Covering(start, lastSpan),
                fromState, eventName, guard, actions.ToImmutable(), outcome);
        }

        private OutcomeNode ParseOutcomeNode()
        {
            var current = Current();
            if (current.Kind == TokenKind.Transition)
            {
                Advance(); // consume 'transition'
                var target = Expect(TokenKind.Identifier);
                return new TransitionOutcomeNode(SourceSpan.Covering(current.Span, target.Span), target);
            }
            if (current.Kind == TokenKind.No)
            {
                var noTok = Advance(); // consume 'no'
                var transTok = Expect(TokenKind.Transition);
                return new NoTransitionOutcomeNode(SourceSpan.Covering(noTok.Span, transTok.Span));
            }
            if (current.Kind == TokenKind.Reject)
            {
                var rejectTok = Advance(); // consume 'reject'
                var message = ParseExpression(0);
                return new RejectOutcomeNode(SourceSpan.Covering(rejectTok.Span, message.Span), message);
            }
            EmitDiagnostic(DiagnosticCode.ExpectedOutcome, current.Span);
            return new NoTransitionOutcomeNode(current.Span);
        }

        /// <summary>
        /// Attempt to parse an action statement; on failure, consume tokens until the next
        /// arrow or declaration boundary and return null to allow parsing to continue.
        /// </summary>
        private Statement? TryParseActionStatementWithRecovery()
        {
            if (ActionKeywords.Contains(Current().Kind))
                return ParseActionStatement();

            // Error recovery: unexpected token after '->'
            EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "action keyword", Current().Text);
            while (!IsAtEnd() && Current().Kind != TokenKind.Arrow && !Constructs.LeadingTokens.Contains(Current().Kind))
                Advance();
            return null;
        }

        // ── on-scoped construct parsers ───────────────────────────────────────

        private EventEnsureNode ParseEventEnsure(SourceSpan start, Token eventName, Expression? stashedGuard)
        {
            Advance(); // consume 'ensure'
            var condition = ParseExpression(0);
            var because = Expect(TokenKind.Because);
            var message = ParseExpression(0);

            return new EventEnsureNode(
                SourceSpan.Covering(start, message.Span),
                eventName, stashedGuard, condition, message);
        }

        private EventHandlerNode ParseEventHandler(SourceSpan start, Token eventName)
        {
            return ParseEventHandlerWithGuardCheck(start, eventName, stashedGuard: null);
        }

        private EventHandlerNode ParseEventHandlerWithGuardCheck(SourceSpan start, Token eventName, Expression? stashedGuard)
        {
            if (stashedGuard is not null)
                EmitDiagnostic(DiagnosticCode.EventHandlerDoesNotSupportGuard, stashedGuard.Span);

            Advance(); // consume '->'
            var first = ParseActionStatement();
            var actions = ImmutableArray.CreateBuilder<Statement>();
            actions.Add(first);

            SkipTrivia();
            while (Current().Kind == TokenKind.Arrow && !IsOutcomeAhead())
            {
                Advance(); // consume '->'
                actions.Add(ParseActionStatement());
                SkipTrivia();
            }

            var lastSpan = actions[^1].Span;
            return new EventHandlerNode(
                SourceSpan.Covering(start, lastSpan),
                eventName, actions.ToImmutable());
        }

        // ── Shared parsing helpers for disambiguated constructs ───────────────

        private FieldTargetNode ParseFieldTargetDirect()
        {
            if (Current().Kind == TokenKind.All)
            {
                var allTok = Advance();
                return new AllFieldTarget(allTok.Span, allTok);
            }

            if (Current().Kind != TokenKind.Identifier)
            {
                EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "field name", Current().Text);
                return new SingularFieldTarget(Current().Span,
                    new Token(TokenKind.Identifier, string.Empty, Current().Span));
            }

            var first = Advance();
            if (Current().Kind != TokenKind.Comma)
                return new SingularFieldTarget(first.Span, first);

            var names = ImmutableArray.CreateBuilder<Token>();
            names.Add(first);
            while (Current().Kind == TokenKind.Comma)
            {
                Advance(); // consume ','
                if (Current().Kind == TokenKind.Identifier)
                    names.Add(Advance());
                else
                {
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "field name", Current().Text);
                    break;
                }
            }
            return new ListFieldTarget(
                SourceSpan.Covering(names[0].Span, names[^1].Span), names.ToImmutable());
        }

        private Token ParseAccessModeKeywordDirect()
        {
            if (Tokens.AccessModeKeywords.Contains(Current().Kind))
                return Advance();

            EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "readonly or editable", Current().Text);
            return new Token(TokenKind.Readonly, string.Empty, Current().Span);
        }

        private Statement ParseActionStatement()
        {
            var current = Current();
            if (!Actions.ByTokenKind.TryGetValue(current.Kind, out var meta))
            {
                EmitDiagnostic(DiagnosticCode.ExpectedToken, current.Span, "action keyword", current.Text);
                return new SetStatement(current.Span,
                    new Token(TokenKind.Identifier, string.Empty, current.Span),
                    new IdentifierExpression(current.Span,
                        new Token(TokenKind.Identifier, string.Empty, current.Span)));
            }

#pragma warning disable CS8524 // unnamed ActionSyntaxShape values are unreachable — CS8509 enforces named-value coverage
            return meta.SyntaxShape switch
            {
                ActionSyntaxShape.AssignValue     => ParseAssignValueStatement(meta),
                ActionSyntaxShape.CollectionValue => ParseCollectionValueStatement(meta),
                ActionSyntaxShape.CollectionInto  => ParseCollectionIntoStatement(meta),
                ActionSyntaxShape.FieldOnly       => ParseFieldOnlyStatement(meta),
            };
#pragma warning restore CS8524
        }

        private Statement ParseAssignValueStatement(ActionMeta meta)
        {
            var kw = Advance();
            var field = Expect(TokenKind.Identifier);
            Expect(TokenKind.Assign);
            var value = ParseExpression(0);
            var span = SourceSpan.Covering(kw.Span, value.Span);
            return meta.Kind switch
            {
                ActionKind.Set => new SetStatement(span, field, value),
                _ => throw new InvalidOperationException($"Unexpected ActionKind {meta.Kind} in ParseAssignValueStatement"),
            };
        }

        private Statement ParseCollectionValueStatement(ActionMeta meta)
        {
            var kw = Advance();
            var field = Expect(TokenKind.Identifier);
            var value = ParseExpression(0);
            var span = SourceSpan.Covering(kw.Span, value.Span);
            return meta.Kind switch
            {
                ActionKind.Add    => new AddStatement(span, field, value),
                ActionKind.Remove => new RemoveStatement(span, field, value),
                ActionKind.Enqueue => new EnqueueStatement(span, field, value),
                ActionKind.Push   => new PushStatement(span, field, value),
                _ => throw new InvalidOperationException($"Unexpected ActionKind {meta.Kind} in ParseCollectionValueStatement"),
            };
        }

        private Statement ParseCollectionIntoStatement(ActionMeta meta)
        {
            var kw = Advance();
            var field = Expect(TokenKind.Identifier);
            Token? into = null;
            if (Current().Kind == TokenKind.Into)
            {
                Advance();
                into = Expect(TokenKind.Identifier);
            }
            var endSpan = into?.Span ?? field.Span;
            var span = SourceSpan.Covering(kw.Span, endSpan);
            return meta.Kind switch
            {
                ActionKind.Dequeue => new DequeueStatement(span, field, into),
                ActionKind.Pop     => new PopStatement(span, field, into),
                _ => throw new InvalidOperationException($"Unexpected ActionKind {meta.Kind} in ParseCollectionIntoStatement"),
            };
        }

        private Statement ParseFieldOnlyStatement(ActionMeta meta)
        {
            var kw = Advance();
            var field = Expect(TokenKind.Identifier);
            var span = SourceSpan.Covering(kw.Span, field.Span);
            return meta.Kind switch
            {
                ActionKind.Clear => new ClearStatement(span, field),
                _ => throw new InvalidOperationException($"Unexpected ActionKind {meta.Kind} in ParseFieldOnlyStatement"),
            };
        }

        private bool IsOutcomeAhead()
        {
            var next = Peek(1);
            return next.Kind is TokenKind.Transition or TokenKind.No or TokenKind.Reject;
        }

        private void SyncToNextDeclaration()
        {
            Advance(); // skip current offending token
            while (!IsAtEnd() && !Constructs.LeadingTokens.Contains(Current().Kind))
                Advance();
        }

        // ── Construct parsers (non-disambiguated) ─────────────────────────────

        private PreceptHeaderNode ParsePreceptHeaderDeclaration()
        {
            var start = Current().Span;
            Advance(); // consume 'precept'
            var name = Expect(TokenKind.Identifier);
            return new PreceptHeaderNode(SourceSpan.Covering(start, name.Span), name);
        }

        private FieldDeclarationNode ParseFieldDeclaration()
        {
            var meta = Constructs.GetMeta(ConstructKind.FieldDeclaration);
            var start = Current().Span;
            Advance(); // consume 'field'
            var slots = ParseConstructSlots(meta);
            var lastSpan = GetLastSlotSpan(slots, start);
            return (FieldDeclarationNode)BuildNode(ConstructKind.FieldDeclaration, slots,
                SourceSpan.Covering(start, lastSpan));
        }

        private StateDeclarationNode ParseStateDeclaration()
        {
            var meta = Constructs.GetMeta(ConstructKind.StateDeclaration);
            var start = Current().Span;
            Advance(); // consume 'state'
            var slots = ParseConstructSlots(meta);
            var lastSpan = GetLastSlotSpan(slots, start);
            return (StateDeclarationNode)BuildNode(ConstructKind.StateDeclaration, slots,
                SourceSpan.Covering(start, lastSpan));
        }

        private EventDeclarationNode ParseEventDeclaration()
        {
            var meta = Constructs.GetMeta(ConstructKind.EventDeclaration);
            var start = Current().Span;
            Advance(); // consume 'event'
            var slots = ParseConstructSlots(meta);
            var lastSpan = GetLastSlotSpan(slots, start);
            return (EventDeclarationNode)BuildNode(ConstructKind.EventDeclaration, slots,
                SourceSpan.Covering(start, lastSpan));
        }

        private RuleDeclarationNode ParseRuleDeclaration()
        {
            var meta = Constructs.GetMeta(ConstructKind.RuleDeclaration);
            var start = Current().Span;
            Advance(); // consume 'rule'
            var slots = ParseConstructSlots(meta);
            var lastSpan = GetLastSlotSpan(slots, start);
            return (RuleDeclarationNode)BuildNode(ConstructKind.RuleDeclaration, slots,
                SourceSpan.Covering(start, lastSpan));
        }

        // ── State entry parsing ───────────────────────────────────────────────

        private ImmutableArray<StateEntryNode> ParseStateEntries()
        {
            var entries = ImmutableArray.CreateBuilder<StateEntryNode>();

            do
            {
                if (Current().Kind != TokenKind.Identifier) break;
                var nameToken = Advance();
                var modifiers = ImmutableArray.CreateBuilder<Token>();
                while (StateModifierKeywords.Contains(Current().Kind))
                    modifiers.Add(Advance());

                var entrySpan = modifiers.Count > 0
                    ? SourceSpan.Covering(nameToken.Span, modifiers[^1].Span)
                    : nameToken.Span;
                entries.Add(new StateEntryNode(entrySpan, nameToken, modifiers.ToImmutable()));
            }
            while (Match(TokenKind.Comma));

            return entries.ToImmutable();
        }

        // ── Identifier list helpers ───────────────────────────────────────────

        private ImmutableArray<Token> ParseIdentifierListTokens()
        {
            var names = ImmutableArray.CreateBuilder<Token>();
            names.Add(Expect(TokenKind.Identifier));

            while (Current().Kind == TokenKind.Comma)
            {
                Advance();
                if (Current().Kind == TokenKind.Identifier)
                    names.Add(Advance());
                else
                {
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "identifier", Current().Text);
                    break;
                }
            }
            return names.ToImmutable();
        }

        // ── Argument list parsing ─────────────────────────────────────────────

        private ImmutableArray<ArgumentNode> ParseArgumentListInner()
        {
            var args = ImmutableArray.CreateBuilder<ArgumentNode>();
            if (Current().Kind == TokenKind.RightParen) return args.ToImmutable();

            do
            {
                var argName = Expect(TokenKind.Identifier);
                var asToken = Expect(TokenKind.As);
                var type = ParseTypeRef();
                var modifiers = ParseFieldModifierNodes();
                var argSpan = modifiers.Length > 0
                    ? SourceSpan.Covering(argName.Span, modifiers[^1].Span)
                    : SourceSpan.Covering(argName.Span, type.Span);
                args.Add(new ArgumentNode(argSpan, argName, type, modifiers));
            }
            while (Match(TokenKind.Comma));

            return args.ToImmutable();
        }

        // ── Generic slot iteration (Slice 2.5) ────────────────────────────────

        /// <summary>
        /// Walks each slot in a construct's metadata and invokes the corresponding
        /// slot parser. Returns a slot array suitable for <see cref="BuildNode"/>.
        /// </summary>
        private SyntaxNode?[] ParseConstructSlots(ConstructMeta meta)
        {
            var slots = new SyntaxNode?[meta.Slots.Count];
            for (int i = 0; i < meta.Slots.Count; i++)
            {
                var slot = meta.Slots[i];
                slots[i] = InvokeSlotParser(slot.Kind, !slot.IsRequired);
            }
            return slots;
        }

        // ── InvokeSlotParser — exhaustive switch (Slice 2.3) ──────────────────

        /// <summary>
        /// Dispatches to the appropriate slot parser for the given slot kind.
        /// CS8509 enforcement: adding a new <see cref="ConstructSlotKind"/> member
        /// without an arm here is a build error.
        /// </summary>
        private SyntaxNode? InvokeSlotParser(ConstructSlotKind slotKind, bool isOptional) => slotKind switch
        {
            ConstructSlotKind.IdentifierList     => ParseIdentifierList(isOptional),
            ConstructSlotKind.TypeExpression      => ParseTypeExpression(isOptional),
            ConstructSlotKind.ModifierList        => ParseModifierList(isOptional),
            ConstructSlotKind.StateEntryList      => ParseStateEntryList(isOptional),
            ConstructSlotKind.ArgumentList        => ParseArgumentList(isOptional),
            ConstructSlotKind.ComputeExpression   => ParseComputeExpression(isOptional),
            ConstructSlotKind.GuardClause         => ParseGuardClause(isOptional),
            ConstructSlotKind.ActionChain         => ParseActionChain(isOptional),
            ConstructSlotKind.Outcome             => ParseOutcome(isOptional),
            ConstructSlotKind.StateTarget         => ParseStateTarget(isOptional),
            ConstructSlotKind.EventTarget         => ParseEventTarget(isOptional),
            ConstructSlotKind.EnsureClause        => ParseEnsureClause(isOptional),
            ConstructSlotKind.BecauseClause       => ParseBecauseClause(isOptional),
            ConstructSlotKind.AccessModeKeyword   => ParseAccessModeKeyword(isOptional),
            ConstructSlotKind.FieldTarget         => ParseFieldTarget(isOptional),
            ConstructSlotKind.RuleExpression      => ParseRuleExpression(isOptional),
            ConstructSlotKind.InitialMarker       => ParseInitialMarker(isOptional),
            // CS8509 enforcement: a new ConstructSlotKind member without an arm is a build error.
            // The wildcard below covers only unnamed numeric values outside the defined enum range.
            _ => throw new ArgumentOutOfRangeException(nameof(slotKind), slotKind,
                $"Unknown ConstructSlotKind: {slotKind}"),
        };

        // ── Slot parsers (PR 3 — non-disambiguated constructs) ────────────────

        private SyntaxNode? ParseIdentifierList(bool isOptional)
        {
            if (Current().Kind != TokenKind.Identifier)
            {
                if (!isOptional)
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "identifier", Current().Text);
                return null;
            }
            var tokens = ParseIdentifierListTokens();
            return new TokenArrayWrapper(tokens[0].Span, tokens);
        }

        private SyntaxNode? ParseTypeExpression(bool isOptional)
        {
            if (Current().Kind != TokenKind.As)
            {
                if (!isOptional)
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "as", Current().Text);
                return null;
            }
            Advance(); // consume 'as'
            return ParseTypeRef();
        }

        private SyntaxNode? ParseModifierList(bool isOptional)
        {
            var modifiers = ParseFieldModifierNodes();
            if (modifiers.Length == 0) return null;
            return new FieldModifierArrayWrapper(modifiers[0].Span, modifiers);
        }

        private SyntaxNode? ParseStateEntryList(bool isOptional)
        {
            var entries = ParseStateEntries();
            if (entries.Length == 0)
            {
                if (!isOptional)
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "state name", Current().Text);
                return null;
            }
            return new StateEntryArrayWrapper(
                SourceSpan.Covering(entries[0].Span, entries[^1].Span), entries);
        }

        private SyntaxNode? ParseArgumentList(bool isOptional)
        {
            if (Current().Kind != TokenKind.LeftParen) return null;
            Advance(); // consume '('
            var args = ParseArgumentListInner();
            Expect(TokenKind.RightParen);
            if (args.Length == 0) return null;
            return new ArgumentArrayWrapper(args[0].Span, args);
        }

        private SyntaxNode? ParseComputeExpression(bool isOptional)
        {
            if (Current().Kind != TokenKind.Arrow) return null;
            Advance(); // consume '->'
            return ParseExpression(0);
        }

        private SyntaxNode? ParseGuardClause(bool isOptional)
        {
            if (Current().Kind != TokenKind.When) return null;
            Advance(); // consume 'when'
            return ParseExpression(0);
        }

        private SyntaxNode? ParseBecauseClause(bool isOptional)
        {
            if (Current().Kind != TokenKind.Because)
            {
                if (!isOptional)
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "because", Current().Text);
                return null;
            }
            Advance(); // consume 'because'
            return ParseExpression(0);
        }

        private SyntaxNode? ParseRuleExpression(bool isOptional)
        {
            return ParseExpression(0);
        }

        private SyntaxNode? ParseInitialMarker(bool isOptional)
        {
            if (Current().Kind != TokenKind.Initial) return null;
            var token = Advance();
            return new TokenWrapper(token.Span, token);
        }

        // ── Slot parsers for disambiguated constructs (PR 4) ────────────────

        private SyntaxNode? ParseActionChain(bool isOptional)
        {
            var actions = ImmutableArray.CreateBuilder<Statement>();
            while (Current().Kind == TokenKind.Arrow && !IsOutcomeAhead())
            {
                Advance(); // consume '->'
                actions.Add(ParseActionStatement());
            }
            if (actions.Count == 0) return null;
            return new StatementArrayWrapper(
                SourceSpan.Covering(actions[0].Span, actions[^1].Span), actions.ToImmutable());
        }

        private SyntaxNode? ParseOutcome(bool isOptional)
        {
            if (Current().Kind != TokenKind.Arrow)
            {
                if (!isOptional)
                    EmitDiagnostic(DiagnosticCode.ExpectedOutcome, Current().Span);
                return null;
            }
            Advance(); // consume '->'
            return ParseOutcomeNode();
        }

        private SyntaxNode? ParseStateTarget(bool isOptional)
        {
            var target = ParseStateTargetDirect();
            return target;
        }

        private SyntaxNode? ParseEventTarget(bool isOptional)
        {
            var tok = ParseEventTargetDirect();
            if (tok is null) return null;
            return new TokenWrapper(tok.Value.Span, tok.Value);
        }

        private SyntaxNode? ParseEnsureClause(bool isOptional)
        {
            if (Current().Kind != TokenKind.Ensure)
            {
                if (!isOptional)
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "ensure", Current().Text);
                return null;
            }
            Advance(); // consume 'ensure'
            return ParseExpression(0);
        }

        private SyntaxNode? ParseAccessModeKeyword(bool isOptional)
        {
            if (Current().Kind is TokenKind.Readonly or TokenKind.Editable)
                return new TokenWrapper(Current().Span, Advance());
            if (!isOptional)
                EmitDiagnostic(DiagnosticCode.ExpectedToken, Current().Span, "readonly or editable", Current().Text);
            return null;
        }

        private SyntaxNode? ParseFieldTarget(bool isOptional)
        {
            var target = ParseFieldTargetDirect();
            return target;
        }

        // ── Type reference parsing ────────────────────────────────────────────

        private TypeRefNode ParseTypeRef()
        {
            var current = Current();

            // "set" in type position: lexer emits Set, parser reinterprets
            if (current.Kind == TokenKind.Set || current.Kind == TokenKind.QueueType || current.Kind == TokenKind.StackType)
            {
                var collectionToken = Advance();
                Expect(TokenKind.Of);
                var elemToken = Advance(); // element type
                return new CollectionTypeRefNode(
                    SourceSpan.Covering(collectionToken.Span, elemToken.Span),
                    collectionToken, elemToken, null);
            }

            if (current.Kind == TokenKind.ChoiceType)
            {
                var choiceToken = Advance();
                var options = ImmutableArray.CreateBuilder<Expression>();
                // choice(...) or choice "A" "B" "C"
                if (Match(TokenKind.LeftParen))
                {
                    if (Current().Kind != TokenKind.RightParen)
                    {
                        do
                        {
                            options.Add(ParseExpression(0));
                        }
                        while (Match(TokenKind.Comma));
                    }
                    Expect(TokenKind.RightParen);
                }
                else
                {
                    while (Current().Kind == TokenKind.StringLiteral)
                        options.Add(new LiteralExpression(Current().Span, Advance()));
                }
                var lastSpan = options.Count > 0 ? options[^1].Span : choiceToken.Span;
                return new ChoiceTypeRefNode(SourceSpan.Covering(choiceToken.Span, lastSpan), options.ToImmutable());
            }

            if (TypeKeywords.Contains(current.Kind))
            {
                var typeToken = Advance();
                TypeQualifierNode? qualifier = null;
                // Check for qualifier: in/of
                if (Current().Kind == TokenKind.In || Current().Kind == TokenKind.Of)
                {
                    var qualKw = Advance();
                    var qualVal = ParseExpression(0);
                    qualifier = new TypeQualifierNode(
                        SourceSpan.Covering(qualKw.Span, qualVal.Span), qualKw, qualVal);
                }
                var span = qualifier is not null
                    ? SourceSpan.Covering(typeToken.Span, qualifier.Span)
                    : typeToken.Span;
                return new ScalarTypeRefNode(span, typeToken, qualifier);
            }

            // Unknown type — emit diagnostic and return a placeholder
            EmitDiagnostic(DiagnosticCode.ExpectedToken, current.Span, "type", current.Text);
            return new ScalarTypeRefNode(current.Span,
                new Token(TokenKind.Identifier, current.Text, current.Span), null);
        }

        // ── Field modifier parsing ────────────────────────────────────────────

        private ImmutableArray<FieldModifierNode> ParseFieldModifierNodes()
        {
            var modifiers = ImmutableArray.CreateBuilder<FieldModifierNode>();
            while (ModifierKeywords.Contains(Current().Kind))
            {
                var modToken = Advance();
                // Check if this is a value-bearing modifier
                var modMeta = Modifiers.All.OfType<FieldModifierMeta>()
                    .FirstOrDefault(m => m.Token.Kind == modToken.Kind);
                if (modMeta?.HasValue == true)
                {
                    var value = ParseExpression(0);
                    modifiers.Add(new ValueModifierNode(
                        SourceSpan.Covering(modToken.Span, value.Span), modToken, value));
                }
                else
                {
                    modifiers.Add(new FlagModifierNode(modToken.Span, modToken));
                }
            }
            return modifiers.ToImmutable();
        }

        // ── Expression parser (Pratt) ─────────────────────────────────────────

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
                    var member = Expect(TokenKind.Identifier);
                    left = new MemberAccessExpression(
                        SourceSpan.Covering(left.Span, member.Span), left, member);
                    continue;
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

                case TokenKind.Identifier:
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

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SourceSpan GetLastSlotSpan(SyntaxNode?[] slots, SourceSpan fallback)
        {
            for (int i = slots.Length - 1; i >= 0; i--)
                if (slots[i] is not null) return slots[i]!.Span;
            return fallback;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  BuildNode — exhaustive 12-arm switch (Slice 2.4)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Assembles the final <see cref="Declaration"/> from the slot array produced by
    /// <see cref="ParseSlots"/>. One arm per <see cref="ConstructKind"/> — no default.
    /// CS8509 fires here when a new <see cref="ConstructKind"/> is added without a
    /// corresponding assembly arm.
    /// </summary>
#pragma warning disable CS8524 // unnamed enum values are unreachable — CS8509 enforces named-value coverage
    internal static Declaration BuildNode(ConstructKind kind, SyntaxNode?[] slots, SourceSpan span) => kind switch
    {
        ConstructKind.PreceptHeader => new PreceptHeaderNode(span,
            ((SyntaxNode)slots[0]!).AsToken()),

        ConstructKind.FieldDeclaration => new FieldDeclarationNode(span,
            ((SyntaxNode)slots[0]!).AsTokenArray(),
            (TypeRefNode)slots[1]!,
            slots[2]?.AsFieldModifiers() ?? [],
            slots[3] as Expression),

        ConstructKind.StateDeclaration => new StateDeclarationNode(span,
            ((SyntaxNode)slots[0]!).AsStateEntries()),

        ConstructKind.EventDeclaration => new EventDeclarationNode(span,
            ((SyntaxNode)slots[0]!).AsTokenArray(),
            slots[1]?.AsArguments() ?? [],
            slots[2] is not null),

        ConstructKind.RuleDeclaration => new RuleDeclarationNode(span,
            (Expression)slots[0]!,
            slots[1] as Expression,
            (Expression)slots[2]!),

        ConstructKind.TransitionRow => new TransitionRowNode(span,
            (StateTargetNode)slots[0]!,
            ((SyntaxNode)slots[1]!).AsToken(),
            slots[2] as Expression,
            slots[3]?.AsStatements() ?? [],
            (OutcomeNode)slots[4]!),

        ConstructKind.StateEnsure => new StateEnsureNode(span,
            default, // preposition token injected by dispatch loop
            (StateTargetNode)slots[0]!,
            null,
            (Expression)((SyntaxNode)slots[1]!),
            default!),

        ConstructKind.AccessMode => new AccessModeNode(span,
            (StateTargetNode)slots[0]!,
            (FieldTargetNode)slots[1]!,
            ((SyntaxNode)slots[2]!).AsToken(),
            slots[3] as Expression),

        ConstructKind.OmitDeclaration => new OmitDeclarationNode(span,
            (StateTargetNode)slots[0]!,
            (FieldTargetNode)slots[1]!),

        ConstructKind.StateAction => new StateActionNode(span,
            default, // preposition token injected by dispatch loop
            (StateTargetNode)slots[0]!,
            null,
            slots[1]?.AsStatements() ?? []),

        ConstructKind.EventEnsure => new EventEnsureNode(span,
            ((SyntaxNode)slots[0]!).AsToken(),
            null,
            (Expression)((SyntaxNode)slots[1]!),
            default!),

        ConstructKind.EventHandler => new EventHandlerNode(span,
            ((SyntaxNode)slots[0]!).AsToken(),
            slots[1]?.AsStatements() ?? []),
    };
#pragma warning restore CS8524
}

// ════════════════════════════════════════════════════════════════════════════
//  Wrapper nodes — thin packaging for slot parser return types
//
//  These let slot parsers return typed collections as SyntaxNode? and let
//  BuildNode unpack them via As* extensions. They are internal implementation
//  detail of the parser — downstream stages see only the final Declaration types.
// ════════════════════════════════════════════════════════════════════════════

internal sealed record TokenWrapper(SourceSpan Span, Token Value) : SyntaxNode(Span);
internal sealed record TokenArrayWrapper(SourceSpan Span, ImmutableArray<Token> Values) : SyntaxNode(Span);
internal sealed record FieldModifierArrayWrapper(SourceSpan Span, ImmutableArray<FieldModifierNode> Values) : SyntaxNode(Span);
internal sealed record StateEntryArrayWrapper(SourceSpan Span, ImmutableArray<StateEntryNode> Values) : SyntaxNode(Span);
internal sealed record ArgumentArrayWrapper(SourceSpan Span, ImmutableArray<ArgumentNode> Values) : SyntaxNode(Span);
internal sealed record StatementArrayWrapper(SourceSpan Span, ImmutableArray<Statement> Values) : SyntaxNode(Span);

internal static class BuildNodeExtensions
{
    internal static Token AsToken(this SyntaxNode node) => ((TokenWrapper)node).Value;
    internal static ImmutableArray<Token> AsTokenArray(this SyntaxNode node) => ((TokenArrayWrapper)node).Values;
    internal static ImmutableArray<FieldModifierNode> AsFieldModifiers(this SyntaxNode node) => ((FieldModifierArrayWrapper)node).Values;
    internal static ImmutableArray<StateEntryNode> AsStateEntries(this SyntaxNode node) => ((StateEntryArrayWrapper)node).Values;
    internal static ImmutableArray<ArgumentNode> AsArguments(this SyntaxNode node) => ((ArgumentArrayWrapper)node).Values;
    internal static ImmutableArray<Statement> AsStatements(this SyntaxNode node) => ((StatementArrayWrapper)node).Values;
}
