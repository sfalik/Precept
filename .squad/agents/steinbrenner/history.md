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

### 2026-04-04 — Hero domain verdict finalized, user override received

- **Hero brief finalized at:** `.squad/agents/steinbrenner/hero-sample-brief.md` — the definitive reference for execution.
- **Domain verdict:** Subscription wins. Scored all five candidate domains (TimeMachine, ServiceTicket, Subscription, Shipment, Loan) against five criteria (recognizability, seriousness, richness, compactness, transition density). TimeMachine scored 1/5 and was initially disqualified. Loan ruled out — canonical 35-line sample already exists. Shipment ruled out on compactness (too many bootstrap fields). Subscription and ServiceTicket both scored 5/5; Subscription wins on universality: Trial→Active→Suspended→Cancelled is legible to any backend developer with no context required.
- **User override received:** User approved TimeMachine as hero candidate (J. Peterman reworked snippet to 18 lines, full feature coverage, clean compile). User confirmed jokes in `because` messages are appropriate for hero. Brand voice updated to permit fun/pop-culture domain. **Conflict:** User preference (TimeMachine) vs. Spec verdict (Subscription). Team decision required to close.
- **J. Peterman's improved TimeMachine:** 3 states (Parked → Accelerating → TimeTraveling), invariant on Speed, dual event asserts, when guard (88mph + 1.21GW), reject with BTTF subversion, clean compile. Candidate promoted to shortlist.
- **Three non-negotiables confirmed:** `invariant` on a business fact (not a technical constraint), `reject` on a blocked path that's obviously correct, `when` guard demonstrating conditional engine reasoning. All three must appear or the hero fails its job.
- **Line budget locked: 15 lines hard cap.** Multi-step transition bodies must be line-broken — one `->` per line.
- **`because` messages are brand copy.** They must sound like a domain expert, not a programmer. Wrong: generic errors. Right: operational facts (or wry domain wit if the domain earns it). Jokes are permitted if the domain is legible (BTTF physics constraints) rather than abstract.
