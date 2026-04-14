# Proposal: Unified Temporal Type System — NodaTime-Aligned

**Author:** Frank (Lead/Architect, Language Designer)
**Date:** 2026-04-13
**Status:** Proposal — ready for owner review
**Supersedes:** Issue #26 (`date` type as standalone proposal)

---

## Summary

Add six temporal types to the Precept DSL — `date`, `time`, `instant`, `duration`, `timezone`, and `datetime` — backed by NodaTime as a runtime dependency. Include three timezone conversion functions (`toLocalDate`, `toLocalTime`, `toInstant`) and duration constructor functions (`days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`). Together, these types and functions give domain authors the vocabulary to express calendar constraints, SLA enforcement, multi-timezone compliance rules, and elapsed-time tracking — all within the governing contract, with no temporal logic delegated to the hosting layer. Every type earns its place by eliminating a specific ambiguity that would otherwise force authors into lossy encodings (`string`, `number`, `integer`) where the compiler cannot enforce domain intent.

---

## Design Philosophy

NodaTime exists because `System.DateTime` lets you be implicit about what your temporal data means. Precept exists because scattered service-layer code lets you be implicit about what your business rules mean. Both libraries are responses to the same failure mode: **implicit behavior creates bugs; explicit behavior creates predictability.**

This shared philosophy is the lens through which every type decision in this proposal is made:

| Precept applies it to... | NodaTime applies it to... | The shared principle |
|---|---|---|
| `date` over `string` | `LocalDate` over `DateTime` | Be explicit that this is a calendar date |
| `instant` over `number` | `Instant` over `DateTime` | Be explicit that this is a point on the timeline |
| `timezone` over `string` | `DateTimeZone` over `string` | Be explicit about the allowed values |
| `duration` over `integer` | `Duration` over `TimeSpan` | Be explicit about what units mean |
| `time` over `integer` | `LocalTime` over `int minutesSinceMidnight` | Be explicit that this is a time of day |
| `datetime` over `string` | `LocalDateTime` over `DateTime` | Be explicit about combined date+time without timezone |

Precept's design principles ground this directly:

- **Principle #1 (Deterministic, inspectable model):** NodaTime's type separation makes it structurally clear which operations are deterministic and which require external context. All types proposed here are deterministic by construction — `instant` comparison is nanosecond math, `date` arithmetic uses the fixed ISO calendar, and timezone conversion functions make the TZ database input explicit in the expression.
- **Principle #2 (English-ish but not English):** The DSL names — `date`, `time`, `instant`, `duration`, `timezone`, `datetime` — are English words that communicate exactly what the data is. `field FiledAt as instant` needs no comment.
- **Principle #8 (Sound, compile-time-first static analysis):** NodaTime's type separation enables the compiler to catch temporal misuse statically. `date + instant` is a type error. `instant.year` is a compile error. The types carry enough information for the compiler to reject nonsensical expressions without runtime evaluation.
- **Principle #12 (AI is a first-class consumer):** Named temporal types with precise semantics give AI consumers a vocabulary to reason about entity data and generate correct precepts. An AI that sees `field DueDate as date` knows the field supports calendar arithmetic — it does not need to infer this from naming conventions on a `string` field.

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
field DueDate as date default date("2026-06-01")
field GracePeriodDays as integer default 30

# Type-safe: DueDate + MealsTotal is a compile error
invariant DueDate + days(GracePeriodDays) >= date("2026-01-01") because "Within grace period"
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
    time("23:59:00"),
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

NodaTime is adopted as a runtime dependency for the entire temporal type system. The DSL author never sees NodaTime type names — `field DueDate as date` is the surface, `NodaTime.LocalDate` is the implementation, just as `field Amount as decimal` has `System.Decimal` behind it.

**Rationale:**

1. **Philosophy alignment.** NodaTime's core design philosophy is: *"Force you to think about what kind of data you really have."* Distinct types for distinct temporal concepts — `LocalDate` is not `Instant` is not `ZonedDateTime`. This is Precept's prevention guarantee applied to temporal data.

2. **Type separation enables compile-time safety.** NodaTime makes it structurally impossible to mix a calendar date with a global timestamp. Precept's type checker inherits this separation: `date + instant` is a type error by construction.

3. **Battle-tested arithmetic.** `LocalDate.PlusDays(int)`, `LocalDate.Plus(Period.FromMonths(n))`, month-end truncation (Jan 31 + 1 month = Feb 28), leap-year handling — all rigorously tested since 2012. Building the same guarantees on `System.DateOnly` would require Precept to own temporal arithmetic correctness, a domain Precept has no expertise in.

4. **Lower marginal cost.** NodaTime has higher up-front cost (dependency + serialization) but significantly lower marginal cost for each subsequent temporal type. `System.DateOnly` is cheaper for `date` alone but increasingly expensive as temporal features accumulate — `TimeOnly` lacks integration with date arithmetic, and `DateTime` reintroduces the ambiguity problems NodaTime was designed to solve.

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

### `date`

**What it makes explicit:** This is a calendar date — not a timestamp, not a string that looks like a date. Day-granularity arithmetic is meaningful. "2026-03-15" means the same calendar day everywhere.

**Backing type:** `NodaTime.LocalDate`

**Declaration:**

