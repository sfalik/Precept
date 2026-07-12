# The Cell, Hunted: Definition-Derived Writes into Declared Bounds in Ops/Pricing Precepts

## 0. What I hold fixed vs. what I challenge (per the framing gate)

**Held fixed — identity-level, not up for revision in this analysis:**
- Prevention / structural impossibility of the fault floor (`docs/philosophy.md:39,49`)
- Determinism (`philosophy.md:22`)
- One file, complete rules — including its corollary that a domain expert decomposes a complex formula into named intermediate fields rather than one opaque expression (this idiom already dominates the corpus: `GrossProfit`, `IsLowStock` in `samples/inventory-item.precept:52,70`)
- Domain-expert author (`philosophy.md:92`)
- Honesty about approximation
- **Soundness over completeness** (`precept-language-spec.md:221`) and **proven-violations-only** (`:223`) — these two together are the actual identity-level content underneath "no false security," independent of any specific engine
- **Legibility of proof** — structured, inspectable, teachable-in-one-sentence, no opaque solver trace. I treat this as the identity commitment. I do **not** treat `precept-language-spec.md:225`'s specific sentence ("this is why SMT/Z3 solvers are excluded") as identity-level — that sentence is one *implementation* of the legibility principle, and the framing explicitly instructs me not to inherit it.
- The posture under study is given: pure prove-or-reject, no author escape. I do not re-litigate that choice; I test what it costs *given the most powerful legible engine*, not today's engine.

