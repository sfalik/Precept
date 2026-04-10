# Precept ToDo list

## Implementation Order

The workstreams below have cross-plan dependencies. Execute in this order:

```
CV 0-3 → Language D-H → CV 4-7 → Language I → MCP Redesign → Update Tool → MCP 7-9
```

**Rationale:** Rename phases (CV 0-3) establish final naming so all subsequent code uses the settled names. Language checker expansion (D-H) then registers new diagnostics on `DiagnosticCatalog` (renamed in CV Phase 2). The structured violation model (CV 4-7) introduces `ConstraintViolation` and the `Rejected` / `ConstraintFailure` split. Language Phase I adds graph analysis warnings (C48–C53) and cleans up compile validation so the MCP redesign can consume one structured diagnostic pipeline. The MCP Redesign Phase consolidates 6 tools into 4 with text input and structured output. The Update Tool Phase adds `precept_update` (the 5th tool), renames `precept_run` to `precept_fire`, fixes inspect thin-wrapper violations, and adds `editableFields` to inspect output. MCP plugin, agent, and skills (MCP 7-9) go last because they reference the full 5-tool surface.

---

## Group 1: Naming Renames (CV Phases 0-3) ✅

- [x] [Phase 0](ConstraintViolationImplementationPlan.md#Phase-0-Model-Type-Renames-PreceptModelcs): Model type renames — `PreceptAssertPreposition` → `AssertAnchor`, `PreceptStateAssert` → `StateAssertion`, `PreceptEventAssert` → `EventAssertion`, `PreceptRejection` → `Rejection`, `PreceptStateTransition` → `StateTransition`, `PreceptNoTransition` → `NoTransition`, `.Preposition` → `.Anchor`.
- [x] [Phase 1](ConstraintViolationImplementationPlan.md#Phase-1-Result-Type--Enum-Renames-PreceptRuntimecs): Result type & enum renames — `PreceptOutcomeKind` → `TransitionOutcome`, `PreceptUpdateOutcome` → `UpdateOutcome`, all result types drop `Precept` prefix, enum value renames, factory method renames.
- [x] [Phase 2](ConstraintViolationImplementationPlan.md#Phase-2-Catalog--Validation-Result-Renames): Catalog & validation result renames — `ConstraintCatalog` → `DiagnosticCatalog` (file + class), `PreceptCompileValidationResult` → `CompileResult` → `ValidationResult`.
- [x] [Phase 3](ConstraintViolationImplementationPlan.md#Phase-3-Runtime-Method-Renames): Runtime method renames + `IsSuccess` — `CollectValidationViolations` → `CollectConstraintViolations`, `EvaluateEventAsserts` → `EvaluateEventAssertions`, `EvaluateStateAsserts` → `EvaluateStateAssertions`, add `IsSuccess` to result types.

## Group 2: Language Checker Expansion (Language Phases D-H) ✅

- [x] [Phase D](PreceptLanguageImplementationPlan.md#L92): Equality and null-compatible comparison policy — enforce same-family equality in `PreceptTypeChecker`, align runtime evaluator, add tests.
- [x] [Phase E](PreceptLanguageImplementationPlan.md#L122): Scope and narrowing hardening — remove bare arg names from transition row symbol tables, enforce dotted form, add event-arg narrowing tests.
- [x] [Phase F](PreceptLanguageImplementationPlan.md#L152): Rule-position strictness and collection contracts — register C46, reject non-boolean expressions in invariants/asserts/guards, harden `contains` coverage.
- [x] [Phase G](PreceptLanguageImplementationPlan.md#L178): Additional sound static reasoning — register C47, detect duplicate guards in `ValidateTransitionRows()`.
- [x] [Phase H](PreceptLanguageImplementationPlan.md#L205): Coverage and tooling sync — fix `MapValidationDiagnostic` severity mapping, verify completions, catalog drift for C46/C47, README sync.

## Group 3: Structured Constraint Violations (CV Phases 4-7) ✅

- [x] [Phase 4](ConstraintViolationImplementationPlan.md#Phase-4-Structured-Constraint-Violations): Structured constraint violations — introduce `ConstraintViolation`, `ConstraintTarget`, `ConstraintSource`, `ExpressionSubjects`; replace `IReadOnlyList<string> Reasons` with `IReadOnlyList<ConstraintViolation> Violations`; split `Rejected` vs `ConstraintFailure`.
- [x] [Phase 5](ConstraintViolationImplementationPlan.md#Phase-5-Test-Migration): Test migration — update all test assertions for new names, enum values, and structured violations; add scenario tests from design doc.
- [x] [Phase 6](ConstraintViolationImplementationPlan.md#Phase-6-Language-Server-Visualizer--MCP-Consumer-Updates): Language server, visualizer & MCP consumer updates — `PreceptPreviewHandler`, `PreceptPreviewProtocol`, `inspector-preview.html`, `LanguageTool`, `RunTool`, `InspectTool`.
- [x] [Phase 7](ConstraintViolationImplementationPlan.md#Phase-7-Documentation--Cleanup): Documentation & cleanup — final sweep for stale references.

## Group 4a: Graph Analysis Warning Diagnostics (Language Phase I) ✅

- [x] [Phase I](PreceptLanguageImplementationPlan.md#Phase-I-Graph-Analysis-Warning-Diagnostics): remove coverage warning C51, renumber graph diagnostics to C48–C53, eliminate throw-based compile validation, implement `PreceptAnalysis.Analyze()` (BFS reachability, orphaned events, dead-end detection, reject-only pairs, event-never-succeeds, empty precepts), add `ConstraintSeverity.Hint`, wire graph analysis into `Validate()`, add `CompileFromText(text)` composed pipeline, and confirm language server/MCP consumers use the structured result.
- Verification: full solution build plus `Precept.Tests`, `Precept.LanguageServer.Tests`, and `Precept.Mcp.Tests` pass; remaining output is limited to pre-existing nullable warnings.

## Group 4b: MCP Redesign (6→4 Tools) ✅

- [x] Implement the [MCP Redesign Phase](McpServerImplementationPlan.md#MCP-Redesign-Phase-64-Tools-with-Text-Input): replace validate+schema+audit with `precept_compile`, rewrite `precept_inspect` to delegate to `engine.Inspect()`, update `precept_run` for text input and structured `ViolationDto`, create shared DTOs (`DiagnosticDto`, `ViolationDto`), delete old tool files, rewrite tests.

## Group 4b-update: Update Tool + Fire Rename + Inspect Fixes ✅

- [x] Implement the [Update Tool Phase](McpServerImplementationPlan.md#Update-Tool-Phase-precept_update--precept_fire-Rename--Inspect-Thin-Wrapper-Fixes): rename `precept_run` → `precept_fire` (matching `engine.Fire()`), fix inspect thin-wrapper violations (remove pre-check block, remove synthetic `"requires-args"` outcome, remove event sorting, simplify `requiredArgs` to `IReadOnlyList<string>`), add `precept_update` tool wrapping `engine.Update()`, add `editableFields` to `precept_inspect` output, write tests.
- Verification: full solution build plus `Precept.Tests` (544), `Precept.LanguageServer.Tests` (74), and `Precept.Mcp.Tests` (48) pass; 666 total tests.

## Group 4c: Copilot Plugin, Agent, and Skills (MCP Phases 7-9) ✅

- ✅ [Phase 7](McpServerImplementationPlan.md#Phase-7-Agent-Plugin-Structure--MCP-Packaging): created `tools/Precept.Plugin/` with `.claude-plugin/plugin.json` (Claude format), plugin `.mcp.json` uses dist format (`dotnet tool run precept-mcp`) for both VS Code and CLI consumers, `.vscode/mcp.json` provides dev override with launcher script, moved launcher to `tools/scripts/`. The earlier `chat.pluginLocations` local-dev path has since been superseded by workspace-native `.github/agents/` and `.github/skills/` for worktree-neutral development.
- ✅ [Phase 8](McpServerImplementationPlan.md#Phase-8-Agent-and-Skill-Content): drafted Precept Author agent (`precept-author.agent.md` — persona + tool restrictions), authoring skill (7-step creation workflow + Mermaid diagrams), debugging skill (compile → inspect → fire diagnosis + common patterns). Validated against agentskills.io spec (names match dirs, descriptions under 1024 chars, bodies under 500 lines).
- ✅ [Phase 9](McpServerImplementationPlan.md#Phase-9-Documentation--Distribution): removed `registerMcpServerDefinitionProvider()` and `mcpServerDefinitionProviders` from extension, removed `publish:mcp` script, updated McpServerDesign.md to reflect `.claude-plugin/` format, verified copilot-instructions MCP sync section is accurate. Distribution (publish to marketplace) and remaining Phase 7/8 testing items are pending.

## Language Expansion Milestones (from research refresh 2026-04-10)

### Milestone 1: "Governed Integrity"

- [ ] #31 — Logical keywords (`and`/`or`/`not`). Ship first — touches every sample.
- [ ] #22 — Data-only precepts. PR #48 in progress (Slices 1-3 committed). Design locked (12 decisions).
- [ ] #13 — Field-level constraints (`min`, `max`, `nonnegative`, `positive`). Constraint-zone architecture.

### Milestone 2: "Full Entity Surface"

- [ ] #8 — Named rule declarations (`rule Name when <expr>`)
- [ ] #14 — Conditional invariants (`invariant ... when ...`)
- [ ] #29 — `integer` type
- [ ] #25 — `choice(...)` type
- [ ] #11 — Event argument absorb shorthand (pending research pass)

### Milestone 3: "Expression Power"

- [ ] #16 — Built-in function library (`abs`, `round`, `floor`, `ceil`, `min`, `max`)
- [ ] #9 — Conditional expressions (`if...then...else` in value positions)
- [ ] #10 — String `.length` accessor
- [ ] #15 — String `.contains()` method
- [ ] #26 — `date` type
- [ ] #27 — `decimal` type (requires #16)
- [ ] #17 — Computed / derived fields (requires #9, benefits from #16)

### Research still needed

- [ ] Absorb (#11) — precedent survey, philosophy tension resolution
- [ ] Null safety — standalone research doc for nullable semantics across new types
- [ ] Built-in function architecture — function-call AST node integration with Superpower
- [ ] Stateless constraint patterns — what patterns arise without states?

---

## Later
- Add sample integration tests without hardcoded cases; evaluate driving them through the CLI or embedding test plans in sample comments.
- Fluent interface for runtime (e.g. engine.CreateInstance)
- **Same-preposition contradiction detection** — two asserts with the same preposition on the same state whose per-field domains are provably empty (e.g. `in Open assert X > 5` + `in Open assert X < 3`). Requires per-field interval/set analysis on expression ASTs. See [PreceptLanguageDesign.md](PreceptLanguageDesign.md) § Compile-time checks, item #4.
- **Cross-preposition deadlock detection** — `in`/`to` vs `from` asserts on the same state whose conjoined per-field domains are empty, making the state provably unexitable. Requires interval analysis + reachability reasoning. See [PreceptLanguageDesign.md](PreceptLanguageDesign.md) § Compile-time checks, item #5.
- **CLI design and implementation** — design exists in [CliDesign.md](CliDesign.md); implementation deferred. Audit for stale `Dsl*` naming when implementing.
- **Structured violations in preview protocol** — carry full `ConstraintViolation` data (source kind, targets, expression text) through `PreceptPreviewProtocol` to the webview instead of flattened strings. Would enable richer inspector UI (field highlighting, constraint source differentiation). Currently `PreceptPreviewEventStatus.Reasons` is `IReadOnlyList<string>`.

## Ideas
- Decide whether a standalone CLI is still needed if MCP already covers the same workflows.
- Support passing one precept as an event argument to another.