### 2026-05-04T23:44Z: `Precept.Types` — separate type library assembly decision

**By:** Frank

**Status:** Recommendation recorded — Shane confirmation needed.

**Merged source:** `frank-type-library-assembly.md`.

- A standalone `Precept.Types` assembly is required. Consumers who want `Money`, `Currency`, `UnitOfMeasure`, etc. in their DTO layer must not pull in the full Precept compiler pipeline.
- Dependency graph: `Precept.Types` has no Precept dependencies; `Precept` (evaluator) references `Precept.Types`; consumers may reference either.
- `Precept.Types` contents: `Currency`, `CurrencyCatalog`, `UnitOfMeasureCode`, `DimensionCode`, `Money`, `Price`, `ExchangeRate`, `Quantity`, `KeyedElement<TValue, TKey>`, `IUnitConversionSource`, embedded ISO 4217 resource.
- `Precept` (evaluator assembly) retains: `Unit` (sealed class with Tier + DimensionVector + ConversionFactors), `UnitCatalog`, `DimensionCatalog`, all pipeline stages, embedded UCUM resource.
- The NodaTime analogy taken seriously implies a separate package, not just separate files.