```precept
field DueDate as date default date("2026-06-01")
field FilingDate as date nullable
field ContractEnd as date default date("2099-12-31")
```

**Literal / Constructor syntax:** `date("<YYYY-MM-DD>")` — ISO 8601, always. No custom formats. `date("2026-03-15")` is valid. `date("03/15/2026")` is a compile error with a teachable message.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `date + days(n)` | `date` | Add N calendar days. `LocalDate.PlusDays(int)`. |
| `date - days(n)` | `date` | Subtract N calendar days. |
| `date + months(n)` | `date` | Add N months. Truncates at month end (Jan 31 + 1 mo = Feb 28). |
| `date + years(n)` | `date` | Add N years. Handles leap years (Feb 29 + 1 yr = Feb 28). |
| `date + weeks(n)` | `date` | Add N weeks (= 7N days). |
| `date - date` | `duration` | Elapsed days between dates. The result is a duration carrying day-unit semantics. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering. ISO calendar only. |

| **Not supported** | **Why** |
|---|---|
| `date + date` | Adding two dates is meaningless — no temporal concept this could represent. |
| `date + integer` | Bare integers don't carry unit semantics — days? months? weeks? Use `date + days(n)`. |
| `date + decimal` | Fractional days are meaningless at day granularity. Type error. |
| `date + number` | `number` is floating-point; temporal arithmetic requires explicit duration constructors. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.year` | `integer` | Calendar year |
| `.month` | `integer` | Month (1–12) |
| `.day` | `integer` | Day of month (1–31) |
| `.dayOfWeek` | `integer` | ISO day of week (Monday=1, Sunday=7) |

**Constraints:** `nullable`, `default date(...)`. Constraints `nonnegative`, `positive`, `min`, `max`, `maxplaces`, `minlength`, `maxlength` are not valid on `date` (compile error).

**Serialization:** ISO 8601 string in MCP JSON payloads: `"2026-03-15"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `field X as date default "2026-01-01"` | Date defaults require the date constructor: `default date("2026-01-01")`. Bare strings are not dates. |
| `date("2026-02-30")` | Invalid date: February 30 does not exist. Use a valid calendar date in ISO 8601 format (YYYY-MM-DD). |
| `date("01/15/2026")` | Invalid date format: expected ISO 8601 (YYYY-MM-DD), got '01/15/2026'. Use `date("2026-01-15")`. |
| `DueDate + FilingDate` | Cannot add two dates. Did you mean `DueDate - FilingDate` (duration) or `DueDate + days(n)` (offset)? |
| `DueDate + 2` | Cannot add an integer to a date. Temporal arithmetic requires explicit duration constructors. Use `DueDate + days(2)` to add 2 calendar days. |
| `DueDate + 2.5` | Cannot add a number to a date. Temporal arithmetic requires explicit duration constructors. Use `DueDate + days(2)` or `DueDate + months(n)`. |

---

### `time`

**What it makes explicit:** This is a time of day — not a duration, not a timestamp, not an integer encoding minutes-since-midnight.

**Backing type:** `NodaTime.LocalTime`

**Declaration:**

```precept
field AppointmentTime as time default time("09:00:00")
field CheckInTime as time nullable
```

**Literal / Constructor syntax:** `time("<HH:mm:ss>")` — ISO 8601 extended time. `time("14:30:00")` is valid. Seconds may be omitted: `time("14:30")` is valid (implies `:00`).

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `time + hours(n)` | `time` | Wraps at midnight. `LocalTime.PlusHours(long)`. |
| `time + minutes(n)` | `time` | Wraps at midnight. `LocalTime.PlusMinutes(long)`. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering within a day. |

| **Not supported** | **Why** |
|---|---|
| `time - time` | Ambiguous: does 23:00 - 01:00 = 22 hours or -22 hours? Defer until use case is clear. |
| `time + integer` | What unit? Use `hours(n)` or `minutes(n)` explicitly. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.hour` | `integer` | Hour (0–23) |
| `.minute` | `integer` | Minute (0–59) |
| `.second` | `integer` | Second (0–59) |

**Constraints:** `nullable`, `default time(...)`.

**Serialization:** ISO 8601 time string: `"14:30:00"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `time("25:00:00")` | Invalid time: hour must be 0–23, got 25. |
| `AppointmentTime + 30` | Cannot add an integer to a time. Use `time + minutes(30)` or `time + hours(1)` to specify the unit. |

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

**Cons:** The same type-safety gap that justified `date` over `string` applies: `integer` fields lose domain semantics, and `EstimatedHours + InvoiceTotal` compiles.

**Recommendation:** Option A — `duration` as a declared field type. The corpus evidence (9 fields across 6 samples) and the type-safety parity argument with `date` both support it. The "zero" duration (`hours(0)`) is a natural default.

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

**Why `timezone` over `string`:** The `date`-over-`string` argument applies with equal force. `field IncidentTimezone as string` means the compiler can't reject `"Not/A/Timezone"` or `"California"`. The `timezone` type validates at compile time (literals) and fire time (event args), closing the type-safety gap that the `date` type closes for calendar dates.

---

### `datetime`

**What it makes explicit:** This is a date and time together — not a point on the global timeline, not two separate fields pretending to be coupled. No timezone.

**Backing type:** `NodaTime.LocalDateTime`

