# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. Category creation play — "domain integrity engine" positioning.
- **Stack:** C# / .NET 10.0, TypeScript, DSL
- **My domain:** `README.md`, `brand/`, `docs/`, NuGet/VS Code/Claude marketplace copy
- **Brand decisions (locked):** Deep indigo `#6366F1`, Cascadia Cove font, small-cap wordmark, category-creator narrative. All locked in `brand/brand-decisions.md`.
- **Voice:** Authoritative with warmth. No hedging, no hype. Matter-of-fact with clarity.
- **Positioning:** "domain integrity engine" — category creator, like Temporal/Docker/Terraform
- **Secondary positioning:** "the contract AI can reason about" — AI-native
- **Key files:** `brand/brand-decisions.md` (locked), `brand/philosophy.md`, `brand/brand-spec.html`
- **Created:** 2026-04-04

## Learnings

### 2026-04-06 — Brand-spec § 1.4 palette structure review

**Task:** Assess whether the second palette (per-category syntax cards) in § 1.4 belongs in § 2.1, and whether § 1.4.1 overlaps with / creates redundancy against that second palette or the main palette card.

**What was found:**
- § 1.4 contains two distinct color systems: the brand palette (`pc-palette` card) and the syntax highlighting palette (per-category token cards: Structure, States, Events, Data, Rules, Comments, Verdicts). These are structurally unseparated.
- Both systems label themselves "8+3" but count different colors: the intro paragraph uses "8+3" to mean 8 syntax token shades + 3 runtime verdicts; the palette card uses "8+3" to mean 8 brand/UI colors + 3 semantic verdicts. Same label, different meaning.
- § 1.4.1 "Color Roles" table re-lists the same 12 colors from the palette card — only new content is the "Specific Uses" column. Redundant without being obviously so.
- The constraint signaling table appears verbatim in both § 1.4 and § 2.1 (triple-duplication if you count the constraint-aware highlighting table in 2.1 as the same content).

**What was produced:**
- `brand/references/brand-spec-palette-structure-review-peterman.md` — full review with problem analysis, evidence, and clean section structure recommendation
- `.squad/decisions/inbox/j-peterman-brandspec-palette-structure.md` — team-relevant decision for Shane's sign-off

