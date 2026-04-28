# Catalog-Driven Parser: Architectural Scope Document

**By:** Frank
**Date:** 2026-04-27
**Status:** Scope complete — ready for estimation

## Purpose

Shane asked: *"What would it look like if the parser were TRULY catalog-driven?"* This document scopes five layers where catalog metadata could drive parser behavior, evaluates each honestly, draws the architectural boundary, and specifies the concrete catalog changes for the layers worth pursuing.

---

## 1. The Layers of "Catalog-Driven"

### Layer A: Vocabulary Tables

**What it is.** Recognition sets inside parse methods — "is this token a type keyword?", "is this token a modifier?", "what's this operator's precedence?" — derived from catalog frozen dictionaries instead of hardcoded lists.

**Catalog changes needed.** None. The metadata already exists:
- `Operators.All` → `OperatorMeta.Precedence`, `Associativity`, `Arity`, `Token`
- `Types.All` → `TypeMeta.Token`
- `Modifiers.All` → `ModifierMeta` subtypes with `Token`
- `Actions.All` → `ActionMeta.Token`

**Parser shape.** At `ParseSession` construction, build frozen lookup tables:

```csharp
// Built once per ParseSession from catalog metadata
private readonly FrozenDictionary<TokenKind, OperatorMeta> _operatorsByToken;
private readonly FrozenSet<TokenKind> _typeKeywords;
private readonly FrozenSet<TokenKind> _fieldModifierKeywords;
private readonly FrozenSet<TokenKind> _stateModifierKeywords;
private readonly FrozenSet<TokenKind> _actionKeywords;
```

Each is derived from the owning catalog's `All` property filtered/projected as needed. Parse methods use `_typeKeywords.Contains(Current().Kind)` instead of a hand-coded `case` list.

**What it buys.**
- Add a new type/modifier/action/operator to its catalog → parser recognizes it automatically, zero parser edits.
- Tooling (completions, grammar gen, MCP) and parser share the same source of truth — no drift.
- Eliminates ~40–50% of vocabulary-as-domain-knowledge from parser code.

**Complexity added.** Negligible. This is dictionary construction at startup. The pattern is already established in the lexer.

**Verdict: PURSUE. This is the unambiguous win — already identified, already designed, just needs implementation.**

---

### Layer B: Dispatch Table

**What it is.** The top-level `ParseAll()` loop that switches on the current token to select a parse production. Can `ConstructMeta.LeadingToken` drive a `FrozenDictionary<TokenKind, Action<ParseSession>>` dispatch instead of a hand-written switch?

**The 1:N problem — but it's solvable.** Four tokens (`In`, `To`, `From`, `On`) each map to 2–3 `ConstructKind`s. My prior analysis stopped here: "a catalog lookup cannot select the production." But let's push further.

The dispatch table has two tiers:
1. **Tier 1 — 1:1 tokens** (`Field`, `State`, `Event`, `Rule`, `Write`, `Precept`): 6 tokens that map to exactly one `ConstructKind`. A frozen dictionary lookup is trivially correct.
2. **Tier 2 — 1:N preposition tokens** (`In`, `To`, `From`, `On`): 4 tokens that require disambiguation.

For Tier 1, a catalog-derived dispatch is clean:

```csharp
// Built from Constructs.All, filtered to 1:1 LeadingToken mappings
private readonly FrozenDictionary<TokenKind, ConstructKind> _directDispatch;
```

For Tier 2, the dispatch entry calls a disambiguation method — not a production directly. The *existence* of the disambiguation entry is still catalog-derived (the parser knows `In` is a valid leading token because the catalog says so), but the *disambiguation logic* remains hand-written.

**Catalog changes needed.** Minor: add a computed property or index to `Constructs`:

```csharp
// New on Constructs (static class)
public static FrozenDictionary<TokenKind, ImmutableArray<ConstructMeta>> ByLeadingToken { get; }
    = All.GroupBy(c => c.LeadingToken)
         .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
```

