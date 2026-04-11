# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. AI-native — "the contract AI can reason about."
- **Stack:** C# / .NET 10.0 (MCP server), Markdown/JSON (plugin/skills)
- **My domain:** `tools/Precept.Mcp/` (MCP server, 5 tools) and `tools/Precept.Plugin/` (Copilot agent plugin)
- **MCP tools:** `precept_language`, `precept_compile`, `precept_inspect`, `precept_fire`, `precept_update`
- **Key docs:** `docs/McpServerDesign.md` (MCP tool specs), `docs/McpServerImplementationPlan.md`
- **Thin wrapper rule:** MCP tools wrap core APIs — no domain logic in the MCP layer
- **Tests:** `test/Precept.Mcp.Tests/`
- **Distribution:** Claude Marketplace (plugin)
- **Created:** 2026-04-04

## Learnings

### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### Issue #27/#25/#29 Slice 6 — MCP Vocabulary + Spec (2026-04-11)

- `LanguageTool.cs` needed zero code changes for `integer`/`decimal`/`choice`/`maxplaces`/`ordered`. All five tokens already existed in `PreceptToken` with correct `TokenCategory` attributes — the catalog-driven vocabulary mechanism picked them up automatically.
- `round()` is not a token (recognized by identifier name in the parser). Registered it in `ConstructCatalog` via `.Register(new ConstructInfo(...))` on the `RoundAtom` parser combinator so it surfaces in `precept_language.constructs`. This is the canonical pattern for non-keyword DSL constructs.
- `CompileTool.cs` `FormatConstraint` had no `Maxplaces` case — it fell through to the catch-all `.GetType().Name.ToLowerInvariant()` producing `"maxplaces"` without the places value. Fixed to `$"maxplaces {m.Places}"` to match the constraint authoring syntax.
- `FieldDto` and `EventArgDto` gained `ChoiceValues` (`IReadOnlyList<string>?`) and `IsOrdered` (`bool?`). Used nullable bool for `IsOrdered` so `false` is omitted from JSON output (only populated when true), keeping choice-less field output clean.
- `McpServerDesign.md` was missing a `ConstraintKeywords` row entirely in the vocabulary table. Added it. Also added scalar type reference table, constraint reference table, and built-in function reference table for `round()`.
- 9 new MCP tests added (65 total, 0 failed).


### MCP Tool Surface (5 Tools — Final Design)

**Tool philosophy:** One tool per concern. No overlap in reporting. Failure modes reinforce the intended signal chain: `compile → fix → inspect/fire/update`.

1. **`precept_language`** (no parameters)
   - Returns full DSL reference: vocabulary (8 keyword categories), constructs, constraints, expression scopes, fire pipeline (6 stages), outcome kinds
   - Serializes `ConstructCatalog.Constructs` + `DiagnosticCatalog.Diagnostics` + reflected `PreceptToken` metadata
   - **No MCP-specific logic** — pure catalog projection
   - Always succeeds (no errors)
   - Used for: AI self-training, syntax hints, semantic scoping rules

2. **`precept_compile(text: string)`** — Single source of structured diagnostics
   - Input: precept text only
   - Output: Full typed structure (states, fields, events, transitions) + `DiagnosticDto[]` (line, column, message, code, severity)
   - Returns partial structure + diagnostics even on type errors (parse failure returns diagnostics only)
   - Outcomes: `valid: true` or `valid: false`
   - Used for: Correctness gating, structural review, graph analysis warnings (unreachable/dead-end states)

3. **`precept_inspect(text, currentState, data?, eventArgs?)`** — Read-only possibility map
   - Input: precept text + state snapshot + optional event args for specific events
   - Output: Resolved instance + events array (outcome, resultState, violations per event) + editable fields
   - Re-inspects individual events with supplied args; others without args
   - **Compile failure returns short error** — only `precept_compile` surfaces diagnostics
   - Used for: Exploring "what can happen from here", field editability, event pre-validation

4. **`precept_fire(text, currentState, event, data?, args?)`** — Single-event execution
   - Input: precept text + state snapshot + event name + optional args
   - Output: Full execution result (fromState, toState, updated data, violations, outcome)
   - Projects `FireResult.Violations` as full `ViolationDto[]`
   - **Compile failure returns short error** — only `precept_compile` surfaces diagnostics
   - Used for: Tracing individual transitions, verifying mutations, step-by-step scenario replay

5. **`precept_update(text, currentState, data?, fields)`** — Direct field editing
   - Input: precept text + state snapshot + field updates
   - Output: Outcome (Updated/UneditableField/ConstraintFailure) + updated data + violations
   - Calls `engine.Update(instance, patch => patch.Set(k, v) for each field)`
   - **Compile failure returns short error** — only `precept_compile` surfaces diagnostics
   - Used for: Testing `in <State> edit` declarations, constraint violation detection

