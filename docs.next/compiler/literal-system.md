# Literal System

> **Status:** Draft
> **Decisions answered:** Lexer decision 3 (interpolation decomposition), plus literal-system-wide contracts for all pipeline stages
> **Grounding:** `docs.next/language/precept-language-vision.md` § Literal System, language design principles
> **Prototype reference:** `docs/LiteralSystemDesign.md` on `research/nodatime-type-alignment` branch (PR #114)

## Overview

The literal system defines every way a value can appear in Precept source. It is a cross-cutting concern that touches every pipeline stage — from how the lexer segments characters into tokens, through how the parser assembles AST nodes, to how the type checker resolves types and the evaluator materializes values.

This document is organized by the contract each pipeline stage needs. Stages reference only the sections relevant to them.

## Quoted Literal Forms

All non-primitive values enter the language through exactly two quoted forms:

| Form | Delimiter | Produces | Interpolation |
|------|-----------|----------|---------------|
| String | `"..."` | Always `string` | `{expr}` — expression evaluated, coerced to string |
| Typed constant | `'...'` | Non-primitive type, inferred from content shape | `{expr}` — expression evaluated, substituted before shape matching |

There is no third form. No bare postfix keywords, no constructor functions, no `type(value)` forms. Zero constructors exist in the language.

### Why two forms

- **One syntax for literal and variable quantities.** `'30 days'` and `'{GraceDays} days'` use the same typed constant form. No split between "the literal way" and "the variable way."
- **No keyword reservation for unit names.** `days`, `hours`, `USD`, `kg` are validated content inside `'...'`, not language keywords. Field names can use these words without collision.
- **Scalable to future domains.** Each new quantity domain (currency, measurement, entity-scoped units) adds validated string patterns inside `'...'` — not new keywords, not new grammar, not new literal forms.

---

## Layer 1: Primitive Literals

Primitive literals are bare tokens — no delimiters. Type is intrinsic to the token.

### Numeric literals

All numeric forms produce a single `NumberLiteral` token kind. The lexer recognizes integers, decimals, and scientific notation:

| Form | Examples | Valid for types | Notes |
|------|----------|-----------------|-------|
| Integer | `42`, `0`, `999` | `integer`, `decimal`, `number` | Whole digits |
| Decimal | `3.14`, `0.001`, `100.0` | `decimal`, `number` | Digits with decimal point |
| Scientific | `1.5e2`, `1e-5`, `3.0E+10` | `number` only | Exponent notation — type error for `integer`/`decimal` |

**Negative numbers** are not a single token. The parser handles unary minus (`Minus` token) followed by `NumberLiteral`.

### Boolean literals

`true` and `false` are keywords that produce `True` and `False` tokens. They are the only literal type that is also a keyword.

### No null literal

The language has no `null` literal. Optional fields use `is set` / `is not set` for presence testing and `clear` for value removal.

---

## Layer 2: String Literals — `"..."`

String literals always produce `string`. They support two forms:

### Static strings

No interpolation. The entire content between `"` delimiters is a single string value.

```precept
reject "Approval requires verified documents"
rule ClaimAmount >= 0 because "Claim amounts cannot be negative"
```

### Interpolated strings

`{expr}` inside `"..."` evaluates the expression, coerces the result to string, and substitutes inline.

```precept
reject "Credit score {CreditScore} is below the 680 minimum"
reject "Amount {Approve.Amount} exceeds claim maximum of {ClaimAmount}"
rule DueDays >= 0 because "Grace period of {DueDays} days cannot be negative"
```

### Always-on interpolation — no prefix

Unlike C# (`$"..."`), Python (`f"..."`), or Kotlin (`"$expr"`), Precept does not require a prefix to enable interpolation. `{` inside any quoted literal is always the interpolation trigger.

**Why this is safe:** Precept uses keywords for structure (Design Principle #5: keyword-anchored readability). `{` has no structural meaning in the language — no code blocks, no object literals, no dict/set syntax. Inside a quoted context, `{` is unambiguously interpolation start. This is a unique structural advantage that no other surveyed language has.

### Escape sequences

| Escape | Produces | Example |
|--------|----------|---------|
| `\"` | Literal `"` | `"She said \"hello\""` |
| `\\` | Literal `\` | `"Path: C:\\temp"` |
| `{{` | Literal `{` | `"Use {{braces}} for grouping"` |
| `}}` | Literal `}` | `"Use {{braces}} for grouping"` |

`{{` and `}}` are the standard interpolation escape — doubling the delimiter produces the literal character. This matches C#, Python, and Rust conventions.

---

## Layer 3: Typed Constants — `'...'`

Typed constants produce non-primitive values. The type is inferred from content shape, not from the delimiter.

### The admission rule — type families

A type qualifies as a typed constant inhabitant if and only if:

1. Its content shape determines a **type family** — a finite, enumerable set of types.
2. The family is **disjoint** from every other family. No content shape may match two families.
3. Expression **context narrows** within the family to select the specific type.
4. If context cannot narrow a multi-member family, it is a **compile error**.

### Current inhabitants

| Inhabitant | Shape signal | Type family | Context narrowing |
|------------|-------------|-------------|-------------------|
| Dates | `YYYY-MM-DD` | `{date}` | Singleton |
| Times | `HH:MM:SS` or `HH:MM` | `{time}` | Singleton |
| DateTimes | Contains `T`, no trailing `Z`, no `[` | `{datetime}` | Singleton |
| Instants | Contains `T`, trailing `Z` | `{instant}` | Singleton |
| ZonedDateTimes | Contains `T`, bracket-enclosed timezone | `{zoneddatetime}` | Singleton |
| Timezones | `Word/Word` pattern | `{timezone}` | Singleton |
| Quantities | `<value> <unit-word>` | `{period, duration}` | `date ±` → period, `instant ±` → duration |
| State names | Plain identifier | `{state}` | Validated against declarations |

### Shape disambiguation priority

The type checker tests typed constant content against shape patterns in priority order. The first match wins.

| Priority | Shape test | Family | Example |
|----------|-----------|--------|--------|
| 1 | Contains `T` + ends with `[...]` | `{zoneddatetime}` | `'2026-04-15T14:30:00[America/New_York]'` |
| 2 | Contains `T` + ends with `Z` | `{instant}` | `'2026-04-15T14:30:00Z'` |
| 3 | Contains `T` (no `Z`, no `[`) | `{datetime}` | `'2026-04-15T14:30:00'` |
| 4 | Matches `YYYY-MM-DD` | `{date}` | `'2026-04-15'` |
| 5 | Matches `HH:MM(:SS)?` | `{time}` | `'14:30:00'`, `'14:30'` |
| 6 | Contains `/` (IANA pattern) | `{timezone}` | `'America/New_York'` |
| 7 | Matches `<value> <unit-word>` | `{period, duration}` | `'30 days'`, `'2 years + 6 months'` |
| 8 | Plain identifier | `{state}` | `'Open'`, `'UnderReview'` |

No content matches two rows — the distinguishing signals are structurally disjoint: `T`, `Z`, `[`, hyphens-in-date-pattern, colons-in-time-pattern, `/` in IANA identifiers, and `<number> <word>` in quantities are mutually exclusive.

### Static typed constants

```precept
field DueDate as date default '2026-06-01'
field SlaLimit as duration default '72 hours'
field ClinicTimezone as timezone default 'America/New_York'
field GracePeriod as period default '30 days'
```

### Interpolated typed constants

```precept
set DueDate = FiledAt + '{GraceDays} days'
set SlaLimit = CreatedAt + '{SlaHours * 2} hours'
```

Interpolation inside `'...'` uses the same `{expr}` syntax as strings. The expression is evaluated, the result is substituted into the content, and then the full content is shape-matched to determine the type family.

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

**These are NOT language keywords.** They are validated strings inside `'...'`. Field names, event names, and event arg names can use these words without collision. Future quantity domains add new validated strings, not new keywords.

### Integer requirement for temporal quantities

The magnitude in a temporal quantity typed constant must evaluate to an integer. `'0.5 days'` is a compile error: *"Unit values must be whole numbers. Use smaller units for fractions: `'12 hours'` for half a day."*

**Scope:** This restriction applies to temporal unit names only. Future non-temporal quantity domains (e.g., currency with `'100.50 USD'`, physical with `'2.5 kg'`) may support non-integer magnitudes if their backing types accept them. The restriction is per-domain, not per-mechanism.

### Combined quantities

The `+` inside `'...'` combines quantity components:

```precept
field ExtendedWarranty as period default '2 years + 6 months'
set Expiry = StartDate + '1 year + 3 months + 15 days'
```

**Left-associative `+` subtlety:** `date + '1 month' + '1 month'` applies sequential truncation (Jan 31 → Feb 28 → Mar 28). `date + '1 month + 1 month'` builds the period first, then applies once (Jan 31 → Mar 31). Both are valid. The `+` inside `'...'` builds a single compound quantity; the `+` outside `'...'` is sequential arithmetic.

---

## Layer 4: List Literals — `[...]`

List literals are comma-separated scalar literals in brackets. Used exclusively in `default` clauses for collection fields.

```precept
field Tags as set of string default ["pending"]
field Scores as queue of number default [85, 90, 78]
```

Elements must be scalar literals. Nested lists are not supported. List literals are not valid in expression positions.

---

## Interpolation — Shared Contract

Interpolation uses the same `{expr}` syntax inside both `"..."` and `'...'`. One mechanism, both forms.

### Expression support

Interpolation supports the full expression language:

| Category | Examples |
|----------|----------|
| Field references | `{FieldName}` |
| Dot accessors | `{Items.count}`, `{DueDate.year}` |
| Event args | `{Approve.Amount}` |
| Arithmetic | `{AnnualIncome * 3}`, `{SlaHours * 2}` |
| Conditionals | `{if IsPremium then GraceDays else DefaultGrace}` |

### Nested quoting

Expressions inside `{...}` may contain quoted literals: `{if Active then "yes" else "no"}`. Same-delimiter nesting (a `"..."` inside `{...}` inside `"..."`) requires the lexer to track interpolation depth via a mode stack.

---

## Pipeline Stage Contracts

### Lexer

The lexer's job is to **segment** literal source text into tokens. It does not resolve types, evaluate expressions, or validate content.

#### String literal tokens

| Scenario | Token sequence |
|----------|---------------|
| Static string `"hello"` | `StringLiteral` |
| Interpolated `"x {A} y"` | `StringStart` → expression tokens → `StringEnd` |
| Multi-interpolation `"x {A} y {B} z"` | `StringStart` → expr → `StringMiddle` → expr → `StringEnd` |

`StringStart` contains the text from `"` through the first `{`. `StringMiddle` contains text between `}` and the next `{`. `StringEnd` contains text from the last `}` through `"`.

#### Typed constant tokens

Identical segmentation pattern:

| Scenario | Token sequence |
|----------|---------------|
| Static `'2026-06-01'` | `TypedConstant` |
| Interpolated `'{GraceDays} days'` | `TypedConstantStart` → expression tokens → `TypedConstantEnd` |
| Multi-interpolation | `TypedConstantStart` → expr → `TypedConstantMiddle` → expr → `TypedConstantEnd` |

#### Mode stack

The lexer maintains a mode stack to handle interpolation nesting:

| Mode | Entered when | Exited when | Tokens emitted |
|------|-------------|-------------|----------------|
| **Normal** | Start of source, or `}` closes interpolation | `"` or `'` opens a literal | All non-literal tokens |
| **String** | `"` encountered in Normal or Interpolation mode | `"` (unescaped) or `{` (non-doubled) | `StringLiteral`, `StringStart`, `StringMiddle`, `StringEnd` |
| **TypedConstant** | `'` encountered in Normal or Interpolation mode | `'` (unescaped) or `{` (non-doubled) | `TypedConstant`, `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd` |
| **Interpolation** | `{` (non-doubled) inside String or TypedConstant mode | `}` at depth 0 | Expression tokens (identifiers, operators, etc.) |

When `{` is encountered inside a String or TypedConstant mode, the lexer pushes Interpolation mode onto the stack. Inside Interpolation mode, another `"` or `'` pushes a new String or TypedConstant mode (enabling nested quoting). `}` pops back to the enclosing mode and emits the appropriate middle/end token.

#### Escape handling

**String mode escapes** (`"..."` only):

| Sequence | Resolves to | Notes |
|----------|------------|-------|
| `\"` | `"` | Stay in String mode |
| `\\` | `\` | |
| `\n` | newline (U+000A) | String-only — not valid in typed constants |
| `\t` | tab (U+0009) | String-only — not valid in typed constants |
| `{{` | `{` | Do NOT enter Interpolation mode |
| `}}` | `}` | |

Any `\X` where X is not one of the recognized sequences above emits `UnrecognizedStringEscape` — the `\` and the following character are both skipped and scanning continues. A lone `}` that is not part of `}}` emits `UnescapedBraceInLiteral`; the `}` is preserved in the token's `Text`.

**Typed constant mode escapes** (`'...'` only):

| Sequence | Resolves to |
|----------|------------|
| `\'` | `'` |
| `\\` | `\` |
| `{{` | `{` |
| `}}` | `}` |

`\n` and `\t` are not valid inside typed constants — typed constant content is opaque data (dates, durations, units), not human-readable prose. Any unrecognized escape sequence (including `\n` and `\t`) inside a typed constant emits `UnrecognizedTypedConstantEscape` — the `\` and the following character are both skipped. A lone `}` that is not part of `}}` emits `UnescapedBraceInLiteral`; the `}` is preserved in the token's `Text`.

#### List literal tokens

List literals use existing punctuation tokens: `LeftBracket`, scalar literal tokens, `Comma`, `RightBracket`. No special lexer mode needed.

### Parser

The parser reassembles segmented tokens into AST nodes. Its contracts:

1. **Static string/typed constant** — single token → leaf AST node.
2. **Interpolated string** — `StringStart` + (expression + `StringMiddle`)* + expression + `StringEnd` → interpolated string expression node containing a list of text segments and expression nodes.
3. **Interpolated typed constant** — same pattern with `TypedConstantStart`/`Middle`/`End` → interpolated typed constant expression node.
4. **Quantity parsing** — for typed constants whose post-substitution content matches the quantity pattern, the parser (or type checker) parses `<value> <unit>` components with `+` combination.

### Type Checker

The type checker's contracts:

1. **String interpolation expressions** — type-check each `{expr}`, verify coercion to string is defined for the expression type. Collections are a compile error.
2. **Typed constant content** — after substituting interpolation results, shape-match the content to determine the type family. Apply context narrowing to select the specific type.
3. **Admission rule enforcement** — verify that the resolved type family is valid for the expression context. If context cannot narrow a multi-member family, emit a compile error.
4. **Quantity type resolution** — use the context-dependent resolution table to select `period` vs `duration`.

#### Context resolution for typed constants

Context flows **inward** from the nearest enclosing typed node to the typed constant. This is standard top-down type inference — the type checker already holds the full expression tree.

| Position | Context source | Example |
|----------|---------------|--------|
| Right operand of `+`/`-` | Left operand's resolved type | `FiledAt + '30 days'` → left is `instant` → context = `instant` |
| `default` clause | Declared field type | `field X as period default '30 days'` → context = `period` |
| `set X = Y + '...'` | Type of `Y` | `set DueDate = FiledAt + '30 days'` → context from `FiledAt` |
| Comparison (`<=`, `>=`) | Other operand's type or subtraction result | `FiledAt - IncidentAt <= '72 hours'` → subtraction result provides context |
| Compound chain | Result of the preceding sub-expression | `StartDate + '1 month' + '15 days'` → second constant gets context from first addition result |
| No context / ambiguous | **Compile error** | *"Can't determine the type of `'30 days'` without context."* |

#### Quantity type resolution tables

**`months`, `years` — always `period`:** No duration representation exists for calendar-length units. Always resolve to `period` regardless of context. `instant + '3 months'` is a type error.

**`days`, `weeks` — context-dependent:**

| Expression context | Resolves to |
|---|---|
| `date ±` / `datetime ±` | `period` |
| `instant ±` / `zoneddatetime ±` | `duration` |
| `time ±` | **compile error** — days/weeks don't apply to times |
| `field X as period default` | `period` |
| `field X as duration default` | `duration` |

**`hours`, `minutes`, `seconds` — context-dependent:**

| Expression context | Resolves to |
|---|---|
| `instant ±` / `zoneddatetime ±` | `duration` |
| `datetime ±` | `period` |
| `time ±` | bridges to `duration` (sub-day arithmetic) |
| `date ±` | **compile error** — sub-day units don't apply to dates |
| `field X as duration default` | `duration` |
| `field X as period default` | `period` |

#### Unit restrictions belong to constraints

The literal system determines **which type** a quantity constant produces. Business rules about **which values are valid** — e.g., a grace period that must be in whole days or larger — are the job of constraint modifiers on the field declaration. The literal system does not need a unit-restriction mechanism; constraints handle it.

### Evaluator

The evaluator's contracts:

1. **String interpolation** — evaluate each expression, coerce to string using deterministic locale-invariant rules, concatenate segments.
2. **Typed constant interpolation** — evaluate expressions, substitute into content, then materialize the typed value using the resolved type.
3. **String coercion** is deterministic and invariant-culture. `{Amount}` always produces `"1234.56"`, never `"1.234,56"`.

---

## String Coercion Table

When an expression inside `{...}` in a string literal evaluates to a non-string type:

| Expression type | String representation |
|-----------------|---------------------|
| `string` | As-is |
| `number` | Invariant numeric string |
| `integer` | Integer string |
| `decimal` | Decimal string |
| `boolean` | `"true"` / `"false"` |
| `date` | ISO 8601: `YYYY-MM-DD` |
| `time` | `HH:MM:SS` |
| `datetime` | ISO 8601 |
| `instant` | ISO 8601 with `Z` |
| `zoneddatetime` | NodaTime general format |
| `timezone` | IANA identifier |
| `duration` | NodaTime round-trip format |
| `period` | NodaTime canonical string |
| Collection | **Compile error** — use `.count` |

---

## Future Extensibility

### New typed constant inhabitants

Future types that pass the admission rule (disjoint type family) can become typed constant inhabitants. The mechanism is ready; the decision is per-type.

### New quantity domains

Each new domain adds validated string patterns to the typed constant shape matcher:

| Domain | Literal form | Interpolated form |
|--------|-------------|-------------------|
| Temporal (current) | `'30 days'` | `'{GraceDays} days'` |
| Currency (future) | `'100 USD'` | `'{Subtotal} USD'` |
| Percentage (future) | `'10 percent'` | `'{Rate} percent'` |
| Physical (future) | `'5 kg'` | `'{Weight} kg'` |

No new keywords, no new grammar, no new doors.
