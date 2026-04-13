# NodaTime Type Model Survey

**Research date:** 2026-04-13
**Author:** George (Runtime Dev)
**Relevance:** Evaluates NodaTime as a potential runtime dependency for Precept's temporal (and related) type system. Cross-references proposals: `date` (#26), `decimal` (#27), `integer` (#29), built-in functions (#16). Companion to [type-system-survey.md](type-system-survey.md).

---

## Why NodaTime Exists

NodaTime was created by Jon Skeet because the .NET BCL date/time types are inadequate for correct temporal reasoning. The core problems with `System.DateTime`:

1. **Semantic overloading.** `DateTime` represents at least three different concepts (UTC instant, local date/time, local-to-system-tz date/time) via a single type with a `Kind` property. This makes it impossible to know what a `DateTime` value *means* without inspecting runtime state.
2. **No date-only type (pre-.NET 6).** Before `DateOnly` was introduced in .NET 6, representing "just a date" required using `DateTime` with an ignored time component — a permanent source of bugs.
3. **No time-zone-aware type.** .NET has no built-in type for "a date/time in a specific time zone." `DateTimeOffset` captures an offset but not the time zone identity, so it cannot answer "what time is it here one hour from now?" across DST transitions.
4. **Implicit lossy conversions.** `DateTime` allows implicit conversions that silently lose information (e.g., `DateTime.Now` to `DateTime.UtcNow` depends on the system time zone).
5. **Mutable-ish semantics.** While `DateTime` is a struct (immutable), the `Kind` property can be changed via `DateTime.SpecifyKind`, creating values that look identical but behave differently.

NodaTime's response is to model each temporal concept as a distinct type with no implicit lossy conversions. This aligns directly with Precept's "make invalid states unrepresentable" philosophy.

---

## NodaTime Design Principles

| Principle | Description | Precept Alignment |
|-----------|-------------|-------------------|
| **Force you to think** | Each temporal concept has its own type — you must decide what kind of data you have | Precept's type system forces field authors to declare precise types |
| **Solve the 99% case** | No leap seconds, no custom calendars, no relativistic time | Precept is a business-entity DSL — the 99% case is exactly right |
| **No implicit defaults** | Won't default to system time zone, "now", or current culture | Precept requires explicit values; no hidden environmental dependencies |
| **Immutable value types** | All core types are immutable structs, thread-safe | Precept field values are immutable snapshots |
| **Sealed types** | Almost all types are sealed; "design for inheritance or prohibit it" | Precept types are closed — no user-defined extensions |
| **Explicit conversions** | Converting between types requires deliberate method calls | Precept's type checker rejects implicit cross-type assignments |
| **Semantic versioning** | Follows SemVer; public API stability is a priority | Important for Precept as a runtime dependency |

---

## Core Type Inventory

### Overview Table

| NodaTime Type | Category | Semantics | TZ Relationship | .NET Equivalent | Struct/Class |
|---------------|----------|-----------|-----------------|-----------------|--------------|
| `LocalDate` | Local | Calendar date (year, month, day) without time or timezone | None | `System.DateOnly` (.NET 6+) | Struct |
| `LocalTime` | Local | Time of day without date or timezone | None | `System.TimeOnly` (.NET 6+) | Struct |
| `LocalDateTime` | Local | Date + time without timezone | None | `System.DateTime` (Kind=Unspecified) | Struct |
| `Instant` | Global | Point on the global timeline (nanoseconds since Unix epoch) | Implicit UTC | `System.DateTimeOffset` (offset=0) | Struct |
| `ZonedDateTime` | Global | Date + time in a specific timezone | Required (full zone) | No BCL equivalent | Struct |
| `OffsetDateTime` | Global | Date + time with a UTC offset | Required (offset only) | `System.DateTimeOffset` | Struct |
| `OffsetDate` | Hybrid | Date with a UTC offset | Required (offset only) | No BCL equivalent | Struct |
| `OffsetTime` | Hybrid | Time with a UTC offset | Required (offset only) | No BCL equivalent | Struct |
| `Duration` | Quantity | Fixed number of nanoseconds (elapsed time) | None | `System.TimeSpan` | Struct |
| `Period` | Quantity | Calendar-based period (years, months, days, hours, etc.) | None | No BCL equivalent | Class |
| `Interval` | Range | Bounded range between two `Instant` values (half-open) | Implicit UTC | No BCL equivalent | Struct |
| `DateInterval` | Range | Bounded range between two `LocalDate` values (inclusive) | None | No BCL equivalent | Class |
| `AnnualDate` | Partial | Recurring month + day (no year) | None | No BCL equivalent | Struct |
| `YearMonth` | Partial | Year + month (no day) | None | No BCL equivalent | Struct |
| `Offset` | Component | Difference between UTC and local time (hours/minutes/seconds) | Is the offset | No BCL equivalent | Struct |
| `DateTimeZone` | Component | Full timezone mapping (UTC ↔ local, including DST rules) | Is the zone | `System.TimeZoneInfo` | Class |
| `CalendarSystem` | Component | Calendar rules (Gregorian, Julian, Islamic, Hebrew, etc.) | None | `System.Globalization.Calendar` | Class |

