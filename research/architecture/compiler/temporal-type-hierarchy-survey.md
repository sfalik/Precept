# Temporal Type Hierarchy Survey

> Raw research collection. No interpretation, no conclusions, no recommendations.
> Research question: How do temporal libraries distinguish between instants, zoned datetimes, local times, dates, durations, and periods? What operations are type-safe vs. type errors?

---

## NodaTime (.NET, Jon Skeet)

Source: https://nodatime.org/3.1.x/userguide/concepts · https://nodatime.org/3.1.x/userguide/arithmetic · https://nodatime.org/3.1.x/api/NodaTime.html

### Type Hierarchy

**Structs (value types):**

| Type | Description |
|------|-------------|
| `Instant` | A point on the global timeline. Nanoseconds since Unix epoch (1970-01-01T00:00:00Z). No timezone, no calendar concept. |
| `Duration` | A fixed, calendar-independent length of time in nanoseconds. Equivalent to `TimeSpan` in .NET BCL but at nanosecond precision. |
| `Period` | A human chronological amount: years, months, weeks, days, hours, minutes, seconds, milliseconds, ticks, nanoseconds. Not a fixed length of time—"one month" varies by month. |
| `LocalDate` | A date within the calendar system, no time zone, no time of day. |
| `LocalTime` | A time of day, no date, no calendar, no time zone. |
| `LocalDateTime` | A date + time in a particular calendar system, no time zone. Does not represent an instant—"November 12th 2009 7pm" happened at different instants for different people. |
| `ZonedDateTime` | A `LocalDateTime` in a specific `DateTimeZone` with a particular `Offset`. Global (maps to a single `Instant`). |
| `OffsetDateTime` | A `LocalDateTime` + `Offset`. Like BCL `DateTimeOffset`. Has a fixed offset but not full timezone rules. |
| `OffsetDate` | A `LocalDate` + `Offset`. No time-of-day component. |
| `OffsetTime` | A `LocalTime` + `Offset`. No date component. |
| `Offset` | A UTC offset in seconds. Positive = ahead of UTC (Europe); negative = behind (America). |
| `Interval` | An interval between two `Instant` values (start and end). |
| `DateInterval` | An interval between two `LocalDate` values. |
| `AnnualDate` | A month-day combination without a year (e.g., birthdays). |
| `YearMonth` | A year and month without a day. |

**Classes:**

| Type | Description |
|------|-------------|
| `DateTimeZone` | A mapping between UTC and local time. Includes DST rules. Uses IANA (Olson/TZDB) database or BCL `TimeZoneInfo`. |
| `CalendarSystem` | Maps the local time line to human concepts (years, months, days). Default is ISO-8601. Supports Gregorian, Julian, Coptic, Buddhist, Hebrew, etc. |
| `IClock` | Interface returning the current `Instant`. `SystemClock.Instance` is the singleton implementation. |

**Internal (unexposed):**

`LocalInstant` — a local value without calendar reference, not part of the public API.

### Duration vs. Period Distinction

`Duration`: A fixed number of nanoseconds. Calendar-independent. "3 minutes" is always 180 seconds. Used for timeline arithmetic.

`Period`: A collection of calendar-based unit/value pairs. "1 month" applied to January 1 yields 31 days; applied to February 1 yields 28 or 29 days. `Period` is not normalized by default: a period of "2 days" is not the same as "48 hours" (period.Hours on a period of "1 day" returns 0, not 24).

Period components: years, months, weeks, days, hours, minutes, seconds, milliseconds, ticks, nanoseconds. These are added from most to least significant when applied to a local value.

### Machine Time vs. Human Time Distinction

**Machine time:** `Instant`, `Interval`. No timezone, no calendar. An `Instant` is the number of nanoseconds since the Unix epoch. The Unix epoch's UTC definition is incidental; only the count matters.

**Human time:** `LocalDate`, `LocalTime`, `LocalDateTime`. These are not points on the global timeline. "7pm" occurred at different instants for people in different timezones.

**Bridge types:** `ZonedDateTime`, `OffsetDateTime`, `OffsetDate`, `OffsetTime`. These carry both a local representation and the information needed to locate it on the global timeline.

### Cross-Hierarchy Conversions

| From | To | Method |
|------|----|--------|
| `Instant` | `ZonedDateTime` | `instant.InZone(DateTimeZone)` |
| `Instant` | `ZonedDateTime` (UTC) | `instant.InUtc()` |
| `ZonedDateTime` | `Instant` | `zdt.ToInstant()` |
| `ZonedDateTime` | `LocalDateTime` | `zdt.LocalDateTime` (property) |
| `ZonedDateTime` | `LocalDate` | `zdt.Date` |
| `ZonedDateTime` | `LocalTime` | `zdt.TimeOfDay` |
| `LocalDateTime` | `ZonedDateTime` | `tz.AtStrictly(ldt)` (throws if ambiguous/skipped); `tz.AtLeniently(ldt)` (auto-resolves) |
| `LocalDate` + `LocalTime` | `LocalDateTime` | `date + time` (operator overload) |
| `LocalDateTime` | `OffsetDateTime` | `ldt.WithOffset(Offset)` |
| `OffsetDateTime` | `Instant` | `odt.ToInstant()` |
| `Instant` | `OffsetDateTime` | `instant.WithOffset(Offset)` |
| `IClock` | `Instant` | `clock.GetCurrentInstant()` |

Explicit timezone required for all conversions between local and global types. No implicit coercion.

### Arithmetic Operations (per type)

**`Instant`:**
- `Instant + Duration → Instant`
- `Instant - Duration → Instant`
- `Instant - Instant → Duration`
- Comparison operators defined

**`Duration`:**
- `Duration + Duration → Duration`
- `Duration - Duration → Duration`
- `Duration * long → Duration`
- `Duration / long → Duration`
- Negation, comparison

**`Period`:**
- `Period + Period → Period` (components summed, no normalization)
- `Period - Period → Period`
- `Period.Between(LocalDate, LocalDate) → Period`
- `Period.Between(LocalDateTime, LocalDateTime) → Period`
- `Period.Between(LocalTime, LocalTime) → Period`

**`LocalDate`:**
- `LocalDate + Period → LocalDate`
- `LocalDate - Period → LocalDate`
- `LocalDate.PlusDays(int)`, `PlusMonths(int)`, `PlusYears(int)`, `PlusWeeks(int)`
- `LocalDate - LocalDate → Period` (via `Period.Between`)
- Comparison operators

