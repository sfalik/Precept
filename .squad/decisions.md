# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-06-23T23:24:14Z: Arg→Field Constraint-Propagation Amendment Assessment

**By:** Frank
**To:** Shane
**Status:** ⚠️ Recommendation pending Shane sign-off. Merged from `.squad/decisions/inbox/frank-arg-constraint-propagation-assessment.md`.
**Target:** `docs/Working/compiler-readiness-plan-2026-06-16.md`

- Plan re-review: still sound; no new blockers.
- Recommendation: do not add a new slice for arg-constraint propagation.
- Open item: fold the ingress-arg axis into the existing GATE-F Phase-0 owner conversation.
- Philosophy gap surfaced, not resolved: prevention boundary for ingress arg→field assignment.

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
