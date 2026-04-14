# Proposal: Unified Temporal Type System — NodaTime-Aligned

**Author:** Frank (Lead/Architect, Language Designer)
**Date:** 2026-04-13
**Status:** Proposal — ready for owner review
**Supersedes:** Issue #26 (`localdate` type as standalone proposal)

---

## Summary

Add seven temporal types to the Precept DSL — `localdate`, `localtime`, `instant`, `duration`, `timezone`, `zoneddatetime`, and `localdatetime` — backed by NodaTime as a runtime dependency. Include three timezone conversion functions (`toLocalDate`, `toLocalTime`, `toInstant`) with provenance-safe `zoneddatetime` overloads, and duration constructor functions (`days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`). Together, these types and functions give domain authors the vocabulary to express calendar constraints, SLA enforcement, multi-timezone compliance rules, and elapsed-time tracking — all within the governing contract, with no temporal logic delegated to the hosting layer. Every type earns its place by eliminating a specific ambiguity that would otherwise force authors into lossy encodings (`string`, `number`, `integer`) where the compiler cannot enforce domain intent.

---

## Design Philosophy

NodaTime exists because `System.DateTime` lets you be implicit about what your temporal data means. Precept exists because scattered service-layer code lets you be implicit about what your business rules mean. Both libraries are responses to the same failure mode: **implicit behavior creates bugs; explicit behavior creates predictability.**

This shared philosophy is the lens through which every type decision in this proposal is made:

| Precept applies it to... | NodaTime applies it to... | The shared principle |
|---|---|---|
| `localdate` over `string` | `LocalDate` over `DateTime` | Be explicit that this is a calendar date |
| `instant` over `number` | `Instant` over `DateTime` | Be explicit that this is a point on the timeline |
| `timezone` over `string` | `DateTimeZone` over `string` | Be explicit about the allowed values |
| `duration` over `integer` | `Duration` over `TimeSpan` | Be explicit about what units mean |
| `localtime` over `integer` | `LocalTime` over `int minutesSinceMidnight` | Be explicit that this is a time of day |
| `localdatetime` over `string` | `LocalDateTime` over `DateTime` | Be explicit about combined date+time without timezone |
| `zoneddatetime` over `instant` + `timezone` | `ZonedDateTime` over separate `Instant` + `DateTimeZone` | Be explicit that this instant and timezone are semantically bound |

Precept's design principles ground this directly:

- **Principle #1 (Deterministic, inspectable model):** NodaTime's type separation makes it structurally clear which operations are deterministic and which require external context. All types proposed here are deterministic by construction — `instant` comparison is nanosecond math, `localdate` arithmetic uses the fixed ISO calendar, and timezone conversion functions make the TZ database input explicit in the expression.
- **Principle #2 (English-ish but not English):** The DSL names — `localdate`, `localtime`, `instant`, `duration`, `timezone`, `localdatetime` — are English words that communicate exactly what the data is. `field FiledAt as instant` needs no comment.
- **Principle #8 (Sound, compile-time-first static analysis):** NodaTime's type separation enables the compiler to catch temporal misuse statically. `date + instant` is a type error. `instant.year` is a compile error. The types carry enough information for the compiler to reject nonsensical expressions without runtime evaluation.
- **Principle #12 (AI is a first-class consumer):** Named temporal types with precise semantics give AI consumers a vocabulary to reason about entity data and generate correct precepts. An AI that sees `field DueDate as localdate` knows the field supports calendar arithmetic — it does not need to infer this from naming conventions on a `string` field.

The governing question for every decision: **"If a domain author has this kind of data, does giving it a named type help them be explicit about what it means?"**

---

## Motivation

### The temporal gap

Across 15 sample `.precept` files analyzed (see [Sample Temporal Pattern Catalog](sample-temporal-pattern-catalog.md)), the corpus contains:

| Category | Count | Current workaround |
|---|---|---|
| Calendar dates (filing, approval, renewal) | 56 fields | `string` or `FUTURE(date)` marker |
| Calendar deadlines (date + period) | 15 fields | Pre-computed by hosting layer |
| Calendar spans (grace periods, loan terms) | 10 integer surrogates | `integer` + manual arithmetic |
| Elapsed durations (work hours, MTBF) | 9 integer surrogates | `integer` or `decimal` |
| SLA/compliance timestamps | Present in 5+ domains | `number` (epoch seconds) |
| Time-of-day | 2 fields in 1 sample | `integer` (minutes since midnight) |
| Full timestamps (incident response) | 6 fields in 1 sample | Not expressible |

Three samples contain elaborate **day-counter simulation machinery** — events like `AdvanceLapseClock` and `AdvanceDay` that exist solely because the DSL lacks temporal arithmetic. These account for ~90 lines of boilerplate that would collapse to trivial date expressions with native types.

### Before and After

**Before** — encoding dates as numbers:

```precept
field DueDayOffset as number default 0
field CurrentDayOffset as number default 0
field GracePeriodDays as number default 30

# No type safety — DueDayOffset + MealsTotal compiles
invariant DueDayOffset + GracePeriodDays >= CurrentDayOffset because "Within grace period"
```

**After** — with temporal types:

```precept
field DueDate as localdate default localdate("2026-06-01")
field GracePeriodDays as integer default 30

# Type-safe: DueDate + MealsTotal is a compile error
invariant DueDate + days(GracePeriodDays) >= localdate("2026-01-01") because "Within grace period"
```

**Before** — encoding SLA rules with raw numbers:

```precept
field FiledAt as number default 0       # Epoch seconds — what epoch?
field IncidentAt as number default 0    # Same question
field SlaSeconds as number default 259200  # 72 hours... or is it?

invariant FiledAt - IncidentAt <= SlaSeconds because "Must file within SLA"
```

**After** — with instant and duration:

```precept
field FiledAt as instant
field IncidentAt as instant

# Self-documenting, type-safe, deterministic
invariant FiledAt - IncidentAt <= hours(72) because "HIPAA: must file within 72 hours of incident"
```

**Before** — multi-timezone compliance pushes logic to hosting layer:

```precept
field FilingDeadline as number  # Pre-computed by hosting layer
# The MEANING of this deadline (30 days, midnight, incident timezone) is NOT in this file
invariant FiledAt <= FilingDeadline because "Filing deadline has passed"
```

**After** — complete rule in the contract:

```precept
field IncidentTimestamp as instant
field FiledTimestamp as instant
field IncidentTimezone as timezone

invariant FiledTimestamp <= toInstant(
    toLocalDate(IncidentTimestamp, IncidentTimezone) + days(30),
    localtime("23:59:00"),
    IncidentTimezone
) because "Claim must be filed by 11:59 PM local time on the 30th day after the incident"
```

The second form satisfies the philosophy's "one file, complete rules" guarantee. An auditor reads the precept and sees the entire business rule — 30 days, 11:59 PM, incident timezone.

### What happens if we don't build this

- 56 calendar date fields remain encoded as strings or numbers. The compiler cannot distinguish a date from a price.
- SLA and compliance timing rules stay in the hosting layer. The contract has a visible gap in its primary target domains (insurance, healthcare, finance).
- Day-counter simulation events remain as boilerplate in 3+ samples.
- The "one file, complete rules" philosophy claim is undermined for any domain with temporal constraints.

---

## NodaTime as Backing Library

### Why NodaTime, not System.DateOnly / TimeOnly / DateTime

NodaTime is adopted as a runtime dependency for the entire temporal type system. The DSL author never sees NodaTime type names — `field DueDate as localdate` is the surface, `NodaTime.LocalDate` is the implementation, just as `field Amount as decimal` has `System.Decimal` behind it.

**Rationale:**

1. **Philosophy alignment.** NodaTime's core design philosophy is: *"Force you to think about what kind of data you really have."* Distinct types for distinct temporal concepts — `LocalDate` is not `Instant` is not `ZonedDateTime`. This is Precept's prevention guarantee applied to temporal data.

2. **Type separation enables compile-time safety.** NodaTime makes it structurally impossible to mix a calendar date with a global timestamp. Precept's type checker inherits this separation: `localdate + instant` is a type error by construction.

3. **Battle-tested arithmetic.** `LocalDate.PlusDays(int)`, `LocalDate.Plus(Period.FromMonths(n))`, month-end truncation (Jan 31 + 1 month = Feb 28), leap-year handling — all rigorously tested since 2012. Building the same guarantees on `System.DateOnly` would require Precept to own temporal arithmetic correctness, a domain Precept has no expertise in.

4. **Lower marginal cost.** NodaTime has higher up-front cost (dependency + serialization) but significantly lower marginal cost for each subsequent temporal type. `System.DateOnly` is cheaper for `localdate` alone but increasingly expensive as temporal features accumulate — `TimeOnly` lacks integration with date arithmetic, and `DateTime` reintroduces the ambiguity problems NodaTime was designed to solve.

5. **Coherent future path.** `LocalDate`, `LocalTime`, `LocalDateTime`, `Instant`, `Duration`, `DateTimeZone` — the entire temporal vocabulary maps from NodaTime types with consistent semantics. Using BCL types would require mixing `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset`, and `TimeSpan` — types with overlapping, inconsistent semantics.

**Dependencies added:**
- `NodaTime` (core library)
- `NodaTime.Serialization.SystemTextJson` (JSON serialization for MCP tools)

**Decision format:**
- **Why:** NodaTime's type model matches Precept's philosophy; battle-tested arithmetic avoids reimplementing solved problems.
- **Alternatives rejected:** `System.DateOnly`/`TimeOnly` — cheaper for v1 but creates increasing technical debt; month/year arithmetic requires `DateTime` conversion. Raw `System.DateTime` — conflates multiple temporal concepts in one type; the exact problem NodaTime was created to solve.
- **Precedent:** NRules inherits `System.Decimal` from .NET for exact arithmetic; Precept inherits NodaTime's temporal model for the same reason.
- **Tradeoff accepted:** Additional NuGet dependency (~1.1 MB). Acceptable — NodaTime is authored by Jon Skeet, stable since 2012, SemVer-compliant, used in production at Google and across the .NET ecosystem.

---

## Proposed Types

### `localdate`

**What it makes explicit:** This is a calendar date — not a timestamp, not a string that looks like a date. Day-granularity arithmetic is meaningful. "2026-03-15" means the same calendar day everywhere.

**Backing type:** `NodaTime.LocalDate`

**Declaration:**

```precept
field DueDate as localdate default localdate("2026-06-01")
field FilingDate as localdate nullable
field ContractEnd as localdate default localdate("2099-12-31")
```

