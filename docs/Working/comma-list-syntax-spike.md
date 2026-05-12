# Comma-Delimited State and Event List Syntax — Spike Investigation

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-05-12
**Branch:** `spike/Precept-V2-Radical`
**Status:** Spike analysis — full scope (states + events)

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

**Critical finding:** The language already has comma-delimited lists in `FieldTarget`. This proposal extends the same pattern to `StateTarget` and `EventTarget`.

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

**Multi-event repetition** — identical handling for different events from the same state:

```precept
# Hypothetical (common in enterprise domains with terminal-event families):
from Active on Cancel -> transition Terminated
from Active on Expire -> transition Terminated
from Active on Withdraw -> transition Terminated
```

Three rows, no guards, no arg references, identical outcomes. The policy is "any of these events terminates the entity from Active" — but the author must express it as three independent rows.

**The gap:** When a subset of states (not "any", not one) shares identical handling, or when multiple events from the same state share identical handling, the author must repeat the entire construct for each name. The language has no shorthand for "these specific states" or "these specific events."

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

### 2.2 Multi-event examples

**Multi-event `on` — no-arg-reference case (actions reference only fields):**

```precept
# Three terminal events, no guards, no event-arg references:
from Active on Cancel, Expire, Withdraw -> transition Terminated
```

**Multi-event `on` — with shared field-only actions:**

```precept
# All events trigger the same field mutation, no event args referenced:
from Open on Approve, FastTrack -> set Approved = true -> transition Active
```

**Multi-event `on` — with guards on shared args (intersection semantics):**

```precept
# Cancel(Reason as string) and Withdraw(Reason as string) both have Reason.
# Guard references Cancel.Reason — the first event in the list is the "template."
# Each desugared row substitutes the event name: Cancel.Reason → Withdraw.Reason.
from Active on Cancel, Withdraw when Cancel.Reason == "fraud"
    -> set FraudFlag = true
    -> transition Terminated
```

**Multi-event `on` — the arg-shape incompatibility error:**

```precept
# Cancel(Reason as string) has Reason. Expire() has no args.
# Guard references Cancel.Reason — Expire has no Reason arg.
# ❌ TYPE ERROR: EventArgShapeIncompatible — Expire has no arg 'Reason'
from Active on Cancel, Expire when Cancel.Reason == "fraud"
    -> transition Terminated
```

### 2.3 Grammar delta

Both `StateTarget` and `EventTarget` expand:

```
StateTarget  :=  Identifier ("," Identifier)* | any     (was: Identifier | any)
EventTarget  :=  Identifier ("," Identifier)*           (was: Identifier only)
```

`StateTarget` mirrors the existing `FieldTarget` production. `EventTarget` gains comma lists but no wildcard — event wildcards are rejected (see §6.4 in the original research: "category shift from declared routing to pattern matching").

### 2.4 Interaction with `any` and `all`

- **`any` remains as-is in `StateTarget`.** It is the "all declared states" wildcard. Comma lists are the subset mechanism — they express "these specific states."
- **No wildcard for `EventTarget`.** There is no `any` equivalent for events. Event wildcards would break the static `(state, event)` routing lookup model.
- **`all` is unchanged.** It operates on fields, not states or events.
- **Comma list + `any` is invalid in state position.** `from Draft, any on Submit` is nonsensical — `any` already includes `Draft`. The type checker rejects this with a diagnostic.
- **Empty list is invalid.** The grammar requires at least one `Identifier` per the `Identifier ("," Identifier)*` production.
- **Duplicate names in a list.** The type checker warns on `from Draft, Draft on Submit` or `from Active on Cancel, Cancel` as redundant entries.

---

## 3. Desugaring Model

### 3.1 Two expansion modes

Multi-state and multi-event comma lists both desugar to N independent constructs, but with different expansion semantics:

| Dimension | Multi-state (`from A, B, C on Event`) | Multi-event (`from State on E1, E2, E3`) |
|-----------|---------------------------------------|------------------------------------------|
| **Expansion kind** | Pure copy | Substitution-based |
| **Why** | State names have no arguments — each desugared row is byte-identical | Event names have typed arguments — guards and actions that reference `E1.ArgName` must be rewritten to `E2.ArgName`, `E3.ArgName` in their respective rows |
| **Guard handling** | Guard copied verbatim to all rows | Guard expression undergoes event-arg-reference substitution per event |
| **Action handling** | Actions copied verbatim to all rows | Action expressions undergo event-arg-reference substitution per event |
| **Validation** | Each state name validated against declared states | Each event name validated against declared events; arg-shape compatibility checked across all listed events for any referenced args |

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

### 3.3 Multi-event expansion (substitution-based)

```precept
from State on E1, E2 when E1.Reason == "fraud" -> set Note = E1.Reason -> transition Flagged
```

desugars to:

```precept
from State on E1 when E1.Reason == "fraud" -> set Note = E1.Reason -> transition Flagged
from State on E2 when E2.Reason == "fraud" -> set Note = E2.Reason -> transition Flagged
```

The first event in the list is the **template event**. All event-arg references in guards and actions use the template event's name (e.g., `E1.Reason`). During desugaring, each subsequent row substitutes the template event name with the row's specific event name in all arg-reference positions.

**What gets substituted:** Only `EventName.ArgName` references in guard expressions and action RHS expressions. State names, field names, literal values, and operators are unchanged.

**What does NOT get substituted:** The event name in `on` position is replaced wholesale (each desugared row gets its own event). The transition target, `no transition`, `reject`, and other outcome forms are copied verbatim.

**No-arg-reference rows are trivial:** When no guard or action references any event arg, multi-event expansion degenerates to pure copy — identical to multi-state expansion. This is the common case in the corpus (terminal-event families with no guards).

### 3.4 Expansion semantics (shared)

**Source-order preservation:** Desugared rows occupy the same position in the row list as the original multi-target row. They are interleaved at the source position, not appended. This preserves first-match evaluation order.

**State reference validation (multi-state):** Each state name in a state comma list is independently validated against declared states. An undeclared state produces a diagnostic pointing to the specific undeclared name.

**Event reference validation (multi-event):** Each event name in an event comma list is independently validated against declared events. An undeclared event produces a diagnostic pointing to the specific undeclared name.

### 3.5 Combined multi-state + multi-event

A single row may have both multi-state and multi-event lists:

```precept
from Active, Suspended on Cancel, Withdraw -> transition Terminated
```

This desugars to the Cartesian product: 2 states × 2 events = 4 rows:

```precept
from Active on Cancel -> transition Terminated
from Active on Withdraw -> transition Terminated
from Suspended on Cancel -> transition Terminated
from Suspended on Withdraw -> transition Terminated
```

The ordering is state-major: all events for the first state, then all events for the second state, preserving source position for first-match evaluation.

### 3.6 No new runtime support needed

The runtime already handles single-state, single-event `TypedTransitionRow` records. Both expansion modes produce standard `TypedTransitionRow`s. **Zero runtime changes required.**

### 3.7 Existing expansion precedent

The parser already has the `ParseIdentifierList` method (Parser.cs L353–394) that produces `IdentifierListSlot` with `ImmutableArray<string>` names and per-name `SourceSpan`s. This is used for multi-name field and state declarations. The expansion infrastructure exists.

---

## 4. Impact Analysis

### 4.1 Runtime — parser, type checker, evaluator, diagnostics, proofs

