# Outcome Type Taxonomy Survey: How Systems Type Runtime Operation Results

**Date:** 2026-04-20  
**Author:** Newman (QA/Tester)  
**Research Angle:** External systems — how runtime operations surface outcome types, distinguishable failure modes, and partial-success representations  
**Purpose:** Raw collection of how real systems encode the full outcome space of firing an event, evaluating a policy, or transitioning state. No Precept conclusions drawn.

---

## Survey Dimensions (Per System)

For each system:
1. What are ALL distinct outcome types?
2. How are they represented in the type system?
3. Can the caller distinguish business-rule rejection vs. invalid input vs. infrastructure error vs. not-applicable?
4. What data travels with each outcome?
5. Is the outcome type the same for all operations, or per-operation?
6. How are partial successes represented?
7. Idiomatic caller pattern for handling each outcome?

---

## Temporal.io — Workflow Update, Signal, and Query Results

**Source:** pkg.go.dev/go.temporal.io/sdk/workflow (v1.42.0, fetched April 2026), Temporal SDK documentation

### Overview of Operation Types

Temporal exposes three communication mechanisms to a running workflow: **Signals** (fire-and-forget delivery of data), **Queries** (synchronous read of workflow state, no side effects), and **Updates** (synchronous request-response that can mutate state, with a pre-persistence validation phase).

### Update Outcomes

Updates are the richest outcome-bearing operation in Temporal. An Update call from a client goes through two phases on the server before the response is returned:

**Phase 1 — Validation (pre-persistence)**

A registered validator function runs before the Update is recorded to history. The validator has the signature:
```go
func Validator(ctx workflow.Context, arg YourUpdateArg) error
```
If the validator returns an error, the Update is **rejected** and no history event is written. This is a pre-persistence rejection — the call returns an error to the client but leaves the workflow history unchanged.

**Phase 2 — Handler execution (post-persistence)**

The handler function runs after the Update is recorded to history. The handler signature:
```go
func Handler(ctx workflow.Context, arg YourUpdateArg) (YourUpdateResult, error)
```
If the handler returns an error, the Update is **failed** — this IS recorded to history and returns an error to the client.

**Full Update outcome space (3 distinguishable outcomes):**

| Outcome | When It Occurs | History Written? | Client Receives |
|---------|---------------|------------------|-----------------|
| Success | Validator passes; handler returns `(result, nil)` | Yes | Typed result |
| Rejected | Validator returns non-nil error | No | Error (type: application error) |
| Failed | Validator passes; handler returns `(_, err)` | Yes | Error (type: application error) |

The distinction between Rejected and Failed matters because Rejected leaves the workflow in its prior state exactly, while Failed records the failure to durable history and the workflow continues from post-Update state. From a client perspective:

```go
updateHandle, err := client.UpdateWorkflow(ctx, UpdateWorkflowOptions{...})
if err != nil {
    // Network/scheduling error — workflow may not have seen the update at all
}
err = updateHandle.Get(ctx, &result)
if err != nil {
    var appErr *temporal.ApplicationError
    if errors.As(err, &appErr) {
        // Could be validator rejection OR handler failure
        // appErr.Type() is a string type code set by the thrower
        // appErr.NonRetryable() indicates if the error is retryable
    }
}
```

**Critical limitation:** Both Rejected and Failed surface as `*temporal.ApplicationError`. The caller cannot distinguish validator rejection from handler failure without either inspecting `appErr.Type()` (a string the application author must set intentionally) or checking whether the Update appears in workflow history (via a secondary Query call). The SDK does not provide a separate outcome variant that structurally encodes the rejection/failure distinction.

### Signal Outcomes

Signals are fire-and-forget at the workflow boundary. The SDK provides:
- `workflow.GetSignalChannel(ctx, signalName)` returns a `ReceiveChannel`
- `SignalExternalWorkflow(ctx, workflowID, runID, signalName, arg)` returns a `Future`

**Signal outcome space (2 distinguishable outcomes from the sender perspective):**

| Outcome | Representation |
|---------|---------------|
| Delivered | `Future.Get()` returns nil error |
| Delivery failed | `Future.Get()` returns error (e.g., workflow not found, already completed) |

There is no per-signal acknowledgment or handler-level result. The signal is enqueued in the workflow's task queue and processed asynchronously. No workflow-level rejection is possible — signals do not have a validator phase. If the workflow has unread signals on completion, `workflow.GetUnhandledSignalNames(ctx)` returns the list of unconsumed signal names.

### Query Outcomes

Queries are synchronous reads. Query handlers must not modify workflow state or schedule any commands; attempting to do so causes a `QueryFailedError`.

**Query outcome space (3 distinguishable outcomes):**

| Outcome | Description |
|---------|-------------|
| Success | Handler returns `(result, nil)` → typed result returned to caller |
| Handler error | Handler returns `(_, err)` → client receives error |
| Invalid handler | Handler attempted blocking call or command → `QueryFailedError` |

Query handlers are run outside the normal workflow execution loop, meaning they execute against a replay of the workflow history. Their constraints:
```go
// Legal: read state
func (w *MyWorkflow) CurrentStateQuery() (string, error) {
    return w.currentState, nil
}
// Illegal: causes QueryFailedError
func (w *MyWorkflow) BadQuery(ctx workflow.Context) (string, error) {
    return "", workflow.ExecuteActivity(...).Get(ctx, nil) // ILLEGAL
}
```

### Error Hierarchy in Temporal SDK (Go)

The Temporal Go SDK defines a concrete error hierarchy for wrapping causes:

```
temporal.ActivityError       — wraps errors from activity execution
temporal.ChildWorkflowExecutionError — wraps errors from child workflows
temporal.ApplicationError    — user-defined error (message, type string, nonRetryable bool)
temporal.TimeoutError        — timeout of activity/workflow/child workflow
temporal.CanceledError       — cancellation propagated to a context
temporal.PanicError          — panic in activity/workflow code
```

**Type checking pattern:**
```go
var appErr *temporal.ApplicationError
var timeoutErr *temporal.TimeoutError
var canceledErr *temporal.CanceledError

if errors.As(err, &appErr) {
    // User-defined, inspect appErr.Type()
} else if errors.As(err, &timeoutErr) {
    // Timeout kind: timeoutErr.TimeoutType() → ScheduleToStart, StartToClose, etc.
} else if errors.As(err, &canceledErr) {
    // Workflow was externally canceled
}
```

**`WorkflowExecutionAlreadyStartedError`** is returned by `StartWorkflow` calls when a workflow with the given ID is already running. It is not an Update outcome — it is a start-phase error:

```go
type WorkflowExecutionAlreadyStartedError struct {
    RunId  string
    Type   string
}
```

### Per-Operation vs. Uniform Outcome Types

Temporal does NOT provide per-operation typed result envelopes. All outcomes collapse to Go's `(T, error)` two-return pattern. Distinguishing subtypes requires `errors.As` checks against the concrete error types in the hierarchy. The same error type (`ApplicationError`) carries both validator rejections and handler failures, with disambiguation available only via `appErr.Type()` — a string field the application must set.

### Partial Successes

Not supported as a first-class concept. An Update either succeeds (returns the typed result) or fails (returns an error). There is no "partial result" envelope.

### Idiomatic Caller Pattern

```go
// 1. Start the update
handle, err := temporalClient.UpdateWorkflow(ctx, client.UpdateWorkflowOptions{
    WorkflowID:   "order-123",
    UpdateName:   "approve",
    Args:         []interface{}{approvalInput},
    WaitForStage: client.WorkflowUpdateStageCompleted,
})
if err != nil {
    // Infrastructure error: workflow not found, service unavailable, etc.
    return fmt.Errorf("scheduling update: %w", err)
}

// 2. Get the result
var result ApprovalResult
if err := handle.Get(ctx, &result); err != nil {
    var appErr *temporal.ApplicationError
    if errors.As(err, &appErr) {
        switch appErr.Type() {
        case "ValidationFailure":
            // Caller-set type: validator rejection
        case "BusinessRuleViolation":
            // Caller-set type: handler-level rejection
        default:
            // Unknown application error
        }
    }
    return err
}
// 3. Use result
```

---

## XState v5 — Snapshot Object

**Source:** XState v4 state documentation (xstate.js.org/docs/guides/states.html, fetched April 2026); XState v5 snapshot API (from published API surface and v5 migration documentation, synthesized from canonical knowledge)

### Background: v4 State vs. v5 Snapshot

XState v4 used `State` objects (accessed via `machine.transition()` or `service.onTransition()`). XState v5 (released December 2023) replaced the `State` type with `Snapshot` for actor-model semantics. The two APIs are related but distinct.

### XState v4 State Object (documented from fetched source)

