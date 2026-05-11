# Interpolation Implementation Plan — Type-Grammar-Driven Slot Classification

**Status:** Awaiting Shane review before Slice 1 (Parser) implementation begins  
**Architect:** Frank (frank-16, frank-18, frank-23 revision)  
**Philosophy grounding:** `docs/philosophy.md` — make invalid configurations structurally impossible  
**Supersedes:** Previous position-text plan (frank-16/frank-18)

---

## Problem Statement

Interpolated typed constants (`'{x} kg'`, `'1 {x}'`, `'{Amt} {Curr}'`) are lexed correctly but completely unimplemented beyond a crash-prevention stub. The parser skips all hole tokens. The type checker maps `TypedConstantStart` to `TypedErrorExpression` with no expression nodes created.

**Lexer (correct):** Tokenizes `{expr}` holes into `TypedConstantStart` / `TypedConstantMiddle` / `TypedConstantEnd` segments.  
**Parser (stub):** `ParseInterpolatedTypedConstant()` at `Parser.Expressions.cs:444` skips all hole tokens — produces flat `LiteralExpression(TypedConstantStart, ...)`. No expression AST nodes.  
**Type checker (crash-safe stub):** `ResolveLiteral` `TypedConstantStart` branch emits `TypeMismatch` diagnostic then returns `TypedErrorExpression` (commit `dd1d8e7f` — D26 crash prevention).

---

## Philosophy Constraint

Precept's core identity: **make invalid configurations structurally impossible, not deferred to runtime.**

A `boolean` in a `quantity` hole that compiles clean is exactly what Precept is built to prevent. There are no "V1 permissive" or "V2 deferred" semantics. All type mismatches must be caught at compile time, with one explicit exception (see `string` below).

### The `string` Exception

`string` is valid in any hole position. A string field could hold a valid unit code or currency code at runtime — the compiler cannot statically know the string's content. This is the one legitimately justified runtime-deferred check, explicitly reasoned against the philosophy.

**Rationale:** Types like `currency` or `unitofmeasure` are string-representable at the boundary. A `string` field could carry `"USD"` or `"kg"`. The compiler cannot statically verify string content but must not prevent the author from bridging string data into typed constant construction. Runtime validators catch illegal values; the compiler defers only when it structurally cannot decide.

---

## Core Design Decision: Type-Grammar-Driven Slot Classification

### What Changed from the Previous Plan

The previous plan (frank-16/frank-18) used **position-text heuristics** — "hole precedes a unit fragment → magnitude slot" — to classify holes. This breaks for:

1. **Compound qualifier types** (`price`, `exchangerate`): `'{Rate} {Curr}/{Unit}'` has three holes; the position-text approach cannot distinguish the currency hole from the unit hole without knowing the target type's grammar.
2. **Compound period**: `'{n} years + {m} months'` has two magnitude holes with `+` separator — no analogue in the previous model.
3. **Structurally invalid forms**: `'1 {x} kg'` — the old model had no mechanism to reject forms that don't match any valid pattern for the target type.

### The Replacement: Type-Grammar Matching

Each typed constant type that supports interpolation defines a **closed set of valid segment-sequence patterns** (a type grammar). The type checker:

1. Knows the target type from context-sensitive resolution (§3.3).
2. Extracts the segment sequence from the parsed `InterpolatedTypedConstantExpression` — alternating `TextSegment` (literal text) and `HoleSegment` (expression) nodes.
3. Matches the segment sequence against the target type's valid-form grammar.
4. On match: assigns a **slot identity** to each hole (magnitude, currency, unit, etc.).
5. On no match: emits `InvalidInterpolatedTypedConstantForm` — a **structural error**, distinct from per-hole type mismatch.
6. For each matched hole: checks the resolved expression type against the slot's compatibility table.

**Rationale:** The type checker already knows the target type. Each type has a finite, small set of valid interpolated forms (at most 4 patterns for any type). Matching against these patterns is simpler, more correct, and more maintainable than position-text heuristics. The canonical docs (`business-domain-types.md` §1289, `temporal-type-system.md` §Temporal Quantity Construction) enumerate exactly these forms — the implementation mirrors the specification.

**Alternative rejected — position-text heuristics:** Examining surrounding text fragments ("is the next text a unit name?") requires the type checker to duplicate content-validation knowledge at the slot level and fails for compound qualifiers where two adjacent holes have different semantic identities. The type-grammar approach avoids this entirely.

**Alternative rejected — parser-level slot classification:** The parser doesn't know the target type. Slot identity is inherently type-dependent. The parser's job is structure (segments); the type checker's job is semantics (slots).

---

## Per-Type Valid Form Grammars

Each grammar uses `T(...)` for a TextSegment containing the described text, and `H` for a HoleSegment. The grammars below define the **complete, closed set** of valid interpolated forms for each type. Any segment sequence that does not match a listed pattern for the context-determined type is a structural error.

**Notation:**
- `T(num)` — text segment containing a numeric literal (integer or decimal)
- `T(unit)` — text segment containing a valid unit name
- `T(curr)` — text segment containing a valid ISO 4217 currency code
- `T(dim)` — text segment containing a valid dimension name
- `T(tz)` — text segment containing an IANA timezone identifier
- `T(' ')` — text segment containing a space
- `T('/')` — text segment containing a `/`
- `T(' + ')` — text segment containing ` + `
- `H[slot]` — hole segment; `slot` names the semantic identity assigned on match
- `ε` — empty text segment (leading/trailing)

