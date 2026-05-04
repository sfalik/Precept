## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Durable baseline: catalogs remain the language truth; consumers derive behavior from metadata/shape instead of construct identity.
- Active runtime baseline: production execution uses `PreceptValue` tagged values, slot arrays, eager execution plans, and catalog-owned dispatch; public execution remains sync-only.
- Typed ingress/egress baseline: `Version.Get<T>` / `FiredArgs.Get<T>` are the primary typed accessors; JSON restore stays the only hydration lane; typed builders (`IArgBuilder`, `IFieldBuilder`) flow through catalog-owned runtime metadata.
- `TypeMeta.Runtime` is the catalog-owned home for runtime conversion/serialization behavior; any lookup tables must be derived from catalog metadata rather than maintained in parallel.
- Collections stay BCL-first with persistent semantics preserved for working-copy discard.
- Canonical docs state what the system is. Decision provenance, CC# numbers, and working-status annotations belong in the squad ledger / working docs rather than canonical documentation.

## Learnings

- When a design question locks, immediately replace provisional wording in canonical docs with factual architecture statements and move provenance to squad records.
- Audit `runtime-api.md`, `result-types.md`, and `evaluator.md` together after runtime-surface changes; they drift independently.
- Cross-cutting decision status must be reflected in both the ledger and the first doc new contributors check, or stale pending markers will survive after the decision is already locked.
- Gap-register migrations should preserve open questions in the canonical docs, not create a second source of truth.

## Recent Activity

### 2026-05-04T04:36:09Z — Citation cleanup closeout
- Frank-68 removed CC# citation artifacts from 8 canonical docs: `evaluator.md`, `precept-builder.md`, `runtime-api.md`, `catalog-system.md`, `compiler-and-runtime-design.md`, `proof-engine.md`, `type-checker.md`, and `parser.md`.
- `docs/working/` stayed untouched; working-record navigation links remained where they still serve discovery.
- Durable takeaway: canonical docs should never carry `(CC#...)`, `Resolved (CC#...)`, or similar provenance wrappers.

### 2026-05-04T04:19:13Z — Chunk-4 migration and full decisions audit
- Frank-66 migrated or triaged all 43 catalog-gap-register entries, archived the working register, and pushed pending questions back into canonical docs with explicit routing for cross-cutting and out-of-scope items.
- Frank-67 audited the CC#25 / CC#2 decision set across canonical docs, fixed lagging coverage in `evaluator.md`, `result-types.md`, `precept-builder.md`, `compiler-and-runtime-design.md`, and `cross-cutting-decisions.md`, and left only the `TypeRuntime<T>` reconciliation and non-expression `SlotValue` conflicts open.

### 2026-05-04T03:45:15Z — Runtime API doc sync baseline
- `runtime-api.md` now reflects the two-lane ingress model, `PreceptValue`-based access surfaces, typed builder interfaces, and JSON-only restore semantics.
- The squad baseline is that dictionary-based convenience ingress is out of scope.