```typescript
interface State<TContext, TEvent> {
    // The current state value — string, object (compound), or nested object (parallel)
    value: StateValue; // string | object

    // The context (extended state) at this state
    context: TContext;

    // The event that caused the transition to this state
    event: TEvent;

    // Actions to execute (side effects declared but not yet run)
    actions: ActionObject<TContext, TEvent>[];

    // Activities currently running
    activities: ActivityMap;

    // Previous State instance
    history?: State<TContext, TEvent>;

    // True if this is a final state
    done: boolean;

    // Meta data from all active state nodes
    meta: Record<string, any>;

    // Methods
    matches(parentStateValue: StateValue): boolean;
    can(event: TEvent): boolean;       // Since 4.25.0
    hasTag(tag: string): boolean;      // Since 4.19.0
    toStrings(): string[];
    
    // Next events that will cause a transition from this state
    nextEvents: string[];
    
    // Whether this state changed from the previous
    changed: boolean | undefined;     // undefined for initial state
}
```

### XState v5 Snapshot Object

In XState v5, actors produce `Snapshot` objects with a `status` discriminant field:

```typescript
type SnapshotStatus = 'active' | 'done' | 'error' | 'stopped';

interface Snapshot<TOutput> {
    status: SnapshotStatus;
    output: TOutput | undefined;    // populated when status === 'done'
    error: unknown | undefined;     // populated when status === 'error'
    context: TContext;
    value: StateValue;
}
```

**Full snapshot outcome space (4 distinguishable statuses):**

| `status` | Meaning | `output` | `error` |
|----------|---------|---------|--------|
| `'active'` | Machine is running, processing events | `undefined` | `undefined` |
| `'done'` | Machine reached a final state | Populated | `undefined` |
| `'error'` | An unhandled error occurred | `undefined` | Populated |
| `'stopped'` | Actor was explicitly stopped | `undefined` | `undefined` |

**Distinguishing `'done'` from `'error'` from `'stopped'`:**
- `'done'` — the machine reached a node with `type: 'final'`. This is the designed completion path.
- `'error'` — an uncaught error was thrown inside an actor (service, guard, or action). The error propagates to the parent actor.
- `'stopped'` — the actor was stopped externally (e.g., parent actor stopped, `stop()` called on interpreter).

### `send()` Return in v5

In XState v5, `send()` on an actor ref returns `void`. The return value was eliminated because actors are asynchronous by design in v5; the caller must subscribe to the actor's `subscribe()` observable to observe state changes after sending an event.

```typescript
// v5 idiomatic pattern
const actor = createActor(myMachine);
actor.subscribe(snapshot => {
    if (snapshot.status === 'done') {
        console.log('Result:', snapshot.output);
    } else if (snapshot.status === 'error') {
        console.error('Error:', snapshot.error);
    }
});
actor.start();
actor.send({ type: 'SUBMIT', data: formData });
// send() returns void — result observed via subscription
```

### `snapshot.can(event)` — Guard Evaluation

`state.can(event)` (v4) / `snapshot.can(event)` (v5) returns a `boolean` indicating whether sending the given event would cause a state transition. This evaluates:
1. Whether any transition is defined for the event in the current state
2. Whether any guard (`guard:` property) on that transition passes

```typescript
// v4 example (from fetched source)
const inactiveState = machine.initialState;
inactiveState.can({ type: 'TOGGLE' });        // true
inactiveState.can({ type: 'DO_SOMETHING' });   // false (not defined in 'inactive')

const activeState = machine.transition(inactiveState, { type: 'TOGGLE' });
activeState.can({ type: 'DO_SOMETHING' });     // true (action will execute)
activeState.can({ type: 'TOGGLE' });           // false (not defined in 'active')
```

**Important:** `can()` executes transition guards as a side effect of the check. Guards must be pure functions.

### `snapshot.nextEvents` — Enabled Event Array

`state.nextEvents` (v4) returns an array of string event type names that will cause a transition from the current state configuration. This is computed from the machine definition — it does not evaluate guards.

```typescript
const { initialState } = lightMachine;
initialState.nextEvents; // => ['TIMER', 'EMERGENCY']
```

**Distinction:** `nextEvents` is based on static transition definitions; `can(event)` is the dynamic evaluation (evaluates guards). An event may appear in `nextEvents` but `can()` returns `false` if the guard fails.

### Per-Operation vs. Uniform Outcome Types

XState uses a uniform `Snapshot` / `State` type — the same structure wraps all outcomes regardless of which event was sent. There is no per-event result type. The `output` field (v5) carries whatever the final state emits, typed by the machine's output type parameter.

### Partial Successes

Parallel state nodes are XState's mechanism for concurrent sub-machines. A machine in a parallel configuration can have multiple regions simultaneously active in different states — this is the closest analog to partial success, but it represents concurrent progress, not a single operation with partial outcome.

### Idiomatic Caller Pattern (v5)

```typescript
import { createActor, waitFor } from 'xstate';

const actor = createActor(checkoutMachine, { input: { cartId: '123' } });
actor.start();

// Option 1: Subscribe
actor.subscribe(snapshot => {
    switch (snapshot.status) {
        case 'active':
            renderCurrentState(snapshot.value, snapshot.context);
            break;
        case 'done':
            handleSuccess(snapshot.output);
            break;
        case 'error':
            handleError(snapshot.error);
            break;
        case 'stopped':
            cleanup();
            break;
    }
});

// Option 2: waitFor utility (promise-based)
const doneSnapshot = await waitFor(actor, s => s.status !== 'active');
if (doneSnapshot.status === 'done') {
    return doneSnapshot.output;
} else if (doneSnapshot.status === 'error') {
    throw doneSnapshot.error;
}
```

---

## Railway-Oriented Programming and Result Type Landscape

**Sources:** Scott Wlaschin, "Railway Oriented Programming," fsharpforfunandprofit.com (canonical source, TLS cert issue — synthesized from widely published source); Rust Reference, doc.rust-lang.org; Haskell standard library; Go specification; crates.io/crates/thiserror and anyhow

### F# Railway-Oriented Programming (Scott Wlaschin)

Scott Wlaschin's Railway-Oriented Programming (ROP) articulates a two-track model for typed error flow. The key insight: every function is either a "switch" function (takes one input, returns a two-track result) or a "dead-end" function (no meaningful output). The metaphor is a two-track railroad: the happy path and the failure track. Once on the failure track, downstream functions are bypassed.

**Core type in F#:**
```fsharp
type Result<'Success, 'Failure> =
    | Ok of 'Success
    | Error of 'Failure
```

**Two-track composition:**
```fsharp
// "bind" (>>= in Haskell, |> Result.bind in F#) chains switch functions
let (>>=) twoTrackInput switchFunction =
    match twoTrackInput with
    | Ok s -> switchFunction s
    | Error f -> Error f

// Example pipeline
let processOrder input =
    input
    |> validateInput     // validates → Result<ValidInput, ValidationError>
    |> (=<<) enrichData  // enriches  → Result<EnrichedInput, EnrichError>
    |> (=<<) submitOrder // submits   → Result<OrderId, SubmitError>
```

**Outcome space:** Exactly two tracks — `Ok` and `Error`. The granularity of the failure space is entirely in the type parameter `'Failure`, which is typically a discriminated union with named variants for each failure mode.

ROP explicitly discourages collapsing two causally distinct failures into one variant. If a function can fail for multiple independent reasons, those reasons should be separate union variants:
```fsharp
type OrderError =
    | InvalidInput of string
    | StockUnavailable of itemId: string * available: int
    | PaymentDeclined of declineCode: string
    | DatabaseError of exn
```

**Partial success:** Not modeled by ROP directly. The pattern handles one successful result or one error. Multi-value results are handled by returning a collection (e.g., a list of successes and failures from a batch) as the success payload.

### Rust `Result<T, E>`

**Source:** doc.rust-lang.org/std/result (canonical Rust std docs)

```rust
pub enum Result<T, E> {
    Ok(T),
    Err(E),
}
```

**Outcome space:** Binary at the top level (`Ok` / `Err`), but `E` is a full type — idiomatically a typed enum with one variant per failure mode.

**Key properties:**
- `#[must_use]` attribute: compiler warns if a `Result` is dropped without being inspected
- `?` operator: propagates `Err` upward by converting the error type via `From::from`
- Pattern matching is exhaustive: a `match` on `Result` that doesn't handle all `Err` variants is a compile error

**Idiomatic error enum:**
```rust
#[derive(Debug, thiserror::Error)]
enum FireError {
    #[error("transition not defined for event {event} in state {state}")]
    Undefined { event: String, state: String },
    
    #[error("no matching guard for event {event}")]
    Unmatched { event: String },
    
    #[error("rejected by business rule: {reason}")]
    Rejected { reason: String },
    
    #[error("constraint violated: {violations:?}")]
    ConstraintViolation { violations: Vec<Violation> },
    
    #[error("infrastructure failure")]
    Infrastructure(#[from] std::io::Error),
}

type FireResult = Result<Instance, FireError>;
```

**Exhaustive match pattern:**
```rust
match engine.fire(state, event, args) {
    Ok(new_instance) => handle_success(new_instance),
    Err(FireError::Undefined { .. }) => handle_routing_gap(),
    Err(FireError::Unmatched { .. }) => handle_guard_miss(),
    Err(FireError::Rejected { reason }) => handle_business_rejection(&reason),
    Err(FireError::ConstraintViolation { violations }) => handle_violations(&violations),
    Err(FireError::Infrastructure(e)) => return Err(e.into()),
}
```

