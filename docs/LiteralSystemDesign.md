# Literal System Design

> **This is the canonical design document for Precept's complete literal system.** It covers every way a value can appear in Precept source: numeric literals, boolean literals, null, string literals with interpolation, typed constants with interpolation, and list literals. The two-door model for non-primitive values (string `"..."` and typed constant `'...'`) and the interpolation syntax (`{expr}`) established here are language-level mechanisms — not features of any specific type domain.

**Author:** Frank (Lead/Architect, Language Designer)
**Date:** 2026-04-17
**Status:** Design — canonical reference
**Origin:** Design discussion and interpolation analysis in `.squad/decisions/inbox/frank-quoted-postfix-units.md` (v1–v3, culminating in the two-door model)

**Related artifacts:**
- **Temporal type system design:** [`docs/TemporalTypeSystemDesign.md`](TemporalTypeSystemDesign.md) — first inhabitants of Door 2
- **Language design:** [`docs/PreceptLanguageDesign.md`](PreceptLanguageDesign.md) — current language surface
- **Philosophy:** [`docs/philosophy.md`](philosophy.md) — design principles referenced throughout

---

## Summary

Precept's literal system defines **every way a value can appear in source**. The system has three layers, from simplest to most expressive:

| Layer | Syntax | Type resolution | Examples |
|-------|--------|----------------|----------|
| **Primitive literals** | Bare tokens — no delimiters | Type is intrinsic to the token | `42`, `3.14`, `true`, `false`, `null` |
| **String literals** (Door 1) | `"..."` | Always `string` | `"hello"`, `"Score: {Score}"` |
| **Typed constants** (Door 2) | `'...'` | Inferred from content shape → type family → narrowed by context | `'2026-04-15'`, `'30 days'` |
| **List literals** | `[...]` | List of the inner scalar type | `["a", "b"]`, `[1, 2, 3]` |

**Primitive literals** are the simplest value forms — numeric, boolean, and null tokens that appear bare in the source. Their type is determined entirely by the token itself. No delimiters, no type inference, no ambiguity.

**String literals** (`"..."`) produce `string` values with optional interpolation: `"Credit score {CreditScore} is below the 680 minimum"`. Interpolation uses `{expr}` syntax — always-on, no prefix required — because `{` has no other meaning in Precept's syntax.

**Typed constants** (`'...'`) produce non-primitive values with type inferred from content shape. The admission rule determines which types qualify: content shape determines a **type family** (a finite, enumerable set of types). Context narrows within the family. No shape may match two families. This formulation allows quantities — `'30 days'` maps to the family `{period, duration}`, narrowed by expression context — while preserving unambiguous type inference for shapes that map to singleton families like `{date}`.

**List literals** (`[...]`) are composite — comma-separated scalar values enclosed in brackets. Used exclusively in `default` clauses for collection fields.

For non-primitive values, the two-door model is **closed**. There is no Door 3. All non-primitive values — including quantities like `'30 days'` and `'{GraceDays} days'` — enter through Door 1 or Door 2. There are no bare postfix keywords, no constructor functions, no `type(value)` forms. Zero constructors exist in the language.

**Interpolation** uses the same `{expr}` syntax inside both doors. `"Amount is {Amount}"` substitutes the field value as a string. `'{GraceDays} days'` substitutes the expression value into the typed constant's content before shape matching. Interpolation supports any expression the language supports: field references, dot accessors, event args, and arithmetic.

### Why two doors, not three

The [temporal type system design](TemporalTypeSystemDesign.md) originally proposed a three-door model:

| Door | Original form |
|------|--------------|
| 1. String | `"..."` |
| 2. Typed constant | `'2026-01-15'` (formatted constants only) |
| 3. Quantity | `30 days`, `(GraceDays) days` (bare postfix keywords) |

Door 3 created three problems:

1. **Variable-quantity split.** Literal quantities (`30 days`) used Door 3's bare syntax, but variable quantities required a different form (`(GraceDays) days`). Two syntaxes for the same concept.
2. **Seven reserved words.** `days`, `hours`, `minutes`, `seconds`, `months`, `years`, `weeks` became language keywords — collision risk with field names and future reserved-word pressure on the language.
3. **Keyword explosion.** Every future quantity domain (currency, measurement, entity-scoped units) would need its own set of reserved words, growing the language's keyword footprint with each domain.

Interpolation inside `'...'` resolves all three: `'{GraceDays} days'` and `'30 days'` use the same Door 2 syntax. Unit names are validated strings inside `'...'`, not language keywords. Future quantity domains add validated string patterns, not keywords.

---

## Primitive Literals

Primitive literals are values that appear bare in the source — no delimiters required. The type is intrinsic to the token. These are the simplest atoms in Precept's expression language.

### Numeric literals

Numeric literals represent `number` values. The tokenizer recognizes integers, decimals, and scientific notation via a single pattern:

```
\d+(\.\d+)?([eE][+-]?\d+)?
```

All numeric forms produce a `NumberLiteral` token. The parser's `NumberAtom` combinator converts the token to a runtime value:

| Form | Syntax | Runtime type | Examples |
|------|--------|-------------|----------|
| Integer | Whole digits | `long` | `42`, `0`, `999` |
| Decimal | Digits with decimal point | `double` | `3.14`, `0.001`, `100.0` |
| Scientific | Exponential notation | `double` | `1.5e2`, `1e-5`, `3.0E+10` |

**Negative numbers** are not a single literal token — the parser handles the unary minus operator (`-`) followed by a `NumberLiteral`. In expression contexts, `-42` is parsed as `Minus` + `NumberAtom`. In default value clauses, the parser explicitly handles the `Minus` + `NumberLiteral` sequence to produce a negative value.

Numeric literals appear in guards, rules, set expressions, default values, and constraint arguments:

```precept
field MaxAttempts as number default 3
field InterestRate as number default 4.25

rule ClaimAmount >= 0 because "Claim amounts cannot be negative"

on Submit ensure Submit.Amount <= 100000 because "Amount cannot exceed $100,000"

from Open on Submit -> set Score = BaseScore + 10 -> no transition
```

### Boolean literals

`true` and `false` are language keywords that produce boolean values. The tokenizer maps them to `True` and `False` tokens via the keyword dictionary. The parser's `TrueAtom` and `FalseAtom` combinators produce `PreceptLiteralExpression(true)` and `PreceptLiteralExpression(false)` respectively.

