### 2026-05-02T18:14:44-04:00: GAP-047 complete
**By:** Frank
**Requested by:** Shane
**What:** Updated `docs/language/precept-language-spec.md` §3.7 to expand `min`, `max`, `abs`, `clamp`, and `round(value, places)` into explicit primitive-numeric, money, and quantity overload rows. Documented same-qualifier requirements, qualifier-preserving results, and clarified that the `round(value, places)` bridge semantics apply only within the primitive numeric lanes.
**Why:** Align the public language spec with the existing `Functions` catalog and close GAP-047 without any code changes.
