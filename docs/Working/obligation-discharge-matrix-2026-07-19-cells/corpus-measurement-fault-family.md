# Corpus measurement — fault-family obligations vs licensed derivations

**Status**: Draft — 2026-07-21. Companion to `corpus-measurement-family-1.md`, which measures the rule × write-site family. Regenerate the tables below with `dotnet run --project tools/Precept.MatrixTools -- measure-corpus-faults samples/ <out.json> <out.md>`.

## What this measurement is

It compiles every `.precept` file under `samples/` through the real pipeline and reads the proof ledger's obligations — the safety preconditions the shipped engine actually minted, with the disposition and strategy it recorded for each. It re-derives nothing; every number below is read off `Compilation.Proof.Obligations`.

## What this measurement can and cannot support

This carries forward the scoping the matrix states for the family-1 measurement (`docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol`, layer 4, owner direction 2026-07-20). It applies to the fault family unchanged, and one limit applies with more force here, not less.

Agreement with the corpus is **necessary, not sufficient**, and the corpus is **never citable as coverage**. Four limits, all load-bearing:

- **The corpus is filtered by a test asserting every sample compiles.** Programs that would demonstrate expressiveness loss could not be in it — they were rewritten until they passed, or never written. A measured "no cases of lost expressiveness" is the expected reading whether the true number is zero or large, and must never be reported as evidence of zero. This shows up directly below: 1,043 of 1,045 minted obligations are proved, and both unresolved ones are in `Test2.precept`, the one sample deliberately kept failing.
- **The interesting derivations are barely exercised.** Of the 30 divide-by-zero obligations in the whole corpus, 18 discharge by folding a literal divisor — no authored premise at all. Four discharge from a guard, two from a declared modifier, six through the compositional-constraint strategy. The entire corpus contains six examples of the premise class the definition spends most of its contract text on.
- **These are documentation samples authored by this project**, not field data from domain experts. They show which spellings *can* occur, never which ones authors reach for, and say nothing about prevalence.
- **The ledger records what HEAD minted, not what the definition owes.** A site the shipped engine does not mint at contributes nothing to these totals. Its absence is evidence about the engine's minting, never evidence that the definition owes no obligation there — and slice 3 measured ten evaluation positions where the engine mints nothing at all. Reading a zero here as "no obligation exists" would invert the finding.

What the corpus does yield is existence proofs and named gaps — both genuinely useful, and both weaker than coverage. The complementary evidence seam is what sample authors *annotated*, collected in `authored-expressiveness-gaps.md`. A ratification that leans on this measurement must cite both, and must state the limits above rather than the bucket counts alone.

## What the numbers show, in words

- Every requirement kind the engine mints in this corpus is discharged, except two interval-containment obligations in `Test2.precept`. There is no fault-family obligation anywhere in `samples/` that the engine mints and then fails to prove, other than that one deliberately-failing file.
- Three of the thirteen declared requirement kinds — `Dimension`, `CountContainment`, `KeyPresence` — are minted zero times. That is a statement about the corpus, not the catalog: all three are declared and reachable.
- The divide-by-zero obligation is minted in exactly three evaluation contexts: transition rows (23), constraints (4), and computed-field expressions (3). It appears in no guard, no interpolation hole, and no declaration position anywhere in the corpus — the same absence slice 3 measured directly and recorded as a build gap.
- The collection non-empty obligation discharges through a guard 17 times and through in-chain collection growth once. It never discharges through a declared `mincount` in this corpus, so the corpus supplies no evidence either way on the `mincount`-as-premise question the slice-3 boundary report routes to the owner.
- Six divide-by-zero obligations discharge through `CompositionalConstraint` — the strategy the verified false-proof defect runs through (`authored-expressiveness-gaps.md` Part 3). Those six are the corpus's only instances of a business rule discharging a fault obligation, and they are exactly the shape that defect makes unsound. They are counted as proved below because that is what the ledger says; they are not evidence the discharge is sound.

