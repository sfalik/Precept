# Runtime API Design

This document describes the current public API surface of the `StateMachine.Dsl` runtime — the three static/sealed types that together form the complete usage contract.

## Overview

The runtime is a three-step pipeline:

```
.sm text  ──►  DslWorkflowParser.Parse()  ──►  DslWorkflowModel
DslWorkflowModel  ──►  DslWorkflowCompiler.Compile()  ──►  DslWorkflowEngine
DslWorkflowEngine  ──►  CreateInstance()  ──►  DslWorkflowInstance (mutable over time)
```

All three steps are pure functions. No hidden state is accumulated outside the values returned by each step.

---

## Step 1 — Parsing: `DslWorkflowParser`

```csharp
public static class DslWorkflowParser
{
    public static DslWorkflowModel Parse(string text)
}
```

Parses a `.sm` DSL text string into a `DslWorkflowModel` record tree. Throws `InvalidOperationException` on syntax errors. The returned `DslWorkflowModel` is a passive, immutable parse tree — it carries no behavior and performs no validation beyond what the parser itself enforces.

---

## Step 2 — Compilation: `DslWorkflowCompiler`

```csharp
public static class DslWorkflowCompiler
{
    public static DslWorkflowEngine Compile(DslWorkflowModel model)
}
```

Compiles a `DslWorkflowModel` into an immutable `DslWorkflowEngine`. Compilation performs semantic validation:

- All state names referenced in transitions exist.
- All event names referenced in transitions exist.
- All field names referenced in `set` assignments exist.
- All literal `set` assignments satisfy field rules and top-level rules.

Throws `InvalidOperationException` with a descriptive message if any check fails. Once compilation succeeds, the returned `DslWorkflowEngine` is guaranteed to be internally consistent.

---

## Step 3 — Engine: `DslWorkflowEngine`

The immutable compiled engine. One `DslWorkflowEngine` instance represents one workflow definition and can be shared freely across threads and requests.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Workflow name as declared with `machine`. |
| `States` | `IReadOnlyList<string>` | All declared state names in declaration order. |
| `InitialState` | `string` | The state marked `initial`. |
| `Events` | `IReadOnlyList<DslEvent>` | All declared events with their argument contracts. |
| `Fields` | `IReadOnlyList<DslField>` | All scalar data fields. |
| `CollectionFields` | `IReadOnlyList<DslCollectionField>` | All collection fields (`set<T>`, `queue<T>`, `stack<T>`). |

### `CreateInstance`

```csharp
public DslWorkflowInstance CreateInstance(
    IReadOnlyDictionary<string, object?>? instanceData = null)

public DslWorkflowInstance CreateInstance(
    string initialState,
    IReadOnlyDictionary<string, object?>? instanceData = null)
```

Creates a new workflow instance, optionally pre-seeded with field data and starting in a custom state.

- Uses `InitialState` when no `initialState` argument is provided.
- Merges caller-supplied `instanceData` with declared field defaults. Callers only supply the fields they care about.
- Collection fields must be supplied as any `IEnumerable` (not `string`); items are coerced to the declared `InnerType`.
- Throws `InvalidOperationException` if `initialState` is not a known state, or if supplied `instanceData` violates the field type contract.
- `instance.InstanceData` uses *clean keys*: collection fields appear under their declared field name (not an internal `__collection__` prefix), and their values are `List<object>`.

### `Inspect` (per-event)

```csharp
public DslEventInspectionResult Inspect(
    DslWorkflowInstance instance,
    string eventName,
    IReadOnlyDictionary<string, object?>? eventArguments = null)
```

Non-mutating evaluation of a single event against the current instance. Returns `DslEventInspectionResult`.

