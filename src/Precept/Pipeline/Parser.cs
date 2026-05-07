using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;

using ConstructsCatalog = Precept.Language.Constructs;
using DiagnosticsCatalog = Precept.Language.Diagnostics;

namespace Precept.Pipeline;

/// <summary>
/// Catalog-driven parser — transforms a <see cref="TokenStream"/> into a <see cref="ConstructManifest"/>.
/// Dispatch, slot walking, and vocabulary recognition are all derived from catalog metadata.
/// </summary>
public static class Parser
{
    // ── Catalog-derived vocabulary sets (computed once at startup) ────────────────

    private static readonly FrozenSet<TokenKind> StateModifierTokens =
        Modifiers.All.OfType<StateModifierMeta>()
            .Select(m => m.Token.Kind)
            .ToFrozenSet();

    private static readonly FrozenSet<TokenKind> FieldModifierTokens =
        Modifiers.ByFieldToken.Keys.ToFrozenSet();

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════════════════════════

    public static ConstructManifest Parse(TokenStream tokens)
    {
        var state = new ParserState(tokens.Tokens);
        state.ParseAll();
        return new ConstructManifest(
            state.Constructs.ToImmutableArray(),
            tokens.Diagnostics.AddRange(state.Diagnostics));
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  ParserState — private mutable cursor over the token array
    // ═══════════════════════════════════════════════════════════════════════════════

    private sealed class ParserState
    {
        private readonly ImmutableArray<Token> _tokens;
        private int _position;
        private readonly List<Diagnostic> _diagnostics = [];
        private readonly List<ParsedConstruct> _constructs = [];

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
        public IReadOnlyList<ParsedConstruct> Constructs => _constructs;

        public ParserState(ImmutableArray<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
            SkipTrivia();
        }

        // ── Token stream helpers ────────────────────────────────────────────────

        private Token Peek(int offset = 0)
        {
            var idx = _position;
            var skipped = 0;
            while (idx < _tokens.Length && skipped < offset)
            {
                idx++;
                if (idx < _tokens.Length && !IsTrivia(_tokens[idx].Kind))
                    skipped++;
            }
            return idx < _tokens.Length ? _tokens[idx] : EofToken();
        }

        private Token Advance()
        {
            var token = _tokens[_position];
            _position++;
            SkipTrivia();
            return token;
        }

        private bool Match(TokenKind kind)
        {
            if (Peek().Kind == kind)
            {
                Advance();
                return true;
            }
            return false;
        }

        private Token Expect(TokenKind kind)
        {
            var current = Peek();
            if (current.Kind == kind)
                return Advance();

            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, current.Span, kind.ToString(), current.Text));
            return new Token(kind, "", current.Span);
        }

        private bool IsAtEnd => Peek().Kind == TokenKind.EndOfSource;

        private void SkipTrivia()
        {
            while (_position < _tokens.Length && IsTrivia(_tokens[_position].Kind))
                _position++;
        }

        private static bool IsTrivia(TokenKind kind) =>
            kind == TokenKind.NewLine || kind == TokenKind.Comment;

        private Token EofToken() => new(TokenKind.EndOfSource, "",
            _tokens.Length > 0 ? _tokens[^1].Span : SourceSpan.Missing);

        // ── Main parse loop ─────────────────────────────────────────────────────

