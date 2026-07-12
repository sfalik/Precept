I have the context I need. I read `docs/philosophy.md` in full, `precept-language-spec.md` §0.4/§0.6/§0.7 and the numeric/function/modifier tables, `collection-types.md:860` (sum/reduce hold-back), `business-domain-types.md` (quantity/unit/dimensional reasoning), the identity thesis, and the `calibration-management` and `statistical-process-control` samples for shape. Here is the analysis.

---

# THE CELL under pure prove-or-reject: science / engineering / health

## 0. BLUF

Under the *maximally-legible* engine (not today's), **the cell is very nearly empty for realistic science/engineering/health authoring.** Almost every fragment that looks like FORCES-WORSE resolves into one of three FORCES-BETTER moves — *reformulate-as-cap*, *state-a-real-premise*, or *delete-a-mis-modeled-invariant* — with the residual work being **legible engine power the current spec withholds**, not genuine undecidability. The pressure that motivates a runtime-govern escape in the *money* corpus (unbounded fields with no principled bound) is structurally weaker in these domains, because physical quantities carry principled operating envelopes.

**One narrow FORCES-WORSE class survives** and it is real: a derived value that (a) genuinely satisfies a true invariant, (b) cannot be capped without lying about the value, and (c) whose truth rests on an **empirical or design-level correlation** (a calibration curve, a hardware qualification) that is *not algebraically derivable from any declared bound* and is *not statable as one legible premise*. Pure prove-or-reject forces the author to drop a real check or fabricate operand bounds. This is exactly GOVERN's home — but it is a *narrow* class, not the broad "any unbounded derived write."

---

## 1. What I hold fixed vs. what I challenge

### Held fixed (identity/philosophy — each justified as identity-level)

- **Prevention / structural impossibility.** "Invalid configurations are structurally impossible" (`philosophy.md:43`); the guarantee object is the configuration (`philosophy.md:43`). Identity-level: it *is* the product.
- **Determinism** (`philosophy.md:22`) and **one file** (`philosophy.md:58`; spec `§0.4.7:172`). Identity-level.
- **Domain-expert author** (`philosophy.md:92`; spec `§0.8:280`). Identity-level; it is "the most distinguishing positioning claim" (`spec:282`).
- **Honesty about approximation** (`philosophy.md:23-25`). Identity-level.
- **Legibility of proof.** This is the real bound on engine power. The commitment is that "proof reasoning must be legible… Proof witnesses must be structured data, not opaque solver traces" (`spec:225`). I hold the *legibility* commitment fixed and treat it as identity-level (it follows from the domain-expert author and from full inspectability, `philosophy.md:60`).
- **The §0.4 execution model** (no loops, finite, pure). I hold this — but note the spec's own reasoning (`§0.4.1:160`) that bounded quantifiers "unfold to a finite conjunction," so an aggregate over a *statically-declared finite* collection is **not** iteration. This matters below.

### Challenged (current spec decisions I put on the table, with what the spec SHOULD say)

1. **`spec:225`'s literal "SMT/Z3 excluded even when they could prove more."** The identity commitment is *legibility*, not an allowlist of forbidden tools. A decision procedure that emits a **structured, inspectable certificate** is legible and should be *allowed*: a Fourier–Motzkin elimination tableau (each derived bound = an explicit nonnegative combination of the author's stated rules), a monotonicity witness ("f is increasing on this interval; endpoints map here"), a **Cholesky / complete-the-square** sum-of-squares witness ("this quantity = ‖…‖², hence ≥ 0"), place-value decimal reasoning for `maxplaces`. **Spec should say:** *proof power is bounded by whether the witness is a domain-expert-inspectable certificate, not by which algorithm produced it.* SMT is excluded to the extent its witness is an opaque UNSAT trace — not categorically.

2. **`spec:256` relational reasoning: "single-pass and depth-bounded (no transitive chasing of a third field)."** This under-powers a legible capability. **Spec should say:** multivariable *linear* closure via Fourier–Motzkin is admitted; transitive chasing of stated linear rules is legible (the certificate is the elimination sequence).