The parser uses `ByLeadingToken` at startup:
- If the array has length 1 → direct dispatch to production.
- If the array has length >1 → hand-written disambiguation method.

This means the parser's switch statement is replaced by a dictionary lookup + a small disambiguation switch for the 4 preposition cases. Adding a new 1:1 construct to the catalog (a hypothetical `import` keyword, for example) would require zero parser dispatch changes — the dictionary would pick it up.

**What it buys.**
- New 1:1 constructs automatically enter the dispatch table — no parser switch edit.
- The set of valid leading tokens is catalog-derived — error recovery (sync points) derives from the same source.
- The parser switch reduces from 9+ cases to a dictionary lookup + 4 disambiguation methods.

**Complexity added.** Low. The disambiguation methods already exist as `ParseInScoped()`, `ParseToScoped()`, `ParseFromScoped()`, `ParseOnScoped()`. They don't change. What changes is how they're reached.

**Verdict: PURSUE. The 1:N problem doesn't invalidate catalog dispatch — it just means disambiguation stays hand-written while the dispatch table itself becomes catalog-derived. This is a clean separation.**

---

### Layer C: Disambiguation via Catalog Context

**What it is.** Can the catalog carry enough context metadata to eliminate the hand-written disambiguation methods? For example:
- `ConstructMeta.DisambiguationToken` — "after the state target, if the next token is `Ensure`, this is a `StateEnsure`"
- `ConstructMeta.ScopeContext` — "this construct only appears when the preceding anchor is a state-ref (not an event-ref)"

**What the catalog would need.** A disambiguation rule per construct, something like:

```csharp
public sealed record DisambiguationRule(
    TokenKind? RequiredFollowToken,     // e.g., Ensure, Arrow, On
    ConstructSlotKind? PrecedingSlot,   // e.g., StateTarget, EventTarget
    int LookaheadDepth);               // how many tokens past the anchor
```

Then the parser's `ParseInScoped()` becomes generic:

```csharp
// Pseudocode
var candidates = ByLeadingToken[TokenKind.In];
var target = ParseStateTarget();
var followToken = Current().Kind;
var match = candidates.FirstOrDefault(c => c.Disambiguation.RequiredFollowToken == followToken);
CallProduction(match);
```

**What it buys.**
- The 4 disambiguation methods (`ParseInScoped`, `ParseToScoped`, `ParseFromScoped`, `ParseOnScoped`) collapse into a single generic `ParseScopedConstruct()`.
- Adding a new scoped construct (e.g., a hypothetical `in <state> audit ...`) would require only a catalog entry with `LeadingToken = In, Disambiguation = { RequiredFollowToken = Audit }` — no new disambiguation method.

**What it costs.**
- The `when` guard lookahead doesn't fit this model cleanly. In `ParseFromScoped()`, if the token after the state target is `When`, the parser consumes the guard expression, then re-inspects the *next* token. That's a two-step lookahead with an intermediate parse action. A flat `RequiredFollowToken` can't express "consume this expression first, then check."
- The `From` case has three candidates and a `When` re-dispatch. Encoding this as metadata means inventing a mini-language for disambiguation rules — which is a parser-in-the-catalog.
- The disambiguation logic is small (4 methods, each ~15 lines). The generic replacement would be similar in size but harder to debug because the dispatch path is now indirect.
- This layer benefits only when new scoped constructs are added under existing prepositions. In Precept's stable grammar, that's rare.

**Verdict: REJECT. The `when` guard re-dispatch breaks the flat-metadata model. The 4 disambiguation methods are small, stable, and readable. The generic replacement adds indirection without removing meaningful complexity. This is complexity for its own sake — it buys automation for a change that almost never happens.**

---

### Layer D: Grammar Productions from Slot Metadata

**What it is.** Instead of hand-writing `ParseFieldDeclaration()` with its specific sequence of `ParseIdentifierList()`, `ParseTypeRef()`, `ParseModifierList()`, `ParseComputeExpression()` — what if the parser read `ConstructMeta.Slots` and generically iterated the slot sequence?

