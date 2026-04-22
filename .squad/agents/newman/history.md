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
### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### Issue #31 Slice 6 — Operator Inventory (2026-04-10)

- Per-state field access modes (absent/readonly/editable) are a consensus pattern across enterprise platforms (Salesforce RecordType, Jira Workflow Screens, Dynamics 365 Business Rules). Declarative condition-based rules (JSON Schema if/then/else, Cedar when/unless, ServiceNow UI Policies) dominate over imperative approaches. Precept's `in <State> define <FieldList> <mode>` aligns with the strongest precedents.

- Field access mode MCP impact: `omit`/`view` keywords are catalog-driven (automatic vocabulary pickup via `[TokenCategory]`). Compile DTO needs resolved per-field `accessModes` map + per-state `fieldAccess` map for AI discoverability. Inspect/fire should strip omitted fields from `data` (structurally honest). `editableFields` should evolve to `fieldAccess` with mode enrichment. All changes are MCP-thin — domain logic belongs in core. Additive path preferred (`accessBlocks` alongside `editBlocks`) unless pre-1.0 clean-rename is accepted.

## Recent Updates

### 2026-04-15 — MCP impact analysis: field access mode syntax
- Full analysis of `omit`/`view`/`edit` impact across all 5 MCP tools and McpServerDesign.md.
- Key findings: vocabulary pickup is automatic (catalog-driven), compile needs per-field `accessModes` + per-state `fieldAccess`, inspect/fire should strip omitted fields from `data`, `editableFields` → `fieldAccess` rename recommended (pre-1.0 clean break).
- Output: `.squad/decisions/inbox/newman-mcp-impact-field-access.md`.

### 2026-07-14 — External research: per-state field access mode platform precedents
- Surveyed 11 systems (JSON Schema, OpenAPI, Salesforce, ServiceNow, Dynamics 365, Jira, Monday.com/Airtable, Zod, GraphQL, Cedar, React Hook Form/Formik) for how they handle conditional field presence, visibility, and editability.
- Output: `research/language/expressiveness/field-access-mode-platform-precedents.md`.

### 2026-04-12 — Squad `squad:copilot` retirement cleanup
- Tracked the workflow/template/doc blast radius and kept `squad:chore` distinct from the retired coding-agent label.

### 2026-04-11 — Declaration guard DTO contract
- Locked the additive `precept_compile` expansion for invariants, state asserts, event asserts, and edit blocks with nullable `when`.
