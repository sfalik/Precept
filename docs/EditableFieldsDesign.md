# Editable Fields Design Notes

Date: 2026-03-04

Status: **Runtime implemented.** Parser/model support was included in the language redesign; the runtime `Update` API, `IUpdatePatchBuilder`, editability enforcement, invariant/assert rule evaluation, and `Inspect` integration are now implemented.

> **Inspector UX note (2026-03-06):** The preview inspector applies direct field edits in explicit **Edit** mode. While typing, draft values are validated through `Inspect(...)`-based preview checks and violations are surfaced inline; runtime data is not committed until the user clicks **Save**. **Cancel** discards draft edits.
>
> The preview contract distinguishes between:
> - **Field-level errors**: violations attributed to specific edited fields and surfaced inline on those fields only.
> - **Form-level errors**: violations that cannot be attributed to one field alone and are surfaced in the draft rule violation banner.
>
> Attribution is owned by the preview service layer, not the webview. The webview renders the authoritative `EditableFields[i].Violation`, `FieldErrors`, and `FormErrors` returned by `inspectUpdate`; it does not infer ownership client-side from the raw runtime payload.

> **Language redesign note (2026-03-05):** The editable fields syntax has been updated from indentation-based `from <State> edit` blocks to flat keyword-anchored `in <State> edit <Field>, <Field>` statements, consistent with the new Precept language design (`docs/PreceptLanguageDesign.md`). The `in` preposition is used instead of `from` because editability is about what you can do **while residing in** a state, matching `in <State> assert` semantics. The parser/model support for `in <State> edit` is included in the language redesign; the runtime `Update` API is deferred to a follow-on task.

Depends on: **Rules** (docs/RulesDesign.md) — ✅ rules are now implemented. The prerequisite dependency is satisfied. Rules enforce data invariants on every mutation regardless of path, making direct field editing safe.

## Overview

Editable fields allow host applications to modify instance data directly — without declaring events, defining arguments, or writing `set` assignments. The DSL author declares which fields are editable in which states, and the runtime exposes a single `Update(patch)` method that validates editability, enforces type constraints, evaluates rules, and commits atomically.

## Motivation

### The three-layer argument

The event pipeline requires three layers of ceremony to change a field value:

1. **Event declaration** — `event EditNotes` with argument declarations
2. **Argument definitions** — `string Notes` on the event
3. **Set assignments** — `set Notes = EditNotes.Notes` inside a `from ... on ...` block

For **lifecycle actions** (state transitions, complex business logic, multi-field mutations with guards), this ceremony is earned — it provides routing, scoping, audit, and atomicity.

For **data editing** (updating a notes field, changing a priority, correcting a typo in a description), the ceremony is overhead. The event carries no routing logic, the guard is absent or trivial, and the `set` assignment is a mechanical pass-through of the argument to the field.

Editable fields eliminate all three layers simultaneously for the data-editing case. This is not a shortcut — it is a genuinely different semantic. "Edit this field" is not a lifecycle action; it is a data operation that happens to be state-scoped.

### "State and data live together"

A core design principle is that the machine instance is the single source of truth for both state and data. If data-heavy fields (notes, descriptions, contact info) require full event ceremony to modify, authors are incentivized to split those fields out of the machine — violating the co-location principle. Editable fields keep data in the machine while acknowledging that not all data changes are lifecycle events.

### Actor model parallel

The DSL's event pipeline maps cleanly to the actor model: private state, message-driven mutation, behavior switching. But the DSL operates in a single-instance, single-threaded domain — not a distributed concurrent system. The "mailbox protection" argument that justifies mandatory message passing in actors is less absolute here. Editable fields are a controlled relaxation for values where the event envelope adds cost without protection.

### Rules as the safety net

Editable fields are viable *because* rules exist. Without rules, direct field editing would bypass all data integrity constraints. With rules, the DSL author declares invariants once (`rule Balance >= 0 "..."`) and those invariants are enforced regardless of whether the field changes via an event `set` assignment or a direct `Update` call.

## Syntax

### The `in <State> edit` declaration

Editable field declarations use the `in` preposition ("while residing in a state") and the `edit` keyword, followed by a comma-separated list of field names:

```text
in <any|StateA[,StateB...]> edit <FieldName>, <FieldName>, ...
```

Each field name in the list declares that field as editable in the specified state(s). This is a flat, keyword-anchored statement — consistent with all other Precept declarations.

### Examples

