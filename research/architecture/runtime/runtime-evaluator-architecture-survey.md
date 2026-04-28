# Runtime Evaluator Architecture Survey

External research: how different rule engines, expression evaluators, configuration languages, and state machine runtimes structure their runtime object model, evaluation strategy, fault representation, versioning, preview/inspect capabilities, result types, and constraint evaluation.

**Scope:** Raw collection of facts about each system's runtime evaluator architecture. No interpretation for Precept, no conclusions, no recommendations.

**Date:** June 2025

**Research angle:** Eight dimensions of runtime evaluator design — (1) runtime object architecture, (2) evaluator design, (3) fault/error representation, (4) compile-time to runtime fault correspondence, (5) entity/activation versioning, (6) inspect/preview architecture, (7) result type design, (8) constraint evaluation model.

---

## CEL (Common Expression Language) — cel-go

Source: cel-go GitHub `google/cel-go`; `cel.go` (Program interface), `interpreter/interpretable.go`, `common/types/err.go`, `cel/env.go`; CEL Language Specification https://github.com/google/cel-spec

### Runtime Object Architecture

CEL's runtime is organized around three main types:

- **`Env`** — The environment holds type declarations, variable declarations, and function bindings. It is the shared context for compiling and evaluating CEL programs.
- **`Ast`** — The checked (type-checked) abstract syntax tree produced by `Env.Check()`.
- **`Program`** — The executable form, produced by `Env.Program(ast)`. A Program is the unit of evaluation.

The `Env` → `Ast` → `Program` pipeline separates compilation from evaluation. A `Program` can be evaluated many times against different `Activation` bindings without re-parsing or re-type-checking.

```go
// Simplified cel-go usage
env, _ := cel.NewEnv(cel.Variable("x", cel.IntType))
ast, iss := env.Compile(`x > 10`)
prg, _ := env.Program(ast)
out, det, err := prg.Eval(map[string]interface{}{"x": 42})
```

### Evaluator Design

CEL uses **tree-walking interpretation** via the `Interpretable` interface in `interpreter/interpretable.go`. The interpretable tree mirrors the AST structure but is optimized for evaluation:

- Each node implements `Eval(Activation) ref.Val` — a single method that evaluates the node against a variable binding.
- The `Activation` interface provides variable resolution: `ResolveName(name string) (interface{}, bool)`. Activations are stackable — `NewHierarchicalActivation(parent, child)` creates scoped bindings.
- The interpreter supports both **standard evaluation** (short-circuiting `&&`, `||`, ternary) and **exhaustive evaluation** (`ExhaustiveEval`) which evaluates all branches regardless of short-circuit conditions.

The `ExhaustiveEval` mode is specifically designed for observability — it forces every subexpression to be evaluated so that cost tracking and observation callbacks fire on all branches.

**Cost tracking** is built into the evaluator. The `CostTracker` tallies per-operation costs during evaluation. This enables cost-bounded evaluation: callers can set a `CostLimit` that aborts evaluation if exceeded.

**`EvalObserver`** is a callback pattern: `type EvalObserver func(id int64, programStep any, val ref.Val)`. Observers fire after every expression node is evaluated, enabling logging, debugging, and preview tooling.

### Fault/Error Representation

CEL's error model uses the `types.Err` type, which implements `ref.Val` — errors are first-class values in the evaluation result:

```go
type Err struct {
    id  int64
    val error
}
```

The `Err` type is a value in the type algebra: it participates in expressions like any other value. When a subexpression produces an `Err`, operators propagate it upward. This is NOT exception-based — errors flow through the expression tree as data.

Predefined runtime fault codes include:
- `errDivideByZero` — division by zero
- `errModulusByZero` — modulus by zero
- `errIntOverflow` — integer arithmetic overflow
- `errTimestampOverflow` — timestamp out of representable range
- `errDurationOverflow` — duration out of range

There is also `EvalCancelledError` with a `CancellationCause` enum distinguishing between timeout and explicit cancellation.

The `Eval()` method returns a **three-value result**: `(ref.Val, *EvalDetails, error)`:
- `ref.Val` — the computed value (which may itself be an `Err` value)
- `*EvalDetails` — optional cost and observation data
- `error` — Go-level error for catastrophic failures (nil in normal operation)

This three-value return distinguishes between: (a) successful evaluation producing a result value, (b) evaluation producing an error *value* within the CEL type system, and (c) infrastructure failure that prevents evaluation entirely.

### Compile-time to Runtime Fault Correspondence

CEL's type checker catches type mismatches, undefined variables, and arity errors at check time. The `Issues` type from `env.Check()` carries diagnostics.

At runtime, the faults that can still occur are:
- Division/modulus by zero (value-dependent, unknowable at type-check time)
- Integer overflow (value-dependent)
- Timestamp/duration overflow (value-dependent)
- Cost limit exceeded (execution-dependent)
- Cancellation (external signal)

There is **no formal mapping** between compile-time diagnostic codes and runtime fault codes. The type checker and runtime evaluator use separate error vocabularies. The type checker eliminates *type-level* errors; the runtime handles *value-level* errors.

### Entity/Activation Versioning

CEL programs are stateless — they evaluate a pure expression against an immutable `Activation` binding. There is no concept of entity versioning or mutable state across evaluations. Each call to `Eval()` produces a fresh result from the provided activation.

However, `Env` supports **extension and composition**: `env.Extend(declarations...)` creates a new environment that inherits all declarations from the parent. This enables layered declaration versioning.

### Inspect/Preview Architecture

CEL's inspect capabilities:
- **Exhaustive evaluation** (`ExhaustiveEval`) disables short-circuit semantics, forcing all subexpression branches to evaluate. This is a built-in preview mode — it shows what every branch *would* compute.
- **`EvalObserver`** fires on every evaluated node, providing the expression ID, the program step type, and the computed value. This enables tooling to reconstruct the full evaluation trace.
- **`EvalDetails`** returns the actual evaluation cost, enabling callers to preview cost before committing to a policy decision.
- **Partial evaluation**: CEL supports `PartialVars` — an activation where some variables are declared as `UnknownAttribute`. The evaluator returns `UnknownValue` for subexpressions that depend on missing variables. This enables partial preview: "given what we know, here is what we can determine."

### Result Type Design

The primary result type is `ref.Val`, CEL's universal value interface:

```go
type Val interface {
    ConvertToNative(typeDesc reflect.Type) (interface{}, error)
    ConvertToType(typeValue Type) Val
    Equal(other Val) Val
    Type() Type
    Value() interface{}
}
```

`ref.Val` is a tagged union — every CEL value (including errors and unknowns) implements this interface. The `Type()` method returns the CEL type, enabling runtime type dispatch.

The three-value `Eval()` return `(ref.Val, *EvalDetails, error)` means callers must distinguish between:
1. `err != nil` → infrastructure failure, `val` is meaningless
2. `err == nil && types.IsError(val)` → CEL-level error (e.g., divide by zero)
3. `err == nil && types.IsUnknown(val)` → partial evaluation with missing variables
4. `err == nil` → successful evaluation, `val` carries the result

### Constraint Evaluation Model

CEL is a pure expression evaluator — it does not have a built-in constraint model. Constraints are expressed as boolean expressions. The calling system (e.g., Google IAM, Kubernetes admission controllers) interprets a `false` result as a constraint violation. There is no `reject`, `assert`, or `require` primitive in the language itself.

The cost tracking system acts as a meta-constraint: if evaluation cost exceeds the configured limit, evaluation is aborted. This is an infrastructure constraint, not a business-logic constraint.

---

## OPA / Rego (Open Policy Agent)

Source: OPA GitHub `open-policy-agent/opa`; `rego/rego.go`, `topdown/eval.go`, `ast/policy.go`; OPA Documentation https://www.openpolicyagent.org/docs/latest/

### Runtime Object Architecture

OPA's runtime is organized around:

- **`Rego`** — The top-level evaluation entry point. Constructed via builder pattern: `rego.New(rego.Query("data.example.allow"), rego.Module(...))`.
- **`PreparedEvalQuery`** — A pre-processed query optimized for repeated evaluation. Created by `Rego.PrepareForEval(ctx)`.
- **`Store`** — An in-memory data store holding base documents (JSON data that policies operate on). The store supports transactions.

The `Rego` → `PreparedEvalQuery` separation mirrors CEL's `Env` → `Program` split: compilation is separated from evaluation, and the prepared form is reusable.