---

### Detailed Type Profiles

#### `LocalDate`

- **Semantics:** A calendar date (year, month, day) in a particular calendar system. No time component, no timezone. Answers the question: "What date is written on the wall calendar?"
- **TZ relationship:** None. A `LocalDate` is the same everywhere in the world.
- **Calendar:** Associated with a `CalendarSystem` (default: ISO-8601 / Gregorian).
- **Range:** Year -9998 to 9999 (Gregorian/ISO).
- **Key properties:** `Year`, `Month`, `Day`, `DayOfWeek`, `DayOfYear`, `Calendar`, `Era`, `YearOfEra`.
- **Arithmetic:**
  - `LocalDate + Period → LocalDate` (calendar-based: months, years, days)
  - `LocalDate - LocalDate → Period` (via `Period.Between(d1, d2)`)
  - `LocalDate.PlusDays(int)`, `PlusMonths(int)`, `PlusYears(int)`, `PlusWeeks(int)` → `LocalDate`
  - No `Duration` arithmetic (durations are clock-based, dates are calendar-based)
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=` (within same calendar system; cross-calendar comparison throws `ArgumentException`).
- **Serialization (JSON):** `"2024-03-15"` — ISO-8601 date pattern `uuuu'-'MM'-'dd`.
- **BCL conversion:** `LocalDate.FromDateOnly(DateOnly)`, `localDate.ToDateOnly()` (.NET 6+). Also `LocalDate.FromDateTime(DateTime)` (ignores time and kind).
- **Key distinction from `DateOnly`:** `LocalDate` supports multiple calendar systems and has richer arithmetic. `DateOnly` is Gregorian-only, arithmetic limited to `AddDays`/`AddMonths`/`AddYears`.

#### `LocalTime`

- **Semantics:** A time of day (hour, minute, second, nanosecond within second). No date, no timezone. Answers: "What time does the alarm go off?"
- **TZ relationship:** None.
- **Range:** midnight (00:00:00) to one nanosecond before midnight (23:59:59.999999999).
- **Key properties:** `Hour`, `Minute`, `Second`, `Millisecond`, `NanosecondOfSecond`, `NanosecondOfDay`, `TickOfSecond`, `TickOfDay`.
- **Arithmetic:**
  - `LocalTime + Period → LocalTime` (time-unit periods only; date-unit periods throw)
  - `LocalTime.PlusHours(long)`, `PlusMinutes(long)`, `PlusSeconds(long)`, etc. → `LocalTime` (wraps at midnight)
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=`.
- **Serialization (JSON):** `"16:45:20.123456789"` — ISO-8601 extended time pattern.
- **BCL conversion:** `LocalTime.FromTimeOnly(TimeOnly)`, `localTime.ToTimeOnly()` (.NET 6+).
- **Key distinction from `TimeOnly`:** `LocalTime` has nanosecond precision (vs. tick/100ns for `TimeOnly`).

#### `LocalDateTime`

