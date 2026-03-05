# Precept 🛡️

Precept is a .NET DSL-driven entity lifecycle engine focused on deterministic transition evaluation, strict data invariances (rules), and persisted instance state. 

## Why this project

Precept is for applications where entities move through explicit, long-lived lifecycles: support tickets, orders, approvals, onboarding flows, loan pipelines, or any process where "what state are we in, what data is valid, and what can happen next" must be unconditionally guaranteed.

Most teams start these workflows as scattered `if`/`switch` logic across handlers and services. That works at first, then drifts: states become implicit, transition rules are duplicated, data invariants are forgotten in one-off edits, and nobody can easily answer why an action is enabled in one case and blocked in another. Precept exists to make that lifecycle and its governing rules explicit, executable, and reviewable in one place.

The design philosophy is driven by domain integrity, not by language novelty:

- **Predictability over cleverness**: deterministic evaluation and declaration-ordered rules mean the same input produces the same outcome every time.
- **Safe introspection over hidden side effects**: `Inspect` works because expressions are pure; you can ask "what would happen" without mutating persisted state.
- **Consistency over syntax shortcuts**: statements mutate and expressions read. This keeps the mental model small and prevents context-specific exceptions.
- **Integrity over partial success**: transitions are atomic, so branch mutations either all commit or all roll back.
- **Rules as an absolute safety net**: Invariants (`rule`) are declared once and enforced on every mutation, whether moving through a transition or performing a direct data edit.

In practice, this library is a good fit when you need auditable, strict workflow behavior that both developers and domain stakeholders can read. The file becomes both a runtime contract and a living document, while persisted instances and explicit event arguments make integration straightforward for APIs, UIs, and background processing.

## Design Philosophy

Precept's DSL is shaped by a small number of principles that enforce strict domain integrity.

### Declare precepts (constraints) once

Guards (`when / if`) are per-transition—they route branch logic. But data invariants ("Balance must not go negative") are not per-transition; they are fundamental precepts about the data that must hold after every mutation, in every state, through every event. 

Precept elevates these data contracts to declarations via `rule <Expr> "<Reason>"`. The author states the invariant once (at the field, top-level, state, or event level), and the runtime enforces it after every mutation. Guards remain for routing logic (which path to take); rules act as the absolute safety net (is the result valid?). This separation is clean: guards answer "which path?", rules answer "is the result acceptable?".

### State and data live together

The instance is the single source of truth for both lifecycle state and associated data. A work order's `Notes`, `Priority`, and `Tags` live alongside its state—not in a separate data store that the host has to keep synchronized. Precept binds data to the lifecycle, ensuring they can only mutate in lockstep.

### Two mutation paths, one safety net

Not every data change is a lifecycle event. Updating a notes field is not the same as approving a loan. The event pipeline (declare event → define arguments → write `set` assignments → route with guards) is the right ceremony for lifecycle actions where routing, scoping, and audit matter. But for pure data editing, that ceremony is overhead.

Editable fields (`from <State> edit`) provide a second mutation path. This path eliminates event declaration, argument mapping, and `set` assignments for simple data updates. The two paths safely coexist because our core *precepts* (`rule` declarations) enforce data integrity regardless of how the data mutates. With rules acting as a net, direct editing is perfectly safe. 

### Expressions are pure; statements mutate

Evaluating a guard condition or a `set` right-hand side never changes state. You can evaluate `Floors.count > 0` a hundred times and it is untouched every time. Mutations—`add`, `remove`, `dequeue`, `set`—are explicit statements that only execute during an accepted `Fire`. This separation is the reason `Inspect` can safely preview any transition without side effects.

### Deterministic by construction

`set<T>` is backed by a sorted structure as a semantic guarantee. Iteration order, `.min`, `.max` are all deterministic regardless of insertion order. Guard evaluation follows declaration order. There are no race conditions, no nondeterministic outcomes, and no cases where running the same event with the same data produces different results.

Null safety is part of this guarantee. A field declared as `number?` can be `null`, and using it without checking is a compile-time error. When a branch tests `if X == null → reject`, ever subsequent branch in the chain knows statically that `X` is non-null.

### Atomic transitions and Read-your-writes

When a branch fires, all scalar assignments and collection mutations either commit together or roll back together. If a `dequeue` fails because the queue is empty, the entire branch is rejected. Within a single branch body, mutations are immediately visible to subsequent expressions (e.g., `set Count = Count + 1` followed by `set DoubleCount = Count * 2` works as expected). 

### Inspect before fire

Every transition can be evaluated read-only (`Inspect`) before committing (`Fire`). This enables UIs to show which events are defined, which are blocked by guards, and which are enabled—all without touching persisted state.

## Core Concepts (Proposed Nomenclature)

- **Precept Definition**: immutable compiled DSL (`PreceptDefinition`).
- **Instance**: persisted runtime state + data (`PreceptInstance`).
- **Inspect**: side-effect-free transition preview returning `Undefined | Blocked | NoTransition | Enabled`.
- **Fire**: applies state change, transition data assignments, and evaluates all relevant `rule` precepts.
- **Instance data**: persisted data loaded from/saved to instance JSON.

## Precept DSL Example

```text
machine TrafficLight

number VehiclesWaiting = 0
  rule VehiclesWaiting >= 0 "Vehicles waiting cannot be negative"
number CycleCount = 0
  rule CycleCount >= 0 "Cycle count cannot be negative"
boolean LeftTurnQueued = false
string? EmergencyReason

state Red initial
state FlashingGreen
state Green
state Yellow
state FlashingRed

event Advance
event Emergency
  string AuthorizedBy
    rule AuthorizedBy != "" "AuthorizedBy is required"
  string Reason
    rule Reason != "" "Reason is required"
    
event VehiclesArrive
  number Count = 5
    rule Count > 0 "Vehicle count must be positive"
event LeftTurnRequest
event ClearEmergency

from any on VehiclesArrive
  set VehiclesWaiting = VehiclesWaiting + VehiclesArrive.Count
```
