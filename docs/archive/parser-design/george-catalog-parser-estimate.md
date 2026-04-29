# Catalog-Driven Parser: Implementation Estimate

**By:** George (Runtime Dev)
**Date:** 2026-04-27
**For:** Shane / Frank (prioritization decision)
**Status:** Estimate — not an implementation plan

---

## Inventory (Counted, Not Estimated)

Before the estimates, the hard numbers from the source:

| Catalog | Members | Parser usage |
|---------|---------|-------------|
| `OperatorKind` | 18 (17 distinct tokens — Minus/Negate share) | Pratt BP table |
| `TypeKind` | 26 (but SetType is parser-synthesized, not in catalog) | Type keyword recognition set |
| `ModifierKind` | 28 (15 field + 7 state + 1 event + 3 access + 3 anchor) | Modifier recognition set |
| `ActionKind` | 8 | Action keyword recognition after `->` |
| `ConstructKind` | 11 | Dispatch routing |
| `ConstructSlotKind` | 15 | Slot positions per construct |

Dispatch routing breakdown:
- **1:1 tokens** (unambiguous): `Field`, `State`, `Event`, `Rule`, `Write` → 5 constructs
- **1:N tokens** (require disambiguation):
  - `In` → `StateEnsure`, `AccessMode` (2 constructs)
  - `To` → `StateEnsure`, `StateAction` (2 constructs)
  - `From` → `TransitionRow`, `StateEnsure`, `StateAction` (3 constructs)
  - `On` → `EventEnsure`, `EventHandler` (2 constructs)
- Total ambiguous paths: 9 across 4 tokens

Parser methods when fully implemented (from doc § Architecture): ~20 methods in `ParseSession` — `ParseFieldDeclaration`, `ParseStateDeclaration`, `ParseEventDeclaration`, `ParseRuleDeclaration`, `ParseAccessMode`, `ParseInScoped`, `ParseToScoped`, `ParseFromScoped`, `ParseOnScoped`, `ParseStateEnsure`, `ParseStateAction`, `ParseTransitionRow`, `ParseEventEnsure`, `ParseEventHandler`, `ParseExpression` (Pratt), `ParseActionChain`, `ParseTypeRef`, `ParseFieldModifiers`, `ParseGuard`, `ParseEnsureClause` = ~20.

Vocabulary entries that would be hardcoded if not catalog-derived: 18 + 26 + 28 + 8 = **80 entries** across 4 tables.

Existing frozen dictionary examples already in the codebase:
- `Operators.ByToken` — `FrozenDictionary<(TokenKind, Arity), OperatorMeta>` — **already exists**
- `Tokens.Keywords` — frozen dictionary for lexer keyword lookup — **already exists**

---

## Layer A: Vocabulary Tables

> Derive operator precedence, type keywords, modifier sets, action sets from catalog frozen dictionaries.

### What files change

| File | Change |
|------|--------|
| `src/Precept/Language/Types.cs` | Add `ByToken: FrozenDictionary<TokenKind, TypeMeta>` (3 lines, same pattern as `Operators.ByToken`) |
| `src/Precept/Language/Modifiers.cs` | Add `ByToken: FrozenDictionary<TokenKind, IReadOnlyList<ModifierMeta>>` — returns a list because `InitialState`/`InitialEvent` share the `initial` keyword |
| `src/Precept/Language/Actions.cs` | Add `ByToken: FrozenDictionary<TokenKind, ActionMeta>` |
| `src/Precept/Pipeline/Parser.cs` | At `ParseSession` init, build: Pratt BP table from `Operators.All`, type keyword set from `Types.All`, modifier set from `Modifiers.All`, action set from `Actions.All` |

`Operators.ByToken` already exists — the Pratt table reads from it directly. No change to `Operators.cs`.

### New catalog types/fields needed

- No new `ConstructMeta` fields
- 3 new `FrozenDictionary` derived indexes (Types, Modifiers, Actions) — each is a 3-line property derived from `All`
- `Modifiers.ByToken` returns `IReadOnlyList<ModifierMeta>` not a single value because `Initial` resolves to two `ModifierKind` values (`InitialState`, `InitialEvent`) — context distinguishes them at parse time

### Effort breakdown

