# Type System Survey

**Research date:** 2026-05-18 (expanded from 2026-05-14)
**Author:** George (Runtime Dev)
**Relevance:** Formal grounding and cross-system precedent for Precept's type-system expansion: `choice` (#25), `date` (#26), `decimal` (#27), `integer` (#29), and any future type-system proposals in this domain.

---

## Formal Grounding

### What kind of type system is Precept extending?

Precept's current type system is a **many-sorted first-order type system** with three base sorts: `string`, `number`, and `boolean`. Fields are typed at declaration time. Expressions are statically typed. There is no subtyping, no generics, no higher-kinded types, and no type inference beyond collection element types. The type system operates identically in stateful and stateless precepts — all coercion, constraint, and comparison rules apply regardless of whether states are declared.

The expansion adds four types. In formal terms these are:

- **`choice(v1, v2, ...) [ordered]`** — a *nominal sum type with string-valued constructors and optional total order*. In type theory, an enum is a finite sum type whose variants carry no data. Precept's `choice(...)` is string-backed rather than integer-backed (unlike C# `enum`), which makes the values self-describing and JSON-serializable without an additional mapping layer.
- **`date`** — a *scalar temporal type with day granularity*. From the type system perspective it is an opaque scalar type with an external representation (`YYYY-MM-DD`), a partial algebra (`date ± integer → date`, `date - date → integer`), and a set of read-only component accessors.
- **`decimal`** — a *scalar exact-numeric type*. It is a subtype of the numeric tower in terms of value coverage but is not in a subtype relationship with `number` for assignment purposes, because mixing exact and approximate arithmetic is semantically harmful. The type system enforces this explicitly.
- **`integer`** — a *scalar whole-number type*. It participates in implicit widening to both `decimal` and `number`, but neither `decimal` nor `number` narrow to `integer` without an explicit function call.

### Type vs constraint

Precept distinguishes type from constraint. The type defines what kind of value the field can hold. The constraint restricts which values of that type are valid. A field typed as `decimal` with `maxplaces 2` is typed as `decimal`; the `maxplaces 2` is a post-type constraint that narrows the allowed value range. A field typed as `choice("Low","High")` is typed as `choice`; the value set is part of the type definition, not a separate constraint. This distinction matters for the type checker: type errors are caught before constraint errors, and they produce different diagnostic categories.

### Coercion hierarchy

The numeric tower has a defined widening direction:

```
integer --> decimal
integer --> number
```

There is no widening between `decimal` and `number`. Mixing them is always a type error. The coercion rules follow C#/.NET semantics where they exist:
- `int` widens to `decimal` in C# — so `integer` widens to `decimal` in Precept
- `int` widens to `double` in C# — so `integer` widens to `number` in Precept
- `decimal` does NOT widen to `double` in C# implicitly — so `decimal` does NOT widen to `number` in Precept

This is not an original design decision. It inherits from the platform.

### Sum-type theory and `choice`

A sum type (or union type, or tagged union, or discriminated union) is a type whose values are drawn from a disjoint union of possible forms. In the simplest case — one constructor per variant with no payload — a sum type is an enum. Precept's `choice(...)` is exactly this: a closed set of string-valued constructors with no data payload.

The critical property is *closedness*: the type system can exhaustively check against the declared set. A `choice` without `ordered` supports equality comparisons only. A `choice` with `ordered` gains a total order defined by declaration position, enabling relational comparisons. Declaration-position ordering is more predictable than lexicographic ordering (which depends on value strings) and more honest than backing-integer ordering (which leaks implementation details).

Two `choice(...)` declarations with identical value sets are structurally equal but nominally distinct. Precept uses nominal typing for `choice`: the field identity, not the value set, defines the type. This prevents `field DocumentType as choice("A","B")` from being silently assignment-compatible with `field StatusCode as choice("A","B")`.

---

## Database Systems

### PostgreSQL

