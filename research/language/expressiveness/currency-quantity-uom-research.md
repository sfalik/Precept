# Currency, Quantity, Unit-of-Measure, and Price — Research

Research grounding for [#95 — Investigate currency, money, quantity, unit-of-measure, and price language features](https://github.com/sfalik/Precept/issues/95).

This file is durable research, not the proposal body. It captures the evidence base, precedent findings, dead ends explored, and design philosophy reasoning that ground the canonical design document at [`docs/CurrencyQuantityUomDesign.md`](../../../docs/CurrencyQuantityUomDesign.md).

**Research date:** 2026-04-17
**Branch:** `research/issue-95-currency-quantity-uom`
**Depends on:** Temporal type system research on branch `research/nodatime-type-alignment`

---

## Background

Issue #95 investigates adding business-domain numeric types to the Precept DSL. The temporal type system proposal (Issue #107) established the grammar mechanisms (typed constants, `in` syntax, type-family admission rule) that these types extend. This research validates whether and how those mechanisms generalize to currency, quantity, unit-of-measure, and price.

The earlier type-system research (`type-system-domain-survey.md`, `type-system-follow-ons.md`, `references/type-system-survey.md`) concluded that `decimal` + `choice("USD","EUR","GBP")` was sufficient for money, and that a dedicated `money` type was "not on the roadmap." This research revisits that conclusion in light of the unit algebra design that emerged during the temporal type system work.

---

## UCUM Compound Unit Findings

**Source:** [UCUM specification](https://ucum.org/ucum) (fetched 2026-04-17)

### Key findings

UCUM (Unified Code for Units of Measure) supports algebraic unit terms with the following operators:

| Operator | Meaning | Example |
|---|---|---|
| `.` | Multiplication | `kg.m` (kilogram-meters) |
| `/` | Division | `m/s` (meters per second) |
| Exponents (integer) | Powers | `m2` (square meters), `s-1` (per second) |
| Parentheses | Grouping | `kg/(m.s2)` |

UCUM also provides:
- **Equality testing** — whether two unit expressions reduce to the same canonical form
- **Commensurability testing** — whether two units measure the same dimension (e.g., `km` and `mi` are commensurable)
- **Annotations** — curly-brace annotations like `{score}` for dimensionless context
- **Special units** — arbitrary units not part of the metric system
- **Fixed time units** — `s` (second), `min` (minute), `h` (hour), `d` (day), `wk` (week), `mo_j` (Julian month), `a_j` (Julian year)

### Implications for Precept

1. **Compound unit expressions are structurally valid.** `USD/kg`, `USD/each`, `USD/EUR` can be modeled as UCUM-style compound expressions where currency codes are treated as unit atoms.
2. **UCUM's `/` operator is the natural syntax for price and rate types.** `price in 'USD/kg'` and `exchangerate in 'USD/EUR'` use the same operator that UCUM uses for division.
3. **UCUM time units overlap with NodaTime period units.** UCUM defines `s`, `min`, `h`, `d`, `wk`, `mo_j`, `a_j` as fixed time units. Precept must map these to NodaTime's `Period`/`Duration` rather than treating them as generic physical quantities.

### UCUM time units vs NodaTime

| UCUM unit | UCUM meaning | NodaTime mapping | Notes |
|---|---|---|---|
| `s` | Second (SI base) | `Period.FromSeconds` or `Duration.FromSeconds` | Context-dependent (same as temporal design) |
| `min` | Minute | `Period.FromMinutes` or `Duration.FromMinutes` | |
| `h` | Hour | `Period.FromHours` or `Duration.FromHours` | |
| `d` | Day (= 24 hours exactly) | `Period.FromDays` or `Duration.FromDays` | UCUM treats days as fixed 86400s; NodaTime `Period.FromDays` is calendar-relative |
| `wk` | Week (= 7 days) | `Period.FromWeeks` or `Duration.FromDays(7)` | |
| `mo_j` | Julian month (= 365.25/12 days) | Not directly mappable | NodaTime months are calendar-relative, not fixed |
| `a_j` | Julian year (= 365.25 days) | Not directly mappable | NodaTime years are calendar-relative |

**Design decision:** Precept uses NodaTime's temporal unit vocabulary (`days`, `hours`, `months`, `years`, etc.) for temporal types and UCUM unit vocabulary for physical/measurement types. The temporal vocabulary takes precedence — there is no ambiguity because temporal units are validated from the NodaTime scope and physical units from the UCUM scope.

---

## NodaTime Period-Basis Validation

**Source:** [NodaTime 3.1.x Period API](https://nodatime.org/3.1.x/api/NodaTime.Period.html), [NodaTime arithmetic user guide](https://nodatime.org/3.1.x/userguide/arithmetic), repo research at `research/language/expressiveness/nodatime-exception-surface-audit.md`

### How NodaTime determines period units

Period units are determined in one of two ways:

1. **Default overload:** `Period.Between(start, end)` uses a default unit set determined by the operand type:
   - `LocalDate` → Years, Months, Days
   - `LocalTime` → all time units
   - `LocalDateTime` → all units
   - `YearMonth` → Years, Months

2. **Explicit overload:** `Period.Between(start, end, PeriodUnits)` uses exactly the unit set specified. The algorithm works from the largest specified unit down to the smallest, picking the component with the greatest magnitude that does not overshoot. Rounding is toward the start value.

### Key semantic properties validated

1. **Periods are structural, not reducible.** `NodaTime.Period` preserves its component structure. "1 month" means 1 month, not 30 days. "24 hours" is not equal to "1 day."

2. **`Period.ToDuration()` throws for non-zero months/years.** This confirms that general periods cannot be converted to fixed-length durations. Only periods with days/hours/minutes/seconds/etc. (no months or years) can be converted.

3. **Mixed-component periods are first-class.** `'1 year + 6 months'` is a valid period with both year and month components. Components are stored independently.

4. **`Normalize()` does not collapse months/years.** Normalization converts weeks → days and balances time units, but months and years are unchanged. "12 months" does not normalize to "1 year."

5. **Period addition unions component sets.** `P2W + P1D` preserves both weeks and days components. Subtraction preserves both periods' component sets even if some become zero.

### The round-trip question — resolved

Early in the design session, we considered adding custom "round-trip validation" on top of NodaTime: compute a candidate period, re-add it to the start value, check if it exactly reaches the end value. This was rejected.

**Why rejected:** It would be Precept inventing behavior that NodaTime does not provide. The user directive is "expose NodaTime faithfully." NodaTime's `Period.Between(..., PeriodUnits)` already defines what happens when you request a specific basis — it computes the largest representable value without overshooting and rounds toward start. That is the behavior Precept should expose, not a stricter custom check.

**Consequence:** `period in 'months'` on a non-month-boundary difference will produce a truncated result (e.g., Jan 31 to Mar 1 → P1M, not a rejection). If an author wants exact month-boundary enforcement, they write a `rule` or `ensure` that checks the condition explicitly.

---

## Reversal of the "No Money Type" Conclusion

The earlier research (`type-system-follow-ons.md`, `type-system-survey.md`) concluded:

> "Every rule engine and database system surveyed has either tried a `money` type and regretted it or avoided it entirely... Precept's answer is `decimal` for the amount and `choice("USD","EUR","GBP")` for the currency code. `money` as a type is not on the roadmap."

And:

> "A dedicated `money` type (or `money("USD")`) encodes currency into the type. This requires a parameterized type system... not warranted by current domain evidence."

**Why this conclusion is now revised:**

1. **The unit algebra changes the calculus.** The earlier conclusion was correct for a language without dimensional types. Once Precept has `quantity`, `price`, and dimensional cancellation (`price * quantity → money`), money must participate in the same algebra. A `decimal` field cannot be the numerator in `price = money / quantity` because it has no currency identity.

2. **`in` syntax avoids parameterized types.** The earlier objection was that `money("USD")` requires a parameterized type system. The `in` syntax (`money in 'USD'`) achieves the same effect using the declaration-site qualification pattern already established for `period`. No new type-system mechanism is needed.

3. **The typed constant delimiter solves the literal problem.** `'100 USD'` enters through Door 2 as a typed constant. No new grammar form is needed.

4. **Cross-currency safety becomes structural.** `'100 USD' + '50 EUR'` is a compile-time type error because the `in` currencies do not match. This is the same enforcement the compiler already provides for `period` vs `duration` — different types prevent nonsensical operations.

The earlier research was good research that reached the right conclusion for the information available at the time. The unit algebra design is new information that changes the answer.

---

## Precedent Survey

### Currency/Money

| System | Approach | Precept takeaway |
|---|---|---|
| PostgreSQL `MONEY` | Locale-dependent decimal; community says "don't use it" | Locale coupling is the failure mode. Precept avoids it by requiring explicit ISO 4217 codes. |
| SQL Server `MONEY` | Fixed-scale decimal; rounding issues under division | Fixed scale is too rigid. Precept uses `decimal` backing with explicit currency identity. |
| Salesforce `Currency` | Decimal amount + separate `CurrencyIsoCode` field | The `decimal` + code pattern. Works but provides no algebra. |
| F# Units of Measure | `[<Measure>] type USD` — compile-time dimensional safety | The gold standard for type-safe currency arithmetic. `1.5<USD> + 1.5<EUR>` is a type error. |
| NMoneys (.NET) | `Money` class with `CurrencyIsoCode` enum and arithmetic | Community library; validates that .NET developers want a Money type. |

### Unit of Measure

| System | Approach | Entity-scoped? | Precept takeaway |
|---|---|---|---|
| F# Units of Measure | Compile-time, global types | No | Proves dimensional safety is valuable; but global-only. |
| UnitsNet (.NET) | ~150 quantity types, 2000+ conversions | No — global | Dominant .NET library; analogous to NodaTime for temporal. |
| SAP MM | Per-material UOM conversions | Yes | Entity-scoped units exist in enterprise; no type-level enforcement. |
| Oracle ERP Cloud | Item UOM conversions (intraclass + interclass) | Yes | Same gap: entity-scoped but runtime-only. |
| Dynamics 365 | Unit groups per product | Yes | Same pattern. |
| UCUM | Formal grammar for all units | N/A (standard) | The registry standard. Supports compound unit algebra. |

**Gap confirmed:** No language combines entity-scoped unit declarations + type-level enforcement + explicit conversion requirements. This is the genuine gap Precept could fill in a future proposal.

### Price/Rate

No DSL or rule engine surveyed treats `price` as a distinct compound type. Prices are universally modeled as plain decimals with field naming conventions. The dimensional cancellation that `price * quantity → money` provides is novel in this domain.

### Exchange Rate

Exchange rates are universally modeled as plain decimals. No system surveyed provides type-safe currency conversion via a dedicated `exchangerate` type with compile-time currency-pair verification.

---

## Dead Ends and Rejected Directions

### 1. Custom round-trip validation for period basis

Explored: compute candidate period via explicit overload → re-add to start → compare to end → reject if mismatch.

**Why rejected:** Invents behavior beyond NodaTime. The directive is "expose NodaTime faithfully." If an author wants exactness, they write a rule.

### 2. `single` modifier for period algebra participation

Explored: `period single` as a modifier meaning "this period has exactly one non-zero component."

**Why rejected:** The `in` syntax is strictly better — it specifies which component(s) are allowed, not just that there is only one. `period in 'days'` is more informative than `period single`.

### 3. Percentage as a distinct type

Explored: `percent` type where `10 percent` resolves to 0.1 and `amount + percentage` is a type error.

**Why deferred:** The semantics of "percentage OF what" are context-dependent and complex. `10 percent` as a literal is trivially `0.1` as a number. Making it a separate type adds complexity without clear domain evidence that the compiler should prevent `amount + percentage`. Deferred to a separate investigation.

### 4. Full Level C unit algebra in v1

Explored: compound dimensional types with automatic cancellation for arbitrary unit expressions.

**Why rejected:** Level C (full unit algebra with dimensional cancellation for arbitrary expressions like `kg.m/s2`) is a significant type system feature. Level B (tagged numbers with entity-scoped enforcement) is the sweet spot for Precept's first iteration. `price` and `exchangerate` are carefully scoped compound types, not general-purpose dimensional algebra.

### 5. Duration participation in unit algebra

Explored: whether `money / duration` should produce a rate type (like "dollars per hour").

**Why deferred:** Duration is a NodaTime timeline quantity (`Duration`), not a calendar quantity. Mixing it into the business-domain algebra creates cross-domain coupling. If needed, this can be modeled as `price in 'USD/hours'` where the denominator is a time unit, without requiring `duration` to participate directly.

### 6. Treating all time spans as quantity

Explored: unifying `period` and `duration` into `quantity` with time-based units.

**Why rejected:** NodaTime deliberately keeps `Period` and `Duration` as separate types. `Period` is calendar-relative (1 month ≠ 30 days). `Duration` is fixed-length nanoseconds. Collapsing them into `quantity` would violate the "expose NodaTime faithfully" directive and lose the calendar/timeline distinction.

### 7. Same `in` keyword for both specific unit and dimension category

Explored: `quantity in 'kg'` for specific unit and `quantity in 'mass'` for dimension category, with the parser disambiguating by checking whether the token is a known dimension name or unit code.

**Why rejected:** Relies on registry knowledge to disambiguate intent — the parser must know whether `'mass'` is a dimension or a unit to determine what the author meant. The distinction is important enough to warrant its own keyword. Option B (`of` for dimension, `in` for specific) was chosen: unambiguous grammar, reads naturally ("measured in kilograms" vs "a measure of length"), and the mutual exclusivity is enforced structurally rather than by content inspection.

### 8. Qualified wildcard syntax for category (`'length:*'`)

Explored: `quantity in 'length:*'` to mean "any length unit," with `*` as a wildcard.

**Why rejected:** Introduces a mini-grammar inside the quoted expression (`:*`) that has no UCUM precedent. The `of 'length'` keyword approach is cleaner — no wildcards, no special syntax, and the preposition itself signals the constraint level.

### 9. Keeping `dateonly`/`timeonly` as separate constraint suffixes for `period`

Explored: the temporal design (Decision #26) introduced `period dateonly` and `period timeonly` as single-word constraint suffixes. When `of` was added for UCUM dimension categories on `quantity`, the question arose whether `period` should keep its own suffix mechanism or unify under `of`.

**Why rejected:** Two mechanisms for the same concept (category constraint) is unnecessary complexity. `period of 'date'` and `period of 'time'` are semantically identical to `period dateonly` and `period timeonly` — they provide the same proof for `time ± period` and `date ± period` safety. The `of` keyword already exists for `quantity`; extending it to `period` eliminates two special-case keywords and unifies category constraints under one mechanism. Reads naturally: "a period of date" vs "a period dateonly."

### 10. Strict same-unit-only arithmetic for commensurable quantities

Explored: requiring exact unit match for all quantity arithmetic, even between commensurable values. `'5 ft' + '10 cm'` would be a type error despite both being length.

**Why rejected:** Makes the `of` dimension category constraint admission-only — it can reject `'5 kg'` on a length field, but you can never combine `'5 ft' + '10 cm'` without explicit conversion. This defeats the purpose of dimension-aware types. The two-rule resolution (target-directed conversion when result flows to `in` field, left-operand-wins otherwise) is deterministic and covers the practical cases. Physical unit conversions are fixed constants (unlike currency exchange rates), so auto-conversion is safe and predictable.

### 11. Allowing bare `money` without narrowing

Explored: allowing `field Amount as money` (no `in` clause) and checking currency compatibility at runtime when the field participates in arithmetic.

**Why rejected:** Violates the philosophy doc's core guarantee: "Prevention, not detection. Invalid entity configurations cannot exist. They are structurally prevented before any change is committed, not caught after the fact." Runtime currency-match validation is detection, not prevention. The philosophy doc is explicit: "This is not validation — it is governance."

The correct solution is discrete equality narrowing (D9): open fields are valid, but arithmetic requires compile-time proof via guards (`when X.currency == 'USD'`). This preserves the prevention guarantee while allowing polymorphic fields.

### 12. Requiring `in`/`of` on every field declaration

Explored: mandating that every `money`, `quantity`, `period`, `price`, and `exchangerate` field must have an `in` or `of` constraint — no open fields allowed.

**Why rejected:** Eliminates legitimate use cases: multi-currency ledgers where the currency comes from event data, generic measurement collection fields, polymorphic event args. The narrowing mechanism (D9) handles these correctly — open fields are allowed, but arithmetic is gated on guard-supplied proof. Banning open fields entirely is an overreaction.

### 17. Dedicated `rate` type for time-denominator quantities

Explored: adding a named `rate` type (parallel to `price`) for quantities with time-unit denominators — `rate in 'kg/hour'`, `rate in 'each/day'`.

**Why rejected:** `price` was named because of D11 (cross-currency safety) — the compiler needs to know "this is a currency ratio" to enforce currency-pair matching. `rate` has no equivalent safety rule. There's no "cross-time-unit safety" concern — `kg/hour * hours → kg` just works by dimensional cancellation. Every `rate` operation can already be expressed as `quantity` with a compound unit. Naming `rate` would set a precedent for naming every compound pattern (`density`, `velocity`, `acceleration`) — but those are Level C, and the naming pressure would be premature. Time-denominator compounds stay within the `quantity` type family — their arithmetic produces quantities, not a different type.

### 18. `period`-only time-denominator cancellation (original D15)

Explored: allowing only `period` to cancel time-unit denominators, excluding `duration` entirely.

**Why rejected:** The most common business pattern for elapsed-time billing is `instant - instant → duration` (clock-in/clock-out, badge swipe, system timestamp). Original D15 created a type-system dead end: the natural computation produced `duration`, but `price in 'USD/hours' * duration` was a type error. The author had no way to use `instant` subtraction results in compound arithmetic without manual decomposition. NodaTime's own philosophy supports this — `Instant` subtraction yields `Duration` *precisely* because elapsed timeline time is the correct answer for "how long did this actually take." The fixed-length boundary (D15 amendment) preserves NodaTime faithfulness: `duration` cancels `hours`/`minutes`/`seconds` (always the same length), while `days` and above remain `period`-only (variable length due to DST/calendar rules).

### 16. Treating `in` as a decomposition hint (not a constraint) for `period`

Explored: making `in` on `period` govern only how `date - date` subtraction is decomposed, without constraining assignment. A `period in 'months'` field could receive a period with days/hours components.

**Why rejected:** Inconsistent with `in` semantics on every other type. `money in 'USD'` rejects EUR assignment at compile time. `quantity in 'kg'` rejects lbs assignment at compile time. Making `in` a "hint" for period but a "hard constraint" for money/quantity creates a special case that violates Precept's uniform design. The philosophy requires prevention: if the field says `in 'months'`, a non-months period cannot be assigned. Guard-based narrowing (D9) handles open-to-constrained assignment.

### 15. Dedicated `units { }` block syntax for entity-scoped conversions

Explored: a custom language construct `units { base: each, purchasing: case = 24 each }` with a `convert()` function for entity-scoped unit-of-measure conversions.

**Why rejected:** The conversion factor is just a number. `field UnitsPerCase as integer default 24` + `set TotalUnits = Cases * UnitsPerCase` works with existing arithmetic. This parallels currency conversion (D11): `exchangerate` is a field the author populates, not an auto-conversion feature. Within a single precept there is only one definition of "case" — no cross-entity mixing ambiguity exists. A dedicated `conversionfactor in 'each/case'` type was also considered but requires Level C compound unit algebra (parsing compound expressions, tracking numerator/denominator through arithmetic, verifying cancellation) — overkill when the conversion is unambiguous within a single precept.

### 14. `double` backing for `quantity`

Explored: using `double` for `quantity` magnitudes (matching UnitsNet's convention), while keeping `decimal` for `money`, `price`, and `exchangerate`.

**Why rejected:** The result-type algebra requires homogeneous backing. `price * quantity → money`, `money / quantity → price`, `money / price → quantity` — every one of these crosses a `decimal ÷ double` boundary if `quantity` uses `double`. C# requires explicit casts for `decimal × double`, and the result carries `double`-precision artifacts into the `decimal` money value, defeating the purpose of using `decimal` for financial types. Precept governs business entities ("5 cases", "100 kg"), not scientific instruments — `decimal` precision is appropriate and the algebra demands it. UCUM conversion factors for commensurable arithmetic can be stored as `decimal` constants; common business-unit factors have exact decimal representations.

### 13. Half-up rounding as default for money

Explored: using `MidpointRounding.AwayFromZero` (half-up) as the default rounding mode for money arithmetic.

**Why rejected:** Half-up rounding introduces a systematic upward bias — in aggregate, sums trend high. This is well-documented in financial mathematics. Half-even (banker's rounding, `MidpointRounding.ToEven`) is the IEEE 754 default, the .NET `Math.Round` default, and the finance-industry standard precisely because it eliminates this bias. Half-even rounds to the nearest even number at the midpoint, distributing rounding up and down equally over many operations.

---

## Philosophy Fit

### Prevention, not detection

The type algebra prevents dimensional errors at compile time:
- `'100 USD' + '50 EUR'` → compile error (currency mismatch)
- `'5 kg' + '3 lbs'` → compile error (unit mismatch, unless conversion is explicit)
- `UnitPrice * OrderQuantity → money` → the compiler verifies dimensional cancellation

**Open-field safety:** Fields declared without `in` or `of` are valid but arithmetic-restricted. The compiler blocks all unit-sensitive arithmetic on open fields unless a guard provides compile-time proof of compatibility — `when Payment.currency == 'USD'` narrows Payment to `money in 'USD'` within the guarded scope. Outside the guard, `Payment + AccountBalance` remains a compile error. This is prevention, not detection: the invalid arithmetic expression cannot exist in the unnarrowed scope.

This pattern directly parallels nullable narrowing: `field X as number?` blocks arithmetic unless `when X != null` provides the non-null proof. Discrete equality narrowing extends the same mechanism to unit, currency, dimension, basis, and component accessors.

### One file, complete rules

Money fields, unit constraints, and pricing rules live in the precept file alongside state transitions and guards. An auditor sees the complete business rule including currency and unit requirements.

### Determinism and inspectability

All unit validation uses compile-time registries (ISO 4217, UCUM subset) or entity-scoped declarations. No ambient locale, runtime registry lookup, or environment-dependent behavior. Money precision defaults are derived from ISO 4217 minor-units data — a fixed, published standard — not from runtime configuration.

### AI-readable authoring

`field ListPrice as money in 'USD'` is self-documenting. An AI reading the precept knows the field is a monetary amount in US dollars without inferring from naming conventions on a `decimal` field.

---

## Open Questions

1. ~~**Backing library for money:** NMoneys exists but is not as dominant as NodaTime. Is it thin enough to be self-contained in Precept? Or should Precept adopt NMoneys the way it adopted NodaTime?~~ **Resolved (D13):** Self-contained. ISO 4217 is ~180 rows of static data — a lookup table, not calendar-grade logic. NMoneys adds allocation/formatting/globalization that Precept doesn't need, and its APIs don't align with `decimal` backing (D12).

2. ~~**Backing library for units:** UnitsNet is dominant for physical units but is global-only. Is a UCUM subset sufficient for v1, with UnitsNet integration deferred?~~ **Resolved (D13):** Self-contained. Embed a UCUM subset (common business units + dimensions) as a static registry. UnitsNet uses `double` throughout (incompatible with D12). UCUM validation is a grammar + registry, not computation — the NodaTime directive applies to complex logic, not static data.

3. ~~**Rounding rules:** Financial rounding varies by currency (JPY: 0 decimals, most: 2, BHD: 3). Should the `money` type enforce currency-specific precision, or leave that to explicit constraints?~~ **Resolved (D10):** ISO 4217 minor units as default precision, `maxplaces` to override, half-even (banker's) rounding.

4. ~~**Entity-scoped units design:** The `units { ... }` block syntax, `convert()` function, and per-entity conversion tables need their own proposal once the standard-registry types are stable.~~ **Resolved (D6):** No custom syntax needed. Conversion factors are plain `integer`/`decimal` fields; arithmetic handles the rest. Same pattern as currency conversion (D11): the factor is data, not a language feature.

5. ~~**`in` on period for addition:** When adding a period to a date, the `in` constraint on the target field does not change how NodaTime applies the period. Should the compiler warn if a period with mixed components is added to a basis-constrained period field?~~ **Resolved (D14):** `in` is a uniform assignment constraint. `period in 'months'` rejects assignment of periods with incompatible components at compile time, same as `money in 'USD'` rejects EUR. Open periods require guard narrowing. No warnings — prevention, not detection.

6. ~~**Discrete equality narrowing as a separate issue:** The narrowing mechanism (D9) reuses existing infrastructure but requires new accessors (`.currency`, `.unit`, `.dimension`, `.basis`, `.component`) and a new `TryApplyEqualityNarrowing` method. Should this be a separate implementation issue, or bundled with the type implementation?~~ **Resolved:** Bundled. You can't ship the types without narrowing (D9 requires it for open-field arithmetic), so splitting creates a dependency with no review benefit. Ships in the same PR as the type implementation.
