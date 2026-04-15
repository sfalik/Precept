## Core Context

- Owns MCP server and plugin distribution surfaces, including DTO shape, tool contracts, and plugin/package correctness.
- Enforces the thin-wrapper rule: MCP should expose core behavior, not duplicate domain logic.
- Historical summary (pre-2026-04-13): drove MCP vocabulary/spec sync for new types and constraints, declaration-guard DTO design, and Squad automation/docs cleanup around the retired `squad:copilot` lane.

## Learnings

- MCP contract changes should be additive when possible and preserve existing consumer shapes.
- Catalog-driven vocabulary is the preferred path for exposing DSL keywords/types; non-token constructs need explicit catalog registration.
- Label/automation removals should be staged through sync workflows and disabled notices, not silent deletion.

## Recent Updates

### 2026-04-12 — Squad `squad:copilot` retirement cleanup
- Tracked the workflow/template/doc blast radius and kept `squad:chore` distinct from the retired coding-agent label.

### 2026-04-11 — Declaration guard DTO contract
- Locked the additive `precept_compile` expansion for invariants, state asserts, event asserts, and edit blocks with nullable `when`.
