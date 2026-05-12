# Decision: B1 static Dimension-form compound cancellation gap closed

**Date:** 2026-05-12T09:02:45.968-04:00  
**By:** Soup Nazi  
**Status:** Implemented and verified

## Decision

Add a proof-engine regression test for the static `quantity of 'each/case'` qualifier form and keep the production path accepting that declaration/default combination.

## What Changed

- Added `CompoundUnit_cancellation_dimension_qualifier_form` to `test/Precept.Tests/ProofEngineTests.cs` beside the existing compound-cancellation regressions.
- Allowed `MapDimensionQualifier` to accept static compound-ratio strings on the `of` axis when both sides are valid unit atoms.
- Updated `QuantityValidator` to validate compound-ratio `of 'X/Y'` qualifiers by exact numerator/denominator unit shape instead of collapsing them to a plain dimension.
- Added `UnitDimensionHelper.TryGetCanonicalCompoundUnitCode` so count atoms such as `each/case` keep their compound identity during validation.

## Verification

- `dotnet test test\Precept.Tests\ --filter "CompoundUnit_cancellation_dimension_qualifier_form"` ✅
- `dotnet test test\Precept.Tests\` ✅ (4914 passed)
