# Runtime API Design

This document describes the current public API surface of the `Precept` runtime — the three static/sealed types that together form the complete usage contract.

## Overview

The runtime is a three-step pipeline:

```
.precept text  ──►  PreceptParser.Parse()  ──►  PreceptDefinition
PreceptDefinition  ──►  PreceptCompiler.Compile()  ──►  PreceptEngine
PreceptEngine  ──►  CreateInstance()  ──►  PreceptInstance (mutable over time)
```

All three steps are pure functions. No hidden state is accumulated outside the values returned by each step.

---

## Step 1 — Parsing: `PreceptParser`

```csharp
public static class PreceptParser
{
    public static PreceptDefinition Parse(string text)
    public static (PreceptDefinition? Model, IReadOnlyList<ParseDiagnostic> Diagnostics) ParseWithDiagnostics(string text)
}
```

Parses a `.precept` DSL text string into a `PreceptDefinition` record tree. Throws `InvalidOperationException` on syntax errors. The returned `PreceptDefinition` is a passive, immutable parse tree — it carries no behavior and performs no validation beyond what the parser itself enforces.

`ParseWithDiagnostics` is the non-throwing variant — it returns a `PreceptDefinition` (or `null` on hard failure) alongside a list of `ParseDiagnostic` records. Use this in tooling contexts where errors must be reported without exceptions.

---

## Step 2 — Compilation: `PreceptCompiler`

```csharp
public static class PreceptCompiler
{
    public static PreceptEngine Compile(PreceptDefinition model)
}
```

Compiles a `PreceptDefinition` into an immutable `PreceptEngine`. Compilation performs semantic validation:

- All state names referenced in transitions exist.
- All event names referenced in transitions exist.
- All field names referenced in `set` assignments exist.
- All literal `set` assignments satisfy field rules and top-level rules.

Throws `InvalidOperationException` with a descriptive message if any check fails. Once compilation succeeds, the returned `PreceptEngine` is guaranteed to be internally consistent.

---

## Step 3 — Engine: `PreceptEngine`

The immutable compiled engine. One `PreceptEngine` instance represents one workflow definition and can be shared freely across threads and requests.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Workflow name as declared with `precept`. |
| `States` | `IReadOnlyList<string>` | All declared state names in declaration order. Empty for stateless precepts. |
| `InitialState` | `string?` | The state marked `initial`. `null` for stateless precepts. |
| `IsStateless` | `bool` | `true` when the precept has no state declarations (`States.Count == 0`). |
| `Events` | `IReadOnlyList<PreceptEvent>` | All declared events with their argument contracts. |
| `Fields` | `IReadOnlyList<PreceptField>` | All scalar data fields. |
| `CollectionFields` | `IReadOnlyList<PreceptCollectionField>` | All collection fields (`set<T>`, `queue<T>`, `stack<T>`). |

### `CreateInstance`

```csharp
public PreceptInstance CreateInstance(
    IReadOnlyDictionary<string, object?>? instanceData = null)

public PreceptInstance CreateInstance(
    string initialState,
    IReadOnlyDictionary<string, object?>? instanceData = null)
```

Creates a new workflow instance, optionally pre-seeded with field data and starting in a custom state.

- `CreateInstance(data?)` — works for both stateful and stateless precepts. For stateful precepts, uses `InitialState`. For stateless precepts, creates an instance with `CurrentState = null`.
- `CreateInstance(state, data?)` — throws `ArgumentException` for stateless precepts (`Precept '{Name}' is stateless. Use CreateInstance(instanceData) — the state argument is not valid.`).
- Merges caller-supplied `instanceData` with declared field defaults. Callers only supply the fields they care about.
- Collection fields must be supplied as any `IEnumerable` (not `string`); items are coerced to the declared `InnerType`.
- Throws `InvalidOperationException` if `initialState` is not a known state, or if supplied `instanceData` violates the field type contract.
- `instance.InstanceData` uses *clean keys*: collection fields appear under their declared field name (not an internal `__collection__` prefix), and their values are `List<object>`.
- `instance.CurrentState` is `string?` — `null` for stateless instances.

### `Inspect` (per-event)

```csharp
public EventInspectionResult Inspect(
    PreceptInstance instance,
    string eventName,
    IReadOnlyDictionary<string, object?>? eventArguments = null)
```

Non-mutating evaluation of a single event. Returns `EventInspectionResult`.