**Declaration:**

```precept
field DetectedAt as datetime nullable
field ScheduledFor as datetime default datetime("2026-04-13T09:00:00")
```

**Literal / Constructor syntax:** `datetime("<YYYY-MM-DD>T<HH:mm:ss>")` — ISO 8601 combined. No timezone suffix (that would make it a `ZonedDateTime`). `datetime("2026-04-13T14:30:00")` is valid.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `datetime + hours(n)` | `datetime` | Offset by hours. |
| `datetime + minutes(n)` | `datetime` | Offset by minutes. |
| `datetime + days(n)` | `datetime` | Add N calendar days. Same as date + days(n). |
| `datetime + months(n)` | `datetime` | Calendar arithmetic via Period. |
| `datetime + years(n)` | `datetime` | Calendar arithmetic via Period. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering within the same calendar. |

| **Not supported** | **Why** |
|---|---|
| `datetime - datetime` | Ambiguous without timezone context. If both datetimes are in the same implied local context, is the subtraction calendar-based or timeline-based? Defer pending use case clarity. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.date` | `date` | Date component extraction. |
| `.time` | `time` | Time component extraction. |
| `.year` | `integer` | Calendar year. |
| `.month` | `integer` | Month (1–12). |
| `.day` | `integer` | Day of month (1–31). |
| `.hour` | `integer` | Hour (0–23). |
| `.minute` | `integer` | Minute (0–59). |
| `.second` | `integer` | Second (0–59). |

**Constraints:** `nullable`, `default datetime(...)`.

**Serialization:** ISO 8601 string without timezone: `"2026-04-13T14:30:00"`.

#### Option: `datetime` inclusion vs. deferral

##### Option A: Include `datetime` in the initial proposal

**Pros:** Completes the local type vocabulary. Security-incident sample (6 NIST compliance timestamps) demonstrates real need. Enables `toInstant(date, time, timezone)` alternative as `datetime + timezone → instant` conversion.

**Cons:** Only 1 of 15 samples requires it. Most combined date+time use cases can be handled with `date` + `time` as separate fields. Higher implementation cost for thin corpus evidence.

##### Option B: Defer `datetime` to a follow-up proposal

**Pros:** Smaller initial surface. Focus resources on the high-frequency types (`date`, `instant`, `duration`, `time`, `timezone`). Ship `datetime` when enterprise adoption demonstrates that separate `date` + `time` fields create friction.

**Cons:** Security-incident and audit-trail domains remain underserved. Authors who need sub-day local timestamps encode them as strings.

**Recommendation:** Open — needs design discussion. The corpus evidence is thin (6 fields in 1 sample), but the use case is real (NIST incident response). Including it completes the type vocabulary cleanly; deferring it reduces the initial surface.

---

## Conversion Functions

### The timezone bridge

Conversion functions bridge the timeline domain (instants, durations) and the calendar domain (dates, times). They make the timezone dependency explicit — visible in the expression, typed at the call boundary, inspectable by `precept_inspect`.

| Function | Signature | Semantics |
|---|---|---|
| `toLocalDate` | `(instant, timezone) → date` | Extract the local calendar date for an instant in a timezone. "What date is it at this moment in this timezone?" |
| `toLocalTime` | `(instant, timezone) → time` | Extract the local time-of-day for an instant in a timezone. "What time is it at this moment in this timezone?" |
| `toInstant` | `(date, time, timezone) → instant` | Convert local date + time-of-day + timezone → UTC instant. "What UTC moment corresponds to this local time in this timezone?" |

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
    time("23:59:00"),
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

**Cons:** "Lenient" means the resolution silently adjusts invalid local times. Authors may not realize `time("02:30:00")` at a spring-forward boundary was adjusted to `time("03:00:00")`.

##### Option B: Strict resolver — reject ambiguous conversions

Gaps and overlaps produce constraint violations. The author must handle DST boundaries explicitly.

**Pros:** No silent adjustment. The author is forced to account for DST.

**Cons:** Extremely burdensome for the common case. Most timezone conversions will never hit a DST boundary. Requiring DST handling in every conversion penalizes the 99% case for the 1% edge case.

**Recommendation:** Option A — lenient resolver. The determinism guarantee is preserved (same inputs = same output), and the 99% case is unaffected. The resolution strategy should be documented in `PreceptLanguageDesign.md` and visible in `precept_inspect` output when a DST adjustment occurs.

### Composability

Conversion functions produce and consume existing types — they are bridges, not new types:

- `toLocalDate` returns a `date` — all date operations (comparison, `.year`, `+ days(n)`, `+ months(n)`) work.
- `toLocalTime` returns a `time` — all time operations (comparison, `.hour`) work.
- `toInstant` returns an `instant` — all instant comparison and duration arithmetic work.

### Duration constructor functions

Duration constructors are the **required interface** for all temporal arithmetic. Bare integers are never valid temporal offsets — every arithmetic operation on a temporal type requires an explicit duration constructor that names the unit.

| Function | Signature | Semantics |
|---|---|---|
| `days(n)` | `(integer) → duration` | Calendar period of N days. Used with `date +`, `datetime +`. Internally `Period.FromDays`. |
| `months(n)` | `(integer) → duration` | Calendar period of N months. Truncates at month end (Jan 31 + 1 mo = Feb 28). Internally `Period.FromMonths`. |
| `years(n)` | `(integer) → duration` | Calendar period of N years. Handles leap years. Internally `Period.FromYears`. |
| `weeks(n)` | `(integer) → duration` | Calendar period of N weeks (= 7N days). Internally `Period.FromWeeks`. |
| `hours(n)` | `(integer) → duration` | Duration of N hours. Used with `time +`, `instant +`, `datetime +`. Internally `Duration.FromHours`. |
| `minutes(n)` | `(integer) → duration` | Duration of N minutes. Internally `Duration.FromMinutes`. |
| `seconds(n)` | `(integer) → duration` | Duration of N seconds. Internally `Duration.FromSeconds`. |

Internally, calendar-unit constructors (`days`, `months`, `years`, `weeks`) produce `NodaTime.Period` values, while timeline constructors (`hours`, `minutes`, `seconds`) produce `NodaTime.Duration` values. The DSL author does not see this distinction — all seven are "duration constructors" at the language surface. `Period` is not exposed as a DSL type.

---

## Semantic Rules

### Type-interaction matrix

The following matrix defines what operations are valid between temporal types. Anything not listed is a type error.

| Left operand | Operator | Right operand | Result | Notes |
|---|---|---|---|---|
| `date` | `+` | `days(n)` | `date` | Add N calendar days |
| `date` | `-` | `days(n)` | `date` | Subtract N calendar days |
| `date` | `+` | `months(n)` | `date` | Calendar arithmetic; truncates at month end |
| `date` | `+` | `years(n)` | `date` | Calendar arithmetic; handles leap years |
| `date` | `+` | `weeks(n)` | `date` | = 7N days |
| `date` | `-` | `date` | `duration` | Elapsed days between dates |
| `instant` | `-` | `instant` | `duration` | Elapsed time between two points |
| `instant` | `+` | `duration` | `instant` | Point offset forward |
| `instant` | `-` | `duration` | `instant` | Point offset backward |
| `duration` | `+` | `duration` | `duration` | Combined elapsed time |
| `duration` | `-` | `duration` | `duration` | Difference |
| `duration` | `*` | `integer` or `number` | `duration` | Scaling (e.g., `SlaWindow * ShiftCount`) |
| `duration` | `/` | `integer` or `number` | `duration` | Scaling (e.g., `SlaWindow / 2`) |
| `duration` | `/` | `duration` | `number` | Ratio (e.g., how many shifts fit) |
| `time` | `+` | `hours(n)` | `time` | Wraps at midnight |
| `time` | `+` | `minutes(n)` | `time` | Wraps at midnight |
| `datetime` | `+` | `days(n)` | `datetime` | Add N days |
| `datetime` | `+` | `months(n)` | `datetime` | Calendar arithmetic |
| `datetime` | `+` | `years(n)` | `datetime` | Calendar arithmetic |
| `datetime` | `+` | `hours(n)` | `datetime` | Time arithmetic |
| `datetime` | `+` | `minutes(n)` | `datetime` | Time arithmetic |

### Comparison rules

All temporal types support `==`, `!=`. `date`, `time`, `instant`, `duration`, `datetime` support `<`, `>`, `<=`, `>=`. `timezone` supports only `==`, `!=` — no ordering.

Cross-type comparison is always a type error:
- `date == instant` → type error
- `time == duration` → type error
- `date == datetime` → type error

### Cross-type arithmetic: what's NOT allowed (and why)

| Expression | Why it's a type error |
|---|---|
| `date + date` | Adding two dates is meaningless. |
| `date + instant` | Different temporal domains (calendar vs. timeline). |
| `date + integer` | Bare integers don't carry unit semantics. Use `date + days(n)` for day offsets. |
| `date + decimal` | Fractional days are meaningless at day granularity. |
| `date + number` | `number` is floating-point; temporal arithmetic requires explicit duration constructors. |
| `instant + integer` | Ambiguous unit. Use `instant + hours(n)` or `instant + seconds(n)`. |
| `instant + months(n)` | Months are calendar units with no fixed duration. Convert to date, add, convert back. |
| `instant.year` | Requires a timezone. Use `toLocalDate(instant, timezone).year`. |
| `time - time` | Ambiguous sign (see `time` section). |
| `duration * duration` | Dimensionally meaningless. |
| `integer * duration` / `number * duration` | Use `duration * integer` or `duration * number` — duration is always the left operand. |
| `duration * decimal` / `duration / decimal` | `decimal → double` is lossy. Use `number` for scaling operands. |
| `integer / duration` / `number / duration` | Dimensionally meaningless (what is "5 / 3 hours"?). |
| `timezone + anything` | Timezones are metadata, not temporal values. |

### Nullable behavior

All temporal types support `nullable`. Nullable temporal fields follow existing null semantics:
- Comparison with null follows existing null propagation rules.
- Accessors on a nullable field require null-check narrowing (same as collection accessors).

### Default behavior

| Type | Default value | Notes |
|---|---|---|
| `date` | `default date("...")` | Author specifies the date. |
| `time` | `default time("...")` | Author specifies the time. |
| `instant` | `default instant("...")` | Author specifies the UTC instant. |
| `duration` | `default hours(0)` (or `minutes(0)`, etc.) | Zero duration is natural. |
| `timezone` | No default | No universally sensible default timezone. |
| `datetime` | `default datetime("...")` | Author specifies the datetime. |

#### Option: `nullable` + `default` for temporal fields

##### Option A: Prohibit `nullable` + `default` on temporal fields

A field is either nullable (absent until populated) or has a default (always present). This was the original #26 position.

**Pros:** Clear semantics — a field is either "unknown" or "has a known starting value." Prevents the ambiguity of "is null the default, or is the default the default?"

**Cons:** Some domain patterns naturally want both: "this field starts as null (unknown), but once populated, reverts to a default on reset." This feels like arbitrary restriction.

##### Option B: Allow `nullable` + `default` — null means "not yet provided," default is the initial value

When both are specified, the field starts at the default value but can be set to null explicitly. Null means "cleared/unknown."

**Pros:** Maximum flexibility. No arbitrary restriction.

**Cons:** `default date("2026-01-01") nullable` — if the field starts at the default, when is it ever null? The semantics are confusing. `null` becomes "explicitly cleared" rather than "never set."

##### Option C: Allow `nullable` + `default` — default is the initial value, null is a valid assignment target

Same as Option B but with clearer documentation: the field initializes to the default and `null` is a valid value that can be assigned via events.

**Recommendation:** Open — this is George's Challenge #3 from the original #26 review. The question is orthogonal to temporal types and applies to all types. Defer to a cross-cutting design decision rather than deciding it here.

---

## Locked Design Decisions

Each locked decision follows the 4-point rationale format required by CONTRIBUTING.md.

### 1. NodaTime as the backing library for all temporal types

- **Why:** NodaTime's type model matches Precept's philosophy (explicit over implicit), provides battle-tested arithmetic, and creates a coherent path for the full temporal vocabulary. Building temporal arithmetic on BCL types would require Precept to own correctness in a domain it has no expertise in.
- **Alternatives rejected:** `System.DateOnly` / `TimeOnly` — cheaper for `date` alone but creates increasing debt for `time`, `datetime`, and cross-type conversions. `System.DateTime` — conflates concepts; the exact problem NodaTime was created to solve. No backing library (custom implementation) — unmaintainable temporal arithmetic with no precedent or test coverage.
- **Precedent:** NRules inherits `System.Decimal` from .NET. Precept inherits NodaTime's temporal model. The pattern is: use the best domain-specific library, expose only DSL-appropriate operations.
- **Tradeoff accepted:** Additional NuGet dependency (~1.1 MB, well-maintained).

### 2. Day granularity for `date` — no time-of-day component

- **Why:** "2026-03-15" means the same calendar day everywhere. Adding time-of-day introduces timezone dependency. Precept's determinism guarantee requires identical results regardless of evaluation location.
- **Alternatives rejected:** `datetime` as the only calendar type — forces timezone reasoning on every author for rules operating at day granularity. Optional timezone — creates two semantics in one type.
- **Precedent:** SQL's `DATE`, FEEL's `date()`, Cedar's `datetime` — all timezone-naive at the date level.
- **Tradeoff accepted:** Authors needing time-of-day use `time` (separate type) or `datetime`.

### 3. ISO 8601 as the sole format — no custom format strings

- **Why:** A single canonical format eliminates parsing ambiguity. Is `01/02/2026` January 2nd or February 1st? ISO 8601 is unambiguous by construction. Same literal, same meaning, always.
- **Alternatives rejected:** Configurable format strings — adds parsing complexity and creates precepts interpretable only with the format specifier. Auto-detection — heuristic and non-deterministic.
- **Precedent:** Cedar (RFC 3339), FEEL (ISO 8601), SQL DATE (ISO format).
- **Tradeoff accepted:** Authors from `MM/DD/YYYY` regions must adapt. One-time learning cost.

### 4. Constructor form for all temporal literals

- **Why:** `date("2026-03-15")`, not bare `"2026-03-15"`. A bare string is type-ambiguous. Context-dependent type resolution violates Principle #9 (tooling drives syntax) — IntelliSense and semantic tokens can't determine type without full expression analysis. The constructor form makes type intent visible at the lexical level.
- **Alternatives rejected:** Bare strings with inferred type — ambiguous. Sigil prefix (`#2026-03-15`) — no precedent. Method-style `Date.parse()` — implies namespaces Precept doesn't have.
- **Precedent:** FEEL `date(...)`, Cedar `datetime(...)`, Precept's existing `choice(...)` convention.
- **Tradeoff accepted:** Slightly more verbose than a bare literal.

