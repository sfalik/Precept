# Temporal-span division & total-conversion — verbatim excerpts (mirror)

Captured during the `division-by-temporal-span-prior-art` survey, snapshotted to defend
against URL rot. All NodaTime and java.time excerpts below are **Primary** (authoritative
library API docs with public versioning). Joda-Time is Primary (official Javadoc). Frink is
Secondary (named-author commentary + vendor description).

---

## NodaTime `Duration` — divides by scalar AND by another Duration

Source: https://www.nodatime.org/2.0.x/api/NodaTime.Duration.html (accessed 2026-05-30)

Struct summary, verbatim:

> "Represents a fixed (and calendar-independent) length of time."

> "a duration represents a fixed length of elapsed time along the time line that occupies
> the same amount of time regardless of when it is applied" — contrasted with Period, which
> "operates in calendrical terms."

Division members, verbatim signatures + summaries:

> `public static double operator /(Duration left, Duration right)`
> "Implements the operator / (division) to divide one duration by another."

> `public static Duration operator /(Duration left, double right)`
> "Implements the operator / (division) to divide a duration by a System.Double."

> `public static Duration operator /(Duration left, long right)`
> "Implements the operator / (division) to divide a duration by an System.Int64."

> `public static double Divide(Duration left, Duration right)`
> "Divides one duration by another. Friendly alternative to `operator/()`."

So NodaTime `Duration` supports both **scalar division** (`/double`, `/long` → `Duration`)
and **ratio division** (`/Duration` → `double`). The fixed-length type divides cleanly in
every direction because it has a single well-defined magnitude.

---

## NodaTime `Period` — no division, no total-conversion; ToDuration refuses years/months

Source: https://www.nodatime.org/2.0.x/api/NodaTime.Period.html (accessed 2026-05-30)

Class summary, verbatim:

> "Represents a period of time expressed in human chronological terms: hours, days, weeks,
> months and so on."

Arithmetic members: `operator +`, `operator -`, `Add(Period, Period)`,
`Subtract(Period, Period)`. **No division operator, no `Divide` method.**

Total-conversion: **No `TotalHours`, `TotalDays`, or equivalent total-conversion property.**

`ToDuration()` method, verbatim summary + exception:

> "For periods that do not contain a non-zero number of years or months, returns a duration
> for this period assuming a standard 7-day week, 24-hour day, 60-minute hour etc."
> Throws `InvalidOperationException`: "The month or year property in the period is non-zero."
> Throws `OverflowException`: "The period doesn't have years or months, but the calculation
> overflows the bounds of Duration."

This is the load-bearing asymmetry: NodaTime converts a Period to a fixed Duration **only
when it contains no years or months**. The calendar components are structurally excluded
from total-conversion because they have no fixed magnitude.

---

## NodaTime concepts — why a period varies in length

Source: NodaTime user-guide / concepts (accessed 2026-05-30; surfaced via nodatime.org docs)

> "if you add 'one month' to January 1st, that's going to be 31 days long. Adding the same
> period to February 1st will give a shorter length of time - which then depends on whether
> the year is a leap year or not."

> "Periods aren't normalized, so a period of '2 days' is not the same as a period of '48
> hours'; likewise if you ask for the number of hours in a period of '1 day' the answer will
> be 0, not 24."

> "While a minute is a fixed length of time, a month isn't - so the concept of adding '3
> months' to an instant makes no sense."

---

## java.time `Duration` — dividedBy(long) and dividedBy(Duration)→long (Java 9+)

Source: https://docs.oracle.com/en/java/javase/11/docs/api/java.base/java/time/Duration.html
(accessed 2026-05-30); class summary also confirmed from Java SE 8 docs.

Class summary, verbatim:

> "A time-based amount of time, such as '34.5 seconds'. This class models a quantity or
> amount of time in terms of seconds and nanoseconds. … the `DAYS` unit can be used and is
> treated as exactly equal to 24 hours, thus ignoring daylight savings effects. See `Period`
> for the date-based equivalent to this class."

`dividedBy(long divisor)`, verbatim:

> "Returns a copy of this duration divided by the specified value. … `divisor` - the value
> to divide the duration by, positive or negative, not zero … Returns: a `Duration` based on
> this duration divided by the specified divisor"

`dividedBy(Duration divisor)`, verbatim (Since: 9):

> "Returns number of whole times a specified Duration occurs within this Duration. …
> Returns: number of whole times, rounded toward zero, a specified `Duration` occurs within
> this Duration, may be negative … Since: 9"

---

## java.time `Period` — NO division method; date-based, DST-conceptual

Source: https://docs.oracle.com/javase/8/docs/api/java/time/Period.html (accessed 2026-05-30)

Class summary, verbatim:

> "A date-based amount of time in the ISO-8601 calendar system, such as '2 years, 3 months
> and 4 days'. This class models a quantity or amount of time in terms of years, months and
> days. See `Duration` for the time-based equivalent to this class.
>
> Durations and periods differ in their treatment of daylight savings time when added to
> `ZonedDateTime`. A `Duration` will add an exact number of seconds, thus a duration of one
> day is always exactly 24 hours. By contrast, a `Period` will add a conceptual day, trying
> to maintain the local time."

Arithmetic methods: `plus`, `plusYears`, `plusMonths`, `plusDays`, `minus`, `minusYears`,
`minusMonths`, `minusDays`, `multipliedBy(int scalar)`, `negated()`, `normalized()`.
**No `divide` / `dividedBy` method.** (Confirmed across Java SE 8 and 11 docs.)

Note on normalization, verbatim:

> "a period of '15 months' is different to a period of '1 year and 3 months'" — months are
> not auto-normalized into years; `normalized()` must be called explicitly.

---

## Joda-Time `Duration` — dividedBy(long) only, no Duration divisor

Source: https://www.joda.org/joda-time/apidocs/org/joda/time/Duration.html (accessed 2026-05-30)

`dividedBy(long divisor)`, verbatim:

> "Returns a copy of this duration divided by the specified value. … `divisor` - the value
> to divide the duration by, positive or negative, not zero … Returns: a `Duration` based on
> this duration divided by the specified divisor"

There is **no** `dividedBy(ReadableDuration)` overload — Joda-Time `Duration` divides only by
a scalar `long`. (Joda-Time `Duration` is a fixed millisecond length; `Period` carries
calendar fields and needs a chronology/reference to resolve to a total.)

---

## Frink — currency ÷ time is first-class (dimensional)

Source: Frink documentation + Hillel Wayne, "The Frink is Good, the Unit is Evil"
(https://www.hillelwayne.com/post/frink/, accessed 2026-05-30) — Secondary.

Frink treats the US dollar as a primitive unit and tracks units through multiply/divide, so
`15 USD/hour * 8 hours` and converting `15 USD/hour` to other compound units are first-class
operations. Frink's time base is the (fixed-length) SI second and its scalar multiples; it
does not model a calendar month as a divisible base dimension.
