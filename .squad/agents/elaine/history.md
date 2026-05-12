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

## Historical Summary

- 2026-05-07 through early 2026-05-12 locked the current visual-system posture: `<-` for computed fields, Data-family `--field` / `--arg` expansion, typed-value tone stability, and hover-doc organization centered on rendered examples first.
- The older unified `--data` anchor is retired history; current design truth lives in the field/arg split and in visual-system HTML when prose lags.

## Recent Updates

### 2026-05-12T13:52:04Z — Hover color and docs alignment pass recorded

- Confirmed the current field/arg split is the intended design direction and that the remaining gap is documentation drift, not a request to revert implementation.
- Prioritized typed-literal semantic consistency and explicit builtin-function coloring over any rollback toward the retired unified slate model.

### 2026-05-12T18:01:17.648-04:00 — Hover Q1/Q2/Q3 resolved in V6

- Locked three implementation-facing hover decisions in `docs/Working/hover-design.md`: suppress qualifier/use counts in V1, wrap long guards instead of truncating them, and inline PRE codes on rule/ensure violation cards.
- Section 6 no longer carries open questions; Elaine’s hover-design revision is complete and ready for implementation consumption.
