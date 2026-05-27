---
status: Active
purpose: Cross-corpus topic-to-file index for the research/ corpus. First stop for "has the team already researched X?"
audience: AI agents and human contributors looking for prior research on a topic
maintenance: Updated as the final step of every `/lifecycle-1-research` skill invocation that produces a new file
---

# research/ — Topic Index

A single-page topic-to-file map across the full `research/` corpus. The sub-area READMEs (`research/language/README.md`, `research/architecture/compiler/README.md`, etc.) carry reading orders and per-folder context; this INDEX answers "where is the research on X?" without requiring an agent to know which sub-folder owns the topic.

If a topic has no research, that's the signal — surface a research-shaped gap to the owner before locking a design against unresearched comparable systems.

**File status discipline.** Every research file declares a `status:` field in frontmatter (Phase 9 convention). Entries below are grouped by topic, not by status; check the file's frontmatter for current status (`Active` / `Cited` / `Promoted to: <link>` / `Active — horizon groundwork` / `Stale` / `Superseded` / `Archived`). The promote-or-cite audit at [`docs/Working/Archive/research-promote-or-cite-audit-2026-05-25.md`](../docs/Working/Archive/research-promote-or-cite-audit-2026-05-25.md) is the 2026-05-25 snapshot.

---

## Money, currency, precision

- [`architecture/compiler/currency-precision-coupling-survey.md`](architecture/compiler/currency-precision-coupling-survey.md) — Joda-Money / JSR-354 / NodaMoney / Stripe / Adyen / IFRS comparator. Grounds Position 3 for F-LANG-BIZ-02.
- [`architecture/compiler/exact-decimal-arithmetic-survey.md`](architecture/compiler/exact-decimal-arithmetic-survey.md) — System.Decimal / BigDecimal / IEEE 754 decimal / SQL DECIMAL arithmetic mechanics.
- [`language/expressiveness/currency-quantity-uom-research.md`](language/expressiveness/currency-quantity-uom-research.md) — money/quantity/uom domain modeling.

## Temporal types (date, time, instant, duration, period, timezone, datetime)

- [`language/expressiveness/temporal-type-strategy.md`](language/expressiveness/temporal-type-strategy.md) — Phase-1/2/3 strategy synthesis. **Cited** from temporal-type-system spec.
- [`language/expressiveness/nodatime-precept-alignment.md`](language/expressiveness/nodatime-precept-alignment.md) — initial NodaTime adoption decision.
- [`language/expressiveness/instant-zoneddatetime-reconsideration.md`](language/expressiveness/instant-zoneddatetime-reconsideration.md) — instant reconsideration; ZonedDateTime deferred.
- [`language/expressiveness/enterprise-timezone-analysis.md`](language/expressiveness/enterprise-timezone-analysis.md) — multi-timezone compliance rule analysis.
- [`language/expressiveness/timezone-type-storability-analysis.md`](language/expressiveness/timezone-type-storability-analysis.md) — `timezone` type adoption.
- [`language/expressiveness/temporal-type-system-proposal-v1.md`](language/expressiveness/temporal-type-system-proposal-v1.md) — v1 proposal (superseded by strategy doc).
- [`language/expressiveness/nodatime-exception-surface-audit.md`](language/expressiveness/nodatime-exception-surface-audit.md) — NodaTime exception inventory.
- [`language/expressiveness/sample-temporal-pattern-catalog.md`](language/expressiveness/sample-temporal-pattern-catalog.md) — 91 temporal markers across 15 samples.
- [`language/expressiveness/native-date-time-literals.md`](language/expressiveness/native-date-time-literals.md) — literal syntax precedent.
- [`language/references/nodatime-type-model.md`](language/references/nodatime-type-model.md) — NodaTime type model reference.
- [`architecture/compiler/temporal-type-hierarchy-survey.md`](architecture/compiler/temporal-type-hierarchy-survey.md) — multi-library temporal hierarchy comparison.

**Known gap (per RS-F4):** `temporal-type-strategy.md` engages NodaTime heavily but lacks survey-grade comparator engagement with Joda-Time / java.time / Python datetime / Rust chrono. A broader survey is a Phase 11 prerequisite for any irreversible temporal-surface decision.

## Quantity, units of measure (UCUM)

- [`architecture/compiler/units-of-measure-dimensional-analysis-survey.md`](architecture/compiler/units-of-measure-dimensional-analysis-survey.md) — dimensional analysis approach.
- [`architecture/compiler/quantity-normalization-design-survey.md`](architecture/compiler/quantity-normalization-design-survey.md) — normalization design.
- [`architecture/compiler/business-units-quantity-normalization-survey.md`](architecture/compiler/business-units-quantity-normalization-survey.md) — business-unit normalization.
- [`language/references/ucum-tier1-curation.md`](language/references/ucum-tier1-curation.md) — UCUM tier-1 catalog source capture (relocated from `language/` in Phase 10).