**`?` operator propagation:**
```rust
fn process(instance: &mut Instance, event: Event) -> Result<(), AppError> {
    let new_state = engine.fire(instance.state(), event, args)?; // propagates Err
    instance.update(new_state);
    Ok(())
}
```

**`thiserror` crate:** Provides derive macros for implementing `std::error::Error` on enum variants, enabling structured typed errors with display formatting:

```rust
#[derive(thiserror::Error, Debug)]
pub enum AppError {
    #[error("database error: {0}")]
    Database(#[from] sqlx::Error),
    
    #[error("validation failed: {message}")]
    Validation { message: String },
    
    #[error("not found: {id}")]
    NotFound { id: String },
}
```

**`anyhow` crate:** Provides type-erased errors for application-level code where the caller does not need to distinguish error types. Uses `anyhow::Error` (a boxed `dyn Error`) and `anyhow::Result<T>` = `Result<T, anyhow::Error>`. The `?` operator converts any `std::error::Error` into `anyhow::Error`. Contextual information is added via `.context("message")`:

```rust
use anyhow::{Context, Result};

fn load_config(path: &str) -> Result<Config> {
    let data = std::fs::read_to_string(path)
        .context("reading config file")?;
    serde_json::from_str(&data)
        .context("parsing config JSON")
}
```

**Outcome space for anyhow:** Opaque — the caller gets a formatted error message with backtrace, but cannot pattern-match on error type. Intended for application-level code, not library APIs.

**Partial success in Rust:** Typically represented by returning `Vec<Result<T, E>>` (batch operations) or by returning `(Vec<T>, Vec<E>)` using `.partition()`:
```rust
let (successes, failures): (Vec<_>, Vec<_>) = items
    .into_iter()
    .map(|item| process(item))
    .partition(Result::is_ok);
```

### Haskell `Either a b`

```haskell
data Either a b = Left a | Right b
```

By convention, `Left` carries the error and `Right` carries the success value ("right" = "correct"). `Either` is a functor and monad, enabling `do`-notation chaining.

**Outcome space:** Exactly two values (`Left` / `Right`). Like Rust, granularity of the error space lives in the `a` type parameter — typically a custom ADT.

**Monadic chaining:**
```haskell
validateOrder :: Input -> Either ValidationError ValidInput
enrichData :: ValidInput -> Either EnrichError EnrichedInput
submitOrder :: EnrichedInput -> Either SubmitError OrderId

processOrder :: Input -> Either AppError OrderId
processOrder input = do
    valid   <- mapLeft InvalidInput   $ validateOrder input
    enriched <- mapLeft EnrichFailed  $ enrichData valid
    orderId  <- mapLeft SubmitFailed  $ submitOrder enriched
    return orderId
```

**Idiomatic pattern:**
```haskell
case processOrder input of
    Left (InvalidInput msg) -> handleValidationError msg
    Left (EnrichFailed e)   -> handleEnrichError e
    Left (SubmitFailed e)   -> handleSubmitError e
    Right orderId           -> handleSuccess orderId
```

### Go Multi-Return `(T, error)` Pattern

Go has no sum types. The idiomatic error pattern is a two-value return: `(T, error)`. The `error` interface has one method: `Error() string`. Type checking is done via type assertions or `errors.As()`.

**Outcome space:** Convention-based, not type-system-enforced. `error == nil` signals success; non-nil `error` signals failure. The error type carries the distinguishing information.

**Typed error pattern:**
```go
type NotFoundError struct {
    Resource string
    ID       string
}
func (e *NotFoundError) Error() string {
    return fmt.Sprintf("%s %s not found", e.Resource, e.ID)
}

type ValidationError struct {
    Field   string
    Message string
}
func (e *ValidationError) Error() string {
    return fmt.Sprintf("validation failed on %s: %s", e.Field, e.Message)
}

// Sentinel errors
var ErrNotAuthorized = errors.New("not authorized")
var ErrAlreadyExists = errors.New("already exists")
```

**Caller pattern:**
```go
result, err := store.FindOrder(ctx, orderID)
if err != nil {
    var notFound *NotFoundError
    var validation *ValidationError
    switch {
    case errors.As(err, &notFound):
        return http.StatusNotFound, fmt.Sprintf("not found: %s", notFound.ID)
    case errors.As(err, &validation):
        return http.StatusBadRequest, validation.Message
    case errors.Is(err, ErrNotAuthorized):
        return http.StatusForbidden, "forbidden"
    default:
        return http.StatusInternalServerError, "internal error"
    }
}
```

**Distinguishing failure modes:** The caller must use `errors.As` (structural) or `errors.Is` (sentinel identity) to branch on error type. There is no compiler enforcement of exhaustive handling. A `default` case silently swallows unhandled errors.

**Partial success:** No first-class representation. Callers return `([]Result, []error)` or custom structs for batch operations.

---

## OPA / Rego — Policy Evaluation Results

**Sources:** pkg.go.dev/github.com/open-policy-agent/opa/rego (v1.15.2, fetched April 2026); openpolicyagent.org/docs/policy-reference (fetched April 2026); OPA Rego language reference

### The `ResultSet` Type

The primary evaluation result from OPA is `rego.ResultSet`:

```go
// ResultSet represents a collection of output from Rego evaluation.
// An empty result set represents an undefined query.
type ResultSet []Result

// Result defines the output of Rego evaluation.
type Result struct {
    Bindings Vars              // variable bindings: map[string]interface{}
    Expressions []*ExpressionValue  // expression values
}

// Vars represents a collection of variable bindings.
type Vars map[string]interface{}

// ExpressionValue defines the value of an expression in a Rego query.
type ExpressionValue struct {
    Value    interface{}
    Text     string
    Location *Location
}
```

### OPA Outcome Space

OPA evaluates Rego queries and returns one of three semantically distinct outcomes:

**1. Non-empty ResultSet — "policy says yes" (allows or produces a value)**

The query evaluated to at least one set of bindings. For authorization policies, a non-empty ResultSet conventionally means the request is allowed.

```go
rs, err := rego.New(
    rego.Query("data.authz.allow == true"),
    rego.Input(input),
    rego.Module("policy.rego", policy),
).Eval(ctx)
if err == nil && len(rs) > 0 {
    // Allowed
}
```

**2. Empty ResultSet — "policy says no" OR "rule is undefined"**

An empty ResultSet is the critical ambiguity in OPA's outcome model. It represents BOTH:
- The policy explicitly evaluated to `false` or was undefined for these inputs
- No matching rules exist for this query

Rego's semantics: if a rule body fails to unify, the rule is **undefined** — not `false`. `undefined` and `false` are distinct in the language but both produce an empty ResultSet in the default query evaluation mode.

**Distinguishing "false" from "undefined" in Rego:**
```rego
# If no rule defines allow, allow is undefined — NOT false
# These are different:

# Rule explicitly set to false:
allow = false { some_condition }

# Rule not defined at all → allow is undefined
```

At the query level, the OPA Go API does not expose this distinction to the caller unless the caller queries for the specific rule value and checks:
```go
// Query for the value of data.authz.allow
rs, err := rego.New(rego.Query("x := data.authz.allow")).Eval(ctx)
if err != nil {
    // Evaluation error (see below)
} else if len(rs) == 0 {
    // data.authz.allow is undefined — rule not defined for this input
} else {
    value := rs[0].Bindings["x"]
    // value could be true, false, or any other Rego value
    if value == true {
        // Explicitly allowed
    } else if value == false {
        // Explicitly denied
    }
}
```

**3. Non-nil error — "evaluation error"**

The query itself failed to evaluate due to a runtime error (e.g., division by zero, type error in a built-in function, halt error from custom built-in). This is structurally distinct from an empty ResultSet.

```go
type Errors []error  // collection of errors returned when evaluating Rego

// HaltError — custom built-in function can return this to abort evaluation
type HaltError struct { Err error }
```

**Full OPA outcome space (3 distinguishable outcomes):**

| Outcome | Representation | Meaning |
|---------|---------------|---------|
| Policy match (positive) | `len(rs) > 0` and `err == nil` | Rule fired, bindings produced |
| Policy undefined/false | `len(rs) == 0` and `err == nil` | No bindings produced (ambiguous: undefined vs. false) |
| Evaluation error | `err != nil` | Runtime failure during evaluation |

### The `undefined` Concept in Rego

In Rego, a rule that has no body matches (fails to unify) produces an **undefined** result, not `false`. This is a closed-world assumption flip from typical boolean logic. The OPA documentation distinguishes:

- `undefined` — the rule was not defined or its conditions did not match
- `false` — the rule was defined and explicitly evaluated to false
- `null` — a valid Rego value (different from undefined)

The `default` keyword bridges this distinction:
```rego
# Without default: allow is undefined when conditions don't match
# allow is undefined if no rule body succeeds

# With default: allow becomes false when conditions don't match
default allow := false

allow if {
    input.user == "admin"
}
```

