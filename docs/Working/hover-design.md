# Precept Hover Design

**By:** Elaine · **V7** · **2026-05-12T18:25:28.876-04:00** · VS Code markdown hover

---

## 1. Overview

Hover answers one question first: **what guarantee is Precept giving me here?** Every card leads with one of three guarantee states — ✅ Proven, ⚡ Enforced, or ⚠️ Gap — then gives one concrete evidence line when proof is the issue. Compactness is a correctness rule: design for 3 lines, spend lines 4–5 only when proof evidence is the user's actual question.

---

## 2. Badge vocabulary

| Icon | Meaning |
|---|---|
| ✅ | **Proven** — established statically before runtime |
| ⚡ | **Enforced** — checked on mutation before commit |
| ⚠️ | **Gap** — not proven; the next line says why |
| 🔒 | Not mutable / structurally absent here |
| ✏️ | Mutable here |
| 🔁 | Transition / routing / from→to shape |
| ⚖️ | Currency, unit, or comparison contract |
| 📍 | Graph position — reachable, dead, terminal, required |
| 🔬 | Calculation / proof check / expression reasoning |

> Icons replace words. They are scan primitives, not decoration.

---

## 3. Card templates

Examples below are grounded in `inventory-item.precept`, `loan-application.precept`, `computed-tax-net.precept`, and `sum-on-rhs-rule.precept`.

### `field` (stored)

```md
⚡ Enforced · `price` · ⚖️ `CatalogCurrency` / `SaleUnit`
✏️ `Listed`, `LowStock` (unconditional) · 🔒 `Unlisted`, `Delisted`
Governed by: 2 rules · 1 ensure
```

**Proof variant:**

```md
⚠️ Gap · `CatalogCurrency` is on this field, 1 use not proven
🔬 Use: `GrossProfit` · `(TotalRevenue - TotalReturns) - TotalCostOfGoods`
Left `(TotalRevenue - TotalReturns)` has no known `CatalogCurrency` · right `TotalCostOfGoods` carries `CatalogCurrency`
```

**Data sources:** type checker (`ResolvedType`, `Presence`, `DeclaredQualifiers`) · access map (`AccessModes` traversal, unconditional entries only in V1 summaries) · proof (`ConstraintInfluenceEntry`, overlapping diagnostics)

---

### `field` (computed)

```md
⚡ Enforced · recomputed before commit
`price` · ⚖️ `CatalogCurrency` / `StockingUnit`
From: `TotalInventoryCost / QuantityOnHand` · Governed by 1 rule
```

**Proof variant:**

```md
✅ Proven · derived calculation stays safe
🔬 `Total - Tax - Fee` proves `Net` stays positive
```

**Data sources:** type checker (`ResolvedType`, `ComputedExpression`) · proof (`ProofLedger.Obligations`, `ConstraintInfluenceEntry`)

---

### `state`

```md
✅ Proven · reachable from `Listed`
🔁 In: `FulfillOrder`, `RecordShrinkage` · Out: `ReceiveShipment → Listed`
✏️ 4 fields (unconditional) · 🧭 terminal ✓ · ⚡ 3 ensures (1 ⚠️)
```

**Proof variant:**

```md
⚠️ Gap · `Approved` unreachable from `Draft`
🧭 `Draft` reaches `UnderReview`
Missing path: `UnderReview --Approve--> Approved` can't be proven
```

**Data sources:** graph (`ReachableStates`, `Edges`, terminal reachability) · access map (unconditional entries only in V1 summaries) · ensures + proof obligations · modifiers

---

### B4 — state proof narrative (**locked**)

**Status:** Locked · shipped in `29cd9938`

B4 ships as an appended sub-card inside the rich `state` hover. It does **not** replace the standard state card; it adds a graph-position proof narrative at the bottom of that card.

**As built templates:**

```md
📍 Draft graph position

⚠️ Gap · Draft --Submit--> Approved can't be proven
```

```md
📍 Draft graph position

✅ Proven · all connected edges satisfy their proof obligations
```

```md
📍 Draft graph position

✅ Proven · no connected edges carry proof obligations
```

- `📍` is the fixed header line for the graph-position block.
- If any connected edge is unproven, the block emits one `⚠️ Gap · From --Event--> To can't be proven` line per failing edge.
- If no connected edge is unproven, the block collapses to one positive `✅ Proven` line.
- B4 uses the locked proof vocabulary only: `📍`, `✅ Proven`, and `⚠️ Gap`.

**`EdgeProofStatus` architecture:**

