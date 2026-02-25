# StateMachine

A typed state machine library for .NET that combines finite state machine constraints with immutable data records, using a fluent builder pattern for construction.

## Design Intent

The goal is to provide a **single, self-contained object** that encapsulates both workflow state and business data for a domain entity — eliminating the common disconnect between "what state is this thing in?" and "what data does it carry?"

Traditional state machine libraries (like Stateless) manage state transitions but leave data management to the consumer. This creates a split where the domain object mutates freely outside the state machine's control, and the machine only governs which transitions are legal. That split is a source of bugs: data can be modified without going through a transition, and transitions can fire without updating data consistently.

This library takes a different approach: **the state machine owns the data**. Business data is stored as an immutable C# record. Every transition produces a new record — the original is never mutated. The state and data are always consistent, always serializable as a single unit, and always under the machine's control.

### What This Enables

- **Business objects as state machines**: A work order, insurance claim, or loan application can be fully represented as a state machine with co-located data. The machine enforces what can happen, when, and how data changes as a result.
- **Deterministic transitions**: The outcome of any event can be evaluated before firing it (via `Inspect(...)`). Guards are pure functions of the current data and event arguments — no hidden external state.
- **Trivial persistence**: Since the entire machine state is one record, saving and restoring is just serializing/deserializing a single object. No separate "state" column plus "data" blob.
- **Audit trail by design**: Because each transition produces a new immutable record, keeping a history of transitions (with before/after snapshots) is trivial. Subscribe to `DataTransitioned` and you get full replay capability.
- **Testable business logic**: Guard conditions and data transforms are pure functions — they can be unit tested in isolation without constructing a state machine.

## Design Philosophy

This library treats a state machine as a **typed reducer** — each transition produces a new immutable data record, ensuring correctness, testability, and thread safety by design.

### Core Principles

- **Immutable data**: Business data is stored as C# records. Transitions produce new records via `with` expressions — the original is never mutated.
- **State on the record**: The state enum is a property on the data record, identified via an expression selector (`d => d.State`). This makes serialization and snapshotting trivial — one object = full machine state.
- **Pure transforms**: The `Transform()` method accepts a pure function `(TData) => TData` or `(TData, TArg) => TData`. Side effects (email, logging) are handled externally by subscribing to the `Transitioned` / `DataTransitioned` observation events.
- **Fluent builder**: The builder API uses interface narrowing so that only valid next steps are available at each point in the chain. The compiler enforces correct construction — you cannot define an incomplete or structurally invalid state machine.
- **Sequential enum constraint**: The `TState` enum must be contiguous and zero-based (e.g., `Off, Red, Green, Yellow` → 0, 1, 2, 3). This is validated once at build time, and enum values are then cast directly to `int` for O(1) array indexing with no boxing or lookup. Sparse or `[Flags]` enums are rejected immediately with a clear error message.
- **Sealed after build**: States and events cannot be added after construction. This enables a lightweight array-based transition table using enum ordinals for O(1) transition lookup — the same efficient data structure used in classical finite state machine implementations.
- **Thread-safe after build**: The built machine uses `lock` to ensure transitions are atomic (read state → evaluate guards → run transform → set new data). The builder itself is not thread-safe and is discarded after `Build()`.
- **Template-first model**: `Build()` produces an immutable template that can stamp out many instances via `CreateInstance(...)`.
- **Event tokens**: `On(out var approve)` captures the event name via `CallerArgumentExpression` and returns a strongly-typed event token that you pass to `Inspect(...)`.

### What the Machine Can Answer

The built machine is designed to answer these questions at runtime:

1. Which events can currently be fired, and which states will they transition to?
2. If an event cannot be triggered, why not — is it not defined for the current state, or is a guard failing (and which one)?
3. What are all the valid states and events, regardless of current state — enabling visualization of the full workflow graph?

## Two Modes

### Data-Less (state-only)

For simple workflows that only need to track state transitions with no associated data:

```csharp
enum TrafficLight { Red, Green, Yellow }

var machine = StateMachine.CreateBuilder<TrafficLight>()
    .On(out var next)
        .WhenStateIs(TrafficLight.Red)
        .TransitionTo(TrafficLight.Green)
        .WhenStateIs(TrafficLight.Green)
        .TransitionTo(TrafficLight.Yellow)
        .WhenStateIs(TrafficLight.Yellow)
        .TransitionTo(TrafficLight.Red)
    .Build()
    .CreateInstance(TrafficLight.Red);

machine.Inspect(next).IfAccepted().Fire();
// machine.State == TrafficLight.Green
```

### Data-Ful (state + immutable data)

For workflows that manage business data alongside state. Both modes start with `CreateBuilder<TState>()` — adding `.WithData<TData>(d => d.State)` attaches an immutable record:

```csharp
enum Status { New, Planned, Approved, Completed, Closed, Cancelled }

record Approval(string ApprovedBy, DateTime ApprovedOn);

record WorkOrderData(
    Status State,
    string CreatedBy,
    DateTime CreatedOn,
    Approval? Approval = null,
    DateTime? CompletedOn = null
);

var machine = StateMachine.CreateBuilder<Status>()
    .WithData<WorkOrderData>(d => d.State)

    .On(out var markAsPlanned)
        .WhenStateIs(Status.New)
        .TransitionTo(Status.Planned)

    .On<Approval>(out var approve)
        .WhenStateIs(Status.Planned)
        .If((data, a) => a.ApprovedBy != data.CreatedBy,
            "Cannot approve your own work orders")
        .Transform((data, a) => data with { Approval = a })
        .ThenTransitionTo(Status.Approved)

    .On(out var complete)
        .WhenStateIs(Status.Approved)
        .Transform(data => data with { CompletedOn = DateTime.Now })
        .ThenTransitionTo(Status.Completed)

    .On(out var cancel)
        .RegardlessOfState()
        .TransitionTo(Status.Cancelled)

    .Build()
    .CreateInstance(new WorkOrderData(Status.New, "Shane Falik", DateTime.Now));

// Use the event tokens returned via 'out' with Inspect(...)
machine.Inspect(markAsPlanned).IfAccepted().Fire();
// machine.State == Status.Planned
// machine.Data == WorkOrderData { State = Planned, ... }
```

## Key Features

### Guards

Events can have guard conditions that must pass before a transition is allowed. Guards receive the current data (and the event argument, if any). They are pure functions — no side effects, no external dependencies:

```csharp
.If((data, approval) => approval.ApprovedBy != data.CreatedBy, "Cannot self-approve")
```

Alternative branches can be defined with `.Else`:

```csharp
.If((data, time) => time > DateTime.Now, "Cannot start in the past")
    .Transform((data, time) => data with { ... })
    .ThenTransitionTo(Status.WorkStarted)
.Else
    .KeepSameState()
```

Every guard requires a reason string — this ensures that when a trigger is rejected, the caller always receives an explanation via `inspection.Reasons`.

### Pre-Check with Inspect (Fluent)

`Inspect` evaluates definition + guards without firing the transition (dry-run), making the state machine **deterministic** — the outcome of any event can be inspected ahead of time:

```csharp
var arg = new Approval("Manager", DateTime.Now);
machine.Inspect(approve, arg)
    .IfAccepted(nextState => Console.WriteLine($"Will transition to {nextState}"))
    .Fire()
    .Else(reasons => Console.WriteLine(string.Join(", ", reasons)));
```

`Inspect(trigger, arg)` evaluates both definition and guards in one call. Use it whenever the argument is already available.

Use `Inspect(trigger)` (no argument) when you need to **branch on definition before building the argument** — for example when the argument is expensive to construct:

```csharp
machine.Inspect(approve)
    .IfDefined(() => Console.WriteLine("approve is valid in current state"))
    .Else(() => Console.WriteLine("approve is not valid in current state"))
    .WithArg(BuildExpensiveApproval()) // only evaluated if IsDefined is true
        .IfAccepted(() => Audit("approved"))
        .Fire()
        .Else(reasons => Console.WriteLine(string.Join(", ", reasons)));
```

`WithArg(...)` is the bridge from definition-only inspection to full guard evaluation — it transitions the chain from `PartialEventInspection` to `EventInspection`. Without `IfDefined`/`IfNotDefined` branching before it, prefer `Inspect(trigger, arg)` directly.

`Inspect` is still a pre-check. In concurrent scenarios, state may change between `Inspect()` and `Fire()`. In that case `Fire()` throws `StaleStateException`.

### When to Use Inspect

Use **fluent `Inspect(...)`** when your caller needs explicit accepted/rejected handling, rejection reasons, or definition checks before providing a parameterized argument.

Practical rule of thumb:

- **Use `Inspect(...).IfAccepted(...).Fire().Else(...)`** in API endpoints, UI command handlers, or orchestration code where rejected transitions are part of normal control flow.
- **Use `Inspect(trigger, arg)`** when the argument is already available — this evaluates both definition and guards in one call.
- **Use `Inspect(trigger).IfDefined(...).Else(...).WithArg(arg)`** only when argument construction is expensive and you want to gate it behind the definition check.

### Transition Observation

Subscribe to state changes for side effects like logging, notifications, or persistence. This is the intended mechanism for side effects — keeping transforms pure while allowing external reactions:

```csharp
// State-level observation (available on all machines)
machine.Transitioned += args =>
{
    Console.WriteLine($"{args.EventName}: {args.FromState} → {args.ToState}");
};

// Data-level observation (available on data-ful machines)
machine.DataTransitioned += args =>
{
    SaveToDatabase(args.NewData);
    if (args.OldData.Approval != args.NewData.Approval)
        SendApprovalNotification(args.NewData);
};
```

Callbacks fire after the transition commits. Treat the event args as the authoritative snapshot; handlers should be fast and non-blocking. Long-running work (sending emails, calling APIs) should be queued from the callback, not performed inline.

### Immutability Guarantees

Since `TData` is a C# record, the machine enforces immutability structurally:

- The user's `Execute` transform receives the current data and returns a new record
- The machine then stamps the new state onto the record (overwriting any state the user may have set in their transform)
- The old record is untouched — consumers holding a reference to previous data see no changes

This means **there is no way to modify the machine's data except through a transition**.

### Async Workflows (Saga Pattern)

This library intentionally does not support `async` transitions. Because transforms are pure functions (`(TData) => TData`), they are inherently synchronous — there is nothing to `await`.

When a workflow requires an asynchronous step (calling an external API, waiting for human approval, running a long computation), model it as **intermediate states** with the async work happening *between* transitions:

```csharp
// States include intermediate "pending" states for async steps
enum Status { Draft, PendingValidation, Validated, PendingApproval, Approved, Rejected }

record WorkOrder(Status Status, string Description, string? ValidationResult = null, string? Approver = null);

var machine = StateMachine.CreateBuilder<Status>()
    .WithData<WorkOrder>(d => d.Status)

    // Synchronous: move to a "waiting" state
    .On(out var submit)
        .WhenStateIs(Status.Draft)
        .TransitionTo(Status.PendingValidation)

    // Async result arrives later as a separate event
    .On<string>(out var validationSucceeded)
        .WhenStateIs(Status.PendingValidation)
        .Transform((data, result) => data with { ValidationResult = result })
        .ThenTransitionTo(Status.Validated)

    .On<string>(out var validationFailed)
        .WhenStateIs(Status.PendingValidation)
        .Transform((data, reason) => data with { ValidationResult = reason })
        .ThenTransitionTo(Status.Draft)

    .On<string>(out var approve)
        .WhenStateIs(Status.Validated)
        .Transform((data, approver) => data with { Approver = approver })
        .ThenTransitionTo(Status.Approved)

    .On(out var reject)
        .WhenStateIs(Status.Validated)
        .TransitionTo(Status.Rejected)

    .Build()
    .CreateInstance(new WorkOrder(Status.Draft, "Install HVAC"));
```

