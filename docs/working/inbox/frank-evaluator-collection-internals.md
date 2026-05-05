### 2026-05-05T05:12Z: Collection internal implementation disseminated to `docs/runtime/evaluator.md`

**By:** Frank

**Status:** Documentation dissemination complete. evaluator.md is now the authoritative reference for collection internals.

**Merged source:** `frank-evaluator-collection-internals.md`.

- `docs/runtime/evaluator.md` **§7.4.1 Collection Internals** added (5 subsections):
  - **§A — Universal `PreceptValue[]` backing:** all 9 collection kinds; stride table; `ref` helper accessors; 5 rationale points; obsolete backing types list (Okasaki logs, `ImmutableDictionary`, `SortedDictionary`, etc. are explicitly off-limits).
  - **§B — CLR Adapter Types:** `PreceptList<T>` and `PreceptLookup<TKey, TValue>` lazy-at-Version-level, eager-on-first-read behavior; semantics.
  - **§C — `CollectionActions` Static Class:** full class shape; "Span in, count out" convention; why-not-wrappers table.
  - **§D — Copy-on-Write Protocol:** `ReferenceEquals` alias check; 5-step protocol table; cost model; `ArrayPool` lifecycle table; rollback snippet.
  - **§E — Scalability Guidance:** size zones table; `log` as the only structurally unbounded kind; `ICollectionBacking` deferred seam.
- `docs/runtime/evaluator.md` **§11 Design Rationale — Decisions 9–11** added:
  - Decision 9: Universal `PreceptValue[]` backing for all 9 collection kinds.
  - Decision 10: `CollectionActions` as a stateless static helper class.
  - Decision 11: Evaluator-owned copy-on-write for multi-mutation events.
- Source: `docs/working/precept-collection-types-investigation.md` §§8, 11–14. The investigation doc conclusions are now captured in evaluator.md; the investigation doc may be archived.