With `default`, an empty ResultSet becomes unreachable for `allow` — the rule always has a value. Without `default`, an empty ResultSet from querying `allow` could mean either "rule says no" or "rule doesn't apply here."

### `StrictBuiltinErrors` Option

By default, OPA built-in function errors (e.g., `http.send` network failure) result in the containing expression being `undefined`, which may produce an empty ResultSet rather than an error. With `StrictBuiltinErrors(true)`, such errors become fatal evaluation errors:

```go
rego.New(
    rego.StrictBuiltinErrors(true),  // errors from built-ins become fatal
    rego.Query("..."),
).Eval(ctx)
```

**This means the caller cannot always determine without `StrictBuiltinErrors` whether an empty ResultSet came from a policy decision or a built-in failure.**

### Per-Operation vs. Uniform Outcome Types

OPA uses a single `ResultSet` type for all query evaluations. There are no per-query result types. The `Result.Bindings` map contains whatever variables the query bound.

### Partial Successes

Partial evaluation (`rego.Rego.PartialResult()`) is a distinct concept in OPA — it pre-evaluates parts of the policy with known inputs, returning residual queries (partial queries) for later evaluation with remaining unknowns. This is not "partial success" in the operational sense; it is compile-time optimization.

### Idiomatic Caller Pattern

```go
// Prepare for repeated evaluation (more efficient)
pq, err := rego.New(
    rego.Query("allow = data.authz.allow"),
    rego.Module("policy.rego", policyText),
).PrepareForEval(ctx)
if err != nil {
    // Compilation error — policy syntax/type error
    return fmt.Errorf("compiling policy: %w", err)
}

// Evaluate with input
rs, err := pq.Eval(ctx, rego.EvalInput(requestInput))
if err != nil {
    // Runtime evaluation error (e.g., custom built-in halted)
    return fmt.Errorf("evaluating policy: %w", err)
}

if len(rs) == 0 {
    // allow is undefined — treat as deny
    return ErrForbidden
}

allowValue, ok := rs[0].Bindings["allow"].(bool)
if !ok || !allowValue {
    return ErrForbidden
}
return nil // allowed
```

---

## CEL (Common Expression Language) — Evaluation Results

**Sources:** github.com/google/cel-go (canonical Go implementation); CEL specification (github.com/google/cel-spec); Kubernetes CEL integration documentation

### The `ref.Val` Universal Value Type

CEL evaluates expressions and returns a `ref.Val` — a universal value interface that represents any CEL value:

```go
// ref.Val is the universal value interface in CEL
type Val interface {
    // ConvertToNative converts the value to a native Go type
    ConvertToNative(typeDesc reflect.Type) (interface{}, error)
    
    // ConvertToType converts the value to the given CEL type
    ConvertToType(typeValue ref.Type) ref.Val
    
    // Equal returns true if the two values are equal
    Equal(other ref.Val) ref.Val
    
    // Type returns the CEL type of the value
    Type() ref.Type
    
    // Value returns the underlying native value
    Value() interface{}
}
```

**CEL type hierarchy (partial):**
```
ref.Val
├── types.Bool
├── types.Int
├── types.Uint
├── types.Double
├── types.String
├── types.Bytes
├── types.Duration
├── types.Timestamp
├── types.Null
├── types.List (wraps []ref.Val)
├── types.Map  (wraps map[ref.Val]ref.Val)
├── types.Err  ← runtime errors as values (not Go errors)
└── types.Unknown ← used in partial evaluation
```

### `types.Err` — Runtime Errors as Values

A critical design choice in CEL: **runtime errors are represented as `ref.Val` values of type `types.Err`, not as Go `error` return values.** The `Program.Eval()` method returns `(ref.Val, *rego.EvalDetails, error)`, but the Go `error` is reserved for structural/infrastructure failures. CEL-level runtime errors (type mismatches, null pointer dereferences, field not found) are embedded in the `ref.Val` return.

```go
val, details, err := prog.Eval(activation)
// err != nil: CEL infrastructure failure (e.g., program not properly compiled)
// val.Type() == types.Err: CEL runtime error during expression evaluation
// val.Type() == types.Bool, types.Int, etc.: successful evaluation
```

**Checking for `types.Err`:**
```go
if val != nil && val.Type() == types.ErrType {
    celErr := val.(*types.Err)
    // celErr.Error() returns the error message
    // celErr.Unwrap() returns the underlying Go error
}

// Or use the helper:
if types.IsError(val) {
    // val is a CEL error
}
```

### `no_matching_overload` vs. `no_such_field`

CEL defines specific error categories for runtime type failures:

**`no_matching_overload`** — the expression applied a function/operator to arguments of types for which no implementation is registered. Example: applying `+` to a string and an int.

**`no_such_field`** — the expression accessed a field that does not exist on a proto message or map. This is distinct from null pointer errors.

```go
// These produce types.Err with specific messages:

// no_matching_overload
// Expression: 1 + "foo"  
// Error: "no matching overload for '_+_' applied to '(int, string)'"

// no_such_field
// Expression: request.nonExistentField
// Error: "no such key: nonExistentField"  // on maps
// Error: "undefined field 'nonExistentField'" // on proto messages
```

### Short-Circuit Semantics

CEL logical operators (`&&`, `||`) use error-propagating short-circuit evaluation — sometimes called "commutative" or "erroring" short-circuit:

- **`&&` (AND):** If either operand is `false`, the result is `false` (regardless of whether the other operand is an error). If both are non-error, short-circuit applies. If one is `true` and the other is an error, the result is the error.
- **`||` (OR):** If either operand is `true`, the result is `true` (regardless of errors). If one is `false` and the other is an error, the result is the error.

This is specifically designed so that `false && <error>` = `false` (not an error), allowing policy expressions like:
```
has(request.claims) && request.claims.role == "admin"
```
to safely short-circuit on the left operand if `request.claims` is absent.

### Full CEL Outcome Space

| Outcome | `ref.Val` type | `error` return |
|---------|---------------|---------------|
| Successful evaluation | `types.Bool`, `types.Int`, etc. | `nil` |
| CEL runtime error (type mismatch, null deref, etc.) | `types.Err` | `nil` |
| Infrastructure failure | `nil` or sentinel | non-nil |

Note: A successful evaluation returning `false` is NOT an error — `types.Bool(false)` is a valid successful result. The `types.Err` type is used only for errors that occurred during evaluation.

### Per-Operation vs. Uniform Outcome Types

CEL uses a single `ref.Val` for all expression evaluations. The type of the value depends on the declared return type of the expression as registered with the CEL environment.

### Partial Successes (Unknown Values)

CEL supports partial evaluation via `types.Unknown` — a value that represents unknowns during partial evaluation. When the evaluator encounters an unknown input, it propagates `types.Unknown` through the expression tree.

```go
// types.Unknown carries the set of unknown attribute IDs
type Unknown struct {
    IDs []int64 // identifiers of the unknowns that contributed to this value
}
```

### Idiomatic Caller Pattern

```go
env, err := cel.NewEnv(/* declarations */)
if err != nil {
    return fmt.Errorf("creating CEL env: %w", err)
}

ast, iss := env.Compile(exprText)
if iss.Err() != nil {
    return fmt.Errorf("compiling expression: %w", iss.Err())
}

prog, err := env.Program(ast)
if err != nil {
    return fmt.Errorf("building program: %w", err)
}

val, _, err := prog.Eval(activation)
if err != nil {
    // Infrastructure failure
    return fmt.Errorf("evaluating: %w", err)
}

if types.IsError(val) {
    celErr := val.(*types.Err)
    switch {
    case strings.Contains(celErr.Error(), "no_matching_overload"):
        // Type mismatch in expression
    case strings.Contains(celErr.Error(), "no such key"):
        // Field access on missing field
    default:
        // Other runtime error
    }
    return fmt.Errorf("CEL runtime error: %s", celErr.Error())
}

result, ok := val.(ref.Val).Value().(bool)
if !ok {
    return fmt.Errorf("expression did not return bool")
}
// use result
```

---

## Kubernetes Status Object

**Sources:** Kubernetes API Conventions (kubernetes.io/docs/reference/using-api/api-concepts/); k8s.io/apimachinery/pkg/api/errors; Kubernetes API reference; kubectl source

### The `metav1.Status` Type

When a Kubernetes API operation fails, the server returns a `Status` object (not the requested resource):

```go
// k8s.io/apimachinery/pkg/apis/meta/v1
type Status struct {
    TypeMeta
    ListMeta
    
    // "Success", "Failure"
    Status string
    
    // Human-readable description of the error
    Message string
    
    // Machine-readable reason (enum string)
    Reason StatusReason
    
    // Extended error details
    Details *StatusDetails
    
    // HTTP status code
    Code int32
}

type StatusDetails struct {
    // Name of the resource (e.g., pod name)
    Name string
    
    // Group of the resource
    Group string
    
    // Kind of the resource
    Kind string
    
    // UID of the resource
    UID types.UID
    
    // Field-level causes
    Causes []StatusCause
    
    // Retry-after seconds (for 429 / 503)
    RetryAfterSeconds int32
}
```