```precept
field IsVerified as boolean default false
field FraudFlag as boolean default false

from Open on Approve -> set ApprovalReady = if IsVerified == true then true else false -> no transition
```

Booleans are the only literal type that is also a keyword — `true` and `false` are reserved words in the language.

### Null literal

`null` is a language keyword representing the absence of a value. The tokenizer maps it to a `Null` token. The parser's `NullAtom` combinator produces `PreceptLiteralExpression(null)`.

```precept
field OverrideReason as string nullable

on Approve ensure Approver != null because "Approver is required"
```

`null` is valid as a default value for any field type. In expressions, `null` participates in equality comparisons (`== null`, `!= null`) and is coerced to empty string `""` in string interpolation contexts.

### Primitive literal summary

| Literal | Token | Parser atom | Runtime value | Keyword? |
|---------|-------|-------------|---------------|----------|
| Integer | `NumberLiteral` | `NumberAtom` | `long` | No |
| Decimal | `NumberLiteral` | `NumberAtom` | `double` | No |
| Scientific | `NumberLiteral` | `NumberAtom` | `double` | No |
| `true` | `True` | `TrueAtom` | `bool (true)` | Yes |
| `false` | `False` | `FalseAtom` | `bool (false)` | Yes |
| `null` | `Null` | `NullAtom` | `null` | Yes |

---

## List Literals

List literals are composite values — comma-separated scalar literals enclosed in brackets. They are the only compound literal form in Precept.

```precept
field Tags as set of string default ["pending"]
field Scores as queue of number default [85, 90, 78]
field Flags as set of boolean default [true, false]
```

The parser's `ListLiteral` combinator matches `LeftBracket`, zero or more `ScalarLiteral` values separated by `Comma`, then `RightBracket`. Each element must be a scalar literal — numbers, strings, booleans, or null. Nested lists are not supported.

List literals appear exclusively in `default` clauses for collection fields (`set of T`, `queue of T`, `stack of T`). They are not valid in expression positions — you cannot write `set Tags = ["a", "b"]` in an event body. Collection mutation uses `add` and `remove` actions instead.

| Syntax | Element type | Examples |
|--------|-------------|----------|
| `[...]` with string elements | `string` | `["pending"]`, `["a", "b", "c"]` |
| `[...]` with numeric elements | `number` | `[1, 2, 3]`, `[85, 90, 78]` |
| `[...]` with boolean elements | `boolean` | `[true, false]` |
| `[]` | Empty list | `[]` |

---

## Door 1: String Literals (`"..."`)

### Static strings

```precept
reject "Approval requires verified documents"
rule ClaimAmount >= 0 because "Claim amounts cannot be negative"
set DecisionNote = "Standard tier — approved"
```

Static strings are unchanged from the current language. The `"..."` delimiter always produces `string`.

### Interpolated strings

```precept
reject "Credit score {CreditScore} is below the 680 minimum"
reject "Outstanding documents: {MissingDocuments.count} remaining"
reject "Amount {Approve.Amount} exceeds claim maximum of {ClaimAmount}"
rule DueDays >= 0 because "Grace period of {DueDays} days cannot be negative"
```

Interpolation uses `{expr}` inside `"..."` — the expression is evaluated, coerced to string, and substituted inline. Multiple interpolations can appear in a single string.

