## Core Context

- Owns README.md, brand/, marketplace copy, and developer-facing narrative. Public copy must describe implemented behavior only.
- Hero samples are both brand assets and AI-facing teaching artifacts. Line economy, plaintext legibility, and verbatim reuse rules matter more than decorative rendering.
- brand/brand-spec.html is the reusable brand reference; README.md is the live public surface. When the hero changes, both surfaces must move together.
- Surfaces-first documentation means exact code blocks, accurate temporary/permanent status, and careful synchronization of color-family terminology and cross-surface rules.

## Recent Updates

### 2026-04-05 - Rule framing reinforced by readability review
- Pushed proposal #8 toward a narrow, English-ish rule concept that reads like authored business policy rather than academic programming vocabulary.
- Added the explicit readability bar that language examples should feel closer to configuration or scripting than to a general-purpose programming language.

### 2026-04-05 - README hero PNG fallback recorded
- README.md now uses brand/readme-hero-dsl.png for the GitHub-facing contract sample, with a collapsed copyable DSL fallback.
- .squad changes from Peterman logged and committed.
- Key paths: README.md, brand/readme-hero-dsl.png, brand/readme-hero-dsl.precept.

### 2026-04-05 - Early product-shape expansion recorded
- Expanded `docs/HowWeGotHere.md` with the original `FiniteStateMachine<TState, TEvent>` surface, nested `WhenStateIs(...).AndEventIs(...).TransitionTo(...)` authoring, and the later builder/inspection phase before the DSL shift.
- Updated `repo-journey-summary` so future history passes ground thin early periods in named README, source, and test snapshots.

### 2026-04-05 - API evolution clarification recorded
- Revised `docs/HowWeGotHere.md` so the chronology explicitly traces the fluent-interface experiments, builder API phase, and current DSL-centered runtime.
- Captured the clarification as a documentation-only correction so future readers do not infer a direct jump to the DSL.

### 2026-04-05 - Journey framing for trunk return recorded
- Drafted 'docs/HowWeGotHere.md' to explain the branch journey and the decisions that survived into the current strategy.
- Framed trunk consolidation as editorial curation rather than merge ritual, reinforcing the team's keep/defer/archive lens for the eventual cutover.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

## Learnings

### 2026-04-05 - README hero PNG fallback
- `README.md` now uses `brand/readme-hero-dsl.png` for the GitHub-facing contract sample, with the source kept in a collapsed copyable block instead of styled inline HTML.
- For branded DSL samples on GitHub, image-first presentation plus an intentional plaintext fallback is safer than relying on GitHub to preserve custom code styling.
- Key paths: `README.md`, `brand/readme-hero-dsl.png`, `brand/readme-hero-dsl.precept`.

### 2026-04-05 - Philosophy filter for DSL compactness proposals
- Compactness proposals land cleanly only when they strengthen Precept's core story: deterministic inspectability, keyword-anchored structure, and the split between data truth (`invariant`) and movement truth (`assert`).
- Named guard reuse across `when`, `invariant`, and state `assert` fits that philosophy when framed as a named business predicate, not as a general alias or macro system.
- Key paths: `docs\PreceptLanguageDesign.md`, `docs\research\dsl-expressiveness\README.md`, `docs\research\dsl-expressiveness\expression-feature-proposals.md`, `.squad\decisions\inbox\j-peterman-philosophy-pass.md`.