**Challenged — current-spec decisions I argue should move, named explicitly below in §4:**
- `precept-language-spec.md:225`'s specific SMT/Z3 exclusion line (reframe around legibility, not that sentence)
- The engine's current ceiling on relational reasoning: "single-pass... depth-bounded (no transitive chasing of a third field)" (`proof-engine-boundary-ruling-2026-07-06.md:25`)
- `collection-types.md:860` — sum/reduce "deliberately held back"
- Absence of interval-arithmetic-through-products as a first-class proof premise (products currently fall to governance, per the boundary ruling's row 10 treatment)
- Absence of a named-template library for standard business-formula shapes (convex combination / mediant, product-of-nonnegatives, low-degree factoring)

## 1. The crux distinction the corpus debate blurs

Before classifying a single case, one distinction has to be made explicit because both Frank's paper and the thesis elide it: **"merely unprovable" is not one thing.** A derived write `set F = E` into a declared bound `[lo, hi]` falls into exactly one of four buckets:

1. **Proved safe.** The engine can certify `E ⊆ [lo, hi]` from carried facts. Compiles clean. Not interesting.
2. **Proved dangerous.** A concrete witness exists — an assignment of governed-legitimate values to `E`'s free variables that produces a value outside `[lo, hi]`. This is **not** "unprovable" at all; it's the strongest form of provable. The overdraft exemplar (`Balance = Deposits - Withdrawals`, `min 0`) is in this bucket: `Deposits=0, Withdrawals=1` is a legitimate governed state and it produces `Balance=-1`. Reject is not even a hard call here.
3. **Provable-but-engine-underpowered.** No witness exists — the true value of `E` is always in `[lo, hi]` given the governed input facts — but the current (or even the spec-drafted) engine's abstraction is too weak to see it: it lacks interval-arithmetic-through-products, lacks a convex-combination/mediant rule, lacks sum/reduce, or artificially depth-bounds relational chaining. **This is not the cell.** Per the framing, this is a spec/engine gap: "the spec under-powers the engine here."
4. **Genuinely unresolved even by the maximally legible engine.** No witness exists, and no legible, terminating, structured-certificate procedure exists to show it either — because the containment fact, while true, only follows from a many-variable, high-degree, non-templatable polynomial relationship (a Positivstellensatz search with no bounded-degree certificate), or from a fact that is true only via case-explosion the domain expert could never read as a certificate. **This is the actual cell.**

I looked specifically for bucket 4 in realistic ops/pricing formulas and, as reported below, it is nearly empty in practice — almost everything that looks like it belongs there is actually bucket 2 or bucket 3.

## 2. Domain fragments

### 2a. Logistics/inventory — weighted average cost (bucket 3, not the cell)

From `samples/inventory-item.precept:52-56`, restructured as a derived write (the sample currently only declares the field; here is the natural stressed version):

```precept
field AverageCost as price in '{CatalogCurrency}' of '{StockingUnit.dimension}'
    default '0 {CatalogCurrency}/{StockingUnit}' nonnegative

on ReceiveShipment(ReceiptQty as quantity of '{StockingUnit.dimension}' positive,
                    ReceiptUnitCost as price in '{CatalogCurrency}' of '{StockingUnit.dimension}' nonnegative)
    -> set AverageCost = (AverageCost * QuantityOnHand + ReceiptUnitCost * ReceiptQty)
                          / (QuantityOnHand + ReceiptQty)
```

This is the textbook weighted-average-cost (mediant) identity: the new average is a convex combination of two nonnegative costs weighted by two nonnegative quantities. **The theorem "a weighted mean of values in `[lo,hi]` with nonnegative weights summing to a positive total stays in `[lo,hi]`" is a one-sentence, teachable, structured certificate** — the same shape of reasoning the philosophy already accepts for the overdraft case's inverse. It requires no opaque search: check each weight ≥ 0 (both quantities are `nonnegative`/`positive` by declaration), check the weight sum is positive (`QuantityOnHand + ReceiptQty > 0` — provable if `ReceiptQty positive`), done. Today's engine cannot see this only because it has no convex-combination/mediant premise and treats division and products as opaque to bound-propagation. **Verdict: bucket 3.** The honest finding is *"the proof engine should add a mediant/weighted-average certificate as a named legible template — not that rejecting this is inherently the right cost of prove-or-reject."** Concrete recommendation (routed to `/design`, not settled here): a proof-engine capability that recognizes `(a*w1 + b*w2)/(w1+w2)` shape and discharges containment from `a,b ∈ [lo,hi]`, `w1,w2 ≥ 0`, `w1+w2 > 0`.

### 2b. Dynamic pricing / stacked discounts — real corpus case (bucket 2, FORCES-BETTER)

`samples/shopping-cart.precept:54,204-206`:

```precept
field DiscountPercent as decimal default 0.0 nonnegative maxplaces 2
rule DiscountPercent <= 100 because "Combined promotion discount cannot exceed 100 percent"

event ApplyPromotion(PromotionCode as ~string notempty, DiscountPercent as decimal positive max 100 maxplaces 2)

from Cart on ApplyPromotion
    -> set DiscountPercent = DiscountPercent + ApplyPromotion.DiscountPercent
```

Apply a 60% promo, then a 50% promo: `DiscountPercent` reaches 110, directly violating the declared rule whose own `because` clause states the invariant it's supposed to prevent. **This is a live witness sitting in the current sample corpus, not a hypothetical** — `DiscountPercent=0` (governed default) plus two governed-legitimate `ApplyPromotion` events (each independently ≤100 per its own event-arg bound) reach 110. Under the maximally legible engine, this is **bucket 2, proved dangerous**, not "merely unprovable" — sum of two independently-bounded-by-100 positive values is trivially known (by the same interval arithmetic used everywhere else in the spec) to range up to 200, and 200 ⊄ [0,100].

**FORCES-BETTER.** The rejection surfaces a genuine, currently-unanswered business question the `because` clause already gestures at but the action logic never answers: *what happens when stacked promotions would exceed 100%?* Three domain-meaningful fixes, none a fiction:

```precept
# Fix A — cap combined discount, a real and common retail policy
from Cart on ApplyPromotion
    -> set DiscountPercent = clamp(DiscountPercent + ApplyPromotion.DiscountPercent, 0.0, 100.0)

# Fix B — reject the stacking outright, a real and common retail policy
from Cart on ApplyPromotion when DiscountPercent + ApplyPromotion.DiscountPercent > 100
    -> reject "Combined promotion discount would exceed 100 percent"

# Fix C — declare the invariant as a rule and let the guard discharge it
rule DiscountPercent + 100 <= 200 because "..."   # (illustrative only — Fix A/B are the real answers)
```

Fix A is worth dwelling on: `clamp` already exists as a first-class function (`src/Precept/Language/Functions.cs:91`, `precept-language-spec.md:1574`, used correctly elsewhere in this very sample at line 218: `set DiscountPercent = max(0.0, DiscountPercent - RemovePromotion.AppliedDiscount)`). `clamp(x, lo, hi)` is *definitionally* bounded in `[lo, hi]` — the containment proof is not "hard," it's a structural fact about the function's own postcondition, exactly as legible as `abs`. So the "saturating semantics" escape a domain expert might reach for is **already in the language**, already used in this file for the symmetric floor case, and the rejection here would correctly teach the author to reach for it on the ceiling side too. This is strong evidence against a common worry (that prove-or-reject with no opt-in forces authors into fictions when they want saturating/clamped semantics) — the clamp exists precisely to make that intent explicit and it is itself trivially provable.

### 2c. Rate/fee schedules — surcharge/discount netting (bucket 2, FORCES-BETTER)

```precept
field BaseRate as money in '{Currency}' nonnegative
field FuelSurcharge as money in '{Currency}' nonnegative
field LoyaltyDiscount as money in '{Currency}' nonnegative

field FinalRate as money in '{Currency}' nonnegative
    <- BaseRate + FuelSurcharge - LoyaltyDiscount
```

`LoyaltyDiscount` could legitimately exceed `BaseRate + FuelSurcharge` for a heavy loyalty tier on a light shipment — a concrete witness exists within the governed domain of each independently-bounded operand. **Bucket 2.** FORCES-BETTER: forces the author to answer "can a discount exceed the billable amount, and if so what happens" — either `rule LoyaltyDiscount <= BaseRate + FuelSurcharge because "..."` (a real relational fact worth declaring, cf. `MinimumCharge <= BaseFee` already in `samples/fee-schedule.precept:19`), or `clamp(..., 0, ...)`, or an explicit reject. None of these is an invented-for-the-prover fiction; each is a normal rate-schedule policy a pricing analyst would recognize and needs to pick anyway.

### 2d. Weighted statistics / composite scoring (splits bucket 3 / bucket 2 depending on weight provenance)

```precept
field Accuracy as decimal nonnegative max 100
field Speed as decimal nonnegative max 100
field Style as decimal nonnegative max 100
field WAccuracy as decimal nonnegative max 1 maxplaces 2
field WSpeed as decimal nonnegative max 1 maxplaces 2
field WStyle as decimal nonnegative max 1 maxplaces 2
rule WAccuracy + WSpeed + WStyle == 1 because "Composite weights must total 100%"

field FinalScore as decimal nonnegative max 100
    <- Accuracy*WAccuracy + Speed*WSpeed + Style*WStyle
```

If the weight-sum-to-1 rule is genuinely declared and enforced (as above), this is the exact convex-combination shape from 2a — **bucket 3**: provable by the same mediant/weighted-mean template, currently unprovable only because the engine lacks (i) interval-arithmetic-through-products and (ii) a checkable "weights are nonnegative and sum to a known constant" premise recognizer. Honest finding: spec should grow a convex-combination certificate, generalized from 2-operand mediant to n-operand weighted sum (this generalization is itself legible — it's still "each weight ≥ 0, weights sum to a fixed value, so the weighted combination is between min and max of the scored values," just with more terms).

If, instead, the weights are *not* constrained to sum to 1 (an ops team edits three independent sliders with no cross-rule), this degrades to **bucket 2**: a witness (e.g., `WAccuracy=1, WSpeed=1, WStyle=1`, each independently within `max 1`) drives `FinalScore` to 300. FORCES-BETTER — the missing weight-sum rule is exactly the domain fact the rejection should surface, and it's the more common real-world bug (composite scoring systems silently misweighting is a known operational failure mode, not a hypothetical).

### 2e. Gaming/scoring — Elo-style rating update (bucket 2, FORCES-BETTER — with a bucket-3 sub-question worth naming)

```precept
field Rating as integer nonnegative max 3000
field KFactor as integer positive max 32

on RecordMatch(ActualScore as decimal min 0 max 1, ExpectedScore as decimal min 0 max 1)
    -> set Rating = Rating + KFactor * (ActualScore - ExpectedScore)
```

`ActualScore - ExpectedScore ∈ [-1, 1]`, so the update term is bounded by `±KFactor ≤ ±32` — that part is ordinary interval arithmetic (bucket-3 shape, trivially provable once the engine does interval-arithmetic-through-products/differences). But `Rating` near the declared ceiling (`Rating=2990`) plus a `+32` update produces `3022 ⊄ [0,3000]` — a real, constructible witness. **Bucket 2, FORCES-BETTER.** The rejection surfaces the actual unresolved question a ratings system designer must answer: what happens at the rating ceiling — clamp (`clamp(Rating + delta, 0, 3000)`), reject the update, or was `max 3000` never meant to be a hard ceiling (in which case the *fix* is removing a bound that doesn't reflect a real invariant — also a legitimate, better outcome than silently permitting a value the field's own declaration claims can't happen).

I also tried a variant with a logistic-function expected-score formula (`ExpectedScore = 1/(1+10^((OpponentRating-Rating)/400))`) to probe whether transcendental functions push this into genuine opacity. They don't, on inspection: the relevant fact ("the logistic function's range is `(0,1)` for all real input") is a single named identity attachable to that one function, exactly the kind of "named nonlinear identity" the framing explicitly allows as legible — it's bucket 3 (a missing named-function fact), not bucket 4.

### 2f. Quotas & rate limits — token bucket (bucket 2, resolves via existing `clamp`/`min`)

```precept
field Tokens as decimal nonnegative max Capacity
field Capacity as decimal positive

on ConsumeToken(RefillAmount as decimal nonnegative, Cost as decimal positive max 10)
    -> set Tokens = min(Capacity, Tokens + RefillAmount) - Cost
```

`min(Capacity, ...)` is a legible, definitional clamp on the upper side (`min(a,b) ≤ b` by construction — trivial). But after clamping to at most `Capacity`, subtracting `Cost` (up to 10) can drive the result below 0 whenever `Tokens` was near-exhausted — a real witness. **Bucket 2, FORCES-BETTER**: this is exactly rate limiting's actual point — the rejection forces the author to write the guard that *is* the feature (`when Tokens + RefillAmount >= Cost -> ... else -> reject "rate limit exceeded"`), not a bound invented to please a prover.

## 3. The FORCES-WORSE hunt — one serious attempt, reported honestly

Per instructions, here is the hardest attempt I could construct, and whether it survives.

**Attempt:** An actuarial "combined ratio" solvency check blending four correlated ratios with a non-obvious cross-term inequality — e.g., a reserve-adequacy formula where `Adequate = (Reserves + PresentValue(FutureLosses, DiscountRate)) - (Premium - AcquisitionCost) ≥ 0`, where the true invariant depends on a relationship between `DiscountRate`, a time-value exponent, and three independently-editable ratio fields, such that no single named template (convex combination, product-of-nonnegatives, sum, mediant) directly matches the polynomial's shape, and only a general real-closed-field decision procedure (Tarski/CAD) — or an ad hoc case-split the domain expert could not read as one certificate — would show containment.

**Does it survive scrutiny?** No, for three compounding reasons:

1. **Decomposition dissolves it.** Precept's own idiom (one file, complete rules, but *named intermediate derived fields* — `GrossProfit`, `IsLowStock`) is exactly the tool a domain expert already uses for a formula this complex. Split `PresentValue(...)` into its own `field DiscountedFutureLosses as money nonnegative <- ...` with its own provable sub-bound (nonnegative, by product-of-nonnegatives if the discount factor is proven in `[0,1]` — itself bucket 3), and the parent inequality becomes a sum of two nonnegative/bounded named quantities minus a bounded quantity — back into bucket-2-or-3 territory, discharged or genuinely-witnessed at each named step. This is not a workaround invented to dodge the prover; it is how actuaries already structure reserve formulas (named sub-reserves, not one giant expression), so the decomposition is domain-native, not fiction-native.
2. **Realistic business polynomials are low-degree and few-variable.** A domain expert writing a `.precept` rule is, by construction of the authoring audience (`philosophy.md:92`), not writing five-variable degree-4 polynomials in one expression — they write sums, ratios, percentage stacks, and clamps. The bounded-template certificate library (mediant/convex-combination, product-of-nonnegatives, factoring for low-degree forms, sum-with-nonnegative-weights) covers essentially the entire realistic vocabulary this analysis's six domains produced. I could not construct a *realistic*, unavoidably-single-expression ops/pricing formula that both (a) has a genuine no-counterexample invariant and (b) resists every named template and resists decomposition.
3. **Where a residue is theoretically imaginable, it's still closer to bucket 2 than bucket 4 in practice** — a formula gnarly enough to need a general polynomial solver is also gnarly enough that the domain expert who wrote it almost certainly has *not* actually verified the invariant holds for every governed edge case either; the more honest disposition is "unverified claim of an invariant," which the rejection correctly surfaces as a question ("prove it or restructure it") rather than a false negative imposed on a definitely-safe formula.

**Verdict: the serious FORCES-WORSE attempt does not survive.** I found no realistic case in logistics/inventory, pricing/discounts, rate schedules, weighted statistics, gaming/scoring, or quotas/rate-limits that is genuinely bucket 4 (opaque-only or truly undecidable) *and* forces a meaningless invented bound rather than a real domain answer or a legible engine extension.

## 4. What the spec should say instead (challenged decisions, with the four legs)

**4.1 Reframe `precept-language-spec.md:225` around the legibility principle, not the SMT/Z3 sentence.**
- *Rationale:* the identity commitment is inspectability of the certificate, not exclusion of any specific solver technology; a Fourier–Motzkin-style linear elimination, a convex-combination witness, or a named nonlinear identity can be exactly as legible as today's interval checks while proving strictly more.
- *Alternatives rejected:* keep the SMT-specific line as-is (conflates "opaque" with "any solver," blocking legible extensions for no legibility reason); adopt full SMT (reintroduces opaque traces, violates the actual principle).
- *Precedent:* SPARK's bounded, named proof obligations; Miné's interval/octagon lineage (cited in the thesis's own research, `interval-vs-value-evaluation-prior-art`) — all legible, none of them SMT, several strictly more powerful than plain intervals.
- *Tradeoff:* a bounded-template library still has a ceiling (item 3 above); the spec must say so honestly rather than implying totality.

**4.2 Extend relational reasoning beyond single-pass/depth-1** (currently: "no transitive chasing of a third field," `proof-engine-boundary-ruling-2026-07-06.md:25`) to a legible, terminating, bounded closure (Fourier–Motzkin-style elimination over the small number of fields realistically co-occurring in one action chain).
- *Rationale:* 2b/2c above show two- and three-field relational facts (`LoyaltyDiscount ≤ BaseRate + FuelSurcharge`) are exactly the shape real rate schedules need; artificially stopping at depth 1 turns real, provable relations into governed-only facts for no legibility reason.
- *Alternatives rejected:* full polyhedral (Omega-test) closure over unbounded field counts (loses the "small, teachable certificate" property at scale); leave depth-bounded (under-serves the corpus's own three-field cases).
- *Precedent:* Fourier–Motzkin elimination is a textbook, hand-checkable procedure — each step names exactly which two premises combined.
- *Tradeoff:* certificate size grows with field count; needs an explicit small bound to stay legible and terminating.

**4.3 Add interval-arithmetic-through-products/quotients as a first-class proof premise** (currently: products fall to governance per the boundary ruling's treatment).
- *Rationale:* "product of two nonnegative-bounded operands is nonnegative-and-bounded" is standard interval arithmetic, one sentence, no opacity — 2a, 2d, 2e all need it.
- *Alternatives rejected:* leave all products governed-only (systematically under-proves the fault floor for the majority-nonlinear ops/pricing domain — the boundary ruling itself notes 21/77 sample files use products).
- *Precedent:* interval arithmetic multiplication rule (Moore, *Interval Analysis*, 1966) — the oldest, most legible nonlinear technique there is.
- *Tradeoff:* none significant; this is arguably underpowering the engine relative to a technique already implicitly assumed by the spec's own "numeric interval reasoning" clause (`:203`).

**4.4 Add a bounded weighted-sum / convex-combination (mediant) certificate, and a bounded, monotone-premise `sum`/`reduce`** (currently held back, `collection-types.md:860`).
- *Rationale:* 2a, 2d, and inventory/statistics aggregation (log totals, weighted averages) are the single most common nonlinear shape in this domain and all reduce to "weighted mean of bounded values, with a checkable nonnegative-weights/positive-total premise, stays within the bound of the values."
- *Alternatives rejected:* keep sum/reduce out entirely (forces authors either to unroll fixed-arity sums by hand, as the samples already do, or to leave genuinely provable aggregation ungoverned); add unrestricted higher-order `map`/`filter`/`reduce` with arbitrary lambdas (crosses into general computation, loses termination/legibility guarantees the spec is right to protect).
- *Precedent:* the framing's own allowance for "an explicit convex-combination witness" and "sum/reduce" as examples of legible-but-currently-missing power.
- *Tradeoff:* scope must stay narrow (fixed monotone/nonnegative premise shapes) to avoid reopening the `map`/`filter` computation-surface question the spec deliberately closed for other reasons.

All four of the above are **capability recommendations**, not settled syntax — per the Language Surface Design gate, the concrete construct (if any new syntax is needed at all, e.g. for 4.4) belongs in `/design` with Tier-2/3 owner consultation, not decided here.

## 5. Bottom line

Scoped correctly against a maximally legible engine, **the cell is much smaller than the corpus debate assumes, and in the six ops/pricing domains probed here it is empty of realistic FORCES-WORSE occupants.** Every apparent "merely unprovable derived write into a real bound" case decomposed into one of:

- **bucket 2 (proved dangerous — FORCES-BETTER, undisputed):** discount stacking, surcharge/discount netting, Elo-ceiling approach, token-bucket depletion — all have constructible witnesses within governed input domains, and the forced remediation (`clamp`, an explicit relational `rule`, a guard, or an explicit `reject`) is domain-meaningful, often the exact feature the field exists to express (rate limiting *is* "what happens when the request would exceed the bucket");
- **bucket 3 (engine underpowered — spec should grow, not accept rejection as inherent):** weighted-average cost, properly-weight-constrained composite scores — all fall to a small, identifiable, legible extension (products-of-intervals, convex-combination certificate, bounded relational closure) that the spec should add rather than treat its absence as a permanent cost of prove-or-reject;
- **bucket 4 (genuinely in the cell):** I could not construct a realistic, single-expression ops/pricing survivor. The one serious attempt (a many-variable actuarial reserve identity) dissolves under Precept's own named-intermediate-field idiom before it ever needs an opaque solver.

The practical implication for the posture under study: **pure prove-or-reject with no escape hatch is not expensive in this domain because of proof-engine weakness — it is expensive today only because the engine is under-built relative to what legibility permits.** Closing §4's gaps converts most of the "friction" the thesis worries about into either clean compiles (bucket 3 → proved safe) or into exactly the kind of forced domain answer (bucket 2) that is prevention's entire point. The corpus itself already contains a live example of the latter going unanswered — `samples/shopping-cart.precept`'s discount-stacking path — which is the strongest evidence in this analysis that "reject forces a real, currently-missing business decision" is the true shape of this domain's cell, not "reject forces a fiction."