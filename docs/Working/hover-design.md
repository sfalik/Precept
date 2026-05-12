# Precept Hover Design

**By:** Elaine · **V5** · **2026-05-12** · VS Code markdown hover

---

## Overview

Hover is a fast trust surface. It answers: what this construct means for the business rule, where it applies, and whether Precept **proved** it, **runtime-checks** it, or has an **unresolved gap**.

Lead with governed meaning — `because` rationale as a blockquote where authored, type/kind metadata where not. For proof-bearing constructs, every hover answers: what was proved OR why it couldn't be proved, compactly. Primary reader: technically literate business author comfortable in SQL/Python, already familiar with Precept basics.

---

## Construct Quick Reference

| Construct | Leading content | Proof status shown |
|---|---|---|
| `field` (stored) | Type · qualifier · access map · governing constraints | ⚡ Runtime checked |
| `field` (computed) | Type · expression · governing constraints | ⚡ Runtime checked |
| `rule` | `because` rationale · scope · referenced fields | ⚡ Runtime checked |
| `state` | Modifiers · reachability · incoming/outgoing · write set | ✅ / ⚠️ |
| `event` | Arg signature · eligible states | ⚡ Runtime checked |
| transition row | Guard · actions · graph status · proof block | ✅ / ⚠️ |
| `ensure` | `because` rationale · anchor type · scope | ⚡ Runtime checked |
| `access` | Write-set metadata · state coverage | ✅ Proof verified |
| `omit` | Structural absence · restore states | ✅ Proof verified |
| `reject` | Reject reason · outcome | ⚡ Runtime checked |
| qualifier | Resolved value · source · active uses · open issues | Per-use status |
| qualified field (proof) | Declaration contract · open proof issues | Proof contract status |
| binary expression (proof) | Operand evidence · result qualifier · verdict | ✅ Proved / ⚠️ Unresolved |
| diagnostic squiggle | Proof verdict · failure evidence · fix hint | ⚠️ Unresolved / specific |

---

## 1. `field` (stored)

```md
**field** `ListPrice`
⚡ **Runtime checked** — enforced on every mutation before commit
Type: `price` · not nullable · `in CatalogCurrency` · `of SaleUnit.dimension`
Writable: `Listed`, `LowStock` · Read-only: `Unlisted`, `Delisted`
Governed by: non-negative rule · positive-in-sellable ensures
```

**Data sources (V1):** type checker (`ResolvedType`, `Presence`, `DeclaredQualifiers`) · graph (`AccessModes` traversal) · proof (`ConstraintInfluenceEntry`)  
**V2 deferred:** event-driven mutation reach (high cost — requires cross-referencing all transition row actions)

---

## 2. `field` (computed)

Computed fields (`<-`) are never directly writable — writable-state map suppressed entirely.

```md
**computed field** `AverageCost`
⚡ **Runtime checked** — recomputed on every mutation before commit
Type: `price` · not nullable · `in CatalogCurrency` · `of SaleUnit.dimension`
Computed from: `TotalCost / QuantityOnHand`
Governed by: non-negative rule
```

**Data sources (V1):** type checker (`ResolvedType`, expression AST) · proof (`ConstraintInfluenceEntry`)

---

## 3. `rule`

```md
**rule** `QuantityOnHand >= '0 {StockingUnit}'`
> Stock on hand cannot go negative
⚡ **Runtime checked** — boolean expression on `QuantityOnHand`
Scope: global — enforced after every mutation
If false: operation rejected before commit
Referenced fields: `QuantityOnHand`
```

Guarded variant:

```md
**rule** `when InForce: ApprovedAmount <= CoverageLimit`
> Approved claims cannot exceed coverage while the policy is active
⚡ **Runtime checked** — boolean expression on `ApprovedAmount`, `CoverageLimit`
Scope: global when `InForce`
If false: operation rejected before commit
Referenced fields: `ApprovedAmount`, `CoverageLimit`
```

**Data sources (V1):** type checker (expression type, operand compatibility) · proof (`ProofLedger.Obligations` by `ConstraintContext(RuleIdentity)`, `ConstraintInfluence`)  
**Notes:** Use `ConstraintInfluenceEntry` for referenced fields — NOT `TypedRule.SemanticSubjects` (currently empty, Kramer N10). Rules are global data truth — never frame scope as "all reachable states." Stateless precepts have no states; scope line must work without them.

---

## 4. `state`

