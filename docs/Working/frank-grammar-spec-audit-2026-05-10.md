# Frank — Grammar & Spec Integrity Audit

> **Date:** 2026-05-10T15:32:08-04:00  
> **Scope:** `SupportsPostActionEnsure` damage assessment, out-of-band parser behavior search, grammar doc / language spec coherence  
> **Requested by:** Shane  
> **Verdict:** One out-of-band bug confirmed (the known target). No others found. Doc gaps exist but are cosmetic.

---

## § 1. Executive Summary

| Metric | Value |
|--------|-------|
| Out-of-band parser behaviors found | **1** — `SupportsPostActionEnsure` (the known target) |
| Other `Supports*` flags gating parser behavior | **0** |
| Grammar doc constructs with gaps | **3** (EventEnsure missing `when`, StateEnsure missing `when` in anatomy, StateAction missing `when` in family detail) |
| Language spec accuracy | **Good** — spec lines 856, 869 describe EventEnsure `when` and EventHandler `ensure` correctly |
| Systemic problem? | **No.** This is an isolated feature that violated grammar semantics. The architecture is otherwise clean. |

**Bottom line:** `SupportsPostActionEnsure` is the sole out-of-band parser injection. Its removal is straightforward (7 files, ~25 lines total). The only other issues are documentation gaps where the grammar doc doesn't show the `when` guard slot that the code and spec already support correctly.

---

## § 2. SupportsPostActionEnsure — Damage Assessment

### 2.1 What the grammar doc currently says about EventHandler

Grammar doc §4, "The `on` family" (line 414–421):

```
on  EventName  ->  actions                         → EventHandler
```

The second keyword (`ensure` vs `->`) is the disambiguation token.

The anatomy diagram (line 333–341):

```
on  Submit  ->  set ClaimAmount = Submit.Amount
 │    │       ↑             │
[1]  [2]  disambig.        [3]
           token
[1] Leading token: `on`   ← shared with EventEnsure
[2] EventTarget slot — the event whose handler is being declared
[3] ActionChain slot — one or more `-> action` steps for the event-scoped handler
```

**The grammar doc does NOT document the combined `on Event -> action ensure ... because ...` form.** This is correct — the combined form should never have existed.

### 2.2 What the language spec says

Language spec line 861–869:

```
#### Stateless event hook

on Identifier
("->" ActionStatement)*
("ensure" BoolExpr)?

Event hooks without a `when`/`ensure` continuation are parsed as stateless event hooks
with an arrow-prefixed action chain. The optional `ensure` clause at the end of the
action chain declares a post-condition guard — a boolean expression that must hold after
all mutations in the handler are applied.
```

**The language spec DOES describe the combined form.** It documents `("ensure" BoolExpr)?` as an optional trailing element. This must be corrected when the feature is removed.

Note: The spec also omits `("because" StringExpr)?` which the parser currently grafts alongside the ensure. So even the spec's description is incomplete relative to the implementation.

### 2.3 The test that covers the bad form

- **File:** `test/Precept.Tests/Parser/ParserSlice8Tests.cs`
- **Method:** `Parser_Bug054_EventHandlerPostActionEnsure_CompilesClean()` (line 181)
- **Content:** `on Rename -> set Name = next ensure Name != "" because "name required"`

Additional catalog tests:
- `test/Precept.Tests/CatalogCapability/ConstructCatalogCapabilityTests.cs` line 25: `EventHandler_SupportsPostActionEnsure_True()`
- `test/Precept.Tests/Language/Track2PhaseAConstructCatalogTests.cs` line 18: `EventHandler_SupportsPostActionEnsure_True()`

### 2.4 Complete file inventory for removal