### 5. No timezone on `date`, `time`, or `datetime`

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

- **Why:** Encoding timezone identifiers as `string` loses type safety — `"California"`, `"EST"`, `"Pacific Standard Time"` all compile fine as strings. The same argument that justified `date` over `string` applies with equal force: the type communicates intent, and the compiler enforces validity.
- **Alternatives rejected:** `string` with naming conventions — the compiler can't enforce convention. `choice` with all IANA identifiers — ~600 choices is unergonomic and requires manual updates when the IANA database changes.
- **Precedent:** NodaTime's `DateTimeZone` is a first-class type with validation. The `date`-over-`string` parallel in Precept is already established.
- **Tradeoff accepted:** New type in the type system. Minimal operational cost — `timezone` is an equality-only validated identifier, closer to `choice` than to `date` in complexity.

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

### 10. Temporal arithmetic requires explicit duration constructors (not bare integers)

- **Why:** `date + 2` is implicit — what does `2` mean? Days? Months? Weeks? This is exactly the ambiguity NodaTime was designed to prevent. In NodaTime, you write `date.PlusDays(2)` or `date + Period.FromDays(2)` — never `date + 2`. Precept applies the same principle: `date + days(2)`, `date + months(1)`, `time + hours(3)`. The duration constructor names the unit; the compiler enforces it. Shane's directive: *“date + integer is implicit.”*
- **Alternatives rejected:** `date + integer` meaning “add N days” — the previous locked decision in this proposal. While NodaTime’s `PlusDays(int)` takes an `int`, the DSL author sees `date + 2` without the `PlusDays` context. The bare integer silently assumes "days" — a unit choice the author never made explicit. `date + number` — allows fractional day offsets that have no calendar meaning. `date + decimal` — same problem.
- **Precedent:** NodaTime’s API requires named methods (`PlusDays`, `PlusMonths`, `Plus(Period)`) for all temporal arithmetic — the type alone doesn’t carry unit information. FEEL uses `duration("P2D")` for explicit day offsets, not bare integers. Shane’s governing directive aligns Precept with this precedent.
- **Tradeoff accepted:** More verbose than `date + 2`. The verbosity is the point — it forces the author to name the unit, making the intent unambiguous to readers, auditors, and AI consumers.