```md
**state** `LowStock`
✅ **Proof verified** — reachable from `Listed`
Modifiers: *none*
Incoming: `FulfillOrder`, `RecordShrinkage`
Outgoing: `ReceiveShipment` → `Listed`, `ReturnOrder` → `Listed`, `AdjustInventory` → `Listed`
Writable here: `RestockThreshold`, `Supplier`, `SupplierCurrency`, `ListPrice`
Terminal reachable · active ensures: 3 (1 unverified)
```

With modifiers:

```md
**state** `Approved` · `required`
✅ **Proof verified** — reachable; every initial→terminal path visits here
Modifiers: `required`
Incoming: `ReviewComplete`
Outgoing: `Disburse` → `Funded`, `Withdraw` → `Cancelled`
Writable here: `DisbursementDate`, `FinalAmount`
Terminal reachable · active ensures: 2
```

**Data sources (V1):** graph (`StateGraph.Edges`, `ReachableStates`, `AccessModes`, dead-end analysis) · proof (`EnsuresByState` + obligations) · manifest (modifiers: `terminal`, `required`, `irreversible`, `success`, `warning`, `error`)

---

## 5. `event`

```md
**event** `UpdateListPrice(Price as price)`
⚡ **Runtime checked** — args validated before transition
Can fire from: `Listed`, `LowStock`
Arg: `Price` is `price` · `in CatalogCurrency` · `of SaleUnit.dimension`
```

Constructor variant:

```md
**event** `initial CreateItem(Name as text, InitialStock as quantity)`
⚡ **Runtime checked** — constructor event (invoked via `CreateInstance`, not `Fire`)
Arg: `Name` is `text` · not nullable
Arg: `InitialStock` is `quantity` · `of StockingUnit.dimension`
```

**Data sources (V1):** type checker (`TypedEvent.Args`) · graph (`GraphEvent.HandledInStates`) · proof (event ensure status) · manifest (`initial` modifier)  
**V2 deferred:** "Typical effects" line — high-cost traversal, no ready projection (Frank N6, Kramer N3)

---

## 6. Transition row (`from … on … when … ->`)

```md
**transition** `from LowStock on ReceiveShipment when ... -> Listed`
⚠️ **Unverified** — row is reachable, but one proof obligation remains
Guard: `PostShipmentStock > RestockThreshold`
Actions: `set TotalCost`, `add QuantityOnHand`, `recompute AverageCost`
Graph: source reachable · target `Listed` reachable
Proof: qualifier arithmetic
  Verdict: Cannot prove — 1 unresolved obligation
  Evidence: compound-unit quantity operands in shipment cost path
  Fix: ensure matching qualifier axes on both sides of the arithmetic
```

**Data sources (V1):** type checker (mutation type-safety, coercions) · graph (row/target reachability) · proof (row-scoped obligations, qualifier constraints) · manifest (guard, action order, outcome)  
**V1 note:** show obligation count + diagnostic category. Prose gap text is V2 (Kramer N4).

---

## 7. `ensure`

Scope line varies by anchor type:

| Anchor | Scope text | Semantics |
|---|---|---|
| `in` | `Scope: residency (in Listed)` | Checked while residing in state |
| `to` | `Scope: entry gate (to Listed)` | Checked on transitions entering state |
| `from` | `Scope: exit gate (from Listed)` | Checked on transitions leaving state |
| `on` | `Scope: event args (on Submit)` | Checked when event fires |

*(Table is design reference — not rendered in hover. Hover uses the inline scope text.)*

**Residency (`in`):**

```md
**ensure** `in Listed ensure ListPrice > '0 {CatalogCurrency}/{SaleUnit}'`
> A listed product must have a positive list price
⚡ **Runtime checked** — enforced on every mutation before commit
Scope: residency (`in Listed`)
Referenced fields: `ListPrice`
Violation rejects the operation
```

**Entry gate (`to`):**

```md
**ensure** `to Approved ensure ApprovedAmount <= CoverageLimit`
> Cannot enter Approved with amount exceeding coverage
⚡ **Runtime checked** — enforced on transitions entering `Approved`
Scope: entry gate (`to Approved`)
Referenced fields: `ApprovedAmount`, `CoverageLimit`
Violation rejects the transition
```

**Event args (`on`):**

```md
**ensure** `on Submit ensure Amount > '0 USD'`
> Submitted amount must be positive
⚡ **Runtime checked** — enforced when `Submit` fires
Scope: event args (`on Submit`)
Referenced args: `Amount`
Violation rejects the event
```

**Data sources (V1):** type checker (ensure expression type, references) · graph (active states) · proof (`ProofLedger.Obligations` by `EnsureIdentity`, `ConstraintInfluence`)  
**Note:** Use `ConstraintInfluenceEntry` for referenced fields/args — NOT `TypedEnsure.SemanticSubjects` (currently empty, Kramer N10).

