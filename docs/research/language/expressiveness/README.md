# DSL Expressiveness Research

This directory contains comparative research showing where Precept's DSL is stronger, weaker, or structurally different from adjacent tools and DSLs.

These files are **research evidence, not proposal sources**. Canonical proposal bodies and status live in GitHub issues.

## Libraries studied

| File | Library | Closest Precept overlap |
|---|---|---|
| [fluent-assertions.md](./fluent-assertions.md) | FluentAssertions | `invariant`, `on ... assert` |
| [zod-valibot.md](./zod-valibot.md) | Zod / Valibot | Field declarations + `invariant` |
| [xstate.md](./xstate.md) | xstate | `from ... on ... when` |
| [polly.md](./polly.md) | Polly | `->` action pipeline |
| [fluent-validation.md](./fluent-validation.md) | FluentValidation | `invariant`, `in <State> assert` |
| [linq.md](./linq.md) | LINQ | mutation/value-selection patterns |

## Core synthesis files

| File | Purpose |
|---|---|
| [expression-language-audit.md](./expression-language-audit.md) | Runtime-grounded map of the current expression surface and its constraints |
| [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) | Corpus-based compactness analysis |
| [expression-tracking-notes.md](./expression-tracking-notes.md) | Labeling guidance for `dsl-expressiveness` vs `dsl-compactness` |
| [type-system-domain-survey.md](./type-system-domain-survey.md) | Real-world domain + platform evidence validating proposal #25 (choice + date types) |

## Proposal issue map

Use the issue for the proposal body; use the research file for evidence.

| Issue | Topic | Primary evidence in this folder |
|---|---|---|
| `#8` | Named rule declarations | `xstate.md`, `polly.md`, `fluent-validation.md`, `expression-language-audit.md` |
| `#9` | Ternary expressions in `set` mutations | `linq.md`, `polly.md`, `expression-language-audit.md` |
| `#10` | String `.length` accessor | `zod-valibot.md`, `fluent-validation.md`, `expression-language-audit.md` |
| `#13` | Field-level range/basic constraints | `zod-valibot.md`, `fluent-validation.md`, `expression-language-audit.md` |
| `#14` | Conditional invariants (`when` on `invariant`) | `fluent-validation.md`, `expression-language-audit.md` |
| `#15` | String `.contains()` membership test | `expression-language-audit.md` |
| `#16` | Numeric built-in functions (`min`, `max`, `abs`, `round`) | `expression-language-audit.md` |
| `#17` | Computed / derived fields | `expression-language-audit.md` |
| `#18` | Conditional outcome in `->` chain (rejected) | `linq.md`, `expression-language-audit.md` |
| `#25` | Type system expansion (choice + date) | `type-system-domain-survey.md` |

## Proposal-author rule

If a GitHub issue proposes a language change, it should link back to:

1. at least one file in this folder, and
2. at least one file in `../references/`

That keeps every proposal grounded in both precedent and principle.

## Hero sample implications

Three recurring findings still matter for sample design:

1. Avoid loading a hero with simple format-only invariants when business-rule invariants tell the story better.
2. Keep `when` guards short unless the point of the sample is to demonstrate named-rule pressure.
3. Feature `in <State> assert` when possible; it remains one of Precept's clearest differentiators.
