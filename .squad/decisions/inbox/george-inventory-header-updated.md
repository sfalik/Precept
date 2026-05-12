# Decision: inventory-item header updated

**Date:** 2026-05-12T09:02:45.968-04:00  
**By:** George  
**Scope:** `samples/inventory-item.precept` header comments on `spike/Precept-V2-Radical`

## Summary

Removed the stale ROOT CAUSE 1 / ROOT CAUSE 2 header entries from `samples/inventory-item.precept`. Those compiler blockers are already implemented.

## Current Blocker

BUG-A remains, but the blocker is now sample-side rather than compiler-side: the sample still declares `Rate as exchangerate` without `in '{SupplierCurrency}' to '{CatalogCurrency}'`. That sample edit is pending Frank's sign-off.

## Notes

- Kept the `THIS FILE DOES NOT COMPILE` banner unchanged.
- Kept the `SAMPLE DESIGN ISSUES` section unchanged.
- Kept the analysis reference line unchanged.
- No tests run; comment-only change.
