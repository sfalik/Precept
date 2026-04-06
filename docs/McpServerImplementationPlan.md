# Precept MCP Server — Implementation Plan

Date: 2025-03-05
Spec: `docs/McpServerDesign.md`
Prerequisite: Language redesign complete (`docs/PreceptLanguageImplementationPlan.md` — all 9 phases)

> **Status (2026-04-03):** Phases 0–6 and the MCP Redesign Phase are implemented — the 5-tool surface (language, compile, inspect, fire, update) is live and tested with text input and structured feedback. Phases 7–8 are implemented — the agent plugin uses the Claude format (`.claude-plugin/plugin.json`); the plugin's `.mcp.json` uses the distribution format (`dotnet tool run precept-mcp`) for both VS Code and Copilot CLI consumers, with `.vscode/mcp.json` overriding for dev-time lazy build. The Precept Author agent and companion skills (precept-authoring, precept-debugging) are drafted and validated. Phase 9 is in progress — README and design docs are updated, MCP provider removed from extension; distribution (publish to marketplace) and remaining testing items are pending.

This plan builds the MCP server as a new project in `tools/Precept.Mcp/`. Each phase adds one tool, fully tested, before moving to the next. The core `src/Precept/` infrastructure (catalogs, parser, runtime) is already in place from the language redesign — this plan only adds tool wrappers and MCP transport.

Planning update (2026-03-22): the distribution phases now target an **agent plugin** for MCP server, custom agent, and skills — instead of the earlier approach where the VS Code extension bundled MCP and scaffolded skills into the workspace. The plugin model eliminates all scaffolding; Copilot discovers plugin-provided agents, skills, and MCP servers automatically. The VS Code extension retains only editor features (language server, syntax highlighting, preview panel, commands).

---

## Guiding Principles

1. **One tool per phase.** Each tool is independently useful. Ship order matches dependency order — earlier tools inform later tools' behavior.
2. **Thin wrappers.** Tools call existing core APIs (`PreceptParser`, `PreceptCompiler`, `PreceptEngine`, catalogs). No domain logic in the MCP project.
3. **Structured JSON output.** Every tool returns well-typed JSON so Copilot can reason about results programmatically — no prose-only responses.
4. **Test every tool in isolation.** Each tool gets integration tests that call the tool method directly (no MCP transport overhead). Asserts on JSON shape and semantic correctness.
5. **Plugin for Copilot, extension for editor.** The MCP server, agent, and skills ship as a Copilot agent plugin — separate from the VS Code extension. The extension retains only editor features. No scaffolding of workspace files.

---

## Phase 0: Project Scaffolding

**Goal:** Empty MCP server project that builds, connects via stdio, and registers zero tools.

### Steps

- [ ] Create `tools/Precept.Mcp/Precept.Mcp.csproj` with dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.1.*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.*" />
    <ProjectReference Include="..\..\src\Precept\Precept.csproj" />
  </ItemGroup>
</Project>
```

- [ ] Create `tools/Precept.Mcp/Program.cs` with MCP host bootstrap:

```csharp
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

- [ ] Create empty `tools/Precept.Mcp/Tools/` directory
- [ ] Add project to `Precept.slnx`
- [ ] Create `test/Precept.Mcp.Tests/Precept.Mcp.Tests.csproj` referencing `Precept.Mcp.csproj`

### Checkpoint

- `dotnet build` passes (entire solution including new project)
- `dotnet run --project tools/Precept.Mcp` starts, accepts stdio, returns empty tool list
- Test project builds

---

## Phase 1: `precept_validate`

**Goal:** Parse + validate a `.precept` file, return structured diagnostics including shared type-checker codes.

**New file:** `tools/Precept.Mcp/Tools/ValidateTool.cs`

### Implementation

```csharp
[McpServerToolType]
public static class ValidateTool
{
    [McpServerTool(Name = "precept_validate")]
    [Description("Parse and compile a .precept file. Returns structured diagnostics.")]
    public static ValidateResult Run(
        [Description("Path to the .precept file")] string path)
    {
        var text = File.ReadAllText(path);
        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(text);

        if (model is null || diagnostics.Count > 0)
            return new(false, null, 0, 0, diagnostics.Select(ToDiagnosticDto).ToList());

        var validation = PreceptCompiler.Validate(model);
        if (validation.HasErrors)
        {
            return new(false, model.Name, 0, 0,
                validation.Diagnostics
                    .Select(d => new DiagnosticDto(d.Line, d.Message, d.DiagnosticCode))
                    .ToList());
        }

        try
        {
            var engine = PreceptCompiler.Compile(model);
            return new(true, model.Name, model.States.Count, model.Events.Count, []);
        }
        catch (Exception ex)
        {
            return new(false, model.Name, 0, 0, [ParseError(ex)]);
        }
    }
}
```

### Input/Output

