# QS-1 Review — Price Qualifier Model Additions

**Commit:** `f391d197`  
**Author:** George  
**Reviewer:** Frank  
**Date:** 2026-05-15  
**Verdict:** **APPROVED**

---

## Findings

### W1: `GetQualifierAxisName` / `GetQualifierDisplayName` — no `PriceIn` arm yet

`RichHoverFactory.cs` lines 2703–2727 enumerate every `QualifierAxis` member with human-readable labels. `PriceIn` falls through to `_ => "qualifier"` / `_ => "Qualifier"`. This is functionally correct — no crash, no wrong answer — but when QS-2 wires `QS_CurrencyAndDimension` to use `PriceIn`, hovering over a `price in '...'` will show the generic "qualifier" label instead of something like "currency, unit, or compound". **Track as QS-2 obligation.** Not blocking.

### W2: `GetQualifierAnnotationLabel` — `PriceIn` falls to default

Line 2024: the annotation label for `PriceIn` would fall to `_ => "qualifier"` instead of `` `in ...` ``. Since `PriceIn` is semantically an `in`-preposition axis, it belongs in the `` `in ...` `` arm. **Same QS-2 scope.** Not blocking.

### W3: `GetQualifierChecksText` / `GetQualifierMismatchText` — generic fallbacks

Lines 2731 and 2742: similar story. `PriceIn` falls to generic text. Not wrong, not blocking, but QS-2 should add explicit arms for all five hover-helper switches.

### W4: No MCP `QualifierAxis` switches found

MCP formatting does not switch on `QualifierAxis` directly — it reads `slot.Axis` for string interpolation. `PriceIn` will render as literal `"PriceIn"` in MCP output, which is opaque but not incorrect. **QS-2 should add the human-readable render branch per the design spec §4d.** Not blocking.

---

## Spec Conformance — Point by Point

| # | Check | Result |
|---|-------|--------|
| 1 | `QualifierAxis.PriceIn` — placement, XML doc | ✅ Placed after `TemporalUnit`, doc says "polymorphic in axis for price", mentions registry disambiguation and `CompoundPrice` xref. Accurate. |
| 2 | `QualifierShape.OfRequiresCurrencyIn: bool` — signature, default, doc | ✅ Default `false` (non-breaking). Doc updated on the `QualifierShape` summary to explain the conditional constraint. Clear guidance for QS-2. |
| 3 | `DeclaredQualifierMeta.CompoundPrice` — params, defaults, base call, doc | ✅ Exact match to spec §3 Change 2: `CurrencyCode`, `UnitCode`, `DimensionName` params; defaulted `Origin`, `Preposition`, `ProofSatisfactions`, `SourceFieldName`; base call wires `QualifierAxis.PriceIn`. XML doc explains compound `in 'currency/unit'` semantics and component accessors. |
| 4 | `Types.cs` NOT changed | ✅ Zero diff on `Types.cs`. `QS_CurrencyAndDimension` still reads `QualifierAxis.Currency` on the `in` slot. Correct — catalog update deferred to QS-2. |
| 5 | Switch exhaustion | ✅ All `QualifierAxis` switches in TypeChecker (3 switches), ProofEngine.Qualifiers (4 switches), RichHoverFactory (7 switches), and CompletionHandler use `_ =>` / `default` fallbacks. `PriceIn` falls through gracefully to null/generic in every case. No stubs needed. |
| 6 | Build & tests | ✅ 5561 passed / 9 failed (Precept.Tests), 305 total / 6 failed (LS.Tests), 44/0 (MCP), 291/2 (Analyzers). All failures are pre-existing — identical to baseline. No regressions introduced. |
| 7 | QS-1 / QS-2 split clean? | ✅ The split is architecturally clean. QS-1 adds the model types with no behavioral wiring. QS-2 updates `QS_CurrencyAndDimension` to use `PriceIn` and wires `MapPriceInQualifier` + `OfRequiresCurrencyIn` enforcement + hover/completion arms + `InvalidQualifierCoexistence` diagnostic. No half-wired state in QS-1. |

---

## Summary

Clean model-only slice. All three additions match the spec exactly. The deferred QS-2 wiring creates no half-states or broken invariants — every downstream consumer fails soft through existing defaults. George's implementation is precise, documented, and non-breaking.

**APPROVED. W1–W4 are tracked obligations for QS-2, not blockers for QS-1.**
