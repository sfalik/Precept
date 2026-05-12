# Newman MCP phase 1-3 complete

## Phases completed

- Phase 1: replaced the 8 catalog/reference MCP tools with compact markdown formatters in `tools/Precept.Mcp/CatalogFormatters.cs`, added `scope` to `precept_types` / `precept_domains`, made `precept_operations` filter-first, and removed `LanguageTool.cs` plus the old catalog DTO files.
- Phase 2: reduced `precept_compile` to minimal JSON (`success`, `diagnosticCount`, `diagnostics`, `summary`) and deleted the old projected definition-graph tests.
- Phase 3: added `docs/McpServerDesign.md`, cleaned dead files/usings, synced squad notes, and ran final validation.

## Commits

- `c8fa70af` — `feat(mcp): Phase 1 — catalog markdown tools`
- `e80e4131` — `feat(mcp): Phase 2 — minimal compile payload`
- Phase 3 is the current HEAD commit: `feat(mcp): Phase 3 — cleanup and docs`

## Test count added

- 25 new/rewritten MCP behavioral test methods across `NewToolTests.cs`, `RecoveryHintTests.cs`, and `CompileToolTests.cs`.
- Current MCP suite result after the redesign: `39/39` passing.

## Inline design decisions

- Formatter layer lives in MCP and reads catalogs directly; no hidden aggregate `precept_language` path was preserved.
- `precept_types` scopes: `types`, `modifiers`, `modifiers:value|state|event|access|anchor`, `functions`.
- `precept_domains` scopes: `currencies`, `units`, `prefixes`, `dimensions`, `temporal`.
- `precept_compile` summary is compact prose only; no structured definition graph remains.

## Needs Frank's attention

- Shane's task text expected `samples/inventory-item.precept` to compile successfully post E-series, but current repo reality does not match that expectation. On this branch/workspace the sample still returns diagnostics, so the MCP contract can only report a reasonable summary; success would require separate runtime/sample-state work outside `tools/Precept.Mcp/`.
- Full-repo validation in this shared workspace ended with 12 failing `Precept.LanguageServer.Tests` semantic-token tests unrelated to the MCP redesign. `dotnet build` succeeded and `test/Precept.Mcp.Tests` is green; the LS failures appear to be pre-existing workspace state / sample drift and should be triaged separately.