Per `McpServerDesign.md § precept_validate`. Input: `{ "path": "..." }`. Output: `{ "valid", "machineName", "stateCount", "eventCount", "diagnostics" }`, where each diagnostic may include a stable `code` such as `PRECEPT038`-`PRECEPT043`.

### Tests

- [ ] Valid file → `valid: true`, zero diagnostics
- [ ] Syntax error → `valid: false`, diagnostic with line number
- [ ] Shared type-check violation → `valid: false`, diagnostic with stable PRECEPT code
- [ ] Multiple shared type-check violations → returns all shared type diagnostics, not just the first one
- [ ] Missing file → graceful error (not unhandled exception)

### Checkpoint

- `dotnet build` passes
- All tool tests green
- Manual test: `dotnet run --project tools/Precept.Mcp` responds to `precept_validate` over stdio

---

## Phase 2: `precept_schema`

**Goal:** Return the full typed structure of a precept — states, fields, events, transitions.

**New file:** `tools/Precept.Mcp/Tools/SchemaTool.cs`

### Implementation

Parse the file, walk `DslWorkflowModel` records, and project into a JSON-friendly DTO. No runtime needed — purely model inspection.

Key mappings:
- `DslState` → `{ name, rules }` (rules from state asserts)
- `DslField` → `{ name, type, nullable, default }`
- `DslCollectionField` → `{ name, kind, innerType }`
- `DslEvent` → `{ name, args: [{ name, type, nullable, required }] }`
- `DslTransitionRow` → `{ from, on, branches }` (branches summarize guard → outcome)

### Input/Output

Per `McpServerDesign.md § precept_schema`. Input: `{ "path": "..." }`. Output: full schema JSON.

### Tests

- [ ] Valid file → correct state count, field types, event args
- [ ] Collection fields → correct `kind` and `innerType`
- [ ] Transitions with guards → branches include guard text
- [ ] File with no events → empty `events` array
- [ ] Invalid file → structured error (not unhandled exception)

### Checkpoint

- `dotnet build` passes
- All tool tests green

---

## Phase 3: `precept_audit`

**Goal:** Graph analysis — reachability, dead ends, terminal states, orphaned events.

**New file:** `tools/Precept.Mcp/Tools/AuditTool.cs`

### Implementation

1. Build a directed graph: edges from each `DslTransitionRow`'s source state to each `DslStateTransition` outcome's target state.
2. BFS from `initialState` → `reachableStates`.
3. `unreachableStates` = all states − reachable states.
4. `terminalStates` = states with zero outgoing transitions.
5. `deadEndStates` = non-terminal states where all outgoing rows have `NoTransition` or `Rejected` outcomes only.
6. `orphanedEvents` = declared events not referenced in any transition row.
7. `warnings` = human-readable descriptions for any non-empty problem sets.

### Input/Output

Per `McpServerDesign.md § precept_audit`. Input: `{ "path": "..." }`. Output: `{ allStates, reachableStates, unreachableStates, terminalStates, deadEndStates, orphanedEvents, warnings }`.

### Tests

- [ ] Fully connected graph → empty unreachable/deadEnd/orphaned
- [ ] Intentionally unreachable state → appears in `unreachableStates`
- [ ] Terminal state (no outgoing transitions) → appears in `terminalStates`
- [ ] Dead-end state (all outcomes reject/no-transition) → appears in `deadEndStates`
- [ ] Declared event with no `from ... on` rows → appears in `orphanedEvents`
- [ ] Each non-empty result produces a corresponding warning string

### Checkpoint

- `dotnet build` passes
- All tool tests green

---

## Phase 4: `precept_run`

**Goal:** Execute a step-by-step scenario against a precept and return outcomes.

**New file:** `tools/Precept.Mcp/Tools/RunTool.cs`

### Implementation

1. Parse + compile the file.
2. `PreceptEngine.CreateInstance(initialData)` — start from initial state with caller-supplied data (or defaults).
3. Loop through `steps`: call `engine.Fire(instance, event, args)` per step.
4. On each step, record `{ step, event, outcome, state, data }`.
5. If a step's outcome is `Rejected`, `ConstraintFailure`, `Undefined`, or `Unmatched`, set `abortedAt` and stop.
6. Return the full step log + `finalState` + `finalData`.

### Input/Output

Per `McpServerDesign.md § precept_run`. Input: `{ path, initialData?, steps }`. Output: `{ steps[], finalState, finalData, abortedAt }`.

### Tests

- [ ] Happy path (all steps Transition/NoTransition) → correct final state/data, `abortedAt` null
- [ ] Step rejected mid-sequence → `abortedAt` set, earlier steps present, later steps absent
- [ ] Step with `Undefined` → aborted with explanation
- [ ] Empty steps array → immediate return with initial state as final
- [ ] Steps with event args → args passed through correctly
- [ ] Invalid file → error before execution starts

### Checkpoint

- `dotnet build` passes
- All tool tests green

---

## Phase 5: `precept_language`

**Goal:** Return the full structured language reference — vocabulary, constructs, constraints, expression scopes, fire pipeline, outcome kinds.

