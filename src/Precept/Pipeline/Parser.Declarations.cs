using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline.SyntaxNodes;

namespace Precept.Pipeline;

public static partial class Parser
{
    internal ref partial struct ParseSession
    {
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

            // Post-condition when-guard: `ensure Cond when Guard because "msg"` (spec §2.2)
            // stashedGuard is a pre-ensure guard parsed before the 'ensure' keyword in the
            // dispatch flow. If no stashed guard exists, consume a post-condition when-guard here.
            Expression? guard = stashedGuard;
            if (guard is null && Current().Kind == TokenKind.When)
            {
                Advance(); // consume 'when'
                guard = ParseExpression(0);
            }

            Expect(TokenKind.Because);
            var message = ParseExpression(0);

            return new StateEnsureNode(
                SourceSpan.Covering(start, message.Span),
                preposition, anchor, guard, condition, message);
        }

        // ── to-scoped construct parsers ───────────────────────────────────────

        private StateActionNode ParseStateAction(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
        {
            // First arrow is the disambiguation token
            Advance(); // consume '->'
            var first = ParseActionStatement();
            var actions = ImmutableArray.CreateBuilder<Statement>();
            actions.Add(first);

            while (Current().Kind == TokenKind.Arrow && !IsOutcomeAhead())
            {
                Advance(); // consume '->'
                actions.Add(ParseActionStatement());
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
            while (Current().Kind == TokenKind.Arrow && !IsOutcomeAhead())
            {
                Advance(); // consume '->'
                var action = TryParseActionStatementWithRecovery();
                if (action is not null)
                    actions.Add(action);
            }

            // Parse outcome
            OutcomeNode outcome;
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

            // Post-condition when-guard: `on Event ensure Cond when Guard because "msg"` (spec §2.2)
            Expression? guard = stashedGuard;
            if (guard is null && Current().Kind == TokenKind.When)
            {
                Advance(); // consume 'when'
                guard = ParseExpression(0);
            }

            Expect(TokenKind.Because);
            var message = ParseExpression(0);

            return new EventEnsureNode(
                SourceSpan.Covering(start, message.Span),
                eventName, guard, condition, message);
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

            while (Current().Kind == TokenKind.Arrow && !IsOutcomeAhead())
            {
                Advance(); // consume '->'
                actions.Add(ParseActionStatement());
            }

            Expression? postConditionGuard = null;
            if (Current().Kind == TokenKind.Ensure)
            {
                Advance(); // consume 'ensure'
                postConditionGuard = ParseExpression(0);
            }

            var lastSpan = postConditionGuard?.Span ?? actions[^1].Span;
            return new EventHandlerNode(
                SourceSpan.Covering(start, lastSpan),
                eventName, actions.ToImmutable(), postConditionGuard);
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
#pragma warning disable CS8524 // unnamed ActionKind values are unreachable — CS8509 enforces named-value coverage
            return meta.Kind switch
            {
                ActionKind.Set     => new SetStatement(span, field, value),
                ActionKind.Add     => throw new InvalidOperationException($"ActionKind.Add does not belong to the AssignValue shape"),
                ActionKind.Remove  => throw new InvalidOperationException($"ActionKind.Remove does not belong to the AssignValue shape"),
                ActionKind.Enqueue => throw new InvalidOperationException($"ActionKind.Enqueue does not belong to the AssignValue shape"),
                ActionKind.Dequeue => throw new InvalidOperationException($"ActionKind.Dequeue does not belong to the AssignValue shape"),
                ActionKind.Push    => throw new InvalidOperationException($"ActionKind.Push does not belong to the AssignValue shape"),
                ActionKind.Pop     => throw new InvalidOperationException($"ActionKind.Pop does not belong to the AssignValue shape"),
                ActionKind.Clear   => throw new InvalidOperationException($"ActionKind.Clear does not belong to the AssignValue shape"),
            };
#pragma warning restore CS8524
        }

        private Statement ParseCollectionValueStatement(ActionMeta meta)
        {
            var kw = Advance();
            var field = Expect(TokenKind.Identifier);
            var value = ParseExpression(0);
            var span = SourceSpan.Covering(kw.Span, value.Span);
#pragma warning disable CS8524 // unnamed ActionKind values are unreachable — CS8509 enforces named-value coverage
            return meta.Kind switch
            {
                ActionKind.Add     => new AddStatement(span, field, value),
                ActionKind.Remove  => new RemoveStatement(span, field, value),
                ActionKind.Enqueue => new EnqueueStatement(span, field, value),
                ActionKind.Push    => new PushStatement(span, field, value),
                ActionKind.Set     => throw new InvalidOperationException($"ActionKind.Set does not belong to the CollectionValue shape"),
                ActionKind.Dequeue => throw new InvalidOperationException($"ActionKind.Dequeue does not belong to the CollectionValue shape"),
                ActionKind.Pop     => throw new InvalidOperationException($"ActionKind.Pop does not belong to the CollectionValue shape"),
                ActionKind.Clear   => throw new InvalidOperationException($"ActionKind.Clear does not belong to the CollectionValue shape"),
            };
#pragma warning restore CS8524
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
#pragma warning disable CS8524 // unnamed ActionKind values are unreachable — CS8509 enforces named-value coverage
            return meta.Kind switch
            {
                ActionKind.Dequeue => new DequeueStatement(span, field, into),
                ActionKind.Pop     => new PopStatement(span, field, into),
                ActionKind.Set     => throw new InvalidOperationException($"ActionKind.Set does not belong to the CollectionInto shape"),
                ActionKind.Add     => throw new InvalidOperationException($"ActionKind.Add does not belong to the CollectionInto shape"),
                ActionKind.Remove  => throw new InvalidOperationException($"ActionKind.Remove does not belong to the CollectionInto shape"),
                ActionKind.Enqueue => throw new InvalidOperationException($"ActionKind.Enqueue does not belong to the CollectionInto shape"),
                ActionKind.Push    => throw new InvalidOperationException($"ActionKind.Push does not belong to the CollectionInto shape"),
                ActionKind.Clear   => throw new InvalidOperationException($"ActionKind.Clear does not belong to the CollectionInto shape"),
            };
#pragma warning restore CS8524
        }

        private Statement ParseFieldOnlyStatement(ActionMeta meta)
        {
            var kw = Advance();
            var field = Expect(TokenKind.Identifier);
            var span = SourceSpan.Covering(kw.Span, field.Span);
#pragma warning disable CS8524 // unnamed ActionKind values are unreachable — CS8509 enforces named-value coverage
            return meta.Kind switch
            {
                ActionKind.Clear   => new ClearStatement(span, field),
                ActionKind.Set     => throw new InvalidOperationException($"ActionKind.Set does not belong to the FieldOnly shape"),
                ActionKind.Add     => throw new InvalidOperationException($"ActionKind.Add does not belong to the FieldOnly shape"),
                ActionKind.Remove  => throw new InvalidOperationException($"ActionKind.Remove does not belong to the FieldOnly shape"),
                ActionKind.Enqueue => throw new InvalidOperationException($"ActionKind.Enqueue does not belong to the FieldOnly shape"),
                ActionKind.Dequeue => throw new InvalidOperationException($"ActionKind.Dequeue does not belong to the FieldOnly shape"),
                ActionKind.Push    => throw new InvalidOperationException($"ActionKind.Push does not belong to the FieldOnly shape"),
                ActionKind.Pop     => throw new InvalidOperationException($"ActionKind.Pop does not belong to the FieldOnly shape"),
            };
#pragma warning restore CS8524
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
            var start = Current().Span;
            Advance(); // consume 'field'

            // [0] Name(s)
            var nameTokens = ParseIdentifierListTokens();

            // [1] Type — 'as Type'
            Expect(TokenKind.As);
            var type = ParseTypeRef();

            // [2a] Pre-expression modifiers (e.g. optional, default N, min N, max N)
            var preModifiers = ParseFieldModifierNodes();

            // [3] Optional computed expression: '-> Expr'
            Expression? computed = null;
            ImmutableArray<FieldModifierNode> postModifiers = [];
            if (Current().Kind == TokenKind.Arrow)
            {
                Advance(); // consume '->'
                computed = ParseExpression(0);

                // [2b] Post-expression modifiers — GAP-B fix (e.g. nonnegative, positive after ->)
                postModifiers = ParseFieldModifierNodes();
            }

            var allModifiers = preModifiers.AddRange(postModifiers);

            SourceSpan lastSpan;
            if (postModifiers.Length > 0)
                lastSpan = postModifiers[^1].Span;
            else if (computed is not null)
                lastSpan = computed.Span;
            else if (preModifiers.Length > 0)
                lastSpan = preModifiers[^1].Span;
            else
                lastSpan = type.Span;

            return new FieldDeclarationNode(
                SourceSpan.Covering(start, lastSpan),
                nameTokens, type, allModifiers, computed);
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
        // CS8509 enforces named-value coverage here; #pragma CS8524 suppresses unnamed-integer noise.
#pragma warning disable CS8524
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
        };
#pragma warning restore CS8524

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

        /// <summary>
        /// Returns true when the current token introduces a type qualifier rather than
        /// a declaration boundary. Called only when the current type's <see cref="QualifierShape"/>
        /// is non-null — callers must guard with a catalog check before entering the qualifier loop.
        /// </summary>
        /// <remarks>
        /// Qualifier values are currently atomic (literal or identifier). If compound qualifier
        /// values are added, revisit this lookahead.
        /// </remarks>
        private bool TryPeekQualifierKeyword()
        {
            var cur = Current();

            // Only Of, In, To can introduce a qualifier
            if (cur.Kind is not (TokenKind.Of or TokenKind.In or TokenKind.To))
                return false;

            // 'of' is never a declaration leader — always a qualifier
            if (cur.Kind == TokenKind.Of)
                return true;

            // 'in' / 'to' may be declaration leaders — disambiguate
            if (!AmbiguousQualifierPrepositions.ContainsKey(cur.Kind))
                return true; // not a construct leader in any context — it's a qualifier

            var valueToken = Peek(1);
            // Literal value → unambiguously a qualifier (e.g. in 'USD', to 'EUR')
            if (valueToken.Kind is not TokenKind.Identifier and not TokenKind.Any)
                return true;

            // Identifier or 'any': check whether the token after it is a disambiguation verb
            var afterValue = Peek(2);
            return !AmbiguousQualifierPrepositions[cur.Kind].Contains(afterValue.Kind);
        }

        private TypeRefNode ParseTypeRef()
        {
            var current = Current();

            // "set" in type position: lexer emits Set, parser reinterprets
            if (current.Kind == TokenKind.Set || current.Kind == TokenKind.QueueType || current.Kind == TokenKind.StackType)
            {
                var collectionToken = Advance();
                Expect(TokenKind.Of);
                var elemToken = Advance(); // element type

                // Parse qualifiers on the element type if catalog permits them
                var qualifiers = ImmutableArray.CreateBuilder<TypeQualifierNode>();
                if (Types.ByToken.TryGetValue(elemToken.Kind, out var elemMeta) && elemMeta.QualifierShape is not null)
                {
                    while (TryPeekQualifierKeyword())
                    {
                        var qualKw = Advance();
                        var qualVal = ParseExpression(0);
                        qualifiers.Add(new TypeQualifierNode(
                            SourceSpan.Covering(qualKw.Span, qualVal.Span), qualKw, qualVal));
                    }
                }

                var builtQualifiers = qualifiers.ToImmutable();
                var endSpan = builtQualifiers.Length > 0 ? builtQualifiers[^1].Span : elemToken.Span;
                return new CollectionTypeRefNode(
                    SourceSpan.Covering(collectionToken.Span, endSpan),
                    collectionToken, elemToken, builtQualifiers);
            }

            if (current.Kind == TokenKind.ChoiceType)
            {
                var choiceToken = Advance();

                // 'of T' is required — bare choice(...) emits ChoiceMissingElementType
                if (!Match(TokenKind.Of))
                {
                    EmitDiagnostic(DiagnosticCode.ChoiceMissingElementType, choiceToken.Span);
                    ConsumeThrough(TokenKind.RightParen);
                    return new ChoiceTypeRefNode(choiceToken.Span, null, ImmutableArray<Expression>.Empty);
                }

                // Element type must be one of the 5 primitive keywords
                var elemToken = Current();
                if (!ChoiceElementTypeKeywords.Contains(elemToken.Kind))
                {
                    EmitDiagnostic(DiagnosticCode.ExpectedToken, elemToken.Span, "string, integer, decimal, number, or boolean", elemToken.Text);
                    ConsumeThrough(TokenKind.RightParen);
                    return new ChoiceTypeRefNode(choiceToken.Span, null, ImmutableArray<Expression>.Empty);
                }
                Advance(); // consume validated element type

                var options = ImmutableArray.CreateBuilder<Expression>();
                Expect(TokenKind.LeftParen);

                if (Current().Kind == TokenKind.RightParen)
                {
                    EmitDiagnostic(DiagnosticCode.EmptyChoice, Current().Span);
                }
                else
                {
                    do { options.Add(ParseChoiceValue(elemToken)); }
                    while (Match(TokenKind.Comma));
                }

                Expect(TokenKind.RightParen);
                var lastSpan = options.Count > 0 ? options[^1].Span : elemToken.Span;
                return new ChoiceTypeRefNode(SourceSpan.Covering(choiceToken.Span, lastSpan), elemToken, options.ToImmutable());
            }

            if (TypeKeywords.Contains(current.Kind))
            {
                var typeToken = Advance();

                // Consult catalog: only types with a QualifierShape accept qualifiers.
                // TryPeekQualifierKeyword() handles the in/to ambiguity within qualified types.
                var qualifiers = ImmutableArray.CreateBuilder<TypeQualifierNode>();
                if (Types.ByToken.TryGetValue(typeToken.Kind, out var typeMeta) && typeMeta.QualifierShape is not null)
                {
                    while (TryPeekQualifierKeyword())
                    {
                        var qualKw = Advance();
                        var qualVal = ParseExpression(0);
                        qualifiers.Add(new TypeQualifierNode(
                            SourceSpan.Covering(qualKw.Span, qualVal.Span), qualKw, qualVal));
                    }
                }

                var builtQualifiers = qualifiers.ToImmutable();
                var span = builtQualifiers.Length > 0
                    ? SourceSpan.Covering(typeToken.Span, builtQualifiers[^1].Span)
                    : typeToken.Span;
                return new ScalarTypeRefNode(span, typeToken, builtQualifiers);
            }

            // Unknown type — emit diagnostic and return a placeholder
            EmitDiagnostic(DiagnosticCode.ExpectedToken, current.Span, "type", current.Text);
            return new ScalarTypeRefNode(current.Span,
                new Token(TokenKind.Identifier, current.Text, current.Span), ImmutableArray<TypeQualifierNode>.Empty);
        }

        // ── Choice helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Parses a single choice value literal that must match the declared element type.
        /// Emits <see cref="DiagnosticCode.ChoiceElementTypeMismatch"/> if the literal kind
        /// doesn't match. Numeric element types (integer, decimal, number) all accept
        /// <see cref="TokenKind.NumberLiteral"/> — subtype discrimination is deferred to the type stage.
        /// </summary>
        private Expression ParseChoiceValue(Token elemToken)
        {
            var cur = Current();

            // For numeric element types, absorb an optional leading minus and fold into a signed literal.
            if (elemToken.Kind is TokenKind.IntegerType or TokenKind.DecimalType or TokenKind.NumberType)
            {
                Token? minusToken = null;
                if (cur.Kind == TokenKind.Minus)
                {
                    minusToken = Advance();
                    cur = Current();
                }

                if (cur.Kind == TokenKind.NumberLiteral)
                {
                    var tok = Advance();
                    if (minusToken is not null)
                    {
                        var negText = "-" + tok.Text;
                        var span = SourceSpan.Covering(minusToken.Value.Span, tok.Span);
                        tok = new Token(TokenKind.NumberLiteral, negText, span);
                    }
                    return new LiteralExpression(tok.Span, tok);
                }

                // Wrong literal kind
                EmitDiagnostic(DiagnosticCode.ChoiceElementTypeMismatch, cur.Span, elemToken.Text);
                if (cur.Kind is not (TokenKind.Comma or TokenKind.RightParen or TokenKind.EndOfSource))
                    Advance();
                return new LiteralExpression(cur.Span, cur);
            }

            bool isValid = elemToken.Kind switch
            {
                TokenKind.StringType  => cur.Kind == TokenKind.StringLiteral,
                TokenKind.BooleanType => cur.Kind is TokenKind.True or TokenKind.False,
                _                     => false,
            };

            if (!isValid)
            {
                EmitDiagnostic(DiagnosticCode.ChoiceElementTypeMismatch, cur.Span, elemToken.Text);
                if (cur.Kind is not (TokenKind.Comma or TokenKind.RightParen or TokenKind.EndOfSource))
                    Advance();
                return new LiteralExpression(cur.Span, cur);
            }

            return new LiteralExpression(cur.Span, Advance());
        }

        /// <summary>
        /// Consumes tokens until a <see cref="TokenKind.RightParen"/> is consumed or end-of-source.
        /// Used in error recovery to prevent cascade diagnostics after a malformed choice type.
        /// </summary>
        private void ConsumeThrough(TokenKind stopKind)
        {
            while (!IsAtEnd() && Current().Kind != stopKind)
                Advance();
            if (Current().Kind == stopKind)
                Advance();
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

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SourceSpan GetLastSlotSpan(SyntaxNode?[] slots, SourceSpan fallback)
        {
            for (int i = slots.Length - 1; i >= 0; i--)
                if (slots[i] is not null) return slots[i]!.Span;
            return fallback;
        }
    }
}
