## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Durable baseline: catalogs remain the language truth; consumers derive behavior from metadata/shape instead of construct identity.
- Active runtime baseline: production execution uses `PreceptValue` tagged values, slot arrays, eager execution plans, and catalog-owned dispatch; public execution remains sync-only.
- Typed ingress/egress baseline: `Version.Get<T>` / `FiredArgs.Get<T>` are the primary typed accessors; JSON restore stays the only hydration lane; typed builders (`IArgBuilder`, `IFieldBuilder`) flow through catalog-owned runtime metadata.
- `TypeMeta.Runtime` is the catalog-owned home for runtime conversion/serialization behavior; any lookup tables must be derived from catalog metadata rather than maintained in parallel.
- Collections stay BCL-first with persistent semantics preserved for working-copy discard.
- Canonical docs state what the system is. Decision provenance, CC# numbers, and working-status annotations belong in the squad ledger / working docs rather than canonical documentation.

## Learnings

- `TypeBuilder` (`System.Reflection.Emit`) was evaluated and rejected as a dispatch codegen approach: NativeAOT incompatibility, catalog-owned delegates achieve identical O(1) dispatch, and TypeBuilder output is opaque vs. the catalog's startup-inspectable wiring. When documenting dispatch design, always record the rejection rationale alongside the chosen pattern.
- The `Operations` / `TypeRuntimeMeta.BinaryExecutors` relationship must be documented together: `Operations` is a flat startup registry aggregated from per-type `TypeRuntimeMeta` registrations; the evaluator is zero-knowledge about which type owns any given delegate. Documenting only the call site (`Operations.BinaryExecutors[(int)kind]`) without explaining where the executors come from leaves the registration model invisible.
- Precept's A+G interpreter is not a dead-end — the pre-indexed dispatch model (`DispatchIndex`, `SlotLayout`, `ConstraintPlanIndex`, `ExecutionPlan`) is an explicit upgrade seam: a future compiled path would consume the same `Precept` model without touching evaluator input types. Document this seam or it looks like a permanent ceiling.
- When auditing a doc for stale "Precept Innovations" claims: tree-walk mentions are not automatically stale — check whether the claim is "Precept uses tree-walk" (stale) vs. "Precept chose flat plans over tree-walk" (accurate). All 13 callouts in `compiler-and-runtime-design.md` were the latter.

