# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-06-23T23:24:14Z: Arg→Field Constraint-Propagation Amendment Assessment

**By:** Frank
**To:** Shane
**Status:** Advisory design opinion — not pending Shane sign-off; merged from `.squad/decisions/inbox/frank-arg-constraint-doc-reframe.md`.
**Target:** `docs/Working/compiler-readiness-plan-2026-06-16.md`
**Persisted assessment:** `docs/Working/arg-constraint-propagation-opinion-2026-06-23.md`

- Reframed per owner direction (2026-06-23) from “plan amendment to fold into GATE-F” to an advisory design opinion; verdict = oppose-as-posed; recommend the inverse (infer-band / reject-provable / govern-rest).
- The earlier “pending Shane sign-off” framing applied to the amendment; this advisory opinion is on the merits, not a ratification item.

---

### 2026-06-16T13:06:00Z: Architectural Review: Compiler Readiness Plan (2026-06-11)

**By:** Frank
**To:** Shane
**Status:** Merged from `.squad/decisions/inbox/frank-compiler-readiness-plan-review.md`.
**Target:** `docs/Working/compiler-readiness-plan-2026-06-11.md`
**Review:** `docs/Working/compiler-readiness-plan-2026-06-11-frank-review.md`
**Companion research:** research/architecture/compiler/fault-floor-definition-2026-06-11.md; docs/Working/compile-time-niche-decision-packet-2026-06-10.md

- Verdict: Sound with required revisions.
- Blocking findings: B1 GATE-F under-enumerates the owner-gated canon-amendment set; B2 `DesugarsToRule` is not literally zero-consumer today but has zero `src/Precept/` consumers; B3 28-vs-32 prior-docs provenance mismatch.
- Advisory findings: A1–A7 folded into the review.
- Verified all 13 load-bearing claims against source.
- Coordinator note: `.squad/config.json` model overrides for Frank and Elaine were bumped to `claude-opus-4.8`.

---

### Compiler-Readiness Plan 06-16 — Amendment Verification (B1 Clearance)

**By:** Frank
**Date:** 2026-06-21T10:38:00-04:00
**Status:** ✅ B1 CLEARED — plan is sign-off-ready. Phase-0 gate ratification remains Shane's.
**Target doc:** `docs/Working/compiler-readiness-plan-2026-06-16.md` (amended after my 06-16/06-21 re-review)
**Review record:** `docs/Working/compiler-readiness-plan-2026-06-16-frank-review.md` § Amendment verification — 2026-06-21
**Requested by:** Shane (owner)

---

## Decision

The amendment **clears B1** and addresses A1 and A3. The plan is **READY FOR OWNER SIGN-OFF on the Phase-0 gate set.** Verified by source-check, not by the doc's self-description.

## Per-finding disposition

### B1 — CLEARED (blocking → resolved), both legs, correct architectural shape

