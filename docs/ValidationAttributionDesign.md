# Validation Attribution Design

Date: 2026-03-22

Status: **Design phase** — walking through scenarios.

## Problem

Precept's runtime currently reports validation failures as flat string lists (`IReadOnlyList<string> Reasons`) with no structured information about what the failure is *about*. Consumers (preview UI, API callers, CLI) have no way to know:

- Is this about a field the user is editing? Which one?
- Is this about an event arg the user provided? Which one?
- Is this a routing problem the user can't fix by changing input?
- Should it render inline on an input, or as a banner?

The preview service currently guesses attribution by string-matching and heuristics. That's fragile. The runtime knows the answer semantically but throws it away and returns strings.

## Goal

Make the runtime the source of truth for "what failed and what it's about," so any consumer can render local feedback without guessing.

## Design Principles

1. **The runtime reports on the subjects the constraint references, not on what caused them to change.** If an invariant on `Balance` fails after `set Balance = Balance - Pay.Amount`, the issue targets `Field("Balance")`, not `EventArg("Pay", "Amount")`. No reverse-mapping through mutations.

2. **The consumer decides rendering.** The runtime provides targets. The consumer knows what its current inputs are and can trivially check: "is this target one of my inputs? → inline. Otherwise → banner."

3. **No UI-leaking terms in the runtime model.** No "form," no "inline," no "banner." Just targets and sources.

4. **Keep `Inspect` as the method name.** Three overloads differentiated by signature (aggregate, event, update) stay as they are.

## Five Categories of Failure

### A. Event asserts

```precept
on MakePayment assert Amount > 0 because "Amount must be positive"
```

- **Scope:** Event args only (enforced at parse time).
- **When checked:** Pre-transition, before row selection.
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Event arg(s) referenced in the expression.

### B. Invariants

```precept
invariant Balance >= 0 because "Balance cannot go negative"
```

- **Scope:** Fields only.
- **When checked:** Post-mutation, pre-commit. Always — every fire and every update.
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Field(s) referenced in the expression.

### C. State asserts

```precept
in Assigned assert AssignedAgent != null because "Must have an assigned agent"
from Draft assert Email != null because "Must provide email before submitting"
to Submitted assert Items.count > 0 because "Must have items to submit"
```

- **Scope:** Fields only.
- **When checked:** Post-mutation, pre-commit. Temporally scoped by preposition (`in` = while residing, `to` = entering, `from` = leaving).
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Field(s) referenced in the expression.

### D. Transition row `reject`

```precept
from UnderReview on Approve -> reject "Approval requires collateral or credit score >= 700"
```

- **Scope:** n/a (literal string, not a predicate).
- **When checked:** When a matching row's outcome is `reject`.
- **Author-supplied reason:** Yes (the reject string).
- **Targets:** The event as a whole. This is a routing decision, not a data constraint.

### E. Transition `when` guards

```precept
from Signing on RecordSignature
    when PendingSignatories contains RecordSignature.SignatoryName
    -> ...
```

- **Scope:** Fields AND event args. The only mixed-scope predicate in the language.
- **When checked:** During first-match row selection. If false, the row is skipped.
- **Author-supplied reason:** No. No `because` clause on `when`. The runtime synthesizes a message from the expression text.
- **Targets:** The fields and/or event args referenced in the guard expression.

## Runtime Model

### `ValidationTarget` — what the issue is about

```csharp
public enum ValidationTargetKind
{
    Field,
    EventArg,
    Event,
    State,
    Precept
}

public abstract record ValidationTarget(ValidationTargetKind Kind)
{
    public sealed record FieldTarget(string FieldName)
        : ValidationTarget(ValidationTargetKind.Field);

    public sealed record EventArgTarget(string EventName, string ArgName)
        : ValidationTarget(ValidationTargetKind.EventArg);

    public sealed record EventTarget(string EventName)
        : ValidationTarget(ValidationTargetKind.Event);

    public sealed record StateTarget(string StateName, AssertPhase? Phase = null)
        : ValidationTarget(ValidationTargetKind.State);

    public sealed record PreceptTarget()
        : ValidationTarget(ValidationTargetKind.Precept);
}

public enum AssertPhase { In, To, From }
```

### `ValidationSource` — where it came from