| Component | Change needed | Complexity |
|-----------|--------------|------------|
| **Parser** (`ParseStateTarget`, `ParseEventTarget`) | Replace single-identifier parsing with `Identifier ("," Identifier)* \| any` loop for states. Add `Identifier ("," Identifier)*` loop for events. Return list-capable slot types. | Low. ~30 lines total. Mirrors `ParseFieldTarget`'s comma loop (L947–960). |
| **Type checker** (`NormalizeTransitionRow`, `NormalizeStateEnsure`, etc.) | Multi-state expansion: if the state target is a list, loop and emit one typed construct per state (pure copy). Multi-event expansion: if the event target is a list, loop and emit one typed construct per event with arg-reference substitution. Enforce arg-shape compatibility on multi-event rows with guards/actions that reference event args. | Medium. Multi-state is ~50 lines across ~5 normalization methods. Multi-event adds arg-shape validation + substitution logic, ~80 lines in `NormalizeTransitionRow`. |
| **Evaluator** | No change. Receives expanded `TypedTransitionRow`s — same shape as today. | None. |
| **Graph analyzer** | No change. Receives expanded rows. | None. |
| **Proof engine** | No change. Per-row proof obligations are unchanged — each desugared row has its own proof context. | None. |
| **Diagnostics** | New diagnostics: `StateListContainsWildcard` (mixing `any` with named states), `DuplicateStateInList` (redundant state entries), `DuplicateEventInList` (redundant event entries), `EventArgShapeIncompatible` (multi-event row references an arg that doesn't exist on all listed events), `EventArgTypeMismatch` (multi-event row references an arg that exists on all events but with different types). Source spans must attribute errors to specific names in the list. | Low-medium. 5 new diagnostic codes. |

### 4.2 Tooling — syntax highlighting, completions, hover, semantic tokens

| Component | Change needed | Complexity |
|-----------|--------------|------------|
| **TextMate grammar** (`tmLanguage.json`) | **Yes — generator update required.** The `fromOnHeader` pattern in `tools/Precept.GrammarGen/Program.cs` (L609–635) is a hand-written structural pattern, not catalog-derived. Its current regex (L617) already supports multi-state commas in the `from` capture group (`any\|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*`) but the `on` capture group (group 8) accepts only a single identifier: `[A-Za-z_][A-Za-z0-9_]*`. **The event capture group must be extended to accept comma-delimited lists** matching the state pattern: `[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*`. After updating the generator, regenerate `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` — the JSON file is a build output, not hand-edited. | Low. One regex change in generator, then regenerate. |
| **Semantic tokens** | Each state name should receive a `state` semantic token; each event name should receive an `event` semantic token. Driven by `StateReference` and `EventReference` entries from the type checker. | Low — dependent on type checker emitting per-name references. |
| **Completions** | After `from ` or after a comma in a state-target list: offer declared state names. After `on ` or after a comma in an event-target list: offer declared event names. | Low-medium. Need to detect "inside a target comma list" context for both slots. |
| **Hover** | Each state/event name in a list produces hover info. Same as today for single names — driven by reference entries. | None if references are emitted correctly. |
| **Go-to-definition** | Each name in a list is a valid go-to-definition source. Driven by `SymbolReference` entries. | None if references are emitted correctly. |
| **Preview / state diagram** | The transition preview shows the desugared rows — one per (state, event) pair. No new visual concept needed. | None — driven by the expanded `TypedTransitionRow` list. |

### 4.3 MCP — DTOs, vocabulary, compile output

| Component | Change needed | Complexity |
|-----------|--------------|------------|
| **`precept_compile` output** | Compiled definition contains expanded `TypedTransitionRow`s. Multi-state and multi-event rows desugar before the MCP tool sees them. No DTO changes. | None. |
| **`precept_language` vocabulary** | The `Constructs` catalog entry for `TransitionRow` (line 118 of `Constructs.cs`) contains a `UsageExample` string `"from Draft on Submit -> set reviewer = approver -> transition Submitted"`. This remains valid (single-state/single-event is still legal syntax) — no update required unless we want to showcase the new feature. The `ConstructSlotKind.StateTarget` comment (L18: `"state name or quantifier (any)"`) and `ConstructSlotKind.EventTarget` comment (L19: `"event name"`) should update to mention comma lists. MCP tool `precept_syntax` reads `CatalogFormatters.FormatSyntax()` which renders each construct's slots, examples, and disambiguation entries from catalog metadata — the MCP output updates automatically when catalog entries update. | Low. Comment-level changes in `ConstructSlot.cs`. |
| **`precept_syntax` tool** | `CatalogFormatters.FormatSyntax()` in `tools/Precept.Mcp/CatalogFormatters.cs` renders construct metadata. It does **not** hardcode `StateTarget` or `EventTarget` grammar rules — it reads `ConstructSlot` descriptions and `ConstructMeta` examples from the catalog. Once `ConstructSlot.cs` descriptions and `Constructs.cs` examples are updated, MCP output updates automatically. No code change to the MCP tool itself. | None — driven by catalog. |
| **MCP DTOs** | No changes to `PreceptTransitionRow`, `PreceptField`, `PreceptState`, etc. Desugaring happens before DTO projection. | None. |

---

## 5. Philosophy Check

### 5.1 Domain integrity, determinism, inspectability

**Preserved.** Both multi-state and multi-event comma lists are pure syntactic sugar with deterministic expansion. The runtime never sees them — it receives the same `TypedTransitionRow` records. Prevention guarantees, constraint evaluation, and proof obligations are unchanged because each desugared row is independently validated.

For multi-event rows, the arg-shape intersection constraint enforced at compile time preserves inspectability: every desugared row has well-typed event-arg references. The type checker catches arg-shape incompatibilities before the runtime ever sees the expanded rows.

### 5.2 Keyword-anchored, flat statements

**Preserved.** The construct shape doesn't change — it's still `from <target> on <event> ...` with a leading keyword. Adding names to the `on` slot follows the same comma-list pattern as `from` and `FieldTarget`. Each expanded row is still a flat, self-contained statement.

`from Active on Cancel, Expire, Withdraw -> transition Terminated` is exactly as flat and keyword-anchored as `from Active, Suspended on Cancel -> transition Terminated`. Both expand along one dimension — events or states — with the other dimension fixed.

### 5.3 AI legibility

**Improved for both dimensions.** An AI reading `from Active on Cancel, Expire, Withdraw -> transition Terminated` immediately sees the termination policy applies to three events. An AI reading three separate rows must diff them to confirm they are identical.

The intersection semantics for arg-shape compatibility are AI-friendly: they are a simple, statically-checkable rule. An AI agent generating a multi-event row knows exactly what is allowed — reference only args that exist on ALL listed events. No ambiguity, no runtime surprises.

### 5.4 Compactness vs. routing clarity

**Net positive.** The comma list saves (N-1) rows × (lines per row) of pure repetition along either dimension. The routing is more explicit, not less — the reader sees exactly which states AND which events share the behavior.

For multi-event rows, the arg-shape intersection constraint means the compact form is only available when the events are genuinely interchangeable in the context of that row's guards and actions. This is a feature — the type system prevents misleading consolidation.

### 5.5 Principle-by-principle assessment

| Principle | Multi-state verdict | Multi-event verdict |
|-----------|--------------------|--------------------|
| 1. Prevention, not detection | **Preserved.** Same validation on expanded rows. | **Preserved.** Arg-shape compatibility is a compile-time check. |
| 2. No ambient authority | **Preserved.** | **Preserved.** |
| 3. Deterministic semantics | **Preserved.** Expansion is deterministic. | **Preserved.** Substitution is deterministic. |
| 4. Immutable source of truth | **Preserved.** | **Preserved.** |
| 5. Flat, keyword-anchored statements | **Preserved.** Same construct shape. | **Preserved.** Same construct shape. |
| 6. First-match routing | **Preserved.** Expansion preserves source order. | **Preserved.** Expansion preserves source order. |
| 7. Self-contained rows | **Minor tension — acceptable.** Reader must know the copy-expansion rule. | **Moderate tension — acceptable.** Reader must know the substitution rule. But the substitution is mechanical and the intersection constraint makes it safe. |
| 8. Sound static analysis | **Preserved.** Each state name independently validated. | **Strengthened.** Arg-shape intersection is a new compile-time guarantee that doesn't exist in any comparable system. |
| 9. Tooling drives syntax | **Positive.** Completions, hover, go-to-def work naturally. | **Positive.** Same tooling patterns apply to event names. |
| 10. Consistent prepositions | **Positive.** Extends `FieldTarget` pattern to `StateTarget`. | **Positive.** Extends the same pattern to `EventTarget`. |
| 12. AI is a first-class consumer | **Positive.** | **Positive.** Intersection rule is simple for AI to follow. |
| 13. Keywords for domain, symbols for math | **Preserved.** | **Preserved.** |

---

## 6. Alternatives Considered

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

```precept
from Active on Cancel when Cancel.Reason == "fraud", Expire -> transition Terminated
```

This alternative allows different guard expressions per event within a single row. More expressive than intersection semantics — each event can have its own guard or no guard. But the grammar complexity is significantly higher: the parser must handle `EventName [when GuardExpr]? ("," EventName [when GuardExpr]?)*`, which nests guard expressions inside a comma list. This breaks the flat left-to-right parsing model. The construct is no longer a simple comma list — it's a list of guarded-event pairs.

The readability cost is high. `from Active on Cancel when Cancel.Reason == "fraud", Expire, Withdraw when Withdraw.Note is set -> transition Terminated` buries three different guard policies in one line with no visual structure.

**Rejected — grammar complexity and readability cost outweigh expressiveness. Authors who need per-event guards should write separate rows.**

### 6.5 No-guard restriction for multi-event rows

Multi-event rows with any guard or event-arg reference in actions are prohibited; guards require single-event rows. Simple to implement and trivially safe, but eliminates the shared-arg case entirely. The intersection semantics approach handles the shared-arg case correctly at compile time with no runtime cost. Banning guards is an artificial restriction when the type system can enforce the real constraint.

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
| `docs/language/precept-language-spec.md` §2.3 (L826) | **Yes** — transition row grammar `from StateTarget on Identifier` must become `from StateTarget on EventTarget`. The file has no formal `StateTarget :=` or `EventTarget :=` production — these are used inline. Add formal grammar rules matching the spike's §2.3: `StateTarget := Identifier ("," Identifier)* \| any` and `EventTarget := Identifier ("," Identifier)*`. Also update the state ensure (L855), state action (L873), and access mode/omit grammar (L896–909) sections to use the new `StateTarget` production consistently. | The spec is the language's source of truth. Every grammar rule referencing `StateTarget` or `EventTarget` must reflect the new comma-list form. |
| `docs/language/precept-grammar.md` (L212, L263, L275, L287–288, L319, L331, L342, L484–489, L505–507, L829–833) | **Yes** — multiple references to `StateTarget` and `EventTarget` describe them as single-name slots. The slot table (L484–485) says `StateTarget = "A state name reference"` and `EventTarget = "An event name reference"` — both need updating to "state name(s)" / "event name(s)" or "comma-delimited list or wildcard." The TransitionRow slot decomposition (L505–507) must mention that `StateTarget` and `EventTarget` slots can contain lists, breaking the offset-2 disambiguation invariant. | This is the grammar design reference; stale slot descriptions mislead implementers. |
| `docs/language/catalog-system.md` (L547, L1643–1644, L1974) | **No change required.** The catalog-system doc describes `ConstructSlotKind` as a helper enum (L547) and lists slot kinds (L1643–1644, L1974) but does not define the grammar of `StateTarget`/`EventTarget` slots — it defers to the grammar docs. The descriptions are structural ("state name", "event name") and don't encode single-vs-list semantics. | The catalog-system doc describes catalog architecture, not grammar rules. |
| `docs/language/PreceptLanguageDesign.md` | **Does not exist.** No file at this path. | N/A. |
| Other `docs/language/*.md` | **No change required.** No other language docs describe transition construct syntax at the grammar-rule level. | N/A. |

### 7.2 Compiler docs

| File | Change required | Rationale |
|------|-----------------|-----------|
| `docs/compiler/parser.md` (L51–52, L193–200, L229–230) | **Yes — critical.** Three updates: (1) Slot type table (L51–52): `StateTargetSlot` is `string?` and `EventTargetSlot` is `string?` — both must become list-capable types (e.g., `ImmutableArray<string>` or a new list slot type). (2) **Disambiguation invariant (L193–200) must be revised.** The decision block states that the disambiguation token is structurally invariant at peek(2) because "every StateScoped construct's first slot is `StateTarget` (a single identifier)." With multi-state comma lists, `from Draft, Pending on Submit` has the disambiguation token (`on`) at offset 4+. The parser must scan past the comma-delimited list to find the disambiguation token. The invariant must be restated. (3) Slot walking descriptions for `StateTarget`/`EventTarget` (L229–230). | The parser doc is the implementation specification. The peek-at-2 invariant is the most critical doc update — an implementer relying on it would produce incorrect disambiguation logic. |
| `docs/compiler/type-checker.md` (L59–60) | **Yes.** Slot type table: `StateTargetSlot` is `string? StateName` and `EventTargetSlot` is `string? EventName` — both must reflect the list-capable form. | Matches the parser slot type changes. |
| `docs/compiler/name-binder.md` (L181–182, L195–196, L201, L222–223, L280, L357) | **Yes.** Multiple references describe `StateTarget` and `EventTarget` as single-name resolution targets. L195–196 explicitly states "Every StateScoped construct's first slot is `StateTarget` (a single identifier)" — this restates the peek-at-2 invariant and must be updated. `SymbolReference` and `SymbolResolution` descriptions must account for per-name references from a list. | The name binder resolves each name in a comma list independently; its doc must describe this. |
| `docs/compiler/grammar-generator.md` (L129) | **Yes.** The `fromOnHeader` description says `from State[, ...] on Event` — already shows multi-state. Must be updated to `from State[, ...] on Event[, ...]` to include multi-event. | Description string only, but must be accurate. |
| `docs/compiler/tooling-surface.md` (L502–503, L526–528, L546–547, L560–561) | **No change required.** The completions infrastructure describes `InStateTarget` and `InEventTarget` as slot contexts that offer declared state/event names. Adding comma lists doesn't change what completions are offered — it changes *when* the context triggers (also after commas). The completions provider code must handle this, but the doc's description of the completion mapping is already abstract enough to cover it. | The doc describes the mapping table, not the trigger logic. |
| `docs/tooling/language-server.md` (L326–327, L353–354, L374–375) | **No change required.** Same reasoning as `tooling-surface.md` — describes the slot-context-to-completion mapping, which is unchanged. | The mapping is abstract; trigger detection is implementation detail. |

### 7.3 Grammar generator and TextMate grammar

| File | Change required | Rationale |
|------|-----------------|-----------|
| `tools/Precept.GrammarGen/Program.cs` (L609–635) | **Yes.** The `fromOnHeader` structural pattern (L617) regex: `^(\\s*)(from)(\\s+)(any\|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)(\\s+)(on)(\\s+)([A-Za-z_][A-Za-z0-9_]*)`. **Capture group 4** (state position) already supports comma lists. **Capture group 8** (event position) accepts only a single identifier. Must extend group 8 to: `[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*`. | The grammar generator is the authoritative source. The hand-authored `tmLanguage.json` is a build output. |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (L666–670) | **Yes — regenerated, not hand-edited.** After updating `Program.cs`, run the grammar generator to emit the updated file. The current `fromOnHeader` regex (L670) has the same single-identifier limitation in the event capture group. The regenerated file will contain the corrected regex. **Do not hand-edit this file.** | Build output. The generator is the source of truth per `docs/compiler/grammar-generator.md`. |

### 7.4 MCP tools and catalog entries

| File | Change required | Rationale |
|------|-----------------|-----------|
| `src/Precept/Language/ConstructSlot.cs` (L18–19) | **Yes.** Comment updates: L18 `StateTarget = 10, // state name or quantifier (any)` → `// state name(s) — comma-delimited list or quantifier (any)`. L19 `EventTarget = 11, // event name (or "initial" marker)` → `// event name(s) — comma-delimited list`. These comments are the slot-kind documentation; MCP's `precept_syntax` tool reads `ConstructSlot.Description` field, not comments, but the comments must stay accurate. | Code comments that are actively wrong mislead all consumers. |
| `src/Precept/Language/Constructs.cs` (L114–122) | **Optional.** The `TransitionRow` `UsageExample` (L118) is `"from Draft on Submit -> set reviewer = approver -> transition Submitted"` — single-state, single-event. This remains valid syntax. Optionally add a second example or update to showcase the feature, but not required for correctness. The `SlotStateTarget` and `SlotEventTarget` slot objects (L29–31) use the `ConstructSlotKind` enum and `Description` property from `ConstructSlot` constructor — these should get updated `Description` strings if MCP consumers should see comma-list documentation. | The example is valid. The slot `Description` fields drive MCP output, so updating them propagates to all MCP tool consumers automatically. |
| `tools/Precept.Mcp/CatalogFormatters.cs` | **No change required.** `FormatSyntax()` (L43+) reads construct metadata generically — it iterates `Constructs.All` and renders slots, examples, and disambiguation from catalog records. It has no hardcoded `StateTarget` or `EventTarget` references. When catalog entries update, MCP output updates automatically. | Thin wrapper — no domain logic. |
| `tools/Precept.Mcp/Tools/SyntaxTool.cs` | **No change required.** Delegates to `CatalogFormatters.FormatSyntax()`. No hardcoded grammar references. | Thin wrapper. |
| Other `tools/Precept.Mcp/Tools/*.cs` | **No change required.** `CompileTool.cs` projects desugared `TypedTransitionRow`s — comma lists are invisible after expansion. No MCP DTO changes needed. | Desugaring happens upstream. |

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
| `src/Precept/Pipeline/Parser.cs` | `ParseStateTarget` → comma-list loop. `ParseEventTarget` → comma-list loop. Return list-capable slot types. | ~30 lines. Model: `ParseFieldTarget` (L947–960). |
| `src/Precept/Pipeline/Parser.cs` | Disambiguation logic update — scanner must skip comma-delimited lists to find the disambiguation token, replacing the peek-at-2 assumption. | Critical parser infrastructure change. |
| `src/Precept/Pipeline/TypeChecker.cs` | `NormalizeTransitionRow`, `NormalizeStateEnsure`, `NormalizeAccessMode`, `NormalizeOmitDeclaration`, `NormalizeStateAction` — multi-state expansion (pure copy). `NormalizeTransitionRow` — multi-event expansion (substitution-based) + arg-shape validation. | ~130 lines total. |
| `src/Precept/Language/Diagnostics.cs` | 5 new diagnostic codes: `StateListContainsWildcard`, `DuplicateStateInList`, `DuplicateEventInList`, `EventArgShapeIncompatible`, `EventArgTypeMismatch`. | Catalog entries with messages, severity, hints. |
| `src/Precept/Language/ConstructSlot.cs` | Update `StateTarget` and `EventTarget` comments and `Description` fields. | 2 lines. |
| `src/Precept/Language/Constructs.cs` | Update `SlotStateTarget` and `SlotEventTarget` slot `Description` strings. Optionally update `TransitionRow` `UsageExample`. | 2–3 lines. |

### 8.2 Documentation changes (required, same PR)

| File | Change | Notes |
|------|--------|-------|
| `docs/language/precept-language-spec.md` | Add formal `StateTarget` and `EventTarget` grammar rules. Update transition row (L826), state ensure (L855), state action (L873), access mode (L896–909) grammar blocks. | Language spec is the source of truth. |
| `docs/language/precept-grammar.md` | Update slot descriptions (L484–489, L505–507, L829–833), TransitionRow decomposition, and all `StateTarget`/`EventTarget` inline references. **Revise or caveat the offset-2 disambiguation invariant discussion** (which cross-references `parser.md`). | Grammar design reference. |
| `docs/compiler/parser.md` | (1) Update slot type table (L51–52). (2) **Revise disambiguation invariant decision block** (L193–200) — the peek-at-2 assumption no longer holds. (3) Update slot walking descriptions (L229–230). | Critical — implementers rely on this. |
| `docs/compiler/type-checker.md` | Update slot type table (L59–60). | Matches parser slot type changes. |
| `docs/compiler/name-binder.md` | Update single-identifier assumptions at L181–182, L195–196, L201, L222–223, L280, L357. | Name binder resolves per-name from list. |
| `docs/compiler/grammar-generator.md` | Update `fromOnHeader` description (L129): `from State[, ...] on Event` → `from State[, ...] on Event[, ...]`. | Description accuracy. |

### 8.3 Tooling changes (required)

| File | Change | Notes |
|------|--------|-------|
| `tools/Precept.GrammarGen/Program.cs` | Update `fromOnHeader` regex (L617) — extend event capture group 8 to accept comma-delimited lists. | 1 regex change. |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | **Regenerated** from grammar generator after the above change. Not hand-edited. | Build output. |

### 8.4 MCP changes

| File | Change | Notes |
|------|--------|-------|
| `tools/Precept.Mcp/CatalogFormatters.cs` | **No change.** Reads catalog metadata generically. | Updates propagate from catalog. |
| `tools/Precept.Mcp/Tools/*.cs` | **No change.** All thin wrappers. Desugaring is upstream. | N/A. |

### 8.5 Samples (selective)

| File | Change | Notes |
|------|--------|-------|
| `samples/hiring-pipeline.precept` | Consolidate 3 RejectCandidate rows into 1 multi-state row. | Primary showcase. |
| `samples/it-helpdesk-ticket.precept` | Consolidate repeated state-subset rows. | Demonstrates value. |
| `samples/utility-outage-report.precept` | Consolidate repeated state-subset rows. | Demonstrates value. |

### 8.6 Tests

| Area | Expected tests |
|------|---------------|
| Parser | Multi-state comma lists in `from`, `in`, `to` positions. Multi-event comma lists in `on` position. Mixed multi-state + multi-event. Edge cases: trailing comma, single-item list, `any` in list (error). |
| Type checker | Multi-state expansion (verify N rows emitted). Multi-event expansion with arg substitution. Arg-shape intersection validation. All 5 new diagnostics. Cartesian product (multi-state × multi-event). |
| Grammar generator | Regenerated grammar matches expected regex for `fromOnHeader`. |
| Samples | All updated samples compile clean with 0 diagnostics. |

---

## 9. Design Decisions (Locked)

### D1. Scope: states AND events — full scope

**Decision:** Comma-delimited lists apply to both `StateTarget` and `EventTarget` in the same proposal. Events are not deferred to a separate proposal.

**Rationale:** The two features share the same grammar pattern (`Identifier ("," Identifier)*`), the same parser infrastructure (`ParseIdentifierList`), and the same type-checker expansion site (`NormalizeTransitionRow`). Shipping states without events leaves the language inconsistently expressive — comma lists work in `from` and `in` but not `on`, despite `on` having the same syntactic shape. The arg-shape compatibility problem is real but has a clean solution (D3).

### D2. Expansion model: type-checker expansion (Path 2) — locked

**Decision:** The parser emits one `ParsedConstruct` with list-capable target slots. The type checker expands lists into N typed constructs during normalization.

**Rationale:** The parser should faithfully represent source structure. Expansion is a semantic operation — the type checker already handles `from any` by setting `FromState = null`. Expanding comma lists is the same kind of semantic operation. Path 2 keeps the parser's output 1:1 with source lines, which matters for diagnostic attribution and source-span accuracy.

**Alternative rejected:** Path 1 (parser expansion) would work but means the parser emits multiple constructs from one source line, complicating span tracking and making the parser responsible for semantic expansion — not its job.

**Tradeoff accepted:** The type checker's normalization methods grow in complexity. This is acceptable because the expansion logic is localized (one loop per normalization method) and the pattern already exists in `from any` handling.

### D3. Arg-shape compatibility: intersection semantics — locked

**Decision:** Guards and actions on multi-event rows may only reference event args that exist on ALL events in the list with compatible types. The type checker enforces this at compile time. If `Cancel.Reason` is in the guard and `Expire` has no `Reason` arg, that is a type error (`EventArgShapeIncompatible`).

**Rationale:** Intersection semantics are the simplest model that is both sound and useful. They require no new grammar (unlike event-gated guard syntax), impose no artificial restrictions (unlike the no-guard rule), and provide a clear compile-time guarantee. The rule is easy to explain: "if you reference an event arg in a multi-event row, every event must have that arg."

**Alternatives rejected:**
- *Event-gated guard syntax* (per-event guard clauses in a single row): Rejected for grammar complexity and readability cost. A row with three different guard clauses interleaved with event names is unreadable.
- *No-guard restriction* (multi-event rows with guards prohibited entirely): Rejected as unnecessarily restrictive. The intersection model handles shared-arg cases correctly — banning them is an artificial limitation.

**Tradeoff accepted:** Some valid multi-event rows with heterogeneous arg shapes cannot be consolidated. When `Cancel(Reason as string)` and `Expire()` have different arg shapes and the guard references `Reason`, the author must write two separate rows. This is the correct outcome — the events are not interchangeable in the context of that guard.

**Precedent:** No adjacent system combines multi-event transitions with typed event arguments (see `transition-shorthand.md` §Arg-Shape Compatibility). Intersection semantics are the conservative, sound answer to a novel problem. CSP events are unparameterized; SCXML events carry flat data objects; XState context is separate from event payloads. Precept's typed args are unique, and the intersection rule is the type-safe response.

### D4. All state-preposition constructs at once — locked

**Decision:** All constructs that use `StateTarget` get comma-list support in one pass: transition rows (`from`), state ensures (`in`), access modes (`in ... modify`), omit declarations (`in ... omit`), and state actions (`to`/`from`).

**Rationale:** The grammar change is in `ParseStateTarget`, which is shared by all constructs. Implementing comma lists for transition rows but not ensures or access modes would require artificial restrictions in the parser — checking which construct is being parsed to decide whether to accept a comma. That is complexity in the wrong direction.

### D5. Sample corpus update — selective

**Decision:** Update the most verbose samples (`it-helpdesk-ticket`, `utility-outage-report`, `hiring-pipeline`) to use comma-list syntax where it eliminates pure repetition. Keep some samples in expanded form for pedagogical value.

**Rationale:** The sample corpus is the canonical usage reference. It should demonstrate available features. But not every sample needs conversion — keeping some in expanded form shows both styles and helps authors who are learning the language.

---

## 10. Recommendation

### Architectural verdict: Proceed to proposal issue — full scope, states and events.

The comma-delimited list syntax for `StateTarget` and `EventTarget` is:

1. **Grammatically consistent** — mirrors the existing `FieldTarget` comma-list pattern (`Identifier ("," Identifier)*`). Extending both target slots to match `FieldTarget` eliminates an inconsistency in the grammar.
2. **Semantically clean** — multi-state is pure copy (deterministic expansion, zero runtime changes). Multi-event is substitution-based but mechanically simple (event-arg references are the only things rewritten).
3. **Type-safe for events** — the intersection semantics for arg-shape compatibility provide a compile-time guarantee that has no precedent in adjacent systems. Every desugared row has well-typed event-arg references.
4. **Philosophically aligned** — preserves flat statements, keyword anchoring, first-match routing, and AI legibility. Improves compactness without obscuring routing. The arg-shape intersection rule preserves inspectability.
5. **Already researched** — `research/language/expressiveness/transition-shorthand.md` provides the precedent survey, philosophy fit analysis, and semantic contracts. The arg-shape compatibility contract is documented and the intersection solution is the conservative-sound answer.

### Implementation cost estimate

| Component | Multi-state only | Full scope (states + events) |
|-----------|-----------------|------------------------------|
| Parser | ~20 lines | ~30 lines |
| Type checker (expansion) | ~50 lines | ~130 lines (expansion + arg-shape validation + substitution) |
| Diagnostics | 2 new codes | 5 new codes |
| Runtime / evaluator / graph / proof | 0 | 0 |
| Tooling (completions, semantic tokens) | Low | Low-medium (event completions in comma context) |
| **Total** | **~70 lines + 2 diagnostics** | **~160 lines + 5 diagnostics** |

The incremental cost of events over states-only is ~90 lines of type-checker logic (arg-shape validation + substitution) and 3 additional diagnostic codes. This is modest and well-bounded.

### Track recommendation

**Track A** (implementation PR, no new design doc). The feature is syntactic sugar with deterministic expansion. The grammar changes are one-line production rule extensions. The arg-shape intersection semantics add a new compile-time validation, but no new semantic concepts or runtime behavior. The language spec's `StateTarget` and `EventTarget` grammar rules get one-line updates each.

### Risk

**Low.** The multi-state expansion is trivially safe (pure copy). The multi-event expansion introduces the arg-shape compatibility check, which is novel but mechanically simple — it's a set intersection over event arg names/types, evaluated at compile time. No runtime ambiguity risk. The comma after an identifier in state-target or event-target position is unambiguous at LL(1) because no construct currently places a comma after these slots.

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

### B.2 Multi-event consolidation candidates

The current sample corpus has **very few** multi-event candidates. This is structural — events tend to carry different argument shapes, so their handling naturally diverges. The existing research predicted this: "genuine multi-event `on` candidates are rare" in the current corpus.

**No clean multi-event candidates exist in the current 20-sample corpus.** The closest candidate is `refund-request.precept` where `Decline(Note as string notempty)` and `Cancel(Reason as string optional)` both transition `from Submitted -> transition Declined` with a `set DecisionNote` action — but the arg names differ (`Note` vs `Reason`) and the optionality differs (`notempty` vs `optional`), so these fail the arg-shape intersection check.

**Where multi-event consolidation has future value:** Enterprise domains with terminal-event families — multiple no-arg events that all route to the same terminal state from the same source state. Examples: `Cancel`, `Expire`, `Withdraw`, `Abandon` all leading to `Terminated`. These patterns appear in subscription management, contract lifecycle, and case management domains that the current sample corpus doesn't fully represent.

The corpus impact is modest today, but the feature is correct to ship alongside multi-state because:
1. The grammar change is trivial (`EventTarget` production rule update).
2. The no-arg-reference case (most common for multi-event) is free — it degenerates to pure copy.
3. The arg-shape intersection semantics add a new compile-time guarantee that strengthens the type system.
4. Deferring events creates an inconsistency in the grammar that would need to be explained and eventually resolved anyway.

### B.3 Combined total

| Dimension | Rows saved (current corpus) | Future value |
|-----------|-----------------------------|--------------|
| Multi-state | ~15 (~7.7%) | High — scales with state count |
| Multi-event | 0 (current corpus) | Medium — scales with terminal-event families |
| **Total** | **~15** | **High** |
