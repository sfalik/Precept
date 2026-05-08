# Literal System

## Status

| Property | Value |
|---|---|
| Doc maturity | Draft |
| Implementation state | Partially implemented — lexer and parser literal handling complete; type checker typed-constant validation infrastructure (`ITypedConstantValidator`) not yet implemented |
| Source | `src/Precept/` — lexer, parser, type checker, evaluator |
| Upstream | Language source text; field type declarations; expression context for type inference |
| Downstream | All pipeline stages (Lexer, Parser, Type Checker, Evaluator); runtime evaluator; diagnostics engine |

**Decisions answered:** Lexer decision 3 (interpolation decomposition), plus literal-system-wide contracts for all pipeline stages  
**Grounding:** `docs/language/precept-language-spec.md`; vision archived at `docs/archive/language-design/precept-language-vision.md` § Literal System  
**Prototype reference:** `docs/LiteralSystemDesign.md` on `research/nodatime-type-alignment` branch (PR #114)

---

## Overview

The literal system defines every way a value can appear in Precept source. It is a cross-cutting concern that touches every pipeline stage — from how the lexer segments characters into tokens, through how the parser assembles AST nodes, to how the type checker resolves types and the evaluator materializes values.

This document is organized by the contract each pipeline stage needs. Stages reference only the sections relevant to them.

---

## Responsibilities and Boundaries

**OWNS:** Token segmentation for string and typed constant literals (lexer); lexer mode stack for interpolation nesting; AST node shapes for all literal forms (parser); context-born type resolution for typed constants (type checker); content validation dispatch via `ITypedConstantValidator` (type checker); interpolation reassembly mechanics across both `"..."` and `'...'` forms; string coercion rules for interpolation expressions (evaluator); value materialization for typed constants (evaluator); quantity type resolution tables (duration vs. period by expression context).

**Does NOT OWN:** Business rules on field values — those are constraint modifiers on field declarations; unit restriction enforcement (e.g., "grace period must be in whole days") — constraint system handles it; domain-specific quantity validators — registered by domain modules, not defined here; null semantics — the language has no null, presence is tested with `is set` / `is not set`; non-literal arithmetic and comparison semantics — owned by the type system and evaluator broadly.

---

## Right-Sizing

The literal system is scoped as a **cross-cutting concern** rather than a single pipeline stage because every stage has literal-specific contracts that must be consistent: the lexer segments characters into literal tokens including mode-stacking for interpolation nesting; the parser reassembles segmented tokens into the correct AST node shapes; the type checker resolves types for context-born forms and validates content against the resolved type; the evaluator materializes values and coerces non-string types in interpolation contexts. Splitting these contracts across per-stage documents would hide cross-stage relationships and invite inconsistency. The scope stops at materialized runtime values — how values behave after materialization belongs to the type system and evaluator broadly.

---

## Inputs and Outputs

The literal system is cross-cutting. Each pipeline stage transforms a different representation:

| Stage | Input | Output |
|---|---|---|
| **Lexer** | Raw source characters inside `"..."`, `'...'`, `[...]`, and bare token positions | `StringLiteral`, `StringStart/Middle/End`, `TypedConstant`, `TypedConstantStart/Middle/End`, `NumberLiteral`, `True`, `False`, `LeftBracket`, `Comma`, `RightBracket` |
| **Parser** | Literal token sequences from the lexer | `ParsedExpression` subtypes: `LiteralExpression` (for strings, numbers, booleans, typed constants), `InterpolatedStringExpression` (for `"...{expr}..."` and `'...{expr}...'`), `ListLiteralExpression` (for `[...]`). Token kind on `LiteralExpression` distinguishes the literal category. |
| **Type Checker** | AST literal nodes + expression context (field type, operator peer, function parameter) | Typed literal nodes with resolved types; compile errors for missing context, failed content validation, or type mismatches |
| **Evaluator** | Typed literal nodes + resolved types + runtime field values | Materialized CLR values; coerced string segments for interpolation; typed constant instances (NodaTime, Money, Quantity, etc.) |

---

## Two-Door Literal Model

All non-primitive values enter the language through exactly two quoted forms:

| Form | Delimiter | Produces | Interpolation |
|------|-----------|----------|---------------|
| String | `"..."` | Always `string` | `{expr}` — expression evaluated, coerced to string |
| Typed constant | `'...'` | Non-primitive type, determined by expression context then content-validated | `{expr}` — expression evaluated, substituted before content validation |

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

Typed constants produce non-primitive values. Like numeric literals, typed constants are **context-born** — the expression context determines the type, and the content is then validated against that type. The delimiter `'...'` is not type-specific; it marks a value whose type comes from context, not from the delimiter itself.

### Resolution model — context-born, then content-validated

Typed constant resolution follows the same two-step model as numeric literals:

1. **Context determines the type.** The type checker propagates an expected type inward from the enclosing expression — field declaration, assignment target, operator peer, function parameter, or comparison operand. This is the same top-down inference that resolves `42` to `integer`, `decimal`, or `number`.
2. **Content is validated against the expected type.** Once the expected type is known, the content is parsed and validated by the type's registered `ITypedConstantValidator`. If the content doesn't parse as the expected type, it is a compile error. If no validator is registered (domain module not yet shipped), structural validation only.
3. **No context → compile error.** A typed constant in a position with no type expectation is a compile error, just as `42` in a contextless position is a compile error.

This replaces the previous "shape-first" model where content determined the type. Shape is no longer the resolution mechanism — context is.

### Content validation table

Given the context-determined type, the content must parse as a valid value of that type:

| Expected type | Valid content patterns | Examples |
|---|---|---|
| `date` | `YYYY-MM-DD` | `'2026-04-15'` |
| `time` | `HH:MM:SS` or `HH:MM` | `'14:30:00'`, `'14:30'` |
| `instant` | ISO 8601 with `T`, trailing `Z` | `'2026-04-15T14:30:00Z'` |
| `datetime` | ISO 8601 with `T`, no zone | `'2026-04-15T14:30:00'` |
| `zoneddatetime` | ISO 8601 with `T`, bracket-enclosed timezone | `'2026-04-15T14:30:00[America/New_York]'` |
| `timezone` | `Word/Word` IANA identifier | `'America/New_York'` |
| `duration` | `<integer> <temporal-unit>` (with optional `+ <integer> <unit>`) | `'72 hours'`, `'2 hours + 30 minutes'` |
| `period` | `<integer> <temporal-unit>` (with optional `+ <integer> <unit>`) | `'30 days'`, `'2 years + 6 months'` |
| `money` | `<number> <ISO-4217-code>` | `'100 USD'`, `'50.25 EUR'` |
| `quantity` | `<number> <unit-name>` | `'5 kg'`, `'24 each'` |
| `price` | `<number> <currency>/<unit>` | `'4.17 USD/each'` |
| `exchangerate` | `<number> <currency>/<currency>` | `'1.08 USD/EUR'` |
| `currency` | `<ISO-4217-code>` (3-letter) | `'USD'`, `'EUR'` |
| `unitofmeasure` | Unit name (lowercase/mixed) | `'kg'`, `'each'` |
| `dimension` | Dimension name (UCUM registry) | `'mass'`, `'length'` |
| state ref | Plain identifier | `'Open'`, `'UnderReview'` |

**Content validation is compile-time.** When a validator is registered for the expected type, malformed content is a compile error — e.g., `'2026-02-30'` fails date validation, `'XYZ 100.00'` fails money validation (XYZ is not a recognized ISO 4217 code). The `ITypedConstantValidator` registry is layered: the checker defines the hook, each domain type family registers its validator. If no validator is registered, structural validation is accepted.

### Context sources

Context flows **inward** from the nearest enclosing typed node to the typed constant. This is standard top-down type inference — the type checker already holds the full expression tree.

| Position | Context source | Example |
|----------|---------------|--------|
| `default` clause | Declared field type | `field X as period default '30 days'` → context = `period` |
| `set X = '...'` | Target field's type | `set DueDate = '2026-06-01'` → context = `date` |
| Right operand of `+`/`-` | Left operand's resolved type | `FiledAt + '30 days'` → left is `instant` → context = `duration` |
| `set X = Y + '...'` | Type of `Y` | `set DueDate = FiledAt + '30 days'` → context from `FiledAt` |
| Comparison (`==`, `!=`, `<`, etc.) | Other operand's type | `when DueDate > '2026-04-15'` → peer is `date` → context = `date` |
| Compound chain | Result of the preceding sub-expression | `StartDate + '1 month' + '15 days'` → second constant gets context from first addition result |
| No context / ambiguous | **Compile error** | *"Cannot determine the type of typed constant without context."* |

### The parallel with numeric literals

| Aspect | Numeric literal (`42`) | Typed constant (`'30 days'`) |
|--------|----------------------|------------------------------|
| Resolution mechanism | Context determines lane | Context determines type |
| Validation | Content checked against lane (e.g., fractional → not `integer`) | Content checked against type (e.g., `'2026-02-30'` → not a valid `date`) |
| No context | Compile error | Compile error |
| Ambiguous context | Compile error | Compile error |

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

Interpolation inside `'...'` uses the same `{expr}` syntax as strings. The expression is evaluated, the result is substituted into the content, and then the full content is validated against the context-determined type.

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
2. **Typed constant resolution** — determine the expected type from context (field type, operator peer, function signature), then validate the content against that type using the registered `ITypedConstantValidator`. No context → compile error.
3. **Interpolated typed constants** — after substituting interpolation results, validate the full content against the context-determined type.
4. **Quantity unit resolution** — for `duration` and `period`, validate the unit words against the temporal unit closed set.

#### Context resolution for typed constants

See "Context sources" in the Resolution Model section above for the full context-flow table.

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

Future types that support context-born resolution can become typed constant inhabitants. The mechanism is ready: add a content validator for the new type, and any typed constant in a context expecting that type will be validated. The decision is per-type.

### New quantity domains

Each new domain adds validated string patterns to the typed constant shape matcher:

| Domain | Literal form | Interpolated form |
|--------|-------------|-------------------|
| Temporal | `'30 days'` | `'{GraceDays} days'` |
| Currency / money | `'100 USD'` | `'{Subtotal} USD'` |
| Physical / quantity | `'5 kg'` | `'{Weight} kg'` |
| Price | `'4.17 USD/each'` | `'{UnitPrice} USD/each'` |
| Exchange rate | `'1.08 USD/EUR'` | `'{Rate} USD/EUR'` |
| Percentage (future) | `'10 percent'` | `'{Rate} percent'` |

No new keywords, no new grammar, no new doors.

---

## Design Rationale and Decisions

### Two-door model: exactly two quoted forms

The language provides `"..."` for strings and `'...'` for typed constants — no third form. No bare postfix keywords for unit names (avoids keyword collisions with field/event names). No constructor functions or `type(value)` forms (zero constructors in the language). No new literal forms for future domains — they extend `'...'` via content validation. The two-door model ensures the grammar surface is permanently stable across all future quantity domains.

### Context-born resolution replaces shape-first

The original shape-first model determined the type of a typed constant from its content shape (e.g., a date pattern → `date`). This was replaced with context-born resolution: the expected type flows inward from the enclosing expression, and content is then validated against that type. Shape-first required type decisions before the full expression context was available. Content is ambiguous without context — `'30 days'` could be `duration` or `period`. Context-born aligns typed constants with numeric literals, creating a unified top-down inference model with the type checker as the single source of type authority.

### Unit names as validated content, not keywords

Temporal unit names (`days`, `hours`, etc.), currency codes (`USD`, `EUR`), and future domain unit names are validated inside `'...'` — not reserved as language keywords. This ensures field names and event names can freely use these words without grammar collisions and keeps the keyword set stable as new domains ship. New domains add validated patterns, not keywords.

### String coercion is invariant-culture and deterministic

String coercion during interpolation always uses invariant-culture formatting. `{Amount}` always produces `"1234.56"` regardless of runtime locale. This prevents subtle formatting bugs in output messages where the producing system uses one locale and the consuming system uses another.

---

## Innovation

- **Always-on interpolation (no prefix required):** Unlike C# (`$"..."`), Python (`f"..."`), or Kotlin (`"$expr"`), Precept enables interpolation inside all quoted literals without a prefix sigil. This is safe because `{` has no structural meaning in Precept — no code blocks, no object literals, no dict/set syntax. Inside any quoted context, `{` is unambiguously the interpolation trigger. This structural advantage is unique among surveyed languages and flows from Precept's keyword-anchored grammar (Design Principle #5).
- **Unified context-born resolution:** Typed constants and numeric literals share the same resolution model — context determines the type, content is validated against the type. This unification means there is exactly one way for the type checker to handle all literal forms: propagate expected type inward, check. No special-casing for `'...'` vs. `42`. Any future literal form follows the same two-step contract.

---

## Open Questions / Implementation Notes

> **Open Question:** `ITypedConstantValidator` registration API
> The literal system defines `ITypedConstantValidator` as the extension hook for typed-constant validation, but the registration surface is still undecided. Consumers need one canonical registration model before domain validators can be wired into compilation predictably.
> *Flagged: 2026-05-04*

> **Open Question:** Interpolated typed-constant validation timing
> The doc says typed-constant content is validated after interpolation substitution, but it does not settle whether that happens entirely at compile time, at runtime, or in a split model. That timing decision changes diagnostics behavior and how much the evaluator must still validate after type checking.
> *Flagged: 2026-05-04*

> **Open Question:** Structural validation fallback for `'...'`
> The fallback rule for "structural validation is accepted" remains undefined when no validator is registered for the expected type. The literal-system contract needs an explicit minimum-check story so deferred or partially shipped domains behave deterministically.
> *Flagged: 2026-05-04*
4. **Percentage domain:** Listed in Future Extensibility as `'10 percent'`. The type name, validator shape, and arithmetic semantics are TBD.
5. **State reference literals:** The content validation table lists `state ref` as a typed constant kind using plain identifiers (`'Open'`, `'UnderReview'`). The mechanics of state reference validation (which state machine provides the context?) are not yet documented here.

---

## Deliberate Exclusions

| Excluded | Reason |
|---|---|
| `null` literal | Precept has no null. Optional fields use `is set` / `is not set` for presence testing and `clear` for removal. |
| Constructor functions / `type(value)` forms | Zero constructors in the language. All values are literals or derived from field values through operations. |
| Interpolation prefix sigil (`$"..."`, `f"..."`) | Unnecessary given `{` has no structural meaning in Precept. Always-on is safe, simpler, and consistent across both literal forms. |
| Third literal form for new domains | New quantity domains extend `'...'` via content validation — no new delimiter or grammar form is ever added. |
| Unit names as language keywords | Avoids keyword collisions with field/event names; keeps the keyword set stable as new domains ship. |
| `\n`, `\t` inside typed constants | Typed constant content is opaque data (dates, durations, units). String-style whitespace escapes are not valid and emit `UnrecognizedTypedConstantEscape`. |
| Nested list literals | `[...]` elements must be scalar literals. Nested collections are not valid at the literal level. |
| List literals in expression positions | List literals are valid only in `default` clauses for collection fields — not in general expressions. |

---

## Cross-References

| Topic | Document |
|---|---|
| Product-level literal system vision | `docs/archive/language-design/precept-language-vision.md` § Literal System (archived) |
| Full DSL semantics reference | `docs/language/precept-language-spec.md` |
| Prototype with full design exploration (PR #114) | `docs/LiteralSystemDesign.md` on `research/nodatime-type-alignment` branch |
| Mode stack implementation, complete token kind catalog | `docs/compiler/lexer.md` |
| AST node shapes for all literal forms | `docs/compiler/parser.md` |
| `ITypedConstantValidator` registration, context propagation mechanics | `docs/compiler/type-checker.md` |
| Pipeline stage ordering, artifact types | `docs/compiler-and-runtime-design.md` |

---

## Source Files

The literal system spans multiple pipeline stages in `src/Precept/`:

| Component | Location | Responsibility |
|---|---|---|
| Lexer | `src/Precept/Pipeline/Lexer.cs` | Mode stack, literal token kinds, escape handling, interpolation segmentation |
| Parser | `src/Precept/Pipeline/Parser.cs`, `src/Precept/Pipeline/Parser.Expressions.cs` | Expression parsing including all literal forms, interpolation reassembly |
| Type Checker | `src/Precept/Pipeline/TypeChecker.cs` | Context-born resolution, quantity type tables |
| Evaluator | `src/Precept/Runtime/Evaluator.cs` | Value materialization, string coercion (invariant-culture), typed constant instantiation |
