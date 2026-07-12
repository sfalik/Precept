---
title: "Prove-or-Reject at the genuinely-undecidable cell — Q1 (forces a better precept?) & Q2 (expressiveness / proof-engine limits), spec-first"
status: Draft — 2026-07-12 (independent analysis; spec treated as a DRAFT to rewrite, not authority; not owner-ratified)
author: Independent analysis (Fable), orchestrated by Claude
owner: Shane
frame: WHAT PRECEPT SHOULD BE. Philosophy is the anchor (minus the thesis's on-the-table tweaks); the current spec is a draft input to be challenged; LEGIBILITY (not "no solver") is the real bound on proof power; proof engine reasoned at its design target. End-state.
questions:
  - "Q1: does pure prove-or-reject (Error, no escape hatch) always force a BETTER precept?"
  - "Q2: does it limit expressiveness / hit FUNDAMENTAL proof-engine limits (vs spec decisions that should change)?"
provenance: prove-or-reject-analysis-supporting-2026-07-12/ (6 probes + adversary incl. spec-anchoring audit)
note: Supersedes the deleted 2026-07-12 spec-anchored run. Nothing owner-ratified; philosophy/spec changes are owner-gated recommendations, quoted and unapplied.
revision: "Revised for legibility 2026-07-12 — argument and citations unchanged; coined terms defined/replaced, illustrative samples added; sample dispositions are the proposed design, not current-compiler output."
---

# Prove-or-Reject at the genuinely-undecidable cell: Integrated Decision Analysis

Synthesis of six probes (P1 corpus-taxonomy, P2 finance, P3 science/eng/health, P3b ops/pricing, P4 engine-theory, P5 prior-art) and the adversary pass (P6). All spec-anchoring findings from P6 are folded in and corrected inline. Nothing below is owner-ratified; agent artifacts (Frank's paper, the thesis) are treated as arguments only.

> **Reading note.** This is a legibility revision. The argument, every verdict, and every `path:line` citation are unchanged from the analysis. Coined shorthand has been defined at first use and collected in the glossary below; small `.precept` fragments illustrate the key beats. **Every disposition on a fragment — REJECTED / CLEAN / GOVERNED / UNWRITABLE — is a design label from this analysis, describing what the *proposed* engine would do, NOT current-compiler output.** Where today's shipped compiler behaves differently, a `(today: …)` note says so. Fragments that use syntax the language does not have are marked *illustrative-only*.

## Glossary of terms used in this document

| Term | Plain meaning |
|---|---|
| the genuinely-undecidable cell ("the cell") | The set of derived writes that no proof engine of any power can either prove safe or refute — the residue left after every legible technique is applied. |
| the three-way split (was "the trichotomy") | For a *linear* derivation the engine always lands in exactly one of three outcomes, and knows which: **provably-in-bounds**, **provably-always-breaks**, or **provably-sometimes-breaks-with-a-concrete-counterexample**. |
| weakest precondition (WP) | The exact condition the inputs must satisfy for the result to land in bounds. "Printed verbatim" / "WP-verbatim" = that exact condition shown in the rejection message. |
| certificate (the "certificate criterion") | A legible, replayable proof artifact drawn from a small, fixed, spec-listed vocabulary. Authority to *accept* a proof rests on the certificate, not on any solver's say-so. |
| proof-carrying | Every accepted verdict ships its own independently checkable proof; a non-proof-carrying verdict is a bare "trust me" from a solver. |
| Farkas / Fourier–Motzkin / simplex | Named linear-arithmetic procedures, kept because each yields a *legible step-by-step proof*, not an opaque solver verdict: combine the author's own declared inequalities with positive weights (Farkas), eliminate one variable at a time (Fourier–Motzkin), or pivot between corner solutions (simplex). |
| counterexample witness | A concrete input configuration the engine exhibits to prove a bound *can* be broken (e.g. `Deposits='0.00', Withdrawals='5.00'`). |
| the pullback ladder | Tracing a derived value backward through the assignments that produced it, rung by rung, until it bottoms out at a governable input or `now()`. |
| congruence discharge | Once a fact is made true at every entry point (by a `rule` or a guard), the engine *applies* it directly to satisfy an obligation instead of re-deriving it. |
| corner evaluation / multilinear | A *multilinear* expression is linear in each variable separately (e.g. a product of two independently-bounded quantities). Its extreme value over a box of input ranges always sits at a corner of the box, so checking the finitely many corners is exact. |
| de Bruijn criterion / LCF architecture | Trust flows from a tiny, independent checker that re-verifies each proof step — not from the (possibly huge) machinery that *found* the proof (Milner 1979). |
| verification, not search | Every Precept write is a single fixed value (`set F = E`), so each obligation asks "does this one value fit?" — never the search question "does *some* value exist that fits?" |
| GOVERN | The **rejected** alternative posture: accept the write, *assume* the declared band holds, and check it at runtime — a second guarantee class layered onto the product. |
| flag-and-record | Model an *expectation* as an unbounded field plus a derived boolean (e.g. `IsOutOfSpec <- …`) instead of a hard bound. |
| "forces a better precept" (was FORCES-BETTER) / "forces a fake bound" (was FORCES-WORSE) | The remediation the posture demands *improves* the definition / the remediation would make the author assert a bound that isn't actually true. |
| Source 1 / 2 / 3 (was S1 / S2 / S3) | The only three origins a cell member can have — see §1.2. |
| Exception 1 / 2 (was E1 / E2) | The two members of the exception class — see §2.2. |

---

## 0. Premises: held fixed vs. challenged

**Held fixed — identity-level, with justification:**

| Premise | Why identity-level |
|---|---|
| Legibility of proof — every verdict explainable to the domain expert via a structured, inspectable witness | Follows from the domain-expert author (`docs/philosophy.md:92`) + full inspectability (`docs/philosophy.md:60`). This is the *actual* commitment behind `precept-language-spec.md:225` — not that line's tool ban (challenged, C1) |
| Prevention / structural impossibility (`philosophy.md:43,49`) | The product's category claim |
| One file, no external oracle (`philosophy.md:58`; spec:227-228) | What makes any proof meaningful to the author of that file; permanently bounds one exception class below |
| Determinism (`philosophy.md:22`) | Forces the engine's power and any legibility budget to be spec-pinned semantics, never an implementation dial |
| Honesty about approximation (`philosophy.md:23-25`) | Grounds the "clamp is illegal when it lies" rulings |
| Loop-free / pure / finite execution model (spec:158-174 §0.4) | Held not because the spec says so but because it is the load-bearing *enabler* of legible proof: it makes weakest-precondition (WP) computation exact substitution (Dijkstra 1975 is complete for straight-line code), eliminating the loop-invariant / widening machinery that is the actual source of illegibility in every general verifier |
| Soundness over completeness (spec:221) | A false "safe" destroys the guarantee |

**Challenged — current-spec decisions ruled on in §4:** spec:225 (SMT/Z3 name-ban as the power line); spec:256 (single-pass, depth-bounded relational reasoning; ≥/> vocabulary only); spec:266-272 §0.7 (derived write must re-prove every bound); spec:223 ("proven violations only") **as applied to containment obligations** — P6-F2 correctly showed P2 held this fixed while defending a posture that contradicts it as written; it must be re-decided, and is, in §4.2; `collection-types.md:860` (sum/reduce held back); the absence of a compute-then-check row construct (P6-F1: the load-bearing gap).

---

## 1. The structural results that reorganize the whole question

Three findings, independently reached by multiple probes, that the debate to date (Frank's paper, the thesis) lacked:

**1.1 The three-way-split correction (P4, confirmed P3b).** Give the engine full *linear* reasoning — Fourier–Motzkin / simplex to search, Farkas-combination certificates to prove, concrete-counterexample certificates to refute — and the linear fragment is *decided in both directions*. Linear rational arithmetic is decidable (Farkas 1902; Fourier 1826 / Motzkin 1936); decimals and money embed in the rationals. So for linear derivations, exactly one of three things holds and the engine always knows which: **proven in-band**, **proven always-violating** (a dead row — a branch no input can ever satisfy without breaking the bound), or **proven sometimes-violating, with a concrete counterexample witness**.

The debate's flagship example — `set Balance = Deposits − Withdrawals` into `min 0` (`frank-prove-or-reject-position-2026-07-11.md:22,45-53`; thesis §2.4(ii)) — is *not in the cell at all*. The compiler knows the exact truth and can exhibit `Deposits='0.00', Withdrawals='5.00'`.

```precept
# Illustrative-only shape; mirrors the nonnegative/decimal idiom of
# shopping-cart.precept:49-51 and the "set F = A - B" form at :154.
field Deposits    as decimal default 0.00 nonnegative maxplaces 2
field Withdrawals as decimal default 0.00 nonnegative maxplaces 2
field Balance     as decimal default 0.00 nonnegative maxplaces 2   # bound: Balance >= 0

from Open on Reconcile
    -> set Balance = Deposits - Withdrawals     # design label: REJECTED
```

Under the proposed engine this is REJECTED — not because "I couldn't tell," but because the engine exhibits a valid configuration that breaks the bound: `Deposits='0.00', Withdrawals='5.00'` yields `Balance = -5.00 < 0`. The rejection message *is* that witness. *(today: the shipped engine's depth-bounded relational reasoning does not narrow this cross-field subtraction, so it neither proves the bound nor produces the witness — this is exactly the under-powering §4.5 removes.)* The debate has been fought over an example that dissolves under the right engine, and both position papers' framings ("undecidable-in-general territory"; "over-rejection of the merely-unprovable") dissolve with it for every linear case.

**1.2 The cell's only possible sources (P1).** In a loop-free, total, finite-vocabulary language, "neither provable nor refutable by the right engine" can arise from only three places:

- **Source 1 — genuine contingency:** both outcomes are actually reachable (this is 1.1's *sometimes-violating* bucket). This is *not* an epistemic gap in the engine; it is a real, unhandled domain case.
- **Source 2 — a missing cross-history premise:** a fact true of every reachable state but established only across the entity's *operation history*, not from the current operation alone.
- **Source 3 — knowledge outside the file:** unknowable by *any* engine that honors the one-file commitment.

Source 1 dominates in practice and is, by definition, an unanswered domain question. Source 2 splits into engine-power cases (which exit the cell under §4's upgrades) and language-power cases (the `sum` gap). Source 3 is permanent, but it is bounded by the product's identity, not by the posture.

**1.3 Verification, not search (P6-F5).** Precept writes are deterministic and single-valued (`set F = E`), so every obligation asks *"does this one fixed assignment fit?"* — never the search question *"does some assignment exist?"* The NP-hard arrangement problems of scheduling / packing never arise; checking a fixed value against cumulative capacity is a polynomial sweep with an endpoint witness. This structurally explains why the cell avoids complexity-theoretic blow-up, and it should be stated in the spec's proof-philosophy section.

---

## 2. Q1 — Does pure prove-or-reject always force a better precept?

**Verdict: EXCEPTION CLASS, not universal — but the class is narrow, precisely characterizable, and every member either (a) dissolves under a spec change this analysis recommends, (b) reduces to one ergonomic cost conditional on one new construct, or (c) is bounded by the one-file identity commitment rather than by the posture.**

### 2.1 What is NOT in the cell (the corpus-dominant population)

Enumerated against all 267 derived writes in the 77-file corpus (P1, method: `grep -n "set [A-Za-z]* = .*[-+*/]" samples/*.precept` cross-referenced against bound declarations), plus constructed fragments across finance (P2), science/eng/health (P3), and six ops/pricing domains (P3b):

- **Provable by the right engine, mis-filed as cell members only because the current spec under-powers it**: row-order complement narrowing — using the guards of the rows *above* to narrow what remains (`saas-trial-to-paid.precept:125-136` vs `max 3` at `:30`); weakest-precondition substitution through set-chains (`non-profit-membership-renewal.precept:70,180-186`); three-variable linear rules via Farkas witnesses (`production-order-tracking.precept:66,234`); event-ensure premises flowing into derived writes (`insurance-claim.precept:86,47,127`); min/max/clamp lemmas; weighted-average / mediant certificates (P3b §2a); tax-bracket branch splits (P2 §3.2); the annuity payment formula via named monotonicity lemmas (P2 §3.1); bounded aggregates once `sum` ships.

  A representative case — a product of two independently-bounded operands, proven by corner evaluation (the extreme value of a product over a box of ranges sits at a corner):

  ```precept
  # Real excerpt — inventory-item.precept
  field QuantityOnHand    as quantity ... nonnegative   # :49  → >= 0
  field AverageCost       as price ... nonnegative       # :60  → >= 0
  field TotalInventoryCost as money ... nonnegative      # :57  → bound: >= 0

  -> set TotalInventoryCost = QuantityOnHand * AverageCost   # :159 — design label: CLEAN
  ```

  Both operands are proven `>= 0`, so the product's minimum over the box of their ranges occurs at the `(0, 0)` corner and is `>= 0` — the bound holds, with a certificate that names the two declared `nonnegative` facts. **The corpus authors already write, voluntarily and idiomatically, exactly the structures a legible engine needs** — the guard / reject-row idiom is the dominant pattern, and the current spec commits to consuming almost none of it.

- **Proved-dangerous with a witness (Source 1)** — the heartland where the posture *forces a better precept*: unguarded shrinkage (`inventory-item.precept:182` vs `:49`), the WAC-drift return (`inventory-item.precept:167`), and the discount-stacking bug (`shopping-cart.precept:54,204-206`) **live in the shipped corpus today**:

  ```precept
  # Real excerpt — shopping-cart.precept
  rule DiscountPercent <= 100 because "Combined promotion discount cannot exceed 100 percent"   # :54

  from Cart on ApplyPromotion
      -> put CartPromotions ApplyPromotion.PromotionCode = ApplyPromotion.DiscountPercent
      -> set DiscountPercent = DiscountPercent + ApplyPromotion.DiscountPercent   # :204-206 — design label: REJECTED
      -> set DiscountApplied = TotalValue * DiscountPercent / 100.0
      -> no transition
  ```

  Two separately-governed promotions, each `≤ 100` (the event arg is `max 100`), reach `110` — indeed up to `200` — against the very rule at `:54` whose own `because` states the invariant. The engine exhibits the witness: two `ApplyPromotion` events with `DiscountPercent = 60` each. Design label: **REJECTED** (the rule is a real invariant; the accumulation has no cap). *(today: the shipped sample compiles CLEAN — the bug is latent, because the current engine does not narrow the running accumulation against the rule.)* The forced remediation is a real domain answer: a guard+authored-reject, or `clamp` — already in the language (`Functions.cs:91`) and already used for the symmetric floor case at `shopping-cart.precept:218` (`max(0.0, DiscountPercent - …)`). The same shape recovers the Elo ceiling, token-bucket depletion, and margin-sufficiency cases. In every one, the forced remediation — guard+authored-reject, `clamp`, a declared relational rule, or deleting a false bound — is a real domain answer to a real unanswered question. **Three latent bugs in polished samples were found by *applying* the posture analytically.**

- **Mis-modeled invariants that rejection correctly catches**: a `max` on a *free measurement* (Reynolds number, sample std-dev) is a category error; the corpus's own SPC sample already models thresholds as fields routed by guards (`statistical-process-control.precept:30-34,122-194`). Prove-or-reject *teaches* the band-vs-classification and band-vs-lifecycle distinctions (the hedge-effectiveness case, P2 §3.6c — GOVERN would have let the mis-modeled band ship).

- **Bound-invention does not materialize**: the "214 unbounded money fields" figure (`research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md:120-133`) measures a *different* posture's cascade (reject-unbounded-*source*). In this cell, obligations attach only where the *target* carries a declared band; the 214 unbounded targets generate zero obligations, and where corpus authors *do* declare bounds they mean them and pair them with cap-handling rows. In finance specifically, forced input bounds are almost always unstated real institutional controls (per-trade limits, rate collars, factor ranges ∈ [0,1]) — this *forces a better precept*, not fiction.

### 2.2 The exception class — exact predicate

A derived write `set F = E` into a declared bound is a genuine exception (a case where the posture does *not* force a better precept) if and only if **all four** hold:

1. **Truthful storage required** — the value is the reported quantity; `min` / `clamp` would report a false value and hide an anomaly (honesty, `philosophy.md:23`, blocks the cap escape).
2. **Genuine invariant** — the bound is true of every reachable state, not a mis-modeled band on a free measurement (else rejection correctly catches a modeling error).
3. **No in-file, ingress-checkable premise** — the discriminator P6-F3 extracted from the P1-Class-4 / P3-Case-5 divergence: if the safety fact can be stated as a `rule` or guard checked against *anything in the file at any entry point*, then the pullback ladder (P2 §2: every derivation bottoms out at a governable input or `now()`) discharges it, and the forced premise is a documented domain control (double-entry, saturation, correlation-consistency) — this *forces a better precept*. The exception requires the fact to be **file-inexpressible**: an empirical / design-level correlation (a fitted calibration curve, qualified hardware, an assay-validated range — P3 Case 5) or an aggregate-linking invariant the language cannot state.
4. **Enforcement, not observation, is wanted** — else the flag-and-record resolution (an unbounded field plus a derived boolean `IsOutOfSpec <- …`) is available and is a *better* model (P6-F5: prove-or-reject correctly forces the invariant-vs-expectation distinction, including for stateless precepts).

**Exception 1 — aggregate-linking invariants (Source 2, language-power): a *conditional* member.** `ItemCount == sum(ItemQuantities)` is true, is the only fact that proves the decrement, and is inexpressible because `collection-types.md:860` holds back `sum` (`shopping-cart.precept:48,125,154`; `bill-of-materials-management.precept:35,114`):

```precept
# Illustrative-only — sum() is held back today per collection-types.md:860.
field ItemQuantities as lookup of string to integer   # real — shopping-cart.precept:31
field ItemCount      as integer default 0 nonnegative # real — shopping-cart.precept:48

rule ItemCount == sum(ItemQuantities)                 # ILLUSTRATIVE-ONLY: cannot be written today
# better still, delete the denormalized counter entirely:
field ItemCount as integer <- sum(ItemQuantities)     # ILLUSTRATIVE-ONLY computed-field form
```

Under today's language, pure reject forces one of two bad options: a **dead-guard fiction** (a reject row asserting a scenario no input can trigger — anti-legible, and it corrupts the definition-as-documentation), or dropping a true bound. Roughly 5 write-sites across 2 corpus files. Disposition today: **UNWRITABLE**. **It dissolves entirely under §4.4** (admit `sum`; prove it by inductive delta preservation — the same per-mutation-delta machinery `spec:258` already ships for `count`; and the computed-field form above deletes the denormalized counter outright). If §4.4 is refused, this is the posture's one real standing cost — a case where reject *forces a fake bound or a fiction* — and it should be named as such.

**Exception 2 — empirically / design-guaranteed hard invariants (P3 Case 5): a *permanent* member, bounded by identity.** Consider a corrected instrument reading whose non-negativity holds only because the calibration curve is *designed* anti-correlated:

```precept
# Illustrative-only shape; grounded in the flag-and-record idiom of
# calibration-management.precept (MeasuredValue :28, OutOfTolerance :29).
field RawReading     as decimal maxplaces 6
field CalFactor      as decimal maxplaces 6
field CorrectedValue as decimal nonnegative maxplaces 6   # bound holds ONLY by design of the curve

-> set CorrectedValue = RawReading * CalFactor            # design label: REJECTED
```

No algebraic entailment from declared facts exists, so **no engine of any power that honors one-file can prove it**. Its membership is charged to the one-file identity commitment, not to prove-or-reject. The *posture-specific* increment over GOVERN reduces (P6-F3) to a guard+reject with a runtime boolean — which needs no proof and discharges the write by congruence:

```precept
# The flag-and-record resolution — an EXPECTATION, not an invariant.
field CorrectedValue as decimal maxplaces 6              # unbounded — truthful storage
field OutOfSpec      as boolean <- CorrectedValue < 0    # derived observation; mirrors OutOfTolerance :29
```

That leaves only (i) the formula-duplication cost (§2.3) and (ii) a mild fiction when the author *believes* the guard unreachable. GOVERN's per-instance runtime check and reject's guard row do the same runtime work; the difference is *who authored the refusal message* — the reject-row version carries the author's `because`, where GOVERN's carries a generic fault.

**Failed break-attempts, for the record:** three-asset portfolio variance under correlation-consistency (P2 §4.1 — the one case at the true legibility ceiling; every off-ramp is domain-honest or standard quant practice); the many-variable actuarial reserve identity (P3b §3 — dissolves under the corpus's own named-intermediate-field idiom before it ever needs an opaque solver); external-delta projections (P1 Class 4 — the corpus itself names arg-trusting as the anti-pattern, `event-venue-booking.precept:208-210`); scheduling / capacity (P6-F5 — verification, not search); derived temporal and string cases (corpus-empty, and analytically neutral given one-timestamp-per-operation, §4.7).

### 2.3 The one universal cost (P6-F1, promoted to headline status)

Every contingent case's honest remediation *duplicates the derived formula into the guard*, because nothing lets a row name a computed value and route on it. This cost is (a) universal across every contingent write in every domain, (b) a real drift hazard for exactly the non-developer audience, and (c) **load-bearing for the Q1 verdict**: the probes' "forces a better precept" conclusions silently assume it away. The verdict is therefore **conditional**: pure prove-or-reject forces a better precept *given* a compute-then-check row construct (bind an intermediate value once; route on it or attach an authored refusal). Without it, the verdict degrades to "forces better, with a duplicated 100-character formula you must keep in sync" — still defensible, but materially worse, and the owner should decide the posture and the construct together. (Construct shape is Tier-2 owner-gated `/design` work; this analysis establishes the *need* only.)

---

## 3. Q2 — Expressiveness and proof-engine limits (standalone)

### 3.1 What the engine SHOULD prove, derived from legibility

The right line is the **certificate criterion** (the de Bruijn criterion / LCF architecture, Milner 1979; proof-carrying code, Necula & Lee 1997), independently converged on by P4 and P5's prior-art survey: *search is unconstrained; authority rests only in a certificate drawn from a small, closed, spec-enumerated vocabulary, where each certificate (a) replays in an independent tiny checker and (b) renders as a bounded-size, domain-vocabulary explanation naming the author's own declarations.* P5's cross-system survey (SPARK, Frama-C, Dafny, F\*, Liquid Haskell) shows the field's working distinction is **certificate vs. trace** (a checkable proof artifact vs. an opaque solver run), never "solver vs. no solver" — and Dafny's `calc` / Liquid Haskell's `?`-chains show the most legible artifacts in the field are exactly the "show your work" shape Precept's `because`-culture already speaks.

Admissible certificate families:

- interval derivations (the current baseline);
- **Farkas / Fourier–Motzkin linear combinations** — strictly stronger than intervals: they see cross-field correlations intervals structurally cannot, and each step reads as "combine these two named rules with these positive weights";
- finite case-splits on named conditions (branches, `min`/`max`/`abs`, calendar bounds, small residue classes);
- monotone endpoint mapping (`sqrt`, constant-exponent `pow`, rounding brackets, place-value grid facts);
- **corner evaluation for multilinear expressions over boxes** — exact, because such an expression's extreme value over a box of ranges is always at a corner (the `QuantityOnHand * AverageCost` case in §2.1);
- **congruence discharge** of author-stated facts — including nonlinear rules: the engine *applies* what governance makes true at ingress, it never *derives* it;
- equality substitution;
- **aggregate bound-transfer** (`sum ∈ [count·min, count·max]`) — the most domain-natural certificate in the whole engine;
- a curated named-lemma catalog for the small nonlinear surface the language actually exposes;
- **bounded** complete-the-square / small-Cholesky witnesses.

Where the probes disagreed on sum-of-squares (SOS) legibility (P5 pro, P3/P4 contra): the reconciliation is the legibility *budget*. A low-degree decomposition renderable as "this equals a square plus nonnegative terms" is admissible; an open-ended, searched SOS with non-obvious coefficients is not. The budget is spec-pinned semantics (determinism), never an implementation dial.

Excluded on the criterion's own terms: CAD cell decompositions (Collins 1975 — sound, complete, and doubly-exponentially illegible: **the proof that power and legibility are independent axes**); raw SMT resolution / CDCL(T) traces even when machine-checkable (machine-checkability ≠ the human-legibility bar spec:225's rationale actually states); heuristic nonlinear-real-arithmetic instantiation; high-degree Positivstellensatz.

### 3.2 What is GENUINELY unwritable / unprovable (FUNDAMENTAL)

1. **Nonlinear terms interacting with relational premises over exact arithmetic**, with no box structure and no matching authored fact (P4 row 11): undecidable for any engine over integers / exact decimals (Matiyasevich 1970); Tarski-decidable over reals but only via illegible certificates. This is a wall for *every compiler anyone can build*, not a Precept limitation — and the WP-printed-verbatim diagnostic (§4.3) plus congruence discharge shrink its practical residue to near zero.

2. **Iterative / fixpoint computation** (P6-F4): yield-to-maturity / IRR, Newton root-finding, iterative reserving have no closed form and are genuinely unwritable — charged to the loop-free execution model (a held-fixed identity), not to the posture:

   ```precept
   # Illustrative-only — NOT writable in Precept today, and not under the proposal.
   field Principal as decimal ...
   field Rate      as decimal ...
   field Periods   as integer ...
   field FutureValue as decimal <- Principal * pow(1 + Rate, Periods)   # variable exponent
   # IRR/yield-to-maturity is worse still: no closed form at all — it is a root-find (a loop).
   ```

   Disposition: **UNWRITABLE**. Constant-exponent `pow` is admissible (§3.1, monotone endpoint mapping); a *variable* exponent, or a root-finding loop, is not. The spec should name this carve-out honestly rather than let "expressively closed" imply otherwise.

3. **User-defined folds** (`reduce` / `map` / `filter` with arbitrary combining logic): a fold is a loop in disguise; its WP needs an inductive invariant — precisely the reasoning §0.4 excludes, and no legible vocabulary covers it. Correctly excluded, and this is a *stronger* justification than `collection-types.md:860`'s current "predicate not computation" line.

4. **Facts outside the file (Source 3)** — charged to one-file, permanent, and correct.

### 3.3 The decidability-ceiling answer

For **silent, unaided proof**: yes, there is a wall (3.2.1), and it is universal. For **the author's ability to state every safety fact as something checked**: **no wall exists.** Because WP is exact substitution in a loop-free pure language, every containment obligation reduces to a quantifier-free predicate *of Precept's own expression language* — which the author can always declare, as a `rule` (made structurally true by governance at every ingress, consumed by congruence) or a row `when` guard (discharged by exact match, forcing the complement to an authored disposition). The engine can even *print* that predicate. Pure prove-or-reject is therefore **expressively closed** modulo 3.2.2's iterative carve-out: the residual cost in the fundamental cell is never "invent a fact you cannot state to appease the prover" — it is "answer the domain question your definition left open, handed back to you verbatim." The one author genuinely refused is the one who wants "compute it and let it land where it lands, no stated disposition for out-of-band" — and refusing *that* a spelling is the prevention identity doing exactly what it claims.

---

## 4. WHAT THE SPEC SHOULD SAY

Each ruling separates (a) identity premises held fixed from (b) current-spec text to rewrite.

**4.1 The legibility line — replace spec:225's second half.** *Held fixed:* legibility itself (§0 table). *Rewrite:* delete "this is why SMT/Z3 solvers are excluded even when they could prove more." Replace with: *"Proof verdicts are certificate-defined. A decision procedure is admissible iff every verdict ships with a certificate from the spec-enumerated vocabulary [§3.1's list], independently replayable and renderable in domain vocabulary at bounded, spec-fixed size. Search may use any procedure, including off-the-shelf solvers internally; authority rests only in the certificate. Certificates exceeding the legibility budget are treated as unproven."* Precedent: de Bruijn / LCF; Farkas 1902 / FM 1826; Parrilo 2000 / Handelman 1988 as the certificate-not-search pattern; CAD as the explicit negative case. Tradeoff accepted: a certificate-family boundary is harder to state and implement than a tool ban, and the per-family scoping is real engineering work.

**4.2 Re-decide spec:223 explicitly (the P6-F2 correction).** As written, "proven violations only — not what might be broken" contradicts rejecting an *unresolved* containment (spec:229's own third bucket). Two probes leaned on spec:223 as identity while defending the posture — a genuine spec-anchoring failure, corrected here by ruling rather than inheriting: *the spec should scope "proven violations only" to violation **reports** (dead rows, contradictions, unreachable states) and separately classify definition-derived writes into declared bounds as **containment obligations** — prove-or-reject, where "unproven" blocks not because the code might be broken but because the definition left a reachable case without an authored disposition.* The diagnostic surface must preserve the three-way split even though both outcomes block: *proven-violating (with a concrete witness configuration)* vs *unresolved (with the computed weakest precondition printed verbatim)*. Under §3.1's engine the unresolved bucket is nearly empty for linear derivations, which is what makes this reconciliation honest rather than definitional sleight-of-hand. This also explicitly re-decides §0.7's classification of policy-band containment alongside fault-prevention — the same obligation posture, consciously chosen, not inherited from a heading.

**4.3 spec:266-272 — the derived-value invariant stays a re-proved obligation, NOT a checkable premise; three amendments.** *Ruling:* keep it re-proved. Under the right engine its price collapses (the linear fragment is fully decided; the nonlinear residue gets the verbatim WP). The GOVERN alternative (assume band, check at runtime) is off the table per the posture, and this analysis found no exception class that justifies reopening it — Exception 1 dissolves via §4.4, Exception 2 reduces to §4.6's construct. Amendments: (i) strengthen "names what would make the operation provably safe" from aspiration to defined artifact — the rejection **must** include the computed WP and the three-way-split verdict; a "cannot prove" without the WP is an engine defect; (ii) **assume-after-establish**: a write proven (or entering) in-band establishes the band as a premise for downstream obligations in the same plan (sequential proof flow, spec item 7, extended to bands); (iii) Frank's own conceded sharpening — "against every *decidable* declared constraint" (`frank-prove-or-reject-position:141`). **Conditionality clause:** the spec section adopting prove-or-reject must state it is conditioned on the engine contract of §4.1 / §4.5 — ratifying the posture over today's interval-plus-depth-1 engine would reject the corpus's best idioms and would deserve the thesis's over-rejection critique.

**4.4 collection-types.md:860 — split the hold-back.** Admit the named algebraic aggregates (`sum`, `min`, `max`, `average`) over numeric-element collections: bound-transfer certificates (`count`-interval × element-band — the machinery spec:258 already ships), inductive delta preservation for declared aggregate-equality rules / computed fields (Event-B invariant-preservation precedent, Abrial 2010), a `count > 0` obligation for `average`. Keep `reduce` / `map` / `filter` excluded, on the stronger ground (§3.2.3: folds need inductive invariants — the one thing no legible vocabulary covers), and rewrite the exclusion sentence to say so. Rationale: the corpus is already computing aggregates by hand, less safely (Exception 1); withholding `sum` from a language whose author governs invoices, claims, and ledgers is a severe gap whose proof story is the *easy* part. Language-surface change → owner gate + `/design`.

**4.5 spec:256 — replace the depth bound with full linear entailment.** Delete "single-pass and depth-bounded (no transitive chasing of a third field)" as spec text (it is implementation staging; ~44ms full-corpus compiles leave enormous headroom). Replace with: full linear entailment over the declared premise set (rules including **equalities**, event ensures, row-complement narrowings from rows above, state-flow facts, guard congruence), Fourier–Motzkin / simplex search, Farkas certificates, counterexample witnesses. Plus the discharge-direction symmetry P3 caught: a relational rule discharges an assignment-range bound exactly as it discharges divisor safety.

**4.6 Commit to the compute-then-check row construct as the posture's named dependency.** A row form that binds an intermediate value once and routes on it / attaches an authored refusal. This is the single highest-leverage ergonomic fix for the posture (§2.3) and must be decided *with* the posture, not after it. Shape via `/design`, Tier-2 gated.

**4.7 Minor:** spec one evaluation timestamp per operation (a determinism corollary; makes same-operation `now()` relations provable). State the verification-not-search property (§1.3) in the proof philosophy. Add the flag-and-record idiom (unbounded field + derived boolean) to guidance as the canonical model for *expectations* vs *invariants*, including stateless precepts.

---

## 5. Q1 ↔ Q2: the relationship

**The strongest case against pure reject**, assembled from the adversary's best material rather than either position paper: *"Your posture's survival depends on (i) an engine you haven't specced (§4.1 / §4.5), (ii) a language construct you haven't designed (§4.6), (iii) a `sum` feature you currently refuse (§4.4), and (iv) reclassifying policy-band overflow as an obligation against your own 'proven violations only' principle (§4.2) — four unratified decisions. And even then, Exception 2's calibration-curve author must choose between a reject row she believes unreachable and dropping a true rule."* This is the honest form of the case, and its answer is not rebuttal but **making the four conditions explicit ratification items**. On Exception 2 specifically: the residual cost against GOVERN is a formula duplication (erased by §4.6) plus a mild fiction — while GOVERN's cost is a second guarantee class threaded through the entire product surface for a class this narrow.

**Is "the forced remediation restores provability" the general pattern once the engine is right? Yes — with the mechanism now visible.** It is not a happy accident; it is the pullback ladder plus exact WP. Every obligation is a predicate *of the language itself*, so the remediation (state it as a rule at ingress, or guard on it) is always expressible, and once expressed it discharges the obligation by congruence — remediation and proof are the *same artifact*. The write-ups' recurring empirical observation — that the forced guard / rule / clamp / deleted-bound is a domain answer, not prover ceremony — is the domain-facing shadow of that structural fact. The exceptions are exactly where the ladder breaks: the fact is inexpressible (Exception 1 — fix the language) or lives outside the file (Exception 2 — identity-bounded, cost reduced to ergonomics).

---

## 6. Bottom lines

1. **Q1: EXCEPTION CLASS, not universal — but the class is two members wide, and neither justifies an escape hatch.** Exception 1 (aggregate-linking invariants, ~5 corpus write-sites) dissolves if `sum` ships; Exception 2 (empirically-guaranteed invariants, real in calibration / assay work) is charged to the one-file identity, and its posture-specific cost reduces to formula duplication plus a mild fiction. Everything else — the entire linear world, the corpus's guarded idioms, the debate's flagship overdraft example — is either decided-with-witness or spuriously rejected only by today's under-built engine. Three latent bugs found in shipped samples (`shopping-cart` discount stacking, `inventory-item` shrinkage and WAC-drift) are direct evidence the posture's rejections are domain findings.
2. **Q2: no expressiveness wall attributable to the posture.** The genuine walls are nonlinear × relational entailment (a wall for every compiler ever buildable — Matiyasevich / illegible-Tarski) and iterative computation (charged to loop-free execution — name the IRR carve-out honestly). On *statability*, the posture is expressively closed: every safety fact is authorable in-language, and the engine can print the missing one verbatim.
3. **The verdict is conditional on four spec decisions, and should be ratified as a package**: the certificate criterion replacing spec:225's tool ban; full linear entailment replacing spec:256's depth bound; `sum`-in / `reduce`-out replacing collection-types.md:860; the spec:223 rescope + WP-printed-verbatim diagnostics. Plus one language construct (compute-then-check) as the posture's named ergonomic dependency. Ratifying prove-or-reject *without* the package means fighting the identity battle over an artificially weak engine — which no side of the debate should accept.
4. **The spec-anchoring audit found one real failure (P2's spec:223 inheritance) and one probe-set inconsistency (P1-Class-4 vs P3-Case-5), both corrected here** — the first by ruling on spec:223 explicitly (§4.2), the second by naming the guard-checkability-at-ingress discriminator (§2.2, condition 3). P1's temporal NEUTRAL rested on corpus-absence but holds analytically given §4.7's timestamp rule.
5. **What GOVERN was for, this design does without it**: authored, `because`-carrying, legibly-addressed runtime refusal already exists wherever the domain wants one — the guard+reject row — with zero new guarantee classes and zero bifurcation of what a declared bound means.
