# Precept Hover Design

**By:** Elaine  
**Version:** 4  
**Date:** 2026-05-12  
**Status:** Revised working draft — proof hover scenarios integrated  
**Primary surface:** VS Code markdown hover

---

## Design Philosophy

Hover is a fast trust surface, not an onboarding tutorial. It should answer three questions in scan order: what this construct means for the business rule, where it applies, and whether Precept proved it, checks it at runtime, or still has an unresolved gap.

Lead with governed meaning, not syntax trivia. For constructs that carry authored rationale (`rule`/`ensure` via `because`, `reject` via `RejectReason`), that text leads as a blockquote. For constructs without preserved free-form rationale (`field`, `state`, `event`, `access`, transition rows, qualifiers), lead with type/kind metadata — the most meaningful structural fact available at compile time.

Tone should be precise and quiet. Status indicators can be strong, but the surrounding copy should read like a technical advisor: statically confirmed, runtime checked, unverified.

### Proof hover principles

1. **Verdict first, evidence second, fix third** — Every proof hover should answer in this order: what Precept proved or could not prove, what evidence it used, and what the user can do next.
2. **AI-parseable structure** — Hover content should use a stable labeled order, not dense prose. Preferred label order: `Verdict` or `Status`, `Context`, `Expression` or `Subject`, `Requirement`, evidence lines (`Left operand`, `Right operand`, `Resolved qualifier`, `Qualifier source`, etc.), `Reason`, `Fix`.
3. **Honest proof language** — If the engine cannot prove something, the hover says **cannot prove** or **unresolved**. It does not overstate certainty with words like **incompatible** unless the engine actually has a proved mismatch verdict.
4. **Declaration truth and use-site truth are different** — A declaration hover explains the declaration's contract. A diagnostic hover explains the use-site failure. Do not blur them.
5. **Hover is for quick trust, not a mini spec** — The hover should be rich enough to repair the issue, but still scan in seconds. Short labeled lines beat paragraph explanations.

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

**Lead line:** Resolved qualifier evidence + active proof usage metadata.

#### Rendered example
```md
**qualifier** `in '{CatalogCurrency}'`

Status: Active in 3 proof-checked uses · 1 unresolved use
Axis: Currency
Declared form: `in '{CatalogCurrency}'`
Resolved value: `'{CatalogCurrency}'`
Resolved source: field `CatalogCurrency`
Resolved value shape: symbolic currency qualifier

Compatibility rule:
- money operands used together must share the same Currency qualifier
- qualifier-preserving operations keep this qualifier in the result

Open proof issues:
- computed field `GrossProfit` — PRE0114 on `(TotalRevenue - TotalReturns) - TotalCostOfGoods`
```

#### Data sources (V1)
- `QualifierHoverInfo`: `Axis`, `Span`, `Label`, `OwnerType`, `ResolvedQualifier` — **low cost**
- Type checker / semantic model: declared qualifier text, resolved qualifier value, symbolic qualifier source — **medium cost**
- Proof engine: active proof-checked use count, unresolved uses, qualifier-preserving strategy context — **medium cost**

#### Implementation notes
- Show resolved value, resolved source field, and open proof issues as separate labeled lines — do not collapse them into prose.
- If unresolved uses exist, do not force an always-positive `Proof verified` tone; this card is a live contract surface, not glossary text.

---

## Proof Hover Scenarios

### Scenario 1: Hovering a qualified field

Example declaration:

```precept
field TotalRevenue as money in '{CatalogCurrency}'
```

#### Trigger condition
The cursor is on a **field identifier** whose declaration includes at least one qualifier.

#### Hover card structure
Field identity stays first, but a proof block is added directly into the existing field hover.

Recommended order:
1. field label
2. status line
3. type
4. declared qualifier
5. resolved qualifier
6. qualifier source
7. proof contract
8. open proof issues summary

#### Recommended base card

```md
**field** `TotalRevenue`

Status: Proof contract active · No active proof issues
Type: `money`
Declared qualifier: `in '{CatalogCurrency}'`
Resolved qualifier: `'{CatalogCurrency}'`
Qualifier source: field `CatalogCurrency`

Proof contract:
- money arithmetic and comparisons require matching Currency qualifiers
- qualifier-preserving operations keep this qualifier in the result

Open proof issues: none
```

#### Proof status variants

##### Variant A — No active proof issues
Use when the field participates in proof-relevant operations but there are no unresolved obligations tied to current uses.

```md
Status: Proof contract active · No active proof issues
Open proof issues: none
```

##### Variant B — `N` unresolved uses
Use when current proof diagnostics or unresolved obligations reference this field at one or more use sites.