3. **The item-2 / item-6 asymmetry.** `spec:256` lets a relational rule discharge *divisor safety* (`Z/(X−Y)` from `rule X>Y`), but item 6 (`spec:208`) — assignment-range containment — is described only in the reject-on-violation direction. The *discharge* direction is not stated. **Spec should say:** a relational rule discharges an assignment-range bound identically (from `rule A>=B`, the write `set N = A−B` into `nonnegative N` proves clean — a one-step legible fact).

4. **No relational-premise-through-multiply/divide.** **Spec should say:** from a stated `rule S <= C*V` with `V positive`, the engine discharges `set R = S/V` into `max C` by multiplying the premise through `V>0` (legible: "multiply both sides of your rule by V").

5. **No monotone-function interval propagation.** `sqrt`/`pow` currently get only *non-negativity* obligations (`spec:206`), not range propagation, and `log`/`exp` don't exist. **Spec should say:** monotone functions propagate intervals with a monotonicity witness.

6. **`collection-types.md:860` holds back `sum`/`reduce`.** Over a *statically-declared finite* collection this is decidable and legible, and by the spec's own quantifier-unfolding argument (`§0.4.1:160`) it is not iteration. **Spec should say:** admit `sum`/`reduce` over declared-finite collections as a legible aggregate; it is needed for conservation invariants.

7. **`spec:266-272` / `§0.6 item 6:208` — "a derived write must prove every rule; assignment-range impossibility is an Error."** This is the crux the whole exercise targets. My finding (below): keep prove-or-reject for **proof-carrying** bounds unconditionally; for **non-proof-carrying policy bands**, prove-or-reject is *defensible and mostly FORCES-BETTER* in these domains **once the engine is powered to (1)–(6)** — but under today's under-powered engine it FORCES-WORSE on cases that are not genuinely undecidable. The correct fix for those is *power up the engine*, not relax to runtime. A genuinely-undecidable/illegible residue remains and is where GOVERN earns its keep.

---

## 2. The legibility ladder (what the right engine decides, and what it cannot)

**Decidable *and* legible (should be PROVEN, in the cell → not in the cell):**
- Interval arithmetic on `+ − × ÷` when operands carry bounds (product/quotient of bounded intervals; the witness is the endpoint arithmetic). Already claimed at `spec:203`.
- `min`/clamp against a constant ceiling: `min(e, k) ≤ k` (monotone, one-line witness).
- Single-variable quadratics: complete-the-square (`x²−x+0.3 = (x−0.5)²+0.05 > 0`).
- Quadratic forms `xᵀMx ≥ 0` for fixed PSD `M`: Cholesky gives an explicit SOS witness `Σ(row·x)²`.
- Multivariable *linear* relations: Fourier–Motzkin (explicit nonnegative-combination certificate).
- Monotone transcendentals (`sqrt`, `log`, `exp`, monotone `pow`) on a bounded domain.
- Dimensional/unit consistency: exact and curated (`business-domain-types.md:180`, PRE0157). Legible.

**Genuinely NOT decidable-with-a-legible-certificate (the true wall):**
- **Non-SOS nonnegative polynomials** (Motzkin-type `x⁴y²+x²y⁴+1−3x²y² ≥ 0`): true, but no legible certificate; CAD/Positivstellensatz witnesses are opaque. *Real, but essentially never authored as a domain invariant.*
- **Empirically-true / design-guaranteed invariants** (a value stays in band because of a calibration curve or qualified hardware, not because of any algebraic relation among declared fields). Not even *algebraically* true from the declared facts — so **no** engine, however powerful, proves it. This is the thesis's "wrong-but-in-band is undecidable for any compile-time system" region (thesis `§2.7(a)`, citing `niche-packet:90-92`).

The exercise's discriminator: **before calling a case merely-unprovable, check the ladder.** If the top rung reaches it, it is *spec-under-powers*, not cell.

---

## 3. Cases

### Case 1 — Energy balance (the overdraft analog restated) → FORCES-BETTER

