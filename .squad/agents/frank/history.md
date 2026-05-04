## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Durable baseline: catalogs remain the language truth; consumers derive behavior from metadata/shape instead of construct identity.
- Active runtime baseline: production execution uses `PreceptValue` tagged values, slot arrays, eager execution plans, and catalog-owned dispatch; public execution remains sync-only.
- Typed ingress/egress baseline: `Version.Get<T>` / `FiredArgs.Get<T>` are the primary typed accessors; JSON restore stays the only hydration lane; typed builders (`IArgBuilder`, `IFieldBuilder`) flow through catalog-owned runtime metadata.
- `TypeMeta.Runtime` is the catalog-owned home for runtime conversion/serialization behavior; any lookup tables must be derived from catalog metadata rather than maintained in parallel.
- Collections stay BCL-first with persistent semantics preserved for working-copy discard.
- Canonical docs state what the system is. Decision provenance, CC# numbers, and working-status annotations belong in the squad ledger / working docs rather than canonical documentation.

## Learnings

- Structural gap register migration closed 46 entries: 44 open gaps were re-homed into canonical docs across parser, type-checker, graph-analyzer, proof-engine, precept-builder, evaluator, language-server, MCP, and literal-system docs; 2 entries were already resolved by the `ParsedExpression` / `TypedExpression` decisions (#45, #53). Pattern: the remaining structural debt clustered around cross-stage shape ownership — parser/type-checker slot contracts, proof-to-runtime site identity, evaluator/inspection result shapes, and tooling indexes that still want first-class compile outputs.
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
- Audit-gap reports can go stale within hours when canonical docs move fast; before trusting a non-✅ status marker or summary total, re-verify the underlying canonical doc and then recalculate the rollup.

## Recent Activity

### Historical summary through 2026-05-04T03:39:16Z
- Consolidated the late-April and early-May design corpus: collection-surface research, parser/catalog remediation, annotation-bridge decisions, CC#25 Q1-Q7 rulings, CC#2 acceptance, and the related canonical-doc sync passes across runtime, evaluator, parser, proof, and catalog surfaces.
- Durable pattern: once a design locked, Frank moved canonical docs to factual architecture, pushed provenance back into squad records, and treated working docs only as temporary coordination scaffolding.

### 2026-05-04T03:45:15Z — Runtime API doc sync baseline
- `runtime-api.md` now reflects two-lane ingress, `PreceptValue`-based access, typed builders, and JSON-only restore semantics.
- The durable squad baseline is that dictionary-based convenience ingress is out of scope.

### 2026-05-04T04:36:09Z — Citation cleanup closeout
- Canonical docs had CC#/decision-provenance wrappers removed while `docs/working/` navigation links stayed in place where still useful.
- Durable takeaway: canonical docs state architecture; squad records carry provenance.

### 2026-05-04T04:36:09Z — Deep content audit closeout
- Scribe recorded the batch that pushed evaluator/runtime docs to the active specificity baseline: `PreceptValue` memory picture, 7-step Fire lifecycle, slot-index `LOAD_ARG`, executor arrays, and the arg presence mask definition.
- The archive hard gate was checked in that pass; no >30-day active entries were eligible then.

### 2026-05-04T05:45:56Z — Audit gap walkthrough closeout
- The audit gap report was driven to the current state: resolved doc gaps were documented, stale status markers were corrected, and only genuine open items remained pending owner sign-off or follow-up.
- Working takeaway: before trusting a gap report rollup, re-verify the canonical docs and then recalculate the status summary.

### 2026-05-04T12:31:31Z — Scribe recorded cross-cutting driver and audit-status batch
- Scribe logged frank-78 (`e10bcef`) and frank-79 (`2b5df03`), merged their inbox notes into the decision ledger, cleared the decisions inbox, and refreshed Frank's durable history.
- The working-doc state now reflects the 5-wave cross-cutting execution driver and the corrected audit-gap status markers as the recorded batch baseline.
