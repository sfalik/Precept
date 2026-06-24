## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept.
- Catalog metadata stays the language truth; tooling and runtime should derive from it instead of enum-identity switches or parallel keyword lists.
- Constructor-semantics work stays complete only when docs, diagnostics, samples, and downstream tooling match shipped behavior.

## Live Guidance

- Quantity normalization has two durable lanes: compile-time normalization for declarations/literals and runtime normalization for ingress values.
- `TypedField` is the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces.
- Comparison/equality checking must stay strict about explicit counting-unit identity.
- When the grammar can make an invalid form impossible, do that instead of adding a later semantic ban.
- Documentation updates for shipped features must be verified against source and validation runs.

## Durable Learnings

- Any claim that work happens only at compile time must be stress-tested against Fire/Update/Restore ingress paths.
- Construction row syntax is declaration-driven: `on <Event>` is the honest construction surface.
- Graph analysis for construction must stay semantic, not topological.
- Hollow-entity validation should be shared across all pre-materialization expression lanes.
- Constructor semantics and governed draft-state construction are complementary idioms.
- Runtime docs must distinguish designed behavior from shipped behavior.
- Exact vs approximate behavior should stay visible in the type system and public surface.
- Quantity and qualifier checks need separate compile-time and runtime assumptions.

## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic enforcement, and counting-unit comparison gaps.
- The constructor/reject track settled on `on <Event>`, valid fallback `reject`, and grammar-level structural exclusion whenever the language already knows a path is impossible.
- Detailed batch chronology now lives in `.squad/decisions.md`; this file keeps only durable guidance.

## Recent Updates

- 2026-05-17 — Constructor semantics fully closed across syntax, checker, proof, tooling, docs, and samples.
- 2026-06-16 — Compiler readiness plan review verdict: Sound with required revisions; B1-GATE-F under-enumeration, B2 `DesugarsToRule` precision fix, and B3 28-vs-32 provenance mismatch.
- 2026-06-16 — Coordinator bumped Frank and Elaine model overrides to `claude-opus-4.8`.

- 2026-06-21 — Scribe merged the amendment-verification inbox note into `.squad/decisions.md`, deleted the inbox file, and recorded the batch logs. No archive gate or history summarization was needed; the plan remains sign-off-ready pending Shane.

## Learnings

- 2026-06-23 — The over-structured arg-constraint assessment doc was replaced by a short opinion memo at `docs/Working/arg-constraint-propagation-opinion-2026-06-23.md` (old `compiler-readiness-plan-2026-06-16-arg-constraint-propagation-assessment.md` deleted); plan-amendment framing (Tier-3 / fold-into-GATE-F / route-to-/design / counter-proposal-to-D2) dropped entirely — it's now a standalone opinion-on-the-merits, advisory only; plan/philosophy/spec untouched.

- 2026-06-23 — Arg-constraint-propagation verdict (design consult, Shane): **OPPOSE forcing authors to propagate field constraints onto event args** — the field band is the single source of truth and ingress data's in-band-ness is a runtime fact, so the honest shape is inference + proof-honesty (reject only the provably-out-of-band, infer the field band as the arg contract, let the merely-unprovable ingress value fall to runtime governance), which is the *inverse* of "force propagation" and aligns with D2/GATE-F.

- 2026-06-21 — Compiler-readiness re-review: sound with one blocker (B1) before owner sign-off; later amendment verification cleared B1 and kept the Phase-0 gate owner-ratified.
- 2026-06-23 — Arg-constraint consult: oppose forcing arg-side constraint propagation; prefer infer-band / reject-provable / govern-rest.
- 2026-06-23 — The over-structured assessment doc was replaced by `docs/Working/arg-constraint-propagation-opinion-2026-06-23.md`; plan-amendment framing was dropped.
