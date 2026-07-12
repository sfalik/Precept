All source reading done. Analysis follows as my return value.

---

# Q1 Deep Probe — Finance Domains: Does Pure Prove-or-Reject Force Better Precepts?

**Cell under study:** merely-unprovable, definition-derived value → non-proof-carrying declared bound. **Posture:** compile Error, no escape construct. **Framing:** design ceiling (solver-free), end-state compiler+runtime.

## 0. Design-ceiling calibration (governs every verdict below)

The spec self-describes the ceiling: "Standard interval arithmetic, bounded relational closure, and single-pass validation" (`docs/language/precept-language-spec.md:174`), with relational reasoning "single-pass and depth-bounded (no transitive chasing of a third field), per §0.4" (`precept-language-spec.md:256`), and SMT/symbolic solvers excluded on principle (`precept-language-spec.md:225`). Conditional *expressions* exist and are per-branch analyzable (`precept-language-spec.md` §0.4 item 2, :164; `samples/loan-application.precept:99`). Committed state is in-band by the sweep guarantee, so each firing's proof may soundly seed operand fields from their declared bounds.

This yields the **structural finding that shapes everything**: over *independent* operands, interval arithmetic at the ceiling is essentially *exact* for `+ − × ÷` and monotone functions. When the engine says "the product of these two intervals can exceed the cap," a real reachable scenario exists. So for independent-operand derived writes, **unprovable ≈ genuinely reachable**, and rejection surfaces a real case almost by construction. The merely-unprovable-but-actually-safe population is therefore confined to three FUNDAMENTAL classes (each grounded in a deliberate principle, not implementation lag):

- **(a) Correlation loss** — the same variable appears on both sides of `−` or `÷` (interval dependency problem); exact treatment requires symbolic cancellation = solver territory, excluded by `:225`/`:174`.
- **(b) Beyond bounded relational closure** — multi-fact linear combination (mini-simplex) and transitive third-field chains, deliberately excluded at `:256`.
- **(c) Piecewise/algebraic identities** — facts true of a formula's shape (bracket algebra, amortization closed forms), requiring symbolic manipulation.

The FORCES-WORSE hunt must live inside (a)–(c). Everything outside them exits the cell (either proves clean or is a genuine gap).

## 1. Derivatives/options — Reg-T short-put margin. **FORCES-BETTER**

```precept
field SpotPrice as money in 'USD' positive editable
field StrikePrice as money in 'USD' positive
field Contracts as integer min 1 max 500
field PremiumReceived as money in 'USD' positive
field MarginRequirement as money in 'USD' nonnegative

on MarkToMarket
    -> set MarginRequirement = PremiumReceived + (SpotPrice * 0.20 * Contracts * 100.0) - (StrikePrice - SpotPrice) * Contracts * 100.0
```

Naive formula (20% of spot minus out-of-the-money amount, plus premium). Interval reasoning: the OTM subtraction can dominate → result interval dips negative → unprovable into `nonnegative`. **Genuinely reachable** (deep-OTM puts make the naive expression negative — this is why the exchange rule is `max(20%·spot − OTM, 10%·strike) + premium`). Compiler demand: prove the write nonnegative. Forced remediation: the author writes the floor branch explicitly —

```precept
    -> set MarginRequirement = if SpotPrice * 0.20 - (StrikePrice - SpotPrice) >= StrikePrice * 0.10
        then PremiumReceived + (SpotPrice * 0.20 - (StrikePrice - SpotPrice)) * Contracts * 100.0
        else PremiumReceived + StrikePrice * 0.10 * Contracts * 100.0
```

Each branch proves nonnegative per-branch (in-ceiling). The rejection forced the author to encode *the actual FINRA rule* — the missing minimum-floor leg. The naive precept was wrong margin math. Textbook forces-better.

## 2. Lending/amortization — two writes, both FORCES-BETTER (one subtle)

```precept
field Principal as money in 'USD' positive max '5000000.00 USD'
field MonthlyRate as decimal min 0.0001 max 0.02 maxplaces 6
field TermMonths as integer min 12 max 480
field MonthlyPayment as money in 'USD' positive max '100000.00 USD'   # ability-to-repay policy cap
field RemainingPrincipal as money in 'USD' nonnegative

on Originate
    -> set MonthlyPayment = Principal * MonthlyRate * pow(1.0 + MonthlyRate, TermMonths) / (pow(1.0 + MonthlyRate, TermMonths) - 1.0)
```

