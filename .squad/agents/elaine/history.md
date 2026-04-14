## Core Context

- Owns UX/design for README structure, brand-spec surfaces, palette application, and cross-surface scannability.
- Locked README principles: mobile-first above-the-fold layout, single primary CTA, semantic heading hierarchy, progressive disclosure, viewport resilience, screen-reader compatibility, and AI-parseable structure.
- Brand surfaces separate identity palette from syntax/runtime semantics. Emerald, Amber, and Rose carry runtime meaning; Gold is a judicious brand-mark accent, not a general UI color lane.
- Peterman owns public prose; Elaine owns form, layout, and surface presentation rules. Hero samples must remain legible in plaintext and constrained viewports before any decorative treatment.

## Recent Updates

### 2026-04-13 - PR #70 loop continuation: committed manifest and pushed all unpushed design work to origin

- Committed `design/system/semantic-visual-system-manifest.md` — the first-pass canonical manifest that was sitting uncommitted in the worktree. This closes out the semantic foundation artifact for the preview redesign lane.
- Pushed all 25+ unpushed local commits to `origin/design/semantic-visual-system-review`, making the full design iteration visible on PR #70. This includes 14 prototype HTML files (`event-button-ux-exploration-v2` through `v5`, `header-badge-exploration`, `river-flow-*`, `state-diagram-specimen-exploration`, `timeline-*`), research docs, and major iterations on `semantic-visual-system.html`.
- The manifest is an explicit boundary document: faithful projection over runtime truth, three target surfaces (state diagram, timeline, data form), open questions surfaced rather than elided, and a clear mapping between what the current runtime already supports and what richer surface semantics still need.
- The "continue in-progress design iteration" checklist item on PR #70 remains open until Shane reviews the prototype work and confirms the design iteration is sufficient to mark the PR ready for review.

### 2026-04-13 - Semantic Visual System manifest draft boundary

- Created `design/system/semantic-visual-system-manifest.md` as the first-pass canonical manifest structure for the Semantic Visual System.
- Locked the draft around three target surfaces: state diagram, timeline, and data form.
- Restated the boundary rule explicitly: the manifest is a faithful projection over runtime truth, not a second semantic system.
- Recorded that current public runtime contracts appear sufficient for much present-tense semantics, while timeline history and some richer surface semantics likely need public runtime support or host-owned receipts/history.
- Marked the data form as the least resolved surface and elevated its semantic questions into an explicit open-questions section instead of implying false closure.

### 2026-04-12 - Header Badge Exploration: Variant 11 — Layout Flip (full card restructure)

- Added Variant 11 ("Layout Flip") to `design/prototypes/header-badge-exploration.html` — a complete card layout restructure, not just a header badge.
- Core idea: the micro state diagram becomes the structural spine of the card. Current state node at **top-left**, precept metadata (name + ref + record) at **top-right**, form fields spanning full width in the middle, event lanes at **bottom-left**, Save/Cancel at **bottom-right**.
- Creates a vertical reading flow: "I'm in this state → here's the data → here's where I can go next."
- Uses CSS Grid (`grid-template-areas`) for the four-quadrant layout. SVG dashed vertical guide line hints at the origin→outcomes connection.
- Full card mockup with VisitorBadgePickup at ReadyForPickup: all 5 fields (VisitorName, HostConfirmed constrained, BadgePrinted, PickupWindowOpen, OutcomeNote editable+required), PickUp (active→PickedUp terminal) and Expire (blocked→Expired terminal) event lanes with inline verdicts.
- All SVS CSS tokens replicated locally: `.svs-state-node`, `--current`, `--terminal`, `--terminal-blocked`, `.svs-edge-token`, `--active`, `--blocked`. Form rows and buttons use `flip-` prefixed variants to avoid collision with existing card-strip styles.
- Annotation below card explains the layout philosophy.
- Updated page lead (10→11 variants), design notes section (added Variant 11 note on structural departure + CSS Grid requirement).
- Learnings: CSS Grid `grid-template-areas` is clean for this kind of four-quadrant card layout. The `flip-` prefix namespace avoids style collision with the existing card-strip and form-card styles used by Variants 1–10 and v5 respectively. SVG overlay for connector lines works but exact positioning is approximate in a grid context — may need JS measurement for pixel-perfect connector endpoints in a production implementation.

### 2026-04-12 - Header Badge Exploration: 10 top-right badge variants

