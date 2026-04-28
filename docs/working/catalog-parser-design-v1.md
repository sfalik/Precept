# Catalog-Driven Parser: Full Vision Design (Round 1)

**By:** Frank
**Date:** 2026-04-27
**Status:** Design session Round 1 — awaiting George's challenge (Round 2)
**References:** Prior analysis artifacts in `docs/working/` (6 files)

---

## 1. Vision Statement

### What "Fully Catalog-Driven Parser" Means

The parser has **zero hardcoded vocabulary.** Every keyword, operator, type name, modifier, action, construct shape, disambiguation path, and sync token is derived from catalog metadata at parser construction time. The parser's code contains **grammar mechanics** — how to navigate structure — but never **domain knowledge** — what the language's vocabulary is.

**The test:** If you can change the meaning, add a member, or remove a member from the language surface by editing only catalog entries (and, where necessary, adding a slot parser for a genuinely new grammar shape), then the parser is fully catalog-driven. If the parser has a hardcoded string, keyword check, or token-kind switch arm that would need a manual edit to accommodate a vocabulary change, it is NOT fully catalog-driven.

### What Changes from the Prior Analysis

The prior analysis walked back Layer D (slot-driven productions) and rejected Layer C (disambiguation). Both walkbacks were driven by legitimate cost/risk concerns — but those concerns assumed a specific implementation approach: "rewrite the existing 11 parse methods as a generic slot iterator simultaneously." That's the wrong framing.

**The new framing:** We don't rewrite. We build the catalog-driven parser from scratch (the parser IS a stub today), using a design that makes the generic path the natural way to write it. The question is not "should we refactor 11 methods into a generic loop?" — it's "when we write the parser for the first time, should we write it catalog-driven or hardcoded?"

The answer is catalog-driven. Here's why and how.

### What Stays

1. **Hand-written recursive descent.** The parser is not generated from a grammar file. It's hand-written C# code that reads catalog metadata.
2. **Pratt expression parser.** Expression parsing uses a Pratt loop with catalog-derived binding powers. The loop structure is hand-written; the precedence table is catalog-derived.
3. **Per-construct AST node types.** `FieldDeclarationNode`, `TransitionRowNode`, etc., remain strongly typed records. Downstream consumers pattern-match on them.
4. **Static pure function entry point.** `Parser.Parse(TokenStream) → SyntaxTree` doesn't change.

---

## 2. The Full Architecture

### Layer A: Vocabulary Tables (Unchanged from Prior Analysis)

**Catalog changes:** None. Existing `All` properties on `Operators`, `Types`, `Modifiers`, `Actions` carry everything.

**Parser shape:** Frozen dictionaries built at `ParseSession` construction:

```csharp
// All vocabulary derived from catalogs — zero hardcoded keyword lists
private readonly FrozenDictionary<TokenKind, OperatorMeta> _operators
    = Operators.All.ToFrozenDictionary(o => o.Token.Kind);

private readonly FrozenSet<TokenKind> _typeKeywords
    = Types.All.Where(t => t.Token is not null).Select(t => t.Token!.Kind).ToFrozenSet();

private readonly FrozenSet<TokenKind> _fieldModifiers
    = Modifiers.All.OfType<FieldModifierMeta>().Select(m => m.Token.Kind).ToFrozenSet();

private readonly FrozenSet<TokenKind> _stateModifiers
    = Modifiers.All.OfType<StateModifierMeta>().Select(m => m.Token.Kind).ToFrozenSet();

private readonly FrozenSet<TokenKind> _actionKeywords
    = Actions.All.Select(a => a.Token.Kind).ToFrozenSet();
```

**Why this is right:** The highest-frequency language changes are new types and modifiers. This layer makes those zero-parser-edit changes. Already agreed upon. Already designed. Just build it.

---

### Layer B: Dispatch Table (Enhanced)

**Catalog changes:** Add two derived indexes to `Constructs`:

```csharp
// On Constructs static class
public static FrozenDictionary<TokenKind, ImmutableArray<ConstructMeta>> ByLeadingToken { get; }
    = All.GroupBy(c => c.LeadingToken)
         .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());

public static FrozenSet<TokenKind> LeadingTokens { get; }
    = All.Select(c => c.LeadingToken).ToFrozenSet();
```

**Parser shape:** The dispatch loop queries the catalog, not a hardcoded switch:

```csharp
private void ParseAll()
{
    ParseHeader();
    while (Current().Kind != TokenKind.EndOfSource)
    {
        SkipTrivia();
        var token = Current().Kind;

        if (!Constructs.ByLeadingToken.TryGetValue(token, out var candidates))
        {
            EmitDiagnostic(DiagnosticCode.UnexpectedToken, Current());
            SyncToNextDeclaration();
            continue;
        }

        Declaration node = candidates.Length == 1
            ? ParseConstruct(candidates[0])            // 1:1 — direct dispatch
            : ParseDisambiguated(token, candidates);   // 1:N — disambiguation

        _declarations.Add(node);
    }
}
```

**Why this is right:** The dispatch table IS the catalog. Adding construct #12 with a unique leading token requires zero dispatch code. The 1:N path delegates to disambiguation — addressed in Layer C.

---

### Layer C: Declarative Disambiguation (THE KEY DEPARTURE)

