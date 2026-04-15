# Proposal: Unified Temporal Type System — NodaTime-Aligned

**Author:** Frank (Lead/Architect, Language Designer)
**Date:** 2026-04-15 (v4 — `local*` naming, dot-accessor mediation, strict hierarchy)
**Status:** Proposal — ready for owner review
**Supersedes:** Issue #26 (`localdate` type as standalone proposal), v1 (collapsed `Period`), v2 (constructor-based literals), v3 (function-call timezone mediation)

---

## Summary

Add eight temporal types to the Precept DSL — `localdate`, `localtime`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, and `localdatetime` — backed by NodaTime as a runtime dependency. Include a single timezone mediation operation (`.inZone(tz)`) accessed via dot syntax, a unified postfix unit system for constructing temporal quantities, and single-quoted typed constants for formatted values. Together, these types, literal forms, and the dot-accessor mediation chain give domain authors the vocabulary to express calendar constraints, SLA enforcement, multi-timezone compliance rules, and elapsed-time tracking — all within the governing contract, with no temporal logic delegated to the hosting layer. Critically, the literal mechanisms established here — the typed constant delimiter (`'...'`) and the postfix quantity system (`N unit`) — are not temporal-specific. They are the language's expansion joints for all future non-primitive types. Temporal types are the gateway.

**Temporal quantity construction** uses two postfix atom forms and one combination operator:
- **Bare postfix:** `30 days`, `72 hours`, `12 months` — integer literal + unit keyword
- **Paren postfix:** `(GraceDays) days`, `(X + 5) hours` — parenthesized expression + unit keyword
- **Combination via `+`:** `2 years + 6 months + 15 days` — standard addition operator

**Typed constants** use the single-quoted `'...'` delimiter — the typed constant delimiter — with type inferred from content shape. Temporal types are the first inhabitants of this mechanism; the delimiter is not temporal-specific:
- `'2026-06-01'` (localdate), `'14:30:00'` (localtime), `'2026-04-13T14:30:00Z'` (instant), `'2026-04-13T09:00:00'` (localdatetime), `'2026-04-13T14:30:00[America/New_York]'` (zoneddatetime), `'America/New_York'` (timezone)

**Timezone mediation** uses a single dot-accessor operation — `.inZone(tz)` — that produces a `zoneddatetime`, with navigation to local types via dot chains:
- `myInstant.inZone(tz)` → `zoneddatetime`
- `myInstant.inZone(tz).localdate` → `localdate`
- `myInstant.inZone(tz).localtime` → `localtime`
- `myInstant.inZone(tz).localdatetime` → `localdatetime`
- `myLDT.inZone(tz).instant` → reverse navigation back to `instant`

**Type hierarchy (strict — no skip-level accessors):**
```
instant                              ← universal timeline point
  ↓ .inZone(tz)       ↑ .instant
zoneddatetime                        ← date + time + timezone
  ↓ .localdatetime     ↑ .inZone(tz)
localdatetime                        ← date + time (no timezone)
  ↓ .localdate  ↓ .localtime        ↑ localdate + localtime
localdate       localtime            ← date / time (no timezone)
```

Plus orthogonal value types:
```
duration    ← timeline arithmetic (hours, minutes, seconds)
period      ← calendar arithmetic (years, months, days)
timezone    ← IANA timezone identifier
```

**What changed in v4:** Six locked decisions from the 2026-04-15 session revised the type surface and timezone mediation:
1. **`local*` naming** (D10) — `date` → `localdate`, `time` → `localtime`, `datetime` → `localdatetime`. Returns to v1 naming convention, explicitly communicating "no timezone" in the type name — the same naming strategy NodaTime uses (`LocalDate`, `LocalTime`, `LocalDateTime`).
2. **Dot-vs-function rule** (D8) — "If the value owns it, dot. If the operation is freestanding, function." `inZone` is a dot accessor because the instant owns the mediation to a timezone. Component accessors (`.year`, `.localdate`, etc.) are dot because the value owns its components.
3. **`inZone` as dot accessor** (D11) — One mediation operation, accessed via dot: `myInstant.inZone(tz)`. Replaces three standalone functions (`toLocalDate`, `toLocalTime`, `toInstant`). Navigation via dot chains: `.localdate`, `.localtime`, `.localdatetime`.
4. **Strict hierarchy** (D12) — No skip-level accessors. `instant.localdate` is a compile error — must go `instant.inZone(tz).localdate`. Forces explicit timezone mediation at every boundary crossing.
5. **`pin` eliminated** (D13) — Reverse navigation from `localdatetime` → `instant` uses `ldt.inZone(tz).instant`. One mediation operation in both directions. `pin` was redundant with dot-chain composition.
6. **Instant restricted semantics** (D18) — `instant` supports comparison, duration arithmetic, and `.inZone(tz)` navigation — NO component accessors, no period arithmetic. An instant is a UTC timeline point with no calendar context.

**What changed in v3:** Four locked decisions from the earlier 2026-04-15 session rewrote the literal surface:
1. **Unified postfix model** — All 7 function-call constructors (`days()`, `months()`, `hours()`, etc.) eliminated. Postfix is the sole quantity construction syntax.
2. **`+` as sole combiner** — Composite juxtaposition (`2 years 6 months`) eliminated. Only `2 years + 6 months`.
3. **No duration/period constructor literals** — `duration(PT72H)` and `period(P1Y6M)` eliminated. Postfix units are the only surface.
4. **Single-quoted typed constants** — `date(2026-01-15)` constructor form replaced by `'2026-01-15'`. All 6 formatted temporal types use the `'...'` typed constant delimiter with type inferred from content shape.

**What changed in v2:** Shane's owner directive: *"No obscurity, expose NodaTime. Don't reinvent the wheel. Force authors to think about the details."* The v1 proposal collapsed `Period` and `Duration` into a single surface type (`duration`) with custom dispatch rules. This was wrong. NodaTime deliberately keeps `Period` and `Duration` as separate types because they represent fundamentally different quantities — calendar distance vs. timeline distance. The v2 proposal exposes this distinction faithfully: `period` and `duration` are separate surface types, calendar units resolve to `period`, timeline units resolve to `duration`, and the type checker inherits NodaTime's enforcement for free. No custom operator dispatch. No re-inventing the wheel.

NodaTime exists because `System.DateTime` lets you be implicit about what your temporal data means. Precept exists because scattered service-layer code lets you be implicit about what your business rules mean. Both libraries are responses to the same failure mode: **implicit behavior creates bugs; explicit behavior creates predictability.**

This shared philosophy is the lens through which every type decision in this proposal is made:

| Precept applies it to... | NodaTime applies it to... | The shared principle |
|---|---|---|
| `localdate` over `string` | `LocalDate` over `DateTime` | Be explicit that this is a calendar date |
| `instant` over `number` | `Instant` over `DateTime` | Be explicit that this is a point on the timeline |
| `timezone` over `string` | `DateTimeZone` over `string` | Be explicit about the allowed values |
| `duration` over `integer` | `Duration` over `TimeSpan` | Be explicit about what fixed-length units mean |
| `period` over `integer` | `Period` over `int monthCount` | Be explicit that calendar units have variable length |
| `localtime` over `integer` | `LocalTime` over `int minutesSinceMidnight` | Be explicit that this is a time of day |
| `localdatetime` over `string` | `LocalDateTime` over `DateTime` | Be explicit about combined date+time without timezone |

Precept's design principles ground this directly:

- **Principle #1 (Deterministic, inspectable model):** NodaTime's type separation makes it structurally clear which operations are deterministic and which require external context. All types proposed here are deterministic by construction — `instant` comparison is nanosecond math, `localdate` arithmetic uses the fixed ISO calendar, and the `.inZone(tz)` dot accessor makes the TZ database input explicit in the expression.
- **Principle #2 (English-ish but not English):** The DSL names — `localdate`, `localtime`, `instant`, `duration`, `period`, `timezone` — communicate exactly what the data is. The `local` prefix explicitly signals "no timezone" — the same naming strategy NodaTime uses. `field FiledAt as instant` needs no comment. Postfix units extend this: `DueDate + 30 days` reads as domain prose.
- **Principle #8 (Sound, compile-time-first static analysis):** NodaTime's type separation enables the compiler to catch temporal misuse statically. `localdate + instant` is a type error. `instant + period` is a type error. `instant.year` is a compile error. Crucially, the `period`/`duration` split means the compiler rejects `instant + 1 month` because `months` always resolves to `period` and instants only accept `duration` — NodaTime's enforcement inherited for free.
- **Principle #12 (AI is a first-class consumer):** Named temporal types with precise semantics give AI consumers a vocabulary to reason about entity data and generate correct precepts. An AI that sees `field DueDate as localdate` knows the field supports calendar arithmetic — it does not need to infer this from naming conventions on a `string` field.

The governing question for every decision: **"If a domain author has this kind of data, does giving it a named type help them be explicit about what it means?"**

### Temporal types as the gateway beyond primitives

Precept's type system today consists of primitives: `string`, `number`, `integer`, `decimal`, `boolean`. Temporal types are the **first step beyond primitives** — the gateway to a richer type vocabulary that lets domain authors express what their data *is*, not just what storage shape it occupies. The literal mechanisms established in this proposal — single-quoted typed constants (`'...'`) and postfix quantities (`N unit`) — are not temporal features. They are the language's **expansion joints** for all future non-primitive types. Every design decision here carries weight beyond temporal: the admission rule for typed constants, the context-dependent resolution for postfix units, and the zero-constructor discipline all set precedent for how the language grows.

### The NodaTime alignment directive

Shane's directive (2026-04-14): *"No obscurity, expose NodaTime. Someone way smarter than us designed NodaTime. Don't try to re-invent the wheel."*

This means:
1. When NodaTime keeps two concepts separate, Precept keeps them separate.
2. When NodaTime exposes a distinction, Precept exposes it.
3. When NodaTime's type system prevents a nonsensical operation, Precept inherits that prevention.
4. When NodaTime has no natural ordering on a type, Precept has no natural ordering on that type.
5. When NodaTime uses structural equality, Precept uses structural equality.

The v1 proposal violated all five. It collapsed `Period` into `Duration`, hid the calendar/timeline distinction behind custom dispatch, and required Precept to implement its own unit-compatibility enforcement. The v2 proposal trusts NodaTime's design and exposes it.

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

**After** — with temporal types (v4: postfix units + single-quoted literals + `local*` naming):

```precept
field DueDate as localdate default '2026-06-01'
field GracePeriod as period default 30 days

# Type-safe: DueDate + MealsTotal is a compile error
# Explicit: 30 days resolves to period, localdate + period → localdate
invariant DueDate + GracePeriod >= '2026-01-01' because "Within grace period"
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
invariant FiledAt - IncidentAt <= 72 hours because "HIPAA: must file within 72 hours of incident"
```

**Before** — multi-timezone compliance pushes logic to hosting layer:

```precept
field FilingDeadline as number  # Pre-computed by hosting layer
# The MEANING of this deadline (30 days, midnight, incident timezone) is NOT in this file
invariant FiledAt <= FilingDeadline because "Filing deadline has passed"
```

**After** — complete rule in the contract (v4: dot-accessor mediation, `localdate + period → localdate`):

```precept
field IncidentTimestamp as instant
field FiledTimestamp as instant
field IncidentTimezone as timezone

# Dot-chain mediation: instant → .inZone(tz) → zoneddatetime → .localdate → localdate
# Then calendar arithmetic, reconstruct back via .inZone(tz).instant
invariant FiledTimestamp <= (
    IncidentTimestamp.inZone(IncidentTimezone).localdate + 30 days + '23:59:00'
).inZone(IncidentTimezone).instant because "Claim must be filed by 11:59 PM local time on the 30th day after the incident"
```

**Before** — encoding a loan term as a bare integer:

```precept
field TermLengthMonths as integer default 12
field StartDate as string  # "2026-01-15" — but the compiler doesn't know that
# What does StartDate + TermLengthMonths even mean? The compiler can't help.
```

**After** — with `period` as a first-class type:

```precept
field LoanTerm as period default 12 months
field StartDate as localdate default '2026-01-15'

# localdate + period → localdate. The period carries its calendar semantics.
# 12 months != 365 days — NodaTime's truth, exposed faithfully.
invariant StartDate + LoanTerm >= '2026-01-01' because "Maturity date must be in the future"
```

The second form satisfies the philosophy's "one file, complete rules" guarantee. An auditor reads the precept and sees the entire business rule — 30 days, 11:59 PM, incident timezone.

### What happens if we don't build this

- 56 calendar date fields remain encoded as strings or numbers. The compiler cannot distinguish a date from a price.
- SLA and compliance timing rules stay in the hosting layer. The contract has a visible gap in its primary target domains (insurance, healthcare, finance).
- Day-counter simulation events remain as boilerplate in 3+ samples.
- The "one file, complete rules" philosophy claim is undermined for any domain with temporal constraints.
- The literal mechanism framework (typed constant delimiter, postfix quantity system) that would serve all future non-primitive types has no proving ground — future type proposals would each need to invent their own syntax.

---

## NodaTime as Backing Library

### Why NodaTime, not System.DateOnly / TimeOnly / DateTime

NodaTime is adopted as a runtime dependency for the entire temporal type system. The DSL author never sees NodaTime type names — `field DueDate as localdate` is the surface, `NodaTime.LocalDate` is the implementation, just as `field Amount as decimal` has `System.Decimal` behind it.

**Rationale:**

1. **Philosophy alignment.** NodaTime's core design philosophy is: *"We want to force you to think about decisions you really need to think about. In particular, what kind of data do you really have and really need?"* (NodaTime 3.3.x Design Philosophy). Distinct types for distinct temporal concepts — `LocalDate` is not `Instant` is not `Period` is not `Duration`. This is Precept's prevention guarantee applied to temporal data.

2. **Type separation enables compile-time safety.** NodaTime makes it structurally impossible to mix a calendar date with a global timestamp, or a calendar period with a timeline duration. Precept's type checker inherits this separation: `localdate + instant` is a type error. `instant + period` is a type error. No custom dispatch needed.

3. **Battle-tested arithmetic.** `LocalDate.PlusDays(int)`, `LocalDate.Plus(Period.FromMonths(n))`, month-end truncation, leap-year handling, `Period.Between(d1, d2)` — all rigorously tested since 2012. Building the same guarantees on `System.DateOnly` would require Precept to own temporal arithmetic correctness, a domain Precept has no expertise in.

4. **The `Period`/`Duration` split is the decisive factor.** The v1 proposal collapsed `Period` into `Duration` and had to build custom unit-compatibility dispatch. This was re-inventing enforcement logic that NodaTime already provides through type separation. The v2+ proposal exposes the separation directly: calendar units resolve to `period`, timeline units resolve to `duration`, and the type checker simply checks `localdate + period ✓`, `instant + duration ✓`, `instant + period ✗`. Zero custom dispatch.