### 11. `date - date → duration` (not → integer or → number)

- **Why:** The result of subtracting two dates is a temporal quantity — it should carry unit semantics, not collapse to a bare integer. `date - date → integer` produces a number that has lost its meaning: is `30` thirty days? Thirty somethings? Returning a `duration` preserves the unit information and enables further temporal arithmetic (`elapsed > days(30)`). This follows the same explicitness principle as Decision #10 — the type system carries meaning that bare numbers lose.
- **Alternatives rejected:** `→ integer` — the previous locked decision. Loses unit semantics; the caller must *remember* that the integer means days. `→ number` — the original #26 proposal. Floating-point is unnecessary and misleading for a value that is structurally a day count.
- **Precedent:** NodaTime’s `Period.Between(d1, d2)` returns a `Period`, not an `int`. The period carries structural unit information. Shane’s directive establishes that implicit results are as problematic as implicit operands.
- **Tradeoff accepted:** `duration` result requires `.totalHours` or comparison with `days(n)` to extract a numeric value. The indirection is intentional — it forces the consumer to acknowledge the unit.

---

## George's Challenges — Resolution

George's design review of the original #26 proposal raised four challenges. The unified temporal type system, backed by NodaTime, resolves all four:

### Challenge 1: `DueDate + MealsTotal` compiles because `date + number → date`

