# Precept MCP Server Design

**Status:** Implemented (2026-03-06); packaging and Copilot workflow design updated (2026-03-22)

## Purpose

An MCP (Model Context Protocol) server that exposes DSL parsing, validation, structural analysis, and runtime execution as tools callable by Copilot (and any other MCP host). This enables semantic understanding of `.precept` files beyond what plain text reading provides.

This design also defines the distribution strategy for the MCP server and companion Copilot customizations (agent + skills). The delivery is split across two vehicles:

- **VS Code extension** — editor features only (language server, syntax highlighting, preview panel, commands)
- **Agent plugin** — Copilot features only (MCP server, custom agent, skills)

This split reflects the principle that the MCP server and Copilot customizations exist solely for AI consumption and have no reason to live in the editor extension.

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

**Purpose:** Parse and validate a `.precept` file. Returns structured diagnostics. This is the primary correctness gate — equivalent to reading the VS Code Problems panel but without requiring the extension to be running.

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
    { "line": 12, "message": "set target 'Value' type mismatch: expected number but expression produces number|null.", "code": "PRECEPT042" },
    { "line": 12, "message": "unknown identifier 'Missing'.", "code": "PRECEPT038" }
  ]
}
```

**Implementation:** `PreceptParser.ParseWithDiagnostics(text)` for syntax/shape errors, then `PreceptCompiler.Validate(model)` for shared compile-phase type diagnostics (`PRECEPT038`-`PRECEPT043`). If shared validation succeeds, the tool still calls `PreceptCompiler.Compile(model)` to surface remaining compile-time runtime checks that are not part of the shared type checker. The result preserves diagnostic codes structurally via `{ line, message, code }` entries and returns all shared type diagnostics instead of stopping at the first error.

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
    { "step": 1, "event": "Assign",       "outcome": "NoTransition",    "state": "Open",       "data": { "Assignee": "alice", ... } },
    { "step": 2, "event": "StartWork",    "outcome": "Transition",      "state": "InProgress",  "data": { ... } },
    { "step": 3, "event": "Block",        "outcome": "Transition",      "state": "Blocked",     "data": { "BlockReason": "Waiting on infra", ... } },
    { "step": 4, "event": "Unblock",      "outcome": "Transition",      "state": "InProgress",  "data": { "BlockReason": null, ... } },
    { "step": 5, "event": "SubmitReview", "outcome": "Transition",      "state": "InReview",    "data": { ... } },
    { "step": 6, "event": "Approve",      "outcome": "Transition",      "state": "Resolved",    "data": { "Resolution": "Approved", ... } }
  ],
  "finalState": "Resolved",
  "finalData": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": "Approved" },
  "abortedAt": null
}
```

If a step fails (Rejected, ConstraintFailure, Undefined, Unmatched), execution stops and `abortedAt` is set to that step number with an explanation.