**Semantics:**
- Verifies schema compatibility via `CheckCompatibility` before evaluating. On failure, returns `Undefined`.
- **Stateless precepts:** Returns `Undefined` immediately after compatibility check. Events have no transition surface on stateless precepts.
- Evaluates the `when` precondition (if present) against instance data alone, before argument validation. If false, returns `Unmatched`.
- Accepts calls with missing/partial event arguments — this is a discovery API. `RequiredEventArgumentKeys` on the result tells callers what is needed before `Fire`.
- When event arguments are provided, validates them against the event's argument contract. Unknown keys are rejected.
- Evaluates ordered `if`/`else if`/`else` guards; the first matching clause determines the outcome.
- Simulates `set` assignments and collection mutations on a working copy, then evaluates field rules, top-level rules, and target-state ensures. Returns `ConstraintFailure` if any post-mutation validation would be violated.
- Does **not** mutate the instance.

**Event argument validation during Inspect:**
Arguments are only validated and event ensures are only run when `eventArguments != null`. Calling `Inspect` without arguments is explicitly supported for discover-mode callers.

### `Inspect` (aggregate)

```csharp
public InspectionResult Inspect(PreceptInstance instance)
```

Evaluates all events that have at least one transition from the instance's current state, returning a single `InspectionResult` with the current state, serialized data, per-event results, and editable field metadata.

- Events are ordered by declaration position.
- Each event is evaluated as `Inspect(instance, eventName)` with no event arguments (discovery mode).
- **Stateless precepts:** All events return `Undefined` outcome (no transition surface). `EditableFields` is populated from the effective root-level editable set, including guarded root-level edit blocks whose guards currently pass.
- `EditableFields` is `null` when the engine has no edit declarations at all, an empty list when edit declarations exist but none are currently effective, or the union of all matching unconditional edit declarations plus any guarded edit declarations whose guards currently pass.
- If the instance fails `CheckCompatibility`, returns a result with an empty events list.

Use this as the primary API for rendering a state-machine inspector view.

### `Inspect` (hypothetical patch)

```csharp
public InspectionResult Inspect(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
```

Applies a hypothetical patch to a working copy of instance data, runs the full rule evaluation pipeline (editability check, type check, rule/ensure evaluation), and returns an `InspectionResult` with violations reflected in `EditableFields`. **No commit occurs** — the instance is unchanged. Used for per-keystroke rule checking in the preview UI.

### `Update`

```csharp
public UpdateResult Update(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
```

Atomically updates editable fields. For **stateful** precepts, only fields granted by the effective edit set for the current state are mutable: unconditional `in <State> edit` declarations plus any guarded edit blocks whose guards pass. For **stateless** precepts, only fields granted by the effective root-level edit set are mutable: unconditional root-level `edit` declarations plus guarded root-level `edit ... when ...` / `edit all when ...` blocks whose guards pass (`CurrentState` is `null` throughout). Evaluation sequence: editability check → type check → atomic mutation on working copy → rule/state-ensure evaluation → commit or rollback. Returns `UpdateResult`.

### `Fire`

```csharp
public FireResult Fire(
    PreceptInstance instance,
    string eventName,
    IReadOnlyDictionary<string, object?>? eventArguments = null)
```

Mutating event execution. Returns `FireResult`.

**Stateless precepts:** Returns `Undefined` immediately after compatibility check. Events have no transition surface.

**Evaluation stages (in order, with full rollback on any failure):**

1. **Compatibility check** — same as `CheckCompatibility`; returns `Undefined` if incompatible.
2. **Event argument validation** — validates types against the event's argument contract; returns `Rejected` on unknown keys or wrong types.
3. **Event ensures** — evaluated against event-argument-only context (field data does not shadow args); returns `Rejected` on violation.
4. **`when` precondition** — if present and false, returns `Unmatched`.
5. **Guard evaluation** — evaluates ordered `if`/`else if`/`else` clauses; returns `Rejected` if all guards fail.
6. **`set` assignments** — executed on a working copy of instance data; returns `Rejected` if an expression fails or the result violates a field's declared type.
7. **Collection mutations** — `add`/`remove`/`enqueue`/`dequeue`/`push`/`pop`/`clear` operations on working copies.
8. **Derived field recomputation** — re-evaluates all computed fields in dependency order against post-mutation data. Computed values are current before validation.
9. **Field and top-level rules** — checked against post-mutation working data (including recomputed derived fields); returns `ConstraintFailure` if violated.
10. **State ensures** — checked against the resulting state. Cross-state `transition` outcomes evaluate `from <Source>`, `to <Target>`, and `in <Target>` ensures; `no transition` outcomes evaluate only `in <CurrentState>` ensures. Returns `ConstraintFailure` if violated.

