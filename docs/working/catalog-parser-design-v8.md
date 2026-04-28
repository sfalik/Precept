# Catalog-Driven Parser: Final Design & Implementation Plan (Round 8)

**By:** Frank (Language Designer / Compiler Architect)
**Date:** 2026-04-28
**Status:** Final design document — supersedes v7. All implementation work should reference v8.
**References:**
- `docs/working/catalog-parser-design-v7.md` — Round 7 (superseded; retained for audit trail)
- `docs/language/precept-language-spec.md` — DSL spec (law)
- `docs/language/catalog-system.md` — metadata-driven catalog architecture
- `src/Precept/Language/Constructs.cs` — current catalog source (reflects George's completed work)
- `src/Precept/Language/Construct.cs` — ConstructMeta record (pre-migration shape)
- `src/Precept/Language/ConstructSlot.cs` — ConstructSlot/ConstructSlotKind
- `src/Precept/Language/TokenKind.cs` — current token catalog (Modify/Readonly/Editable/Omit live)
- `CONTRIBUTING.md` — implementation plan quality bar

> **This document supersedes v7.** All implementation work should reference v8. v7 is retained in `docs/working/` for audit trail purposes but is no longer the canonical design anchor.

---

## §0 — Document Preamble

**What changed from v7 to v8:**

1. **OmitDeclaration is a separate construct** — v7 combined `modify` and `omit` under a single `AccessMode` disambiguation entry (`[TokenKind.Modify, TokenKind.Omit]`). v8 splits them: `AccessMode` gets `[new(TokenKind.In, [TokenKind.Modify])]`, `OmitDeclaration` gets `[new(TokenKind.In, [TokenKind.Omit])]`. This affects PR 1 entries, PR 2 BuildNode, PR 4 disambiguation routing, and all tests.

2. **FieldTargetNode is a discriminated union** — v7 left `FieldTargetNode` as a flat node. v8 requires abstract base + 3 sealed subtypes (`SingularFieldTarget`, `ListFieldTarget`, `AllFieldTarget`). This is architecturally non-negotiable per `docs/language/catalog-system.md` § Architectural Identity.

3. **`ByLeadingToken[In]` dispatches to 3 constructs** — was 2 in v7 (StateEnsure + AccessMode), now 3 (+ OmitDeclaration). Test data updated accordingly.

4. **Total ConstructKinds is 12** — was 11 in v7. `OmitDeclaration` is the addition (already in `Constructs.cs` — George completed this).

5. **v7's `InScoped_RoutesToAccessMode_WhenOmitFollowsState` test is wrong** — replaced with `InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState`.

6. **Soup Nazi test specs added per slice** — v7 had tests but not the per-slice Soup Nazi specification format.

7. **Proposal C (when as StateAction disambiguation token) is DEFERRED** — not incorporated.

---

## §1 — Session Decision Summary (All Locked Decisions)

### F12 — Access Mode Vocabulary: `modify`/`readonly`/`editable` (LOCKED)

**Decision:** The access-mode declaration surface uses `modify` as the verb and `readonly`/`editable` as the adjective pair. `omit` is the structural exclusion verb.

**Rationale:** The verb/adjective split is semantically clean — `modify` is the operation on the field's access configuration, the adjective names the resulting mode. `readonly`/`editable` are paradox-free with `modify` (unlike `fixed`, which creates a modify-to-fix tension). True verb parallelism with `omit`. Supersedes the B2 `editable`/`fixed` recommendation and the original `write`/`read` vocabulary.

**Key points:**
- `modify` = verb for access-mode declarations. Consumed by the disambiguator as the disambiguation token — NOT stored as a slot.
- `readonly` / `editable` = access mode adjectives. Stored in the `AccessModeKeyword` slot.
- `omit` = structural exclusion verb. Field is absent from the state entirely.
- Guard position is **post-field**: `in State modify Field readonly when Guard`.
- `omit` NEVER has a `when` guard clause — this is a **permanently locked invariant**, not a current-sprint decision. Structural field presence must never be data-dependent.
- `TokenKind.Write` and `TokenKind.Read` retired from access mode context. `TokenKind.Modify`, `TokenKind.Readonly`, `TokenKind.Editable` are live (already added by George).
- Root-level `write all` removed from the language (stateless precepts use `writable` modifier).

> **Source:** Shane's B4 vocabulary selection, Frank's B4 recommendation. Decisions.md 2026-04-28T06:41:30Z merge.

### AST Split — Separate Nodes, No Shared Parent (LOCKED)

**Decision:** `OmitDeclaration` is a SEPARATE construct from `AccessMode`. No shared parent node, no internal branching on verb.

**Rationale:** `omit` and `modify` differ in: (a) slot sequence (omit has 2 slots, access mode has 4), (b) guard eligibility (omit: never, access mode: optional), (c) semantic category (structural exclusion vs. mutability constraint). A shared node would require internal branching and nullable fields for the mode and guard that are structurally impossible for omit. Per `catalog-system.md` § Architectural Identity: "Do not use flat records with inapplicable nullable fields — use a DU instead."

**AST node definitions:**
- `OmitDeclarationNode`: `StateTargetNode State, FieldTargetNode Fields` — no Mode, no Guard, ever.
- `AccessModeNode`: `StateTargetNode State, FieldTargetNode Fields, Token Mode, Expression? Guard`

> **Source:** Shane approved Frank's proposal for separate AST nodes. Decisions.md 2026-04-28T06:41:30Z merge.

### FieldTarget Shorthand — Discriminated Union (LOCKED)

**Decision:** `FieldTargetNode` is a discriminated union — abstract base with three sealed subtypes.

**Three shapes:**
1. `SingularFieldTarget(SourceSpan Span, Token Name) : FieldTargetNode` — single field identifier
2. `ListFieldTarget(SourceSpan Span, ImmutableArray<Token> Names) : FieldTargetNode` — comma-separated field list
3. `AllFieldTarget(SourceSpan Span, Token AllToken) : FieldTargetNode` — the `all` keyword

**Rationale:** The three shapes carry different data (`Token` vs `ImmutableArray<Token>` vs keyword token). A flat record with nullable fields would have `Token? Name`, `ImmutableArray<Token>? Names`, `Token? AllToken` — three mutually exclusive nullable fields, which is the exact anti-pattern `catalog-system.md` prohibits. The DU ensures consumers switch on subtype and access only the fields that exist for that shape.

Both `modify` and `omit` support ALL THREE shapes.

> **Source:** Session plan decision checklist. Architecturally non-negotiable per catalog-system.md.

### Disambiguation — Separate Entries for AccessMode and OmitDeclaration (LOCKED)

**Decision:**
- `AccessMode` disambiguation entry: `[new(TokenKind.In, [TokenKind.Modify])]`
- `OmitDeclaration` disambiguation entry: `[new(TokenKind.In, [TokenKind.Omit])]`
- These are SEPARATE entries — NOT combined as `[Modify, Omit]` under a single AccessMode entry (which was v7's pre-split design).
- `ByLeadingToken[In]` dispatches to **3 constructs**: StateEnsure, AccessMode, OmitDeclaration.

**Routing:**
- `in State modify ...` → AccessMode
- `in State omit ...` → OmitDeclaration
- `in State ensure ...` → StateEnsure
- Anything else → diagnostic

> **Source:** Session plan, decisions.md 2026-04-28T06:41:30Z merge — OmitDeclaration is a separate construct with its own DisambiguationEntry.

### Slot Sequences (LOCKED)

- **AccessMode:** `[StateTarget, FieldTarget, AccessModeKeyword, GuardClause(opt)]` — 4 slots
- **OmitDeclaration:** `[StateTarget, FieldTarget]` — 2 slots. NO GuardClause slot, ever.

> **Source:** Already reflected in `Constructs.cs` (George's completed work). Verified against current source.

### Sync Tokens (LOCKED)

**Decision:** `SyncToNextDeclaration()` sync set must include `modify` and `omit` as tokens that can begin a new declaration context (they appear after the `in` leading token, so they are disambiguation signals, not leading tokens — but they serve as recovery anchors within `in`-scoped parse failures).

> **Source:** Session plan sync-point update. Decisions.md 2026-04-28T06:41:30Z.

### Token Catalog State (LOCKED — Completed by George)

- `TokenKind.Modify` — ADDED (new keyword, access mode verb)
- `TokenKind.Readonly` — ADDED (new keyword, access mode adjective)
- `TokenKind.Editable` — ADDED (new keyword, access mode adjective)
- `TokenKind.Omit` — ADDED (new keyword, structural exclusion verb)
- `TokenKind.Write` and `TokenKind.Read` — RETIRED from access mode context (remain in TokenKind enum for backward compatibility but have no access-mode consumer)

**Verified in source:** `src/Precept/Language/TokenKind.cs` lines 51–58 confirm all four new tokens are live under the `// ── Keywords: Access Modes (B4 — 2026-04-28)` section.

### `write all` Removal (LOCKED)

Root-level `write all` is removed from the Precept language entirely. Stateless precepts opt into mutability through field-level `writable` only. No root-level bulk access mode construct exists.

> **Source:** Decisions.md 2026-04-28T04:49:58Z — Shane owner directive.

---

## §2 — Complete 9-Form Grammar

All 9 valid forms. No other forms are valid.

### Access Mode Forms (6 — guarded)

```
in State modify Field readonly [when Guard]            ← singular access constraint
in State modify Field editable [when Guard]            ← singular access upgrade
in State modify F1, F2, ... readonly [when Guard]      ← list form, constraint
in State modify F1, F2, ... editable [when Guard]      ← list form, upgrade
in State modify all readonly [when Guard]              ← all form, constraint
in State modify all editable [when Guard]              ← all form, upgrade
```

### Omit Forms (3 — never guarded)

```
in State omit Field                                    ← singular structural exclusion
in State omit F1, F2, ...                             ← list form
in State omit all                                      ← all form (marker states)
```

### Grammar Production Rules

```
AccessModeDeclaration := "in" StateTarget "modify" FieldTarget AccessModeKeyword GuardClause?
OmitDeclaration       := "in" StateTarget "omit" FieldTarget

FieldTarget           := Identifier
                       | Identifier ("," Identifier)+
                       | "all"

AccessModeKeyword     := "readonly" | "editable"
GuardClause           := "when" Expression
StateTarget           := Identifier
```

### Parse Trace (Representative)

```
Input:  in Draft modify Amount, Balance readonly when not Finalized
Tokens: In Identifier Modify Identifier Comma Identifier Readonly When Not Identifier

1. Consume In → leading token, look up ByLeadingToken[In] → 3 candidates
2. Parse StateTarget → Identifier "Draft" → StateTargetNode
3. Peek disambiguation token → Modify → route to AccessMode
4. Consume Modify (disambiguation token, not stored as slot)
5. Parse FieldTarget → Identifier "Amount" Comma Identifier "Balance" → ListFieldTarget
6. Parse AccessModeKeyword → Readonly → TokenValueNode
7. Parse GuardClause → When, ParseExpression → "not Finalized" → Expression
8. BuildNode(AccessMode, [StateTarget, ListFieldTarget, TokenValue, Expression], span)
```

```
Input:  in Closed omit Amount
Tokens: In Identifier Omit Identifier

1. Consume In → leading token, look up ByLeadingToken[In] → 3 candidates
2. Parse StateTarget → Identifier "Closed" → StateTargetNode
3. Peek disambiguation token → Omit → route to OmitDeclaration
4. Consume Omit (disambiguation token, not stored as slot)
5. Parse FieldTarget → Identifier "Amount" → SingularFieldTarget
6. BuildNode(OmitDeclaration, [StateTarget, SingularFieldTarget], span)
```

---

## §3 — AST Node Specifications

### FieldTargetNode — Discriminated Union

```csharp
/// <summary>
/// Abstract base for field target shapes in access mode and omit declarations.
/// Three shapes: singular identifier, comma-separated list, or the "all" keyword.
/// </summary>
public abstract record FieldTargetNode(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Single named field: <c>Amount</c></summary>
public sealed record SingularFieldTarget(SourceSpan Span, Token Name)
    : FieldTargetNode(Span);

/// <summary>Comma-separated field list: <c>Amount, Balance, Notes</c></summary>
public sealed record ListFieldTarget(SourceSpan Span, ImmutableArray<Token> Names)
    : FieldTargetNode(Span);

/// <summary>The <c>all</c> keyword: every field in the state.</summary>
public sealed record AllFieldTarget(SourceSpan Span, Token AllToken)
    : FieldTargetNode(Span);
```

**Why a DU, not a flat record:** The three shapes carry structurally different data. A flat record would have three mutually exclusive nullable fields — the exact anti-pattern `catalog-system.md` § Architectural Identity prohibits. The DU ensures compile-time exhaustiveness in every consumer via pattern matching. This is architecturally non-negotiable.

### OmitDeclarationNode

```csharp
/// <summary>
/// Structural exclusion: field is absent from the state entirely.
/// <c>in State omit Field</c> — no guard, no access mode adjective, ever.
/// </summary>
public sealed record OmitDeclarationNode(
    SourceSpan Span,
    StateTargetNode State,
    FieldTargetNode Fields)
    : Declaration(Span);
```

**Slot mapping:** `Slots[0]` = StateTarget → `State`, `Slots[1]` = FieldTarget → `Fields`. No further slots.

### AccessModeNode

```csharp
/// <summary>
/// Access mode declaration: field present, mutability constrained.
/// <c>in State modify Field readonly|editable [when Guard]</c>
/// </summary>
public sealed record AccessModeNode(
    SourceSpan Span,
    StateTargetNode State,
    FieldTargetNode Fields,
    TokenValueNode Mode,
    Expression? Guard)
    : Declaration(Span);
```

**Slot mapping:** `Slots[0]` = StateTarget → `State`, `Slots[1]` = FieldTarget → `Fields`, `Slots[2]` = AccessModeKeyword → `Mode`, `Slots[3]` = GuardClause → `Guard`.

### Complete Declaration Node List (12 ConstructKinds)

| # | ConstructKind | Node Type | Slots |
|---|---------------|-----------|-------|
| 1 | PreceptHeader | PreceptHeaderNode | [IdentifierList] |
| 2 | FieldDeclaration | FieldDeclarationNode | [IdentifierList, TypeExpression, ModifierList?, ComputeExpression?] |
| 3 | StateDeclaration | StateDeclarationNode | [IdentifierList, StateModifierList?] |
| 4 | EventDeclaration | EventDeclarationNode | [IdentifierList, ArgumentList?] |
| 5 | RuleDeclaration | RuleDeclarationNode | [RuleExpression, GuardClause?, BecauseClause] |
| 6 | TransitionRow | TransitionRowNode | [StateTarget, EventTarget, GuardClause?, ActionChain?, Outcome] |
| 7 | StateEnsure | StateEnsureNode | [StateTarget, EnsureClause] |
| 8 | **AccessMode** | **AccessModeNode** | **[StateTarget, FieldTarget, AccessModeKeyword, GuardClause?]** |
| 9 | **OmitDeclaration** | **OmitDeclarationNode** | **[StateTarget, FieldTarget]** |
| 10 | StateAction | StateActionNode | [StateTarget, ActionChain] |
| 11 | EventEnsure | EventEnsureNode | [EventTarget, EnsureClause] |
| 12 | EventHandler | EventHandlerNode | [EventTarget, ActionChain] |

---

## §4 — Catalog State

### What George Already Completed (Do NOT Re-Implement in PR 1)

The following changes are already in `src/Precept/Language/` and verified against current source:

1. **TokenKind additions:** `Modify`, `Readonly`, `Editable`, `Omit` — all present in `TokenKind.cs` lines 55–58 under the `// ── Keywords: Access Modes (B4 — 2026-04-28)` section.

2. **TokenKind retirements:** `Write` and `Read` no longer serve as access mode keywords. They remain in the enum (comment documents retirement).

3. **ConstructKind.OmitDeclaration** — added as a separate enum member. Verified: `Constructs.GetMeta(ConstructKind.OmitDeclaration)` returns a valid entry.

4. **Constructs.cs slot sequences:**
   - `AccessMode`: `[SlotStateTarget, SlotFieldTarget, SlotAccessModeKeyword, SlotGuardClause]` — 4 slots, guard is optional ✓
   - `OmitDeclaration`: `[SlotStateTarget, SlotFieldTarget]` — 2 slots, no guard ✓

5. **ConstructSlotKind additions:** `AccessModeKeyword` and `FieldTarget` — both present in `ConstructSlot.cs`.

6. **Shared slot instances:** `SlotAccessModeKeyword` and `SlotFieldTarget` defined in `Constructs.cs`.

### What PR 1 Must Still Do (DisambiguationEntry Migration)

The current `ConstructMeta` record shape (`Construct.cs`) still uses `TokenKind LeadingToken` as a single field. The migration to `DisambiguationEntry[]` has not been done. PR 1 must:

1. **Create `DisambiguationEntry` record** — `src/Precept/Language/DisambiguationEntry.cs`
2. **Migrate `ConstructMeta`** — replace `TokenKind LeadingToken` with `ImmutableArray<DisambiguationEntry> Entries`
3. **Add `PrimaryLeadingToken` bridge property** — `public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;`
4. **Add `[Obsolete]` alias** for `LeadingToken` to ease consumer migration
5. **Rewrite `GetMeta()` entries** with correct `DisambiguationEntry` arrays for all 12 constructs
6. **Build derived indexes** — `ByLeadingToken`, `LeadingTokens`

**Critical entries table for GetMeta() (PR 1):**

| Construct | Entries | Notes |
|-----------|---------|-------|
| PreceptHeader | `[new(TokenKind.Precept)]` | Unique leading token |
| FieldDeclaration | `[new(TokenKind.Field)]` | Unique leading token |
| StateDeclaration | `[new(TokenKind.State)]` | Unique leading token |
| EventDeclaration | `[new(TokenKind.Event)]` | Unique leading token |
| RuleDeclaration | `[new(TokenKind.Rule)]` | Unique leading token |
| TransitionRow | `[new(TokenKind.From, [TokenKind.On])]` | Disambiguated |
| StateEnsure | `[new(TokenKind.In, [TokenKind.Ensure]), new(TokenKind.To, [TokenKind.Ensure]), new(TokenKind.From, [TokenKind.Ensure])]` | 3 entries |
| **AccessMode** | **`[new(TokenKind.In, [TokenKind.Modify])]`** | **v7 had `[Modify, Omit]` — SPLIT** |
| **OmitDeclaration** | **`[new(TokenKind.In, [TokenKind.Omit])]`** | **NEW separate entry** |
| StateAction | `[new(TokenKind.To, [TokenKind.Arrow]), new(TokenKind.From, [TokenKind.Arrow])]` | 2 entries |
| EventEnsure | `[new(TokenKind.On, [TokenKind.Ensure])]` | Disambiguated |
| EventHandler | `[new(TokenKind.On, [TokenKind.Arrow])]` | Disambiguated |

**Derived index verification:**
- **Unique leading token constructs:** 5 (Precept, Field, State, Event, Rule)
- **Disambiguated constructs:** 7 (TransitionRow, StateEnsure, AccessMode, OmitDeclaration, StateAction, EventEnsure, EventHandler)
- **Total constructs:** 12
- **Shared leading tokens:** `In` → 3 constructs, `To` → 2, `From` → 3, `On` → 2

---

## §5 — Architecture Summary (5-Layer Design)

### Layer A — Vocabulary FrozenDictionaries

Operator precedence, type keywords, modifier sets, action recognition — all derived from catalog metadata at startup. No hardcoded vocabulary in the parser.

### Layer B — Top-Level Dispatch

Keyword-dispatched loop. Leading token → dispatch to construct parser or disambiguator. `Constructs.ByLeadingToken` index provides the mapping. Unique leading tokens dispatch directly. Shared leading tokens enter Layer D.

### Layer C — Generic Slot Iteration

`ParseConstructSlots()` iterates `ConstructMeta.Slots`, calling `InvokeSlotParser()` per slot. CS8509 exhaustive switch on `ConstructSlotKind` ensures build-time enforcement.

### Layer D — Disambiguation

Four scoped prepositions (`in`, `to`, `from`, `on`) share a generic disambiguator.

**`In`-scoped (3 constructs — updated from v7's 2):**
- After `in`, consume state target, then peek:
  - `modify` → AccessMode. Consume modify, parse FieldTarget, parse AccessModeKeyword, optional GuardClause.
  - `omit` → OmitDeclaration. Consume omit, parse FieldTarget. DONE — no adjective, no guard.
  - `ensure` → StateEnsure.
  - Anything else → diagnostic.

**`To`-scoped (2 constructs):** StateEnsure (ensure), StateAction (arrow).

**`From`-scoped (3 constructs):** TransitionRow (on), StateEnsure (ensure), StateAction (arrow).

**`On`-scoped (2 constructs):** EventEnsure (ensure), EventHandler (arrow).

Pre-event guard rejection for `from`-scoped transition rows (F9). Post-field guard for access modes (F12).

### Layer E — Error Sync

`SyncToNextDeclaration()` recovers from parse errors by advancing to the next known leading token, derived from `Constructs.LeadingTokens`. The sync set includes all leading tokens from the `LeadingTokens` FrozenSet. Within `in`-scoped parse failures, `modify` and `omit` serve as additional recovery anchors.

### Validation Tiers (from v5)

- **Tier 1:** CS8509 compile-time exhaustiveness on `InvokeSlotParser` and `BuildNode` switches.
- **Tier 2:** CS8509 on `GetMeta()` — adding a ConstructKind without a GetMeta arm is a build error.
- **Tier 3:** Startup/test-time assertions — `AllConstructsHaveAtLeastOneEntry`, slot ordering drift tests, exhaustiveness tests.
- **Tier 4:** Documentation-driven discoverability — this document, code comments.

---

## §6 — Implementation Plan (PR Sequence)

### PR Dependency Graph

```
PR 1: Catalog Migration
  ↓
PR 2: Parser Infrastructure
  ↓
PR 3: Non-Disambiguated Constructs
  ↓
PR 4: Disambiguated Constructs — Simple
  ↓
PR 5: Disambiguated Constructs — From + Error Sync
```

Each PR depends on the previous. No parallelization between PRs.

---

### PR 1: Catalog Migration

**Goal:** Migrate `ConstructMeta` from single `LeadingToken` to `DisambiguationEntry[]`. Add `RuleExpression` slot kind. Rewrite `GetMeta()` with complete disambiguation entries for all 12 constructs. Establish validation test infrastructure.

#### Slice 1.1: `DisambiguationEntry` Record

**Create:** `src/Precept/Language/DisambiguationEntry.cs` (~15 lines)

```csharp
public sealed record DisambiguationEntry(
    TokenKind                      LeadingToken,
    ImmutableArray<TokenKind>?     DisambiguationTokens = null,
    ConstructSlotKind?             LeadingTokenSlot = null);
```

- `LeadingToken`: the keyword token that begins this construct form.
- `DisambiguationTokens`: for shared leading tokens, the tokens that distinguish this construct from siblings. Null for unique leading tokens.
- `LeadingTokenSlot`: when the leading token is ALSO slot content, identifies which slot receives the consumed token value. (No current consumer — `write all` removed; retained for future constructs.)

**Tests:** No standalone tests — validated transitively by Slice 1.4 tests.

**Soup Nazi Test Spec:**
- No dedicated tests for this slice. Record shape validated by downstream consumers in slices 1.4 and 1.5.

---

#### Slice 1.2: `ConstructMeta` Migration

**Modify:** `src/Precept/Language/Construct.cs`

- Replace `TokenKind LeadingToken` parameter with `ImmutableArray<DisambiguationEntry> Entries`.
- Add bridge property: `public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;`
- Add obsolete alias: `[Obsolete("Use PrimaryLeadingToken or Entries")] public TokenKind LeadingToken => PrimaryLeadingToken;`

Updated record signature:

```csharp
public sealed record ConstructMeta(
    ConstructKind                       Kind,
    string                              Name,
    string                              Description,
    string                              UsageExample,
    ConstructKind[]                     AllowedIn,
    IReadOnlyList<ConstructSlot>        Slots,
    ImmutableArray<DisambiguationEntry> Entries,
    string?                             SnippetTemplate = null)
{
    public IReadOnlyList<ConstructSlot> Slots { get; } = Slots;
    public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;

    [Obsolete("Use PrimaryLeadingToken or Entries")]
    public TokenKind LeadingToken => PrimaryLeadingToken;
}
```

**Tests:** Existing `ConstructsTests.LeadingToken_IsCorrect` theory — update to use `PrimaryLeadingToken`. All inline data rows must pass unchanged.

**Regression anchors:** All 19+ existing tests in `test/Precept.Tests/ConstructsTests.cs` must pass.

**Soup Nazi Test Spec:**
- `PrimaryLeadingToken_MatchesExpectedToken(ConstructKind, TokenKind)` — theory, all 12 kinds. Verifies bridge property returns same values as the old `LeadingToken` did.
- `Entries_IsNotEmpty_ForAllConstructs()` — fact. Verifies migration didn't leave any construct with empty entries.

---

#### Slice 1.3: `ConstructSlotKind.RuleExpression` Addition

**Modify:** `src/Precept/Language/ConstructSlot.cs` — add `RuleExpression` member.

**Modify:** `src/Precept/Language/Constructs.cs` — add shared slot instance:

```csharp
private static readonly ConstructSlot SlotRuleExpression = new(ConstructSlotKind.RuleExpression);
```

**Tests:** Covered by Slice 1.4 (the `RuleDeclaration` slot sequence test).

**Soup Nazi Test Spec:**
- Covered by slice 1.4 tests. No standalone tests needed.

---

#### Slice 1.4: `GetMeta()` Rewrite with Complete Entries

**Modify:** `src/Precept/Language/Constructs.cs` — rewrite entire `GetMeta()` switch body.

All 12 constructs get `Entries` arrays. The current `TokenKind LeadingToken` parameter is replaced with the `Entries` parameter using the entries table from §4.

**Critical v8 updates from v7:**
- `AccessMode` row: `[new(TokenKind.In, [TokenKind.Modify])]` — NOT `[Modify, Omit]` as in v7
- NEW row: `OmitDeclaration`: `[new(TokenKind.In, [TokenKind.Omit])]` — SEPARATE entry
- `RuleDeclaration` slots: `[SlotRuleExpression, SlotGuardClause, SlotBecauseClause]` (F8 correction)

**Slot sequence corrections from v7:**
- `RuleDeclaration`: `[SlotRuleExpression, SlotGuardClause, SlotBecauseClause]` (was `[SlotGuardClause, SlotBecauseClause]` in original code — F8 fix)
- All other slot sequences unchanged.

Full `GetMeta()` rewrite (~140 lines).

**Soup Nazi Test Spec:**

```
AllConstructsHaveAtLeastOneEntry()
  — For each ConstructMeta in Constructs.All, Entries must not be empty.

LeadingTokenSlot_OnlyUsedWhenLeadingTokenIsAlsoSlotContent()
  — For each entry with a non-null LeadingTokenSlot, the construct's Slots must contain that kind.

RuleDeclaration_HasRuleExpressionSlot()
  — RuleDeclaration has 3 slots: [RuleExpression, GuardClause(opt), BecauseClause].

AccessMode_HasCorrectSlotSequence()
  — AccessMode has 4 slots: [StateTarget, FieldTarget, AccessModeKeyword, GuardClause(opt)].

OmitDeclaration_HasCorrectSlotSequence()
  — OmitDeclaration has 2 slots: [StateTarget, FieldTarget]. No GuardClause.

OmitDeclaration_HasNoGuardSlot()
  — Explicitly verify OmitDeclaration.Slots does not contain ConstructSlotKind.GuardClause.

DisambiguatedConstructs_HaveCorrectEntryCount(ConstructKind, int)
  — Theory with rows:
    InlineData(ConstructKind.StateEnsure, 3)          // In, To, From
    InlineData(ConstructKind.AccessMode, 1)            // In only (modify)
    InlineData(ConstructKind.OmitDeclaration, 1)       // In only (omit) ← NEW
    InlineData(ConstructKind.StateAction, 2)            // To, From
    InlineData(ConstructKind.EventEnsure, 1)            // On (Ensure)
    InlineData(ConstructKind.EventHandler, 1)           // On (Arrow)
    InlineData(ConstructKind.TransitionRow, 1)          // From (On)

AccessMode_DisambiguationTokens_ContainModifyOnly()
  — AccessMode's In entry has DisambiguationTokens = [TokenKind.Modify]. Does NOT contain Omit.

OmitDeclaration_DisambiguationTokens_ContainOmitOnly()
  — OmitDeclaration's In entry has DisambiguationTokens = [TokenKind.Omit]. Does NOT contain Modify.

AllConstructs_UsageExample_IsNotNullOrEmpty()
  — Regression: every construct has a non-empty usage example string.
```

**Regression anchors:** `GetMeta_ReturnsForEveryConstructKind`, `All_ContainsEveryKindExactlyOnce`, `All_IsInDeclarationOrder`, `Total_Count`, `TransitionRow_HasGuardClauseAndActionChainAsOptional`, `TransitionRow_HasRequiredOutcomeSlot`, `AllConstructs_HaveSlots`, `KeyConstructs_HaveMinimumSlotCount`.

---

#### Slice 1.5: Derived Indexes

**Modify:** `src/Precept/Language/Constructs.cs` — add two static properties after `All`:

```csharp
public static IReadOnlyDictionary<TokenKind, ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)>>
    ByLeadingToken { get; } = All
        .SelectMany(meta => meta.Entries.Select(entry => (meta.Kind, entry)))
        .GroupBy(t => t.entry.LeadingToken)
        .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());

public static FrozenSet<TokenKind> LeadingTokens { get; } = All
    .SelectMany(m => m.Entries)
    .Select(e => e.LeadingToken)
    .ToFrozenSet();
```

**Soup Nazi Test Spec:**

```
EveryLeadingTokenMapsToAtLeastOneConstruct()
  — All leading tokens from Constructs.All entries exist as keys in ByLeadingToken.

LeadingTokens_ContainsAllExpectedTokens()
  — Verify: Field, State, Event, Rule, From, In, To, On, Precept all present.
  — Verify: Write is NOT present (retired from access mode context).

SharedLeadingTokens_HaveCorrectCandidateCount(TokenKind, int)
  — Theory with rows:
    InlineData(TokenKind.In, 3)       // StateEnsure + AccessMode + OmitDeclaration ← WAS 2 in v7
    InlineData(TokenKind.To, 2)       // StateEnsure + StateAction
    InlineData(TokenKind.From, 3)     // TransitionRow + StateEnsure + StateAction
    InlineData(TokenKind.On, 2)       // EventEnsure + EventHandler

UniqueLeadingTokens_HaveSingleCandidate()
  — For Precept, Field, State, Event, Rule: ByLeadingToken[token] has exactly 1 entry.

LeadingTokens_Count_IsCorrect()
  — FrozenSet has exactly 9 members (Precept, Field, State, Event, Rule, From, In, To, On).
```

---

#### Slice 1.6: Consumer Migration

**Modify:** `test/Precept.Tests/ConstructsTests.cs` — update `LeadingToken_IsCorrect` theory to use `PrimaryLeadingToken`.

**Check:** `tools/Precept.Mcp/` — search for `LeadingToken` references. Update to `PrimaryLeadingToken`. Currently MCP tools are minimal, so likely no changes needed.

**Check:** `tools/Precept.LanguageServer/` — search for `LeadingToken` references. Update to `PrimaryLeadingToken` or `Entries` as appropriate.

**Regression anchors:** Full existing test suite (all 3 test projects) must pass green.

**Soup Nazi Test Spec:**
- No new tests. Existing `LeadingToken_IsCorrect` theory updated in-place to use `PrimaryLeadingToken` — must pass with same inline data. Full regression run.

---

### PR 2: Parser Infrastructure

**Goal:** Build the parser skeleton — dispatch loop, generic slot iteration, `InvokeSlotParser` exhaustive switch, `BuildNode` exhaustive switch (12 arms), vocabulary FrozenDictionaries, AST node base types. No construct parsing yet — every parse method is a `throw new NotImplementedException()` stub.

**Depends on:** PR 1 (Entries, derived indexes, RuleExpression slot kind).

#### Slice 2.1a: Base Types + FieldTargetNode DU (~80 lines)

**Create:** `src/Precept/Pipeline/SyntaxNodes/` directory with base types:

- `SyntaxNode.cs` — abstract base: `public abstract record SyntaxNode(SourceSpan Span);`
- `Declaration.cs` — abstract: `public abstract record Declaration(SourceSpan Span) : SyntaxNode(Span);`
- `Expression.cs` — abstract: `public abstract record Expression(SourceSpan Span) : SyntaxNode(Span);`
- `FieldTargetNode.cs` — abstract base + 3 sealed subtypes (`SingularFieldTarget`, `ListFieldTarget`, `AllFieldTarget`) as specified in §3
- Shared node types: `StateTargetNode`, `EventTargetNode`, `TokenValueNode`, `BecauseClauseNode`

**Soup Nazi Test Spec:**

```
FieldTargetNode_SingularSubtype_IsSealed()
  — SingularFieldTarget is sealed.

FieldTargetNode_ListSubtype_IsSealed()
  — ListFieldTarget is sealed.

FieldTargetNode_AllSubtype_IsSealed()
  — AllFieldTarget is sealed.

SyntaxNodeHierarchy_DeclarationExtendsBase()
  — Declaration inherits SyntaxNode.

FieldTargetNode_IsAbstract()
  — FieldTargetNode is abstract.
```

**Dependency:** Slice 2.1b depends on this slice — `FieldTargetNode` must exist before declaration nodes that carry it as a property.

---

#### Slice 2.1b: Declaration Nodes (~120 lines)

**Create:** One concrete `Declaration` subtype per `ConstructKind` (12 records total, ~10 lines each):

- `FieldDeclarationNode`, `StateDeclarationNode`, `EventDeclarationNode`, `RuleDeclarationNode`, `TransitionRowNode`, `StateEnsureNode`, `AccessModeNode`, `OmitDeclarationNode`, `StateActionNode`, `EventEnsureNode`, `EventHandlerNode`, `PreceptHeaderNode`
- Remaining shared node types: `IdentifierListNode`, `TypeRefNode`, `ModifierListNode`, `ArgumentListNode`, `ActionChainNode`, `OutcomeNode`

**Note:** Total ConstructKinds is now **12** (was 11 in v7) due to OmitDeclaration addition. Every declaration node must be accounted for.

**Soup Nazi Test Spec:**

```
AllDeclarationNodes_AreSealedRecords()
  — For each of the 12 declaration node types, verify: is sealed, is record, inherits Declaration.

FieldTargetNode_SubtypesHaveCorrectProperties()
  — SingularFieldTarget has exactly 2 properties: Span, Name.
  — ListFieldTarget has exactly 2 properties: Span, Names.
  — AllFieldTarget has exactly 2 properties: Span, AllToken.

OmitDeclarationNode_HasExactlyTwoSlotProperties()
  — Properties: State (StateTargetNode), Fields (FieldTargetNode). No Mode, no Guard.
  — Assert absence: no Mode property, no Guard property.

AccessModeNode_HasFourSlotProperties()
  — Properties: State, Fields, Mode, Guard (nullable).

DeclarationNodeCount_Matches_ConstructKindCount()
  — Number of concrete Declaration subtypes == Enum.GetValues<ConstructKind>().Length == 12.
```

**Dependency:** Requires Slice 2.1a.

---

#### Slice 2.2: Vocabulary FrozenDictionaries (Layer A)

**Modify:** `src/Precept/Pipeline/Parser.cs` — add vocabulary lookups:

- `OperatorPrecedence: FrozenDictionary<TokenKind, (int Precedence, bool RightAssociative)>`
- `TypeKeywords: FrozenSet<TokenKind>`
- `ModifierKeywords: FrozenSet<TokenKind>`
- `ActionKeywords: FrozenSet<TokenKind>`

~30 lines total.

**Soup Nazi Test Spec:**

```
VocabularyDictionaries_ArePopulatedFromCatalogs()
  — OperatorPrecedence contains TokenKind.Plus, Star, And, Or.
  — TypeKeywords contains TokenKind.StringType, IntegerType, MoneyType.
  — ModifierKeywords contains TokenKind.Nonnegative, Positive.
  — ActionKeywords contains TokenKind.Set, Add, Remove.

VocabularyDictionaries_AreNonEmpty()
  — Each dictionary/set has at least 1 entry.
```

---

#### Slice 2.3: `InvokeSlotParser` Exhaustive Switch

**Modify:** `src/Precept/Pipeline/Parser.cs` — add `InvokeSlotParser` method with exhaustive switch on `ConstructSlotKind`. All 16 slot kinds must have arms. Each `Parse*` method is initially a `throw new NotImplementedException()` stub.

CS8509 enforcement: adding a new `ConstructSlotKind` member without an arm is a build error.

**Soup Nazi Test Spec:**

```
EveryConstructSlotKindIsUsedByAtLeastOneConstruct()
  — All ConstructSlotKind members appear in at least one construct's Slots sequence.

InvokeSlotParser_HasArmForEverySlotKind()
  — For each ConstructSlotKind, calling InvokeSlotParser does NOT throw ArgumentOutOfRangeException.
  — (It may throw NotImplementedException from the stub — that's expected.)
```

---

#### Slice 2.4: `BuildNode` Exhaustive Switch

**Modify:** `src/Precept/Pipeline/Parser.cs` — add public `BuildNode` method with **12 arms** (updated from v7's 11):

```csharp
public static Declaration BuildNode(ConstructKind kind, SyntaxNode?[] slots, SourceSpan span) => kind switch
{
    ConstructKind.PreceptHeader     => new PreceptHeaderNode(span, ...),
    ConstructKind.FieldDeclaration  => new FieldDeclarationNode(span, ...),
    ConstructKind.StateDeclaration  => new StateDeclarationNode(span, ...),
    ConstructKind.EventDeclaration  => new EventDeclarationNode(span, ...),
    ConstructKind.RuleDeclaration   => new RuleDeclarationNode(span, ...),
    ConstructKind.TransitionRow     => new TransitionRowNode(span, ...),
    ConstructKind.StateEnsure       => new StateEnsureNode(span, ...),
    ConstructKind.AccessMode        => new AccessModeNode(span, ...),
    ConstructKind.OmitDeclaration   => new OmitDeclarationNode(span,
        (StateTargetNode)slots[0]!, (FieldTargetNode)slots[1]!),         // ← NEW arm
    ConstructKind.StateAction       => new StateActionNode(span, ...),
    ConstructKind.EventEnsure       => new EventEnsureNode(span, ...),
    ConstructKind.EventHandler      => new EventHandlerNode(span, ...),
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
        $"Unknown ConstructKind: {kind}")
};
```

**Soup Nazi Test Spec:**

```
BuildNodeHandlesEveryConstructKind()
  — T1 resolution pattern: for each ConstructKind, call BuildNode with null-filled slots.
    ArgumentOutOfRangeException = gap (fail).
    Any other exception = arm exists (pass).
  — Must iterate all 12 kinds.

BuildNode_OmitDeclaration_CastsCorrectly()
  — Provide real StateTargetNode + FieldTargetNode (SingularFieldTarget).
  — Result should be OmitDeclarationNode with correct State and Fields.

BuildNode_AccessMode_CastsCorrectly()
  — Provide real StateTargetNode + FieldTargetNode + TokenValueNode + null Guard.
  — Result should be AccessModeNode.
```

---

#### Slice 2.5: `ParseConstructSlots` Generic Loop

**Modify:** `src/Precept/Pipeline/Parser.cs` — add generic slot iteration (~15 lines).

**Tests:** Integration tests deferred to PR 3 (needs actual slot parsers).

**Soup Nazi Test Spec:**
- Deferred to PR 3. Slot parsers are stubs at this point.

---

#### Slice 2.6: Slot-Ordering Drift Tests

**Create:** `test/Precept.Tests/SlotOrderingDriftTests.cs`

**Soup Nazi Test Spec:**

```
PreParsedInjection_AnchorSlotIsAlwaysAtIndex0()
  — For all constructs with In/To/From/On leading tokens, Slots[0] is StateTarget or EventTarget.

PreParsedInjection_GuardSlotPositionMatchesExpectation()
  — TransitionRow: GuardClause at index 2.
  — AccessMode: GuardClause at index 3 (post-field, F12).
  — OmitDeclaration: NO GuardClause slot at any index. ← NEW check
  — StateEnsure: no guard slot (guard is embedded in ensure expression).

PreParsedInjection_OnlyRecognizedConstructsUseInjectionPath()
  — Only constructs with In/To/From/On entries use injection.

OmitDeclaration_NeverHasGuardAtAnySlotPosition()
  — Dedicated regression anchor: iterate OmitDeclaration.Slots and assert none are GuardClause.
```

**Regression anchors:** All PR 1 tests pass unchanged.

---

### PR 3: Non-Disambiguated Constructs

**Goal:** Implement parsing for the 5 constructs with unique leading tokens: `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, `RuleDeclaration`. Replace `NotImplementedException` stubs with real parsers.

**Depends on:** PR 2 (parser skeleton, AST nodes, slot infrastructure).

#### Slice 3.1: Core Expression Parser (Pratt)

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `ParseExpression(int minPrecedence)`:

- Pratt precedence climbing using `OperatorPrecedence` vocabulary dictionary.
- Atom parsing: identifiers, literals, function calls, parenthesized expressions.
- Infix/prefix operators.
- Natural termination on tokens with no left-binding power.

~100–150 lines. Largest single method.

**Soup Nazi Test Spec:**

```
ParseExpression_ProducesPrecedenceCorrectTree(string input, string expected)
  — Theory: "amount > 0", "a + b * c" → "a + (b * c)", "not active", "x > 0 and y > 0".
  — ~15 cases covering: arithmetic precedence, logical precedence, prefix not, parenthesized, function calls,
    string literals, number literals, boolean literals, member access (dot), nested parentheses.

ParseExpression_TerminatesAtBoundaryTokens()
  — Expression parsing stops at: when, because, ->, keywords. Does not consume them.
  — ~5 cases: "amount > 0 when ...", "amount > 0 because ...", "amount > 0 -> ...".

ParseExpression_EmptyInput_ProducesDiagnostic()
  — Edge case: no expression tokens → diagnostic emitted.
```

---

#### Slice 3.2: Slot Parsers for Simple Constructs

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement:

| Method | ~Lines |
|--------|--------|
| `ParseIdentifierList()` | ~15 |
| `ParseTypeExpression()` | ~20 |
| `ParseModifierList()` | ~15 |
| `ParseStateModifierList()` | ~15 |
| `ParseArgumentList()` | ~25 |
| `ParseComputeExpression()` | ~10 |
| `ParseRuleExpression()` | ~5 |
| `ParseGuardClause()` | ~8 |
| `ParseBecauseClause()` | ~8 |

**Soup Nazi Test Spec:**

```
ParseIdentifierList_SingleIdentifier()
ParseIdentifierList_MultipleCommaSeparated()
ParseIdentifierList_TrailingComma_ProducesDiagnostic()

ParseTypeExpression_SimpleType()
ParseTypeExpression_CollectionType()  // set of string, queue of integer
ParseTypeExpression_MissingAs_ProducesDiagnostic()

ParseModifierList_SingleModifier()
ParseModifierList_MultipleModifiers()
ParseModifierList_NoModifiers_ReturnsNull()

ParseStateModifierList_Terminal_Success()
ParseStateModifierList_Initial()
ParseStateModifierList_NoModifiers_ReturnsNull()

ParseArgumentList_SingleArg()
ParseArgumentList_MultipleArgs()
ParseArgumentList_MissingCloseParen_ProducesDiagnostic()
ParseArgumentList_NoParens_ReturnsNull()

ParseComputeExpression_SimpleExpression()
ParseComputeExpression_NoEquals_ReturnsNull()

ParseRuleExpression_DirectExpression()  // no intro token (F8)

ParseGuardClause_WhenPresent()
ParseGuardClause_WhenAbsent_ReturnsNull()

ParseBecauseClause_StringLiteral()
ParseBecauseClause_MissingBecause_ProducesDiagnostic()
```

---

#### Slice 3.3: Top-Level Dispatch (Unique Tokens)

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement dispatch loop.

**Soup Nazi Test Spec:**

```
Parse_MultipleDeclarations_ProducesCorrectNodeCount()
  — Multi-declaration input: precept + field + state + event + rule → 5 nodes.

Parse_FieldDeclaration_ValidInputs(string input)
  — Theory: "field amount as money nonnegative", "field name, description as string",
    "field total as money = principal + interest". ~5 cases.

Parse_StateDeclaration_ValidInputs(string input)
  — Theory: "state Draft initial, Submitted, Approved terminal success", "state Active". ~3 cases.

Parse_EventDeclaration_ValidInputs(string input)
  — Theory: "event Submit", "event Submit(approver as string)", "event Create initial". ~3 cases.

Parse_RuleDeclaration_ValidInputs(string input)
  — Theory: "rule amount > 0 because \"Amount must be positive\"",
    "rule amount > 0 when Active because \"msg\"". ~3 cases.

Parse_PreceptHeader_ValidInputs(string input)
  — "precept LoanApplication", "precept TestApp". ~2 cases.

Parse_UnknownLeadingToken_ProducesDiagnosticAndSyncs()
  — Input with garbage token → diagnostic, skips to next valid declaration.
```

**Regression anchors:** All PR 1 and PR 2 tests pass.

---

### PR 4: Disambiguated Constructs — Simple

**Goal:** Implement the generic disambiguator and parsing for constructs with `in`, `to`, and `on` leading tokens.

**Depends on:** PR 3 (slot parsers, expression parser).

#### Slice 4.1: Generic Disambiguator

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `DisambiguateAndParse()` (~40 lines).

The disambiguator:
1. Leading token already consumed by caller.
2. Parse anchor target (state or event name).
3. Pre-consume optional `when` guard (G1 — stash for later injection).
4. Match current token against each candidate's `DisambiguationTokens`.
5. Route to matched construct.
6. No match → diagnostic + error recovery.

**Soup Nazi Test Spec:**

```
Disambiguator_NoMatchingToken_EmitsDiagnosticAndSyncs()
  — Input like "in Draft foobar Amount" → diagnostic for unexpected disambiguation token.

Disambiguator_EmptyInput_AfterAnchor_EmitsDiagnostic()
  — Input "in Draft" with no further tokens → diagnostic.
```

---

#### Slice 4.2: Anchor + Guard Injection

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `ParseDisambiguatedConstruct()` (~25 lines).

1. Look up `ConstructMeta` for routed kind.
2. Allocate slot array.
3. Inject pre-parsed anchor into `slots[0]`.
4. Inject stashed guard into `GuardClause` slot index (if present).
5. Call `ParseConstructSlots()` for remaining slots.
6. Call `BuildNode()`.

**Soup Nazi Test Spec:**

```
GuardInjection_StashedGuard_LandsInCorrectSlot()
  — Parse "in Draft when Active ensure amount > 0 because \"msg\"". Guard lands in StateEnsure's GuardClause slot.

AnchorInjection_StateTarget_LandsAtSlot0()
  — For any in-scoped construct, State is at slot index 0.

ParseOmit_WithPreFieldStashedGuard_EmitsDiagnosticAndParses()
  — "in Closed when SomeCondition omit Amount" → DiagnosticCode.OmitDoesNotSupportGuard.
  — Parse still completes → OmitDeclarationNode with no guard.
```

**Stashed guard + OmitDeclaration behavior:**

> When the stashed guard is non-null AND the routed construct is `OmitDeclaration`, the parser MUST emit `DiagnosticCode.OmitDoesNotSupportGuard` (same code as the post-field guard rejection in Slice 4.4). The guard span should be used for the diagnostic location. Error recovery: proceed with parsing OmitDeclaration without injecting the guard (there is no slot for it). The resulting `OmitDeclarationNode` has no guard.

**Pre-field stashed guard test (full implementation):**

```csharp
[Fact]
public void ParseOmit_WithPreFieldStashedGuard_EmitsDiagnosticAndParses()
{
    // Covers the pre-consumed guard landing on OmitDeclaration (no injection slot).
    // The grammar "in State when Guard omit Field" pre-stashes the guard in the
    // disambiguator, then routes to OmitDeclaration which has no GuardClause slot.
    var tree = Parse(Lex("in Closed when SomeCondition omit Amount"));
    tree.Diagnostics.Should().ContainSingle(d =>
        d.Code == DiagnosticCode.OmitDoesNotSupportGuard);
    // Parse still completes — OmitDeclarationNode with no guard
    tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
}
```

---

#### Slice 4.3: Remaining Slot Parsers

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement:

| Method | ~Lines | Behavior |
|--------|--------|----------|
| `ParseStateTarget()` | ~10 | Consume identifier or `Any` quantifier. |
| `ParseEventTarget()` | ~10 | Consume identifier. |
| `ParseEnsureClause()` | ~10 | Check for `Ensure`, consume, call `ParseExpression(0)`. |
| `ParseActionChain()` | ~25 | Peek for `->` (F3), consume arrows and action statements. |
| `ParseOutcome()` | ~20 | `transition State` / `no transition` / `reject "msg"`. |
| `ParseAccessModeKeyword()` | ~8 | Match `Readonly`/`Editable`, return `TokenValueNode`. (`Modify` consumed by disambiguator, not stored.) |
| `ParseFieldTarget()` | ~20 | Returns `FieldTargetNode` (DU subtype). See below. |

**`ParseFieldTarget()` specification:**

```
1. If current token is `All` → consume, return AllFieldTarget(span, allToken)
2. If current token is Identifier:
   a. Consume first identifier
   b. If next token is Comma → enter list mode:
      - Consume comma, consume next identifier, repeat while comma follows
      - Return ListFieldTarget(span, names)
   c. Else → return SingularFieldTarget(span, name)
3. Else → diagnostic "Expected field name or 'all'"
```

**Soup Nazi Test Spec:**

```
ParseStateTarget_Identifier()
ParseStateTarget_AnyQuantifier()
ParseStateTarget_Missing_ProducesDiagnostic()

ParseEventTarget_Identifier()
ParseEventTarget_Missing_ProducesDiagnostic()

ParseEnsureClause_ValidExpression()
ParseEnsureClause_MissingEnsure_ProducesDiagnostic()

ParseActionChain_SingleAction()
ParseActionChain_MultipleActions()
ParseActionChain_MissingArrow_ReturnsNull()

ParseOutcome_Transition()
ParseOutcome_NoTransition()
ParseOutcome_Reject()
ParseOutcome_MissingOutcome_ProducesDiagnostic()

ParseAccessModeKeyword_Readonly()
ParseAccessModeKeyword_Editable()
ParseAccessModeKeyword_InvalidToken_ProducesDiagnostic()

ParseFieldTarget_SingleIdentifier_ReturnsSingular()
  — "Amount" → SingularFieldTarget.

ParseFieldTarget_CommaSeparatedList_ReturnsList()
  — "Amount, Balance, Notes" → ListFieldTarget with 3 names.

ParseFieldTarget_AllKeyword_ReturnsAll()
  — "all" → AllFieldTarget.

ParseFieldTarget_EmptyInput_ProducesDiagnostic()

ParseFieldTarget_TrailingComma_ProducesDiagnostic()
  — "Amount," with no following identifier → diagnostic.
```

---

#### Slice 4.4: `In`-Scoped Constructs

Implements parsing for:
- `AccessMode` via `in State modify Field readonly|editable [when Guard]`
- `OmitDeclaration` via `in State omit Field` — **routes to OmitDeclarationNode, NOT AccessModeNode**
- `StateEnsure` via `in State [when Guard] ensure Expr because Msg`

**v8 disambiguation for `In`-scoped constructs:**
1. Consume `in` and parse state target.
2. Peek for disambiguation token:
   - `modify` → route to `AccessMode`. Consume modify, parse FieldTarget, parse AccessModeKeyword, optional GuardClause.
   - `omit` → route to `OmitDeclaration`. Consume omit, parse FieldTarget. DONE — no adjective, no guard.
   - `ensure` → route to `StateEnsure`.
   - Anything else → diagnostic.

**⚠️ v7 CORRECTION:** v7's test `InScoped_RoutesToAccessMode_WhenOmitFollowsState` was WRONG — it asserted `BeOfType<AccessModeNode>()` for `omit` input. The correct test is:

```csharp
[Fact]
public void InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState()
{
    var tree = Parse(Lex("in Closed omit Amount"));
    tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
    // NOT AccessModeNode — omit routes to its own construct
}
```

**Soup Nazi Test Spec:**

```
// ═══ Access Mode — all 6 forms ═══
ParseAccessMode_SingularReadonly()
  — "in Draft modify Amount readonly" → AccessModeNode, SingularFieldTarget, Mode=Readonly.

ParseAccessMode_SingularEditable()
  — "in Draft modify Amount editable" → AccessModeNode, SingularFieldTarget, Mode=Editable.

ParseAccessMode_SingularWithGuard()
  — "in Processing modify Amount readonly when not Finalized" → AccessModeNode, Guard present.

ParseAccessMode_ListReadonly()
  — "in Red modify VehiclesWaiting, LeftTurnQueued readonly" → AccessModeNode, ListFieldTarget.

ParseAccessMode_ListEditable()
  — "in Draft modify EmployeeName, Department editable" → AccessModeNode, ListFieldTarget.

ParseAccessMode_ListWithGuard()
  — "in Processing modify Amount, Balance readonly when Active" → AccessModeNode, ListFieldTarget, Guard present.

ParseAccessMode_AllReadonly()
  — "in Approved modify all readonly" → AccessModeNode, AllFieldTarget.

ParseAccessMode_AllEditable()
  — "in Draft modify all editable" → AccessModeNode, AllFieldTarget.

ParseAccessMode_AllWithGuard()
  — "in Approved modify all readonly when not Override" → AccessModeNode, AllFieldTarget, Guard.

// ═══ Omit — all 3 forms ═══
ParseOmit_Singular()
  — "in Closed omit Amount" → OmitDeclarationNode, SingularFieldTarget.

ParseOmit_List()
  — "in Archived omit Notes, Attachments" → OmitDeclarationNode, ListFieldTarget with 2 names.

ParseOmit_All()
  — "in Terminal omit all" → OmitDeclarationNode, AllFieldTarget.

// ═══ Omit — NO guard, ever ═══
ParseOmit_NeverHasGuard()
  — For each omit form, verify: result node has NO Guard property at all (structural impossibility).

// ═══ Disambiguation routing ═══
InScoped_RoutesToAccessMode_WhenModifyFollowsState()
  — "in Draft modify Amount editable" → BeOfType<AccessModeNode>().

InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState()      ← REPLACES v7's WRONG test
  — "in Closed omit Amount" → BeOfType<OmitDeclarationNode>().

InScoped_RoutesToStateEnsure_WhenEnsureFollowsState()
  — "in Draft ensure amount > 0 because \"msg\"" → BeOfType<StateEnsureNode>().

InScoped_UnknownToken_AfterState_ProducesDiagnostic()
  — "in Draft foobar Amount" → diagnostic.

// ═══ Omit — post-field guard rejection ═══
ParseOmit_WithPostFieldGuard_EmitsDiagnostic()
  — "in Closed omit Amount when Active" → DiagnosticCode.OmitDoesNotSupportGuard.
  — Error recovery: construct still parses with guard consumed/discarded → OmitDeclarationNode.

// ═══ State Ensure ═══
ParseStateEnsure_InScoped(string input)
  — "in Approved ensure amount > 0 because \"Approved amount positive\"" → StateEnsureNode.
```

**Post-field guard rejection test (full implementation):**

```csharp
[Fact]
public void ParseOmit_WithPostFieldGuard_EmitsDiagnostic()
{
    // "omit" never supports a guard — this is a permanently locked invariant.
    var tree = Parse(Lex("in Closed omit Amount when Active"));
    tree.Diagnostics.Should().ContainSingle(d =>
        d.Code == DiagnosticCode.OmitDoesNotSupportGuard);
    // Error recovery: construct still parses with guard consumed/discarded
    tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
}
```

> **Note:** `DiagnosticCode.OmitDoesNotSupportGuard` is a new diagnostic code that must be added to `src/Precept/Language/DiagnosticCode.cs` in Slice 4.4 or earlier.

---

#### Slice 4.5: `To`-Scoped Constructs

- `StateEnsure` via `to State ensure Expr because Msg`
- `StateAction` via `to State -> Actions`

**Soup Nazi Test Spec:**

```
ToScoped_RoutesToStateEnsure_WhenEnsureFollows()
  — "to Approved ensure amount > 0 because \"msg\"" → StateEnsureNode.

ToScoped_RoutesToStateAction_WhenArrowFollows()
  — "to Submitted -> set submittedAt = now()" → StateActionNode.

ToScoped_UnknownToken_ProducesDiagnostic()
  — "to Submitted foobar" → diagnostic.

ParseStateAction_SingleAction()
ParseStateAction_MultipleActions()
```

---

#### Slice 4.6: `On`-Scoped Constructs

- `EventEnsure` via `on Event ensure Expr because Msg`
- `EventHandler` via `on Event -> Actions`

**Soup Nazi Test Spec:**

```
OnScoped_RoutesToEventEnsure_WhenEnsureFollows()
  — "on Submit ensure reviewer != \"\" because \"Reviewer required\"" → EventEnsureNode.

OnScoped_RoutesToEventHandler_WhenArrowFollows()
  — "on UpdateName -> set name = newName" → EventHandlerNode.

OnScoped_UnknownToken_ProducesDiagnostic()
  — "on Submit foobar" → diagnostic.
```

**Regression anchors:** All PR 1–3 tests pass.

---

### PR 5: Disambiguated Constructs — From + Error Sync

**Goal:** Implement `from`-scoped three-way disambiguation, pre-event guard rejection (F9), and `SyncToNextDeclaration()` error recovery.

**Depends on:** PR 4 (disambiguator, all slot parsers).

#### Slice 5.1: `From`-Scoped Three-Way Disambiguation

`from` has three candidates:

| Token after guard | Routes to |
|-------------------|-----------|
| `On` | `TransitionRow` |
| `Ensure` | `StateEnsure` |
| `Arrow` (`->`) | `StateAction` |

The generic disambiguator from PR 4 handles this — no special case needed.

**Soup Nazi Test Spec:**

```
FromScoped_RoutesToTransitionRow_WhenOnFollows()
  — "from Draft on Submit -> transition Submitted" → TransitionRowNode.

FromScoped_RoutesToStateEnsure_WhenEnsureFollows()
  — "from Submitted ensure amount > 0 because \"msg\"" → StateEnsureNode.

FromScoped_RoutesToStateAction_WhenArrowFollows()
  — "from Submitted -> set submittedAt = now()" → StateActionNode.

FromScoped_UnknownToken_ProducesDiagnostic()
  — "from Submitted foobar" → diagnostic.
```

---

#### Slice 5.2: Pre-Event Guard Rejection (F9)

When a `from`-scoped construct routes to `TransitionRow` (disambiguation token is `On`) AND a guard was pre-consumed (stashedGuard is not null), emit `DiagnosticCode.PreEventGuardNotAllowed`.

**Add:** `DiagnosticCode.PreEventGuardNotAllowed` to `src/Precept/Language/DiagnosticCode.cs`.

Error recovery: inject the guard at the post-event GuardClause slot anyway (parse continues).

**Soup Nazi Test Spec:**

```
FromScoped_PreEventGuard_EmitsDiagnostic()
  — "from Submitted when Active on Approve -> transition Approved"
  → diagnostic PreEventGuardNotAllowed. Construct still parses (error recovery).

FromScoped_PostEventGuard_NoDiagnostic()
  — "from Submitted on Approve when Active -> transition Approved"
  → no diagnostics.

FromScoped_PreEventGuard_RecoveryInjectsGuard()
  — After diagnostic, the TransitionRowNode still has the guard in its GuardClause slot.
```

---

#### Slice 5.3: ~~`write all` LeadingTokenSlot Injection~~ REMOVED

Root-level `write all` has been removed from the language. This slice is removed from the implementation plan. The `LeadingTokenSlot` mechanism in `DisambiguationEntry` remains available for future constructs but has no current consumer.

---

#### Slice 5.4: Error Sync (Layer E)

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `SyncToNextDeclaration()`:

```csharp
private void SyncToNextDeclaration()
{
    while (Current().Kind != TokenKind.EndOfSource)
    {
        if (Constructs.LeadingTokens.Contains(Current().Kind))
            return;
        Advance();
    }
}
```

Uses `Constructs.LeadingTokens` — the `FrozenSet<TokenKind>` derived from catalog metadata in PR 1.

> Note: `modify` and `omit` are not in `Constructs.LeadingTokens` (they are post-anchor disambiguation tokens, not construct-initiating leading tokens). The `SyncToNextDeclaration()` recovery from an in-scoped parse error works at the level of the enclosing `in` token — if parsing fails inside an `in`-scoped construct, the error sync advances until it finds the next top-level leading token (including `in` itself), at which point the outer dispatch loop re-enters disambiguation cleanly. There is no need to add `modify`/`omit` to the sync set directly.

**Soup Nazi Test Spec:**

```
ErrorSync_SkipsGarbageAndResumesAtNextDeclaration()
  — Input: field + garbage + state → 2 declarations, diagnostic for garbage.

ErrorSync_RecoverAfterMissingSlot()
  — "field as string\nstate Draft initial" → 2 declarations, diagnostic for missing field name.

ErrorSync_RecoverAfterBadDisambiguation()
  — "in Draft foobar\nfield name as string" → 1 field declaration, diagnostic for bad in-scoped token.

ErrorSync_EndOfSource_Terminates()
  — "in Draft" with no further input → terminates without infinite loop.

SampleFile_ParsesWithoutErrors(string path)
  — Theory: loan-application.precept, insurance-claim.precept, customer-profile.precept.
  — Full round-trip: Parse(Lex(source)).Diagnostics.Should().BeEmpty().

ErrorSync_InScopedFailure_RecoversByResynchingToNextLeadingToken()
  — Within a malformed in-block, recovery advances past modify/omit (which are NOT in the sync set) and resumes at the next top-level leading token (e.g., `in`, `field`, `state`).
```

**Regression anchors:** All PR 1–4 tests pass. Sample file round-trip tests pass.

---

### Slice Sizing Assessment

| Slice | Est. Lines | Borderline? |
|-------|-----------|-------------|
| 1.1 | ~15 | No |
| 1.2 | ~25 | No |
| 1.3 | ~5 | No |
| 1.4 | ~140 | **Borderline** — large switch rewrite. George should confirm this is completable in one context window. |
| 1.5 | ~20 | No |
| 1.6 | ~10 | No |
| 2.1a | ~80 | No |
| 2.1b | ~120 | No |
| 2.2 | ~30 | No |
| 2.3 | ~50 | No |
| 2.4 | ~60 | No |
| 2.5 | ~15 | No |
| 2.6 | ~40 | No |
| 3.1 | ~150 | **Borderline** — Pratt parser is the largest single method. George should confirm. |
| 3.2 | ~120 | No |
| 3.3 | ~50 | No |
| 4.1 | ~40 | No |
| 4.2 | ~25 | No |
| 4.3 | ~100 | No |
| 4.4 | ~80 | No |
| 4.5 | ~30 | No |
| 4.6 | ~30 | No |
| 5.1 | ~20 | No |
| 5.2 | ~15 | No |
| 5.4 | ~15 | No |

Borderline slices: 1.4, 3.1. George should confirm these are manageable or propose splits.

---

### File Inventory

| File | PRs |
|------|-----|
| `src/Precept/Language/DisambiguationEntry.cs` | PR 1 (create) |
| `src/Precept/Language/Construct.cs` | PR 1 (modify) |
| `src/Precept/Language/ConstructSlot.cs` | PR 1 (modify) |
| `src/Precept/Language/Constructs.cs` | PR 1 (modify) |
| `src/Precept/Language/DiagnosticCode.cs` | PR 4 (modify — add OmitDoesNotSupportGuard), PR 5 (modify — add PreEventGuardNotAllowed) |
| `src/Precept/Pipeline/Parser.cs` | PR 2–5 (modify) |
| `src/Precept/Pipeline/SyntaxTree.cs` | PR 2 (modify) |
| `src/Precept/Pipeline/SyntaxNodes/*.cs` | PR 2 (create ~15 files: 2.1a base types + FieldTargetNode DU, 2.1b declaration nodes including OmitDeclarationNode) |
| `test/Precept.Tests/ConstructsTests.cs` | PR 1 (modify) |
| `test/Precept.Tests/SlotOrderingDriftTests.cs` | PR 2 (create) |
| `test/Precept.Tests/ParserTests.cs` | PR 3–5 (create, then extend) |
| `test/Precept.Tests/DisambiguationTests.cs` | PR 4–5 (create, then extend) |
| `test/Precept.Tests/ErrorRecoveryTests.cs` | PR 5 (create) |

---

## §7 — Tooling/MCP Sync Assessment

| Surface | PR | Impact | Action |
|---------|-----|--------|--------|
| **TextMate grammar** | None | Grammar is generated from `Tokens` catalog, not from `Constructs` catalog or parser. Parser changes do not affect syntax highlighting. | No changes needed. |
| **`precept_language` MCP tool** | PR 1 | `ConstructMeta` shape changes — `LeadingToken` → `Entries`. If `LanguageTool.cs` serializes `LeadingToken`, it will hit the `[Obsolete]` alias. | Check `LanguageTool.cs` for `LeadingToken` references. Update to `PrimaryLeadingToken` or serialize `Entries` array. Currently MCP tools are minimal, so likely no changes needed. **Note:** `precept_language` should eventually expose `OmitDeclaration` in its construct vocabulary — but this is a separate task, not part of the parser PRs. |
| **`precept_compile` MCP tool** | PR 5 | Parser becomes functional. Tool will produce real AST output. | No code changes to MCP — it already wraps `Compiler.Compile()`. |
| **Language server completions** | PR 1 | LS may reference `LeadingToken` for context-aware completions. | Search LS codebase for `LeadingToken` references. Update to `PrimaryLeadingToken` or `Entries` as appropriate. |
| **Language server semantic tokens** | None | Semantic tokens derive from type checker output, not parser construct dispatch. | No changes needed until type checker is updated. |
| **Language server diagnostics** | PR 5 | Parser diagnostics flow through to LS display. | Verify diagnostic format and severity match LS expectations. |

---

## §8 — Proposal C Status

### `when` as `StateAction` Disambiguation Token

**Context:** George analyzed the feasibility of adding `when` to `StateAction`'s `DisambiguationTokens` alongside `Arrow`. This would allow `to State when Guard -> Actions` as a guarded state action.

**George's analysis:** Zero cost. Feasible. The `when` token can be added to StateAction's disambiguation tokens, and a third routing path added in the `To`/`From` disambiguator.

**Shane's decision:** **DEFERRED/OPEN.** Shane has not approved this proposal. It is explicitly NOT incorporated in v8.

**How to add it later (if approved):**
1. Add `TokenKind.When` to StateAction's `DisambiguationTokens` in its `DisambiguationEntry`.
2. Add a third routing path in the `To`-scoped and `From`-scoped disambiguators: after consuming state target, if next token is `When`, check if the token after the guard is `Arrow` → route to StateAction.
3. Update tests: `SharedLeadingTokens_HaveCorrectCandidateCount` does not change (StateAction entry count stays the same — `When` would be added to existing entries' DisambiguationTokens, not as a new entry).

**Does NOT block any v8 implementation work.**

---

*This document supersedes v7 and marks the final design anchor for the catalog-driven parser implementation. The design is complete and ready for implementation.*
