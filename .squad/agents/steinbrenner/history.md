## Core Context

- Owns PM framing, roadmap sequencing, proposal structure, and reviewer-facing positioning across the Squad workflow.
- Keeps proposal work tied to philosophy fit, durable taxonomy, and clear implementation/review sequencing.
- Historical summary (pre-2026-04-13): shaped language-research batching, proposal/body standards, expressiveness vs compactness tagging, named-rule positioning, and PM analysis for event hooks and related roadmap decisions.
- Owns PM briefs, hero-evaluation rubrics, README ship planning, and cross-agent sequencing.
- Hero decisions are judged on recognizability, feature density, line budget, and adoption clarity; once a temporary domain is chosen, downstream work should execute without reopening the selection casually.
- README delivery is a gated sequence: proposal/spec first, then rewrite, review, and final sign-off.

## Recent Updates

### 2026-04-15 — Low-code function-call vs dot-access research delivered
- Completed external survey of 8+ platforms across Shane's 6 requested categories (spreadsheets, low-code, BRMS, workflow/automation, database/query, Power Fx).
- **Key finding:** The low-code landscape is NOT uniformly function-based — it's a four-position spectrum from pure function (Excel, Power Fx, Power Automate) through hybrid properties+functions (FEEL, Precept today) to hybrid both-forms (Notion 2.0) to heavy dot-method (Coda).
- **Strongest precedent for dot methods on temporal values:** Coda's `Time(1,30,45).DateTimeTruncate("minute")` — directly targeting non-developers.
- **Strongest precedent for Precept's current hybrid:** FEEL (DMN) — dot for zero-arg properties (`date.year`, `time.hour`), functions for parameterized ops (`day of week(date)`). An OMG standard for business analysts.
- **No platform uses `value.inZone(tz)` style for timezone conversion.** All use function form: Power Automate's `convertFromUtc(ts, tz)`.
- **PM recommendation:** `inZone(instant, tz)` (function form) — matches majority pattern, matches Precept's audience's primary tools, preserves clean FEEL-style hybrid boundary. Dot-method form is defensible minority bet but breaks the current boundary.
- Filed research to `research/language/expressiveness/low-code-function-patterns.md` and decision to `.squad/decisions/inbox/steinbrenner-low-code-research.md`.
- Key learning: Power Fx is the strongest comparable system (same audience, same philosophy) and explicitly rejected dot methods with stated reasoning: "Excel isn't object-oriented, and neither is Power Fx." This is the most authoritative external precedent for the function-form choice.
- Key learning: The assumption "only developers use dot methods" is FALSE — Notion 2.0 and Coda both target non-developers and use dot methods extensively. But they are the minority among low-code platforms, and Precept's audience is more likely to come from Excel/Power Fx than from Coda/Notion.

### 2026-04-11 - Named rule keyword confusion analysis delivered
- Completed PM/UX analysis of Shane's concern about Issue #8: `rule LoanEligible when ...` creates a passive predicate using a keyword that currently means "auto-enforced constraint." This overload fails 3 of 6 philosophy checks (prevention, keyword-anchored clarity, AI legibility).
- Surveyed 6 comparable tools: xstate (`guard` — not auto-enforced), FluentValidation (`RuleSet` — opt-in), Drools (`rule` — auto-enforced, but with action blocks), Alloy (`pred` — must be used in fact/assert), OCL (`let` — binding only), FHIR (`constraint` — auto-enforced). No comparable tool uses the same keyword for both auto-enforced constraints and passive reusable predicates.
- PM recommendation: rename the #8 construct to `guard <Name> when <Expr>`. This preserves `rule` for enforcement, aligns with xstate precedent, and passes all 6 philosophy checks. Decision recommendation filed to `.squad/decisions/inbox/steinbrenner-named-rule-keyword-confusion.md`.
- Key learning: keyword overloading in a prevention-first language is more dangerous than in a validation-first language — users trust that every construct they see is actively protecting them. A passive construct wearing enforcement clothing undermines that trust silently.
### 2026-04-12 — Event hooks PM motivation and use case analysis
- Built use-case inventory for event-level action hooks from the 24-sample corpus.
- Confirmed real friction: repeated `RegisterAgent` calls in `it-helpdesk-ticket.precept` across 4 identical rows; TrafficLight counter requires duplication across all non-reject Advance rows.
- **PM recommendation: two-proposal split.** Issue A (stateless) advances first — zero Principle 7 tension, clean execution order. Issue B (stateful) deferred — unresolved execution order (4 options with different semantics) and outcome-scoping question.
- **C49 revision confirmed in-scope** for Issue A — not optional follow-up. Events with hooks must suppress C49; ships in same PR as runtime/grammar changes.
- Drafted acceptance criteria for Issue A (stateless only). Filed at `.squad/decisions/inbox/steinbrenner-event-hooks-pm.md` (now merged to decisions.md).



