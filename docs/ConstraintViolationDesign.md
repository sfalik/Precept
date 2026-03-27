# Constraint Violation Design

Date: 2026-03-22

Status: **Design phase** — walking through scenarios.

## Problem

Precept's runtime currently reports constraint violations as flat string lists (`IReadOnlyList<string> Reasons`) with no structured information about what the failure is *about*. Consumers (preview UI, API callers, CLI) have no way to know:

- Is this about a field the user is editing? Which one?
- Is this about an event arg the user provided? Which one?
- Is this a routing problem the user can't fix by changing input?
- Should it render inline on an input, or as a banner?

The preview service currently guesses attribution by string-matching and heuristics. That's fragile. The runtime knows the answer semantically but throws it away and returns strings.

## Goal

Make the runtime the source of truth for "which constraint was violated and what it's about," so any consumer can render local feedback without guessing.

## Design Principles

1. **The runtime reports on the subjects the constraint references, not on what caused them to change.** If an invariant on `Balance` fails after `set Balance = Balance - Pay.Amount`, the violation targets `Field("Balance")`, not `EventArg("Pay", "Amount")`. No reverse-mapping through mutations.

2. **The consumer decides rendering.** The runtime provides targets. The consumer knows what its current inputs are and can trivially check: "is this target one of my inputs? → inline. Otherwise → banner."

3. **No UI-leaking terms in the runtime model.** No "form," no "inline," no "banner." Just targets and sources.

4. **Keep `Inspect` as the method name.** Three overloads differentiated by signature (aggregate, event, update) stay as they are.

