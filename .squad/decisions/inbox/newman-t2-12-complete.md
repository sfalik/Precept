# Newman t2-12 complete

## Commit
- `5f79fc7a` — `feat(t2-12): MCP DTO audit — sync DTOs to catalog growth`

## What changed
- Synced `CompileToolDtos.cs` to the audited compile contract: state hooks, event ensures, rule guards, row outcomes/reject messages, state omit/access details, event arg optionality, and choice metadata are now represented.
- Rewired `CompileTool.cs` to populate every added DTO field from the real semantic/construct surfaces already present in core (`SemanticIndex`, `ConstructManifest`, and catalog metadata).
- Fixed compile rendering gaps: `~string`, structural collection type names, valued modifiers, stripped `because` keyword/message quotes, and string default values.
- Added focused MCP definition regression tests covering each DTO sync item.
- Updated `docs/tooling/mcp.md` (the current MCP design doc surface in-repo) to match the shipped `precept_compile` contract.

## Validation
- `dotnet test test/Precept.Mcp.Tests/` → 74 passed
- `dotnet test test/Precept.Tests/` → 3925 passed

## Notes
- `docs/McpServerDesign.md` is not present in this repo; `docs/tooling/mcp.md` is the active design-contract document that was updated in the same pass.
