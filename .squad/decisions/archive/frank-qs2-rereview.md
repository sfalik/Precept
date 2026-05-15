# QS-2 Re-Review — Frank

**Commit:** `2317ee28`  
**Date:** 2026-05-16  
**Verdict:** APPROVED with nit

## Finding-by-Finding Verification

### B1 — `ResolveDeclarationQualifier` PriceIn fallback ✅

`RichHoverFactory.cs` adds a `PriceIn` block that searches declared qualifiers for `Currency` or `Unit` axis matches. Semantically correct — PriceIn resolves to one of these concrete axes after type checking, and hover needs to find the resolved qualifier. Matches my prescription exactly.

### B2 — Missing compound+of coexistence test ✅

`PriceIn_CompoundWithOf_EmitsInvalidQualifierCoexistence` added. Precept syntax `price in 'USD/kg' of 'mass'` correctly expects `InvalidQualifierCoexistence`. Test method name, diagnostic code, and DSL syntax are all correct.

### W1 — `InvalidPriceQualifier` (PRE0140) ✅

- `DiagnosticCode.cs`: `InvalidPriceQualifier = 140`, no numbering gap (follows 139). XML doc is accurate.
- `Diagnostics.cs`: Message template covers all three valid forms (currency, unit, compound). Fix hint, recovery steps, related codes (`InvalidCurrencyCode`, `InvalidUnitString`), and examples are all well-formed.
- `TypeChecker.cs`: Fallback correctly changed from `InvalidCurrencyCode` → `InvalidPriceQualifier` at the right call site (end of `MapPriceInQualifier` after currency and unit checks both fail).

### W2 — Resolution-aware `GetQualifierAxisName` overload ✅

Two new arms in the `QualifierHoverInfo` switch: `Currency when PriceIn → "currency"`, `Unit when PriceIn → "unit"`. Pattern guard on `info.Axis` correctly distinguishes PriceIn-resolved qualifiers from native Currency/Unit qualifiers. Semantically correct.

### W3 — CompletionHandler TODO ✅

Two-line TODO comment placed directly above the `PriceIn → TypeKind.Currency` mapping. Clearly states the polymorphic gap and that it's a pragmatic first pass. Matches intent.

### N1 — Edge-case tests for `'USD/'` and `'/kg'` ✅

- `PriceIn_TrailingSlash_EmitsDiagnostic` — expects `InvalidPriceQualifier`. Correct.
- `PriceIn_LeadingSlash_EmitsDiagnostic` — expects `InvalidPriceQualifier`. Correct.
- Comments explain the resolution path (compound guard rejects → currency fails → unit fails → `InvalidPriceQualifier`). Helpful for future readers.

### N2 — MCP PriceIn slot label ✅

`RenderSlotAxisLabel` helper renders PriceIn as `` `currency`, `unit`, or compound `currency/unit` `` — human-readable, accurate, distinct from the opaque `` `PriceIn` `` that other axes use. Clean extraction.

## Allow-List Indentation Nit

`DiagnosticCoverageAllowLists.cs` line 145: `"InvalidQualifierCoexistence"` lost 4 spaces of indentation during the insertion. It has 4 spaces instead of 8. Pure formatting — no behavioral impact. The entry is in the correct alphabetical position.

## Swept-In Files Assessment

Three non-source files were included via `git add -A`:

1. **`.squad/agents/frank/history.md`** — appends my own QS-1 review history entries. Benign metadata.
2. **`.squad/decisions/inbox/george-qs2-typechecker.md`** — appends Section H documenting the review follow-up. Benign metadata.
3. **`docs/Working/quantity-normalization-design.md`** — removes a speculative "Phase 3 — Runtime Quantity Ingress Normalization" section that referenced unimplemented types. This is a cleanup of aspirational content per George's earlier B22 note. Benign and correct.

No source code was swept in. All three changes are documentation/metadata.

## All Tests Pass

9/9 `TypeCheckerPriceInQualifierTests` pass (6 pre-existing + 3 new).

## Verdict

**APPROVED.** All seven findings (B1, B2, W1, W2, W3, N1, N2) are addressed correctly and match the prescribed fixes. One indentation nit in the allow list (line 145) — cosmetic only, no behavioral impact.
