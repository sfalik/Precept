# QS-2 Review — TypeChecker + Catalog Wiring

**Commits:** `88522c02`, `1daaefec`
**Reviewer:** Frank
**Verdict:** APPROVED WITH REQUIRED CHANGES

## Findings

### [B1]: `ResolveDeclarationQualifier` cannot find Currency/Unit resolutions for PriceIn slot — hover silently degrades

`RichHoverFactory.ResolveDeclarationQualifier` (line 1823) searches `declaredQualifiers` for a meta whose `.Axis == axis`, where `axis` is the *parsed qualifier's* axis — `PriceIn` for the `in` slot on price. But `MapPriceInQualifier` returns `DeclaredQualifierMeta.Currency` (`.Axis = Currency`) for `price in 'USD'` and `DeclaredQualifierMeta.Unit` (`.Axis = Unit`) for `price in 'kg'`. Neither matches `PriceIn`, so `ResolveDeclarationQualifier` returns `null` for the two most common price qualifier forms.

Only `CompoundPrice` (`.Axis = PriceIn`) is found. This means hover for `price in 'USD'` and `price in 'kg'` silently falls back to unresolved display — no proof card, no constraint status, no qualifier source description.

**Severity:** Blocking. Hover is a core UX surface and this is a regression for the two most common cases.

**File:** `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`, line 1823–1834.

**Fix:** Add a `PriceIn` fallback in `ResolveDeclarationQualifier`, analogous to the existing Unit→Dimension and Dimension→TemporalDimension fallbacks:

```csharp
if (axis == QualifierAxis.PriceIn)
{
    foreach (var qualifier in declaredQualifiers)
    {
        if (qualifier.Axis is QualifierAxis.Currency or QualifierAxis.Unit)
            return qualifier;
    }
}
```

---

### [B2]: Missing test for compound + `of` coexistence (`price in 'USD/kg' of 'mass'`)

The spec (§3 Change 3, §4b.iii) explicitly defines three `OfRequiresCurrencyIn` error cases: unit + of, compound + of, and currency + of (valid). The test file covers unit + of and currency + of but **omits compound + of**. This is one of only three paths through the enforcement logic — if the `CompoundPrice` arm in the `inValue` switch (line 157) has a typo or the pattern match is wrong, no test catches it.

**Severity:** Blocking. The compound + of case is a distinct control flow path (line 157) and must be tested.

**File:** `test/Precept.Tests/TypeChecker/TypeCheckerPriceInQualifierTests.cs`

**Fix:** Add:
```csharp
[Fact]
public void PriceIn_CompoundWithOf_EmitsInvalidQualifierCoexistence()
{
    var precept = """
        precept Product
        field Cost as price in 'USD/kg' of 'mass'
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidQualifierCoexistence);
}
```

---

### [W1]: Fallback diagnostic for unrecognized PriceIn value emits `InvalidCurrencyCode` — spec says `InvalidPriceQualifier`

The spec (§4b.i, step 4) calls for an `InvalidPriceQualifier` diagnostic when the value is neither currency, unit, nor compound. The implementation (line 412) emits `InvalidCurrencyCode` instead. The diagnostic code `InvalidPriceQualifier` does not exist.

For a user who writes `price in 'foo'`, the error "Invalid currency code 'foo'" is misleading — it implies only currencies are valid, when units are equally valid. The message should reflect the polymorphic nature of the slot.

**Severity:** Warning. Not a correctness bug (the error is still emitted and the field is correctly rejected), but a UX regression and a spec deviation. Can ship as-is with a follow-up issue.

**File:** `src/Precept/Pipeline/TypeChecker.cs`, line 412.

---

### [W2]: `GetQualifierAxisName(QualifierHoverInfo)` uses slot axis (`PriceIn`) for resolved Currency/Unit

Even after B1 is fixed, `GetQualifierAxisName(QualifierHoverInfo info)` at line 2051 falls through to `GetQualifierAxisName(info.Axis)` for resolved `Currency` and `Unit` metas. Since `info.Axis` is the parsed qualifier's axis (`PriceIn`), the hover label says "currency, unit, or compound" instead of the specific "currency" or "unit" that was actually resolved.

**Severity:** Warning. The hover is technically correct (it describes the *slot*) but suboptimal (it should describe the *resolved value*). Deferrable to a polish pass.

**File:** `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`, lines 2051–2056.

**Fix:** Add resolution-aware arms:
```csharp
private static string GetQualifierAxisName(QualifierHoverInfo info) => info.ResolvedQualifier switch
{
    DeclaredQualifierMeta.TemporalDimension or DeclaredQualifierMeta.TemporalUnit => GetQualifierAxisName(info.ResolvedQualifier.Axis),
    DeclaredQualifierMeta.CompoundPrice => "compound price",
    DeclaredQualifierMeta.Currency when info.Axis == QualifierAxis.PriceIn => "currency",
    DeclaredQualifierMeta.Unit when info.Axis == QualifierAxis.PriceIn => "unit",
    _ => GetQualifierAxisName(info.Axis),
};
```

---

### [W3]: `CompletionHandler` maps `PriceIn → Currency` only — unit completions not offered

`TryMapQualifierAxisToExpectedType` (line 985) maps `PriceIn → TypeKind.Currency`. Users typing `price in '<cursor>'` see only ISO 4217 completions, never UCUM unit options. George's decision record acknowledges this as a pragmatic choice, but there is no TODO in the code.

