## Telling the author exactly what to fix

**1. What it proves.** Nothing new, by itself — this is the reporting half of prove-or-reject. When the compiler rejects a definition because a computed value could escape its declared bound, this capability makes the rejection say two things in plain terms: the exact condition on your input fields that would make the formula safe, and one concrete set of input values that actually breaks the bound, so you can see the failure instead of taking the compiler's word for it.

**2. What gets rejected without it.** PROPOSED design — but note this shape is *already rejected today*; what's missing is the explanation, not the rejection. Synthetic example, written to the conventions of `samples/invoice-line-item.precept`:

```precept
precept RateCard

field BaseRate as decimal default 0 nonnegative max 50 maxplaces 2 editable
field SurchargePercent as decimal default 0 nonnegative max 30 maxplaces 2 editable

field EffectiveRate as decimal max 60 <- BaseRate * (1 + SurchargePercent / 100)
```

Today's compiler rejects this with (verbatim, verified via compile): *"PRE0078: Numeric computation exceeded the representable range on field 'EffectiveRate'"*. That message names neither the declared limit (60), nor the range the formula can actually reach, nor which input drives it — even though the engine has already computed the answer internally: the compile result's proof record carries "computed value must be within declared bounds [−∞ .. 60]" with a computed range of **[0 .. 65.0]** (stored per obligation at `ProofLedger.cs:31`, filled in at `ProofEngine.cs:141`). The numbers exist; the author never sees them. With this capability the same rejection would read, roughly: *"EffectiveRate can reach 65.0, above its max of 60. Safe when BaseRate × (1 + SurchargePercent/100) ≤ 60 — for example, BaseRate 50 with SurchargePercent 30 produces 65."*

**3. How the author clears it today, by hand.** The author must redo the compiler's arithmetic themselves: work out the worst case (50 × (1 + 30/100) = 65), decide which side to tighten, and work the limit backwards (60 ÷ 1.3 ≈ 46.15). Then the one-line fix:

```precept
field BaseRate as decimal default 0 nonnegative max 46 maxplaces 2 editable
```

Verified: this compiles clean on today's engine — the proof record shows the containment obligation **Proved** with computed range [0 .. 59.8]. Grade: **DUPLICATE-A-FORMULA** — the edit is one token, but finding it means hand-evaluating the formula at its corners and inverting it, which is precisely the work the compiler already did and threw away. Real authors already do this dance: `samples/shopping-cart.precept:45-47` carries a comment explaining that two bounds were rewritten as rules specifically because "the proof engine cannot narrow" the inputs — a hand-routed workaround documented in prose because the rejection didn't say what would satisfy it.

**4. Cost to build.** Two halves with very different price tags.

- *Concrete breaking example* — running start. The per-field narrowed range is already computed and stored on every failed obligation (`ProofLedger.cs:31`, `ProofEngine.cs:141`), the failing intersection is already localized (`Intervals.cs:646`), and an expression renderer for messages already exists (`ProofEngine.cs:986`). Picking real values out of those stored ranges and printing them is new but sits on finished plumbing. **Small–Medium.**
- *Exact safe condition* — genuinely new. Nothing in the engine works backwards from a bound to a condition on the inputs (inventory: confirmed absent; the only backward-flavored pieces are small local moves like guard negation, `Intervals.cs:819`). Deriving and printing a readable condition through every operator in a formula is fresh machinery, and the main risk is that for formulas with several interacting inputs the printed condition becomes an unreadable wall rather than a fix. **Large** on its own.

Staged sensibly (ship the numbers-and-example half first, the derived-condition half later), overall **Medium**.

**5. Benefit.** This bites on every single rejection, because it is the rejection's usability. The exposure surface in the corpus: 246 bounded numeric field declarations across 66 of 77 sample files, 35 computed-field formulas across 14 files, and 149 rules — each one is an obligation that, when unprovable under prove-or-reject, becomes a hard stop. Without this capability every such stop hands a business-domain author a message like "exceeded the representable range" and leaves them to reverse-engineer the formula by hand (the §3 work, repeated per rejection). With it, most rejections become a copy-the-number edit. The value grows as prove-or-reject expands: the readiness plan's Phase 5 makes today-inert bounds start generating rejections (BUG-020), so the volume of rejections needing explanation goes up, not down.

**6. Verdict.** **MVP** — a prevention engine whose primary author is a domain expert cannot ship rejections that name neither the limit, the actual reachable range, nor a breaking input; the cheap half (surface the already-computed numbers plus one sampled example) is the minimum for prove-or-reject to be adoptable, with the derived safe-condition printing as the later, larger half.