```go
query, _ := rego.New(
    rego.Query("data.authz.allow"),
    rego.Module("authz.rego", policy),
).PrepareForEval(ctx)

rs, _ := query.Eval(ctx, rego.EvalInput(input))
```

### Evaluator Design

OPA uses **top-down evaluation** with backtracking. The `topdown` package implements a search-based evaluator that:

1. Starts from the query and works backward through rules to find satisfying assignments.
2. Uses **unification** to bind variables.
3. Supports **backtracking** when a rule body does not match, trying alternative rule definitions.

This is fundamentally a **logic programming** evaluator (Datalog-inspired), not a tree-walking expression evaluator. Rules are tried in order, and partial evaluation produces residual queries.

The `EvalContext` carries:
- **Store transaction** — data reads happen within a consistent snapshot
- **Tracing** — an optional tracer that records every evaluation step
- **Caching** — memoization of rule evaluation results for performance
- **Partial evaluation** — the evaluator can compute residual policies when input is incomplete

### Fault/Error Representation

OPA uses two levels of error handling:

1. **Policy-level undefined**: When a query has no satisfying assignments, the result set is empty. This is not an error — it is the defined semantics of "the policy does not permit this action." An empty result set means denial.

2. **Runtime errors**: OPA has a `StrictBuiltinErrors` option. When enabled, built-in function errors (type mismatches in built-in calls, JSON parsing failures) become hard errors that halt evaluation. When disabled (the default), these errors cause the enclosing rule to be undefined (effectively treating the error as "this rule does not apply").

The `BuiltinErrorList` collects non-fatal built-in errors when strict mode is off.

OPA does not use exceptions. Errors are represented as Go-level `error` values returned from evaluation functions.

### Compile-time to Runtime Fault Correspondence

Rego has a type checker (`ast.TypeChecker`) that performs static analysis on policies. The type checker detects:
- Type mismatches in built-in function arguments
- Unreachable rules (dead code)
- Recursive definitions (which are prohibited in Rego)

At runtime, the faults that remain are value-dependent:
- Built-in function errors (e.g., regex compilation failure, JSON parse error)
- Undefined references to data paths that don't exist in the store

There is no formal mapping between type-checker warnings and runtime errors. The type checker uses a separate diagnostic vocabulary.

### Entity/Activation Versioning

OPA's store supports **transactions**: `store.NewTransaction(ctx, storage.WriteParams)` creates a consistent snapshot. Multiple evaluations within the same transaction see the same data. Changes to the store (via `store.Write()`) are committed atomically.

There is no entity versioning in the lifecycle sense — OPA evaluates policies against point-in-time snapshots of data.

**Bundle versioning**: OPA supports loading policy and data bundles with revision strings. The runtime tracks which bundle revision is active, enabling rollback and audit.

### Inspect/Preview Architecture

OPA provides several inspection mechanisms:

- **Tracing**: `rego.EvalTracer(tracer)` attaches a tracer that records every evaluation step — rule entry, rule exit, variable bindings, unification attempts. The `topdown.BufferTracer` captures a complete evaluation trace as structured events.
- **Partial evaluation**: `rego.Partial(query, unknowns)` evaluates a query with some inputs unknown, producing a **residual policy** — a simplified policy that captures what remains to be decided. This is a structural preview: "given what we know, here are the remaining conditions."
- **Decision logging**: OPA's decision log captures query, input, result, and timing for every evaluation. This is an operational audit trail, not a preview mechanism.

### Result Type Design

OPA's `ResultSet` is a slice of `Result` values:

```go
type ResultSet []Result

type Result struct {
    Expressions []*ExpressionValue
    Bindings    Bindings
}

type ExpressionValue struct {
    Value    interface{}
    Text     string
    Location *Location
}
```

Each `Result` contains the bindings (variable assignments) that satisfy the query and the evaluated expression values. An empty `ResultSet` means no satisfying assignments exist (denial in the policy context).

### Constraint Evaluation Model

Rego rules are **conjunctive**: every statement in a rule body must be satisfied for the rule to fire. The constraint model is:

```rego
allow {
    input.method == "GET"
    input.path[0] == "public"
}
```

Every line is a constraint. If any constraint fails, the rule is undefined (does not contribute to the result). Multiple rules with the same name form a **disjunction** (any rule can make the decision true). This is a Datalog-style constraint model: conjunction within rules, disjunction across rules, negation via `not`.

The `deny` pattern collects violation messages:

```rego
deny[msg] {
    not input.user
    msg := "user is required"
}
```

This produces a *set* of violation messages — all failing constraints are collected, not just the first.

---

## XState v5

Source: XState GitHub `statelyai/xstate`; `packages/core/src/actors/`, `packages/core/src/StateMachine.ts`, `packages/core/src/State.ts`; XState v5 documentation https://stately.ai/docs/xstate-v5

### Runtime Object Architecture

XState v5 is built on an **actor model**:

- **`ActorLogic<TSnapshot, TEvent, TInput, TSystem>`** — The abstract interface for any actor's behavior. It defines `transition()`, `getInitialSnapshot()`, `getPersistedSnapshot()`, and `restoreSnapshot()`.
- **`Actor`** — A running instance created by `createActor(logic, options)`. Actors have a lifecycle: created → started → stopped/done/error.
- **`Snapshot`** — The immutable state of an actor at a point in time. For state machines, this is `MachineSnapshot` containing `value` (current state), `context` (extended state data), `status`, `output`, and `error`.

The `ActorLogic` interface is the core abstraction — state machines are one implementation, but promise logic, observable logic, callback logic, and transition logic are all `ActorLogic` implementations.

```typescript
interface ActorLogic<TSnapshot, TEvent, TInput, TSystem> {
  transition(
    snapshot: TSnapshot,
    event: TEvent,
    actorScope: ActorScope<TSnapshot, TEvent, TSystem>
  ): TSnapshot;
  
  getInitialSnapshot(
    actorScope: ActorScope<TSnapshot, TEvent, TSystem>,
    input: TInput
  ): TSnapshot;
  
  getPersistedSnapshot(snapshot: TSnapshot): unknown;
  restoreSnapshot?(snapshot: unknown): TSnapshot;
}
```

### Evaluator Design

XState v5 provides **two evaluation modes**:

1. **Stateful actor evaluation**: `createActor(machine).start()` creates a running actor that maintains internal state. Events sent via `actor.send(event)` cause state transitions. The actor manages subscriptions, invocations, and side effects.

2. **Pure functional evaluation**: `transition(machine, currentState, event)` is a pure function that returns `[nextState, actions]` — the next snapshot and the list of actions to execute. This is the stateless core: no side effects, no subscriptions, no actor lifecycle.

The pure `transition()` function returns a tuple of `[Snapshot, ActionObject[]]`, clearly separating the state computation from the side effects.

Additionally, `getNextSnapshot(machine, currentState, event)` returns just the next snapshot without actions — a convenience for preview scenarios.

### Fault/Error Representation

XState v5 `Snapshot` has a `status` field with three possible values:

```typescript
type SnapshotStatus = 'active' | 'done' | 'error';
```

- `'active'` — the actor is running normally
- `'done'` — the actor reached a final state, `output` contains the final value
- `'error'` — the actor encountered an unrecoverable error, `error` contains the error value

Errors in XState v5 are structural — they are part of the snapshot, not thrown exceptions. A machine can explicitly escalate errors via the `escalate` action, which sets `status: 'error'` on the parent actor.

Guard evaluation failures are not errors — a guard returning `false` simply means the transition is not taken. The event is silently ignored if no transition's guard is satisfied.

There is no built-in constraint violation type. Business-rule rejections must be modeled as explicit states (e.g., a `rejected` state) or via guard logic that blocks transitions.

### Compile-time to Runtime Fault Correspondence

XState v5 has TypeScript type generation (`typegen`) that provides compile-time checking of:
- Event types that are valid in each state
- Context type shape
- Guard names that must be defined
- Action names that must be implemented

There is no formal mapping between TypeScript type errors and runtime status values. TypeScript catches structural errors (wrong event name, missing guard implementation); runtime status catches value-dependent failures (guard evaluation failure is silent, action execution error sets `status: 'error'`).

### Entity/Activation Versioning

XState v5 supports **snapshot persistence and restoration**:

- `actor.getPersistedSnapshot()` serializes the current state to a plain JSON object.
- `createActor(machine, { snapshot: persisted })` restores an actor from a persisted snapshot.

This enables entity versioning: an actor's state can be persisted, the machine definition can be updated, and the actor can be restored against the new definition. XState does **not** perform automatic schema migration — if the machine definition changes incompatibly, restoration may produce undefined behavior.