**Resolution:** No form of `date + <non-duration>` is defined. The only valid temporal arithmetic uses explicit duration constructors: `date + days(n)`, `date + months(n)`, etc. `DueDate + MealsTotal` is a **type error** regardless of whether `MealsTotal` is `decimal`, `number`, or `integer` — none of these are duration values. This is strictly stronger than the previous `date + integer` rule, which would have allowed `DueDate + MealsTotal` if `MealsTotal` happened to be `integer`. The explicit-duration requirement eliminates the cross-domain error structurally.

### Challenge 2: `date + 2.5` should reject fractional offsets

**Resolution:** `date + 2.5` is a type error — and now `date + 2` is *also* a type error. No bare numeric value (integer, number, or decimal) is a valid operand for temporal arithmetic. Only explicit duration constructors are accepted: `date + days(2)`, `date + months(1)`, etc. The fractional offset `2.5` is rejected not by a granularity check but by the fundamental rule: temporal arithmetic requires duration constructors that name the unit. This is strictly stronger than the previous resolution — it eliminates *all* implicit temporal arithmetic, not just fractional offsets.

### Challenge 3: `nullable + default` prohibition seems arbitrary

**Resolution:** This is presented as an open option in this proposal (see "Option: `nullable` + `default` for temporal fields" above). The question is orthogonal to temporal types and applies to all types — it should be resolved as a cross-cutting design decision. The NodaTime backing type has no opinion here.

### Challenge 4: `date - date → number` should be `→ integer`

**Resolution:** Now `date - date → duration` — even more explicit than `→ integer`. The result is a duration value that carries unit semantics (days), not a bare number or integer that has lost its meaning. The consumer can compare it directly with `days(n)` expressions (`elapsed > days(30)`) or extract a numeric value via `.totalHours`. This aligns with Shane’s directive that implicit results are as problematic as implicit operands.

---

## Dependencies and Related Issues

| Issue | Relationship |
|---|---|
| #25 (choice type) | Currency codes as `choice("USD", "EUR", ...)` complement `decimal` for money; `choice` for state/region complements `timezone` for jurisdiction lookups. |
| #26 (date type) | **Superseded by this proposal.** The `date` section here incorporates all of #26’s design plus NodaTime backing, explicit-duration-only arithmetic, and month/year arithmetic. |
| #27 (decimal type) | Complementary numeric type. `decimal` and temporal types do not interact arithmetically (`decimal + date` is a type error). `duration.totalHours` returns `number`, not `decimal`. |
| #29 (integer type) | Duration constructor function args are `integer`. The `integer` type is a dependency for correct temporal arithmetic. |
| #16 (built-in functions) | `round()` from #27; `toLocalDate`, `toLocalTime`, `toInstant`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds` from this proposal. |
| #13 (field-level constraints) | `nullable`, `default`, `nonnegative` — constraint-zone architecture that temporal fields use. |

