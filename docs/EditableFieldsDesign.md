# Editable Fields Design Notes

Date: 2026-03-04

Status: **Design phase — not yet implemented.**

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

### The `from <State> edit` block

Editable field blocks reuse the existing `from` keyword and follow the same multi-state syntax as transitions:

```text
from <any|StateA[,StateB...]> edit
  <FieldName>
  <FieldName>
  ...
```

Each field name on an indented line declares that field as editable in the specified state(s). Block-form only — no inline one-liners, consistent with transition blocks.

### Examples

```text
machine WorkOrder

string Description = ""
string? Notes = null
number Priority = 3
string AssignedTo = ""
set<string> Tags
string? ResolutionSummary = null

state Open initial
state InProgress
state Resolved
state Closed

# Notes and Priority are editable in any state
from any edit
  Notes
  Priority

# Description and Tags are editable while work is active
from Open, InProgress edit
  Description
  Tags

# Assigned technician is editable before resolution
from Open, InProgress edit
  AssignedTo

# Resolution summary is editable only when resolved
from Resolved edit
  ResolutionSummary
```

### Multi-state support

Multi-state `from` is already supported for transitions (`from State1, State2 on Event`). Edit blocks reuse the same syntax — the parser handles comma-separated state lists identically.

### `from any edit`

`from any edit` makes the listed fields editable in every declared state, including terminal states. `any` means any — no special exclusions.

## Semantics

### Additive across blocks

When multiple edit blocks match the current state, their field lists are **unioned**. The effective editable set for a state is the union of all matching `from ... edit` blocks.

```text
from any edit
  Notes

from Open edit
  Description

# In state Open: Notes + Description are editable
# In state Closed: only Notes is editable
```

This is consistent with how `from any on Event` coexists with state-specific `from State on Event` blocks — they are independent declarations, not overrides.

### Independence from events

Edit blocks and event blocks are independent features. A field can be both editable (via `from ... edit`) and modified by event `set` assignments. There is no conflict — they are different mutation paths with different semantics:

- **Event path**: lifecycle action with guards, branching, state transitions, audit trail
- **Edit path**: direct data modification with editability scope and rules enforcement

### No special terminal state treatment

`from any edit` includes terminal states (states with no outgoing transitions). If the author wants to exclude a terminal state, they list states explicitly instead of using `any`.

### Fields without rules

No warning is emitted for editable fields that have no associated rules. Many fields (notes, descriptions, free-text comments) are legitimately unconstrained. Adding a warning would create noise for the most common case.

### All field types supported

Editable fields support all declared field types:

- Scalar types: `string`, `number`, `boolean`, and their nullable variants
- Collection types: `set<T>`, `queue<T>`, `stack<T>`

Collection editing supports both granular operations (add/remove individual elements) and full replacement (swap the entire collection contents).

## Compiler Validations

The compiler validates edit blocks at compile time:

- **Unknown field names**: every field listed in an `edit` block must be a declared instance data field.
- **Unknown state names**: every state in the `from` clause must be a declared state (same validation as transition blocks).
- **Duplicate field in same block**: a field listed twice in the same `edit` block is a warning.
- **No fields**: an `edit` block with no field names is a parse error.

## Model Extension

### `DslEditBlock` record

The parser produces one `DslEditBlock` per `(FromState, FieldNames)` grouping:

```csharp
public sealed record DslEditBlock(
    string FromState,
    IReadOnlyList<string> FieldNames,
    int SourceLine = 0);
```

When `from any edit` is used, the parser expands `any` into one `DslEditBlock` per declared state (same expansion pattern as `from any on Event`). When `from State1, State2 edit` is used, the parser creates one `DslEditBlock` per listed state.

### `DslWorkflowModel` extension

```csharp
public sealed record DslWorkflowModel(
    string Name,
    IReadOnlyList<DslState> States,
    DslState InitialState,
    IReadOnlyList<DslEvent> Events,
    IReadOnlyList<DslTransition> Transitions,
    IReadOnlyList<DslField> Fields,
    IReadOnlyList<DslCollectionField> CollectionFields,
    IReadOnlyList<DslRule>? TopLevelRules = null,
    IReadOnlyList<DslEditBlock>? EditBlocks = null);  // <-- NEW
```

### `DslWorkflowEngine` — editability map

At compile time, `DslWorkflowEngine` builds an internal editability map:

```csharp
private readonly IReadOnlyDictionary<string, HashSet<string>> _editableFieldsByState;
// Key: state name → Value: set of editable field names (union of all matching edit blocks)
```