**Literal / Constructor syntax:** `localdate("<YYYY-MM-DD>")` — ISO 8601, always. No custom formats. `localdate("2026-03-15")` is valid. `localdate("03/15/2026")` is a compile error with a teachable message.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `localdate + days(n)` | `localdate` | Add N calendar days. `LocalDate.PlusDays(int)`. |
| `localdate - days(n)` | `localdate` | Subtract N calendar days. |
| `localdate + months(n)` | `localdate` | Add N months. Truncates at month end (Jan 31 + 1 mo = Feb 28). |
| `localdate + years(n)` | `localdate` | Add N years. Handles leap years (Feb 29 + 1 yr = Feb 28). |
| `localdate + weeks(n)` | `localdate` | Add N weeks (= 7N days). |
| `localdate - localdate` | `duration` | Elapsed days between dates. The result is a duration carrying day-unit semantics. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering. ISO calendar only. |

| **Not supported** | **Why** |
|---|---|
| `localdate + localdate` | Adding two dates is meaningless — no temporal concept this could represent. |
| `localdate + integer` | Bare integers don't carry unit semantics — days? months? weeks? Use `localdate + days(n)`. |
| `localdate + decimal` | Fractional days are meaningless at day granularity. Type error. |
| `localdate + number` | `number` is floating-point; temporal arithmetic requires explicit duration constructors. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.year` | `integer` | Calendar year |
| `.month` | `integer` | Month (1–12) |
| `.day` | `integer` | Day of month (1–31) |
| `.dayOfWeek` | `integer` | ISO day of week (Monday=1, Sunday=7) |

**Constraints:** `nullable`, `default localdate(...)`. Constraints `nonnegative`, `positive`, `min`, `max`, `maxplaces`, `minlength`, `maxlength` are not valid on `localdate` (compile error).

**Serialization:** ISO 8601 string in MCP JSON payloads: `"2026-03-15"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `field X as localdate default "2026-01-01"` | Date defaults require the localdate constructor: `default localdate("2026-01-01")`. Bare strings are not dates. |
| `localdate("2026-02-30")` | Invalid date: February 30 does not exist. Use a valid calendar date in ISO 8601 format (YYYY-MM-DD). |
| `localdate("01/15/2026")` | Invalid date format: expected ISO 8601 (YYYY-MM-DD), got '01/15/2026'. Use `localdate("2026-01-15")`. |
| `DueDate + FilingDate` | Cannot add two dates. Did you mean `DueDate - FilingDate` (duration) or `DueDate + days(n)` (offset)? |
| `DueDate + 2` | Cannot add an integer to a date. Temporal arithmetic requires explicit duration constructors. Use `DueDate + days(2)` to add 2 calendar days. |
| `DueDate + 2.5` | Cannot add a number to a date. Temporal arithmetic requires explicit duration constructors. Use `DueDate + days(2)` or `DueDate + months(n)`. |

---

### `localtime`

**What it makes explicit:** This is a time of day — not a duration, not a timestamp, not an integer encoding minutes-since-midnight.

**Backing type:** `NodaTime.LocalTime`

**Declaration:**

```precept
field AppointmentTime as localtime default localtime("09:00:00")
field CheckInTime as localtime nullable
```

**Literal / Constructor syntax:** `localtime("<HH:mm:ss>")` — ISO 8601 extended time. `localtime("14:30:00")` is valid. Seconds may be omitted: `localtime("14:30")` is valid (implies `:00`).

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `localtime + hours(n)` | `localtime` | Wraps at midnight. `LocalTime.PlusHours(long)`. |
| `localtime + minutes(n)` | `localtime` | Wraps at midnight. `LocalTime.PlusMinutes(long)`. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering within a day. |

| **Not supported** | **Why** |
|---|---|
| `localtime - localtime` | Ambiguous: does 23:00 - 01:00 = 22 hours or -22 hours? Defer until use case is clear. |
| `localtime + integer` | What unit? Use `hours(n)` or `minutes(n)` explicitly. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.hour` | `integer` | Hour (0–23) |
| `.minute` | `integer` | Minute (0–59) |
| `.second` | `integer` | Second (0–59) |

**Constraints:** `nullable`, `default localtime(...)`.

**Serialization:** ISO 8601 time string: `"14:30:00"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `localtime("25:00:00")` | Invalid time: hour must be 0–23, got 25. |
| `AppointmentTime + 30` | Cannot add an integer to a time. Use `localtime + minutes(30)` or `localtime + hours(1)` to specify the unit. |

---

### `instant`

**What it makes explicit:** This is a point on the global timeline — UTC, no timezone ambiguity. Not a date, not "seconds since epoch" encoded as a number.

**Backing type:** `NodaTime.Instant`

**Declaration:**

```precept
field FiledAt as instant nullable
field IncidentTimestamp as instant
```

**Literal / Constructor syntax:** `instant("<ISO-8601-UTC>")` — trailing `Z` required. `instant("2026-04-13T14:30:00Z")` is valid. Without `Z`: compile error.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `instant - instant` | `duration` | Elapsed time between two points. Pure nanosecond subtraction. |
| `instant + duration` | `instant` | Point in time offset by duration. |
| `instant - duration` | `instant` | Point in time offset backward. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering — nanosecond comparison. |

| **Not supported** | **Why** |
|---|---|
| `instant.year`, `.month`, `.hour`, etc. | **Compile error.** Extracting calendar components from an instant requires a timezone — it would be implicit behavior hiding a timezone dependency. Use `toLocalDate(instant, timezone).year` instead. This is the canonical implicit-timezone bug that NodaTime was designed to prevent. |
| `instant + integer` | What unit? Use `instant + hours(n)` or `instant + minutes(n)`. |
| `instant + months(n)` | Months are calendar units, not timeline units. "1 month" has no fixed duration. Convert to a local date, add the month, convert back. |

**Accessors:** **None.** Deliberately empty. To get calendar components, call `toLocalDate(instant, timezone)` and access `.year` on the result. The conversion makes the timezone dependency explicit.

**Constraints:** `nullable`, `default instant(...)`.

**Serialization:** ISO 8601 UTC string: `"2026-04-13T14:30:00Z"` (always trailing `Z`).

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `instant("2026-04-13T14:30:00")` | Instant requires UTC designation. Add trailing Z: `instant("2026-04-13T14:30:00Z")`. |
| `FiledAt.year` | Cannot access calendar components on an instant — this requires a timezone. Use `toLocalDate(FiledAt, timezone_field).year` to extract the year in a specific timezone. |
| `FiledAt + 3600` | Cannot add an integer to an instant. Use `FiledAt + hours(1)` or `FiledAt + seconds(3600)`. |

---

### `duration`

**What it makes explicit:** This is an elapsed amount of time — a fixed count of nanoseconds. Not a calendar interval, not an hour-count encoded as an integer.

**Backing type:** `NodaTime.Duration`

**Constructor functions:**

```precept
hours(72)       # 72 hours as a duration
minutes(30)     # 30 minutes
seconds(3600)   # 3600 seconds
```

These are thin wrappers around `Duration.FromHours`, `Duration.FromMinutes`, `Duration.FromSeconds`.

**Composite durations** are built with duration arithmetic:

```precept
days(5) + hours(7) + seconds(32)    # 5 days, 7 hours, and 32 seconds
```

**Literal / Constructor syntax:** `duration("<ISO-8601-duration>")` — ISO 8601 duration format. `duration("PT72H")` is valid (72 hours). `duration("PT8H30M")` is valid (8 hours 30 minutes). `duration("P5DT7H32S")` is valid (5 days, 7 hours, 32 seconds). Parsing via `NodaTime.Text.DurationPattern`.

| Expression | Valid? | Notes |
|---|---|---|
| `duration("PT72H")` | Yes | 72 hours |
| `duration("PT8H30M")` | Yes | 8 hours 30 minutes |
| `duration("P5DT7H32S")` | Yes | 5 days, 7 hours, 32 seconds |
| `duration("8 hours")` | No | Not ISO 8601 format. Use `duration("PT8H")` or `hours(8)`. |
| `duration("")` | No | Empty duration string. Use `hours(0)` for zero duration. |

The string constructor and the function constructors are interchangeable — `duration("PT72H")` and `hours(72)` produce the same value. The function constructors are preferred for single-unit durations (more readable); the string constructor serves composite durations that would otherwise require chained addition.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `duration + duration` | `duration` | Combined elapsed time. |
| `duration - duration` | `duration` | Difference in elapsed time. |
| `duration * integer` or `duration * number` | `duration` | Scaling — e.g., `SlaWindow * 2`, `BaseDuration * Rate`. NodaTime: `Duration * long`, `Duration * double`. `decimal` excluded — `decimal → double` is a lossy narrowing conversion, violating explicit-over-implicit. |
| `duration / integer` or `duration / number` | `duration` | Scaling — e.g., `SlaWindow / 2`. NodaTime: `Duration / long`, `Duration / double`. Same `decimal` exclusion. |
| `duration / duration` | `number` | Ratio — e.g., `Elapsed / ShiftLength` counts how many shifts fit. NodaTime: `Duration / Duration → double`. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering — nanosecond comparison. |