```csharp
// Pseudocode for a generic production
void ParseConstruct(ConstructMeta meta)
{
    foreach (var slot in meta.Slots)
    {
        var node = slot.Kind switch
        {
            ConstructSlotKind.IdentifierList    => ParseIdentifierList(),
            ConstructSlotKind.TypeExpression    => ParseTypeExpression(),
            ConstructSlotKind.ModifierList      => ParseModifierList(),
            ConstructSlotKind.GuardClause       => ParseGuardClause(),
            ConstructSlotKind.ActionChain       => ParseActionChain(),
            ConstructSlotKind.Outcome           => ParseOutcome(),
            ConstructSlotKind.StateTarget       => ParseStateTarget(),
            ConstructSlotKind.EventTarget       => ParseEventTarget(),
            ConstructSlotKind.EnsureClause      => ParseEnsureClause(),
            ConstructSlotKind.BecauseClause     => ParseBecauseClause(),
            ConstructSlotKind.AccessModeKeyword => ParseAccessModeKeyword(),
            ConstructSlotKind.FieldTarget       => ParseFieldTarget(),
            ConstructSlotKind.ComputeExpression => ParseComputeExpression(),
            ConstructSlotKind.ArgumentList      => ParseArgumentList(),
            ConstructSlotKind.StateModifierList => ParseStateModifierList(),
            _ => throw new InvalidOperationException($"Unknown slot: {slot.Kind}")
        };

        if (!slot.IsRequired && node is null)
            continue;
        if (slot.IsRequired && node is null)
            EmitDiagnostic(DiagnosticCode.MissingRequiredSlot, slot);
        builder.AddSlot(node);
    }
}
```

**What it buys.**
- **Per-construct parse methods disappear.** 11 constructs × ~20–40 lines each = ~250–400 lines of per-construct code replaced by ~40 lines of generic slot iteration.
- **Add a new construct to the catalog → it parses automatically.** No new `Parse___()` method. Define the slot sequence in `ConstructMeta`, add a `ConstructSlotKind` if needed, implement the slot parser if it's new — done.
- **Slot optionality is enforced uniformly.** No per-construct hardcoding of "this slot is required" vs. "this slot is optional." The `IsRequired` flag on `ConstructSlot` drives it.
- **Diagnostics for missing required slots auto-generate** from metadata — "expected TypeExpression in FieldDeclaration" comes from the slot's `Kind` name and the construct's `Name`.

**What it costs.**
- **Slot parsers still need hand-written implementations.** The switch from `ConstructSlotKind` to a parse method is a vocabulary table (catalog-driveable), but the parse methods themselves are grammar code. You still need `ParseGuardClause()`, `ParseActionChain()`, etc.
- **The AST node hierarchy complicates this.** Currently, each construct maps to a specific record type (`FieldDeclarationNode`, `TransitionRowNode`) with named fields. A generic `ParseConstruct()` would produce a generic node with a slot array — losing the type-safety of named fields. The alternative is a two-pass approach: generic parse → construct-specific node factory. That adds indirection.
- **Edge cases in slot parsing.** `ParseActionChain()` has internal loop logic (consume `->`, check for outcome). `ParseGuardClause()` involves expression parsing with boundary tokens. These aren't pure "parse one thing" — they have grammar-specific control flow. The generic iterator handles them only if each slot parser is self-contained, which requires careful boundary-token contracts.
- **The `when` guard complication resurfaces.** In `TransitionRow`, the guard clause appears between the event target and the action chain. In `StateEnsure`, there is no action chain. The slot sequence handles this via optionality, but the guard's expression parser needs to know its termination tokens — and those differ by construct context (the guard in a transition stops at `->`, while the guard in an ensure stops at `ensure`... wait, the guard IS the ensure expression). These cross-slot dependencies mean the generic iterator needs context about what follows.