### Data Transfer Objects (DTOs)

**Compile-time: `DiagnosticDto`** (inline in `CompileTool.cs`)
```
{ line: number, column?: number, message: string, code?: string, severity: "error"|"warning"|"hint" }
```

**Runtime: `ViolationDto`** (shared `Tools/ViolationDto.cs` — used by inspect, fire, update)
```
{
  message: string,
  source: { kind, stateName?, anchor?, eventName?, expressionText?, reason?, sourceLine? },
  targets: { kind, fieldName?, eventName?, argName?, stateName?, anchor? }[]
}
```
- Source kinds: `invariant`, `state-assertion`, `event-assertion`, `transition-rejection`
- Target kinds: `field`, `event-arg`, `event`, `state`, `definition`
- **Preserves full violation model from core** — no information loss

### Implementation Alignment

✅ **Exact match with design spec:**
- All 5 tools implemented as designed
- Text input + structured JSON output throughout
- Compile as sole diagnostic source
- No domain logic in MCP layer (pure wrappers)
- Shared `ViolationDtoMapper` for consistent violation projection

✅ **Thin wrapper pattern enforced:**
- `CompileTool`: calls `PreceptCompiler.CompileFromText()`, projects model + diagnostics
- `InspectTool`: calls `engine.Inspect()`, re-inspects specific events if args supplied
- `FireTool`: calls `engine.Fire()`, projects `FireResult`
- `UpdateTool`: calls `engine.Update()`, projects `UpdateResult`
- `JsonConvert.ToNativeDict()` bridges MCP JSON to native types (string, double, bool, null)

### Copilot Plugin (Agent + Skills)

**Plugin format:** `.claude-plugin/plugin.json` (Claude format, not generic)

**Agent: Precept Author**
- Persona: DSL specialist
- Tool restrictions: `read`, `edit`, `search`, `fetch`, `precept/*` MCP tools (no terminal)
- Role: routing to skills + enforcing guardrails (gated tool usage, artifact vs. source distinction)
- Thin by design — owns persona + boundaries, not procedures

**Skills:**
1. **`precept-authoring`** — creation/editing workflow
   - Steps: vocabulary → gather conventions → design model → write precept → compile → verify behavior → state diagram
   - Diagram is optional (not mandatory)
   - Repo-agnostic (cannot assume `samples/` exists)
   - Workflow gates: only call `inspect`/`fire` after `compile` succeeds

2. **`precept-debugging`** — diagnosis/tracing workflow
   - Steps: compile first → understand structure → inspect → fire → test edits → diagram
   - Common patterns documented: guard ordering, unreachable states, dead-end states, constraint violations
   - Diagrams optional, focused on problem areas only

Both skills use `precept_language` for syntax authority, handle Mermaid diagrams as optional aids (not mandatory output), and follow the MCP tool signal chain.

**MCP launcher:**
- Plugin `.mcp.json`: `dotnet tool run precept-mcp` (distribution format)
- Dev override `.vscode/mcp.json`: launcher script + shadow-copy build (file-locking safety)
- Launcher rebuilds lazily on next tool invocation after reload

### Test Coverage

6 test classes (CompileTool, InspectTool, FireTool, UpdateTool, LanguageTool, implicit integration):
- CompileTool: valid definitions, field types, event args, parse errors, type errors
- InspectTool: state snapshots, event outcomes, editableFields, constraint previews
- FireTool: transitions, mutations, violations, state changes
- UpdateTool: field edits, constraint violations, uneditable fields
- LanguageTool: vocabulary serialization, construct catalog, constraints catalog
- Tests use sample files from `samples/` directory (canonical DSL examples)

### Potential Contract Ambiguities (None Found)

**Clean design — no ambiguities detected:**
- Error handling tiers are explicit (only compile surfaces structured diagnostics)
- Tool inputs/outputs are well-typed
- ViolationDto fully models constraint structure
- Expression scoping rules explicitly documented in `precept_language` output
- Fire pipeline stages enumerated and described

### Architecture Alignment

- **MCP sits correctly:** thin wrapper layer, all domain logic in `src/Precept/`
- **Compile is gatekeeper:** inspect/fire/update treat it as pass/fail, preventing diagnostic duplication
- **JSON shapes match design spec exactly:** no field additions, no name drift
- **Catalog-driven:** `LanguageTool` uses `ConstructCatalog` + `DiagnosticCatalog` + `PreceptToken` metadata — all single source of truth
- **DTOs stay in sync:** `ViolationDtoMapper` handles all 4 source types × 5 target types automatically via switch statements
