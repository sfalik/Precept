# xstate — DSL Expressiveness Research

**Studied:** 2026-05-01  
**Source:** https://xstate.js.org/docs/ + Stately docs on guards, transitions  
**Relevance:** xstate is the canonical JavaScript/TypeScript state machine library. It handles the same problem domain as Precept (states, events, transitions, guards) and has been through multiple design iterations. Comparing guard patterns and transition composition directly maps to Precept's `from ... on ... when` model.

---

## What xstate Does Well

### 1. Simple state machine — clean object structure

```typescript
const lightMachine = createMachine({
  id: 'light',
  initial: 'red',
  states: {
    red:    { on: { TIMER: 'green' } },
    yellow: { on: { TIMER: 'red' } },
    green:  { on: { TIMER: 'yellow' } },
  }
});
```

Three states, one event each, clear visual grouping by state.

### 2. Guarded transitions — prioritized conditional routing

```typescript
const loanMachine = createMachine({
  context: { creditScore: 0, documentsVerified: false },
  states: {
    underReview: {
      on: {
        APPROVE: [
          {
            target: 'approved',
            guard: ({ context }) =>
              context.documentsVerified && context.creditScore >= 680
          },
          { target: 'declined' }  // fallback
        ]
      }
    }
  }
});
```

Multiple guarded transitions for the same event are expressed as an array. First match wins — identical semantics to Precept's top-to-bottom first-match.

### 3. Named, reusable guards

```typescript
const machine = createMachine({
  states: {
    pending: {
      on: {
        PROCESS: { target: 'processing', guard: 'hasEnoughBalance' },
        REFUND:  { target: 'refunding', guard: 'hasEnoughBalance' },  // reused
      }
    }
  }
}, {
  guards: {
    hasEnoughBalance: ({ context }) => context.balance >= context.amount,
  }
});
```

Guard logic is defined once under `guards` and referenced by name across multiple transitions.

### 4. Composable guards with `and`/`or`/`not`

```typescript
guards: {
  isEligible: and(['isVerified', 'hasSufficientScore', 'isAffordable']),
  isVerified: ({ context }) => context.documentsVerified,
  hasSufficientScore: ({ context }) => context.creditScore >= 680,
}
```

Compound guards can be assembled from named primitives without rewriting the boolean expression.

---

## Equivalent Precept DSL

Same loan approval example in Precept:

```precept
from UnderReview on Approve when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount
    -> set ApprovedAmount = Approve.Amount
    -> set DecisionNote = Approve.Note
    -> transition Approved
from UnderReview on Approve -> reject "Approval requires verified documents, strong credit, and affordable debt load"
```

(This is the actual loan-application sample, line 52–53.)

---

## Gap Analysis

### What's equal
The first-match transition routing semantics are identical between xstate and Precept. Both evaluate conditions top-to-bottom and take the first match. The last row acts as the default/fallback. No construct is missing here.

### Where Precept is more verbose — GAP 1: No named guards

**The most significant structural expressiveness gap vs. xstate.**

In the loan application sample, the approval guard is 5 conditions joined with `&&`:

```precept
when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount
```

If a second transition in the same precept needs to check the same eligibility (say, a re-application event), the entire expression must be copy-pasted. There is no way to name this guard and reference it.

In xstate, this becomes:

```typescript
guard: 'isLoanEligible'
// defined once:
isLoanEligible: ({ context, event }) => context.documentsVerified && ...
```

**Language lacks a construct.** Named guards would allow:

```precept
# hypothetical
guard LoanEligible when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2

from UnderReview on Approve when LoanEligible -> ...
from UnderReview on Reconsider when LoanEligible -> ...
```

For precepts with 3+ transition rows sharing a complex eligibility condition, this saves significant repetition.

### Where Precept is richer — data-in-event

xstate's context is a flat JavaScript object. Mutations to context require `assign` actions. There is no built-in concept of event arguments carrying new data that mutates context — it's all imperative action functions.

Precept's `from X on Submit -> set Name = Submit.Name` pattern is more declarative and readable:

```precept
# Precept — obvious, one line per field
from Draft on Submit
    -> set ApplicantName = Submit.Applicant
    -> set RequestedAmount = Submit.Amount
    -> transition UnderReview
```

```typescript
// xstate — requires assign() with context spread
on: {
  SUBMIT: {
    target: 'underReview',
    actions: assign(({ event }) => ({
      applicantName: event.applicant,
      requestedAmount: event.amount,
    }))
  }
}
```

**Precept wins on event-argument mutation clarity.** The `set Field = Event.Arg` pattern is visibly more readable than `assign` with spread.

### Where xstate is richer — hierarchical (nested) states

xstate supports hierarchical states (substates within states). Precept is strictly flat — no state nesting. For complex UIs with modal sub-states (a checkout flow inside an order), this is a real limitation.

**Verdict:** Out of scope for Precept's domain. Precept targets backend entity lifecycle (order, subscription, loan), not UI state machines. Flat states are appropriate and correct for the target domain.

---

## Feature Proposal

### Proposal: Named guard declarations

Allow authors to name frequently-used guard expressions for reuse across transition rows:

```precept
guard LoanEligible when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2

from UnderReview on Approve when LoanEligible && Approve.Amount <= RequestedAmount -> ...
from UnderReview on Reconsider when LoanEligible -> ...
```

**Impact:** Eliminates long repeated `when` expressions in precepts with multi-condition eligibility gates. Most impactful when 3+ transition rows share a base condition.

**Complexity:** Moderate. Named guards are a new declaration form. The type checker must resolve guard names in `when` expressions. Guard bodies may only reference fields (not event args), or the declaration form must optionally reference event args. The simplest version is field-only guards.

**External precedent:** xstate named guards, Polly named policies, FluentValidation `RuleSet`.

**Priority:** Medium. The loan-application sample already uses a 100+ character `when` clause. Any enterprise-scale precept will hit this pain point.

---

## Takeaway for Hero Sample

The hero sample should keep `when` guards short (2–3 conditions) to stay readable at 15 lines. For a hero, named guards are not needed — the expressiveness gain only appears in larger precepts. However, the hero should show at least one non-trivial `when` guard to demonstrate conditional routing power.
