# Comma-Delimited State List Syntax — Spike Investigation

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-05-12
**Branch:** `spike/Precept-V2-Radical`
**Status:** Spike analysis — state-only scope

---

## 1. Current State

### 1.1 Where `any` and `all` appear today

The Precept grammar uses two quantifier keywords — `any` and `all` — as wildcards in state and field target positions. Their roles are distinct:

| Keyword | Token metadata | Target slot | Constructs that accept it |
|---------|---------------|-------------|--------------------------|
| `any` | `IsStateWildcard = true` | `StateTarget` | `from any on Event ...` (transition row), `in any ensure ...` (state ensure), `to any ...` / `from any ...` (state action), `in any modify ...` (access mode), `in any omit ...` (omit declaration) |
| `all` | `IsFieldBroadcast = true` | `FieldTarget` | `in State modify all readonly`, `in State omit all` |

**Key observation:** `any` is a **state wildcard** — it means "every declared state." `all` is a **field broadcast** — it means "every declared field." Neither operates on events. The `on` slot (`EventTarget`) accepts only a single `Identifier` — no wildcard, no list.

The grammar rules from the language spec (§2.3):

```
StateTarget  :=  Identifier | any           (single state name or wildcard)
EventTarget  :=  Identifier                 (single event name, no wildcard)
FieldTarget  :=  Identifier ("," Identifier)* | all    (comma list or broadcast)
```

Note: `FieldTarget` already accepts comma-delimited lists. `StateTarget` and `EventTarget` do not.

### 1.2 Where comma-delimited lists already exist

The grammar already uses comma-delimited lists in several positions:

| Position | Grammar rule | Example |
|----------|-------------|---------|
| **Field declarations** | `field Ident ("," Ident)* as Type ...` | `field A, B as number default 0` |
| **State declarations** | `state Ident ("," Ident)* Modifiers` | `state Draft, Pending initial` (only when all share modifiers) |
| **Field targets** | `Identifier ("," Identifier)* \| all` | `in Draft modify Amount, Rate editable` |
| **Event arguments** | `event Ident(Arg, Arg, ...)` | `event Submit(Name as string, Amount as number)` |
| **Choice values** | `choice of Type(val, val, ...)` | `choice of string("Low","Medium","High")` |
| **List literals** | `[val, val, ...]` | `[1, 2, 3]` |
| **Identifier lists** | `Identifier ("," Identifier)*` | `field A, B, C as number default 0` |

**Critical finding:** The language already has comma-delimited lists in `FieldTarget`. This proposal extends the same pattern to `StateTarget`.

### 1.3 The repetition problem

The current grammar forces authors to repeat identical constructs across multiple states or events when the same logic applies to a subset.

**Multi-state repetition** — identical handling across a subset of states:

```precept
# hiring-pipeline.precept — 3 rows, identical actions and outcomes
from Screening on RejectCandidate
    -> set FinalNote = RejectCandidate.Note
    -> transition Rejected
from InterviewLoop on RejectCandidate
    -> set FinalNote = RejectCandidate.Note
    -> transition Rejected
from Decision on RejectCandidate
    -> set FinalNote = RejectCandidate.Note
    -> transition Rejected
```

Three rows, identical actions and outcomes. `from any` would be incorrect here because it would also match `Draft`, `OfferExtended`, `Hired`, and `Rejected` — states where rejection should be `Undefined`.

**The gap:** When a subset of states (not "any", not one) shares identical handling, the author must repeat the entire construct for each name. The language has no shorthand for "these specific states."

---

## 2. Proposed Syntax

### 2.1 Multi-state examples

**Multi-state `from` with comma list:**

```precept
# Instead of 3 identical rows:
from Screening, InterviewLoop, Decision on RejectCandidate
    -> set FinalNote = RejectCandidate.Note
    -> transition Rejected
```

**Multi-state `in` for ensures:**

```precept
in Screening, InterviewLoop, Decision ensure FinalNote is not set because "Rejection note not yet authored"
```

**Multi-state `in` for access modes:**

```precept
in Active, Suspended modify Balance editable
```

**Multi-state `to`/`from` for state actions:**

```precept
to Dispatched, Restored -> set LastDispatchTime = now()
```

### 2.3 Grammar delta

Only `StateTarget` expands:

```
StateTarget  :=  Identifier ("," Identifier)* | any     (was: Identifier | any)
EventTarget  :=  Identifier                             (unchanged)
```

`StateTarget` mirrors the existing `FieldTarget` production. `EventTarget` remains single-identifier — see §6.0 for why multi-event is deferred.

### 2.4 Interaction with `any` and `all`

- **`any` remains as-is in `StateTarget`.** It is the "all declared states" wildcard. Comma lists are the subset mechanism — they express "these specific states."
- **`all` is unchanged.** It operates on fields, not states or events.
- **Comma list + `any` is invalid in state position.** `from Draft, any on Submit` is nonsensical — `any` already includes `Draft`. The type checker rejects this with a diagnostic.
- **Empty list is invalid.** The grammar requires at least one `Identifier` per the `Identifier ("," Identifier)*` production.
- **Duplicate names in a list.** The type checker warns on `from Draft, Draft on Submit` as redundant entries.

---

## 3. Desugaring Model

