# Decision: When-Guard — Enum Eliminated, Slot List IS the Metadata

**Author:** Frank — Lead/Architect  
**Date:** 2026-05-10T13:16:47-04:00  
**Status:** Final — supersedes all prior GuardPolicy/SupportsPreVerbWhenGuard proposals  
**Supersedes:** Prior 2-member `GuardPolicy` proposal, prior 4-member proposal, original `SupportsPreVerbWhenGuard` boolean

---

## Answer

**YES. The enum disappears entirely. `SlotGuardClause` placed at the correct position in each construct's slot list, with per-construct termination tokens, eliminates `SupportsPreVerbWhenGuard` and any `GuardPolicy` enum. The slot list IS the metadata.**

My prior analysis (Option C in the superseded document) was wrong. I said it "requires rearchitecting how `ParseScopedConstruct` walks slots" and called it "scope-expanding." Having now read the parser code in detail, the refactor is actually a **simplification** — the current 3-phase protocol (anchor → flag-gated injection → disambig + remaining slots) collapses to a single unified loop.

---

## 1. Why It Works: Disambiguation Timing

The critical question was: **when does disambiguation happen relative to slot walking?**

**Answer: disambiguation happens FIRST, completely, before `ParseScopedConstruct` is ever called.**

### The routing phase (Parser.cs lines 140–198)

```
Main dispatch loop:
  → ByLeadingToken lookup → candidates
  → If 1 candidate: call ParseScopedConstruct(meta) directly
  → If N candidates: call ResolveDisambiguationToken(candidates)
    → resolved meta → call ParseScopedConstruct(meta)
```

### `ResolveDisambiguationToken` (Parser.cs lines 201–227)

Already handles `when` appearing before the disambiguation token:

```csharp
var disambToken = Peek(2).Kind;          // line 204
if (disambToken != TokenKind.When)       // line 205 — not when? done.
    return disambToken;
// when IS at position 2 — skip past the guard clause to find the real verb
var offset = 3;
while (true) { ... find actual disamb token ... }
```

**By the time `ParseScopedConstruct(meta)` is called, `meta` is fully resolved.** The parser knows exactly which construct it's parsing. This means a `GuardClause` at slot[1] is just a regular optional slot — no routing ambiguity, no special-casing needed.

---

## 2. The Refactored Algorithm

The current `ParseScopedConstruct` has a 3-phase protocol with a flag-gated injection:

```
Phase 1: Parse Slots[0] (anchor)
Phase 2: IF SupportsPreVerbWhenGuard flag, inject synthesized guard
Phase 3: Consume disambig keyword, walk Slots[1..]
```

The refactored version has a **single unified loop** — iterate all slots in order, consuming the disambiguation keyword at the natural boundary:

```csharp
private void ParseScopedConstruct(ConstructMeta meta)
{
    var startToken = Advance(); // consume leading keyword
    var startSpan = startToken.Span;
    var slots = new List<SlotValue>();

    // Collect disambiguation token set once
    var disambTokens = new HashSet<TokenKind>();
    foreach (var entry in meta.Entries)
        if (entry.DisambiguationTokens is { } dts)
            foreach (var dt in dts)
                disambTokens.Add(dt);

    bool disambConsumed = false;

    for (int i = 0; i < meta.Slots.Count; i++)
    {
        // Before each slot after the anchor, check for disambiguation keyword
        if (i > 0 && !disambConsumed && disambTokens.Contains(Peek().Kind))
        {
            if (Peek().Kind != TokenKind.Arrow)
                Advance(); // consume keyword — maps to no slot
            disambConsumed = true;
        }

        var slot = meta.Slots[i];
        var value = ParseSlotValue(slot, meta);
        if (slot.IsRequired || value.Span != SourceSpan.Missing)
            slots.Add(value);
    }

    // Post-action ensure (unchanged — SupportsPostActionEnsure is separate scope)
    if (meta.SupportsPostActionEnsure)
    {
        // ... unchanged from current code ...
    }

    // ... finalization unchanged ...
}
```

### Why this works — construct-by-construct verification

