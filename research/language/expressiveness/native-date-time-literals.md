# External Research: Native ISO Date/Time Literals

**Research date:** 2026-04-14
**Author:** Frank (Lead/Architect, Language Designer)
**Status:** External research — RESOLVED by Decision #18 (typed literals without string quotes)
**Context:** Temporal type system proposal originally included string-wrapped constructors (`date("2024-01-15")`, `time("14:30")`, `instant("2024-01-15T14:30:00Z")`) and postfix unit literals (`30 days`, `72 hours`). This research investigated whether to ADD native ISO literal forms (`2024-01-15`, `14:30`) alongside constructors. Decision #18 resolved this differently: typed literals with unquoted content inside typed parens (`date(2024-01-15)`). See §11.

---

## 1. VB Date Literals — What Exactly Was the Mistake?

### The VB.NET Design

VB.NET provides `Date` literals enclosed in `#` delimiters: `#5/31/1993#`, `#1993-5-31#`, `#8/13/2002 12:14 PM#`. The `Date` data type maps to `System.DateTime` (IEEE 64-bit, 100-nanosecond precision).

Source: [Microsoft VB Date Data Type reference](https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/data-types/date-data-type)

### The Four Distinct Problems

**Problem 1 — Locale-dependent format ambiguity.** VB allows `M/d/yyyy` (US) format. The Microsoft docs explicitly state the problem: *"Suppose you hard-code a Date literal of #3/4/1998# and intend it to mean March 4, 1998. In a locale that uses mm/dd/yyyy, 3/4/1998 compiles as you intend. But suppose you deploy your application in many countries/regions. In a locale that uses dd/mm/yyyy, your hard-coded literal would compile to April 3, 1998."* The compiler also accepts `yyyy-MM-dd` with slashes (year-first), but the locale-dependent form is the one everyone uses.

**Problem 2 — Type conflation.** VB's `Date` type holds both date AND time. There is no separate `Date`-only or `Time`-only type. `#5/31/1993#` silently defaults the time to midnight. `#1:15:30 PM#` silently defaults the date to January 1, 0001. This means date-only and time-only values are indistinguishable at the type level from full date+time values.

**Problem 3 — The `#` delimiter is non-standard.** No other mainstream language uses `#` for date literals. It's a VB-specific syntactic novelty that doesn't correspond to any ISO standard or format convention. Copy-pasting dates from external sources always requires adding delimiters.