### 3.1 One expansion mode: pure copy

Multi-state comma lists desugar to N independent constructs via **pure copy**. There is only one expansion mode — state names carry no arguments, so each desugared row is byte-identical in its guards, actions, and outcomes.

### 3.2 Multi-state expansion (pure copy)

```precept
from A, B, C on Event when Guard -> actions -> outcome
```

desugars to:

```precept
from A on Event when Guard -> actions -> outcome
from B on Event when Guard -> actions -> outcome
from C on Event when Guard -> actions -> outcome
```

Each desugared row is a complete, independent construct with identical guards, actions, and outcomes. No substitution occurs — states have no arguments.

### 3.4 Expansion semantics

**Source-order preservation:** Desugared rows occupy the same position in the row list as the original multi-target row. They are interleaved at the source position, not appended. This preserves first-match evaluation order.

**State reference validation:** Each state name in a state comma list is independently validated against declared states. An undeclared state produces a diagnostic pointing to the specific undeclared name.

### 3.6 No new runtime support needed

The runtime already handles single-state, single-event `TypedTransitionRow` records. Multi-state expansion produces standard `TypedTransitionRow`s. **Zero runtime changes required.**

### 3.7 Existing expansion precedent

The parser already has the `ParseIdentifierList` method (Parser.cs L353–394) that produces `IdentifierListSlot` with `ImmutableArray<string>` names and per-name `SourceSpan`s. This is used for multi-name field and state declarations. The expansion infrastructure exists.

---

## 4. Impact Analysis

### 4.1 Runtime — parser, type checker, evaluator, diagnostics, proofs

| Component | Change needed | Complexity |
|-----------|--------------|------------|
| **Parser** (`ParseStateTarget`) | Replace single-identifier parsing with `Identifier ("," Identifier)* \| any` loop for states. Return list-capable slot type. `EventTarget` unchanged. | Low. ~20 lines. Mirrors `ParseFieldTarget`'s comma loop (L947–960). |
| **Type checker** (`NormalizeTransitionRow`, `NormalizeStateEnsure`, etc.) | Multi-state expansion: if the state target is a list, loop and emit one typed construct per state (pure copy). | Low-medium. ~50 lines across ~5 normalization methods. |
| **Evaluator** | No change. Receives expanded `TypedTransitionRow`s — same shape as today. | None. |
| **Graph analyzer** | No change. Receives expanded rows. | None. |
| **Proof engine** | No change. Per-row proof obligations are unchanged — each desugared row has its own proof context. | None. |
| **Diagnostics** | New diagnostics: `StateListContainsWildcard` (mixing `any` with named states), `DuplicateStateInList` (redundant state entries). Source spans must attribute errors to specific names in the list. | Low. 2 new diagnostic codes. |

### 4.2 Tooling — syntax highlighting, completions, hover, semantic tokens

| Component | Change needed | Complexity |
|-----------|--------------|------------|
| **TextMate grammar** (`tmLanguage.json`) | **No change required — state comma lists are already supported by the existing regex at group 4.** The `fromOnHeader` pattern in `tools/Precept.GrammarGen/Program.cs` (L617) already supports multi-state commas in the `from` capture group (`any\|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*`). The `on` capture group (group 8) remains single-identifier — `EventTarget` is unchanged. | None. |
| **Semantic tokens** | Each state name should receive a `state` semantic token. Driven by `StateReference` entries from the type checker. | Low — dependent on type checker emitting per-name references. |
| **Completions** | After `from ` or after a comma in a state-target list: offer declared state names. | Low. Need to detect "inside a state-target comma list" context. |
| **Hover** | Each state/event name in a list produces hover info. Same as today for single names — driven by reference entries. | None if references are emitted correctly. |
| **Go-to-definition** | Each name in a list is a valid go-to-definition source. Driven by `SymbolReference` entries. | None if references are emitted correctly. |
| **Preview / state diagram** | The transition preview shows the desugared rows — one per (state, event) pair. No new visual concept needed. | None — driven by the expanded `TypedTransitionRow` list. |

### 4.3 MCP — DTOs, vocabulary, compile output

| Component | Change needed | Complexity |
|-----------|--------------|------------|
| **`precept_compile` output** | Compiled definition contains expanded `TypedTransitionRow`s. Multi-state rows desugar before the MCP tool sees them. No DTO changes. | None. |
| **`precept_syntax` vocabulary** | The `Constructs` catalog entry for `TransitionRow` (line 118 of `Constructs.cs`) contains a `UsageExample` string `"from Draft on Submit -> set reviewer = approver -> transition Submitted"`. This remains valid — no update required unless we want to showcase the new feature. The `ConstructSlotKind.StateTarget` comment (L18: `"state name or quantifier (any)"`) should update to mention comma lists. `ConstructSlotKind.EventTarget` (L19) is unchanged. MCP tool `precept_syntax` reads `CatalogFormatters.FormatSyntax()` which renders each construct's slots from catalog metadata — the MCP output updates automatically when catalog entries update. | Low. Comment-level change in `ConstructSlot.cs` for `StateTarget` only. |
| **`precept_syntax` tool** | `CatalogFormatters.FormatSyntax()` in `tools/Precept.Mcp/CatalogFormatters.cs` renders construct metadata. It does **not** hardcode `StateTarget` or `EventTarget` grammar rules — it reads `ConstructSlot` descriptions and `ConstructMeta` examples from the catalog. Once `ConstructSlot.cs` descriptions and `Constructs.cs` examples are updated, MCP output updates automatically. No code change to the MCP tool itself. | None — driven by catalog. |
| **MCP DTOs** | No changes to `PreceptTransitionRow`, `PreceptField`, `PreceptState`, etc. Desugaring happens before DTO projection. | None. |