*(Divisor nonzero is proof-carrying — pow at interval endpoints is monotone, provable in-ceiling, uncontested; not the cell.)* The `max '100000.00 USD'` cap: interval on the whole expression loses the r-correlation between numerator and denominator (class (a)) → wildly loose upper bound → unprovable. But check reachability: it is **not** merely-unprovable — P=5M, r=2%, n=12 genuinely yields ≈ $470k/month. The cap is violable, and the compiler forcing `when <payment expr> <= '100000.00 USD' -> set … / -> reject "Computed payment exceeds the ability-to-repay ceiling — extend the term or reduce principal"` is a real underwriting decision (decline vs restructure). **FORCES-BETTER.**

The subtle one:

```precept
on PostPayment
    -> set RemainingPrincipal = RemainingPrincipal - (MonthlyPayment - RemainingPrincipal * MonthlyRate)
```

The claim "amortization math guarantees the balance never goes negative" is true only in exact reals. Precept decimals are place-bounded; the payment is rounded to cents; **real servicing schedules do not clear exactly** — every production servicer carries a final-payment adjustment. The nonnegativity is unprovable (class (a): `RemainingPrincipal` appears twice with a multiplier), and the "it's safe, the prover is just blind" intuition is *false under rounding*. Forced remediation — `when MonthlyPayment - RemainingPrincipal * MonthlyRate <= RemainingPrincipal -> set … / else set RemainingPrincipal = '0.00 USD'` (final-payment plug) — is the genuine domain behavior the naive precept omitted. I went into this case expecting my first forced-fake; the rounding reality killed it. **FORCES-BETTER.**

## 3. Accounting — accumulated depreciation. **FORCES-BETTER**

```precept
field AcquisitionCost as money in 'USD' positive
field AccumulatedDepreciation as money in 'USD' nonnegative default '0.00 USD'
rule AccumulatedDepreciation <= AcquisitionCost because "An asset cannot depreciate below zero book value"

on PostDepreciation(Amount as money in 'USD' positive)
    -> set AccumulatedDepreciation = AccumulatedDepreciation + PostDepreciation.Amount
```

