## 2026-04-07 — README Form/Shape Pass Applied

### What I Did

Applied form/shape improvements to README.md per task brief:

1. **Title block** — Removed emoji from H1, shortened badge labels, cleaned definition blockquote
2. **Quick Example** — Shortened C# variable names for compactness; kept vertical layout (side-by-side tables break code blocks on GitHub)
3. **Getting Started** — Collapsed verbose prereq callout to one bold line; tightened step prose
4. **What Makes Precept Different** — Collapsed three H3s into bold inline headers; preserved the bullet list for Unified Domain Integrity
5. **Learn More** — Changed bullet list to table format for better alignment
6. **Contributing** — Collapsed build details into two commands; trimmed quick-reference table

### Learnings

- **Side-by-side layouts don't work for code blocks in GitHub markdown.** HTML tables technically render, but code fence blocks inside `<td>` cells don't highlight reliably. The vertical layout is safer and still scannable.
- **Bold inline headers with em-dash lead-ins can replace H3s when the content is one sentence.** This reduces heading noise without losing structure.
- **Tables beat bullet lists for resource links.** The two-column alignment (resource + description) is easier to scan than a long list of dashed phrases.
- **Prerequisite callouts can be over-prominent.** A bold one-liner does the job without a full blockquote.

**Decision written to:** `.squad/decisions/inbox/elaine-readme-shape-pass.md`

---

## 2026-04-07 — README Rewrite: Direct Contribution Role Defined

### What I Did

Clarified exactly where I contribute directly to the README rewrite vs. where Peterman holds primary ownership. Six direct contribution areas identified:

