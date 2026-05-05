### 2026-05-04T23:58Z: Computation locality — Option A analysis (superseded by frank-evaluator-vs-clr-computation)

**By:** Frank

**Status:** Analysis only — superseded. Final verdict in `frank-evaluator-vs-clr-computation.md` (Option B).

**Merged source:** `frank-computation-locality.md`.

- Proposed Option A: arithmetic on the types. `Money`, `Price`, `ExchangeRate`, `Quantity` carry their own computation methods. `IUnitConversionSource` injected for `Quantity.ConvertTo()`.
- `RoundToMinorUnit()` illustrates why computation fits on types: `Currency.MinorUnit` is already in `Precept.Types` — no evaluator needed.
- Computation boundary: same-unit/same-currency operations are fully self-contained on types; D8 auto-conversion requires catalog injection via `IUnitConversionSource`.
- **This analysis was superseded.** The evaluator-only computation model (Option B) was accepted. CLR types are now pure data records; computation lives in named executor modules registered on `TypeRuntimeMeta`.
