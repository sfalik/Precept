# Core Context

- Owns UX/design across Data Form, Event Timeline, state-diagram, and author-facing product language.
- Keeps product wording plain, confident, and faithful to runtime truth rather than implementation jargon.
- Canonical surface names matter: `Data Form` and `Event Timeline` are the durable labels; legacy labels are errors.
- Visual-system HTML is the implementation ground truth when manifest prose and older working docs drift.
- Precept serves human DSL authors directly; AI help is optional, first-class, and never the required authoring mode.

## Learnings

- Hover docs need a quick-reference table first so implementers can find the construct they care about before reading prose.
- Meaning-first hover copy works best when the authored `because` text leads and proof detail stays compact.
- Field/arg coloring is a semantic split: fields read as structure identity, args as event-scoped behaviour; docs must not drift back to the retired unified data-name lane.
- Construct colors, verdict colors, and disabled-surface colors are separate systems and should never be blended.
- Field-state diagnostics need Problems-panel copy that names the field, the relevant state or state change, and the repair action in plain DSL terms; compiler shorthand like `omit→non-omit` is not shippable user text.
- Diagnostic IDs should use subject-first, plain-English condition names; `MustSetOmitToNonOmit` is a naming smell because it encodes compiler shorthand instead of the user-visible failure.

## Historical Summary

- 2026-05-07 through early 2026-05-12 locked the current visual-system posture: `<-` for computed fields, Data-family `--field` / `--arg` expansion, typed-value tone stability, and hover-doc organization centered on rendered examples first.
- The older unified `--data` anchor is retired history; current design truth lives in the field/arg split and in visual-system HTML when prose lags.

## Recent Updates

### 2026-05-13T00:32:50Z — Field-state diagnostic UX review locked the user-facing naming bar

- Elaine reviewed the canonicalized `docs/Working/field-state-guarantees-v3.md` surface and flagged code drift: earlier team notes used provisional D131/D133/D135, but the v3 doc now canonically uses D130/D131/D132.
- She judged D130 and D131 shippable in concept, rejected `MustSetOmitToNonOmit` as compiler shorthand, and proposed direct Problems-panel copy that names the field first, the state(s) second, and the repair action explicitly.
- In a follow-up naming-normalization proposal, she argued the catalog family should move toward subject-first names like `FieldOmittedInStateCannotBeRead`, `FieldOmittedInTargetStateCannotBeSet`, and `RequiredFieldNeedsAssignmentWhenBecomingPresent`, while leaving D42/D43 alone.

### 2026-05-12T22:25:28Z — B4 as-built hover doc sync recorded

- Updated `docs/Working/hover-design.md` so the working hover spec now records the shipped B4 state-proof narrative as-built, including the `📍`, `✅ Proven`, and `⚠️ Gap` badge vocabulary.
- Locked the doc boundary that B4 appends to the rich state hover card and does not ship as a standalone hover kind.

### 2026-05-12T13:52:04Z — Hover color and docs alignment pass recorded

- Confirmed the current field/arg split is the intended design direction and that the remaining gap is documentation drift, not a request to revert implementation.
- Prioritized typed-literal semantic consistency and explicit builtin-function coloring over any rollback toward the retired unified slate model.

### 2026-05-12T18:01:17.648-04:00 — Hover Q1/Q2/Q3 resolved in V6

- Locked three implementation-facing hover decisions in `docs/Working/hover-design.md`: suppress qualifier/use counts in V1, wrap long guards instead of truncating them, and inline PRE codes on rule/ensure violation cards.
- Section 6 no longer carries open questions; Elaine’s hover-design revision is complete and ready for implementation consumption.