This is where the prior analysis stopped. I rejected Layer C because the `when` guard intermediate breaks flat metadata. Shane is asking me to think outside the box. So here's the outside-the-box reframe:

**The insight:** Disambiguation is not one concern — it's two separable concerns:

1. **Token-level routing:** After the anchor target, which token(s) select which construct? This IS flat metadata. `Ensure` → `StateEnsure`. `Arrow` → `StateAction`. `On` → `TransitionRow`.
2. **Guard consumption:** The `when` clause can appear before the disambiguation token. This is a **grammar-level** concern — the parser must consume the guard before looking at the routing token.

The prior analysis conflated these. It tried to express both in a single `DisambiguationRule` record and concluded the guard consumption made it impossible. But **guard consumption is not a disambiguation concern — it's a slot concern.** The guard is a slot that the construct definition already declares. The disambiguation happens AFTER the guard is consumed.

**New catalog addition — `DisambiguationKey`:**

```csharp
/// <summary>
/// Describes how this construct is distinguished from other constructs
/// sharing the same leading token. Null for unambiguous (1:1) constructs.
/// </summary>
public sealed record DisambiguationKey(
    /// <summary>
    /// Token(s) that, when seen after the anchor target (and optional guard),
    /// select this construct. Array because AccessMode has 3 tokens (Write/Read/Omit).
    /// </summary>
    ImmutableArray<TokenKind> Tokens);
```

Added to `ConstructMeta`:

```csharp
public sealed record ConstructMeta(
    ConstructKind                Kind,
    string                       Name,
    string                       Description,
    string                       UsageExample,
    ConstructKind[]              AllowedIn,
    IReadOnlyList<ConstructSlot> Slots,
    TokenKind                    LeadingToken,
    DisambiguationKey?           Disambiguation = null,  // ← NEW
    string?                      SnippetTemplate = null);
```

**How the disambiguation table populates:**

| Construct | LeadingToken | Disambiguation.Tokens |
|-----------|-------------|----------------------|
| `PreceptHeader` | `Precept` | null (1:1) |
| `FieldDeclaration` | `Field` | null (1:1) |
| `StateDeclaration` | `State` | null (1:1) |
| `EventDeclaration` | `Event` | null (1:1) |
| `RuleDeclaration` | `Rule` | null (1:1) |
| `TransitionRow` | `From` | `[On]` |
| `StateEnsure` | `In` | `[Ensure]` |
| `AccessMode` | `In`/`Write` | `[Write, Read, Omit]` for In-scoped; null for root-level Write |
| `StateAction` | `To` | `[Arrow]` |
| `EventEnsure` | `On` | `[Ensure]` |
| `EventHandler` | `On` | `[Arrow]` |

**Wait — `StateEnsure` has three leading tokens (`In`, `To`, `From`).** The current catalog records one `LeadingToken` per construct. This is a real problem the prior analysis sidestepped because it never got this far.

**The fix: `LeadingTokens` (plural) on ConstructMeta.**

```csharp
public sealed record ConstructMeta(
    ConstructKind                Kind,
    string                       Name,
    string                       Description,
    string                       UsageExample,
    ConstructKind[]              AllowedIn,
    IReadOnlyList<ConstructSlot> Slots,
    ImmutableArray<TokenKind>    LeadingTokens,           // ← PLURAL
    DisambiguationKey?           Disambiguation = null,
    string?                      SnippetTemplate = null);
```

`StateEnsure` declares `LeadingTokens: [In, To, From]` with `Disambiguation: [Ensure]`.
`StateAction` declares `LeadingTokens: [To, From]` with `Disambiguation: [Arrow]`.
`AccessMode` declares `LeadingTokens: [Write, In]` with disambiguation that varies by leading token.

**Problem:** `AccessMode` has DIFFERENT disambiguation depending on its leading token. From `Write` at root level, it's unambiguous (null). From `In`, it disambiguates on `[Write, Read, Omit]`.

**Solution — `DisambiguationKey` is per-leading-token, not per-construct:**

```csharp
/// <summary>
/// Per-leading-token disambiguation entries. For constructs with multiple
/// leading tokens that disambiguate differently per entry.
/// </summary>
public sealed record DisambiguationEntry(
    TokenKind                  LeadingToken,
    ImmutableArray<TokenKind>? DisambiguationTokens);   // null = unambiguous from this leading token
```

And on `ConstructMeta`:

```csharp
public sealed record ConstructMeta(
    ConstructKind                    Kind,
    string                           Name,
    string                           Description,
    string                           UsageExample,
    ConstructKind[]                  AllowedIn,
    IReadOnlyList<ConstructSlot>     Slots,
    ImmutableArray<DisambiguationEntry> Entries,          // ← replaces LeadingToken
    string?                          SnippetTemplate = null);
```

**Full disambiguation table:**

| Construct | Entry.LeadingToken | Entry.DisambiguationTokens |
|-----------|-------------------|---------------------------|
| `PreceptHeader` | `Precept` | null |
| `FieldDeclaration` | `Field` | null |
| `StateDeclaration` | `State` | null |
| `EventDeclaration` | `Event` | null |
| `RuleDeclaration` | `Rule` | null |
| `TransitionRow` | `From` | `[On]` |
| `StateEnsure` | `In` | `[Ensure]` |
| `StateEnsure` | `To` | `[Ensure]` |
| `StateEnsure` | `From` | `[Ensure]` |
| `AccessMode` | `Write` | null |
| `AccessMode` | `In` | `[Write, Read, Omit]` |
| `StateAction` | `To` | `[Arrow]` |
| `StateAction` | `From` | `[Arrow]` |
| `EventEnsure` | `On` | `[Ensure]` |
| `EventHandler` | `On` | `[Arrow]` |