---

## 8. `access` and `omit`

**`access` (editable):**

```md
**access** `in Listed modify RestockThreshold, Supplier, SupplierCurrency, ListPrice editable`
✅ **Proof verified** — write map is structural
Editable here: `RestockThreshold`, `Supplier`, `SupplierCurrency`, `ListPrice`
Same write set in `LowStock` · locked in `Unlisted`, `Delisted`
```

**`omit`:**

```md
**omit** `in Unlisted omit ListPrice`
✅ **Proof verified** — field is structurally absent in `Unlisted`
`ListPrice` does not exist in this state — not readable, not writable
Restored on transition to: `Listed`, `LowStock`
```

**Data sources (V1):** type checker (field existence, duplicate safety) · graph (`TypedAccessMode`, same-write-set/locked-state summaries, restore-transition scan) · manifest (omit declaration)  
**V1 limit:** guarded-access final write map not materialized — V1 shows raw declaration only.

---

## 9. `reject`

```md
**reject** `from Listed on FulfillOrder -> reject`
> Insufficient stock to fulfill this order
⚡ **Runtime checked** — deliberate business rejection
Result: state unchanged · no field mutations commit
```

**Data sources (V1):** manifest (`RejectReason`) · graph (row reachability)  
**V2 deferred:** "Selected when: no earlier guard matches" — requires ordered-row analysis (Kramer N6).

---

## 10. Qualifier (`of …`, `in …`)

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

**Data sources (V1):** `QualifierHoverInfo` (Axis, Span, Label, OwnerType, ResolvedQualifier) · type checker (declared/resolved qualifier, symbolic source) · proof (active use counts, unresolved uses, qualifier-preserving strategy)  
**Note:** Show resolved value, source, and open issues as **separate labeled lines** — never collapsed into prose. Do not force a `Proof verified` tone if unresolved uses exist.

---

## Proof Hover Scenarios

### Scenario 1: Qualified field

> **Trigger:** cursor on a field identifier with at least one declared qualifier

```md
**field** `TotalRevenue`
Type: `money`
Proof: Currency qualifier contract
  Verdict: Active — no open issues
  Declared qualifier: `in '{CatalogCurrency}'`
  Resolved value: `'{CatalogCurrency}'`
  Qualifier source: field `CatalogCurrency`
```

**Variant B — unresolved uses:**

```md
Proof: Currency qualifier contract
  Verdict: Active — 2 unresolved uses
  Declared qualifier: `in '{CatalogCurrency}'`
  Resolved value: `'{CatalogCurrency}'`
  Qualifier source: field `CatalogCurrency`
  Open issues: computed field `GrossProfit` · transition `from Draft on Approve`
```

**Variant C — proved:**

```md
Proof: Currency qualifier contract
  Verdict: Proved via qualifier-preserving operations
  Declared qualifier: `in '{CatalogCurrency}'`
  Resolved value: `'{CatalogCurrency}'`
  Qualifier source: field `CatalogCurrency`
```

> The field hover tells the user what contract this field carries. The diagnostic hover tells them where that contract failed to prove. Do not show failure tone on the declaration hover unless the declaration itself is the failing span. Do not collapse declared form, resolved value, and source into one sentence.

**Data needed:** `TypedField.Name`, `ResolvedType`, `DeclaredQualifiers`, `Qualifier`, `NameSpan` · resolved qualifier value + symbolic source · proof-usage summary (the missing join from declaration to open obligations).

---

### Scenario 2: Binary expression with proof obligations

> **Trigger:** cursor on operator or operand inside a `TypedBinaryOp` with at least one proof obligation. Target the **smallest proof-bearing binary expression** at the cursor — not the generic operator.

**Proved:**

```md
**expression** `(TotalRevenue - TotalReturns)`
Proof: Currency qualifier propagation
  Verdict: Proved — same-qualifier propagation
  Left: `TotalRevenue` · qualifier `'{CatalogCurrency}'`
  Right: `TotalReturns` · qualifier `'{CatalogCurrency}'`
  Result type: `money` · qualifier `'{CatalogCurrency}'`
```

**Unresolved:**

```md
**expression** `(TotalRevenue - TotalReturns) - TotalCostOfGoods`
Proof: Currency qualifier propagation
  Verdict: Cannot prove — left subexpression qualifier not proved at this site
  Left: `(TotalRevenue - TotalReturns)` · qualifier: not proved here
  Right: `TotalCostOfGoods` · qualifier `'{CatalogCurrency}'`
  Result type: `money` · qualifier: unresolved
  Fix: align qualifier sources or insert an explicit conversion / intermediate field
```