**Semantics:**
- Verifies schema compatibility via `CheckCompatibility` before evaluating. On failure, returns `NotDefined`.
- Evaluates the `when` precondition (if present) against instance data alone, before argument validation. If false, returns `NotApplicable`.
- Accepts calls with missing/partial event arguments — this is a discovery API. `RequiredEventArgumentKeys` on the result tells callers what is needed before `Fire`.
- When event arguments are provided, validates them against the event's argument contract. Unknown keys are rejected.
- Evaluates ordered `if`/`else if`/`else` guards; the first matching clause determines the outcome.
- Simulates `set` assignments and collection mutations on a working copy, then evaluates field, top-level, and target-state rules. Rejects if any rule would be violated.
- Does **not** mutate the instance.

**Event argument validation during Inspect:**
Arguments are only validated and event rules are only run when `eventArguments != null`. Calling `Inspect` without arguments is explicitly supported for discover-mode callers.

### `Inspect` (aggregate)

```csharp
public DslInspectionResult Inspect(DslWorkflowInstance instance)
```

Evaluates all events that have at least one transition from the instance's current state, returning a single `DslInspectionResult` with the current state, serialized data, and per-event results.

- Events are ordered by declaration position.
- Each event is evaluated as `Inspect(instance, eventName)` with no event arguments (discovery mode).
- If the instance fails `CheckCompatibility`, returns a result with an empty events list.

Use this as the primary API for rendering a state-machine inspector view.

### `Fire`

```csharp
public DslFireResult Fire(
    DslWorkflowInstance instance,
    string eventName,
    IReadOnlyDictionary<string, object?>? eventArguments = null)
```

Mutating event execution. Returns `DslFireResult`.

**Evaluation stages (in order, with full rollback on any failure):**

1. **Compatibility check** — same as `CheckCompatibility`; returns `NotDefined` if incompatible.
2. **Event argument validation** — validates types against the event's argument contract; returns `Rejected` on unknown keys or wrong types.
3. **Event rules** — evaluated against event-argument-only context (field data does not shadow args); returns `Rejected` on violation.
4. **`when` precondition** — if present and false, returns `NotApplicable`.
5. **Guard evaluation** — evaluates ordered `if`/`else if`/`else` clauses; returns `Rejected` if all guards fail.
6. **`set` assignments** — executed on a working copy of instance data; returns `Rejected` if an expression fails or the result violates a field's declared type.
7. **Collection mutations** — `add`/`remove`/`enqueue`/`dequeue`/`push`/`pop`/`clear` operations on working copies.
8. **Field and top-level rules** — checked against post-`set` working data; returns `Rejected` if violated.
9. **State rules** — checked against the *target* state (only for `transition` outcomes, not `no transition`); returns `Rejected` if violated.

On any rejection, the original instance is unchanged — all stages are fully rolled back.

On success, returns `DslFireResult` with `UpdatedInstance != null`. The `UpdatedInstance` carries the new `CurrentState`, the updated `InstanceData`, and the current `UpdatedAt` timestamp.

`no transition` outcomes execute `set` and collection mutations but do **not** trigger state rules.

### `CheckCompatibility`

```csharp
public DslCompatibilityResult CheckCompatibility(DslWorkflowInstance instance)
```

Validates that an externally loaded or deserialized instance is compatible with this compiled engine:

1. `WorkflowName` matches `Name`.
2. `CurrentState` is a known state.
3. `InstanceData` satisfies the field type contract (no unknown fields with wrong types).
4. All data rules (field rules + top-level rules) and the current state's entry rules pass.

Returns `DslCompatibilityResult(IsCompatible, Reason?)`. If `IsCompatible` is false, `Reason` contains a human-readable explanation.

Use this before using an externally deserialized instance — for example, one loaded from a JSON file or a database — to ensure schema evolution has not created a violating instance.

### `CoerceEventArguments`

```csharp
public IReadOnlyDictionary<string, object?>? CoerceEventArguments(
    string eventName,
    IReadOnlyDictionary<string, object?>? args)
```

Coerces raw argument values to the declared scalar types for the named event.