- **(a) DoD-4 verification scope widened.** Plan `:1679` now scopes the "no canon sentence claims overflow prevented" grep over `docs/` **AND** `src/Precept.Analyzers/*.cs` analyzer message/description strings **AND** the `Diagnostics.cs`/`Faults.cs` *prevention*-claim strings. Correctly scoped to **prevention-claims**, not fault *message* templates (a fault's own message legitimately names the fault; only a prevention claim is the false canon-in-code sentence).
- **(b) PRECEPT0002 reconciliation enumerated as an in-scope code-as-canon edit in Slice 6.2.** Work `:1044`; exit criterion `:1051` (the description no longer asserts universal delivered prevention; a `src/Precept.Analyzers/*.cs` grep finds no in-code sentence claiming `NumericOverflow` prevented/unreachable); doc-touch `:1053` lists it explicitly as a **"(B1) Code-as-canon edit"** on `Precept0002FaultCodeMustHaveStaticallyPreventable.cs`.
- **Architecturally sound.** The fix rewords to the **linkage invariant** ("every fault must reference its preventing diagnostic") / adds a **`known-not-prevented` carve-out** — it does **not** delete the honesty requirement, and it keeps the attribute (NumericOverflow is still bijective and emitted). Source-confirmed: the live analyzer over-claims at `Precept0002FaultCodeMustHaveStaticallyPreventable.cs:24-25` ("makes this fault unreachable at runtime"); the carve-out precedent it mirrors exists at `StaticallyPreventableMap.cs:18-26` (the design-time-linkage docstring for `UnexpectedNull`); `NumericOverflow` is in `BijectiveDiagnosticCodes` (`StaticallyPreventableMap.cs:34`).

### A1 — CLEARED

General honesty-sweep principle named at plan `:1045`: when a decision relaxes a guarantee, **every enforcing surface** — prose canon **and** the analyzer family (`PRECEPT0002`/`0027`/`0029`/`0019`) — is part of the sweep, not docs alone. This B1 fix is the first instance.

### A3 — CLEARED

`CollectComputedFieldBoundObligations` (`ProofEngine.Analysis.cs:534`) named at plan `:281` with the **target-state-not-true-at-HEAD** framing: today it stamps a live `IntervalContainmentProofRequirement` → `NumericOverflow`; the "no live obligation" gap becomes true only **after** Slice 1.3 reclassifies the band IntervalContainment. Source-confirmed against `ProofEngine.Analysis.cs`.

## Regression check

- **Boundary clean.** Both added verification surfaces (DoD-4 grep `:1679`; Slice 6.2 exit `:1051`) are static string-greps over analyzer source — not runtime-execution assertions. Slice 6.2 `:1052` keeps "runtime emits the fault" out (deferred evaluator facet, §11). No new boundary leak.
- **Coverage ledger reconciles.** Registers (16/8/10/236/43) and §12 re-reconciliation untouched; clauses added inside existing slice bodies without renumbering or dropping a row.
- **D1/D2 distinction intact and reinforced.** Slice 1.3 `:268` keeps A6 relocation distinct from D1 relaxation; the new B1 text reconciles only the overflow (D1) prevention claim, leaving the linkage invariant and band/bijective machinery (D2) standing.

## Final call

**READY FOR OWNER SIGN-OFF on the Phase-0 gate set — Yes.** No residual blockers. The Phase-0 gate ratification itself — GATE-F's owner-gated spec amendments, the `philosophy.md` overflow reconciliation, GATE-I's fault-delivery pick — remains Shane's, as it must be.

— Frank
# Decision note — Band-model ruling: Phase-0 corpus map & frozen question set

**Author:** Frank (Lead/Architect) · **Owner:** Shane · **Date:** 2026-07-06 · **Phase:** 0 (setup — no ruling)
**Branch:** `spike/Precept-V2-Radical`

## What this note records
Phase-0 of the Shane-approved bounded research operation that will rule on **which guarantee model governs Precept**. Phase 0 only *frames* the evidence hunt; the ruling is Phase 3, Shane ratifies Phase 4. Nothing here decides the model.

## Primary axis (Q1) — the frozen fork
The ruling's primary axis is a three-way meta-fork; every other question resolves as a consequence:

- **Model A — Bounded compiler + runtime governance.** Compiler proves a subset; non-discharged bounds are *governed at runtime*. Partial compiler + stopping rule. Precedent: gradual verification, SPARK/Ada. Risk: false promise + runtime-enforcement limits.
- **Model B — Total compiler + deliberately limited surface.** Not-statically-decidable ⇒ **inexpressible**; author rewrites into the provable subset. "Compiles clean" = "totally proven." Cost: expressive power. Philosophy-native.
- **Principled hybrid — total over the definition-internal surface, with an honest, typed, VISIBLE governance boundary only at genuinely-external runtime input** (§0.7:268 surface). No silent partial-proof.

Q2–Q11 (stopping/admissibility rule; expressiveness cost of B; runtime-enforceability of the deferred set; proven-violation severity; default-vs-set-action consistency; merely-unprovable case; carries-proof bounds; false-promise resolution; philosophy over-read + GATE-F spec amendments; taxonomy/adjacent-calls) are each framed "under A / under B / under hybrid" so readers hunt for *discriminating* evidence, not history summaries. Added sub-questions: Q1a (provenance of the debate's own authority), Q2a (is D2 even shipped — finding: proposal-only), Q3a (does §0.6:249 already show Model B live for the string-length axis), Q4a (does `ConstraintsFailed` distinguish external-input governance from deferred internal-band enforcement).

## Two ground-truth discriminators (verified first-hand at HEAD)
1. **Spec §0.7:266 says "there is no deferral"** and §0.6:258 says count bounds are "never deferred to a runtime check." Canonical spec is presently written B/hybrid-shaped; D2 (`decision-index.md:20`) would amend it toward A (that amendment IS GATE-F).
2. **D2 band-split is proposal-only, not shipped.** At HEAD `CountBoundViolation`(:1295) / `LengthBoundViolation`(:1283) are `Severity.Error`; PRE0136 fires even on the merely-unprovable case. Readers must not describe D2 as current behavior.

## Deliverables & locations
- **Reader-brief template + full corpus map + frozen question set:** `~/.copilot/session-state/21c4af47-5141-4752-ac79-6938d5c2e269/files/phase0-corpus-map-and-brief-spec.md` (Sections 1–4).
- **Fleet:** 6 readers (A niche packet · B fault-floor+precedent · C 06-16 plan/D1–D4/GATEs · **D reserved-crux+runtime-enforceability, owned by George** · E 2026-07-06 convergence [verify-don't-inherit] · F expressiveness ledger) + **anchor G** (primary-source/provenance/git-blame loop-breaker).
- Corpus gaps I fixed vs the plan: added the expressiveness-cost corpus (F), the prior-art precedent surveys (B), and an end-to-end runtime-enforceability owner (D/George); corrected the plan's "§0.7:258" → §0.6:258.

## Guardrails carried
Owner-gated surfaces (`philosophy.md`, spec §0.6/§0.7, GATE-F/GATE-H amendments) are **recommend-only** until Shane's one-pass ratification (Phase 4). No spec/philosophy edit in this operation. Fresh re-derivation (D-A): the 2026-07-03 and 2026-07-06 docs are claims to verify, not a starting position.

---

---
title: Runtime CANNOT enforce a deferred internal declared band — Model A escape hatch is a false promise at the enforcement layer
author: George (Runtime Dev)
date: 2026-07-06
operation: Band Enforcement — Compiler Guarantee-Model Ruling (Model A vs B vs hybrid)
slice: D (reserved crux + runtime-enforceability reality)
status: Evidence finding for Frank's Phase-3 ruling — NOT a ruling. Stated as a hard yes/no per mandate.
brief: session-state/.../files/briefs/brief-D-george-runtime.md
---

# Hard finding (Q4)

**The runtime CANNOT reliably enforce a deferred internal declared band** (`min`/`max`/
`minlength`/`maxlength`/`mincount`/`maxcount`) — because a declared band has **no runtime
governance representation at all**, and even the rule/ensure governance path it would
share is unbuilt at HEAD.

## Why (primary-source spine)

1. **Bands are not a runtime constraint kind.** `ConstraintKind` (`src/Precept/Language/ConstraintKind.cs:9-24`)
   = `Invariant` (rule) + `StateResident`/`StateEntry`/`StateExit` (ensures) + `EventPrecondition`.
   No band kind. A band cannot become a `ConstraintDescriptor` — that type requires an
   `ExpressionText` + `Because` a band declaration lacks (`src/Precept/Runtime/SharedTypes.cs:42`).
2. **`ConstraintsFailed` covers rules + ensures only.** `docs/runtime/result-types.md:114,121`
   ("Covers ALL post-fire constraints: global rules, state ensures, AND event ensures"). One
   undifferentiated governance path; bands are on **none** of it (answers Q4a: the disposition
   surface has nothing to render for a deferred band).
3. **The §7.6 sweep evaluates only rule/ensure buckets.** `docs/runtime/evaluator.md:1326-1360`,
   `:1695-1698` ("Every `ConstraintDescriptor` appears in exactly one bucket").
4. **Band proof is compile-time-only.** `docs/compiler/proof-engine.md:107` — "Proof ledger does
   NOT cross the compile-runtime boundary — only `FaultSiteDescriptor` residue (defense-in-depth
   backstops) crosses into runtime." An internal-computed band violation is a **compile-time**
   `NumericOverflow` Error (`docs/compiler/diagnostic-system.md:163`; `Diagnostics.cs:697/1283/1295`
   all Error at HEAD).
5. **The only runtime residue is a defense-in-depth fault, framed as a compiler defect.**
   `FaultCode.OutOfRange` (`src/Precept/Language/FaultCode.cs:47-48`); `docs/runtime/fault-system.md:280,316`
   — "reachable only for out-of-contract data … a fault on contract data would indicate a
   proof-engine gap — a defect to fix, not a condition to design around."
6. **No band-deferral seam exists.** The only "deferred" seam in the evaluator is lazy collection
   materialization (`docs/runtime/evaluator.md:1280`, "DEFERRED — do NOT implement"). Unrelated.
7. **The commit pipeline is stubbed.** `Version.cs:81` (`Fire` → `UndefinedEvent()`), `Version.cs:85`
   (`Update` → `NotImplementedException`), `Precept.cs:158` (`Constraints` → `NotImplementedException`),
   `Evaluator.cs:46` ("TODO: implement Fire/Update once the executable model is designed").

## Bearing on the ruling

- **Undermines Model A.** Its "defer to runtime" escape hatch requires a runtime band-enforcement
  surface that is neither designed nor shipped, and that §0.7:266 ("there is no deferral") forbids.
  The governance-checkpoint view (`band-guarantee-boundary-analysis-2026-07-03.md:114`) *assumes*
  "Fire refuses the write"; the runtime canon **refutes** the premise.
- **Consistent with B / already-hybrid.** Shipped reality = compile-time prove-or-reject for bands;
  runtime governance for rules/ensures + §0.7:268 external-input ingress. That *is* the hybrid line.
- **Cost of choosing A anyway:** must first commission a runtime band-governance surface (lower
  bands → `Invariant` constraints, or add an ingress band-validation pass) AND amend §0.7:266.
  Owner-gated; out of scope for a design pass.

*Neutral on the final model choice. Frank rules in Phase 3; Shane ratifies. This is evidence, stated hard per the Slice-D mandate.*
