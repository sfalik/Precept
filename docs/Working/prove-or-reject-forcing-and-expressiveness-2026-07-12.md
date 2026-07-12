---
title: "Pure Prove-or-Reject on the Merely-Unprovable Cell — Q1 (forces-better?) & Q2 (expressiveness/proof-engine limits)"
status: Draft — 2026-07-12 (independent analysis; not owner-ratified)
author: Independent analysis (Fable), orchestrated by Claude
owner: Shane
frame: DESIGN — proof engine at its full design ceiling (solver-free, legible, no SMT); current-engine gaps and the unbuilt runtime have NO bearing. End-state.
questions:
  - "Q1: does pure prove-or-reject (Error, no escape hatch) always force BETTER precept?"
  - "Q2: does it limit expressiveness / hit fundamental proof-engine limits?"
provenance: prove-or-reject-analysis-supporting-2026-07-12/ (6 probes + adversary)
note: Nothing here is owner-ratified. Any philosophy/spec change implied is an owner-gated recommendation, quoted and unapplied.
---

# Pure Prove-or-Reject on the Merely-Unprovable Cell — Synthesis

**Cell:** a definition-derived value (`set F = E` / `F <- E`) written into a non-proof-carrying declared band, where a proof engine at its full design ceiling can prove neither containment nor violation. **Posture:** compile-time Error, no escape construct. All reasoning is end-state (compiler + runtime complete) and design-ceiling (current-engine gaps never counted as unprovability). Spec citations below were re-verified against `docs/language/precept-language-spec.md` at HEAD during this synthesis.

---

## 0. The structural result everything else hangs on