- `ConstraintViolation.FailingValue` is `PreceptValue?` not `object?` — the evaluator works entirely in `PreceptValue`; callers use `TypeRuntime<T>.ToClr` to convert if needed. Any doc that uses `object?` here predates the CC#25 baseline and must be updated.
- When a shape gets promoted from a stale minimal form to a canonical design, update the Provisional annotation to record the ruling date and drop obsolete field names — don't just add new fields alongside the old ones.
- Item 13 resolved: parser output = ConstructManifest (Shane ruling 2026-05-04). Diagram was already correct. Artifact list updated to match. Source code rename deferred.
- Wrote `docs/working/decisions-summary.md` as a navigational reference over `.squad/decisions.md`.Structure: seven thematic groups (Runtime & Evaluation / CC#25, Public API Surface, Catalog System, Compiler Pipeline, Language Surface, Documentation, Tooling & Infrastructure, Process & Documentation Standards). Each entry is a 1–3 sentence capsule: what was decided, why, and any explicit rejections. File is ~220 lines and covers all active and foundation decisions.
- When a design question locks, immediately replace provisional wording in canonical docs with factual architecture statements and move provenance to squad records.
- Audit `runtime-api.md`, `result-types.md`, and `evaluator.md` together after runtime-surface changes; they drift independently.
- Cross-cutting decision status must be reflected in both the ledger and the first doc new contributors check, or stale pending markers will survive after the decision is already locked.
- Gap-register migrations should preserve open questions in the canonical docs, not create a second source of truth.
- Naming a struct size is not enough — the struct layout (FieldOffset, tag type, union field shape) must be explicitly documented or flagged as an open design question. A 32-byte size target without a layout spec leaves the implementation team with no design anchor.
- Hot-path memory pictures (slot counts, byte totals, boundary allocation size) belong in the evaluator doc as a quantified baseline, not just in the squad ledger.
- Any time an abstract class gets new abstract members (BinaryExecutors, UnaryExecutors), the registration mechanism and assembly strategy must be co-documented — else the interface is defined but unimplementable.
- Stale `object?[]` code in evaluator.md is a canary for PreceptValue baseline drift. If a code sample uses `object?` in the evaluation loop, it predates the CC#25 runtime decision and must be annotated or replaced.
- When pseudocode is fixed to use correct internal types, fix the helper signatures called within the same pseudocode block too (e.g., `EvaluateFireConstraints` / `EvaluateConstraint` must use `PreceptValue[]` / `FiredArgs` to stay type-consistent with the callers). Partial fixes leave type-mismatched call sites that are just as misleading as the original error.
- The `Create` variant without an initial event has no event args; pass `FiredArgs.Empty` rather than threading a nullable `FiredArgs?` parameter. The `Create(FiredArgs? args)` overload only appears on the "with initial event" path that delegates straight to `Fire`.
- `EmptyArgs` was the pre-CC#25 name. The correct name is `FiredArgs.Empty` everywhere in the evaluation loop pseudocode (Restore's `EmptyArgs` usage is legacy but explicitly excluded from change).
- §8 Integration Contract must show both public ingress lanes (JSON `JsonElement?` and CLR `Action<IArgBuilder>?`) materializing to `FiredArgs` before reaching `Evaluator.*`. A single-lane example that shows only a dictionary bridge is doubly wrong: wrong type and wrong architecture.
- When a gap register is fully migrated, the archived file should carry a migration header that records: date, total entries, breakdown by status, and which canonical docs received blocks. This makes the archive a reference rather than a dead dump.
- The 43-entry gap register distributed 23 Pending Decision gaps across 9 canonical docs — a healthy spread indicating the gaps were real cross-doc knowledge breadth, not concentrated defects in a single surface. Out-of-Scope was the clearest call: MCP output design, API naming, and grammar tooling implementation are not catalog metadata questions by definition.

## Recent Activity

### 2026-05-04T02:02:54Z — Catalog gap register migration audit

Shane requested migration of `docs/working/catalog-gap-register.md` open questions to canonical docs. Audit confirmed the migration was already completed in commit `2715872` (Frank-66). The register is archived at `docs/working/Archived/catalog-gap-register-migrated.md`.

**Migration summary confirmed:**
- 23 Pending Decision gaps → placed as `Open Question` blocks across 9 canonical docs (parser.md, graph-analyzer.md, proof-engine.md, type-checker.md, evaluator.md, diagnostic-system.md, language-server.md, tooling-surface.md, catalog-system.md)
- 5 Already Captured → confirmed in-place in catalog-system.md
- 3 Resolved in Source → #17, #18, #24 marked resolved; stale open-question bullets removed
- 4 Out of Scope → API/tooling questions excluded from canonical docs
- 8 Cross-cutting → tracked in cross-cutting-decisions.md; source attributions added in register

**Pattern observed:** The gap register had a clean triaging model — every entry had a clear status and a destination. The only ambiguity was between "Out of Scope" (not catalog metadata) and "Pending Decision" (catalog metadata needing a ruling). All 4 Out-of-Scope entries were correctly excluded: they were MCP output design, API naming, strategy scope, or grammar implementation tooling — none were catalog metadata questions. The 23 Pending Decision gaps distributed across 9 docs fairly evenly, suggesting the gap register captured real cross-doc knowledge breadth rather than concentrating on a single surface.

Wrote inbox decision entry `frank-catalog-gaps-migrated.md`.

### 2026-05-04T01:44:47Z — P2 doc fixes: four audit gap items

- Added `### Approaches Considered and Rejected` to `evaluator.md` §4 covering two items:
  - **TypeBuilder rejected** (Item 10): AOT incompatibility, catalog delegates achieve identical O(1) dispatch, TypeBuilder output is opaque.
  - **Upgrade seam toward compilation** (Item 12): A+G interpreter implements the same contract a compiled path would; pre-indexed dispatch model is the seam.
- Added `Operations` / `TypeRuntimeMeta.BinaryExecutors` relationship note to `evaluator.md` §7.3 (Item 11): explains that `Operations` aggregates per-type executors from `TypeRuntimeMeta` registrations at startup; evaluator is zero-knowledge about type identity.
- Audited all 13 Precept Innovations callouts in `compiler-and-runtime-design.md` (Item 14): no stale TypeBuilder, dual-interpreter, or code-gen claims found; tree-walk mentions correctly describe the rejected alternative; no changes needed.

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
