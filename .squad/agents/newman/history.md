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


### Issue #14 — Final DTO specification filed (2026-04-11)

- **Four new top-level arrays for `precept_compile`:** `invariants`, `stateAsserts`, `eventAsserts`, `editBlocks`. All use camelCase field names. `when: string | null` on each entry. `StateDto.rules: string[]` preserved unchanged (additive, not replacement).
- **Synthetic invariant filtering is mandatory:** `model.Invariants` must be filtered with `!i.IsSynthetic` before projecting to `InvariantDto`. Synthetic invariants desugar from field constraints — including them in the `invariants` array would duplicate the field's own `constraints` array.
- **`stateAsserts` anchor is lowercased string:** `AssertAnchor.In/To/From` → `"in"/"to"/"from"` via `.ToString().ToLowerInvariant()`. Matches the DSL keyword form.
- **`editBlocks[].state` is nullable:** Root-level stateless edit declarations have no state — `state: null` is a valid DTO shape, not an error.
- **`constraintTrace` belongs on per-event `InspectEventDto`**, not the top-level `InspectResult`. Per-event placement preserves evaluation context — mixing all events' traces at the top level is harder to correlate.
- **`guardStatus` field is omitted when there is no guard**: Only populated when `when != null`. When `guardStatus == "skipped"`, `status` is also omitted (constraint was never evaluated). When `guardStatus == "applied"`, `status` is always present. When no guard: `when: null`, no `guardStatus`, `status` always present.
- **Edit block editability in inspect — no change for now:** `EditableFieldDto` requires no changes. Form 4 (`in State when guard edit`) is deferred; `grantedWhen` shape is out of scope for this sprint.
- **C69 is a mechanism-selection diagnostic, not a typo diagnostic:** Fires when an event arg identifier IS known but is referenced in the wrong scope (invariant/state-assert guard). C38 fires for unknown identifiers. C69 message must name the dotted identifier, state the scope mismatch, and propose the correct transition-row guard alternative. These are mutually exclusive.
- **Two-commit sequence within one PR:** Commit A (structured arrays, `when: null` hardcoded) can land before George's model record changes. Commit B (wire `when` from `WhenText`) is gated on George's parser changes. Commit B stays in the same Issue #14 PR — not a separate PR.
- **`precept_language` — 6 construct entries need form/description updates** (invariant, in/to/from state assert × 3, on event assert, in state edit). No new vocabulary entries — `when` is already in `controlKeywords`.

### Issue #14 — `when` guards MCP contract assessment (2026-04-11)