5. **Coherent future path.** `LocalDate`, `LocalTime`, `LocalDateTime`, `Instant`, `Duration`, `Period`, `DateTimeZone` — the entire temporal vocabulary maps from NodaTime types with consistent semantics.

**Dependencies added:**
- `NodaTime` (core library)
- `NodaTime.Serialization.SystemTextJson` (JSON serialization for MCP tools)

**Decision format:**
- **Why:** NodaTime's type model matches Precept's philosophy; the `Period`/`Duration` split eliminates custom dispatch logic; battle-tested arithmetic avoids reimplementing solved problems.
- **Alternatives rejected:** `System.DateOnly`/`TimeOnly` — cheaper for v1 but no `Period` equivalent; month/year arithmetic requires `DateTime` conversion. Raw `System.DateTime` — conflates concepts; the exact problem NodaTime solved. Collapsed `Period`/`Duration` surface (v1 approach) — requires custom dispatch, re-invents enforcement NodaTime provides for free, contradicts the "expose NodaTime faithfully" directive.
- **Precedent:** NRules inherits `System.Decimal` from .NET for exact arithmetic; Precept inherits NodaTime's temporal model for the same reason.
- **Tradeoff accepted:** Additional NuGet dependency (~1.1 MB). Acceptable — NodaTime is authored by Jon Skeet, stable since 2012, SemVer-compliant, used in production at Google and across the .NET ecosystem.

---

## Proposed Types

### `localdate`

**What it makes explicit:** This is a calendar date — not a timestamp, not a string that looks like a date. Day-granularity arithmetic is meaningful. "2026-03-15" means the same calendar day everywhere. The `local` prefix explicitly signals: no timezone.

**Backing type:** `NodaTime.LocalDate`

**Declaration:**

```precept
field DueDate as localdate default '2026-06-01'
field FilingDate as localdate nullable
field ContractEnd as localdate default '2099-12-31'
```

**Single-quoted literal:** `'2026-03-15'` — the single-quote delimiter signals a typed constant. Type is inferred from content shape: `YYYY-MM-DD` (digits and hyphens, no `T`) → `localdate`. The date's content shape is what qualifies it for the typed constant delimiter — distinguishable from every other inhabitant. Content is validated at compile time. `'2026-03-15'` is valid. `'03/15/2026'` is a compile error with a teachable message. See Locked Decision #18.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `localdate + period` | `localdate` | Calendar arithmetic. `LocalDate.Plus(Period)`. Handles truncation. |
| `localdate - period` | `localdate` | Calendar arithmetic backward. `LocalDate.Minus(Period)`. |
| `localdate + 30 days` | `localdate` | Postfix unit — `30 days` resolves to `period` in localdate context. |
| `localdate + (GraceDays) days` | `localdate` | Paren postfix — variable expression resolves to `period` in localdate context. |
| `localdate + 3 months` | `localdate` | Truncates at month end (Jan 31 + 1 month = Feb 28). |
| `localdate + 1 year` | `localdate` | Leap years (Feb 29 + 1 year = Feb 28). |
| `localdate + 2 weeks` | `localdate` | = 14 days. |
| `localdate - localdate` | `period` | Calendar distance. `Period.Between(d1, d2)`. Preserves structural components. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering. ISO calendar only. |

| **Not supported** | **Why** |
|---|---|
| `localdate + localdate` | Adding two dates is meaningless. |
| `localdate + integer` | Bare integers don't carry unit semantics. Use `localdate + (n) days`. See Locked Decision #10. |
| `localdate + decimal` / `localdate + number` | Fractional/floating-point days are meaningless at day granularity. |
| `localdate + duration` | **Duration is timeline-only (hours, minutes, seconds). Localdate is calendar-only (days, months, years).** Mixing them is what NodaTime was designed to prevent. `localdate + 3 hours` makes no sense — a date has no time component. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.year` | `integer` | Calendar year |
| `.month` | `integer` | Month (1–12) |
| `.day` | `integer` | Day of month (1–31) |
| `.dayOfWeek` | `integer` | ISO day of week (Monday=1, Sunday=7) |

**Constraints:** `nullable`, `default '...'` (single-quoted date). Numeric constraints (`nonnegative`, `min`, `max`, `maxplaces`, `minlength`, `maxlength`) are compile errors on `localdate`.

**Serialization:** ISO 8601 string: `"2026-03-15"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `field X as localdate default "2026-01-01"` | Date defaults require a single-quoted typed constant: `default '2026-01-01'`. Double-quoted strings are not dates. |
| `'2026-02-30'` | Invalid date: February 30 does not exist. Use a valid calendar date in ISO 8601 format (YYYY-MM-DD). |
| `'01/15/2026'` | Invalid date format: expected ISO 8601 (YYYY-MM-DD), got '01/15/2026'. Use `'2026-01-15'`. |
| `DueDate + FilingDate` | Cannot add two dates. Did you mean `DueDate - FilingDate` (period between dates) or `DueDate + (n) days` (offset)? |
| `DueDate + 2` | Cannot add an integer to a date. Temporal arithmetic requires explicit units. Use `DueDate + 2 days` to add 2 calendar days. |
| `DueDate + 3 hours` | Cannot add a duration to a date — dates have no time-of-day component. Use `DueDate + (n) days` for calendar offsets. If you need time-of-day arithmetic, use `localdatetime` or `instant`. |

---

### `localtime`

**What it makes explicit:** This is a time of day — not a duration, not a timestamp, not an integer encoding minutes-since-midnight. The `local` prefix explicitly signals: no timezone.

**Backing type:** `NodaTime.LocalTime`

**Declaration:**

```precept
field AppointmentTime as localtime default '09:00:00'
field CheckInTime as localtime nullable
```