- **Semantics:** Date + time in a particular calendar system, without timezone. Answers: "The appointment is at 3pm on March 15th." Does NOT identify a unique instant — that requires a timezone.
- **TZ relationship:** None (explicitly). Converting to an `Instant` requires supplying a `DateTimeZone`.
- **Range:** Combination of `LocalDate` and `LocalTime` ranges.
- **Key properties:** `Date` (→ `LocalDate`), `TimeOfDay` (→ `LocalTime`), plus all date and time component properties.
- **Arithmetic:**
  - `LocalDateTime + Period → LocalDateTime`
  - `LocalDateTime - LocalDateTime → Period` (via `Period.Between`)
  - All `PlusXyz` methods from both `LocalDate` and `LocalTime`
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=` (same calendar only).
- **Serialization (JSON):** `"2024-03-15T16:45:20.123456789"`.
- **BCL conversion:** `LocalDateTime.FromDateTime(DateTime)` (ignores `Kind`), `localDateTime.ToDateTimeUnspecified()`.

#### `Instant`

- **Semantics:** A point on the global timeline — a fixed number of nanoseconds since the Unix epoch (1970-01-01T00:00:00Z). No calendar system, no timezone. Answers: "Exactly when did this happen?"
- **TZ relationship:** Implicit UTC (the epoch is defined in UTC, but an `Instant` is just a number of nanoseconds — it has no timezone concept).
- **Range:** -9998-01-01T00:00:00Z to 9999-12-31T23:59:59.999999999Z.
- **Key properties:** None for date/time components (you must convert to a `ZonedDateTime` or `LocalDateTime` to access those).
- **Arithmetic:**
  - `Instant + Duration → Instant`
  - `Instant - Instant → Duration`
  - `Instant - Duration → Instant`
  - No `Period` arithmetic (periods are calendar-based; instants are not)
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=`.
- **Serialization (JSON):** `"2024-03-15T16:45:20.123456789Z"` — always has trailing `Z`.
- **BCL conversion:** `Instant.FromDateTimeUtc(DateTime)` (Kind must be Utc), `Instant.FromDateTimeOffset(DateTimeOffset)`, `instant.ToDateTimeUtc()`, `instant.ToDateTimeOffset()` (always offset 0).

#### `ZonedDateTime`

- **Semantics:** A `LocalDateTime` in a specific timezone, with the exact `Offset` resolved. The richest temporal type — it captures the instant, the local representation, the timezone rules, and the calendar. Answers: "What time is it on someone's wall clock in Tokyo, and what timezone rules apply?"
- **TZ relationship:** Required (full `DateTimeZone`).
- **Key properties:** `LocalDateTime`, `Date`, `TimeOfDay`, `Offset`, `Zone`, `Calendar`, plus all component accessors.
- **Arithmetic:**
  - `ZonedDateTime + Duration → ZonedDateTime` (time-line arithmetic; may cross DST boundaries)
  - `ZonedDateTime - Duration → ZonedDateTime`
  - `ZonedDateTime - ZonedDateTime → Duration`
  - No `Period` arithmetic (deliberately — timezone interactions with calendar periods are ambiguous; NodaTime forces you to convert to `LocalDateTime` first)
- **Comparison:** Custom comparers available; default compares by instant, then by local time, then by zone.
- **Serialization (JSON):** `"2024-03-15T16:45:20.123456789+01:00 Europe/London"`.
- **BCL conversion:** `ZonedDateTime.FromDateTimeOffset(DateTimeOffset)` (creates with fixed-offset zone), `zonedDateTime.ToDateTimeUtc()`, `zonedDateTime.ToDateTimeOffset()`.
- **Why `Period` arithmetic is excluded:** Adding "1 month" to a `ZonedDateTime` can cross DST transitions, creating ambiguous or skipped local times. NodaTime refuses to guess — it forces you to convert to `LocalDateTime`, do the arithmetic, then convert back with explicit ambiguity resolution.

#### `OffsetDateTime`

- **Semantics:** A `LocalDateTime` with a UTC offset. Identifies a unique instant, but unlike `ZonedDateTime`, carries no timezone *rules* — you cannot predict what the offset would be at a different time. Common in wire formats (RFC 3339, ISO 8601). Answers: "This happened at 4pm local time, which was 1 hour ahead of UTC."
- **TZ relationship:** Required (offset only — no rules).
- **Key properties:** `LocalDateTime`, `Date`, `TimeOfDay`, `Offset`, `Calendar`, plus component accessors.
- **Arithmetic:** Currently none supported (NodaTime considers the use cases unclear).
- **Comparison:** Custom comparers available; no default ordering.
- **Serialization (JSON):** `"2024-03-15T16:45:20.123456789+01:00"` — RFC 3339 pattern.
- **BCL conversion:** `OffsetDateTime.FromDateTimeOffset(DateTimeOffset)`, `offsetDateTime.ToDateTimeOffset()`.

#### `OffsetDate` / `OffsetTime`

- **Semantics:** A `LocalDate` or `LocalTime` with a UTC offset. Rarely used — mainly for XML date/time types like `<date>2024-03-15+01:00</date>`.
- **Introduced:** NodaTime 2.3.
- **Arithmetic:** None.
- **Serialization (JSON):** `"2024-03-15+01:00"` / `"T16:45:20.123456789+01:00"`.

#### `Duration`