### 2026-04-08 - Language research plan fully executed
- The three-batch domain-first plan is complete on `squad/language-research-corpus`, closed by `3cc5343` after Batch 1 `54a77da` and Batch 2 `48860ae`.
- PM guardrails held through closeout: no proposal-body edits, horizon domains remained represented, and the final indexes point active proposals back to their grounding research.

### 2026-04-08 - Language research batching finalized
- Finished `docs/research/language/domain-research-batches.md` as the domain-first execution plan for the corpus.
- Regrouped Batch 1 so constraint composition stays with the rest of the validator/rule/declaration lane instead of being split into a later pass.
- Preserved the session rules: no proposal-body edits during corpus work, horizon domains stay visible, and each completed batch closes with its own commit (`54a77da` for Batch 1, `48860ae` for Batch 2). Batch 3 and the final README/index sweep remain open.

### 2026-04-05 - Proposal #8 finalized around named rules
- Synced the roadmap framing to rule <Name> when <BoolExpr>, locked the field-only/boolean-only boundaries, and recorded the issue rename to "Proposal: Named rule declarations."
- PM proposal guidance now requires philosophy fit, non-goals, and the configuration-like readability check on future language work.

### 2026-04-05 - Expressiveness proposal label locked for the next wave
- Created the `dsl-expressiveness` repository label and applied it to the expression-focused proposal issues #8, #9, and #10.
- Added `docs/research/dsl-expressiveness/expression-tracking-notes.md` so the team has one repo-local definition of what belongs under the tag and how it differs from `dsl-compactness`.

### 2026-04-05 - Compactness proposal label standardized on GitHub issues
- Created the `dsl-compactness` repository label and applied it to language improvement proposal issues #8, #9, #10, #11, #12, and #13.
- Verified the label now sits alongside `squad:frank` on all six proposals, giving the roadmap a durable compactness-focused slice across the language queue.

### 2026-04-05 - Proposal bodies expanded for issues #8-#10
- Expanded GitHub issues #8, #9, and #10 into a shared proposal format covering motivation, Precept-today pain, hypothetical syntax, reference-language snippets, benefits, and open questions.
- Reinforced the PM guardrail that hypothetical DSL examples in roadmap issues must be labeled as unimplemented behavior.

### 2026-04-05 - Freeze-and-curate cutover became the safe team path
- Proposed freezing the exact feature SHA, cutting a fresh integration branch from 'main', and re-landing approved content as curated commits.
- Uncle Leo's review ratified that sequence as the only approved trunk-return pattern, so PM sequencing now assumes curation, validation gates, and post-cutover cleanup.

## Learnings

- Prevention-first languages cannot casually overload enforcement-shaped keywords for passive constructs.
- Proposal issues land best with one durable structure and clearly labeled hypothetical syntax.
- Roadmap labels and workflow metadata are most useful when taxonomy, ownership, and exceptions stay separate.

## Recent Updates

### 2026-04-12 — Event hooks PM analysis
- Recommended a two-proposal split: advance stateless event hooks first and defer stateful hooks until execution-order and scope questions are resolved.

### 2026-04-11 — Named rule keyword analysis
- Recommended avoiding `rule` for passive named predicates because it silently implies enforcement in a prevention-first product.
