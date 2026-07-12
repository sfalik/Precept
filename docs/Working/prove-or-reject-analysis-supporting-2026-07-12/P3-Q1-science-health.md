I have the grounding I need. Design ceiling confirmed from spec: standard interval arithmetic + bounded relational closure + single-pass validation (`precept-language-spec.md:174`), no-SMT by principle (`:225`), dB/pH log-units foreclosed from normalization (`:1448`), divisor two-tier and relational-subtraction proofs (`:207`, `:256`), and the loop-free/acyclic/finite execution model (`:154–174`) that makes much decidable by legible means. Here is the analysis.

---

# Q1 Deep Probe: prove-or-reject on the merely-unprovable, definition-derived → non-proof-carrying-bound cell (SCIENCE / ENGINEERING / HEALTH)

## The discriminator I found (states the whole result)

Across every constructed case, the verdict turns on **one question, not on the domain**:

> **Is the declared band mathematically *reachable* by the derived write, given the operands' true (not interval-widened) ranges?**

- **Reachable** → rejection forces the author to handle a real out-of-band outcome (apply the ceiling, route to a failure state, tighten a source spec). The band is a real rule and the derivation can really hit it. **FORCES-BETTER.** This is the overdraft exemplar's structure: `Balance = Deposits − Withdrawals` into `nonnegative` — you *can* overdraw, so the reject surfaces a genuine unmade decision.
- **Unreachable but interval-unprovable** → the value is provably in-band *by the mathematics*, the band is never actually violated, and the only compiler-satisfying move is a **provably-dead clamp or a vacuous guard**. That code encodes no domain rule and never executes. **FORCES-WORSE** — "representability masquerading as policy" in its purest form.

The adversarial surprise is a **second filter** that shrinks FORCES-WORSE hard: Precept's loop-free, acyclic, finite execution model (`:154–174`) makes a *lot* of the "unreachable-but-unprovable" cases **merely-unimplemented, not fundamental** — a legible solver-free engine at the design ceiling can bound them (corner enumeration for multilinear-over-box, monotone-interval for same-sign polynomials, part-of-whole ratio rules, convex-combination recognition). Those compile clean at the ceiling; counting them as FORCES-WORSE would be counting current-engine weakness, which the brief forbids. What survives as true FORCES-WORSE is a genuinely narrow residue.

---

## FORCES-BETTER cases (the thesis holds where the band is reachable)

