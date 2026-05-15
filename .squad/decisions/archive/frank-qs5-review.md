# QS-5 Code Review — CompoundPrice Axis Routing in ProofEngine

**Reviewer:** Frank  
**Date:** 2025-07-25  
**Commits:** c020d1de (implementation), 2d1ce55c (decision doc)  
**Verdict:** APPROVED WITH REQUIRED CHANGES

**Summary:** 0 blockers, 1 required change, 3 notes

---

## Required Change

### W1. PriceIn fallback ordering inconsistency between `ResolveQualifierOnAxis` and `ResolveFieldQualifier`

**Severity:** Required change — silent wrong-answer risk in edge cases.

In `ResolveQualifierOnAxis` (ProofEngine.Qualifiers.cs lines 323–337), the inline field-resolution path runs PriceIn fallback **before** implied qualifiers:

```
PriceIn fallback       ← line 323–328
Implied qualifiers     ← line 330–337
```

In `ResolveFieldQualifier` (lines 696–705), the ordering is reversed:

```
Implied qualifiers     ← line 696–698
PriceIn fallback       ← line 700–705
```

Both methods resolve the same data (a field's qualifier on an axis). A `price in 'USD/kg'` field reached via `ResolveQualifierOnAxis` will find the projected `Currency("USD")` before any implied qualifier. The same field reached via `ResolveQualifierFromExpression` → `ResolveFieldQualifier` will check implied qualifiers first.

**Correct ordering:** PriceIn before implied. An explicit `CompoundPrice` declared on a field is stronger than a type-level implied qualifier. `ResolveQualifierOnAxis` has the right ordering; `ResolveFieldQualifier` should match it.

**Fix:** In `ResolveFieldQualifier`, move the PriceIn fallback block (lines 700–705) above the implied-qualifier block (lines 696–698).

**Current risk:** Low in practice — `price` types likely have no implied qualifiers today. But this is the proof engine; inconsistent resolution order is a structural defect that will produce wrong answers silently when it does bite.

---

## Notes

### N1. `ExtractQualifierSourcePath` for `CompoundPrice` extracts only `CurrencyCode`

Line 135: `DeclaredQualifierMeta.CompoundPrice { CurrencyCode: var value } => value`. This discards `UnitCode`. If two CompoundPrices share a currency source field but differ in their unit source field, `QualifiersSymbolicallyEqual` would incorrectly consider them equal (when falling back to `ExtractQualifierSourcePath` — the `SourceFieldName` primary check would need to also fail).

**Current risk:** None. Interpolated `CompoundPrice` does not exist yet (George flag #2), so no CompoundPrice will have `{FieldRef}`-style values. When interpolated compound is implemented, this must be revisited — the source path should be `CurrencyCode/UnitCode` or a composite to preserve both components.

### N2. Missing test coverage for `FormatQualifierValue` and `ExtractQualifierSourcePath`

The 10 tests cover: projection (3 axes + empty dimension + non-compound passthrough), compatibility (same, different currency, different unit, cross-subtype), and `ExtractComparableValue`. No test covers:
- `FormatQualifierValue` for `CompoundPrice` → should produce `"'USD/kg'"` format
- `ExtractQualifierSourcePath` for `CompoundPrice` → should exercise the CurrencyCode-only extraction path

These are low-risk omissions (the code is straightforward pattern-match arms), but for completeness they should be added.

### N3. Projection preserves `Origin`, `Preposition`, `ProofSatisfactions`, and `SourceFieldName`

`TryProjectCompoundPrice` correctly forwards all inherited metadata properties from the source `CompoundPrice` to the projected subtype (lines 722–731). This is the right behavior — projected qualifiers inherit the provenance of the original compound. Noted for the record since this was a specific review concern.

---

## Responses to George's 4 Flagged Items

### G1. "Projection creates fresh instances" — NOT A PROBLEM

`DeclaredQualifierMeta` subtypes are sealed records. The `==` operator and `Equals()` use structural (value-based) equality, not reference equality. Line 71 of `QualifiersAreCompatible` (`leftQualifier == rightQualifier`) will correctly match a projected `Currency("USD")` against any other `Currency("USD")`. No consumer in the proof engine uses `ReferenceEquals` on qualifier metas — the `ReferenceEquals` calls in `ProofEngine.cs` and `ProofEngine.Analysis.cs` are on `ParameterMeta` and constant-fold sentinels, not qualifier metas.

### G2. "Interpolated PriceIn not handled" — ACCEPTABLE DEFERRAL

`ResolveQualifierFromInterpolatedConstant` (line 483–491) maps `QualifierAxis.PriceIn` to `null` in the `targetSlot` switch (falls through `_ => null`). The `StaticQualifier` path (lines 456–480) handles `StaticCurrencyAndUnitQualifier` which is the static-compound case — so static interpolated compounds ARE handled. Only fully dynamic interpolated PriceIn (`price in '{SomeField}'` where SomeField is a compound string) is deferred. This is correct scoping for QS-5.

### G3. "`CreateQualifierFromSlotExpression` has no PriceIn arm" — NOT BLOCKING

`CreateQualifierFromSlotExpression` (line 558+) is only reached from interpolation slot processing. Since no `InterpolationSlotKind.PriceIn` exists, no slot will reach this method with a PriceIn axis. The `_ => null` fallthrough is safe. When `InterpolationSlotKind.PriceIn` is added, this will need a corresponding arm, but that's part of the interpolated-compound follow-up, not QS-5.

### G4. "Compatibility ignores DimensionName" — CORRECT BEHAVIOR

Two `CompoundPrice` qualifiers with the same `CurrencyCode` + `UnitCode` but different `DimensionName` are considered compatible. This is correct: `DimensionName` is derived from `UnitCode` via `UcumParser` + `UnitDimensionHelper`. If `UnitCode` matches, `DimensionName` must also match (the derivation is deterministic). Including `DimensionName` in the comparison would be redundant. The only way they could differ is if the derivation had a bug, in which case the compatibility check should not be the place to catch it.

---

## Summary of `TryProjectCompoundPrice` Correctness

| Target Axis | Projected Subtype | Source Fields Used | Correct? |
|---|---|---|---|
| `Currency` | `Currency(CurrencyCode)` | `CurrencyCode`, `Origin`, `Preposition`, `ProofSatisfactions`, `SourceFieldName` | ✅ |
| `Unit` | `Unit(UnitCode, DimensionName)` | `UnitCode`, `DimensionName`, `Origin`, `Preposition`, `ProofSatisfactions`, `SourceFieldName` | ✅ |
| `Dimension` | `Dimension(DimensionName)` | `DimensionName` (guarded by non-empty check), `Origin`, `Preposition`, `ProofSatisfactions`, `SourceFieldName` | ✅ |
| Other | `null` | — | ✅ |

The method is called in all 6 qualifier resolution paths (3 in `ResolveQualifierOnAxis`, 2 in `ResolveQualifierFromExpression`, 1 in `ResolveFieldQualifier`) as a last-resort fallback after standard axis matching and dimension fallbacks. It is NOT called in `QualifiersAreCompatible` (correct — compatibility handles CompoundPrice directly), `FormatQualifierValue` (correct — formats the compound as-is), or `ExtractComparableValue` (correct — extracts the composite `CurrencyCode/UnitCode` identity).

---

## Test Entry Point Pattern

`TryProjectCompoundPriceForTest` in `ProofEngine.Analysis.cs` follows the established codebase pattern — `Analysis.cs` already exposes `QualifiersAreCompatibleForTest`, `QualifiersSymbolicallyEqualForTest`, `GetImpliedQualifierOnAxis`, and similar internal helpers via `internal static` methods. The naming convention and placement are consistent.
