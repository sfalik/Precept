## Core Context

- Owns architecture, language design, and final review gates across runtime, tooling, and documentation.
- Co-owns language research with George and keeps `docs/PreceptLanguageDesign.md` aligned with actual implementation.
- Standing architecture rules: keep MCP as thin wrappers, keep docs factual, surface open decisions instead of inventing behavior, and preserve philosophy boundaries.
- Historical summary (pre-2026-04-13): drove proposal/design work for conditional guards, event hooks, verdict modifiers, modifier ordering, computed fields, and related issue-review passes; also reviewed the Squad `@copilot` lane retirement and other cross-surface contract updates.

## Learnings

- Issue #8 (named rule declarations) rejected by owner as too close to function semantics. The `rule` keyword is now free for issue #96's simpler purpose: renaming `invariant` to `rule` as a declarative noun keyword. Closed as not_planned with rationale comment.
- RulesDesign.md false "Implemented" status corrected to "Not implemented — superseded." This was a documentation sync violation that persisted across sessions until caught during issue #96 review.
- Issue #96 rewritten as a philosophy-grounded proposal. Core thesis: `invariant`/`assert` are jargon that contradicts Precept's identity as a domain-expert-readable contract language. `rule`/`ensure` follow the established noun-verb grammar pattern and make the keyword itself signal enforcement timing (timeless vs temporal). Two keywords are better than one because the keyword signals the constraint category without requiring positional context.
- Design Principle #5 update approved by owner (Shane) — the principle's semantic distinction (data-truth vs movement-truth) is preserved, only the vocabulary changes. Decision recorded in `.squad/decisions/inbox/frank-rule-ensure-principle5-update.md`.
- Issue #96 owner decisions (2026-04-15): Shane locked 5 decisions after design review — hard cutover (no deprecated aliases), internal C# type renames, MCP DTO renames (breaking contract accepted), archive RulesDesign.md, and gold syntax highlighting for `rule`/`ensure` via `keyword.other.grammar.precept`. All incorporated into issue body; decision record at `.squad/decisions/inbox/frank-issue96-owner-decisions-locked.md`.
- Computed fields: the critical semantic contract is one recomputation pass after all mutations and before constraint evaluation.
- Modifier ordering: parser rigidity lived mostly in parser/completion surface; runtime/model layers were already largely order-independent.
- Guarded declarations: scope-inherited guards are the correct model; implementation gaps should be treated separately from design soundness.
- Docs work: `PreceptLanguageDesign.md`, editability/runtime docs, and MCP docs must stay synchronized whenever wording around constraints, updates, or inspection/editability changes.

## Recent Updates

### 2026-04-15 — Issue #96 owner decisions incorporated into proposal
- Shane made 5 owner decisions after design review: hard cutover (no backwards compat), internal type renames, MCP DTO renames (breaking contract accepted), archive RulesDesign.md, gold highlighting for constraint keywords.
- Updated issue #96 body: removed deprecated alias sections, added Breaking Changes section, added Owner Decisions section, updated Migration Path/Impact/Acceptance Criteria throughout.
- Created decision record at `.squad/decisions/inbox/frank-issue96-owner-decisions-locked.md`.

### 2026-04-15 — Issue #96 rule/ensure proposal rewrite + blocker resolution
- Closed issue #8 (named rule declarations) as not_planned per owner decision — too close to function semantics.
- Fixed RulesDesign.md false "Implemented" status to "Not implemented — superseded."
- Rewrote issue #96 as a thorough, philosophy-grounded proposal: `invariant` → `rule` (noun), `assert` → `ensure` (verb). Includes precedent survey, noun-verb mapping, migration path, full impact analysis (runtime/tooling/MCP), and behavioral acceptance criteria.
- Created decision note for Principle #5 vocabulary update with owner sign-off.

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
