# Advisory Fix Report

**Date:** 2026-05-01
**Authors:** Steinbrenner (PM) + J. Peterman (Brand)
**Subject:** Resolution of PRECEPT050/051 advisories in Candidates 1 and 3

---

## Overview

Two hero candidates carried non-fatal compiler advisories after Soup Nazi's validation run. Both shared the same pattern: an explicit `reject` statement placed in a terminal state (a state with no outgoing non-reject transitions). The compiler correctly identifies this as redundant — the `reject` is the only defined behavior for that state+event pair, so the event can never succeed. Both advisories were resolved by relocating the `reject` to a non-terminal state where it guards a meaningful domain rule.

---

## Candidate 1: Subscription Billing (Ranked #1, Score 29/30)

### Advisory

| Code | Severity | Line | Message |
|------|----------|------|---------|
| PRECEPT050 | hint | 8 | State 'Cancelled' has outgoing transitions but all reject or no-transition — no path forward. |
| PRECEPT051 | warning | 21 | Every transition row for (Cancelled, Activate) ends in reject — the event can never succeed from this state. |

### Root Cause

`from Cancelled on Activate -> reject "Cancelled subscriptions cannot be reactivated"` was the only defined row for the `Cancelled` state. `Cancelled` had no non-reject paths, making it a dead-end state. The `reject` was redundant — the compiler already produces `Undefined` for unhandled event+state pairs.

### Fix Applied

Removed the `Cancelled` reject row. Relocated `reject` to the `Active` state as a downgrade guard: `Activate` is now split into two rows — one for upgrades (price increase or equal), one that rejects attempts to activate a lower-priced plan.

**Before:**
```
from Active on Activate -> set MonthlyPrice = Activate.Price -> no transition
from Active on Cancel -> transition Cancelled
from Cancelled on Activate -> reject "Cancelled subscriptions cannot be reactivated"
```

**After:**
```
from Active on Activate when Activate.Price >= MonthlyPrice -> set MonthlyPrice = Activate.Price -> no transition
from Active on Activate -> reject "Cannot downgrade to a lower plan price"
from Active on Cancel -> transition Cancelled
```

### Compile Confirmation

`precept_compile` returns `valid: true`, `diagnostics: []` (zero errors, zero warnings, zero hints).

### Score Impact

None. All six rubric constructs are preserved: `invariant`, `assert`, `reject`, `when`, transitions, and multiple states. Line count is unchanged. Score remains **29/30**.

---

## Candidate 3: Pull Request Review (Ranked #3, Score 25/30)

### Advisory

| Code | Severity | Line | Message |
|------|----------|------|---------|
| PRECEPT050 | hint | 8 | State 'Merged' has outgoing transitions but all reject or no-transition — no path forward. |
| PRECEPT051 | warning | 20 | Every transition row for (Merged, Submit) ends in reject — the event can never succeed from this state. |

### Root Cause

`from Merged on Submit -> reject "Merged pull requests cannot be reopened"` was the only defined row for the `Merged` state. Same pattern as Candidate 1: `Merged` had no non-reject exits, making the `reject` redundant.

### Fix Applied

Removed the `Merged` reject row. Relocated `reject` to the `Review` state as a premature-merge guard. `Merge` from `Review` now has two rows: one that transitions directly to `Merged` when `ApprovalCount >= 2` (short-circuit path when approval threshold is met), and one that rejects if the threshold has not been reached. `from Approved on Merge -> transition Merged` is preserved for the canonical approval-then-merge flow.

**Before:**
```
from Review on Approve -> set ApprovalCount = ApprovalCount + 1 -> no transition
from Approved on Merge -> transition Merged
from Merged on Submit -> reject "Merged pull requests cannot be reopened"
```

**After:**
```
from Review on Approve -> set ApprovalCount = ApprovalCount + 1 -> no transition
from Review on Merge when ApprovalCount >= 2 -> transition Merged
from Review on Merge -> reject "Pull request has not received enough approvals to merge"
from Approved on Merge -> transition Merged
```

Theme description updated from "you can't reopen what's been merged" to "merged PRs become structurally frozen" — accurate because `Merged` is still terminal (no defined transitions out), just no longer explicitly guarded by a `reject`.

### Compile Confirmation

`precept_compile` returns `valid: true`, `diagnostics: []` (zero errors, zero warnings, zero hints).

### Score Impact

None. All rubric constructs are preserved. The `reject` moved from `Merged` to `Review`; all six criterion constructs still present. Score remains **25/30**.

---

## Summary

| Candidate | Domain | Fix | Score Before | Score After |
|-----------|--------|-----|-------------|------------|
| 1 | Subscription Billing | Moved `reject` to `Active` state (downgrade guard) | 29/30 | 29/30 |
| 3 | Pull Request Review | Moved `reject` to `Review` state (premature-merge guard) | 25/30 | 25/30 |

Both candidates now compile with zero diagnostics. Neither fix changed the score.