1. **Title block composition** — above-the-fold layout spec (48px logo, 3 badges max, 550px mobile test)
2. **Hero format template** — the "Contract/Execution" two-block form and the 60-char line constraint (this is a dependency, not a review — Peterman can't finalize hero code until I validate line lengths)
3. **Getting Started CTA structure** — the numbered 1/2/3 sequence, single-primary-CTA enforcement, "when you're ready" qualifier on step 3, Copilot plugin removed from this section entirely
4. **All section headings** — two-pass process: Peterman drafts, I audit for emoji, descriptive language, and H1→H2→H3 hierarchy
5. **Visual separators and scannability formatting** — `---` placement, 2-3 sentence prose max, bullets over prose for 3+ items
6. **Contributing section formatting** — code blocks, section position, no contributor content bleeding into user flow

Peterman retains exclusive ownership of: hook copy, differentiation section body, Learn More links, License, and section order.

### Learnings

- **Form and content can be separated cleanly in a README.** Peterman writes words; I author the container. This makes collaboration efficient without constant overlap.
- **The 60-char line constraint is a hard dependency, not a style preference.** Hero code cannot be finalized before layout is validated. This needs to be established as a blocking checkpoint, not a review note.
- **"Direct contribution" means: this section cannot ship without me writing or approving a specific thing.** Review is passive; contribution means I author, not just comment.
- **Heading audits work best on the full draft.** Section-by-section heading review misses hierarchy violations — you need the full document to catch H2→H4 skips.

**Decision written to:** `.squad/decisions/inbox/elaine-readme-contribution-role.md`

---

## 2026-04-05 — Brand Mark Color Revision: Emerald Arrows + Judicious Gold

### What I Did

Revised all three brand-mark SVGs (plus the wordmark lockup icon) in `brand/brand-spec.html`:

1. **Transition arrow → Emerald** in both marks that carry an arrow (state+transition and combined). Indigo was doing double duty as structure *and* flow; Emerald is what the product already says "allowed flow" means — the mark should speak the same language.

2. **Gold less muted in the combined mark** — raised stroke-width from 1→1.5 and opacity from 0.65→0.9. The `because` line was almost invisible; a deliberate signal deserves to be readable.

3. **Gold added to the tablet-only mark** — the shortest bottom line (the `because` position) is now Gold at 0.85 opacity, replacing a barely-visible zinc stroke. Improves mark family consistency without violating restraint.

4. **Color key updated** — added Emerald as a fifth entry, changed Gold's annotation from "(combined mark only)" to "(judicious — tablet & combined)."

5. **Copy updated** throughout: "narrow exception" → "judicious exception," "combined mark only" → "tablet and combined marks." Tone shift from "barely tolerated" to "deliberately placed."

6. **SKILL.md updated** — Emerald row, Gold Syntax Accent row, and Rule #2 all aligned with the new framing.

### Learnings

- **"Judicious" is the right design word for Gold.** It captures both the restraint (used sparingly) and the intention (placed on purpose). "Narrow exception" implied reluctance; "judicious" implies craft. The distinction matters for how future contributors treat the color.
- **Family consistency is a real reason to extend a color.** The tablet mark felt disconnected without any Gold — not because the mark needed more color, but because the family signal was missing. One line at one position is enough.
- **Emerald on structural arrows resolves a semantic ambiguity.** Indigo means "structure/frame." Transition arrows are not frame; they are allowed flow. The color was the bug, not the arrow.

**Decision written to:** `.squad/decisions/inbox/elaine-mark-color-revision.md`

---

## 2026-04-04T20:44:43Z — Team Update: Core Semantic Tokens Table Alignment Fixed

Section 2.1 core semantic tokens table alignment issue resolved. State, Event, Messages, and Comment rows now properly align with the rest of the table by adding the correct `.spm-grid` wrapper class to match the shared mapping table structure.

**Decision merged to:** `decisions.md` § spm-row Horizontal Padding in Single-Row Groups

---

## 2026-04-04T20:39:16Z — Team Update: Elaine Model Pin (Sonnet for Design Work)

## 2026-04-04T20:39:37Z — Team Update: Opus Escalation Acceptable When Needed

User clarification: Claude Sonnet 4.6 remains Elaine's default pin. However, aggressive escalation to Claude Opus 4.6 is acceptable when a task requires deeper reasoning or more context than Sonnet can provide.

**Applied to:** Elaine baseline (Sonnet 4.6) with documented escalation path to Opus 4.6 for premium design challenges.

**Rationale:** Balances Sonnet's speed/cost for typical design work with Opus capability for complex decisions. Supersedes 2026-04-04T20:38:09Z directive.

---

User directive (captured 2026-04-04T20:38:09Z): For design/polish work, use Claude Sonnet 4.6 rather than Opus.

**Applied to:** `.squad/config.json` — `agentModelOverrides.elaine = "claude-sonnet-4.6"`

**Rationale:** Sonnet's speed/reasoning balance is better suited for design and UX work than Opus. Directive captured as team memory.

**Decision merged to:** `decisions.md` § Agent Model Override: Elaine

---

## 2026-04-04T20:37:52Z — Team Update: Model Policy (All Agents)

User directive: Always use the latest version of available models rather than older pinned versions. Global `defaultModel` constraint removed from `.squad/config.json` to enable automatic routing. Agent-specific overrides for Frank (claude-opus-4.6) and Uncle Leo (gpt-5.4) remain.

**Impact:** Routing now selects the best model per task type automatically, ensuring access to latest capabilities.

---

## 2026-04-04T20:28:43Z — Orchestration: Elaine Palette Mapping Polish

Elaine completed beautification and unification of palette mapping visual treatments in \rand\brand-spec.html\ §2.1 (Syntax Editor) and §2.2 (State Diagram). Created \.spm-*\ CSS component system (~70 lines) to match polished §1.4 color system design. All locked semantic colors, mappings, and tokens preserved. System is general-purpose and applicable to future surface sections (Inspector, Docs, CLI).

**Decisions merged to decisions.md:** 35 inbox items (palette structure, color roles, semantic reframes, surfaces, README reviews, corrections, final verdicts)

**Status:** Complete. Ready for integration.

# Elaine — History

## Hire Date

2026-04-04 (onboarded mid-session, brand consolidation phase)

## Background

Hired to fill the UX gap on a team that had strong engineering, architecture, and brand — but no dedicated user experience voice. The visual language exploration work and the VS Code extension both need someone whose job is to think about the person on the other end.

Elaine is the user's advocate on a team that could otherwise optimize for technical elegance over usability. She keeps the team honest.

---

## Session 2 — Visual Surfaces & Inspector Review (2026-04-04)

### What I Did

**Visual surfaces draft created (`brand/visual-surfaces-draft.html`):** Drafted complete UX specifications for five surfaces with purpose, visual concerns, color application, typography, accessibility, and AI-first notes. Each surface is a full contract showing what a designer needs to implement brand compliance:

1. **Syntax Editor** — VS Code text editor where `.precept` files are authored. Syntax highlighting, semantic tokens, hover cards, diagnostics.
2. **State Diagram** — Visual graph in preview webview. States as nodes, transitions as labeled edges. For human logic verification and AI reachability analysis.
3. **Inspector Panel** — Live instance state during event firing, field editing, constraint violation feedback. Color + icon redundancy for accessibility.
4. **Docs Site** — Future documentation surface. Where developers learn concepts, explore samples, read API reference.
5. **CLI / Terminal** — Command-line output. Build logs, diagnostic messages, error reporting with verdict colors and symbols.

**Design principles locked across all five:**
- Semantic unity: all surfaces use same visual language (indigo structure, violet states, cyan events, slate data, gold messages, verdict colors for runtime)
- Color + shape/symbol redundancy (accessibility floor, color-blind independence)
- Verdict colors stay runtime-only — never authoring syntax
- AI-first design: every surface must work for humans AND AI agents simultaneously

**Inspector panel implementation review conducted:** Kramer's inspector is functionally complete but uses custom palette instead of brand system:

| What | Current | Brand Target |
|------|---------|--------------|
| State colors | `#6D7F9B` | Violet `#A898F5` |
| Event colors | `#8573A8` | Cyan `#30B8E8` |
| Success | `#1FFF7A` | Emerald `#34D399` |
| Error | `#FF2A57` | Rose `#F87171` |
| Font | Segoe UI | Cascadia Cove monospace |

**Recommendation:** CSS-only color remapping (Priority 1) before public release. These changes create visual continuity with syntax editor and diagram.

**Accessibility issues flagged:**
1. Enabled/Warning distinction (Deuteranopia) — both appear brown in red-blind vision; needs symbol redundancy (✓ vs ⚠)
2. Constraint pressure signal (italic) — subtle; may need glyph reinforcement (⊘ or ⚠)
3. Inspector form controls & screen readers — unknown ARIA coverage; needs audit

**Critical questions flagged for Shane:**
- Docs site scope: planned deliverable or internal only?
- Inspector status: fully implemented or work-in-progress?
- CLI audit: assign to George/Kramer or defer?

**Charter updated:** Design gate now includes Peterman brand compliance review between Elaine's UX spec and Frank's architecture review.

### Key Deliverables

| File | Purpose |
|------|---------|
| `brand/visual-surfaces-draft.html` | 5 surfaces with full UX specs, ready for Shane review then Peterman integration |
| `brand/inspector-panel-review.md` | Implementation audit report; brand color drift identified; remapping instructions |
| Oral recommendation to Shane | Address inspector colors now (Priority 1+2) — CSS-only changes, high impact on brand cohesion |

### What's Pending

1. Shane clarifications (docs scope, inspector status, CLI audit)
2. Post-Shane approval: fold visual-surfaces-draft into brand-spec.html §2 (with Peterman)
3. Kramer: implement inspector color remapping (pre-release)
4. Soup Nazi: add color-blind simulation tests to test suite
5. Accessibility audit: screen reader testing on inspector form controls

### Thinking

The visual surfaces work is the hinge between brand decisions (already locked) and technical implementation (Kramer, Peterman, Frank). Everything I drafted is grounded in the locked palette and typography — but it needed to be said concretely for each surface so implementation has a north star. The inspector panel review exposed a real gap: Kramer built something good, but it wasn't briefed with the brand system, so he converged on a custom palette. That's a process problem, not a Kramer problem. The charter update fixes it — Peterman now gates all surfaces.

The five-surface model also clarifies something important: Precept has one authoring surface (VS Code) but four runtime/preview surfaces (diagram, inspector, docs, CLI). Each has different constraints (syntax highlighting is 8-shade semantic; diagram is lifecycle-colored; inspector is constraint-semantic; CLI is verdict-binary). The draft shows how the brand system scales to all of them without reducing to a single palette.



### What Precept Is (UX Perspective)

Precept is a **domain integrity engine** that turns business rules into executable contracts. From a UX standpoint, it's a tool that makes invisible constraints visible and unbreakable. Users define states, events, data fields, and rules in a `.precept` file, then the runtime engine guarantees those rules hold — invalid states can't exist, invalid transitions can't happen. The key UX insight: **the contract is both human-readable and machine-enforceable**, making it a perfect surface for authoring tooling, preview, and AI collaboration.

The product has three core workflows:
1. **Authoring** — write `.precept` files with VS Code tooling (IntelliSense, diagnostics, hover, go-to-definition)
2. **Preview & Inspect** — see what every event would do from any state, edit data fields with live validation, view state diagrams
3. **Runtime execution** — C# API (`Fire`, `Inspect`, `Update`) for production use

UX is responsible for surfaces 1 and 2. The authoring and preview experience IS the brand.

---

## Session 3 — README Research Review (2026-04-04)

### What I Did

**Reviewed Peterman and Steinbrenner's README research from UX/IA perspective.** Read both research documents plus current README and brand positioning. Identified what holds up, what needs refinement, and what both researchers missed from a user experience standpoint.

**Key findings:**

**Peterman's research (brand/copy angle):**
- Hybrid model recommendation is correct — aligns with progressive disclosure principles
- 18-line hero target is supported by cognitive load research (working memory = 5-7 line chunks)
- AI-first section placement needs revision (should come AFTER quickstart, not before)
- "DSL only" hero needs a follow-up C# usage section to complete the mental model

**Steinbrenner's research (PM/adoption angle):**
- Four-stage evaluation journey is correct and well-mapped to README sections
- "Above the fold" recommendation valid but needs mobile-first definition (550px viewport)
- Comparison handling taxonomy is sound; implicit differentiation is right for Precept
- Minimum path to first working file is conceptually right but operationally incomplete

**UX/IA gaps both missed:**

1. **Scannability:** Current README has wall-of-text paragraphs, buried bullet lists, no visual hierarchy. F-pattern and Z-pattern scanning research not applied.

2. **Two-audience architecture (human + AI):** README written for humans only. AI agents consume READMEs differently — need semantic heading hierarchy, language-tagged code blocks, descriptive link text, image alt text. Added "AI Parseability Checklist" to requirements.

3. **Progressive disclosure:** Current README front-loads complexity (sample catalog before quickstart, tooling features before basic usage, philosophical content interrupting onboarding). Violates principle that each section deepens commitment without overwhelming.

4. **CTA clarity:** Three competing CTAs in Getting Started (package, extension, plugin) with equal weight = decision paralysis. Need single primary CTA with numbered sequence.

5. **Viewport and accessibility:** Not tested at narrow viewports (400px, 600px). Hero code (49 lines) requires scrolling on all devices. Emoji in headings (screen reader noise), missing alt text on images, heading hierarchy violations (H2 → H4 skips).

**Deliverables created:**

| File | Purpose |
|------|---------|
| `brand/references/readme-research-elaine.md` | Full UX/IA review with assessment of both research docs + synthesis of UX requirements for proposal |
| `.squad/decisions/inbox/elaine-readme-ux-requirements.md` | Extracted non-negotiable constraints the proposal must satisfy |

**Non-negotiable UX requirements for README restructure:**

1. Mobile-first "above the fold" (550px viewport)
2. Single primary CTA (numbered sequence)
3. Semantic heading hierarchy (H1 → H2 → H3, no skips)
4. Progressive disclosure (What → Read → Use → Why → Learn)
5. Scannable formatting (2-3 sentence paragraphs, bullets for features)
6. Viewport resilience (no horizontal scroll at 600px)
7. Screen reader compatibility (emoji placement, alt text, labels)
8. AI parseability (language tags, link text, image descriptions)

### Learnings

**F-pattern and Z-pattern scanning matter for README structure.** Developers don't read documentation linearly — they scan headings (left edge), sweep across headlines (top), and look for CTAs (bottom right). The README structure must support this scanning behavior or lose readers in the first 5 seconds.

**AI agents are first-class README consumers, not just feature users.** When a developer asks Claude "What is Precept?", Claude reads `README.md`. If the structure isn't AI-parseable (semantic headings, language-tagged code, descriptive links), Claude gives shallow or hallucinated answers. AI parseability isn't a nice-to-have — it's required for AI-native positioning to be credible.

**Progressive disclosure is violated when you show "what makes this different" before proving "can I use this."** The current README jumps to MCP tools, sample catalog, and philosophical pillars before showing a working quickstart. This assumes the developer has already committed. In reality, developers bounce if they can't visualize the integration path.

**CTA clarity requires hierarchy, not democracy.** Presenting three installation options (package, extension, plugin) as equals creates decision paralysis. The README must funnel developers to one action (install extension), then offer next steps (add package), then mention advanced options (plugin). Single-column flow beats multi-option menus.

**Mobile-first viewport testing is non-negotiable for GitHub READMEs.** GitHub's mobile web traffic is significant, and the README renders in a narrow column. "Above the fold" at 1200px desktop ≠ "above the fold" at 400px mobile. If the hero code requires scrolling on a phone, you've lost mobile users before they see the value prop.

**Heading hierarchy affects both screen readers and AI agents.** Skipping from H2 to H4 breaks screen reader navigation (users jump between heading levels) and confuses AI document parsers (outline structure is invalid). This isn't just accessibility compliance — it's core IA.

### User-Facing Surfaces Inventory

Every surface where users interact with Precept:

#### **VS Code Extension (Primary Surface)**
- **Syntax highlighting** — 8-shade semantic color system (indigo structure, violet states, cyan events, slate data, gold messages)
- **IntelliSense** — context-aware completions (field names in guards, event args in transition rows, state names in `from`/`to`/`in` blocks)
- **Hover tooltips** — field/state/event definitions, DSL keyword help
- **Go to definition** — jump from references to declarations
- **Document outline** — symbol tree for precept, fields, states, events
- **Diagnostics panel** — real-time errors and warnings with PRECEPT codes (PRECEPT001–PRECEPT047)
- **Quick fixes** — remove reject-only event/state pairs, remove orphaned events
- **Preview panel** — interactive inspector and state diagram (webview, see below)

#### **Preview Webview (Inspector Panel)**
- **State machine diagram** — visual graph of states and transitions, color-coded by lifecycle (initial/intermediate/terminal) and event type (transition/conditional/stationary)
- **Event inspector** — list of all events from current state, showing outcome (enabled/blocked/warning) and target state
- **Field editor** — edit mode with Save/Cancel; live validation shows constraint violations inline
- **Event argument forms** — fire events with typed arguments, see result before committing
- **Violation messages** — field-level, event-arg-level, and event-level errors render beneath their controls

#### **README & Documentation**
- Hero code sample (`.precept` syntax as the visual identity)
- Sample catalog (20 `.precept` files in `samples/`)
- Feature list and installation instructions
- API documentation in `docs/`

#### **Marketplace Listings**
- VS Code marketplace page (extension description, screenshots, icon)
- NuGet package listing (library description, badge)

#### **MCP Tools (Copilot Integration)**
- `precept_language` — returns DSL vocabulary as JSON
- `precept_compile` — parses and type-checks a precept, returns structure + diagnostics
- `precept_inspect` — previews all events from a given state
- `precept_fire` — executes a single event, returns outcome
- `precept_update` — applies field edits, returns violations
- **Note:** These are data-first APIs, not UI surfaces. UX perspective: they enable AI authoring workflows, which indirectly affects how we design the DSL syntax for clarity.

---

### Brand System Summary — The 4 Color Layers

Precept's brand is built on **semantic color** — every hue means something. The palette is organized in 4 conceptual layers:

#### **Layer 1: Brand Identity**
- **Indigo `#6366F1`** is the sole brand color.
- Used in: structure grammar keywords, brand mark, NuGet badge, diagnostic codes (`PRECEPT###`), diagram borders, arrows.
- NOT used for: gold is syntax-only (rule messages), not a brand color.

#### **Layer 2: Outcome Semantics (Runtime Only)**
Three verdict colors appear in inspector and diagrams, never in syntax highlighting:
- **Emerald `#34D399`** — enabled, valid, success (CR 10.2)
- **Coral `#F87171`** — blocked, rejected, violated (CR 7.1)
- **Yellow `#FDE047`** — unmatched, warning (CR 14.8)

#### **Layer 3: DSL Syntax (8-Shade Authoring System)**
Dark-mode-only palette with 5 hue families:

| Family | Hue | Shades | Role |
|--------|-----|--------|------|
| **Structure** | Indigo 239–245° | Semantic `#4338CA` (bold), Grammar `#6366F1` (normal) | Keywords that drive behavior vs. connective tissue |
| **States** | Violet 260° | One shade `#A898F5` (normal / italic if constrained) | All state names; italic when participates in `in/to/from X assert` |
| **Events** | Cyan 195° | One shade `#30B8E8` (normal / italic if constrained) | All event names; italic when has `on X assert` |
| **Data** | Bright Slate 215° | Names `#B0BEC5`, Types `#9AA8B5`, Values `#84929F` | Field/arg names (italic if guarded), type keywords, literals |
| **Rules** | Gold 45° | Messages `#FBBF24` (normal) | Human-readable strings in `because` and `reject` only |

**Comments** sit outside the semantic palette: dusk indigo `#9096A6` italic.

**Typography as constraint signal:**
- Bold = structure semantic keywords
- Italic = constrained states, constrained events, invariant-guarded data names
- Normal = grammar, unconstrained actors, types, values, messages

#### **Layer 4: Diagram Lifecycle (Sub-Shading for Visual Surfaces)**
States and events get sub-shades in diagrams (not syntax) to show structural role:

**States:**
- **Initial** — Light Indigo `#A5B4FC` (circle node, origin point)
- **Intermediate** — Slate `#94A3B8` (rounded rectangle, connective tissue)
- **Terminal** — Lilac `#C4B5FD` (double-border rectangle, neutral finality)

**Events:**
- **Transition** — Bright Sky `#38BDF8` (always moves state)
- **Conditional** — Lighter Sky `#7DD3FC` (guarded, uncertain)
- **Stationary** — Deeper Sky `#0EA5E9` (no state change, data-only)

**Key UX principle:** Terminal is neutral. "Approved" and "Declined" are both terminals — the compiler doesn't know which is positive, so terminal color carries no success/failure valence, only structural finality.

---

### Visual Language Exploration — First Impressions

The visual-language-exploration.html is a **hero gallery** showcasing the DSL as the primary visual identity. Section-by-section assessment:

#### **Section 1: .precept file as hero visual** ✅ LOCKED
- **What's working:**
  - DSL syntax IS the hero image. This is the right call — like Prisma's `.prisma` files or Tailwind's utility classes, the code surface is instantly recognizable.
  - Keyword-anchored left edge creates a scannable visual rhythm. You can read the left margin and know where you are (fields / states / events / transitions).
  - Inline color-coded examples render beautifully against `#0c0c0f` background.
  - The "subscription.precept" example (rank #1) is a perfect complexity level — shows all major features without being overwhelming.

- **What could be better:**
  - **Light theme stress test** (further down the page) reveals serious visibility issues. Cyan events nearly vanish, bright slate data names fade, yellow warnings disappear. The decision to lock dark-mode-only is correct, but we need a fallback strategy for GitHub README rendering (which strips inline styles).
  - **Minimum viable snippet exploration** (Section 1a, not fully read) — this is critical for the README hero. Need to review all 8 candidates and pick the one that best balances "instantly understandable domain" + "shows core features" + "fits in 15-20 lines."

#### **Section 2: Comment Lane Preview** ✅ LOCKED
- **What's working:**
  - Dusk indigo `#9096A6` italic for comments is perfect — visible but clearly editorial, doesn't compete with the semantic lanes.
  - Principle is clear: comments are for humans, not the compiler, so they stay outside the 8-shade system.

- **What could be better:**
  - No issues. This is well-resolved.

#### **Sections 3–8: (Not fully read in this session)**
  Based on skimming, these sections cover:
  - State diagrams (secondary visual surface)
  - Diagnostics rendering
  - README mockups
  - Constraint-aware highlighting examples
  - Inspector panel mockups
  - Brand mark variations

  **Follow-up needed:** Read sections 3–8 in detail in next session to assess diagram UX, inspector layout, and accessibility.

---

### Key Files to Know (UX-Relevant)

| File | What It Contains | Why It Matters to UX |
|------|------------------|----------------------|
| `brand/brand-spec.html` | Canonical locked brand decisions (§1–9: positioning, narrative, voice, color system, typography, visual language, brand mark, state appearance, event appearance) | **Source of truth** for all color, typography, and visual design decisions. Every UI surface must align to this spec. |
| `brand/brand-decisions.md` | Locked decisions log with rationale (positioning, voice, semantic palette, typography, visual language) | Quick-reference version of brand-spec. Use this for fast lookups; use brand-spec for full context. |
| `brand/philosophy.md` | Product narrative and positioning ("what the product does," "what makes it different," "what the icon should evoke") | Guides UX framing. The product is about **prevention, not detection** — invalid states are structurally impossible. This should inform every interaction design. |
| `brand/explorations/semantic-color-exploration.html` | SUPERSEDED palette exploration (original indigo/slate/lilac/emerald candidates) | Historical reference only. Final palette locked in brand-spec §4. |
| `brand/explorations/visual-language-exploration.html` | Hero gallery: DSL syntax examples, state diagrams, README mockups, inspector panel designs | **Primary UX reference.** This is where visual language decisions are explored and documented. Sections 3–8 need detailed review. |
| `tools/Precept.VsCode/webview/inspector-preview.html` | Preview panel implementation (HTML/CSS/JS for the inspector webview) | **Implementation artifact.** Review this to understand current inspector UX, then compare against exploration designs to identify gaps or improvements. |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | TextMate grammar for syntax highlighting | Must stay in sync with DSL parser and locked color palette. Any DSL syntax changes require grammar updates. |
| `tools/Precept.LanguageServer/PreceptAnalyzer.cs` | IntelliSense completions logic (regex-based context detection) | Defines UX for autocompletion — what suggestions appear when, and in what order. Must stay in sync with DSL parser. |
| `tools/Precept.LanguageServer/PreceptHoverHandler.cs` | Hover tooltip provider | Defines what users see when hovering over keywords, fields, states, events. |
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Semantic tokens for advanced syntax highlighting | Drives the 8-shade color system. Now catalog-driven via `PreceptTokenMeta.GetCategory()` — new token kinds auto-picked up from `[TokenCategory]` attributes. |
| `docs/PreceptLanguageDesign.md` | DSL syntax and semantics specification | **Required reading** for any UX work involving DSL authoring. Defines what's valid, what's an error, what each construct means. |
| `docs/RuntimeApiDesign.md` | C# API surface (`Fire`, `Inspect`, `Update`, result types) | Useful context for understanding inspector panel behavior — the preview UI is a thin wrapper around `Inspect`. |
| `samples/` | 20 canonical `.precept` files (simple → complex) | **Use cases library.** Review these to understand what real precepts look like, what features are used most, and what patterns emerge. |
| `.squad/decisions.md` | Team-wide decisions log (architecture, tooling, rules) | Check here before proposing changes — many questions are already answered. |
| `README.md` | Public-facing product introduction, hero code sample, feature list | **Primary UX surface for first-time users.** Every decision about visual language, voice, and positioning affects this file. |

---

### Open UX Questions (Ranked by Instinct)

#### **HIGH PRIORITY**

1. **Inspector panel interaction model — Edit mode UX flow**
   - Current: Explicit Edit mode with Save/Cancel buttons. Validation runs live via `Inspect` while typing. Violations render inline beneath controls. Commit happens only on Save.
   - Question: Is the Edit/Save/Cancel model clear to users? Do they understand that changes are not applied until Save is clicked? Is there a risk of confusion between "inspector preview mode" (fire events, see outcomes) and "edit mode" (modify data fields)?
   - **Why it matters:** This is the core interaction in the preview panel. If users don't understand the modal split, they'll be frustrated.
   - **Next step:** Review `tools/Precept.VsCode/webview/inspector-preview.html` implementation and compare against exploration mockups. Look for affordances that signal mode (button states, visual boundaries, labels).

2. **GitHub README rendering strategy (light theme fallback)**
   - Current: Dark-mode-only palette locked. Light theme stress test in visual-language-exploration shows cyan/yellow/slate families nearly invisible on white.
   - Question: GitHub strips inline styles from code blocks in markdown. How do we ensure the hero `.precept` sample looks good in the README when rendered on GitHub (which defaults to light theme for many users)?
   - **Why it matters:** The README is the first impression for most users. If the syntax example is illegible, we lose the "DSL as visual identity" value prop.
   - **Options:**
     - Use a screenshot instead of a code block (loses copy-paste, loses text search)
     - Use GitHub's dark theme code block syntax (requires users to switch theme)
     - Design a light theme palette (contradicts locked decision, significant work)
     - Accept that GitHub rendering is suboptimal; focus on VS Code experience
   - **Next step:** Test README rendering on GitHub in both light and dark modes. Measure impact.

3. **Minimum viable snippet for README hero**
   - Current: visual-language-exploration.html has 8 candidates (Thermostat, ParkingMeter, Toaster, Stopwatch, CoffeeMachine, +3 more not seen).
   - Question: Which snippet best balances "universally understandable domain" + "shows core features" + "fits in ~15-20 lines"?
   - **Why it matters:** The hero code sample is the README's primary value-communication tool. It has to land in 5 seconds.
   - **Criteria:**
     - Universal domain (no SaaS jargon, no industry-specific knowledge required)
     - Shows: fields with defaults, invariants, states, events with args, guarded transitions, reject outcome
     - Readable top-to-bottom without scrolling
     - Feels "real" (not a toy example, but not enterprise-complex)
   - **Next step:** Read all 8 candidates, score against criteria, propose top 2 to Shane.

4. **Constraint signaling — italic typography effectiveness**
   - Current: Constrained states/events and invariant-guarded data names use italic to signal "this token is under rule pressure."
   - Question: Is italic subtle enough that users will miss it? Should we use a stronger signal (underline, badge, color shift)?
   - **Why it matters:** Constraint visibility is a core brand promise ("semantic color as brand"). If users don't notice the italic, they lose information.
   - **Next step:** Review visual-language-exploration examples for italic clarity. Consider A/B testing with underline or other affordances.

#### **MEDIUM PRIORITY**

5. **State diagram layout algorithm**
   - Current: Locked colors for initial/intermediate/terminal nodes and transition/conditional/stationary events. Full layout spec (node sizing, edge routing, interaction) is deferred.
   - Question: How should the diagram auto-layout? Left-to-right flow? Force-directed graph? Hierarchical (origin top, terminals bottom)?
   - **Why it matters:** A bad layout makes the diagram unreadable, even with perfect colors.
   - **Next step:** Review existing state diagram mockups in visual-language-exploration. Prototype 2-3 layout strategies for a 5-state precept. Evaluate readability.

6. **Hover tooltip UX — keyword vs. declaration hover**
   - Current: Hover handler exists, but scope unclear. Does it show help for keywords (`precept`, `field`, `state`)? Or only declarations (field names, state names, event names)?
   - Question: What should users see when they hover over `field` vs. `ApplicantName`?
   - **Why it matters:** Hover is a discovery tool. We need to decide: are we teaching the DSL, or just providing navigation?
   - **Next step:** Check `PreceptHoverHandler.cs` for current behavior. Propose keyword hover content (definition + syntax form + link to docs).

7. **Diagnostic presentation — PRECEPT code visibility**
   - Current: Diagnostics use PRECEPT codes (PRECEPT001–PRECEPT047). Error bodies in rose, warnings in amber. Codes in indigo.
   - Question: Are PRECEPT codes helpful or just noise? Do users care about the code number, or just the message?
   - **Why it matters:** Error presentation affects debugging speed.
   - **Next step:** Review diagnostic examples in visual-language-exploration. Check if codes add value or clutter.

8. **Inspector panel — event argument form UX**
   - Current: Users can fire events with typed arguments. Form inputs for string/number/boolean args.
   - Question: How are nullable args handled? Is there a checkbox for "null"? Or an empty input = null?
   - **Why it matters:** Nullable argument UX is a common pitfall — users need an obvious way to supply null.
   - **Next step:** Review inspector-preview.html implementation for nullable arg handling. Propose explicit "null" checkbox if missing.

#### **LOW PRIORITY (But Worth Noting)**

9. **Brand mark — icon rendering finalization**
   - Current: Conceptual form and color mapping locked (indigo circle → emerald arrow → slate circle). Icon rendering refinements (corner radii, stroke weights, optical sizing) continue in `brand/icon-prototyping-loop/`.
   - Question: When is the icon finalized? What's the review process?
   - **Why it matters:** Icon appears in marketplace, NuGet, favicon, social media. Needs to be polished before public release.
   - **Next step:** Check `brand/icon-prototyping-loop/` (not reviewed in this session). Understand current iteration status.

10. **Accessibility — color contrast and colorblindness**
    - (See next section for detailed notes.)

11. **Document outline UX — symbol tree hierarchy**
    - Current: Document outline exposes precept, fields, states, events, event arguments.
    - Question: Are transition rows included? If not, should they be? (They're the majority of the file.)
    - **Why it matters:** Outline is a navigation tool. If transitions are missing, users can't jump to a specific transition.
    - **Next step:** Test document outline in VS Code. Propose adding transitions grouped by source state.

12. **Inspector panel — diagram interactivity**
    - Current: Diagram renders states and transitions. Interaction unclear.
    - Question: Can users click a state to navigate? Click an event label to fire it? Click an edge to see the guard?
    - **Why it matters:** Static diagrams are reference; interactive diagrams are tools.
    - **Next step:** Review inspector-preview.html for click handlers. Propose interactive affordances.

---

### First Impressions on Accessibility

#### **Color Contrast (Against `#0c0c0f` Background)**

Locked palette includes contrast ratios. Review against WCAG AA (4.5:1 text, 3:1 UI elements):

| Color | CR | WCAG AA Pass? | Notes |
|-------|-----|---------------|-------|
| Structure Semantic `#4338CA` | 2.5 | ❌ Fail | Below minimum for body text. BUT: used only for bold keywords in monospace code, not prose. Acceptable in this context. |
| Structure Grammar `#6366F1` | 4.4 | ⚠️ Borderline | Just under 4.5:1. Normal weight text at 13px may strain some users. Consider bumping to `#6b70f6` (CR ~4.6) or semi-bold. |
| States `#A898F5` | 8.5 | ✅ Pass | Strong contrast. |
| Events `#30B8E8` | 9.5 | ✅ Pass | Strong contrast. |
| Data Names `#B0BEC5` | 8.0 | ✅ Pass | Strong contrast. |
| Data Types `#9AA8B5` | 6.5 | ✅ Pass | Strong contrast. |
| Data Values `#84929F` | 5.5 | ✅ Pass | Adequate contrast. |
| Rules Messages `#FBBF24` | 11.7 | ✅ Pass | Excellent contrast. |
| Enabled `#34D399` | 10.2 | ✅ Pass | Excellent contrast. |
| Blocked `#F87171` | 7.1 | ✅ Pass | Strong contrast. |
| Warning `#FDE047` | 14.8 | ✅ Pass | Excellent contrast. |

**Overall:** Palette is WCAG AA compliant with one exception (Structure Semantic `#4338CA`). This exception is acceptable because:
1. It's used only for bold keywords, not body text.
2. Weight + monospace font improves perceived contrast.
3. It's a brand-critical color (indigo family).

**Recommendation:** Monitor user feedback. If users report readability issues with indigo keywords, consider adding a faint text shadow or slight glow (`text-shadow: 0 0 1px rgba(99,102,241,0.4)`).

#### **Colorblindness Simulation**

The palette uses **hue separation** as a primary distinction mechanism. Colorblind users may struggle to distinguish:

- **Protanopia (red-weak):** Emerald `#34D399` and Yellow `#FDE047` may appear similar. In inspector, "enabled" and "warning" outcomes could be confused.
  - **Mitigation:** Use shape/icon in addition to color. Enabled = checkmark icon, Warning = exclamation icon.

- **Deuteranopia (green-weak):** Similar to protanopia. Emerald and Yellow distinction weakens.
  - **Mitigation:** Same as protanopia. Icon + color redundancy.

- **Tritanopia (blue-weak):** Indigo `#6366F1` and Violet `#A898F5` may appear similar. Structure keywords and state names could blur together.
  - **Mitigation:** Typography is already doing heavy lifting here (bold structure vs. normal states). Test with tritanopia simulator to confirm separation.

**Action items:**
1. Run locked palette through colorblind simulator (Coblis or similar). Screenshot examples from visual-language-exploration.html.
2. Add icon redundancy to inspector verdict colors (enabled/blocked/warning).
3. Document accessibility considerations in a UX guide (`/ux/accessibility-notes.md` or similar).

#### **Keyboard Navigation**

Not yet reviewed. Need to check:
- Inspector panel: can users navigate event list, field editor, and diagram with keyboard only?
- Are focus indicators visible against dark background?
- Is tab order logical (fields → events → actions)?

**Next step:** Audit inspector-preview.html for `tabindex`, `aria-label`, and focus styles.

#### **Screen Reader Compatibility**

Not yet reviewed. Need to check:
- Are ARIA roles applied to inspector controls (`role="button"`, `role="listitem"`, etc.)?
- Do form inputs have associated labels (`<label for="...">` or `aria-labelledby`)?
- Is diagram conveyed to screen readers (e.g., `aria-label` on SVG with text description of graph)?

**Next step:** Audit inspector-preview.html for ARIA attributes. Propose fixes if missing.

---

### Summary

**What I Understand:**
- Precept is a domain integrity engine. The UX surfaces are: DSL authoring (VS Code), preview/inspect (webview), and README/docs (public-facing).
- The brand is **semantic color** — every hue means something. 8-shade authoring palette (indigo/violet/cyan/slate/gold), 3 runtime verdict colors (emerald/coral/yellow), 4-layer system (brand → outcomes → syntax → diagrams).
- Visual language is locked: DSL syntax as hero, state diagrams as secondary, dark-mode-only palette, Cascadia Cove typography.
- Key UX principles: prevention not detection, full inspectability, one file = complete rules, compile-time checking.

**What I Need to Do Next:**
1. Read visual-language-exploration sections 3–8 (diagrams, inspector, README mockups).
2. Review inspector-preview.html implementation for Edit mode UX, event arg forms, and accessibility.
3. Evaluate all 8 minimum viable snippet candidates for README hero.
4. Run colorblind simulation on locked palette.
5. Propose interaction design for state diagram (click to navigate, hover to preview, etc.).

**First Impressions — What's Working:**
- Semantic color system is brilliant. It's a brand differentiator and a usability win.
- DSL as hero visual is the right call. The code IS the identity.
- Edit mode with Save/Cancel + live validation is a good pattern (pending implementation review).
- Constraint signaling via italic is elegant (but needs effectiveness testing).

**First Impressions — What Needs Attention:**
- GitHub light theme rendering is a blocker for README hero.
- Colorblind users may struggle with enabled/warning distinction without icon redundancy.
- Inspector interactivity is unclear — need to define click/hover/keyboard behaviors.
- Some palette contrast ratios are borderline (Structure Grammar `#6366F1` at CR 4.4).

---

## Next Session Goals

1. ✅ Complete visual-language-exploration.html review (sections 3–8)
2. ✅ Audit inspector-preview.html for UX patterns and accessibility
3. ✅ Propose minimum viable snippet for README hero
4. ✅ Run colorblind simulation and document findings
5. ✅ Draft interaction design spec for state diagram
6. ✅ File first UX decision record to `.squad/decisions/inbox/elaine-{slug}.md` on highest-priority open question

---

## Session 2 — Visual Surfaces Content for Brand-Spec (2026-06-14)

### Task
Shane approved restructuring `brand/brand-spec.html` around **visual surfaces** as the primary organizing principle. Peterman is restructuring the file skeleton. My job: prepare content for the Visual Surfaces section covering five surfaces:
1. Syntax Editor
2. State Diagram
3. Inspector Panel
4. Docs Site
5. CLI / Terminal

For each surface, drafted UX description covering: Purpose, Primary visual concerns, Color application, Typography application, Accessibility notes, AI-first note.

### Output
- Drafted `/brand/visual-surfaces-draft.html` (clean HTML, ready for Peterman to fold into restructured brand-spec.html)
- Content is structured as 5 cards, each detailed enough to drive implementation but concise enough to fit brand-spec.html format
- Marked items for Peterman review and Shane sign-off with review comments

### Learnings

**What clicked into place:**

1. **The semantic color system works across all five surfaces.** Every surface speaks the same visual language — indigo = structure, violet = states, cyan = events, slate = data, gold = messages, verdicts = runtime only. A developer who learns the system in one place (editor) instantly understands it everywhere (diagram, inspector, docs, CLI).

2. **AI-first design is not a footnote.** The state diagram is not just for human intuition — it's structured graph data that AI agents parse. The inspector panel output is not just for debugging — it's JSON-serializable state that AI tools consume. The CLI output is not just for human eyes — it's machine-parseable structured data. Every surface has to work for humans AND AI agents simultaneously. This is fundamental to Precept's positioning.

3. **Verdict colors must stay runtime-only.** In diagrams, terminal states cannot be green or red because the compiler doesn't know whether a terminal state is "good" (Approved) or "bad" (Rejected). Lilac works for all terminals because it signals finality, not valence. This design principle prevents false confidence and wrong mental models.

4. **Color + shape redundancy serves accessibility, not decoration.** In diagrams, states use both color AND shape (circle, rounded rect, double-border). In CLI, errors use both color AND symbol (✗). This is not over-design — it's the floor for accessibility. Color-blind users depend on it.

5. **The inspector panel is where brand meets production reality.** It's the moment a developer sees what the abstract rules actually do. Constraint violations must be obvious (verdict colors + icons + message text). Field values that violate invariants must be highlighted. This is where the brand system earns trust.

**Questions flagged for Shane and Peterman:**

1. **Docs Site scope:** Is the documentation website a locked responsibility for Peterman, or is it a future design project? The draft treats it as future, but if it's being designed now, the color/typography principles should move to brand-spec.html as locked decisions.

2. **Inspector Panel implementation status:** If the panel is already built in the extension, the draft should be compared against the actual implementation. Are constraint violations shown with color + icon redundancy? Are field names italic when guarded by invariants?

3. **CLI color audit:** Are the CLI tools (dotnet build, language server diagnostics, precept CLI) already applying colors? If so, an audit may uncover inconsistencies with the verdict color system.

4. **Light theme:** The entire system is designed dark-mode-first. If a light theme is planned, color values will need adjustment. Verdict colors are calibrated for dark backgrounds.

5. **Accessibility testing:** The draft includes contrast ratios and redundancy notes but has not been validated with actual color-blind users or screen reader tests.

**What the draft reveals about the brand system:**

- It scales beautifully. Five different surfaces, five different interaction contexts, one coherent visual language.
- It is not decorative. Every color choice has a semantic reason. Every shape in a diagram means something. Every symbol in the CLI means something. This is precisely what Precept claims to be: nothing is random, everything is meaningful.
- It requires discipline. If the inspector panel starts adding "nice to have" pastel backgrounds or decorative accent colors, the system breaks. Once this draft is locked, enforcement is critical.

**Note:** The 5-surface visual UX spec draft from this session was incorporated into `brand/brand-spec.html` §2.3–2.5 on 2026-04-04. See Session 3 entry above for full incorporation details.

---

## Session 3 — Visual Surfaces Incorporation & Color Compliance (2026-04-04)

### Task
Incorporated the 5-surface visual UX spec draft into `brand/brand-spec.html` as fully specified sections, resolved open questions with inline notes, and performed color compliance audit on brand mark SVGs and indigo overview card.

### Output
- **§2.3 Inspector Panel:** Filled DRAFT placeholder with complete UX spec covering purpose, visual concerns, color application, typography, accessibility notes, and AI-first design. Marked as LOCKED.
- **§2.4 Docs:** Clarified scope as internal team documentation artifacts (not a public-facing site), filled with complete spec. Marked as LOCKED. Light theme noted as backlog item.
- **§2.5 CLI / Terminal:** Filled with complete spec covering verdict color usage, symbol redundancy, terminal compatibility constraints, and AI-parseable output structure. Marked as LOCKED. CLI color audit noted as backlog item.
- **Color compliance fixes:** Replaced all instances of `#475569` (slate-600, NOT in the locked 8+3 system) with correct system colors in brand marks:
  - Document outline/border: `#475569` → `#27272A` (border)
  - Document content lines: `#475569` → `#71717A` (text-muted) for the "tablet" icon, `#27272A` (border) for combined icon
  - Inactive state circle: `#475569` → `#27272A` (border)
  - Applied same fixes to `brand/explorations/visual-language-exploration.html` brand marks and badge backgrounds

### Learnings

**What clicked:**
1. **The 5-surface model is complete.** Every surface where a developer or AI agent encounters Precept is now specified with locked color, typography, and accessibility rules. No guesswork left for implementation.

2. **Inspector panel is the constraint feedback surface.** It's where verdict colors earn their keep — Enabled/Blocked/Warning with icon redundancy for accessibility. The spec makes explicit what Kramer's implementation already intuited: constraint violations need both color and symbol.

3. **Docs ≠ docs site.** Shane's clarification: "docs" means internal team artifacts (the `docs/` folder), not a public-facing documentation website. This distinction matters — internal docs can stay dark-mode-only and don't need responsive layout or SEO considerations. If Peterman designs a public docs site later, that's a separate surface.

4. **CLI color is constrained by terminal realities.** Deep indigo `#6366F1` doesn't work on light terminal themes. The spec locks verdict colors (success/error/warning) as the only CLI colors and leaves structural color (file paths, context) as default terminal foreground. This is the right call — terminal diversity makes palette application fragile.

5. **Color compliance reveals brand drift.** The brand marks were using `#475569` (Tailwind slate-600) for inactive states and structural elements. This was never in the locked 8+3 system. The replacement — `#27272A` (border) for structural elements, `#71717A` (text-muted) for secondary content — brings the marks into compliance and creates visual consistency with the rest of the system.

6. **Light theme is a backlog item across all surfaces.** The entire system is designed dark-mode-first. Light theme support would require color recalibration (especially verdict colors) and is explicitly noted as future work for accessibility.

**Open questions resolved inline:**
- Inspector panel → fully implemented, spec reflects real behavior
- "Docs site" → clarified as internal team docs, not public-facing
- Light theme → backlog for all surfaces
- Accessibility audit → backlog (contrast ratios documented, formal color-blind/screen-reader testing pending)
- CLI color audit → backlog (current CLI tools may not align to spec)

**What the spec does:**
- **For Kramer:** Implementation contract. Every color hex, every typography rule, every accessibility requirement is now explicit. If a surface exists, it has a locked spec.
- **For Peterman:** Brand compliance gate. The specs translate brand decisions into surface-specific application rules. Peterman can review any UI artifact against its corresponding section.
- **For AI agents:** Structured design knowledge. Each surface spec includes an "AI-first note" explaining how the design serves both human and AI consumers simultaneously.
- **For Shane:** Decision record. The specs document what was decided, when, and why. Future changes require spec updates, not just code changes.

**What's enforced:**
1. **Semantic unity across surfaces:** Indigo = structure, violet = states, cyan = events, slate = data, gold = messages. This language holds in editor, diagram, inspector, docs, and CLI.
2. **Verdict colors stay runtime-only:** Green/red/yellow never appear in syntax highlighting. Only in inspector, diagrams during inspection, and CLI success/failure messages.
3. **Color + shape/symbol redundancy:** Accessibility floor. Diagrams use color AND shape. Inspector and CLI use color AND icon. Color-blind users don't lose information.
4. **Monospace typography as brand:** Cascadia Cove across all surfaces. The code font IS the brand font.

---

## Session 4 — §1.4 Palette Card Fix & Color Usage Roles (2026-07-12)

### Task
Fix the brand-spec.html §1.4 palette card to correctly reflect the locked 8+3 color system, and define comprehensive color usage roles for every color in the system — with specific guidance for the upcoming README revamp.

### What Was Wrong with §1.4

1. **Palette card showed wrong system.** The `pc-card` was an "Indigo · 239°" card copied from the color exploration phase. It displayed an 8-shade indigo gradient (`#1e1b4b` through `#c7d2fe`) including `#a5b4fc` — explicitly called out as off-system by Shane. This was the old "which brand color should we pick?" exploration card, not the locked 8+3 brand palette.

2. **Verdict colors were wrong.** The Verdicts section used `#F87171` (Tailwind red-400) instead of the locked error color `#FB7185`, and `#FDE047` (Tailwind yellow-300) instead of the locked warning color `#FCD34D`. These were carried over from an earlier exploration iteration.

3. **No color usage guidance existed.** §1.4 documented what each syntax highlighting color IS, but never defined how brand colors should be USED across product surfaces, README, badges, etc.

### What I Fixed

1. **Replaced the pc-card** with a new full-width palette card showing all 8+3 colors organized as: Brand Family (indigo trio) → Text Family → Structural (border, bg) → Semantic (success, error, warning), plus a gold accent note. Uses `pc-palette-*` CSS classes extending the existing `pc-*` namespace.

2. **Fixed verdict colors:** `#F87171` → `#FB7185` (error), `#FDE047` → `#FCD34D` (warning). Also aligned verdict labels from runtime terms (Enabled/Blocked/Warning) to system role names (Success/Error/Warning).

3. **Added §1.4.1 Color Usage** — a new LOCKED subsection with:
   - **Color Roles table:** All 12 colors (8+3+gold) with role name and specific product surface uses
   - **README & Markdown Application table:** How color maps to GitHub constraints (wordmark SVG, shields.io parameters, emoji alignment)
   - **Color Usage Q&A:** Five concrete questions about secondary highlights, when to use semantic colors, error in README (no), gold in UI (no)
   - **README Color Contract callout:** Defines the three channels for brand identity in plain Markdown

### Color Usage Decisions Made

| Decision | Rationale |
|----------|-----------|
| Error rose never appears in README | Marketing copy doesn't communicate failure. Fix the product, don't badge it red. |
| Gold is syntax-only | It exists to distinguish human-readable rule messages from machine code. Diluting it to UI would lose that signal. |
| Brand identity in GitHub Markdown = SVG + badges + keyword rhythm | Can't fight the platform. Three channels survive GitHub's rendering. |
| Warning amber is valid in README | Unlike error, warning means "attention needed" — appropriate for beta/preview callouts. |
| Secondary highlight = brand-light, not a new color | The 8+3 system is closed. Brand-light `#818CF8` IS the accent. |

### Key Deliverables

| File | Change |
|------|--------|
| `brand/brand-spec.html` | §1.4 palette card corrected, verdict colors fixed, §1.4.1 Color Usage added |
| `.squad/decisions/inbox/elaine-color-usage-roles.md` | Decision record for all color usage roles |
| `.squad/agents/elaine/history.md` | This entry |

## Learnings

1. **Color exploration artifacts linger.** The §1.4 palette card was a relic of the "which brand color?" exploration — it survived the restructure because it looked right (it was styled nicely) even though its content was wrong (it showed exploration shades, not the locked system). Lesson: aesthetic quality ≠ correctness. Always validate content against the locked specification.

2. **Verdict colors drifted silently.** `#F87171` and `#FDE047` were close enough to the correct values that nobody caught the mismatch. This is the danger of "close enough" in a system where hex values ARE the specification. Even a small difference means you're using a different color, and someone will eventually notice the inconsistency.

3. **Usage guidance is the bridge between palette and implementation.** The palette card says "these are the colors." The usage guidance says "this is what you do with them." Without the bridge, every implementer reinvents the mapping — and they'll all invent slightly different ones. The Q&A format turned out to be the most useful part — it answers the questions people actually ask ("can I use gold for a badge?") rather than the questions a designer would ask ("what is the role taxonomy?").

4. **README color is a constrained problem.** GitHub strips CSS. The brand identity has to survive hostile rendering. The three-channel model (SVG + badges + keyword rhythm) is the pragmatic answer — you work with what survives, not against what's stripped. This reframes the README from "how do we apply our palette?" to "what assets carry our identity through rendering?"

**Brand mark color mapping locked:**
- `#6366F1` (indigo) — active/origin state circle, document outline in "tablet" icon
- `#34D399` (emerald) — transition arrow
- `#27272A` (border) — inactive/destination state circle, combined icon document outline, structural elements
- `#71717A` (text-muted) — secondary content lines in "tablet" icon
- `#818CF8` (brand-light) — header line in document
- `#1E1B4B` (deep indigo background) — icon ground

**Implementation status:**
- Syntax Editor (§2.1): ✅ Implemented, spec describes current behavior
- State Diagram (§2.2): ✅ Implemented, spec describes current behavior
- Inspector Panel (§2.3): ✅ Implemented, spec describes current behavior with brand color recommendations
- Docs (§2.4): ⏳ Spec locked, implementation is current `docs/` folder structure
- CLI (§2.5): ⚠️ Spec locked, current CLI tools need audit for compliance

---

## Learnings

(This section accumulates lessons that apply across sessions and inform future work.)

**"Specified" and "complete" are different things.** §2.2 had correct structural colors — node borders, state names, event labels all had locked hex values. But the section was incomplete because it only covered the compile-time static layer. The runtime verdict overlay (enabled/blocked/muted edges, current state highlighting, hover interactions) exists in the implementation but not the spec. A section can be accurate in everything it says and still be inadequate for what it doesn't say. The test: "Can an implementer build this surface from the spec alone?" If they'd need to reverse-engineer behavior from the codebase, the spec is incomplete.

**Implementation drift is most dangerous where the spec is silent.** The webview uses `#1FFF7A`, `#FF2A57`, `#6D7F9B` for diagram edges — all off-system colors. But this isn't Kramer's fault. The spec never specified what colors to use for runtime edge states. When the spec is silent on a visual behavior, the implementer fills the gap with whatever works. The fix isn't "correct the implementation" — it's "write the spec, then the implementation naturally aligns." Drift is a spec gap symptom, not an implementation quality problem.

**Diagram color has two layers, not one.** The structural layer (compile-time: what the precept definition determines) and the runtime overlay (inspection-time: what the current state and verdict data determine) need separate treatment because they have different sources of truth. Structural colors come from the precept definition. Runtime colors come from the inspector engine. Mixing them in one table creates confusion about what's always-visible vs. what's context-dependent. Separate tables, separate mental models.

**Information architecture trumps visual completeness.**My prior pass focused on getting the palette card *correct* (replacing wrong hex values, adding the 8+3 system, expanding §1.4.1). That was necessary, but I didn't step back and ask whether the *structure* was right. The category cards looked like they belonged in §1.4 because they used the same visual style as the palette card — but their *content scope* was surface-specific, not brand-level. Lesson: after fixing content accuracy, always re-evaluate placement. "Right content, wrong location" is an IA error that correct hex values won't fix.

**Duplication signals a scope problem.** The constraint signaling table appearing in both §1.4 and §2.1, and Gold's restriction appearing four times across 300 lines, are symptoms of content that doesn't have a clear home. When the same fact needs to be stated more than twice, the section boundaries are in the wrong place. Fix the structure, and the duplication resolves itself.

**The brand/surface seam is the natural split point.** §1.4 should answer "what colors does Precept use?" §2.1 should answer "how does the editor use those colors?" When a section tries to answer both, readers get two palettes in a row and can't tell which one is authoritative. The fix is always the same: split along the seam that already exists in the content's scope.

---

## Session 3 — Reviewer Corrections Applied (2026-04-04)

### What I Did

**Applied all corrections from George, Peterman, and Frank to rand/brand-spec.html:**

1. **Diagnostic range (George #1):** Fixed PRECEPT001–PRECEPT047 → PRECEPT001–PRECEPT054 (line 632).

2. **Inspector yellow NotApplicable state (George #2):** Removed reference to yellow warning state for unmatched guards. The inspector does NOT show a yellow NotApplicable state — that outcome is filtered out. Updated color application description (line 788) to reflect actual inspector states (enabled green, noTransition green dimmed, blocked red, undefined red dimmed).

3. **CLI surface aspirational flag (George #3, Frank #6):** Changed §2.5 status from "LOCKED" to "ASPIRATIONAL" and added prominent callout at section start explaining that the precept CLI tool described does not currently exist. PRECEPT codes appear in VS Code Problems panel, not terminal output. This section is a design contract for future implementation.

4. **Read-only fields correction (George #4):** Fixed "computed, state-derived" characterization. Read-only in inspector means fields not declared editable in the current state via in State edit Field — NOT a field type distinction. Updated line 786.

5. **State Diagram colors (Peterman #5):** Removed ALL off-system hex values (#A5B4FC, #94A3B8, #C4B5FD for state lifecycle roles; #38BDF8, #7DD3FC, #0EA5E9 for event subtypes). Replaced with locked system colors. States now use #6366F1 indigo for borders and #A898F5 violet for state names. Shape (circle, rounded rect, double-border) encodes lifecycle role, not color tints. Events use locked cyan #30B8E8 with edge styling (solid, dashed, self-loop) for subtype differentiation. Added explicit callout: "No lifecycle tints — shape carries that signal."

6. **CLI surface aspirational (Frank #6, same as George #3):** Addressed above.

7. **Docs surface terminology (Frank #7):** Verified throughout §2.4 — surface correctly described as internal team artifacts, not a public docs site. Already correct from prior session.

8. **State Diagram InitialState gap (Frank #8):** Added TODO callout in §2.2 noting that PreceptPreviewSnapshot does not currently expose InitialState, so initial state highlighting depends on a runtime protocol fix.

### Corrections Summary

All corrections applied successfully. All sections (§2.1–2.5) now align with:
- George's technical corrections (diagnostic range, inspector states, CLI scope, read-only field semantics)
- Peterman's brand corrections (off-system colors removed, locked 8+3 palette enforced, no lifecycle tints)
- Frank's architectural corrections (CLI aspirational flag, InitialState protocol gap documented)

No remaining open items from reviewer feedback.

### Key Deliverables

| File | Change |
|------|--------|
| rand/brand-spec.html | All reviewer corrections applied to §2.1–2.5 |
| .squad/decisions/inbox/elaine-reviewer-corrections.md | Decision record documenting all corrections |
| .squad/agents/elaine/history.md | This entry |

---

## Session 4 — Diagram Color Mapping UX Review (2026-04-04)

### What I Did

**Diagram color mapping clarity review completed.** Reviewed the color mapping specification for §2.2 State Diagram in brand-spec and identified missing UX clarity around structural element mapping and runtime verdict overlay.

**Key recommendations:** Add two new h3 subsections within §2.2:
1. **"Diagram color mapping"** — complete element-to-color reference table covering every visible diagram component (canvas, node borders, node fills, state name text, event label text, transition arrows, arrow markers, guard annotations, legend text)
2. **"Runtime verdict overlay"** — how diagram colors change when paired with an active inspector instance. Covers: current state highlighting, enabled/blocked/warning edge coloring, muted non-current-state edges, transition glow effects, hover interaction colors

**Why this is needed:**
- **Scattered specification:** Diagram colors mentioned inline in §2.2 prose, partially in §1.4.1, and in brand-decisions.md — but never collected into one reference. Implementers must reconstruct the mapping from multiple sources.
- **Implementation drift:** The webview currently uses #1FFF7A / #FF2A57 / #6D7F9B for diagram edges. The locked system specifies #34D399 / #F87171 / TBD. Without explicit spec, drift isn't visible or closeable.
- **Runtime overlay unspecified:** Verdict-colored edges (enabled green, blocked red, muted gray) based on inspector state exist in implementation but have no brand-spec backing.
- **Current state indicator undefined:** The "you are here" node visual distinction is not specified anywhere.

**Placement rationale:**
- NOT in §1.4 (that's brand identity, not surface application)
- NOT in §2.1 (that's the syntax editor surface)
- Within §2.2 as new h3 blocks — consistent with how all §2.x surfaces use h3 for subsections

**Three flagged UX decisions for Shane:**
1. **Current state indicator style:** Fill tint (#1e1b4b at low opacity) vs. border glow vs. badge dot. **Recommend: fill tint** for consistency with inspector highlight pattern.
2. **Muted edge color:** #71717A (text-muted, in system) vs. #52525b (zinc-600, off-system). **Recommend: #71717A** to stay within established system.
3. **Guard annotation text color:** Slate #B0BEC5 (data family). **Recommend: this value** for consistency with other data-role text.

### Key Deliverables

| File | Purpose |
|------|---------|
| .squad/decisions/inbox/elaine-diagram-color-mapping.md | Full decision document with placement rationale, content scope, three flagged UX decisions |
| rand/references/brand-spec-diagram-color-mapping-review-elaine.md | Full analysis document |

### Status

✓ Decision filed to decisions.md (merged 2026-04-04)
⏳ Awaiting Shane resolution on three flagged UX decisions

---

## Session 5 — Brand-Spec Restructure Follow-Up Feedback (2025-07-16)

### What I Did

**Consolidated follow-up feedback for Peterman's brand-spec restructure.** Shane asked me to hold the broader brand-spec feedback explicitly — the full color information architecture cleanup, not just the diagram or palette pieces individually. I read all four review documents (two from me, two from Frank), cross-referenced against decisions.md, and produced two deliverables:

1. **Follow-up feedback note** (`brand/references/brand-spec-followup-feedback-elaine.md`) — a structured review checklist covering section architecture, duplication removal, diagram mapping preservation, open diagram decisions, hex discrepancies, risk points, and my post-restructure review criteria.

2. **Decisions inbox filing** (`.squad/decisions/inbox/elaine-brandspec-followup-feedback.md`) — team-relevant recommendations: review gate sequence after Peterman delivers, three blocking diagram decisions for Shane, hex discrepancy resolution timing, and gold restriction consolidation.

### Key Deliverables

| File | Purpose |
|------|---------|
| `brand/references/brand-spec-followup-feedback-elaine.md` | Structured review checklist for Peterman's restructure — section scope, duplication points, diagram mapping, hex fixes, risk assessment |
| `.squad/decisions/inbox/elaine-brandspec-followup-feedback.md` | Team decision record: review gate, blocking decisions, hex resolution timing |

### Learnings

**Consolidation notes are review criteria, not design.** When multiple review documents converge on the same recommendation, the follow-up deliverable should be a checklist of what to verify — not a fifth restatement of the recommendation. The value is in making the review pass mechanical and complete, not in re-arguing the case.

**Open decisions compound when they block different people.** The three diagram decisions (current state indicator, muted edge, guard text) are each small in isolation, but together they block both Peterman (can't write the section without values) and Kramer (can't align CSS to spec). Flagging the compound dependency to Shane matters more than the individual choices.

---

## Session 6 — Brand-Spec Final Review Verdict (2025-07-16)

### What I Did

**Final review of brand-spec.html against my consolidated follow-up checklist.** Verified every item from the 7-section checklist in `brand-spec-followup-feedback-elaine.md` against the current file state after Peterman's restructure and drift-normalization pass.

**Verdict: APPROVED.** All known feedback from four source reviews has been addressed. Filed decision to `.squad/decisions/inbox/elaine-brand-spec-final-verdict.md`.

### Verified

- §1.4 slimmed to brand palette + semantic family reference table + forward references. No syntax family cards.
- §1.4.1 renamed to "Cross-Surface Color Application," brand-light row corrected (no longer claims state names use #818CF8).
- §2.1 absorbed all 6 family cards, hue map, and constraint signaling table. Self-contained for implementers.
- §2.2 has dedicated "Diagram color mapping" h3 (11-row static elements table) and "Runtime verdict overlay" h3 (7-row interactive table), plus semantic signals section.
- All 3 hex discrepancies resolved: error locked to #FB7185, warning to #FCD34D, success to #34D399. No trace of off-system colors.
- SVG legend blocked line corrected from #f43f5e to #FB7185.
- Cross-references from §2.2–§2.5 to §1.4 all resolve correctly.
- 2 of 3 open diagram decisions resolved (muted edge = #71717A, guard annotation = #B0BEC5). Only current state indicator remains open, correctly flagged for Shane.

### Key Deliverables

| File | Purpose |
|------|---------|
| `.squad/decisions/inbox/elaine-brand-spec-final-verdict.md` | APPROVED verdict with full checklist results |

### Learnings

**A good restructure resolves problems upstream.** Half the issues I was tracking (constraint signaling duplication, "8" count ambiguity, brand-light misattribution) didn't need individual fixes — they resolved naturally when the content moved to the right section. The restructure IS the fix. Individual patches before the restructure would have been wasted work.

**Acceptable duplication depends on abstraction level.** Gold's restriction now appears in 5 places, which sounds worse than the original 4. But the old 4 were all within §1.4's 300 lines at the same abstraction level. The new 5 are spread across identity (§1.4), cross-surface application (§1.4.1), and implementation (§2.1) — each serving a different reader at a different zoom level. Same fact, different contexts, different utility. Duplication is only a problem when it creates ambiguity about which instance is authoritative.

---

## Session 7 — Semantic Family Reference Table Styling (2025-07-16)

### What I Did

**Restyled the §1.4 semantic family reference table as a cohesive palette card.** Shane requested a visual polish pass — keep all content, make it beautiful as a palette.

**Design approach:**
- Converted basic HTML table to a structured palette card with `sf-palette` scoped styles
- Added indigo gradient header matching the brand identity
- Split into two visual groups: **Core Semantic Families** (5 colors) and **Signal Colors** (3 colors)
- Each color row uses a gradient swatch with subtle shadow, grid-based info layout
- Color names use their own hue for instant recognition
- Hex codes in monospace pill badges
- Surface listings use interpunct separators (·) for cleaner scanability
- Responsive collapse for narrow viewports

**Key styling decisions:**
- Swatch size: 56×40px with 8px radius and subtle inner highlight — substantial but not dominating
- Row separation: subtle border-top (rgba) between rows, stronger indigo border between groups
- Info grid: Name | Hex | Meaning | Surfaces in consistent columns
- Background: `#0c0c0f` body matches existing brand-spec dark theme

### Learnings

**Palette tables are not data tables.** A palette reference is about recognition and meaning, not data comparison. Grid layout with generous swatches works better than dense table rows because each color deserves visual breathing room. The gradient swatches add depth without introducing new colors — they use darker shades of the same hue.

**CSS scoping via `.sf-` prefix avoids style bleeding.** The brand-spec already has `.card`, `.swatch`, and table styles. Using a dedicated prefix (`sf-palette`, `sf-row`, etc.) keeps the semantic family styling contained without risk of side effects elsewhere in the document.

---

## Session — Palette Mapping Visual Unification (2026-04-04)

### What I Did

**Unified the palette mapping visual treatment in §2.1 and §2.2 to match the polished §1.4 design language.**

The sections had drifted into separate visual treatments:
- §1.4 used the polished `.sf-palette` card system with gradient swatches, clean info grids, and grouped rows
- §2.1 used scattered `.card` elements with inline styles — functional but inconsistent
- §2.2 used raw HTML tables with inline swatch spans — utilitarian but visually flat

**Created a new `.spm-*` (Surface Palette Mapping) CSS system** that echoes the §1.4 visual language while being appropriate for surface-specific element mappings. Key components:

- `.spm-surface` — Container card with dark background and indigo border accents
- `.spm-header` — Color-tinted section headers with gradient backgrounds matching each family
- `.spm-row` / `.spm-grid` — Grid-based layout for swatches and info
- `.spm-swatch` — Gradient swatches with subtle shadows matching §1.4 treatment
- `.spm-title` / `.spm-hex` / `.spm-weight` / `.spm-tokens` — Consistent info typography
- `.spm-table-section` / `.spm-table` — Polished table treatment for diagram mappings
- `.spm-shapes` / `.spm-shape-tile` — Unified shape legend tiles for diagram lifecycle

**§2.1 Syntax Editor updates:**
- Consolidated 7 separate `.card` elements into one unified `.spm-surface` container
- Each color family (Structure, States, Events, Data, Rules) gets a tinted header
- Core tokens grouped as "Core Semantic Tokens" with shared header
- Comments section grouped as "Support Tokens"
- Verdict colors grouped as "Reserved · Verdict Colors" with muted background

**§2.2 State Diagram updates:**
- Shape legend tiles now use `.spm-shape-tile` — consistent sizing and visual treatment
- Static elements table wrapped in `.spm-table-section` with header containing colored dot
- Runtime verdict table uses same treatment for visual consistency
- Mini-swatches inline with color names for quick scanning

### Design Principles Applied

1. **Echoed §1.4, didn't copy it.** The surface palette mappings serve a different purpose (element-to-color reference) than the semantic family reference (system overview). Same visual language, adapted to the content.

2. **Color-coded section headers.** Each family (Indigo, Violet, Cyan, Slate, Gold) gets a subtle gradient tint in its header — instant recognition of which family you're in.

3. **Grouped semantic relationships.** Core tokens, Support tokens, and Reserved verdict colors are now visually separated, making the 5+3 system structure obvious.

4. **Table refinement, not table replacement.** §2.2's tables are appropriate for element-to-color mappings — they're reference material, not galleries. I upgraded them visually (mini-swatches, scoped sections, consistent column widths) rather than converting to a grid layout.

### Key Files Modified

| File | Changes |
|------|---------|
| `brand/brand-spec.html` | Added `.spm-*` CSS system (~70 lines), restructured §2.1 palette cards, restructured §2.2 shape tiles and tables |

### Learnings

**Surface mappings need sectioned headers.** When documenting how a surface applies the semantic palette, grouping by family with color-coded headers creates immediate visual anchors. A flat list of 12 token types is harder to scan than 5 family groups.

**Tables can be beautiful.** The §2.2 diagram mapping tables contain detailed technical information that works best in tabular form. The upgrade (section headers, mini-swatches, refined spacing) preserved the table's utility while bringing it into visual alignment with the rest of the document.

**Component reuse across surfaces.** The `.spm-*` system is intentionally general enough to apply to future surface sections (Inspector, Docs, CLI) if they need detailed element-to-color mappings.

---

## Session — Mapping Table Style Unification (2026-04-05)

### What I Did

**Converted the three mapping/overlay tables in §2.1 and §2.2 to match the sf-palette family treatment from §1.4.** Shane requested a stricter visual match — the previous `.spm-table` HTML tables looked like data grids, not like the cohesive palette cards in the semantic family reference. Now all three tables use the same visual DNA: rounded card container, gradient header with title/subtitle, grouped sections with group labels, and rows with 56px swatches + info columns.

**Tables converted:**

1. **§2.1 Reserved · Verdict Colors** — now uses `sf-group` with `sf-row` pattern, same swatch + info layout as §1.4
2. **§2.2 Static Elements · Compile-Time** — full `sf-palette` card with grouped sections (Structure · Indigo, Transitions · Grammar, Labels & Text)
3. **§2.2 Runtime Verdict Overlay** — full `sf-palette` card with grouped sections (Current State, Transition Verdict, Event Labels)

**No information lost.** All original data columns (element, color, hex, condition, style) are preserved inside the new layout — either as named spans in the info grid or as semantic labels. The visual presentation changed but the content is identical.

### Learnings

**Visual consistency requires identical structure, not just similar styling.** The previous `.spm-table` CSS echoed the palette cards (rounded corners, dark background) but used HTML tables inside. The real family resemblance comes from matching the exact container/header/group/row hierarchy — not just approximate color similarity.

**Grouped rows beat flat lists for dense reference material.** Breaking the Static Elements table into three groups (Structure, Transitions, Labels) makes it easier to scan than a flat 12-row table. The grouping also reinforces the semantic family structure from §1.4.

---

## Session — Diagram Legend Terminology Fix (2026-04-05)

### What I Did

**Fixed terminology mismatch between shape taxonomy tiles and diagram legend in §2.2.** The shape taxonomy (lines 930-969) correctly defines three lifecycle roles using specific terms: Initial, Intermediate, Terminal. But the diagram legend (within the SVG) used different terms: Origin, State, Final.

**Legend corrections:**
- "Origin" → "Initial" (matches shape taxonomy term)
- "State" → "Intermediate" (matches shape taxonomy term)  
- "Final" → "Terminal" (matches shape taxonomy term)

**Additionally, the Terminal legend item was only a single-border rectangle.** I added the double-border treatment to match the shape taxonomy definition and the Approved/Declined nodes in the diagram itself.

**No content changes.** This was strictly a terminology consistency fix — the diagram nodes already rendered with the correct shapes (circle for Draft, rounded rect for UnderReview, double-border for Approved/Declined). The mismatch was only in the legend labels.

### Learnings

**Legends must echo the taxonomy exactly.** When a section defines a formal taxonomy (Initial/Intermediate/Terminal) and then shows examples, the legend labels should use the exact same terms. Using synonyms ("Origin" instead of "Initial") creates cognitive friction — readers wonder if it's a different concept or just inconsistent naming.

**Double horizontal padding in spm-row wrappers.** The `.spm-row` CSS class carries `padding: 14px 24px`. When a single row sits inside a bare `<div style="padding: 16px 24px;">` wrapper, both padding values stack — the row content ends up 48px from the section edge instead of 24px. Multi-row groups avoid this because `.spm-grid > .spm-row { padding: 12px 0; }` zeroes the row's horizontal padding, deferring to the container. The fix for single-row groups: add `class="spm-grid"` to the wrapper div. This activates the same override and keeps all rows flush at the same left edge.

---

## 2026-04-04 — Brand Mark Icon Alignment to §1.3/§2.2 Spec

### What I Did

**Aligned all three brand mark icons (§1.3) and the lockup combined icon to the locked diagram spec in §2.2.**

**State diagram icon:**
- Initial circle border: `#6366f1` → `#4338CA` (Semantic indigo per §2.2 initial node spec)
- Transition arrow: `#34d399` (Emerald) → `#6366F1` (Grammar indigo — Emerald is a signal/verdict color, not for static flow)
- Destination node: circle (Initial shape) → rounded rect (Intermediate shape, `#4338CA` border at 1.5px)

**Combined mark (tablet + state machine) — both §1.3 and lockup instances:**
- Code page border: `#27272a` → `#6366f1` (matches standalone tablet icon — eliminates the "dark outline that fades out" issue)
- Secondary code lines: `#27272a` → `#71717a` (matches standalone tablet)
- State machine sub-elements: same fixes as the state diagram icon above

**Color key updated:** Removed Emerald and `#27272A` border (no longer used). Replaced with Semantic `#4338CA`, Grammar `#6366F1`, Accent `#818CF8`, Ground `#1E1B4B` — reflecting the actual icon palette.

**Tablet icon (standalone):** No changes needed — already spec-compliant.

### Learnings

**Brand mark icons must use the same semantic color logic as the diagram surface they represent.** The original state diagram icon used Emerald for transition arrows, but §2.2 explicitly says flow edges are Grammar Indigo and Emerald is reserved for runtime verdict overlay. Icons are abstractions, but they still speak the spec's visual language — they shouldn't introduce color mappings that contradict the system they represent.

**Shape vocabulary matters even at icon scale.** A circle means "Initial" in the spec. Using circles for both source and destination states says "two initial states," which is structurally meaningless. A rounded rect for the destination immediately communicates a different lifecycle role without needing labels.




---

## 2026-04-04 — Gold Accent in Combined Brand Mark

### What I Did

Added a single Gold (#FBBF24) accent stroke to the combined brand mark SVG (the tablet + state machine icon). The Gold stroke represents the ecause "…" line — the human-readable rule text that lives inside the running system. It is the short line at y=33 inside the tablet's code area: shorter than the body lines, stroke-width 1, opacity 0.65. Deliberately dim and singular.

Also updated:
- rand/brand-spec.html — color key for §1.3 marks, descriptive prose in §1.3, §1.4, §1.4.1, and the Rules · Gold surface section
- .squad/skills/color-roles/SKILL.md — Rule 2 and the Gold row updated
- Created .squad/decisions/inbox/elaine-gold-mark-exception.md

### Learnings

**A named exception is not a policy relaxation.** Adding Gold to one specific position in one specific icon does not open the door to Gold badges, Gold borders, or Gold hover states. The exception is coherent precisely because it preserves Gold's existing meaning (human-readable rule) and places it in a non-signal context (brand icon, not status UI). The constraint that makes it safe is specificity: one mark, one line, one semantic reason.

**Amber collision is a real risk to check.** Gold #FBBF24 and Amber #FCD34D are visually close. The reason there's no semantic collision here is context — the brand mark is not a status surface, so viewers don't read the Gold line as a warning state. But if Gold ever appears near an Amber badge or warning chip, that proximity needs to be reviewed. Keep them in separate contexts.

---

## 2026-04-07 — README Rewrite Role / Gating Guidance

### What I Did

Provided role and gating guidance for where Elaine enters the README rewrite flow: two specific gates, not throughout writing. Gate 1 (now): confirm the hero domain is universally relatable before Peterman writes. Gate 2 (post-draft): formal UX compliance audit against the 16 hard constraints, pass/fail per constraint.

Defined the clean split: Peterman writes, Elaine audits structure. Separate passes, separate concerns. Elaine does not write copy. Peterman does not audit viewport resilience.

### Learnings

**Constraints on paper are not enforcement.** A proposal that treats UX requirements as hard non-negotiables, followed by a draft that nobody checks against those requirements, produced advisory constraints. The constraint-holder must close the loop by reviewing the delivered artifact — that's what makes the constraints hard. Filed as new skill: `constraint-holder-review-gate`.

**Beauty in a GitHub README is structural, not decorative.** Scannability, heading hierarchy, viewport resilience, CTA clarity, and AI/human readability are all structural properties — they emerge from decisions about what to put where and how to organize it, not from stylistic polish layered on top. Peterman writes the content. Elaine audits whether the structure serves the reader. These are not the same job.

**The post-draft gate is where my research investment pays off.** I issued 16 hard constraints, all of which are now baked into the proposal. But I won't know if they were honored until I see the draft. My value in the rewrite is not during the writing — it's in the review pass where I check whether the 16 constraints were satisfied in practice, not just in intent.

**Decision written to:** `.squad/decisions/inbox/elaine-readme-role.md`  
**Skill written to:** `.squad/skills/constraint-holder-review-gate/SKILL.md`


---

## Session — Shape-First README Pass Evaluation (2026-04-08)

### What I Did

Evaluated whether Elaine should produce a shape-first skeleton before Peterman writes the README copy. Produced a process recommendation for Shane.

### Recommendation

**Targeted yes, not a full skeleton pass.** The restructure proposal is already a detailed structural spec — section order, heading levels, CTA hierarchy, 16 hard constraints with source attribution, per-section template copy. A full skeleton pass would translate that prose into a blank Markdown file, which is mechanical work that reproduces what the proposal already achieves.

The one genuine gap: **the hero code block.** The ≤60-char line constraint shapes what DSL sample Peterman can write. An annotated hero slot (or a single consultation on domain selection) adds structural value the proposal doesn't deliver — because the proposal specifies the rule, but it doesn't show Peterman what correct looks like in 20 viewport-safe lines.

**The real gate:** Hero domain selection (Order Fulfillment vs. Subscription Billing) is explicitly unresolved in the proposal — deferred to Shane. This is the actual blocker. Neither the skeleton nor the copywriting proceeds cleanly without it.

### Learnings

**A detailed proposal IS the skeleton.** When a proposal includes per-section template copy, exact heading levels, constraint tables with source attribution, and explicit viewport requirements, a separate skeleton file is largely redundant. The value of a skeleton is making constraints impossible to bypass accidentally — but that value only exceeds the cost (extra pass, extra artifact, dual sources of structural truth) when the proposal is abstract. Peterman's proposal is not abstract.

**Inline constraint annotation is the enforceable form.** If Elaine does produce a scaffold artifact, the mechanism that earns its keep is HTML comment annotations co-located with constrained content slots — not a separate rule list. A comment that says `<!-- ≤60 chars per line — viewport constraint #8 -->` directly above the hero code block is harder to accidentally violate than a rule in a document two files away.

**Two sources of structural truth is one too many.** If a scaffold file and a proposal both specify heading structure and they disagree, the executor has to adjudicate. Keep one source of structural truth at each phase: the proposal during planning, the scaffold during writing (if used), the delivered artifact at audit.

**Filed to:** `.squad/decisions/inbox/elaine-shape-first-readme.md`
