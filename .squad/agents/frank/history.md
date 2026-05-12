## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.
- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation remain philosophically out of bounds.
- Proof and qualifier fixes should stay bounded to catalog metadata and one-hop semantic traces rather than speculative provenance systems.

## Live Guidance

- String holes remain out of scope for interpolated typed constants; typed-hole composition is the only acceptable path.
- Slice 6 stays numeric-only, including the single-hole whole-value fallback; qualifier, dimension, modifier, and presence obligations remain declaration-driven.
- Temporal semantics stay with `duration` / `period`; `quantity of 'time'` remains invalid, while temporal-denominated prices stay on `price of ...`.
- MCP/public tooling contracts should continue to expose curated projections rather than raw core catalog records.

## Learnings

### 2026-05-11T21:05:25-04:00 — inventory-item.precept coverage analysis
- Compiled `samples/inventory-item.precept` via `precept_compile`: 125 diagnostics, ~73% are BUG-C or direct cascades from BUG-C (failed interpolation → failed arg parsing → cascade "not declared" errors).
- **Plan gap found:** compound-unit interpolation (`'{StockingUnit}/{PurchaseUnit}'`) is not covered by any per-type grammar in the plan. The `unitofmeasure` type only defines `U1: H[whole-value]`; quantity Q1–Q4 have no compound-unit patterns. This blocks 4 field declarations and 2 rules.
- BUG-B is indirectly covered by Part A + Slice 9 axis fallback (Unit→Dimension). No separate slice needed.
- BUG-A cannot be distinguished from BUG-C cascades in this file — all event args use interpolated qualifiers. Once BUG-C ships, Slice 10 (assignment expression qualifier propagation) should handle the remaining scenarios, but explicit test coverage for args-in-expressions is recommended.
- Sample file has its own design issues: `SupplierUnitCost` is declared `money` but used as `price` (no `MoneyTimesQuantity` operation exists); `Sku is set` on non-optional field; division by zero on `TotalInventoryCost / QuantityOnHand`.
- Compound-unit interpolation needs a new slice (Slice 2B or Slice 2 extension) with patterns for `unitofmeasure` and `quantity` compound forms.

## Recent Updates

### 2026-05-12T00:50:06Z — Q2 derivation inference ruling recorded
- D19 is now locked in `docs/language/business-domain-types.md`: derivation operations do not infer qualifiers onto resulting `price` values.
- Authors must declare `of 'time'` / `of 'date'` explicitly on target price fields when temporal denomination matters.

### 2026-05-12T00:50:06Z — Temporal proof-plan audit reconciled
- Canonical-doc review confirmed Slice 11B/12 stays additive to locked docs and that G15 is a false gap.
- Durable status correction: Slices 7–11 are already implemented; only Slice 11B and Slice 12 remain open.

### 2026-05-11T20:25:57Z — MCP projection contract direction preserved
- Raw catalog serialization stays off the public MCP contract path; the curated projection layer remains the durable direction.

### 2026-05-11T20:03:33Z — Slice 6 boundary held
- Slice 6 remains numeric-only, with the single-hole whole-value fallback included and compile-time guarantees preserved.

## Historical Summary

- Detailed 2026-05-11 research chronology, proof-plan audits, interpolation follow-ons, and temporal-design analysis were compacted into `history-archive.md` during the 2026-05-12T00:50:06Z summarization pass.