| Task | Size | Hours |
|------|------|-------|
| `Types.ByToken` index | XS | 1h |
| `Actions.ByToken` index | XS | 1h |
| `Modifiers.ByToken` index (list, dual-initial handling) | XS | 2h |
| Parser vocabulary init from catalog at ParseSession startup | S | 4h |
| Edge case: `SetType` is parser-synthesized (lexer emits `Set`; parser position-disambiguates) — document the exception | XS | 1h |
| Edge case: `min`/`max` dual-use (constraint vs function) — same token, disambiguated by presence of `(` | XS | 1h |
| Tests: catalog index completeness + parser vocabulary coverage | S | 4h |
| **Total** | **S** | **~14h / ~2 days** |

### Risk flags

1. **`SetType` exception** — `TypeKind.SetType` has no `Token` in `Types.GetMeta()` (the catalog entry uses `Set` token, not `SetType`). The `Types.ByToken` index must exclude `SetType` and document the exception explicitly. Low severity — just needs a comment and a guard.
2. **`initial` dual-use** — Two `ModifierKind` values share `TokenKind.Initial`. The `ByToken` index returns a list; the parser picks the right one by parse context (state declaration vs event declaration). Straightforward once recognized.
3. **Parser is currently a stub** — this means there's no rework cost. Implementing vocabulary tables catalog-driven from day one is essentially free vs implementing them hardcoded and refactoring. This is the clean-slate advantage.

### Value/cost verdict: **YES — highest ROI of all 5 layers**

80 vocabulary entries stay in the catalog and never get duplicated. Cost is ~2 days, risk is near-zero. This is the clear win Frank already identified.

---

## Layer B: Dispatch Table

> Can `ConstructMeta.LeadingToken` drive the top-level dispatch loop?

**Frank's ruling (2026-04-27):** The dispatch table is declared hand-written grammar mechanics per `frank-parser-dispatch-analysis.md`. This estimate evaluates the cost/value to give Shane a real number, regardless of that ruling.

### What files change

| File | Change |
|------|--------|
| `src/Precept/Language/Constructs.cs` | Add `ByLeadingToken: FrozenDictionary<TokenKind, ConstructKind[]>` (3-5 lines) |
| `src/Precept/Pipeline/Parser.cs` | Dispatch loop queries `Constructs.ByLeadingToken` to route 1:1 tokens; 1:N tokens still call hand-written `ParseInScoped()` etc. |

### New catalog types/fields needed

- `Constructs.ByLeadingToken: FrozenDictionary<TokenKind, ConstructKind[]>` — derived from `All`, grouped by `LeadingToken`

No new fields on `ConstructMeta`.

### Effort breakdown

| Task | Size | Hours |
|------|------|-------|
| `Constructs.ByLeadingToken` derived dictionary | XS | 1h |
| Update parser dispatch loop to use it for 1:1 cases | XS | 2h |
| 4 disambiguation methods still hand-written (unchanged) | — | (already needed) |
| Tests | XS | 2h |
| **Total** | **XS** | **~5h / ~0.5-1 day** |

### Risk flags

1. **Marginal value.** The dispatch loop is 9 cases. A hardcoded switch with a comment citing the catalog is equally maintainable. The real ROI is: if a 12th `ConstructKind` is ever added with a new 1:1 leading token, the dispatch loop auto-propagates. For a grammar this intentionally small and closed, that's a theoretical benefit.
2. **The 4 disambiguation methods are unchanged.** This layer does nothing for the `In`/`To`/`From`/`On` cases that make the dispatch non-trivial.
3. **Frank has already ruled this hand-written.** I agree with the ruling — the dispatch loop is structurally simple enough that the catalog-derived version adds no correctness guarantees that the hand-written version doesn't already have. The `ByLeadingToken` index is still worth adding to `Constructs` for LS completions and MCP vocabulary consumers (they want "what constructs are valid at this position?" — the index answers that).

### Value/cost verdict: **MARGINAL for parser; YES for catalog completeness**

The `ByLeadingToken` index should exist in the catalog as a derived accessor for tooling consumers (LS context-aware completions, MCP, grammar generation). Whether the parser uses it for dispatch routing vs a hand-written switch is an implementation detail that doesn't change catalog quality. Add the index: ~1 hour. Whether to wire parser dispatch through it: not worth the argument.

---

