---
title: "Prove-or-Reject: What the Proof Engine Needs — MVP and Roadmap"
status: Draft — 2026-07-12 (independent analysis; not owner-ratified)
author: Independent analysis (Fable), orchestrated by Claude
owner: Shane
purpose: Practicality of the proposed prove-or-reject engine — per proof type, what precept it rejects, how the author clears it today, cost/benefit, and MVP-vs-later-phase; rolled up into an MVP set + phased roadmap.
provenance: proof-engine-practicality-supporting-2026-07-12/ (engine inventory + 9 proof-type sections + adversary)
note: Plain-language by design. Compile results describe the CURRENT engine (observations, not design authority). Nothing owner-ratified.
revised: "Revised 2026-07-12 to incorporate Frank's review — §1 split into §1a/§1b, spec:256 & spec:225 linkages surfaced, disposition framing softened; owner-gated items surfaced not resolved; verdicts/figures/citations unchanged"
---

# Prove-or-Reject: What the Proof Engine Needs — MVP and Roadmap

**Status: assembled proposal for owner decision. Nothing in this document is owner-ratified — every verdict below is a recommendation awaiting your call.**

## What this document decides

Precept's design target is **prove-or-reject**: when a definition says a value must stay inside a limit, the compiler must *prove* it can never escape — and if it can't prove it, it rejects the definition outright. Nothing is ever "checked later at runtime." That target is **assumed-settled here for scoping purposes** — the citation (`ProofEngine.cs:432`) is a code comment recording implementation intent, not an owner ratification; whether prove-or-reject is the disposition at all remains an open owner call. This document does not re-litigate it; it **costs and stages the work conditional on it**: which kinds of proof the engine must be able to do before you can turn prove-or-reject on without rejecting the definitions authors naturally write, and which kinds are worth adding later. One contingency governs everything below: **this whole roadmap is downstream of ratifying the disposition — if the disposition softens, the staging is void.**

**Open owner decisions this document surfaces but does not make:**

1. **The prove-or-reject disposition itself.** Not ratified. Everything below is conditional on it.
2. **Whether §1b — combining two or more facts — is built at all, and if so whether it lands in the MVP or a later phase.** It would require an owner-authorized override of the locked single-pass depth-bound (`docs/language/precept-language-spec.md:256`). The 77-sample corpus shows no case that needs it, but the corpus is not comprehensive — the need is an open question to weigh on domain grounds, feasibility pending (§1b).
3. **Whether a legible Farkas-style certificate satisfies the opaque-solver ban** (`docs/language/precept-language-spec.md:225`). Any linear-arithmetic strategy (§1a or §1b) is spec-legal only if it ships with such a certificate (§1, §5a) — and whether that criterion *satisfies* the ban is itself an owner ratification, not a settled fact.
4. **The `.sum`/`.average` language-surface decision** that gates collection aggregates (§7) — deferred to the language-design process; no syntax is sketched here.

One term, defined once and used throughout: a **check** (the code calls it a "proof obligation") is a single fact the compiler is required to prove — "this divisor is never zero," "this total never exceeds 5,000." Every limit, rule, and risky operation generates checks; the engine tries a fixed list of strategies to prove each one; an unproven check becomes a rejection.

**Scope:** this document covers *numeric-limit reasoning* — the proof types that decide whether computed values stay inside declared bounds. Several whole proof families are already built, working, and not at issue; they're summarized next so the baseline is honest, then left alone.

**Baseline — what already works today (keep it all; ~zero cost):**

- **Currency and unit matching.** Mixing dollars and euros, or hours and kilograms, is rejected today (PRE0114; ~950 lines in `ProofEngine.Qualifiers.cs`; money appears in 37 of 77 sample files). Live and prove-or-reject already.
- **Text-length limits.** `maxlength`/`minlength`/`notempty` are proved today (`ProofEngine.Lengths.cs:25`); 72 of 77 sample files use them.
- **Collection-size tracking.** Every add/remove to a size-limited collection is tracked and proved (`ProofEngine.cs:579–671`).
- **Division safety via declared signs.** Mark a divisor `positive` or `nonzero` and division proves safe (verified live: `samples/travel-reimbursement.precept:13,58`).
- **Rounding functions.** `floor`/`ceil`/`round`/`truncate`/`min`/`max`/`clamp`/`abs` all carry hand-written result-range rules (`Functions.cs:44–155`) and prove end to end (verified by compile).
- **Rules comparing one plain field to another.** `rule Deposits > Withdrawals` already discharges checks that depend on it (`ProofEngine.Strategies.cs:1078`; verified live). 88 of the corpus's 149 rules have this shape.

Everything below is what's *missing*, section by section, each with: what it proves, an example rejected without it, how an author clears it by hand today (with an honesty grade), cost, benefit, and a verdict. All compile results are observations of the current engine's behavior, not design authority. Corpus = the 77 `.precept` files in `samples/`.

---

# The MVP set

## 1. Linear relationships between fields — two candidate builds (§1a and §1b)

**What it proves.** That a value built by adding or subtracting other fields stays inside its declared limits — using the relationships the author has already written down. If the definition says `Deposits − Withdrawals` can never go below zero *because a guard or rule guarantees withdrawals never exceed deposits*, this proof type is what lets the compiler follow that reasoning and accept the definition.

**Two separable capabilities live here, and they are costed separately:**

