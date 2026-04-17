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

1. **The runtime reports the semantic subjects a violated rule is about, not just the literal tokens it mentions and not the operation inputs that happened to feed them.** Directly referenced stored fields remain targets. If a rule references a computed field, that computed field is also a target, and the runtime expands it transitively to the concrete stored fields it depends on. This broadens targeting from direct syntax to dependency-aware subject attribution while preserving the same non-goal as today: the runtime does not reverse-map through mutations or blame event arguments for a post-mutation field rule unless the rule itself is an event ensure.

2. **The consumer decides rendering.** The runtime provides targets. The consumer knows what its current inputs are and can trivially check: "is this target one of my inputs? → inline. Otherwise → banner."

3. **No UI-leaking terms in the runtime model.** No "form," no "inline," no "banner." Just targets and sources.

4. **Keep `Inspect` as the method name.** Three overloads differentiated by signature (aggregate, event, update) stay as they are.

5. **Every scoped constraint includes its scope as a target.** Event ensures include `Event(name)`. State ensures include `State(name, anchor)`. Rules include `Definition()`. This is uniform — every violation carries both its semantic subjects (what's wrong) and its scope (why this constraint exists).

## Four Kinds of Constraints

A precept is a collection of rules among other things. Rules, ensures, and rejects are all constraints — each expresses a condition the engine enforces. When a constraint is violated, the engine reports a `ConstraintViolation`.

### A. Event ensures

```precept
on MakePayment ensure Amount > 0 because "Amount must be positive"
```

- **Scope:** Event args only (enforced at parse time).
- **When checked:** Pre-transition, before row selection.
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Expression-referenced arg(s) + `Event(name)`.

### B. Rules

```precept
rule Balance >= 0 because "Balance cannot go negative"
```

- **Scope:** Fields only.
- **When checked:** Post-mutation, pre-commit. Always — every fire and every update.
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Directly referenced field(s), any transitive field dependencies beneath referenced computed fields, + `Definition()`.

### C. State ensures

```precept
in Assigned ensure AssignedAgent != null because "Must have an assigned agent"
from Draft ensure Email != null because "Must provide email before submitting"
to Submitted ensure Items.count > 0 because "Must have items to submit"
```

- **Scope:** Fields only.
- **When checked:** Post-mutation, pre-commit. Temporally scoped by preposition (`in` = while residing, `to` = entering, `from` = leaving).
- **Author-supplied reason:** Yes (`because`).
- **Targets:** Directly referenced field(s), any transitive field dependencies beneath referenced computed fields, + `State(name, anchor)`.

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

Every scoped constraint includes its scope alongside its semantic subjects:

| Source | Targets |
|---|---|
| Event ensure | Expression-referenced arg(s) + `Event(name)` |
| Rule | Directly referenced field(s) + transitive dependencies beneath referenced computed fields + `Definition()` |
| State ensure | Directly referenced field(s) + transitive dependencies beneath referenced computed fields + `State(name, anchor)` |
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

    public sealed record StateTarget(string StateName, EnsureAnchor? Anchor = null)
        : ConstraintTarget(ConstraintTargetKind.State);

    public sealed record DefinitionTarget()
        : ConstraintTarget(ConstraintTargetKind.Definition);
}

public enum EnsureAnchor { In, To, From }
```

### `ConstraintSource` — where it came from

```csharp
public enum ConstraintSourceKind
{
    Rule,
    StateEnsure,
    EventEnsure,
    TransitionRejection
}

public abstract record ConstraintSource(ConstraintSourceKind Kind, int? SourceLine = null)
{
    public sealed record RuleSource(string ExpressionText, string Reason, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.Rule, SourceLine);

    public sealed record StateEnsureSource(string ExpressionText, string Reason,
        string StateName, EnsureAnchor Anchor, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.StateEnsure, SourceLine);

