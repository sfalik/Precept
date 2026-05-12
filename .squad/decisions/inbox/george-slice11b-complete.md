# George — Slice 11B Complete

**Date:** 2026-05-11  
**Author:** George (Runtime Dev)  
**Slice:** 11B — Temporal Price Denominator Type System Extension  

---

## What Was Implemented

Four work items, ~30 LOC, all passing:

**A. `ExtractQualifiers` temporal routing for price** (`TypeChecker.cs`)  
Added a guarded `QualifierAxis.Dimension` arm in the switch. When `typeRef.ResolvedKind == TypeKind.Price && qualifier.Value is "date" or "time"`, routes to `MapTemporalDimensionQualifier` instead of `MapDimensionQualifier`. `quantity of 'time'` still emits `InvalidDimensionString` — the type guard keeps temporal acceptance scoped to price only.

**B. `ExtractComparableValue` temporal arms** (`ProofEngine.cs`)  
Added `TemporalUnit` → `tu.UnitName` and `TemporalDimension` → `"date"/"time"/null` arms. `PeriodDimension.Any` returns null (locked decision). Physical and temporal dimension name spaces are disjoint so no collision with the existing `Dimension` arm.

**C. `ResolveQualifierOnAxis` fallback chain** (`ProofEngine.cs`)  
Extended the existing `Unit → Dimension` fallback (Slice 9) with a new `Dimension → TemporalDimension` fallback for price temporal denomination. Then added an implied-qualifier check loop: after both declared and fallback paths are exhausted, check `Types.GetMeta(field.ResolvedType).ImpliedQualifiers`. Full chain: `Unit → Dimension → TemporalDimension → implied`.

**D. `ImpliedQualifiers` on `TypeMeta` + Duration entry** (`Type.cs`, `Types.cs`)  
Added `DeclaredQualifierMeta[]? ImpliedQualifiers = null` positional parameter (last, defaulted) and a computed property (parallel to `ImpliedModifiers`). Duration entry gets `ImpliedQualifiers: [new DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Time, QualifierOrigin.Baseline)]`.

**MCP sync** (`LanguageToolDtos.cs`, `LanguageTool.cs`)  
Added `string[]? ImpliedQualifiers` to `TypeCatalogEntryDto`. `MapType` renders implied qualifiers as `"Axis:Value"` strings via a new `RenderImpliedQualifier` helper. Null when empty.

**Test entry points** (`ProofEngine.cs`)  
Added `ExtractComparableValueForTest` and `GetImpliedQualifierOnAxis` internal shims for direct unit testing.

---

## Tests Written (13 tests, `Slice11B_TemporalPriceDenominatorTests.cs`)

All 13 pass, zero new failures introduced (26 pre-existing failures on spike branch unchanged):

1. `Price_Of_Time_CompilesClean_And_Stores_TemporalDimension_Time` ✅  
2. `Price_Of_Date_CompilesClean_And_Stores_TemporalDimension_Date` ✅  
3. `Price_Of_Mass_CompilesClean_And_Stores_Physical_Dimension` ✅  
4. `Quantity_Of_Time_Emits_InvalidDimensionString` ✅  
5. `ExtractComparableValue_TemporalUnit_Returns_UnitName` ✅  
6. `ExtractComparableValue_TemporalDimension_Time_Returns_Time_String` ✅  
7. `ExtractComparableValue_TemporalDimension_Date_Returns_Date_String` ✅  
8. `ExtractComparableValue_TemporalDimension_Any_Returns_Null` ✅  
9. `Duration_TypeMeta_Has_Implied_TemporalDimension_Time` ✅  
10. `ResolveQualifierOnAxis_DurationField_TemporalDimension_Returns_ImpliedTime` ✅  
11. `Regression_Period_Of_Time_Still_Stores_TemporalDimension` ✅  
12. `Regression_Quantity_Of_Mass_Still_Stores_Physical_Dimension` ✅  
13. `Regression_Non_Duration_Types_Have_No_ImpliedQualifiers` ✅  

---

## Deviations from Spec

None. Spec was followed exactly.

One implementation note: the spec says "resolvedKind is available via `qualified.InnerType.ResolvedKind` (already computed in the method)" — the method doesn't actually pre-compute this, but `typeRef.ResolvedKind` (on the outer `QualifiedTypeReference`) produces the identical result through the `ParsedTypeReference.ResolvedKind` dispatch. Used `typeRef.ResolvedKind` directly.

Added an extra test (test 7 — `TemporalDimension(Date)` → "date") beyond the spec's 8 scenarios because the spec's test 6 only explicitly tests `Time` and it's correct to also pin `Date`. Total 13 tests including regressions.

---

## Is Slice 12 Unblocked?

**Yes.** All four prerequisites are now in place:

| Gap | Status |
|-----|--------|
| `price of 'time'`/`'date'` stores `TemporalDimension` | ✅ Slice 11B |
| `ExtractComparableValue` handles `TemporalUnit`/`TemporalDimension` | ✅ Slice 11B |
| `ResolveQualifierOnAxis` `Dimension → TemporalDimension` fallback | ✅ Slice 11B |
| Duration has implied `TemporalDimension(Time)` qualifier | ✅ Slice 11B |

Slice 12 can now add `QualifierChainProofRequirement` entries to `PriceTimesPeriod` (G8) and `PriceTimesDuration` (G13) with confidence that the chain comparison infrastructure will work. The validation matrix from the spec is fully exercisable.

---

## Completions Note

Price `of` completions should include `'time'` and `'date'` alongside physical dimension names (~2 LOC in the dimension completion provider). I didn't find an obvious completion hook in the scope of this slice — flagged here as a Tooling N/A for Kramer to pick up if the completion provider lives in the language server.