**`LocalTime`:**
- `LocalTime + Period → LocalTime` (period must contain only time units)
- `LocalTime.PlusHours(int)`, etc.
- Wraps around midnight transparently

**`LocalDateTime`:**
- `LocalDateTime + Period → LocalDateTime`
- `LocalDateTime.PlusYears(int)`, `PlusMonths(int)`, etc.

**`ZonedDateTime`:**
- `ZonedDateTime + Duration → ZonedDateTime` (timeline arithmetic, DST transitions are "experienced")
- `ZonedDateTime - Duration → ZonedDateTime`
- `ZonedDateTime - ZonedDateTime → Duration`
- No `Period` arithmetic (deliberately unsupported)

### Type Errors (deliberately unsupported operations)

- `Instant + Period` — not defined; a Period has no meaning without a local context.
- `ZonedDateTime + Period` — not defined; Noda Time forces conversion to local types for Period arithmetic.
- `LocalDateTime + Duration` — not defined; Duration arithmetic belongs to the global timeline.
- `LocalDate + Period` with time-unit components — raises `ArgumentException` at runtime.
- `LocalTime + Period` with date-unit components — raises `ArgumentException` at runtime.
- `OffsetDateTime + Period` or `+ Duration` — not supported (no arithmetic defined).
- Comparing `LocalDateTime` values with different calendar systems — throws.
- `DateTimeZone.AtStrictly(ldt)` when ldt is ambiguous — throws `AmbiguousTimeException`.
- `DateTimeZone.AtStrictly(ldt)` when ldt is skipped — throws `SkippedTimeException`.

### Timezone Resolution

`DateTimeZone` resolves using the IANA (Olson/TZDB) database, embedded in the NodaTime distribution. Access via `DateTimeZoneProviders.Tzdb["Europe/London"]`. BCL `TimeZoneInfo` is also supported via `DateTimeZoneProviders.Bcl`.

Offset is a simple `Offset` struct (seconds from UTC). ZoneId is implied by `DateTimeZone`.

Three resolution modes when converting `LocalDateTime → ZonedDateTime`:
- `AtStrictly(ldt)`: throws exceptions for ambiguous or skipped times.
- `AtLeniently(ldt)`: for gaps, advances to after the gap; for ambiguities, returns earlier offset.
- `ResolveLocal(ldt, resolver)`: custom resolver function.

### DST Handling

When clocks spring forward (gap): a local time in the gap does not exist. `AtStrictly` throws `SkippedTimeException`. `AtLeniently` advances to the next valid instant after the gap.

When clocks fall back (overlap): a local time occurs twice. `AtStrictly` throws `AmbiguousTimeException`. `AtLeniently` returns the earlier of the two instants.

`ZonedDateTime + Duration` uses elapsed (timeline) time. Adding 20 minutes to 12:45am the night of spring-forward results in 2:05am, not 1:05am—20 elapsed minutes are experienced across the transition.

`DateTimeZone` exposes `GetZoneInterval(Instant)` to query offset, name, and transition boundaries at any instant.

### Common Bugs Prevented

- Using `DateTime.Kind` to track whether a value is UTC or local—Noda Time uses distinct types.
- Adding months to an `Instant` without a calendar—not compilable.
- Comparing `LocalDateTime` values from different timezones as if they were the same instant—not supported; local types have no global ordering.
- Treating a `Period` as a fixed duration—the type system forces `Duration` for fixed arithmetic.
- Forgetting to handle DST gaps/overlaps—`AtStrictly` throws; caller must handle.

### Notes

- Granularity: nanoseconds (vs. 100-nanosecond ticks in .NET BCL).
- Zero point: Unix epoch (1970-01-01 00:00:00 UTC), chosen for interop, not intrinsic meaning.
- Default calendar: ISO-8601 (proleptic Gregorian).
- All types are immutable.
- `Period` components are stored unnormalized; `Period.Normalize()` is available but optional.

---

## java.time (JSR-310, Java 8+)

Source: https://docs.oracle.com/javase/8/docs/api/java/time/package-summary.html · https://docs.oracle.com/javase/8/docs/api/java/time/temporal/package-summary.html

### Type Hierarchy

**Primary date-time types:**

| Type | Description |
|------|-------------|
| `Instant` | An instantaneous point on the time-line. Nanosecond precision. Closest equivalent to `java.util.Date`. |
| `LocalDate` | A date without a time or time-zone. Stores `2010-12-03`. |
| `LocalTime` | A time without a date or time-zone. Stores `11:30`. All precision levels stored in a single type (with zeroes for lower precision). |
| `LocalDateTime` | Date + time without timezone. Stores `2010-12-03T11:30`. |
| `ZonedDateTime` | Full date-time with `ZoneId`. Resolves DST. Closest equivalent to `java.util.GregorianCalendar`. |
| `OffsetDateTime` | Date-time + UTC offset (`ZoneOffset`). No DST rules. Common in XML/network protocols and database storage. |
| `OffsetTime` | Time + UTC offset. No date. |

**Quantity types:**

| Type | Description |
|------|-------------|
| `Duration` | A time-based amount. Nanosecond precision. Implements `TemporalAmount`. Stored as seconds + nano-adjustment. |
| `Period` | A date-based amount. Years, months, days. Implements `TemporalAmount`. |

**Partial types:**

| Type | Description |
|------|-------------|
| `Year` | A year alone. |
| `YearMonth` | Year + month (e.g., credit card expiry). |
| `MonthDay` | Month + day-of-month (e.g., annual events). |
| `Month` | Enum: January through December. |
| `DayOfWeek` | Enum: Monday through Sunday. |

**Timezone types:**

| Type | Description |
|------|-------------|
| `ZoneId` | A time-zone identifier such as `Europe/Paris`. Encodes DST transition rules. |
| `ZoneOffset` | A fixed offset from UTC such as `+02:00`. A subtype of `ZoneId`. |

**Framework/abstraction layer (`java.time.temporal`):**

| Interface/Class | Description |
|----------------|-------------|
| `TemporalAccessor` | Read-only interface. All date-time types implement this. Field access via `get(TemporalField)`. |
| `Temporal` | Read-write interface extending `TemporalAccessor`. Supports `plus`, `minus`, `with`. |
| `TemporalAmount` | Interface for amounts of time. Both `Duration` and `Period` implement this. Methods: `addTo(Temporal)`, `subtractFrom(Temporal)`, `get(TemporalUnit)`. |
| `TemporalField` | A field such as `HOUR_OF_DAY`. Standard fields in `ChronoField`. |
| `TemporalUnit` | A unit such as `DAYS`. Standard units in `ChronoUnit`. |
| `TemporalAdjuster` | A function `Temporal → Temporal`. Implemented by `TemporalAdjusters` (e.g., `next(DayOfWeek)`, `lastDayOfMonth()`). |
| `TemporalQuery<R>` | A query over a temporal object. |
| `UnsupportedTemporalTypeException` | Thrown when a `ChronoField` or `ChronoUnit` is not supported by a specific type. |

