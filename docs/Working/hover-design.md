# Precept Hover Design

**By:** Elaine  
**Version:** 3  
**Date:** 2026-05-12  
**Status:** Revised working draft — incorporates Frank + Kramer review feedback  
**Primary surface:** VS Code markdown hover

---

## Design Philosophy

Hover is a fast trust surface, not an onboarding tutorial. It should answer three questions in scan order: what this construct means for the business rule, where it applies, and whether Precept proved it, checks it at runtime, or still has an unresolved gap.

Lead with governed meaning, not syntax trivia. For constructs that carry authored rationale (`rule`/`ensure` via `because`, `reject` via `RejectReason`), that text leads as a blockquote. For constructs without preserved free-form rationale (`field`, `state`, `event`, `access`, transition rows, qualifiers), lead with type/kind metadata — the most meaningful structural fact available at compile time.

Tone should be precise and quiet. Status indicators can be strong, but the surrounding copy should read like a technical advisor: statically confirmed, runtime checked, unverified.

## Target User Context

The primary reader is a technically literate business author — think analyst comfortable in SQL or Python, using VS Code by choice, and already familiar with Precept basics. They understand terms like type, nullable, constraint, guard, and validation rule. Hover should explain what Precept is doing to their business logic, not teach compiler concepts.

## VS Code Rendering Constraints

Bold, blockquotes, code spans, and inline emoji render well in VS Code hover markdown. **Do not use tables, HTML, or deep nested lists** — hover width makes these degrade fast. All templates below use only safe markdown patterns.

## V1 / V2 Boundary

**V1 is compile-time only.** The `HoverHandler` receives only the current `Compilation` snapshot (tokens, manifest, symbols, semantics, graph, proof, diagnostics). It does not have access to an executable model, runtime descriptors, or inspect/fire/update projections. Everything in V1 templates must be derivable from compile-time artifacts.

**V2 (deferred):** Runtime preview integration, event-driven mutation reach, "typical effects" summaries, prose proof-gap text, qualifier-usage indexes, and guarded-access final write maps.

## Hover Design by Construct

Examples use `samples/inventory-item.precept`. Each hover should spend its first lines on meaning and status; raw syntax and deeper detail come second.

### 1. `field` declaration (stored)

**Lead line:** Type/qualifier metadata (no free-form description available at compile time).

#### Rendered example
```md
**field** `ListPrice`
⚡ **Runtime checked** — enforced on every mutation before commit
Type: `price` · not nullable · `in CatalogCurrency` · `of SaleUnit.dimension`
Writable: `Listed`, `LowStock` · Read-only: `Unlisted`, `Delisted`
Governed by: non-negative rule · positive-in-sellable ensures
```

#### Data sources (V1)
- Type checker: `TypedField.ResolvedType`, `Presence`, `DeclaredQualifiers` — **low cost**
- Graph analyzer: direct-edit state lists via `SemanticIndex.AccessModes` traversal — **medium cost**
- Proof engine: governed-by via `ProofLedger.ConstraintInfluence` (`ConstraintInfluenceEntry`) — **medium cost**

#### Deferred to V2
- Event-driven mutation reach (high cost — requires cross-referencing all transition row actions)

---

### 1b. `field` declaration (computed)

Computed fields (declared with `<-`) are never directly writable. Hover suppresses the writable-state map entirely.

#### Rendered example
```md
**computed field** `AverageCost`
⚡ **Runtime checked** — recomputed on every mutation before commit
Type: `price` · not nullable · `in CatalogCurrency` · `of SaleUnit.dimension`
Computed from: `TotalCost / QuantityOnHand`
Governed by: non-negative rule
```

#### Data sources (V1)
- Type checker: `TypedField.ResolvedType`, expression AST — **low cost**
- Proof engine: `ConstraintInfluenceEntry` for governing constraints — **medium cost**

---

### 2. `rule`

**Lead line:** `because` text (authored rationale preserved in `TypedRule.Message`). ✅ Feasible.

#### Rendered example
```md
**rule** `QuantityOnHand >= '0 {StockingUnit}'`
> Stock on hand cannot go negative
⚡ **Runtime checked** — boolean expression on `QuantityOnHand`
Scope: global — enforced after every mutation
If false: the operation is rejected before commit
Referenced fields: `QuantityOnHand`
```

For guarded rules:
```md
**rule** `when InForce: ApprovedAmount <= CoverageLimit`
> Approved claims cannot exceed coverage while the policy is active
⚡ **Runtime checked** — boolean expression on `ApprovedAmount`, `CoverageLimit`
Scope: global when `InForce`
If false: the operation is rejected before commit
Referenced fields: `ApprovedAmount`, `CoverageLimit`
```

