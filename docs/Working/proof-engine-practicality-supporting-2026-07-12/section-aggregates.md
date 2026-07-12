## Aggregates over a collection (sum, count, average)

**1. What it proves.** That a total, count, or average taken over a collection stays within a declared limit — for example, "the units across all components never exceed 5,000" — by combining what's already known about the collection: how many items it can hold, and what each item's value is allowed to be. It also keeps a hand-maintained running total honest: when every add and remove adjusts the total, the compiler confirms the total genuinely tracks the collection instead of trusting the author to keep them in sync.

**2. What gets rejected without it.** A synthetic example, modeled on the real bill-of-materials sample (`samples/bill-of-materials-management.precept:35,109`). PROPOSED design — and today's compiler already rejects it (PRE0078), because bounds on assigned fields are already enforced prove-or-reject; what's missing is the reasoning that would let this *safe* definition pass:

```precept
field ComponentPartNumbers as set of string maxcount 50
field TotalComponentUnits as integer default 0 nonnegative max 5000

event AddComponent(PartNumber as string notempty, Quantity as integer min 1 max 100)

from Draft on AddComponent when ComponentPartNumbers.count >= 50
    -> reject "BOM is full"
from Draft on AddComponent
    -> add ComponentPartNumbers AddComponent.PartNumber
    -> set TotalComponentUnits = TotalComponentUnits + AddComponent.Quantity   # REJECTED: PRE0078
    -> no transition
```

This is mathematically safe — at most 50 items of at most 100 units each is exactly 5,000 — but the compiler cannot connect "the collection holds at most 50 items" to "the total of their quantities is at most 5,000." It sees only "some number plus up to 100," computes a possible range of 1 to 5,100, and rejects. So without this capability, prove-or-reject turns away correct definitions of a very common shape.

A second gap sits in front of this one: you cannot ask about a collection's total directly at all. There is no `.sum` or `.average` on collections — those were deliberately held back from the language (`docs/language/collection-types.md:860`), which is exactly why every sample keeps a separate running-total field by hand.

**3. How the author clears it today, by hand.** Guard the row so the total, *before* the add, leaves room for the largest possible addition — the author subtracts the biggest allowed item (100) from the bound (5,000) in their head:

```precept
from Draft on AddComponent when TotalComponentUnits <= 4900
    -> add ComponentPartNumbers AddComponent.PartNumber
    -> set TotalComponentUnits = TotalComponentUnits + AddComponent.Quantity
    -> no transition
from Draft on AddComponent
    -> reject "Adding more units would push the total past 5000"
```

Verified: this compiles clean on today's engine (proof ledger shows the bound proved, computed range 1 to 5,000). Note what does *not* work: writing the natural guard `when TotalComponentUnits + AddComponent.Quantity > 5000 -> reject` — verified still rejected today, because the prover can't use a guard that mixes two values with arithmetic. The author must pre-compute the `4900` themselves, repeat it on every row that touches the total, and re-derive it whenever the bound or the per-item cap changes. Grade: **DUPLICATE-A-FORMULA**.

**4. Cost to build.** Two separable pieces. (a) *The bound-transfer rule* — "total lies between count-times-smallest and count-times-largest" — reuses the existing range arithmetic (`NumericInterval.cs:73–108`), the already-built item-count tracking that follows every add/remove (`ProofEngine.cs:579–671`, proved in the probe above), and the existing extraction of per-item value limits (`ProofEngine.Intervals.cs:279,304`). The transfer rule itself is new — today the machinery explicitly refuses to carry item bounds through anything that isn't a plain item read (`ProofEngine.Intervals.cs:84–97`). (b) *Running-total consistency* — recognizing that `Total = Total + item` paired with `add` preserves "total equals sum of items" — is new proof logic, though it mirrors the shipped count-delta pattern step for step. Main risk: neither piece can ship until `.sum`/`.average` enter the language, and that surface was deliberately held back as a language-design decision (`collection-types.md:860`) — a decision gate, not just engineering. Proof work alone: **Medium**. With the language surface: **Large**.

**5. Benefit.** The hand-maintained running total is one of the most common patterns in the corpus: 179 accumulation sites (`set X = X + …`) across 40 of 77 sample files (corpus grep), 12 of them fields literally named *Total*. The bill-of-materials sample maintains one total across three separate rows (add at line 109, update at 114, remove at 123) — one wrong row and the total silently drifts from the collection, which is exactly what this proof would catch. Meanwhile zero samples use an aggregate accessor, because none exists. High reach — but today's authors have a working (if repetitive) workaround, and only totals that carry a declared bound trigger the proof at all.

**6. Verdict.** WORTH-BUILDING-AS-A-LATER-PHASE — the pattern is everywhere (40 of 77 samples), but it's gated behind a deliberately deferred language-surface decision, and the one-line pre-computed guard clears it on today's engine in the meantime.