> This is the flagship proof card — where the user expects Precept to explain itself. Do not show generic operator help when a proof-bearing `TypedBinaryOp` is present. Do not show only `1 unresolved obligation` — the card must name the requirement and evidence.

**Data needed:** `TypedBinaryOp.Left/Right/ResultType/ResultQualifier/ProofRequirements/Span` · matching `ProofObligation` from `ProofLedger` · proof disposition and strategy · resolved left/right qualifier values at this site · expression context (`FieldExpressionContext(TypedField)`)  
**Note:** `ResultQualifier` describes the propagation rule, not the humanized resolved value — the hover needs a resolution step, not just the raw DU case.

---

### Scenario 3: Diagnostic squiggle

> **Trigger:** cursor within the span of a proof-stage diagnostic. **Diagnostic hover wins over all other hovers at this position.**

Problems panel gives one-line verdicts for scanning. Hover gives a mini proof card for diagnosis and repair. Never compress proof context into inline parentheses in hover.

**PRE0114 — Currency qualifier mismatch:**

```md
**PRE0114 — Cannot prove Currency qualifier compatibility**
Proof: Currency qualifier compatibility
  Verdict: Cannot prove — nested subtraction result not proved qualifier-preserving here
  Context: computed field `GrossProfit`
  Expression: `(TotalRevenue - TotalReturns) - TotalCostOfGoods`
  Left: `(TotalRevenue - TotalReturns)` · qualifier: unresolved
  Right: `TotalCostOfGoods` · qualifier `'{CatalogCurrency}'` from field `CatalogCurrency`
  Fix: align qualifier paths or insert an explicit conversion / intermediate field
```

**PRE0116 — Presence requirement:**

```md
**PRE0116 — Cannot prove presence**
Proof: Optional field access guard
  Verdict: Cannot prove `TrackingNumber` is present before this access
  Context: computed field `ShipmentSummary`
  Expression: `TrackingNumber.length`
  Subject: `TrackingNumber` · declared `optional`
  Fix: guard with `when TrackingNumber is set`, initialize on all paths, or remove `optional`
```

---

## Status Indicators

| Badge | Meaning |
|---|---|
| ✅ **Proof verified** | Statically established for the hovered construct |
| ⚡ **Runtime checked** | Enforced on every mutation before commit — not pre-proved |
| ⚠️ **Unverified** | A relevant proof or analysis obligation is still unresolved |

> "Runtime checked" describes enforcement at mutation time. `Inspect` is a non-mutating preview — it evaluates constraints but does not enforce. Badges describe enforcement semantics, not preview behavior.

---

## Routing Rules

| Priority | Target | Card returned |
|---|---|---|
| 1 | Proof diagnostic span | Diagnostic proof card |
| 2 | `TypedBinaryOp` with proof obligation | Expression proof card |
| 3 | Construct declaration span | Rich construct card |
| 4 | Field identifier with declared qualifier | Field symbol + proof block (combined) |
| 5 (fallback) | Any other position | Generic operator / function / type card |

**Routing notes:**
- Qualifier hover (`TryCreateQualifierHover`) remains the qualifier-syntax entry point, but the card content becomes proof-aware.
- Qualified-field hover stays **one combined card** — declaration identity and proof contract together.

---

## Constraints

### Rendering (VS Code)

Bold, blockquotes, code spans, and inline emoji render well. **Do not use tables, HTML, or deep nested lists** — hover width degrades them fast. All templates above use safe markdown patterns only.

### V1 Boundary (compile-time only)

`HoverHandler` receives only the current `Compilation` snapshot. Everything in V1 templates must be derivable from compile-time artifacts.

**Available in V1:**
- `SemanticIndex`: fields, states, events, rules, ensures, access modes, transition rows, qualifier bindings
- Type-check results: declared type, nullability, coercions, mutation assignment safety
- `StateGraph`: reachability, dead states, transitions, terminal reachability
- `ProofLedger` + diagnostics: per-construct status, `ConstraintInfluenceEntry`, unresolved obligations
- Manifest: modifiers, guard expressions, action sequences, reject reasons, omit declarations

**Not available in V1:** executable model or runtime descriptors · inspect/fire/update projections · ordered mutation summaries · qualifier-usage index · precomputed prose proof-gap text · guarded-access final write maps

### V2 Deferred

