# Precept MCP Server Design

**Status:** Implemented (2026-03-06)

## Purpose

An MCP (Model Context Protocol) server that exposes DSL parsing, validation, structural analysis, and runtime execution as tools callable by Copilot (and any other MCP host). This enables semantic understanding of `.precept` files beyond what plain text reading provides.

## Project Location

```
tools/Precept.Mcp/
    Program.cs
    Tools/
        ValidateTool.cs
        SchemaTool.cs
        AuditTool.cs
        RunTool.cs
        LanguageTool.cs
        InspectTool.cs
    Precept.Mcp.csproj
```

References `src/Precept/Precept.csproj` directly — all parsing, compilation, and runtime execution reuse the existing implementation unchanged.

## SDK

[`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) — the official Microsoft C# MCP SDK. Exposes tools as attributed methods on a class; the SDK handles JSON-RPC transport over stdio.

```xml
<PackageReference Include="ModelContextProtocol" Version="0.1.*" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.*" />
```

Transport: **stdio** (default for local MCP servers launched by VS Code).

---

## Tools

### 1. `precept_validate`

**Purpose:** Parse and compile a `.precept` file. Returns structured diagnostics. This is the primary correctness gate — equivalent to reading the VS Code Problems panel but without requiring the extension to be running.

**Input:**
```json
{
  "path": "samples/bugtracker.precept"
}
```

**Output (success):**
```json
{
  "valid": true,
  "machineName": "BugTracker",
  "stateCount": 7,
  "eventCount": 9,
  "diagnostics": []
}
```

**Output (error):**
```json
{
  "valid": false,
  "diagnostics": [
    { "line": 12, "message": "compile-time rule violation: rule \"Priority must be between 1 and 5\" on field 'Priority' is violated by the field's default value." }
  ]
}
```

**Implementation:** `PreceptParser.Parse(text)` + `PreceptCompiler.Compile(model)`. Catches `InvalidOperationException` / `ArgumentException` thrown by the compiler and maps them to diagnostics using the existing `LineErrorRegex` already present in `PreceptAnalyzer`.

---

### 2. `precept_schema`

**Purpose:** Return the full structure of a precept as typed JSON — states, fields, events with their args, and the transition table. Lets Copilot reason about the precept's shape without re-parsing text.

**Input:**
```json
{
  "path": "samples/bugtracker.precept"
}
```

**Output:**
```json
{
  "name": "BugTracker",
  "initialState": "Triage",
  "states": [
    { "name": "Triage", "rules": [] },
    { "name": "Blocked", "rules": ["Must have an assignee while blocked", "Blocked state requires a block reason"] }
  ],
  "fields": [
    { "name": "Assignee", "type": "string", "nullable": true, "default": null },
    { "name": "Priority", "type": "number", "nullable": false, "default": 3 }
  ],
  "collectionFields": [
    { "name": "PendingSignatories", "kind": "set", "innerType": "string" }
  ],
  "events": [
    {
      "name": "Block",
      "args": [{ "name": "Reason", "type": "string", "nullable": false, "required": true }]
    }
  ],
  "transitions": [
    { "from": "InProgress", "on": "Block", "branches": ["if → Blocked", "else → reject"] }
  ]
}
```

**Implementation:** Walks `DslWorkflowModel` records directly (`DslState`, `DslField`, `DslCollectionField`, `DslEvent`, `DslTransition`) — no runtime needed.

---

### 3. `precept_audit`

**Purpose:** Graph analysis of the precept. Identifies structural problems that are valid DSL but semantically suspect.

**Input:**
```json
{
  "path": "samples/bugtracker.precept"
}
```

**Output:**
```json
{
  "allStates": ["Triage", "Open", "InProgress", "Blocked", "InReview", "Resolved", "Closed"],
  "reachableStates": ["Triage", "Open", "InProgress", "Blocked", "InReview", "Resolved", "Closed"],
  "unreachableStates": [],
  "terminalStates": ["Closed"],
  "deadEndStates": [],
  "orphanedEvents": [],
  "warnings": []
}
```

Fields:
- **`unreachableStates`** — states that cannot be reached from the initial state by any event sequence.
- **`terminalStates`** — states with no outgoing transitions (natural end states).
- **`deadEndStates`** — non-terminal states where all outgoing transitions only `no transition` or `reject` (no path forward).
- **`orphanedEvents`** — events declared but referenced in zero `from … on` blocks.
- **`warnings`** — human-readable descriptions of any of the above when non-empty.

**Implementation:** Build a directed graph from `DslWorkflowModel.Transitions` where edges are `(FromState → TargetState)` for all `DslStateTransition` outcomes. Run BFS from `DslWorkflowModel.InitialState`. Compare `DslWorkflowModel.States` against visited set. Detect orphaned events by diffing `DslWorkflowModel.Events` against `DslWorkflowModel.Transitions`.

---

### 4. `precept_run`

**Purpose:** Execute a sequence of events against a precept starting from a given state and instance data snapshot. Returns step-by-step outcomes. Lets Copilot verify that a scenario it describes or edits actually works at runtime.

**Input:**
```json
{
  "path": "samples/bugtracker.precept",
  "initialData": {
    "Assignee": null,
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "steps": [
    { "event": "Assign", "args": { "User": "alice" } },
    { "event": "StartWork" },
    { "event": "Block", "args": { "Reason": "Waiting on infra" } },
    { "event": "Unblock" },
    { "event": "SubmitReview" },
    { "event": "Approve" }
  ]
}
```

**Output:**
```json
{
  "steps": [
    { "step": 1, "event": "Assign",       "outcome": "AcceptedInPlace", "state": "Open",       "data": { "Assignee": "alice", ... } },
    { "step": 2, "event": "StartWork",    "outcome": "Accepted",        "state": "InProgress",  "data": { ... } },
    { "step": 3, "event": "Block",        "outcome": "Accepted",        "state": "Blocked",     "data": { "BlockReason": "Waiting on infra", ... } },
    { "step": 4, "event": "Unblock",      "outcome": "Accepted",        "state": "InProgress",  "data": { "BlockReason": null, ... } },
    { "step": 5, "event": "SubmitReview", "outcome": "Accepted",        "state": "InReview",    "data": { ... } },
    { "step": 6, "event": "Approve",      "outcome": "Accepted",        "state": "Resolved",    "data": { "Resolution": "Approved", ... } }
  ],
  "finalState": "Resolved",
  "finalData": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": "Approved" },
  "abortedAt": null
}
```

If a step fails (Rejected, NotDefined, NotApplicable), execution stops and `abortedAt` is set to that step number with an explanation.

**Implementation:** `PreceptEngine.CreateInstance(initialData)` then `engine.Fire(instance, event, args)` in a loop, threading the updated `DslWorkflowInstance` through each step. Maps `DslFireResult` outcome kinds to outcome strings.

---

### 5. `precept_language`

**Purpose:** Return a complete, structured reference for the Precept DSL — vocabulary, construct forms, semantic constraints, expression scoping rules, fire pipeline stages, and outcome kinds. Enables Copilot to write semantically correct `.precept` files without relying on trial-and-error against `precept_validate`.

Unlike the other tools (which operate on a specific `.precept` file), this tool takes no input — it describes the language itself.

**Input:**
```json
{}
```

**Output:**
```json
{
  "vocabulary": {
    "controlKeywords": ["precept", "state", "initial", "from", "on", "when", "any", "in", "to", "of", "with"],
    "actionKeywords": ["set", "add", "remove", "enqueue", "dequeue", "push", "pop", "clear", "into", "transition", "no", "reject"],
    "declarationKeywords": ["field", "as", "nullable", "default", "invariant", "because", "event", "assert"],
    "typeKeywords": ["string", "number", "boolean"],
    "literalKeywords": ["true", "false", "null"],
    "operators": [
      { "symbol": "||", "precedence": 1, "description": "Logical OR" },
      { "symbol": "&&", "precedence": 2, "description": "Logical AND" },
      { "symbol": "==", "precedence": 3, "description": "Equality" },
      { "symbol": "!=", "precedence": 3, "description": "Inequality" },
      { "symbol": ">", "precedence": 4, "description": "Greater than" },
      { "symbol": ">=", "precedence": 4, "description": "Greater than or equal" },
      { "symbol": "<", "precedence": 4, "description": "Less than" },
      { "symbol": "<=", "precedence": 4, "description": "Less than or equal" },
      { "symbol": "contains", "precedence": 4, "description": "Collection membership test" },
      { "symbol": "!", "precedence": 5, "arity": "unary", "description": "Logical NOT" }
    ]
  },
  "constructs": [
    { "form": "precept <Name>", "context": "top-level", "description": "Top-level declaration. Exactly one per file.", "example": "precept BankLoan" },
    { "form": "field <Name> as <Type> [nullable] [default <Value>]", "context": "top-level", "description": "Scalar data field declaration.", "example": "field Priority as number default 3" },
    { "form": "field <Name> as <set|queue|stack> of <Type>", "context": "top-level", "description": "Collection field declaration. Always starts empty.", "example": "field Tags as set of string" },
    { "form": "invariant <Expr> because \"<Reason>\"", "context": "top-level", "description": "Data truth — must hold after every mutation.", "example": "invariant Priority >= 1 because \"Priority must be positive\"" },
    { "form": "state <Name> [initial]", "context": "top-level", "description": "State declaration. Exactly one must be initial.", "example": "state Review" },
    { "form": "in <State> assert <Expr> because \"<Reason>\"", "context": "top-level", "description": "Must hold while in the state (entry + in-place).", "example": "in Active assert Assignee != null because \"Active requires assignee\"" },
    { "form": "to <State> assert <Expr> because \"<Reason>\"", "context": "top-level", "description": "Must hold when entering via cross-state transition.", "example": "to Review assert Reviewer != null because \"Review requires reviewer\"" },
    { "form": "from <State> assert <Expr> because \"<Reason>\"", "context": "top-level", "description": "Must hold when leaving the state.", "example": "from Draft assert Title != null because \"Cannot leave Draft without title\"" },
    { "form": "to <State> -> <Actions>", "context": "top-level", "description": "Automatic entry actions for a state.", "example": "to Review -> set ReviewStarted = true" },
    { "form": "from <State> -> <Actions>", "context": "top-level", "description": "Automatic exit actions for a state.", "example": "from Active -> set ActiveTime = ActiveTime + 1" },
    { "form": "event <Name> [with <ArgName> as <Type> [nullable] [default <Value>], ...]", "context": "top-level", "description": "Event declaration with optional inline arguments.", "example": "event Submit with token as string, priority as number default 3" },
    { "form": "on <Event> assert <Expr> because \"<Reason>\"", "context": "top-level", "description": "Event argument validation — checked before any transition.", "example": "on Submit assert token != null because \"Token is required\"" },
    { "form": "from <State|any> on <Event> [when <Guard>] -> <Actions> -> <Outcome>", "context": "top-level", "description": "Transition row. Multiple rows per (state,event) pair use first-match evaluation.", "example": "from Open on Submit when Priority > 3 -> set Urgent = true -> transition Review" }
  ],
  "constraints": [
    { "id": "C1", "phase": "parse", "rule": "Exactly one 'precept' declaration per file." },
    { "id": "C2", "phase": "parse", "rule": "Exactly one state must be marked 'initial'." },
    { "id": "C3", "phase": "parse", "rule": "No duplicate state names." },
    { "id": "C4", "phase": "parse", "rule": "No duplicate event names." },
    { "id": "C5", "phase": "parse", "rule": "No duplicate field names (scalar or collection, across both)." },
    { "id": "C6", "phase": "parse", "rule": "No duplicate event argument names within an event." },
    { "id": "C7", "phase": "parse", "rule": "Non-nullable fields without 'default' are a parse error." },
    { "id": "C8", "phase": "parse", "rule": "At least one state must be declared." },
    { "id": "C9", "phase": "parse", "rule": "Each (state, event) pair may only appear in transition rows that share compatible guards — no duplicate unguarded rows." },
    { "id": "C10", "phase": "compile", "rule": "A field's default value must satisfy all invariants that reference that field." },
    { "id": "C11", "phase": "compile", "rule": "Initial state entry asserts ('in' and 'to') must pass against default data." },
    { "id": "C12", "phase": "compile", "rule": "A literal 'set' assignment in a transition row must not violate invariants." },
    { "id": "C13", "phase": "runtime", "rule": "Event asserts are evaluated first (Stage 1). Failure → Rejected." },
    { "id": "C14", "phase": "runtime", "rule": "Transition rows are evaluated in source order; first 'when' guard that is true (or absent) wins (Stage 2). No match → NotApplicable." },
    { "id": "C15", "phase": "runtime", "rule": "Exit actions run before row mutations, which run before entry actions (Stage 3→4→5)." },
    { "id": "C16", "phase": "runtime", "rule": "Invariants are checked after all mutations commit. Failure → full rollback, Rejected." },
    { "id": "C17", "phase": "runtime", "rule": "State asserts ('in'/'to'/'from') are checked with correct temporal scoping after mutations. Failure → full rollback, Rejected." },
    { "id": "C18", "phase": "runtime", "rule": "'dequeue' from empty queue or 'pop' from empty stack → Rejected." },
    { "id": "C19", "phase": "runtime", "rule": "Non-nullable event args without a default are required — caller must supply them." },
    { "id": "C20", "phase": "runtime", "rule": "'set' assignments execute in declaration order with read-your-writes semantics." }
  ],
  "expressionScopes": [
    { "position": "invariant expression", "allowed": "All data fields, collection accessors" },
    { "position": "state assert expression", "allowed": "All data fields, collection accessors" },
    { "position": "event assert expression", "allowed": "That event's args only (bare ArgName or EventName.ArgName)" },
    { "position": "when guard", "allowed": "All data fields, EventName.ArgName, collection accessors" },
    { "position": "set RHS", "allowed": "All data fields (read-your-writes), EventName.ArgName, collection accessors" }
  ],
  "firePipeline": [
    { "stage": 1, "name": "Event asserts", "description": "Validate event args against 'on <Event> assert' rules. Failure → Rejected." },
    { "stage": 2, "name": "Row selection", "description": "Iterate transition rows for (state, event) in source order. First 'when' match wins. No match → NotApplicable." },
    { "stage": 3, "name": "Exit actions", "description": "Run 'from <SourceState> ->' automatic mutations." },
    { "stage": 4, "name": "Row mutations", "description": "Execute the matched row's '-> set/add/remove/...' action chain in declaration order." },
    { "stage": 5, "name": "Entry actions", "description": "Run 'to <TargetState> ->' automatic mutations." },
    { "stage": 6, "name": "Validation", "description": "Check invariants, state asserts (in/to/from with temporal scoping). Any failure → full rollback, Rejected." }
  ],
  "outcomeKinds": [
    { "kind": "Accepted", "description": "Event handled, state changed.", "mutated": true },
    { "kind": "AcceptedInPlace", "description": "Event handled via 'no transition', data may change but state stays.", "mutated": true },
    { "kind": "Rejected", "description": "Event matched but blocked (assert failure, invariant violation, empty collection op, reject outcome).", "mutated": false },
    { "kind": "NotDefined", "description": "No transition rows exist for this event in the current state.", "mutated": false },
    { "kind": "NotApplicable", "description": "Transition rows exist but no 'when' guard matched.", "mutated": false }
  ]
}
```

#### Data Sources (Three Tiers)

The response is assembled from three core infrastructure components, all defined in `src/Precept/` and used by the parser, language server, and error reporting — not just by MCP. See `docs/PreceptLanguageImplementationPlan.md` for the full design of each.

| Tier | Content | Source | Drift Risk |
|---|---|---|---|
| **1. Vocabulary** | Keywords, operators, types | `PreceptToken` enum with `[TokenCategory]` and `[TokenDescription]` attributes — reflected at runtime | **Zero** — the enum IS the parser's token set |
| **2. Constructs** | Statement forms, examples | `ConstructCatalog` — parser combinators registered with syntax templates, descriptions, and parseable examples | **Near-zero** — co-located with parser; examples validated by tests |
| **3. Semantics** | Constraints, scopes, pipeline, outcomes | `ConstraintCatalog` — each constraint is a record with ID, phase, and description, co-located with enforcement code | **Near-zero** — constraint descriptions serve as error messages; tests verify enforcement |

#### Implementation

The tool serializes `ConstructCatalog.Constructs` + `ConstraintCatalog.Constraints` + reflected `PreceptToken` vocabulary into the JSON response. No MCP-specific data files — everything comes from core infrastructure that also powers parser error messages, language server hovers, and completions.

---

### 6. `precept_inspect`

**Purpose:** From a given state and data snapshot, evaluate all declared events and report what each would do — without mutating anything. Lets Copilot explore the precept interactively ("what can happen from here?") instead of guessing event sequences for `precept_run`.

**Input:**
```json
{
  "path": "samples/bugtracker.precept",
  "currentState": "InProgress",
  "data": {
    "Assignee": "alice",
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "eventArgs": {
    "Block": { "Reason": "Waiting on infra" },
    "Reassign": { "User": "bob" }
  }
}
```

The `eventArgs` field is optional. When provided, the specified args are used for the named events during evaluation. Events not listed in `eventArgs` are evaluated with their default/null args (events whose required args are missing report `"requiresArgs": true` instead of a misleading `Rejected`).

**Output:**
```json
{
  "currentState": "InProgress",
  "events": [
    {
      "event": "Block",
      "outcome": "Accepted",
      "resultState": "Blocked",
      "resultData": { "Assignee": "alice", "Priority": 3, "BlockReason": "Waiting on infra", "Resolution": null }
    },
    {
      "event": "Reassign",
      "outcome": "AcceptedInPlace",
      "resultState": "InProgress",
      "resultData": { "Assignee": "bob", "Priority": 3, "BlockReason": null, "Resolution": null }
    },
    {
      "event": "SubmitReview",
      "outcome": "Accepted",
      "resultState": "InReview",
      "resultData": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null }
    },
    {
      "event": "Approve",
      "outcome": "NotDefined",
      "reason": "No transition from InProgress on Approve"
    },
    {
      "event": "Close",
      "outcome": "NotDefined",
      "reason": "No transition from InProgress on Close"
    },
    {
      "event": "Escalate",
      "requiresArgs": true,
      "requiredArgs": [{ "name": "Level", "type": "number" }],
      "note": "Supply args via eventArgs to see the full outcome"
    }
  ]
}
```

Events are grouped implicitly: actionable events (Accepted/AcceptedInPlace) first, then unavailable (NotDefined/NotApplicable/Rejected), then those needing args. This ordering is a presentation convenience — the JSON array is sorted by outcome kind.

**Implementation:** `PreceptEngine.CreateInstance(state, data)` then `engine.Inspect(instance)` — which internally calls `engine.Fire()` per event in a read-only snapshot mode. For events with caller-supplied args (from `eventArgs`), those args are passed. For events with required args not supplied, report `requiresArgs` instead of calling Fire. Maps `DslInspectionResult` entries to the output format.

---

## Registration in VS Code

`tools/Precept.VsCode/package.json` would gain an MCP server entry:

```json
"mcpServers": {
  "precept": {
    "command": "dotnet",
    "args": ["run", "--project", "${workspaceFolder}/tools/Precept.Mcp"],
    "type": "stdio"
  }
}
```

This makes all six tools available to Copilot in agent mode without any additional setup.

---

## Build Order

`Precept.Mcp.csproj` sits alongside the language server in `tools/` and is included in `Precept.slnx`. It depends only on `Precept.csproj` — not on the language server project.

---

## Not In Scope (First Version)

- Hot-reload / file watching (Copilot calls tools on demand)
- Multi-file / import resolution (not a DSL feature)
- Authentication / remote transport (stdio is sufficient for local VS Code use)
- A `precept_fix` tool (auto-correction is out of scope; validation + schema + run + inspect provide enough signal for Copilot to self-correct)
