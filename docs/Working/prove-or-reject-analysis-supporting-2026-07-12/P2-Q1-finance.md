# Q1 Deep Analysis — The Merely-Unprovable Derived Write into a Non-Proof-Carrying Bound, in Finance, Under Pure Prove-or-Reject

## 0. Premises: held fixed vs. challenged

**Held fixed (identity-level, with justification):**

- **Legibility of proof.** Proof witnesses must be structured, inspectable, explainable to the domain-expert author (`docs/philosophy.md:60` inspectability; `docs/language/precept-language-spec.md:225` states the principle — though I challenge its *specific line-drawing* below; spec:292 "No opaque proof"). Identity-level because the author is the domain expert (`docs/philosophy.md:92`, spec:280) and an unexplainable verdict disqualifies the product for that author.
- **One-file knowledge boundary.** "All proof facts derive from the `.precept` definition. No external oracle" (spec:227). Identity-level: it is what makes the artifact the contract (`docs/philosophy.md:58`).
- **Soundness over completeness** (spec:221) and **proven-violations-only** (spec:223). Identity-level: false "safe" claims destroy the prevention guarantee; false alarms destroy trust.
- **Determinism** (`docs/philosophy.md:22`) and **prevention/structural impossibility** (`docs/philosophy.md:49`).

**Challenged (current-spec decisions I treat as open and, where relevant, recommend replacing):**

1. **spec:225's specific exclusion line** ("SMT/Z3 excluded even when they could prove more"). The identity commitment constrains the *witness*, not the *search*. Recommended replacement in §1.
2. **spec:256's engine shape** ("single-pass and depth-bounded, no transitive chasing") — an implementation modesty currently written into the spec as if it were a semantic line. Under-powers the engine relative to what legibility permits.
3. **`collection-types.md:860`** — `sum`/`reduce` held back. Finance reserving and accounting need `sum`; its interval reasoning is perfectly legible.
4. **spec:266-272 / spec:208** (derived write must prove every decidable bound, else Error) — this is the posture under study; my finding below is that it *survives* in finance, but only conditional on the engine and construct changes named, and that conditionality must be stated in the spec.
5. **The absence of a compute-then-check construct** (no way to name an intermediate value inside a row and route on it) — a language-shape gap the posture's honest remediation pattern exposes hard.

---

## 1. The maximally-legible engine — derived, not inherited

The legibility principle admits any decision procedure whose **certificate** is a bounded chain of steps, each of which is (a) a named, teachable lemma, (b) an arithmetic check the author can redo by hand, or (c) a concrete counterexample valuation. Search may be arbitrarily clever; the witness must be readable. This is strictly more powerful than spec:225's line and strictly less than SMT-as-oracle. Concretely, the RIGHT engine has:

- **Complete linear arithmetic over rationals/decimals with Farkas witnesses.** A linear consequence of declared rules is certified by a nonnegative combination of the author's own rules — "multiply the rules you wrote by these positive weights and add; the claim falls out." Fully legible (each premise is a line the author wrote, with its `because`); complete for the linear fragment. (Prior art: Farkas certificates in verification, simplex duals; SPARK/Why3 emit these routinely.) This subsumes and completes spec:256's single-pass relational reasoning.
- **Case-split over `if/then/else` branches and guard paths, with branch-condition narrowing.** Case analysis is the domain expert's native reasoning ("in the case where income is over the bracket floor…"). Each branch gets a linear certificate.
- **Substitution/match discharge of declared premises.** A rule syntactically entailing the obligation (including nonlinear rules) discharges it by instantiation — certificate: "the rule you declared at line N is exactly this condition."
- **A closed, curated catalog of named lemmas** for the nonlinear surface the language actually exposes (`+ − × / pow(·,int) sqrt min max abs clamp round floor ceil`, spec §3.7): sign rules (product/quotient of nonnegatives), monotonicity of `×`/`÷` by a positive, `min(x,C) ≤ C`, `max(x,C) ≥ C`, `clamp` containment, `abs ≥ 0`, a-square-is-nonnegative, powers-of-a-number-below-one-stay-below-one, place-value rounding envelopes (`round(x,2) ∈ [x−0.005, x+0.005]`). Each is one teachable sentence; a certificate is a short chain of citations.
- **Counterexample witnesses for the contingent case.** When containment is sometimes-violated over the governed input space, the engine exhibits a concrete valuation ("with Deposits = 100.00 and Withdrawals = 250.00 — both permitted by your constraints — Balance = −150.00, below its declared minimum"). A concrete counterexample is the most legible artifact that exists, and for the linear fragment finding one is complete (simplex). This upgrades "cannot prove" rejections into "here is the unhandled scenario" rejections — load-bearing for everything below.