**Derived index for the parser:**

```csharp
// On Constructs — built from All entries
public static FrozenDictionary<TokenKind, ImmutableArray<(ConstructMeta Meta, DisambiguationEntry Entry)>>
    ByLeadingToken { get; } = /* flatten all Entries across all Metas, group by LeadingToken */;
```

**Parser disambiguation — the generic method:**

```csharp
private Declaration ParseDisambiguated(
    TokenKind leadingToken,
    ImmutableArray<(ConstructMeta Meta, DisambiguationEntry Entry)> candidates)
{
    // Step 1: Parse the shared anchor (state target or event target).
    // All candidates under a shared leading token parse the same anchor type.
    var anchor = ParseAnchorTarget(leadingToken);

    // Step 2: Consume optional guard if present (the "when" intermediate).
    Expression? guard = null;
    if (Current().Kind == TokenKind.When)
    {
        Advance(); // consume 'when'
        guard = ParseExpression(0);
    }

    // Step 3: Look at the current token and match against disambiguation tokens.
    var currentToken = Current().Kind;
    foreach (var (meta, entry) in candidates)
    {
        if (entry.DisambiguationTokens is null)
            continue; // skip unambiguous entries — they shouldn't be in a 1:N group

        if (entry.DisambiguationTokens.Value.Contains(currentToken))
            return ParseConstructBody(meta, anchor, guard);
    }

    // Step 4: No match — emit diagnostic, produce error node.
    var expected = candidates
        .Where(c => c.Entry.DisambiguationTokens is not null)
        .SelectMany(c => c.Entry.DisambiguationTokens!.Value)
        .Distinct()
        .ToArray();
    EmitDiagnostic(DiagnosticCode.ExpectedOneOf, expected, Current());
    return CreateMissingNode(leadingToken, anchor);
}
```

**The `when` guard is handled generically.** Every preposition-scoped construct can have an optional `when` clause before the disambiguation token. This is not a per-construct special case — it's a uniform grammar rule for all scoped constructs. The generic disambiguator handles it once, in one place.

**Why this is right:**

1. Adding a new scoped construct (e.g., hypothetical `in <state> audit ...`) requires one catalog entry with `Entries: [(In, [Audit])]` — zero parser disambiguation code.
2. The `when` guard is handled uniformly — no per-construct `if (token == When) { ... }` code.
3. LS completions read `DisambiguationEntry.DisambiguationTokens` to suggest what comes after a state/event target — this is live metadata with a real consumer.
4. MCP vocabulary exposes the full disambiguation table — AI agents know exactly how to complete a partial construct.

**What I rejected before and why I'm un-rejecting it:**

I said the `when` guard "breaks the flat-metadata model." It doesn't. The guard is consumed generically as "optional `when` expression" before disambiguation lookup. This is uniform behavior, not a metadata encoding problem. My error was trying to encode guard consumption as catalog metadata; the right answer is to encode guard consumption as **generic parser behavior for all scoped constructs** and let the catalog handle only the token-based routing.

---

### Layer D: Slot-Driven Productions (THE CLEAN-SLATE APPROACH)

This is the layer the prior analysis walked back. Here's why that walkback was wrong for a clean-slate build.

**The prior concern:** "Rewriting 11 parse methods simultaneously creates a regression surface." True — for a rewrite. But there ARE no 11 parse methods today. The parser is `throw new NotImplementedException()`. We are writing the parser for the first time. The question is: do we write it with 11 per-construct methods, or do we write it with a generic slot iterator? The cost difference on a clean-slate build is modest. The architecture difference is permanent.

**The ActionChain concern:** George correctly identified that ActionChain is a loop, not a slot. My answer: **it's both.** From the construct's perspective, the action chain occupies one slot position. From the parser's perspective, the slot parser for `ActionChain` contains a loop. The slot metadata says "there's an ActionChain slot here." The slot parser for `ActionChain` knows how to parse the loop. These are not in conflict.

**The Outcome sub-grammar concern:** Outcome has 3 forms: `-> transition <state>`, `-> no transition`, `-> reject "message"`. The slot parser for `Outcome` handles this internally. The generic iterator doesn't see the 3 forms — it sees "there's an Outcome slot here" and calls `ParseOutcome()`, which handles the 3 forms. Again, not in conflict.

**The BuildNode concern:** The factory that maps slot arrays to typed AST records. This is real work — but it's the SAME work whether the parser is generic or per-construct. In the per-construct model, you write `new FieldDeclarationNode(span, names, type, modifiers, computed)`. In the generic model, you write `new FieldDeclarationNode(span, (Names)slots[0], (TypeRef)slots[1], (Modifiers)slots[2], (Computed)slots[3])`. Same code. Different call site.

**The architecture:**