```md
Status: Proof contract active · 2 unresolved uses
Open proof issues:
- computed field `GrossProfit`
- transition `from Draft on Approve -> transition Approved`
```

##### Variant C — Proved via qualifier-preserving operations
Use when the field is currently used in proof-bearing expressions that all discharge through a known qualifier-preserving strategy.

```md
Status: Proof contract active · Proved via qualifier-preserving operations
Open proof issues: none
```

#### What data is needed from the runtime / semantic model
- `TypedField.Name`, `ResolvedType`, `DeclaredQualifiers`, `Qualifier`, `NameSpan`
- declaration syntax snippet for the authored qualifier text
- resolved qualifier value for the relevant axis
- symbolic qualifier source, if the qualifier resolves through another field or symbolic path
- proof-usage summary for this field's current use sites
- proof strategy labels for proved uses, if surfaced in aggregate form

**Implementation note:** current field hover already has most declaration data. The missing piece is the join from a field declaration to proof-bearing uses and unresolved obligations.

#### What NOT to show
- do **not** say the field declaration is broken when the failure is only at a use site
- do **not** show a red-failure tone on the declaration hover unless the declaration itself is the failing span
- do **not** collapse declared form, resolved value, and source into one sentence
- do **not** show generic qualifier education text before the field's actual proof contract

> The field hover should tell the user what contract this field carries. The diagnostic hover tells them where that contract failed to prove.

---

### Scenario 2: Hovering a binary expression with proof obligations

Example expression:

```precept
GrossProfit <- TotalRevenue - TotalReturns
```

#### Trigger condition
The cursor is on an operator or operand inside a `TypedBinaryOp` whose node has at least one proof obligation, or whose span overlaps a proof-stage fault site / diagnostic.

#### Routing rule for this scenario
The hover should target the **smallest proof-bearing binary expression** at the cursor, not the generic operator metadata.

#### Hover card structure
Use this order:
1. expression label
2. status / verdict
3. context
4. requirement
5. left operand evidence
6. right operand evidence
7. result type
8. result qualifier
9. proof strategy or failure reason
10. fix

#### Proved state hover card

```md
**expression** `(TotalRevenue - TotalReturns)`

Status: Proved
Context: computed field `GrossProfit`
Requirement: both operands must resolve to the same Currency qualifier
Left operand: `TotalRevenue`
Left qualifier: `'{CatalogCurrency}'`
Right operand: `TotalReturns`
Right qualifier: `'{CatalogCurrency}'`
Result type: `money`
Result qualifier: `'{CatalogCurrency}'`
Proof strategy: same-qualifier propagation
```

#### Unproved state hover card

```md
**expression** `(TotalRevenue - TotalReturns) - TotalCostOfGoods`

Status: Unresolved proof obligation
Context: computed field `GrossProfit`
Requirement: both operands must resolve to the same Currency qualifier
Left operand: `(TotalRevenue - TotalReturns)`
Left qualifier: not proved at this site
Right operand: `TotalCostOfGoods`
Right qualifier: `'{CatalogCurrency}'`
Result type: `money`
Result qualifier: unresolved
Reason: qualifier preservation for the left subexpression is not proved here
Fix: align qualifier sources or use an explicit conversion / explicit intermediate field
```

#### What data is needed
- `TypedBinaryOp.Left`
- `TypedBinaryOp.Right`
- `TypedBinaryOp.ResultType`
- `TypedBinaryOp.ResultQualifier`
- `TypedBinaryOp.ProofRequirements`
- `TypedBinaryOp.Span`
- matching `ProofObligation` entry or entries from `ProofLedger`
- proof disposition and strategy
- resolved left/right qualifier values at this exact expression site
- expression context (for example `FieldExpressionContext(TypedField)`)

**Important nuance:** `ResultQualifier` describes the propagation rule, not necessarily the fully humanized resolved value. The hover needs a resolution step, not just the raw DU case.

#### What NOT to do
- do not show generic operator help when a proof-bearing `TypedBinaryOp` is present
- do not show only `1 unresolved obligation`; the card must name the requirement and evidence
- do not surface raw type-checker object names like `TypedBinaryOp` or `QualifierBinding` in user-facing copy

> The expression hover is the flagship proof card. This is where the user expects Precept to explain itself.

---

### Scenario 3: Hovering the diagnostic squiggle (`PRE0114` and other proof codes)

#### Trigger condition
The cursor is within the span of a **proof-stage diagnostic**.

#### Design rule
The squiggle hover must be **richer than the Problems panel**.

