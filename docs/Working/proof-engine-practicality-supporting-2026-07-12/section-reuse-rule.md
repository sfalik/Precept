## Letting a rule the author already wrote do the work

**1. What it proves.** When you have already declared a rule — "the security deposit never exceeds three months of payment" — a later calculation should be able to lean on that rule directly. The engine takes the fact you stated and applies it where it's needed; it does not ask you to restate it, and it does not try to figure it out from scratch.

**2. What gets rejected without it.** Half of this capability is already built and working: if your rule compares two plain fields (`rule Deposits > Withdrawals`), today's engine already uses it — a division by `Deposits - Withdrawals` proves safe against exactly that rule (verified live: obligation "Divisor must be non-zero" **Proved**, via the shipped rule-matching step, `ProofEngine.Strategies.cs:1078`). The corpus leans on this shape heavily: 88 of the 149 rules across `samples/*.precept` are plain field-vs-field comparisons.

What is *not* built: rules where either side has any arithmetic in it — a multiplier, a sum, a percentage. The moment you write `MonthlyPayment * 3` instead of a bare field name, the rule becomes invisible to the proof, even when a later calculation is word-for-word the same expression. Synthetic example, modeled on the real rule at `equipment-lease-agreement.precept:78`:

```precept
field MonthlyPayment as decimal positive maxplaces 2 default 1.0 editable
field SecurityDeposit as decimal nonnegative maxplaces 2 default 0.0
field RefundableHeadroom as decimal nonnegative maxplaces 2 default 0.0

rule SecurityDeposit <= MonthlyPayment * 3 because "Security deposit cannot exceed three months of payment"

from Active on RecordDeposit
    -> set SecurityDeposit = RecordDeposit.Amount
    -> set RefundableHeadroom = MonthlyPayment * 3 - SecurityDeposit
    -> no transition
```

The rule guarantees the headroom is never negative — the author already said so. Under the PROPOSED design, the write into the `nonnegative` field must be proven, the rule can't be applied, and the definition is **rejected** *(today: compiles clean — this posture isn't built; verified by compile probe, zero errors)*. And where obligations are already live today, this gap already rejects real shapes: divide by `Deposits - Withdrawals * 2` under the rule `Deposits > Withdrawals * 2` fails **today** with error PRE0083, even though the rule states the divisor is positive verbatim (verified by compile probe). Writing the comparison into the row's `when` guard instead of a rule does not help either — same PRE0083 (verified; guards go through the same bare-fields-only reading, `ProofEngine.Strategies.cs:1240`).

**3. How the author clears it today, by hand.** Introduce a mirror field that holds the arithmetic, keep it up to date at every write site, and restate the rule in bare field-vs-field form:

```precept
field DepositCap as decimal nonnegative maxplaces 2 default 3.0

rule SecurityDeposit <= DepositCap because "Security deposit cannot exceed the deposit cap"

from Active on SetPayment
    -> set MonthlyPayment = SetPayment.Monthly
    -> set DepositCap = SetPayment.Monthly * 3    # must repeat the formula here, forever
    -> no transition
```

Verified: this compiles clean on today's engine, and in the live division variant the obligation flips from **Unresolved** to **Proved** (compile probes; discharge via the shipped rule-matching step). A `max(0.0, …)` clamp also compiles clean today, but it papers over the guarantee rather than using it — and it can't fix the division case at all. Grade: **DUPLICATE-A-FORMULA** — the formula now lives in the rule's meaning *and* in every event row that touches its inputs, and nothing stops the copies drifting apart.

**4. Cost to build.** Substantial reuse: the match-a-declared-rule-to-an-obligation step already exists and runs for bare fields (`ProofEngine.Strategies.cs:1078`, rule-sourced facts at `:1159`, unconditional-rule collection at `ProofEngine.Composition.cs:539`), and the staleness discipline — a fact stops being usable once one of its fields is rewritten mid-flow — is already enforced (`ProofEngine.cs:409`). New work: the rule-reading step must accept arithmetic on either side (today it accepts only `field compare field`, `ProofEngine.Strategies.cs:1240`), and the matching must recognize more than the one subtraction shape it handles now (`ProofEngine.Strategies.cs:1277`). Deliberately *not* needed: no equation solver, no chaining of multiple rules — this stays "apply one stated fact where it appears," the same one-hop discipline the engine already enforces (`ProofEngine.Intervals.cs:629`). Main risk: scope creep — deciding how much rearranging (`A <= B*3` vs `B*3 - A >= 0`) still counts as "applying what was written" before it quietly becomes an equation solver, which is a different and much larger project. **Medium.**

**5. Benefit.** 22 of the 149 rules in `samples/*.precept` (15%) carry arithmetic in the comparison and are invisible to proof today (grep: 149 rules total; 88 bare field-vs-field; 22 with `*`, `+`, or `-`). Of those 22, fourteen are money/count caps of exactly the kind later calculations lean on — deposit caps, debt ceilings, refund limits (e.g. `SecurityDeposit <= MonthlyPayment * 3`, `ExistingDebt <= AnnualIncome * 3.0`, `RefundAmount + ForfeitAmount <= AmountPaid`); the other eight are date-window rules. Every one of these that a calculation touches costs the author a mirror field plus a repeated formula at every write site — a standing invitation for the two copies to drift. This is also the proof type that most directly honors the product's promise: the author states the business fact once, and it does the work.

**6. Verdict.** **PREREQUISITE** — prove-or-reject can technically ship without it (the mirror-field fix compiles today), but flipping the posture on would turn one in seven of the corpus's rules into a demand to duplicate a formula, which is exactly the scattered-logic disease the product exists to cure.