**Why interpolation matters:** Every reject message in the 25 sample files is a static string. Authors cannot communicate instance-specific diagnostic information — the engine knows *why* a rejection occurred but cannot include the specific values in the message. For a product whose core value is inspectability (Principle #1), this is a genuine gap. `reject "Credit score {CreditScore} is below the 680 minimum"` exposes the data that caused the outcome.

### Escape sequences

| Escape | Produces | Example |
|--------|---------|---------|
| `\"` | Literal `"` | `"She said \"hello\""` |
| `\\` | Literal `\` | `"Path: C:\\temp"` |
| `{{` | Literal `{` | `"Use {{braces}} for grouping"` |
| `}}` | Literal `}` | `"Use {{braces}} for grouping"` |

`{{` and `}}` are the interpolation escape — doubling the delimiter produces the literal character. `"Section (a)"` is literal text with no ambiguity, because `(` is not an interpolation trigger. `"Use {Field}"` is interpolation. `"Use {{braces}}"` produces the literal text `"Use {braces}"`.

### Type coercion for string interpolation

When an expression inside `{...}` evaluates to a non-string type, it is coerced to string using a deterministic, locale-invariant rule:

| Expression type | String representation | Example |
|---|---|---|
| `string` | As-is | `{Name}` → `"John"` |
| `number` | Numeric string | `{Amount}` → `"1234.56"` |
| `integer` | Integer string | `{Score}` → `"680"` |
| `decimal` | Decimal string | `{Rate}` → `"4.25"` |
| `boolean` | `"true"` / `"false"` | `{FraudFlag}` → `"true"` |
| `date` | ISO 8601: `YYYY-MM-DD` | `{DueDate}` → `"2026-04-15"` |
| `time` | `HH:MM:SS` | `{StartTime}` → `"14:30:00"` |
| `datetime` | ISO 8601 | `{DetectedAt}` → `"2026-04-15T14:30:00"` |
| `instant` | ISO 8601 with Z | `{FiledAt}` → `"2026-04-15T14:30:00Z"` |
| `zoneddatetime` | ISO 8601 with zone bracket | `{Context}` → `"2026-04-15T14:30:00[America/New_York]"` |
| `timezone` | IANA identifier | `{Tz}` → `"America/New_York"` |
| `duration` | NodaTime canonical string | `{Elapsed}` → `"PT72H"` |
| `period` | NodaTime canonical string | `{Grace}` → `"P30D"` |
| `null` | Empty string `""` | `{NullableField}` → `""` |
| Collection | **Compile error** | `{Items}` → error: "Collections cannot be interpolated directly. Use `{Items.count}` for the count." |

**Invariant culture:** All coercions use invariant culture formatting. `{Amount}` always produces `"1234.56"`, never `"1.234,56"`. Same `.precept` file, same output everywhere. This aligns with Principle #1 (deterministic model) — the interpolation result is the same regardless of the host system's locale.

---

## Door 2: Typed Constants (`'...'`)

### The admission rule — type families

A type qualifies for Door 2 (`'...'`) **if and only if** its content shape determines a **type family** — a finite, enumerable set of types — that is disjoint from every other family. Context narrows within the family. No shape may match two families.

This formulation evolved from the original "self-identifying property," which required each shape to map to exactly one type. The type-family formulation:

1. **Preserves shape uniqueness at the family level.** Every content shape maps to exactly one family. `'2026-04-15'` → `{date}`. `'30 days'` → `{period, duration}`. These families are disjoint — no content can belong to two families simultaneously.
2. **Allows context-dependent narrowing within a family.** Within a family, expression context selects the specific type. `'30 days'` is `period` in `date +` context, `duration` in `instant +` context. The narrowing is bounded — the family is enumerable.
3. **Bounds the ambiguity.** The family is finite and small. The type checker can verify that every usage context selects exactly one member. No open-ended inference, no non-local reasoning.
4. **Preserves compile-time safety.** If no context narrows the family, it is a compile error: *"Can't determine the type of `'30 days'` without context. Use it in an expression like `DueDate + '30 days'` so the type is clear."*

**Formal criterion:** For any proposed new type `T` to enter Door 2, define its content shape `S(T)`. Compute the type family `F(S(T))`. Verify that `F(S(T))` is disjoint from every existing family `F(S(T'))` for all current inhabitants `T'`. If the disjointness check fails, the type cannot enter Door 2 — it must use a different mechanism or its shape must be refined.

### Current inhabitants and their families

| Inhabitant | Shape signal | Type family | Context narrowing |
|------------|-------------|-------------|-------------------|
| State names | Plain identifier (no digits-first, no special chars) | `{state}` | Validated against precept's state declarations |
| Dates | `YYYY-MM-DD` (digits + hyphens, no `T`) | `{date}` | Singleton — no narrowing needed |
| Times | `HH:MM:SS` or `HH:MM` (digits + colons, no hyphen-before-digits) | `{time}` | Singleton — no narrowing needed |
| DateTimes | `...T...` (contains `T`, no trailing `Z`, no `[`) | `{datetime}` | Singleton — no narrowing needed |
| Instants | `...T...Z` (contains `T`, trailing `Z`) | `{instant}` | Singleton — no narrowing needed |
| ZonedDateTimes | `...T...[zone]` (contains `T`, bracket-enclosed timezone) | `{zoneddatetime}` | Singleton — no narrowing needed |
| Timezones | `Word/Word` (alpha + `/`, no digits-first) | `{timezone}` | Singleton — no narrowing needed |
| Quantities | `<value> <unit-word>` or `<value> <unit> + <value> <unit>` | `{period, duration}` | `date ±` → `period`, `instant ±` → `duration`, field type → match declared type |

### Shape disambiguation table

The parser distinguishes typed constant inhabitants by content shape using the following precedence:

| Priority | Shape test | Inhabitant | Examples |
|----------|-----------|------------|---------|
| 1 | Contains `T` + ends with `[...]` | `zoneddatetime` | `'2026-04-15T14:30:00[America/New_York]'` |
| 2 | Contains `T` + ends with `Z` | `instant` | `'2026-04-15T14:30:00Z'` |
| 3 | Contains `T` (no `Z`, no `[`) | `datetime` | `'2026-04-15T14:30:00'` |
| 4 | Matches `YYYY-MM-DD` | `date` | `'2026-04-15'` |
| 5 | Matches `HH:MM(:SS)?` | `time` | `'14:30:00'`, `'14:30'` |
| 6 | Contains `/` (IANA pattern) | `timezone` | `'America/New_York'` |
| 7 | Matches `<value> <unit-word>` pattern | quantity | `'30 days'`, `'{GraceDays} days'`, `'2 years + 6 months'` |
| 8 | Plain identifier | state name | `'Open'`, `'UnderReview'` |

Shapes are tested in priority order. No shape can match two rows because the distinguishing signals are structurally disjoint: `T`, `Z`, `[`, hyphens-in-date-pattern, colons-in-time-pattern, `/` in IANA identifiers, and `<number> <word>` in quantities are all mutually exclusive signals.

### Static typed constants

```precept
field DueDate as date default '2026-06-01'
field AppointmentTime as time default '09:00:00'
field IncidentTimestamp as instant default '2026-04-15T14:30:00Z'
field ScheduledFor as datetime default '2026-04-13T09:00:00'
field ClinicTimezone as timezone default 'America/New_York'

field GracePeriod as period default '30 days'
field SlaLimit as duration default '72 hours'
field LoanTerm as period default '12 months'
field WarrantyLength as period default '2 years'
field ExtendedWarranty as period default '2 years + 6 months'
field EstimatedHours as duration default '8 hours'
```

### Interpolated typed constants

```precept
set DueDate = FiledAt + '{GraceDays} days'
set SlaLimit = CreatedAt + '{SlaHours * 2} hours'
set Expiry = StartDate + '{TermMonths + 6} months'
```

Interpolation inside `'...'` uses the same `{expr}` syntax as strings. The expression is evaluated, the result is substituted into the content, and then the full content is shape-matched to determine the type family.

**Example evaluation:** `'{GraceDays} days'` where `GraceDays` is `30`:

1. Evaluate `GraceDays` → `30`
2. Coerce to string → `"30"`
3. Substitute → content becomes `30 days`
4. Shape match → quantity pattern → family `{period, duration}`
5. Context narrow → `period` (in `date +` context) or `duration` (in `instant +` context)

### Combined quantities

```precept
field ExtendedWarranty as period default '2 years + 6 months'
set Expiry = StartDate + '1 year + 3 months + 15 days'
rule FiledAt - IncidentAt <= '72 hours + 30 minutes' because "SLA window"
```

The `+` operator inside `'...'` combines quantity components. All components must resolve to the same type family member.

**Left-associative `+` subtlety:** `date + '1 month' + '1 month'` applies sequential truncation (Jan 31 → Feb 28 → Mar 28). `date + '1 month + 1 month'` builds the period first, then applies once (Jan 31 → Mar 31). Both are valid. The `+` inside `'...'` builds a single compound quantity; the `+` outside `'...'` is sequential arithmetic.

### Quantity unit names

The following unit names are recognized inside typed constants:

| Unit | Calendar/Timeline | Always-period | Always-duration | Context-dependent |
|------|-------------------|---------------|-----------------|-------------------|
| `years` | Calendar | ✓ | | |
| `months` | Calendar | ✓ | | |
| `weeks` | Both | | | ✓ |
| `days` | Both | | | ✓ |
| `hours` | Timeline | | | ✓ |
| `minutes` | Timeline | | | ✓ |
| `seconds` | Timeline | | | ✓ |

**These are NOT language keywords.** They are validated strings inside `'...'`. This means:

- `days` can be used as a field name, event name, or event arg name without collision
- Future quantity domains add new validated strings, not new keywords
- The parser does not need to reserve these words in the global keyword table
- No breaking change to existing precepts that use these words as identifiers

### Type resolution rules for quantities

The type of a quantity typed constant is determined by expression context. `'30 days'` is not inherently `period` or `duration` — the surrounding expression resolves it.

**For `days` and `weeks` — context-dependent:**

| Expression context | `'30 days'` / `'{n} days'` resolves to | NodaTime call |
|---|---|---|
| `date ± _` | `period` | `Period.FromDays(30)` / `Period.FromDays(n)` |
| `datetime ± _` | `period` | `Period.FromDays(30)` / `Period.FromDays(n)` |
| `instant ± _` | `duration` | `Duration.FromDays(30)` / `Duration.FromDays(n)` |
| `zoneddatetime ± _` | `duration` | `Duration.FromDays(30)` / `Duration.FromDays(n)` |
| `time ± _` | **compile error** | "Days/weeks don't apply to a time — times have no date component." |
| `field X as period default _` | `period` | `Period.FromDays(30)` |
| `field X as duration default _` | `duration` | `Duration.FromDays(30)` |
| No context / ambiguous | **compile error** | "Can't determine the type of `'30 days'` without context." |

**For `months` and `years` — always `period`:** No `Duration.FromMonths` or `Duration.FromYears` exists in NodaTime — months and years have variable nanosecond length. `months` and `years` ALWAYS resolve to `period` regardless of expression context. `instant + '3 months'` is a type error: the quantity resolves to `period`, and `instant ± period` is not allowed.

**For `hours`, `minutes`, `seconds` — context-dependent (with `period` bridging):**

| Expression context | `'3 hours'` / `'{n} hours'` resolves to | NodaTime call |
|---|---|---|
| `instant ± _` | `duration` | `Duration.FromHours(3)` |
| `zoneddatetime ± _` | `duration` | `Duration.FromHours(3)` |
| `time ± _` | bridges to duration | Sub-day bridging (see temporal design Decision #16) |
| `datetime ± _` | `period` | `Period.FromHours(3)` (see temporal design Decision #9/#27) |
| `field X as duration default _` | `duration` | `Duration.FromHours(3)` |
| `field X as period default _` | `period` | `Period.FromHours(3)` |
| `date ± _` | **compile error** | "Hours don't apply to a date — dates have no time of day." |
| No context / ambiguous | **compile error** | "Can't determine the type of `'3 hours'` without context." |

### Integer requirement for quantity values

The magnitude in a quantity typed constant must evaluate to an integer. `'0.5 days'` is a compile error: *"Unit values must be whole numbers. Use smaller units for fractions: `'12 hours'` for half a day."* This matches NodaTime's `Period.FromDays(int)` and related factory methods. See temporal design Decision #28.

**Scope:** This restriction applies to temporal unit names. Future non-temporal quantity domains (e.g., currency with `'100.50 USD'`) may support non-integer magnitudes if their backing types accept them. The restriction is per-domain, not per-mechanism.

---

## Context Resolution Algorithm

When the type checker encounters a typed constant like `'30 days'`, the content shape maps to a type family (`{period, duration}`) but does not uniquely identify the type. The surrounding expression provides **context** that narrows the family to a single member. This section documents how that context flows and how the type checker uses it.

### Context sources

Context flows **inward** from the nearest enclosing typed node to the typed constant. The type checker already holds the full expression tree — this is standard top-down type inference, not a new mechanism.

| Position | Context source | Resolved from |
|----------|---------------|---------------|
| Right operand of `+`/`-` | Left operand's resolved type | `FiledAt + '30 days'` → left is `instant` → context = `instant` |
| `default` clause | Declared field type | `field GracePeriod as period default '30 days'` → context = `period` |
| `set X = Y + '...'` | Type of `Y` | `set DueDate = FiledAt + '30 days'` → context from `FiledAt` |
| Comparison (`<=`, `>=`) | Other operand's type or subtraction result | `FiledAt - IncidentAt <= '72 hours'` → subtraction result type provides context |
| Compound chain | Result type of the preceding sub-expression | `StartDate + '1 month' + '15 days'` → second constant gets context from result of first addition |

### Concrete walkthrough: `set DueDate = FiledAt + '30 days'`

Each step the type checker takes:

1. **Resolve `FiledAt`** — field lookup → type `instant`
2. **Operator `+`, left type `instant`** — emit context signal: right operand expects a quantity compatible with `instant ±`
3. **Encounter `'30 days'`** — shape match → quantity pattern → family `{period, duration}`
4. **Apply context** — look up `instant ±` in the temporal resolution table → narrows to `duration`
5. **Emit** `Duration.FromDays(30)`
6. **Result of `FiledAt + Duration.FromDays(30)`** → `instant`
7. **Assign to `DueDate`** — `DueDate` declared as `instant` → consistent ✓

### Compound expressions chain context left-to-right

`StartDate + '1 month' + '15 days'`:

1. `StartDate` → `date`
2. `date + '1 month'` → months always `period` → result type `date`
3. Result type `date` becomes context for `+ '15 days'` → `date ±` → `period` → `Period.FromDays(15)`
4. `date + Period.FromDays(15)` → `date` ✓

Each `+` node resolves strictly from its left operand. No look-ahead required.

### When context is absent or ambiguous

If no typed node provides context, the type checker emits a compile error at the typed constant site:

```
'30 days' cannot be resolved without context.
Use it in an expression — e.g., 'set DueDate = FiledAt + '30 days'' — so the type can be determined.
```

### Context is the type checker's job — no new mechanism needed

The type checker already builds the full expression tree before resolving leaf types. Context resolution is a table lookup keyed on the enclosing node's type. Adding context resolution for a new quantity family means adding a resolution table — not modifying the resolver's architecture.

```
ExpressionTree → TypeChecker.Resolve(node, contextType?)
  node         = TypedConstant('30 days')
  contextType  = instant   (from parent BinaryOp's left operand)
  family       = {period, duration}
  lookup       = instant ± → duration
  emit         = Duration.FromDays(30)
```

### Unit restrictions belong to constraints, not the literal system

The literal system determines **which type** a quantity constant produces. Business rules about **which values are valid** — e.g., a grace period that must be expressed in whole days or larger, not hours — are the job of constraint modifiers on the field declaration:

```precept
field GracePeriod as period
  min '1 day'
```

The `min '1 day'` constraint enforces the granularity invariant at runtime. IntelliSense may filter offered unit completions based on the declared type and active constraints — that is a tooling concern. The literal system does not need a unit-restriction mechanism; constraints handle it.

### Future UOM extensibility

The admission rule's disjointness criterion is the extensibility gate. A future quantity category (length, currency, weight) requires three things before entering Door 2:

| Requirement | Purpose |
|-------------|---------|
| A shape pattern (e.g., `'<value> <ISO-4217-code>'`) | Identifies the family in the parser |
| A disjointness proof against all existing families | Ensures no content shape is ambiguous |
| A context resolution table for that domain | Maps expression context → specific type within the family |

The two-door model and admission rule were designed to be domain-agnostic. A new domain adds rows to the shape-disambiguation table and a new resolution table — no language changes, no new keywords, no new doors.

### Design review note — George's B1

This section directly addresses George's blocker **B1** from the design review, which asked for *"a context-dependent resolution algorithm sketch for how `'30 days'` determines its type given a usage position."* The algorithm and walkthrough above document that mechanism. The temporal-specific resolution tables (period vs. duration by operator context; always-period for months/years) were already in the doc above. B1 is partially resolved by this section; the full picture is the algorithm here plus those tables.

---

## Interpolation Syntax

### Delimiter: `{expr}`

Interpolation uses curly braces `{expr}` inside both `"..."` and `'...'`:

```precept
# String interpolation — Door 1
reject "Credit score {CreditScore} is below the 680 minimum"
reject "Amount {Approve.Amount} exceeds claim maximum of {ClaimAmount}"

# Typed constant interpolation — Door 2
set DueDate = FiledAt + '{GraceDays} days'
set SlaLimit = CreatedAt + '{SlaHours * 2} hours'
```

### Always-on — no prefix needed

Unlike C# (`$"..."`), Python (`f"..."`), or Kotlin (`"$expr"`), Precept does not require a prefix to enable interpolation. `{` inside any quoted literal is always the interpolation trigger. `{{` produces a literal `{`.

**Why no prefix is safe in Precept:** Every surveyed language requires an opt-in prefix because `{` (or `(` or `$`) has other meanings in the language. In C#, `{` starts a code block; in Python, `{` starts a dict/set literal; in JavaScript, `{` starts an object literal. In Precept, **`{` has no meaning outside of quoted literals.** Precept uses keywords for structure (Principle #3: "No colons, no curly braces, no semicolons"). There is zero ambiguity at the tokenizer level: inside a quoted context, `{` is always interpolation start.

This is a unique structural advantage of Precept's keyword-anchored design. The "no curly braces" principle (Principle #3) was a deliberate syntax choice for structure and readability — and it creates the unexpected benefit that `{` is available as an unambiguous interpolation trigger without any prefix ceremony.

### Why `{expr}` over alternatives

| Alternative | Why rejected |
|---|---|
| `(expr)` inside strings | **Catastrophic ambiguity.** Parentheses are ubiquitous in business text: `"Call (555) 123-4567"`, `"See section (a)"`, `"Per CFR §42(b)(3)"`. Zero languages use bare `()` for interpolation. Business domains — Precept's primary audience — routinely use parenthetical text in rules, regulations, and rejection messages. |
| `$"..."` prefix (C#-style) | Adds ceremony. Two string token types in the tokenizer. Authors must remember to add `$` — if they forget, interpolation silently produces literal text. A "forgot the prefix" bug class. |
| `\(expr)` (Swift-style) | `\` is already the escape character in `"..."` (`\"` for literal quote). Overloads escape semantics. Awkward inside `'...'` typed constants. |
| `$(expr)` | Burns `$` as a language character. Dollar sign has natural meaning in business domains (currency). |
| `[expr]` | Collision risk with future collection literal syntax. Brackets are used for IANA timezone zones inside typed constants (`[America/New_York]`). |
| No interpolation | Leaves the diagnostic-specificity gap in reject/because messages permanently open. |

### Precedent survey — interpolation delimiters

| Language | Interpolation syntax | Opt-in | Always-on? |
|---|---|---|---|
| C# | `$"text {expr} text"` | `$` prefix on string | No |
| Python | `f"text {expr} text"` | `f` prefix on string | No |
| Kotlin | `"text ${expr} text"` | `$` prefix on expression | No |
| Ruby | `"text #{expr} text"` | `#` prefix on expression | No |
| JavaScript | `` `text ${expr} text` `` | `$` + backtick string | No |
| Swift | `"text \(expr) text"` | `\` prefix on expression | No |
| Scala | `s"text ${expr} text"` | `s` prefix on string | No |
| Perl | `"text ${var} text"` | Always-on in `"..."` | Yes — uses `${}` |
| PHP | `"text {$var} text"` | Always-on in `"..."` | Yes — uses `{$}` |
| **Precept** | `"text {expr} text"` | **Always-on** | **Yes — unique: `{` is free** |

Precept is the only language that uses bare `{expr}` without a prefix and without ambiguity. This is possible solely because `{` is not a structural token in the language.

### Expression subset for interpolation

Interpolation supports any expression the language supports:

| Category | Examples |
|----------|----------|
| Field references | `{FieldName}` |
| Dot accessors | `{Items.count}`, `{DueDate.year}` |
| Event args | `{Approve.Amount}` |
| Arithmetic | `{AnnualIncome * 3}`, `{Score + Bonus}`, `{SlaHours * 2}` |

**Not supported:** Conditional expressions inside interpolation (`{if X then Y else Z}`) — nested quoted literals inside `{...}` create parsing ambiguity with the `}` interpolation terminator. This is a grammar constraint, not a phasing choice.

### Philosophy alignment

- **Principle #1 (Deterministic, inspectable model):** Interpolated messages expose the data that caused outcomes. `reject "Amount {Amount} exceeds limit"` is more inspectable than `reject "Amount exceeds limit"` because the caller sees the specific value.
- **Principle #2 (English-ish but not English):** `"Credit score {CreditScore} is below minimum"` reads as a template sentence. Natural for domain authors — closer to how a business analyst would phrase a rejection message.
- **Principle #3 (No curly braces — for structure):** The "no curly braces" principle applies to structural code — blocks, scoping, grouping. `{expr}` inside quoted literals is a template marker, not a structural element. The absence of `{` from structure is precisely what makes it available for interpolation.
- **Principle #8 (Sound analysis):** Interpolated expressions are type-checked at compile time. `{Items}` (collection type) is a compile error — not a runtime formatting failure. `{NonExistentField}` is a compile error — not a runtime null reference. The static analysis guarantee extends inside interpolation.
- **Principle #12 (AI-first):** One interpolation syntax, one delimiter, always-on. An AI authoring a precept does not need to decide "should I use `$` or not" — `{expr}` works in every quoted context. The deterministic, prefix-free syntax is optimized for reliable AI generation.

---

## Locked Design Decisions

### L1. Two-door model — string + typed constant, no bare postfix keywords

- **Why:** The two-door model eliminates Door 3 (bare postfix quantities like `30 days`, `(GraceDays) days`). All quantities enter through Door 2 as typed constants: `'30 days'`, `'{GraceDays} days'`. This solves three problems simultaneously: (1) the variable-quantity split — literal and variable quantities use the same `'...'` syntax, (2) seven reserved words eliminated — `days`, `hours`, etc. are validated strings inside `'...'`, not keywords, (3) keyword explosion — future quantity domains add validated string patterns, not keywords. The three-door model was architecturally sound but created scaling problems that the two-door model resolves structurally.
- **Alternatives rejected:** (A) Three-door model with bare postfix — the original temporal proposal design. Readable (`30 days` reads as English) but creates the variable-quantity split, reserves 7 keywords, and each future quantity domain adds more keywords. (B) Three-door model with quoted quantities as supplement — redundant, two syntaxes for the same concept. (C) Constructor functions (`days(30)`, `hours(72)`) — eliminated earlier: function-call syntax doesn't read as English, creates dual syntax, and the fixed return type of `days(n) → period` cannot serve both `period` and `duration` contexts. (D) Keep Door 3 with interpolation-enabled paren postfix — `(GraceDays) days` is bare postfix with keywords; interpolation in Door 2 makes it unnecessary.
- **Precedent:** SQL uses `'...'` for all non-numeric literals including dates, intervals, and identifiers. INTERVAL syntax in PostgreSQL: `INTERVAL '30 days'`. No language surveyed uses both bare postfix keywords and quoted constants for the same value category.
- **Tradeoff:** `'30 days'` is 3 characters longer than `30 days` and requires quotes. The readability cost is marginal — field declarations and expressions still read naturally: `field GracePeriod as period default '30 days'`, `set DueDate = FiledAt + '30 days'`. The benefits (zero keyword reservation, unified syntax for literal/variable/computed quantities, scalable to future domains) outweigh the noise. Every quantity the author writes is explicitly marked as a typed constant, reinforcing the two-door model's conceptual clarity.

### L2. `{expr}` interpolation — always-on, no prefix, curly braces

- **Why:** `{` has no structural meaning in Precept's syntax. Precept uses keywords for structure (Principle #3: "No colons, no curly braces, no semicolons"), so `{` is unambiguously available as an interpolation trigger inside quoted literals. No prefix (`$`, `f`, `\`) is needed because there is no ambiguity to resolve — the tokenizer knows `{` inside a quoted context is always interpolation start. This is a unique structural advantage that no other surveyed language possesses, because every other language uses `{` (or `(`) for structural purposes.
- **Alternatives rejected:** (A) `(expr)` — catastrophic ambiguity with parenthetical text in business strings. Zero surveyed languages use bare `()` for interpolation. Parentheses are the most common grouping mechanism in natural language; business rules, legal text, and regulatory citations use `(...)` pervasively. (B) `$"..."` prefix (C#-style) — adds ceremony, creates two string token types, introduces "forgot the prefix" bugs where interpolation silently becomes literal text. (C) `\(expr)` (Swift-style) — overloads escape character semantics; `\` already means escape-next-character in `"..."`. (D) `$(expr)` — burns `$` as a language character; dollar sign has business meaning (currency). (E) No interpolation — leaves the diagnostic-specificity gap in reject/because messages.
- **Precedent:** C#, Python, Kotlin, Rust all use `{expr}` or `${expr}` inside strings. Perl and PHP have always-on interpolation in `"..."` using `$`-prefixed expressions. Precept is unique in using bare `{expr}` without a prefix — justified solely by the fact that `{` is not a language token.
- **Tradeoff:** `{{` must be used for literal `{` in strings. In practice, curly braces in business text are rare — far rarer than parentheses (which is why `(expr)` was rejected and `{expr}` was chosen). The escape syntax `{{` is standard across C#, Python, and Rust.

### L3. Interpolation inside both `"..."` and `'...'` — same mechanism, two doors

- **Why:** String interpolation (`"Amount {Amount}"`) serves diagnostics — reject messages, because reasons, rule messages need instance-specific values. Typed constant interpolation (`'{GraceDays} days'`) serves computation — variable quantities, computed temporal values need expression evaluation inside the constant. Both are independently valuable and together they eliminate the variable-quantity split that motivated Door 3. The same `{expr}` syntax in both doors means one mechanism to learn, one parser implementation, one expression evaluation path.
- **Alternatives rejected:** (A) String interpolation only, no typed constant interpolation — leaves the variable-quantity split unsolved. `'30 days'` works for literal quantities but variable quantities (`GraceDays` days) would need a different mechanism, re-creating Door 3. (B) Typed constant interpolation only, no string interpolation — loses the diagnostic-specificity capability; reject messages remain static. (C) Different interpolation syntax per door — two mechanisms to learn, two parser paths, unnecessary complexity for identical semantics.
- **Precedent:** No direct precedent for interpolation inside typed constants (this is novel to Precept's literal model). String interpolation is universal. The novelty is bounded — typed constant interpolation shares the exact same mechanism as string interpolation, applied to a different content context.
- **Tradeoff:** Typed constant interpolation transforms `'...'` from a passive content container into a template evaluator. The tokenizer must recognize `{` inside `'...'` and switch to expression mode. The type checker must evaluate interpolation expressions, substitute results, then shape-match the reconstructed content. This adds complexity, but the complexity is shared with string interpolation — one implementation serves both doors.

### L4. Type-family admission rule for typed constants

- **Why:** The original admission rule (the "self-identifying property") required each content shape to map to exactly one type. Quantities violate this — `'30 days'` is `period` in date context, `duration` in instant context. The type-family formulation preserves the key guarantee (no shape matches two unrelated type families) while allowing context-dependent narrowing within a family. `'30 days'` maps to the family `{period, duration}` — a finite, enumerable set. The type checker verifies every context selects exactly one family member. The family formulation is strictly weaker than "one shape = one type" but strictly stronger than "anything goes" — it bounds the ambiguity to a compile-time-checkable, finite set.
- **Alternatives rejected:** (A) Strict self-identifying property — blocks quantities from Door 2 entirely. Requires Door 3 (bare postfix) or constructor functions. Forces the variable-quantity split. (B) Fully context-dependent resolution with no family constraint — unbounded inference, type errors become non-local and confusing ("why did the compiler pick duration here?"). (C) Separate door for quantities — Door 3, which creates the three problems described in the two-door rationale (L1).
- **Precedent:** NodaTime's own type system uses context to select between `Period.FromDays(n)` and `Duration.FromDays(n)` for the same unit. The family `{period, duration}` mirrors NodaTime's factory families for `days` and `weeks`.
- **Tradeoff:** The admission rule is more complex than "one shape = one type." Evaluating a new type's admission requires proving its family is disjoint from all existing families. The complexity is bounded — families are finite sets, disjointness is a compile-time-checkable property, and the number of families grows slowly (one per domain, not one per type).

### L5. Interpolation supports the full expression language

- **Why:** Interpolation expressions (`{expr}`) use the same expression evaluator as the rest of the language — field references, dot accessors, event args, and arithmetic. `{Amount}` for field values, `{Items.count}` for collection counts, `{Approve.Amount}` for event args, `{SlaHours * 2}` for computed values. The grammar handles `{` → evaluate expression → `}` uniformly. Operator precedence inside `{...}` is the same as anywhere else in the language — the `}` interpolation terminator is unambiguous because `}` is not an operator.
- **Alternatives rejected:** (A) Field references only — too restrictive. `{Items.count}` and `{SlaHours * 2}` are immediate needs for diagnostics and computed quantities; excluding them would force authors to create intermediate fields for every accessor or arithmetic expression. (B) Conditional expressions inside interpolation (`{if X then Y else Z}`) — nested quoted literals inside `{...}` create parsing ambiguity. This is excluded as a grammar constraint.
- **Precedent:** C# string interpolation supports full expressions. Python f-strings support full expressions. Precept follows the same model.
- **Tradeoff:** The `}` terminator must be disambiguated from `>=` comparisons if comparisons are ever allowed inside interpolation. Currently not an issue — comparisons return `boolean`, which is not useful in interpolation contexts. If needed, the precedent is C# which handles this with the same `}` terminator.

### L6. Escape sequences: `\"`, `\\`, `{{`, `}}`

- **Why:** `\"` and `\\` are the existing string escape sequences (unchanged). `{{` and `}}` are the standard interpolation escape for curly-brace interpolation — doubling the delimiter produces the literal character. This is the same convention used by C#, Python, and Rust. Four escape sequences total; no new escape characters introduced.
- **Alternatives rejected:** (A) No escape for `{` — makes literal `{` impossible to write in strings. (B) `\{` instead of `{{` — overloads backslash with two meanings (escape-next-character AND interpolation-escape), inconsistent with C#/Python/Rust precedent. (C) Raw string mode (R-prefix or similar for non-interpolated strings) — adds a third string variant; unnecessary when `{{` covers the rare case cleanly.
- **Precedent:** C# (`{{` for literal `{` in interpolated strings), Python f-strings (`{{`), Rust `format!` (`{{`). Universal standard for curly-brace interpolation.
- **Tradeoff:** None significant. Curly braces in business text are rare and `{{` is familiar from other languages.

### L7. State names as typed constants — `'Open'` validated against declarations

- **Why:** State names inside `'...'` are validated against the precept's state declarations at compile time. `'Open'` in a context expecting a state name is type-checked — `'NotAState'` is a compile error. This keeps all non-primitive constants under the typed constant delimiter, consistent with the two-door model. The shape (plain identifier without digits-first or special characters) maps to the type family `{state}`, narrowed by validation against declared states.
- **Alternatives rejected:** (A) State names as bare identifiers (no quotes) — parser ambiguity with field references in expression positions. `Status == Open` — is `Open` a field name or a state name? (B) State names as double-quoted strings — no compile-time validation; `"NotAState"` would be a valid string. (C) State names without quotes only in specific syntactic positions — inconsistent; sometimes quoted, sometimes not.
- **Precedent:** The existing temporal proposal already uses `'...'` for typed constants. Extending it to state names is consistent with the admission rule.
- **Tradeoff:** `'Open'` is slightly noisier than a hypothetical bare `Open` for state comparisons. The compile-time validation and consistency with the two-door model justify the quotes.

---

## Forward Design — Expansion Joints

### Future quantity domains

The two-door model with `{expr}` interpolation provides expansion joints for future quantity domains without new grammar concepts:

| Domain | Literal form | Interpolated form | Unit validation source |
|--------|-------------|-------------------|------------------------|
| **Temporal** (current) | `'30 days'` | `'{GraceDays} days'` | NodaTime unit names |
| **Currency** (future) | `'100 USD'` | `'{Subtotal} USD'` | ISO 4217 registry |
| **Percentage** (future) | `'10 percent'` | `'{Rate} percent'` | Single unit |
| **Physical** (future) | `'5 kg'` | `'{Weight} kg'` | UCUM subset |
| **Entity-scoped** (future) | `'24 each'` | `'{Qty} each'` | Precept `units` block |

Each domain adds validated string patterns to the typed constant shape matcher — no new keywords, no new grammar, no new doors. The admission rule ensures each new domain's shape is disjoint from existing families. The magnitude restriction (integer-only for temporal) is per-domain — currency might allow `'100.50 USD'` if the backing type accepts decimals.

### Expression subset expansion

The interpolation grammar already allows any expression inside `{...}`. Expansion is a type-checker restriction lift, not a grammar change:

| Level | What's added | Grammar change needed? |
|-------|-------------|------------------------|
| 4. Arithmetic | `{Amount * 2}`, `{Score + Bonus}` | None — operators already parse inside `{...}` |
| 5. Conditionals | `{if Urgent then "URGENT" else "normal"}` | None — `if...then...else` already parses |
| 6. Function calls | `{min(A, B)}`, `{round(Rate, 2)}` | None — function calls already parse |

The grammar is future-proof. The type-checker restriction is the safety valve that can be relaxed incrementally.

### New typed constant inhabitants

Future types that pass the admission rule can enter Door 2. Candidates (illustrative, not committed):

| Type | Content shape | Family | Disjoint from existing? |
|------|--------------|--------|------------------------|
| UUID | `8-4-4-4-12` hex pattern | `{uuid}` | Yes — hex + fixed hyphen pattern |
| Email | `user@domain` | `{email}` | Yes — `@` is unique signal |
| URI | `scheme://...` | `{uri}` | Yes — `://` is unique signal |
| Semver | `N.N.N` | `{semver}` | Yes — dot-separated integers |

Each would need formal analysis against the admission rule before entering. The mechanism is ready; the decision is per-type.

### Unit registry model — three resolution scopes

The temporal design established a pattern; future domains fill new resolution scopes:

| Scope | Source of truth | Examples | Resolution timing |
|-------|----------------|---------|-------------------|
| **Language-level** | Backing library (NodaTime) | `days`, `hours`, `months` | Compile-time — closed set |
| **Standard registry** | External standard (ISO 4217, UCUM subset) | `USD`, `EUR`, `kg`, `lbs` | Compile-time — large but closed set |
| **Entity-scoped** | `units` block in precept definition | `each`, `case`, `six-pack` | Compile-time within the precept — conversions are entity data |

The grammar `'<value> <unit>'` is the same across all three scopes. What changes is where the unit identifier resolves from — the validation source, not the syntax.

---

## Implementation Impact

### Tokenizer

- Recognize `{` inside `"..."` and `'...'` as interpolation start. Switch from string-content mode to expression mode.
- Track brace depth to find matching `}`. Nested braces in interpolation expressions require a depth counter.
- `{{` and `}}` produce literal `{` and `}` characters, not interpolation boundaries.
- String and typed constant tokens become segmented: `StringStart`, `InterpolationStart`, expression tokens, `InterpolationEnd`, `StringEnd` (or equivalent representation).
- The Superpower tokenizer (`PreceptTokenizerBuilder`) is currently stateless. String interpolation requires stateful tokenization. This can be implemented via custom `TextParser<Unit>` implementations that track "inside string" vs. "inside interpolation" state.
- Single-quoted content after interpolation resolution follows the same shape-matching as static constants.

### Parser

- String literals are no longer single opaque tokens. They become a sequence of text segments and interpolation expressions.
- Typed constant content with interpolation is parsed as: text segments, `{` expression `}` interpolation regions, and text segments — assembled into a template that is evaluated and then shape-matched.
- Quantity typed constants (`'30 days'`, `'2 years + 6 months'`) parse the (post-substitution) content as a quantity expression: number + unit-word, with `+` combination for compound quantities.
- **No bare postfix combinators needed.** The `PostfixUnitExprBare` and `PostfixUnitExprParen` combinators from the three-door model are eliminated. Quantity parsing happens inside the typed constant content, not at the expression level.

### Type Checker

- Interpolation expressions inside `"..."`: type-check, then verify coercion to string is defined for the expression type (compile error on collections).
- Interpolation expressions inside `'...'`: type-check, evaluate type of expression, verify the substituted content will produce a valid shaped constant.
- Quantity typed constants: resolve type family `{period, duration}` based on expression context, using the same context-dependent resolution rules as before.
- State name typed constants: validate identifier against declared states.
- Interpolation expressions use the full expression evaluator — field references, dot accessors, event args, arithmetic. The grammar parses any expression inside `{...}`; the type checker validates types as usual.

### Language Server

- `"` trigger: offer field names after `{` inside strings. Offer dot-accessor completions after `{FieldName.`.
- `'` trigger: offer temporal format ghost text for formatted constants. Offer unit names after a number inside typed constants. Offer field names after `{` inside typed constants.
- Semantic tokens: interpolation expressions inside strings and constants get expression-level token types (field references, dot accessors).
- Diagnostics: interpolation-specific errors (unknown field in `{...}`, collection interpolation attempt, type coercion failure).

### TextMate Grammar

- String literal pattern updated to recognize `{...}` interpolation regions with expression-level highlighting inside.
- Typed constant pattern updated similarly.
- `{{` and `}}` highlighted as escape sequences, not interpolation boundaries.

### MCP Tools

- `precept_language`: document interpolation syntax, expression subset levels, typed constant inhabitants, admission rule with type-family formulation, string coercion table.
- `precept_compile`: report interpolation-related diagnostics (unknown field, type coercion errors, collection interpolation).
- `precept_fire`/`inspect`: interpolated reject/because messages include substituted values in output — the caller sees `"Credit score 580 is below the 680 minimum"`, not `"Credit score {CreditScore} is below the 680 minimum"`.

---

## Relationship to Other Design Documents

- **[`docs/TemporalTypeSystemDesign.md`](TemporalTypeSystemDesign.md):** References this document for the literal mechanism architecture. Temporal types are the first inhabitants of Door 2. Quantity construction uses the typed constant mechanism defined here.
- **[`docs/PreceptLanguageDesign.md`](PreceptLanguageDesign.md):** Will be updated when literals and interpolation are implemented. The grammar section will add interpolation forms. Primitive literal syntax (numbers, booleans, null) is already implemented and documented there.
- **[`docs/philosophy.md`](philosophy.md):** Principles #1, #2, #3, #8, #12 are referenced throughout. No philosophy changes required.
