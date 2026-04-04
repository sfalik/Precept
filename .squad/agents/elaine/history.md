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

---

## Learnings

(This section accumulates lessons that apply across sessions and inform future work.)
