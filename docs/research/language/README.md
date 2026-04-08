# Language Research

Start here for Precept language research.

This folder now follows a strict split:

- **Research stays in `docs/`** — evidence, audits, precedent, rationale.
- **Proposals stay in GitHub issues** — canonical feature framing, status, and review discussion.

## Reading order

1. `expressiveness/README.md` — comparative research and the issue map for language-shape proposals.
2. `expressiveness/expression-language-audit.md` — implementation-grounded inventory of what the DSL can and cannot express today.
3. `references/README.md` — formal language and PLT references used to evaluate proposals.

## Folders

| Folder | Purpose |
|---|---|
| `expressiveness/` | Comparative research against adjacent tools plus runtime-grounded audits of current authoring pain. |
| `references/` | Language-theory and design-principle references used to judge proposal fit and risk. |

## Current proposal issue map

| Issue | Focus | Research starting points |
|---|---|---|
| `#8` | Named rule declarations | `expressiveness/xstate.md`, `expressiveness/polly.md`, `expressiveness/fluent-validation.md`, `expressiveness/expression-language-audit.md` |
| `#9` | Conditional expressions (`if...then...else`) | `expressiveness/conditional-logic-strategy.md`, `references/conditional-invariant-survey.md`, `expressiveness/linq.md`, `expressiveness/expression-language-audit.md` |
| `#10` | String `.length` accessor | `expressiveness/zod-valibot.md`, `expressiveness/fluent-validation.md`, `expressiveness/expression-language-audit.md`, `references/expression-evaluation.md` |
| `#11` | Event argument absorb shorthand | `expressiveness/internal-verbosity-analysis.md`, `references/expression-compactness.md` |
| `#12` | Inline guarded fallback (`else reject`) — **closed** | `expressiveness/conditional-logic-strategy.md`, `expressiveness/internal-verbosity-analysis.md`, `references/expression-compactness.md` |
| `#13` | Field-level range/basic constraints | `expressiveness/zod-valibot.md`, `expressiveness/README.md`, `references/constraint-composition.md` |
| `#14` | Conditional invariants (`when`/`unless` guards + `not` keyword) | `expressiveness/conditional-logic-strategy.md`, `references/conditional-invariant-survey.md`, `expressiveness/fluent-validation.md`, `references/constraint-composition.md` |
| `#15` | String `.contains()` membership test | `expressiveness/expression-language-audit.md`, `references/expression-evaluation.md` |
| `#16` | Numeric built-in functions (`min`, `max`, `abs`, `round`) | `expressiveness/expression-language-audit.md`, `references/expression-evaluation.md` |
| `#17` | Computed / derived fields | `expressiveness/expression-language-audit.md`, `references/expression-evaluation.md` |
| `#18` | Conditional outcome in `->` chain (rejected) | `expressiveness/linq.md`, `expressiveness/expression-language-audit.md` |
| `#25` | Type system expansion (choice and date types) | `references/type-system-survey.md`, `expressiveness/expression-language-audit.md` |
| `#22` | Data-only precepts (stateless domain definitions) | `expressiveness/data-only-precepts-research.md` |
| `#31` | Replace `!` with `not` keyword for logical negation | `expressiveness/conditional-logic-strategy.md`, `references/conditional-invariant-survey.md` |

## Rule of engagement

If you need to understand **why** a feature came up, read the research here.
If you need the current **proposal shape or status**, go to the corresponding GitHub issue.

## Proposal tracking workflow

Language proposals now follow one GitHub-native workflow:

- Keep the proposal body in the GitHub issue, not in `docs/`
- Add every language proposal to **Precept Language Improvements**
- Apply `proposal` + `language`
- Use the project board for workflow state: `Backlog -> Ready -> In Progress -> In Review -> Done`
- Use `deferred` only when the proposal is intentionally parked as an exception
- Keep `dsl-expressiveness` and `dsl-compactness` as slice labels, not status labels

That split keeps research in this folder, proposal state on GitHub, and roadmap filtering in one project queue.
