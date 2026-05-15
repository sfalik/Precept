# Frank — QS-5 Targeted Re-Review

**Commits:** `4d0a758a`, `f5613c87`
**Verdict:** APPROVED

## W1 — PriceIn fallback ordering in `ResolveFieldQualifier`

**Status: Fixed correctly.**

`ResolveFieldQualifier` (line 698) now follows this resolution order:
1. Exact axis match on `DeclaredQualifiers`
2. Unit→Dimension fallback
3. Dimension→TemporalDimension fallback
4. **PriceIn fallback via `TryProjectCompoundPrice`** (line 717–721)
5. Implied qualifiers from `Types.GetMeta` (line 723–725)

The PriceIn block is definitively before implied qualifiers, with the comment on line 716 explicitly documenting the intent: "checked before implied qualifiers — explicit CompoundPrice is stronger than type-level implied."

This ordering matches `ResolveQualifierOnAxis` (line 191+), which uses the same sequence within each subject branch (argQualifiers, tcQualifiers): exact → Unit→Dim → Dim→TemporalDim → PriceIn projection.

## N1 — `ExtractQualifierSourcePath` CompoundPrice composite arm

**Status: Fixed correctly.**

The arm at line 135 now produces `$"{cp.CurrencyCode}/{cp.UnitCode}"` — a composite path with both components.

`StripInterpolationBraces` (line 156–168) is correctly extracted and handles:
- Non-interpolated values (no braces) → returns `null` (no transformation needed)
- Interpolated values like `{Currency}` → strips braces, returns `Currency`
- Dotted paths like `{field.Sub}` → returns prefix before dot

The composite path handling (lines 142–150) splits on `/`, strips each component independently, and reconstructs. The fallback logic on line 148–149 correctly preserves the original component if `StripInterpolationBraces` returns null (meaning no braces to strip).

The test assertion `"Currency/kg"` is correct: `{Currency}` strips to `Currency`, `kg` has no braces so stays as-is.

## N2 — Tests for `FormatQualifierValue` and `ExtractQualifierSourcePath`

**Status: Fixed correctly.**

Two new `[Fact]` tests at lines 5382 and 5392:
- `CompoundPrice_FormatQualifierValue_ReturnsCurrencySlashUnit` — constructs `CompoundPrice("USD", "kg", "mass")`, asserts formatted output is `"'USD/kg'"`. Matches `FormatQualifierValue` arm at line 187.
- `CompoundPrice_ExtractQualifierSourcePath_ReturnsComposite` — constructs `CompoundPrice("{Currency}", "kg", "mass")`, asserts source path is `"Currency/kg"`. Exercises both the composite arm and `StripInterpolationBraces`.

Test entry points in `Analysis.cs` (lines 170–176) follow the existing `ForTest` suffix pattern (consistent with `TryProjectCompoundPriceForTest` at line 165) and use the `internal` access modifier.

## New Issues

None found. The `StripInterpolationBraces` extraction is clean — it's a pure function with no side effects, correctly scoped as `private static`, and the composite path reconstruction handles all edge cases (both components interpolated, one interpolated, neither interpolated).