**New file:** `tools/Precept.Mcp/Tools/LanguageTool.cs`

### Implementation

Assemble the response from three core infrastructure sources (all in `src/Precept/Dsl/`):

| Section | Source |
|---|---|
| `vocabulary` | Reflect `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` on `PreceptToken` enum members. Group by category. Operators include precedence and arity from a small lookup table in the tool. |
| `constructs` | Serialize `ConstructCatalog.Constructs` directly. |
| `constraints` | Serialize `DiagnosticCatalog.Constraints` directly (ID, phase, rule). |
| `expressionScopes` | Static data — 5 entries describing what identifiers are allowed in each expression position. |
| `firePipeline` | Static data — 6 stages. |
| `outcomeKinds` | Static data — 5 outcome kinds. |

The static data sections (`expressionScopes`, `firePipeline`, `outcomeKinds`) are constant arrays defined in the tool class. They change rarely and are validated by the documentation-match test (Phase 5 of the language redesign plan).

### Input/Output

Per `McpServerDesign.md § precept_language`. Input: `{}` (no parameters). Output: full language reference JSON.

### Tests

- [ ] Output contains all vocabulary categories (control, declaration, action, outcome, type, literal, operator)
- [ ] Every keyword in the token enum appears in the vocabulary
- [ ] `constructs` array matches `ConstructCatalog.Constructs` count
- [ ] `constraints` array matches `DiagnosticCatalog.Constraints` count
- [ ] `expressionScopes` has 5 entries
- [ ] `firePipeline` has 6 stages
- [ ] `outcomeKinds` has 5 entries
- [ ] Output is valid JSON (round-trip serialize/deserialize)

### Checkpoint

- `dotnet build` passes
- All tool tests green

---

## Phase 6: `precept_inspect`

**Goal:** From a given state and data, report what every event would do — without mutating.

**New file:** `tools/Precept.Mcp/Tools/InspectTool.cs`

### Implementation

1. Parse + compile the file.
2. `PreceptEngine.CreateInstance(currentState, data)`.
3. For each declared event:
   - If the event has required args and the caller didn't supply them in `eventArgs`, report `requiresArgs` with the arg list.
   - Otherwise, call `engine.Fire(instance, event, args)` with a snapshot copy of the instance (read-only).
   - Map the `FireResult` to `{ event, outcome, resultState?, resultData?, reason? }`.
4. Sort results: actionable outcomes first (Transition/NoTransition), then unavailable (Undefined/Unmatched/Rejected/ConstraintFailure), then `requiresArgs`.

### Input/Output

Per `McpServerDesign.md § precept_inspect`. Input: `{ path, currentState, data, eventArgs? }`. Output: `{ currentState, events[] }`.

### Tests

- [ ] Event that transitions → `outcome: "Transition"`, correct `resultState`
- [ ] Event not defined for state → `outcome: "Undefined"`, `reason` present
- [ ] Event with guard that doesn't match → `outcome: "Unmatched"`
- [ ] Event with required args not supplied → `requiresArgs: true`, arg list present
- [ ] Event with args supplied via `eventArgs` → evaluated with those args
- [ ] Result ordering: actionable before unavailable before requiresArgs

### Checkpoint

- `dotnet build` passes
- All tool tests green

---

## MCP Redesign Phase: 6→4 Tools with Text Input

> **Prerequisite:** Language Phase I (Graph Analysis Warning Diagnostics — C48–C53 plus structured compile validation cleanup) from `PreceptLanguageImplementationPlan.md`

**Goal:** Replace the 6-tool surface (validate, schema, audit, run, language, inspect) with 4 tools (`precept_language`, `precept_compile`, `precept_inspect`, `precept_run`) that accept inline text, return structured feedback, and enforce the thin-wrapper principle. See `McpServerDesign.md` for the full redesign spec.

### Design Decisions

The following decisions were finalized during the design walkthrough (2026-03-27):

1. **DiagnosticDto location:** Inline in `CompileTool.cs` — it is the sole consumer.
2. **Schema DTOs location:** Inline in `CompileTool.cs`.
3. **InspectTool delegation:** Uses `engine.Inspect(instance, eventName, args?)` per event — delegates to the core API instead of reimplementing the inspection loop.
4. **ViolationDto shape:** Flat tagged records in a shared `Tools/ViolationDto.cs` file (used by both inspect and run).
5. **CompileTool on partial failure:** Returns partial schema on validation errors, diagnostics-only on parse failure.
6. **Graph analysis surface:** Diagnostics only — no legacy audit arrays.
7. **CompileTool entry point:** `CompileFromText` as sole entry point.
8. **Compile failure in inspect/run:** Short error string: `"Compilation failed. Use precept_compile to diagnose and fix errors first."` Only `precept_compile` surfaces structured diagnostics. This prevents diagnostic duplication and gives Copilot a clear signal chain: compile → fix → inspect/run.
9. **Inspect instance echo:** Echoes the resolved snapshot (`state` + `data` with defaults applied) so Copilot can see what defaults were filled in.
10. **Run tool shape:** Single-event execution. Takes `text`, `currentState`, `data`, `event`, `args` (same input shape as inspect). No batch. Copilot chains sequential calls to trace multi-step scenarios.

