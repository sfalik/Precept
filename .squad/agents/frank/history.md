## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, and field-state design work must stay grounded in the shipped spec and evaluator/runtime surfaces, not inferred intent.

## Live Guidance

- `readonly` / `editable` / guarded access modes govern the Update (patch) surface; they do not restrict event-driven `set` actions unless the spec changes.
- Business-domain magnitude modifier legality is a catalog contract: fix drift in metadata and docs, not with checker-only special cases.
- Required-field constructor enforcement is still a product-level gap; samples currently rely on Draft-state editability while runtime `Create()` remains unfinished.

## Historical Summary

- 2026-05-12 concentrated Frank's work around hover contract reviews, comma-list `StateTarget` design closure, field-state-guarantees analysis, modifier applicability drift, and the required-field constructor gap.
- Durable batch-by-batch detail now lives in `.squad/decisions.md`; this file keeps the research posture and the latest high-value conclusions other agents need immediately.

## Recent Updates

### 2026-05-12T23:50:08Z — Modifier applicability and constructor gaps are durable team memory

- Frank's modifier audit locked the core judgment: `price` bound modifiers and business-magnitude `maxplaces` were missing catalog metadata, while `notempty` on scalar business magnitudes should remain invalid and identity-type `notempty` should be redundancy-only.
- Frank's required-field analysis confirmed PRE0093/PRE0094 are specified but unimplemented, with no emitting pipeline stage, no runtime `Create()` support, and no sample coverage.

### 2026-05-12T23:25:25Z — Final comma-list spike approval is the current architectural baseline

- Frank approved commits `53d68d51` and `cf3c6a81`, locking `ResolvedStateTarget.IsWildcard` and keeping `NormalizeTransitionRow` as the intentional compatibility boundary that projects wildcard rows back to `TypedTransitionRow.FromState = null`.
- Remaining follow-up is proposal hardening, not implementation repair: defend the wildcard boundary, stay honest about localized parser grammar shaping, and strengthen the written rationale around locked decisions `D3` / `D4`.

### 2026-05-12T19:38:00Z — Field-state v2 consistency review stayed blocked on spec drift

- Frank confirmed D133, the parser field-target fix, omit/access-mode unification, and D42/D43 emission are grounded in the canonical spec.
- He blocked D132, D134, and the broader proof-enforcement surface because the spec explicitly says `readonly` / `editable` do not restrict event-driven `set`, and he flagged from-state D130 / guard-read D131 as needing narrower justification or explicit spec extension.

### 2026-05-13T00:08:20Z — Frank's hover B1-B4 review cycle finished fully approved

- Across B2/B3 and B4, Frank's blockers locked the final quality bar: correct rich-construct routing order, omit-aware mutability honesty, explicit `omit all` regression coverage, honest no-obligation proof narration, and duplicate-proof suppression tests.
- Final re-reviews `frank-7` and `frank-9` approved the repaired implementations, closing the full B1-B4 hover program without remaining review debt.
- The approved end state is commit-backed by `c2a38a56`, `47f3068c`, and `9617f39b`, with `279/279` language-server tests and `4973` core tests green.
## Learnings

- When a design spans Update and Fire behavior, verify the split against the spec and evaluator before planning diagnostics; a single explicit rule can invalidate an otherwise plausible implementation plan.
- Catalog/spec drift around business-domain types should be recorded as metadata gaps, not framed as deep semantic exclusions, when the checker is merely enforcing incomplete applicability tables.
- If a spec guarantee depends on runtime surfaces that do not yet exist (for example constructor enforcement around `Create()`), record the gap and owner decision boundary before pushing implementation work.