At the design ceiling, interval arithmetic over **independent, bounded operands is essentially exact** for the entire catalog (all catalog numerics are piecewise-monotone; there are no transcendentals to defeat — the function catalog is closed, `precept-language-spec.md:1599` region / §3.7). Consequence, confirmed independently by three probes (P2 §0, P3's discriminator, P4 §2) and unbroken by the adversary:

> **When the engine says "unprovable" over independent operands, a genuinely reachable violating scenario exists.** "Merely-unprovable-but-actually-safe" is therefore confined to cases whose safety rests on a **correlation, cancellation, or cross-write invariant** that the ceiling deliberately excludes — nonlinear/piecewise algebra (SMT-class, excluded at spec:225), beyond-depth-bounded relational facts (spec:256), or undeclared inductive invariants (spec:174 item 7, one-file proof facts).

This single fact sorts almost the entire universe, and it is why the two questions converge on the same boundary.

---

## Q1 — Does pure reject always force a better precept?

### Verdict: **EXCEPTION CLASS** — not universal. The distinguishing predicate the owner would decide on:

> **Is the declared band mathematically reachable by the derived expression under the operands' true ranges?**
>
> - **Reachable** → rejection is a true positive; the forced remediation (guard+reject row, clamp, source bound) is a genuine, previously-unanswered domain decision. **FORCES-BETTER — no counterexample found.**
> - **Unreachable-but-unprovable** (the value is always in-band as a matter of algebra, but the proof needs ceiling-excluded reasoning, **and no source-side annotation terminates the obligation**) → the only compiling forms are (a) delete a true, auditor-meaningful bound, or (b) a guard whose else-branch is provably dead — a fictional refusal with the formula duplicated. **FORCES-WORSE.**

The italicized termination clause is load-bearing: in every forces-better case, remediation *terminates* in a domain fact (a floor leg, a final-payment plug, a capacity guard, a sign declaration). In the exception class, remediation *relocates* the identical unprovable step until it hits already-bounded ingress and dies.

### Evidence for the dominant population (forces-better)

**Corpus (P1, 238 derived writes × bounded targets, all inspected):** strict forces-worse occurred **zero** times in 77 samples. Unguarded subtractions into `nonnegative` (`samples/inventory-item.precept:167`, `samples/saas-license-management.precept:199`) are the overdraft exemplar occurring in the wild — each rejection surfaces a real missing rule sibling rows already have, or a real refuse-vs-signed-register decision. Authors already write the proof witnesses as business rules 258 times (`saas-trial-to-paid.precept:126-128`, `global-meeting-scheduler.precept:197-203`, etc.) — because they ARE the domain rules. Decisive census fact: exactly **one** money field in the entire corpus carries a `max` (`samples/Test.precept:5`); the corpus cannot by itself represent the universe (P1's own caveat: real authors may band more aggressively — the constructed probes found exactly the members the corpus lacks).

**Beyond-corpus, constructed adversarially (P2 finance, P3 science/health, P3b ops/pricing):** Reg-T margin (rejection forces the missing FINRA max-floor leg), amortization balance (the "always safe" intuition is *false under place-bounded decimals* — the forced final-payment plug is real servicing behavior), accumulated depreciation (final-period plug is actual GAAP), IBNR nonnegativity (rejection exposes that the bound itself is actuarially wrong — negative IBNR is real), weight-based dosing with a hard ceiling (`min(rate*weight, '1000 mg')` *is* the clinical protocol), actuator saturation, calibration-error mis-modeling (the compiler correctly refuses to let the author *declare* that measured reality stays in band — routing to a Failed state is the fix), warehouse capacity, discount stacking, token-bucket limits.

**Failed break attempts (the adversary's evidence for the dominant claim):**
- P3b's flagship forces-worse (blended-APR usury cap) **did not survive** — mis-constructed: with one tranche at `Rate2 max 0.32` against `BlendedAPR max 0.30`, the band is *reachable* (degenerate weighting), rejection is a true positive, and the self-guard `when <expr> <= 0.30` discharges by congruence without any invented principal ceiling. Its two corroborators collapse the same way.
- P2's weighted-average-cost band: downgraded honestly to NEUTRAL (the band is a plausibility check no one operates on).
- P3's convex-blend case (`f·C1 + (1−f)·C2` into `max 100`): a nameable, legible, solver-free identity — **merely-unimplemented at the ceiling**, not fundamental; likewise multilinear corner enumeration and part-of-whole ratio bounds. P3's own best constructions did not survive as fundamental.
- The monitoring-KPI-as-bound case (P3b): fails the differential test — GOVERN refuses the same legitimate write at runtime; it's a mismodeling finding under either posture, not evidence against reject.

### The surviving exception class (members, after the adversary's filter)

1. **Closed-form always-in-band algebraic identities.** Exemplar (P2 §6, survived adversarial scrutiny): `EffectiveRate = TaxDue / GrossIncome` into `max 0.37` where `TaxDue` is a constants-only piecewise bracket formula. Mathematically `< 0.37` for all inputs; proving it needs cancellation of `GrossIncome` through a piecewise numerator — symbolic algebra outside "standard interval arithmetic, bounded relational closure" (spec:174) and inside the opaque-witness exclusion (spec:225). No source bound terminates: `rule TaxDue <= GrossIncome * 0.37` relocates the identical obligation to `TaxDue`'s own write. Flips to FORCES-BETTER the moment bracket rates become *data* (the dead else-branch goes live against a fat-fingered rate table) — the class members are constants-only formulas.
2. **Manual aggregate mirrors** (`Count`/`Total` shadowing a collection, ~6 corpus sites, P1 Class 4). The justifying invariant (`Count == collection sum/cardinality`) is a cross-write inductive fact with no declared premise to derive it from (spec:174 item 7) — fundamental, not a TODO. The adversary correctly reclassified this from P1's "NEUTRAL" to the same shape as class 1 (always-in-band, real invariant, only remedy a redundant always-true guard). **Compounding fact (verified):** `sum`/`reduce` are *deliberately held back* from the collection surface (`docs/language/collection-types.md:860`), which is *why* authors hand-maintain mirrors — the friction is structurally induced by a separate language decision. (Cardinality mirrors escape via restructure: `<- Emails.count` + `maxcount` on the set; sum mirrors do not.)
3. **Irreducible non-stateable nonlinear correlations** (P3 attempt 5b residue, P6-filtered): safety resting on a correlation that is a real practice but not an affine fact (e.g. "large tranches always carry the safer rate"), or tight bounding requiring nonlinear optimization over a non-box region. Genuinely CAD/real-closed-field territory, excluded under any solver-free ceiling. Narrow: requires (a) nonlinear self-correlation, (b) not reducible to any legible identity, (c) always-in-band, (d) a real band. Most physical/business bounds fail (c) — they are reachable safety ceilings.

**Ceiling-conditionality (owner-gated):** under a *generous* reading of the ceiling (any legible solver-free transform — polynomial division, convex-combination recognition, corner enumeration), class 1 shrinks toward empty and class 3 is the whole residue. Under the *literal* spec:174 reading, class 1 stands. Where the owner draws this line materially resizes the exception class. A second ceiling question with ~10 corpus sites riding on it: whether first-match row-complement narrowing (fall-through rows inherit negation of earlier guards) is in-ceiling (P1 assumption 1 — deterministic and legible, so plausibly yes; if no, those sites become pure duplication friction).

---

## Q2 — Does the posture limit expressiveness / hit proof-engine limits?

### Verdict: **No computation class is removed; exactly one *capability* is removed** — and it is not a computation.

**(a) Precisely when nonlinearity/complexity defeats the ceiling engine.** "Nonlinear ⇒ unprovable" is false. The engine is defeated in exactly two situations (P4 §2, adversary-confirmed):
1. **An unbounded operand** — ⊤ absorbs (`[0,∞)×[0,20] = [0,∞)`). The dominant real case: 84% of corpus numeric fields unbounded; 214:10 unbounded:bounded money (`research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md:120`).
2. **A needed nonlinear/piecewise cross-field fact not stated verbatim** — the engine can *check* a stated nonlinear fact by expression-congruence and *compute* exact nonlinear images over bounded boxes; it cannot *derive* an unstated nonlinear consequence (cancellation, correlation, piecewise algebra).

Not defeated by: bounded-operand products/powers (exact monotone images), transcendentals (none exist — closed catalog), **accumulation** (`Balance = Balance + x` into a band is *genuinely-violable*, not merely-unprovable: the band is a sound inductive entry premise via §3A.4 atomicity, and a concrete violating sequence exists — rejection there is a true positive, out of this cell). Relational needs beyond the octagon fragment are mostly stateable as rules (spec:256's mechanism), with the 3+-variable linear case hanging on an interpretive fork: Fourier–Motzkin elimination produces explicit certificate chains — legible — so whether spec:225 bans *decision procedures* or *opaque witnesses* is an **owner call** (the current text bans opacity: "Proof witnesses must be structured data, not opaque solver traces").

**(b) UNWRITABLE vs FORCEABLE — the honest list.**

FORCEABLE (computation retained; annotation forced), with universal escapes at the ceiling — E1 self-guard (`when E <= B`, always syntactically available because chains are loop-free and pure, discharging by congruence with zero arithmetic even for nonlinear `E`), E2 `clamp` (catalog surface, spec:1574 region), E3 source bound, E4 rule on *ingress* fields:
- Unbounded-operand arithmetic (E1/E2/E3 — note E1/E2 avoid inventing false ceilings on open-ended inputs; the write-site cap *is* the policy band's meaning, dissolving the "unnatural money max" objection at the write site)
- Stated nonlinear cross-field facts (congruence)
- Invariant-protected accumulation (E4: `rule Withdrawals <= Deposits` — octagon-representable, ingress-governed)
- 3+-var linear relations (E4/E1; or resolve the FM fork and it proves natively)
- Non-convex facts (guard rows are native disjunction)
- Mid-chain compositions (E1 with the substituted expression; witness-size cost to the §0.8 audience noted)

UNWRITABLE — the adversary's decisive correction of P4's "none":
1. **A compiler-checked true invariant on a derived value in the exception class.** P4's escape E4 is **circular for derived fields**: `rule EffectiveRate <= 0.37` is not an ingress premise — per spec:266-272 a derived write must *prove* every rule, so the rule converts the band obligation into an identical unprovable rule obligation. E1 produces a runtime gate, not a proven property. The *computation* survives (drop the bound, or dead-guard it); the *checked declaration of a known truth* does not. This directly collides with the §0.8 audience commitment ("the domain expert declares what they know to be true"). Prior art makes the removal precise (P5, `research/architecture/compiler/nonlinear-undecidable-proof-escape-mechanisms-2026-07-12.md`): SPARK, Frama-C, Dafny, F*, and Liquid Haskell **all** keep a Tier-2 "supply a decidable checked fact" path (lemmas/ghost procedures/refinement reflection) and a Tier-3 trust escape for exactly this case. **No surveyed system combines solver-free + compilation-gating + no-escape** — Precept's pure posture is unprecedented at precisely this conjunction, and Tier 3 is independently foreclosed by Precept's own soundness principle (spec:221).
2. **Conditional residue — temporal writes (open hole, no probe closed it):** `set Expiry = now() + Term` into a temporal band is forceable via self-guard **only if `now()` is operation-snapshot-stable**. The spec gives only the signature `() → instant` (spec:1599) — no snapshot semantics anywhere (adversary-verified; re-verified here). If `now()` re-samples per call, the guard and the write read different instants and the whole temporal family becomes genuinely unforceable. **Owner-gated spec obligation the posture depends on.** Corpus has zero bounded temporal targets today, so exposure is prospective.
3. Orthogonal but compounding: Σ-aggregation is unwritable by a *separate deliberate exclusion* (`collection-types.md:860`), not by this posture — but it manufactures the mirror-accumulator members of Q1's exception class.

**(c) The decidability ceiling.** Can the engine ever be "made comprehensive enough"? **No — two hard walls, and no engineering closes them:** (i) nonlinear *integer* entailment is undecidable for any system, SMT included (Matiyasevich 1970 / Hilbert's 10th); (ii) nonlinear *real* entailment is decidable (Tarski 1951) but only via CAD-class machinery (Collins 1975) whose witnesses are exactly what spec:225 excludes on principle. **But comprehensiveness is the wrong metric.** Because the language is loop-free, pure, finite, and single-file (spec:158-174), every safety fact a derived write needs is expressible *in the language* as a guard or rule, and congruence-discharge of stated facts is decidable and maximally legible. The system is **closed under forcing** — undecidability caps what the engine can *discover*, never what the author can *state* — **with exactly one hole**: the derived-value invariant, where "stating it" regenerates the same obligation (the circularity above). That hole is the entire genuine limit surface, and it is principle-chosen (spec:225), not mathematically compelled for the reals.

---

## Relating Q1 and Q2 — the strongest case, and the general pattern

**Does a case exist that is BOTH forces-worse AND unwritable? Yes — one, and Q1's exception class and Q2's removed capability are the *same object* viewed from two sides.** The effective-rate exemplar (and its class): as an authoring outcome it is forces-worse (delete a truth, or ship a dead refusal with a fictional message and a duplicated formula); as a capability it is the unwritable checked invariant. Precision matters for the owner's weighing: the *computation* is never lost — what is lost is the pairing ⟨this true bound, compiler-checked⟩. That is strictly narrower than "legitimate computation is unwritable," and strictly worse than "annotation is forced."

**Is "the forced remediation is exactly what restores provability" the general pattern? Yes — by construction, wherever the obligation terminates.** The obligation mechanism is *defined* as naming what would make the operation provably safe (spec:266-272), so supplying it restores provability definitionally. Every reachable-band case terminates in a real domain fact. The exception class is *exactly* the set where no annotation terminates — the termination predicate is the single boundary answering both questions:

| | Obligation terminates in a domain fact | Obligation never terminates |
|---|---|---|
| **Q1** | forces-better (all probes, no surviving counterexample) | forces-worse (dead guard / deleted truth) |
| **Q2** | forceable — annotation, capability retained | unwritable-as-checked invariant |

Residual honest costs on the forceable side, not capability but real: wrong-reason obligations (source-directed message when the fix is elsewhere — diagnostic quality), fragment-boundary brittleness (an ordinary edit flips compile→reject), guard-expression hostility to the §0.8 reader in worst compositions, and the shipped-engine-vs-ceiling gap (until congruence discharge, disjunctive intervals, etc. actually ship, authors hit exception-class-*shaped* friction on cases that are in principle clean — a roadmap exposure, not posture evidence).

---

## Owner-gated items (nothing here is ratified)

1. **Ceiling interpretation of spec:225** — opaque *witnesses* vs decision *procedures*: does a certificate-producing procedure (Fourier–Motzkin; polynomial division; convex-combination recognition) count as legible? Generous reading shrinks Q1's exception class to the non-stateable-correlation core and may dissolve the effective-rate exemplar.
2. **`now()` snapshot stability** — an unstated spec semantics (spec:1599 has signature only) that P4's forceability theorem silently depends on for the whole temporal family. Recommend pinning as a spec obligation if the posture is adopted.
3. **Philosophy tension, quote-and-recommend:** proof philosophy #2 — *"The language reports what is definitively broken, not what might be broken… Flagging only proven violations makes it a trusted guide"* (spec:221-229 region). Pure prove-or-reject in this cell rejects *unproven* violations. That is already accepted posture for proof-carrying obligations (tri-state, "unresolved → supply constraints"), and it behaves identically well wherever supplying constraints is possible — but in the non-terminating class there is *no constraint to supply*, which is the exact "nag" situation the philosophy warns about. If the posture is adopted pure, the spec should either document the dead-guard idiom as the sanctioned answer for this class or the owner should address the class boundary. Philosophy edits require explicit owner approval; none is proposed here.
4. **The Tier-2 question prior art poses:** every comparator keeps a lemma-style discharge path for nonlinear invariants; Precept's `rule` already *is* that slot for the linear fragment (spec:256), and extending its discharge to verbatim nonlinear premises is classified merely-unimplemented (P5). Whether to omit it anyway is the real decision Q2 surfaces. (The runtime-governed opt-in construct remains off the table per the brief and is not proposed.)
5. Row-complement narrowing in/out of ceiling (~10 corpus sites' friction rides on it).
6. Incidental: two latent sample defects found during the hunt, worth filing (`samples/saas-customer-onboarding.precept:127-128` double-increment read; `samples/saas-license-management.precept:155` peak overwritten unconditionally).

---

## Bottom lines

**Q1:** Not universal — **exception class**, but narrow, precisely bounded, and adversarially confirmed. For every case where the declared band is *reachable*, pure reject forced a strictly better precept across corpus, finance, science/health, and ops/pricing — several "surely-safe" intuitions (amortization balances, depreciation) turned out to hide real domain behavior the naive precept omitted, and the three strongest constructed counterexamples were broken on examination. The surviving exception is the **unreachable-but-unprovable** class: always-true invariants on derived values whose proof needs ceiling-excluded algebra and where no annotation terminates the obligation (constants-only algebraic identities; sum-mirror invariants; non-stateable nonlinear correlations). **The predicate you'd decide on: band reachability under true operand ranges, plus obligation termination.** The class's size is in your hands twice over — it shrinks under a generous legible-transform ceiling, and its exemplars flip to forces-better whenever their constants become data.

**Q2:** No legitimate *computation* is removed — the loop-free/pure/finite design (spec:158-174) makes the system closed under forcing, and "bound the operands / state the guard" is exactly what restores provability, by construction. One *capability* is removed: **declaring-and-having-checked a true nonlinear invariant on a derived value** — the one place where stating the fact regenerates the same obligation. The ceiling is real and permanent (integer nonlinearity undecidable for anyone; real nonlinearity decidable only by principle-excluded machinery), but it caps discovery, not authorship, except at that single hole. Prior art's verdict is exact: every surveyed verifier keeps either a lemma path or a trust escape for this hole; solver-free + gating + no-escape is a conjunction no shipped system has attempted.

**The joined decision:** the strongest case against pure reject exists, is singular, and is precisely characterized — the checked-true-invariant hole, simultaneously Q1's forces-worse and Q2's unwritable. Everything else the posture touches is either genuinely better precepts or annotation-with-capability-retained. The decision is therefore not "is there a counterexample" (there is) but whether that one hole — narrow, ceiling-conditional, data-flip-prone, but real and unprecedented-to-omit — is an acceptable price for the pure posture, or whether the class boundary (not the posture) deserves a designed answer.