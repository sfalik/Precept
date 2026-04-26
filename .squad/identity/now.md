---
updated_at: 2026-04-25T18:00:00Z
focus_area: catalog-review-2 I-items complete — next: O-items (missing operations), X-items (test gaps)
active_issues: []
---

# What We're Focused On

**Branch:** `precept-architecture` — v1 purge complete, v2 is the only implementation.

**Status (2026-04-25):** All C, M, and I items from catalog-review-2 are committed (`db9961d`). 2116 tests passing.

**Remaining work in priority order:**

1. **O1–O3 (missing operations):** `ZonedDateTimePlusPeriod/Minus`, `DurationTimesDecimal`, `PeriodTimesInteger/DivideInteger` — symmetry gaps in the Operations catalog.

2. **I23 (AnyTypeTarget):** Design spec in inbox — breaking change to `ModifiedTypeTarget` DU. Needs TypeChecker audit before implementation.

3. **I32 (DimensionProofRequirement):** Design spec in inbox — unblocks after O1–O3 add the operations that need it.

4. **X2–X22 (test gaps):** Soup Nazi's full test gap list. Snapshot tests, exhaustive matrix tests, cross-catalog validity tests.

5. **T1–T13 (tooling/grammar):** TextMate grammar sync, LS completions, semantic tokens — depends on catalog metadata now in place.

6. **N1–N5 (nits):** Cosmetic/naming cleanup.

**Design specs in inbox:**
- `frank-i23-any-type-target.md` — nullable `TypeTarget.Kind` design
- `frank-i32-dimension-proof.md` — `QualifierCompatibilityProofRequirement` design
