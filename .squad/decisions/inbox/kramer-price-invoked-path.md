# Decision: invoked slash-slot completions for price and exchange rate

**Author:** Kramer  
**Date:** 2026-05-17

## Summary

Ctrl+Space inside `price` / `exchangerate` typed constants must not reuse the after-amount pair list once the caret has moved into the slash-delimited suffix.

- Before `/`, return the ISO currency catalog.
- After `/` in `price`, return the quantity/UCUM unit catalog (dimension-filtered when qualifiers allow it).
- After `/` in `exchangerate`, return the ISO currency catalog.

## Root cause

The invoked path already reached `GetPriceLiteralItems` / `GetExchangeRateLiteralItems`, but the slot handlers only looked at coarse phase (`AfterNumberSpace` / `UnitTyping`) and ignored which side of `/` the caret was on. That made Ctrl+Space inside `USD/each` and `USD/EUR` behave like the space-trigger path after the amount and offer whole-pair suffix items.

## Durable rule

For structured typed constants with internal separators, invoked completions need a second layer of caret-position routing inside the literal. Phase detection alone is not enough once multiple semantic slots share the same token.