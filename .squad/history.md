# Project Context

- **Owner:** {user name}
- **Project:** {project description}
- **Stack:** {languages, frameworks, tools}
- **Created:** {timestamp}

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-28 — Frank Round 5: Validation Layer Design

**Agent:** Frank (Language Designer / Compiler Architect)
**Document:** `docs/working/catalog-parser-design-v5.md`
**Decisions:** `F7`, `F8`, `F9`, `F10` (filed to `.squad/decisions/inbox/frank-r5-validation-layer-20260428.md`)

**Summary:** Addressed Shane's validation-over-generation directive. Designed a four-tier validation layer (compile-time CS8509, startup assertions, test enforcement, documentation checklist) that ensures adding a new construct fails loudly at every gap. Key decisions:

- **F7:** Replaced `_slotParsers` dictionary with exhaustive switch — CS8509 enforces slot parser completeness at build time (supersedes F2).
- **F8:** Resolved G5 bug — introduced `ConstructSlotKind.RuleExpression` for the rule body (no introduction token). `RuleDeclaration` corrected from `[GuardClause, BecauseClause]` to `[RuleExpression, GuardClause(opt), BecauseClause]`.
- **F9:** Withdrew F4 (pre-event guard). Spec is correct as written. Disambiguator consumes `when` unconditionally but emits diagnostic for `from State when Guard on Event`.
- **F10:** Confirmed `EnsureClause` and `BecauseClause` as separate slots.

Resolved all five of George's P1–P5 items. Design declared stable for implementation planning. PR 1 scope confirmed with F8 addition.