**StateEnsure** — `in Approved when x > 0 ensure y > 0 because "reason"`
Slots: `[StateTarget, GuardClause(term: [Ensure]), EnsureClause, OptBecauseClause]`

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Approved` | Parse StateTarget ✓ |
| slot[1] | `when` | Disamb check: `when` ∉ {Ensure} → skip. Parse GuardClause → consumes `when`, parses expr, terminates at `ensure` ✓ |
| slot[2] | `ensure` | Disamb check: `ensure` ∈ {Ensure} → consume, disambConsumed=true. Parse EnsureClause → required, no `ensure` to consume (already consumed), parses expr ✓ |
| slot[3] | `because` | Parse BecauseClause ✓ |

**StateEnsure without guard** — `in Approved ensure y > 0`

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Approved` | Parse StateTarget ✓ |
| slot[1] | `ensure` | Disamb check: `ensure` ∈ {Ensure} → consume, disambConsumed=true. Parse GuardClause → sees expression token, not `when` → returns Missing ✓ |
| slot[2] | `y` | disambConsumed → skip check. Parse EnsureClause → required, `ensure` already consumed, parses expr ✓ |

**StateAction** — `to Submitted when x > 0 -> set submittedAt = now()`
Slots: `[StateTarget, GuardClause(term: [Arrow]), ActionChain]`

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Submitted` | Parse StateTarget ✓ |
| slot[1] | `when` | Disamb check: `when` ∉ {Arrow} → skip. Parse GuardClause → terminates at Arrow ✓ |
| slot[2] | `->` | Disamb check: `->` ∈ {Arrow} → Arrow exception → don't advance → disambConsumed=true. Parse ActionChain → ActionChain parser consumes `->` ✓ |

**StateAction without guard** — `to Submitted -> set x = 1`

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Submitted` | Parse StateTarget ✓ |
| slot[1] | `->` | Disamb check: `->` ∈ {Arrow} → Arrow exception → don't advance → disambConsumed=true. Parse GuardClause → sees `->`, not `when` → returns Missing ✓ |
| slot[2] | `->` | disambConsumed → skip. Parse ActionChain → consumes `->` ✓ |

**EventEnsure** — `on Submit when x > 0 ensure y > 0 because "reason"`
Slots: `[EventTarget, GuardClause(term: [Ensure]), EnsureClause, OptBecauseClause]`
Same pattern as StateEnsure. ✓

**TransitionRow** — `from Draft on Submit when IsValid -> set y = 1 -> transition Approved`
Slots: `[StateTarget, EventTarget, GuardClause(term: [Because, Arrow]), ActionChain, Outcome]`

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Draft` | Parse StateTarget ✓ |
| slot[1] | `on` | Disamb check: `on` ∈ {On} → consume → disambConsumed=true. Parse EventTarget (Submit) ✓ |
| slot[2] | `when` | disambConsumed → skip check. Parse GuardClause ✓ |
| slot[3] | `->` | Parse ActionChain ✓ |
| slot[4] | `transition` | Parse Outcome ✓ |

**AccessMode** — `in Draft when IsOwner modify Amount editable`
Slots: `[StateTarget, GuardClause(term: [Modify]), FieldTarget, AccessModeKeyword]`

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Draft` | Parse StateTarget ✓ |
| slot[1] | `when` | Disamb check: `when` ∉ {Modify} → skip. Parse GuardClause → terminates at `modify` ✓ |
| slot[2] | `modify` | Disamb check: `modify` ∈ {Modify} → consume → disambConsumed=true. Parse FieldTarget (Amount) ✓ |
| slot[3] | `editable` | Parse AccessModeKeyword ✓ |