---

## Explicit Exclusions / Out of Scope

Each exclusion includes rationale — items are excluded for reasons, not convenience.

### `ZonedDateTime` as a field type — Excluded

A full NodaTime-style `ZonedDateTime` with component accessors (`.year`, `.month`, `.hour`), arithmetic (`zdt + days(1)`), and ambiguity resolution is permanently excluded. Component accessors depend on the TZ database — `instant.InZone(tz).Hour` produces different results depending on DST rules, which change with TZ database updates. This creates non-deterministic expressions that violate Precept's core guarantee.

**The minimal `zoneddatetime` composite** (instant + timezone, `.instant`/`.timezone` accessors only, comparison by instant, no component accessors, no arithmetic) is safe but deferred — see below.

### Minimal `zoneddatetime` composite — Deferred

A composite type bundling `instant + timezone` as a single field with co-assignment enforcement. This is **safe** (the assessment in `timezone-type-storability-analysis.md` downgraded it from Fatal to Deferred) but not yet justified by the cost/benefit analysis. The type system cost (constructor syntax, composite accessors, null/default semantics, collection behavior) is significant, and the primary benefit over two separate fields is co-assignment enforcement. Ship when enterprise adoption demonstrates the co-assignment failure mode.

### `OffsetDateTime` — Excluded

Carries a UTC offset without timezone rules. Same determinism concerns as `ZonedDateTime`, weaker than full timezone — offset without rules is misleading. Not useful in entity modeling.

### `AnnualDate` — Deferred

Recurring month-day combination (birthday, anniversary, annual deadline). Real demand in HR and insurance, but low corpus frequency. Evaluate after core temporal types are in use to measure demand.

### `YearMonth` — Deferred

Year-month pair (billing period, fiscal month, credit card expiry). Real demand in SaaS billing, but low priority given current evidence.

### `DateInterval` / `daterange` — Deferred

Range between two calendar dates. Two `date` fields with an invariant (`StartDate <= EndDate`) cover the use case with existing machinery. The marginal benefit of a composite type is co-assignment enforcement and `contains(date)` operations.

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

- Add `date`, `time`, `instant`, `duration`, `timezone`, `datetime` as type keywords.
- Add `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds` as function keywords.
- Add `toLocalDate`, `toLocalTime`, `toInstant` as function keywords.
- Parse constructor forms: `date("...")`, `time("...")`, `instant("...")`, `timezone("...")`, `datetime("...")`.
- Parse function calls: `days(expr)`, `months(expr)`, `hours(expr)`, `toLocalDate(expr, expr)`, `toInstant(expr, expr, expr)`.
- Parse accessors: `.year`, `.month`, `.day`, `.dayOfWeek`, `.hour`, `.minute`, `.second`, `.date`, `.time`, `.totalHours`, `.totalMinutes`, `.totalSeconds`.

### Type Checker

- New type entries for `date`, `time`, `instant`, `duration`, `timezone`, `datetime`.
- Operator resolution: the full cross-type interaction matrix above.
- Accessor resolution: per-type accessor tables.
- Constructor validation: ISO 8601 format check for literals (compile-time).
- Constraint validation: which constraints apply to which temporal types.
- Cross-type arithmetic rejection: `date + instant`, `date + integer`, `instant + months(n)`, etc.
- `instant` component accessor rejection (new diagnostic).
- `timezone` ordering rejection (new diagnostic).
- Duration constructor argument validation: `hours(n)` requires `integer` argument.

### Expression Evaluator

- `date` arithmetic via `LocalDate.PlusDays`, `LocalDate.Plus(Period)`.
- `time` arithmetic via `LocalTime.PlusHours`, `LocalTime.PlusMinutes`.
- `instant` arithmetic via `Instant.Plus(Duration)`, `Instant.Minus(Duration)`.
- `duration` arithmetic via `Duration.Plus`, `Duration.Minus`.
- `datetime` arithmetic via `LocalDateTime` methods.
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

- Add `date`, `time`, `instant`, `duration`, `timezone`, `datetime` to `typeKeywords` alternation.
- Add `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds` to function keyword patterns.
- Add `toLocalDate`, `toLocalTime`, `toInstant` to function keyword patterns.
- Temporal accessors (`.year`, `.totalHours`, etc.) handled by existing member-access pattern.

### Language Server

- **Completions:** Temporal types offered after `as` in field/event-arg declarations. Constructor functions offered in expression positions. Accessors offered after `.` on temporal-typed fields. Conversion functions offered in expression positions.
- **Hover:** Temporal field hover shows type, constraints, and backing NodaTime type. Constructor hover shows format requirements. Conversion function hover shows signature and DST resolution behavior.
- **Diagnostics:** All type errors from the type checker. Compile-time invalid temporal literals. Compile-time invalid timezone identifiers.
- **Semantic tokens:** Temporal type keywords colored as types (automatic via `[TokenCategory(Type)]`). Constructor/function keywords colored as functions.

