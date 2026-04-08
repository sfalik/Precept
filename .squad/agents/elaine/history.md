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

### 2026-05-02 — Interactive Guidance System for Concept 17

- Added three-layer interactive guidance to the Interactive Journey prototype (Concept 17): visual emphasis on controls, 5-step coach mark tour, and contextual step hints with golden-path suggestion.
- Visual emphasis pattern: CSS pulse-ring animation (::after pseudo-element) on Fire ▶ buttons draws the eye without blocking interaction. Low-opacity (≤45%), slow cycle (2.4s), cyan for suggested events.
- Coach mark pattern: spotlight overlay using box-shadow trick + positioned card. 5 steps covering layout, fire controls, topology rail, undo/reset, and the hint system itself. Accessible from welcome banner and status bar.
- Golden-path suggestion: data-driven SUGGESTED_JOURNEY array maps state → recommended next event. Generates "▶ suggested next" badge on event cards and contextual step hints after each fire. Non-blocking — users can diverge freely.
- Welcome banner on first load explains the interaction model and offers two CTAs: "Got it" (dismiss) and "🎓 Take the tour" (start coach marks). Disappears after first event is fired.
- All guidance elements are independently dismissible and session-scoped (no persistence). Appropriate for a prototype.
- Updated index page Concept 17 card to mention guided tour and suggested-next badges.
- Decision record at `.squad/decisions/inbox/elaine-guided-journey.md`.
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

### 2026-05-02 — Preview Recombination Pass: Flow Cards (Concept 13)

- Shane observed that Timeline/history is stronger for production UI than dev preview — manual simulation rarely builds deep history. Asked for a mockup combining Focus/Spotlight's large-card aesthetic with a whole-flow view.
- Created Concept 13: Flow Cards at `tools/Precept.VsCode/mockups/preview-reimagined-13-flow-cards.html`. Combines large-card spotlight (from 04), vertical scrolling (from 05), and whole-flow topology (from 11) into a single concept.
- Key design: vertical scroll of state cards connected by transition edges. Current state gets spotlight treatment (36px name, glow, full data/events/rules). Other states are compact but expandable. Left-rail mini-diagram for spatial context.
- First mockup using InsuranceClaim sample (6 states, 7 events, 8 fields including set<string>, 3 invariants, compound guards, edit declarations) — validates layout at real-world complexity.
- First mockup rendering collection fields (set items as chips with count), rejected events with guard conditions and rejection messages, and edit-declaration badges.
- Timeline (01) reclassified from Tier 1 to production-UI pattern. Flow Cards (13) is new top recommendation for dev preview. Notebook (05) remains strong secondary.
- Updated index to 13 concepts with new Phase 5 section and revised recommendation. Updated deep-analysis doc with Concept 13 addendum and revised tier ranking.
- Decision record at `.squad/decisions/inbox/elaine-preview-recombination.md`.
- Mockup directory now has 18 files: 3 original + 11 reimagined concepts + 1 new (Flow Cards) + 1 index + 1 shared CSS + 1 deep analysis doc.

### 2026-05-02 — Contract Explorer: Concept 14 (Refinement Pass)

- Created Concept 14: Contract Explorer at `tools/Precept.VsCode/mockups/preview-reimagined-14-contract-explorer.html`. Refines Flow Cards (13) into a shipping-candidate shape by integrating five features identified as gaps in the deep analysis.
- Key additions over Concept 13: (1) Inline Execution Trace — expand any event to see the 6-stage fire pipeline with expression evaluation, guard matching, and skip/fail states. (2) Structured violation detail with source-kind badges (transition-rejection), expression text, because-reasons, and clickable target-field links. (3) Edit-in-place — editable fields show inline toggle directly in the data grid. (4) What-if bar — hypothetically change a field and see all event outcomes re-evaluate. (5) All 6 outcome kinds with distinct visual badges (Transition, NoTransition, Rejected, ConstraintFailure, Undefined, Unmatched). (6) Mode tabs (Flow / Matrix / Notebook) in the header.
- Concept 12 (Execution Trace) is now integrated into Concept 14 as an inline expandable panel rather than a standalone concept.
- Reorganized the concept index into three tiers for Shane's review: Leading Direction (14), Strong Secondary Modes (05, 03, 09), and Exploration Archive (all others). Concept 12 marked "✓ Integrated" and Concept 13 labeled as predecessor.
- The pipeline trace for Approve shows the full debugging flow: event asserts pass (Amount > 0, Note null check) → row selection fails (guard `(!PoliceReportRequired || MissingDocuments.count == 0) && Amount ≤ ClaimAmount` evaluates to false because MissingDocuments has 1 item) → fallback reject row matches → stages 3–6 skipped.
- What-if bar shows FraudFlag toggled to true, demonstrating exploratory debugging without committing changes.
- Decision record at `.squad/decisions/inbox/elaine-preview-ux-progress.md`.
- Mockup directory now has 21 files: 3 original + 11 reimagined concepts + 2 refined (Flow Cards + Contract Explorer) + 2 history-aware (Journey + Navigator) + 1 index + 1 shared CSS + 1 deep analysis doc.

