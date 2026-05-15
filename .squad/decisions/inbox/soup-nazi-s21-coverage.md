# Soup Nazi — Slice 21 Coverage Report

**Date:** 2026-05-15T11:37:42Z  
**Author:** Soup Nazi (Tester)  
**Ref:** Slice 21 — Interpolated Quantity Overflow Proofs  
**File:** `test/Precept.Tests/TypeChecker/TypeCheckerInterpolatedQuantityTests.cs`

---

## Test Results

All 10 tests in `TypeCheckerInterpolatedQuantityTests` pass. Suite: 5543 total / 5534 passed / 9 failed (9 failures are unchanged pre-existing baseline).

| # | Test Name | Result | Notes |
|---|-----------|--------|-------|
| 1 | `InterpolatedQuantity_StaticUnitMagnitudeWithinMax_DoesNotEmitNumericOverflow` | ✅ GREEN | Pre-existing |
| 2 | `InterpolatedQuantity_StaticUnitMagnitudeExceedsMax_EmitsNumericOverflow` | ✅ GREEN | Pre-existing |
| 3 | `InterpolatedQuantity_UnboundedMagnitudeField_EmitsNumericOverflowConservatively` | ✅ GREEN | New |
| 4 | `InterpolatedQuantity_WholeValueSlot_SourceFieldWithinMax_DoesNotEmitNumericOverflow` | ✅ GREEN | New |
| 5 | `InterpolatedQuantity_WholeValueSlot_SourceFieldExceedsMax_EmitsNumericOverflow` | ✅ GREEN | New |
| 6 | `InterpolatedQuantity_DynamicUnitSlot_EmitsNumericOverflowConservatively` | ✅ GREEN | New |
| 7 | `InterpolatedPrice_DynamicDenominatorSlot_EmitsNumericOverflowConservatively` | ✅ GREEN | New |
| 8a | `InterpolatedMoney_MagnitudeSlotWithinMax_DoesNotEmitNumericOverflow` | ✅ GREEN | New |
| 8b | `InterpolatedMoney_MagnitudeSlotExceedsMax_EmitsNumericOverflow` | ✅ GREEN | New |
| 9 | `InterpolatedQuantity_WholeValueCrossUnit_NoDoubleNormalization_DoesNotEmitNumericOverflow` | ✅ GREEN | New |

No red tests. The Slice 19/20 implementation covers all required paths.

---

## Implementation Verified

The following behavioral contracts are now locked by executable tests:

1. **Magnitude slot + static unit suffix**: `'{intField} [lb_av]'` — interval scales by UCUM factor; bounded → no overflow, overflowing → overflow.
2. **Unbounded magnitude field**: No declared max → `ExtractFieldInterval` returns Unbounded → conservative overflow fires. This is the correct safe-by-default behavior.
3. **WholeValue slot (same-dimension, kg→kg)**: `IntervalOfNarrowed` recurses into the source field; `ApplyStaticUnitScaling` does NOT re-scale (HasSingleMagnitudeSlot = false for WholeValue). Proved when within bound, Unresolved when exceeding.
4. **Dynamic Unit slot** (`'3 {unitField}'`): Single `Unit` slot is not `Magnitude` or `WholeValue` → Unbounded → conservative overflow. Correct; unit is not statically known.
5. **Dynamic denominator price** (`'5 USD/{unitField}'`): Unit slot on a price field → Unbounded → conservative overflow.
6. **Money magnitude slot** (`'{amount} USD'`): Currency qualifier carries no UCUM unit → no scaling; proof dispatches purely on the integer magnitude interval vs. the money bound.
7. **WholeValue cross-unit** (lb→kg): `NormalizedDeclaredMax` on the source field is read (≈ 1.36 kg for `max '3 [lb_av]'`); `HasSingleMagnitudeSlot` guard prevents re-scaling. Proof is Proved (1.36 < 5).

---

## Double-Normalization Finding (from George W1 / §5.5.2)

**Status:** Implementation is correct. Guard is `HasSingleMagnitudeSlot` in `ProofEngine.Intervals.cs` line 317.

The `WholeValue` slot kind returns `false` from `HasSingleMagnitudeSlot`, so `ApplyStaticUnitScaling` returns the interval unchanged. This means the already-normalized bound from `NormalizedDeclaredMax` is used directly without re-applying the source unit's scale factor.

**Limitation of test 9 as a double-normalization detector:** The test uses `max '5 kg'` target with `max '3 [lb_av]'` source (normalized: 1.36 kg). If double-normalization were active, the interval would be 0.617 kg — which still fits in 5 kg. Therefore test 9 does NOT expose false safety; it anchors the correct happy-path behavior.

**To write a genuine double-normalization detection test** (if the bug ever resurfaces):
```csharp
// Source: max '3 [lb_av]' → normalized 1.36 kg
// Target: max '1 kg'
// Correct: 1.36 > 1.0 → NumericOverflow fires
// With double-norm: 0.617 < 1.0 → no overflow (false safety)
```
This tighter-bound variant is not added here (test 9 per spec uses 5 kg), but it should be added if `HasSingleMagnitudeSlot` is ever changed or its call site modified.

---

## Coverage Gap: Tests 4 & 5 vs. Normalization File

Tests 4 (`WholeValueSlot_SourceFieldWithinMax`) and 5 (`WholeValueSlot_SourceFieldExceedsMax`) in `TypeCheckerInterpolatedQuantityTests` are structurally identical to tests in `TypeCheckerQuantityNormalizationTests` (which cover the same kg→kg WholeValue case). The duplication is intentional: the normalization file tests are anchored to the `weight/qtyField` naming and precept structure, while the interpolated file tests are anchored to the `x/qtyField` interpolated-assignment structure. Both test classes are needed because they use different compilation helpers and test slightly different precept shapes.

---

## No New Regressions

Ran full `dotnet test test/Precept.Tests/` after committing: 9 pre-existing failures, 5534 passed, 5543 total. No new failures introduced.
