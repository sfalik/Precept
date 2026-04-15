## Core Context

- Owns architecture, language design, and final review gates across runtime, tooling, and documentation.
- Co-owns language research with George and keeps `docs/PreceptLanguageDesign.md` aligned with actual implementation.
- Standing architecture rules: keep MCP as thin wrappers, keep docs factual, surface open decisions instead of inventing behavior, and preserve philosophy boundaries.
- Historical summary (pre-2026-04-13): drove proposal/design work for conditional guards, event hooks, verdict modifiers, modifier ordering, computed fields, and related issue-review passes; also reviewed the Squad `@copilot` lane retirement and other cross-surface contract updates.

## Learnings

- Computed fields: the critical semantic contract is one recomputation pass after all mutations and before constraint evaluation.
- Modifier ordering: parser rigidity lived mostly in parser/completion surface; runtime/model layers were already largely order-independent.
- Guarded declarations: scope-inherited guards are the correct model; implementation gaps should be treated separately from design soundness.
- Docs work: `PreceptLanguageDesign.md`, editability/runtime docs, and MCP docs must stay synchronized whenever wording around constraints, updates, or inspection/editability changes.

## Recent Updates

### 2026-04-12 — Research: Architectural Patterns for Runtime Inspectability in Business Tools
- Produced comprehensive external research document at `research/design-system/business-app-inspectability-architecture.md`.
- **Surveyed 5 system categories:** state machine runtimes (XState/Stately, Temporal, AWS Step Functions, Camunda), business rules engines (Drools, IBM ODM, FICO Blaze Advisor, InRule), workflow task forms (Pega, Appian, OutSystems, Mendix), low-code inspector panels (Retool, Budibase, ToolJet), and runtime inspection APIs.
- **Key architectural finding:** Two fundamental approaches to inspectability — **snapshot-centric** (XState, Precept, ODM) where the runtime returns a single structured object, vs. **event-stream-centric** (Temporal, Camunda, Step Functions) where the runtime emits chronological events. Precept's `InspectResult` is snapshot-centric, which is architecturally superior for form/task UIs.
- **Precept's unique advantage confirmed:** No surveyed system provides guard-level explanation AND constraint status on editable fields in a single real-time API response. XState's `state.can()` returns only boolean; Camunda's incident diagnosis only explains failures (not availability); Drools has no structured explanation API at all.
- **5 patterns identified:** (1) Snapshot vs. Stream consumption models, (2) Explanation depth spectrum (boolean → structural → incident → trace), (3) Universal 3-4 level progressive disclosure, (4) Graph overlay (execution state on definition diagram), (5) Monitoring-vs-task UI separation.
- **Top recommendations for Precept:** Implement 3-level progressive disclosure (summary → detail → trace) in inspector panel; add graph overlay to preview diagram; adopt Camunda-style incident diagnosis richness for constraint violations; consider XState-like state metadata for UI hints; distribute `InspectResult` data across the form (fields inline, events as buttons) rather than a monolithic panel.
- **Closest external analog to InspectResult:** IBM ODM's execution trace — a structured object containing per-rule evaluation detail alongside the decision result. But ODM's is post-execution, not real-time. Precept's real-time point-in-time inspection is architecturally more ambitious than anything surveyed.
### 2026-04-13 — Issue #88 docs sync completed for PR #90
- Reconciled the editability/documentation story across `docs/PreceptLanguageDesign.md`, `docs/EditableFieldsDesign.md`, `docs/RuntimeApiDesign.md`, and `docs/McpServerDesign.md`.
- Validation recorded on branch `squad/88-docs-reconcile-editability`: `git diff --check` and `dotnet test --no-restore`.
- Commit `9ea5609` pushed; PR #90 checklist updated and left clean for review.

### 2026-04-12 — Squad `@copilot` lane retirement contract review
- Confirmed the change is narrowly scoped to retiring the Squad-owned `squad:copilot` routing lane while preserving general repo-wide Copilot tooling.
- Required all live workflows, mirrored templates, and squad docs to agree so the lane is retired cleanly.

### 2026-04-12 — Issue #9 design review resolutions incorporated
- Folded the resolved design decisions back into the issue body, including null-handling, split diagnostics (C72/C73), and inspect trace expectations.
- Design left implementation-ready after proposal/body synchronization.
