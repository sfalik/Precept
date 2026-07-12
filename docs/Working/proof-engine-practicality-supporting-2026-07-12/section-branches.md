I have everything verified. Here is the section.

## Reasoning case-by-case (guards, min/max, date ranges)

**1. What it proves.** When a definition already names its cases — the `when` condition on a handler row, the two arms of an `if … then … else`, the two sides of a `min`/`max` — this proof type checks the bound separately in each case, using what each case tells you. "The discount is the requested amount when the request is 50 or less, otherwise it's exactly 50" is provably within a 0–50 band *because* of the case split; no single formula shows it.

**2. What gets rejected without it.** Under the PROPOSED prove-or-reject design, this definition is rejected — the compiler can't see that the `if` condition keeps the first arm under the cap (synthetic example, written in the conventions of `samples/travel-reimbursement.precept`):

```precept
field DiscountPercent as decimal default 0 min 0 max 50

event ApplyDiscount(Requested as decimal nonnegative)

from Active on ApplyDiscount
    -> set DiscountPercent = if ApplyDiscount.Requested <= 50 then ApplyDiscount.Requested else 50
    -> no transition
```

(Today: **already rejected**, PRE0078 — the constant-band check is live, and the engine unions the two arms of the `if` without ever assuming the condition inside each arm, so the result is treated as unbounded. Verified by compile: obligation `IntervalContainment` lands `Unresolved` with computed range `[−∞ .. +∞]`; the union-without-assumption behavior is `ProofEngine.Intervals.cs:135-140`.)

Two more shapes fail the same way today, verified by compile probes: rewriting the cap as `min(ApplyDiscount.Requested, 50)` proves the *upper* bound but not the lower (the `nonnegative` on the event input doesn't flow into the range — computed range came back `[−79228162514264337593543950335 .. 50]`); and putting the condition on the handler row (`when ApplyDiscount.Requested >= 0 and ApplyDiscount.Requested <= 50`) doesn't help either, because guard narrowing works for **fields** but not for **event inputs** — the identical guard over a plain field proves clean (`[0 .. 50]`, Proved), the event-input version stays `[−∞ .. +∞]`. (These compile results are current-behavior observations from the live compiler, not design authority.)

**3. How the author clears it today, by hand.** Replace the case logic with `clamp`, which the engine understands directly:

```precept
from Active on ApplyDiscount
    -> set DiscountPercent = clamp(ApplyDiscount.Requested, 0, 50)
    -> no transition
```

Verified: compiles clean on today's engine — the containment obligation is Proved with computed range `[0 .. 50]` (clamp's range rule is hand-written in the function catalog, `Functions.cs:44-149`). Grade: **TRIVIAL** — *when the case logic is really just a cap or floor*. When each case carries a different business formula (e.g. `samples/insurance-claim.precept:127`: "if the fraud flag is set, approve at most half the claim; otherwise approve the requested amount"), there is no single clamp — the author must restructure into separate handler rows and route the value through a plain field first (since event-input guards don't narrow). That shape grades **DUPLICATE-A-FORMULA**.

**4. Cost to build.** Most of this proof type already exists and runs. Present: splitting a guard into its and/or branches and requiring each branch to prove independently (`ProofEngine.Strategies.cs:794`); guard-narrowed per-field ranges (`ProofEngine.Intervals.cs` — the `BuildNarrowedIntervals` builder); hand-written range rules for `min`/`max`/`clamp`/`abs`/`floor`/`ceil`/`round` (`Functions.cs:44-149`). The genuinely new delta is two pieces: (a) assuming the `if` condition inside each arm of a conditional — this is exactly Slice 5.2 of the readiness plan ("per-branch guard narrowing via AssumedConditions/EffectiveGuard," `docs/Working/compiler-readiness-plan-2026-06-16.md:1003-1020`), scoped but unbuilt; and (b) extending guard narrowing to event inputs, which today only covers fields (observed via the compile probes above; the narrowing builder is keyed per-field). Both reuse the existing branch-splitting and range machinery wholesale. Calendar-date cases ride along only as far as dates are modeled as numbers (as the samples do with day counters, e.g. `samples/library-book-checkout.precept`); a true date-range domain would be extra. Main risk: branch counts multiply with deeply nested and/or guards (the splitter is exponential in nesting — noted at `ProofEngine.Strategies.cs:794`), bounded in practice by guard size. Plain size: **Small**.

**5. Benefit.** This is the single most common structure in real definitions. Across the 77 sample files: **288** handler rows carry a `when` guard, **63** of those compare values numerically, there are **28** `if … then … else` expressions (**4** of them computing a numeric field — `samples/insurance-claim.precept:127,156`, `samples/event-registration.precept:62,72`), and **5** `min`/`max`/`clamp`/`abs` calls (all grep counts, `samples/*.precept`). Guards and case expressions are *how domain authors natively write caps and tiers*. Without the missing delta, prove-or-reject forces every conditional cap into a `clamp` rewrite (loses the author's phrasing) or a duplicated-formula restructure — and the insurance-claim fraud split has no clamp form at all.

**6. Verdict.** **MVP** — case logic is the corpus's most common idiom for keeping values in bounds; a prove-or-reject engine that can't read the cases the author already wrote rejects natural, correct definitions wholesale, and most of the machinery is already built.