| **Not supported** | **Why** |
|---|---|
| `duration * duration` | Multiplying two durations is dimensionally meaningless. |
| `duration * decimal` | `decimal → double` is a lossy narrowing conversion. Use `number` for scaling operands. |
| `integer * duration` / `number * duration` | Use `duration * integer` or `duration * number` — duration is always the left operand, matching the Precept convention for temporal types. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.totalHours` | `number` | Total elapsed hours (may be fractional). |
| `.totalMinutes` | `number` | Total elapsed minutes. |
| `.totalSeconds` | `number` | Total elapsed seconds. |

Note: Accessors return `number` because these are unit conversions that may produce fractional results (e.g., 90 minutes = 1.5 hours).

**Serialization:** ISO 8601 duration-style string based on total hours: `"PT72H"` for 72 hours.

#### Option: `duration` as declared field type vs. expression-result only

##### Option A: `duration` as a declared field type

```precept
field EstimatedHours as duration default hours(8)
field ActualHours as duration default hours(0)
```

**Pros:** 9 fields across 6 samples in the corpus (MTBF, repair hours, work hours) are naturally `duration`. Avoids encoding elapsed time as `integer` or `decimal`. Authoring intent is clear. `nullable`, `default`, and constraints like `nonnegative` apply naturally.

**Cons:** Adds a storable type to the serialization surface. Default semantics need definition (what is the "zero" duration? `hours(0)` is natural).

##### Option B: Expression-result only (like `Period` in NodaTime's internal role)

`duration` exists as the result type of `instant - instant` and in constructor functions (`hours(n)`, etc.) but cannot be declared as a field type.

**Pros:** Smaller type system surface. Duration fields are modeled as `integer` or `decimal` with naming conventions.

**Cons:** The same type-safety gap that justified `localdate` over `string` applies: `integer` fields lose domain semantics, and `EstimatedHours + InvoiceTotal` compiles.

**Recommendation:** Option A — `duration` as a declared field type. The corpus evidence (9 fields across 6 samples) and the type-safety parity argument with `localdate` both support it. The "zero" duration (`hours(0)`) is a natural default.

**Constraints (if field type):** `nullable`, `default hours(...)` or `default minutes(...)`, `nonnegative`.

---

### `timezone`

**What it makes explicit:** This is a valid IANA timezone identifier — not an arbitrary string that might hold "California" or "EST" or "Pacific Standard Time".

**Backing type:** `NodaTime.DateTimeZone`

**Declaration:**

```precept
field IncidentTimezone as timezone
field CustomerTimezone as timezone nullable
```

**Literal / Constructor syntax:** `timezone("<IANA-identifier>")` — validated at compile time. `timezone("America/Los_Angeles")` is valid. `timezone("EST")` produces a warning (abbreviation, not IANA). `timezone("Not/A/Timezone")` is a compile error.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `==`, `!=` | `boolean` | Equality by canonical IANA identifier. |

| **Not supported** | **Why** |
|---|---|
| `<`, `>`, `<=`, `>=` | Timezones have no meaningful sort order. |
| Arithmetic of any kind | You cannot add, subtract, or compare timezones in a domain-meaningful way. |

**Accessors:** None. A timezone is a metadata identifier, not a temporal value. Timezone data is consumed by conversion functions, not inspected directly.

**Validation:**
- **Compile-time:** Literal timezone strings are validated against the IANA TZ database bundled with NodaTime. Deprecated aliases (e.g., `"US/Pacific"`) produce warnings suggesting the canonical form.
- **Runtime:** Event arguments declared `as timezone` are validated at fire time. Invalid timezone strings produce a constraint-violation-style rejection, not a runtime exception.

**Constraints:** `nullable`. No `default` — there is no universally sensible default timezone.

**Serialization:** IANA identifier string: `"America/Los_Angeles"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `timezone("EST")` | Warning: "EST" is a timezone abbreviation, not an IANA identifier. Did you mean `timezone("America/New_York")`? |
| `timezone("Pacific Standard Time")` | "Pacific Standard Time" is a Windows timezone name, not an IANA identifier. Use `timezone("America/Los_Angeles")`. |
| `timezone("Not/A/Timezone")` | Unknown IANA timezone identifier: "Not/A/Timezone". See https://en.wikipedia.org/wiki/List_of_tz_database_time_zones for valid identifiers. |

**Why `timezone` over `string`:** The `localdate`-over-`string` argument applies with equal force. `field IncidentTimezone as string` means the compiler can't reject `"Not/A/Timezone"` or `"California"`. The `timezone` type validates at compile time (literals) and fire time (event args), closing the type-safety gap that the `localdate` type closes for calendar dates.

---

### `zoneddatetime`

**What it makes explicit:** This is a datetime with timezone context — an instant resolved to local date and time in a specific timezone. The zone travels with the value, so component access and calendar arithmetic are always deterministic: the bound timezone resolves calendar components, and DST transitions are handled via the proposal's lenient resolver strategy (see [DST ambiguity resolution](#dst-ambiguity-resolution)). Conversion functions that accept a `zoneddatetime` preserve timezone provenance across decomposition and recomposition.

**Backing type:** `NodaTime.ZonedDateTime`

**Declaration:**

```precept
field IncidentContext as zoneddatetime
field FilingContext as zoneddatetime nullable
```

**Constructor syntax:** `zoneddatetime(instant_expr, timezone_expr)` — two required arguments. Both must be present; co-assignment is structural.

```precept
-> set IncidentContext = zoneddatetime(Submit.Timestamp, Submit.Timezone)
```

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `zoneddatetime + days(n)` | `zoneddatetime` | Calendar arithmetic. DST resolved via lenient strategy. |
| `zoneddatetime + months(n)` | `zoneddatetime` | Calendar arithmetic via Period. Month-end truncation applies. |
| `zoneddatetime + years(n)` | `zoneddatetime` | Calendar arithmetic. Leap year handling. |
| `zoneddatetime + weeks(n)` | `zoneddatetime` | Calendar arithmetic (= 7N days). |
| `zoneddatetime + hours(n)` | `zoneddatetime` | Timeline arithmetic. Duration added to underlying instant, re-resolved in zone. |
| `zoneddatetime + minutes(n)` | `zoneddatetime` | Timeline arithmetic. |
| `zoneddatetime + duration` | `zoneddatetime` | Timeline arithmetic with composed duration. |
| `zoneddatetime - duration` | `zoneddatetime` | Timeline arithmetic backward. |
| `zoneddatetime - zoneddatetime` | `duration` | Elapsed time between two points (instant subtraction). |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Comparison by underlying instant. |