**Severity:** Warning. Non-blocking; the completion is correct for the majority case (currency-only price) but incomplete. Add a code-level TODO for follow-up.

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`, line 985.

---

### [N1]: No edge-case tests for malformed compounds or unrecognized values

Missing tests for:
- `price in 'USD/'` — trailing slash, compound guard rejects (slashIndex not `< value.Length - 1`), falls to currency check, fails, UCUM fails, emits `InvalidCurrencyCode`
- `price in '/kg'` — leading slash, compound guard rejects (slashIndex not `> 0`), falls to currency, fails, UCUM parse of `/kg` likely fails, emits `InvalidCurrencyCode`
- `price in 'XYZ/unknown'` — invalid currency + invalid unit in compound, two diagnostics emitted, CompoundPrice with empty `DimensionName` returned
- `price in 'NOTACODE'` — completely unrecognized token

These are all handled (diagnostics emitted, method returns null or degraded meta), but the behavior is not pinned by tests.

**Severity:** Note. Non-blocking — happy paths are covered. Recommend adding at least `'USD/'` and `'/kg'` to pin the boundary conditions of the compound guard.

**File:** `test/Precept.Tests/TypeChecker/TypeCheckerPriceInQualifierTests.cs`

---

### [N2]: `MCP RenderQualifierShape` renders slot as `` in `PriceIn` `` — opaque to MCP consumers

The spec (§4d) recommends rendering PriceIn slots as `` in `currency`, `unit`, or compound `currency/unit` ``. The implementation renders `` in `PriceIn` `` (the raw axis enum name via `$"{RenderToken(slot.Preposition)} `{slot.Axis}`"`), with an appended note about `OfRequiresCurrencyIn`. This is technically correct but less helpful than the spec suggests.

**Severity:** Note. The `OfRequiresCurrencyIn` clause is rendered, which is good. The slot rendering is a UX polish item.

**File:** `tools/Precept.Mcp/CatalogFormatters.cs`, line 811.

---

### [N3]: PRE0139 numbering is correct and well-placed

`InvalidQualifierCoexistence = 139` follows sequentially after `CountDimensionBoundsAmbiguous = 138` in the TypeChecker diagnostic range. Related codes (`MutuallyExclusiveQualifiers`, `QualifierMismatch`) are correctly referenced. No numbering gaps or conflicts.

**Severity:** Note (positive). No action needed.

---

### [N4]: Gate 2 allow-list entry is appropriate

`InvalidQualifierCoexistence` is correctly placed in the Gate 2 allow-list because the test lives in `Precept.Tests` (a separate project from the analyzer's scan scope). The comment block at line 87–88 explains this cross-project detection gap. Entry is consistent with all other entries in the list.

**Severity:** Note (positive). No action needed.

---

## Spec Conformance

| # | Check | Result |
|---|-------|--------|
| 1 | `QS_CurrencyAndDimension` `in` slot changed to `PriceIn` | ✅ Matches §3 Change 4 |
| 2 | `OfRequiresCurrencyIn: true` on shape | ✅ Matches §3 Change 3 |
| 3 | `MapPriceInQualifier` disambiguation order (compound → currency → unit) | ✅ Matches §4b.i (except see W1) |
| 4 | `MapPriceInQualifier` uses catalog registries, not hardcoded strings | ✅ Uses `CurrencyCatalog.All`, `UcumParser.Parse`, `UnitDimensionHelper` |
| 5 | `CompoundPrice` carries `CurrencyCode`, `UnitCode`, `DimensionName` | ✅ Matches §3 Change 2 |
| 6 | `OfRequiresCurrencyIn` enforcement fires for non-Currency `in` + `of` | ✅ Matches §4b.iii |
| 7 | Interpolated PriceIn resolves as Currency | ✅ Matches §4b.ii (minimal safe option) |
| 8 | `RequiredBoundQualifierAxes` expanded to `[Currency, Unit, PriceIn]` | ✅ Covers all three resolution forms |
| 9 | Hover arms for `PriceIn` and `CompoundPrice` added | ⚠️ Arms present but B1 prevents resolution lookup for Currency/Unit |
| 10 | Completion maps `PriceIn → Currency` | ⚠️ Pragmatic first pass per spec §4c, but no TODO (W3) |
| 11 | MCP formatter renders `OfRequiresCurrencyIn` | ✅ |
| 12 | MCP formatter renders `CompoundPrice` qualifier | ✅ |
| 13 | Fallback diagnostic for unrecognized value | ⚠️ Emits `InvalidCurrencyCode`, spec says `InvalidPriceQualifier` (W1) |
| 14 | Compound + of test coverage | ❌ Missing (B2) |

## Summary

**APPROVED WITH REQUIRED CHANGES** — 2 blockers, 3 warnings, 4 notes.

The core `MapPriceInQualifier` disambiguation logic is correct and properly metadata-driven. The `OfRequiresCurrencyIn` enforcement is clean. The two blockers are:

1. **B1** — `ResolveDeclarationQualifier` in hover factory cannot find Currency/Unit resolutions for PriceIn-axis parsed qualifiers, silently degrading hover for `price in 'USD'` and `price in 'kg'`. Fix: add PriceIn fallback.
2. **B2** — Missing test for `price in 'USD/kg' of 'mass'` → `InvalidQualifierCoexistence`. This is a distinct code path that must be pinned.

Warnings W1–W3 are non-blocking and can be addressed in a follow-up.