## Parser architecture

- [`architecture/compiler/parser-combinator-scalability.md`](architecture/compiler/parser-combinator-scalability.md) — Superpower / PEG / ANTLR / recursive descent / Roslyn comparison (relocated from `language/` in Phase 10).
- [`architecture/compiler/compiler-pipeline-architecture-survey.md`](architecture/compiler/compiler-pipeline-architecture-survey.md) — pipeline architecture comparators.
- [`language/expressiveness/superpower-keyword-function-disambiguation.md`](language/expressiveness/superpower-keyword-function-disambiguation.md) — Superpower disambiguation.

## Compilation, intermediate representation, runtime model

- [`architecture/compiler/compilation-result-type-survey.md`](architecture/compiler/compilation-result-type-survey.md) — Compilation artifact shape.
- [`architecture/compiler/compiler-result-to-runtime-survey.md`](architecture/compiler/compiler-result-to-runtime-survey.md) — compile-to-runtime transformation.
- [`architecture/runtime/runtime-evaluator-architecture-survey.md`](architecture/runtime/runtime-evaluator-architecture-survey.md) — 10-system evaluator architecture survey.

## Type system, type inference, typed constants

- [`language/expressiveness/type-system-domain-survey.md`](language/expressiveness/type-system-domain-survey.md) — type system domains across sample corpus.
- [`language/references/type-system-survey.md`](language/references/type-system-survey.md) — type system survey.
- [`language/expressiveness/type-system-follow-ons.md`](language/expressiveness/type-system-follow-ons.md) — follow-on questions.
- [`language/type-checker-research-validation.md`](language/type-checker-research-validation.md) — type checker validation patterns.
- [`architecture/compiler/context-sensitive-literal-typing-survey.md`](architecture/compiler/context-sensitive-literal-typing-survey.md) — context-sensitive literal typing.

## Proof engine, proof discharge

- [`architecture/compiler/proof-engine-interval-arithmetic-survey.md`](architecture/compiler/proof-engine-interval-arithmetic-survey.md) — interval arithmetic strategies.
- [`architecture/compiler/proof-attribution-witness-design-survey.md`](architecture/compiler/proof-attribution-witness-design-survey.md) — proof attribution / witness design.
- [`language/references/static-reasoning-expansion.md`](language/references/static-reasoning-expansion.md) — static reasoning capability expansion.
- [`philosophy/formal-spec-languages-comparators.md`](philosophy/formal-spec-languages-comparators.md) — Alloy / TLA+ / Event-B / Z notation comparison.

## Constraint composition, fluent validation

- [`language/expressiveness/constraint-composition-domain.md`](language/expressiveness/constraint-composition-domain.md) — constraint composition surface.
- [`language/references/constraint-composition.md`](language/references/constraint-composition.md) — constraint composition reference.
- [`language/expressiveness/constraint-language-function-survey.md`](language/expressiveness/constraint-language-function-survey.md) — constraint-language function survey.
- [`language/references/conditional-invariant-survey.md`](language/references/conditional-invariant-survey.md) — conditional invariant patterns.
- [`language/expressiveness/conditional-logic-strategy.md`](language/expressiveness/conditional-logic-strategy.md) — conditional logic strategy.
- [`language/research-conditional-construction.md`](language/research-conditional-construction.md) — conditional construction research.
- [`language/expressiveness/fluent-validation.md`](language/expressiveness/fluent-validation.md) — FluentValidation comparator.
- [`language/expressiveness/fluent-assertions.md`](language/expressiveness/fluent-assertions.md) — FluentAssertions comparator.
- [`language/expressiveness/zod-valibot.md`](language/expressiveness/zod-valibot.md) — Zod / Valibot comparators.
- [`language/references/cel-comparison.md`](language/references/cel-comparison.md) — CEL comparator.

## State machines, state graph, transitions

- [`language/references/state-machine-expressiveness.md`](language/references/state-machine-expressiveness.md) — state-machine expressiveness reference.
- [`language/expressiveness/xstate.md`](language/expressiveness/xstate.md) — XState comparator.
- [`language/expressiveness/transition-shorthand.md`](language/expressiveness/transition-shorthand.md) — transition shorthand options.
- [`architecture/compiler/state-graph-analysis-survey.md`](architecture/compiler/state-graph-analysis-survey.md) — state-graph analysis algorithms.
- [`architecture/compiler/state-machine-runtime-api-survey.md`](architecture/compiler/state-machine-runtime-api-survey.md) — state-machine runtime APIs.

## Events (modeling, hooks, shorthand, ingestion)

