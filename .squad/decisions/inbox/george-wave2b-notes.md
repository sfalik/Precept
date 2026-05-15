# George Wave 2b Notes

## Implemented
- Slice 35: `TypedConstantNormalizer` now supports affine params via `TryGetStaticAffineParams`, uses `(value + offset) * scale` in `NormalizeQuantity`, and inverse affine transform in `DenormalizeQuantity`.
- Slice 36: Added `NumericInterval.Shift(decimal)` and updated `ProofEngine.IntervalOf` static-unit scaling to apply affine transforms (`Shift` then `Scale`) for affine quantity units.
- Slice 37: Affine matrix tests now pass; added precision trimming for affine normalization/interval scaling to eliminate decimal noise at boundaries.

## Test expectation corrections
- Corrected `UcumAtom_dB_NoAffineOffset` to parse `dB` via `UcumParser` instead of direct `UcumAtomCatalog.All["dB"]` lookup, because `dB` is valid parsable UCUM but not guaranteed as a direct key in `All`.

## Open issues / edge cases
- Full `Precept.Tests` still has 9 existing non-affine failures (ProofEngine/type-checking areas unrelated to this wave2b affine lane).

## Validation snapshot
- `dotnet build src\Precept\Precept.csproj`: **Succeeded**
- `dotnet test test\Precept.Tests\Precept.Tests.csproj`: **Failed** (Failed: 9, Passed: 5513, Skipped: 0, Total: 5522)
- Affine-focused validation: `TypedConstantNormalizerTests` + `ProofEngineIntervalTests` + Celsius/Fahrenheit interval integration check are passing.
