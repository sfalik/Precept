# Type System Survey — Business Rule Engines and Expression Languages

**Research date:** 2026-04-05  
**Author:** Frank (Lead/Architect & Language Designer)  
**Source:** DMN/FEEL spec (Camunda docs), Cedar policy language docs, Drools DRL reference, NRules architecture, SQL standard, BPMN spec  
**Relevance:** Evidence base for Proposal #25 (type system expansion — choice and date types). Answers: what types do real-world business rule systems need?

---

## 1. DMN / FEEL (Decision Model and Notation)

FEEL is the expression language defined by the OMG DMN standard — the closest industry comparator to Precept's domain.

### Type catalog

| Type | Description |
|------|-------------|
| `null` | Absence of value |
| `number` | Arbitrary-precision decimal (Java `BigDecimal`) |
| `string` | Character sequence |
| `boolean` | `true` / `false` |
| `date` | Calendar date without time component (`yyyy-MM-dd`) |
| `time` | Local or offset time (`HH:mm:ss+/-HH:mm`) |
| `date and time` | Full timestamp, local or zoned |
| `days and time duration` | Duration in days/hours/minutes/seconds (ISO 8601 `PxDTxHxMxS`) |
| `years and months duration` | Duration in years/months (ISO 8601 `PxYxM`) |
| `list` | Ordered collection of any type |
| `context` | Key-value map (like a record/struct) |
| `range` | Interval with inclusive/exclusive endpoints, used in unary tests |
| `function` | First-class function values |

### Date/time handling

FEEL provides the richest temporal model of any business rule language:

- **Two duration types** — "days-time" for sub-month precision, "years-months" for calendar arithmetic. This split avoids the ambiguity of "what is 1 month in days?"
- **Constructor functions** — `date("2024-03-15")`, `time("11:30:00")`, `date and time("2024-03-15T11:30:00+01:00")`, `duration("P4DT2H")`
- **Arithmetic** — date ± duration → date; date − date → duration; duration ± duration → duration
- **Accessors** — `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`, `.time offset`
- **Comparison** — full `<`, `<=`, `>`, `>=`, `==` on all temporal types

### Enum/choice handling

FEEL has **no dedicated enum type**. Constrained value sets are handled through:
- Decision table input columns with explicit allowed-values lists
- String comparison in expressions (`Status = "Active"`)
- The DMN `ItemDefinition` layer can declare `allowedValues` as a constraint facet

This is a notable gap — FEEL relies on the surrounding DMN model to constrain values, not the expression language itself.

### Duration/interval handling

- Two distinct duration types prevent calendar/clock confusion
- Range type (`[1..10]`, `(0..100]`) provides interval membership testing in unary tests
- Ranges work on any comparable type, including dates and numbers

### Currency/money

No dedicated money type. Financial calculations use `number` (BigDecimal semantics give precision).

### What Precept should learn

1. **Date at day granularity is the floor** — FEEL provides full datetime, but date-only is the minimum any business rule language needs.
2. **Two duration types is smart** — but may be overengineered for Precept's domain. Day-level duration (number of days) may suffice as v1.
3. **Constructor functions are the standard pattern** — `date("2024-03-15")`, not a date literal syntax.
4. **No enum is a gap that DMN acknowledges** — they push it to the modeling layer. Precept should not repeat this: having `choice` in the expression language is strictly better.
5. **Range/interval type is powerful** but requires significant operator design. Not a v1 candidate.

---

## 2. Cedar (AWS Authorization Policy Language)

Cedar is the strongest "minimal by design" comparator — it deliberately constrains its type system for formal analyzability.

### Type catalog

| Layer | Types |
|-------|-------|
| **Core** | `Bool`, `String`, `Long` (64-bit signed integer), `Set`, `Record`, `Entity` |
| **Extension** | `datetime`, `decimal`, `duration`, `ipaddr` |

### Design philosophy

Cedar's core has **only 3 value types** (Bool, String, Long) plus structural types (Set, Record, Entity). Every extension type was added because:
- Real-world policies required it (datetime for temporal access rules, decimal for financial thresholds)
- The type could be added without breaking formal verification properties
- A constructor function pattern (`datetime("...")`) kept the parser unchanged

### Date/time handling

- `datetime` stores milliseconds since Unix epoch internally
- Constructor: `datetime("2024-10-15")` (date-only) or `datetime("2024-10-15T11:35:00Z")` (full timestamp)
- `duration` stores milliseconds, constructed as `duration("2h30m")`
- Rich datetime API: `.offset(duration)`, `.durationSince(datetime)`, `.toDate()`, `.toTime()`, `.toDays()`, `.toHours()`, `.toMinutes()`, `.toSeconds()`, `.toMilliseconds()`
- Full comparison operators (`<`, `<=`, `>`, `>=`) on datetime and duration
- **Timezone handling**: offset-based only (no timezone ID) — deliberate simplification

### Enum/choice handling

Cedar has **no enum type**. Entity types serve a similar role — `Action::"ReadFile"` is effectively an enum member. For non-entity constrained values, you use string comparison or set membership (`context.role.contains("admin")`).

### Duration/interval handling

- Single duration type (millisecond-based), not two like FEEL
- No range/interval type
- Duration arithmetic via `.offset()` on datetime

### Currency/money

`decimal` extension type with 4 decimal places (range: -922337203685477.5808 to 922337203685477.5807). Uses method-style comparison: `decimal("1.23").lessThan(decimal("1.24"))`. This is effectively a money-capable type with fixed precision.

