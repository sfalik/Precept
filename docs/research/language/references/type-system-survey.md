# Type System Survey — Reference and Semantic Contracts

**Research date:** 2026-06-12
**Author:** Frank (Lead/Architect & Language Designer)
**Relevance:** Formal grounding for evaluating type-system expansion proposals (#25, #26, #27, #29). Covers literals, operators, coercion policy, nullability interaction, numeric semantics, categorical values, comparison rules, and collection-membership implications.

**Companion document:** [type-system-domain-survey.md](../expressiveness/type-system-domain-survey.md) covers domain-level research (sample pressure, cross-category precedent, philosophy fit, dead ends). This file covers the theory, reference semantics, and semantic contracts that must hold regardless of domain.

---

## Scope and Non-Goals

### In scope

- Literal syntax and parsing rules for each proposed type
- Operator semantics: which operators are defined, what types they accept, what types they return
- Coercion policy: which implicit conversions exist, which are forbidden, and why
- Nullability interaction: how `nullable` composes with each new type
- Exact-decimal vs integer vs approximate-number semantics
- Ordered vs unordered categorical values
- Comparison rules across type boundaries
- Collection-membership implications (inner types for `set`, `queue`, `stack`)
- Explicit non-goals for this type-system pass

### Not in scope

- Domain justification for each type (see [type-system-domain-survey.md](../expressiveness/type-system-domain-survey.md))
- Implementation plan or parser mechanics (see proposal bodies in `temp/issue-body-rewrites/`)
- Function-call surface (`round`, `floor`, `ceil`, `truncate`) — covered by #16 and #27 proposals; only function *signatures* relevant to type semantics are noted here
- Grammar or language-server completions (tooling sync, not theory)

---

## Current Type System

Precept's current scalar types, per the [language catalog](../../../../tools/Precept.Mcp/):

| Type | Literal form | Operators | Accessors | Collection inner type |
|------|-------------|-----------|-----------|----------------------|
| `string` | `"text"` | `==`, `!=`, `contains` | — | `set of string`, `queue of string`, `stack of string` |
| `number` | `42`, `3.14`, `-7` | `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `>`, `<`, `>=`, `<=` | — | `set of number`, `queue of number`, `stack of number` |
| `boolean` | `true`, `false` | `==`, `!=`, `&&` / `||` / `!` (pending: `and`, `or`, `not`) | — | `set of boolean`, `queue of boolean`, `stack of boolean` |

Collection accessors (all collection kinds): `.count` → `number`, `.min` / `.max` → inner type (numeric only), `.peek` → inner type (queue/stack only).

`null` is a literal value, not a type. Any type with the `nullable` modifier accepts `null`.

---

## Reference Survey: Type Semantics in Rule and Decision Systems

This survey covers how six rule/decision systems handle the four proposed type domains. These are the same systems referenced by the proposal bodies — this section provides the primary-source grounding.

### 1. FEEL (DMN) — Friendly Enough Expression Language

**Source:** [OMG DMN Specification §10 FEEL Semantics](https://www.omg.org/spec/DMN/1.4/PDF), [Camunda FEEL Data Types](https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-data-types/)

| Concern | FEEL semantics |
|---------|---------------|
| **Numeric** | Single `number` type backed by Java `BigDecimal`. All arithmetic is exact. No integer/float distinction in the type system. |
| **Categorical** | No first-class enum. String comparison only. |
| **Temporal** | `date` (day-granularity), `time`, `date and time`, `days and time duration`, `years and months duration`. Five distinct temporal types. |
| **Literals** | Numbers: `42`, `3.14`. Dates: `date("2026-03-15")`. Strings: `"text"`. Booleans: `true`, `false`. |
| **Coercion** | No implicit coercion between types. `number + date` is a type error in the spec (Camunda allows `date + number` as add-days). |
| **Null** | First-class `null` value. `null + 1 = null` (null-propagating arithmetic). `null == null` is `true`. |
| **Comparison** | Defined only between same-type operands. Cross-type comparison produces `null` (not an error). |
| **Collections** | `list` type with index access, filtering, quantified expressions (`every`, `some`). |

**Key design lesson:** FEEL chose a single exact-number type, avoiding the integer/decimal split entirely. This simplifies coercion but loses the semantic signal that "this field is a count." FEEL's five-temporal-type model shows a mature system splitting date from time from duration — Precept's day-only `date` is the correct bounded first step.

### 2. Cedar (AWS) — Authorization Policy Language

**Source:** [Cedar Language Reference](https://docs.cedarpolicy.com/policies/syntax-grammar.html), [Cedar Operators](https://docs.cedarpolicy.com/policies/syntax-operators.html)

| Concern | Cedar semantics |
|---------|----------------|
| **Numeric** | `Long` only (64-bit signed integer). No floating point. No decimal. Deliberate restriction for formal verification. |
| **Categorical** | No first-class enum. String equality only. |
| **Temporal** | `datetime` and `duration` as extension types. `datetime` is offset-aware (RFC 3339). |
| **Literals** | Integers: `42`. Strings: `"text"`. Booleans: `true`, `false`. Extension constructors: `datetime("2026-03-15T00:00:00Z")`, `duration("5d")`. |
| **Coercion** | None. Cedar has no implicit conversions. `Long + String` does not type-check. |
| **Null** | No `null` value. Attributes are either present or absent; missing-attribute access is a type error. |
| **Comparison** | `<`, `>`, `<=`, `>=` defined on `Long` only. Strings support `==`, `!=`, `like` (glob). |
| **Collections** | `Set` type with `contains`, `containsAll`, `containsAny`. No ordered lists, no index access. |

**Key design lesson:** Cedar deliberately omits decimal and floating-point types to preserve formal verifiability. Its `Long`-only model validates that integer-as-sole-numeric is viable for policy systems, but business entity governance needs fractional arithmetic. Cedar's zero-coercion policy is the strongest precedent for Precept's "no silent conversion" rule.

### 3. Drools (Red Hat) — Business Rule Engine

**Source:** [Drools Documentation — DRL Rules](https://docs.drools.org/latest/drools-docs/drools/language-reference/index.html)

| Concern | Drools semantics |
|---------|-----------------|
| **Numeric** | Inherits Java: `int`, `long`, `float`, `double`, `BigDecimal`, `BigInteger`. Full Java numeric tower. |
| **Categorical** | `declare enum` or Java `enum`. First-class with switch/case matching. |
| **Temporal** | `java.time.LocalDate`, `LocalDateTime`, `Duration`, etc. Drools itself adds temporal operators (`before`, `after`, `during`, `meets`). |
| **Literals** | Java literals: `42`, `3.14`, `42L` (long), `3.14B` (BigDecimal). |
| **Coercion** | Java widening rules. `int → long → float → double` implicit. `BigDecimal` requires explicit construction. |
| **Null** | Java `null`. NullPointerException on unguarded access. |
| **Comparison** | Java semantics. `==` is identity for objects unless overridden; `.equals()` for value equality. Drools LHS uses `==` as value comparison. |
| **Collections** | Java collections. `from` clause iterates. `accumulate` for aggregation. |

**Key design lesson:** Drools inherits the full Java type system, gaining power but losing one-file completeness. Its `declare enum` is the closest model for Precept's `choice` — a type declared inside the rule definition rather than externally.

### 4. NRules (.NET)

**Source:** [NRules Wiki — Getting Started](https://github.com/NRules/NRules/wiki/Getting-Started)

| Concern | NRules semantics |
|---------|-----------------|
| **Numeric** | Inherits C#: `int`, `long`, `decimal`, `double`. Full .NET numeric hierarchy. |
| **Categorical** | C# `enum`. Not declared in rules; declared in host code. |
| **Temporal** | C# `DateTime`, `DateOnly`, `DateTimeOffset`. Host types. |
| **Literals** | C# literals within LINQ expressions. |
| **Coercion** | C# implicit/explicit conversion rules. `int → long → decimal` implicit. `double → decimal` requires explicit cast. |
| **Null** | C# nullable reference/value types. |
| **Comparison** | C# `==`, `IComparable<T>`. |
| **Collections** | C# `IEnumerable<T>`, LINQ queries. |

**Key design lesson:** NRules is the "just use the host language" model. It demonstrates that for .NET rule systems, `decimal` and `int`/`long` are the standard carriers. Precept can't follow this model (no one-file completeness), but NRules validates the C#-aligned coercion semantics proposed for `integer` and `decimal`.

### 5. BPMN / Camunda

**Source:** [BPMN 2.0 Specification](https://www.omg.org/spec/BPMN/2.0.2/PDF), [Camunda Modeler FEEL Types](https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-data-types/)

| Concern | BPMN semantics |
|---------|---------------|
| **Numeric** | XSD types: `xsd:integer`, `xsd:decimal`, `xsd:double`. Three distinct numeric tiers. |
| **Categorical** | XSD `enumeration` restriction on `xsd:string`. |
| **Temporal** | `xsd:date`, `xsd:time`, `xsd:dateTime`, `xsd:duration`. ISO 8601 lexical forms. |
| **Literals** | XSD lexical representations. `2026-03-15` for date, `42` for integer, `3.14` for decimal. |
| **Coercion** | XSD type hierarchy. `xsd:integer` is a restriction of `xsd:decimal`. Widening is implicit in the type hierarchy. |
| **Null** | `xsi:nil` attribute. Separate from empty values. |
| **Comparison** | XSD-defined for each type. Date comparison is chronological. Enumeration comparison is string equality unless restricted. |
| **Collections** | Not first-class in BPMN data; modeled as multi-instance activities. |

**Key design lesson:** BPMN's XSD foundation explicitly separates `integer` as a restriction of `decimal`. This is the strongest standards-body precedent for treating `integer` as a semantically distinct type rather than a constraint on `number`. The `xsd:enumeration` model — restricting a base type to named values — aligns with Precept's `choice` design (restricting string to a closed set).

### 6. SQL (ISO Standard / PostgreSQL)

**Source:** [SQL:2016 Standard Part 2: Foundation](https://www.iso.org/standard/63556.html), [PostgreSQL Numeric Types](https://www.postgresql.org/docs/current/datatype-numeric.html), [PostgreSQL Enum Types](https://www.postgresql.org/docs/current/datatype-enum.html), [PostgreSQL Date/Time Types](https://www.postgresql.org/docs/current/datatype-datetime.html)

| Concern | SQL semantics |
|---------|--------------|
| **Numeric** | `INTEGER` / `BIGINT` (exact whole), `NUMERIC(p,s)` / `DECIMAL(p,s)` (exact fractional), `REAL` / `DOUBLE PRECISION` (approximate). Three-tier numeric tower: integer → exact decimal → approximate float. |
| **Categorical** | `ENUM` (PostgreSQL extension). Ordered by declaration order. `CREATE TYPE mood AS ENUM ('sad', 'ok', 'happy')`. Also `CHECK(col IN ('a','b','c'))` as the standard-SQL alternative. |
| **Temporal** | `DATE`, `TIME`, `TIMESTAMP`, `INTERVAL`. `DATE` is day-granularity. `TIMESTAMP WITH TIME ZONE` is offset-aware. `INTERVAL` supports complex period arithmetic. |
| **Literals** | `42` (integer), `3.14` (numeric), `'text'` (string), `DATE '2026-03-15'` (typed date literal). |
| **Coercion** | Implicit widening: `INTEGER → NUMERIC → DOUBLE PRECISION`. No implicit narrowing. `NUMERIC` to `INTEGER` requires explicit `CAST` or `TRUNC`. |
| **Null** | `NULL` is a mark, not a value. Three-valued logic: `TRUE`, `FALSE`, `UNKNOWN`. `NULL = NULL` is `UNKNOWN`, not `TRUE`. `IS NULL` / `IS NOT NULL` for null testing. |
| **Comparison** | Defined per type. `ENUM` comparison uses declaration order. `DATE` comparison is chronological. Cross-type numeric comparison widens. |
| **Collections** | `ARRAY` type (PostgreSQL). `ANY`, `ALL` operators. No `SET` type in standard SQL. |

**Key design lesson:** SQL's three-tier numeric tower (integer → decimal → float) is the most complete model for what Precept is proposing. The `ENUM` type with declaration-order comparison directly validates `choice ... ordered`. PostgreSQL's `MONEY` type — [widely considered a mistake](https://wiki.postgresql.org/wiki/Don't_Do_This#Don.27t_use_money) — validates Precept's decision to use `decimal` + `choice` for currency instead of a dedicated `money` type.

---

## Semantic Contracts

These contracts must hold across the type-system expansion. They are derived from the reference survey and from Precept's design principles (determinism, inspectability, prevention, no silent behavior).

### Contract 1: No silent coercion across type boundaries

**Rule:** No implicit conversion between:
- `string` ↔ `choice` (choice is structurally distinct from open text)
- `number` ↔ `decimal` (approximate ↔ exact)
- `number` ↔ `integer` (fractional ↔ whole; *widening* from `integer` to `number` is allowed)
- `decimal` ↔ `number` (exact ↔ approximate)
- `string` ↔ `date` (no auto-parsing)
- `choice` ↔ `date`, `decimal`, `integer`, `number`, `boolean` (categorical values are strings, but the type boundary is real)

**Widening hierarchy (implicit, safe):**
```
integer → decimal   (whole number is exact fractional with zero decimal places)
integer → number    (whole number is representable as double)
```

**Narrowing (always explicit, never implicit):**
```
number → integer    (requires truncate/floor/ceil function)
number → decimal    (forbidden — choose decimal from the start)
decimal → integer   (requires truncate/floor/ceil function)
```

**Reference justification:**
- Cedar: zero coercion. Every type boundary is explicit.
- SQL: implicit widening (INTEGER → NUMERIC → FLOAT), explicit narrowing (CAST required).
- FEEL: no coercion between types; cross-type operations produce `null`.
- C# (.NET): implicit widening (`int → long → decimal`), explicit narrowing.

Precept follows the SQL/C# widening model — it is familiar to .NET developers and loses no safety because widening is value-preserving. Narrowing is always explicit because it is value-changing.

### Contract 2: Literal parsing rules are unambiguous

**Rule:** A literal's type is determined by its syntactic form alone, with no context-dependent inference.

| Literal form | Type | Examples |
|-------------|------|---------|
| `"text"` | `string` | `"hello"`, `""`, `"2026-03-15"` |
| Digit sequence without `.` | `integer` | `42`, `0`, `-7` |
| Digit sequence with `.` | `number` | `3.14`, `0.0`, `-7.5` |
| `true`, `false` | `boolean` | |
| `null` | null literal | (assignable to any `nullable` target) |
| `date("YYYY-MM-DD")` | `date` | `date("2026-03-15")` |
| `choice(...)` | — | Not a literal; a type constructor in declarations only |

**Design notes:**
- Integer literals (`42`) parse as `integer` when the `integer` type exists. Before #29 ships, all bare numerics parse as `number` (current behavior). This is the one parsing-behavior change that #29 introduces.
- `number` literals always contain a decimal point: `5.0` is `number`, `5` is `integer`. This mirrors C# (`5` is `int`, `5.0` is `double`).
- `decimal` has no distinct literal form in v1. Decimal fields receive values from integer literals (widening), from `round()` results, and from event arguments. A future `5m` or `decimal(5.00)` literal suffix is possible but not proposed.
- `date(...)` is a constructor call, not a bare literal. This avoids context-dependent string parsing (`"2026-03-15"` remains a string, always).

**Reference justification:**
- SQL: `DATE '2026-03-15'` uses a typed literal prefix. Precept's `date("...")` is the keyword-anchored equivalent.
- Cedar: `datetime("2026-03-15T00:00:00Z")` uses a constructor form. Same pattern.
- FEEL: `date("2026-03-15")` — identical syntax to what Precept proposes.

### Contract 3: Operator validity is statically determined by operand types

**Rule:** The type checker can determine at compile time whether an operator application is valid, based solely on the declared types of its operands. No runtime type tests.

| Operator class | Valid operand types | Result type |
|---------------|--------------------| ------------|
| Arithmetic (`+`, `-`, `*`, `/`, `%`) | `integer` × `integer` → `integer`; `integer` × `decimal` → `decimal`; `integer` × `number` → `number`; `decimal` × `decimal` → `decimal`; `number` × `number` → `number`; `date` + `integer`/`number` → `date`; `date` - `date` → `integer` (day count) | See per-pair |
| Comparison (`==`, `!=`) | Same-type or widening-compatible | `boolean` |
| Ordering (`<`, `>`, `<=`, `>=`) | `number` × `number`, `integer` × `integer` (or widened), `decimal` × `decimal` (or widened), `date` × `date`, `choice` × `choice` (only with `ordered`, same field type) | `boolean` |
| Membership (`contains`) | `collection` × `inner type` | `boolean` |
| Assignment (`=`) | Target type must accept source type (same type or implicit widening) | — |

**Forbidden cross-type operations (compile-time errors):**
- `decimal + number` / `number + decimal` — mixing approximate and exact
- `choice + anything` / `choice - anything` — categorical values are not arithmetic
- `date + date` — adding two dates is meaningless
- `date * number` — scaling a date is meaningless
- `string + number` — no implicit concatenation/coercion
- `choice < choice` on an unordered choice field — ordering must be declared

**Reference justification:**
- Cedar: all operators are statically typed. Invalid combinations are compile-time errors.
- SQL: operator validity is resolved at query planning (compile) time. `DATE + DATE` is an error.
- FEEL: cross-type operators produce `null` — a weaker form of rejection. Precept should reject at compile time, not produce null.

### Contract 4: `choice` is a closed type, not decorated `string`

**Rule:** A `choice(...)` field is a distinct type. String operations do not apply. Assignment accepts only declared members.

| Property | Contract |
|----------|----------|
| Member values | String literals declared at field definition time |
| Universe | Closed at compile time. No runtime extension. |
| Equality | `==`, `!=` between choice values of the same field type |
| Ordering | Only with `ordered` keyword. Declaration order defines the total order (first = lowest, last = highest). |
| Cross-field compatibility | Two fields with textually identical `choice(...)` declarations are NOT assignment-compatible. Each declaration creates a distinct type. |
| String interop | `choice` values are serialized as strings in JSON/MCP. But `choice == string` is a compile-time error. No implicit conversion. |
| Collection inner type | `set of choice(...)` is valid. `add` validates membership at compile time. |

**Ordered choice semantics:**

Given `field Severity as choice("Low", "Medium", "High") ordered`:
- `Severity == "High"` → valid
- `Severity > "Low"` → valid (compares by declaration position: "Low" = 0, "Medium" = 1, "High" = 2)
- `Severity > "Unknown"` → compile-time error ("Unknown" is not a member)

Without `ordered`: `>`, `<`, `>=`, `<=` are compile-time errors. `==`, `!=` remain valid.

**Reference justification:**
- PostgreSQL ENUM: ordered by declaration order. `'sad' < 'ok' < 'happy'` is valid. Ordering is intrinsic, not opt-in. Precept's `ordered` opt-in is more conservative.
- SQL CHECK: `CHECK(col IN ('a','b','c'))` constrains values but provides no ordering. Equivalent to unordered `choice`.
- Cedar: no enum; string equality only. No ordering on categorical values.
- FEEL: no enum. String comparison uses lexicographic order, which is not domain-meaningful.
- Drools: `declare enum` with declaration-order semantics (through Java enum ordinals).

### Contract 5: `date` means calendar day with no timezone and no time-of-day

**Rule:** `date` is a day-granularity value in the proleptic Gregorian calendar. No timezone. No sub-day precision. Deterministic by construction.

| Property | Contract |
|----------|----------|
| Granularity | Day (no hours, minutes, seconds) |
| Calendar | Proleptic Gregorian (ISO 8601) |
| Constructor | `date("YYYY-MM-DD")` — ISO 8601 format only |
| Timezone | None. Dates are naive calendar facts. |
| Arithmetic | `date + integer` → `date` (add N days), `date - integer` → `date`, `date - date` → `integer` (day count, signed) |
| Comparison | `<`, `>`, `<=`, `>=`, `==`, `!=` — chronological ordering |
| Accessors | `.year` → `integer`, `.month` → `integer`, `.day` → `integer`, `.dayOfWeek` → `integer` (1=Mon, 7=Sun per ISO 8601) |
| Null | `nullable` modifier allowed. Null date is distinct from any calendar date. |
| Invalid dates | `date("2026-02-30")` is a compile-time error when the literal is known, a runtime rejection otherwise |

**What `date` is NOT:**
- Not a timestamp. No time component.
- Not timezone-aware. `date("2026-03-15")` means the same calendar day regardless of where the server runs.
- Not a duration. `date - date` produces a day count (integer), not a duration object.

**Reference justification:**
- FEEL: `date("2026-03-15")` — identical constructor syntax. Day-granularity. Separate from `date and time`.
- SQL: `DATE` type is day-granularity. `DATE '2026-03-15'`. Arithmetic produces intervals (Precept simplifies to integer day count in v1).
- Cedar: `datetime` is offset-aware (RFC 3339). Precept deliberately avoids this — a naive date is deterministic; an offset-aware datetime requires ambient context.
- PostgreSQL: `DATE` is calendar-day-only. `DATE - DATE` → `INTEGER` (days). Direct alignment.

### Contract 6: `decimal` means exact base-10 arithmetic, not "number with precision annotation"

**Rule:** `decimal` uses exact base-10 representation (.NET `System.Decimal`). No floating-point approximation. No silent rounding. Precision is constrained by `maxplaces`, not by type parameters.

| Property | Contract |
|----------|----------|
| Representation | .NET `System.Decimal` (128-bit, 28–29 significant digits) |
| Arithmetic | Exact. `0.1 + 0.2 == 0.3` is `true`. |
| Precision constraint | `maxplaces <N>` — values with more than N decimal places are **rejected**, not rounded |
| Rounding | Explicit only, via `round(decimal, N)`. Banker's rounding (MidpointRounding.ToEven). |
| Mixed with `integer` | Implicit widening: `integer → decimal`. `decimal + integer` → `decimal`. |
| Mixed with `number` | **Type error.** `decimal + number` does not compile. Prevents mixing exact and approximate. |
| Division | `decimal / decimal` → `decimal`. May produce arbitrary decimal places. Target field's `maxplaces` rejects if not rounded. Division by zero is a runtime error. |
| Comparison | Full ordering: `<`, `>`, `<=`, `>=`, `==`, `!=`. Cross-type comparison with `integer` widens to `decimal`. |
| Null | `nullable` allowed. Null is distinct from zero. |
| Default | Must satisfy all constraints: `decimal default 1.999 maxplaces 2` is a compile-time error. |

**The `maxplaces` constraint is a constraint, not a type parameter:**
- `maxplaces` fires on assignment, not during intermediate computation.
- `UnitPrice * Quantity` (both `decimal`) may produce a result with many decimal places. The assignment to a `maxplaces 2` field rejects unless the author explicitly calls `round(expr, 2)`.
- This is consistent with Precept's constraint philosophy: constraints declare what must be true; the engine rejects violations.

**Reference justification:**
- SQL `NUMERIC(p,s)`: precision and scale are type parameters, not constraints. Precept's approach is simpler — one `decimal` type with optional `maxplaces` constraint.
- PostgreSQL `MONEY`: a convenience type that bundles locale and rounding. [Community consensus: don't use it.](https://wiki.postgresql.org/wiki/Don't_Do_This#Don.27t_use_money) Validates `decimal + choice` over `money`.
- FEEL: single `number` backed by `BigDecimal`. All arithmetic is exact. No distinction between integer and decimal — simpler but loses semantic signal.
- Cedar `decimal` extension: fixed 4 decimal places. Simpler than `maxplaces` but inflexible.
- C# `System.Decimal`: 28–29 digits, banker's rounding default. Precept's backing type.

### Contract 7: `integer` means whole-number semantics, not "decimal with zero places"

**Rule:** `integer` is a distinct type for values that are semantically whole numbers. Backed by `System.Int64`.

| Property | Contract |
|----------|----------|
| Representation | .NET `System.Int64` (64-bit signed, range ±9.2 × 10¹⁸) |
| Literal form | Digit sequence without decimal point: `42`, `0`, `-7` |
| Integer division | Truncates toward zero: `5 / 2` → `2`, `-5 / 2` → `-2`. Matches C#. |
| Modulo | Follows dividend sign: `17 % 5` → `2`, `-17 % 5` → `-2`. Matches C#. |
| Mixed with `decimal` | Implicit widening: `integer → decimal`. Result is `decimal`. |
| Mixed with `number` | Implicit widening: `integer → number`. Result is `number`. |
| Assignment narrowing | `integer` field = `number` expr → **type error**. Requires explicit `truncate`/`floor`/`ceil`. |
| Boolean conversion | None. `when Count` is a type error. `when Count > 0` is required. |
| Constraints | `nonnegative`, `positive`, `min <N>`, `max <N>` from #13. `maxplaces` on integer is a compile-time error. |
| Collection inner type | `set of integer`, `queue of integer`, `stack of integer`. `.min`/`.max` return `integer`. |
| `.count` accessor | Returns `number` for backward compatibility (refinement to `integer` is a future option). |

**Why `integer` is not just `decimal maxplaces 0`:**
- Semantic readability: `field VisitCount as integer` communicates intent instantly. `field VisitCount as decimal maxplaces 0` communicates a precision constraint, not a domain fact.
- Division behavior: `integer / integer` truncates. `decimal / decimal` produces exact fractional results. These are different operations.
- Representation: `System.Int64` vs `System.Decimal` — different memory layout, different performance characteristics, different overflow behavior.
- Coercion asymmetry: `integer` widens to both `number` and `decimal`. `decimal maxplaces 0` widens only to `decimal` (not to `number`, since `decimal + number` is a type error).

**Reference justification:**
- SQL: `INTEGER` and `DECIMAL` are distinct types in the standard. `INTEGER` truncates on division; `DECIMAL` does not.
- BPMN/XSD: `xsd:integer` is a restriction of `xsd:decimal` in the type hierarchy but a distinct named type.
- C#: `int`/`long` and `decimal` are distinct types with distinct division behavior.
- Cedar: `Long` only. Validates that integer-as-primary-numeric is viable.
- FEEL: no integer type. Single `number`. Simplifies but loses the "this is a count" signal.

---

## Nullability Interaction

Each new type composes with `nullable` using the same semantics as existing types. No special null-propagation rules (unlike FEEL's `null + 1 = null`).

**Rule:** `nullable` is a modifier, not a type. `T nullable` means the field accepts `null` OR a value of type `T`. Null is not a member of `T`; it is an alternative to having a value.

| Scenario | Behavior |
|----------|----------|
| `choice("A","B") nullable` | Field holds one of `"A"`, `"B"`, or `null`. |
| `date nullable` | Field holds a date or `null`. |
| `decimal nullable maxplaces 2` | Field holds `null`, or a decimal with ≤ 2 places. `null` satisfies `maxplaces`. |
| `integer nullable nonnegative` | Field holds `null`, or a non-negative integer. |
| `nullable field == null` in guard | Narrows to "field is null" in the true branch; "field is not null" in the false branch. Existing `&&`-narrowing applies. |
| `nullable field.year` | Compile-time error unless guarded by `field != null`. Accessor requires non-null narrowing. |
| `nullable choice field > "Low"` | Compile-time error unless guarded. Ordering requires non-null narrowing. |

**Constraints on nullable fields:** Constraints (`nonnegative`, `positive`, `min`, `max`, `maxplaces`, `ordered`) apply only to non-null values. `null` always satisfies constraints — it is not a value that can violate them.

**Reference justification:**
- SQL: `NULL` satisfies `CHECK` constraints by SQL standard convention. Constraints are about valid values; NULL means "no value."
- C#: nullable value types (`int?`) — constraints on the underlying value apply only when `HasValue` is true.
- Cedar: no null at all. If an attribute might be absent, the policy must use `has` to test presence before access.

---

## Collection-Membership Implications

Each new type should be usable as a collection inner type.

| Declaration | Valid | Membership check | `.min` / `.max` |
|------------|-------|-----------------|-----------------|
| `set of choice("A","B","C")` | ✅ | `contains "A"` → valid; `contains "D"` → compile error | Not meaningful (no ordering without `ordered`) |
| `set of choice("A","B","C") ordered` | Design question | `contains "B"` → valid | Meaningful — `.min` = first declared, `.max` = last declared |
| `set of date` | ✅ | `contains date("2026-03-15")` → valid | `.min` = earliest date, `.max` = latest date |
| `set of decimal` | ✅ | `contains 3.14` — requires type compatibility | `.min` / `.max` return `decimal` |
| `set of integer` | ✅ | `contains 42` → valid | `.min` / `.max` return `integer` |
| `queue of date` | ✅ | N/A (queue has `.peek`, not `contains`) | N/A |
| `stack of integer` | ✅ | N/A (stack has `.peek`, not `contains`) | N/A |

**`add` and `remove` type checking:** For `set of choice(...)`, the `add` operand must be a compile-time-valid member of the choice set. `add MissingDocs "BirthCert"` is a compile-time error if `"BirthCert"` is not in the choice declaration. This extends the existing `add` type checking for `set of string` — the check is strictly tighter.

**Open design question for `set of choice(...) ordered`:** Should the `ordered` keyword apply to the choice declaration inside a collection type? If so, `.min` and `.max` acquire meaning. The domain survey does not show strong pressure for this — defer to proposal-level decision.

---

## Comparison Rules — Consolidated

The following table consolidates all comparison behavior across existing and proposed types. It is the single reference for what the type checker must enforce.

| Left type | Right type | `==`, `!=` | `<`, `>`, `<=`, `>=` |
|-----------|-----------|-----------|----------------------|
| `string` | `string` | ✅ | ❌ (no ordering on strings) |
| `number` | `number` | ✅ | ✅ |
| `boolean` | `boolean` | ✅ | ❌ |
| `integer` | `integer` | ✅ | ✅ |
| `integer` | `number` | ✅ (widens) | ✅ (widens) |
| `integer` | `decimal` | ✅ (widens) | ✅ (widens) |
| `decimal` | `decimal` | ✅ | ✅ |
| `decimal` | `number` | ❌ **type error** | ❌ **type error** |
| `number` | `decimal` | ❌ **type error** | ❌ **type error** |
| `date` | `date` | ✅ | ✅ (chronological) |
| `choice` | same `choice` type | ✅ | ✅ only with `ordered` |
| `choice` | different `choice` type | ❌ **type error** | ❌ **type error** |
| `choice` | `string` | ❌ **type error** | ❌ **type error** |
| Any type | different incompatible type | ❌ **type error** | ❌ **type error** |

**Note:** `null` comparisons use `== null` and `!= null` on any `nullable` type. `null < null` is not defined; ordering operators require non-null operands (enforced by nullable narrowing).

---

## Explicit Non-Goals for This Type-System Pass

These are deliberate exclusions grounded in the reference survey and Precept's product identity. Each has been considered and rejected with evidence.

### 1. `datetime` / `timestamp` / timezone semantics

Precept will not add any time-bearing temporal type in this pass. Timezone-aware comparison introduces ambient context (server timezone, user timezone, offset normalization) that directly violates deterministic inspectability. Cedar's `datetime` is offset-aware but operates in an authorization context where the evaluator controls the clock. Precept operates across entity lifecycles where the evaluator does not control the clock.

`date` (day-granularity, naive) is sufficient for the domain evidence. Time and duration types are explicitly deferred.

### 2. `money` as a scalar type

No system in the survey except PostgreSQL's deprecated `MONEY` type bundles amount, currency, rounding, and locale into one scalar. Industry practice is `decimal` for amount + `choice`/`enum` for currency code. Precept follows this pattern.

### 3. Parameterized types (`decimal(p,s)`, `integer(32)`, `choice<T>`)

Parameterized types would require a type-parameter syntax, generic resolution, and constraint propagation infrastructure that does not exist in the Precept parser or type checker. The proposed design uses constraint-zone annotations (`maxplaces`, `ordered`, `min`, `max`) to express the same restrictions at lower language complexity. This is consistent with Precept's configuration-like posture — constraints on declarations, not type algebra.

### 4. Records, maps, or structured types

These would shift Precept from flat, field-local entity declarations toward nested data-shape modeling. That is a category change with major parser, tooling, editability, and inspectability costs. The domain survey shows no sample pressure for nested structures — real entities have flat field sets.

### 5. `any` / dynamic typing / open payload slots

Dynamic escape hatches would defeat the type-system expansion's purpose. The entire point is making domain distinctions structurally enforceable. An `any` type is a loophole that makes every other type optional.

### 6. Host-language type leakage

Precept fields will not reference C# types, .NET enums, or SDK wrappers. One-file completeness means the `.precept` file declares all types it uses. MCP and inspect surfaces expose DSL-level values (strings for choices, ISO 8601 for dates, plain numbers for integer/decimal), not .NET runtime objects.

### 7. Implicit boolean conversion from numeric types

`when Count` (where `Count` is an integer) will be a type error. `when Count > 0` is required. This prevents the class of bugs where `0` is "falsy" — Precept does not inherit truthy/falsy semantics from any host language.

### 8. String ordering (`string < string`)

Lexicographic string comparison is not proposed. It is locale-dependent and semantically meaningless for the domains Precept governs. `choice ... ordered` provides domain-meaningful ordering for categorical values.

---

## Key References

Primary sources cited in this survey:

| Reference | URL | Used for |
|-----------|-----|----------|
| OMG DMN 1.4 Specification (FEEL) | https://www.omg.org/spec/DMN/1.4/PDF | Temporal types, single-number model, null semantics |
| Camunda FEEL Data Types | https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-data-types/ | FEEL type reference, date constructor |
| Cedar Language Reference | https://docs.cedarpolicy.com/policies/syntax-grammar.html | Zero-coercion policy, Long-only numeric, extension types |
| Cedar Operators | https://docs.cedarpolicy.com/policies/syntax-operators.html | Operator validity by type |
| Drools DRL Reference | https://docs.drools.org/latest/drools-docs/drools/language-reference/index.html | Java type inheritance, `declare enum` |
| NRules Wiki | https://github.com/NRules/NRules/wiki/Getting-Started | .NET type alignment |
| BPMN 2.0 Specification | https://www.omg.org/spec/BPMN/2.0.2/PDF | XSD type hierarchy, integer as decimal restriction |
| SQL:2016 Standard Part 2 | https://www.iso.org/standard/63556.html | Three-tier numeric tower, ENUM, DATE, coercion |
| PostgreSQL Numeric Types | https://www.postgresql.org/docs/current/datatype-numeric.html | INTEGER, NUMERIC, MONEY (deprecated) |
| PostgreSQL Enum Types | https://www.postgresql.org/docs/current/datatype-enum.html | Declaration-order comparison |
| PostgreSQL Date/Time Types | https://www.postgresql.org/docs/current/datatype-datetime.html | DATE semantics, DATE - DATE → integer |
| PostgreSQL "Don't Do This" — Money | https://wiki.postgresql.org/wiki/Don't_Do_This#Don.27t_use_money | MONEY type deprecation evidence |
| C# Numeric Types | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types | int/long semantics, widening rules |
| C# Decimal Type | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types | System.Decimal, MidpointRounding.ToEven |
| C# DateOnly | https://learn.microsoft.com/en-us/dotnet/api/system.dateonly | Day-granularity date in .NET |
| Pierce, *Types and Programming Languages* Ch. 8 | ISBN 978-0-262-16209-8 | Typed arithmetic expressions, safety theorems |
| Pierce, *TAPL* Ch. 15 | ISBN 978-0-262-16209-8 | Subtyping and coercion theory |
| Cardelli, "Type Systems" (CRC Handbook) | https://lucacardelli.name/Papers/TypeSystems.pdf | Type system classification, coercion taxonomy |

---

## Cross-References

- Domain-level research: [type-system-domain-survey.md](../expressiveness/type-system-domain-survey.md)
- Computed fields (recomputation timing interacts with type constraints): [computed-fields.md](../expressiveness/computed-fields.md)
- Expression evaluation (decidability of new types): [expression-evaluation.md](./expression-evaluation.md)
- Constraint composition (field-level constraints host `maxplaces`, `ordered`): [constraint-composition.md](./constraint-composition.md)
- Proposals: [#25](https://github.com/sfalik/Precept/issues/25) (choice), [#26](https://github.com/sfalik/Precept/issues/26) (date), [#27](https://github.com/sfalik/Precept/issues/27) (decimal), [#29](https://github.com/sfalik/Precept/issues/29) (integer)
