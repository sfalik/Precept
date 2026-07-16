---
title: "Canonicalization Plan — Promoting the Go-Forward Posture (v2) into Canon"
status: Draft — 2026-07-14 (Frank's proposal; nothing ratified; NO canon file touched)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
source: docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md
note: >
  Proposal only. This maps each settled point of the v2 posture to the canonical doc it
  belongs in, marks it as a NEW addition or a CORRECTION to existing canon, and states in one
  or two sentences what the canonical text should assert. No file under docs/ outside
  docs/Working/ is edited here. Every candidate that touches docs/philosophy.md is called out
  separately in the PHILOSOPHY GATE section — that file requires Shane's explicit sign-off and
  is never folded in with the rest.
---

# Canonicalization Plan — Go-Forward Posture (v2) → Canon

Shane — read the two headline findings first; they change what this promotion *is*.

## Headline finding 1 — most of the posture is already in canon

The spec (`§0.6` proof contract, `§0.7` guarantee contract), `compiler-and-runtime-design.md §1.2`,
`proof-engine.md` (Decision 1 + §13), and `soundness-and-coverage.md` (§1.1 rationale, §2 verdict, §5
certificate) already carry prove-or-reject, the three-way verdict, the certificate criterion, the
compiler/runtime split, and the §1b-deferral rationale. This is **not** a broad promotion job. It is a
**small correction pass plus a few targeted additions.**

## Headline finding 2 — canon is internally inconsistent on the routing axis, and the wrong side is the one already promoted

`compiler-and-runtime-design.md:125` states the **ratified** framing plainly: disposition — proven vs.
governed — "is decided by **provenance**, not by whether the rule involves a fault." But
`soundness-and-coverage.md:85` (§1.1) states the **overruled** framing: "the boundary is
**proof-carrying-vs-not**." These contradict each other in live canon. The boundary ruling explicitly
rejected proof-carrying/decidability as the router; provenance is the router and decidability is only the
discharge-vs-reject oracle. **The single highest-priority act in this whole plan is correcting
`soundness-and-coverage.md §1.1` to the provenance framing** so it stops contradicting §1.2 and stops
canonizing an overruled distinction. (This is the same drift my own capture recommendation carried; that
doc is corrected in the same pass — see `frank-canonical-capture-recommendation-2026-07-14.md`.)

---

## Section-by-section map (v2 → canon)

For each settled point: **(a)** canonical home, **(b)** NEW addition or CORRECTION, **(c)** what the
canonical text should assert. Points marked **PHILOSOPHY GATE** are owner-sign-off items, collected again
at the end.

### v2 §1 — What Precept is

- **(a)** `docs/philosophy.md`; `docs/compiler-and-runtime-design.md`.
- **(b)** Already canon — no change.
- **(c)** Domain integrity engine; governance-not-validation; no unchecked path; states are the
  coordinate system, stateless precepts first-class. All present. **No action.**

### v2 §2 — What Precept guarantees (+ overflow carve-out and tension)

- **(a)** `docs/philosophy.md` (Principles / guarantee prose); `precept-language-spec.md` Principles 10/11
  (`:110`/`:112`); `docs/compiler/soundness-and-coverage.md §6` (soundness-hole / overflow); optionally a
  one-line pointer in `diagnostic-system.md`.
- **(b)** Guarantee statement: already canon. Overflow present-tense-is-target ruling: **NEW note** in a
  compiler doc. Overflow *strategy space* (fixed-width vs. arbitrary precision): **NEW one-line note.**
- **(c)** Assert once, where the gap-ledger track will see it (recommend `soundness-and-coverage.md §6`):
  representational overflow (`NumericOverflow`, value-vs-type-range) is **parked, GATE-O, post-MVP**; the
  present-tense overflow language in philosophy/spec states the ratified **target end-state** by owner
  ruling, not today's enforced surface, and no doc may claim overflow is prevented today. Add that the
  overflow **strategy is itself unchosen** — fixed-width (trap/saturate/wrap) vs. arbitrary precision (grow
  the representation so overflow cannot occur) — which is a further reason the question is deferred.
  Declared-bound containment (`OutOfRange`, value-vs-declared-`min`/`max`) is a **different fault and stays
  enforced.**
- **PHILOSOPHY GATE:** the present-tense overflow wording lives in `philosophy.md`. It is ruled
  LEAVE-AS-IS. This plan does **not** edit it; it only asks that the *tension be recorded in a compiler
  doc* so the separate drift track does not re-report it. Confirm.

### v2 §3 — The governing model (Hybrid; the enforced fault set; provenance-not-linearity)

- **(a)** `docs/compiler/soundness-and-coverage.md §3` (coverage by fault mode, §3.1); `proof-engine.md`;
  `compiler-and-runtime-design.md §1.2`.
- **(b)** Hybrid + total-over-the-fault-surface: already canon. Enforced fault set incl. the **count/
  cardinality family**: verify it is named in `soundness-and-coverage.md §3.1` and `proof-engine.md`
  Strategy 10 — **CORRECTION only if missing**. Provenance-not-linearity: already canon at
  `compiler-and-runtime-design.md:125`; **NEW cross-reference** from `soundness-and-coverage.md`.
- **(c)** The compiler is **total over the fault surface, not over every declared constraint.** The
  enforced fault set is: division-by-zero; `sqrt`/`pow` non-negativity; empty-collection/index access;
  declared-bound containment of a computed result (`OutOfRange`); and cardinality/count-bound containment
  (`CountBoundViolation`/`LengthBoundViolation`). A **declared relational invariant over independently-set
  fields is governed, not proven** — because it declares a relationship rather than computing a derived
  value — and this is true whether the invariant is linear or nonlinear: **linearity is not the routing
  axis; provenance is.** State plainly that the strengthened engine (multi-term facts, case reasoning,
  money ×/÷, author-stated fact via §6) is **designed, not built** — a claim that "the compiler proves
  everything decidable" overclaims both today's build and the target.

### v2 §4 — The discriminating axis (Obligation-Role Rule → provenance)

- **(a)** `docs/compiler/soundness-and-coverage.md §1.1` (**the correction target**); optionally a new
  short subsection in the same doc stating the Obligation-Role Rule; `compiler-and-runtime-design.md §1.2`
  (already aligned).
- **(b)** **CORRECTION — highest priority.** Replace the "proof-carrying-vs-not" paragraph with the
  provenance framing. Optionally **NEW**: a compact statement of the two obligation families + the oracle.
- **(c)** Disposition is a property of an **obligation, never of a construct** (Obligation-Role Rule).
  Every declared constraint generates **both** a governance obligation and a premise obligation — the
  carries-proof/does-not-carry-proof split **dissolves**; a constraint is never routed, it always does
  both. Every fault-prone operation over a **definition-derived** value carries one **prove-or-reject**
  obligation, routed **by the origin/shape of the value it consumes, never by how hard the obligation is
  to prove.** **Decidability is the discharge-vs-reject oracle, not the router.** The operational
  shorthand is **provenance**: derived-by-a-Precept-expression → prove-or-reject; supplied-from-outside or
  a declared relational invariant → govern. Explicitly retire the sentence "the boundary is proof-carrying
  vs. not" — it was the historical dispute-fence, not the router, and the boundary ruling rejected it.
  Keep "old D2 (band-vs-rule) is dead; a bound is a rule" — that part is correct and stays.

### v2 §5 — Runtime governance (two mechanisms; contract-path scope)

- **(a)** `precept-language-spec.md §0.7` (Guarantee Contract) and `§3A.4`; `docs/runtime/result-types.md`;
  `docs/runtime/runtime-api.md`; `docs/runtime/fault-system.md`.
- **(b)** Ingress governance + composition seam: already canon. The **ingress-vs-sweep** decomposition of
  the govern route: **CORRECTION/elaboration** — currently `§0.7` describes governance as ingress only.
  Out-of-contract evaluator traps as a **third zone, not a third governance mechanism**: already canon in
  `fault-system.md`; verify wording.
- **(c)** The two governance mechanisms — **ingress** (an externally-supplied value checked against its own
  carried constraint at entry) and the **post-mutation sweep** (a declared relational invariant over
  independently-set fields enforced on the resulting whole-entity state) — govern the **contract path
  only.** Restored/hydrated state is trusted at persistence time and re-governed on the next operation;
  host-injected/out-of-contract data can reach the `[StaticallyPreventable]` evaluator traps, which are
  **defense-in-depth for out-of-contract entry — unreachable for contract data; a trap firing on contract
  data is a proof-engine defect, not an accommodated path.**
- **OWNER-GATED SPEC SURFACE (not philosophy):** whether `§0.7`'s Governance paragraph should be synced to
  **name the sweep** (it currently names ingress only) is an open owner question — v2 §13 Q1. This plan
  **flags it; it does not rewrite `§0.7`.** Confirm the direction before any `§0.7` edit.

### v2 §6 — Outcome vs. epistemic guarantee

- **(a)** `docs/compiler-and-runtime-design.md §1.2` (mechanism/epistemics); candidate for a sentence in
  `philosophy.md` — **PHILOSOPHY GATE**.
- **(b)** **NEW clarifying assertion** in a compiler doc; philosophy candidate flagged, not written.
- **(c)** The **outcome** guarantee (no invalid configuration commits) is identical on both the proven and
  the governed paths; the **epistemic** guarantee differs — a proven site is known-safe before any entity
  exists; a governed site is made-safe at runtime on every operation. State these **separately**;
  collapsing them into "runtime governance is the same guarantee" hides a real distinction. There is **no
  GOVERNED disposition without real runtime enforcement behind it.**
- **PHILOSOPHY GATE:** whether `philosophy.md` should carry the outcome-vs-epistemic distinction in words
  is Shane's call. My lean: **no** — philosophy states the guarantee; the epistemic nuance belongs in the
  compiler doc. Flag only.

### v2 §7 — The diagnostic / legibility surface

- **(a)** `precept-language-spec.md §0.6` (three-way verdict — already present); `soundness-and-coverage.md
  §2`; `docs/compiler/diagnostic-system.md`; `docs/compiler/tooling-surface.md`; `docs/tooling/mcp.md`
  (deferred to Slice 0).
- **(b)** Three-way verdict + `spec:223` rescope + Warning→Error severity flips: already canon (verify
  severity flips are recorded in `diagnostic-system.md`). **Per-obligation PROVEN/GOVERNED disposition
  surface** and the **false-security constraint**: **NOT canon-assert yet** — route to `/design`.
- **(c)** Canon already carries the three-way verdict (Proven / ProvenViolating-with-witness /
  Unresolved-with-weakest-precondition, both non-proven verdicts block) and the "no DEFERRED state" rule.
  Assert (if not already) that a provably-always-violating derived write on a reachable row is a hard
  **Error**, and that the six structural-severity Warning→Error flips stand. **Do NOT** promote the
  inline/ambient disposition surface or the false-security acceptance criterion as settled — they are a
  **ratified-but-unvalidated legibility hypothesis routed to `/design`** (v2 §13 Q3). Documenting them as
  solved would itself be a false-security failure.
- **DEFERRED-TO-SLICE-0:** `mcp.md`'s certificate/verdict projection into `precept_proofs`/`precept_compile`
  updates when Slice 0 ships the `ProofVerdict` DU + projection — **not before** (aspirational-claim rule).

### v2 §8 — Certificate criterion + stopping rule

- **(a)** `precept-language-spec.md §0.6`; `docs/compiler/soundness-and-coverage.md §1`/`§5`;
  `proof-engine.md` Decision 1 + §13.
- **(b)** Already canon — four legs verbatim in `§0.6`, Decision 1 carries the rationale, §13 carries the
  §1b "never an expressibility gain" reasoning.
- **(c)** **No action** beyond confirming `CertificateSteps` catalog **membership** and **OQ1** are marked
  as unruled/blocking the `Proven` mint (v2 §13 Q4). Those are open-question tracking, not new assertions.

### v2 §9 — Deferred / parked surface

- **(a)** `proof-engine.md §13` (§1b — already there); `soundness-and-coverage.md §3.3`/`§6`;
  proof-engine `/design` backlog for aggregates and compute-then-check.
- **(b)** §1b-deferral: already canon and correct. **Band-suppression / escape-hatch-closed tradeoff:**
  candidate **NEW** note in `soundness-and-coverage.md` (the accepted cost of closing #9). Aggregates and
  compute-then-check: **NOT canon-assert** — they are `/design` backlog items.
- **(c)** If promoted at all, assert only: closing the escape hatch (no author-visible "trust me" marker;
  an unresolved derived obligation rejects) **accepts the band-suppression cost** — an author who cannot
  discharge a bound may delete it — deliberately, on the ground that a visible over-rejection at authoring
  time beats a silently-shipped unanswered case, and §6 does not neutralize it. Aggregates/compute-then-check
  stay parked as `/design` items and are **not** written into canon as either shipped or rejected.

### v2 §10 — Banned framings

- **(a)** `soundness-and-coverage.md §1.1` (rejected-alternatives record); `proof-engine.md` Decision 1.
- **(b)** Some already canon (Option-B rejected, no escape hatch, D2 dead). The **routing-axis bans**
  (no spelling router; proof-carrying is not the router; decidability is the oracle not the router) ride
  the §1.1 **CORRECTION**. The **process** bans (honest-on-old-engine, engineering-wall-as-identity-crisis,
  dirty-Slice-0, one-more-matcher) are **retrospective/process material — NOT canonical posture.**
- **(c)** In the §1.1 correction, add the one-line rejected-alternatives: routing by spelling, by
  proof-carrying-vs-not, or by decidability are all wrong; same provenance ⇒ same disposition; decidability
  is the discharge oracle. Do **not** promote the process/anti-spiral bans into canon — see §11.

### v2 §11 — Why this was hard-won / anti-spiral / build-sequencing

- **(a)** Stays in `docs/Working/` (retrospective) or a `docs/contributing/` process note — **NOT** the
  language/compiler canon.
- **(b)** **NOT canonical posture.** This is engineering-discipline history (the 2c-ii diagnosis, the two
  P0s, hold-the-parking-lines). It grounds *how we build*, not *what Precept is*.
- **(c)** No canon action. Keep it in the retrospective; if a durable home is wanted, a contributing-guide
  process note is the right surface, not a spec/compiler doc. Flag for Shane if he wants it preserved
  outside Working.

### v2 §12 — Deliberately out of scope

- **(a)** N/A — plan/ledger tracking (gate tracking, Slice-0 mechanics, positioning copy, superseded
  proposals). Stays in the readiness plan / decision ledger.
- **(b)** **Not canon.** No action.

### v2 §13 — Open questions for Shane

- **(a)** N/A — owner-gated decisions, not canonical assertions.
- **(b)** **Not canon.** These four (§0.7 sweep-sync; capture-doc drift correction; disposition-surface
  `/design` routing; certificate-format/OQ1 scope) gate the corresponding canon acts above. Resolve them
  first; several canon edits in this plan are downstream of them.

---

## Proposed sequencing

1. **CORRECT `soundness-and-coverage.md §1.1`** proof-carrying → provenance (Headline finding 2). Highest
   priority; removes a live canon self-contradiction. Downstream of v2 §13 Q2 confirmation.
2. **Add the routing-axis rejected-alternatives** one-liner to the same §1.1 correction (v2 §10).
3. **Verify** the count/cardinality family (§3), the severity flips (§7), and the out-of-contract-trap
   wording (§5) are present; add only if missing.
4. **Add the overflow present-tense-is-target + strategy-unchosen note** to `soundness-and-coverage.md §6`
   (v2 §2). Not a philosophy edit.
5. **Route to `/design` (not canon):** the disposition surface + false-security (§7), aggregates and
   compute-then-check (§9).
6. **Defer to Slice 0:** `mcp.md` verdict/certificate projection (§7).
7. **Owner-gated, do not touch until ruled:** `§0.7` sweep-sync (v2 §13 Q1); any `philosophy.md` wording.

Steps 1–4 are a single small direct doc-sync pass into existing scaffolds — **not a `/promote` run.** The
source is a ratified decision ledger spanning many Working artifacts and the target sections already exist;
a focused rationale-correction into an existing skeleton is the right mechanism.

---

## PHILOSOPHY GATE — collected, for Shane's explicit sign-off

`docs/philosophy.md` requires Shane's explicit approval; nothing below is written by this plan.

1. **Overflow present-tense language (§2).** Ruled LEAVE-AS-IS (states the ratified target end-state). This
   plan does **not** edit it; it asks only that the tension be *recorded in a compiler doc* so the drift
   track does not re-open it. Confirm the LEAVE-AS-IS stance holds.
2. **Outcome-vs-epistemic distinction (§6).** Candidate philosophy sentence. My lean: **do not** add it —
   philosophy states the guarantee; the epistemic nuance belongs in the compiler doc. Confirm.
3. **Naming "prove-or-reject" / "certificate" / PROVEN-vs-GOVERNED in philosophy.** Philosophy currently
   does not use these terms. My lean: **leave as-is** — the spec owns the vocabulary. Confirm.

No other part of this plan touches `philosophy.md`.

---

## Bottom line

The posture is **already largely in canon.** The real work is a **small correction pass**, and its
single most important act is fixing the `soundness-and-coverage.md §1.1` **proof-carrying → provenance**
drift so canon stops contradicting `compiler-and-runtime-design.md §1.2`. Everything else is verification,
three small additions, and correct deferral of the genuinely-unbuilt surface to `/design` and Slice 0.
Nothing here promotes the anti-spiral/process material or the unvalidated legibility hypothesis into canon,
and nothing touches `philosophy.md` without the explicit gate above.

— Frank
