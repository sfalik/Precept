# DSL Expressiveness Research — README

**Author:** Steinbrenner (PM)  
**Date:** 2026-05-01  
**Status:** Complete (v1)

This directory contains expressiveness research comparing Precept's DSL surface against six reference libraries. The goal: find where Precept requires more statements to express a concept that comparable tools express more concisely, and identify whether each gap warrants a language change.

---

## Libraries Studied

| File | Library | Closest Precept overlap |
|---|---|---|
| [fluent-assertions.md](./fluent-assertions.md) | FluentAssertions | `invariant`, `on ... assert` (collect-all validation) |
| [zod-valibot.md](./zod-valibot.md) | Zod / Valibot | Field declarations + `invariant` (schema + constraints) |
| [xstate.md](./xstate.md) | xstate | `from ... on ... when` (guarded transitions) |
| [polly.md](./polly.md) | Polly | `->` action pipeline (sequential behavior chains) |
| [fluent-validation.md](./fluent-validation.md) | FluentValidation | `invariant`, `in <State> assert` (conditional validation) |
| [linq.md](./linq.md) | LINQ | `->` action pipeline (method chain + conditional projections) |

---

## Top 3 Gap Findings

### Gap 1 — Inline field constraints (Zod, FluentValidation)

**What they do:** Zod writes `z.string().min(1).max(100)` in the field declaration. FluentValidation writes `RuleFor(x => x.Name).NotNull().MaximumLength(100)`. Type + constraints co-located in one statement.

**What Precept requires:**
```precept
field Name as string nullable
invariant Name != null because "Name is required"
invariant Name != "" because "Name cannot be empty"
# no string length constraint at all
```

**Statement ratio:** 1 line (Zod/FV) vs 2–3+ lines (Precept) per constrained field.

**Root cause:** Deliberate separation of structural declaration (`field`) from behavioral rules (`invariant`). Correct for complex business rules; creates unnecessary verbosity for simple format/range guards.