## Measured totals

- Files measured: **78**; minting at least one proof obligation: **78**; with error diagnostics: **1**
- Proof obligations minted across the corpus: **1045** (proved **1043**, unresolved **2**)

## Requirement kinds exercised

| Requirement kind | Minted | Unresolved |
|---|---|---|
| Numeric | 313 | 0 |
| LengthContainment | 298 | 0 |
| QualifierCompatibility | 235 | 0 |
| Presence | 91 | 0 |
| IntervalContainment | 74 | 2 |
| QualifierChain | 16 | 0 |
| DimensionalProduct | 8 | 0 |
| Modifier | 6 | 0 |
| IndexBounds | 3 | 0 |
| AssignmentQualifier | 1 | 0 |

## Requirement kinds the corpus never exercises

- `Dimension` — zero obligations minted across the corpus. This is a fact about the corpus, not about the definition.
- `CountContainment` — zero obligations minted across the corpus. This is a fact about the corpus, not about the definition.
- `KeyPresence` — zero obligations minted across the corpus. This is a fact about the corpus, not about the definition.

## Evaluation contexts the minted obligations sit in

| Obligation context | Count |
|---|---|
| TransitionRowContext | 354 |
| FieldDefaultContext | 264 |
| ConstraintContext | 218 |
| EventHandlerContext | 169 |
| FieldExpressionContext | 36 |
| StateHookContext | 3 |
| ArgDefaultContext | 1 |

## Discharge strategies HEAD used

| Strategy | Count |
|---|---|
| LengthContainment | 298 |
| Literal | 283 |
| QualifierCompatibility | 252 |
| GuardInPath | 115 |
| IntervalContainment | 72 |
| DeclarationAttribute | 8 |
| DimensionalProduct | 8 |
| CompositionalConstraint | 6 |
| (none recorded) | 2 |
| CollectionGrowth | 1 |

## Per-file rows

