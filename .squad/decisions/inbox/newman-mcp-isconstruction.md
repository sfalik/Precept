# Newman — MCP `isConstruction` closeout

- DTO change: `CompileEventRowDto` carries `eventName` and `isConstruction`, and `CompileTool.MapEventRow(...)` projects the flag directly from `TypedEventRow.IsConstruction`.
- Test coverage: `CompileTool_EventRow_IsConstruction_True_ForInitialEvent` covers `event Create initial` + `on Create -> ...` returning `isConstruction: true`; `CompileTool_EventRow_IsConstruction_False_ForNonInitialEvent` covers a regular event row returning `isConstruction: false`.
- `McpServerDesign.md` update status: `docs\McpServerDesign.md` is absent on this branch; the active MCP contract doc is `docs\tooling\mcp.md`, and its `precept_compile` section now documents `eventHandlers` plus `isConstruction`.
