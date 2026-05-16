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
public static partial class Parser
{
    // ── Catalog-derived vocabulary sets (computed once at startup) ────────────────

    private static readonly FrozenSet<TokenKind> StateModifierTokens =
        Modifiers.ByStateToken.Keys.ToFrozenSet();

    private static readonly FrozenSet<TokenKind> ValueModifierTokens =
        Modifiers.ByValueToken.Keys.ToFrozenSet();

    private static readonly FrozenSet<TokenKind> ExpressionStartTokens =
        ExpressionForms.All
            .Where(form => !form.IsLeftDenotation)
            .SelectMany(form => form.LeadTokens)
            .ToFrozenSet();

    public static FrozenSet<TokenKind> KeywordsValidAsMemberName { get; } =
        Tokens.KeywordsValidAsMemberName;

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

    [global::Precept.HandlesCatalogExhaustively(typeof(ExpressionFormKind))]
    [global::Precept.HandlesCatalogExhaustively(typeof(OutcomeArgumentKind))]
    [global::Precept.HandlesCatalogExhaustively(typeof(ActionSyntaxShape))]
    private sealed partial class ParserState
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

        private SourceSpan LastSignificantConsumedSpan(SourceSpan fallback)
        {
            for (var index = _position - 1; index >= 0; index--)
            {
                if (!IsTrivia(_tokens[index].Kind))
                    return _tokens[index].Span;
            }

            return fallback;
        }

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
                    if (candidates.Length == 1)
                    {
                        // Unambiguous — single candidate
                        var meta = ConstructsCatalog.GetMeta(candidates[0].Kind);
                        if (meta.RoutingFamily == RoutingFamily.Direct)
                            ParseConstruct(meta);
                        else
                            ParseScopedConstruct(meta);
                    }
                    else
                    {
                        // B3 disambiguation: state-scoped constructs may carry
                        // comma-delimited state lists before the disambiguation token.
                        // For constructs with optional pre-verb guards, the disambiguation
                        // token can also appear after "when <expr>".
                        var disambToken = ResolveDisambiguationToken(candidates);
                        ConstructKind? resolved = null;

                        foreach (var (kind, entry) in candidates)
                        {
                            if (entry.DisambiguationTokens is { } tokens
                                && tokens.Contains(disambToken.Kind))
                            {
                                resolved = kind;
                                break;
                            }
                        }

                        if (resolved == null)
                        {
                            // Disambiguation failure — emit diagnostic, select first candidate
                            _diagnostics.Add(DiagnosticsCatalog.Create(
                                DiagnosticCode.ExpectedToken, disambToken.Span,
                                "disambiguation keyword", disambToken.Text));
                            resolved = candidates[0].Kind;
                        }

                        // Secondary disambiguation: detect reject-variant forms
                        var finalKind = resolved.Value is ConstructKind.EventRow or ConstructKind.TransitionRow
                            ? ResolveRejectVariant(resolved.Value)
                            : resolved.Value;

                        var selectedMeta = ConstructsCatalog.GetMeta(finalKind);
                        if (selectedMeta.RoutingFamily == RoutingFamily.Direct)
                            ParseConstruct(selectedMeta);
                        else
                            ParseScopedConstruct(selectedMeta);
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

        private Token ResolveDisambiguationToken(
            ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)> candidates)
        {
            var offset = GetDisambiguationTokenOffset(candidates);
            var disambToken = Peek(offset);
            if (disambToken.Kind != TokenKind.When)
                return disambToken;

            var disambCandidates = candidates
                .SelectMany(candidate => candidate.Entry.DisambiguationTokens ?? [])
                .Distinct()
                .ToArray();
            if (disambCandidates.Length == 0)
                return disambToken;

            offset++;
            while (true)
            {
                var token = Peek(offset);
                if (token.Kind == TokenKind.EndOfSource)
                    return disambToken;
                if (disambCandidates.Contains(token.Kind))
                    return token;
                if (ConstructsCatalog.LeadingTokens.Contains(token.Kind) && token.Kind != TokenKind.When)
                    return disambToken;
                offset++;
            }
        }

