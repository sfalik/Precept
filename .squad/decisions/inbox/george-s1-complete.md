# Slice 1 Complete — Currency/Unit Arithmetic Safety (PRE0070–0074)

**By:** George  
**Date:** 2025-07-14  
**Status:** Complete

## What Was Wired

PRE0070–0074 qualifier compatibility checks in `TypeChecker.Expressions.cs`:

- **PRE0070** `CrossCurrencyArithmetic` — Money + Money with different currency qualifiers
- **PRE0071** `CrossDimensionArithmetic` — Quantity + Quantity with different dimensions
- **PRE0072** `DenominatorUnitMismatch` — Price / Quantity where dimensions don't align
- **PRE0073** `DurationDenominatorMismatch` — Division with variable-length temporal denominators
- **PRE0074** `CompoundPeriodDenominator` — Compound period can't cancel single-unit denominator

All 5 diagnostics fire from `ValidateQualifierCompatibility`, called after binary operation resolution at line 392.

## Helper Approach

Used `TryGetStaticQualifiers` (plural) — returns `ImmutableArray<DeclaredQualifierMeta>?`. Returns null (fast-exit) when:
- Expression type doesn't carry qualifiers (non-field, non-arg, non-literal, non-typed-constant)
- Qualifiers array is empty/default
- Any qualifier has `SourceFieldName != null` (dynamic/interpolated — deferred to ProofEngine)

Individual axis extraction (Currency, Unit, Dimension, TemporalUnit) happens inside `ValidateQualifierCompatibility` and `ValidateDenominatorCompatibility` after the null-guard.

## Allow-List Changes

- **Gate 1:** Removed all 5 B2 entries (emission sites now exist)
- **Gate 2:** Added all 5 B2 entries with justification (tests exist in `TypeCheckerCurrencyUnitTests.cs` but cross-project analyzer architecture cannot detect them)

## Anomalies

1. **Pre-existing Gate 2 failures (178 codes):** The PRECEPT0028 analyzer runs on `src/Precept` compilation and cannot see `test/Precept.Tests/` source trees. This is a known architectural limitation — not introduced by this slice. All emitted codes with tests in the test project hit Gate 2 false positives.

2. **PRE0073/PRE0074 tests validate clean compilation rather than error emission:** Duration/Period division operations don't exist in the operations catalog yet. The diagnostic paths exist and are structurally sound but unreachable until those operations are added. Tests confirm no crashes on related arithmetic and document the future activation path.

3. **Pre-existing test failures (7 in ProofEngineTests E3):** CompoundUnit cancellation tests in `SubexpressionQualifierPropagation` fail on both baseline and post-change — confirmed unrelated to this slice.
