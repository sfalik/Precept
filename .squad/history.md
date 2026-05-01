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

## [Session] Phase 2 Plan Complete

Phase 1 full implementation review by Frank-6: 9 slices clean, Slice 2 (GAP-A when-guard) and Slice 13 (ExpressionFormCoverageTests) not implemented. George-6 technical design for all Phase 2 items — key finding: GAP-A requires zero AST changes (Guard field already present). Frank-7 synthesis: Slices 14–26 authored and appended to docs/working/parser-gap-fixes-plan.md. Shane's directive confirmed: no deferred items before type-checker work. 13-point acceptance gate defined. Phase 1 test baseline: 2482 passing.