**Problem 4 — Weak validation.** The underlying `System.DateTime` is widely criticized for its ambiguous semantics (Jon Skeet's NodaTime motivation). VB inherits all of `DateTime`'s problems: no timezone awareness, ambiguous `Kind` property, naive equality comparison across timezone boundaries.

### Does Precept's Design Address These Problems?

| VB Problem | Precept's Design | Addressed? |
|---|---|---|
| **Locale-dependent format** | ISO 8601 only (`yyyy-MM-dd`). No `mm/dd/yyyy` or `dd/mm/yyyy`. Year-month-day is unambiguous in every locale. | **Yes — fully addressed.** The format `2024-01-15` has exactly one interpretation worldwide. |
| **Type conflation** | Separate types: `date`, `time`, `instant`, `datetime`. A `date` literal cannot accidentally carry a time component. | **Yes — fully addressed.** NodaTime's type model prevents conflation by construction. |
| **Non-standard delimiter** | No delimiter proposed. Bare ISO format: `2024-01-15` instead of `#1/15/2024#`. | **Yes — addressed by omission.** The ISO format IS the standard. No delimiter needed. |
| **Weak validation** | NodaTime backing validates calendar correctness, separate types encode semantic intent, compile-time type checking prevents misuse. | **Yes — fully addressed.** NodaTime was built specifically to fix every `DateTime` problem. |

**Assessment: The VB precedent does NOT apply.** VB's date literal problems were caused by locale ambiguity, type conflation, non-standard delimiters, and weak backing types. Precept would have ISO-only format, separate types, no delimiters, and NodaTime backing. The VB-era objection is a valid historical lesson but not a disqualifying precedent for this design.

---

## 2. Precedent Survey — Who Has Done This, and How?

### Programming Languages

| Language/System | Literal Syntax | Type | Success? | Why? |
|---|---|---|---|---|
| **VB.NET** | `#1/15/2024#` | `Date` (DateTime) | ❌ Failure | Locale ambiguity, type conflation, non-standard delimiter |
| **SQL (ANSI/ISO)** | `DATE '2024-01-15'`, `TIME '14:30:00'`, `TIMESTAMP '...'` | Typed | ✅ Success | Keyword prefix disambiguates, string literal carries the value, ISO format required |
| **PostgreSQL** | `'2024-01-15'::date` or `DATE '2024-01-15'` | Typed | ✅ Success | Multiple input formats accepted but ISO recommended. Separate types for date, time, timestamp, interval. |
| **T-SQL** | `CAST('2024-01-15' AS DATE)` | `date` | ✅ Success | ISO 8601 format is unambiguous in all `DATEFORMAT` settings. Separate date/time/datetime2 types. |
| **TOML** | `1979-05-27` (date), `07:32:00` (time), `1979-05-27T07:32:00Z` (offset datetime) | First-class | ✅ Success | ISO 8601 format, separate semantic types (Local Date, Local Time, Local Date-Time, Offset Date-Time). No ambiguity. |
| **YAML 1.1/1.2** | `2002-12-14` (auto-detected) | `!!timestamp` | ❌ Problematic | Implicit typing: `NO` → `false`, `9.3` → float. Date auto-detection is part of the same "surprise casting" problem (the Norway Problem). |
| **FEEL/DMN** | `date("2024-01-15")`, `time("14:30:00")`, `duration("P3D")` | Typed | ✅ Success | Function-wrapped, explicit, ISO format. The OMG standard chose constructors over bare literals. |
| **Crystal** | No date/time literals | N/A | N/A | Crystal has extensive literal support (regex, proc, command, named tuple) but deliberately omits date/time. Uses `Time.parse` and `Time.utc`. |
| **Ruby** | No date/time literals | N/A | N/A | Ruby is famous for syntactic sugar but has never added date literals. Uses `Date.parse`, `Time.new`. |
| **Elixir** | `~D[2024-01-15]`, `~T[14:30:00]`, `~U[2024-01-15T14:30:00Z]` | Sigiled | ✅ Success | Sigil prefix (`~D`, `~T`, `~U`) removes all ambiguity. Type is explicit in the prefix. ISO format inside brackets. Validated at compile time. |
| **Ballerina** | No temporal literals | `time:Utc` is a tuple `[int, decimal]` | N/A | Modern cloud-native language eschews temporal literals. Uses library API: `time:utcNow()`. |
| **Kotlin** | No date/time literals | N/A | N/A | Despite extension functions for everything (`3.days`), no bare date literals. Uses `LocalDate.parse("2024-01-15")`. |
| **GraphQL** | No date type at all | Custom scalars (`String`) | ❌ Anti-example | Dates are passed as strings with custom scalar validation. No compiler help. |
| **CSS** | Unit literals (`3px`, `2em`, `1s`) | Typed | ✅ Success | Postfix units on numeric values. No date literals, but establishes the "numeric + unit" pattern. |
| **Power Fx** | `Date(2024, 1, 15)`, `Time(14, 30, 0)` | Typed | ✅ Success | Function call with numeric arguments, not string parsing. No ISO literal form. |

### Configuration Formats (Deeper Analysis)

**TOML — The strongest positive precedent.** TOML specifies four first-class date/time types with ISO 8601 format:

```toml
odt1 = 1979-05-27T07:32:00Z          # Offset Date-Time
ldt1 = 1979-05-27T07:32:00            # Local Date-Time  
ld1  = 1979-05-27                     # Local Date
lt1  = 07:32:00                       # Local Time
```

Key observations from the TOML spec:
1. **No ambiguity with arithmetic.** TOML has no expression language, so `1979-05-27` can never be confused with `1979 - 05 - 27`. The token appears only in value position after `=`.
2. **Separate semantic types.** TOML distinguishes Local Date, Local Time, Local Date-Time, and Offset Date-Time as four distinct types. This exactly mirrors Precept's `date`, `time`, `datetime`, `instant` split.
3. **Community reception:** TOML's date/time literals are universally praised. No reports of confusion. The format is cited as one of TOML's best features.
4. **CRITICAL DIFFERENCE:** TOML's dates appear in assignment position only (`key = value`). They are never inside expressions alongside arithmetic operators. This is the fundamental difference from Precept's situation.

**YAML — The strongest negative precedent.** YAML's implicit date detection is part of the "Norway Problem" — the family of bugs where unquoted values are silently reinterpreted. From the [StrictYAML documentation](https://hitchdev.com/strictyaml/why/implicit-typing-removed/): implicit typing represents a major violation of the principle of least astonishment. The [YAML Document from Hell](https://ruudvanasseldonk.com/2023/01/11/the-yaml-document-from-hell) by Ruud van Asseldonk catalogs how `no` becomes `false`, version numbers become floats, and dates become timestamps — all because YAML tries to infer type from syntax shape rather than requiring explicit declaration.

**Key lesson from YAML:** The problem is not that YAML recognizes dates. The problem is that YAML recognizes dates **in an untyped context where the author didn't request date interpretation.** When Precept requires a type declaration (`field X as date`), the context IS the explicit request. There is no "surprise casting."

### Elixir's Sigil Approach — A Middle Ground

Elixir uses sigils — a prefix character that declares what kind of literal follows:

```elixir
~D[2024-01-15]        # Date
~T[14:30:00]          # Time  
~N[2024-01-15 14:30]  # NaiveDateTime
~U[2024-01-15T14:30:00Z]  # UTC DateTime
```

The sigil makes the type explicit at the token level, validated at compile time. There is zero ambiguity in the lexer because `~D[` is a unique prefix. This is conceptually similar to Precept's existing `date("...")` constructor — the type prefix removes all doubt. Elixir's approach is a successful implementation of "explicit but concise."

---

## 3. Deep Dive: Expression Languages with Temporal Values

**Research date:** 2026-07-23
**Author:** Frank (Lead/Architect, Language Designer)
**Motivation:** The original precedent survey (§2) focused on programming languages and configuration formats. Shane flagged a significant gap: *query languages, business rule engines, authorization policy languages, smart contract languages, and configuration languages with expression capabilities*. These are categories where dates are primary concerns AND arithmetic operators exist — the exact combination that matters for Precept's decision. This section fills that gap with documentation-verified findings.

### Query Languages

#### Flux (InfluxDB 2.x) — Bare Date Literals WITH Restricted Arithmetic

Flux is the most important partial counterexample to the "no successful precedent" finding.

**What Flux has:** True lexical-level datetime literals as first-class values. The formal grammar defines: `date_time_lit = date [ "T" time ] .` where `date = year "-" month "-" day`. Examples from the spec:

```flux
2018-01-01T00:00:00Z       // full instant
2018-01-01                  // date-only (defaults to midnight UTC)
```

These are NOT strings, NOT constructors — they are bare tokens recognized at the lexer level, exactly like integer and float literals. Flux also has compound duration literals using postfix units: `1s`, `10d`, `1h15m`, `5w`, `1mo5d`.

**What Flux restricts:** Flux has arithmetic operators (`+`, `-`, `*`, `/`, `%`), but the type system explicitly PREVENTS their use on time values. The Flux spec states:
- "Subtractable types are Integer, Uinteger, and Float" — Time is NOT Subtractable
- "Addable types are Integer, Uinteger, Float, and String" — Time is NOT Addable

Date arithmetic in Flux uses **function calls**: `date.add(d: 1d, to: 2018-01-01T00:00:00Z)`, `date.sub(d: 1w, from: 2021-01-01T00:00:00Z)`.

**Why this matters for Precept:** Flux avoids the `2024-01-15` vs `2024 - 01 - 15` ambiguity by a deliberate type-system constraint: the minus sign in `2024-01-15` is never ambiguous because datetime minus datetime isn't a valid operation. Flux treats this as a feature, not a limitation. But Precept's temporal type proposal explicitly WANTS `date - date = duration` arithmetic via operators. Flux's approach only works if you give up operator-based temporal arithmetic.

**Status:** Flux was deprecated by InfluxData in favor of SQL-based InfluxDB 3, suggesting the language design did not endure as a long-term bet.

Sources: [Flux specification — types](https://docs.influxdata.com/flux/v0/spec/types/), [lexical elements](https://docs.influxdata.com/flux/v0/spec/lexical-elements/), [Flux data types](https://docs.influxdata.com/flux/v0/data-types/).

#### InfluxQL (InfluxDB 1.x) — String-Quoted Time Literals

InfluxQL, the predecessor query language, uses single-quoted strings for time values:

```influxql
WHERE time >= '2015-08-18T00:00:00Z'
WHERE time >= '2015-08-18'
WHERE time > '2015-09-18T21:24:00Z' + 6m
```

Also supports epoch timestamps as bare integers: `WHERE time >= 1439856000000000000`. Duration literals use postfix units: `1h`, `6m`, `12m`, `1000d`.

Notably, InfluxQL does support time + duration arithmetic: `'2015-09-18T21:24:00Z' + 6m`. But THIS IS NOT a general expression language — it operates only in WHERE clauses with limited operator support.

**Key observation:** Even InfluxQL, a query language whose entire purpose is temporal data, chose single-quoted strings rather than bare literals for time values.

Source: [InfluxQL specification](https://docs.influxdata.com/influxdb/v1/query_language/spec/)

#### KQL/Kusto (Microsoft) — Constructor Syntax with Full Arithmetic

KQL (Kusto Query Language) is Microsoft's query language for Azure Data Explorer, Log Analytics, and Application Insights. It is a full expression language whose primary domain is querying time-series data.

**Temporal syntax:** KQL uses explicit `datetime()` constructor syntax:

```kql
datetime(2015-12-31 23:59:59.9)
datetime(2015-12-31)
datetime(null)
```

**Full arithmetic support:** KQL supports all temporal arithmetic via operators:

```kql
datetime(1997-06-25) - datetime(1910-06-11)   // → timespan
datetime(1910-06-11) + 1d                      // → datetime
1.5 * 1h                                       // → 1.5 hours
1d / 5h                                        // → 4.8
```

Duration/timespan values use bare postfix unit literals: `1d`, `2d`, `5h`, `1s`, `1h` — which is exactly Precept's Decision #17 pattern.

**Why KQL matters:** KQL is a query language where dates are THE primary concern (querying logs, metrics, telemetry — all timestamped). The KQL team had every incentive to make date entry as frictionless as possible. They chose `datetime()` constructor syntax specifically to avoid ambiguity, despite postfix duration literals being bare. This is the strongest query-language precedent for Precept's current approach: constructors for date/time values, bare postfix units for durations.

Source: [KQL datetime data type](https://learn.microsoft.com/en-us/kusto/query/scalar-data-types/datetime), [datetime-timespan arithmetic](https://learn.microsoft.com/en-us/kusto/query/datetime-timespan-arithmetic)

#### PromQL (Prometheus) — No Temporal Literals at All

PromQL has no datetime type and no datetime literals. Time is always represented as epoch-based floats (Unix timestamps). The `@` modifier uses epoch integers: `http_requests_total @ 1609746000`.

Duration literals use postfix units: `5m`, `1h30m`, `1w`, `7d`, `54s321ms`. But these are syntactic sugar for floats representing seconds — `1s` equals `1.`, `2m` equals `120.`. They are not typed duration values.

**Key observation:** PromQL avoids the entire temporal literal question by keeping everything as epoch floats. No temporal types = no ambiguity. But this sacrifices type safety and human readability — the exact things Precept's temporal system is designed to provide.

Source: [PromQL operators](https://prometheus.io/docs/prometheus/latest/querying/operators/), [PromQL basics](https://prometheus.io/docs/prometheus/latest/querying/basics/)

### Business Rule Engines

#### FEEL/DMN (OMG Standard) — Constructor Syntax, No Bare Literals, Full Arithmetic

FEEL (Friendly Enough Expression Language) is the expression language of the DMN (Decision Model and Notation) standard published by the OMG. **This is the single most important precedent for Precept.**

FEEL is:
- A business-domain expression language (exactly Precept's intended audience)
- An expression language with arithmetic operators (`+`, `-`, `*`, `/`, `**`)
- A language with first-class temporal types (date, time, date and time, days and time duration, years and months duration)
- A language with full temporal arithmetic via operators

**The FEEL spec explicitly states:** *"Date literals are not supported in FEEL."* Same for time, date-and-time, and duration. All temporal values use constructor functions:

```feel
date("2017-06-23")
time("04:25:12")
date and time("2017-10-22T23:59:00")
duration("P1DT23H12M30S")
duration("P3Y5M")
```

**Temporal arithmetic works via standard operators:**

```feel
date("2012-12-25") - date("2012-12-24") = duration("P1D")               // date - date = duration
date and time("2012-12-24T23:59:00") + duration("PT1M")                  // datetime + duration = datetime
  = date and time("2012-12-25T00:00:00")
time("23:59:00z") + duration("PT2M") = time("00:01:00@Etc/UTC")          // time + duration = time
```

FEEL also has property access on temporal values: `date("2022-12-31").year`, `.month`, `.day`, `.weekday`.

**The `@` prefix notation (DMN 1.3+):** Newer DMN versions introduced a `@`-prefixed shorthand for temporal values in certain contexts: `@"2021-01-01"`. This appears in range expressions and for-loops: `for x in @"2021-01-01"..@"2021-01-03" return x + 1`. Note that this is STILL a prefix-annotated form — the `@` signals "parse the following string as a temporal value." It's closer to SQL's `DATE '...'` pattern than to a bare literal.

**Why FEEL is the decisive precedent:** FEEL was designed by a standards committee (OMG) specifically for business analysts and domain experts — exactly Precept's target audience. The committee considered and rejected bare temporal literals in favor of constructor functions. And FEEL supports full temporal arithmetic via standard operators. If the OMG — with decades of business modeling experience — concluded that constructors are the right form for temporal values in a business expression language with arithmetic, that is extremely strong evidence for Precept to follow the same path.

Source: [DMN FEEL Handbook](https://kiegroup.github.io/dmn-feel-handbook/) (Drools implementation of the OMG DMN standard)

### Authorization & Policy Languages

#### Cedar (AWS) — Constructor Syntax with Method-Based Arithmetic

Cedar is AWS's authorization policy language, designed for fine-grained access control. It has an expression language with comparison operators and temporal types.

**Temporal syntax:** Cedar uses `datetime()` and `duration()` constructors with string arguments:

```cedar
datetime("2024-10-15")
datetime("2024-10-15T01:00:00Z")
duration("2h30m")
```

**Comparison operators work on datetime:** `<`, `<=`, `>`, `>=`, `==`, `!=` — all supported.

**Arithmetic operators are restricted to `long` only:** Cedar deliberately prevents `+`, `-`, `*` on temporal types. Date arithmetic uses METHOD-BASED syntax:

```cedar
datetime("2024-10-15").offset(duration("1h"))
datetime("2024-10-15T01:00:00Z").durationSince(datetime("2024-10-15"))
```

**The compile-time validated constructor pattern:** Cedar's policy validator REQUIRES constructor arguments to be string literals (not variables). This enables compile-time validation: the policy analysis engine can verify at validation time — before any request is evaluated — that `datetime("2024-10-15")` is a valid datetime. This is exactly the "compile-time validated constructor" pattern that is natural in Precept: `date(2024-01-15)` (typed literal, no string quotes — Decision #18) is validated during parsing/type-checking, not at runtime.

**Why Cedar matters:** Cedar independently arrived at the same design: constructors for temporal values, restricted arithmetic. The method-based arithmetic (`.offset()`, `.durationSince()`) is more verbose than Precept's operator-based approach, but the constructor pattern for value construction is identical.

Source: [Cedar syntax and datatypes](https://docs.cedarpolicy.com/policies/syntax-datatypes.html), [Cedar operators](https://docs.cedarpolicy.com/policies/syntax-operators.html)

### Smart Contract Languages

#### Solidity (Ethereum) — Postfix Time Units, No Date Type

Solidity has postfix time unit suffixes that apply to numeric literals:

```solidity
1 seconds
24 hours
7 days
1 weeks
```

These are purely numeric multipliers — `1 days` equals `86400` (seconds in a day). Time is epoch-based: `block.timestamp` returns a `uint` (seconds since Unix epoch). There is no datetime type at all.

**Notable:** The `years` suffix was REMOVED in Solidity v0.5.0 because "not every year equals 365 days." The `now` alias for `block.timestamp` was also removed. These are cautionary examples of temporal convenience features that created more confusion than they resolved.

**Why Solidity matters:** Solidity's postfix unit pattern (`30 days`, `24 hours`) is EXACTLY what Precept's Decision #17 approved. Solidity confirms this pattern works well for duration/unit literals in a language with arithmetic. But Solidity has NO date literals — time is always Unix timestamps as integers. The postfix unit pattern is sufficient for "duration meets arithmetic."

Source: [Solidity units and global variables](https://docs.soliditylang.org/en/latest/units-and-global-variables.html)

### Configuration Languages with Expressions

#### CUE — No Temporal Types at All

CUE's type hierarchy (null, bool, int, float, string, bytes, structs, lists) has NO temporal types whatsoever. CUE has arithmetic operators and SI multiplier literals (`1.5G`, `1.3Ki`) — an interesting postfix unit pattern for bytes/numbers — but nothing temporal. This confirms that even modern configuration languages designed for structured data don't automatically include temporal types.

Source: [CUE language specification](https://cuelang.org/docs/reference/spec/), [CUE types](https://cuelang.org/docs/tour/types/)

#### Pkl (Apple) — Durations but No Datetime

Pkl has `Duration` as a first-class type with postfix notation using dot-property syntax:

```pkl
5.min    // 5 minutes
3.d      // 3 days
5.h      // 5 hours
5.s      // 5 seconds
```

Pkl also has `DataSize` with the same pattern: `5.mb`, `20.gb`. Duration arithmetic works naturally: `5.min + 3.s`, `5.min * 3`, `5.min / 3.min`. But Pkl has **NO datetime/date/time types at all** in its type system. Temporal values beyond durations are handled by string manipulation or external libraries.

**Key observation:** Pkl validates the "postfix unit literals for durations" pattern (similar to Precept's `30 days`) while confirming that even well-designed modern config languages with rich expression systems don't add date/time types or literals.

Source: [Pkl language reference](https://pkl-lang.org/main/current/language-reference/index.html)

### The Compile-Time Validated Constructor Pattern

Several of the languages surveyed demonstrate a design pattern worth naming explicitly: **the compile-time validated constructor**. This is a constructor that takes a string literal argument, where the validator can verify the string's format and value at compile time (or policy-validation time, or type-check time) rather than deferring to runtime.

| Language | Constructor | Validation Time | What's Validated |
|---|---|---|---|
| **Cedar** | `datetime("2024-10-15")` | Policy validation (before any request) | String is valid datetime, argument must be a literal |
| **KQL** | `datetime(2015-12-31)` | Query parse time | Datetime components are valid |
| **FEEL/DMN** | `date("2017-06-23")` | Expression evaluation | String matches XML Schema date format |
| **Precept** | `date(2024-01-15)` | Type-check phase (compile time) | Content is valid ISO 8601, matches declared type. **Updated by Decision #18:** typed literal form, no string quotes. |

This pattern gives you the UX of "paste an ISO date and it works" with the safety of "the compiler catches `date("2024-13-45")` before anything runs." It's functionally equivalent to a native literal for compile-time constant expressions — the parser KNOWS the value at parse time. The only difference is syntax: `date("2024-01-15")` instead of `2024-01-15`.

### Updated Precedent Table (Expression Languages with Temporal Values)

| System | Category | Date Syntax | Has Arithmetic Operators? | Temporal Arithmetic? | Bare Date Literals? |
|---|---|---|---|---|---|
| **FEEL/DMN** | Business rule engine | `date("...")` constructor | ✅ `+`, `-`, `*`, `/`, `**` | ✅ Operator-based | ❌ Explicitly rejected by spec |
| **KQL/Kusto** | Query language | `datetime(...)` constructor | ✅ Full arithmetic | ✅ Operator-based | ❌ Constructor chosen to avoid ambiguity |
| **Flux** | Query language | `2024-01-15` bare literal | ✅ `+`, `-`, `*`, `/`, `%` | ❌ Function-call only | ✅ BUT arithmetic on time types is prohibited |
| **InfluxQL** | Query language | `'2024-01-15'` string | ✅ Limited to WHERE | ✅ Time + duration | ❌ Single-quoted string |
| **PromQL** | Query language | None (epoch float) | ✅ `+`, `-`, `*`, `/`, `%`, `^` | N/A (no temporal type) | ❌ No temporal types at all |
| **Cedar** | Policy language | `datetime("...")` constructor | ✅ On `long` only | ❌ Method-based only | ❌ Constructor required |
| **Solidity** | Smart contracts | None (epoch uint) | ✅ Full arithmetic | N/A (no temporal type) | ❌ Postfix units for durations only |
| **CUE** | Config language | None | ✅ `+`, `-`, `*`, `/` | N/A (no temporal types) | ❌ No temporal types at all |
| **Pkl** | Config language | None | ✅ `+`, `-`, `*`, `/`, `**`, `%` | Durations only (`5.min + 3.s`) | ❌ No date/time types at all |

### Revised Assessment of "No Successful Precedent"

The original finding stated: *"No successful precedent exists for bare date literals inside expression languages with arithmetic operators."*

After deep-diving into query languages, business rule engines, authorization languages, smart contracts, and config languages, the finding is **confirmed with nuance:**

**Flux is the only partial counterexample** — it has bare datetime literals in an expression language with arithmetic operators. But Flux deliberately prevents arithmetic operators on time types (`+` and `-` work on numbers and strings, not on Time). The bare literal works precisely BECAUSE the ambiguity is defused at the type level: `2024-01-15` can't be confused with `2024 - 01 - 15` because you can't subtract time values anyway.

**Every language that supports BOTH temporal values AND operator-based arithmetic on those values uses constructors, not bare literals.** This includes:
- FEEL/DMN (the OMG standard for business expressions) — `date("...")`
- KQL (Microsoft's primary query language for time-series data) — `datetime(...)`
- Cedar (AWS's policy language) — `datetime("...")`

**The "compile-time validated constructor" pattern is the universal choice** when dates must coexist with arithmetic operators. The constructor form was not an accident or a concession — it was a deliberate design decision in each of these languages, made by teams whose primary domain involves temporal data.

**What this means for Precept:** The current `date("2024-01-15")` constructor design is not a placeholder awaiting a better syntax. It IS the proven pattern for this exact combination of requirements: typed temporal values + arithmetic operators + business-domain audience. The postfix unit literals for durations (`30 days`) handle the ergonomic case that most deserves native syntax.

---

## 4. Interaction with Postfix Unit Literals

### The Three-Form Question

If native ISO literals were added, Precept would have three syntax forms for temporal values:

```precept
# Form 1: String constructor (already in the proposal)
invariant DueDate >= date("2024-01-15")
field CutoffTime as time default time("17:00")

# Form 2: Native ISO literal (proposed)
invariant DueDate >= 2024-01-15
field CutoffTime as time default 17:00

# Form 3: Postfix unit literal (Decision #17)
on Extend -> set DueDate = DueDate + 30 days
```

### Precedent Analysis: Multiple Literal Forms

| Language | Forms | Domain | Coexistence? |
|---|---|---|---|
| **CSS** | `3px` (literal) + `calc(3px + 2em)` (function) + `var(--width)` (variable) | Dimensions | ✅ Successful — each form serves a distinct need |
| **SQL** | `DATE '...'` (literal) + `CAST('...' AS DATE)` (function) + `INTERVAL '3' DAY` (duration) | Temporal | ✅ Successful — SQL has had this for decades |
| **Python** | `3` (int literal) + `int("3")` (constructor) + `3 + 4j` (complex literal) | Numeric | ✅ Successful — different entry points for the same domain |
| **F#** | `3.0<m>` (unit of measure) + `System.Decimal(3.0)` (constructor) | Numeric | ✅ Successful — literal is preferred, constructor exists for dynamic values |

**Assessment:** Having multiple literal forms for the same domain is common and well-precedented. The key is that each form should serve a **distinct purpose** and be preferred in different contexts:
- Constructors → dynamic values, runtime input, variables
- Native literals → compile-time constants, hardcoded defaults
- Postfix units → duration/period arithmetic

The concern would arise only if the forms overlap so completely that no guidance exists for when to use which.

### Distinct vs. Redundant

In the three-form model:
- `date("2024-01-15")` and `2024-01-15` would be **fully redundant** in expression position — both create the same `date` value.
- `30 days` and `days(30)` are **already accepted** as contextually equivalent (Decision #17).
- `date("2024-01-15")` and `30 days` are **not redundant** — different types, different uses.

The "two ways to do the same thing" concern applies specifically to constructor vs. native literal. SQL resolved this by making the native form canonical and the cast form explicit — `DATE '2024-01-15'` is the standard, `CAST('...' AS DATE)` exists for dynamic strings. TOML resolved it by having ONLY the native form (no constructor).

---

## 5. Lexer/Parser Risks

### The Core Ambiguity: `2024-01-15` vs. `2024 - 01 - 15`

This is the single most significant technical concern. In an expression language with subtraction:

```precept
# Is this a date literal or arithmetic?
invariant DueDate >= 2024-01-15

# These are clearly arithmetic
invariant Count >= 2024 - 01 - 15
set Total = Year - Month - Day
```

### How Other Systems Handle This

**TOML:** Avoids the problem entirely. TOML has no expression language — values appear only after `=` in key-value position. `1979-05-27` is always a date because there are no operators to create ambiguity.

**SQL:** Uses keyword prefix: `DATE '2024-01-15'`. The keyword `DATE` signals the parser that what follows is a date literal, not arithmetic. The string quotes prevent tokenizer ambiguity. This is the most robust solution but the most verbose.

**YAML:** Uses contextual heuristics. A bare `2002-12-14` in value position is interpreted as a timestamp. But this automatic detection is precisely what causes YAML's "Norway Problem" family of bugs — any value that LOOKS like a date IS treated as a date, whether the author intended it or not.

**ISO 8601 in general:** The format `YYYY-MM-DD` is designed for unambiguous human reading, not for unambiguous machine parsing inside expression languages. The hyphens ARE minus signs in most programming languages.

### Lexer Strategies

**Strategy 1 — Longest match (greedy tokenization).** The lexer tries to match `\d{4}-\d{2}-\d{2}` before trying to match an integer followed by minus. If the pattern matches AND the value is a valid date (month 01-12, day 01-31), emit a `DateLiteral` token. Otherwise, fall back to `IntegerLiteral` + `Minus` + `IntegerLiteral`.

- **Pros:** Minimal syntax overhead. `2024-01-15` just works.
- **Cons:** `2024-01-45` (invalid date) silently becomes `2024 - 01 - 45` (arithmetic). Error recovery is non-obvious. Whitespace becomes significant: `2024-01-15` vs `2024 - 01 - 15`.

**Strategy 2 — Context-sensitive tokenization.** The tokenizer switches behavior based on parser state. After `>=`, `<=`, `==`, `default`, etc., try date pattern first. After an identifier or closing paren, try subtraction first.

- **Pros:** Can handle most cases correctly.
- **Cons:** Couples the tokenizer to parser state. Superpower's `TokenizerBuilder` is designed for context-free tokenization. This would require custom tokenizer logic that fights the framework.

**Strategy 3 — Distinct delimiter (Elixir's approach adapted).** Use a prefix or delimiter: `@2024-01-15` or `d"2024-01-15"` or `#2024-01-15`.

- **Pros:** Zero ambiguity. Tokenizer is trivial.
- **Cons:** We're back to a decorated form, losing the "bare ISO" benefit.

### Superpower-Specific Considerations

Superpower's `TokenizerBuilder<T>` uses ordered recognizer rules with longest-match semantics. The recognizer list is tried in order, and the first match wins. This means:

1. A date recognizer (`\d{4}-\d{2}-\d{2}`) placed BEFORE the integer recognizer would greedily consume date-shaped tokens.
2. But it would also consume `2024-01-45` (invalid date components) at the token level — validation would need to happen later.
3. The subtraction case `Year - Month - Day` works naturally because identifiers don't start with digits. The ambiguity only exists between `<integer>-<integer>-<integer>` and `<date>`.

**The real edge case:** `set X = 2024 - 01 - 15`. With greedy tokenization:
- `2024-01-15` (no spaces) → Date literal ✅
- `2024 - 01 - 15` (spaces around operators) → Subtraction ✅ (spaces break the date pattern)
- `2024-01 - 15` (mixed) → `2024-01` fails date pattern (only 2 groups), so → `2024` `-` `01` `-` `15` ✅

**Whitespace sensitivity risk:** Making `2024-01-15` mean date (no spaces) and `2024 - 01 - 15` mean subtraction (spaces) works technically but violates common formatting expectations. Many auto-formatters add spaces around operators.

### Time Literal Ambiguity

`14:30` is even more problematic than dates:
- `:` is not currently an operator in Precept, but it could be in the future
- `14:30` alone looks like a ratio or label in many languages
- YAML's sexagesimal problem: `22:22` was parsed as 1342 (base-60 number!) in YAML 1.1

Time literals would likely need the `time("14:30")` constructor or a prefix to remain unambiguous.

### Instant Literal Ambiguity

`2024-01-15T14:30:00Z` contains `T` and `Z` characters. The `T` could be parsed as a type reference or identifier; the `Z` as an identifier suffix. This is extremely problematic in an expression language. Instant literals almost certainly need to remain string-wrapped.

---

## 6. Risks We Haven't Considered (and Some We Have)

### Risk 1: "Two Ways to Do the Same Thing"

If `date("2024-01-15")` and `2024-01-15` both produce an identical `date` value, the language has two syntaxes for one operation. Precept's design philosophy is explicitness. Having two paths to the same value creates a style question the language doesn't answer:

- Which form do you use in an invariant? `invariant X >= date("2024-01-15")` or `invariant X >= 2024-01-15`?
- Linters/formatters would need a preference.
- AI consumers generating precepts need a canonical form.
- Documentation examples must pick one and be consistent.

**Mitigation:** If native literals exist, make the constructor form a secondary alternative (like SQL's `CAST` vs `DATE '...'`). The native form becomes canonical; the constructor exists for computed/dynamic values.

### Risk 2: Error Recovery

If `2024-01-45` fails date validation:
- **Greedy tokenizer:** Already consumed the token. Must report "invalid date literal" rather than trying arithmetic fallback.
- **Fallback tokenizer:** Tries arithmetic: `2024 - 01 - 45` → produces value `1978`. This is a silent misinterpretation — the author MEANT a date, got arithmetic.
- **Assessment:** Greedy with hard error is the safer path. But it means a typo in a date literal is a hard stop, not a graceful degradation.

### Risk 3: Copy-Paste from External Systems

ISO 8601 dates are everywhere: JSON APIs, database outputs, log files, spreadsheets. Native ISO literals would make copy-paste seamless: paste `2024-01-15` directly into a precept expression.

**This is both a benefit and a risk.** The benefit is obvious. The risk: pasting a timestamp like `2024-01-15T14:30:00+05:00` into a `date` context. With string constructors, this fails clearly (`date("2024-01-15T14:30:00+05:00")` is not a valid date string). With native literals, the parser would consume `2024-01-15`, then choke on `T14:30:00+05:00`. The error message may be confusing.

### Risk 4: Tooling Complexity

**Syntax highlighting:** Bare `2024-01-15` is harder to highlight than `date("2024-01-15")`. The TextMate grammar needs a regex to match date-shaped numeric sequences, and the regex must not match partial arithmetic expressions. TOML's TextMate grammar handles this, so it's solvable — but it's additional complexity.

**Completions:** `date("` triggers string completion with ISO format hints. A bare date literal has no trigger character — the language server must recognize that four digits followed by a hyphen MIGHT be a date being typed, and offer date-specific completions.

**Hover:** Both forms can show the same hover information ("date: January 15, 2024, Monday"). No difference.

**Go-to-definition:** No difference — literals don't have definitions.

### Risk 5: Future Grammar Constraints

If native date literals use the `\d{4}-\d{2}-\d{2}` pattern, that pattern is permanently reserved. Future features that might use digit-hyphen-digit sequences (version numbers, IP-like patterns, custom identifiers) would conflict. The constructor form keeps the grammar extension surface clean.

### Risk 6: Date-Only Is the Easy Case

The full temporal type family has varying literal-viability:

| Type | Native Literal | Ambiguity Level |
|---|---|---|
| `date` | `2024-01-15` | Medium (subtraction ambiguity, solvable with whitespace rules) |
| `time` | `14:30` | High (colon is unusual, sexagesimal precedent, no clear prefix) |
| `instant` | `2024-01-15T14:30:00Z` | Very High (`T` and `Z` as identifiers, timezone offsets as arithmetic) |
| `datetime` | `2024-01-15T14:30:00` | Very High (same as instant) |
| `duration` | `PT3H30M` | Very High (ISO 8601 duration notation is alien to DSL authors) |
| `period` | `P3M15D` | Very High (same as duration) |

If native literals only work for `date`, the benefit is narrow. The other five types MUST use constructors regardless. This creates asymmetry: dates look one way, everything else looks another way.

---

## 7. Precedent Table (Consolidated)

| System | Syntax Form | Type Safety | Lexer Ambiguity | Community Reception | Notes |
|---|---|---|---|---|---|
| **TOML** | `1979-05-27` (bare) | ✅ Separate types | None (no expressions) | ✅ Universally praised | No expression language — not comparable |
| **SQL** | `DATE '2024-01-15'` | ✅ Keyword-typed | None (keyword prefix) | ✅ Industry standard | Keyword + string — explicit, not bare |
| **Elixir** | `~D[2024-01-15]` | ✅ Sigil-typed | None (sigil prefix) | ✅ Beloved | Sigil = explicit type prefix |
| **VB.NET** | `#1/15/2024#` | ❌ Conflated type | Low (delimiter) | ❌ Criticized | Locale-dependent, type conflation |
| **YAML** | `2024-01-15` (auto) | ❌ Implicit typing | High (implicit) | ❌ Norway Problem | Silent surprise casting |
| **FEEL/DMN** | `date("2024-01-15")` | ✅ Function-typed | None (function call) | ✅ Standard-compliant | Closest analog to Precept's current design |
| **Power Fx** | `Date(2024, 1, 15)` | ✅ Function-typed | None (function call) | ✅ Accessible | Numeric args, not ISO string |
| **GraphQL** | `"2024-01-15"` (string scalar) | ❌ No type safety | None (it's a string) | ❌ Known weakness | Anti-example — no temporal types at all |
| **Crystal, Ruby, Kotlin, Ballerina** | None (constructors only) | ✅ (via constructors) | None | N/A | Modern typed languages don't add date literals |

**Pattern:** Every SUCCESSFUL native date literal exists in either (a) a context without expressions (TOML), (b) a context with explicit type prefixes (SQL, Elixir), or (c) both. No successful precedent exists for bare ISO dates inside an expression language with arithmetic operators.

---

## 8. VB-Era Objection Assessment

| VB-Era Objection | Does Precept's design address it? | Confidence |
|---|---|---|
| Locale-dependent format creates interpretation variance | **Yes.** ISO 8601 only. `2024-01-15` is always January 15. | High |
| Single type conflates date, time, and datetime | **Yes.** Separate `date`, `time`, `instant`, `datetime` types backed by NodaTime. | High |
| Non-standard delimiter (`#`) creates learning overhead | **Yes (by omission).** No delimiter. ISO format is the universal standard. | High |
| Weak underlying type (`System.DateTime`) enables semantic bugs | **Yes.** NodaTime is the gold standard for temporal correctness. | High |
| Literals in expressions create lexer ambiguity | **Partially.** Not a VB problem (VB uses `#` delimiters), but IS a problem for bare ISO literals in Precept's expression language. This is a NEW risk, not a VB-inherited one. | Medium |

**Summary:** The VB-era objections are fully addressed by Precept's design. But the lexer ambiguity concern is a NEW problem specific to bare ISO dates in expression languages — one that VB itself avoided by using `#` delimiters.

---

## 9. Options for Shane

### Option A: No Native Literals — Stay with Constructors Only

```precept
invariant DueDate >= date("2024-01-15") because "contract start"
field CutoffTime as time default time("17:00")
on Extend -> set DueDate = DueDate + 30 days
```

**Pros:**
- Zero lexer ambiguity. No parser changes needed for the temporal feature.
- One canonical form per type. No "which syntax do I use?" question.
- Consistent with FEEL/DMN, Power Fx, Crystal, Ruby, Kotlin, Ballerina — the modern consensus.
- The constructor form already reads well. `date("2024-01-15")` is clear English.
- All six temporal types use the same syntax pattern, no asymmetry.
- Postfix unit literals (`30 days`) handle the ergonomic case that matters most (duration arithmetic).

**Cons:**
- More verbose than TOML-style bare dates for hardcoded constants.
- Copy-paste from external systems requires wrapping in `date("...")`.

**Frank's read:** This is the safe choice with strong precedent in comparable DSLs. The postfix unit literals already deliver the "native feel" for the most common temporal expression pattern (arithmetic). Constructors handle the rest cleanly. No one will criticize a temporal type system for using the same factory-function pattern as FEEL and SQL's `CAST`.

### Option B: Native Date Literals Only — Bare ISO for `date`, Constructors for Everything Else

```precept
invariant DueDate >= 2024-01-15 because "contract start"
field CutoffTime as time default time("17:00")    # time stays as constructor
on Extend -> set DueDate = DueDate + 30 days
```

**Pros:**
- Date literals are the most common temporal constant. Optimizes the 80% case.
- `2024-01-15` is genuinely beautiful — zero noise, pure data.
- TOML proves the format is learnable and well-received.

**Cons:**
- Asymmetry: `date` has native literals, `time`/`instant`/`datetime` don't. Authors will ask "why can I write `2024-01-15` but not `14:30`?"
- Lexer ambiguity with subtraction. Solvable (whitespace rules, greedy tokenization) but adds complexity.
- `2024-01-45` (invalid date) requires good error reporting — it LOOKS like arithmetic.
- TextMate grammar, completions, and error recovery all need date-aware logic.
- Only `date` benefits. Five of six temporal types still use constructors.

**Frank's read:** This is the tempting choice. The bare `2024-01-15` really does look great. But the asymmetry and lexer complexity are real costs. The benefit is narrow (one type out of six, appearance only in constant expressions). And the parser complexity is permanent — once you commit to `\d{4}-\d{2}-\d{2}` as a token pattern, you can never use that shape for anything else.

### Option C: Keyword-Prefixed Literals — SQL-Style Type Prefixed

```precept
invariant DueDate >= date 2024-01-15 because "contract start"
field CutoffTime as time default time 17:00
on Extend -> set DueDate = DueDate + 30 days
```

**Pros:**
- Zero lexer ambiguity. The keyword prefix signals "temporal literal follows."
- Works for ALL temporal types: `date 2024-01-15`, `time 17:00`, `instant 2024-01-15T14:30:00Z`.
- Removes the string quotes — lighter than `date("2024-01-15")` but still explicit.
- Type is visible at the token level (keyword + bare value), not hidden in a string.
- Follows SQL precedent — the most battle-tested temporal literal syntax in computing history.

**Cons:**
- `date 2024-01-15` looks like `date` is a function call without parens — could confuse readers.
- Tokenizer must recognize that `date` followed by a date-pattern is a compound literal, not `date` as an identifier followed by subtraction.
- New compound-token form in the grammar (keyword + pattern). Not hard, but not trivial.
- No string wrapping means the parser must still handle the ISO pattern matching.

**Frank's read:** This is architecturally sound and has the strongest precedent (SQL). The `date 2024-01-15` form is lighter than `date("2024-01-15")` while preserving explicit type annotation. But it adds syntax complexity (a new compound literal form) for what is essentially a cosmetic improvement — removing two characters (`"` + `"`) from the constructor. The gain-to-cost ratio may not justify it for Precept's scope.

### Option D: Defer — Research Now, Decide After Phase 1 Ships

Ship the temporal type system with constructors only. After domain authors use it for a real cycle, gather data:
- Do authors complain about `date("2024-01-15")` verbosity?
- Do samples and real precepts have enough hardcoded date constants to justify a second syntax?
- Has the parser changed in ways that affect the lexer ambiguity analysis?

**Pros:**
- No irreversible syntax commitment during the research phase.
- Postfix unit literals already ship the highest-value ergonomic improvement.
- Constructors work. Adding native literals later is additive, not breaking.
- More information = better decision.

**Cons:**
- If native literals ARE added later, early adopters have precepts in constructor form only. Not breaking, but creates two eras of style.
- Delays a potentially loved feature.

**Frank's read:** This is pragmatic. The temporal type system is already the most complex language change in Precept's history. Adding native literal syntax ON TOP of eight new types, seven constructor functions, postfix unit literals, context-dependent type resolution, and bidirectional type inference is scope creep. Ship constructors, measure real demand, add literals if the data supports it.

---

## 10. Summary Finding

The external evidence is clear on two points:

1. **The VB precedent does not apply.** VB's problems were locale ambiguity + type conflation + weak backing. Precept has none of these. Anyone who objects to Precept date literals by citing VB is citing the wrong bug.

2. **No successful precedent exists for bare date literals inside expression languages with arithmetic.** Every success story (TOML, SQL, Elixir) either has no expression language, or uses explicit type prefixes. Precept's expression language creates a genuine lexer ambiguity that VB avoided with `#` and SQL avoids with the `DATE` keyword.

The decision isn't "are native date literals good?" (they are, in the right context). The decision is "does the ergonomic benefit of removing `date("...")` wrappers justify the parser complexity and style-guide burden in a language that already has postfix unit literals for the highest-value temporal expressions?"

That's Shane's call.

---

## 11. Locked Decision #18 — Typed Literals Without Strings

**Decision date:** 2026-04-14
**Decision by:** Shane (owner)
**Status:** Locked

Shane chose a hybrid that this research did not present as a distinct option: **Option A (constructors only) with no-quotes typed-literal form**. This combines the constructor's disambiguation benefit (type keyword as prefix eliminates all lexer ambiguity) with the bare literal's honesty (no string quotes suggesting "a string being parsed").

**Before (string constructors — the form analyzed in this research):**
```precept
date("2024-01-15")
time("14:30")
instant("2024-01-15T14:30:00Z")
duration("PT72H")
period("P1Y6M")
timezone("America/New_York")
```

**After (typed literals — LOCKED):**
```precept
date(2024-01-15)
time(14:30)
instant(2024-01-15T14:30:00Z)
duration(PT72H)
period(P1Y6M)
timezone(America/New_York)
```

### How this resolves the options

This is structurally **Option A** (constructors only — one canonical form per type, type keyword as prefix) but with the string quotes removed. The result is:

- **Zero lexer ambiguity** — the type keyword prefix (`date(`, `time(`, `instant(`, etc.) disambiguates completely. The [§5 lexer risks](#5-lexerparser-risks) do not apply — `date(2024-01-15)` is never confused with subtraction because `date(` is a distinct token prefix.
- **All 7 temporal types + timezone use the same pattern** — no asymmetry (the concern raised in [§6 Risk 6](#risk-6-date-only-is-the-easy-case)).
- **Honest about what it is** — `date(2024-01-15)` communicates "this IS a date" rather than "this is a string being parsed into a date." The parens contain a pattern, not a string expression.
- **One canonical form** — no "two ways to do the same thing" (the concern raised in [§6 Risk 1](#risk-1-two-ways-to-do-the-same-thing)).
- **KQL precedent** — Microsoft's KQL uses `datetime(2015-12-31)` — no quotes, bare ISO inside typed parens.

### New parser concept: TypedLiteralAtom

This introduces a new parser concept distinct from function calls. After `date(`, the parser does NOT enter expression mode — it matches an ISO pattern and consumes until `)`. Content inside typed-literal parens is a raw pattern (no operator precedence, no identifier resolution). This is a separate combinator from `FunctionCallAtom`.

`days(30)`, `hours(3)`, `months(6)`, etc. REMAIN as function calls — they take integer expressions, not patterns. `days(GraceDays)` must work with variables.

### Impact on this research's findings

The core finding — "no successful precedent for bare ISO dates inside expression languages with arithmetic" — remains valid and unchanged. This decision does NOT add bare literals. It keeps the constructor prefix while removing the quotes, making the form lighter without introducing any of the [§5 lexer risks](#5-lexerparser-risks). The research correctly identified that the type-prefix pattern is the universal proven solution; this decision keeps the prefix and improves what's inside it.
