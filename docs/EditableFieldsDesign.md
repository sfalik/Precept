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

## Runtime API

### `Update(patch)`

A single new method on the workflow instance:

```csharp
DslInstanceUpdateResult Update(Action<IUpdatePatchBuilder> patch)
```

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
var result = instance.Update(patch => patch
    .Set("Notes", "Customer called back — issue is intermittent")
    .Set("Priority", 1)
    .Add("Tags", "urgent")
    .Remove("Tags", "low-priority")
);

if (result.Outcome == UpdateOutcome.Updated)
{
    // Commit persisted instance
}
```

### Validation sequence

When `Update` is called, the runtime executes the following steps in order:

1. **Editability check** — For each field in the patch, verify the field is editable in the current state (union of matching `from ... edit` blocks). If any field is not editable, the entire update is rejected with `NotAllowed` outcome. No partial application.

2. **Type check** — Verify each value matches the declared field type. Scalar type mismatches, null on non-nullable fields, and wrong-typed collection elements are rejected.

3. **Atomic mutation** — Apply all patch operations to a working copy of instance data, in declaration order within the patch. This is the same working-copy pattern used by `set` assignments in transitions.

4. **Rules evaluation** — Evaluate field rules, top-level rules, and current-state entry rules against the post-mutation working copy. If any rule fails, all mutations are rolled back and the outcome is `Blocked` with violated rule reasons.

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
public record DslInstanceUpdateResult(
    UpdateOutcome Outcome,
    IReadOnlyList<string> Reasons,
    DslWorkflowInstance? UpdatedInstance  // null when not Updated
);
```

`Updated` is a new outcome kind — distinct from `Enabled` (which implies a lifecycle event was processed). The caller sees "data was edited" vs. "an event was fired" as different categories.

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

### Extended Inspect response

`Inspect` is extended to include editable field information alongside event/transition status. No new `GetEditable` endpoint — the existing inspect call returns everything the UI needs.

The inspect response includes an `EditableFields` collection for the current state:

```csharp
public record DslEditableFieldInfo(
    string FieldName,
    string FieldType,        // "string", "number", "boolean", "set<string>", etc.
    bool IsNullable,
    object? CurrentValue,    // Current field value (scalar or serialized collection)
    string? CollectionType   // null for scalars; "set", "queue", "stack" for collections
);
```

This gives the host application (or preview UI) enough information to render an edit form pre-populated with current values, type-appropriate input controls, and editability scope.

### Inspect does not simulate Update

Unlike event inspection (which simulates guard evaluation and set assignments), inspect does not simulate update operations. It reports *what is editable* — the host decides what to change and calls `Update` with a concrete patch.

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
var machine = StateMachineDslParser.Parse(dslText);
var definition = DslWorkflowCompiler.Compile(machine);
var instance = definition.CreateInstance();

// Lifecycle: assign a technician (event pipeline)
var fireResult = instance.Fire("Assign", new Dictionary<string, object?>
{
    ["Technician"] = "Alice"
});
// fireResult.Outcome == Enabled, state => InProgress

// Data editing: update notes and priority (no event needed)
var editResult = instance.Update(patch => patch
    .Set("Notes", "Customer prefers morning appointments")
    .Set("Priority", 1)
    .Add("Tags", "urgent")
);
// editResult.Outcome == Updated, state unchanged (InProgress)

// Inspect: see what's editable, what events are available
var inspection = instance.Inspect();
// inspection.EditableFields => [Notes, Priority, Tags, Description, EstimatedHours, AssignedTo]
// inspection.Events => [Resolve] (with guard status)

// Rules enforcement: priority out of range
var badEdit = instance.Update(patch => patch
    .Set("Priority", 99)
);
// badEdit.Outcome == Blocked
// badEdit.Reasons => ["Priority must be between 1 and 5"]

// Not editable in current state
var wrongState = instance.Update(patch => patch
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

## Language Server Impact

### Parser

- Recognize `edit` after `from <states>` as an alternative to `on <event>`.
- Parse indented field names as the edit block body.
- Report errors for unknown fields, unknown states, empty blocks.

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

Implement the editable fields feature for the state machine DSL as specified in docs/EditableFieldsDesign.md. This is a full-stack implementation across parser, model, compiler, runtime, language server, and documentation. Read docs/EditableFieldsDesign.md thoroughly before starting — it is the complete design spec. This feature depends on the rules feature (docs/RulesDesign.md) being implemented first.

Summary of what editable fields are: a way to declare subsets of instance data fields as directly modifiable in specific states, without going through the event pipeline. The DSL syntax is `from <any|State1,State2> edit` with indented field names. The runtime exposes `Update(patch)` for host applications to modify editable fields with full type checking and rules enforcement.

DSL syntax: `from <any|StateA[,StateB...]> edit` followed by indented lines each containing a single field name. Block-form only, no inline syntax. Multi-state comma-separated lists are supported (same as transition blocks). `from any edit` makes fields editable in all states. Multiple edit blocks are additive — the effective editable set for a state is the union of all matching blocks.

Compiler validations: unknown field names in edit blocks are errors. Unknown state names are errors. Empty edit blocks (no field names) are parse errors. Duplicate field in same block is a warning. `edit` is a new reserved word.

Runtime API: add `Update(Action<IUpdatePatchBuilder> patch)` method to DslWorkflowDefinition or the instance type. The patch builder supports `Set` for scalars, `Add`/`Remove` for sets, `Enqueue`/`Dequeue` for queues, `Push`/`Pop` for stacks, `Replace` and `Clear` for all collections. Validation sequence: editability check (is field editable in current state?), type check (does value match declared type?), atomic mutation on working copy, rules evaluation (field rules, top-level rules, current-state entry rules), commit or rollback. Outcomes: `Updated` (success), `NotAllowed` (field not editable in current state), `Blocked` (rules violated), `Invalid` (type mismatch, unknown field, patch conflict). Patch conflicts detected at build time: duplicate Set on same scalar, Replace + granular op on same collection, Set on collection field, granular op on scalar field. Update never triggers state transitions. Update and Fire are independent.

Inspect integration: extend the existing Inspect response to include an `EditableFields` collection with field name, field type, nullability, current value, and collection type info. No new GetEditable endpoint.

Language server: recognize `edit` after `from <states>` as alternative to `on <event>`. Parse indented field names. Validate field and state references. Suggest `edit` in completions after `from <states>`. Suggest field names inside edit block body. Highlight `edit` keyword and field references with semantic tokens.

Tests: add comprehensive tests covering edit block parsing, multi-state edit blocks, `from any edit`, additive semantics across overlapping blocks, Update API with scalar fields, Update with collection fields (all types), editability enforcement per state, type checking, rules enforcement on update, atomic rollback on rule violation, patch conflict detection, inspect editable fields response, and coexistence of edit blocks with event transitions.

Documentation: update docs/DesignNotes.md DSL Syntax Contract section to include edit block syntax. Update README.md DSL Syntax Reference, DSL Cookbook, and Status sections. Update docs/EditableFieldsDesign.md status from design phase to implemented.

Build with dotnet build from repo root. Run tests in test/StateMachine.Tests/ and test/StateMachine.Dsl.LanguageServer.Tests/. Make sure all existing tests still pass.