```
field HeatIn  as quantity in 'J' nonnegative
field HeatOut as quantity in 'J' nonnegative
field NetHeat as quantity in 'J' nonnegative
# ...
-> set NetHeat = HeatIn - HeatOut
```
`HeatIn − HeatOut` has interval `[−∞ᵤ, +∞]` given only per-operand nonnegativity → cannot prove `≥ 0`. **Not in the cell.** The missing fact is a *real domain decision*: is `HeatIn ≥ HeatOut` an invariant (a directional/conservation assumption) or not? **Forced remediation:** state `rule HeatIn >= HeatOut because "..."`. The right engine discharges it (challenge #3: relational rule → `A−B ≥ 0`, one-step). If the author *cannot* assert it, then `NetHeat` genuinely can be negative and `nonnegative` is the wrong model — remove it or make `NetHeat` a signed quantity. Identical structure to `Balance = Deposits − Withdrawals`. **FORCES-BETTER.**

### Case 2 — Weight-based dosing → FORCES-BETTER (a missing clinical ceiling)

```
field WeightKg    as quantity in 'kg' positive max '150 kg'
field DosePerKg   as quantity in 'mg/kg'          # protocol rate
field ComputedDose as quantity in 'mg' max '4000 mg'   # adult ceiling
# ...
-> set ComputedDose = WeightKg * DosePerKg
```
If `DosePerKg` is bounded (a protocol rate, e.g. `max '15 mg/kg'`), interval `×` proves `[0, 2250 mg] ⊆ [_, 4000] ` → **compiles clean**. If it is *unbounded*, the product is unbounded → reject. **Not in the cell** either way. **Forced remediation** in the reject case is the actual clinical rule — the ceiling is a **cap**, not a band: `set ComputedDose = min(WeightKg * DosePerKg, '4000 mg')`, which proves clean (`min ≤ ceiling`) *and* encodes the adult-ceiling protocol. The rejection catches a genuine overdose path for heavy patients (weight × rate exceeding the ceiling). **FORCES-BETTER — patient-safety-positive.** This is the dominant shape: *an output "band" that is physically a cap should be authored as a cap, and then it is trivially provable.*

### Case 3 — Concentration vs. saturation → spec-under-powers, then FORCES-BETTER

```
field Solute        as quantity in 'mol' positive
field Volume        as quantity in 'L'   positive
field Concentration as quantity in 'mol/L' max '5 mol/L'   # saturation limit
# ...
-> set Concentration = Solute / Volume
```
`Volume positive` discharges divisor safety, but `Solute/Volume` is unbounded above → cannot prove `≤ 5`. **You cannot cap it** — `min(Solute/Volume, 5)` would *report a false concentration*, hiding a supersaturation anomaly (a dead-guard fiction; forbidden by honesty, `philosophy.md:23`). So Case 2's cap move is unavailable. **But this is not the cell:** the invariant, when it holds, holds *because* `Solute ≤ Csat·Volume` ("we never dissolve past saturation") — a premise the chemist actually holds. Under the right engine (challenge #4) `rule Solute <= '5 mol/L' * Volume` discharges `Concentration ≤ 5` by multiplying through `Volume > 0`. **Forced remediation:** state that rule → **FORCES-BETTER** (the "never supersaturated" assumption becomes explicit and enforced). If the process *can* supersaturate, then `max '5 mol/L'` is a **mis-modeled invariant** → remove it and *route* on it (Case 4). Either branch is FORCES-BETTER; the only defect is engine power the spec withholds. **Does not survive as FORCES-WORSE.**

### Case 4 — Regime classification → FORCES-BETTER (the biggest catch: a mis-modeled invariant)

```
field Reynolds as number max 2300     # WRONG: Re is a free measurement, not bounded
# ...
-> set Reynolds = (Density * Velocity * Diameter) / Viscosity
```
Reject. **Not in the cell** — this is a *modeling error*. Reynolds number is not bounded by construction; `max 2300` declares an invariant the physics does not provide. **Forced remediation:** delete the bound; model the threshold as data and *route* on it with guards — exactly the pattern the corpus already uses. In `statistical-process-control.precept` the limits are **fields** (`UpperControlLimit`, etc., lines 30–34) and each reading is classified by **guards** (`when RecordSample.Value <= UpperControlLimit …`, lines 122–194), never by putting a `max` on the sample. Pure prove-or-reject here is a *feature*: it refuses to let the author assert a false invariant on a free measurement. **FORCES-BETTER** — and this is the most common realistic trigger, because "derived value with no natural operand bound" is usually *because the value is a measurement/classification*, for which an output band is a category error.

### Case 5 — Calibration-corrected value in spec (the serious FORCES-WORSE attempt) → **SURVIVES, narrowly**

```
field RawReading     as quantity in 'uV' positive max '5000 uV'   # ADC range
field CalFactor      as decimal positive                          # from cal curve
field CorrectedValue as quantity in 'uV'
field UpperSpec      as quantity in 'uV'
rule CorrectedValue <= UpperSpec because "Corrected reading must stay within the instrument's qualified upper spec"
# ...
-> set CorrectedValue = RawReading * CalFactor
```
Try to dissolve it:
- **Cap?** No — `min(RawReading*CalFactor, UpperSpec)` reports a false corrected value and hides an out-of-spec instrument. Case 2's move is illegal (honesty).
- **Interval `×`?** If `CalFactor` is bounded, interval arithmetic gives a range and either proves or rejects with a real fix → *not in the cell*. So the survivor requires the truth to rest on a **correlation** interval arithmetic cannot see: the calibration curve is *designed* so that high raw readings receive lower correction factors (an anti-correlation), keeping `RawReading*CalFactor` in spec across the range. The invariant is **true**, but it is true by *curve design*, not by any product of the operands' independent ranges.
- **Legible premise?** There is none. The guarantee is `∀ operating points on this instrument's cal curve, Raw·Cal(Raw) ≤ UpperSpec` — a property of a *fitted function*, not a linear/monotone relation the author can write as one rule. No rung of the ladder reaches it; **no engine, however legible, proves it**, because it is not algebraically entailed by the declared bounds — it is an *empirical/design* fact.

**This is genuinely in the cell, and pure prove-or-reject FORCES-WORSE:** the author must either **drop the `rule`** (lose a real, enforceable check — a governance-integrity regression) or **fabricate operand bounds** that don't capture the real reason (representability masquerading as policy, anti-honest per `philosophy.md:23`). **Forced remediation under prove-or-reject:** delete the invariant → the very kind of silent unchecked path the product exists to abolish (`philosophy.md:67`).

**What GOVERN delivers here** is the same structural-impossibility guarantee at *per-instance* granularity: every write of `CorrectedValue` is checked against `UpperSpec` on a working copy discarded on failure (`spec:268`), the violating configuration never persists, and the refusal carries the rule's mandatory `because`. The invalid configuration is still structurally impossible — proven "in advance over all inputs" is traded for "refused at the moment a concrete value arrives," which is precisely the thesis's `§2.4(ii)`.

**But note how narrow the survivor is.** It required *all* of: value legitimately stored, capping illegal, and truth resting on an *empirical/design correlation* invisible to any legible certificate. Strip any one condition and it collapses back to Case 2/3/4. In science/eng/health this class is real (calibration curves, hardware-qualification noise floors, assay-validated recovery ranges) but *bounded* — it is not "every unbounded derived write."

### Case 6 — Aggregate statistics into a tolerance → FORCES-BETTER *or* spec-lacks-sum/reduce (does not survive)

```
field Readings   as list of quantity in 'mm' min '9.5 mm' max '10.5 mm'
field SampleStdDev as quantity in 'mm' max '0.05 mm'   # process tolerance
```
First, `sum`/`reduce` is absent (`collection-types.md:860`), so this is not even expressible — a *lacks-construct* finding (challenge #6). Second, even *with* aggregates: per-element bounds give `stddev ≤ (hi−lo) = 1.0 mm`, nowhere near `0.05`. `stddev ≤ 0.05` is **not** entailed by the declared bounds — it is an *empirical tightness* fact about the data. So a `max '0.05 mm'` on a computed sample-std-dev is a **mis-modeled invariant** on a free statistic → **route/flag**, exactly as SPC does with a control limit, not bound. **FORCES-BETTER.** (Where an aggregate *is* bounded by construction — e.g. a weighted mean of bounded inputs — it becomes provable once `sum`/`reduce` ships, so the only defect is the withheld construct.)

### Case 7 (NEUTRAL / abstract residue) — non-SOS polynomial band

A derived field bounded by a genuinely nonnegative polynomial that is *not* a sum of squares (Motzkin-type). Abstractly in the cell (decidable by CAD, but no legible certificate → excluded by legibility). **Realistically absent** — no domain expert authors a Motzkin form as a field invariant. Classification: **NEUTRAL/rare**; if it ever arose, prove-or-reject would FORCES-WORSE for the same reason as Case 5, and GOVERN would be the home.

---

## 4. The surviving FORCES-WORSE, stated precisely

Pure prove-or-reject (no runtime opt-in) FORCES-WORSE **only** on a non-proof-carrying policy band when the derived write satisfies *all* of:

1. the value is legitimately stored (it is the reported quantity), so it **cannot be reformulated as a `min`/clamp cap** without lying (Case 2's escape is blocked);
2. the bound is a **genuine invariant**, not a mis-modeled band on a free measurement (Case 4 is excluded — the author truly guarantees it);
3. its truth is **not entailed by any declared bound via a legible certificate** — it rests on an *empirical* or *design-level* correlation (a fitted calibration curve, qualified hardware, an assay-validated range), so **no** maximally-legible engine can prove it (Case 3/6's premise route is unavailable).

In that class, prove-or-reject forces *dropping a real check* or *fabricating meaningless operand bounds*. GOVERN delivers the identical per-configuration structural-impossibility guarantee at ingress/commit. Everything else that *looks* like this class is either provable-once-the-engine-is-powered-up (Cases 1, 3, 6-bounded, and the discharge-direction asymmetry of item 2 vs item 6) or a mis-modeled invariant that rejection *correctly* catches (Cases 4, 6-free).

---

## 5. What this says the spec should do

- **Power the engine up to the legibility ceiling** (challenges #1–#6). Do this *first*: it converts Cases 1, 3, and bounded-6 from spurious rejections into clean compiles or genuine-domain-decision surfacings, and it is the correct answer to the thesis's "bound-invention / over-rejection" cost — most of that cost in these domains is an artifact of the *under-powered* engine, not of prove-or-reject. Reframe `spec:225` from "no SMT" to "no illegible witness"; state the FM linear closure, the relational-premise-through-mul/div discharge, monotone-function propagation, the item-2/item-6 discharge symmetry, and `sum`/`reduce` over finite collections.

- **Keep prove-or-reject for proof-carrying bounds** unconditionally (fault-safety; `spec:266`). Not in question.

- **On the crux (`§0.6 item 6` / `spec:266-272`) for non-proof-carrying policy bands:** the science/eng/health evidence *weakens* the case for a broad govern-relaxation relative to the money corpus — principled operating envelopes make most derived writes provable, and rejection *correctly* catches the pervasive mis-modeled-invariant error (Case 4, the biggest catch). **But it does not eliminate it.** The Case 5 class (empirically/design-guaranteed, un-cappable, un-premisable derived invariants) is real and authentic to calibration/measurement/assay work, and pure prove-or-reject there forces deleting a genuine check — a governance-integrity regression the product's own identity (`philosophy.md:67`) condemns. The honest spec position is therefore **narrower than either pole:** prove-or-reject everywhere the powered-up engine can reach (which is most of these domains, *including* cases the current spec wrongly rejects), and a governed disposition confined to the *empirically/illegibly-guaranteed derived-invariant* residue — with the disposition rendered legibly at the constraint so the author knows which of the two guarantees a given band carries. That confinement is exactly the thesis's proof-carrying-vs-not line (thesis `§2.4`), and this analysis independently locates its live edge, in these domains, at Case 5's three conditions — not at "any unbounded derived write."