# Precept 🛡️

> **pre·​cept** *(noun)*: A general rule intended to regulate behavior or thought; a strict command or principle of action.

**Precept is a domain integrity engine for .NET.** It binds an entity's state, data, and business rules into a single, executable contract. By treating your business constraints as unbreakable *precepts*, the engine ensures that invalid states and illegal data mutations are fundamentally impossible.

## The Problem

Most complex entities—orders, loan applications, support tickets—start simple. But as business requirements grow, the rules governing their lifecycles scatter across your codebase:
- **State transitions** land in `switch` statements or scattered handler logic.
- **Data validation** gets pushed into ORMs, FluentValidation, or entity constructors.
- **Side effects** trigger asynchronously with no guarantee the data is ready.

Eventually, the system drifts. An entity ends up in a `Shipped` state without a `TrackingNumber`. A UI shows an "Approve" button that fails when clicked because a hidden precondition wasn't met. When stakeholders ask, "Under what exact conditions can an Order be refunded?", developers have to traverse six different classes to find the answer.

## The Precept Solution

Precept fixes this by treating the lifecycle of an entity as a **comprehensive constitution**. You define rules, state, and data in a `.precept` file. 

This file is not just documentation—it is a compiled, deterministic runtime contract. 

When you define an entity in Precept, you get three absolute guarantees:
1. **Data and state move in lockstep.** They are bound together and mutate as a single atomic unit.
2. **Rules are bulletproof.** Top-level data invariants (`rule`) are enforced on every single mutation, whether from a complex lifecycle event or a direct UI edit. 
3. **Inspection is side-effect-free.** Because Precept strictly separates pure expressions from mutational statements, you can ask the engine "What would happen if I tried to Fire this event?" and get an exact, deterministic answer without touching persisted data.

---

## 🏗️ The Pillars of Precept

### 1. The Universal Safety Net (`rule`)
In most systems, validation is bound to *actions* (e.g., "Validate this API payload"). In Precept, rules are bound to the *data itself*. 

When you declare `rule Balance >= 0 "Balance cannot be negative"`, that precept is absolute. Whether a complex workflow transition deducts from the balance, or a user directly edits a linked field via an administrative override, the engine enforces the rule upon completion. If the rule fails, the entire transaction rolls back.

### 2. Pure Inspection (`Inspect` before `Fire`)
Because Precept enforces rigorous grammar constraints—expressions evaluate, statements mutate—it is impossible for a transition guard to accidentally mutate data. This allows the `Inspect` API to safely preview any action. 

Your UI can evaluate `instance.Inspect("Submit")`. The engine will run the current data against all rules, guards, and transition paths, and return a precise outcome (`Enabled`, `Blocked`, `NoTransition`) with specific error reasons—all without saving a thing.

### 3. Atomic, Deterministic Mutations
A Precept transition either completely succeeds or entirely rolls back. If a branch executes three variable assignments and a collection mutation (`add Item to ShoppingCart`), and the final rule evaluation fails, the memory state reverts cleanly. Every evaluation is deterministic: the same definitions and the same data will *always* result in the same outcome. 

### 4. Two Mutational Paths
Precept acknowledges that entirely different ceremonies apply to different types of data updates:
* **Transitions (`event`):** For lifecycle changes where routing, auditing, and complex state progression matter (e.g., "Approve Loan").
* **Direct Edits (`edit`):** For simple data mutations where event ceremony is overkill (e.g., "Update Notes"). 

Both paths are safely watched by the exact same `rule` engine. Direct editing isn't a hack; it is a first-class feature protected by the same ironclad invariants.

---

## 📖 The DSL at a Glance

A `.precept` file is readable by domain experts but compiles into strict .NET runtime behavior.

```text
machine LoanApplication

# 1. Data bound to the lifecycle
number AmountRequested
  rule AmountRequested >= 1000 "Minimum loan amount is $1,000"
number CreditScore

string? RejectionReason

# 2. Clear state progression
state Draft initial
state UnderReview
  # State rules enforce contracts upon entry
  rule CreditScore >= 600 "Score too low for review"
state Approved
state Rejected

# 3. Explicit Events and Arguments
event Submit
event Reject
  string Reason
    rule Reason != "" "Must provide a rejection reason"

# 4. Transitions & Guards
from Draft on Submit
  transition UnderReview

from UnderReview on Reject
  set RejectionReason = Reject.Reason
  transition Rejected
```

---

## 🛠️ Developer Experience

- **Visual Studio Code Extension**: Precept offers a deeply integrated extension featuring real-time null-flow analysis (squiggles for unsafe null access), auto-completion, and a live **Interactive Inspector**.
- **Live Diagramming**: As you type, the extension renders a dynamic transition diagram.
- **Live Simulation**: Fire events directly inside VS Code against a mock instance to prove your rules function exactly as desired.

*Say goodbye to "invalid application state". Define your precepts, and let the engine enforce them.*