    public sealed record EventEnsureSource(string ExpressionText, string Reason,
        string EventName, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.EventEnsure, SourceLine);

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

At compile time, walk each constraint's expression AST to record two layers of subject data: the direct subjects named by the expression itself, and the expanded subjects produced by walking any referenced computed fields through the computed-field dependency graph.

```csharp
public sealed record ExpressionSubjects(
    IReadOnlyList<string> DirectFieldReferences,
    IReadOnlyList<string> ExpandedFieldReferences,
    IReadOnlyList<(string Event, string Arg)> ArgReferences);
```

- `PreceptIdentifierExpression` with no dot in field scope → direct field reference.
- `PreceptIdentifierExpression` with no dot in event-ensure scope → event arg reference for that event.
- `PreceptIdentifierExpression` with dot (`Event.Arg`) → explicit event arg reference.
- Any directly referenced computed field contributes itself to the direct subject set and contributes its full transitive stored-field dependency closure to the expanded field set.

This is computed once in `PreceptCompiler` and stored alongside each rule, state ensure, event ensure, and transition row's `WhenGuard`. At inspect time, the runtime looks up the precomputed subjects and returns a de-duplicated union of direct and expanded subjects in stable dependency order, then appends the scope target — no expression re-parsing needed.

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

**Prefix rule (middle-ground):** Keep `Precept` on types whose bare name is too generic for C# (`PreceptField`, `PreceptState`, `PreceptEvent`, `PreceptInstance`, `PreceptDefinition`, `PreceptRuntime`, `PreceptEngine`, `PreceptCompiler`, `PreceptRule`, `PreceptTransitionRow`, `PreceptEditableFieldInfo`). Drop it on domain-specific types (`FireResult`, `EventInspectionResult`, `ConstraintViolation`, `TransitionOutcome`, `EnsureAnchor`, etc.).

**Model type renames:**
- `PreceptStateAssert` → `StateEnsure` *(done)*
- `PreceptEventAssert` → `EventEnsure` *(done)*
- `PreceptAssertPreposition` → `EnsureAnchor` (members: `In`, `To`, `From`) *(done)*
- `PreceptRejection` → `Rejection`
- `PreceptStateTransition` → `StateTransition`
- `PreceptNoTransition` → `NoTransition`

**Compile-time vs runtime vocabulary:**
- "Constraints" = runtime data rules (rules, ensures, rejections) that produce `ConstraintViolation`
- "Diagnostics" = compile-time DSL checks (`DiagnosticCatalog`, `ValidationResult`)

## Scenarios (Agreed)

### Scenario 1: Event ensure fails

```precept
precept Payment

field Balance as number default 100

state Active initial

event MakePayment with Amount as number
on MakePayment ensure Amount > 0 because "Amount must be positive"

from Active on MakePayment
    -> set Balance = Balance - MakePayment.Amount
    -> no transition
```

`Inspect(instance, "MakePayment", { Amount: 0 })`

```
Message:    "Amount must be positive"
Source:     EventEnsure
Targets:    [ EventArg("MakePayment", "Amount"), Event("MakePayment") ]
```

Event ensures only reference event args. The user provided those args. The consumer shows this inline on the Amount input. The `Event` scope target tells the consumer which event this constraint belongs to.

### Scenario 2: Post-mutation rule fails during event inspection

```precept
precept Payment

field Balance as number default 1000
rule Balance >= 0 because "Balance cannot go negative"

state Active initial

event MakePayment with Amount as number
on MakePayment ensure Amount > 0 because "Amount must be positive"

from Active on MakePayment
    -> set Balance = Balance - MakePayment.Amount
    -> no transition
```

`Inspect(instance, "MakePayment", { Amount: 5000 })` when `Balance` is `1000`.

Event ensure passes. Row matches. Simulation: `Balance = 1000 - 5000 = -4000`. Rule fails.

```
Message:    "Balance cannot go negative"
Source:     Rule
Targets:    [ Field("Balance"), Definition() ]
```

The consumer checks: is `Field("Balance")` one of my inputs? No — the user typed `Amount`. The `Definition()` scope confirms this is a global rule. Shows as a banner/event-level error.

The runtime does not reverse-map through mutations. If the author wants arg-level feedback, they write a `when` guard or an event ensure.

### Scenario 3: State ensure fails on entry

```precept
precept Ticket

field AssignedAgent as string nullable
field Priority as number default 3

state Open initial
state Assigned
in Assigned ensure AssignedAgent != null because "Must have an assigned agent"

event StartWork

from Open on StartWork -> transition Assigned
```

`Inspect(instance, "StartWork")` when `AssignedAgent` is `null`.

Row matches. No mutations. Entry into Assigned triggers `in Assigned ensure AssignedAgent != null`. Fails.

```
Message:    "Must have an assigned agent"
Source:     StateEnsure
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

### Scenario 6: Field edit fails a rule

```precept
precept Settings

field Priority as number default 3
rule Priority >= 1 && Priority <= 5 because "Priority must be between 1 and 5"

state Open initial

in Open edit Priority
```

`Inspect(instance, patch => patch.Set("Priority", 99))`

The patch sets Priority to 99. Simulation runs. Rule `Priority >= 1 && Priority <= 5` fails.

```
Message:    "Priority must be between 1 and 5"
Source:     Rule
Targets:    [ Field("Priority"), Definition() ]
```

The consumer (in edit mode) checks: is `Field("Priority")` one of my inputs? Yes — the user is editing Priority. Shows inline on the Priority input with the reason.

## Scenarios (Remaining)

### Scenario 7: Multi-subject rule (cross-field)

```precept
precept TimeWindow

field StartHour as number default 0
field EndHour as number default 24
rule StartHour <= EndHour because "Start must not exceed end"

state Active initial

in Active edit StartHour, EndHour
```

**Case A:** `Inspect(instance, patch => patch.Set("StartHour", 20))` when `EndHour = 10`.

Simulation: `StartHour = 20`, `EndHour = 10`. Rule `StartHour <= EndHour` fails.

```
Message:    "Start must not exceed end"
Source:     Rule
Targets:    [ Field("StartHour"), Field("EndHour"), Definition() ]
```

Both fields appear as targets because the expression references both. The runtime always reports all expression subjects regardless of what the consumer patched. The consumer knows its own patch and can decide rendering — e.g. attach inline to `StartHour` (which it edited) and show `EndHour` as context.

**Case B:** `Inspect(instance, patch => patch.Set("StartHour", 20).Set("EndHour", 10))`

Same rule fails, same violation, same targets. The consumer can now attach the violation to both inputs.

**Rule:** The runtime always returns the full semantic dependency target set for the violated rule. All Inspect paths behave identically — no filtering by patch, event, or overload. For field-based rules, that means directly referenced fields plus any transitive field dependencies beneath referenced computed fields; for event ensures, it remains the expression-referenced event args. The consumer decides how to render based on what it knows about its own inputs.

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

The old `Rejected` outcome was overloaded — it covered both author-intentional rejects (`reject "reason"`) and automatic constraint failures (rule/state ensure violations post-mutation). These are fundamentally different:

- **`Rejected`** = the author wrote an explicit reject as a routing decision. The message is a literal string, not a predicate over data. The user can't "fix" the input to avoid it — it's the author's declared outcome for that path.
- **`ConstraintFailure`** = a data constraint (rule or state ensure) failed after the mutation was simulated. The user might be able to change inputs to avoid it.

Consumers need to distinguish these: a `Rejected` outcome should display the author's message as a definitive "no"; a `ConstraintFailure` should display which constraints failed and what they're about, potentially with inline field-level feedback.

Event ensures use `Rejected` (not `ConstraintFailure`) because they are author-intentional pre-checks on args — semantically equivalent to a reject. They fire before row selection, their expressions reference only event args, and the author wrote them with a `because` reason. `ConstraintFailure` is reserved for post-mutation constraint violations.

### Why `NoTransition` replaced `AcceptedInPlace`

The old name `AcceptedInPlace` was misleading. Source code confirmed that when this outcome fires, **no transition actually occurs** — the event is processed (mutations apply), but the state machine stays in the same state. The "Accepted" prefix suggested a transition happened; "InPlace" was vague.

`NoTransition` directly describes the outcome: the event was processed, no state change occurred. This also avoids past-tense issues (see below).

### Why tense-neutral noun forms for enum values

`Inspect` is a predictive API — it reports what *would* happen if an event were fired. Past-tense values like `Accepted` and `Rejected` read as descriptions of events that already occurred, which is semantically wrong for Inspect results. Noun forms (`Transition`, `NoTransition`, `Rejection`, `Unmatched`) work in both predictive and past-tense contexts:

- Fire result: "The outcome was `Transition`" (it happened)
- Inspect result: "The outcome would be `Transition`" (it would happen)

### Why `TransitionOutcome` over `EventOutcome`

"Event" collides with the `event` keyword and `PreceptEvent` type in the DSL. An outcome enum named `EventOutcome` would be ambiguous — does it describe the outcome of processing an event, or is it a kind of event? `TransitionOutcome` is unambiguous because "transition" is the noun for state-machine movement, which is exactly what this enum describes.

### Why `EnsureAnchor`

The prepositions `in`, `to`, and `from` describe **spatial anchoring** relative to a state:

- `in Active` = while anchored in that state
- `to Submitted` = anchoring to that state (entering)
- `from Draft` = unanchoring from that state (leaving)

Alternatives considered:
- `EnsureScope` — too broad, "scope" means many things in a language
- `EnsureTiming` — temporal framing, but the prepositions aren't purely about time
- `EnsureBinding` — implies a data binding relationship that doesn't exist

`EnsureAnchor` captures the spatial metaphor of prepositions applied to state positions.

### Why `DiagnosticCatalog` (not `ConstraintCatalog`)

`ConstraintCatalog` was the original name. After introducing the runtime `ConstraintViolation` model, "constraint" became overloaded — it would mean both "compile-time DSL validity check" and "runtime data rule." These are distinct concepts:

- **Constraints** (runtime vocabulary): rules, ensures, and rejections that the engine enforces at runtime, producing `ConstraintViolation` when they fail.
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

The middle-ground was chosen. Types that keep `Precept`: `PreceptField`, `PreceptState`, `PreceptEvent`, `PreceptInstance`, `PreceptDefinition`, `PreceptRuntime`, `PreceptEngine`, `PreceptCompiler`, `PreceptRule`, `PreceptTransitionRow`, `PreceptEditableFieldInfo`. Types that drop it: `FireResult`, `EventInspectionResult`, `ConstraintViolation`, `TransitionOutcome`, `EnsureAnchor`, `StateEnsure`, `EventEnsure`, `Rejection`, `StateTransition`, `NoTransition`, `ValidationResult`, etc.

---

## Compile-Phase Diagnostics for Structural Constraints

Compile-phase and parse-phase diagnostics (DSL validity checks) use a separate vocabulary from runtime `ConstraintViolation` objects. They are registered in `DiagnosticCatalog` and reported as structured `ParseDiagnostic` entries. The following codes are relevant to data-only (stateless) precepts:

| Code | Phase | Severity | Rule |
|------|-------|----------|------|
| C12 / PRECEPT012 | parse | Error | At least one `field` or `state` must be declared. A `precept` header alone with neither a field nor a state is invalid. |
| C49 / PRECEPT049 | compile | Warning | Event declared but never referenced in any transition row. On a stateless precept, every declared event is structurally orphaned \u2014 no state routing surface exists. Emitted per event. |
| C50 / PRECEPT050 | compile | Warning | Non-terminal state has outgoing rows but none can reach another state. Upgraded from `Hint` to `Warning` (2026-04-08). Rationale: a state where every outgoing path dead-ends is a structural smell that warrants author attention, not just an informational note. Consistent with the severity model for C49 (same kind of structural quality problem). |
| C55 / PRECEPT055 | compile | Error | Root-level `edit` is not valid when states are declared. Message: `"Root-level \`edit\` is not valid when states are declared. Use \`in any edit all\` or \`in <State> edit <Fields>\` instead."` |
| C69 / PRECEPT069 | compile | Error | Cross-scope guard reference in `when` clause. Guard expression references an identifier that belongs to a different scope than the declaration's guard allows. Rule/state-ensure/edit guards are entity-field-scoped; event-ensure guards are event-arg-scoped. |
| C70 / PRECEPT070 | parse | Error | Duplicate modifier on field or event argument declaration. Each modifier (`nullable`, `default`, `ordered`) may appear at most once per declaration. |
| C71 / PRECEPT071 | compile | Error | Unknown function name in expression. The identifier is not a recognized built-in function. |
| C72 / PRECEPT072 | compile | Error | Function called with incorrect number of arguments, or argument types do not match any overload. Message includes the function name and argument count. |
| C73 / PRECEPT073 | compile | Error | Function argument type mismatch. A specific parameter expects one type but received another. Message identifies the parameter and the expected vs. actual types. |
| C74 / PRECEPT074 | compile | Error | `round()` precision argument must be a non-negative integer literal. The second argument to `round(value, places)` cannot be a field reference or expression — it must be a compile-time constant like `2`. |
| C75 / PRECEPT075 | compile | Error | `pow()` exponent must be integer type. The second argument to `pow(base, exponent)` must resolve to `integer`, not `number` or `decimal`. This ensures totality — integer exponentiation always produces a finite result. |
| C76 / PRECEPT076 | compile | Error | `sqrt()` requires a non-negative argument. The argument may be negative at runtime. Proof sources: `nonnegative` or `positive` constraint, `rule Field >= 0`, state/event `ensure`, or `when` guard with `>= 0`. Also accepted: literal values ≥ 0 and `abs()` results. |
| C77 / PRECEPT077 | compile | Error | Function does not accept nullable arguments. The argument may be null at runtime. Add a null check (e.g., `field != null and ...`) before calling the function. Same pattern as C56 for string `.length`. |
| C78 / PRECEPT078 | compile | Error | Conditional expression condition must be a boolean expression. The `if` clause evaluates to a non-boolean type. |
| C79 / PRECEPT079 | compile | Error | Conditional expression branches must produce the same scalar type. The `then` and `else` branches return different types. |
| C80 / PRECEPT080 | parse | Error | A field cannot have both a default value and a derived expression. Use `default` for user-set fields or `->` for computed fields, not both. |
| C81 / PRECEPT081 | parse | Error | A nullable field cannot have a derived expression. Computed fields always produce a value and cannot be nullable. |
| C82 / PRECEPT082 | parse | Error | Multi-name field declarations cannot have a derived expression. Each computed field must be declared separately. |
| C83 / PRECEPT083 | compile | Error | Computed field expression references a nullable field. Computed fields must always produce a value — use only non-nullable fields or collection accessors that guarantee a result. |
| C84 / PRECEPT084 | compile | Error | Computed field expression references an event argument. Computed fields can only reference persistent fields and safe collection accessors. |
| C85 / PRECEPT085 | compile | Error | Computed field expression uses an unsafe collection accessor (`.peek`, `.min`, `.max`). Only `.count` is allowed in computed expressions. |
| C86 / PRECEPT086 | compile | Error | Circular dependency detected among computed fields. The cycle path is included in the message (e.g., "A → B → A"). |
| C87 / PRECEPT087 | compile | Error | Computed field cannot appear in edit declarations. Computed fields are read-only — the formula is the only authority on the field's value. |
| C88 / PRECEPT088 | compile | Error | Computed field cannot be assigned via set. Its value is always derived from the declared expression. |
| C92 / PRECEPT092 | compile | Error | Division by zero: the divisor is literal `0`. The compiler proves the divisor is always zero — this is unconditionally wrong. |
| C93 / PRECEPT093 | compile | Error | Divisor has no compile-time nonzero proof. Context-aware variant: if the field is `nonnegative`, the message explains that `nonnegative` allows zero and suggests `positive` instead. Generic variant suggests adding a `positive` constraint, `rule Field != 0`, or `when Field != 0` guard. Compound expressions are analyzed by the interval-arithmetic proof engine — see [ProofEngineDesign.md](ProofEngineDesign.md). See also `PreceptLanguageDesign.md` § Divisor safety for proof sources. |

**Distinction from runtime violations:** These are compile-time diagnostics, not runtime `ConstraintViolation` objects. They are reported during `PreceptCompiler.CompileFromText()` and surfaced via the language server (squiggles), MCP `precept_compile`, and CLI. They do not produce `ConstraintViolation` instances.

**C12 redefinition history:** The original C12 rule was "At least one state must be declared." It was broadened to include fields as part of the data-only precepts feature \u2014 a precept with only fields (no states) is now valid (a stateless precept), so the minimum requirement is at least one field OR at least one state.
