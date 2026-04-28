## Core Context

- Owns MCP server and plugin distribution surfaces, including DTO shape, tool contracts, and plugin/package correctness.
- Enforces the thin-wrapper rule: MCP should expose core behavior, not duplicate domain logic.
- Historical summary (pre-2026-04-13): drove MCP vocabulary/spec sync for new types and constraints, declaration-guard DTO design, and Squad automation/docs cleanup around the retired `squad:copilot` lane.

## Learnings

- MCP contract changes should be additive when possible and preserve existing consumer shapes.
- Catalog-driven vocabulary is the preferred path for exposing DSL keywords/types; non-token constructs need explicit catalog registration.
- Label/automation removals should be staged through sync workflows and disabled notices, not silent deletion.
- Proof-engine MCP integration is only agent-complete when `precept_compile` exposes a documented proof schema with structured assessments; a raw `Dump()` reference is not a sufficient contract.
- `precept_language` already has the right catalog-driven insertion point for proof diagnostics, but `id`/`phase`/`rule` alone is too thin for contradiction-vs-unresolved proof semantics.
- Repo-local MCP config now has three explicit surfaces: repo-root `.mcp.json` for Copilot CLI, `.vscode/mcp.json` for VS Code workspace development, and `tools/Precept.Plugin/.mcp.json` for the shipped payload.

## Recent Updates

### 2026-04-27 — Dual-surface MCP config landed and validated
- Implemented the approved minimal change: repo-root `.mcp.json` now gives Copilot CLI a repo-local `mcpServers.precept` entry pointing at `node tools/scripts/start-precept-mcp.js`.
- Preserved the existing boundaries: `.vscode/mcp.json` stays the VS Code/workspace-local `servers` config, and `tools/Precept.Plugin/.mcp.json` stays the shipped `dotnet tool run precept-mcp` payload.
- Directly related docs were updated in the same batch, and the stale `docs/ArtifactOperatingModelDesign.md` reference was retired in favor of `tools/Precept.Plugin/README.md`.
- Validation confirmed the `github` server should not be mirrored into the root CLI config because Copilot CLI already provides it natively.

### 2026-04-25 — AI Surface Completeness Review
- Wrote `.squad/decisions/inbox/newman-catalog-metadata-ai-review.md` assessing all 10 catalogs as AI grounding surface.
- Current `precept_language` covers ~40% of what AI needs. Critical gaps: no type system metadata (widening, accessors), no operation legality table, no modifier applicability matrix, no syntax reference.
- Recommended: single-response catalog-keyed serialization, tagged unions for DUs, new `syntaxReference` prose section. No per-catalog endpoints.
- Prioritized 11 changes across 3 tiers. Types, Operations, and Modifiers catalogs are the bottleneck — MCP serialization is trivial once `All` exists.

### 2026-04-12 — Squad `squad:copilot` retirement cleanup
- Tracked the workflow/template/doc blast radius and kept `squad:chore` distinct from the retired coding-agent label.

### 2026-04-11 — Declaration guard DTO contract
- Locked the additive `precept_compile` expansion for invariants, state asserts, event asserts, and edit blocks with nullable `when`.
