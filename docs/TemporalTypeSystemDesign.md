# Temporal Type System Design — NodaTime-Aligned

> **This is the canonical design document for Precept's temporal type system.** It is maintained alongside the implementation. For the implementation proposal and acceptance criteria, see [Issue #107](https://github.com/sfalik/Precept/issues/107).

**Author:** Frank (Lead/Architect, Language Designer)
**Date:** 2026-04-15 (v4 — bare English naming, dot-accessor mediation, strict hierarchy)
**Status:** Design — canonical reference
**Supersedes:** Issue #26 (`date` type as standalone proposal), v1 (collapsed `Period`), v2 (constructor-based literals), v3 (function-call timezone mediation)

**Related artifacts:**
- **NodaTime exception surface audit:** [`research/language/expressiveness/nodatime-exception-surface-audit.md`](../research/language/expressiveness/nodatime-exception-surface-audit.md)
- **Temporal research trail:** [`research/language/README.md` § Temporal type research trail](../research/language/README.md#temporal-type-research-trail)

---

## Summary

Add eight temporal types to the Precept DSL — `date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, and `datetime` — backed by NodaTime as a runtime dependency. Include a single timezone mediation operation (`.inZone(tz)`) accessed via dot syntax, and single-quoted typed constants for all temporal values — both formatted constants and quantities. Together, these types, literal forms, and the dot-accessor mediation chain give domain authors the vocabulary to express calendar constraints, SLA enforcement, multi-timezone compliance rules, and elapsed-time tracking — all within the governing contract, with no temporal logic delegated to the hosting layer. The literal mechanism architecture is defined in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md) — the canonical design for Precept's two-door literal model.

**Temporal quantity construction** uses typed constants with optional interpolation:
- **Static quantities:** `'30 days'`, `'72 hours'`, `'12 months'` — value + unit name inside `'...'`
- **Interpolated quantities:** `'{GraceDays} days'`, `'{X + 5} hours'` — `{expr}` interpolation inside `'...'`
- **Combined quantities:** `'2 years + 6 months + 15 days'` — `+` combination inside `'...'`

**Formatted temporal constants** use the single-quoted `'...'` delimiter — the typed constant delimiter — with type inferred from content shape. Temporal types are the first inhabitants of this mechanism; the delimiter is not temporal-specific:
- `'2026-06-01'` (date), `'14:30:00'` (time), `'2026-04-13T14:30:00Z'` (instant), `'2026-04-13T09:00:00'` (datetime), `'2026-04-13T14:30:00[America/New_York]'` (zoneddatetime), `'America/New_York'` (timezone)

**Timezone mediation** uses a single dot-accessor operation — `.inZone(tz)` — that produces a `zoneddatetime`, with navigation to local types via dot chains:
- `myInstant.inZone(tz)` → `zoneddatetime`
- `myInstant.inZone(tz).date` → `date`
- `myInstant.inZone(tz).time` → `time`
- `myInstant.inZone(tz).datetime` → `datetime`
- `myLDT.inZone(tz).instant` → reverse navigation back to `instant`

**Type hierarchy (strict — no skip-level accessors):**
```
instant                              ← universal timeline point
  ↓ .inZone(tz)       ↑ .instant
zoneddatetime                        ← date + time + timezone
  ↓ .datetime     ↑ .inZone(tz)
datetime                        ← date + time (no timezone)
  ↓ .date  ↓ .time        ↑ date + time
date       time            ← date / time (no timezone)
```

Plus orthogonal value types:
```
duration    ← timeline arithmetic (hours, minutes, seconds)
period      ← calendar arithmetic (years, months, days)
timezone    ← IANA timezone identifier
```

**What changed in v5:** The literal mechanism architecture transitioned from a three-door model to a **two-door model** (see [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md)):
1. **Two-door model** — Door 3 (bare postfix keywords: `30 days`, `(GraceDays) days`) is eliminated. Quantities enter through Door 2 as typed constants: `'30 days'`, `'{GraceDays} days'`.
2. **`{expr}` interpolation** — Both `"..."` and `'...'` support `{expr}` interpolation, always-on (no prefix), because `{` has no structural meaning in Precept.
3. **Unit names are no longer keywords** — `days`, `hours`, `minutes`, `seconds`, `months`, `years`, `weeks` are validated strings inside `'...'`, not reserved words.
4. **Type-family admission rule** — Content shape determines a type family (finite set); context narrows within the family. Replaces the strict self-identifying property.

**What changed in v4:** Six locked decisions from the 2026-04-15 session revised the type surface and timezone mediation:
1. **Bare English naming** (D10, revised) — `date`, `time`, `datetime` use natural English words. The absence of a `zoned` prefix signals "no timezone" — the bare name is the default; `zoneddatetime` is the marked form that carries additional information.
2. **Dot-vs-function rule** (D8) — "If the value owns it, dot. If the operation is freestanding, function." `inZone` is a dot accessor because the instant owns the mediation to a timezone. Component accessors (`.year`, `.date`, etc.) are dot because the value owns its components.
3. **`inZone` as dot accessor** (D11) — One mediation operation, accessed via dot: `myInstant.inZone(tz)`. Replaces three standalone functions (`toLocalDate`, `toLocalTime`, `toInstant`). Navigation via dot chains: `.date`, `.time`, `.datetime`.
4. **Strict hierarchy** (D12) — No skip-level accessors. `instant.date` is a compile error — must go `instant.inZone(tz).date`. Forces explicit timezone mediation at every boundary crossing.
5. **`pin` eliminated** (D13) — Reverse navigation from `datetime` → `instant` uses `ldt.inZone(tz).instant`. One mediation operation in both directions. `pin` was redundant with dot-chain composition.
6. **Instant restricted semantics** (D18) — `instant` supports comparison, duration arithmetic, and `.inZone(tz)` navigation — NO component accessors, no period arithmetic. An instant is a UTC timeline point with no calendar context.

**What changed in v3:** Four locked decisions from the earlier 2026-04-15 session rewrote the literal surface:
1. **Unified postfix model** — All 7 function-call constructors (`days()`, `months()`, `hours()`, etc.) eliminated. Postfix was the sole quantity construction syntax. *(v5 further evolved this: postfix quantities moved into `'...'` typed constants — see v5 changes above.)*
2. **`+` as sole combiner** — Composite juxtaposition (`2 years 6 months`) eliminated. Only `'2 years + 6 months'`.
3. **No duration/period constructor literals** — `duration(PT72H)` and `period(P1Y6M)` eliminated. Typed constant quantities are the only surface.
4. **Single-quoted typed constants** — `date(2026-01-15)` constructor form replaced by `'2026-01-15'`. All 6 formatted temporal types use the `'...'` typed constant delimiter with type inferred from content shape.

**What changed in v2:** Shane's owner directive: *"No obscurity, expose NodaTime. Don't reinvent the wheel. Force authors to think about the details."* The v1 proposal collapsed `Period` and `Duration` into a single surface type (`duration`) with custom dispatch rules. This was wrong. NodaTime deliberately keeps `Period` and `Duration` as separate types because they represent fundamentally different quantities — calendar distance vs. timeline distance. The v2 proposal exposes this distinction faithfully: `period` and `duration` are separate surface types, calendar units resolve to `period`, timeline units resolve to `duration`, and the type checker inherits NodaTime's enforcement for free. No custom operator dispatch. No re-inventing the wheel.

NodaTime exists because `System.DateTime` lets you be implicit about what your temporal data means. Precept exists because scattered service-layer code lets you be implicit about what your business rules mean. Both libraries are responses to the same failure mode: **implicit behavior creates bugs; explicit behavior creates predictability.**

This shared philosophy is the lens through which every type decision in this proposal is made:

| Precept applies it to... | NodaTime applies it to... | The shared principle |
|---|---|---|
| `date` over `string` | `LocalDate` over `DateTime` | Be explicit that this is a calendar date |
| `instant` over `number` | `Instant` over `DateTime` | Be explicit that this is a point on the timeline |
| `timezone` over `string` | `DateTimeZone` over `string` | Be explicit about the allowed values |
| `duration` over `integer` | `Duration` over `TimeSpan` | Be explicit about what fixed-length units mean |
| `period` over `integer` | `Period` over `int monthCount` | Be explicit that calendar units have variable length |
| `time` over `integer` | `LocalTime` over `int minutesSinceMidnight` | Be explicit that this is a time of day |
| `datetime` over `string` | `LocalDateTime` over `DateTime` | Be explicit about combined date+time without timezone |

Precept's design principles ground this directly:

- **Principle #1 (Deterministic, inspectable model):** NodaTime's type separation makes it structurally clear which operations are deterministic and which require external context. All types proposed here are deterministic by construction — `instant` comparison is nanosecond math, `date` arithmetic uses the fixed ISO calendar, and the `.inZone(tz)` dot accessor makes the TZ database input explicit in the expression.
- **Principle #2 (English-ish but not English):** The DSL names — `date`, `time`, `instant`, `duration`, `period`, `timezone` — communicate exactly what the data is. The bare name is the default (no timezone); `zoneddatetime` is the marked form. `field FiledAt as instant` needs no comment. Typed constant quantities extend this: `DueDate + '30 days'` reads as domain prose.
- **Principle #8 (Sound, compile-time-first static analysis):** NodaTime's type separation enables the compiler to catch temporal misuse statically. `date + instant` is a type error. `instant + period` is a type error. `instant.year` is a compile error. Crucially, the `period`/`duration` split means the compiler rejects `instant + 1 month` because `months` always resolves to `period` and instants only accept `duration` — NodaTime's enforcement inherited for free.
- **Principle #12 (AI is a first-class consumer):** Named temporal types with precise semantics give AI consumers a vocabulary to reason about entity data and generate correct precepts. An AI that sees `field DueDate as date` knows the field supports calendar arithmetic — it does not need to infer this from naming conventions on a `string` field.

The governing question for every decision: **"If a domain author has this kind of data, does giving it a named type help them be explicit about what it means?"**

### Temporal types as the gateway beyond primitives

Precept's type system today consists of primitives: `string`, `number`, `integer`, `decimal`, `boolean`. Temporal types are the **first step beyond primitives** — the gateway to a richer type vocabulary that lets domain authors express what their data *is*, not just what storage shape it occupies. The literal mechanisms established here — single-quoted typed constants (`'...'`) with `{expr}` interpolation — are not temporal features. They are the language's **expansion joints** for all future non-primitive types. The canonical design for these mechanisms lives in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md). Every design decision here carries weight beyond temporal: the type-family admission rule for typed constants, the context-dependent resolution for quantities, and the zero-constructor discipline all set precedent for how the language grows.

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

**After** — with temporal types (v5: quoted typed constants + bare English naming):

```precept
field DueDate as date default '2026-06-01'
field GracePeriod as period default '30 days'

# Type-safe: DueDate + MealsTotal is a compile error
# Explicit: '30 days' resolves to period, date + period → date
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
invariant FiledAt - IncidentAt <= '72 hours' because "HIPAA: must file within 72 hours of incident"
```

**Before** — multi-timezone compliance pushes logic to hosting layer:

```precept
field FilingDeadline as number  # Pre-computed by hosting layer
# The MEANING of this deadline (30 days, midnight, incident timezone) is NOT in this file
invariant FiledAt <= FilingDeadline because "Filing deadline has passed"
```

**After** — complete rule in the contract (v5: dot-accessor mediation, quoted quantities, `date + period → date`):

```precept
field IncidentTimestamp as instant
field FiledTimestamp as instant
field IncidentTimezone as timezone

# Dot-chain mediation: instant → .inZone(tz) → zoneddatetime → .date → date
# Then calendar arithmetic, reconstruct back via .inZone(tz).instant
invariant FiledTimestamp <= (
    IncidentTimestamp.inZone(IncidentTimezone).date + '30 days' + '23:59:00'
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
field LoanTerm as period default '12 months'
field StartDate as date default '2026-01-15'

# date + period → date. The period carries its calendar semantics.
# 12 months != 365 days — NodaTime's truth, exposed faithfully.
invariant StartDate + LoanTerm >= '2026-01-01' because "Maturity date must be in the future"
```

The second form satisfies the philosophy's "one file, complete rules" guarantee. An auditor reads the precept and sees the entire business rule — 30 days, 11:59 PM, incident timezone.

### What happens if we don't build this

- 56 calendar date fields remain encoded as strings or numbers. The compiler cannot distinguish a date from a price.
- SLA and compliance timing rules stay in the hosting layer. The contract has a visible gap in its primary target domains (insurance, healthcare, finance).
- Day-counter simulation events remain as boilerplate in 3+ samples.
- The "one file, complete rules" philosophy claim is undermined for any domain with temporal constraints.
- The literal mechanism framework (typed constant delimiter with interpolation, type-family admission rule) that would serve all future non-primitive types has no proving ground — future type proposals would each need to invent their own syntax.

---

## NodaTime as Backing Library

### Why NodaTime, not System.DateOnly / TimeOnly / DateTime

NodaTime is adopted as a runtime dependency for the entire temporal type system. The DSL author never sees NodaTime type names — `field DueDate as date` is the surface, `NodaTime.LocalDate` is the implementation, just as `field Amount as decimal` has `System.Decimal` behind it.

**Rationale:**

1. **Philosophy alignment.** NodaTime's core design philosophy is: *"We want to force you to think about decisions you really need to think about. In particular, what kind of data do you really have and really need?"* (NodaTime 3.3.x Design Philosophy). Distinct types for distinct temporal concepts — `LocalDate` is not `Instant` is not `Period` is not `Duration`. This is Precept's prevention guarantee applied to temporal data.

2. **Type separation enables compile-time safety.** NodaTime makes it structurally impossible to mix a calendar date with a global timestamp, or a calendar period with a timeline duration. Precept's type checker inherits this separation: `date + instant` is a type error. `instant + period` is a type error. No custom dispatch needed.

3. **Battle-tested arithmetic.** `LocalDate.PlusDays(int)`, `LocalDate.Plus(Period.FromMonths(n))`, month-end truncation, leap-year handling, `Period.Between(d1, d2)` — all rigorously tested since 2012. Building the same guarantees on `System.DateOnly` would require Precept to own temporal arithmetic correctness, a domain Precept has no expertise in.

4. **The `Period`/`Duration` split is the decisive factor.** The v1 proposal collapsed `Period` into `Duration` and had to build custom unit-compatibility dispatch. This was re-inventing enforcement logic that NodaTime already provides through type separation. The v2+ proposal exposes the separation directly: calendar units resolve to `period`, timeline units resolve to `duration`, and the type checker simply checks `date + period ✓`, `instant + duration ✓`, `instant + period ✗`. Zero custom dispatch.

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

### `date`

**What it makes explicit:** This is a calendar date — not a timestamp, not a string that looks like a date. Day-granularity arithmetic is meaningful. "2026-03-15" means the same calendar day everywhere. No timezone is attached — `date` is the bare default; use `zoneddatetime` when timezone context is needed.

**Backing type:** `NodaTime.LocalDate`

**Declaration:**

```precept
field DueDate as date default '2026-06-01'
field FilingDate as date nullable
field ContractEnd as date default '2099-12-31'
```

**Single-quoted literal:** `'2026-03-15'` — the single-quote delimiter signals a typed constant. Type is inferred from content shape: `YYYY-MM-DD` (digits and hyphens, no `T`) → `date`. The date's content shape is what qualifies it for the typed constant delimiter — distinguishable from every other inhabitant. Content is validated at compile time. `'2026-03-15'` is valid. `'03/15/2026'` is a compile error with a teachable message. See Locked Decision #18.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `date ± period dateonly` | `date` | Calendar arithmetic. `LocalDate.Plus/Minus(Period)`. Handles truncation. Period must be provably date-only — NodaTime throws on time components; see Decision #26. |
| `date ± period` (unconstrained) | **compile error** | An unconstrained period might contain time parts like hours. Use `period dateonly` or add specific units like `'30 days'`. |
| `date ± '30 days'` | `date` | Typed constant quantity — `'30 days'` resolves to `period` in date context. |
| `date ± '{GraceDays} days'` | `date` | Interpolated typed constant — variable expression resolves to `period` in date context. |
| `date ± '3 months'` | `date` | Truncates at month end (Jan 31 ± 1 month = Feb 28). |
| `date ± '1 year'` | `date` | Leap years (Feb 29 ± 1 year = Feb 28). |
| `date ± '2 weeks'` | `date` | = 14 days. |
| `date + time` | `datetime` | Combines a calendar date with a time of day. `LocalDate + LocalTime → LocalDateTime`. NodaTime native. |
| `date - date` | `period` | Calendar distance. `Period.Between(d1, d2)`. Preserves structural components. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | NodaTime default behavior. Thin wrapper — no custom logic. |

| **Not supported** | **Why** |
|---|---|
| `date + date` | You can't add two dates together. To get the time between them, use `DueDate - FilingDate`. To shift a date forward, use `DueDate ± 3 days`. |
| `date ± integer` | A bare number doesn't specify a unit. Use `DueDate ± '2 days'`. See Locked Decision #10. |
| `date ± decimal` / `date ± number` | A bare number doesn't specify a unit. Use `DueDate ± '{n} days'`. |
| `date ± duration` | Durations measure hours, minutes, and seconds — they can't be added to a date. Use `DueDate ± 3 days` to shift a date. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.year` | `integer` | Calendar year |
| `.month` | `integer` | Month (1–12) |
| `.day` | `integer` | Day of month (1–31) |
| `.dayOfWeek` | `integer` | ISO day of week (Monday=1, Sunday=7) |

**Constraints:** `nullable`, `default '...'` (single-quoted date). Numeric constraints (`nonnegative`, `min`, `max`, `maxplaces`, `minlength`, `maxlength`) are compile errors on `date`.

**Serialization:** ISO 8601 string: `"2026-03-15"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `field X as date default "2026-01-01"` | Date values use single quotes, not double quotes. Use `default '2026-01-01'`. |
| `'2026-02-30'` | Invalid date: February 30 does not exist. |
| `'01/15/2026'` | Dates must be written as YYYY-MM-DD. Use `'2026-01-15'`. |
| `DueDate + FilingDate` | You can't add two dates together. To get the time between them, use `DueDate - FilingDate`. To shift a date forward, use `DueDate + '(n) days'`. |
| `DueDate + 2` | A bare number doesn't specify a unit. Use `DueDate + '2 days'` to add 2 days. |
| `DueDate + '3 hours'` | Hours don't apply to a date — dates have no time of day. Did you mean `DueDate + '3 days'`? |

---

### `time`

**What it makes explicit:** This is a time of day — not a duration, not a timestamp, not an integer encoding minutes-since-midnight. No timezone is attached — `time` is the bare default.

**Backing type:** `NodaTime.LocalTime`

**Declaration:**

```precept
field AppointmentTime as time default '09:00:00'
field CheckInTime as time nullable
```

**Single-quoted literal:** `'14:30:00'` — content shape `HH:mm:ss` (colons without hyphens-before-T) → `time`. The time's content shape is what qualifies it for the typed constant delimiter — distinguishable from all other inhabitants. Seconds may be omitted: `'14:30'` is valid (implies `:00`). See Locked Decision #18.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `time ± period timeonly` | `time` | `LocalTime.Plus/Minus(Period)`. Period must be provably time-only — NodaTime throws on date components; see Decision #26. |
| `time ± period` (unconstrained) | **compile error** | An unconstrained period might contain date parts like months. Use `period timeonly` or add specific units like `3 hours`. |
| `time ± duration` | `time` | Sub-day bridging. Wraps at midnight. Runtime: nanosecond arithmetic on `LocalTime` (see Decision #16). For sub-day units, `duration` and `period` represent identical physical quantities — the type checker bridges this. |
| `time ± '3 hours'` | `time` | Typed constant quantity — resolves via sub-day bridging. Wraps at midnight. |
| `time ± '30 minutes'` | `time` | Same bridging. Wraps at midnight. |
| `time ± '45 seconds'` | `time` | Same bridging. Wraps at midnight. |
| `time + date` | `datetime` | Commutative form of `date + time`. Same result: `LocalDate + LocalTime → LocalDateTime`. |
| `time - time` | `period` | Time-component period between two times. `Period.Between(t1, t2)` returns period with `.hours`, `.minutes`, `.seconds` components. NodaTime faithful. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | NodaTime default behavior. Thin wrapper — no custom logic. |

**Note:** `time ± period` requires the period to be provably time-only (Decision #26). NodaTime's `LocalTime.Plus(Period)` **throws `ArgumentException`** on periods with non-zero date components — it does NOT silently ignore them. The `timeonly` constraint or literal time-unit analysis provides the compile-time proof. `time + 5 days` is a compile error. `time + 3 hours` is valid (literal analysis proves time-only).

| **Not supported** | **Why** |
|---|---|
| `time + time` | You can't add two times together. To get the difference between them, use `EndTime - StartTime`. To shift a time forward, use `StartTime ± 3 hours`. |
| `time ± integer` | What unit? Use `'{n} hours'` or `'{n} minutes'`. |
| `time ± '1 day'` / `time ± 'N days'` | Days can't be added to a time — times only support hours, minutes, and seconds. |
| `time ± 'N months'` / `time ± 'N years'` | Same — months and years don't apply to a time. |
| `time ± SomePeriodField` (unconstrained) | This period field may include date parts. Declare it as `period timeonly` to use it with times, or use a `duration` field instead. |

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
| `'25:00:00'` | Invalid time: hours must be 0–23. |
| `AppointmentTime + 30` | A bare number doesn't specify a unit. Use `StartTime + '30 minutes'` or `StartTime + '1 hour'`. |
| `AppointmentTime + '1 day'` | Days can't be added to a time — times only support hours, minutes, and seconds. |
| `AppointmentTime + SomePeriodField` | This period field may include date parts. Declare it as `period timeonly` to use it with times, or use a `duration` field instead. |

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
| `instant ± duration` | `instant` | Point offset. `Instant.Plus/Minus(Duration)`. |
| `instant ± '3 days'` | `instant` | Typed constant quantity — `'3 days'` resolves to `Duration.FromDays(3)` in instant context. |
| `instant ± '{DayCount} days'` | `instant` | Interpolated typed constant — variable expression resolves to `Duration.FromDays(n)` in instant context. |
| `instant ± '72 hours'` | `instant` | Typed constant quantity — `'72 hours'` resolves to `Duration.FromHours(72)` in instant context. |
| `instant ± '30 minutes'` | `instant` | Typed constant quantity — resolves to `Duration.FromMinutes(30)`. |
| `instant ± '45 seconds'` | `instant` | Typed constant quantity — resolves to `Duration.FromSeconds(45)`. |
| `instant ± '2 weeks'` | `instant` | Typed constant quantity — `'weeks'` resolves to `Duration.FromDays(14)` in instant context. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | NodaTime default behavior. Thin wrapper — no custom logic. |

| **Not supported** | **Why** |
|---|---|
| `instant.year`, `.month`, `.hour`, etc. | **Compile error.** An instant doesn't have calendar components — it needs a timezone first. Use `myInstant.inZone(tz).date.year` (Decision #22). |
| `instant.date`, `instant.time` | **Compile error.** An instant doesn't have a date or time without a timezone. Use `myInstant.inZone(tz).date` (Decision #22). |
| `instant ± integer` | A bare number doesn't specify a unit. Use `FiledAt ± '1 hour'` or `FiledAt ± '3600 seconds'`. |
| `instant ± period` | Periods can't be added to an instant — instants use fixed-length durations. Navigate to a local type first: `FiledAt.inZone(tz).datetime ± myPeriod`, then `.inZone(tz).instant` to convert back. For day-count periods, use `'{GracePeriod.days} days'` to convert to duration. |

**Note on `days`/`weeks` in instant context:** `instant ± '3 days'` and `instant ± '{n} days'` are both valid — `days` and `weeks` resolve to `duration` (`Duration.FromDays`) in instant context. This is the key capability of context-dependent type resolution: the same unit name (`days`) resolves to `period` in calendar contexts and `duration` in timeline contexts. `months` and `years` have no duration equivalent (no `Duration.FromMonths` in NodaTime), so they always resolve to `period`.

**Overflow note:** `Instant.Plus(Duration)` can overflow if a Duration pushes the result outside NodaTime's representable range (`-9998-01-01T00:00:00Z` to `9999-12-31T23:59:59Z`). NodaTime throws `OverflowException` at runtime. The compiler cannot catch this statically because field values are only known at runtime. This is an accepted edge case — the range covers all practical business dates.

**Navigation via `.inZone(tz)` (the sole mediation path):**

`.inZone(tz)` is the ONLY dot-accessor on `instant`. When the author types `myInstant.`, the completion list shows exactly one entry: `.inZone(tz)`. The completion detail should explain WHY this is the only option: *"An instant is a point on the timeline with no timezone. Use `.inZone(tz)` to see it as a local date and time."* This teaches the timezone mediation rule proactively rather than waiting for the author to attempt `instant.date` and hit an error.

| Expression | Produces | Description |
|---|---|---|
| `instant.inZone(tz)` | `zoneddatetime` | "View this moment in this timezone." The single mediation operation. |
| `instant.inZone(tz).date` | `date` | Calendar date at this moment in this timezone. |
| `instant.inZone(tz).time` | `time` | Time of day at this moment in this timezone. |
| `instant.inZone(tz).datetime` | `datetime` | Combined local date+time in this timezone. |
| `instant.inZone(tz).year` | `integer` | Calendar year — same as `.date.year`. |

**Accessors:** `.inZone(tz)` only. No component accessors. Deliberately empty — to get calendar components, navigate via `.inZone(tz)`.

**Constraints:** `nullable`, `default '...'` (single-quoted instant).

**Serialization:** ISO 8601 UTC string: `"2026-04-13T14:30:00Z"`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `'2026-04-13T14:30:00'` in instant context | Instants must end with Z to indicate UTC. Use `'2026-04-13T14:30:00Z'`. |
| `FiledAt.year` | An instant doesn't have a year — it needs a timezone first. Use `FiledAt.inZone(tz).date.year`. |
| `FiledAt.date` | An instant doesn't have a date without a timezone. Use `FiledAt.inZone(tz).date`. |
| `FiledAt + 3600` | A bare number doesn't specify a unit. Use `FiledAt + '1 hour'` or `FiledAt + '3600 seconds'`. |
| `FiledAt + '1 month'` | Months vary in length and can't be added to an instant. Convert to a date first: `FiledAt.inZone(tz).date + '1 month'`, then convert back. |

---

### `duration`

**What it makes explicit:** This is an elapsed amount of time — a fixed count of nanoseconds on the timeline. Not a calendar interval, not an hour-count encoded as an integer.

**Backing type:** `NodaTime.Duration`

**What v2 changed:** In v1, `duration` held both calendar units (months, years) and timeline units (hours, minutes) — requiring custom dispatch. In v2+, `duration` holds **only timeline units**. Calendar units live in `period`. The type checker handles the rest.

**Construction via postfix units:**

```precept
'72 hours'              # 72 hours as a duration
'30 minutes'            # 30 minutes
'3600 seconds'          # 3600 seconds
'{SlaHours} hours'      # variable — interpolated typed constant
```

These resolve to `Duration.FromHours`, `Duration.FromMinutes`, `Duration.FromSeconds` respectively.

**Combined durations:**

```precept
'5 hours + 30 minutes + 15 seconds'    # 5 hours, 30 minutes, 15 seconds
```

**Duration and period have no constructor literal form** (Locked Decision — 2026-04-15). ISO 8601 duration notation (`PT72H`, `PT8H30M`) is specialist serialization syntax designed for machines, not humans. The postfix unit system with `+` combination handles every duration expressible in ISO 8601:

| ISO notation | Precept equivalent |
|---|---|
| `PT72H` | `'72 hours'` |
| `PT2H30M` | `'2 hours + 30 minutes'` |
| `PT0.5S` | Not directly expressible — temporal typed constant quantities require integer values (Decision #28). Use millisecond-precision via `'500 milliseconds'` if sub-second support is added, or model as a `duration` field with a computed value. |

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `duration ± duration` | `duration` | Combined elapsed time / difference. |
| `duration * integer` or `duration * number` | `duration` | Scaling. NodaTime: `Duration * long`, `Duration * double`. |
| `integer * duration` or `number * duration` | `duration` | Commutative form. NodaTime supports both operand orders. |
| `duration / integer` or `duration / number` | `duration` | Scaling. Divisor safety applies (see below). |
| `duration / duration` | `number` | Ratio (e.g., how many shifts fit). Divisor safety applies (see below). |
| `-duration` | `duration` | Unary negation. `Duration.Negate()`. |

**Division-by-zero prevention:** Division by zero on `duration` throws `DivideByZeroException` in NodaTime. Precept catches this via the unified narrowing system (Issue #106):

- **Literal zero:** `duration / 0` is a compile **error** — provably always wrong.
- **Unproven field divisor:** `duration / Field` where no nonzero proof exists emits a compile **warning**. Proofs come from any of: `positive` constraint, explicit `rule Field > 0`, `when Field != 0` guard, or `in State ensure Field > 0`.
- **Compound expressions:** `duration / (Rate + 1)` assumes satisfiable — no warning (consistent with #106's conservatism).

Duration division is not a special case — it inherits the same divisor safety as numeric division. The only temporal-specific note is that `DivideByZeroException` (instead of IEEE 754 `Infinity`) makes the runtime consequence more severe for duration than for float.
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | NodaTime default behavior. Thin wrapper — no custom logic. |

| **Not supported** | **Why** |
|---|---|
| `duration * duration` | You can't multiply two durations together. Did you mean `SomeDuration * 2` to double it? |
| `duration * decimal` | Durations can only be multiplied by whole numbers or `number`. |
| `duration / decimal` | Durations can only be divided by whole numbers or `number`. |
| `duration ± period` | Durations (hours, minutes, seconds) and periods (days, months, years) can't be mixed. |
| `duration ± integer` | A bare number doesn't specify a unit. Use `SomeDuration ± '30 minutes'` or construct a duration with a unit like `'72 hours'`. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.totalDays` | `number` | Total elapsed 24-hour days (may be fractional). |
| `.totalHours` | `number` | Total elapsed hours (may be fractional). |
| `.totalMinutes` | `number` | Total elapsed minutes. |
| `.totalSeconds` | `number` | Total elapsed seconds. |

**As a declared field type:**

```precept
field EstimatedHours as duration default '8 hours'
field ActualHours as duration default '0 hours'
```

9 fields across 6 samples (MTBF, repair hours, work hours) are naturally `duration`.

**Constraints:** `nullable`, `default '8 hours'` or `default '30 minutes'`, `nonnegative`. `min`/`max` are compile errors — constraint values are bare numbers, not duration quantities.

**Implementation note:** `nonnegative` desugars to `Field >= 0` for numeric types, which would be `duration >= integer` — a type error. The desugar model must be type-aware: for `duration`, generate a comparison against `Duration.Zero` instead of the integer literal `0`.

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
'30 days'               # 30 calendar days
'3 months'              # 3 months — NOT 90 days
'1 year'                # 1 year — NOT 365 days
'2 weeks'               # 2 weeks (= 14 days)
'{GraceDays} days'      # variable — interpolated typed constant
```

These resolve to `Period.FromDays`, `Period.FromMonths`, `Period.FromYears`, `Period.FromWeeks` respectively.

**Combined periods:**

```precept
'1 year + 6 months'               # 1 year and 6 months
'3 months + 15 days'              # 3 months and 15 days
```

**Period has no constructor literal form** (Locked Decision — 2026-04-15). ISO 8601 period notation (`P1Y6M`, `P2W`) is specialist serialization syntax. The postfix unit system with `+` combination handles every period expressible in ISO 8601:

| ISO notation | Precept equivalent |
|---|---|
| `P1Y6M` | `'1 year + 6 months'` |
| `P1Y6M15D` | `'1 year + 6 months + 15 days'` |
| `P30D` | `'30 days'` |
| `PT5H` | `'5 hours'` (in period field context) |
| `P1DT12H` | `'1 day + 12 hours'` |

**Decision: Precept's `period` is the full NodaTime `Period` — date AND time components (v2.1 correction).** The v2 proposal restricted `period` to date-only components. This created a structural contradiction with NodaTime's `LocalTime` API: `LocalTime.Plus(Period)` is the native arithmetic method, and `Period.Between(LocalTime, LocalTime)` returns time-component periods. The date-only restriction forced `time` arithmetic through `duration` via a non-existent `LocalTime.Plus(Duration)` method. The full NodaTime `Period` resolves all three contradictions and honors the "expose NodaTime faithfully" directive. See Locked Decision #13 (revised).

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `period ± period` | `period` | Combined calendar period / difference. `Period ± Period`. |
| `-period` | `period` | Unary negation. `-(3 months)` is equivalent to negating the period. |
| `==`, `!=` | `boolean` | **Structural equality.** `1 month != 30 days` — structurally different. NodaTime's equality is structural. |

**Compiler warning — always-false literal comparisons:** When both operands of a period `==` are constant expressions with non-overlapping components (e.g., `1 month == 30 days`), the compiler emits a warning: *"This is always `false` — `1 month` and `30 days` use different parts. Period equality compares each part (years, months, days) separately."* The comparison is legal (structural equality is well-defined), but the result is statically knowable and almost certainly not what the author intended. Similarly, `!=` between non-overlapping constants warns that it's always `true`.

| **Not supported** | **Why** |
|---|---|
| `<`, `>`, `<=`, `>=` | Periods can't be compared with `<` or `>` — a month isn't always the same length, so ordering isn't reliable. See Locked Decision #14. |
| `period * integer` | Periods can't be multiplied. Write the value directly: `60 days` instead of `30 days * 2`. |
| `period ± duration` | Periods (days, months, years) and durations (hours, minutes, seconds) can't be mixed. |

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
field GracePeriod as period default '30 days'
field LoanTerm as period default '12 months'
field WarrantyLength as period default '2 years'
field NoticePeriod as period default '2 weeks'
field ExtendedWarranty as period default '2 years + 6 months'
```

10 period fields across 7 samples (`GracePeriodDays`, `TermLengthMonths`, `WarrantyMonths`, etc.) are currently `integer` surrogates. See Locked Decision #12.

**Constraints:** `nullable`, `default '30 days'` / `default '12 months'` / `default '2 years'` / `default '2 weeks'`, or combined: `default '2 years + 6 months'`. `timeonly` (hours/minutes/seconds only), `dateonly` (years/months/weeks/days only) — see Decision #26.

**Serialization:** ISO 8601 period string: `"P1Y2M3D"`, `"PT5H30M"`, `"P1MT2H"`. Via `PeriodPattern.Roundtrip`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `1 month > 30 days` | Periods can't be compared with `<` or `>` — is 1 month greater than 30 days? It depends on which month. |
| `1 month == 30 days` | This is `false` — `1 month` and `30 days` are written with different parts, so they're not equal. Period equality compares each part (years, months, days) separately. |
| `FiledAt + '1 month'` | Months vary in length and can't be added to an instant. Convert to a date first: `FiledAt.inZone(tz).date + '1 month'`, then convert back. |
| `GracePeriod * 2` | Periods can't be multiplied. Write the value directly, e.g. `'60 days'` instead of `GracePeriod * 2`. |

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

**Constraints:** `nullable`, `default '...'` (single-quoted IANA identifier). No ambient/global implicit timezone — defaults are explicit author intent per field.

**Serialization:** IANA identifier string: `"America/Los_Angeles"`.

**Teachable error messages:**

| Invalid code | Severity | Message |
|---|---|---|
| `'EST'` in timezone context | **Error** | 'EST' is a legacy IANA abbreviation, not a canonical timezone. Use `'America/New_York'` (or `'America/Panama'` if you need fixed UTC-5). |
| `'MST'`, `'CET'`, `'EET'`, `'MET'`, `'WET'`, `'HST'`, etc. | **Error** | Same pattern — legacy abbreviations are rejected. Use the canonical `Region/City` form. |
| `'Pacific Standard Time'` in timezone context | Error | 'Pacific Standard Time' is a Windows timezone name. Use `'America/Los_Angeles'`. |
| `'Not/A/Timezone'` | Error | 'Not/A/Timezone' is not a recognized timezone name. |

---

### `zoneddatetime`

**What it makes explicit:** A datetime with timezone context — an instant resolved to local date and time in a specific timezone.

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

# From datetime — "anchor this local reading to the timeline in this timezone"
-> set PinnedContext = DetectedAt.inZone(IncidentTimezone)
```

No standalone construction function exists. `zoneddatetime` is always reached via `.inZone(tz)` from either an `instant` or a `datetime`.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `zoneddatetime ± duration` | `zoneddatetime` | Timeline arithmetic on underlying instant. |
| `zoneddatetime ± '3 hours'` | `zoneddatetime` | Typed constant quantity — resolves to `duration`. |
| `zoneddatetime ± '30 minutes'` | `zoneddatetime` | Typed constant quantity — resolves to `duration`. |
| `zoneddatetime ± '45 seconds'` | `zoneddatetime` | Typed constant quantity — resolves to `duration`. |
| `zoneddatetime ± '3 days'` | `zoneddatetime` | Typed constant quantity — `days` resolves to `duration` in zoneddatetime context (timeline arithmetic). |
| `zoneddatetime ± '2 weeks'` | `zoneddatetime` | Typed constant quantity — `weeks` resolves to `duration` in zoneddatetime context. |
| `zoneddatetime ± '{N} hours'` | `zoneddatetime` | Interpolated typed constant — variable expression resolves to `duration`. |
| `zoneddatetime ± '{N} days'` | `zoneddatetime` | Interpolated typed constant — `days` resolves to `duration` in zoneddatetime context. |
| `zoneddatetime - zoneddatetime` | `duration` | Instant subtraction. |
| `==`, `!=` | `boolean` | NodaTime default behavior (`ZonedDateTime.Equals()` — compares instant + calendar + zone). Thin wrapper. |

| **Not supported** | **Why** |
|---|---|
| `zoneddatetime < zoneddatetime` (and `>`, `<=`, `>=`) | **No natural ordering.** NodaTime deliberately omits `IComparable<ZonedDateTime>` — ordering semantics are ambiguous (by instant? by local time?). Compare via accessor: `zdt1.instant < zdt2.instant` for timeline ordering, or `zdt1.datetime < zdt2.datetime` for wall-clock ordering. |
| `zoneddatetime ± period` | Periods can't be added directly to a zoned datetime. Navigate to `.datetime` first: `(myZdt.datetime ± myPeriod).inZone(myZdt.timezone)`. |
| `zoneddatetime ± integer` | A bare number doesn't specify a unit. Use an explicit unit like `'3 days'` or `'1 hour'`. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.instant` | `instant` | The underlying UTC point. |
| `.timezone` | `timezone` | The bound IANA timezone. |
| `.datetime` | `datetime` | Local date+time in bound timezone. |
| `.date` | `date` | Local calendar date in bound timezone. |
| `.time` | `time` | Local time in bound timezone. |
| `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`, `.dayOfWeek` | `integer` | Local components in bound timezone. |

**Constraints:** `nullable`, `default '...'` (single-quoted zoneddatetime literal with bracket-enclosed timezone).

**Serialization:** Two-property JSON: `{ "instant": "..Z", "timezone": "..." }`.

**Teachable error messages:**

| Invalid code | Error message |
|---|---|
| `zdt + '1 month'` | Periods can't be added directly to a zoned datetime. Navigate to `.datetime` first: `(myZdt.datetime + '1 month').inZone(myZdt.timezone)`. |
| `zdt + 5` | A bare number doesn't specify a unit. Use an explicit unit like `'3 days'` or `'1 hour'`. |

---

### `datetime`

**What it makes explicit:** A date and time together — not a point on the global timeline, not two separate fields. No timezone is attached — `datetime` is the bare default; use `zoneddatetime` when timezone context is needed.

**Backing type:** `NodaTime.LocalDateTime`

**Declaration:**

```precept
field DetectedAt as datetime nullable
field ScheduledFor as datetime default '2026-04-13T09:00:00'
```

**Single-quoted literal:** `'2026-04-13T09:00:00'` — content shape includes `T` but no trailing `Z` and no bracket-enclosed timezone → `datetime`. The presence of `T` without `Z` or `[` is the distinguishing shape signal. See Locked Decision #18.

**Operators:**

| Expression | Produces | Rationale |
|---|---|---|
| `datetime ± period` | `datetime` | Calendar arithmetic. `LocalDateTime.Plus/Minus(Period)`. Accepts all component categories — no constraint required. |
| `datetime ± '30 days'`, `± '3 months'`, `± '1 year'`, `± '2 weeks'` | `datetime` | Typed constant quantities — resolve to `period` in datetime context. |
| `datetime ± '3 hours'`, `± '30 minutes'`, `± '45 seconds'` | `datetime` | Typed constant quantities — resolve to `period` in datetime context (Decision #9). `LocalDateTime.Plus(Period.FromHours(3))`. Overflow advances the date naturally. |
| `datetime - datetime` | `period` | `Period.Between(ldt1, ldt2)`. |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | `boolean` | NodaTime default behavior. Thin wrapper — no custom logic. |

Note: `datetime ± period` accepts all component categories — `LocalDateTime.Plus/Minus(Period)` has no restrictions. Time-unit typed constants (`'3 hours'`, `'30 minutes'`, `'45 seconds'`) resolve to `period` in `datetime` context via context-dependent resolution (Decision #9), NOT to `duration`. This means `datetime + '3 hours'` becomes `LocalDateTime.Plus(Period.FromHours(3))` — NodaTime-native, overflow advances the date naturally (e.g., `'2026-01-15T23:00:00' + '3 hours'` → `'2026-01-16T02:00:00'`).

| **Not supported** | **Why** |
|---|---|
| `datetime ± integer` | A bare number doesn't specify a unit. Use `AppointmentTime + '2 hours'` or `AppointmentTime + '3 days'`. |
| `datetime ± duration` | Durations are for instant arithmetic, not datetimes. Use units directly: `AppointmentTime + 2 hours` or `AppointmentTime + 3 days`. |

**Navigation via `.inZone(tz)` (reverse mediation — local → universal):**

| Expression | Produces | Description |
|---|---|---|
| `datetime.inZone(tz)` | `zoneddatetime` | "Anchor this local reading to the timeline in this timezone." |
| `datetime.inZone(tz).instant` | `instant` | Reverse navigation to UTC instant. Replaces the eliminated `pin` function. |

**Accessors:**

| Accessor | Returns | Description |
|---|---|---|
| `.date` | `date` | Date component. |
| `.time` | `time` | Time component. |
| `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second` | `integer` | Direct components. |
| `.dayOfWeek` | `integer` | ISO day of week (Monday=1, Sunday=7) |

**Constraints:** `nullable`, `default '...'` (single-quoted datetime).

**Serialization:** ISO 8601 without timezone: `"2026-04-13T14:30:00"`.

#### Decision: `datetime` included (locked v4)

`datetime` is included in the initial temporal type surface. It completes the local type vocabulary, provides the bridge type that accepts all `period` component categories without constraint, and is required by 6 NIST compliance fields in the security-incident sample. The full spec and ACs above are active.

---

## Timezone Mediation — `.inZone(tz)` Dot Accessor

### The single mediation operation

All timezone mediation flows through one operation — `.inZone(tz)` — accessed via dot syntax on the value that owns it. There are no standalone conversion functions.

| Expression | Produces | Semantics |
|---|---|---|
| `instant.inZone(tz)` | `zoneddatetime` | "View this moment in this timezone." |
| `datetime.inZone(tz)` | `zoneddatetime` | "Anchor this local reading to the timeline in this timezone." |

From `zoneddatetime`, navigation to any local type or back to `instant` uses dot accessors on the result:

| Dot chain | Produces | Semantics |
|---|---|---|
| `myInstant.inZone(tz).date` | `date` | "What date is it at this moment in this timezone?" |
| `myInstant.inZone(tz).time` | `time` | "What time is it at this moment in this timezone?" |
| `myInstant.inZone(tz).datetime` | `datetime` | "What local date+time is it at this moment in this timezone?" |
| `myInstant.inZone(tz).year` | `integer` | "What year is it at this moment in this timezone?" |
| `myLDT.inZone(tz).instant` | `instant` | "What UTC moment corresponds to this local date+time?" (reverse navigation) |

### Why one operation, not three functions

The v3 proposal had three standalone conversion functions: `toLocalDate(instant, tz)`, `toLocalTime(instant, tz)`, `toInstant(date, time, tz)`. Shane locked three decisions that collapsed these to one:

1. **D8 — Dot-vs-function rule:** "If the value owns it, dot." An instant owns its mediation to a timezone. Therefore `instant.inZone(tz)`, not `inZone(instant, tz)`.

2. **D11 — `inZone` as dot accessor:** `toLocalDate(instant, tz)` was a freestanding function doing two things: mediate timezone AND extract date. With dot chains, these are separate operations: `instant.inZone(tz)` (mediate) then `.date` (extract). Cleaner separation of concerns. Better IntelliSense — after typing `.inZone(tz).`, the completion list shows all available components.

3. **D13 — `pin` eliminated:** The reverse direction (`datetime → instant`) was originally a separate function `pin(ldt, tz)`. But `datetime.inZone(tz).instant` achieves the same result via the same `.inZone(tz)` operation. One mediation operation in both directions — no reason for two.

### Strict hierarchy — no skip-level accessors (Decision #22)

Navigation must follow the type hierarchy step by step. No skip-level accessors exist.

```
instant → .inZone(tz) → zoneddatetime → .datetime → datetime → .date / .time
                                       → .date → date
                                       → .time → time
                                       → .instant → instant (reverse)
```

| Valid | Invalid (compile error) | Why |
|---|---|---|
| `myInstant.inZone(tz).date` | `myInstant.date` | Skips timezone mediation — where's the timezone? |
| `myInstant.inZone(tz).year` | `myInstant.year` | Same — year depends on timezone. |
| `myLDT.inZone(tz).instant` | `myLDT.instant` | Skips timezone mediation — which timezone? |
| `zdt.datetime.date` | — | Valid: step-by-step navigation. |
| `zdt.date` | — | Also valid: `zoneddatetime` provides direct `.date` as one-step navigation within the zone-aware tier. |

**Rationale:** The strict hierarchy forces explicit timezone mediation at every boundary crossing between the universal tier (`instant`) and the local tier (`date`, `time`, `datetime`). This prevents the most common temporal bug: extracting calendar components from a UTC timestamp without specifying which timezone to interpret it in.

**Exception:** `zoneddatetime` provides `.date`, `.time`, and component accessors (`.year`, etc.) directly because it already carries a timezone. The timezone mediation has already happened — the hierarchy step was the `.inZone(tz)` call that produced the `zoneddatetime`.

### DST ambiguity resolution

- **Gap (spring forward):** Map to the instant *after* the gap. NodaTime's `LenientResolver`.
- **Overlap (fall back):** Map to the *later* instant. Safer for deadline calculations.

**Recommendation:** Lenient resolver. Deterministic. The 99% case is unaffected.

### Composability with existing operations

The `.inZone(tz)` accessor produces and consumes existing types — all existing operations chain naturally:

- `myInstant.inZone(tz).date` returns `date` — all date operations work.
- `myInstant.inZone(tz).time` returns `time` — all time operations work.
- `myLDT.inZone(tz).instant` returns `instant` — all instant operations work.

**Example — complete multi-timezone compliance rule (dot form):**

```precept
field ClinicTimezone as timezone default 'America/New_York'
field ShiftStart as instant
field ShiftLocalDateTime as datetime
field ShiftLocalDate as date
field ShiftLocalTime as time

# Timezone mediation — dot-accessor chains
-> set ShiftLocalDateTime = ShiftStart.inZone(ClinicTimezone).datetime
-> set ShiftLocalDate = ShiftStart.inZone(ClinicTimezone).date
-> set ShiftLocalTime = ShiftStart.inZone(ClinicTimezone).time
-> set IsOvernight = ShiftStart.inZone(ClinicTimezone).time >= '22:00'

# Reverse navigation (pin eliminated — use dot chain)
-> set BackToInstant = ShiftLocalDateTime.inZone(ClinicTimezone).instant
```

---

### Temporal Quantity Construction — Typed Constants

**Status:** Locked (Decision #17 — rewritten 2026-04-17). Quantities enter through Door 2 as typed constants. See [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md) for the canonical two-door model.

Temporal quantities are expressed as typed constants inside `'...'`, with optional `{expr}` interpolation. All bare postfix keywords (`30 days`, `(GraceDays) days`) and all function-call constructors (`days()`, `months()`, etc.) have been eliminated.

#### Three forms

**Static quantity** — literal value + unit name inside `'...'`:

```precept
set DueDate = CreatedDate + '30 days'
invariant FiledAt - IncidentAt <= '72 hours' because "SLA"
field GracePeriod as period default '30 days'
```

**Interpolated quantity** — `{expr}` inside `'...'`:

```precept
set Deadline = StartDate + '{GraceDays} days'       # variable argument
set SlaLimit = '{SlaHours * 2} hours'                # computed expression
set Expiry = StartDate + '{TermMonths + 6} months'   # arithmetic inside interpolation
```

**Combined quantity** — `+` inside `'...'`:

```precept
field ExtendedWarranty as period default '2 years + 6 months'
set Expiry = StartDate + '1 year + 3 months + 15 days'
invariant FiledAt - IncidentAt <= '72 hours + 30 minutes' because "SLA window"
```

Unit names: `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`. These are **not** language keywords — they are validated strings inside `'...'`. See [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md) § Quantity unit names.

**Integer requirement:** The magnitude must evaluate to an integer. `'0.5 days'` is a compile error: *"Unit values must be whole numbers. Use smaller units for fractions: `'12 hours'` for half a day."* This restriction applies to temporal unit names; future non-temporal quantity domains may support non-integer magnitudes. See Decision #28.

**Left-associative `+` subtlety:** `date + '1 month' + '1 month'` applies sequential truncation (Jan 31 → Feb 28 → Mar 28). `date + '1 month + 1 month'` builds the period first, then applies once (Jan 31 → Mar 31). Both are valid. `+` inside `'...'` builds a compound quantity; `+` outside `'...'` is sequential arithmetic.

#### Type resolution rules — context-dependent

The type of a quantity typed constant (`period` or `duration`) is determined by expression context. `'3 days'` is not inherently `period` or `duration` — the surrounding expression resolves it, using the type-family admission rule defined in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md).

**For `days` and `weeks` — context-dependent:**

| Expression context | `'3 days'` / `'{n} days'` resolves to | NodaTime call |
|---|---|---|
| `date ± _` | `period` | `Period.FromDays(3)` / `Period.FromDays(n)` |
| `datetime ± _` | `period` | `Period.FromDays(3)` / `Period.FromDays(n)` |
| `instant ± _` | `duration` | `Duration.FromDays(3)` / `Duration.FromDays(n)` |
| `zoneddatetime ± _` | `duration` | `Duration.FromDays(3)` / `Duration.FromDays(n)` |
| `time ± _` | **compile error** | "Days/weeks don't apply to a time — times have no date component." |
| `field X as period default _` | `period` | `Period.FromDays(3)` |
| `field X as duration default _` | `duration` | `Duration.FromDays(3)` |
| No context / ambiguous | **compile error** | "Can't determine the type of `'3 days'` without context. Use it in an expression like `DueDate ± '3 days'` so the type is clear." |

**For `months` and `years` — always `period`:**

`months` and `years` ALWAYS resolve to `period` regardless of expression context. No `Duration.FromMonths` or `Duration.FromYears` exists in NodaTime — months and years have variable nanosecond length.

| Expression context | `'3 months'` / `'{n} months'` resolves to | Result |
|---|---|---|
| `date ± _` | `period` | ✓ `date ± period → date` |
| `datetime ± _` | `period` | ✓ `datetime ± period → datetime` |
| `instant ± _` | `period` | ✗ **type error** — `instant ± period` |
| `zoneddatetime ± _` | `period` | ✗ **type error** — `zoneddatetime ± period` |
| `field X as period default _` | `period` | ✓ |
| `field X as duration default _` | `period` | ✗ **type error** |

**For `hours`, `minutes`, `seconds` — context-dependent (with `period` bridging):**

| Expression context | `'3 hours'` / `'{n} hours'` resolves to | NodaTime call |
|---|---|---|
| `instant ± _` | `duration` | `Duration.FromHours(3)` |
| `zoneddatetime ± _` | `duration` | `Duration.FromHours(3)` |
| `time ± _` | bridges to duration | Sub-day bridging (Decision #16) |
| `datetime ± _` | `period` | `Period.FromHours(3)` (Decision #9/#27) |
| `field X as duration default _` | `duration` | `Duration.FromHours(3)` |
| `field X as period default _` | `period` | `Period.FromHours(3)` |
| `date ± _` | **compile error** | "Hours don't apply to a date — dates have no time of day. Did you mean `datetime ± '3 hours'` or `DueDate ± '3 days'`?" |
| No context / ambiguous | **compile error** | "Can't determine the type of `'3 hours'` without context." |

**Owner's rationale (Shane):** "The author IS thinking about the details — they wrote `instant + '3 days'`, which means '3 fixed 24-hour days on the timeline.' The context makes the intent unambiguous."

#### Teachable error messages

| Invalid code | Error message |
|---|---|
| `set X = '3 days'` (no type context) | Can't determine the type of `'3 days'` on its own. Provide context: `DueDate + '3 days'` or `FiledAt + '3 days'`. |
| `GraceDays days` (bare, no quotes) | Quantities must be enclosed in single quotes. Use `'{GraceDays} days'` with interpolation. |
| `date + '3 hours'` | Hours don't apply to a date — dates have no time of day. Did you mean `datetime + '3 hours'` or `DueDate + '3 days'`? |
| `'2 years 6 months'` | Unit values must be combined with `+`. Use `'2 years + 6 months'`. |
| `instant + '3 months'` | Months vary in length and can't be added to an instant. Convert to a date first: `FiledAt.inZone(tz).date + '3 months'`, then convert back with `.inZone(tz).instant`. |

---

## Literal Mechanism Architecture

> The canonical design for Precept's literal system — the two-door model, admission rule, interpolation syntax, and expansion joints — lives in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md). This section summarizes the temporal-specific aspects.

Every non-primitive value in Precept enters through one of **two doors**:

| Door | Delimiter | What it carries | Type resolution | Temporal proof |
|------|-----------|----------------|-----------------|----------------|
| **1. String** | `"..."` | Text values with optional `{expr}` interpolation | Always `string` | N/A — strings remain strings |
| **2. Typed constant** | `'...'` | Formatted constants and quantities with optional `{expr}` interpolation | Inferred from content shape → type family → narrowed by context | `date`, `time`, `datetime`, `instant`, `zoneddatetime`, `timezone`, quantities (`'30 days'`, `'{GraceDays} days'`) |

**Zero constructors exist.** There is no `type(value)` form in the language — no `date(2026-01-15)`, no `duration(PT72H)`, no `money("100", "USD")`. Every value enters through one of the two doors above.

### The admission rule — type families

A type qualifies for Door 2 (`'...'`) **if and only if** its content shape determines a **type family** — a finite, enumerable set of types — that is disjoint from every other family. Context narrows within the family. No shape may match two families.

The current inhabitants demonstrate family disjointness:

| Inhabitant | Shape signal | Type family |
|------------|-------------|-------------|
| `date` | `YYYY-MM-DD` | `{date}` (singleton) |
| `time` | `HH:MM:SS` | `{time}` (singleton) |
| `datetime` | `...T...` | `{datetime}` (singleton) |
| `instant` | `...T...Z` | `{instant}` (singleton) |
| `zoneddatetime` | `...T...[...]` | `{zoneddatetime}` (singleton) |
| `timezone` | `Word/Word` | `{timezone}` (singleton) |
| Quantities | `<value> <unit>` | `{period, duration}` (narrowed by context) |
| State names | Plain identifier | `{state}` (validated against declarations) |

### Why two doors are sufficient

The two-door model covers all value categories a business DSL encounters:

- **Opaque identifiers with internal structure** (dates, times, UUIDs, emails, URIs) → Door 2: the structure *is* the shape signal.
- **Quantities with units** (durations, periods, currency amounts, physical measurements) → Door 2: `'<value> <unit>'` inside `'...'`.
- **Plain text** (names, descriptions, codes without distinguishing shape) → Door 1: the default.

No third category has been identified. The two-door model is closed by construction.

### Temporal types prove the framework

Temporal types are the **first inhabitants** of Door 2 — the gateway through which the language moves beyond primitives. They prove that:

1. **The typed constant delimiter works.** Six formatted types and quantity expressions coexist in `'...'` without collision. The admission rule holds.
2. **Interpolation works inside typed constants.** `'{GraceDays} days'` provides variable quantities through the same door as static `'30 days'`. No variable-quantity split.
3. **Unit names as validated strings work.** `days`, `hours`, etc. are validated inside `'...'` — no keyword reservation, no collision with identifiers.
4. **Zero constructors are sufficient.** Every temporal value that constructors would have expressed (`date(2026-01-15)`, `duration(PT72H)`) has a more readable representation through Door 2.

This is what makes temporal types the gateway: they don't just add temporal capability — they **validate the literal architecture** that all future non-primitive types will use.

---

## Semantic Rules

### Type-interaction matrix

**Calendar domain (uses `period`):**

| Left | Op | Right | Result | Notes |
|---|---|---|---|---|
| `date` | `±` | `period dateonly` | `date` | Calendar arithmetic. Truncation at month end. Period must be provably date-only (Decision #26) — NodaTime throws on time components. `date ± period` (unconstrained) is a compile error. |
| `date` | `-` | `date` | `period` | Calendar distance. `Period.Between`. |
| `period` | `±` | `period` | `period` | Combined / difference. |
| `datetime` | `±` | `period` | `datetime` | Calendar arithmetic. No constraint — `LocalDateTime.Plus/Minus(Period)` accepts all components. |
| `datetime` | `-` | `datetime` | `period` | Calendar distance. |
| `time` | `±` | `period timeonly` | `time` | `LocalTime.Plus/Minus(Period)`. Period must be provably time-only (Decision #26) — NodaTime throws on date components. `time ± period` (unconstrained) is a compile error. |

**Timeline domain (uses `duration`):**

| Left | Op | Right | Result | Notes |
|---|---|---|---|---|
| `instant` | `-` | `instant` | `duration` | Elapsed time. |
| `instant` | `±` | `duration` | `instant` | Offset forward/backward. |
| `duration` | `±` | `duration` | `duration` | Combined / difference. |
| `duration` | `*` | `integer`/`number` | `duration` | Scaling. |
| `duration` | `/` | `integer`/`number` | `duration` | Scaling. |
| `duration` | `/` | `duration` | `number` | Ratio. |
| `time` | `±` | `duration` | `time` | Wraps at midnight. |
| `time` | `-` | `time` | `period` | Time-unit period. |
| `zoneddatetime` | `±` | `duration` | `zoneddatetime` | Timeline arithmetic. |
| `zoneddatetime` | `-` | `zoneddatetime` | `duration` | Instant subtraction. |

**Composition (cross-type operations that produce a new type):**

| Left | Op | Right | Result | Notes |
|---|---|---|---|---|
| `date` | `+` | `time` | `datetime` | `LocalDate + LocalTime → LocalDateTime`. Decision #25. |
| `time` | `+` | `date` | `datetime` | Commutative — same result as `date + time`. |

**Note:** The v3 proposal had a "Bridge type" section listing `datetime ± duration → datetime`. Decision #27 reversed this — `datetime + duration` and `datetime - duration` are now **compile errors.** `datetime` uses `period` for all arithmetic; `duration` is reserved for timeline types (`instant`, `zoneddatetime`). See Decision #27.

### Comparison rules

| Type | `==`, `!=` | `<`, `>`, `<=`, `>=` | Ordering semantics |
|---|---|---|---|
| `date` | ✓ | ✓ | ISO calendar |
| `time` | ✓ | ✓ | Within-day |
| `instant` | ✓ | ✓ | Nanoseconds on global timeline |
| `duration` | ✓ | ✓ | Nanoseconds |
| `period` | ✓ | **✗** | **No natural ordering.** See Decision #14. |
| `timezone` | ✓ | ✗ | Equality by IANA identifier. |
| `datetime` | ✓ | ✓ | Same-calendar |
| `zoneddatetime` | ✓ | **✗** | **No natural ordering.** NodaTime omits `IComparable<ZonedDateTime>`. Compare via `.instant` or `.datetime` accessor. |

Cross-type comparison is always a type error.

### Cross-type arithmetic: what's NOT allowed (and why)

| Expression | Why it's a type error |
|---|---|
| `date ± duration` | Duration is for instants (timeline arithmetic). Dates need periods (calendar arithmetic). |
| `instant ± period` | Periods may contain calendar units with variable length. Instants need durations (fixed-length timeline arithmetic). |
| `duration ± period` | Durations (hours, minutes, seconds) and periods (days, months, years) can't be mixed. |
| `zoneddatetime ± period` | Periods can't be added directly to a zoned datetime — navigate to `.datetime` first. |
| `date ± integer` | A bare number doesn't specify a unit. Use `(n) days`. |
| `instant ± integer` | A bare number doesn't specify a unit. Use `(n) hours` or `(n) seconds`. |
| `period < period` | Periods can't be compared with `<` or `>`. |
| `zoneddatetime < zoneddatetime` | No natural ordering — NodaTime omits `IComparable<ZonedDateTime>`. Compare via `.instant` or `.datetime` accessor. |
| `period * integer` | Periods can't be multiplied. |
| `date + date` / `time + time` / `instant + instant` / `datetime + datetime` / `zoneddatetime + zoneddatetime` | Adding two points together isn't meaningful. Use `-` to get the distance between them. |
| `instant.year` | Requires timezone. Use `myInstant.inZone(tz).date.year`. |
| `instant.date` | Skips timezone mediation. Use `myInstant.inZone(tz).date`. |
| `duration * duration` | You can't multiply two durations together. |
| `duration * decimal` | Durations can only be multiplied by whole numbers. Use `number`. |
| `duration / decimal` | Durations can only be divided by whole numbers. Use `number`. |
| `date - time` | Subtraction doesn't reverse composition. `date + time → datetime`, but `date - time` is not the inverse. To decompose, use accessors: `myDatetime.date`, `myDatetime.time`. |

**Field-type-aware diagnostics:** When a cross-type error involves a field reference (not an inline literal), the error message should name the field and its declared type, then suggest the concrete fix. For example:

| Expression | With field reference | Improved message |
|---|---|---|
| `DueDate + Elapsed` | `Elapsed` is `duration` | "`Elapsed` is a duration (hours/minutes/seconds). Dates need calendar units like `days` or `months`. Use a `period` field instead, or add units directly: `DueDate + 3 days`." |
| `FiledAt + GracePeriod` | `GracePeriod` is `period` | "`GracePeriod` is a period (days/months/years). Instants need fixed-length units. Convert to a date first: `FiledAt.inZone(tz).date + GracePeriod`." |
| `AppointmentTime + Elapsed` | `Elapsed` is `duration`, `AppointmentTime` is `datetime` | "`Elapsed` is a duration. Datetimes need calendar units. Use a `period` field instead, or add units directly: `AppointmentTime + 2 hours`." |

The type checker already knows both operand types. When either operand is a field identifier, include the field name and declared type in the diagnostic. This is a presentation concern — the underlying type rules are unchanged.

### Nullable and default behavior

All temporal types support `nullable`. All follow existing null propagation rules.

| Type | Default value | Notes |
|---|---|---|
| `date` | `default '...'` | Author specifies (single-quoted date). |
| `time` | `default '...'` | Author specifies (single-quoted time). |
| `instant` | `default '...'` | Author specifies (single-quoted instant). |
| `duration` | `default '0 hours'` | Zero duration. |
| `period` | `default '0 days'` | Zero period. |
| `timezone` | `default '...'` | Author specifies (single-quoted IANA identifier). No ambient implicit — explicit per field. |
| `zoneddatetime` | `default '...'` | Author specifies (single-quoted zoneddatetime). No ambient implicit — explicit per field. |
| `datetime` | `default '...'` | Author specifies (single-quoted datetime). |

**`nullable` + `default`:** Open — George's Challenge #3. Deferred to cross-cutting decision.

---

## Locked Design Decisions

### 1. NodaTime as the backing library for all temporal types

- **Why:** Philosophy match; `Period`/`Duration` split eliminates custom dispatch; battle-tested arithmetic.
- **Alternatives rejected:** BCL types (no `Period` equivalent). Custom implementation (unmaintainable). Collapsed `Period`/`Duration` (v1 — requires custom dispatch, contradicts directive).
- **Precedent:** NRules inherits `System.Decimal`. Precept inherits NodaTime's temporal model.
- **Tradeoff:** Additional NuGet dependency (~1.1 MB).

### 2. Day granularity for `date`

- **Why:** "2026-03-15" means the same calendar day everywhere. No timezone dependency.
- **Alternatives rejected:** `datetime` as only calendar type. Optional timezone.
- **Precedent:** SQL `DATE`, FEEL `date()`.
- **Tradeoff:** Time-of-day needs separate `time` type or `datetime`.

### 3. ISO 8601 as the sole format

- **Why:** Single canonical format eliminates parsing ambiguity.
- **Alternatives rejected:** Configurable formats. Auto-detection.
- **Precedent:** Cedar (RFC 3339), FEEL, SQL.
- **Tradeoff:** Authors from `MM/DD/YYYY` regions must adapt.

### 4. Single-quoted delimiter is the typed constant delimiter — not temporal-specific (revised — locked 2026-04-15)

- **Why:** The `'...'` delimiter is the **typed constant delimiter** for any non-primitive type whose content shape is unambiguous — not a temporal-specific feature. The three literal mechanisms are a closed set: `"..."` (string), `'...'` (typed constant, shape-inferred), `N unit` (quantity). Zero constructors exist in the language. A type qualifies for `'...'` if and only if its content shape is distinguishable from every other single-quote inhabitant (the admission rule). Temporal types are the **first inhabitants**: `date` (`YYYY-MM-DD`), `time` (`HH:MM:SS`), `datetime` (`...T...`), `instant` (`...T...Z`), `zoneddatetime` (`...T...[zone]`), `timezone` (`Word/Word`). This follows Precept's literal grain: `"hello"` doesn't need `string("hello")`, so `'2026-01-15'` doesn't need `date(2026-01-15)`.
- **Alternatives rejected:** (A) `date(2026-01-15)` (unquoted constructor — original Decision #4/18) — the team's unanimous initial recommendation. Shane challenged it with the string-delimiter precedent insight: if strings use `"..."` without a `string()` wrapper, temporal values should use a delimiter too. See "Double-Quote Alternative Analysis" below. (B) `date("2026-01-15")` (string constructor) — quotes suggest "a string being parsed." (C) Bare ISO `2024-01-15` — lexer ambiguity with subtraction. (D) `"2026-01-15"` (double-quoted, context-resolved) — explored and rejected; see "Double-Quote Alternative Analysis." (E) Sigil prefix (`#2026-01-15`) — non-obvious, not Precept's voice.
- **Precedent:** SQL `'2026-01-15'` for value literals. Python `b'...'` / `r'...'` for typed string variants with distinct delimiters.
- **Tradeoff:** Type is implicit in content shape rather than explicit in a keyword. Readers must recognize ISO formats. Mitigated by: (a) ISO date formats are culturally universal, (b) field declarations provide type context, (c) syntax highlighting colors single-quoted content distinctly.

**`char` type permanent exclusion (supporting decision):** Precept will never have a `char` type — characters are programming-language implementation details, not business-domain data. Single quotes are permanently reserved for the typed constant delimiter with zero future collision risk. Confirmed by Frank and George.

### 5. No timezone on `date`, `time`, or `datetime`

- **Why:** Calendar/clock types represent what's on a calendar or wall clock. The bare name is the unmarked default; `zoneddatetime` is the marked form that carries timezone context.
- **Alternatives rejected:** UTC-anchored dates. Timezone as required metadata.
- **Precedent:** NodaTime `Local*` types (`LocalDate`, `LocalTime`, `LocalDateTime`). SQL `DATE`, `TIME`, `DATETIME` — all timezone-free by default.
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

- **Why:** D8 (dot-vs-function rule): "If the value owns it, dot." An instant owns its mediation to a timezone. D11: `.inZone(tz)` produces a `zoneddatetime`; navigation to local types uses dot chains (`.date`, `.time`, `.datetime`). This replaces three standalone functions (`toLocalDate`, `toLocalTime`, `toInstant`) with one composable operation. Better IntelliSense: after `.inZone(tz).`, the completion list shows all available components.
- **Alternatives rejected:** Three standalone functions (`toLocalDate(instant, tz)`, `toLocalTime(instant, tz)`, `toInstant(date, time, tz)`) — v3 position. Conflated mediation+extraction in one call. `pin(ldt, tz)` for reverse direction — redundant with `ldt.inZone(tz).instant`.
- **Precedent:** NodaTime's own `Instant.InZone(DateTimeZone)` returns `ZonedDateTime`. Java's `Instant.atZone(ZoneId)` returns `ZonedDateTime`. The dot-accessor form is the universal pattern.
- **Tradeoff:** Longer expression for simple cases: `myInstant.inZone(tz).date` vs. `toLocalDate(myInstant, tz)`. The verbosity is intentional — two separate operations (mediate, then extract) are visually distinct.

### 9. Determinism is relative to the runtime environment

- **Why:** "Same inputs = same output" includes TZ database version.
- **Alternatives rejected:** Excluding all timezone operations. Mandatory TZ pinning.
- **Precedent:** Every timezone-sensitive system manages TZ database freshness.
- **Tradeoff:** Determinism input surface includes TZ database version.

### 10. `date + integer` is a type error — use `date + (n) days`

- **Why:** `date + 2` is implicit — what does `2` mean? NodaTime requires `LocalDate.Plus(Period.FromDays(n))` — the `Period` makes the unit visible. Now that `period` is a surface type, `(n) days` resolves to `period` and the type checker validates `date + period`. The shortcut `date + integer` would bypass this type-level enforcement.
- **Alternatives rejected:** `date + integer` meaning "add N days" — the v1 position. Convenient but implicit. In a proposal with both `(n) days` and `(n) months` as period constructors, `+ 5` is genuinely ambiguous. NodaTime's `LocalDate` has no `operator+(int)` — it requires `Plus(Period)`.
- **Precedent:** NodaTime `LocalDate` has no integer arithmetic operator. FEEL requires explicit offsets.
- **Tradeoff:** `DueDate + 5 days` is more verbose than `DueDate + 5`. The verbosity forces the author to name the unit.

### 11. `date - date` returns `period` (not integer, not duration)

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

- **Why:** NodaTime's `Period` includes both date components (years, months, weeks, days) and time components (hours, minutes, seconds). The v2 date-only restriction contradicted the "expose NodaTime faithfully" directive in three ways: (1) `LocalTime.Plus(Period)` is NodaTime's native API for time arithmetic, but date-only `period` couldn't carry time results; (2) `Period.Between(LocalTime, LocalTime)` returns time-component Periods with no home in a date-only type; (3) timeline constructors returning `duration` plus `LocalTime` not accepting `Duration` left no clean implementation path for `time + 3 hours`. `Period.FromHours()`, `.FromMinutes()`, `.FromSeconds()` all exist in NodaTime.
- **Alternatives rejected:** Date-only `period` (v2 position) — created the three structural contradictions above. Re-invents a boundary NodaTime chose not to draw.
- **Precedent:** NodaTime `Period` — full date+time components. `Period.HasDateComponent` / `Period.HasTimeComponent` for introspection.
- **Tradeoff:** `date + period` where the period has time components: NodaTime's `LocalDate.Plus(Period)` throws `ArgumentException` on non-zero time components (`Preconditions.CheckArgument(!period.HasTimeComponent, ...)`). Precept's `dateonly` constraint catches this at compile time rather than letting it reach the runtime exception. This is stricter-earlier, not a divergence.

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

### 16. Sub-day duration bridging — `time + duration` via thin type-checker translation

- **Why:** `hours`, `minutes`, `seconds` in most expression contexts resolve to `duration` (fixed-length quantities — Duration's natural type). NodaTime's `LocalTime` accepts `Period`, not `Duration`. But for sub-day units, `Duration` and `Period` represent identical physical quantities — an hour is always exactly 3600 seconds regardless of whether it's stored as `Duration.FromHours(1)` or `Period.FromHours(1)`. The type checker allows `time + duration → time`. Runtime: nanosecond arithmetic on `LocalTime` (NodaTime stores `LocalTime` as nanosecond-of-day internally). This uses NodaTime's own primitives, not a reimplementation.
- **Alternatives rejected:** (A) Time units always producing `period` — `instant + 3 hours` fails unless compiler can guarantee no date components, which is impossible for period fields at compile time. Violates Principle #8. (C) Context-dependent return type — implicit, violates the directive. (D) Separate unit keywords for duration vs. period — two names for the same physical quantity; verbose duplication.
- **Precedent:** NodaTime has BOTH `Duration.FromHours(1)` AND `Period.FromHours(1)` for the same physical quantity. Precept picks one entry point (`duration`) and bridges at the type-checker level.
- **Tradeoff:** `time + duration` is not a direct NodaTime API call. The translation layer is thin — nanosecond arithmetic — not a reimplementation. NodaTime's own `LocalTime.PlusHours()` convenience methods do the same nanosecond manipulation internally.

### 17. Typed constant quantities — sole mechanism for temporal quantity construction (rewritten 2026-04-17)

**What:** Temporal quantities are expressed as typed constants inside `'...'`, with optional `{expr}` interpolation:

1. **Static quantity:** `'30 days'`, `'72 hours'`, `'12 months'` — value + unit name inside `'...'`
2. **Interpolated quantity:** `'{GraceDays} days'`, `'{X + 5} hours'` — `{expr}` inside `'...'`
3. **Combined quantity:** `'2 years + 6 months + 15 days'` — `+` combination inside `'...'`

**What was eliminated (v5):**
- All 7 bare postfix keywords: `30 days`, `72 hours`, `12 months`, `3 months`, `1 year`, `2 weeks`, `45 seconds`
- All 7 paren postfix forms: `(GraceDays) days`, `(SlaHours * 2) hours`, etc.
- The 7 reserved unit keywords: `days`, `hours`, `minutes`, `seconds`, `months`, `years`, `weeks` — **no longer keywords**

**What was eliminated (v3, carried forward):**
- All 7 function-call constructors: `days()`, `months()`, `years()`, `weeks()`, `hours()`, `minutes()`, `seconds()`
- Composite literal form (greedy juxtaposition): `2 years 6 months 15 days`

- **Why:** One syntax, zero redundancy, zero keyword reservation. Quantities inside `'...'` with `{expr}` interpolation solve the variable-quantity split (literal `'30 days'` and variable `'{GraceDays} days'` use the same Door 2 syntax), eliminate 7 reserved words (unit names are validated strings, not keywords), and scale to future domains (currency, measurement, entity-scoped units add validated strings, not keywords). The `'...'` delimiter visually marks quantities as typed constants, consistent with formatted temporal values like `'2026-04-15'`. See [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md) for the full two-door model rationale.
- **Alternatives rejected:** (A) Bare postfix keywords (v3/v4 design — `30 days`, `(GraceDays) days`) — readable but reserves 7 keywords, creates the variable-quantity split for variable expressions, and every future quantity domain adds more keywords. (B) Both syntaxes coexist (original Decision #17's "Option C") — redundant, dual syntax for the same concepts. (C) Composites via juxtaposition — eliminated in v3. (D) Function-call only — eliminated in v3.
- **Precedent:** PostgreSQL `INTERVAL '30 days'`. SQL `'...'` for value literals. CSS `calc(...)` for computed values inside property contexts.
- **Tradeoff:** `'30 days'` is 3 characters longer and visually noisier than bare `30 days`. The readability cost is marginal; the benefits (zero keyword reservation, unified syntax, scalability) outweigh it. Context-dependent type resolution is unchanged — `'3 days'` alone has no fixed type; the context that determines the type (`date +` vs `instant +`) is visible in the same expression.

**Context-dependent type resolution unchanged:** `date +` context → `period`, `instant +` context → `duration`, field default → match declared type, no context → compile error.

**Supersedes:** Original Decision #17 (bare postfix keywords), v3 rewrite (unified bare/paren postfix model).

### 18. Single-quoted typed constants — `'...'` as the typed constant delimiter (rewritten 2026-04-15, reframed 2026-04-15, updated 2026-04-17)

**What:** The `'...'` delimiter is the **typed constant delimiter** — a language-level mechanism for any non-primitive type whose content shape determines a type family. The canonical design lives in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md). Temporal types are the first inhabitants, not the definition. Constants for `date`, `time`, `datetime`, `instant`, `zoneddatetime`, `timezone`, and temporal quantities use `'...'` with type inferred from content shape via the type-family admission rule.

| Type | Literal | Content shape | Type family |
|------|---------|--------------|-------------|
| date | `'2026-01-15'` | `YYYY-MM-DD` | `{date}` |
| time | `'14:30:00'` | `HH:MM:SS` | `{time}` |
| datetime | `'2026-01-15T14:30:00'` | `...T...` (no `Z`, no `[`) | `{datetime}` |
| instant | `'2026-01-15T14:30:00Z'` | `...T...Z` | `{instant}` |
| zoneddatetime | `'2026-01-15T14:30:00[America/New_York]'` | `...T...[zone]` | `{zoneddatetime}` |
| timezone | `'America/New_York'` | IANA identifier | `{timezone}` |
| quantity | `'30 days'`, `'{GraceDays} days'` | `<value> <unit>` | `{period, duration}` |

**Delimiter semantics — the two-door model:**

| Delimiter | Meaning | Type resolution |
|-----------|---------|-----------------|
| `"..."` | String with optional `{expr}` interpolation | Always `string` |
| `'...'` | Typed constant with optional `{expr}` interpolation | Inferred from content shape → type family → narrowed by context |

**Type-family admission rule (updated 2026-04-17):** A type qualifies for `'...'` if and only if its content shape determines a **type family** — a finite, enumerable set of types — that is disjoint from every other family. Context narrows within the family. This replaces the original "self-identifying property" which required each shape to map to exactly one type. The type-family formulation allows quantities (`'30 days'` → family `{period, duration}`, narrowed by expression context) while preserving unambiguous type inference for singleton families (`'2026-01-15'` → `{date}`).

**Current inhabitants (temporal — first wave):** The seven types listed above. **Future candidates (illustrative, not committed):** UUID, email, URI, semver — each requires formal family-disjointness analysis. See [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md) § Forward Design.

**Design evolution:** The `date(2026-01-15)` constructor form was the team's unanimous initial recommendation (original Decision #18). Shane then challenged it with the string-delimiter precedent insight: strings don't need `string("hello")` — the `"..."` delimiter IS the type signal. Why should temporal values need `date(...)` when a distinct delimiter can carry the same information? This led to exploring `"..."` with context resolution (rejected — see "Double-Quote Alternative Analysis" below), then to single quotes as the clean solution.

- **Why:** (1) Follows Precept's literal grain — the delimiter IS the type signal. (2) Single token in the tokenizer (`'[^']*'`), same as `NumberLiteral` and `StringLiteral`. (3) Uniform across all 6 types. (4) `'` is an unambiguous IntelliSense trigger for temporal completions and progressive validation. (5) No `char` type collision — permanently safe.
- **Alternatives rejected:** (A) `date(2026-01-15)` (unquoted constructor) — team's initial choice, superseded by delimiter insight. (B) `date("2026-01-15")` (string constructor) — quotes suggest "string being parsed." (C) `"2026-01-15"` (double-quoted, context-resolved) — explored and rejected; see "Double-Quote Alternative Analysis." (D) Bare ISO `2024-01-15` — lexer ambiguity with subtraction. (E) SQL-style `DATE '2026-01-15'` — redundant keyword when content shape is sufficient.
- **Precedent:** SQL `'...'` for value literals including dates. Precept's own `"..."` → string pattern.
- **Tradeoff:** Type is implicit in content shape rather than explicit in a keyword. Readers must recognize ISO formats. Mitigated by: (a) ISO formats are culturally universal, (b) field declarations provide type context, (c) syntax highlighting can color single-quoted content distinctly.

**Tooling implications:**
- **IntelliSense:** `'` triggers temporal completions with format-aware ghost text. After `'2024-`, show `-MM-DD` placeholder. After `'14:`, show `mm:ss`. Content-shape detection enables type-specific validation as the author types.
- **Default-position IntelliSense:** Normalize completions across all temporal field types. After `default `, offer the canonical starter for the declared field type: `date/time/instant/datetime/timezone/zoneddatetime` get single-quoted typed constants, `duration/period` get typed constant quantity snippets.
- **Hover content (invariant culture, deterministic):**
  - `'2024-01-15'` → "date: January 15, 2024 (Monday)"
  - `'14:30'` → "time: 2:30 PM"
  - `'2024-01-15T14:30:00'` → "datetime: January 15, 2024 at 2:30 PM (Monday)"
  - `'2024-01-15T14:30:00Z'` → "instant: 2024-01-15T14:30:00Z (UTC)" — NO day-of-week (requires timezone)
  - `'America/New_York'` → "timezone: America/New_York (UTC-05:00 / UTC-04:00 DST)"
- **Timezone completions:** `'` in timezone context triggers IANA timezone completion from `DateTimeZoneProviders.Tzdb.Ids` with UTC offset descriptions.
- **Hover is always invariant culture English** — not locale-dependent. Same `.precept` file, same hover everywhere.

### 19. Bare English naming — `date`, `time`, `datetime` (revised v4)

- **Why:** `date`, `time`, and `datetime` are the natural English words for these concepts. Business-domain authors — compliance officers, clinic administrators, insurance analysts — think in dates and times, not "local dates" and "local times." The bare name is the unmarked default; `zoneddatetime` is the marked form that carries additional information. The asymmetry is intentional: the common case (`date`) is short, the rarer case (`zoneddatetime`) is longer and self-documenting. The `local` prefix is NodaTime's internal naming convention, not a user-facing concept that business authors need to learn.
- **Alternatives rejected:** `local*` naming (`localdate`, `localtime`, `localdatetime`) — v4 initial position. Precise for NodaTime developers but adds cognitive overhead for business-domain authors. The `local` prefix reads as geographic locality to non-programmers, not "no timezone attached." `calendardate` / `walltime` — descriptive but overly verbose.
- **Precedent:** SQL (`DATE`, `TIME`, `DATETIME` — all timezone-free by default). FEEL (`date()`, `time()`, `date and time()`). Most business-domain DSLs use bare English names.
- **Tradeoff:** Potential ambiguity with `System.DateTime` for .NET developers. Mitigated by: (1) Precept is a DSL with its own type system — no `System.DateTime` interop at the surface level, (2) `zoneddatetime` is the distinguished form, making the naming asymmetry a teaching moment, (3) field declarations (`as date`, `as datetime`) provide context.

### 20. Dot-vs-function rule — "If the value owns it, dot" (locked v4)

- **Why:** This is a language-wide design principle, not just a temporal decision. Applied to temporal types: `myInstant.inZone(tz)` (the instant owns its mediation), `zdt.date` (the zoneddatetime owns its components), `myPeriod.years` (the period owns its structural components). Freestanding functions remain for operations that don't belong to any particular value: `abs()`, `min()`, `trim()`.
- **Alternatives rejected:** All operations as freestanding functions — v3 position (`toLocalDate(instant, tz)`). Loses IntelliSense discoverability (see Elaine's research). Functions don't chain naturally.
- **Precedent:** NodaTime uses instance methods (`instant.InZone(tz)`, `zdt.LocalDateTime`). Java uses instance methods (`instant.atZone(tz)`, `zdt.toLocalDate()`). C# `System.DateTime` uses instance properties (`.Date`, `.TimeOfDay`). Dot access is the universal pattern.
- **Tradeoff:** Dot chains can become long (`myInstant.inZone(tz).date.year`). Mitigated by set-to-variables pattern and intermediate field assignments.

### 21. `inZone` as the sole timezone mediation operation (locked v4)

- **Why:** One operation in both directions. `instant.inZone(tz)` → `zoneddatetime` (universal→local). `datetime.inZone(tz)` → `zoneddatetime` (local→universal). Navigation to specific types via dot chains on the result. Eliminates three standalone functions (`toLocalDate`, `toLocalTime`, `toInstant`) and the eliminated `pin` function. Cleaner separation of concerns — mediation and extraction are separate dot operations, not conflated in one function call.
- **Alternatives rejected:** Three standalone functions (v3 position) — conflated mediation+extraction, poor IntelliSense discoverability. Two functions (`inZone` + `pin`) — `pin` is redundant with `ldt.inZone(tz).instant`.
- **Precedent:** NodaTime `Instant.InZone(DateTimeZone) → ZonedDateTime`. Java `Instant.atZone(ZoneId) → ZonedDateTime`.
- **Tradeoff:** `myInstant.inZone(tz).date` is longer than `toLocalDate(myInstant, tz)`. The verbosity makes the two-step process (mediate, then extract) visually explicit.

### 22. Strict type hierarchy — no skip-level accessors (locked v4)

- **Why:** Forces explicit timezone mediation at every boundary crossing between the universal tier (`instant`) and the local tier (`date`, `time`, `datetime`). `instant.date` is a compile error — must go `instant.inZone(tz).date`. This prevents the most common temporal bug: extracting calendar components from a UTC timestamp without specifying which timezone.
- **Alternatives rejected:** Skip-level convenience accessors (`instant.date(tz)` — parameter on the accessor) — conflates mediation into the accessor, inconsistent with other accessor patterns. Implicit UTC extraction (`instant.year` defaults to UTC) — exactly what NodaTime was designed to prevent.
- **Precedent:** NodaTime's `Instant` has no date/time properties at all — `InZone()` is required. Java's `Instant` has no `getYear()` — `atZone()` is required.
- **Tradeoff:** Verbose chains for simple extractions. `myInstant.inZone(tz).date.year` requires understanding the full hierarchy. Mitigated by: assign intermediate results to named fields for readability.

### 23. `pin` eliminated — reverse navigation via dot chain (locked v4)

- **Why:** `datetime.inZone(tz).instant` achieves the same result as the proposed `pin(ldt, tz)` function. One mediation operation (`.inZone(tz)`) in both directions is simpler than two operations (`inZone` for forward, `pin` for reverse). Fewer concepts to learn, fewer function names to remember.
- **Alternatives rejected:** `pin(datetime, tz) → instant` — dedicated reverse-direction function. Redundant with dot-chain composition.
- **Precedent:** NodaTime uses `LocalDateTime.InZoneLeniently(DateTimeZone).ToInstant()` — the same composition pattern.
- **Tradeoff:** `ldt.inZone(tz).instant` is slightly longer than `pin(ldt, tz)`. The consistency of using the same `.inZone(tz)` operation in both directions outweighs the character count.

### 24. Instant restricted semantics — comparison, duration arithmetic, and `.inZone(tz)` only (locked v4)

- **Why:** An instant is a point on the UTC timeline with no calendar context. The only operations that make physical sense on a UTC timeline point are: comparison (which came first?), duration arithmetic (add/subtract fixed time), and timezone mediation (view in a timezone). Component accessors (`.year`, `.date`) are compile errors because they require a timezone. Period arithmetic (`+ 1 month`) is a type error because months have variable duration. This is NodaTime's `Instant` design, exposed faithfully.
- **Alternatives rejected:** Instant with implicit UTC component accessors (`instant.year` meaning "year in UTC") — hides a timezone dependency behind convenience. Instant with period arithmetic via implicit UTC conversion — implicit behavior is what NodaTime was designed to prevent.
- **Precedent:** NodaTime `Instant`: no date/time properties, no period arithmetic, only comparison + duration + `InZone()`.
- **Tradeoff:** Every calendar operation on an instant requires `.inZone(tz)` first. Verbosity is the point — it makes timezone dependencies explicit.

### 25. `date + time → datetime` as the sole component construction path (locked v4)

- **Why:** `date + time` is NodaTime's native construction method (`LocalDate + LocalTime → LocalDateTime`). This is the only way to build a `datetime` from its components — no factory function, no constructor syntax. Commutative: `time + date` produces the same result. This is consistent with the operator-centric design where the `+` operator handles all composition and arithmetic. The motivation example at line ~183 already uses `date + '23:59:00'` — this decision formalizes what the example assumes.
- **Alternatives rejected:** `datetime(date, time)` factory function — introduces a new function-call form that doesn't exist elsewhere in the DSL. `combine(date, time)` — same problem, new concept for a single operation.
- **Precedent:** NodaTime `LocalDate + LocalTime → LocalDateTime`. Kotlin `LocalDate.atTime(LocalTime)` — similar composition, different syntax.
- **Tradeoff:** None significant. The operation is natural, NodaTime-native, and already expected by the motivation example.

### 26. Period component constraints — `timeonly` and `dateonly` (locked v4)

- **Why:** NodaTime's `LocalTime.Plus(Period)` throws `ArgumentException` on periods with non-zero date components. `LocalDate.Plus(Period)` throws on non-zero time components. The type `period` alone carries no information about which component categories a value contains. Without a proof mechanism, `time ± period` and `date ± period` create a soundness gap — the expression may throw at runtime depending on field data, violating Precept's deterministic execution guarantee (Principle #1) and sound static analysis (Principle #8). The `timeonly` and `dateonly` constraint suffixes provide the proof mechanism: (1) For literal postfix expressions (`3 hours`, `30 days`), the compiler infers component category from the unit keyword — no annotation needed. (2) For `time - time` and `date - date`, the result is always same-category — no annotation needed. (3) For `period` field references and event args, the author declares `period timeonly` or `period dateonly` and the compiler enforces it at every assignment site. (4) For event args (external input), the runtime validates at the API boundary in `TryValidateEventArguments` — input validation, not mid-evaluation exception. This closes the gap completely: every `time ± period` or `date ± period` expression either has compile-time proof or is rejected at compile time.
- **Syntax:** `timeonly` and `dateonly` as single-word constraint suffixes, matching the existing convention (`nonnegative`, `notempty`, `minlength`). Three forms: `period timeonly` (hours/minutes/seconds only), `period dateonly` (years/months/weeks/days only), `period` (unconstrained — any components, but `time ± period` and `date ± period` are compile errors). Applicable to field declarations and event arg declarations.
- **Semantic rules:** `time ± period timeonly` → OK. `time ± period` (unconstrained) → compile error. `date ± period dateonly` → OK. `date ± period` (unconstrained) → compile error. `datetime ± period` (any) → always OK (`LocalDateTime.Plus/Minus(Period)` accepts all components). Component categories are closed under arithmetic: `timeonly + timeonly = timeonly`, `dateonly + dateonly = dateonly`, `timeonly + dateonly = mixed (unconstrained)`.
- **Alternatives rejected:** (1) Ban `time ± period` entirely — overly restrictive, prevents `time + GracePeriod` when GracePeriod is known time-only. (2) Runtime strip of date components — silently changes behavior, diverges from NodaTime, violates "expose NodaTime faithfully" directive. (3) Runtime rejection mid-evaluation — violates Principle #1 (deterministic model). (4) Period subtypes `period<time>` / `period<date>` — angle brackets aren't in the grammar; parameterized types add language complexity disproportionate to the problem. (5) Hyphenated `time-only` / `date-only` — breaks the existing constraint keyword convention (all single lowercase compounds).
- **Precedent:** NodaTime `PeriodUnits.DateOnly` / `PeriodUnits.TimeOnly` masks. `Period.HasDateComponent` / `Period.HasTimeComponent` introspection (used for runtime boundary validation).
- **Tradeoff:** Authors must annotate period fields and args when used with `time` or `date` arithmetic. The annotation is the proof — without it, the compiler cannot guarantee safety. This is consistent with Precept's philosophy: explicit over implicit.

### 27. No `datetime + duration` — context-dependent resolution to `period` instead (locked v4)

- **Why:** NodaTime's `LocalDateTime` has no `Plus(Duration)` method — by design. `Duration` is a timeline quantity for `Instant` arithmetic; `LocalDateTime` is a local observation with no timeline context. Mixing them is a category error, the same reason `date + duration` is rejected. The proposal initially supported `datetime + duration` via "duration bridging" (same mechanism as `time + duration`, Decision #16), but this invented an operation NodaTime deliberately excludes. The clean solution: time-unit typed constants (`'3 hours'`, `'30 minutes'`, `'45 seconds'`) resolve to `period` in `datetime` context via context-dependent resolution (Decision #9). `datetime + '3 hours'` becomes `LocalDateTime.Plus(Period.FromHours(3))` — NodaTime-native, overflow advances the date naturally, no bridging layer needed. For `duration` field references, the author must convert explicitly: navigate to `instant` for timeline arithmetic, or use a `period` field instead.
- **Alternatives rejected:** (1) Duration bridging — invents `LocalDateTime.Plus(Duration)` that NodaTime doesn't have; wrapping vs. overflow behavior is ambiguous and underspecified. (2) Convert duration to period at runtime — implicit coercion violates "expose NodaTime faithfully." (3) Allow both paths — two ways to do the same thing with subtly different overflow behavior is a bug factory.
- **Precedent:** NodaTime's `LocalDateTime` accepts only `Period`. Java's `LocalDateTime.plus(TemporalAmount)` also routes through period-like units, not raw durations.
- **Tradeoff:** `datetime + durationField` is a compile error. The author must either change the field to `period` or navigate to `instant` for timeline arithmetic. This is deliberate — it forces the author to choose the right abstraction level.

### 28. Temporal typed constant quantities require integer values (locked v4, updated v5)

- **Why:** NodaTime's `Period.FromDays(int)`, `Period.FromMonths(int)`, `Period.FromYears(int)`, `Period.FromWeeks(int)`, `Period.FromHours(long)`, `Period.FromMinutes(long)`, `Period.FromSeconds(long)`, and `Duration.FromHours(double)`, `Duration.FromDays(double)` etc. have mixed signatures — some take `int`, some `long`, some `double`. Rather than exposing this inconsistency, Precept standardizes: all temporal quantity typed constants require integer values. `'0.5 days'` is a compile error. `'{GraceDays} months'` requires `GraceDays` to evaluate to an integer. This is consistent with how temporal quantities work in practice — "2.5 months" is ambiguous (is the .5 half a 28-day month or a 31-day month?), while `'2 months + 15 days'` is precise.
- **Scope:** This restriction applies to **temporal** unit names (`days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`) inside typed constants. The typed constant grammar mechanism is a general language expansion joint (noted in the proposal's "Expansion Joint" section and in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md)). Future non-temporal quantity typed constants (e.g., currency, measurement units) may support non-integer values if their backing types accept them.
- **Alternatives rejected:** (1) Allow fractional values — `Period.FromDays` takes `int`, so fractional days require decomposition into days + hours, which is what precise authors should write anyway. (2) Truncate silently — violates "no silent coercion" principle. (3) Round — same problem, which direction?
- **Precedent:** NodaTime's `Period` factory methods take `int` for all calendar units. ISO 8601 allows `PT0.5S` but NodaTime represents sub-second precision via nanoseconds, not fractional seconds in the string.
- **Tradeoff:** `PT0.5S` (half a second) is not directly expressible via typed constant quantity. The author would need `'500 milliseconds'` if sub-second support is added, or model as a computed `duration` field.

---

## Double-Quote Alternative Analysis

After the `date(2026-01-15)` constructor form was challenged by the delimiter insight, the team explored whether double-quoted strings (`"2026-01-15"`) could serve as temporal literals with the type inferred from context (field declaration, expression position).

### The compiler CAN resolve it in ~99% of cases

Context resolution is technically feasible. The compiler knows the target type from:
- Field declarations: `field DueDate as date default '2026-01-15'` → the field is `date`, the literal is a date.
- Binary operators: `DueDate + '2026-01-15'` → left operand is `date`, right must be `date` (for comparison) or `period` (for arithmetic).
- Dot-accessor context: `myInstant.inZone(tz).date` → the chain produces a `date`.

The ~1% failure case is genuinely ambiguous positions (pure variable assignment without type annotation), which could be mitigated by requiring explicit type annotation in those rare positions.

### Why it was rejected

Three arguments against double-quoted temporal resolution, in order of strength:

1. **Refactoring safety.** Changing `field DueDate as date` to `field DueDate as string` silently changes the meaning of every `"2026-01-15"` assigned to it. With single quotes (`'2026-01-15'`), those expressions immediately become type errors, flagging every usage site for review. Type changes should produce compile errors at affected usage sites — not silent semantic shifts.

2. **IntelliSense quality.** `"` is the trigger for string completions. If `"` sometimes starts a temporal literal, the completion engine needs full expression-context analysis before offering suggestions. `'` is an unambiguous trigger — inside single quotes, always offer temporal completions with format-aware ghost text and progressive validation.

3. **Error clarity.** When `"2026-01-15"` fails validation in a date context, the error is: "Invalid date format in string." With `'2026-01-15'`, the error is: "Invalid date: ..." — the system knows this was intended as a temporal value because the author used the temporal delimiter. The error can be specific and actionable.

### The choice-type precedent

An important counter-argument was considered: Precept's `choice` fields already use `"..."` double-quoted strings where the meaning depends on the field declaration. `choice("Draft", "Active", "Closed")` defines allowed values — `"Draft"` is only valid in the context of that field.

However, the distinction matters: **choice is a constraint on a value within the same type** — a string field constrained to specific string values. The string `"Draft"` is still a string; the field merely limits which strings are valid. Temporal would be a **type-family conversion** — the string `"2026-01-15"` would need to become a fundamentally different type (`date`). This crosses from value-constraint to type-conversion, which is the stronger argument for a distinct delimiter.

### Conclusion

Single quotes win on all three criteria that matter for a DSL: refactoring safety (silent semantic shift → compile error), IntelliSense (unambiguous trigger), and error clarity (intent-aware messages). The ~99% context-resolution success rate is insufficient when the 1% failure mode is silent type conversion.

---

## George's Challenges — Resolution

### Challenge 1: `DueDate + MealsTotal` compiles because `date + number → date`

**Resolution (v2 — strengthened):** No form of `date + <non-period>` is defined. `DueDate + MealsTotal` is a type error regardless of `MealsTotal`'s type — none of `integer`, `number`, `decimal`, or `duration` are `period`.

### Challenge 2: `date + 2.5` should reject fractional offsets

**Resolution (v2 — strengthened, updated v5):** `date + 2.5` is a type error — `2.5` is not a `period`. `date + 2` is also a type error. `'0.5 days'` is a compile error — temporal typed constant quantities require integer values (Decision #28).

### Challenge 3: `nullable + default` prohibition

**Resolution:** Open. Deferred to cross-cutting design decision.

### Challenge 4: `date - date` result type

**Resolution (v2 — redesigned):** `date - date → period`. Preserves structural calendar units. Matches `Period.Between(d1, d2)`.

---

## Dependencies and Related Issues

| Issue | Relationship |
|---|---|
| #25 (choice type) | Complementary. |
| #26 (date type) | **Superseded.** `date` section here incorporates all of #26 plus NodaTime, period-based arithmetic, typed constant quantity construction. |
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

### Collection inner types

Temporal types are valid as collection inner types where the collection's structural requirements are met.

**`queue of <T>` and `stack of <T>`** — all 8 temporal types are valid. Queues and stacks are ordered by insertion, not by value comparison.

**`set of <T>`** — Precept sets are backed by `SortedSet<object>` and require `IComparable<T>`. Five temporal types support this:

| Inner type | `set of` | `queue of` / `stack of` |
|---|---|---|
| `date` | ✓ | ✓ |
| `time` | ✓ | ✓ |
| `instant` | ✓ | ✓ |
| `duration` | ✓ | ✓ |
| `datetime` | ✓ | ✓ |
| `period` | **✗** — no natural ordering | ✓ |
| `timezone` | **✗** — no natural ordering | ✓ |
| `zoneddatetime` | **✗** — no natural ordering | ✓ |

`set of period`, `set of timezone`, and `set of zoneddatetime` are compile errors: "This type has no natural ordering. Use `queue of <T>` or `stack of <T>` instead."

---

## Implementation Scope

### Parser / Tokenizer

- Add `date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `datetime` as type keywords.
- Temporal unit names (`days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`) are **not** language keywords. They are validated strings inside `'...'` typed constants. No keyword reservation needed.
- Add `inZone` as dot-accessor keyword for timezone mediation.
- **Single-quoted typed constants with interpolation:** New token type `TypedConstantLiteral`. The tokenizer recognizes `{` inside `'...'` as interpolation start and switches to expression mode (same mechanism as string interpolation). Content inside single quotes — after interpolation resolution — is a constant pattern matched to determine the type family. `'2026-03-15'` is valid (date). `'03/15/2026'` is a compile error with a teachable message. `'30 days'` is valid (quantity). `'{GraceDays} days'` uses interpolation. See [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md) for the full tokenizer architecture.
- **String interpolation:** `"..."` strings gain `{expr}` interpolation. The tokenizer recognizes `{` inside `"..."` as interpolation start. `{{` and `}}` produce literal `{` and `}`. See [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md).
- **Quantity typed constants:** Quantity content (`'30 days'`, `'2 years + 6 months'`, `'{GraceDays} days'`) is parsed after interpolation resolution as: `<integer> <unit-name>`, with `+` combination for compound quantities. No bare postfix combinators needed at the expression level.
- Parse `.inZone(tz)` dot-accessor form for timezone mediation on `instant` and `datetime` types.
- Parse accessors: `.year`, `.month`, `.day`, `.dayOfWeek`, `.hour`, `.minute`, `.second`, `.date`, `.time`, `.datetime`, `.totalHours`, `.totalMinutes`, `.totalSeconds`, `.instant`, `.timezone`, `.years`, `.months`, `.weeks`, `.days`, `.hours`, `.minutes`, `.seconds`, `.hasDateComponent`, `.hasTimeComponent`.

### Type Checker

- 8 new type entries.
- **Period/duration split enforcement:** `date + period ✓`, `date + duration ✗`, `instant + duration ✓`, `instant + period ✗`. Standard type-checking — no custom dispatch.
- **Postfix unit type resolution:** Resolve quantity typed constants (`'<value> <unit>'`) to `period` or `duration` based on expression context. `date +` / `datetime +` context → `period`. `instant +` / `zoneddatetime +` context → `duration`. `months`/`years` → always `period`. Field default context → match declared field type. No context → compile error.
- **Typed constant type inference:** Determine specific type from content shape via the type-family admission rule. Current inhabitants: `YYYY-MM-DD` without `T` = date, `HH:MM:SS` without `-` = time, `...T...` without `Z` or `[` = datetime, `...T...Z` = instant, `...T...[zone]` = zoneddatetime, IANA pattern = timezone, `<value> <unit>` = quantity (family `{period, duration}`). Framework is extensible to future inhabitants whose shapes produce disjoint families.
- **Meaningless combination warnings:** `date + 3 hours` (hours on a date) → warning. `date + 3 minutes` → warning.
- Full cross-type interaction matrix.
- `period` ordering rejection (`<`, `>`, `<=`, `>=`).
- `period` scaling rejection (`* integer`).
- `duration + period` / `period + duration` rejection.
- **Sub-day duration bridging:** `time + duration → time` allowed by type checker. See Decision #16.
- All v1 type-checker items apply.

### Expression Evaluator

- `date` arithmetic via `LocalDate.Plus(Period)`, `LocalDate.Minus(Period)`.
- `date - date` via `Period.Between(d1, d2)`.
- `period` arithmetic via `Period + Period`, `Period - Period`.
- `time + period` via `LocalTime.Plus(Period)` — NodaTime native.
- `time + duration` via nanosecond arithmetic on `LocalTime` — thin translation (Decision #16).
- `time - time` via `Period.Between(t1, t2)` → time-component period.
- `instant` arithmetic via `Instant.Plus(Duration)`, `Instant.Minus(Duration)`.
- `duration` arithmetic via `Duration` operators.
- `datetime` arithmetic via `LocalDateTime.Plus(Period)`. Time-unit typed constants resolve to `period` in datetime context (Decision #9, #27) — no duration bridging needed.
- `zoneddatetime` timeline arithmetic via underlying instant.
- Quantity typed constant resolution: `'30 days'` / `'{n} days'` → `Period.FromDays(n)` or `Duration.FromDays(n)` based on resolved type family member.
- String interpolation: evaluate `{expr}` inside `"..."`, coerce to string per coercion table in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md).
- Typed constant interpolation: evaluate `{expr}` inside `'...'`, substitute, then shape-match.
- `.inZone(tz)` mediation: `instant.inZone(tz)` via `Instant.InZone(DateTimeZone)`, `datetime.inZone(tz)` via `LocalDateTime.InZoneLeniently(DateTimeZone)`.

### Runtime Engine

- Value carriers for all 8 temporal types.
- `period` serialization via `PeriodPattern.Roundtrip`.
- Constraint enforcement, timezone validation at fire boundary.

### TextMate Grammar

- Add all 8 type keywords to `typeKeywords`.
- Temporal unit names (`days`, `hours`, etc.) are NOT added as keywords — they are validated strings inside `'...'` and do not need grammar-level highlighting as keywords.
- Add single-quoted literal pattern with `{...}` interpolation region highlighting.
- Add string interpolation `{...}` region highlighting inside `"..."`.
- Add `inZone` as dot-accessor keyword.

### Language Server

- Completions, hover, diagnostics for all 8 types including full `period` (date+time components).
- Single-quote trigger for typed constant completions with format-aware ghost text. Unit name completions after a number inside `'...'`.
- String interpolation completions: field names and dot accessors after `{` inside `"..."`.
- Period-specific diagnostics (ordering rejection, scaling rejection, cross-domain rejection for `instant + period`).
- Sub-day bridging documentation in hover for `time + duration`.

### MCP Tools

- `precept_language`: All 8 types. Typed constant quantity syntax description. Typed constant delimiter syntax. Interpolation syntax. Reference [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md) for canonical literal model.
- `precept_compile`/`fire`/`inspect`/`update`: All temporal values serialized as ISO 8601 strings. `zoneddatetime` serialized as `{ "instant": "..Z", "timezone": "..." }`.
- **Serialization:** Configure `NodaTime.Serialization.SystemTextJson` on the serializer. Output is fully automatic — NodaTime types in instance data and DTOs serialize to ISO 8601 strings via registered converters. Input requires type-directed deserialization: MCP input arrives as generic dictionaries (values are raw strings), so the engine dispatches to the correct NodaTime converter based on the compiled field type declaration. The MCP layer stays thin — no conversion code.

### Samples, Documentation, Tests

- Update samples with `period` field type usage.
- All documentation sync per copilot-instructions.
- Full test coverage for period/duration split enforcement.

---

## Forward Design: Temporal Types as the Gateway Beyond Primitives

Temporal types are not just a feature — they are the **gateway** to Precept's evolution beyond primitive types. The literal mechanisms established in this proposal — the single-quoted typed constant delimiter (`'...'`) with `{expr}` interpolation — are the language's **expansion joints** for all future non-primitive types. The canonical design for these mechanisms lives in [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md). Every non-primitive value in Precept enters through one of two doors: `"..."` (string) or `'...'` (typed constant). When a future type is proposed, the question is "which door?" — not "does it need new syntax?"

The `'...'` typed constant delimiter and `{expr}` interpolation are both foundational grammar patterns that naturally extend to other business-domain types. This section documents the extensibility implications so that temporal design decisions account for the broader framework they are establishing.

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

**Door 2 — Typed constant (`'...'`) with interpolation:** For values with a shape-recognizable constant form AND for magnitude+unit quantities. Future candidates beyond temporal: UUID (`'550e8400-...'` — `8-4-4-4-12` hex), email (`'user@example.com'` — `@` present), URI (`'https://...'` — scheme `://`), semver (`'2.1.0'` — `N.N.N`). Quantities like `'100 USD'`, `'24 each'`, `'{Quantity} each'` also enter through this door — the shape includes a numeric portion followed by a unit name. Interpolation (`'{expr}'`) allows computed values without a separate grammar form. Each inhabitant qualifies only if its content shape is distinguishable from all existing inhabitants via the type-family admission rule (see [`docs/LiteralSystemDesign.md`](LiteralSystemDesign.md)).

**Door 1 — String (`"..."`) with interpolation:** For values with no distinguishing shape. Three-letter currency codes like `USD` are indistinguishable from other short strings — so currency *amounts* use typed constants (`'100 USD'`), but standalone currency codes remain strings. Interpolation (`"{expr}"`) allows dynamic content.

The two-door design — `"..."` (string) and `'...'` (typed constant) — covers **all** anticipated future non-primitive types without requiring new grammar concepts. Quantities that previously needed a separate grammar door (`N unit` postfix) now enter through Door 2 as `'N unit'` typed constants, with `'{expr} unit'` for computed values. When a future type is proposed, the first question is always: "which door?" — not "does it need new syntax?"

**Keyword explosion: solved.** The original three-door model required every unit name (`days`, `hours`, `USD`, `each`, ...) to be a language keyword — creating unbounded keyword growth. The two-door model eliminates this entirely: unit names are validated strings inside `'...'`, not reserved words. The language's keyword set remains finite and fixed regardless of how many domains or unit systems Precept supports.

### Unit resolution model — three validation scopes

The temporal design establishes the pattern; future domains fill new validation scopes. Unit names are validated strings inside `'...'` — not language keywords. What changes across scopes is which unit names are recognized.

| Scope | Source of truth | Examples | Validation timing |
|-------|----------------|---------|-------------------|
| **Language-level** | Backing library (NodaTime) | `days`, `hours`, `months` | Compile-time — closed set |
| **Standard registry** | External standard (ISO 4217, UCUM subset) | `USD`, `EUR`, `kg`, `lbs` | Compile-time — large but closed set |
| **Entity-scoped** | `units` block in precept definition | `each`, `case`, `six-pack` | Compile-time within the precept — conversions are entity data |

The grammar `'<value> <unit>'` is the same across all three scopes. What changes is where the unit name validates from.

### Design implications for temporal decisions

These implications inform the temporal decisions being made now:

1. **Unit name as validated content.** The unit name in `'<value> <unit>'` is a string validated against a scope chain — not a hard-coded keyword. `days` validates from the temporal scope (NodaTime). `USD` would validate from a standard registry scope (ISO 4217). `cases` would validate from the entity's own `units` block. Same grammar, different validation source.

2. **Context-dependent resolution generalizes.** The mechanism designed for period vs. duration resolution (`'3 days'` → `period` in date context, `duration` in instant context) is the same mechanism needed for entity-scoped unit disambiguation (`'24 items'` → quantity-in-purchasing-units vs. quantity-in-stocking-units depending on field declaration).

3. **`convert()` boundary enforcement.** Not needed for temporal types (NodaTime handles unit compatibility internally), but essential for entity-scoped units where the type checker must require explicit conversion at unit boundaries (`convert(issueQty, baseUnit)`). The temporal grammar should not preclude adding conversion enforcement later.

4. **IntelliSense scalability.** Temporal unit completion inside `'...'` is a finite list from NodaTime. Future domains need completion from open-ended sets — standard registries (ISO 4217 currencies) and per-entity declarations. The completion infrastructure should be designed for pluggable unit sources.

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

This section is a **forward design note**, not a commitment. It documents why the literal mechanism decisions in this proposal carry more weight than temporal types alone — they establish the language's entire framework for moving beyond primitives. Temporal types are the gateway: they prove the typed constant delimiter with interpolation, the type-family admission rule, and the zero-constructor discipline. The actual design of future types (UUID, email, currency, physical quantity) belongs in separate proposals when demand warrants it. The temporal decisions should be made with this framework in mind, but should not be over-engineered to serve hypothetical future needs.
