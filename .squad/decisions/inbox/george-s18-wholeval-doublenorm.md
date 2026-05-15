# George — Slice 18 WholeValue Double-Normalization Risk Note

**By:** George  
**Date:** 2026-05-15T15:35:00Z  
**For:** Soup Nazi (test coverage request)

---

## Summary

While reviewing `ProofEngine.Intervals.cs` for the §5.5.2 WholeValue double-normalization risk, I found that the current code **does not double-normalize**, but the guard is implicit and brittle.

---

## The Risk Path

In `IntervalOfNarrowed`, a WholeValue slot recurses into the slot expression:

```csharp
case InterpolatedTypedConstant interpolated:
{
    if (interpolated.Slots.Length == 1)
    {
        var slot = interpolated.Slots[0];
        if (slot.SlotKind is InterpolationSlotKind.Magnitude or InterpolationSlotKind.WholeValue)
            return IntervalOfNarrowed(slot.Expression, semantics, narrowed);
    }
    return NumericInterval.Unbounded;
}
```

For a `WholeValue` slot whose inner expression is a `TypedFieldRef`, `ExtractFieldInterval` returns the source field's **already-normalized** interval. Then in `IntervalOf`, `ApplyStaticUnitScaling` is called on the outer `InterpolatedTypedConstant`.

**Why it doesn't double-normalize now:** `ApplyStaticUnitScaling` only fires when `HasSingleMagnitudeSlot(interpolated)` is true — and `HasSingleMagnitudeSlot` checks for `InterpolationSlotKind.Magnitude` specifically, excluding `WholeValue`. So the second scaling is skipped.

---

## The Brittle Assumption

The guard relies on `HasSingleMagnitudeSlot` excluding WholeValue. If someone ever extends `HasSingleMagnitudeSlot` to include WholeValue (e.g., to support static-unit WholeValue scaling), double normalization will silently corrupt quantity interval containment checks.

Additionally: for a WholeValue slot whose inner expression is a `TypedTypedConstant` literal (e.g., a constant like `'5 kg'` arriving as a WholeValue form), `TryExtractTypedConstantMagnitudeRaw` returns the raw magnitude (5), and `ApplyStaticUnitScaling` skips scaling (WholeValue guard). The resulting interval Point(5) would be compared against the target field's normalized bound (e.g., 0.005 for `min '5 g'` on a kg field), producing a false positive overflow. This path may not be reachable in valid DSL today, but it is structurally reachable.

---

## Requested Test Coverage (Slice 17)

Please add the following tests when writing Slice 17 tests:

1. **WholeValue interval passes unchanged** — `field a max '5 kg'`, `field b max '8 kg'`, assignment `set a = '{b}'`. Source field b interval [0..8 kg normalized] should be compared against a's bound [0..5 kg normalized]. No double-normalization should occur. Currently this test exists in `TypeCheckerQuantityNormalizationTests.QuantityBound_WholeValueInterpolation_UsesSourceQuantityIntervalWithoutDoubleNormalization` but only covers the green path.

2. **WholeValue overflow detected** — same setup but b's max exceeds a's max — obligation should be Unresolved → NumericOverflow. A version of this test was added in the working branch but depends on the WholeValue form being correctly parsed.

3. **Regression anchor for `HasSingleMagnitudeSlot` exclusion** — a direct unit test asserting `HasSingleMagnitudeSlot` returns false for a WholeValue slot, so that any future expansion would require a deliberate choice.

---

## No Code Change Required Now

The double-normalization is not currently materialized. The guard is working. No fix is needed in Slice 18. This note records the structural risk so Slice 17 test coverage can lock the invariant explicitly.
