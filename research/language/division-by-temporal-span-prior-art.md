---
status: Active
authored: 2026-05-30
author: research (external-engagement leg)
topic: Prior art for dividing money/quantity by a time span (duration vs period) and the soundness asymmetry between fixed-length and calendar-relative divisors
external-engagement: strong
---

# Division by a Temporal Span — Prior Art and the Fixed-vs-Calendar Soundness Asymmetry

> Surveys whether dividing a money/quantity by a time span to get a rate is well-precedented, and whether prior art distinguishes a *fixed-length* duration divisor (sound) from a *calendar-relative* period divisor (ill-defined) — the asymmetry Precept's locked D15 asserts but does not yet cite.

## Background

Precept models two temporal span types after NodaTime: `duration` (a single fixed-length scalar — nanoseconds, calendar-independent) and `period` (calendar-aware components — years/months/weeks/days/hours/minutes/seconds — stored separately, with **no** total-conversion). Precept is considering division-by-temporal-span: `money ÷ duration → price`, `quantity ÷ duration`, possibly `money ÷ period`, `quantity ÷ period`, and `period ÷ period` / `duration ÷ duration`.

The consuming decision is the W-C Part-B keep / delete / build-properly call on Precept's dormant divide-by-temporal-span code.

Precept's canonical spec **already locks** a related boundary in `docs/language/business-domain-types.md § D15`. Notably, the *current* spec table is **dual-cancellation, not period-refusing**: it lists both `money / period → price` and `money / duration → price`, with the asymmetry drawn at the *unit-grain* (`duration` cancels `hours`/`minutes`/`seconds`; `days` and above remain `period`-only "due to DST/calendar rules"):

> "D15 amended to support dual cancellation — both `period` and `duration` cancel time-unit denominators, with a fixed-length boundary: `duration` cancels `hours`/`minutes`/`seconds` (always the same length); `days` and above remain `period`-only (variable length due to DST/calendar rules)."
> — `business-domain-types.md` ("What changed in v2", accessed 2026-05-30)

This survey grounds the **fixed-vs-variable-length** principle that D15 invokes ("always the same length" vs "variable length due to DST/calendar rules") with verbatim primary-source excerpts, and surfaces a tension the W-C Part-B decision must resolve: prior art draws the divisibility line at the **type** level (fixed-length `Duration` divides; calendar `Period` does not), whereas Precept's current D15 draws it at the **unit-grain** level (sub-day grains cancel; day-and-above don't). The survey does **not** override the lock — it supplies the precedent leg and names the tension for the consuming decision.

## Methodology