- Lives in `src/Precept/Pipeline/StateGraph.cs` as a graph-level projection record on `StateGraph.EdgeProofStatuses`.
- Each `EdgeProofStatus` carries `FromState`, `EventName`, `ToState`, `HasObligations`, `IsProven`, and `ImmutableArray<string> UnresolvedObligationSummaries`.
- `src/Precept/Compiler.cs` populates it in `EnrichGraphWithProofStatus(...)` **after** `GraphAnalyzer.Analyze(...)` and `ProofEngine.Prove(...)`.
- Population rule: match `ProofLedger` obligations whose context is `TransitionRowContext` onto concrete `GraphEdge` instances, respect explicit-row-over-wildcard precedence, set `HasObligations` when any obligation matched the edge, de-duplicate unresolved `Requirement.Description`, then mark the edge proven when no unresolved summaries remain.
- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` consumes the projection via `GetEdgeProofStatusesForState(...)`, which filters to incident edges (`from` or `to` the hovered state) and de-duplicates by `(FromState, EventName, ToState)`.
- Current render behavior uses the edge identity plus `HasObligations`/`IsProven` for the visible card. `UnresolvedObligationSummaries` are carried in the architecture but are not yet printed in the hover body.

**Routing rule:**

- A state hover reaches B4 only through the normal rich state-hover path: `HoverHandler` resolves a `TypedState` and calls `RichHoverFactory.CreateStateHover(...)`.
- That happens from both state identifier lookup (`TryFindState(...)`) and symbol-occurrence lookup (`StateOccurrence`).
- B4 is therefore part of the standard state card, not a separate hover kind.
- Global proof-first routing still applies above it: proof diagnostics and proof-expression hovers win earlier when the cursor is on those spans instead of the state symbol.

**Representative rendered output (from LS regression coverage):**

```md
📍 Draft graph position

⚠️ Gap · Draft --Submit--> Approved can't be proven
```

`HoverHandlerTests` also locks the positive variant:

```md
📍 Draft graph position

