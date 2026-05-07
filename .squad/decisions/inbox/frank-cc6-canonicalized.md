# CC#6 Canonicalization — FaultSiteAnnotation on Opcode

**By:** Frank
**Date:** 2026-05-06
**Status:** Complete — canonical docs updated

---

## What Changed

Propagated the locked CC#6 ruling (Option A — nullable `FaultSiteAnnotation?` on each opcode) into three canonical docs:

### `docs/compiler/proof-engine.md`
- Closed the `ProofObligation.Site` structural identity Open Question → Resolved (CC#6): builder matches by `TypedExpression` identity during Pass 4 compilation.
- Closed the `FaultSiteLink.Site` to `FaultSiteDescriptor` binding Open Question → Resolved (CC#6): builder stamps `FaultSiteAnnotation` directly on the emitted opcode. Added canonical annotation shape and structural elision model description.
- Updated Decision 2 and §12 summary: `FaultSiteDescriptor` → `FaultSiteAnnotation` as the artifact that crosses the compile-runtime boundary.

### `docs/runtime/precept-builder.md`
- Added `FaultSiteAnnotation` type definition and planting contract to Pass 4 (expression compilation) — the canonical site where annotation stamping occurs.
- Added `FaultSiteAnnotation?` to the `Opcode` base record.
- Updated Pass 6 to clarify its residual role: `Precept.FaultBackstops` is a derived/tooling artifact, not the execution-path contract. `FaultSiteDescriptor` remains as the materialized tooling shape.
- Updated pipeline overview diagram, pass dependency table, and invariants.

### `docs/runtime/evaluator.md`
- Updated §7.3 dispatch loop with inline `FaultSiteAnnotation` check after each opcode dispatch.
- Added two-layer defense note: compile gate (first line) + runtime backstop (second line for force-builds/catalog evolution).
- Updated fault backstop routing description, resolved design question #10, and integration table.

## Cross-references added
- proof-engine.md → precept-builder.md §Pass 4 (planting contract)
- proof-engine.md → evaluator.md §7.3 (consumption contract)
- precept-builder.md → proof-engine.md §2 (output shape)
- precept-builder.md → evaluator.md §7.3 (consumption contract)
- evaluator.md → proof-engine.md §2 (structural elision model)
- evaluator.md → precept-builder.md §Pass 4 (planting contract)