There is no built-in versioning mechanism for the machine definition itself. Version tracking is the responsibility of the application.

### Inspect/Preview Architecture

XState v5 has extensive inspection capabilities:

- **Pure `transition()` function**: Because `transition(machine, state, event)` is pure and returns a new snapshot without side effects, it serves directly as a preview/dry-run mechanism. The caller can compute the next state for any event without committing.
- **`getNextSnapshot()`**: A convenience function that returns only the next snapshot (no action list), specifically for preview scenarios.
- **`actor.subscribe(observer)`**: Live observation of state changes with `next`, `error`, and `complete` callbacks.
- **Inspection API**: `createActor(machine, { inspect: callback })` registers an inspector that receives structured inspection events for every state transition, action execution, and event send.
- **`@xstate/graph`**: A companion package that enumerates all reachable states and transitions from a machine definition, producing a complete state graph for analysis.

### Result Type Design

The `MachineSnapshot` carries all state information:

```typescript
interface MachineSnapshot<TContext, TEvent> {
  value: StateValue;        // Current state(s) - string or nested object for parallel
  context: TContext;         // Extended state data
  status: 'active' | 'done' | 'error';
  output?: unknown;          // Final output when status === 'done'
  error?: unknown;           // Error value when status === 'error'
  tags: Set<string>;         // Tags from the current state configuration
  can(event: TEvent): boolean;  // Preview: can this event cause a transition?
  matches(stateValue: StateValue): boolean;
  hasTag(tag: string): boolean;
}
```

The `can(event)` method is a built-in preview: it returns `true` if sending the event would cause a state change, without actually performing it.

### Constraint Evaluation Model

XState v5 uses **guards** as its constraint mechanism:

```typescript
createMachine({
  on: {
    submit: {
      guard: ({ context }) => context.items.length > 0,
      target: 'submitted'
    }
  }
});
```

Guards are boolean functions evaluated synchronously. A guard returning `false` blocks the transition — the event is silently dropped. There is no built-in mechanism to report *why* a guard failed or to collect all failing guards.

Multiple transitions on the same event with different guards are tried in declaration order — the first satisfied guard wins. This is a **first-match** model, not a collect-all-violations model.

---

## Temporal (.NET SDK)

Source: Temporal .NET SDK GitHub `temporalio/sdk-dotnet`; `src/Temporalio/Workflows/Workflow.cs`; Temporal documentation https://docs.temporal.io/develop/dotnet

### Runtime Object Architecture

Temporal workflows execute as deterministic replay-safe functions:

- **`Workflow`** — A static class providing the workflow execution context. All workflow-level operations (timers, activities, signals, queries) are accessed through `Workflow.XXX` static methods.
- **`WorkflowDefinition`** — The user-authored workflow class, decorated with `[Workflow]` and `[WorkflowRun]` attributes.
- **`ActivityDefinition`** — Side-effecting operations that run outside the deterministic workflow sandbox.

The architecture strictly separates deterministic orchestration logic (workflows) from non-deterministic side effects (activities). The workflow function is replayed from its event history on every recovery — the same input + history must produce the same output.

```csharp
[Workflow]
public class GreetingWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync(string name)
    {
        var greeting = await Workflow.ExecuteActivityAsync(
            (GreetingActivities a) => a.Compose(name),
            new() { StartToCloseTimeout = TimeSpan.FromMinutes(5) });
        return greeting;
    }
}
```

### Evaluator Design

Temporal's "evaluator" is a **deterministic replay engine**:

1. On first execution, the workflow function runs forward, and each side-effecting call (activity, timer, child workflow) records a completion event in the event history.
2. On replay, the workflow function re-executes from the beginning, and each side-effecting call is matched against the recorded event history. If the history has a matching completion, the call returns immediately with the recorded result.

This is not tree-walking evaluation — it is **re-execution with history matching**. The workflow code runs as normal C# code, but the `Workflow` static methods intercept calls and route them through the replay mechanism.

Determinism is enforced by providing workflow-safe replacements for non-deterministic operations:
- `Workflow.UtcNow` instead of `DateTime.UtcNow`
- `Workflow.Random` instead of `System.Random`
- `Workflow.NewGuid()` instead of `Guid.NewGuid()`

### Fault/Error Representation

Temporal uses a typed exception hierarchy:

- **`ActivityFailureException`** — wraps failures from activity execution. Contains `InnerException` with the original activity error.
- **`ChildWorkflowFailureException`** — wraps failures from child workflow execution.
- **`ApplicationFailureException`** — the general-purpose failure type for business-logic errors. Has `ErrorType` (string), `Message`, and `Details` (serializable payload).
- **`CancelledFailureException`** — workflow or activity was cancelled.
- **`TimeoutFailureException`** — activity or workflow timed out.
- **`ContinueAsNewException`** — not an error; a control flow mechanism that restarts the workflow with new parameters (used for long-running workflows that accumulate too much history).

`ApplicationFailureException` is the primary mechanism for business-rule rejections. The `ErrorType` string enables callers to distinguish failure categories without parsing messages. The `NonRetryable` property controls whether the Temporal server should retry the operation.

### Compile-time to Runtime Fault Correspondence

Temporal has **no compile-time analysis** of workflow correctness. The `[Workflow]` and `[WorkflowRun]` attributes are validated at registration time (not compile time). Determinism violations are detected only at runtime during replay — if the replayed execution diverges from the recorded history, a `NonDeterminismException` is thrown.

There is no static analysis tool that checks whether a workflow is deterministic. The compile-time surface is standard C# type checking; Temporal-specific faults are entirely runtime phenomena.

### Entity/Activation Versioning

Temporal has a **patching** mechanism for workflow versioning:

```csharp
if (Workflow.Patched("my-change-id"))
{
    // New behavior
}
else
{
    // Old behavior
}
```

`Workflow.Patched(id)` returns `true` for new executions and `false` for replaying executions that started before the patch was deployed. This enables safe migration of workflow code while maintaining replay compatibility with in-flight executions.

`Workflow.DeprecatePatch(id)` marks a patch as fully migrated — all in-flight workflows using the old behavior have completed. After deprecation, the old code path can be removed.

**`ContinueAsNew`** provides entity-level versioning: a long-running workflow can restart itself with fresh state, effectively creating a new "version" of the entity with a clean event history.

`Unsafe.IsReplaying` is a boolean flag available during workflow execution that indicates whether the current execution is a replay. This is intended for logging and observability — logs during replay are typically suppressed to avoid duplicates.

### Inspect/Preview Architecture

Temporal workflows are inspectable via **queries**:

- **`[WorkflowQuery]`** methods are read-only handlers that return information about the workflow's current state without advancing execution.
- Queries execute against the current workflow state snapshot — they do not modify state or generate events.

There is no built-in "what-if" or dry-run mechanism. You cannot ask "what would happen if I sent this signal?" without actually sending it. The query mechanism provides read-only inspection but not predictive preview.

The **event history** itself serves as a complete audit trail — every activity completion, timer firing, signal receipt, and state change is recorded.

### Result Type Design

Workflow results are returned as the return value of the `[WorkflowRun]` method. Failures are communicated via exceptions (the Temporal failure hierarchy described above).

The Temporal client receives results via `WorkflowHandle<TResult>`:

```csharp
var handle = await client.StartWorkflowAsync(
    (GreetingWorkflow wf) => wf.RunAsync("World"),
    new(id: "greeting-1", taskQueue: "greetings"));

var result = await handle.GetResultAsync(); // throws on failure
```

`GetResultAsync()` returns the workflow's return value or throws a `WorkflowFailedException` wrapping the workflow's failure. The result/error distinction is a standard C# success-or-exception pattern.

### Constraint Evaluation Model

Temporal has no built-in constraint evaluation model. Business rules are expressed in ordinary C# code within the workflow function. Constraints are implemented as standard `if/throw` logic or by invoking validation activities.

The determinism requirement is itself a constraint — but it is enforced by the runtime (via replay divergence detection), not by a declarative constraint language.

---

## Dhall

Source: Dhall GitHub `dhall-lang/dhall-haskell`; `dhall/src/Dhall/Eval.hs`; Dhall Language Specification https://github.com/dhall-lang/dhall-lang/blob/master/standard/semantics.md

### Runtime Object Architecture

Dhall's runtime is structured around two representations:

- **`Expr`** — The surface-syntax AST, parameterized by import resolution status. `Expr Void a` has all imports resolved; `Expr Src a` retains source positions.
- **`Val`** — The evaluated (normalized) form. Values in Dhall are either fully reduced (normal form) or contain free variables (neutral terms).

