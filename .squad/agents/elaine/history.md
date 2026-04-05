## Core Context

- Owns UX/design for README structure, brand-spec surfaces, palette application, and cross-surface scannability.
- Locked README principles: mobile-first above-the-fold layout, single primary CTA, semantic heading hierarchy, progressive disclosure, viewport resilience, screen-reader compatibility, and AI-parseable structure.
- Brand surfaces separate identity palette from syntax/runtime semantics. Emerald, Amber, and Rose carry runtime meaning; Gold is a judicious brand-mark accent, not a general UI color lane.
- Peterman owns public prose; Elaine owns form, layout, and surface presentation rules. Hero samples must remain legible in plaintext and constrained viewports before any decorative treatment.

## Recent Updates

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

## Learnings

### 2026-07-17 - SVG Hero Proposal for Issue #4
- Proposed "The Contract That Says No" — a dark-canvas SVG hero that stages the Subscription lifecycle as a visual narrative with the rejected reactivation as the focal punchline.
- Key learning: Developer-facing "cute/funny" lives in structural irony (elegant flow vs. blunt refusal), not illustration. The comedy is the contrast between the system's precision and the futility of the bad request. Gold rejection callout provides warmth without decoration.
- Key learning: Hero SVGs need one focal point. Split-panel and triptych layouts compete for attention; a single canvas with spatial zones (flow row → rejection row) creates narrative sequence without chrome.
- Key learning: At hero scale, abstract brand marks feel empty. The brand mark works at icon scale because it's symbolic; a hero needs content and a story to anchor comprehension.
- Delivered to `.squad/decisions/inbox/elaine-svg-hero-proposal.md` for Shane review. Covers composition, visual language, tone, README structure, and three alternatives considered.

### 2026-07-17 - SVG Hero First-Pass Implementation for Issue #4
- Created draft SVG at `brand/readme-hero.svg` — a hand-authored static SVG hero showing the Subscription lifecycle as a state diagram with the rejected reactivation as the visual punchline.
- Created design spec at `.squad/decisions/inbox/elaine-readme-hero.md` with concept, composition, color mapping, open questions, and alternatives considered.
- Key learning: The "one event, three states, three outcomes" narrative is the strongest visual argument for the product thesis — Activate does three different things depending on state, and one of them is structurally impossible.
- Key learning: GitHub SVG rendering is server-side via `<img>` tags. Font availability is limited; design must degrade gracefully to any monospace font. Colors and shapes carry the brand identity, not typeface.
- Key learning: Rose in the README hero is a legitimate design tension. Brand-spec says "Do not use Rose in README marketing surfaces" but the hero IS a product diagram (§2.2 visual language), not marketing decoration. Flagged as open question for Shane.
- Key learning: The SVG Hero Approval Framing skill (`.squad/skills/svg-hero-approval-framing/SKILL.md`) correctly predicts that separating strategy approval from execution polish prevents stalls. The design spec frames the ask around message/hierarchy/concept, not pixel detail.
- Open: Three questions for Shane — Rose usage, tagline text, and whether additional supporting copy is needed between hero and Quick Example.