### Duration vs. Period Distinction

`Duration`: time-based, nanosecond precision, stored as `long seconds + int nanoAdjustment`. Represents elapsed machine time. `PT34.5S` = 34.5 seconds.

`Period`: date-based, stored as `int years + int months + int days`. Represents a human calendar amount. `P2Y3M4D` = 2 years, 3 months, 4 days.

Both implement `TemporalAmount`, so code can accept either; however, applying a `Period` to an `Instant` throws `UnsupportedTemporalTypeException` because `Instant` does not support date fields.

The ISO 8601 format `P[n]Y[n]M[n]DT[n]H[n]M[n]S` is shared conceptually, but `Duration` only parses the time portion (`PT...`); `Period` only handles the date portion.

### Machine Time vs. Human Time Distinction

**Machine time:** `Instant`. A point on the global timeline. No timezone, no calendar fields. Not human-readable as such.

**Human time:** `LocalDate`, `LocalTime`, `LocalDateTime`. No timezone association. Represent calendar values a person would read from a wall clock or calendar.

**Hybrid (bridging):** `ZonedDateTime` (carries `ZoneId`, resolves DST), `OffsetDateTime` (carries `ZoneOffset`, no DST).

### Cross-Hierarchy Conversions

| From | To | Method |
|------|----|--------|
| `Instant` | `ZonedDateTime` | `instant.atZone(ZoneId)` |
| `ZonedDateTime` | `Instant` | `zdt.toInstant()` |
| `ZonedDateTime` | `LocalDateTime` | `zdt.toLocalDateTime()` |
| `ZonedDateTime` | `OffsetDateTime` | `zdt.toOffsetDateTime()` |
| `LocalDateTime` | `ZonedDateTime` | `ldt.atZone(ZoneId)` |
| `LocalDateTime` | `OffsetDateTime` | `ldt.atOffset(ZoneOffset)` |
| `LocalDate` + `LocalTime` | `LocalDateTime` | `date.atTime(time)` |
| `LocalDateTime` | `LocalDate` | `ldt.toLocalDate()` |
| `LocalDateTime` | `LocalTime` | `ldt.toLocalTime()` |
| `OffsetDateTime` | `ZonedDateTime` | `odt.atZoneSameInstant(ZoneId)` or `atZoneSimilarLocal(ZoneId)` |
| `Instant` | Unix epoch seconds | `instant.getEpochSecond()` |
| Unix epoch seconds | `Instant` | `Instant.ofEpochSecond(long)` |
| `OffsetDateTime` | `Instant` | `odt.toInstant()` |

`ZoneId.systemDefault()` returns the JVM's current timezone. Timezone is always explicit in conversions.

### Arithmetic Operations (per type)

**`Instant`:**
- `instant.plus(Duration)` → `Instant`
- `instant.minus(Duration)` → `Instant`
- `instant.plus(long, ChronoUnit)` for fixed units (seconds, minutes, hours, etc.)
- `instant.until(Instant, ChronoUnit)` → `long`
- `instant.plus(Period)` → throws `UnsupportedTemporalTypeException`

**`LocalDate`:**
- `date.plus(Period)` → `LocalDate`
- `date.plus(long, ChronoUnit)` → `LocalDate` (for date units: DAYS, WEEKS, MONTHS, YEARS)
- `date.until(LocalDate, ChronoUnit)` → `long`
- `date.until(ChronoLocalDate)` → `Period`
- `date.plusDays(long)`, `plusWeeks(long)`, `plusMonths(long)`, `plusYears(long)`

**`LocalTime`:**
- `time.plus(Duration)` → `LocalTime`
- `time.plus(long, ChronoUnit)` for time units
- `time.until(LocalTime, ChronoUnit)` → `long`
- Wraps around midnight

**`LocalDateTime`:**
- `ldt.plus(Period)` → `LocalDateTime`
- `ldt.plus(Duration)` → `LocalDateTime`
- Both `Period` and `Duration` can be applied

**`ZonedDateTime`:**
- `zdt.plus(Period)` → `ZonedDateTime` (calendar arithmetic, respects DST)
- `zdt.plus(Duration)` → `ZonedDateTime` (elapsed time arithmetic)
- `zdt1.until(zdt2, ChronoUnit)` → `long`
- Adding `Period.ofDays(1)` ≠ adding `Duration.ofHours(24)` across a DST boundary

**`Duration`:**
- `d1.plus(d2)` → `Duration`
- `d.multipliedBy(long)` → `Duration`
- `d.dividedBy(long)` → `Duration`
- `d.negated()` → `Duration`
- `Duration.between(Temporal, Temporal)` → `Duration`

**`Period`:**
- `p1.plus(Period)` → `Period`
- `p.multipliedBy(int)` → `Period`
- `p.negated()` → `Period`
- `Period.between(LocalDate, LocalDate)` → `Period`

### Type Errors (deliberately unsupported operations)

- `instant.plus(Period)` → `UnsupportedTemporalTypeException` (Instant does not support date fields like `MONTHS`)
- `localDate.plus(Duration)` → `UnsupportedTemporalTypeException` (LocalDate does not support `SECONDS` or `NANOS`)
- Comparing `LocalDate` to `ZonedDateTime` directly — different type; `ChronoLocalDate` comparisons possible but carry warnings
- `LocalDate.until(ZonedDateTime, ...)` — mixed types rejected
- `OffsetDateTime` added to `OffsetDateTime` — `TemporalAmount` addTo is the pattern, not direct +

### Timezone Resolution

`ZoneId` = a timezone rule set (e.g., `America/New_York`). Knows DST transitions. Resolved from IANA TZDB embedded in the JDK.

`ZoneOffset` = a fixed offset (`+02:00`). A subtype of `ZoneId`. Carries no DST information.

`ZonedDateTime.of(LocalDateTime, ZoneId)` resolves DST gaps by default by advancing to the next valid time. Overlaps default to the pre-transition offset.

`ZonedDateTime.ofStrict(LocalDateTime, ZoneOffset, ZoneId)` throws `DateTimeException` if the offset does not match the zone at that local time.