The evaluation function is:

```haskell
eval :: Environment a -> Expr Void a -> Val a
```

where `Environment` is a stack of variable bindings (a de Bruijn-indexed environment).

Dhall separates three phases: **parsing** → **import resolution** → **type checking** → **normalization (evaluation)**. Each phase is a pure function with no side effects (import resolution reads files but is treated as an effect that completes before pure evaluation begins).

### Evaluator Design

Dhall uses an **eval-apply environment machine** — a call-by-need evaluation strategy:

1. **Closures**: Lambda abstractions capture their environment at definition time.
2. **Environment**: Variable lookup is by de Bruijn index into the environment stack.
3. **Normalization**: Every well-typed expression normalizes to a unique normal form. There are no non-terminating computations.

Dhall is **not Turing-complete** by design. The language has no general recursion, no unbounded loops, and no fixpoint combinators. Every type-correct program terminates. This is the defining property: **if it type-checks, evaluation always succeeds in finite time.**

The evaluator never needs to handle timeout, stack overflow, or infinite loops. These are structurally impossible.

### Fault/Error Representation

Dhall has a radical error model: **there are no runtime errors after type checking.**

If a Dhall expression passes the type checker, evaluation is guaranteed to succeed. The type system prevents:
- Division by zero (there is no division operator in the standard library; numeric operations are limited to `Natural/fold`, `Natural/build`, etc.)
- Null pointer / missing field access (the type system ensures all accessed fields exist)
- Type mismatches (caught by the type checker)
- Non-termination (prevented by the totality guarantee)

The only "errors" in Dhall are:
- **Parse errors** — malformed syntax (pre-type-check)
- **Import resolution errors** — missing files or circular imports (pre-type-check)
- **Type errors** — type mismatches, unresolved variables (pre-type-check)

Once an expression is type-checked, `eval` is a total function — it always produces a `Val`.

### Compile-time to Runtime Fault Correspondence

Dhall has **total correspondence**: every possible fault is caught at type-check time. There are zero runtime fault categories. The compile-time to runtime fault mapping is:

| Compile-time diagnostic | Runtime fault | 
|---|---|
| Type mismatch | N/A (impossible) |
| Unresolved variable | N/A (impossible) |
| Non-termination | N/A (structurally prevented) |
| Division by zero | N/A (no division operator) |

This is the strongest possible compile-time to runtime correspondence: the runtime fault set is empty.

### Entity/Activation Versioning

Dhall does not have a concept of mutable entities or activation records. Every expression is evaluated from scratch. There is no persistent state, no mutation, and no versioning.

However, Dhall has a **semantic hash** system: every expression has a content-addressable hash based on its alpha-normal form. This provides expression-level versioning — two expressions with the same hash are guaranteed to be semantically identical.

```
sha256:abcdef1234567890... -- content address of a Dhall expression
```

Imports can be frozen with a hash: `https://example.com/config.dhall sha256:abc...`. If the remote content changes and no longer matches the hash, import resolution fails. This is a form of dependency versioning.

### Inspect/Preview Architecture

Dhall's evaluation is inherently inspectable because it is pure and total:

- **Normalization is preview**: evaluating a Dhall expression IS the preview — the result is deterministic and there are no side effects. Running `dhall` on a configuration file shows exactly what the final value will be.
- **Type inference**: `dhall type` shows the inferred type of any expression, which is a structural preview of the value's shape.
- **Alpha-normalization**: `dhall normalize --alpha` normalizes variable names, enabling structural comparison of expressions.
- **Diffing**: `dhall diff expr1 expr2` computes a semantic diff between two Dhall expressions.

There is no need for a separate "dry-run" mode because evaluation itself has no side effects.

### Result Type Design

The result of Dhall evaluation is simply `Val a` — a value in normal form. There is no error case, no result-or-error wrapper. The type signature:

```haskell
eval :: Environment a -> Expr Void a -> Val a
```

is a total function. The caller never needs to handle failure cases after type checking.

For the `Optional` type, Dhall uses `Some value` and `None T` — absence is typed and explicit, not an error. This is the only "might be missing" construct, and it is part of the value, not a runtime failure mode.

### Constraint Evaluation Model

Dhall has no runtime constraint evaluation. All constraints are enforced by the type system:

- **Type constraints** — enforced at type-check time
- **Totality constraint** — enforced by the absence of general recursion
- **Import integrity constraints** — enforced by semantic hashing at import resolution time

There is no mechanism for business-rule assertions, invariants, or runtime preconditions. If you need to express "this number must be positive," you either encode it in the type (using a specialized type) or validate after Dhall evaluation in the host language.

---

## CUE

Source: CUE GitHub `cue-lang/cue`; `cue/types.go`, `internal/core/adt/expr.go`, `internal/core/adt/eval.go`; CUE documentation https://cuelang.org/docs/

### Runtime Object Architecture

CUE's runtime is built around **lattice-based values**:

- **`Value`** — The primary user-facing type, wrapping an internal `*adt.Vertex` with a `*runtime.Runtime`. A `Value` represents a point in CUE's value lattice.
- **`adt.Vertex`** — The internal representation of a value node. Vertices form a tree mirroring the CUE value structure. Each vertex can be in various states of evaluation (unevaluated, partially evaluated, finalized).
- **`runtime.Runtime`** — The evaluation context holding the index of all loaded CUE instances.

CUE's fundamental operation is **unification**: combining two values to produce a value that satisfies both constraints simultaneously. Values form a lattice where `Top` (⊤) is the unconstrained value, `Bottom` (⊥) is the inconsistent/error value, and unification computes the greatest lower bound.

```go
a := ctx.CompileString(`{name: string, age: int}`)
b := ctx.CompileString(`{name: "Alice", age: > 0}`)
c := a.Unify(b)
// c represents: {name: "Alice", age: int & > 0}
```

### Evaluator Design

CUE uses **lazy evaluation with unification**:

1. Values are not fully evaluated until needed. A vertex can be in an unevaluated state, holding only its source expressions.
2. When a value is queried (e.g., exported, validated), it is **finalized** — all unifications are computed, constraints are checked, and the value is reduced to its canonical form.
3. The `Finalize()` operation on a vertex triggers full evaluation.

The unification operation (`Value.Unify(other)`) computes the lattice meet of two values. If the values are incompatible, the result is `Bottom` — CUE's error value.

CUE's evaluation is **not** tree-walking in the traditional sense. It is closer to constraint propagation / lattice reduction: each value is the intersection of all constraints that apply to it.

### Fault/Error Representation

CUE uses **Bottom (⊥)** as its universal error representation:

```go
type Bottom struct {
    Src  ast.Node
    Err  errors.Error
    Code ErrorCode
    // ...
}
```

A `Bottom` value represents an evaluation failure — incompatible constraints, type mismatches, or incomplete data. The `ErrorCode` distinguishes:
- **`IncompleteError`** — the value is not yet concrete (has free variables or unevaluated parts)
- **`CycleError`** — circular reference detected
- **`EvalError`** — general evaluation failure

`Value.Err()` returns the error if the value is Bottom, or nil if the value is valid. `IsConcrete()` checks whether a value is fully resolved to a concrete value (not just a constraint).

Errors in CUE are **values, not exceptions**. A Bottom value participates in the lattice like any other value — unifying anything with Bottom produces Bottom. Errors propagate structurally through the value tree.

### Compile-time to Runtime Fault Correspondence

CUE blurs the compile-time / runtime boundary because validation and evaluation are the same operation. There is no separate "compile" step that produces a different artifact — CUE values are always in the lattice, and "evaluation" is constraint reduction.

However, there are phases:
- **Parsing** — syntax errors
- **Building** — reference resolution, scope errors
- **Evaluation** — constraint unification, type checking, concreteness checking

All of these produce `errors.Error` values. The error representation is uniform across phases. CUE does not distinguish "this error was caught early" from "this error was caught late" — all errors are structural incompatibilities in the value lattice.

### Entity/Activation Versioning

CUE does not have mutable entities. Values are immutable — `Unify()` creates a new value; it does not modify the inputs.

CUE has a **module system** with semantic versioning. Modules can depend on specific versions of other modules. The `cue.mod/module.cue` file specifies dependencies with version constraints. This provides package-level versioning.

### Inspect/Preview Architecture

CUE's inspection model is based on **export profiles**:

```go
type Options struct {
    Concrete       bool  // require all values to be concrete
    Raw            bool  // return unevaluated form
    Final          bool  // fully evaluate, no free variables
    OmitHidden     bool  // hide hidden fields (prefixed with _)
    OmitDefinitions bool // hide definition fields (prefixed with #)
    OmitOptional   bool  // hide optional fields (suffixed with ?)
    ShowErrors     bool  // include Bottom values in output
}
```

These options control what parts of the value lattice are visible in the output. The `Concrete: false` mode is essentially a preview — it shows the value with all constraints applied but without requiring all values to be fully determined.

Additional inspection:
- `Value.Subsume(other)` checks whether one value is a supertype of another — useful for API compatibility checking.
- `Value.Validate(options...)` checks whether a value satisfies concreteness and other constraints, returning errors.
- `Value.LookupPath(path)` navigates the value tree to inspect subvalues.

### Result Type Design

CUE's primary result type is `Value`, which is a lattice point that may be:
- **Concrete** — a fully determined value (e.g., `"hello"`, `42`)
- **Constrained** — a value with remaining constraints (e.g., `int & > 0`)
- **Bottom** — an error/inconsistency

There is no separate result-or-error type. The `Value` itself carries its error state:

```go
v := ctx.CompileString(`1 & 2`) // incompatible constraints
if err := v.Err(); err != nil {
    // v is Bottom
}
```

### Constraint Evaluation Model

CUE's entire evaluation model **is** constraint evaluation. Every CUE value is a constraint, and evaluation is constraint unification:

- **Type constraints**: `string`, `int`, `float`, `bool` — restricts the value to a type
- **Bound constraints**: `> 0`, `< 100`, `>= 1 & <= 10` — restricts the numeric range
- **Pattern constraints**: `=~ "^[a-z]+$"` — restricts strings by regex
- **Structural constraints**: `{name: string, age: int}` — restricts object shape
- **Disjunction constraints**: `"red" | "green" | "blue"` — restricts to enumerated values

Constraints compose via unification (conjunction). Incompatible constraints produce Bottom. The constraint evaluation model is:

1. All constraints on a value are collected.
2. Unification computes the greatest lower bound.
3. If the result is Bottom, a constraint violation has occurred.
4. `Validate(Concrete(true))` checks that the final value is fully determined.

CUE does not have a separate "validate then evaluate" flow — validation IS evaluation.

---

## Eiffel — Design by Contract (DbC)

Source: Bertrand Meyer, "Object-Oriented Software Construction" 2nd ed. (1997); ECMA-367 Standard "Eiffel: Analysis, Design and Programming Language"; Eiffel Documentation https://www.eiffel.org/doc/eiffel/ET-_Design_by_Contract; ISE EiffelStudio Runtime

### Runtime Object Architecture

Eiffel's runtime is a conventional object-oriented system. The DbC architecture adds contract annotations to the standard class/method structure:

- **Preconditions** (`require` block) — assertions that must hold before a routine (method) executes.
- **Postconditions** (`ensure` block) — assertions that must hold after a routine returns.
- **Class invariants** (`invariant` block) — assertions that must hold whenever an object is in a stable state (between method calls).

```eiffel
deposit (amount: REAL) is
    require
        positive_amount: amount > 0
    do
        balance := balance + amount
    ensure
        balance_increased: balance = old balance + amount
    end
```

Contracts are part of the class interface, not the implementation. They are visible to clients and subclasses.

### Evaluator Design

Eiffel's evaluator is a standard OO method dispatch mechanism. The contract layer adds assertion checking around method invocations:

1. Before method entry: evaluate `require` block. If any precondition fails, raise a `PRECONDITION` violation.
2. Execute method body.
3. After method return: evaluate `ensure` block. If any postcondition fails, raise a `POSTCONDITION` violation.
4. After any public method returns: evaluate `invariant` block. If any invariant fails, raise an `INVARIANT` violation.

The `old` keyword in postconditions captures the value of an expression at method entry, enabling before/after comparisons.

Contract evaluation is **configurable at build time**: contracts can be compiled in or stripped out at different granularity levels. EiffelStudio provides:
- `none` — no contracts checked
- `require` — only preconditions
- `require + ensure` — preconditions and postconditions
- `all` — preconditions, postconditions, and invariants

This allows production builds to check only preconditions (cheap) while development builds check everything.

### Fault/Error Representation

Contract violations are represented as **exceptions** in Eiffel's exception hierarchy:

- `PRECONDITION` violation — a client bug: the caller violated the routine's contract.
- `POSTCONDITION` violation — a supplier bug: the routine failed to deliver its promise.
- `INVARIANT` violation — a supplier bug: the class's internal consistency was broken.

Each violation includes:
- The **label** (the named assertion tag, e.g., `positive_amount`)
- The **class** and **routine** where the violation occurred
- The **expression** that evaluated to false

The exception model follows Meyer's **Disciplined Exception Handling** principle: exceptions represent contract violations, not expected error conditions. Contract violations always indicate bugs, not business-logic rejections.

### Compile-time to Runtime Fault Correspondence

Eiffel's type system catches type mismatches, undefined features (methods), and VOID (null) safety violations at compile time (Eiffel has void safety since Eiffel 2005).

Contract violations are exclusively runtime phenomena — the compiler does not attempt to prove contracts. The compile-time / runtime fault boundary is clear:

| Compile-time | Runtime |
|---|---|
| Type mismatches | Precondition violations |
| Undefined features | Postcondition violations |
| Void safety violations | Invariant violations |
| Syntax errors | Arithmetic overflow/underflow |

There is no static analysis of contract satisfiability in standard Eiffel (though research tools like AutoProof have explored this).

### Entity/Activation Versioning

Eiffel objects are mutable. The class invariant mechanism provides a consistency boundary: the invariant must hold between method calls but may be temporarily violated during method execution.

There is no built-in versioning mechanism. Object persistence is supported through the `STORABLE` class, but without schema migration or versioning.

### Inspect/Preview Architecture

Eiffel has no built-in preview or dry-run mechanism. Contracts are evaluated at method entry/exit, not as a preview before execution.

EiffelStudio's debugger can evaluate assertions in a stopped state, and AutoTest (the automatic testing framework) systematically probes objects for contract violations.

The `require` block itself serves as a documentation-level preview: "these are the conditions under which this operation is valid." But there is no programmatic API to ask "would this call succeed?" without actually making the call.

### Result Type Design

Eiffel uses a standard return-value model. Functions return values; procedures (void methods) return nothing. Contract violations are propagated as exceptions through the retry mechanism:

```eiffel
my_routine is
    do
        -- attempt operation
    rescue
        -- handle failure
        retry  -- re-execute the do block
    end
```

There is no result-or-error wrapper type. Success is the return value; failure is an exception.

### Constraint Evaluation Model

Eiffel's constraint evaluation model is **assertion-based with blame assignment**:

- **Precondition violation** = blame the **client** (caller)
- **Postcondition violation** = blame the **supplier** (callee)
- **Invariant violation** = blame the **supplier** (class implementation)

This blame assignment is a unique design feature. It answers not just "what constraint was violated?" but "who is responsible?"