This precomputed map makes `Update` validation O(1) per field.

## Runtime API

### `Update(instance, patch)`

A single new method on `DslWorkflowEngine`, following the same instance-parameter pattern as `Fire`:

```csharp
DslUpdateResult Update(DslWorkflowInstance instance, Action<IUpdatePatchBuilder> patch)
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
var model = DslWorkflowParser.Parse(dslText);
var engine = DslWorkflowCompiler.Compile(model);
var instance = engine.CreateInstance();

var result = engine.Update(instance, patch => patch
    .Set("Notes", "Customer called back — issue is intermittent")
    .Set("Priority", 1)
    .Add("Tags", "urgent")
    .Remove("Tags", "low-priority")
);

if (result.Outcome == UpdateOutcome.Updated)
{
    instance = result.UpdatedInstance!; // new immutable instance with updated data
}
```

### Validation sequence

When `Update` is called, the runtime executes the following steps in order:

1. **Editability check** — For each field in the patch, verify the field is editable in the current state (union of matching `from ... edit` blocks). If any field is not editable, the entire update is rejected with `NotAllowed` outcome. No partial application.

2. **Type check** — Verify each value matches the declared field type. Scalar type mismatches, null on non-nullable fields, and wrong-typed collection elements are rejected.

3. **Atomic mutation** — Apply all patch operations to a working copy of instance data, in declaration order within the patch. This is the same working-copy pattern used by `set` assignments in transitions.

4. **Rules evaluation** — Evaluate field rules, top-level rules, and the current state's rules against the post-mutation working copy. If any rule fails, all mutations are rolled back and the outcome is `Blocked` with violated rule reasons. (Note: the current state's rules are checked because the data must remain valid for the state we're in, even though no state entry occurs.)

5. **Commit** — If all validations pass, the working copy replaces the live instance data.

### Outcome kinds

```csharp
public enum UpdateOutcome
{
    Updated,      // Success — all fields modified, all rules passed
    NotAllowed,   // One or more fields not editable in current state
    Blocked,      // Rules violated — reasons collected
    Invalid       // Type mismatch, unknown field, or patch conflict
}
```

```csharp
public sealed record DslUpdateResult(
    UpdateOutcome Outcome,
    IReadOnlyList<string> Reasons,
    DslWorkflowInstance? UpdatedInstance  // null when not Updated
);
```

`Updated` is a new outcome kind — distinct from `Accepted` (which implies a lifecycle event was processed). The caller sees "data was edited" vs. "an event was fired" as different categories. `UpdateOutcome` is a separate enum from `DslOutcomeKind` because `Update` and `Fire` are fundamentally different operations with different outcome semantics.

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

The engine already has an aggregate `Inspect(instance)` method that returns `DslInspectionResult(CurrentState, InstanceData, Events[])` — a full snapshot for rendering an inspector view. Editable field information is state-level, not event-level — it answers "what fields can be edited in this state?" regardless of any event context. This makes it a natural extension of the aggregate inspect result.

Extend `DslInspectionResult` with an `EditableFields` collection:

```csharp
public sealed record DslInspectionResult(
    string CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<DslEventInspectionResult> Events,
    IReadOnlyList<DslEditableFieldInfo>? EditableFields = null);  // <-- NEW
```

Each entry provides enough metadata for the host to render edit controls:

```csharp
public sealed record DslEditableFieldInfo(
    string FieldName,
    string FieldType,        // "string", "number", "boolean", "set<string>", etc.
    bool IsNullable,
    object? CurrentValue,    // Current field value (scalar or serialized collection)
    string? CollectionType   // null for scalars; "set", "queue", "stack" for collections
);
```

When no edit blocks are declared, `EditableFields` is `null`. Otherwise, it contains the effective editable field set for the instance's current state (the union of all matching `from ... edit` blocks), pre-populated with current values.

This gives the host application (or preview UI) enough information to render an edit form with type-appropriate input controls and editability scope — all from a single `engine.Inspect(instance)` call.

### Inspect does not simulate Update

Unlike event inspection (which simulates guard evaluation and set assignments), the editable fields list does not simulate update operations. It reports *what is editable* — the host decides what to change and calls `Update` with a concrete patch.

### Relationship to per-event Inspect

The per-event `Inspect(instance, eventName)` returns `DslEventInspectionResult` and is unchanged. The aggregate `Inspect(instance)` now also carries editable field metadata. Both answer different questions:

- **Per-event Inspect**: "What happens if I fire this event?"
- **Aggregate Inspect `EditableFields`**: "What data can I edit in this state?"

The preview handler calls aggregate `Inspect` once and receives both event statuses and editable field info in one result.

## Full Example

### DSL definition

```text
machine WorkOrder

string Description = ""
string? Notes = null
number Priority = 3
string AssignedTo = ""
string Status = "New"
set<string> Tags
number? EstimatedHours = null
string? ResolutionSummary = null

# Rules protect data integrity regardless of mutation path
number Priority
  rule Priority >= 1 && Priority <= 5 "Priority must be between 1 and 5"

state Open initial
state InProgress
state Resolved
  rule ResolutionSummary != null "Resolution requires a summary"
state Closed

event Assign
  string Technician

event StartWork

event Resolve
  string Summary

event Close

# Lifecycle transitions — full event pipeline
from Open on Assign
  if Assign.Technician != ""
    set AssignedTo = Assign.Technician
    transition InProgress
  else
    reject "Technician name is required"

from InProgress on Resolve
  if Resolve.Summary != ""
    set ResolutionSummary = Resolve.Summary
    transition Resolved
  else
    reject "Resolution summary is required"

from Resolved on Close
  transition Closed

# Data editing — direct field access scoped by state
from any edit
  Notes
  Priority
  Tags

from Open, InProgress edit
  Description
  EstimatedHours
  AssignedTo

from Resolved edit
  ResolutionSummary
```

### Host application

```csharp
// Create and compile
DslWorkflowModel model = DslWorkflowParser.Parse(dslText);
DslWorkflowEngine engine = DslWorkflowCompiler.Compile(model);
DslWorkflowInstance instance = engine.CreateInstance();

// Lifecycle: assign a technician (event pipeline)
DslFireResult fireResult = engine.Fire(instance, "Assign", new Dictionary<string, object?>
{
    ["Technician"] = "Alice"
});
// fireResult.Outcome == Accepted, state => InProgress
instance = fireResult.UpdatedInstance!;

// Data editing: update notes and priority (no event needed)
DslUpdateResult editResult = engine.Update(instance, patch => patch
    .Set("Notes", "Customer prefers morning appointments")
    .Set("Priority", 1)
    .Add("Tags", "urgent")
);
// editResult.Outcome == Updated, state unchanged (InProgress)
instance = editResult.UpdatedInstance!;

// Full snapshot: events + editable fields in one call
DslInspectionResult snapshot = engine.Inspect(instance);
// snapshot.EditableFields => [Notes, Priority, Tags, Description, EstimatedHours, AssignedTo]
// snapshot.Events => per-event inspection results for all relevant events

// Per-event inspect: see what happens if we fire a specific event
DslEventInspectionResult inspectResolve = engine.Inspect(instance, "Resolve");
// inspectResolve.Outcome == Accepted (with guard status)

// Rules enforcement: priority out of range
DslUpdateResult badEdit = engine.Update(instance, patch => patch
    .Set("Priority", 99)
);
// badEdit.Outcome == Blocked
// badEdit.Reasons => ["Priority must be between 1 and 5"]

// Not editable in current state
DslUpdateResult wrongState = engine.Update(instance, patch => patch
    .Set("ResolutionSummary", "Fixed it")
);
// wrongState.Outcome == NotAllowed
// wrongState.Reasons => ["Field 'ResolutionSummary' is not editable in state 'InProgress'"]
```

## Grammar Extension

The edit block adds one new production to the DSL grammar:

```text
<EditBlock> := from <any|StateA[,StateB...]> edit
                 <FieldName>+

<FieldName> := identifier referencing a declared instance data field
```

The `edit` keyword is a new reserved word. It appears only in the `from ... edit` position — it cannot be used as a field name, state name, or event name.

## Preview Protocol Extension

### Snapshot: `EditableFields` collection

The `SmPreviewSnapshot` record is extended with an `EditableFields` collection:

```csharp
internal sealed record SmPreviewSnapshot(
    // ... existing fields ...
    IReadOnlyList<SmPreviewEditableField>? EditableFields = null);

internal sealed record SmPreviewEditableField(
    string Name,
    string Type,           // "string", "number", "boolean", "set<string>", etc.
    bool IsNullable,
    object? CurrentValue,
    string? CollectionType // null for scalars; "set", "queue", "stack" for collections
);
```