- **Research question** — Is "divide money/quantity by a fixed `duration` → rate" a well-precedented, sound operation in prior art, while "divide by a calendar `period`" is the operation prior art tends to refuse? Does any system divide a quantity (or money) by a time span at all, and do unit-of-measure systems distinguish fixed-length time from calendar spans as a divisor?
- **Search strategy** — Primary-source-first: NodaTime API docs (nodatime.org, versioned), java.time Javadoc (Oracle docs, Java SE 8 and 11), Joda-Time Javadoc (joda.org), and unit-of-measure / money-library knowledge (Frink, F# UoM, JSR-354/Joda-Money). Venues: official API documentation and language specifications, accessed via WebFetch/WebSearch.
- **Inclusion / exclusion criteria** — In scope: whether each system exposes division of a span by a scalar, by another span, and whether it distinguishes fixed vs calendar spans; whether money libraries define money÷time→rate. Out of scope: addition/subtraction of spans (surveyed in `period-basis-separator-survey.md`), runtime arithmetic mechanics (`exact-decimal-arithmetic-survey.md`).
- **Source-grade declaration** — Predominantly **Primary**: NodaTime, java.time, and Joda-Time legs are all grounded in verbatim API-doc excerpts captured this session. Frink is **Secondary** (named-author commentary). The money-library "no money÷time primitive" claim is **Tertiary** (training-data knowledge; no excerpt obtained). The unit-of-measure "only fixed-length time is a divisible dimension" claim is **Secondary/Tertiary** mix (Frink excerpt + training-data generalization).
- **Time bounds** — Investigation ran 2026-05-30. A transient web-tooling outage occurred mid-session and recovered; all Primary excerpts below were captured after recovery and re-verified. Fetch dates are 2026-05-30 throughout.

## Findings

| System | Span ÷ scalar? | Span ÷ same-type span (ratio)? | money/qty ÷ time → rate as a library op? | Fixed vs calendar divisor distinction | Grade |
|---|---|---|---|---|---|
| **NodaTime** `Duration` | **Yes** (`/double`, `/long` → `Duration`) | **Yes** (`/Duration` → `double`) | n/a (not a money lib) | `Duration` *is* the fixed-length type; "fixed (and calendar-independent)" | Primary ✓ |
| **NodaTime** `Period` | **No** | **No** | n/a | `Period` *is* the calendar type; no division, no total-conversion; `ToDuration()` refuses years/months | Primary ✓ |
| **java.time** `Duration` | **Yes** (`dividedBy(long)` → `Duration`) | **Yes** (`dividedBy(Duration)` → `long`, Java 9+) | n/a | `Duration` = "time-based amount … seconds and nanoseconds" | Primary ✓ |
| **java.time** `Period` | **No** divide method | **No** | n/a | `Period` = "date-based amount … years, months and days"; only plus/minus/multipliedBy/negated/normalized | Primary ✓ |
| **Joda-Time** `Duration` | **Yes** (`dividedBy(long)`) | **No** (no `ReadableDuration` divisor) | n/a | `Duration` is fixed milliseconds; `Period` carries calendar fields | Primary ✓ |
| **Frink** (unit calculator) | n/a | n/a | **Yes** — `15 USD/hour` is first-class dimensional division | Time base is the fixed SI second; no calendar-month base dimension | Secondary |
| **Money libraries** (JSR-354 / Joda-Money) | n/a | n/a | **No** primitive — rates are user-modeled compounds | n/a | Tertiary |

### NodaTime (Primary) — the direct model

NodaTime is Precept's direct model. Mirrored to `research/language/references/division-by-temporal-span/nodatime-duration-period-excerpts.md`.

**`Duration` is the fixed-length type and divides cleanly — by scalar AND by another Duration** [Primary; accessed 2026-05-30; mirrored]:

> "Represents a fixed (and calendar-independent) length of time."

> `public static double operator /(Duration left, Duration right)` — "Implements the operator / (division) to divide one duration by another."
> `public static Duration operator /(Duration left, double right)` — "to divide a duration by a System.Double."
> `public static Duration operator /(Duration left, long right)` — "to divide a duration by an System.Int64."
> — NodaTime `Duration` (https://www.nodatime.org/2.0.x/api/NodaTime.Duration.html, accessed 2026-05-30)

A `Duration` divides by a number (→ `Duration`) and by another `Duration` (→ `double` ratio). Both are well-defined because a Duration has a single fixed magnitude.

**`Period` is the calendar type — no division, no total-conversion, and conversion refuses years/months** [Primary; accessed 2026-05-30; mirrored]:

> "Represents a period of time expressed in human chronological terms: hours, days, weeks, months and so on."
> — NodaTime `Period` class summary (https://www.nodatime.org/2.0.x/api/NodaTime.Period.html, accessed 2026-05-30)

Period's documented arithmetic is `+` / `-` / `Add` / `Subtract` only — **no** division operator or `Divide` method, and **no** `TotalHours` / `TotalDays`. The conversion path is gated:

> `ToDuration()`: "For periods that do not contain a non-zero number of years or months, returns a duration for this period assuming a standard 7-day week, 24-hour day, 60-minute hour etc." Throws `InvalidOperationException`: "The month or year property in the period is non-zero."
> — NodaTime `Period.ToDuration` (same URL, accessed 2026-05-30)

This is the load-bearing precedent: NodaTime converts a `Period` to a fixed `Duration` **only when it contains no years or months**. Calendar components have no fixed magnitude and are structurally excluded.

**Why a period varies in length** [Primary; accessed 2026-05-30; mirrored]:

> "if you add 'one month' to January 1st, that's going to be 31 days long. Adding the same period to February 1st will give a shorter length of time - which then depends on whether the year is a leap year or not."

> "Periods aren't normalized, so a period of '2 days' is not the same as a period of '48 hours'; likewise if you ask for the number of hours in a period of '1 day' the answer will be 0, not 24."
> — NodaTime user guide (concepts/arithmetic, accessed 2026-05-30)

Note the `'2 days' ≠ '48 hours'` excerpt: in NodaTime, **days** are already calendar-variable (a day can be 23/24/25 hours across a DST boundary). This is direct precedent for D15's "days and above remain period-only" grain boundary — but it also shows NodaTime draws the *divisibility* line one step lower than D15 does: NodaTime's divisible type (`Duration`) excludes **all** of `Period`, while NodaTime's *fixed* sub-day units (hours/minutes/seconds) live inside `Duration`, not `Period`.

### java.time / JSR-310 (Primary) — exact same split

**`Duration` divides by scalar and (Java 9+) by another Duration** [Primary; accessed 2026-05-30; mirrored]:

> "A time-based amount of time, such as '34.5 seconds'. This class models a quantity or amount of time in terms of seconds and nanoseconds. … See `Period` for the date-based equivalent to this class."

> `dividedBy(long divisor)`: "Returns a copy of this duration divided by the specified value. … a `Duration` based on this duration divided by the specified divisor."
> `dividedBy(Duration divisor)` (Since: 9): "Returns number of whole times a specified Duration occurs within this Duration. … number of whole times, rounded toward zero, a specified `Duration` occurs within this Duration."
> — java.time `Duration` (https://docs.oracle.com/en/java/javase/11/docs/api/java.base/java/time/Duration.html, accessed 2026-05-30)

**`Period` has NO division method and is explicitly date-based** [Primary; accessed 2026-05-30; mirrored]:

> "A date-based amount of time in the ISO-8601 calendar system, such as '2 years, 3 months and 4 days'. This class models a quantity or amount of time in terms of years, months and days. See `Duration` for the time-based equivalent to this class. Durations and periods differ in their treatment of daylight savings time when added to `ZonedDateTime`. A `Duration` will add an exact number of seconds, thus a duration of one day is always exactly 24 hours. By contrast, a `Period` will add a conceptual day, trying to maintain the local time."
> — java.time `Period` (https://docs.oracle.com/javase/8/docs/api/java/time/Period.html, accessed 2026-05-30)

`Period`'s arithmetic is `plus*` / `minus*` / `multipliedBy(int)` / `negated()` / `normalized()` — **no `divide` / `dividedBy`** (confirmed across Java SE 8 and 11 docs). java.time matches NodaTime exactly: the fixed-length type (`Duration`) divides (including a ratio against another Duration); the calendar type (`Period`) supports neither division nor context-free total-conversion.

### Joda-Time (Primary) — scalar division only on the fixed type

> `dividedBy(long divisor)`: "Returns a copy of this duration divided by the specified value. … a `Duration` based on this duration divided by the specified divisor."
> — Joda-Time `Duration` (https://www.joda.org/joda-time/apidocs/org/joda/time/Duration.html, accessed 2026-05-30)

Joda-Time `Duration` (fixed milliseconds) divides **only by a scalar `long`** — there is no `dividedBy(ReadableDuration)` overload (a span÷span ratio was the later java.time addition, not Joda's). Joda's `Period` carries calendar fields and needs a chronology/reference to resolve to a total. Consistent with the asymmetry.

### Frink (Secondary) — currency ÷ time is first-class, but time is fixed-length

> Frink treats the US dollar as a primitive unit and tracks units through multiply/divide; `15 USD/hour * 8 hours` and converting `15 USD/hour` to other compound units are first-class.
> — Frink docs + Hillel Wayne, "The Frink is Good, the Unit is Evil" (https://www.hillelwayne.com/post/frink/, accessed 2026-05-30)

Frink (and the dimensional-analysis tradition generally — F# units of measure, Boost.Units, Haskell `dimensional`, Rust `uom`) makes money/quantity ÷ time a first-class dimensional operation. But the time base is the **fixed-length SI second** and its scalar multiples; a calendar month is not a base dimension. So these systems implicitly enforce Precept's rule from the other direction: you can divide by fixed time, and "per calendar month" simply isn't expressible as a divisor.

### Money libraries (Tertiary)

Training-data knowledge, **no excerpt obtained**: JSR-354 / Joda-Money / NodaMoney define no `money ÷ time-span → rate` primitive. Money arithmetic is money±money, money×scalar, money÷scalar, and allocate/split; a "per month/second" rate is a user-modeled compound. If correct, money÷time→rate is **not** a money-library precedent — Precept would be grounding it in the dimensional tradition (Frink et al.), not the money-library tradition. (See `research/architecture/compiler/currency-precision-coupling-survey.md`, which already engages these libraries' arithmetic surface.)

## Threats to Validity

- **Transient mid-session web outage (recovered).** Web tooling briefly returned empty/403 mid-session, then recovered. All Primary excerpts (NodaTime, java.time, Joda-Time) were captured *after* recovery and several were re-verified across multiple fetches with consistent text. This is noted for transparency; it does **not** weaken the captured Primary excerpts.
- **Money-library leg is Tertiary (no excerpt).** The "no money÷time primitive" claim rests on training-data knowledge; it is graded Tertiary and flagged here. It is **corroborating, not load-bearing** — the central conclusion stands on the NodaTime/java.time Primary excerpts. To upgrade, fetch the JSR-354 spec / Joda-Money Javadoc operator surface.
- **Version recency / version-specific facts.** NodaTime excerpts are 2.0.x; a `Duration ÷ Duration` ratio operator is present in 2.0.x (verified). java.time `Duration.dividedBy(Duration)` is Java 9+. Joda-Time has no Duration-divisor overload. These version facts are stated where load-bearing; the fixed-vs-calendar asymmetry (the load-bearing part) is stable across all surveyed versions.
- **Spec-vs-prior-art tension is real, not glossed.** Prior art draws divisibility at the **type** boundary (fixed `Duration` divides; calendar `Period` does not). Precept's current D15 draws it at the **unit-grain** boundary (sub-day grains cancel against `duration`; day-and-above stay `period`-only). The NodaTime `'2 days' ≠ '48 hours'` excerpt actually supports the *grain* intuition (days are calendar-variable), but no surveyed library exposes division-by-a-calendar-`Period`-at-all, which is what D15's `money / period → price` row contemplates. The W-C Part-B decision must reconcile these; this survey names the tension rather than resolving it.
- **Selection bias.** Comparators chosen from the task brief; representative of the temporal-library and dimensional-analysis fields but not exhaustive (Python `dateutil.relativedelta`, Rust `chrono`/`time` not fetched).

## Implications for Precept

- The fixed-vs-variable-length principle D15 invokes is **strongly and directly grounded**: NodaTime and java.time both (a) make the fixed-length type (`Duration`) divisible — by scalar and by another span — and (b) give the calendar type (`Period`) **no** division and **no** context-free total-conversion. D15's bare "NodaTime's Period/Duration split; java.time Period vs Duration" precedent can now cite this survey's verbatim excerpts.
- **For the W-C Part-B keep / delete / build-properly call:** the evidence supports **building divide-by-`duration` properly** — it is first-class in NodaTime, java.time, Joda-Time (scalar), and the entire dimensional-analysis tradition (Frink et al.). It is the well-precedented, sound half.
- **The `money / period → price` row in the current D15 table has weaker prior-art support than the `money / duration → price` row.** No surveyed library exposes division *by a calendar `Period`*. If W-C Part-B is deciding the disposition of dormant divide-by-period code, the prior-art signal is: divide-by-period is the operation no comparator offers, and NodaTime/java.time structurally refuse to even *total* a year/month-bearing period. The cleanest alignment with prior art is duration-divisor-yes / period-divisor-no — which is **narrower** than the current D15 dual-cancellation table. Whether to tighten D15 to match prior art, or keep dual-cancellation with the grain boundary as a deliberate Precept extension, is an owner decision (it touches a locked decision — Tier 3).
- **Span ÷ span ratio** (`duration ÷ duration → number`, `period ÷ period`): NodaTime and java.time (Java 9+) both support `Duration ÷ Duration → scalar`; neither supports `Period ÷ Period`. If Precept scopes a span-ratio operation, the same asymmetry applies — duration ratio is precedented, period ratio is not.

## Conclusions

**Conclusion (Primary-grounded): Divide-by-fixed-`duration` is well-precedented and sound; divide-by-calendar-`period` is the operation prior art does not offer.** Build `duration`-divisor support properly; treat `period`-divisor as the prior-art-unsupported case requiring an explicit owner decision (extend deliberately, or drop to match prior art).

- **Rationale** — A `duration`/`Duration` has a single fixed magnitude, so dividing money or quantity by it yields a rate with a well-defined denominator, and dividing two of them yields a well-defined ratio. A calendar `period`/`Period` has no fixed magnitude without an anchor date; NodaTime and java.time therefore expose **no** division on it and refuse to total a year/month-bearing period. Building duration-divisor matches the type that every surveyed library makes divisible; building period-divisor invents an operation none of them offer.
- **Alternatives considered and rejected** — (a) *Build `period`-divisor with implicit month normalization (e.g. month = 30 days)*: rejected — presents approximation as exactness (violates Precept's honesty commitment) and no surveyed system does it; NodaTime/java.time explicitly refuse to total a year/month period. (b) *Build neither / delete the dormant code wholesale*: rejected — divide-by-fixed-time is first-class across NodaTime, java.time, Joda-Time, and the dimensional tradition, so the `duration`-divisor half is the well-precedented one, not dead weight. (c) *Build both symmetrically (the current D15 dual-cancellation table, extended)*: not rejected, but **flagged** — it goes beyond prior art for the `period` side and is an owner-authorized extension, not a precedent-grounded default.
- **Precedent** — NodaTime `Duration` ("fixed (and calendar-independent)"; `/double`, `/long`, `/Duration`) vs `Period` (no division, no total-conversion, `ToDuration()` refuses years/months). java.time `Duration` (`dividedBy(long)`, `dividedBy(Duration)→long` Java 9+) vs `Period` (date-based, no divide). Joda-Time `Duration.dividedBy(long)` only. Frink: currency ÷ fixed-time is first-class dimensionally.
- **Tradeoff accepted** — Authors who think in "per calendar month" cannot write `money ÷ '1 month'(period)` under the prior-art-aligned (narrow) shape; they must use a fixed `duration` (e.g. `'30 days'`) or model the rate explicitly. The language refuses to pick a month length for them — the same tradeoff Precept's honesty commitment already accepts elsewhere.

## What would change this conclusion

- If a re-fetch shows **java.time `Period` or NodaTime `Period` actually exposes a division or a context-free total-conversion** (contradicting the captured excerpts), the "calendar type refuses division" precedent collapses and the asymmetry would need re-grounding.
- If **two or more surveyed unit-of-measure systems support a calendar-month divisor** as a well-defined dimensional operation (not just fixed seconds), the claim "prior art can't express divide-by-calendar-span" is wrong and `period`-divisor becomes defensible on precedent.
- If a **money library defines money÷time→rate as a primitive with an explicit calendar-span divisor**, the "no money-library precedent for calendar rates" claim falls and the money-library tradition would need re-weighing alongside the dimensional one.

## Open Questions

- **Reconcile the type-boundary (prior art) vs unit-grain (current D15) divisibility line.** Prior art divides at fixed-`Duration`-vs-calendar-`Period`; D15 divides at sub-day-vs-day-and-above. The NodaTime `'2 days' ≠ '48 hours'` excerpt supports the grain intuition for *addition*, but no library exposes division-by-`Period` at all. This is the central question W-C Part-B must settle, and it touches a locked decision (Tier 3 — owner-authorized).
- **Money-library leg upgrade** — fetch JSR-354 / Joda-Money operator surface for a verbatim "no money÷time primitive" confirmation (currently Tertiary).
- **`period ÷ period`** scope — neither NodaTime nor java.time offers it; if Precept wants it, it would be a Precept-original operation, not a precedented one.

## Sources

- **NodaTime `Duration` struct** — Noda Time (Jon Skeet et al.) — nodatime.org API docs, 2.0.x — **Primary** — accessed 2026-05-30 — mirrored to `research/language/references/division-by-temporal-span/nodatime-duration-period-excerpts.md` — https://www.nodatime.org/2.0.x/api/NodaTime.Duration.html
- **NodaTime `Period` class** (incl. `ToDuration` exceptions) — Noda Time — nodatime.org API docs, 2.0.x — **Primary** — accessed 2026-05-30 — mirrored (same file) — https://www.nodatime.org/2.0.x/api/NodaTime.Period.html
- **NodaTime user guide — Duration vs Period** ("one month … 31 days"; "'2 days' is not … '48 hours'") — Noda Time — nodatime.org — **Primary** — accessed 2026-05-30 — mirrored (same file)
- **java.time `Duration` Javadoc** (`dividedBy(long)`, `dividedBy(Duration)` Since 9) — Oracle (JSR-310) — Java SE 11 / SE 8 API docs — **Primary** — accessed 2026-05-30 — mirrored (same file) — https://docs.oracle.com/en/java/javase/11/docs/api/java.base/java/time/Duration.html , https://docs.oracle.com/javase/8/docs/api/java/time/Duration.html
- **java.time `Period` Javadoc** (no divide method; date-based) — Oracle (JSR-310) — Java SE 8 / SE 11 API docs — **Primary** — accessed 2026-05-30 — mirrored (same file) — https://docs.oracle.com/javase/8/docs/api/java/time/Period.html
- **Joda-Time `Duration` Javadoc** (`dividedBy(long)` only) — Joda.org (Stephen Colebourne) — Joda-Time 2.14.x API — **Primary** — accessed 2026-05-30 — mirrored (same file) — https://www.joda.org/joda-time/apidocs/org/joda/time/Duration.html
- **Frink — currency ÷ time** — Alan Eliasen (Frink) + Hillel Wayne, "The Frink is Good, the Unit is Evil" — **Secondary** — accessed 2026-05-30 — mirrored (same file) — https://www.hillelwayne.com/post/frink/
- **JSR-354 / Joda-Money / NodaMoney** (no money÷time primitive) — **Tertiary** (training-data; no excerpt) — see also internal `research/architecture/compiler/currency-precision-coupling-survey.md`.
