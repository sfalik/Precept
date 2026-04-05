## Core Context

- Owns README.md, brand/, marketplace copy, and developer-facing narrative. Public copy must describe implemented behavior only.
- Hero samples are both brand assets and AI-facing teaching artifacts. Line economy, plaintext legibility, and verbatim reuse rules matter more than decorative rendering.
- brand/brand-spec.html is the reusable brand reference; README.md is the live public surface. When the hero changes, both surfaces must move together.
- Surfaces-first documentation means exact code blocks, accurate temporary/permanent status, and careful synchronization of color-family terminology and cross-surface rules.

## Recent Updates

### 2026-04-05 - README quick example now points to the inline contract
- Removed the standalone contract links from README.md so the hero's inline DSL block is the single quick-example reading path.
- Preserved `brand/readme-hero.svg`, `brand/readme-hero-dsl.html`, and `brand/readme-hero-dsl.precept` as companion artifacts rather than README-promoted destinations.

### 2026-04-05 - Elaine delivered README hero draft for brand review
- Elaine produced `brand/readme-hero.svg` and a review spec centered on the Subscription lifecycle hero concept.
- Brand review now needs to resolve whether Rose is acceptable on the README hero surface and whether any extra supporting copy should sit between the hero and Quick Example.

### 2026-04-08 - Canonical hero snippet shifted from styled preview to reusable text contract
- Reworked brand/brand-spec.html section 2.6 so it mirrors the live README hero verbatim, keeps the sample explicitly temporary, and defines reusable rules for README, VS Code Marketplace, NuGet, Claude Marketplace, and AI-facing contexts.
- Corrected drift from the live README and removed the false implication that a standalone samples/Order.precept file already existed.
- Key learning: hero snippets are text contracts first. The canonical artifact is the exact text plus transport rules, not the prettiest rendering.

### 2026-04-08 - README copy polish pass completed
- Tightened the hook, AI-native tooling sentence, unified-domain-integrity phrasing, and live-editor wording without reopening structure.
- Key learning: once structure is locked, copy polish should remove redundant qualifiers and strengthen cadence without changing meaning.

## Learnings

- When a README hero graduates from a teaching sample to an SVG product surface, the approval ask should center on message, composition, and README hierarchy — not on raw vector execution details. Keep the practical code proof, but move it lower so the first screen sells the category before it teaches syntax.
- VS Code markdown preview will not help a linked standalone HTML hero; if the README itself needs to teach the DSL, inline the contract in `README.md` with README-safe HTML (`<pre><code>` plus inline span styling) and keep `brand\readme-hero-dsl.html`, `brand\readme-hero-dsl.precept`, and `brand\readme-hero.svg` as companion artifacts.