**Verdict: PURSUE WITH CONSTRAINTS. The generic slot iteration is the single biggest structural win in making the parser catalog-driven. But the AST node type-safety concern is real. The right approach:**

1. **Generic slot iteration drives parsing and validation** — the slot sequence is read from metadata.
2. **Per-construct AST node factories remain** — the slot array is mapped to named record fields in a construct-specific factory. This preserves downstream type safety.
3. **Slot parsers are self-contained** — each returns a `SyntaxNode?`. The generic iterator handles optionality and missing-node insertion. Slot-specific boundary tokens are part of the slot parser contract (the slot parser knows when to stop).
4. **The `ConstructSlotKind` → parse method table is itself a frozen dictionary** — adding a new slot kind means adding one slot parser and one dictionary entry.

This gives us: construct-level parsing is catalog-driven (slot sequence from metadata), slot-level parsing is hand-written (grammar mechanics), and AST nodes retain named fields (type safety).

---

### Layer E: Error Recovery from Catalog Metadata

**What it is.** Sync-point selection — when the parser encounters an error, which tokens does it scan forward to? Currently the parser doc says:

> "sync to the next known leading token (Field, State, Event, Rule, Write, In, To, From, On)"

These are exactly `ConstructMeta.LeadingToken` values. Can the sync set be catalog-derived?

**Catalog changes needed.** None. The sync set IS `Constructs.All.Select(c => c.LeadingToken).Distinct()` plus `EndOfSource`.

**Parser shape.**

```csharp
// Built once at ParseSession construction
private readonly FrozenSet<TokenKind> _syncTokens =
    Constructs.All.Select(c => c.LeadingToken).Distinct()
        .Append(TokenKind.EndOfSource)
        .ToFrozenSet();

void SyncToNextDeclaration()
{
    while (!_syncTokens.Contains(Current().Kind))
        Advance();
}
```

**What it buys.**
- Add a new construct with a new leading token → error recovery automatically syncs to it.
- The sync set never drifts from the construct inventory.
- The parser doc already says this is what should happen — making it literal code costs nothing.

**Complexity added.** Zero. This is a 4-line derivation.

**Verdict: PURSUE. This is trivial and definitionally correct.**

---

## 2. The Real Boundary Question

Given layers A–E, the RIGHT boundary for Precept is:

| Layer | Pursue? | Value | Complexity |
|-------|---------|-------|------------|
| **A: Vocabulary tables** | ✅ Yes | High — eliminates vocabulary drift, enables auto-recognition of new language members | Negligible |
| **B: Dispatch table** | ✅ Yes | Medium — auto-dispatch for new 1:1 constructs, catalog-derived sync set | Low |
| **D: Slot-driven productions** | ✅ Yes (with constraints) | High — eliminates per-construct parse methods, auto-parses new constructs from slot metadata | Medium |
| **E: Error recovery** | ✅ Yes | Low–Medium — trivial derivation, definitionally correct | Zero |
| **C: Disambiguation** | ❌ No | Low — saves 4 small stable methods that rarely change | Medium–High (inventing a disambiguation DSL) |

**The boundary:** The parser's **dispatch**, **recognition**, **slot iteration**, and **error recovery** are catalog-driven. The parser's **disambiguation logic**, **expression parsing mechanics**, **slot-level parse method implementations**, and **AST node construction** remain hand-written.

This gives us a parser where:
- Adding a new **type/modifier/action/operator** → zero parser changes (Layer A)
- Adding a new **1:1 construct** → zero parser dispatch changes, one AST node record, one node factory (Layers B + D)
- Adding a new **scoped construct** under an existing preposition → one disambiguation case + one AST node record + one node factory (Layer B + D + hand-written disambiguation edit)
- Adding a new **slot kind** → one slot parser method + one dictionary entry (Layer D)

That last case — new slot kind — is the rarest change in the language. New types and modifiers are the most common changes, and they're fully automated.

---

## 3. Concrete Catalog Changes

