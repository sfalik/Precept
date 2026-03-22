# Precept MCP Server — Implementation Plan

Date: 2025-03-05
Spec: `docs/McpServerDesign.md`
Prerequisite: Language redesign complete (`docs/PreceptLanguageImplementationPlan.md` — all 9 phases)

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
5. `deadEndStates` = non-terminal states where all outgoing rows have `NoTransition` or `Rejection` outcomes only.
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

- [ ] Happy path (all steps Accepted) → correct final state/data, `abortedAt` null
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

## Phase 7: Agent Plugin Structure + MCP Packaging

**Goal:** Create the agent plugin directory structure with MCP server binaries, so the six tools are callable by Copilot with zero manual setup after plugin installation.

### Steps

- [ ] Create plugin directory structure at `tools/Precept.Plugin/`:
  ```
  tools/Precept.Plugin/
  ├── .github/plugin/
  │   └── plugin.json
  ├── agents/
  ├── skills/
  │   ├── precept-authoring/
  │   └── precept-debugging/
  ├── .mcp.json
  └── README.md
  ```
- [ ] Create `plugin.json` with name, description, version, agents, and skills arrays
- [ ] Create dev `.mcp.json` referencing the shared launcher script:
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
- [ ] Move the MCP launcher script from `tools/Precept.VsCode/scripts/start-precept-mcp.js` to `tools/scripts/start-precept-mcp.js` and update `.vscode/mcp.json` to reference the new location
- [x] Create `tools/scripts/toggle-plugin.js` — reads/writes `chat.pluginLocations` in `.vscode/settings.json` *(already implemented)*
- [x] Add `plugin: enable` and `plugin: disable` tasks to `.vscode/tasks.json` *(already implemented)*
- [x] Rename existing extension tasks: `extension: loop local install` → `extension: install`, `extension: loop local uninstall` → `extension: uninstall` *(already implemented)*
- [x] Remove the `extension: watch` task (unused in the local install loop) *(already implemented)*
- [ ] Test locally using `chat.pluginLocations` setting pointing to the plugin directory
- [ ] Verify Copilot lists all 6 precept tools from the plugin's MCP server
- [ ] Test with at least 2 sample files per tool

### Checkpoint

- Plugin loads without errors when registered via `chat.pluginLocations`
- Copilot lists 6 precept tools from the plugin's MCP server
- `precept_validate`, `precept_schema`, and `precept_run` produce correct results
- Dev tasks (`extension: install`, `extension: uninstall`, `plugin: enable`, `plugin: disable`) work correctly
- `.vscode/mcp.json` references the moved launcher at `tools/scripts/start-precept-mcp.js`

---

## Phase 8: Agent and Skill Content

**Goal:** Draft and test the Precept Author agent and two companion skills that ship inside the agent plugin.

### Steps

- [ ] Draft `agents/precept-author.md` with:
    - YAML frontmatter: name, description, tools restricted to `read`, `edit`, `search`, and all `precept/*` MCP tools
    - Body instructions: opinionated authoring workflow, `precept_language` as DSL authority, mandatory validate → audit → inspect loop
    - Handoff to debugging skill for diagnosis workflows
- [ ] Draft `skills/precept-authoring/SKILL.md` with:
    - Frontmatter: name `precept-authoring`, description with explicit trigger phrases
    - Body: step-by-step creation/editing workflow using MCP tools in prescribed order
    - Repo-agnostic: no assumption that `samples/` exists; prefer local `.precept` files for convention matching, fall back to `precept_language`
    - Mermaid Diagrams section: after creating/editing a precept, include a `stateDiagram-v2` diagram of the resulting state machine; use `precept_schema` for transition data
- [ ] Draft `skills/precept-debugging/SKILL.md` with:
    - Frontmatter: name `precept-debugging`, description with explicit trigger phrases
    - Body: diagnosis workflow using `precept_schema` → `precept_validate` → `precept_audit` → `precept_inspect` → `precept_run`
    - Mermaid Diagrams section: when explaining structure or transition behavior, include a focused `stateDiagram-v2` showing only the relevant subset; annotate guards in brackets, mark reject branches, annotate audit findings (unreachable/dead-end states)