---

## 5. Philosophy Check

### 5.1 Domain integrity, determinism, inspectability

**Preserved.** Multi-state comma lists are pure syntactic sugar with deterministic expansion. The runtime never sees them — it receives the same `TypedTransitionRow` records. Prevention guarantees, constraint evaluation, and proof obligations are unchanged because each desugared row is independently validated.

### 5.2 Keyword-anchored, flat statements

**Preserved.** The construct shape doesn't change — it's still `from <target> on <event> ...` with a leading keyword. Adding names to the `from` slot follows the same comma-list pattern already established by `FieldTarget`. Each expanded row is still a flat, self-contained statement.

### 5.3 AI legibility

**Improved.** An AI reading `from Screening, InterviewLoop, Decision on RejectCandidate -> ...` immediately sees the rejection policy applies to three states. An AI reading three separate rows must diff them to confirm they are identical.

### 5.4 Compactness vs. routing clarity

**Net positive.** The comma list saves (N-1) rows × (lines per row) of pure repetition in the state dimension. The routing is more explicit, not less — the reader sees exactly which states share the behavior, and the single event in `on` position remains explicit.

### 5.5 Principle-by-principle assessment

| Principle | Verdict |
|-----------|---------|
| 1. Prevention, not detection | **Preserved.** Same validation on expanded rows. |
| 2. No ambient authority | **Preserved.** |
| 3. Deterministic semantics | **Preserved.** Expansion is deterministic. |
| 4. Immutable source of truth | **Preserved.** |
| 5. Flat, keyword-anchored statements | **Preserved.** Same construct shape. |
| 6. First-match routing | **Preserved.** Expansion preserves source order. |
| 7. Self-contained rows | **Minor tension — acceptable.** Reader must know the copy-expansion rule. |
| 8. Sound static analysis | **Preserved.** Each state name independently validated. |
| 9. Tooling drives syntax | **Positive.** Completions, hover, go-to-def work naturally. |
| 10. Consistent prepositions | **Positive.** Extends `FieldTarget` pattern to `StateTarget`. |
| 12. AI is a first-class consumer | **Positive.** |
| 13. Keywords for domain, symbols for math | **Preserved.** |

---

## 6. Alternatives Considered

### 6.0 Multi-event comma lists — deferred

> `from Active on Cancel, Expire, Withdraw -> transition Terminated`

Multi-event comma lists for the `EventTarget` slot were investigated and deferred. The feature introduces an arg-shape compatibility problem: when guards or actions reference event args (e.g., `Cancel.Reason`), the referenced arg may not exist on all events in the list (e.g., `Expire` has no `Reason`). This requires either (a) intersection semantics with `EventArgShapeIncompatible` validation, or (b) restricting multi-event to no-arg-reference rows only.

**Why deferred:** The intersection semantics approach is sound but adds ~90 lines of type-checker logic (arg-shape validation + substitution-based expansion) and 3 additional diagnostic codes for a feature that has zero consolidation candidates in the current 20-sample corpus. Multi-event consolidation opportunities are rare because events tend to carry different argument shapes — their handling naturally diverges. The implementation cost is disproportionate to the near-term benefit.

**Path to reconsideration:** If future sample corpora reveal meaningful multi-event consolidation opportunities (e.g., terminal-event families in subscription or contract lifecycle domains), the arg-shape intersection semantics design is documented in this spike and can be re-evaluated. The grammar production rule change (`EventTarget := Identifier ("," Identifier)*`) is one line; the complexity is entirely in the type-checker validation.

**Rationale for rejecting "ship events now":** Shipping a feature for consistency (the grammar symmetry argument) without real-world demand is premature generalization. The state-only feature already eliminates ~7.7% of rows in the current corpus. Events can wait for demonstrated need.

### 6.1 Named state/event groups

```precept
stategroup PreDecision = Screening, InterviewLoop, Decision
from PreDecision on RejectCandidate -> ...
```

Introduces a new namespace, a new declaration kind, a new resolution phase, and membership-tracking complexity. The `transition-shorthand.md` research rejects this: "The cost is disproportionate to the benefit." Inline comma lists achieve the same row reduction without a new namespace.

**Rejected — namespace complexity without proportional value.**

### 6.2 Repeating rules (status quo)

Authors write N separate rows. Verbose but explicit, independently readable, and requires no parser changes. The verbosity is real — the IT helpdesk sample has 4 identical rows (12 lines) for `RegisterAgent`, the hiring pipeline has 3 identical rows for `RejectCandidate`. The repetition obscures intent and introduces copy-paste drift risk.

**Acceptable baseline but suboptimal.**

### 6.3 Extending `any` with exclusion (`from any except Closed on ...`)

