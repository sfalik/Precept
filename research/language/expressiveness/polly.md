# Polly — DSL Expressiveness Research

**Studied:** 2026-05-01  
**Source:** Web research: pollydocs.org, DEV Community examples, C# Corner  
**Relevance:** Polly is the dominant .NET resilience library. It uses fluent, chainable policy builders with `.Handle<>().WaitAndRetry()` syntax. Polly's pipeline composition model and its named-policy registration pattern are the .NET developer's reference for "fluent DSL over sequential behavior rules." Precept's `->` action chain invites comparison.

---

## What Polly Does Well

### 1. Fluent policy builder — type + condition + behavior in one chain

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (ex, wait, count, _) =>
            Console.WriteLine($"Retry {count} in {wait.TotalSeconds}s: {ex.Message}"));
```

The chain reads: "When `HttpRequestException` → retry up to 3 times → with exponential backoff → logging each retry." All behavior is expressed in one expression.

### 2. Policy composition — wrap behaviors like layers

```csharp
var pipeline = Policy.WrapAsync(retryPolicy, circuitBreaker, timeout);
await pipeline.ExecuteAsync(async () => await httpClient.GetAsync(url));
```

Policies compose by wrapping — the outermost policy runs first, then hands off inward. Order determines behavior.

### 3. Polly v8 builder-style named pipelines (DI-friendly)

```csharp
services.AddResiliencePipeline("loan-service", builder =>
{
    builder.AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 });
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureThreshold = 0.5,
        BreakDuration = TimeSpan.FromSeconds(30)
    });
});
```

Named pipelines registered in DI, resolved by name — the name is the reusable handle.

---

## Equivalent Precept DSL

Polly's domain (resilience, retries, circuit breakers) has no direct Precept equivalent — they solve different problems. However, the *structural analog* for Polly's layered policy composition is Precept's `->` action pipeline:

```precept
# Polly: Handle → Retry → CircuitBreaker → Execute
# Precept: from → on → when → set → set → transition

from Draft on Submit when Submit.Amount > 0 && Submit.Score >= 0
    -> set ApplicantName = Submit.Applicant
    -> set RequestedAmount = Submit.Amount
    -> set CreditScore = Submit.Score
    -> transition UnderReview
```

Polly's fallback policy has the closest Precept analog in the default-fallback transition row:

```csharp
// Polly: if primary fails, use fallback
var policy = Policy.Handle<Exception>()
    .FallbackAsync(ct => Task.FromResult(defaultValue));
```

```precept
# Precept: if guarded row doesn't match, fall through to reject
from UnderReview on Approve when DocumentsVerified && CreditScore >= 680 -> transition Approved
from UnderReview on Approve -> reject "Approval conditions not met"
```

---

## Gap Analysis

### Where the domains diverge
Polly is a *behavior wrapping* library — it wraps code execution with policy layers. Precept is a *domain constraint* engine — it enforces state, data, and event rules without wrapping arbitrary code. These are orthogonal problem spaces. Most Polly patterns have no meaningful Precept translation.

### The structural lesson from Polly

Polly's **named pipeline registration** is the most transferable idea. Polly discovered that when policies are long or complex, giving them names solves both reuse and readability:

```csharp
services.AddResiliencePipeline("loan-service", builder => { ... });
// Later, resolved by name — not re-expressed inline
```

This maps precisely to the **named guards** gap identified in the xstate research. Precept `when` clauses that are long (e.g., the 5-condition loan eligibility check) would benefit from the same named-reference pattern.

**This provides second-source evidence for the named guard proposal.** Polly, xstate, and FluentValidation `RuleSet` all independently converge on the same solution to the "complex condition repeated in multiple rules" problem: give it a name.

### Action pipeline comparison

Polly's `WrapAsync(retry, circuitBreaker, timeout)` is a composition of *independent policies*. Precept's `-> set X -> set Y -> transition Z` is a *sequential mutation pipeline* — each step depends on the previous. These are structurally different:

- Polly: parallel policy layers wrapping one call (order = priority, not data flow)
- Precept: sequential data mutations (order = causality)

**No gap.** Precept's `->` pipeline is correctly modeled for sequential domain mutations. Polly's pattern is not a better model for what Precept does.

### Where Polly is richer: conditional action within a pipeline

Polly can vary behavior based on outcome at each layer (e.g., retry only on certain exceptions, fallback only if circuit is open). Precept's `->` chain has no branching — it is strictly linear.

```csharp
// Polly: different behavior depending on exception type
Policy.Handle<NotFoundException>().FallbackAsync(fallback)
      .WrapAsync(Policy.Handle<TimeoutException>().RetryAsync(2));
```

In Precept, if two execution paths differ only in one mutation step, you need two complete transition rows:

```precept
# Scenario: set Status to "Priority" if urgent, else "Normal"
from Draft on Submit when Submit.IsUrgent -> set Status = "Priority" -> transition Submitted
from Draft on Submit -> set Status = "Normal" -> transition Submitted
```

The `transition Submitted` and all other shared mutations must be written in both rows. Branching within a single `->` chain is not possible.

**Language lacks a construct** — ternary/conditional expressions in mutations. This is the most practical gap this comparison reveals for day-to-day Precept authoring.

---

## Related GitHub proposal issues

- **#9 — Ternary expressions in `set` mutations**: primary proposal for the branching-value pressure this comparison exposes.
- **#8 — Named rule declarations**: secondary evidence from Polly's named pipeline registration pattern.

The proposal bodies live in GitHub. This file remains the structural comparison: what Polly teaches about naming, linear pipelines, and why conditional value selection is the safer answer than branching outcomes inside `->`.

---

## Takeaway for Hero Sample

The ternary gap is a hero-sample risk: a compelling domain often has "set field to X or Y based on condition" logic that currently requires a duplicate row. If the hero sample has this shape, it will look verbose. Consider choosing a domain where branching mutations aren't needed, OR use this as a candidate feature to prototype alongside the hero.