1. Event "typical effects" summary (Frank N6, Kramer N3)
2. Prose proof-gap text (Kramer N4)
3. Qualifier "applied to" line — requires usage index (Kramer N7)
4. Event-driven mutation reach on field hover (Kramer N1)
5. Reject row ordering context (Kramer N6)
6. Guarded-access final write maps (Kramer N5)
7. Runtime preview integration

---

## Implementation Notes (Kramer)

### Derived projections (V1)
- `ConstructAtPosition` — resolve hover target
- `FieldWriteMapByState` — via `AccessModes` traversal (medium cost)
- `ConstraintInfluenceSummary` — from `ConstraintInfluenceEntry` (medium cost)
- `HoverStatusBadge` — from `ProofLedger.Obligations`

### Helper APIs needed

```csharp
TryFindProofDiagnosticAtPosition(Compilation, Position, out Diagnostic, out ProofObligation?)
TryFindProofBearingExpressionAtPosition(Compilation, Position, out TypedExpression, out ImmutableArray<ProofObligation>)
ResolveQualifierEvidence(TypedExpression, QualifierAxis, Compilation) → (value, source, state)
GetProofUsesForField(TypedField, Compilation) → open/clean use summary
HumanizeProofStrategy(ProofStrategy)
```

> If the ledger cannot provide a truthful `Verdict` or failure explanation, the hover falls back to a precise generic explanation — never invents specificity.

### Data model availability

**From `Compilation`:**

| Need | Status |
|---|---|
| Token / position context | **Available** — `Compilation.Tokens` |
| Semantic declarations and expressions | **Available** — `Compilation.Semantics` |
| Proof ledger | **Available** — `Compilation.Proof` |
| Diagnostics at a span | **Available** — `RichHoverFactory.GetDiagnosticsOverlapping(...)` |
| Proof diagnostic at cursor → matching obligation | **Not exposed as helper** — inferred by overlapping span/code; dedicated helper safer `[George verify]` |

**From `SemanticIndex`:**

| Need | Status |
|---|---|
| Field declaration qualifiers | **Available** — `TypedField.DeclaredQualifiers` |
| Field qualifier behavior | **Available** — `TypedField.Qualifier` |
| Binary expression proof requirements | **Available** — `TypedBinaryOp.ProofRequirements` |
| Binary result qualifier propagation rule | **Available** — `TypedBinaryOp.ResultQualifier` (needs humanization) |
| Smallest expression at cursor | **Available** — `SemanticExpressionLocator.TryFindExpressionAt(...)` |
| Qualifier value resolution for expression/site | **Not public** — needs extracted resolver `[George verify]` |
| Qualifier usage index: declaration → use sites | **Not obvious** — may need language-server helper `[George verify]` |
| Symbolic source extraction for qualifier value | **Partial** — `RichHoverFactory.TryGetQualifierResolvedSource(...)` covers declaration cases; expression-site needs same |

**From `ProofLedger`:**

| Need | Status |
|---|---|
| Proof obligations with site + context | **Available** — `ProofLedger.Obligations` (`Requirement`, `Site`, `Context`, `Disposition`, `Strategy`, `EmittedDiagnostic`) |
| Fault-site links | **Available** — `ProofLedger.FaultSiteLinks` |
| Proof verdict | **Available** — `ProofDisposition` (`Proved` / `Unresolved`) |
| Proof strategy | **Available** — `ProofStrategy?` |
| Explicit unresolved reason text | **Not visible** — current model gives verdict not user-ready explanation `[George verify]` |
| Stable diagnostic ↔ obligation join | **Partially inferable** — joinable by span + code + site; confirm before implementation `[George verify]` |

---

## Open Questions

1. **Inline warnings:** when a construct has a diagnostic squiggle, repeat the proof gap inline in hover? Recommendation: yes — one short sentence.
2. **Status wording:** keep `Proof verified` / `Runtime checked` / `Unverified`? Recommendation: keep.
3. **V1 boundary:** markdown only, or reserve hooks for richer hover actions? Recommendation: ship markdown now, shape DTOs for follow-on.
4. **Demo mix:** show both ✅ and ⚠️ states in hover demos? Recommendation: yes — demonstrates honest status reporting.
5. **Proof strategy line on proved hovers** — `Proof strategy: same-qualifier propagation` is valuable but implementation-forward. Confirm tone with Shane.
6. **Qualifier hover shows open proof issues**, not just static semantics — turns a declaration-syntax hover operational. Confirm scope.
7. **Proof hover wins over generic operator docs** — right for proof as flagship surface, but a real routing tradeoff.