### Group 1 — Magnitude + Qualifier Types

#### `money`

**Canonical source:** `business-domain-types.md` §1289 — `'100 USD'`, `'{Amount} USD'`, `'100 {Curr}'`, `'{Amount} {Curr}'`

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| M1 | `H[whole-value]` | `'{x}'` | whole-value |
| M2 | `H[magnitude] T(' ') T(curr)` | `'{Amt} USD'` | magnitude |
| M3 | `T(num) T(' ') H[currency]` | `'100 {Curr}'` | currency |
| M4 | `H[magnitude] T(' ') H[currency]` | `'{Amt} {Curr}'` | magnitude, currency |

**Notes:**
- The space between magnitude and currency is mandatory (matches the static form `'100 USD'`).
- `T(num)` validation: must be a valid numeric literal. `T(curr)` validation: must be a valid ISO 4217 code. These are content validations applied to the text segments themselves, the same as static typed constant validation.

#### `quantity`

**Canonical source:** `business-domain-types.md` §1289 — `'5 kg'`, `'{Weight} kg'`, `'5 {Unit}'`, `'{Weight} {Unit}'`

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| Q1 | `H[whole-value]` | `'{x}'` | whole-value |
| Q2 | `H[magnitude] T(' ') T(unit)` | `'{Wt} kg'` | magnitude |
| Q3 | `T(num) T(' ') H[unit]` | `'5 {Unit}'` | unit |
| Q4 | `H[magnitude] T(' ') H[unit]` | `'{Wt} {Unit}'` | magnitude, unit |

#### `price`

**Canonical source:** `business-domain-types.md` §1289 — `'4.17 USD/each'`, `'{Rate} USD/each'`, `'4.17 {Curr}/{Unit}'`, `'{Rate} {Curr}/{Unit}'`

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| P1 | `H[whole-value]` | `'{x}'` | whole-value |
| P2 | `H[magnitude] T(' ') T(curr) T('/') T(unit)` | `'{Rate} USD/each'` | magnitude |
| P3 | `T(num) T(' ') H[currency] T('/') T(unit)` | `'4.17 {Curr}/each'` | currency |
| P4 | `T(num) T(' ') T(curr) T('/') H[unit]` | `'4.17 USD/{Unit}'` | unit |
| P5 | `T(num) T(' ') H[currency] T('/') H[unit]` | `'4.17 {Curr}/{Unit}'` | currency, unit |
| P6 | `H[magnitude] T(' ') H[currency] T('/') T(unit)` | `'{Rate} {Curr}/each'` | magnitude, currency |
| P7 | `H[magnitude] T(' ') T(curr) T('/') H[unit]` | `'{Rate} USD/{Unit}'` | magnitude, unit |
| P8 | `H[magnitude] T(' ') H[currency] T('/') H[unit]` | `'{Rate} {Curr}/{Unit}'` | magnitude, currency, unit |

**Notes:**
- The `/` between currency and unit is mandatory and has no surrounding spaces (matches the static form `'4.17 USD/each'`).
- The full pattern set is the combinatorial expansion: each of { magnitude, currency, unit } is independently either a text literal or a hole. 2³ = 8 combinations; the fully-static form `'4.17 USD/each'` is not interpolated, leaving 7 interpolated patterns plus the whole-value form = 8 total.

#### `exchangerate`

**Canonical source:** `business-domain-types.md` §1289 — `'1.08 USD/EUR'`, `'{Rate} USD/EUR'`, `'1.08 {From}/{To}'`, `'{Rate} {From}/{To}'`

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| X1 | `H[whole-value]` | `'{x}'` | whole-value |
| X2 | `H[magnitude] T(' ') T(curr) T('/') T(curr)` | `'{Rate} USD/EUR'` | magnitude |
| X3 | `T(num) T(' ') H[from-currency] T('/') T(curr)` | `'1.08 {From}/EUR'` | from-currency |
| X4 | `T(num) T(' ') T(curr) T('/') H[to-currency]` | `'1.08 USD/{To}'` | to-currency |
| X5 | `T(num) T(' ') H[from-currency] T('/') H[to-currency]` | `'1.08 {From}/{To}'` | from-currency, to-currency |
| X6 | `H[magnitude] T(' ') H[from-currency] T('/') T(curr)` | `'{Rate} {From}/EUR'` | magnitude, from-currency |
| X7 | `H[magnitude] T(' ') T(curr) T('/') H[to-currency]` | `'{Rate} USD/{To}'` | magnitude, to-currency |
| X8 | `H[magnitude] T(' ') H[from-currency] T('/') H[to-currency]` | `'{Rate} {From}/{To}'` | magnitude, from-currency, to-currency |

**Notes:**
- Same `/` separator as `price`. Both currency codes are ISO 4217. The first is the from-currency, the second is the to-currency. Positional identity is determined by the type grammar, not by examining text content.

### Group 2 — Temporal Quantity Types

#### `duration`

**Canonical source:** `temporal-type-system.md` §Temporal Quantity Construction — `'72 hours'`, `'{SlaHours} hours'`, `'5 hours + 30 minutes'`

Duration typed constants follow the `<integer> <unit-name>` pattern with optional `+` compound combination. Valid temporal unit names for `duration`: `hours`, `minutes`, `seconds`.

**Single-component forms:**

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| D1 | `H[whole-value]` | `'{x}'` | whole-value |
| D2 | `H[magnitude] T(' ') T(temporal-unit)` | `'{SlaHours} hours'` | magnitude |
| D3 | `T(int) T(' ') H[unit]` | `'72 {unit}'` | unit |
| D4 | `H[magnitude] T(' ') H[unit]` | `'{n} {unit}'` | magnitude, unit |