The orchestrator (a background service, message handler, or saga coordinator) drives the workflow by subscribing to observation events and firing the next event when the async work completes:

```csharp
machine.DataTransitioned += args =>
{
    if (args.NewData.Status == Status.PendingValidation)
    {
        // Kick off async work outside the machine
        Task.Run(async () =>
        {
            try
            {
                var result = await externalValidator.ValidateAsync(args.NewData);
                machine.Inspect(validationSucceeded, result).IfAccepted().Fire();
            }
            catch (Exception ex)
            {
                machine.Inspect(validationFailed, ex.Message).IfAccepted().Fire();
            }
        });
    }
};

// Start the workflow
machine.Inspect(submit).IfAccepted().Fire();
```

This keeps the state machine purely synchronous while the saga layer handles async coordination. Each pending state is explicitly visible in the state enum, making it easy to query, persist, and resume workflows.

## Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Instance model | Build immutable template, then stamp instances | Define once; create many identical machines |
| Data ownership | Machine owns immutable `TData` record | Prevents external mutation, trivial serialization |
| State location | Property on `TData`, identified by expression selector | Single source of truth |
| Undefined transition | `Inspect(...)` rejects (`IsDefined == false`) | Lets callers handle invalid events as control flow |
| All guards fail without Else | `Inspect(...)` rejects with aggregated reasons | Provides actionable feedback |
| Build-time validation | Error on empty events and duplicate transitions | Catch construction mistakes early |
| Enum constraint | `TState` must be contiguous and zero-based | Enables direct cast to `int` for O(1) array indexing — no `Array.IndexOf`, no boxing |
| Transform semantics | Pure: `(TData) => TData` | Testable, no hidden dependencies |
| Transform vs state order | Transform first, then state change | If transform throws, nothing changes |
| Thread safety | `lock` around full transition; sealed after build | Sync transforms keep it simple; O(1) array lookup |
| Pre-check API | `Inspect(...)` fluent chain | Clear accepted/rejected flow, explicit reasons, optional `Fire()` |
| Async events | Not supported | Pure transforms are synchronous; async side effects go in observers |
| Side effects | Via `Transitioned` / `DataTransitioned` observation events | Keeps transforms pure, decouples concerns |
| Guard reasons | Required on every guard | Ensures rejected events always explain why |

## Comparison with Existing Libraries

| Library | State Machine | Immutable Data | Guards | Fluent Builder | Pure Transforms |
|---|---|---|---|---|---|
| **Stateless** (C#) | Yes | No | Yes | Yes | No |
| **MassTransit Automatonymous** | Yes | No | Yes | Yes | No |
| **XState** (JS) | Yes | Yes (context) | Yes | No (JSON config) | Yes (assign) |
| **This library** | Yes | Yes | Yes | Yes | Yes |

The closest analogue is **XState** in the JavaScript ecosystem. This library brings that concept to .NET with compile-time type safety and a fluent construction API. The key differentiator is the combination of **immutable records as the data model** with **a fluent builder that enforces correct construction at compile time**.

## Project Structure

```
src/StateMachine/
    Interfaces.cs          — Public interfaces, event types, exceptions, fluent builder contracts
    Inspection.cs          — Inspect API chain types (`EventInspection`, `AcceptedChain`, etc.)
    StateMachine.cs        — Entry point, machine implementations, builder stubs
    FiniteStateMachine.cs  — Legacy implementation (two type parameters: TState + TEvent)
    IStateful.cs           — Legacy interface

test/StateMachine.Tests/
    StateMachineTests.cs   — Tests for the new fluent builder API
    FiniteStateMachineTests.cs — Tests for the legacy implementation
```