Inverts the model — name the states you don't want instead of the ones you do. Introduces an `except` keyword and a negation model. If the author adds a new state, the `except` clause silently includes it.

**Rejected — negation model is less explicit and introduces silent scope changes on state addition.**

### 6.4 Event-gated guard syntax (per-event guard clauses)

*This alternative is relevant only if multi-event comma lists ship (see §6.0 for deferral rationale).*

```precept
from Active on Cancel when Cancel.Reason == "fraud", Expire -> transition Terminated
```

This alternative allows different guard expressions per event within a single row. More expressive than intersection semantics — each event can have its own guard or no guard. But the grammar complexity is significantly higher: the parser must handle `EventName [when GuardExpr]? ("," EventName [when GuardExpr]?)*`, which nests guard expressions inside a comma list. This breaks the flat left-to-right parsing model. The construct is no longer a simple comma list — it's a list of guarded-event pairs.

The readability cost is high. `from Active on Cancel when Cancel.Reason == "fraud", Expire, Withdraw when Withdraw.Note is set -> transition Terminated` buries three different guard policies in one line with no visual structure.

**Rejected — grammar complexity and readability cost outweigh expressiveness. Authors who need per-event guards should write separate rows.**

### 6.5 No-guard restriction for multi-event rows

*This alternative is relevant only if multi-event comma lists ship (see §6.0 for deferral rationale).*

Multi-event rowswith any guard or event-arg reference in actions are prohibited; guards require single-event rows. Simple to implement and trivially safe, but eliminates the shared-arg case entirely. The intersection semantics approach handles the shared-arg case correctly at compile time with no runtime cost. Banning guards is an artificial restriction when the type system can enforce the real constraint.

**Rejected — unnecessarily restrictive when intersection semantics are sound.**

### 6.6 Event wildcards in guards

```precept
from Open on * when *.Amount > 0 -> transition Processing
```

A step toward pattern matching — fundamentally different language model. Requires runtime dispatch on event shape rather than declared name, breaking the static `(state, event)` lookup model.

**Rejected — category shift from declared routing to pattern matching.**

---

## 7. Exhaustive Documentation Coverage Audit

Every file that could plausibly need an update is listed below with a definitive verdict and evidence.

### 7.1 Language surface docs

| File | Change required | Rationale |
|------|-----------------|-----------|
| `docs/language/precept-language-spec.md` §2.3 (L826) | **Yes** — add formal `StateTarget` grammar rule: `StateTarget := Identifier ("," Identifier)* \| any`. Also update the state ensure (L855), state action (L873), and access mode/omit grammar (L896–909) sections to use the new `StateTarget` production consistently. `EventTarget` grammar rule is unchanged. | The spec is the language's source of truth for `StateTarget`. |
| `docs/language/precept-grammar.md` (L212, L263, L275, L287–288, L319, L331, L342, L484, L505–507, L829–833) | **Yes** — references to `StateTarget` describe it as a single-name slot. The slot table (L484) says `StateTarget = "A state name reference"` — update to "state name(s) — comma-delimited list or wildcard." The TransitionRow slot decomposition (L505–507) must mention that `StateTarget` can contain a list, breaking the offset-2 disambiguation invariant. `EventTarget` slot description (L485) stays "An event name reference" — no change. | Grammar design reference; stale `StateTarget` descriptions mislead implementers. |
| `docs/language/catalog-system.md` (L547, L1643–1644, L1974) | **No change required.** The catalog-system doc describes `ConstructSlotKind` as a helper enum (L547) and lists slot kinds (L1643–1644, L1974) but does not define the grammar of `StateTarget`/`EventTarget` slots — it defers to the grammar docs. The descriptions are structural ("state name", "event name") and don't encode single-vs-list semantics. | The catalog-system doc describes catalog architecture, not grammar rules. |
| `docs/language/PreceptLanguageDesign.md` | **Does not exist.** No file at this path. | N/A. |
| Other `docs/language/*.md` | **No change required.** No other language docs describe transition construct syntax at the grammar-rule level. | N/A. |

### 7.2 Compiler docs