```csharp
public enum ValidationSourceKind
{
    Invariant,
    StateAssert,
    EventAssert,
    WhenGuard,
    TransitionReject,
    InputContract,
    Editability,
    Compatibility
}

public sealed record ValidationSource(
    ValidationSourceKind Kind,
    string? ExpressionText,
    string? Reason,
    string? EventName,
    string? StateName,
    AssertPhase? StatePhase,
    int? SourceLine);
```

### `ValidationIssue` — the unit of feedback

```csharp
public sealed record ValidationIssue(
    string Message,
    ValidationSourceKind SourceKind,
    ValidationSource Source,
    IReadOnlyList<ValidationTarget> Targets);
```

No `Confidence` field. The consumer knows what its inputs are and can trivially determine whether a target is inline-renderable:

```
for each issue:
  for each target:
    if target is one of my current inputs → attach inline
  if no target matched any input → show as banner
```

### Compile-time subject extraction

At compile time, walk each constraint's expression AST to record which fields and args it references:

```csharp
public sealed record ExpressionSubjects(
    IReadOnlyList<string> FieldReferences,
    IReadOnlyList<(string Event, string Arg)> ArgReferences);
```

- `PreceptIdentifierExpression` with no dot → field reference (or arg name inside event assert scope).
- `PreceptIdentifierExpression` with dot (`Event.Arg`) → arg reference.

This is computed once in `PreceptCompiler` and stored alongside each invariant, state assert, event assert, and transition row's `WhenGuard`. At inspect time, the runtime looks up the precomputed subjects — no expression re-parsing needed.

## Scenarios (Agreed)

### Scenario 1: Event assert fails

```precept
precept Payment

field Balance as number default 100

state Active initial

event MakePayment with Amount as number
on MakePayment assert Amount > 0 because "Amount must be positive"

from Active on MakePayment
    -> set Balance = Balance - MakePayment.Amount
    -> no transition
```

`Inspect(instance, "MakePayment", { Amount: 0 })`

```
Message:    "Amount must be positive"
SourceKind: EventAssert
Targets:    [ EventArg("MakePayment", "Amount") ]
```

Event asserts only reference event args. The user provided those args. The consumer shows this inline on the Amount input.

### Scenario 2: Post-mutation invariant fails during event inspection

```precept
precept Payment

field Balance as number default 1000
invariant Balance >= 0 because "Balance cannot go negative"

state Active initial

event MakePayment with Amount as number
on MakePayment assert Amount > 0 because "Amount must be positive"

from Active on MakePayment
    -> set Balance = Balance - MakePayment.Amount
    -> no transition
```

`Inspect(instance, "MakePayment", { Amount: 5000 })` when `Balance` is `1000`.

Event assert passes. Row matches. Simulation: `Balance = 1000 - 5000 = -4000`. Invariant fails.

```
Message:    "Balance cannot go negative"
SourceKind: Invariant
Targets:    [ Field("Balance") ]
```

The consumer checks: is `Field("Balance")` one of my inputs? No — the user typed `Amount`. Shows as a banner/event-level error.

The runtime does not reverse-map through mutations. If the author wants arg-level feedback, they write a `when` guard or an event assert.

### Scenario 3: State assert fails on entry

```precept
precept Ticket

field AssignedAgent as string nullable
field Priority as number default 3

state Open initial
state Assigned
in Assigned assert AssignedAgent != null because "Must have an assigned agent"

event StartWork

from Open on StartWork -> transition Assigned
```

`Inspect(instance, "StartWork")` when `AssignedAgent` is `null`.

Row matches. No mutations. Entry into Assigned triggers `in Assigned assert AssignedAgent != null`. Fails.

```
Message:    "Must have an assigned agent"
SourceKind: StateAssert
Targets:    [ Field("AssignedAgent") ]
```

The consumer checks: is `Field("AssignedAgent")` one of my inputs? No — StartWork has no args. Shows as a banner.

## Scenarios (Remaining)

- Scenario 4: `when` guard fails, fallback row is `reject`
- Scenario 5: All `when` guards fail → `NotApplicable`
- Scenario 6: Field edit fails an invariant via `Inspect(instance, patch)`
- Scenario 7: Multi-subject invariant (cross-field)
- Scenario 8: Mixed `when` guard (fields + args)
