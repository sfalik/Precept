# LINQ — DSL Expressiveness Research

**Studied:** 2026-05-01  
**Source:** Web research: Microsoft Docs, Syncfusion LINQ Succinctly, DEV Community  
**Relevance:** LINQ (Language Integrated Query) is the reference example for fluent method chaining in .NET. Every .NET developer has internalized it. Precept's `->` action pipeline is structurally similar — a sequence of operations on a subject. LINQ teaches the limits and strengths of that model, particularly around branching, deferred execution, and composability.

---

## What LINQ Does Well

### 1. Fluent method chain — linear pipeline

```csharp
var result = employees
    .Where(e => e.IsActive)
    .Where(e => e.Department == "Engineering")
    .OrderByDescending(e => e.StartDate)
    .Select(e => new { e.Name, e.Salary });
```

Reads left-to-right as a pipeline of transformations. Each step refines the collection. The `->` in Precept was designed with this readability in mind.

### 2. Conditional chain extension (pattern — not built-in)

A common .NET pattern extends LINQ chains conditionally:

```csharp
var query = db.Users.AsQueryable();
if (filterByDepartment)
    query = query.Where(u => u.Department == department);
if (filterByActive)
    query = query.Where(u => u.IsActive);
var results = query.OrderBy(u => u.Name).ToList();
```

Or via a custom `.If` extension:

```csharp
var results = db.Users
    .If(filterByDepartment, q => q.Where(u => u.Department == department))
    .If(filterByActive, q => q.Where(u => u.IsActive))
    .OrderBy(u => u.Name)
    .ToList();
```

### 3. Inline conditional values — ternary in `Select`

```csharp
var labeled = orders
    .Select(o => new
    {
        o.OrderId,
        Status = o.IsUrgent ? "Priority" : "Standard",
        Fee = o.IsMember ? 0 : 25.00m,
    });
```

Conditional value selection is inline in the projection — no need to duplicate the entire pipeline for each case.

### 4. Deferred execution — pipeline built before run

LINQ builds the execution plan before materializing results. Each `.Where` / `.Select` adds to an expression tree; the query only executes when enumerated. This allows the pipeline to be assembled and composed before execution.

---

## Equivalent Precept DSL

### Linear pipeline — Precept is equivalent

```precept
from Draft on Submit
    -> set ApplicantName = Submit.Applicant
    -> set RequestedAmount = Submit.Amount
    -> set CreditScore = Submit.Score
    -> transition UnderReview
```

This is Precept's `->` pipeline. Semantically equivalent to LINQ's method chain: sequential, ordered, readable.

### Conditional value — Precept lacks inline branching

The LINQ ternary pattern:

```csharp
Status = o.IsUrgent ? "Priority" : "Standard"
```

In Precept, this requires two full transition rows:

```precept
from Draft on Submit when Submit.IsUrgent
    -> set Status = "Priority"
    -> transition Submitted

from Draft on Submit
    -> set Status = "Standard"
    -> transition Submitted
```

Both rows duplicate the `-> transition Submitted` (and any other shared mutations). For 3–4 shared mutations, this doubles the statement count.

### Conditional chain extension — Precept handles differently

LINQ's conditional query building (add `.Where` if a filter is active) is a runtime concern. Precept handles all conditional logic at event-fire time via `when` guards on transition rows — static, not dynamically composed. This is appropriate for a domain-rule engine.

---

## Gap Analysis

### What's equal

The linear pipeline model — `->` in Precept, `.Method()` in LINQ — is semantically equivalent for sequential, ordered operations. Statement count and readability are comparable. No gap.

### Where Precept is more verbose — GAP: No ternary in mutations

**This is the same gap identified in the Polly research**, with a cleaner LINQ parallel to illustrate it.

When the only difference between two transition rows is a single field's value, Precept requires the complete rows to be written twice. LINQ's `Select(x => condition ? a : b)` is one line.

**Concrete example from the hiring-pipeline sample:**

```precept
# These two rows share everything except the transition target
from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer && PendingInterviewers.count == 1
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> transition Decision

from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> no transition
```

The only difference is `transition Decision` vs `no transition`. The identical mutations (`remove`, `set FeedbackCount`) repeat across both rows. With ternary outcomes, this could be:

```precept
# Hypothetical — not current syntax
from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> transition when PendingInterviewers.count == 0 Decision else no transition
```

**Language lacks a construct.** This is repeated duplication in the current samples. It is the single highest-statement-count antipattern in the existing sample library.

### Deferred execution — Precept's different model

LINQ builds a query plan before executing. Precept evaluates transitions at fire time, not build time. This is a design choice, not a gap — Precept's inspectable, deterministic semantics require eager evaluation.

---

## Feature Proposals

### Proposal (High Impact): Conditional outcome in `->` chain

Allow the outcome (`transition`, `no transition`, `reject`) to be conditional:

```precept
from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> transition Decision when PendingInterviewers.count == 0
    -> no transition
```

Semantics: the first outcome whose `when` condition is true fires; if none match, the next `->` row is evaluated as a fallback.

**Alternative syntax — ternary transition:**

```precept
-> transition Decision if PendingInterviewers.count == 0 else no transition
```

**Impact:** High. Eliminates the most common row-duplication pattern in the sample library — "same mutations, different outcome based on post-mutation state." Searching the samples, this pattern appears in: hiring-pipeline (2 rows), trafficlight (multiple), subscription (1 row).

**Complexity:** High. The `->` chain semantics change from strictly-linear to potentially-branching. The runtime must handle evaluating post-mutation field state within the chain (post-`remove` count). Type checker must validate `when` condition scope within the chain.

**Risk:** This meaningfully increases language complexity and potentially undermines the "read top to bottom, first match wins" mental model. Consider carefully before adopting.

**Simpler alternative:** Allow ternary expressions in `set` only (not in outcomes). This is lower risk:

```precept
# Ternary in set — safe, type-checkable, no outcome branching
-> set Status = Submit.IsUrgent ? "Priority" : "Standard"
```

Ternary in `set` would reduce verbosity for value-branch mutations without touching the outcome routing model.

---

## Takeaway for Hero Sample

The hiring-pipeline's duplicate rows are a direct consequence of this gap. For the hero sample, choose a domain where all branches have distinct outcomes AND distinct mutations — so duplicate rows don't appear. Or, accept a duplicate row if the `when` guards are short and the "why two rows" explanation is part of the teaching value (conditional routing is a feature, not a bug).