**Inheritance and contracts** follow strict rules:
- Preconditions can only be **weakened** in subclasses (`require else` — the subclass precondition is OR'd with the parent's)
- Postconditions can only be **strengthened** in subclasses (`ensure then` — the subclass postcondition is AND'd with the parent's)
- Invariants are always **strengthened** (inherited invariants are AND'd)

This ensures that Liskov Substitution Principle (LSP) is maintained through contracts: if code works with a parent class, it will work with any subclass.

The default contract is `require True ensure True` — the weakest possible contract that permits all inputs and promises nothing.

---

## Pkl (Apple)

Source: Pkl GitHub `apple/pkl`; Pkl Language Reference https://pkl-lang.org/main/current/language-reference/; `pkl-core/src/main/java/org/pkl/core/EvaluatorBuilder.java`; Pkl Blog "Meet Pkl" https://pkl-lang.org/blog/introducing-pkl.html

### Runtime Object Architecture

Pkl's runtime is built around **typed objects with prototypical inheritance**:

- **`Module`** — The top-level unit. A Pkl file is a module. Modules can amend (inherit from and override) other modules.
- **`Object`** — The primary value type. Objects have typed properties, elements (indexed values), and entries (keyed values).
- **`Typed`** vs **`Dynamic`** objects — Typed objects have a fixed schema enforced by their class; Dynamic objects accept any property.

Pkl uses a **late-binding, spreadsheet-like** evaluation model: properties can reference other properties, and changes cascade. If property `a` depends on property `b`, modifying `b` in an amendment automatically recomputes `a`.

```pkl
pigeon {
  name = "Common Pigeon"
  lifespan = 8
  greeting = "Hi, I'm \(name) and I live for \(lifespan) years!"
}

myPigeon = (pigeon) {
  name = "Carrier Pigeon"
  // greeting is automatically recomputed: "Hi, I'm Carrier Pigeon and I live for 8 years!"
}
```

### Evaluator Design

Pkl's evaluator supports both lazy and eager evaluation:

- **Lazy data types**: `Listing` (ordered collection) and `Mapping` (key-value collection) — elements are evaluated on demand.
- **Eager data types**: `List` and `Map` — elements are evaluated immediately on construction.
- **Property evaluation**: properties are evaluated lazily by default. When a property is first accessed, its expression is evaluated. Subsequent accesses use the cached result.

The `EvaluatorBuilder` configures the evaluation environment:

```java
EvaluatorBuilder builder = EvaluatorBuilder.preconfigured()
    .setSecurityManager(securityManager)
    .setTimeout(duration)
    .setModuleCacheDir(cacheDir)
    .addAllowedModule("pkl:")
    .addAllowedResource("file:");
```

Configuration includes:
- **Security manager** — restricts which modules and resources can be accessed
- **Timeout** — limits evaluation time
- **Module cache** — caches resolved modules
- **Stack frame transformer** — customizes error stack traces
- **Trace mode** — enables evaluation tracing

### Fault/Error Representation

Pkl has a **fatal error model**: errors are not recoverable.

- The `throw` expression produces a fatal error with a message. There is no `try/catch` mechanism.
- Type constraint violations are fatal: if a value does not match its type constraint, evaluation halts.
- Property type mismatches are fatal.

```pkl
age: Int(this >= 0) = -5  // FATAL: type constraint violated
```

Pkl error output includes:
- The source location (file, line, column)
- The constraint expression that failed
- The actual value that violated the constraint
- A stack trace showing the evaluation path

**Power assertions** (configurable via `EvaluatorBuilder.powerAssertionsEnabled`) provide enhanced error output that shows intermediate values in the failing expression.

### Compile-time to Runtime Fault Correspondence

Pkl performs type checking at **runtime**, not compile time. Type annotations and constraints are validated when values are materialized:

```pkl
age: Int(this >= 0)  // Type annotation + constraint
```

The `Int(this >= 0)` syntax defines a type annotation with a constraint. The `Int` part is the type; `(this >= 0)` is a boolean constraint expression. Both are checked at evaluation time when the property is accessed.

There is no separate compile-time phase that validates constraints. Pkl's parser checks syntax; all semantic validation happens during evaluation. This means there is no compile-time / runtime fault distinction — all faults are runtime faults.

### Entity/Activation Versioning

Pkl's **module amending** mechanism provides a form of entity versioning:

```pkl
amends "base-config.pkl"

// Override specific properties
database {
  port = 5433  // changed from base
}
```

An amended module inherits all properties from the base and can override specific ones. This is prototypical inheritance — the amendment is a new version of the configuration that differs from the base in specified ways.

Pkl caches modules by URI, and the `moduleCacheDir` setting controls persistence of resolved modules.

### Inspect/Preview Architecture

Pkl's evaluation IS preview — evaluating a Pkl file produces a fully resolved configuration value. Since Pkl files are configuration (not side-effecting programs), running the evaluator shows what the configuration will be.

The `EvaluatorBuilder` supports **trace mode** for detailed evaluation inspection: when enabled, the evaluator logs property accesses, module loads, and evaluation steps.

Pkl also supports multiple output formats: JSON, YAML, XML, property files. The evaluator can render the same configuration in different formats for inspection.

### Result Type Design

Pkl evaluation produces a structured value tree. The Java API returns `PModule` (a module value) from `Evaluator.evaluate(ModuleSource)`:

```java
PModule result = evaluator.evaluate(ModuleSource.text("x = 1 + 2"));
// result.get("x") returns PInt(3)
```

Errors are thrown as Java exceptions — there is no result-or-error type. Success returns the value; failure throws.

Pkl property modifiers control visibility:
- `const` — property cannot be overridden in amendments (compile-time enforcement)
- `fixed` — property value is frozen and cannot change (similar to const but for values)
- `hidden` — property is omitted from output
- `local` — property is private to the module

### Constraint Evaluation Model

Pkl uses **type constraints** as its primary constraint mechanism:

```pkl
port: UInt16           // type constraint: unsigned 16-bit integer
name: String(length >= 3)  // type + value constraint
email: String(matches(Regex("^[^@]+@[^@]+$")))  // regex constraint
```

The parenthesized expression after a type is a boolean constraint where `this` refers to the value being constrained. The constraint is evaluated when the property value is materialized.

Constraints are **all-or-nothing**: a constraint violation is a fatal error that halts evaluation. There is no mechanism to collect multiple violations or to report partial results.

Pkl also supports `check` blocks at the module level and in classes:

```pkl
class Server {
  host: String
  port: UInt16
  
  // Module-level constraint spanning multiple properties
  hidden check {
    port != 0
    host.length > 0
  }
}
```

`check` blocks are evaluated after all properties are resolved. Individual assertions within the block are evaluated independently — all failing assertions are reported, not just the first.

---

## Drools (Red Hat)

Source: Drools Documentation https://docs.drools.org/latest/drools-docs/drools/language-reference/; Drools GitHub `apache/incubator-kie-drools`; `drools-core/src/main/java/org/drools/core/common/`; KIE (Knowledge Is Everything) platform documentation

### Runtime Object Architecture

Drools' runtime is organized around:

- **`KieBase`** — The compiled rule knowledge base. Immutable after building. Contains all parsed and compiled rules, types, queries, and functions.
- **`KieSession`** — A runtime execution context with working memory. Mutable — facts are inserted, modified, and retracted. Sessions are created from a KieBase.
- **`Rule Unit`** — (Drools 8+) An encapsulation of rules + data sources. Data sources come in three kinds:
  - `DataStore<T>` — mutable collection (insert/update/delete)
  - `DataStream<T>` — append-only event stream
  - `SingletonStore<T>` — a single mutable value

Working memory holds **facts** — Java objects that rules pattern-match against. The Rete/PHREAK algorithm indexes facts for efficient pattern matching.

```java
KieBase kbase = KieServices.Factory.get()
    .newKieBuilder(kfs).getKieModule().getKieBase();
KieSession session = kbase.newKieSession();
session.insert(new Person("Alice", 30));
session.fireAllRules();
```

### Evaluator Design

Drools uses the **PHREAK algorithm** (an evolution of Rete) for rule evaluation:

1. **Pattern matching**: Facts in working memory are matched against rule conditions using an incremental network. When a fact is inserted, modified, or retracted, only the affected portions of the rule network are re-evaluated.
2. **Conflict resolution**: When multiple rules match, the agenda determines firing order based on rule attributes (`salience`, `activation-group`, etc.).
3. **Rule firing**: Matching rules execute their `then` (consequence) block, which may insert, modify, or retract facts, triggering further matches.

Two execution modes:
- **`fireAllRules()`** — Passive mode. Evaluates all currently matched rules and fires them. Returns when no more rules match.
- **`fireUntilHalt()`** — Active mode. Continuously evaluates and fires rules, blocking until `halt()` is called. Used for event processing.

**Rule attributes** control evaluation behavior:
- `salience` — firing priority (higher fires first)
- `no-loop` — prevents a rule from re-firing due to its own modifications
- `lock-on-active` — prevents a rule from firing while its ruleflow group is active
- `activation-group` — only the first matching rule in a group fires

### Fault/Error Representation

Drools error handling:

- **Compile-time errors** use a standardized format: error code, line/column, description. Errors are collected in `KieBuilder.getResults()`.
- **Runtime errors** in rule consequences are Java exceptions. If a rule's `then` block throws an exception, it propagates to the `fireAllRules()` caller.
- **Pattern match failures** are not errors — they simply mean the rule does not fire.

There is no built-in constraint violation type. Business-rule rejections are modeled by:
1. Inserting a violation fact: `insert(new Violation("age must be positive"))`.
2. Using `insertLogical()` for truth-maintenance: the violation fact is automatically retracted if the conditions that created it are no longer true.

`insertLogical()` is a key mechanism: it creates facts that are **truth-maintained** — they exist only as long as the rule's conditions remain satisfied. If a fact changes and the rule's conditions no longer match, the logically inserted facts are automatically retracted.

### Compile-time to Runtime Fault Correspondence

Drools has a distinct compile phase (`KieBuilder.buildAll()`) that detects:
- Syntax errors in DRL (Drools Rule Language) files
- Type resolution errors (unknown classes in patterns)
- Semantic errors (conflicting rule definitions)

Runtime errors are:
- Java exceptions from rule consequence execution
- Working memory consistency issues (rare)
- Resource exhaustion (infinite rule loops when `no-loop` is not set)

There is no formal mapping between compile-time and runtime error codes. They use separate error reporting mechanisms.

### Entity/Activation Versioning

Drools supports **incremental rule base updates**: rules can be added to or removed from a `KieBase` while sessions are active. The PHREAK network incrementally re-evaluates affected patterns.

Fact versioning is handled via the `modify` / `update` mechanism:

```drl
rule "Promote to senior"
when
    $p: Person(age > 60, role != "senior")
then
    modify($p) { setRole("senior") }
end
```

`modify()` notifies the rule engine that a fact has changed, triggering re-evaluation of all rules that pattern-match against that fact type. This is analogous to entity mutation notification.

Drools also supports **type declarations with metadata** for event processing:

```drl
declare SensorReading
    @role(event)
    @timestamp(readingTime)
    @duration(processingTime)
    @expires(1h)
end
```

Events with `@expires` are automatically retracted from working memory after the specified duration, providing temporal entity lifecycle management.

### Inspect/Preview Architecture

Drools provides several inspection mechanisms:

- **Queries**: Named patterns that can be evaluated on demand without firing rules:

```drl
query "people over 60"
    $p: Person(age > 60)
end
```

Queries return all matching facts without side effects.

- **`getObjects(ObjectFilter)`**: Retrieves all facts in working memory matching a filter.
- **Agenda inspection**: `session.getAgenda().getActivationGroup("group")` reveals which rules are activated.
- **Event listeners**: `AgendaEventListener` and `RuleRuntimeEventListener` provide callbacks for rule matching, firing, and fact operations.

There is no built-in "what-if" mechanism to preview what would happen if a fact were inserted without actually inserting it.

### Result Type Design

Drools does not have a single result type. Results are extracted from working memory:

```java
// Query results
QueryResults results = session.getQueryResults("people over 60");
for (QueryResultsRow row : results) {
    Person p = (Person) row.get("$p");
}

// Direct fact retrieval
Collection<? extends Object> facts = session.getObjects(
    new ClassObjectFilter(Violation.class));
```

The result model is working-memory-centric: you query the working memory for the facts that rules produced or modified. There is no wrapper type distinguishing "success" from "failure."

### Constraint Evaluation Model

Drools rules are naturally a constraint evaluation system:

- **Positive patterns**: `Person(age > 18)` — matches facts satisfying the constraint
- **Negative patterns**: `not Person(age < 18)` — matches when no fact satisfies the constraint
- **Existential patterns**: `exists Person(role == "admin")` — matches if at least one fact satisfies
- **Universal patterns**: `forall($p: Person() Person(this == $p, age > 18))` — matches if all facts satisfy
- **Accumulate**: `accumulate(Person($a: age), $avg: average($a))` — aggregate computation across matching facts
- **OOPath**: `/$p: Person/addresses[city == "NYC"]` — reactive path-based navigation

All constraints are evaluated against the current working memory state. Constraint evaluation is incremental — when a fact changes, only the affected constraints are re-evaluated.

The `from` clause enables evaluation against external data sources:

```drl
rule "Check credit"
when
    $customer: Customer()
    $score: CreditScore(value < 500) from creditService.getScore($customer)
then
    // ...
end
```

---

## SPARK Ada — Formal Verification of Runtime Properties

Source: SPARK User's Guide https://docs.adacore.com/spark2014-docs/html/ug/; AdaCore GNATprove documentation; SPARK 2014 Language Reference; John Barnes, "SPARK: The Proven Approach to High Integrity Software" (2012)

### Runtime Object Architecture

SPARK is not a standalone runtime — it is a formally verifiable subset of Ada. SPARK programs run on the standard Ada runtime. The SPARK-specific architecture is the **formal verification toolchain** (GNATprove) that statically proves properties about the program:

- **`SPARK_Mode`** — A pragma that marks code regions as subject to SPARK restrictions and verification.
- **GNATprove** — The verification tool that performs flow analysis and formal proof.
- **Proof obligations** — Mathematical propositions generated from the code that, if proved, guarantee the absence of specific runtime errors.

SPARK's goal is to **eliminate runtime checks by proving them at analysis time**. The Ada language defines many implicit runtime checks (range checks, overflow checks, index checks). SPARK/GNATprove attempts to prove these checks can never fail, eliminating the need for runtime checking.

### Evaluator Design

SPARK's "evaluator" is not a runtime evaluator but a **static verifier** that uses SMT solvers (primarily cvc5) to discharge proof obligations:

1. GNATprove generates **verification conditions** (VCs) from the source code — one per potential runtime check, assertion, or contract.
2. Each VC is sent to one or more SMT solvers.
3. If the solver proves the VC, the corresponding runtime check is guaranteed to never fail.

The verification runs in configurable modes:
- `--mode=check` — SPARK language subset checking only
- `--mode=flow` — Data flow analysis (initialization, dependencies)
- `--mode=prove` — Full formal proof of runtime checks and contracts
- `--mode=all` — Everything

### Fault/Error Representation

GNATprove categorizes verification results by **check category**:

| Category | What it verifies |
|---|---|
| Run-time Checks | Absence of runtime errors (AoRTE): overflow, range, division by zero, index |
| Assertions | User-written `pragma Assert` statements |
| Functional Contracts | Preconditions, postconditions, type invariants |
| Data Dependencies | Specified vs. actual data flow |
| Flow Dependencies | Information flow correctness |
| Initialization | All variables are initialized before use |
| LSP Verification | Liskov Substitution Principle for tagged types |
| Termination | Loop termination and subprogram termination |

Each check has a **severity** assigned by GNATprove:
- `low` — the check is likely provable with more effort
- `medium` — the check may or may not be valid
- `high` — the check is likely a real bug (supported by a counterexample)

GNATprove can generate **counterexamples** for unproved checks: concrete input values that would cause the check to fail.

### Compile-time to Runtime Fault Correspondence

SPARK has the **most explicit compile-time to runtime fault correspondence** of any system surveyed:

Each Ada runtime check (range check, overflow check, division check, index check, etc.) generates a specific proof obligation. The GNATprove summary table shows:

```
Run-time Checks     474    .    .    458 (CVC5 95%, Trivial 5%)    16    .
```

This means: 474 runtime checks exist in the code; 458 were proved by SMT solvers; 16 were justified by manual annotations; 0 remain unproved. Every single potential runtime error is accounted for.

The mapping is structural and exhaustive:
- Every `Integer` arithmetic operation → overflow check proof obligation
- Every array access → index check proof obligation
- Every type conversion → range check proof obligation
- Every division → divide-by-zero proof obligation

When a check is proved, the corresponding runtime check can be **safely removed** from the compiled executable. The proof serves as a compile-time certificate that the runtime fault cannot occur.

The SARIF output format provides machine-readable proof results with source locations, enabling tooling integration.

### Entity/Activation Versioning

SPARK does not have a concept of entity versioning — it operates on static source code. Version tracking is handled by the Ada build system and configuration management tools.

GNATprove does maintain **session files** for proof caching: proof results are cached and reused across runs, with invalidation when source code changes.

### Inspect/Preview Architecture

GNATprove provides extensive inspection capabilities:

- **Analysis Report Panel** — Interactive view of all proof results, filterable by file, subprogram, and severity.
- **`gnatprove.out` summary file** — Textual summary with per-category statistics and prover usage percentages.
- **SARIF output** — Machine-readable results in the Static Analysis Results Interchange Format.
- **Counterexamples** — When a check cannot be proved, GNATprove generates concrete counterexample values and displays them inline in the source editor.
- **`--report=provers`** — Shows which prover discharged each obligation and whether the result came from cache.

The counterexample mechanism is particularly notable: GNATprove uses cvc5 to generate counterexamples, then validates them by checking whether they correspond to a feasible execution path.

### Result Type Design

GNATprove's result is a structured verification report:

- Per-check: proved | justified | unproved, with severity and prover information
- Per-subprogram: summary of all checks in the subprogram
- Per-file: summary of all checks in the file
- Per-project: summary table with category breakdowns

Results are available in:
- Text format (`gnatprove.out`)
- SARIF format (`gnatprove.sarif`)
- IDE integration (GNAT Studio annotations)

There is no single "result type" — the result is a report over all proof obligations.

### Constraint Evaluation Model

SPARK's constraint model is Ada's contract model, verified statically:

```ada
procedure Deposit (Amount : in Positive; Balance : in out Natural)
  with Pre  => Balance <= Natural'Last - Amount,
       Post => Balance = Balance'Old + Amount;
```

- **Preconditions** (`Pre`) — proved at each call site
- **Postconditions** (`Post`) — proved at each return point
- **Type invariants** — proved at each external boundary
- **Subtype predicates** — proved at each assignment and parameter passing

All constraints are evaluated **statically** by SMT solvers. There is no runtime constraint evaluation in SPARK — the entire point is to prove constraints before execution.

---

## Cross-System Comparison

### Dimension 1: Runtime Object Architecture

| System | Primary runtime unit | State model | Mutability |
|---|---|---|---|
| CEL | `Program` (compiled expression) | Stateless — `Activation` bindings per eval | Immutable programs, fresh activations |
| OPA/Rego | `PreparedEvalQuery` | Store with transactions | Immutable policies, mutable store |
| XState v5 | `Actor` (running machine instance) | Stateful — `Snapshot` with status, context, value | Immutable snapshots, stateful actors |
| Temporal | Workflow instance | Stateful — replayed from event history | Deterministic replay, history-append-only |
| Dhall | `Val` (normal form value) | Stateless — pure evaluation | Fully immutable |
| CUE | `Value` (lattice point) | Stateless — unification produces new values | Fully immutable |
| Eiffel | Object instance | Stateful — mutable OO objects | Mutable, invariant-bounded |
| Pkl | Module/Object instance | Stateful — late-binding, cascading recomputation | Amendments create new versions |
| Drools | `KieSession` (working memory) | Stateful — fact insertion/modification/retraction | Mutable working memory |
| SPARK Ada | Source program (verified statically) | N/A — static analysis, not runtime | N/A |

### Dimension 2: Evaluator Design

| System | Strategy | Key mechanism |
|---|---|---|
| CEL | Tree-walking interpretation | `Interpretable.Eval(Activation)`, short-circuit or exhaustive |
| OPA/Rego | Top-down search with backtracking | Datalog-style unification + rule evaluation |
| XState v5 | State machine transition | Pure `transition(machine, state, event)` → [snapshot, actions] |
| Temporal | Deterministic replay engine | Re-execution with history matching |
| Dhall | Eval-apply environment machine | Call-by-need, total (always terminates) |
| CUE | Lazy lattice unification | Constraint propagation, finalize-on-demand |
| Eiffel | Standard OO dispatch + contract checks | Pre/post/invariant assertion evaluation around methods |
| Pkl | Lazy property evaluation | Late-binding, spreadsheet-like recomputation |
| Drools | PHREAK (incremental Rete) | Forward-chaining pattern matching with agenda |
| SPARK Ada | SMT-based proof | Verification condition generation + solver dispatch |

### Dimension 3: Fault/Error Representation

| System | Error model | Error as value? | Exception-based? |
|---|---|---|---|
| CEL | `types.Err` implementing `ref.Val` | Yes — errors are values in the type algebra | No |
| OPA/Rego | Undefined (empty result) or Go error | Partially — undefined is a value (absence) | No |
| XState v5 | `status: 'error'` on snapshot | Yes — error is a snapshot field | No |
| Temporal | Exception hierarchy | No | Yes |
| Dhall | None (impossible after type check) | N/A | N/A |
| CUE | `Bottom` (⊥) lattice value | Yes — Bottom is a value in the lattice | No |
| Eiffel | Contract violation exceptions | No | Yes |
| Pkl | Fatal errors (no recovery) | No | Yes (fatal) |
| Drools | Java exceptions from consequences | No | Yes |
| SPARK Ada | Proof obligations (pre-runtime) | N/A — verified statically | N/A |

### Dimension 4: Compile-time to Runtime Fault Correspondence

| System | Correspondence strength | Description |
|---|---|---|
| Dhall | **Total** — zero runtime faults | Type checker eliminates all faults; eval is total |
| SPARK Ada | **Structural + exhaustive** | Every runtime check maps to a proof obligation; proved = eliminated |
| CUE | **Unified** — no compile/runtime boundary | Validation and evaluation are the same lattice operation |
| CEL | **Partial** — types vs. values | Type checker catches type faults; runtime handles value faults |
| OPA/Rego | **Partial** — types vs. values | Type checker warns on type issues; runtime handles value issues |
| XState v5 | **Weak** — TypeScript types vs. runtime status | TypeScript catches structural errors; runtime catches value errors |
| Pkl | **None** — all faults are runtime | No compile-time semantic analysis; everything checked at eval time |
| Eiffel | **Separate domains** | Compiler catches types; runtime catches contracts |
| Temporal | **None** — determinism is runtime-only | No static analysis of workflow correctness |
| Drools | **Separate domains** | Builder catches syntax/types; runtime catches consequence errors |

### Dimension 5: Entity/Activation Versioning

| System | Versioning mechanism |
|---|---|
| CEL | None (stateless expressions) |
| OPA/Rego | Store transactions, bundle revisions |
| XState v5 | Snapshot persistence/restoration |
| Temporal | `Patched()` / `DeprecatePatch()`, `ContinueAsNew` |
| Dhall | Semantic hash (content-addressable) |
| CUE | Module system with semver |
| Eiffel | None (standard OO mutation) |
| Pkl | Module amending (prototypical inheritance) |
| Drools | Incremental rule base updates, `@expires` for temporal events |
| SPARK Ada | Proof session caching |

### Dimension 6: Inspect/Preview Architecture

| System | Preview mechanism | Side-effect-free? |
|---|---|---|
| CEL | ExhaustiveEval, EvalObserver, PartialVars | Yes |
| OPA/Rego | Partial evaluation → residual policy | Yes |
| XState v5 | Pure `transition()`, `getNextSnapshot()`, `can()` | Yes |
| Temporal | Queries (read-only handlers) | Yes (queries), no preview for signals |
| Dhall | Evaluation IS preview (pure, total) | Yes (inherently) |
| CUE | Export profiles, `Validate()`, `Subsume()` | Yes |
| Eiffel | None (contracts evaluated at call time) | No |
| Pkl | Evaluation IS preview (configuration output) | Yes |
| Drools | Queries, fact retrieval, agenda inspection | Yes (queries), no what-if |
| SPARK Ada | Counterexamples, SARIF reports, summary tables | Yes (pre-runtime) |

### Dimension 7: Result Type Design

| System | Result shape | Error channel |
|---|---|---|
| CEL | `(ref.Val, *EvalDetails, error)` — three-value | Separate: value-error vs. infra-error vs. unknown |
| OPA/Rego | `ResultSet` (may be empty) | Empty = denial; Go error = infrastructure failure |
| XState v5 | `MachineSnapshot` with status field | Status enum on snapshot: active/done/error |
| Temporal | Return value or exception | Exception hierarchy |
| Dhall | `Val` (always succeeds) | No error channel (total function) |
| CUE | `Value` (may be Bottom) | `Value.Err()` returns error if Bottom |
| Eiffel | Return value or exception | Contract violation exceptions |
| Pkl | Module value or fatal exception | Fatal exceptions |
| Drools | Working memory contents (query results) | Java exceptions |
| SPARK Ada | Verification report (per-obligation) | Unproved checks with severity |

### Dimension 8: Constraint Evaluation Model

| System | Constraint style | Violation collection | Blame model |
|---|---|---|---|
| CEL | Boolean expressions | No (single result) | No |
| OPA/Rego | Conjunctive rules, disjunction across rules | Yes (`deny[msg]` pattern) | No |
| XState v5 | Guards (boolean functions) | No (first match, silent drop) | No |
| Temporal | Imperative if/throw | No (first failure) | No |
| Dhall | Type system only | N/A (type errors pre-eval) | N/A |
| CUE | Lattice unification (all values are constraints) | Yes (Bottom accumulates all conflicts) | No |
| Eiffel | Pre/post/invariant assertions | No (first failure throws) | Yes — client vs. supplier blame |
| Pkl | Type constraints with boolean predicates | Partial (check blocks collect; type constraints are fatal) | No |
| Drools | Pattern matching with positive/negative/existential/universal | Yes (rule network evaluates all matches) | No |
| SPARK Ada | Contracts proved by SMT | Yes (all obligations tracked) | Yes — precondition vs. postcondition |