**Enum type:** `CREATE TYPE status_code AS ENUM ('draft', 'active', 'closed')`. Values are stored as integers internally but exposed as strings. Ordered by declaration position. Supports `<`, `>`, `<=`, `>=` comparison by declaration order without an explicit opt-in. Adding values later is possible via `ALTER TYPE ... ADD VALUE` but removing values is not. Casting between enum types requires explicit CAST. [Docs](https://www.postgresql.org/docs/current/datatype-enum.html)

*Precept divergence:* Precept's `ordered` is opt-in; PostgreSQL enums always support ordering. Precept's inline-declared `choice(...)` cannot be extended after definition — the closed-universe model is stronger.

**Date type:** `DATE` stores a calendar date (year, month, day) without time zone. `TIME` adds time of day. `TIMESTAMP` combines both without timezone. `TIMESTAMPTZ` adds timezone offset. These are four distinct types, not a single polymorphic type. Date arithmetic: `date + integer` (days) returns `date`; `date - date` returns `interval` (day count component). [Docs](https://www.postgresql.org/docs/current/datatype-datetime.html)

*Precept alignment:* The `DATE` type maps directly to Precept's `date`. The `TIMESTAMP` / `TIMESTAMPTZ` distinction validates why Precept defers time-of-day: timezone handling is a separate type design problem, not an extension of day-granularity dates.

**Decimal type:** `DECIMAL(p,s)` and `NUMERIC(p,s)` are synonyms — exact numeric with user-specified precision `p` (total digits) and scale `s` (digits after decimal point). `MONEY` type exists but stores currency as a locale-dependent string; the PostgreSQL documentation itself recommends against `MONEY` for financial calculations due to rounding under locale changes. [Docs](https://www.postgresql.org/docs/current/datatype-numeric.html) and [money docs](https://www.postgresql.org/docs/current/datatype-money.html)

*Precept alignment:* The `MONEY` lesson is directly applicable. Precept's `decimal` does not parameterize precision and scale at the type level (unlike `DECIMAL(p,s)`) — it uses `maxplaces N` as a constraint instead. This is a deliberate simplification: `System.Decimal` covers 28-29 digits, making p/s parameterization unnecessary.

**Integer type:** `SMALLINT` (16-bit), `INTEGER` (32-bit), `BIGINT` (64-bit). Distinct from floating-point types. Integer division truncates toward zero. [Docs](https://www.postgresql.org/docs/current/datatype-numeric.html)

---

### SQL Server

**Constrained strings:** No native `ENUM` type. Constrained value sets are implemented via `CHECK` constraints (`CHECK column IN ('A','B','C')`) or lookup tables with foreign keys. This is verbose and does not surface in IntelliSense or static analysis. The absence of a first-class enum type is considered a weakness by the T-SQL community.

*Precept improvement:* `choice(...)` is the typed, compile-time-checked version of what SQL Server authors do with CHECK constraints. Precept improves on this by making membership a type-level concept with IDE support and compile-time diagnostics.

**Date type:** `DATE` (day only), `TIME(n)` (time only with fractional seconds), `DATETIME2(n)` (date + time without TZ), `DATETIMEOFFSET(n)` (date + time + TZ offset). Each is a distinct type. [Docs](https://learn.microsoft.com/en-us/sql/t-sql/data-types/date-transact-sql)

*Precept alignment:* The `DATE` type is the direct analog of Precept's `date`. The separation of `DATETIME2` and `DATETIMEOFFSET` illustrates why timezone handling is a separate type problem.

**Decimal type:** `DECIMAL(p,s)` and `NUMERIC(p,s)` are identical. `MONEY` (19 digits, 4 decimal places) and `SMALLMONEY` (10 digits, 4 decimal places) exist; SQL Server documentation warns that division operations on `MONEY` type may lose precision and recommends using `DECIMAL` for financial calculations. [Docs](https://learn.microsoft.com/en-us/sql/t-sql/data-types/decimal-and-numeric-transact-sql)

*Precept alignment:* Another independent confirmation that `MONEY` is a design mistake. `DECIMAL` is the correct financial type.

**Integer type:** `TINYINT` (8-bit), `SMALLINT` (16-bit), `INT` (32-bit), `BIGINT` (64-bit). All signed except `TINYINT`. [Docs](https://learn.microsoft.com/en-us/sql/t-sql/data-types/int-bigint-smallint-and-tinyint-transact-sql)

*Precept simplification:* Precept uses `System.Int64` (64-bit) exclusively — no width parameterization needed for business entity modeling.

---

### MySQL

**Enum type:** `ENUM('v1','v2',...)` — inline declaration, stored as integer index, ordered by declaration position. The documentation notes that this can cause unexpected behavior when values are added out of order later. Comparison uses declaration order. String operations on `ENUM` columns work but can produce surprising results. [Docs](https://dev.mysql.com/doc/refman/8.0/en/enum.html)

*Precept alignment:* MySQL's inline `ENUM` is the closest structural match to Precept's inline `choice(...)`. The ordering-by-declaration-position behavior matches Precept's `ordered` semantics. Precept's `ordered` opt-in avoids the MySQL footgun of implicit ordering that surprises authors.

**Date type:** `DATE` (YYYY-MM-DD), `DATETIME` (no timezone), `TIMESTAMP` (UTC-stored, session-tz-displayed). The distinction between `DATETIME` and `TIMESTAMP` is exactly the determinism argument: `TIMESTAMP` values change meaning depending on session timezone setting. [Docs](https://dev.mysql.com/doc/refman/8.0/en/date-and-time-types.html)

*Precept alignment:* Confirms `date` without time is the safe, deterministic choice for v1.

**Decimal type:** `DECIMAL(p,s)` / `NUMERIC(p,s)` — exact, same semantics as PostgreSQL. [Docs](https://dev.mysql.com/doc/refman/8.0/en/numeric-types.html)

**Integer type:** `TINYINT`, `SMALLINT`, `INT`, `BIGINT`. [Docs](https://dev.mysql.com/doc/refman/8.0/en/numeric-types.html)

---

## Languages

### C# / .NET

**Enum:** `enum Status { Draft, Active, Closed }` — named integer-backed constants. Compiler enforces that variables hold declared values; runtime does not prevent out-of-range integer casts. `[Flags]` enables bitfield semantics. Enums require a full class declaration. [Docs](https://learn.microsoft.com/en-us/csharp/language-reference/builtin-types/enum)

*Precept divergence:* C# enums are integer-backed (not string-backed) and require a separate declaration block. Precept's `choice(...)` is inline, string-backed, and scope-local to the field. For a domain language that must remain human-readable and AI-readable without compilation context, string-backed is the right choice.

**DateOnly:** `System.DateOnly` — introduced in .NET 6. Represents a year-month-day without time component. Arithmetic: `AddDays(n)`, subtraction via `DayNumber` property. ISO 8601 parsing via `DateOnly.Parse("YYYY-MM-DD")`. [Docs](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly)

*Precept alignment:* `System.DateOnly` is the exact backing type for Precept's `date`. The `DayNumber` property enables `date - date` → `integer` arithmetic efficiently.

**Decimal:** `System.Decimal` — 128-bit, base-10 floating point, 28-29 significant decimal digits. Does not widen to `double` implicitly. Division can produce a `DivideByZeroException`. `Math.Round(d, places, MidpointRounding.ToEven)` implements banker's rounding. [Docs](https://learn.microsoft.com/en-us/dotnet/api/system.decimal)

*Precept alignment:* `System.Decimal` is the backing type for `decimal`. The absence of implicit widening to `double` in C# is what makes the `decimal + number → type error` rule correct — it directly mirrors C# semantics.

**Int64:** `System.Int64` — 64-bit signed integer. `long` in C#. Widening to `double` and to `decimal` is both implicit in C#. Division truncates toward zero (not floor). Overflow in `unchecked` context wraps silently. [Docs](https://learn.microsoft.com/en-us/dotnet/api/system.int64)

*Precept alignment:* `System.Int64` is the backing type for `integer`. The widening behavior mirrors Precept's coercion rules.

---

### TypeScript

**String literal union types:** `type Priority = "Low" | "Normal" | "High" | "Urgent"`. Compile-time membership checking, no runtime overhead, no ordering semantics. Template literal types (`type Prefix = "doc_${string}"`) provide constrained string patterns, not closed value sets. [Docs](https://www.typescriptlang.org/docs/handbook/2/everyday-types.html#union-types)

*Precept alignment:* String literal unions are the TypeScript equivalent of `choice`. The absence of ordering semantics in TypeScript's unions validates Precept's `ordered` opt-in as an explicit addition. TypeScript's `const enum` compiles to inlined integers, which is the opposite of Precept's goal (string values for inspectability).

**Numbers:** TypeScript has only `number` (IEEE 754 double). `bigint` for arbitrary-precision integers. No `decimal` type. The TC39 Decimal proposal has been under discussion for years with no standardization. This is a known gap in the TypeScript ecosystem. [TypeScript handbook](https://www.typescriptlang.org/docs/handbook/2/everyday-types.html)

*Precept improvement:* TypeScript's lack of a decimal type is a well-documented pain point for financial applications. Precept adding `decimal` as a first-class type positions it ahead of TypeScript in this dimension.

---

### Kotlin

**Enum classes:** `enum class Severity { LOW, MEDIUM, HIGH, CRITICAL }`. Each constant is an object with `name`, `ordinal`, and optionally custom properties. Comparable by declaration order via `ordinal`. Pattern matching via `when`. [Docs](https://kotlinlang.org/docs/enum-classes.html)

**Sealed classes:** `sealed class Status { object Draft : Status(); data class Active(val since: LocalDate) : Status() }` — richer variant type with data payloads. [Docs](https://kotlinlang.org/docs/sealed-classes.html)

*Precept relationship:* Kotlin's `enum class` is the nominal, typed equivalent of `choice`. Sealed classes are more powerful (payload-bearing variants) but also more complex. Precept's `choice` is deliberate in being payload-free — the string value IS the data.

**Dates:** `java.time.LocalDate` (day only), `java.time.LocalDateTime` (no tz), `java.time.ZonedDateTime` (with tz). Smart casting after null checks applies to date values.

---

### F#

**Discriminated unions:** `type DocumentType = ID | ProofOfAddress | PoliceReport`. Pattern matching is exhaustive — the compiler requires all variants to be handled in `match` expressions. [Docs](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions)

*Precept relationship:* F# discriminated unions are the formal type-theory analog of `choice`. They are more powerful (variants can carry payloads, have methods) and require pattern matching for access. Precept's `choice` is the read-configuration subset of a discriminated union: string-valued, payload-free, equality and optional ordering only.

**Units of measure:** `[<Measure>] type USD` and `[<Measure>] type EUR` — compile-time dimensional type safety. `1.5<USD> + 1.5<EUR>` is a type error. [Docs](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure)

*Future Precept relevance:* Units of measure are the type-system approach to currency safety (preventing USD + EUR). Precept's v1 approach (separate `choice` for currency code, `decimal` for amount) does not provide this guarantee. A future parameterized `money("USD")` type would be the Precept equivalent. This is Batch 3+ territory — noted as a known gap, not a current proposal.

---

### Rust

**Enums:** `enum DocumentType { ID, ProofOfAddress, PoliceReport, MedicalRecord }`. Algebraic data type. Pattern matching via `match` is exhaustive. Derive `PartialOrd`/`Ord` for comparison by declaration order. [Docs](https://doc.rust-lang.org/book/ch06-01-defining-an-enum.html)

*Precept alignment:* Rust's exhaustiveness checking is the gold standard for what Precept's `choice` type checker should enforce: assigning a non-member value is a compile error, and future tooling (like exhaustiveness checks in `when` guards) would mirror `match` coverage analysis.

**Numeric types:** `i32`, `i64` for integers; `f32`, `f64` for floats; no stdlib decimal. The `rust_decimal` crate is common for financial applications. No implicit coercion between numeric types — all conversions are explicit `as` casts.

*Precept alignment:* Rust's strict no-implicit-coercion model is stronger than Precept's model (Precept allows widening). Rust's pattern confirms that mixing integer and float without explicit conversion is a design smell. Precept's widening (integer to decimal or number) is intentional and explicit in the coercion rules.

---

### Python

**Enum:** `class Status(enum.Enum): DRAFT = "draft"; ACTIVE = "active"`. String-valued enums with `.value` for the string. `IntEnum` for integer-backed ordered enums. `StrEnum` (3.11+) for string-backed with comparison support. Membership testing via `Status("draft")` raises `ValueError` for non-members. [Docs](https://docs.python.org/3/library/enum.html)

*Precept alignment:* Python's `StrEnum` is the closest language analog to `choice`. String-backed, declared set, membership enforced. Python's `IntEnum` with ordering is the analog for `choice(...) ordered`. The `ValueError` on non-member construction is the Python equivalent of Precept's compile-time rejection.

**Date:** `datetime.date` — year, month, day. ISO 8601 parsing via `date.fromisoformat("YYYY-MM-DD")`. `timedelta` for date arithmetic (add/subtract days). No timezone on `date`. [Docs](https://docs.python.org/3/library/datetime.html)

*Precept alignment:* Python's `date.fromisoformat()` maps directly to Precept's `date("YYYY-MM-DD")` constructor. The `timedelta` type is the analog of future Precept `duration` — not needed in v1.

**Decimal:** `decimal.Decimal` — IEEE 754-2008 decimal arithmetic with configurable context (precision and rounding mode). `getcontext().prec = 28` sets precision. `ROUND_HALF_EVEN` is banker's rounding. [Docs](https://docs.python.org/3/library/decimal.html)

*Precept alignment:* Python's `Decimal` with `ROUND_HALF_EVEN` is the behavioral model for `round(decimal, N)` with banker's rounding.

---

## Enterprise Platforms

### Salesforce

**Picklist fields:** Field type that stores one value from a defined list. Values are strings. Display labels can differ from API names. Global picklists allow sharing value sets across fields (an analog for future Precept `choiceset`). Picklist values are enforced on write by the Salesforce API. [Docs](https://help.salesforce.com/s/articleView?id=sf.fields_about_picklist_values.htm&type=5)

*Precept alignment:* Salesforce Picklist is the enterprise-platform precedent for `choice`. The global picklist (shared value set) is the platform analog for a future `choiceset` keyword. The local picklist (per-field value set) is the v1 `choice(...)` model. The enforcement-on-write pattern matches Precept's rejection-at-assignment model.

**Date field:** Stores a date without time component. Displayed in user's locale. API stores ISO 8601. Formula fields can reference Date fields for date arithmetic.

**Currency field:** Stores a decimal amount in the organization's currency. Multi-currency orgs have a `CurrencyIsoCode` field that stores the ISO 4217 code separately from the amount. This is the exact `decimal` + `choice("USD","EUR","GBP")` pattern Precept uses.

---

### Dynamics 365 / Dataverse

**Choice column:** A column where the user selects from a predefined option set. Option sets can be local (defined on the column) or global (reused across tables). Values have an integer key and a display label. [Docs](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields)

*Precept alignment:* Dataverse Choice is the platform-level equivalent of `choice`. The global option set is the analog for a future `choiceset`. The local option set is v1 `choice(...)`. Dataverse's integer keys are an implementation detail; Precept uses string values directly.

**Date and Time column:** Three behaviors — `Date Only`, `Date and Time` (user-local time), `Date and Time (Time Zone Independent)`. `Date Only` has no time component and no timezone conversion. [Docs](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/behavior-format-date-time-field)

*Precept alignment:* Dataverse's `Date Only` behavior maps exactly to Precept's `date`. The three behaviors illustrate why the separation matters: `Date and Time` with timezone conversion is a different semantic entity from a calendar date.

**Decimal Number column:** `Decimal` — up to 10 decimal places, exact arithmetic. Distinct from `Whole Number` (integer) and `Floating Point Number` (IEEE 754). The explicit three-way split — Whole Number, Decimal Number, Floating Point — is the direct enterprise precedent for Precept's `integer`, `decimal`, `number` split.

---

### ServiceNow

**Choice field:** `choice` — a string field with a predefined list of values. Dependent choices (child choices filtered by a parent choice value) are supported. Values are stored as strings.

**Date field:** `glide_date` — stored as YYYY-MM-DD. Separate from `glide_date_time`. No timezone conversion on `glide_date`. [Docs](https://docs.servicenow.com/bundle/washingtondc-platform-administration/page/administer/field-administration/reference/field-types.htm)

**Decimal field:** `decimal` — stored as a decimal in the database. Used for financial and measurement values.

**Integer field:** `integer` — 32-bit. `long integer` available for 64-bit values.

*Overall Precept alignment:* ServiceNow's field type vocabulary (`choice`, `glide_date`, `decimal`, `integer`) maps almost exactly to Precept's expansion. The naming alignment across ServiceNow, Dataverse, and Salesforce — all using `choice` or `Choice` and `Date Only` and `Decimal` — is strong product-positioning evidence that these are the right concepts at the right level of abstraction for an entity-definition platform.

---

## End-User Tools

### Excel

**Data Validation lists:** A cell or range can be restricted to a list of values. Values are defined inline or in a range. Membership is enforced on user entry; formulas can bypass validation. [Docs](https://support.microsoft.com/en-us/office/apply-data-validation-to-cells-29fecbcc-d1b9-42c1-9d76-eff3ce5f7249)

**Date cells:** Dates are stored as serial numbers (days since Jan 1, 1900). Displayed in locale-specific or ISO 8601 format. Date arithmetic is integer arithmetic on the serial number. No timezone concept. [Docs](https://support.microsoft.com/en-us/office/format-numbers-as-dates-or-times-418bd3fe-0577-47c8-8caa-b4d30c528309)

**Numbers:** All numbers are IEEE 754 double. No decimal type. `ROUND()`, `ROUNDUP()`, `ROUNDDOWN()` functions exist for display precision, but the underlying storage is still floating-point.

*Precept improvement:* Excel's number precision issues in financial models are legendary (the `0.1 + 0.2` problem surfaces in financial audit contexts regularly). Precept's `decimal` type gives the guarantee Excel cannot. Excel's data validation list is the end-user mental model that `choice` should meet — the user experience of selecting from a known set is already established.

---

### Google Sheets

**Data Validation with dropdown:** Lists, custom criteria, or range-based value constraints. Membership enforced on user input; formulas bypass. [Docs](https://support.google.com/docs/answer/186103)

**Numbers:** Same as Excel — IEEE 754 double. No exact decimal type.

*Precept improvement:* Same analysis as Excel. Sheets users encounter financial precision issues. The data validation dropdown is the end-user analog of `choice`.

---

### Notion

**Select property:** A closed set of colored option values. Author defines the option set; users select from it. New values can be created by users unless restricted by workspace settings. [Docs](https://www.notion.so/help/database-properties)

**Date property:** Stores a date (with optional time). ISO 8601 internally. Date-only mode available.

**Number property:** IEEE 754 double with various display formats (number, dollar, percent, etc.). No exact decimal type.

*Precept improvement:* Notion's Select is the closest end-user analog of `choice`. The ability for users to add new option values (unlike Precept's closed universe) is the key difference — and it is a deliberate Precept strength. A `choice(...)` set is declared in code; it cannot be extended at runtime.

---

## Rule Engines and Decision Systems

### FEEL (DMN / Camunda)

**Types:** `number` (BigDecimal-backed, arbitrary precision), `string`, `boolean`, `date` (ISO 8601 day only), `date and time` (combined), `time`, `days and time duration`, `years and months duration`. No enum type. Constrained value sets in decision tables are expressed as entry conditions, not types. [Docs](https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-data-types/)

*Precept comparison:* FEEL's two-duration-type model (`days and time duration` vs `years and months duration`) is the cautionary precedent for Precept's `duration` deferral. Month arithmetic and day arithmetic are not the same: "add 1 month to 2026-01-31" is ambiguous in days. FEEL solves this with two distinct duration types. Precept's v1 `date` supports integer-day arithmetic only — the correct conservative scope. FEEL's `number` being BigDecimal-backed means FEEL does not need a separate `decimal` type; Precept's `number` is IEEE 754, so Precept does.

---

### Cedar (AWS)

**Types:** `Long` (64-bit integer, the only numeric type), `String`, `Boolean`, `Decimal` extension (fixed 4 decimal places), `datetime` (RFC 3339, includes timezone offset). No float. No enum — constrained values are expressed as policy conditions. [Docs](https://docs.cedarpolicy.com/policies/syntax-datatypes.html)

*Precept comparison:* Cedar's `Long`-only numeric model (no float at all) is at one extreme. Precept's three-type split (integer, decimal, number) is a deliberate middle ground: integer for discrete counts, decimal for exact financial values, and number for the (rare) cases where floating-point approximation is appropriate. Cedar's `Decimal` extension with fixed 4 places is weaker than Precept's `maxplaces N` constraint — 4 places covers many cases but not all (tax rates may require 6).

---

### Drools / NRules

These systems inherit their type systems from the host language (Java / C#). Drools authors use `java.math.BigDecimal` for exact arithmetic, `java.time.LocalDate` for dates, and Java `enum` for constrained values. NRules authors use `System.Decimal`, `System.DateOnly`, and C# `enum`. [Drools docs](https://docs.jboss.org/drools/release/latest/drools-docs/drools-docs.html)

*Precept comparison:* Rule engines that host-language-delegate all type decisions are the "no opinion" baseline. Precept's explicit type vocabulary is a design choice to give entity definitions a self-contained, readable type system that does not require C# knowledge to understand.

---

## Validators

### Pydantic (Python)

`@computed_field` and `model_validator` give Pydantic a rich type-narrowing model. Field types use Python type annotations. `str` can be constrained with `Annotated[str, Field(pattern=r'...')]`. `Enum` fields enforce membership. `datetime.date` for day-only dates. `Decimal` for exact arithmetic.

*Precept comparison:* Pydantic is the closest validator analog to Precept's type system. It uses Python's own types. Precept's DSL type system is the one-file equivalent — no Python class definitions required.

---

### Zod / Valibot

Zod's `z.enum(["A","B","C"])` creates a runtime-enforced string union. `.date()` validates ISO 8601 date strings (not a native date type). No exact decimal type. [Zod docs](https://zod.dev/)

*Precept comparison:* Zod validates at runtime boundaries. Precept enforces at compile time and at the runtime boundary both. Zod's `z.enum(...)` is inline like `choice(...)` — same structural pattern. Zod's lack of a native `date` type (it validates strings that look like dates) confirms that validation-focused tools leave the date type gap for the host language.

---

## Non-Goals

These type additions are explicitly out of scope for the type system expansion and should not recur as proposals without new evidence.

### `money` type

A dedicated `money` type (or `money("USD")`) encodes currency into the type. This requires a parameterized type system and introduces cross-currency type safety (preventing `USD + EUR`). The entire database and enterprise platform ecosystem has converged on the pattern of using `decimal` for amount and a separate code field for currency. Precept follows this consensus. A future parameterized `money("USD")` type would require a significant type-system extension and is not warranted by current domain evidence.

### `timestamp` / timezone-aware `datetime`

Time-of-day with timezone is a determinism problem. The same precept definition would produce different behavior depending on the deployment timezone, violating one-file completeness. This is not a deferral for convenience — it is a deliberate boundary. If Precept ever ships `datetime`, it will require an explicit timezone contract (like Cedar's RFC 3339 with mandatory offset), not a silent timezone assumption.

### `duration` type

Day arithmetic on `date` fields is supported in v1 (`date + integer` adds days). Month and year arithmetic requires a duration type because months and years do not have fixed day counts. FEEL's two-duration-type split (`days and time duration` vs `years and months duration`) shows how complex this becomes. Deferred to Batch 3 research.

### Parameterized `decimal(p,s)`

Database-style precision and scale as type parameters. `System.Decimal`'s 28-29 significant digits covers all practical business scenarios. `maxplaces N` as a constraint is the right mechanism for scale enforcement. Total-digit precision (`precision N`) has no domain evidence in the survey.

### Parameterized integer widths (`int32`, `int16`, `uint`)

No domain evidence. `System.Int64` covers all practical business integers. Width parameterization adds parser complexity for zero user benefit in business entity modeling.

### Structural / record types

Inline structured fields (`field Address as { Street as string, City as string }`). The flat-field model is Precept's authoring design. Nested structures create object-graph semantics (partial nullability, partial mutability, traversal syntax) that require significant type-system work. No domain evidence suggests flat fields are insufficient for the entity-modeling use case.

### Dynamic or open-ended `choice` sets

A `choice` whose members can be extended at runtime defeats the compile-time membership checking guarantee. Open-ended value sets belong to `string` fields with invariants or to external validation. The `choice` type is a closed universe by definition.

### Implicit coercion between `decimal` and `number`

The precision guarantee that `decimal` provides is only meaningful if mixing `decimal` and `number` in arithmetic is never silent. An implicit coercion (in either direction) would either silently degrade to floating-point arithmetic or silently introduce a potentially-lossy conversion. Both are worse than a type error. The `decimal + number → type error` rule is non-negotiable.

### Bit operations on `integer`

`&`, `|`, `^`, `<<`, `>>`. No business entity modeling use case found. These are programming-level operations. Out of scope.

### User-defined function calls on any new type

`choice` values with methods, `date` with custom formatting functions, `decimal` with custom aggregation. The built-in function surface (from #16) is the only extension point. User-defined functions introduce undecidability and are outside Precept's design constraints.

---

## Cross-System Pattern Summary

| Concept | PostgreSQL | SQL Server | MySQL | C# | TypeScript | Kotlin | F# | Rust | Python | Salesforce | Dataverse | ServiceNow | FEEL | Cedar |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Closed value set | ENUM | CHECK | ENUM | enum | string union | enum class | DU | enum | Enum | Picklist | Choice | choice | (none) | (none) |
| Day-only date | DATE | DATE | DATE | DateOnly | (Temporal) | LocalDate | DateOnly | NaiveDate | date | Date | Date Only | glide_date | date | (none) |
| Exact numeric | DECIMAL | DECIMAL | DECIMAL | Decimal | (none) | BigDecimal | decimal | (decimal crate) | Decimal | Currency | Decimal Number | decimal | number | Decimal ext |
| Whole-number | INTEGER/BIGINT | INT/BIGINT | INT/BIGINT | Int64 | (none) | Int/Long | int/int64 | i64 | int | (Number, 0 dec) | Whole Number | integer | (none) | Long |

The pattern is clear: all four type categories exist in every major database system and every enterprise platform. The gap is in general-purpose programming languages (TypeScript lacks decimal; Python lacks integer type; etc.) and in rule/decision engines that delegate to the host language. Precept, as a domain language, should match the database and enterprise platform tier — where type systems serve entity modeling — not the programming language tier, where types serve algorithm implementation.

---

## Semantics Reference — Operators, Literals, Coercion, and Nullability

The per-system sections above are a precedent survey. This section synthesizes the cross-cutting semantic principles that should govern any type in Precept's system — now and in future proposals. All claims are grounded in the evidence above.

---

### Literal Syntax and Constructor Form

A type system's value story starts with how values are written. Three patterns appear across systems:

**Bare literal:** `5`, `"hello"`, `true`. No decoration; the parser assigns a type by token class alone. This is unambiguous for numbers and booleans. It is ambiguous for strings that could be dates (`"2026-01-15"`) or choice members (`"Low"`) — the parser cannot assign the correct type without context.

**Typed prefix / cast:** SQL's `DATE '2026-01-15'`, `CAST('2026-01-15' AS DATE)`. Contextual disambiguation via a keyword before the literal. Verbose but unambiguous at the source level.

**Constructor form:** FEEL's `date("2026-01-15")`, Cedar's `datetime("2024-10-15T00:00:00Z")`. A function-call syntax where the type name is the function and the literal is the argument. Consistent with how complex types are expressed in expression languages. Unambiguous in all positions.

**Design principle: constructor form for non-trivial types.** For types whose values cannot be distinguished from string literals by token class alone — dates, in particular — a constructor form is the safe and consistent choice. It prevents the lexer from needing to look ahead into string content to determine a token's type, and it makes type intent visible at the usage site without requiring type-inference context. Bare numeric literals (`5`, `5.0`) remain sufficient for `integer` and `number` because their token class (digit sequences with or without decimal point) is unambiguous.

**Integer vs decimal literals.** The absence of a decimal point is the canonical integer-literal signal across all surveyed systems: C# (`5` = `int`, `5.0` = `double`), Java (`5` = `int`, `5.0` = `double`), Python (`5` = `int`, `5.0` = `float`). This convention is deep enough that relying on it is not a design choice — it is alignment with universal expectation. `5` in a Precept expression should be inferred as `integer`; `5.0` as `number`.

**Choice member literals.** Choice values are string literals at the source level. Membership validation is a compile-time type check, not a token-class distinction. The type system assigns the choice type based on the field or argument context. `"Low"` is a bare string literal unless the context assigns it a `choice(...)` type, at which point the type checker validates membership.

---

### Arithmetic Operator Closure

**Closure** is the guarantee that applying an operator to operands of known types produces a result of a known type. Without closure, the type checker cannot propagate types through expressions.

#### Integer arithmetic

| Expression | Result type | Notes |
|---|---|---|
| `integer + integer` | `integer` | Closed; no promotion needed |
| `integer - integer` | `integer` | Closed |
| `integer * integer` | `integer` | Closed; overflow is a runtime concern |
| `integer / integer` | `integer` | **Truncates toward zero** — this is C#, Java, Cedar, and SQL convention (not floor division) |
| `integer % integer` | `integer` | Remainder; sign follows dividend (not divisor) |
| `-integer` (unary) | `integer` | Closed |

**Integer division truncates toward zero** is the universal rule for systems that inherit C or Java semantics. `5 / 2 = 2`, `-5 / 2 = -2` (not `-3`). This is a deliberate truncation (toward zero), not a floor (toward negative infinity). Any future type that participates in integer division should inherit this convention.

#### Decimal arithmetic

| Expression | Result type | Notes |
|---|---|---|
| `decimal + decimal` | `decimal` | Exact; intermediate precision is max of operands |
| `decimal - decimal` | `decimal` | Exact |
| `decimal * decimal` | `decimal` | Exact; precision expands |
| `decimal / decimal` | `decimal` | Exact; may produce infinite decimal (banker's rounding at assignment) |
| `-decimal` (unary) | `decimal` | Closed |

**Decimal arithmetic is always closed on `decimal`.** There is no expression involving two `decimal` operands that produces a `number` (IEEE 754) result. This is the `System.Decimal` contract in C#: all `decimal` operators return `decimal`. The same is true of PostgreSQL `NUMERIC` and Python `Decimal`. Violating this would silently degrade exact arithmetic to floating-point.

#### Mixed-type arithmetic

| Expression | Result type | Sound? | Notes |
|---|---|---|---|
| `integer + decimal` | `decimal` | ✓ | Lossless widening of integer |
| `decimal + integer` | `decimal` | ✓ | Symmetric |
| `integer * decimal` | `decimal` | ✓ | Lossless widening |
| `integer + number` | `number` | ✓ | Lossless widening (for representable integers) |
| `number + integer` | `number` | ✓ | Symmetric |
| `decimal + number` | **type error** | — | Exact + approximate = unknown; must be explicit |
| `number + decimal` | **type error** | — | Symmetric; same reasoning |

**The `decimal + number → type error` rule is non-negotiable.** Once a value has been through IEEE 754 arithmetic, its precision is already lost before the addition. Allowing `decimal + number` would mean: start with exact arithmetic, add an approximate value, produce an exact result. The "exact result" would be contaminated by the approximation in the `number` operand. The only honest outcome is a type error — force the author to be explicit about which type they want.

#### Date arithmetic

| Expression | Result type | Notes |
|---|---|---|
| `date + integer` | `date` | Add N days; result is a calendar date |
| `date - integer` | `date` | Subtract N days |
| `date - date` | `integer` | Day count between dates (not a `duration` type in v1) |
| `date + date` | **type error** | Undefined; no meaning |
| `date + decimal` | **type error** | Days must be whole numbers |
| `date + number` | **type error** | Imprecise day count violates day-granularity contract |
| `date.year` | `integer` | Read-only accessor |
| `date.month` | `integer` | Read-only accessor (1–12) |
| `date.day` | `integer` | Read-only accessor (1–31) |
| `date.dayOfWeek` | `integer` | Read-only accessor (ISO 8601: 1=Monday, 7=Sunday) |

**Date arithmetic is closed only with integer day counts.** Adding a `decimal` (e.g., `1.5 days`) is semantically undefined at day granularity. Adding a `number` introduces floating-point imprecision into temporal arithmetic. Both are type errors. When duration types are added in a future wave, `date + duration → date` will be the appropriate form.

---

### Comparison Operators and Cross-Type Policy

All surveyed systems support `==`, `!=`, `<`, `>`, `<=`, `>=` for numeric and date types. The critical dimension is what happens when the two operands have different types.

#### Within-type comparison (always valid)

| Type | `==` / `!=` | `<` / `>` / `<=` / `>=` | Ordering semantics |
|---|---|---|---|
| `integer` | ✓ | ✓ | Numeric |
| `decimal` | ✓ | ✓ | Numeric (exact) |
| `number` | ✓ | ✓ | Numeric (IEEE 754; NaN comparisons are false) |
| `date` | ✓ | ✓ | Chronological |
| `string` | ✓ | ✓ | Lexicographic (byte-order) |
| `boolean` | ✓ | ✗ | No total order on booleans |
| `choice` (no `ordered`) | ✓ (==, !=) | **type error** | No ordinal semantics |
| `choice` (`ordered`) | ✓ | ✓ | Declaration order |

#### Cross-type comparison

| Comparison | Policy | Rationale |
|---|---|---|
| `integer == decimal` | Widens integer to decimal; compares exact | Lossless; correct |
| `integer == number` | Widens integer to number; compares IEEE 754 | May lose precision for very large integers; permitted for practical range |
| `decimal == number` | **type error** | Mixing exact and approximate comparison; result would be meaningless |
| `decimal == string` | **type error** | Different kinds; no coercion |
| `date == integer` | **type error** | Date is not a serial number in Precept |
| `choice == string` | Validates string is a member; compares as string | Allows literal comparisons without constructor |

**`decimal == number` is a type error, not a widening.** If the `number` operand was produced by IEEE 754 arithmetic, its precision is already contaminated. Comparing it to a `decimal` value would compare an exact representation with an approximate one, and a result of `false` would be ambiguous: does it mean the values are different, or does it mean IEEE 754 lost precision? The type error forces the author to be explicit.

**Choice comparison without `ordered` is `==`/`!=` only.** There is no natural ordering on an unordered choice set. `Priority < "High"` is not defined unless `Priority` was declared with `ordered`. This prevents a common enum bug: comparing enum values lexicographically when you meant to compare by domain significance. Lexicographic order (`"High" < "Low"` because `H < L`) is usually wrong for priority enums. Declaration order is the correct semantics — but only when the author explicitly opts in with `ordered`.

---

### Coercion and Widening Policy

**Widening** is a type promotion that is always lossless. **Narrowing** is a conversion that may lose information and is never implicit.

#### Widening table (always implicit, always lossless)

| From | To | Lossless? | C# analog |
|---|---|---|---|
| `integer` | `decimal` | ✓ | `long` → `decimal` |
| `integer` | `number` | ✓ (for representable integers) | `long` → `double` |
| `decimal` | `number` | **never** (explicit only) | `decimal` → `double` is explicit in C# |
| `number` | `decimal` | **never** (explicit only) | `double` → `decimal` is explicit in C# |

There are exactly two valid widening paths: integer widens to decimal, and integer widens to number. No other widening is defined. This graph is:

```
integer → decimal
integer → number
```

There is intentionally no `decimal ↔ number` edge. The two numeric representations are not in a subtype relationship for assignment purposes.

#### Narrowing (always explicit via function)

| Narrowing | Function required |
|---|---|
| `decimal` → `integer` | `truncate(decimal)`, `floor(decimal)`, `ceil(decimal)` |
| `number` → `integer` | `truncate(number)`, `floor(number)`, `ceil(number)` |
| `decimal` → `number` | Not defined (avoids exact→approximate confusion) |
| `number` → `decimal` | Not defined (the number may already be imprecise) |

**No implicit narrowing.** C#'s rule is definitive: `long = decimal` is a compile error, requiring an explicit cast or conversion. Precept follows this. The author must call `truncate()`, `floor()`, or `ceil()` to convert a fractional value to an integer, making the truncation decision explicit and readable.

#### Assignment coercion

Assignment is a subset of expression coercion. The target field's type constrains what can be assigned:

| Assigned from → | to `integer` field | to `decimal` field | to `number` field |
|---|---|---|---|
| `integer` expression | ✓ | ✓ (widens) | ✓ (widens) |
| `decimal` expression | **type error** | ✓ | **type error** |
| `number` expression | **type error** | **type error** | ✓ |

**`decimal field = number expr` is a type error** even though `integer` widens to both. The decimal field's exact-arithmetic contract would be violated the moment an approximate value is assigned to it. Similarly, `number field = decimal expr` is a type error: not because it would cause data loss, but because it allows the author to silently escape the exact-arithmetic boundary. If an author wants to assign a decimal result to a number field, they have chosen a type mismatch that should be explicit.

---

### Nullability Interaction

#### Null semantics across paradigms

Three distinct null models appear in the survey:

1. **Three-valued logic (SQL NULL):** `NULL` propagates through arithmetic — `NULL + 5 = NULL`. Comparisons produce `UNKNOWN` — `NULL = 5` is neither `true` nor `false`. Requires `IS NULL` / `IS NOT NULL` checks. The source of many bugs: `WHERE status != 'Approved'` silently excludes `NULL` rows.

2. **Propagating null with symmetric equality (FEEL):** `null` is a value; arithmetic with `null` produces `null`. But `null = null` is `true` (unlike SQL). Comparisons involving `null` propagate `null`.

3. **Nullable types with two-valued logic (C#, Kotlin, TypeScript strict null):** Null is a distinct type-level annotation (`T?`). Operations on a nullable type produce nullable results. Smart casts and type narrowing narrow `T?` to `T` within a null-checked branch. Boolean operators evaluate normally on non-null booleans; they do not propagate null.

**Precept's model follows C#'s nullable type approach** (not SQL three-valued logic). A `nullable` field is annotated at declaration time; expressions that reference it may produce nullable results; the type checker requires explicit null guards before accessing a nullable field in an expression. This is the deterministic, two-valued model: constraints are either satisfied or not, with no `UNKNOWN` outcome.

#### Constraint semantics with null

The SQL `CHECK CONSTRAINT` null rule is the correct model for Precept constraints: **a constraint that checks a nullable field applies only to non-null values**. A field declared `decimal nullable min 0` accepts `null` and also accepts any non-null decimal ≥ 0. It does not accept `-5` (violates `min 0`), but it does accept `null` (null is not subject to `min`).

This is also C#'s behavior for nullable value types with `[Range]` attributes: the attribute is skipped when the value is `null`.

#### Nullable arithmetic propagation

| Expression | Result | Notes |
|---|---|---|
| `nullable_field + 5` | nullable | If field is null, result is null |
| `nullable_field == 5` | boolean | False if null (not unknown) — Precept uses two-valued logic |
| `nullable_field != null` | boolean | True only if field has a value |
| `nullable_field > 0 && nullable_field < 100` | boolean | The `&&` short-circuits; if field is null, `nullable_field > 0` is false |

**Null guards narrow the type.** Following a check `when Field != null`, the type of `Field` within that branch is narrowed to the non-nullable form. This eliminates the need for defensive null checks inside the guarded expression. This is the Kotlin smart-cast / TypeScript type-narrowing pattern applied to Precept's nullable model.

#### Default values and null

A field with a `default` may not also be `nullable` in most cases — the default eliminates the need for null. Conversely, a `nullable` field without a `default` starts as `null` at `CreateInstance`. This is consistent with C# nullable properties and SQL `DEFAULT NULL`.

---

### Collection Membership Types

A typed collection constrains both the operations available on it and the types of values it can hold. The key principle across all surveyed systems: **the element type of a collection is part of the collection's type, and the type system enforces membership at the element level.**

#### Typed collections and element type enforcement

| System | Mechanism | Compile-time enforcement |
|---|---|---|
| C# `HashSet<T>` | Generic type parameter | `Add(wrongType)` is compile error |
| TypeScript `Set<T>` | Generic type parameter | Type error on wrong element |
| PostgreSQL `ARRAY` | Element type in declaration (`int[]`) | Runtime rejection for type mismatch |
| PostgreSQL enum | Enum column | Runtime rejection for non-member values |

**For Precept collections, the element type is the field's declared element type.** A `set of string` accepts any string. A `set of choice("ID","Passport")` accepts only `"ID"` or `"Passport"`. A `set of integer` accepts only whole numbers. Attempting to `add` a value that violates the element type is a compile-time error — not a constraint violation at runtime.

#### `set of choice(...)` — membership is part of the type

The critical semantic point for choice-typed collections: **a `set of choice(...)` is not a `set of string` with a runtime membership invariant.** It is a typed collection whose element type is a `choice` type. The distinction matters:

- `set of string` — any string; membership constraint would be an `invariant`
- `set of choice("ID","Passport")` — only `"ID"` or `"Passport"`; membership is enforced at compile time by the type checker on every `add` operation

This is the `Set<DocumentType>` in C# or TypeScript — `add("BirthCertificate")` is a compile error if `BirthCertificate` is not a declared member of `DocumentType`.

#### Collection accessor types

Collection accessors return typed values. Their types must be consistent with the element type:

| Accessor | Element type | Return type | Notes |
|---|---|---|---|
| `.count` | any | `integer` | Always defined; empty collection → 0 |
| `.min` | `number`, `integer`, `decimal`, `date` | same as element | Undefined on empty collection |
| `.max` | `number`, `integer`, `decimal`, `date` | same as element | Undefined on empty collection |
| `.peek` | any | element type (nullable) | Undefined on empty queue/stack |
| `contains` | any | `boolean` | Always defined |

The return type of `.min` and `.max` follows the element type — for a `set of integer`, `.min` returns `integer`. For a `set of decimal`, `.max` returns `decimal`. This is type closure applied to collection accessors: the type of the element determines the type of the accessor result.

**`.count` returns `integer`, not `number`.** A count of elements in a collection is always a whole number. The current behavior (returning `number` for backward compatibility) is a temporary compromise — the correct type is `integer`, and when `integer` ships, `.count`'s return type should be refined. However, changing `.count` to return `integer` widens its usability (integer widens to number, so existing comparisons like `.count > 0` where `0` is currently a `number` literal would still work when `0` becomes an `integer` literal).

---

### Exact-Decimal vs Integer Semantics — Synthesis

The three-type numeric split exists to serve three distinct semantic domains in business entity modeling. The MONEY anti-pattern is the clearest evidence that conflating these domains produces systems that cannot be trusted.

#### Domain mapping

| Semantic domain | Correct Precept type | Why NOT the other types |
|---|---|---|
| **Counting things** (participants, seats, defects, days delinquent) | `integer` | These are discrete units; `2.7 participants` is semantically wrong; `decimal maxplaces 0` is a hack that communicates the wrong intent |
| **Exact financial amounts** (prices, premiums, tolerances, rates) | `decimal` | IEEE 754 produces `0.30000000000000004`; invoice totals will not sum; audit tests will fail |
| **Scientific / ratio computations** (where approximation is acceptable) | `number` | When the calculation is inherently approximate and the result's imprecision is acceptable to the domain |

#### The MONEY type anti-pattern

PostgreSQL's `MONEY` type is the definitive negative example. It was designed to simplify financial amounts but introduced locale-dependent behavior and imprecise division. The PostgreSQL community now [documents it as something to avoid](https://www.postgresql.org/docs/current/datatype-money.html), and the PostgreSQL wiki's "Don't Do This" guide lists it explicitly.

The lesson: **a named type that embeds domain semantics (currency) into a numeric type creates problems that outweigh the convenience.** The correct design is:

1. `decimal` for the amount (exact arithmetic, precision-constrained via `maxplaces`)
2. `choice("USD", "EUR", "GBP")` for the currency code (closed set, compile-time checked)

These two fields together model a monetary amount more correctly than any `money` type, and they do so without requiring the type system to understand currencies, exchange rates, or arithmetic rules for mixed-currency operations.

#### Integer division as a semantic boundary

`integer / integer → integer` (truncating) is the correct rule for counting-domain arithmetic. `5 participants / 2 groups = 2 (whole groups)` — not `2.5`. The truncation is semantically meaningful: you cannot have half a group. When a fractional result IS needed, the author must make the type promotion explicit: `decimal(5) / decimal(2) = 2.5` (exact).

This is also why `integer / integer → number` would be wrong: it would silently produce a floating-point approximation of a count, mixing the counting domain with the approximation domain.

#### Banker's rounding as the standard for `round()`

When explicit rounding is required (for `decimal` fields with `maxplaces` constraints), the rounding mode matters for financial correctness. Three modes are in common use:

| Mode | Rule | Bias | Precedent |
|---|---|---|---|
| Half-up (common) | Round 0.5 always up | Positive bias over large datasets | SQL `ROUND()` default |
| Half-down | Round 0.5 always down | Negative bias | Uncommon |
| Banker's rounding (half-even) | Round 0.5 to nearest even | No systematic bias | C# `Math.Round` default, Python `round()`, FEEL decimal function |

**Banker's rounding is the statistically neutral choice.** For large datasets where many values fall at the midpoint, always rounding up (or down) produces a systematic bias that accumulates into audit discrepancies. Banker's rounding eliminates this bias. Its use in C#'s `Math.Round(v, n, MidpointRounding.ToEven)` and Python's `round()` built-in makes it the .NET and Python default, which is the correct alignment for Precept's `round()` function.

---

## Key References

- [PostgreSQL Numeric Types](https://www.postgresql.org/docs/current/datatype-numeric.html)
- [PostgreSQL ENUM Type](https://www.postgresql.org/docs/current/datatype-enum.html)
- [PostgreSQL Date/Time Types](https://www.postgresql.org/docs/current/datatype-datetime.html)
- [PostgreSQL MONEY Type](https://www.postgresql.org/docs/current/datatype-money.html)
- [C# Numeric Conversions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/numeric-conversions)
- [System.Decimal (.NET)](https://learn.microsoft.com/en-us/dotnet/api/system.decimal)
- [System.Int64 (.NET)](https://learn.microsoft.com/en-us/dotnet/api/system.int64)
- [System.DateOnly (.NET 6+)](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly)
- [MidpointRounding (.NET)](https://learn.microsoft.com/en-us/dotnet/api/system.midpointrounding)
- [Cedar Datatypes](https://docs.cedarpolicy.com/policies/syntax-datatypes.html)
- [Cedar Operators](https://docs.cedarpolicy.com/policies/syntax-operators.html)
- [FEEL Data Types (Camunda docs)](https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-data-types/)
- [OMG DMN 1.4 Specification](https://www.omg.org/spec/DMN/1.4/PDF)
- [Java BigDecimal](https://docs.oracle.com/en/java/se/17/docs/api/java.base/java/math/BigDecimal.html)
- [TypeScript Union Types and Literal Types](https://www.typescriptlang.org/docs/handbook/2/everyday-types.html#union-types)
- [Python Enum](https://docs.python.org/3/library/enum.html)
- [Python Decimal](https://docs.python.org/3/library/decimal.html)
- [Python datetime.date](https://docs.python.org/3/library/datetime.html)
- [XSD Numeric Types (W3C)](https://www.w3.org/TR/xmlschema-2/#numeric)