### DST Handling

Spring forward (gap): `ZonedDateTime.of` adjusts the local time forward past the gap. The invalid hour does not produce an exception by default.

Fall back (overlap): `ZonedDateTime.of` defaults to the earlier (pre-transition) offset. `ZonedDateTime.ofInstant` can be used to specify the exact instant.

`ZoneRules.getValidOffsets(LocalDateTime)` returns a list: empty for gaps, two offsets for overlaps, one for normal times.

`ZonedDateTime.plus(Period.ofDays(1))` keeps the same local time (e.g., noon stays noon across a DST change); `plus(Duration.ofHours(24))` is 24 elapsed hours.

### Common Bugs Prevented

- Using `java.util.Date` ambiguously — `Instant` has no timezone, `LocalDate` has no time.
- Applying a month-based period to an instant — `UnsupportedTemporalTypeException` at runtime.
- Storing a `ZoneId` as a fixed offset in a database — `OffsetDateTime` is explicit for this pattern.
- Confusing `Period.ofDays(1)` with `Duration.ofHours(24)` at DST boundaries — different types.

### Notes

- Design deliberately avoids type explosion: no separate `Hour`, `HourMinute` types — `LocalTime` covers all precisions.
- API uses consistent method prefix conventions: `of`, `from`, `parse`, `get`, `is`, `with`, `plus`, `minus`, `to`, `at`.
- `TemporalAmount` abstraction allows algorithms to work over both `Duration` and `Period`, but individual temporal types often don't support both.
- `UnsupportedTemporalTypeException` is a runtime exception — the type hierarchy does not prevent misuse at compile time for generic `TemporalAmount` code.

---

## Chrono (Rust)

Source: https://docs.rs/chrono/latest/chrono/

### Type Hierarchy

**Timezone-aware:**

| Type | Description |
|------|-------------|
| `DateTime<Utc>` | UTC date-time. Most efficient timezone type. |
| `DateTime<Local>` | System local timezone. Depends on OS timezone database. |
| `DateTime<FixedOffset>` | Fixed offset (e.g., `UTC+09:00` or `UTC-10:30`). Often produced by parsing. Stores most information; independent of system environment. |
| `DateTime<Tz>` | Generic over any type implementing `TimeZone` trait. |

**Timezone-naive (the `Naive*` family):**

| Type | Description |
|------|-------------|
| `NaiveDate` | Calendar date without timezone. ISO 8601. Supports proleptic Gregorian from ~262145 BCE to ~262143 CE. |
| `NaiveTime` | Time of day without timezone. Nanosecond precision. Supports optional leap second representation. |
| `NaiveDateTime` | Combined date and time without timezone. |

**Duration / time-delta types:**

| Type | Description |
|------|-------------|
| `TimeDelta` | Signed duration in seconds + nanoseconds. Calendar-independent elapsed time. (Previously named `Duration`; `Duration` remains as a type alias.) |
| `Months` | A duration in calendar months. Separate type—not a `TimeDelta`. |
| `Days` | A duration in calendar days. Separate type—not a `TimeDelta`. |

**Traits:**

| Trait | Description |
|-------|-------------|
| `TimeZone` | Defines how local time maps to UTC. Implemented by `Utc`, `Local`, `FixedOffset`. |
| `Datelike` | Common date methods: `year()`, `month()`, `day()`, `weekday()`, etc. |
| `Timelike` | Common time methods: `hour()`, `minute()`, `second()`, `nanosecond()`. |
| `Offset` | The UTC offset for a particular instant in a timezone. |

**Disambiguation type:**

| Type | Description |
|------|-------------|
| `MappedLocalTime<T>` | An enum returned when converting local time to UTC: `Single(T)` (unambiguous), `Ambiguous(T, T)` (overlap), `None` (gap/invalid). |

### Duration vs. Period Distinction

`TimeDelta`: exact elapsed time in seconds + nanoseconds. Calendar-independent. Corresponds to NodaTime's `Duration`.

`Months`: calendar months. The number of days it represents depends on the starting date. Adding 1 month to January 31 in a non-leap year yields February 28 (or similar truncation).

`Days`: calendar days. Distinguished from `TimeDelta` because a "day" may be 23 or 25 hours across DST transitions.

Chrono does not have a general "period" type combining years, months, weeks, and days into one value.

### Machine Time vs. Human Time Distinction

**Machine time:** `std::time::SystemTime` (OS system clock, UTC), `std::time::Instant` (monotonic, opaque, not calendar-based). These are Rust standard library types, not Chrono types.

**Calendar time, timezone-aware:** `DateTime<Utc>`, `DateTime<Local>`, `DateTime<FixedOffset>`.

**Calendar time, timezone-naive:** `NaiveDate`, `NaiveTime`, `NaiveDateTime`. Named `Naive*` to signal that timezone context is absent.

### Cross-Hierarchy Conversions

| From | To | Method |
|------|----|--------|
| `NaiveDate` + `NaiveTime` | `NaiveDateTime` | `date.and_time(time)` |
| `NaiveDateTime` | `DateTime<Utc>` | `naive_dt.and_utc()` |
| `NaiveDateTime` | `DateTime<FixedOffset>` | `FixedOffset::east_opt(secs)?.from_local_datetime(&naive_dt)?` |
| `DateTime<Tz>` | `DateTime<OtherTz>` | `dt.with_timezone(&other_tz)` |
| `DateTime<Tz>` | `NaiveDateTime` (local view) | `dt.naive_local()` |
| `DateTime<Tz>` | `NaiveDateTime` (UTC view) | `dt.naive_utc()` |
| Unix timestamp | `DateTime<Utc>` | `DateTime::from_timestamp(secs, nanos)` |
| `DateTime<Utc>` | Unix timestamp | `dt.timestamp()` + `dt.timestamp_subsec_nanos()` |
| `Utc::now()` | `DateTime<Utc>` | direct |
| `Local::now()` | `DateTime<Local>` | direct |

### Arithmetic Operations (per type)

**`DateTime<Tz>`:**
- `DateTime + TimeDelta → DateTime<Tz>` (elapsed time arithmetic)
- `DateTime - TimeDelta → DateTime<Tz>`
- `DateTime - DateTime → TimeDelta` (via `signed_duration_since`)
- `DateTime + Days → DateTime<Tz>` (calendar days)
- `DateTime + Months → DateTime<Tz>` (calendar months)
- Comparison operators defined (compares instants)

