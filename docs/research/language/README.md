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

## Domain index

| Domain | Open proposals | Primary grounding | Theory / reference companion | Notes |
|---|---|---|---|---|
| Type system expansion | `#25`, `#26`, `#27`, `#29` | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md) | [references/type-system-survey.md](./references/type-system-survey.md) | Core active type lane. |
| Event ingestion shorthand | `#11` | [expressiveness/event-ingestion-shorthand.md](./expressiveness/event-ingestion-shorthand.md) | [references/expression-compactness.md](./references/expression-compactness.md) | Compactness lane; research says keep it narrow and audit-friendly. |
| Constraint composition | `#8`, `#13`, `#14` | [expressiveness/constraint-composition-domain.md](./expressiveness/constraint-composition-domain.md) | [references/constraint-composition.md](./references/constraint-composition.md), [references/conditional-invariant-survey.md](./references/conditional-invariant-survey.md) | Shared validator / rule / declaration domain. |
| Expression expansion | `#9`, `#10`, `#15`, `#16`, `#31` | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md) | [references/expression-evaluation.md](./references/expression-evaluation.md) | One vocabulary lane for conditionals, string operations, functions, and logical keywords. |
| Entity-modeling surface | `#17`, `#22` | [expressiveness/entity-modeling-surface.md](./expressiveness/entity-modeling-surface.md), [expressiveness/computed-fields.md](./expressiveness/computed-fields.md), [expressiveness/data-only-precepts-research.md](./expressiveness/data-only-precepts-research.md) | [references/expression-evaluation.md](./references/expression-evaluation.md) | Governs stored-vs-derived facts and lifecycle-light entities. |
| Transition shorthand | — | [expressiveness/transition-shorthand.md](./expressiveness/transition-shorthand.md) | [references/multi-event-shorthand.md](./references/multi-event-shorthand.md), [references/state-machine-expressiveness.md](./references/state-machine-expressiveness.md) | Horizon domain with no open proposal yet. |
| Static reasoning expansion | — | [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md) | [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md) | Horizon domain; currently theory-first rather than proposal-first. |
| Type-system follow-ons | — | [expressiveness/type-system-follow-ons.md](./expressiveness/type-system-follow-ons.md) | [references/type-system-survey.md](./references/type-system-survey.md) | Residual type pressure after the main type wave; no active proposal yet. |

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
| `#17` | Computed / derived fields | Entity-modeling surface | [expressiveness/entity-modeling-surface.md](./expressiveness/entity-modeling-surface.md), [expressiveness/computed-fields.md](./expressiveness/computed-fields.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |
| `#22` | Data-only precepts | Entity-modeling surface | [expressiveness/entity-modeling-surface.md](./expressiveness/entity-modeling-surface.md), [expressiveness/data-only-precepts-research.md](./expressiveness/data-only-precepts-research.md) |
| `#25` | `choice` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#26` | `date` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#27` | `decimal` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#29` | `integer` type | Type system expansion | [expressiveness/type-system-domain-survey.md](./expressiveness/type-system-domain-survey.md), [references/type-system-survey.md](./references/type-system-survey.md) |
| `#31` | Logical keyword forms (`and` / `or` / `not`) | Expression expansion | [expressiveness/expression-expansion-domain.md](./expressiveness/expression-expansion-domain.md), [references/expression-evaluation.md](./references/expression-evaluation.md) |

## Cross-cutting research that supports multiple domains

| File | Why it matters |
|---|---|
| [expressiveness/expression-language-audit.md](./expressiveness/expression-language-audit.md) | Runtime-grounded inventory of current expression limits and proposal pressure. |
| [expressiveness/internal-verbosity-analysis.md](./expressiveness/internal-verbosity-analysis.md) | Corpus evidence for compactness and declaration-pressure work. |
| [expressiveness/conditional-logic-strategy.md](./expressiveness/conditional-logic-strategy.md) | Guard vocabulary and conditional-shape guidance across the language surface. |
| [domain-map.md](./domain-map.md) | Full durable map of every language research domain. |
| [domain-research-batches.md](./domain-research-batches.md) | Sequencing plan for active and horizon domain research. |

## Domains with research but no active proposal

These docs are deliberate horizon groundwork, not orphaned notes:

- [expressiveness/transition-shorthand.md](./expressiveness/transition-shorthand.md)
- [references/static-reasoning-expansion.md](./references/static-reasoning-expansion.md)
- [expressiveness/type-system-follow-ons.md](./expressiveness/type-system-follow-ons.md)

## Workflow reminder

Per [CONTRIBUTING.md](../../../CONTRIBUTING.md): research lives here, proposal bodies stay in GitHub issues, and future proposals should link back to the domain packet and theory companion that ground them.