### 3.1 — `Constructs` Static Class: `ByLeadingToken` Index

**New member on `Constructs`:**

```csharp
/// <summary>
/// Constructs grouped by leading token. Used by the parser's dispatch table
/// and sync-point recovery set.
/// </summary>
public static FrozenDictionary<TokenKind, ImmutableArray<ConstructMeta>> ByLeadingToken { get; }
    = All.GroupBy(c => c.LeadingToken)
         .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());

/// <summary>
/// The set of tokens that start any construct — used for sync-point recovery.
/// </summary>
public static FrozenSet<TokenKind> LeadingTokens { get; }
    = All.Select(c => c.LeadingToken).ToFrozenSet();
```

### 3.2 — `ConstructSlotKind` → Parse Method Registry (Parser-Internal)

Not a catalog change — this is parser infrastructure. But it's the mechanism that makes Layer D work:

```csharp
// Inside ParseSession
private readonly FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> _slotParsers;

// Initialized in ParseSession constructor:
_slotParsers = new Dictionary<ConstructSlotKind, Func<SyntaxNode?>>
{
    [ConstructSlotKind.IdentifierList]    = ParseIdentifierList,
    [ConstructSlotKind.TypeExpression]    = ParseTypeExpression,
    [ConstructSlotKind.ModifierList]      = ParseModifierList,
    [ConstructSlotKind.StateModifierList] = ParseStateModifierList,
    [ConstructSlotKind.ArgumentList]      = ParseArgumentList,
    [ConstructSlotKind.ComputeExpression] = ParseComputeExpression,
    [ConstructSlotKind.GuardClause]       = ParseGuardClause,
    [ConstructSlotKind.ActionChain]       = ParseActionChain,
    [ConstructSlotKind.Outcome]           = ParseOutcome,
    [ConstructSlotKind.StateTarget]       = ParseStateTarget,
    [ConstructSlotKind.EventTarget]       = ParseEventTarget,
    [ConstructSlotKind.EnsureClause]      = ParseEnsureClause,
    [ConstructSlotKind.BecauseClause]     = ParseBecauseClause,
    [ConstructSlotKind.AccessModeKeyword] = ParseAccessModeKeyword,
    [ConstructSlotKind.FieldTarget]       = ParseFieldTarget,
}.ToFrozenDictionary();
```

### 3.3 — Vocabulary Frozen Dictionaries (Parser-Internal)

Not catalog changes — derived at parser initialization from existing catalog metadata:

```csharp
// Operator precedence/associativity from Operators.All
private readonly FrozenDictionary<TokenKind, OperatorMeta> _operatorsByToken
    = Operators.All.ToFrozenDictionary(o => o.Token.Kind);

// Type keyword recognition from Types.All
private readonly FrozenSet<TokenKind> _typeKeywords
    = Types.All.Where(t => t.Token is not null).Select(t => t.Token!.Kind).ToFrozenSet();

// Modifier keyword recognition from Modifiers.All
private readonly FrozenSet<TokenKind> _fieldModifierKeywords
    = Modifiers.All.OfType<FieldModifierMeta>().Select(m => m.Token.Kind).ToFrozenSet();

private readonly FrozenSet<TokenKind> _stateModifierKeywords
    = Modifiers.All.OfType<StateModifierMeta>().Select(m => m.Token.Kind).ToFrozenSet();

// Action keyword recognition from Actions.All
private readonly FrozenSet<TokenKind> _actionKeywords
    = Actions.All.Select(a => a.Token.Kind).ToFrozenSet();
```

### 3.4 — Generic Construct Node Factory (AST Infrastructure)

A new mechanism in `SyntaxNodes.cs` — construct-specific factories that map a generic slot array to typed record fields:

```csharp
// Each construct kind has a factory that constructs its specific AST node
// from the generic parsed-slot array. The factory IS per-construct code,
// but it's pure field-mapping — not parsing logic.
private static Declaration BuildNode(ConstructKind kind, SourceSpan span, SyntaxNode?[] slots) => kind switch
{
    ConstructKind.FieldDeclaration => new FieldDeclarationNode(span, ...slots mapped...),
    ConstructKind.StateDeclaration => new StateDeclarationNode(span, ...slots mapped...),
    // ... exhaustive
};
```

This is a new exhaustive switch — but it maps slots to named fields, not grammar structure. It will need a parser change when a new construct is added, but the change is trivially mechanical (map positional slots to named record fields), not grammatically complex.

### 3.5 — No Changes to `ConstructMeta` Shape

The existing `ConstructMeta` shape is sufficient:

```csharp
public sealed record ConstructMeta(
    ConstructKind                Kind,
    string                       Name,
    string                       Description,
    string                       UsageExample,
    ConstructKind[]              AllowedIn,
    IReadOnlyList<ConstructSlot> Slots,      // ← Layer D reads this
    TokenKind                    LeadingToken, // ← Layer B reads this
    string?                      SnippetTemplate = null);
```

No new fields needed on `ConstructMeta`. The slot sequence and leading token already carry the information the parser needs. The `ByLeadingToken` and `LeadingTokens` properties are derived indexes on the `Constructs` static class, not new metadata.

---

## 4. What This Buys

### Automatic propagation of language changes

| Change type | Parser edits required (current) | Parser edits required (after) |
|-------------|-------------------------------|-------------------------------|
| New type keyword | Add to type-keyword switch | **None** — derived from `Types.All` |
| New field modifier | Add to modifier-keyword switch | **None** — derived from `Modifiers.All` |
| New action verb | Add to action-keyword switch | **None** — derived from `Actions.All` |
| New operator | Add to precedence table + expression dispatch | **None** — derived from `Operators.All` |
| New 1:1 construct (unique leading token) | Add dispatch case + parse method | **Add AST node record + factory case** — dispatch is automatic |
| New scoped construct (shared preposition) | Add dispatch case + disambiguation case + parse method | **Add disambiguation case + AST node record + factory case** |
| New slot kind | N/A (add to per-construct parse methods) | **Add one slot parser method + one dictionary entry** |

### Tooling stays in sync

Grammar generation, LS completions, MCP vocabulary, and the parser all read from the same catalog entries. No parallel lists to drift.

### Diagnostics from metadata

Missing required slots emit diagnostics derived from `ConstructSlot.Kind` and `ConstructMeta.Name` — e.g., "Expected TypeExpression in field declaration." No per-construct diagnostic message strings.

### Sync-point recovery is definitionally correct

The sync set is `Constructs.LeadingTokens` — it cannot drift from the construct inventory.

---

## 5. What I Reject and Why

### Layer C: Catalog-Driven Disambiguation — Rejected

The disambiguation logic for `In`/`To`/`From`/`On` is 4 methods of ~15 lines each, totaling ~60 lines. To make this catalog-driven would require:

1. A `DisambiguationRule` record type with fields for follow-tokens, lookahead depth, and intermediate parse actions.
2. A generic dispatcher that interprets disambiguation rules — which is a parser for parsers.
3. Handling the `when` guard re-dispatch, which is a two-step lookahead with an intermediate expression parse — not expressible as flat metadata without inventing a control-flow DSL.

The result would be ~60 lines of metadata definition + ~40 lines of generic dispatcher — replacing ~60 lines of direct method calls. Net complexity increase, plus the indirection makes debugging harder. And the benefit — automatic disambiguation for new scoped constructs — addresses a change that happens approximately never in a stable grammar.

**The principle:** catalog metadata should express *what* the language is (vocabulary, structure, slot sequences). It should not express *how* the parser navigates ambiguity. Disambiguation is control flow. Control flow is code.

### Binding Power Table — Rejected as Catalog Metadata

The Pratt parser's binding power table (left BP, right BP per operator) is a parser-internal representation of precedence, not a language surface concept. The `OperatorMeta.Precedence` value IS the catalog's expression of operator precedence. The parser derives binding powers from it:

```csharp
int leftBp  = meta.Precedence;
int rightBp = meta.Associativity switch
{
    Associativity.Left            => meta.Precedence,
    Associativity.Right           => meta.Precedence - 1,
    Associativity.NonAssociative  => meta.Precedence + 1,
};
```

This is a 3-line derivation, not a catalog gap. Adding `LeftBindingPower` and `RightBindingPower` to `OperatorMeta` would expose parser-internal mechanics on the catalog surface — the wrong direction.

### AST Node Hierarchy Genericization — Rejected

Making the AST a generic `ConstructNode` with a `SyntaxNode?[]` slot array (instead of per-construct records like `FieldDeclarationNode`) would make the parser fully generic but destroy downstream type safety. The type checker, graph analyzer, proof engine, and language server all pattern-match on specific node types. Replacing `node is FieldDeclarationNode { TypeRef: ... }` with `node.Slots[1]` would be an architectural regression.

The per-construct AST node records stay. The per-construct node *factories* are the thin adaptation layer between the generic slot iterator and the typed AST.

---

## 6. Estimation Inputs for George

### Scope summary

| Work item | Layer | Estimated complexity |
|-----------|-------|---------------------|
| Vocabulary frozen dictionaries (operators, types, modifiers, actions) | A | Small — 4 frozen collections, derived from existing `All` properties |
| `Constructs.ByLeadingToken` + `Constructs.LeadingTokens` indexes | B | Trivial — 2 derived properties, ~6 lines |
| Dispatch table refactor from switch → dictionary lookup + 4 disambiguation methods | B | Medium — restructure `ParseAll()` loop |
| Generic slot iteration in `ParseConstruct()` | D | Medium–Large — ~40 lines of generic iterator, boundary-token contracts on each slot parser |
| Per-construct AST node factory (`BuildNode` exhaustive switch) | D | Medium — 11 construct cases, pure field-mapping |
| Sync-point set derived from `Constructs.LeadingTokens` | E | Trivial — 4-line derivation |
| Slot parser method registry (`ConstructSlotKind` → parse method dictionary) | D | Small — 15 dictionary entries, no new logic |
| Tests: vocabulary derivation, dispatch coverage, slot iteration, sync recovery | All | Large — comprehensive coverage for all catalog-derived paths |

### Dependencies

- Layer A is independent — can ship first.
- Layer B depends on `Constructs.ByLeadingToken` (trivial).
- Layer D depends on Layer B's dispatch refactor being in place.
- Layer E is independent — can ship anytime.

### Risk

The main risk is Layer D's slot iteration. The boundary-token contract for each slot parser must be precisely defined: `ParseGuardClause()` must know it stops at `ensure`, `->`, or newline; `ParseActionChain()` must know it stops at outcome keywords or newline. Today these boundaries are implicit in the per-construct method flow. Making them explicit for the generic iterator requires care — but the explicitness is itself a design improvement (self-documenting slot contracts).

---

## 7. Architectural Summary

The TRULY catalog-driven parser has this shape:

1. **Dispatch** reads `Constructs.ByLeadingToken` to select a production (or a disambiguation method).
2. **Recognition** reads `Types.All`, `Modifiers.All`, `Actions.All`, `Operators.All` via frozen dictionaries.
3. **Slot iteration** reads `ConstructMeta.Slots` to drive the parse sequence for each construct.
4. **Error recovery** reads `Constructs.LeadingTokens` for sync points.
5. **Disambiguation** stays hand-written — 4 methods, ~60 lines total.
6. **Expression parsing** stays hand-written — Pratt loop with catalog-derived precedence.
7. **Slot-level parse methods** stay hand-written — grammar mechanics for each slot kind.
8. **AST node construction** stays hand-written — per-construct factories mapping slots to named fields.

The catalog drives ~70% of the parser's decision-making. The remaining ~30% is grammar mechanics that changes only when the grammar algorithm changes, not when the language surface changes.