| # | File | What changes |
|---|------|--------------|
| 1 | `src/Precept/Language/Construct.cs` line 25 | Remove `SupportsPostActionEnsure` parameter from `ConstructMeta` record |
| 2 | `src/Precept/Language/Constructs.cs` line 184 | Remove `SupportsPostActionEnsure: true` from EventHandler entry |
| 3 | `src/Precept/Pipeline/Parser.cs` lines 292–308 | Delete entire `if (meta.SupportsPostActionEnsure) { ... }` block |
| 4 | `test/Precept.Tests/Parser/ParserSlice8Tests.cs` lines 180–192 | Delete `Parser_Bug054_EventHandlerPostActionEnsure_CompilesClean` test |
| 5 | `test/Precept.Tests/CatalogCapability/ConstructCatalogCapabilityTests.cs` lines 24–28 | Delete `EventHandler_SupportsPostActionEnsure_True` test |
| 6 | `test/Precept.Tests/Language/Track2PhaseAConstructCatalogTests.cs` lines 17–19 | Delete `EventHandler_SupportsPostActionEnsure_True` test |
| 7 | `docs/language/precept-language-spec.md` lines 861–869 | Remove the `("ensure" BoolExpr)?` from grammar, rewrite prose to say EventHandler is `on Identifier ("->" ActionStatement)*` only |
| 8 | `docs/language/catalog-system.md` lines 1598, 1626 | Remove `SupportsPostActionEnsure` from the ConstructMeta shape documentation |

### 2.5 The correct grammar for the `on` family after removal

```
on  EventName  [when Guard]  ensure  Expr  because  "..."    → EventEnsure
on  EventName                ->  actions                     → EventHandler
```

EventHandler: `on Identifier ("->" ActionStatement)*` — no trailing ensure, no guard.
EventEnsure: `on Identifier ("when" BoolExpr)? ensure BoolExpr because StringExpr` — with optional pre-verb guard.

The disambiguation remains: `ensure` routes to EventEnsure, `->` routes to EventHandler. They are mutually exclusive paths.

---

## § 3. EventEnsure `when` Slot — Gap Assessment

### 3.1 Current state of grammar doc for EventEnsure

The grammar doc anatomy (line 269–277) shows:

```
on  Submit  ensure  Amount > 0  because  "…"
 │    │       ↑          │          ↑        │
[1]  [2]  disambig.     [3]        slot     [4]
          token                     marker
```

**Missing:** The `when` guard slot is NOT shown in this anatomy.

The family detail (line 417) shows:

```
on  EventName  ensure  Expr  because  "..."        → EventEnsure
```

**Missing:** No `[when Guard]` shown before `ensure`.

### 3.2 What it SHOULD say

The EventEnsure catalog entry (`Constructs.cs` line 165–173) has slots:
1. `SlotEventTarget` — the event name
2. `SlotPreVerbGuardEnsure` — optional `when` guard (terminates at `ensure`)
3. `SlotEnsureClause` — the constraint expression
4. `SlotOptBecauseClause` — optional reason

**Corrected anatomy:**

```
on  Submit  when  Submit.Type == "payment"  ensure  Submit.Amount > 0  because  "…"
 │    │       ↑            │                  ↑          │                ↑        │
[1]  [2]    slot          [3]              disambig.    [4]              slot     [5]
           marker                           token                       marker
[1] Leading token: `on`   ← shared with EventHandler
[2] EventTarget slot — the anchor event name
[3] GuardClause slot (optional) — `when` expression scoping when the ensure applies
[4] EnsureClause slot — the expression condition (`ensure <expression>`)
[5] BecauseClause slot (optional) — the explanatory reason
```

**Corrected family detail:**

```
on  EventName  [when Guard]  ensure  Expr  [because  "..."]   → EventEnsure
on  EventName                ->  actions                      → EventHandler
```

### 3.3 Current state of language spec

Language spec line 856:

```
on Identifier ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

**The spec is CORRECT.** It shows the optional `when` guard. The grammar doc is the one lagging.

### 3.4 Sample file evidence

**`samples/insurance-claim.precept` line 35:**

```precept
on Submit when Submit.RequiresPoliceReport ensure Submit.Amount <= 100000 because "Police-report claims are capped at $100,000"
```

This confirms the guarded EventEnsure form is a real, working feature that samples exercise.

### 3.5 Note on BecauseClause being optional

The catalog entry uses `SlotOptBecauseClause` (IsRequired: false). The language spec says "because is syntactically required on every rule and ensure" (line 48). However, the parser makes it optional to enable better error recovery — the type checker or a later stage enforces the requirement. The grammar doc should reflect this as optional-in-grammar, required-by-semantic-rule.

---

## § 4. Additional Out-of-Band Behaviors Found

### Exhaustive search results

#### 4.1 All boolean fields on `ConstructMeta`

| Field | Purpose | Gates parser behavior? |
|-------|---------|----------------------|
| `SupportsPostActionEnsure` | Post-slot-walk injection of ensure/because | **YES — THE BUG** |
| `IsOutlineNode` | Consumed by language server for document outline | No — not read by parser at all |

**That's it.** Only two boolean fields exist on `ConstructMeta`. `IsOutlineNode` is consumed exclusively by the language server's outline provider, never by the parser.

#### 4.2 Parser accesses to `meta.*` (beyond slot-walk)

All `meta.` accesses in `Parser.cs`:

| Line | Access | Purpose | Out-of-band? |
|------|--------|---------|--------------|
| 32–33 | `meta.IsValidAsMemberName` | TokenMeta (not ConstructMeta) — keyword-as-identifier check | No |
| 152 | `meta.RoutingFamily` | Dispatch: Direct vs Scoped | No — routing architecture |
| 247, 277, 286 | `meta.Slots` | Slot-walk iteration | No — core architecture |
| 271 | `meta.Entries` | Get disambiguation tokens for scoped constructs | No — core architecture |
| 292 | `meta.SupportsPostActionEnsure` | **The injection** | **YES — THE BUG** |
| 761 | `meta.Kind` (ModifierMeta) | Maps state modifier token to kind | No — different `meta` type |
| 873 | `meta.IsStateWildcard` (TokenMeta) | `any` keyword in state target | No — TokenMeta, not ConstructMeta |
| 923 | `meta.IsFieldBroadcast` (TokenMeta) | `all` keyword in field target | No — TokenMeta, not ConstructMeta |
| 1003, 1009, 1011 | `meta.Kind`, `meta.SyntaxShape` (ActionMeta) | Action parsing | No — ActionMeta, not ConstructMeta |

**No other out-of-band parser behaviors found.** The parser's ConstructMeta accesses are limited to: RoutingFamily (dispatch), Slots (slot-walk), Entries (disambiguation), and the one bug (`SupportsPostActionEnsure`).

#### 4.3 Injection patterns in Parser.cs

The only block that appends to parsed slot values after the main slot-walk is lines 292–308 (the `SupportsPostActionEnsure` block). No other post-loop injection exists.

The `ParseConstruct` method (line 240–259) is clean: consume leading keyword, walk slots, emit construct.  
The `ParseScopedConstruct` method (line 266–316) has one injection point: the `SupportsPostActionEnsure` block at line 292.

#### 4.4 SlotMeta flags

`ConstructSlot` has:
- `Kind` (enum) — determines parsing behavior
- `IsRequired` (bool) — determines whether missing slot is an error
- `Description` (string?) — documentation
- `TerminationTokens` (TokenKind[]?) — expression boundary markers

**No weird flags.** All four properties serve legitimate slot-walk mechanics. None gate out-of-band behavior.

### Verdict

**"No additional out-of-band behaviors found."** The evidence is:
1. Only two booleans on ConstructMeta; one is the bug, one is LS-only.
2. Every `meta.*` access in the parser is either routing/dispatch or slot-walk — except the one bug.
3. No post-loop injection blocks exist other than the one bug.
4. ConstructSlot has no behavior-gating flags.

---

## § 5. Grammar Doc Gaps — Construct-by-Construct

### 5.1 EventEnsure — missing `when` guard in anatomy

**What the doc says (line 269):**
```
on  Submit  ensure  Amount > 0  because  "…"
```
No `when` guard shown.

**What it should say:**
```
on  Submit  [when Guard]  ensure  Amount > 0  because  "…"
```
With GuardClause slot documented between EventTarget and EnsureClause.

**Fix type:** Doc-only. Code is correct.

### 5.2 EventEnsure — missing `when` in family detail

**What the doc says (line 417):**
```
on  EventName  ensure  Expr  because  "..."        → EventEnsure
```

**What it should say:**
```
on  EventName  [when Guard]  ensure  Expr  [because  "..."]  → EventEnsure
```

**Fix type:** Doc-only.

### 5.3 StateEnsure — missing `when` guard in anatomy

**What the doc says (line 258):**
```
in  Approved  ensure  ApprovedAmount > 0  because  "…"
```
No `when` guard shown.

**What it should say:**
```
in  Approved  [when Guard]  ensure  ApprovedAmount > 0  because  "…"
```

The catalog (`Constructs.cs` line 130) has `SlotPreVerbGuardEnsure` at position [1] (after StateTarget). The language spec (line 855) shows `("when" BoolExpr)?`. The grammar doc lags.

**Fix type:** Doc-only. Code is correct.

### 5.4 StateEnsure — missing `when` in `in` family detail

**What the doc says (line 407):**
```
in  [AnchorState]  ensure  Expr  because  "..."                    → StateEnsure
```

**What it should say:**
```
in  [AnchorState]  [when Guard]  ensure  Expr  [because  "..."]   → StateEnsure
```

Same gap for `to` and `from` families (lines 427, 436).

**Fix type:** Doc-only.

### 5.5 StateAction — missing `when` in family detail

**What the doc says (line 428):**
```
from  [AnchorState]  ->  actions                                              → StateAction
```

**What it should say:**
```
from  [AnchorState]  [when Guard]  ->  actions                                → StateAction
```

Same for `to` family (line 437). The catalog (`Constructs.cs` line 161) has `SlotPreVerbGuardArrow`.

The language spec (line 874) shows `("when" BoolExpr)?` for StateAction. The grammar doc lags.

**Fix type:** Doc-only.

### 5.6 BecauseClause optionality

The grammar doc anatomy shows `BecauseClause` as slot [4] without noting it's optional in the parser. The catalog uses `SlotOptBecauseClause` (IsRequired: false) for StateEnsure and EventEnsure. The diagram labels should indicate optionality where applicable.

**Fix type:** Doc-only (minor).

---

## § 6. Language Spec Gaps

### 6.1 Stateless event hook grammar — must be corrected after removal

**Section:** Line 861–869, "Stateless event hook"

**What it says:**
```
on Identifier
("->" ActionStatement)*
("ensure" BoolExpr)?
```
Plus prose: "The optional `ensure` clause at the end of the action chain declares a post-condition guard."

**What it should say after removal:**
```
on Identifier ("->" ActionStatement)*
```
With prose: "Event hooks without a `when`/`ensure` continuation are parsed as stateless event hooks with an arrow-prefixed action chain. No trailing ensure is supported."

### 6.2 EventHandlerDoesNotSupportGuard diagnostic description

**Section:** Line 1045

**What it says:** "Event handlers ('on Event -> action') do not support 'when' guards — guards are only valid on event ensures and transition rows"

**What it should say:** "…guards are only valid on event ensures, state ensures, state actions, access modes, and transition rows"

This is a minor accuracy gap — the current text omits state ensures, state actions, and access modes from the list of constructs that support guards.

### 6.3 No other grammar-related spec gaps found

The spec's grammar section (§2.2) correctly describes:
- `field` declaration (line 774–778) ✓
- `state` declaration ✓
- `event` declaration ✓  
- `rule` declaration ✓
- Transition row ✓
- State/event ensure with `when` (line 855–859) ✓
- State action with `when` (line 874–878) ✓
- Dispatch table (line 760–770) ✓

---

## § 7. Remediation Plan

### Priority 1: Code changes (SupportsPostActionEnsure removal)

| # | File | Change |
|---|------|--------|
| 1 | `src/Precept/Language/Construct.cs:25` | Remove `bool SupportsPostActionEnsure = false` parameter |
| 2 | `src/Precept/Language/Constructs.cs:184` | Remove `SupportsPostActionEnsure: true` from EventHandler |
| 3 | `src/Precept/Pipeline/Parser.cs:292-308` | Delete the entire `if (meta.SupportsPostActionEnsure) { ... }` block |

### Priority 2: Test deletions

| # | File | Change |
|---|------|--------|
| 4 | `test/Precept.Tests/Parser/ParserSlice8Tests.cs:180-192` | Delete `Parser_Bug054_EventHandlerPostActionEnsure_CompilesClean` |
| 5 | `test/Precept.Tests/CatalogCapability/ConstructCatalogCapabilityTests.cs:24-28` | Delete `EventHandler_SupportsPostActionEnsure_True` |
| 6 | `test/Precept.Tests/Language/Track2PhaseAConstructCatalogTests.cs:17-19` | Delete `EventHandler_SupportsPostActionEnsure_True` |

### Priority 3: Language spec corrections

| # | File | Section | Change |
|---|------|---------|--------|
| 7 | `docs/language/precept-language-spec.md:861-869` | "Stateless event hook" | Remove `("ensure" BoolExpr)?`, rewrite prose |
| 8 | `docs/language/precept-language-spec.md:1045` | Diagnostic table | Expand guard-supporting construct list |

### Priority 4: Grammar doc corrections

| # | File | Section | Change |
|---|------|---------|--------|
| 9 | `docs/language/precept-grammar.md:258-266` | StateEnsure anatomy | Add `[when Guard]` slot between StateTarget and EnsureClause |
| 10 | `docs/language/precept-grammar.md:269-277` | EventEnsure anatomy | Add `[when Guard]` slot between EventTarget and EnsureClause |
| 11 | `docs/language/precept-grammar.md:407` | `in` family detail | Add `[when Guard]` to StateEnsure form |
| 12 | `docs/language/precept-grammar.md:417` | `on` family detail | Add `[when Guard]` to EventEnsure form |
| 13 | `docs/language/precept-grammar.md:427-428` | `from` family detail | Add `[when Guard]` to StateEnsure and StateAction forms |
| 14 | `docs/language/precept-grammar.md:436-437` | `to` family detail | Add `[when Guard]` to StateEnsure and StateAction forms |

### Priority 5: Catalog documentation

| # | File | Section | Change |
|---|------|---------|--------|
| 15 | `docs/language/catalog-system.md:1598` | ConstructMeta shape table | Remove `SupportsPostActionEnsure` from shape |
| 16 | `docs/language/catalog-system.md:1626` | Full shape csharp block | Remove `SupportsPostActionEnsure` line |

---

## Appendix: Evidence Trail

### Constructs with `when` guard support (from `Constructs.cs`)

| Construct | Guard slot | Terminates at |
|-----------|-----------|---------------|
| TransitionRow | `SlotGuardClause` | `Because`, `Arrow` |
| StateEnsure | `SlotPreVerbGuardEnsure` | `Ensure` |
| AccessMode | `SlotPreVerbGuardModify` | `Modify` |
| StateAction | `SlotPreVerbGuardArrow` | `Arrow` |
| EventEnsure | `SlotPreVerbGuardEnsure` | `Ensure` |
| RuleDeclaration | `SlotGuardClause` | `Because`, `Arrow` |

Constructs that explicitly DO NOT support guards:
- `EventHandler` — diagnosed as `EventHandlerDoesNotSupportGuard`
- `OmitDeclaration` — diagnosed as `OmitDoesNotSupportGuard`
- `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration` — no guard slot in catalog

### Grammar doc coverage of `when` guards

| Construct | Guard in catalog? | Guard in grammar doc anatomy? | Guard in family detail? |
|-----------|:-:|:-:|:-:|
| TransitionRow | ✅ | ✅ (line 287) | ✅ (line 426) |
| StateEnsure | ✅ | ❌ | ❌ |
| AccessMode | ✅ | ✅ (line 318) | ✅ (line 408) |
| StateAction | ✅ | ❌ (no anatomy shown) | ❌ |
| EventEnsure | ✅ | ❌ | ❌ |
| RuleDeclaration | ✅ | ✅ (line 307) | N/A (Direct) |