### Scope

1. **Core API (already implemented):** `PreceptCompiler.CompileFromText(string text)` — a composed pipeline that runs parse → type-check → graph analysis → compile, returning a single result with the compiled definition (if successful), partial model (on type errors), and all diagnostics.
2. **DTOs:** `DiagnosticDto` inline in `CompileTool.cs`; `ViolationDto` shared at `Tools/ViolationDto.cs`.
3. **Replace tool files:**
   - `ValidateTool.cs` + `SchemaTool.cs` + `AuditTool.cs` → `CompileTool.cs`
   - `InspectTool.cs` → rewritten to delegate to `engine.Inspect()`, echo resolved snapshot, compile failure returns error string
   - `RunTool.cs` → rewritten as single-event execution with state+data input, structured `ViolationDto` output, compile failure returns error string
   - `LanguageTool.cs` → unchanged (no file input)
4. **Update all tests:** Rewrite test files to match new tool signatures, input shapes, and output DTOs.
5. **Switch input contract:** All tools that previously took `string path` now take `string text`.

### Steps

- [x] Implement `PreceptCompiler.CompileFromText(text)` in core (`src/Precept/Dsl/`) — already done in Language Phase I
- [x] Add `ConstraintSeverity.Hint` to the severity enum — already done in Language Phase I
- [x] Implement graph analysis (C48–C53) in `PreceptAnalysis.Analyze()` and consume the structured validation result instead of exception parsing — already done in Language Phase I
- [x] Wire graph analysis into `CompileFromText` so warnings appear alongside type-check errors — already done in Language Phase I
- [x] Create `ViolationDto.cs` at `tools/Precept.Mcp/Tools/ViolationDto.cs` — shared between inspect and run
- [x] Implement `CompileTool.cs` with inline `DiagnosticDto` and schema DTOs — merges validate + schema + audit; returns full model + diagnostics
- [x] Rewrite `InspectTool.cs` — delegate to `engine.Inspect()`, echo resolved snapshot, compile failure returns error string
- [x] Rewrite `RunTool.cs` — single-event with state+data+event+args input, structured `ViolationDto` output, compile failure returns error string
- [x] Delete `ValidateTool.cs`, `SchemaTool.cs`, `AuditTool.cs`
- [x] Rewrite test files for the new 4-tool surface
- [x] Update `Program.cs` if any registration changes are needed
- [x] Verify language server diagnostic mapping still works (LS calls same core APIs)

### Tests

- [x] `precept_compile`: valid input → full schema + zero diagnostics
- [x] `precept_compile`: type errors → partial schema + error-severity diagnostics with codes
- [x] `precept_compile`: parse failure → diagnostics only, no schema
- [x] `precept_compile`: unreachable state → warning-severity diagnostic (C48)
- [x] `precept_compile`: orphaned event → warning-severity diagnostic (C49)
- [x] `precept_compile`: dead-end state → hint-severity diagnostic (C50)
- [x] `precept_inspect`: text input + state + data → structured event outcomes with `ViolationDto`
- [x] `precept_inspect`: requires-args detection → `requiredArgs` reported
- [x] `precept_inspect`: compile errors in text → error string, no runtime results
- [x] `precept_inspect`: echoes resolved instance snapshot with defaults applied
- [x] `precept_fire`: single event execution → outcome with `ViolationDto` array
- [x] `precept_fire`: compile errors in text → error string, no execution
- [x] `precept_fire`: constraint failure → structured violations on the result
- [x] `precept_fire`: echoes resolved data snapshot with defaults applied
- [x] `precept_language`: unchanged behavior (regression)

### Checkpoint

- `dotnet build` passes (entire solution)
- All MCP tool tests green with new 4-tool surface (then expanded to 5-tool in the Update Tool Phase)
- `precept_compile` returns warnings and hints alongside errors
- `ViolationDto` preserves full `ConstraintViolation` structure (source hierarchy + targets)
- `precept_inspect` and `precept_fire` return error string on compile failure (not diagnostics)
- `precept_fire` is single-event (no batch)
- `precept_inspect` echoes resolved instance snapshot
- No domain logic in tool files beyond DTO projection

### Redesign implementation prompt

Use this prompt to execute the redesign phase in a new Copilot Chat session:

> Implement the MCP redesign in `docs/McpServerImplementationPlan.md` under `MCP Redesign Phase: 6→4 Tools with Text Input`, using `docs/McpServerDesign.md` as the external contract.
>
> Prerequisite: `docs/PreceptLanguageImplementationPlan.md` Phase I must already be complete, including C48–C53 diagnostics, `ConstraintSeverity.Hint`, `PreceptAnalysis.Analyze()`, structured compile validation, and `PreceptCompiler.CompileFromText(string text)`.
>
> Before making changes, read the redesign phase and the linked design doc in full, then pause and recommend the most appropriate model for the work. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models in Copilot if desired, then continue.
>
> Then:
>
> 1. Replace the old validate/schema/audit tool surface with `precept_compile` (inline `DiagnosticDto` + schema DTOs).
> 2. Keep tool files as thin wrappers; domain logic belongs in `src/Precept/`.
> 3. Use text input for all file-backed tools.
> 4. Create shared `ViolationDto.cs` at `Tools/` level (used by both inspect and run).
> 5. Rewrite `InspectTool.cs` to delegate to `engine.Inspect()`, echo resolved snapshot, return error string on compile failure.
> 6. Rewrite `RunTool.cs` as single-event execution with state+data+event+args input, structured `ViolationDto` output, error string on compile failure.
> 7. Delete obsolete tool files after replacement coverage exists.
> 8. Rewrite MCP tests to match the new 4-tool surface.
> 9. End with `dotnet build` and all MCP tests passing.

---

## Phase 7: Agent Plugin Structure + MCP Packaging

**Goal:** Create the agent plugin directory structure with MCP server binaries, so the five tools are callable by Copilot with zero manual setup after plugin installation.

### Steps

- [x] Create plugin directory structure at `tools/Precept.Plugin/`:
  ```
  tools/Precept.Plugin/
  ├── .claude-plugin/plugin.json
  ├── agents/
  ├── skills/
  │   ├── precept-authoring/
  │   └── precept-debugging/
  ├── .mcp.json
  └── README.md
  ```
- [x] Create `.claude-plugin/plugin.json` with name, description, version, agents, and skills arrays *(Claude format)*
- [x] Create `.mcp.json` with the distribution command (`dotnet tool run precept-mcp`) — works across both VS Code plugin system and Copilot CLI
- [x] Create `.vscode/mcp.json` with the dev launcher script override (`tools/scripts/start-precept-mcp.js`) — overrides the plugin's MCP config during development for lazy build + shadow copy
- [x] Move the MCP launcher script from `tools/Precept.VsCode/scripts/start-precept-mcp.js` to `tools/scripts/start-precept-mcp.js`
- [x] Remove the `Precept Dev` entry from `.vscode/mcp.json` (the plugin now provides the MCP server)
- [x] Create `tools/scripts/toggle-plugin.js` — reads/writes `chat.pluginLocations` in `.vscode/settings.json` *(later superseded by workspace-native `.github/agents/` and `.github/skills/` plus `plugin: sync payload` for worktree-neutral local development)*
- [x] Add `plugin: enable` and `plugin: disable` tasks to `.vscode/tasks.json` *(later superseded by `plugin: sync payload` for explicit plugin payload validation)*
- [x] Rename existing extension tasks: `extension: loop local install` → `extension: install`, `extension: loop local uninstall` → `extension: uninstall` *(already implemented)*
- [x] Remove the `extension: watch` task (unused in the local install loop) *(already implemented)*
- [x] Test locally using `chat.pluginLocations` setting pointing to the plugin directory *(historical path; superseded by workspace-native `.github/` customizations for local development)*
- [x] Verify Copilot lists all 5 precept tools from the plugin's MCP server
- [ ] Test with at least 2 sample files per tool

### Checkpoint

- Plugin loads without errors when registered via `chat.pluginLocations` *(historical path; superseded by workspace-native `.github/` customizations for local development)*
- Copilot lists 5 precept tools from the plugin's MCP server
- `precept_compile`, `precept_inspect`, `precept_fire`, `precept_update` produce correct results
- Dev tasks (`extension: install`, `extension: uninstall`) work correctly; the original plugin enable/disable tasks were later superseded by `plugin: sync payload`
- `Precept Dev` entry no longer present in `.vscode/mcp.json`

### Phase 7 implementation prompt

Use this prompt to execute Phase 7 in a new Copilot Chat session:

> Implement Phase 7 in `docs/McpServerImplementationPlan.md`: `Agent Plugin Structure + MCP Packaging`.
>
> Before making changes, read the phase section and the related plugin/distribution guidance in full, then pause and recommend the most appropriate model for the work. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models in Copilot if desired, then continue.
>
> Create the plugin directory structure under `tools/Precept.Plugin/`, add `plugin.json` at the plugin root, add the dev `.mcp.json` using `${CLAUDE_PLUGIN_ROOT}` path expansion, move the MCP launcher to `tools/scripts/start-precept-mcp.js`, and remove the `Precept Dev` entry from `.vscode/mcp.json` (the plugin replaces it).
>
> Keep the existing completed task changes intact (historical note: `toggle-plugin.js` and plugin enable/disable tasks were later superseded by workspace-native `.github/` customizations plus `plugin: sync payload`). Only add the remaining missing structure and verify local loading.
>
> End with local verification that Copilot lists the Precept tools from the plugin MCP server.

---