### `StatusReason` Enum

`StatusReason` is a typed string alias (not a Go enum), representing machine-readable error categories:

```go
type StatusReason string

const (
    StatusReasonUnknown              StatusReason = ""
    StatusReasonUnauthorized         StatusReason = "Unauthorized"       // 401
    StatusReasonForbidden            StatusReason = "Forbidden"          // 403
    StatusReasonNotFound             StatusReason = "NotFound"           // 404
    StatusReasonAlreadyExists        StatusReason = "AlreadyExists"      // 409
    StatusReasonConflict             StatusReason = "Conflict"           // 409
    StatusReasonGone                 StatusReason = "Gone"               // 410
    StatusReasonInvalid              StatusReason = "Invalid"            // 422
    StatusReasonServerTimeout        StatusReason = "ServerTimeout"      // 500
    StatusReasonTimeout              StatusReason = "Timeout"            // 504
    StatusReasonTooManyRequests      StatusReason = "TooManyRequests"    // 429
    StatusReasonBadRequest           StatusReason = "BadRequest"         // 400
    StatusReasonMethodNotAllowed     StatusReason = "MethodNotAllowed"   // 405
    StatusReasonNotAcceptable        StatusReason = "NotAcceptable"      // 406
    StatusReasonRequestEntityTooLargeReason StatusReason = "RequestEntityTooLarge" // 413
    StatusReasonUnsupportedMediaType StatusReason = "UnsupportedMediaType" // 415
    StatusReasonInternalError        StatusReason = "InternalError"      // 500
    StatusReasonExpired              StatusReason = "Expired"            // 410
    StatusReasonServiceUnavailable   StatusReason = "ServiceUnavailable" // 503
)
```

### `StatusCause` — Field-Level Error Details

The `StatusDetails.Causes` array contains per-field cause information, used primarily when `Reason == "Invalid"` (HTTP 422 Unprocessable Entity):

```go
type StatusCause struct {
    // Machine-readable cause type
    Type CauseType
    
    // Human-readable description
    Message string
    
    // Field path in dotted notation (e.g., "spec.containers[0].image")
    Field string
}

type CauseType string

const (
    CauseTypeFieldValueNotFound     CauseType = "FieldValueNotFound"
    CauseTypeFieldValueRequired     CauseType = "FieldValueRequired"
    CauseTypeFieldValueDuplicate    CauseType = "FieldValueDuplicate"
    CauseTypeFieldValueInvalid      CauseType = "FieldValueInvalid"
    CauseTypeFieldValueNotSupported CauseType = "FieldValueNotSupported"
    CauseTypeUnexpectedServerResponse CauseType = "UnexpectedServerResponse"
    CauseTypeFieldManagerConflict   CauseType = "FieldManagerConflict"
    CauseTypeForbiddenReason        CauseType = "FieldValueForbidden"
)
```

### Distinguishing Failure Modes

The `StatusReason` enum makes the following distinctions:

| Failure Mode | `Reason` | `Code` | Distinguishes From |
|-------------|----------|--------|-------------------|
| Business rule rejection (field invalid) | `Invalid` | 422 | Structural bad request vs. semantically invalid |
| Input missing required field | `Invalid` with `FieldValueRequired` cause | 422 | Present-but-wrong vs. absent |
| Duplicate resource | `AlreadyExists` | 409 | Not-found vs. already-there |
| Conflict (optimistic concurrency) | `Conflict` | 409 | AlreadyExists vs. concurrent modification |
| Permission denied (known user) | `Forbidden` | 403 | Authorization vs. authentication |
| Not authenticated | `Unauthorized` | 401 | Forbidden vs. unauthenticated |
| Resource not found | `NotFound` | 404 | — |
| Infrastructure error | `InternalError` | 500 | — |
| Rate limited | `TooManyRequests` | 429 | with `RetryAfterSeconds` in Details |

**Critical distinction: `Invalid` vs. `BadRequest`:**
- `BadRequest` (400): The request was malformed at the protocol/structural level (e.g., invalid JSON, unrecognized fields in strict mode)
- `Invalid` (422): The request was structurally correct but semantically invalid (field value out of range, required field missing, etc.)

### `errors.IsNotFound`, `errors.IsConflict`, etc.

The `k8s.io/apimachinery/pkg/api/errors` package provides typed predicates:

```go
import k8serrors "k8s.io/apimachinery/pkg/api/errors"

err := client.Create(ctx, pod)
switch {
case k8serrors.IsNotFound(err):
    // referenced resource doesn't exist
case k8serrors.IsAlreadyExists(err):
    // resource already exists — handle idempotency
case k8serrors.IsConflict(err):
    // resource version conflict — re-fetch and retry
case k8serrors.IsForbidden(err):
    // RBAC denied
case k8serrors.IsInvalid(err):
    statusErr := err.(*k8serrors.StatusError)
    causes := statusErr.ErrStatus.Details.Causes
    // iterate causes for per-field errors
case k8serrors.IsServerTimeout(err) || k8serrors.IsServiceUnavailable(err):
    // transient, retry with backoff
}
```

### Per-Operation vs. Uniform Outcome Types

Kubernetes uses a single `Status` type for all API failures regardless of operation (GET, POST, PUT, DELETE, PATCH). The HTTP verb is expressed in the request, not the response type.

### Partial Successes

Server-Side Apply (SSA) and admission webhooks can produce partial acceptance. The API returns the applied resource state with field manager information, but this is encoded as the successful result object, not a separate "partial success" type.

List operations that page through resources may encounter errors mid-stream (via watches or ListOptions); the stream communicates errors via a `Status` event embedded in the watch event stream.

---

## Erlang `gen_statem` — Call Results

**Sources:** Erlang OTP documentation, erlang.org/doc/man/gen_statem.html; OTP source code; Erlang standard library documentation

### Overview

`gen_statem` is the Erlang/OTP state machine behavior. It supersedes `gen_fsm`. Callbacks receive events and return action tuples that the framework executes.

### State Machine Callback Return Terms

The callback functions (`handle_event/4`, `state_name/3`) return terms that instruct the `gen_statem` engine what to do next:

```erlang
%% Core return shapes from callback functions:

{next_state, NextState, NewData}
%% Transition to NextState, update data. No reply sent.

{next_state, NextState, NewData, Actions}
%% Same, plus a list of actions (e.g., [{reply, From, Reply}])

{keep_state, NewData}
%% Stay in current state, update data

{keep_state, NewData, Actions}
%% Stay in current state, update data, execute actions

keep_state_and_data
%% Stay in current state, keep existing data

keep_state_and_data
{keep_state_and_data, Actions}

{repeat_state, NewData}
%% Repeat current state (fire entry actions again)

{stop, Reason}
%% Stop the gen_statem with Reason

{stop, Reason, NewData}

{stop_and_reply, Reason, Replies}
%% Stop and send replies

{stop_and_reply, Reason, Replies, NewData}
```

### `Actions` — What Travels with State Transitions

The `Actions` list can contain:

```erlang
%% Reply to a caller (for gen_statem:call/2,3)
{reply, From, Reply}

%% Send event to self (postpone current event, re-queue, or timeout)
postpone          %% keep current event for next state
{next_event, EventType, EventContent}

%% Timers
{state_timeout, Time, EventContent}
{timeout, Time, EventContent}
{abs_timeout, Time, EventContent}

%% State entry (only valid with state_enter option)
{state_enter, EventType}
```

### `gen_statem:call/2,3` — Synchronous Call Outcomes

When a caller uses `gen_statem:call(ServerRef, Request, Timeout)`, the outcome is:

```erlang
{ok, Reply}     %% Successful reply from {reply, From, Reply} action
{error, Reason} %% Process not found, timeout, or EXIT propagated
```

**Full `call` outcome space:**

| Outcome | Representation | When |
|---------|---------------|------|
| Success | `{ok, Reply}` | Callback returned `{reply, From, Reply}` action |
| Timeout | `exit({timeout, ...})` | Timeout expired before reply |
| Process not found | `exit({noproc, ...})` | Server doesn't exist |
| Process exited | `exit({Reason, ...})` | Server crashed during call |
| Stopped | `exit({normal, ...})` (or `Reason`) | Server stopped |

Note: In Erlang idiom, `gen_statem:call` exits (raises) on error rather than returning an error tuple. This is caught by the `catch` or `try...catch` expression.

### How Errors Surface — EXIT Signals

Erlang's error model is based on linked processes and EXIT signals. When a `gen_statem` crashes:
1. It sends an `EXIT` signal to all linked processes
2. Linked processes either crash (if not trapping exits) or receive `{'EXIT', Pid, Reason}` (if trapping exits)

```erlang
%% Supervisor child spec determines restart behavior
ChildSpec = #{
    id => my_statem,
    start => {my_statem, start_link, []},
    restart => permanent,   %% always restart
    shutdown => 5000,
    type => worker
}
```

