## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.
- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation remain philosophically out of bounds.
- Proof and qualifier fixes should stay bounded to catalog metadata and conservative symbolic reasoning rather than speculative provenance systems.

## Live Guidance

- String holes remain out of scope for interpolated typed constants; typed-hole composition is the only acceptable path.
- Slice 6 stays numeric-only, including the single-hole whole-value fallback; qualifier, dimension, modifier, and presence obligations remain declaration-driven.
- Temporal semantics stay with `duration` / `period`; `quantity of 'time'` remains invalid, while temporal-denominated prices stay on `price of ...`.
- MCP/public tooling contracts should continue to expose curated projections rather than raw core catalog records.

## Historical Summary

- 2026-05-11 work established the current research baseline: inventory-item root-cause triage, Part D pre-existing test-failure design, Slice 2B completion audit, and the durable MCP projection boundary. The decision ledger and any existing history archives remain the source of full chronology.
- Early 2026-05-12 work locked the temporal qualifier guidance, reconciled the proof-plan status, and appended the Newman-ready DTO-free MCP execution plan plus context-window mitigation notes.

## Recent Updates

### 2026-05-12T03:07:25.498-04:00 — Post-recovery inventory-item verdict corrected
- inventory-item is not gated on missing language features; the parser and compound-unit support already shipped.
- Remaining state is **21 diagnostics**: **16 compiler** (exchange-rate / symbolic qualifier equality) and **5 sample** (division-by-zero guards and margin-expression design).
- Recommended next sequence: fix the stale inventory-item header, design symbolic equality for interpolated qualifiers, then revisit the sample-only diagnostics.
- Validation snapshot: **5471/5471 tests passing** with a clean working tree.

### 2026-05-12T03:33:33Z — Scalar-op qualifier propagation design locked as D4
- Kept the work in Part D, not Part C: scalar `money|quantity|price ×|÷ decimal` propagation fixes syntax-reference/test fallout but does not move inventory-item.
- Locked the metadata-driven approach: `ResultQualifierPolicy.InheritFromQualifiedOperand` → `QualifiedOperandInherited` binding → transitive `ResolveQualifierOnAxis` handling for `TypedBinaryOp` subjects.
- Durable side effect: nested same-qualifier binary expressions can now resolve qualifiers transitively instead of dying at the inner expression boundary.

### 2026-05-12T08:40:00-04:00 — P2/P3/P3b code review complete

- P2 (SourceFieldName symbolic equality): Approved. Clean two-tier design — SourceFieldName at type-check time, ExtractQualifierSourcePath fallback for legacy. ExtractSourceFieldName correctly handles leading empty TextSegment from parser. Cross-subtype comparison (ToCurrency ↔ Currency) is the F4 critical path and is tested.
- P3 (PriceDivideQuantity): Approved. New OperationKind=203, ResultQualifierPolicy.CompoundDimensionElevation, CompoundDimensionElevationRequired binding all follow catalog-driven architecture. TryDeriveCompoundElevationQualifiers in TypedConstants and TryResolveCompoundElevationDimension in ProofEngine are correctly asymmetric (right operand only — compound-quantity is always the divisor).
- P3b (CompoundUnitCancellation Dimension form): Approved. The Dimension fallback in TryGetCompoundUnit was shipped inside the P3 commit, not separately. P3b is test-only — 3 tests validating the `of '{dim}'` form. ExtractCompoundValue in ProofEngine already handles Dimension. Symmetric in both operand orders.
- Full suite: 5496/5496 tests pass (4913 core + 264 LS + 39 MCP + 280 analyzers). No regressions.

### 2026-05-12T13:02:45Z — inventory-item qualifier edit queued for sign-off
- George removed the stale RC1 / RC2 inventory-item header comments after the compiler support landed.
- The remaining BUG-A work is the sample-side `Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}'` edit, which is waiting on Frank's approval before it is applied.
