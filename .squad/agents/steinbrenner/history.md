# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. Feature-complete on core language and runtime (Phase 4c complete).
- **Stack:** C# / .NET 10.0, TypeScript, DSL
- **My domain:** Roadmap (`docs/@ToDo.md`), release planning, issue triage, milestone tracking
- **Pending roadmap items:** CLI implementation, fluent interface, same-preposition contradiction detection, cross-preposition deadlock detection, structured violations in preview protocol
- **Completed phases:** Core DSL (A-D), type checking, constraint violations, syntax highlighting, MCP redesign (Phase 9)
- **Distribution:** NuGet, VS Code Marketplace, Claude Marketplace
- **Created:** 2026-04-04

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-05-01 — DSL expressiveness research (6 libraries)

- **Libraries studied:** FluentAssertions, Zod/Valibot, xstate, Polly, FluentValidation, LINQ.
- **Files created:** `docs/research/dsl-expressiveness/{fluent-assertions,zod-valibot,xstate,polly,fluent-validation,linq}.md` + README.
- **Top gap — named guards:** xstate and Polly both use named/reusable conditions. Precept requires re-inlining complex `when` expressions (e.g., the 5-condition loan eligibility guard) in every transition row that uses them. Proposal: `guard LoanEligible when ...` declaration form.
- **Top gap — ternary in set:** LINQ and every mainstream language support inline conditional value selection. Precept requires two full transition rows when only a mutated value differs. Confirmed this pattern appears in hiring-pipeline, trafficlight, and subscription samples. Proposal: `-> set Status = isUrgent ? "Priority" : "Standard"`.
- **Top gap — string .length:** Zod and FluentValidation have first-class string length validation. Precept has no string length accessor. Proposal: add `.length` mirroring `.count` on collections.
- **Precept's advantages confirmed:** Cross-field `invariant` is more concise than Zod `.refine()` or FluentValidation `.Equal(x => x.OtherProp)`. State-scoped `in <State> assert` has no equivalent in any studied library — this is the strongest hero-sample differentiator.
- **Hero implication:** Business-rule invariants show Precept's conciseness advantage; format-guard invariants show relative verbosity vs. Zod. Hero must feature `in <State> assert` prominently.
- **Decision inbox:** `.squad/decisions/inbox/steinbrenner-dsl-research.md` — three proposals ready for George/team review.

### 2026-04-04 — Language spec deep dive (language-spec-brief.md)

