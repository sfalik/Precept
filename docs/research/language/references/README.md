# Language Theory and Reference Research

This directory is the theory and reference companion to the domain packets in `../expressiveness/`.

Use it when you need formal grounding, precedent from language theory, or a principled boundary for what should stay out of scope.

## Reference index

| File | Domain grounded | Open proposals | Why it matters |
|---|---|---|---|
| [type-system-survey.md](./type-system-survey.md) | Type system expansion and type-system follow-ons | `#25`, `#26`, `#27`, `#29` | Formal type-model, coercion, scalar semantics, and cross-system precedent for the expanded type surface. |
| [constraint-composition.md](./constraint-composition.md) | Constraint composition | `#8`, `#13`, `#14` | Predicate-combinator theory, scope boundaries, and desugaring model for rules and field constraints. |
| [conditional-invariant-survey.md](./conditional-invariant-survey.md) | Constraint composition | `#14` | Why guarded declarations should read as positive conditions instead of formal implication syntax. |
| [expression-evaluation.md](./expression-evaluation.md) | Expression expansion and computed-field semantics | `#9`, `#10`, `#15`, `#16`, `#17`, `#31` | Decidability map for expression growth and bounded-function design. |
| [expression-compactness.md](./expression-compactness.md) | Event ingestion shorthand and future compactness work | `#11` | Desugaring and shorthand boundaries for keeping compact syntax inspectable. |
| [multi-event-shorthand.md](./multi-event-shorthand.md) | Transition shorthand | — | Formal basis for multi-event `on` and related event-set sugar. |
| [state-machine-expressiveness.md](./state-machine-expressiveness.md) | Transition shorthand | — | Statechart and state-machine precedent for what Precept should and should not borrow. |
| [static-reasoning-expansion.md](./static-reasoning-expansion.md) | Static reasoning expansion | — | Horizon-domain grounding for contradiction detection, deadlock analysis, and interval-first reasoning. |
| [cel-comparison.md](./cel-comparison.md) | Expression expansion, keyword vs symbol surface | `#9`, `#10`, `#15`, `#16`, `#31` | Full language-level comparison against Google CEL — validates conditional expression priority, small string function surface, and deterministic logical operator semantics. |
| [governance-vs-validation.md](./governance-vs-validation.md) | Governance architecture, philosophy positioning | `#22` | Defines the governance-vs-validation distinction, surveys cross-system enforcement architectures, catalogs validation failure modes governance prevents. |

## Pairing guide

If you start from a domain packet, pair it with the reference docs below.

| Domain packet | Pair with |
|---|---|
| [../expressiveness/type-system-domain-survey.md](../expressiveness/type-system-domain-survey.md) | [type-system-survey.md](./type-system-survey.md) |
| [../expressiveness/event-ingestion-shorthand.md](../expressiveness/event-ingestion-shorthand.md) | [expression-compactness.md](./expression-compactness.md) |
| [../expressiveness/constraint-composition-domain.md](../expressiveness/constraint-composition-domain.md) | [constraint-composition.md](./constraint-composition.md), [conditional-invariant-survey.md](./conditional-invariant-survey.md) |
| [../expressiveness/expression-expansion-domain.md](../expressiveness/expression-expansion-domain.md) | [expression-evaluation.md](./expression-evaluation.md), [cel-comparison.md](./cel-comparison.md) |
| [../expressiveness/entity-modeling-surface.md](../expressiveness/entity-modeling-surface.md) | [expression-evaluation.md](./expression-evaluation.md) |
| [../expressiveness/data-only-precepts-research.md](../expressiveness/data-only-precepts-research.md) | [governance-vs-validation.md](./governance-vs-validation.md) |
| [../expressiveness/transition-shorthand.md](../expressiveness/transition-shorthand.md) | [multi-event-shorthand.md](./multi-event-shorthand.md), [state-machine-expressiveness.md](./state-machine-expressiveness.md) |
| [../expressiveness/type-system-follow-ons.md](../expressiveness/type-system-follow-ons.md) | [type-system-survey.md](./type-system-survey.md) |
| Horizon compile-time analysis work | [static-reasoning-expansion.md](./static-reasoning-expansion.md) |

## Open proposal grounding from this folder

| Issue | Focus | Theory grounding |
|---|---|---|
| `#8` | Named rule declarations | [constraint-composition.md](./constraint-composition.md) |
| `#9` | Conditional expressions in value positions | [expression-evaluation.md](./expression-evaluation.md) |
| `#10` | String `.length` accessor | [expression-evaluation.md](./expression-evaluation.md) |
| `#11` | Event argument absorb shorthand | [expression-compactness.md](./expression-compactness.md) |
| `#13` | Field-level range / basic constraints | [constraint-composition.md](./constraint-composition.md) |
| `#14` | Conditional invariants / guarded declarations | [constraint-composition.md](./constraint-composition.md), [conditional-invariant-survey.md](./conditional-invariant-survey.md) |
| `#15` | String `.contains()` | [expression-evaluation.md](./expression-evaluation.md) |
| `#16` | Built-in function library | [expression-evaluation.md](./expression-evaluation.md) |
| `#17` | Computed / derived fields | [expression-evaluation.md](./expression-evaluation.md) |
| `#22` | Data-only precepts | [governance-vs-validation.md](./governance-vs-validation.md), [../expressiveness/entity-modeling-surface.md](../expressiveness/entity-modeling-surface.md) |
| `#25` | `choice` type | [type-system-survey.md](./type-system-survey.md) |
| `#26` | `date` type | [type-system-survey.md](./type-system-survey.md) |
| `#27` | `decimal` type | [type-system-survey.md](./type-system-survey.md) |
| `#29` | `integer` type | [type-system-survey.md](./type-system-survey.md) |
| `#31` | Logical keyword forms (`and` / `or` / `not`) | [expression-evaluation.md](./expression-evaluation.md), [cel-comparison.md](./cel-comparison.md) |

## Theory-first domains with no proposal yet

These docs are here so future proposal work starts from grounding that already exists.

- [multi-event-shorthand.md](./multi-event-shorthand.md)
- [state-machine-expressiveness.md](./state-machine-expressiveness.md)
- [static-reasoning-expansion.md](./static-reasoning-expansion.md)

## Related indexes

- [../README.md](../README.md) — master domain index and issue map
- [../expressiveness/README.md](../expressiveness/README.md) — domain packets and comparative research index
