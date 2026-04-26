---
updated_at: 2026-04-25T20:00:00Z
focus_area: O1/I23/I32 complete — O2/O3 definitively closed per docs — next: X-items (test gaps)
active_issues: []
---

# What We're Focused On

**Branch:** `precept-architecture` — v1 purge complete, v2 is the only implementation.

**Status (2026-04-25):** All C, M, I, and O items resolved. Two commits landed this session:
- `db9961d` — I-items bulk (39 files, 2116 tests)
- `572c40b` — O1, I23, I32 + stale test corrections (1624 tests)

**O-item outcomes:**
- ✅ O1: `ZonedDateTimePlusPeriod` / `ZonedDateTimeMinusPeriod` — implemented
- ❌ O2: `DurationTimesDecimal` — **explicitly excluded** by docs (line 511, 1144): *"Durations can only be multiplied by whole numbers or `number`."* NodaTime has no `Duration * decimal` operator.
- ❌ O3: `PeriodTimesInteger` / `PeriodDivideInteger` — **explicitly excluded** by docs (lines 600, 663, 1139, 1569): *"Periods can't be multiplied. Write the value directly."*

**Remaining work in priority order:**

1. **X2–X22 (test gaps):** Soup Nazi's full test gap list. Snapshot tests, exhaustive matrix tests, cross-catalog validity tests.

2. **T1–T13 (tooling/grammar):** TextMate grammar sync, LS completions, semantic tokens — depends on catalog metadata now in place.

3. **N1–N5 (nits):** Cosmetic/naming cleanup.

4. **I33 (modifier requirement for choice ordering):** Metadata for choice ordering operations — minor, not started.

**Design specs in inbox (completed, not yet purged):**
- `frank-i23-any-type-target.md` — ✅ done
- `frank-i32-dimension-proof.md` — ✅ done
