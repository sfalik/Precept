# Sample Completeness — Summary and Sequencing

**Date:** 2026-05-12T01:17:00-04:00
**Author:** Frank (Lead/Architect)
**Scope:** All 30 `.precept` sample files

---

## Findings

**30 samples** analyzed. **20 compile clean. 10 have errors — 46 total diagnostics.**

### By Fix Type

| Category | Count | Files | Effort |
|---------|-------|-------|--------|
| **Sample fix** (DSL correction only) | 10 | 7 files | Small — no compiler changes |
| **Compiler fix** (proof engine) | 9 | 4 files | Medium — F3 slice |
| **Compiler fix** (exchange rate binding) | 27 | 1 file (inventory-item) | Large — F4 slice + design decision |
| **Total** | **46** | **10 files** | |

### Root Causes (4 distinct)

1. **`optional notempty` sample bug** — 8 errors across 6 files. `notempty` and `optional` are mutually exclusive modifiers. Fix: drop `notempty`, keep `optional`. D1 already addressed this exact issue in test fixtures; these are the same bug in samples.

2. **`number`/`decimal` type mismatch** — 2 errors in `travel-reimbursement.precept`. Event args declared as `number`, assigned to `decimal` fields. Fix: change event args to `decimal`.

3. **Static typed constant qualifier extraction** — 9 errors across 4 files. The proof engine's `ResolveQualifierFromExpression()` has no branch for `TypedLiteral` nodes. When comparing `money in 'USD' > '0.00 USD'`, it can resolve the field's qualifier but not the typed constant's qualifier. Fix: add `DeclaredQualifiers` to `TypedLiteral` (populated by type checker), add proof engine branch. **Design question Q2 filed** for node shape decision.

4. **ExchangeRateTimesMoney result qualifier** — 27 errors in `inventory-item.precept`. The operation exists in the catalog but lacks a `ResultQualifierPolicy`. The proof engine cannot determine the result money's currency after exchange rate conversion. Fix: new `CurrencyConversion` policy (or general `ResultQualifierSource`). **Design question Q1 filed** for policy approach.

---

## Sequencing Recommendation

```
F1 (sample: optional notempty)     ← no dependencies, immediate
F2 (sample: number/decimal)        ← no dependencies, immediate
F3 (compiler: static TC qualifier) ← needs Q2 decision, then George
F4 (compiler: ExchangeRate)        ← needs Q1 decision, then George
F5 (recompile audit)               ← after F3+F4, verify cascading clears
```

**F1 + F2 are immediate.** They are pure sample edits — no compiler work. Any team member can execute them. They clear 10 of the 46 diagnostics instantly.

**F3 is the highest-ROI compiler fix.** It clears 9 errors across 4 samples in one proof engine change. It also establishes the pattern for static typed constant qualifier tracking, which benefits the entire qualifier proof surface. Needs Q2 decision (node shape) first.

**F4 is the remaining inventory-item wall.** The 27 remaining inventory-item errors cascade from the missing exchange rate result qualifier binding. This is the largest single fix and needs the Q1 design decision. Once Q1 is decided and F4 lands, many of the 27 errors should clear — but some may reveal additional cascading issues (compound-unit interpolated qualifier proofs), hence F5.

**F5 is a verification pass.** After F3+F4, recompile all 30 samples and confirm zero remaining diagnostics. If any persist, they'll be individually diagnosed and resolved.

---

## After F-Series

If F1–F5 execute as designed, the expected outcome is:
- **30/30 samples compile clean** (minus any residual F5 findings)
- The 16 "deferred exchange rate" PRE0114 errors in inventory-item are resolved
- The 9 "static typed constant" PRE0114 errors across 4 samples are resolved
- All 10 `optional notempty` sample bugs are fixed
- The proof engine has complete qualifier extraction for both static and interpolated typed constants