- Created `design/prototypes/header-badge-exploration.html` — standalone HTML with 10 header badge variants, each showing a realistic form card header strip (left: precept name + record ref, right: candidate badge).
- Context: VisitorBadgePickup #VBP-2847, Ada Reyes, ReadyForPickup.
- Variants: (1) Constraint Pulse — rule health ratio, green/amber (shown in healthy + warning sub-variants), (2) Action Readiness — dot indicators for available vs blocked events, (3) Journey Progress Arc — segmented bar showing lifecycle position (state 4 of 6), (4) Data Completeness Ring — SVG donut showing 5/7 fields set, (5) Time-in-State — duration badge with clock glyph, (6) Risk/Attention Tier — colored dot (green/amber/red) with hover-expandable label (3 sub-variants shown), (7) Last-Touch Attribution — avatar + name + timestamp, (8) Next-Action Hint — forward-looking event in event colour, (9) Transition Sparkline — state-coloured dot sequence showing recent transitions, (10) Inspectability Toggle — inspect glyph with hover affordance.
- Design notes section categorises variants by: data-rich (1–4), temporal (5), compact (6), attribution (7), forward-looking (8), retrospective (9), structural (10). Notes on composition potential and runtime feasibility.
- No state pill / state badge used — each variant explores what replaces it.

### 2026-04-12 - Event Button UX v5: Micro State Diagram — definitive prototype

- Created `design/prototypes/event-button-ux-exploration-v5.html` — standalone HTML implementing Shane's "micro state diagram" direction, evolving from K4 (Action Lane with Verdict).
- Concept: the action area at the bottom of each form card IS a miniature inline state diagram. Current state node on the left, edges flowing right through event tokens to target state nodes, with always-visible verdict lines under each event.
- Uses **exact Semantic Visual System CSS** from `design/system/semantic-visual-system.html`: `.svs-state-node`, `.svs-state-node--current` (bright violet fill), `.svs-state-node--terminal::after` (inner double-border), `.svs-state-node--initial .svs-state-node-dot` (8px filled dot), `.svs-state-node--unreachable-current` (dashed, dimmed). Edge tokens: `.svs-edge-token`, `.svs-edge-token--active` (glow + strong border), `.svs-edge-token--blocked` (dashed). Form: `.form-row`, `.form-lbl`, `.form-locked-val`, `.form-input`, `.form-btn`.
- Six examples covering all variations:
  1. **ReadyForPickup** (2 events: 1 active, 1 blocked) — canonical branching scenario
  2. **Requested** (1 event: active with args) — single lane, initial state dot, arg hint
  3. **Approved** (2 events: both active, one with args) — dual active lanes, terminal target on Cancel
  4. **InProgress — scalability test** (5 events: 3 active, 2 blocked, 1 self-transition, 1 with args) — IT helpdesk scenario testing vertical limits
  5. **PickedUp — terminal** (0 events) — empty diagram with terminal badge, "no transitions available"
  6. **WaitingForCustomer — self-transition** (3 events: 1 self-loop, 1 active, 1 blocked) — ↺ glyph and current-state modifier on self-transition target
- Scalability analysis: pattern is strongest at 1–4 events, acceptable at 5 with active-first sorting, breaks at 6+ (action area dominates form). Mitigation: collapse blocked lanes into summary row.
- Shane's feedback requirements addressed: (1) micro state diagram — not just buttons but a visible mini diagram, (2) multiple examples with all variations, (3) scalability testing, (4) explains why events are available AND blocked, (5) exact SVS implementation.

### 2026-04-12 - Event Button UX v4: 5 transition lane refinements, recommendation: Transition Manifest (K5)

- Created `design/prototypes/event-button-ux-exploration-v4.html` — standalone HTML with 5 variants (K1–K5) evolving from v3's Variant K (Transition Lane), per Shane's feedback.
- Shane's constraints addressed: (1) no hover required — all verdicts/targets visible at rest, (2) don't repeat current state — origin lives in header badge or shown once as shared diverge point, (3) always show target state — even blocked events show their target, (4) semantic modifiers on target states — terminal ◎ double-border, initial filled-dot, current solid-bright, unreachable dimmed, constrained italic.
- Variants: K1 (Diverging Lanes — shared origin, horizontal branching), K2 (Target-Only Cards — event→target mini-cards, most compact), K3 (Stacked Transitions — vertical table-like rows, narrow-panel friendly), K4 (Action Lane with Verdict — horizontal lanes with always-visible verdict strips), K5 (Transition Manifest — synthesis of K3 structure + K4 verdicts + K1 semantic modifiers + K2 no-origin principle).
- Semantic state modifier visual system defined as shared CSS: `.state-node--current` (solid bright border), `.state-node--terminal` (double border via box-shadow), `.state-node--terminal-blocked` (rose double border), `.state-node--initial` (filled dot ::before), `.state-node--unreachable` (dimmed), `.state-node--constrained` (italic). Terminal marker glyph: ◎.
- Recommendation: Ship K5 (Transition Manifest). Satisfies all four constraints. Vertical structure fits VS Code sidebar. Each row is self-documenting with event → target + verdict. Second choice: K3 for minimal vertical budget. K1 for wide panels where branching diagram expressiveness justifies the horizontal cost.