        public void ParseAll()
        {
            // Parse header (precept keyword expected first)
            if (Peek().Kind == TokenKind.Precept)
            {
                var headerMeta = ConstructsCatalog.GetMeta(ConstructKind.PreceptHeader);
                ParseConstruct(headerMeta);
            }

            // Main dispatch loop
            while (!IsAtEnd)
            {
                var current = Peek();
                if (ConstructsCatalog.ByLeadingToken.TryGetValue(current.Kind, out var candidates))
                {
                    // Find the first Direct or Header candidate (Slice 1 scope)
                    ConstructMeta? selected = null;
                    foreach (var (kind, _) in candidates)
                    {
                        var meta = ConstructsCatalog.GetMeta(kind);
                        if (meta.RoutingFamily == RoutingFamily.Direct)
                        {
                            selected = meta;
                            break;
                        }
                    }

                    if (selected != null)
                    {
                        ParseConstruct(selected);
                    }
                    else
                    {
                        // StateScoped/EventScoped — not implemented in Slice 1, skip to boundary
                        SkipToConstructBoundary();
                    }
                }
                else
                {
                    // No construct starts with this token — error recovery
                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.ExpectedToken, current.Span, "declaration keyword", current.Text));
                    SkipToConstructBoundary();
                }
            }
        }

        private void SkipToConstructBoundary()
        {
            Advance(); // consume at least one token to avoid infinite loop
            while (!IsAtEnd && !ConstructsCatalog.LeadingTokens.Contains(Peek().Kind))
                Advance();
        }

        // ── Construct parsing ───────────────────────────────────────────────────

        private void ParseConstruct(ConstructMeta meta)
        {
            var startToken = Advance(); // consume leading keyword
            var startSpan = startToken.Span;
            var slots = new List<SlotValue>();

            // Build slot array: include all required slots and present optional slots.
            // Absent optional slots (sentinel Span == SourceSpan.Missing) are omitted.
            // Downstream consumers find slots by Kind, not by catalog index position.
            foreach (var slot in meta.Slots)
            {
                var value = ParseSlotValue(slot, meta);
                if (slot.IsRequired || value.Span != SourceSpan.Missing)
                    slots.Add(value);
            }

            var endSpan = _position > 0 && !IsTrivia(_tokens[_position - 1].Kind)
                ? _tokens[_position - 1].Span
                : startSpan;
            var span = SourceSpan.Covering(startSpan, endSpan);

            _constructs.Add(new ParsedConstruct(meta, slots.ToImmutableArray(), span));
        }

        // ── Slot sub-parsers ────────────────────────────────────────────────────

        private SlotValue ParseSlotValue(ConstructSlot slot, ConstructMeta constructMeta)
        {
            return slot.Kind switch
            {
                ConstructSlotKind.IdentifierList    => ParseIdentifierList(slot),
                ConstructSlotKind.TypeExpression    => ParseTypeExpression(slot),
                ConstructSlotKind.ModifierList      => ParseModifierList(slot, constructMeta),
                ConstructSlotKind.StateEntryList    => ParseStateEntryList(slot),
                ConstructSlotKind.ArgumentList      => ParseArgumentList(slot),
                ConstructSlotKind.InitialMarker     => ParseInitialMarker(slot),
                ConstructSlotKind.BecauseClause     => ParseBecauseClause(slot),
                ConstructSlotKind.RuleExpression    => ParseExpressionPlaceholder(slot),
                ConstructSlotKind.GuardClause       => ParseGuardClausePlaceholder(slot),
                ConstructSlotKind.ComputeExpression => ParseComputeExpressionPlaceholder(slot),
                ConstructSlotKind.EnsureClause      => ParseEnsureClausePlaceholder(slot),
                ConstructSlotKind.ActionChain       => ParseActionChainPlaceholder(slot),
                ConstructSlotKind.Outcome           => ParseOutcomePlaceholder(slot),
                ConstructSlotKind.StateTarget       => ParseStateTarget(slot),
                ConstructSlotKind.EventTarget       => ParseEventTarget(slot),
                ConstructSlotKind.AccessModeKeyword => ParseAccessMode(slot),
                ConstructSlotKind.FieldTarget       => ParseFieldTarget(slot),
                _ => MakeSentinel(slot),
            };
        }

        private static SlotValue MakeSentinel(ConstructSlot slot) => slot.Kind switch
        {
            ConstructSlotKind.IdentifierList    => new IdentifierListSlot(ImmutableArray<string>.Empty, SourceSpan.Missing),
            ConstructSlotKind.TypeExpression    => new TypeExpressionSlot(Types.All.First(), SourceSpan.Missing),
            ConstructSlotKind.ModifierList      => new ModifierListSlot(ImmutableArray<ModifierKind>.Empty, SourceSpan.Missing),
            ConstructSlotKind.StateEntryList    => new StateEntryListSlot(ImmutableArray<(string, ImmutableArray<ModifierKind>)>.Empty, SourceSpan.Missing),
            ConstructSlotKind.ArgumentList      => new ArgumentListSlot(ImmutableArray<(string, TypeMeta)>.Empty, SourceSpan.Missing),
            ConstructSlotKind.InitialMarker     => new InitialMarkerSlot(false, SourceSpan.Missing),
            ConstructSlotKind.BecauseClause     => new BecauseClauseSlot("", SourceSpan.Missing),
            ConstructSlotKind.GuardClause       => new GuardClauseSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.RuleExpression    => new RuleExpressionSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ComputeExpression => new ComputeExpressionSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.EnsureClause      => new EnsureClauseSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ActionChain       => new ActionChainSlot(ImmutableArray<ActionKind>.Empty, SourceSpan.Missing),
            ConstructSlotKind.Outcome           => new OutcomeSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.StateTarget       => new StateTargetSlot(null, SourceSpan.Missing),
            ConstructSlotKind.EventTarget       => new EventTargetSlot(null, SourceSpan.Missing),
            ConstructSlotKind.AccessModeKeyword => new AccessModeSlot(TokenKind.Readonly, SourceSpan.Missing),
            ConstructSlotKind.FieldTarget       => new FieldTargetSlot(null, SourceSpan.Missing),
            _ => throw new InvalidOperationException($"No sentinel for {slot.Kind}"),
        };

        // ── IdentifierList: comma-separated identifiers ─────────────────────────

        private SlotValue ParseIdentifierList(ConstructSlot slot)
        {
            if (!slot.IsRequired && Peek().Kind != TokenKind.Identifier)
                return MakeSentinel(slot);
            var startSpan = Peek().Span;
            var names = new List<string>();

            if (Peek().Kind == TokenKind.Identifier)
            {
                var tok = Advance();
                names.Add(tok.Text);

                while (Peek().Kind == TokenKind.Comma)
                {
                    Advance(); // consume comma
                    if (Peek().Kind == TokenKind.Identifier)
                    {
                        var next = Advance();
                        names.Add(next.Text);
                    }
                }
            }
            else if (slot.IsRequired)
            {
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, Peek().Span, "identifier", Peek().Text));
            }

            var endSpan = names.Count > 0
                ? _tokens[_position - 1].Span
                : startSpan;

            return new IdentifierListSlot(names.ToImmutableArray(),
                SourceSpan.Covering(startSpan, endSpan));
        }

        // ── TypeExpression: "as TypeKeyword" ────────────────────────────────────

        private SlotValue ParseTypeExpression(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.As)
            {
                if (!slot.IsRequired)
                    return MakeSentinel(slot);
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, Peek().Span, "as", Peek().Text));
                return new TypeExpressionSlot(Types.All.First(), SourceSpan.Missing);
            }

            var asToken = Advance(); // consume 'as'
            var typeToken = Peek();

            // Handle dual-use 'set' token: in type context, treat Set as SetType
            var lookupKind = typeToken.Kind == TokenKind.Set ? TokenKind.SetType : typeToken.Kind;

            if (Types.ByToken.TryGetValue(lookupKind, out var typeMeta))
            {
                Advance(); // consume type token
                var span = SourceSpan.Covering(asToken.Span, typeToken.Span);
                return new TypeExpressionSlot(typeMeta, span);
            }

            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, typeToken.Span, "type keyword", typeToken.Text));
            return new TypeExpressionSlot(Types.All.First(), asToken.Span);
        }

        // ── ModifierList: field modifiers from catalog ──────────────────────────

        private SlotValue ParseModifierList(ConstructSlot slot, ConstructMeta constructMeta)
        {
            var modifiers = new List<ModifierKind>();
            var startSpan = Peek().Span;
            var lastSpan = startSpan;

            while (FieldModifierTokens.Contains(Peek().Kind))
            {
                var modToken = Peek();
                if (Modifiers.ByFieldToken.TryGetValue(modToken.Kind, out var modMeta))
                {
                    Advance();
                    modifiers.Add(modMeta.Kind);
                    lastSpan = modToken.Span;

                    // Valued modifiers consume the next token as their value
                    if (modMeta.HasValue && Peek().Kind is TokenKind.NumberLiteral
                        or TokenKind.StringLiteral or TokenKind.True or TokenKind.False
                        or TokenKind.Identifier)
                    {
                        lastSpan = Advance().Span;
                    }
                }
                else
                {
                    break;
                }
            }

            if (modifiers.Count == 0)
                return MakeSentinel(slot);

            return new ModifierListSlot(modifiers.ToImmutableArray(),
                SourceSpan.Covering(startSpan, lastSpan));
        }

        // ── StateEntryList: "Name [Modifiers], Name [Modifiers]" ────────────────

        private SlotValue ParseStateEntryList(ConstructSlot slot)
        {
            var entries = new List<(string Name, ImmutableArray<ModifierKind> Modifiers)>();
            var startSpan = Peek().Span;

            while (Peek().Kind == TokenKind.Identifier)
            {
                var nameToken = Advance();
                var modifiers = new List<ModifierKind>();

                // State modifiers follow the name
                while (StateModifierTokens.Contains(Peek().Kind))
                {
                    var modToken = Advance();
                    // Map token to ModifierKind via catalog
                    var modKind = MapStateModifierToken(modToken.Kind);
                    if (modKind.HasValue)
                        modifiers.Add(modKind.Value);
                }

                entries.Add((nameToken.Text, modifiers.ToImmutableArray()));

                // Comma separates entries
                if (Peek().Kind == TokenKind.Comma)
                    Advance();
                else
                    break;
            }

            if (entries.Count == 0 && slot.IsRequired)
            {
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, Peek().Span, "state name", Peek().Text));
                return new StateEntryListSlot(
                    ImmutableArray<(string, ImmutableArray<ModifierKind>)>.Empty, startSpan);
            }

            var endSpan = _position > 0 ? _tokens[_position - 1].Span : startSpan;
            return new StateEntryListSlot(entries.ToImmutableArray(),
                SourceSpan.Covering(startSpan, endSpan));
        }

        private static ModifierKind? MapStateModifierToken(TokenKind token)
        {
            // Derive from catalog: find the StateModifierMeta whose token matches
            foreach (var mod in Modifiers.All)
            {
                if (mod is StateModifierMeta smm && smm.Token.Kind == token)
                    return smm.Kind;
            }
            return null;
        }

        // ── ArgumentList: "(name as type, ...)" ─────────────────────────────────

        private SlotValue ParseArgumentList(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.LeftParen)
                return MakeSentinel(slot);

            var startToken = Advance(); // consume '('
            var args = new List<(string Name, TypeMeta Type)>();

            while (Peek().Kind != TokenKind.RightParen && !IsAtEnd
                   && !ConstructsCatalog.LeadingTokens.Contains(Peek().Kind))
            {
                if (Peek().Kind == TokenKind.Identifier)
                {
                    var nameToken = Advance();
                    Expect(TokenKind.As);

                    var typeToken = Peek();
                    var lookupKind = typeToken.Kind == TokenKind.Set ? TokenKind.SetType : typeToken.Kind;

                    if (Types.ByToken.TryGetValue(lookupKind, out var typeMeta))
                    {
                        Advance();
                        args.Add((nameToken.Text, typeMeta));
                    }
                    else
                    {
                        _diagnostics.Add(DiagnosticsCatalog.Create(
                            DiagnosticCode.ExpectedToken, typeToken.Span, "type keyword", typeToken.Text));
                        break;
                    }
                }

                if (Peek().Kind == TokenKind.Comma)
                    Advance();
                else
                    break;
            }

            var endToken = Peek();
            if (endToken.Kind == TokenKind.RightParen)
                Advance();

            var span = SourceSpan.Covering(startToken.Span,
                endToken.Kind == TokenKind.RightParen ? endToken.Span : startToken.Span);

            return new ArgumentListSlot(args.ToImmutableArray(), span);
        }

        // ── InitialMarker: optional "initial" keyword ───────────────────────────

        private SlotValue ParseInitialMarker(ConstructSlot slot)
        {
            if (Peek().Kind == TokenKind.Initial)
            {
                var tok = Advance();
                return new InitialMarkerSlot(true, tok.Span);
            }
            return slot.IsRequired ? new InitialMarkerSlot(false, Peek().Span) : MakeSentinel(slot);
        }

        // ── BecauseClause: "because \"message\"" ───────────────────────────────

        private SlotValue ParseBecauseClause(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.Because)
            {
                if (slot.IsRequired)
                {
                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.ExpectedToken, Peek().Span, "because", Peek().Text));
                    return new BecauseClauseSlot("", Peek().Span);
                }
                return MakeSentinel(slot);
            }

            var becauseToken = Advance(); // consume 'because'
            if (Peek().Kind == TokenKind.StringLiteral)
            {
                var strToken = Advance();
                // Lexer emits string content without surrounding quotes
                return new BecauseClauseSlot(strToken.Text,
                    SourceSpan.Covering(becauseToken.Span, strToken.Span));
            }

            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "string literal", Peek().Text));
            return new BecauseClauseSlot("", becauseToken.Span);
        }

        // ── StateTarget: state name identifier ──────────────────────────────────

        private SlotValue ParseStateTarget(ConstructSlot slot)
        {
            if (Peek().Kind == TokenKind.Identifier || Peek().Kind == TokenKind.Any)
            {
                var tok = Advance();
                return new StateTargetSlot(tok.Text, tok.Span);
            }
            if (!slot.IsRequired)
                return MakeSentinel(slot);
            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "state name", Peek().Text));
            return new StateTargetSlot(null, Peek().Span);
        }

        // ── EventTarget: event name identifier ──────────────────────────────────

        private SlotValue ParseEventTarget(ConstructSlot slot)
        {
            if (Peek().Kind == TokenKind.Identifier)
            {
                var tok = Advance();
                return new EventTargetSlot(tok.Text, tok.Span);
            }
            if (!slot.IsRequired)
                return MakeSentinel(slot);
            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "event name", Peek().Text));
            return new EventTargetSlot(null, Peek().Span);
        }

        // ── AccessMode: readonly | editable ─────────────────────────────────────

        private SlotValue ParseAccessMode(ConstructSlot slot)
        {
            if (Peek().Kind == TokenKind.Readonly || Peek().Kind == TokenKind.Editable)
            {
                var tok = Advance();
                return new AccessModeSlot(tok.Kind, tok.Span);
            }
            if (!slot.IsRequired)
                return MakeSentinel(slot);
            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "readonly or editable", Peek().Text));
            return new AccessModeSlot(TokenKind.Readonly, Peek().Span);
        }

        // ── FieldTarget: field name or "all" ────────────────────────────────────

        private SlotValue ParseFieldTarget(ConstructSlot slot)
        {
            if (Peek().Kind == TokenKind.Identifier || Peek().Kind == TokenKind.All)
            {
                var tok = Advance();
                return new FieldTargetSlot(tok.Text, tok.Span);
            }
            if (!slot.IsRequired)
                return MakeSentinel(slot);
            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "field name", Peek().Text));
            return new FieldTargetSlot(null, Peek().Span);
        }

        // ── Expression placeholders (Slice 1) ───────────────────────────────────
        // These advance past tokens until a construct boundary. Expression parsing
        // (Pratt parser) is deferred to Slice 3.

        private SlotValue ParseExpressionPlaceholder(ConstructSlot slot)
        {
            // RuleExpression: everything after 'rule' until 'when', 'because', or boundary
            var startSpan = Peek().Span;
            if (IsAtConstructBoundary() && !slot.IsRequired)
                return MakeSentinel(slot);
            var lastSpan = startSpan;
            while (!IsAtEnd && !IsExpressionTerminator())
            {
                lastSpan = Advance().Span;
            }

            if (lastSpan.Offset == startSpan.Offset && lastSpan.Length == startSpan.Length
                && !slot.IsRequired)
                return MakeSentinel(slot);
            var span = SourceSpan.Covering(startSpan, lastSpan);
            var placeholder = new LiteralExpression(TokenKind.True, "true", span);
            return new RuleExpressionSlot(placeholder, span);
        }

        private SlotValue ParseGuardClausePlaceholder(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.When)
                return MakeSentinel(slot);

            var whenToken = Advance(); // consume 'when'
            var startSpan = Peek().Span;
            var lastSpan = whenToken.Span;

            while (!IsAtEnd && !IsExpressionTerminatorAfterGuard())
            {
                lastSpan = Advance().Span;
            }

            var span = SourceSpan.Covering(whenToken.Span, lastSpan);
            var placeholder = new LiteralExpression(TokenKind.True, "true", span);
            return new GuardClauseSlot(placeholder, span);
        }

        private SlotValue ParseComputeExpressionPlaceholder(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.Arrow)
                return MakeSentinel(slot);

            var arrowToken = Advance(); // consume '->'
            var startSpan = Peek().Span;
            var lastSpan = arrowToken.Span;

            while (!IsAtEnd && !IsAtConstructBoundary())
            {
                lastSpan = Advance().Span;
            }

            var span = SourceSpan.Covering(arrowToken.Span, lastSpan);
            var placeholder = new LiteralExpression(TokenKind.True, "true", span);
            return new ComputeExpressionSlot(placeholder, span);
        }

        private SlotValue ParseEnsureClausePlaceholder(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.Ensure)
                return MakeSentinel(slot);

            var ensureToken = Advance();
            var startSpan = Peek().Span;
            var lastSpan = ensureToken.Span;

            while (!IsAtEnd && Peek().Kind != TokenKind.Because && !IsAtConstructBoundary())
            {
                lastSpan = Advance().Span;
            }

            var span = SourceSpan.Covering(ensureToken.Span, lastSpan);
            var placeholder = new LiteralExpression(TokenKind.True, "true", span);
            return new EnsureClauseSlot(placeholder, span);
        }

        private SlotValue ParseActionChainPlaceholder(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.Arrow)
                return MakeSentinel(slot);

            var actions = new List<ActionKind>();
            var startSpan = Peek().Span;
            var lastSpan = startSpan;

            while (Peek().Kind == TokenKind.Arrow && !IsAtEnd)
            {
                Advance(); // consume '->'
                var actionToken = Peek();
                if (Actions.ByTokenKind.TryGetValue(actionToken.Kind, out var actionMeta))
                {
                    actions.Add(actionMeta.Kind);
                    lastSpan = Advance().Span;
                    // Skip action operands until next arrow or boundary
                    while (!IsAtEnd && Peek().Kind != TokenKind.Arrow && !IsAtConstructBoundary())
                    {
                        lastSpan = Advance().Span;
                    }
                }
                else
                {
                    break;
                }
            }

            if (actions.Count == 0 && !slot.IsRequired)
                return MakeSentinel(slot);
            return new ActionChainSlot(actions.ToImmutableArray(),
                SourceSpan.Covering(startSpan, lastSpan));
        }

        private SlotValue ParseOutcomePlaceholder(ConstructSlot slot)
        {
            // Outcomes follow action chains: "-> transition State", "-> no transition", "-> reject ..."
            if (Peek().Kind != TokenKind.Arrow)
                return MakeSentinel(slot);

            var startSpan = Peek().Span;
            var lastSpan = startSpan;

            while (!IsAtEnd && !IsAtConstructBoundary())
            {
                lastSpan = Advance().Span;
            }

            var span = SourceSpan.Covering(startSpan, lastSpan);
            var placeholder = new LiteralExpression(TokenKind.True, "true", span);
            return new OutcomeSlot(placeholder, span);
        }

        // ── Boundary detection ──────────────────────────────────────────────────

        private bool IsAtConstructBoundary() =>
            ConstructsCatalog.LeadingTokens.Contains(Peek().Kind) || IsAtEnd;

        private bool IsExpressionTerminator() =>
            Peek().Kind == TokenKind.When
            || Peek().Kind == TokenKind.Because
            || IsAtConstructBoundary();

        private bool IsExpressionTerminatorAfterGuard() =>
            Peek().Kind == TokenKind.Because
            || Peek().Kind == TokenKind.Arrow
            || IsAtConstructBoundary();
    }
}

