# Slice 12 Blocked: Price Has No Temporal Qualifier Axis

**By:** George  
**Date:** 2026-05-11T18:41:49-04:00  
**Status:** 🚫 Blocked — prerequisite missing  
**Context:** Slice 12 (G8 + G13) asks for `QualifierChainProofRequirement` on `PriceTimesPeriod` and `PriceTimesDuration`. Investigation reveals the price type cannot carry temporal denominator information.

---

## Finding

The plan assumes `price per 'month'` declares a temporal qualifier axis. In reality:

1. **No `per` preposition exists** — `TokenKind.Per` does not exist in the token catalog.
2. **Price uses `QS_CurrencyAndDimension`** — `in` → `QualifierAxis.Currency`, `of` → `QualifierAxis.Dimension` (physical). There is no temporal axis on price.
3. **Period uses `QS_TemporalUnitOrDimension`** — `of` → `QualifierAxis.TemporalDimension`, `in` → `QualifierAxis.TemporalUnit`. These are distinct from price's physical `Dimension` axis.
4. **Duration has no qualifier shape at all** — it is intrinsically time-dimension, unqualified.
5. **`ExtractComparableValue` doesn't handle `TemporalDimension` or `TemporalUnit`** — chain comparison returns null for temporal qualifiers.

### Why adding catalog entries would be incorrect

If we add `QualifierChainProofRequirement(PPrice, QualifierAxis.Dimension, PPeriod, QualifierAxis.TemporalDimension, ...)`:
- Any `price of 'mass' * period of 'date'` would fail because `ExtractComparableValue(TemporalDimension(Date))` returns null.
- Unqualified price × period would also fail (null left side).
- This **breaks existing valid operations** — price × period currently compiles clean when both sides are valid.

### What needs to happen first

For temporal chain validation to work, price needs temporal denominator support:
- Option A: Extend price's qualifier shape to include a temporal axis (e.g., `per` → `QualifierAxis.TemporalUnit`)
- Option B: Generalize the `Dimension` axis to bridge physical and temporal dimensions
- Either option requires type system changes beyond ~8 LOC catalog entries.
- `ExtractComparableValue` also needs `TemporalDimension` and `TemporalUnit` arms.

### Recommendation

Defer Slice 12 until the price type supports temporal denomination. The gap is real (G8/G13 are valid observations) but the fix requires type system work, not just catalog metadata.

---

## Impact

- **No code changes made** — adding incorrect requirements would regress existing valid price × period arithmetic.
- **Plan Slice 12 LOC estimate of ~8 is wrong** — prerequisite work is ~40-80 LOC across Types.cs, TypeChecker.cs, ProofEngine.cs, and Operations.cs.
- **Slices 7–11 are unaffected** — they are complete and correct.
