All probes done. I have verified current-engine behavior and corpus counts. Writing the section.

## Linear relationships between fields

**1. What it proves.** That a value built by adding or subtracting other fields stays inside its declared limits — using the relationships the author has already written down. If the definition says `Deposits − Withdrawals` can never go below zero *because a guard or rule guarantees withdrawals never exceed deposits*, this proof type is what lets the compiler follow that reasoning and accept the definition.

**2. What gets rejected without it.** The most common protective idiom in the whole sample corpus is "check the sum in the guard, then apply it" — e.g. `samples/event-venue-booking.precept:234` (`when DepositPaid + PayDeposit.Amount > RequiredDeposit → reject`) and `samples/production-order-tracking.precept:140`. Under the PROPOSED prove-or-reject design, a bounded running total written that way is rejected unless the engine can read the guard as a fact about the sum. Synthetic example (mirrors the sample idiom above; whole-dollar amounts):

```precept
field Total as integer min 0 max 10000 default 0

event Charge(Amount as integer min 0 max 10000)

state Open initial

from Open on Charge when Total + Charge.Amount <= 10000
    -> set Total = Total + Charge.Amount
    -> no transition

from Open on Charge
    -> reject "Charge of {Charge.Amount} would push the total past the 10000 limit"
```

This definition is safe — the guard makes overflow impossible. **(Today: already rejected — PRE0078, "Numeric computation exceeded the representable range on field 'Total'", verified via `precept_compile`.)** The band check on `min`/`max` fields is live today, but the engine cannot use the guard: it can only carry facts of the shape "one field compared to one other field" (`ProofEngine.Strategies.cs:1240`), never "field plus field compared to a limit," and only one such fact at a time — it deliberately never chains two facts together (`ProofEngine.Intervals.cs:629`, `ProofEngine.Composition.cs:512`). So it falls back to raw ranges — Total could be 10000, Amount could be 10000, sum 20000 — and rejects. Note the same accumulator into a field marked only `nonnegative` compiles clean today, but only because no proof obligation is generated at that write site at all (that posture isn't built — it's Phase 5 readiness-plan work), not because anything is proved.

**3. How the author clears it today, by hand.** Cap the assignment explicitly so the engine can see the bound in the expression itself:

```precept
from Open on Charge when Total + Charge.Amount <= 10000
    -> set Total = min(10000, Total + Charge.Amount)
    -> no transition
```

Verified: this compiles clean on today's engine (obligation Proved, computed range [0..10000]) because `min` carries a hand-written range rule (`Functions.cs:44-149`). Grade: **DUPLICATE-A-FORMULA** — the limit 10000 now appears three times (the field's `max`, the guard, and the cap), and the cap is worse than redundant: if the guard is ever edited wrong, the `min` silently truncates the overage instead of the definition rejecting it. The author is forced to weaken "impossible" into "silently clamped" to get past the compiler.

**4. Cost to build.** Reuses a real substrate: the guard-splitting machinery that breaks and/or conditions into cases (`ProofEngine.Strategies.cs:794`), the existing one-field-vs-one-field fact extraction and rule-sourced facts (`Strategies.cs:1159`, `Composition.cs:539`), the range arithmetic (`NumericInterval.cs`), and a clean splice point for a new strategy in the discharge cascade (`ProofEngine.cs:1054`, extension seams documented). Genuinely new: representing facts with constants and multiple terms (today's shape is strictly `field op field` — no constants, no coefficients), combining two or more facts (today is deliberately single-hop), and producing a concrete "here's a value assignment that breaks it" counter-example on failure (no such machinery exists anywhere — grep of `src/Precept` for any linear-solver or witness code returns nothing; the inventory confirms the solving core is entirely new). Main risk: the current one-hop limit exists to prevent circular reasoning and stale facts (a guard fact must go dead once the field it mentions is rewritten mid-handler — the `ReassignedBefore` stamps, `ProofEngine.cs:409`); a general solver must respect both disciplines or it proves things that aren't true. Mitigating the size: each proof problem is tiny — one guard, a handful of rules, one assignment. Plain grade: **Large** — the single biggest new piece of proof machinery in the proposal, but well-bounded per use.

**5. Benefit.** This is the highest-frequency pattern in the corpus. Grep of `samples/*.precept` (77 files): **179 accumulator writes** (`set X = X + …` / `set X = X − …`) across **42 of 77 files**; **24 guards** that compare a field-plus-amount sum against a limit (the guard-then-accumulate idiom of the example above); **6 rules** relating a sum of fields to a third field (e.g. `ProducedQuantity + ScrapQuantity <= PlannedQuantity`, `production-order-tracking.precept:66`); **16 computed fields** that add or subtract fields (`OutstandingBalance <- TotalCharged - TotalPaid` and kin). Without this proof type, prove-or-reject rejects the corpus's own house style, and every one of those ~180 sites needs a semantics-weakening clamp or a duplicated formula by hand. With it, the definitions authors already write — guard first, then apply — pass as-is, and a genuine mistake comes back with a concrete breaking example instead of a bare "cannot prove."

**6. Verdict.** **MVP** — over half the sample files use the exact pattern this proof type covers, and the only hand-escape today quietly converts "structurally impossible" into "silently capped"; prove-or-reject cannot ship without it.