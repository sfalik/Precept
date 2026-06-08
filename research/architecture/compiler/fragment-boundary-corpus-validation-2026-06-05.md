---
status: Cited
external-engagement: purely-internal
authored: 2026-06-05
topic: empirical validation of the decidable data-relation fragment (intervals + two-variable unit-coefficient difference-bounds) against the full samples/ corpus; grounds OD-2 in docs/Working/precept-guarantee-specification-2026-06-05.md
---

# Fragment-Boundary Corpus Validation

**Question (OD-2, the data-fragment boundary).** Precept's proof engine can decide, **solver-free**, relations that live in (1) **intervals** (a single field versus a constant) and (2) **two-variable, unit-coefficient difference-bounds** (`X ≥ Y`, `X > Y`, `X − Y ≤ c`, `X = Y`), with the nuance that a **nonlinear subterm appearing identically on both sides can be atomized** (treated as one opaque variable) and stays in-fragment. Everything else is out-of-fragment: 3-or-more-term linear identities, non-unit-coefficient two-variable linear, value-level nonlinearity the proof must look inside, and non-numeric (string/choice/enum/congruence) relations.

**Does the real corpus's set of INVARIANTS — `rule`s, `ensure`s, guards — and the invariant-PRESERVATION checks they imply against the assignments, mostly fall IN-fragment, or is the fragment too narrow to be useful?**

**Verdict, one line:** **Acceptable.** ~82% of explicit `rule` invariants and ~91% of `ensure` invariants are in-fragment as written; the dominant out-of-fragment numeric shape is the **3-field balance identity** (`A + B ≤/== C`), which recurs in exactly four precepts and is a genuine constrained invariant (not a derived accumulator) — the one named candidate for a bounded extension. The other out-of-fragment shapes are non-numeric (choice/string/boolean) rules the fragment was never meant to cover, and **derived accumulators** (`<-` fields), which are computed, not constrained, and therefore need no preservation proof at all.

---

## Methodology

Corpus: all 77 `.precept` files under `samples/` (76 domain samples + `Test.precept`). Extraction by structural grep, then classification (programmatic first pass, hand-verified on every edge case).

Constructs extracted and counted across the corpus:

| Construct | How extracted | Count |
|---|---|---|
| `rule …` (global invariants) | `^\s*rule ` | **149** |
| `in X ensure …` / `on E ensure …` (state + event invariants) | `ensure ` | **565** |
| `when …` guard clauses | `when ` | **509** |
| `-> set <field> = <expr>` assignments | `-> set ` | **1806** |
| `field … <- <expr>` derived fields | `<-` | **32** |

Classification buckets (the rubric):

- **A1 — interval.** Single field vs constant: `X ≥ 0`, `X ≤ k`, `nonnegative`, `positive`, `min`/`max`, `.count`/`.length` vs N, `is set` (a presence/membership fact the interval/DBM machinery represents).
- **A2 — two-atom difference/order.** `X ≥ Y`, `X > Y`, `X − Y ≤ c`, `X == Y`, temporal orderings, two fields/args. An atom may be a field, an event arg, or a nonlinear subterm that recurs identically (**atomized**, flagged).
- **B1 — 3+-term linear.** Sums/differences of 3+ distinct atoms in one relation (`A + B ≤ C`, `A − B − C − D`), **and** non-unit-coefficient two-variable linear (`X ≤ c·Y` with `c ≠ 1`) — a difference-bound requires unit coefficients, so a scaled coefficient leaves the fragment even at two variables. Sub-tagged `B1-3field` vs `B1-coeff`.
- **B2 — value-level nonlinear.** variable×variable or variable÷variable the proof must reason inside (`Cost/Qty`, `Price·Qty` not cancelled by an identical guard term).
- **B3 — non-numeric.** string/choice/enum equality and disequality, boolean-shape, pattern (`~`, `!~`, `endsWith`), congruence (`% k == 0`).
- **B4 — other / unclear.** noted inline.