```precept
precept WorkOrder

field Description as string default ""
field Notes as string nullable
field Priority as number default 3
field AssignedTo as string default ""
field Tags as set of string
field ResolutionSummary as string nullable

state Open initial
state InProgress
state Resolved
state Closed

# Notes and Priority are editable in any state
in any edit Notes, Priority

# Description and Tags are editable while work is active
in Open, InProgress edit Description, Tags

# Assigned technician is editable before resolution
in Open, InProgress edit AssignedTo

# Resolution summary is editable only when resolved
in Resolved edit ResolutionSummary
```

### Multi-state support

Multi-state `in` is already supported for state asserts (`in Open, InProgress assert ...`). Edit declarations reuse the same syntax — the parser handles comma-separated state lists identically.

### `in any edit`

`in any edit` makes the listed fields editable in every declared state, including terminal states. `any` means any — no special exclusions.

## Semantics

### Additive across blocks

When multiple edit declarations match the current state, their field lists are **unioned**. The effective editable set for a state is the union of all matching `in ... edit` declarations.

```precept
in any edit Notes

in Open edit Description

# In state Open: Notes + Description are editable
# In state Closed: only Notes is editable
```

This is consistent with how `in any assert` coexists with state-specific `in State assert` declarations — they are independent declarations, not overrides.

### Independence from events

Edit declarations and event transitions are independent features. A field can be both editable (via `in ... edit`) and modified by event `set` assignments. There is no conflict — they are different mutation paths with different semantics:

- **Event path**: lifecycle action with guards, branching, state transitions, audit trail
- **Edit path**: direct data modification with editability scope and invariant/assert enforcement

### No special terminal state treatment

`in any edit` includes terminal states (states with no outgoing transitions). If the author wants to exclude a terminal state, they list states explicitly instead of using `any`.

### Fields without rules

No warning is emitted for editable fields that have no associated rules. Many fields (notes, descriptions, free-text comments) are legitimately unconstrained. Adding a warning would create noise for the most common case.

### All field types supported

Editable fields support all declared field types:

- Scalar types: `string`, `number`, `boolean`, and their nullable variants
- Collection types: `set<T>`, `queue<T>`, `stack<T>`

Collection editing supports both granular operations (add/remove individual elements) and full replacement (swap the entire collection contents).

## Compiler Validations

The compiler validates edit declarations at compile time:

- **Unknown field names**: every field listed in an `edit` declaration must be a declared instance data field.
- **Unknown state names**: every state in the `in` clause must be a declared state (same validation as state asserts).
- **Duplicate field in same declaration**: a field listed twice in the same `edit` declaration is a warning.
- **No fields**: an `edit` declaration with no field names is a parse error.

## Model Extension

### `PreceptEditBlock` record

The parser produces one `PreceptEditBlock` per `(State, FieldNames)` grouping:

```csharp
public sealed record PreceptEditBlock(
    string State,
    IReadOnlyList<string> FieldNames,
    int SourceLine = 0);
```

When `in any edit` is used, the parser expands `any` into one `PreceptEditBlock` per declared state (same expansion pattern as `in any assert`). When `in State1, State2 edit` is used, the parser creates one `PreceptEditBlock` per listed state.

### `PreceptDefinition` extension

```csharp
public sealed record PreceptDefinition(
    string Name,
    IReadOnlyList<PreceptState> States,
    PreceptState InitialState,
    IReadOnlyList<PreceptEvent> Events,
    IReadOnlyList<PreceptTransitionRow> TransitionRows,
    IReadOnlyList<PreceptField> Fields,
    IReadOnlyList<PreceptCollectionField> CollectionFields,
    IReadOnlyList<PreceptInvariant>? Invariants = null,
    IReadOnlyList<StateAssertion>? StateAsserts = null,
    IReadOnlyList<EventAssertion>? EventAsserts = null,
    IReadOnlyList<PreceptEditBlock>? EditBlocks = null);  // <-- NEW
```

### `PreceptEngine` — editability map

At compile time, `PreceptEngine` builds an internal editability map:

```csharp
private readonly IReadOnlyDictionary<string, HashSet<string>> _editableFieldsByState;
// Key: state name → Value: set of editable field names (union of all matching edit blocks)
```

This precomputed map makes `Update` validation O(1) per field.

## Runtime API

### `Update(instance, patch)`

A single new method on `PreceptEngine`, following the same instance-parameter pattern as `Fire`:

```csharp
UpdateResult Update(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
```

The caller passes the current instance; `Update` returns a result containing the updated instance (or null on failure). Instances remain immutable records — `Update` returns a new instance via `with` expression, just like `Fire`.

The patch builder exposes typed operations for each supported field kind:

```csharp
public interface IUpdatePatchBuilder
{
    // Scalar fields
    IUpdatePatchBuilder Set(string fieldName, object? value);

    // Set operations
    IUpdatePatchBuilder Add(string fieldName, object value);
    IUpdatePatchBuilder Remove(string fieldName, object value);

    // Queue operations
    IUpdatePatchBuilder Enqueue(string fieldName, object value);
    IUpdatePatchBuilder Dequeue(string fieldName);

    // Stack operations
    IUpdatePatchBuilder Push(string fieldName, object value);
    IUpdatePatchBuilder Pop(string fieldName);

    // Full collection replacement
    IUpdatePatchBuilder Replace(string fieldName, IEnumerable<object> values);

    // Clear (all collection types)
    IUpdatePatchBuilder Clear(string fieldName);
}
```

### Usage example

```csharp
var definition = PreceptParser.Parse(dslText);
var engine = PreceptCompiler.Compile(definition);
var instance = engine.CreateInstance();

var result = engine.Update(instance, patch => patch
    .Set("Notes", "Customer called back — issue is intermittent")
    .Set("Priority", 1)
    .Add("Tags", "urgent")
    .Remove("Tags", "low-priority")
);

if (result.IsSuccess)
{
    instance = result.UpdatedInstance!; // new immutable instance with updated data
}
```

### Rule evaluation sequence

When `Update` is called, the runtime executes the following steps in order:

1. **Editability check** — For each field in the patch, verify the field is editable in the current state (union of matching `in ... edit` declarations). If any field is not editable, the entire update is rejected with `UneditableField` outcome. No partial application.

2. **Type check** — Verify each value matches the declared field type. Scalar type mismatches, null on non-nullable fields, and wrong-typed collection elements are rejected.

3. **Atomic mutation** — Apply all patch operations to a working copy of instance data, in declaration order within the patch. This is the same working-copy pattern used by `set` assignments in transitions.