**`when` guard evaluation on declarations:**
- **Event ensures (stage 3):** If an event ensure has a `when` guard, the guard is evaluated against event args before the ensure body. If the guard is false, that ensure is skipped. Guards on event ensures are arg-scoped only.
- **Rules and state ensures (stage 9–10):** If a rule or state ensure has a `when` guard, the guard is evaluated against post-mutation field data before the declaration body. If the guard is false, that declaration is skipped. Collect-all semantics are preserved — guard-skipped declarations don’t short-circuit other declarations.
- **Edit blocks (`Update`/`Inspect`):** If an edit declaration has a `when` guard, the guard is evaluated against current instance data at each `Update`/`Inspect` call. Fail-closed: guard evaluation error → field not granted editability.

On any failure outcome, the original instance is unchanged — all stages are fully rolled back.

On success, returns `FireResult` with `UpdatedInstance != null`. The `UpdatedInstance` carries the new `CurrentState`, the updated `InstanceData`, and the current `UpdatedAt` timestamp.

`no transition` outcomes execute `set` and collection mutations, skip entry/exit actions, and still evaluate `in <CurrentState>` ensures before commit.

### `CheckCompatibility`

```csharp
public PreceptCompatibilityResult CheckCompatibility(PreceptInstance instance)
```

Validates that an externally loaded or deserialized instance is compatible with this compiled engine:

1. `WorkflowName` matches `Name`.
2. For stateful precepts: `CurrentState` is a known declared state. For stateless precepts: `CurrentState` must be `null`.
3. `InstanceData` satisfies the field type contract (no unknown fields with wrong types).
4. All data rules (field rules + top-level rules) and the current state's `in` ensures pass.

Returns `PreceptCompatibilityResult(IsCompatible, Reason?)`. If `IsCompatible` is false, `Reason` contains a human-readable explanation.

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

## Instance: `PreceptInstance`

```csharp
public sealed record PreceptInstance(
    string WorkflowName,
    string? CurrentState,
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

`PreceptInstance` is a plain record with no custom serialization logic. Serialize `InstanceData` directly — all values are `string`, `double`, `bool`, `null`, or `List<object>`. Deserialize by passing the resulting dictionary to `engine.CreateInstance(data)`.

---

## Result Types

### `TransitionOutcome`

```csharp
public enum TransitionOutcome
{
    // Failure
    Undefined,           // Event or state is unknown to the engine
    Unmatched,           // All when-guards failed, no row matched
    Rejected,            // Explicit reject outcome in transition row
    ConstraintFailure,   // Post-mutation rule or state ensure violation

    // Success
    Transition,          // Event fired; state changed to TargetState / NewState
    NoTransition         // 'no transition' outcome; state unchanged, data may change
}
```

### `EventInspectionResult`

```csharp
public sealed record EventInspectionResult(
    TransitionOutcome Outcome,
    string? CurrentState,
    string EventName,
    string? TargetState,           // null unless Outcome is Transition or NoTransition
    IReadOnlyList<string> RequiredEventArgumentKeys,
    IReadOnlyList<ConstraintViolation> Violations)
{
    public bool IsSuccess => Outcome is TransitionOutcome.Transition
                          or TransitionOutcome.NoTransition;
}
```

Returned by the per-event `Inspect` overload. `TargetState` is populated for `Transition` (the transition target) and `NoTransition` (same as `CurrentState`). `CurrentState` is `null` for stateless instances.

### `InspectionResult`

```csharp
public sealed record InspectionResult(
    string? CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<EventInspectionResult> Events,
    IReadOnlyList<PreceptEditableFieldInfo>? EditableFields = null)
```

Returned by the aggregate `Inspect(instance)` overload. `InstanceData` is the same clean dictionary as `instance.InstanceData`. `CurrentState` is `null` for stateless instances. `EditableFields` is `null` when the engine has no edit declarations, an empty list when edit declarations exist but none are currently effective, or the effective editable field set for the current state/root context after guarded edit blocks are evaluated against the current data.

### `PreceptEditableFieldInfo`

```csharp
public sealed record PreceptEditableFieldInfo(
    string FieldName,
    string FieldType,        // "string", "number", "set<string>", etc.
    bool IsNullable,
    object? CurrentValue,
    string? Violation = null)
```

Metadata for one editable field. `Violation` is a human-readable message, populated only by `Inspect(instance, patch)` when a hypothetical patch fails a constraint.

### `UpdateResult`

```csharp
public sealed record UpdateResult(
    UpdateOutcome Outcome,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is UpdateOutcome.Update;
}
```

Returned by `Update`. `UpdatedInstance` is non-null only when `Outcome` is `Update`.

### `UpdateOutcome`

```csharp
public enum UpdateOutcome
{
    // Success
    Update,              // All fields modified, all constraints passed