The **preservation cross-tab** is the load-bearing step: for each invariant, substitute the RHS of the assignments that write its fields into the invariant and ask whether the resulting obligation is still A1/A2 (possibly via atomization) or falls to B.

**Threat acknowledged up front (see Threats to Validity):** every count below carries classification judgment calls (chiefly: is a coefficient-scaled relation "still basically interval"? is a disjunction of presence facts A1 or B3?). Counts are reported to the precision the rubric supports, not to false exactness.

---

## Findings

### Per-bucket counts — `rule` invariants (149)

The `rule`s are the purest signal: they are explicit, author-declared global invariants — the relations the author most wants proved-preserved.

| Bucket | Count | % | Representative lines (verbatim from corpus) |
|---|---:|---:|---|
| **A1** interval | 28 | 19% | `TotalCreditHours <= 21`, `DaysPastDue <= 365`, `ItemCount <= 1000`, `AdultCount >= 1`, `Tests.count > 0`, `RequiredDeposit >= '200.00 USD'` |
| **A2** two-atom diff/order | 94 | 63% | `ApprovedAmount <= RequestedAmount`, `CheckOutDate > CheckInDate`, `LicensesInUse <= LicensesAllocated`, `ResponseDeadline - RequestDate <= '24 hours'`, `ToleranceUpper >= ToleranceLower` |
| **In-fragment subtotal** | **122** | **82%** | |
| **B1-3field** 3+-term linear | 6 | 4% | `RefundAmount + ForfeitAmount <= AmountPaid`, `PaymentsMade + PaymentsMissed <= PaymentsScheduled`, `LicensesAvailable + LicensesAllocated == TotalLicensesPurchased`, `ProducedQuantity + ScrapQuantity <= PlannedQuantity`, `PatronFeePaid <= LoanFee + LateFee + ReplacementCost`, `AdultCount + ChildCount <= 8` |
| **B1-coeff** non-unit-coeff 2-var | 7 | 5% | `ExistingDebt <= AnnualIncome * 3.0`, `ApprovedAmount * 2 <= ClaimAmount`, `SecurityDeposit <= MonthlyPayment * 3`, `RequiredDeposit >= QuotedTotal * 0.25`, `ProposedPremium <= CurrentPremium * 3`, `InviteCount <= SeatTarget * 3`, `TotalScheduledValue >= EquipmentValueAtCommencement * 0.9` |
| **B2** value-nonlinear | 1 | 1% | `AmountPaid <= DuesAmount * (RenewalCount + 1)` (field×field-shaped) |
| **B3** non-numeric | 13 | 9% | `RiskLevel == "High"`, `BillingFrequency == "Lifetime"`, `BaseCurrency != QuoteCurrency`, `ScheduledMinute % 15 == 0`, `not EmissionsRequired`, `CanDelete == false`, `Active == false or Discontinued == false` |
| **Out-of-fragment subtotal** | **27** | **18%** | |

**The single most important boundary observation:** the temporal/numeric difference-bounds (`X − Y ≤ c`, 8 occurrences across `medical-prior-auth`, `prior-auth-appeal`) read at a glance like B1 three-term relations because they contain a `+`/`−`. They are **not** — they are the canonical A2 difference-bound shape and are decided directly by DBM. Correctly attributing them to A2 (not B1) is what moves the in-fragment fraction from "marginal" to "acceptable." The `medical-prior-auth` SLA family is in-fragment:

```
rule ResponseDeadline - RequestDate <= '24 hours'  when UrgencyLevel == "Critical"
rule ResponseDeadline - RequestDate <= '72 hours'  when UrgencyLevel == "Urgent"
rule ResponseDeadline - RequestDate <= '7 days'    when UrgencyLevel == "Standard"
rule ResponseDeadline - RequestDate <= '14 days'   when UrgencyLevel == "Routine"
```

