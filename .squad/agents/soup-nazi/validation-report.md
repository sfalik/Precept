# Hero Candidate Validation Report

**Date:** 2026-04-10
**Total:** 30 candidates
**Passed:** 30
**Failed:** 0
**Candidates with compiler advisories (warnings/hints):** 2 (Candidates 1 and 3)

> All 30 candidates compiled successfully (`valid: true`). No candidate produced a compile error.
> Two candidates produced non-fatal diagnostics — a `warning` and a `hint` each — flagged below.
> These are design advisories, not correctness failures. The deliberation team should be aware.

---

## Results

### Candidate 1: Subscription Billing — ✅ PASS ⚠️ ADVISORIES

Compiled clean. Two non-fatal diagnostics raised:

| Severity | Code | Line | Message |
|----------|------|------|---------|
| hint | PRECEPT050 | 8 | State 'Cancelled' has outgoing transitions but all reject or no-transition — no path forward. |
| warning | PRECEPT051 | 21 | Every transition row for (Cancelled, Activate) ends in reject — the event can never succeed from this state. Remove the rows and let Undefined handle it. |

**Explanation:** The `from Cancelled on Activate -> reject "..."` row is intentional — it is the hero's structural impossibility showcase. The compiler flags it as redundant because a missing transition row already produces an `Undefined` outcome. The `reject` form is more explicit and pedagogically clearer, but the compiler is correct that it is redundant. **Deliberation team: this is a known design trade-off, not a defect.**

---

### Candidate 2: Support Ticket — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 3: Pull Request Review — ✅ PASS ⚠️ ADVISORIES

Compiled clean. Two non-fatal diagnostics raised:

| Severity | Code | Line | Message |
|----------|------|------|---------|
| hint | PRECEPT050 | 8 | State 'Merged' has outgoing transitions but all reject or no-transition — no path forward. |
| warning | PRECEPT051 | 20 | Every transition row for (Merged, Submit) ends in reject — the event can never succeed from this state. Remove the rows and let Undefined handle it. |

**Explanation:** Same pattern as Candidate 1. The `from Merged on Submit -> reject "..."` row is the structural fence for "merged PRs cannot be reopened." Same trade-off applies.

---

### Candidate 4: Deploy Pipeline — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 5: API Key Lifecycle — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 6: Feature Flag — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 7: Password Reset — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 8: Purchase Order — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 9: Expense Report — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 10: Job Queue Task — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 11: Hotel Reservation — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 12: Food Delivery — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 13: Return / Refund — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 14: Email Campaign — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 15: SSL Certificate — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 16: Course Enrollment — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 17: Inventory Restock — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 18: Medical Appointment — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 19: Two-Factor Auth — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 20: Gym Membership — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 21: Building Access Badge — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 22: Parking Permit — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 23: Flight Booking — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 24: Freelance Contract — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 25: License Renewal — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 26: Coffee Order — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 27: Shopping Cart Checkout — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 28: SaaS Trial — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 29: Database Migration — ✅ PASS

Compiled clean. No diagnostics.

---

### Candidate 30: Code Review Approval — ✅ PASS

Compiled clean. No diagnostics.

---

## Summary of Failures

**No failures.** All 30 candidates compiled successfully.

---

## Compiler Advisory Summary (Non-Fatal)

| Candidate | Domain | Code | Severity | Message |
|-----------|--------|------|----------|---------|
| 1 | Subscription Billing | PRECEPT050 | hint | State 'Cancelled' has outgoing transitions but all reject or no-transition — no path forward. |
| 1 | Subscription Billing | PRECEPT051 | warning | Every transition row for (Cancelled, Activate) ends in reject — the event can never succeed from this state. |
| 3 | Pull Request Review | PRECEPT050 | hint | State 'Merged' has outgoing transitions but all reject or no-transition — no path forward. |
| 3 | Pull Request Review | PRECEPT051 | warning | Every transition row for (Merged, Submit) ends in reject — the event can never succeed from this state. |

---

## Common Error Patterns

**None.** No compile errors were found across all 30 candidates.

### Advisory Pattern: Explicit Reject on Terminal States (Candidates 1, 3)

Both candidates with advisories use `reject` on a terminal state to communicate a structural fence — the "this operation is permanently forbidden" pattern. The compiler correctly identifies this as redundant (an unhandled event already produces `Undefined`), but the explicit `reject` carries pedagogical intent: it teaches the reader that the system *actively refuses* the operation rather than simply not recognizing it.

**Recommendation for deliberation team:**
- If the hero sample wants to teach the `reject` pattern cleanly, using it in a non-terminal context (e.g., Candidate 1's `from Cancelled on Activate -> reject` *is* the canonical pattern for structural gates) is correct.
- The advisory is a style note, not a bug. PRECEPT051 is flagged as `warning` severity, not `error`. The snippet is valid and will run correctly.
- **If the hero must be advisory-free**, Candidate 28 (SaaS Trial) uses `reject` on a *conditional* path — guarded by `TrialDaysLeft > 0` — which avoids the all-reject-terminal pattern entirely. It scores 29 and generates zero advisories.

---

## Final Verdict

**The DSL quality of all 30 candidates is confirmed.** Every snippet parses, type-checks, and compiles. The deliberation team's scoring and ranking can proceed with confidence that no candidate has a disqualifying DSL defect.

The only open question is whether the `reject`-on-terminal advisory in Candidates 1 and 3 matters for the hero sample. Candidate 28 (SaaS Trial) is the clean alternative at the same score.
