using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static class Parser
{
    public static SyntaxTree Parse(TokenStream tokens)
    {
        var session = new ParseSession(tokens);
        var root = session.ParsePrecept();
        return new SyntaxTree(root, session.Diagnostics.ToImmutable());
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ParseSession — mutable cursor over the token stream
    // ════════════════════════════════════════════════════════════════════════════

    private struct ParseSession
    {
        private readonly ImmutableArray<Token> _tokens;
        private int _pos;

        public ImmutableArray<Diagnostic>.Builder Diagnostics { get; }

        public ParseSession(TokenStream stream)
        {
            _tokens = stream.Tokens;
            _pos = 0;
            Diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            SkipTrivia();
        }

        private Token Current => _pos < _tokens.Length
            ? _tokens[_pos]
            : new Token(TokenKind.EndOfSource, "", 0, 0, 0, 0);

        private Token Peek(int offset)
        {
            int p = _pos;
            int skipped = 0;
            while (p < _tokens.Length && skipped < offset)
            {
                p++;
                while (p < _tokens.Length && IsTrivia(_tokens[p].Kind)) p++;
                skipped++;
            }
            return p < _tokens.Length ? _tokens[p] : new Token(TokenKind.EndOfSource, "", 0, 0, 0, 0);
        }

        private Token Advance()
        {
            var t = Current;
            _pos++;
            SkipTrivia();
            return t;
        }

        private Token Consume(TokenKind kind)
        {
            var t = Current;
            if (t.Kind != kind)
                throw new InvalidOperationException($"Internal parser error: expected {kind} but found {t.Kind}");
            return Advance();
        }

        private Token Expect(TokenKind kind) => Expect(kind, DisplayName(kind));

        private Token Expect(TokenKind kind, string display)
        {
            if (Current.Kind == kind)
                return Advance();

            AddDiagnostic(DiagnosticCode.ExpectedToken, SpanOf(Current), display, Current.Text);
            return MissingToken(kind);
        }

        private static string DisplayName(TokenKind kind) => kind switch
        {
            TokenKind.Identifier       => "a name",
            TokenKind.Precept          => "the 'precept' keyword",
            TokenKind.As               => "'as'",
            TokenKind.Because          => "'because'",
            TokenKind.Assign           => "'='",
            TokenKind.RightParen       => "a closing ')'",
            TokenKind.LeftParen        => "an opening '('",
            TokenKind.RightBracket     => "a closing ']'",
            TokenKind.Transition       => "'transition'",
            TokenKind.Of               => "'of'",
            TokenKind.Then             => "'then' after the condition",
            TokenKind.Else             => "'else'",
            TokenKind.Set              => "'set'",
            TokenKind.StringEnd        => "a closing '\"' to end the text value",
            TokenKind.TypedConstantEnd => "a closing ''' to end the value",
            _                          => $"'{kind.ToString().ToLower()}'"
        };

        private void SkipTrivia()
        {
            while (_pos < _tokens.Length && IsTrivia(_tokens[_pos].Kind))
                _pos++;
        }

        private static bool IsTrivia(TokenKind k) => k is TokenKind.NewLine or TokenKind.Comment;

        private Token MissingToken(TokenKind kind) =>
            new(kind, "", Current.Line, Current.Column, Current.Offset, 0);

        private static SourceSpan SpanOf(Token t) =>
            new(t.Offset, t.Length, t.Line, t.Column, t.Line, t.Column + Math.Max(t.Length, 1));

        private void AddDiagnostic(DiagnosticCode code, SourceSpan span, params object?[] args)
        {
            Diagnostics.Add(Language.Diagnostics.Create(code, span, args));
        }

        // ── Sync-point recovery ────────────────────────────────────────────────

        private static bool IsSyncToken(TokenKind k) => k is
            TokenKind.Precept or TokenKind.Field or TokenKind.State or
            TokenKind.Event or TokenKind.Rule or TokenKind.From or
            TokenKind.To or TokenKind.In or TokenKind.On;

        private void SkipToNextSyncPoint()
        {
            while (Current.Kind != TokenKind.EndOfSource)
            {
                if (IsSyncToken(Current.Kind))
                    return;
                _pos++;
                SkipTrivia();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Top-level: precept header + declaration dispatch
        // ════════════════════════════════════════════════════════════════════════

        public PreceptNode ParsePrecept()
        {
            var start = Current;
            Expect(TokenKind.Precept);
            var name = Expect(TokenKind.Identifier, "the precept name");

            var body = ImmutableArray.CreateBuilder<Declaration>();
            while (Current.Kind != TokenKind.EndOfSource)
            {
                var decl = ParseDeclaration();
                if (decl != null)
                    body.Add(decl);
            }

            return new PreceptNode(
                SourceSpan.Covering(SpanOf(start), SpanOf(Current)),
                name, body.ToImmutable());
        }

        private Declaration? ParseDeclaration()
        {
            return Current.Kind switch
            {
                TokenKind.Field  => ParseFieldDeclaration(),
                TokenKind.State  => ParseStateDeclaration(),
                TokenKind.Event  => ParseEventDeclaration(),
                TokenKind.Rule   => ParseRuleDeclaration(),
                TokenKind.Write  => ParseRootWriteDeclaration(),
                TokenKind.In     => ParseInStatement(),
                TokenKind.To     => ParseToStatement(),
                TokenKind.From   => ParseFromStatement(),
                TokenKind.On     => ParseOnStatement(),
                TokenKind.EndOfSource => null,
                _ => SkipUnexpected(),
            };
        }

        private Declaration? SkipUnexpected()
        {
            AddDiagnostic(DiagnosticCode.UnexpectedKeyword, SpanOf(Current), Current.Text, "precept body");
            SkipToNextSyncPoint();
            return null;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Declaration parsers
        // ════════════════════════════════════════════════════════════════════════

        private FieldDeclaration ParseFieldDeclaration()
        {
            var start = Current;
            Consume(TokenKind.Field);

            var names = ImmutableArray.CreateBuilder<Token>();
            names.Add(Expect(TokenKind.Identifier, "a field name"));
            while (Current.Kind == TokenKind.Comma)
            {
                Advance();
                names.Add(Expect(TokenKind.Identifier, "a field name"));
            }

            Expect(TokenKind.As);
            var type = ParseTypeRef();
            var modifiers = ParseFieldModifiers();

            Expression? computed = null;
            if (Current.Kind == TokenKind.Arrow)
            {
                Advance();
                computed = ParseExpression(0);
            }

            return new FieldDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                names.ToImmutable(), type, modifiers, computed);
        }

        private StateDeclaration ParseStateDeclaration()
        {
            var start = Current;
            Consume(TokenKind.State);

            var entries = ImmutableArray.CreateBuilder<StateEntry>();
            entries.Add(ParseStateEntry());
            while (Current.Kind == TokenKind.Comma)
            {
                Advance();
                entries.Add(ParseStateEntry());
            }

            return new StateDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                entries.ToImmutable());
        }

        private StateEntry ParseStateEntry()
        {
            var start = Current;
            var name = Expect(TokenKind.Identifier, "a state name");
            bool isInitial = false;
            if (Current.Kind == TokenKind.Initial)
            {
                isInitial = true;
                Advance();
            }

            var mods = ImmutableArray.CreateBuilder<StateModifierKind>();
            while (IsStateModifier(Current.Kind))
            {
                mods.Add(Current.Kind switch
                {
                    TokenKind.Terminal     => StateModifierKind.Terminal,
                    TokenKind.Required     => StateModifierKind.Required,
                    TokenKind.Irreversible => StateModifierKind.Irreversible,
                    TokenKind.Success      => StateModifierKind.Success,
                    TokenKind.Warning      => StateModifierKind.Warning,
                    TokenKind.Error        => StateModifierKind.Error,
                    _ => throw new InvalidOperationException()
                });
                Advance();
            }

            return new StateEntry(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                name, isInitial, mods.ToImmutable());
        }

        private static bool IsStateModifier(TokenKind k) => k is
            TokenKind.Terminal or TokenKind.Required or TokenKind.Irreversible or
            TokenKind.Success or TokenKind.Warning or TokenKind.Error;

        private EventDeclaration ParseEventDeclaration()
        {
            var start = Current;
            Consume(TokenKind.Event);

            var names = ImmutableArray.CreateBuilder<Token>();
            names.Add(Expect(TokenKind.Identifier, "an event name"));
            while (Current.Kind == TokenKind.Comma)
            {
                Advance();
                names.Add(Expect(TokenKind.Identifier, "an event name"));
            }

            var args = ImmutableArray<ArgDeclaration>.Empty;
            if (Current.Kind == TokenKind.LeftParen)
                args = ParseArgList();

            bool isInitial = false;
            if (Current.Kind == TokenKind.Initial)
            {
                isInitial = true;
                Advance();
            }

            return new EventDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                names.ToImmutable(), args, isInitial);
        }

        private ImmutableArray<ArgDeclaration> ParseArgList()
        {
            Consume(TokenKind.LeftParen);
            var args = ImmutableArray.CreateBuilder<ArgDeclaration>();
            if (Current.Kind != TokenKind.RightParen)
            {
                args.Add(ParseArgDeclaration());
                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    args.Add(ParseArgDeclaration());
                }
            }
            Expect(TokenKind.RightParen);
            return args.ToImmutable();
        }

        private ArgDeclaration ParseArgDeclaration()
        {
            var start = Current;
            var name = Expect(TokenKind.Identifier, "an argument name");
            Expect(TokenKind.As);
            var type = ParseTypeRef();
            var mods = ParseFieldModifiers();
            return new ArgDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                name, type, mods);
        }

        private RuleDeclaration ParseRuleDeclaration()
        {
            var start = Current;
            Consume(TokenKind.Rule);
            var condition = ParseExpression(0);

            Expression? guard = null;
            if (Current.Kind == TokenKind.When)
            {
                Advance();
                guard = ParseExpression(0);
            }

            Expect(TokenKind.Because);
            var message = ParseExpression(0);

            return new RuleDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                condition, guard, message);
        }

        private AccessModeDeclaration ParseRootWriteDeclaration()
        {
            var start = Current;
            Consume(TokenKind.Write);
            var fields = ParseFieldTarget();
            return new AccessModeDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                null, AccessMode.Write, fields, null);
        }

        // ── in / to / from dispatch ────────────────────────────────────────────

        private Declaration ParseInStatement()
        {
            var start = Current;
            Consume(TokenKind.In);
            var stateTarget = ParseStateTarget();

            Expression? guard = null;
            if (Current.Kind == TokenKind.When)
            {
                Advance();
                guard = ParseExpression(0);
            }

            return Current.Kind switch
            {
                TokenKind.Write or TokenKind.Read or TokenKind.Omit =>
                    ParseAccessModeDeclaration(start, stateTarget, guard),
                TokenKind.Ensure =>
                    ParseStateEnsureDeclaration(start, EnsureAnchor.In, stateTarget, guard),
                _ => EmitUnexpectedAndResync(start),
            };
        }

        private Declaration ParseToStatement()
        {
            var start = Current;
            Consume(TokenKind.To);
            var stateTarget = ParseStateTarget();

            Expression? guard = null;
            if (Current.Kind == TokenKind.When)
            {
                Advance();
                guard = ParseExpression(0);
            }

            return Current.Kind switch
            {
                TokenKind.Ensure =>
                    ParseStateEnsureDeclaration(start, EnsureAnchor.To, stateTarget, guard),
                TokenKind.Arrow =>
                    ParseStateActionDeclaration(start, StateActionAnchor.To, stateTarget, guard),
                _ => EmitUnexpectedAndResync(start),
            };
        }

        private Declaration ParseFromStatement()
        {
            var start = Current;
            Consume(TokenKind.From);
            var stateTarget = ParseStateTarget();

            if (Current.Kind == TokenKind.On)
                return ParseTransitionRowDeclaration(start, stateTarget);

            Expression? guard = null;
            if (Current.Kind == TokenKind.When)
            {
                Advance();
                guard = ParseExpression(0);
            }

            return Current.Kind switch
            {
                TokenKind.Ensure =>
                    ParseStateEnsureDeclaration(start, EnsureAnchor.From, stateTarget, guard),
                TokenKind.Arrow =>
                    ParseStateActionDeclaration(start, StateActionAnchor.From, stateTarget, guard),
                _ => EmitUnexpectedAndResync(start),
            };
        }

        private Declaration ParseOnStatement()
        {
            var start = Current;
            Consume(TokenKind.On);
            var eventName = Expect(TokenKind.Identifier, "an event name");

            if (Current.Kind is TokenKind.When or TokenKind.Ensure)
                return ParseEventEnsureDeclaration(start, eventName);

            return ParseStatelessEventHookDeclaration(start, eventName);
        }

        private Declaration EmitUnexpectedAndResync(Token start)
        {
            AddDiagnostic(DiagnosticCode.UnexpectedKeyword, SpanOf(Current), Current.Text, "this context");
            SkipToNextSyncPoint();
            var missing = MissingToken(TokenKind.Identifier);
            // Recovery node uses RuleDeclaration — cheapest two-child shape for a discardable placeholder.
            return new RuleDeclaration(
                SourceSpan.Covering(SpanOf(start), SpanOf(Current)),
                new IdentifierExpression(SourceSpan.Missing, missing) { IsMissing = true },
                null,
                new StringLiteralExpression(SourceSpan.Missing, MissingToken(TokenKind.StringLiteral)) { IsMissing = true }
            ) { IsMissing = true };
        }

        // ── Transition row ─────────────────────────────────────────────────────

        private TransitionRowDeclaration ParseTransitionRowDeclaration(Token start, StateTarget fromStates)
        {
            Consume(TokenKind.On);
            var eventName = Expect(TokenKind.Identifier, "an event name");

            Expression? guard = null;
            if (Current.Kind == TokenKind.When)
            {
                Advance();
                guard = ParseExpression(0);
            }

            var actions = ImmutableArray.CreateBuilder<Statement>();
            OutcomeNode? outcome = null;

            while (Current.Kind == TokenKind.Arrow)
            {
                Advance();
                if (IsActionKeyword(Current.Kind))
                {
                    actions.Add(ParseActionStatement());
                }
                else
                {
                    outcome = ParseOutcome();
                    break;
                }
            }

            outcome ??= MissingOutcome();

            return new TransitionRowDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                fromStates, eventName, guard, actions.ToImmutable(), outcome);
        }

        private OutcomeNode ParseOutcome()
        {
            return Current.Kind switch
            {
                TokenKind.Transition => ParseTransitionOutcome(),
                TokenKind.No         => ParseNoTransitionOutcome(),
                TokenKind.Reject     => ParseRejectOutcome(),
                _ => MissingOutcome(),
            };
        }

        private TransitionOutcomeNode ParseTransitionOutcome()
        {
            var start = Current;
            Consume(TokenKind.Transition);
            var name = Expect(TokenKind.Identifier, "a state name");
            return new TransitionOutcomeNode(
                SourceSpan.Covering(SpanOf(start), SpanOf(name)), name);
        }

        private NoTransitionOutcomeNode ParseNoTransitionOutcome()
        {
            var start = Current;
            Consume(TokenKind.No);
            var kw = Expect(TokenKind.Transition);
            return new NoTransitionOutcomeNode(
                SourceSpan.Covering(SpanOf(start), SpanOf(kw)));
        }

        private RejectOutcomeNode ParseRejectOutcome()
        {
            var start = Current;
            Consume(TokenKind.Reject);
            var message = ParseExpression(0);
            return new RejectOutcomeNode(
                SourceSpan.Covering(SpanOf(start), message.Span), message);
        }

        private OutcomeNode MissingOutcome()
        {
            AddDiagnostic(DiagnosticCode.ExpectedToken, SpanOf(Current),
                "transition, no transition, or reject", Current.Text);
            return new TransitionOutcomeNode(
                SpanOf(Current), MissingToken(TokenKind.Identifier)) { IsMissing = true };
        }

        // ── Ensure declarations ────────────────────────────────────────────────

        private StateEnsureDeclaration ParseStateEnsureDeclaration(
            Token start, EnsureAnchor anchor, StateTarget states, Expression? guard)
        {
            Consume(TokenKind.Ensure);
            var condition = ParseExpression(0);
            Expect(TokenKind.Because);
            var message = ParseExpression(0);

            return new StateEnsureDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                anchor, states, condition, guard, message);
        }

        private EventEnsureDeclaration ParseEventEnsureDeclaration(Token start, Token eventName)
        {
            Expression? guard = null;
            if (Current.Kind == TokenKind.When)
            {
                Advance();
                guard = ParseExpression(0);
            }

            Consume(TokenKind.Ensure);
            var condition = ParseExpression(0);
            Expect(TokenKind.Because);
            var message = ParseExpression(0);

            return new EventEnsureDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                eventName, condition, guard, message);
        }

        // ── Hook / state-action declarations ───────────────────────────────────

        private StatelessEventHookDeclaration ParseStatelessEventHookDeclaration(
            Token start, Token eventName)
        {
            var actions = ParseArrowActions();
            return new StatelessEventHookDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                eventName, actions);
        }

        private StateActionDeclaration ParseStateActionDeclaration(
            Token start, StateActionAnchor anchor, StateTarget states, Expression? guard)
        {
            var actions = ParseArrowActions();
            return new StateActionDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                anchor, states, guard, actions);
        }

        private AccessModeDeclaration ParseAccessModeDeclaration(
            Token start, StateTarget stateTarget, Expression? guard)
        {
            var mode = Current.Kind switch
            {
                TokenKind.Write => AccessMode.Write,
                TokenKind.Read  => AccessMode.Read,
                TokenKind.Omit  => AccessMode.Omit,
                _ => AccessMode.Write
            };
            Advance();
            var fields = ParseFieldTarget();

            return new AccessModeDeclaration(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                stateTarget, mode, fields, guard);
        }

        // ── Action chain ───────────────────────────────────────────────────────

        private ImmutableArray<Statement> ParseArrowActions()
        {
            var actions = ImmutableArray.CreateBuilder<Statement>();
            while (Current.Kind == TokenKind.Arrow)
            {
                Advance();
                if (IsActionKeyword(Current.Kind))
                    actions.Add(ParseActionStatement());
                else
                    break;
            }
            return actions.ToImmutable();
        }

        private static bool IsActionKeyword(TokenKind k) => k is
            TokenKind.Set or TokenKind.Add or TokenKind.Remove or
            TokenKind.Enqueue or TokenKind.Dequeue or TokenKind.Push or
            TokenKind.Pop or TokenKind.Clear;

        private Statement ParseActionStatement()
        {
            var start = Current;
            return Current.Kind switch
            {
                TokenKind.Set     => ParseSetAction(start),
                TokenKind.Add     => ParseFieldValueAction(start),
                TokenKind.Remove  => ParseFieldValueAction(start),
                TokenKind.Enqueue => ParseFieldValueAction(start),
                TokenKind.Push    => ParseFieldValueAction(start),
                TokenKind.Dequeue => ParseTransferAction(start),
                TokenKind.Pop     => ParseTransferAction(start),
                TokenKind.Clear   => ParseClearAction(start),
                _ => ParseMissingAction(start),
            };
        }

        private SetActionStatement ParseSetAction(Token start)
        {
            Consume(TokenKind.Set);
            var field = Expect(TokenKind.Identifier, "a field name");
            Expect(TokenKind.Assign);
            var value = ParseExpression(0);
            return new SetActionStatement(
                SourceSpan.Covering(SpanOf(start), value.Span), field, value);
        }

        private Statement ParseFieldValueAction(Token start)
        {
            var kind = Current.Kind;
            Advance();
            var field = Expect(TokenKind.Identifier, "a field name");
            var value = ParseExpression(0);
            var span = SourceSpan.Covering(SpanOf(start), value.Span);
            return kind switch
            {
                TokenKind.Add     => new AddActionStatement(span, field, value),
                TokenKind.Remove  => new RemoveActionStatement(span, field, value),
                TokenKind.Enqueue => new EnqueueActionStatement(span, field, value),
                TokenKind.Push    => new PushActionStatement(span, field, value),
                _ => throw new InvalidOperationException()
            };
        }

        private Statement ParseTransferAction(Token start)
        {
            var kind = Current.Kind;
            Advance();
            var field = Expect(TokenKind.Identifier, "a field name");
            Token? into = null;
            if (Current.Kind == TokenKind.Into)
            {
                Advance();
                into = Expect(TokenKind.Identifier, "a field name");
            }
            var span = SourceSpan.Covering(SpanOf(start), LastSpan());
            return kind switch
            {
                TokenKind.Dequeue => new DequeueActionStatement(span, field, into),
                TokenKind.Pop     => new PopActionStatement(span, field, into),
                _ => throw new InvalidOperationException()
            };
        }

        private ClearActionStatement ParseClearAction(Token start)
        {
            Consume(TokenKind.Clear);
            var field = Expect(TokenKind.Identifier, "a field name");
            return new ClearActionStatement(
                SourceSpan.Covering(SpanOf(start), SpanOf(field)), field);
        }

        private Statement ParseMissingAction(Token start)
        {
            AddDiagnostic(DiagnosticCode.ExpectedToken, SpanOf(Current),
                "an action like set, add, or remove", Current.Text);
            var missing = MissingToken(TokenKind.Identifier);
            return new SetActionStatement(SpanOf(start), missing,
                new IdentifierExpression(SourceSpan.Missing, missing) { IsMissing = true }
            ) { IsMissing = true };
        }

        // ── Auxiliary: state target, field target ──────────────────────────────

        private StateTarget ParseStateTarget()
        {
            var start = Current;
            if (Current.Kind == TokenKind.Any)
            {
                Advance();
                return new StateTarget(SpanOf(start), true, ImmutableArray<Token>.Empty);
            }

            var names = ImmutableArray.CreateBuilder<Token>();
            names.Add(Expect(TokenKind.Identifier, "a state name"));
            while (Current.Kind == TokenKind.Comma)
            {
                Advance();
                names.Add(Expect(TokenKind.Identifier, "a state name"));
            }
            return new StateTarget(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                false, names.ToImmutable());
        }

        private FieldTarget ParseFieldTarget()
        {
            var start = Current;
            if (Current.Kind == TokenKind.All)
            {
                Advance();
                return new FieldTarget(SpanOf(start), true, ImmutableArray<Token>.Empty);
            }

            var names = ImmutableArray.CreateBuilder<Token>();
            names.Add(Expect(TokenKind.Identifier, "a field name"));
            while (Current.Kind == TokenKind.Comma)
            {
                Advance();
                names.Add(Expect(TokenKind.Identifier, "a field name"));
            }
            return new FieldTarget(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                false, names.ToImmutable());
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Type references + field modifiers
        // ════════════════════════════════════════════════════════════════════════

        private TypeRef ParseTypeRef()
        {
            var start = Current;

            // Collection: set of T, queue of T, stack of T
            if (Current.Kind == TokenKind.Set && Peek(1).Kind == TokenKind.Of)
                return ParseCollectionTypeRef(start, CollectionKind.Set);
            if (Current.Kind == TokenKind.QueueType)
                return ParseCollectionTypeRef(start, CollectionKind.Queue);
            if (Current.Kind == TokenKind.StackType)
                return ParseCollectionTypeRef(start, CollectionKind.Stack);

            // Choice
            if (Current.Kind == TokenKind.ChoiceType)
                return ParseChoiceTypeRef(start);

            // Scalar
            var kind = TryMapScalarKind(Current.Kind);
            if (kind.HasValue)
            {
                Advance();
                TypeQualifier? qualifier = null;
                if (Current.Kind is TokenKind.In or TokenKind.Of)
                    qualifier = ParseTypeQualifier();
                TypeQualifier? secondQualifier = null;
                if (qualifier != null && Current.Kind is TokenKind.In or TokenKind.Of)
                    secondQualifier = ParseTypeQualifier();
                return new ScalarTypeRef(
                    SourceSpan.Covering(SpanOf(start), LastSpan()),
                    kind.Value, qualifier, SecondQualifier: secondQualifier);
            }

            // Missing type
            AddDiagnostic(DiagnosticCode.ExpectedToken, SpanOf(Current), "type name", Current.Text);
            return new ScalarTypeRef(SpanOf(start), ScalarTypeKind.String, null) { IsMissing = true };
        }

        private CollectionTypeRef ParseCollectionTypeRef(Token start, CollectionKind kind)
        {
            Advance(); // consume set/queue/stack
            Expect(TokenKind.Of);
            var elemStart = Current;

            // ~string: case-insensitive string inner type
            bool caseInsensitive = false;
            if (Current.Kind == TokenKind.Tilde)
            {
                caseInsensitive = true;
                Advance(); // consume ~
            }

            var elemKind = TryMapScalarKind(Current.Kind);
            if (elemKind.HasValue)
            {
                Advance();
                TypeQualifier? qualifier = null;
                if (Current.Kind is TokenKind.In or TokenKind.Of)
                    qualifier = ParseTypeQualifier();
                var elem = new ScalarTypeRef(
                    SourceSpan.Covering(SpanOf(elemStart), LastSpan()),
                    elemKind.Value, qualifier, caseInsensitive);
                return new CollectionTypeRef(
                    SourceSpan.Covering(SpanOf(start), LastSpan()), kind, elem);
            }
            AddDiagnostic(DiagnosticCode.ExpectedToken, SpanOf(Current), "the type for items in this collection (string, number, ...)", Current.Text);
            var missing = new ScalarTypeRef(SpanOf(elemStart), ScalarTypeKind.String, null) { IsMissing = true };
            return new CollectionTypeRef(SpanOf(start), kind, missing) { IsMissing = true };
        }

        private ChoiceTypeRef ParseChoiceTypeRef(Token start)
        {
            Consume(TokenKind.ChoiceType);
            Expect(TokenKind.LeftParen);
            var choices = ImmutableArray.CreateBuilder<Expression>();
            if (Current.Kind != TokenKind.RightParen)
            {
                choices.Add(ParseExpression(0));
                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    choices.Add(ParseExpression(0));
                }
            }
            Expect(TokenKind.RightParen);
            return new ChoiceTypeRef(
                SourceSpan.Covering(SpanOf(start), LastSpan()),
                choices.ToImmutable());
        }

        private TypeQualifier ParseTypeQualifier()
        {
            var start = Current;
            var kind = Current.Kind == TokenKind.In
                ? TypeQualifierKind.In : TypeQualifierKind.Of;
            Advance();
            var value = ParseExpression(0);
            return new TypeQualifier(
                SourceSpan.Covering(SpanOf(start), value.Span), kind, value);
        }

        private static ScalarTypeKind? TryMapScalarKind(TokenKind k) => k switch
        {
            TokenKind.StringType        => ScalarTypeKind.String,
            TokenKind.NumberType        => ScalarTypeKind.Number,
            TokenKind.IntegerType       => ScalarTypeKind.Integer,
            TokenKind.DecimalType       => ScalarTypeKind.Decimal,
            TokenKind.BooleanType       => ScalarTypeKind.Boolean,
            TokenKind.DateType          => ScalarTypeKind.Date,
            TokenKind.TimeType          => ScalarTypeKind.Time,
            TokenKind.InstantType       => ScalarTypeKind.Instant,
            TokenKind.DurationType      => ScalarTypeKind.Duration,
            TokenKind.PeriodType        => ScalarTypeKind.Period,
            TokenKind.TimezoneType      => ScalarTypeKind.Timezone,
            TokenKind.ZonedDateTimeType => ScalarTypeKind.ZonedDateTime,
            TokenKind.DateTimeType      => ScalarTypeKind.DateTime,
            TokenKind.MoneyType         => ScalarTypeKind.Money,
            TokenKind.CurrencyType      => ScalarTypeKind.Currency,
            TokenKind.QuantityType      => ScalarTypeKind.Quantity,
            TokenKind.UnitOfMeasureType => ScalarTypeKind.UnitOfMeasure,
            TokenKind.DimensionType     => ScalarTypeKind.Dimension,
            TokenKind.PriceType         => ScalarTypeKind.Price,
            TokenKind.ExchangeRateType  => ScalarTypeKind.ExchangeRate,
            _ => null
        };

        private ImmutableArray<FieldModifier> ParseFieldModifiers()
        {
            var mods = ImmutableArray.CreateBuilder<FieldModifier>();
            while (IsFieldModifierStart(Current.Kind))
                mods.Add(ParseFieldModifier());
            return mods.ToImmutable();
        }

        private FieldModifier ParseFieldModifier()
        {
            var start = Current;
            var span = SpanOf(start);

            if (IsValueModifier(Current.Kind))
            {
                var kind = Current.Kind;
                Advance();
                var value = ParseExpression(0);
                var fullSpan = SourceSpan.Covering(span, value.Span);
                return kind switch
                {
                    TokenKind.Default   => new DefaultModifier(fullSpan, value),
                    TokenKind.Min       => new MinModifier(fullSpan, value),
                    TokenKind.Max       => new MaxModifier(fullSpan, value),
                    TokenKind.Minlength => new MinLengthModifier(fullSpan, value),
                    TokenKind.Maxlength => new MaxLengthModifier(fullSpan, value),
                    TokenKind.Mincount  => new MinCountModifier(fullSpan, value),
                    TokenKind.Maxcount  => new MaxCountModifier(fullSpan, value),
                    TokenKind.Maxplaces => new MaxPlacesModifier(fullSpan, value),
                    _ => throw new InvalidOperationException()
                };
            }

            // Flag modifiers (no value)
            Advance();
            return start.Kind switch
            {
                TokenKind.Optional    => new OptionalModifier(span),
                TokenKind.Ordered     => new OrderedModifier(span),
                TokenKind.Nonnegative => new NonnegativeModifier(span),
                TokenKind.Positive    => new PositiveModifier(span),
                TokenKind.Nonzero     => new NonzeroModifier(span),
                TokenKind.Notempty    => new NotemptyModifier(span),
                _ => throw new InvalidOperationException()
            };
        }

        private static bool IsValueModifier(TokenKind k) => k is
            TokenKind.Default or TokenKind.Min or TokenKind.Max or
            TokenKind.Minlength or TokenKind.Maxlength or
            TokenKind.Mincount or TokenKind.Maxcount or TokenKind.Maxplaces;

        private static bool IsFieldModifierStart(TokenKind k) => k is
            TokenKind.Optional or TokenKind.Ordered or TokenKind.Nonnegative or
            TokenKind.Positive or TokenKind.Nonzero or TokenKind.Notempty or
            TokenKind.Default or TokenKind.Min or TokenKind.Max or
            TokenKind.Minlength or TokenKind.Maxlength or
            TokenKind.Mincount or TokenKind.Maxcount or TokenKind.Maxplaces;

        // ════════════════════════════════════════════════════════════════════════
        //  Pratt expression parser
        // ════════════════════════════════════════════════════════════════════════

        private Expression ParseExpression(int minBp)
        {
            var left = Nud();
            while (true)
            {
                var lbp = LeftBindingPower(Current.Kind);
                if (lbp <= minBp) break;
                left = Led(left);
            }
            return left;
        }

        // ── Null-denotation (atoms + prefix) ───────────────────────────────────

        private Expression Nud()
        {
            var start = Current;
            return Current.Kind switch
            {
                TokenKind.Identifier        => NudIdentifier(),
                TokenKind.NumberLiteral     => NudNumber(),
                TokenKind.True              => NudBoolean(true),
                TokenKind.False             => NudBoolean(false),
                TokenKind.StringLiteral     => NudString(),
                TokenKind.StringStart       => ParseInterpolatedString(),
                TokenKind.TypedConstant     => NudTypedConstant(),
                TokenKind.TypedConstantStart => ParseInterpolatedTypedConstant(),
                TokenKind.LeftBracket       => ParseListLiteral(),
                TokenKind.LeftParen         => NudParen(),
                TokenKind.Not              => NudNot(),
                TokenKind.Minus            => NudNegate(),
                TokenKind.If               => ParseConditionalExpression(),
                _ => NudMissing(start),
            };
        }

        private IdentifierExpression NudIdentifier()
        {
            var t = Advance();
            return new IdentifierExpression(SpanOf(t), t);
        }

        private NumberLiteralExpression NudNumber()
        {
            var t = Advance();
            return new NumberLiteralExpression(SpanOf(t), t);
        }

        private BooleanLiteralExpression NudBoolean(bool value)
        {
            var t = Advance();
            return new BooleanLiteralExpression(SpanOf(t), value);
        }

        private StringLiteralExpression NudString()
        {
            var t = Advance();
            return new StringLiteralExpression(SpanOf(t), t);
        }

        private TypedConstantExpression NudTypedConstant()
        {
            var t = Advance();
            return new TypedConstantExpression(SpanOf(t), t);
        }

        private ParenthesizedExpression NudParen()
        {
            var start = Current;
            Consume(TokenKind.LeftParen);
            var inner = ParseExpression(0);
            var close = Expect(TokenKind.RightParen);
            return new ParenthesizedExpression(
                SourceSpan.Covering(SpanOf(start), SpanOf(close)), inner);
        }

        private UnaryExpression NudNot()
        {
            var start = Advance();
            var operand = ParseExpression(25);
            return new UnaryExpression(
                SourceSpan.Covering(SpanOf(start), operand.Span),
                UnaryOp.Not, operand);
        }

        private UnaryExpression NudNegate()
        {
            var start = Advance();
            var operand = ParseExpression(65);
            return new UnaryExpression(
                SourceSpan.Covering(SpanOf(start), operand.Span),
                UnaryOp.Negate, operand);
        }

        private ConditionalExpression ParseConditionalExpression()
        {
            var start = Current;
            Consume(TokenKind.If);
            var condition = ParseExpression(0);
            Expect(TokenKind.Then);
            var consequence = ParseExpression(0);
            Expect(TokenKind.Else);
            var alternative = ParseExpression(0);
            return new ConditionalExpression(
                SourceSpan.Covering(SpanOf(start), alternative.Span),
                condition, consequence, alternative);
        }

        private ListLiteralExpression ParseListLiteral()
        {
            var start = Current;
            Consume(TokenKind.LeftBracket);
            var elements = ImmutableArray.CreateBuilder<Expression>();
            if (Current.Kind != TokenKind.RightBracket)
            {
                elements.Add(ParseExpression(0));
                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    elements.Add(ParseExpression(0));
                }
            }
            var close = Expect(TokenKind.RightBracket);
            return new ListLiteralExpression(
                SourceSpan.Covering(SpanOf(start), SpanOf(close)),
                elements.ToImmutable());
        }

        private Expression NudMissing(Token start)
        {
            AddDiagnostic(DiagnosticCode.ExpectedToken, SpanOf(start), "a value or condition", start.Text);
            return new IdentifierExpression(SpanOf(start),
                MissingToken(TokenKind.Identifier)) { IsMissing = true };
        }

        // ── Left-denotation (infix + postfix) ──────────────────────────────────

        private Expression Led(Expression left)
        {
            var op = Current;
            Advance();

            return op.Kind switch
            {
                // Logical
                TokenKind.Or  => MakeBinary(left, BinaryOp.Or, 10),
                TokenKind.And => MakeBinary(left, BinaryOp.And, 20),

                // Comparison (non-associative: rbp = 31, explicit chaining check)
                TokenKind.DoubleEquals       => MakeComparison(left, BinaryOp.Equal, op),
                TokenKind.NotEquals          => MakeComparison(left, BinaryOp.NotEqual, op),
                TokenKind.CaseInsensitiveEquals    => MakeComparison(left, BinaryOp.CaseInsensitiveEqual, op),
                TokenKind.CaseInsensitiveNotEquals => MakeComparison(left, BinaryOp.CaseInsensitiveNotEqual, op),
                TokenKind.LessThan           => MakeComparison(left, BinaryOp.Less, op),
                TokenKind.GreaterThan        => MakeComparison(left, BinaryOp.Greater, op),
                TokenKind.LessThanOrEqual    => MakeComparison(left, BinaryOp.LessOrEqual, op),
                TokenKind.GreaterThanOrEqual => MakeComparison(left, BinaryOp.GreaterOrEqual, op),

                // Membership
                TokenKind.Contains => MakeContains(left),
                TokenKind.Is       => ParseIsExpression(left),

                // Arithmetic
                TokenKind.Plus    => MakeBinary(left, BinaryOp.Plus, 50),
                TokenKind.Minus   => MakeBinary(left, BinaryOp.Minus, 50),
                TokenKind.Star    => MakeBinary(left, BinaryOp.Star, 60),
                TokenKind.Slash   => MakeBinary(left, BinaryOp.Slash, 60),
                TokenKind.Percent => MakeBinary(left, BinaryOp.Percent, 60),

                // Member access + call
                TokenKind.Dot       => ParseMemberAccess(left),
                TokenKind.LeftParen => ParseCallOrMethodCall(left),

                _ => left,
            };
        }

        private BinaryExpression MakeBinary(Expression left, BinaryOp op, int rbp)
        {
            var right = ParseExpression(rbp);
            return new BinaryExpression(
                SourceSpan.Covering(left.Span, right.Span), left, op, right);
        }

        private static bool IsComparisonOp(BinaryOp op) => op is
            BinaryOp.Equal or BinaryOp.NotEqual or
            BinaryOp.CaseInsensitiveEqual or BinaryOp.CaseInsensitiveNotEqual or
            BinaryOp.Less or
            BinaryOp.Greater or BinaryOp.LessOrEqual or BinaryOp.GreaterOrEqual;

        private BinaryExpression MakeComparison(Expression left, BinaryOp op, Token opToken)
        {
            if (left is BinaryExpression { Op: var prevOp } && IsComparisonOp(prevOp))
                AddDiagnostic(DiagnosticCode.NonAssociativeComparison, SpanOf(opToken),
                    "use 'and' to combine them");
            return MakeBinary(left, op, 31);
        }

        private ContainsExpression MakeContains(Expression left)
        {
            var right = ParseExpression(40);
            return new ContainsExpression(
                SourceSpan.Covering(left.Span, right.Span), left, right);
        }

        private IsSetExpression ParseIsExpression(Expression left)
        {
            bool isNot = false;
            if (Current.Kind == TokenKind.Not)
            {
                isNot = true;
                Advance();
            }
            Expect(TokenKind.Set);
            return new IsSetExpression(
                SourceSpan.Covering(left.Span, LastSpan()), left, isNot);
        }

        private MemberAccessExpression ParseMemberAccess(Expression left)
        {
            var member = Expect(TokenKind.Identifier, "a member name after '.'");
            return new MemberAccessExpression(
                SourceSpan.Covering(left.Span, SpanOf(member)), left, member);
        }

        private Expression ParseCallOrMethodCall(Expression left)
        {
            var args = ParseCallArgs();

            if (left is MemberAccessExpression mac)
            {
                return new MethodCallExpression(
                    SourceSpan.Covering(mac.Object.Span, LastSpan()),
                    mac.Object, mac.Member, args);
            }
            if (left is IdentifierExpression ide)
            {
                return new CallExpression(
                    SourceSpan.Covering(ide.Span, LastSpan()),
                    ide.Name, args);
            }

            AddDiagnostic(DiagnosticCode.InvalidCallTarget, left.Span, left.ToString()!);
            return left;
        }

        private ImmutableArray<Expression> ParseCallArgs()
        {
            var args = ImmutableArray.CreateBuilder<Expression>();
            if (Current.Kind != TokenKind.RightParen)
            {
                args.Add(ParseExpression(0));
                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    args.Add(ParseExpression(0));
                }
            }
            Expect(TokenKind.RightParen);
            return args.ToImmutable();
        }

        // ── Binding power table ────────────────────────────────────────────────

        private static int LeftBindingPower(TokenKind k) => k switch
        {
            TokenKind.Or                   => 10,
            TokenKind.And                  => 20,
            TokenKind.DoubleEquals         => 30,
            TokenKind.NotEquals            => 30,
            TokenKind.CaseInsensitiveEquals => 30,
            TokenKind.CaseInsensitiveNotEquals => 30,
            TokenKind.LessThan             => 30,
            TokenKind.GreaterThan          => 30,
            TokenKind.LessThanOrEqual      => 30,
            TokenKind.GreaterThanOrEqual   => 30,
            TokenKind.Contains             => 40,
            TokenKind.Is                   => 40,
            TokenKind.Plus                 => 50,
            TokenKind.Minus                => 50,
            TokenKind.Star                 => 60,
            TokenKind.Slash                => 60,
            TokenKind.Percent              => 60,
            TokenKind.Dot                  => 80,
            TokenKind.LeftParen            => 80,
            _ => 0
        };

        // ── Interpolation reassembly ───────────────────────────────────────────

        private InterpolatedStringExpression ParseInterpolatedString()
        {
            var segments = ImmutableArray.CreateBuilder<InterpolationSegment>();
            var startToken = Consume(TokenKind.StringStart);
            var startSpan = SpanOf(startToken);
            segments.Add(new TextSegment(startSpan, startToken));

            while (true)
            {
                var expr = ParseExpression(0);
                segments.Add(new ExpressionSegment(expr.Span, expr));

                if (Current.Kind == TokenKind.StringMiddle)
                {
                    var mid = Advance();
                    segments.Add(new TextSegment(SpanOf(mid), mid));
                }
                else
                {
                    var end = Expect(TokenKind.StringEnd);
                    segments.Add(new TextSegment(SpanOf(end), end));
                    break;
                }
            }

            return new InterpolatedStringExpression(
                SourceSpan.Covering(startSpan, segments[^1].Span),
                segments.ToImmutable());
        }

        private InterpolatedTypedConstantExpression ParseInterpolatedTypedConstant()
        {
            var segments = ImmutableArray.CreateBuilder<InterpolationSegment>();
            var startToken = Consume(TokenKind.TypedConstantStart);
            var startSpan = SpanOf(startToken);
            segments.Add(new TextSegment(startSpan, startToken));

            while (true)
            {
                var expr = ParseExpression(0);
                segments.Add(new ExpressionSegment(expr.Span, expr));

                if (Current.Kind == TokenKind.TypedConstantMiddle)
                {
                    var mid = Advance();
                    segments.Add(new TextSegment(SpanOf(mid), mid));
                }
                else
                {
                    var end = Expect(TokenKind.TypedConstantEnd);
                    segments.Add(new TextSegment(SpanOf(end), end));
                    break;
                }
            }

            return new InterpolatedTypedConstantExpression(
                SourceSpan.Covering(startSpan, segments[^1].Span),
                segments.ToImmutable());
        }

        // ── Span helpers ───────────────────────────────────────────────────────

        private SourceSpan LastSpan()
        {
            for (int i = _pos - 1; i >= 0; i--)
            {
                if (!IsTrivia(_tokens[i].Kind))
                    return SpanOf(_tokens[i]);
            }
            return SpanOf(Current);
        }
    }
}