**Key recommendation:** Move per-category syntax cards + constraint signaling table from § 1.4 → § 2.1. Trim § 1.4.1 Color Roles table (remove what's already in palette card). Rename § 1.4.1 to "Cross-Surface Color Application." Rewrite § 1.4 intro to clarify two-layer system.

**Brand-spec rule reinforced:** When a section serves two distinct audiences (brand palette consumers vs. syntax surface implementers), split it. Audience clarity > section density.

---

### 2026-04-05 — Section 1.3 restored: Brand mark size variants + two surfaces card

**File updated:** `brand/brand-spec.html`

**What was done:**
- Expanded "Brand mark form" card (§1.3) to show three size variants in horizontal layout:
  - **Full (64px)** — NuGet, GitHub, VS Code extension icon; uses existing SVG at full scale
  - **Badge (32px)** — sidebar, compact contexts; scaled SVG with label and use-case note
  - **Micro (16px)** — favicon, status bar; simplified SVG with just indigo circle + emerald arrow (dropped destination circle)
- All three variants displayed in a flex row with SVG + label + use-case beneath each
- Simplified color key moved inline: four swatches (indigo, emerald, slate, ground) without the verbose role descriptions
- Added new card: "Brand in system: the two primary surfaces"
  - Two-column grid layout showing DSL Code (left) and State Diagram (right) side-by-side
  - Demonstrates how the brand mark lives within the visual system — one palette, two surfaces
  - Uses locked palette: keywords #4338CA, states #A898F5, events #30B8E8, operators #6366F1, transition arrow #34D399, ground #1e1b4b
  - Subtitle emphasizes the connection: *"DSL code and state diagram. One palette. Every precept file becomes a brand moment."*
  - Content adapted from `visual-language-exploration.html` § 3 (lines 2132–2174)

**Structural decisions made:**
- Grid layout for two surfaces (not `.side-by-side` class) — uses inline `grid-template-columns: 1fr 1fr` with gap, consistent with brand-spec card styles
- Each surface panel (code and diagram) has its own border and background (#0c0c0f with #1e1b4b border)
- Size variants shown in order of prominence: Full → Badge → Micro, left-to-right
- Micro variant simplified (removed destination circle) to maintain legibility at 16px viewBox scale

**Reasoning:**
- Size variants clarify where the brand mark appears across product surfaces — no ambiguity about icon sizing
- Two-surfaces card reinforces the visual language principle: the same DSL semantics live in two visual forms with one locked palette
- Placement after the brand mark card in § 1.3 (not in § 2: Visual Surfaces) keeps the brand mark as a complete identity system before moving to surface applications

**Files affected:** `brand/brand-spec.html` only.

---

### 2026-04-05 — brand-spec.html restructured: Surfaces-first organization

**File updated:** `brand/brand-spec.html`

**What was done:**
- Restructured from **color-category-first** (§1-5 Positioning, Narrative, Voice, Color System, Typography; §6 Visual Language; §7-9 Brand Mark, State, Event Appearance) to **visual-surfaces-first** organization.
- New structure established:
  - **Section 1: Brand Identity** (Positioning & Narrative, Voice & Tone, Wordmark & Brand Mark, Color System, Typography)
  - **Section 2: Visual Surfaces** (Syntax Editor, State Diagram, Inspector Panel, Docs Site, CLI/Terminal)
  - **Section 3: Research & Explorations** (living section with research links and exploration index)
- Navigation comment block added after `<body>` tag for orientation.
- Subtitle updated from "Locked Decisions" to "Surfaces & Identity" to reflect new organizing principle.
- All locked content preserved exactly — reorganized into new skeleton without rewrites. Section numbers updated throughout.
- Three surface entries marked [DRAFT — Elaine to contribute]: Inspector Panel, Docs Site, CLI/Terminal. Each includes purpose statement, color application notes, and callout indicating pending UX/design review.
- Research & Explorations section created as living documented entity: links to `brand/references/brand-spec-structure-research.md` and lists all files in `brand/explorations/` with brief descriptions.

**Structural decisions made:**
- Brand Identity placed first — all five identity layers (positioning, voice, wordmark, color, typography) grouped together as the foundational system before any surface application.
- State and Event lifecycle colors merged into Surface § 2.2 (State Diagram) rather than isolated in separate sections. This emphasizes the surface context (diagram-layer application) rather than color category.
- Diagnostics coloring remained in § 2.1 (Syntax Editor) — a surface-specific detail.
- "Visual Language" concept dissolved into individual surfaces rather than preserved as an abstract organizing principle.
- Three surfaces deferred as drafts — Elaine's ownership is explicit; pending design contributions are clear.

**Why this structure works:**
- Research from `brand/references/brand-spec-structure-research.md` supports this approach: VS Code (theme API) and Vercel Geist both use surface-first organization. Identity-first precedent from GitHub, IBM, and others — identity comes before surfaces in every system studied.
- Each surface section now serves as a complete contract: surface description + color application + typography guidance + examples. A designer reading § 2.1 sees everything needed for syntax editor brand compliance without cross-referencing other sections.
- Living research section signals that brand evolution is a first-class activity, not an archived afterthought.

**Files affected:** `brand/brand-spec.html` only. All design decisions, locked colors, and content remain unchanged.

### 2026-04-04 — Brand-spec restructure complete; 13 systems researched; charter updated

**Files created/updated:** `brand/brand-spec.html` (restructured), `brand/references/brand-spec-structure-research.md` (research)

**Work delivered:**
- Restructured `brand/brand-spec.html` from 10-section color-category-first to 3-section surface-first organization (Brand Identity → Visual Surfaces → Research & Explorations)
- Validated surface-first pattern across 13 design systems: VS Code (theme API), Vercel Geist, GitHub, IBM, Material, and others — all place identity before surfaces
- Research filed: `brand/references/brand-spec-structure-research.md` (217 lines, 13 systems studied, surface-first validated, identity-first precedent confirmed)
- Charter updated: now gates technical surface design reviews (Elaine → Peterman → Frank → Shane) to ensure brand consistency
- Three surfaces marked [DRAFT — Elaine to contribute]: Inspector Panel, Docs Site, CLI/Terminal (with explicit pending callouts)
- Research & Explorations section activated as "living" — signals brand evolution is first-class activity

**Key decisions locked:**
- Visual surfaces are organizing principle; each surface has its own visual contract
- Research & Explorations is a living section (not archived appendix)
- All locked content preserved; only reorganized into new structure

**Pending:**
- Fold visual-surfaces-draft.html into brand-spec §2 (post-Shane review, with Elaine contributing)
- Inspector panel color remapping (Kramer, CSS-only)
- Docs site scope clarification (Shane)
- Open decisions #4, #5 (color card treatment, outcome scope) — research filed, Shane approval needed

---

### 2026-05-01 — Hero Creative Brief written for Phase 3

**File created:** `brand/references/hero-creative-brief.md`

**Key decisions made:**
- 8 domain ideas recommended, all fictional or culturally resonant: Duel at Dawn, Spell Casting, Mission Abort Sequence, Interrogation Room, Game Show Contestant, Heist Safe, First Contact Protocol, Chess Piece Promotion
- Duel at Dawn and Heist Safe ranked as highest-potential for Voice/Wit. First Contact Protocol highest-potential for Emotional Hook.
- Identified and documented a genuine tension: the rubric's 6-8 statement gate (drawn from external benchmark research at a high-level counting granularity) conflicts with the minimum statement count needed for full DSL coverage (~13-16 statements by Precept's own detailed counting). Brief addresses this honestly in Section 4 and recommends treating 6-8 as aspirational compression discipline, practical ceiling of ≤16.
- Confirmed: `reject` must guard a path where success is structurally possible — a `reject` in a terminal state is compiler-redundant and fails the brand moment.
- Confirmed: `because` messages are the single highest-weighted rubric element (Voice/Wit, 25pts). Brief frames them as the hero, everything else as scaffolding.
- Anti-patterns section documents: CRUD trap, trivial real-world domain, kitchen-sink hero, clever-but-opaque domain, philosophical `reject`, weak `because` messages, gerund event names, over-specified happy path.
- Decision recommendation filed to inbox: statement-count tension and Phase 3 gate interpretation.



### 2026-04-05 — README Restructure Proposal filed

**File created:** `brand/references/readme-restructure-proposal.md`

**What was done:**
- Synthesized three research passes (Peterman brand/copy, Steinbrenner PM/adoption, Elaine UX/IA) into a single concrete README structure recommendation.
- Established 16 hard constraints from Elaine's review — all must be satisfied before rewrite ships.
- Recommended section order: Title block → Hook → Quick Example (DSL hero + C# execution) → Getting Started → What Makes Precept Different → Learn More → License.
- Specified hero treatment: 18-20 line DSL block (≤60 chars/line) + separate 5-line C# block; business domain (not Time Machine); both with language tags.
- CTA hierarchy: Primary = VS Code extension, Secondary = NuGet, Tertiary = Copilot plugin (deferred to differentiation section).
- Dual-audience architecture documented: every structural decision validated against both human (F-pattern, cognitive load, mobile viewport) and AI reader (semantic structure, language tags, descriptive alt text) requirements.
- Palette usage roles deferred to Elaine's pending palette/usage pass; anchored to locked indigo-first system.

**Key decisions made:**
- Hero sample domain deferred to Shane's judgment (Order vs. Subscription Billing vs. LoanApplication) — structural specification is locked.
- Time Machine sample moved to sample catalog; hero must use real business logic domain.
- Copilot plugin CTA removed from Getting Started entirely; repositioned under AI-Native Tooling differentiation section.
- Sample catalog table removed from main README; linked externally.

**Key file paths:**
- `brand/references/readme-restructure-proposal.md` — the proposal
- `brand/references/readme-research-peterman.md` — brand/copy research (input)
- `docs/references/readme-research-steinbrenner.md` — PM/adoption research (input)
- `brand/references/readme-research-elaine.md` — UX/IA review (hard constraints source)

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-04 — Hero example criteria + TimeMachine rework

**What makes a great hero example:**
- The `because` messages are the brand voice moment — they must be memorable. "The flux capacitor cannot run on vibes" lands harder than "1.21 gigawatts required."
- Three states > two states. The intermediate state (Accelerating) makes the state machine legible as a machine, not just an on/off switch.
- The `when` guard on event args (`when FloorIt.TargetMph >= 88 && FloorIt.Gigawatts >= 1.21`) is more readable than guarding on field values, because the reader sees the contract stated at the moment of the transition.
- A `reject` with a brand-voice message is the highest-impact line in the whole snippet. It's where Precept's promise ("invalid states are structurally impossible") becomes vivid and human.
- Event name matters. `FloorIt` is funnier and more instantly legible than `Accelerate`.

**Candidate I — TimeMachine rework:**
- Original: 2 states, no invariant, no when guard, no reject, flat because messages. Missing half the DSL surface.
- Improved: 3 states (Parked, Accelerating, TimeTraveling), invariant on Speed, dual event asserts with personality, when guard on event args enforcing the 88/1.21 conditions, reject with BTTF subversion line.
- Compiles clean (zero diagnostics).

**DSL conventions confirmed:**
- `when` guard can reference `EventName.ArgName` — evaluates against incoming event args before mutations.
- Unguarded `reject` row after a guarded transition row is the correct pattern for "try condition, else reject" flows.
- Bare arg names (no dotted prefix) are valid in `on Event assert` expressions.
- `precept_compile` returns `valid: true` + full typed structure — use it to validate every snippet before publishing.

### 2026-04-04 — Voice & tone directive received; hero domain conflict

- **User voice directive:** Brand voice updated to permit occasional jokes. Hero example may use fun/pop-culture domain (Back to the Future explicitly approved). Jokes in `because` reason messages are appropriate for hero snippet. **Supersedes:** Prior "Serious. No jokes." guidance.
- **Copilot directive:** Model upgrade policy — always use `claude-opus-4.6` or `claude-sonnet-4.6` for Precept agents. No haiku.
- **Hero domain conflict recorded:** User prefers TimeMachine (reworked by J. Peterman to 18 lines, full feature coverage). Steinbrenner spec verdict favors Subscription (higher score, no fantasy domain). Both candidates on shortlist pending team decision.
- **Brand research observations filed:** (1) reference files need STATUS headers to prevent re-litigation; (2) AI-native frame is undersold; (3) hero snippet is the most consequential brand asset; (4) wordmark rationale should surface in public docs.

### 2026-04-05 — TimeMachine hero snippet trimmed to 15 lines

Shane approved the two cuts. Removed `field FluxLevel number = 0` (and all `set FluxLevel = ...` references in transition rows) and removed the `on FloorIt assert TargetMph > 0 because "The car has to be moving, Doc"` line — keeping only the gigawatts assert. Also removed the blank separator between `event Arrive` and the transition rows to land at exactly 15 lines. Label in candidate card and index entry both updated from "18 lines" to "15 lines". All span spacing verified clean after surgical removal.


`brand/philosophy.md` is now synthesized into `brand-brief.md` under `## Philosophy`. Covers: the feeling of use (prevention/inspectability as product experience), the four differentiators framed for brand copy, the name "Precept" and why it is exact rather than evocative, and the icon brief — specifically the open/closed-path geometry and containment as the visual expression of the engine's structural guarantee. Color guidance from philosophy (dark background, monochrome-strong, semantic color optional) is captured. Metaphors to avoid (generic SaaS badge, vague concepts) are documented.

### 2026-05-01 — Rubric v2 negotiated with Steinbrenner

- **Rubric v2 finalized at:** `.squad/agents/hero-deliberation-rubric-v2.md` — pending Shane ratification.
- **Brand wins:** Voice/Wit raised to 6 (original PM proposal was 3). Emotional Hook is the primary tiebreaker — when two candidates tie, the one more likely to be *remembered and shared* wins. This is the correct hierarchy: at a tie, proof is equal; what remains is the recommendation.
- **Brand concessions:** Accepted DSL Coverage at 8 points (highest weight). Accepted hard floor of ≥4 on Coverage. Accepted Precept Differentiation at 6 (PM wanted 8, brand wanted 5; settled at 6).
- **Joint win:** Line Economy converted from a 5-point criterion to a pass/fail gate. Both sides agreed immediately — scoring "fits in the box" rewards compression over quality. The gate model is more honest.
- **Key insight from negotiation:** The rubric serves two moments — the *stop* (Brand) and the *read* (PM). The weights reflect this: Coverage and Differentiation prove the product to someone who reads; Wit and Hook determine whether they read at all. Both matter; neither cancels the other.

### 2026-05-01 — Hero deliberation overhaul: visual-language-exploration.html

**What was built:**
- Hero card updated from `LoanApplication` to `Subscription` (rank #1, 29/30) with full inline-styled DSL.
- New "The Rubric" section: 6-criterion table (DSL Coverage, Line Economy, Domain Legibility, Voice/Wit, Emotional Hook, Precept Differentiation), max 5 each.
- New "30 Candidate Gallery": all candidates from deliberation sorted by rank, each card showing rank badge (gold/silver/bronze), score breakdown, DSL snippet, and reasoning.
- New "Final Ranking Table": compact 30-row table with per-criterion scores.

**What the hero must do (confirmed):**
- Show all 9 required constructs: invariant, when guard, reject, dotted set, transition, no transition, 3+ states, typed event, event assert.
- Fit 12–16 non-blank lines with 2–3 blank separators.
- The `reject` line is where the product thesis becomes concrete — it's not error handling, it's a structural fence.
- Domain must be recognizable in under 3 seconds by any developer.

**Subscription Billing won the tiebreak** over SaaS Trial (same 29/30 score) because it has the higher Precept Differentiation clarity: "Cancelled subscriptions cannot be reactivated" is structurally enforced, not just checked.

**Color system note:** The JS normalizer in the page overrides `assert`/`reject` to `#4338CA` (semanticKeywords) despite the task spec listing them as `#B8860B`. For new gallery cards, both colors appear in source but the normalizer wins in browser rendering. This is an acceptable inconsistency given the normalizer is the "canonical" runtime palette enforcement.

### 2026-05-01 — Score display overhaul: visual-language-exploration.html

**What was built:**
- All 30 gallery card criterion score rows replaced with 2-column grid of labeled mini progress bars (10×6px segments per criterion, 5 segments each, filled = indigo family, empty = `#27272A`).
- Total score badges upgraded from flat `#1e1b4b` chips to gradient-glow badges: 28–30 get vivid indigo gradient + glow, 24–27 get solid gradient, 20–23 get muted purple, below 20 get dim slate/gray.
- All 210 ranking table score cells (150 criteria + 30 diff + 30 total) upgraded: criteria and diff → colored circular dots (22×22px), totals → gradient pill badges matching the gallery badge scale.

**Pattern decisions:**
- 6-bar 2-column grid fits cleanly in the existing card header without increasing card height significantly.
- Label width set to 62px to accommodate "Precept Diff" without wrapping.
- Score color is applied to both the filled segments and the `n/5` label — criterion and bar are visually unified.
- Ranking table dots are 22×22 circles — large enough to read at a glance, small enough to keep table compact.
- The glow effect (`box-shadow`) is only on the ≥28 tier to keep it meaningful (not decorative).

### 2026-05-01 — Rubric v2 revised: weighted 0–10 scale (Shane directive, round 2)

- **Directive 1:** Each criterion now scored 0–10. Weights (multipliers) replace variable max-points. Total max = 100 (weights sum to 10.0).
- **Directive 2:** Brand criteria combined weight (4.5×) now exceeds PM criteria combined weight (3.5×). The hero sample is a brand artifact first — this is now encoded in the rubric's structure, not just its argument.
- **Brand outcome:** Voice/Wit earns the single highest weight in the rubric: 2.5× (max 25). This is the headline number. It signals to every future author that the `because` messages are not copy decoration — they are the most heavily weighted quality in the rubric.
- **Brand held:** Emotional Hook at 2.0× (max 20). Tiebreaker position unchanged. Brand combined max 45 vs. PM combined max 35.
- **Concession accepted:** DSL Coverage at 2.0× (same as Hook and Legibility). Steinbrenner's floor (raw ≥ 4/10) is the real Coverage enforcement — the weight concession costs nothing when the gate is still inviolable.
- **Final weights:** Voice/Wit 2.5×, Emotional Hook 2.0×, DSL Coverage 2.0×, Domain Legibility 2.0×, Precept Differentiation 1.5×. Line Economy = gate.
- **Key brand principle reconfirmed:** The rubric's weight architecture is the brand's message to future authors. What gets the highest weight is what gets the most authorial attention. Voice/Wit at the top is a content strategy decision, not just a scoring decision.
- **Decision record:** `.squad/decisions/inbox/rubric-v2-decision.md` (updated)

### 2026-05-01 — Advisory fix: `reject` relocated in hero candidates 1 and 3

- **The terminal-state `reject` is brand-elegant but compiler-redundant.** "Cancelled subscriptions cannot be reactivated" is a perfect brand line — it's the thesis of structural impossibility made human. But in a truly terminal state (no exits), the compiler sees it as noise. The fix is to earn the `reject` by putting it where it has work to do.
- **Candidate 1 new `reject`:** "Cannot downgrade to a lower plan price" — still a SaaS operator truth, still has authority. Loses some of the structural-impossibility punch ("once cancelled, gone forever") but gains a runtime-guarded domain rule.
- **Candidate 3 new `reject`:** "Pull request has not received enough approvals to merge" — plain engineering truth, less witty but more universally relatable. The `Merged` state remaining structurally frozen (no transitions, no rows) preserves the "can't reopen" contract implicitly.
- **Brand principle reconfirmed:** `reject` is the highest-impact line in any Precept snippet. It must be positioned in a context where it earns its moment — guarding a path that could succeed but doesn't meet the condition. A `reject` in a dead state is a non-moment. A `reject` after a conditional transition is the thesis in action.


## 2026-04-04 — Expanded rubric criterion labels in visual-language-exploration.html

**Requested by:** Shane

Expanded all abbreviated rubric criterion labels to their full readable names across three locations in rand/explorations/visual-language-exploration.html:

- **Rubric definition table** (abbreviated code column + full name column): DSL → DSL Coverage, Eco → Line Economy, Leg → Domain Legibility, Wit → Voice / Wit, Hook → Emotional Hook, Diff → Precept Diff., Precept Differentiation → Precept Diff.
- **Gallery card score labels** (all 30 cards, 6 labels each): same mapping; DSL Cov → DSL Coverage, Line Eco → Line Economy, Legibility → Domain Legibility, Voice/Wit → Voice / Wit, Hook → Emotional Hook, Precept Diff → Precept Diff.
- **Ranking table column headers**: Cov → DSL Coverage, plus the same five as above.

Prose descriptions, ecause messages, and DSL code snippets were left untouched. Verified no double-expansion or breakage.


### 2026-05-01 — Line Economy gate: statement count research

**Requested by:** Shane — two problems with the current gate: (1) 12–16 lines feels too long, (2) lines are the wrong unit because cramming is invisible to a line counter.

**Research filed at:** `.squad/agents/j-peterman/line-economy-research.md`

**Method:** Count each syntactic atom — precept, field, invariant, state, event declarations (one each), `from…on` rule headers (one each whether or not guarded), and body actions `set`/`transition`/`no transition`/`reject` (one each). `on X assert` counts as zero (part of its event declaration). Multi-statement collapsed lines are unpacked.

**Key findings:**
- Top-10 candidates range 18–25 statements when measured this way.
- The #1 candidate (Subscription Billing, 29/30) lands at exactly 18 — the most compact sample in the top tier.
- The #5 candidate (Food Delivery, 27/30) lands at exactly 20 — the leanest of the 27-point tier.
- Candidates that scored Eco: 5 under the old line gate sit at 18–20 statements. Candidates that scored Eco: 4 sit at 22–25.
- The old gate's effective ceiling was ~25 statements — crammed lines let samples sneak through.

**Recommendation filed:** Gate of **12–20 statements**. Ceiling of 20 matches the two leanest passing candidates. Floor of 12 ensures all nine required constructs are present without padding. This is genuinely tighter than the old effective ceiling of ~25.

**Disqualified at 12–20:** Coffee Order (#3, 22), SaaS Trial (#2, 23), Deploy Pipeline (#4, 23), Feature Flag (#6, 23), Email Campaign (#8, 23), Freelance Contract (#7, 24), Gym Membership (#9, 25). All could qualify with minor editorial surgery. The gate is a target, not a verdict.
### Phase 1 Research Contribution: Hero Creative Brief (2026-04-04)

**Deliverable:** rand/references/hero-creative-brief.md

**8 Fictional Domains:** Duel at Dawn, Heist Safe, First Contact Protocol, Interrogation Room, Spell Casting (RPG), Mission Abort Sequence, Game Show Contestant, Chess Piece Promotion.

**Statement Count Tension Identified:** The 6–8 gate conflicts with minimum statements for full DSL coverage (~13–16). Recommend treating 6–8 as aspirational discipline, practical ceiling ≤16.

**Brand Principles Locked:**
- reject must guard a path where success is possible (not terminal state)
- because messages = highest-weight brand element (Voice/Wit, 25pts)
- in <State> assert = strongest differentiator (no equivalent in competitors)
- Fictional domains preferred — readers project themselves without real-world constraints

### Voice & Tone Revision: Wit Integration (2026-XX-XX)

**Requested by:** Shane Falik  
**Change:** Section 1.2 Voice & Tone in brand-spec.html updated to acknowledge and embrace dry wit.

**What Changed:**
- **Table row (Serious ↔ Funny):** Replaced "Serious. No jokes." with "Dry wit welcome. Never forced. Precision finds the humor in the truth."
- **Prose paragraph:** Expanded to acknowledge wit as integral to voice — "The voice states facts. It doesn't hedge. It doesn't oversell. It finds the wit in precision. When something matters, it says why — and the clarity itself can be the humor."
- **Do examples:** Added two wit examples showcasing precision humor:
  - "If you've been writing the same validation in four services, Precept has questions."
  - "Turns out business rules don't change just because you moved them to a different service."
- **Status chip:** Changed from "LOCKED" to "REVISED" (blue background, lighter text).

**Reasoning:**  
Precept's wit is not performance—it's earned from specificity. The tool knows what it does. The alternatives are slightly absurd. This wit doesn't mock the user; it states the truth in a way that makes the truth funnier than a joke. Like Stripe docs, like the best changelogs. Precision humor.

**Files Modified:**
- rand/brand-spec.html (section 1.2 only)


---

### 2026-04-05 — Section 1.4: Indigo Color System Overview card added

**File updated:** `brand/brand-spec.html`

**What was done:**
- Added a new **"Indigo Color System — Overview"** card at the TOP of section 1.4 (Color System), before the existing "Structure · Indigo" card.
- Card structure follows color-exploration.html palette-card format, adapted with inline styles to match .card class typography in brand-spec.
- Card contents:
  1. **Swatch bar** — 48px solid block at #6366F1, full-width bleed
  2. **Gradient ramp** — 8 equal segments from #1e1b4b (ground) → #312e81 → #3730a3 → #4338ca → #6366f1 → #818cf8 → #a5b4fc → #c7d2fe (pale)
  3. **Title**: "Indigo · 239°" in #6366F1; subtitle describes the 8-shade system
  4. **Color role table** — grid layout: swatch dot · hex · role name · usage description; Semantic/Grammar rows colored for legibility
  5. **NuGet badge** — using #6366f1 brand primary
  6. **Syntax snippet** — 5-line LoanApplication DSL with #4338CA keywords and #6366F1 grammar connectives
  7. **State diagram** — 280×80 SVG: Draft → Submit → UnderReview, #4338CA stroke, #A898F5 state text, #30B8E8 event text, #34D399 arrow
  8. **Bottom note** — italic sign-off on the indigo selection rationale

**Structural decisions made:**
- Card uses negative margins on swatch bar and ramp to achieve full-bleed effect inside the .card padding box
- Color role table built as grid-template-columns: 14px 80px 96px 1fr — swatch dot, hex, role, usage
- No new CSS classes introduced — all styling is inline per brief
- Arrow marker ID scoped as indigo-arrow-overview to avoid SVG <defs> collisions with other diagrams on the page
- #4338CA (Semantic) and #6366F1 (Grammar) rows use the actual token color for text, all other role names in #a1a1aa

**Reasoning:**
- The overview card gives readers the full picture — all 8 shades in one scan — before the per-role breakdown in the Structure card below
- The in-context examples (badge, snippet, diagram) demonstrate the palette live, not in abstraction
- Indigo-only treatment (no emerald/cyan/violet here) keeps the overview focused on the family

**Files affected:** `brand/brand-spec.html` only.


### 2026-07-11 — README hero example replaced: LoanApplication → TimeMachine

**File updated:** README.md

**What was done:**
Replaced the hero code block in the "Aha! Moment" section. LoanApplication — 40+ lines, bureaucratic, deeply nested guard logic — swapped for TimeMachine (15 lines). Clean structural confidence: one field, one invariant, 3 states, 2 events, dotted arg access, a when guard on 88 mph && 1.21 gigawatts, a reject with brand voice, and a tidy arrival loop. Comments stripped for austere register. Language tag removed from fence (plain monospace, no GitHub syntax highlighting).

**Why:**
- TimeMachine demonstrates Precept's core primitives more memorably — invariant, guard, reject, dotted access — in half the lines
- The error messages land: "The flux capacitor cannot run on vibes" communicates constraint philosophy faster than any explainer
- LoanApplication was correct but dense; it read like compliance, not craft
- Brand alignment: the hero is the first thing a developer reads. It should make them want to write a precept, not reach for a lawyer

**Files affected:** README.md only.

## 2026-04-04 — Brand mark: corrected to three forms

**Task:** Replace single-mark (Full/Badge/Micro size variants) in rand/brand-spec.html section 1.3 with the correct three marks from rand/explorations/semantic-color-exploration.html section 5. Also sync rand/explorations/visual-language-exploration.html Surface 3 icon section.

**What was wrong:**
- A previous agent replaced the brand mark card with a Full/Badge/Micro size-variant display — three sizes of the *same* mark. That was incorrect.
- A previous agent added a "Brand in system: the two primary surfaces" card with DSL code + state diagram — also incorrect placement.
- isual-language-exploration.html Surface 3 still showed TBD placeholder boxes.

**What was fixed:**
- rand/brand-spec.html section 1.3: Replaced "Brand mark form" card (size variants) + removed "Brand in system" card → new "Brand mark — three forms" card showing all 3 correct marks side by side with labels.
- rand/explorations/visual-language-exploration.html: Replaced TBD placeholder → same 3-mark display.
- All three SVGs copied exactly from semantic-color-exploration.html section 5, lines 637–675.
- Marks: State + transition / Tablet / precept / Tablet + state machine (primary).


### 2026-05-01 — Section 1.4: Indigo overview card reformatted to palette-card style

**Requested by:** Shane

**File updated:** `brand/brand-spec.html`

**What was done:**
- Replaced the bespoke `.card`-based indigo overview block in § 1.4 with a `.pc-card` that exactly mirrors the `palette-card` format from `brand/explorations/color-exploration.html`.
- New card structure: 64px solid swatch bar (#6366F1) → 32px 8-shade gradient ramp → h2 title "Indigo · 239°" → monospace hex line → four `.pc-context-block` sections (NuGet Badge, Icon Mock, Syntax Highlighting, State Diagram Accent).
- Added `pc-*` CSS class family to brand-spec.html's `<style>` block to reproduce the exact dimensions, colors, and typography of the exploration format without colliding with the existing `.palette-card` grid styles.
- Replaced the old role table + two-column NuGet/Syntax layout + separate diagram block with the unified four-section palette-card layout.
- NuGet badges: `v1.0.0` (indigo #4338ca), `.NET 8.0` and `license MIT` (slate).
- Icon mocks: combined tablet + state machine SVG at 64px and 32px.
- Syntax snippet: keywords #4338ca bold, state names #818cf8, event #30b8e8, operators #6366f1.
- State diagram: Draft → Submit → Review with indigo stroke (#6366f1) and emerald arrow (#34d399).

**Structural decisions made:**
- Used a new `pc-` class prefix rather than overriding `.palette-card` — the existing class is used as a grid item in a different context (§ 1.4 palette swatches).
- Kept all surrounding content intact: § 1.4 heading, both callouts, and all cards below the overview card.

**Decision record:** `.squad/decisions/inbox/peterman-indigo-card-format.md`

### 2026-04-04 — Brand compliance review: Elaine's 5-Surface Visual UX Spec

**Requested by:** Shane

**Review filed:** `.squad/decisions/inbox/j-peterman-surfaces-review.md`

**What was reviewed:**
Elaine's `brand/visual-surfaces-draft.html` — full UX specifications for five surfaces: Syntax Editor, State Diagram, Inspector Panel, Docs Site, CLI/Terminal.

**Overall verdict:** Approved with notes. The draft is grounded in the locked system throughout. One priority issue blocks integration into brand-spec.html; everything else is minor or aspirational.

**Key findings:**

1. **State Diagram — priority issue.** The color table lists three off-system hex values (`#A5B4FC`, `#94A3B8`, `#C4B5FD`) as lifecycle-tinted node colors — directly contradicting the spec's own "no lifecycle tints" callout and brand-decisions.md. Additionally, three off-system cyan sub-shades (`#38BDF8`, `#7DD3FC`, `#0EA5E9`) were introduced for event subtype differentiation. Both must be corrected: state nodes use indigo borders + violet text (`#A898F5`); event sub-shading either uses the single locked cyan or goes through a palette extension decision.

2. **Syntax Editor** — fully compliant. Eight-shade palette, typography signal, and AI-first note are all accurate and well-stated.

3. **Inspector Panel** — compliant. Verdict colors applied correctly. Gold (`#FBBF24`) used for constraint message text; this is defensible (human-readable explanation earns the warm interrupt) and spatially distinct from syntax gold. Minor: `#9096A6` (editorial/comments shade) repurposed for read-only field indicators — valid secondary use, should be documented.

4. **Docs Site** — aspirational, correctly held as future spec. Minor: "system font stack" should read Cascadia Cove with monospace fallback chain, not as an alternative typeface.

5. **CLI / Terminal** — compliant. Decision to exclude brand indigo from terminal output is correct given light terminal rendering constraints. Verdict colors + symbol redundancy is right.

**Brand positioning quality:** Strong across all five surfaces. The inspector framing ("both a human debugging tool and an API contract for AI tools") and the CLI note ("color should never be the only signal") reflect exactly the right positioning for a domain integrity engine used by both developers and AI agents.

---

### 2025-01-18 — README research complete: 13 projects studied, Hybrid model recommended

**Files created:** `brand/references/readme-research-peterman.md`

**Research delivered:**
- Studied 13 READMEs (8 comparable libraries, 5 exemplar projects) with real data: opening hooks (6–32 words), hero placement (2–51 lines from top), hero code (6–26 lines), section structure, positioning claims, AI-first signals
- Measured every metric from fetched content — no inference, no imagination
- Three README models identified: Content-Rich (XState, Stateless, Polly), Gateway (Bun, Biome, FastEndpoints), Hybrid (Zod, FluentValidation, React)
- AI-first positioning is **rare** (only NRules mentions GPT tooling) — Precept's MCP + Copilot + LSP story is unique opportunity
- Hero code ranges 6–26 lines; median 13 lines. Precept target: 18 lines (Subscription Billing DSL)

**Key findings:**
1. **Structural pattern:** Hybrid model works for Precept — hook → hero → AI tooling → features → links (not Content-Rich deep dives, not Gateway redirect-only)
2. **Hero conventions:** 18-line DSL sample (no runtime code), complete round trip (definition → usage), real domain (Subscription Billing not foo/bar), proves DSL readability in 30 seconds
3. **Positioning language:** Category-creating tools use "[X] is a [new category] for [platform]" structure. Recommendation: "Precept is a domain integrity engine for .NET that binds an entity's state, data, and business rules into a single executable DSL contract." (23 words)
4. **AI-first section:** Add dedicated "AI-First Tooling" section after hero with MCP server (5 tools), Copilot plugin (agent + 2 skills), language server (LSP) — this is **unique to Precept** and factual
5. **Copy tone:** Declarative present tense, concrete metrics (18-line hero, 9 DSL constructs, 5 MCP tools), technical precision (DSL runtime, interpreter, contract), no hedging
6. **Anti-patterns:** Don't bury hero after 50 lines (Polly), don't show runtime invocation in hero (MediatR), don't redirect before showing code (Vue), don't use 32-word opening sentence (Polly)

**Recommendations synthesized:**
- **Structure:** Hybrid README (hook + hero + AI tooling + features + links)
- **Hero:** Subscription Billing 18-line DSL sample (aligns with line-economy research pole star)
- **Positioning:** "Domain integrity engine for .NET" (category-creating language, not comparative)
- **AI-first:** Lead with MCP + Copilot + LSP as differentiated tooling story
- **Copy:** J. Peterman voice (evocative but precise, concrete, confident, technical)

**What this unblocks:**
- README revision with research-backed structure (not guesswork)
- AI-first positioning as competitive advantage (no comparable library leads with MCP/Copilot/LSP)
- Hero sample selection finalized (Subscription Billing at 18 DSL statements is research-validated length)

**Next:** Draft new README.md using Hybrid model, 18-line hero, AI-first section.