### Case 1 — Weight-based drug dosing with a hard single-dose ceiling (HEALTH). **FORCES-BETTER.**
Constructed fragment (shape per `samples/medical-prior-auth.precept`, `min(...)` per `samples/insurance-claim.precept:127`):
```
field WeightKg     as quantity in 'kg'    positive
field DosePerKg    as quantity in 'mg/kg' positive      # protocol rate, e.g. 15 mg/kg
field ComputedDose as quantity in 'mg' max '1000 mg'    # real rule: max single dose, any patient
on Order -> set ComputedDose = DosePerKg * WeightKg
```
Even with **no natural operand bound** (a patient's weight is just a weight), interval arithmetic of a product of two positive quantities straddles the cap → unresolved → reject. This is exactly the "no natural operand bound" case the brief says to push on. **The forced remediation is real, not an artifact:** the compiler cannot prove `rate × weight ≤ 1000 mg`, and the clinical protocol *literally* reads "15 mg/kg, max 1000 mg/dose." The fix `set ComputedDose = min(DosePerKg * WeightKg, '1000 mg')` encodes the actual ceiling-application rule. Reachability is real (a large patient *does* exceed the cap), so the naive form silently overdoses and the reject surfaces the unencoded clamp. Clean parallel to overdraft.

### Case 2 — Actuator/control saturation (ENGINEERING). **FORCES-BETTER.**
```
field Setpoint   as quantity in 'Cel'
field Measured   as quantity in 'Cel'
field Correction as quantity in 'Cel' max '2 Cel'   # actuator authority: ±2 °C physical limit
on Control -> set Correction = Setpoint - Measured
```
`Measured` is an unbounded observation → `Setpoint − Measured` unbounded → reject. Remediation `set Correction = min('2 Cel', max('-2 Cel', Setpoint - Measured))` is the **saturation model** — a real, universal control-systems rule (actuators physically clip). The band is reachable (error can exceed authority) and the reject forces the author to state what happens at saturation. FORCES-BETTER. (Temperature units are affine and in-scope for normalization per `:1448`, so the unit side proves cleanly.)

### Case 3 — Calibration error routed to a Failed state, *not* a bounded field (MEASUREMENT). **FORCES-BETTER (and a modeling correction).**
A tempting anti-pattern:
```
field MeasuredValue as decimal
field ReferenceValue as decimal
field Error as decimal max 0.5     # "must be within tolerance"
on Measure -> set Error = MeasuredValue - ReferenceValue
```
Reject (unbounded measurement into a capped derived field). The *wrong* fix would be to clamp `Error` — which **falsifies the measurement**. The compiler correctly refuses to let you *declare* that observed reality stays in band. The right remediation is the model `samples/calibration-management.precept` already uses: an `OutOfTolerance` boolean + a `Failed` terminal state, with a guard routing the out-of-band error to failure (`:81` `in Failed ensure OutOfTolerance`). Rejection surfaces that a *measured/derived-from-measured* quantity was mis-modeled as a *controlled* one. FORCES-BETTER: the fixed precept distinguishes "discover the value" from "constrain the value."

### Case 4 — Yield / part-of-whole ratio with a relational rule (CHEMISTRY / PRODUCTION). **FORCES-BETTER on the divisor, knife-edge on the magnitude.**
```
field TheoreticalYield as quantity in 'mol' positive
field ActualYield      as quantity in 'mol' positive
rule ActualYield <= TheoreticalYield because "cannot exceed theoretical yield (conservation of mass)"
field YieldPercent as decimal min 0 max 100
on Record -> set YieldPercent = ActualYield * 100 / TheoreticalYield
```
The **divisor** obligation is discharged cleanly: `TheoreticalYield positive` proves the denominator nonzero (two-tier divisor safety, `:207` item 3) — a real, correct demand (you cannot compute a yield against a zero theoretical). The **magnitude** band (`≤ 100`) needs `A/B ≤ 1` from `A ≤ B ∧ B > 0`. Proving a *part-of-whole ratio* lands in `[0,1]` is a specific, legible, bounded inference — the same class the spec already ships for divisor-subtraction (`:256`). So at the design ceiling this is **merely-unimplemented → compiles clean**, not FORCES-WORSE. Where the engine hasn't built it yet, the forced remediation (a `rule YieldPercent <= 100` hand-restated) is mild friction but not fake, because the relation is real. Verdict: FORCES-BETTER on the guard, NEUTRAL-trending-clean on the magnitude.

---

## NEUTRAL case (design foreclosure that forces a defensible restructure)

### Case 5 — Logarithmic units: pH / dB (CHEMISTRY / ACOUSTICS). **NEUTRAL.**
`:1448` deliberately forecloses compile-time normalization of logarithmic/reference-level units (`dB`, `[pH]`) — "they do not reduce to a stable affine conversion law." This is a *fundamental design foreclosure*, not an implementation gap. But it rarely produces the target cell: a pH or dB value is almost always an **ingress measurement** governed at entry (uncontested), or the arithmetic that would trip the band (combining two sound levels, `10·log₁₀(10^{a/10}+10^{b/10})`; converting `[H+]` to pH) requires `log`/`exp` that aren't core operators. When a `quantity in 'dB'` flows into a `max` band, the engine can still interval-reason on the raw log-domain magnitude (gain in dB adds linearly), so most real dB writes prove. The genuine foreclosure (cross-unit normalization) forces the author to keep the value in one unit or split the field — a defensible restructure, encoding a real "these don't convert" fact. Neither better nor forced-fake. **NEUTRAL.**

---

## The serious FORCES-WORSE attempt — and whether it survived

**Target shape:** a derived value that is *mathematically always in-band* (band unreachable), flowing into a *genuine* domain band, where interval arithmetic over-widens because a variable appears with **cancelling / self-correlated** occurrences — the dependency problem — and every compiler-satisfying remediation is dead code.

### Attempt 5a — Convex blend of two concentrations (CHEMISTRY / mixing).
```
field C1       as decimal min 0 max 100     # concentration % of stream 1
field C2       as decimal min 0 max 100     # concentration % of stream 2
field Fraction as decimal min 0 max 1       # blend fraction of stream 1
field BlendConc as decimal min 0 max 100    # a concentration % — genuinely cannot exceed 100
on Blend -> set BlendConc = Fraction * C1 + (1 - Fraction) * C2
```
`BlendConc` is a convex combination of two values in `[0,100]`, so its **true range is exactly `[0,100]`** — the `max 100` band is a real domain law (a concentration cannot exceed 100 %) and is **never violated**. Interval arithmetic computes `[0,1]·[0,100] + [0,1]·[0,100] = [0,200]`, straddles 100 → **reject**. Refactoring to `C2 + Fraction·(C1 − C2)` doesn't help (`C1 − C2 = [−100,100]`, times `[0,1]` keeps the ± span). The only compiler-satisfying moves:
- `set BlendConc = min(100, Fraction*C1 + (1-Fraction)*C2)` — the `min` selecting 100 is **provably-dead code**; it encodes no real scenario.
- `rule / when` on the expression `≤ 100` — a **vacuous guard** (always true), with a dead-end else.

Neither operand can be tightened further (the bounds are already the domain's). This is textbook "representability masquerading as policy": the author writes a bound-satisfying artifact purely to compensate for the engine.

**Did it survive scrutiny? Partially — it survived as a *demonstration* but is knife-edge on FUNDAMENTAL-vs-MERELY-UNIMPLEMENTED.** Recognizing `f·A + (1−f)·B, f∈[0,1] ⟹ result ∈ [min(A,B), max(A,B)]` is a *specific, legible, solver-free identity* — precisely the kind of bounded special-case the spec already ships (divisor-subtraction, `:256`). A design-ceiling engine plausibly **could** encode convex-combination recognition, in which case *this instance compiles clean* and is merely-unimplemented — **not** FORCES-WORSE. So Attempt 5a, taken alone, does **not** robustly survive: it's more likely a build-TODO than a fundamental wall.

### Attempt 5b — the residue that *does* survive: non-recognizable nonlinear self-correlation.
The convex blend fails to survive *because it's a nameable identity*. The surviving FORCES-WORSE is the family that is **not reducible to any legible bounded method** — which the no-SMT principle (`:225`) genuinely forecloses:
```
# Response-surface / non-ideal mixing model, coded factors X,Y in [-1,1]
field X as decimal min -1 max 1
field Y as decimal min -1 max 1
field Response as decimal min 30 max 65     # real product-spec window
on Predict -> set Response = 50 + 8*X + 6*Y - 3*X*Y - 2*X*X + ... (fitted higher-order terms)
```
For a *multilinear/low-order* form, corners are finite and enumerable — so again a legible engine **could** evaluate the finite corner set (the acyclic/finite model, `:154`, makes this decidable) → merely-unimplemented. The residue that is **fundamentally** foreclosed is where tight bounding needs genuine **nonlinear real optimization over a non-box region** or **transcendental exact-boundary** reasoning (e.g. a rational/irrational calibration function whose true supremum *touches* the band, or ranges induced by a nonlinear inter-field relation rather than a box) — where corner enumeration is unsound and only an SMT-class solver would prove in-band. There the value is mathematically in-band, the engine *cannot* prove it without violating the legibility principle, and the forced remediation is the same dead clamp / vacuous guard.

**This residue survives scrutiny as a genuine FORCES-WORSE** — but it is **narrow and rare** in practice: it requires a derived write that is (a) nonlinear with real cancellation/correlation, (b) *not* reducible to any legible identity, corner enumeration, or monotone-interval argument, (c) mathematically confined to a band it never reaches, and (d) aimed at a band that is a real domain law rather than a reachable safety ceiling. Most real science/engineering/health derivations fail (b)–(c): physical bounds are usually *reachable* (safety ceilings, saturation, conservation limits, proportions that genuinely can exceed 1), which flips them to FORCES-BETTER.

---

## Bottom line for the owner's hypothesis

- **The "rejection forces better precept" claim is robust in these domains — more robust than in the sample corpus** — because physical/clinical bounds are overwhelmingly *reachable* real rules (dosing ceilings, actuator saturation, conservation, tolerance-failure routing). Where the band can really be hit, prove-or-reject surfaces a genuine unmade decision every time (Cases 1–4).
- **A true FORCES-WORSE cell exists but is narrow:** a mathematically-in-band derived value with *non-recognizable nonlinear self-correlation* into a real-but-unreachable band, where the no-SMT foreclosure (`:225`) is the binding constraint and the only fix is a provably-dead clamp / vacuous guard. This is real "representability masquerading as policy."
- **The adversarial finding that matters most:** the boundary between FORCES-WORSE and "just build it" is *not* the domain — it is whether the case is **fundamentally** SMT-hard or **merely-unimplemented**. Precept's loop-free/acyclic/finite model (`:154–174`) pulls a surprising amount of nonlinear-looking work (multilinear corner enumeration, monotone same-sign polynomials, part-of-whole ratios, convex combinations) into the *legible, provable* zone. My best concrete FORCES-WORSE constructions (convex blend, low-order response surface) **did not survive** as fundamental — they are build-TODOs at the design ceiling. Only the irreducible nonlinear/transcendental residue survives.
- **Honest caveat this analysis does not resolve:** the "merely-unimplemented" escape hatch is doing heavy lifting. It means the pure-prove-or-reject posture's practical acceptability depends on the engine actually *shipping* those legible special-cases (convex-combination recognition, corner enumeration, ratio bounds). Until it does, authors in these domains will hit FORCES-WORSE-shaped friction on cases that are *in principle* clean — friction that reads to the author as an engine limit, not a domain decision. That gap between design-ceiling and shipped-engine is the real exposure, and it is a build/roadmap question, not evidence against the posture.

No spec or philosophy edit is implied by this probe; the dB/pH foreclosure (`:1448`), no-SMT principle (`:225`), and interval+bounded-relational ceiling (`:174`) are the load-bearing constraints and I have quoted, not overridden, them.