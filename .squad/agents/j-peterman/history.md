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
