## Core Context

- Owns UX/design across README form, brand-spec presentation, semantic visual surfaces, and preview/mockup direction.
- Keeps visual decisions aligned with runtime semantics: Emerald/Amber/Rose are semantic runtime signals; Gold is a restrained brand accent.
- Historical summary (pre-2026-04-13): led README layout/hero passes, semantic-visual-system refinements, preview-concept explorations, and verdict-modifier UX analysis.
- Owns UX/design for README structure, brand-spec surfaces, palette application, and cross-surface scannability.
- Locked README principles: mobile-first above-the-fold layout, single primary CTA, semantic heading hierarchy, progressive disclosure, viewport resilience, screen-reader compatibility, and AI-parseable structure.
- Brand surfaces separate identity palette from syntax/runtime semantics. Emerald, Amber, and Rose carry runtime meaning; Gold is a judicious brand-mark accent, not a general UI color lane.
- Peterman owns public prose; Elaine owns form, layout, and surface presentation rules. Hero samples must remain legible in plaintext and constrained viewports before any decorative treatment.

## Recent Updates

### 2026-04-15 — Temporal Literal UX Analysis
- Created comprehensive 10-dimension UX analysis at `.squad/decisions/inbox/elaine-temporal-literal-ux.md` evaluating Options A–F and beyond for temporal literal syntax.
- **Recommendation: `date(2026-01-15)` (Decision #18) is the right choice.** It wins on first encounter (self-documenting), writing from memory (familiar constructor pattern), reading in context (natural English cadence), error recovery (richest diagnostic context), AI readability (explicitly typed), and scaling to all 8 temporal types.
- Key UX insight: delimiter-only forms (`#`, `@`, `'...'`) collapse at complexity — they work for simple dates but become opaque for `zoneddatetime(...)`, `timezone(...)`, `duration(PT72H)`, and `period(P1Y6M)`. Only the typed-keyword form stays self-documenting across all 8 types.
- Key UX insight: the "awkwardness" Shane noticed is an expectation mismatch (dates have a keyword wrapper while numbers don't), not a syntax deficiency. The wrapper does real work — disambiguation, self-documentation, tooling enablement — and the postfix unit pattern (`30 days`) provides the zero-ceremony escape valve for the most common temporal expressions.
- Enhancement recommendation: syntax highlighting should render the `date` keyword in typed literals using the same hue as the `date` type in field declarations, reinforcing the type connection visually.

### 2026-04-05 - Kanban concept decision merged into squad record
- Moved Concept 11's kanban-board preview decision from .squad/decisions/inbox/elaine-kanban-preview-concept.md into .squad/decisions.md during the proposal-expansion consolidation pass.
- Preserved the core recommendation: use kanban as a complementary lifecycle-overview mode for simpler linear precepts, not as a replacement for Timeline, Storyboard, or Notebook views.

- Team update (2026-04-04T23:02:22Z): Hero snippet source of truth now lives in brand/brand-spec.html section 2.6, mirrors the README verbatim, stays TEMPORARY, and treats plaintext reuse as canonical across README, VS Code Marketplace, NuGet, Claude Marketplace, and AI contexts. Decision by J. Peterman.

### 2026-04-06 - "carrier" renamed to "signal" throughout semantic-visual-system.html
- Replaced all 21 instances of "carrier/carriers/Carrier/Carriers" with "signal/signals/Signal/Signals" per team decision 2026-04-06.
- Hero conviction updated: "Carriers may adapt." → "Forms may adapt." (signal is stable; form adapts).
- Stance-words definition updated: "carrier = visual form inside that context" → "signal = visual mechanism inside that context".
- Nav href="#carriers" updated to href="#signals"; section id updated to match.
- Three non-task-list instances also caught and corrected: "Carrier medium" (inheritance checklist), "Carrier rules" (boundary note card), footer footnote.

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

### 2026-04-11 — Verdict Modifiers UX Perspective Pass

- Created comprehensive UX analysis at `research/language/expressiveness/verdict-modifier-ux-perspective.md`.
- Key finding: **State verdict modifiers are genuinely novel UX territory** with zero precedent in comparable systems (XState, BPMN, Kubernetes, UML). This is a natural differentiator for Precept's governance positioning.
- Visual integration: Verdict modifiers layer as authored-intent beneath runtime verdicts. Authored verdicts render at 60% opacity; runtime outcomes overlay at 100% if different. Opacity distinction prevents false confidence.
- Minimum viable treatment: Small badge glyphs (✓, ✕, ⚠) in node corners, not full fills. Emerald/Rose/Amber from semantic system. No visual noise, high semantic clarity.
- Interaction pattern established: Completions scaffold verdicts toward consistency (not hard requirement). Authoring suggests `success` for events with only transition outcomes; `error` for events with any rejection rows.
- Diagnostic code C60 introduces verdict mismatch messaging: "Event declared success, but produced rejection." Educates without blocking; info/warning tier.
- Ship order recommendation: Events first (strong precedent, low noise), rules second (builds on events), states third (highest novelty, requires post-launch validation).
- Key UX risks identified and mitigated: visual noise (badges only), precedent confusion (lead with differentiator), authored/runtime distinction (opacity + diagnostics), authoring friction (optional, not required).
- Decision note filed at `.squad/decisions/inbox/elaine-verdict-ux-perspective.md` for Shane/Frank review.
- State verdict emerges as the differentiator: diagram tells a governance story (happy paths in green, error paths in red). This is compellingly unique to Precept.

## Learnings

- README form work is mostly hierarchy, line economy, and plaintext resilience.
- Preview concepts must be validated against realistic sample complexity, not only small demo precepts.
- Verdict modifiers are strongest as subtle authored-intent cues layered beneath runtime outcomes.

## Recent Updates

### 2026-04-11 — Verdict modifiers UX perspective
- Recommended badge-level authored verdict cues, not full-surface fills, to preserve clarity and avoid false confidence.
- Identified state verdicts as novel differentiator territory for Precept tooling and storytelling.