- **Problems panel:** one-line verdict for list scanning
- **Hover:** mini proof card for local diagnosis and repair

#### Formatting rule
Use labeled multiline fields in hover. Do **not** compress proof context into inline parentheses.

| Surface | Format | Purpose |
|---|---|---|
| Problems panel | compact one-line message; inline square-bracket metadata is acceptable | scan many diagnostics quickly |
| Hover | multiline labeled proof card | understand one proof failure deeply |

#### Complete hover card example — `PRE0114`

```md
**PRE0114 — Cannot prove Currency qualifier compatibility**

Verdict: Cannot prove both operands resolve to the same Currency qualifier
Context: computed field `GrossProfit`
Expression: `(TotalRevenue - TotalReturns) - TotalCostOfGoods`
Requirement: both operands must resolve to the same Currency qualifier
Left operand: `(TotalRevenue - TotalReturns)`
Left qualifier: not proved at this site
Left qualifier source: unresolved
Right operand: `TotalCostOfGoods`
Right qualifier: `'{CatalogCurrency}'`
Right qualifier source: field `CatalogCurrency`
Status: unresolved
Reason: the nested subtraction result is not currently proved qualifier-preserving here
Fix:
- make both operands resolve through the same qualifier path
- or insert an explicit conversion / explicit intermediate field
```

#### Complete hover card example — `PRE0116` (presence requirement)

```md
**PRE0116 — Cannot prove presence**

Verdict: Cannot prove `TrackingNumber` is present before this access
Context: computed field `ShipmentSummary`
Expression: `TrackingNumber.length`
Requirement: optional fields must be proved present before access
Subject: `TrackingNumber`
Declared presence: optional
Status: unresolved
Reason: no guard or earlier assignment proves the field is set on this path
Fix:
- guard the usage with `when TrackingNumber is set`
- initialize the field earlier on every reachable path
- or remove `optional` if the field should always be present
```

#### Additional design notes
- Show the diagnostic code in the first line.
- Use the hover to expand evidence that would be too noisy in the Problems list.
- If the same span has both a proof diagnostic and generic construct hover, the diagnostic hover wins.

---

## Status Indicators

- `✅ Proof verified` — statically established for the hovered construct.
- `⚡ Runtime checked` — enforced on every mutation before commit, not fully pre-proved.
- `⚠️ Unverified` — a relevant proof or analysis obligation is still unresolved.

Note: "Runtime checked" means enforcement happens at mutation time. Inspection (`Inspect`) is a non-mutating preview — it evaluates constraints but does not enforce (does not reject or commit). Status indicators describe enforcement semantics, not preview behavior.

---

## Implementation Notes for Kramer

### Hover routing / precedence rules

Kramer should implement these routing rules explicitly.

1. **Proof diagnostic span wins over all other hovers.**  
   If the position falls within a proof diagnostic span, return the proof diagnostic hover first.

2. **Proof-bearing binary expression wins over generic operator hover.**  
   If the position is on a `TypedBinaryOp` with an associated proof obligation or proof diagnostic, return the expression proof hover instead of the generic operator hover.

3. **Qualified-field proof hover supplements field symbol hover.**  
   If the position is on a field identifier with a declared qualifier, run the field proof augmentation after symbol identification and render one combined field card.

4. **Qualifier hover is upgraded, not replaced.**  
   `TryCreateQualifierHover(...)` remains the qualifier-syntax entry point, but the card content becomes proof-aware.

#### Recommended routing order

| Proposed order | Reason |
|---|---|
| Proof diagnostic hover | most specific and most urgent |
| Proof-bearing expression hover | direct proof explanation at the operator/expression |
| Rich construct hover | existing rule/ensure/transition/access/omit content |
| Symbol hover with qualified-field proof block | declaration contract explanation |
| Generic operator / function / type hover | fallback only when no proof context exists |

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


### Data model requirements

This section is intentionally honest about what appears to exist now versus what likely needs new shaping.

#### From `Compilation`

| Need | Current availability | Notes |
|---|---|---|
| current token / position context | **Available** via `Compilation.Tokens` | already used by `HoverHandler` |
| semantic declarations and expressions | **Available** via `Compilation.Semantics` | already used by current hovers |
| proof ledger | **Available** via `Compilation.Proof` | includes obligations, fault-site links, diagnostics |
| diagnostics at a span | **Available** via `Compilation.Diagnostics` | `RichHoverFactory.GetDiagnosticsOverlapping(...)` already does overlap filtering |
| direct helper: proof diagnostic at cursor → matching obligation | **Not exposed as a helper** | today implementation appears to infer by overlapping span/code; a dedicated helper would be safer `[needs George to verify]` |