        private int GetDisambiguationTokenOffset(
            ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)> candidates)
        {
            if (candidates.IsDefaultOrEmpty)
                return 2;

            var routingFamily = ConstructsCatalog.GetMeta(candidates[0].Kind).RoutingFamily;
            if (routingFamily != RoutingFamily.StateScoped)
                return 2;

            var disambTokens = candidates
                .SelectMany(candidate => candidate.Entry.DisambiguationTokens ?? [])
                .ToFrozenSet();
            var offset = 1;
            var token = Peek(offset);
            var tokenMeta = Tokens.GetMeta(token.Kind);

            if (tokenMeta.IsStateWildcard)
                return offset + 1;

            if (token.Kind != TokenKind.Identifier)
                return disambTokens.Contains(token.Kind) ? offset : offset + 1;

            offset++;
            while (Peek(offset).Kind == TokenKind.Comma)
            {
                offset++;
                if (Peek(offset).Kind != TokenKind.Identifier)
                    return offset;
                offset++;
            }

            return offset;
        }

        /// <summary>
        /// After primary disambiguation resolves to ConstructionRow or TransitionRow,
        /// performs secondary lookahead to detect reject-variant forms by finding
        /// the first Arrow token and checking if the next token is Reject.
        /// </summary>
        private ConstructKind ResolveRejectVariant(ConstructKind baseKind)
        {
            var offset = 1; // start past leading token
            while (true)
            {
                var token = Peek(offset);
                if (token.Kind == TokenKind.EndOfSource)
                    return baseKind;
                if (token.Kind == TokenKind.Arrow)
                {
                    var afterArrow = Peek(offset + 1);
                    if (afterArrow.Kind == TokenKind.Reject)
                    {
                        return baseKind switch
                        {
                            ConstructKind.EventRow => ConstructKind.ConstructionRowReject,
                            ConstructKind.TransitionRow => ConstructKind.TransitionRowReject,
                            _ => baseKind,
                        };
                    }
                    // Arrow found but not followed by reject — base kind stands
                    return baseKind;
                }
                // Stop at construct boundaries (but allow 'on' which appears mid-TransitionRow)
                if (offset > 2
                    && ConstructsCatalog.LeadingTokens.Contains(token.Kind)
                    && token.Kind != TokenKind.On
                    && token.Kind != TokenKind.When)
                {
                    return baseKind;
                }
                offset++;
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

            var endSpan = LastSignificantConsumedSpan(startSpan);
            var span = SourceSpan.Covering(startSpan, endSpan);

            _constructs.Add(new ParsedConstruct(meta, slots.ToImmutableArray(), span, startToken.Kind));
        }

        // ── Scoped construct parsing ────────────────────────────────────────────
        // Protocol: consume leading keyword, iterate all slots, consuming the
        // disambiguation keyword at the natural boundary between scope and body.

        private void ParseScopedConstruct(ConstructMeta meta)
        {
            var startToken = Advance(); // consume leading keyword
            var startSpan = startToken.Span;
            var slots = new List<SlotValue>();
            var disambTokens = meta.Entries
                .SelectMany(entry => entry.DisambiguationTokens ?? [])
                .ToFrozenSet();

            var disambConsumed = false;

            for (int i = 0; i < meta.Slots.Count; i++)
            {
                // ── Guard gate: TransitionRow pre-event guard rejection ──────────
                // If a 'when' appears before the disambiguation 'on' token in a
                // TransitionRow, it's a misplaced guard. Emit PRE0015 and skip it.
                // TODO(allow-list): remove PRE0015 from allow-list after Slice 0 ships
                if (meta.Kind == ConstructKind.TransitionRow
                    && i > 0 && !disambConsumed
                    && Peek().Kind == TokenKind.When)
                {
                    var whenSpan = Advance().Span; // consume 'when'
                    SkipGuardExpression(disambTokens);
                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.PreEventGuardNotAllowed, whenSpan));
                    // Fall through: disamb check below will now see 'on' (or error)
                }

                if (i > 0 && !disambConsumed && disambTokens.Contains(Peek().Kind))
                {
                    if (Peek().Kind != TokenKind.Arrow)
                        Advance(); // consume disambiguation keyword — maps to no slot
                    disambConsumed = true;
                }

                var slot = meta.Slots[i];
                var value = ParseSlotValue(slot, meta);
                if (slot.IsRequired || value.Span != SourceSpan.Missing)
                    slots.Add(value);
            }

            // ── Guard gates: post-slot rejection for constructs that forbid guards ──
            // TODO(allow-list): remove PRE0013 from allow-list after Slice 0 ships
            if (Peek().Kind == TokenKind.When)
            {
                if (meta.Kind == ConstructKind.OmitDeclaration)
                {
                    var whenSpan = Advance().Span;
                    SkipToConstructBoundary();
                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.OmitDoesNotSupportGuard, whenSpan));
                }
            }

            var endSpan = LastSignificantConsumedSpan(startSpan);
            var span = SourceSpan.Covering(startSpan, endSpan);

            _constructs.Add(new ParsedConstruct(meta, slots.ToImmutableArray(), span, startToken.Kind));
        }

        /// <summary>
        /// Skips tokens that form a guard expression until a disambiguation token,
        /// construct boundary, or end of source is reached.
        /// </summary>
        private void SkipGuardExpression(FrozenSet<TokenKind> stopTokens)
        {
            while (!IsAtEnd
                   && !stopTokens.Contains(Peek().Kind)
                   && !ConstructsCatalog.LeadingTokens.Contains(Peek().Kind))
            {
                Advance();
            }
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
                ConstructSlotKind.RuleExpression    => ParseRuleExpression(slot),
                ConstructSlotKind.GuardClause       => ParseGuardClause(slot),
                ConstructSlotKind.ComputeExpression => ParseComputeExpression(slot),
                ConstructSlotKind.EnsureClause      => ParseEnsureClause(slot),
                ConstructSlotKind.ActionChain       => ParseActionChain(slot),
                ConstructSlotKind.Outcome           => ParseOutcome(slot),
                ConstructSlotKind.RejectClause      => ParseRejectClause(slot),
                ConstructSlotKind.SuccessOutcome    => ParseSuccessOutcome(slot),
                ConstructSlotKind.StateTarget       => ParseStateTarget(slot),
                ConstructSlotKind.EventTarget       => ParseEventTarget(slot),
                ConstructSlotKind.AccessModeKeyword => ParseAccessMode(slot),
                ConstructSlotKind.FieldTarget       => ParseFieldTarget(slot),
                _ => MakeSentinel(slot),
            };
        }

        private static SlotValue MakeSentinel(ConstructSlot slot) => slot.Kind switch
        {
            ConstructSlotKind.IdentifierList    => new IdentifierListSlot(ImmutableArray<string>.Empty, ImmutableArray<SourceSpan>.Empty, SourceSpan.Missing),
            ConstructSlotKind.TypeExpression    => new TypeExpressionSlot(new MissingTypeReference(SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ModifierList      => new ModifierListSlot(ImmutableArray<ParsedModifier>.Empty, SourceSpan.Missing),
            ConstructSlotKind.StateEntryList    => new StateEntryListSlot(ImmutableArray<StateEntrySyntax>.Empty, SourceSpan.Missing),
            ConstructSlotKind.ArgumentList      => new ArgumentListSlot(ImmutableArray<ArgumentSyntax>.Empty, SourceSpan.Missing),
            ConstructSlotKind.InitialMarker     => new InitialMarkerSlot(false, SourceSpan.Missing),
            ConstructSlotKind.BecauseClause     => new BecauseClauseSlot("", SourceSpan.Missing),
            ConstructSlotKind.GuardClause       => new GuardClauseSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.RuleExpression    => new RuleExpressionSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ComputeExpression => new ComputeExpressionSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.EnsureClause      => new EnsureClauseSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ActionChain       => new ActionChainSlot(ImmutableArray<ParsedAction>.Empty, SourceSpan.Missing),
            ConstructSlotKind.Outcome           => new OutcomeSlot(new MalformedOutcome(SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.RejectClause      => new RejectClauseSlot("", SourceSpan.Missing),
            ConstructSlotKind.SuccessOutcome    => new SuccessOutcomeSlot(new MalformedOutcome(SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.StateTarget       => new StateTargetSlot(ImmutableArray<string>.Empty, ImmutableArray<SourceSpan>.Empty, SourceSpan.Missing),
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
            var nameSpans = new List<SourceSpan>();

            if (Peek().Kind == TokenKind.Identifier)
            {
                var tok = Advance();
                names.Add(tok.Text);
                nameSpans.Add(tok.Span);

                while (Peek().Kind == TokenKind.Comma)
                {
                    Advance(); // consume comma
                    if (Peek().Kind == TokenKind.Identifier)
                    {
                        var next = Advance();
                        names.Add(next.Text);
                        nameSpans.Add(next.Span);
                    }
                }
            }
            else if (slot.IsRequired)
            {
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, Peek().Span, "identifier", Peek().Text));
            }

            var endSpan = names.Count > 0
                ? LastSignificantConsumedSpan(startSpan)
                : startSpan;

            return new IdentifierListSlot(
                names.ToImmutableArray(),
                nameSpans.ToImmutableArray(),
                SourceSpan.Covering(startSpan, endSpan));
        }

        // ── ModifierList: value modifiers from catalog ──────────────────────────

        private SlotValue ParseModifierList(ConstructSlot slot, ConstructMeta constructMeta)
        {
            var modifiers = new List<ParsedModifier>();
            var startSpan = Peek().Span;
            var lastSpan = startSpan;

            while (ValueModifierTokens.Contains(Peek().Kind))
            {
                var modToken = Peek();
                if (Modifiers.ByValueToken.TryGetValue(modToken.Kind, out var modMeta))
                {
                    Advance();
                    lastSpan = modToken.Span;
                    ParsedExpression? valueExpr = null;

                    // Valued modifiers parse an expression for their value
                    if (modMeta.HasValue)
                    {
                        if (ExpressionStartTokens.Contains(Peek().Kind))
                        {
                            valueExpr = ParseExpression(0, () =>
                                ValueModifierTokens.Contains(Peek().Kind)
                                || IsAtConstructBoundary());
                            lastSpan = valueExpr.Span;
                        }
                    }

                    modifiers.Add(new ParsedModifier(
                        modMeta.Kind,
                        valueExpr,
                        valueExpr is null ? modToken.Span : SourceSpan.Covering(modToken.Span, valueExpr.Span)));
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
            var entries = new List<StateEntrySyntax>();
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

                entries.Add(new StateEntrySyntax(nameToken.Text, modifiers.ToImmutableArray(), nameToken.Span));

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
                    ImmutableArray<StateEntrySyntax>.Empty, startSpan);
            }

            var endSpan = LastSignificantConsumedSpan(startSpan);
            return new StateEntryListSlot(entries.ToImmutableArray(),
                SourceSpan.Covering(startSpan, endSpan));
        }

        private static ModifierKind? MapStateModifierToken(TokenKind token)
        {
            // O(1) lookup via catalog index
            return Modifiers.ByStateToken.TryGetValue(token, out var meta)
                ? meta.Kind
                : null;
        }

        // ── ArgumentList: "(name as type, ...)" ─────────────────────────────────

        private SlotValue ParseArgumentList(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.LeftParen)
                return MakeSentinel(slot);

            var startToken = Advance(); // consume '('
            var args = new List<ArgumentSyntax>();
            const ValueModifierDeclarationSite argSite = ValueModifierDeclarationSite.EventArgDeclaration;

            while (Peek().Kind != TokenKind.RightParen && !IsAtEnd
                   && !ConstructsCatalog.LeadingTokens.Contains(Peek().Kind))
            {
                if (Peek().Kind == TokenKind.Identifier)
                {
                    var nameToken = Advance();
                    var asToken = Expect(TokenKind.As);
                    var parsedType = ParseTypeReference(asToken.Span);

                    var modifiers = ImmutableArray.CreateBuilder<ModifierKind>();
                    var parsedModifiers = ImmutableArray.CreateBuilder<ParsedModifier>();
                    while (Modifiers.ByValueToken.TryGetValue(Peek().Kind, out var modMeta)
                           && modMeta.ApplicableDeclarationSites.HasFlag(argSite))
                    {
                        var modToken = Peek();
                        modifiers.Add(modMeta.Kind);
                        Advance();

                        if (!modMeta.HasValue)
                        {
                            parsedModifiers.Add(new ParsedModifier(modMeta.Kind, null, modToken.Span));
                            continue;
                        }

                        if (!ExpressionStartTokens.Contains(Peek().Kind))
                        {
                            _diagnostics.Add(DiagnosticsCatalog.Create(
                                DiagnosticCode.ExpectedToken, Peek().Span, "expression", Peek().Text));
                            parsedModifiers.Add(new ParsedModifier(modMeta.Kind, null, modToken.Span));
                            continue;
                        }

                        var valueExpr = ParseExpression(0, () =>
                            Peek().Kind is TokenKind.Comma or TokenKind.RightParen
                            || IsAtConstructBoundary()
                            || (Modifiers.ByValueToken.TryGetValue(Peek().Kind, out var nextMod)
                                && nextMod.ApplicableDeclarationSites.HasFlag(argSite)));
                        parsedModifiers.Add(new ParsedModifier(
                            modMeta.Kind, valueExpr,
                            SourceSpan.Covering(modToken.Span, valueExpr.Span)));
                    }

                    args.Add(new ArgumentSyntax(
                        nameToken.Text,
                        parsedType,
                        modifiers.ToImmutable(),
                        nameToken.Span,
                        parsedModifiers.ToImmutable()));
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
            if (TryParseStringExpression(out var message, out var messageSpan))
            {
                return new BecauseClauseSlot(message,
                    SourceSpan.Covering(becauseToken.Span, messageSpan));
            }

            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "string expression", Peek().Text));
            return new BecauseClauseSlot("", becauseToken.Span);
        }

        // ── StateTarget: state name list or wildcard ────────────────────────────

        private SlotValue ParseStateTarget(ConstructSlot slot)
        {
            var current = Peek();
            var meta = Tokens.GetMeta(current.Kind);
            if (meta.IsStateWildcard)
            {
                var tok = Advance();
                return new StateTargetSlot(
                    ImmutableArray.Create(tok.Text),
                    ImmutableArray.Create(tok.Span),
                    tok.Span);
            }

            if (current.Kind == TokenKind.Identifier)
            {
                var names = new List<string>();
                var nameSpans = new List<SourceSpan>();
                var first = Advance();
                var lastSpan = first.Span;

                names.Add(first.Text);
                nameSpans.Add(first.Span);

                while (Peek().Kind == TokenKind.Comma)
                {
                    Advance(); // consume comma
                    if (Peek().Kind == TokenKind.Identifier)
                    {
                        var next = Advance();
                        names.Add(next.Text);
                        nameSpans.Add(next.Span);
                        lastSpan = next.Span;
                        continue;
                    }

                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.ExpectedToken, Peek().Span, "identifier", Peek().Text));
                    break;
                }

                return new StateTargetSlot(
                    names.ToImmutableArray(),
                    nameSpans.ToImmutableArray(),
                    SourceSpan.Covering(first.Span, lastSpan));
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
                return new EventTargetSlot(tok.Text, tok.Span)
                {
                    NameSpan = tok.Span,
                };
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
            if (Tokens.AccessModeKeywords.Contains(Peek().Kind))
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
            var current = Peek();
            var meta = Tokens.GetMeta(current.Kind);
            if (meta.IsFieldBroadcast)
            {
                var tok = Advance();
                return new FieldTargetSlot(tok.Text, tok.Span)
                {
                    NameSpan = tok.Span,
                };
            }

            if (current.Kind == TokenKind.Identifier)
            {
                var first = Advance();
                var lastSpan = first.Span;
                var additionalBuilder = ImmutableArray.CreateBuilder<(string Name, SourceSpan Span)>();

                while (Peek().Kind == TokenKind.Comma)
                {
                    Advance(); // consume comma
                    if (Peek().Kind == TokenKind.Identifier)
                    {
                        var next = Advance();
                        additionalBuilder.Add((next.Text, next.Span));
                        lastSpan = next.Span;
                        continue;
                    }

                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.ExpectedToken, Peek().Span, "identifier", Peek().Text));
                    break;
                }

                return new FieldTargetSlot(first.Text, SourceSpan.Covering(first.Span, lastSpan))
                {
                    NameSpan = first.Span,
                    AdditionalFields = additionalBuilder.ToImmutable(),
                };
            }

            if (!slot.IsRequired)
                return MakeSentinel(slot);
            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "field name", Peek().Text));
            return new FieldTargetSlot(null, Peek().Span);
        }

        // ── Boundary detection ──────────────────────────────────────────────────

        private bool IsAtConstructBoundary() =>
            ConstructsCatalog.LeadingTokens.Contains(Peek().Kind) || IsAtEnd;
    }
}

