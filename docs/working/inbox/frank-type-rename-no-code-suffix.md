### 2026-05-05T00:39Z: Rename recommendations — `Code` suffix removal analysis

**By:** Frank

**Status:** Recommendation recorded — Shane decision needed to finalize.

**Merged source:** `frank-type-rename-no-code-suffix.md`.

- `UnitOfMeasureCode` → `UnitOfMeasure`: **Approved.** No direct CLR naming conflict. Distinguishes the lightweight API proxy from the evaluator-internal `Unit` entity without stealing the shorter name.
- `DimensionCode` → `Dimension`: **Rejected.** Conflict with the existing `ProofRequirementMeta.Dimension` type in live source, and conceptual collision with the planned algebraic `Dimension` SI-exponent type.
- If `Code` suffix must go on `DimensionCode`: use `MeasureDimension` — distinct from the algebraic `Dimension`, domain-readable, pairs well with `UnitOfMeasure`.
- `CurrencyCode` is not relevant here; the accepted direction already uses `Currency` (sealed class).
