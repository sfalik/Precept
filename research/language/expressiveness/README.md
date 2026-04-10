# Language Expressiveness and Domain Research

This directory is organized **domain first**.

Use it to answer two questions:

1. **Which capability domain does this proposal belong to?**
2. **Which comparative research and corpus evidence ground that domain?**

These files are durable research, not proposal bodies. Proposal text and status stay in GitHub issues.

## Domain packets

Start with the domain packet, then branch into the comparative docs it cites.

| Domain | Open proposals | Start here | Key supporting docs |
|---|---|---|---|
| Type system expansion | `#25`, `#26`, `#27`, `#29` | [type-system-domain-survey.md](./type-system-domain-survey.md) | [expression-language-audit.md](./expression-language-audit.md), [type-system-follow-ons.md](./type-system-follow-ons.md) |
| Event ingestion shorthand | `#11` | [event-ingestion-shorthand.md](./event-ingestion-shorthand.md) | [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) |
| Constraint composition | `#8`, `#13`, `#14` | [constraint-composition-domain.md](./constraint-composition-domain.md) | [conditional-logic-strategy.md](./conditional-logic-strategy.md), [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) |
| Expression expansion | `#9`, `#10`, `#15`, `#16`, `#31` | [expression-expansion-domain.md](./expression-expansion-domain.md) | [expression-language-audit.md](./expression-language-audit.md), [conditional-logic-strategy.md](./conditional-logic-strategy.md) |
| Entity-modeling surface | `#17`, `#22` | [entity-modeling-surface.md](./entity-modeling-surface.md) | [computed-fields.md](./computed-fields.md), [data-only-precepts-research.md](./data-only-precepts-research.md) |
| Transition shorthand | — | [transition-shorthand.md](./transition-shorthand.md) | [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) |
| Type-system follow-ons | — | [type-system-follow-ons.md](./type-system-follow-ons.md) | [type-system-domain-survey.md](./type-system-domain-survey.md) |

## Comparative research set

These files compare Precept against adjacent tools and systems. They are not proposal specs; they are precedent inputs reused across domains.

| File | Comparative focus | Most useful for |
|---|---|---|
| [fluent-assertions.md](./fluent-assertions.md) | Collect-all assertion ergonomics and readable failure framing | Constraint composition |
| [fluent-validation.md](./fluent-validation.md) | Validator rule structure, conditional validation, field-local constraints | Constraint composition, expression expansion |
| [zod-valibot.md](./zod-valibot.md) | Modern schema DSLs, string/field constraints, validator ergonomics | Constraint composition, expression expansion, type pressure |
| [xstate.md](./xstate.md) | Guard reuse, transition organization, explicit action semantics | Constraint composition, transition shorthand, event ingestion boundaries |
| [polly.md](./polly.md) | Pipeline-style composition and conditional outcome pressure | Expression expansion |
| [linq.md](./linq.md) | Value selection, conditional expressions, concise data-oriented syntax | Expression expansion |

## External language comparisons

| File | Comparative focus | Most useful for |
|---|---|---|
| [../references/cel-comparison.md](../references/cel-comparison.md) | Google CEL: expression model, type system, logical operators, extension model, safety guarantees | Expression expansion, keyword vs symbol surface |

## Cross-cutting analysis docs

| File | Purpose |
|---|---|
| [expression-language-audit.md](./expression-language-audit.md) | Runtime-grounded inventory of what the DSL can and cannot currently express. |
| [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) | Corpus evidence for compactness and ceremony-reduction pressure. |
| [conditional-logic-strategy.md](./conditional-logic-strategy.md) | Shared guidance for `when`, conditional shapes, and related guard vocabulary. |
| [expression-tracking-notes.md](./expression-tracking-notes.md) | Labeling and tracking guidance for `dsl-expressiveness` vs `dsl-compactness`. |
| [type-system-domain-survey.md](./type-system-domain-survey.md) | Real-world domain and platform evidence validating proposal #25 (`choice`, `date`, and follow-on scalar pressure). |

## Proposal grounding map