#### Data sources (V1)
- Type checker: expression type and operand compatibility — **low cost**
- Proof engine: `ProofLedger.Obligations` keyed by `ConstraintContext(RuleIdentity)` — **low cost**
- Proof engine: `ProofLedger.ConstraintInfluence` → Referenced fields/args — **low cost** (Kramer N9)

#### Implementation notes
- Use `ConstraintInfluenceEntry` for the "Referenced fields" line, NOT `TypedRule.SemanticSubjects` (currently empty — Kramer N10)
- Rules are global data truth, not state-partitioned. Never frame scope as "all reachable states"
- Stateless precepts have no states and rules still hold — the scope line must work without states

---

### 3. `state`

**Lead line:** Modifiers + reachability metadata (no free-form description available at compile time).

#### Rendered example
```md
**state** `LowStock`
✅ **Proof verified** — reachable from `Listed`
Modifiers: *none*
Incoming: `FulfillOrder`, `RecordShrinkage`
Outgoing: `ReceiveShipment` → `Listed`, `ReturnOrder` → `Listed`, `AdjustInventory` → `Listed`
Writable here: `RestockThreshold`, `Supplier`, `SupplierCurrency`, `ListPrice`
Terminal reachable · active ensures: 3 (1 unverified)
```

For a state with modifiers:
```md
**state** `Approved` · `required`
✅ **Proof verified** — reachable; every initial→terminal path visits here
Modifiers: `required`
Incoming: `ReviewComplete`
Outgoing: `Disburse` → `Funded`, `Withdraw` → `Cancelled`
Writable here: `DisbursementDate`, `FinalAmount`
Terminal reachable · active ensures: 2
```

#### Data sources (V1)
- Graph analyzer: `StateGraph.Edges`, `ReachableStates`, incoming/outgoing transitions — **low cost**
- Graph analyzer: writable fields via `AccessModes` — **medium cost**
- Graph analyzer: terminal reachability via dead-end analysis — **medium cost** (derivable but indirect)
- Proof engine: active ensures by grouping `EnsuresByState` with proof obligations — **medium cost**
- Manifest: state modifiers (`terminal`, `required`, `irreversible`, `success`, `warning`, `error`) — **low cost**

---

### 4. `event` declaration

**Lead line:** Arg signature + fire eligibility metadata (no free-form description available at compile time).

#### Rendered example
```md
**event** `UpdateListPrice(Price as price)`
⚡ **Runtime checked** — args validated before transition
Can fire from: `Listed`, `LowStock`
Arg: `Price` is `price` · `in CatalogCurrency` · `of SaleUnit.dimension`
```

For an `initial` (constructor) event:
```md
**event** `initial CreateItem(Name as text, InitialStock as quantity)`
⚡ **Runtime checked** — constructor event (invoked via `CreateInstance`, not `Fire`)
Arg: `Name` is `text` · not nullable
Arg: `InitialStock` is `quantity` · `of StockingUnit.dimension`
```

#### Data sources (V1)
- Type checker: `TypedEvent.Args` — types, qualifiers, nullability — **low cost**
- Graph analyzer: `GraphEvent.HandledInStates` → "Can fire from" — **low cost**
- Proof engine: event ensure status — **low cost**
- Manifest: `initial` modifier — **low cost**

#### Deferred to V2
- "Typical effects" line (requires summarizing actions across all `TransitionRows` and `EventHandlers` — high-cost traversal, not a ready projection; flagged by both Frank N6 and Kramer N3)

---

### 5. `from ... on ... when ... ->` transition row

**Lead line:** Guard + outcome metadata (no free-form description available at compile time).

#### Rendered example
```md
**transition** `from LowStock on ReceiveShipment when ... -> Listed`
⚠️ **Unverified** — row is reachable, but one proof obligation remains
Guard: `PostShipmentStock > RestockThreshold`
Actions: `set TotalCost`, `add QuantityOnHand`, `recompute AverageCost`
Graph: source reachable · target `Listed` reachable
Proof gap: 1 unresolved obligation (qualifier arithmetic)
```

#### Data sources (V1)
- Type checker: mutation type-safety, coercions, assignment compatibility — **low cost**
- Graph analyzer: row reachability, target reachability — **low cost**
- Proof engine: row-scoped obligations, qualifier constraints — **medium cost**
- Manifest: guard expression, action order, outcome — **low cost**

#### V1 vs V2 for proof gaps
- **V1:** Show obligation count and diagnostic code/category (e.g., "1 unresolved obligation (qualifier arithmetic)")
- **V2 (deferred):** Prose proof-gap text like "compound-unit + currency arithmetic in shipment cost path" — not precomputed, would need diagnostic-driven formatting or a new summarizer (Kramer N4)