- **Semantics:** A fixed number of nanoseconds. The "physical" measure of elapsed time — always the same length regardless of when or where it's applied. Answers: "How many nanoseconds elapsed between these two events?"
- **TZ relationship:** None.
- **Range:** Internally stored as days (signed 24-bit) + nanoseconds within day. Covers approximately ±16.7 million days (±45,000 years). Subsumes the range of `System.TimeSpan`.
- **Key properties:** `Days`, `NanosecondOfDay`, `TotalDays`, `TotalHours`, `TotalMinutes`, `TotalSeconds`, `TotalMilliseconds`, `TotalNanoseconds`, `TotalTicks`.
- **Arithmetic:**
  - `Duration + Duration → Duration`
  - `Duration - Duration → Duration`
  - `Duration * number → Duration`
  - `Duration / number → Duration`
  - `Duration / Duration → double`
  - Unary negation
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=`.
- **Serialization (JSON):** `"-H:mm:ss.FFFFFFFFF"` (hours, not days, as the leading component).
- **BCL conversion:** `Duration.FromTimeSpan(TimeSpan)`, `duration.ToTimeSpan()` (loses sub-tick precision).
- **Key distinction from `TimeSpan`:** Nanosecond precision (vs. 100ns ticks). Deliberate separation from `Offset` (which is also a "time span" but means something different).

#### `Period`

- **Semantics:** A set of calendar-based components: years, months, weeks, days, hours, minutes, seconds, milliseconds, ticks, nanoseconds. The "human" measure of time difference. Answers: "How many years, months, and days between these two dates?" Critically, Periods are NOT normalized — a Period of "2 days" is NOT the same as "48 hours," because they have different semantic intentions.
- **TZ relationship:** None.
- **Value type:** Reference type (class), not a struct. This is because Period can be null and has complex structure.
- **Key properties:** `Years` (int), `Months` (int), `Weeks` (int), `Days` (int), `Hours` (long), `Minutes` (long), `Seconds` (long), `Milliseconds` (long), `Ticks` (long), `Nanoseconds` (long). Each can take any value for its type independently.
- **Arithmetic:**
  - `Period + Period → Period` (component-wise addition; no normalization)
  - `Period - Period → Period` (component-wise subtraction)
  - `LocalDate + Period → LocalDate` / `LocalDateTime + Period → LocalDateTime` / `LocalTime + Period → LocalTime`
  - `Period.Between(start, end, units?)` — computes the period between two local values
- **Comparison:** Equality only (component-wise). No ordering — "1 month" vs "30 days" has no defined order, because it depends on which month.
- **Serialization (JSON):** ISO 8601 duration pattern, e.g., `"P1Y2M3DT4H5M6S"`. Also available: normalizing ISO pattern.
- **Key insight for Precept:** Period's calendar-based arithmetic (adding months respects month lengths, adding years respects leap years) is exactly what business-entity date arithmetic needs. The sequential evaluation model (add months first, then days, with truncation at each step) is predictable but can surprise naive users.

#### `Interval`

- **Semantics:** A half-open range `[start, end)` between two `Instant` values. If you have abutting intervals, any instant falls in exactly one. Can be unbounded on either end (extending to start/end of time).
- **TZ relationship:** Implicit UTC (instants are UTC-based).
- **Key properties:** `Start` (`Instant`), `End` (`Instant`), `Duration` (→ `Duration`), `HasStart`, `HasEnd`.
- **Methods:** `Contains(Instant)`.
- **Comparison:** Equality only (component-wise). No ordering.
- **Serialization (JSON):** `{ "Start": "...", "End": "..." }` (compound object).

#### `DateInterval`

- **Semantics:** A closed range `[start, end]` between two `LocalDate` values in the same calendar. Both endpoints are included. Answers: "I'm on vacation from Monday through Friday" — Friday is included. Implements `IEnumerable<LocalDate>`.
- **TZ relationship:** None.
- **Key properties:** `Start` (`LocalDate`), `End` (`LocalDate`), `Length` (int, in days), `Calendar`.
- **Methods:** `Contains(LocalDate)`, `Contains(DateInterval)`, `Intersection(DateInterval)`, `Union(DateInterval)`.
- **Comparison:** Equality only.
- **Serialization:** `"[2024-01-01, 2024-12-31]"` (toString format).

#### `AnnualDate`

- **Semantics:** A recurring month + day combination without a year. For recurring events: birthdays, anniversaries, annual deadlines. ISO calendar only.
- **TZ relationship:** None.
- **Key properties:** `Month`, `Day`.
- **Methods:** `InYear(int) → LocalDate` (handles Feb 29 in non-leap years by truncating to Feb 28), `IsValidYear(int) → bool`.
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=` (ordered by month, then day).
- **Serialization:** Pattern-formatted, e.g., `"03-15"`.
- **Default value:** January 1st.

