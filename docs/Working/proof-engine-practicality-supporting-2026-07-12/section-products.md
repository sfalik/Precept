All data gathered. Writing the section.

## Products and division

**1. What it proves.** When a value is computed by multiplying or dividing other fields — `TotalCost = AvgCost × Quantity` — the compiler proves the result can never land outside the bound you declared on the result field. For division it additionally proves the divisor can never be zero. It does this by taking the lowest and highest value each ingredient field is allowed to hold and checking every extreme combination of them against the bound.

**2. What gets rejected without it (PROPOSED design).** This is `samples/Test.precept:1-10` verbatim:

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

Nothing limits `AvgCost` or `Quantity`, so the product can be anything — the compiler cannot show it stays under $1,000 and rejects the definition. Note: **today's compiler already rejects this** (verified: error PRE0078, "Numeric computation exceeded the representable range on field 'TotalCost'"). The rejection half of this proof type is live; what's missing is the *acceptance* half for money — see below.

**3. How the author clears it today, by hand.** Split grade, verified by compiling both shapes against today's engine:

- **Plain numbers (decimal, integer): TRIVIAL.** Put bounds on the ingredients so the extremes multiply out under the cap — either declared bounds (`field A as decimal min 0 max 10` × `field B as decimal min 0 max 100` into `max 1000`) or the same limits written as guards on the event row. Both compile clean today; the proof ledger shows the product proved at exactly `[0 .. 1000]`.
- **Money and other qualified types: CAN'T-BE-DONE-BY-HAND.** For the Test.precept case above, every fix an author could write fails today: declared bounds on the money field (`min '0 USD' max '10 USD'`) are ignored by the product math (still rejected, PRE0078); the same bounds written as guards are ignored too; even guarding on the product itself (`when AvgCost * Quantity <= '1000 USD'`) doesn't clear it; and clamping with `min(AvgCost * Quantity, '1000.00 USD')` fails because a money amount isn't accepted as a literal there (PRE0052). The only way out is to *delete the `max` from `TotalCost`* — which abandons the guarantee, not clears it.
- **Division safety: TRIVIAL, already fully built.** Mark the divisor `positive` (or `nonzero`). Verified: `LodgingTotal / TripDays` is rejected (PRE0083, "TripDays can be zero") without the modifier and compiles clean with it — exactly how the real sample handles it (`samples/travel-reimbursement.precept:13,58`).

**4. Cost to build: Small.** The hard part exists. The interval type already multiplies and divides ranges by checking all four extreme corners, safely handling unbounded ends and divisors that could be zero (`src/Precept/Language/NumericInterval.cs:85–102`); the containment check, the obligation that triggers on writing a bounded field, and the guard-narrowing machinery all run today (inventory §2; the decimal probes above prove the full chain end to end). The genuinely new work is one wiring gap: feeding the declared and guard-narrowed ranges of *money and other unit-carrying fields* into that same product math — plain decimals already flow, money doesn't. A magnitude normalizer for money/quantity already exists to build on (`src/Precept/Language/Numeric/TypedConstantNormalizer.cs`, used by the interval math per inventory). Main risk: getting currency/unit scaling right when the two sides carry different units (e.g. `'USD/h' × hours` in `samples/equipment-lease-agreement.precept:212`). A known conservatism can stay: the math treats `X × X` as two independent fields, which is safe but occasionally stricter than necessary.

**5. Benefit: high — this is the most common computation shape in the corpus.** Across `samples/*.precept`, 45 `set` actions compute a product or quotient (15 files — invoice totals, lease values, cart discounts, yield percentages, proration credits), and 22 rules/ensures/guards compare a product or quotient against a business cap (the 3×-income rule in `loan-application.precept:38`, the 3×-rent ensure in `apartment-rental-application.precept:84`, the 25%-deposit rule in `event-venue-booking.precept:100`). Almost all of the 45 computed targets are money-typed — precisely the case that today has *no* hand-fix, which is why sample authors currently leave bounds off computed money totals entirely. 17 of the 45 are divisions, and every one already carries the `positive`/`nonzero` divisor discipline this proof type enforces. Building the money wiring converts "can't declare a cap on any computed money total" into "declare it and the compiler proves it."

**6. Verdict: MVP** — the rejection side already fires on every bounded money product (PRE0078 is live today), so adopting prove-or-reject without the money-band wiring would force authors to strip bounds off their most common computed fields; the fix is small because the corner math, containment proof, and division safety are already built.