**Excluded even from the maximal engine:** cylindrical algebraic decomposition, unstructured sums-of-squares/Positivstellensatz certificates with non-obvious rational coefficients, auto-derived transcendental Taylor bounds. Their certificates are checkable only by machine, not readable by the rule's owner — that is the real line legibility draws.

One structural observation before the cases: **the language's austerity and the legible-proof ceiling are co-designed.** No loops, no transcendentals, integer-only exponents (spec:158-174, §3.7) mean the expressible nonlinear surface is small enough that the named-lemma catalog plus complete linear reasoning covers almost all *true* containments a finance author can write. This is why the cell turns out to be thin.

---

## 2. A structural result that reshapes the question: the pullback ladder

Every derivation chain bottoms out at externally-governed ingress. A derived value is built from fields/args that are either (i) external inputs (governed at ingress, spec:268), (ii) themselves derived (recurse), (iii) literals/defaults (statically known), or (iv) `now()` (the one ungovernable source — see the actuarial case). Therefore, for any containment obligation `formula(inputs) ∈ band`, the author can always **pull the condition back to the boundary**: declare `rule formula(inputs) <in band> because "…"` (or the per-row guard form `when formula(inputs) <in band>` + a `reject` row). The rule is expressible whenever the derivation was (same expression grammar); governance enforces it at every operation on the working copy; the maximally-legible engine discharges the write-site obligation by substitution/match. Note `min 5` ≡ `rule X >= 5` (spec:1138), so this is not a loophole — it is the language's own composition seam (`docs/philosophy.md:55`) applied one level up.

