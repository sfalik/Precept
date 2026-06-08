# Soundness and Coverage — verify-don't-trust verdict checking

**Status:** Design — building per the soundness build plan. **Stub:** this is the reserved canonical home for the soundness-and-coverage architecture; it is populated *incrementally* as each build phase lands (status will move Design → Incremental → Implemented per phase). Until a section is filled here, the authoritative design source is the in-flight working docs listed under [§ Design source](#design-source-transitional).

> **What this doc is.** The canonical reference for the layer that makes Precept's compile-time guarantee *checked* rather than *trusted*. The guarantee — "if it compiles, the entity cannot fault and no invalid configuration persists" (spec § 0.1, Principles 7/10/11; § 0.7) — is only as sound as the compiler's verdicts, and historically those verdicts were **asserted, not verified**: `[StaticallyPreventable]` is a correspondence claim, not a proof that the diagnostic fires, and a `Proved` disposition could be hollow (the BUG-017 family). This layer closes that gap.

> **Cross-cutting by nature.** Soundness checking is not a pipeline stage — it sits across the proof engine, graph analyzer, type checker, the fault↔diagnostic correspondence, *and* the compiler↔evaluator evaluation-order invariant. It is applied **proof-engine-first, proportional to risk.** That is why it lives here as cross-cutting infrastructure rather than in a single stage doc.

---

## Overview — the two halves

The architecture is two complementary mechanisms, each closing one failure direction:

1. **Witness-checking** (closes *false* `Proved` — a verdict that is accepted but wrong). Every soundness-critical "proved / clean / well-typed / reachable" verdict carries a **legible witness** (a structured certificate), and a small **trusted checker** independently re-verifies it (verify-simple / validate-hard, Verasco-style). A hollow `Proved` cannot survive the checker. The engine may be arbitrarily buggy — soundness rests on the checker, not on the engine being correct.
2. **Catalog-driven coverage** (closes *missing* obligations — a fault-prone site that is never checked at all). A Roslyn coverage analyzer proves, by catalog set-difference, that every fault-prone construct the catalogs declare reaches a live obligation-emission site that actually stamps — at every walked position of every walked construct. A missing handler is a build error, not a latent hole.

*Section forthcoming — filled in the witness/checker and coverage phases of the build plan.*

## The two theorems

- **Soundness theorem** — for every soundness-critical verdict, *if the checker accepts the witness, the verdict genuinely holds.*
- **Coverage theorem** — *every obligation that should be raised, is raised, and reaches a checker.*

Together they are the mechanism behind Principle 11: every fault site raises an obligation (coverage), and every accepted obligation genuinely discharges (soundness). The precise statements, scope (the complete soundness-verdict family), and the trusted-base audit live in the companion **Soundness Case**.

*Section forthcoming.*

## Witness format

The structured certificate each verdict carries — completing spec § 0.6 item 13 (structured proof attribution) and Proof philosophy #6. Keyed by witness *shape* (more shapes than `ProofStrategy` members). Built on `ProofLedger.cs`'s `ProofObligation`/`ProofStrategy`, extending the existing `ProofForwardingFact` convention rather than forking.

*Section forthcoming.*

## The trusted checker

Two tiers — re-execution (verify-simple) for the trivial strategies; named-contributor re-verification (validate-hard) for the relational/qualifier/compositional strategies. Plus the cross-cutting invariants (sequential staleness, cross-unit normalization, structural-suppression). Designed small, pure, side-effect-free so it can later be formally verified.

*Section forthcoming.*

## Catalog-driven coverage analyzer

Built on the `Precept0019PipelineCoverageExhaustiveness` / `Precept0027DiagnosticEmissionCoverage` set-difference model. Closes the obligation-stamping-completeness family at the structural levels (emission-site presence, procedural-synthesizer firing, walk-arm presence-plus-stamp-firing, child-position recursion, construct-position entry-point). Runs in CI so coverage cannot regress.

*Section forthcoming.*

## Trusted base

What is trusted vs. untrusted, the per-component correctness argument, and the size target (the working estimate is ~750–900 LOC of checker + witness model for the current proof strategies). Everything else — the entire obligation-discharge search — is untrusted; a bug there can only over-reject (friction), never falsely accept.

*Section forthcoming.*

## Relationship to conditional totality

Path-sensitive intra-expression narrowing (short-circuit `and`/`or` + the `AssumedConditions`/`EffectiveGuard` mechanism — promoted into the spec, `proof-engine.md`, and `evaluator.md`) is an **additive expressiveness technique made safe by this architecture**: a buggy narrowing yields a false `Proved` the checker rejects. This is the general pattern — the checker is what makes new prover capabilities safe to add. Conditional totality is documented in its own surfaces; this section records only the safety relationship.

*Section forthcoming.*

## Empirical net

The standing harnesses that give independent evidence the theorems hold in the running system: an adversarial false-witness suite (the checker must reject planted false witnesses), mutation testing of the trusted checker (100% kill — the test that justifies trusting it), bounded-model-checking over the finite state space, and differential-vs-runtime testing (gated on the evaluator, a stub today).

*Section forthcoming.*

## Generalization

Proof-engine-first; the witness-plumbing convention and catalog-as-oracle generalize to the graph analyzer (reachability paths, cut-set dominance certificates) and the type checker (the `TypedExpression` typing derivation, retained past the proof-engine boundary). The checkers themselves are phase-specific.

*Section forthcoming.*

---

## Design source (transitional)

Until each section above is promoted here, these in-flight design docs are the authoritative source. They are inlined into this doc as the build phases land, then archived:

- `docs/Working/soundness-and-coverage-architecture-2026-06-07.md` — the locked-candidate architecture design (witness format, checker, coverage analyzer, decisions).
- `docs/Working/conditional-totality-short-circuit-narrowing-2026-06-07.md` — the conditional-totality design (its proof-engine half promotes into the spec / `proof-engine.md` / `evaluator.md`; this doc records only the safety relationship).
- `docs/Working/soundness-case-2026-06-07.md` — the **Soundness Case**, the companion canonical artifact (the living "why the guarantee holds" argument). Promoted alongside this doc and maintained, not archived.
- `docs/Working/soundness-build-plan-2026-06-07.md` — the phased build plan that drives the incremental promotion into this doc.

## Cross-references

- [`proof-engine.md`](proof-engine.md) — the stage whose verdicts this layer checks first; the discharge strategies and `ProofLedger` the witness format extends.
- [`diagnostic-system.md`](diagnostic-system.md) — the `[StaticallyPreventable]` fault↔diagnostic correspondence this layer turns from an assertion into a checked property.
- [`graph-analyzer.md`](graph-analyzer.md), [`type-checker.md`](type-checker.md) — the generalization targets.
- [`../runtime/evaluator.md`](../runtime/evaluator.md) — the evaluation-order invariant (short-circuit) the conditional-totality narrowing depends on.
- `docs/language/precept-language-spec.md` § 0.1 (Principles 7/10/11), § 0.4 (single-pass), § 0.5 (graph over-approximation), § 0.6 (proof contract; item 13 attribution; philosophy #3 legible witnesses), § 0.7 (fault-prevention vs governance).
