## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, and field-state design work must stay grounded in the shipped spec and evaluator/runtime surfaces, not inferred intent.

## Live Guidance

- `readonly` / `editable` / guarded access modes govern the Update (patch) surface; they do not restrict event-driven `set` actions unless the spec changes.
- Business-domain magnitude modifier legality is a catalog contract: fix drift in metadata and docs, not with checker-only special cases.
- Required-field constructor enforcement is now an active implementation surface: D93 and D94 are real checker obligations, while stateless construction stays outside the state-entry slice.

## Historical Summary

- 2026-05-12 through 2026-05-13 concentrated Frank's work around hover contract reviews, comma-list `StateTarget` closure, field-state-guarantees design, modifier applicability drift, constructor-gap analysis, and diagnostic coverage enforcement planning.
- Durable batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps the research posture plus the newest conclusions other agents need immediately.

## Recent Updates

### 2026-05-13 — Field-state guarantees and constructor enforcement are the active baseline

- The v3 field-state design is now canonically D130/D131/D132, with author-language follow-up still needed for D132's shipped name.
- Frank's gap audit established that declaration metadata is not enough: D93/D94 were specified but unenforced, so prerequisite audits must verify real `DiagnosticCode` emission sites before downstream design work assumes behavior exists.
- George's Slice 9 and Slice 10/11 follow-through closed the immediate construction-risk loop: disjunction support landed, D93 now blocks no-initial-event required-field holes, and D94 now enforces required assignments on initial-event construction paths while keeping stateless precepts exempt.
- The diagnostic coverage enforcement recommendation remains a convention-test allow-list model, using literal `DiagnosticCode.{Member}` references in pipeline and catalog-emission files as the minimum coverage bar.

## Learnings

- If a design depends on an existing diagnostic, confirm the emitting pipeline stage instead of trusting `DiagnosticMeta` declarations or spec prose.
- `set` into an `omit` target-state field is the decisive field-state rule; Update access modes do not constrain Fire semantics.
- D132 is the structural complement to D94: both exist to prevent required fields from becoming present without a valid value.
- Catalog static initialization may not reach downstream catalog statics during cctor execution; reverse references must defer through `Lazy<T>`.