Every open proposal in this lane should be traceable back to a domain packet here and a theory companion in `../references/`.

| Issue | Focus | Domain packet | Comparative / supporting docs |
|---|---|---|---|
| `#8` | Named rule declarations | [constraint-composition-domain.md](./constraint-composition-domain.md) | [fluent-validation.md](./fluent-validation.md), [xstate.md](./xstate.md), [fluent-assertions.md](./fluent-assertions.md) |
| `#9` | Conditional expressions in value positions | [expression-expansion-domain.md](./expression-expansion-domain.md) | [linq.md](./linq.md), [polly.md](./polly.md), [expression-language-audit.md](./expression-language-audit.md) |
| `#10` | String `.length` accessor | [expression-expansion-domain.md](./expression-expansion-domain.md) | [zod-valibot.md](./zod-valibot.md), [fluent-validation.md](./fluent-validation.md), [expression-language-audit.md](./expression-language-audit.md) |
| `#11` | Event argument absorb shorthand | [event-ingestion-shorthand.md](./event-ingestion-shorthand.md) | [internal-verbosity-analysis.md](./internal-verbosity-analysis.md), [xstate.md](./xstate.md) |
| `#13` | Field-level range / basic constraints | [constraint-composition-domain.md](./constraint-composition-domain.md) | [zod-valibot.md](./zod-valibot.md), [fluent-validation.md](./fluent-validation.md), [fluent-assertions.md](./fluent-assertions.md) |
| `#14` | Conditional invariants / guarded declarations | [constraint-composition-domain.md](./constraint-composition-domain.md) | [conditional-logic-strategy.md](./conditional-logic-strategy.md), [fluent-validation.md](./fluent-validation.md) |
| `#15` | String `.contains()` | [expression-expansion-domain.md](./expression-expansion-domain.md) | [expression-language-audit.md](./expression-language-audit.md), [zod-valibot.md](./zod-valibot.md) |
| `#16` | Built-in function library | [expression-expansion-domain.md](./expression-expansion-domain.md) | [expression-language-audit.md](./expression-language-audit.md), [linq.md](./linq.md), [polly.md](./polly.md) |
| `#17` | Computed / derived fields | [entity-modeling-surface.md](./entity-modeling-surface.md), [computed-fields.md](./computed-fields.md) | [expression-language-audit.md](./expression-language-audit.md) |
| `#22` | Data-only precepts | [entity-modeling-surface.md](./entity-modeling-surface.md), [data-only-precepts-research.md](./data-only-precepts-research.md) | [expression-tracking-notes.md](./expression-tracking-notes.md) |
| `#25` | `choice` type | [type-system-domain-survey.md](./type-system-domain-survey.md) | [expression-language-audit.md](./expression-language-audit.md) |
| `#26` | `date` type | [type-system-domain-survey.md](./type-system-domain-survey.md) | [expression-language-audit.md](./expression-language-audit.md) |
| `#27` | `decimal` type | [type-system-domain-survey.md](./type-system-domain-survey.md) | [expression-language-audit.md](./expression-language-audit.md) |
| `#29` | `integer` type | [type-system-domain-survey.md](./type-system-domain-survey.md) | [expression-language-audit.md](./expression-language-audit.md) |
| `#31` | Logical keyword forms (`and` / `or` / `not`) | [expression-expansion-domain.md](./expression-expansion-domain.md) | [conditional-logic-strategy.md](./conditional-logic-strategy.md), [expression-language-audit.md](./expression-language-audit.md) |

## Domains already grounded without an active proposal

These docs intentionally exist ahead of proposal work:

| Domain | Why it exists now |
|---|---|
| [transition-shorthand.md](./transition-shorthand.md) | Future compactness work should start from desugaring and diagnostic fidelity research, not intuition. |
| [type-system-follow-ons.md](./type-system-follow-ons.md) | Residual type pressure should stay attached to the main type-system corpus instead of spawning premature proposal churn. |

## Pair this folder with

- [../README.md](../README.md) for the master issue map and top-level domain index
- [../references/README.md](../references/README.md) for the theory companions each domain should cite
