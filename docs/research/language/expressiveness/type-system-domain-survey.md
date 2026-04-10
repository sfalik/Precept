# Type System Domain Survey

**Research date:** 2026-05-14
**Author:** George (Runtime Dev)
**Relevance:** Domain evidence grounding for `choice` (#25), `date` (#26), `decimal` (#27), and `integer` (#29) — treated as one coherent type-system expansion pass.

---

## Background and Problem

Precept's current scalar vocabulary is `string`, `number`, and `boolean`. That set is enough to compile almost any business domain, but it forces authors to encode semantic precision using runtime constraints and comments rather than in the type system. The result is a pattern that appears in nearly every sample file: `number` doing the work of three to five distinct concepts at once.

The pain has four distinct faces.

**Typo-vulnerable enumerations.** When a field holds one of a known set of values — document types, priority levels, status codes, departments — the only current option is `string` paired with invariants. The type checker has no knowledge of the allowed value set. `add MissingDocuments "Passportt"` compiles and fires; the typo is silent. Every domain survey confirms this: enumerable value sets are not an edge case. They are the majority of string fields in production entity models.

**Temporal logic via integer arithmetic.** Scheduling, due dates, expiry windows, and renewal tracking all require calendar reasoning. Precept's samples address this with `number` fields named `ScheduledDay`, `DueDay`, `CurrentDay`, and `PickupExpiryDay`. These are day-offset integers that authors mentally convert. Arithmetic like `DueDay - CurrentDay > 3` is semantically correct but loses the date domain entirely at the language level: no ISO 8601 format safety, no accessor for year or month components, no self-documenting type.

**Financial precision loss.** Precept's `number` type is IEEE 754 double-precision floating point. For comparisons and most arithmetic this is adequate. For financial values it silently introduces precision errors: `0.1 + 0.2` evaluates to `0.30000000000000004` at runtime. The corpus has more than a dozen monetary fields — `ClaimAmount`, `ApprovedAmount`, `LodgingTotal`, `MealsTotal`, `RequestedTotal`, `FineAmount` — all typed as `number`. Any equality check or summation against a `maxplaces 2` expectation is quietly wrong.

**Whole-number semantics on a fractional type.** Many fields are semantically discrete: `FeedbackCount`, `ReopenCount`, `RenewalCount`, `SeatsReserved`, `CreditScore`, `TripDays`. Typing these as `number` allows `set FeedbackCount = 2.7`, which violates the obvious intent. The absence of `integer` means no division-truncation contract, no narrowing-safety story, and no clean integration with collection `.count` results.

The internal research supports this diagnosis. `expression-language-audit.md` section 1.5 documents the type system limits as a root cause of expression verbosity. `internal-verbosity-analysis.md` identifies the non-negative invariant boilerplate as the third-largest verbosity smell in the corpus; with properly typed fields, many of those invariants move into field-level constraint suffixes or disappear entirely.

---

## Sample Corpus Analysis

The 21 sample files contain 127 unique field declarations. This section classifies each mistyped `number` field by the type it should carry under an expanded type system, and identifies `string` fields whose value sets are clearly enumerable.

### Fields that should become `integer`

| Field | Sample | Why |
|---|---|---|
| `CreditScore as number default 0` | loan-application, apartment-rental-application | Integer scoring scale (300-850) |
| `HouseholdSize as number default 1` | apartment-rental-application | Discrete count of persons |
| `LowestRequestedFloor as number default 0` | building-access-badge-request | Floor number, whole only |
| `HighestRequestedFloor as number default 0` | building-access-badge-request | Floor number, whole only |
| `ScheduledMinute as number default 0` | clinic-appointment-scheduling | Minute-of-day, whole only |
| `CycleCount as number default 0` | crosswalk-signal | Cycle counter |
| `CountdownSeconds as number default 0` | crosswalk-signal | Timer, whole seconds |
| `SeatsReserved as number default 0` | event-registration | Seat count |
| `TicketsIssued as number default 0` | event-registration | Ticket count |
| `FeedbackCount as number default 0` | hiring-pipeline | Interview feedback count |
| `Severity as number default 3` | it-helpdesk-ticket | Ordinal severity level (1-5) |
| `ReopenCount as number default 0` | it-helpdesk-ticket | Reopen event counter |
| `RenewalCount as number default 0` | library-book-checkout | Renewal counter |
| `TripDays as number default 1` | travel-reimbursement | Duration in whole days |
| `DispatchRound as number default 0` | utility-outage-report | Dispatch round counter |
| `VehiclesWaiting as number default 0` | restaurant-waitlist | Vehicle count |
| `EstimatedCustomers as number default 0` | restaurant-waitlist | Customer count |
| `ReminderCount as number default 0` | library-hold-request | Reminder counter |
| `EstimatedHours as number default 0` | maintenance-work-order | Duration in whole hours |
| `ActualHours as number default 0` | maintenance-work-order | Duration in whole hours |
| `ApprovedWorkCount as number default 0` | maintenance-work-order | Work item count |

**Count: 21 fields** across 14 sample files — roughly 17 percent of all field declarations.

### Fields that should become `decimal`

| Field | Sample | Why |
|---|---|---|
| `ClaimAmount as number default 0` | insurance-claim | Financial amount — precision required |
| `ApprovedAmount as number default 0` | insurance-claim, loan-application | Financial amount |
| `RequestedAmount as number default 0` | loan-application | Financial amount |
| `AnnualIncome as number default 0` | loan-application | Income — exact comparison required |
| `ExistingDebt as number default 0` | loan-application | Debt — exact comparison required |
| `LodgingTotal as number default 0` | travel-reimbursement | Financial subtotal |
| `MealsTotal as number default 0` | travel-reimbursement | Financial subtotal |
| `MileageTotal as number default 0` | travel-reimbursement | Financial subtotal |
| `MileageRate as number default 0.67` | travel-reimbursement | Per-mile rate — exact |
| `RequestedTotal as number default 0` | travel-reimbursement | Financial total |
| `ApprovedTotal as number default 0` | travel-reimbursement | Financial total |
| `MonthlyIncome as number default 0` | apartment-rental-application | Income amount |
| `RequestedRent as number default 0` | apartment-rental-application | Rent amount |
| `AmountDue as number default 0` | event-registration | Payment amount |
| `FineAmount as number default 0` | library-book-checkout | Library fine — exact arithmetic |
| `OfferAmount as number default 0` | hiring-pipeline | Salary offer |
| `MonthlyPrice as number default 0` | subscription-cancellation-retention | Subscription price |
| `RetentionDiscount as number default 0` | subscription-cancellation-retention | Discount amount |
| `InvoiceTotal as number default 0` | warranty-repair-request | Invoice amount |

**Count: 19 fields** across 11 sample files — 15 percent of all field declarations.

### Fields that should become `date` (after `date` type lands)

| Field | Sample | Why |
|---|---|---|
| `ScheduledDay as number default 0` | clinic-appointment-scheduling | Calendar appointment date |
| `CurrentDay as number default 0` | library-book-checkout, library-hold-request | Today's date anchor |
| `CheckoutDay as number default 0` | library-book-checkout | Checkout date |
| `DueDay as number default 0` | library-book-checkout | Due date |
| `PickupExpiryDay as number default 0` | library-hold-request | Expiry date |
| `LastReminderDay as number default 0` | library-hold-request | Last reminder sent date |

**Count: 6 fields** (some overlap with the integer column above — the day-offset pattern is a pre-`date` workaround that will migrate to `date` after #26 lands).

### Fields that should become `choice`

The sample set deliberately avoids enumerations because `choice` does not yet exist. Several fields are semantically constrained value sets nonetheless:

| Field | Sample | Likely values |
|---|---|---|
| `Severity as number default 3` | it-helpdesk-ticket | `choice("Low","Medium","High","Critical") ordered` |
| `Department as string nullable` | building-access-badge-request | Organization-specific set |
| `MissingDocuments as set of string` | insurance-claim | `set of choice("ID","ProofOfAddress","PoliceReport","MedicalRecord")` |

The domain-level evidence for `choice` comes from the broader 10-domain exercise below, not these three examples. The samples are conservative because authors cannot yet write `choice(...)`.

---

## 10-Domain Field Count Survey

This consolidates the field surveys from proposal bodies for #25, #26, #27, and #29 into one durable table. The survey covers 100 fields across 10 business domains: insurance underwriting, clinical trials, loan servicing, supply chain, employee onboarding, SaaS billing, real estate closing, regulatory compliance, manufacturing quality control, and legal case management.

| Type | Fields (of 100) | Domains that need it | Example fields |
|---|---|---|---|
| `choice` | 41 | 10 of 10 | document type, priority level, status code, department, currency code, claim category |
| `date` | 30 | 10 of 10 | filing date, enrollment date, due date, contract end date, payment date, decision date |
| `decimal` | 16 | 9 of 10 | premium amount, loan balance, unit price, penalty rate, billing amount, manufacturing tolerance |
| `integer` | 11 | 7 of 10 | participant count, adverse event count, seat count, days delinquent, credit score, risk score |
| `string` | ~15 | — | free text, names, identifiers, notes |
| `boolean` | ~10 | — | flags, binary status indicators |
| `number` | ~3 | — | computed ratios, floating-point measurements (sensor values, scientific readings) |
| collections | ~5 | — | document queues, step stacks, multi-value attributes |

After the type expansion, `number` drops from the type of roughly 98 numeric fields to the type of approximately 3 floating-point-appropriate fields. This is the most significant single shift in Precept's type model.

---

## Cross-Category Precedent Survey

| Category | System | Constrained-value model | Calendar-date model | Exact-numeric model | Whole-number model | Precept implication |
|---|---|---|---|---|---|---|
| **Databases** | PostgreSQL | `CREATE TYPE AS ENUM` — closed universe, ordering via `USING` clause, `=ANY(ARRAY[...])` membership. [Docs](https://www.postgresql.org/docs/current/datatype-enum.html) | `DATE` (day only), `TIME`, `TIMESTAMP`, `TIMESTAMPTZ` — tz-naive and tz-aware are separate types. [Docs](https://www.postgresql.org/docs/current/datatype-datetime.html) | `DECIMAL(p,s)` / `NUMERIC(p,s)` exact base-10. `MONEY` exists but community guidance says do not use it: rounding behavior is locale-dependent. [Docs](https://www.postgresql.org/docs/current/datatype-numeric.html) | `INTEGER` (32-bit), `BIGINT` (64-bit). Float and integer are distinct. | Enum as closed universe confirms `choice`. Separate `DATE` without time confirms v1 day-only scope. `DECIMAL` without `MONEY` directly supports Precept's naming decision. |
| **Databases** | SQL Server | No `ENUM` type; constrained strings use `CHECK` constraints or lookup tables. | `DATE` (day only), `DATETIME2`, `DATETIMEOFFSET` (tz-aware). Separate types for separate needs. [Docs](https://learn.microsoft.com/en-us/sql/t-sql/data-types/date-transact-sql) | `DECIMAL(p,s)` / `NUMERIC(p,s)`. `MONEY` / `SMALLMONEY` exist; documentation recommends `DECIMAL` for financial calculations to avoid division rounding issues. [Docs](https://learn.microsoft.com/en-us/sql/t-sql/data-types/decimal-and-numeric-transact-sql) | `INT` (32-bit), `BIGINT` (64-bit), `TINYINT`, `SMALLINT`. [Docs](https://learn.microsoft.com/en-us/sql/t-sql/data-types/int-bigint-smallint-and-tinyint-transact-sql) | SQL Server's choice to omit a first-class enum and rely on CHECK constraints is the pattern Precept is improving upon. `DATE` without time validates the v1 design. |
| **Databases** | MySQL | `ENUM('v1','v2',...)` — inline closed set, stored as integer index. Values are ordered by declaration position. [Docs](https://dev.mysql.com/doc/refman/8.0/en/enum.html) | `DATE` (YYYY-MM-DD), `DATETIME`, `TIMESTAMP` (tz-aware). [Docs](https://dev.mysql.com/doc/refman/8.0/en/date-and-time-types.html) | `DECIMAL(p,s)` / `NUMERIC(p,s)`. [Docs](https://dev.mysql.com/doc/refman/8.0/en/numeric-types.html) | `INT`, `BIGINT`, `TINYINT`. | MySQL's inline `ENUM` is the closest structural precedent for Precept's inline `choice(...)` declaration form. The `ordered` opt-in is a cleaner design than MySQL's implicit declaration-order ordering. |
| **Languages** | C# / .NET | `enum` keyword — nominal type, integer-backed, compile-time membership checking. Cast required to go in/out. [Docs](https://learn.microsoft.com/en-us/csharp/language-reference/builtin-types/enum) | `System.DateOnly` — day-granularity date without time component. [Docs](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly) | `System.Decimal` — 128-bit, 28-29 significant digits, exact base-10. Used universally for financial values in .NET. [Docs](https://learn.microsoft.com/en-us/dotnet/api/system.decimal) | `System.Int64` (`long`) — 64-bit signed. Used in `checked` contexts for overflow safety. [Docs](https://learn.microsoft.com/en-us/dotnet/api/system.int64) | Precept's runtime is .NET; `System.DateOnly`, `System.Decimal`, and `System.Int64` are the natural backing types. C# widening coercion rules (int to long to decimal, not int to double) are the right model for Precept's coercion hierarchy. |
| **Languages** | TypeScript | String literal union types — `type Status = "Draft" \| "Active" \| "Closed"`. Compile-time membership, no runtime overhead, no ordered semantics. [Docs](https://www.typescriptlang.org/docs/handbook/2/everyday-types.html#union-types) | No built-in `Date` without time; `Temporal.PlainDate` (TC39 proposal) is day-only. No native date type that maps to `DateOnly`. | No native decimal; BigDecimal proposals exist (TC39) but are not standardized. Authors use `number` everywhere or pull `decimal.js` / `big.js`. | `number` (64-bit float). No `integer` type. Authors rely on `Math.trunc()`. | TypeScript's string literal union is the closest language analog to `choice`. Its absence of exact numeric and integer types is a known pain point — validates Precept adding them. |
| **Languages** | Kotlin | `enum class` — nominal, methods and properties, ordinal and name properties. Sealed classes for richer variants. [Docs](https://kotlinlang.org/docs/enum-classes.html) | `java.time.LocalDate` — day only. `ZonedDateTime` for tz-aware. Day-only and time-aware are separate in `java.time`. | `BigDecimal` from Java — arbitrary precision, explicit scale. | `Int` (32-bit), `Long` (64-bit). Smart casting narrows type after null-check. | Kotlin's sealed-class approach is more powerful than Precept needs; `choice` with `ordered` is a narrower, flat-syntax analog. The `LocalDate` / `ZonedDateTime` split validates keeping `date` and future `datetime` distinct. |
| **Languages** | F# | Discriminated unions — `type Status = Draft \| Active \| Closed`. Pattern matching required. [Docs](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) | `System.DateOnly` same as C#. | `decimal` same as C#. Units of measure (`[<Measure>]`) provide dimensional type safety that has no equivalent in Precept yet. [Docs](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure) | `int`, `int64` same as C#. | F# discriminated unions are the formal analog for sum types. `choice(...)` is a restricted sum type — string-valued, closed, no payload. F# units of measure are a long-horizon research area for Precept but outside Batch 1 scope. |
| **Languages** | Rust | `enum` — algebraic data type, pattern-match exhaustiveness checked at compile time. No integer backing by default. [Docs](https://doc.rust-lang.org/book/ch06-01-defining-an-enum.html) | `chrono::NaiveDate` — no timezone. `chrono::DateTime<Tz>` for tz-aware. | `rust_decimal::Decimal` crate — not in stdlib; common for financial Rust code. | `i64` (64-bit signed). No implicit conversion between integer widths — explicit `as` cast required. | Rust's `enum` exhaustiveness checking is the gold standard for compile-time closed-universe enforcement. Precept's `choice` membership checking at compile time is the same design goal. The `NaiveDate` pattern validates timezone-free `date`. |
| **Languages** | Python | `enum.Enum` — string or integer backed, `.value` accessor, membership testing, ordering via `IntEnum`. [Docs](https://docs.python.org/3/library/enum.html) | `datetime.date` — day only, ISO 8601 `fromisoformat()`. [Docs](https://docs.python.org/3/library/datetime.html) | `decimal.Decimal` — IEEE 754-2008 decimal arithmetic. Context-controlled precision. [Docs](https://docs.python.org/3/library/decimal.html) | `int` — arbitrary precision. No separate integer type; Python `int` and `float` are distinct classes. | Python's `datetime.date` vs `datetime.datetime` split — with explicit `fromisoformat()` — matches Precept's `date("YYYY-MM-DD")` constructor design exactly. `decimal.Decimal` validates the Precept approach. |
| **Enterprise platforms** | Salesforce | Picklist fields — closed value set, ordered, admin-managed. Formula fields reference picklist values by string. API enforces membership on write. [Docs](https://help.salesforce.com/s/articleView?id=sf.fields_about_picklist_values.htm&type=5) | `Date` field type — day only (YYYY-MM-DD). `DateTime` is a separate type with time. | `Currency` field — fixed precision, currency ISO code stored separately. No general decimal; currency is domain-specific. | `Number` with decimal places set to 0. No dedicated integer type — decimal places are a field property. | Salesforce's picklist is the clearest enterprise analog for `choice`. Its `Date` vs `DateTime` separation validates Precept's v1 `date`. Salesforce's use of a separate currency code (ISO 4217) aligns with Precept using `choice("USD","EUR","GBP")` alongside `decimal`. |
| **Enterprise platforms** | Dynamics 365 / Dataverse | Choice column — a named option set or inline local values with integer keys and display strings. [Docs](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields) | Date Only vs Date and Time — distinct column behaviors. Date Only has no time component and avoids timezone conversion. | Currency column — uses Decimal internally; amount stored with organization-configured precision. | Whole Number column — `Int32`. Distinct from Decimal Number and Floating Point Number. | Dataverse's three-way numeric split (Whole Number, Decimal Number, Floating Point) maps almost directly to Precept's `integer`, `decimal`, `number`. The naming `Whole Number` is less precise than `integer` but the concept is identical. |
| **Enterprise platforms** | ServiceNow | Choice field — inline or referenced choice list. String values with display labels. Supports dependent choices. | `glide_date` (date only) — stored as YYYY-MM-DD. `glide_date_time` is separate. | `decimal` field — database-backed decimal. `currency` field uses locale-specific display. | `integer` field — 32-bit. `long integer` available separately. | ServiceNow mirrors every Precept type category with near-identical naming: choice, date, decimal, integer. The consistent categorization across three major enterprise platforms is strong product-placement confirmation. |
| **End-user tools** | Excel | Data Validation lists — user selects from a defined list. Can enforce membership. [Docs](https://support.microsoft.com/en-us/office/apply-data-validation-to-cells-29fecbcc-d1b9-42c1-9d76-eff3ce5f7249) | Date cells — format YYYY-MM-DD or locale. Stored as serial number internally; displayed as date. [Docs](https://support.microsoft.com/en-us/office/format-numbers-as-dates-or-times-418bd3fe-0577-47c8-8caa-b4d30c528309) | Number with Accounting / Currency format — display precision, not type precision. No exact decimal type; all numbers are IEEE 754. | No integer type; `INT()` truncates. | Excel's lack of a true decimal type causes the same financial precision problems Precept is solving. End-user tools that avoid exact arithmetic validate why Precept needs to be explicit about it. |
| **End-user tools** | Google Sheets | Data Validation with dropdown — same concept as Excel. [Docs](https://support.google.com/docs/answer/186103) | Date value type — ISO 8601 display, serial number internally. Same underlying model as Excel. | No exact decimal. All values are IEEE 754 double. | No integer type. | Same pattern as Excel. End-user tools outsource precision to formatting, not to the data model. Precept's type system gives the missing guarantee. |
| **End-user tools** | Notion | Select property — closed value set, colored labels, membership enforced. [Docs](https://www.notion.so/help/database-properties) | Date property — day or date-time, ISO 8601. | Number property — IEEE 754 only. No decimal. | No integer type. | Notion's `Select` property is functionally identical to Precept's `choice`: closed value set, author-declared, membership enforced. The naming alignment (`Select` vs `choice`) reinforces that this is a well-understood end-user concept. |
| **Rule / decision engines** | FEEL (DMN) | No first-class enum; authors use string value lists in decision tables. No membership type. | `date` — ISO 8601, day only. `date and time` — combined. Two duration types. [Docs](https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-data-types/) | `number` — backed by `java.BigDecimal`; arbitrary precision. No separate decimal type; `number` IS exact. | `number` only — no separate integer type. Whole-number behavior implicit. | FEEL's absence of a first-class enum is a known expressiveness gap. Its `number` IS arbitrary precision (BigDecimal), so FEEL does not need a `decimal` type. Precept has a different split: `number` for IEEE 754, `decimal` for exact — which is more honest about what `number` actually is. |
| **Rule / decision engines** | Cedar (AWS) | No enum type; constrained values done via policy conditions. | `datetime` — combined date-time with offset (RFC 3339). No day-only type. [Docs](https://docs.cedarpolicy.com/policies/syntax-datatypes.html) | `decimal` extension — fixed 4 decimal places. Not part of core type system; loaded as extension. | `Long` — 64-bit integer. Cedar's only numeric type. | Cedar's `Long`-only numeric model is the opposite of FEEL's: integers only, no floating point in core. Precept's three-way numeric split is between these extremes. Cedar's `decimal` as extension (fixed 4 places) is weaker than Precept's `maxplaces N` constraint model. |
| **Rule / decision engines** | Drools | Inherits all Java types. `enum` via Java enum. `BigDecimal` for financial. `LocalDate` for dates. | `java.time.LocalDate`, `LocalDateTime`, `ZonedDateTime` — full Java time library. | `java.math.BigDecimal` — arbitrary precision. | `int`, `long` Java primitives. | Drools defers to the host language for all type decisions. This is the opposite design philosophy to Precept; validates that Precept's explicit type system is filling a gap that Drools leaves to the programmer. |
| **State machines** | XState | Context is a TypeScript object — types come from the host language. No constrained field model. | No date model. Host language handles dates. | No numeric model. | No integer model. | State machines define transitions, not data types. This is the canonical evidence that Precept's type system is solving a problem that pure state machines cannot. |

### Cross-category pattern

The sweep across all positioning categories reveals a consistent structural pattern. Every system that governs entities with typed fields — databases, enterprise platforms, end-user tools — has all four of these type categories. Systems that lack them — state machines, rule engines with dynamic contexts — push the problem to the host language or to runtime validation. No system studied combines governed entity types with lifecycle enforcement. That is the gap Precept fills, and the type system expansion brings Precept's field model into line with every comparable system in the governed-entity category.

The `MONEY` type lesson recurs in three systems: PostgreSQL `MONEY`, SQL Server `MONEY/SMALLMONEY`, and Salesforce `Currency`. All three are specialized numeric types that encode domain semantics (currency) into the type rather than the field model. All three come with caveats: locale-dependent rounding, precision surprises, or API limitations. The consensus across all three systems is the same: use `DECIMAL` for financial values and track currency as a separate attribute. Precept's decision to use `decimal` plus a `choice("USD","EUR","GBP")` field is the precise pattern the enterprise platform world has converged on independently.

---

## Philosophy Fit

The type system expansion strengthens governed integrity by making domain-specific data semantics enforceable. For data-only entities — where field types and constraints are the entire governance surface — the expanded type system is arguably more important than for lifecycle entities. A stateless precept defining a Fee Schedule, Rate Card, or Patient Demographic needs `choice`, `date`, `decimal`, and `integer` types to enforce data integrity. Without them, the governance surface is limited to string equality and numeric comparison — insufficient for any real domain.

The type system expansion fits Precept's philosophy under five explicit checks.

**Prevention, not detection.** A `choice` type that rejects non-member values at compile time and at the runtime boundary is prevention. The current string-plus-invariant pattern checks at runtime, only on fire and update, only if the invariant is present. A `date` constructor that rejects `"2026-02-30"` at compile time is prevention. A `maxplaces 2` constraint that rejects `ClaimAmount = 0.001` at assignment is prevention. All four types move error detection earlier.

**One file, complete rules.** Type declarations live on the field line. The type is part of the field's complete definition — not a separate invariant that might be omitted or duplicated. A `field DocumentType as choice("ID","ProofOfAddress","PoliceReport")` is the complete rule. The current equivalent requires a field declaration plus a separate invariant expression. The type system expansion reduces the number of required declarations for the same correctness guarantee.

**Compile-time structural checking.** `choice` membership checking, `date` constructor validation, `decimal + number` as a type error, and `integer` division semantics are all compile-time enforcements. They belong in the type checker, not the runtime evaluator. This is consistent with Precept's existing compile-time model.

**Determinism and inspectability.** Types make behavior deterministic by being explicit. `decimal` arithmetic is reproducible across machines and locales; IEEE 754 arithmetic is not, when precision matters. `date` without timezone is deterministic across locations. `integer` division truncates toward zero per platform-defined semantics. `choice` membership is static. All four types make the value's behavior inspectable without running the code.

**Flat, keyword-anchored syntax.** The proposed syntaxes — `choice(...)`, `date`, `decimal`, `integer` — are all inline type annotations on the field line. They do not introduce new block structures, hierarchical declarations, or control flow. The constraint zone suffixes (`ordered`, `maxplaces`) remain keyword-anchored and flat. This is consistent with Precept's authoring model.

**AI-first legibility.** `field ClaimAmount as decimal maxplaces 2` is unambiguous for an AI agent reading the precept. The type and constraint are on one line, explicit, and vocabulary-grounded. An AI agent can reason about the allowed value set from `choice(...)` without needing to parse and interpret a separate invariant expression. Structured type metadata in `precept_compile` output — value sets for `choice`, `maxplaces` for `decimal`, accessor list for `date` — makes the type system machine-readable, not just human-readable.

---

## Semantic Contracts to Make Explicit

### 1. Coercion Hierarchy

The four types form a partial numeric hierarchy: `integer` widens to `decimal` or to `number`; `decimal` and `number` do not mix. The contract must be explicit:

- `integer + integer` produces `integer`
- `integer + decimal` produces `decimal` (exact widening)
- `integer + number` produces `number` (approximate widening)
- `decimal + number` is a type error — mixing exact and approximate arithmetic is never implicit
- `decimal` field cannot receive a `number` expression (no narrowing)
- `number` field cannot receive a `decimal` expression (no narrowing)

This hierarchy is the most important semantic contract in the entire expansion. Getting it wrong silently breaks the precision guarantee that `decimal` provides.

### 2. Choice Cross-Field Incompatibility

Two `choice(...)` declarations with identical value sets are distinct types. `field A as choice("X","Y")` and `field B as choice("X","Y")` are incompatible for assignment. This prevents coincidental overlap from enabling silent cross-field assignment and keeps the type system structural rather than structural-and-nominal-mixed.

### 3. `ordered` Semantics

`ordered` is an opt-in constraint on `choice` fields. It enables `<`, `>`, `<=`, `>=` with declaration-order semantics (first declared value is the lowest). Without `ordered`, those operators are compile-time errors. The choice of declaration order (not lexicographic order) is explicit: reordering values in the field declaration changes comparison behavior. This is a contract between the author and the type system that must be documented prominently.

### 4. `date` Constructor and Validation Timing

`date("YYYY-MM-DD")` validates format at compile time (parser) and validates value feasibility at the compile step (Feb 30 is rejected). The runtime does not need to re-validate date strings — they arrive as `System.DateOnly` values after parsing. This contract ensures that `date` comparisons at runtime operate on already-validated values.

### 5. `maxplaces` Fires at Assignment, Not During Intermediate Computation

`decimal` fields with `maxplaces N` reject values at the assignment boundary. Intermediate arithmetic within a `set` expression can produce more decimal places than the target allows — the `round()` function is the author's explicit tool to reduce places before assignment. The constraint does not fire silently; it fires at the `set` statement with a clear diagnostic.

### 6. `integer` Division Truncates Toward Zero

`5 / 2 == 2`, `(-5) / 2 == -2`. This follows C# platform semantics. Division by zero is a runtime error, not a compile-time error — authors guard explicitly using `when Divisor != 0` in the transition guard.

---

## Dead Ends and Rejected Directions

### `money` Type

Every rule engine and database system surveyed has either tried a `money` type and regretted it or avoided it entirely. PostgreSQL `MONEY` is locale-dependent and the community says do not use it. SQL Server `MONEY` has rounding issues under division. Salesforce has `Currency` but warns about complex cross-currency scenarios. The universal lesson is: track currency code as a separate field; use exact-numeric arithmetic for the amount. Precept's answer is `decimal` for the amount and `choice("USD","EUR","GBP")` for the currency code. `money` as a type is not on the roadmap.

### Parameterized `decimal(p,s)`

SQL's `DECIMAL(p,s)` requires the author to specify both total precision and scale at declaration time. This is appropriate for schema-level database columns where storage efficiency matters. For an in-process entity model, `System.Decimal`'s 28-29 significant digits covers all practical business values. The `maxplaces N` constraint provides scale enforcement without parameterizing the type. A `precision N` constraint (total digits) was considered and rejected: no domain evidence found for it, and `System.Decimal` makes it unnecessary.

### `datetime` and `timezone` in v1

Time-of-day requires a timezone decision to be deterministic. `time("17:00")` in New York and `time("17:00")` in Sydney are not the same moment. Precept's inspectability promise breaks under timezone-aware time unless the definition includes a timezone reference. That reference couples the definition to deployment context, which violates one-file completeness. `date` without time avoids this entirely. `duration` (months/years arithmetic) is deferred for the same reason: month arithmetic requires a calendar-aware duration type. Both are Batch 3 research territory.

### Integer Subtypes (`int32`, `int16`, `uint`)

SQL Server has `TINYINT`, `SMALLINT`, `INT`, `BIGINT`. C# has `byte`, `short`, `int`, `long`, `uint`, `ulong`. None of this matters for business entity modeling: no domain field requires 16-bit storage, no business domain has unsigned-only semantics that would benefit from `uint`. `System.Int64` covers all practical business integers without overflow risk. Parameterized width is infrastructure complexity with zero user benefit.

### Bit Operations on Integer

`&`, `|`, `^`, `<<`, `>>` on `integer` were considered as a set. No domain evidence found for bit manipulation in business entity models. These are programming-level operations. Precept is a domain integrity language; bit masking is outside its scope.

### Structural / Record Types

`field Address as { Street as string, City as string }` — a structured field type with named sub-fields. Multiple systems support this (Pydantic, TypeScript, Dataverse complex types). The research found no compelling evidence that inline record types serve the one-file model better than flat top-level fields with naming conventions. More importantly, structured types create object-graph concerns: nullability, partial update semantics, accessor syntax, and serialization format. These are all design complications with no precedent in Precept's current surface. Rejected for now; the flat field model is the right default.

### Dynamic / Open-Ended Value Sets

`choice(...)` is a closed universe — the value set is declared in the source file. An open-ended or dynamically extended value set would require a separate data source, defeating one-file completeness. This is explicitly rejected. No runtime-extended membership; no external lookup tables; no dynamic choice sets.

---

## Proposal Implications

### #25 — `choice` type

The precedent survey validates both the `choice` keyword (over `enum`) and the inline declaration form. The cross-field incompatibility contract is the most important semantic boundary to hold: two identical-looking `choice(...)` declarations are distinct types. The `ordered` opt-in is correct — MySQL's implicit declaration-order ordering has surprised users; opt-in is safer. The proposal's v1 scope (no named shared `choiceset`) is appropriate: inline first, reuse later.

### #26 — `date` type

Day-only granularity in v1 is confirmed by every system surveyed: PostgreSQL `DATE`, SQL Server `DATE`, MySQL `DATE`, `datetime.date`, `System.DateOnly`, Salesforce `Date`, Dataverse `Date Only`, ServiceNow `glide_date`. The deferral of `time` and `duration` is confirmed by the timezone determinism argument. The ISO 8601 constructor form `date("YYYY-MM-DD")` is the right design — consistent with `choice(...)`, no bare string ambiguity.

### #27 — `decimal` type

System.Decimal is the right backing type. `maxplaces N` as constraint (not type parameter) is validated by Dataverse Decimal Number and ServiceNow decimal, both of which use a precision property on the field, not a parameterized type. The `round()` function is essential — without it, division and mixed-precision multiplication produce values that cannot be assigned to `maxplaces`-constrained fields. The `decimal + number` type error is the single most important rule in the coercion hierarchy.

### #29 — `integer` type

`System.Int64` universally. Widening to `decimal` and `number` is implicit. Narrowing is never implicit. The coercion rules follow C# semantics exactly, which is correct for a .NET runtime: no original language design, no implementation surprise. The `truncate`, `floor`, and `ceil` conversion functions (from #16) are the narrowing path.

### Sequencing note

`decimal` depends on `integer` being present (mixed arithmetic `integer + decimal`). `choice` and `date` are independent of each other and of the numeric types. The natural implementation order is: `integer` first, then `decimal`, then `choice` and `date` in either order.
