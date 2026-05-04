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
- Naming a struct size is not enough — the struct layout (FieldOffset, tag type, union field shape) must be explicitly documented or flagged as an open design question. A 32-byte size target without a layout spec leaves the implementation team with no design anchor.
- Hot-path memory pictures (slot counts, byte totals, boundary allocation size) belong in the evaluator doc as a quantified baseline, not just in the squad ledger.
- Any time an abstract class gets new abstract members (BinaryExecutors, UnaryExecutors), the registration mechanism and assembly strategy must be co-documented — else the interface is defined but unimplementable.
- Stale `object?[]` code in evaluator.md is a canary for PreceptValue baseline drift. If a code sample uses `object?` in the evaluation loop, it predates the CC#25 runtime decision and must be annotated or replaced.

## Recent Activity

### 2026-05-04T04:36:09Z — Scribe closeout: deep content audit recorded

- Scribe merged `frank-deep-content-audit.md` into `decisions.md`, cleared the decisions inbox, and recorded the batch in the orchestration/session logs.
- Durable runtime-doc baseline now explicitly covers the `PreceptValue` performance rationale and memory picture, the 7-step Fire lifecycle, slot-index `LOAD_ARG`, `TypeRuntime` executor arrays, and the `bool[]` arg presence mask semantics.
- The archive hard gate was checked at closeout; `decisions.md` was over the size threshold, but there were no active entries older than 30 days to move.

### 2026-05-04T00:36:09Z — Deep content audit: decisions.md → canonical docs

Shane requested an urgent specificity audit — not a coverage check. The question was not "is PreceptValue mentioned?" but "does the canonical doc describe the struct layout, the tag bits, the union fields, the memory picture, the WHY?"

**Audit found 6 real gaps:**
1. **PreceptValue struct layout missing** — evaluator.md named the 32-byte size but had zero layout detail: no FieldOffset, no PreceptValueTag, no union field description. Added the `### PreceptValue: Evaluation Currency` subsection to evaluator.md §5 with the GC-pressure rationale (~768 MB/s gen-0 at 100k ev/sec), the performance motivation, and the precise struct layout flagged as an open design question. The specific FieldOffset layout was NOT decided in decisions.md; documenting it as a pending decision is the correct action — not inventing a layout.
2. **Hot-path memory picture missing** — ~44–48 PreceptValue slots, ~4,480 bytes stack traffic per Fire, ~88 bytes boundary objects — none of it was in evaluator.md. Added as a table in the PreceptValue subsection.
3. **Full Fire lifecycle missing Load-args step** — Working Copy Management section was 5 steps, missing "Load args from pre-materialized FiredArgs.ArgSlots" and "Recompute computed fields." Rewrote to 7-step lifecycle.
4. **LOAD_ARG opcode stale as name-based** — opcode table described LOAD_ARG as a name lookup. Decision says it resolves to a slot index at build time. Updated; added open design question about ArgDescriptor.SlotIndex.
5. **BinaryExecutors/UnaryExecutors missing from TypeRuntime** — decisions.md locked these as part of the active TypeRuntime surface; catalog-system.md's abstract class had none of them. Added both to TypeRuntime with the catalog-delegate dispatch explanation and an open design question on the registration mechanism.
6. **Presence mask undefined** — runtime-api.md mentioned "presence mask" but never defined it. Added a concrete definition (bool[] same length as arg slot array, true = set, false = absent).

**Three things that were already correct:**
- IReadOnlyDictionary lane obsolescence: runtime-api.md was correct.
- JSON-only Restore: runtime-api.md was correct.
- SlotLayout and construct/field-slot vocabulary: precept-builder.md was correct.

**Stale code fixed:** evaluator.md §7.3 had a pre-CC#25 `object?`-based EvaluatePlan implementation that contradicted the PreceptValue baseline in §7.0. Replaced with the canonical PreceptValue implementation showing catalog-delegate dispatch. §7.1 pseudocode annotated as using `object?[]` for readability.

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
