## Core Context

- Owns UX/design for README structure, brand-spec surfaces, palette application, and cross-surface scannability.
- Locked README principles: mobile-first above-the-fold layout, single primary CTA, semantic heading hierarchy, progressive disclosure, viewport resilience, screen-reader compatibility, and AI-parseable structure.
- Brand surfaces separate identity palette from syntax/runtime semantics. Emerald, Amber, and Rose carry runtime meaning; Gold is a judicious brand-mark accent, not a general UI color lane.
- Peterman owns public prose; Elaine owns form, layout, and surface presentation rules. Hero samples must remain legible in plaintext and constrained viewports before any decorative treatment.

## Recent Updates

### 2026-04-05 - Kanban concept decision merged into squad record
- Moved Concept 11's kanban-board preview decision from .squad/decisions/inbox/elaine-kanban-preview-concept.md into .squad/decisions.md during the proposal-expansion consolidation pass.
- Preserved the core recommendation: use kanban as a complementary lifecycle-overview mode for simpler linear precepts, not as a replacement for Timeline, Storyboard, or Notebook views.

- Team update (2026-04-04T23:02:22Z): Hero snippet source of truth now lives in brand/brand-spec.html section 2.6, mirrors the README verbatim, stays TEMPORARY, and treats plaintext reuse as canonical across README, VS Code Marketplace, NuGet, Claude Marketplace, and AI contexts. Decision by J. Peterman.

### 2026-04-07 - README Form/Shape Pass Applied
- Removed visual noise from the title block, tightened the quick example layout, simplified Getting Started, and reduced section heading density.
- Preserved Peterman's content ownership while making the README easier to scan on GitHub and small viewports.
- Key learning: README structure work is primarily rhythm, hierarchy, and line economy; code-block-safe vertical layouts beat clever side-by-side treatments.

### 2026-04-07 - README Rewrite: Direct Contribution Role Defined
- Locked Elaine's direct contribution areas to README form: title block composition, hero layout constraints, CTA structure, heading hygiene, separators, and contributing-section formatting.
- Key learning: the 60-character line budget for hero code is a layout dependency, not a copy preference.

### 2026-04-05 - Brand Mark Color Revision: Emerald Arrows + Judicious Gold
- Shifted transition arrows to Emerald, strengthened the Gold because accent where approved, and aligned mark-family explanations across the spec.
- Key learning: color meaning must stay semantically consistent across the brand mark, editor surfaces, and documentation.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

## Learnings

