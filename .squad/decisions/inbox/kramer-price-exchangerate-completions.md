# Decision: price + exchange rate typed-constant completions

**Author:** Kramer  
**Date:** 2026-05-17

## Summary

Price and exchange-rate typed-constant completions stay pattern-aligned with the existing money and quantity handlers:

- `price` now prioritizes `CompoundPrice` -> `Currency + Dimension` -> `Currency` -> generic template.
- `price` slot completions only hard-prefix dimension-filtered `currency/unit` items when both currency and dimension qualifiers are literal; field-qualified slot contexts still fall back to example stripping.
- `exchangerate` gets explicit handlers for both the quote-trigger and the amount-space slot phase instead of falling through to free-form/examples.

## Durable rule

Any structured typed-constant type with slot vocabulary must be wired in both completion dispatch points:
1. the `'` trigger type switch (`GetTypedConstantItems`)
2. the space-trigger slot switch in `GetCompletions`

If only one path is wired, the feature is half-shipped.