Consequence: **under pure prove-or-reject, no author is ever hard-stuck.** The worst-case remediation is one restatement rule with a mandatory `because` — and the `because` is the crux. If the author can state a *true domain reason* for the pulled-back premise, the file gained a documented, runtime-enforced domain fact whose refusal is addressed to the party who supplied the inputs (dissolving Frank's "recoverable by whom" objection — the refusal lands at ingress, where there IS a supplier). If the author *cannot* state a true reason, they have discovered that the bound itself was unjustified — also a win. The FORCES-WORSE question therefore reduces to: **are there cases where every available move — premise rule, guard+reject, cap via `min`/`clamp`, loosen/delete the bound — is a domain distortion?** That is what the case studies hunt.

---

## 3. Case studies

All fragments follow the shape of `samples/loan-application.precept` (read this session: money-in-'USD' fields, `rule … because`, `from/on` rows). Compiled mentally against the *maximally-legible* engine, per the framing.

### 3.1 Lending/amortization — the annuity payment formula. NOT IN THE CELL (under-powered-engine finding)

```precept
field Principal    as money in 'USD' positive max '2000000.00 USD'
field MonthlyRate  as decimal positive max 0.03        # rate collar — a real underwriting control
field Months       as integer min 12 max 480
field Payment      as money in 'USD' positive

rule Payment >= Principal * MonthlyRate because "Each payment must at least cover one month's interest — otherwise the loan negatively amortizes"

on Reprice -> set Payment = Principal * MonthlyRate / (1 - pow(1 / (1 + MonthlyRate), Months))
```

The write must prove: divisor nonzero, `Payment > 0`, and the interest-coverage rule. All three are TRUE and legibly provable: `1/(1+r) ∈ (0,1)` for `r > 0` (division-by-greater-than-one lemma); `pow(q, n) ∈ (0,1)` for `q ∈ (0,1), n ≥ 1` (powers-below-one lemma); divisor `∈ (0,1)` hence nonzero; dividing a positive by a value in `(0,1)` yields at least the numerator (division-monotonicity lemma) ⇒ `Payment ≥ Principal·r` ✓. Each step is one teachable sentence. **The central nonlinear derived write of consumer lending compiles clean under the right engine.** Under the *current* engine (intervals + single-pass relations, spec:247,256) this rejects — which would be prove-or-reject forcing worse across the heart of the domain. The honest finding is: *the spec under-powers the engine here; it should adopt the named-lemma catalog and division-monotonicity reasoning.* Posture is not the problem.

Contingent variant: add `rule Payment <= Principal because "…"`. FALSE at `Months = 12`, `r = 0.03`? (Payment ≈ P·0.1005 — fine, true here; but at an extreme like `Months=1` if `min 1`, payment = P(1+r) > P.) The engine *refutes-as-universal with a witness valuation*. **FORCES-BETTER**: the author confronts "for short terms the payment can exceed principal — is my rule wrong or my term floor wrong?", a genuine domain decision. Remediation: correct the bound or tighten the term floor — both domain-meaningful.

### 3.2 Tax brackets — piecewise marginal computation. NOT IN THE CELL (under-powered-engine finding)

```precept
field Income as money in 'USD' nonnegative max '10000000.00 USD'
field Tax    as money in 'USD' nonnegative

rule Tax <= Income * 0.37 because "Total tax can never exceed the top marginal rate applied to the whole income"

on Assess -> set Tax =
    if Income <= '11000.00 USD' then Income * 0.10
    else if Income <= '44725.00 USD' then '1100.00 USD' + (Income - '11000.00 USD') * 0.12
    else '5147.00 USD' + (Income - '44725.00 USD') * 0.22
```

Both the `nonnegative` band and the effective-rate rule are TRUE, and provable by per-branch case-split + Farkas: e.g. in the middle branch the narrowing `Income > 11000` gives `1100 + 0.12(Income − 11000) ≥ 1100 ≥ 0` and `≤ 0.37·Income ⟺ 0.25·Income ≥ −220 + 0.12·11000…` — two-line linear certificates per branch, each readable. **Bracket tables — the bread and butter of tax precepts — must compile clean; a rejection here is an engine deficiency, never an author debt.** Under the current interval engine the branches likely join into a loose hull and this rejects; that is the spec under-powering the engine, and the FORCES-WORSE appearance evaporates once branch-narrowed linear proof exists. (This case is the strongest argument that ratifying prove-or-reject *without* simultaneously ratifying the engine upgrade would be a domain catastrophe.)

### 3.3 Options — payoff bounds and margin. Split verdict

Intrinsic value (NOT in the cell):

```precept
field Spot      as money in 'USD' nonnegative
field Strike    as money in 'USD' positive
field Intrinsic as money in 'USD' nonnegative

rule Intrinsic <= Spot because "A call's intrinsic value can never exceed the underlying price itself"

on Mark -> set Intrinsic = max(Spot - Strike, '0.00 USD')
```

`nonnegative`: `max(x, 0) ≥ 0` — one lemma. `Intrinsic ≤ Spot`: case-split — if `Spot − Strike ≥ 0`, then result `= Spot − Strike ≤ Spot` since `Strike ≥ 0`; else result `= 0 ≤ Spot`. Legible; should compile clean. Current relational engine cannot relate a `max()` result to a third field → under-powered finding again.

Margin sufficiency (**FORCES-BETTER**, the overdraft pattern's derivatives twin):

```precept
field NetLiq    as money in 'USD' nonnegative
field MarginReq as money in 'USD' nonnegative
rule MarginReq <= NetLiq because "Required margin above account equity is a margin call, not a bookable position"

on Book -> set MarginReq = Premium + Spot * 0.20 * Contracts
```

Genuinely contingent — a large-enough position breaches under any engine, and the witness the engine exhibits *is a margin-call scenario*. Forced remediation: a guarded row + `-> reject "Position requires {…} margin against {NetLiq} equity — margin call"` — which is exactly the domain's answer. The rejection surfaced an unanswered domain decision (what happens when margin exceeds equity), and the fixed precept is strictly better.

### 3.4 Accounting balance identity — FORCES-BETTER

```precept
field TotalDebits  as money in 'USD' nonnegative
field TotalCredits as money in 'USD' nonnegative
field NetPosition  as money in 'USD'          # unbounded — can be negative

field Imbalance as money in 'USD' min '0.00 USD' max '0.00 USD'   # the trial-balance identity as a band
on Close -> set Imbalance = TotalDebits - TotalCredits
```

Unprovable without a premise — and the forced premise **is the double-entry invariant itself**: `rule TotalDebits == TotalCredits because "Every posting must balance — double-entry"`. Governance then refuses the unbalanced *posting* at ingress (addressed to the poster, with the reason), and the write-site obligation discharges by Farkas (equality premise ⇒ difference = 0). Prove-or-reject forced the author to put the domain's foundational invariant where it belongs — on the inputs. Textbook FORCES-BETTER.

### 3.5 FX — settlement caps and cross-rates. FORCES-BETTER with one dissolved residual

```precept
field TradeAmountEur as money in 'EUR' nonnegative max '10000000.00 EUR'   # per-trade limit — a real desk control
field UsdEurRate     as exchangerate in 'USD/EUR' min 0.70 max 1.60        # rate-feed sanity collar — real
field SettlementUsd  as money in 'USD' nonnegative max '16000000.00 USD'   # daily settlement cap — real

on Settle -> set SettlementUsd = round(TradeAmountEur * UsdEurRate, 2)
```

With both input bounds declared, containment is legible: product of maxima `= 16,000,000.00` exactly, plus the place-value lemma (`round(x,2) ≤ x` when x is at or below a 2-place-representable cap... precisely: `round(x,2) ≤ 16,000,000.00` for `x ≤ 16,000,000`). Compiles clean. Without the input bounds it rejects — and here the corpus's "214 unbounded money fields" objection (`research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md:133,156`) meets a domain-specific answer: **in finance, virtually every input has a real institutional limit** (per-trade limits, rate collars, term caps, retention ≤ treaty limit, factors ∈ [0,1]). The forced bound is *not invented* — it is an unstated control being made explicit. FORCES-BETTER in this domain, whatever is true of generic samples.

The residual I hunted: **the true limit is dynamic and externally owned** (an intraday limits engine). Hardcoding `max '16000000.00 USD'` would be a stale fiction — a genuine FORCES-WORSE vector against one-file. But it dissolves: model the limit as a governed *field* (`field DailyLimit as money in 'USD'` updated by a `SetLimit` event from the limits system) and write the containment as a guard: `from Open on Settle when TradeAmountEur * UsdEurRate <= DailyLimit -> set … ; from Open on Settle -> reject "Trade exceeds today's settlement limit of {DailyLimit}"`. Fully domain-honest, refusal addressed to the caller, no invented number. The cost is that the formula appears twice (guard and set) — see §4.3.

### 3.6 Actuarial — three sub-cases

**(a) Earned premium and the clock — FORCES-BETTER.** `set EarnedPremium = WrittenPremium * DaysElapsed / PolicyDays` into `EarnedPremium <= WrittenPremium` needs `DaysElapsed <= PolicyDays` (declarable, true, domain-real) — but if `DaysElapsed` derives from `now()` and a future-dated inception, it can be negative. `now()` is the one source with no ingress to govern and no supplier to refuse. The forced remediation is a guard `when InceptionDate <= now()` + `reject "Policy has not yet incepted — no premium can be earned"`. The rejection surfaced the pre-inception case, which the naive precept silently mispriced. Better.

**(b) Reserve ≤ face — NOT IN THE CELL, because actuarial inputs are honestly bound-rich.** Net-premium reserve written with table-driven factor fields: `field AxFactor as decimal min 0.0 max 1.0` (a true property of actuarial present-value factors, not an invention). `set Reserve = FaceAmount * AxFactor - NetPremium * AnnuityFactor` proves `Reserve <= FaceAmount` by intervals + sign lemmas alone. The domain hands the prover real bounds; the posture costs nothing.

**(c) Hedge-accounting effectiveness (the 80–125% rule) — FORCES-BETTER, and instructive.**

```precept
# WRONG MODEL — rejected under prove-or-reject:
field EffectivenessRatio as decimal min 0.80 max 1.25   # the real 80–125 rule as a band
on Assess -> set EffectivenessRatio = ChangeInHedge / ChangeInHedged
```

Neither provable nor dead (and the divisor obligation fires too). But the *domain* semantics of a breach is not "invalid configuration" — it is "hedge accounting is discontinued," a lifecycle event. The rejection forces the author to discover the modeling error and move it where it belongs:

```precept
from Active on Assess when ChangeInHedged != '0.00 USD'
        and abs(ChangeInHedge) >= abs(ChangeInHedged) * 0.80
        and abs(ChangeInHedge) <= abs(ChangeInHedged) * 1.25
    -> transition Effective
from Active on Assess -> transition Discontinued
```

Prove-or-reject here *teaches the band-vs-lifecycle distinction*: a bound is for values that must never exist; a domain-significant out-of-band outcome is a guarded transition. This also answers the "soft bound / review band" family generally (e.g., "reserve moved >10% from prior estimate needs peer review"): the language already expresses it as guard-routed states, and the field-band spelling was the wrong precept. GOVERN would have let the wrong model ship.

### 3.7 A genuine NEUTRAL

`set CrossRate = UsdJpy / UsdEur` into a cross-rate collar where the only available premise is the verbatim restatement `rule UsdJpy >= UsdEur * 155.0 … because "cross-rate consistency collar"`. The rule is the containment condition itself, pulled to the inputs. It is *mildly* domain-meaningful (triangular-consistency collars are a real market-data control), costs one line, and relocates the refusal to the rate feed's ingress. Neither better nor worse: the same check, same commit, one authored `because` richer. This is the shape most of the "cell" collapses to when no cleaner premise exists.

---

## 4. The FORCES-WORSE hunt — serious attempts and survival status

### 4.1 Attempt 1 (strongest): portfolio-variance nonnegativity under correlation consistency — the legibility ceiling made concrete

```precept
field W1  as decimal min 0.0 max 1.0
field W2  as decimal min 0.0 max 1.0
field V1  as decimal min 0.0 max 0.80    # asset vols
field V2  as decimal min 0.0 max 0.80
field R12 as decimal min -1.0 max 1.0    # correlation
field PortVar as decimal nonnegative      # variance is nonnegative BY MEANING — a real bound

on Remeasure -> set PortVar = pow(W1*V1, 2) + pow(W2*V2, 2) + 2*W1*W2*V1*V2*R12
```

Two-asset containment is TRUE (`= (W1·V1 − W2·V2)² + 2·W1·W2·V1·V2·(1+R12)`, a square plus a product of nonnegatives) — and that certificate ("rewrite as a square plus a nonnegative term") is *just barely* legible: each sentence is checkable, though *finding* the decomposition is search. A curated small-support-SOS lemma arguably admits it. **The three-asset version does not survive legibility**: with `R12,R13,R23 ∈ [−1,1]` containment is FALSE (witness: all correlations −1, equal weights), so the author must add the joint-consistency premise — expressible, and genuinely domain-named: `rule 1 + 2*R12*R13*R23 - pow(R12,2) - pow(R13,2) - pow(R23,2) >= 0 because "The three correlations must be jointly consistent"`. Containment given that premise is TRUE (positive-semidefiniteness of the correlation matrix ⇒ the quadratic form is nonnegative) but **provable only via quadratic-form/Cholesky reasoning whose certificate no teachable-sentence chain captures**. This is a real inhabitant of the cell: true, decidable band, containment provable only illegibly.

**Did it survive as FORCES-WORSE?** *Only weakly — and ultimately no.* The forced remediations, in order of honesty:

1. `when pow(W1*V1,2) + … >= 0.0 -> set PortVar = …` + `-> reject "The supplied volatilities and correlations are not jointly consistent"` — the guard restates the radicand (duplication cost, §4.3) but the reject reason is *true domain language*: an inconsistent correlation set is genuinely refusable input. Domain-honest.
2. `set PortVar = max(…, 0.0)` — one token; masks rather than proves; but it is *exactly* the floor production quant code applies for numerical hygiene. Mildly dishonest in exact-decimal Precept (the floor is a dead limb if the consistency premise holds), which is the residue of "worse" that survives: the author ships an operation whose triggering case they believe impossible, and a genuinely inconsistent input gets silently zeroed instead of surfaced.
3. Tighten the premise to something the linear engine consumes (e.g., all correlations `min 0.0` for a long-only equity book — then every term is a product of nonnegatives, sign-lemma legible). Domain-narrowing that is often actually true.

Verdict: the case demonstrates that the **legibility ceiling itself (not engine under-build) can force the author off the proof path** — but every off-ramp is either domain-honest (guard+reject with a true reason) or standard domain practice (the variance floor). It survives as *the* example that prove-or-reject's cost, at the true ceiling, is duplication and a mild fiction — not a dead-guard fiction with *no* domain meaning. I could not make it worse than that.

### 4.2 Attempt 2: aggregate reserving needs `sum` — not the posture's fault, but adjacent

Total case reserve ≤ policy aggregate, where the total is a sum over a collection of claim reserves: **inexpressible today** (`collection-types.md:860` holds back `sum`). The author flattens to scalar fields or keeps the aggregation outside the file — both erode one-file completeness for the single most common accounting/actuarial derivation shape. This never even reaches the cell (no derived write exists to reject), but it is the finance domain's loudest spec gap: **admit `sum` (at least) — its proof story is legible** (`sum of n elements each in [a,b], count ∈ [c,d]` → interval `[c·a, d·b]`, one sentence; per the same count-interval machinery spec:258 already ships). Recommendation: reverse the `sum` holdback for numeric elements.

### 4.3 Attempt 3: formula duplication in the guard+reject remediation — a construct gap, not a posture failure

Every contingent case's honest remediation (guard on the containment condition + authored reject) requires restating the derived formula in the guard, because a row's guard is evaluated before its actions and nothing lets the author name the computed value and route on it. For the amortization or variance formulas this means maintaining two copies of a 100+-character expression — a genuine drift hazard for exactly the audience the language targets. The posture is not wrong; the language is missing a shape. **Recommendation (to route through `/design`, per the gate — I am naming the gap, not settling syntax): a compute-then-check form** — some way for a row to bind an intermediate value once and either route on it or attach an authored refusal to the write. This is the single highest-leverage ergonomic fix for prove-or-reject in finance.

### 4.4 Attempt 4: genuinely-undecidable territory

Variable integer exponents (`pow(1 + Rate, Months)` with both fields symbolic) mixed into equalities/inequalities pushes toward exponential-Diophantine territory where no complete procedure exists at all. In practice the monotonicity lemmas (result increasing in both arguments over the governed boxes) certify the practical bounds; I could not construct a *finance-realistic* fragment where the needed fact was both true and beyond the lemma catalog *and* lacked a domain-honest guard remediation. The compounding cap case (`pow(1+r, n) ≤ 100`) resolves by evaluating the lemma-certified maximum at the box corner — legible — and when that maximum breaches, the case is contingent with a witness (rate/term combinations that DO breach), i.e., FORCES-BETTER: the author confronts the missing joint rate-term policy.

---

## 5. Findings

**F1. The cell is far thinner than the corpus debate assumes — but only under the right engine.** Most finance cases that *look* merely-unprovable are legibly provable: amortization payment bounds, tax-bracket effective-rate containment, option intrinsic-value bounds, reserve ≤ face with factor bands, rounding containment. Under the current engine (intervals + spec:256's single-pass relations) every one of these **spuriously rejects**, and prove-or-reject would force-worse across the core of the domain — bound-invention, formula contortions, deleted rules. The honest statement is therefore conditional: **prove-or-reject is the right posture in finance if and only if the spec commits to the maximally-legible engine** — complete linear arithmetic with Farkas witnesses, branch/guard narrowing with per-branch certificates, substitution/match discharge of declared (including nonlinear) premises, the named-lemma catalog, place-value rounding envelopes, and counterexample witnesses on rejection. Every piece emits a domain-readable certificate; none is an opaque solver. spec:225 should be rewritten from "no SMT/Z3" to a certificate-legibility criterion; spec:256's depth-bounds should be dropped as spec text (they are implementation staging, and the ~44ms corpus compile leaves enormous headroom).

**F2. The pullback ladder means prove-or-reject never hard-blocks, and its forced remediations are usually domain assets.** Every derivation bottoms out at governable ingress (or `now()`, handled by guards), so a verbatim premise rule — with a mandatory `because` — is always available, relocating the refusal to a party who *can* act on it. Where the author can state a true reason, the file gains a documented control (double-entry, trade limits, rate collars, correlation consistency, inception-before-earning); where they cannot, the bound was fiction and its removal is the fix. This directly answers Frank's "recoverable by whom" and simultaneously blunts his opponent's bound-invention charge *for this domain*: finance inputs are institutionally bound-rich, and the forced declarations are almost always unstated real controls.

**F3. FORCES-WORSE result: no case survived cleanly.** My strongest attempt (three-asset variance nonnegativity given the correlation-consistency determinant premise — true, decidable band, provable only via illegible PSD reasoning) is a genuine inhabitant of the cell, and it demonstrates the legibility ceiling is real and binding. But its forced remediations bottom out in either a domain-honest guarded refusal ("correlations not jointly consistent") or the domain's own standard variance floor. What survives is a *residue*, not a verdict: (a) duplicated formulas in guards (fixable by a compute-then-check construct, §4.3), and (b) a mild masking fiction when the author chooses `max(…, 0)` over the refusal. Nowhere did I find the posture forcing a bound **with no domain meaning** that the author must ship as fiction — the overdraft pattern's forcing-toward-truth generalized across all six finance areas, twice producing outcomes strictly better than what GOVERN would have permitted (margin-call handling, §3.3; the hedge-effectiveness band-vs-lifecycle modeling correction, §3.6c — GOVERN would have let a mismodeled band ship as a runtime refusal addressed to no one).

**F4. What the spec SHOULD say (challenged decisions, recommended dispositions):**
1. Replace spec:225's exclusion line with the certificate-legibility criterion (§1) and enumerate the admitted procedure classes.
2. Keep spec:266-272/208's prove-or-reject for derived writes into decidable bounds — *conditioned in the same spec section on the engine contract of (1)*, and add counterexample-witness emission to the rejection diagnostics (rejection = "here is the unhandled scenario," not "cannot prove").
3. Reverse `collection-types.md:860` for `sum` over numeric elements (legible interval story; unblocks reserving/accounting).
4. Route a compute-then-check row construct to `/design` (names the intermediate value; kills guard-formula duplication — the posture's main residual authoring cost).
5. No opt-in escape construct is *needed* for the finance domain on this evidence: the pullback ladder plus guard+reject already provide authored, `because`-carrying, legibly-addressed runtime refusal wherever the domain wants one — which is Frank's "authored ingress-of-intent" achieved with zero new surface.

**Method note:** fragments were constructed against `samples/loan-application.precept` (read in full) and the shipped function/type surface (spec §3.7 read in full; `business-domain-types.md` money/exchangerate sections). Corpus claims cite `bounds-only-constraint-enforcement-2026-06-05.md:133,156`. Provability claims are hand-derived certificates in the terms of §1's engine, not runs of the current engine; where the current engine's behavior is asserted (spurious rejection of relational/max/branch cases) it follows from the spec's own implementation statements at spec:247-258.