(The `when` guard is a choice-equality — B3-class — but a guard restricting *when* the invariant applies does not change the invariant's own fragment class; it gates which DBM the relation joins. The relation proved is A2.)

### Per-bucket counts — `ensure` invariants (565)

State/event ensures are dominated by **presence** checks (the entity/event must carry a field) and by **interval** checks on event args.

| Bucket | Count | % |
|---|---:|---:|
| **A1 — presence** (`X is set`) | 368 | 65% |
| **A1 — interval** (`X > '0 USD'`, arg `>= 0`, `X <= 100`) | 106 | 19% |
| **A2** two-atom diff/order (incl. `X − Y` temporal, `X <= now()`) | 41 | 7% |
| **In-fragment subtotal** | **515** | **~91%** |
| **B1 / B1-coeff** | 2 | <1% |
| **B2** value-nonlinear | 1 | <1% |
| **B3** non-numeric (choice/string/bool/`%`) + boolean-shape compounds | ~46 | ~8% |

Notes on the ensure tail:
- The only genuine **B2** ensure in the whole corpus is `inventory-item`'s `in Listed ensure ListPrice / StockingUnitsPerSaleUnit >= AverageCost` — a division relation. (Its divisor `StockingUnitsPerSaleUnit` is declared `positive`, so it is *fault-safe*; the relation itself is still value-nonlinear.)
- The compound ensures (`VisualInspectionPassed and DimensionalCheckPassed and ChemistryCheckPassed`, `DepositPaid and LeaseSigned`, `EmissionsRequired or SafetyInspectionRequired`, `ApprovedBy... is set or ... is set or OverrideReason is set`) are conjunctions/disjunctions of **boolean/presence** atoms — B3-class, but trivially so: they need no numeric preservation reasoning, only that the named booleans hold. Conjunction is native to a DBM (a conjunction of A1/A2 atoms stays in-fragment); disjunction over presence facts is a routing concern, not a numeric-fragment concern.

### Per-bucket counts — guard clauses (509)

Guards were sampled and pattern-classified rather than fully enumerated (they are the substitution *context*, not themselves invariants). Findings:
- **80 / 509** guard clauses are pure `X is set` presence; **101** contain an `and` conjunction. The overwhelming shape is **a conjunction of A1/A2 atoms** (presence + field-vs-arg / field-vs-field comparisons), which is exactly the DBM's native form. Example (`loan-application`, the 5-way approval guard): `DocumentsVerified and CreditScore >= 680 and AnnualIncome >= ExistingDebt * 2.0 and RequestedAmount < AnnualIncome / 2.0 and Approve.Amount <= RequestedAmount` — four of five conjuncts are A1/A2; two (`ExistingDebt * 2.0`, `AnnualIncome / 2.0`) carry a non-unit coefficient / division and are the out-of-fragment conjuncts.
- Genuine nonlinearity in guards is rare and concentrated: `QuantityOnHand >= FulfillOrder.Qty * StockingUnitsPerSaleUnit` (`inventory-item`), `Book.Subtotal != NightlyRate * Book.NightsCount`, `CompletedMilestones + 1 >= (TotalMilestones * SuccessThreshold) / 100`, `LodgingTotal / TripDays <= '350.00 USD'`.

### The atomization case (the in-fragment rescue) — `inventory-item`

`inventory-item.precept` is the corpus's hardest preservation case and the clearest demonstration of why atomization matters. The `FulfillOrder` path:

```
from Listed on FulfillOrder when QuantityOnHand >= FulfillOrder.Qty * StockingUnitsPerSaleUnit
    -> set QuantityOnHand = QuantityOnHand - FulfillOrder.Qty * StockingUnitsPerSaleUnit
```

The field `QuantityOnHand` is `nonnegative` (an A1 lower-bound invariant). The assignment subtracts a **nonlinear subterm** `FulfillOrder.Qty * StockingUnitsPerSaleUnit`. The preservation obligation is `QuantityOnHand − (Qty · UnitsPerSaleUnit) ≥ 0`. The *same* nonlinear subterm appears in the guard `QuantityOnHand >= Qty · UnitsPerSaleUnit`. **Atomizing** `Qty · UnitsPerSaleUnit` as one opaque variable `W` collapses both to `QuantityOnHand ≥ W` (guard) ⟹ `QuantityOnHand − W ≥ 0` (obligation) — a pure A2 difference-bound. **In-fragment via atomization.** Without atomization the engine must look inside the product and would fall to B2. This single mechanism is what keeps the corpus's signature inventory/decrement pattern provable.

### The preservation cross-tab — invariant vs. the assignments that write it

This is the OD-2 crux: an invariant being A1/A2 is necessary but not sufficient — the *assignments* writing its fields must keep the substituted obligation in-fragment too.

| Disposition | Meaning | Count (of the 122 in-fragment `rule`s, est.) | Examples |
|---|---|---:|---|
| **provable-in-fragment** | invariant is A1/A2 AND every writing assignment keeps preservation A1/A2 (possibly via atomization) | the large majority | `ApprovedAmount <= RequestedAmount` (written by `set ApprovedAmount = Approve.Amount` under guard `Approve.Amount <= RequestedAmount`); `QuantityOnHand nonnegative` (atomized, above); all the date-ordering rules (timestamps written by `set …At = now()` entry hooks) |
| **invariant simple, preservation out** | invariant A1/A2 but a writing assignment is a 3-term/nonlinear formula | small | A `nonnegative` accumulator written by a multi-term `set` (e.g. `inventory-item`'s `set AverageCost = (TotalInventoryCost + Rate*(Cost*(Qty*Units))) / (QuantityOnHand + Qty*Units)` — division-of-sums; the bound on `AverageCost` is A1, but proving the quotient nonnegative needs value-level reasoning) |
| **out** | invariant itself is B | the 27 out-of-fragment `rule`s | the 3-field balances and coefficient rules below |

The four **3-field balance invariants** are where the boundary actually bites, and they merit individual preservation analysis because all four are **genuine constrained invariants** maintained by `set` assignments (not derived `<-` fields):

- **`saas-license-management`** — `rule LicensesAvailable + LicensesAllocated == TotalLicensesPurchased`. Maintained by *paired* assignments: `set LicensesAllocated = LicensesAllocated + 1` together with `set LicensesAvailable = LicensesAvailable - 1`. The +1 and −1 cancel, so the invariant *is* preserved — but proving it requires reasoning that two assignments to two different fields offset inside a **three-variable equality**. That is squarely B1 (3 atoms); no atomization helps (the atoms are distinct fields). **Out-of-fragment, genuinely.**
- **`equipment-lease-agreement`** — `rule PaymentsMade + PaymentsMissed <= PaymentsScheduled`, with `set PaymentsMissed = PaymentsMissed - 1` paired with `set PaymentsMade = PaymentsMade + 1` (a missed payment later made good). Same 3-variable structure. **Out.**
- **`production-order-tracking`** — `rule ProducedQuantity + ScrapQuantity <= PlannedQuantity`, with `set ScrapQuantity = PlannedQuantity - ProducedQuantity` and incremental `set ProducedQuantity = ProducedQuantity + Batch.Quantity`. **Out.**
- **`event-venue-booking`** — `rule RefundAmount + ForfeitAmount <= AmountPaid`, with `set RefundAmount = CancelWithPartialRefund.Refund` / `set ForfeitAmount = CancelWithPartialRefund.Forfeit`. Preservation reduces to `Refund.arg + Forfeit.arg <= AmountPaid` — a 3-term obligation on event args. **Out.**

**The crux distinction (derived ≠ constrained).** The corpus is *full* of 3+-term linear arithmetic — but almost all of it lives in **derived fields**, which do not generate preservation obligations because they are recomputed, never constrained:

```
field GrossProfit       <- TotalRevenue - TotalReturns - TotalCostOfGoods - TotalShrinkage   # 4-term, B1, DERIVED
field TotalFeesDue       <- BaseRegistrationFee + WeightSurcharge + EmissionsFee + PlatesFee + LateFee + BackedUpFees  # 6-term, DERIVED
field NetAmountDue        <- TotalInvoiceAmount - CreditsApplied - AmountPaid                  # 3-term, DERIVED
field CombinedRateAdditive <- StateRate + LocalRate + SurchargeRate                            # 3-term, DERIVED
field RequestedTotal      <- LodgingTotal + MealsTotal + MileageTotal                          # 3-term, DERIVED
field OverallEquipmentEffectiveness <- AvailabilityPercent * PerformancePercent * QualityPercent / 10000  # nonlinear, DERIVED
```

Of the 32 derived `<-` fields, the majority are 3+-term linear or nonlinear. **None of them needs a preservation proof** — a derived field is *defined* by its formula; the engine keeps it consistent by re-evaluation (the derived-field DAG is finite → exact), not by proving an assignment preserves a constraint. A 4-term `GrossProfit <-` formula is B1 *as an expression*, but it is computed, not constrained, so the boundary does not bite it. **This is the single fact that decides OD-2:** the heavy linear arithmetic the corpus contains is overwhelmingly on the derived (compute) side, not the invariant (constrain) side.

### Per-sample summary (where the out-of-fragment invariants concentrate)

| Sample | Out-of-fragment invariant(s) | Class |
|---|---|---|
| `saas-license-management` | `LicensesAvailable + LicensesAllocated == TotalLicensesPurchased` | B1-3field (balance) |
| `equipment-lease-agreement` | `PaymentsMade + PaymentsMissed <= PaymentsScheduled`; `SecurityDeposit <= MonthlyPayment * 3`; `TotalScheduledValue >= EquipmentValueAtCommencement * 0.9` | B1-3field; B1-coeff |
| `production-order-tracking` | `ProducedQuantity + ScrapQuantity <= PlannedQuantity` | B1-3field |
| `event-venue-booking` | `RefundAmount + ForfeitAmount <= AmountPaid`; `RequiredDeposit >= QuotedTotal * 0.25` | B1-3field; B1-coeff |
| `hotel-reservation-management` | `AdultCount + ChildCount <= 8` | B1-3field (2-field sum vs const) |
| `library-inter-library-loan` | `PatronFeePaid <= LoanFee + LateFee + ReplacementCost` | B1 (4-term) |
| `loan-application` | `ExistingDebt <= AnnualIncome * 3.0` | B1-coeff |
| `insurance-claim` | `ApprovedAmount * 2 <= ClaimAmount` | B1-coeff |
| `insurance-renewal-processing` | `ProposedPremium <= CurrentPremium * 3` | B1-coeff |
| `team-invite-campaign` | `InviteCount <= SeatTarget * 3` | B1-coeff |
| `non-profit-membership-renewal` | `AmountPaid <= DuesAmount * (RenewalCount + 1)`; `MembershipLevel == "Lifetime"` family | B2; B3 |
| `inventory-item` | `ListPrice / StockingUnitsPerSaleUnit >= AverageCost` (ensure) | B2 |
| `saas-customer-success`, `customer-profile`, `product-catalog`, `saas-user-provisioning`, `currency-exchange-rates`, `unit-of-measure-reference`, `clinic-appointment-scheduling`, `vehicle-registration-renewal` | choice/string `==`/`!=`, boolean-shape, `% 15 == 0` congruence | B3 |

Every B1/B2 out-of-fragment invariant is concentrated in ~10 of 77 samples; the B3 cases are non-numeric by nature.

---

## Threats to Validity

- **Classification judgment calls.** The A2-vs-B1 line for the temporal difference-bounds (`X − Y ≤ c`) is load-bearing: classifying those 8 + 41 (rules + ensures) as B1 instead of A2 would drop the in-fragment fraction materially. The rubric is explicit that unit-coefficient `X − Y ≤ c` is A2 (DBM-native); this matches the spec's §7 "octagon-style relations Precept already has." If the implemented engine does *not* in fact decide `X − Y ≤ c` directly, these reclassify and the verdict weakens from "acceptable" toward "marginal." **This is the one assumption a reviewer should confirm against `ProofEngine.*.cs` before locking OD-2.**
- **Coefficient cases (B1-coeff).** `X ≤ c·Y` (7 rules) is classified out-of-fragment because a difference-bound requires unit coefficients. An octagon/UTVPI extension that permitted a *constant* scalar on one variable would pull all 7 back in-fragment cheaply (they are still two-variable). They are reported separately precisely so OD-2 can decide them as a unit.
- **Syntactic ambiguity.** Counts come from grep + a Python classifier over stripped relation text (quoted unit literals like `'0 {Cur}/{Unit}'` collapsed to a single token; `because` clauses and `when` tails stripped). A `/` inside a typed-zero literal is not a division; the classifier was hardened against that false positive after an initial overcount, but a residual ±2–3 miscount per bucket is plausible. The verdict is robust to that noise (it does not hinge on single-digit shifts).
- **Preservation cross-tab is partial.** The full substitution was traced by hand only for the four 3-field balances and the inventory atomization case (the cases that determine the verdict). The "provable-in-fragment majority" is an informed estimate, not a line-by-line substitution of all 1806 assignments. The estimate is conservative: the dominant assignment shapes are `set X = arg`, `set X = X ± arg`, and `set …At = now()`, all of which keep an A1/A2 invariant in-fragment.
- **Corpus representativeness.** These are *authored demonstration* samples, curated to exercise the type system; they may over-represent clean integrity rules and under-represent the messy multi-field accounting a production tenant would write. The 3-field balance count (4) could be higher in the wild. This cuts toward treating the balance identity as the *named* extension candidate rather than dismissing it.

---

## Implications for Precept

1. **The difference-bound fragment is acceptable for the integrity-rule surface.** 82% of explicit invariants and ~91% of ensures are in-fragment as written. The fragment is not a toy: it covers presence, all interval bounds, every date/temporal ordering, field-vs-arg and field-vs-field comparisons, and (via atomization) the signature guarded-decrement pattern.
2. **The named bounded-extension candidate is the 3-field balance identity** `A + B ≤/== C`. It is the *only* recurring out-of-fragment **numeric invariant** (4 precepts), it is a genuine constrained invariant (not derived), and it is exactly the case the guarantee spec's §7 names as the first step out of the octagon. If OD-2 chooses to push the boundary, this is the cell to push on — and it is a *bounded* push (a fixed-arity 3-variable sum, not general linear arithmetic), keeping it plausibly solver-free.
3. **Non-unit-coefficient 2-variable relations (`X ≤ c·Y`, 7 rules) are a cheaper second candidate.** They remain two-variable; a UTVPI/octagon variant admitting a constant scalar would absorb them without entering 3-variable territory. Worth pricing alongside the balance case.
4. **The B3 (choice/string/boolean) rules are not a fragment failure.** They are non-numeric relations the difference-bound fragment was never meant to decide; they are handled by equality/membership reasoning elsewhere (or are presence-routing). They should not count against the numeric fragment's adequacy.
5. **Derived fields must be excluded from the "out-of-fragment invariant" tally, by definition.** The corpus's heaviest linear/nonlinear arithmetic (`GrossProfit`, `TotalFeesDue`, `NetAmountDue`, `OverallEquipmentEffectiveness`) is all in derived `<-` fields. These are computed, not constrained; they generate no preservation obligation. Counting them as "out-of-fragment" would badly overstate the boundary's bite. This distinction is the difference between a "too narrow" verdict and an "acceptable" one.

---

## Conclusions (four-leg)

**Rationale.** The empirical in-fragment fraction (82% of rules, ~91% of ensures) and the *qualitative* finding that the heavy arithmetic lives on the derived/compute side (not the constrained/invariant side) together show the difference-bound boundary covers the integrity-rule surface the product actually carries. The fragment was designed to the right shape, not a toy subset.

**Alternatives considered and rejected.**
- *Verdict "too narrow"* — rejected: it would require the out-of-fragment invariants to be common and integrity-critical; the corpus shows them rare (4 balance + 7 coefficient + 1 nonlinear numeric invariants across 77 files) and concentrated, with the bulk of apparent linear arithmetic being derived (no preservation obligation).
- *Verdict "marginal"* — rejected for the corpus as written, **conditional on** the engine actually deciding `X − Y ≤ c` directly (see Threats). If that assumption fails, the verdict moves to marginal; the assumption is the explicit gate.
- *Push the boundary to general linear arithmetic now* — rejected: it would cross into polyhedra/SMT (the §0.6 #3 no-solver line) to buy ~5% more invariants. The bounded 3-field extension captures most of that gain while staying plausibly solver-free.

**Precedent.** Grounded in the sibling research already cited from the guarantee spec: `liveness-completability-verification-survey.md` (DBM emptiness is polynomial / solver-free for the interval+difference-bound fragment; transitive 3-field chains / full linear arithmetic / congruence cross into SMT), `interval-vs-value-evaluation-prior-art-2026-06-05.md` (the non-relational interval limit; octagon recovers `±x±y≤c` but not general 3-var/non-unit-coefficient linear — "a named bounded gap"), and `bounds-only-constraint-enforcement-2026-06-05.md` (the `NumericInterval` abstraction is "structurally blind to … 3-field sums" — verified against the same 77-file corpus). This audit supplies the *demand-side* evidence those *capability-side* surveys lacked: that the named gap is rarely exercised by real invariants.

**Tradeoff accepted.** The verdict rests on a classification judgment (`X − Y ≤ c` is A2, not B1) and on a partial preservation cross-tab (full hand-trace only for the verdict-determining cases). A reviewer must confirm the engine decides difference-bounds directly before OD-2 is locked on this evidence, and accept that authored samples may under-represent production-grade multi-field accounting (the 4-balance count could rise in the wild — which is *why* the balance identity is named as the extension candidate rather than dismissed).

---

## Verdict (for OD-2)

- **In-fragment fraction of invariants:** ~82% of `rule`s (122/149), ~91% of `ensure`s (515/565). Combined explicit-invariant in-fragment fraction ≈ **89%**.
- **Dominant out-of-fragment shapes, ranked:** (1) **3-field balance identity** `A + B ≤/== C` — 4 precepts, genuine constrained invariants, **the named extension candidate**; (2) **non-unit-coefficient 2-variable** `X ≤ c·Y` — 7 rules, the cheaper second candidate; (3) **non-numeric** (choice/string/boolean/`%`) — out by nature, not a numeric-fragment failure; (4) **value-nonlinear** `X / Y` — 1–2 occurrences, rare.
- **Invariants vs derived:** the heaviest linear/nonlinear arithmetic in the corpus is in **derived `<-` fields** (compute, no preservation proof needed), not in constrained invariants. The boundary does **not** bite the derived side. This is the crux that makes the verdict "acceptable" rather than "too narrow."
- **Boundary verdict: ACCEPTABLE.** The difference-bound fragment covers the integrity-rule surface the corpus actually carries. If OD-2 chooses to extend, the **3-field balance identity** (fixed-arity, plausibly still solver-free) is the single named candidate; the **non-unit-coefficient 2-variable** case is a cheap adjacent win. Both are bounded extensions, not a slide into general linear arithmetic / SMT.
