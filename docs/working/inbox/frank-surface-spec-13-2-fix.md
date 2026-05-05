### 2026-05-05T04:37Z: Surface spec §13.2–13.6 corrected to eager-on-first-read adapter semantics

**By:** Frank

**Status:** Applied.

**Merged source:** `frank-surface-spec-13-2-fix.md`.

- `docs/working/runtime-api-public-surface-spec.md` §13.2–13.6 corrected: `PreceptList<T>` and `PreceptLookup<K,V>` are eager-on-first-read (full `T[]` materialization on first access), not per-index lazy projection.
- Old framing: `this[int index]` invokes projection function on every access — zero allocation, per-access cost.
- New framing: on first access materialize full `T[]` from backing `PreceptValue[]`; serve all subsequent reads from materialized array. O(n) once, O(1) thereafter. "Lazy" applies only at the Version level (adapter constructed on first field read), not at the element level.
- Sections touched: §13.2 Option A, §13.3 evaluation table, §13.4 recommendation, §13.5 resolved note, §13.6 adapter inventory.