Calendar arithmetic on `zoneddatetime` uses the same lenient resolver strategy defined in [DST ambiguity resolution](#dst-ambiguity-resolution): gaps map to the post-gap instant, overlaps map to the later instant. This applies to `+ days(n)`, `+ months(n)`, `+ years(n)`, and `+ weeks(n)`. Timeline arithmetic (`+ hours(n)`, `+ minutes(n)`, `+ duration`) operates on the underlying instant and re-resolves in the bound timezone — no ambiguity arises.

| **Not supported** | **Why** |
|---|---|
| `zoneddatetime + zoneddatetime` | Adding two zoned datetimes is meaningless — same as `date + date`. |
| `zoneddatetime + integer` | Bare integers don't carry unit semantics. Use `+ days(n)` or `+ hours(n)`. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.instant` | `instant` | The underlying UTC point in time. |
| `.timezone` | `timezone` | The bound IANA timezone. |
| `.date` | `localdate` | Local calendar date in the bound timezone. |
| `.time` | `localtime` | Local time of day in the bound timezone. |
| `.year` | `integer` | Local calendar year in the bound timezone. |
| `.month` | `integer` | Local month (1–12) in the bound timezone. |
| `.day` | `integer` | Local day of month (1–31) in the bound timezone. |
| `.hour` | `integer` | Local hour (0–23) in the bound timezone. |
| `.minute` | `integer` | Local minute (0–59) in the bound timezone. |
| `.second` | `integer` | Local second (0–59) in the bound timezone. |
| `.dayOfWeek` | `integer` | ISO day of week (Monday=1, Sunday=7) in the bound timezone. |

**Constraints:** `nullable`. No `default` — there is no universally sensible default timezone, so there is no universally sensible default `zoneddatetime`. Fields are either `nullable` or populated by events.

**Serialization:** Two-property JSON object in MCP payloads:

```json
{
  "IncidentContext": {
    "instant": "2026-04-13T14:30:00Z",
    "timezone": "America/Los_Angeles"
  }
}
```

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `zdt + 5` | Cannot add an integer to a zoneddatetime — use `zdt + days(5)` or `zdt + hours(5)` to specify the unit. |
| `field X as zoneddatetime default zoneddatetime(...)` | zoneddatetime fields cannot have a default — there is no universally sensible default timezone. Use `nullable` or populate via events. |

---

### `localdatetime`

**What it makes explicit:** This is a date and time together — not a point on the global timeline, not two separate fields pretending to be coupled. No timezone.

**Backing type:** `NodaTime.LocalDateTime`

**Declaration:**

```precept
field DetectedAt as localdatetime nullable
field ScheduledFor as localdatetime default localdatetime("2026-04-13T09:00:00")
```

**Literal / Constructor syntax:** `localdatetime("<YYYY-MM-DD>T<HH:mm:ss>")` — ISO 8601 combined. No timezone suffix (that would make it a `ZonedDateTime`). `localdatetime("2026-04-13T14:30:00")` is valid.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `localdatetime + hours(n)` | `localdatetime` | Offset by hours. |
| `localdatetime + minutes(n)` | `localdatetime` | Offset by minutes. |
| `localdatetime + days(n)` | `localdatetime` | Add N calendar days. Same as localdate + days(n). |
| `localdatetime + months(n)` | `localdatetime` | Calendar arithmetic via Period. |
| `localdatetime + years(n)` | `localdatetime` | Calendar arithmetic via Period. |
| `localdatetime + duration` | `localdatetime` | Offset forward by a composed duration (e.g., `days(5) + hours(7)`). |
| `localdatetime - duration` | `localdatetime` | Offset backward by a composed duration. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering within the same calendar. |

| **Not supported** | **Why** |
|---|---|
| `localdatetime - localdatetime` | Ambiguous without timezone context. If both localdatetimes are in the same implied local context, is the subtraction calendar-based or timeline-based? Defer pending use case clarity. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.date` | `localdate` | Date component extraction. |
| `.time` | `localtime` | Time component extraction. |
| `.year` | `integer` | Calendar year. |
| `.month` | `integer` | Month (1–12). |
| `.day` | `integer` | Day of month (1–31). |
| `.hour` | `integer` | Hour (0–23). |
| `.minute` | `integer` | Minute (0–59). |
| `.second` | `integer` | Second (0–59). |

**Constraints:** `nullable`, `default localdatetime(...)`.

**Serialization:** ISO 8601 string without timezone: `"2026-04-13T14:30:00"`.

#### Option: `localdatetime` inclusion vs. deferral

##### Option A: Include `localdatetime` in the initial proposal

**Pros:** Completes the local type vocabulary. Security-incident sample (6 NIST compliance timestamps) demonstrates real need. Enables `toInstant(localdate, localtime, timezone)` alternative as `localdatetime + timezone → instant` conversion.

**Cons:** Only 1 of 15 samples requires it. Most combined date+time use cases can be handled with `localdate` + `localtime` as separate fields. Higher implementation cost for thin corpus evidence.

##### Option B: Defer `localdatetime` to a follow-up proposal

**Pros:** Smaller initial surface. Focus resources on the high-frequency types (`localdate`, `instant`, `duration`, `localtime`, `timezone`). Ship `localdatetime` when enterprise adoption demonstrates that separate `localdate` + `localtime` fields create friction.

**Cons:** Security-incident and audit-trail domains remain underserved. Authors who need sub-day local timestamps encode them as strings.

**Recommendation:** Open — needs design discussion. The corpus evidence is thin (6 fields in 1 sample), but the use case is real (NIST incident response). Including it completes the type vocabulary cleanly; deferring it reduces the initial surface.

---

## Conversion Functions

### The timezone bridge

Conversion functions bridge the timeline domain (instants, durations) and the calendar domain (dates, times). They make the timezone dependency explicit — visible in the expression, typed at the call boundary, inspectable by `precept_inspect`.

| Function | Signature | Semantics |
|---|---|---|
| `toLocalDate` | `(instant, timezone) → localdate` | Extract the local calendar date for an instant in a timezone. "What date is it at this moment in this timezone?" |
| `toLocalTime` | `(instant, timezone) → localtime` | Extract the local time-of-day for an instant in a timezone. "What time is it at this moment in this timezone?" |
| `toInstant` | `(localdate, localtime, timezone) → instant` | Convert local date + time-of-day + timezone → UTC instant. "What UTC moment corresponds to this local time in this timezone?" |

### Why these functions exist

Without them, multi-timezone compliance rules must be split between the precept and the hosting layer:

```precept
# WITHOUT conversion functions — rule is split
field FilingDeadline as instant  # Computed by hosting layer — the MEANING is not in this file
invariant FiledTimestamp <= FilingDeadline because "Filing deadline has passed"
```

With them, the entire rule lives in one file:

```precept
# WITH conversion functions — rule is complete
invariant FiledTimestamp <= toInstant(
    toLocalDate(IncidentTimestamp, IncidentTimezone) + days(30),
    localtime("23:59:00"),
    IncidentTimezone
) because "Claim must be filed by 11:59 PM local time on the 30th day after the incident"
```

### DST ambiguity resolution

When `toInstant` converts local date + time + timezone → UTC instant, DST transitions can create ambiguity:

- **Gap (clocks spring forward):** The local time doesn't exist. Resolution: map to the instant *after* the gap (standard spring-forward behavior). This is NodaTime's `Resolvers.LenientResolver` for gaps.
- **Overlap (clocks fall back):** The local time occurs twice. Resolution: map to the *later* instant (the offset after the transition). This is the safer choice for deadline calculations — it gives the later, more generous deadline.

#### Option: DST resolution strategy

##### Option A: Lenient resolver (recommended above)

Gaps → post-gap instant. Overlaps → later instant. Deterministic. Matches NodaTime's `LenientResolver`. Favors the more generous interpretation for deadline calculations.

**Pros:** Simple mental model. Favors compliance safety (later deadline is safer for filers). Single deterministic strategy — no author choice needed.

**Cons:** "Lenient" means the resolution silently adjusts invalid local times. Authors may not realize `localtime("02:30:00")` at a spring-forward boundary was adjusted to `localtime("03:00:00")`.

##### Option B: Strict resolver — reject ambiguous conversions

Gaps and overlaps produce constraint violations. The author must handle DST boundaries explicitly.

**Pros:** No silent adjustment. The author is forced to account for DST.

**Cons:** Extremely burdensome for the common case. Most timezone conversions will never hit a DST boundary. Requiring DST handling in every conversion penalizes the 99% case for the 1% edge case.

**Recommendation:** Option A — lenient resolver. The determinism guarantee is preserved (same inputs = same output), and the 99% case is unaffected. The resolution strategy should be documented in `PreceptLanguageDesign.md` and visible in `precept_inspect` output when a DST adjustment occurs.

### Composability

Conversion functions produce and consume existing types — they are bridges, not new types:

- `toLocalDate` returns a `localdate` — all localdate operations (comparison, `.year`, `+ days(n)`, `+ months(n)`) work.
- `toLocalTime` returns a `localtime` — all localtime operations (comparison, `.hour`) work.
- `toInstant` returns an `instant` — all instant comparison and duration arithmetic work.

### `zoneddatetime` overloads

The conversion functions gain `zoneddatetime` overloads that **preserve timezone provenance**. The bound timezone travels with the value — the author cannot accidentally pair a date produced from one timezone with a recomposition using a different timezone.

| Function | New Overload | Semantics |
|---|---|---|
| `toLocalDate` | `(zoneddatetime) → localdate` | Extract local date using the **bound** timezone. Provenance-safe. |
| `toLocalTime` | `(zoneddatetime) → localtime` | Extract local time using the bound timezone. |
| `toInstant` | `(localdate, localtime, zoneddatetime) → instant` | Convert back to instant using the **same timezone** the original conversion came from. |

The provenance-safe pattern:

```precept
# Provenance-safe: zone is carried by the composite
invariant FiledTimestamp <= toInstant(
    toLocalDate(IncidentContext) + days(30),
    localtime("23:59:00"),
    IncidentContext
) because "Claim must be filed by 11:59 PM local time on the 30th day after the incident"
```

The `IncidentContext` carries both the instant and the timezone. `toLocalDate(IncidentContext)` extracts the local date using the bound zone. `toInstant(localdate, localtime, IncidentContext)` recomposes using the same zone. The zone mismatch bug from the decomposed approach **cannot be expressed**.

The two-argument forms remain — both patterns coexist:

```
toLocalDate(instant, timezone) → localdate     # still valid
toLocalDate(zoneddatetime) → localdate         # new overload — provenance-safe
```

Authors who prefer decomposed storage use the two-argument form. Authors who use `zoneddatetime` get the overload that preserves provenance. No breaking change.

### Duration constructor functions

Duration constructors are the **required interface** for all temporal arithmetic. Bare integers are never valid temporal offsets — every arithmetic operation on a temporal type requires an explicit duration constructor that names the unit.

| Function | Signature | Semantics |
|---|---|---|
| `days(n)` | `(integer) → duration` | Calendar period of N days. Used with `localdate +`, `localdatetime +`. Internally `Period.FromDays`. |
| `months(n)` | `(integer) → duration` | Calendar period of N months. Truncates at month end (Jan 31 + 1 mo = Feb 28). Internally `Period.FromMonths`. |
| `years(n)` | `(integer) → duration` | Calendar period of N years. Handles leap years. Internally `Period.FromYears`. |
| `weeks(n)` | `(integer) → duration` | Calendar period of N weeks (= 7N days). Internally `Period.FromWeeks`. |
| `hours(n)` | `(integer) → duration` | Duration of N hours. Used with `localtime +`, `instant +`, `localdatetime +`. Internally `Duration.FromHours`. |
| `minutes(n)` | `(integer) → duration` | Duration of N minutes. Internally `Duration.FromMinutes`. |
| `seconds(n)` | `(integer) → duration` | Duration of N seconds. Internally `Duration.FromSeconds`. |

Internally, calendar-unit constructors (`days`, `months`, `years`, `weeks`) produce `NodaTime.Period` values, while timeline constructors (`hours`, `minutes`, `seconds`) produce `NodaTime.Duration` values. The DSL author does not see this distinction — all seven are "duration constructors" at the language surface. `Period` is not exposed as a DSL type.

#### Option: Duration constructor syntax — function call vs. postfix suffix

##### Option A: Function-call syntax only (current)

```precept
set DueDate = CreatedDate + days(30)
set Deadline = StartDate + days(GracePeriodDays)
set EndTime = StartTime + hours(ShiftLength)
```

**Pros:** Consistent for both literals and variables. No new grammar construct — function calls are already parsed. Familiar to programmers.

**Cons:** Less natural for literal constants. `days(30)` reads like a function call, not "30 days."

##### Option B: Postfix suffix syntax only

```precept
set DueDate = CreatedDate + 30 days
set Deadline = StartDate + GracePeriodDays days
set EndTime = StartTime + ShiftLength hours
```

**Pros:** Reads like natural English for literals: `DueDate + 30 days`. Aligns with Principle #2 (English-ish). Matches Kotlin, Ruby, PromQL, and CSS — languages optimized for readability use postfix.

**Cons:** Awkward with variables: `GracePeriodDays days` lacks the visual cue that a function call provides. Parser impact: `days`, `hours`, etc. become context-sensitive keywords (after numeric literals or identifiers in expression position). Superpower handles this as single-token lookahead, but it's still new grammar surface.

##### Option C: Both syntaxes (dual form)

Suffix for literals, function call for variables and complex expressions:

```precept
set DueDate = CreatedDate + 30 days
set Deadline = StartDate + days(GracePeriodDays)
set EndTime = StartTime + hours(BaseHours + Overtime)
```

**Pros:** Best of both — natural English for the common literal case, clean syntax for the variable case. Authors choose the form that reads best.

**Cons:** Two ways to do the same thing. Language surface is larger. Grammar is more complex. Teaching burden: "when do I use which?"

##### External precedent

| Language | Syntax | Family |
|---|---|---|
| Kotlin | `5.days`, `5.hours` | Postfix (extension property) |
| Ruby | `5.days`, `5.hours` | Postfix (monkey-patch) |
| Swift | No built-in suffix | Function call |
| Scala | `5.days`, `5.hours` (via FiniteDuration) | Postfix (implicit conversion) |
| Go | `5 * time.Hour` | Multiplication |
| Rust | No built-in suffix | Function call |
| PromQL | `5d`, `5h` | Postfix (abbreviation) |
| CSS | `5s`, `500ms` | Postfix (unit) |
| PostgreSQL | `INTERVAL '5 days'` | String parse |
| Terraform | `"5h"`, `"30m"` | String parse |
| Kubernetes | `5d`, `30m` | Postfix (abbreviation) |
| F# | No built-in suffix | Function call (`TimeSpan.FromDays 5.0`) |

**Key finding:** Languages optimized for readability (Kotlin, Ruby, Scala) adopt postfix suffix. Languages optimized for precision/generality (Rust, Go, F#) use function calls. Precept's Principle #2 ("English-ish but not English") suggests readability is a priority, but Principle #8 ("Sound, compile-time-first") suggests the variable case must be clean too.

**Parser note:** Postfix suffix requires `days`, `hours`, `minutes`, `seconds`, `weeks`, `months`, `years` to be recognized as unit keywords when they follow a numeric literal or identifier in expression position. Superpower handles this as single-token lookahead — trivial to implement. The keywords would be context-sensitive (only treated as unit suffixes in expression position after a number/identifier, not as bare identifiers elsewhere).

**Recommendation:** Open — needs design discussion. The literal readability advantage of suffix syntax is compelling (Principle #2), but the variable awkwardness is a real concern. If both forms are offered (Option C), the language surface grows but authors get the most readable form for each context.

---

## Semantic Rules

### Type-interaction matrix

The following matrix defines what operations are valid between temporal types. Anything not listed is a type error.

| Left operand | Operator | Right operand | Result | Notes |
|---|---|---|---|---|
| `localdate` | `+` | `days(n)` | `localdate` | Add N calendar days |
| `localdate` | `-` | `days(n)` | `localdate` | Subtract N calendar days |
| `localdate` | `+` | `months(n)` | `localdate` | Calendar arithmetic; truncates at month end |
| `localdate` | `+` | `years(n)` | `localdate` | Calendar arithmetic; handles leap years |
| `localdate` | `+` | `weeks(n)` | `localdate` | = 7N days |
| `localdate` | `-` | `localdate` | `duration` | Elapsed days between dates |
| `instant` | `-` | `instant` | `duration` | Elapsed time between two points |
| `instant` | `+` | `duration` | `instant` | Point offset forward |
| `instant` | `-` | `duration` | `instant` | Point offset backward |
| `duration` | `+` | `duration` | `duration` | Combined elapsed time |
| `duration` | `-` | `duration` | `duration` | Difference |
| `duration` | `*` | `integer` or `number` | `duration` | Scaling (e.g., `SlaWindow * ShiftCount`) |
| `duration` | `/` | `integer` or `number` | `duration` | Scaling (e.g., `SlaWindow / 2`) |
| `duration` | `/` | `duration` | `number` | Ratio (e.g., how many shifts fit) |
| `localtime` | `+` | `hours(n)` | `localtime` | Wraps at midnight |
| `localtime` | `+` | `minutes(n)` | `localtime` | Wraps at midnight |
| `localdatetime` | `+` | `days(n)` | `localdatetime` | Add N days |
| `localdatetime` | `+` | `months(n)` | `localdatetime` | Calendar arithmetic |
| `localdatetime` | `+` | `years(n)` | `localdatetime` | Calendar arithmetic |
| `localdatetime` | `+` | `hours(n)` | `localdatetime` | Time arithmetic |
| `localdatetime` | `+` | `minutes(n)` | `localdatetime` | Time arithmetic |
| `localdatetime` | `+` | `duration` | `localdatetime` | Offset forward by composed duration (e.g., `days(5) + hours(7)`) |
| `localdatetime` | `-` | `duration` | `localdatetime` | Offset backward by composed duration |
| `zoneddatetime` | `+` | `days(n)` | `zoneddatetime` | Calendar arithmetic; DST via lenient resolver |
| `zoneddatetime` | `+` | `months(n)` | `zoneddatetime` | Calendar arithmetic; month-end truncation |
| `zoneddatetime` | `+` | `years(n)` | `zoneddatetime` | Calendar arithmetic; leap years |
| `zoneddatetime` | `+` | `weeks(n)` | `zoneddatetime` | = 7N days |
| `zoneddatetime` | `+` | `hours(n)` | `zoneddatetime` | Timeline arithmetic |
| `zoneddatetime` | `+` | `minutes(n)` | `zoneddatetime` | Timeline arithmetic |
| `zoneddatetime` | `+` | `duration` | `zoneddatetime` | Timeline arithmetic with composed duration |
| `zoneddatetime` | `-` | `duration` | `zoneddatetime` | Timeline arithmetic backward |
| `zoneddatetime` | `-` | `zoneddatetime` | `duration` | Elapsed time (instant subtraction) |
| `zoneddatetime` | `==`, `!=`, `<`, `>`, `<=`, `>=` | `zoneddatetime` | `boolean` | Comparison by underlying instant |

### Comparison rules

All temporal types support `==`, `!=`. `localdate`, `localtime`, `instant`, `duration`, `localdatetime`, `zoneddatetime` support `<`, `>`, `<=`, `>=`. `timezone` supports only `==`, `!=` — no ordering.

Cross-type comparison is always a type error:
- `localdate == instant` → type error
- `localtime == duration` → type error
- `localdate == localdatetime` → type error

### Cross-type arithmetic: what's NOT allowed (and why)

| Expression | Why it's a type error |
|---|---|
| `localdate + localdate` | Adding two dates is meaningless. |
| `localdate + instant` | Different temporal domains (calendar vs. timeline). |
| `localdate + integer` | Bare integers don't carry unit semantics. Use `localdate + days(n)` for day offsets. |
| `localdate + decimal` | Fractional days are meaningless at day granularity. |
| `localdate + number` | `number` is floating-point; temporal arithmetic requires explicit duration constructors. |
| `instant + integer` | Ambiguous unit. Use `instant + hours(n)` or `instant + seconds(n)`. |
| `instant + months(n)` | Months are calendar units with no fixed duration. Convert to localdate, add, convert back. |
| `instant.year` | Requires a timezone. Use `toLocalDate(instant, timezone).year`. |
| `localtime - localtime` | Ambiguous sign (see `localtime` section). |
| `duration * duration` | Dimensionally meaningless. |
| `integer * duration` / `number * duration` | Use `duration * integer` or `duration * number` — duration is always the left operand. |
| `duration * decimal` / `duration / decimal` | `decimal → double` is lossy. Use `number` for scaling operands. |
| `integer / duration` / `number / duration` | Dimensionally meaningless (what is "5 / 3 hours"?). |
| `timezone + anything` | Timezones are metadata, not temporal values. |
| `zoneddatetime + integer` | Bare integers don't carry unit semantics. Use `zoneddatetime + days(n)` or `+ hours(n)`. |
| `zoneddatetime + zoneddatetime` | Adding two zoned datetimes is meaningless. |

### Nullable behavior

All temporal types support `nullable`. Nullable temporal fields follow existing null semantics:
- Comparison with null follows existing null propagation rules.
- Accessors on a nullable field require null-check narrowing (same as collection accessors).

### Default behavior

| Type | Default value | Notes |
|---|---|---|
| `localdate` | `default localdate("...")` | Author specifies the date. |
| `localtime` | `default localtime("...")` | Author specifies the time. |
| `instant` | `default instant("...")` | Author specifies the UTC instant. |
| `duration` | `default hours(0)` (or `minutes(0)`, etc.) | Zero duration is natural. |
| `timezone` | No default | No universally sensible default timezone. |
| `zoneddatetime` | No default | No universally sensible default timezone (same as `timezone`). |
| `localdatetime` | `default localdatetime("...")` | Author specifies the localdatetime. |

#### Option: `nullable` + `default` for temporal fields

##### Option A: Prohibit `nullable` + `default` on temporal fields

A field is either nullable (absent until populated) or has a default (always present). This was the original #26 position.

**Pros:** Clear semantics — a field is either "unknown" or "has a known starting value." Prevents the ambiguity of "is null the default, or is the default the default?"

**Cons:** Some domain patterns naturally want both: "this field starts as null (unknown), but once populated, reverts to a default on reset." This feels like arbitrary restriction.

##### Option B: Allow `nullable` + `default` — null means "not yet provided," default is the initial value

When both are specified, the field starts at the default value but can be set to null explicitly. Null means "cleared/unknown."

**Pros:** Maximum flexibility. No arbitrary restriction.

**Cons:** `default localdate("2026-01-01") nullable` — if the field starts at the default, when is it ever null? The semantics are confusing. `null` becomes "explicitly cleared" rather than "never set."

##### Option C: Allow `nullable` + `default` — default is the initial value, null is a valid assignment target

Same as Option B but with clearer documentation: the field initializes to the default and `null` is a valid value that can be assigned via events.

**Recommendation:** Open — this is George's Challenge #3 from the original #26 review. The question is orthogonal to temporal types and applies to all types. Defer to a cross-cutting design decision rather than deciding it here.

---

## Locked Design Decisions

Each locked decision follows the 4-point rationale format required by CONTRIBUTING.md.

### 1. NodaTime as the backing library for all temporal types

- **Why:** NodaTime's type model matches Precept's philosophy (explicit over implicit), provides battle-tested arithmetic, and creates a coherent path for the full temporal vocabulary. Building temporal arithmetic on BCL types would require Precept to own correctness in a domain it has no expertise in.
- **Alternatives rejected:** `System.DateOnly` / `TimeOnly` — cheaper for `localdate` alone but creates increasing debt for `localtime`, `localdatetime`, and cross-type conversions. `System.DateTime` — conflates concepts; the exact problem NodaTime was created to solve. No backing library (custom implementation) — unmaintainable temporal arithmetic with no precedent or test coverage.
- **Precedent:** NRules inherits `System.Decimal` from .NET. Precept inherits NodaTime's temporal model. The pattern is: use the best domain-specific library, expose only DSL-appropriate operations.
- **Tradeoff accepted:** Additional NuGet dependency (~1.1 MB, well-maintained).

### 2. Day granularity for `localdate` — no time-of-day component

- **Why:** "2026-03-15" means the same calendar day everywhere. Adding time-of-day introduces timezone dependency. Precept's determinism guarantee requires identical results regardless of evaluation location.
- **Alternatives rejected:** `localdatetime` as the only calendar type — forces timezone reasoning on every author for rules operating at day granularity. Optional timezone — creates two semantics in one type.
- **Precedent:** SQL's `DATE`, FEEL's `date()`, Cedar's `datetime` — all timezone-naive at the date level.
- **Tradeoff accepted:** Authors needing time-of-day use `localtime` (separate type) or `localdatetime`.

### 3. ISO 8601 as the sole format — no custom format strings

- **Why:** A single canonical format eliminates parsing ambiguity. Is `01/02/2026` January 2nd or February 1st? ISO 8601 is unambiguous by construction. Same literal, same meaning, always.
- **Alternatives rejected:** Configurable format strings — adds parsing complexity and creates precepts interpretable only with the format specifier. Auto-detection — heuristic and non-deterministic.
- **Precedent:** Cedar (RFC 3339), FEEL (ISO 8601), SQL DATE (ISO format).
- **Tradeoff accepted:** Authors from `MM/DD/YYYY` regions must adapt. One-time learning cost.

### 4. Constructor form for all temporal literals

- **Why:** `localdate("2026-03-15")`, not bare `"2026-03-15"`. A bare string is type-ambiguous. Context-dependent type resolution violates Principle #9 (tooling drives syntax) — IntelliSense and semantic tokens can't determine type without full expression analysis. The constructor form makes type intent visible at the lexical level.
- **Alternatives rejected:** Bare strings with inferred type — ambiguous. Sigil prefix (`#2026-03-15`) — no precedent. Method-style `Date.parse()` — implies namespaces Precept doesn't have.
- **Precedent:** FEEL `date(...)`, Cedar `datetime(...)`, Precept's existing `choice(...)` convention.
- **Tradeoff accepted:** Slightly more verbose than a bare literal.

### 5. No timezone on `localdate`, `localtime`, or `localdatetime`

- **Why:** These are calendar/clock types — they represent what's written on a calendar or displayed on a wall clock, independent of location. Timezone-aware local types would create non-deterministic comparisons ("Is it still March 15?" depends on where you are).
- **Alternatives rejected:** UTC-anchored dates — comparisons against "today" depend on timezone. Timezone as required metadata — forces timezone reasoning on calendar-day rules.
- **Precedent:** NodaTime's `Local*` types are timezone-free by construction.
- **Tradeoff accepted:** Timezone conversion requires explicit function calls (`toLocalDate`, etc.).

### 6. `instant` has no component accessors

- **Why:** Extracting `.year`, `.month`, `.hour` from an instant requires a timezone. Allowing it would hide a timezone dependency inside what looks like a property access — the canonical implicit-timezone bug. Precept makes the dependency explicit: `toLocalDate(instant, timezone).year`.
- **Alternatives rejected:** Component accessors with mandatory timezone parameter — syntax doesn't fit accessor pattern. Implicit UTC extraction — hides the timezone dependency.
- **Precedent:** NodaTime's `Instant` has no date/time component accessors. Same design decision, same rationale.
- **Tradeoff accepted:** More verbose than `FiledAt.year`. The verbosity makes the timezone dependency visible.

### 7. `timezone` as a first-class type, not `string`

- **Why:** Encoding timezone identifiers as `string` loses type safety — `"California"`, `"EST"`, `"Pacific Standard Time"` all compile fine as strings. The same argument that justified `localdate` over `string` applies with equal force: the type communicates intent, and the compiler enforces validity.
- **Alternatives rejected:** `string` with naming conventions — the compiler can't enforce convention. `choice` with all IANA identifiers — ~600 choices is unergonomic and requires manual updates when the IANA database changes.
- **Precedent:** NodaTime's `DateTimeZone` is a first-class type with validation. The `localdate`-over-`string` parallel in Precept is already established.
- **Tradeoff accepted:** New type in the type system. Minimal operational cost — `timezone` is an equality-only validated identifier, closer to `choice` than to `localdate` in complexity.

### 8. Conversion functions take explicit timezone parameters

- **Why:** The timezone dependency is visible in the expression: `toLocalDate(IncidentTimestamp, IncidentTimezone)` shows exactly what timezone is used. No hidden timezone in a type, no implicit system timezone, no deployment-context coupling.
- **Alternatives rejected:** Timezone stored inside a composite type (`ZonedDateTime`) — hides the dependency in the type rather than making it explicit in the expression. System timezone — deployment-dependent, non-deterministic.
- **Precedent:** NodaTime requires explicit `DateTimeZone` for all instant ↔ local conversions.
- **Tradeoff accepted:** More verbose than `ZonedDateTime.LocalDate`. Verbosity is the point — explicitness.

### 9. Determinism is relative to the runtime environment

- **Why:** "Same inputs = same output" means same `.precept` file + same entity data + same operation + same runtime environment (including .NET version, NodaTime version, TZ database version). The TZ database is an explicit, versioned, operationally managed dependency — the same category as the .NET runtime or NodaTime package version.
- **Alternatives rejected:** Excluding all timezone operations to avoid TZ database dependency — the dependency exists the moment NodaTime is adopted (the TZ database is bundled). Refusing to use it just means the hosting layer uses it instead. Mandatory TZ database version pinning — operationally impractical; breaks automatic security updates.
- **Precedent:** Every system that handles timezone-sensitive data (banking, healthcare, aviation) manages TZ database freshness. This is standard operational practice, not a new burden Precept introduces.
- **Tradeoff accepted:** The input surface for determinism expands to include TZ database version. Practically near-zero risk — the US last changed DST rules in 2007; TZ database changes affect ~1–3 jurisdictions per update.

### 10. Temporal arithmetic requires explicit constructor functions — period/duration split is visible

- **Why:** `date + 2` is implicit — what does `2` mean? Days? Months? Weeks? This is exactly the ambiguity NodaTime was designed to prevent. Precept applies the same principle: `date + days(2)`, `date + months(1)`, `time + hours(3)`. The constructor function names the unit; the compiler enforces it. Furthermore, the period/duration split is exposed at the DSL surface: calendar constructors (`days`, `months`, `years`, `weeks`) produce `period` values, while timeline constructors (`hours`, `minutes`, `seconds`) produce `duration` values. This matches NodaTime’s `Period`/`Duration` distinction exactly. Shane’s directive: *“NodaTime has already solved this hard problem. I see no reason to diverge.”*
- **Alternatives rejected:** `date + integer` meaning “add N days” — a previous position in this proposal. Merged `duration` hiding the Period/Duration distinction — the original position in this proposal. NodaTime deliberately keeps `Period` and `Duration` separate because months have no fixed length while hours do.
- **Precedent:** NodaTime’s API requires named methods (`PlusDays`, `PlusMonths`, `Plus(Period)`) for all temporal arithmetic, and keeps `Period` and `Duration` as separate types. FEEL uses `duration("P2D")` for explicit offsets.
- **Tradeoff accepted:** More verbose than `date + 2`. Two constructor result types (`period` and `duration`) rather than one. The verbosity and distinction are the point.

### 11. `localdate - localdate` returns `period` (not integer, number, or duration)

- **Why:** The result of subtracting two dates is a calendar quantity — it should carry unit semantics, not collapse to a bare integer or a timeline duration. `date - date → integer` produces a number that has lost its meaning. `date - date → duration` conflates calendar and timeline domains — it implies the result is a fixed-length time span, when in fact it’s a `Period` that knows it’s “2 months 3 days” without converting to a fixed number of hours. Returning a `period` preserves the structural calendar units and matches NodaTime’s `Period.Between(d1, d2)` exactly.
- **Alternatives rejected:** `→ integer` — loses unit semantics; the caller must *remember* that the integer means days. `→ duration` — the previous position in this proposal. Assumes the result is a fixed-length time span when the domain quantity is calendar-based. `→ number` — floating-point is unnecessary and misleading.
- **Precedent:** NodaTime’s `Period.Between(d1, d2)` returns a `Period`, not a `Duration` or `int`. This is deliberate — the result represents calendar distance (months, days), not timeline distance (nanoseconds).
- **Tradeoff accepted:** `period` result requires accessor inspection (`.days`, `.months`) or comparison with `days(n)` to extract values. The indirection is intentional — it preserves the calendar structure NodaTime was designed to express.

---

## George's Challenges — Resolution

George's design review of the original #26 proposal raised four challenges. The unified temporal type system, backed by NodaTime, resolves all four:

### Challenge 1: `DueDate + MealsTotal` compiles because `date + number → date`

**Resolution:** No form of `date + <non-duration>` is defined. The only valid temporal arithmetic uses explicit duration constructors: `date + days(n)`, `date + months(n)`, etc. `DueDate + MealsTotal` is a **type error** regardless of whether `MealsTotal` is `decimal`, `number`, or `integer` — none of these are duration values. This is strictly stronger than the previous `date + integer` rule, which would have allowed `DueDate + MealsTotal` if `MealsTotal` happened to be `integer`. The explicit-duration requirement eliminates the cross-domain error structurally.

### Challenge 2: `date + 2.5` should reject fractional offsets

**Resolution:** `date + 2.5` is a type error — and now `date + 2` is *also* a type error. No bare numeric value (integer, number, or decimal) is a valid operand for temporal arithmetic. Only explicit duration constructors are accepted: `date + days(2)`, `date + months(1)`, etc. The fractional offset `2.5` is rejected not by a granularity check but by the fundamental rule: temporal arithmetic requires duration constructors that name the unit. This is strictly stronger than the previous resolution — it eliminates *all* implicit temporal arithmetic, not just fractional offsets.

### Challenge 3: `nullable + default` prohibition seems arbitrary

**Resolution:** This is presented as an open option in this proposal (see "Option: `nullable` + `default` for temporal fields" above). The question is orthogonal to temporal types and applies to all types — it should be resolved as a cross-cutting design decision. The NodaTime backing type has no opinion here.

### Challenge 4: `date - date` result type

**Resolution:** Now `date - date` returns `period` — even more explicit than returning `integer` or `duration`. The result is a `period` value that preserves structural calendar units (“2 months 3 days”), not a bare number that has lost its meaning or a `duration` that collapses calendar distance into nanoseconds. The consumer can inspect components (`.days`, `.months`) or compare with `days(n)` expressions. This aligns with NodaTime’s `Period.Between(d1, d2)` and Shane’s directive that implicit results are as problematic as implicit operands.

---

## Dependencies and Related Issues

| Issue | Relationship |
|---|---|
| #25 (choice type) | Currency codes as `choice("USD", "EUR", ...)` complement `decimal` for money; `choice` for state/region complements `timezone` for jurisdiction lookups. |
| #26 (date type) | **Superseded by this proposal.** The `localdate` section here incorporates all of #26’s design plus NodaTime backing, explicit constructor functions (period/duration split), and month/year arithmetic. |
| #27 (decimal type) | Complementary numeric type. `decimal` and temporal types do not interact arithmetically (`decimal + date` is a type error). `duration.totalHours` returns `number`, not `decimal`. |
| #29 (integer type) | Duration constructor function args are `integer`. The `integer` type is a dependency for correct temporal arithmetic. |
| #16 (built-in functions) | `round()` from #27; `toLocalDate`, `toLocalTime`, `toInstant`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds` from this proposal. |
| #13 (field-level constraints) | `nullable`, `default`, `nonnegative` — constraint-zone architecture that temporal fields use. |

---

## Explicit Exclusions / Out of Scope

Each exclusion includes rationale — items are excluded for reasons, not convenience.

### `zoneddatetime` as a field type — Proposed (full surface)

The `zoneddatetime` type provides the full operation surface of a datetime with timezone context: component accessors (`.year`, `.hour`, `.date`, `.time`, etc.), calendar arithmetic (`+ days(n)`, `+ months(n)`), timeline arithmetic (`+ hours(n)`, `+ duration`), and subtraction (`zdt - zdt → duration`). The bound timezone makes all operations deterministic — component access resolves against the zone, and DST transitions are handled by the lenient resolver strategy defined in this proposal.

This replaces the earlier "permanently excluded" assessment. The original exclusion rationale — that component accessors depend on the TZ database — applied to `instant`, which lacks a timezone. `zoneddatetime` carries the timezone, making the dependency explicit and the resolution deterministic. The [zoneddatetime reconsideration](../../../.squad/decisions/inbox/frank-zoneddatetime-reconsideration.md) analysis upgraded it from Deferred to Proposed based on the provenance erasure problem; this revision opens the full surface based on the recognition that the `instant`-inherited bans were inapplicable.

See the `### zoneddatetime` type section in Proposed Types for the full specification.

### `OffsetDateTime` — Excluded

Carries a UTC offset without timezone rules. Same determinism concerns as `ZonedDateTime`, weaker than full timezone — offset without rules is misleading. Not useful in entity modeling.

### `AnnualDate` — Deferred

Recurring month-day combination (birthday, anniversary, annual deadline). Real demand in HR and insurance, but low corpus frequency. Evaluate after core temporal types are in use to measure demand.

### `YearMonth` — Deferred

Year-month pair (billing period, fiscal month, credit card expiry). Real demand in SaaS billing, but low priority given current evidence.

### `DateInterval` / `daterange` — Deferred

Range between two calendar dates. Two `localdate` fields with an invariant (`StartDate <= EndDate`) cover the use case with existing machinery. The marginal benefit of a composite type is co-assignment enforcement and `contains(localdate)` operations.

### Fiscal/business calendars — Excluded

Precept uses the ISO calendar exclusively. Non-Gregorian calendars (Hebrew, Islamic, Julian) and fiscal calendars (4-4-5) are outside scope. NodaTime supports multiple calendar systems; Precept does not expose them.

### Leap seconds — Excluded

NodaTime's `Instant` does not model leap seconds (it uses a "smoothed" UTC scale). This matches the reality of virtually all business computing and is not a limitation for entity governance.

### `Period` as a DSL type — Excluded

`Period` is the internal mechanism for calendar arithmetic (`date + months(1)` uses `Period.FromMonths`). Domain fields that hold period-like values (`GracePeriodDays`, `TermLengthMonths`) are more precisely modeled as `integer` fields consumed by constructor functions: `StartDate + months(TermLengthMonths)`. NodaTime's `Period` backs the functions; the DSL author doesn't see it.

### Parameterized temporal types — Excluded

No `date(format)`, `instant(precision)`, or `duration(unit)`. Temporal type behavior is fixed by the type name. Parameterized types require infrastructure that doesn't exist and provide zero practical benefit.

---

## Implementation Scope

### Parser / Tokenizer

- Add `localdate`, `localtime`, `instant`, `duration`, `timezone`, `zoneddatetime`, `localdatetime` as type keywords.
- Add `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds` as function keywords.
- Add `toLocalDate`, `toLocalTime`, `toInstant` as function keywords.
- Parse constructor forms: `localdate("...")`, `localtime("...")`, `instant("...")`, `period("...")`, `duration("...")`, `timezone("...")`, `localdatetime("...")`, `zoneddatetime(expr, expr)`.
- Parse function calls: `days(expr)`, `months(expr)`, `hours(expr)`, `toLocalDate(expr, expr)`, `toLocalDate(expr)`, `toInstant(expr, expr, expr)`, `toInstant(expr, expr, expr_zdt)`.
- Parse accessors: `.year`, `.month`, `.day`, `.dayOfWeek`, `.hour`, `.minute`, `.second`, `.date`, `.time`, `.totalHours`, `.totalMinutes`, `.totalSeconds`, `.instant`, `.timezone`.

### Type Checker

- New type entries for `localdate`, `localtime`, `instant`, `duration`, `timezone`, `zoneddatetime`, `localdatetime`.
- Operator resolution: the full cross-type interaction matrix above.
- Accessor resolution: per-type accessor tables.
- Constructor validation: ISO 8601 format check for literals (compile-time). `zoneddatetime(instant, timezone)` constructor argument type validation.
- Constraint validation: which constraints apply to which temporal types. `zoneddatetime` allows `nullable`, rejects `default`.
- Cross-type arithmetic rejection: `date + instant`, `date + integer`, `instant + months(n)`, `zoneddatetime + integer`, etc.
- `instant` component accessor rejection (new diagnostic).
- `zoneddatetime` arithmetic: calendar (`+ days`, `+ months`, `+ years`, `+ weeks`) and timeline (`+ hours`, `+ minutes`, `+ duration`, `- duration`) operators, `zdt - zdt → duration`.
- `zoneddatetime` accessor resolution: `.instant`, `.timezone`, `.date`, `.time`, `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`, `.dayOfWeek`.
- `timezone` ordering rejection (new diagnostic).
- Duration constructor argument validation: `hours(n)` requires `integer` argument.
- Conversion function overload resolution: `toLocalDate(zoneddatetime)`, `toLocalTime(zoneddatetime)`, `toInstant(date, time, zoneddatetime)`.

### Expression Evaluator

- `localdate` arithmetic via `LocalDate.PlusDays`, `LocalDate.Plus(Period)`.
- `localtime` arithmetic via `LocalTime.PlusHours`, `LocalTime.PlusMinutes`.
- `instant` arithmetic via `Instant.Plus(Duration)`, `Instant.Minus(Duration)`.
- `duration` arithmetic via `Duration.Plus`, `Duration.Minus`.
- `localdatetime` arithmetic via `LocalDateTime` methods.
- `zoneddatetime` calendar arithmetic via `ZonedDateTime.Plus(Period)` with `LenientResolver`; timeline arithmetic via underlying `Instant.Plus(Duration)` re-resolved in zone; subtraction via `zdt.ToInstant() - zdt.ToInstant()`.
- Conversion functions: `ZonedDateTimeExtensions` / `DateTimeZoneProviders.Tzdb` for timezone lookups.
- DST resolution via NodaTime's `LenientResolver` (or chosen strategy).
- Accessor evaluation: `.year` → `localDate.Year`, etc.
- Duration constructor functions: `days(n)` → `Period.FromDays(n)`, `hours(n)` → `Duration.FromHours(n)`, etc.
- Calendar constructor functions: `months(n)` → `Period.FromMonths(n)`, `years(n)` → `Period.FromYears(n)`, `weeks(n)` → `Period.FromWeeks(n)`.

### Runtime Engine

- Value carriers for all temporal types (NodaTime structs).
- Serialization/deserialization for fire/update/inspect payloads.
- Constraint enforcement for temporal fields.
- Timezone validation at fire boundary (event args typed as `timezone`).

### TextMate Grammar

- Add `localdate`, `localtime`, `instant`, `duration`, `timezone`, `zoneddatetime`, `localdatetime` to `typeKeywords` alternation.
- Add `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds` to function keyword patterns.
- Add `toLocalDate`, `toLocalTime`, `toInstant` to function keyword patterns.
- Temporal accessors (`.year`, `.totalHours`, etc.) handled by existing member-access pattern.

### Language Server

- **Completions:** Temporal types offered after `as` in field/event-arg declarations. Constructor functions offered in expression positions. Accessors offered after `.` on temporal-typed fields. Conversion functions offered in expression positions.
- **Hover:** Temporal field hover shows type, constraints, and backing NodaTime type. Constructor hover shows format requirements. Conversion function hover shows signature and DST resolution behavior.
- **Diagnostics:** All type errors from the type checker. Compile-time invalid temporal literals. Compile-time invalid timezone identifiers.
- **Semantic tokens:** Temporal type keywords colored as types (automatic via `[TokenCategory(Type)]`). Constructor/function keywords colored as functions.

### MCP Tools

- `precept_language`: Include `localdate`, `localtime`, `instant`, `duration`, `timezone`, `zoneddatetime`, `localdatetime` in type keywords. Include all constructor and conversion functions (including `zoneddatetime` overloads). TZ database version in environment info.
- `precept_compile`: Temporal field DTOs include type, constraints, and assessed properties. New diagnostics for temporal type errors.
- `precept_fire` / `precept_inspect` / `precept_update`: Temporal values serialized as ISO 8601 strings. Timezone values as IANA identifiers. Duration values as ISO 8601 duration strings.

### Samples

- Update existing samples with `FUTURE(localdate)` markers to use `localdate` fields.
- Replace day-counter simulation machinery (3 samples) with date arithmetic.
- Add or update at least one sample demonstrating `instant` / `duration` / `timezone` / conversion functions (e.g., multi-region insurance claim).

### Documentation

- `docs/PreceptLanguageDesign.md`: New section for temporal types covering syntax, operators, accessors, constraints, semantic rules.
- `docs/RuntimeApiDesign.md`: Value carrier types, serialization format, NodaTime dependency.
- `docs/McpServerDesign.md`: Temporal value serialization in MCP payloads.
- `README.md`: Update feature claims and examples to include temporal types.

### Tests

- Parser tests: all declaration forms, constructor forms, function calls.
- Type checker tests: full cross-type interaction matrix, all error cases.
- Evaluator tests: arithmetic edge cases (month-end truncation, leap years, midnight wrapping, DST boundaries).
- Runtime tests: temporal value serialization, constraint enforcement, timezone validation.
- Language server tests: completions, hover, diagnostics for temporal constructs.
- MCP tests: temporal values in compile/fire/inspect/update payloads.

---

## Acceptance Criteria

### `localdate` type

- [ ] `field X as localdate` parses and type-checks.
- [ ] `localdate("2026-03-15")` literal validates ISO 8601 at compile time.
- [ ] `localdate("2026-02-30")` produces compile-time error.
- [ ] `localdate + days(n) → localdate` works; `localdate + integer`, `localdate + number`, and `localdate + decimal` are type errors.
- [ ] `localdate + months(n)`, `localdate + years(n)`, `localdate + weeks(n)` work with correct truncation.
- [ ] `localdate + period → localdate`, `localdate - period → localdate` work.
- [ ] `localdate - localdate → period` (not `integer`, `number`, or `duration`).
- [ ] `localdate + duration` is a type error (duration is timeline-only; use calendar constructors or `+ period`).
- [ ] `.year`, `.month`, `.day`, `.dayOfWeek` return `integer`.
- [ ] Nullable and default work. Constraints `nonnegative`, `min`, `max`, etc. are compile errors.
- [ ] MCP tools serialize as ISO 8601 string.

### `localtime` type

- [ ] `field X as localtime` parses and type-checks.
- [ ] `localtime("14:30:00")` and `localtime("14:30")` validate at compile time.
- [ ] `localtime + hours(n)` and `localtime + minutes(n)` wrap at midnight correctly.
- [ ] `localtime + seconds(n)` works with proper wrapping.
- [ ] `localtime - localtime → period` (time-unit period: hours, minutes, seconds).
- [ ] `localtime + days(n)` is a type error (days are not meaningful for wall-clock time).
- [ ] `.hour`, `.minute`, `.second` return `integer`.
- [ ] `localtime + integer` is a type error.

### `instant` type

- [ ] `field X as instant` parses and type-checks.
- [ ] `instant("2026-04-13T14:30:00Z")` validates. Without `Z`: compile error.
- [ ] `instant - instant → duration`.
- [ ] `instant + duration → instant`, `instant - duration → instant`.
- [ ] `instant + period` is a type error (instant is timeline-only; use `+ duration`).
- [ ] `instant.year` is a compile error with a teachable message.
- [ ] `instant + integer` is a type error with a teachable message.
- [ ] MCP tools serialize as ISO 8601 UTC string.

### `duration` type

- [ ] `hours(72)`, `minutes(30)`, `seconds(3600)` produce duration values (timeline constructors only).
- [ ] `duration("PT72H")` parses as 72 hours. `duration("PT5H7M32S")` parses as composite.
- [ ] `duration("P5D")` is a compile error with a teachable message (days are calendar units; use `period("P5D")` or `days(5)` for calendar arithmetic).
- [ ] `duration("8 hours")` is a compile error with a teachable message (not ISO 8601).
- [ ] `days(n)`, `months(n)`, `years(n)`, `weeks(n)` produce `period` values, NOT `duration`.
- [ ] `duration + duration → duration`, `duration - duration → duration`.
- [ ] `duration * integer → duration`, `duration * number → duration` (scaling).
- [ ] `duration / integer → duration`, `duration / number → duration` (scaling).
- [ ] `duration * decimal` is a type error (lossy narrowing; use `number`).
- [ ] `duration / duration → number` (ratio).
- [ ] `duration * duration` is a type error.
- [ ] `integer * duration`, `number * duration` are type errors (duration must be left operand).
- [ ] `duration + period` and `duration - period` are type errors (cannot mix timeline and calendar units).
- [ ] `.totalHours`, `.totalMinutes`, `.totalSeconds` return `number`.
- [ ] `duration == duration`, `duration < duration` comparison works.
- [ ] If field type: `field X as duration default hours(0)` parses.
- [ ] Duration constructor syntax chosen (function call, postfix suffix, or both) and all examples in the proposal consistently reflect the chosen form.

### `period` type

- [ ] `field X as period` parses and type-checks.
- [ ] `days(n)`, `months(n)`, `years(n)`, `weeks(n)` produce `period` values (calendar constructors).
- [ ] `period("P1Y2M3D")` literal parses ISO 8601 duration string as a calendar period.
- [ ] `period("PT5H")` is a compile error (time-only ISO strings are `duration`, not `period`).
- [ ] `period + period → period`, `period - period → period`.
- [ ] `period == period` and `period != period` work.
- [ ] `period < period`, `period > period`, etc. are type errors (periods are not orderable; months have variable length).
- [ ] `period * integer` is a type error (period scaling is not supported; construct the value you need directly).
- [ ] `period + duration` and `period - duration` are type errors (cannot mix calendar and timeline units).
- [ ] `.years`, `.months`, `.weeks`, `.days` accessors return `integer`.
- [ ] If field type: `field X as period default days(0)` parses.
- [ ] MCP tools serialize as ISO 8601 period string.

### `timezone` type

- [ ] `field X as timezone` parses and type-checks.
- [ ] `timezone("America/Los_Angeles")` validates at compile time.
- [ ] `timezone("EST")` produces a warning.
- [ ] `timezone("Not/A/Thing")` is a compile error.
- [ ] `timezone == timezone` works. `timezone < timezone` is a type error.
- [ ] Event args typed `as timezone` validated at fire time.

### `zoneddatetime` type

- [ ] `field X as zoneddatetime` parses and type-checks.
- [ ] `zoneddatetime(instant, timezone)` constructor validates at compile time.
- [ ] `.instant → instant`, `.timezone → timezone` accessors work.
- [ ] `.date → localdate`, `.time → localtime` decomposition works.
- [ ] `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`, `.dayOfWeek` return `integer` resolved in the bound timezone.
- [ ] `zoneddatetime + hours(n)`, `+ minutes(n)`, `+ seconds(n)`, `+ duration`, `- duration` timeline arithmetic works.
- [ ] `zoneddatetime - zoneddatetime → duration` (instant subtraction).
- [ ] `zoneddatetime + days(n)`, `+ months(n)`, `+ years(n)`, `+ weeks(n)`, `+ period`, `- period` are type errors with a teachable message directing the user to decompose via `.date` for calendar arithmetic, then reconstruct.
- [ ] `zoneddatetime + integer` is a type error with a teachable message.
- [ ] `zoneddatetime == zoneddatetime` uses multi-dimensional comparison (primary: instant, tiebreaker: local datetime, final: timezone ID lexicographic). Two ZDTs at the same instant in different timezones are NOT equal.
- [ ] `zoneddatetime < zoneddatetime` compares by instant.
- [ ] `toLocalDate(zoneddatetime)` overload returns localdate in the bound timezone.
- [ ] `toLocalTime(zoneddatetime)` overload returns localtime in the bound timezone.
- [ ] `toInstant(localdate, localtime, zoneddatetime)` overload converts using the bound timezone.
- [ ] Nullable works. No default allowed (compile error if specified).
- [ ] MCP tools serialize as two-property JSON object.

### `localdatetime` type (if included)

- [ ] `field X as localdatetime` parses and type-checks.
- [ ] `localdatetime("2026-04-13T14:30:00")` validates (no timezone suffix).
- [ ] `.date → localdate`, `.time → localtime` decomposition works.
- [ ] All component accessors (`.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`) return `integer`.
- [ ] `localdatetime + days(n)`, `+ months(n)`, `+ years(n)`, `+ weeks(n)` calendar arithmetic works.
- [ ] `localdatetime + hours(n)`, `+ minutes(n)`, `+ seconds(n)` time arithmetic works.
- [ ] `localdatetime + period → localdatetime`, `localdatetime - period → localdatetime` work.
- [ ] `localdatetime - localdatetime → period`.
- [ ] `localdatetime + duration` and `localdatetime - duration` are type errors (convert to instant first for timeline arithmetic).
- [ ] `localdatetime + integer` is a type error.

### Conversion functions

- [ ] `toLocalDate(instant, timezone) → localdate` works.
- [ ] `toLocalTime(instant, timezone) → localtime` works.
- [ ] `toInstant(localdate, localtime, timezone) → instant` works.
- [ ] DST gap resolution: maps to post-gap instant.
- [ ] DST overlap resolution: maps to later instant.
- [ ] Invalid timezone in field value produces constraint violation at fire time.
- [ ] Conversion function results compose with existing type operations.
- [ ] `toLocalDate(zoneddatetime)` overload returns localdate in the bound timezone.
- [ ] `toLocalTime(zoneddatetime)` overload returns localtime in the bound timezone.
- [ ] `toInstant(localdate, localtime, zoneddatetime)` overload converts using the bound timezone.

### Tooling

- [ ] TextMate grammar highlights all temporal type keywords (including `period`).
- [ ] Language server offers temporal types in completions after `as` (including `period`).
- [ ] Language server offers temporal accessors after `.` on temporal fields (including `.years`, `.months`, `.weeks`, `.days` on `period`).
- [ ] Language server offers constructor/conversion functions in expression positions.
- [ ] Semantic tokens color temporal keywords as types and functions.
- [ ] All diagnostics (type errors, invalid literals, invalid timezones) display with teachable messages.

### Cross-type

- [ ] All entries in the "Not supported" tables produce type errors, not runtime exceptions.
- [ ] Cross-type comparison (`localdate == instant`, etc.) is a type error.
- [ ] No implicit mixing of `localdate`/`number`, `localdate`/`decimal`, `localdate`/`integer`, `instant`/`integer`, etc. Only explicit constructor functions are valid temporal arithmetic operands.
- [ ] `localdate + duration` is a type error (duration is timeline-only; use calendar constructors or `+ period`).
- [ ] `localtime + duration` is a type error (use `hours(n)`, `minutes(n)`, `seconds(n)`).
- [ ] `localdatetime + duration` is a type error (convert to instant first for timeline arithmetic).
- [ ] `instant + period` is a type error (instant is timeline-only; use `+ duration`).
- [ ] `zoneddatetime + period` is a type error (decompose via `.date` for calendar arithmetic).
- [ ] `zoneddatetime + days(n)`, `+ months(n)`, `+ years(n)`, `+ weeks(n)` are type errors (decompose via `.date`).
- [ ] `duration + period`, `period + duration` are type errors (cannot mix timeline and calendar).
- [ ] `period * integer`, `period / integer` are type errors (period scaling is not supported).
- [ ] `period < period` is a type error (periods are not orderable).

---

## Research Trail

| Document | Role |
|---|---|
| [temporal-type-strategy.md](temporal-type-strategy.md) | Unified strategy document — the synthesis that this proposal realizes. |
| [nodatime-precept-alignment.md](nodatime-precept-alignment.md) | NodaTime feasibility analysis. Type mapping, philosophy alignment, #26 impact. |
| [instant-zoneddatetime-reconsideration.md](instant-zoneddatetime-reconsideration.md) | Instant exclusion reversed. SLA use case validated. ZonedDateTime Fatal rating maintained. |
| [enterprise-timezone-analysis.md](enterprise-timezone-analysis.md) | Multi-timezone compliance gap. Conversion functions proposed. Determinism argument reframed. |
| [timezone-type-storability-analysis.md](timezone-type-storability-analysis.md) | `timezone` type accepted. ZonedDateTime downgraded from Fatal to Deferred. |
| [sample-temporal-pattern-catalog.md](sample-temporal-pattern-catalog.md) | Empirical evidence: 91 temporal markers across 15 samples, type frequency analysis. |
| [NodaTime type model survey](../references/nodatime-type-model.md) | Comprehensive inventory of NodaTime types, arithmetic, serialization, BCL correspondence. |
| [Precept Language Design](../../../docs/PreceptLanguageDesign.md) | Design principles (Principles #1, #2, #8, #12 cited throughout). |
| [Product Philosophy](../../../docs/philosophy.md) | Prevention guarantee, one-file completeness, determinism model. |
| Issue #26 body | Original `localdate` type proposal — superseded by this document's `localdate` section. |
| Issue #27 body | `decimal` type proposal — cross-interaction rules with temporal types. |
| Issue #29 body | `integer` type proposal — duration constructor function args are `integer`. |
| [zoneddatetime reconsideration](../../../.squad/decisions/inbox/frank-zoneddatetime-reconsideration.md) | Provenance erasure analysis. Upgrade from Deferred to Proposed. |