**The `Reason` in exit signals:**
```erlang
%% Standard reasons:
normal      %% clean shutdown
shutdown    %% supervisor-initiated shutdown
{shutdown, Term} %% supervisor-initiated with extra info

%% Error reasons (anything else):
badarg
badarith
{badmatch, Value}
function_clause
{case_clause, Value}
if_clause
{try_clause, Value}
undef
...
%% or any custom term returned from {stop, Reason}
```

### State Data Mutation vs. Reply

Unlike request-response systems, `gen_statem` separates:
- **State mutation:** `{next_state, NewState, NewData}` — always applies
- **Reply:** `{reply, From, Reply}` — an action in the `Actions` list

This means a callback can:
- Transition state AND reply: `{next_state, pending, Data, [{reply, From, ok}]}`
- Transition state WITHOUT replying (async): `{next_state, pending, Data}`
- Reply without transitioning: `{keep_state, Data, [{reply, From, result}]}`
- Stop and reply: `{stop_and_reply, normal, [{reply, From, {error, stopped}}]}`

### Distinguishing "Business Rule Rejection" from Error

`gen_statem` has no built-in concept of "business rule rejection" as distinct from a handler error. The distinction is carried by what the callback returns:

```erlang
%% Convention: reply with error tuple to indicate business rejection
handle_event({call, From}, {approve_order, OrderId}, State, Data) ->
    case can_approve(OrderId, Data) of
        {error, insufficient_funds} ->
            %% Business rule rejection: reply with error, keep state
            {keep_state, Data, [{reply, From, {error, insufficient_funds}}]};
        ok ->
            NewData = mark_approved(OrderId, Data),
            {next_state, approved, NewData, [{reply, From, ok}]}
    end.
```

The caller receives either `{ok, Reply}` (where Reply can be an error tuple) or an EXIT:

```erlang
case gen_statem:call(Pid, {approve_order, OrderId}) of
    ok ->
        handle_success();
    {error, insufficient_funds} ->
        handle_rejection();
    %% EXIT (timeout, crash) caught by try/catch outside this block
end
```

### Per-Operation vs. Uniform Outcome Types

`gen_statem` return terms are uniform across all events and states — the framework does not support per-event typed results. The result type is whatever the application puts in the reply term.

---

## Domain-Driven Design — Command Handling Outcomes

**Sources:** Vaughn Vernon, "Implementing Domain-Driven Design" (2013); Greg Young, CQRS documents (2010); Martin Fowler, "Domain Event Pattern"; Eric Evans, "Domain-Driven Design" (2003); various DDD community writings

### Command Rejection vs. Exception

The fundamental DDD distinction for command outcomes:

**Command Rejected (business rule violation):**  
The command was valid in structure but violated a domain invariant. The aggregate enforces its own consistency. This is a designed, expected outcome — not exceptional.

Examples:
- "Cannot ship order that is not in Paid state"
- "Cannot add item to cart that is already checked out"
- "Credit limit exceeded"

**Exception (infrastructure or programming error):**  
Something unexpected went wrong. The aggregate's state is unknown. This is not a designed business outcome.

Examples:
- Database unavailable
- Serialization failure
- NullReferenceException from a programming bug

### Command Handler Outcome Patterns

**Pattern 1: Exception-only (classic DDD)**
```csharp
// Command handler throws on business rule violation
public class OrderCommandHandler
{
    public void Handle(ShipOrderCommand command)
    {
        var order = _repository.Find(command.OrderId);
        // Throws if preconditions not met
        order.Ship(); // throws InvalidOperationException if not in Paid state
        _repository.Save(order);
    }
}

// Aggregate throws on invariant violation
public class Order
{
    public void Ship()
    {
        if (_status != OrderStatus.Paid)
            throw new InvalidOperationException($"Cannot ship order in {_status} state");
        
        _status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShipped(Id, DateTime.UtcNow));
    }
}
```

**Pattern 2: `Either<DomainError, DomainEvent>` return type**

More explicit, avoids using exceptions for control flow:

```csharp
// F#-style:
type OrderError =
    | NotInCorrectState of current: OrderStatus
    | InsufficientStock of productId: Guid * requested: int * available: int
    | PaymentNotConfirmed
    | ShippingAddressInvalid of message: string

// Command returns Either
type Either<'Left, 'Right> = 
    | Left of 'Left    // error
    | Right of 'Right  // success

type ShipOrderResult = Either<OrderError, OrderShipped>
```

```csharp
// C# equivalent:
public record OrderError
{
    public record NotInCorrectState(OrderStatus Current) : OrderError;
    public record InsufficientStock(Guid ProductId, int Requested, int Available) : OrderError;
    public record PaymentNotConfirmed : OrderError;
    public record ShippingAddressInvalid(string Reason) : OrderError;
}

public class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;
    private readonly bool _isSuccess;

    public static Result<TValue, TError> Success(TValue value) => new(value, default, true);
    public static Result<TValue, TError> Failure(TError error) => new(default, error, false);
    
    public bool IsSuccess => _isSuccess;
    public TValue Value => _isSuccess ? _value! : throw new InvalidOperationException();
    public TError Error => !_isSuccess ? _error! : throw new InvalidOperationException();
}

// Command handler with typed result
public Result<OrderShipped, OrderError> Handle(ShipOrderCommand command)
{
    var order = _repository.Find(command.OrderId);
    return order.TryShip(); // returns Result, not throws
}
```

**Pattern 3: Domain Events + Aggregate State (CQRS/Event Sourcing)**

In event-sourced systems, a successful command produces one or more domain events. A rejected command produces nothing (or raises an exception). The pattern:

```csharp
public class Order : AggregateRoot
{
    public IReadOnlyList<DomainEvent> Handle(ShipOrderCommand command)
    {
        // Guard (throws on violation — classic style)
        if (_status != OrderStatus.Paid)
            throw new DomainException($"Cannot ship order: status is {_status}");
        
        // If we get here, return the events
        return new[] { new OrderShipped(Id, DateTime.UtcNow) };
    }
}
```

### Aggregate Invariant Violations

An aggregate invariant is an always-true condition that the aggregate enforces at all times. When a command would violate an invariant:

- In exception-based DDD: the aggregate throws a domain exception before applying any state changes
- In Result-based DDD: the aggregate returns a typed error before applying state changes
- The aggregate's internal state is guaranteed consistent after a rejected command

**Key property:** The aggregate does not apply partial changes. It either fully transitions (success, domain events emitted) or fully rejects (no state change, no events).

### Full Command Handling Outcome Space (DDD)

| Outcome | Description | Type Representation |
|---------|-------------|---------------------|
| Success | Command accepted, domain events produced | `IReadOnlyList<DomainEvent>` or void |
| Business rejection | Aggregate invariant violated | `DomainException` or `Left<DomainError>` |
| Invalid input | Command itself malformed | `ValidationException` or separate validation result |
| Not found | Aggregate not found in repository | `NotFoundException` or `Option<T>` |
| Infrastructure failure | Repository unavailable, etc. | Infrastructure exception |

### Distinguishing Business Rejection from Invalid Input

DDD literature distinguishes these:
- **Business rejection:** The command was valid, but the domain says "not now" or "not permitted for this entity in its current state"
- **Invalid input:** The command itself was malformed — missing required fields, out-of-range values, wrong format

In practice:
```csharp
// Two separate exception hierarchies:
public class DomainException : Exception       // business rule violations
public class ValidationException : Exception   // command structure violations

// Or two separate result types:
public ValidationResult Validate(ShipOrderCommand command) { ... }
public Result<OrderShipped, DomainError> Handle(ShipOrderCommand command) { ... }
```

### Idiomatic Caller Pattern (C# Result-based)

```csharp
// Validate input structure first
var validation = _validator.Validate(command);
if (!validation.IsValid)
    return BadRequest(validation.Errors);

// Handle the command
var result = _orderCommandHandler.Handle(command);

return result switch
{
    { IsSuccess: true } => Ok(result.Value),
    { Error: OrderError.NotInCorrectState e } => 
        Conflict($"Order is in {e.Current} state"),
    { Error: OrderError.InsufficientStock e } => 
        UnprocessableEntity($"Only {e.Available} units available"),
    { Error: OrderError.PaymentNotConfirmed } => 
        UnprocessableEntity("Payment not confirmed"),
    _ => StatusCode(500, "Internal error")
};
```

---

## gRPC Status Codes

**Sources:** grpc.io/docs/guides/status-codes (fetched April 2026); grpc.github.io/grpc/core/md_doc_statuscodes.html (fetched April 2026); google.golang.org/grpc/codes

### Status Object Structure

Every gRPC RPC call returns a `status` object:
```
Status {
    code:    uint32   // numeric code (0-16)
    message: string   // human-readable message
}
```

In addition, **trailing metadata** can carry structured error details via the `google.rpc.Status` protobuf message (separate from the wire-level status).

### The 17 Status Codes (Full List)

**Source:** grpc.io/docs/guides/status-codes (fetched April 2026)