`Accum + Amount ≤ Cost` from `Accum ≤ Cost` alone is unprovable — and genuinely reachable (the straight-line periodic amount overshoots in the final period whenever cost isn't an exact multiple). Forced remediation: guard + `else set AccumulatedDepreciation = AcquisitionCost` — the final-period plug, which is *actual GAAP practice*. The rejection forces the author to answer a question the domain genuinely answers. **FORCES-BETTER.**

## 4. Actuarial/reserving — IBNR. **FORCES-BETTER (by exposing a wrong bound)** + one clean exit

```precept
field UltimateLosses as money in 'USD' nonnegative editable
field PaidLosses as money in 'USD' nonnegative
field CaseReserves as money in 'USD' nonnegative
field IbnrReserve as money in 'USD' nonnegative

on Reestimate -> set IbnrReserve = UltimateLosses - PaidLosses - CaseReserves
```

Unprovable (no relation declared; three-field linear fact is class (b)). The author's instinct — "ultimate is by definition at least paid + case, add a rule" — is **actuarially false**: favorable development with redundant case reserves produces *negative* IBNR in real reserving. The compile Error forces the author to confront that the `nonnegative` bound was a naive domain rule; the better precept either drops it (IBNR as signed) or authors the redundancy-release path explicitly. Rejection surfaced a genuine unanswered domain decision. **FORCES-BETTER.**

Contrast (exits the cell): `set NetReserve = GrossReserve - CededReserve` with `rule CededReserve <= GrossReserve because "Ceded reinsurance cannot exceed gross"` — nonnegativity of the subtraction discharges from the declared relation, exactly the pattern at `precept-language-spec.md:256`. The declared rule is a *real* reinsurance rule. Where a simple declarable relation exists, the ceiling proves it and the cell is empty.

## 5. FX — settlement cap. **FORCES-BETTER**

```precept
field AmountUsd as money in 'USD' positive max '10000000.00 USD'
field UsdEurRate as decimal min 0.70 max 1.20 maxplaces 6 editable
field SettlementEur as money in 'EUR' positive max '10000000.00 EUR'   # counterparty settlement limit

on Convert -> set SettlementEur = AmountUsd * UsdEurRate
```

Independent operands → interval product is exact → up to 12M EUR, cap violable, genuinely reachable. Forced remediation (guard + reject "exceeds counterparty settlement limit — split the trade") is a real FX-ops decision (auto-split vs reject vs escalate). **FORCES-BETTER** — and it illustrates the structural finding: independent operands never produce forced-fake, because "unprovable" there means "reachable."

## 6. Tax — effective rate. **FORCES-WORSE. This is the serious attempt, and it (mostly) survived scrutiny.**

```precept
field GrossIncome as money in 'USD' positive
field TaxDue as money in 'USD' nonnegative
field EffectiveRate as decimal min 0 max 0.37 maxplaces 4   # true invariant: effective rate never exceeds top marginal rate

on Assess
    -> set TaxDue = if GrossIncome <= '11600.00 USD' then GrossIncome * 0.10
        else if GrossIncome <= '47150.00 USD' then '1160.00 USD' + (GrossIncome - '11600.00 USD') * 0.12
        else '5426.00 USD' + (GrossIncome - '47150.00 USD') * 0.37
    -> set EffectiveRate = TaxDue / GrossIncome
```

The `max 0.37` bound is **mathematically true** of every progressive bracket schedule (effective ≤ top marginal, always). Proving it requires (i) per-branch piecewise-linear algebra over `TaxDue`'s definition (class (c)) and (ii) cancellation of `GrossIncome` through the division (class (a)) — both FUNDAMENTAL against the stated ceiling (`:174`, `:225`, `:256`), not implementation lag. The value is neither provably in-band nor provably violating. Squarely in the cell.

Now the decisive property: **"bound the source" does not terminate here.** Declaring `rule TaxDue <= GrossIncome * 0.37 because "top marginal ceiling"` merely relocates the identical undischargeable obligation to `TaxDue`'s own write (proving the piecewise formula satisfies the rule needs the same class-(c) algebra). Chasing further reaches raw ingress (`GrossIncome`), already bounded — no bound on any source discharges the target. In the FORCES-BETTER cases above, remediation *terminated* in a domain decision (a floor branch, a plug, a split rule). Here the only terminating remediations are:

1. **Delete `max 0.37`** — remove a true, auditor-meaningful invariant from the definition because it cannot be proven. The precept now says *less* about the domain than the author knows.
2. **Guard + dead else:** `when TaxDue / GrossIncome <= 0.37 -> set EffectiveRate = TaxDue / GrossIncome` plus `-> reject "Effective rate exceeds the top marginal rate"` — a refusal branch the author can prove on paper is unreachable, with an author-invented message describing an impossible event. The formula is duplicated (drift risk), and the definition now asserts a case *can* happen that cannot. Note the whipsaw with §0.6 item 10 (tautological-guard detection, `:232`-region): the guard is semantically tautological; the engine is simultaneously too weak to prove the bound and — were it ever strengthened — would flag the mandated workaround as a tautology.

Neither option encodes a domain decision. Option 2's *guard* still carries real domain content (the invariant, relocated), so "representability masquerading as policy" applies precisely to the **forced else-branch**, not the whole remediation — that is the honest scope of the finding. **Verdict: FORCES-WORSE, survived scrutiny with two caveats:**

- **Caveat 1:** if bracket thresholds/rates are ever *data* (editable fields, rate-table events — cf. `samples/tax-rate-configuration.precept`), the invariant stops being mathematically guaranteed, the else-branch becomes live governance against a fat-fingered table, and the case flips to FORCES-BETTER. The forced-fake only exists while the schedule is literal constants in the definition.
- **Caveat 2:** the field is a derived audit/display metric, not a governing input. One can argue such fields shouldn't carry bounds at all — but the spec's own audience commitment (`§0.8`) says the domain expert declares what they know to be true, and "effective rate never exceeds top marginal" is exactly such a truth. Pure prove-or-reject makes that truth *unwritable*.

## 7. Second FORCES-WORSE attempt — weighted-average cost basis. **Did not fully survive → NEUTRAL**

`set AvgCost = TotalCost / TotalShares` with `min`/`max` band = historical price limits; true by weighted-mean algebra; unprovable (classes (a)+(b): the inductive step `TotalCost + q·p ≤ (Shares+q)·High` needs two-fact linear combination, beyond bounded relational closure). But under scrutiny the band itself is weak as a *business rule* — it's a plausibility check, not a governing bound anyone operates on; deleting it loses little, and the guard-with-dead-else cost is the same as Case 6 without Case 6's audit-grade invariant. Downgraded to **NEUTRAL** honestly. Similarly the transitive-chain case (rule `A ≥ B`, rule `B ≥ C`, write `A − C` into nonnegative; unprovable by the deliberate depth bound at `:256`): remediation is declaring the implied `rule A >= C` directly — logically redundant, mild drift risk, but cheap, legible, and self-documenting. **NEUTRAL**, not worse.

## Verdict summary

| # | Domain / case | In-cell class | Verdict |
|---|---|---|---|
| 1 | Reg-T short-put margin | reachable (exits cell) | FORCES-BETTER (missing max-floor leg) |
| 2a | Amortization payment vs cap | reachable (exits cell) | FORCES-BETTER (decline/restructure decision) |
| 2b | Remaining principal nonnegative | (a) correlation — but rounding makes it violable | FORCES-BETTER (final-payment plug) |
| 3 | Accumulated depreciation ≤ cost | reachable | FORCES-BETTER (final-period plug, real GAAP) |
| 4 | IBNR nonnegative | (b) | FORCES-BETTER (exposes wrong domain bound — negative IBNR is real) |
| 5 | FX settlement cap | reachable (exits cell) | FORCES-BETTER (split/reject/escalate decision) |
| 6 | Effective tax rate ≤ top marginal | (a)+(c), constants-only | **FORCES-WORSE** — survived, scoped to the forced dead else-branch; flips to BETTER if the schedule is data |
| 7 | Avg-cost band / transitive chain | (a)(b) | NEUTRAL |

## Bottom line for the owner

1. **The hypothesis substantially holds in finance, for a structural reason, not a corpus accident:** at the design ceiling, independent-operand interval arithmetic is near-exact, so "unprovable" almost always means "genuinely reachable," and rejection then surfaces a real domain decision (floor legs, final-period plugs, caps, split rules, redundancy releases). Several cases where I expected forced-fake (amortization balance, depreciation) turned out to hide *real* domain behavior (rounding remainders, plug entries) that the naive precept omitted — the rejection was doing exactly the claimed work.
2. **The genuine FORCES-WORSE class exists and is precisely characterizable:** a *mathematically-true* invariant over a derived value whose proof requires correlated/piecewise algebra (classes (a)/(c)), where **no source bound terminates the obligation** — remediation relocates the identical unprovable proof until the author either deletes a true statement or authors an unreachable refusal with a fictional message. Effective-rate-≤-top-marginal is the exemplar. The class is narrow: its members are audit/display-grade derived metrics with constants-only formulas, and each flips to FORCES-BETTER the moment the formula's parameters become data.
3. **Design-relevant tension to note (quote-and-recommend, owner-gated):** proof philosophy #2 — "The language reports what is definitively broken, not what might be broken… Flagging only proven violations makes it a trusted guide" (`precept-language-spec.md:223`). Pure prove-or-reject in this cell rejects *unproven*, not proven, violations. That is already the accepted posture for proof-carrying obligations (classification #5, `:229`: unresolved → supply constraints), and in classes where remediation terminates it behaves identically well. But in the non-terminating class (finding 2) there is *no constraint the author can supply*, which is exactly the situation philosophy #2's "nag" warning describes. If pure prove-or-reject is adopted with no escape construct, the spec should either accept and document the forced dead-else idiom for this class, or the owner should revisit the class-boundary — not the posture — with that tension on the table.

Method note: all fragments constructed against `samples/loan-application.precept` and `samples/Test.precept` conventions (money bounds `max '1000 USD'` at `samples/Test.precept:5`; if/then/else expression at `samples/loan-application.precept:99`; guard+reject row pattern at `:97-102`). Ceiling classifications FUNDAMENTAL vs merely-unimplemented were each grounded in `precept-language-spec.md:174` (interval + bounded relational closure), `:225` (SMT exclusion), `:256` (depth bound). The three 2026-07-11 working docs were consulted for cell-fencing only (`docs/Working/fable-analysis-of-frank-response-2026-07-11.md` §1–2); no verdict above relies on them.