```csharp
private Declaration ParseConstruct(ConstructMeta meta)
{
    var startSpan = Current().Span;
    var slots = new SyntaxNode?[meta.Slots.Count];

    for (int i = 0; i < meta.Slots.Count; i++)
    {
        var slot = meta.Slots[i];
        var node = _slotParsers[slot.Kind]();

        if (node is null && slot.IsRequired)
        {
            EmitDiagnostic(DiagnosticCode.ExpectedSlot, slot.Kind, meta.Kind);
            node = CreateMissingSlotNode(slot.Kind);
        }

        slots[i] = node;
    }

    var endSpan = PreviousToken().Span;
    return _nodeFactories[meta.Kind](SourceSpan.Covering(startSpan, endSpan), slots);
}
```

**The slot parser registry (parser-internal, NOT in the catalog):**

```csharp
private readonly FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> _slotParsers;

// Built at ParseSession construction
_slotParsers = new Dictionary<ConstructSlotKind, Func<SyntaxNode?>>
{
    [ConstructSlotKind.IdentifierList]    = ParseIdentifierList,
    [ConstructSlotKind.TypeExpression]    = ParseTypeExpression,
    [ConstructSlotKind.ModifierList]      = ParseFieldModifiers,
    [ConstructSlotKind.StateModifierList] = ParseStateModifiers,
    [ConstructSlotKind.ArgumentList]      = ParseArgumentList,
    [ConstructSlotKind.ComputeExpression] = ParseComputeExpression,
    [ConstructSlotKind.GuardClause]       = ParseGuardClause,
    [ConstructSlotKind.ActionChain]       = ParseActionChain,     // contains internal loop
    [ConstructSlotKind.Outcome]           = ParseOutcome,         // contains 3-form dispatch
    [ConstructSlotKind.StateTarget]       = ParseStateTarget,
    [ConstructSlotKind.EventTarget]       = ParseEventTarget,
    [ConstructSlotKind.EnsureClause]      = ParseEnsureClause,
    [ConstructSlotKind.BecauseClause]     = ParseBecauseClause,
    [ConstructSlotKind.AccessModeKeyword] = ParseAccessModeKeyword,
    [ConstructSlotKind.FieldTarget]       = ParseFieldTarget,
}.ToFrozenDictionary();
```

**The node factory registry (parser-internal, NOT in the catalog):**

```csharp
private readonly FrozenDictionary<ConstructKind, Func<SourceSpan, SyntaxNode?[], Declaration>>
    _nodeFactories;

// Built at ParseSession construction
_nodeFactories = new Dictionary<ConstructKind, Func<SourceSpan, SyntaxNode?[], Declaration>>
{
    [ConstructKind.PreceptHeader] = (span, slots) =>
        new PreceptHeaderNode(span, (Token)slots[0]!),

    [ConstructKind.FieldDeclaration] = (span, slots) =>
        new FieldDeclarationNode(span,
            ((IdentifierListNode)slots[0]!).Names,
            (TypeRefNode)slots[1]!,
            ((ModifierListNode?)slots[2])?.Modifiers ?? [],
            (Expression?)slots[3]),

    [ConstructKind.StateDeclaration] = (span, slots) =>
        new StateDeclarationNode(span,
            ((IdentifierListNode)slots[0]!).Names,
            ((StateModifierListNode?)slots[1])?.Modifiers ?? []),

    [ConstructKind.TransitionRow] = (span, slots) =>
        new TransitionRowNode(span,
            (StateTargetNode)slots[0]!,
            (EventTargetNode)slots[1]!,
            (Expression?)slots[2],        // guard
            (ActionChainNode?)slots[3],
            (OutcomeNode)slots[4]!),

    // ... remaining 7 constructs follow the same pattern
}.ToFrozenDictionary();
```

**Why a factory dictionary instead of a switch?**

I previously ruled that an exhaustive switch with CS8509 enforcement was better. I still think the switch is cleaner for the `BuildNode` pattern. But there's a subtlety: the factory dictionary can be **tested independently.** You can write a test that verifies, for each `ConstructKind`, that a factory entry exists and that its slot expectations match `ConstructMeta.Slots.Count`. The switch gives you CS8509 (missing-arm enforcement at compile time); the dictionary gives you a testable registration contract. Both are valid. For the full-vision design, I'll go with the dictionary because it composes better with the generic `ParseConstruct` — and I'll mandate a test that validates factory completeness and slot-count alignment.

**The factory completeness test:**

```csharp
[Fact]
public void AllConstructKinds_HaveFactoryRegistration()
{
    foreach (var meta in Constructs.All)
    {
        Assert.True(_nodeFactories.ContainsKey(meta.Kind),
            $"Missing factory for {meta.Kind}");
    }
}

[Theory]
[MemberData(nameof(AllConstructKinds))]
public void FactorySlotCount_MatchesCatalogSlotCount(ConstructKind kind)
{
    var meta = Constructs.GetMeta(kind);
    // Parse a minimal valid example and verify the factory receives
    // exactly meta.Slots.Count entries
}
```

**Migration path:** There is no migration. The parser is a stub. We write it this way from the start.

---

### Layer E: Error Recovery (Unchanged from Prior Analysis)

```csharp
private static readonly FrozenSet<TokenKind> SyncPoints =
    Constructs.All
        .SelectMany(c => c.Entries.Select(e => e.LeadingToken))
        .Append(TokenKind.EndOfSource)
        .ToFrozenSet();
```