| File | Change required | Rationale |
|------|-----------------|-----------|
| `docs/compiler/parser.md` (L51–52, L193–200, L229–230) | **Yes — critical.** Two updates: (1) Slot type table (L51–52): `StateTargetSlot` is `string?` — must become list-capable (e.g., `ImmutableArray<string>`). `EventTargetSlot` stays `string?`. (2) **Disambiguation invariant (L193–200) must be revised.** The decision block states the disambiguation token is structurally invariant at peek(2). With multi-state comma lists, `from Draft, Pending on Submit` has the disambiguation token (`on`) at a variable offset. The parser must scan past the comma-delimited list to find the disambiguation token. The invariant must be restated. | The parser doc is the implementation specification. The peek-at-2 invariant is the most critical doc update — an implementer relying on it would produce incorrect disambiguation logic. |
| `docs/compiler/type-checker.md` (L59–60) | **Yes.** Slot type table: `StateTargetSlot` is `string? StateName` — must reflect the list-capable form. `EventTargetSlot` stays `string? EventName`. | Matches parser slot type change. |
| `docs/compiler/name-binder.md` (L181–182, L195–196, L201, L222–223, L280, L357) | **Yes.** Multiple references describe `StateTarget` as a single-name resolution target. L195–196 explicitly states "Every StateScoped construct's first slot is `StateTarget` (a single identifier)" — this restates the peek-at-2 invariant and must be updated. `SymbolReference` descriptions must account for per-name references from a state list. `EventTarget` single-identifier references remain accurate. | The name binder resolves each state name in a comma list independently. |
| `docs/compiler/grammar-generator.md` (L129) | **No change required.** The `fromOnHeader` description already shows `from State[, ...] on Event` — multi-state is already reflected. `EventTarget` stays single identifier; no update needed. | Description is accurate for state-only scope. |
| `docs/compiler/tooling-surface.md` (L502–503, L526–528, L546–547, L560–561) | **No change required.** The completions infrastructure describes `InStateTarget` and `InEventTarget` as slot contexts that offer declared state/event names. Adding comma lists doesn't change what completions are offered — it changes *when* the context triggers (also after commas). The completions provider code must handle this, but the doc's description of the completion mapping is already abstract enough to cover it. | The doc describes the mapping table, not the trigger logic. |
| `docs/tooling/language-server.md` (L326–327, L353–354, L374–375) | **No change required.** Same reasoning as `tooling-surface.md` — describes the slot-context-to-completion mapping, which is unchanged. | The mapping is abstract; trigger detection is implementation detail. |

### 7.3 Grammar generator and TextMate grammar

| File | Change required | Rationale |
|------|-----------------|-----------|
| `tools/Precept.GrammarGen/Program.cs` (L609–635) | **No change required — state comma lists already supported.** The `fromOnHeader` structural pattern (L617) regex capture group 4 (state position) already supports comma lists: `any\|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)`. Capture group 8 (event position) stays single-identifier — `EventTarget` is unchanged. | No generator change needed for state-only scope. |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | **No change required.** The event capture group (group 8) remains single-identifier. The state capture group (group 4) already supports comma lists. **Do not hand-edit this file.** | Build output; no regeneration needed. |

### 7.4 MCP tools and catalog entries

| File | Change required | Rationale |
|------|-----------------|-----------|
| `src/Precept/Language/ConstructSlot.cs` (L18) | **Yes.** Comment update: L18 `StateTarget = 10, // state name or quantifier (any)` → `// state name(s) — comma-delimited list or quantifier (any)`. L19 `EventTarget` comment is unchanged — it remains `"event name"`. | Code comments that are actively wrong mislead all consumers. |
| `src/Precept/Language/Constructs.cs` (L114–122) | **Optional.** The `TransitionRow` `UsageExample` (L118) is `"from Draft on Submit -> set reviewer = approver -> transition Submitted"` — remains valid. The `SlotStateTarget` slot `Description` string should update to mention comma lists; MCP consumers see this description via `precept_syntax`. `SlotEventTarget` `Description` is unchanged. | The slot `Description` field drives MCP output. |
| `tools/Precept.Mcp/CatalogFormatters.cs` | **No change required.** `FormatSyntax()` (L43+) reads construct metadata generically — it iterates `Constructs.All` and renders slots, examples, and disambiguation from catalog records. It has no hardcoded `StateTarget` or `EventTarget` references. When catalog entries update, MCP output updates automatically. | Thin wrapper — no domain logic. |
| `tools/Precept.Mcp/Tools/SyntaxTool.cs` | **No change required.** Delegates to `CatalogFormatters.FormatSyntax()`. No hardcoded grammar references. | Thin wrapper. |
| Other `tools/Precept.Mcp/Tools/*.cs` | **No change required.** `CompileTool.cs` projects desugared `TypedTransitionRow`s — comma lists are invisible after expansion. No MCP DTO changes needed. | Desugaring happens upstream. |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | **Already removed — no action required.** `precept_language` was deregistered as a discoverable MCP tool (`[McpServerTool]` removed from `Language()`). Grep confirms: no internal callers exist in `tools/Precept.Mcp/` and `LanguageTool.cs` itself does not exist — the implementation was fully deleted, not merely deregistered. No `StateTarget` description update or deletion task remains for this spike. | None — cleanup already complete. |

### 7.5 Samples

The spike commits to selective sample updates. Based on the corpus analysis in the research (`transition-shorthand.md`), these specific samples have repeated transition rows that could be consolidated:

| Sample file | Consolidation opportunity | Update required |
|-------------|--------------------------|-----------------|
| `samples/hiring-pipeline.precept` | 3 identical `from Screening/InterviewLoop/Decision on RejectCandidate` rows → 1 multi-state row | **Yes** — primary showcase. |
| `samples/it-helpdesk-ticket.precept` | Multiple repeated state subsets sharing identical reject/transition logic | **Yes** — demonstrates multi-state value. |
| `samples/utility-outage-report.precept` | Repeated rows across state subsets | **Yes** — demonstrates multi-state value. |
| All other samples | No clean consolidation candidates or repetition is semantically distinct | **No** — leave as-is. |

### 7.6 Top-level docs

