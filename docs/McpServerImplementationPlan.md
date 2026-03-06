# Precept MCP Server — Implementation Plan

Date: 2025-03-05
Spec: `docs/McpServerDesign.md`
Prerequisite: Language redesign complete (`docs/PreceptLanguageImplementationPlan.md` — all 9 phases)

This plan builds the MCP server as a new project in `tools/Precept.Mcp/`. Each phase adds one tool, fully tested, before moving to the next. The core `src/Precept/` infrastructure (catalogs, parser, runtime) is already in place from the language redesign — this plan only adds tool wrappers and MCP transport.

---

## Guiding Principles

1. **One tool per phase.** Each tool is independently useful. Ship order matches dependency order — earlier tools inform later tools' behavior.
2. **Thin wrappers.** Tools call existing core APIs (`PreceptParser`, `PreceptCompiler`, `PreceptEngine`, catalogs). No domain logic in the MCP project.
3. **Structured JSON output.** Every tool returns well-typed JSON so Copilot can reason about results programmatically — no prose-only responses.
4. **Test every tool in isolation.** Each tool gets integration tests that call the tool method directly (no MCP transport overhead). Asserts on JSON shape and semantic correctness.

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

**Goal:** Parse + compile a `.precept` file, return structured diagnostics.

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

Per `McpServerDesign.md § precept_validate`. Input: `{ "path": "..." }`. Output: `{ "valid", "machineName", "stateCount", "eventCount", "diagnostics" }`.

### Tests

- [ ] Valid file → `valid: true`, zero diagnostics
- [ ] Syntax error → `valid: false`, diagnostic with line number
- [ ] Compile-time constraint violation → `valid: false`, diagnostic with constraint ID
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
5. If a step's outcome is `Rejected`, `NotDefined`, or `NotApplicable`, set `abortedAt` and stop.
6. Return the full step log + `finalState` + `finalData`.

### Input/Output

Per `McpServerDesign.md § precept_run`. Input: `{ path, initialData?, steps }`. Output: `{ steps[], finalState, finalData, abortedAt }`.

### Tests

- [ ] Happy path (all steps Accepted) → correct final state/data, `abortedAt` null
- [ ] Step rejected mid-sequence → `abortedAt` set, earlier steps present, later steps absent
- [ ] Step with `NotDefined` → aborted with explanation
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
| `constraints` | Serialize `ConstraintCatalog.Constraints` directly (ID, phase, rule). |
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
- [ ] `constraints` array matches `ConstraintCatalog.Constraints` count
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
   - Map the `DslFireResult` to `{ event, outcome, resultState?, resultData?, reason? }`.
4. Sort results: actionable outcomes first (Accepted/AcceptedInPlace), then unavailable (NotDefined/NotApplicable/Rejected), then `requiresArgs`.

### Input/Output

Per `McpServerDesign.md § precept_inspect`. Input: `{ path, currentState, data, eventArgs? }`. Output: `{ currentState, events[] }`.

### Tests

- [ ] Event that transitions → `outcome: "Accepted"`, correct `resultState`
- [ ] Event not defined for state → `outcome: "NotDefined"`, `reason` present
- [ ] Event with guard that doesn't match → `outcome: "NotApplicable"`
- [ ] Event with required args not supplied → `requiresArgs: true`, arg list present
- [ ] Event with args supplied via `eventArgs` → evaluated with those args
- [ ] Result ordering: actionable before unavailable before requiresArgs

### Checkpoint

- `dotnet build` passes
- All tool tests green

---

## Phase 7: VS Code Registration + Integration

**Goal:** The six tools are callable by Copilot in agent mode from VS Code with zero manual setup.

### Steps

- [ ] Add MCP server entry to `tools/Precept.VsCode/package.json`:

```json
"mcpServers": {
  "precept": {
    "command": "dotnet",
    "args": ["run", "--project", "${workspaceFolder}/tools/Precept.Mcp"],
    "type": "stdio"
  }
}
```

