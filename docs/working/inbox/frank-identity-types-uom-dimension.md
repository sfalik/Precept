### 2026-05-04T23:22Z: `UnitOfMeasureCode` and `DimensionCode` CLR identity-type designs

**By:** Frank

**Status:** Recommendation recorded — awaiting Shane decisions on final naming.

**Merged source:** `frank-identity-types-uom-dimension.md`.

- `UnitOfMeasureCode` is a `readonly record struct` (not `Unit` sealed class) — the API proxy carrying only the validated UCUM code string. `Unit` is the evaluator-internal enriched catalog entity; `UnitOfMeasureCode` is the lightweight field value at the public boundary.
- `DimensionCode` is likewise a `readonly record struct` carrying only the dimension name string. Same dual-shape rationale: `Dimension` (7-exponent SI vector) is the evaluator-internal type; `DimensionCode` is the identity code.
- Both types include a curated set of `static readonly` well-known members (~25–30 Tier 1 units for `UnitOfMeasureCode`; 12–15 named dimensions for `DimensionCode`) for DX convenience.
- Both implement `Parse`, `TryParse`, and structural equality. `UnitOfMeasureCode` supports entity-scoped unit lookup via a `PreceptDefinition context` overload.
- The governing principle: expose the catalog entity at the API boundary only when all its properties are consumer-facing; use a proxy struct when the entity carries evaluator-internal metadata (tiers, conversion factors, SI exponent vectors).