### 2026-04-12 - Event Button UX v3: 5 research-informed variants, recommendation: Smart Badge Preview

- Created `design/prototypes/event-button-ux-exploration-v3.html` — standalone HTML with 5 complete form-card mockups (Variants G–K), driven by external research across 20+ business products.
- Absorbed findings from `research/design-system/business-app-inspectability-*.md` (3 research docs: UX, product communication, architecture).
- Key research principles applied: three-tier progressive disclosure, "because" grammar, always-visible beats hover, never hide blocked actions, checklists beat expressions, one-line domain-language reasons, IBM Carbon callout pattern.
- Variants: G (Verdict Strip — always-visible verdict per button), H (Checklist Gate — Guidewire workplan pattern), I (Smart Badge Preview — Shane's v2 idea + persistent blocked reason), J (Action Card — events as Stripe-style mini-cards), K (Transition Lane — wild card, inline micro-diagram in action bar).
- Full form context: constrained field (italic label), required field (amber label), editable input, Save/Cancel buttons on left side of action bar.
- Recommendation: Ship Variant I (Smart Badge Preview). Satisfies all three of Shane's requirements (business-app aesthetic, compact, fully inspectable). Badge morph is Shane's own idea enhanced with always-visible blocked reason from research. Variant G as fallback; Variant H for multi-guard events.

### 2026-04-12 - Event Button UX v2: 10 alternatives, top 3 prototypes, state-badge preview (Shane's idea)

- Created `design/prototypes/event-button-ux-exploration-v2.html` — standalone HTML with 10 alternatives table and full form-card mockups for Variant D (State Badge Preview), Variant E (Inline Outcome Strip), and Variant F (Consequence Gutter).
- Mock data: VisitorBadgePickup #VBP-2847, Ada Reyes, ReadyForPickup. Events: PickUp (active → PickedUp), Expire (blocked: PickupWindowOpen must be false → Expired).
- All three variants use business-app aesthetic: humanized field labels, sans-serif body text, proper action button sizing — not dev-tool chrome.
- Variant D (State Badge Preview) implements Shane's exact idea: three stacked `.sbadge` elements in the badge area, `opacity` transitions driven by CSS `:has()` selectors. Active hover fades current state out and previews destination in violet; blocked hover fades in rose badge + guard explanation below the action bar. No JavaScript.
- Variant E (Inline Outcome Strip) is the always-visible zero-interaction pattern: persistent subordinate label below each button showing "→ TargetState" or "⊘ Guard reason". Solves both UX problems at zero interaction cost; accessible by default.
- Variant F (Consequence Gutter) is a fixed-height band between the field section and actions. Quiet at rest (italic hint text). On hover: full path sequence renders (FromState → Event → ToState). Blocked events show path up to event then guard reason. Also CSS `:has()` driven.
- CSS `:has()` validated as the right mechanism for card-scoped hover state — no JS needed for the prototype. Browser support: Chrome 105+, Firefox 121+, Safari 15.4+ — acceptable for a design prototype.

### 2026-04-12 - Canonical semantic visual system artifacts flattened into design/system
- Moved `semantic-visual-system.html`, `semant-visual-system-canonical.precept`, and `semantic-visual-system-notes.md` from `design/system/foundations/` into `design/system/` and retired the empty `foundations/` folder.
- Updated live workspace references in design READMEs, brand review/spec docs, and the state-diagram specimen prototype so the canonical artifact path is now `design/system/semantic-visual-system.html` and the shared specimen path is `design/system/semant-visual-system-canonical.precept`.

### 2026-04-12 - Sticky subsection labels aligned with rail typography reset
- Removed the remaining small-caps override from the sticky subsection labels in `design/system/foundations/semantic-visual-system.html` so they match the active rail.
- Preserved sticky placement, left-rail alignment, spacing, and the existing typography scale; the change was limited to the label treatment.

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

## Learnings

- 2026-04-12 — Header badge exploration: the "strip-only" card format (header without form body) is the right fidelity for badge comparison. Full form cards add visual noise that distracts from the badge itself. When comparing 10+ small variants, minimal context is better than complete context.
- 2026-04-12 — Header badge feasibility split: variants 1 (Constraint Pulse), 2 (Action Readiness), 3 (Journey Progress), 4 (Data Completeness), 8 (Next-Action Hint), and 10 (Inspectability Toggle) can all be derived from the precept definition + current instance state alone. Variants 5 (Time-in-State), 7 (Last-Touch), 9 (Sparkline) require runtime metadata the DSL doesn't currently model. Variant 6 (Risk Tier) requires a computed model. This feasibility split matters for implementation ordering.
- 2026-04-12 — Badge composition: the inspect toggle (variant 10) is unique in being combinable with any other badge — it's structural (a gateway), not semantic (a signal). Natural pairings: Constraint Pulse + Toggle, Journey Progress + Next-Action Hint. Design should consider which combinations produce information overload vs. complementary signals.
- 2026-04-12 — SVG donut for data completeness: a 22×22px SVG ring with `stroke-dasharray`/`stroke-dashoffset` is the cleanest way to render a proportional fill in a tiny badge. No JS required — pure CSS transition on `stroke-dashoffset` for animated fill. The `transform: rotate(-90deg)` on the SVG starts the fill from the 12 o'clock position.
- 2026-04-12 — v5 key insight: the "micro state diagram" framing transforms the action area from a button bar into a decision surface with structural meaning. It's not a button-with-metadata; it's a diagram that happens to contain clickable nodes. This reframing changes how users read the UI — diagram reading is top-to-bottom, left-to-right, following arrows, which matches exactly how the micro diagram lays out.
- 2026-04-12 — v5 SVS fidelity: using exact SVS class names and CSS properties (`.svs-state-node`, `.svs-edge-token`, `::after` inner double-border instead of box-shadow trick) produces a micro diagram that is visually continuous with the full state diagram in the preview panel. The user doesn't have to learn a second visual vocabulary for the action area — same node shapes, same token shapes, same modifiers.
- 2026-04-12 — v5 terminal double-border: the `::after` pseudo-element with `inset: 4px` is the canonical SVS terminal marker. The v4 box-shadow trick (`0 0 0 2px bg, 0 0 0 3px border`) was a prototype approximation. v5 uses the canonical `::after` method, which is more reliable across backgrounds and doesn't break at different opacity levels.
- 2026-04-12 — v5 self-transition treatment: showing the target as a visually-current node with a ↺ glyph is the most honest representation. The diagram literally says "you go back to where you started." The glyph prevents the user from thinking it's a display error (why is the target the same as the origin?). Tried alternative: a looping arrow — too complex for inline rendering, would need SVG.
- 2026-04-12 — v5 arg hint pattern: showing `(Name)` inside the edge token is the lightest-weight way to signal "this event needs input." It doesn't add a separate UI element; it's inline typography in the token itself. For events with multiple args, this could get long — consider `(2 args)` as a fallback for 3+ arguments.
- 2026-04-12 — v5 scalability ceiling: 5 events is the practical maximum for the micro diagram before the action area overwhelms the form visually. At 5 lanes (~280px vertical), active lanes need to sort before blocked lanes so the user sees actionable options without scrolling. At 6+, a "collapsed blocked" summary row is necessary. The VisitorBadgePickup precept (max 2 events per state) is well within the sweet spot.
- 2026-04-12 — v5 verdict voice: "✓ guard passes — PickupWindowOpen is true" (active) vs. "⊘ guard fails — PickupWindowOpen must be false" (blocked). The consistent structure (icon + category + explanation) makes verdicts scannable without reading full sentences. Shane specifically liked that active events explain WHY they're available, not just that they are.

- 2026-04-12 — v4 key insight: when Shane says "no hover required," he means the transition lane concept must be self-documenting at rest. The v3 Variant K hid target states behind hover — that was the core issue. Making targets always-visible transforms the lane from a progressive-reveal widget into a decision manifest.
- 2026-04-12 — v4 redundancy elimination: repeating the origin state per-lane is visually wasteful when all events share the same origin (which they always do — events are scoped to a single state). Three valid solutions: (a) shared origin node with diverging lanes (K1), (b) origin in the header badge only (K2/K3/K4/K5), (c) implicit from context. Option (b) won because the header badge already carries the current state.
- 2026-04-12 — v4 semantic modifiers: the double-border terminal marker (box-shadow trick: `0 0 0 2px bg, 0 0 0 3px border`) is the cleanest CSS-only double-border effect. The ◎ glyph as inline terminal marker is more readable than relying solely on the double-border at 9.5px font size.
- 2026-04-12 — v4 vertical vs. horizontal: the vertical stacked layout (K3/K5) is structurally better for the VS Code sidebar because it uses block-axis space (unlimited scroll) instead of inline-axis space (constrained by panel width). Horizontal lanes (K1/K4) look better in wide viewports but break at sidebar default width (~350px).
- 2026-04-12 — v4 verdict-per-row pattern: embedding a one-line verdict below each transition row (K4/K5) creates a self-documenting decision surface. The verdict answers "can I do this?" without requiring the user to parse the blocked reason's negative framing. Active verdict: "✓ Ready — fire to complete pickup"; Blocked verdict: "⊘ reason". The positive-voice ready message for active events is a UX win over just showing the target state — it confirms intent, not just destination.
- 2026-04-12 — v4 variant ranking: K5 (Transition Manifest) > K3 (Stacked Transitions) > K4 (Action Lane with Verdict) > K2 (Target-Only Cards) > K1 (Diverging Lanes). K5 wins because it's the only variant satisfying all four of Shane's constraints in a sidebar-friendly layout. K1 is the strongest visually but needs wide panels.
- 2026-04-12 — v3 key insight: the winning pattern is "always-visible for blocks + progressive enhancement for destinations." Blocked reasons must never be hover-gated — research unanimously confirms this across NNGroup, Guidewire, Stripe, and Carbon. Active-event destination can justify hover as a progressive enhancement because the user's primary task is clicking the button, not reading its metadata.
- 2026-04-12 — v3 variant ranking: I (Smart Badge Preview) > G (Verdict Strip) > H (Checklist Gate) > J (Action Card) > K (Transition Lane). I wins because it's the only variant satisfying all three of Shane's constraints (compact, business-aesthetic, fully inspectable) simultaneously. G is functionally equivalent but lacks the badge-morph delight.
- 2026-04-12 — The Action Card pattern (Variant J) is the strongest for event-dense forms (3+ events) because each card is a self-contained information unit — but for 2-event forms it consumes too much horizontal space relative to a button strip. Consider J as a density-adaptive variant: use button strip for ≤2 events, switch to card grid for 3+.
- 2026-04-12 — The Transition Lane pattern (Variant K) is the most novel — no surveyed product embeds a micro-diagram in the action bar. Strongest for teaching users how state machines work (onboarding), but readability degrades at narrow panel widths because the lane is inherently horizontal. Not suitable for the VS Code sidebar at default width.
- 2026-04-12 — The Checklist Gate pattern (Variant H) is the right progressive-disclosure expansion for events with multiple guard conditions. A single guard renders as a one-item checklist (which just looks like a verdict line), but 3+ guards render as a scannable requirement list with individual pass/fail dots. Consider H as a disclosure expansion under I: click the blocked reason to expand into a full checklist.
- 2026-04-12 — Save/Cancel buttons belong on the LEFT side of the action bar, event buttons on the RIGHT. This mirrors the standard form-vs-workflow separation in Salesforce, ServiceNow, and Dynamics. Form operations (save/cancel) operate on field edits; event operations (pick up/expire) operate on lifecycle transitions. Spatial separation prevents accidental lifecycle transitions when the user intended to save.
- 2026-04-12 — Constrained fields (italic label) and required fields (amber label + asterisk) are sufficient visual indicators for field metadata in the form card. The italic/amber distinction maps to Precept's `edit` constraints vs. `required` validators without requiring additional chrome or explanation panels.
- 2026-04-12 — Event Button UX exploration: the Event Row Table (Approach A) is the correct abstraction for the preview panel event bar. It frames events as a decision surface — name, guard status+reason, target state, and action all visible simultaneously in resting state with no interaction required. Solves disabled-reason and pre-click destination blindness simultaneously. Semantically structured table markup makes it accessible by default.
- 2026-04-12 — For the event bar specifically, "zero interaction to understand the full state" is the UX bar to clear. Hover tooltips, popovers, and click-to-reveal patterns all fail that bar. Inline Metadata Strip (Approach B) is the acceptable interim if Approach A's vertical budget is a blocker.
- 2026-04-12 — Split Button (Approach C) has the best resting compactness for active events but introduces keyboard/accessibility ambiguity (two interaction zones per button) that needs explicit design work before it's implementation-ready. Do not ship C without fully speccing the keyboard contract.
- 2026-04-12 — Event Button UX v2 learnings: CSS `:has()` is the cleanest mechanism for card-scoped hover preview effects in prototypes — no JS, no inline event handlers, fully inspectable. Stack three `position: absolute` badge elements at opacity 0/1 and toggle them with `.card:has(.btn:hover)` selectors. Requires the badge container to have a fixed min-width/height to prevent layout collapse during the transition.
- 2026-04-12 — The Inline Outcome Strip (Variant E) is the only one of the three top patterns that solves both UX problems with zero interaction dependency. For touch-first or accessibility-first contexts, E must be the baseline and D/F are progressive enhancements.
- 2026-04-12 — Shane's hover-state-preview idea (Variant D) works because it repurposes a UI element (the state badge) that the user is already trained to look at for status information. The badge hover-preview is an extension of existing mental model, not a new pattern to learn.
- 2026-04-12 — The Consequence Gutter (Variant F) is strongest for forms with 3+ events where button labels alone are ambiguous. For a 2-event form like VisitorBadgePickup, E is the better choice — F's idle-to-active reveal feels under-populated with only two events.
- 2026-04-12 — Business-app aesthetic for event buttons: use `font-family: var(--sans)` for button labels (not mono), padding 8px 18px, border-radius 4px, and font-weight 500. Reserve mono for state badge text, field labels, and record IDs. This single switch from mono to sans makes the action bar read as a business form rather than a dev tool.

- 2026-04-12 — The event bar in the preview panel is currently under-designed relative to the information density the developer actually needs. The current disabled-button-with-title-tooltip pattern is a placeholder, not a finished design.

- 2026-04-12 — The cleanest folder story is status-based: `design/system/` holds live canonical system artifacts, `design/prototypes/` root stays reserved for active durable prototype work, and `design/prototypes/archive/` holds preserved reference sets. An empty-looking prototypes root is acceptable if it prevents archive material from reading like live direction.

- 2026-04-13 — In a split-row lifecycle timeline, putting event labels on the opposite side of the rail from state labels clarifies cause vs. result without touching the rail geometry. Keep edit rows with the state/context side unless the UI is explicitly treating edits as lifecycle-driving actions.

- 2026-04-12 — The semantic visual system artifacts are canonical enough that they should live at the root of `design/system/`, not inside a now-misleading `foundations/` subfolder. The prototype state specimen also depends on the shared canonical `.precept` by relative path, so path moves have to be validated as UX-adjacent behavior, not treated as a pure docs rename.

- 2026-04-06 — The semantic visual system page should frame itself as Precept's strongest live expression of the visual system: still canonical and disciplined, but explicitly allowed to be beautiful enough to prove the system rather than merely police it.
- 2026-04-06 — On semantic-system foundation pages, the typography contract should be explicit as a role map: Cascadia for identity and system-facing UI, Segoe UI Variable Text for paragraph reading, and italic reserved for semantic pressure rather than generic emphasis.
- 2026-04-06 — Canonical design-system pages for Precept work best as black-field editorial systems: sticky left rail, stacked spec sections, disciplined indigo linework, and one strong specimen block instead of multiple floating hero treatments.
- 2026-04-06 — Design-system foundation pages that define Precept surface semantics should stay on a near-solid black field with restrained indigo structure cues. Atmospheric gradients and blur read as off-brand for Precept's editor-adjacent identity.
- 2026-04-06 — On semantic-system foundation pages, the type system should split cleanly: Cascadia for identity, headings, labels, and tables; a restrained sans reading face for paragraph-length prose. All-mono long-form copy feels unfinished, but serif pushes the system away from Precept's editor-adjacent character.
- 2026-04-06 — The right typography hierarchy for system-spec pages is Cascadia-led throughout: mono labels, mono section heads, and a small-caps display moment only where a real Precept wordmark/display treatment is warranted.
- 2026-04-06 — In design-system language, "surface" must explicitly include editorial/public carriers like the website, docs, and README examples, not just operational product UI. The distinction to preserve is operational vs. explanatory, not product vs. non-product.
- 2026-04-06 — For long-form system-spec pages, shared section-heading clamps and shared card/body paragraph sizes need to stay conservative. Big mono headings and enlarged explanatory copy make the document feel louder than the content needs; calmer scales work better when the hierarchy is carried by spacing, panel rhythm, and contrast.
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

### 2026-04-11 — State Diagram Specimen: Prototype-to-Specimen Redesign

- Redesigned `design/prototypes/state-diagram-runtime-options.html` from a dense 1557-line prototype comparison board into a ~905-line design-system specimen page.
- Removed: hero section, Option 01/02/03 headers and numbering, secondary-section mini-cards, commentary grid, footnote, snapshot comparison cards, annotation strip, legend, surface stats, surface controls, meta cards, all prototype labels/kickers/status badges. Removed ~60 CSS rules that no longer had referencing HTML.
- Preserved: authored-file renderer (JS tokenizer + data-source fetch), routeLeadRuntimeDiagram() SVG path routing, hover sync (mouseenter/mouseleave/focus/blur → is-linked class), black diagram stage, current-state violet fill, terminal double-border, initial node-dot, unreachable-current dashed treatment, unreachable-initial hatched treatment, tightened canvas max-width, responsive stacking media queries.
- New specimen header: single `h1` in 11px/0.2em uppercase Geist Mono — vanishingly quiet so the split surface is the hero.
- Surface bar labels now carry data context ("VisitorBadgePickup" / "ReadyForPickup · PickupWindowOpen = true") instead of prototype framing text.
- Code pane simplified: code-frame with code-head (filename + pills) directly — no pane-kicker, no pane-copy, no pane-head wrapper.
- Runtime pane simplified: diagram-surface → diagram-stage only — no surface-topline, no annotations, no snapshots, no caption, no legend.
- CSS variables pruned: removed --panel, --panel-2, --panel-3, --valid, --warn (no remaining consumers).
- Key design decision: a specimen page should let the surface speak. Every removed element was a justification layer that a specimen doesn't need — the split surface IS the argument.
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
- 2026-04-06 — Canonical design-system hero copy should describe the system as a user-facing meaning layer, not as document governance. Lead with what the system does, then explain the recognition and cognitive-load benefits of consistent signals across authoring, diagram, and preview surfaces.
- 2026-04-06 — In semantic-system guidance, "surface" is the user-facing context and "carrier" is the visual form used within that context. Canonical overview copy should name both usability benefits and the brand outcome: the system must make Precept easier to read and more visibly, proudly itself.
- 2026-04-06 — Canonical system-foundation pages work best as black-field editorial control surfaces: one manifesto block, one semantic specimen, a sticky rule rail, and sharp section rhythm so the page reads as operating grammar rather than generic dark-mode documentation.
- 2026-04-06 — For system-foundation prose, Segoe UI Variable Text with Segoe UI fallback is the right reading companion to Cascadia: same Microsoft lineage, calmer in paragraphs, and technical without turning corporate or soft.

- 2026-05-17 — README hero DSL text sizing: image-based approaches (PNG or SVG) are fundamentally brittle for text-alongside-text on GitHub — the image and page text live in disconnected scaling contexts. `<font color>` inside `<pre>` is a potential middle ground (native text + syntax color) but depends on deprecated HTML and needs dual-theme testing. Fenced code block is the only guaranteed-consistent approach. Long-term path is Linguist registration for native `` `precept `` highlighting.
- 2026-05-17 — Frank's architectural analysis of the PNG sizing problem is thorough and correct on the core constraint. His recommendation (fenced code block) is the same as mine, and that tradeoff is now preserved in `.squad/decisions.md`.
- 2026-05-18 — Regenerated `readme-hero-dsl.png` to match GitHub's 830px max image display width. Previous image (1268px) scaled to 830px, shrinking code text to ~8.5px vs GitHub's ~13.6px code blocks. New approach: capture at 830px viewport with 2× deviceScaleFactor → 1660px image. Code text now renders at ~13px on GitHub. Added `design/brand/capture-hero-dsl.mjs` Playwright script for reproducible regeneration. Final width contract is now preserved in `.squad/decisions.md`.
- 2026-05-18 — GitHub README image width reference: repo README view caps at 830px (per wh0/banner-width research). The `.markdown-body` container maxes at 980px with 45px padding. Different views have different caps: markdown view 1012px, VS Code extension 882px, VS Marketplace ~711px.

### 2026-04-12 — Business App Inspectability UX Research

- Wrote comprehensive external research document at `research/design-system/business-app-inspectability-ux.md` covering 7 categories of business software and how they surface system reasoning to end users.
- Categories researched: Workflow/BPM (Salesforce Path, ServiceNow, Pega, Camunda, Temporal), Form Builders (PowerApps, Appian, Airtable), Issue Trackers (Jira, Linear, GitHub Projects), Document/Approval (DocuSign, Ironclad), Insurance/Claims (Guidewire ClaimCenter, Clio), Banking/Fintech (Stripe Dashboard, nCino), State Machine Tools (Stately.ai/XState, AWS Step Functions, Node-RED).
- 6 key findings extracted:
  1. The "Why" Gap is universal — almost no product explains why an action is blocked. This is Precept's top differentiator opportunity.
  2. Checklists beat boolean expressions — Guidewire workplans and nCino stage requirements are the most legible guard-presentation patterns.
  3. Always-visible beats hover/click — NNGroup + Smashing Magazine evidence confirms always-visible guard status is the correct baseline (validates Variant E).
  4. Form shape-shifting is a powerful teaching tool — ServiceNow UI Policies and PowerApps conditional visibility are analogous to Precept's `edit` declarations.
  5. State diagram + form is genuinely novel — no researched product combines state machine visualization with form-level inspectability. Precept's three-zone layout is unique.
  6. Stripe's event timeline is the gold standard for structured inspectability — every state named, every transition triggered, every failure structured.
- Pattern glossary with 10 named patterns mapped to Precept implementation: Progress Rail, Guard Checklist, Inline Outcome Strip, State Badge Preview, Event Timeline, Transition Buttons, Per-Field Validation Message, Form Shape-Shifting, IF/ELSE Guard Annotation, Failure Diagnosis Panel.
- Key anti-pattern identified: Jira's approach of hiding blocked transitions entirely (conditions filter out the button). Precept must always show all declared events including blocked ones.
- Smashing Magazine disabled-button research validates: disabled buttons without explanation are a frustration pattern. The recommended alternative (always-enabled + validate on click, or disabled + adjacent explanation text) aligns with Precept's Variant E design direction.

## Learnings

- 2026-04-12 — The universal failure across business apps is not explaining WHY an action is blocked. Most tools either hide the action (Jira), disable with no explanation (most forms), or show errors only after clicking (Salesforce Path). Precept's DSL-level guard expressions give it a structural advantage: the "why" is always available because it's declared in the precept definition.
- 2026-04-12 — Guard conditions rendered as a checklist (item + pass/fail status) are more legible than guard conditions rendered as boolean expressions. Guidewire workplans and nCino stage requirement lists prove this at enterprise scale. Precept should consider a "guard checklist" mode for the event detail expansion.
- 2026-04-12 — No existing product combines state machine visualization with form-level inspectability in a single view. Stately has diagrams without forms. ServiceNow has forms without diagrams. Guidewire has workplans without state graphs. Precept's three-zone preview layout is genuinely novel.
- 2026-04-12 — Stripe's PaymentIntent lifecycle is the best external reference architecture for Precept's inspectability model: explicit states, named transitions, structured failure reasons, event timeline. The key difference: Stripe is for payment operations teams; Precept is for any business entity lifecycle.
- 2026-04-12 — Stately.ai's "IF guardName / ELSE" annotation on guarded transitions is the most elegant guard-visibility pattern found in any state machine tool. Precept's state diagram could adopt this for transition edge labels.
- 2026-04-12 — Progressive disclosure tiers for inspectability: Tier 1 (always visible) = state badge, event names, enabled/blocked status, target state, guard summary. Tier 2 (on hover) = full guard expression, state preview, field changes. Tier 3 (on click) = complete transition trace, history, constraint details. This three-tier model aligns with NNGroup's recommendation of max two disclosure levels plus one investigative level.
- 2026-04-12 — Sticky subsection labels in `design/system/foundations/semantic-visual-system.html` should follow the same typography reset as the active rail: no small-caps override and no OpenType small-caps feature. Natural title case keeps the left navigation vocabulary consistent without disturbing the baseline-led alignment model.
- 2026-04-12 — `design/prototypes/` should stay visually quiet at the root: active prototype work or orientation only. Once a prototype set becomes preserved reference material rather than active top-level work, move the whole coupled artifact group into `design/prototypes/archive/` and fix any relative links to shared system assets in the same pass.
- 2026-04-13 — The disabled-controls neutral reads warmer than intended if the border/path tone carries too much charcoal-brown and the text gray leans too close to muted prose. Keeping disabled states convincingly unavailable works better when both tones shift slightly bluer while staying low-chroma and dim.