Trivially derived. Definitionally correct. Zero discussion needed.

---

### Layer F: Anchor Type Derivation (NEW)

**The insight:** Every preposition-scoped construct starts with either a state target or an event target. This is currently implicit in the per-construct disambiguation methods — `ParseInScoped` knows to call `ParseStateTarget`, `ParseOnScoped` knows to call `ParseEventTarget`. But this IS language structure. The anchor type for each leading token is deterministic:

| Leading Token | Anchor Parse Method |
|---------------|-------------------|
| `In` | `ParseStateTarget()` |
| `To` | `ParseStateTarget()` |
| `From` | `ParseStateTarget()` |
| `On` | `ParseEventTarget()` |

This can be derived from catalog metadata. Every construct under `In`/`To`/`From` has `SlotStateTarget` as its first slot. Every construct under `On` has `SlotEventTarget` as its first slot. The generic disambiguator can read the first slot of any candidate to determine the anchor type:

```csharp
private SyntaxNode ParseAnchorTarget(
    ImmutableArray<(ConstructMeta Meta, DisambiguationEntry Entry)> candidates)
{
    // All candidates under the same leading token share the same first slot kind
    var anchorSlotKind = candidates[0].Meta.Slots[0].Kind;
    return _slotParsers[anchorSlotKind]();
}
```

No new metadata needed — the slot sequence already encodes this. The generic disambiguator reads it.

---

## 3. What This Requires of the Catalog

### ConstructMeta — Before

```csharp
public sealed record ConstructMeta(
    ConstructKind                Kind,
    string                       Name,
    string                       Description,
    string                       UsageExample,
    ConstructKind[]              AllowedIn,
    IReadOnlyList<ConstructSlot> Slots,
    TokenKind                    LeadingToken,
    string?                      SnippetTemplate = null);
```

### ConstructMeta — After

```csharp
public sealed record ConstructMeta(
    ConstructKind                        Kind,
    string                               Name,
    string                               Description,
    string                               UsageExample,
    ConstructKind[]                      AllowedIn,
    IReadOnlyList<ConstructSlot>         Slots,
    ImmutableArray<DisambiguationEntry>  Entries,    // ← replaces LeadingToken
    string?                              SnippetTemplate = null);
```

### New Type: DisambiguationEntry

```csharp
/// <summary>
/// Describes one way a construct can be entered from its leading token.
/// Constructs with multiple leading tokens (e.g., StateEnsure: In/To/From)
/// have multiple entries. Constructs with one leading token have one entry.
/// </summary>
public sealed record DisambiguationEntry(
    TokenKind                      LeadingToken,
    ImmutableArray<TokenKind>?     DisambiguationTokens = null);
```

### Constructs.cs — New Derived Indexes

```csharp
/// <summary>
/// All (Meta, Entry) pairs grouped by leading token. The parser's dispatch table.
/// </summary>
public static FrozenDictionary<TokenKind, ImmutableArray<(ConstructMeta Meta, DisambiguationEntry Entry)>>
    ByLeadingToken { get; } = All
        .SelectMany(m => m.Entries.Select(e => (Meta: m, Entry: e)))
        .GroupBy(x => x.Entry.LeadingToken)
        .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());

/// <summary>
/// Unique set of leading tokens — the parser's sync-point set.
/// </summary>
public static FrozenSet<TokenKind> LeadingTokens { get; } =
    All.SelectMany(m => m.Entries.Select(e => e.LeadingToken))
       .ToFrozenSet();
```

### ConstructSlot — No Changes

The existing `ConstructSlot(Kind, IsRequired, Description)` is sufficient. No new fields.

### GetMeta — Updated Example Entries

```csharp
ConstructKind.StateEnsure => new(
    kind,
    "state ensure",
    "State-scoped constraint ...",
    "in Approved ensure amount > 0 because \"...\"",
    [ConstructKind.StateDeclaration],
    [SlotStateTarget, SlotEnsureClause],
    [                                                    // ← Entries
        new(TokenKind.In,   [TokenKind.Ensure]),
        new(TokenKind.To,   [TokenKind.Ensure]),
        new(TokenKind.From, [TokenKind.Ensure]),
    ]),

ConstructKind.AccessMode => new(
    kind,
    "access mode",
    "Declares field write access ...",
    "in Draft write Amount",
    [],
    [SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget],
    [
        new(TokenKind.Write),                            // root-level: unambiguous
        new(TokenKind.In, [TokenKind.Write, TokenKind.Read, TokenKind.Omit]),
    ]),

ConstructKind.TransitionRow => new(
    kind,
    "transition row",
    "State-to-state transition ...",
    "from Draft on Submit -> ... -> transition Submitted",
    [],
    [SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotOutcome],
    [new(TokenKind.From, [TokenKind.On])]),
```

---

## 4. What the Generic Parser Looks Like

### Complete `ParseSession` Sketch

