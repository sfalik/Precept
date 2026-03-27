# Precept MCP Server Design

**Status:** Original 6-tool surface implemented (2026-03-06); packaging and Copilot workflow design updated (2026-03-22); redesigned to 4 tools with text input and structured feedback (2026-03-26)

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
        RunTool.cs
        JsonConvert.cs
    Dtos/
        DiagnosticDto.cs
        ViolationDto.cs
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

> **Redesign status (2026-03-26):** The original 6-tool surface (validate, schema, audit, run, language, inspect) was implemented on 2026-03-06. The redesign below reduces to 4 tools, switches from file paths to text input, adds structured feedback, and enforces the thin-wrapper principle. Implementation is pending — see `McpServerImplementationPlan.md` for phases.

### Tool Tiers

| Tier | Tools | Input | Requires Runtime |
|---|---|---|---|
| Language | `precept_language` | *(none)* | No |
| Definition | `precept_compile` | `text` | No (parse + type-check + graph analysis) |
| Runtime | `precept_inspect`, `precept_run` | `text` + state + data | Yes |

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

**Purpose:** From a given state and data snapshot, evaluate all declared events and report what each would do — without mutating anything. Lets Copilot explore the precept interactively ("what can happen from here?") instead of guessing event sequences for `precept_run`.

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

The `eventArgs` field is optional. When provided, the specified args are used for the named events during evaluation. Events not listed in `eventArgs` whose required args are missing report `requiresArgs: true`.

**Output:**
```json
{
  "currentState": "InProgress",
  "events": [
    {
      "event": "Block",
      "status": "evaluated",
      "outcome": "Transition",
      "resultState": "Blocked",
      "resultData": { "Assignee": "alice", "Priority": 3, "BlockReason": "Waiting on infra", "Resolution": null },
      "violations": []
    },
    {
      "event": "SubmitReview",
      "status": "evaluated",
      "outcome": "ConstraintFailure",
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
      "event": "Approve",
      "status": "evaluated",
      "outcome": "Undefined",
      "violations": []
    },
    {
      "event": "Escalate",
      "status": "requires-args",
      "requiredArgs": [{ "name": "Level", "type": "number" }]
    }
  ],
  "diagnostics": []
}
```

Events are sorted: actionable (Transition/NoTransition) first, then unavailable (Undefined/Unmatched/Rejected/ConstraintFailure), then requires-args.

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, then `engine.Inspect(instance)`. The core `InspectionResult` already contains structured `EventInspectionResult` records with typed `Violations` — the MCP tool projects them into the output DTOs. No reimplementation of the inspection loop.

If the input text has parse or compile errors, returns `diagnostics` without runtime results.

---

### 4. `precept_run`

**Purpose:** Execute a sequence of events against a precept starting from initial state and data. Returns step-by-step outcomes with structured violation data. Lets Copilot verify that a scenario it describes or edits actually works at runtime.

**Input:**
```json
{
  "text": "precept BugTracker\n...",
  "initialData": {
    "Assignee": null,
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "steps": [
    { "event": "Assign", "args": { "User": "alice" } },
    { "event": "StartWork" },
    { "event": "Block", "args": { "Reason": "Waiting on infra" } }
  ]
}
```

**Output:**
```json
{
  "steps": [
    {
      "step": 1,
      "event": "Assign",
      "outcome": "NoTransition",
      "state": "Open",
      "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
      "violations": []
    },
    {
      "step": 2,
      "event": "StartWork",
      "outcome": "Transition",
      "state": "InProgress",
      "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
      "violations": []
    },
    {
      "step": 3,
      "event": "Block",
      "outcome": "Transition",
      "state": "Blocked",
      "data": { "Assignee": "alice", "Priority": 3, "BlockReason": "Waiting on infra", "Resolution": null },
      "violations": []
    }
  ],
  "finalState": "Blocked",
  "finalData": { "Assignee": "alice", "Priority": 3, "BlockReason": "Waiting on infra", "Resolution": null },
  "abortedAt": null,
  "diagnostics": []
}
```

If a step fails (Rejected, ConstraintFailure, Undefined, Unmatched), execution stops, `abortedAt` is set, and the failing step includes structured `violations`.

If the input text has parse or compile errors, returns `diagnostics` without executing any steps.

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, then `engine.Fire(instance, event, args)` in a loop. Each step projects `FireResult.Violations` as full `ViolationDto` arrays — no string joining.

---

### Shared DTOs

All tools that return diagnostics or violations use the same DTO types for consistency. These are thin projections of core types — no domain logic.

**`DiagnosticDto`** — parse, type-check, and graph analysis findings:

```json
{ "line": 12, "column": 18, "message": "unknown identifier 'Missing'.", "code": "PRECEPT038", "severity": "error" }
```

Fields: `line` (1-based), `column` (0-based, optional), `message`, `code` (optional — present for all registered constraints), `severity` (`"error"`, `"warning"`, or `"hint"`).

**`ViolationDto`** — runtime constraint violations from fire/inspect:

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

The running process never locks the build output directory. Old runtime copies are pruned on the next launch; locked directories are silently skipped. The MCP tools accept precept text directly (no file reads), and the language server reads exclusively from LSP in-memory buffers (never from disk).

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
- Follows a mandatory compile → inspect loop after every edit
- Is invocable from the agents dropdown and as a subagent from default Agent mode

The agent is the primary vehicle for high-quality Precept authoring across any workspace. It provides stronger workflow isolation than a skill alone, enforcing the correct tool sequence as its core identity rather than as a suggestion.

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
- Explore runtime behavior with `precept_inspect` and `precept_run`
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

The VS Code extension has no dependency on the plugin — they are separate distribution artifacts. The extension's `registerMcpServerDefinitionProvider()` is removed once the plugin is the shipping path for MCP. The MCP launcher script (`tools/scripts/start-precept-mcp.js`) is shared infrastructure used by both the plugin's dev `.mcp.json` and (temporarily) the workspace `.vscode/mcp.json` during the transition.

---

## Not In Scope (First Version)

- Hot-reload / file watching (Copilot calls tools on demand)
- Multi-file / import resolution (not a DSL feature)
- Authentication / remote transport (stdio is sufficient for local VS Code use)
- A `precept_fix` tool (auto-correction is out of scope; compile + inspect + run provide enough signal for Copilot to self-correct)