### MCP Tools

- `precept_language`: Include `date`, `time`, `instant`, `duration`, `timezone`, `datetime` in type keywords. Include all constructor and conversion functions. TZ database version in environment info.
- `precept_compile`: Temporal field DTOs include type, constraints, and assessed properties. New diagnostics for temporal type errors.
- `precept_fire` / `precept_inspect` / `precept_update`: Temporal values serialized as ISO 8601 strings. Timezone values as IANA identifiers. Duration values as ISO 8601 duration strings.

### Samples

- Update existing samples with `FUTURE(date)` markers to use `date` fields.
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

### `date` type

- [ ] `field X as date` parses and type-checks.
- [ ] `date("2026-03-15")` literal validates ISO 8601 at compile time.
- [ ] `date("2026-02-30")` produces compile-time error.
- [ ] `date + days(n) → date` works; `date + integer`, `date + number`, and `date + decimal` are type errors.
- [ ] `date + months(n)`, `date + years(n)`, `date + weeks(n)` work with correct truncation.
- [ ] `date - date → duration` (not `integer` or `number`).
- [ ] `.year`, `.month`, `.day`, `.dayOfWeek` return `integer`.
- [ ] Nullable and default work. Constraints `nonnegative`, `min`, `max`, etc. are compile errors.
- [ ] MCP tools serialize as ISO 8601 string.

### `time` type

- [ ] `field X as time` parses and type-checks.
- [ ] `time("14:30:00")` and `time("14:30")` validate at compile time.
- [ ] `time + hours(n)` and `time + minutes(n)` wrap at midnight correctly.
- [ ] `.hour`, `.minute`, `.second` return `integer`.
- [ ] `time + integer` is a type error.

### `instant` type

- [ ] `field X as instant` parses and type-checks.
- [ ] `instant("2026-04-13T14:30:00Z")` validates. Without `Z`: compile error.
- [ ] `instant - instant → duration`.
- [ ] `instant + duration → instant`, `instant - duration → instant`.
- [ ] `instant.year` is a compile error with a teachable message.
- [ ] `instant + integer` is a type error with a teachable message.
- [ ] MCP tools serialize as ISO 8601 UTC string.

### `duration` type

- [ ] `days(7)`, `hours(72)`, `minutes(30)`, `seconds(3600)` produce duration values.
- [ ] `duration + duration → duration`, `duration - duration → duration`.
- [ ] `duration * integer → duration`, `duration * number → duration` (scaling).
- [ ] `duration / integer → duration`, `duration / number → duration` (scaling).
- [ ] `duration * decimal` is a type error (lossy narrowing; use `number`).
- [ ] `duration / duration → number` (ratio).
- [ ] `duration * duration` is a type error.
- [ ] `integer * duration`, `number * duration` are type errors (duration must be left operand).
- [ ] `.totalHours`, `.totalMinutes`, `.totalSeconds` return `number`.
- [ ] `duration == duration`, `duration < duration` comparison works.
- [ ] If field type: `field X as duration default hours(0)` parses.

### `timezone` type

- [ ] `field X as timezone` parses and type-checks.
- [ ] `timezone("America/Los_Angeles")` validates at compile time.
- [ ] `timezone("EST")` produces a warning.
- [ ] `timezone("Not/A/Thing")` is a compile error.
- [ ] `timezone == timezone` works. `timezone < timezone` is a type error.
- [ ] Event args typed `as timezone` validated at fire time.

### `datetime` type (if included)

- [ ] `field X as datetime` parses and type-checks.
- [ ] `datetime("2026-04-13T14:30:00")` validates (no timezone suffix).
- [ ] `.date → date`, `.time → time` decomposition works.
- [ ] All component accessors (`.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`) return `integer`.

### Conversion functions

- [ ] `toLocalDate(instant, timezone) → date` works.
- [ ] `toLocalTime(instant, timezone) → time` works.
- [ ] `toInstant(date, time, timezone) → instant` works.
- [ ] DST gap resolution: maps to post-gap instant.
- [ ] DST overlap resolution: maps to later instant.
- [ ] Invalid timezone in field value produces constraint violation at fire time.
- [ ] Conversion function results compose with existing type operations.

### Tooling

- [ ] TextMate grammar highlights all temporal type keywords.
- [ ] Language server offers temporal types in completions after `as`.
- [ ] Language server offers temporal accessors after `.` on temporal fields.
- [ ] Language server offers constructor/conversion functions in expression positions.
- [ ] Semantic tokens color temporal keywords as types and functions.
- [ ] All diagnostics (type errors, invalid literals, invalid timezones) display with teachable messages.

### Cross-type

- [ ] All entries in the "Not supported" tables produce type errors, not runtime exceptions.
- [ ] Cross-type comparison (`date == instant`, etc.) is a type error.
- [ ] No implicit mixing of `date`/`number`, `date`/`decimal`, `date`/`integer`, `instant`/`integer`, etc. Only explicit duration constructors are valid temporal arithmetic operands.

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
| Issue #26 body | Original `date` type proposal — superseded by this document's `date` section. |
| Issue #27 body | `decimal` type proposal — cross-interaction rules with temporal types. |
| Issue #29 body | `integer` type proposal — duration constructor function args are `integer`. |