| File | Obligations | Proved | Unresolved | Error diagnostics |
|---|---|---|---|---|
| Test.precept | 6 | 6 | 0 | 0 |
| Test2.precept | 4 | 2 | 2 | 2 |
| academic-course-registration.precept | 8 | 8 | 0 | 0 |
| apartment-rental-application.precept | 10 | 10 | 0 | 0 |
| bill-of-materials-management.precept | 5 | 5 | 0 | 0 |
| building-access-badge-request.precept | 4 | 4 | 0 | 0 |
| calibration-management.precept | 9 | 9 | 0 | 0 |
| clinic-appointment-scheduling.precept | 6 | 6 | 0 | 0 |
| contractor-invoice-settlement.precept | 9 | 9 | 0 | 0 |
| crosswalk-signal.precept | 2 | 2 | 0 | 0 |
| currency-exchange-rates.precept | 2 | 2 | 0 | 0 |
| customer-profile.precept | 2 | 2 | 0 | 0 |
| equipment-downtime-tracking.precept | 9 | 9 | 0 | 0 |
| equipment-lease-agreement.precept | 24 | 24 | 0 | 0 |
| event-registration.precept | 8 | 8 | 0 | 0 |
| event-venue-booking.precept | 36 | 36 | 0 | 0 |
| fee-schedule.precept | 7 | 7 | 0 | 0 |
| final-product-inspection.precept | 10 | 10 | 0 | 0 |
| global-meeting-scheduler.precept | 10 | 10 | 0 | 0 |
| hiring-pipeline.precept | 3 | 3 | 0 | 0 |
| hotel-reservation-management.precept | 18 | 18 | 0 | 0 |
| incoming-material-inspection.precept | 8 | 8 | 0 | 0 |
| insurance-claim-adjudication.precept | 14 | 14 | 0 | 0 |
| insurance-claim.precept | 10 | 10 | 0 | 0 |
| insurance-policy-endorsement.precept | 7 | 7 | 0 | 0 |
| insurance-renewal-processing.precept | 13 | 13 | 0 | 0 |
| insurance-subrogation.precept | 23 | 23 | 0 | 0 |
| insurance-underwriting.precept | 20 | 20 | 0 | 0 |
| inventory-item.precept | 76 | 76 | 0 | 0 |
| invoice-line-item.precept | 9 | 9 | 0 | 0 |
| it-helpdesk-ticket.precept | 22 | 22 | 0 | 0 |
| lab-test-order-results.precept | 4 | 4 | 0 | 0 |
| library-book-checkout.precept | 7 | 7 | 0 | 0 |
| library-hold-request.precept | 11 | 11 | 0 | 0 |
| library-inter-library-loan.precept | 28 | 28 | 0 | 0 |
| loan-application.precept | 11 | 11 | 0 | 0 |
| maintenance-work-order.precept | 3 | 3 | 0 | 0 |
| manufacturing-quality-inspection.precept | 8 | 8 | 0 | 0 |
| medical-device-tracking.precept | 13 | 13 | 0 | 0 |
| medical-prior-auth.precept | 51 | 51 | 0 | 0 |
| non-profit-membership-renewal.precept | 19 | 19 | 0 | 0 |
| parcel-locker-pickup.precept | 6 | 6 | 0 | 0 |
| patient-care-plan-coordination.precept | 12 | 12 | 0 | 0 |
| patient-enrollment.precept | 15 | 15 | 0 | 0 |
| patient-referral-management.precept | 14 | 14 | 0 | 0 |
| payment-method.precept | 5 | 5 | 0 | 0 |
| prescription-refill-request.precept | 26 | 26 | 0 | 0 |
| prior-auth-appeal.precept | 18 | 18 | 0 | 0 |
| product-catalog.precept | 7 | 7 | 0 | 0 |
| production-order-tracking.precept | 16 | 16 | 0 | 0 |
| production-schedule-management.precept | 11 | 11 | 0 | 0 |
| production-shift-downtime.precept | 14 | 14 | 0 | 0 |
| refund-request.precept | 4 | 4 | 0 | 0 |
| restaurant-waitlist.precept | 5 | 5 | 0 | 0 |
| saas-customer-onboarding.precept | 17 | 17 | 0 | 0 |
| saas-customer-success.precept | 48 | 48 | 0 | 0 |
| saas-license-management.precept | 20 | 20 | 0 | 0 |
| saas-plan-upgrade-downgrade.precept | 15 | 15 | 0 | 0 |
| saas-subscription-billing.precept | 11 | 11 | 0 | 0 |
| saas-trial-to-paid.precept | 16 | 16 | 0 | 0 |
| saas-usage-metering-and-billing.precept | 22 | 22 | 0 | 0 |
| saas-user-provisioning.precept | 8 | 8 | 0 | 0 |
| shopping-cart.precept | 16 | 16 | 0 | 0 |
| statistical-process-control.precept | 14 | 14 | 0 | 0 |
| subscription-cancellation-retention.precept | 9 | 9 | 0 | 0 |
| supplier-corrective-action.precept | 5 | 5 | 0 | 0 |
| supplier-quality-management.precept | 37 | 37 | 0 | 0 |
| tax-rate-configuration.precept | 8 | 8 | 0 | 0 |
| team-invite-campaign.precept | 8 | 8 | 0 | 0 |
| trafficlight.precept | 3 | 3 | 0 | 0 |
| travel-reimbursement.precept | 6 | 6 | 0 | 0 |
| unit-of-measure-reference.precept | 1 | 1 | 0 | 0 |
| utility-outage-report.precept | 10 | 10 | 0 | 0 |
| utility-service-connection.precept | 22 | 22 | 0 | 0 |
| utilization-review-case.precept | 12 | 12 | 0 | 0 |
| vehicle-registration-renewal.precept | 31 | 31 | 0 | 0 |
| vehicle-service-appointment.precept | 3 | 3 | 0 | 0 |
| warranty-repair-request.precept | 1 | 1 | 0 | 0 |

