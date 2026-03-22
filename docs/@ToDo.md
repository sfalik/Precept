# Precept ToDo list

## Copilot Authoring of Precept
- Implement [Phase 7](PreceptLanguageImplementationPlan.md) ([MCP plan L322](McpServerImplementationPlan.md#L322)): create `tools/Precept.Plugin/` structure (plugin.json, dev .mcp.json), move launcher to `tools/scripts/`, verify plugin loads via `chat.pluginLocations`.
- Implement [Phase 8](McpServerImplementationPlan.md#L371): draft Precept Author agent and authoring/debugging skills, validate against agentskills.io spec, test in Chat. Both skills include Mermaid diagramming instructions for full and partial state diagrams (no separate tool — skills use `precept_schema` + `precept_audit` data).
- Implement [Phase 9](McpServerImplementationPlan.md#L407): remove extension MCP registration, remove `Precept Dev` from `.vscode/mcp.json`, rewrite plugin `.mcp.json` for distribution, publish plugin.
 
## Language Design and Implementation
- Implement [Phase D](PreceptLanguageImplementationPlan.md#L92): Equality and null-compatible comparison policy — enforce same-family equality in `PreceptTypeChecker`, align runtime evaluator, add tests.
- Implement [Phase E](PreceptLanguageImplementationPlan.md#L122): Scope and narrowing hardening — remove bare arg names from transition row symbol tables, enforce dotted form, add event-arg narrowing tests.
- Implement [Phase F](PreceptLanguageImplementationPlan.md#L152): Rule-position strictness and collection contracts — register C44, reject non-boolean expressions in invariants/asserts/guards, harden `contains` coverage.
- Implement [Phase G](PreceptLanguageImplementationPlan.md#L178): Identical-guard duplicate detection — register C45, detect duplicate guards in `ValidateTransitionRows()`.
- Implement [Phase H](PreceptLanguageImplementationPlan.md#L205): Coverage and tooling sync — fix `MapTypeDiagnostic` severity mapping, verify completions, catalog drift for C44/C45, README sync.

## Validation Design and Implementation
- Finish the validation-attribution design by walking the remaining scenarios and locking how targets and sources are reported for guard failures, `NotApplicable`, edit-time validation, and mixed field-plus-arg constraints.
- Implement structured validation issues end to end so runtime inspection becomes the source of truth for attribution and every consumer can replace string-matching heuristics with semantic targets.

## Later
- Add sample integration tests without hardcoded cases; evaluate driving them through the CLI or embedding test plans in sample comments.
- Fluent interface for runtime (e.g. engine.CreateInstance)

## Ideas
- Decide whether a standalone CLI is still needed if MCP already covers the same workflows.
- Support passing one precept as an event argument to another.