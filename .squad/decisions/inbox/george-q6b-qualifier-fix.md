# George Q6b — Unit-Aware Qualifier Derivation for Dimensionless UCUM Units

**Date:** 2026-05-09  
**Author:** George (Runtime Dev)  
**Branch:** Precept-V2-Radical  
**Commits:** recorded in the Q6b qualifier-fix change set

---

## Decision

`TypeChecker.MapUnitQualifier(...)` now derives the unit qualifier category with unit-code awareness instead of trusting the UCUM dimension alias for every dimensionless unit.

## Locked Rules

- Known count-category UCUM codes stay mapped to `count`: `1`, `%`, `[ppm]`, `[ppb]`, `[ppth]`, `[pptr]`, `each`, `[iU]`, `[arb'U]`, `[USP'U]`, `[CFU]`, `[pH]`, and `dB`.
- Angle and solid-angle units must not inherit the `count` alias just because UCUM represents them with `DimensionVector.None`; `rad`, `deg`, `'`, `''`, `gon`, `sr`, and derived units that use those atoms now return an empty derived dimension category.
- The fallback behavior for any other dimensionless UCUM unit still uses `DimensionCatalog.TryGetAlias(...)`, so `count` remains the default business alias when no non-count unit evidence is present.
- The fix lives in `src/Precept/Pipeline/TypeChecker.cs`; `DimensionCatalog.TryGetAlias(...)` remains unchanged.

## Files

- `src/Precept/Pipeline/TypeChecker.cs`
- `test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs`

## Validation

- `dotnet build src\Precept\Precept.csproj`
- `dotnet test test\Precept.Tests\Precept.Tests.csproj`

## Outcome

Runtime build passed and the runtime test suite passed with explicit regression coverage for `rad`, `deg`, `sr`, `1`, `%`, and `kg` quantity qualifiers.