- **Current compile output is nearly empty of declaration structure.** Invariants, event asserts, and edit blocks are entirely absent. State asserts appear only as `StateDto.rules: string[]` (reason text only — no expression, no anchor, no guard field). This is the most important finding: #14 cannot "add a guard field to existing declaration DTOs" because most declaration DTOs don't exist yet in the compile output.
- **Correct compile expansion:** Add new top-level arrays (`invariants`, `stateAsserts`, `eventAsserts`, `editBlocks`) each with a `when: string?` property. Do NOT replace `StateDto.rules: string[]` — that would break existing consumers. Keep the string array; add structured arrays alongside it.
- **Inspect trace requires runtime support.** The `constraintTrace` (#14's "skipped/applied/violated" requirement) doesn't exist at the core runtime layer — violations only, no full evaluation trace. George needs to surface guard evaluation results via `InspectionResult` before the MCP DTO can carry them. This is the highest-effort item.
- **`when` is already in `controlKeywords`.** It appears there because of transition row guards (`from S on E when G ->`). No new vocabulary additions needed for #14 — just construct catalog entry updates.
- **Cross-scope guard diagnostics must name the preferred form** (e.g., `from S on E when G -> reject`). A generic C038 "unknown identifier" is insufficient — the agent needs to understand it's a mechanism-selection problem, not a typo. Needs a dedicated C6X diagnostic code.
- **Verdict: Minor update, additive only.** Provided new declaration arrays are added (not replacing existing ones) and `constraintTrace` is treated as an additive nullable field.

### Issue #14 Slice 8 — MCP tool updates for when guards (2026-04-11)

- **8a: CompileTool.cs — 4 new DTO arrays added.** `InvariantDto`, `StateAssertDto`, `EventAssertDto`, `EditBlockDto` with `When` (nullable string) on all four. `CompileResult` expanded from 12 to 16 positional parameters. `DiagnosticsOnly` factory updated with 4 extra `null` args. Synthetic invariants filtered with `!inv.IsSynthetic`. `StateAssertDto.Anchor` lowercased via `.ToString().ToLowerInvariant()`.
- **8b: LanguageTool.cs — no changes needed.** `ConstructCatalog.Constructs` auto-picks up updated parser `.Register()` descriptions from Slice 2. Verified the dynamic projection in `LanguageTool.Run()` requires zero manual edits.
- **Validation: 67/67 MCP tests pass, 850/850 core tests pass.** Zero regressions — new DTO fields are additive and don't break existing deserialization.

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

### Issue #16 — MCP function catalog (2026-04-12)

- **`LanguageTool.cs`:** Added `Functions` section to `LanguageResult`. New DTOs: `FunctionDto(Name, Description, Signatures)`, `FunctionSignatureDto(Parameters, ReturnType, IsVariadic)`, `FunctionParamDto(Name, Type, Constraint?)`. Built dynamically from `FunctionRegistry.AllFunctions` — zero hardcoded function metadata in MCP layer.
- **`FunctionRegistry.cs`:** Added `Description` field to `FunctionDefinition` record. Added `AllFunctions` property. Registered all 18 functions (abs, ceil, clamp, endsWith, floor, left, max, mid, min, pow, right, round, sqrt, startsWith, toLower, toUpper, trim, truncate) with overloads, parameter types, return types, and constraints.
- **`CompileTool.cs`:** No changes needed. Expressions are string-based via `ExpressionText` / `ReconstituteExpr`. `PreceptFunctionCallExpression` case already exists in parser's `ReconstituteExpr` (produces `"round(Amount, 2)"` etc.). Nested function calls serialize correctly.
- **`PreceptParser.cs`:** Updated ConstructInfo ID from `round-function` to `function-call`. Updated description to list all 18 available functions. Kept `round` as lead keyword in Form for backward compatibility with `SampleFiles_CoverAllConstructs` drift test.
- **`StaticValueKind` display:** `FormatValueKind` helper collapses `Number|Integer|Decimal` → `"number"`, surfaces specific types otherwise. `FormatArgConstraint` maps `MustBeIntegerLiteral` → `"must be integer literal"`.
- **Test impact:** Fixed assertion in `ConstructsIncludeRoundFunction` (checks description contains "built-in function" instead of form starts with "round"). Updated `CatalogDriftTests` switch case for new construct ID. All 1187 tests pass.
- **Docs note:** `docs/McpServerDesign.md` needs a `functions` section added in the final doc sync slice. The existing `round()` reference table should expand to cover all 18 functions.

### Issue #9 — MCP updates for conditional expressions (2026-04-12)

- **`LanguageTool.cs`:** Zero changes needed. `if`, `then`, `else` tokens carry `[TokenCategory(TokenCategory.Control)]` attributes — `BuildVocabulary()` picks them up automatically. C78/C79 diagnostics in `DiagnosticCatalog` surface automatically via catalog reflection.
- **`CompileTool.cs`:** Zero changes needed. Conditional expressions serialize via `ReconstituteExpr` in the parser (`if <cond> then <then> else <else>`). `ExpressionText` on set assignments captures the full conditional text. No new DTO fields required.
- **`InspectTool.cs`:** No changes made — **trace enhancement (AC-9: `conditionResult` + `branchTaken`) requires core engine changes.** The evaluator's `EvaluationResult(bool Success, object? Value, string? Error)` returns only the final value, not which branch was taken. `EventInspectionResult` carries no per-expression evaluation trace. Surfacing conditional branch decisions requires: (1) the evaluator to produce trace metadata, (2) `EventInspectionResult` to carry it, (3) the MCP layer to project it. Items 1–2 are George's domain. MCP shape would be an optional `ConditionalTraceDto(string conditionText, bool conditionResult, string branchTaken, object? value)` array on `InspectEventDto`.
- **`FireTool.cs`:** Zero changes needed. `engine.Fire()` evaluates conditionals correctly — field values reflect branch selection.
- **Tests added (8 new, 83 total, 0 failed):**
  - `CompileToolTests`: conditional in set RHS compiles cleanly, C78 (non-boolean condition), C79 (branch type mismatch)
  - `LanguageToolTests`: `if`/`then`/`else` in control keywords vocabulary, C78/C79 in constraints catalog
  - `FireToolTests`: conditional set produces correct value (then branch), conditional set produces correct value (else branch)
  - `InspectToolTests`: conditional set assignment shows correct transition outcome
- **Docs note:** `docs/McpServerDesign.md` may need a note about conditional expression support in compile output. Deferred to doc sync.
