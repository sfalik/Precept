## Core Context

- Owns README.md, brand/, marketplace copy, and developer-facing narrative. Public copy must describe implemented behavior only.
- Hero samples are both brand assets and AI-facing teaching artifacts. Line economy, plaintext legibility, and verbatim reuse rules matter more than decorative rendering.
- brand/brand-spec.html is the reusable brand reference; README.md is the live public surface. When the hero changes, both surfaces must move together.
- Surfaces-first documentation means exact code blocks, accurate temporary/permanent status, and careful synchronization of color-family terminology and cross-surface rules.

## Recent Updates

### 2026-04-05 - API evolution clarification recorded
- Revised `docs/HowWeGotHere.md` so the chronology explicitly traces the fluent-interface experiments, builder API phase, and current DSL-centered runtime.
- Captured the clarification as a documentation-only correction so future readers do not infer a direct jump to the DSL.

### 2026-04-05 - Journey framing for trunk return recorded
- Drafted 'docs/HowWeGotHere.md' to explain the branch journey and the decisions that survived into the current strategy.
- Framed trunk consolidation as editorial curation rather than merge ritual, reinforcing the team's keep/defer/archive lens for the eventual cutover.

## Learnings

### 2026-04-05 - API evolution belongs in the chronology
- When a product moves from fluent interfaces to a builder surface and then to a DSL-centered runtime, that progression should appear inside the historical timeline, not as an orphan aside. It clarifies that the DSL was a strategic shift in authoring model, not just a syntax refresh.