**`NaiveDate`:**
- `NaiveDate + TimeDelta → NaiveDate` (days component of TimeDelta)
- `NaiveDate - NaiveDate → TimeDelta`
- `NaiveDate + Months → NaiveDate`
- `NaiveDate + Days → NaiveDate`

**`NaiveTime`:**
- `NaiveTime + TimeDelta → NaiveTime` (wraps; returns carry)
- `NaiveTime - NaiveTime → TimeDelta`

**`NaiveDateTime`:**
- `NaiveDateTime + TimeDelta → NaiveDateTime`
- `NaiveDateTime - NaiveDateTime → TimeDelta`

**`TimeDelta`:**
- `TimeDelta + TimeDelta → TimeDelta`
- `TimeDelta - TimeDelta → TimeDelta`
- `TimeDelta * i32 → TimeDelta`
- `TimeDelta / i32 → TimeDelta`
- Negation, abs

### Type Errors (deliberately unsupported operations)

- `DateTime<Utc> + DateTime<Local>` — types are distinct; no addition between timezone-parameterized types. `with_timezone()` is required to convert.
- `DateTime<Tz1>` and `DateTime<Tz2>` do not mix — Rust's type system enforces this at compile time via the generic parameter.
- `NaiveDate` cannot be directly compared to `DateTime<Tz>` — type error at compile time.
- `TimeDelta` added to `NaiveDate` uses only the days portion; nanoseconds are silently ignored on NaiveDate (only integral days apply).

### Timezone Resolution

`TimeZone` trait defines `from_local_datetime(NaiveDateTime) → MappedLocalTime<DateTime<Tz>>`.

`MappedLocalTime` enum prevents ignoring DST ambiguity:
- `Single(dt)` — unambiguous
- `Ambiguous(dt_early, dt_late)` — two valid instants; caller must choose
- `None` — invalid local time (gap); caller must handle

`chrono-tz` companion crate provides the full IANA timezone database via the `Tz` enum (e.g., `chrono_tz::America::New_York`). Not included by default to limit binary sizes.

`FixedOffset` stores only a numeric offset; it does not track DST transitions.

### DST Handling

DST ambiguity/gaps are surfaced through `MappedLocalTime<T>` rather than exceptions. Callers use `.unwrap()`, `.single()`, `.earliest()`, `.latest()`, or pattern-match.

Adding `Days::new(1)` to a `DateTime<Tz>` preserves local clock time across a DST boundary. Adding `TimeDelta::hours(24)` advances by exactly 24 elapsed hours.

`FixedOffset` timezones never produce ambiguous or invalid times; they are always `Single`.

### Common Bugs Prevented

- Mixing timezone-aware and timezone-naive types at compile time—Rust's type system rejects it.
- Two datetimes in different timezone types cannot be compared directly—requires explicit `with_timezone` conversion.
- DST gaps and overlaps surface `MappedLocalTime::None` and `MappedLocalTime::Ambiguous` rather than silently returning an incorrect time.

### Notes

- Chrono does not support leap seconds fully; `NaiveTime` can represent a leap second but arithmetic may not handle them correctly.
- `std::time::Instant` (monotonic) is distinct from Chrono's `DateTime`—different intended use cases.
- Rust's ownership and type system enforce timezone type-safety at compile time, not just at runtime.
- Timezone data is not included by default; must be added via `chrono-tz` or `tzfile`.

---

## Python `datetime` module + `pytz` / `zoneinfo`

Source: https://docs.python.org/3/library/datetime.html

### Type Hierarchy

| Type | Description |
|------|-------------|
| `date` | Year, month, day. Always naive. Idealized Gregorian calendar extended in both directions. |
| `time` | Hour, minute, second, microsecond, optional `tzinfo`. Can be naive or aware. No notion of leap seconds. |
| `datetime` | Combination of `date` + `time`. Inherits from `date`. Can be naive or aware. Microsecond precision. |
| `timedelta` | Duration. Stored internally as days + seconds + microseconds. Microsecond precision. |
| `tzinfo` | Abstract base class for timezone information objects. |
| `timezone` | Concrete subclass of `tzinfo` for fixed UTC offsets. Built in since Python 3.2. |
| `zoneinfo.ZoneInfo` | IANA timezone database access. Added in Python 3.9. |

The `datetime` class inherits from `date`; `timezone` inherits from `tzinfo`.

### Naive vs. Aware Distinction

A `datetime` or `time` is **aware** if:
1. `.tzinfo` is not `None`, AND
2. `.tzinfo.utcoffset(self)` does not return `None`.

Otherwise it is **naive**.

`date` objects are always naive. `timedelta` objects have no naive/aware distinction.

Naive datetimes have no timezone association—the program is responsible for knowing what they mean. The standard library documentation warns that naive datetimes are often ambiguous and are best avoided when working across timezones.

### Duration vs. Period Distinction

Python's `timedelta` is a fixed-duration type (days + seconds + microseconds). There is no equivalent of NodaTime's `Period` or Java's `Period`.

There is no built-in "1 month" or "1 year" type. Adding months requires third-party libraries (`dateutil.relativedelta`) or manual calendar logic. `timedelta` can represent any number of days, seconds, and microseconds, but not calendar-relative units.

### Machine Time vs. Human Time Distinction

Python does not distinguish between machine time and human time at the type level. A naive `datetime` can represent either, depending on programmer convention.

`datetime.utcnow()` returns a naive `datetime` representing UTC—but the type does not say so. This is a known source of bugs and was deprecated in Python 3.12.

`datetime.now(timezone.utc)` returns an aware `datetime` with UTC timezone attached.

The `time.time()` function returns a Unix timestamp (float); `datetime.fromtimestamp(ts)` converts to local time.

### Cross-Hierarchy Conversions

| From | To | Method |
|------|----|--------|
| `datetime` (naive) | `datetime` (aware) | `dt.replace(tzinfo=tz)` — attaches tz without conversion |
| `datetime` (aware) | `datetime` (different tz) | `dt.astimezone(tz)` — converts to equivalent moment in tz |
| `datetime` (aware) | `datetime` (naive) | `dt.replace(tzinfo=None)` — strips timezone |
| `datetime` | `date` | `dt.date()` |
| `datetime` | `time` | `dt.time()` (naive) or `dt.timetz()` (preserves tzinfo) |
| `date` + `time` | `datetime` | `datetime.combine(date, time)` |
| `datetime` (aware) | Unix timestamp | `dt.timestamp()` |
| Unix timestamp | `datetime` (aware UTC) | `datetime.fromtimestamp(ts, tz=timezone.utc)` |