- **Core runtime is genuinely feature-complete.** Parser, compiler, runtime engine, MCP 5-tool surface, Copilot plugin + skills, and 21 canonical sample files all exist and are tested (666 tests). Phase 4c is a real boundary — the core contract is built.
- **Syntax highlighting palette is the single most important unstarted work item.** The 8-shade semantic palette is locked in brand spec, design doc, and implementation plan. Zero phases implemented. The extension currently inherits VS Code theme colors. This undercuts the entire "color encodes meaning" value proposition and should be treated as a v1 release gate.
- **RulesDesign.md is a documentation liability.** It describes a `rule` keyword with indented syntax and type-prefix field declarations that do not match the current language surface (`invariant`/`assert`). Status says "Implemented" but no sample file uses `rule`. This doc misleads readers and AI agents. Archive or rewrite.
- **Contradiction/deadlock detection is spec-promised but unimplemented.** PreceptLanguageDesign.md § State Asserts marks same-preposition contradiction (Check #4) and cross-preposition deadlock (Check #5) as compile-time **errors** — but neither is implemented. This is a false promise in the spec.
- **Preview protocol is inconsistently structured.** The runtime returns rich `ConstraintViolation` objects; the preview webview still receives flat strings. The inspector can't do field-level inline highlighting even though the runtime has been ready since the constraint violation redesign.
- **CLI decision is overdue.** The CLI design exists but is deferred. MCP covers the same workflows. A decision is needed: implement or kill.
- **The DSL's architectural coherence is strong.** Flat keyword-anchored syntax, deterministic semantics, compile-time-first posture, and AI-first design principles are consistently applied across the language and toolchain. No major design debt in the core language surface.

### 2026-03-27 — Deep language spec review

- **The language is feature-complete.** All core constructs implemented: flat keyword-anchored syntax, four assert kinds, state actions, first-match transitions, editable fields, comprehensive type checking (Phases A–H), graph analysis (Phase I). Parser (Superpower), compiler, runtime, language server, extension, and Copilot plugin all delivered. 20 sample files provide canonical reference.
- **Type safety is a strategic moat.** Compile-time strictness (equality rules Phase D, scope hardening Phase E, non-boolean rejection Phase F, duplicate guards Phase G) prevents author bugs before runtime. This + structured violation model (Phase CV 4–7) + graph analysis (Phase I) = comprehensive semantic checking that differentiates from simpler DSLs.
- **Constraint violations now structured, not flat.** Runtime returns `ConstraintViolation(Source: Invariant|StateAssertion|EventAssertion|TransitionRejection, Targets: Field|EventArg|Event|State|Definition)`. Preview UI and CLI can now attribute violations precisely (inline for field targets, banner for scope), not guess via string matching. This bridges compile-time correctness to author-friendly feedback.
- **Distribution strategy clarified.** Precept is ready to ship: language complete, tooling complete, samples complete. What's pending: CLI implementation (design exists), hero example (language ready but no marketing vehicle yet), adoption stories (where does Precept fit in real workflows?). These are delivery/marketing work, not language/runtime work.

### 2026 — Hero example analysis (TimeMachine candidate I)

- **The hero's job is proof, not education.** It must demonstrate `invariant`, `when`, and `reject` — the three features that prove "invalid states structurally impossible." If any of those are missing, the hero fails its one job.
- **TimeMachine (I) is weak on all three:** no `invariant`, no `when` guard, no `reject`. It also violates brand voice ("Serious. No jokes.") with pop culture asserts.
- **15 lines is the correct hard cap** for a README hero. Both shortlisted candidates (B′ ParkingMeter, H′ TrafficLight) confirm this is achievable with 10+ features.
- **Domain matters as much as syntax.** The developer must be able to project themselves into the domain. Fantasy domains (TimeMachine) block that transfer. Real workflows (subscription, service ticket, shipment) enable it.
- **Gold `because` messages are the only copy that breathes** in the hero. They must sound like a domain expert, not a comedian. One wry edge is fine if the domain earns it; pop culture references are not.
- **Multi-step transition bodies must be line-broken.** Cramming `-> set -> set -> transition` on one line defeats the top-to-bottom scanability argument that is the product's authoring story.
- **Spec delivered to:** `.squad/decisions/inbox/steinbrenner-hero-example-spec.md` — ready for J. Peterman.

### 2026-05-01 — Rubric v2 negotiated with J. Peterman

- **Rubric v2 finalized at:** `.squad/agents/hero-deliberation-rubric-v2.md` — pending Shane ratification.
- **Decision record at:** `.squad/decisions/inbox/rubric-v2-decision.md`
- **PM wins:** DSL Coverage at 8 points (highest single weight) with a hard floor of ≥4. Any candidate scoring below 4 on Coverage is disqualified regardless of other scores. Line Economy converted to a pass/fail gate — no scored points.
- **PM concessions:** Voice/Wit raised from 3 to 6 (Peterman's non-negotiable). Emotional Hook tiebreaker — when two candidates tie on total, Hook decides, not Differentiation.
- **Final weights:** DSL Coverage 8, Precept Differentiation 6, Domain Legibility 5, Voice/Wit 6, Emotional Hook 5. Line Economy = gate. Total: 30.
- **Key insight from negotiation:** Floors are better than inflated weights for non-negotiable requirements. "You can't buy your way out of it" is a cleaner rule than piling on points.

### 2026-04-04 — Hero domain verdict finalized, user override received

- **Hero brief finalized at:** `.squad/agents/steinbrenner/hero-sample-brief.md` — the definitive reference for execution.
- **Domain verdict:** Subscription wins. Scored all five candidate domains (TimeMachine, ServiceTicket, Subscription, Shipment, Loan) against five criteria (recognizability, seriousness, richness, compactness, transition density). TimeMachine scored 1/5 and was initially disqualified. Loan ruled out — canonical 35-line sample already exists. Shipment ruled out on compactness (too many bootstrap fields). Subscription and ServiceTicket both scored 5/5; Subscription wins on universality: Trial→Active→Suspended→Cancelled is legible to any backend developer with no context required.
- **User override received:** User approved TimeMachine as hero candidate (J. Peterman reworked snippet to 18 lines, full feature coverage, clean compile). User confirmed jokes in `because` messages are appropriate for hero. Brand voice updated to permit fun/pop-culture domain. **Conflict:** User preference (TimeMachine) vs. Spec verdict (Subscription). Team decision required to close.
- **J. Peterman's improved TimeMachine:** 3 states (Parked → Accelerating → TimeTraveling), invariant on Speed, dual event asserts, when guard (88mph + 1.21GW), reject with BTTF subversion, clean compile. Candidate promoted to shortlist.
- **Three non-negotiables confirmed:** `invariant` on a business fact (not a technical constraint), `reject` on a blocked path that's obviously correct, `when` guard demonstrating conditional engine reasoning. All three must appear or the hero fails its job.
- **Line budget locked: 15 lines hard cap.** Multi-step transition bodies must be line-broken — one `->` per line.
- **`because` messages are brand copy.** They must sound like a domain expert, not a programmer. Wrong: generic errors. Right: operational facts (or wry domain wit if the domain earns it). Jokes are permitted if the domain is legible (BTTF physics constraints) rather than abstract.

### 2026-05-01 — Rubric v2 revised: weighted 0–10 scale (Shane directive, round 2)

- **Directive 1:** Each criterion now scored 0–10. Weights (multipliers) replace variable max-points. Total max = 100 (weights sum to 10.0).
- **Directive 2:** Brand criteria combined weight (4.5×) now exceeds PM criteria combined weight (3.5×). Shane's call — the hero sample is a brand artifact first.
- **PM outcome:** Held DSL Coverage hard floor (raw ≥ 4/10). The floor is the real enforcement; DSL Coverage does not need the top weight if a candidate below raw 4 is automatically disqualified regardless of score.
- **Weight conceded:** DSL Coverage dropped from top-weight position to 2.0× (tied with Hook and Legibility). Voice/Wit takes the top weight at 2.5×.
- **PM held:** DSL Coverage 2.0× and Precept Differentiation 1.5× — PM combined 35 points max. Domain Legibility at 2.0× (neutral). Line Economy remains a gate.
- **Final weights:** Voice/Wit 2.5×, Emotional Hook 2.0×, DSL Coverage 2.0×, Domain Legibility 2.0×, Precept Differentiation 1.5×. Line Economy = gate.
- **Passing threshold:** ≥ 65/100 (structural problems below this), ≥ 80/100 for hero placement.
- **Decision record:** `.squad/decisions/inbox/rubric-v2-decision.md` (updated)

### 2026-05-01 — Rubric v2 reframed: direct point scores (Shane directive, round 3)

- **Table restructure:** Removed Weight and Max Raw Score columns. Single "Max Points" column now directly encodes the scoring scale (0–N, not 0–10 scaled by multiplier).
- **New thresholds scaled proportionally:** DSL Coverage threshold raised from raw ≥ 4/10 to ≥ 8/25 (same 40% floor). Domain Legibility threshold raised from ≥ 3/10 to ≥ 6/20 (30% floor). Soft floor for Differentiation remains relative.
- **Line Economy gate updated:** Threshold description now reads "Statement count gate — to be updated once Peterman's research lands" (Peterman researching correct statement count range).
- **Scoring simplified:** Scorers award points directly from 0 to each criterion's max. No multiplication — just addition. Example recalculated: Voice/Wit 20 + Emotional Hook 14 + DSL Coverage 18 + Domain Legibility 16 + Differentiation 14 = 82 (same effective score, zero arithmetic burden).
- **Pedagogical improvement:** Direct-scoring removes a cognitive layer for both scorers and readers. The max-points column now visibly encodes the importance ranking: Wit 25 > Hook/Coverage/Legibility 20 > Differentiation 15.
- **All decision records updated:** `.squad/agents/hero-deliberation-rubric-v2.md` (rubric table + "How to Score" + example), `.squad/agents/steinbrenner/history.md` (this entry).

### 2026-05-01 — Advisory fix: PRECEPT050/051 in Candidates 1 and 3

- **PRECEPT050/051 fires when `reject` is the only behavior in a terminal state.** The compiler correctly identifies it as redundant: `Undefined` is already returned for unhandled (state, event) pairs. Explicit `reject` in a terminal state adds no value — the system is already stuck.
- **Fix pattern: relocate `reject` to a non-terminal state with a conditional guard.** The key is that the (state, event) pair must have at least one row that ends in a non-reject outcome (transition or no-transition). This makes `reject` a meaningful enforcement, not dead code.
- **Candidate 1:** Moved `reject` from `Cancelled` to `Active` as a downgrade guard — `Activate` now splits on `Activate.Price >= MonthlyPrice`. Score unchanged (29/30); all six DSL constructs preserved.
- **Candidate 3:** Moved `reject` from `Merged` to `Review` as a premature-merge guard — `Merge` from `Review` splits on `ApprovalCount >= 2`. Score unchanged (25/30); also adds a short-circuit merge path (Review → Merged when threshold met).
- **No scores were lost.** Both fixes preserved all rubric constructs. The "reject" pedagogical intent survived — it just moved to a context where it teaches the conditional-gate pattern rather than the structural-fence pattern.

### 2026-04-04 — Phase 1 DSL Expressiveness Research

- **Deliverable:** `docs/research/dsl-expressiveness/` — 6 library comparison files + README (FluentAssertions, Zod/Valibot, xstate, Polly, FluentValidation, LINQ)
- **Top 3 language proposals identified:**
  1. **Named Guard Declarations (HIGH)** — xstate and Polly both use reusable conditions. Precept requires full re-inline. Proposal: `guard LoanEligible when ...` form.
  2. **Ternary in Set Mutations (HIGH)** — LINQ and all mainstream languages support inline conditional value selection. Confirmed pattern in hiring-pipeline, trafficlight, subscription samples.
  3. **String `.length` Accessor (MEDIUM)** — Zod and FluentValidation have first-class string validation. Precept has `.count` on collections but nothing on strings.
- **Strategic finding:** State-scoped `in <State> assert` has no equivalent in any studied library — this is Precept's strongest hero-sample differentiator and must appear prominently in hero candidate.
- **Decision inbox filed:** Three proposals ready for team review and design gating.
