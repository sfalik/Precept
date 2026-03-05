# Precept MCP Server Design

## Purpose

An MCP (Model Context Protocol) server that exposes DSL parsing, validation, structural analysis, and runtime execution as tools callable by Copilot (and any other MCP host). This enables semantic understanding of `.precept` files beyond what plain text reading provides.

## Project Location

```
tools/Precept.Mcp/
    Program.cs
    Tools/
        ValidateTool.cs
        DescribeTool.cs
        ReachabilityTool.cs
        ExecuteTool.cs
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

**Implementation:** `PreceptParser.Parse(text)` + `PreceptCompiler.Compile(model)`. Catches `InvalidOperationException` / `ArgumentException` thrown by the compiler and maps them to diagnostics using the existing `LineErrorRegex` already present in `SmDslAnalyzer`.

---

### 2. `precept_describe`

**Purpose:** Return the full structure of a machine as typed JSON — states, fields, events with their args, and the transition table. Lets Copilot reason about the machine's shape without re-parsing text.

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

### 3. `precept_reachability`

**Purpose:** Graph analysis of the state machine. Identifies structural problems that are valid DSL but semantically suspect.

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

### 4. `precept_execute`

**Purpose:** Execute a sequence of events against a machine starting from a given state and instance data snapshot. Returns step-by-step outcomes. Lets Copilot verify that a scenario it describes or edits actually works at runtime.

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

This makes all four tools available to Copilot in agent mode without any additional setup.

---

## Build Order

`Precept.Mcp.csproj` sits alongside the language server in `tools/` and is included in `Precept.slnx`. It depends only on `Precept.csproj` — not on the language server project.

---

## Not In Scope (First Version)

- Hot-reload / file watching (Copilot calls tools on demand)
- Multi-file / import resolution (not a DSL feature)
- Authentication / remote transport (stdio is sufficient for local VS Code use)
- A `precept_fix` tool (auto-correction is out of scope; validation + describe + execute provide enough signal for Copilot to self-correct)
