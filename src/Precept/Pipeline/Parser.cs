using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline.SyntaxNodes;

namespace Precept.Pipeline;

// Design: Parser Architecture and Dispatch Table
// ================================================
// The parser is a hand-written recursive descent parser. This is intentional and
// architecturally correct — see docs/language/catalog-system.md § Pipeline Stage Impact,
// which explicitly carves out grammar productions as hand-written code. The dispatch
// table below is grammar mechanics, not domain vocabulary.
//
// Architectural Boundary (per Frank's ruling, 2026-04-27):
//   - Grammar productions (recursive descent structure, dispatch loop, disambiguation
//     sequences) → hand-written. The catalog's ConstructMeta.Entries / PrimaryLeadingToken
//     exists for completions, grammar generation, and MCP — not for parser dispatch.
//   - Vocabulary tables (operator precedence, type keywords, modifier sets, action
//     recognition sets) → MUST be derived from catalog metadata, specifically:
//       • Operators.All       → operator precedence/association tables
//       • Types.All           → type keyword recognition
//       • Modifiers.All       → modifier recognition set
//       • Actions.All         → action recognition set
//     These MUST NOT be hardcoded lists in the parser. That would be a real
//     catalog-system violation.
//
// Why the dispatch table is hand-written (the 1:N problem):
//   Five leading tokens (Field, State, Event, Rule, Write) map 1:1 to a single
//   ConstructKind and can be dispatched trivially. But four tokens (In, To, From, On)
//   each map to 2–3 ConstructKinds. Disambiguation requires consuming a state/event
//   target and then looking ahead to the following verb. That's grammar structure that
//   cannot be expressed as a flat catalog lookup table.
//
// Top-Level Dispatch Loop (intended shape):
// -----------------------------------------
// The parser operates as a keyword-dispatched loop over the top-level token stream.
// Each leading token routes to a dedicated parse method. Unknown tokens emit a
// diagnostic and synchronize to the next declaration boundary.
//
//   while not EndOfSource:
//     switch current token:
//
//       Field  → ParseFieldDeclaration()      → ConstructKind.FieldDeclaration
//       State  → ParseStateDeclaration()      → ConstructKind.StateDeclaration
//       Event  → ParseEventDeclaration()      → ConstructKind.EventDeclaration
//       Rule   → ParseRuleDeclaration()       → ConstructKind.RuleDeclaration
//       Write  → ParseAccessMode()            → ConstructKind.AccessMode (stateless precepts only)
//
//       In     → ParseInScoped()              → disambiguates:
//                                                  ConstructKind.StateEnsure  (In <state> ensure ...)
//                                                  ConstructKind.AccessMode   (In <state> write ...)
//
//       To     → ParseToScoped()              → disambiguates:
//                                                  ConstructKind.StateEnsure  (To <state> ensure ...)
//                                                  ConstructKind.StateAction  (To <state> <action> ...)
//
//       From   → ParseFromScoped()            → disambiguates:
//                                                  ConstructKind.TransitionRow (From <state> To <state> On <event>)
//                                                  ConstructKind.StateEnsure   (From <state> ensure ...)
//                                                  ConstructKind.StateAction   (From <state> <action> ...)
//
//       On     → ParseOnScoped()              → disambiguates:
//                                                  ConstructKind.EventEnsure   (On <event> ensure ...)
//                                                  ConstructKind.EventHandler  (On <event> <action> ...)
//
//       EndOfSource → exit loop
//       anything else → EmitDiagnostic() + SyncToNextDeclaration()
//
// Error Recovery:
//   SyncToNextDeclaration() advances past the offending token(s) to the next known
//   leading token (Field, State, Event, Rule, Write, In, To, From, On) so parsing
//   can continue and produce a maximal set of diagnostics in one pass.

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
    /// Token kinds that begin an action statement (<c>set</c>, <c>add</c>, etc.).
    /// Derived from <see cref="Actions.All"/>.
    /// </summary>
    internal static readonly FrozenSet<TokenKind> ActionKeywords =
        Actions.All
            .Select(a => a.Token.Kind)
            .ToFrozenSet();

    // ════════════════════════════════════════════════════════════════════════════
    //  Public entry point
    // ════════════════════════════════════════════════════════════════════════════

    public static SyntaxTree Parse(TokenStream tokens) => throw new NotImplementedException();

    // ════════════════════════════════════════════════════════════════════════════
    //  ParseSession — mutable cursor state for a single parse pass
    // ════════════════════════════════════════════════════════════════════════════

    private ref struct ParseSession
    {
        private readonly ImmutableArray<Token> _tokens;
#pragma warning disable CS0414 // Fields assigned but not yet used (PR 3 dispatch loop will use)
        private int _position;
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics;
#pragma warning restore CS0414

        public ParseSession(ImmutableArray<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
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
            ConstructSlotKind.StateModifierList   => ParseStateModifierList(isOptional),
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
            // CS8509 enforcement: a new ConstructSlotKind member without an arm is a build error.
            // The wildcard below covers only unnamed numeric values outside the defined enum range.
            _ => throw new ArgumentOutOfRangeException(nameof(slotKind), slotKind,
                $"Unknown ConstructSlotKind: {slotKind}"),
        };

        // ── Slot parser stubs (PR 3–5 will implement) ─────────────────────────

        private SyntaxNode? ParseIdentifierList(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseTypeExpression(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseModifierList(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseStateModifierList(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseArgumentList(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseComputeExpression(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseGuardClause(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseActionChain(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseOutcome(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseStateTarget(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseEventTarget(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseEnsureClause(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseBecauseClause(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseAccessModeKeyword(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseFieldTarget(bool isOptional) => throw new NotImplementedException();
        private SyntaxNode? ParseRuleExpression(bool isOptional) => throw new NotImplementedException();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  BuildNode — exhaustive 12-arm switch (Slice 2.4)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Assembles a typed <see cref="Declaration"/> from the generic slot array
    /// produced by <see cref="ParseSession.ParseConstructSlots"/>. One arm per
    /// <see cref="ConstructKind"/>.
    /// </summary>
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
            false),

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

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ConstructKind: {kind}"),
    };
}

// ════════════════════════════════════════════════════════════════════════════
//  BuildNode helper extensions — temporary casting bridges until PR 3
//  introduces concrete wrapper nodes for identifier lists, etc.
// ════════════════════════════════════════════════════════════════════════════

internal static class BuildNodeExtensions
{
    internal static Token AsToken(this SyntaxNode _) => throw new NotImplementedException();
    internal static ImmutableArray<Token> AsTokenArray(this SyntaxNode _) => throw new NotImplementedException();
    internal static ImmutableArray<FieldModifierNode> AsFieldModifiers(this SyntaxNode _) => throw new NotImplementedException();
    internal static ImmutableArray<StateEntryNode> AsStateEntries(this SyntaxNode _) => throw new NotImplementedException();
    internal static ImmutableArray<ArgumentNode> AsArguments(this SyntaxNode _) => throw new NotImplementedException();
    internal static ImmutableArray<Statement> AsStatements(this SyntaxNode _) => throw new NotImplementedException();
}