| Code | Integer | Meaning |
|------|---------|---------|
| `OK` | 0 | Not an error; returned on success |
| `CANCELLED` | 1 | Operation cancelled, typically by the caller |
| `UNKNOWN` | 2 | Unknown error; used for unrecognized error spaces |
| `INVALID_ARGUMENT` | 3 | Client specified an invalid argument; independent of system state |
| `DEADLINE_EXCEEDED` | 4 | Deadline expired before operation completed |
| `NOT_FOUND` | 5 | Requested entity not found |
| `ALREADY_EXISTS` | 6 | Entity client tried to create already exists |
| `PERMISSION_DENIED` | 7 | Caller lacks permission for specified operation |
| `RESOURCE_EXHAUSTED` | 8 | Resource exhausted (quota, disk space, etc.) |
| `FAILED_PRECONDITION` | 9 | System not in required state for operation |
| `ABORTED` | 10 | Operation aborted (concurrency issue, transaction abort) |
| `OUT_OF_RANGE` | 11 | Operation attempted past valid range |
| `UNIMPLEMENTED` | 12 | Operation not implemented or not supported |
| `INTERNAL` | 13 | Internal invariant broken; reserved for serious errors |
| `UNAVAILABLE` | 14 | Service currently unavailable (transient) |
| `DATA_LOSS` | 15 | Unrecoverable data loss or corruption |
| `UNAUTHENTICATED` | 16 | Request lacks valid authentication credentials |

### Critical Distinctions: `FAILED_PRECONDITION` vs. `ABORTED` vs. `OUT_OF_RANGE`

The gRPC documentation provides explicit guidance for choosing between these three codes when the operation cannot proceed:

**`FAILED_PRECONDITION` (9):**
> Use if the client should NOT retry until the system state has been explicitly fixed. The system is not in the required state, but retrying the same request won't help until the state changes externally.

Examples:
- Attempting to delete a non-empty directory
- Applying an operation to a wrong type (rmdir on a file)
- Business rule: "order must be in Paid state to ship"

**`ABORTED` (10):**
> Use if the client should retry at a higher level — the client should restart the read-modify-write sequence. Typically due to concurrency issues.

Examples:
- Optimistic concurrency check failure (etag mismatch)
- Transaction serialization failure
- Test-and-set failure

**`OUT_OF_RANGE` (11):**
> Use when an operation was attempted past the valid range, and the range is bounded by current system state (not fundamental to the input). Prefer over `FAILED_PRECONDITION` for iterating callers.

Examples:
- Reading past current end-of-file (file might grow)
- Page cursor past current result set end

**`INVALID_ARGUMENT` (3) vs. `FAILED_PRECONDITION` (9):**
- `INVALID_ARGUMENT`: The argument itself is wrong, regardless of system state (malformed file name, out-of-type-range value)
- `FAILED_PRECONDITION`: The argument is structurally valid but the system is not in the right state to accept it

### Library-Generated vs. Application-Generated Codes

The gRPC documentation notes that only certain codes are generated by the gRPC library itself:

**Library-generated:**
- `CANCELLED`, `DEADLINE_EXCEEDED`, `UNIMPLEMENTED`, `UNAVAILABLE`, `UNKNOWN`, `RESOURCE_EXHAUSTED`, `INTERNAL`, `UNAUTHENTICATED`

**Application-only (never generated by the library):**
- `INVALID_ARGUMENT`, `NOT_FOUND`, `ALREADY_EXISTS`, `FAILED_PRECONDITION`, `ABORTED`, `OUT_OF_RANGE`, `DATA_LOSS`

This means a caller can be certain that `FAILED_PRECONDITION` was returned by application code, not the gRPC infrastructure.

### Trailing Metadata for Structured Error Details

The `google.rpc.Status` protobuf message (separate from the wire-level status code) provides structured error details via trailing metadata:

```protobuf
// google/rpc/status.proto
message Status {
    int32 code = 1;       // StatusCode enum value
    string message = 2;   // Human-readable error description
    repeated google.protobuf.Any details = 3;  // Typed error details
}

// google/rpc/error_details.proto — common detail types:
message BadRequest {
    message FieldViolation {
        string field = 1;
        string description = 2;
    }
    repeated FieldViolation field_violations = 1;
}

message PreconditionFailure {
    message Violation {
        string type = 1;
        string subject = 2;
        string description = 3;
    }
    repeated Violation violations = 1;
}

message RetryInfo {
    google.protobuf.Duration retry_delay = 1;
}

message ResourceInfo {
    string resource_type = 1;
    string resource_name = 2;
    string owner = 3;
    string description = 4;
}

message RequestInfo {
    string request_id = 1;
    string serving_data = 2;
}

message ErrorInfo {
    string reason = 1;   // machine-readable reason
    string domain = 2;   // error domain (e.g., "googleapis.com")
    map<string, string> metadata = 3;
}
```

**Go caller pattern:**
```go
import (
    "google.golang.org/grpc/status"
    "google.golang.org/grpc/codes"
    errdetails "google.golang.org/genproto/googleapis/rpc/errdetails"
)

resp, err := client.DoOperation(ctx, req)
if err != nil {
    st, ok := status.FromError(err)
    if !ok {
        // Not a gRPC status error — network error
    }
    
    switch st.Code() {
    case codes.InvalidArgument:
        // Check details for field violations
        for _, detail := range st.Details() {
            switch d := detail.(type) {
            case *errdetails.BadRequest:
                for _, v := range d.FieldViolations {
                    fmt.Printf("Field %s: %s\n", v.Field, v.Description)
                }
            }
        }
    case codes.FailedPrecondition:
        // Check details for precondition info
        for _, detail := range st.Details() {
            if pf, ok := detail.(*errdetails.PreconditionFailure); ok {
                for _, v := range pf.Violations {
                    fmt.Printf("Precondition %s/%s: %s\n", v.Type, v.Subject, v.Description)
                }
            }
        }
    case codes.Unavailable:
        // Check RetryInfo for backoff
    case codes.Internal:
        // Infrastructure failure
    }
}
```

### Per-Operation vs. Uniform Outcome Types

gRPC uses a uniform `Status` structure for all RPC outcomes. There are no per-RPC typed result envelopes. The structured error details in trailing metadata provide per-operation extensibility.

### Partial Successes

gRPC does not have a first-class "partial success" result type. Server streaming RPCs can stream individual item results and then close with a final status. Batch APIs typically return a custom response type containing per-item statuses alongside the overall RPC status.

---

## HTTP Problem Details (RFC 7807)

**Sources:** RFC 7807 "Problem Details for HTTP APIs" (IETF, 2016, updated 2022); RFC 9457 (2023, obsoletes 7807); IANA Problem Type registry

### `application/problem+json` Structure

RFC 7807 (and its successor RFC 9457) defines a standard JSON format for HTTP API error responses:

```json
{
    "type": "https://example.com/probs/out-of-credit",
    "title": "You do not have enough credit.",
    "status": 403,
    "detail": "Your current balance is 30, but that costs 50.",
    "instance": "/account/12345/msgs/abc"
}
```

**Standard members:**

```typescript
interface ProblemDetails {
    // URI reference that identifies the problem type.
    // Dereferencing it SHOULD provide human-readable documentation.
    // Default: "about:blank" (generic HTTP error)
    type: string;     // URI

    // Short, human-readable summary of the problem type.
    // SHOULD NOT change between occurrences.
    title: string;

    // HTTP status code for this occurrence.
    status: number;   // integer

    // Human-readable, specific to THIS occurrence.
    // May change between occurrences of the same type.
    detail: string;

    // URI reference identifying the specific occurrence.
    // May yield further information when dereferenced.
    instance: string; // URI
}
```

### Extension Members for Domain-Specific Data

RFC 7807 explicitly allows (and encourages) additional members for domain-specific error context:

```json
{
    "type": "https://api.example.com/problems/validation-failed",
    "title": "Input validation failed",
    "status": 422,
    "detail": "Multiple fields failed validation.",
    "instance": "/orders/request-456",
    "invalid-fields": [
        {
            "field": "quantity",
            "message": "must be greater than 0",
            "value": -5
        },
        {
            "field": "productId",
            "message": "product not found",
            "value": "PRODUCT-9999"
        }
    ]
}
```

### `type` URI as Machine-Readable Discriminant

The `type` field is the machine-readable discriminant for Problem Details. Clients are expected to branch on `type` to determine how to handle the error:

```python
# Python caller pattern
response = requests.post("/orders", json=order_data)
if response.status_code >= 400:
    problem = response.json()
    match problem.get("type"):
        case "https://api.example.com/problems/validation-failed":
            handle_validation_errors(problem.get("invalid-fields", []))
        case "https://api.example.com/problems/insufficient-credit":
            redirect_to_payment(problem.get("available-credit"))
        case "https://api.example.com/problems/product-unavailable":
            show_alternative_products(problem.get("alternatives", []))
        case _:
            # Unknown problem type — use status code
            handle_by_status_code(response.status_code)
```