**Implementation:** `PreceptEngine.CreateInstance(initialData)` then `engine.Fire(instance, event, args)` in a loop, threading the updated instance through each step. Maps `FireResult` outcome kinds to outcome strings.

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
    { "id": "C14", "phase": "runtime", "rule": "Transition rows are evaluated in source order; first 'when' guard that is true (or absent) wins (Stage 2). No match → Unmatched." },
    { "id": "C15", "phase": "runtime", "rule": "Exit actions run before row mutations, which run before entry actions (Stage 3→4→5)." },
    { "id": "C16", "phase": "runtime", "rule": "Invariants are checked after all mutations commit. Failure → full rollback, ConstraintFailure." },
    { "id": "C17", "phase": "runtime", "rule": "State asserts ('in'/'to'/'from') are checked with correct temporal scoping after mutations. Failure → full rollback, ConstraintFailure." },
    { "id": "C18", "phase": "runtime", "rule": "'dequeue' from empty queue or 'pop' from empty stack → ConstraintFailure." },
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
    { "stage": 2, "name": "Row selection", "description": "Iterate transition rows for (state, event) in source order. First 'when' match wins. No match → Unmatched." },
    { "stage": 3, "name": "Exit actions", "description": "Run 'from <SourceState> ->' automatic mutations." },
    { "stage": 4, "name": "Row mutations", "description": "Execute the matched row's '-> set/add/remove/...' action chain in declaration order." },
    { "stage": 5, "name": "Entry actions", "description": "Run 'to <TargetState> ->' automatic mutations." },
    { "stage": 6, "name": "Constraint evaluation", "description": "Check invariants, state asserts (in/to/from with temporal scoping). Any failure → full rollback, ConstraintFailure." }
  ],
  "outcomeKinds": [
    { "kind": "Transition", "description": "Event handled, state changed.", "mutated": true },
    { "kind": "NoTransition", "description": "Event handled via 'no transition', data may change but state stays.", "mutated": true },
    { "kind": "Rejected", "description": "Author's explicit reject outcome.", "mutated": false },
    { "kind": "ConstraintFailure", "description": "Event matched but blocked by constraint violation (invariant, assert, empty collection op).", "mutated": false },
    { "kind": "Unmatched", "description": "Transition rows exist but no 'when' guard matched.", "mutated": false },
    { "kind": "Undefined", "description": "No transition rows exist for this event in the current state.", "mutated": false }
  ]
}
```

#### Data Sources (Three Tiers)

The response is assembled from three core infrastructure components, all defined in `src/Precept/` and used by the parser, language server, and error reporting — not just by MCP. See `docs/PreceptLanguageImplementationPlan.md` for the full design of each.

| Tier | Content | Source | Drift Risk |
|---|---|---|---|
| **1. Vocabulary** | Keywords, operators, types | `PreceptToken` enum with `[TokenCategory]` and `[TokenDescription]` attributes — reflected at runtime | **Zero** — the enum IS the parser's token set |
| **2. Constructs** | Statement forms, examples | `ConstructCatalog` — parser combinators registered with syntax templates, descriptions, and parseable examples | **Near-zero** — co-located with parser; examples validated by tests |
| **3. Semantics** | Constraints, scopes, pipeline, outcomes | `DiagnosticCatalog` — each diagnostic is a record with ID, phase, and description, co-located with enforcement code | **Near-zero** — diagnostic descriptions serve as error messages; tests verify enforcement |

#### Implementation

The tool serializes `ConstructCatalog.Constructs` + `DiagnosticCatalog.Diagnostics` + reflected `PreceptToken` vocabulary into the JSON response. No MCP-specific data files — everything comes from core infrastructure that also powers parser error messages, language server hovers, and completions.

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
      "outcome": "Transition",
      "resultState": "Blocked",
      "resultData": { "Assignee": "alice", "Priority": 3, "BlockReason": "Waiting on infra", "Resolution": null }
    },
    {
      "event": "Reassign",
      "outcome": "NoTransition",
      "resultState": "InProgress",
      "resultData": { "Assignee": "bob", "Priority": 3, "BlockReason": null, "Resolution": null }
    },
    {
      "event": "SubmitReview",
      "outcome": "Transition",
      "resultState": "InReview",
      "resultData": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null }
    },
    {
      "event": "Approve",
      "outcome": "Undefined",
      "reason": "No transition from InProgress on Approve"
    },
    {
      "event": "Close",
      "outcome": "Undefined",
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

Events are grouped implicitly: actionable events (Transition/NoTransition) first, then unavailable (Undefined/Unmatched/Rejected/ConstraintFailure), then those needing args. This ordering is a presentation convenience — the JSON array is sorted by outcome kind.

**Implementation:** `PreceptEngine.CreateInstance(state, data)` then `engine.Inspect(instance)` — which internally calls `engine.Fire()` per event in a read-only snapshot mode. For events with caller-supplied args (from `eventArgs`), those args are passed. For events with required args not supplied, report `requiresArgs` instead of calling Fire. Maps `InspectionResult` entries to the output format.

---

## Agent Plugin Distribution

The MCP server, custom agent, and skills are distributed as a **Copilot agent plugin** — a self-contained bundle that users install once and that works across any workspace. Agent plugins are a Preview feature (`chat.plugins.enabled`) that bundle any combination of agents, skills, MCP servers, hooks, and slash commands into a single installable unit.

This replaces the earlier design where the VS Code extension bundled the MCP server via `registerMcpServerDefinitionProvider()` and scaffolded skills into `.github/skills/`. The plugin model eliminates the scaffolding problem entirely — Copilot discovers plugin-provided agents, skills, and MCP servers automatically without workspace file creation.

### Why Plugin Instead of Extension

| Concern | Extension | Plugin |
|---|---|---|
| MCP server registration | `registerMcpServerDefinitionProvider()` | `.mcp.json` at plugin root |
| Skills | No native API; must scaffold into workspace | Discovered automatically from plugin `skills/` |
| Agents | No native API; must scaffold into workspace | Discovered automatically from plugin `agents/` |
| Trust model | Implicit (extension is trusted) | Implicit on install (plugin MCP servers are implicitly trusted) |
| Update mechanism | VS Code extension marketplace updates | Plugin marketplace updates (auto-checked every 24h) |
| User setup | Zero (extension activation) | One-time install from marketplace or Git URL |
| Works in any workspace | Yes (extension is global) | Yes (plugin is global once installed) |

The VS Code extension continues to provide all editor features: language server (diagnostics, completions, semantic tokens, code actions), syntax highlighting (TextMate grammar), preview panel, and commands. It no longer carries MCP server binaries or Copilot customization content.

### Plugin Directory Structure

```
tools/Precept.Plugin/
├── .github/plugin/
│   └── plugin.json           # Plugin metadata and configuration
├── agents/
│   └── precept-author.md     # Precept Author custom agent
├── skills/
│   ├── precept-authoring/
│   │   └── SKILL.md          # Authoring workflow skill
│   └── precept-debugging/
│       └── SKILL.md          # Debugging/diagnosis skill
├── .mcp.json                 # MCP server definition (dev: launcher, dist: dotnet tool)
└── README.md
```

### plugin.json

```json
{
  "name": "precept",
  "description": "Precept DSL authoring, validation, and debugging tools for GitHub Copilot.",
  "version": "1.0.0",
  "author": { "name": "Precept" },
  "repository": "https://github.com/<org>/precept-plugin",
  "license": "MIT",
  "keywords": ["precept", "dsl", "state-machine", "workflow", "domain-integrity"],
  "agents": ["./agents"],
  "skills": [
    "./skills/precept-authoring",
    "./skills/precept-debugging"
  ]
}
```

### .mcp.json (Distribution)

```json
{
  "mcpServers": {
    "precept": {
      "command": "dotnet",
      "args": ["tool", "run", "precept-mcp"],
      "env": {}
    }
  }
}
```

The distributed plugin uses `dotnet tool run precept-mcp` to launch the MCP server. This requires .NET on the user's machine but eliminates per-platform binary bundling entirely — the dotnet tool restores and runs the correct binary automatically. End users must have the .NET SDK installed (same prerequisite as using the Precept NuGet package).

### .mcp.json (Development)

During development in this repo, the plugin's `.mcp.json` uses a launcher script instead:

```json
{
  "mcpServers": {
    "precept": {
      "command": "node",
      "args": ["../../scripts/start-precept-mcp.js"]
    }
  }
}
```

The launcher (`tools/scripts/start-precept-mcp.js`) builds from source, shadow-copies the output, and runs the copy — preventing file locking during rebuilds. At publish time, CI rewrites the `.mcp.json` to the `dotnet tool run` form. The launcher script is not included in the published plugin.

### Distribution Channels

The plugin can be distributed through multiple channels:

1. **Marketplace listing** — submit to `github/awesome-copilot` or a dedicated marketplace repo. Users discover and install via the Extensions view (`@agentPlugins`).
2. **Direct Git install** — users run `Chat: Install Plugin From Source` with the plugin repo URL. No marketplace needed.
3. **Workspace recommendation** — repos using Precept can recommend the plugin via workspace settings:

```json
{
  "enabledPlugins": {
    "precept@awesome-copilot": true
  }
}
```

4. **Local development** — register the plugin directory via `chat.pluginLocations` setting during development:

```json
{
  "chat.pluginLocations": {
    "/path/to/precept-plugin": true
  }
}
```

### Developer Inner Loop

This repo produces two VS Code artifacts: the **extension** (editor features) and the **agent plugin** (Copilot features). Both are developed locally from `tools/` and follow the same edit → build → reload cycle.

#### Prerequisites

Enable the agent plugins preview in your user settings:

```json
{ "chat.plugins.enabled": true }
```

#### Tasks

All dev tasks are in `.vscode/tasks.json`, runnable via **Tasks: Run Task**:

| Task | What it does |
|------|-------------|
| `build` | Builds the language server to `temp/dev-language-server/` |
| `extension: install` | Builds + installs the extension from `tools/Precept.VsCode/` |
| `extension: uninstall` | Uninstalls the local extension |
| `plugin: enable` | Registers `tools/Precept.Plugin/` in workspace `chat.pluginLocations` |
| `plugin: disable` | Unregisters the plugin from `chat.pluginLocations` |

#### Edit → Test cycle

**Extension changes** (language server, syntax, preview, commands):

1. For C# changes: run `Build Task` / `Ctrl+Shift+B`. The extension detects the new DLL, shadow-copies it, and restarts automatically.
2. For TypeScript/webview changes: run task `extension: install`, then `Developer: Reload Window`.

**Plugin changes** (agent, skills, MCP tools):

1. For agent/skill markdown: edit in `tools/Precept.Plugin/`, then `Developer: Reload Window`.
2. For MCP server C#: edit in `tools/Precept.Mcp/`, then `Developer: Reload Window` and trigger any MCP action. The launcher rebuilds and shadow-copies on demand.
3. First-time setup: run task `plugin: enable` (one time), then `Developer: Reload Window`.

The toggle script (`tools/scripts/toggle-plugin.js`) updates `chat.pluginLocations` in `.vscode/settings.json`. To stop loading the plugin, run task `plugin: disable` and reload.

#### File locking safety

Both the language server and MCP server use shadow-copy launchers to avoid file locking:

| Server | Build output | Running process locks | Rebuild safe? |
|--------|-------------|----------------------|---------------|
| Language server | `temp/dev-language-server/bin/` | `temp/dev-language-server/runtime/` | Yes |
| MCP server | `temp/dev-mcp/bin/` | `temp/dev-mcp/runtime/run-*/` | Yes |

The running process never locks the build output directory. Old runtime copies are pruned on the next launch; locked directories are silently skipped. The MCP tools read `.precept` files via momentary `File.ReadAllText()` (no held locks), and the language server reads exclusively from LSP in-memory buffers (never from disk).

#### Development vs Distribution

| Concern | Development (this repo) | Distribution (end users) |
|---|---|---|
| MCP server launch | Launcher script (build + shadow copy) | `dotnet tool run precept-mcp` |
| Plugin registration | `chat.pluginLocations` → `./tools/Precept.Plugin` | Marketplace install or Git URL |
| Extension registration | `extension: install` task | VS Code Marketplace |
| Plugin toggle | `plugin: enable` / `plugin: disable` tasks | Extensions panel |

### Precept Author Agent

The plugin ships a custom agent (`precept-author.md`) that provides an opinionated authoring workflow. The agent:

- Restricts tools to read, edit, search, and all Precept MCP tools (no terminal, no destructive operations)
- Treats `precept_language` as the authoritative DSL reference — not local files or training data
- Follows a mandatory validate → audit → inspect loop after every edit
- Is invocable from the agents dropdown and as a subagent from default Agent mode

The agent is the primary vehicle for high-quality Precept authoring across any workspace. It provides stronger workflow isolation than a skill alone, enforcing the correct tool sequence as its core identity rather than as a suggestion.

### Companion Skills

Two skills provide targeted capabilities accessible as slash commands and via automatic model invocation:

**`precept-authoring`** — standardizes the creation and editing workflow:
- If the workspace already contains `.precept` files, read one representative file first to match local conventions
- If no `.precept` files exist, rely on `precept_language` plus task requirements
- Call `precept_schema` when editing an existing file
- Validate with `precept_validate`, review with `precept_audit`
- After creating or editing a precept, include a Mermaid `stateDiagram-v2` diagram showing the resulting state machine to confirm the design with the user
- The skill must be repo-agnostic — it cannot assume `samples/` exists

**`precept-debugging`** — standardizes diagnosis and behavior tracing:
- Inspect shape with `precept_schema`
- Diagnose correctness with `precept_validate`
- Review structural quality with `precept_audit`
- Explore runtime behavior with `precept_inspect` and `precept_run`
- When explaining structure or transition behavior, include a focused Mermaid `stateDiagram-v2` diagram showing only the relevant states and transitions

Both skills include a "Mermaid Diagrams" section that teaches the model how to generate full or partial state diagrams from `precept_schema` + `precept_audit` data. Diagrams are rendered natively by VS Code Chat — no separate tool is needed. The extension's interactive preview panel (ELK + custom SVG) remains independent; the skill-generated Mermaid diagrams serve a different purpose (conversation-embedded, focused, partial).

Both skills follow the [Agent Skills specification](https://agentskills.io/specification):
- `name` must be lowercase kebab-case, match the parent directory name, max 64 chars
- `description` must be explicit and trigger-oriented for reliable discovery
- Keep `SKILL.md` body under 500 lines; move detailed reference to separate files
- Progressive disclosure: metadata (~100 tokens) → instructions (<5000 tokens) → resources (on demand)

---

## Build Order

`Precept.Mcp.csproj` sits alongside the language server in `tools/` and is included in `Precept.slnx`. It depends only on `Precept.csproj` — not on the language server project.

The plugin assembly work depends on the MCP project existing first. The agent and skill content can be drafted in parallel with MCP tool development since they are plain markdown files. The plugin packaging step combines MCP server (via `dotnet tool run`) + agent + skills into the final plugin directory structure.

The VS Code extension has no dependency on the plugin — they are separate distribution artifacts. The extension's `registerMcpServerDefinitionProvider()` is removed once the plugin is the shipping path for MCP. The MCP launcher script (`tools/scripts/start-precept-mcp.js`) is shared infrastructure used by both the plugin's dev `.mcp.json` and (temporarily) the workspace `.vscode/mcp.json` during the transition.

---

## Not In Scope (First Version)

- Hot-reload / file watching (Copilot calls tools on demand)
- Multi-file / import resolution (not a DSL feature)
- Authentication / remote transport (stdio is sufficient for local VS Code use)
- A `precept_fix` tool (auto-correction is out of scope; validation + schema + run + inspect provide enough signal for Copilot to self-correct)
