# Decision: qualifier-aware completion filtering audit

**Author:** Kramer  
**Date:** 2026-05-17

## Summary

Qualifier-aware typed-constant completions must honor the declaration's resolved qualifier metadata on every completion path, not just the initial quote-trigger snippet path.

### Audit outcome

- `money in 'USD'` — already correct; added invoked-slot regression coverage.
- `quantity in 'km/h'` — already correct; added invoked-slot regression coverage.
- `quantity of 'mass'` — already correct; added invoked-slot regression coverage.
- `price in 'USD'` — broken on amount-space and slash-slot completion; fixed to keep the currency side pinned.
- `price of 'mass'` — broken on quote/amount-space completion; fixed to emit dimension-filtered unit candidates with an open currency slot.
- `price in 'USD' of 'mass'` — partially correct before (pair suggestions) but broken inside the slash-delimited currency slot; fixed so both slots respect qualifiers.
- `exchangerate in 'USD'` / `to 'CAD'` / both — broken on invoked slash-slot completion; fixed so left/right component completion returns only the declared side when pinned.

## Root cause

`TypedConstantContext` already carried `DeclaredQualifierMeta`, but `GetPriceSlotItems` and `GetExchangeRateSlotItems` treated slash-delimited literals as generic component catalogs once the caret moved inside the suffix. They knew the caret was before or after `/`, but they ignored whether the declaration had already fixed that component. `GetPriceSnippetItems` also lacked a dimension-only branch, so `price of 'mass'` fell back to the generic currency/unit template instead of narrowing the unit vocabulary.

## Durable rule

For structured typed constants, slot routing needs two decisions:

1. **Which component is active?** (`currency`, `unit`, `from`, `to`)
2. **What did the declaration already bind?** (fixed currency, fixed unit, dimension family)

Both decisions must be derived from resolved qualifier metadata (`DeclaredQualifierMeta`, populated from `Types` qualifier shapes and content validation), and both must be applied consistently in the quote-trigger, space-trigger, and invoked completion paths.