- Returns `null` if `args` is `null`.
- `System.Text.Json.JsonElement` values are unwrapped to `string`, `double`, `bool`, or `null` before type coercion.
- After unwrapping: `Number` fields receive `Convert.ToDouble`, `Boolean` fields accept `bool` or `"true"`/`"false"` strings, `String` fields receive `.ToString()`.
- Unknown argument keys pass through unchanged.
- Never throws — values that cannot be coerced are returned unchanged.

Call this on arguments obtained from JSON deserialization (e.g. CLI input or HTTP body) before passing them to `Inspect` or `Fire`.

---

## Instance: `DslWorkflowInstance`

```csharp
public sealed record DslWorkflowInstance(
    string WorkflowName,
    string CurrentState,
    string? LastEvent,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object?> InstanceData)
```

A pure value representing one workflow instance at a point in time. Immutable — `Fire` returns a new instance rather than mutating the existing one.

### `InstanceData` format

`InstanceData` uses the clean public key format:

| Field type | Key | Value type |
|------------|-----|------------|
| Scalar field | `FieldName` | `string`, `double`, `bool`, `null`, or `object?` |
| Collection field | `FieldName` | `List<object>` |

There are no `__collection__` prefix keys in instances returned by the engine. The internal `__collection__` prefix is an implementation detail confined to evaluation-time working copies inside the engine.

**Collection field ordering semantics by kind:**

| Kind | Ordering | Peek position |
|------|----------|---------------|
| `set<T>` | Elements sorted ascending | Not applicable |
| `queue<T>` | Insertion order (FIFO) | `list[0]` (front) |
| `stack<T>` | Insertion order (LIFO) | `list[^1]` (top) |

### Serialization

`DslWorkflowInstance` is a plain record with no custom serialization logic. Serialize `InstanceData` directly — all values are `string`, `double`, `bool`, `null`, or `List<object>`. Deserialize by passing the resulting dictionary to `engine.CreateInstance(data)`.

---

## Result Types

### `DslOutcomeKind`

```csharp
public enum DslOutcomeKind
{
    NotDefined,      // Event or state is unknown to the engine
    NotApplicable,   // 'when' precondition was false
    Rejected,        // Guard evaluation or rule check blocked the event
    Accepted,        // Event fired; state changed to TargetState / NewState
    AcceptedInPlace  // 'no transition' outcome; state unchanged, data may change
}
```

### `DslEventInspectionResult`

```csharp
public sealed record DslEventInspectionResult(
    DslOutcomeKind Outcome,
    string CurrentState,
    string EventName,
    string? TargetState,           // null unless Outcome is Accepted or AcceptedInPlace
    IReadOnlyList<string> RequiredEventArgumentKeys,  // non-nullable required args
    IReadOnlyList<string> Reasons) // non-empty when Outcome is Rejected or NotDefined
```

Returned by the per-event `Inspect` overload. `TargetState` is populated for `Accepted` (the transition target) and `AcceptedInPlace` (same as `CurrentState`).

### `DslInspectionResult`

```csharp
public sealed record DslInspectionResult(
    string CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<DslEventInspectionResult> Events)
```

Returned by the aggregate `Inspect(instance)` overload. `InstanceData` is the same clean dictionary as `instance.InstanceData`.

### `DslFireResult`

```csharp
public sealed record DslFireResult(
    DslOutcomeKind Outcome,
    string PreviousState,
    string EventName,
    string? NewState,              // null unless Outcome is Accepted or AcceptedInPlace
    IReadOnlyList<string> Reasons, // non-empty when Outcome is Rejected or NotDefined
    DslWorkflowInstance? UpdatedInstance) // null unless Outcome is Accepted or AcceptedInPlace
```

`UpdatedInstance` is non-null only when the event was accepted. On any rejection, `UpdatedInstance` is `null` and the original instance is unchanged. `NewState` equals `PreviousState` for `AcceptedInPlace`.

