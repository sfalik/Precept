### 2026-05-05T04:32Z: Collection types investigation — section-by-section surface spec coverage audit

**By:** Frank

**Status:** Analysis complete — all prescriptions confirmed applied.

**Merged source:** `frank-collection-doc-analysis.md`.

- All §§1–10 prescriptions from the collection types investigation are confirmed applied to `docs/working/runtime-api-public-surface-spec.md`.
- §§11–14 (internal representation, scalability, action architecture, CoW protocol) have no surface-spec prescriptions — marked N/A. They should eventually migrate to `docs/runtime/evaluator.md` as implementation guidance (Wave 3/4).
- §3 `PreceptValue` leakage conflict noted: canonical `docs/runtime/runtime-api.md` still shows `PreceptValue` in several public signatures. These are stale given the raw lane = JsonElement ruling. Stale locations: `Version` indexer (line 234), `FieldAccessInfo.CurrentValue` (line 428), `ConstraintViolation.FailingValue` (line 390), `FiredArgs` indexer (line 543), overview paragraph (line 37).
