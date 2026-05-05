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

## Learnings

- Working docs (`runtime-api-public-surface-spec.md`) can fall out of sync with later-locked authoritative design docs. The temporal type system doc's NodaTime alignment directive supersedes any earlier BCL-type mappings in working docs.
- The temporal doc's Explicit Exclusions table is the authority on what is NOT in scope - check it before promoting any temporal-adjacent type to "confirmed."
- The NodaTime alignment directive is absolute: expose NodaTime types directly at the backing layer. No DateOnly/TimeOnly wrappers, no custom temporal structs. The pattern is `field DueDate as date` -> `NodaTime.LocalDate` backing.

## Recent Updates

### 2026-05-05T15:32:50Z - Value-types investigation reconciled to authoritative docs

- Reconciled `docs/working/precept-value-types-investigation.md` against `docs/language/temporal-type-system.md` and `docs/language/business-domain-types.md`.
- Corrected §7.4 `DateRange` from confirmed to deferred, aligned `date` to `NodaTime.LocalDate`, and removed the stale `DateOnly` public-surface claim.
- All remaining sections stayed consistent; the only follow-up tensions left for Shane are the DX-layer well-known-constants surface and the authoritative `currency` accessor doc lag.

### 2026-05-05T15:20:17Z - Value-types investigation ledger sync recorded

- Scribe merged Frank's value-types sync and catalog-delegate evaluation inbox notes into `.squad/decisions/decisions.md`.
- The canonical record now reflects the 9-14 investigation integration plus the `OperationMeta`/executor separation verdict.
- Processed inbox files were cleared after deduplication and ledger merge.

### 2026-05-05T11:32:50Z - Value types investigation reconciled against authoritative docs

- Reconciled `docs/working/precept-value-types-investigation.md` against `docs/language/temporal-type-system.md` and `docs/language/business-domain-types.md`.
- 7.4 DateRange: Corrected from confirmed to deferred - the temporal type system doc explicitly defers DateInterval/daterange in its Exclusions table. Changed CLR type from DateOnly to NodaTime.LocalDate to match the locked backing type decision. Removed the erroneous "public API uses DateOnly" claim (the NodaTime alignment directive says expose NodaTime directly).
- 7.6 Summary Table: Updated DateRange row to reflect deferred status.
- All other sections (1-6, 7.1-7.3, 7.5, 8-14) verified as consistent with authoritative docs. No changes needed.
- Flagged two tensions for Shane: 12 well-known constants vs 4 "no blessed subset" principle; 8 currency accessors extending beyond what the authoritative doc currently specifies (locked Shane decision, not a contradiction).
- Decision record: `.squad/decisions/inbox/frank-value-types-reconciliation.md`.

### 2026-05-05T05:19:25Z - Collection types investigation fully archived

- Full walkthrough of `docs/working/precept-collection-types-investigation.md` is complete and the document is now archived under `docs/working/Archived/`.
- `docs/runtime/evaluator.md` now carries the OQ-C3 direction-model closure in 7.4.1 C: `EnqueueByPriority` takes `SortDirection`, and the new "Direction model (OQ-C3)" subsection makes declared-direction storage explicit.
- Remaining collection implementation guidance is already disseminated into the evaluator documentation; detailed provenance remains in `.squad/decisions.md`.

### Historical summary through 2026-05-05

- 2026-05-04 locked the execution architecture baseline: `TypeRuntime` owns per-type behavior, the runtime aggregation registry owns flat dispatch, and the evaluator never regains type-specific knowledge.
- Collection API direction stabilized around CLR-friendly adapters (`PreceptList<T>`, `PreceptLookup<TKey, TValue>`, `KeyedElement<TValue, TKey>`) with evaluator-owned CoW and declared-direction pair storage.
- Currency, unit, and dimension work converged on catalog-backed identity types with strict provenance notes and API-boundary-specific shapes.
- Use `.squad/decisions.md` for full per-decision provenance; keep `history.md` focused on durable operating context plus the newest closures.
