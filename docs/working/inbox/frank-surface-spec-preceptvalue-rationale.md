### 2026-05-05T04:45Z: `PreceptValue` Axiom 1 rationale added to surface spec

**By:** Frank

**Status:** Applied.

**Merged source:** `frank-surface-spec-preceptvalue-rationale.md`.

- Added "Why Axiom 1 is non-negotiable" rationale block to `docs/working/runtime-api-public-surface-spec.md`, immediately after the axioms block and before §1.
- Four reasons sourced from collection types investigation §3: (1) Brittleness — evaluator-internal types have different stability requirements than public surface; (2) AI agent hostility — opaque internal types degrade agent accuracy; (3) Contract — generic type parameters are the hardest leakage vector; (4) Dual-shape model — collections are the vectorized case of the same internal/external shape rule.
- Axiom 1 governs every collection return type decision and any future surface extension; the rationale must be inline so future engineers encounter it before deciding to violate it.