    // Failure
    ConstraintFailure,   // Constraints violated — violations collected
    UneditableField,     // One or more fields not editable in current state
    InvalidInput         // Type mismatch, unknown field, patch conflict, or empty patch
}
```

### `FireResult`

```csharp
public sealed record FireResult(
    TransitionOutcome Outcome,
    string? PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is TransitionOutcome.Transition
                          or TransitionOutcome.NoTransition;
}
```

`UpdatedInstance` is non-null only when the event was accepted. On any rejection, `UpdatedInstance` is `null` and the original instance is unchanged. `NewState` equals `PreviousState` for `NoTransition`.

### `PreceptCompatibilityResult`

```csharp
public sealed record PreceptCompatibilityResult(bool IsCompatible, string? Reason)
```

---

## Concurrency Model

`PreceptEngine` is immutable and thread-safe after construction — share one engine instance across all requests.

`PreceptInstance` is an immutable record — share or store freely. Do not mutate `InstanceData` after construction.

All coordination for concurrent reads and writes to persisted instance storage (files, databases, etc.) is outside the runtime's scope and must be managed by the caller.

---

## Typical Usage Pattern

```csharp
// 1. One-time startup
var model  = PreceptParser.Parse(File.ReadAllText("loan.precept"));
var engine = PreceptCompiler.Compile(model);

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

if (result.IsSuccess)
{
    instance = result.UpdatedInstance!;  // advance to new instance
    // persist instance...
}
else
{
    foreach (var v in result.Violations)
        Console.WriteLine(v.Message);
}

// 6. Direct field editing (for fields granted by state-scoped `in <State> edit` or stateless root-level `edit`)
var editResult = engine.Update(instance, patch => patch
    .Set("Notes", "Customer called back")
    .Set("Priority", 1.0));

if (editResult.IsSuccess)
    instance = editResult.UpdatedInstance!;
```

---

## Model Types (Parse Tree)

The following types are returned by `PreceptParser.Parse` and consumed by `PreceptCompiler.Compile`. They are not normally needed by callers of the engine directly.

| Type | Description |
|------|-------------|
| `PreceptDefinition` | Root record — name, states, events, transition rows, fields, rules, ensures |
| `PreceptState` | State declaration |
| `PreceptEvent` | Event with argument contract |
| `PreceptEventArg` | One typed argument: name, `PreceptScalarType`, nullability, optional default |
| `PreceptField` | One scalar data field: name, `PreceptScalarType`, nullability, optional default, optional derived expression (`IsComputed`, `DerivedExpression`, `DerivedExpressionText`) |
| `PreceptCollectionField` | One collection field: name, `PreceptCollectionKind`, `PreceptScalarType` inner type |
| `PreceptRule` | A global data rule: `rule <expr> because "reason"` — always holds |
| `StateEnsure` | A state-scoped ensure: `in/to/from <State> ensure <expr> because "reason"` |
| `EventEnsure` | An event-scoped arg validator: `on <Event> ensure <expr> because "reason"` |
| `PreceptTransitionRow` | One flat transition row: `from <State> on <Event> [when <expr>] → <outcome>` |
| `StateTransition` | Row outcome: `transition <State>` |
| `Rejection` | Row outcome: `reject "<message>"` |
| `NoTransition` | Row outcome: `no transition` |
| `PreceptEditBlock` | Editable field declaration: state-scoped `in <State> [when <Guard>] edit <Field>, ...` or stateless root-level `edit <Field>, ... [when <Guard>]` |
| `PreceptStateAction` | State entry/exit action: `to <State> ->` or `from <State> ->` automatic mutations |

### Scalar and Collection Enums

```csharp
public enum PreceptScalarType  { String, Number, Boolean, Null, Integer, Decimal, Choice }
public enum PreceptCollectionKind { Set, Queue, Stack }
```

### Expression AST

Guard predicates and rule expressions are pre-parsed at compile time into a `PreceptExpression` AST:

| Node type | Represents |
|-----------|-----------|
| `PreceptLiteralExpression` | `true`, `false`, `null`, string literal, number literal |
| `PreceptIdentifierExpression` | bare field/arg name, or dotted `EventName.ArgKey` form |
| `PreceptUnaryExpression` | `not` operator |
| `PreceptBinaryExpression` | `and`, `or`, `==`, `!=`, `<`, `>`, `<=`, `>=`, `+`, `-`, `*`, `/`, `contains` |
| `PreceptParenthesizedExpression` | `( … )` grouping |
| `PreceptFunctionCallExpression` | Built-in function calls: `round()`, `abs()`, `pow()`, `sqrt()`, etc. |
| `PreceptConditionalExpression` | `if ... then ... else` conditional expressions |

The AST is evaluated by `PreceptExpressionEvaluator` (internal). Callers never need to evaluate the AST directly.
