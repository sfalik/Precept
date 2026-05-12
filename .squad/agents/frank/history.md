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