`pytz` (third-party): `tz.localize(naive_dt)` attaches timezone; `dt.normalize(dt)` adjusts for DST transitions after arithmetic. Using `replace(tzinfo=pytz_tz)` directly (without `localize`) produces incorrect offsets for non-UTC zones.

`zoneinfo` (Python 3.9+): `dt.replace(tzinfo=ZoneInfo('America/New_York'))` attaches timezone; DST normalization handled automatically. Recommended over `pytz` for new code.

### Arithmetic Operations (per type)

**`date`:**
- `date + timedelta → date`
- `date - timedelta → date`
- `date - date → timedelta` (result has only days; seconds and microseconds are 0)
- Comparison operators

**`datetime`:**
- `datetime + timedelta → datetime` (result has same tzinfo as input; no timezone adjustment is performed)
- `datetime - timedelta → datetime`
- `datetime - datetime → timedelta` — **only if both are naive, or both are aware**
  - If both aware with different tzinfo, converts both to UTC first, then subtracts
- Comparison: naive and aware datetimes are never equal; comparing raises `TypeError` for ordering

**`time`:**
- Arithmetic on `time` objects is **not supported**. `time.resolution = timedelta(microseconds=1)` but no `+` or `-` operators.
- Comparison between naive and aware `time` raises `TypeError`

**`timedelta`:**
- `timedelta + timedelta → timedelta`
- `timedelta - timedelta → timedelta`
- `timedelta * int → timedelta`
- `timedelta * float → timedelta`
- `timedelta / int → timedelta`
- `timedelta / timedelta → float`
- `timedelta // timedelta → int`
- `timedelta % timedelta → timedelta`
- `divmod(timedelta, timedelta) → (int, timedelta)`
- Negation, `abs()`

### Type Errors (deliberately unsupported operations)

- `date + date` — no operator; `TypeError`
- `datetime - date` — `TypeError`
- `datetime (naive) - datetime (aware)` — `TypeError`
- `datetime (naive) < datetime (aware)` — `TypeError`
- `time - time` — not supported; no operator defined
- `time (naive) == time (aware)` — always `False` (never equal), no `TypeError` since Python 3.3
- `time (naive) < time (aware)` — `TypeError`

### Timezone Resolution

`timezone` class: fixed offsets only. `timezone(timedelta(hours=5, minutes=30))` = `+05:30`. Cannot represent DST.

`pytz`: provides IANA timezone objects. `pytz.timezone('America/New_York')`. Requires `localize()` for naive-to-aware conversion; `normalize()` after arithmetic that crosses DST boundaries.

`zoneinfo.ZoneInfo('America/New_York')`: wraps IANA database. Handles DST automatically when attached to a `datetime`. DST ambiguity resolution uses the `fold` attribute.

`fold` attribute (Python 3.6+) on `datetime` and `time`: `fold=0` = earlier (pre-transition) occurrence; `fold=1` = later (post-transition) occurrence. DST-aware `tzinfo` implementations use this to disambiguate wall clock times.

### DST Handling

Spring forward (gap): `datetime.replace(tzinfo=...)` does not validate that the time exists; it can produce a datetime in the skipped hour. `pytz.normalize()` adjusts forward. `zoneinfo` follows RFC 5546/IANA rules.

Fall back (overlap): `fold=0` selects the pre-transition (DST) time; `fold=1` selects the post-transition (standard) time. `datetime` instances differing only by `fold` compare as equal.

`astimezone()` accounts for DST: calling it on a naive datetime presumes system local time.

`pytz` special case: `pytz.timezone` objects are not safe to use with `replace()`—they store a "LMT" (Local Mean Time) offset by default. Only `localize()` is safe.

### Common Bugs Prevented (or not prevented)

Python's `datetime` does **not** prevent many common bugs:
- Naive and aware datetimes coexist in the same type; the only enforcement is comparison operators.
- `datetime.utcnow()` returns a naive datetime representing UTC—visually indistinguishable from a local naive datetime. Deprecated Python 3.12.
- Arithmetic `datetime + timedelta` silently produces a result with the same tzinfo—no DST normalization occurs. A naive datetime representing 2am on a spring-forward night plus 1 hour may produce 3am, skipping over the 2am gap without error.
- `time` object arithmetic is not supported, which prevents some bugs but also limits utility.

Third-party types: The `DateType` library (PyPI) introduces static type distinctions between naive and aware datetimes for type checkers.

---

## PostgreSQL Temporal Types

Source: https://www.postgresql.org/docs/current/datatype-datetime.html · https://www.postgresql.org/docs/current/functions-datetime.html

### Type Hierarchy

| Type | Storage | Description |
|------|---------|-------------|
| `date` | 4 bytes | Date only. Range: 4713 BC to 5874897 AD. 1-day resolution. |
| `time [WITHOUT TIME ZONE]` | 8 bytes | Time of day. No date. Range: 00:00:00 to 24:00:00. 1-microsecond resolution. |
| `time WITH TIME ZONE` (`timetz`) | 12 bytes | Time of day + stored UTC offset. Recommended against by PostgreSQL documentation. |
| `timestamp [WITHOUT TIME ZONE]` | 8 bytes | Date + time. No timezone association. Range: 4713 BC to 294276 AD. 1-microsecond resolution. |
| `timestamp WITH TIME ZONE` (`timestamptz`) | 8 bytes | Date + time, stored as UTC internally. Displayed in session timezone. |
| `interval` | 16 bytes | A time interval. Stored as 3 fields: months (int), days (int), microseconds (int8). Range: ±178000000 years. |

**Key architectural note:** `timestamptz` stores UTC; `timestamp` stores the literal value with no timezone semantics.

All timezone-aware values (`timestamptz`, `timetz`) are stored internally in UTC. They are converted to the session's `TimeZone` parameter for display.

### Duration vs. Period Distinction

PostgreSQL uses a single `interval` type that conflates what other systems separate into "duration" and "period":

- `interval` stores: months (calendar-based), days (calendar-based, separate from months because month lengths vary), microseconds (elapsed time within a day).
- A day in `interval` can represent 23 or 25 hours across a DST transition.
- `'1 day'::interval` ≠ `'24 hours'::interval` in DST arithmetic; they are stored differently in the interval struct.
- `justify_days(interval)` converts 30-day periods to months; `justify_hours(interval)` converts 24-hour periods to days—these are normalization functions.

### Machine Time vs. Human Time Distinction

**Machine time / UTC:** `timestamptz`. Stored as UTC. Conceptually analogous to an instant. Converting to display uses the session `TimeZone`.

