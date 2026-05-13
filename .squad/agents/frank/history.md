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

### 2026-05-13T00:45:00Z — Adopted field-state names were applied to the v3 doc

- Frank updated `docs\Working\field-state-guarantees-v3.md` everywhere the old field-state diagnostic names appeared so the v3 design now uses `OmittedFieldReadInState`, `OmittedFieldSetInTargetState`, and `RequiredFieldUnassignedOnEntry` throughout.
- He checked `src\Precept\Language\SyntaxReference.cs` for overlap with Elaine's prose work and intentionally made no edit there to avoid trampling concurrent changes.

### 2026-05-13T00:32:50Z — Field-state v3 is now canonically D130/D131/D132

- Frank's v3 design now records the canonical numbering after the doc renumber pass: `ReadOfOmittedField` = D130, `WriteToTargetOmittedField` = D131, and `MustSetOmitToNonOmit` = D132; older notes that said D131/D133/D135 should be read through that mapping.
- The design boundary remains unchanged: the blocked from-state target-write, readonly/access-condition, and ProofEngine surfaces stay out because Update access modes do not govern Fire/`set`.
- The companion initialization analysis still locks the three construction scenarios: initial events must assign required fields, no-initial-event precepts cannot contain required no-default fields, and stateless precepts inherit the same constructor guarantees minus state-entry behavior.
- Elaine's UX pass says D130 and D131 are conceptually sound, but D132's canonical name still needs an author-language rewrite before ship; her proposed Problems-panel copy is now part of durable team memory.
- Elaine also proposed a subject-first naming cleanup if the family is normalized in code: `FieldOmittedInStateCannotBeRead`, `FieldOmittedInTargetStateCannotBeSet`, `RequiredFieldNeedsAssignmentWhenBecomingPresent`, and a tighter `InitialEventMissingRequiredFieldAssignments`.

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
- §2.2 rule #6 is the single most important sentence for field-state enforcement: "`set` targeting an `omit` field in the target state is a compile error; `readonly`/`editable` do not restrict `set`." This one rule invalidated three v2 diagnostics (D130, D132, D134) and the entire ProofEngine conditional enforcement phase.
- Canonical v3 D130 scope must extend beyond transition row guards to all state-anchored expression contexts (`in`-state ensures, `from`-state ensures, state action guards). The evaluator confirms guard timing at line ~499: guard evaluates against from-state slots before working copy creation.
- Canonical v3 D132 (`MustSetOmitToNonOmit`) is the structural dual of `InitialEventMissingAssignments` — both prevent required fields from existing without valid values. D132 fills a spec gap where rule #5 covers entering-omit but is silent on leaving-omit.
- The three precept forms (with initial event, without initial event, stateless) have different D132 applicability profiles. Form 2 (no initial event) makes D132 structurally unsatisfiable because `RequiredFieldsNeedInitialEvent` forces all fields to have defaults or be optional.
- §3.5 "All field names" describes name resolution scope, not semantic validity. Canonical v3 D130 operates in the gap between "the name resolves" and "reading it is meaningful." This distinction must be annotated in the spec when D130 ships.