5. **Every scoped constraint includes its scope as a target.** Event asserts include `Event(name)`. State asserts include `State(name, anchor)`. Invariants include `Definition()`. This is uniform — every violation carries both its semantic subjects (what's wrong) and its scope (why this constraint exists).

## Four Kinds of Constraints

A precept is a collection of rules among other things. Invariants, asserts, and rejects are all constraints — each expresses a condition the engine enforces. When a constraint is violated, the engine reports a `ConstraintViolation`.

### A. Event asserts

```precept
on MakePayment assert Amount > 0 because "Amount must be positive"
```

- **Scope:** Event args only (enforced at parse time).
- **When checked:** Pre-transition, before row selection.
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Expression-referenced arg(s) + `Event(name)`.

### B. Invariants

```precept
invariant Balance >= 0 because "Balance cannot go negative"
```

- **Scope:** Fields only.
- **When checked:** Post-mutation, pre-commit. Always — every fire and every update.
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Expression-referenced field(s) + `Definition()`.

### C. State asserts

```precept
in Assigned assert AssignedAgent != null because "Must have an assigned agent"
from Draft assert Email != null because "Must provide email before submitting"
to Submitted assert Items.count > 0 because "Must have items to submit"
```

- **Scope:** Fields only.
- **When checked:** Post-mutation, pre-commit. Temporally scoped by preposition (`in` = while residing, `to` = entering, `from` = leaving).
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Expression-referenced field(s) + `State(name, anchor)`.

### D. Transition row `reject`

```precept
from UnderReview on Approve -> reject "Approval requires collateral or credit score >= 700"
```

- **Scope:** n/a (literal string, not a predicate).
- **When checked:** When a matching row's outcome is `reject`.
- **Author-supplied reason:** Yes (the reject string).
- **Targets:** The event as a whole. This is an authored routing rule, not a data constraint.

### E. Transition `when` guards (not constraints)

`when` guards are routing logic, not constraints. They don't produce violations.

```precept
from Signing on RecordSignature
    when PendingSignatories contains RecordSignature.SignatoryName
    -> ...
```

- **Scope:** Fields AND event args. The only mixed-scope predicate in the language.
- **When checked:** During first-match row selection. If false, the row is skipped.
- **Produces violations?** No. `when` guards are routing logic, not constraints. A guard that evaluates to false simply means the row doesn't match — the runtime moves to the next row. Only the row that actually fires (or an explicit `reject` fallback) produces outcomes.

### Constraint Summary

Every scoped constraint includes its scope alongside its expression-referenced subjects:

| Source | Targets |
|---|---|
| Event assertion | Expression-referenced arg(s) + `Event(name)` |
| Invariant | Expression-referenced field(s) + `Definition()` |
| State assertion | Expression-referenced field(s) + `State(name, anchor)` |
| Transition rejection | `Event(name)` |

## Runtime Model

### `ConstraintTarget` — what the violation is about

```csharp
public enum ConstraintTargetKind
{
    Field,
    EventArg,
    Event,
    State,
    Definition
}

public abstract record ConstraintTarget(ConstraintTargetKind Kind)
{
    public sealed record FieldTarget(string FieldName)
        : ConstraintTarget(ConstraintTargetKind.Field);

    public sealed record EventArgTarget(string EventName, string ArgName)
        : ConstraintTarget(ConstraintTargetKind.EventArg);

    public sealed record EventTarget(string EventName)
        : ConstraintTarget(ConstraintTargetKind.Event);

    public sealed record StateTarget(string StateName, AssertAnchor? Anchor = null)
        : ConstraintTarget(ConstraintTargetKind.State);

    public sealed record DefinitionTarget()
        : ConstraintTarget(ConstraintTargetKind.Definition);
}

public enum AssertAnchor { In, To, From }
```

### `ConstraintSource` — where it came from

```csharp
public enum ConstraintSourceKind
{
    Invariant,
    StateAssertion,
    EventAssertion,
    TransitionRejection
}

public abstract record ConstraintSource(ConstraintSourceKind Kind, int? SourceLine = null)
{
    public sealed record InvariantSource(string ExpressionText, string Reason, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.Invariant, SourceLine);

    public sealed record StateAssertionSource(string ExpressionText, string Reason,
        string StateName, AssertAnchor Anchor, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.StateAssertion, SourceLine);

    public sealed record EventAssertionSource(string ExpressionText, string Reason,
        string EventName, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.EventAssertion, SourceLine);

    public sealed record TransitionRejectionSource(string Reason, string EventName, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.TransitionRejection, SourceLine);
}
```

Dropped from `ConstraintSourceKind`:
- `WhenGuard` — guards are routing logic, not constraints (Scenario 8).
- `InputContract`, `Editability`, `Compatibility` — deferred until those scenarios are designed.

### ``ConstraintViolation`` — the unit of feedback

```csharp
public sealed record ConstraintViolation(
    string Message,
    ConstraintSource Source,
    IReadOnlyList<ConstraintTarget> Targets);
```

`SourceKind` dropped — redundant with `Source.Kind`. The consumer can switch on `violation.Source.Kind` or pattern-match on the subtype.

No `Confidence` field. The consumer knows what its inputs are and can trivially determine whether a target is inline-renderable:

```
for each violation:
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

### Outcome enums

Two separate enums for the two operation paths, with `IsSuccess` on result types:

```csharp
public enum TransitionOutcome
{
    // Success
    Transition,              // state A → state B
    NoTransition,            // event processed, no state change

    // Failure
    Rejected,                // author's explicit reject
    ConstraintFailure,       // constraints failed post-mutation
    Unmatched,               // rows exist but no guard matched
    Undefined                // no rows exist for this event/state
}

public enum UpdateOutcome
{
    // Success
    Update,                  // field edit succeeded

    // Failure
    ConstraintFailure,       // constraints failed post-edit
    UneditableField,         // field not editable in current state
    InvalidInput             // wrong type, unknown field, etc.
}
```

### Result types

All result types carry `IReadOnlyList<ConstraintViolation> Violations` (replacing the old `IReadOnlyList<string> Reasons`) and expose `bool IsSuccess`:

```csharp
public sealed record FireResult(
    TransitionOutcome Outcome,
    string PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is TransitionOutcome.Transition
                          or TransitionOutcome.NoTransition;
}

public sealed record EventInspectionResult(
    TransitionOutcome Outcome,
    string CurrentState,
    string EventName,
    string? TargetState,
    IReadOnlyList<string> RequiredEventArgumentKeys,
    IReadOnlyList<ConstraintViolation> Violations)
{
    public bool IsSuccess => Outcome is TransitionOutcome.Transition
                          or TransitionOutcome.NoTransition;
}

public sealed record InspectionResult(
    string CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<EventInspectionResult> Events,
    IReadOnlyList<PreceptEditableFieldInfo>? EditableFields = null);

public sealed record UpdateResult(
    UpdateOutcome Outcome,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is UpdateOutcome.Update;
}
```

`PreceptViolation` is deleted — `ConstraintViolation` subsumes it entirely.

### Naming conventions

**Prefix rule (middle-ground):** Keep `Precept` on types whose bare name is too generic for C# (`PreceptField`, `PreceptState`, `PreceptEvent`, `PreceptInstance`, `PreceptDefinition`, `PreceptRuntime`, `PreceptEngine`, `PreceptCompiler`, `PreceptInvariant`, `PreceptTransitionRow`, `PreceptEditableFieldInfo`). Drop it on domain-specific types (`FireResult`, `EventInspectionResult`, `ConstraintViolation`, `TransitionOutcome`, `AssertAnchor`, etc.).

**Model type renames:**
- `PreceptStateAssert` → `StateAssertion`
- `PreceptEventAssert` → `EventAssertion`
- `PreceptAssertPreposition` → `AssertAnchor` (members: `In`, `To`, `From`)
- `PreceptRejection` → `Rejection`
- `PreceptStateTransition` → `StateTransition`
- `PreceptNoTransition` → `NoTransition`

**Compile-time vs runtime vocabulary:**
- "Constraints" = runtime data rules (invariants, assertions, rejections) that produce `ConstraintViolation`
- "Diagnostics" = compile-time DSL checks (`DiagnosticCatalog`, `ValidationResult`)

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
Source:     EventAssertion
Targets:    [ EventArg("MakePayment", "Amount"), Event("MakePayment") ]
```

Event asserts only reference event args. The user provided those args. The consumer shows this inline on the Amount input. The `Event` scope target tells the consumer which event this constraint belongs to.

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
Source:     Invariant
Targets:    [ Field("Balance"), Definition() ]
```

The consumer checks: is `Field("Balance")` one of my inputs? No — the user typed `Amount`. The `Definition()` scope confirms this is a global invariant. Shows as a banner/event-level error.

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
Source:     StateAssertion
Targets:    [ Field("AssignedAgent"), State("Assigned", In) ]
```

The consumer checks: is `Field("AssignedAgent")` one of my inputs? No — StartWork has no args. The `State("Assigned", In)` scope tells the consumer this is about entering Assigned. Shows as a banner: "Cannot enter Assigned: must have an assigned agent."

### Scenario 4: `when` guard fails, fallback row is `reject`

```precept
precept Loan

field CreditScore as number default 0
field CollateralVerified as boolean default false
field RequestedAmount as number default 0
field ApprovedAmount as number default 0

state UnderReview initial
state Approved

event Approve with ApprovedAmount as number

from UnderReview on Approve
    when (CollateralVerified || CreditScore >= 700) && Approve.ApprovedAmount <= RequestedAmount
    -> set ApprovedAmount = Approve.ApprovedAmount
    -> transition Approved

from UnderReview on Approve
    -> reject "Approval requires collateral or credit score >= 700 and valid amount"
```

`Inspect(instance, "Approve", { ApprovedAmount: 40000 })` when `CollateralVerified = false`, `CreditScore = 500`, `RequestedAmount = 50000`.

Row 1's `when` guard: `(false || 500 >= 700) && 40000 <= 50000` → `false`. Skipped.
Row 2: unguarded, matches. Outcome is `reject`.

```
Message:    "Approval requires collateral or credit score >= 700 and valid amount"
Source:     TransitionRejection
Targets:    [ Event("Approve") ]
```

The reject is a routing decision by the author — a literal string, not a predicate over data. No expression-referenced subjects. The only target is the event scope. The consumer shows this as an event-level banner.

## Scenarios (Remaining)

### Scenario 5: All `when` guards fail → `Unmatched`

```precept
precept Document

field ExpiryExtended as boolean default false

state Expired initial
state Signing

event ExtendExpiry

from Expired on ExtendExpiry when !ExpiryExtended
    -> set ExpiryExtended = true
    -> transition Signing
```

`Inspect(instance, "ExtendExpiry")` when `ExpiryExtended = true`.

One row for `(Expired, ExtendExpiry)` with a `when` guard. Guard: `!true` → `false`. No row matches. Result: `Unmatched`.

**No `ConstraintViolation` is emitted.** `Unmatched` is a disposition, not a failure. The event simply doesn't apply right now. The caller gets the disposition and can render accordingly (e.g., grayed-out button).

### Scenario 6: Field edit fails an invariant

```precept
precept Settings

field Priority as number default 3
invariant Priority >= 1 && Priority <= 5 because "Priority must be between 1 and 5"

state Open initial

in Open edit Priority
```

`Inspect(instance, patch => patch.Set("Priority", 99))`

The patch sets Priority to 99. Simulation runs. Invariant `Priority >= 1 && Priority <= 5` fails.

```
Message:    "Priority must be between 1 and 5"
Source:     Invariant
Targets:    [ Field("Priority"), Definition() ]
```

The consumer (in edit mode) checks: is `Field("Priority")` one of my inputs? Yes — the user is editing Priority. Shows inline on the Priority input with the reason.

## Scenarios (Remaining)

### Scenario 7: Multi-subject invariant (cross-field)

```precept
precept TimeWindow

field StartHour as number default 0
field EndHour as number default 24
invariant StartHour <= EndHour because "Start must not exceed end"

state Active initial

in Active edit StartHour, EndHour
```

**Case A:** `Inspect(instance, patch => patch.Set("StartHour", 20))` when `EndHour = 10`.

Simulation: `StartHour = 20`, `EndHour = 10`. Invariant `StartHour <= EndHour` fails.

```
Message:    "Start must not exceed end"
Source:     Invariant
Targets:    [ Field("StartHour"), Field("EndHour"), Definition() ]
```

Both fields appear as targets because the expression references both. The runtime always reports all expression subjects regardless of what the consumer patched. The consumer knows its own patch and can decide rendering — e.g. attach inline to `StartHour` (which it edited) and show `EndHour` as context.

**Case B:** `Inspect(instance, patch => patch.Set("StartHour", 20).Set("EndHour", 10))`

Same invariant fails, same violation, same targets. The consumer can now attach the violation to both inputs.

**Rule:** The runtime always returns the full set of expression-referenced targets. All Inspect paths behave identically — no filtering by patch, event, or overload. The consumer decides how to render based on what it knows about its own inputs.

### Scenario 8: Mixed `when` guard (fields + args)

```precept
precept LoanApplication

field CreditScore as number
field AnnualIncome as number
field RequestedAmount as number
field DocumentsVerified as boolean default false
field ExistingDebt as number default 0

event Approve with Amount as number, Note as string

state UnderReview
from UnderReview on Approve
    when DocumentsVerified && CreditScore >= 680
         && AnnualIncome >= ExistingDebt * 2
         && RequestedAmount < AnnualIncome / 2
         && Approve.Amount <= RequestedAmount
    -> set ApprovedAmount = Approve.Amount
    -> transition Approved
from UnderReview on Approve reject "Approval criteria not met"
```

**Situation:** `Inspect(instance, "Approve", { Amount = 500000 })` when `CreditScore = 600, DocumentsVerified = false`.

The `when` guard evaluates to false. The row is skipped. The next row is the `reject` fallback, which fires.

```
Message:    "Approval criteria not met"
Source:     TransitionRejection
Targets:    [ Event("Approve") ]
```

The `when` guard's expression subjects (CreditScore, DocumentsVerified, etc.) never become targets — because the guard is routing logic, not a constraint. It didn't "fail"; it just didn't match.

**Rule:** `when` guards never directly produce ConstraintViolations. They are control flow. Only the outcomes of the row that fires (or the explicit `reject` fallback) produce violations. If no row matches and there is no reject fallback, the result is `Unmatched` with no violation (Scenario 5).

## Design Rationale

Key decisions and the reasoning behind them, recorded during the naming consistency review session.

### Why `Rejected` and `ConstraintFailure` are separate outcomes

The old `Rejected` outcome was overloaded — it covered both author-intentional rejects (`reject "reason"`) and automatic constraint failures (invariant/state assert violations post-mutation). These are fundamentally different:

- **`Rejected`** = the author wrote an explicit reject as a routing decision. The message is a literal string, not a predicate over data. The user can't "fix" the input to avoid it — it's the author's declared outcome for that path.
- **`ConstraintFailure`** = a data constraint (invariant or state assert) failed after the mutation was simulated. The user might be able to change inputs to avoid it.

Consumers need to distinguish these: a `Rejected` outcome should display the author's message as a definitive "no"; a `ConstraintFailure` should display which constraints failed and what they're about, potentially with inline field-level feedback.

Event asserts use `Rejected` (not `ConstraintFailure`) because they are author-intentional pre-checks on args — semantically equivalent to a reject. They fire before row selection, their expressions reference only event args, and the author wrote them with a `because` reason. `ConstraintFailure` is reserved for post-mutation constraint violations.

### Why `NoTransition` replaced `AcceptedInPlace`

The old name `AcceptedInPlace` was misleading. Source code confirmed that when this outcome fires, **no transition actually occurs** — the event is processed (mutations apply), but the state machine stays in the same state. The "Accepted" prefix suggested a transition happened; "InPlace" was vague.

`NoTransition` directly describes the outcome: the event was processed, no state change occurred. This also avoids past-tense issues (see below).

### Why tense-neutral noun forms for enum values

`Inspect` is a predictive API — it reports what *would* happen if an event were fired. Past-tense values like `Accepted` and `Rejected` read as descriptions of events that already occurred, which is semantically wrong for Inspect results. Noun forms (`Transition`, `NoTransition`, `Rejection`, `Unmatched`) work in both predictive and past-tense contexts:

- Fire result: "The outcome was `Transition`" (it happened)
- Inspect result: "The outcome would be `Transition`" (it would happen)

### Why `TransitionOutcome` over `EventOutcome`

"Event" collides with the `event` keyword and `PreceptEvent` type in the DSL. An outcome enum named `EventOutcome` would be ambiguous — does it describe the outcome of processing an event, or is it a kind of event? `TransitionOutcome` is unambiguous because "transition" is the noun for state-machine movement, which is exactly what this enum describes.

### Why `AssertAnchor`

The prepositions `in`, `to`, and `from` describe **spatial anchoring** relative to a state:

- `in Active` = while anchored in that state
- `to Submitted` = anchoring to that state (entering)
- `from Draft` = unanchoring from that state (leaving)

Alternatives considered:
- `AssertScope` — too broad, "scope" means many things in a language
- `AssertTiming` — temporal framing, but the prepositions aren't purely about time
- `AssertBinding` — implies a data binding relationship that doesn't exist

`AssertAnchor` captures the spatial metaphor of prepositions applied to state positions.

### Why `DiagnosticCatalog` (not `ConstraintCatalog`)

`ConstraintCatalog` was the original name. After introducing the runtime `ConstraintViolation` model, "constraint" became overloaded — it would mean both "compile-time DSL validity check" and "runtime data rule." These are distinct concepts:

- **Constraints** (runtime vocabulary): invariants, assertions, and rejections that the engine enforces at runtime, producing `ConstraintViolation` when they fail.
- **Diagnostics** (compile-time vocabulary): DSL syntax and semantic checks that the compiler/parser enforces during compilation, producing error diagnostics.

Renaming to `DiagnosticCatalog` separates the vocabularies cleanly. `ConstraintViolationException` stays as-is — it's thrown by the catalog infrastructure and represents a violation of a *language* constraint (a registered diagnostic), which is a different concept from a runtime `ConstraintViolation`.

### Why `ValidationResult` (not other alternatives)

`PreceptCompileValidationResult` was verbose and included "Validation," which clashes with the runtime vocabulary where "validation" was being removed in favor of "constraint." Alternatives considered:

- `CompileDiagnosticResult` — accurate but unnecessarily long for an internal type
- `CompilationResult` — slightly more formal than needed
- `ValidationResult` — clearer now that `PreceptCompiler.Validate()` is the primary structured compile-time entry point for diagnostics.

The current name matches the behavior better: the type represents structured validation output, not engine construction.

### Why the middle-ground prefix convention

Three approaches were evaluated:

1. **Prefix everything with `Precept`** — safe from collisions but creates stutter (`PreceptPreceptTarget` was the trigger for reconsidering)
2. **Prefix nothing** — clean but risks collision with common C# types (`Field`, `State`, `Event`, `Instance`)
3. **Middle-ground** — keep `Precept` on types whose bare names are too generic for C#; drop it on domain-specific types

The middle-ground was chosen. Types that keep `Precept`: `PreceptField`, `PreceptState`, `PreceptEvent`, `PreceptInstance`, `PreceptDefinition`, `PreceptRuntime`, `PreceptEngine`, `PreceptCompiler`, `PreceptInvariant`, `PreceptTransitionRow`, `PreceptEditableFieldInfo`. Types that drop it: `FireResult`, `EventInspectionResult`, `ConstraintViolation`, `TransitionOutcome`, `AssertAnchor`, `StateAssertion`, `EventAssertion`, `Rejection`, `StateTransition`, `NoTransition`, `ValidationResult`, etc.