**Single-quoted literal:** `'14:30:00'` — content shape `HH:mm:ss` (colons without hyphens-before-T) → `localtime`. The time's content shape is what qualifies it for the typed constant delimiter — distinguishable from all other inhabitants. Seconds may be omitted: `'14:30'` is valid (implies `:00`). See Locked Decision #18.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `localtime + period` | `localtime` | `LocalTime.Plus(Period)`. Uses time components (hours, minutes, seconds); date components (years, months, weeks, days) are ignored. NodaTime native. |
| `localtime + duration` | `localtime` | Sub-day bridging. Wraps at midnight. Runtime: nanosecond arithmetic on `LocalTime` (see Decision #16). For sub-day units, `duration` and `period` represent identical physical quantities — the type checker bridges this. |
| `localtime + 3 hours` | `localtime` | Postfix unit — resolves via sub-day bridging. Wraps at midnight. |
| `localtime + 30 minutes` | `localtime` | Same bridging. Wraps at midnight. |
| `localtime + 45 seconds` | `localtime` | Same bridging. Wraps at midnight. |
| `localtime - localtime` | `period` | Time-component period between two times. `Period.Between(t1, t2)` returns period with `.hours`, `.minutes`, `.seconds` components. NodaTime faithful. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering within a day. |

**Note:** `localtime + period` uses only the period's time components. Date components are silently ignored — this is `LocalTime.Plus(Period)` behavior in NodaTime. `localtime + 5 days` is valid but a no-op. `localtime + 3 months + 2 hours` adds only the 2 hours.

| **Not supported** | **Why** |
|---|---|
| `localtime + integer` | What unit? Use `(n) hours` or `(n) minutes`. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.hour` | `integer` | Hour (0–23) |
| `.minute` | `integer` | Minute (0–59) |
| `.second` | `integer` | Second (0–59) |

**Constraints:** `nullable`, `default '...'` (single-quoted time).

**Serialization:** ISO 8601 time string: `"14:30:00"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `'25:00:00'` | Invalid time: hour must be 0–23, got 25. |
| `AppointmentTime + 30` | Cannot add an integer to a time. Use `localtime + 30 minutes` or `localtime + 1 hour` to specify the unit. |
| `AppointmentTime + 1 day` | Warning: Adding a date-only period to a time has no effect — date components are ignored. Did you mean `AppointmentTime + 1 hour`, or did you intend `localdatetime` + `1 day` arithmetic? |

---

### `instant`

**What it makes explicit:** This is a point on the global timeline — UTC, no timezone ambiguity. Not a date, not "seconds since epoch" encoded as a number.

**Backing type:** `NodaTime.Instant`

**Declaration:**

```precept
field FiledAt as instant nullable
field IncidentTimestamp as instant
```

**Single-quoted literal:** `'2026-04-13T14:30:00Z'` — content shape includes `T` and trailing `Z` → `instant`. The trailing `Z` is the distinguishing shape signal that qualifies `instant` for the typed constant delimiter. Without `Z`: compile error. See Locked Decision #18.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `instant - instant` | `duration` | Elapsed time between two points. Pure nanosecond subtraction. |
| `instant + duration` | `instant` | Point offset forward. `Instant.Plus(Duration)`. |
| `instant - duration` | `instant` | Point offset backward. `Instant.Minus(Duration)`. |
| `instant + 3 days` | `instant` | Postfix unit — `3 days` resolves to `Duration.FromDays(3)` in instant context. |
| `instant + (DayCount) days` | `instant` | Paren postfix — variable expression resolves to `Duration.FromDays(n)` in instant context. |
| `instant + 72 hours` | `instant` | Postfix unit — `72 hours` resolves to `Duration.FromHours(72)` in instant context. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering — nanosecond comparison. |

| **Not supported** | **Why** |
|---|---|
| `instant.year`, `.month`, `.hour`, etc. | **Compile error.** Extracting calendar components requires a timezone — implicit behavior hiding a timezone dependency. Use `myInstant.inZone(tz).localdate.year`. Strict hierarchy enforces explicit timezone mediation (Decision #22). |
| `instant.localdate`, `instant.localtime` | **Compile error.** Skip-level accessors violate the strict hierarchy. Must go `instant.inZone(tz).localdate`. Forces explicit timezone mediation at every boundary crossing (Decision #22). |
| `instant + integer` | Ambiguous unit. Use `instant + (n) hours` or `instant + (n) seconds`. |
| `instant + period` | **Periods are calendar units, instants live on the timeline.** "Add 1 month to an instant" is undefined — months have variable nanosecond length. Convert to a localdate via `myInstant.inZone(tz).localdate`, add the month, navigate back via `.inZone(tz).instant`. NodaTime's `Instant` accepts only `Duration`, not `Period`. |
| `instant + 3 months` | `months` always resolves to `period` regardless of expression context. `instant + period` is a type error. Same applies to `(n) months`, `(n) years`. |

**Note on `days`/`weeks` in instant context:** `instant + 3 days` and `instant + (n) days` are both valid — `days` and `weeks` resolve to `duration` (`Duration.FromDays`) in instant context. This is the key capability of context-dependent type resolution: the same unit keyword (`days`) resolves to `period` in calendar contexts and `duration` in timeline contexts. `months` and `years` have no duration equivalent (no `Duration.FromMonths` in NodaTime), so they always resolve to `period`.

**Navigation via `.inZone(tz)` (the sole mediation path):**

| Expression | Produces | Description |
|---|---|---|
| `instant.inZone(tz)` | `zoneddatetime` | "View this moment in this timezone." The single mediation operation. |
| `instant.inZone(tz).localdate` | `localdate` | Calendar date at this moment in this timezone. |
| `instant.inZone(tz).localtime` | `localtime` | Time of day at this moment in this timezone. |
| `instant.inZone(tz).localdatetime` | `localdatetime` | Combined local date+time in this timezone. |
| `instant.inZone(tz).year` | `integer` | Calendar year — same as `.localdate.year`. |

**Accessors:** `.inZone(tz)` only. No component accessors. Deliberately empty — to get calendar components, navigate via `.inZone(tz)`.

**Constraints:** `nullable`, `default '...'` (single-quoted instant).

**Serialization:** ISO 8601 UTC string: `"2026-04-13T14:30:00Z"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `'2026-04-13T14:30:00'` in instant context | Instant requires UTC designation. Use `'2026-04-13T14:30:00Z'` (note trailing Z). Without Z, this is a `localdatetime`, not an `instant`. |
| `FiledAt.year` | Cannot access calendar components on an instant — this requires a timezone. Use `FiledAt.inZone(tz).localdate.year` to extract the year in a specific timezone. |
| `FiledAt.localdate` | Cannot access `.localdate` directly on an instant — this skips timezone mediation. Use `FiledAt.inZone(tz).localdate` to get the date in a specific timezone. |
| `FiledAt + 3600` | Cannot add an integer to an instant. Use `FiledAt + 1 hour` or `FiledAt + 3600 seconds`. |
| `FiledAt + 1 month` | Cannot add a period to an instant — periods are calendar units with variable length. Convert to a date first: `FiledAt.inZone(tz).localdate + 1 month`, then navigate back with `.inZone(tz).instant`. |

---

### `duration`

**What it makes explicit:** This is an elapsed amount of time — a fixed count of nanoseconds on the timeline. Not a calendar interval, not an hour-count encoded as an integer.

**Backing type:** `NodaTime.Duration`

**What v2 changed:** In v1, `duration` held both calendar units (months, years) and timeline units (hours, minutes) — requiring custom dispatch. In v2+, `duration` holds **only timeline units**. Calendar units live in `period`. The type checker handles the rest.

**Construction via postfix units:**

```precept
72 hours              # 72 hours as a duration
30 minutes            # 30 minutes
3600 seconds          # 3600 seconds
(SlaHours) hours      # variable — paren postfix form
```

These resolve to `Duration.FromHours`, `Duration.FromMinutes`, `Duration.FromSeconds` respectively.

**Combined durations:**

```precept
5 hours + 30 minutes + 15 seconds    # 5 hours, 30 minutes, 15 seconds
```

**Duration and period have no constructor literal form** (Locked Decision — 2026-04-15). ISO 8601 duration notation (`PT72H`, `PT8H30M`) is specialist serialization syntax designed for machines, not humans. The postfix unit system with `+` combination handles every duration expressible in ISO 8601:

| ISO notation | Precept equivalent |
|---|---|
| `PT72H` | `72 hours` |
| `PT2H30M` | `2 hours + 30 minutes` |
| `PT0.5S` | `(0.5) seconds` |

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `duration + duration` | `duration` | Combined elapsed time. |
| `duration - duration` | `duration` | Difference. |
| `duration * integer` or `duration * number` | `duration` | Scaling. NodaTime: `Duration * long`, `Duration * double`. |
| `duration / integer` or `duration / number` | `duration` | Scaling. |
| `duration / duration` | `number` | Ratio (e.g., how many shifts fit). |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering — nanosecond comparison. |

| **Not supported** | **Why** |
|---|---|
| `duration * duration` | Dimensionally meaningless. |
| `duration * decimal` | `decimal → double` is lossy. Use `number`. |
| `integer * duration` / `number * duration` | Duration is the left operand convention. |
| `duration + period` / `duration - period` | **Cannot mix timeline and calendar units.** `3 hours + 1 month` is nonsensical. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.totalHours` | `number` | Total elapsed hours (may be fractional). |
| `.totalMinutes` | `number` | Total elapsed minutes. |
| `.totalSeconds` | `number` | Total elapsed seconds. |

**As a declared field type:**

```precept
field EstimatedHours as duration default 8 hours
field ActualHours as duration default 0 hours
```

9 fields across 6 samples (MTBF, repair hours, work hours) are naturally `duration`.

**Constraints:** `nullable`, `default 8 hours` or `default 30 minutes`, `nonnegative`.

**Serialization:** ISO 8601 time-duration string: `"PT72H"`.

---

### `period`

**What it makes explicit:** This is a calendar-and-time quantity backed by the full `NodaTime.Period` type. Calendar components (years, months, weeks, days) have variable length — "1 month" means 1 month, not 30 days. Time components (hours, minutes, seconds) are structurally preserved alongside date components. A period can hold date-only, time-only, or mixed date+time components.

**Backing type:** `NodaTime.Period`

**Why this type exists (v2 → v2.1 evolution):** Shane's directive: *"Someone way smarter than us designed NodaTime. Don't try to re-invent the wheel."* NodaTime deliberately separates `Period` (variable-length calendar+time units) from `Duration` (fixed nanoseconds on the timeline). The v1 proposal collapsed this distinction. The v2 proposal exposed the separation but restricted `period` to date-only components. The v2.1 correction removes the date-only restriction because it contradicted NodaTime in three ways: (1) `LocalTime.Plus(Period)` is NodaTime's API for time arithmetic — but date-only `period` couldn't carry time results; (2) `Period.Between(LocalTime, LocalTime)` returns time-component Periods with no home in a date-only type; (3) timeline constructors returning `duration` plus `LocalTime` not accepting `Duration` left no faithful implementation path for `time + 3 hours`.

NodaTime's `Period` type is intentionally comprehensive: `Period.FromHours()`, `Period.FromMinutes()`, `Period.FromSeconds()` all exist alongside `Period.FromDays()`, `Period.FromMonths()`, `Period.FromYears()`. `Period.HasDateComponent` and `Period.HasTimeComponent` introspection properties exist. The full Period is the faithful exposure.

The key insight from NodaTime's design philosophy: *"Some of these periods represent different lengths of time depending on what they're applied to."* (NodaTime 3.3.x Core Concepts). A `Period` of 1 month added to January 31 gives February 28; added to March 1 gives April 1. A `Duration` of 30 days is always exactly 2,592,000,000,000,000 nanoseconds. These are fundamentally different concepts.

**Construction via postfix units:**

```precept
30 days               # 30 calendar days
3 months              # 3 months — NOT 90 days
1 year                # 1 year — NOT 365 days
2 weeks               # 2 weeks (= 14 days)
(GraceDays) days      # variable — paren postfix form
```

These resolve to `Period.FromDays`, `Period.FromMonths`, `Period.FromYears`, `Period.FromWeeks` respectively.

**Combined periods:**

```precept
1 year + 6 months               # 1 year and 6 months
3 months + 15 days              # 3 months and 15 days
```

**Period has no constructor literal form** (Locked Decision — 2026-04-15). ISO 8601 period notation (`P1Y6M`, `P2W`) is specialist serialization syntax. The postfix unit system with `+` combination handles every period expressible in ISO 8601:

| ISO notation | Precept equivalent |
|---|---|
| `P1Y6M` | `1 year + 6 months` |
| `P1Y6M15D` | `1 year + 6 months + 15 days` |
| `P30D` | `30 days` |
| `PT5H` | `5 hours` (in period field context) |
| `P1DT12H` | `1 day + 12 hours` |

**Decision: Precept's `period` is the full NodaTime `Period` — date AND time components (v2.1 correction).** The v2 proposal restricted `period` to date-only components. This created a structural contradiction with NodaTime's `LocalTime` API: `LocalTime.Plus(Period)` is the native arithmetic method, and `Period.Between(LocalTime, LocalTime)` returns time-component periods. The date-only restriction forced `localtime` arithmetic through `duration` via a non-existent `LocalTime.Plus(Duration)` method. The full NodaTime `Period` resolves all three contradictions and honors the "expose NodaTime faithfully" directive. See Locked Decision #13 (revised).

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `period + period` | `period` | Combined calendar period. `Period + Period`. |
| `period - period` | `period` | Calendar period difference. `Period - Period`. |
| `-period` | `period` | Unary negation. `-(3 months)` is equivalent to negating the period. |
| `==`, `!=` | `boolean` | **Structural equality.** `1 month != 30 days` — structurally different. NodaTime's equality is structural. |

| **Not supported** | **Why** |
|---|---|
| `<`, `>`, `<=`, `>=` | **No natural ordering.** "Is 1 month greater than 30 days?" depends on which month. NodaTime's `Period` has no `IComparable`. See Locked Decision #14. |
| `period * integer` | No multiplication operator on NodaTime's `Period`. Construct directly: `3 months` not `1 month * 3`. |
| `period + duration` / `period - duration` | **Cannot mix calendar and timeline units.** |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.years` | `integer` | Years component (structural, not normalized) |
| `.months` | `integer` | Months component |
| `.weeks` | `integer` | Weeks component |
| `.days` | `integer` | Days component |
| `.hours` | `integer` | Hours component |
| `.minutes` | `integer` | Minutes component |
| `.seconds` | `integer` | Seconds component |

Note: These are structural components. `14 months` has `.months == 14` and `.years == 0`. The period is NOT automatically normalized. This matches NodaTime: *"Period is NOT normalized by default."* (NodaTime 3.3.x Arithmetic).

**Introspection:**

| Property | Returns | Description |
|---|---|---|
| `.hasDateComponent` | `boolean` | `true` if any of years, months, weeks, days is non-zero |
| `.hasTimeComponent` | `boolean` | `true` if any of hours, minutes, seconds is non-zero |

These map to NodaTime's `Period.HasDateComponent` and `Period.HasTimeComponent`.

**As a declared field type:**

```precept
field GracePeriod as period default 30 days
field LoanTerm as period default 12 months
field WarrantyLength as period default 2 years
field NoticePeriod as period default 2 weeks
field ExtendedWarranty as period default 2 years + 6 months
```

10 period fields across 7 samples (`GracePeriodDays`, `TermLengthMonths`, `WarrantyMonths`, etc.) are currently `integer` surrogates. See Locked Decision #12.

**Constraints:** `nullable`, `default 30 days` / `default 12 months` / `default 2 years` / `default 2 weeks`, or combined: `default 2 years + 6 months`.

**Serialization:** ISO 8601 period string: `"P1Y2M3D"`, `"PT5H30M"`, `"P1MT2H"`. Via `PeriodPattern.Roundtrip`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `1 month > 30 days` | Cannot compare periods with `<`, `>`, `<=`, `>=` — "is 1 month greater than 30 days?" depends on which month. Periods support `==` and `!=` only (structural equality). |
| `1 month == 30 days` | Evaluates to `false` — 1 month is structurally different from 30 days. NodaTime's period equality is structural, not calendrical. |
| `FiledAt + 1 month` | Cannot add a period to an instant — periods may contain calendar units with variable length. Convert to a date first: `FiledAt.inZone(tz).localdate + 1 month`, then navigate back. |
| `GracePeriod * 2` | Cannot multiply a period. Construct the value directly: `60 days` instead of `30 days * 2`. |

---

### `timezone`

**What it makes explicit:** This is a valid IANA timezone identifier — not an arbitrary string.

**Backing type:** `NodaTime.DateTimeZone`

**Declaration:**

```precept
field IncidentTimezone as timezone
field CustomerTimezone as timezone nullable
```

**Single-quoted literal:** `'America/New_York'` — content shape is an IANA timezone identifier (forward-slash-separated components, no ISO date characters) → `timezone`. The `Word/Word` pattern is what qualifies `timezone` for the typed constant delimiter — distinguishable from all other inhabitants. Validated at compile time against the IANA TZ database bundled with NodaTime. See Locked Decision #18.

**Operators:** `==`, `!=` only. No ordering, no arithmetic.

**Accessors:** None.

**Validation:**
- **Compile-time:** Literal strings validated against IANA TZ database bundled with NodaTime. Deprecated aliases produce warnings.
- **Runtime:** Event arguments typed `as timezone` validated at fire time.

**Constraints:** `nullable`. No `default`.

**Serialization:** IANA identifier string: `"America/Los_Angeles"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `'EST'` in timezone context | Warning: "EST" is a timezone abbreviation, not an IANA identifier. Did you mean `'America/New_York'`? |
| `'Pacific Standard Time'` in timezone context | "Pacific Standard Time" is a Windows timezone name, not an IANA identifier. Use `'America/Los_Angeles'`. |
| `'Not/A/Timezone'` | Unknown IANA timezone identifier. |

---

### `zoneddatetime`

**What it makes explicit:** A localdatetime with timezone context — an instant resolved to local date and time in a specific timezone.

**Backing type:** `NodaTime.ZonedDateTime`

**Declaration:**

```precept
field IncidentContext as zoneddatetime
field FilingContext as zoneddatetime nullable
```

**Single-quoted literal:** `'2026-04-13T14:30:00[America/New_York]'` — content shape includes `T` and bracket-enclosed timezone `[...]` → `zoneddatetime`. The bracket-enclosed timezone `[...]` is the distinguishing shape signal. See Locked Decision #18.

**Construction via `.inZone(tz)` dot accessor:**

```precept
# From instant — "view this moment in this timezone"
-> set Context = FiledAt.inZone(IncidentTimezone)

# From localdatetime — "anchor this local reading to the timeline in this timezone"
-> set PinnedContext = DetectedAt.inZone(IncidentTimezone)
```

No standalone construction function exists. `zoneddatetime` is always reached via `.inZone(tz)` from either an `instant` or a `localdatetime`.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `zoneddatetime + duration` | `zoneddatetime` | Timeline arithmetic on underlying instant. |
| `zoneddatetime - duration` | `zoneddatetime` | Timeline arithmetic backward. |
| `zoneddatetime + 3 hours` | `zoneddatetime` | Postfix unit — resolves to `duration`. |
| `zoneddatetime + 3 days` | `zoneddatetime` | Postfix unit — `days` resolves to `duration` in zoneddatetime context (timeline arithmetic). |
| `zoneddatetime - zoneddatetime` | `duration` | Instant subtraction. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Comparison by underlying instant. |

| **Not supported** | **Why** |
|---|---|
| `zoneddatetime + period` | **Calendar arithmetic on `zoneddatetime` requires decomposition.** NodaTime's `ZonedDateTime` deliberately excludes `Period` arithmetic — forces explicit conversion. Navigate to `.localdate`, add the period, reconstruct via `.inZone(tz)`. |
| `zoneddatetime + 3 months` | `months` always resolves to `period`. Same rule. |
| `zoneddatetime + integer` | Bare integers don't carry unit semantics. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.instant` | `instant` | The underlying UTC point. |
| `.timezone` | `timezone` | The bound IANA timezone. |
| `.localdatetime` | `localdatetime` | Local date+time in bound timezone. |
| `.localdate` | `localdate` | Local calendar date in bound timezone. |
| `.localtime` | `localtime` | Local time in bound timezone. |
| `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`, `.dayOfWeek` | `integer` | Local components in bound timezone. |

**Constraints:** `nullable`. No `default`.

**Serialization:** Two-property JSON: `{ "instant": "..Z", "timezone": "..." }`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `zdt + 1 month` | Cannot add a period to a zoneddatetime — calendar arithmetic requires decomposition. Navigate to `.localdate`, add the period, then reconstruct via `.inZone(tz)`. |
| `zdt + 5` | Cannot add an integer to a zoneddatetime — use `zdt + 5 hours` for timeline offset, or navigate via `.localdate` for calendar arithmetic. |

---

### `localdatetime`

**What it makes explicit:** A date and time together — not a point on the global timeline, not two separate fields. No timezone. The `local` prefix explicitly signals: no timezone.

**Backing type:** `NodaTime.LocalDateTime`

**Declaration:**

```precept
field DetectedAt as localdatetime nullable
field ScheduledFor as localdatetime default '2026-04-13T09:00:00'
```

**Single-quoted literal:** `'2026-04-13T09:00:00'` — content shape includes `T` but no trailing `Z` and no bracket-enclosed timezone → `localdatetime`. The presence of `T` without `Z` or `[` is the distinguishing shape signal. See Locked Decision #18.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `localdatetime + period` | `localdatetime` | Calendar arithmetic. `LocalDateTime.Plus(Period)`. |
| `localdatetime + duration` | `localdatetime` | Time arithmetic. Duration bridging — same mechanism as `localtime + duration` (Decision #16). NodaTime's `LocalDateTime` has no `Plus(Duration)` — uses nanosecond arithmetic on the time component, preserving date component. |
| `localdatetime + 30 days`, `+ 3 months`, `+ 1 year`, `+ 2 weeks` | `localdatetime` | Postfix units — resolve to `period` in localdatetime context. |
| `localdatetime + 3 hours`, `+ 30 minutes`, `+ 45 seconds` | `localdatetime` | Postfix units — resolve to `duration`. |
| `localdatetime - localdatetime` | `period` | `Period.Between(ldt1, ldt2)`. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | Full ordering. |

Note: `localdatetime` and `localtime` both accept `period` and `duration`. For `localdatetime`, all components of both types are meaningful. For `localtime`, only time components of `period` are used (date components ignored — NodaTime's `LocalTime.Plus(Period)` behavior). `localdatetime` is the only type where every component of both `period` and `duration` applies. Both `localtime + duration` and `localdatetime + duration` use the same duration bridging mechanism (Decision #16) — NodaTime's `LocalTime` and `LocalDateTime` natively accept `Period`, not `Duration`, so the type checker translates sub-day Duration components into equivalent nanosecond arithmetic.

| **Not supported** | **Why** |
|---|---|
| `localdatetime + integer` | Bare integers don't carry unit semantics. |

**Navigation via `.inZone(tz)` (reverse mediation — local → universal):**

| Expression | Produces | Description |
|---|---|---|
| `localdatetime.inZone(tz)` | `zoneddatetime` | "Anchor this local reading to the timeline in this timezone." |
| `localdatetime.inZone(tz).instant` | `instant` | Reverse navigation to UTC instant. Replaces the eliminated `pin` function. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.localdate` | `localdate` | Date component. |
| `.localtime` | `localtime` | Time component. |
| `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second` | `integer` | Direct components. |

**Constraints:** `nullable`, `default '...'` (single-quoted localdatetime).

**Serialization:** ISO 8601 without timezone: `"2026-04-13T14:30:00"`.

#### Option: `localdatetime` inclusion vs. deferral

**Option A (Include):** Completes the local type vocabulary. 6 NIST compliance fields in security-incident sample. `localdatetime` is uniquely the bridge type that accepts both `period` and `duration`.

**Option B (Defer):** Smaller initial surface. 1 of 15 samples. Separate `localdate` + `localtime` fields cover most cases.

**Recommendation:** Open — needs design discussion.

---

## Timezone Mediation — `.inZone(tz)` Dot Accessor

### The single mediation operation

All timezone mediation flows through one operation — `.inZone(tz)` — accessed via dot syntax on the value that owns it. There are no standalone conversion functions.

| Expression | Produces | Semantics |
|---|---|---|
| `instant.inZone(tz)` | `zoneddatetime` | "View this moment in this timezone." |
| `localdatetime.inZone(tz)` | `zoneddatetime` | "Anchor this local reading to the timeline in this timezone." |

From `zoneddatetime`, navigation to any local type or back to `instant` uses dot accessors on the result:

| Dot chain | Produces | Semantics |
|---|---|---|
| `myInstant.inZone(tz).localdate` | `localdate` | "What date is it at this moment in this timezone?" |
| `myInstant.inZone(tz).localtime` | `localtime` | "What time is it at this moment in this timezone?" |
| `myInstant.inZone(tz).localdatetime` | `localdatetime` | "What local date+time is it at this moment in this timezone?" |
| `myInstant.inZone(tz).year` | `integer` | "What year is it at this moment in this timezone?" |
| `myLDT.inZone(tz).instant` | `instant` | "What UTC moment corresponds to this local date+time?" (reverse navigation) |

### Why one operation, not three functions

The v3 proposal had three standalone conversion functions: `toLocalDate(instant, tz)`, `toLocalTime(instant, tz)`, `toInstant(date, time, tz)`. Shane locked three decisions that collapsed these to one:

1. **D8 — Dot-vs-function rule:** "If the value owns it, dot." An instant owns its mediation to a timezone. Therefore `instant.inZone(tz)`, not `inZone(instant, tz)`.

2. **D11 — `inZone` as dot accessor:** `toLocalDate(instant, tz)` was a freestanding function doing two things: mediate timezone AND extract date. With dot chains, these are separate operations: `instant.inZone(tz)` (mediate) then `.localdate` (extract). Cleaner separation of concerns. Better IntelliSense — after typing `.inZone(tz).`, the completion list shows all available components.

3. **D13 — `pin` eliminated:** The reverse direction (`localdatetime → instant`) was originally a separate function `pin(ldt, tz)`. But `localdatetime.inZone(tz).instant` achieves the same result via the same `.inZone(tz)` operation. One mediation operation in both directions — no reason for two.

### Strict hierarchy — no skip-level accessors (Decision #22)

Navigation must follow the type hierarchy step by step. No skip-level accessors exist.

```
instant → .inZone(tz) → zoneddatetime → .localdatetime → localdatetime → .localdate / .localtime
                                       → .localdate → localdate
                                       → .localtime → localtime
                                       → .instant → instant (reverse)
```

| Valid | Invalid (compile error) | Why |
|---|---|---|
| `myInstant.inZone(tz).localdate` | `myInstant.localdate` | Skips timezone mediation — where's the timezone? |
| `myInstant.inZone(tz).year` | `myInstant.year` | Same — year depends on timezone. |
| `myLDT.inZone(tz).instant` | `myLDT.instant` | Skips timezone mediation — which timezone? |
| `zdt.localdatetime.localdate` | — | Valid: step-by-step navigation. |
| `zdt.localdate` | — | Also valid: `zoneddatetime` provides direct `.localdate` as one-step navigation within the zone-aware tier. |

**Rationale:** The strict hierarchy forces explicit timezone mediation at every boundary crossing between the universal tier (`instant`) and the local tier (`localdate`, `localtime`, `localdatetime`). This prevents the most common temporal bug: extracting calendar components from a UTC timestamp without specifying which timezone to interpret it in.

**Exception:** `zoneddatetime` provides `.localdate`, `.localtime`, and component accessors (`.year`, etc.) directly because it already carries a timezone. The timezone mediation has already happened — the hierarchy step was the `.inZone(tz)` call that produced the `zoneddatetime`.

### DST ambiguity resolution

- **Gap (spring forward):** Map to the instant *after* the gap. NodaTime's `LenientResolver`.
- **Overlap (fall back):** Map to the *later* instant. Safer for deadline calculations.

**Recommendation:** Lenient resolver. Deterministic. The 99% case is unaffected.

### Composability with existing operations

The `.inZone(tz)` accessor produces and consumes existing types — all existing operations chain naturally:

- `myInstant.inZone(tz).localdate` returns `localdate` — all localdate operations work.
- `myInstant.inZone(tz).localtime` returns `localtime` — all localtime operations work.
- `myLDT.inZone(tz).instant` returns `instant` — all instant operations work.

**Example — complete multi-timezone compliance rule (dot form):**

```precept
field ClinicTimezone as timezone default 'America/New_York'
field ShiftStart as instant
field ShiftLocalDateTime as localdatetime
field ShiftLocalDate as localdate
field ShiftLocalTime as localtime

# Timezone mediation — dot-accessor chains
-> set ShiftLocalDateTime = ShiftStart.inZone(ClinicTimezone).localdatetime
-> set ShiftLocalDate = ShiftStart.inZone(ClinicTimezone).localdate
-> set ShiftLocalTime = ShiftStart.inZone(ClinicTimezone).localtime
-> set IsOvernight = ShiftStart.inZone(ClinicTimezone).localtime >= '22:00'

# Reverse navigation (pin eliminated — use dot chain)
-> set BackToInstant = ShiftLocalDateTime.inZone(ClinicTimezone).instant
```

---

### Postfix Unit Literals

**Status:** Locked (Decision #17 — rewritten 2026-04-15). Unified postfix model — the sole mechanism for constructing temporal quantities.

Postfix unit literals provide an English-reading syntax for temporal quantities. All 7 function-call constructors (`days()`, `months()`, `years()`, `weeks()`, `hours()`, `minutes()`, `seconds()`) have been eliminated. The postfix system is the only quantity construction surface.

#### Two atom forms

**Bare postfix** — integer literal + unit keyword:

```
<integer-literal> <unit-keyword>
```

```precept
set DueDate = CreatedDate + 30 days
invariant FiledAt - IncidentAt <= 72 hours because "SLA"
field GracePeriod as period default 30 days
```

**Paren postfix** — parenthesized expression + unit keyword:

```
( <expression> ) <unit-keyword>
```

```precept
set Deadline = StartDate + (GraceDays) days       # variable argument
set SlaLimit = (SlaHours * 2) hours                # computed expression
set Expiry = StartDate + (TermMonths + 6) months   # arithmetic inside parens
```

Unit keywords: `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`.

#### Combination via `+` only

Multiple quantities combine using the standard `+` operator:

```precept
field ExtendedWarranty as period default 2 years + 6 months
set Expiry = StartDate + 1 year + 3 months + 15 days
invariant FiledAt - IncidentAt <= 72 hours + 30 minutes because "SLA window"
```

No composite juxtaposition. `2 years 6 months` (without `+`) is a compile error. This eliminates ~45–60 lines of parser machinery (composite combinator, composite AST node, composite validation, composite desugaring) and uses the existing `Parse.Chain` at the additive precedence level.

**Left-associative `+` subtlety:** `date + 1 month + 1 month` applies sequential truncation (Jan 31 → Feb 28 → Mar 28). `date + (1 month + 1 month)` builds the period first, then applies once (Jan 31 → Mar 31). Both are valid. Parens are available when "build-then-apply" semantics are needed.

#### Type resolution rules — context-dependent

The type of a postfix unit expression (`period` or `duration`) is determined by the expression context. `3 days` is not inherently `period` or `duration` — the surrounding expression resolves it. This applies equally to bare and paren postfix forms.

**For `days` and `weeks` — context-dependent:**

| Expression context | `3 days` / `(n) days` resolves to | NodaTime call |
|---|---|---|
| `localdate + _` | `period` | `Period.FromDays(3)` / `Period.FromDays(n)` |
| `localdatetime + _` | `period` | `Period.FromDays(3)` / `Period.FromDays(n)` |
| `instant + _` | `duration` | `Duration.FromDays(3)` / `Duration.FromDays(n)` |
| `zoneddatetime + _` | `duration` | `Duration.FromDays(3)` / `Duration.FromDays(n)` |
| `localtime + _` | depends on unit | Sub-day bridging applies (Decision #16) |
| `field X as period default _` | `period` | `Period.FromDays(3)` |
| `field X as duration default _` | `duration` | `Duration.FromDays(3)` |
| No context / ambiguous | **compile error** | "Cannot infer temporal type for `3 days` — provide context: `localdate_field + 3 days` or `instant_field + 3 days`" |

**For `months` and `years` — always `period`:**

`months` and `years` ALWAYS resolve to `period` regardless of expression context. No `Duration.FromMonths` or `Duration.FromYears` exists in NodaTime — months and years have variable nanosecond length.

| Expression context | `3 months` / `(n) months` resolves to | Result |
|---|---|---|
| `localdate + _` | `period` | ✓ `localdate + period → localdate` |
| `localdatetime + _` | `period` | ✓ `localdatetime + period → localdatetime` |
| `instant + _` | `period` | ✗ **type error** — `instant + period` |
| `zoneddatetime + _` | `period` | ✗ **type error** — `zoneddatetime + period` |
| `field X as period default _` | `period` | ✓ |
| `field X as duration default _` | `period` | ✗ **type error** |

**For `hours`, `minutes`, `seconds` — context-dependent (with `period` bridging):**

| Expression context | `3 hours` / `(n) hours` resolves to | NodaTime call |
|---|---|---|
| `instant + _` | `duration` | `Duration.FromHours(3)` |
| `zoneddatetime + _` | `duration` | `Duration.FromHours(3)` |
| `localtime + _` | bridges to duration | Sub-day bridging (Decision #16) |
| `localdatetime + _` | `duration` | `Duration.FromHours(3)` |
| `field X as duration default _` | `duration` | `Duration.FromHours(3)` |
| `field X as period default _` | `period` | `Period.FromHours(3)` |
| `localdate + _` | **warning** | "Adding hours to a date has no effect — dates have no time component." |
| No context / ambiguous | **compile error** | "Cannot infer temporal type for `3 hours`" |

**Owner's rationale (Shane):** "The author IS thinking about the details — they wrote `instant + 3 days`, which means '3 fixed 24-hour days on the timeline.' The context makes the intent unambiguous."

#### Teachable error messages

| Invalid code | Error message |
|---|---|
| `set X = 3 days` (no type context) | Cannot infer temporal type for `3 days` — the target type is ambiguous. Provide context: `localdate_field + 3 days` or `instant_field + 3 days`. |
| `GraceDays days` | Postfix unit literals require an integer constant or parenthesized expression. Use `(GraceDays) days` for variable arguments. |
| `localdate + 3 hours` | Warning: Adding hours to a date has no effect — dates have no time-of-day component. Did you mean `localdatetime + 3 hours`, or did you intend `localdate + 3 days`? |
| `2 years 6 months` | Postfix unit expressions must be combined with `+`. Use `2 years + 6 months`. |
| `instant + 3 months` | Cannot add a period to an instant — `months` always resolves to `period` (variable-length calendar units). Convert to a date first: `inst.inZone(tz).localdate + 3 months`, then navigate back via `.inZone(tz).instant`. |

---

## Literal Mechanism Architecture

Every non-primitive value in Precept enters through one of three doors. This is a **closed set of mechanisms** — a top-level design element of the language, not an incidental feature of temporal types.

### The three doors

| Door | Delimiter | What it carries | Type resolution | Temporal proof |
|------|-----------|----------------|-----------------|----------------|
| **1. String** | `"..."` | Text values | Always `string` | N/A — strings remain strings |
| **2. Typed constant** | `'...'` | Formatted non-primitive constants | Inferred from content shape | `localdate`, `localtime`, `localdatetime`, `instant`, `zoneddatetime`, `timezone` |
| **3. Quantity** | `N unit` | Magnitude + unit values | Context-dependent (`period` or `duration`) | `30 days`, `72 hours`, `3 months` |

**Zero constructors exist.** There is no `type(value)` form in the language — no `date(2026-01-15)`, no `duration(PT72H)`, no `money("100", "USD")`. Every value enters through one of the three doors above.

### The admission rule

A type qualifies for Door 2 (`'...'`) **if and only if** its content shape is distinguishable from every other single-quote inhabitant. No shape may match two types. This is a formal criterion — not a judgment call — that prevents ambiguity from accumulating as the language grows.

The current inhabitants demonstrate shape uniqueness:

| Inhabitant | Shape signal | Why it's unique |
|------------|-------------|-----------------|
| `localdate` | `YYYY-MM-DD` | Digits + hyphens, no `T` |
| `localtime` | `HH:MM:SS` | Digits + colons, no hyphen |
| `localdatetime` | `...T...` | `T` present, no `Z`, no `[` |
| `instant` | `...T...Z` | `T` + trailing `Z` |
| `zoneddatetime` | `...T...[...]` | `T` + bracket-enclosed zone |
| `timezone` | `Word/Word` | Alpha + `/`, no digits-first |

When a future type is proposed, the question is **"which door?"** — not "does it need new syntax?" If its values have a shape-recognizable constant form that passes the admission rule, it enters through Door 2. If its values are magnitude+unit quantities, it enters through Door 3. If neither applies, it remains a string (Door 1).

### Why three doors are sufficient

The three-door model covers all value categories a business DSL encounters:

- **Opaque identifiers with internal structure** (dates, times, UUIDs, emails, URIs) → Door 2: the structure *is* the shape signal.
- **Quantities with units** (durations, periods, currency amounts, physical measurements) → Door 3: magnitude and unit are the natural representation.
- **Plain text** (names, descriptions, codes without distinguishing shape) → Door 1: the default.

No fourth category has been identified. Values are either text, structured constants, or quantities. The three-door model is closed by construction — it does not need extensibility because it covers the domain.

### Temporal types prove the framework

Temporal types are the **first inhabitants** of Doors 2 and 3 — the gateway through which the language moves beyond primitives. They prove that:

1. **The typed constant delimiter works.** Six types with unambiguous shapes coexist in `'...'` without collision. The admission rule holds.
2. **The postfix quantity system works.** Seven unit keywords with context-dependent resolution handle both calendar and timeline quantities. The `+` combiner handles composites.
3. **Zero constructors are sufficient.** Every temporal value that constructors would have expressed (`date(2026-01-15)`, `duration(PT72H)`) has a more readable representation through Doors 2 and 3.

This is what makes temporal types the gateway: they don't just add temporal capability — they **validate the literal architecture** that all future non-primitive types will use.

---

## Semantic Rules

### Type-interaction matrix

**Calendar domain (uses `period`):**

| Left | Op | Right | Result | Notes |
|---|---|---|---|---|
| `localdate` | `+` | `period` | `localdate` | Calendar arithmetic. Truncation at month end. |
| `localdate` | `-` | `period` | `localdate` | Calendar arithmetic backward. |
| `localdate` | `-` | `localdate` | `period` | Calendar distance. `Period.Between`. |
| `period` | `+` | `period` | `period` | Combined. |
| `period` | `-` | `period` | `period` | Difference. |
| `localdatetime` | `+` | `period` | `localdatetime` | Calendar arithmetic on date part. |
| `localdatetime` | `-` | `period` | `localdatetime` | Backward. |
| `localdatetime` | `-` | `localdatetime` | `period` | Calendar distance. |
| `localtime` | `+` | `period` | `localtime` | `LocalTime.Plus(Period)`. Time components only; date components ignored. |

**Timeline domain (uses `duration`):**

| Left | Op | Right | Result | Notes |
|---|---|---|---|---|
| `instant` | `-` | `instant` | `duration` | Elapsed time. |
| `instant` | `+` | `duration` | `instant` | Offset forward. |
| `instant` | `-` | `duration` | `instant` | Offset backward. |
| `duration` | `+` | `duration` | `duration` | Combined. |
| `duration` | `-` | `duration` | `duration` | Difference. |
| `duration` | `*` | `integer`/`number` | `duration` | Scaling. |
| `duration` | `/` | `integer`/`number` | `duration` | Scaling. |
| `duration` | `/` | `duration` | `number` | Ratio. |
| `localtime` | `+` | `duration` | `localtime` | Wraps at midnight. |
| `localtime` | `-` | `localtime` | `period` | Time-unit period. |
| `zoneddatetime` | `+` | `duration` | `zoneddatetime` | Timeline arithmetic. |
| `zoneddatetime` | `-` | `duration` | `zoneddatetime` | Backward. |
| `zoneddatetime` | `-` | `zoneddatetime` | `duration` | Instant subtraction. |

**Bridge type (`localdatetime` — accepts both):**

| Left | Op | Right | Result | Notes |
|---|---|---|---|---|
| `localdatetime` | `+` | `duration` | `localdatetime` | Time arithmetic. |
| `localdatetime` | `-` | `duration` | `localdatetime` | Backward. |

### Comparison rules

| Type | `==`, `!=` | `<`, `>`, `<=`, `>=` | Ordering semantics |
|---|---|---|---|
| `localdate` | ✓ | ✓ | ISO calendar |
| `localtime` | ✓ | ✓ | Within-day |
| `instant` | ✓ | ✓ | Nanoseconds on global timeline |
| `duration` | ✓ | ✓ | Nanoseconds |
| `period` | ✓ | **✗** | **No natural ordering.** See Decision #14. |
| `timezone` | ✓ | ✗ | Equality by IANA identifier. |
| `localdatetime` | ✓ | ✓ | Same-calendar |
| `zoneddatetime` | ✓ | ✓ | By underlying instant |

Cross-type comparison is always a type error.

### Cross-type arithmetic: what's NOT allowed (and why)

| Expression | Why it's a type error |
|---|---|
| `localdate + duration` | Duration is timeline-only. Localdate needs period (calendar) arithmetic. |
| `instant + period` | Period may contain calendar units with variable length. Instant needs duration (timeline) arithmetic. |
| `duration + period` | Cannot mix timeline and calendar units in arithmetic. |
| `zoneddatetime + period` | Calendar arithmetic on ZDT requires decomposition. |
| `localdate + integer` | Bare integers don't carry unit semantics. Use `(n) days`. |
| `instant + integer` | Ambiguous unit. Use `(n) hours` or `(n) seconds`. |
| `period < period` | Periods are not orderable. |
| `period * integer` | Period scaling not supported. |
| `localdate + localdate` / `instant + instant` | Adding two points is meaningless. |
| `instant.year` | Requires timezone. Use `myInstant.inZone(tz).localdate.year`. |
| `instant.localdate` | Skips timezone mediation. Use `myInstant.inZone(tz).localdate`. |
| `duration * duration` | Dimensionally meaningless. |
| `duration * decimal` | Lossy narrowing. Use `number`. |

### Nullable and default behavior

All temporal types support `nullable`. All follow existing null propagation rules.

| Type | Default value | Notes |
|---|---|---|
| `localdate` | `default '...'` | Author specifies (single-quoted date). |
| `localtime` | `default '...'` | Author specifies (single-quoted time). |
| `instant` | `default '...'` | Author specifies (single-quoted instant). |
| `duration` | `default 0 hours` | Zero duration. |
| `period` | `default 0 days` | Zero period. |
| `timezone` | No default | No universally sensible default. |
| `zoneddatetime` | No default | Same as timezone. |
| `localdatetime` | `default '...'` | Author specifies (single-quoted localdatetime). |

**`nullable` + `default`:** Open — George's Challenge #3. Deferred to cross-cutting decision.

---

## Locked Design Decisions

### 1. NodaTime as the backing library for all temporal types

- **Why:** Philosophy match; `Period`/`Duration` split eliminates custom dispatch; battle-tested arithmetic.
- **Alternatives rejected:** BCL types (no `Period` equivalent). Custom implementation (unmaintainable). Collapsed `Period`/`Duration` (v1 — requires custom dispatch, contradicts directive).
- **Precedent:** NRules inherits `System.Decimal`. Precept inherits NodaTime's temporal model.
- **Tradeoff:** Additional NuGet dependency (~1.1 MB).

### 2. Day granularity for `localdate`

- **Why:** "2026-03-15" means the same calendar day everywhere. No timezone dependency.
- **Alternatives rejected:** `localdatetime` as only calendar type. Optional timezone.
- **Precedent:** SQL `DATE`, FEEL `date()`.
- **Tradeoff:** Time-of-day needs separate `localtime` type or `localdatetime`.

### 3. ISO 8601 as the sole format

- **Why:** Single canonical format eliminates parsing ambiguity.
- **Alternatives rejected:** Configurable formats. Auto-detection.
- **Precedent:** Cedar (RFC 3339), FEEL, SQL.
- **Tradeoff:** Authors from `MM/DD/YYYY` regions must adapt.

### 4. Single-quoted delimiter is the typed constant delimiter — not temporal-specific (revised — locked 2026-04-15)

- **Why:** The `'...'` delimiter is the **typed constant delimiter** for any non-primitive type whose content shape is unambiguous — not a temporal-specific feature. The three literal mechanisms are a closed set: `"..."` (string), `'...'` (typed constant, shape-inferred), `N unit` (quantity). Zero constructors exist in the language. A type qualifies for `'...'` if and only if its content shape is distinguishable from every other single-quote inhabitant (the admission rule). Temporal types are the **first inhabitants**: `localdate` (`YYYY-MM-DD`), `localtime` (`HH:MM:SS`), `localdatetime` (`...T...`), `instant` (`...T...Z`), `zoneddatetime` (`...T...[zone]`), `timezone` (`Word/Word`). This follows Precept's literal grain: `"hello"` doesn't need `string("hello")`, so `'2026-01-15'` doesn't need `localdate(2026-01-15)`.
- **Alternatives rejected:** (A) `date(2026-01-15)` (unquoted constructor — original Decision #4/18) — the team's unanimous initial recommendation. Shane challenged it with the string-delimiter precedent insight: if strings use `"..."` without a `string()` wrapper, temporal values should use a delimiter too. See "Double-Quote Alternative Analysis" below. (B) `date("2026-01-15")` (string constructor) — quotes suggest "a string being parsed." (C) Bare ISO `2024-01-15` — lexer ambiguity with subtraction. (D) `"2026-01-15"` (double-quoted, context-resolved) — explored and rejected; see "Double-Quote Alternative Analysis." (E) Sigil prefix (`#2026-01-15`) — non-obvious, not Precept's voice.
- **Precedent:** SQL `'2026-01-15'` for value literals. Python `b'...'` / `r'...'` for typed string variants with distinct delimiters.
- **Tradeoff:** Type is implicit in content shape rather than explicit in a keyword. Readers must recognize ISO formats. Mitigated by: (a) ISO date formats are culturally universal, (b) field declarations provide type context, (c) syntax highlighting colors single-quoted content distinctly.

**`char` type permanent exclusion (supporting decision):** Precept will never have a `char` type — characters are programming-language implementation details, not business-domain data. Single quotes are permanently reserved for the typed constant delimiter with zero future collision risk. Confirmed by Frank and George.

### 5. No timezone on `localdate`, `localtime`, or `localdatetime`

- **Why:** Calendar/clock types represent what's on a calendar or wall clock. The `local` prefix makes this explicit.
- **Alternatives rejected:** UTC-anchored dates. Timezone as required metadata.
- **Precedent:** NodaTime `Local*` types. The naming convention directly mirrors NodaTime's `LocalDate`, `LocalTime`, `LocalDateTime`.
- **Tradeoff:** Timezone conversion requires explicit `.inZone(tz)` dot-accessor chains.

### 6. `instant` has no component accessors — restricted semantics (strengthened in v4)

- **Why:** Extracting `.year` from an instant requires a timezone. NodaTime's `Instant` has no date/time accessors. `instant` supports only: comparison, duration arithmetic, and `.inZone(tz)` navigation. No component accessors, no period arithmetic.
- **Alternatives rejected:** Accessors with mandatory timezone parameter. Implicit UTC.
- **Precedent:** NodaTime `Instant`.
- **Tradeoff:** More verbose than `FiledAt.year`. Verbosity is the point — it forces explicit timezone mediation.

### 7. `timezone` as a first-class type

- **Why:** `"California"`, `"EST"` compile as strings. The `date`-over-`string` argument applies.
- **Alternatives rejected:** `string` with conventions. `choice` with all IANA identifiers.
- **Precedent:** NodaTime's `DateTimeZone`.
- **Tradeoff:** New type in the system.

### 8. Timezone mediation via `.inZone(tz)` dot accessor (rewritten in v4)

- **Why:** D8 (dot-vs-function rule): "If the value owns it, dot." An instant owns its mediation to a timezone. D11: `.inZone(tz)` produces a `zoneddatetime`; navigation to local types uses dot chains (`.localdate`, `.localtime`, `.localdatetime`). This replaces three standalone functions (`toLocalDate`, `toLocalTime`, `toInstant`) with one composable operation. Better IntelliSense: after `.inZone(tz).`, the completion list shows all available components.
- **Alternatives rejected:** Three standalone functions (`toLocalDate(instant, tz)`, `toLocalTime(instant, tz)`, `toInstant(date, time, tz)`) — v3 position. Conflated mediation+extraction in one call. `pin(ldt, tz)` for reverse direction — redundant with `ldt.inZone(tz).instant`.
- **Precedent:** NodaTime's own `Instant.InZone(DateTimeZone)` returns `ZonedDateTime`. Java's `Instant.atZone(ZoneId)` returns `ZonedDateTime`. The dot-accessor form is the universal pattern.
- **Tradeoff:** Longer expression for simple cases: `myInstant.inZone(tz).localdate` vs. `toLocalDate(myInstant, tz)`. The verbosity is intentional — two separate operations (mediate, then extract) are visually distinct.

### 9. Determinism is relative to the runtime environment

- **Why:** "Same inputs = same output" includes TZ database version.
- **Alternatives rejected:** Excluding all timezone operations. Mandatory TZ pinning.
- **Precedent:** Every timezone-sensitive system manages TZ database freshness.
- **Tradeoff:** Determinism input surface includes TZ database version.

### 10. `localdate + integer` is a type error — use `localdate + (n) days`

- **Why:** `localdate + 2` is implicit — what does `2` mean? NodaTime requires `LocalDate.Plus(Period.FromDays(n))` — the `Period` makes the unit visible. Now that `period` is a surface type, `(n) days` resolves to `period` and the type checker validates `localdate + period`. The shortcut `localdate + integer` would bypass this type-level enforcement.
- **Alternatives rejected:** `localdate + integer` meaning "add N days" — the v1 position. Convenient but implicit. In a proposal with both `(n) days` and `(n) months` as period constructors, `+ 5` is genuinely ambiguous. NodaTime's `LocalDate` has no `operator+(int)` — it requires `Plus(Period)`.
- **Precedent:** NodaTime `LocalDate` has no integer arithmetic operator. FEEL requires explicit offsets.
- **Tradeoff:** `DueDate + 5 days` is more verbose than `DueDate + 5`. The verbosity forces the author to name the unit.

### 11. `localdate - localdate` returns `period` (not integer, not duration)

- **Why:** The result of subtracting two dates is a calendar quantity. NodaTime's `Period.Between(d1, d2)` returns `Period` — it preserves structural calendar components. Returning `integer` loses unit semantics. Returning `duration` conflates calendar distance with timeline distance.
- **Alternatives rejected:** `→ integer` — loses unit semantics. `→ duration` (v1 position) — conflates calendar and timeline. NodaTime deliberately returns `Period`.
- **Precedent:** NodaTime `Period.Between(LocalDate, LocalDate)`.
- **Tradeoff:** Period result requires accessor inspection or comparison. Indirection is intentional.

### 12. `period` is a surface type (field type AND expression type)

- **Why:** Shane's directive: *"No obscurity, expose NodaTime."* 10 period fields across 7 samples are `integer` surrogates. The integer loses the unit; the period carries it. NodaTime provides full factories, operators, equality, and serialization for `Period`.
- **Alternatives rejected:** `period` as expression-result only, with `integer` surrogates (v1 position) — same type-safety gap as `string` for dates.
- **Precedent:** NodaTime `Period` — full factories, operators, equality, `PeriodPattern.Roundtrip`.
- **Tradeoff:** Larger type system surface. Worth it — `field LoanTerm as period default 12 months` is strictly more expressive than `field TermLengthMonths as integer default 12`.

### 13. `period` is full NodaTime Period — date AND time components (v2.1 revision)

- **Why:** NodaTime's `Period` includes both date components (years, months, weeks, days) and time components (hours, minutes, seconds). The v2 date-only restriction contradicted the "expose NodaTime faithfully" directive in three ways: (1) `LocalTime.Plus(Period)` is NodaTime's native API for time arithmetic, but date-only `period` couldn't carry time results; (2) `Period.Between(LocalTime, LocalTime)` returns time-component Periods with no home in a date-only type; (3) timeline constructors returning `duration` plus `LocalTime` not accepting `Duration` left no clean implementation path for `localtime + 3 hours`. `Period.FromHours()`, `.FromMinutes()`, `.FromSeconds()` all exist in NodaTime.
- **Alternatives rejected:** Date-only `period` (v2 position) — created the three structural contradictions above. Re-invents a boundary NodaTime chose not to draw.
- **Precedent:** NodaTime `Period` — full date+time components. `Period.HasDateComponent` / `Period.HasTimeComponent` for introspection.
- **Tradeoff:** `localdate + period` where the period has time components: the localdate type ignores the time portion (NodaTime's `LocalDate.Plus(Period)` behavior). Some expressions are valid but semantically empty for one component. This matches NodaTime — they made the same choice.

### 14. No ordering on `period` — `==` and `!=` only

- **Why:** NodaTime's `Period` has no `IComparable`. "Is 1 month greater than 30 days?" depends on which month. NodaTime requires `CreateComparer(LocalDateTime)` — a reference date. Precept exposes this truth.
- **Alternatives rejected:** Reference-date ordering (adds complexity). Approximate ordering (incorrect — violates "force authors to think about the details").
- **Precedent:** NodaTime `Period` — no `IComparable`.
- **Tradeoff:** Cannot sort by period or use `<`/`>` guards. Compare concrete dates instead: `StartDate + GracePeriod < Deadline`.

### 15. Structural equality on `period` — `1 month != 30 days`

- **Why:** NodaTime's equality is structural — "24 hours" ≠ "1 day" unless `NormalizingEqualityComparer` is used. Calendar units are non-equivalent: 1 month is 28–31 days.
- **Alternatives rejected:** Normalizing equality (requires reference date). Calendar-aware equality (which month?).
- **Precedent:** NodaTime standard `Period` equality.
- **Tradeoff:** `7 days == 1 week` is `false`. Structurally different representations.

### 16. Sub-day duration bridging — `localtime + duration` via thin type-checker translation

- **Why:** `hours`, `minutes`, `seconds` in most expression contexts resolve to `duration` (fixed-length quantities — Duration's natural type). NodaTime's `LocalTime` accepts `Period`, not `Duration`. But for sub-day units, `Duration` and `Period` represent identical physical quantities — an hour is always exactly 3600 seconds regardless of whether it's stored as `Duration.FromHours(1)` or `Period.FromHours(1)`. The type checker allows `localtime + duration → localtime`. Runtime: nanosecond arithmetic on `LocalTime` (NodaTime stores `LocalTime` as nanosecond-of-day internally). This uses NodaTime's own primitives, not a reimplementation.
- **Alternatives rejected:** (A) Time units always producing `period` — `instant + 3 hours` fails unless compiler can guarantee no date components, which is impossible for period fields at compile time. Violates Principle #8. (C) Context-dependent return type — implicit, violates the directive. (D) Separate unit keywords for duration vs. period — two names for the same physical quantity; verbose duplication.
- **Precedent:** NodaTime has BOTH `Duration.FromHours(1)` AND `Period.FromHours(1)` for the same physical quantity. Precept picks one entry point (`duration`) and bridges at the type-checker level.
- **Tradeoff:** `localtime + duration` is not a direct NodaTime API call. The translation layer is thin — nanosecond arithmetic — not a reimplementation. NodaTime's own `LocalTime.PlusHours()` convenience methods do the same nanosecond manipulation internally.

### 17. Unified postfix model — sole mechanism for temporal quantity construction (rewritten 2026-04-15)

**What:** The temporal quantity grammar is standardized to two atom forms and one combination operator:

1. **Bare postfix:** `30 days`, `72 hours`, `12 months` — integer literal + unit keyword
2. **Paren postfix:** `(GraceDays) days`, `(X + 5) hours` — parenthesized expression + unit keyword
3. **Combination via `+`:** `2 years + 6 months + 15 days` — standard addition operator

**What was eliminated:**
- All 7 function-call constructors: `days()`, `months()`, `years()`, `weeks()`, `hours()`, `minutes()`, `seconds()`
- Composite literal form (greedy juxtaposition): `2 years 6 months 15 days`
- `and` as a composite combiner (explored and rejected — dual meaning with boolean `and`)

- **Why:** One syntax, zero redundancy. Postfix reads as English prose (Principle #2). `+` is already the operator for combining quantities — no new grammar concepts needed. Eliminates ~45–60 lines of parser machinery (composite combinator, AST node, validation, desugaring). CSS precedent: literals are bare (`30px`), expressions require grouping (`calc(...)`). Mathematical notation precedent: $(x + 5)\text{ kg}$ — grouping before unit. The function-call form was broken anyway — `days(n)` always returned `period`, useless for `instant + duration` contexts. With paren postfix, `(GraceDays) days` in instant context correctly resolves to `Duration.FromDays(n)` — something `days(GraceDays)` could never do.
- **Alternatives rejected:** (A) Function-call only — leaves three NodaTime alignment gaps: `days(n)` always returns `period`, no way to get `Duration.FromDays(n)`; `hours(n)` always returns `duration`, no way to get `Period.FromHours(n)` for period defaults; no access to both NodaTime factory families. (B) Both syntaxes coexist (original Decision #17 — "Option C") — redundant, dual syntax for the same concepts, confusing for authors and AI consumers. Function calls and postfix had DIFFERENT semantics (`days(n)` always `period`, `3 days` contextual), creating a teaching trap. (C) Composites via juxtaposition — requires ~45–60 lines of parser machinery, dual semantics with left-associative `+`, no advantage over explicit `+`.
- **Precedent:** CSS (`30px` bare, `calc()` for expressions), Kotlin (`3.days`, extension functions), F# units of measure (`3<kg>`), mathematical notation, ISO 80000-1.
- **Tradeoff:** Context-dependent type resolution is implicit — `3 days` alone has no fixed type. Mitigated by: (1) compile error on ambiguous standalone usage, (2) the context that determines the type (`date +` vs `instant +`) is visible in the same expression, (3) `months`/`years` have NO duration equivalent, so they always resolve to `period` without ambiguity.

**Context-dependent type resolution unchanged:** `localdate +` context → `period`, `instant +` context → `duration`, field default → match declared type, no context → compile error.

**Left-associative `+` subtlety:** `date + 1 month + 1 month` applies sequentially with truncation (Jan 31 → Feb 28 → Mar 28). `date + (1 month + 1 month)` builds period first then applies once (Jan 31 → Mar 31). Both valid — parens available for "build-then-apply" semantics.

**Supersedes:** Original Decision #17's "Option C — both syntaxes coexist" rationale and composite literal rules.

### 18. Single-quoted typed constants — `'...'` as the typed constant delimiter (rewritten 2026-04-15, reframed 2026-04-15)

**What:** The `'...'` delimiter is the **typed constant delimiter** — a language-level mechanism for any non-primitive type whose content shape is unambiguous. Temporal types are the first inhabitants, not the definition. Constants for `date`, `time`, `datetime`, `instant`, `zoneddatetime`, and `timezone` use `'...'` with type inferred from content shape.

| Type | Literal | Content shape |
|------|---------|--------------|
| date | `'2026-01-15'` | `YYYY-MM-DD` |
| time | `'14:30:00'` | `HH:MM:SS` |
| datetime | `'2026-01-15T14:30:00'` | `...T...` (no `Z`, no `[`) |
| instant | `'2026-01-15T14:30:00Z'` | `...T...Z` |
| zoneddatetime | `'2026-01-15T14:30:00[America/New_York]'` | `...T...[zone]` |
| timezone | `'America/New_York'` | IANA identifier |

**Delimiter semantics — the three-mechanism model:**

| Delimiter | Meaning | Type resolution |
|-----------|---------|-----------------|
| `"..."` | String | Always `string` |
| `'...'` | Typed constant | Inferred from content shape — admits any type whose shape is distinguishable from all other inhabitants |
| `N unit` | Quantity | Magnitude + unit keyword — resolves to `period` or `duration` by expression context |

**Admission rule:** A type qualifies for `'...'` if and only if its content shape is distinguishable from every other single-quote inhabitant. No shape may match two types. This formal criterion prevents ambiguity from accumulating as new types are admitted.

**Current inhabitants (temporal — first wave):** The six temporal types listed above. **Future candidates (illustrative, not committed):** UUID (`'550e8400-...'` — `8-4-4-4-12` hex), email (`'user@example.com'` — `@` present), URI (`'https://...'` — scheme `://`), semver (`'2.1.0'` — `N.N.N`). Each would need to pass the admission rule before entering.

**Duration and period have no literal form** — postfix units only (see Decision #17).

**Design evolution:** The `date(2026-01-15)` constructor form was the team's unanimous initial recommendation (original Decision #18). Shane then challenged it with the string-delimiter precedent insight: strings don't need `string("hello")` — the `"..."` delimiter IS the type signal. Why should temporal values need `date(...)` when a distinct delimiter can carry the same information? This led to exploring `"..."` with context resolution (rejected — see "Double-Quote Alternative Analysis" below), then to single quotes as the clean solution.

- **Why:** (1) Follows Precept's literal grain — the delimiter IS the type signal. (2) Single token in the tokenizer (`'[^']*'`), same as `NumberLiteral` and `StringLiteral`. (3) Uniform across all 6 types. (4) `'` is an unambiguous IntelliSense trigger for temporal completions and progressive validation. (5) No `char` type collision — permanently safe.
- **Alternatives rejected:** (A) `localdate(2026-01-15)` (unquoted constructor) — team's initial choice, superseded by delimiter insight. (B) `localdate("2026-01-15")` (string constructor) — quotes suggest "string being parsed." (C) `"2026-01-15"` (double-quoted, context-resolved) — explored and rejected; see "Double-Quote Alternative Analysis." (D) Bare ISO `2024-01-15` — lexer ambiguity with subtraction. (E) SQL-style `DATE '2026-01-15'` — redundant keyword when content shape is sufficient.
- **Precedent:** SQL `'...'` for value literals including dates. Precept's own `"..."` → string pattern.
- **Tradeoff:** Type is implicit in content shape rather than explicit in a keyword. Readers must recognize ISO formats. Mitigated by: (a) ISO formats are culturally universal, (b) field declarations provide type context, (c) syntax highlighting can color single-quoted content distinctly.

**Tooling implications:**
- **IntelliSense:** `'` triggers temporal completions with format-aware ghost text. After `'2024-`, show `-MM-DD` placeholder. After `'14:`, show `mm:ss`. Content-shape detection enables type-specific validation as the author types.
- **Hover content (invariant culture, deterministic):**
  - `'2024-01-15'` → "localdate: January 15, 2024 (Monday)"
  - `'14:30'` → "localtime: 2:30 PM"
  - `'2024-01-15T14:30:00'` → "localdatetime: January 15, 2024 at 2:30 PM (Monday)"
  - `'2024-01-15T14:30:00Z'` → "instant: 2024-01-15T14:30:00Z (UTC)" — NO day-of-week (requires timezone)
  - `'America/New_York'` → "timezone: America/New_York (UTC-05:00 / UTC-04:00 DST)"
- **Timezone completions:** `'` in timezone context triggers IANA timezone completion from `DateTimeZoneProviders.Tzdb.Ids` with UTC offset descriptions.
- **Hover is always invariant culture English** — not locale-dependent. Same `.precept` file, same hover everywhere.

### 19. `local*` naming convention — `localdate`, `localtime`, `localdatetime` (locked v4)

- **Why:** NodaTime's own naming convention uses `LocalDate`, `LocalTime`, `LocalDateTime` — the `Local` prefix explicitly signals "no timezone." The v1 proposal used `local*` naming, v2–v3 switched to bare English words (`date`, `time`, `datetime`). Shane's explicit lock (D10) returns to `local*` because: (1) symmetry with NodaTime types eliminates a naming translation layer, (2) the `local` prefix is semantically precise — these types represent *local* observations without timezone context, (3) it avoids ambiguity with generic English words (`datetime` could be confused with `System.DateTime`).
- **Alternatives rejected:** Bare English names (`date`, `time`, `datetime`) — v2–v3 position. Natural-sounding but lose the "no timezone" semantic signal. `calendardate` / `walltime` — descriptive but diverge from NodaTime naming.
- **Precedent:** NodaTime (`LocalDate`, `LocalTime`, `LocalDateTime`), Java 8 (`LocalDate`, `LocalTime`, `LocalDateTime`), Kotlin datetime library. Every major temporal library that distinguishes local from zoned types uses the `Local` prefix.
- **Tradeoff:** Slightly longer type names (`localdate` vs `date`). Worth it — the precision eliminates an entire class of "does this type carry a timezone?" confusion.

### 20. Dot-vs-function rule — "If the value owns it, dot" (locked v4)

- **Why:** This is a language-wide design principle, not just a temporal decision. Applied to temporal types: `myInstant.inZone(tz)` (the instant owns its mediation), `zdt.localdate` (the zoneddatetime owns its components), `myPeriod.years` (the period owns its structural components). Freestanding functions remain for operations that don't belong to any particular value: `abs()`, `min()`, `trim()`.
- **Alternatives rejected:** All operations as freestanding functions — v3 position (`toLocalDate(instant, tz)`). Loses IntelliSense discoverability (see Elaine's research). Functions don't chain naturally.
- **Precedent:** NodaTime uses instance methods (`instant.InZone(tz)`, `zdt.LocalDateTime`). Java uses instance methods (`instant.atZone(tz)`, `zdt.toLocalDate()`). C# `System.DateTime` uses instance properties (`.Date`, `.TimeOfDay`). Dot access is the universal pattern.
- **Tradeoff:** Dot chains can become long (`myInstant.inZone(tz).localdate.year`). Mitigated by set-to-variables pattern and intermediate field assignments.

### 21. `inZone` as the sole timezone mediation operation (locked v4)

- **Why:** One operation in both directions. `instant.inZone(tz)` → `zoneddatetime` (universal→local). `localdatetime.inZone(tz)` → `zoneddatetime` (local→universal). Navigation to specific types via dot chains on the result. Eliminates three standalone functions (`toLocalDate`, `toLocalTime`, `toInstant`) and the eliminated `pin` function. Cleaner separation of concerns — mediation and extraction are separate dot operations, not conflated in one function call.
- **Alternatives rejected:** Three standalone functions (v3 position) — conflated mediation+extraction, poor IntelliSense discoverability. Two functions (`inZone` + `pin`) — `pin` is redundant with `ldt.inZone(tz).instant`.
- **Precedent:** NodaTime `Instant.InZone(DateTimeZone) → ZonedDateTime`. Java `Instant.atZone(ZoneId) → ZonedDateTime`.
- **Tradeoff:** `myInstant.inZone(tz).localdate` is longer than `toLocalDate(myInstant, tz)`. The verbosity makes the two-step process (mediate, then extract) visually explicit.

### 22. Strict type hierarchy — no skip-level accessors (locked v4)

- **Why:** Forces explicit timezone mediation at every boundary crossing between the universal tier (`instant`) and the local tier (`localdate`, `localtime`, `localdatetime`). `instant.localdate` is a compile error — must go `instant.inZone(tz).localdate`. This prevents the most common temporal bug: extracting calendar components from a UTC timestamp without specifying which timezone.
- **Alternatives rejected:** Skip-level convenience accessors (`instant.localdate(tz)` — parameter on the accessor) — conflates mediation into the accessor, inconsistent with other accessor patterns. Implicit UTC extraction (`instant.year` defaults to UTC) — exactly what NodaTime was designed to prevent.
- **Precedent:** NodaTime's `Instant` has no date/time properties at all — `InZone()` is required. Java's `Instant` has no `getYear()` — `atZone()` is required.
- **Tradeoff:** Verbose chains for simple extractions. `myInstant.inZone(tz).localdate.year` requires understanding the full hierarchy. Mitigated by: assign intermediate results to named fields for readability.

### 23. `pin` eliminated — reverse navigation via dot chain (locked v4)

- **Why:** `localdatetime.inZone(tz).instant` achieves the same result as the proposed `pin(ldt, tz)` function. One mediation operation (`.inZone(tz)`) in both directions is simpler than two operations (`inZone` for forward, `pin` for reverse). Fewer concepts to learn, fewer function names to remember.
- **Alternatives rejected:** `pin(localdatetime, tz) → instant` — dedicated reverse-direction function. Redundant with dot-chain composition.
- **Precedent:** NodaTime uses `LocalDateTime.InZoneLeniently(DateTimeZone).ToInstant()` — the same composition pattern.
- **Tradeoff:** `ldt.inZone(tz).instant` is slightly longer than `pin(ldt, tz)`. The consistency of using the same `.inZone(tz)` operation in both directions outweighs the character count.

### 24. Instant restricted semantics — comparison, duration arithmetic, and `.inZone(tz)` only (locked v4)

- **Why:** An instant is a point on the UTC timeline with no calendar context. The only operations that make physical sense on a UTC timeline point are: comparison (which came first?), duration arithmetic (add/subtract fixed time), and timezone mediation (view in a timezone). Component accessors (`.year`, `.localdate`) are compile errors because they require a timezone. Period arithmetic (`+ 1 month`) is a type error because months have variable duration. This is NodaTime's `Instant` design, exposed faithfully.
- **Alternatives rejected:** Instant with implicit UTC component accessors (`instant.year` meaning "year in UTC") — hides a timezone dependency behind convenience. Instant with period arithmetic via implicit UTC conversion — implicit behavior is what NodaTime was designed to prevent.
- **Precedent:** NodaTime `Instant`: no date/time properties, no period arithmetic, only comparison + duration + `InZone()`.
- **Tradeoff:** Every calendar operation on an instant requires `.inZone(tz)` first. Verbosity is the point — it makes timezone dependencies explicit.

---

## Double-Quote Alternative Analysis

After the `date(2026-01-15)` constructor form was challenged by the delimiter insight, the team explored whether double-quoted strings (`"2026-01-15"`) could serve as temporal literals with the type inferred from context (field declaration, expression position).

### The compiler CAN resolve it in ~99% of cases

Context resolution is technically feasible. The compiler knows the target type from:
- Field declarations: `field DueDate as localdate default '2026-01-15'` → the field is `localdate`, the literal is a localdate.
- Binary operators: `DueDate + '2026-01-15'` → left operand is `localdate`, right must be `localdate` (for comparison) or `period` (for arithmetic).
- Dot-accessor context: `myInstant.inZone(tz).localdate` → the chain produces a `localdate`.

The ~1% failure case is genuinely ambiguous positions (pure variable assignment without type annotation), which could be mitigated by requiring explicit type annotation in those rare positions.

### Why it was rejected

Three arguments against double-quoted temporal resolution, in order of strength:

1. **Refactoring safety.** Changing `field DueDate as localdate` to `field DueDate as string` silently changes the meaning of every `"2026-01-15"` assigned to it. With single quotes (`'2026-01-15'`), those expressions immediately become type errors, flagging every usage site for review. Type changes should produce compile errors at affected usage sites — not silent semantic shifts.

2. **IntelliSense quality.** `"` is the trigger for string completions. If `"` sometimes starts a temporal literal, the completion engine needs full expression-context analysis before offering suggestions. `'` is an unambiguous trigger — inside single quotes, always offer temporal completions with format-aware ghost text and progressive validation.

3. **Error clarity.** When `"2026-01-15"` fails validation in a date context, the error is: "Invalid date format in string." With `'2026-01-15'`, the error is: "Invalid date: ..." — the system knows this was intended as a temporal value because the author used the temporal delimiter. The error can be specific and actionable.

### The choice-type precedent

An important counter-argument was considered: Precept's `choice` fields already use `"..."` double-quoted strings where the meaning depends on the field declaration. `choice("Draft", "Active", "Closed")` defines allowed values — `"Draft"` is only valid in the context of that field.

However, the distinction matters: **choice is a constraint on a value within the same type** — a string field constrained to specific string values. The string `"Draft"` is still a string; the field merely limits which strings are valid. Temporal would be a **type-family conversion** — the string `"2026-01-15"` would need to become a fundamentally different type (`localdate`). This crosses from value-constraint to type-conversion, which is the stronger argument for a distinct delimiter.

### Conclusion

Single quotes win on all three criteria that matter for a DSL: refactoring safety (silent semantic shift → compile error), IntelliSense (unambiguous trigger), and error clarity (intent-aware messages). The ~99% context-resolution success rate is insufficient when the 1% failure mode is silent type conversion.

---

## George's Challenges — Resolution

### Challenge 1: `DueDate + MealsTotal` compiles because `localdate + number → localdate`

**Resolution (v2 — strengthened):** No form of `localdate + <non-period>` is defined. `DueDate + MealsTotal` is a type error regardless of `MealsTotal`'s type — none of `integer`, `number`, `decimal`, or `duration` are `period`.

### Challenge 2: `localdate + 2.5` should reject fractional offsets

**Resolution (v2 — strengthened):** `localdate + 2.5` is a type error — `2.5` is not a `period`. `localdate + 2` is also a type error. `(2.5) days` is a type error — `days` requires `integer` (bare postfix) or integer-typed expression (paren postfix).

### Challenge 3: `nullable + default` prohibition

**Resolution:** Open. Deferred to cross-cutting design decision.

### Challenge 4: `localdate - localdate` result type

**Resolution (v2 — redesigned):** `localdate - localdate → period`. Preserves structural calendar units. Matches `Period.Between(d1, d2)`.

---

## Dependencies and Related Issues

| Issue | Relationship |
|---|---|
| #25 (choice type) | Complementary. |
| #26 (date type) | **Superseded.** `localdate` section here incorporates all of #26 plus NodaTime, period-based arithmetic, postfix unit construction. |
| #27 (decimal type) | Complementary. No cross-type arithmetic. |
| #29 (integer type) | Postfix unit expressions take `integer`. Dependency. |
| #16 (built-in functions) | Conversion functions from this proposal. Constructor functions eliminated — replaced by postfix units. |
| #13 (field-level constraints) | `nullable`, `default`, `nonnegative` architecture. |

---

## Explicit Exclusions / Out of Scope

| Item | Status | Rationale |
|---|---|---|
| `OffsetDateTime` | Excluded | UTC offset without timezone rules. Weaker than full timezone. |
| `AnnualDate` | Deferred | Real demand (HR, insurance), low corpus frequency. Evaluate post-Phase 2. |
| `YearMonth` | Deferred | Real demand (billing), low priority. |
| `DateInterval` / `daterange` | Deferred | Two `date` fields + invariant covers it. |
| Fiscal/business calendars | Excluded | ISO calendar only. |
| Leap seconds | Excluded | NodaTime `Instant` uses smoothed UTC. Not a limitation. |
| Parameterized temporal types | Excluded | No type parameterization. |

---

## Implementation Scope

### Parser / Tokenizer

- Add `localdate`, `localtime`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `localdatetime` as type keywords.
- Add `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds` as unit keywords for postfix expressions.
- Add `inZone` as dot-accessor keyword for timezone mediation.
- **Single-quoted typed constants:** New token type `TypedConstantLiteral`. The tokenizer matches `'[^']*'` as a single token. The parser validates content shape and infers the specific type. Content inside single quotes is a constant pattern, not an expression — no operator precedence, no identifier resolution. `'2026-03-15'` is valid. `'03/15/2026'` is a compile error with a teachable message. Compile-time validated per inferred type. Temporal types are the first inhabitants; the mechanism is not temporal-specific.
- **Postfix unit expressions:** New expression form. Two combinators:
  - `PostfixUnitExprBare`: `<integer-literal> <unit-keyword>`. Must precede `NumberAtom` in atom chain (both start with `NumberLiteral`).
  - `PostfixUnitExprParen`: `( <expression> ) <unit-keyword>`. Must precede `ParenExpr` in atom chain (both start with `LeftParen`). Uses `.Try()` — backtracking on missing unit keyword is clean.
- Unit keywords in postfix position must not be consumed as identifiers.
- Parse `.inZone(tz)` dot-accessor form for timezone mediation on `instant` and `localdatetime` types.
- Parse accessors: `.year`, `.month`, `.day`, `.dayOfWeek`, `.hour`, `.minute`, `.second`, `.localdate`, `.localtime`, `.localdatetime`, `.totalHours`, `.totalMinutes`, `.totalSeconds`, `.instant`, `.timezone`, `.years`, `.months`, `.weeks`, `.days`, `.hours`, `.minutes`, `.seconds`, `.hasDateComponent`, `.hasTimeComponent`.

### Type Checker

- 8 new type entries.
- **Period/duration split enforcement:** `localdate + period ✓`, `localdate + duration ✗`, `instant + duration ✓`, `instant + period ✗`. Standard type-checking — no custom dispatch.
- **Postfix unit type resolution:** Resolve `<value> <unit-keyword>` expressions to `period` or `duration` based on expression context. `localdate +` / `localdatetime +` context → `period`. `instant +` / `zoneddatetime +` context → `duration`. `months`/`years` → always `period`. Field default context → match declared field type. No context → compile error.
- **Typed constant type inference:** Determine specific type from content shape via the admission rule. Current inhabitants (temporal — first wave): `YYYY-MM-DD` without `T` = localdate, `HH:MM:SS` without `-` = localtime, `...T...` without `Z` or `[` = localdatetime, `...T...Z` = instant, `...T...[zone]` = zoneddatetime, IANA pattern = timezone. Framework is extensible to future inhabitants whose shapes pass the admission rule.
- **Meaningless combination warnings:** `localdate + 3 hours` (hours on a localdate) → warning. `localdate + 3 minutes` → warning.
- Full cross-type interaction matrix.
- `period` ordering rejection (`<`, `>`, `<=`, `>=`).
- `period` scaling rejection (`* integer`).
- `duration + period` / `period + duration` rejection.
- **Sub-day duration bridging:** `time + duration → time` allowed by type checker. See Decision #16.
- All v1 type-checker items apply.

### Expression Evaluator

- `localdate` arithmetic via `LocalDate.Plus(Period)`, `LocalDate.Minus(Period)`.
- `localdate - localdate` via `Period.Between(d1, d2)`.
- `period` arithmetic via `Period + Period`, `Period - Period`.
- `localtime + period` via `LocalTime.Plus(Period)` — NodaTime native.
- `localtime + duration` via nanosecond arithmetic on `LocalTime` — thin translation (Decision #16).
- `localtime - localtime` via `Period.Between(t1, t2)` → time-component period.
- `instant` arithmetic via `Instant.Plus(Duration)`, `Instant.Minus(Duration)`.
- `duration` arithmetic via `Duration` operators.
- `localdatetime` arithmetic via `LocalDateTime.Plus(Period)`. `localdatetime + duration` uses duration bridging — same thin translation as `localtime + duration` (Decision #16).
- `zoneddatetime` timeline arithmetic via underlying instant.
- Postfix unit resolution: bare/paren postfix → `Period.FromDays(n)`, `Duration.FromHours(n)`, etc., based on resolved type.
- `.inZone(tz)` mediation: `instant.inZone(tz)` via `Instant.InZone(DateTimeZone)`, `localdatetime.inZone(tz)` via `LocalDateTime.InZoneLeniently(DateTimeZone)`.

### Runtime Engine

- Value carriers for all 8 temporal types.
- `period` serialization via `PeriodPattern.Roundtrip`.
- Constraint enforcement, timezone validation at fire boundary.

### TextMate Grammar

- Add all 8 type keywords to `typeKeywords`.
- Add all 7 unit keywords to appropriate keyword group.
- Add single-quoted literal pattern: `'[^']*'` as typed constant token.
- Add `inZone` as dot-accessor keyword.

### Language Server

- Completions, hover, diagnostics for all 8 types including full `period` (date+time components).
- Single-quote trigger for typed constant completions with format-aware ghost text.
- Period-specific diagnostics (ordering rejection, scaling rejection, cross-domain rejection for `instant + period`).
- Sub-day bridging documentation in hover for `time + duration`.

### MCP Tools

- `precept_language`: All 8 types. Postfix unit system description. Typed constant delimiter syntax.
- `precept_compile`/`fire`/`inspect`/`update`: `period` as ISO 8601 string (`P1Y2M3D`), `duration` as time-duration (`PT72H`).

### Samples, Documentation, Tests

- Update samples with `period` field type usage.
- All documentation sync per copilot-instructions.
- Full test coverage for period/duration split enforcement.

---

## Acceptance Criteria

### `localdate` type

- [ ] `field X as localdate` parses and type-checks.
- [ ] `'2026-03-15'` validates at compile time as a localdate. `'2026-02-30'` is a compile error.
- [ ] `localdate + 30 days`, `+ 3 months`, `+ 1 year`, `+ 2 weeks` work (postfix units resolve to `period` in localdate context).
- [ ] `localdate + (GraceDays) days` works (paren postfix resolves to `period`).
- [ ] `localdate + period → localdate`, `localdate - period → localdate` work for any `period`.
- [ ] `localdate - localdate → period` (not integer, number, or duration).
- [ ] `localdate + integer`, `localdate + number`, `localdate + decimal` are type errors.
- [ ] **`localdate + duration` is a type error** with teachable message.
- [ ] **`localdate + 3 hours` produces a warning** (hours meaningless on localdates).
- [ ] `.year`, `.month`, `.day`, `.dayOfWeek` return `integer`.
- [ ] Nullable, default (single-quoted), serialization work.

### `localtime` type

- [ ] `localtime + 3 hours`, `+ 30 minutes`, `+ 45 seconds` wrap at midnight.
- [ ] `localtime + duration → localtime` works (sub-day bridging, Decision #16).
- [ ] `localtime + period → localtime` works (`LocalTime.Plus(Period)` — time components used, date components ignored).
- [ ] `localtime - localtime → period` (returns time-component period with `.hours`, `.minutes`, `.seconds`).
- [ ] `.hour`, `.minute`, `.second` return `integer`.

### `instant` type

- [ ] `instant - instant → duration`.
- [ ] `instant + duration → instant`.
- [ ] `instant + 3 days` works (postfix unit resolves to `Duration.FromDays(3)` in instant context).
- [ ] `instant + (DayCount) days` works (paren postfix resolves to `Duration.FromDays(n)` in instant context).
- [ ] `instant + 72 hours + 30 minutes` works (postfix units resolve to `duration`).
- [ ] **`instant + period` is a type error** (covers `+ 3 months`, `+ (n) months`, `+ (n) years`).
- [ ] **`instant + 3 months` is a type error** (`months` always resolves to `period`, never `duration`).
- [ ] `instant.year` is a compile error — skip-level accessor requires `.inZone(tz)` first.
- [ ] `instant.inZone(tz)` produces `zoneddatetime`.
- [ ] `instant.inZone(tz).localdate` works (full dot chain).
- [ ] `instant.localdate` is a compile error with teachable message: "instant has no .localdate — use instant.inZone(tz).localdate".

### `duration` type

- [ ] `72 hours`, `30 minutes`, `3600 seconds` produce `duration` in appropriate contexts.
- [ ] **`duration + period` is a type error.**
- [ ] Arithmetic, comparison, scaling, ratio work.
- [ ] `.totalHours`, `.totalMinutes`, `.totalSeconds` return `number`.

### `period` type

- [ ] `30 days`, `3 months`, `1 year`, `2 weeks` produce `period` in appropriate contexts.
- [ ] `period + period → period`, `period - period → period`.
- [ ] **`1 month != 30 days` — structural equality.**
- [ ] **`period < period` is a type error.**
- [ ] **`period * integer` is a type error.**
- [ ] **`period + duration` is a type error.**
- [ ] `.years`, `.months`, `.weeks`, `.days`, `.hours`, `.minutes`, `.seconds` return `integer`.
- [ ] `.hasDateComponent`, `.hasTimeComponent` return `boolean`.
- [ ] `field X as period default 12 months` parses.
- [ ] `field X as period default 2 years + 6 months` parses.

### `timezone` type

- [ ] Compile-time validation on single-quoted IANA identifiers, fire-time validation on event args.
- [ ] `==`, `!=` work. `<`, `>` are type errors.

### `zoneddatetime` type

- [ ] Timeline arithmetic (`+ duration`, `+ 3 days`, `+ 72 hours`) works.
- [ ] **`+ period` is a type error** (including `+ 3 months`, `+ (n) months`).
- [ ] Single-quoted literal `'2026-01-15T14:30:00[America/New_York]'` parses.
- [ ] `.localdatetime` returns `localdatetime`, `.localdate` returns `localdate`, `.localtime` returns `localtime`.
- [ ] `.instant` returns `instant`, `.timezone` returns `timezone`.
- [ ] Accessors, serialization work.

### `localdatetime` type

- [ ] `+ period` and `+ duration` both work (bridge type).
- [ ] `localdatetime + 30 days` works (postfix unit resolves to `period` in localdatetime context).
- [ ] `localdatetime - localdatetime → period`.
- [ ] `.localdate` returns `localdate`, `.localtime` returns `localtime`.
- [ ] `localdatetime.inZone(tz)` produces `zoneddatetime`.
- [ ] `localdatetime.inZone(tz).instant` works (reverse navigation — replaces `pin`).

### Timezone mediation — `.inZone(tz)` dot accessor

- [ ] `instant.inZone(tz) → zoneddatetime` works.
- [ ] `localdatetime.inZone(tz) → zoneddatetime` works.
- [ ] `instant.inZone(tz).localdate` works (full dot-chain extraction).
- [ ] `instant.inZone(tz).localtime` works.
- [ ] `instant.inZone(tz).localdatetime` works.
- [ ] `localdatetime.inZone(tz).instant` works (reverse navigation).
- [ ] `instant.localdate` is a compile error — strict hierarchy, no skip-level accessors.
- [ ] `instant.localdatetime` is a compile error.
- [ ] `localdate.inZone(tz)` is a type error — `localdate` has no time component for mediation.
- [ ] `localtime.inZone(tz)` is a type error — `localtime` has no date component for mediation.
- [ ] DST resolution is deterministic (lenient resolver).

### Postfix unit literals (Decision #17 — rewritten)

- [ ] **Bare postfix:** `<integer-literal> <unit-keyword>` parses.
- [ ] **Paren postfix:** `( <expression> ) <unit-keyword>` parses.
- [ ] `localdate + 30 days` resolves `30 days` to `period` (`Period.FromDays(30)`).
- [ ] `instant + 3 days` resolves `3 days` to `duration` (`Duration.FromDays(3)`).
- [ ] `instant + (DayCount) days` resolves to `Duration.FromDays(n)` in instant context.
- [ ] `instant + 72 hours` resolves to `Duration.FromHours(72)`.
- [ ] `field X as period default 30 days` resolves postfix unit in field-type context.
- [ ] `field X as duration default 8 hours` resolves postfix unit in field-type context.
- [ ] Combination: `2 years + 6 months` works with standard `+` operator.
- [ ] **Juxtaposition: `2 years 6 months` (without `+`) → compile error.**
- [ ] **Ambiguous standalone: `set X = 3 days` with no typed context → compile error.**
- [ ] **Variable bare postfix: `GraceDays days` → compile error** with teachable message directing to `(GraceDays) days`.
- [ ] **Meaningless combination: `localdate + 3 hours` → warning.**
- [ ] **`instant + 3 months` → type error** (`months` never resolves to `duration`).

### Single-quoted typed constants (Decision #18 — rewritten)

- [ ] `'2026-01-15'` parses as a `localdate` literal.
- [ ] `'14:30'` and `'14:30:00'` parse as `localtime` literals. Seconds optional.
- [ ] `'2026-01-15T14:30:00Z'` parses as an `instant`. `'2026-01-15T14:30:00'` (no Z) parses as `localdatetime`.
- [ ] `'2026-01-15T14:30:00[America/New_York]'` parses as a `zoneddatetime`.
- [ ] `'America/New_York'` parses as a `timezone`.
- [ ] **Compile-time validation:** `'2026-13-45'` is a compile error with a teachable message.
- [ ] **Not an expression:** Content inside single quotes is NOT parsed as an expression — `'2024-01-15'` does NOT resolve `2024-01-15` as subtraction.
- [ ] **Double-quoted string distinction:** `"2026-01-15"` remains a string. `'2026-01-15'` is a date. Using the wrong delimiter produces a teachable error.
- [ ] **IntelliSense:** `'` triggers typed constant completions with format-aware ghost text.
- [ ] **Hover content:** `'2024-01-15'` shows "localdate: January 15, 2024 (Monday)" in invariant culture English.
- [ ] **Timezone completions:** `'` in timezone context triggers IANA timezone completion from `DateTimeZoneProviders.Tzdb.Ids`.

### Cross-type enforcement

- [ ] `date + duration` → type error.
- [ ] `instant + period` → type error.
- [ ] `duration + period` → type error.
- [ ] `period < period` → type error.
- [ ] `period == duration` → type error.
- [ ] `time + duration → time` (sub-day bridging — NOT a type error).
- [ ] `time + period → time` (NodaTime native — NOT a type error).
- [ ] All "Not supported" tables produce type errors, not runtime exceptions.

---

## Forward Design: Temporal Types as the Gateway Beyond Primitives

Temporal types are not just a feature — they are the **gateway** to Precept's evolution beyond primitive types. The literal mechanisms established in this proposal — the single-quoted typed constant delimiter (`'...'`) and the postfix quantity system (`N unit`) — are the language's **expansion joints** for all future non-primitive types. Every non-primitive value in Precept enters through one of three doors: `"..."` (string), `'...'` (typed constant), or `N unit` (quantity). When a future type is proposed, the question is "which door?" — not "does it need new syntax?"

The `<number> <unit>` postfix literal pattern and the `'...'` typed constant delimiter are both foundational grammar patterns that naturally extend to other business-domain types. This section documents the extensibility implications so that temporal design decisions account for the broader framework they are establishing.

### Why this matters now

The inventory unit-of-measure use case makes this concrete. A single SKU in an inventory system has multiple valid units with entity-specific conversion factors:

| UOM Context | Unit | Conversion | Example |
|-------------|------|------------|---------|
| Purchasing | case | 1 case = 24 each | Buy in cases |
| Stocking | each | base unit | Store as individual items |
| Pricing | each | $4.17/each | Retail price |
| Issuing | six-pack | 1 six-pack = 6 each | Sell as six-packs |

The critical insight: "case" means 24 for beer and 12 for wine. Conversion factors are **data on the entity**, not universal constants. This is fundamentally different from temporal units (which are globally defined by NodaTime) or physical units (which are globally defined by UCUM/SI).

### Natural extension domains

| Domain | Example literals | Unit registry | Complexity |
|--------|-----------------|---------------|------------|
| **Currency/money** | `100 USD`, `50.25 EUR` | ISO 4217 (3-letter codes, ~180 active) | Low |
| **Percentage** | `10 percent` or `10%` | Single unit | Trivial |
| **Physical quantity** | `5 kg`, `150 lbs` | UCUM subset | Medium |
| **Entity-scoped units** | `24 each`, `5 cases` | Per-entity `units` block | High |

Each domain follows the grammar shapes established by this proposal — entering through one of the three literal doors:

**Door 2 — Typed constant (`'...'`):** For values with a shape-recognizable constant form. Future candidates beyond temporal: UUID (`'550e8400-...'` — `8-4-4-4-12` hex), email (`'user@example.com'` — `@` present), URI (`'https://...'` — scheme `://`), semver (`'2.1.0'` — `N.N.N`). Each qualifies only if its content shape is distinguishable from all existing inhabitants via the admission rule.

**Door 3 — Postfix quantity (`N unit`):** For magnitude+unit values. Temporal types proved the pattern; future domains extend it:
1. **Postfix literal** for the common case (`100 USD`, `24 each`)
2. **Paren postfix** for computed values (`(Subtotal) USD`, `(Quantity) each`)
3. **Context-dependent resolution** where a unit could resolve differently depending on field type

**Door 1 — String (`"..."`):** For values with no distinguishing shape. Three-letter currency codes like `USD` are indistinguishable from other short strings — so currency *amounts* use postfix (`100 USD`), but standalone currency codes remain strings.

The two-mechanism design — the typed constant delimiter (`'...'`) for shape-recognizable constants and the postfix quantity system (`N unit`) for magnitude+unit values — covers **all** anticipated future non-primitive types without requiring new grammar concepts. When a future type is proposed, the first question is always: "which door?" — not "does it need new syntax?"

### Unit registry model — three resolution scopes

The temporal design establishes the pattern; future domains fill new resolution scopes:

| Scope | Source of truth | Examples | Resolution timing |
|-------|----------------|---------|-------------------|
| **Language-level** | Backing library (NodaTime) | `days`, `hours`, `months` | Compile-time — closed set |
| **Standard registry** | External standard (ISO 4217, UCUM subset) | `USD`, `EUR`, `kg`, `lbs` | Compile-time — large but closed set |
| **Entity-scoped** | `units` block in precept definition | `each`, `case`, `six-pack` | Compile-time within the precept — conversions are entity data |

The grammar `<number> <unit>` is the same across all three scopes. What changes is where the unit identifier resolves from.

### Design implications for temporal decisions

These implications inform the temporal decisions being made now:

1. **Unit suffix as resolvable identifier.** The unit keyword in `<number> <unit>` should be treated as an identifier resolved from a scope chain — not a hard-coded keyword list. `days` resolves from the temporal scope (NodaTime). `USD` would resolve from a standard registry scope (ISO 4217). `cases` would resolve from the entity's own `units` block. Same grammar, different resolution.

2. **Context-dependent resolution generalizes.** The mechanism designed for period vs. duration resolution (`3 days` → `period` in date context, `duration` in instant context) is the same mechanism needed for entity-scoped unit disambiguation (`24 items` → quantity-in-purchasing-units vs. quantity-in-stocking-units depending on field declaration).

3. **`convert()` boundary enforcement.** Not needed for temporal types (NodaTime handles unit compatibility internally), but essential for entity-scoped units where the type checker must require explicit conversion at unit boundaries (`convert(issueQty, baseUnit)`). The temporal grammar should not preclude adding conversion enforcement later.

4. **IntelliSense scalability.** Temporal unit completion is a finite list from NodaTime. Future domains need completion from open-ended sets — standard registries (ISO 4217 currencies) and per-entity declarations. The completion infrastructure should be designed for pluggable unit sources.

### Three design levels for quantity types

| Level | Description | Example | Precept fit |
|-------|-------------|---------|-------------|
| **A — Scalars with names** | Numbers + field names carry semantics | `field unitPrice as number` | Works today. No type-level help. |
| **B — Tagged numbers** | Fields declare their unit; type checker enforces unit compatibility and requires explicit conversion | `field quantityOnHand as quantity in baseUnit` | Sweet spot for Precept — entity declares its own unit system, type checker enforces it |
| **C — Unit algebra** | Compound types, dimensional cancellation | `field unitPrice as money per issueUnit` | Powerful but significant type system feature |

**Recommendation:** Level B is the natural Precept-shaped answer. The entity definition IS the type system — the entity's conversion table is declared right in the precept, so the type checker knows the conversion graph at compile time. This is the intersection nobody has built: a DSL where the entity declares its own unit system, and the type checker uses those declarations to enforce correctness.

### Standards landscape for future reference

| Standard | Maintainer | Scope | TZDB analog? |
|----------|-----------|-------|-------------|
| **ISO 4217** | ISO | Currency codes (~180 active) | Yes — `USD` is as unambiguous as `'America/New_York'` |
| **UCUM** | Regenstrief Institute | All units — SI, customary, dimensionless | Yes — formal grammar, parseable codes, canonical registry |
| **QUDT** | NASA / TopQuadrant | Units + quantities + dimensions as linked data | Broader — full ontology with dimensional analysis |
| **UN/CEFACT Rec 20** | United Nations | Trade/commerce units | Code table, not a live database |

For .NET: **UnitsNet** (~150 quantity types, 2000+ conversions) is the dominant library, analogous to NodaTime's role for temporal types.

### What exists today (gap analysis)

No programming language combines entity-scoped unit declarations, type-level enforcement, and explicit conversion requirements. Physics-oriented UOM systems (F# units of measure, Frink, Boost.Units) use global constants. ERP systems (SAP, Oracle, Dynamics 365) handle entity-scoped units as runtime data with no type-level enforcement. Precept's structural advantage — the entity definition IS the type system — positions it to close this gap.

### Scope boundary

This section is a **forward design note**, not a commitment. It documents why the literal mechanism decisions in this proposal carry more weight than temporal types alone — they establish the language's entire framework for moving beyond primitives. Temporal types are the gateway: they prove the typed constant delimiter, the postfix quantity system, and the zero-constructor discipline. The actual design of future types (UUID, email, currency, physical quantity) belongs in separate proposals when demand warrants it. The temporal decisions should be made with this framework in mind, but should not be over-engineered to serve hypothetical future needs.