The preview handler reads `EditableFields` from the aggregate `engine.Inspect(instance)` result and maps each `DslEditableFieldInfo` to an `SmPreviewEditableField` record in the snapshot.

### New `"update"` action

The preview handler adds an `"update"` action (parallel to `"fire"`):

```json
{
  "action": "update",
  "uri": "file:///path/to/file.precept",
  "patches": [
    { "field": "Notes", "op": "set", "value": "Updated notes" },
    { "field": "Tags", "op": "add", "value": "urgent" },
    { "field": "Priority", "op": "set", "value": 1 }
  ]
}
```

The `patches` array is translated to `IUpdatePatchBuilder` calls. On success, the preview handler updates the session instance and returns a fresh snapshot.

### `SmPreviewRequest` extension

```csharp
internal sealed record SmPreviewRequest(
    string Action,
    DocumentUri Uri,
    string? Text = null,
    string? EventName = null,
    IReadOnlyDictionary<string, object?>? Args = null,
    IReadOnlyList<SmPreviewReplayStep>? Steps = null,
    IReadOnlyList<SmPreviewPatchOp>? Patches = null);  // <-- NEW

internal sealed record SmPreviewPatchOp(
    string Field,
    string Op,       // "set", "add", "remove", "enqueue", "dequeue", "push", "pop", "replace", "clear"
    object? Value);  // null for dequeue, pop, clear
```

## Language Server Impact

### Parser

- Add `FromEditRegex` to recognize `from <states> edit` as a new block form (parallel to `FromOnRegex`).
- Parse indented field names as the edit block body.
- Report errors for unknown fields, unknown states, empty blocks.
- Call `ParseIdentifierList` for multi-state expansion (same as `ParseFromOnBlock`).
- Generate one `DslEditBlock` per source state.

### Analyzer

- Validate field references in edit blocks.
- Compute effective editable set per state for diagnostics.
- Warn on duplicate field within the same block.

### Completions

- After `from <states>`, suggest both `on` and `edit` as continuations.
- Inside an edit block body, suggest declared field names not already listed.

### Semantic tokens

- `edit` keyword highlighted consistently with `on`, `transition`, etc.
- Field names in edit blocks highlighted as variable references.

## Implementation Prompt

The following prompt can be pasted into a new session to implement the editable fields feature:

---

Implement the editable fields feature for the state machine DSL as specified in docs/EditableFieldsDesign.md. This is a full-stack implementation across parser, model, compiler, runtime, language server, and documentation. Read docs/EditableFieldsDesign.md thoroughly before starting — it is the complete design spec. Also read docs/RuntimeApiDesign.md for the current public API surface and naming conventions. This feature depends on the rules feature (docs/RulesDesign.md) being implemented first.

Summary of what editable fields are: a way to declare subsets of instance data fields as directly modifiable in specific states, without going through the event pipeline. The DSL syntax is `from <any|State1,State2> edit` with indented field names. The runtime exposes `engine.Update(instance, patch)` for host applications to modify editable fields with full type checking and rules enforcement.

DSL syntax: `from <any|StateA[,StateB...]> edit` followed by indented lines each containing a single field name. Block-form only, no inline syntax. Multi-state comma-separated lists are supported (same as transition blocks). `from any edit` makes fields editable in all states. Multiple edit blocks are additive — the effective editable set for a state is the union of all matching blocks.

Compiler validations: unknown field names in edit blocks are errors. Unknown state names are errors. Empty edit blocks (no field names) are parse errors. Duplicate field in same block is a warning. `edit` is a new reserved word.

Model: add `DslEditBlock(string FromState, IReadOnlyList<string> FieldNames, int SourceLine = 0)` record. Add `IReadOnlyList<DslEditBlock>? EditBlocks = null` to `DslWorkflowModel`. The parser expands `from any edit` into one `DslEditBlock` per declared state, and `from State1, State2 edit` into one per listed state (same expansion pattern as transitions).

Runtime API: add `Update(DslWorkflowInstance instance, Action<IUpdatePatchBuilder> patch)` method on `DslWorkflowEngine`, returning `DslUpdateResult`. The patch builder supports `Set` for scalars, `Add`/`Remove` for sets, `Enqueue`/`Dequeue` for queues, `Push`/`Pop` for stacks, `Replace` and `Clear` for all collections. Validation sequence: editability check (is field editable in current state?), type check (does value match declared type?), atomic mutation on working copy, rules evaluation (field rules, top-level rules, current-state rules), commit or rollback. Outcomes: `Updated` (success), `NotAllowed` (field not editable in current state), `Blocked` (rules violated), `Invalid` (type mismatch, unknown field, patch conflict). Patch conflicts detected at build time: duplicate Set on same scalar, Replace + granular op on same collection, Set on collection field, granular op on scalar field. Update never triggers state transitions. Update and Fire are independent. `DslUpdateResult` mirrors `DslFireResult` naming convention: `DslUpdateResult(UpdateOutcome Outcome, IReadOnlyList<string> Reasons, DslWorkflowInstance? UpdatedInstance)`.