| File | Change required | Rationale |
|------|-----------------|-----------|
| `README.md` | **No change required.** Lines 40–46 show single-state, single-event transition examples (`from Trial on Activate...`, `from Active on Cancel...`). These remain valid syntax. The README describes Precept at a product level — it is not a grammar reference. The comma-list feature is an enhancement, not a change to existing syntax. | Valid syntax; product narrative, not grammar spec. |
| `CONTRIBUTING.md` | **No change required.** Describes the development workflow — not the DSL grammar. | Process doc, not language doc. |
| `docs/philosophy.md` | **No change required.** The comma-list feature is syntactic sugar within existing semantics. It does not change the category of entities governed, the core guarantee, the positioning, or the constraint model. | Philosophy §5.1–5.5 analysis confirms no philosophical tension. |

### 7.7 Research and working docs

| File | Change required | Rationale |
|------|-----------------|-----------|
| `research/language/expressiveness/transition-shorthand.md` | **No change required.** This is research input — it describes the problem space that motivated this spike. It does not need updating when the solution ships; it is archival evidence. | Research is immutable evidence. |
| `docs/working/comma-list-syntax-spike.md` (this file) | **This file is the spike.** It becomes archival once the implementation PR ships. | Working doc — not a maintained specification. |

---

## 8. Implementation File Inventory

Complete list of every file touched in the implementation PR, organized by category.

### 8.1 Runtime changes (required)

| File | Change | Notes |
|------|--------|-------|
| `src/Precept/Pipeline/Parser.cs` | `ParseStateTarget` → comma-list loop. `ParseEventTarget` unchanged. Return list-capable slot type for state only. | ~20 lines. Model: `ParseFieldTarget` (L947–960). |
| `src/Precept/Pipeline/Parser.cs` | Disambiguation logic update — scanner must skip comma-delimited state list to find the disambiguation token (`on`), replacing the peek-at-2 assumption. | Critical parser infrastructure change. |
| `src/Precept/Pipeline/TypeChecker.cs` | `NormalizeTransitionRow`, `NormalizeStateEnsure`, `NormalizeAccessMode`, `NormalizeOmitDeclaration`, `NormalizeStateAction` — multi-state expansion (pure copy). | ~50 lines total. |
| `src/Precept/Language/Diagnostics.cs` | 2 new diagnostic codes: `StateListContainsWildcard`, `DuplicateStateInList`. | Catalog entries with messages, severity, hints. |
| `src/Precept/Language/ConstructSlot.cs` | Update `StateTarget` comment and `Description` field. `EventTarget` unchanged. | 1 line. |
| `src/Precept/Language/Constructs.cs` | Update `SlotStateTarget` slot `Description` string. `SlotEventTarget` unchanged. Optionally update `TransitionRow` `UsageExample`. | 1–2 lines. |

### 8.2 Documentation changes (required, same PR)

| File | Change | Notes |
|------|--------|-------|
| `docs/language/precept-language-spec.md` | Add formal `StateTarget` grammar rule. Update transition row (L826), state ensure (L855), state action (L873), access mode (L896–909) grammar blocks. `EventTarget` grammar rule unchanged. | Language spec is the source of truth. |
| `docs/language/precept-grammar.md` | Update `StateTarget` slot descriptions (L484, L505–507, L829–833) and TransitionRow decomposition. Revise or caveat the offset-2 disambiguation invariant. `EventTarget` slot description (L485) unchanged. | Grammar design reference. |
| `docs/compiler/parser.md` | (1) Update `StateTargetSlot` type in slot type table (L51). (2) **Revise disambiguation invariant decision block** (L193–200). `EventTargetSlot` stays `string?`. | Critical — implementers rely on this. |
| `docs/compiler/type-checker.md` | Update `StateTargetSlot` type in slot type table (L59). `EventTargetSlot` unchanged. | Matches parser slot type change. |
| `docs/compiler/name-binder.md` | Update single-identifier `StateTarget` assumptions at L181–182, L195–196, L201, L222–223, L280, L357. `EventTarget` single-identifier references remain accurate. | Name binder resolves per-name from state list. |
| `docs/compiler/grammar-generator.md` | **No change required.** Description `from State[, ...] on Event` already accurately reflects state-only multi-name support. | Accurate as-is. |

### 8.3 Tooling changes

| File | Change | Notes |
|------|--------|-------|
| `tools/Precept.GrammarGen/Program.cs` | **No change required.** State capture group (group 4) in `fromOnHeader` already supports comma lists. Event capture group (group 8) stays single-identifier. | State-only scope requires no generator changes. |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | **No change required.** No generator change means no regeneration needed. | Build output; accurate as-is. |

### 8.4 MCP changes

| File | Change | Notes |
|------|--------|-------|
| `tools/Precept.Mcp/CatalogFormatters.cs` | **No change.** Reads catalog metadata generically. | Updates propagate from catalog. |
| `tools/Precept.Mcp/Tools/*.cs` | **No change.** All thin wrappers. Desugaring is upstream. | N/A. |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | **No action required — already removed.** `precept_language` was deregistered (`[McpServerTool]` stripped from `Language()`). File does not exist; no internal callers confirmed by grep. Dead code was fully deleted. | None. |

### 8.5 Samples (selective)

| File | Change | Notes |
|------|--------|-------|
| `samples/hiring-pipeline.precept` | Consolidate 3 RejectCandidate rows into 1 multi-state row. | Primary showcase. |
| `samples/it-helpdesk-ticket.precept` | Consolidate repeated state-subset rows. | Demonstrates value. |
| `samples/utility-outage-report.precept` | Consolidate repeated state-subset rows. | Demonstrates value. |

