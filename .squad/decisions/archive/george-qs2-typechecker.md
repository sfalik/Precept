# QS-2: TypeChecker PriceIn Wiring — Decision Record

**Author:** George  
**Date:** 2026-05-15  
**Commit:** `88522c02`  
**Follow-up commit:** *(see below — Frank review fixes)*

## What changed and why

### A. `Types.cs` — `QS_CurrencyAndDimension` update
Changed the `in` slot from `QualifierAxis.Currency` to `QualifierAxis.PriceIn` and set `OfRequiresCurrencyIn: true`. Also updated `RequiredBoundQualifierAxes` from `[Currency]` to `[Currency, Unit, PriceIn]` so that bounds validation recognizes all three resolution forms.

### B. `TypeChecker.cs` — `MapPriceInQualifier`
New method that disambiguates the polymorphic `in` value on price fields:
- `"USD"` → `DeclaredQualifierMeta.Currency` (existing subtype, axis stays `Currency`)
- `"USD/kg"` → `DeclaredQualifierMeta.CompoundPrice` (new subtype, axis `PriceIn`)
- `"kg"` → `DeclaredQualifierMeta.Unit` (existing subtype, axis stays `Unit`)
- Unrecognized → emits `InvalidCurrencyCode` and returns null

### C. `ExtractQualifiers` wiring
Added `QualifierAxis.PriceIn` arm to `MapLiteralQualifier` switch. Added `PriceIn` to `MapInterpolatedQualifier` (resolves as `Currency` for interpolated forms since runtime can't do compound disambiguation statically).

### D. `OfRequiresCurrencyIn` enforcement
After all qualifiers are resolved in `ExtractQualifiers`, checks if `shape.OfRequiresCurrencyIn` is true and the `in` slot resolved to something other than `Currency`. If an `of` qualifier is also present, emits `InvalidQualifierCoexistence` (PRE0139).

### E. Hover/completion labels
Added `PriceIn` arms to all 5 qualifier display methods in `RichHoverFactory.cs`. Added `CompoundPrice` arm to `GetQualifierRawValue` and the `QualifierHoverInfo` overloads.

### F. MCP formatter
Updated `RenderQualifierShape` to append `OfRequiresCurrencyIn` note. Added `CompoundPrice` arm to `RenderImpliedQualifier`.

### G. Completion handler
Added `PriceIn` → `TypeKind.Currency` mapping in `TryMapQualifierAxisToExpectedType`.

## Surprising things found

1. **`RequiredBoundQualifierAxes` needed updating.** The design doc didn't mention this, but since `MapPriceInQualifier` returns `Currency`/`Unit`/`CompoundPrice` with different `.Axis` values, the bounds checker couldn't match any of them against the old `[Currency]` list. I expanded it to `[Currency, Unit, PriceIn]` to cover all three resolution forms.

2. **`TypesTests.GetMeta_Price_HasCurrencyAndDimensionQualifiers` was asserting `Currency` on slot 0.** Updated to assert `PriceIn` and added `OfRequiresCurrencyIn` assertion.

3. **Price fields don't accept `'0.00 USD'` as a default value.** The typed constant format for price differs from money. Removed the default from the regression test.

4. **Diagnostic coverage analyzer (PRECEPT0028)** requires a Gate 2 allow-list entry for any new emitted code that has tests in the separate test project.

## Frank's review focus areas

- **`MapPriceInQualifier` disambiguation order**: Currency check comes before unit check. This means if a UCUM unit code happens to collide with a currency code, currency wins. This matches the existing `price in 'USD'` behavior but Frank should confirm this priority is correct.
- **Interpolated PriceIn resolution**: Currently resolves as `Currency` for interpolated forms (`price in '{CatalogCurrency}'`). Compound disambiguation at runtime would need further work.
- **`RequiredBoundQualifierAxes` expansion**: The `[Currency, Unit, PriceIn]` list is broader than before. Frank should verify this doesn't weaken bounds validation in edge cases.
- **No ProofEngine changes**: The proof engine will see `CompoundPrice` qualifiers for the first time. The existing `_ =>` defaults should handle it gracefully, but compound price proof obligations (e.g., cross-dimension checks) may need future work.

## H. Frank Review Follow-Up

Frank reviewed QS-2 and returned APPROVED WITH REQUIRED CHANGES. All findings addressed in follow-up commit:

### B1 — `ResolveDeclarationQualifier` PriceIn fallback
Added PriceIn→Currency/Unit fallback in `RichHoverFactory.cs` so hover resolves correctly for `price in 'USD'` and `price in 'kg'` (not just compound forms).

### B2 — Missing compound+of coexistence test
Added `PriceIn_CompoundWithOf_EmitsInvalidQualifierCoexistence` test to `TypeCheckerPriceInQualifierTests.cs`.

### W1 — `InvalidPriceQualifier` diagnostic
Replaced the misleading `InvalidCurrencyCode` fallback in `MapPriceInQualifier` with a new `InvalidPriceQualifier` diagnostic (PRE0140) that correctly describes all valid forms (currency, unit, compound). Added to DiagnosticCode, Diagnostics catalog, and Gate 2 allow-list.

### W2 — `GetQualifierAxisName` resolution-aware
Updated the `QualifierHoverInfo` overload to show "currency" or "unit" when a PriceIn slot resolves to a specific type, instead of the generic slot description.

### W3 — CompletionHandler TODO
Added TODO comment at the `PriceIn → TypeKind.Currency` mapping acknowledging the polymorphic completion gap.

### N1 — Malformed compound edge-case tests
Added `PriceIn_TrailingSlash_EmitsDiagnostic` and `PriceIn_LeadingSlash_EmitsDiagnostic` tests.

### N2 — MCP `RenderQualifierShape` PriceIn rendering
Extracted `RenderSlotAxisLabel` helper so PriceIn slots render as `` in `currency`, `unit`, or compound `currency/unit` `` instead of opaque `` in `PriceIn` ``.