#### From `SemanticIndex`

| Need | Current availability | Notes |
|---|---|---|
| field declaration qualifiers | **Available** on `TypedField.DeclaredQualifiers` | enough for authored contract |
| field qualifier behavior | **Available** on `TypedField.Qualifier` | helpful for declaration hover; not the full resolved story |
| binary expression proof requirements | **Available** on `TypedBinaryOp.ProofRequirements` | direct signal for Scenario 2 |
| binary result qualifier propagation rule | **Available** on `TypedBinaryOp.ResultQualifier` | needs humanization for hover |
| smallest expression at cursor | **Available** via `SemanticExpressionLocator.TryFindExpressionAt(...)` | currently used for operator/accessor/function hover |
| qualifier value resolution for arbitrary expression/site | **Not exposed as a public hover helper** | current proof engine has internal resolution helpers; hover likely needs an extracted/shared resolver `[needs George to verify]` |
| qualifier usage index: declaration → use sites / open issues | **Not obvious in current model** | may need a derived language-server helper rather than a core-model addition `[needs George to verify]` |
| symbolic source extraction for qualifier value | **Partially present** | `RichHoverFactory.TryGetQualifierResolvedSource(...)` handles some declaration cases, but expression-site proof hover needs the same answer for operands too |

#### From `ProofLedger`

| Need | Current availability | Notes |
|---|---|---|
| proof obligations with site + context | **Available** on `ProofLedger.Obligations` | includes `Requirement`, `Site`, `Context`, `Disposition`, `Strategy`, `EmittedDiagnostic` |
| fault-site links | **Available** on `ProofLedger.FaultSiteLinks` | likely useful for diagnostic-hover routing |
| proof verdict | **Available** as `ProofDisposition` | `Proved` vs `Unresolved` |
| proof strategy | **Available** as `ProofStrategy?` | good input for proved hovers |
| explicit unresolved reason text | **Not visible in the current ledger model** | current model appears to give verdict but not a user-ready failure explanation `[needs George to verify]` |
| stable join between diagnostic hover and exact obligation | **Partially inferable** | probably joinable by span + code + site, but this should be confirmed before implementation `[needs George to verify]` |

#### Minimum helper APIs the hover path likely needs

Recommended helper layer for Kramer:
- `TryFindProofDiagnosticAtPosition(Compilation compilation, Position position, out Diagnostic diagnostic, out ProofObligation? obligation)`
- `TryFindProofBearingExpressionAtPosition(Compilation compilation, Position position, out TypedExpression expression, out ImmutableArray<ProofObligation> obligations)`
- `ResolveQualifierEvidence(TypedExpression expression, QualifierAxis axis, Compilation compilation)` → resolved value, source, resolution state
- `GetProofUsesForField(TypedField field, Compilation compilation)` → current open/clean use summary
- `HumanizeProofStrategy(ProofStrategy strategy)`

> If the current core model does not expose enough information for a truthful `Reason:` line, the hover should fall back to a precise generic explanation instead of inventing one.

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

### General

1. **Inline warnings:** when a construct already has a diagnostic squiggle, should hover repeat the unresolved proof gap inline? My recommendation: yes — one short sentence.
2. **Status wording:** keep `Proof verified` / `Runtime checked` / `Unverified` as the visible labels, or rename them? My recommendation: keep them.
3. **V1 boundary:** markdown only, or reserve hooks for richer hover actions later? My recommendation: ship markdown now, but shape DTOs for richer follow-ons.
4. **Demo mix:** should hover demos intentionally show both `✅` and `⚠️` states? My recommendation: yes — it demonstrates honest status reporting.

### Proof hover

1. **Proved hovers show proof strategy on a dedicated line.**  
   Example: `Proof strategy: same-qualifier propagation`. This is valuable, but it is more implementation-detail-forward than today's hover tone.

2. **Qualified-field hover stays one combined card rather than splitting into separate symbol and proof cards.**  
   This keeps declaration identity and proof contract together, but it makes field hover denser.

3. **Qualifier hover shows current open proof issues, not just static qualifier semantics.**  
   This makes the syntax hover operational, but it does turn a declaration-syntax hover into a program-state summary.

4. **Unresolved proof hovers prefer an honest generic reason over false specificity when the ledger cannot provide a concrete failure explanation.**  
   Example fallback: `Reason: qualifier preservation is not proved here` rather than guessed internals.

5. **Proof hover wins even when it suppresses otherwise-useful generic operator documentation.**  
   This is the right UX for proof as a flagship surface, but it is a real routing tradeoff.