**Human time / local:** `timestamp` (without time zone). No timezone association. What you store is what you get back.

**No explicit type for "local naive datetime" vs. "UTC instant"**—PostgreSQL's type system uses `WITH/WITHOUT TIME ZONE` modifiers.

### Cross-Hierarchy Conversions

| Operation | Result |
|-----------|--------|
| `timestamp AT TIME ZONE zone` | `timestamptz` — assumes input is in named zone, returns UTC |
| `timestamptz AT TIME ZONE zone` | `timestamp` — converts UTC to local time in named zone, strips tz |
| `timestamp AT LOCAL` | `timestamptz` (using session TimeZone) |
| `timestamptz AT LOCAL` | `timestamp` (local view of UTC value) |
| `time WITH TIME ZONE AT TIME ZONE zone` | `time WITH TIME ZONE` (in new zone) |
| `to_timestamp(double)` | `timestamptz` (from Unix epoch seconds) |
| `EXTRACT(EPOCH FROM timestamptz)` | Unix epoch seconds (accounting for UTC) |
| `EXTRACT(EPOCH FROM timestamp)` | Nominal seconds since 1970 (no tz adjustment) |

Timezone specifications: full name (`America/New_York`), abbreviation (`PST` = fixed offset), POSIX-style (sign convention opposite to ISO 8601).

Note: PostgreSQL timezone abbreviations (`PST`) represent a fixed offset. Full timezone names (`America/New_York`) encode DST rules. Using an abbreviation in a DST context requires a date to resolve correctly—and `timetz` cannot provide one.

### Arithmetic Operations (per type)

| Operation | Result | Notes |
|-----------|--------|-------|
| `date + integer` | `date` | Adds days |
| `date - integer` | `date` | Subtracts days |
| `date - date` | `integer` | Number of days elapsed |
| `date + interval` | `timestamp` | |
| `date - interval` | `timestamp` | |
| `date + time` | `timestamp` | |
| `timestamp + interval` | `timestamp` | |
| `timestamp - timestamp` | `interval` | Produces `N days HH:MM:SS` |
| `timestamptz + interval` | `timestamptz` | DST-aware when session TZ applies |
| `timestamptz - timestamptz` | `interval` | Accounts for UTC offsets |
| `time + interval` | `time` | |
| `time - time` | `interval` | |
| `time - interval` | `time` | |
| `interval + interval` | `interval` | |
| `interval - interval` | `interval` | |
| `interval * double` | `interval` | |
| `interval / double` | `interval` | |
| `-interval` | `interval` | Negation |

**NOT defined:**
- `date + date` — no operator
- `timestamp + timestamp` — no operator
- `timestamptz + timestamptz` — no operator

