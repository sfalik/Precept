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

| Form | Examples | Notes |
|------|----------|-------|
| Integer | `42`, `0`, `999` | Whole digits |
| Decimal | `3.14`, `0.001`, `100.0` | Digits with decimal point |
| Scientific | `1.5e2`, `1e-5`, `3.0E+10` | Exponential notation |

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

### Combined quantities

The `+` inside `'...'` combines quantity components:

```precept
field ExtendedWarranty as period default '2 years + 6 months'
set Expiry = StartDate + '1 year + 3 months + 15 days'
```

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

- `\"` inside String mode → include literal `"` in token text, stay in String mode.
- `\\` inside String/TypedConstant mode → include literal `\` in token text.
- `{{` inside String/TypedConstant mode → include literal `{` in token text, do NOT enter Interpolation mode.
- `}}` inside String/TypedConstant mode → include literal `}` in token text.

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
4. **Quantity type resolution** — use the context-dependent resolution table (documented in temporal/quantity design) to select `period` vs `duration`.

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
