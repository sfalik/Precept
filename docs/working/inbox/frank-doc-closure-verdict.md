### 2026-05-05T03:45Z: Doc closure status for collection and value-types investigation docs

**By:** Frank

**Status:** Recorded — action items for Frank and Shane.

**Merged source:** `frank-doc-closure-verdict.md`.

- **`precept-collection-types-investigation.md`:** Ready to archive after two minor corrections: (1) `KeyedElement` namespace declaration → `Precept.Types`; (2) raw lane return type → `JsonElement`. Then archive to `docs/working/Archived/`. No Shane input needed.
- **`precept-value-types-investigation.md`:** Needs updates before archive. Nine stale sections resolvable by Frank now (CLR shapes use `UnitOfMeasure`, `MeasureDimension`, `Currency` structs/class; computation locality settled; operator overloads superseded; `Precept.Types` assembly). Ten open questions block full archival — blocked on Shane for: OQ-3b, OQ-3f, OQ-CUR-1, OQ-CUR-3, OQ-CUR-4, OQ-7a, OQ-7e, OQ-7f, OQ-CL-1–5, OQ-DISP-1. The doc's investigation purpose is fulfilled; archival with explicitly-marked open OQs is viable.
- **`runtime-api.md`:** Current. No changes needed. Optional: add `Money` registration example alongside `decimal` example.