- [ ] Verify `dotnet run --project tools/Precept.Mcp` starts cleanly and responds to MCP `initialize` + `tools/list` requests
- [ ] End-to-end test: in a VS Code window with the extension loaded, Copilot can list and call all six tools
- [ ] Verify tool descriptions appear in Copilot's tool list
- [ ] Test with at least 2 sample files per tool

### Checkpoint

- Extension loads without errors
- Copilot lists 6 precept tools
- `precept_validate`, `precept_schema`, and `precept_run` produce correct results on `samples/bugtracker.precept`

---

## Phase 8: Documentation

**Goal:** README and design docs reflect the MCP server as implemented.

### Steps

- [ ] Update `README.md`:
  - Add MCP Server section describing the 6 tools and how to use them
  - Add setup instructions (the `mcpServers` entry happens automatically via the extension)
  - Update Current Status to include MCP server
- [ ] Update `docs/McpServerDesign.md`:
  - Mark as "Implemented" with date
  - Note any deviations from the original design discovered during implementation
- [ ] Verify `docs/CatalogInfrastructureDesign.md` cross-references are still accurate
- [ ] Add MCP sync rule to `.github/copilot-instructions.md`:

**MCP Tool Sync** (new section in copilot-instructions):
- When core model types change (`DslWorkflowModel`, `DslField`, `DslState`, `DslEvent`, `DslTransitionRow`, etc.), check whether MCP tool DTOs in `tools/Precept.Mcp/Tools/` need corresponding updates.
- When `ConstructCatalog` or `ConstraintCatalog` records gain or lose properties, verify `LanguageTool.cs` serialization still matches `McpServerDesign.md § precept_language` output format.
- When the fire pipeline stages change, update the static `firePipeline` array in `LanguageTool.cs`.
- The MCP tools are **thin wrappers** — never duplicate domain logic. If a tool method exceeds ~30 lines of non-serialization code, the logic probably belongs in `src/Precept/`.

### Checkpoint

- README accurately describes all 6 tools
- No aspirational claims presented as implemented
- Design doc marked as implemented
- Copilot-instructions includes MCP Tool Sync section

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
| `tools/Precept.VsCode/package.json` | **Edit** — add `mcpServers` entry | 7 |
| `Precept.slnx` | **Edit** — add `Precept.Mcp` project | 0 |
| `README.md` | **Edit** — MCP server section | 8 |
| `docs/McpServerDesign.md` | **Edit** — mark implemented | 8 |
| `.github/copilot-instructions.md` | **Edit** — add MCP Tool Sync section | 8 |

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
Phase 7: VS Code registration
    ↓
Phase 8: Documentation
```

Phases 5 and 6 are interchangeable. Phase 5 (`precept_language`) has no dependency on any other tool and could be implemented at any point after Phase 0. It's placed here so the more commonly used tools (`validate`, `schema`, `audit`, `run`) are built first.

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
| 7. VS Code registration | ~10 | Low |
| 8. Documentation | ~150 | Low |
| **Total** | **~970** | |

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
> 1. Execute the phases in order (0 through 8), committing at each checkpoint.
> 2. Each phase must end with `dotnet build` and `dotnet test` passing before moving to the next.
> 3. Tools are **thin wrappers** — all domain logic lives in `src/Precept/`. The MCP project calls `PreceptParser`, `PreceptCompiler`, `PreceptEngine`, `ConstructCatalog`, and `ConstraintCatalog` directly.
> 4. Every tool returns structured JSON — no prose-only responses.
> 5. Every tool has integration tests in `test/Precept.Mcp.Tests/` that call the tool method directly (no MCP transport).
> 6. For `precept_language` (Phase 5), vocabulary comes from reflecting token enum attributes, constructs from `ConstructCatalog.Constructs`, and constraints from `ConstraintCatalog.Constraints`. The static sections (`expressionScopes`, `firePipeline`, `outcomeKinds`) are constant arrays in the tool class.
> 7. Follow the project's copilot-instructions (`.github/copilot-instructions.md`) for documentation sync — update README and design docs in Phase 8.
>
> Begin with Phase 0: create the project scaffolding and add it to the solution.