### `DslCompatibilityResult`

```csharp
public sealed record DslCompatibilityResult(bool IsCompatible, string? Reason)
```

---

## Concurrency Model

`DslWorkflowEngine` is immutable and thread-safe after construction — share one engine instance across all requests.

`DslWorkflowInstance` is an immutable record — share or store freely. Do not mutate `InstanceData` after construction.

All coordination for concurrent reads and writes to persisted instance storage (files, databases, etc.) is outside the runtime's scope and must be managed by the caller.

---

## Typical Usage Pattern

```csharp
// 1. One-time startup
var model  = DslWorkflowParser.Parse(File.ReadAllText("loan.sm"));
var engine = DslWorkflowCompiler.Compile(model);

// 2. Per-request — create or load instance
var instance = engine.CreateInstance();

// 3. Optional: validate a previously persisted instance
var compat = engine.CheckCompatibility(instance);
if (!compat.IsCompatible) throw new Exception(compat.Reason);

// 4. Discover available events
var snapshot = engine.Inspect(instance);
foreach (var evt in snapshot.Events)
    Console.WriteLine($"{evt.EventName}: {evt.Outcome}  → {evt.TargetState}");

// 5. Coerce args from external input and fire
var rawArgs = new Dictionary<string, object?> { ["Amount"] = jsonElement };
var args    = engine.CoerceEventArguments("Pay", rawArgs);
var result  = engine.Fire(instance, "Pay", args);

if (result.Outcome == DslOutcomeKind.Accepted)
{
    instance = result.UpdatedInstance!;  // advance to new instance
    // persist instance...
}
else
{
    Console.WriteLine($"Rejected: {string.Join(", ", result.Reasons)}");
}
```

---

## Model Types (Parse Tree)

The following types are returned by `DslWorkflowParser.Parse` and consumed by `DslWorkflowCompiler.Compile`. They are not normally needed by callers of the engine directly.

| Type | Description |
|------|-------------|
| `DslWorkflowModel` | Root record — name, states, events, transitions, fields, rules |
| `DslState` | State with optional entry rules |
| `DslEvent` | Event with argument contract and optional event rules |
| `DslEventArg` | One typed argument: name, `DslScalarType`, nullability, optional default |
| `DslField` | One scalar data field: name, `DslScalarType`, nullability, optional default, optional field rules |
| `DslCollectionField` | One collection field: name, `DslCollectionKind`, `DslScalarType` inner type, optional rules |
| `DslRule` | A boolean constraint expression with a human-readable `Reason` string |
| `DslTransition` | One `from … on … [when …]` block: source states, event name, optional `when` predicate, ordered clauses |
| `DslClause` | One branch inside a `from … on` block: optional guard predicate, outcome, `set` assignments, collection mutations |
| `DslStateTransition` | Clause outcome: `transition <State>` |
| `DslRejection` | Clause outcome: `reject "<message>"` |
| `DslNoTransition` | Clause outcome: `no transition` |

### Scalar and Collection Enums

```csharp
public enum DslScalarType  { String, Number, Boolean, Null }
public enum DslCollectionKind { Set, Queue, Stack }
```

### Expression AST

Guard predicates and rule expressions are pre-parsed at compile time into a `DslExpression` AST:

| Node type | Represents |
|-----------|-----------|
| `DslLiteralExpression` | `true`, `false`, `null`, string literal, number literal |
| `DslIdentifierExpression` | bare field/arg name, or dotted `EventName.ArgKey` form |
| `DslUnaryExpression` | `!` operator |
| `DslBinaryExpression` | `&&`, `\|\|`, `==`, `!=`, `<`, `>`, `<=`, `>=`, `+`, `-`, `*`, `/` |
| `DslParenthesizedExpression` | `( … )` grouping |

The AST is evaluated by `DslExpressionRuntimeEvaluator` (internal). Callers never need to evaluate the AST directly.