#### `YearMonth`

- **Semantics:** A year + month without a day. For "billing period," "monthly report," etc. Supports multiple calendar systems.
- **TZ relationship:** None.
- **Key properties:** `Year`, `Month`, `Calendar`, `Era`, `YearOfEra`.
- **Methods:** `OnDayOfMonth(int) → LocalDate`, `ToDateInterval() → DateInterval`, `PlusMonths(int) → YearMonth`.
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=` (same calendar only).
- **Introduced:** NodaTime 3.0.

---

## Arithmetic Algebra Summary

| Left Operand | Operator | Right Operand | Result | Domain |
|--------------|----------|---------------|--------|--------|
| `LocalDate` | + | `Period` | `LocalDate` | Calendar |
| `LocalDate` | - | `Period` | `LocalDate` | Calendar |
| `LocalTime` | + | `Period` | `LocalTime` | Calendar |
| `LocalTime` | - | `Period` | `LocalTime` | Calendar |
| `LocalDateTime` | + | `Period` | `LocalDateTime` | Calendar |
| `LocalDateTime` | - | `Period` | `LocalDateTime` | Calendar |
| `Instant` | + | `Duration` | `Instant` | Timeline |
| `Instant` | - | `Duration` | `Instant` | Timeline |
| `Instant` | - | `Instant` | `Duration` | Timeline |
| `ZonedDateTime` | + | `Duration` | `ZonedDateTime` | Timeline |
| `ZonedDateTime` | - | `Duration` | `ZonedDateTime` | Timeline |
| `ZonedDateTime` | - | `ZonedDateTime` | `Duration` | Timeline |
| `Duration` | + | `Duration` | `Duration` | Timeline |
| `Duration` | - | `Duration` | `Duration` | Timeline |
| `Duration` | * | `number` | `Duration` | Timeline |
| `Duration` | / | `number` | `Duration` | Timeline |
| `Duration` | / | `Duration` | `double` | Timeline |
| `Period` | + | `Period` | `Period` | Calendar |
| `Period` | - | `Period` | `Period` | Calendar |
| `YearMonth` | + months | `int` | `YearMonth` | Calendar |

**Critical design split:** Calendar arithmetic (Period-based) is kept strictly separate from timeline arithmetic (Duration-based). You cannot add a `Period` to an `Instant` or a `Duration` to a `LocalDate`. This separation prevents the most common temporal bugs.

---

## BCL Type Correspondence

| NodaTime Type | BCL Equivalent | Conversion Methods | Notes |
|---------------|---------------|-------------------|-------|
| `LocalDate` | `System.DateOnly` | `FromDateOnly()` / `ToDateOnly()` | .NET 6+. NodaTime adds calendar support, richer arithmetic |
| `LocalTime` | `System.TimeOnly` | `FromTimeOnly()` / `ToTimeOnly()` | .NET 6+. NodaTime adds nanosecond precision |
| `LocalDateTime` | `System.DateTime` (Unspecified) | `FromDateTime()` / `ToDateTimeUnspecified()` | NodaTime ignores `Kind` on input |
| `Instant` | `System.DateTime` (Utc) | `FromDateTimeUtc()` / `ToDateTimeUtc()` | Input must have Kind=Utc |
| `Instant` | `System.DateTimeOffset` | `FromDateTimeOffset()` / `ToDateTimeOffset()` | Output always has offset=0 |
| `OffsetDateTime` | `System.DateTimeOffset` | `FromDateTimeOffset()` / `ToDateTimeOffset()` | Most natural BCL mapping |
| `ZonedDateTime` | *(none)* | `FromDateTimeOffset()` (loses zone) | BCL has no equivalent |
| `Duration` | `System.TimeSpan` | `FromTimeSpan()` / `ToTimeSpan()` | Duration has ns precision; TimeSpan has 100ns ticks |
| `Offset` | `System.TimeSpan` | `FromTimeSpan()` / `ToTimeSpan()` | Different semantic — offset not a duration |
| `DateTimeZone` | `System.TimeZoneInfo` | `BclDateTimeZone.FromTimeZoneInfo()` | NodaTime prefers IANA/TZDB IDs |
| `Period` | *(none)* | — | BCL has no calendar-period type |
| `DateInterval` | *(none)* | — | BCL has no date-range type |
| `AnnualDate` | *(none)* | — | BCL has no recurring-date type |
| `YearMonth` | *(none)* | — | BCL has no year-month type |

---

## Serialization Story

### JSON (System.Text.Json)

NodaTime provides the `NodaTime.Serialization.SystemTextJson` NuGet package. Configuration:

```csharp
var options = new JsonSerializerOptions()
    .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