---

### 6. `ensure`

**Lead line:** `because` text (authored rationale preserved in `TypedEnsure.Message`). ✅ Feasible.

**Scope line must distinguish the four anchor types** (Frank N1) — these have fundamentally different enforcement semantics:

| Anchor | Scope line pattern | Semantics |
|--------|-------------------|-----------|
| `in` | Scope: residency (`in Listed`) | Checked while residing in state |
| `to` | Scope: entry gate (`to Listed`) | Checked on transitions entering state |
| `from` | Scope: exit gate (`from Listed`) | Checked on transitions leaving state |
| `on` | Scope: event args (`on Submit`) | Checked when event fires |

*(Table above is design reference only — not rendered in hover. Hover uses inline text.)*

#### Rendered example — residency (`in`)
```md
**ensure** `in Listed ensure ListPrice > '0 {CatalogCurrency}/{SaleUnit}'`
> A listed product must have a positive list price
⚡ **Runtime checked** — enforced on every mutation before commit
Scope: residency (`in Listed`)
Referenced fields: `ListPrice`
Violation rejects the operation
```

#### Rendered example — entry gate (`to`)
```md
**ensure** `to Approved ensure ApprovedAmount <= CoverageLimit`
> Cannot enter Approved with amount exceeding coverage
⚡ **Runtime checked** — enforced on transitions entering `Approved`
Scope: entry gate (`to Approved`)
Referenced fields: `ApprovedAmount`, `CoverageLimit`
Violation rejects the transition
```

#### Rendered example — event args (`on`)
```md
**ensure** `on Submit ensure Amount > '0 USD'`
> Submitted amount must be positive
⚡ **Runtime checked** — enforced when `Submit` fires
Scope: event args (`on Submit`)
Referenced args: `Amount`
Violation rejects the event
```

#### Data sources (V1)
- Type checker: ensure expression type and field references — **low cost**
- Graph analyzer: states where the ensure is active — **low cost**
- Proof engine: `ProofLedger.Obligations` keyed by `EnsureIdentity` — **low cost**
- Proof engine: `ProofLedger.ConstraintInfluence` → Referenced fields/args — **low cost** (Kramer N9)

#### Implementation notes
- Use `ConstraintInfluenceEntry` for referenced fields/args, NOT `TypedEnsure.SemanticSubjects` (currently empty — Kramer N10)

---

### 7. `in <State> modify <Field> editable` declaration

**Lead line:** Write-set metadata (no free-form description available at compile time).

#### Rendered example
```md
**access** `in Listed modify RestockThreshold, Supplier, SupplierCurrency, ListPrice editable`
✅ **Proof verified** — write map is structural
Editable here: `RestockThreshold`, `Supplier`, `SupplierCurrency`, `ListPrice`
Same write set in `LowStock` · locked in `Unlisted`, `Delisted`
```

#### Data sources (V1)
- Type checker: referenced field existence and duplicate safety — **low cost**
- Graph analyzer: `TypedAccessMode` raw declaration — **low cost**
- Graph analyzer: same-write-set/locked-state summaries via scanning other access declarations — **medium cost**

#### Limitations (V1)
- Guarded access: the final state×field write map is NOT materialized today. V1 shows the raw declaration; V2 can add computed effective maps.

---

### 7b. `in <State> omit <Field>` declaration

`omit` means the field is **structurally absent** in that state — not just read-only. This is semantically distinct from `editable`/locked access.

#### Rendered example
```md
**omit** `in Unlisted omit ListPrice`
✅ **Proof verified** — field is structurally absent in `Unlisted`
`ListPrice` does not exist in this state — not readable, not writable
Restored on transition to: `Listed`, `LowStock`
```