## Layer C: Disambiguation Hints in ConstructMeta

> What if `ConstructMeta` carried context/scope constraints or lookahead hints to drive disambiguation programmatically?

### What files change

| File | Change |
|------|--------|
| `src/Precept/Language/Construct.cs` | Add `DisambiguationToken: TokenKind?` field to `ConstructMeta` |
| `src/Precept/Language/Constructs.cs` | Update all 11 `GetMeta()` switch arms with disambiguation tokens; add `ByLeadingAndDisambiguationToken` derived index |
| `src/Precept/Pipeline/Parser.cs` | 4 disambiguation methods (`ParseInScoped`, `ParseToScoped`, `ParseFromScoped`, `ParseOnScoped`) use the catalog index |

### What disambiguation tokens would be

| Construct | LeadingToken | DisambiguationToken |
|-----------|-------------|---------------------|
| `PreceptHeader` | `Precept` | (none — no ambiguity) |
| `FieldDeclaration` | `Field` | (none) |
| `StateDeclaration` | `State` | (none) |
| `EventDeclaration` | `Event` | (none) |
| `RuleDeclaration` | `Rule` | (none) |
| `TransitionRow` | `From` | `On` |
| `StateEnsure` (from) | `From` | `Ensure` |
| `StateEnsure` (in) | `In` | `Ensure` |
| `StateEnsure` (to) | `To` | `Ensure` |
| `AccessMode` | `In` | `Write`/`Read`/`Omit` |
| `StateAction` (to) | `To` | `Arrow` |
| `StateAction` (from) | `From` | `Arrow` |
| `EventEnsure` | `On` | `Ensure` |
| `EventHandler` | `On` | `Arrow` |

Problem: `StateEnsure` appears 3 times with 3 different leading tokens. `AccessMode` appears with leading token `Write` (root-level) AND with `In` + disambiguation `Write`. These are not clean 1:1 mappings.

### New catalog types/fields needed

```csharp
// ConstructMeta gains:
TokenKind? DisambiguationToken = null  // token seen after anchor target

// Constructs gains:
FrozenDictionary<(TokenKind Leading, TokenKind? Disambiguation), ConstructKind[]> ByLeadingAndDisambiguationToken
```

3 switch arms need to be updated (StateEnsure, StateAction, AccessMode) to carry disambiguation tokens because they appear with multiple leading tokens. 8 other arms are trivial nulls.

### Effort breakdown

| Task | Size | Hours |
|------|------|-------|
| Add `DisambiguationToken` to `ConstructMeta` record | XS | 1h |
| Update 11 switch arms in `Constructs.GetMeta()` | S | 3h |
| Add derived `ByLeadingAndDisambiguationToken` index | S | 2h |
| Update 4 disambiguation methods in parser | S | 6h |
| Tests | S | 4h |
| **Total** | **S-M** | **~16h / ~2-3 days** |

### Risk flags

1. **The `When` intermediate problem.** The disambiguation is not always `(LeadingToken, NextToken after target)`. The optional `when <guard>` can appear before the disambiguation token:
   ```
   from UnderReview when condition on Submit → TransitionRow
   from UnderReview on Submit → TransitionRow (same result, different path)
   ```
   This means the parser MUST consume the optional guard first, THEN look up the disambiguation token. The catalog can tell you what token to look for; it can't tell you to skip the guard first. The parser still needs: "if next is `When`, consume guard; then look up disambiguation." This is one `if` statement — not complex — but it means the catalog lookup is a step in the flow, not the full flow.

2. **`AccessMode` dual entry path.** `AccessMode` has `LeadingToken = TokenKind.Write` (root-level stateless form) AND appears as `in <state> write/read/omit` (disambiguation path from `In`). The `ByLeadingAndDisambiguationToken` index has `(Write, null) → AccessMode` AND `(In, Write) → AccessMode`, `(In, Read) → AccessMode`, `(In, Omit) → AccessMode`. The disambiguation token for the `In` case is actually 3 possible tokens. A single `DisambiguationToken?` field can't express this — you'd need `DisambiguationToken: TokenKind[]?`.

3. **Catalog completeness benefit is real.** Even if the disambiguation table doesn't make parser code dramatically simpler, having `DisambiguationToken` in the catalog means MCP can answer "what constructs can follow `from <state>`?" and LS can drive context-aware completions. The data belongs in the catalog under the metadata-driven principle. The parser just doesn't have to be structurally dependent on it.

