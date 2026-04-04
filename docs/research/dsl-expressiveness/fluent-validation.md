# FluentValidation — DSL Expressiveness Research

**Studied:** 2026-05-01  
**Source:** https://docs.fluentvalidation.net/en/latest/ (built-in validators, conditions)  
**Relevance:** FluentValidation is the standard .NET validation library for the same developer audience Precept targets. It ships with rich built-in validators, conditional rule application (`When`/`Unless`), and rule sets. The comparison here is the most commercially important: a .NET developer choosing Precept will compare it against FluentValidation.

---

## What FluentValidation Does Well

### 1. Co-located property + constraint chain

```csharp
RuleFor(x => x.Surname)
    .NotNull()
    .NotEmpty()
    .MaximumLength(100)
    .WithMessage("Surname cannot exceed 100 characters");
```

One expression: the property selector and all its rules. `.WithMessage()` overrides the default message for the preceding rule.

### 2. Numeric range constraints with readable names

```csharp
RuleFor(x => x.CreditLimit)
    .GreaterThan(0).WithMessage("Credit limit must be positive")
    .LessThanOrEqualTo(x => x.MaxCreditLimit).WithMessage("Cannot exceed max limit");
```

Cross-property comparison (`.LessThanOrEqualTo(x => x.MaxCreditLimit)`) is first-class.

### 3. Conditional rule blocks (`When`/`Unless`)

```csharp
When(customer => customer.IsPreferred, () =>
{
    RuleFor(x => x.Discount).GreaterThan(0);
    RuleFor(x => x.CreditCard).NotNull();
}).Otherwise(() =>
{
    RuleFor(x => x.Discount).Equal(0);
});
```

One condition gates a block of multiple rules. The `Otherwise` branch handles the inverse. This eliminates repeating the condition on every `RuleFor`.

### 4. Cross-field equality validation

```csharp
RuleFor(x => x.PasswordConfirmation)
    .Equal(x => x.Password)
    .WithMessage("Passwords must match");
```

Property-to-property equality is a single, readable rule.

---

## Equivalent Precept DSL

### Property chain

```precept
field Surname as string nullable
invariant Surname != null because "Surname is required"
invariant Surname != "" because "Surname cannot be empty"
# Note: no string length constraint exists in current Precept DSL
```

### Numeric range with cross-field

```precept
field CreditLimit as number default 0
field MaxCreditLimit as number default 1000

invariant CreditLimit > 0 because "Credit limit must be positive"
invariant CreditLimit <= MaxCreditLimit because "Cannot exceed max limit"
```

### Conditional rules (state-based equivalent)

```precept
state PreferredCustomer initial
state StandardCustomer

in PreferredCustomer assert Discount > 0 because "Preferred customers must have a discount"
in PreferredCustomer assert CreditCard != null because "Preferred customers need a card on file"
in StandardCustomer assert Discount == 0 because "Standard customers receive no discount"
```

### Cross-field equality

```precept
field Password as string nullable
field PasswordConfirmation as string nullable
invariant Password == PasswordConfirmation because "Passwords must match"
```

---

## Gap Analysis

### What's equal

**Cross-field invariants:** Precept's `invariant A == B because "..."` is more concise than FluentValidation's `.Equal(x => x.B).WithMessage("...")`. Precept wins here.

**Collect-all behavior:** FluentValidation evaluates all rules by default (unless `StopOnFirstFailure` is configured). Precept's invariants and asserts are always collect-all. Equal semantics.

### Where Precept is more verbose — GAP 1: No built-in constraint vocabulary

FluentValidation has `NotNull()`, `NotEmpty()`, `Length(min, max)`, `GreaterThan(n)`, `LessThan(n)`, `EmailAddress()`, `Matches(regex)` as first-class, named methods with default error messages.

Precept expresses each of these as boolean expressions in `invariant`:

| FluentValidation | Precept equivalent |
|---|---|
| `.NotNull()` | `invariant X != null because "..."` |
| `.NotEmpty()` | `invariant X != "" because "..."` |
| `.GreaterThan(0)` | `invariant X > 0 because "..."` |
| `.MaximumLength(100)` | **no equivalent** (no `.length`) |
| `.EmailAddress()` | **no equivalent** (no regex or format validators) |
| `.Matches(@"\d{5}")` | **no equivalent** |

FluentValidation's named constraint methods also carry **default error messages** — you get a human-readable error without writing `because`. Every Precept invariant requires an explicit `because` string.

**Language lacks constructs** for string length and format validation. The `because` requirement is by design but adds a line to every rule.

### Where Precept is more verbose — GAP 2: No conditional invariant block

FluentValidation's `When` block applies one condition to multiple rules without repeating the condition per rule:

```csharp
When(x => x.HasDiscount, () =>
{
    RuleFor(x => x.DiscountAmount).GreaterThan(0);
    RuleFor(x => x.DiscountCode).NotEmpty();
    RuleFor(x => x.DiscountExpiry).GreaterThan(DateTime.Now);
});
```

Precept has no equivalent conditional invariant block. The closest approximation is state-scoped asserts:

```precept
in HasDiscount assert DiscountAmount > 0 because "..."
in HasDiscount assert DiscountCode != "" because "..."
```

But `in <State>` is about a **named state** — you can't use an arbitrary boolean expression as the condition. If `HasDiscount` is a field value, not a state, there is no Precept equivalent.

**Language lacks a construct** for field-value-conditional invariants. This is a real gap for entities where a boolean flag enables/disables a set of related constraints.

### Where Precept is richer

**State-time discrimination:** FluentValidation has no concept of lifecycle state. All rules apply uniformly at validation time. Precept's `in <State> assert`, `to <State> assert`, and `from <State> assert` apply rules at specific lifecycle moments — entry, exit, and in-place. This is a significant expressiveness advantage for workflow entities that FluentValidation cannot match without manual state-checking code.

**Event-scoped validation:** `on <Event> assert` validates event argument integrity before any mutation. FluentValidation has no event/mutation model — validation is always against current object state. Precept's separation of event-arg validation from field invariants is structurally richer.

---

## Feature Proposals

### Proposal 1 (High Impact): Field-value conditional invariants

Add `when` as an optional condition prefix on `invariant`:

```precept
invariant when HasDiscount DiscountAmount > 0 because "Discount requires a positive amount"
invariant when HasDiscount DiscountCode != "" because "Discount requires a valid code"
```

Or, to match the `When` block pattern with multiple rules sharing one condition:

```precept
# No explicit block syntax — individual conditional invariants are sufficient
invariant DiscountAmount > 0 when HasDiscount because "Discount requires a positive amount"
invariant DiscountCode != "" when HasDiscount because "Discount requires a valid code"
```

**Impact:** Eliminates the workaround of creating artificial states for boolean-field conditions. Enables conditional validation for entities with a "is this feature enabled?" flag.

**Complexity:** Moderate. The `when` guard already exists on transition rows; applying it to `invariant` is a natural extension of the same grammar. Type checker must validate that the `when` condition references only field names, not event args.

**External precedent:** FluentValidation `.When()`, xstate guards, Zod `.refine()` with conditional logic.

### Proposal 2 (Medium Impact): String length accessor

As noted in the Zod research, add `.length` on `string` fields:

```precept
invariant Name.length >= 1 because "Name is required"
invariant Name.length <= 100 because "Name too long"
```

Low risk, high utility. Mirrors `.count` on collections.

---

## Takeaway for Hero Sample

The hero sample should deliberately showcase `in <State> assert` — the feature FluentValidation cannot express. This is where Precept beats the category leader. Avoid showing 4+ simple field invariants (non-null, non-empty) unless they're demonstrably business rules, not format rules — that's where FluentValidation's named validators have an advantage.