#### Data sources (V1)
- Manifest: omit declaration — **low cost**
- Graph analyzer: which transitions restore the field (states where it's not omitted) — **medium cost**

---

### 8. `reject "message"`

**Lead line:** `RejectReason` (authored text preserved on the reject row). ✅ Feasible.

#### Rendered example
```md
**reject** `from Listed on FulfillOrder -> reject`
> Insufficient stock to fulfill this order
⚡ **Runtime checked** — deliberate business rejection
Result: state unchanged · no field mutations commit
```

#### Data sources (V1)
- Manifest: `RejectReason` text — **low cost**
- Graph analyzer: row reachability — **low cost**

#### V1 vs V2 for row ordering
- **V1:** Show the reject reason and outcome. Omit the "selected when no earlier guard matches" line — it requires ordered row analysis for the same `(state, event)` pair plus wildcard-row handling, which has no existing projection (Kramer N6).
- **V2:** Add "Selected when: no earlier `<Event>` row guard matches" once ordered-row analysis is available.

---

### 9. Qualifier expressions (`of ...`, `in ...`)

**Lead line:** Axis/type metadata (no free-form description available at compile time).

#### Rendered example
```md
**qualifier** `of '{StockingUnit.dimension}'`
✅ **Proof verified** — qualifier resolves from `StockingUnit`
Axis: physical dimension
Checks: assignments, comparisons, arithmetic stay dimension-compatible
Mismatch: incompatible combinations are rejected
```

#### Data sources (V1)
- Type checker: `QualifiedTypeReference.Qualifiers`, `DeclaredQualifierMeta` — **low cost**
- Proof engine: qualifier compatibility across expressions — **medium cost**

#### Deferred to V2
- "Applied to `X`, `Y`" line — requires a repo-wide qualifier-usage index scan that does not exist today (Kramer N7). Omit from V1.

---

## Status Indicators

- `✅ Proof verified` — statically established for the hovered construct.
- `⚡ Runtime checked` — enforced on every mutation before commit, not fully pre-proved.
- `⚠️ Unverified` — a relevant proof or analysis obligation is still unresolved.

Note: "Runtime checked" means enforcement happens at mutation time. Inspection (`Inspect`) is a non-mutating preview — it evaluates constraints but does not enforce (does not reject or commit). Status indicators describe enforcement semantics, not preview behavior.

---

## Implementation Notes for Kramer

### Resolver order
- Resolve the enclosing construct first: rule, ensure, reject row, transition row, access declaration, omit declaration, qualifier span.
- Reject must win before the generic transition-row hover because `-> reject` rows live in the same `TransitionRows` projection and would otherwise fall through to the transition template.
- Fall back to token hover only when no construct-level result exists.

### Available data (V1 — compile-time only)
- `SemanticIndex`: fields, states, events, rules, ensures, access modes, transition rows, span-to-construct refs, qualifier bindings.
- Type-check results: declared type, nullability, coercions, expression compatibility, mutation assignment safety.
- `StateGraph`: reachability, dead states, incoming/outgoing counts, event coverage, terminal reachability.
- `ProofLedger` + diagnostics: per-construct status, `ConstraintInfluenceEntry` (not SemanticSubjects), unresolved obligations, related spans.
- Manifest: modifiers, guard expressions, action sequences, reject reasons, omit declarations.

### NOT available in V1
- No executable model or runtime descriptors
- No inspect/fire/update projections
- No ordered mutation summaries from evaluator
- No qualifier-usage index (cross-field qualifier scan)
- No precomputed prose proof-gap text
- No guarded-access final write maps

### Derived projections (V1)
- `ConstructAtPosition` — resolve hover target
- `FieldWriteMapByState` — via AccessModes traversal (medium cost)
- `ConstraintInfluenceSummary` — from `ConstraintInfluenceEntry` (medium cost)
- `HoverStatusBadge` — from ProofLedger obligations

### V1 delivery boundary
- Markdown only — no rich hover actions
- All data from `Compilation` snapshot
- Proof status from `ProofLedger.Obligations` and diagnostics
- Referenced fields/args from `ConstraintInfluenceEntry`
- Shape DTOs for V2 extensibility but do not block V1 on runtime integration

---

## V2 Aspirations (Deferred)

These items are explicitly out of scope for V1 but should inform DTO design:

1. **Event "typical effects" summary** — cross-referencing all transition rows per event (Frank N6, Kramer N3)
2. **Prose proof-gap text** — natural-language summarization of unresolved obligations (Kramer N4)
3. **Qualifier "applied to" line** — requires qualifier-usage index (Kramer N7)
4. **Event-driven mutation reach** on field hover — high-cost traversal (Kramer N1)
5. **Reject row ordering context** — "selected when no earlier guard matches" (Kramer N6)
6. **Guarded access final write maps** — not materialized (Kramer N5)
7. **Runtime preview integration** — fire/inspect/update projections layered onto hover

---

## Open Questions for Shane

1. **Inline warnings:** when a construct already has a diagnostic squiggle, should hover repeat the unresolved proof gap inline? My recommendation: yes — one short sentence.
2. **Status wording:** keep `Proof verified` / `Runtime checked` / `Unverified` as the visible labels, or rename them? My recommendation: keep them.
3. **V1 boundary:** markdown only, or reserve hooks for richer hover actions later? My recommendation: ship markdown now, but shape DTOs for richer follow-ons.
4. **Demo mix:** should hover demos intentionally show both `✅` and `⚠️` states? My recommendation: yes — it demonstrates honest status reporting.
