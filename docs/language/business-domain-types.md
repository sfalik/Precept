# Currency, Quantity, Unit-of-Measure, and Price Design

> **Status:** Draft
> **Grounding:** `docs/language/precept-language-vision.md` § Type System, language design principles
> **Prototype reference:** `docs/CurrencyQuantityUomDesign.md` on `research/issue-95-currency-quantity-uom` branch
> **Related proposals:** [Issue #95](https://github.com/sfalik/Precept/issues/95)
> **Depends on:** [Temporal Type System Design](temporal-type-system.md) (Issue #107) — establishes `period`, `duration`, `timezone`, the typed constant delimiter, and the `in` syntax pattern this proposal extends. [Issue #115](https://github.com/sfalik/Precept/issues/115) — exact `decimal` arithmetic; context-sensitive literal typing; non-ambiguous inference invariant.

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

**Capabilities enabled by Issue #115:**
- `Amount * 0.0825` works directly — fractional scalar literals resolve as `decimal` when the co-operand is a business-domain type. No `as decimal` annotation needed.
- `min`/`max` field constraint bounds use exact `decimal` precision — no `double`-rounding in bound comparisons.
- No `double` anywhere in the business-domain operator chain: operands, intermediate results, and final assignments all stay in `decimal` end-to-end.

### The NodaTime alignment directive — extended

NodaTime alignment directive: *"No obscurity, expose NodaTime. Someone way smarter than us designed NodaTime. Don't try to re-invent the wheel."*

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

**Scalar operands are `decimal`, not `number`.** The `decimal` guarantee extends to operator tables: scaling operations like `money * decimal → money` and ratio results like `money / money → decimal` use `decimal` throughout. The `number` type (backed by `double`) is rejected as a scalar operand for business-domain types — the type checker emits a teachable diagnostic. `integer` widens to `decimal` losslessly, so `Amount * 2` works without friction. Fractional literals resolve to `decimal` or `number` via context-sensitive literal typing — no new syntax needed. See D12 and [Issue #115](https://github.com/sfalik/Precept/issues/115) for the full contract.

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
| `price` | Denominator dimension category | `field Rate as price of 'mass'` |

`of` is available on `quantity`, `period`, and `price`. It is **not** available on `money`, `currency`, `exchangerate`, `unitofmeasure`, or `dimension`. The identity types (`currency`, `unitofmeasure`, `dimension`) do not carry a separate category slot — they ARE the identity. Use `when` guards for category checks (e.g., `when UOM.dimension == 'mass'`).

**`price` exception — `in` and `of` may coexist.** For types with a single unit axis (quantity, period), `in` and `of` are mutually exclusive. `price` has two independent axes (currency + unit), so `in` with a currency-only value may combine with `of` for the denominator dimension: `field Rate as price in 'USD' of 'mass'`. When `in` specifies a compound `'currency/unit'` value, `of` is rejected — both axes are already constrained. The compiler disambiguates bare `in` values by registry lookup: ISO 4217 match → currency, UCUM match → unit.

#### UCUM dimension categories (curated registry)

Precept ships a curated set of dimension categories for the UCUM partition. UCUM itself defines 7 base dimensions and ~85 informal "kind of quantity" labels across its tables, but does not publish a formal taxonomy of dimension categories. Precept selects the subset relevant to business-domain quantities and assigns each a friendly name mapped to UCUM base-dimension exponents. The v1 set was determined by a cross-industry survey of 9 business verticals, prior art analysis (SAP, Oracle, ISO 80000, UN/CEFACT Rec 20), and regulatory forcing functions — see [ucum-dimension-categories.md](../research/language/ucum-dimension-categories.md) for the full research.

| Category name | Base dimension | Example units admitted |
|---|---|---|
| `'length'` | L | m, km, ft, mi, in, cm, mm |
| `'mass'` | M | kg, g, lb, oz, t |
| `'volume'` | L³ | L, mL, gal, fl_oz |
| `'area'` | L² | m2, ft2, acre, ha |
| `'temperature'` | Θ | Cel, [degF], K |
| `'energy'` | M·L²·T⁻² | J, kJ, cal, [Btu], kWh |
| `'pressure'` | M·L⁻¹·T⁻² | Pa, bar, atm, [psi], mm[Hg] |

`'energy'` appears in 6/9 industries surveyed — it is mandatory for utility billing (kWh) and nutrition labeling (kcal/kJ, required by FDA and EU regulation on every packaged food product). `'pressure'` appears in 7/9 industries — it is critical for healthcare (blood pressure in mmHg is the most common clinical vital sign), HVAC, industrial process control, and construction (compressive strength in MPa shares the same dimension vector).

This registry is **extensible** — additional categories (e.g., `'power'`, `'force'`, `'speed'`) can be added in future versions without breaking changes. Each addition maps a friendly name to a UCUM base-dimension vector and is purely additive. The post-v1 watchlist includes `'power'` (3/9 industries, derivable as energy/time), `'flow-rate'` (4/9, derivable as volume/time), and `'concentration'` (important in healthcare and pharma but blocked by UCUM's dimensionless treatment of amount-of-substance — needs separate investigation).

Time units (`s`, `min`, `h`, `d`) are excluded from the `quantity` category system because they belong to NodaTime's temporal types. Counting units (`each`, `case`, `pack`, `dozen`) are **opaque** — each is its own unit with no shared dimension and no auto-conversion. Conversion between counting units requires explicit multiplication by a typed conversion factor (e.g., `quantity in 'each/case'`).

#### Period temporal dimensions

`period of 'date'` and `period of 'time'` replace the temporal design's `dateonly` and `timeonly` constraint suffixes with the same general `of` mechanism. The `of` value is a `dimension` from the temporal partition — the same type used for UCUM dimensions on `quantity`, unified under a single partitioned registry. The proof semantics are identical — the compiler uses the dimension to verify that `time ± period` and `date ± period` are safe.

| Temporal dimension | Admitted components | Replaces | NodaTime safety guarantee |
|---|---|---|---|
| `'date'` | years, months, weeks, days | `dateonly` | `LocalDate.Plus(Period)` throws on time components |
| `'time'` | hours, minutes, seconds | `timeonly` | `LocalTime.Plus(Period)` throws on date components |
| `'datetime'` | all components | (new) | `LocalDateTime.Plus(Period)` accepts all |

### Admission vs arithmetic

**Authoring decision guide — `in` vs `of` vs neither:**

| Use | When | Example |
|-----|------|---------|
| `in 'USD'` | Field always holds this exact currency or unit | Invoice amounts — always USD |
| `of 'mass'` | Field accepts any unit within a dimension family | Scale reading — any mass unit |
| `in '{BaseCurrency}'` | Unit is data-driven but statically known at compile time | Multi-tenant SaaS — base currency set at configuration |
| Neither | Fully open — currency or unit comes from event data | Payment processor — currency determined at runtime |

`of` governs **admission** — which values can be assigned to the field — and also enables **commensurable arithmetic** between values of the same dimension with automatic unit conversion.

**Unit conversion resolution** (D8):

1. **Target-directed:** When the result flows into a field with a known `in` unit, both operands convert to the target unit.
2. **Left-operand wins:** When there is no target context, the right operand converts to the left operand's unit.

Both rules require **commensurable** operands (same UCUM dimension). `'5 kg' + '3 ft'` is a compile-time type error — mass ≠ length. Conversion only happens within the same dimension, using UCUM standard conversion factors.

**Note:** This applies to `quantity` arithmetic only. Cross-currency `money` arithmetic is always a type error — currency conversion requires an explicit `exchangerate` multiplication (D11). Exchange rates are volatile external data, not fixed constants.

#### `of` enforcement tiers

`of` enforcement mirrors the three-tier model from `in` (D14). The detection tier depends on how much is statically known at compile time:

| Source | Detection tier | Error type |
|---|---|---|
| Literal with statically-known unit: `set F = '5 kg'` where `F as quantity of 'length'` | **Compile time** — unit `kg` resolves to dimension `mass` ≠ `length` | `DimensionCategoryMismatch` |
| Field with statically-known `in`: `set F = WeightKg` where `WeightKg as quantity in 'kg'` and `F as quantity of 'length'` | **Compile time** — `kg` is `mass`, target requires `length` | `DimensionCategoryMismatch` |
| Open field without dimension narrowing: `set F = Reading` where `Reading as quantity` and `F as quantity of 'length'` | **Compile error** — dimension unproven; requires `when Reading.dimension == 'length'` guard before assignment | `DimensionCategoryMismatch` |
| Runtime event-arg input | **Fire/update boundary** — dimension is validated against the declared category before the engine runs | `DimensionCategoryMismatch` |

**Enforcement messages:**

| Case | Message template |
|---|---|
| Static dimension mismatch | `'5 kg'` has dimension `mass` — field `F` requires `of 'length'`. Provide a value with a length unit (m, km, ft, mi, in, cm). |
| Open field without guard | `Reading` has no proven dimension. Use `when Reading.dimension == 'length'` to narrow before assigning to `quantity of 'length'`. |
| Runtime boundary | `'5 kg'` has dimension `mass` — field `F` is constrained to `of 'length'`. Provide a value with a length unit. |

---

## Proposed Types

### `money`

**What it makes explicit:** This is a monetary amount in a single currency — not a bare decimal, not a decimal+string pair. The currency is part of the value's identity. Arithmetic respects dimensional rules: you can't add USD to EUR.

**Backing type:** `decimal` magnitude + ISO 4217 currency code

**Declaration:**

```precept
field TotalCost as money in 'USD'
field Payment as money                    # open — currency comes from event data
field Budget as money in 'EUR' optional
```

**Typed constant literal:** `'100 USD'` — when context expects `money`, the content `<number> <3-uppercase-letters>` is validated as a money value with the letters checked against the ISO 4217 registry. See Literal Forms below.

**Interpolation:** `'{Amount} USD'`, `'100 {Curr}'`, `'{Amount} {Curr}'` — any component can be interpolated. Dynamic components require guard narrowing when assigned to an `in`-constrained field (D9).

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `-money` | `money` | Negation. Unary minus preserves currency. Useful for write-downs, refunds, contra-entries. |
| `money ± money` | `money` | Same currency required; cross-currency is a compile error (D11). |
| `money * decimal` | `money` | Scaling. `integer` widens to `decimal` losslessly, so `Amount * 2` works. |
| `decimal * money` | `money` | Commutative. |
| `money / decimal` | `money` | Division by scalar. Divisor safety applies. |
| `money / money` | `decimal` | Same currency; produces dimensionless ratio. Result is full 28-digit `decimal` precision — scale is not capped. `'1.00 USD' / '3.00 USD'` = `0.3333″3″` (28 digits). Apply `round()` before assigning to a `maxplaces`-constrained field. |
| `money / money` (different currencies) | `exchangerate` | Currency / currency → exchange rate derivation. |
| `money / quantity` | `price` | Currency / non-currency → price derivation. |
| `money / period` | `price` | Time-based price derivation: `'1000 USD' / '8 hours'` → `price in 'USD/hours'` (D15). |
| `money / duration` | `price` | Duration-based price derivation for `hours`/`minutes`/`seconds` denominators (D15). |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Same currency required. Orderable within currency. |

| **Not supported** | **Why** |
|---|---|
| `money + money` (different currencies) | **Compile error.** You can't add USD to EUR — exchange rates are volatile. Convert first: `AmountEur * FxRate`. See D11. |
| `money * money` | You can't multiply two monetary amounts. Did you mean `Amount * 2` to double it, or `Amount / Quantity` to derive a price? |
| `money * number` | Monetary arithmetic requires exact `decimal` scalars, not `number` (which uses floating-point). Change the scalar field to `as decimal`, or write the literal directly — `Cost * 0.0825` works because the literal resolves as `decimal` in this context. |
| `money + number` | A bare number has no currency. Use `Amount + '50 USD'` to add $50. |
| `money / 0` | **Compile error (C92).** Division by zero is provably always wrong. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.currency` | `currency` | ISO 4217 code (`'USD'`, `'EUR'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<currency>'`, `optional`, `default '...'`, `nonnegative`. The `maxplaces` constraint overrides the ISO 4217 default when needed.

**Default precision (D10):** `money in 'USD'` carries an implicit `maxplaces 2` (ISO 4217 minor units). `money in 'JPY'` → `maxplaces 0`. `money in 'BHD'` → `maxplaces 3`. This is a validation constraint, not auto-rounding — assigning `'1.999 USD'` to a 2-place field is a constraint violation. An explicit `maxplaces` on the field overrides the ISO default. See D10 for full semantics.

**Serialization:** `"100 USD"` (string — matches typed constant literal syntax). The runtime type handles `Parse`/`ToString` natively, like NodaTime types. No special JSON conversion logic in MCP or hosting layer.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `CostUsd + CostEur` | You can't add USD and EUR — exchange rates change. Convert one currency first using an exchange rate: `CostEur * FxUsdEur`. |
| `field Cost as money in "USD"` | Unit constraints use single quotes, not double quotes. Use `in 'USD'`. |
| `Cost + 50` | A bare number has no currency. Use `Cost + '50 USD'` to add a monetary amount. |
| `Cost * Revenue` | You can't multiply two monetary amounts together. Did you mean `Cost * 2` to double it? |
| `Cost * TaxRate` (where `TaxRate as number`) | Monetary arithmetic requires exact `decimal` scalars. `number` uses floating-point, which can introduce rounding artifacts. Change `TaxRate` to `as decimal`. |
| `set Tax = TotalCost * TaxRate` (where `TaxRate as decimal`) | `set Tax = TotalCost * 0.0825   # ✓ literal resolves as decimal` — use a decimal literal directly. |
| `set TotalUsd = Payment` (open → constrained, no guard) | `Payment` has no proven currency. Use `when Payment.currency == 'USD'` to narrow it, or declare `Payment as money in 'USD'`. |

---

### `currency`

**What it makes explicit:** This is an ISO 4217 currency code — not a string that happens to look like a currency. It identifies a currency but carries no magnitude.

**Backing type:** `string` (validated against ISO 4217 at compile time for literals, at fire/update boundary for runtime values)

**Declaration:**

```precept
field BaseCurrency as currency default 'USD'
field InvoiceCurrency as currency optional
```

**Typed constant literal:** `'USD'` — content shape `<3-uppercase-letters>` matching an ISO 4217 code → `currency`. Distinguishable from timezone identifiers (which contain `/`), dates (which contain `-`), and unit names (which are lowercase/mixed).

**Operators:** None. `currency` is a reference type — it identifies a currency, not a numeric value. It participates in comparisons and guard narrowing but not arithmetic.

| `==`, `!=` | `boolean` | Equality comparison. |

| **Not supported** | **Why** |
|---|---|
| `currency + currency` | Currency codes are identifiers, not numbers. Use `money` for monetary amounts. |
| `currency < currency` | Currency codes have no natural ordering. |

**Accessors:** None.

**Constraints:** `optional`, `default '...'`.

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

**Typed constant literal:** `'5 kg'` — when context expects `quantity`, the content `<number> <unit-name>` is validated as a quantity value with the unit name checked against UCUM and entity-scoped registries.

**Interpolation:** `'{Weight} kg'`, `'5 {Unit}'`, `'{Weight} {Unit}'`

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `-quantity` | `quantity` | Negation. Unary minus preserves unit and dimension. Useful for inventory adjustments, contra-entries. |
| `quantity ± quantity` | `quantity` | Same dimension required; auto-converts if commensurable (D8). |
| `quantity * decimal` | `quantity` | Scaling. `integer` widens to `decimal` losslessly. |
| `decimal * quantity` | `quantity` | Commutative. |
| `quantity / decimal` | `quantity` | Division by scalar. Divisor safety applies. |
| `quantity / quantity` (same dimension) | `decimal` | Same dimension required; produces dimensionless ratio. `'10 kg' / '5 kg'` → `2`. Result is full 28-digit `decimal` precision — scale is not capped. Apply `round()` before assigning to a `maxplaces`-constrained field. |
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
| `quantity * number` | Quantity arithmetic requires exact `decimal` scalars, not `number` (which uses floating-point). Change the scalar field to `as decimal`, or write the literal directly — `Weight * 0.5` works because the literal resolves as `decimal` in this context. |
| `quantity + number` | A bare number has no unit. Use `Weight + '2 kg'` to add 2 kilograms. |
| `quantity * quantity` (both simple) | Multiplying two simple quantities (e.g., `kg * kg`) produces a multi-term compound (`kg²`) — Level C, out of scope. Multiplication is allowed when one operand is a compound and the other cancels its denominator. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.unit` | `unitofmeasure` | The specific UCUM unit (`'kg'`, `'each'`) |
| `.dimension` | `dimension` | The UCUM dimension category (`'mass'`, `'length'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<unit>'`, `of '<dimension>'`, `optional`, `default '...'`, `nonnegative`.

**Serialization:** `"5 kg"` (string — matches typed constant literal syntax). The runtime type handles `Parse`/`ToString` natively.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `WeightKg + DistanceMi` | You can't add `quantity in 'kg'` (mass) to `quantity in 'mi'` (length) — they measure different things. |
| `field X as quantity in 'kg' of 'mass'` | A field can use `in` or `of`, but not both. `in 'kg'` already pins to kilograms (which is mass). |
| `Weight + 5` | A bare number has no unit. Use `Weight + '5 kg'` to add 5 kilograms. |
| `Weight * Rate` (where `Rate as number`) | Quantity arithmetic requires exact `decimal` scalars. `number` uses floating-point, which can introduce rounding artifacts. Change `Rate` to `as decimal`. |
| `set Adjusted = Weight * ScaleFactor` (where `ScaleFactor as decimal`) | `set Adjusted = Weight * 0.5   # ✓ literal resolves as decimal` — use a decimal literal directly. |
| `set TargetKg = Measurement` (open → constrained, no guard) | `Measurement` has no proven unit. Use `when Measurement.unit == 'kg'` or `when Measurement.dimension == 'mass'` to narrow it. |
| `set F = WeightKg` (where `F as quantity of 'length'`) | `WeightKg` has dimension `mass` — field `F` requires `of 'length'`. Provide a value with a length unit (m, km, ft, mi, in, cm). |

---

### `unitofmeasure`

**What it makes explicit:** This is a unit identifier — not a string that happens to name a unit. It identifies a unit but carries no magnitude.

**Backing type:** `string` (validated against UCUM registry or entity-scoped units)

**Runtime validation:** Allowlist-only. Values must match a known UCUM unit atom (excluding temporal units, which belong to the `period` type family) or an entity-scoped unit declared within the precept. Structural characters (`/`, `*`, `^`, `.`) are rejected in atomic unit positions — they are syntactic operators in compound unit expressions and must not appear in `unitofmeasure` values, preventing injection into compound unit parsing.

**Declaration:**

```precept
field AllowedUnit as unitofmeasure default 'kg'
field SelectedUnit as unitofmeasure optional
```

**Typed constant literal:** `'kg'` — when context expects `unitofmeasure`, the bare name is validated against the UCUM unit registry and entity-scoped unit declarations. Like `'USD'` for `currency`, bare identifiers are context-born — the type comes from the expression context, not from the content itself.

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

**Constraints:** `optional`, `default '...'`.

**Serialization:** `"kg"` (string)

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `set SelectedUnit = 'kg/m'` | `'kg/m'` contains a structural character (`/`). `unitofmeasure` holds a single unit identifier, not a compound expression. Use `'kg'` and express the ratio as a `price` or compound `quantity in 'kg/m'`. |
| `set SelectedUnit = 'KG'` | Unit names are lowercase. Use `'kg'`. |
| `when Reading.unit == AllowedUnit` (where `AllowedUnit` has no `in` constraint) | `AllowedUnit` is an unconstrained `unitofmeasure` field. Add `when AllowedUnit == 'kg' and Reading.unit == 'kg'` to narrow both before using them together. |

### `dimension`

**What it makes explicit:** This is a measurement category identifier — not a string. It classifies the family a measurement belongs to: physical dimensions (UCUM: `'mass'`, `'length'`, `'volume'`) or temporal dimensions (`'date'`, `'time'`, `'datetime'`). The registry is partitioned by parent type — the type checker knows which partition to validate against.

**Backing type:** `string` (validated against the dimension registry — UCUM partition for `quantity`/`unitofmeasure`, temporal partition for `period`)

**Declaration:**

```precept
field MeasuredDimension as dimension default 'mass'
field AllowedDimension as dimension optional
```

**Typed constant literal:** `'mass'` — when context expects `dimension`, the bare name is validated against the UCUM dimension registry. Like other bare-identifier typed constants (`currency`, `unitofmeasure`), the type is context-born.

**Operators:** None. `dimension` is a reference type.

| `==`, `!=` | `boolean` | Equality comparison. |

**Accessors:** None.

**Constraints:** `optional`, `default '...'`.

**Serialization:** `"mass"` (string)

**Dimension registry partitions:**

| Partition | Valid values | Used by |
|-----------|-------------|--------|
| UCUM (physical) | `'mass'`, `'length'`, `'volume'`, `'area'`, `'temperature'`, `'energy'`, `'pressure'` | `quantity.dimension`, `unitofmeasure.dimension`, `quantity of '...'` |
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

**Usage patterns:**

**Pattern 1 — Guard-based dimension check** (enforce a field's dimension at runtime):
```precept
field Reading as quantity
field AllowedDimension as dimension default 'mass'

from Active on RecordReading
  when Reading.dimension == AllowedDimension
    set LastReading = Reading
  else
    reject 'Reading dimension must match allowed dimension'
```

**Pattern 2 — Rule-based dimension enforcement** (invariant across the precept):
```precept
field Measurement as quantity
field ExpectedDimension as dimension default 'length'

rule MeasurementDimensionConsistency
  Measurement.dimension == ExpectedDimension
```

**Pattern 3 — Cross-field consistency** (two fields that must have the same dimension):
```precept
field Input as quantity
field Output as quantity

rule InputOutputDimensionMatch
  Input.dimension == Output.dimension
```

**Teachable error messages:**

| Invalid assignment | Error message |
|---|---|
| `set AllowedDimension = 'meters'` (where `AllowedDimension as dimension`) | `'meters'` is a unit name, not a dimension. Did you mean `'length'`? Dimension names describe categories, not specific units. |
| `set AllowedDimension = 'weight'` | `'weight'` is not a recognized dimension. Did you mean `'mass'`? (In physics, weight is a force — mass is the correct UCUM dimension.) |
| `set AllowedDimension = 'date'` (in a `quantity` context) | `'date'` is a temporal dimension and cannot be used with `quantity`. Use a UCUM physical dimension (`'mass'`, `'length'`, `'volume'`, etc.). |
| `quantity.dimension == 'date'` | `'date'` is not in the UCUM partition. `quantity.dimension` returns a UCUM dimension; use `period.dimension` to access temporal dimensions. |

### `price`

**What it makes explicit:** This is a currency-per-unit ratio — not a bare decimal. It carries both a currency numerator and a unit denominator. The key operation: `price * quantity → money` (dimensional cancellation).

**Backing type:** `decimal` magnitude + `string` numerator currency + `string` denominator unit

**Declaration:**

```precept
field UnitPrice as price in 'USD/each'
field HourlyRate as price in 'USD/hours'
field PricePerKg as price in 'EUR/kg'
field MassRate as price in 'USD' of 'mass'  # currency fixed, any mass unit
field WeightPrice as price in 'kg'          # unit fixed, currency from event data
field MassPrice as price of 'mass'           # any mass-unit denominator, currency open
field DynamicRate as price                   # open — currency/unit from event data
```

**Declaration patterns:**

| Pattern | Declaration form | When to use |
|---------|-----------------|-------------|
| **Fixed** | `as price in 'USD/each'` | Both currency and unit are known at design time. The compiler enforces both. Produces the strictest operator contract. |
| **Currency + dimension** | `as price in 'USD' of 'mass'` | Currency is fixed; the denominator must be a mass unit but the specific unit is data-driven. The only case where `in` and `of` coexist. |
| **Currency-only** | `as price in 'USD'` | Currency is fixed; the denominator unit is data-driven. `price * quantity` still type-checks — the denominator-to-unit match is enforced at the runtime boundary. |
| **Unit-only** | `as price in 'kg'` | Denominator unit is fixed; currency comes from event data. The compiler disambiguates bare `in` values by registry lookup (ISO 4217 → currency, UCUM → unit). |
| **Dimension-only** | `as price of 'mass'` | Denominator must be in the mass dimension; both currency and specific unit are data-driven. |
| **Open** | `as price` | Both currency and denominator unit come from event data. Fewest compile-time guarantees. Use when accepting prices from external systems with unknown units. |

**Typed constant literal:** `'4.17 USD/each'` — when context expects `price`, the content `<number> <currency>/<unit>` is validated as a price value with the currency checked against ISO 4217 and the unit checked against the unit registry.

**Interpolation:** `'{Rate} USD/each'`, `'4.17 {Curr}/{Unit}'`, `'{Rate} {Curr}/{Unit}'`

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `-price` | `price` | Negation. Unary minus preserves currency and unit. Useful for discount rates, credits, price reversals. |
| `price * quantity` | `money` | Dimensional cancellation: (currency/unit) × unit → currency. |
| `quantity * price` | `money` | Commutative. |
| `price * period` | `money` | Time-denominator cancellation: (currency/time) × time → currency (D15). |
| `period * price` | `money` | Commutative. |
| `price * duration` | `money` | Duration cancellation for `hours`/`minutes`/`seconds` denominators (D15). |
| `duration * price` | `money` | Commutative. |
| `price * decimal` | `price` | Scaling. `integer` widens to `decimal` losslessly. |
| `decimal * price` | `price` | Commutative. |
| `price / decimal` | `price` | Division by scalar. Divisor safety applies. |
| `price ± price` | `price` | Same currency and unit required. |
| `money / quantity` | `price` | Derivation. |
| `money / period` | `price` | Time-based derivation: currency ÷ time → currency/time (D15). |
| `money / duration` | `price` | Duration-based derivation for `hours`/`minutes`/`seconds` denominators (D15). |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Same currency and unit required. Orderable. |

| **Not supported** | **Why** |
|---|---|
| `price * price` | You can't multiply two prices together. Use `price * quantity → money` for dimensional cancellation. |
| `price + price` (different currency or unit) | **Compile error.** `'USD/each'` and `'EUR/each'` have different currencies. `'USD/each'` and `'USD/kg'` have different denominators. |
| `floor` / `ceil` / `truncate` on `price` | Floor/ceil/truncate on a currency-per-unit ratio has no standard business domain meaning. Use `round(Rate, N)` for precision management. |
| `price * number` | Price arithmetic requires exact `decimal` scalars, not `number` (which uses floating-point). Change the scalar field to `as decimal`, or write the literal directly — `Rate * 0.9` works because the literal resolves as `decimal` in this context. |
| `price + number` | A bare number has no currency or unit. Use `Rate + '1 USD/each'` to add to a price. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.currency` | `currency` | Numerator currency (`'USD'`) |
| `.unit` | `unitofmeasure` | Denominator unit (`'each'`, `'kg'`, `'hours'`) |
| `.dimension` | `dimension` | Denominator unit dimension category (`'mass'`, `'length'`, etc.) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<currency>/<unit>'`, `in '<currency>'`, `in '<unit>'`, `of '<dimension>'`, `in '<currency>' of '<dimension>'`, `optional`, `default '...'`.

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
field SpotRate as exchangerate optional
```

**Declaration patterns:**

| Pattern | Declaration form | When to use |
|---------|-----------------|-------------|
| **Fixed** | `as exchangerate in 'USD/EUR'` | Currency pair is known at design time (e.g., a service that only supports USD→EUR). The compiler validates every conversion against this pair. |
| **Partial** | `as exchangerate in 'USD'` (numerator-only) | Target currency is fixed; the source currency is data-driven. Useful when a system always converts INTO one currency but accepts multiple source currencies. |
| **Open** | `as exchangerate` | Both currencies are runtime data. Required for general-purpose rate tables (multi-currency wallets, FX APIs). |

**Typed constant literal:** `'1.08 USD/EUR'` — when context expects `exchangerate`, the content `<number> <currency>/<currency>` is validated as an exchange rate value with both currency codes checked against ISO 4217.

**Interpolation:** `'{Rate} USD/EUR'`, `'1.08 {From}/{To}'`, `'{Rate} {From}/{To}'`

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `exchangerate * money` | `money` | Converts currency: (USD/EUR) × EUR → USD. |
| `money * exchangerate` | `money` | Commutative. |
| `money / money` (different currencies) | `exchangerate` | Derivation. |
| `exchangerate * decimal` | `exchangerate` | Scaling. `integer` widens to `decimal` losslessly. |
| `decimal * exchangerate` | `exchangerate` | Commutative. |
| `exchangerate / decimal` | `exchangerate` | Division by scalar. Divisor safety applies. |
| `==`, `!=` | `boolean` | Same currency pair required. |

| **Not supported** | **Why** |
|---|---|
| `exchangerate + exchangerate` | Exchange rates are ratios, not additive quantities. |
| `-exchangerate` | Negative exchange rates have no business meaning. Rates are directional ratios, not signed quantities. |
| `abs(exchangerate)` | Since negative exchange rates are impossible by domain rule, `abs` has no meaningful input to operate on. Use the `positive` field constraint to enforce this at declaration. See D16. |
| `exchangerate < exchangerate` | Exchange rates have no meaningful ordering outside their time context. |
| `min(exchangerate, exchangerate)` / `max(exchangerate, exchangerate)` | Selection functions require ordering, which is not defined for `exchangerate` (row above). |
| `floor` / `ceil` / `truncate` on `exchangerate` | Floor/ceil/truncate on a currency pair ratio has no standard business domain meaning. Use `round(FxRate, N)` for precision management. |
| `exchangerate * number` | Exchange rate arithmetic requires exact `decimal` scalars, not `number` (which uses floating-point). Change the scalar field to `as decimal`, or write the literal directly — `FxRate * 0.99` works because the literal resolves as `decimal` in this context. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.from` | `currency` | Source currency (`'USD'` in `'USD/EUR'`) |
| `.to` | `currency` | Target currency (`'EUR'` in `'USD/EUR'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Constraints:** `in '<currency>/<currency>'`, `optional`, `default '...'`. **Implicit constraint:** `positive` — zero and negative exchange rates are always invalid configurations (see D16 Corollary 2). Declaring `positive` or `nonzero` explicitly is redundant.

**Serialization:** `"1.08 USD/EUR"` (string — matches typed constant literal syntax). The runtime type handles `Parse`/`ToString` natively.

**Currency conversion example:**

```precept
field AmountEur as money in 'EUR'
field FxRate as exchangerate in 'USD/EUR'
field AmountUsd as money in 'USD'

from Active on Convert
  set AmountUsd = FxRate * AmountEur    # (USD/EUR) × EUR → USD  ✓
```

The compiler verifies that the exchange rate's target currency (`.to`) matches the source money's currency, and that the result matches the exchange rate's source currency (`.from`).

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `FxUsdEur * AmountGbp` | `exchangerate in 'USD/EUR'` × `money in 'GBP'` — the target currency (`EUR`) doesn't match the money's currency (`GBP`). |
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

This is the same contract as optional narrowing: `field X as number optional` is open (optional). Using `X` in arithmetic is a compile error unless `when X != null` narrows it. Discrete equality narrowing extends this principle to unit, currency, dimension, and basis accessors.

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
| `price` | `.dimension` | `dimension` | Denominator unit dimension category |
| `exchangerate` | `.from` | `currency` | Source currency code |
| `exchangerate` | `.to` | `currency` | Target currency code |
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

Typed constants follow the context-born resolution model established in `literal-system.md` — the expression context determines the type, and the content is validated against it. Given context-determined type, valid content patterns:

| Expected type | Valid content | Examples |
|---|---|---|
| `money` | `<number> <ISO-4217-code>` | `'100 USD'`, `'50.25 EUR'` |
| `quantity` | `<number> <unit-name>` | `'5 kg'`, `'24 each'` |
| `price` | `<number> <currency>/<unit>` | `'4.17 USD/each'` |
| `exchangerate` | `<number> <currency>/<currency>` | `'1.08 USD/EUR'` |
| `currency` | `<ISO-4217-code>` (3-letter) | `'USD'`, `'EUR'` |
| `unitofmeasure` | Unit name | `'kg'`, `'each'` |
| `dimension` | Dimension name (UCUM dimension registry) | `'mass'`, `'length'` |

### Content validation

Content validation is compile-time. When the type's `ITypedConstantValidator` is registered, malformed content is a compile error:
- `'XYZ 100.00'` in a `money` context → error (XYZ is not a recognized ISO 4217 code)
- `'5 invalidunit'` in a `quantity` context → error (not in UCUM or entity-scoped registry)
- `'1.08 USD/FAKE'` in an `exchangerate` context → error (FAKE is not ISO 4217)

Bare-identifier typed constants (`currency`, `unitofmeasure`, `dimension`) are purely context-born — `'USD'` is only born as `currency` when the expression context expects it. This parallels how `42` is only born as `integer` when the context expects it.

### Integer requirement — scoped to temporal only

The temporal proposal's integer requirement (Decision #28: `'0.5 days'` is a compile error) applies only to temporal unit names. Non-temporal quantities accept non-integer magnitudes because their backing types accept them: `'2.5 kg'` is valid, `'100.50 USD'` is valid.

### Scalar literal type resolution

Plain numeric literals (e.g. `2.5`, `0.0825`) are context-sensitive — their resolved type depends on the surrounding expression. This is the Issue #115 context-sensitive literal typing contract applied to business-domain types.

| Context | Literal resolves to |
|---|---|
| Co-operand is a business-domain type (`money`, `quantity`, `price`, `exchangerate`) | `decimal` |
| Assignment target is a `decimal`-backed field | `decimal` |
| `default <number>` on a `decimal`-backed type declaration | `decimal` (magnitude component) |
| Co-operand is `number` or unannotated context | `number` |

This resolution is what makes `amount * 2.5` work when `amount` is `money` — the literal `2.5` is born as `decimal` from the context, satisfying D12's scalar operand contract without any explicit conversion or `as decimal` annotation.

```precept
field Tax as money in 'USD'
field TotalCost as money in 'USD'

set Tax = TotalCost * 0.0825    # ✓ 0.0825 resolves as decimal (co-operand is money)

field Rate as number
set Tax = TotalCost * Rate       # ✗ Rate is number — see D12, not decimal
```

The non-ambiguity invariant (Issue #115) guarantees exactly one resolution per literal per context. A literal that would resolve differently depending on evaluation order is a compile error.

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

The seven business-domain types reuse the existing field-constraint vocabulary from the language specification. No new constraint keywords are introduced. The constraint system's desugaring, compile-time diagnostics (C57/C58/C59), and proof-engine integration (C94–C98 via `NumericInterval.FromConstraints()`) apply uniformly.

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

**`maxplaces` and ISO 4217:** `maxplaces` is available on all four magnitude types. `money in '<currency>'` carries an **implicit `maxplaces`** derived from ISO 4217 minor units (D10): `USD` → `maxplaces 2`, `JPY` → `maxplaces 0`, `BHD` → `maxplaces 3`. An explicit `maxplaces` on the field overrides the ISO default. No other magnitude type (`quantity`, `price`, `exchangerate`) has an implicit `maxplaces` — all other uses must be declared explicitly. Authors who need non-standard precision for `money` (e.g., forex platforms requiring 6 decimal places for USD) declare the override explicitly: `field Rate as money in 'USD' maxplaces 6`.

**`maxplaces` enforcement scope:** `maxplaces` is checked at three points:
1. **Literal assignment (compile time):** `set Cost = '1.999 USD'` where `Cost` is `money in 'USD'` is a compile-time error — the literal's decimal places are statically known.
2. **Event-arg input (runtime boundary):** `precept_fire`/`precept_update` validate event arg values at the input boundary. `{ "Amount": "1.999 USD" }` for a `maxplaces 2` field is rejected before the engine runs.
3. **Arithmetic result assignment (runtime):** `set Cost = UnitPrice * Qty` where `UnitPrice = '1.333 USD/each'` and `Qty = '3 each'` produces exactly `'3.999 USD'` (exact `decimal` arithmetic — see D12). If `Cost` is `money in 'USD'` (implicit `maxplaces 2`), this is a constraint violation at `set` time. The author must apply `round()` explicitly: `set Cost = round(UnitPrice * Qty, 2)`.

**Optional interaction:** Business-domain magnitude types follow the same optional pattern as `decimal` and `number`, with one addition: extracting a component (`.amount`, `.currency`, `.unit`) from an optional field without a null guard is a compile error.

- `field Payment as money optional in 'USD'` — null requires a null guard before arithmetic: `when Payment != null`.
- At the evaluator level: a null business value is `null` in the data dictionary. The evaluator must not attempt `decimal` extraction from a null entry — this would produce a `NullReferenceException` in the backing value. Same enforcement as `number optional` arithmetic today.
- `maxplaces` on optional fields: the desugared constraint gains a null guard — `Field == null or Field.amount.DecimalPlaces <= N`. A null value always passes the `maxplaces` check (the field is absent, not violating).
- `in` and `of` constraints on optional fields: same pattern — `Field == null or <in/of check>`. Null passes; present values are validated normally.

**`nonzero` contract for magnitude types:** `nonzero` checks that the `decimal` magnitude component is not `0m`. C# `decimal` has no negative zero — `decimal.Negate(0m) == 0m` is true, so `nonzero` on a business type rejects any value where `.amount == 0m`. Component strings are unaffected. `'0 USD'` fails `nonzero`; `'0.001 USD'` passes regardless of `maxplaces`.

Constraint interaction: `nonzero` and `maxplaces` are evaluated independently. `'0.001 USD'` on a field with both `nonzero` and `maxplaces 2`: `nonzero` passes (magnitude ≠ 0); `maxplaces 2` fails (3 decimal places). Both diagnostics fire.

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
| Dimensionless (plain scalars) | `number` | `5 * 3` |
| Dimensionless (business-type ratio) | `decimal` | `'100 USD' / '50 USD'` → `2` |
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

For business-domain types, comparison operators carry domain preconditions. **Cross-unit comparisons are compile errors, not `false`** — the same rule that makes cross-currency arithmetic a compile error (D11) applies to cross-currency equality.

| Type | Equality (`==`, `!=`) | Ordering (`<`, `>`, `<=`, `>=`) | Constraint |
|---|---|---|---|
| `money` | ✓ | ✓ | Same currency required — `'100 USD' == '100 EUR'` is a **compile error** |
| `currency` | ✓ | ✗ | — |
| `quantity` | ✓ | ✓ | Same dimension required; auto-converts within dimension — `'1 kg' < '500 g'` is valid (and true); `'5 kg' == '5 m'` is a **compile error** |
| `unitofmeasure` | ✓ | ✗ | — |
| `dimension` | ✓ | ✗ | — |
| `price` | ✓ | ✓ | Same currency AND same denominator unit required — `'4 USD/each' == '4 EUR/each'` is a **compile error** |
| `exchangerate` | ✓ | ✗ | Same currency pair required — `'1.08 USD/EUR' == '1.08 USD/GBP'` is a **compile error** |

### Cross-type arithmetic: what's NOT allowed (and why)

| Expression | Why not |
|---|---|
| `money + quantity` | Currencies and physical units are incompatible dimensions. |
| `money * number` | Business-domain types require exact `decimal` scalars. `number` (backed by `double`) would contaminate the `decimal` chain. Change the scalar to `as decimal`. |
| `quantity * number` | Same — `number` cannot be a scalar operand for `decimal`-backed types. |
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
  | `price` | `.currency`, `.unit`, `.dimension` | `when X.currency == 'USD'` | `$eq:X.currency:USD` |
  | `exchangerate` | `.from`, `.to` | `when X.from == 'USD'` | `$eq:X.from:USD` |
- **Alternatives rejected:** (A) Require `in`/`of` on every field — eliminates open-field use cases. (B) Allow open fields but block all arithmetic. (C) Runtime validation — violates philosophy.
- **Precedent:** Same contract as optional narrowing. Same friction, same reason.
- **Tradeoff accepted:** Authors who use open fields must write guards.

### D10. ISO 4217 default precision as implicit `maxplaces` for `money`

- **What:** `money in 'USD'` carries an implicit `maxplaces 2` derived from ISO 4217 minor units. `money in 'JPY'` carries `maxplaces 0`. `money in 'BHD'` carries `maxplaces 3`. An explicit `maxplaces` on the field overrides the ISO default. The implicit `maxplaces` is a **validation constraint, not an auto-rounding rule** — assigning `'1.999 USD'` to a field with `maxplaces 2` is a constraint violation (compile-time for literal assignments, at the fire/update boundary for event-arg inputs, and at `set` time for arithmetic results), not a silent truncation to `'2.00 USD'`. When the author wants rounding, they apply it explicitly. Default rounding mode for explicit rounding operations: half-even (banker's rounding).
- **Why:** ISO 4217 defines the natural precision. Making it an implicit `maxplaces` means authors get correct precision by default without annotating every field, while preserving Precept's prevention guarantee — no silent data loss. Half-even eliminates systematic bias when authors do round.
- **Alternatives rejected:** (A) Auto-round to ISO precision — hides precision loss, violates prevention guarantee. (B) `maxplaces` required on every money field — boilerplate when ISO already provides the answer. (C) Half-up rounding — upward bias. (D) No default precision — money fields accept arbitrary decimal places, defeating the purpose of ISO 4217 awareness.
- **Precedent:** NMoneys, Java `Currency.getDefaultFractionDigits()`, Python `decimal`.
- **Tradeoff accepted:** Non-standard precision requires explicit `maxplaces` override. Authors who need sub-cent precision (e.g., gas station pricing at `maxplaces 3`) must declare it. Arithmetic expressions can produce results with more decimal places than the target field allows (exact `decimal` arithmetic does not auto-round) — authors who compute monetary results should apply `round()` before assigning to a `maxplaces`-constrained field.

### D11. Cross-currency `money` arithmetic requires explicit `exchangerate`

- **What:** `money in 'USD' + money in 'EUR'` is a compile-time type error. Convert via `exchangerate` multiplication.
- **Why:** Physical unit conversions are fixed constants. Currency exchange rates are volatile external data. Auto-conversion would require ambient external state.
- **Alternatives rejected:** (A) Auto-convert using exchange rate field — implicit coupling. (B) Mixed-currency arithmetic with runtime error — violates prevention guarantee.
- **Tradeoff accepted:** Currency conversion is more verbose than quantity conversion. This reflects real-world complexity.

### D12. `decimal` backing for all seven types

- **What:** All seven types use `decimal` as magnitude backing, not `double`. Scalar operands in business-domain operator tables must also be `decimal` (not `number`), enforcing an unbroken `decimal` chain from operands through results.
- **Why:** The result-type algebra demands homogeneous backing. `price * quantity → money` — if any type uses `double`, cross-type operations hit `decimal ÷ double` boundaries with `double`-precision artifacts. `0.1 + 0.2 ≠ 0.3` is unacceptable for business arithmetic. The scalar operand rule follows directly: if `money` is `decimal`-backed but `money * number` means `decimal × double`, the D12 guarantee is violated at the operator boundary.
- **Scalar operand contract:** All scaling and ratio operations use `decimal` as the scalar type. `money * decimal → money`, `quantity / decimal → quantity`, `money / money → decimal`. The `number` type (backed by `double`) is rejected as a scalar operand for business-domain types — the type checker emits a teachable diagnostic directing the author to use `decimal`. `integer` widens to `decimal` losslessly, so `Amount * 2` works without friction. **Division precision:** `money / money` and `quantity / quantity` produce a full 28-digit `decimal` result. Scale is not implicitly capped — `'1.00 USD' / '3.00 USD'` yields `0.333...` to 28 digits. Authors must apply `round()` before assigning a ratio result to any `maxplaces`-constrained field.
- **Context-sensitive literal typing:** Fractional literals (`2.5`, `0.0825`) resolve to `decimal` or `number` based on expression context. When the co-operand or assignment target is a `decimal`-backed business type, the literal is born as `decimal`. When the context is `number`, the literal is born as `double`. No new syntax is needed — every expression in Precept has a deterministic context. See [Scalar literal type resolution](#scalar-literal-type-resolution) for the full resolution table and the non-ambiguous inference invariant.
- **Alternatives rejected:** (A) `double` for `quantity` — breaks algebra. (B) `double` for all — loses base-10 precision. (C) Mixed backing with promotion — explodes complexity. (D) Keep `number` in operator tables, coerce to `decimal` at boundary — hidden coercion contradicts Precept's inspectability philosophy. (E) Accept the inconsistency — violates D12's own stated guarantee.
- **Precedent:** NMoneys, .NET `System.Currency` proposals, all C# financial libraries use `decimal`. Type-directed literal resolution is standard practice (C# target-typed `new()`, Kotlin/Swift numeric literals).
- **Research:** Analysis grounding this decision covered language design implications, runtime audit of decimal-vs-double arithmetic paths, and author-facing impact assessment (0/25 sample breakage).
- **Tradeoff accepted:** UCUM conversion factors become `decimal` constants. Authors who have `field Rate as number` and want to multiply by a `money` field must change the declaration to `as decimal` — the teachable error message explains why. This is a one-time migration cost that prevents silent precision loss in every subsequent operation.

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

**Enforcement mechanism:** The `QualifierMismatch` diagnostic enforces `in` constraints at compile time using the proven-violation-only policy (same principle as the proof engine's interval diagnostics which apply to numeric range violations). Three enforcement tiers:

1. **Literals with statically-known content:** `set CostUsd = '100 EUR'` where `CostUsd` is `money in 'USD'` — the compiler resolves the literal's currency to EUR, proves it violates the USD constraint, and emits `QualifierMismatch` as a compile-time error. Same for `set MonthsField = '30 days'` against `period in 'months'`.
2. **Expressions with guard-narrowed proof:** `when Payment.currency == 'USD'` seeds a `$eq:Payment.currency:USD` proof marker. An assignment to `money in 'USD'` succeeds because the proof engine can verify the constraint is satisfied. Without the guard, `QualifierMismatch` fires (unproven — open field assigned to constrained field).
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
- **Period single-basis cancellation rule:** A `period` cancels a single-unit time denominator **only when the period has a single matching basis**. `period in 'hours'` cancels `price in 'USD/hours'`. `period in 'hours&minutes'` does **not** cancel `price in 'USD/hours'` — it is a compile error. NodaTime stores period components separately (`.Hours`, `.Minutes`, `.Seconds`) with no native `TotalHours` conversion. Converting a multi-basis period to a single unit would require Precept-invented arithmetic that NodaTime deliberately refuses. The author must decompose first: extract the hours component or use a single-basis period. `CompoundPeriodDenominator` enforces this — a compound period assignment to a single-unit denominator context is a proven constraint violation.
- **Duration is exempt from this restriction.** `Duration` is a single scalar (nanoseconds internally) — `duration.ToInt64Nanoseconds()` always yields an exact conversion to any time unit. There is no multi-basis ambiguity. `duration` cancels any fixed-length time denominator (`hours`/`minutes`/`seconds`) regardless of how the duration was constructed.
- **Date-component denominators remain period-only.** `days`, `weeks`, `months`, `years` denominators cancel only with `period`, and follow the same single-basis rule: `period in 'months'` cancels `price in 'USD/months'`, but `period in 'months&days'` does not — because NodaTime cannot convert "2 months + 15 days" into a pure months count without a reference date.
- **Alternatives rejected:** (A) UCUM time units — requires translation table. (B) Time spans as `quantity` — dead end #6. (C) No cancellation — makes `HourlyRate * HoursWorked` impossible. (D) `period`-only — blocks `instant - instant → duration` from compound arithmetic. (E) Compiler warning on dual paths — second-guessing the author's deliberate type choice.
- **Precedent:** NodaTime separates `Duration` (fixed elapsed) from `Period` (calendar distance). The temporal proposal preserves this. The boundary is faithful to NodaTime's model.
- **Tradeoff accepted:** UCUM time units not valid in denominators. Minor vocabulary restriction for zero-translation cancellation.
- **Implementation note:** Duration scalar extraction via `(decimal)duration.ToInt64Nanoseconds() / <nanoseconds-per-unit>m` — NOT `Duration.TotalHours` (`double`). `long → decimal` is always exact.

### D16. Business-domain magnitude types inherit Precept `decimal` semantics by default — domain identity justifies specific exceptions

- **What:** All business-domain magnitude types (`money`, `quantity`, `price`, `exchangerate`) are domain-wrapped `decimal`s. Every operation valid for Precept `decimal` is valid for these types by default — on the magnitude side — unless a domain rule explicitly overrides it. The burden of proof is on exclusions, not inclusions.

  **Governance hook:** Any addition to Precept's `decimal` operation surface requires a D16 exception review before shipping. Implementors adding a new `decimal` operation must check: is this operation correct for all four business-domain types, or does at least one type require an exception? If so, update the exception table. Without this check, the "silent inheritance" default becomes a source of semantically invalid operations passing the type checker.

  **Inherited by default** (domain preconditions documented where they differ from plain `decimal`):

  | Operation / modifier | Applies to | Domain precondition / notes |
  |---|---|---|
  | Unary `-` | `money`, `quantity`, `price` | None — magnitude negation; unit/currency identity preserved in result. Blocked for `exchangerate` — see exception table. |
  | `abs`, `round` | `money`, `quantity`, `price` | Return type is the same domain type as input (see Corollary 1 below). `abs(exchangerate)` → see exception table. |
  | `floor`, `ceil`, `truncate` | `money`, `quantity` | No standard business domain meaning on `price` or `exchangerate` — see exception table. For `money`: floors/ceils to integer magnitude (always safe for `maxplaces N ≥ 0`). |
  | `clamp` | `money`, `quantity`, `price` | Bounds must be the same domain type as the clamped value, satisfying the same unit/currency compatibility as comparison operators for that type. `clamp(Qty, '0 kg', '100 kg')` is valid; `clamp(Qty, 0, 100)` is not — bare numbers are not `quantity`. |
  | `min(A, B)` / `max(A, B)` selection functions | `money`, `quantity`, `price` | Same ordering preconditions as `<`/`>`/`<=`/`>=` for that type (see bottom row). Return type is the same domain type as the operands. Blocked for `exchangerate` — see exception table. |
  | `positive`, `nonnegative`, `nonzero` field constraints | All four | `exchangerate` carries an implicit `positive` — see Corollary 2. Explicitly declaring `positive` or `nonzero` on an `exchangerate` field is redundant. |
  | `min N` / `max N` field constraints (lower/upper bound at declaration) | `money`, `quantity`, `price` | Bound constant `N` must be the same domain type as the field, with matching unit/currency. Blocked for `exchangerate` — these constraints require `>=`/`<=` comparison, which is not defined for `exchangerate`; use `positive` instead. |
  | `maxplaces N` field constraint | All four | Only `money` carries an implicit ISO 4217 default (D10). For `quantity`, `price`, and `exchangerate`, `maxplaces` is an explicit-only constraint — no implicit default applies. |
  | `==`, `!=` | All four | **Domain precondition:** same currency / unit / currency-pair required. Cross-unit `==` is a **compile error**, not `false` — see exception table. |
  | `<`, `>`, `<=`, `>=` | `money`, `quantity`, `price` | `money`: same currency required. `quantity`: same dimension required — auto-converts within dimension per D8. `price`: same currency AND same denominator unit required. |

  **Corollary 1 — Return type preservation:** Arithmetic functions applied to a business-domain magnitude type return the same domain type as their magnitude-bearing argument, with all unit, currency, and compound-unit annotations preserved. `abs('−100 USD') → money in 'USD'`; `round(UnitPrice, 2) → price in 'USD/each'` when `UnitPrice` is `price in 'USD/each'`; `clamp(Qty, '0 kg', '100 kg') → quantity in 'kg'`. If the result type did not preserve domain identity, subsequent assignment to a `money` or `quantity` field would be a type error — the design already assumes type-preservation in D10's `round()` examples.

  **Corollary 2 — `exchangerate` carries an implicit `positive` constraint:** Exchange rates are always positive by domain rule. A zero rate (`'0.000 USD/EUR'`) silently converts any amount to zero — a degenerate result indistinguishable from a modeling error. A negative rate has no economic meaning. `exchangerate` therefore carries an implicit `positive` constraint analogous to `money in 'USD'` carrying an implicit `maxplaces 2` via D10. Explicitly declaring `positive` or `nonzero` on an `exchangerate` field is redundant (compiler may warn). The implicit constraint is enforced at the same tiers as `in`-constraint enforcement (D14): literal assignment (compile), event-arg input (runtime boundary), and `set` time (runtime).

  **Exceptions — domain identity overrides:**

  | Plain `decimal` operation | Business-type exception | Governing rule | Reason |
  |---|---|---|---|
  | `decimal * decimal → decimal` | `money * money → compile error` | D2 | Money-squared has no business identity |
  | `decimal + decimal → decimal` | `money + money` (different currencies) `→ compile error` | D11 | Exchange rates are volatile; implicit conversion is not structurally safe |
  | `decimal / decimal → decimal` | `money / money → decimal` (same currency) | D2 | Ratio stays in the exact chain ✓ — this is an **inherited** result, not an override |
  | `decimal / decimal → decimal` | `money / money → exchangerate` (different currencies) | D2 | Result has named dimensional identity |
  | `decimal / decimal → decimal` | `money / quantity → price` | D2 | Result has named dimensional identity |
  | `decimal * decimal → decimal` | `money * number → compile error` | D12 | `number` is `double`-backed; mixing with `decimal`-backed type breaks the exact chain |
  | Unary `-decimal → decimal` | `-exchangerate → compile error` | D2 | Exchange rates are directional currency ratios, not signed quantities; a negative `USD/EUR` rate has no economic meaning |
  | `abs(decimal) → decimal` | `abs(exchangerate) → compile error` | D2 | Since `-exchangerate` is impossible by domain rule, `abs` is a no-op at best and misleading at worst — its only meaningful input (a negative value) can never exist for `exchangerate` |
  | `decimal == decimal → boolean` | `money == money` (different currencies) `→ compile error` | D11 | Cross-currency equality is a structural type mismatch, not `false`; `'100 USD' == '100 EUR'` has no defined meaning — the types are incompatible |
  | `decimal == decimal → boolean` | `quantity == quantity` (different dimensions) `→ compile error` | D8 | Cross-dimension equality is a structural type mismatch; `'5 kg' == '5 m'` is a compile error |
  | `decimal == decimal → boolean` | `price == price` (different currency or unit) `→ compile error` | D2 | `'4 USD/each' == '4 EUR/each'` — incompatible currency; `'4 USD/each' == '4 USD/kg'` — incompatible unit |
  | `decimal == decimal → boolean` | `exchangerate == exchangerate` (different currency pair) `→ compile error` | D2 | `'1.08 USD/EUR' == '1.08 USD/GBP'` — incompatible pairs |
  | `floor`/`ceil`/`truncate`(`decimal`) → `decimal` | `floor`/`ceil`/`truncate`(`price`) `→ compile error` | D2 | Floor/ceil/truncate on a currency-per-unit ratio has no standard business domain meaning; use `round` for precision management on `price` |
  | `floor`/`ceil`/`truncate`(`decimal`) → `decimal` | `floor`/`ceil`/`truncate`(`exchangerate`) `→ compile error` | D2 | Same rationale as `price`; `round(FxRate, N)` is the correct precision tool for exchange rates |
  | `<`, `>`, `<=`, `>=` | `exchangerate` ordering `→ compile error` | D2 | Exchange rates are point-in-time conversion factors, not ordered quantities; rate direction requires temporal context |
  | `min(decimal, decimal)` selection | `min`/`max` selection functions on `exchangerate → compile error` | D2 | Selection functions require ordering; `exchangerate` ordering is undefined (row above) |
  | `min N`/`max N` field constraints | `min`/`max` field constraints on `exchangerate → not applicable` | D2 | These constraints enforce `Field >= N` and `Field <= N`, which require ordering operators; use `positive` for lower-bound enforcement on `exchangerate` |

- **Why:** Without this principle, each type's operator table reads as a hand-crafted rule set with no governing logic. With it, every "not supported" row is an instance of a named domain exception, and every supported row is either inherited from `decimal` or promoted to a named type by the result-type algebra. The principle also enables correctness by induction: if a future contributor adds a new operation that is valid for `decimal` and does not trigger a domain exception, it is automatically valid for business-domain types — with the governance hook above ensuring exceptions are caught before they become implementation bugs.
- **Why now (Issue #115):** Before #115, the evaluator's `decimal` path was incomplete — the "decimal semantics by default" principle was aspirational. With #115 delivered (exact `decimal` arithmetic end-to-end, context-sensitive literal typing), the principle is a structural fact. The design must state it so the implementation can be verified against it.
- **Alternatives rejected:** (A) Enumerate every valid operation per type explicitly — produces 7× redundant table entries with no governing principle, and creates a "closed world" that blocks obvious extensions. (B) State "business types are just decimal" — collapses domain identity, hides the named exception set, violates D1. (C) Require per-operation justification — inverts the burden of proof; the domain exceptions are the thing that needs justification, not the default.
- **Precedent:** F# units of measure (operations on unit-annotated types inherit `float` operations unless specifically blocked by the unit algebra), Haskell `newtype` deriving (the wrapped type's operations are inherited unless explicitly overridden), Java `BigDecimal`-backed financial types in standard banking libraries.
- **Tradeoff accepted:** The "inherits by default" rule means contributors must check D16's exception table when adding new `decimal` operations or proposing new business-domain type constraints. The governance hook makes this check explicit. The exception table is the complete specification of domain overrides — it is not exhaustive if unchecked.

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
| [Issue #115](https://github.com/sfalik/Precept/issues/115) — Evaluator semantic fidelity | **Completed prerequisite.** Delivered: `decimal`-preserving arithmetic path, context-sensitive literal typing, non-ambiguous inference invariant, and Option A operator tables. Business-domain operator tables require `decimal` scalars (D12 scalar operand contract) and the runtime must honor the `decimal` lane end to end. |
| [Issue #106](https://github.com/sfalik/Precept/issues/106) — Proof engine | Provides the narrowing infrastructure (guard decomposition, marker injection, cross-branch accumulation) that discrete equality narrowing reuses. |
| [Issue #111](https://github.com/sfalik/Precept/issues/111) — `nonzero` constraint | Needed for `money / decimal` and `price / decimal` divisor safety. |
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
- **Typed-constant content remains opaque through the parser phase.** The parser preserves the literal text and span only. The type checker performs context-directed parsing and validation so diagnostics still attach to the literal span, but type identity is never pre-classified in the parser.

### Type checker changes

> **File placement (#118):** After the type checker decomposition (#118), new type-inference logic (operator tables, typed-constant content validation, dot-accessor resolution, dimensional cancellation) lands in `PreceptTypeChecker.DomainTypeInference.cs` via expected-type dispatch from `TryInferBinaryKind`. Discrete equality narrowing lands in `Narrowing.cs`. `in`/`of` constraint validation lands in `FieldConstraints.cs`. See #118's future-proofing analysis for the full per-file growth estimates.

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

**Evaluator dispatch contract — business-domain binary operators:**

The evaluator must dispatch business-domain arithmetic via typed value objects, not raw `decimal` extraction. The correct dispatch chain for a binary expression `Left op Right` where both operands are business-domain types:

1. **Evaluate** both operands to their runtime value objects (`MoneyValue`, `QuantityValue`, `PriceValue`, `ExchangeRateValue`).
2. **Validate** domain preconditions (currency match, unit-dimension match, denominator match) and surface a `ConstraintViolation` if they fail — do NOT throw.
3. **Compute** via the typed value object's operator overload (e.g., `MoneyValue.operator+(MoneyValue, MoneyValue)`). This keeps `decimal` arithmetic inside the type boundary.
4. **Return** the typed result value object. The evaluator MUST NOT unwrap to `decimal` and re-wrap manually — doing so loses currency/unit identity.

For scalar operands (`decimal` co-operand): the scalar is extracted first; the business-domain operand's operator overload accepts the `decimal` directly. Scalar extraction from an optional business value without a null guard is a precondition failure (see Optional contract above).

**Door 2 — `precept_update` deserialization contract:**

`precept_update` accepts field values as JSON strings (the "Door 2" input path). Business-domain types must be deserialized from their canonical string form before entering the evaluator:

| Type | Canonical string form | Parse contract |
|------|-----------------------|----------------|
| `money` | `"100.00 USD"` | `<decimal> <ISO-4217>` — exactly one space, uppercase currency code |
| `quantity` | `"5.0 kg"` | `<decimal> <UCUM-atom>` — exactly one space, lowercase unit name |
| `price` | `"4.17 USD/each"` | `<decimal> <ISO-4217>/<unit-atom>` — no space around `/` |
| `exchangerate` | `"1.08 USD/EUR"` | `<decimal> <ISO-4217>/<ISO-4217>` — no space around `/` |
| `currency` | `"USD"` | Plain uppercase ISO 4217 code |
| `unitofmeasure` | `"kg"` | Plain UCUM atom (lowercase) |
| `dimension` | `"mass"` | Plain dimension name (lowercase) |

Parse failure returns a `QualifierMismatch`/`DimensionCategoryMismatch` diagnostic (depending on which constraint is violated) at the `precept_update` call site — never a thrown exception. Unknown currency codes (not in ISO 4217 registry) and unknown unit names (not in UCUM registry or entity-scoped units block) are treated as parse failures.

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

---

## Diagnostic Code Reference

This proposal introduces business-domain diagnostic codes in the `DiagnosticCode` enum. These use the same symbolic naming convention as the core diagnostic system (`diagnostic-system.md`) — no numeric code prefixes. The exact `DiagnosticCode` → `DiagnosticMeta` wiring happens in the `Diagnostics` exhaustive switch during implementation; these are the category assignments.

| Code | Phase | Condition | Triggering example |
|---|---|---|---|
| `QualifierMismatch` | Compile / Runtime boundary | `in` constraint violation — assigned value's currency or unit does not match the field's declared `in` qualifier | `set CostUsd = '100 EUR'` against `money in 'USD'`; open field assigned to `in`-constrained field without dimension-equality proof |
| `DimensionCategoryMismatch` | Compile / Runtime boundary | `of` constraint violation — assigned value's dimension does not match the field's declared `of` category | `set F = '5 kg'` against `quantity of 'length'`; open `quantity` field assigned to `quantity of 'length'` without `when F.dimension == 'length'` proof |
| `CrossCurrencyArithmetic` | Compile | Cross-currency arithmetic — `money` values with different currencies used in a single arithmetic expression | `CostUsd + CostEur` |
| `CrossDimensionArithmetic` | Compile | Cross-dimension arithmetic — `quantity` values with incompatible dimensions in an arithmetic expression | `'5 kg' + '3 mi'` (mass ≠ length) |
| `DenominatorUnitMismatch` | Compile | Denominator unit mismatch — the denominator of a `price` or compound `quantity` does not match the operand's unit | `price in 'USD/kg' * quantity in 'mi'` |
| `DurationDenominatorMismatch` | Compile | `duration` against variable-length time denominator — `duration` cannot cancel `days`, `weeks`, `months`, or `years` denominators (D15) | `price in 'USD/days' * duration` |
| `CompoundPeriodDenominator` | Compile | Compound period against single-unit denominator — `period in 'hours&minutes'` cannot cancel against a rate whose denominator is a single time unit | `period in 'hours&minutes' * price in 'USD/hours'` |
| `MutuallyExclusiveQualifiers` | Parse | `in` and `of` on the same field declaration — mutually exclusive | `field X as quantity in 'kg' of 'mass'` |
| `InvalidUnitString` | Compile / Runtime boundary | Invalid unit string for `unitofmeasure` field — structural characters (`/`, `*`) are not valid in an atomic unit value | `set SelectedUnit = 'kg/m'` |
| `InvalidCurrencyCode` | Compile / Runtime boundary | Invalid ISO 4217 currency code | `'USDX'` used as a currency literal or `currency` field value |
| `InvalidDimensionString` | Compile / Runtime boundary | Invalid dimension string — value is not a recognized UCUM dimension category; common case is passing a unit name (`'meters'`) where a dimension name (`'length'`) is required | `set AllowedDim = 'meters'` for `field AllowedDim as dimension` |
| `MaxPlacesExceeded` | Compile / Runtime | `maxplaces` constraint violation — assigned value has more decimal places than allowed; fires at literal assignment (compile), event-arg input (runtime boundary), and arithmetic result `set` (runtime) | `'1.999 USD'` assigned to `money in 'USD'` (implicit `maxplaces 2`); `set Cost = round_result` where result has 3 places |

**Phase key:**
- `Compile` — type checker emits at parse/compile time; caught before fire/run
- `Runtime boundary` — validated at `precept_fire`/`precept_update` input before the engine runs
- `Runtime` — evaluated during `set` execution inside the engine