- [`language/expressiveness/event-hooks.md`](language/expressiveness/event-hooks.md) — event hook patterns.
- [`language/expressiveness/stateless-events.md`](language/expressiveness/stateless-events.md) — stateless event modeling.
- [`language/expressiveness/event-ingestion-shorthand.md`](language/expressiveness/event-ingestion-shorthand.md) — event-ingestion shorthand.
- [`language/references/multi-event-shorthand.md`](language/references/multi-event-shorthand.md) — multi-event shorthand reference.

## Modifiers, verdict modifiers

- [`language/expressiveness/access-modifier-keyword-unification.md`](language/expressiveness/access-modifier-keyword-unification.md) — Cross-position access-modifier keyword precedent survey across 8 production languages (TypeScript, Kotlin, Rust, Swift, C#, Java, F#, Scala). Grounds F-LANG-GRAPH-04 Decision 5 keyword-unification precedent leg. **Cited** from `docs/Working/field-never-set-diagnostic.md`.
- [`language/expressiveness/modifier-taxonomy-proposal.md`](language/expressiveness/modifier-taxonomy-proposal.md) — modifier taxonomy.
- [`language/expressiveness/structural-lifecycle-modifiers.md`](language/expressiveness/structural-lifecycle-modifiers.md) — structural lifecycle modifiers.
- [`language/expressiveness/milestone-modifier-feasibility.md`](language/expressiveness/milestone-modifier-feasibility.md) — milestone modifier feasibility.
- [`language/expressiveness/verdict-modifiers.md`](language/expressiveness/verdict-modifiers.md) — verdict modifier overview.
- [`language/expressiveness/verdict-modifier-design-options.md`](language/expressiveness/verdict-modifier-design-options.md) — design options.
- [`language/expressiveness/verdict-modifier-roadmap-positioning.md`](language/expressiveness/verdict-modifier-roadmap-positioning.md) — roadmap positioning.
- [`language/expressiveness/verdict-modifier-ux-perspective.md`](language/expressiveness/verdict-modifier-ux-perspective.md) — UX perspective.
- [`language/expressiveness/verdict-modifier-runtime-enforceability.md`](language/expressiveness/verdict-modifier-runtime-enforceability.md) — runtime enforceability.
- [`language/expressiveness/verdict-semantic-reframing.md`](language/expressiveness/verdict-semantic-reframing.md) — semantic reframing.

## Expression language, evaluation, computed fields

- [`language/expressiveness/expression-language-audit.md`](language/expressiveness/expression-language-audit.md) — expression language audit (heavily cross-referenced).
- [`language/references/expression-evaluation.md`](language/references/expression-evaluation.md) — expression evaluation reference (heavily cross-referenced).
- [`language/expressiveness/expression-expansion-domain.md`](language/expressiveness/expression-expansion-domain.md) — expression expansion domains.
- [`language/expressiveness/expression-tracking-notes.md`](language/expressiveness/expression-tracking-notes.md) — expression tracking notes.
- [`language/references/expression-compactness.md`](language/references/expression-compactness.md) — expression compactness reference.
- [`language/expressiveness/computed-fields.md`](language/expressiveness/computed-fields.md) — computed fields (heavily cross-referenced).
- [`language/expressiveness/function-library-comparison.md`](language/expressiveness/function-library-comparison.md) — function library comparison.
- [`language/expressiveness/low-code-function-patterns.md`](language/expressiveness/low-code-function-patterns.md) — low-code function patterns.

## IntelliSense, discoverability, dot access

- [`language/expressiveness/intellisense-discoverability-ux.md`](language/expressiveness/intellisense-discoverability-ux.md) — dot access vs function catalogs UX research.
- [`language/expressiveness/dot-access-vs-function-precedent.md`](language/expressiveness/dot-access-vs-function-precedent.md) — dot access precedent.
- [`language/expressiveness/keyword-clarity-audit.md`](language/expressiveness/keyword-clarity-audit.md) — keyword clarity audit.

## String operations (ordering, case insensitivity)

- [`language/expressiveness/string-ordering-gap-analysis.md`](language/expressiveness/string-ordering-gap-analysis.md) — initial gap analysis.
- [`language/expressiveness/string-ordering-external-survey.md`](language/expressiveness/string-ordering-external-survey.md) — external survey.
- [`language/expressiveness/business-string-ordering-use-cases.md`](language/expressiveness/business-string-ordering-use-cases.md) — business use cases.
- [`language/expressiveness/string-ordering-broad-use-cases.md`](language/expressiveness/string-ordering-broad-use-cases.md) — broader use-case analysis.
- [`language/expressiveness/string-ordering-vs-ordered-choice.md`](language/expressiveness/string-ordering-vs-ordered-choice.md) — vs ordered-choice analysis.
- [`language/expressiveness/string-ordering-architectural-analysis.md`](language/expressiveness/string-ordering-architectural-analysis.md) — architectural analysis. **Grounds primitive-types.md § String Ordering — Out of Scope.**
- [`language/expressiveness/case-insensitive-comparison-survey.md`](language/expressiveness/case-insensitive-comparison-survey.md) — case-insensitive comparison operator survey.
- [`language/expressiveness/case-insensitive-implementation-survey.md`](language/expressiveness/case-insensitive-implementation-survey.md) — case-insensitive implementation survey.

## Polly (resilience patterns), LINQ comparator

- [`language/expressiveness/polly.md`](language/expressiveness/polly.md) — Polly comparator.
- [`language/expressiveness/linq.md`](language/expressiveness/linq.md) — LINQ comparator.

## Diagnostics, output design, dry-run / preview / inspect

- [`architecture/compiler/diagnostic-and-output-design-survey.md`](architecture/compiler/diagnostic-and-output-design-survey.md) — diagnostic output design.
- [`architecture/compiler/outcome-type-taxonomy-survey.md`](architecture/compiler/outcome-type-taxonomy-survey.md) — outcome type taxonomy.
- [`architecture/compiler/dry-run-preview-inspect-api-survey.md`](architecture/compiler/dry-run-preview-inspect-api-survey.md) — dry-run / preview / inspect API patterns.

## Language server, MCP tooling

- [`architecture/compiler/language-server-integration-survey.md`](architecture/compiler/language-server-integration-survey.md) — LSP integration patterns.
- [`architecture/tooling/precept-language-mcp-audit.md`](architecture/tooling/precept-language-mcp-audit.md) — MCP tool audit (relocated from `language/` in Phase 10).
- [`architecture/tooling/precept-language-tool-architecture.md`](architecture/tooling/precept-language-tool-architecture.md) — tool architecture audit (relocated from `language/` in Phase 10).

## Entity modeling, domain integrity, governance

- [`language/expressiveness/entity-modeling-surface.md`](language/expressiveness/entity-modeling-surface.md) — entity modeling surface.
- [`language/expressiveness/data-only-precepts-research.md`](language/expressiveness/data-only-precepts-research.md) — stateless precept research.
- [`language/references/governance-vs-validation.md`](language/references/governance-vs-validation.md) — governance vs validation distinction.
- [`philosophy/domain-integrity-formal-concept.md`](philosophy/domain-integrity-formal-concept.md) — domain integrity formal concept (C.J. Date / Fowler / Greg Young).
- [`philosophy/entity-first-positioning-evidence.md`](philosophy/entity-first-positioning-evidence.md) — entity-first positioning evidence.
- [`product/entity-governance-landscape.md`](product/entity-governance-landscape.md) — entity-governance landscape.

## Domain mapping, research strategy

- [`language/domain-map.md`](language/domain-map.md) — domain-to-research map.
- [`language/domain-research-batches.md`](language/domain-research-batches.md) — research batch plan.

## Internal verbosity, language compactness

- [`language/expressiveness/internal-verbosity-analysis.md`](language/expressiveness/internal-verbosity-analysis.md) — internal verbosity analysis.

## Philosophy, positioning

- [`philosophy/philosophy-refresh-assessment.md`](philosophy/philosophy-refresh-assessment.md) — philosophy refresh assessment (relocated from `language/` in Phase 10).
- [`product/data-vs-state-pm-research.md`](product/data-vs-state-pm-research.md) — PM perspective on data-first vs state-first positioning.
- [`product/readme-research-steinbrenner.md`](product/readme-research-steinbrenner.md) — README adoption research.

## Security

- [`security/security-survey.md`](security/security-survey.md) — security landscape; OWASP / SLSA / supply chain.

---

## Out of scope for this INDEX

Brand and design-system research are filed under `research/brand/` and `research/design-system/` for historical reasons, but **per the `lifecycle-1-research` skill they belong in `design/brand/research/` and `design/system/research/`** respectively. The audit at [`docs/Working/Archive/research-promote-or-cite-audit-2026-05-25.md`](../docs/Working/Archive/research-promote-or-cite-audit-2026-05-25.md) flagged 16 files in these folders as systematically uncited; their relocation or archival is owner-judgment work outside Phase 10's scope. This INDEX deliberately doesn't list them.

---

## Maintenance

The `/lifecycle-1-research` skill's final step on every new research file requires updating this INDEX to add the new file under its topic. If a new topic doesn't exist, the author creates a new `## Topic name` section in the appropriate position (alphabetical-ish, but grouped by domain affinity — temporal types near other type-system topics, etc.).

If an existing file is renamed, relocated, or archived, the INDEX entry is updated in the same change-set.

Periodic audits (`/lifecycle-7-audit` when shipped) verify the INDEX still maps every research file. Files in `research/archive/` are NOT listed here — they're historical record.
