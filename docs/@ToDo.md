# Precept ToDo list

## Copilot Authoring of Precept
- Implement Phase 7: create `tools/Precept.Plugin/` structure (plugin.json, dev .mcp.json), move launcher to `tools/scripts/`, verify plugin loads via `chat.pluginLocations`.
- Implement Phase 8: draft Precept Author agent and authoring/debugging skills, validate against agentskills.io spec, test in Chat. Both skills include Mermaid diagramming instructions for full and partial state diagrams (no separate tool — skills use `precept_schema` + `precept_audit` data).
- Implement Phase 9: remove extension MCP registration, remove `Precept Dev` from `.vscode/mcp.json`, rewrite plugin `.mcp.json` for distribution, publish plugin.
 
## Language Design and Implementation
- Implement Phase D: Equality and null-compatible comparison policy — enforce same-family equality in `PreceptTypeChecker`, align runtime evaluator, add tests. See implementation prompt in `PreceptLanguageImplementationPlan.md`.
- Implement Phase E: Scope and narrowing hardening — remove bare arg names from transition row symbol tables, enforce dotted form, add event-arg narrowing tests. See implementation prompt.
- Implement Phase F: Rule-position strictness and collection contracts — register C44, reject non-boolean expressions in invariants/asserts/guards, harden `contains` coverage. See implementation prompt.
- Implement Phase G: Identical-guard duplicate detection — register C45, detect duplicate guards in `ValidateTransitionRows()`. See implementation prompt.
- Implement Phase H: Coverage and tooling sync — fix `MapTypeDiagnostic` severity mapping, verify completions, catalog drift for C44/C45, README sync. See implementation prompt.

## Validation Design and Implementation
- Finish the validation-attribution design by walking the remaining scenarios and locking how targets and sources are reported for guard failures, `NotApplicable`, edit-time validation, and mixed field-plus-arg constraints.
- Implement structured validation issues end to end so runtime inspection becomes the source of truth for attribution and every consumer can replace string-matching heuristics with semantic targets.

## Later
- Add sample integration tests without hardcoded cases; evaluate driving them through the CLI or embedding test plans in sample comments.
- Fluent interface for runtime (e.g. engine.CreateInstance)

## Ideas
- Decide whether a standalone CLI is still needed if MCP already covers the same workflows.
- Support passing one precept as an event argument to another.