### 2026-05-02 — History + Inspection Hybrid Pass: Journey (15) and Navigator (16)

- Shane observed that even though Timeline (01) is a production-UI pattern, there's value in lightweight history awareness during dev simulation — even 2–3 steps give useful context. Asked for a hybrid combining history (where we came from) with inspection (where we go next).
- Created Concept 15: Journey at `tools/Precept.VsCode/mockups/preview-reimagined-15-journey.html`. A temporal narrative: Past→Present→Future as a single vertical scroll. Past section shows compact trail of steps with data deltas. Present section is the hero card with "recently changed" markers connecting field values to the step that set them. Future section groups available events by outcome type (transitions, self-loops, blocked). Closes with "also reachable" topology hints.
- Created Concept 16: Navigator at `tools/Precept.VsCode/mockups/preview-reimagined-16-navigator.html`. Flow Cards (13) with history threaded through — three visual layers: visited cards (warm, step numbers, mini deltas), current card (spotlight with data provenance), unvisited cards (dimmed, event hints). Left rail shows traced path. Transition edges distinguish "traced" (taken) from "available" (not yet taken).
- Key design insight: Journey (15) organizes by *time* — you see only what's relevant to your path. Navigator (16) organizes by *topology* — you see the whole machine but with your path highlighted. These answer different sub-questions of "where am I?"
- Key design insight: data provenance annotations ("← set at #1") are valuable in both concepts. They connect current field values to the simulation step that produced them without requiring the developer to remember or scroll back through history.
- The "recently changed" border treatment on data fields (green-tinted border on fields modified in recent steps) is a low-cost, high-value addition to any concept — it could be added to Contract Explorer (14) regardless of which history approach ships.
- Concept 14 (Contract Explorer) remains the leading feature-complete direction. Concepts 15–16 explore an orthogonal dimension (history awareness) that could be layered onto 14. Strongest synthesis: Contract Explorer + Navigator's history layer.
- Updated index to 16 concepts with new Phase 7 section, revised intro, new concept cards, and updated recommendation.
- Decision record at `.squad/decisions/inbox/elaine-history-inspection-hybrid.md`.
- Mockup directory now has 21 files: 3 original + 11 reimagined concepts + 1 new (Flow Cards) + 1 Contract Explorer + 2 new (Journey + Navigator) + 1 index + 1 shared CSS + 1 deep analysis doc.

### 2026-05-02 — Interactive Journey Prototype: Concept 17

- Created Concept 17: Interactive Journey at `tools/Precept.VsCode/mockups/preview-reimagined-17-interactive-journey.html`. First fully clickable prototype in the mockup series — synthesizes Concepts 14 (Contract Explorer), 15 (Journey), and 16 (Navigator) into a playable simulation.
- Key features: fire events with typed args, state transitions with toast feedback, past trail with data deltas, data provenance annotations ("← set at #N"), guard enforcement with rejection messages, edit-in-place (FraudFlag toggle in UnderReview), undo/reset, topology rail with visited/current/unvisited layers, terminal state completion summaries.
- Interaction pattern established: "fill args → click Fire ▶ → watch Past grow + Present update + Future recalculate." This fire-and-observe loop should be the primary interaction for any production implementation.
- Toast feedback pattern: green for transitions, muted green for self-loops, red for rejections. Quick, non-modal, auto-dismissing after 2 seconds.
- Data provenance as micro-annotation ("← set at #N") validated as a low-cost pattern that gives causal context without a separate history view.
- Undo is single-step with full data restoration — not a scrubbing debugger, just "try a different path."
- The suggested play-through: Submit → AssignAdjuster → RequestDocument → ReceiveDocument → Approve → PayClaim covers all outcome types (transition, no-transition, rejected fallback) and the full data lifecycle.
- Updated index to 17 concepts with interactive prototype prominently featured at top with green border and suggested play-through instructions.
- Decision record at `.squad/decisions/inbox/elaine-clickable-history-mockups.md`.
- Mockup directory now has 22 files: 3 original + 11 reimagined concepts + 1 Flow Cards + 1 Contract Explorer + 2 Journey/Navigator + 1 Interactive Journey + 1 index + 1 shared CSS + 1 deep analysis doc.