### Distinguishing Outcome Types

RFC 7807 leaves the taxonomy of problem types to the API author. The `status` code provides coarse categorization:

| HTTP Status | Conventional Meaning for Problem Details |
|------------|----------------------------------------|
| 400 | Bad Request — malformed syntax or invalid argument |
| 401 | Unauthorized — authentication required |
| 403 | Forbidden — authentication insufficient for this operation |
| 404 | Not Found — resource does not exist |
| 409 | Conflict — state conflict (duplicate, concurrent modification) |
| 422 | Unprocessable Entity — input valid but semantically incorrect |
| 429 | Too Many Requests — rate limited |
| 500 | Internal Server Error — infrastructure failure |
| 503 | Service Unavailable — transient failure |

### `about:blank` Default Type

When a server returns a problem with `type: "about:blank"`, the `title` SHOULD be the standard HTTP reason phrase for the `status` code. This is the minimal Problem Details response — no machine-readable type discrimination.

### RFC 9457 Changes (2023)

RFC 9457 obsoletes RFC 7807 with minor clarifications:
- `type` defaults to `"about:blank"` when absent
- Clarifies that `status` is advisory only; the HTTP response status code governs
- Introduces registry guidance for problem types

### Per-Operation vs. Uniform Outcome Types

RFC 7807/9457 uses a uniform structure. Per-operation type discrimination is provided by the `type` URI.

### Partial Successes

RFC 7807 does not address partial successes. APIs that need to express partial batch results typically return a custom body with per-item `status` fields alongside the overall HTTP 207 Multi-Status response code.

---

## BPMN Error Events

**Sources:** OMG BPMN 2.0 specification (object management group, omg.org); Camunda documentation on BPMN error handling; BPMN.io modeling tool documentation; Flowable documentation

### BPMN Error Event Types

BPMN 2.0 defines error events for process-level exception signaling. The key constructs:

**Start Error Event:** Triggers a sub-process when an error occurs in the containing process.

**End Error Event:** Signals that a process or sub-process ended with an error condition. Propagates up to the nearest catching error boundary event.

**Boundary Error Event:** Attached to a task or sub-process; catches an error thrown by that element and redirects flow to an error handling path.

**Intermediate Throw Error Event:** Explicitly throws a BPMN error mid-process (non-End position).

### Error Object Structure

```xml
<!-- Error object definition in the process definition -->
<error id="insufficientFundsError" 
       name="InsufficientFunds" 
       errorCode="ERR_INSUFFICIENT_FUNDS" />

<!-- End error event (throws the error) -->
<endEvent id="errorEnd">
    <errorEventDefinition errorRef="insufficientFundsError" />
</endEvent>

<!-- Boundary error event (catches the error) -->
<boundaryEvent id="catchError" attachedToRef="processPaymentTask">
    <errorEventDefinition errorRef="insufficientFundsError" />
</boundaryEvent>

<!-- Sequence flow from catch to error handler -->
<sequenceFlow sourceRef="catchError" targetRef="handleInsufficientFundsTask" />
```

### Error Codes

The `errorCode` attribute is the machine-readable discriminant. A boundary error event can:
1. Catch a specific error by `errorCode` — only activates for matching code
2. Catch all errors (no `errorCode` specified) — catches any error thrown by the attached element

```xml
<!-- Catches only ERR_INSUFFICIENT_FUNDS -->
<errorEventDefinition errorRef="insufficientFundsError" />

<!-- Catches any BPMN error -->
<errorEventDefinition />
```

### Distinguishing "Process Rejected Input" vs. "Process Failed" vs. "Process Completed in Error State"

BPMN 2.0 provides structurally distinct event types for different failure semantics:

**Process rejected input (BPMN Error):**
- Modeled with an End Error Event and caught by a Boundary Error Event
- The error is a DESIGNED part of the business process — "this path through the process is an error path"
- Control flow continues on the error-handling path; the process is still executing
- Example: payment declined → redirect to payment-failed sub-process

**Process failed (Escalation or interruption):**
- BPMN Escalation events signal a condition that needs attention but are not necessarily errors
- An interrupting boundary event terminates the attached task; non-interrupting does not
- For system-level failures (infrastructure), BPMN relies on the engine's exception handling (compensations or escalations)

**Process completed in error state (designed terminal error):**
- The process ends with an End Error Event that is NOT caught by any boundary event
- The parent process (or calling process) sees an error signal and must handle it
- If uncaught at any level, the process instance is marked as failed by the engine

### Error Data (Variables Passed with Error)

Camunda-specific (not in BPMN 2.0 base spec): error events can carry output variables:

```xml
<boundaryEvent id="paymentError" attachedToRef="processPaymentTask">
    <errorEventDefinition 
        errorRef="paymentError"
        camunda:errorCodeVariable="errorCode"
        camunda:errorMessageVariable="errorMessage" />
</boundaryEvent>
```

In Flowable/Camunda, the error boundary event can map the errorCode and errorMessage into process variables for use in the error-handling path.

### Full BPMN Error Outcome Space

| Outcome Type | BPMN Construct | Caught By | Process Continues? |
|-------------|---------------|-----------|-------------------|
| Business error (designed) | End Error Event | Boundary Error Event | Yes (on error path) |
| Unhandled business error | End Error Event (uncaught) | Parent process Boundary Error Event | Yes (in parent) |
| Catastrophic failure | Engine exception | Compensation events or external monitoring | No |
| Escalation (non-error) | Escalation Event | Boundary Escalation Event | Yes |
| Cancellation | Cancel Event (transactions) | Boundary Cancel Event | No (compensates) |
| Normal completion | End Event | — | No (complete) |

### Per-Operation vs. Uniform Outcome Types

BPMN does not have operation-level outcome types. Errors are process-model constructs — they are declared in the process diagram and carry an `errorCode` as their discriminant. There is no type system enforcement; the `errorCode` is a string.

### Partial Successes

BPMN Multi-Instance Tasks allow a task to execute N times (parallel or sequential). Partially successful multi-instance execution is modeled by counting completions via output collection variables. The BPMN 2.0 spec does not provide a standard "partial success" outcome type; this is implementation-specific in process engines.

---

## Cross-Cutting Observations (Raw Data Summary)

### How Systems Represent Outcome Space

| System | Outcome Representation | Number of Top-Level Outcomes |
|--------|-----------------------|------------------------------|
| Temporal Update | `(T, error)` — binary, error type string for subtype | 3 (success / validator-reject / handler-fail, same wire type for 2+3) |
| XState v5 Snapshot | `status: 'active' \| 'done' \| 'error' \| 'stopped'` | 4 |
| F# ROP | `Ok(T) \| Error(E)` — E is a typed union | 2 top-level, N effective |
| Rust Result | `Ok(T) \| Err(E)` — E is a typed enum | 2 top-level, N effective |
| Haskell Either | `Left a \| Right b` | 2 |
| Go `(T, error)` | binary, type-assertion for subtype | 2 top-level, N via type switch |
| OPA ResultSet | `len(rs) == 0` vs `len(rs) > 0` vs `err != nil` | 3 (undefined ambiguity within #2) |
| CEL `ref.Val` | success value OR `types.Err` value OR Go error | 3 |
| Kubernetes `Status` | `Status.Reason` enum string | ~18 defined reasons |
| Erlang gen_statem | `{ok, Reply}` / `EXIT` / reply-as-error-tuple | convention-based, 3 |
| DDD Command | Exception / typed error / domain event | typically 3-5 |
| gRPC Status | 17 enumerated status codes + detail extensions | 17 |
| HTTP Problem Details | HTTP status + `type` URI | unbounded (type-per-problem) |
| BPMN Error Events | Error events, escalations, cancellations | ~5 event types |

### Business Rule Rejection vs. Invalid Input vs. Infrastructure Error

| System | Distinguishes BizRule vs. InvalidInput | Distinguishes BizRule vs. Infrastructure |
|--------|--------------------------------------|------------------------------------------|
| Temporal | No (both `ApplicationError`) | Yes (different exception types) |
| XState | N/A (process-level) | Yes (status: 'error') |
| Rust/F# | Yes (typed enum variants) | Yes (separate variant) |
| OPA | No (both empty ResultSet) | Yes (err != nil) |
| CEL | Partially (types.Err details) | Yes (types.Err vs. Go error) |
| Kubernetes | Yes (Invalid vs. InternalError) | Yes |
| Erlang gen_statem | Convention-only | Yes (EXIT vs. reply tuple) |
| DDD | Yes (DomainException vs. ValidationException) | Yes |
| gRPC | Yes (FAILED_PRECONDITION vs. INVALID_ARGUMENT) | Yes (INTERNAL vs. UNAVAILABLE) |
| HTTP Problem Details | Yes (type URI per problem type) | Yes (status 5xx vs. 4xx) |
| BPMN | Yes (Error vs. Escalation) | Yes (Error vs. engine exception) |

---

*Sources: All URLs cited are documented inline per section. Fetched content dated April 2026. Synthesized content from canonical published documentation where direct fetch was not available.*
