## Core Context

- Owns MCP server and plugin distribution surfaces, including DTO shape, tool contracts, and plugin/package correctness.
- Enforces the thin-wrapper rule: MCP should expose core behavior, not duplicate domain logic.
- Repo-local MCP development still has three distinct surfaces: repo-root `.mcp.json`, `.vscode\mcp.json`, and `tools\Precept.Plugin\.mcp.json`.

## Learnings

- MCP contract changes should be additive when possible and preserve existing consumer shapes.
- When the core DU already carries the needed semantic flag on the base type, the DTO projection should read it directly instead of re-dispatching by subtype.
- `docs\tooling\mcp.md` remains the active in-repo MCP contract on this branch; absent legacy docs should be noted, not revived.

## Historical Summary

- Early May locked the focused-tool MCP surface, recovery-hint sync, DTO audit posture, and the thin-wrapper rule for catalog projections.
- Older rollout detail now lives in `.squad\decisions.md`; this file keeps the durable MCP posture plus the latest closeout.

## Recent Updates

### 2026-05-17T12:46:26Z — `isConstruction` DTO closeout recorded

- Commit `4e31f435` added `isConstruction` to the compile event-row DTO and projected it directly from `TypedEventRow.IsConstruction`.
- Validation held at 46/46 MCP tests green.
- The MCP compile contract now tracks the same declaration-level construction semantics as the runtime, grammar, language server, docs, and samples.