4. **Constraint evaluation** — Evaluate invariants, state asserts (`in <CurrentState>` asserts), and field invariants against the post-mutation working copy. If any fail, all mutations are rolled back and the outcome is `ConstraintFailure` with violations. (Note: state asserts are checked because the data must remain valid for the state we're in, even though no state entry occurs.)

5. **Commit** — If all validations pass, the working copy replaces the live instance data.

### Outcome kinds

```csharp
public enum UpdateOutcome
{
    // Success
    Update,              // All fields modified, all constraints passed

    // Failure
    ConstraintFailure,   // Constraints violated — violations collected
    UneditableField,     // One or more fields not editable in current state
    InvalidInput         // Type mismatch, unknown field, or patch conflict
}
```

```csharp
public sealed record UpdateResult(
    UpdateOutcome Outcome,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is UpdateOutcome.Update;
}
```

`Update` is a new outcome kind — distinct from `Transition` (which implies a lifecycle event was processed). The caller sees "data was edited" vs. "an event was fired" as different categories. `UpdateOutcome` is a separate enum from `TransitionOutcome` because `Update` and `Fire` are fundamentally different operations with different outcome semantics.

### Patch conflict rules

The patch builder rejects structurally conflicting operations at build time (before validation begins):

| Conflict | Result |
|---|---|
| Duplicate `Set` on same scalar field | Error: `"Duplicate Set on field 'X'"` |
| `Replace` + granular op (`Add`/`Remove`/etc.) on same collection | Error: `"Cannot combine Replace with granular operations on field 'X'"` |
| Multiple granular ops on same collection | Allowed — executed in declaration order |
| `Set` on a collection field | Error: `"Use Replace for collection fields"` |
| `Add`/`Remove`/etc. on a scalar field | Error: `"Granular operations are only valid on collection fields"` |

### Relationship to Fire

`Update` and `Fire` are independent operations. `Update` never triggers state transitions — it modifies data within the current state. `Fire` never consults edit blocks — it uses event declarations and `from ... on ...` blocks.

Both share the same rules evaluation infrastructure. A field modified by `Update` is subject to the same rules that would apply if it were modified by a `set` assignment in a transition.

## Inspect Integration

### Editable fields on the aggregate `Inspect` result

The engine already has an aggregate `Inspect(instance)` method that returns `InspectionResult(CurrentState, InstanceData, Events[])` — a full snapshot for rendering an inspector view. Editable field information is state-level, not event-level — it answers "what fields can be edited in this state?" regardless of any event context. This makes it a natural extension of the aggregate inspect result.

Extend `InspectionResult` with an `EditableFields` collection:

```csharp
public sealed record InspectionResult(
    string CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<EventInspectionResult> Events,
    IReadOnlyList<PreceptEditableFieldInfo>? EditableFields = null);  // <-- NEW
```

Each entry provides enough metadata for the host to render edit controls, and carries a back-pointer to an associated `ConstraintViolation` (populated during hypothetical-patch inspection):

```csharp
public sealed record PreceptEditableFieldInfo(
    string FieldName,
    string FieldType,        // composite type: "string", "number", "boolean", "set<string>", "queue<number>", etc.
    bool IsNullable,
    object? CurrentValue,    // Current field value (scalar or collection)
    ConstraintViolation? Violation = null);  // non-null when a patch inspection found a violation implicating this field
```

### `ConstraintViolation` — structured violation with field attribution

A violation carries both a human-readable reason and a list of all `PreceptEditableFieldInfo` objects it implicates. This allows the host to highlight every field involved in a multi-field constraint (e.g. `A + B <= 100` implicates both `A` and `B`):

```csharp
public sealed record ConstraintViolation(
    string Message,
    ConstraintSource Source,
    IReadOnlyList<ConstraintTarget> Targets);
```

The bidirectional references (`FieldInfo.Violation` and `Violation.Targets`) form an object graph that is safe in C# but never serialized directly. The preview protocol uses indices instead (see Preview Protocol Extension).

### `Inspect(instance, patches)` — hypothetical-patch inspection

A new `Inspect` overload applies a patch to a working copy of instance data, runs the full rule evaluation pipeline (editability check, type check, invariant/assert evaluation), and returns an `InspectionResult` with violations reflected in `EditableFields`. **No commit occurs** — the session instance is unchanged. This is the runtime primitive used for per-keystroke rule checking in the preview UI.

```csharp
InspectionResult Inspect(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
```

The returned `EditableFields` is the same as for `Inspect(instance)`, but each `PreceptEditableFieldInfo` whose field is implicated in a violation has its `Violation` property set.

When no edit declarations exist, `EditableFields` is `null`. Otherwise, it contains the effective editable field set for the instance's current state (the union of all matching `in ... edit` declarations), pre-populated with current values.

### Relationship to per-event Inspect

The per-event `Inspect(instance, eventName)` returns `EventInspectionResult` and is unchanged. The aggregate `Inspect(instance)` now also carries editable field metadata. The new `Inspect(instance, patches)` overload adds hypothetical-patch rule evaluation without committing. All three answer different questions:

- **`Inspect(instance, eventName)`**: "What happens if I fire this event?"
- **`Inspect(instance)`**: "What data can I edit in this state, and what are the current values?"
- **`Inspect(instance, patches)`**: "If I apply these edits, which fields would be in violation?"

## Full Example

### DSL definition

```precept
precept WorkOrder

field Description as string default ""
field Notes as string nullable
field Priority as number default 3
field AssignedTo as string default ""
field Status as string default "New"
field Tags as set of string
field EstimatedHours as number nullable
field ResolutionSummary as string nullable

# Invariants protect data integrity regardless of mutation path
invariant Priority >= 1 && Priority <= 5 because "Priority must be between 1 and 5"

state Open initial
state InProgress
state Resolved
state Closed

in Resolved assert ResolutionSummary != null because "Resolution requires a summary"

event Assign with Technician as string
event StartWork
event Resolve with Summary as string
event Close

# Lifecycle transitions — full event pipeline
from Open on Assign when Assign.Technician != ""
    -> set AssignedTo = Assign.Technician
    -> transition InProgress

from Open on Assign
    -> reject "Technician name is required"

from InProgress on Resolve when Resolve.Summary != ""
    -> set ResolutionSummary = Resolve.Summary
    -> transition Resolved

from InProgress on Resolve
    -> reject "Resolution summary is required"

from Resolved on Close -> transition Closed

# Data editing — direct field access scoped by state
in any edit Notes, Priority, Tags
in Open, InProgress edit Description, EstimatedHours, AssignedTo
in Resolved edit ResolutionSummary
```

### Host application

```csharp
// Create and compile
PreceptDefinition definition = PreceptParser.Parse(dslText);
PreceptEngine engine = PreceptCompiler.Compile(definition);
PreceptInstance instance = engine.CreateInstance();

// Lifecycle: assign a technician (event pipeline)
FireResult fireResult = engine.Fire(instance, "Assign", new Dictionary<string, object?>
{
    ["Technician"] = "Alice"
});
// fireResult.Outcome == Transition, state => InProgress
instance = fireResult.UpdatedInstance!;

// Data editing: update notes and priority (no event needed)
UpdateResult editResult = engine.Update(instance, patch => patch
    .Set("Notes", "Customer prefers morning appointments")
    .Set("Priority", 1)
    .Add("Tags", "urgent")
);
// editResult.Outcome == Update, state unchanged (InProgress)
instance = editResult.UpdatedInstance!;

// Full snapshot: events + editable fields in one call
InspectionResult snapshot = engine.Inspect(instance);
// snapshot.EditableFields => [Notes, Priority, Tags, Description, EstimatedHours, AssignedTo]
// snapshot.Events => per-event inspection results for all relevant events

// Per-event inspect: see what happens if we fire a specific event
EventInspectionResult inspectResolve = engine.Inspect(instance, "Resolve");
// inspectResolve.Outcome == Transition (with guard status)

// Hypothetical-patch inspect: validate edits before applying
PreceptInspectionResult dryRun = engine.Inspect(instance, patch => patch.Set("Priority", 99));
// dryRun.EditableFields[Priority].Violation.Reason == "Priority must be between 1 and 5"
// No commit — instance is unchanged

// Rules enforcement: priority out of range
PreceptUpdateResult badEdit = engine.Update(instance, patch => patch
    .Set("Priority", 99)
);
// badEdit.Outcome == Blocked
// badEdit.Reasons => ["Priority must be between 1 and 5"]

// Not editable in current state
PreceptUpdateResult wrongState = engine.Update(instance, patch => patch
    .Set("ResolutionSummary", "Fixed it")
);
// wrongState.Outcome == NotAllowed
// wrongState.Reasons => ["Field 'ResolutionSummary' is not editable in state 'InProgress'"]
```

## Grammar Extension

The edit declaration adds one new production to the DSL grammar:

```text
<EditDecl> := "in" <StateTarget> "edit" <FieldList>

<FieldList> := <FieldName> ("," <FieldName>)*
<FieldName> := identifier referencing a declared instance data field
<StateTarget> := "any" | Identifier ("," Identifier)*
```

The `edit` keyword is a new reserved word. It appears only in the `in ... edit` position — it cannot be used as a field name, state name, or event name.

**Disambiguation:** `in <State>` is followed by either `assert` (state-scoped invariant) or `edit` (editable field declaration). The parser disambiguates at LL(2).

## Preview Protocol Extension

### Typed field data in snapshot

The `PreceptPreviewSnapshot.Data` property changes from a raw value map to a map of typed field values. This gives the webview all information needed to render field values correctly — including collection fields — regardless of whether they are editable:

```csharp
internal sealed record PreceptPreviewFieldValue(
    object? Value,
    string Type,       // composite: "string", "number", "boolean", "set<string>", "queue<number>", etc.
    bool IsNullable);

// Snapshot.Data changes from:
IReadOnlyDictionary<string, object?> Data
// to:
IReadOnlyDictionary<string, PreceptPreviewFieldValue> Data
```

The handler builds this map from `session.Engine.Fields` and `session.Engine.CollectionFields` (both already public on `PreceptEngine`) combined with `session.Instance.InstanceData`. No new engine API is needed.

The webview parses `Type` to determine rendering:

```javascript
const isCollection = type.includes('<');
const collectionKind = isCollection ? type.split('<')[0] : null;  // "set", "queue", "stack"
const elementType = isCollection ? type.slice(type.indexOf('<') + 1, -1) : type;
```

### Collection display (read-only mode)

| Collection type | Read-only display |
|---|---|
| `set<T>` | `{ urgent, billing }` — unordered, curly braces |
| `queue<T>` | `[ a, b, c →]` — ordered front-to-back, arrow at tail |
| `stack<T>` | `[ 3 \| 2 \| 1 ]` — top at left, pipe separators |
| empty collection | `{ }` or `[ ]` matching the type |

### Snapshot: `Violations` and `EditableFields`

The snapshot gains two new fields for edit mode support. `EditableFields` identifies which fields are editable in the current state. `Violations` carries structured violation objects referenced by index from `EditableFields`:

```csharp
internal sealed record PreceptPreviewViolation(
    string Reason,
    IReadOnlyList<int> AffectedFieldIndexes);  // indices into EditableFields list

internal sealed record PreceptPreviewEditableField(
    string Name,
    int? ViolationIndex = null);  // index into snapshot.Violations; null if no violation

// Snapshot gains:
IReadOnlyList<PreceptPreviewEditableField>? EditableFields = null,
IReadOnlyList<PreceptPreviewViolation>? Violations = null
```

The handler translates the runtime's object graph (bidirectional `ConstraintViolation`/`PreceptEditableFieldInfo` refs) into stable indices. Each `EditableFields[i].ViolationIndex` points into `Violations[j]`, and each `Violations[j].AffectedFieldIndexes` lists the indices back into `EditableFields`. This lets the webview:

- Turn an input red using `field.violationIndex != null`
- Show the reason using `snapshot.violations[field.violationIndex].reason`
- Highlight sibling fields using `snapshot.violations[j].affectedFieldIndexes`

### Edit mode UI behavior

The data panel has two modes — **read-only** and **edit mode**:

**Read-only mode** (default):
- All field values display as formatted text using the typed `Data` map.
- Editable fields (those in `EditableFields`) show a subtle pencil indicator (✎) next to their value. Non-editable fields have no indicator.
- Field order follows declaration order.
- An "Edit" button in the panel header enters edit mode.

**Edit mode**:
- Editable fields replace their value display with type-appropriate input controls (see below).
- Non-editable fields remain as formatted text, visually dimmed.
- The panel header shows "Apply" and "Cancel" buttons in place of "Edit".
- **Cancel** — discards all buffered changes and returns to read-only mode. No server call.
- **Apply** — sends a single `"update"` action with all buffered patches. On success, updates the session instance and returns to read-only mode with a fresh snapshot. On failure (`Blocked`/`Invalid`), stays in edit mode and shows violations.

**Per-keystroke validation**:
- On each input change, the webview debounces and sends an `"inspect"` action with the full current patch buffer. No commit occurs.
- The server applies patches to a working copy, runs the full validation pipeline, and returns a snapshot with `editableFields[i].violationIndex` populated for any violations.
- Inputs with violations are highlighted red; the violation reason appears below the input.
- The full patch buffer is sent each time (not just the changed field) so multi-field invariants are evaluated correctly.

### Collection field edit controls

| Collection type | Edit controls |
|---|---|
| `set<T>` | Chip list with ✕ per item + add-item input; **Clear** button |
| `queue<T>` | Ordered list; Enqueue input at tail; Dequeue button at head; **Clear** button |
| `stack<T>` | Ordered list top-first; Push input at top; Pop button at top; **Clear** button |
| All | **Replace** button — opens a confirmation textarea for full collection replacement |

Each granular operation (add/remove/enqueue/dequeue/push/pop) adds to the buffered patch using the appropriate `op` value. Replace and Clear add a single `"replace"` or `"clear"` op and disable granular controls for that field for the remainder of the edit session.

### `PreceptPreviewRequest` extension

```csharp
internal sealed record PreceptPreviewRequest(
    string Action,
    DocumentUri Uri,
    string? Text = null,
    string? EventName = null,
    IReadOnlyDictionary<string, object?>? Args = null,
    IReadOnlyList<PreceptPreviewReplayStep>? Steps = null,
    IReadOnlyList<PreceptPreviewPatchOp>? Patches = null);  // <-- NEW for "inspect" and "update" actions

internal sealed record PreceptPreviewPatchOp(
    string Field,
    string Op,       // "set", "add", "remove", "enqueue", "dequeue", "push", "pop", "replace", "clear"
    object? Value);  // null for dequeue, pop, clear
```

**Actions:**
- `"inspect"` with `Patches` — applies patches to working copy, returns snapshot with violations populated, **no commit**. Used for per-keystroke validation (debounced).
- `"update"` with `Patches` — applies patches atomically, commits on success. Returns updated snapshot or failure snapshot with violations.

## Language Server Impact

### Parser

- The `EditDecl` parser combinator recognizes `in <states> edit <fieldList>` as a statement (no regex needed — Superpower token stream).
- Report errors for unknown fields, unknown states, empty field lists.
- Expand multi-state and `any` targets (same as state asserts).
- Generate one `PreceptEditBlock` per source state.

### Analyzer

- Validate field references in edit declarations.
- Compute effective editable set per state for diagnostics.
- Warn on duplicate field within the same declaration.

### Completions

- After `in <states>`, suggest both `assert` and `edit` as continuations.
- After `in <states> edit`, suggest declared field names (comma-separated context).

### Semantic tokens

- `edit` keyword highlighted consistently with `assert`, `transition`, etc.
- Field names in edit declarations highlighted as variable references.

## Implementation Prompt

The following prompt can be pasted into a new session to implement the editable fields feature:

---

Implement the editable fields feature for the Precept state machine DSL as specified in `docs/EditableFieldsDesign.md`. This is a full-stack implementation across parser, model, compiler, runtime, language server, preview protocol, preview UI, and documentation. Read `docs/EditableFieldsDesign.md` thoroughly before starting — it is the complete design spec. Also read `docs/RuntimeApiDesign.md` for the current public API surface and naming conventions. Read at least one sample from `samples/` before writing any DSL strings.

**Summary:** Editable fields allow direct modification of instance data fields in specific states without going through the event pipeline. The DSL syntax is `in <any|State1,State2> edit <Field>, <Field>` as a flat statement. The runtime exposes `engine.Update(instance, patch)` for atomic edits with full validation. The preview UI adds an explicit edit mode with per-keystroke inspect validation and a single Apply/Cancel commit.

**DSL syntax:** `in <any|StateA[,StateB...]> edit <FieldName>, <FieldName>, ...` — flat, keyword-anchored, comma-separated field names. Multi-state lists supported (same as `in <State> assert`). `in any edit` = editable in all states. Multiple declarations are additive (union). The `in` preposition is used because editability is about residing in a state. `edit` is a new reserved word.

**Compiler validations:** Unknown field names → error. Unknown state names → error. Empty field list → parse error. Duplicate field in same declaration → warning.

**Model:** Add `PreceptEditBlock(string State, IReadOnlyList<string> FieldNames, int SourceLine = 0)` record. Add `IReadOnlyList<PreceptEditBlock>? EditBlocks = null` to `PreceptDefinition`. Parser expands `in any edit` into one `PreceptEditBlock` per declared state and `in State1, State2 edit` into one per listed state (same expansion as state asserts).

**Runtime API:**
- Add `Update(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)` on `PreceptEngine`, returning `PreceptUpdateResult`.
- `IUpdatePatchBuilder` supports: `Set` (scalars), `Add`/`Remove` (sets), `Enqueue`/`Dequeue` (queues), `Push`/`Pop` (stacks), `Replace` and `Clear` (all collections).
- Validation sequence: editability check → type check → atomic mutation on working copy → invariant/`in <CurrentState>` assert evaluation → commit or rollback.
- Outcomes: `UpdateOutcome.Update`, `UneditableField`, `ConstraintFailure`, `InvalidInput`.
- Patch conflicts detected at build time: duplicate Set on same scalar, Replace + granular on same collection, Set on collection field, granular op on scalar field.
- `UpdateResult(UpdateOutcome Outcome, IReadOnlyList<ConstraintViolation> Violations, PreceptInstance? UpdatedInstance)`.

**Inspect integration:**
- Add `IReadOnlyList<PreceptEditableFieldInfo>? EditableFields = null` to `InspectionResult`.
- `PreceptEditableFieldInfo(string FieldName, string FieldType, bool IsNullable, object? CurrentValue, ConstraintViolation? Violation = null)`. `FieldType` is composite: `"string"`, `"set<string>"`, `"queue<number>"`, etc. No separate `CollectionType` property.
- Add `ConstraintViolation(string Message, ConstraintSource Source, IReadOnlyList<ConstraintTarget> Targets)` — structured violation with attribution.
- Add `Inspect(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)` overload: applies patch to working copy, runs full validation pipeline, returns `InspectionResult` with violations reflected in `EditableFields`. **No commit.** Used for per-keystroke validation.
- The aggregate `engine.Inspect(instance)` returns event statuses AND editable field info in one call. `engine.Inspect(instance, patches)` does the same with hypothetical patch evaluation.

**Preview protocol (`PreceptPreviewProtocol.cs`):**
- Change `PreceptPreviewSnapshot.Data` from `IReadOnlyDictionary<string, object?>` to `IReadOnlyDictionary<string, PreceptPreviewFieldValue>`.
- `PreceptPreviewFieldValue(object? Value, string Type, bool IsNullable)` — `Type` is composite (e.g. `"set<string>"`).
- Add `IReadOnlyList<PreceptPreviewEditableField>? EditableFields = null` and `IReadOnlyList<PreceptPreviewViolation>? Violations = null` to `PreceptPreviewSnapshot`.
- `PreceptPreviewEditableField(string Name, int? ViolationIndex = null)` — index into `Violations`.
- `PreceptPreviewViolation(string Reason, IReadOnlyList<int> AffectedFieldIndexes)` — indices into `EditableFields`.
- `PreceptPreviewRequest` gains `IReadOnlyList<PreceptPreviewPatchOp>? Patches = null`.
- `PreceptPreviewPatchOp(string Field, string Op, object? Value)` — ops: `"set"`, `"add"`, `"remove"`, `"enqueue"`, `"dequeue"`, `"push"`, `"pop"`, `"replace"`, `"clear"`.
- `"inspect"` action with `Patches`: applies patches to working copy, returns snapshot with `EditableFields`/`Violations` populated, **no commit**. Used for per-keystroke validation (debounced).
- `"update"` action with `Patches`: commits atomically. Returns updated snapshot on success; failure snapshot with violations on `Blocked`/`Invalid`.

**Preview handler (`PreceptPreviewHandler.cs`):**
- Build typed `Data` map using `session.Engine.Fields` (scalar) and `session.Engine.CollectionFields` (collections) — both already public on `PreceptEngine`. No new engine API needed.
- Handle `"inspect"` with patches: call `engine.Inspect(instance, patches)`, translate bidirectional object graph to index-based protocol types, return snapshot without updating `session.Instance`.
- Handle `"update"` with patches: call `engine.Update(instance, patches)`, on `Updated` advance `session.Instance` and return fresh snapshot; on `Blocked`/`Invalid` return failure snapshot with violations.

**Preview UI (`inspector-preview.html`):**
- **Collection display (read-only):** Parse `type` to detect collections (`type.includes('<')`). Render: `set` → `{ a, b }`, `queue` → `[ a, b, c →]`, `stack` → `[ top | 2 | 3 ]`, empty → `{ }` or `[ ]`. Update `formatValue` to be collection-aware.
- **Edit mode toggle:** "Edit" button in the data panel header. In read-only mode, fields listed in `snapshot.editableFields` show a subtle ✎ indicator next to their value; non-editable fields show no indicator. Field order matches declaration order throughout.
- **Edit mode layout:** Editable fields show type-appropriate input controls; non-editable fields stay as dimmed text. Header shows "Apply" and "Cancel" buttons.
- **Per-keystroke validation:** On each input change, debounce (e.g. 300ms) and send `"inspect"` with the full current patch buffer. Read `editableFields[i].violationIndex` from the response — highlight the input red and show `violations[j].reason` below if violated. Also highlight any sibling fields implicated by `violations[j].affectedFieldIndexes`.
- **Apply:** Send `"update"` with full patch buffer. On success, update session state and exit edit mode. On `Blocked`/`Invalid`, stay in edit mode and display violations.
- **Cancel:** Discard buffer, exit edit mode. No server call.
- **Collection edit controls — `set<T>`:** Chip list with ✕ per item + add-item input + **Clear** button.
- **Collection edit controls — `queue<T>`:** Ordered list; Enqueue input at tail; Dequeue button at head; **Clear** button.
- **Collection edit controls — `stack<T>`:** Ordered list top-first; Push input at top; Pop button at top; **Clear** button.
- **Replace (all collection types):** A **Replace** button opens a confirmation textarea pre-filled with current items; on confirm, adds a `"replace"` op and disables granular controls for that field.

**Tests:** Add to `test/Precept.Tests/` and `test/Precept.LanguageServer.Tests/`:
- Edit declaration parsing: single state, multi-state, `in any`
- Additive semantics across overlapping declarations
- `Update` with scalar fields: `Updated`, `NotAllowed`, `Blocked`, `Invalid` outcomes
- `Update` with collection fields: all op types (`Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`, `Replace`, `Clear`)
- Editability enforcement per state (field editable in one state, not another)
- Patch conflict detection at build time
- Invariant/assert enforcement on update; atomic rollback on violation
- `Inspect(instance)` → `EditableFields` populated correctly
- `Inspect(instance, patches)` → violations reflected in `EditableFields` with correct `AffectedFields`
- Coexistence of edit declarations with event transitions

**Documentation:** Update `README.md` to mention editable fields. Update `docs/EditableFieldsDesign.md` status from "Design phase" to "Implemented". Update `docs/RuntimeApiDesign.md` if API signatures changed.

**Grammar sync (non-negotiable — do not skip):** Update `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`:
1. Add `inEditHeader` pattern for `in … edit` header lines, consistent with existing `inAssertHeader`.
2. Add `edit` to `controlKeywords` alternation with `\b` word-boundary anchors.
3. Insert `inEditHeader` before `controlKeywords` in the top-level `patterns` array.
4. Verify JSON is valid after changes.

**Intellisense sync (non-negotiable — do not skip):** Update `tools/Precept.LanguageServer/PreceptAnalyzer.cs` and `PreceptSemanticTokensHandler.cs`:
1. Add `edit` to `KeywordItems` in `PreceptAnalyzer.cs`.
2. Add `edit` to `KeywordTokens` in `PreceptSemanticTokensHandler.cs`.
3. Add regex branch in `GetCompletions` detecting `^\s*in\s+[^\n]+\s+edit` and returning declared field name completions.
4. Add `in … edit` header line to `HighlightNamedSymbols` semantic token handling.

**Build:** `dotnet build` from repo root. Run `test/Precept.Tests/` and `test/Precept.LanguageServer.Tests/`. All 394 existing tests must continue to pass.