### 8.6 Tests

| Area | Expected tests |
|------|---------------|
| Parser | Multi-state comma lists in `from`, `in`, `to` positions. Edge cases: trailing comma, single-item list, `any` in list (error). `EventTarget` single-identifier — no change needed. |
| Type checker | Multi-state expansion (verify N rows emitted). Both new diagnostics: `StateListContainsWildcard`, `DuplicateStateInList`. |
| Samples | All updated samples compile clean with 0 diagnostics. |

**Test count record:**

| Checkpoint | `Precept.Tests` count | Notes |
|------------|----------------------|-------|
| Pre-spike baseline | 4938 | Verified by Soup Nazi against `dotnet test` on `spike/Precept-V2-Radical` before S1/S2 landed |
| Post-spike (commit `a63d88b4`) | 4962 | Verified by Frank during spike review; Soup Nazi also confirmed this count (`dotnet test` output) |
| After Soup Nazi blocker fixes (B1–B4) | 4969 | 7 new tests added: 5 parser comma-list AST shape tests, guard-cloning test, multi-unknown-state fan-out test |

---

## 9. Design Decisions (Locked)

### D1. Scope: states only

**Decision:** Comma-delimited lists apply to `StateTarget` only. `EventTarget` remains single-identifier. Multi-event lists are deferred — see §6.0.

**Rationale:** Multi-event lists introduce an arg-shape compatibility problem: guards and actions may reference event args (e.g., `Cancel.Reason`) that don't exist on all events in the list. Resolving this requires intersection semantics with `EventArgShapeIncompatible` validation and substitution-based expansion — approximately 90 additional lines of type-checker logic and 3 additional diagnostic codes. Zero multi-event consolidation candidates exist in the current 20-sample corpus. The implementation cost is disproportionate to demonstrated need. State-only delivers ~7.7% row reduction immediately; events can be reconsidered when corpus evidence justifies the complexity.

**Alternative rejected:** Including events now for grammar symmetry. Rejected — symmetry is not a sufficient reason to ship premature complexity. Grammar symmetry is a weaker argument than demonstrated demand.

**Tradeoff accepted:** The `on` slot remains single-identifier while `from`, `in`, and `to` gain comma lists. This is a transient inconsistency that resolves when (if) multi-event ships.

### D2. Expansion model: type-checker expansion (Path 2) — locked

**Decision:** The parser emits one `ParsedConstruct` with list-capable target slots. The type checker expands lists into N typed constructs during normalization.

**Rationale:** The parser should faithfully represent source structure. Expansion is a semantic operation — the type checker already handles `from any` by setting `FromState = null`. Expanding comma lists is the same kind of semantic operation. Path 2 keeps the parser's output 1:1 with source lines, which matters for diagnostic attribution and source-span accuracy.

**Alternative rejected:** Path 1 (parser expansion) would work but means the parser emits multiple constructs from one source line, complicating span tracking and making the parser responsible for semantic expansion — not its job.

**Tradeoff accepted:** The type checker's normalization methods grow in complexity. This is acceptable because the expansion logic is localized (one loop per normalization method) and the pattern already exists in `from any` handling.

### D3. All state-preposition constructs at once — locked

**Decision:** All constructs that use `StateTarget` get comma-list support in one pass: transition rows (`from`), state ensures (`in`), access modes (`in ... modify`), omit declarations (`in ... omit`), and state actions (`to`/`from`).

**Rationale:** The grammar change is in `ParseStateTarget`, which is shared by all constructs. Implementing comma lists for transition rows but not ensures or access modes would require artificial restrictions in the parser — checking which construct is being parsed to decide whether to accept a comma. That is complexity in the wrong direction.

### D4. Sample corpus update — selective

**Decision:** Update the most verbose samples (`it-helpdesk-ticket`, `utility-outage-report`, `hiring-pipeline`) to use comma-list syntax where it eliminates pure repetition. Keep some samples in expanded form for pedagogical value.

**Rationale:** The sample corpus is the canonical usage reference. It should demonstrate available features. But not every sample needs conversion — keeping some in expanded form shows both styles and helps authors who are learning the language.

---

## 10. Recommendation

### Architectural verdict: Proceed to proposal issue — state-only scope.

The comma-delimited list syntax for `StateTarget` is:

1. **Grammatically consistent** — mirrors the existing `FieldTarget` comma-list pattern (`Identifier ("," Identifier)*`). Extending `StateTarget` to match `FieldTarget` eliminates an internal inconsistency in the grammar.
2. **Semantically clean** — pure copy expansion. Deterministic, zero runtime changes, trivially safe.
3. **Philosophically aligned** — preserves flat statements, keyword anchoring, first-match routing, and AI legibility. Improves compactness without obscuring routing.
4. **Already researched** — `research/language/expressiveness/transition-shorthand.md` provides the precedent survey, philosophy fit analysis, and semantic contracts.

### Implementation cost estimate

| Component | Implementation estimate |
|-----------|------------------------|
| Parser | ~20 lines |
| Type checker (expansion) | ~50 lines |
| Diagnostics | 2 new codes |
| Runtime / evaluator / graph / proof | 0 |
| Tooling (completions, semantic tokens) | Low |
| **Total** | **~70 lines + 2 diagnostics** |