**Compound forms** (N components joined by ` + `):

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| D5 | `H[magnitude₁] T(' ') T(tu₁) T(' + ') H[magnitude₂] T(' ') T(tu₂)` | `'{h} hours + {m} minutes'` | magnitude₁, magnitude₂ |
| D6 | `T(int₁) T(' ') T(tu₁) T(' + ') H[magnitude₂] T(' ') T(tu₂)` | `'5 hours + {m} minutes'` | magnitude₂ |
| D7 | `H[magnitude₁] T(' ') T(tu₁) T(' + ') T(int₂) T(' ') T(tu₂)` | `'{h} hours + 30 minutes'` | magnitude₁ |

**Notes:**
- The compound `+` separator is ` + ` (space-plus-space), matching the canonical form `'2 years + 6 months'`.
- Compound forms extend to N components (e.g., `'5 hours + 30 minutes + 15 seconds'`). The grammar generalizes: `(component₁) T(' + ') (component₂) [T(' + ') (component₃)]...` where each component is either static (`T(int) T(' ') T(tu)`) or has a magnitude hole (`H[magnitudeₙ] T(' ') T(tu)`).
- Unit holes in compound forms are not supported — each compound component must name its unit literally. This matches the canonical docs which show `'{n} years + {m} months'` but never `'{n} {u₁} + {m} {u₂}'`. A compound form with dynamic unit names would require runtime reassembly of the quantity structure; the `+` semantics depend on knowing which units are being combined.
- Temporal magnitudes must resolve to `integer` (Decision #28). `decimal` and `number` are compile errors in temporal magnitude slots.

#### `period`

**Canonical source:** `temporal-type-system.md` §Temporal Quantity Construction — `'30 days'`, `'{GraceDays} days'`, `'2 years + 6 months'`, `'{n} years + {m} months'`

Valid temporal unit names for `period`: `years`, `months`, `weeks`, `days`, `hours`, `minutes`, `seconds`.

**Single-component forms:** Identical structure to `duration` (D1–D4 above).

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| Pe1 | `H[whole-value]` | `'{x}'` | whole-value |
| Pe2 | `H[magnitude] T(' ') T(temporal-unit)` | `'{GraceDays} days'` | magnitude |
| Pe3 | `T(int) T(' ') H[unit]` | `'30 {unit}'` | unit |
| Pe4 | `H[magnitude] T(' ') H[unit]` | `'{n} {unit}'` | magnitude, unit |

**Compound forms:** Same extension as `duration` (D5–D7 pattern, N components with ` + `).

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| Pe5 | `H[magnitude₁] T(' ') T(tu₁) T(' + ') H[magnitude₂] T(' ') T(tu₂)` | `'{n} years + {m} months'` | magnitude₁, magnitude₂ |
| Pe6 | `T(int₁) T(' ') T(tu₁) T(' + ') H[magnitude₂] T(' ') T(tu₂)` | `'2 years + {m} months'` | magnitude₂ |
| Pe7 | `H[magnitude₁] T(' ') T(tu₁) T(' + ') T(int₂) T(' ') T(tu₂)` | `'{n} years + 6 months'` | magnitude₁ |

**Notes:**
- Period permits up to 7 unit components (`years + months + weeks + days + hours + minutes + seconds`).
- Temporal magnitude integer requirement applies (Decision #28).
- Same unit-hole prohibition in compound forms as `duration`.

### Group 3 — Single-Component Types

#### `currency`

**Canonical source:** `precept-language-spec.md` §3.3 — content is `<ISO-4217-code>` (e.g., `'USD'`)

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| C1 | `H[whole-value]` | `'{x}'` | whole-value |

**Notes:**
- `currency` is a single-component type — the entire content is the currency code. There is no magnitude. The only valid interpolated form is a whole-value hole.

#### `unitofmeasure`

**Canonical source:** `precept-language-spec.md` §3.3 — content is `<unit-name>` (e.g., `'kg'`)

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| U1 | `H[whole-value]` | `'{x}'` | whole-value |

#### `dimension`

**Canonical source:** `precept-language-spec.md` §3.3 — content is `<dimension-name>` (e.g., `'mass'`)

| # | Pattern | Example | Slot assignments |
|---|---------|---------|------------------|
| Dim1 | `H[whole-value]` | `'{x}'` | whole-value |

### Group 4 — Formatted Temporal Types

#### Decision: `date`, `time`, `instant`, `datetime`, `zoneddatetime`, `timezone` — interpolation NOT supported

**Rationale:**

These six types use **formatted string content** — fixed patterns like `YYYY-MM-DD`, `HH:MM:SS`, `YYYY-MM-DDTHH:MM:SSZ`, `Word/Word`. Unlike magnitude+qualifier types where components have independent semantic identity (amount vs. currency), formatted temporal values have **positional character patterns** where individual fragments have no meaningful type. What would `'{x}-04-15'` mean for `date`? The hole replaces the year component — but `x` would need to be a 4-digit integer in a very specific string position, not a first-class Precept type.

Consider the alternatives:

1. **Allow whole-value holes only** (`'{x}'` where `x` is `date`): This is legal today via `set DateField = SomeDateField` — direct field assignment. An interpolated typed constant with a single whole-value hole adds no capability over direct assignment and is misleading (it looks like construction but is just copying).

2. **Allow component holes** (`'{y}-{m}-{d}'`): This would require defining year/month/day as independent typed slots, validating their ranges, and reconstructing a valid date — essentially a constructor. Precept has zero constructors by design (temporal-type-system.md Decision #17). This path contradicts the zero-constructor discipline.

3. **Disallow entirely**: Clean, consistent, no implementation cost. Authors construct dates through arithmetic: `StartDate + '{n} days'` uses `period` interpolation, which IS supported.

**Decision:** Interpolation is **not supported** for `date`, `time`, `instant`, `datetime`, `zoneddatetime`, and `timezone`. An interpolated typed constant with any of these as the context type emits:

> `InterpolationNotSupportedForType` (new diagnostic, see below): "Interpolation is not supported for `{type}` typed constants. Formatted values like `'2026-04-15'` must be written as complete literals. To compute temporal values dynamically, use arithmetic: `StartDate + '{n} days'`."

**Precedent:** The canonical spec (§3.3 content validation table) lists formatted patterns for these types with no interpolation examples. `business-domain-types.md` §1289 lists interpolation forms only for the magnitude+qualifier types. `temporal-type-system.md` shows interpolation only for `duration`/`period` quantity forms, never for `date`/`time`/`instant`/`datetime`/`zoneddatetime`/`timezone`.

---

## Per-Slot Hole Type Compatibility Tables

These tables define the valid expression types for each semantic slot across all types. Derived from `business-domain-types.md` §1289 and `temporal-type-system.md` §Temporal Quantity Construction.

### Business Domain Types

#### `money`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` | `integer`, `decimal`, `number`, `string` | Amount component — any numeric type widens to `decimal` (money's backing magnitude). `number` accepted because the widening check is post-resolution (the hole expression resolved to `number`, and `number → decimal` is the explicit `round()` bridge — the type checker's assignment validation handles this, not the slot compatibility table). |
| `currency` | `currency`, `string` | Currency code component. Only `currency` and `string` can produce a valid ISO 4217 code. |
| `whole-value` | `money`, `string` | Entire money value. Only `money` (same type) and `string` (runtime-deferred) are valid. |

#### `quantity`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` | `integer`, `decimal`, `number`, `string` | Magnitude component — any numeric type is valid for a quantity's decimal-backed magnitude. |
| `unit` | `unitofmeasure`, `string` | Unit name component. Only `unitofmeasure` and `string` can produce a valid UCUM unit code. |
| `whole-value` | `quantity`, `string` | Entire quantity value. |

#### `price`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` | `integer`, `decimal`, `number`, `string` | Rate/amount component. |
| `currency` | `currency`, `string` | Numerator currency component. |
| `unit` | `unitofmeasure`, `string` | Denominator unit component. |
| `whole-value` | `price`, `string` | Entire price value. |

#### `exchangerate`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` | `integer`, `decimal`, `number`, `string` | Rate component. |
| `from-currency` | `currency`, `string` | Numerator currency. |
| `to-currency` | `currency`, `string` | Denominator currency. |
| `whole-value` | `exchangerate`, `string` | Entire exchange rate value. |

### Temporal Quantity Types

#### `duration`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` (including compound `magnitude₁`..`magnitudeₙ`) | `integer`, `string` | Temporal quantities require integer magnitudes (Decision #28). `decimal` and `number` are compile errors — `'3.5 hours'` is invalid. `string` is the universal escape. |
| `unit` | `unitofmeasure`, `string` | Unit name slot — valid temporal unit names (`hours`, `minutes`, `seconds`) are a subset of `unitofmeasure`. |
| `whole-value` | `duration`, `string` | Entire duration value. |

#### `period`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` (including compound `magnitude₁`..`magnitudeₙ`) | `integer`, `string` | Same integer requirement as `duration` (Decision #28). |
| `unit` | `unitofmeasure`, `string` | Valid temporal unit names (`years`, `months`, `weeks`, `days`, `hours`, `minutes`, `seconds`). |
| `whole-value` | `period`, `string` | Entire period value. |

### Single-Component Types

#### `currency`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `whole-value` | `currency`, `string` | The entire content is a currency code. |

#### `unitofmeasure`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `whole-value` | `unitofmeasure`, `string` | The entire content is a unit name. |

#### `dimension`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `whole-value` | `dimension`, `string` | The entire content is a dimension name. |

---

## Structural Error: Invalid Interpolated Form

### New Diagnostic Codes

**`InvalidInterpolatedTypedConstantForm = 120`**

Emitted when the segment sequence of an interpolated typed constant does not match any valid pattern for the context-determined type. This is a **structural error** — the arrangement of text and holes is wrong, independent of the types of the hole expressions.

**Message template:** `"Invalid interpolated form for '{type}'. Expected forms: {valid-forms-summary}. The segments '{actual-segments}' do not match any valid pattern."`

**Example triggers:**
- `'1 {x} kg'` for `quantity` → three segments (num, hole, unit) — no quantity pattern has a hole between a magnitude literal and a unit literal. The magnitude is already provided by `1`; there is no semantic slot for a second value between magnitude and unit.
- `'{x} {y} {z}'` for `money` → three holes with two spaces — no money pattern has three components.
- `'{x}/{y}'` for `money` → `/` separator — money uses space separator, not `/`.
- `'{x}'` for `date` → interpolation not supported for formatted temporal types.

**`InterpolationNotSupportedForType = 121`**

Emitted when an interpolated typed constant appears in a context where the target type does not support interpolation (formatted temporal types).

**Message template:** `"Interpolation is not supported for '{type}' typed constants. {type-specific-guidance}"`

**Type-specific guidance:**
- `date`: "Date values like `'2026-04-15'` must be written as complete literals. To compute dates dynamically, use arithmetic: `StartDate + '{n} days'`."
- `time`: "Time values like `'14:30:00'` must be written as complete literals. To compute times dynamically, use arithmetic: `StartTime + '{n} hours'`."
- `instant`: "Instant values must be written as complete literals."
- `datetime`: "DateTime values must be written as complete literals."
- `zoneddatetime`: "ZonedDateTime values must be written as complete literals."
- `timezone`: "Timezone values like `'America/New_York'` must be written as complete literals."

**Rationale for two separate codes:** `InvalidInterpolatedTypedConstantForm` fires when interpolation is supported but the form is wrong (fixable by restructuring). `InterpolationNotSupportedForType` fires when interpolation itself is categorically disallowed for the type (not fixable by restructuring — use a different approach). Different diagnostics enable different fix suggestions.

---

## Multi-Hole Form Handling

### Two-hole business domain forms

**`'{Amt} {Curr}'` for `money`** (pattern M4):
- Segment sequence: `H[magnitude] T(' ') H[currency]`
- Two holes, one space separator
- Each hole is independently type-checked against its slot's compatibility table

**`'{Rate} {Curr}/{Unit}'` for `price`** (pattern P8):
- Segment sequence: `H[magnitude] T(' ') H[currency] T('/') H[unit]`
- Three holes: magnitude, currency, unit
- The `/` between currency and unit is a literal text segment — the parser produces it as a `TextSegment`
- Slot identity is determined by grammar position, not by text content examination

**`'{Rate} {From}/{To}'` for `exchangerate`** (pattern X8):
- Segment sequence: `H[magnitude] T(' ') H[from-currency] T('/') H[to-currency]`
- Three holes, same structural shape as price P8
- The type grammar distinguishes: price has `currency/unit`, exchangerate has `from-currency/to-currency`

### Multi-hole temporal forms

**`'{n} years + {m} months'` for `period`** (pattern Pe5):
- Segment sequence: `H[magnitude₁] T(' years + ') H[magnitude₂] T(' months')`
- Two magnitude holes with compound-period text between them
- Both magnitude slots require `integer` (Decision #28)

**Implementation note:** The ` + ` inside the typed constant is a literal text separator between period components. It is NOT the `+` operator — it appears inside `'...'` and is part of the typed constant content. The parser sees it as text. The type checker recognizes `T(' <unit> + ')` as the compound separator pattern when matching against period/duration grammars.

---

## Type-Grammar Matching Algorithm (Slice 2 Design)

### Overview

The type checker implements `ResolveInterpolatedTypedConstant()` with this flow:

```
1. Determine target type from context (§3.3 resolution)
2. If target type ∈ {date, time, instant, datetime, zoneddatetime, timezone}:
     → emit InterpolationNotSupportedForType, return TypedErrorExpression
3. Extract segment sequence from InterpolatedTypedConstantExpression
4. Normalize segment sequence into abstract form:
     - TextSegments → classify content: numeric literal? unit name? currency code? separator?
     - HoleSegments → H (opaque at this stage)
5. Match abstract sequence against target type's valid-form table
6. If no match → emit InvalidInterpolatedTypedConstantForm, return TypedErrorExpression
7. On match → assign slot identity to each hole
8. For each hole:
     a. Resolve the hole expression (ParseExpression result)
     b. Check resolved type against slot compatibility table
     c. If mismatch → emit InterpolatedTypedConstantHoleTypeMismatch
9. Construct TypedInterpolatedTypedConstant with slot-annotated holes
```

### Text Segment Classification

Text segments are classified to support grammar matching:

| Classification | Recognition | Examples |
|---|---|---|
| `numeric` | Matches numeric literal pattern | `100`, `4.17`, `1.08` |
| `currency-code` | 3 uppercase letters, valid in ISO 4217 registry | `USD`, `EUR` |
| `unit-name` | Valid in UCUM registry or temporal unit name list | `kg`, `each`, `days`, `hours` |
| `separator-space` | Single space `' '` | ` ` |
| `separator-slash` | Single `/` | `/` |
| `compound-bridge` | ` <unit-name> + ` pattern (temporal compound) | ` years + `, ` hours + ` |
| `trailing-unit` | ` <unit-name>` at end of sequence | ` kg`, ` USD`, ` days` |

**Note:** Text segment classification is NOT a lexer concern — it happens in the type checker during grammar matching. The text content has already passed through the lexer as opaque text inside `'...'`. This classification step replaces what static typed constant validators already do (parse the content of `'100 USD'` into magnitude + currency).

### Matching Strategy

The valid-form tables above are finite and small (≤ 8 patterns per type). The matching algorithm is:

1. Count the holes in the segment sequence. This immediately narrows candidates:
   - 1 hole → whole-value or single-slot patterns
   - 2 holes → two-slot patterns
   - 3 holes → three-slot patterns (price/exchangerate only)
2. For each candidate pattern with the matching hole count:
   - Walk the pattern and the actual segment sequence in parallel
   - For `T(...)` pattern elements: check that the actual text segment matches the required content classification
   - For `H[slot]` pattern elements: record the slot assignment
3. First match wins (patterns are non-overlapping within a type's grammar — verified below).

**Non-overlap proof:** Within each type's grammar, patterns with the same hole count differ in their text segment content or arrangement. For example, in `money` with 1 hole: M1 has zero text segments (whole-value), M2 has trailing currency text, M3 has leading numeric text. These are structurally distinct. Compound temporal forms are also distinct — the ` + ` bridge text is unique to compound patterns.

---

## New Language Surface

### Diagnostic Codes

| Code | Name | Value | Severity | Description |
|------|------|-------|----------|-------------|
| New | `InvalidInterpolatedTypedConstantForm` | `120` | Error | Segment sequence does not match any valid interpolated form for the target type |
| New | `InterpolationNotSupportedForType` | `121` | Error | Target type does not support interpolation (formatted temporal types) |
| New | `InterpolatedTypedConstantHoleTypeMismatch` | `122` | Error | Hole expression type is not valid for the assigned slot |

### AST Node (Parser output)
`InterpolatedTypedConstantExpression` — mirrors `InterpolatedStringExpression`

### ExpressionFormKind
`InterpolatedTypedConstant = 15`

### Typed Node (TypeChecker output)
`TypedInterpolatedTypedConstant` — mirrors `TypedInterpolatedString`, carries slot-annotated holes

---

## Implementation Slices

### Slice 1 — Parser (UNCHANGED from frank-16)

**File:** `src/Precept/Pipeline/Parser.Expressions.cs`  
**Method:** `ParseInterpolatedTypedConstant()` at ~line 444  
**Reference:** `ParseInterpolatedString()` at ~lines 399–441

**Work:**
- Rewrite `ParseInterpolatedTypedConstant()` to mirror `ParseInterpolatedString()`
- Consume `TypedConstantStart`, then alternate between `TextSegment` (literal fragments) and `HoleSegment` (expressions), closing on the matching end token
- Add `InterpolatedTypedConstantExpression` record to `src/Precept/Pipeline/ParsedExpression.cs`
- Add `ExpressionFormKind.InterpolatedTypedConstant = 15` to `src/Precept/Language/ExpressionForms.cs`
- Reuse existing `InterpolationSegment`, `HoleSegment`, `TextSegment` types

**What the parser does NOT do:**
- No slot classification — the parser produces raw segments only
- No text content validation — text segments are opaque strings
- No target-type awareness — the parser is context-free at this level

**Tests:** Parser round-trips:
- Single hole: `'{x}'`, `'{x} kg'`, `'100 {x}'`
- Two holes: `'{x} {y}'`, `'{x} {y}/each'`
- Three holes: `'{x} {y}/{z}'`
- Compound temporal: `'{n} days + {m} hours'`
- Expressions in holes: `'{x + 1} kg'`, `'{a.b} USD'`

---

### Slice 2 — Type Checker (REDESIGNED — type-grammar matching)

**Files:**
- `src/Precept/Pipeline/TypeChecker.Expressions.cs` — new `ResolveInterpolatedTypedConstant()`
- `src/Precept/Pipeline/SemanticIndex.cs` — new `TypedInterpolatedTypedConstant` record
- `src/Precept/Pipeline/TypeChecker.cs` — add `TypedInterpolatedTypedConstant` to `ContainsError()`
- `src/Precept/Language/DiagnosticCode.cs` — add codes 120, 121, 122
- `src/Precept/Language/Diagnostics.cs` — add message templates and teachable messages

**Work:**

1. **Add `InterpolatedTypedConstantExpression` case to `Resolve()` dispatch switch** — route to new `ResolveInterpolatedTypedConstant()` method.

2. **Implement `ResolveInterpolatedTypedConstant()`:**
   - Step 1: Obtain target type from `expectedType` context parameter (same mechanism as static typed constant resolution).
   - Step 2: If no target type or target type is `ErrorType`, emit diagnostic, return `TypedErrorExpression`.
   - Step 3: If target type ∈ formatted temporal types, emit `InterpolationNotSupportedForType`, return `TypedErrorExpression`.
   - Step 4: Extract the `ImmutableArray<InterpolationSegment>` from the parsed expression.
   - Step 5: Classify text segments by content (numeric, currency, unit, separator).
   - Step 6: Match the classified segment sequence against the target type's valid-form grammar table.
   - Step 7: If no match, emit `InvalidInterpolatedTypedConstantForm` with the list of valid patterns, return `TypedErrorExpression`.
   - Step 8: On match, iterate over holes with their assigned slot identities. For each:
     - Call `Resolve(holeExpr, ctx, slotExpectedType)` where `slotExpectedType` is advisory (same as the existing `expectedType` threading).
     - Check the resolved expression type against the slot's compatibility table.
     - If `string` → accept (universal escape).
     - If compatible → accept.
     - If incompatible → emit `InterpolatedTypedConstantHoleTypeMismatch`.
   - Step 9: Construct `TypedInterpolatedTypedConstant` with the resolved typed holes and their slot annotations.

3. **Remove the `ResolveLiteral` `TypedConstantStart` stub** — the new `Resolve()` dispatch supersedes it.

4. **Update `ContainsError()`** to handle `TypedInterpolatedTypedConstant`.

5. **Type-grammar table implementation:** The per-type valid-form tables are best represented as static data — an array of pattern descriptors per type. Each pattern descriptor is a sequence of `(SegmentKind, SlotIdentity?)` entries. The matching function walks pattern and actual segments in parallel. This is ~50 lines of matching code plus the static data tables — no complex state machines needed.

**Tests:**

_Structural validity (InvalidInterpolatedTypedConstantForm):_
- `'1 {x} kg'` for `quantity` → structural error (no pattern matches)
- `'{x} {y} {z}'` for `money` → structural error (3 holes, money has max 2)
- `'{x}/{y}'` for `money` → structural error (slash separator, money uses space)
- `'{x} USD EUR'` for `money` → structural error (no pattern with two currency texts)

_Interpolation not supported (InterpolationNotSupportedForType):_
- `'{x}'` for `date` → interpolation not supported
- `'{x}'` for `time` → interpolation not supported
- `'{x}'` for `instant` → interpolation not supported
- `'{x}'` for `datetime` → interpolation not supported
- `'{x}'` for `zoneddatetime` → interpolation not supported
- `'{x}'` for `timezone` → interpolation not supported

_Hole type compatibility (InterpolatedTypedConstantHoleTypeMismatch):_
- `'{b} kg'` where `b` is `boolean` → magnitude slot type mismatch
- `'100 {q}'` where `q` is `quantity` → currency/unit slot type mismatch
- `'{q} USD'` where `q` is `quantity` → magnitude slot type mismatch (quantity in magnitude)
- `'{d} days'` where `d` is `decimal` → temporal magnitude requires integer

_String exception:_
- `'{s} kg'` where `s` is `string` → valid (string in magnitude)
- `'100 {s}'` where `s` is `string` → valid (string in unit)
- `'{s}'` for any type → valid (string in whole-value)

_Valid combinations (positive tests):_
- `'{n} kg'` where `n` is `integer` → valid quantity
- `'{n} kg'` where `n` is `decimal` → valid quantity
- `'{a} {c}'` where `a` is `integer`, `c` is `currency` → valid money
- `'{r} {c}/{u}'` where `r` is `decimal`, `c` is `currency`, `u` is `unitofmeasure` → valid price
- `'{r} {f}/{t}'` where `r` is `decimal`, `f`/`t` are `currency` → valid exchangerate
- `'{n} days'` where `n` is `integer` → valid period/duration
- `'{n} hours + {m} minutes'` where `n`, `m` are `integer` → valid compound duration

_Whole-value forms:_
- `'{m}'` where `m` is `money` → valid money whole-value
- `'{q}'` where `q` is `quantity` → valid quantity whole-value
- `'{d}'` where `d` is `duration` → valid duration whole-value
- `'{p}'` where `p` is `period` → valid period whole-value
- `'{c}'` where `c` is `currency` → valid currency whole-value
- `'{u}'` where `u` is `unitofmeasure` → valid unitofmeasure whole-value

---

### Slice 3 — Completions (unchanged)

**File:** `tools/Precept.LanguageServer/Handlers/CompletionsHandler.cs`

**Work:**
- Add `IsInsideTypedConstantHole()` helper — checks if cursor is inside `{...}` in a typed constant
- When inside a hole, serve field/arg completions filtered to types valid for that hole's slot position
- Slot position is determined by counting preceding segments and matching against the target type's grammar — same logic as the type checker, but in the completion handler
- Fixes open bug I2 (completions inside `{}` holes)

**Tests:** Completions inside holes for quantity, money, price targets — verify only slot-compatible types appear

---

### Slice 4 — Semantic Tokens (unchanged)

**File:** `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs`

**Work:**
- Add `TypedInterpolatedTypedConstant` case to `EnumerateExpressionTree()`
- Walk hole expressions for token classification
- Fixes open bug I3 (semantic highlighting inside `{}` holes)

**Tests:** Token classification inside holes

---

### Slice 5 — Docs/MCP

**Files:**
- `docs/language/precept-language-spec.md` — update §3.6 typed constant interpolation to reference the valid-form grammars; add the three new diagnostic codes to §3.10
- `src/Precept/Language/DiagnosticCode.cs` — verify all three codes have complete metadata (examples, teachable messages)
- No grammar changes needed (lexer already handles holes correctly)
- No MCP tool changes needed (no new catalog entries — diagnostic codes auto-surface through the existing `precept_language` diagnostic lookup)

---

### Slice 6 — Compositional Constraint Propagation (ProofEngine)

**File:** `src/Precept/Pipeline/ProofEngine.cs`  
**Depends on:** Slice 2 (requires `TypedInterpolatedTypedConstant` with slot annotations)  
**Can parallel with:** Slices 3, 4, 5

**Rationale:** The primary use case for interpolated typed constants is compositional typed-literal construction from typed event args: `set Balance = '{Amount} {Code}'` where `Amount as number nonzero`. Without this slice, the proof engine cannot reason that Balance's magnitude is nonzero — producing an unresolved warning on `Total / Balance` even though the source is provably constrained. This contradicts the philosophy ("make invalid configurations structurally impossible") for the most common interpolation scenario.

**Work:**

1. **Add `ProofStrategy.CompositionalConstraint = 6`** to `src/Precept/Pipeline/ProofLedger.cs`.

2. **Implement `TryCompositionalConstraintProof(obligation, semantics)`** — new strategy method (~55 lines):
   - Accept only `NumericProofRequirement` obligations (nonzero, positive, nonnegative).
   - Resolve the target field name from the obligation subject.
   - Iterate `semantics.TransitionRows[].Actions[]` and `semantics.EventHandlers[].Actions[]` to find ALL `TypedInputAction` assignments to that field where `InputExpression` is `TypedInterpolatedTypedConstant`.
   - If no interpolated assignments found → decline (return false).
   - For each interpolated assignment, call `GetSlotSource()` to extract the relevant source expression: magnitude slot if present; whole-value slot if the interpolated constant is a single whole-value hole with no magnitude decomposition.
   - Resolve the source expression to a field name, look up that field's modifiers.
   - Use existing `SatisfactionCovers()` to check whether the source's modifier satisfactions cover the obligation.
   - **Intersection semantics:** ALL assignment paths must satisfy. One path without coverage → conservative failure (return false).

3. **Helper: `FindInterpolatedAssignments(fieldName, semantics)`** (~20 lines) — iterates transition rows and event handlers, collects all `TypedInterpolatedTypedConstant` RHS values assigned to the named field.

4. **Helper: `GetSlotSource(TypedInterpolatedTypedConstant)`** (~15 lines) — iterates slot-annotated holes; returns the `Magnitude` slot source expression if present; if no magnitude slot exists but the interpolated constant is a single whole-value hole, returns that hole's source expression instead. This covers both composite typed-constant construction (magnitude path) and degenerate whole-value interpolation (`'{x}'` pattern).

5. **Integration:** Add `TryCompositionalConstraintProof` call to `TryDischarge()` after S5 (QualifierCompatibility), before the Unresolved fallback.

**What S6 proves:**
- `Total / Balance` where Balance = `'{Amount} {Code}'` with `Amount as number nonzero` → **Proved** (nonzero obligation on Balance discharged via Amount's modifier)
- `ensure Balance > '0 USD'` where Balance's magnitude source is `positive` → **Proved**
- Any numeric proof obligation on a field whose ALL interpolated-assignment magnitude sources carry satisfying modifiers

**What S6 does NOT prove (conservative decline):**
- Fields with ANY non-interpolated assignment (e.g., `set Balance = someOtherField`) — strategy declines entirely
- Non-numeric obligations (presence, qualifier, dimension) — strategy only handles numeric requirements
- Fields where some assignment paths lack modifier coverage on the slot source — intersection fails

> **Why `in`/`of` qualifier constraints do NOT need propagation here:** Qualifier constraints (currency `in`, unit `in`, dimension `of`, etc.) are resolved by S2/S5 directly from the target field's declaration — not from assignment provenance. If `Balance as money in 'USD'`, S5 reads `Balance.DeclaredQualifiers` and never traces back through the interpolated assignment. Adding qualifier propagation in S6 would be redundant with the existing strategy resolution and could introduce conflicting proof paths. What might appear to be a gap here is actually qualifier *inference* (inferring an undeclared qualifier from assignment flow) — a different feature with different complexity that does not belong in this plan.

**LOC estimate:** ~90 lines new code (less than Strategy 4 / FlowNarrowing at ~120 lines)
- `TryCompositionalConstraintProof`: ~55 lines
- `GetSlotSource` helper: ~15 lines
- `FindInterpolatedAssignments` helper: ~20 lines
- Tests: ~10

**Tests (~10):**
- Basic: `nonzero` on magnitude source → numeric obligation discharged
- Multiple paths: 2 transitions both assign from `nonzero` sources → proved
- Mixed paths: 1 of 2 assignments lacks `nonzero` → conservative Unresolved
- Non-interpolated assignment to same field mixed with interpolated → decline
- Whole-value hole (`'{x}'` where x is `money nonzero`) → **proved** (whole-value source's modifier covers numeric obligation)
- Whole-value hole (`'{x}'` where x is bare `money`) → decline (no modifier coverage)
- `positive` covers both `> 0` and `≠ 0` obligations (subsumption via existing `SatisfactionCovers`)
- Non-numeric obligation (presence, qualifier) → strategy declines
- Arg source vs field source: `TypedArgRef` with modifiers from event arg declaration
- `GetSlotSource` returns magnitude slot when present even if whole-value slot also exists

---

## Dependency Order

```
Slice 1 (Parser) — UNCHANGED from frank-16
    ↓
Slice 2 (TypeChecker) — REDESIGNED, type-grammar matching
    ↓
Slice 3 (Completions) ← can parallel with Slice 4 and Slice 6
Slice 4 (SemanticTokens) ← can parallel with Slice 3 and Slice 6
Slice 6 (ProofEngine) ← can parallel with Slices 3, 4, 5
    ↓
Slice 5 (Docs/MCP)
```

---

## Open Bugs Unblocked by This Plan

| Bug | Description | Unblocked after |
|-----|-------------|-----------------|
| I2 | Completions inside `{}` holes | Slice 2 |
| I3 | Semantic highlighting inside `{}` holes | Slice 2 |

---

## Open Questions for Shane

1. **Temporal magnitude widening:** Decision #28 says temporal magnitudes must be integer. Should the `InterpolatedTypedConstantHoleTypeMismatch` diagnostic for `decimal`/`number` in a temporal magnitude slot include a teachable suggestion like "Temporal unit values must be whole numbers. Use `integer` fields for temporal magnitudes"? Or is the generic type-mismatch message sufficient?

2. **Compound temporal with unit holes:** The current plan prohibits unit holes in compound forms (`'{n} {u1} + {m} {u2}'`) because the `+` semantics depend on knowing the unit names. Should this be a Phase 2 feature, or is it permanently out of scope?

3. **Diagnostic code numbering:** I've used 120/121/122. The previous plan used 120 for a single code. Confirm the three-code allocation is acceptable, or suggest a different numbering range.

---

## Gates Before Slice 1

- [ ] Shane approves this revised plan
- [ ] Open questions above are resolved (or deferred with explicit acknowledgment)