### Value/cost verdict: **CONDITIONAL**

The `DisambiguationToken` field belongs in the catalog on philosophical grounds — it IS language structure. The value for LS and MCP is genuine. The value for parser simplification is limited because of the `When` intermediate case and the `AccessMode` multi-token disambiguation. Add it to the catalog, use it in tooling. Don't restructure the parser disambiguation methods to be catalog-table-driven — the special-case handling would be more complex than 4 clean disambiguation methods.

**Revised recommendation:** Add `DisambiguationToken: TokenKind[]?` to `ConstructMeta` for catalog completeness and tooling value. Document the `When`-intermediate exception. Parser methods reference it as metadata (comment/documentation) but continue to own their own parsing sequences.

**Cost if done correctly (catalog + tooling, not parser restructuring): ~2 days.**
**Cost if forced to be parser-driven: ~3-4 days with moderate risk.**

---

## Layer D: Grammar Productions in Catalog (Slot-Driven Parsing)

> What if required/optional token sequences within a construct were catalog metadata? Parser validates against metadata instead of per-method hardcode.

### Current state of ConstructSlot

`ConstructSlotKind` has 15 values. Every `ConstructMeta` already carries `Slots: IReadOnlyList<ConstructSlot>`. The skeleton exists. The question is whether the parser can be driven by iterating slots rather than executing per-method code.

### What a slot-driven parser looks like

```csharp
// Generic slot-driven approach
private Declaration ParseDeclaration(ConstructMeta meta)
{
    var slots = new Dictionary<ConstructSlotKind, SyntaxNode?>();
    foreach (var slot in meta.Slots)
    {
        slots[slot.Kind] = ParseSlot(slot.Kind, slot.IsRequired);
    }
    return BuildDeclaration(meta.Kind, slots);
}
```

The problem: each `ConstructSlotKind` still needs its own `ParseSlot(kind)` implementation — there's no way around custom parsing logic per slot type. The abstraction reorganizes the call site but doesn't reduce total code.

### What files change

| File | Change |
|------|--------|
| `src/Precept/Language/ConstructSlot.cs` | Add slot introduction tokens, separator tokens, stop conditions |
| `src/Precept/Pipeline/Parser.cs` | Major rewrite: ~15 per-method parsers → generic slot-dispatch + BuildDeclaration factory |
| `src/Precept/Pipeline/SyntaxNodes.cs` | `BuildDeclaration` factory assembles typed AST nodes from heterogeneous slot results |

### New catalog types/fields needed

To make `ConstructSlot` carry enough data for slot-driven parsing:

```csharp
// ConstructSlot would need:
TokenKind? IntroductionToken   // token that opens the slot (e.g., "as" for TypeExpression)
TokenKind? SeparatorToken      // token between repeated items (e.g., "," for IdentifierList)
TokenKind[]? StopTokens        // tokens that terminate the slot (for loops)
bool       IsRepeated          // whether the slot can appear multiple times
```

4 new fields on `ConstructSlot`. Each of the 11 constructs' slot lists needs careful annotation. `ActionChain` and `Outcome` slots have complex stopping conditions (`->` can be both separator and stop depending on what follows).

### Effort breakdown

| Task | Size | Hours |
|------|------|-------|
| Design slot metadata additions (introduction, separator, stop conditions) | M | 8h |
| Update `ConstructSlot` record with new fields | S | 4h |
| Update all 11 construct entries in `Constructs.GetMeta()` | M | 12h |
| Implement `ParseSlot(ConstructSlotKind)` dispatch (~15 slot kinds) | L | 40h |
| Implement `BuildDeclaration` factory (typed AST from slot dictionary) | L | 20h |
| Rewrite ~15 per-method parsers as slot-metadata-driven | L | 30h |
| Regression tests (all 11 constructs × expression/error-recovery cases) | XL | 40h |
| **Total** | **XL** | **~154h / ~4-5 weeks** |

### Risk flags

1. **The `ActionChain` slot is a loop, not a slot.** An action chain is `-> action -> action → outcome`. The `->` separator also terminates the loop when followed by `transition`/`no`/`reject`. Encoding this stopping condition as catalog metadata requires either special-casing or a more complex slot descriptor (a `LoopSlot` DU subtype). This is the #1 complexity spike.