**AccessMode without guard** — `in Draft modify Amount editable`

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Draft` | Parse StateTarget ✓ |
| slot[1] | `modify` | Disamb check: `modify` ∈ {Modify} → consume → disambConsumed=true. Parse GuardClause → not `when` → Missing ✓ |
| slot[2] | `Amount` | disambConsumed → skip. Parse FieldTarget ✓ |
| slot[3] | `editable` | Parse AccessModeKeyword ✓ |

**OmitDeclaration** — `in Draft omit InternalNotes`
Slots: `[StateTarget, FieldTarget]` (no guard — unchanged)

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `Draft` | Parse StateTarget ✓ |
| slot[1] | `omit` | Disamb check: `omit` ∈ {Omit} → consume → disambConsumed=true. Parse FieldTarget ✓ |

**EventHandler** — `on UpdateName -> set name = newName`
Slots: `[EventTarget, ActionChain]` (no guard — unchanged)

| Step | Token | Action |
|------|-------|--------|
| slot[0] | `UpdateName` | Parse EventTarget ✓ |
| slot[1] | `->` | Disamb check: `->` ∈ {Arrow} → Arrow exception → don't advance → disambConsumed=true. Parse ActionChain → consumes `->` ✓ |

**All 7 scoped constructs verified. The unified loop handles every case.**

---

## 3. Updated Slot Lists — All 6 When-Using Constructs

### New per-construct guard clause slots

```csharp
// Pre-verb guard — terminates at 'ensure' (for ensure-disambiguated constructs)
private static readonly ConstructSlot SlotPreVerbGuardEnsure = new(
    ConstructSlotKind.GuardClause, IsRequired: false,
    TerminationTokens: [TokenKind.Ensure]);

// Pre-verb guard — terminates at 'arrow' (for arrow-disambiguated constructs)
private static readonly ConstructSlot SlotPreVerbGuardArrow = new(
    ConstructSlotKind.GuardClause, IsRequired: false,
    TerminationTokens: [TokenKind.Arrow]);

// Pre-verb guard — terminates at 'modify' (for access mode)
private static readonly ConstructSlot SlotPreVerbGuardModify = new(
    ConstructSlotKind.GuardClause, IsRequired: false,
    TerminationTokens: [TokenKind.Modify]);
```

### Before → After

| Construct | Slots (before) | Extra metadata (before) | Slots (after) | Extra metadata (after) |
|-----------|---------------|------------------------|--------------|----------------------|
| **Rule** | `[RuleExpr, GuardClause, BecauseClause]` | (none) | **Unchanged** | (none) |
| **TransitionRow** | `[StateTarget, EventTarget, GuardClause, ActionChain, Outcome]` | (none) | **Unchanged** | (none) |
| **StateEnsure** | `[StateTarget, EnsureClause, OptBecauseClause]` | `SupportsPreVerbWhenGuard: true` | `[StateTarget, PreVerbGuardEnsure, EnsureClause, OptBecauseClause]` | **(none)** |
| **StateAction** | `[StateTarget, ActionChain]` | `SupportsPreVerbWhenGuard: true` | `[StateTarget, PreVerbGuardArrow, ActionChain]` | **(none)** |
| **EventEnsure** | `[EventTarget, EnsureClause, OptBecauseClause]` | `SupportsPreVerbWhenGuard: true` | `[EventTarget, PreVerbGuardEnsure, EnsureClause, OptBecauseClause]` | **(none)** |
| **AccessMode** | `[StateTarget, FieldTarget, AccessModeKeyword, GuardClause]` | (none) | `[StateTarget, PreVerbGuardModify, FieldTarget, AccessModeKeyword]` | **(none)** |

### What disappeared

- `SupportsPreVerbWhenGuard` parameter from `ConstructMeta`: **deleted**
- `GuardPolicy` enum: **never created** (the prior proposal is superseded)
- Synthesized guard slot injection in `ParseScopedConstruct`: **deleted**
- The guard is now IN the slot list for every construct that supports it. No injection needed.

---

## 4. Why My Prior Analysis Was Wrong

The superseded document (Option C) said:

> *"If the guard is at slot[1] but must be parsed BEFORE the disambiguation token, you need the parser to know which slots are pre-disambiguation and which are post. That's a larger refactor."*

This was wrong for two reasons:

1. **The parser doesn't need to "know" pre-vs-post.** The unified loop checks for disambiguation tokens before each slot. The check is generic — it doesn't care about slot kind. The disambiguation keyword is consumed at the first slot boundary where it appears, regardless of whether that's before slot[1] or slot[2].

2. **The refactor is a simplification, not a complication.** The current code has 3 phases and a flag-gated injection. The refactored code has 1 loop and no flags. Net code reduction.

The key insight I missed: disambiguation keyword consumption doesn't need a fixed slot index. It happens naturally at the boundary where the disambiguation token appears in the token stream. The unified loop discovers this position dynamically.

---

## 5. Guard Clause Termination Tokens — The Essential Detail

Each guard clause slot must terminate at the right token. This is NOT new metadata — it's the same `TerminationTokens` that every slot already carries:

| Slot instance | TerminationTokens | Used by |
|--------------|-------------------|---------|
| `SlotGuardClause` (shared, existing) | `[Because, Arrow]` | TransitionRow, RuleDeclaration |
| `SlotPreVerbGuardEnsure` (new) | `[Ensure]` | StateEnsure, EventEnsure |
| `SlotPreVerbGuardArrow` (new) | `[Arrow]` | StateAction |
| `SlotPreVerbGuardModify` (new) | `[Modify]` | AccessMode |

The termination tokens are derived from the construct's disambiguation tokens. In the current code, this derivation happens at runtime in `ParseScopedConstruct` (line 285–288). In the refactored code, it's baked into the slot definition at catalog time. **Catalog-time is better** — it makes the construct definition self-describing.

---

## 6. `ParseScopedConstruct` — Complete Refactored Code

```csharp
// Protocol: consume leading keyword, iterate all slots, consuming the
// disambiguation keyword at the natural boundary between scope and body.