✅ Proven · all connected edges satisfy their proof obligations
```

---

### `event`

```md
⚡ Enforced · args checked before route
🔁 Fires from: `Listed`, `LowStock`
Args: `Price as price in CatalogCurrency of SaleUnit`
```

**Initial-event variant:**

```md
⚡ Enforced · constructor event
Args: `Applicant`, `Amount`, `Score`, `Income`, `Debt`
```

**Data sources:** type checker (`TypedEvent.Args`) · graph (`HandledInStates`) · event ensure proof status · modifiers

---

### Transition row (`from … on … when … ->`)

```md
✅ Proven · currencies match
🔁 `LowStock` → `Listed` on `ReceiveShipment`
Guard: `QuantityOnHand + ReceiveShipment.PurchaseQty *`
`StockingUnitsPerPurchaseUnit > RestockThreshold`
```

**Proof variant:**

```md
⚠️ Gap · `LowStock` → `Listed` on `ReceiveShipment`
Guard: `PostShipmentStock > RestockThreshold`
🔬 Can't confirm `PostShipmentStock` carries `StockingUnit` from `ReceiveShipment.PurchaseQty * StockingUnitsPerPurchaseUnit` · `RestockThreshold` carries `StockingUnit`
```

Long `Guard:` expressions wrap to the next line in V1; they do not truncate.

**Data sources:** transition row span + manifest (guard, outcome) · graph reachability · proof obligations + overlapping proof diagnostics

---

### `rule`

```md
⚡ Enforced after every mutation
> Stock on hand cannot go negative
Fields: `QuantityOnHand`
```

**Guarded variant:**

```md
⚡ Enforced when `DocumentsVerified`
> Debt cannot exceed 3× income while verified
Fields: `ExistingDebt`, `AnnualIncome`
```

**Proof variant:**

```md
⚠️ `PRE0114` · Gap · `ListPrice / StockingUnitsPerSaleUnit >= AverageCost`
⚖️ Fields: `ListPrice`, `StockingUnitsPerSaleUnit`, `AverageCost`
`ListPrice / StockingUnitsPerSaleUnit` has no known `CatalogCurrency/{StockingUnit}` · `AverageCost` carries `CatalogCurrency/{StockingUnit}`
```

**Data sources:** type checker (guard + condition spans) · proof (`ConstraintInfluenceEntry`, `ProofLedger.Obligations`) · `because` text

---

### `ensure`

```md
⚡ Entry gate · `Approved`
> Cannot enter Approved with amount exceeding coverage
Fields: `ApprovedAmount`, `CoverageLimit`
```

**Anchor variants:**
- `in` → `⚡ Residency · <State>`
- `to` → `⚡ Entry gate · <State>`
- `from` → `⚡ Exit gate · <State>`
- `on` → `⚡ Arg gate · <Event>`

**Proof variant:**

```md
⚠️ `PRE0114` · Gap · `to Approved` checks `ApprovedAmount`, `CoverageLimit`
⚖️ `ApprovedAmount` carries `CatalogCurrency`
`CoverageLimit` carries `SupplierCurrency` · different currencies — can't compare
```

**Data sources:** ensure kind/anchor · `because` text · proof (`ConstraintInfluenceEntry`, `ProofLedger.Obligations`)

---

### `access`

```md
✅ Proven · write access declared in manifest
✏️ `RestockThreshold`, `Supplier`, `SupplierCurrency`, `ListPrice`
Also in: `LowStock` · 🔒 `Unlisted`, `Delisted`
```

**Data sources:** `AccessModes` + state set

---

### `omit`

```md
✅ Proven · structurally absent in `Unlisted`
🔒 `ListPrice` does not exist here
🔁 Restored on: `Listed`, `LowStock`
```

**Data sources:** manifest omit declarations · graph edges

---

### `reject`

```md
⚡ Enforced · event rejected
> Insufficient stock to fulfill this order
State unchanged · no changes apply
```

**Data sources:** transition row outcome + reject reason

---

### Qualifier (`of …`, `in …`, `to …`)

```md
⚖️ Currency · `CatalogCurrency`
Mixed currencies or units aren't allowed
```

**Proof variant:**

```md
⚠️ Gap · currency is `CatalogCurrency`
⚖️ Use: `GrossProfit`
Left side of `(TotalRevenue - TotalReturns) - TotalCostOfGoods` can't be confirmed to carry `CatalogCurrency`
```

**Data sources:** `QualifierHoverInfo` · resolved qualifier + source · overlapping proof diagnostic can name the failing use in V1; declaration→use counts are deferred to V2 when a declaration→use projection exists

---

### Proof expression (`TypedBinaryOp`)

```md
✅ Proven · result keeps `CatalogCurrency`
🔬 `TotalRevenue - TotalReturns`
Left/Right: `CatalogCurrency` · Result: `CatalogCurrency`
```

**Proof variant:**

```md
⚠️ Gap · currency not proven
🔬 `(TotalRevenue - TotalReturns) - TotalCostOfGoods`
Left `(TotalRevenue - TotalReturns)` has no known `CatalogCurrency` · right `TotalCostOfGoods` carries `CatalogCurrency`
```

**Data sources:** `TypedBinaryOp` · `ProofLedger.Obligations` · resolved left/right/result qualifier evidence

---

### Diagnostic squiggle

```md
⚠️ `PRE0114` · Can't confirm currencies match
🔬 `GrossProfit` · `(TotalRevenue - TotalReturns) - TotalCostOfGoods`
Left `(TotalRevenue - TotalReturns)` has no known `CatalogCurrency` · right `TotalCostOfGoods` carries `CatalogCurrency`
```

**Presence variant:**

```md
⚠️ `PRE0116` · Can't confirm presence
🔬 `ShipmentSummary` · `TrackingNumber.length`
`TrackingNumber` is optional · `.length` accessed without presence guard
```

**Data sources:** proof-stage diagnostic + matching obligation + proof subject resolution

---

## 4. Routing rules

1. Proof diagnostic span wins.
2. Smallest proof-bearing `TypedBinaryOp` wins next.
3. Then construct cards on declaration spans.
4. Within construct cards: `reject` beats generic transition; qualifier beats symbol hover on the qualifier span.
5. State symbols route to the rich state card, and B4 renders inside that card rather than as a separate hover kind.
6. Otherwise fall back to generic operator / function / type help.

---

## 5. V1 boundary

**Available in V1:**
- `SemanticIndex`: fields, states, events, rules, ensures, access modes, transition rows, qualifier bindings
- type summaries: declared type, presence, qualifiers, computed-expression spans, argument signatures
- `StateGraph`: reachability, incoming/outgoing edges, handled states, terminal reachability, and `EdgeProofStatuses`
- `ProofLedger` + diagnostics: unresolved obligations, requirement kind, context, `ConstraintInfluenceEntry`
- manifest snippets: guard text, action order, reject reasons, omit/access declarations
- B4 state proof narrative: connected-edge proof verdicts projected from proof into graph data, then rendered on state hover

**Not available in V1:**
- runtime `Inspect` / `Fire` / `Update` facts
- ordered mutation summaries beyond the current row
- declaration → use qualifier index
- qualifier/use counts on qualifier cards (suppressed until the declaration → use projection exists)
- authored one-line proof explanations beyond what the proof model already exposes
- final guarded-access maps (guarded entries are omitted from V1 mutability summaries)
- per-edge rendering of `UnresolvedObligationSummaries` inside the B4 card (the projection exists; V1 shows verdict lines only)

> V7 assumes compact helper projections, not new runtime surfaces.

---

## 6. Status

All design questions resolved — V7 is synced to shipped behavior, and B4 is now locked as-built in `29cd9938`.
