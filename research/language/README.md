# Language Research

This folder is the durable, domain-first research map for Precept language work.

- **Research stays here** — precedent, rationale, philosophy fit, dead ends, and domain synthesis.
- **Proposal bodies stay in GitHub issues** — scope, status, acceptance criteria, and review discussion.
- **The corpus is organized by domain first** so future proposals can start from evidence instead of rebuilding context.

## Start here

| If you need... | Start with |
|---|---|
| The full domain map | [domain-map.md](./domain-map.md) |
| Research execution order | [domain-research-batches.md](./domain-research-batches.md) |
| Domain packets and comparative research | [expressiveness/README.md](./expressiveness/README.md) |
| Theory and formal reference work | [references/README.md](./references/README.md) |
| Parser / grammar scalability analysis | [parser-combinator-scalability.md](./parser-combinator-scalability.md) |

## Domain index

| Domain | Open proposals | Primary grounding | Theory / reference companion | Notes |
|---|---|---|---|---|
| Type system expansion | `#25`, `#26`, `#27`, `#29` | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md) | [references/type-system-survey.md](./references/type-system-survey.md) | Core active type lane. |
| Event ingestion shorthand | `#11` | [expressiveness/event-ingestion-shorthand.md](./expressiveness/event-ingestion-shorthand.md) | [references/expression-compactness.md](./references/expression-compactness.md) | Compactness lane; research says keep it narrow and audit-friendly. |
| Constraint composition | `#8`, `#13`, `#14` | [expressiveness/constraint-composition-domain.md](./expressiveness/constraint-composition-domain.md) | [references/constraint-composition.md](./references/constraint-composition.md), [references/conditional-invariant-survey.md](./references/conditional-invariant-survey.md) | Shared validator / rule / declaration domain. |
| Expression expansion | `#9`, `#10`, `#15`, `#16`, `#31` | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md) | [references/expression-evaluation.md](./references/expression-evaluation.md) | One vocabulary lane for conditionals, string operations, functions, and logical keywords. |
| Entity-modeling surface | `#17`, `#22` | [expressiveness/entity-modeling-surface.md](./expressiveness/entity-modeling-surface.md), [expressiveness/computed-fields.md](./expressiveness/computed-fields.md), [expressiveness/data-only-precepts-research.md](./expressiveness/data-only-precepts-research.md) | [references/expression-evaluation.md](./references/expression-evaluation.md) | Governs stored-vs-derived facts and lifecycle-light entities. |
| Transition shorthand | — | [expressiveness/transition-shorthand.md](./expressiveness/transition-shorthand.md) | [references/multi-event-shorthand.md](./references/multi-event-shorthand.md), [references/state-machine-expressiveness.md](./references/state-machine-expressiveness.md) | Horizon domain with no open proposal yet. |
| Static reasoning expansion | `#106` (implemented, PR #108) | [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md) | [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md) | Implemented via local proof engine in PR #108 (compile-time divisor safety, unified interval + relational inference). |
| Structural lifecycle modifiers | — | [expressiveness/structural-lifecycle-modifiers.md](./expressiveness/structural-lifecycle-modifiers.md) | [references/state-machine-expressiveness.md](./references/state-machine-expressiveness.md), [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md) | Horizon domain. `terminal` is the strong Tier 1 candidate; `required`/`transient` are Tier 2. |
| Type-system follow-ons | — | [expressiveness/type-system-follow-ons.md](./expressiveness/type-system-follow-ons.md) | [references/type-system-survey.md](./references/type-system-survey.md) | Residual type pressure after the main type wave; no active proposal yet. |
| Event action hooks | `#65` | [expressiveness/event-hooks.md](./expressiveness/event-hooks.md) | [references/state-machine-expressiveness.md](./references/state-machine-expressiveness.md) | Two-case split: stateless (Issue A, #65, ready) and stateful (Issue B, deferred). Issue A has zero Principle 7 tension. |
| Stateless events | `#112` | [expressiveness/stateless-events.md](./expressiveness/stateless-events.md) | [expressiveness/data-only-precepts-research.md](./expressiveness/data-only-precepts-research.md), [expressiveness/event-hooks.md](./expressiveness/event-hooks.md) | `on EventName` mutation surface for stateless precepts. Extends #22 (data-only precepts) with event-driven mutation. |

## Open proposal issue map

Use this table when you already know the issue number and need its research grounding.

| Issue | Focus | Domain | Research grounding |
|---|---|---|---|
| `#8` | Named rule declarations | Constraint composition | [expressiveness/constraint-composition-domain.md](./expressiveness/constraint-composition-domain.md), [references/constraint-composition.md](./references/constraint-composition.md) |
| `#9` | Conditional expressions in value positions | Expression expansion | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |
| `#10` | String `.length` accessor | Expression expansion | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |
| `#11` | Event argument absorb shorthand | Event ingestion shorthand | [expressiveness/event-ingestion-shorthand.md](./expressiveness/event-ingestion-shorthand.md), [references/expression-compactness.md](./references/expression-compactness.md) |
| `#13` | Field-level range / basic constraints | Constraint composition | [expressiveness/constraint-composition-domain.md](./expressiveness/constraint-composition-domain.md), [references/constraint-composition.md](./references/constraint-composition.md) |
| `#14` | Conditional invariants / guarded declarations | Constraint composition | [expressiveness/constraint-composition-domain.md](./expressiveness/constraint-composition-domain.md), [references/constraint-composition.md](./references/constraint-composition.md), [references/conditional-invariant-survey.md](./references/conditional-invariant-survey.md) |
| `#15` | String `.contains()` | Expression expansion | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |
| `#16` | Built-in function library | Expression expansion | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |
| `#17` | Computed / derived fields | Entity-modeling surface | [expressiveness/entity-modeling-surface.md](./expressiveness/entity-modeling-surface.md), [expressiveness/computed-fields.md](./expressiveness/computed-fields.md), [expressiveness/nullable-computed-fields-research.md](./expressiveness/nullable-computed-fields-research.md), [expressiveness/null-elimination-research.md](./expressiveness/null-elimination-research.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |
| `#22` | Data-only precepts | Entity-modeling surface | [expressiveness/entity-modeling-surface.md](./expressiveness/entity-modeling-surface.md), [expressiveness/data-only-precepts-research.md](./expressiveness/data-only-precepts-research.md) |
| `#25` | `choice` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#26` | `date` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#27` | `decimal` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#29` | `integer` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#31` | Logical keyword forms (`and` / `or` / `not`) | Expression expansion | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |
| `#65` | Event action hooks (stateless) | Event action hooks | [expressiveness/event-hooks.md](./expressiveness/event-hooks.md), [references/state-machine-expressiveness.md](./references/state-machine-expressiveness.md) |
| `#106` | Compile-time divisor safety (local proof engine) | Static reasoning expansion | [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md) |
| `#112` | Stateless events — `on EventName` mutation surface | Stateless events | [expressiveness/stateless-events.md](./expressiveness/stateless-events.md), [expressiveness/data-only-precepts-research.md](./expressiveness/data-only-precepts-research.md) |

## Cross-cutting research that supports multiple domains

| File | Why it matters |
|---|---|
| [expressiveness/expression-language-audit.md](./expressiveness/expression-language-audit.md) | Runtime-grounded inventory of current expression limits and proposal pressure. |
| [expressiveness/internal-verbosity-analysis.md](./expressiveness/internal-verbosity-analysis.md) | Corpus evidence for compactness and declaration-pressure work. |
| [expressiveness/conditional-logic-strategy.md](./expressiveness/conditional-logic-strategy.md) | Guard vocabulary and conditional-shape guidance across the language surface. |
| [expressiveness/case-insensitive-comparison-survey.md](./expressiveness/case-insensitive-comparison-survey.md) | `~=` precedent survey, CI comparison patterns across 15+ systems, cascade analysis. Supports `#16`. |
| [expressiveness/nullable-computed-fields-research.md](./expressiveness/nullable-computed-fields-research.md) | 10-system survey of null propagation approaches; taxonomy of implicit/explicit/monadic/absence-eliminates strategies. Supports `#17`. |
| [expressiveness/null-elimination-research.md](./expressiveness/null-elimination-research.md) | Deep external research on eliminating nulls from Precept — surveys null-free languages, config DSLs, serialization formats, databases, UI form modeling. Supports `#17`. |
| [expressiveness/null-reduction-proposal.md](./expressiveness/null-reduction-proposal.md) | Full design proposal for null reduction — `optional`, state-scoped visibility, presence operators, `clear`. Supports `#17`. |
| [expressiveness/grammar-consistency-audit.md](./expressiveness/grammar-consistency-audit.md) | Comprehensive audit of 21 grammar consistency items — keyword overloading, operator positions, classification gaps, naming patterns. Triggered during null-reduction proposal refinement. |
| [domain-map.md](./domain-map.md) | Full durable map of every language research domain. |
| [domain-research-batches.md](./domain-research-batches.md) | Sequencing plan for active and horizon domain research. |
| [references/cel-comparison.md](./references/cel-comparison.md) | Full Precept vs CEL language-level comparison — validates expression expansion priorities. |

## Domains with research but no active proposal

These docs are deliberate horizon groundwork, not orphaned notes:

- [expressiveness/transition-shorthand.md](./expressiveness/transition-shorthand.md)
- [expressiveness/type-system-follow-ons.md](./expressiveness/type-system-follow-ons.md)
- [expressiveness/structural-lifecycle-modifiers.md](./expressiveness/structural-lifecycle-modifiers.md)

## Implemented domains

Research that grounded a shipped feature:

- [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md) — grounded issue `#106` (local proof engine, PR #108)

## Workflow reminder

Per [CONTRIBUTING.md](../../../CONTRIBUTING.md): research lives here, proposal bodies stay in GitHub issues, and future proposals should link back to the domain packet and theory companion that ground them.