- **§1a — richer facts, one at a time.** Represent facts with constants and multiple terms (`field + field ≤ constant`; today's fact shape is strictly one field compared to one other field), and recognize that an assigned expression *is* the very sum a guard already bounds — so the guard's bound transfers directly. One fact, applied once. Still single-hop.
- **§1b — combining two or more facts.** Chaining facts together to derive a bound no single stated fact gives — the genuinely new solving core (in textbook terms: Fourier–Motzkin elimination / Farkas combinations / simplex — machinery that adds inequalities together).

Every corpus site this section counts — the flagship guard-then-apply idiom below, the 24 guard-sum sites, the 6 sum-vs-field rules, the 16 add/subtract computed fields — is a **single-fact** shape: §1a covers all of them. The 77-sample corpus shows **no case that requires §1b**. That is not the same as "§1b is not needed": the corpus is not comprehensive, and whether real domains will demand multi-fact combination is an **open question for the owner to weigh on domain grounds, feasibility pending** (details in §1b). **Whether §1b belongs in the MVP or a later phase is an open owner decision this document does not make.**

### 1a. Multi-term facts + guard-sum matching (the single-fact core)

**What gets rejected without it.** The most common protective idiom in the whole corpus is "check the sum in the guard, then apply it" — e.g. `samples/event-venue-booking.precept:234` (`when DepositPaid + PayDeposit.Amount > RequiredDeposit → reject`) and `samples/production-order-tracking.precept:140`. Under the proposed design, a bounded running total written that way is rejected unless the engine can read the guard as a fact about the sum. Synthetic example (mirrors the sample idiom; whole-dollar amounts):

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

This definition is safe — the guard makes overflow impossible. **Today: already rejected** (PRE0078, "Numeric computation exceeded the representable range on field 'Total'", verified by compile). The limit check on `min`/`max` fields is live today, but the engine cannot use the guard, for two distinct reasons. First — §1a's target — it can only carry facts of the shape "one field compared to one other field" (`ProofEngine.Strategies.cs:1240`), never "field plus field compared to a limit." Second — §1b's territory — it carries only one such fact at a time and deliberately never chains two facts together; the two code sites enforcing that (`ProofEngine.Intervals.cs:629`, `ProofEngine.Composition.cs:512`) are not incidental engineering but the in-code enforcement of a locked spec decision (see §1b). The flagship idiom needs only the first fix: read the guard's multi-term fact and match it to the assigned expression. Without it, the engine falls back to raw ranges — Total could be 10000, Amount could be 10000, sum 20000 — and rejects. Note: the same running total written into a field marked only `nonnegative` compiles clean today, but only because no check is generated at that write site at all (that half of prove-or-reject isn't built yet — it's Phase 5 readiness-plan work), not because anything is proved.

**How the author clears it today.** Cap the assignment explicitly so the engine can see the limit in the formula itself:

```precept
from Open on Charge when Total + Charge.Amount <= 10000
    -> set Total = min(10000, Total + Charge.Amount)
    -> no transition
```

Verified: compiles clean today (check Proved, computed range [0..10000]) because `min` carries a hand-written range rule (`Functions.cs:44–149`). Grade: **DUPLICATE-A-FORMULA** — the limit 10000 now appears three times (the field's `max`, the guard, and the cap), and the cap is worse than redundant: if the guard is ever edited wrong, the `min` silently truncates the overage instead of the definition rejecting it. The author is forced to weaken "impossible" into "silently clamped" to get past the compiler.

**Cost to build (§1a).** Reuses a real substrate: the guard-splitting machinery that breaks and/or conditions into cases (`ProofEngine.Strategies.cs:794`), the existing one-field-vs-one-field fact extraction and rule-sourced facts (`Strategies.cs:1159`, `Composition.cs:539`), the range arithmetic (`NumericInterval.cs`), and a clean splice point for a new strategy in the discharge cascade (`ProofEngine.cs:1054`). Genuinely new for §1a: representing facts with constants and multiple terms (today's shape is strictly `field op field` — no constants, no coefficients), and the matching step that recognizes the assigned expression as the very sum the guard bounds. §1a stays one-fact-at-a-time, so both existing safety disciplines survive unchanged: the one-hop limit against circular reasoning, and the staleness rule that a guard fact goes dead once the field it mentions is rewritten mid-handler (the `ReassignedBefore` stamps, `ProofEngine.cs:409`).

**Spec relationship (§1a): likely no override needed.** The language spec locks the relational-reasoning mechanism as *"single-pass and depth-bounded (no transitive chasing of a third field), per §0.4"* (`docs/language/precept-language-spec.md:256`). §1a stays inside that discipline — single-pass, depth-bounded, no chasing of a third field. What would need updating is the spec's *fact-shape* wording (it currently describes field-to-field facts), widened to admit multi-term facts; the depth-bound decision itself is untouched. That is a wording widening, not an override of the locked decision — though the owner confirms even that.

Taken together, §1a plus §1b carry this section's original grade: **Large** — the single biggest new piece of proof machinery in this proposal. The expensive part of that grade is the solving core, which sits entirely in §1b; §1a is the smaller share, riding on the existing substrate. (Explaining *failed* proofs with a concrete breaking example is priced separately in section 4, not here.)

**Benefit (§1a covers all of it).** This is the highest-frequency pattern in the corpus. Grep of `samples/*.precept` (77 files): **179 running-total writes** (`set X = X + …` / `set X = X − …`) across **42 of 77 files**; **24 guards** that compare a field-plus-amount sum against a limit (the guard-then-apply idiom above); **6 rules** relating a sum of fields to a third field (e.g. `ProducedQuantity + ScrapQuantity <= PlannedQuantity`, `production-order-tracking.precept:66`); **16 computed fields** that add or subtract fields (`OutstandingBalance <- TotalCharged - TotalPaid` and kin). Without this proof type, prove-or-reject rejects the corpus's own house style, and every one of those ~180 sites needs a guarantee-weakening clamp or a duplicated formula by hand.

**A sequencing caveat on the 179-site headline.** That figure overstates §1a's *current* bite. Most of those sites are running totals written into fields marked only `nonnegative`, which compile clean today because no check is generated at that write site at all — that half of prove-or-reject is Phase 5 readiness-plan work. Those sites only begin rejecting once Phase 5 lands obligation generation. The set that bites *today* is the narrower one: bounded `min`/`max` fields with the guard-then-apply idiom. The MVP need is real, but its urgency is coupled to Phase 5 sequencing — weigh §1a against items that bite now with that coupling in view.

**Verdict (§1a): MVP.** Over half the sample files use the exact pattern §1a covers, and the only hand-escape quietly converts "structurally impossible" into "silently capped." The hand-fix does compile — so this is MVP on scale and honesty grounds, not because authors are literally stuck — but prove-or-reject that rejects the product's own recommended idiom is not shippable.

### 1b. Combining two or more facts (open owner decision)

**What it adds.** Deriving a bound that no single stated fact gives — for example, combining "A never exceeds B" with "B plus C stays under the limit" to bound a value built from A and C. This is the classical linear-arithmetic solving core (Fourier–Motzkin elimination / Farkas combinations / simplex). It is entirely greenfield: a grep of `src/Precept` finds no linear-arithmetic solver code of any kind.

**What gets rejected without it.** The shapes at stake are the ones where the safety argument only exists once two separately-stated rules are put together — the knowledge genuinely arrives as two declarations, from two different concerns, and neither one alone covers the value being computed (the companion feasibility analysis catalogs the recurring business families of this shape: staged approvals, balance identities, blended caps, allocations — `docs/Working/proof-engine-linear-solver-feasibility-2026-07-12.md` §1b). Synthetic example, in the conventions of `samples/production-order-tracking.precept`: a planning rule keeps commitments below the plan, an allocation rule keeps allocations within commitments, and a later calculation divides by the open remainder:

```precept
field PlannedUnits as integer min 0 max 10000 default 100 editable
field CommittedUnits as integer min 0 max 10000 default 0 editable
field AllocatedUnits as integer min 0 max 10000 default 0 editable
field OverheadPool as decimal nonnegative maxplaces 2 default 0.0 editable
field OverheadPerOpenUnit as decimal default 0.0

rule CommittedUnits < PlannedUnits because "Commitments must always leave open capacity on the plan"
rule AllocatedUnits <= CommittedUnits because "Units are allocated only out of committed capacity"

event Reallocate

state Active initial

from Active on Reallocate
    -> set OverheadPerOpenUnit = OverheadPool / (PlannedUnits - AllocatedUnits)
    -> no transition
```

The division is safe, and the author has already written down why: allocations never exceed commitments, and commitments always leave room, so `PlannedUnits - AllocatedUnits` is at least 1. But **no single rule says so** — the first rule never mentions `AllocatedUnits`, the second never mentions `PlannedUnits`, and only adding the two together yields the missing fact (`AllocatedUnits < PlannedUnits`). This is exactly what separates §1b from §1a: §1a lets the engine read one richer fact and apply it once; here there is no one fact to apply, because the safety argument does not exist until two rules are combined. **Today: already rejected** (PRE0083, *"Division is unsafe: '(PlannedUnits - AllocatedUnits)' can be zero"*; the divisor check lands Unresolved — verified by compile). Division safety is one of the checks that is already live, so this gap bites now, not only under the proposed design. The same two-rules-jointly-bound-a-value shape behind a plain limit — e.g. the staged-approvals pair `rule Approved <= Requested` and `rule Requested <= BudgetLine`, with `BudgetLine - Approved` written into a `min 0` field — is likewise rejected today (PRE0078, computed range reported as [−100000 .. 100000] against declared [0 .. 100000]; verified by compile).

**How the author clears it today.** Run the combining step by hand: derive the conclusion of the two rules yourself and state it as a third rule, in the bare field-vs-field shape the engine's single-fact machinery already reads (§6):

```precept
rule AllocatedUnits < PlannedUnits because "Allocations can never fill the plan (restates the two rules above)"
```

Verified: compiles clean today — the divisor check flips to **Proved** (strategy FlowNarrowing, the shipped rule-matching step, `ProofEngine.Strategies.cs:1078`). Putting the same comparison on the handler row instead (`when AllocatedUnits < PlannedUnits`) also proves clean (verified), at the same duplication cost restated at every division site. Grade: **DUPLICATE-A-FORMULA** — the third rule is not new knowledge; it is a consequence of the first two, hand-derived once and then maintained forever. Unlike §1a's clamp it does not weaken the guarantee (the composite rule is itself enforced at runtime), but nothing ties the three declarations together: soften either source rule later (say commitments may now fill the plan) and the composite silently stops following from anything — it lingers as an unexplained standalone constraint, rejecting data the two real rules would allow. The author is hand-running the exact combining step §1b would own, which is tolerable for one two-rule chain and degrades as chains lengthen — precisely the scattered, keep-these-in-sync-yourself logic the product exists to prevent. And the workaround only exists where the single-fact rule reading is live: in the bounds variant above, even the hand-written composite rule does not discharge today (verified — the rule-reading step serves the risky-operation checks, not the declared-bounds check), leaving only §1a's guarantee-weakening clamp. None of this decides §1b — the workaround's cost is one input to the open owner decision below, not a verdict on it.

**Corpus status — and why that does not close the question.** No site in the 77-file corpus needs two facts combined; every counted site in this section is a single-fact shape that §1a handles. But the corpus is 77 samples, not the universe of definitions Precept intends to govern. So §1b's need is an **open question to weigh on domain grounds — what real domains will demand — with feasibility assessment pending.** It is *not* concluded out of scope here, and it is not deferred by default; the owner weighs it.

**The locked-spec question §1b raises (owner-gated).** The spec locks the relational mechanism: *"The mechanism is single-pass and depth-bounded (no transitive chasing of a third field), per §0.4."* (`docs/language/precept-language-spec.md:256`). Combining two or more facts is precisely a relaxation of that locked decision. The engine enforces it at two points — `ProofEngine.Intervals.cs:629` and `ProofEngine.Composition.cs:512` deliberately refuse to chain facts. Those two sites are the locked decision's enforcement in code, and they are exactly what a §1b build would change. **Building §1b therefore requires an owner-authorized override of the locked spec decision — a staging document cannot grant that.** Whether to authorize the override, and whether §1b then lands in the MVP or a later phase, are open owner calls.

**Cost, if authorized.** The solving core is the expensive part of this section's Large grade. Main technical risks: the current one-hop limit exists to prevent circular reasoning and stale facts; a multi-fact solver must reproduce both protections in general form or it proves things that aren't true. To be clear about scale: each *individual* proof problem is small (one guard, a handful of rules, one assignment), which is good for compile speed — but that does **not** shrink the build. The solver core is expensive to construct regardless of how small each run is.

**Verdict (§1b): OPEN — owner decision.** Not slotted into the MVP or any phase by this document. It enters the roadmap only after the owner weighs the domain need (the corpus shows none, but the corpus is not comprehensive), rules on the spec override, and settles the certificate question below.

**The certificate requirement — binds both §1a and §1b (spec-legality, owner-gated).** The spec rejects opaque solvers on principle: proof reasoning must be legible, and proof witnesses must be structured data — *"SMT/Z3 solvers are excluded even when they could prove more"* (`docs/language/precept-language-spec.md:225`, Principle 3). Any linear-arithmetic strategy — §1a or §1b — must therefore ship *with* a legible, independently-checkable justification, or it violates that ban outright. The known-good form is a Farkas-style certificate: the verdict names which stated facts it used and the non-negative multipliers that combine them, so anyone can re-check the arithmetic without re-running the engine (unlike an opaque solver trace, this is genuinely inspectable). Concretely: **§5(a)'s justification format is a hard precondition for §1 being spec-legal at all — not a build-ordering nicety** (see §5). And one open item rides on top: **whether the Farkas-certificate criterion in fact satisfies the opaque-solver ban is itself an owner ratification, not a settled fact.**

## 2. Reasoning case-by-case (guards, min/max, if/then/else)

**What it proves.** When a definition already names its cases — the `when` condition on a handler row, the two arms of an `if … then … else`, the two sides of a `min`/`max` — this proof type checks the limit separately in each case, using what each case tells you. "The discount is the requested amount when the request is 50 or less, otherwise it's exactly 50" is provably within a 0–50 band *because* of the case split; no single formula shows it.

**What gets rejected without it.** Under the proposed design this is rejected — the compiler can't see that the `if` condition keeps the first arm under the cap (synthetic example, in the conventions of `samples/travel-reimbursement.precept`):

```precept
field DiscountPercent as decimal default 0 min 0 max 50

event ApplyDiscount(Requested as decimal nonnegative)

from Active on ApplyDiscount
    -> set DiscountPercent = if ApplyDiscount.Requested <= 50 then ApplyDiscount.Requested else 50
    -> no transition
```

**Today: already rejected** (PRE0078) — the constant-band check is live, and the engine merges the two arms of the `if` without ever assuming the condition inside each arm, so the result is treated as unbounded (verified by compile: check lands Unresolved with computed range unbounded; the merge-without-assumption behavior is `ProofEngine.Intervals.cs:135–140`).

Two more shapes fail the same way today, verified by compile: rewriting the cap as `min(ApplyDiscount.Requested, 50)` proves the *upper* limit but not the lower — the `nonnegative` on the event input doesn't flow into the range, so the computed range comes back as "lowest possible decimal .. 50" (the engine literally reports the decimal type's absolute minimum as the floor, i.e. unbounded below). And putting the condition on the handler row (`when ApplyDiscount.Requested >= 0 and ApplyDiscount.Requested <= 50`) doesn't help either, because guard narrowing works for **fields** but not for **event inputs** — the identical guard over a plain field proves clean ([0..50], Proved), the event-input version stays unbounded.

**How the author clears it today.** Replace the case logic with `clamp`, which the engine understands directly:

```precept
from Active on ApplyDiscount
    -> set DiscountPercent = clamp(ApplyDiscount.Requested, 0, 50)
    -> no transition
```

Verified: compiles clean today — the check is Proved with computed range [0..50] (`clamp`'s range rule, `Functions.cs:44–149`). Grade: **TRIVIAL** — *when the case logic is really just a cap or floor*. When each case carries a different business formula (e.g. `samples/insurance-claim.precept:127`: "if the fraud flag is set, approve at most half the claim; otherwise approve the requested amount"), there is no single clamp — the author must restructure into separate handler rows and route the value through a plain field first (since event-input guards don't narrow). That shape grades **DUPLICATE-A-FORMULA**.

**Cost to build.** Most of this already exists and runs: guard-splitting into and/or branches with each branch proving independently (`ProofEngine.Strategies.cs:794`); guard-narrowed per-field ranges (`ProofEngine.Intervals.cs`, the `BuildNarrowedIntervals` builder); range rules for `min`/`max`/`clamp`/`abs`/`floor`/`ceil`/`round` (`Functions.cs:44–149`). The genuinely new delta is two pieces: (a) assuming the `if` condition inside each arm — exactly Slice 5.2 of the readiness plan, scoped but unbuilt (`docs/Working/compiler-readiness-plan-2026-06-16.md:1003–1020`); and (b) extending guard narrowing to event inputs, which today covers only fields (verified by the compile probes above). Both reuse the existing branch-splitting and range machinery wholesale. Calendar dates ride along as far as they're modeled as numbers (as the samples do with day counters, e.g. `samples/library-book-checkout.precept`); a true date-range domain would be extra. Main risk: branch counts multiply with deeply nested and/or guards (the splitter is exponential in nesting, `ProofEngine.Strategies.cs:794`), bounded in practice by guard size. Grade: **Small**.

**Benefit.** The single most common structure in real definitions. Across the 77 files: **288** handler rows carry a `when` guard, **63** of those compare values numerically, **28** `if … then … else` expressions (**4** computing a numeric field — `samples/insurance-claim.precept:127,156`, `samples/event-registration.precept:62,72`), and **5** `min`/`max`/`clamp`/`abs` calls (all grep counts). Guards and case expressions are *how domain authors natively write caps and tiers*. Without this delta, prove-or-reject forces every conditional cap into a `clamp` rewrite (loses the author's phrasing) or a duplicated-formula restructure — and the insurance-claim fraud split has no clamp form at all.

**Verdict: MVP.** Case logic is the corpus's most common idiom for keeping values in bounds; an engine that can't read the cases the author already wrote rejects natural, correct definitions wholesale — and most of the machinery is already built, so this is cheap.

## 3. Products and division (especially money)

**What it proves.** When a value is computed by multiplying or dividing other fields — `TotalCost = AvgCost × Quantity` — the compiler proves the result can never land outside the limit declared on the result field. For division it additionally proves the divisor can never be zero. It works by taking the lowest and highest value each ingredient field is allowed to hold and checking every extreme combination against the limit.

**What gets rejected without it.** This is `samples/Test.precept:1–10` verbatim:

```precept
precept TotalCostInvariant

field AvgCost as money in 'USD' editable optional
field Quantity as integer editable optional
field TotalCost as money in 'USD' max '1000 USD' optional

event testEvent
on testEvent
    when Quantity is set and AvgCost is set
    -> set TotalCost = AvgCost * Quantity
```

Nothing limits `AvgCost` or `Quantity`, so the product can be anything — the compiler cannot show it stays under $1,000 and rejects. **Today's compiler already rejects this** (verified: PRE0078). The *rejection* half is live; what's missing is the *acceptance* half for money.

**How the author clears it today.** Split grade, verified by compiling both shapes:

- **Plain numbers (decimal, integer): TRIVIAL.** Put limits on the ingredients so the extremes multiply out under the cap — declared bounds or the same limits as guards. Both compile clean today; the proof record shows the product proved at exactly [0..1000].
- **Money and other unit-carrying types: CAN'T-BE-DONE-BY-HAND.** For the case above, every fix an author could write fails today: declared limits on the money fields are ignored by the product math (still PRE0078); the same limits as guards are ignored; guarding on the product itself (`when AvgCost * Quantity <= '1000 USD'`) doesn't clear it; and clamping with `min(AvgCost * Quantity, '1000.00 USD')` fails because a money amount isn't accepted there (PRE0052). The only way out is to *delete the `max` from `TotalCost`* — abandoning the guarantee, not clearing it. (Independent verification confirmed the gap is exactly this narrow: a bare money value passed straight into a bounded money field *does* prove — money limits flow; only the `×`/`÷` range math is unwired for money, covering Integer/Decimal/Number only.)
- **Division safety: TRIVIAL, already fully built.** Mark the divisor `positive` (or `nonzero`). Verified: `LodgingTotal / TripDays` is rejected (PRE0083) without the modifier and compiles clean with it — exactly how the real sample handles it (`samples/travel-reimbursement.precept:13,58`).

**Cost to build: Small.** The hard part exists. The range type already multiplies and divides by checking all four extreme corners, safely handling unbounded ends and could-be-zero divisors (`NumericInterval.cs:85–102`); the containment check, the check triggered on writing a bounded field, and the guard-narrowing machinery all run today (proved end to end by the decimal probes above). The new work is one wiring gap: feeding declared and guard-narrowed ranges of *money and other unit-carrying fields* into the same product math. A magnitude normalizer for money/quantity already exists to build on (`TypedConstantNormalizer.cs`). Main risk: currency/unit scaling when the two sides carry different units (e.g. `'USD/h' × hours`, `samples/equipment-lease-agreement.precept:212`). A known conservatism can stay: the math treats `X × X` as two independent fields — safe, occasionally stricter than necessary.

**Benefit: high — the most common computation shape in the corpus.** 45 `set` actions compute a product or quotient (15 files — invoice totals, lease values, cart discounts, yield percentages, proration credits), and 22 rules/ensures/guards compare a product or quotient against a business cap (the 3×-income rule, `loan-application.precept:38`; the 3×-rent ensure, `apartment-rental-application.precept:84`; the 25%-deposit rule, `event-venue-booking.precept:100`). Almost all 45 computed targets are money-typed — precisely the case with *no* hand-fix, which is why sample authors currently leave limits off computed money totals entirely. 17 of the 45 are divisions, and every one already carries the `positive`/`nonzero` divisor discipline.

**Verdict: MVP.** The rejection side already fires on every bounded money product today, and there is genuinely no hand-fix — adopting prove-or-reject without this would force authors to strip limits off their most common computed fields. This is the one item on the list an author literally *cannot* clear today, and the fix is small.

## 4. Telling the author exactly what to fix

**What it proves.** Nothing new by itself — this is the reporting half of prove-or-reject. When the compiler rejects a definition because a computed value could escape its limit, this makes the rejection say two things in plain terms: the exact condition on your inputs that would make the formula safe, and one concrete set of input values that actually breaks the limit, so you can see the failure instead of taking the compiler's word for it.

**What gets rejected without it.** This shape is *already rejected today*; what's missing is the explanation. Synthetic example, in the conventions of `samples/invoice-line-item.precept`:

```precept
precept RateCard

field BaseRate as decimal default 0 nonnegative max 50 maxplaces 2 editable
field SurchargePercent as decimal default 0 nonnegative max 30 maxplaces 2 editable

field EffectiveRate as decimal max 60 <- BaseRate * (1 + SurchargePercent / 100)
```

Today's rejection reads, verbatim (verified by compile): *"PRE0078: Numeric computation exceeded the representable range on field 'EffectiveRate'."* It names neither the declared limit (60), nor the range the formula can actually reach, nor which input drives it — even though the engine has already computed the answer internally: the compile result's proof record carries "computed value must be within declared bounds [−∞ .. 60]" with a computed range of **[0 .. 65.0]** (stored per check at `ProofLedger.cs:31`, filled in at `ProofEngine.cs:141`). The numbers exist; the author never sees them. With this capability the same rejection would read, roughly: *"EffectiveRate can reach 65.0, above its max of 60. Safe when BaseRate × (1 + SurchargePercent/100) ≤ 60 — for example, BaseRate 50 with SurchargePercent 30 produces 65."*

**How the author clears it today.** Redo the compiler's arithmetic by hand: work out the worst case (50 × 1.3 = 65), decide which side to tighten, invert the limit (60 ÷ 1.3 ≈ 46.15). Then the one-token fix:

```precept
field BaseRate as decimal default 0 nonnegative max 46 maxplaces 2 editable
```

Verified: compiles clean — check Proved, computed range [0 .. 59.8]. Grade: **DUPLICATE-A-FORMULA** — the edit is one token, but finding it means hand-evaluating the formula at its corners and inverting it, precisely the work the compiler already did and threw away. Real authors already do this dance: `samples/shopping-cart.precept:45–47` carries a comment explaining that two limits were rewritten as rules specifically because "the proof engine cannot narrow" the inputs — a workaround documented in prose because the rejection didn't say what would satisfy it.

**Cost to build.** Two halves with very different price tags. This section is the single owner of counterexample pricing — the linear-relationships section deliberately does not double-count it.

- *Concrete breaking example* — running start. The per-field narrowed range is already computed and stored on every failed check (`ProofLedger.cs:31`, `ProofEngine.cs:141`), the failing intersection is already localized (`Intervals.cs:646`), and a formula renderer for messages exists (`ProofEngine.cs:986`). Picking real values out of those stored ranges and printing them is new but sits on finished plumbing. **Small–Medium.**
- *Exact safe condition* — genuinely new. Nothing in the engine works backwards from a limit to a condition on the inputs (confirmed absent; the only backward-flavored pieces are small local moves like guard negation, `Intervals.cs:819`). Deriving a readable condition through every operator in a formula is fresh machinery, and the main risk is that for formulas with several interacting inputs the printed condition becomes an unreadable wall rather than a fix. **Large** on its own.

Staged (numbers-and-example first, derived-condition later), the MVP slice is **Small–Medium**.

**Benefit.** This bites on every single rejection, because it *is* the rejection's usability. Exposure surface: 246 bounded numeric field declarations across 66 of 77 files, 35 computed-field formulas across 14 files, and 149 rules — each one a check that, when unprovable, becomes a hard stop. Without this, every stop hands a business author "exceeded the representable range" and a reverse-engineering session. The value grows as prove-or-reject expands: Phase 5 of the readiness plan makes today-inert limits start generating rejections (BUG-020, `docs/Working/bugs.md:188`), so rejection volume goes up.

**Verdict: MVP (the cheap half).** A prevention engine whose primary author is a domain expert cannot ship rejections that name neither the limit, the reachable range, nor a breaking input. Surface the already-computed numbers plus one sampled example at minimum; the derived safe-condition printing is a later, larger phase.

---

# Prerequisite (build-ordering)

## 5. Showing its work (a checkable justification for every verdict)

**What it proves.** Nothing new by itself — this is shared machinery underneath every other proof type. It makes the compiler attach a short, plain-language justification to every "proved" and every "rejected" verdict, and (in its full form) adds an independent double-check that re-reads each justification and confirms it holds. The compiler never just says "trust me" — in either direction.

A note on the label: this is a **prerequisite in the build-ordering sense** — the justification *format* should be laid down before the new proof types above are built, because retrofitting "emit your reasoning" under strategies after the fact costs strictly more than building it in. That is a different claim from "authors can't ship without it" — they can.

For §1 specifically, the claim is stronger than build-ordering. The spec rejects opaque solvers on principle — proof witnesses must be structured data, and *"SMT/Z3 solvers are excluded even when they could prove more"* (`docs/language/precept-language-spec.md:225`, Principle 3). A linear-arithmetic strategy (§1a or §1b) that cannot show a legible, independently-checkable justification — a Farkas-style certificate naming the facts used and the non-negative multipliers combining them — violates that ban outright. So half (a) here is a **hard precondition for §1 being spec-legal at all**, not merely cheaper done first (details in §1). Whether the certificate criterion *satisfies* the opaque-solver ban is an owner ratification, still open.

**What gets rejected without it.** Nothing extra — this changes what a verdict *says*, not which definitions pass. Where it bites: take this real excerpt from `samples/invoice-line-item.precept` with one word added (`nonnegative` on the last field, synthetic addition):

```precept
field UnitPrice as money in '{CurrencyCode}' default '0 {CurrencyCode}' nonnegative editable
field DiscountPercent as decimal default 0 nonnegative max 100 maxplaces 2 editable

field Subtotal as money in '{CurrencyCode}' <- UnitPrice * Quantity
field DiscountAmount as money in '{CurrencyCode}' <- Subtotal * DiscountPercent / 100
field TaxableAmount as money in '{CurrencyCode}' nonnegative <- Subtotal - DiscountAmount
```

Under the proposed design this is rejected — the range math treats `Subtotal` and `DiscountAmount` as independent, so it cannot see that a ≤100% discount never exceeds the subtotal. (Today: compiles clean — that posture isn't built; verified live, the compiler generates zero checks for that `nonnegative`.) Without show-your-work, the rejection reads "cannot prove TaxableAmount is nonnegative" — and the author, who *knows* the math is safe, cannot tell whether the compiler found a real hole or is just being cautious. With it, the verdict carries the chain: "DiscountPercent may reach 100; I track Subtotal and DiscountAmount as separate ranges; so their difference could, as far as I can see, go below zero." Now the author knows exactly which link to attack. Accepted definitions get the same treatment: today a pass reports only the *name* of the strategy that proved it (observed live: "Proved — DeclarationAttribute — Divisor must be non-zero"), not reasoning anyone could re-check.

**How the author clears it today.** Grade: **CAN'T-BE-DONE-BY-HAND** — there is no `.precept` construct that supplies, demands, or displays a justification. The author's only substitute is re-deriving the compiler's reasoning mentally from the one-line diagnostic and the docs. (The *underlying* rejections each have hand-fixes in their own sections; this fixes the missing explanation, which has none.)

**Cost to build — split grade.** Much raw material exists: the compiler already records, per check, pass/fail, which strategy fired, the computed numeric range, and a link to the exact spot in the file (`ProofLedger.cs:24–31,117`), and the language-server/MCP tools already consume those records. A formula pretty-printer exists (`ProofEngine.cs:986`). All 11 strategies flow through one dispatch point (`ProofEngine.cs:1054`), so there's a single seam to wire.

- *(a) Justification emission* — each existing strategy (`docs/compiler/proof-engine.md:710–1727`) emits its actual reasoning steps, in a written-down format, surfaced through the existing records: **Medium**.
- *(b) The independent re-checker* — honestly priced, this is **Large**, not a bolt-on. A checker that independently confirms each justification must cover all 11 strategies' claim shapes — range arithmetic, sign reasoning, field-relation facts, currency/unit matching, dimensional products, count and length containment. That is a second checking implementation; the inventory confirms a serialized proof format and a separate re-verifier are entirely new (`ProofLedger` records *what* was proved and *by which* strategy, not a machine-recheckable proof).
- Standing cost either way: every future proof strategy inherits an "emit your reasoning" obligation — a permanent tax on all later proof work. That tax is the point, but it's real and it never ends.

**Benefit.** Rides on every proof: 14 files carry 33 computed-field formulas, 149 rule/ensure lines, roughly 100 declared min/max limits — every one a proof site whose verdict this makes checkable. The hand-work it removes is the re-derivation loop on every disputed rejection. In a solo-dev, pre-release setting the deeper benefit of half (b) is to the *builder*: an independent re-check that fails loudly whenever a strategy drifts is the cheapest standing defense against the engine quietly accepting something it didn't actually prove.

**Verdict: PREREQUISITE — for half (a) only.** Lay the justification format down before the MVP proof types land, so the new strategies emit reasoning from day one instead of being retrofitted — and because, for §1, a linear strategy without a legible certificate is not spec-legal at all (`precept-language-spec.md:225`; see §1's certificate requirement). The independent re-checker (b) is valuable but Large and separable — recommended as a later phase, not a gate.

---

# Recommended upgrade (first after MVP)

## 6. Letting a rule the author already wrote do the work

**What it proves.** When you have already declared a rule — "the security deposit never exceeds three months of payment" — a later calculation should be able to lean on that rule directly. The engine takes the fact you stated and applies it where it's needed; it does not ask you to restate it.

**What gets rejected without it.** Half of this is already built: if your rule compares two plain fields (`rule Deposits > Withdrawals`), today's engine already uses it — a division by `Deposits - Withdrawals` proves safe against exactly that rule (verified live: "Divisor must be non-zero" **Proved**, via the shipped rule-matching step, `ProofEngine.Strategies.cs:1078`). 88 of the corpus's 149 rules have that shape.

What is *not* built: rules where either side has arithmetic — a multiplier, a sum, a percentage. The moment you write `MonthlyPayment * 3` instead of a bare field name, the rule becomes invisible to the proof, even when a later calculation is word-for-word the same expression. Synthetic example, modeled on the real rule at `equipment-lease-agreement.precept:78`:

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

The rule guarantees the headroom is never negative — the author already said so. Under the proposed design, the write into the `nonnegative` field must be proven, the rule can't be applied, and the definition is rejected. *(Today: compiles clean — that check isn't generated yet; verified by compile, zero errors.)* And where checks are already live today, this gap already rejects real shapes: divide by `Deposits - Withdrawals * 2` under the rule `Deposits > Withdrawals * 2` fails **today** with PRE0083, even though the rule states the divisor is positive verbatim (verified by compile). Writing the comparison into the row's `when` guard doesn't help — same PRE0083 (verified; guards go through the same bare-fields-only reading, `ProofEngine.Strategies.cs:1240`).

**How the author clears it today.** Introduce a mirror field holding the arithmetic, keep it up to date at every write site, and restate the rule in bare field-vs-field form:

```precept
field DepositCap as decimal nonnegative maxplaces 2 default 3.0

rule SecurityDeposit <= DepositCap because "Security deposit cannot exceed the deposit cap"

from Active on SetPayment
    -> set MonthlyPayment = SetPayment.Monthly
    -> set DepositCap = SetPayment.Monthly * 3    # must repeat the formula here, forever
    -> no transition
```

Verified: compiles clean today, and in the live division variant the check flips from Unresolved to **Proved** (compile probes). A `max(0.0, …)` clamp also compiles but papers over the guarantee rather than using it — and can't fix the division case at all. Grade: **DUPLICATE-A-FORMULA** — the formula now lives in the rule's meaning *and* in every event row that touches its inputs, and nothing stops the copies drifting apart.

**Cost to build.** Substantial reuse: the match-a-declared-rule-to-a-check step exists and runs for bare fields (`Strategies.cs:1078`, rule-sourced facts at `:1159`, unconditional-rule collection at `Composition.cs:539`), and the staleness discipline — a fact stops being usable once one of its fields is rewritten mid-flow — is already enforced (`ProofEngine.cs:409`). New: the rule-reading step must accept arithmetic on either side (today strictly `field compare field`, `Strategies.cs:1240`), and matching must recognize more than the one subtraction shape it handles now (`Strategies.cs:1277`). Deliberately *not* needed: no equation solver, no chaining of multiple rules — this stays "apply one stated fact where it appears," the same one-hop discipline the engine already enforces (`Intervals.cs:629`). Main risk: scope creep — deciding how much rearranging (`A <= B*3` vs `B*3 - A >= 0`) still counts as "applying what was written" before it quietly becomes an equation solver, a different and much larger project. Grade: **Medium.**

**Benefit.** 22 of the 149 rules in the corpus (15%) carry arithmetic in the comparison and are invisible to proof today (grep: 149 total; 88 bare field-vs-field; 22 with `*`, `+`, or `-`). Fourteen of the 22 are money/count caps of exactly the kind later calculations lean on — deposit caps, debt ceilings, refund limits (`SecurityDeposit <= MonthlyPayment * 3`, `ExistingDebt <= AnnualIncome * 3.0`, `RefundAmount + ForfeitAmount <= AmountPaid`); the other eight are date-window rules. Every one that a calculation touches costs the author a mirror field plus a repeated formula at every write site — a standing invitation for the copies to drift. This is also the proof type that most directly honors the product's promise: state the business fact once, and it does the work.

**Verdict: RECOMMENDED UPGRADE — first after MVP.** Not a prerequisite in the build-ordering sense: prove-or-reject can ship without it, since the mirror-field fix genuinely compiles today. But flipping prove-or-reject on turns one in seven of the corpus's rules into a demand to duplicate a formula — exactly the scattered-logic disease the product exists to cure — so it should follow the MVP closely.

---

# Later phases

## 7. Aggregates over a collection (sum, count, average)

**What it proves.** That a total, count, or average taken over a collection stays within a declared limit — "the units across all components never exceed 5,000" — by combining what's already known: how many items the collection can hold, and what each item's value is allowed to be. It also keeps a hand-maintained running total honest: when every add and remove adjusts the total, the compiler confirms the total genuinely tracks the collection instead of trusting the author.

**What gets rejected without it.** Synthetic example, modeled on the real bill-of-materials sample (`samples/bill-of-materials-management.precept:35,109`). Today's compiler already rejects the write (PRE0078); what's missing is the reasoning that would let this *safe* definition pass. Note the example carries **two independent checks**: the collection's `maxcount 50` (guarded by the reject row) and the numeric total's `max 5000` (the one at issue):

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

This is mathematically safe — at most 50 items of at most 100 units each is exactly 5,000 — but the compiler cannot connect "at most 50 items" to "the total of their quantities is at most 5,000." It sees only "some number plus up to 100," computes 1 to 5,100, and rejects. A second gap sits in front of this one: you cannot ask for a collection's total at all — there is no `.sum` or `.average`, deliberately held back from the language (`docs/language/collection-types.md:860`), which is exactly why every sample keeps a separate running-total field by hand.

**How the author clears it today.** Guard the row so *both* limits provably hold: keep a count guard for the collection cap, and add a hand-derived headroom guard for the total — the author subtracts the biggest allowed item (100) from the limit (5,000) in their head:

```precept
from Draft on AddComponent when ComponentPartNumbers.count < 50 and TotalComponentUnits <= 4900
    -> add ComponentPartNumbers AddComponent.PartNumber
    -> set TotalComponentUnits = TotalComponentUnits + AddComponent.Quantity
    -> no transition
from Draft on AddComponent
    -> reject "BOM is full or adding more units would push the total past 5000"
```

Verified: this two-guard form compiles clean today — both checks Proved (the total at computed range 1 to 5,000). A version with *only* the headroom guard does **not** compile: it fails PRE0136 on the unguarded collection cap ("Cannot prove `ComponentPartNumbers` stays within `maxcount 50` after this add"). What also does *not* work: the natural guard `when TotalComponentUnits + AddComponent.Quantity > 5000 -> reject` — verified still rejected, because the prover can't use a guard mixing two values with arithmetic (that's the linear-relationships gap — the single-fact core, section 1a). Grade: **DUPLICATE-A-FORMULA**, and heavier than it first looks — the author maintains **two coupled guards** (a count guard *and* a pre-computed `4900` headroom), repeated on every row that touches the total, both re-derived by hand whenever the limit or the per-item cap changes.

**Cost to build.** Two separable pieces. (a) *The bound-transfer rule* — "total lies between count-times-smallest and count-times-largest" — reuses the existing range arithmetic (`NumericInterval.cs:73–108`), the already-built item-count tracking (`ProofEngine.cs:579–671`, proved live above), and the existing extraction of per-item limits (`Intervals.cs:279,304`). The transfer rule itself is new — the machinery explicitly refuses to carry item limits through anything that isn't a plain item read (`Intervals.cs:84–97`). (b) *Running-total consistency* — recognizing that `Total = Total + item` paired with `add` preserves "total equals sum of items" — is new proof logic, though it mirrors the shipped count-tracking pattern step for step. Main risk: neither piece can ship until `.sum`/`.average` enter the language, and that surface was deliberately held back as a language-design decision (`collection-types.md:860`) — a decision gate for the owner, not just engineering, and one that must go through the language-design process. Proof work alone: **Medium**. With the language surface: **Large**.

**Benefit.** The hand-maintained running total is everywhere: 179 accumulation sites across 40 of 77 files (corpus grep), 12 of them fields literally named *Total*. The bill-of-materials sample maintains one total across three separate rows (add at line 109, update at 114, remove at 123) — one wrong row and the total silently drifts from the collection, exactly what this proof would catch. Zero samples use an aggregate accessor, because none exists. High reach — but today's authors have a working (if repetitive, two-guard) workaround, and only totals carrying a declared limit trigger the proof at all.

**Verdict: LATER PHASE.** The pattern is everywhere, but it's gated behind a deliberately deferred language-surface decision, and the two-guard hand-fix clears it on today's engine in the meantime. Note that section 1a (the single-fact linear core) removes the *worst* part of the hand-fix — the natural arithmetic guard starts working — which lowers the urgency here further. Forward note: this section is the gating dependency for Phase 6 — if aggregates are ever wanted, the `.sum`/`.average` language decision must enter the language-design process on its own timeline. Nothing here sketches or pre-decides that syntax; the decision remains the owner's.

## 8. Square roots, powers, rounding — and modulo

**What it proves.** When a formula takes a square root, raises a value to a whole-number power, or rounds a number, the compiler can work out the tightest possible range of the result from the range of the input — because these functions never "jump around": a bigger input never produces a smaller output. That lets it confirm the result stays inside the field's declared band.

**Status split — half already exists.** Rounding is done: `floor`, `ceil`, `truncate`, and `round` already carry hand-written result-range rules (`Functions.cs:108–155`), and the end-to-end proof works today — a computed `integer min 0 max 10 <- round(Rate)` over a `Rate` banded [0..9.9] compiles clean, computed range [0..10] (verified by compile). The gap is `sqrt` and `pow`: both are declared in the catalog (`Functions.cs:183–213`) but carry **no result-range rule**, so their output is treated as "could be anything." **Modulo (remainder) is the same story**: no result-range rule anywhere (`Operations.cs` — no `%` entry carries a transfer), so its result is treated as unbounded even though a remainder is inherently bounded (a value `% n` lands between 0 and n−1); the fix is the same catalog-shaped rule.

**What gets rejected without it** (today's engine already rejects this the same way — the posture is live for this case). Synthetic — no shipped sample uses `sqrt`:

```precept
precept SafetyStockPolicy

field DemandVariance as number default 25 nonnegative min 0 max 400

# Square-root-of-variance safety-stock heuristic.
# sqrt of [0..400] is truly [0..20] — but the compiler can't see that.
field SafetyStock as number min 0 max 20 <- sqrt(DemandVariance)
```

Verified: fails PRE0078 because the sqrt result is treated as unbounded, even though the true range [0..20] fits exactly. (The separate "input must be non-negative" check works today — without `nonnegative` it also fails PRE0084.)

**How the author clears it today.** Wrap in `clamp`, doing the endpoint math yourself:

```precept
field SafetyStock as number min 0 max 20 <- clamp(sqrt(DemandVariance), 0, 20)
```

Verified: compiles clean, all checks Proved, computed range [0..20] (`clamp`'s rule at `Functions.cs:91–105` absorbs the unknown inner range). Grade: **DUPLICATE-A-FORMULA** — one line, but the author must know that √400 = 20 and restate it, exactly the arithmetic the compiler should do. And `clamp` doesn't *check* — it silently clips, so wrong hand-math still compiles and quietly caps values.

**Cost to build: Small.** The delivery mechanism exists and is proven: each function carries an optional result-range rule (`Function.cs:31`), consumed generically (`Intervals.cs:104,124`), with four shipped rounding functions plus `abs`/`min`/`max`/`clamp` as the direct template (`Functions.cs:44–155`). The work is three more rules: `sqrt` (map the two endpoints; input already proven non-negative), `pow` (the fiddly one — an even exponent on a range straddling zero needs the sign-split treatment `abs` already demonstrates at `Functions.cs:74–86`; a non-constant exponent must conservatively give up), and modulo. No new strategy, no engine changes. Main risk: `pow`'s corner cases — mitigated by the corner-evaluation precedent in `NumericInterval.Multiply` (`NumericInterval.cs:85–90`).

**Benefit.** Low frequency today: across all 77 files in `samples/`, grep finds **zero** call sites of `sqrt`, `pow`, `round`, `floor`, `ceil`, or `truncate` (the only "floor(" hit is prose in a comment, `loan-application.precept:94`); the math functions actually used are `min`/`max` — 5 call sites in 4 files — which already have rules. The real benefit is forward-looking coherence: under prove-or-reject these functions are unusable in any banded field without the clamp workaround — shipping them broken-by-default is a wart even if the corpus doesn't hit it yet.

**Verdict: LATER PHASE.** Zero corpus usage and a one-line (if slightly dishonest) hand-fix mean it blocks no one now — but it's cheap, pattern-following catalog work; do it when convenient.

---

# Skip

## 9. Squared / statistical quantities (like variance)

**What it proves.** When a value is multiplied by itself — a squared deviation, a variance — the result can never be negative, even when nobody knows whether the underlying value is positive or negative. This teaches the engine to notice "both sides of this multiplication are the same thing, so the result is at least zero."

**What gets rejected without it.** Today the engine estimates each side of a multiplication separately and never notices the sides are identical (`NumericInterval.cs:90` — a value spanning −1..1 times itself is estimated as −1..1, when the truth is 0..1). Under the proposed design, writing a squared deviation into a field declared `nonnegative` would be rejected (synthetic example matching `samples/statistical-process-control.precept` conventions):

```precept
precept ProcessVariance

field TargetValue as decimal default 0
field SquaredDeviation as decimal nonnegative default 0

state Monitoring initial

event SetTarget(Target as decimal)
event RecordSample(Value as decimal)

from Monitoring on SetTarget
    -> set TargetValue = SetTarget.Target
    -> no transition

from Monitoring on RecordSample
    -> set SquaredDeviation = (RecordSample.Value - TargetValue) * (RecordSample.Value - TargetValue)
    -> no transition
```

(Today: compiles clean — that check isn't generated yet; verified by compile, zero diagnostics. Today's engine only checks the field's *default* against `nonnegative`.)

**How the author clears it today.** Wrap each side in `abs(...)`. Mathematically identical (|d| × |d| = d²), and it hands the engine exactly what it understands: `abs` guarantees a result of at least zero (`Functions.cs:44–149`), and multiplying two known-non-negative ranges stays non-negative (`NumericInterval.cs:85–102`):

```precept
    -> set SquaredDeviation = abs(RecordSample.Value - TargetValue) * abs(RecordSample.Value - TargetValue)
```

Verified: compiles clean today (0 diagnostics). The same rewrite also helps with *upper* limits when the inputs carry declared bands. Grade: **TRIVIAL** — a one-line exact rewrite, no duplication, no new rules.

**Cost to build.** The narrow version — "both sides of a `*` are the identical expression, so the result is ≥ 0" — is one new step in the strategy cascade (`ProofEngine.cs:1054`) plus one name (`ProofLedger.cs:100`): **Small**. The full version — recognizing squares hidden inside rearranged formulas, or sums of squared terms — needs genuine formula-rearrangement machinery that does not exist in any form (no equation-solving code anywhere in `src/Precept`); that tail is **Medium-to-Large**, with scope creep toward general polynomial reasoning as the main risk.

**Benefit.** Zero occurrences: across all 77 files, no file computes a square, variance, or squared deviation (`grep -iE 'squar|varianc|deviat'` → 0 hits outside one prose comment; `grep '\) *\* *\('` → 0 hits). 22 files use multiplication, every one multiplying two *different* quantities — shapes the existing math already handles. Even the flagship statistics sample takes its control limits as inputs rather than computing them.

**Verdict: SKIP.** Zero incidence in 77 real definitions plus a trivial exact hand-fix buys almost nothing; revisit only if computed statistical quantities become a real authoring pattern.

---

# Roll-up: what to build, in what order

*Everything below is recommendation, not decision — none of it is owner-ratified.*

## The MVP

The smallest set needed to turn prove-or-reject on without it rejecting the corpus's own house style. Strictly one item is a genuine "author cannot clear it by hand at all today": **money product wiring** (§3 — every attempted hand-fix fails; the only escape is deleting the guarantee). The other three are MVP because the hand-escapes either silently weaken the guarantee at ~180 sites (§1a), force rewrites of the corpus's most common idiom (§2), or leave every rejection unexplainable (§4):

1. **Money/unit ranges through × and ÷** (§3) — Small; the one true cannot-clear.
2. **Case-by-case reasoning** — if-arm assumption + event-input guard narrowing (§2) — Small; mostly built already (Slice 5.2 is scoped).
3. **Rejections that show the numbers + one breaking example** (§4, cheap half) — Small–Medium; the data is already computed and stored (`ProofLedger.cs:31`).
4. **Linear relationships — the single-fact core (§1a)** — the biggest remaining MVP build (the section's combined Large grade is dominated by the §1b solving core, which is *not* slotted here); prove-or-reject rejects 42 of 77 sample files' central idiom without it. Most of its 179 sites bite only after Phase 5 lands obligation generation (see §1a's sequencing caveat).

**Not slotted in the MVP by this document: §1b (multi-fact combination).** Whether §1b belongs in the MVP, a later phase, or is built at all is an open owner decision — it requires an owner-authorized override of the locked depth-bound (`precept-language-spec.md:256`), and its need is weighed on domain grounds (the corpus shows no case, but the corpus is not comprehensive; feasibility pending).

Plus the groundwork: **the justification format** (§5, half (a), Medium) laid down *before* items 1–4 land — both because retrofitting costs strictly more, and because for §1a a linear strategy without a legible certificate violates the opaque-solver ban (`precept-language-spec.md:225`); whether the certificate criterion satisfies that ban is an owner ratification, still open.

## Prerequisites vs later ergonomic upgrades — the full split

| Item | Class | Why |
|---|---|---|
| Justification format + reasoning emission (§5a) | **True prerequisite** (build-ordering + spec-legality for §1) | Retrofitting under 11+ strategies costs strictly more than building it in first; and any linear strategy without a legible certificate violates the opaque-solver ban (`precept-language-spec.md:225`) — whether the certificate criterion satisfies the ban is an owner ratification, open |
| Money product wiring (§3) | **MVP** | No hand-fix exists at all |
| Case-by-case (§2), rejection explanations (§4 cheap half), linear single-fact core (§1a) | **MVP** | Hand-fixes exist but weaken guarantees or reject the house style at scale |
| Multi-fact combination (§1b) | **Open — owner decision** | No corpus case demonstrated, but the corpus is not comprehensive — need weighed on domain grounds, feasibility pending; requires an owner-authorized override of the locked depth-bound (`precept-language-spec.md:256`) |
| Rules with arithmetic (§6) | **Ergonomic upgrade, first in line** | Mirror-field hand-fix compiles today; but 22/149 rules otherwise demand formula duplication — the disease the product cures |
| Independent proof re-checker (§5b) | **Ergonomic/assurance upgrade** | Large (a second checking implementation covering all 11 strategy shapes); main value is builder-side drift defense |
| Derived safe-condition printing (§4, second half) | **Ergonomic upgrade** | Large; readability risk on multi-input formulas |
| Collection aggregates (§7) | **Ergonomic upgrade, gated** | Blocked on a deliberately deferred language decision (`collection-types.md:860`); two-guard hand-fix works today |
| sqrt/pow/modulo range rules (§8) | **Ergonomic upgrade, cheap** | Zero corpus usage; one-line hand-fix |
| Same-expression squares (§9) | **Skip** | Zero corpus usage; trivial exact hand-fix |

## Phased roadmap (ordered by benefit per cost)

| Phase | What it adds | Cost | Benefit | Worth it? |
|---|---|---|---|---|
| **0** | Keep everything already built: currency/unit matching, length limits, count tracking, division safety, rounding rules, bare-field rules | ~Zero | Already carries 37/77 (money) and 72/77 (length) file coverage | **Recommended** (it's free) |
| **1** | Justification format (§5a) + money product wiring (§3) + case-by-case delta (§2) + rejection numbers-and-example (§4a) | Small×2 + Small–Medium + Medium | Unblocks bounded money math (45 computed sites), the corpus's most common idiom (288 guarded rows), and makes every rejection actionable; §5a is also the spec-legality precondition for Phase 2 (`precept-language-spec.md:225`) | **Recommended — this is most of the MVP at the smallest cost** |
| **2** | Linear single-fact core (§1a): multi-term facts + guard-sum matching, single-hop | The larger share of the MVP builds (the §1b solving core — the expensive part of the section's Large grade — is not included here) | The remaining MVP item: 179 running-total sites across 42 files stop needing guarantee-weakening clamps (most bite only after Phase 5 lands obligation generation); also dissolves the worst of the aggregates hand-fix | **Recommended — MVP-completing; biggest remaining build, budget accordingly.** Spec note: stays inside the locked depth-bound; the spec's fact-shape wording widens to multi-term |
| **TBD** | Multi-fact combination (§1b) — the Fourier–Motzkin/Farkas solving core | Large (greenfield; no linear-arithmetic solver code exists in `src/Precept`) | No corpus case demonstrated; the corpus is not comprehensive — need is an open question on domain grounds, feasibility pending | **Open — owner decision**, both on need and on placement (MVP vs later); requires an owner-authorized override of the locked depth-bound (`precept-language-spec.md:256`) and rides on the certificate ratification (`precept-language-spec.md:225`) |
| **3** | Rules with arithmetic (§6) | Medium | 22 rules (14 money/count caps) work as written; no mirror fields | **Recommended** |
| **4** | Independent re-checker (§5b) | Large | Standing drift defense for the engine itself; verdicts become independently checkable | **Recommended but deferrable** — value is builder assurance, not author unblocking |
| **5** | Derived safe-condition printing (§4b) | Large | Rejections say exactly what condition would fix them | **Defer** — the Phase-1 numbers-and-example half covers most of the need |
| **6** | Collection aggregates (§7) — after the `.sum`/`.average` language decision | Medium (proof) + language-surface work | Removes the two-guard hand-maintenance pattern (40 files) | **Defer** — gated on an owner language decision that must go through the design process first |
| **7** | sqrt/pow/modulo range rules (§8) | Small | Removes "these functions can never pass a bound check"; zero current demand | **Defer** — cheap; do opportunistically |
| — | Same-expression squares (§9) | Small (narrow) / Large (full) | Zero corpus demand; trivial hand-fix | **Skip** |

**One-line summary for the decision:** Phases 1–2 are the MVP — Phase 1 is four small-to-medium items that unblock most of the corpus cheaply; Phase 2 is the biggest remaining build (the single-fact linear core, §1a) without which prove-or-reject rejects the product's own recommended authoring style. Phase 3 is the first upgrade worth taking after that. Everything else can wait, and one item (squares) shouldn't be built at all on current evidence.

**And the standing open calls, restated:** the prove-or-reject disposition itself is not ratified — this whole roadmap is conditional on it; whether §1b's multi-fact solver is built at all (and where) is open — it needs a locked-spec override (`precept-language-spec.md:256`) and a need weighed on domain grounds; and whether the Farkas-certificate criterion satisfies the opaque-solver ban (`precept-language-spec.md:225`) is an owner ratification. None of those are decided here.