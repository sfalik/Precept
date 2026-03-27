# Precept MCP Server Design

**Status:** Original 6-tool surface implemented (2026-03-06); packaging and Copilot workflow design updated (2026-03-22); redesigned to 4 tools with text input and structured feedback (2026-03-26); design decisions finalized (2026-03-27); expanded to 5 tools with `precept_update` + `precept_fire` rename (2026-03-27)

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
        LanguageTool.cs
        CompileTool.cs
        InspectTool.cs
        FireTool.cs
        UpdateTool.cs
        ViolationDto.cs
        JsonConvert.cs
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

> **Redesign status (2026-03-27):** The original 6-tool surface (validate, schema, audit, run, language, inspect) was implemented on 2026-03-06. The redesign to 5 tools is now implemented — `precept_compile` (merging validate+schema+audit), `precept_inspect` (delegating to engine.Inspect), `precept_fire` (single-event execution, named to match `engine.Fire()`), `precept_update` (direct field editing), and `precept_language` (unchanged). All tools accept inline text input and return structured feedback.

### Tool Philosophy

Each tool owns exactly one concern. There is no overlap in what the tools report, and their failure modes reinforce the intended workflow:

- **`precept_compile`** is the sole source of structured diagnostics (parse errors, type errors, graph warnings). It answers: *"Is this definition correct and well-structured?"*
- **`precept_inspect`** is a read-only possibility map. It answers: *"From this state and data, what can happen for each event, and which fields are editable?"*
- **`precept_fire`** is single-event execution. It answers: *"What actually happens when I fire this event from this state and data?"*
- **`precept_update`** is direct field editing. It answers: *"Can I change these fields from this state, and what constraints fire?"*

Inspect and run compile internally (stateless), but treat compilation as a pass/fail gate — not a diagnostic surface. On compile failure they return a short error directing Copilot to use `precept_compile`. Only `precept_compile` returns structured diagnostics. This gives Copilot a clear signal chain: compile → fix → inspect/fire/update.

### Tool Tiers

| Tier | Tools | Input | Requires Runtime |
|---|---|---|---|
| Language | `precept_language` | *(none)* | No |
| Definition | `precept_compile` | `text` | No (parse + type-check + graph analysis) |
| Runtime | `precept_inspect`, `precept_fire`, `precept_update` | `text` + state + data | Yes |

### 1. `precept_language`

**Purpose:** Return a complete, structured reference for the Precept DSL — vocabulary, construct forms, semantic constraints, expression scoping rules, fire pipeline stages, and outcome kinds. Enables Copilot to write semantically correct `.precept` definitions without relying on trial-and-error against `precept_compile`.

Unlike the other tools, this tool takes no input — it describes the language itself.

**Input:** `{}` (no parameters)

**Output:** Full language reference JSON (vocabulary, constructs, constraints, expressionScopes, firePipeline, outcomeKinds). See the `precept_language` output format section below — unchanged from the original design.

**Implementation:** Serializes `ConstructCatalog.Constructs` + `DiagnosticCatalog.Diagnostics` + reflected `PreceptToken` vocabulary. No MCP-specific data — everything comes from core infrastructure.

---

### 2. `precept_compile`

**Purpose:** Parse, type-check, analyze, and compile a precept definition. Returns the full typed structure (states, fields, events, transitions) alongside any diagnostics — errors that block compilation and warnings/hints that flag structural quality issues. This is the single correctness and structure tool, replacing the former `precept_validate`, `precept_schema`, and `precept_audit`.

**Input:**
```json
{
  "text": "precept BugTracker\nfield Assignee as string nullable\n..."
}
```

**Output (valid definition):**
```json
{
  "valid": true,
  "name": "BugTracker",
  "initialState": "Triage",
  "stateCount": 7,
  "eventCount": 9,
  "states": [
    { "name": "Triage", "rules": [] },
    { "name": "Blocked", "rules": ["Must have an assignee while blocked"] }
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
  ],
  "diagnostics": [
    { "line": 0, "message": "State 'Archived' is unreachable from the initial state.", "code": "PRECEPT048", "severity": "warning" }
  ]
}
```