### What Precept should learn

1. **The constructor function pattern works** — Cedar proves you can add temporal types without changing the parser. `datetime("...")` as a function-like constructor is the established pattern.
2. **Start minimal, extend carefully** — Cedar launched with only Bool/String/Long. Extensions came later when real use cases demanded them. Precept should do the same.
3. **No division operator** — Cedar deliberately omits `/` to maintain formal properties. Precept should note: not every arithmetic operation is safe to add.
4. **Decimal is the money type** — Fixed-precision decimal is how Cedar handles financial comparisons. This is worth watching but not a Precept v1 need.
5. **Single duration type is sufficient** — Cedar chose one duration (millisecond-based) vs FEEL's two. For a DSL that operates at day granularity, number-of-days is even simpler.

---

## 3. Drools DRL (Java Business Rule Engine)

Drools is the dominant open-source business rule engine, used heavily in enterprise workflows.

### Type catalog

Drools uses the **full Java type system**. Any Java class can be a fact type. Common types in rule conditions:

| Category | Types |
|----------|-------|
| **Primitives** | `int`, `long`, `double`, `boolean`, `String` |
| **Temporal** | `java.util.Date`, `java.time.LocalDate`, `java.time.LocalDateTime`, `java.time.Duration`, `java.time.Period` |
| **Numeric** | `java.math.BigDecimal`, `java.math.BigInteger` |
| **Collections** | `java.util.List`, `java.util.Set`, `java.util.Map` |
| **Custom** | Any POJO declared as a fact type |

### Date/time handling