### Track recommendation

**Track A** (implementation PR, no new design doc). The feature is syntactic sugar with deterministic expansion. The grammar change is a one-line production rule extension for `StateTarget`. The language spec's `StateTarget` grammar rule gets a one-line update.

### Risk

**Low.** Multi-state expansion is trivially safe — pure copy, zero runtime risk. The comma after an identifier in state-target position is unambiguous at LL(1) because no construct currently places a comma after a state slot. The disambiguation invariant update (peek-past-comma-list) is the only structural parser change and is well-bounded.

---

## Appendix A: Research References

- `research/language/expressiveness/transition-shorthand.md` — Comprehensive transition compactness research including precedent survey, philosophy fit, dead ends, arg-shape compatibility contracts, and row-count impact quantification.
- `research/language/expressiveness/internal-verbosity-analysis.md` — Statement count distribution across all 21 samples, verbosity pattern identification.
- `docs/language/precept-language-spec.md` §2.3 — Grammar rules for `StateTarget`, `EventTarget`, `FieldTarget`, transition rows, state ensures, access modes.
- `src/Precept/Pipeline/Parser.cs` L876–894 — Current `ParseStateTarget` implementation (single identifier or `IsStateWildcard`).
- `src/Precept/Pipeline/Parser.cs` L932–960 — Current `ParseFieldTarget` implementation (comma-delimited list or `IsFieldBroadcast`) — the model to follow.
- `src/Precept/Pipeline/TypeChecker.cs` L1083–1126 — Current `NormalizeTransitionRow` state/event resolution — the expansion point.
- `src/Precept/Language/Constructs.cs` L114–122 — `TransitionRow` construct definition with `SlotStateTarget` and `SlotEventTarget`.
- `src/Precept/Language/Tokens.cs` L199–201 — `any` token metadata with `IsStateWildcard = true`.

## Appendix B: Corpus Impact

### B.1 Multi-state consolidation candidates

If multi-state comma lists were available, the following sample corpus rows could be consolidated:

| Sample | Event | States | Rows saved | New syntax |
|--------|-------|--------|------------|------------|
| `it-helpdesk-ticket` | `RegisterAgent` | New, Assigned, WaitingOnCustomer, Resolved | 3 | `from New, Assigned, WaitingOnCustomer, Resolved on RegisterAgent -> ...` |
| `utility-outage-report` | `RegisterCrew` | Reported, VerifiedState, Dispatched, Restored | 3 | `from Reported, VerifiedState, Dispatched, Restored on RegisterCrew -> ...` |
| `hiring-pipeline` | `RejectCandidate` | Screening, InterviewLoop, Decision | 2 | `from Screening, InterviewLoop, Decision on RejectCandidate -> ...` |
| `library-book-checkout` | `ReturnBook` | CheckedOut, Overdue | 1 | `from CheckedOut, Overdue on ReturnBook -> ...` |
| `library-book-checkout` | `ReportLost` | CheckedOut, Overdue | 1 | `from CheckedOut, Overdue on ReportLost -> ...` |
| `maintenance-work-order` | `Cancel` | Open, Scheduled | 1 | `from Open, Scheduled on Cancel -> ...` |
| `event-registration` | `Cancel` | Draft, PendingPayment | 1 | `from Draft, PendingPayment on Cancel -> ...` |
| `inventory-item` | `UpdateListPrice` | Listed, LowStock | 1 | `from Listed, LowStock on UpdateListPrice -> ...` |
| `inventory-item` | `Delist` | Listed, LowStock | 1 | `from Listed, LowStock on Delist -> ...` |
| `insurance-claim` | `RequestDocument` | Submitted, UnderReview | 1 | `from Submitted, UnderReview on RequestDocument -> ...` |

**Multi-state total: ~15 rows saved out of 196 (~7.7%).** Concentrated in the highest-verbosity samples.

### B.2 Why multi-event is deferred

The current sample corpus has **zero** multi-event consolidation candidates. The closest candidate is `refund-request.precept` where `Decline(Note as string notempty)` and `Cancel(Reason as string optional)` both transition `from Submitted -> transition Declined` with a `set DecisionNote` action — but the arg names differ (`Note` vs `Reason`) and the optionality differs (`notempty` vs `optional`), so these fail the arg-shape intersection check.

Events tend to carry different argument shapes, so their handling naturally diverges. The existing research predicted this: "genuine multi-event `on` candidates are rare" in the current corpus.

**Where multi-event consolidation has future value:** Enterprise domains with terminal-event families — multiple no-arg events that all route to the same terminal state from the same source state. Examples: `Cancel`, `Expire`, `Withdraw`, `Abandon` all leading to `Terminated`. These patterns appear in subscription management, contract lifecycle, and case management domains that the current sample corpus doesn't fully represent.

This corpus evidence is the primary rationale for deferring `EventTarget` comma lists. See §6.0 for the full deferral rationale and path to reconsideration.

### B.3 Corpus impact (state-only)

Multi-state comma lists save ~15 rows (~7.7%) across the current corpus. Concentrated in the highest-verbosity samples.