private void ParseScopedConstruct(ConstructMeta meta)
{
    var startToken = Advance(); // consume leading keyword
    var startSpan = startToken.Span;
    var slots = new List<SlotValue>();

    // Collect disambiguation token set once
    var disambTokens = new HashSet<TokenKind>();
    foreach (var entry in meta.Entries)
        if (entry.DisambiguationTokens is { } dts)
            foreach (var dt in dts)
                disambTokens.Add(dt);

    bool disambConsumed = false;

    for (int i = 0; i < meta.Slots.Count; i++)
    {
        // Before each slot after the anchor, check for disambiguation keyword
        if (i > 0 && !disambConsumed && disambTokens.Contains(Peek().Kind))
        {
            if (Peek().Kind != TokenKind.Arrow)
                Advance(); // consume keyword — maps to no slot
            disambConsumed = true;
        }

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
```

### What disappeared from the parser

- Lines 272–278 (special anchor parsing): **merged into the unified loop** (slot[0] is just the first iteration)
- Lines 280–292 (flag-gated guard injection): **deleted entirely** — the guard is now a regular slot
- Lines 294–308 (separate disambig consumption): **merged into the loop** (the disamb check happens before each slot)
- Lines 310–317 (separate Slots[1..] walk): **merged into the loop**
- The `SupportsPreVerbWhenGuard` check: **gone**

Net: the body of `ParseScopedConstruct` shrinks from ~77 lines to ~45 lines. No special cases remain for guard handling.

---

## 7. AccessMode Surface Syntax Change

**Before (post-verb):**
```
in Draft modify Amount editable when IsOwner
```

**After (pre-verb — consistent with all other guard positions):**
```
in Draft when IsOwner modify Amount editable
```

This is a **breaking change** to `.precept` files. No current sample files use guarded access mode (confirmed by prior audit). The old post-verb form would produce a parse error after implementation (no diagnostic needed — the parser simply won't find `when` in a position where it's valid).

---

## 8. Complete File Change Inventory

### Source

| File | Change |
|------|--------|
| `src/Precept/Language/Construct.cs` | **Remove** `SupportsPreVerbWhenGuard` parameter from `ConstructMeta`. |
| `src/Precept/Language/Constructs.cs` | Add 3 new shared slot instances (`SlotPreVerbGuardEnsure`, `SlotPreVerbGuardArrow`, `SlotPreVerbGuardModify`). Update slot lists for StateEnsure, StateAction, EventEnsure, AccessMode. Remove `SupportsPreVerbWhenGuard: true` from all 3 constructs. Update AccessMode description/example strings. |
| `src/Precept/Pipeline/Parser.cs` | Replace `ParseScopedConstruct` with unified loop. Delete lines 272–317 (anchor special-case, flag-gated injection, separate disambig consumption, separate slot walk). No `GuardPolicy` enum, no `SupportsPreVerbWhenGuard` reference. |

### Tests

| File | Change |
|------|--------|
| `test/Precept.Tests/Language/Track2PhaseAConstructCatalogTests.cs` | Delete 3 `SupportsPreVerbWhenGuard` tests. Add tests asserting guard slot presence at correct position in slot lists. |
| `test/Precept.Tests/CatalogCapability/ConstructCatalogCapabilityTests.cs` | Delete 3 `SupportsPreVerbWhenGuard` capability tests. Add guard-slot-position tests. |
| Parser test files | Add parse tests for `in Draft when IsOwner modify Amount editable`. Verify pre-verb guard parsing for all 4 constructs. |

### Documentation

| File | Change |
|------|--------|
| `docs/language/precept-language-spec.md` | Update access mode grammar to show pre-verb guard position. |
| `docs/language/catalog-system.md` | Remove `SupportsPreVerbWhenGuard` from schema. Document that guard position is encoded in the slot list. |

### MCP / Language Server

| Surface | Impact |
|---------|--------|
| MCP `precept_language` | `SupportsPreVerbWhenGuard` disappears from construct JSON. No replacement field — the slot list in the construct output already shows the guard position. |
| LS completions | `when` suggestion for AccessMode moves to post-state-target position. Completions already derive from slot lists, so this may be automatic. |
| LS semantic tokens / grammar | No impact — `when` keyword matching is not construct-specific. |

### Samples

No sample files use guarded access mode — no sample changes needed.

---

## 9. Confirming Zero Special-Casing

After this change:

1. **No `SupportsPreVerbWhenGuard` flag** — deleted from `ConstructMeta`
2. **No `GuardPolicy` enum** — never created
3. **No synthesized guard slot** — the parser never creates a `ConstructSlot` at runtime for guard injection
4. **No slot-kind checking in the loop** — the parser doesn't check `slot.Kind == GuardClause` anywhere; it just calls `ParseSlotValue` which dispatches by kind as it always does
5. **No position-based heuristics** — the loop doesn't assume "slot[1] is the guard" or "slot[0] is special"; it iterates uniformly
6. **The only non-slot behavior** — disambiguation keyword consumption, which is the same for ALL scoped constructs (guard or not) and happens at the natural boundary

The slot list IS the metadata. The parser is a dumb slot walker that also handles disambiguation keywords.

---

## 10. The `SupportsPostActionEnsure` Parallel Smell

`SupportsPostActionEnsure` (used only by `EventHandler`) is the exact same pattern — a flag that triggers post-slot injection of synthesized ensure/because slots. The same refactoring approach would work: add `EnsureClause` and `BecauseClause` to EventHandler's slot list.

This is noted but out of scope for this decision. The refactor pattern is identical and can be applied independently.

---

## 11. Rationale Summary

| Question | Answer |
|----------|--------|
| Can the enum be eliminated? | **Yes.** The slot list encodes guard position. No metadata flag or enum is needed. |
| What prevents it? | Nothing. My prior objection ("requires rearchitecting") was wrong — the refactor is a simplification. |
| Is it a fundamental constraint? | No. Disambiguation happens before slot walking. A guard at slot[1] is just a regular optional slot. |
| What about termination tokens? | Per-construct guard slot instances carry the right termination tokens. This is existing catalog infrastructure. |
| What about AccessMode? | Guard moves from post-verb slot[3] to pre-verb slot[1]. Surface syntax changes. |
| Is anything lost? | No. The unified loop handles all 7 scoped constructs without special cases. Net code reduction. |
| What about `SupportsPostActionEnsure`? | Same smell, same fix pattern. Separate scope. |

---

## 12. Alternatives Rejected

| Alternative | Reason |
|-------------|--------|
| 2-member `GuardPolicy` enum (`SlotDriven`/`PreVerb`) | Adds metadata that the slot list already encodes. The enum names a concept that disappears when the slot list is authoritative. |
| Boolean rename (`InjectsPreVerbGuard`) | Same problem — flag that duplicates what the slot list says. |
| Keep `SupportsPreVerbWhenGuard` | The original smell. Parser logic keyed on a flag instead of walking the catalog-declared slot list. |
| Synthetic `DisambiguationKeyword` slot | Over-engineering. The disambiguation keyword consumption is structural (all scoped constructs have it) and doesn't need a slot — the unified loop handles it with a simple boundary check. |
