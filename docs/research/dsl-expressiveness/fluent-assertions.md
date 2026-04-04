# FluentAssertions — DSL Expressiveness Research

**Studied:** 2026-05-01  
**Source:** https://fluentassertions.com/introduction  
**Relevance:** .NET-native, same target audience as Precept. Teaches readable, composable assertion chains and collect-all failure reporting — both patterns Precept uses in its validation model.

---

## What FluentAssertions Does Well

FluentAssertions expresses multi-constraint validation as a single **fluent chain** on a subject, with `.And` connectors:

```csharp
// All four checks on one subject, one expression
string actual = "ABCDEFGHI";
actual.Should()
      .StartWith("AB")
      .And.EndWith("HI")
      .And.Contain("EF")
      .And.HaveLength(9);
```

For entity-level validation (multiple fields, multiple rules), it uses `AssertionScope` to collect all failures instead of stopping at the first:

```csharp
using (new AssertionScope())
{
    user.Email.Should().NotBeNullOrWhiteSpace();
    user.Age.Should().BeGreaterThan(18);
    user.CreditLimit.Should().BeGreaterThan(0).And.BeLessOrEqualTo(MaxLimit);
}
// All failures reported together
```

Custom assertions can be encapsulated and re-used across tests:

```csharp
user.Should().BeActivePremiumUser();
// → internally checks: Active == true, Tier == "Premium", CreditCard != null
```

---

## Equivalent Precept DSL

For the same multi-constraint validation (email non-empty, age, credit limit):

```precept
invariant Email != null because "Email is required"
invariant Email != "" because "Email cannot be blank"
invariant Age > 18 because "Must be an adult"
invariant CreditLimit > 0 because "Credit limit must be positive"
invariant CreditLimit <= MaxCreditLimit because "Credit limit cannot exceed maximum"
```

Precept already collect-all by default — all five `invariant` statements evaluate before committing, reporting every failure.

---

## Gap Analysis

### What's equal
Precept's `invariant` is semantically equivalent to FluentAssertions' `AssertionScope` collect-all behavior. Every `invariant` in a precept is always evaluated regardless of prior failures. No construct is missing here.

### Where Precept is more verbose
**Problem:** FluentAssertions can chain multiple checks on the *same field* in one expression with distinct failure messages per check:

```csharp
// One property, two distinct failure messages
RuleFor(x => x.Email)
    .NotNull().WithMessage("Email is required")
    .NotEmpty().WithMessage("Email cannot be blank");
```

In Precept, each distinct message requires a separate `invariant` statement:

```precept
invariant Email != null because "Email is required"
invariant Email != "" because "Email cannot be blank"
```

This is 2 lines vs 2 chained clauses — the statement count is the same, but the Precept versions are longer because the field name repeats. For a field with 3–4 constraints this compounds.

**Root cause:** Precept has no per-constraint `because` within a compound expression. A compound `&&` expression gets one `because` for the whole expression:

```precept
# Only one because for both checks — loss of attribution
invariant Email != null && Email != "" because "Email must be a non-blank string"
```

### Verbosity verdict
**Education gap, not language gap.** For simple non-null + non-empty checks, authors *should* write `invariant Email != null && Email != ""` with a single consolidated message. Most domain constraints don't need separate error messages per sub-clause — the combined message is cleaner.

However, for numeric range constraints that users *do* want separate messages for (e.g., `> 0` and `<= 100`), separate `invariant` lines are necessary. This is **appropriate verbosity** — each constraint is a distinct business rule with its own policy statement.

### Custom/reusable assertions
FluentAssertions lets you name and reuse assertion bundles (`BeActivePremiumUser()`). Precept has no named invariant groups — every check is re-stated inline. For a precept with many states sharing the same field invariants, `in any assert` provides one form of reuse (`in any assert Email != null`), but there is no way to name a reusable constraint bundle.

**Verdict:** Minor education gap. The `in any assert` pattern covers most cross-state invariant sharing. Named invariant groups would be a convenience, not a correctness feature.

---

## Feature Proposal

**None required for correctness.** One low-priority convenience:

> **Compound `because` clauses** — allow per-sub-expression messages in compound `&&` invariants:  
> `invariant Email != null because "required" && Email != "" because "cannot be blank"`  
> This is a sugar form that desugars to two separate invariants at compile time.

**Risk:** Complicates the grammar significantly, especially for nested `&&`/`||`. Not recommended without a concrete user pain signal. The two-invariant pattern is readable and precedented.

---

## Takeaway for Hero Sample

No change needed. The `invariant` + `on ... assert` pattern already matches FluentAssertions' expressiveness for the validation use cases a hero sample would demonstrate. The hero should show `invariant` and `on ... assert` together to demonstrate the data-truth vs. movement-truth distinction — a concept FluentAssertions conflates.