```csharp
private struct ParseSession
{
    private readonly TokenStream _tokens;
    private int _position;
    private ImmutableArray<Declaration>.Builder _declarations;
    private ImmutableArray<Diagnostic>.Builder _diagnostics;

    // ═══════════════════════════════════════════════════════════════
    //  Catalog-derived tables — built once at construction
    // ═══════════════════════════════════════════════════════════════
    private readonly FrozenDictionary<TokenKind, OperatorMeta> _operators;
    private readonly FrozenSet<TokenKind> _typeKeywords;
    private readonly FrozenSet<TokenKind> _fieldModifiers;
    private readonly FrozenSet<TokenKind> _stateModifiers;
    private readonly FrozenSet<TokenKind> _actionKeywords;
    private readonly FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> _slotParsers;
    private readonly FrozenDictionary<ConstructKind, Func<SourceSpan, SyntaxNode?[], Declaration>> _nodeFactories;

    // ═══════════════════════════════════════════════════════════════
    //  Top-level dispatch — fully catalog-driven
    // ═══════════════════════════════════════════════════════════════
    public void ParseAll()
    {
        ParseHeader();

        while (Current().Kind != TokenKind.EndOfSource)
        {
            SkipTrivia();
            if (Current().Kind == TokenKind.EndOfSource) break;

            var token = Current().Kind;

            if (!Constructs.ByLeadingToken.TryGetValue(token, out var candidates))
            {
                EmitDiagnostic(DiagnosticCode.UnexpectedToken, Current());
                SyncToNextDeclaration();
                continue;
            }

            var leadingSpan = Current().Span;
            Advance(); // consume the leading token

            Declaration node;
            if (candidates.Length == 1 && candidates[0].Entry.DisambiguationTokens is null)
            {
                // Unambiguous 1:1 dispatch
                node = ParseConstructSlots(candidates[0].Meta, leadingSpan);
            }
            else
            {
                // Disambiguation required
                node = ParseDisambiguated(leadingSpan, candidates);
            }

            _declarations.Add(node);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Generic disambiguation — replaces 4 hand-written methods
    // ═══════════════════════════════════════════════════════════════
    private Declaration ParseDisambiguated(
        SourceSpan leadingSpan,
        ImmutableArray<(ConstructMeta Meta, DisambiguationEntry Entry)> candidates)
    {
        // Step 1: Parse shared anchor — derived from first slot of any candidate
        var anchorKind = candidates[0].Meta.Slots[0].Kind;
        var anchor = _slotParsers[anchorKind]();

        // Step 2: Consume optional guard (uniform for all scoped constructs)
        Expression? guard = null;
        if (Current().Kind == TokenKind.When)
        {
            Advance();
            guard = ParseExpression(0);
        }

        // Step 3: Match disambiguation token
        var disambigToken = Current().Kind;
        foreach (var (meta, entry) in candidates)
        {
            if (entry.DisambiguationTokens is null) continue;
            if (entry.DisambiguationTokens.Value.Contains(disambigToken))
            {
                // Found the match — parse remaining slots (skip the anchor slot,
                // inject the pre-parsed guard if the construct has a GuardClause slot)
                return ParseConstructBodyWithPreParsedSlots(
                    meta, leadingSpan, anchor, guard);
            }
        }

        // No match among disambiguation candidates — check for unambiguous fallback
        // (e.g., root-level Write entering AccessMode through the 1:N group)
        foreach (var (meta, entry) in candidates)
        {
            if (entry.DisambiguationTokens is null)
                return ParseConstructBodyWithPreParsedSlots(
                    meta, leadingSpan, anchor, guard);
        }

        // Total failure
        EmitDiagnostic(DiagnosticCode.AmbiguousConstruct, leadingSpan);
        return CreateMissingDeclaration(leadingSpan);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Generic slot-driven production — replaces 11 per-construct methods
    // ═══════════════════════════════════════════════════════════════
    private Declaration ParseConstructSlots(ConstructMeta meta, SourceSpan leadingSpan)
    {
        var slots = new SyntaxNode?[meta.Slots.Count];
        for (int i = 0; i < meta.Slots.Count; i++)
        {
            var slot = meta.Slots[i];
            slots[i] = _slotParsers[slot.Kind]();

            if (slots[i] is null && slot.IsRequired)
            {
                EmitDiagnostic(DiagnosticCode.ExpectedSlot, slot.Kind, meta.Kind);
                slots[i] = CreateMissingSlotNode(slot.Kind);
            }
        }

        var endSpan = PreviousToken().Span;
        return _nodeFactories[meta.Kind](
            SourceSpan.Covering(leadingSpan, endSpan), slots);
    }

    private Declaration ParseConstructBodyWithPreParsedSlots(
        ConstructMeta meta, SourceSpan leadingSpan,
        SyntaxNode anchor, Expression? guard)
    {
        var slots = new SyntaxNode?[meta.Slots.Count];

        for (int i = 0; i < meta.Slots.Count; i++)
        {
            var slot = meta.Slots[i];

            // Inject pre-parsed anchor and guard
            if (i == 0)
            {
                slots[i] = anchor; // anchor target (state or event)
                continue;
            }
            if (slot.Kind == ConstructSlotKind.GuardClause && guard is not null)
            {
                slots[i] = guard;
                continue;
            }

            // Parse remaining slots normally
            slots[i] = _slotParsers[slot.Kind]();

            if (slots[i] is null && slot.IsRequired)
            {
                EmitDiagnostic(DiagnosticCode.ExpectedSlot, slot.Kind, meta.Kind);
                slots[i] = CreateMissingSlotNode(slot.Kind);
            }
        }

        var endSpan = PreviousToken().Span;
        return _nodeFactories[meta.Kind](
            SourceSpan.Covering(leadingSpan, endSpan), slots);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sync-point recovery — catalog-derived
    // ═══════════════════════════════════════════════════════════════
    private static readonly FrozenSet<TokenKind> SyncTokens = Constructs.LeadingTokens;

    private void SyncToNextDeclaration()
    {
        while (!SyncTokens.Contains(Current().Kind) && Current().Kind != TokenKind.EndOfSource)
            Advance();
    }
}
```