## Phase 8: Agent and Skill Content

**Goal:** Draft and test the Precept Author agent and two companion skills that ship inside the agent plugin.

### Steps

- [x] Draft `agents/precept-author.agent.md` with:
    - YAML frontmatter: name, description, tools restricted to `read`, `edit`, `search`, `fetch`, and all `precept/*` MCP tools
    - Body: lightweight persona, skill-routing hints, and a small set of cross-cutting guardrails
    - The agent body is intentionally thin: it owns persona, routing, and tool restrictions; detailed workflows live in the companion skills (auto-discovered by VS Code based on the user's request)
- [x] Draft `skills/precept-authoring/SKILL.md` with:
    - Frontmatter: name `precept-authoring`, description with explicit trigger phrases
    - Body: step-by-step creation/editing workflow using MCP tools in prescribed, gated order
    - Repo-agnostic: no assumption that `samples/` exists; prefer local `.precept` files for convention matching, fall back to `precept_language`
    - Mermaid guidance: optionally include a `stateDiagram-v2` diagram when it helps the user understand the resulting state machine; use `precept_compile` for transition data
- [x] Draft `skills/precept-debugging/SKILL.md` with:
    - Frontmatter: name `precept-debugging`, description with explicit trigger phrases
    - Body: diagnosis workflow using gated `precept_compile` → `precept_inspect` → `precept_fire` when each later step is still needed
    - Mermaid guidance: optionally include a focused `stateDiagram-v2` showing only the relevant subset; annotate guards in brackets, mark reject branches, annotate warning/hint findings (unreachable/dead-end states)
- [x] Validate all files against the [Agent Skills specification](https://agentskills.io/specification):
    - `name` lowercase kebab-case, matches parent directory, max 64 chars
    - `description` max 1024 chars, specific and trigger-oriented
    - `SKILL.md` body under 500 lines
- [ ] Test the agent by selecting it from the agents dropdown and authoring a precept from scratch
- [ ] Test skills via `/precept-authoring` and `/precept-debugging` slash commands
- [ ] Verify the agent and skills discover and invoke MCP tools in the correct order

### Checkpoint

- The agent appears in the agents dropdown when plugin is loaded locally
- Both skills appear in the `/` slash command menu
- Agent correctly restricts its tool set and follows the prescribed workflow
- Skills are valid markdown with valid YAML frontmatter per agentskills.io spec

### Phase 8 implementation prompt

Use this prompt to execute Phase 8 in a new Copilot Chat session:

> Implement Phase 8 in `docs/McpServerImplementationPlan.md`: `Agent and Skill Content`.
>
> Before making changes, read the phase section and the linked agent/skill specifications in full, then pause and recommend the most appropriate model for the work. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models in Copilot if desired, then continue.
>
> Draft the Precept Author agent (`precept-author.agent.md`) plus the `precept-authoring` and `precept-debugging` skills inside `tools/Precept.Plugin/`. The agent should be a thin persona with tool restrictions; detailed workflows belong in the skills (VS Code auto-discovers them). Follow the linked agent and skill specs exactly. Keep the workflow opinionated: `precept_language` is the DSL authority, and compile/inspect/fire are used in that order when diagnosing behavior.
>
> Include Mermaid guidance exactly as described in the phase steps. Validate frontmatter, naming, and line-count constraints before finishing.
>
> End with local validation that the agent appears in the dropdown, both skills appear in slash commands, and the tool invocation order is correct.

---

## Phase 9: Documentation + Distribution

**Goal:** README and design docs reflect the agent plugin distribution model. Plugin is published to a distribution channel.

### Steps

- [x] Update `README.md`:
  - Add MCP Server section describing the 5 tools and how to use them
    - Add setup instructions: install the Precept agent plugin (marketplace or Git URL)
    - Document the Precept Author agent and companion skills
  - Update Current Status to include MCP server and agent plugin
- [x] Update `docs/McpServerDesign.md`:
    - Ensure plugin distribution model is accurately documented
    - Update to reflect `.claude-plugin/` format (required for `${CLAUDE_PLUGIN_ROOT}` expansion)
- [ ] Verify `docs/CatalogInfrastructureDesign.md` cross-references are still accurate
- [x] Remove `registerMcpServerDefinitionProvider()` from VS Code extension (MCP now lives in plugin)
- [x] Remove `mcpServerDefinitionProviders` contribution from `tools/Precept.VsCode/package.json`
- [x] Update distribution `.mcp.json` to use `dotnet tool run precept-mcp` (plugin's `.mcp.json` now uses dist format directly; `.mcp.dist.json` removed; `.vscode/mcp.json` provides dev override)
- [x] Configure `Precept.Mcp.csproj` as a .NET tool (`PackAsTool`, `ToolCommandName: precept-mcp`)
- [x] Add bundled language server mode to VS Code extension (`server/` directory, framework-dependent)
- [x] Add `package:marketplace` script for building marketplace VSIX with bundled language server
- [ ] Publish plugin to a distribution channel (Git repo for direct install, and/or submit to `awesome-copilot`)
- [ ] Verify MCP sync rule in `.github/copilot-instructions.md` is still accurate (already exists with correct post-rename names; do not overwrite with stale `Dsl*` prefixes)

### Checkpoint

- README accurately describes all 5 tools
- README accurately describes agent plugin installation and Precept Author agent
- No aspirational claims presented as implemented
- Design doc documents plugin distribution model accurately
- Copilot-instructions MCP Tool Sync section is verified accurate
- Extension no longer registers MCP server provider
- Plugin is published and installable

### Phase 9 implementation prompt

Use this prompt to execute Phase 9 in a new Copilot Chat session:

> Implement Phase 9 in `docs/McpServerImplementationPlan.md`: `Documentation + Distribution`.
>
> Before making changes, read the phase section and all referenced distribution/documentation guidance in full, then pause and recommend the most appropriate model for the work. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models in Copilot if desired, then continue.
>
> Update `README.md`, `docs/McpServerDesign.md`, and any affected cross-references so they describe the plugin-based MCP distribution model accurately. Remove MCP server registration from the VS Code extension, remove the local `Precept Dev` MCP entry, and convert the plugin distribution `.mcp.json` to the published form.
>
> Follow `.github/copilot-instructions.md` for mandatory documentation sync and MCP tool sync guidance. Do not leave aspirational claims in the docs.
>
> End with a consistency pass: docs match implementation, extension no longer owns MCP registration, and the plugin is ready for publication.

---

## File Change Summary

| File | Action | Phase |
|---|---|---|
| `tools/Precept.Mcp/Precept.Mcp.csproj` | **New** | 0 |
| `tools/Precept.Mcp/Program.cs` | **New** | 0 |
| `tools/Precept.Mcp/Tools/ValidateTool.cs` | **New** → **Delete** | 1 → Redesign |
| `tools/Precept.Mcp/Tools/SchemaTool.cs` | **New** → **Delete** | 2 → Redesign |
| `tools/Precept.Mcp/Tools/AuditTool.cs` | **New** → **Delete** | 3 → Redesign |
| `tools/Precept.Mcp/Tools/RunTool.cs` | **New** → **Rewrite** → **Rename** to `FireTool.cs` | 4 → Redesign → Update Tool |
| `tools/Precept.Mcp/Tools/FireTool.cs` | **Renamed** from `RunTool.cs` — `precept_fire` single-event execution | Update Tool |
| `tools/Precept.Mcp/Tools/UpdateTool.cs` | **New** — `precept_update` direct field editing | Update Tool |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | **New** (unchanged in redesign) | 5 |
| `tools/Precept.Mcp/Tools/InspectTool.cs` | **New** → **Rewrite** (delegate to engine.Inspect) | 6 → Redesign |
| `tools/Precept.Mcp/Tools/CompileTool.cs` | **New** — merges validate + schema + audit; inline DiagnosticDto + schema DTOs | Redesign |
| `tools/Precept.Mcp/Tools/ViolationDto.cs` | **New** — shared violation DTO (inspect, fire, and update) | Redesign |
| `src/Precept/Dsl/PreceptAnalysis.cs` | **New** — graph analysis (C48–C53) | Redesign (Language Phase I) |
| `src/Precept/Dsl/PreceptCompiler.cs` | **Edit** — add `CompileFromText(text)` | Redesign (Language Phase I) |
| `test/Precept.Mcp.Tests/Precept.Mcp.Tests.csproj` | **New** | 0 |
| `test/Precept.Mcp.Tests/ValidateToolTests.cs` | **New** → **Delete** | 1 → Redesign |
| `test/Precept.Mcp.Tests/SchemaToolTests.cs` | **New** → **Delete** | 2 → Redesign |
| `test/Precept.Mcp.Tests/AuditToolTests.cs` | **New** → **Delete** | 3 → Redesign |
| `test/Precept.Mcp.Tests/RunToolTests.cs` | **New** → **Rewrite** → **Rename** to `FireToolTests.cs` | 4 → Redesign → Update Tool |
| `test/Precept.Mcp.Tests/FireToolTests.cs` | **Renamed** from `RunToolTests.cs` | Update Tool |
| `test/Precept.Mcp.Tests/UpdateToolTests.cs` | **New** | Update Tool |
| `test/Precept.Mcp.Tests/LanguageToolTests.cs` | **New** (unchanged in redesign) | 5 |
| `test/Precept.Mcp.Tests/InspectToolTests.cs` | **New** → **Rewrite** | 6 → Redesign |
| `test/Precept.Mcp.Tests/CompileToolTests.cs` | **New** | Redesign |
| `tools/Precept.VsCode/package.json` | **Edit** — remove MCP provider contributions | 9 |
| `tools/Precept.VsCode/src/extension.ts` | **Edit** — remove MCP provider registration | 9 |
| `tools/Precept.Plugin/.claude-plugin/plugin.json` | **New** — plugin metadata (Claude format) | 7 |
| `tools/Precept.Plugin/.mcp.json` | **New** — MCP server definition (dev: launcher, dist: dotnet tool) | 7 |
| `tools/Precept.Plugin/agents/precept-author.md` | **New** — Precept Author custom agent | 8 |
| `tools/Precept.Plugin/skills/precept-authoring/SKILL.md` | **New** — authoring workflow skill | 8 |
| `tools/Precept.Plugin/skills/precept-debugging/SKILL.md` | **New** — debugging workflow skill | 8 |
| `tools/Precept.Plugin/README.md` | **New** — plugin documentation | 8 |
| `tools/scripts/start-precept-mcp.js` | **Move** from `tools/Precept.VsCode/scripts/` | 7 |
| `tools/scripts/toggle-plugin.js` | **New** — toggle `chat.pluginLocations` in workspace settings *(later removed when local development moved to workspace-native `.github/` customizations)* | 7 |
| `.vscode/tasks.json` | **Edit** — rename extension tasks, remove watch, add plugin tasks | 7 |
| `.vscode/mcp.json` | **Edit** — update launcher path; remove in Phase 9 | 7, 9 |
| `Precept.slnx` | **Edit** — add `Precept.Mcp` project | 0 |
| `README.md` | **Edit** — MCP server and Copilot skill setup section | 9 |
| `docs/McpServerDesign.md` | **Edit** — packaging and skill delivery design | 9 |
| `.github/copilot-instructions.md` | **Edit** — add MCP Tool Sync section | 9 |

## Dependency Graph

```
Phase 0: Scaffolding (no core dependency beyond Precept.csproj)
    ↓
Phase 1: precept_validate (PreceptParser + PreceptCompiler)
    ↓
Phase 2: precept_schema (DslWorkflowModel records)
    ↓
Phase 3: precept_audit (DslWorkflowModel graph analysis)
    ↓
Phase 4: precept_run (PreceptEngine — full runtime)
    ↓
Phase 5: precept_language (catalogs — zero file dependency, could be any order)
    ↓
Phase 6: precept_inspect (PreceptEngine.Inspect — builds on Phase 4's runtime knowledge)
    ↓
MCP Redesign: 6→4 tools (requires Language Phase I for structured validation + C48–C53 diagnostics)
    ↓
Update Tool Phase: precept_update + precept_fire rename + inspect thin-wrapper fixes (requires MCP Redesign)
    ↓
Phase 7: Agent plugin structure + MCP packaging (5 tools)
    ↓
Phase 8: Agent and skill content (can be drafted in parallel with Redesign)
    ↓
Phase 9: Documentation + distribution
```

Phases 0–6, the MCP Redesign Phase, and the Update Tool Phase are complete — the 5-tool surface (language, compile, inspect, fire, update) is implemented and tested. Phase 7 requires the 5-tool surface. Phase 8 (agent/skill markdown) can be drafted in parallel but testing requires the redesigned tools. Phase 9 depends on both 7 and 8.

## Estimated Scope

| Phase | New LOC (est.) | Risk | Status |
|---|---|---|---|
| 0. Scaffolding | ~30 | None | ✅ Done |
| 1. `precept_validate` | ~80 | Low | ✅ Done |
| 2. `precept_schema` | ~120 | Low (model walking) | ✅ Done |
| 3. `precept_audit` | ~150 | Low-Medium (graph BFS) | ✅ Done |
| 4. `precept_run` | ~100 | Low (thin wrapper over engine) | ✅ Done |
| 5. `precept_language` | ~200 | Low (reflection + static data) | ✅ Done |
| 6. `precept_inspect` | ~130 | Low-Medium (arg detection logic) | ✅ Done |
| MCP Redesign | ~350 | Medium (core API + DTO + rewrite 4 tools + tests) | ✅ Done |
| Update Tool Phase | ~180 | Low-Medium (rename + refactor + new tool + shared DTOs) | ✅ Done |
| 7. Agent plugin structure | ~80 | Low-Medium (launcher move + toggle script) | Not started |
| 8. Agent and skill content | ~300 | Medium (prompt engineering) | Not started |
| 9. Documentation + distribution | ~180 | Low | Not started |
| **Total** | **~1,720** | |

---

## Execution note

The MCP plan now keeps implementation prompts inside the relevant execution phases:

- Use `MCP Redesign Phase: 6→4 Tools with Text Input` for the tool-surface migration prompt.
- Use `Phase 7 implementation prompt` for plugin structure and MCP packaging.
- Use `Phase 8 implementation prompt` for agent and skill content.
- Use `Phase 9 implementation prompt` for documentation and distribution.

Cross-phase rules still apply everywhere:

1. Keep MCP tools as thin wrappers over core APIs.
2. Return structured JSON from tools.
3. Keep tests in sync in the same change pass.
4. End each phase with build/test verification before moving on.
5. Keep docs synchronized with implementation in the same edit pass.
