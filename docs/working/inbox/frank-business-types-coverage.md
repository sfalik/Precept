### 2026-05-04T17:00:09Z: Business value type coverage narrowed: Price stays semantic-only; ExchangeRate, Percentage, and DateRange advance as candidates

**By:** Frank

**Status:** Recommendation recorded from inbox; Shane decision still needed on OQ-7a through OQ-7f.

**Merged source:** `frank-business-types-coverage.md`.

- `Price` is not a new first-class type; it remains a role name on `MoneyValue` fields rather than a distinct structural/runtime type.
- `ExchangeRate` is recommended as a new first-class value type with `(BaseCurrency, QuoteCurrency, Rate)` shape and a positive-rate invariant lean.
- `Percentage` and `DateRange` are recommended as additional first-class candidates because they introduce invariant-bearing, runtime-significant semantics that bare decimals and paired dates cannot express safely.
- The investigation scope should expand from unit types to the broader Precept value-type surface; six open questions remain for Shane on built-in status, invariants, interval semantics, and a future `DateTimeRange` companion.

---

---
### 2026-05-04T05:45:56Z: Audit-gap P2 clarifications recorded; compiler/runtime innovation callouts confirmed clean

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-p2-doc-fixes.md`.

- `docs/runtime/evaluator.md` §4 now records both the `TypeBuilder` rejection rationale and the stable compiled-path upgrade seam against the existing A+G execution contract.
- `docs/runtime/evaluator.md` §7.3 now clarifies that per-type `TypeRuntimeMeta.BinaryExecutors` / `UnaryExecutors` are registered into flat `Operations` arrays, preserving zero-knowledge O(1) dispatch inside the evaluator.
- `docs/compiler-and-runtime-design.md` required no edit for Item 14; all `Precept Innovations` callouts already match the single-interpreter, catalog-dispatch architecture.
