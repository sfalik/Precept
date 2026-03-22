# Precept ToDo list

## Copilot Authoring of Precept
- Implement [Phase 7](PreceptLanguageImplementationPlan.md) ([MCP plan L322](McpServerImplementationPlan.md#L322)): create `tools/Precept.Plugin/` structure (plugin.json, dev .mcp.json), move launcher to `tools/scripts/`, verify plugin loads via `chat.pluginLocations`.
- Implement [Phase 8](McpServerImplementationPlan.md#L371): draft Precept Author agent and authoring/debugging skills, validate against agentskills.io spec, test in Chat. Both skills include Mermaid diagramming instructions for full and partial state diagrams (no separate tool — skills use `precept_schema` + `precept_audit` data).
- Implement [Phase 9](McpServerImplementationPlan.md#L407): remove extension MCP registration, remove `Precept Dev` from `.vscode/mcp.json`, rewrite plugin `.mcp.json` for distribution, publish plugin.
 
## Language Design and Implementation
- Implement [Phase D](PreceptLanguageImplementationPlan.md#L92): Equality and null-compatible comparison policy — enforce same-family equality in `PreceptTypeChecker`, align runtime evaluator, add tests.
- Implement [Phase E](PreceptLanguageImplementationPlan.md#L122): Scope and narrowing hardening — remove bare arg names from transition row symbol tables, enforce dotted form, add event-arg narrowing tests.
- Implement [Phase F](PreceptLanguageImplementationPlan.md#L152): Rule-position strictness and collection contracts — register C46, reject non-boolean expressions in invariants/asserts/guards, harden `contains` coverage.
- Implement [Phase G](PreceptLanguageImplementationPlan.md#L178): Additional sound static reasoning — register C47, detect duplicate guards in `ValidateTransitionRows()`.
- Implement [Phase H](PreceptLanguageImplementationPlan.md#L205): Coverage and tooling sync — fix `MapTypeDiagnostic` severity mapping, verify completions, catalog drift for C46/C47, README sync.

## Constraint Violation Model
- Implement [Phase 0](ConstraintViolationImplementationPlan.md#Phase-0-Model-Type-Renames-PreceptModelcs): Model type renames — `PreceptAssertPreposition` → `AssertAnchor`, `PreceptStateAssert` → `StateAssertion`, `PreceptEventAssert` → `EventAssertion`, `PreceptRejection` → `Rejection`, `PreceptStateTransition` → `StateTransition`, `PreceptNoTransition` → `NoTransition`, `.Preposition` → `.Anchor`.
- Implement [Phase 1](ConstraintViolationImplementationPlan.md#Phase-1-Result-Type--Enum-Renames-PreceptRuntimecs): Result type & enum renames — `PreceptOutcomeKind` → `TransitionOutcome`, `PreceptUpdateOutcome` → `UpdateOutcome`, all result types drop `Precept` prefix, enum value renames, factory method renames.
- Implement [Phase 2](ConstraintViolationImplementationPlan.md#Phase-2-Catalog--Compile-Result-Renames): Catalog & compile result renames — `ConstraintCatalog` → `DiagnosticCatalog` (file + class), `PreceptCompileValidationResult` → `CompileResult`.
- Implement [Phase 3](ConstraintViolationImplementationPlan.md#Phase-3-Runtime-Method-Renames): Runtime method renames + `IsSuccess` — `CollectValidationViolations` → `CollectConstraintViolations`, `EvaluateEventAsserts` → `EvaluateEventAssertions`, `EvaluateStateAsserts` → `EvaluateStateAssertions`, add `IsSuccess` to result types.
- Implement [Phase 4](ConstraintViolationImplementationPlan.md#Phase-4-Structured-Constraint-Violations): Structured constraint violations — introduce `ConstraintViolation`, `ConstraintTarget`, `ConstraintSource`, `ExpressionSubjects`; replace `IReadOnlyList<string> Reasons` with `IReadOnlyList<ConstraintViolation> Violations`; split `Rejected` vs `ConstraintFailure`.
- Implement [Phase 5](ConstraintViolationImplementationPlan.md#Phase-5-Test-Migration): Test migration — update all test assertions for new names, enum values, and structured violations; add scenario tests from design doc.
- Implement [Phase 6](ConstraintViolationImplementationPlan.md#Phase-6-Language-Server-Visualizer--MCP-Consumer-Updates): Language server, visualizer & MCP consumer updates — `PreceptPreviewHandler`, `PreceptPreviewProtocol`, `inspector-preview.html`, `LanguageTool`, `RunTool`, `InspectTool`.
- Implement [Phase 7](ConstraintViolationImplementationPlan.md#Phase-7-Documentation--Cleanup): Documentation & cleanup — final sweep for stale references.

## Later
- Add sample integration tests without hardcoded cases; evaluate driving them through the CLI or embedding test plans in sample comments.
- Fluent interface for runtime (e.g. engine.CreateInstance)
- **Same-preposition contradiction detection** — two asserts with the same preposition on the same state whose per-field domains are provably empty (e.g. `in Open assert X > 5` + `in Open assert X < 3`). Requires per-field interval/set analysis on expression ASTs. See [PreceptLanguageDesign.md](PreceptLanguageDesign.md) § Compile-time checks, item #4.
- **Cross-preposition deadlock detection** — `in`/`to` vs `from` asserts on the same state whose conjoined per-field domains are empty, making the state provably unexitable. Requires interval analysis + reachability reasoning. See [PreceptLanguageDesign.md](PreceptLanguageDesign.md) § Compile-time checks, item #5.
- **CLI design and implementation** — design exists in [CliDesign.md](CliDesign.md); implementation deferred. Audit for stale `Dsl*` naming when implementing.

## Ideas
- Decide whether a standalone CLI is still needed if MCP already covers the same workflows.
- Support passing one precept as an event argument to another.