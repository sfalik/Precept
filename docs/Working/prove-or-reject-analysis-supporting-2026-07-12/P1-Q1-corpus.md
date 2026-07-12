All corpus and canon evidence is gathered. Here is the analysis.

---

# Q1 — The Merely-Unprovable Cell Under Pure Prove-or-Reject: Corpus-Grounded Taxonomy and Verdicts

## 0. Premises: held fixed vs. challenged

**Held fixed (identity-level, with justification):**

1. **Legibility of proof** — proof reasoning must be explainable to the domain-expert author via structured, inspectable witnesses (`docs/philosophy.md:60` full inspectability; `precept-language-spec.md:225`'s *underlying commitment*, not its specific solver-exclusion line — see challenge C1). Identity-level because the primary author is a non-developer (`docs/philosophy.md:92`) and an unexplainable verdict is unusable to her.
2. **Prevention / structural impossibility** (`docs/philosophy.md:7,49`). The product's category claim.
3. **One-file knowledge boundary** — "All proof facts derive from the `.precept` definition. No external oracle" (`precept-language-spec.md:228`, proof philosophy #4). Identity-level because it is what makes any proof *meaningful to the author of that one file*; it also permanently bounds Class 4 below.
4. **Determinism** (`docs/philosophy.md:22`) — grounds the single-timestamp-per-operation recommendation in Class 7.
5. **Honesty about approximation** (`docs/philosophy.md:23-25`) — grounds the rounding findings (Class 5c) and the bound-is-false verdict (Class 6).

**Challenged current-spec decisions** (each with a "spec should say" — expanded in §3):

- **C1**: `precept-language-spec.md:225` (blanket SMT/Z3 exclusion as the power line)
- **C2**: `precept-language-spec.md` §0.6 relational scope — "single-pass and depth-bounded (no transitive chasing of a third field)" (§0.6 item 2/3 closing paragraph, quoted from spec lines ~258-263)
- **C3**: `docs/language/collection-types.md:860` — "deliberately held back: `map`, `filter`, `reduce`, `sum`…"
- **C4**: `precept-language-spec.md:266-272` (§0.7 "no result outside a declared bound") — which, on this analysis, largely **survives**, with one wording sharpening.

**Not held as authority:** Frank's paper (`docs/Working/frank-prove-or-reject-position-2026-07-11.md`), the thesis (`docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md`) — engaged as arguments below.

---

## 1. The decisive structural fact: what "merely unprovable" can even mean in this language

Before the taxonomy, one observation that reorganizes the whole question. Precept's execution model is loop-free, total, and finite-vocabulary (`precept-language-spec.md:158-174`). A single operation is a bounded chain of assignments over (mostly linear) arithmetic with finite conditionals. **Linear arithmetic over ordered fields is decidable, with certificates** — Fourier–Motzkin elimination and linear-combination (Farkas) witnesses are complete for it and every step is a human-readable inequality derivation. So for a single operation, genuine logical undecidability essentially never arises. In this language, "the right engine can neither prove nor refute containment" can only come from **three sources**:

- **(S1) Contingency** — both outcomes are genuinely reachable depending on runtime values. `Deposits − Withdrawals` really can be negative for some governed inputs and nonnegative for others. Nothing to prove; nothing to refute; a real unhandled case exists.
- **(S2) Missing inductive premise** — the fact that makes the write safe is true of every reachable state but is established only *across operation history* (an invariant the definition maintains but nowhere states, or cannot state).
- **(S3) Knowledge outside the file** — the fact lives in the world (the caller's ledger, an external job table), not in the definition. Unknowable by *any* engine honoring the one-file boundary (held-fixed premise 3).

This is the deep reason the overdraft intuition generalizes: **S1 is the dominant source, and an S1 case is by definition an unanswered domain question** — the definition reaches a value the domain may not accept and does not say what happens then. S2 splits into "expressible with more engine power" (exits the cell) and "expressible only with more *language*" (the one FORCES-WORSE pocket, Class 2). S3 is permanent but, as the corpus shows, its forced remediation is domain-honest.

**Corpus method (reproducible):** census of all derived writes via `grep -n "set [A-Za-z]* = .*[-+*/]" samples/*.precept` (267 hits across 77 files), all subtraction writes via `grep -n "set [A-Za-z]* = .*- "`, cross-referenced against each file's bounded field declarations (`grep "^field" | grep -E "min |max |nonnegative|positive"`) and guard/reject rows. Every claim below cites file:line.

---

## 2. The taxonomy and per-class verdicts

### Class 1 — Provable by the RIGHT engine: **NOT in the cell.** The honest finding is "current spec under-powers the engine."

This class is enormous in the corpus — and it is the central corpus finding: **the sample authors already write, voluntarily and idiomatically, exactly the structures a legible engine needs**, and the current spec does not commit to consuming them. Each sub-item names the required engine power, its legibility certificate, and corpus exemplars.

**1a. Row-order complement narrowing.** A fall-through row runs only when the rows above did not match; their negated guards are premises. Certificate: "this row executes only when `ExtensionCount < 3`, because the row above catches `>= 3`" — a sentence any author who understands row order already believes.

```precept
from ActiveTrial on ExtendTrial when ExtensionCount >= 3
    -> reject "Maximum of three extensions already applied…"
from ActiveTrial on ExtendTrial
    -> set ExtensionCount = ExtensionCount + 1     # provable ≤ 3 ONLY via complement
```
(`saas-trial-to-paid.precept:125-136`, field `max 3` at `:30`. Same shape: `global-meeting-scheduler.precept:197-203` (max 500); `crosswalk-signal.precept:60-66`; the health-score routing into `in Active ensure OverallHealthScore >= 50`, `saas-customer-success.precept:126-142,74-75`.)

**1b. Weakest-precondition substitution through set-chains.** Check a post-state rule by substituting the assignments into it; the certificate is the rewritten expression side-by-side with the guard that discharges it.

```precept
from ExpiringSoon on Renew when AmountPaid + Renew.Amount > DuesAmount * (RenewalCount + 2)
    -> reject "…"
from ExpiringSoon on Renew
    -> set AmountPaid = AmountPaid + Renew.Amount
    -> set RenewalCount = RenewalCount + 1
# rule AmountPaid <= DuesAmount * (RenewalCount + 1)   — post-state instance ≡ the complement guard, exactly
```
(`non-profit-membership-renewal.precept:70,180-186`. Also the transfer pattern `PaymentsMissed − 1` / `PaymentsMade + 1` preserving `rule PaymentsMade + PaymentsMissed <= PaymentsScheduled` — sum unchanged under transfer, `equipment-lease-agreement.precept:79,191-198`; and `event-venue-booking.precept:274` discharging `rule AmountPaid <= QuotedTotal` through the computed field `AmountPaid <- DepositPaid + FinalPayment`, `:53,96`.)

**1c. Declared relational rules as premises, including three-variable linear.** `bounds-only-constraint-enforcement-2026-06-05.md:112` marks `A + B <= C` as unrepresentable in the interval/octagon lane — but linear-combination reasoning with an explicit witness handles it legibly:

```precept
rule ProducedQuantity + ScrapQuantity <= PlannedQuantity because "…"   # production-order-tracking:66
-> set ScrapQuantity = PlannedQuantity - ProducedQuantity              # :234
# nonnegative because: Produced + Scrap ≤ Planned and Scrap ≥ 0 ⟹ Produced ≤ Planned. Two lines, citable.
```
Likewise `YieldPercent = ProducedQuantity * 100.0 / PlannedQuantity` into `rule YieldPercent <= 100` (`production-order-tracking.precept:33-35,144`) via the ratio lemma `0 ≤ x ≤ y, y > 0 ⟹ 100·x/y ≤ 100` — a *named lemma with instantiated operands* is a legible witness.

**1d. `min`/`max`/clamp lemmas** — `min(a,b) ≤ b` (`insurance-claim.precept:127`); clamping (`min(expr, cap)`) as the author's explicit saturation choice.

**1e. Event-scoped ensures as argument premises** — `on Approve ensure Approve.Amount <= ClaimAmount` (`insurance-claim.precept:86`) is governed at ingress and must flow as a premise into `set ApprovedAmount = … else Approve.Amount` proving `rule ApprovedAmount <= ClaimAmount` (`:47,127`). This is the composition seam (`docs/philosophy.md:55`) applied to derived writes — the derived value's *inputs* carry ingress-made-true constraints.

**1f. State-flow fact propagation** — `on Submit ensure ClaimAmount > '0.00 USD'` (`insurance-claim.precept:79`) plus `ClaimAmount` modifiable only in Draft (`:65`) means `ClaimAmount > 0` holds at every Approve. The certificate is a path argument ("every path into UnderReview passes Submit's ensure; the field is frozen after Draft") — inspectable dataflow, not an oracle.

**1g. Rounding-aware exact decimal reasoning** — see Class 5c, where it *refutes* rather than proves.

**Verdict on Class 1: these cases must compile clean.** None of them is an argument about the posture; every one is a spec-power obligation. The current spec's relational mechanism is explicitly "single-pass and depth-bounded (no transitive chasing)" and commits to none of 1a/1b/1e/1f — that is challenge C2. A prove-or-reject posture shipped *without* Class-1 powers would reject half the corpus's best idioms and would deserve the thesis's over-rejection critique; with them, the corpus's guarded rows all discharge.

---

### Class 2 — TRUE CELL today: internal truth, aggregate-linking invariant the *language cannot state*. **The only FORCES-WORSE pocket — and it converts to "spec should be more powerful."**

The shape: a scalar accumulator mirrors an aggregate of a collection the entity itself holds; a decrement reads the collection back and subtracts.

```precept
# shopping-cart.precept:48,53,125,154,168 — ItemCount nonnegative, rule ItemCount <= 1000
from Cart on RemoveItem when LineItems contains RemoveItem.ItemId
    -> set ItemCount = ItemCount - (ItemQuantities for RemoveItem.ItemId)   # ≥ 0 only because
    -> remove ItemQuantities RemoveItem.ItemId                              # ItemCount == sum(ItemQuantities)
```
Same shape: `bill-of-materials-management.precept:35,55,114,123` (`TotalComponentUnits` nonnegative, plus `rule TotalComponentUnits >= ComponentPartNumbers.count`); `event-venue-booking.precept:214` dodges only because `AddOnTotal` (`:48`) carries no bound.

**Is it truly in the cell under the right engine?** The invariant `ItemCount == sum of ItemQuantities values` is (i) true in every reachable state, (ii) the *only* fact that proves the write, (iii) **inexpressible**, because `collection-types.md:860` deliberately holds back `sum`. No amount of engine power can use a premise the language cannot state. So yes — truly in the cell *under the current language*.

**The forced remediation under pure prove-or-reject, exactly:** one of —
- `when (ItemQuantities for RemoveItem.ItemId) <= ItemCount` + an unreachable reject row — a **dead-guard fiction**: the definition now asserts, in permanent legible source, that "removed quantity exceeding the item count" is a live scenario, and carries a refusal message no input can ever trigger. This is anti-legible: it corrupts the definition-as-documentation.
- Drop `nonnegative` from `ItemCount` — abandon a true domain constraint. Worse.
- Hand-maintained special cases (the cart already does this: the `ItemQuantities.count == 1` row hard-sets `ItemCount = 0`, `shopping-cart.precept:144-150`) — denormalization workarounds multiplying.

**Verdict: FORCES-WORSE — but the honest finding is C3, not GOVERN.** The right engine *plus the right language* proves this legibly: admit scalar `sum(Collection)` in rule/computed-field position and check declared aggregate-equality rules by **inductive preservation with delta certificates** — "this operation replaces the entry (old value v) with Quantity and adjusts ItemCount by Quantity − v; the sum changes by Quantity − v; equality preserved." That is a ledger argument a bookkeeper reads, with Event-B invariant-preservation obligations (Abrial, *Modeling in Event-B*, 2010) as precedent — discharged automatically here because the mutation vocabulary is closed and each action's aggregate delta is known (the spec already does exactly this for `count` intervals: §0.6 implementation notes, "advanced by each mutation's sound per-kind/per-action delta"). Sum-of-scalar-elements does not need the "structured collection element types" that `collection-types.md:860` cites as the hold-back reason; the corpus is *already computing aggregates by hand, less safely* — the hold-back protects a purity line ("predicate not computation") at the cost of forcing exactly the unsound manual accounting a domain-integrity engine exists to prevent. **Recommendation: reopen the `sum` hold-back through the owner-consultation gate + `/design`; the best-supported fix is a computed field `ItemCount <- sum(ItemQuantities)`, which deletes the denormalized counter entirely.** Until that ships, this class is the real, bounded cost of the pure posture: 2 corpus files (cart, BOM), ~5 write-sites.

---

### Class 3 — In the cell only by denormalization; the current language already has the better fix. **FORCES-BETTER.**

The count-mirror shape: a stored counter shadowing `Collection.count`, decremented under a membership guard.

```precept
# global-meeting-scheduler.precept:102,215-219 — ParticipantCount nonnegative max 500
from Draft, Scheduled on RemoveParticipant       # membership reject-row above (:215)
    -> remove ParticipantEmails RemoveParticipant.Email
    -> set ParticipantCount = ParticipantCount - 1        # ≥ 0 only via ParticipantCount == ParticipantEmails.count
```
(Same: `library-inter-library-loan.precept:39,201-206`; `event-venue-booking.precept:63,215`.)

The linking invariant is unstated, so the write is unprovable — but unlike Class 2, the language can already express the fix: **make the counter computed** (`field ParticipantCount as integer <- ParticipantEmails.count` — the corpus uses `<-` at `event-venue-booking.precept:53-54`) or declare the count-equality rule (count-comparison rules exist: `lab-test-order-results.precept:31`) and let Class-1c/1h powers close it. Rejection here flushes out a denormalized counter — the classic source of drift bugs, squarely Precept's mandate. **The forced remediation deletes state rather than inventing fiction. FORCES-BETTER.**

### Class 4 — TRUE CELL permanently: the truth lives outside the file. **FORCES-BETTER, and the corpus authors already agree.**

The projection-entity shape: the entity summarizes external state; a mutation trusts a caller-supplied delta.

```precept
# production-schedule-management.precept:27,102-116 — TotalScheduledHours nonnegative
from Draft on RemoveJob when RemoveJob.Hours > TotalScheduledHours
    -> reject "Cannot remove {RemoveJob.Hours} — only {TotalScheduledHours} currently scheduled"
from Draft on RemoveJob …
    -> set TotalScheduledHours = TotalScheduledHours - RemoveJob.Hours
```
No engine honoring the one-file boundary (held-fixed premise 3) can prove `RemoveJob.Hours` matches the hours once added — the invariant lives in the world. Truly, permanently in the cell. But look at the forced remediation: **the guard is already there, written voluntarily**, and it is *not* fiction — the caller genuinely can send a wrong delta, the refusal is reachable, and it is addressed to the party who can act (the caller). The deeper fix the cell pressure produces is even better: keep the truth *inside* the entity — which is precisely what `event-venue-booking.precept:208-210` documents in its own comment: *"reads the fee back from AddOnFees so AddOnTotal can be decremented without trusting an event-arg."* The corpus names arg-trusting as the anti-pattern unprompted. Note also `production-schedule-management.precept`'s residual hole: the non-high-priority RemoveJob row cannot preserve `rule HighPriorityJobCount <= JobCount` (`:46`) when every remaining job is high-priority — a *reachable* violation a prove-or-reject compiler would force into an explicit reject row. **FORCES-BETTER.**

### Class 5 — TRUE CELL (contingency, S1): the genuinely-open domain edge. **FORCES-BETTER — the exemplar class, with three corpus-caught latent bugs.**

**(5a) Overdraft/underflow.** Frank's exemplar (`frank-prove-or-reject-position:50-53,121-135`), and the corpus has real unguarded instances *inconsistent with the same file's own idiom*:

```precept
# inventory-item.precept — FulfillOrder IS guarded (:155), RecordShrinkage is NOT:
from Listed on RecordShrinkage
    -> set QuantityOnHand = QuantityOnHand - RecordShrinkage.Qty     # :182 — nonnegative (:49), unguarded
```
Forced remediation: the guard+reject pair the file already uses two rows up ("what happens when shrinkage exceeds stock?" — a real warehouse question). **Latent bug #1.**

**(5b) The false-invariant subtraction.** `TotalCostOfGoods -= ReturnOrder.Qty × SUPS × AverageCost` (`inventory-item.precept:167`, nonnegative at `:67`): the head-invariant "returns never exceed recorded COGS" is *actually false* — `AverageCost` is a moving weighted average, so a return valued at today's WAC can exceed the COGS recorded at sale time. Rejection forces the author to confront a genuine accounting flaw. **Latent bug #2.**

**(5c) The rounding penny.** `rule ApprovedAmount * 2 <= ClaimAmount when FraudFlag` with `set ApprovedAmount = … min(Approve.Amount, ClaimAmount / 2) …` (`insurance-claim.precept:49,127`): if money division rounds to representable cents, an odd-cent `ClaimAmount` makes `(ClaimAmount/2)·2` exceed `ClaimAmount` by one cent — a rounding-aware, place-value-legible engine cannot prove containment *because it is contingently false*. Forced remediation: restate the rule as `ApprovedAmount <= ClaimAmount / 2` (congruent with the computed expression — then provable) or fix the rounding direction. **Latent penny bug #3** (conditional on the rounding model — the D1-adjacent numeric decision the owner has not made; flagging, not asserting).

**(5d) Cap overflow on accumulate/multiply.** `shopping-cart.precept` AddItem rows (`:124-136`) carry **no** `<= 1000` guard against `rule ItemCount <= 1000` (`:53`) — "what happens at the cart cap?" is unanswered. And `samples/Test.precept:5,10` — `set TotalCost = AvgCost * Quantity` into `max '1000 USD'` with both sources unbounded/optional — is the pure S1 case: the forced remediation is the mirror guard + explicit else (`when AvgCost * Quantity <= '1000 USD' -> set … ; -> reject/clamp …`), which a congruence-capable engine (1i) *always accepts*. Note what this means structurally: **with guard congruence, the maximally-forced remediation in this class is never bound-invention — it is "make the unhandled case explicit,"** and that is a domain decision, not prover ceremony.

**Verdict: FORCES-BETTER across the class**, corroborated by the corpus itself: the guard/reject-row idiom is already the overwhelmingly dominant pattern (trial-to-paid, meeting-scheduler, nonprofit, vehicle-registration, lease, venue, crosswalk, prod-sched, inventory-FulfillOrder), and the exceptions read as sample bugs prove-or-reject would have caught.

### Class 6 — The bound-is-false class. **FORCES-BETTER (honesty), and the 214-figure re-read.**

Where a signed quantity would be wrongly bounded, rejection forces removing the false bound. The corpus already behaves this way: `NetRecovery` (`insurance-subrogation.precept:31` — settlement minus legal costs, legitimately negative), `PremiumChange` (`insurance-renewal-processing.precept:141`), `PremiumAdjustment` (`insurance-policy-endorsement.precept:139`) are all **unbounded**, by evident authorial intent. This is the correct re-reading of the **214 unbounded vs 10 bounded money fields** (`research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md:120-122`): that figure quantifies the over-rejection cascade of a *different* posture — reject-unbounded-**source** to feed relational narrowing (the doc's own framing: "every money field that appears as a relational-rule **RHS**… would need a new max"). The Q1 cell's obligation attaches only where the *target* carries a declared band — and 214 unbounded targets generate **zero** containment obligations. The corpus shows authors do not write bounds they don't mean; where they do write one (`max 3` extensions, `max 500` participants, `max 100` seats, `<= 1000` cart items), it is real policy and the corpus pairs it with cap-handling rows. **The bound-invention cascade does not materialize in this cell**; the guard-mirror escape (5d) exists at every site the cascade could have hit.

### Class 7 — Corpus-empty hypotheticals. **NEUTRAL.**

- **Derived temporal into temporal bounds**: zero corpus instances (`GracePeriodEnd`, `TargetCompletion` are optional and unconstrained relative to other dates — `insurance-renewal-processing.precept:32`, `insurance-claim-adjudication.precept:63`). Same-operation `now()`-relations should be made provable by speccing **one evaluation timestamp per operation** (a determinism corollary worth writing down); cross-operation clock relations stay in the cell, remediation = a guard on the comparison — a reachable, meaningful condition. NEUTRAL.
- **Derived strings into `maxlength`**: zero corpus instances (`DenialReason` is unbounded, `patient-enrollment.precept:50`); remediation would be a `maxlength` on the source event-arg — an ordinary ingress contract. NEUTRAL.

---

## 3. What the spec SHOULD say (the challenged decisions, resolved)

**C1 — replace spec:225's solver-name line with a certificate criterion.** The identity commitment is legibility, not any particular tool exclusion. The right line: *a decision procedure is admissible iff every verdict it returns carries a structured, domain-legible witness (the premises used, the derivation steps, the instantiated lemma); certificate-free oracle verdicts are inadmissible regardless of power.* This admits Fourier–Motzkin/Farkas linear certificates, min/max lemmas, place-value decimal and rounding derivations, count/sum delta ledgers, path arguments — everything Class 1 needs — while still excluding "Z3 said UNSAT." (Precedent: certifying algorithms, McConnell et al., *Computer Science Review* 2011; proof-carrying SMT cores are themselves certificates, which shows the line is witness-vs-oracle, not tool-vs-tool.)

**C2 — the relational engine must grow to the corpus's idioms**: row-complement narrowing (1a), WP substitution through set-chains (1b), declared-rule premises incl. three-variable linear (1c), event-ensure premises (1e), state-flow fact propagation (1f), guard-expression congruence (1i). The current "single-pass, depth-bounded, no transitive chasing" scope leaves the corpus's own guarded rows unprovable — under prove-or-reject that under-power *is* the over-rejection problem, and it is fixable without any posture change.

**C3 — reopen the `sum` hold-back** (`collection-types.md:860`) for scalar aggregates in rule/computed-field position, with inductive preservation checking (delta certificates). This is the single change that empties the only FORCES-WORSE class (Class 2). Language-surface change → owner conversation + `/design` per the gate; this analysis only establishes the need.

**C4 — §0.7's "no result outside a declared bound" (spec:266-272) can stand** for this cell — *contra* the thesis's Recommendation A — **conditional on C1–C3 shipping**, with Frank's own conceded sharpening folded in: "derived values are proven against every **decidable** declared constraint" (`frank-prove-or-reject-position:141`). Without C1–C3, prove-or-reject over-rejects the corpus's best idioms and the thesis's critique lands.

---

## 4. Bottom line

**The cell, under the right engine, is small and almost entirely FORCES-BETTER.** Enumerated against all 267 derived writes in the 77-file corpus: every guarded accumulator, transfer, ratio, routing, and cap idiom is provable by a legible engine (Class 1 — the dominant population, currently mis-filed as cell members only because the spec under-powers the engine). What genuinely remains is: contingent domain edges where rejection surfaces an unanswered question or a latent bug (Class 5 — three real bugs found in polished samples: unguarded shrinkage, WAC-drift returns, the fraud-cap penny case); external-truth projections where the forced guard is honest and the corpus already writes it (Class 4); denormalized counters where rejection forces strictly better modeling (Class 3); and false bounds rejection forces the author to delete (Class 6 — which is also the correct reading of the 214-unbounded-money figure: it measures a different posture's cascade; in this cell, unbounded targets carry no obligation and the guard-mirror escape means bound-invention is never actually forced).

**Exactly one FORCES-WORSE class survives scrutiny — the aggregate-linking invariants (Class 2: `shopping-cart`, `bill-of-materials`) — and its honest resolution is "the spec should be more powerful" (admit `sum` + inductive preservation, challenge C3), not "reject forces worse, therefore govern."** Under pure prove-or-reject with today's language, those ~5 write-sites are forced into dead-guard fiction, which is a real anti-legibility cost and should be named as the posture's price if C3 is refused. One honesty note against Frank's strongest rhetorical leg: for accumulator-plus-external-delta shapes (the cart cap, payment accumulators), a runtime refusal is *not* "addressed to no one" — the caller's argument caused the breach and the caller can act; his argument is exact only for purely-internal derivations. It does not change the verdicts — the prove-or-reject remediation (an explicit reject row with a domain-authored message) is better authoring than a generic `ConstraintsFailed` in every corpus instance examined — but the boundary ruling should not rest on the overstated form.