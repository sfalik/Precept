### 2026-05-04T23:31Z: API boundary consistency — `Currency` vs. `UnitOfMeasure`/`MeasureDimension` shapes justified

**By:** Frank

**Status:** Recommendation recorded.

**Merged source:** `frank-uom-dimension-currency-consistency.md`.

- The apparent asymmetry (Currency = sealed class, UnitOfMeasureCode/DimensionCode = readonly record structs) is real and architecturally justified, not inconsistent.
- `Currency`: every property (AlphaCode, Name, Symbol, MinorUnit, NumericCode) is consumer-facing → expose the catalog entity directly as the API type.
- `Unit`: carries evaluator-internal fields (Tier, DimensionVector, conversion factors) → API boundary needs a lean proxy struct (`UnitOfMeasureCode`) that strips internal concerns.
- `Dimension` (SI exponent vector): dimensional analysis machinery → `DimensionCode` proxy is the API boundary type.
- Governing principle: *expose the catalog entity at the API boundary only when all its properties are consumer-facing; use a proxy struct otherwise.*
