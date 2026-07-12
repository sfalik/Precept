# Deep analysis: merely-unprovable derived writes into non-proof-carrying bounds (operations/pricing domains)

## Grounding and method

Read before constructing cases: `docs/language/precept-language-spec.md` (§0.6/§0.7, the SMT exclusion at :225, the opaque-proof exclusion at :292, the no-deferral fault-prevention language at :266/:268, the count-bound inductive-discharge pattern at :258), `samples/fee-schedule.precept` and `samples/inventory-item.precept` for DSL shape/conventions, and the three framing documents (`docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md` §2.4(ii), `docs/Working/frank-prove-or-reject-position-2026-07-11.md`, `docs/Working/fable-analysis-of-frank-response-2026-07-11.md`), plus `research/architecture/compiler/over-proving-pullback-reconstruction-2026-07-06.md` and `docs/Working/compile-time-niche-decision-packet-2026-06-10.md` for what the locked relational design (octagon-style, 2-variable) and the corpus evidence (214 unbounded vs. 10 bounded money fields, `research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md:120-122`) already establish. These are arguments, not authority, per the task's discipline.

**Design-ceiling calibration used throughout** (per the task's explicit instruction not to undercount): a fully-built solver-free engine can do (a) box interval arithmetic, including products/quotients of *independently bounded* intervals (Moore interval arithmetic — sound, legible, no solver); (b) affine relational closure of arbitrary arity — the "3-variable sums" example the task itself names as *not* fundamental is folded in; I extend this to a general bounded-polyhedra affine domain, not just 2-variable octagons, since nothing about polyhedra requires SMT; (c) inductive per-transition invariants over accumulators guarded at every write (the mechanism `precept-language-spec.md:258` already locks for `mincount`/`maxcount`: "an unprovable grow/shrink emits `CountBoundViolation`… discharged by an author guard, never deferred to a runtime check" — I treat this pattern as generalizable to any guarded accumulator, not just cardinality). What remains **fundamentally** out of reach: **nonlinear reasoning about products/ratios of two or more free (non-constant) variables where the tightening depends on a correlation between them that isn't itself a stated affine fact** — proving *that* class of inequality in general requires real-closed-field/nonlinear decision procedures, which is exactly the SMT/Z3 family `precept-language-spec.md:225` excludes on principle ("If the compiler cannot prove safety, it says so explicitly… this is why SMT/Z3 solvers are excluded even when they could prove more"), reinforced by the opaque-witness exclusion at `:292`. That is the one fundamental line I use below; everything short of it is scored as provable-at-ceiling (compiles clean) and explicitly called out as such rather than miscounted.

---

## Case 1 — Logistics: warehouse capacity accumulator (positive control)

```
field WarehouseCapacity as quantity of '{StockingUnit.dimension}' default '0 {StockingUnit}' nonnegative editable
field TotalAllocated as quantity of '{StockingUnit.dimension}' default '0 {StockingUnit}' nonnegative

event AllocateStock(OrderId as string, Qty as quantity of '{StockingUnit.dimension}')
on AllocateStock ensure AllocateStock.Qty > '0 {StockingUnit}' because "Allocation quantity must be positive"

from Active on AllocateStock
    -> set TotalAllocated = TotalAllocated + AllocateStock.Qty
    -> no transition
```

`TotalAllocated` is an unguarded running sum into a `nonnegative`-only field with no natural upper bound stated — but the *real* domain concern is capacity, not sign. If the author had declared `max WarehouseCapacity`-style containment (or just relied on "don't over-allocate" as an implicit assumption), an unguarded accumulator is unprovable: nothing bounds `TotalAllocated` above.

**Remediation forced:** add the guard that should have been there anyway —
```
from Active on AllocateStock when TotalAllocated + AllocateStock.Qty <= WarehouseCapacity
    -> set TotalAllocated = TotalAllocated + AllocateStock.Qty
    -> no transition
from Active on AllocateStock
    -> reject "Allocating {AllocateStock.Qty} would exceed warehouse capacity {WarehouseCapacity} (currently {TotalAllocated} allocated)"
```
This is now inductively provable (guarded-accumulator discharge, the same shape `precept-language-spec.md:258` already locks for cardinality) and, crucially, is *exactly* the business rule any competent WMS needs regardless of proof posture — "don't allocate more than the warehouse holds" is not a fact invented for the prover.

**Verdict: FORCES-BETTER.** Structurally identical to the overdraft exemplar — subtraction/addition of an external, uncapped delta into a bound whose natural guard is a genuine, previously-unstated domain decision.

---

## Case 2 — Dynamic pricing: stacked discounts

**2a. Percentage stack (calibration case — should NOT be miscounted as unprovable):**
```
field BasePrice as money in 'USD' nonnegative
field VolumeDiscount as decimal default 0 nonnegative max 1 maxplaces 4
field LoyaltyDiscount as decimal default 0 nonnegative max 1 maxplaces 4
field FinalPrice as money in 'USD' <- BasePrice * (1 - VolumeDiscount) * (1 - LoyaltyDiscount)
```
Each factor `(1 - VolumeDiscount)` and `(1 - LoyaltyDiscount)` is bounded in `[0,1]` by the fields' own declared `max`/`nonnegative`; a product of independently-bounded, sign-known intervals is exactly what box interval arithmetic computes soundly and exactly here (no correlation needed — the extremes are 0 and `BasePrice`). **This compiles clean at design ceiling.** I flag it explicitly because it is easy to mistake for a "scary nonlinear multiplication" case; it isn't — it's the textbook case interval arithmetic exists for.

**Verdict: NEUTRAL** (not a rejection at all — included to calibrate the boundary, not to pad the FORCES-* count).

**2b. Absolute stack into nonnegative price (real target-cell case):**
```
field CouponAmount as money in 'USD' nonnegative editable
field PromoCredit as money in 'USD' nonnegative editable
field FinalPrice as money in 'USD' nonnegative <- BasePrice - CouponAmount - PromoCredit
```
`CouponAmount`/`PromoCredit` are independently editable money fields with no natural ceiling relative to `BasePrice` (a promo system can in principle stack more credit than the item costs). Unprovable: nothing ties the subtrahends to `BasePrice`.

**Remediation forced:** state the relationship (`rule CouponAmount + PromoCredit <= BasePrice`), guard the discount-application event, or add an explicit `reject` row for the over-discount case.

**Verdict: FORCES-BETTER** — same shape as overdraft, same reasoning as Case 1. "What happens when stacked discounts exceed price — clamp, refuse, or allow a credit balance?" is a real, previously-unanswered pricing-engine question.

---

## Case 3 — Interest/fee schedules: blended-APR usury cap (the serious FORCES-WORSE attempt — survives)

```
field Principal1 as money in 'USD' nonnegative editable
field Principal2 as money in 'USD' nonnegative editable
field Principal3 as money in 'USD' nonnegative editable
field Rate1 as decimal default 0 nonnegative max 0.30 maxplaces 4 editable
field Rate2 as decimal default 0 nonnegative max 0.32 maxplaces 4 editable
field Rate3 as decimal default 0 nonnegative max 0.28 maxplaces 4 editable

# Regulatory/disclosure ceiling — a real legal constraint, not an invented one.
field BlendedAPR as decimal
    <- (Principal1 * Rate1 + Principal2 * Rate2 + Principal3 * Rate3)
       / (Principal1 + Principal2 + Principal3)
    max 0.30
```

`BlendedAPR` is a weighted average — each term is a *product of two free (non-constant) fields* (a tranche's principal and its rate), summed and divided by the sum of the weights. This is the genuinely hard case:

- Interval box arithmetic on each `Principal_i * Rate_i` product is sound but treats the weights as *uncorrelated* with the rates — it cannot express "large loans get the safer rate," even when that is standing underwriting policy. The provable box for the whole expression, given each `Rate_i`'s own declared cap and *no* declared ceiling on principal size, is unbounded above (principal has no natural ceiling — loan sizes grow with the book).
- This is not "3-variable affine sums" (which the task correctly rules non-fundamental) — it is a **product of two free variables inside a ratio**, and any tightening beyond the loose box requires reasoning about a *correlation* between the weight and the rate. Stating that correlation as a rule (e.g. `rule Rate2 <= 0.30 - k * (Principal2 / TotalPrincipal)`) only relocates the nonlinearity: verifying that this rule, combined with the others, *entails* the aggregate bound requires combining two nonlinear (ratio) facts — exactly the class of inequality that needs a nonlinear/real-closed-field decision procedure to check soundly, which is the SMT/Z3 family excluded on principle (`precept-language-spec.md:225`, reinforced by the opaque-witness exclusion at `:292`). This is a **fundamental** limit, not an unimplemented one.

**What the compiler is forced to demand:** since box arithmetic is the only sound tool available, the only way to make the assignment provable is to put a hard, tight `max` on `Principal2` (and the others) small enough that `Principal2 * Rate2`'s worst case can't push the blend over 0.30 — e.g. `Principal2 max '50000 USD'`. That ceiling reflects *nothing about underwriting* (large qualified borrowers legitimately get large loans in that tranche; the real business protection is the size/rate correlation, which the engine cannot verify). It is bound-invention in its purest form — the exact "representability masquerading as policy" pattern named in `docs/Working/compile-time-niche-decision-packet-2026-06-10.md:129` and credited even by the pro-reject position in `docs/Working/frank-prove-or-reject-position-2026-07-11.md:107`.

**Differential check against GOVERN (required — a bad-modeling critique that also afflicts GOVERN is not a FORCES-WORSE finding):** under GOVERN, the identical `max 0.30` field is enforced at every write: the tentative post-transition value is computed, checked, and — in the overwhelming majority of real originations, where the size/rate correlation genuinely holds — the operation *succeeds silently, correctly, forever*; in the rare case a specific combination would breach the cap, the operation is refused gracefully with a legible reason ("this loan would push the blended portfolio APR to 31.4%, exceeding the 30% regulatory cap"), addressed to the actor, previewable via `Inspect` — precisely the first-class governed-refusal shape both `docs/Working/frank-prove-or-reject-position-2026-07-11.md:131-137` and `docs/Working/fable-analysis-of-frank-response-2026-07-11.md:61` treat as legitimate for overdraft. GOVERN pays **zero** ongoing friction for the 99.9% of compliant loans and correctly protects the rare edge case. Pure prove-or-reject cannot express "rarely, conditionally violates" at all — it must either reject the whole *definition* until an artificial principal ceiling is invented, or (if the author tries to write the exact guard GOVERN would apply) still fail, because verifying that guard's own postcondition is the identical excluded nonlinear step. The artificial ceiling will eventually **reject a genuine, compliant, well-underwritten large loan** for no domain reason — which is the opposite of "business logic cannot produce a wrong answer" (`docs/philosophy.md:49`): here, *refusing a valid transaction* is the wrong answer.

**Verdict: FORCES-WORSE — survives scrutiny.** Two independent corroborating instances in the same fundamental shape (nonlinear ratio-of-weighted-sums with free, uncorrelated weights), to show this isn't a one-off:

- **Statistics/weighted averages** — a supplier scorecard: `field BlendedDefectRate as decimal <- (Qty1*Rate1 + Qty2*Rate2 + Qty3*Rate3) / (Qty1+Qty2+Qty3) max 0.02` gating vendor certification. Same shape, same fundamental block, same forced-invention-of-a-fake-shipment-size-ceiling remediation.
- **Gaming/scoring** — a matchmaking fairness index: `field SkillGapRatio as decimal <- abs(TeamARating - TeamBRating) / (TeamARating + TeamBRating) max 0.20` gating "ranked eligible," where each team rating is itself a weighted average of individually-bounded player ratings. Same nonlinear-ratio-of-sums shape.

---

## Case 4 — Gaming/scoring economies: anti-cheat power-score cap (FORCES-BETTER)

```
field Kills as integer default 0 nonnegative
field Deaths as integer default 0 nonnegative
field Assists as integer default 0 nonnegative
field PowerScore as integer <- Kills * 3 + Assists - Deaths * 2 nonnegative max 100000
```
Here every term is *variable × constant* (linear, not variable × variable) — squarely inside affine/box reasoning, no fundamental block. Unprovable only because `Kills`/`Assists`/`Deaths` are otherwise unbounded per season.

**Remediation forced:** declare the real per-season ceiling that already exists in the game's own design (max matches per season × max kills per match), or reset accumulators on a season-boundary transition with a guard capping per-match contribution. Either way the compiler forces the author to state a fact ("how many kills are physically possible in a season") that is genuinely useful for anti-cheat/balance analytics independent of the proof engine.

**Verdict: FORCES-BETTER.**

---

## Case 5 — Resource quotas & rate limits: token-bucket rate limiter (FORCES-BETTER)

```
field TokensAvailable as integer default 1000 nonnegative max 1000

event Request(Cost as integer)
on Request ensure Request.Cost > 0 because "Request cost must be positive"

from Active on Request when TokensAvailable >= Request.Cost
    -> set TokensAvailable = TokensAvailable - Request.Cost
    -> no transition
from Active on Request
    -> reject "Rate limit exceeded — {Request.Cost} tokens requested, {TokensAvailable} available"
```
Same overdraft shape (subtraction of an external bounded delta into a `nonnegative` field); the natural pre-check guard is exactly the desired rate-limiting behavior, and it's identical under GOVERN or prove-or-reject (no differential loss).

**Verdict: FORCES-BETTER.**

---

## The FORCES-WORSE attempt that did NOT survive scrutiny (reported honestly)

**Attempted case:** an internal KPI/monitoring threshold masquerading as a governed bound — e.g. `field CostVarianceAlert as money in 'USD' max '1000 USD'` computed from a weighted-average-cost swing per shipment, intended as a fraud/anomaly-detection trigger rather than a data-integrity invariant. `SupplierUnitCost` is genuinely open-ended (rare/custom parts can cost far more than typical stock), so the derived swing is unprovable, and the only prove-or-reject remediation is an artificial cost ceiling that will eventually reject a legitimate expensive shipment.

**Why it fails as a *differential* FORCES-WORSE finding:** under GOVERN, the identical `max` field is *also* enforced unconditionally at every write (`precept-language-spec.md:268`; the runtime does not distinguish "this bound is really a monitoring threshold" from "this bound is a real invariant" — a violating write is refused, full stop). So a legitimate high-cost shipment is refused **either way** — at compile time (prove-or-reject) or at every single occurrence at runtime (GOVERN). The defect here is that the field was **mismodeled from the start** — a monitoring/alert signal should never have been declared with a hard structural `max` under *either* posture; it belongs on a guard/flag construct, not a governed field. Since the harm is identical under both postures, this is not evidence against prove-or-reject specifically — it's a modeling-discipline finding orthogonal to the posture question. I report it because it was a genuine, non-trivial candidate that looked promising before the differential check, and the differential check is exactly what a hunt for FORCES-WORSE (as opposed to FORCES-BAD-MODELING-EITHER-WAY) requires.

---

## Summary table

| Domain | Fragment | Shape | Fundamental block? | Verdict |
|---|---|---|---|---|
| Logistics/inventory | `TotalAllocated` accumulator vs `WarehouseCapacity` | guarded-accumulator, linear | No | FORCES-BETTER |
| Pricing (% stack) | `FinalPrice <- Base*(1-d1)*(1-d2)` | bounded-interval product | No — compiles clean | NEUTRAL |
| Pricing ($ stack) | `FinalPrice <- Base - Coupon - Promo`, nonnegative | overdraft-shape subtraction | No | FORCES-BETTER |
| Interest/fee schedules | `BlendedAPR` weighted average vs usury cap | ratio of Σ(variable×variable) | **Yes — nonlinear correlation, SMT-excluded** | **FORCES-WORSE (survives)** |
| Statistics/weighted averages | `BlendedDefectRate` vendor scorecard | same ratio-of-weighted-sums shape | Yes | FORCES-WORSE (corroborating) |
| Gaming/scoring | `PowerScore` anti-cheat cap | linear (var×const) accumulator | No | FORCES-BETTER |
| Gaming/scoring | `SkillGapRatio` matchmaking fairness | same ratio-of-weighted-sums shape | Yes | FORCES-WORSE (corroborating) |
| Quotas/rate limits | token-bucket `TokensAvailable` | overdraft-shape subtraction | No | FORCES-BETTER |
| (attempted) monitoring KPI | `CostVarianceAlert` cap | unbounded source into policy alert | No — but GOVERN equally bad | Attempted FORCES-WORSE, **did not survive** (not differential vs. GOVERN) |

## Overall finding

The "rejection forces a better precept" claim holds up strongly for the **linear/accumulator family** — any case shaped like *bounded-delta accumulated or subtracted into a nonnegative/capped field* (overdraft, capacity, discount stacking, rate limits, anti-cheat caps) reduces to a guard the business genuinely wants regardless of proof posture, and prove-or-reject and GOVERN converge on the same remediation. This generalizes the overdraft exemplar cleanly across five of the six requested domains.

It **breaks down** — genuinely, not just as friction — exactly where the derived value is a **ratio of weighted sums of two or more free variables whose product terms are meant to be correlated** (blended rates, weighted defect rates, fairness indices): the correlation that makes the true value safe is a real, statable, often already-practiced business fact, but *verifying* that a stated correlation entails the aggregate bound requires nonlinear reasoning Precept's design excludes by principle (no SMT, no opaque witnesses). In that cell, pure prove-or-reject cannot express "correctly governed 99.9% of the time, gracefully refused in rare edge cases" — its only way to compile is to invent a representability ceiling on an operand (loan size, shipment size, team rating) that has no domain meaning and will eventually misfire on legitimate data. This is the one place in the six domains where the hunt produced a FORCES-WORSE case that survives the differential-vs-GOVERN check, and it maps precisely onto the boundary the thesis and Frank's paper already argue over in the abstract (§2.4(ii)) — the operations/pricing corpus supplies concrete instances (weighted-average rate/quality/fairness metrics) of exactly the shape their debate left open.

**Files consulted (paths for follow-up):** `docs/language/precept-language-spec.md:225,258,266,268,292`; `docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md` §2.4; `docs/Working/frank-prove-or-reject-position-2026-07-11.md:69,107-137`; `docs/Working/fable-analysis-of-frank-response-2026-07-11.md:61-155`; `docs/Working/compile-time-niche-decision-packet-2026-06-10.md:129,184`; `research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md:120-122`; `research/architecture/compiler/over-proving-pullback-reconstruction-2026-07-06.md`; `samples/fee-schedule.precept`, `samples/inventory-item.precept` (DSL shape reference only — no sample precept covers pricing/logistics/gaming domains, confirming the prompt's premise that the corpus under-represents these verticals).