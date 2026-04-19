# Currency, Quantity, Unit-of-Measure, and Price Design

> **This is the canonical design document for Precept's business-domain numeric types.** It is maintained alongside the implementation. For the investigation issue, see [Issue #95](https://github.com/sfalik/Precept/issues/95).

**Author:** Shane (Owner)
**Date:** 2026-04-18 (v2 — restructured to match temporal design doc format; D15 amended for dual cancellation)
**Status:** Design — canonical reference
**Depends on:** [Temporal Type System Design](TemporalTypeSystemDesign.md) (Issue #107) — establishes `period`, `duration`, `timezone`, the typed constant delimiter, and the `in` syntax pattern this proposal extends.
**Prerequisite bug:** [Issue #115](https://github.com/sfalik/Precept/issues/115) — `decimal` arithmetic silently loses precision through `double` conversion. Will be completed before this proposal ships.

**Related artifacts:**
- **Research document:** [`research/language/expressiveness/currency-quantity-uom-research.md`](../research/language/expressiveness/currency-quantity-uom-research.md)
- **Type system follow-ons:** [`research/language/expressiveness/type-system-follow-ons.md`](../research/language/expressiveness/type-system-follow-ons.md)
- **Type system survey:** [`research/language/references/type-system-survey.md`](../research/language/references/type-system-survey.md)
- **Temporal expansion-joint section:** [TemporalTypeSystemDesign.md § Forward Design](TemporalTypeSystemDesign.md#why-this-matters-now)

---

## Summary

Add seven business-domain types to the Precept DSL — `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, and `exchangerate` — plus extend the temporal `period` type with unit-basis selection via the `in` syntax. Together these types give domain authors the vocabulary to express monetary constraints, unit-of-measure governance, pricing rules, and currency conversion — all within the governing contract.

The types enter through the same literal mechanisms established by the temporal proposal:
- **Door 2 typed constants:** `'100 USD'`, `'5 kg'`, `'24.50 USD/kg'`
- **Declaration-site unit qualification:** `field Cost as money in 'USD'`
- **Declaration-site dimension constraint:** `field Distance as quantity of 'length'`

Two mutually exclusive preposition keywords govern field unit context:
- **`in`** — pins to a **specific unit**: `field Weight as quantity in 'kg'`
- **`of`** — constrains to a **dimension category**: `field Distance as quantity of 'length'`

A field declaration may use `in` or `of`, but never both.

**Compound types and dimensional cancellation** — two levels in v1 scope:
- **Level A (named types):** `price` (currency/unit) and `exchangerate` (currency/currency) get dedicated type names with full operator tables.
- **Level B (time-denominator compounds):** `quantity in 'kg/hour'` uses UCUM `/` syntax with NodaTime time-unit denominators. Cancellation operates against both `period` and `duration`, with a fixed-length boundary (D15).

**What changed in v2:** D15 amended to support dual cancellation — both `period` and `duration` cancel time-unit denominators, with a fixed-length boundary: `duration` cancels `hours`/`minutes`/`seconds` (always the same length); `days` and above remain `period`-only (variable length due to DST/calendar rules). The field type declaration (`instant` vs `date`/`datetime`) is the author's statement of business intent — the compound type algebra follows from that choice.

### The NodaTime alignment directive — extended

Shane's directive (2026-04-14): *"No obscurity, expose NodaTime. Someone way smarter than us designed NodaTime. Don't try to re-invent the wheel."*

This directive extends to the broader type system:
1. When NodaTime provides the backing model (period, duration), Precept exposes NodaTime's behavior exactly.
2. When an external standard provides the vocabulary (ISO 4217 for currencies, UCUM for physical units), Precept adopts it rather than inventing a proprietary registry.
3. When the algebra produces a result with a clear domain identity, Precept names it (money, price, exchangerate) rather than collapsing everything into a generic numeric.

Precept's design principles ground this directly:

| Precept applies it to... | External standard applies it to... | The shared principle |
|---|---|---|
| `money` over `decimal` + `choice` | NodaTime's `Period` over `int monthCount` | Be explicit that this value has dimensional identity |
| `quantity in 'kg'` over `number` | F# units of measure over bare numerics | Be explicit about what unit this value is measured in |
| `price` over generic compound | UCUM's algebraic unit grammar | Be explicit about the ratio structure |
| `exchangerate` over `number` | ISO 4217's currency pair model | Be explicit that this is a currency conversion factor |
| `of 'mass'` over no constraint | UCUM dimension categories | Be explicit about which dimension family is valid |
| `in 'USD'` as hard constraint | NodaTime's type separation (no implicit conversion) | Prevention, not detection — mismatched units cannot be combined |

The governing question for every decision: **"If a domain author has this kind of data, does giving it a named type help them be explicit about what it means?"**

---

## Motivation

### The business-domain gap

Precept's type system today consists of primitives (`string`, `number`, `integer`, `decimal`, `boolean`) plus temporal types (`date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `datetime`). Domain authors modeling business entities must encode monetary amounts as `decimal`, quantities as `number`, and prices as naked division results — losing dimensional identity and the compiler's ability to catch unit mismatches.

### Before and After

**Before — scattered primitives, no dimensional safety:**

```precept
field UnitPrice as decimal
field OrderQty as integer
field TotalCost as decimal
field PaymentCurrency as string

from Pending on PriceOrder
  set TotalCost = UnitPrice * OrderQty   # no unit verification — USD * each? EUR * kg?
```

The compiler cannot verify that `UnitPrice` is in the same currency as `TotalCost`, that `OrderQty` is in the correct unit for the price's denominator, or that the multiplication produces a meaningful result. The author is on their own.

**After — typed values, compiler-verified dimensional safety:**

```precept
field UnitPrice as price in 'USD/each'
field OrderQty as quantity in 'each'
field TotalCost as money in 'USD'
field PaymentCurrency as currency

from Pending on PriceOrder
  set TotalCost = UnitPrice * OrderQty   # (USD/each) × each → USD  ✓
```

The compiler verifies that `each` cancels, that the result is `money in 'USD'`, and that it's compatible with `TotalCost`'s declaration. `UnitPrice * Weight` where Weight is `quantity in 'kg'` is a compile-time error — the price's denominator is `each`, not `kg`.

**Elapsed-time payroll — the `instant → duration` path:**

```precept
field ShiftStart as instant
field ShiftEnd as instant
field HourlyRate as price in 'USD/hours'
field Pay as money in 'USD'

from Active on CalculatePay
  set Pay = HourlyRate * (ShiftEnd - ShiftStart)   # (USD/hours) × duration → USD  ✓
```

The author declares `instant` fields — stating "I care about actual elapsed time on the timeline." Subtraction yields `duration`, which cancels the `hours` denominator. This is FLSA-correct through DST transitions: `instant - instant` measures real elapsed time, not wall-clock distance.

**Calendar-time billing — the `date → period` path:**

```precept
field ProjectStart as date
field ProjectEnd as date
field DailyRate as price in 'USD/days'
field TotalFee as money in 'USD'

from Active on CalculateFee
  set TotalFee = DailyRate * (ProjectEnd - ProjectStart)   # (USD/days) × period → USD  ✓
```

The author declares `date` fields — stating "I care about calendar distance." Subtraction yields `period`, which cancels the `days` denominator. Calendar days are the correct unit because the business contract says "10 business days," not "864,000 seconds."

**Currency conversion — governed exchange rates:**

```precept
field InvoiceTotal as money in 'EUR'
field FxRate as exchangerate in 'USD/EUR'
field SettlementAmount as money in 'USD'

from Approved on Settle
  set SettlementAmount = FxRate * InvoiceTotal   # (USD/EUR) × EUR → USD  ✓
```

The compiler verifies the currency pair: the exchange rate's denominator (`EUR`) matches the source money, and the numerator (`USD`) matches the target. `FxRate * PaymentGbp` where PaymentGbp is `money in 'GBP'` is a compile-time error — EUR ≠ GBP. Without typed exchange rates, currency conversion is bare multiplication with no structural guarantee that the pair is correct.

**Inventory — ordering, stocking, and pricing in different UOMs:**

```precept
# Unit definitions — stored as data, referenced via interpolation
field OrderingUom as unitofmeasure default 'case'
field StockingUom as unitofmeasure default 'each'
field PricingUom as unitofmeasure default 'kg'

# Conversion factors — typed compound units via interpolation
field StockPerOrder as quantity in '{StockingUom}/{OrderingUom}' default '24 each/case'
field PricingPerStock as quantity in '{PricingUom}/{StockingUom}' default '0.5 kg/each'

# Supplier pricing — in pricing UOM
field SupplierPrice as price in 'USD/{PricingUom}'

# Customer pricing — in stocking UOM
field SellingPrice as price in 'USD/{StockingUom}'

# Inventory tracked in stocking UOM
field OnHand as quantity in '{StockingUom}'
field ReorderPoint as quantity in '{StockingUom}'

# Ordering
field CasesOrdered as quantity in '{OrderingUom}'
field OrderCost as money in 'USD'

# Sales
field QtySold as quantity in '{StockingUom}'
field Revenue as money in 'USD'

from InStock on ReceiveOrder
  # Convert cases → eaches for stocking
  set OnHand = OnHand + (CasesOrdered * StockPerOrder)              # each + (case × each/case → each)  ✓
  # Convert cases → kg for pricing: case × each/case × kg/each → kg
  set OrderCost = SupplierPrice * (CasesOrdered * StockPerOrder * PricingPerStock)
                                                                     # (USD/kg) × kg → USD  ✓

from InStock on Sell
  when QtySold <= OnHand
    set OnHand = OnHand - QtySold                                    # each - each → each  ✓
    set Revenue = Revenue + (SellingPrice * QtySold)                  # USD + (USD/each × each → USD)  ✓
  when OnHand < ReorderPoint
    transition Reorder
```

Three UOMs, three purposes: ordering (`case`), stocking (`each`), pricing (`kg`). The unit names appear once as data defaults; every field references them through interpolation. The conversion chain for `OrderCost` is: `case × each/case × kg/each → kg`, then `USD/kg × kg → USD`. Each step cancels one unit — the compiler verifies the full chain. `OnHand + CasesOrdered` without conversion is a compile-time error: `each ≠ case`. Division provides the inverse: `OnHand / StockPerOrder` yields `case` without defining a second factor.

### What happens if we don't build this

Without business-domain types:
- Authors encode currencies as strings, amounts as decimals, units as naming conventions. The compiler cannot prevent `CostUsd + CostEur` — the author must catch this manually.
- Pricing logic lives in arithmetic expressions that look correct but have no dimensional verification. `UnitPrice * Quantity` compiles whether the units match or not.
- Exchange rate application is a bare multiplication with no structural guarantee that the currency pair is correct.
- AI consumers cannot reliably generate correct business-rule precepts because the type system provides no vocabulary for monetary or quantity constraints.

---

## Backing Standards

### ISO 4217 — Currency codes

ISO 4217 defines ~180 active 3-letter currency codes (USD, EUR, JPY, GBP, etc.) with associated metadata including the number of minor units (decimal places). Precept embeds ISO 4217 as a static lookup table — no external library dependency (D13). Currency codes are validated at compile time for literals and at the fire/update boundary for runtime values.

### UCUM — Units of measure

The Unified Code for Units of Measure (UCUM) provides a formal grammar for unit expressions including compound units with multiplication (`.`), division (`/`), exponents, and parentheses. It supports equality testing (canonical reduction) and commensurability testing (same dimension). Precept uses a UCUM subset covering common business units — no external library dependency (D13).

Time units are excluded from UCUM's domain in Precept — they belong to NodaTime's temporal types (dead end #6 in the research doc). NodaTime's vocabulary (`hours`, `minutes`, `days`) replaces UCUM's (`h`, `min`, `d`) everywhere, including inside compound type denominators.

### `decimal` backing — no `double` in the business chain

All seven types use `decimal` as their magnitude backing (D12). The result-type algebra demands homogeneous backing — `price * quantity → money`, `money / quantity → price`, `money / price → quantity`. If any one type uses `double`, every cross-type operation hits a `decimal ÷ double` boundary that silently injects `double`-precision artifacts into the `decimal` result. `0.1 + 0.2 ≠ 0.3` in IEEE 754 — this is unacceptable for business arithmetic.

---

## The `in` and `of` Qualification System

Two mutually exclusive preposition keywords qualify a field's unit context:

```
field <Name> as <type> in '<specific-unit>'       # pins to an exact unit
field <Name> as <type> of '<dimension-category>'   # admits any unit in that dimension
```

A field declaration may carry **at most one** of `in` or `of`. Declaring both is a compile error.

### `in` — specific unit

| Type | `in` meaning | Example |
|------|-------------|--------|
| `money` | Currency code (ISO 4217) | `field Cost as money in 'USD'` |
| `quantity` | Unit of measure | `field Weight as quantity in 'kg'` |
| `price` | Currency / unit ratio | `field UnitPrice as price in 'USD/kg'` |
| `exchangerate` | Currency / currency ratio | `field FxRate as exchangerate in 'USD/EUR'` |
| `period` | NodaTime `PeriodUnits` basis | `field LeadTime as period in 'days'` |

`in` is a **uniform assignment constraint** across all types (D14). `money in 'USD'` rejects EUR at compile time. `quantity in 'kg'` rejects `'5 lbs'` at compile time. `period in 'months'` rejects a period with days components at compile time. The same word means the same thing everywhere.

**Not available on identity types:** `currency`, `unitofmeasure`, and `dimension` do not support `in`. These types ARE the identity — their value is the currency code, unit name, or dimension name itself. `currency in 'USD'` would mean "a currency that is USD," which is a constant, not a constraint. Use `rule` or `when` guards for equality checks on identity types.

### `of` — category constraint

`of` constrains a field to accept **any member of a named category** rather than a single specific value:

| Type | `of` meaning | Example |
|------|-------------|--------|
| `quantity` | UCUM dimension category | `field Distance as quantity of 'length'` |
| `period` | Temporal dimension category | `field GracePeriod as period of 'date'` |

`of` is available on `quantity` and `period`. It is **not** available on `money`, `currency`, `price`, `exchangerate`, `unitofmeasure`, or `dimension`. The identity types (`currency`, `unitofmeasure`, `dimension`) do not carry a separate category slot — they ARE the identity. Use `when` guards for category checks (e.g., `when UOM.dimension == 'mass'`).

#### UCUM dimension categories

| Category name | Base dimension | Example units admitted |
|---|---|---|
| `length` | L | m, km, ft, mi, in, cm, mm |
| `mass` | M | kg, g, lb, oz, t |
| `volume` | L³ | L, mL, gal, fl_oz |
| `area` | L² | m2, ft2, acre, ha |
| `temperature` | Θ | Cel, [degF], K |

Time units (`s`, `min`, `h`, `d`) are excluded from the `quantity` category system because they belong to NodaTime's temporal types. Counting units (`each`, `case`, `pack`, `dozen`) are **opaque** — each is its own unit with no shared dimension and no auto-conversion. Conversion between counting units requires explicit multiplication by a typed conversion factor (e.g., `quantity in 'each/case'`).

#### Period temporal dimensions

`period of 'date'` and `period of 'time'` replace the temporal design's `dateonly` and `timeonly` constraint suffixes with the same general `of` mechanism. The `of` value is a `dimension` from the temporal partition — the same type used for UCUM dimensions on `quantity`, unified under a single partitioned registry. The proof semantics are identical — the compiler uses the dimension to verify that `time ± period` and `date ± period` are safe.

| Temporal dimension | Admitted components | Replaces | NodaTime safety guarantee |
|---|---|---|---|
| `'date'` | years, months, weeks, days | `dateonly` | `LocalDate.Plus(Period)` throws on time components |
| `'time'` | hours, minutes, seconds | `timeonly` | `LocalTime.Plus(Period)` throws on date components |
| `'datetime'` | all components | (new) | `LocalDateTime.Plus(Period)` accepts all |

### Admission vs arithmetic

`of` governs **admission** — which values can be assigned to the field — and also enables **commensurable arithmetic** between values of the same dimension with automatic unit conversion.

**Unit conversion resolution** (D8):

1. **Target-directed:** When the result flows into a field with a known `in` unit, both operands convert to the target unit.
2. **Left-operand wins:** When there is no target context, the right operand converts to the left operand's unit.

Both rules require **commensurable** operands (same UCUM dimension). `'5 kg' + '3 ft'` is a compile-time type error — mass ≠ length. Conversion only happens within the same dimension, using UCUM standard conversion factors.

**Note:** This applies to `quantity` arithmetic only. Cross-currency `money` arithmetic is always a type error — currency conversion requires an explicit `exchangerate` multiplication (D11). Exchange rates are volatile external data, not fixed constants.

---

## Proposed Types

### `money`

**What it makes explicit:** This is a monetary amount in a single currency — not a bare decimal, not a decimal+string pair. The currency is part of the value's identity. Arithmetic respects dimensional rules: you can't add USD to EUR.

**Backing type:** `decimal` magnitude + ISO 4217 currency code

**Declaration:**

```precept
field TotalCost as money in 'USD'
field Payment as money                    # open — currency comes from event data
field Budget as money in 'EUR' nullable
```

**Typed constant literal:** `'100 USD'` — content shape `<number> <3-uppercase-letters>` where the letters match an ISO 4217 code → `money`. The 3-uppercase-letter pattern is the distinguishing shape signal. See Literal Forms below.

**Interpolation:** `'{Amount} USD'`, `'100 {Curr}'`, `'{Amount} {Curr}'` — any component can be interpolated. Dynamic components require guard narrowing when assigned to an `in`-constrained field (D9).

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `money + money` | `money` | Same currency required; cross-currency is a compile error (D11). |
| `money - money` | `money` | Same currency required. |
| `money * number` | `money` | Scaling. |
| `number * money` | `money` | Commutative. |
| `money / number` | `money` | Division by scalar. Divisor safety applies. |
| `money / money` | `number` | Same currency; produces dimensionless ratio. |
| `money / money` (different currencies) | `exchangerate` | Currency / currency → exchange rate derivation. |
| `money / quantity` | `price` | Currency / non-currency → price derivation. |
| `money / period` | `price` | Time-based price derivation: `'1000 USD' / '8 hours'` → `price in 'USD/hours'` (D15). |
| `money / duration` | `price` | Duration-based price derivation for `hours`/`minutes`/`seconds` denominators (D15). |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Same currency required. Orderable within currency. |

| **Not supported** | **Why** |
|---|---|
| `money + money` (different currencies) | **Compile error.** You can't add USD to EUR — exchange rates are volatile. Convert first: `AmountEur * FxRate`. See D11. |
| `money * money` | You can't multiply two monetary amounts. Did you mean `Amount * 2` to double it, or `Amount / Quantity` to derive a price? |
| `money + number` | A bare number has no currency. Use `Amount + '50 USD'` to add $50. |
| `money / 0` | **Compile error (C92).** Division by zero is provably always wrong. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.currency` | `currency` | ISO 4217 code (`'USD'`, `'EUR'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<currency>'`, `nullable`, `default '...'`, `nonnegative`. The `maxplaces` constraint overrides the ISO 4217 default when needed.

**Default precision (D10):** `money in 'USD'` carries an implicit `maxplaces 2` (ISO 4217 minor units). `money in 'JPY'` → `maxplaces 0`. `money in 'BHD'` → `maxplaces 3`. This is a validation constraint, not auto-rounding — assigning `'1.999 USD'` to a 2-place field is a constraint violation. An explicit `maxplaces` on the field overrides the ISO default. See D10 for full semantics.

**Serialization:** `"100 USD"` (string — matches typed constant literal syntax). The runtime type handles `Parse`/`ToString` natively, like NodaTime types. No special JSON conversion logic in MCP or hosting layer.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `CostUsd + CostEur` | You can't add USD and EUR — exchange rates change. Convert one currency first using an exchange rate: `CostEur * FxUsdEur`. |
| `field Cost as money in "USD"` | Unit constraints use single quotes, not double quotes. Use `in 'USD'`. |
| `Cost + 50` | A bare number has no currency. Use `Cost + '50 USD'` to add a monetary amount. |
| `Cost * Revenue` | You can't multiply two monetary amounts together. Did you mean `Cost * 2` to double it? |
| `set TotalUsd = Payment` (open → constrained, no guard) | `Payment` has no proven currency. Use `when Payment.currency == 'USD'` to narrow it, or declare `Payment as money in 'USD'`. |

---

### `currency`

**What it makes explicit:** This is an ISO 4217 currency code — not a string that happens to look like a currency. It identifies a currency but carries no magnitude.

**Backing type:** `string` (validated against ISO 4217 at compile time for literals, at fire/update boundary for runtime values)

**Declaration:**

```precept
field BaseCurrency as currency default 'USD'
field InvoiceCurrency as currency nullable
```

**Typed constant literal:** `'USD'` — content shape `<3-uppercase-letters>` matching an ISO 4217 code → `currency`. Distinguishable from timezone identifiers (which contain `/`), dates (which contain `-`), and unit names (which are lowercase/mixed).

**Operators:** None. `currency` is a reference type — it identifies a currency, not a numeric value. It participates in comparisons and guard narrowing but not arithmetic.

| `==`, `!=` | `boolean` | Equality comparison. |

| **Not supported** | **Why** |
|---|---|
| `currency + currency` | Currency codes are identifiers, not numbers. Use `money` for monetary amounts. |
| `currency < currency` | Currency codes have no natural ordering. |

**Accessors:** None.

**Constraints:** `nullable`, `default '...'`.

**Serialization:** `"USD"` (string)

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `field X as currency default 'usd'` | Currency codes must be uppercase (ISO 4217). Use `'USD'`. |
| `field X as currency default 'USDX'` | `'USDX'` is not a recognized ISO 4217 currency code. |

---

### `quantity`

**What it makes explicit:** This is a numeric value with a unit of measure — not a bare number. The unit is part of the value's identity. `'5 kg'` and `'5 lbs'` are different quantities, even though both have magnitude 5.

**Backing type:** `decimal` magnitude + unit identifier (UCUM or entity-scoped)

**Declaration:**

```precept
field Weight as quantity in 'kg'          # specific unit
field ItemCount as quantity in 'each'     # specific unit
field Distance as quantity of 'length'    # any length unit
field Payload as quantity of 'mass'       # any mass unit
field Measurement as quantity             # open — unit comes from event data
```

`in` and `of` are mutually exclusive — a field uses one or the other, never both.

**Typed constant literal:** `'5 kg'` — content shape `<number> <unit-name>` where the unit name is not an ISO 4217 code and not a temporal unit name → `quantity`. Temporal unit names (`days`, `hours`, etc.) are a known closed set and resolve to `period`/`duration` instead.

**Interpolation:** `'{Weight} kg'`, `'5 {Unit}'`, `'{Weight} {Unit}'`

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `quantity + quantity` | `quantity` | Same dimension required; auto-converts if commensurable (D8). |
| `quantity - quantity` | `quantity` | Same dimension required; auto-converts if commensurable. |
| `quantity * number` | `quantity` | Scaling. |
| `number * quantity` | `quantity` | Commutative. |
| `quantity / number` | `quantity` | Division by scalar. Divisor safety applies. |
| `quantity / quantity` (same dimension) | `number` | Same dimension required; produces dimensionless ratio. `'10 kg' / '5 kg'` → `2`. |
| `quantity / quantity` (different dimensions) | `quantity` (compound) | Produces compound unit: `'12 kg' / '24 each'` → `'0.5 kg/each'` (Level B). |
| `quantity / period` | `quantity` (compound) | Produces time-denominator rate: `'5 kg' / '1 hour'` → `'5 kg/hour'` (Level B, D15). |
| `quantity / duration` | `quantity` (compound) | Same, but `hours`/`minutes`/`seconds` denominators only (D15). |
| `quantity (compound) * quantity` | `quantity` | Dimensional cancellation: `'0.5 kg/each' * '24 each'` → `'12 kg'` (Level B). |
| `quantity * quantity (compound)` | `quantity` | Commutative. |
| `quantity (compound) * period` | `quantity` | Dimensional cancellation: `'5 kg/hour' * '2 hours'` → `'10 kg'` (D15). |
| `period * quantity (compound)` | `quantity` | Commutative. |
| `quantity (compound) * duration` | `quantity` | Cancellation for `hours`/`minutes`/`seconds` denominators (D15). |
| `duration * quantity (compound)` | `quantity` | Commutative. |
| `money / quantity` | `price` | Currency / non-currency → price derivation. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Same dimension required; auto-converts for comparison. Orderable. |

| **Not supported** | **Why** |
|---|---|
| `quantity + quantity` (different dimensions) | **Compile error.** You can't add kilograms to meters — they measure different things. |
| `quantity + number` | A bare number has no unit. Use `Weight + '2 kg'` to add 2 kilograms. |
| `quantity * quantity` (both simple) | Multiplying two simple quantities (e.g., `kg * kg`) produces a multi-term compound (`kg²`) — Level C, out of scope. Multiplication is allowed when one operand is a compound and the other cancels its denominator. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.unit` | `unitofmeasure` | The specific UCUM unit (`'kg'`, `'each'`) |
| `.dimension` | `dimension` | The UCUM dimension category (`'mass'`, `'length'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<unit>'`, `of '<dimension>'`, `nullable`, `default '...'`, `nonnegative`.

**Serialization:** `"5 kg"` (string — matches typed constant literal syntax). The runtime type handles `Parse`/`ToString` natively.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `WeightKg + DistanceMi` | You can't add `quantity in 'kg'` (mass) to `quantity in 'mi'` (length) — they measure different things. |
| `field X as quantity in 'kg' of 'mass'` | A field can use `in` or `of`, but not both. `in 'kg'` already pins to kilograms (which is mass). |
| `Weight + 5` | A bare number has no unit. Use `Weight + '5 kg'` to add 5 kilograms. |
| `set TargetKg = Measurement` (open → constrained, no guard) | `Measurement` has no proven unit. Use `when Measurement.unit == 'kg'` or `when Measurement.dimension == 'mass'` to narrow it. |

---

### `unitofmeasure`

**What it makes explicit:** This is a unit identifier — not a string that happens to name a unit. It identifies a unit but carries no magnitude.

**Backing type:** `string` (validated against UCUM registry or entity-scoped units)

**Runtime validation:** Allowlist-only. Values must match a known UCUM unit atom (excluding temporal units, which belong to the `period` type family) or an entity-scoped unit declared within the precept. Structural characters (`/`, `*`, `^`, `.`) are rejected in atomic unit positions — they are syntactic operators in compound unit expressions and must not appear in `unitofmeasure` values, preventing injection into compound unit parsing.

**Declaration:**

```precept
field AllowedUnit as unitofmeasure default 'kg'
field SelectedUnit as unitofmeasure nullable
```

**Typed constant literal:** `'kg'` — a bare lowercase/mixed-case unit name not matching any other type family's content shape → `unitofmeasure` when the target field is declared as `unitofmeasure`. Disambiguation from `dimension` uses registry disjointness — unit names and dimension names never overlap.

**Operators:** None. `unitofmeasure` is a reference type.

| `==`, `!=` | `boolean` | Equality comparison. |

| **Not supported** | **Why** |
|---|---|
| `unitofmeasure < unitofmeasure` | Unit names have no natural ordering. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.dimension` | `dimension` | UCUM dimension category for this unit |

**Registry scopes:**

| Scope | Source of truth | Examples | Validation timing |
|---|---|---|---|
| **Language-level** | NodaTime | `days`, `hours`, `months` | Compile-time — closed set |
| **Standard registry** | ISO 4217, UCUM subset | `USD`, `EUR`, `kg`, `lbs` | Compile-time — large but closed set |
| **Entity-scoped** | `units` block in precept definition | `each`, `case`, `six-pack` | Compile-time within the precept |

**Constraints:** `nullable`, `default '...'`.

**Serialization:** `"kg"` (string)

---

### `dimension`

**What it makes explicit:** This is a measurement category identifier — not a string. It classifies the family a measurement belongs to: physical dimensions (UCUM: `'mass'`, `'length'`, `'volume'`) or temporal dimensions (`'date'`, `'time'`, `'datetime'`). The registry is partitioned by parent type — the type checker knows which partition to validate against.

**Backing type:** `string` (validated against the dimension registry — UCUM partition for `quantity`/`unitofmeasure`, temporal partition for `period`)

**Declaration:**

```precept
field MeasuredDimension as dimension default 'mass'
field AllowedDimension as dimension nullable
```

**Typed constant literal:** `'mass'` — a bare name matching the dimension registry → `dimension` when the target field is declared as `dimension`. Distinguishable from unit names because the UCUM unit registry and dimension registry are disjoint sets. Temporal dimension names (`'date'`, `'time'`, `'datetime'`) are also disjoint from both.

**Operators:** None. `dimension` is a reference type.

| `==`, `!=` | `boolean` | Equality comparison. |

**Accessors:** None.

**Constraints:** `nullable`, `default '...'`.

**Serialization:** `"mass"` (string)

**Dimension registry partitions:**

| Partition | Valid values | Used by |
|-----------|-------------|--------|
| UCUM (physical) | `'mass'`, `'length'`, `'volume'`, `'area'`, `'temperature'`, ... | `quantity.dimension`, `unitofmeasure.dimension`, `quantity of '...'` |
| Temporal | `'date'`, `'time'`, `'datetime'` | `period.dimension`, `period of '...'` |

The type checker enforces partition correctness: `quantity.dimension == 'date'` is a compile error because `'date'` is not in the UCUM partition. `period.dimension == 'mass'` is a compile error because `'mass'` is not in the temporal partition. Cross-type comparison `quantity.dimension == period.dimension` is a compile error — different partitions are never equal.

**Relationship to `quantity`, `unitofmeasure`, and `period`:**

The `.dimension` accessor on `quantity`, `unitofmeasure`, and `period` fields returns a `dimension` value. This enables type-safe cross-field consistency checks:

```precept
field AllowedDimension as dimension
field Reading as quantity

from Active on RecordReading
  when Reading.dimension == AllowedDimension
    set LastReading = Reading
```

The `of` keyword in `quantity of 'mass'` and `period of 'date'` uses the same dimension registry — `of` is a static compile-time constraint, while the `dimension` type holds the same category as runtime data.

---

### `price`

**What it makes explicit:** This is a currency-per-unit ratio — not a bare decimal. It carries both a currency numerator and a unit denominator. The key operation: `price * quantity → money` (dimensional cancellation).

**Backing type:** `decimal` magnitude + `string` numerator currency + `string` denominator unit

**Declaration:**

```precept
field UnitPrice as price in 'USD/each'
field HourlyRate as price in 'USD/hours'
field PricePerKg as price in 'EUR/kg'
field DynamicRate as price                # open — currency/unit from event data
```

**Typed constant literal:** `'4.17 USD/each'` — content shape `<number> <3-uppercase-letters>/<unit-name>` → `price`. The `/` between a currency and a non-currency unit is the distinguishing shape signal.

**Interpolation:** `'{Rate} USD/each'`, `'4.17 {Curr}/{Unit}'`, `'{Rate} {Curr}/{Unit}'`

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `price * quantity` | `money` | Dimensional cancellation: (currency/unit) × unit → currency. |
| `quantity * price` | `money` | Commutative. |
| `price * period` | `money` | Time-denominator cancellation: (currency/time) × time → currency (D15). |
| `period * price` | `money` | Commutative. |
| `price * duration` | `money` | Duration cancellation for `hours`/`minutes`/`seconds` denominators (D15). |
| `duration * price` | `money` | Commutative. |
| `price * number` | `price` | Scaling. |
| `number * price` | `price` | Commutative. |
| `price + price` | `price` | Same currency and unit required. |
| `price - price` | `price` | Same currency and unit required. |
| `money / quantity` | `price` | Derivation. |
| `money / period` | `price` | Time-based derivation: currency ÷ time → currency/time (D15). |
| `money / duration` | `price` | Duration-based derivation for `hours`/`minutes`/`seconds` denominators (D15). |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Same currency and unit required. Orderable. |

| **Not supported** | **Why** |
|---|---|
| `price * price` | You can't multiply two prices together. Use `price * quantity → money` for dimensional cancellation. |
| `price + price` (different currency or unit) | **Compile error.** `'USD/each'` and `'EUR/each'` have different currencies. `'USD/each'` and `'USD/kg'` have different denominators. |
| `price + number` | A bare number has no currency or unit. Use `Rate + '1 USD/each'` to add to a price. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.currency` | `currency` | Numerator currency (`'USD'`) |
| `.unit` | `unitofmeasure` | Denominator unit (`'each'`, `'kg'`, `'hours'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<currency>/<unit>'`, `nullable`, `default '...'`.

**Serialization:** `"4.17 USD/each"` (string — matches typed constant literal syntax). The runtime type handles `Parse`/`ToString` natively.

**Dimensional cancellation — the two paths:**

The key value of `price` as a distinct type is that `price * quantity → money` is type-safe dimensional cancellation. The compiler verifies that the quantity's unit matches the price's denominator unit.

When the denominator is a time unit (D15), both `period` and `duration` can cancel — which one depends on the author's field type choice and the fixed-length boundary:

**Calendar-time path** — the author declares `period` fields explicitly:

```precept
field HourlyRate as price in 'USD/hours'
field HoursWorked as period in 'hours'
field Pay as money in 'USD'

from Active on CalculatePay
  set Pay = HourlyRate * HoursWorked    # (USD/hours) × hours → USD  ✓
```

**Elapsed-time path** — the author uses `instant` fields, and subtraction yields `duration`:

```precept
field ShiftStart as instant
field ShiftEnd as instant
field HourlyRate as price in 'USD/hours'
field Pay as money in 'USD'

from Active on CalculatePay
  set Pay = HourlyRate * (ShiftEnd - ShiftStart)   # (USD/hours) × duration → USD  ✓
```

The field type declaration is the author's statement of business intent. `instant` means "I care about actual elapsed time" (FLSA-correct for payroll, DST-safe). `date`/`datetime` + `period` means "I care about calendar distance" (correct for lease billing, subscription terms). The compiler follows the author's declared intent — no warning, no preference between paths.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `PricePerKg * DistanceMi` | `price in 'USD/kg'` × `quantity in 'mi'` — the denominator unit (`kg`) doesn't match the quantity's unit (`mi`). Use a quantity measured in `kg`. |
| `PricePerDay * SomeDuration` | `price in 'USD/days'` × `duration` — durations can only cancel `hours`, `minutes`, or `seconds` denominators. `days` varies in length (23–25 hours near DST). Use `date`/`datetime` subtraction to get a `period` instead. |
| `set Pay = HourlyRate * Gap` (open period, no guard) | `Gap` has no proven basis. Use `when Gap.basis == 'hours'` or declare `Gap as period in 'hours'`. |

---

### `exchangerate`

**What it makes explicit:** This is a currency-per-currency ratio — not a bare number. It enables explicit, governed currency conversion. The compiler verifies that currency pairs match during conversion.

**Backing type:** `decimal` magnitude + `string` numerator currency + `string` denominator currency

**Declaration:**

```precept
field FxRate as exchangerate in 'USD/EUR'
field SpotRate as exchangerate nullable
```

**Typed constant literal:** `'1.08 USD/EUR'` — content shape `<number> <3-uppercase-letters>/<3-uppercase-letters>` where both match ISO 4217 → `exchangerate`. The `/` between two currency codes is the distinguishing shape signal.

**Interpolation:** `'{Rate} USD/EUR'`, `'1.08 {From}/{To}'`, `'{Rate} {From}/{To}'`

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `exchangerate * money` | `money` | Converts currency: (USD/EUR) × EUR → USD. |
| `money * exchangerate` | `money` | Commutative. |
| `money / money` (different currencies) | `exchangerate` | Derivation. |
| `exchangerate * number` | `exchangerate` | Scaling. |
| `number * exchangerate` | `exchangerate` | Commutative. |
| `==`, `!=` | `boolean` | Same currency pair required. |

| **Not supported** | **Why** |
|---|---|
| `exchangerate + exchangerate` | Exchange rates are ratios, not additive quantities. |
| `exchangerate < exchangerate` | Exchange rates have no meaningful ordering outside their time context. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.numerator` | `currency` | Numerator currency (`'USD'` in `'USD/EUR'`) |
| `.denominator` | `currency` | Denominator currency (`'EUR'` in `'USD/EUR'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<currency>/<currency>'`, `nullable`, `default '...'`.

**Serialization:** `"1.08 USD/EUR"` (string — matches typed constant literal syntax). The runtime type handles `Parse`/`ToString` natively.

**Currency conversion example:**

```precept
field AmountEur as money in 'EUR'
field FxRate as exchangerate in 'USD/EUR'
field AmountUsd as money in 'USD'

from Active on Convert
  set AmountUsd = FxRate * AmountEur    # (USD/EUR) × EUR → USD  ✓
```

The compiler verifies that the exchange rate's denominator currency matches the source money's currency, and that the result matches the exchange rate's numerator currency.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `FxUsdEur * AmountGbp` | `exchangerate in 'USD/EUR'` × `money in 'GBP'` — the denominator currency (`EUR`) doesn't match the money's currency (`GBP`). |
| `FxUsdEur + FxGbpEur` | You can't add exchange rates together. Exchange rates are conversion factors, not additive quantities. |

---

## Compound Types and Dimensional Cancellation

### The `/` ratio model

When two unit-bearing values are divided, the result carries a compound unit — a ratio of the numerator's unit over the denominator's unit. This is UCUM's standard algebraic unit composition: `kg/hour`, `USD/kg`, `USD/EUR`, `miles/day`.

Precept recognizes two levels of compound types in v1:

**Level A — Named compound types (full type-system support)**

These are the two ratio patterns that dominate business domains. They get dedicated type names, operator tables, and `in` constraint support:

| Pattern | Named type | Example | Why named |
|---|---|---|---|
| Currency / non-currency unit | `price` | `'4.17 USD/kg'`, `'75 USD/hours'` | Most common business ratio; enables `price * quantity → money` cancellation |
| Currency / currency | `exchangerate` | `'1.08 USD/EUR'` | Enables governed currency conversion; D11 requires it |

**Level B — Single-ratio compound quantities (v1 scope)**

Quantities divided by a single denominator unit produce ratios that are ubiquitous in business domains — rates, conversion factors, throughput, density. These don't need a new named type because they can be modeled as `quantity` with a compound unit:

```precept
# Time-denominator rates
field FlowRate as quantity in 'kg/hour'
field Throughput as quantity in 'each/day'

# Entity-scoped conversion factors
field EachPerCase as quantity in 'each/case' default '24 each/case'
field KgPerEach as quantity in 'kg/each' default '0.5 kg/each'

# Standard unit ratios
field Yield as quantity in 'kg/L'
```

The `/` inside the unit string is UCUM compound unit syntax — the same mechanism that backs `price` and `exchangerate`. The type checker validates compound unit expressions and supports dimensional cancellation.

**Time-denominator cancellation** — where the denominator is a NodaTime time unit:

| Expression | Result | Cancellation |
|---|---|---|
| `quantity in 'kg/hour' * period in 'hours'` | `quantity in 'kg'` | (kg/hour) × hour → kg |
| `quantity in 'kg/hour' * duration` | `quantity in 'kg'` | (kg/hour) × duration → kg (fixed-length denominator) |
| `quantity in 'kg' / period in 'hours'` | `quantity in 'kg/hour'` | kg ÷ hour → kg/hour |
| `quantity in 'miles' / period in 'days'` | `quantity in 'miles/day'` | miles ÷ day → miles/day |

Time denominators use NodaTime vocabulary (`hours`, `days`, not `h`, `d`) and cancel against both `period` and `duration` per the fixed-length boundary (D15).

**Non-time-denominator cancellation** — where the denominator is any other unit:

| Expression | Result | Cancellation |
|---|---|---|
| `quantity in 'each/case' * quantity in 'case'` | `quantity in 'each'` | (each/case) × case → each |
| `quantity in 'each' / quantity in 'each/case'` | `quantity in 'case'` | each ÷ each/case → case (division inverts) |
| `quantity in 'kg/each' * quantity in 'each'` | `quantity in 'kg'` | (kg/each) × each → kg |
| `quantity in 'kg/L' * quantity in 'L'` | `quantity in 'kg'` | (kg/L) × L → kg |

Non-time denominators cancel against `quantity` operands. Division provides the inverse direction: `each ÷ each/case → case`. No second conversion factor, no repeating-decimal precision problem, no auto-inversion. Standard dimensional algebra.

**Deriving compound quantities from arithmetic:**

Compound quantities don't have to be declared with literal defaults — they can be derived from dividing two quantities with different units:

```precept
field TotalWeight as quantity in 'kg'
field ItemCount as quantity in 'each'
field WeightPerItem as quantity in 'kg/each'

from Active on Weigh
  set WeightPerItem = TotalWeight / ItemCount    # kg ÷ each → kg/each  ✓
```

The compiler infers the compound unit from the operands: `kg / each → kg/each`. The result is a compound `quantity` that participates in all Level B cancellation — `WeightPerItem * SomeCount` would produce `kg`. This also works for time-denominator derivation: `quantity in 'miles' / period in 'days'` → `quantity in 'miles/day'`.

**Chained conversion** — multiple ratios composed in sequence:

```precept
field EachPerCase as quantity in 'each/case' default '24 each/case'
field KgPerEach as quantity in 'kg/each' default '0.5 kg/each'
field SupplierPrice as price in 'USD/kg'
field CasesOrdered as quantity in 'case'

# case × each/case × kg/each → kg, then USD/kg × kg → USD
set OrderCost = SupplierPrice * (CasesOrdered * EachPerCase * KgPerEach)
```

Each step cancels one unit. The compiler verifies the entire chain — if a step is missing, the units don't cancel and it's a compile error.

Level B covers all single-ratio compounds (`X/Y` where X and Y are single units). This includes time-denominator rates, entity-scoped conversion factors, and standard unit ratios. The compound unit grammar, cancellation algebra, and type checker are identical across all denominator types — the time-denominator boundary (D15) is the only special case.

**Registry bridge: NodaTime vocabulary in denominators**

UCUM defines time units as `h`, `min`, `d`, `s`. NodaTime uses `hours`, `minutes`, `days`, `seconds`. Precept's DSL surface uses NodaTime's vocabulary for all time references — including inside compound unit denominators. This means `'kg/hour'` (not `'kg/h'`), `'USD/hours'` (not `'USD/h'`), `'each/day'` (not `'each/d'`).

This is not arbitrary. The cancellation partner for a time denominator is always a `period` or `duration`, never a `quantity` (dead end #6). Using the same vocabulary in both the denominator and the `period` declaration means the compiler matches them directly — `in 'USD/hours'` cancels against `period in 'hours'` without a cross-registry translation table.

### The fixed-length boundary (D15)

Both `period` and `duration` can cancel time-unit denominators, but with a constraint: `duration` only cancels **fixed-length** denominators — units whose real-world length never varies:

| Denominator unit | Fixed-length? | `duration` cancels? | `period` cancels? |
|---|---|---|---|
| `seconds` | Yes (always 1 second) | ✓ | ✓ |
| `minutes` | Yes (always 60 seconds) | ✓ | ✓ |
| `hours` | Yes (always 3600 seconds) | ✓ | ✓ |
| `days` | No (23 or 25 hours near DST) | ✗ | ✓ |
| `weeks` | No (contains variable-length days) | ✗ | ✓ |
| `months` | No (28–31 days) | ✗ | ✓ |
| `years` | No (365 or 366 days) | ✗ | ✓ |

This boundary reflects NodaTime's own type separation — `Duration` measures fixed elapsed time (nanoseconds on the timeline), while `Period` measures calendar distance (variable-length units).

**Author intent — which cancellation path to use:**

| Business question | Field type | Subtraction yields | Cancellation partner | Example |
|---|---|---|---|---|
| "How many actual hours/minutes elapsed?" | `instant` | `duration` | `duration` cancels `hours`/`minutes`/`seconds` | Payroll, parking, SLA penalties |
| "How many calendar days/months passed?" | `date` / `datetime` | `period` | `period` cancels all time denominators | Lease billing, subscription terms, consulting |

The type choice *is* the declaration of business intent. The compound type algebra follows from it.

**Implementation note (precision):** When `duration` cancels a time denominator, the runtime extracts the scalar value via `(decimal)duration.ToInt64Nanoseconds() / <nanoseconds-per-unit>m` — NOT via `Duration.TotalHours` (which returns `double` and crosses the decimal boundary). The `long → decimal` cast is always exact (decimal has 29 significant digits; `long.MaxValue` has 19).

### Why `price` and `exchangerate` are named types, not generic `quantity` with compound units

The alternative would be: no `price` or `exchangerate` types — just `quantity in 'USD/kg'` and `quantity in 'USD/EUR'`. This was rejected (D2) because:

1. **Semantic clarity.** `price` and `exchangerate` communicate business intent. `quantity in 'USD/EUR'` does not — it looks like a physics measurement.
2. **Different operator behavior.** `price * quantity → money` is dimensional cancellation that produces a *different type*, not another quantity. Generic compound quantities don't have this cross-type algebra.
3. **Currency-specific rules.** `price` and `exchangerate` participate in D11 (cross-currency safety). Generic compound quantities don't need currency-pair checking.

Level B single-ratio compounds don't need named types because they stay within the `quantity` type family — their arithmetic produces quantities, not money or exchange rates.

**Level C — Multi-term compound unit algebra (out of scope)**

Multi-term compound units — acceleration (`m/s2`), force (`kg.m/s2`), pressure (`kg/m.s2`) — require capabilities beyond single-ratio parsing:

- Multi-term numerators: `kg.m` (mass × length)
- Exponents: `s2`, `m2`, `m3`
- Parenthesized sub-expressions: `(kg.m)/s2`
- Dimension vector tracking: [M¹ L¹ T⁻²] through arithmetic chains
- Canonical normalization: `N` = `kg.m/s2` equivalence

These are physics and engineering constructs. Precept governs business entities — money, counts, weights, time, and the single-ratio relationships between them. Level B's `X/Y` grammar covers the business domain. Level C is permanently out of scope, not deferred.

---

## Discrete Equality Narrowing

Open fields (declared without `in` or `of`) participate in the full type system — but arithmetic involving them requires compile-time proof of compatibility. This proof comes from **discrete equality narrowing**: guard expressions that constrain an open field's unit/currency/dimension to a specific value within a guarded scope.

### The contract

| Field declaration | Arithmetic status | How to use in arithmetic |
|---|---|---|
| `field X as money in 'USD'` | Statically known. Arithmetic checked at compile time. | Direct — no guard needed. |
| `field X as money` | Open. Arithmetic is a **compile error** without a guard. | `when X.currency == 'USD'` narrows X to `money in 'USD'` in scope. |
| `field X as quantity of 'mass'` | Dimension known. Same-dimension arithmetic allowed. | Direct for dimension-compatible operations. Unit-specific ops need `when X.unit == 'kg'`. |
| `field X as quantity` | Fully open. | `when X.dimension == 'length'` for dimension, `when X.unit == 'kg'` for unit. |

This is the same contract as nullable narrowing: `field X as number?` is open (nullable). Using `X` in arithmetic is a compile error unless `when X != null` narrows it. Discrete equality narrowing extends this principle to unit, currency, dimension, and basis accessors.

### Mechanism

Discrete equality narrowing plugs into the existing guard-narrowing pipeline from the null-flow narrowing and proof-engine (#106) work:

1. **Guard decomposition** — `when X.currency == 'USD'` is decomposed into an equality assertion on the accessor.
2. **Marker injection** — A `$eq:X.currency:USD` marker is injected into the symbol table for the guarded scope.
3. **Cross-branch accumulation** — Already works. `else` branches see the negation; subsequent `when` branches see accumulated narrowing.
4. **Sequential assignment flow** — `set X = Y` where Y is narrowed copies the markers (already handled by `ApplyAssignmentNarrowing`).
5. **Compatibility check** — The type checker consumes `$eq:` markers alongside static `in`/`of` constraints when checking arithmetic operands.
6. **Static `in` seeding** — Fields declared with static `in` values (e.g., `field Cost as money in 'USD'`) pre-seed `$eq:` markers at compile time. The type checker resolves the `in` value and injects `$eq:Cost.currency:USD` into the symbol table unconditionally — no guard required. This is why `money in 'USD'` fields can participate in arithmetic directly while open fields cannot.

### Accessors per type

| Type | Accessors available | Returns | Notes |
|------|-------------------|---------|-------|
| `money` | `.currency` | `currency` | ISO 4217 code |
| `money` | `.amount` | `decimal` | Magnitude (the numeric part) |
| `quantity` | `.unit` | `unitofmeasure` | Specific UCUM unit |
| `quantity` | `.dimension` | `dimension` | UCUM dimension category |
| `period` | `.basis` | `string` | Canonical basis name from the field's `in` constraint (e.g., `'hours'`, `'hours&minutes'`). For open periods, returns the runtime decomposition basis. NodaTime lowering: computed from which `PeriodUnits` flags are non-zero in the `Period` value |
| `period` | `.dimension` | `dimension` | Temporal dimension: `'date'`, `'time'`, or `'datetime'`. Date bases: `years`, `months`, `weeks`, `days`. Time bases: `hours`, `minutes`, `seconds`, `milliseconds`, `nanoseconds`, `ticks`. Multi-basis periods spanning both date and time components (e.g., `'days&hours'`) return `'datetime'`. NodaTime lowering: computed from `HasDateComponent` / `HasTimeComponent` boolean properties |
| `price` | `.currency` | `currency` | Numerator currency |
| `price` | `.unit` | `unitofmeasure` | Denominator unit |
| `exchangerate` | `.numerator` | `currency` | Numerator currency code |
| `exchangerate` | `.denominator` | `currency` | Denominator currency code |
| `unitofmeasure` | `.dimension` | `dimension` | UCUM dimension category |

### Example — open money with guard narrowing

```precept
field Payment as money                 # open — currency unknown at declaration
field AccountBalance as money in 'USD'

from Pending on ApplyPayment
  when Payment.currency == 'USD'
    set AccountBalance = AccountBalance + Payment   # valid — both proven USD
  else
    reject "Only USD payments accepted"
```

### Example — open quantity with dimension narrowing

```precept
field Measurement as quantity              # open — unit and dimension unknown
field TargetKg as quantity in 'kg'

from Pending on Record
  when Measurement.dimension == 'mass'     # narrows to mass dimension
    set TargetKg = Measurement             # valid — commensurable (same dimension), auto-converts to kg
  else
    reject "Expected a mass measurement"
```

### Example — open period with dimension narrowing

```precept
field Interval as period                   # open — any components
field DueDate as date

from Active on Extend
  when Interval.dimension == 'date'        # narrows to date-dimension period
    set DueDate = DueDate + Interval       # valid — proven date-safe for LocalDate.Plus
  else
    reject "Extension interval must use date components"
```

### Relationship to the proof engine (#106)

The proof engine introduced by #106 handles **numeric interval reasoning** — `$ival:`, `$positive:`, `$nonzero:` markers for divisor safety and sqrt safety. Discrete equality narrowing handles **string equality reasoning** — `$eq:` markers for unit/currency/dimension compatibility.

Both systems share the same infrastructure:
- String-encoded markers in `IReadOnlyDictionary<string, StaticValueKind>`
- Guard decomposition via `ApplyNarrowing`
- Cross-branch accumulation
- Sequential assignment flow via `ApplyAssignmentNarrowing`

The proof engine does not need modification. Discrete equality narrowing is a parallel layer that reuses the pipeline but operates on a different domain (discrete values vs. numeric intervals).

---

## Period Extensions

This proposal extends the temporal `period` type with two mechanisms: unit-basis selection via `in`, and temporal dimension constraint via `of`.

### Period basis — Noda-faithful semantics

For `period`, the `in` syntax is a **unit-selection instruction**, not an exactness constraint (D3). It maps directly to `NodaTime.PeriodUnits` flags and selects which overload of `Period.Between` is used when computing differences.

**Atomic mapping:**

| Precept basis atom | NodaTime `PeriodUnits` |
|---|---|
| `'years'` | `Years` |
| `'months'` | `Months` |
| `'weeks'` | `Weeks` |
| `'days'` | `Days` |
| `'hours'` | `Hours` |
| `'minutes'` | `Minutes` |
| `'seconds'` | `Seconds` |

**Composite basis** uses `&` as separator (D4):

| Precept basis | NodaTime flags |
|---|---|
| `'years&months'` | `Years \| Months` |
| `'years&months&days'` | `Years \| Months \| Days` |
| `'hours&minutes'` | `Hours \| Minutes` |
| `'hours&minutes&seconds'` | `Hours \| Minutes \| Seconds` |
| `'days&hours&minutes'` | `Days \| Hours \| Minutes` |

`&` means "and" (basis composition). `/` is reserved exclusively for ratio expressions.

### Legal basis by source operation

NodaTime enforces which period units are valid for each local-type subtraction. The compiler validates that the declared basis is compatible with the source operation.

| Operation | Legal basis components |
|---|---|
| `date - date` | Years, Months, Weeks, Days |
| `time - time` | Hours, Minutes, Seconds |
| `datetime - datetime` | All of the above |

**Invalid combinations (compile-time errors):**

| Surface | Reason |
|---|---|
| `time - time` as `period in 'days'` | `LocalTime.Between` rejects date units |
| `date - date` as `period in 'hours'` | `LocalDate.Between` rejects time units |

### Default NodaTime basis (no `in` specified)

| Operation | Default basis |
|---|---|
| `date - date` | Years, Months, Days |
| `time - time` | All time units |
| `datetime - datetime` | Years, Months, Days + all time units |

### Lowering rules

| Precept expression | NodaTime lowering |
|---|---|
| `date - date` as `period in 'months'` | `Period.Between(start, end, PeriodUnits.Months)` |
| `date - date` as `period in 'weeks&days'` | `Period.Between(start, end, PeriodUnits.Weeks \| PeriodUnits.Days)` |
| `time - time` as `period in 'hours&minutes'` | `Period.Between(start, end, PeriodUnits.Hours \| PeriodUnits.Minutes)` |
| `datetime - datetime` as `period in 'days&hours&minutes'` | `Period.Between(start, end, PeriodUnits.Days \| PeriodUnits.Hours \| PeriodUnits.Minutes)` |

The result is whatever NodaTime returns for the requested basis. Precept does not add additional exactness rejection or round-trip validation beyond what NodaTime itself provides.

### Adding an existing period

When a `period` is an operand to addition (e.g., `DueDate + Grace`), NodaTime applies whatever units are already in the period. There is no alternate "add using these output units" overload. The compiler validates that the period's structural content is compatible with the target type.

### Period operations that produce a `period` (from temporal design)

| Operation | Result | Default basis |
|---|---|---|
| `date - date` | `period` | Years, Months, Days |
| `time - time` | `period` | All time units |
| `datetime - datetime` | `period` | All units |
| `period ± period` | `period` | Union of both periods' units |
| `-period` | `period` | Same units, negated |

With `in`, each of these can be narrowed to a specific basis by lowering to the explicit `Period.Between(..., PeriodUnits)` overload.

---

## Literal Forms

All new types enter through the existing two-door literal model established by the temporal proposal.

### Door 2 — Typed constant (`'...'`) with interpolation

| Content shape | Type family | Examples |
|---|---|---|
| `<number> <ISO-4217-code>` | `money` | `'100 USD'`, `'50.25 EUR'` |
| `<number> <unit-name>` | `quantity` | `'5 kg'`, `'24 each'` |
| `<number> <currency>/<unit>` | `price` | `'4.17 USD/each'` |
| `<number> <currency>/<currency>` | `exchangerate` | `'1.08 USD/EUR'` |
| `<ISO-4217-code>` (3-letter, no number) | `currency` | `'USD'`, `'EUR'` |
| `<unit-name>` (no number) | `unitofmeasure` | `'kg'`, `'each'` |
| `<dimension-name>` (no number, UCUM dimension registry) | `dimension` | `'mass'`, `'length'` |

### Type-family admission rule

Each content shape must be distinguishable from all existing inhabitants:
- ISO 4217 codes are 3 uppercase letters — distinguishable from IANA timezone identifiers (which contain `/`), ISO 8601 dates (which contain `-`), and unit names (which are lowercase or mixed).
- Quantity literals (`<number> <unit>`) are distinguishable from temporal quantities because temporal unit names (`days`, `hours`, etc.) are a known closed set; non-temporal unit names come from ISO 4217 or UCUM registries.
- Price/rate literals contain `/` between unit components — distinguishable from temporal and plain quantity forms.
- `unitofmeasure` vs `dimension` bare strings: disambiguated by the target field's declared type. The UCUM unit registry and dimension registry are disjoint sets — unit names (`kg`, `m`, `lbs`) never overlap with dimension names (`mass`, `length`, `volume`), so context-free disambiguation is also possible.

### Integer requirement — scoped to temporal only

The temporal proposal's integer requirement (Decision #28: `'0.5 days'` is a compile error) applies only to temporal unit names. Non-temporal quantities accept non-integer magnitudes because their backing types accept them: `'2.5 kg'` is valid, `'100.50 USD'` is valid.

### Interpolation — any component, any position

`{expr}` interpolation can substitute any positional component of a typed constant or any declaration-site constraint. This section is the canonical reference for all interpolation in the business-domain types.

#### Expression-position interpolation (typed constants)

`{expr}` substitutes magnitude, unit, or currency components inside typed constant expressions:

| Type | Static | Magnitude interpolated | Unit interpolated | Both interpolated |
|---|---|---|---|---|
| `money` | `'100 USD'` | `'{Amount} USD'` | `'100 {Curr}'` | `'{Amount} {Curr}'` |
| `quantity` | `'5 kg'` | `'{Weight} kg'` | `'5 {Unit}'` | `'{Weight} {Unit}'` |
| `price` | `'4.17 USD/each'` | `'{Rate} USD/each'` | `'4.17 {Curr}/{Unit}'` | `'{Rate} {Curr}/{Unit}'` |
| `exchangerate` | `'1.08 USD/EUR'` | `'{Rate} USD/EUR'` | `'1.08 {From}/{To}'` | `'{Rate} {From}/{To}'` |

When a dynamic component flows into a field with an `in` constraint, the narrowing contract (D9, D14) applies: the compiler requires compile-time proof that the interpolated value matches the constraint.

```precept
field Cost as money in 'USD'
set Cost = '{LineTotal} {InvoiceCurrency}'    # ✗ no proof InvoiceCurrency == 'USD'

when InvoiceCurrency == 'USD'
  set Cost = '{LineTotal} {InvoiceCurrency}'  # ✓ guard narrows currency
```

#### Declaration-position interpolation (`in` and `of`)

`{FieldRef}` substitutes field values inside `in '...'` and `of '...'` constraints. This enables data-driven unit and category configuration:

**`in` interpolation:**

| Type | Static | Interpolated | Example |
|---|---|---|---|
| `money` | `in 'USD'` | `in '{BaseCurrency}'` | `field Revenue as money in '{BaseCurrency}'` |
| `quantity` | `in 'kg'` | `in '{StockingUom}'` | `field OnHand as quantity in '{StockingUom}'` |
| `quantity` (compound) | `in 'each/case'` | `in '{StockingUom}/{OrderingUom}'` | `field StockPerOrder as quantity in '{StockingUom}/{OrderingUom}'` |
| `price` | `in 'USD/kg'` | `in '{BaseCurrency}/{PricingUom}'` | `field UnitPrice as price in '{BaseCurrency}/{PricingUom}'` |
| `exchangerate` | `in 'USD/EUR'` | `in '{BaseCurrency}/{ForeignCurrency}'` | `field FxRate as exchangerate in '{BaseCurrency}/{ForeignCurrency}'` |
| `period` | `in 'months'` | `in '{BillingBasis}'` | `field BillingCycle as period in '{BillingBasis}'` |

**`of` interpolation:**

| Type | Static | Interpolated | Example |
|---|---|---|---|
| `quantity` | `of 'mass'` | `of '{AllowedDimension}'` | `field Reading as quantity of '{AllowedDimension}'` |
| `period` | `of 'date'` | `of '{TemporalDimension}'` | `field Grace as period of '{TemporalDimension}'` |

**Compound unit interpolation** — any component in a compound `in` constraint can be interpolated independently:

```precept
field OrderingUom as unitofmeasure default 'case'
field StockingUom as unitofmeasure default 'each'
field PricingUom as unitofmeasure default 'kg'
field BaseCurrency as currency default 'USD'

# Single-component interpolation
field OnHand as quantity in '{StockingUom}'                          # 'each'

# Multi-component interpolation in compound units
field StockPerOrder as quantity in '{StockingUom}/{OrderingUom}'     # 'each/case'
field PricingPerStock as quantity in '{PricingUom}/{StockingUom}'    # 'kg/each'
field UnitPrice as price in '{BaseCurrency}/{PricingUom}'           # 'USD/kg'
field FxRate as exchangerate in '{BaseCurrency}/{ForeignCurrency}'  # 'USD/EUR'
```

#### Resolution semantics

`in` and `of` interpolation resolves in two tiers:

| Source | When resolved | Constraint status | Arithmetic verified at |
|---|---|---|---|
| Field with `default` value (not set by events) | Compile time | Static — fully known | Compile time |
| Field set by events (no default, or overwritten) | Runtime | Dynamic — requires narrowing | Compile time via guards, runtime at fire/update boundary |

**Tier 1 — Static resolution (default values):**

When the referenced field has a `default` and is never `set` by any event handler, the compiler resolves the interpolation at compile time. The constraint becomes fully static — arithmetic is verified as if `in 'each'` were written directly.

```precept
field StockingUom as unitofmeasure default 'each'   # never set by events
field OnHand as quantity in '{StockingUom}'          # resolves to in 'each' at compile time
```

**Tier 2 — Dynamic resolution (event-set values):**

When the referenced field can be changed by events, the `in` constraint is dynamic. The compiler requires discrete equality narrowing before allowing arithmetic with other constrained fields:

```precept
field ActiveCurrency as currency                     # set by events — no static value
field Revenue as money in '{ActiveCurrency}'

# Without narrowing — compile error
set Revenue = Revenue + Payment                      # ✗ ActiveCurrency is unknown

# With narrowing — valid
when ActiveCurrency == 'USD' and Payment.currency == 'USD'
  set Revenue = Revenue + Payment                    # ✓ both proven USD
```

**Tier interaction — static and dynamic fields in the same compound:**

```precept
field BaseCurrency as currency default 'USD'         # static — never set by events
field PricingUom as unitofmeasure                    # dynamic — set by events

field UnitPrice as price in '{BaseCurrency}/{PricingUom}'
# BaseCurrency resolves to 'USD' at compile time
# PricingUom requires narrowing for the denominator
```

The compiler resolves what it can statically and requires narrowing for the remainder. Each interpolated component is resolved independently.

---

## Field Constraints

The seven business-domain types reuse the existing field-constraint vocabulary from `PreceptLanguageDesign.md`. No new constraint keywords are introduced. The constraint system's desugaring, compile-time diagnostics (C57/C58/C59), and proof-engine integration (C94–C98 via `NumericInterval.FromConstraints()`) apply uniformly.

### Magnitude types

`money`, `quantity`, `price`, and `exchangerate` are `decimal`-backed magnitude types. All numeric constraints apply:

| Constraint | Desugars to | Proof engine | Example |
|---|---|---|---|
| `nonnegative` | `Field >= 0` | `[0, +∞)` | `field Balance as money in 'USD' nonnegative` |
| `positive` | `Field > 0` | `(0, +∞)` | `field Rate as exchangerate in 'USD/EUR' positive` |
| `nonzero` (#111) | `Field != 0` | excludes zero | `field Divisor as quantity in 'each' nonzero` |
| `min N` | `Field >= N` | `[N, +∞)` | `field OrderQty as quantity in 'each' min 1` |
| `max N` | `Field <= N` | `(-∞, N]` | `field Score as quantity max 100` |
| `maxplaces N` | Runtime enforcement | — | `field Amount as money in 'USD' maxplaces 2` |

**`maxplaces` and ISO 4217:** `maxplaces` is available on all four magnitude types. For `money`, the author may choose to align with ISO 4217 minor units (e.g., `maxplaces 2` for USD, `maxplaces 0` for JPY), but there is no auto-default — the author must declare it explicitly. This avoids surprising behavior for domains that intentionally use higher precision (e.g., forex uses 4–6 decimal places for USD).

**Nullable interaction:** Same as existing types — when a nullable magnitude field carries a constraint, the desugared expression gains a null guard: `Field == null or Field >= N`.

### Identity types

`currency`, `unitofmeasure`, and `dimension` are identity types — their value IS the code/name, not a numeric magnitude. No numeric constraints apply:

| Constraint | `currency` | `unitofmeasure` | `dimension` |
|---|---|---|---|
| `nonnegative` / `positive` / `nonzero` | ✗ (C57) | ✗ (C57) | ✗ (C57) |
| `min` / `max` | ✗ (C57) | ✗ (C57) | ✗ (C57) |
| `maxplaces` | ✗ (C57) | ✗ (C57) | ✗ (C57) |
| `notempty` | ✗ (C57) | ✗ (C57) | ✗ (C57) |

For equality restrictions on identity types, use `rule` expressions or `when` guards: `rule Currency == 'USD'`, `when UOM.dimension == 'mass'`.

### Constraint + `in`/`of` interaction

`in` and `of` are unit/dimension qualifiers, not numeric constraints. They compose freely with numeric constraints:

```precept
field Balance as money in 'USD' nonnegative maxplaces 2
field Weight as quantity in 'kg' min 0 max 1000
field Distance as quantity of 'length' positive
field UnitPrice as price in 'USD/each' positive maxplaces 4
```

The `in`/`of` qualifier constrains the unit slot. The numeric constraints constrain the magnitude. Both are enforced independently — `in 'USD'` rejects EUR, `nonnegative` rejects negative amounts. The proof engine reasons about the numeric constraints via interval analysis; `in`/`of` enforcement operates through discrete equality narrowing and compile-time/runtime validation.

---

## Semantic Rules

### Result-type algebra

When operations combine typed values, the result type is determined by the dimensional identity of the operands:

| Operand dimensions | Result type | Example |
|---|---|---|
| Dimensionless | `number` | `5 * 3` |
| Pure currency | `money` | `'100 USD' + '50 USD'` |
| Pure time | `period` | `DueDate - StartDate` |
| General unit-bearing | `quantity` | `'5 kg' + '3 kg'` |
| Currency / non-currency | `price` | `'100 USD' / '5 kg'` |
| Currency / currency | `exchangerate` | `'1.08 USD' / '1 EUR'` |
| Currency / time | `price` | `'100 USD' / '2 hours'` → `'50 USD/hours'` |
| Unit-bearing / time | `quantity` (compound unit) | `'5 kg' / '1 hour'` → `'5 kg/hour'` |
| Compound unit-time × time | `quantity` (cancellation) | `'5 kg/hour' * '2 hours'` → `'10 kg'` |
| Price(time-denom) × time | `money` (cancellation) | `'50 USD/hours' * '3 hours'` → `'150 USD'` |
| Price(time-denom) × duration | `money` (cancellation) | `HourlyRate * (ShiftEnd - ShiftStart)` where instants |

### Comparison rules

| Type | Equality (`==`, `!=`) | Ordering (`<`, `>`, `<=`, `>=`) | Constraint |
|---|---|---|---|
| `money` | ✓ | ✓ | Same currency required |
| `currency` | ✓ | ✗ | — |
| `quantity` | ✓ | ✓ | Same dimension required; auto-converts |
| `unitofmeasure` | ✓ | ✗ | — |
| `dimension` | ✓ | ✗ | — |
| `price` | ✓ | ✓ | Same currency and unit required |
| `exchangerate` | ✓ | ✗ | Same currency pair required |

### Cross-type arithmetic: what's NOT allowed (and why)

| Expression | Why not |
|---|---|
| `money + quantity` | Currencies and physical units are incompatible dimensions. |
| `money + number` | A bare number has no currency — the result's currency would be ambiguous. |
| `quantity + number` | A bare number has no unit — the result's unit would be ambiguous. |
| `price * price` | Multiplying two ratios produces a unit²/unit² — no business meaning. |
| `money * money` | Multiplying two currencies produces currency² — no business meaning. |
| `exchangerate + exchangerate` | Exchange rates are ratios, not additive quantities. |

---

## Locked Design Decisions

### D1. `money` as a distinct type — not `decimal` + `choice`

- **What:** `money` is a first-class type that carries both magnitude and currency, not a pair of `decimal` + `choice("USD","EUR","GBP")` fields.
- **Why:** Once Precept has `quantity`, `price`, and dimensional cancellation, money must participate in the same algebra. A `decimal` field cannot be the numerator in `price = money / quantity` because it has no currency identity.
- **Alternatives rejected:** (A) `decimal` + `choice` — no algebra participation. (B) Parameterized `money("USD")` — too complex; `in 'USD'` achieves the same effect.
- **Precedent:** F# units of measure, UCUM, temporal `in` syntax.
- **Tradeoff accepted:** Reverses the earlier "money is not on the roadmap" conclusion from type-system-follow-ons.md. That conclusion was made before the unit algebra design existed.

### D2. Result-type refinement — algebra produces named types, not generic `quantity`

- **What:** When arithmetic produces a value with a recognizable dimensional identity, the result gets a named type: `money` for pure currency, `price` for currency/non-currency, `exchangerate` for currency/currency.
- **Why:** Collapsing all results into `quantity` loses the compiler's ability to enforce domain rules.
- **Alternatives rejected:** (A) Everything is `quantity` — loses dimensional identity. (B) Only `money` and `quantity` — misses the price/rate distinction.
- **Precedent:** Dimensional analysis in physics; F# units of measure.
- **Tradeoff accepted:** More types to learn. The benefit is precise operator semantics and compile-time dimensional error catching.

### D3. `in` on `period` is unit-selection, not exactness constraint — Noda-faithful

- **What:** `period in 'months'` means "use the months-only NodaTime `PeriodUnits` overload." It does **not** mean "this value must be exactly representable in whole months."
- **Why:** Custom round-trip rejection logic would be Precept inventing behavior beyond NodaTime. Violates the "expose NodaTime, don't reinvent" directive.
- **Alternatives rejected:** (A) Exactness constraint — requires custom validation beyond NodaTime. (B) No `in` on period — loses decomposition basis control.
- **Precedent:** NodaTime's `Period.Between(start, end, PeriodUnits)` overload.
- **Tradeoff accepted:** `period in 'months'` on a non-month-boundary date difference will silently truncate toward start, per NodaTime behavior.

### D4. `&` for period basis composition, `/` exclusively for ratios

- **What:** `period in 'hours&minutes'` means Hours AND Minutes. `price in 'USD/kg'` means USD PER kg. `/` has one meaning everywhere: division/ratio.
- **Why:** `&` eliminates the overloaded `/` problem. Every `/` inside a quoted expression means "per."
- **Alternatives rejected:** (A) `/` for both — context-dependent disambiguation needed. (B) `+` for basis composition — confusing because `+` already means addition.
- **Tradeoff accepted:** None significant.

### D5. UCUM as the standard registry for physical units

- **What:** Physical unit names inside `'...'` are validated against a UCUM subset.
- **Why:** UCUM provides formal grammar for unit expressions with compound support, equality testing, and commensurability testing.
- **Alternatives rejected:** (A) Custom registry — reinventing the wheel. (B) QUDT — too heavy. (C) UN/CEFACT Rec 20 — no algebraic grammar.
- **Precedent:** NodaTime uses IANA TZDB. ISO 4217 for currencies. UCUM for units (HL7/FHIR, scientific computing).
- **Tradeoff accepted:** Precept uses a practical subset, not the full UCUM specification.

### D6. Entity-scoped conversion factors are typed compound quantities, not bare integers

- **What:** Entity-scoped unit conversions (e.g., "24 each per case") are modeled as `quantity` fields with compound units: `field EachPerCase as quantity in 'each/case' default '24 each/case'`. No dedicated `units { }` block syntax.
- **Why:** Compound unit syntax (`X/Y`) already exists for Level B. Using it for conversion factors gives compile-time dimensional cancellation verification — `case × each/case → each` is checked, not just hoped. The author chains conversions explicitly; the compiler verifies each cancellation step. Division provides the inverse direction: `each ÷ each/case → case`.
- **Alternatives rejected:** (A) Dedicated `units { }` block — complex language feature for what amounts to multiplication. (B) Bare `integer` conversion factor — no compile-time unit verification (the original D6 design). (C) Auto-conversion registry — requires the compiler to discover and compose conversion paths, which is Level C territory.
- **Precedent:** SAP MM, Oracle ERP Cloud, Dynamics 365 all model UOM conversions as data. Precept improves on this by making the conversion factor carry its unit ratio, so the compiler can verify dimensional correctness.
- **Tradeoff accepted:** Counting units (`each`, `case`, `pack`) are opaque to each other — no shared dimension, no auto-conversion. The author must multiply or divide explicitly. This is deliberate: unlike `kg ↔ lbs` (fixed universal constant), entity-scoped conversion factors vary per entity.

### D7. `of` for category constraint — unified across `quantity` and `period`

- **What:** `of '<category>'` constrains a field to any member of a named category. For `quantity`, the category is a UCUM dimension. For `period`, the category is a component class (`'date'`, `'time'`).
- **Why:** Two constraints serve different authoring needs. `in 'kg'` = "always kilograms." `of 'mass'` = "any mass unit." Extending `of` to `period` eliminates the special-case `dateonly`/`timeonly` suffixes.
- **Alternatives rejected:** (A) Same `in` keyword, content-disambiguated — implicit intent. (B) `'length:*'` mini-grammar — no UCUM precedent. (C) Keep `dateonly`/`timeonly` as separate suffixes.
- **Precedent:** English preposition distinction: "measured in kilograms" vs "a measure of length."
- **Tradeoff accepted:** Two keywords instead of one. Zero ambiguity.

### D8. Commensurable arithmetic with deterministic unit resolution

- **What:** Arithmetic between `quantity` values of the same UCUM dimension is allowed even when units differ. Resolution: (1) target-directed, (2) left-operand wins.
- **Why:** If commensurable values can't combine, `of` is validation-only. Both rules are fully deterministic.
- **Alternatives rejected:** (A) Strict same-unit only — makes `of` useless for arithmetic. (B) Always SI base unit — unintuitive. (C) Require explicit conversion always — verbose.
- **Precedent:** UnitsNet supports cross-unit arithmetic with target-directed conversion.
- **Tradeoff accepted:** UCUM conversion factors become a runtime dependency. Physical unit conversions are fixed constants — safe for auto-conversion.

### D9. Open fields require discrete equality narrowing for arithmetic

- **What:** A field declared without `in` or `of` is valid, but arithmetic with constrained fields is a compile error without a guard narrowing the open field. Applies uniformly to all `in`/`of`-constrained types.
- **Why:** Per philosophy: "Prevention, not detection. Invalid entity configurations cannot exist." An open `money` field has no statically known currency. Adding it to `money in 'USD'` without proof would require runtime validation.
- **Mechanism:** `when Amount.currency == 'USD'` injects `$eq:Amount.currency:USD` markers. Reuses existing guard-narrowing pipeline from null-flow and proof engine (#106).
- **Uniform pattern:**
  | Type | Accessor | Guard pattern | Marker |
  |------|----------|---------------|--------|
  | `money` | `.currency` | `when X.currency == 'USD'` | `$eq:X.currency:USD` |
  | `quantity` | `.unit` | `when X.unit == 'kg'` | `$eq:X.unit:kg` |
  | `quantity` | `.dimension` | `when X.dimension == 'length'` | `$eq:X.dimension:length` |
  | `period` | `.basis` | `when X.basis == 'hours&minutes'` | `$eq:X.basis:hours&minutes` |
  | `period` | `.dimension` | `when X.dimension == 'date'` | `$eq:X.dimension:date` |
  | `price` | `.currency`, `.unit` | `when X.currency == 'USD'` | `$eq:X.currency:USD` |
  | `exchangerate` | `.numerator`, `.denominator` | `when X.numerator == 'USD'` | `$eq:X.numerator:USD` |
- **Alternatives rejected:** (A) Require `in`/`of` on every field — eliminates open-field use cases. (B) Allow open fields but block all arithmetic. (C) Runtime validation — violates philosophy.
- **Precedent:** Same contract as nullable narrowing. Same friction, same reason.
- **Tradeoff accepted:** Authors who use open fields must write guards.

### D10. ISO 4217 default precision as implicit `maxplaces` for `money`

- **What:** `money in 'USD'` carries an implicit `maxplaces 2` derived from ISO 4217 minor units. `money in 'JPY'` carries `maxplaces 0`. `money in 'BHD'` carries `maxplaces 3`. An explicit `maxplaces` on the field overrides the ISO default. The implicit `maxplaces` is a **validation constraint, not an auto-rounding rule** — assigning `'1.999 USD'` to a field with `maxplaces 2` is a constraint violation (compile-time via C94 for literals, runtime at the fire/update boundary for event args), not a silent truncation to `'2.00 USD'`. When the author wants rounding, they apply it explicitly. Default rounding mode for explicit rounding operations: half-even (banker's rounding).
- **Why:** ISO 4217 defines the natural precision. Making it an implicit `maxplaces` means authors get correct precision by default without annotating every field, while preserving Precept's prevention guarantee — no silent data loss. Half-even eliminates systematic bias when authors do round.
- **Alternatives rejected:** (A) Auto-round to ISO precision — hides precision loss, violates prevention guarantee. (B) `maxplaces` required on every money field — boilerplate when ISO already provides the answer. (C) Half-up rounding — upward bias. (D) No default precision — money fields accept arbitrary decimal places, defeating the purpose of ISO 4217 awareness.
- **Precedent:** NMoneys, Java `Currency.getDefaultFractionDigits()`, Python `decimal`.
- **Tradeoff accepted:** Non-standard precision requires explicit `maxplaces` override. Authors who need sub-cent precision (e.g., gas station pricing at `maxplaces 3`) must declare it.

### D11. Cross-currency `money` arithmetic requires explicit `exchangerate`

- **What:** `money in 'USD' + money in 'EUR'` is a compile-time type error. Convert via `exchangerate` multiplication.
- **Why:** Physical unit conversions are fixed constants. Currency exchange rates are volatile external data. Auto-conversion would require ambient external state.
- **Alternatives rejected:** (A) Auto-convert using exchange rate field — implicit coupling. (B) Mixed-currency arithmetic with runtime error — violates prevention guarantee.
- **Tradeoff accepted:** Currency conversion is more verbose than quantity conversion. This reflects real-world complexity.

### D12. `decimal` backing for all seven types

- **What:** All seven types use `decimal` as magnitude backing, not `double`.
- **Why:** The result-type algebra demands homogeneous backing. `price * quantity → money` — if any type uses `double`, cross-type operations hit `decimal ÷ double` boundaries with `double`-precision artifacts. `0.1 + 0.2 ≠ 0.3` is unacceptable for business arithmetic.
- **Alternatives rejected:** (A) `double` for `quantity` — breaks algebra. (B) `double` for all — loses base-10 precision. (C) Mixed backing with promotion — explodes complexity.
- **Precedent:** NMoneys, .NET `System.Currency` proposals, all C# financial libraries use `decimal`.
- **Tradeoff accepted:** UCUM conversion factors become `decimal` constants. Common factors have exact decimal representations. Exotic irrational conversions (π-related) would lose precision — but those are Level C, out of scope.

### D13. Self-contained registries — no external library dependency for currency or units

- **What:** Precept embeds ISO 4217 as a static currency table and a UCUM subset as a static unit registry. No NMoneys, no UnitsNet.
- **Why:** What Precept needs is data, not logic. ISO 4217 is ~180 rows. UCUM validation is a grammar + registry. Neither requires the complexity that justified NodaTime.
- **Alternatives rejected:** (A) NMoneys — adds dependency for a 180-row lookup; `double`-era APIs. (B) UnitsNet — uses `double` (incompatible with D12). (C) Full UCUM parser — overkill for v1.
- **Precedent:** NodaTime itself embeds TZDB data as a static resource. Same principle: own the data, not the library.
- **Tradeoff accepted:** Precept owns registry updates. ISO 4217 changes ~1-2 times/year. UCUM core units are stable.

### D13a. String-form serialization — types own their own `Parse`/`ToString`

- **What:** All compound business-domain types serialize to JSON as strings matching their typed constant literal syntax: `"100 USD"` (money), `"5 kg"` (quantity), `"4.17 USD/each"` (price), `"1.08 USD/EUR"` (exchangerate). Identity types serialize as bare strings: `"USD"` (currency), `"kg"` (unitofmeasure), `"mass"` (dimension). The runtime types implement `Parse`/`ToString` natively — no special serialization logic in MCP tools, `JsonConvert`, or the hosting layer.
- **Why:** Consistent with temporal types, where NodaTime's STJ converters serialize `date` as `"2026-03-15"`, `duration` as `"72:00:00"`, etc. The type owns its string representation. MCP `data` dicts, `precept_fire` results, and `precept_inspect` snapshots all use the same string form. One parsing path, one format, no structural mismatch between literal syntax and wire format.
- **Alternatives rejected:** (A) Structured JSON objects (`{ "amount": 100, "currency": "USD" }`) — requires `JsonConvert.ToNative` to handle nested objects (currently silently discards them), creates a second parsing path divergent from literal syntax, and makes MCP tool consumers learn a different representation than what appears in `.precept` files. (B) Mixed — strings for simple types, objects for compound — inconsistent, harder to document.
- **Precedent:** NodaTime serialization — every NodaTime type serializes to a string via its own pattern. `LocalDate` → `"2026-03-15"`, `Duration` → `"72:00:00"`, `Period` → `"P1Y2M3D"`. The type is the serializer.
- **Tradeoff accepted:** String parsing at deserialization boundaries. But the parser already exists (literal parsing), and the string form is human-readable in MCP tool output.

### D14. `in` is a uniform assignment constraint across all types

- **What:** `in` constrains assignment, not just decomposition. `period in 'months'` means only months-component periods can be assigned — same as `money in 'USD'` rejecting EUR.
- **Why:** `in` must mean the same thing everywhere. Making it a "hint" for period but a "constraint" for money would be an inconsistency.
- **Alternatives rejected:** (A) `in` governs decomposition only for period — inconsistent with money/quantity. (B) Silently truncate incompatible components — lossy, violates NodaTime faithfulness. (C) Warn but allow — detection, not prevention.
- **Tradeoff accepted:** Authors must explicitly extract components or use intermediate fields when assigning mixed-component periods to single-component fields.

**Reconciliation with D3:** D3 and D14 govern different phases of the same `in` keyword. D3 governs the **decomposition basis** — which NodaTime `PeriodUnits` overload `Period.Between()` uses. D14 governs the **assignment constraint** — what values the field accepts. Both apply simultaneously: a `period in 'months'` field uses the months decomposition basis (D3) AND rejects assignments containing non-months components (D14).

**Enforcement mechanism:** The proof engine's C94 diagnostic (`InConstraintRange` — see `ProofEngineDesign.md`) enforces `in` constraints at compile time using the proven-violation-only policy. Three enforcement tiers:

1. **Literals with statically-known content:** `set CostUsd = '100 EUR'` where `CostUsd` is `money in 'USD'` — the compiler resolves the literal's currency to EUR, proves it violates the USD constraint, and emits C94 as a compile-time error. Same for `set MonthsField = '30 days'` against `period in 'months'`.
2. **Expressions with guard-narrowed proof:** `when Payment.currency == 'USD'` seeds a `$eq:Payment.currency:USD` proof marker. An assignment to `money in 'USD'` succeeds because the proof engine can verify the constraint is satisfied. Without the guard, C94 fires (unproven — open field assigned to constrained field).
3. **Runtime boundary validation:** Event args and `precept_fire`/`precept_update` inputs are validated at the API boundary before entering the engine. This is input validation, not mid-evaluation exception — consistent with the temporal proposal's `TryValidateEventArguments` pattern.

### D15. Time-unit denominators use NodaTime vocabulary and cancel against `period` or `duration`

- **What:** Time-unit denominators use NodaTime vocabulary (`hours`, `minutes`, `days`), not UCUM (`h`, `min`, `d`). Both `period` and `duration` cancel, with a fixed-length boundary:

  | Denominator unit | Fixed-length? | `duration` cancels? | `period` cancels? |
  |---|---|---|---|
  | `seconds` | Yes | ✓ | ✓ |
  | `minutes` | Yes | ✓ | ✓ |
  | `hours` | Yes | ✓ | ✓ |
  | `days` | No (23–25h near DST) | ✗ | ✓ |
  | `weeks` | No | ✗ | ✓ |
  | `months` | No (28–31d) | ✗ | ✓ |
  | `years` | No (365–366d) | ✗ | ✓ |

- **Why:** The field type declaration is the author's statement of business intent. `instant` fields produce `duration` ("how many actual hours elapsed?"); `date`/`datetime` fields produce `period` ("how many calendar days?"). Both are valid cancellation partners. The fixed-length boundary reflects NodaTime's own type separation.
- **Operators enabled:**
  - Period path: `price × period → money`, `money ÷ period → price`, `quantity(compound) × period → quantity`, `quantity ÷ period → quantity(compound)` — cancels any time denominator.
  - Duration path: `price × duration → money`, `money ÷ duration → price`, `quantity(compound) × duration → quantity`, `quantity ÷ duration → quantity(compound)` — cancels `hours`/`minutes`/`seconds` only.
- **Period single-basis cancellation rule:** A `period` cancels a single-unit time denominator **only when the period has a single matching basis**. `period in 'hours'` cancels `price in 'USD/hours'`. `period in 'hours&minutes'` does **not** cancel `price in 'USD/hours'` — it is a compile error. NodaTime stores period components separately (`.Hours`, `.Minutes`, `.Seconds`) with no native `TotalHours` conversion. Converting a multi-basis period to a single unit would require Precept-invented arithmetic that NodaTime deliberately refuses. The author must decompose first: extract the hours component or use a single-basis period. The proof engine's C94 enforces this — a multi-basis period assignment to a single-unit denominator context is a proven constraint violation.
- **Duration is exempt from this restriction.** `Duration` is a single scalar (nanoseconds internally) — `duration.ToInt64Nanoseconds()` always yields an exact conversion to any time unit. There is no multi-basis ambiguity. `duration` cancels any fixed-length time denominator (`hours`/`minutes`/`seconds`) regardless of how the duration was constructed.
- **Date-component denominators remain period-only.** `days`, `weeks`, `months`, `years` denominators cancel only with `period`, and follow the same single-basis rule: `period in 'months'` cancels `price in 'USD/months'`, but `period in 'months&days'` does not — because NodaTime cannot convert "2 months + 15 days" into a pure months count without a reference date.
- **Alternatives rejected:** (A) UCUM time units — requires translation table. (B) Time spans as `quantity` — dead end #6. (C) No cancellation — makes `HourlyRate * HoursWorked` impossible. (D) `period`-only — blocks `instant - instant → duration` from compound arithmetic. (E) Compiler warning on dual paths — second-guessing the author's deliberate type choice.
- **Precedent:** NodaTime separates `Duration` (fixed elapsed) from `Period` (calendar distance). The temporal proposal preserves this. The boundary is faithful to NodaTime's model.
- **Tradeoff accepted:** UCUM time units not valid in denominators. Minor vocabulary restriction for zero-translation cancellation.
- **Implementation note:** Duration scalar extraction via `(decimal)duration.ToInt64Nanoseconds() / <nanoseconds-per-unit>m` — NOT `Duration.TotalHours` (`double`). `long → decimal` is always exact.

---

## Relationship to Temporal Type System

This proposal extends mechanisms established by the temporal proposal (Issue #107):

| Temporal mechanism | Extension in this proposal |
|---|---|
| Door 2 typed constants (`'...'`) | Currency amounts (`'100 USD'`), quantities (`'5 kg'`), prices (`'4.17 USD/each'`) |
| `{expr}` interpolation inside `'...'` | `'{BaseAmount} USD'`, `'{OrderSize} cases'` |
| Context-dependent type resolution | Period basis selection, unit validation scope chain |
| `period` and `duration` as separate types | `period in 'days'` maps to `PeriodUnits.Days`; both cancel time denominators with fixed-length boundary |
| `period dateonly` / `period timeonly` | `period of 'date'` / `period of 'time'` — unified under `of` category constraint |
| `.inZone(tz)` dot-accessor mediation | Future: `.convert(unit)` for entity-scoped UOM conversion |
| `instant - instant → duration` | Duration participates in compound type cancellation (D15) |

---

## Dependencies and Related Issues

| Dependency | Why |
|---|---|
| [Issue #107](https://github.com/sfalik/Precept/issues/107) — Temporal type system | Establishes `period`, `duration`, typed constant delimiter, `in` syntax, and the NodaTime alignment directive this proposal extends. |
| [Issue #115](https://github.com/sfalik/Precept/issues/115) — `decimal` precision bug | `TryToNumber` converts through `double`, losing precision. This is a security prerequisite — `double` intermediates for financial values are a trust-boundary violation, not just a correctness bug. Will be completed before this proposal ships — no blocking dependency at implementation time. |
| [Issue #106](https://github.com/sfalik/Precept/issues/106) — Proof engine | Provides the narrowing infrastructure (guard decomposition, marker injection, cross-branch accumulation) that discrete equality narrowing reuses. |
| [Issue #111](https://github.com/sfalik/Precept/issues/111) — `nonzero` constraint | Needed for `money / number` and `price / number` divisor safety. |
| [Issue #118](https://github.com/sfalik/Precept/issues/118) — Type checker decomposition | Should land before this proposal. #95 adds ~520–795 lines to `TryInferBinaryKind` (operator tables, typed-constant inference, dot-accessor resolution, dimensional cancellation). #118 plans a `PreceptTypeChecker.DomainTypeInference.cs` 7th partial file as the split point — `TryInferBinaryKind` gains early type-family dispatch ("if either operand is a business domain type, delegate to `TryInferDomainBinaryKind`"). Discrete equality narrowing (~30 lines) lands in `Narrowing.cs`. `in`/`of` validation lands in `FieldConstraints.cs`. |

---

## Explicit Exclusions / Out of Scope

- **Multi-term compound unit algebra (Level C)** — `quantity in 'kg.m/s2'` requires multi-term numerators, exponents, dimension vectors, and canonical normalization. These are physics constructs, not business constructs. Permanently out of scope.
- **Auto-conversion between counting units** — `each + case` is a compile error. Conversion requires explicit multiplication by a typed conversion factor (`each/case`). No auto-conversion registry, no conversion chain solver.
- **Entity-scoped unit declaration blocks** — No `units { case, pack, pallet }` syntax. Counting units are recognized from field declarations (`in 'case'`); conversion factors are typed `quantity` fields (`in 'each/case'`). No separate declaration form needed.
- **Percentage type** — Whether `percent` is a type or syntactic sugar for `number / 100` is a separate investigation.
- **Sub-cent precision or financial accounting standards** — Precept governs field rules, not accounting compliance.
- **`zoneddatetime` as a compound-type participant** — `zoneddatetime` is a navigation waypoint, not a declared field type. Compound type cancellation uses `instant` (→ `duration`) or `date`/`datetime` (→ `period`).

---

## Implementation Scope

### Parser changes

- Recognize `in '<unit-expression>'` and `of '<dimension-category>'` after type keywords in field declarations.
- Enforce mutual exclusivity: `in` and `of` on the same field declaration is a parse error.
- Parse unit expressions inside `'...'` — distinguish period basis (atoms separated by `&`), currency codes (3 uppercase letters), UCUM unit names, and compound price/rate expressions (containing `/` between a currency and a unit).
- Parse category names inside `of '...'` — validate against the known UCUM dimension vocabulary for `quantity`, and the fixed set `'date'`/`'time'` for `period`.
- **Typed-constant content parsing belongs in the parser phase**, not the type checker. Content-shape recognition (detecting `<number> <3-uppercase>` vs `<number> <unit>` vs `<bare-name>`) happens during parsing so that diagnostics attach to the correct token span. The type checker consumes already-classified typed constants.

### Type checker changes

> **File placement (#118):** After the type checker decomposition (#118), new type-inference logic (operator tables, typed-constant content inference, dot-accessor resolution, dimensional cancellation) lands in `PreceptTypeChecker.DomainTypeInference.cs` via type-family dispatch from `TryInferBinaryKind`. Discrete equality narrowing lands in `Narrowing.cs`. `in`/`of` constraint validation lands in `FieldConstraints.cs`. See #118's future-proofing analysis for the full per-file growth estimates.

- New types: `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`.
- Unit compatibility enforcement: same-currency for money arithmetic, same-unit for quantity arithmetic, dimensional cancellation for price × quantity.
- Dimension category validation: for `quantity of` fields, verify that assigned values' units are commensurable with the declared dimension.
- Period temporal dimension validation: for `period of 'date'`/`period of 'time'`, enforce the same proof semantics as the temporal design's `dateonly`/`timeonly`. The `of` value is a `dimension` from the temporal partition.
- Period basis validation: verify that declared basis components are legal for the source operation type.
- Mutual exclusivity: reject any field declaration that has both `in` and `of`.
- **Discrete equality narrowing:** New `TryApplyEqualityNarrowing` method (~30 lines) pattern-matches `when Field.accessor == 'literal'` guards, injecting `$eq:Field.accessor:value` markers. Reuses existing guard decomposition, cross-branch accumulation, and `ApplyAssignmentNarrowing` infrastructure.
- **Money precision:** ISO 4217 minor-units lookup during constraint resolution. Default `maxplaces` derived from currency code. Half-even rounding on all money arithmetic results.
- **Duration cancellation (D15):** When a duration operand appears against a compound type with a time-unit denominator, verify the denominator is `hours`, `minutes`, or `seconds`. Emit a compile error for `days`/`weeks`/`months`/`years` denominators with duration operands, with a teachable message explaining the fixed-length boundary.

> **Implementation risk — cancellation algebra complexity:** Compound-type cancellation (`price × quantity = money`, `amount / exchangerate = money`) is the highest-complexity implementation item in this proposal. The design deliberately constrains scope via D15's single-basis matching rule — periods must match compound denominators exactly, multi-hop conversion chains and compound denominators are out of scope. Even so, the cancellation verifier touches type checking, operator resolution, and unit compatibility in a single pass. Budget implementation time accordingly.

### Runtime engine changes

- `money`: `decimal` magnitude + `string` currency code.
- `quantity`: `decimal` magnitude + `string` unit identifier.
- `price`: `decimal` magnitude + `string` numerator currency + `string` denominator unit.
- `exchangerate`: `decimal` magnitude + `string` numerator currency + `string` denominator currency.
- Commensurable quantity arithmetic: UCUM conversion factors for standard units; convert operands to target or left-operand unit before computing.
- Period basis lowering: when computing `date - date` for a basis-constrained period field, call the explicit `Period.Between(..., PeriodUnits)` overload.
- **Duration scalar extraction:** `(decimal)duration.ToInt64Nanoseconds() / <nanoseconds-per-unit>m` for time-denominator cancellation. Nanoseconds-per-unit constants: seconds = `1_000_000_000m`, minutes = `60_000_000_000m`, hours = `3_600_000_000_000m`.

### Language server changes

- Completions: ISO 4217 codes after `money in '`, UCUM unit names after `quantity in '`, UCUM dimension names after `quantity of '` and `dimension` field assignment, temporal dimension names (`date`/`time`/`datetime`) after `period of '`, period unit atoms after `period in '`.
- Diagnostics: cross-currency arithmetic errors, unit mismatch errors, invalid period basis for source type, `in`/`of` mutual exclusivity violation, dimension mismatch on `of` fields, duration-vs-variable-length-denominator errors.

### TextMate grammar changes

- New type keywords: `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`.
- New keyword: `of` (field qualifier, same category as `in`).

### MCP tool changes

- `precept_language`: add all seven types, `in` and `of` syntax, operator tables, D15 cancellation rules.
- `precept_compile`: return unit information in field metadata (currency, unit, dimension, compound status).

---

## Scope Boundary

This document covers the design of the seven new business-domain types, the period basis extension, the `in`/`of` qualification system, compound types with dimensional cancellation (Levels A and B), and discrete equality narrowing. Level A (named `price`/`exchangerate`) and Level B (single-ratio `quantity` compounds — including time-denominator rates, entity-scoped conversion factors, and standard unit ratios) are in v1 scope. Level C (multi-term compound unit algebra) is permanently out of scope.