- 2026-04-05 — `brand/inspector-panel-review.md` stayed right about brand drift but had gone stale on surface scope. Current source of truth for inspector/preview UX is the combination of `brand/brand-spec.html § 2.3`, `tools/Precept.VsCode/webview/inspector-preview.html`, and `docs/archive/InteractiveInspectorMockup.md`.
- 2026-04-05 — The preview surface is a combined shell, not a standalone inspector: header shell, diagram canvas with in-canvas data lane, and bottom event dock. PRD work should preserve that triad unless Shane approves a structural change.
- 2026-04-07 — Created first redesign mockup at `tools/Precept.VsCode/mockups/preview-inspector-redesign-mockup.html` for issue #7. Key decisions: current-state violet pill in header chrome, field type as secondary metadata row, gold constraint messages (not red), event outcome text inline in dock rows, title changed to "Precept Preview." UX decision record at `.squad/decisions/inbox/elaine-issue-7-mockup.md`.
- 2026-04-07 — Brand color mapping for inspector: States = Violet `#A898F5`, Events = Cyan `#30B8E8`, Enabled = Emerald `#34D399`, Blocked = Rose `#FB7185`, Constraint messages = Gold `#FBBF24`, Field names = Slate `#B0BEC5`, Field types = `#9AA8B5`, Field values = `#84929F`. All from brand-spec § 1.4 + § 2.3.
- 2026-04-07 — The Subscription precept is the best mockup sample: 3 states, 2 events (one with args), invariant, assertion, reject, no-transition — covers every visual state the inspector needs to show.
- 2026-04-07 — Created exploratory mockup at `tools/Precept.VsCode/mockups/preview-inspector-in-diagram-transitions-mockup.html` exploring in-diagram transitions (events as edge-anchored panels instead of bottom dock). Key tradeoffs: spatial context is genuinely better for debugging, but keyboard accessibility regresses and panels overlap at 5+ events. Decision record at `.squad/decisions/inbox/elaine-diagram-transitions-mockup.md`. Needs Shane review before proceeding.
- 2026-04-07 — Mockup directory now has three files: `interactive-inspector-mockup.html` (historical), `preview-inspector-redesign-mockup.html` (issue #7 baseline), `preview-inspector-in-diagram-transitions-mockup.html` (this exploration). All use the same brand palette and Subscription sample data for comparability.
- 2026-04-07 — Created five "reimagined" alternative preview concepts exploring fundamentally different product shapes: (01) Timeline Debugger, (02) Conversational REPL, (03) Decision Matrix, (04) Focus/Spotlight, (05) Notebook/Report. All at `tools/Precept.VsCode/mockups/preview-reimagined-*.html` with shared CSS and an index page. UX decision record at `.squad/decisions/inbox/elaine-preview-reimagined-directions.md`.
- 2026-04-07 — Top recommendation for deeper iteration: Concepts 01 (Timeline) and 05 (Notebook), with 03 (Decision Matrix) as a strong secondary mode. The Timeline uniquely answers "how did I get here?" while the Notebook gives complete progressive-disclosure coverage. A hybrid of the two could be the strongest future.
- 2026-04-07 — Shared CSS pattern established: `preview-reimagined-shared.css` contains brand palette variables, utility classes (state-pill, event-chip, field-name/type/value), and scrollbar styling. All five reimagined mockups reference it. Future mockup explorations should use this shared file for consistency.
- 2026-04-07 — Mockup directory now has 10 files: 3 original + 5 reimagined concepts + 1 index + 1 shared CSS. All existing mockups preserved.
- 2026-04-07 — Phase 2: Created five more reimagined concepts (06–10): Dual-Pane Diff, Rule Pressure Map, Graph Canvas, Storyboard/Scenarios, Dashboard/Control Room. All at `tools/Precept.VsCode/mockups/preview-reimagined-06-*` through `10-*`. Index updated to show all ten with revised recommendation.
- 2026-04-07 — Phase 2 standout: Concept 09 (Storyboard/Scenarios) is the only concept that frames the preview as a test harness with coverage tracking. A Timeline+Storyboard hybrid is the strongest future direction alongside Notebook for comprehensive coverage.
- 2026-04-07 — Concept 07 (Rule Pressure Map) introduces a genuinely novel organizing principle: constraints first, states second. Worth pursuing as a secondary mode for complex precepts with many business rules.
- 2026-04-07 — Concept 06 (Dual-Pane Diff) and 08 (Graph Canvas) are strong utility patterns that could be features within any primary shape rather than standalone directions.
- 2026-04-07 — Mockup directory now has 15 files: 3 original + 10 reimagined concepts + 1 index + 1 shared CSS. All existing mockups preserved. Decision record at `.squad/decisions/inbox/elaine-preview-reimagined-directions-phase-2.md`.
- 2026-04-07 — Phase 3: Created Concept 11 (Kanban Board) at `tools/Precept.VsCode/mockups/preview-reimagined-11-kanban-board.html`. States as columns, entity as a movable card, transitions as connectors with guard labels, ghost cards for history. Index updated to 11 concepts. Best suited for simple-to-moderate linear lifecycles (3–5 states). Scales poorly past 6 states or with many bidirectional transitions. Decision record at `.squad/decisions/inbox/elaine-kanban-preview-concept.md`.
- 2026-04-07 — Kanban concept's unique UX contribution: spatial position answers "where am I?" faster than any other concept. Guard conditions on connectors make the constraint surface visible without drilling in. Ghost cards give history without a timeline. Terminal states (empty columns, no outgoing arrows) are self-documenting.
- 2026-04-07 — Mockup directory now has 16 files: 3 original + 11 reimagined concepts + 1 index + 1 shared CSS.
- 2026-05-02 — Subscription is no longer the best mockup sample for *scaling* evaluation. InsuranceClaim (6 states, 7 events, set collection, 12 rules, guarded reject rows with compound expressions) is the minimum-complexity sample that tests all visual dimensions.
- 2026-05-02 — The fire pipeline's 6 stages (event asserts → row selection → exit actions → row mutations → entry actions → validation) is the most important invisible structure in the preview. No concept visualizes it; Concept 12 (Execution Trace) fills this gap.
- 2026-05-02 — Observable's reactive-cell pattern directly maps to Precept's hypothetical-patch inspect API: change a data field → all event outcomes re-evaluate. This should be the core interaction pattern for the Notebook concept.
- 2026-05-02 — DMN decision-table editors (Camunda, Trisotech) validate the Decision Matrix concept but show that multi-guard cells need sub-table rendering and hit-policy indicators — not just a single outcome badge.
- 2026-05-02 — VS Code webview constraint: sidebar panel width (300–600px) breaks Matrix, Dashboard, and Kanban concepts. Only Timeline (vertical-list fallback), Notebook (single-column cards), and REPL (text reflow) work at sidebar width. Responsive layout is a hard design requirement, not a nice-to-have.
- 2026-05-02 — Collection rendering is a shared-component design problem. Sets need `{ item, item }` with count badge; queues need `[ front → back ]` with peek indicator; stacks need `[ top | ... | bottom ]` with top indicator. 11/21 samples use collections.

### 2026-05-02 — Preview Concepts Deep Analysis Pass

- Conducted research-backed deep pass across all 11 existing preview concepts. Grounded analysis in full language surface (21 samples, 6 outcome kinds, 6 pipeline stages, 3 collection types, structured violation model) and external research (XState/Stately, Redux DevTools, DMN decision table editors, Observable notebooks, Elm debugger, VS Code webview patterns).
- Key finding: all mockups use the simplest sample (Subscription: 3 states, 2 events, 0 collections) — they've never been stress-tested against real complexity. Insurance-claim (6 states, 7 events, set collections) or hiring-pipeline (7 states, 12 transition rows) would expose scaling issues in Matrix, Dashboard, Kanban, and Focus/Spotlight.
- Key finding: critical DSL features invisible in all mockups: collection mutations, entry/exit actions, 3 of 6 outcome kinds, structured violation detail, edit declarations, fire pipeline internals.
- Introduced Concept 12: Execution Trace / Pipeline Debugger — the only concept visualizing the 6-stage fire pipeline's internal stages. Uniquely Precept-specific; shows *why* an event was rejected (which pipeline stage, which expression, which field).
- Updated tier ranking: Tier 1 (primary) = Timeline + Notebook; Tier 2 (strong secondary) = Decision Matrix + Storyboard + Execution Trace; Tier 3 (modes) = Rule Pressure Map + Kanban; Tier 4 (features) = Dual-Pane Diff + Graph Canvas + Dashboard; Tier 5 (situational) = REPL + Focus/Spotlight.
- Created `tools/Precept.VsCode/mockups/preview-concepts-deep-analysis.md` — comprehensive research document with concept-by-concept analysis, external references, scaling assessments, and cross-cutting issues.
- Updated concept index to 12 concepts with deeper descriptions, tier tags, and revised recommendations.
- Decision record at `.squad/decisions/inbox/elaine-preview-concepts-deep-pass.md`.
- Mockup directory now has 17 files: 3 original + 11 reimagined concepts + 1 index + 1 shared CSS + 1 deep analysis doc.

