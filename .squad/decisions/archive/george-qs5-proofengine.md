# QS-5 — CompoundPrice Axis Routing in ProofEngine

**Author:** George  
**Date:** 2025-07-24  
**Commit:** c020d1de  
**Branch:** spike/Precept-V2-Radical

## What Changed

### A. `TryProjectCompoundPrice` — new helper (ProofEngine.Qualifiers.cs)

Added a private helper that takes any `DeclaredQualifierMeta` and a target `QualifierAxis`, and if the qualifier is a `CompoundPrice`, projects it onto the requested axis:
- `Currency` axis → returns `DeclaredQualifierMeta.Currency(compound.CurrencyCode)`
- `Unit` axis → returns `DeclaredQualifierMeta.Unit(compound.UnitCode, compound.DimensionName)`
- `Dimension` axis → returns `DeclaredQualifierMeta.Dimension(compound.DimensionName)` (if non-empty)
- Otherwise → `null`

This is the core routing mechanism. All qualifier resolution paths call this as a fallback after the standard axis-matching loops.

### B. `ResolveQualifierOnAxis` — PriceIn fallbacks added

Three qualifier-list iteration sections (argQualifiers, tcQualifiers, field.DeclaredQualifiers) now include a final PriceIn fallback loop that calls `TryProjectCompoundPrice` after all standard axis and dimension fallbacks.

### C. `ResolveQualifierFromExpression` — PriceIn fallbacks added

Same pattern as above for the argQuals and tcQuals cases in the expression switch.

### D. `ResolveFieldQualifier` — PriceIn fallback added

Added after implied-qualifier check. Ensures field-level CompoundPrice qualifiers are discoverable on Currency/Unit/Dimension axes.

### E. `QualifiersAreCompatible` — CompoundPrice arm

Two `CompoundPrice` qualifiers are compatible iff both `CurrencyCode` AND `UnitCode` match (string ordinal). Cross-subtype (`CompoundPrice` vs `Currency`) falls through to `return false`.

### F. `ExtractComparableValue` — CompoundPrice arm

Returns `"CurrencyCode/UnitCode"` for chain qualifier matching.

### G. `ExtractQualifierSourcePath` — CompoundPrice arm

Returns `CurrencyCode` for symbolic path extraction (interpolated template matching).

### H. `FormatQualifierValue` (ProofEngine.cs) — CompoundPrice arm

Diagnostic formatting: returns `"CurrencyCode/UnitCode"`.

## Follow-up — Frank Review (APPROVED WITH REQUIRED CHANGES)

**Reviewed by:** Frank  
**Follow-up commit:** (see git log)

Addressed all findings:

- **W1 (REQUIRED):** Fixed `ResolveFieldQualifier` fallback ordering — PriceIn fallback now runs *before* implied qualifiers, matching `ResolveQualifierOnAxis` ordering.
- **N1 (NOTE):** Added TODO comment on `ExtractQualifierSourcePath` CompoundPrice arm documenting that interpolated CompoundPrice will need composite `CurrencyCode/UnitCode` source path extraction.
- **N2 (NOTE):** Added two new tests in `QS5_CompoundPriceAxisRouting`: `CompoundPrice_FormatQualifierValue_ReturnsCurrencySlashUnit` and `CompoundPrice_ExtractQualifierSourcePath_ReturnsCurrencyCode`. Added corresponding test entry points in `ProofEngine.Analysis.cs`.

### I. Test helper

`TryProjectCompoundPriceForTest` added to `ProofEngine.Analysis.cs` for unit test access.

## Files Changed

| File | Change |
|------|--------|
| `src/Precept/Pipeline/ProofEngine.Qualifiers.cs` | `TryProjectCompoundPrice` helper + PriceIn fallbacks in 5 qualifier loops + `QualifiersAreCompatible`/`ExtractComparableValue`/`ExtractQualifierSourcePath` arms |
| `src/Precept/Pipeline/ProofEngine.cs` | `FormatQualifierValue` CompoundPrice arm |
| `src/Precept/Pipeline/ProofEngine.Analysis.cs` | `TryProjectCompoundPriceForTest` test entry point |
| `test/Precept.Tests/ProofEngineTests.cs` | 10 new tests in `QS5_CompoundPriceAxisRouting` |

## Test Results

- **10 new QS-5 tests:** all pass
- **Precept.Tests:** 9 pre-existing failures, 5592 passed (no regressions)
- **LanguageServer.Tests:** 6 pre-existing failures, 299 passed (no regressions)
- **Mcp.Tests:** 0 failures, 44 passed (no regressions)

## What Frank Should Watch

1. **Projection creates new qualifier instances.** `TryProjectCompoundPrice` returns fresh `Currency`/`Unit`/`Dimension` records — not the original `CompoundPrice`. This means downstream consumers see canonical subtypes, which is intentional (they compare naturally with non-compound qualifiers). But it also means the original `CompoundPrice` identity is lost after projection. If any future proof strategy needs to know "this Currency came from a CompoundPrice," it won't see that.

2. **Interpolated PriceIn is not handled.** `ResolveQualifierFromInterpolatedConstant` returns `null` for `PriceIn` axis because there's no `InterpolationSlotKind.PriceIn`. This matches the design spec's "interpolated compound is already handled by the proof engine as a dynamic qualifier" note. But it means `price in '{SomeField}'` where SomeField is a compound string won't get qualifier resolution until that's wired.

3. **`CreateQualifierFromSlotExpression` has no `PriceIn` arm.** Falls through to `_ => null`. Safe for now since PriceIn slots only come from literal typed constants, not interpolation slot expressions.

4. **CompoundPrice compatibility ignores DimensionName.** Two CompoundPrice with same currency+unit but different dimensions are considered compatible. This matches the spec (currency+unit are the identity axes) but worth confirming that's the intent.