### 2026-05-03 — Right-Rail Animated State Diagram for Concept 17

- Added an SVG-based animated state diagram to the right rail of the Interactive Journey prototype (Concept 17). Shows the full InsuranceClaim topology: 6 state nodes, 5 transition edges with directional arrowheads, and 2 self-loop indicators (Submitted, UnderReview).
- Node layout: vertical flow with a fork at UnderReview → Approved/Denied. Approved → Paid continues below. Terminal states (Denied, Paid) use dashed strokes.
- Color coding: current state = violet fill + pulsing glow ring, visited = dusk stroke, unvisited = dim border. Same palette as existing rail and breadcrumb.
- Edge animations: traversed edges glow violet; the most-recently-taken edge flashes green → violet (900ms ease-out). Self-loop curves flash green when a self-loop event fires.
- Edge labels ("Submit", "Assign", "Approve", "Deny", "Pay") at 6.5px, dim by default, brighten when traced/active. Offset from midpoints to avoid overlapping the fork.
- Progress indicator: "N of 6 visited" text below the diagram, tracks exploration coverage.
- Undo and reset fully update the diagram — visited-state set and edge-traced calculations derive from sim.history.
- Coach mark tour expanded from 5 to 6 steps: new step 4 "The State Diagram" with `position: 'left'` targeting the right rail. Added CSS positioning support for left-side coach cards.
- Responsive: diagram rail hidden at ≤700px (collapses to 2-column layout), left rail additionally hidden at ≤500px (1-column).
- Updated index page: description updated to mention state diagram, tag added (violet "state diagram"), helper text updated to "6 quick steps."
- Key design decision: the diagram intentionally does NOT duplicate data, events, or rule detail from the journey scroll. It shows only topology + traversal state — reinforcing spatial awareness without information overload.
- 2026-05-03 — For linked UX issues #1 and #7, the working branch is `squad/1-7-inspector-preview-redesign` off `main`. Preserve unrelated in-flight notes such as `.squad/agents/frank/history.md` when branching so setup work stays non-destructive.

- 2026-05-17 — README hero DSL text sizing: image-based approaches (PNG or SVG) are fundamentally brittle for text-alongside-text on GitHub — the image and page text live in disconnected scaling contexts. `<font color>` inside `<pre>` is a potential middle ground (native text + syntax color) but depends on deprecated HTML and needs dual-theme testing. Fenced code block is the only guaranteed-consistent approach. Long-term path is Linguist registration for native `` `precept `` highlighting.
- 2026-05-17 — Frank's architectural analysis of the PNG sizing problem is thorough and correct on the core constraint. His recommendation (fenced code block) is the same as mine, and that tradeoff is now preserved in `.squad/decisions.md`.
- 2026-05-18 — Regenerated `readme-hero-dsl.png` to match GitHub's 830px max image display width. Previous image (1268px) scaled to 830px, shrinking code text to ~8.5px vs GitHub's ~13.6px code blocks. New approach: capture at 830px viewport with 2× deviceScaleFactor → 1660px image. Code text now renders at ~13px on GitHub. Added `design/brand/capture-hero-dsl.mjs` Playwright script for reproducible regeneration. Final width contract is now preserved in `.squad/decisions.md`.
- 2026-05-18 — GitHub README image width reference: repo README view caps at 830px (per wh0/banner-width research). The `.markdown-body` container maxes at 980px with 45px padding. Different views have different caps: markdown view 1012px, VS Code extension 882px, VS Marketplace ~711px.