```

All types serialize to ISO 8601 / RFC 3339 patterns by default:

| Type | JSON Format | Example |
|------|------------|---------|
| `Instant` | `uuuu-MM-ddTHH:mm:ss.FFFFFFFFFZ` | `"2024-03-15T16:45:20Z"` |
| `LocalDate` | `uuuu-MM-dd` | `"2024-03-15"` |
| `LocalTime` | `HH:mm:ss.FFFFFFFFF` | `"16:45:20"` |
| `LocalDateTime` | `uuuu-MM-ddTHH:mm:ss.FFFFFFFFF` | `"2024-03-15T16:45:20"` |
| `OffsetDateTime` | RFC 3339 with offset | `"2024-03-15T16:45:20+01:00"` |
| `ZonedDateTime` | RFC 3339 + zone ID | `"2024-03-15T16:45:20+01:00 Europe/London"` |
| `Duration` | `-H:mm:ss.FFFFFFFFF` | `"1:12:34:56.123456789"` |
| `Period` | ISO 8601 duration | `"P1Y2M3DT4H5M6S"` |
| `Offset` | General pattern | `"+01"` or `"-03:30"` |
| `DateTimeZone` | Zone ID string | `"Europe/London"` |
| `Interval` | Compound object | `{ "Start": "...", "End": "..." }` |
| `OffsetDate` | ISO + offset | `"2024-03-15+01:00"` |
| `OffsetTime` | ISO + offset | `"T16:45:20+01:00"` |

### XML

Most types implement `IXmlSerializable`. Serialized forms use the same ISO patterns. Calendar systems use a `calendar` attribute for non-ISO calendars.

### Protocol Buffers

`NodaTime.Serialization.Protobuf` package provides conversions to/from Google well-known types (`Timestamp` ↔ `Instant`, `Date` ↔ `LocalDate`, `TimeOfDay` ↔ `LocalTime`, `Duration` ↔ `Duration`).

---

## Package Footprint

| Property | Value |
|----------|-------|
| Package name | `NodaTime` |
| Latest version | 3.3.1 |
| Package size | ~792 KB (.nupkg) |
| Target frameworks | .NET 8.0, .NET Standard 2.0 |
| External dependencies | **None** (zero runtime dependencies) |
| License | Apache-2.0 |
| Total NuGet downloads | ~275 million |
| Serialization packages | `NodaTime.Serialization.SystemTextJson` (separate, ~50 KB) |
| Testing support | `NodaTime.Testing` (separate, ~30 KB) — includes `FakeClock` |
| Maintenance | Actively maintained by Jon Skeet; regular releases; semver-compliant |
| Version stability | Major versions are rare (1.x → 2.x → 3.x over ~12 years). 3.x stable since 2019. |

**Dependency analysis for Precept:** NodaTime has zero transitive dependencies. Adding it to `src/Precept/` adds one NuGet package reference and ~792 KB. The serialization companion (`NodaTime.Serialization.SystemTextJson`) would be needed for MCP/JSON interop. Both packages are prefix-reserved on NuGet and Apache-2.0 licensed.

---

## Cross-Reference with Precept Proposals

### `date` (Issue #26) — Proposed Precept `date` type

**Current proposal:** `date` backed by `System.DateOnly`. ISO 8601 literal format `date("YYYY-MM-DD")`. Arithmetic: `date ± integer → date` (days), `date - date → integer` (day count).

**What changes with NodaTime `LocalDate`:**

| Aspect | `System.DateOnly` (current) | `NodaTime.LocalDate` | Impact |
|--------|---------------------------|---------------------|--------|
| Calendar support | Gregorian only | Multiple (ISO, Julian, Islamic, Hebrew, etc.) | Precept would only use ISO, but the capability exists |
| Arithmetic | `AddDays`, `AddMonths`, `AddYears` | Same + `Period` composition, `PlusWeeks`, month-length-aware truncation | Richer, but Precept's DSL surface may not need it initially |
| Range | 0001-01-01 to 9999-12-31 | -9998-01-01 to 9999-12-31 | NodaTime has wider historical range; neutral for business entities |
| Day-of-week | `DayOfWeek` (BCL enum) | `IsoDayOfWeek` (NodaTime enum, Sunday=7 per ISO) | Different enum; trivially convertible |
| Serialization | `ToString("O")` → `"YYYY-MM-DD"` | Pattern-based, ISO by default | Both produce the same string |
| Precision | Day | Day | Identical |
| Size | 4 bytes (int, day number since 0001-01-01) | ~8 bytes (int + calendar reference bits) | Slightly larger in NodaTime |
| Interop | Native .NET 6+ | Requires NodaTime package | Trade-off: dependency vs. richness |

**Key trade-off:** `DateOnly` is zero-dependency and perfectly adequate for Precept's v1 `date` proposal. `LocalDate` adds calendar support and richer arithmetic, but neither is needed for v1 business-entity modeling. The main argument for `LocalDate` is *type-system consistency* if Precept later adds `time`, `duration`, or `datetime` types — using NodaTime for all temporal types gives a unified arithmetic model and prevents the piecemeal problem (`DateOnly` + `TimeOnly` + homebrew duration ≠ coherent temporal algebra).

### `time` (deferred) — Future Precept time type

**Relevant NodaTime types:** `LocalTime` is the obvious backing type.

| Aspect | `System.TimeOnly` | `NodaTime.LocalTime` |
|--------|--------------------|----------------------|
| Precision | 100ns (ticks) | 1ns (nanoseconds) |
| Wrap semantics | Wraps at midnight | Wraps at midnight |
| Arithmetic | Add hours/minutes | Add via `Period` or `PlusXyz` methods |
| Key extras | `IsBetween(start, end)` | `Next(day)`, `Previous(day)` on parent types |

**Consequence for Precept:** If `date` ships backed by `DateOnly` and `time` later ships backed by `TimeOnly`, Precept has two independent BCL types with no unified arithmetic model. If both ship backed by NodaTime, `LocalDate + LocalTime → LocalDateTime` is a single method call, and `Period` provides the unified arithmetic bridge.

### `duration` (deferred) — Future Precept duration type

**Relevant NodaTime types:** Both `Duration` and `Period`, depending on semantics.

- `Duration` (fixed nanoseconds) is appropriate for elapsed-time calculations: "how long did this take?" or "add 30 minutes."
- `Period` (calendar-based) is appropriate for business-rule scheduling: "add 1 month" or "3 years from now."

**Precept design question (out of scope for this research, noted for Frank):** If Precept adds a `duration` type, does it mean elapsed time (→ `Duration`) or calendar interval (→ `Period`)? NodaTime's two-type split suggests these are different concepts that should not be conflated. A business-entity DSL is more likely to need `Period` semantics ("renew in 12 months") than `Duration` semantics ("elapsed time: 3,600 seconds").

### Related numeric proposals

- **`decimal` (#27):** NodaTime is irrelevant here — `System.Decimal` remains the right backing type. No overlap.
- **`integer` (#29):** NodaTime is irrelevant. `System.Int64` remains correct. However, if Precept's `date ± integer` arithmetic uses `LocalDate`, the integer day-count would need to go through `Period.FromDays(int)` rather than direct `AddDays(int)`. This is slightly more ceremony but type-safe.
- **Built-in functions (#16):** NodaTime provides rich accessor properties (`.Year`, `.Month`, `.Day`, `.DayOfWeek`, `.DayOfYear`) that map directly to the component-accessor functions proposed for `date`. If backed by `LocalDate`, these are method/property calls. If backed by `DateOnly`, they are also method/property calls. No meaningful difference for Precept's function surface.

---

## Types Definitively Out of Scope for Precept

| NodaTime Type | Why Out of Scope |
|---------------|-----------------|
| `ZonedDateTime` | Business-entity field definitions should not carry timezone rules. Timezone resolution is a presentation/integration concern, not a domain-integrity concern. Precept's determinism guarantee would be compromised by timezone-dependent calculations. |
| `OffsetDateTime` | UTC offsets are a wire-format concern. If a Precept entity needs to record "when something happened," an `Instant` or `LocalDateTime` is sufficient. The offset is metadata about the observation context, not the entity state. |
| `OffsetDate` / `OffsetTime` | Rarely used even in general .NET development. No business-entity use case. |
| `DateTimeZone` | A timezone is configuration/context, not entity data. Precept fields should not store timezone objects. |
| `CalendarSystem` | Precept should use ISO calendar exclusively. Non-ISO calendars are a localization concern, not a domain-integrity concern. |
| `IClock` | A clock is infrastructure. Precept treats "now" as an externally-supplied value, not something the DSL reads from a clock. This is consistent with Precept's determinism guarantee. |

### Types Potentially Relevant in Future

| NodaTime Type | Possible Precept Use | When |
|---------------|---------------------|------|
| `LocalDate` | Backing type for `date` | If NodaTime is adopted as a dependency |
| `LocalTime` | Backing type for future `time` | When time-of-day type is proposed |
| `LocalDateTime` | Backing type for future `datetime` | If a combined date+time type is ever needed |
| `Instant` | Backing type for future `timestamp` | If "when did this happen" semantics are needed |
| `Duration` | Backing type for future `duration` (elapsed) | If elapsed-time semantics are proposed |
| `Period` | Backing type for future `duration` (calendar) | If calendar-period semantics are proposed |
| `DateInterval` | Backing type for future date-range constraints | If "date must be within range" becomes a first-class concept |
| `AnnualDate` | Backing type for recurring-date fields | If recurring dates (birthdays, annual deadlines) become a type |
| `YearMonth` | Backing type for billing-period fields | If year-month (credit card expiry, billing cycle) becomes a type |

---

## NodaTime's Make-Invalid-States-Unrepresentable Alignment

NodaTime enforces type-level correctness that mirrors Precept's philosophy:

1. **Cannot accidentally mix local and global:** `LocalDate + Duration` is a compile error. You must choose which domain you're in.
2. **Cannot lose timezone information silently:** Converting `ZonedDateTime → Instant` is explicit — you know the timezone is dropped.
3. **Cannot create impossible dates:** `new LocalDate(2024, 2, 30)` throws `ArgumentOutOfRangeException`.
4. **Cannot add calendar periods to global types:** `Instant + Period` is a compile error — you must convert to local first, add the period, then convert back with explicit ambiguity handling.
5. **Cannot compare across calendars:** `localDateInGregorian > localDateInJulian` throws `ArgumentException`.

These constraints are exactly the kind of "prevention over detection" that Precept's philosophy demands. Adopting NodaTime would give Precept's runtime the same category of type-safety guarantees for temporal operations that NodaTime gives to general .NET code.

---

## Summary of Findings

1. **NodaTime's type model is comprehensive and well-aligned with Precept's philosophy.** The "make invalid states unrepresentable" principle, the rejection of implicit lossy conversions, and the separation of calendar vs. clock concerns all mirror Precept's design values.

2. **For the `date` proposal (#26) alone, `System.DateOnly` is sufficient.** NodaTime's `LocalDate` is richer but the additional capabilities (multi-calendar, Period arithmetic) are not needed for v1.

3. **The case for NodaTime becomes stronger if/when Precept adds `time`, `duration`, or `datetime`.** At that point, having a unified temporal type system (where `LocalDate + LocalTime = LocalDateTime`, and `Period` provides the arithmetic bridge) is significantly cleaner than wiring together `DateOnly` + `TimeOnly` + custom duration logic.

4. **The dependency cost is low.** ~792 KB, zero transitive dependencies, Apache-2.0, actively maintained, 275M+ downloads, stable API. The serialization companion for System.Text.Json is a lightweight separate package.

5. **Several NodaTime types (`ZonedDateTime`, `OffsetDateTime`, `DateTimeZone`, `CalendarSystem`, `IClock`) are definitively out of scope** for a deterministic business-entity DSL. This is a feature, not a gap — Precept should not model timezone-dependent state.

6. **NodaTime's two-type split for "elapsed time" (`Duration`) vs. "calendar interval" (`Period`) is a design insight that Precept's eventual `duration` proposal should address explicitly.** Business rules almost always mean `Period` ("renew in 12 months") not `Duration` ("elapsed 31,536,000 seconds").

---

## References

- [NodaTime user guide: Core concepts](https://nodatime.org/3.2.x/userguide/concepts)
- [NodaTime user guide: Core types quick reference](https://nodatime.org/3.2.x/userguide/core-types)
- [NodaTime user guide: Design philosophy](https://nodatime.org/3.2.x/userguide/design)
- [NodaTime user guide: Choosing between types](https://nodatime.org/3.2.x/userguide/type-choices)
- [NodaTime user guide: Arithmetic](https://nodatime.org/3.2.x/userguide/arithmetic)
- [NodaTime user guide: BCL conversions](https://nodatime.org/3.2.x/userguide/bcl-conversions)
- [NodaTime user guide: Serialization](https://nodatime.org/3.2.x/userguide/serialization)
- [NodaTime user guide: Range of valid values](https://nodatime.org/3.2.x/userguide/range)
- [NodaTime user guide: Rationale](https://nodatime.org/3.2.x/userguide/rationale)
- [NodaTime API reference (3.2.x)](https://nodatime.org/3.2.x/api/)
- [NodaTime NuGet package](https://www.nuget.org/packages/NodaTime)
- [NodaTime GitHub repository](https://github.com/nodatime/nodatime)
- Precept type-system survey: [type-system-survey.md](type-system-survey.md)
