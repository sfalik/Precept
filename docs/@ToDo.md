# Precept ToDo list

## Copilot Authoring of Precept
- Implement Phase 7: create `tools/Precept.Plugin/` structure (plugin.json, dev .mcp.json), move launcher to `tools/scripts/`, verify plugin loads via `chat.pluginLocations`.
- Implement Phase 8: draft Precept Author agent and authoring/debugging skills, validate against agentskills.io spec, test in Chat. Both skills include Mermaid diagramming instructions for full and partial state diagrams (no separate tool — skills use `precept_schema` + `precept_audit` data).
- Implement Phase 9: remove extension MCP registration, remove `Precept Dev` from `.vscode/mcp.json`, rewrite plugin `.mcp.json` for distribution, publish plugin.
 
## Language Design and Implementation
- All compile-time policy decisions for Phases D–G are locked: equality, collection mutation nullability, event-arg scope, event-assert scope, static impossibility boundary, diagnostic severity, and diagnostic code policy. See `PreceptLanguageDesign.md` Status section and `PreceptLanguageImplementationPlan.md` locked policy sections.
- Deliver the next checker hardening pass across compiler, analyzer, MCP validation, tests, and docs so scope rules, boolean-only rule positions, collection contracts, and multi-diagnostic coverage all stay aligned.

## Validation Design and Implementation
- Finish the validation-attribution design by walking the remaining scenarios and locking how targets and sources are reported for guard failures, `NotApplicable`, edit-time validation, and mixed field-plus-arg constraints.
- Implement structured validation issues end to end so runtime inspection becomes the source of truth for attribution and every consumer can replace string-matching heuristics with semantic targets.

## Later
- Add sample integration tests without hardcoded cases; evaluate driving them through the CLI or embedding test plans in sample comments.
- Fluent interface for runtime (e.g. engine.CreateInstance)

## Ideas
- Decide whether a standalone CLI is still needed if MCP already covers the same workflows.
- Support passing one precept as an event argument to another.