## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs derive behavior from metadata and shape rather than hardcoded enum identity or parallel lists.
- Public API surfaces must expose stable CLR/JSON interchange types; evaluator internals stay internal and never leak into the durable surface.
- Operation legality lives in `Language.Operations`; computation lives in `TypeRuntime` plus the runtime dispatch registry. The evaluator stays zero-knowledge.
- Identity-type work follows the dual-shape rule: enriched internal entities when metadata/lifetime demands it, lightweight API-boundary code/value shapes when callers need stable interchange.
- Collection internals are settled around universal `PreceptValue[]` backing, stride-2 pair storage, static `CollectionActions` helpers, and evaluator-owned copy-on-write.
- Collection CLR adapters are lazy at the `Version` level and eager on first materialization, not per-index lazy.
- Working docs drift quickly during heavy deliberation; canonical docs and squad records must be synchronized as soon as a decision locks.
- Investigation docs may be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Recent Updates

### 2026-05-05T15:20:17Z — Value-types investigation ledger sync recorded

- Scribe merged Frank's value-types sync and catalog-delegate evaluation inbox notes into `.squad/decisions/decisions.md`.
- The canonical record now reflects the §§9–§14 investigation integration plus the `OperationMeta`/executor separation verdict.
- Processed inbox files were cleared after deduplication and ledger merge.

### 2026-05-05T05:19:25Z — Collection types investigation fully archived

- Full walkthrough of `docs/working/precept-collection-types-investigation.md` is complete and the document is now archived under `docs/working/Archived/`.
- `docs/runtime/evaluator.md` now carries the OQ-C3 direction-model closure in §7.4.1 §C: `EnqueueByPriority` takes `SortDirection`, and the new "Direction model (OQ-C3)" subsection makes declared-direction storage explicit.
- Remaining collection implementation guidance is already disseminated into the evaluator documentation; detailed provenance remains in `.squad/decisions.md`.

### Historical summary through 2026-05-05

- 2026-05-04 locked the execution architecture baseline: `TypeRuntime` owns per-type behavior, the runtime aggregation registry owns flat dispatch, and the evaluator never regains type-specific knowledge.
- Collection API direction stabilized around CLR-friendly adapters (`PreceptList<T>`, `PreceptLookup<TKey, TValue>`, `KeyedElement<TValue, TKey>`) with evaluator-owned CoW and declared-direction pair storage.
- Currency, unit, and dimension work converged on catalog-backed identity types with strict provenance notes and API-boundary-specific shapes.
- Use `.squad/decisions.md` for full per-decision provenance; keep `history.md` focused on durable operating context plus the newest closures.
