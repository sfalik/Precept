# NodaTime Feasibility Analysis for Precept Temporal Types

**Research date:** 2026-04-13
**Author:** Frank (Lead/Architect, Language Designer)
**Relevance:** Evaluates whether Precept should adopt NodaTime as a runtime dependency to back its temporal type system, and how NodaTime's type model maps to Precept's DSL surface. Directly impacts `date` type proposal (#26) and future temporal type proposals.

---

## Background

Precept's `date` type proposal (#26) currently specifies `System.DateOnly` as the backing type and defines a single `date` type at day granularity. George's design review of #26 raised four open challenges:

1. The "Before/After" prevention claim is dishonest — `DueDate + MealsTotal` compiles because `date + number → date` is defined
2. `date + number` should reject fractional offsets — `date + 2.5` is meaningless for day granularity
3. `nullable + default` prohibition seems arbitrary
4. `date - date → number` should be `→ integer` since day counts are whole

Shane's question: **If we adopt NodaTime as a dependency, should Precept's temporal datatypes align with NodaTime's type model instead of rolling our own?**

This analysis evaluates the feasibility, fit, and risks of NodaTime adoption through the lens of Precept's locked design philosophy and language principles.

---

## 1. Philosophy Alignment

### 1.1 "Make invalid states unrepresentable" — shared root principle

NodaTime's core design philosophy is: *"We want to force you to think about decisions you really need to think about. In particular, what kind of data do you really have and really need?"* (NodaTime design docs). This manifests as distinct types for distinct temporal concepts: `LocalDate` is not `LocalDateTime` is not `ZonedDateTime` is not `Instant`. You cannot accidentally mix a timezone-free date with a global timestamp — the type system prevents it.

Precept's prevention guarantee (philosophy §"What makes it different") says: *"Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist."*

These are the same principle applied at different layers. NodaTime prevents invalid temporal representations at the type level. Precept prevents invalid entity configurations at the contract level. Adopting NodaTime means the temporal foundation inherits a battle-tested application of the principle Precept already enforces — rather than Precept reimplementing temporal type safety from scratch atop `System.DateOnly`.

**Philosophy fit: Strong alignment.** Both NodaTime and Precept reject implicit defaults, reject ambiguous representations, and force the author to be explicit about what their data means. NodaTime's refusal to default to the system time zone mirrors Precept's rejection of hidden state and deployment-context coupling.

### 1.2 Separation of concerns → determinism

NodaTime deliberately separates:
- **Date vs. time** — `LocalDate` vs `LocalTime` (you cannot accidentally add hours to a date)
- **Calendar arithmetic vs. elapsed time** — `Period` vs `Duration` (adding "1 month" is not the same as adding "30 days")
- **Local vs. global** — `LocalDate` vs `Instant` (a date in your calendar vs. a point on the universal timeline)
- **User-driven vs. clock-driven** — the type choice guidance explicitly distinguishes between values a user enters and values a system records

This separation maps directly to Precept's determinism guarantee (Language Design §1): *"Fire/inspect always produces the same result for the same inputs."* NodaTime's `LocalDate` is deterministic by construction — no timezone, no clock reference, no ambiguity. `Period`-based arithmetic (add 1 month) follows explicit, documented rules (truncate to valid day, process largest units first). There is no hidden calendar context that could produce different results on different machines.

**Determinism fit: Strong alignment.** NodaTime's `LocalDate` and date-unit `Period` are the two types that participate in Precept's day-granularity world. Both are fully deterministic. The dangerous types (`ZonedDateTime`, `Instant`, `Duration`) are exactly the ones Precept should not expose — and NodaTime's type system makes it structurally impossible to accidentally use them when you meant to use `LocalDate`.

### 1.3 Battle-tested temporal model vs. rolling our own

NodaTime is authored by Jon Skeet, has been stable since 2012, is used in production at Google and in the .NET ecosystem broadly, and has a rigorous test suite covering edge cases (leap years, month-end truncation, Hebrew calendar arithmetic). The `LocalDate` + `Period` arithmetic rules are documented, predictable, and extensively tested.

Building the same guarantees on `System.DateOnly` would require Precept to:
- Implement month/year arithmetic with truncation rules
- Handle leap-year edge cases
- Define and document the exact behavior of every edge case
- Test all of this independently

`System.DateOnly` provides day arithmetic (`AddDays`) but delegates month/year arithmetic to `DateTime` conversion, which reintroduces the ambiguity problems NodaTime was designed to solve.

**Philosophy fit: Strong win.** Precept's Principle #8 (sound analysis) and Principle #9 (tooling drives syntax) both argue against reimplementing solved problems. Using NodaTime's `LocalDate` as the backing type gets us a correct, edge-case-tested temporal foundation. Using `System.DateOnly` gets us a simpler dependency but requires Precept to own temporal arithmetic correctness — a domain Precept has no expertise in and no reason to compete in.

---

## 2. Type Mapping — What Makes Sense for Precept

| NodaTime Type | Precept DSL Candidate? | DSL Surface Name | Philosophy Fit | Determinism Risk | Domain Demand |
|---|---|---|---|---|---|
| `LocalDate` | **Yes — v1** | `date` | Perfect. Day-only, no TZ, no time. Matches "user entered a date" model. | None. Fully deterministic. | 30 of 100 surveyed fields need dates. All 10 domains. |
| `LocalTime` | **Deferred — v2/v3** | `time` | Good fit for appointment times, business hours. But timezone question is unresolved per #26 deferral rationale. | Low if timezone-free (local time only). High if combined with timezone. | Clinic scheduling, restaurant reservations, work shifts. Present but secondary to date. |
| `Period` | **Internal only — not a DSL type** | N/A | Period is the *mechanism* for date arithmetic, not a field type authors would declare. No domain field is "a period." | None — used only in expression evaluation. | N/A — arithmetic mechanism, not a data type. |
| `Duration` | **No — not for v1-v3** | N/A | Duration represents elapsed nanoseconds. Precept's day-granularity world has no use for sub-second time measurement. Duration is for `Instant` arithmetic; Precept uses `LocalDate` arithmetic. | High — implies global timeline, which Precept deliberately avoids. | None in the surveyed domain set. |
| `Instant` | **No** | N/A | Instant represents a point on the global timeline. This is system-generated clock data. Precept governs user-declared entity data, not timestamps. | Deterministic in isolation, but interpreting it requires a timezone — violates one-file completeness. | Audit logs, system timestamps — outside Precept's governance scope. |
| `ZonedDateTime` | **No** | N/A | Requires a timezone reference, which couples the precept file to deployment context. Directly violates one-file completeness and determinism. | **Fatal** — same `ZonedDateTime` value behaves differently depending on the IANA database version installed. | None. Precept definitions must be location-independent. |
| `LocalDateTime` | **Deferred — after `time` lands** | `datetime` | Combines date and time. Only meaningful after `time` is established. | Low (no TZ), but combined date+time arithmetic introduces "what hour on Feb 29?" edge cases. | Meeting scheduling, appointment slots, deadline timestamps. |
| `OffsetDateTime` | **No** | N/A | Carries a UTC offset — partial timezone information. Same determinism concerns as `ZonedDateTime`, weaker than full TZ. | High — offset without zone rules is misleading. | None in entity modeling. |
| `AnnualDate` | **Interesting — future research** | `monthday` or similar | Represents a month-day pair (e.g., birthday, anniversary). No year component. | None — it's a subset of `LocalDate`. | HR systems, insurance renewal dates, annual events. Niche but real. |
| `YearMonth` | **Interesting — future research** | `yearmonth` or similar | Represents a year-month pair (e.g., billing period, fiscal month). | None. | Billing cycles, reporting periods, fiscal months. Real demand in SaaS/finance. |

### Key insight: Precept's DSL surface should expose `date` (and eventually `time`, `datetime`). NodaTime provides the *backing types*, not the DSL vocabulary.

The DSL author writes `field DueDate as date`. The runtime stores a `NodaTime.LocalDate`. The author never sees `LocalDate`, `Period`, or any NodaTime type name. NodaTime is an implementation dependency, not a surface dependency. This is the same relationship Precept has with `System.Decimal` for the `decimal` type — the author writes `decimal`, the runtime uses `System.Decimal`.

---

## 3. Impact on Proposal #26

### 3.1 What changes if we adopt NodaTime's `LocalDate`?

**Backing type:** `System.DateOnly` → `NodaTime.LocalDate`. The DSL surface (`date`, `date("2026-01-15")`) is unchanged. Authors see no difference.

**Internal arithmetic:** `DateOnly.AddDays(n)` → `LocalDate.PlusDays(n)`. Both are correct for integer-day offsets. But `LocalDate` also provides `PlusMonths`, `PlusYears`, and `PlusWeeks` with well-defined truncation semantics — capabilities `DateOnly` does not offer natively.

**Accessors:** #26 proposes `.year`, `.month`, `.day`. `LocalDate` provides `Year`, `Month`, `Day`, plus `DayOfWeek`, `DayOfYear`, and calendar-aware equivalents. The accessor surface expands naturally without custom implementation.

**Parsing:** `date("2026-01-15")` → parse via `LocalDatePattern.Iso.Parse(value)` instead of `DateOnly.ParseExact`. NodaTime's parser provides better error messages and stricter ISO 8601 validation.

**Comparison:** `LocalDate` implements `IComparable<LocalDate>`, `IEquatable<LocalDate>`, and operator overloads (`<`, `>`, `<=`, `>=`, `==`, `!=`). Same as `DateOnly`.

### 3.2 Does NodaTime's `Period` solve the deferred duration problem?

**Partially, and importantly.** The `date + number → date` design in #26 means "add N days." This works for day offsets but cannot express "add 1 month" — because a month is not a fixed number of days.

NodaTime's `Period` solves this cleanly:
- `Period.FromDays(7)` — add 7 days (same as current `date + 7`)
- `Period.FromMonths(1)` — add 1 month (with truncation: Jan 31 + 1 month = Feb 28)
- `Period.FromYears(1)` — add 1 year (with leap-year truncation)
- `Period.Between(date1, date2, PeriodUnits.Days)` — exact day count between dates

This means Precept *could* (in a future proposal) offer:
```precept
set RenewalDate = OriginalDate + months(1)
set AnniversaryDate = StartDate + years(1)
```

Without NodaTime, implementing month/year arithmetic on `System.DateOnly` requires converting to `DateTime`, doing the arithmetic, converting back — reintroducing the ambiguity NodaTime was designed to eliminate.

**Recommendation:** Do NOT expose `Period` as a DSL type in v1. Use it internally if/when month/year arithmetic is added in a future proposal. The v1 `date + integer → date` (day offset) is correct and sufficient.

### 3.3 Does `LocalDate + Period` give us cleaner month/year arithmetic than `date + number`?

**Yes, definitively.** `date + number` can only mean "add N days" because `number` has no unit. `LocalDate + Period.FromMonths(1)` explicitly means "add 1 month" with documented truncation rules. This is a future capability that NodaTime enables but `System.DateOnly` does not, and it's a natural extension of the Precept DSL:

```precept
# Future syntax (NOT v1)
set NextPayment = CurrentDate + months(1)
set ReviewDate = HireDate + years(1)
```

These `months()` and `years()` functions would be thin wrappers around `Period.FromMonths` and `Period.FromYears`. The arithmetic rules are NodaTime's — Precept does not need to invent them.

### 3.4 How does NodaTime address George's four challenges?

**Challenge 1: `DueDate + MealsTotal` compiles because `date + number → date` is defined.**

NodaTime does not change this directly — the issue is in Precept's type checker, not the backing type. However, if #26 is revised to use `date + integer → date` (matching the `date - date → integer` return type change in Challenge 4), then `DueDate + MealsTotal` becomes a type error when `MealsTotal` is `decimal` — because `date + decimal` would not be defined. NodaTime strengthens the argument for this by making the arithmetic contract explicit: `LocalDate.PlusDays(int)` takes an integer, not a floating-point value.

**Challenge 2: `date + 2.5` should reject fractional offsets.**

NodaTime's `LocalDate.PlusDays(int days)` takes an `int`, not a `double`. This is exactly the right constraint. In Precept's type system, `date + integer → date` is well-defined; `date + number → date` and `date + decimal → date` should be type errors. NodaTime's API reinforces this: day-granularity arithmetic operates on whole numbers, period. This directly resolves George's challenge by making the contract match the backing API.

**Challenge 3: `nullable + default` prohibition seems arbitrary.**

This is a semantic design question independent of the backing type. NodaTime has no opinion here — `LocalDate` is a value type and does not have a null state. The prohibition in #26 is about Precept's field semantics, not the backing type.

**Challenge 4: `date - date → number` should be `→ integer`.**

NodaTime's `Period.Between(date1, date2, PeriodUnits.Days)` returns a `Period` whose `Days` property is an `int`. This confirms George's position: day counts between dates are always whole numbers. `date - date → integer` is correct, and NodaTime's API makes this obvious. With `System.DateOnly`, the equivalent is `date2.DayNumber - date1.DayNumber`, which also returns an `int` — but the NodaTime API is more explicit about the semantics.

**Summary:** NodaTime directly resolves Challenge 2 (fractional day rejection) and confirms Challenge 4 (integer return type). Challenge 1 is partially addressed by the type-system narrowing that follows from Challenge 2. Challenge 3 is orthogonal to the backing type.

---

## 4. Impact on Future Temporal Proposals

### 4.1 Roadmap for `time` type

NodaTime's `LocalTime` is the natural backing type for a future Precept `time` type — timezone-free, representing a time-of-day. `LocalTime` provides:
- Constructors: `new LocalTime(14, 30)` → 2:30 PM
- Accessors: `Hour`, `Minute`, `Second`, `Millisecond`
- Arithmetic: `PlusHours`, `PlusMinutes` — wraps around midnight transparently
- Comparison: full operator set

A future Precept `time("14:30")` type maps cleanly to `LocalTime`. The same pattern as `date`: DSL keyword, ISO 8601 constructor, NodaTime backing type.

Without NodaTime, `System.TimeOnly` provides similar functionality. But `TimeOnly` was introduced in .NET 6 with less rigorous edge-case handling than NodaTime's `LocalTime`, and NodaTime provides the arithmetic integration between `LocalDate` and `LocalTime` that would be needed for `LocalDateTime`-backed `datetime` type.

### 4.2 `Duration` vs. `Period` distinction

NodaTime's clean separation is illuminating for Precept's future:

- **`Period`** — calendar-based units: years, months, days. Adding "1 month" to Jan 31 gives Feb 28 (or 29). Periods are not a fixed length of time. Used with local types (`LocalDate`, `LocalTime`, `LocalDateTime`).
- **`Duration`** — fixed elapsed time in nanoseconds. Adding "86400 seconds" is always the same real-world interval. Used with global types (`Instant`, `ZonedDateTime`).

For Precept:
- **`Period`-based arithmetic is the right model.** Precept governs business entities with calendar-based deadlines, renewal dates, and scheduling. "Add 1 month to the due date" is a `Period` operation. "30 days from now" is also a `Period` operation (days are fixed in length). Precept never needs to compute "how many nanoseconds between two instants."
- **`Duration` is irrelevant to Precept.** Duration belongs to the global timeline — elapsed real time, system scheduling, timeout measurement. Precept's domain is entity data integrity, not system time management.

### 4.3 Implementation cost comparison

**NodaTime-backed:**
- Add NuGet dependency: `NodaTime` (core) + `NodaTime.Serialization.SystemTextJson` (JSON)
- `date` field backed by `LocalDate`, serialized as ISO 8601 string
- `date(...)` constructor uses `LocalDatePattern.Iso.Parse`
- Day arithmetic: `localDate.PlusDays(n)` — already handles all edge cases
- Future month/year arithmetic: `localDate.Plus(Period.FromMonths(n))` — already tested
- Future `time`: `LocalTime`, same pattern
- Future `datetime`: `LocalDateTime`, same pattern

**System.DateOnly-backed:**
- No additional dependency
- `date` field backed by `DateOnly`, serialized via custom converter
- `date(...)` constructor uses `DateOnly.ParseExact` or `DateOnly.Parse`
- Day arithmetic: `dateOnly.AddDays(n)` — correct, simpler
- Future month/year arithmetic: must convert to `DateTime`, add, convert back — reenters the problem space NodaTime solves
- Future `time`: `TimeOnly` — separate type, no integration with date arithmetic
- Future `datetime`: `DateTime` or custom composition — back to the BCL problems NodaTime was designed to replace

**Cost assessment:** NodaTime has a higher up-front cost (dependency + serialization setup) but a significantly lower marginal cost for each subsequent temporal type. `System.DateOnly` is cheaper for v1 in isolation but becomes increasingly expensive as temporal features accumulate.

---

## 5. Risks and Concerns

### 5.1 Does NodaTime violate Precept principles?

**Dependency concern:** Precept currently depends only on the .NET BCL and Superpower (parser). Adding NodaTime is the third external dependency. However, NodaTime is a mature, well-maintained library with stable APIs and semantic versioning. It is categorically different from adding a large framework dependency.

**One-file completeness:** NodaTime does not affect the `.precept` file surface. The author still writes `field DueDate as date`. NodaTime is a runtime implementation detail, invisible to the DSL author. No violation.

**Determinism:** NodaTime's `LocalDate` and `Period` are fully deterministic — no system clock, no timezone database, no locale dependency. The types Precept would use are the deterministic subset of NodaTime. The non-deterministic types (`ZonedDateTime`, `Instant`) are the ones Precept will never expose. No violation.

**AI-first legibility:** NodaTime's types are not exposed in the DSL surface. The MCP tools return `date` values as ISO 8601 strings. AI consumers never see `NodaTime.LocalDate`. No violation.

### 5.2 Serialization

NodaTime types do not serialize to JSON natively with `System.Text.Json`. The `NodaTime.Serialization.SystemTextJson` package provides converters. This adds a second NuGet dependency.

**Mitigation:** Precept already serializes field values through its own MCP DTO layer. The serialization boundary is controlled:
- MCP tools serialize dates as ISO 8601 strings (e.g., `"2026-01-15"`)
- The `PreceptInstance` data dictionary stores typed values; JSON serialization happens at the MCP/API boundary
- `NodaTime.Serialization.SystemTextJson` configures `JsonSerializerOptions` with NodaTime converters — one-time setup

This is a manageable engineering task, not a design risk.

### 5.3 API surface leakage

**Risk:** NodaTime types could leak into Precept's public C# API. If `PreceptInstance.Data["DueDate"]` returns a `NodaTime.LocalDate`, consumers of the C# API are forced to take a NodaTime dependency.

**Mitigation options:**
1. **Wrap at the API boundary.** Store `NodaTime.LocalDate` internally; expose `System.DateOnly` or ISO 8601 strings at the public API. This adds conversion overhead but isolates consumers.
2. **Accept the transitive dependency.** NodaTime is a common .NET library; requiring it as a transitive dependency is not unusual. Entity Framework, ASP.NET Core, and many other libraries have transitive dependencies.
3. **Expose both.** Provide `GetDateOnly()` and `GetLocalDate()` accessors. Consumers choose their preferred representation.

**Recommendation:** Option 2 (accept transitive dependency) for v1, with Option 3 as a convenience addition. NodaTime is lightweight (no native dependencies, no runtime requirements) and is already a de-facto standard in .NET temporal processing. Forcing consumers to convert at every boundary creates friction without proportional benefit.

### 5.4 NodaTime maintenance and longevity

NodaTime is authored by Jon Skeet and maintained under the `nodatime` GitHub org. It has been actively maintained since 2012 (14+ years). The latest stable release is v3.3.1. It is widely used in production by Google (Skeet's employer) and across the .NET ecosystem.

**Risk:** Development could slow or stop.

**Mitigation:** NodaTime's API surface for the types Precept uses (`LocalDate`, `LocalTime`, `Period`) has been stable since v2 (2017). Even if development stopped entirely, these types would continue to work indefinitely — they have no external dependencies (no IANA database needed for local types, no system clock interaction). The library is MIT-licensed, so Precept could fork if necessary (extremely unlikely).

**Assessment:** Low risk. NodaTime's maturity and stability are an argument *for* adoption, not against it.

### 5.5 Calendar system complexity

NodaTime supports multiple calendar systems (Gregorian, Julian, Hebrew, Coptic, etc.). `LocalDate` carries a calendar system reference. By default, this is ISO-8601 (effectively Gregorian).

**Risk:** An author could construct a `LocalDate` in a non-ISO calendar, producing behavior that breaks Precept's determinism guarantee.

**Mitigation:** Precept's `date("YYYY-MM-DD")` constructor always parses in the ISO calendar. There is no DSL syntax for specifying a non-ISO calendar. NodaTime's calendar flexibility is available to the runtime but not exposed to the DSL — the DSL constrains the API surface. This is safe.

---

## 6. Recommendation

### Adopt NodaTime as the backing type library for Precept's temporal types.

**Rationale:**

1. **Philosophy alignment is near-perfect.** NodaTime's "force you to think about what your data means" principle maps directly to Precept's prevention guarantee. Its type separation (date vs. time vs. duration vs. timezone) maps to Precept's determinism guarantee. This is not a coincidental overlap — both libraries are designed to make invalid representations structurally impossible.

2. **It directly resolves two of George's four challenges.** NodaTime's `LocalDate.PlusDays(int)` API enforces integer-only day arithmetic (Challenge 2: reject fractional offsets) and `Period.Between` returns integer day counts (Challenge 4: `date - date → integer`). Challenge 1 is partially addressed by the narrowing that follows.

3. **It provides a tested roadmap for future temporal types.** `time` → `LocalTime`, `datetime` → `LocalDateTime`, month/year arithmetic → `Period`. Each future temporal type has a battle-tested NodaTime backing type waiting for it. With `System.DateOnly`, each future type requires Precept to reinvent temporal arithmetic.

4. **The marginal cost decreases with each temporal type added.** The up-front cost (NodaTime + serialization NuGet packages, API boundary decisions) is paid once. Each subsequent temporal type (`time`, `datetime`, month/year arithmetic) is a thin DSL-to-NodaTime mapping, not a from-scratch implementation.

5. **The risk is low and manageable.** NodaTime is MIT-licensed, stable for 14 years, has no native dependencies, and its `LocalDate`/`Period` API surface is frozen. The serialization concern is solved by a companion NuGet package. The API leakage concern is mitigated by accepting the transitive dependency (standard practice in .NET).

### Proposed type mapping and sequencing

| Phase | DSL Keyword | NodaTime Backing Type | Notes |
|---|---|---|---|
| v1 (Proposal #26) | `date` | `NodaTime.LocalDate` | Day-only. ISO 8601 constructor. `date + integer → date`, `date - date → integer`. |
| v2 (future) | `time` | `NodaTime.LocalTime` | Time-of-day. No timezone. `time("14:30")` constructor. |
| v2 (future) | `months(n)`, `years(n)` | `NodaTime.Period` | Temporal offset functions, not types. `date + months(1) → date`. |
| v3 (future) | `datetime` | `NodaTime.LocalDateTime` | Combined date+time. No timezone. |

Types that **should never become Precept DSL types:** `Instant`, `ZonedDateTime`, `OffsetDateTime`, `Duration`. These are global/clock types that violate Precept's determinism and one-file-completeness guarantees.

### Impact on #26 revisions

If NodaTime is adopted, #26 should be revised as follows:

1. **Backing type:** `System.DateOnly` → `NodaTime.LocalDate`
2. **Date arithmetic:** `date + number → date` → `date + integer → date` (directly mirrors `LocalDate.PlusDays(int)`)
3. **Date subtraction:** `date - date → number` → `date - date → integer` (mirrors `Period.Between` day count)
4. **Fractional day offset:** `date + 2.5` becomes a type error (Challenge 2 resolved by construction)
5. **Cross-type arithmetic:** `DueDate + MealsTotal` where `MealsTotal` is `decimal` becomes a type error (Challenge 1 partially resolved)
6. **Implementation notes:** Reference NodaTime APIs for parsing, arithmetic, and comparison
7. **Serialization:** Add note about `NodaTime.Serialization.SystemTextJson` for MCP/API JSON output

---

## Philosophy Gap Flag

⚠️ **Potential gap: NodaTime enables capabilities the philosophy doesn't describe.**

If NodaTime is adopted, Precept gains *potential access* to:
- Calendar-system-aware arithmetic (Hebrew, Coptic calendars)
- Sub-day temporal arithmetic (`LocalTime`, `LocalDateTime`)
- Period-based "add 1 month" operations that involve calendar-specific truncation rules

The current philosophy describes Precept as governing "fields (scalar values and typed collections)" with "prevention, not detection." It does not specifically describe the domain of temporal arithmetic, calendar-sensitive operations, or the boundaries of what "date" means in the governance context.

**This is not a blocking concern.** The DSL surface controls what's exposed, and v1 exposes only `date` (day-only, ISO calendar). But the philosophy should eventually address: *What temporal operations does Precept govern, and where does it defer to the host application?* Flagging this per the copilot instructions — I am not resolving it myself.

---

## Appendix: NodaTime vs. System.DateOnly Feature Comparison

| Capability | System.DateOnly | NodaTime LocalDate | Precept Implication |
|---|---|---|---|
| Day-only date representation | ✅ | ✅ | Both work for v1 |
| ISO 8601 parsing | ✅ (manual format) | ✅ (built-in pattern) | NodaTime's parser is more robust |
| Day arithmetic (add N days) | ✅ `AddDays(int)` | ✅ `PlusDays(int)` | Equivalent |
| Month arithmetic | ❌ (requires DateTime conversion) | ✅ `PlusMonths(int)` with truncation | NodaTime wins for v2+ |
| Year arithmetic | ❌ (requires DateTime conversion) | ✅ `PlusYears(int)` with leap handling | NodaTime wins for v2+ |
| Day count between dates | ✅ `DayNumber` subtraction | ✅ `Period.Between` | Both work |
| Day-of-week | ✅ `DayOfWeek` | ✅ `DayOfWeek` (NodaTime `IsoDayOfWeek`) | Both work |
| Integration with time-of-day | ❌ (separate `TimeOnly`) | ✅ `LocalDate + LocalTime → LocalDateTime` | NodaTime provides composition |
| JSON serialization | ✅ (native `System.Text.Json`) | ⚠️ (requires `NodaTime.Serialization.SystemTextJson`) | Small additional dependency |
| Dependency weight | None (BCL) | ~500 KB NuGet package | Minimal |
| API stability | Stable since .NET 6 (2021) | Stable since v2 (2017) | Both mature |
| Edge-case coverage | Basic | Extensive (documented truncation rules, calendar support) | NodaTime is more thorough |

---

## References

- NodaTime core concepts: https://nodatime.org/3.2.x/userguide/concepts
- NodaTime type choice guidance: https://nodatime.org/3.2.x/userguide/type-choices
- NodaTime design philosophy: https://nodatime.org/3.2.x/userguide/design
- NodaTime date arithmetic: https://nodatime.org/3.2.x/userguide/arithmetic
- Precept philosophy: `docs/philosophy.md`
- Precept language design: `docs/PreceptLanguageDesign.md`
- Type system domain survey: `research/language/expressiveness/type-system-domain-survey.md`
- Type system formal survey: `research/language/references/type-system-survey.md`
- Proposal #26: `date` type (GitHub issue)