2. **`Outcome` has 3 forms.** `-> transition <state>`, `-> no transition`, `-> reject <msg>`. Representing these as catalog metadata requires a slot type that's essentially a sub-grammar. At that point you're building a grammar DSL inside the catalog, which is the wrong abstraction.

3. **`BuildDeclaration` is fragile.** Assembling typed AST records from a dictionary of heterogeneous slot results is complex and error-prone. Compared to a method that builds the exact record it expects, the factory needs runtime type coercion and produces worse error messages on mismatch.

4. **At Precept's scale, this is over-abstracted.** 11 constructs, intentionally closed grammar. The benefit of "adding construct #12 is just a catalog entry" is theoretical at Precept's grammar scale. A closed DSL with exactly 11 constructs doesn't need a generic production framework.

5. **High regression risk.** Converting 11 proven parse methods (once written) to a generic framework means any bug in the slot-dispatch machinery affects all 11 constructs simultaneously instead of one at a time.

### Value/cost verdict: **NOT RECOMMENDED**

The `Slots` field in `ConstructMeta` is already high-value for LS completions, MCP vocabulary, grammar generation, and AI grounding — all of those consumers iterate `meta.Slots` for declarative metadata about construct structure. Those benefits are already captured. Driving the parser itself from slot iteration doesn't add meaningful value over well-written per-method parsers and adds substantial complexity. The cost is 4-5 weeks. The risk is high. The correctness improvement is near-zero (the parser produces the same AST either way).

---

## Layer E: Error Recovery Sync Points

> Derive sync-point token set from `Constructs.All.Select(m => m.LeadingToken)` instead of hardcoded list.

### What files change

| File | Change |
|------|--------|
| `src/Precept/Pipeline/Parser.cs` | Replace hardcoded sync token list with `FrozenSet<TokenKind>` derived from `Constructs.All` |

### Current design (from parser.md)

The doc already says: "These are exactly the `LeadingToken` values from the Constructs catalog plus `EndOfSource`." The intent is explicit. The implementation should match.

### New catalog types/fields needed

None. `LeadingToken` is already on every `ConstructMeta`.

### Implementation

```csharp
// In ParseSession — static, computed once
private static readonly FrozenSet<TokenKind> SyncPoints =
    Constructs.All
        .Select(m => m.LeadingToken)
        .Distinct()
        .ToFrozenSet();
```

9 unique leading tokens. Exact same set as the hardcoded list. Automatically gains any future leading tokens if the grammar ever expands.

### Effort breakdown

| Task | Size | Hours |
|------|------|-------|
| Add `SyncPoints` derived from catalog | XS | 1h |
| Update `SyncToNextDeclaration()` to use it | XS | 1h |
| Test: verify sync token set matches catalog | XS | 1h |
| **Total** | **XS** | **~3h / 0.5 days** |

### Risk flags

None. The sync token set is 9 tokens either way. If the grammar adds a new construct with a new leading token in the future, the catalog-derived version auto-updates. The hardcoded version would miss it. This is the textbook no-brainer catalog derivation.

### Value/cost verdict: **YES — trivial, no risk, do it as part of Layer A**

---

## Summary Table

| Layer | Description | Effort | Risk | Worth it? |
|-------|-------------|--------|------|-----------|
| **A** | Vocabulary tables (operator BP, type keywords, modifier sets, action sets derived from `Operators.ByToken`, + 3 new `ByToken` indexes) | **S / ~2 days** | Low — `SetType` and dual-`Initial` edge cases are documented exceptions | **Yes — highest ROI** |
| **B** | Dispatch table (add `Constructs.ByLeadingToken` index; wire parser dispatch through it) | **XS / ~0.5-1 day** | Low — value for tooling consumers; marginal for parser | **Marginal for parser; Yes for `ByLeadingToken` index as catalog data** |
| **C** | Disambiguation hints (`DisambiguationToken: TokenKind[]?` in `ConstructMeta`; `ByLeadingAndDisambiguationToken` index) | **S-M / ~2-3 days** | Medium — `When` intermediate case; `AccessMode` multi-token disambiguation | **Yes for catalog completeness + tooling; No for parser restructuring** |
| **D** | Grammar productions as slot-driven parsing (parser iterates `meta.Slots`, generic `ParseSlot()` dispatch) | **XL / ~4-5 weeks** | High — `ActionChain` loop, `Outcome` 3-form, `BuildDeclaration` fragility, full regression suite | **No — `Slots` already serves tooling consumers; parser doesn't need to be slot-driven** |
| **E** | Sync-point derivation from `Constructs.All.Select(LeadingToken)` | **XS / ~0.5 days** | None | **Yes — trivial, do with Layer A** |

