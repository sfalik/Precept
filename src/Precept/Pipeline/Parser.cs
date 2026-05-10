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
        Tokens.All
            .Where(meta => meta.IsValidAsMemberName)
            .Select(meta => meta.Kind)
            .ToFrozenSet();

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
                        // B3 disambiguation: peek(2) is the disambiguation token.
                        // For constructs with optional pre-verb guards, the disambiguation
                        // token can appear after "when <expr>".
                        var disambToken = ResolveDisambiguationToken(candidates);
                        ConstructKind? resolved = null;

                        foreach (var (kind, entry) in candidates)
                        {
                            if (entry.DisambiguationTokens is { } tokens
                                && tokens.Contains(disambToken))
                            {
                                resolved = kind;
                                break;
                            }
                        }

                        if (resolved == null)
                        {
                            // Disambiguation failure — emit diagnostic, select first candidate
                            _diagnostics.Add(DiagnosticsCatalog.Create(
                                DiagnosticCode.ExpectedToken, Peek(2).Span,
                                "disambiguation keyword", Peek(2).Text));
                            resolved = candidates[0].Kind;
                        }

                        var selectedMeta = ConstructsCatalog.GetMeta(resolved.Value);
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

        private TokenKind ResolveDisambiguationToken(
            ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)> candidates)
        {
            var disambToken = Peek(2).Kind;
            if (disambToken != TokenKind.When)
                return disambToken;

            var disambCandidates = candidates
                .SelectMany(candidate => candidate.Entry.DisambiguationTokens ?? [])
                .Distinct()
                .ToArray();
            if (disambCandidates.Length == 0)
                return disambToken;

            var offset = 3;
            while (true)
            {
                var token = Peek(offset).Kind;
                if (token == TokenKind.EndOfSource)
                    return disambToken;
                if (disambCandidates.Contains(token))
                    return token;
                if (ConstructsCatalog.LeadingTokens.Contains(token) && token != TokenKind.When)
                    return disambToken;
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

            var endSpan = _position > 0 && !IsTrivia(_tokens[_position - 1].Kind)
                ? _tokens[_position - 1].Span
                : startSpan;
            var span = SourceSpan.Covering(startSpan, endSpan);

            _constructs.Add(new ParsedConstruct(meta, slots.ToImmutableArray(), span, startToken.Kind));
        }

        // ── Scoped construct parsing ────────────────────────────────────────────
        // Protocol: consume leading keyword, parse anchor slot (Slots[0]),
        // consume disambiguation keyword (no slot), walk remaining Slots[1..].

        private void ParseScopedConstruct(ConstructMeta meta)
        {
            var startToken = Advance(); // consume leading keyword
            var startSpan = startToken.Span;
            var slots = new List<SlotValue>();

            // Slots[0] = anchor (StateTarget or EventTarget)
            if (meta.Slots.Count > 0)
            {
                var anchorValue = ParseSlotValue(meta.Slots[0], meta);
                if (meta.Slots[0].IsRequired || anchorValue.Span != SourceSpan.Missing)
                    slots.Add(anchorValue);
            }

            if (meta.SupportsPreVerbWhenGuard && Peek().Kind == TokenKind.When)
            {
                var guardSlot = ParseGuardClause(new ConstructSlot(
                    ConstructSlotKind.GuardClause,
                    IsRequired: false,
                    TerminationTokens: meta.Entries
                        .SelectMany(entry => entry.DisambiguationTokens ?? [])
                        .Distinct()
                        .ToArray()));

                if (guardSlot.Span != SourceSpan.Missing)
                    slots.Add(guardSlot);
            }

            // Consume disambiguation keyword (not a slot) — but only if it won't
            // be consumed by the next slot's sub-parser. Arrow serves as both
            // disambiguation token AND ActionChain entry trigger, so we leave it.
            var peek = Peek();
            var isDisambToken = false;
            foreach (var entry in meta.Entries)
            {
                if (entry.DisambiguationTokens is { } dTokens && dTokens.Contains(peek.Kind))
                {
                    isDisambToken = true;
                    break;
                }
            }
            if (isDisambToken && peek.Kind != TokenKind.Arrow)
                Advance(); // consume disambiguation keyword — maps to no slot

            // Walk remaining slots (Slots[1..])
            for (int i = 1; i < meta.Slots.Count; i++)
            {
                var slot = meta.Slots[i];
                var value = ParseSlotValue(slot, meta);
                if (slot.IsRequired || value.Span != SourceSpan.Missing)
                    slots.Add(value);
            }

            if (meta.SupportsPostActionEnsure)
            {
                var ensureSlot = ParseEnsureClause(new ConstructSlot(
                    ConstructSlotKind.EnsureClause,
                    IsRequired: false,
                    TerminationTokens: [TokenKind.Because]));
                if (ensureSlot.Span != SourceSpan.Missing)
                {
                    slots.Add(ensureSlot);

                    var becauseSlot = ParseBecauseClause(new ConstructSlot(
                        ConstructSlotKind.BecauseClause,
                        IsRequired: false));
                    if (becauseSlot.Span != SourceSpan.Missing)
                        slots.Add(becauseSlot);
                }
            }

            var endSpan = _position > 0 && !IsTrivia(_tokens[_position - 1].Kind)
                ? _tokens[_position - 1].Span
                : startSpan;
            var span = SourceSpan.Covering(startSpan, endSpan);

            _constructs.Add(new ParsedConstruct(meta, slots.ToImmutableArray(), span, startToken.Kind));
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
            ConstructSlotKind.TypeExpression    => new TypeExpressionSlot(new MissingTypeReference(SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ModifierList      => new ModifierListSlot(ImmutableArray<ParsedModifier>.Empty, SourceSpan.Missing),
            ConstructSlotKind.StateEntryList    => new StateEntryListSlot(ImmutableArray<(string, ImmutableArray<ModifierKind>)>.Empty, SourceSpan.Missing),
            ConstructSlotKind.ArgumentList      => new ArgumentListSlot(ImmutableArray<(string, ParsedTypeReference, ImmutableArray<ModifierKind>)>.Empty, SourceSpan.Missing),
            ConstructSlotKind.InitialMarker     => new InitialMarkerSlot(false, SourceSpan.Missing),
            ConstructSlotKind.BecauseClause     => new BecauseClauseSlot("", SourceSpan.Missing),
            ConstructSlotKind.GuardClause       => new GuardClauseSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.RuleExpression    => new RuleExpressionSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ComputeExpression => new ComputeExpressionSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.EnsureClause      => new EnsureClauseSlot(new LiteralExpression(TokenKind.True, "true", SourceSpan.Missing), SourceSpan.Missing),
            ConstructSlotKind.ActionChain       => new ActionChainSlot(ImmutableArray<ParsedAction>.Empty, SourceSpan.Missing),
            ConstructSlotKind.Outcome           => new OutcomeSlot(new MalformedOutcome(SourceSpan.Missing), SourceSpan.Missing),
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

        // ── TypeExpression: "as TypeKeyword [of InnerType] [Qualifiers]" ────────────

        private SlotValue ParseTypeExpression(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.As)
            {
                if (!slot.IsRequired)
                    return MakeSentinel(slot);
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, Peek().Span, "as", Peek().Text));
                return new TypeExpressionSlot(new MissingTypeReference(SourceSpan.Missing), SourceSpan.Missing);
            }

            var asToken = Advance(); // consume 'as'
            var typeRef = ParseTypeReference(asToken.Span);
            var span = SourceSpan.Covering(asToken.Span, typeRef.Span);
            return new TypeExpressionSlot(typeRef, span);
        }

        /// <summary>
        /// Parses a type reference after 'as' has been consumed.
        /// Handles: simple types, ~type (CI), collection types with inner types,
        /// choice types with domain, and keyed collections (log by, queue by, lookup).
        /// </summary>
        private ParsedTypeReference ParseTypeReference(SourceSpan startSpan)
        {
            var peekToken = Peek();

            // Handle CI type prefix: ~string
            if (peekToken.Kind == TokenKind.Tilde)
            {
                var tildeToken = Advance();
                var innerToken = Peek();
                var lookupKind = innerToken.Kind == TokenKind.Set ? TokenKind.SetType : innerToken.Kind;
                if (Types.ByToken.TryGetValue(lookupKind, out var innerType))
                {
                    Advance();
                    var span = SourceSpan.Covering(tildeToken.Span, innerToken.Span);
                    return new CITypeReference(innerType, span);
                }
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, innerToken.Span, "type keyword", innerToken.Text));
                return new MissingTypeReference(tildeToken.Span);
            }

            // Handle dual-use 'set' token: in type context, treat Set as SetType
            var lookupTokenKind = peekToken.Kind == TokenKind.Set ? TokenKind.SetType : peekToken.Kind;

            if (!Types.ByToken.TryGetValue(lookupTokenKind, out var typeMeta))
            {
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, peekToken.Span, "type keyword", peekToken.Text));
                return new MissingTypeReference(startSpan);
            }

            var typeToken = Advance();

            // Check if this is a choice type: choice of T(...)
            if (typeMeta.Kind == TypeKind.Choice)
            {
                return ParseChoiceType(typeMeta, typeToken.Span);
            }

            // Check if this is a collection type (needs "of" inner type)
            if (typeMeta.Category == TypeCategory.Collection)
            {
                return ParseCollectionType(typeMeta, typeToken.Span);
            }

            // Simple type — try qualifier extensions ('in', 'of', 'to')
            var simpleRef = new SimpleTypeReference(typeMeta, typeToken.Span);
            return TryParseQualifiers(simpleRef, typeMeta);
        }

        private ParsedTypeReference ParseChoiceType(TypeMeta choiceMeta, SourceSpan typeSpan)
        {
            TypeMeta? elementType = null;
            var domain = ImmutableArray.CreateBuilder<string>();
            var lastSpan = typeSpan;

            // choice of T(...) - look for 'of' and element type
            if (Peek().Kind == TokenKind.Of)
            {
                Advance(); // consume 'of'
                var elemToken = Peek();
                var elemLookup = elemToken.Kind == TokenKind.Set ? TokenKind.SetType : elemToken.Kind;
                if (Types.ByToken.TryGetValue(elemLookup, out var elemMeta))
                {
                    Advance();
                    elementType = elemMeta;
                    lastSpan = elemToken.Span;
                }
            }

            // Parse domain: (value, value, ...)
            if (Peek().Kind == TokenKind.LeftParen)
            {
                Advance(); // consume '('
                while (Peek().Kind != TokenKind.RightParen && !IsAtEnd && !IsAtConstructBoundary())
                {
                    // Accept sign prefix for numeric choices
                    var signPrefix = "";
                    if (Peek().Kind == TokenKind.Minus || Peek().Kind == TokenKind.Plus)
                    {
                        signPrefix = Peek().Text;
                        Advance();
                    }

                    var valueToken = Peek();
                    if (valueToken.Kind is TokenKind.StringLiteral or TokenKind.NumberLiteral
                        or TokenKind.True or TokenKind.False or TokenKind.Identifier)
                    {
                        domain.Add(signPrefix + valueToken.Text);
                        lastSpan = Advance().Span;
                    }
                    else
                    {
                        break;
                    }

                    if (Peek().Kind == TokenKind.Comma)
                        Advance();
                    else
                        break;
                }

                if (Peek().Kind == TokenKind.RightParen)
                    lastSpan = Advance().Span;
            }

            var span = SourceSpan.Covering(typeSpan, lastSpan);
            return new ChoiceTypeReference(choiceMeta, elementType, domain.ToImmutable(), span);
        }

        private ParsedTypeReference ParseCollectionType(TypeMeta collectionMeta, SourceSpan typeSpan)
        {
            var lastSpan = typeSpan;
            ParsedTypeReference? elementType = null;
            ParsedTypeReference? keyType = null;

            // Collections require "of" for element type
            if (Peek().Kind == TokenKind.Of)
            {
                Advance(); // consume 'of'
                elementType = ParseInnerTypeReference();
                lastSpan = elementType.Span;
            }

            // Keyed collections (log by, queue by, lookup ... to ...) have a key/ordering type
            // log of T by K, queue of T by K, lookup of K to V
            if (collectionMeta.Kind == TypeKind.Lookup && Peek().Kind == TokenKind.To)
            {
                Advance(); // consume 'to'
                keyType = elementType; // In lookup, the first type is the key
                elementType = ParseInnerTypeReference(); // second is value
                lastSpan = elementType.Span;
            }
            else if ((collectionMeta.Kind is TypeKind.LogBy or TypeKind.QueueBy) && Peek().Kind == TokenKind.By)
            {
                Advance(); // consume 'by'
                keyType = ParseInnerTypeReference();
                lastSpan = keyType.Span;
            }
            else if (Peek().Kind == TokenKind.By)
            {
                // Regular log or queue gets promoted to log-by or queue-by
                Advance(); // consume 'by'
                keyType = ParseInnerTypeReference();
                lastSpan = keyType.Span;
            }

            TokenKind? orderingModifier = null;
            if (Peek().Kind is TokenKind.Ascending or TokenKind.Descending)
            {
                orderingModifier = Advance().Kind;
                lastSpan = _tokens[_position - 1].Span;
            }

            if (elementType == null)
            {
                elementType = new MissingTypeReference(typeSpan);
            }

            var span = SourceSpan.Covering(typeSpan, lastSpan);
            return new CollectionTypeReference(collectionMeta, elementType, keyType, orderingModifier, span);
        }

        /// <summary>
        /// Parses a simple inner type (no nested collections) for collection element types.
        /// </summary>
        private ParsedTypeReference ParseInnerTypeReference()
        {
            var peekToken = Peek();

            // Handle CI type prefix: ~string
            if (peekToken.Kind == TokenKind.Tilde)
            {
                var tildeToken = Advance();
                var innerToken = Peek();
                var lookupKind = innerToken.Kind == TokenKind.Set ? TokenKind.SetType : innerToken.Kind;
                if (Types.ByToken.TryGetValue(lookupKind, out var innerType))
                {
                    Advance();
                    var span = SourceSpan.Covering(tildeToken.Span, innerToken.Span);
                    return new CITypeReference(innerType, span);
                }
                return new MissingTypeReference(tildeToken.Span);
            }

            var lookupTokenKind = peekToken.Kind == TokenKind.Set ? TokenKind.SetType : peekToken.Kind;
            if (Types.ByToken.TryGetValue(lookupTokenKind, out var typeMeta))
            {
                var typeToken = Advance();
                return new SimpleTypeReference(typeMeta, typeToken.Span);
            }

            return new MissingTypeReference(peekToken.Span);
        }

        /// <summary>
        /// Attempts to parse qualifier slots that follow a simple type reference.
        /// Iterates over <see cref="QualifierShape.Slots"/> from the catalog — no hardcoded token sets.
        /// Returns the original <paramref name="typeRef"/> unchanged if the type has no qualifier shape
        /// or if no preposition tokens are found.
        /// </summary>
        private ParsedTypeReference TryParseQualifiers(
            ParsedTypeReference typeRef, TypeMeta typeMeta)
        {
            if (typeMeta.QualifierShape is null)
                return typeRef;

            var qualifiers = ImmutableArray.CreateBuilder<ParsedQualifier>();
            var lastSpan = typeRef.Span;

            foreach (var slot in typeMeta.QualifierShape.Slots)
            {
                if (Peek().Kind != slot.Preposition)
                    continue;

                Advance(); // consume preposition ('in' / 'of' / 'to')

                if (Peek().Kind != TokenKind.TypedConstant)
                {
                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.ExpectedToken, Peek().Span,
                        "typed constant", Peek().Text));
                    continue;
                }

                var valueToken = Advance();
                qualifiers.Add(new ParsedQualifier(
                    slot.Preposition, slot.Axis,
                    valueToken.Text, valueToken.Span));
                lastSpan = valueToken.Span;
            }

            if (qualifiers.Count == 0)
                return typeRef;

            return new QualifiedTypeReference(
                typeRef, qualifiers.ToImmutable(),
                SourceSpan.Covering(typeRef.Span, lastSpan));
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

                    modifiers.Add(new ParsedModifier(modMeta.Kind, valueExpr));
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
            var args = new List<(string Name, ParsedTypeReference Type, ImmutableArray<ModifierKind> Modifiers)>();
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
                    while (Modifiers.ByValueToken.TryGetValue(Peek().Kind, out var modMeta)
                           && modMeta.ApplicableDeclarationSites.HasFlag(argSite))
                    {
                        modifiers.Add(modMeta.Kind);
                        Advance();

                        if (!modMeta.HasValue)
                            continue;

                        if (!ExpressionStartTokens.Contains(Peek().Kind))
                        {
                            _diagnostics.Add(DiagnosticsCatalog.Create(
                                DiagnosticCode.ExpectedToken, Peek().Span, "expression", Peek().Text));
                            continue;
                        }

                        ParseExpression(0, () =>
                            Peek().Kind is TokenKind.Comma or TokenKind.RightParen
                            || IsAtConstructBoundary()
                            || (Modifiers.ByValueToken.TryGetValue(Peek().Kind, out var nextMod)
                                && nextMod.ApplicableDeclarationSites.HasFlag(argSite)));
                    }

                    args.Add((nameToken.Text, parsedType, modifiers.ToImmutable()));
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

        // ── StateTarget: state name identifier ──────────────────────────────────

        private SlotValue ParseStateTarget(ConstructSlot slot)
        {
            var current = Peek();
            var meta = Tokens.GetMeta(current.Kind);
            if (current.Kind == TokenKind.Identifier || meta.IsStateWildcard)
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
                return new FieldTargetSlot(tok.Text, tok.Span);
            }

            if (current.Kind == TokenKind.Identifier)
            {
                var first = Advance();
                var lastSpan = first.Span;

                while (Peek().Kind == TokenKind.Comma)
                {
                    Advance(); // consume comma
                    if (Peek().Kind == TokenKind.Identifier)
                    {
                        lastSpan = Advance().Span;
                        continue;
                    }

                    _diagnostics.Add(DiagnosticsCatalog.Create(
                        DiagnosticCode.ExpectedToken, Peek().Span, "identifier", Peek().Text));
                    break;
                }

                return new FieldTargetSlot(first.Text, SourceSpan.Covering(first.Span, lastSpan));
            }

            if (!slot.IsRequired)
                return MakeSentinel(slot);
            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "field name", Peek().Text));
            return new FieldTargetSlot(null, Peek().Span);
        }

        private SlotValue ParseActionChain(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.Arrow)
                return MakeSentinel(slot);

            // Peek ahead: only enter action chain if token after arrow is an action keyword
            if (!Actions.ByTokenKind.ContainsKey(Peek(1).Kind))
                return MakeSentinel(slot);

            var actions = new List<ParsedAction>();
            var startSpan = Peek().Span;
            var lastSpan = startSpan;

            while (Peek().Kind == TokenKind.Arrow && !IsAtEnd)
            {
                // Before consuming arrow, verify next token is an action keyword
                if (!Actions.ByTokenKind.ContainsKey(Peek(1).Kind))
                    break;

                Advance(); // consume '->'
                var actionToken = Peek();
                if (Actions.ByTokenKind.TryGetValue(actionToken.Kind, out var actionMeta))
                {
                    var actionStartSpan = Advance().Span; // consume action keyword
                    var action = ParseActionByShape(actionMeta, actionStartSpan);
                    actions.Add(action);
                    lastSpan = action.Span;
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

        /// <summary>
        /// Parses action operands based on ActionSyntaxShape from catalog metadata.
        /// </summary>
        private ParsedAction ParseActionByShape(ActionMeta meta, SourceSpan actionStartSpan)
        {
            var kind = meta.Kind;

            // Terminator for action expressions: next arrow or construct boundary
            Func<bool> isAtActionBoundary = () => Peek().Kind == TokenKind.Arrow || IsAtConstructBoundary();

            // Shape-specific separator tokens from catalog — only this shape's separators terminate the target.
            var separators = Actions.GetShapeMeta(meta.SyntaxShape).SeparatorTokens;

            switch (meta.SyntaxShape)
            {
                case ActionSyntaxShape.AssignValue:
                    return ParseAssignValueAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionValue:
                    return ParseCollectionValueAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionInto:
                    return ParseCollectionIntoAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.FieldOnly:
                    return ParseFieldOnlyAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionValueBy:
                    return ParseCollectionValueByAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.InsertAt:
                    return ParseInsertAtAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.RemoveAtIndex:
                    return ParseRemoveAtIndexAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.PutKeyValue:
                    return ParsePutKeyValueAction(kind, actionStartSpan, isAtActionBoundary, separators);
                case ActionSyntaxShape.CollectionIntoBy:
                    return ParseCollectionIntoByAction(kind, actionStartSpan, isAtActionBoundary, separators);
                default:
                {
                    // Unknown shape — produce malformed action
                    return new MalformedAction(kind, actionStartSpan);
                }
            }
        }

        private static FrozenSet<TokenKind> GetSecondarySeparators(ActionKind primaryKind, int slotIndex)
        {
            if (!Actions.SecondaryByPrimaryActionKind.TryGetValue(primaryKind, out var secondaries))
                return Enumerable.Empty<TokenKind>().ToFrozenSet();

            return secondaries
                .Select(secondary => Actions.GetShapeMeta(secondary.SyntaxShape))
                .Where(shape => shape.Slots.Length > slotIndex && shape.Slots[slotIndex].PrecedingSeparator is not null)
                .Select(shape => shape.Slots[slotIndex].PrecedingSeparator!.Value)
                .Distinct()
                .ToFrozenSet();
        }

        private static bool TryGetSecondaryAction(ActionKind primaryKind, int slotIndex, TokenKind separator, out ActionMeta secondary)
        {
            secondary = null!;

            if (!Actions.SecondaryByPrimaryActionKind.TryGetValue(primaryKind, out var secondaries))
                return false;

            foreach (var candidate in secondaries)
            {
                var slots = Actions.GetShapeMeta(candidate.SyntaxShape).Slots;
                if (slots.Length > slotIndex && slots[slotIndex].PrecedingSeparator == separator)
                {
                    secondary = candidate;
                    return true;
                }
            }

            return false;
        }

        [HandlesCatalogMember(ActionSyntaxShape.AssignValue)]
        private ParsedAction ParseAssignValueAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field = expression
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.AssignValue).Slots;
            var target = ParseActionTarget(separators, isAtActionBoundary);
            Expect(slots[1].PrecedingSeparator!.Value); // '='
            var value = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, value.Span);
            return new AssignAction(kind, target, value, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionValue)]
        private ParsedAction ParseCollectionValueAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field expression
            var target = ParseActionTarget(separators, isAtActionBoundary);

            if (TryGetSecondaryAction(kind, 1, Peek().Kind, out var secondaryAfterTarget))
            {
                return secondaryAfterTarget.SyntaxShape switch
                {
                    ActionSyntaxShape.RemoveAtIndex => ParseRemoveAtIndexAction(
                        secondaryAfterTarget.Kind,
                        actionStartSpan,
                        target,
                        isAtActionBoundary),
                    _ => new MalformedAction(secondaryAfterTarget.Kind, actionStartSpan),
                };
            }

            var secondaryValueSeparators = GetSecondarySeparators(kind, 2);
            var value = ParseExpression(0, () => secondaryValueSeparators.Contains(Peek().Kind) || isAtActionBoundary());

            if (TryGetSecondaryAction(kind, 2, Peek().Kind, out var secondaryAfterValue))
            {
                return secondaryAfterValue.SyntaxShape switch
                {
                    ActionSyntaxShape.CollectionValueBy => ParseCollectionValueByAction(
                        secondaryAfterValue.Kind,
                        actionStartSpan,
                        target,
                        value,
                        isAtActionBoundary),
                    _ => new MalformedAction(secondaryAfterValue.Kind, actionStartSpan),
                };
            }

            var span = SourceSpan.Covering(actionStartSpan, value.Span);
            return new CollectionValueAction(kind, target, value, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionInto)]
        private ParsedAction ParseCollectionIntoAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field [into field]
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionInto).Slots;
            var intoSlot = slots[1]; // IntoTarget: optional, 'into'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var lastSpan = target.Span;
            ParsedExpression? intoTarget = null;
            if (Peek().Kind == intoSlot.PrecedingSeparator)
            {
                Advance(); // consume 'into'
                intoTarget = ParseActionTarget(separators, isAtActionBoundary);
                lastSpan = intoTarget.Span;
            }

            if (TryGetSecondaryAction(kind, 2, Peek().Kind, out var secondaryAfterInto))
            {
                return secondaryAfterInto.SyntaxShape switch
                {
                    ActionSyntaxShape.CollectionIntoBy => ParseCollectionIntoByAction(
                        secondaryAfterInto.Kind,
                        actionStartSpan,
                        target,
                        intoTarget,
                        isAtActionBoundary),
                    _ => new MalformedAction(secondaryAfterInto.Kind, actionStartSpan),
                };
            }

            var span = SourceSpan.Covering(actionStartSpan, lastSpan);
            return new CollectionIntoAction(kind, target, intoTarget, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.FieldOnly)]
        private ParsedAction ParseFieldOnlyAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, target.Span);
            return new FieldOnlyAction(kind, target, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionValueBy)]
        private ParsedAction ParseCollectionValueByAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field expr by expr
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy).Slots;
            var orderingKeySlot = slots[2]; // OrderingKey: required, 'by'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var value = ParseExpression(0, () => Peek().Kind == orderingKeySlot.PrecedingSeparator || isAtActionBoundary());
            return ParseCollectionValueByAction(kind, actionStartSpan, target, value, isAtActionBoundary);
        }

        private ParsedAction ParseCollectionValueByAction(ActionKind kind, SourceSpan actionStartSpan, ParsedExpression target, ParsedExpression value, Func<bool> isAtActionBoundary)
        {
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy).Slots;
            var orderingKeySlot = slots[2]; // OrderingKey: required, 'by'
            Expect(orderingKeySlot.PrecedingSeparator!.Value); // 'by'
            var orderingKey = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, orderingKey.Span);
            return new CollectionValueByAction(kind, target, value, orderingKey, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.InsertAt)]
        private ParsedAction ParseInsertAtAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field expr at expr
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.InsertAt).Slots;
            var indexSlot = slots[2]; // Index: required, 'at'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var value = ParseExpression(0, () => Peek().Kind == indexSlot.PrecedingSeparator || isAtActionBoundary());
            Expect(indexSlot.PrecedingSeparator!.Value); // 'at'
            var index = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, index.Span);
            return new InsertAtAction(kind, target, value, index, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.RemoveAtIndex)]
        private ParsedAction ParseRemoveAtIndexAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field at expr
            var target = ParseActionTarget(separators, isAtActionBoundary);
            return ParseRemoveAtIndexAction(kind, actionStartSpan, target, isAtActionBoundary);
        }

        private ParsedAction ParseRemoveAtIndexAction(ActionKind kind, SourceSpan actionStartSpan, ParsedExpression target, Func<bool> isAtActionBoundary)
        {
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.RemoveAtIndex).Slots;
            var indexSlot = slots[1]; // Index: required, 'at'
            Expect(indexSlot.PrecedingSeparator!.Value); // 'at'
            var index = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, index.Span);
            return new RemoveAtAction(kind, target, index, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.PutKeyValue)]
        private ParsedAction ParsePutKeyValueAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field key = value
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.PutKeyValue).Slots;
            var valueSlot = slots[2]; // Value: required, '='
            var target = ParseActionTarget(separators, isAtActionBoundary);
            var key = ParseExpression(0, () => Peek().Kind == valueSlot.PrecedingSeparator || isAtActionBoundary());
            Expect(valueSlot.PrecedingSeparator!.Value); // '='
            var value = ParseExpression(0, isAtActionBoundary);
            var span = SourceSpan.Covering(actionStartSpan, value.Span);
            return new PutKeyValueAction(kind, target, key, value, span);
        }

        [HandlesCatalogMember(ActionSyntaxShape.CollectionIntoBy)]
        private ParsedAction ParseCollectionIntoByAction(ActionKind kind, SourceSpan actionStartSpan, Func<bool> isAtActionBoundary, FrozenSet<TokenKind> separators)
        {
            // verb field [into field] [by key]
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy).Slots;
            var intoSlot = slots[1]; // IntoTarget: optional, 'into'
            var target = ParseActionTarget(separators, isAtActionBoundary);
            ParsedExpression? intoTarget = null;

            if (Peek().Kind == intoSlot.PrecedingSeparator)
            {
                Advance(); // consume 'into'
                intoTarget = ParseActionTarget(separators, isAtActionBoundary);
            }

            return ParseCollectionIntoByAction(kind, actionStartSpan, target, intoTarget, isAtActionBoundary);
        }

        private ParsedAction ParseCollectionIntoByAction(ActionKind kind, SourceSpan actionStartSpan, ParsedExpression target, ParsedExpression? intoTarget, Func<bool> isAtActionBoundary)
        {
            var slots = Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy).Slots;
            var orderingCaptureSlot = slots[2]; // OrderingCapture: optional, 'by'
            var lastSpan = intoTarget?.Span ?? target.Span;
            ParsedExpression? orderingCapture = null;

            if (Peek().Kind == orderingCaptureSlot.PrecedingSeparator)
            {
                Advance(); // consume 'by'
                orderingCapture = ParseActionTarget(Actions.GetShapeMeta(ActionSyntaxShape.CollectionIntoBy).SeparatorTokens, isAtActionBoundary);
                lastSpan = orderingCapture.Span;
            }

            var span = SourceSpan.Covering(actionStartSpan, lastSpan);
            return new CollectionIntoByAction(kind, target, intoTarget, orderingCapture, span);
        }

        /// <summary>
        /// Parses the target (field reference) of an action.
        /// Terminates on the shape-specific <paramref name="separators"/> derived from
        /// <see cref="Actions.GetShapeMeta"/> — never on the union of all separator tokens.
        /// </summary>
        private ParsedExpression ParseActionTarget(FrozenSet<TokenKind> separators, Func<bool> terminates)
        {
            return ParseExpression(0, () => separators.Contains(Peek().Kind) || terminates());
        }

        // ── Boundary detection ──────────────────────────────────────────────────

        private bool IsAtConstructBoundary() =>
            ConstructsCatalog.LeadingTokens.Contains(Peek().Kind) || IsAtEnd;
    }
}