Inspect integration: extend the aggregate `DslInspectionResult` with an `IReadOnlyList<DslEditableFieldInfo>? EditableFields = null` property. `DslEditableFieldInfo` has `FieldName`, `FieldType`, `IsNullable`, `CurrentValue`, `CollectionType`. The aggregate `engine.Inspect(instance)` now returns event statuses AND editable field info in one call. No separate `GetEditableFields` method — this is folded into the aggregate inspect result.

Language server: recognize `edit` after `from <states>` as alternative to `on <event>`. Parse indented field names. Validate field and state references. Suggest `edit` in completions after `from <states>`. Suggest field names inside edit block body. Highlight `edit` keyword and field references with semantic tokens.

Preview UI needs to be updated to support editing of fields inline in the data panel according to the engine defining which fields are editable for the current state. While editing, the rules should be checked and the input box highlighted red when rules are violated.

Tests: add comprehensive tests covering edit block parsing, multi-state edit blocks, `from any edit`, additive semantics across overlapping blocks, Update API with scalar fields, Update with collection fields (all types), editability enforcement per state, type checking, rules enforcement on update, atomic rollback on rule violation, patch conflict detection, inspect editable fields in aggregate result, and coexistence of edit blocks with event transitions.

Documentation: update docs/DesignNotes.md DSL Syntax Contract section to include edit block syntax. Update README.md DSL Syntax Reference, DSL Cookbook, and Status sections. Update docs/EditableFieldsDesign.md status from design phase to implemented.

Syntax highlighting grammar sync (non-negotiable — do not skip): update `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` for every new DSL construct introduced by this feature. Apply the Grammar Sync Checklist from `.github/copilot-instructions.md` in full. At minimum, the following changes are required for this feature:

1. **Declaration form** — add a `fromEditHeader` pattern matching `^(\s*)(from)(\s+)(any|StateList)(\s+)(edit)` so that `from` and `edit` are colored as control/action keywords and the state list gets entity coloring, consistent with the existing `fromOnHeader` pattern.
2. **Keyword** — add `edit` to the `controlKeywords` alternation so it is highlighted wherever it appears.
3. **Field references in edit block body** — field names indented under `from ... edit` are already caught by the `identifierReference` catch-all; no dedicated pattern is needed unless a more specific scope (e.g. `variable.other.editable-field`) is desired.
4. **Pattern ordering** — insert `fromEditHeader` into the top-level `patterns` array alongside (and at the same priority as) `fromOnHeader`, before `controlKeywords`.

Verify the grammar file is valid JSON after changes by parsing it. Confirm that `edit` does not accidentally color identifiers that happen to start with the substring — word-boundary anchors (`\b`) are required.

Intellisense sync (non-negotiable — do not skip): apply the Intellisense Sync Checklist from `.github/copilot-instructions.md` in full. At minimum, the following changes are required for this feature:

1. **`KeywordItems`** — add `edit` to `KeywordItems` in `SmDslAnalyzer.cs`.
2. **`KeywordTokens`** — add `edit` to `KeywordTokens` in `SmSemanticTokensHandler.cs`.
3. **Completion context for `from … edit` header** — add a regex branch in `GetCompletions` that detects `^\s*from\s+[^\n]+\s+edit(?:\s+[^\n]*)?$` and returns field name completions (the declared instance data fields are the valid completions inside an edit block body).
4. **Completion context for edit block body** — lines indented under `from … edit` contain bare field names; add a regex branch that detects this indented position and suggests declared data field names.
5. **Semantic token for edit header** — add a regex to `HighlightNamedSymbols` matching the `from … edit` header line and push the state list tokens as `type` and `edit` as `keyword`.
6. **`ExpressionLineRegex`** — edit block bodies do not contain expressions (only bare field names), so no update to `ExpressionLineRegex` is needed.

Build with dotnet build from repo root. Run tests in test/Precept.Tests/ and test/Precept.LanguageServer.Tests/. Make sure all existing tests still pass.
