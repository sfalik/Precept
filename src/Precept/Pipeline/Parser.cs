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
public static partial class Parser
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
            .OfType<SingleTokenOp>()
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

    // ════════════════════════════════════════════════════════════════════════════
    //  Structural sets — derived from catalog or structurally motivated
    //
    //  BEFORE ADDING A NEW FrozenSet<TokenKind> HERE: check whether a catalog
    //  already encodes this distinction. Catalog sources:
    //    - Constructs.ByLeadingToken         — leading tokens per construct
    //    - ConstructEntry.DisambiguationTokens — disambiguation verbs per construct
    //    - Modifiers / Actions / Types / Operators — their respective token sets
    //  Derive; never hardcode a parallel copy of catalog knowledge.
    // ════════════════════════════════════════════════════════════════════════════

    // NewLine is intentionally absent: whitespace is cosmetic (§0.1.5).
    // NewLine tokens never reach Parser.Parse() — they are stripped by the pre-parse
    // filter at the Parse() call site (direct ParseSession construction bypasses this).
    // The Pratt loop additionally terminates at NewLine via the !OperatorPrecedence
    // fallthrough — belt-and-suspenders, not load-bearing.
    private static readonly FrozenSet<TokenKind> StructuralBoundaryTokens = new[]
    {
        TokenKind.When, TokenKind.Because, TokenKind.Arrow, TokenKind.Ensure,
        TokenKind.EndOfSource,
    }.ToFrozenSet();

    internal static readonly FrozenSet<TokenKind> ExpressionBoundaryTokens =
        StructuralBoundaryTokens.Union(Constructs.LeadingTokens).ToFrozenSet();

    /// <summary>
    /// Types valid as element types in a <c>choice of T(...)</c> declaration.
    /// Derived from <see cref="TypeTrait.ChoiceElement"/> — never hardcoded.
    /// </summary>
    internal static readonly FrozenSet<TokenKind> ChoiceElementTypeKeywords =
        Types.ByToken
            .Where(kvp => kvp.Value.Traits.HasFlag(TypeTrait.ChoiceElement))
            .Select(kvp => kvp.Key)
            .ToFrozenSet();

    /// <summary>
    /// Maps qualifier-preposition tokens that are also construct-leading tokens to their
    /// catalog-derived disambiguation verb sets. Derived from <see cref="Constructs.ByLeadingToken"/>
    /// + <see cref="DisambiguationEntry.DisambiguationTokens"/> — never hardcoded.
    ///   In → {Ensure, Modify, Omit}
    ///   To → {Arrow, Ensure}
    /// </summary>
    internal static readonly FrozenDictionary<TokenKind, FrozenSet<TokenKind>>
        AmbiguousQualifierPrepositions =
            new[] { TokenKind.In, TokenKind.To }
                .Where(Constructs.ByLeadingToken.ContainsKey)
                .ToFrozenDictionary(
                    k => k,
                    k => Constructs.ByLeadingToken[k]
                        .Where(c => c.Entry.DisambiguationTokens is { IsDefaultOrEmpty: false })
                        .SelectMany(c => c.Entry.DisambiguationTokens!.Value)
                        .ToFrozenSet());

    /// <summary>
    /// Token kinds that may appear as a member name after <c>.</c> even though they
    /// are normally keywords. <c>min</c> and <c>max</c> are DSL aggregation keywords
    /// but are also idiomatic member-accessor names on numeric sequences.
    /// Catalog-derived from <see cref="TokenMeta.IsValidAsMemberName"/>.
    /// </summary>
    internal static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName =
        Tokens.All.Where(t => t.IsValidAsMemberName).Select(t => t.Kind).ToFrozenSet();

    // ════════════════════════════════════════════════════════════════════════════
    //  Public entry point
    // ════════════════════════════════════════════════════════════════════════════

    public static SyntaxTree Parse(TokenStream tokens)
    {
        // The parser is comment-blind and whitespace-insensitive by design (§0.1.5).
        // NewLine and Comment are stripped here; full-fidelity token data lives in
        // Compilation.Tokens for LSP and other consumers that need it.
        // NOTE: direct ParseSession construction (e.g. in tests) bypasses this filter.
        var parseTokens = tokens.Tokens
            .Where(t => t.Kind is not TokenKind.NewLine and not TokenKind.Comment)
            .ToImmutableArray();
        var session = new ParseSession(parseTokens);
        return session.ParseAll();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ParseSession — mutable cursor state for a single parse pass
    // ════════════════════════════════════════════════════════════════════════════

    [Precept.HandlesCatalogExhaustively(typeof(ExpressionFormKind))]
    internal ref partial struct ParseSession
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

        private void EmitDiagnostic(DiagnosticCode code, SourceSpan span, params object?[] args)
        {
            _diagnostics.Add(Diagnostics.Create(code, span, args));
        }

        // ── Top-level dispatch loop ───────────────────────────────────────────

        internal SyntaxTree ParseAll()
        {
            PreceptHeaderNode? header = null;
            var declarations = ImmutableArray.CreateBuilder<Declaration>();

            // Parse optional precept header
            if (!IsAtEnd() && Current().Kind == TokenKind.Precept)
            {
                header = ParsePreceptHeaderDeclaration();
            }

            // Main dispatch loop
            while (!IsAtEnd())
            {
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
            slots[1]?.AsStatements() ?? [],
            null), // PostConditionGuard — always null in slot-based path (dead code)
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
