# xstate — DSL Expressiveness Research

**Studied:** 2026-05-01  
**Source:** https://xstate.js.org/docs/ + Stately docs on guards, transitions  
**Relevance:** xstate is the canonical JavaScript/TypeScript state machine library. It overlaps with the lifecycle-governance dimension of Precept (states, events, transitions, guards), but Precept's scope is broader: it governs entity integrity — lifecycle transitions, field constraints, editability rules, or any combination — including entities with no lifecycle at all. Comparing guard patterns and transition composition directly maps to Precept's `from ... on ... when` model.

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

**Verdict:** Hierarchical states serve UI state machines with deeply nested interaction modes. Precept governs entity integrity — lifecycle transitions, field constraints, and data rules — where flat states are appropriate and correct. The target domain is business entities that need governed integrity, not UI interaction trees.

### The pure state machine failure mode

The core gap in xstate — and any pure state machine library — is not the pipeline model or the syntax. It is that **an entity can pass through every valid transition and still hold corrupted field values.** xstate has no declared field model, no invariants, no data integrity enforcement. A `CancelledOrder` in the right state but with `Total > 0` is a valid xstate configuration — because no rule required otherwise.

Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine. The transition that would produce an invalid data configuration is rejected before it commits. Fields, invariants, and assertions are first-class declared constructs, not runtime action code the developer must remember to write.

### What xstate cannot serve: data-only entities

Precept also governs entities with no lifecycle at all — address records, fee schedules, policy configurations, patient demographics. These are data-only entities that need governed integrity (field constraints, cross-field invariants, editability rules) but have no state machine.

xstate cannot serve these entities. It has no model for a stateless entity with declared fields and structural constraint enforcement. In every real business domain, data and reference entities outnumber workflow entities. The test for whether an entity belongs in Precept is not "does it have a state machine?" but "does it need governed integrity?"

---

## Related GitHub proposal issues

- **#8 — Named rule declarations**: the canonical proposal body and scope boundaries now live in GitHub.

This file stays focused on evidence: why xstate's named guards matter, where Precept is more verbose today, and what semantic boundaries should survive adaptation.

---

## Takeaway for Hero Sample

The hero sample should keep `when` guards short (2–3 conditions) to stay readable at 15 lines. For a hero, named guards are not needed — the expressiveness gain only appears in larger precepts. However, the hero should show at least one non-trivial `when` guard to demonstrate conditional routing power.