### How Slot Parsing Works for ActionChain (The Hard Case)

```csharp
private ActionChainNode? ParseActionChain()
{
    if (Current().Kind != TokenKind.Arrow) return null;

    var actions = ImmutableArray.CreateBuilder<ActionStatementNode>();

    while (Current().Kind == TokenKind.Arrow)
    {
        Advance(); // consume '->'

        // Check for outcome keywords — this is where ActionChain stops
        if (IsOutcomeKeyword(Current().Kind))
            break; // leave the token for ParseOutcome

        // Parse the action: keyword + target + optional value
        var actionToken = Current();
        if (!_actionKeywords.Contains(actionToken.Kind))
        {
            EmitDiagnostic(DiagnosticCode.ExpectedAction, actionToken);
            break;
        }

        Advance(); // consume action keyword
        var action = ParseActionStatement(actionToken);
        actions.Add(action);
    }

    return actions.Count > 0
        ? new ActionChainNode(SourceSpan.Covering(actions[0].Span, actions[^1].Span), actions.ToImmutable())
        : null;
}

private bool IsOutcomeKeyword(TokenKind kind)
    => kind == TokenKind.Transition || kind == TokenKind.No || kind == TokenKind.Reject;
```

**Key point:** `ParseActionChain()` owns its loop. The generic iterator calls it once as a slot parser. It handles its own loop termination. The slot metadata says "ActionChain is here, it's optional." The slot parser knows the grammar. This is the right separation — the catalog says WHAT, the parser says HOW.

### How Outcome Parsing Works (The 3-Form Case)

```csharp
private OutcomeNode? ParseOutcome()
{
    if (Current().Kind != TokenKind.Arrow) return null;

    Advance(); // consume '->'

    return Current().Kind switch
    {
        TokenKind.Transition => ParseTransitionOutcome(),
        TokenKind.No         => ParseNoTransitionOutcome(),
        TokenKind.Reject     => ParseRejectOutcome(),
        _ => null  // not an outcome — might be an error, handled by caller
    };
}
```

Same pattern. The slot parser owns the sub-grammar. The generic iterator doesn't see it.

---

## 5. The Outside-the-Box Option: Source Generation

Shane's provocation included source generation. Here's the honest evaluation.

### What a Roslyn Source Generator Could Do

A source generator reads `Constructs.All` at compile time and emits:
1. The `_slotParsers` dictionary initialization
2. The `_nodeFactories` dictionary initialization
3. Validation tests (slot count alignment, factory completeness)
4. Possibly the `ParseConstructSlots` loop itself (one generated method per construct that calls slot parsers in order)

### Why It's Tempting

- **Zero drift.** Generated code is definitionally aligned with the catalog.
- **Type safety.** The generator can emit strongly typed factory calls instead of `SyntaxNode?[]` casts.
- **No runtime cost.** Everything resolves at compile time.

### Why I Don't Recommend It (Yet)

1. **The parser is 11 constructs.** Source generation infrastructure (generator project, attributes, incremental pipeline, debugging story) costs more than the code it replaces. The break-even point for generator ROI is around 30-50 constructs.

2. **Debugging generated code is painful.** When the parser produces a wrong AST, you want to step through `ParseConstruct` and see what happened. Stepping through generated code requires understanding the generator's output, which is one more indirection layer.

3. **The factory dictionary achieves 95% of the benefit.** Catalog-derived, testable, explicit. The 5% you lose (compile-time type safety on slot casts) is recoverable via the factory completeness test suite.

4. **We don't have source generators in the project yet.** Adding the first generator is an infrastructure investment. That investment is justified when generators serve multiple consumers (analyzers already serve multiple rules). For a single consumer (the parser), the investment is premature.

### When Source Generation Becomes Right

If Precept's construct count grows past ~25-30, or if the factory-slot-alignment tests become a maintenance burden, or if a second pipeline stage needs the same kind of catalog-to-code derivation, then source generation becomes the right call. The design in this document is **generator-ready** — the factory dictionary could be replaced by generated code without changing the parser's architecture. The `ParseConstruct`/`ParseDisambiguated` methods don't care whether the factory dictionary was built by hand or by a generator.

**Verdict: Not now. The design is generator-ready for later. The factory dictionary plus test suite is the right approach at 11 constructs.**

---

## 6. What This Achieves vs. the Prior Analysis

### The Prior "70% Catalog-Driven" Claim

The prior analysis claimed 70% catalog-driven coverage with the remaining 30% being grammar mechanics. Here's the updated accounting:

| Concern | Prior Analysis | This Design |
|---------|---------------|-------------|
| Vocabulary tables | Catalog-driven ✓ | Catalog-driven ✓ |
| Dispatch table | Catalog-derived index, hand-written switch | **Fully catalog-driven** — dictionary lookup |
| Disambiguation | Hand-written 4 methods (60 lines) | **Catalog-driven** — generic disambiguator reads `DisambiguationEntry` |
| Slot iteration | Hand-written 11 per-construct methods | **Catalog-driven** — generic `ParseConstructSlots` reads `meta.Slots` |
| Expression parsing | Hand-written Pratt with catalog-derived BP | Same — unchanged |
| Slot-level parse methods | Hand-written (15 methods) | Same — unchanged |
| AST node construction | Hand-written switch/dictionary | **Parser-internal factory dictionary** — still hand-written, but testably aligned with catalog |
| Error recovery sync | Catalog-derived | Catalog-derived |

**Updated coverage: ~85-90% catalog-driven.** The remaining 10-15% is:
- Pratt expression loop mechanics
- 15 slot parser method bodies (grammar mechanics per slot kind)
- Node factory lambdas (per-construct field mapping)
- `ParseActionChain` loop structure and `ParseOutcome` sub-grammar dispatch

Everything that IS language vocabulary or structure is catalog-derived. Everything that IS grammar mechanics (how to parse expressions, how action chains loop, how outcomes branch) remains hand-written. That's the right boundary.

---

## 7. Migration Path

There is no migration. The parser is a stub. We write it catalog-driven from the start.

**Build order:**

1. **Add `DisambiguationEntry` and update `ConstructMeta`.** This is the only catalog change. Update all 11 `GetMeta` entries. Add derived indexes.
2. **Build vocabulary tables in `ParseSession`.** Layer A — already designed.
3. **Build the dispatch loop.** `ParseAll()` with catalog-driven dispatch and generic disambiguation.
4. **Build slot parsers one at a time.** Start with the simple ones (`ParseIdentifierList`, `ParseStateTarget`, `ParseEventTarget`). Add `ParseTypeExpression`, `ParseFieldModifiers`. Then the complex ones (`ParseExpression`, `ParseActionChain`, `ParseOutcome`).
5. **Build node factories.** One per construct. Tested for slot-count alignment.
6. **Wire `ParseConstructSlots` and `ParseConstructBodyWithPreParsedSlots`.** The generic iterator that ties slots to constructs.
7. **Integration tests.** Parse every sample file in `samples/`. Verify AST structure.

Each step is independently testable. No big-bang moment.

---

## For George

### Assumptions I Want Challenged

1. **`DisambiguationEntry` replaces `LeadingToken` entirely.** I'm proposing that `ConstructMeta` no longer has a single `LeadingToken` field — it has `Entries` (plural). This is a breaking change to the catalog's public shape. Every consumer that reads `LeadingToken` must change. Is the breakage worth it? Would a computed `PrimaryLeadingToken` convenience property ease the transition for consumers that only need one?

2. **The `when` guard is uniform across all scoped constructs.** I'm treating "optional `when` before disambiguation" as a generic pattern. Is there a scoped construct where this doesn't hold? Is there a construct where the guard appears AFTER the disambiguation token? If so, the generic disambiguator breaks.

3. **Slot parsers returning `SyntaxNode?` is sufficient.** The factory receives `SyntaxNode?[]` and casts. George flagged this fragility before. I'm mitigating with a test suite. Is the test suite sufficient, or is there a type-safe alternative that doesn't require source generation?

4. **The factory dictionary vs. exhaustive switch.** I chose the dictionary for testability. George preferred the switch for CS8509. Is there a hybrid — a switch INSIDE a method that's registered in a dictionary? Or does the test suite make CS8509 redundant?

5. **`ParseConstructBodyWithPreParsedSlots` — the anchor/guard injection.** I'm pre-parsing the anchor and guard in the disambiguator, then injecting them into the slot array at known positions (slot 0 for anchor, first GuardClause slot for guard). This creates a coupling between the disambiguator and the slot ordering. Is this fragile? Is there a cleaner injection pattern?

6. **Scope of `DisambiguationEntry.DisambiguationTokens`.** I have `AccessMode` with `[Write, Read, Omit]` as three disambiguation tokens under `In`. But these are also slot values (the `AccessModeKeyword` slot). Is there a way to derive the disambiguation tokens FROM the slot's vocabulary instead of declaring them separately? That would prevent drift between the disambiguation table and the slot parser.

7. **Scale concern revisited.** George's original estimate for Layer D was 4-5 weeks. My design eliminates the ConstructSlot metadata additions (~24h per George) and the slot metadata design work (~8h). But the generic iterator, factory dictionary, slot parser boundary contracts, and regression suite remain. What's George's honest re-estimate for this design on a clean-slate build?

### Design Choices I'm Least Confident About

- **`Entries` replacing `LeadingToken`.** This might be over-engineering the catalog for a parser concern. Maybe `LeadingTokens: ImmutableArray<TokenKind>` (just the tokens, no disambiguation) plus a separate `Disambiguation` field is cleaner.
- **The generic disambiguator handling the `when` guard.** If any future construct breaks the "guard before disambiguation" pattern, the generic method needs a special case — and then we're back to per-construct disambiguation code.
- **Factory dictionary over switch.** I'm trading compile-time exhaustiveness for runtime testability. That's a real tradeoff.

**George: break what you can. I want the strongest version of this design, not the version I happen to have written first.**