- [ ] Validate all files against the [Agent Skills specification](https://agentskills.io/specification):
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

---

## Phase 9: Documentation + Distribution

**Goal:** README and design docs reflect the agent plugin distribution model. Plugin is published to a distribution channel.

### Steps

- [ ] Update `README.md`:
  - Add MCP Server section describing the 6 tools and how to use them
    - Add setup instructions: install the Precept agent plugin (marketplace or Git URL)
    - Document the Precept Author agent and companion skills
  - Update Current Status to include MCP server and agent plugin
- [ ] Update `docs/McpServerDesign.md`:
    - Ensure plugin distribution model is accurately documented
- [ ] Verify `docs/CatalogInfrastructureDesign.md` cross-references are still accurate
- [ ] Remove `registerMcpServerDefinitionProvider()` from VS Code extension (MCP now lives in plugin)
- [ ] Remove `mcpServerDefinitionProviders` contribution from `tools/Precept.VsCode/package.json`
- [ ] Remove `Precept Dev` entry from `.vscode/mcp.json` (MCP is now provided by the plugin)
- [ ] Update distribution `.mcp.json` to use `dotnet tool run precept-mcp` (CI rewrites the dev launcher form)
- [ ] Publish plugin to a distribution channel (Git repo for direct install, and/or submit to `awesome-copilot`)
- [ ] Add MCP sync rule to `.github/copilot-instructions.md`:

**MCP Tool Sync** (new section in copilot-instructions):
- When core model types change (`DslWorkflowModel`, `DslField`, `DslState`, `DslEvent`, `DslTransitionRow`, etc.), check whether MCP tool DTOs in `tools/Precept.Mcp/Tools/` need corresponding updates.
- When `ConstructCatalog` or `DiagnosticCatalog` records gain or lose properties, verify `LanguageTool.cs` serialization still matches `McpServerDesign.md § precept_language` output format.
- When the fire pipeline stages change, update the static `firePipeline` array in `LanguageTool.cs`.
- The MCP tools are **thin wrappers** — never duplicate domain logic. If a tool method exceeds ~30 lines of non-serialization code, the logic probably belongs in `src/Precept/`.

### Checkpoint

- README accurately describes all 6 tools
- README accurately describes agent plugin installation and Precept Author agent
- No aspirational claims presented as implemented
- Design doc documents plugin distribution model accurately
- Copilot-instructions includes MCP Tool Sync section
- Extension no longer registers MCP server provider
- Plugin is published and installable

---

## File Change Summary

| File | Action | Phase |
|---|---|---|
| `tools/Precept.Mcp/Precept.Mcp.csproj` | **New** | 0 |
| `tools/Precept.Mcp/Program.cs` | **New** | 0 |
| `tools/Precept.Mcp/Tools/ValidateTool.cs` | **New** | 1 |
| `tools/Precept.Mcp/Tools/SchemaTool.cs` | **New** | 2 |
| `tools/Precept.Mcp/Tools/AuditTool.cs` | **New** | 3 |
| `tools/Precept.Mcp/Tools/RunTool.cs` | **New** | 4 |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | **New** | 5 |
| `tools/Precept.Mcp/Tools/InspectTool.cs` | **New** | 6 |
| `test/Precept.Mcp.Tests/Precept.Mcp.Tests.csproj` | **New** | 0 |
| `test/Precept.Mcp.Tests/ValidateToolTests.cs` | **New** | 1 |
| `test/Precept.Mcp.Tests/SchemaToolTests.cs` | **New** | 2 |
| `test/Precept.Mcp.Tests/AuditToolTests.cs` | **New** | 3 |
| `test/Precept.Mcp.Tests/RunToolTests.cs` | **New** | 4 |
| `test/Precept.Mcp.Tests/LanguageToolTests.cs` | **New** | 5 |
| `test/Precept.Mcp.Tests/InspectToolTests.cs` | **New** | 6 |
| `tools/Precept.VsCode/package.json` | **Edit** — remove MCP provider contributions | 9 |
| `tools/Precept.VsCode/src/extension.ts` | **Edit** — remove MCP provider registration | 9 |
| `tools/Precept.Plugin/.github/plugin/plugin.json` | **New** — plugin metadata | 7 |
| `tools/Precept.Plugin/.mcp.json` | **New** — MCP server definition (dev: launcher, dist: dotnet tool) | 7 |
| `tools/Precept.Plugin/agents/precept-author.md` | **New** — Precept Author custom agent | 8 |
| `tools/Precept.Plugin/skills/precept-authoring/SKILL.md` | **New** — authoring workflow skill | 8 |
| `tools/Precept.Plugin/skills/precept-debugging/SKILL.md` | **New** — debugging workflow skill | 8 |
| `tools/Precept.Plugin/README.md` | **New** — plugin documentation | 8 |
| `tools/scripts/start-precept-mcp.js` | **Move** from `tools/Precept.VsCode/scripts/` | 7 |
| `tools/scripts/toggle-plugin.js` | **New** — toggle `chat.pluginLocations` in workspace settings | 7 |
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
Phase 7: Agent plugin structure + MCP packaging
    ↓
Phase 8: Agent and skill content (can be drafted in parallel with 1-6)
    ↓
Phase 9: Documentation + distribution
```

Phases 5 and 6 are interchangeable. Phase 5 (`precept_language`) has no dependency on any other tool and could be implemented at any point after Phase 0. It's placed here so the more commonly used tools (`validate`, `schema`, `audit`, `run`) are built first.

Phase 8 (agent and skill content) is markdown-only work that can proceed in parallel with any of Phases 1-6. However, testing the skills against MCP tools requires Phase 6 to be complete. Phase 7 requires the MCP binary build from Phase 6. Phase 9 depends on both 7 and 8.

## Estimated Scope

| Phase | New LOC (est.) | Risk |
|---|---|---|
| 0. Scaffolding | ~30 | None |
| 1. `precept_validate` | ~80 | Low |
| 2. `precept_schema` | ~120 | Low (model walking) |
| 3. `precept_audit` | ~150 | Low-Medium (graph BFS) |
| 4. `precept_run` | ~100 | Low (thin wrapper over engine) |
| 5. `precept_language` | ~200 | Low (reflection + static data) |
| 6. `precept_inspect` | ~130 | Low-Medium (arg detection logic) |
| 7. Agent plugin structure + MCP packaging | ~80 | Low-Medium (launcher move + toggle script) |
| 8. Agent and skill content | ~300 | Medium (prompt engineering) |
| 9. Documentation + distribution | ~180 | Low |
| **Total** | **~1,210** | |

---

## Implementation Prompt

Use this prompt to begin implementation in a new Copilot Chat session:

> Implement the Precept MCP server described in `docs/McpServerDesign.md`, following the phased plan in `docs/McpServerImplementationPlan.md`.
>
> Start by reading both documents in full, plus `docs/CatalogInfrastructureDesign.md` for the catalog architecture.
>
> Prerequisites: The language redesign (`docs/PreceptLanguageImplementationPlan.md`) must be complete — the Superpower parser, catalogs, and runtime are assumed to exist in `src/Precept/`.
>
> Then:
>
> 1. Execute the phases in order (0 through 9), committing at each checkpoint.
> 2. Each phase must end with `dotnet build` and `dotnet test` passing before moving to the next.
> 3. Tools are **thin wrappers** — all domain logic lives in `src/Precept/`. The MCP project calls `PreceptParser`, `PreceptCompiler`, `PreceptEngine`, `ConstructCatalog`, and `DiagnosticCatalog` directly.
> 4. Every tool returns structured JSON — no prose-only responses.
> 5. Every tool has integration tests in `test/Precept.Mcp.Tests/` that call the tool method directly (no MCP transport).
> 6. For `precept_language` (Phase 5), vocabulary comes from reflecting token enum attributes, constructs from `ConstructCatalog.Constructs`, and constraints from `DiagnosticCatalog.Constraints`. The static sections (`expressionScopes`, `firePipeline`, `outcomeKinds`) are constant arrays in the tool class.
> 7. For the agent plugin (Phase 7), create the plugin directory structure at `tools/Precept.Plugin/` with `plugin.json` and dev `.mcp.json` (launcher-based). Move the MCP launcher script to `tools/scripts/start-precept-mcp.js`. Create the `toggle-plugin.js` script and add `plugin: enable`/`plugin: disable` tasks. Rename extension tasks (`extension: install`, `extension: uninstall`) and remove `extension: watch`.
> 8. For agent and skill content (Phase 8), follow the [Agent Skills specification](https://agentskills.io/specification) for SKILL.md format and the [VS Code custom agents docs](https://code.visualstudio.com/docs/copilot/customization/custom-agents) for the agent file format.
> 9. In Phase 9, remove the MCP server registration from the VS Code extension (`registerMcpServerDefinitionProvider` and `mcpServerDefinitionProviders` in package.json), remove `Precept Dev` from `.vscode/mcp.json`, rewrite the plugin's `.mcp.json` for distribution (`dotnet tool run precept-mcp`), and update README and design docs.
> 10. Follow the project's copilot-instructions (`.github/copilot-instructions.md`) for documentation sync.
>
> Begin with Phase 0: create the project scaffolding and add it to the solution.