**Output (type errors — partial structure with diagnostics):**
```json
{
  "valid": false,
  "name": "BugTracker",
  "initialState": "Triage",
  "stateCount": 7,
  "eventCount": 9,
  "states": [...],
  "fields": [...],
  "collectionFields": [...],
  "events": [...],
  "transitions": [...],
  "diagnostics": [
    { "line": 12, "message": "set target 'Value' type mismatch: expected number but expression produces number|null.", "code": "PRECEPT042", "severity": "error" },
    { "line": 12, "message": "unknown identifier 'Missing'.", "code": "PRECEPT038", "severity": "error" }
  ]
}
```

**Output (parse failure — diagnostics only):**
```json
{
  "valid": false,
  "diagnostics": [
    { "line": 3, "message": "Expected state declaration.", "severity": "error" }
  ]
}
```

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)` — a composed pipeline that runs parse → structured validation → compile. Returns the full model projection when parsing succeeds (even with type errors), diagnostics only when parsing fails. Graph analysis findings (C48–C53) appear as warning/hint-severity diagnostics alongside any type errors. The tool is a thin projection of the core result into JSON.

---

### 3. `precept_inspect`

**Purpose:** From a given state and data snapshot, evaluate all declared events and report what each would do — without mutating anything. Lets Copilot explore the precept interactively ("what can happen from here?") instead of guessing event sequences for `precept_fire`.

**Input:**
```json
{
  "text": "precept BugTracker\n...",
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

The `eventArgs` field is optional. When provided, the specified args are used for the named events during evaluation — the tool re-inspects those events individually with the supplied args. Events not listed in `eventArgs` are inspected without args, and the engine reports the actual outcome (which may be `MissingRequiredArguments` if args are needed).

**Output:**
```json
{
  "currentState": "InProgress",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "events": [
    {
      "event": "Block",
      "outcome": "Transition",
      "resultState": "Blocked",
      "violations": []
    },
    {
      "event": "SubmitReview",
      "outcome": "ConstraintFailure",
      "resultState": null,
      "violations": [
        {
          "message": "Cannot leave InProgress without completion note",
          "source": {
            "kind": "state-assertion",
            "stateName": "InProgress",
            "anchor": "from",
            "expressionText": "CompletionNote != null",
            "reason": "Cannot leave InProgress without completion note",
            "sourceLine": 14
          },
          "targets": [
            { "kind": "field", "fieldName": "CompletionNote" }
          ]
        }
      ]
    },
    {
      "event": "Escalate",
      "outcome": "MissingRequiredArguments",
      "resultState": null,
      "violations": [],
      "requiredArgs": ["Level"]
    }
  ],
  "editableFields": [
    { "name": "Priority", "type": "number", "nullable": false, "currentValue": 3 },
    { "name": "BlockReason", "type": "string", "nullable": true, "currentValue": null }
  ],
  "error": null
}
```

The response echoes the resolved instance snapshot (`currentState` + `data` with defaults applied), so Copilot can see what defaults were filled in and confirm the starting point matches intent. Events appear in declaration order (no sorting). The `editableFields` array lists fields that have `in <State> edit` declarations for the current state.

Each event reports:
- `outcome` — the engine's actual `TransitionOutcome` string (e.g. `Transition`, `NoTransition`, `ConstraintFailure`, `Rejected`, `MissingRequiredArguments`, `Undefined`, `Unmatched`)
- `resultState` — the target state on success, `null` otherwise
- `violations` — structured `ViolationDto` array (empty unless `ConstraintFailure`)
- `requiredArgs` — list of required argument names (present only when the engine populates `RequiredEventArgumentKeys`)

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, then `engine.Inspect(instance)` for the full state-level inspection (declaration order preserved). When `eventArgs` are supplied, re-inspects those specific events individually with `engine.Inspect(instance, eventName, args)`. Projects `EditableFields` from the core `InspectionResult`. No reimplementation of the inspection loop.

**On compile failure:** Returns a short error: `"Compilation failed. Use precept_compile to diagnose and fix errors first."` — no diagnostics, no runtime results. Only `precept_compile` surfaces structured diagnostics.

---

### 4. `precept_fire`

**Purpose:** Fire a single event against a precept from a given state and data snapshot. Returns the execution outcome — the new state, updated data, and any constraint violations. Lets Copilot verify that a specific action actually works at runtime. Named to match the core API (`engine.Fire()`).

Unlike `precept_inspect` (which previews all events read-only), `precept_fire` executes one event and returns its concrete result. Copilot chains sequential calls to trace multi-step scenarios, feeding each result’s state+data into the next call.

**Input:**
```json
{
  "text": "precept BugTracker\n...",
  "currentState": "InProgress",
  "data": {
    "Assignee": "alice",
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "event": "Block",
  "args": { "Reason": "Waiting on infra" }
}
```

The `currentState` and `data` inputs match the same shape as `precept_inspect`. The `args` field is optional — only needed for events that declare arguments.

**Output (success):**
```json
{
  "event": "Block",
  "outcome": "Transition",
  "fromState": "InProgress",
  "toState": "Blocked",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": "Waiting on infra", "Resolution": null },
  "violations": [],
  "error": null
}
```

**Output (constraint failure):**
```json
{
  "event": "SubmitReview",
  "outcome": "ConstraintFailure",
  "fromState": "InProgress",
  "toState": null,
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "violations": [
    {
      "message": "Cannot leave InProgress without completion note",
      "source": {
        "kind": "state-assertion",
        "stateName": "InProgress",
        "anchor": "from",
        "expressionText": "CompletionNote != null",
        "reason": "Cannot leave InProgress without completion note",
        "sourceLine": 14
      },
      "targets": [
        { "kind": "field", "fieldName": "CompletionNote" }
      ]
    }
  ],
  "error": null
}
```

The response echoes the resolved data snapshot (with defaults applied), matching the inspect tool's behavior.

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, creates an instance at the given state+data, then calls `engine.Fire(instance, event, args)`. Projects `FireResult.Violations` as full `ViolationDto` arrays — no string joining.

**On compile failure:** Returns a short error: `"Compilation failed. Use precept_compile to diagnose and fix errors first."` — no execution results. Only `precept_compile` surfaces structured diagnostics.

---

### 5. `precept_update`

**Purpose:** Apply a direct field edit to a precept instance from a given state and data snapshot. Returns the update outcome — whether the edit succeeded, was rejected (uneditable field, constraint failure, invalid input), and the resulting data. Lets Copilot test `in <State> edit` declarations without firing events.

**Input:**
```json
{
  "text": "precept BugTracker\n...",
  "currentState": "InProgress",
  "data": {
    "Assignee": "alice",
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "fields": {
    "Priority": 1
  }
}
```

The `fields` object contains the field names and new values to apply. At least one field must be provided.

**Output (success):**
```json
{
  "outcome": "Updated",
  "data": { "Assignee": "alice", "Priority": 1, "BlockReason": null, "Resolution": null },
  "violations": [],
  "error": null
}
```

**Output (uneditable field):**
```json
{
  "outcome": "UneditableField",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "violations": [],
  "error": null
}
```

**Output (constraint failure):**
```json
{
  "outcome": "ConstraintFailure",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "violations": [
    {
      "message": "Priority must be between 1 and 5",
      "source": { "kind": "invariant", "expressionText": "Priority >= 1 and Priority <= 5", "reason": "Priority must be between 1 and 5", "sourceLine": 8 },
      "targets": [{ "kind": "field", "fieldName": "Priority" }]
    }
  ],
  "error": null
}
```

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, creates an instance at the given state+data, then calls `engine.Update(instance, patch => { foreach field: patch.Set(key, value) })`. Projects `UpdateResult.Violations` as `ViolationDto` arrays.

**On compile failure:** Returns a short error: `"Compilation failed. Use precept_compile to diagnose and fix errors first."` — no update results. Only `precept_compile` surfaces structured diagnostics.

---

### DTOs

Tools use two DTO types for structured feedback — diagnostics (compile-time) and violations (runtime). These are thin projections of core types — no domain logic.

**`DiagnosticDto`** (inline in `CompileTool.cs` — sole consumer) — parse, type-check, and graph analysis findings:

```json
{ "line": 12, "column": 18, "message": "unknown identifier 'Missing'.", "code": "PRECEPT038", "severity": "error" }
```

Fields: `line` (1-based), `column` (0-based, optional), `message`, `code` (optional — present for all registered constraints), `severity` (`"error"`, `"warning"`, or `"hint"`).

**`ViolationDto`** (shared `Tools/ViolationDto.cs` — used by inspect, fire, and update) — runtime constraint violations:

```json
{
  "message": "Active requires assignee",
  "source": {
    "kind": "state-assertion",
    "stateName": "Active",
    "anchor": "in",
    "expressionText": "Assignee != null",
    "reason": "Active requires assignee",
    "sourceLine": 14
  },
  "targets": [
    { "kind": "field", "fieldName": "Assignee" }
  ]
}
```

`ViolationDto` is a full projection of core `ConstraintViolation`:

- **`source`** — projects `ConstraintSource` (4 subtypes: `invariant`, `state-assertion`, `event-assertion`, `transition-rejection`). Each subtype carries its relevant fields (expression text, reason, state name, anchor, event name, source line).
- **`targets`** — projects `ConstraintTarget[]` (5 subtypes: `field`, `event-arg`, `event`, `state`, `definition`). Each subtype carries its relevant identifiers.

This preserves the full structured violation model from core without information loss.

### Error Handling by Tier

| Tool | Compile-time issues | Runtime violations |
|---|---|---|
| `precept_compile` | `IReadOnlyList<DiagnosticDto>` | n/a |
| `precept_inspect` | Short error string (gate) | `IReadOnlyList<ViolationDto>` per event |
| `precept_fire` | Short error string (gate) | `IReadOnlyList<ViolationDto>` |
| `precept_update` | Short error string (gate) | `IReadOnlyList<ViolationDto>` |
| `precept_language` | n/a | n/a |

Only `precept_compile` surfaces structured diagnostics. Inspect, fire, and update treat compilation as a pass/fail gate — on failure they return `"Compilation failed. Use precept_compile to diagnose and fix errors first."` with no runtime results. This prevents diagnostic duplication and gives Copilot a clear signal about which tool to call.

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
├── plugin.json                    # Plugin metadata and configuration
├── agents/
│   └── precept-author.agent.md    # Precept Author custom agent
├── skills/
│   ├── precept-authoring/
│   │   └── SKILL.md               # Authoring workflow skill
│   └── precept-debugging/
│       └── SKILL.md               # Debugging/diagnosis skill
├── .mcp.json                      # MCP server definition (dev: launcher, dist: dotnet tool)
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
      "args": ["${CLAUDE_PLUGIN_ROOT}/../scripts/start-precept-mcp.js"]
    }
  }
}
```

The `${CLAUDE_PLUGIN_ROOT}` token is expanded by VS Code to the plugin's absolute path at runtime, making the reference unambiguous regardless of working directory. The launcher (`tools/scripts/start-precept-mcp.js`) builds from source, shadow-copies the output, and runs the copy — preventing file locking during rebuilds. At publish time, CI rewrites the `.mcp.json` to the `dotnet tool run` form. The launcher script is not included in the published plugin.

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

The running process never locks the build output directory. Old runtime copies are pruned on the next launch; locked directories are silently skipped. The MCP tools accept precept text directly (no file reads), and the language server reads exclusively from LSP in-memory buffers (never from disk).

#### Development vs Distribution

| Concern | Development (this repo) | Distribution (end users) |
|---|---|---|
| MCP server launch | Launcher script (build + shadow copy) | `dotnet tool run precept-mcp` |
| Plugin registration | `chat.pluginLocations` → `./tools/Precept.Plugin` | Marketplace install or Git URL |
| Extension registration | `extension: install` task | VS Code Marketplace |
| Plugin toggle | `plugin: enable` / `plugin: disable` tasks | Extensions panel |

### Precept Author Agent

The plugin ships a custom agent (`precept-author.agent.md`) that establishes a lightweight persona with strict tool restrictions. The agent:

- Restricts tools to `read`, `edit`, `search`, `fetch`, and all `precept/*` MCP tools (no terminal, no destructive operations)
- Treats `precept_language` as the authoritative DSL reference — never generate syntax from memory or training data
- Establishes core principles: compile after every edit, match local `.precept` conventions when present, fall back to `precept_language` when no local files exist
- Is invocable from the agents dropdown and as a subagent from default Agent mode

The agent body is intentionally thin — it owns the **persona and tool restrictions**. Detailed workflows live in the companion skills, which VS Code auto-discovers and loads based on the user's request. This separation keeps the agent focused on identity ("what am I allowed to do") while skills handle procedure ("how do I do it").

### Companion Skills

Two skills provide targeted capabilities accessible as slash commands and via automatic model invocation:

**`precept-authoring`** — standardizes the creation and editing workflow:
- If the workspace already contains `.precept` files, read one representative file first to match local conventions
- If no `.precept` files exist, rely on `precept_language` plus task requirements
- Call `precept_compile` to validate and inspect the structure of any file being edited
- After creating or editing a precept, include a Mermaid `stateDiagram-v2` diagram showing the resulting state machine to confirm the design with the user
- The skill must be repo-agnostic — it cannot assume `samples/` exists

**`precept-debugging`** — standardizes diagnosis and behavior tracing:
- Diagnose correctness and review structural quality with `precept_compile` (errors + warnings/hints)
- Explore runtime behavior with `precept_inspect` and `precept_fire`
- When explaining structure or transition behavior, include a focused Mermaid `stateDiagram-v2` diagram showing only the relevant states and transitions

Both skills include a "Mermaid Diagrams" section that teaches the model how to generate full or partial state diagrams from `precept_compile` output data. Diagrams are rendered natively by VS Code Chat — no separate tool is needed. The extension's interactive preview panel (ELK + custom SVG) remains independent; the skill-generated Mermaid diagrams serve a different purpose (conversation-embedded, focused, partial).

Both skills follow the [Agent Skills specification](https://agentskills.io/specification):
- `name` must be lowercase kebab-case, match the parent directory name, max 64 chars
- `description` must be explicit and trigger-oriented for reliable discovery
- Keep `SKILL.md` body under 500 lines; move detailed reference to separate files
- Progressive disclosure: metadata (~100 tokens) → instructions (<5000 tokens) → resources (on demand)

---

## Build Order

`Precept.Mcp.csproj` sits alongside the language server in `tools/` and is included in `Precept.slnx`. It depends only on `Precept.csproj` — not on the language server project.

The plugin assembly work depends on the MCP project existing first. The agent and skill content can be drafted in parallel with MCP tool development since they are plain markdown files. The plugin packaging step combines MCP server (via `dotnet tool run`) + agent + skills into the final plugin directory structure.

The VS Code extension has no dependency on the plugin — they are separate distribution artifacts. The extension's `registerMcpServerDefinitionProvider()` is removed once the plugin is the shipping path for MCP. The `Precept Dev` entry in `.vscode/mcp.json` is removed when the plugin is created (Phase 7), since the plugin replaces it. The MCP launcher script (`tools/scripts/start-precept-mcp.js`) is shared infrastructure used by the plugin's dev `.mcp.json`.

---

## Not In Scope (First Version)

- Hot-reload / file watching (Copilot calls tools on demand)
- Multi-file / import resolution (not a DSL feature)
- Authentication / remote transport (stdio is sufficient for local VS Code use)
- A `precept_fix` tool (auto-correction is out of scope; compile + inspect + run provide enough signal for Copilot to self-correct)
