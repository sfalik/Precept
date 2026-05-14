# Slice 9B Complete — Typed-Constant Family Catalog

**Date:** 2025-07-14
**By:** George (Runtime Dev)
**Status:** Complete

## What Was Wired

Catalog-mediated diagnostic emission for typed-constant validation failures. Instead of always emitting generic `InvalidTypedConstantContent` (PRE0053) on validation failure, the type checker now reads `FormatErrorCode`/`SemanticErrorCode` from the `ContentValidation` catalog entry and emits the domain-specific code.

## Catalog Shape Chosen

Rather than creating a separate `TypedConstantFamilyMeta` record, the diagnostic pair was added directly to the existing `ContentValidation` abstract record as nullable properties:

```csharp
public abstract record ContentValidation(
    string FormatDescription,
    string[] Examples,
    DiagnosticCode? FormatErrorCode = null,
    DiagnosticCode? SemanticErrorCode = null);
```

This is architecturally correct because `ContentValidation` already IS the per-family metadata — it's the discriminated union that identifies validation strategy. Adding diagnostic codes to it makes the catalog the single source of truth for "what code to emit when this family's validation fails."

A `TypedConstantErrorKind` enum (Format/Semantic) was added to `TypedConstantDiagnostic` and `TemporalDiagnostic` to let validators communicate WHICH kind of failure occurred, enabling the type checker to select the right catalog code.

## Codes Now Catalog-Mediated

| Family | FormatErrorCode | SemanticErrorCode |
|--------|----------------|-------------------|
| Date | PRE0056 `InvalidDateFormat` | PRE0055 `InvalidDateValue` |
| Time | — | PRE0057 `InvalidTimeValue` |
| DateTime | PRE0056 `InvalidDateFormat` | PRE0055 `InvalidDateValue` |
| Instant | PRE0058 `InvalidInstantFormat` | — |
| Timezone | — | — (PRE0059 remains unwired pending Timezone-specific validation refactor) |
| Duration/Period | — | — |
| Currency/Unit/Regex/ClosedSet/etc. | — | — (fall back to PRE0053) |

## Selection Logic

```
SelectDiagnosticCode(validation, errorKind):
  Semantic → SemanticErrorCode ?? FormatErrorCode ?? PRE0053
  Format   → FormatErrorCode ?? SemanticErrorCode ?? PRE0053
```

Fallback chain ensures families with only one code still get domain-specific emission regardless of error kind classification.

## Anomalies

1. **PRE0059 (InvalidTimezoneId) not wired.** The timezone validation path uses a different discriminator (`TemporalLiteralKind.Timezone`) that maps to the same `NodaTimeValidation` pattern but through a different `TypeKind` (Timezone). Its catalog entry currently has no format/semantic codes — wiring it requires understanding whether timezone failures are "format" or "semantic" (answer: semantic, since any string is a valid format candidate for timezone lookup). Left for a follow-up.

2. **TemporalParser format/semantic heuristic.** For dates, the distinction uses regex: if input matches `\d{4}-\d{2}-\d{2}` but NodaTime rejects it, it's semantic (invalid date like Feb 30). If it doesn't match the pattern at all, it's format. This is a reasonable heuristic — NodaTime's `ParseResult` doesn't expose failure reason categorization.

3. **Pre-existing test failures (24).** These are in ProofRequirementCatalog, F5TempVerify samples, and CurrencyUnitTests — all pre-existing and unrelated to this slice.