- Drools inherits Java's temporal types (java.time.*)
- Date literals in constraints: `bornBefore < "27-Oct-2009"` (configurable format via `drools.dateformat`)
- Complex Event Processing (CEP) adds temporal reasoning: `@role(event)`, `@timestamp`, `@duration`, `@expires` metadata
- Temporal operators: `before`, `after`, `during`, `meets`, `overlaps`, `coincides` (Allen's interval algebra)

### Enum/choice handling

- `declare enum` supports full enumerative type declarations:
  ```
  declare enum DaysOfWeek
    SUN("Sunday"), MON("Monday"), ...;
    fullName : String
  end
  ```
- Enum values used in constraints: `dayOff == DaysOfWeek.MONDAY`
- Can also use Java enums directly

### Duration/interval handling

- Uses Java's `Duration` and `Period` types
- Timer expressions with duration syntax: `timer(int: 30s 5m)`, `@expires(1h35m)`
- CEP sliding windows for temporal reasoning over event streams

### Currency/money

No dedicated money type — uses `BigDecimal` from Java.

### What Precept should learn

1. **Enum support is table stakes for enterprise rules** — Drools provides full `declare enum`. Business domains need constrained value sets.
2. **Date literals are convenient** — `bornBefore < "27-Oct-2009"` is readable. But configurable date formats create ambiguity. Precept's constructor approach is safer.
3. **Full Java type system is the opposite of Precept's design** — Drools gains flexibility but loses the self-contained, flat, inspectable properties that define Precept. This validates Precept's deliberate type restriction.
4. **Temporal operators (Allen's algebra) are powerful but complex** — `before`, `after`, `meets`, `overlaps` etc. are overkill for Precept's domain. Simple comparison operators (`<`, `>`) on dates are sufficient.

---

## 4. NRules (.NET Business Rule Engine)

NRules is the primary .NET rule engine, using C# as its rule authoring language.

### Type catalog

NRules inherits the **full .NET CLR type system**. It has no independent type system — rules are written in C# fluent API and operate on CLR objects.

| Category | Common Types |
|----------|--------------|
| **Value types** | `int`, `long`, `double`, `decimal`, `bool` |
| **Temporal** | `DateTime`, `DateTimeOffset`, `TimeSpan` |
| **Strings** | `string` |
| **Enums** | C# `enum` types |
| **Collections** | `List<T>`, `HashSet<T>`, `Dictionary<K,V>` |

### Date/time handling

Full .NET DateTime/DateTimeOffset/TimeSpan — no restrictions.

### Enum/choice handling

C# `enum` types are first-class citizens. Enum members used directly in rule conditions.

### Duration/interval handling

`TimeSpan` provides full duration semantics.

### Currency/money

`decimal` type (128-bit, 28-29 significant digits) — the standard .NET money type.

### What Precept should learn

1. **NRules confirms that business rule engines need datetime and enum** — even when inheriting from a rich host language, these are the types that appear in virtually every rule condition.
2. **Host-language inheritance is the antipode of Precept's approach** — NRules proves the model works but at the cost of self-containment. Precept's value proposition is that the `.precept` file *is* the entire contract.

---

## 5. BPMN (Business Process Model and Notation)

BPMN defines process variable types via `ItemDefinition` elements that reference structural types.

### Type catalog

BPMN delegates to XML Schema (XSD) or implementation-specific type systems. The common types used in BPMN process variables:

| Type | XSD Origin | Usage |
|------|-----------|-------|
| `string` | `xsd:string` | Names, labels, identifiers |
| `boolean` | `xsd:boolean` | Flags, conditions |
| `integer` | `xsd:integer` | Counts, quantities |
| `float`/`double` | `xsd:float`/`xsd:double` | Measurements |
| `date` | `xsd:date` | Calendar dates |
| `time` | `xsd:time` | Clock times |
| `dateTime` | `xsd:dateTime` | Timestamps |
| `duration` | `xsd:duration` | ISO 8601 durations |

### Enum/choice handling

BPMN uses XSD `enumeration` constraintFacets to define allowed value sets. This is a schema-level constraint, not an expression-level type — similar to DMN's approach.

### What Precept should learn

1. **date, dateTime, and duration all appear in the BPMN standard** — these are considered essential for business process modeling.
2. **BPMN delegates type definition** — it doesn't define its own type system. Precept's self-contained approach is architecturally cleaner.

---

## 6. SQL DDL (The Baseline)

SQL is the universal baseline for data type design in business systems.

### Type catalog

| Category | Types |
|----------|-------|
| **Numeric** | `INTEGER`, `BIGINT`, `DECIMAL(p,s)`, `NUMERIC(p,s)`, `FLOAT`, `DOUBLE` |
| **String** | `VARCHAR(n)`, `TEXT`, `CHAR(n)` |
| **Boolean** | `BOOLEAN` |
| **Temporal** | `DATE`, `TIME`, `TIMESTAMP`, `TIMESTAMP WITH TIME ZONE` |
| **Interval** | `INTERVAL YEAR TO MONTH`, `INTERVAL DAY TO SECOND` |
| **Enum** | PostgreSQL `CREATE TYPE ... AS ENUM (...)`, MySQL `ENUM(...)` |

### Date/time handling

- `DATE` — calendar date (year, month, day). The most universally needed temporal type.
- `TIME` — wall-clock time. Rarely used standalone.
- `TIMESTAMP` — date + time. Essential for audit trails.
- `TIMESTAMP WITH TIME ZONE` — widely considered a design mistake in practice. Timezone semantics create non-determinism and confusion. Most applications store UTC and convert at display.

### Enum/choice handling

- PostgreSQL: `CREATE TYPE mood AS ENUM ('happy', 'sad', 'neutral')` — value set is the type definition.
- MySQL: `ENUM('Low', 'Medium', 'High')` — inline enum at column level.
- Standard SQL has no `ENUM` — relies on `CHECK` constraints for value restriction.
- In practice, almost every database application uses enums or check constraints for constrained value sets.

### Duration/interval handling

SQL defines two interval types (matching FEEL's two-duration design):
- `INTERVAL YEAR TO MONTH` — calendar intervals
- `INTERVAL DAY TO SECOND` — clock intervals

This split exists for the same reason as FEEL's: "1 month" is ambiguous in days.

### Currency/money

`DECIMAL(p,s)` is the standard SQL money type. PostgreSQL additionally offers a `MONEY` type, but it's generally considered a bad idea (locale-dependent formatting). The industry consensus is: use `DECIMAL` for money.

### What Precept should learn

1. **DATE is essential** — SQL, FEEL, Cedar, Drools, BPMN all have it. A business rule language without date is incomplete.
2. **TIMESTAMP WITH TIME ZONE is a trap** — SQL's experience validates Precept's proposal to avoid time-of-day and timezone. Date-only at day granularity is the safe choice.
3. **ENUM exists in practice even when not in the standard** — PostgreSQL and MySQL both added it because the need was overwhelming. Precept should not wait.
4. **Two interval types** — SQL and FEEL agree on splitting duration into calendar vs clock. But for Precept's day-granularity model, a single "number of days" duration is simpler and sufficient.

---

## Synthesis

### Types appearing in 3+ systems (universal consensus)

| Type | FEEL | Cedar | Drools | NRules | BPMN | SQL |
|------|------|-------|--------|--------|------|-----|
| **Number/Integer** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **String** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Boolean** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Date** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Duration** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Decimal/Money** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Set/List/Collection** | ✓ | ✓ | ✓ | ✓ | — | — |
| **Enum/Constrained values** | — | — | ✓ | ✓ | (schema) | ✓ |

### Types appearing in 1–2 systems (niche or domain-specific)

| Type | Systems | Notes |
|------|---------|-------|
| Date-time (full timestamp) | FEEL, Cedar, Drools, NRules, BPMN, SQL | Universal in general computing but **dangerous in rule DSLs** due to timezone non-determinism |
| Time (standalone) | FEEL, BPMN, SQL | Rarely needed without a date component |
| Range/interval type | FEEL only | Powerful but complex; not widely adopted |
| IP address | Cedar only | Domain-specific to authorization |
| Context/Record/Map | FEEL, Cedar, Drools, NRules | Requires nested structure — conflicts with Precept's flat model |
| Entity type | Cedar only | Authorization-domain hierarchical identity |
| Function type | FEEL only | First-class functions in a business rule language |

### Minimum viable type set for a business rule DSL

Based on this survey, the minimum set that avoids forcing workarounds in real business domains:

1. **number** — arithmetic, comparisons, counts, amounts
2. **string** — labels, identifiers, text
3. **boolean** — flags, conditions
4. **date** — calendar date at day granularity (consensus across all 6 systems)
5. **choice/enum** — constrained value sets (Drools, NRules, SQL all have it; FEEL and Cedar lack it and their users work around the absence)
6. **collection** — sets or lists of the above (Precept already has this)

**Duration** appears in all systems but its form varies. For a day-granularity DSL, number-of-days arithmetic (`date + number → date`, `date - date → number`) may be sufficient without a dedicated duration type.

**Decimal/money** appears universally but is a growth-phase addition, not a v1 essential. Precept's `number` already handles decimal values.

### How this maps to Precept's Proposal #25

| Proposal #25 element | Survey evidence | Confidence |
|----------------------|-----------------|------------|
| `choice("Low", "Medium", "High")` type | Drools has `declare enum`, SQL has `ENUM`, NRules uses C# `enum`. FEEL and Cedar lack it and rely on workarounds. **Strongest consensus for addition.** | **Very High** |
| `date` type at day granularity | All 6 systems have date. Cedar proves day-only is viable (`datetime("2024-10-15")` without time). SQL's `DATE` is the most used temporal type. | **Very High** |
| Day arithmetic (`date + number → date`) | FEEL: `date + duration → date`. Cedar: `datetime.offset(duration)`. SQL: `DATE + INTERVAL`. All support date arithmetic. Day-level is the simplest form. | **High** |
| `.day`, `.month`, `.year` accessors | FEEL: `.day`, `.month`, `.year`. Not in Cedar (extract via operations). Common in SQL (`EXTRACT`). | **Medium-High** |
| Reject `datetime`/`timestamp` | Cedar stores millis-since-epoch but **also** supports date-only form. SQL's timezone mess validates the concern. FEEL supports full datetime but business rules often need only date. | **High** — day-only is defensible |
| Reject structured/record types | FEEL has `context`, Cedar has `Record`. Both serve different architectural goals than Precept. Precept's flat model is a feature. | **High** — validated by design, not by absence |
| `choice` as inner type for collections | `set of choice(...)` replaces `set of string` for known domains. No direct precedent but natural composition. | **Medium** — novel but sound |

### Key finding: the proposal is well-calibrated

The survey confirms that Proposal #25 targets the two highest-value type additions:
- **`choice`** fills a gap that even the DMN standard acknowledges (it pushes enum to the modeling layer)
- **`date`** is universally present — a business rule language without it is incomplete

The proposal correctly rejects `datetime` (timezone non-determinism), structured types (breaks flat model), and overengineered duration (day-count arithmetic suffices). These rejections are validated by Cedar's minimalism and SQL's timezone cautionary tale.

---

## Beyond v1 — Type System Growth Path

**Context:** With `choice` and `date` confirmed for v1, Shane asked the question: "What types will Precept eventually need as it grows into more complex business domains?" This section maps the honest answer — candidates, evidence, timing, and the things we should never build.

The governing principle remains: **Precept is a domain integrity engine, not a general-purpose programming language.** Every type addition must pass the philosophy filter — does it serve the business-domain author who writes configuration-like definitions, or does it serve the programmer who would be better off in C#?

---

### Candidate 1: `integer` — Distinct Whole-Number Type

**What it is.** A numeric type that rejects fractional values. `field RetryCount as integer default 0` — the type system, not a runtime constraint, guarantees no decimals.

**Survey evidence.** Cedar's *only* numeric type is `Long` (64-bit signed integer) — it has no floating-point type at all. SQL distinguishes `INTEGER` from `DECIMAL`/`FLOAT`. BPMN inherits XSD's `xsd:integer`. Drools and NRules inherit their host language's `int`/`long`/`double` distinction. FEEL is the outlier — it uses a single `number` type (BigDecimal) and relies on context to determine integer-ness.

**Precept domains.** RetryCount, NumberOfApplicants, FloorNumber, QueuePosition, NumberOfDependents, SeatCount, HeadCount. Any field that represents a discrete countable quantity. Today these use `number`, which technically permits `RetryCount = 2.7` — caught only if the author remembers to write an invariant.

**Why not v1.** Precept's `number` already works for all existing samples. The gap between "technically permits 2.7" and "causes real authoring errors" hasn't been demonstrated. Adding `integer` doubles the numeric type surface — every operator, every collection inner type, every expression rule must handle both. The type checker's interval analysis would need separate integer and continuous interval domains.

**What would trigger adding it.** (a) Corpus evidence of authors writing fractional-rejection invariants as a workaround pattern; (b) a domain where integer vs decimal semantics are load-bearing for correctness (e.g., inventory management where 0.5 units is nonsensical and dangerous); (c) financial domains where `integer` cents coexist with `decimal` dollars and the distinction matters for rounding rules.

**Parser/architecture implications.** Low parser cost — `integer` is just a new type keyword. Moderate type-checker cost — numeric binary operators must resolve `integer op integer → integer`, `integer op number → number`, etc. Collection inner types widen from `{string, number, boolean, choice, date}` to include `integer`. The real cost is in the expression evaluator: does `5 / 2` yield `2` (integer division) or `2.5` (promote to number)? This single question has derailed larger languages. Recommendation: if we add `integer`, integer division truncates and we provide no implicit promotion — the author must be explicit.

---

### Candidate 2: `duration` — First-Class Time Span

**What it is.** A type representing a span of time, enabling `date + duration → date` and `date - date → duration` with richer semantics than raw day-counting.

**Survey evidence.** FEEL splits duration into two types: `days and time duration` (sub-month) and `years and months duration` (calendar). Cedar uses a single `duration` type (millisecond-based) with constructor `duration("2h30m")`. SQL defines `INTERVAL YEAR TO MONTH` and `INTERVAL DAY TO SECOND`. BPMN inherits XSD `duration` (ISO 8601). All 6 surveyed systems have duration in some form.

**Precept domains.** SLA deadlines ("resolve within 3 business days"), warranty periods ("coverage lasts 365 days"), cooling-off periods ("cancel within 14 days"), payment terms ("net 30"), lease durations, probation periods, trial subscriptions. Today all of these encode as `number` representing day-count, which works but loses semantic intent — `field WarrantyDays as number` tells the reader nothing that `field WarrantyPeriod as duration` would.

**Why not v1.** v1's `date` type already supports `date ± number → date` and `date - date → number`, which covers the day-counting pattern cleanly. Duration becomes necessary only when: (a) the day-count representation is ambiguous or error-prone (e.g., "add 2 months" where month length varies); (b) sub-day granularity matters (hours, minutes); or (c) the semantic intent gap causes real authoring confusion. None of these pressures exist in the current corpus.

**What would trigger adding it.** (a) Precept enters scheduling/appointment domains where "2 hours 30 minutes" is a natural expression; (b) calendar-month arithmetic becomes necessary ("add 1 month" that correctly handles Feb → Mar); (c) ISO 8601 duration interchange with external systems becomes a requirement.

**Parser/architecture implications.** Moderate parser cost — constructor function `duration("P3D")` or `duration("2h30m")` follows Cedar's pattern and requires no new literal syntax, just a recognized function-call form. But: do we adopt FEEL's two-duration split or Cedar's single type? If single type, arithmetic is straightforward. If split, the type checker must prevent `calendar_duration + clock_duration`. The bigger question is granularity — if Precept stays day-level, `duration` is just a wrapped integer and barely justifies its own type. It only earns its keep when sub-day or calendar-month semantics enter the picture.

---

### Candidate 3: `decimal(p,s)` / `money` — Precision-Controlled Numeric

**What it is.** A numeric type with explicit precision and scale, ensuring that `$100.10 + $99.90 == $200.00` exactly, not `$199.99999999999997`. Alternatively, a dedicated `money` type with fixed precision rules.

**Survey evidence.** FEEL uses `BigDecimal` under the hood — precision is implicit and unlimited. Cedar has a `decimal` extension type with fixed 4-decimal-place precision. SQL defines `DECIMAL(p,s)` / `NUMERIC(p,s)` with explicit precision and scale. Drools and NRules inherit `BigDecimal` and `decimal` from their host languages. PostgreSQL has a `MONEY` type but the community discourages it (locale-dependent). The universal consensus is: precise decimal arithmetic is required for financial domains, and `DECIMAL(p,s)` is the standard form.

**Precept domains.** Insurance claim amounts, loan principals, reimbursement totals, refund amounts, subscription pricing, deductibles, copays, billing line items. Any domain where monetary values are compared, summed, or constrained and rounding errors would be a business-logic bug. Today Precept uses `number` (IEEE 754 double), which is sufficient for small amounts and simple comparisons but technically introduces floating-point imprecision on arithmetic.

**Why not v1.** Current sample files don't perform arithmetic on monetary values — they store amounts and compare them (`Amount > 500`), which IEEE 754 handles correctly for typical business ranges. The imprecision problem manifests on cumulative arithmetic (running totals, multi-line invoicing), which is beyond Precept's current single-entity model. Adding `decimal(p,s)` means Precept must decide: (a) what happens when two decimal types with different precision interact; (b) whether to support parameterized types (a first for the DSL); (c) rounding modes. These are deep design decisions with no easy defaults.

**What would trigger adding it.** (a) Precept enters financial domains where running totals or compound calculations happen within a single entity lifecycle; (b) integration with external ledger systems requires exact decimal interchange; (c) authors report actual rounding bugs in production precepts.

**Parser/architecture implications.** High parser cost if parameterized: `decimal(10,2)` requires parsing type parameters — a new syntactic form with no precedent in the current grammar. The type checker would need parametric type comparison (is `decimal(10,2)` assignable to `decimal(12,4)`?). Alternative: follow Cedar and fix precision (e.g., always 4 decimal places) — this avoids parameterized types but limits flexibility. A simpler alternative might be `money` as a built-in alias for `decimal(19,4)` (SQL's common money precision). The Superpower parser can handle any of these, but the conceptual weight is the real cost.

---

### Candidate 4: `time` — Time-of-Day Without Date

**What it is.** A type representing wall-clock time independent of any calendar date. `field AppointmentTime as time` — stores `14:30` without associating it to a specific day.

**Survey evidence.** FEEL has `time` as a core type with constructor `time("14:30:00")`. SQL has `TIME` and `TIME WITH TIME ZONE`. BPMN inherits XSD `xsd:time`. Cedar does not have a standalone time type — its `datetime` includes both. Drools and NRules inherit `LocalTime` / `TimeOnly` from their host languages.

**Precept domains.** Appointment scheduling (clinic hours, service windows), business-hour rules ("only process between 08:00 and 17:00"), shift management, restaurant seating (waitlist open hours), parking enforcement (meter hours). The clinic-appointment sample would benefit: `field PreferredTime as time` instead of encoding minutes-since-midnight as a `number`.

**Why not v1.** Time-of-day introduces the hardest question in temporal design: **what timezone?** If `time` is "wall clock time," whose wall? A precept that says `AppointmentTime > time("17:00")` behaves differently in New York vs Los Angeles. Precept's deterministic inspectability contract requires that the same inputs produce the same outputs — timezone-dependent time comparison violates this. Date at day granularity avoids this because calendar dates are timezone-independent for all practical business purposes (a due date is the same date everywhere).

**What would trigger adding it.** (a) A clear design for timezone handling that preserves determinism — likely "time is always provided as an event argument, never computed, and the precept treats it as an opaque ordinal value"; (b) multiple real domains where the minutes-since-midnight `number` workaround causes authoring confusion or errors; (c) user research showing that `time` is expected by domain authors.

**Parser/architecture implications.** Low parser cost — constructor `time("14:30")` follows the established pattern. Moderate type-checker cost — time comparison operators, time arithmetic (`time + duration → time`? with wrapping at midnight?). The hard design work is not in the parser but in the semantics: does `time("23:00") + duration("2h")` yield `time("01:00")` (wrap) or an error? Every decision here has business implications.

---

### Candidate 5: `range` / `interval` — First-Class Bounded Intervals

**What it is.** A type representing a bounded interval with inclusive/exclusive endpoints. FEEL writes `[1..10]` for "1 to 10, inclusive" and `(0..100]` for "greater than 0, up to and including 100." Used for membership testing: `Amount in [100..500]`.

**Survey evidence.** FEEL has range as a first-class type with rich syntax: `[1..10]`, `(0..100]`, `[date("2024-01-01")..date("2024-12-31")]`. SQL has `BETWEEN ... AND ...` (operator, not a type). No other surveyed system has a first-class range type. Cedar accomplishes range testing with `context.amount > 0 && context.amount <= 100`. Drools has a `from` accumulate pattern that can test membership but no range literal.

**Precept domains.** Credit score tiers, income brackets, age brackets, temperature ranges, priority bands, pricing tiers, weight classes — any domain with contiguous bands where the current encoding requires paired comparison operators (`Score >= 300 and Score < 670`).

**Why not v1.** Range is powerful but architecturally expensive: (a) it's a **parameterized type** (`range of number`, `range of date`); (b) it introduces bracket syntax (`[`, `]`, `(`, `)`) that conflicts with Precept's no-bracket-delimiter principle; (c) the `in` keyword is already used for state context (`in <State>`); (d) the design space is large — are ranges field types, expression-only constructs, or guard-only test forms? FEEL devoted significant spec to range semantics. Precept would need a complete design document before implementation.

**What would trigger adding it.** (a) Precept is used for insurance underwriting, credit decisioning, or tax calculation where tier-based rules dominate and the paired-comparison pattern creates significant row duplication; (b) a clean syntax is found that doesn't introduce brackets or ambiguate existing keywords.

**Parser/architecture implications.** High parser cost. Bracket delimiters are currently unused in Precept — introducing `[` and `]` for ranges while avoiding confusion with potential future array syntax requires careful tokenizer design. The Superpower parser can handle it, but `[` would need to be a context-dependent token (range delimiter vs. potential future use). Type checker must handle `range of T` as a generic type and implement containment operators. This is one of the most expensive additions on the list, which is why FEEL is nearly alone in doing it.

---

### Candidate 6: Named Choice Sets (`choiceset`)

**What it is.** A reusable, named declaration for a set of allowed values, referenced by multiple fields. Instead of repeating `choice("Low", "Medium", "High")` on every field that uses the same value set, declare it once:

```
choiceset Priority "Low", "Medium", "High"
field TicketPriority as Priority
field EscalationLevel as Priority
```

**Survey evidence.** PostgreSQL's `CREATE TYPE ... AS ENUM (...)` is exactly this pattern — name a value set, reference it across columns. Drools' `declare enum` is the same. FEEL lacks this (no enum at all). Cedar lacks it (no enum). This is a database/rule-engine pattern, not an expression-language pattern.

**Precept domains.** Any domain where the same value set appears on multiple fields: Status codes shared across parent/child entities, priority levels, risk categories, approval tiers, document classification labels. The insurance-claim sample has multiple fields that could share a `ClaimStatus` value set if such entities were composed.

**Why not v1.** v1 `choice` is field-scoped — the value set is part of the field declaration. This is simpler, requires no name-resolution for choice sets, and avoids forward-reference issues. Named choice sets require: (a) a new declaration form (`choiceset`); (b) name resolution (the field's type references a named construct); (c) declaration ordering rules (can a field reference a choiceset declared below it?). The benefit only materializes when the same value set is repeated — and within a single precept file, repetition is usually minimal.

**What would trigger adding it.** (a) Precept supports multi-entity composition (cross-precept references) and shared value sets become essential for consistency; (b) single-precept files grow large enough that repeating `choice("A", "B", "C", "D", "E", "F")` on 4+ fields becomes a maintenance burden; (c) authors request it as a readability improvement.

**Parser/architecture implications.** Low parser cost — `choiceset <Name> <values>` is a straightforward declaration combinator. Moderate type-checker cost — name resolution must treat choiceset names as type-level symbols, distinct from field names and state names. The IntelliSense impact is positive (auto-suggest choiceset names in `as` position). The Superpower parser handles this trivially. The real question is whether it's worth the conceptual weight before multi-entity composition exists.

---

### Candidate 7: `map<K,V>` / Associative Data

**What it is.** A key-value collection where values are looked up by key. `field Metadata as map<string, string>` or FEEL's `context` type where `Metadata.get("key")` returns a value.

**Survey evidence.** FEEL's `context` type is a key-value map — `{ name: "John", age: 30 }`. Cedar's `Record` type is structurally typed key-value data. Drools and NRules inherit `Map<K,V>` / `Dictionary<K,V>` from their host languages. SQL has `JSON`/`JSONB` columns and `HSTORE` (PostgreSQL). 4 of 6 surveyed systems have some form of associative data.

**Precept domains.** Configuration metadata, attribute bags, audit trail key-value pairs, custom field systems, localization dictionaries. Any domain where the set of keys is not known at definition time — "the operational data has arbitrary properties."

**Why not v1 (or possibly ever).** This is the candidate most likely to hit the **"never add" line.** Maps fundamentally conflict with Precept's design:
- **Flat field model** — every field is a named, typed, statically-known symbol. Maps introduce dynamic keys that the type checker cannot validate.
- **Inspectability** — `precept_inspect` can report on every field because it knows them all at compile time. Map entries are invisible to static analysis.
- **Invariant scope** — you can't write `invariant Metadata.get("priority") != null` without dynamic field access, which breaks keyword-anchored expressions.
- **Philosophy** — a domain expert who needs a key-value bag is describing a system that hasn't been modeled yet. The right answer is "model your fields explicitly," not "throw them in a map."

**What would trigger adding it.** Honestly: almost nothing within Precept's design philosophy. If a domain truly requires dynamic key-value data, the precept should model the known keys as explicit fields and leave the dynamic portion to the hosting application. The only scenario that might justify it is **integration metadata** — a bag of pass-through values that Precept doesn't validate but carries through the lifecycle.

**Parser/architecture implications.** Very high cost. Parameterized types (`map<string, number>`), dynamic member access syntax, new collection operations (`.get()`, `.put()`, `.containsKey()`), iteration constructs for map entries. This would be the single most complex type addition possible and would pressure every layer from tokenizer through runtime.

---

### Candidate 8: Ordinal `choice` Comparison

**What it is.** The ability to compare `choice` values using `<`, `>`, `<=`, `>=` based on declaration order. If `field Severity as choice("Low", "Medium", "High")`, then `Severity > "Medium"` would mean `Severity == "High"` because "High" is declared after "Medium."

**Survey evidence.** PostgreSQL `ENUM` supports ordinal comparison by declaration order. Drools `declare enum` assigns implicit ordinal values. SQL `ENUM` in MySQL uses insertion-order comparison. FEEL has no enum, so no precedent. Cedar has no enum. The pattern is well-established in database systems.

**Precept domains.** Priority escalation ("if Priority > Medium, assign to senior"), severity-based routing, risk tiering, approval level hierarchies, subscription tier comparisons ("Gold > Silver > Bronze"). Any domain where the choice values have an inherent ordering that drives business logic.

**Why not v1.** Ordinal comparison was explicitly deferred from the v1 `choice` proposal. The reasons: (a) declaration order as implicit semantics is fragile — reordering the values in the source changes comparison behavior silently; (b) it requires the author to understand that order matters, which is a mental-model burden for non-programmers; (c) `==` and `!=` cover the majority of choice-comparison use cases; (d) authors can work around ordering needs with explicit guards: `when Severity == "High"` instead of `when Severity > "Medium"`.

**What would trigger adding it.** (a) Pattern evidence of authors writing multi-branch `==` chains that would collapse to a single `>=` comparison; (b) a syntax design that makes the ordering explicit (e.g., `choice ordered ("Low", "Medium", "High")`) so the implicit-order risk is mitigated; (c) user research showing domain authors expect ordering to work intuitively.

**Parser/architecture implications.** Low parser cost (comparison operators already exist; choice values already have declaration order). Moderate type-checker cost — the type checker must track whether a choice type is `ordered` and emit diagnostics if `<`/`>` are used on non-ordered choices. Expression evaluator must resolve comparison by ordinal index. The `ordered` keyword variant adds one keyword and one boolean flag to the choice type representation. If we add this, the `ordered` keyword should be mandatory — never allow implicit ordinal comparison.

---

### Candidate 9: Additional Candidates (Brief Assessment)

**`regex` / pattern type.** FEEL has no regex. Cedar has no regex. SQL has `LIKE`/`SIMILAR TO`. Drools inherits Java regex. Use case: input validation (`field Email as string matching "^[^@]+@[^@]+$"`). Verdict: **not a type** — this is a constraint/validation feature (`invariant Email matches "..."`) rather than a type addition. If Precept adds pattern validation, it belongs in the constraint system, not the type system.

**`url` / `email` / domain-specific string subtypes.** Cedar has `ipaddr`. SQL has domain types. Use case: structured string validation. Verdict: **never as built-in types.** These are application-level concerns. If Precept grows a constraint system with pattern matching, these become expressible as constraints on `string`. A DSL that adds `url` and `email` types will be asked for `phone`, `ssn`, `iban`, and `postal-code` next — there's no stable stopping point.

**`list<T>` (ordered collection).** Precept has `set`, `queue`, `stack` but no general ordered list with index access. FEEL has `list` with index operators. SQL has `ARRAY`. Use case: ordered preferences, ranked options. Verdict: **possible Phase 3 candidate** if authors need index-based access. Current collections cover the common business patterns (unique set, FIFO queue, LIFO stack).

**`null` as a type (union types / option types).** FEEL has first-class `null`. Cedar avoids null. Precept has `nullable` as a field modifier. Use case: richer null-handling expressions. Verdict: **Precept's nullable modifier is the right design.** Making `null` a type creates union-type pressure (`string | null`) which cascades into the type checker. The modifier approach is simpler and sufficient.

**`record` / structured types.** Covered in the main survey — FEEL has `context`, Cedar has `Record`. Verdict: **never.** This is the clearest "never add" item. Precept's flat field model is a core architectural decision, not a limitation. Structured types would break inspectability, complicate the type checker's per-field analysis, require nested path expressions, and pressure the keyword-anchored flat statement model. If a domain needs nested data, the outer system models it and passes flat fields into the precept.

---

### Growth Phases

#### Phase 1 (v1): `choice` + `date` — Confirmed

The two highest-confidence additions, validated by all 6 surveyed systems. `choice` fills the enum gap with idiomatic configuration-like syntax. `date` at day granularity fills the universal temporal gap without timezone risk. These are the "table stakes" types that every business rule DSL must have.

#### Phase 2: Post-v1 Stabilization

After v1 ships and real-world usage patterns emerge, the next candidates in priority order:

1. **Ordinal `choice` comparison** (`choice ordered`) — lowest implementation cost, highest author convenience, directly extends v1's `choice` type. Requires the `ordered` keyword to prevent implicit-order bugs. Phase 2 because it's a refinement of v1, not a new type.

2. **Named choice sets** (`choiceset`) — natural progression once `choice` proves its value. Low parser cost, modest type-checker cost. Becomes essential if precepts grow larger or if multi-entity composition is explored. Phase 2 because it reduces repetition in real definitions.

3. **`integer`** — the first genuinely new type. Justified when corpus evidence shows fractional-rejection invariants appearing as a workaround pattern. The design decision (integer division semantics) must be resolved before implementation. Phase 2 because the need is real but not yet acute.

**Phase 2 trigger:** v1 has shipped, 50+ real-world precepts exist, and pattern analysis reveals which workarounds (if any) recur.

#### Phase 3: Complex Enterprise Domains

When Precept moves beyond workflow/approval domains into regulated industries (finance, insurance, healthcare, logistics):

1. **`decimal(p,s)` or `money`** — required for financial domains where arithmetic precision is contractual. The design decision is whether to support parameterized types (`decimal(19,4)`) or a fixed-precision `money` alias. Phase 3 because it requires parameterized type support, which is a significant architectural step.

2. **`duration`** — required when Precept enters scheduling, SLA management, or calendar-aware domains. The design decision is whether to follow FEEL's two-type split or Cedar's single type. Phase 3 because day-count arithmetic suffices until sub-day or calendar-month granularity is needed.

3. **`time`** — required for scheduling and business-hour domains, but *only* after a clean timezone/determinism design is established. Phase 3 because the determinism risk is real and the design work is substantial.

**Phase 3 trigger:** Precept is adopted in a regulated or financial vertical where precision, scheduling, or temporal constraints are contractual requirements.

#### Long-Term: Standard Business Rule DSL

If Precept becomes the standard DSL for business rule authoring across industries:

1. **`range` / interval type** — when tier-based decisioning (insurance underwriting, credit scoring, tax brackets) is a primary use case and paired comparisons create unsustainable row duplication. Requires a bracket syntax design that doesn't conflict with Precept's principles.

2. **Pattern constraints on `string`** — `invariant Email matches "..."` for input validation. Not a type, but a constraint-system extension that serves the same need as domain-specific string subtypes.

3. **`list<T>` with index access** — if ordered, indexed collections prove necessary beyond what `queue` and `stack` provide.

**Long-term trigger:** Precept has hundreds of production deployments, a community requesting features, and a clear pattern catalog showing which workarounds dominate.

---

### What Precept Should Never Add

Even at the longest horizon, some types and features should remain permanently excluded because they violate core architectural principles:

| Candidate | Why Never |
|-----------|-----------|
| **`record` / structured types** | Breaks the flat field model. Introduces nested paths, complicates per-field static analysis, pressures keyword-anchored statements. If you need nested data, model it outside the precept. |
| **`map<K,V>` / dynamic keys** | Breaks static inspectability. The type checker and preview tools cannot reason about keys that don't exist at compile time. A precept with a map is a precept that hasn't been modeled. |
| **`datetime` / `timestamp`** | Timezone semantics violate deterministic inspectability. Time-of-day (if ever added) must be provided as an input, never computed. Full timestamps belong in the hosting application's audit layer. |
| **`function` types** | Precept is declarative. Function values create computational opacity — you can't inspect what a function *will do* without executing it. This directly contradicts "deterministic, inspectable model." |
| **Domain-specific string types** (`url`, `email`, `phone`) | No stable stopping point. These are validation concerns, not type-system concerns. A pattern constraint on `string` is the right abstraction. |
| **`any` / dynamic typing** | Destroys the type checker's ability to prove anything. Precept's value comes from static guarantees — an `any` type opts out of all of them. |
| **Inheritance / subtyping** | Creates type hierarchies that require nominal or structural subtyping. Precept's type system is flat by design. Choice types with `ordered` provide the only ordering that makes sense in this domain. |

The "never add" list is not about technical impossibility — it's about architectural identity. Precept's competitive advantage is that a `.precept` file is the *entire* contract, statically analyzed, deterministically executable. Every item on the "never add" list would erode that contract.

---

### Summary: Growth Trajectory

```
v1          choice, date
            │
Phase 2     choice ordered, choiceset, integer
            │
Phase 3     decimal/money, duration, time
            │
Long-term   range, string patterns, list<T>
            │
Never       record, map, datetime, function, any, domain strings, inheritance
```

Each phase transition is gated by **evidence from real usage**, not speculation. Precept adds types when the workaround cost exceeds the complexity cost — and not before.