**Severity:** Medium. Impacts data-entry-heavy entities (5+ fields with range constraints). Does not affect workflow-logic-heavy entities (where Precept's model is stronger).

---

### Gap 2 — No named guards (xstate, Polly)

**What they do:** xstate's `guards: { isEligible: ctx => ctx.score >= 680 }` names a guard once and references it by name in multiple transitions. Polly's named pipeline registration does the same for resilience policies. FluentValidation's `RuleSet` names a group of rules.

**What Precept requires:**
```precept
# 5-condition guard must be re-written in every transition row that uses it
from UnderReview on Approve when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount -> ...
from UnderReview on Reconsider when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 -> ...
```

**Statement ratio:** Not more statements, but significantly more *repeated expression*. A change to eligibility logic requires updating every row that inlines it.

**Root cause:** Language lacks a `guard` declaration form. No way to name an expression for reuse.

**Severity:** Medium-High for complex precepts with shared multi-condition guards. Low for simple precepts.

---

### Gap 3 — No ternary/conditional in mutation chains (LINQ, Polly)

**What they do:** LINQ's `Select(x => condition ? a : b)` allows inline conditional value selection. Polly's fallback policy provides a conditional outcome without duplicating the surrounding pipeline.

**What Precept requires:**
```precept
# Same mutations, different outcome — requires two complete transition rows
from InterviewLoop on RecordFeedback when PendingInterviewers contains RecordFeedback.Interviewer && PendingInterviewers.count == 1
    -> remove PendingInterviewers RecordFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> transition Decision

from InterviewLoop on RecordFeedback when PendingInterviewers contains RecordFeedback.Interviewer
    -> remove PendingInterviewers RecordFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> no transition
```

**Statement ratio:** 2 complete rows instead of 1. The shared mutations repeat verbatim. This pattern appears across multiple sample files (hiring-pipeline, trafficlight, subscription).

**Root cause:** The `->` chain is strictly linear — no branching. Outcomes are always the last step; they cannot be conditional within a chain.

**Severity:** High for workflow-heavy precepts. The hiring-pipeline sample has 2 rows exhibiting this pattern; a real enterprise precept might have 6–8.

---

## Feature Proposals With Strong External Precedent

Three proposals emerged with cross-library backing. Ranked by PM priority:

### 🥇 Priority 1: Named guard declarations

```precept
guard LoanEligible when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2

from UnderReview on Approve when LoanEligible && Approve.Amount <= RequestedAmount -> ...
from UnderReview on Reconsider when LoanEligible -> ...
```

**Precedent:** xstate (named guard functions), Polly (named pipeline registration), FluentValidation (RuleSet)  
**Impact:** Eliminates repeated multi-condition guards. Enables single-point updates for shared eligibility logic.  
**Complexity:** Moderate. New `guard` declaration form, name resolution in `when` context.

---

### 🥈 Priority 2: Ternary expressions in `set` mutations

```precept
-> set Status = Submit.IsUrgent ? "Priority" : "Standard"
-> set Fee = Submit.IsMember ? 0 : 25
```

**Precedent:** LINQ (Select projection), every mainstream language  
**Impact:** Eliminates duplicate transition rows where only a mutated value differs.  
**Complexity:** Low-moderate. Expression language already handles boolean; ternary is a type-compatible extension. Scope-limited to `set` values for safety.

---

### 🥉 Priority 3: String `.length` accessor

```precept
invariant Name.length >= 1 because "Name is required"
invariant Description.length <= 500 because "Description too long"
```

**Precedent:** Zod (`.min(n)`), FluentValidation (`.Length(n, m)`), JavaScript  
**Impact:** Unlocks string-length invariants that are currently inexpressible.  
**Complexity:** Very low. Mirrors `.count` on collections. Expression parser change only.

---

## Proposals Rejected

| Proposal | Reason |
|---|---|
| Conditional invariant block (`when ... { invariant ... }`) | FluentValidation's `When` block adds syntax complexity; state-scoped asserts (`in <State> assert`) already handle most cases. Revisit if conditional-invariant pain is reported by users. |
| Inline field constraints (`field X as number min 0`) | Conflicts with keyword-anchored design principle. Every statement must start with a keyword; inline constraints after a type break this. Not recommended without strong user demand. |
| Conditional outcome in `->` chain | Too complex. Outcome branching within a chain conflicts with the "first-match wins" top-to-bottom mental model. Ternary in `set` is the safer substitute. |

---

## Impact on Hero Sample Design

Three concrete implications for hero sample selection and authoring:

1. **Avoid simple format invariants.** `invariant Name != null && Name != ""` is readable, but 4–5 such invariants on entry fields will look verbose vs. Zod. Hero should show *business-rule* invariants (e.g., `ApprovedAmount <= RequestedAmount`), not format-guard invariants.

2. **Keep `when` guards short.** The named-guard gap hurts most on 5-condition guards. A hero sample at 15 lines should use at most 2–3 conditions in a `when` clause.

3. **Feature the `in <State> assert` pattern.** This is where Precept beats FluentValidation, Zod, and xstate — none of them express state-conditional validation as concisely. The hero must demonstrate this to differentiate from the competition.

---

## Files Created

- `fluent-assertions.md` — FluentAssertions; education gap only; no proposals  
- `zod-valibot.md` — Zod/Valibot; Gap 1 (inline constraints); Proposal: string `.length`  
- `xstate.md` — xstate; Gap 2 (named guards); Priority 1 proposal  
- `polly.md` — Polly; confirms named-guard and ternary gaps; Priority 2 proposal  
- `fluent-validation.md` — FluentValidation; Gap 1 and conditional invariant gap; Priority 3 proposal  
- `linq.md` — LINQ; Gap 3 (ternary in mutations); Priority 2 proposal  