`age(timestamptz, timestamptz)`: returns a "symbolic" interval using years/months/days rather than raw days, calculated via field subtraction. Ambiguous at month boundaries (uses earlier month's day count).

`timestamp + interval '1 day'` vs. `timestamp + interval '24 hours'` across DST: adding `'1 day'` preserves wall clock time; adding `'24 hours'` is exactly 24 elapsed hours.

`date_add(timestamptz, interval [, tz])`: DST-aware interval addition that can specify the timezone explicitly.

### Type Errors (deliberately unsupported operations)

- `date + date` — not an operator
- `timestamp + timestamp` — not an operator
- Comparing `time` to `timestamp` — requires explicit cast
- Interval with stride > day in `date_bin` — `date_bin` requires stride to be less than a month

### Timezone Resolution

PostgreSQL accepts three timezone specification forms:
1. **Full timezone name** (e.g., `America/New_York`): encodes DST rules; from IANA database in `.../share/timezone/`.
2. **Abbreviation** (e.g., `PST`, `EDT`): a fixed offset at the time of use. Does not imply DST rules. `PST` always means UTC−8; `EDT` always means UTC−4—regardless of whether DST is active at that date.
3. **POSIX-style** (e.g., `PST8PDT`): sign convention is opposite to ISO 8601 (POSIX uses positive for west-of-UTC).

`TimeZone` configuration parameter: sets the session timezone for display of `timestamptz` values. Can be set per-session via `SET TIME ZONE '...'`.

PostgreSQL `pg_timezone_names` and `pg_timezone_abbrevs` views list recognized timezone names and abbreviations.

Ambiguity: `MSK` has historically meant different UTC offsets. PostgreSQL interprets abbreviations by the most recently known definition, which may not match civil time at the given date.

### DST Handling

`timestamptz` stores UTC; DST is applied on output only (conversion to session timezone). DST transitions do not affect the stored value.

`timestamp` (without tz): DST has no effect—the stored value is a local wall-clock value with no timezone association. DST arithmetic is the caller's responsibility.

Adding `interval '1 day'` to a `timestamptz` in DST context: PostgreSQL's `+` operator for `timestamptz + interval` adds months and days in calendar steps (preserving local clock time), then adds the microseconds component literally. So `'2005-04-02 12:00-07'::timestamptz + '1 day'::interval` → `2005-04-03 12:00-06` (noon to noon, DST transition applied).

Adding `interval '24 hours'` is purely elapsed time: `→ 2005-04-03 13:00-06` (13:00 because 24 elapsed hours crossed a 1-hour DST spring-forward).

`date_add(ts, interval, 'America/Denver')` performs DST-aware day addition using the named timezone, regardless of session timezone.

### Common Bugs Prevented (or not prevented)

PostgreSQL does not prevent many temporal pitfalls at the type system level:
- `TIMESTAMP` values silently store whatever is given; no timezone information is tracked.
- Casting `timestamp '2004-10-19 10:23:54+02'` silently ignores the offset—produces `2004-10-19 10:23:54` as `timestamp without time zone`.
- `TIME WITH TIME ZONE` stores only an offset, not a timezone name; DST resolution requires a date that the type cannot provide. PostgreSQL documentation recommends against using it.
- Comparing `timestamp` to `timestamptz` assumes the former is in the session timezone—implicit and possibly incorrect.
- `EXTRACT(EPOCH FROM timestamp)` treats the timestamp as nominal seconds since epoch with no timezone, which may be misleading.

---

## ISO 8601 / RFC 3339 Semantics

Source: https://en.wikipedia.org/wiki/ISO_8601 · RFC 3339 (IETF, July 2002) · RFC 9557 (IXDTF, 2024)

### Type Distinctions in the Standard

ISO 8601 defines **representations**, not programming types. The semantic distinctions are:

| Concept | Standard Form | Example |
|---------|--------------|---------|
| Calendar date | `YYYY-MM-DD` | `2024-04-21` |
| Ordinal date | `YYYY-DDD` | `2024-112` |
| Week date | `YYYY-Www-D` | `2024-W17-1` |
| Time of day | `Thh:mm:ss[.sss]` | `T14:30:00` |
| Local datetime | `YYYY-MM-DDThh:mm:ss` | `2024-04-21T14:30:00` |
| UTC instant | `YYYY-MM-DDThh:mm:ssZ` | `2024-04-21T14:30:00Z` |
| Offset datetime | `YYYY-MM-DDThh:mm:ss±hh:mm` | `2024-04-21T14:30:00+05:30` |
| Duration | `PnYnMnDTnHnMnS` | `P1Y2M3DT4H5M6S` |
| Duration (weeks) | `PnW` | `P2W` |
| Time interval | `start/end`, `start/duration`, `duration/end` | `2024-04-21T14:30Z/P1H` |
| Repeating interval | `Rn/interval` | `R5/2024-04-21T14:30Z/P1D` |

### Duration vs. Period Distinction

ISO 8601 uses the word "duration" for all calendar amounts expressed as `P[n]Y[n]M[n]DT[n]H[n]M[n]S`. It does not distinguish between elapsed time (fixed nanoseconds) and calendar quantities (variable month lengths).

A duration in ISO 8601 is inherently ambiguous without an anchor:
- `P1M` could represent 28, 29, 30, or 31 days depending on the starting month.
- `PT36H` and `P1DT12H` represent the same amount of seconds, but have different behavior across DST transitions.
- The standard notes: "keep in mind that `PT36H` is not the same as `P1DT12H` when switching to or from daylight saving time."

Time intervals use duration to specify a length: `2003-02-15T00:00:00Z/P2M` ends two calendar months later, resolving the ambiguity.

### Machine Time vs. Human Time Distinction

ISO 8601 does not use these terms but encodes the distinction through the presence or absence of a timezone designator:

- `2024-04-21T14:30:00` — local time, unqualified. No timezone. Standard says: "assumed to be in local time"; warns this is ambiguous across different timezones.
- `2024-04-21T14:30:00Z` — UTC. The `Z` suffix is a zone designator for UTC (zero offset).
- `2024-04-21T14:30:00+05:30` — a specific offset from UTC. Not necessarily UTC, not necessarily local.

The standard does not define a type for "instant" independently of "offset datetime."

### The `Z` vs. `+00:00` Distinction

Within ISO 8601 proper: both `Z` and `+00:00` mean UTC. `-00:00` is explicitly forbidden (the sign must be `+` for zero offset).

In RFC 3339 (a profile of ISO 8601): `-00:00` is permitted as a distinct representation meaning "the time is UTC but the preferred local offset is unknown." `+00:00` and `Z` both mean UTC is the intended reference. This distinction was inherited from RFC 2822 (email headers).

RFC 3339 deviates from ISO 8601 in:
- Requiring that datetime strings always include the full date, time, and offset (no reduced precision).
- Permitting a space character instead of `T` as the date-time separator (for readability).
- Excluding durations, repeating intervals, and ordinal/week date formats.
- Permitting `-00:00` (forbidden in ISO 8601).

### String Formats

**Dates:** `YYYY-MM-DD` (extended) or `YYYYMMDD` (basic). Basic format discouraged in plain text.

**Times:** `Thh:mm:ss.sss` (extended) or `Thhmmss.sss` (basic). `T` prefix required in ISO 8601-1:2019 for unambiguous contexts.

**Timezone designators:** `Z` (UTC), `±hh:mm` (extended), `±hhmm` (basic), `±hh` (hours only). Absence means local/unqualified.

**Durations:** Begin with `P`. `M` before `T` = months; `M` after `T` = minutes. `P1M` = one month; `PT1M` = one minute.

**Interval formats:**
1. `<start>/<end>` — e.g., `2007-03-01T13:00:00Z/2008-05-11T15:30:00Z`
2. `<start>/<duration>` — e.g., `2007-03-01T13:00:00Z/P1Y2M10DT2H30M`
3. `<duration>/<end>` — e.g., `P1Y2M10DT2H30M/2008-05-11T15:30:00Z`
4. `<duration>` alone — context-dependent

### Ambiguities in the Standard

1. **Local vs. UTC:** `2024-04-21T14:30` is unambiguously local (no `Z` or offset). `2024-04-21T14:30Z` is UTC. The string looks similar; the `Z` is the distinction.

2. **Duration ambiguity without anchor:** `P1M` on its own cannot be converted to seconds without knowing the start date and timezone.

3. **`PT36H` vs. `P1DT12H`:** Same number of seconds but different DST behavior when applied calendrically.

4. **`T` separator:** ISO 8601 requires `T` between date and time. RFC 3339 allows a space. Some implementations accept other single characters.

5. **24:00:00:** After ISO 8601-1:2019/Amd 1:2022, `24:00:00` refers to midnight at the end of a calendar day (same instant as `00:00:00` the next day). Removed and re-added in different amendment cycles.

6. **Week-numbering year vs. calendar year:** The ISO week year can differ from the Gregorian year for dates near January 1 (e.g., 2008-12-29 is ISO week `2009-W01`).

### RFC 9557 (IXDTF — Internet Extended Date/Time Format)

Published 2024. Extends RFC 3339 to include an associated timezone name appended in brackets:
`1996-12-19T16:39:57-08:00[America/Los_Angeles]`

This addresses the common use case where the UTC offset is known from parsing but the timezone name (and its future DST behavior) is also needed. RFC 3339 alone cannot distinguish `−08:00` in standard time vs. daylight time.

### Operations Defined on ISO 8601 Intervals

- Interval `<start>/<end>` — can compute whether two intervals overlap; boundary semantics: the interval is half-open `[start, end)`.
- Repeating interval — specifies a count or unbounded repetition. Does not define arithmetic.
- The standard defines no API for "add duration to date." It defines representation only.

### Notes

- ISO 8601 explicitly does not assign meaning to represented elements beyond what the standard defines—a `2024-04-21` is a date, but the standard does not say whether it is in the Gregorian calendar year 2024 CE in the proleptic Gregorian sense or any other calendar system.
- The standard restricts to the Gregorian calendar; dates before 1582-10-15 are "by mutual agreement."
- A duration format (`P1Y2M3DT4H5M6S`) does not specify whether it is ISO 8601's "duration" (which can conflate elapsed and calendar amounts) or a purely elapsed value—the semantics depend on the application.