---

## Aggregate Estimates

### All 5 Layers

| Layer | Effort |
|-------|--------|
| A (vocabulary tables) | 2 days |
| B (dispatch table index + wiring) | 1 day |
| C (disambiguation hints, catalog + tooling only) | 2-3 days |
| D (slot-driven parsing, full rewrite) | 4-5 weeks |
| E (sync-point derivation) | bundled in A |
| **Total all 5** | **~6-7 weeks** |

Layer D is 85%+ of the total. Without it:

### Recommended: A + B index + C (catalog/tooling, not parser-restructuring) + E

| Layer | Effort |
|-------|--------|
| A | 2 days |
| B (`ByLeadingToken` index only) | bundled in A / 1h |
| C (catalog completeness + tooling, no parser restructuring) | 2 days |
| E | bundled in A |
| **Total recommended** | **~4-5 days / 1 week** |

This delivers:
- **80 vocabulary entries** staying in catalogs, never duplicated in the parser
- **Sync points** auto-derived from the grammar (no drift risk)
- **`ByLeadingToken`** and **`DisambiguationToken`** indexes in the catalog for LS and MCP consumers
- **Zero architectural risk** — parser methods are still clean recursive descent, no generic machinery

---

## What We Don't Get (and Why That's OK)

**Layer D** (slot-driven parsing) would give us: if we ever add construct #12, the parser "just works." But Precept's grammar is intentionally small and closed. A new construct is a design-level decision (design review, language spec change, full pipeline impact) — not a routine addition. The "one catalog entry and done" benefit is real but theoretical at current scale.

The more honest framing: the `Slots` metadata IS the slot-driven parser, just for tooling consumers instead of the parsing engine itself. LS completions iterate slots to suggest what comes next. MCP vocabulary exposes slot shapes. Grammar generation uses slot sequences to produce `.tmLanguage` patterns. All of that value is already captured by the existing `Slots` field in `ConstructMeta`. Wiring the parser itself to iterate those same slots buys nothing except complexity.

---

## Hard Call on Layer C

The philosophical case for `DisambiguationToken` in the catalog is strong — the disambiguation token IS language structure, and the catalog-system doc's principle says "if it's language structure, it gets cataloged." But I want to be honest: the practical parser simplification from adding this field is modest because of the `When` intermediate case. Every one of the 4 disambiguation methods has this shape:

```
1. Parse anchor target (state or event name)
2. IF next token is `when`: consume guard expression  ← can't be catalog-driven
3. Look at next token → route to correct construct     ← this part CAN be catalog-driven
```

Step 2 will always be hand-written logic. Step 3 is where the catalog lookup would help. At 4 disambiguation methods, step 3 is each a 3-4 branch if-chain. The catalog lookup would replace those if-chains with a dictionary lookup. That's real, but it's ~12-16 lines of code total.

My verdict: add `DisambiguationToken` to the catalog for correctness and tooling value. Reference it in the parser methods as documentation. Don't restructure the parser to be mechanically table-driven for those 4 methods — the explicit code is clearer and easier to debug.

---

## George's Bottom Line

The clean win is A + E: ~2 days, zero risk, 80 vocabulary entries catalog-derived instead of hardcoded. Layer B (`ByLeadingToken` index) should be bundled in for another hour of work — it's useful for tooling consumers regardless of parser wiring. Layer C (`DisambiguationToken`) is worth ~2 days for catalog completeness and LS/MCP value. Layer D is 4-5 weeks of high-risk work that doesn't improve parser correctness.

**The architecture is already catalog-driven where it matters.** The 80-entry vocabulary is the real correctness surface. Grammar productions at 11 constructs are too small to need a generic framework. The `Slots` field serves tooling consumers, not the parser runtime. That's the right boundary.
