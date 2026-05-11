# Typed Constants & Proof Coverage Plan

**Status:** Awaiting Shane review before Slice 1 (Parser) implementation begins  
**Architect:** Frank (frank-16, frank-18, frank-23 revision; proof audit integration 2026-05-11)  
**Philosophy grounding:** `docs/philosophy.md` — make invalid configurations structurally impossible  
**Supersedes:** Previous position-text plan (frank-16/frank-18); standalone `interpolation-plan.md`  
**Scope:** Interpolated typed constant implementation (Slices 1–6) + Proof engine qualifier coverage gaps (Slices 7–12)  
**Source audits:** `docs/Working/proof-engine-qualifier-audit.md`, `docs/Working/proof-gaps-issues.md`

---

> **This plan is the canonical implementation document for two workstreams:**
>
> 1. **Part A — Interpolated Typed Constants** (§Problem Statement through §Slice 6): Full parser → type checker → completions → semantic tokens → docs → proof engine pipeline for `'{x} kg'`, `'{Amt} {Curr}'`, and related forms.
> 2. **Part B — Proof Engine Qualifier Coverage** (§Proof Engine Qualifier Coverage onward): Surgical fixes to catalog metadata and proof resolution logic to close 14 gaps identified in the exhaustive qualifier-proof audit.

---

## Part A — Interpolated Typed Constants

### Problem Statement

Interpolated typed constants (`'{x} kg'`, `'1 {x}'`, `'{Amt} {Curr}'`) are lexed correctly but completely unimplemented beyond a crash-prevention stub. The parser skips all hole tokens. The type checker maps `TypedConstantStart` to `TypedErrorExpression` with no expression nodes created.

**Lexer (correct):** Tokenizes `{expr}` holes into `TypedConstantStart` / `TypedConstantMiddle` / `TypedConstantEnd` segments.  
**Parser (stub):** `ParseInterpolatedTypedConstant()` at `Parser.Expressions.cs:444` skips all hole tokens — produces flat `LiteralExpression(TypedConstantStart, ...)`. No expression AST nodes.  
**Type checker (crash-safe stub):** `ResolveLiteral` `TypedConstantStart` branch emits `TypeMismatch` diagnostic then returns `TypedErrorExpression` (commit `dd1d8e7f` — D26 crash prevention).

---

## Philosophy Constraint

Precept's core identity: **make invalid configurations structurally impossible, not deferred to runtime.**

A `boolean` in a `quantity` hole that compiles clean is exactly what Precept is built to prevent. There are no "V1 permissive" or "V2 deferred" semantics. All type mismatches must be caught at compile time — no exceptions.

### The `string` Exclusion

`string` is **NOT** valid in any hole position. A `string` field in a typed constant hole produces `InterpolatedTypedConstantHoleTypeMismatch` (code 123) at compile time — the author must convert to the appropriate typed field before using it in a typed constant.

**Rationale:** The inability to statically validate string content is a reason to **reject**, not accept. Typed interpolation composes typed holes — qualifier resolution and proof power collapse once hole text becomes opaque runtime content. If an author has a `string` field that holds a currency code, they must use a properly typed `currency` field instead. This preserves Precept's core guarantee: invalid configurations are structurally impossible, not deferred to runtime.

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
| `magnitude` | `integer`, `decimal`, `number` | Amount component — any numeric type widens to `decimal` (money's backing magnitude). `number` accepted because the widening check is post-resolution (the hole expression resolved to `number`, and `number → decimal` is the explicit `round()` bridge — the type checker's assignment validation handles this, not the slot compatibility table). |
| `currency` | `currency` | Currency code component. Only `currency` can produce a statically validated ISO 4217 code. |
| `whole-value` | `money` | Entire money value. Only `money` (same type) preserves compile-time guarantees. |

#### `quantity`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` | `integer`, `decimal`, `number` | Magnitude component — any numeric type is valid for a quantity's decimal-backed magnitude. |
| `unit` | `unitofmeasure` | Unit name component. Only `unitofmeasure` can produce a statically validated UCUM unit code. **Dimension consistency:** When the hole resolves to `unitofmeasure` via a qualifier-returning accessor (e.g., `f1.unit` where `f1` is `quantity of 'length'`), a post-slot-assignment dimension check compares the source field's declared dimension against the target field's declared dimension. Mismatch emits `DimensionMismatchInUnitSlot`. See §Dimension-Unit Consistency Validation. |
| `whole-value` | `quantity` | Entire quantity value. Only `quantity` (same type) preserves compile-time guarantees. |

#### `price`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` | `integer`, `decimal`, `number` | Rate/amount component. |
| `currency` | `currency` | Numerator currency component. |
| `unit` | `unitofmeasure` | Denominator unit component. **Dimension consistency:** Same dimension check as `quantity` — a `unitofmeasure` hole sourced from a qualifier-returning accessor triggers `DimensionMismatchInUnitSlot` if the source dimension conflicts with the target field's declared dimension. See §Dimension-Unit Consistency Validation. |
| `whole-value` | `price` | Entire price value. Only `price` (same type) preserves compile-time guarantees. |

#### `exchangerate`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` | `integer`, `decimal`, `number` | Rate component. |
| `from-currency` | `currency` | Numerator currency. |
| `to-currency` | `currency` | Denominator currency. |
| `whole-value` | `exchangerate` | Entire exchange rate value. Only `exchangerate` (same type) preserves compile-time guarantees. |

### Temporal Quantity Types

#### `duration`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` (including compound `magnitude₁`..`magnitudeₙ`) | `integer` | Temporal quantities require integer magnitudes (Decision #28). `decimal` and `number` are compile errors — `'3.5 hours'` is invalid. |
| `unit` | `unitofmeasure` | Unit name slot — valid temporal unit names (`hours`, `minutes`, `seconds`) are a subset of `unitofmeasure`. |
| `whole-value` | `duration` | Entire duration value. Only `duration` (same type) preserves compile-time guarantees. |

#### `period`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `magnitude` (including compound `magnitude₁`..`magnitudeₙ`) | `integer` | Same integer requirement as `duration` (Decision #28). |
| `unit` | `unitofmeasure` | Valid temporal unit names (`years`, `months`, `weeks`, `days`, `hours`, `minutes`, `seconds`). |
| `whole-value` | `period` | Entire period value. Only `period` (same type) preserves compile-time guarantees. |

### Single-Component Types

#### `currency`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `whole-value` | `currency` | The entire content is a currency code. Only `currency` preserves compile-time guarantees. |

#### `unitofmeasure`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `whole-value` | `unitofmeasure` | The entire content is a unit name. Only `unitofmeasure` preserves compile-time guarantees. |

#### `dimension`

| Slot | Valid hole types | Rationale |
|------|-----------------|-----------|
| `whole-value` | `dimension` | The entire content is a dimension name. Only `dimension` preserves compile-time guarantees. |

---

## Structural Error: Invalid Interpolated Form

### New Diagnostic Codes

**`InvalidInterpolatedTypedConstantForm = 121`**

Emitted when the segment sequence of an interpolated typed constant does not match any valid pattern for the context-determined type. This is a **structural error** — the arrangement of text and holes is wrong, independent of the types of the hole expressions.

**Message template:** `"Invalid interpolated form for '{type}'. Expected forms: {valid-forms-summary}. The segments '{actual-segments}' do not match any valid pattern."`

**Example triggers:**
- `'1 {x} kg'` for `quantity` → three segments (num, hole, unit) — no quantity pattern has a hole between a magnitude literal and a unit literal. The magnitude is already provided by `1`; there is no semantic slot for a second value between magnitude and unit.
- `'{x} {y} {z}'` for `money` → three holes with two spaces — no money pattern has three components.
- `'{x}/{y}'` for `money` → `/` separator — money uses space separator, not `/`.
- `'{x}'` for `date` → interpolation not supported for formatted temporal types.

**`InterpolationNotSupportedForType = 122`**

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

## Dimension-Unit Consistency Validation

### The Gap

When an interpolated typed constant's unit slot is filled by a member access like `f1.unit`, the slot compatibility check (§Per-Slot Hole Type Compatibility Tables) validates that the expression is `unitofmeasure` — but this is a **TypeKind-only** check. It does not verify that the *dimension* of the extracted unit is compatible with the target field's declared dimension.

**Example:** `field f1 as quantity of 'length'`, `field f2 as quantity of 'mass' default '1 {f1.unit}'`. The hole `f1.unit` resolves to `TypeKind.UnitOfMeasure` and passes the unit-slot type check. But `f1.unit` carries a length unit (because `f1` is `of 'length'`), and `f2` requires mass. This is dimensionally incoherent and must be caught at compile time.

### Why This Is Not a Type System Problem

The `TypedMemberAccess` record stores `ResultType = TypeKind.UnitOfMeasure` — no dimension qualifier flows through accessor return types. `FixedReturnAccessor.ReturnsQualifier` is metadata for the proof engine (signals "which qualifier axis this accessor extracts"), not a type-narrowing mechanism. Enriching accessor return types with dimension provenance (Option A from the analysis) would require a qualified-type concept that does not exist in the type system today — a disproportionate investment for a check that can be done structurally.

### Approach: Structural AST Pattern Match (Option B)

After slot assignment and per-hole type compatibility checks, a dedicated consistency pass examines each `unit`-slot hole whose resolved type is `unitofmeasure`. The check works on the resolved typed AST, not on the type system:

**Pattern match:**
```
hole.ResolvedExpression is TypedMemberAccess {
    ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: QualifierAxis.Unit },
    Object: TypedFieldRef { DeclaredQualifiers: { } sourceQualifiers }
            | TypedArgRef { DeclaredQualifiers: { } sourceQualifiers }
}
```

**Dimension extraction from source:**
```csharp
string? sourceDimension = sourceQualifiers
    .OfType<DeclaredQualifierMeta.Dimension>().Select(q => q.DimensionName).FirstOrDefault()
    ?? sourceQualifiers
    .OfType<DeclaredQualifierMeta.Unit>().Select(q => q.DimensionName).FirstOrDefault();
```

**Dimension extraction from target field:** Same logic applied to the target field's `DeclaredQualifiers` (available from `CheckContext.FieldLookup` via the assignment target).

**Decision logic:**
1. If `sourceDimension` is null → **accept** (source has no declared dimension; cannot statically determine — conservative).
2. If target has no dimension qualifier → **accept** (target is unconstrained).
3. If both are non-null and case-insensitive equal → **accept** (matching dimensions).
4. If both are non-null and differ → **emit `DimensionMismatchInUnitSlot`**.

This reuses the same dimension-extraction logic already present in `ValidateAssignmentQualifiers()` (`TypeChecker.Expressions.cs` ~line 1281) and `QuantityValidator.Validate()` (`QuantityValidator.cs` ~line 30). The pattern match is ~25 lines of new code in `ResolveInterpolatedTypedConstant()`.

### Applicability

The check applies to the `unit` slot of:
- **`quantity`** — `QualifierShape: QS_UnitOrDimension` (patterns Q3, Q4)
- **`price`** — `QualifierShape: QS_CurrencyAndDimension`, unit in denominator position (patterns P4, P5, P7, P8)

It does NOT apply to:
- **`duration`/`period`** unit slots — temporal units (`hours`, `days`, etc.) are a separate namespace from UCUM physical units. Temporal dimension consistency (`period of 'date'` vs `period of 'time'`) operates on the `TemporalDimension` axis, not the physical `Dimension` axis. A temporal unit hole would need different checking, but the surface area is narrow (temporal unit literals are a closed set), and this is out of scope for this plan.
- **`currency`** slots — currency qualifier mismatch is a separate axis (`QualifierAxis.Currency`) already handled for direct assignments by `ValidateAssignmentQualifiers()`. The interpolated currency case is analogous but is tracked separately since it requires the same structural pattern match on `QualifierAxis.Currency` rather than dimension extraction.
- **`string`** holes — `string` is not valid in any hole position; it produces `InterpolatedTypedConstantHoleTypeMismatch` before dimension checking is reached.

### Static Typed Constant Dimension Validation

The static case (`field f2 as quantity of 'mass' default '1 [ft_i]'`) is **already handled**. `QuantityValidator.Validate()` (`src/Precept/Language/QuantityValidator.cs` lines 30–53) extracts the literal unit's dimension via `UnitDimensionHelper.DeriveUnitDimensionName()` and compares it against `DeclaredQualifiers` from the `TypedConstantContext`. The `DeclaredQualifiers` are threaded from `ResolveTypedConstant()` → `TypedConstantContext` → `QuantityValidator.Validate()`. This was verified in the current analysis; the earlier gap assessment in `frank-dimension-proof-propagation.md` was incorrect about the static case.

No additional work is needed for static typed constant dimension validation.

### New Diagnostic

**`DimensionMismatchInUnitSlot = 124`**

Emitted when a unit-slot hole expression carries a dimension that conflicts with the target field's declared dimension. This is a dimension consistency error specific to interpolated typed constant unit slots.

**Message template:** `"Unit from '{sourceFieldName}' has dimension '{sourceDimension}' but target field '{targetFieldName}' requires dimension '{targetDimension}'."`

**Teachable message:** `"The .unit accessor extracts the unit from the source field, but that field's dimension ('{sourceDimension}') does not match the target field's declared dimension ('{targetDimension}'). Use a field with a compatible dimension, or remove the dimension constraint from the target field."`

**Example triggers:**
- `f1.unit` from `quantity of 'length'` in unit slot of `quantity of 'mass'` target
- `f1.unit` from `quantity in 'kg'` (derived dimension = 'mass') in unit slot of `quantity of 'length'` target
- `Arg.unit` from event arg `as quantity of 'length'` in unit slot of `price of 'mass'` target

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
9. For each unit-slot hole that resolved to unitofmeasure:
     → Apply dimension-unit consistency check (§Dimension-Unit Consistency Validation)
     → If dimension mismatch → emit DimensionMismatchInUnitSlot
10. Construct TypedInterpolatedTypedConstant with slot-annotated holes
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
| New | `InvalidInterpolatedTypedConstantForm` | `121` | Error | Segment sequence does not match any valid interpolated form for the target type |
| New | `InterpolationNotSupportedForType` | `122` | Error | Target type does not support interpolation (formatted temporal types) |
| New | `InterpolatedTypedConstantHoleTypeMismatch` | `123` | Error | Hole expression type is not valid for the assigned slot |
| New | `DimensionMismatchInUnitSlot` | `124` | Error | Unit-slot hole carries a dimension that conflicts with the target field's declared dimension |

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
- `src/Precept/Language/DiagnosticCode.cs` — add codes 121, 122, 123, 124
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
     - If `string` → reject with `InterpolatedTypedConstantHoleTypeMismatch` (string is not a valid hole type).
     - If compatible → accept.
     - If incompatible → emit `InterpolatedTypedConstantHoleTypeMismatch`.
   - Step 9: Construct `TypedInterpolatedTypedConstant` with the resolved typed holes and their slot annotations.

3. **Remove the `ResolveLiteral` `TypedConstantStart` stub** — the new `Resolve()` dispatch supersedes it.

4. **Update `ContainsError()`** to handle `TypedInterpolatedTypedConstant`.

5. **Type-grammar table implementation:** The per-type valid-form tables are best represented as static data — an array of pattern descriptors per type. Each pattern descriptor is a sequence of `(SegmentKind, SlotIdentity?)` entries. The matching function walks pattern and actual segments in parallel. This is ~50 lines of matching code plus the static data tables — no complex state machines needed.

6. **Dimension-unit consistency validation** (see §Dimension-Unit Consistency Validation below): After slot assignment and per-hole type compatibility checks (step 8), apply the dimension consistency check to every `unit`-slot hole that resolved to `unitofmeasure`. ~25 lines of checking code.

**Slice 2 LOC estimate:** ~200 lines new code + ~9 dimension-consistency tests on top of existing test count. Breakdown: `ResolveInterpolatedTypedConstant` dispatch + algorithm (~80 lines), type-grammar tables + matching (~50 lines), slot compatibility checking (~30 lines), dimension-unit consistency checking (~25 lines), diagnostic definitions (~15 lines).

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

_Dimension-unit consistency (DimensionMismatchInUnitSlot):_
- `'1 {f1.unit}'` where `f1` is `quantity of 'length'`, target is `quantity of 'mass'` → `DimensionMismatchInUnitSlot` (length ≠ mass)
- `'1 {f1.unit}'` where `f1` is `quantity of 'mass'`, target is `quantity of 'mass'` → no error (matching dimensions)
- `'1 {f1.unit}'` where `f1` is `quantity` (no declared dimension), target is `quantity of 'mass'` → no error (source dimension unknown, cannot statically determine — conservative accept)
- `'1 {f1.unit}'` where `f1` is `quantity of 'length'`, target is `quantity` (no declared dimension) → no error (target dimension unconstrained)
- `'1 {f1.unit}'` where `f1` is `quantity in 'kg'`, target is `quantity of 'length'` → `DimensionMismatchInUnitSlot` (`in 'kg'` produces `DeclaredQualifierMeta.Unit("kg", "mass")`, source dimension = 'mass' ≠ target 'length')
- `'{r} USD/{f1.unit}'` where `f1` is `quantity of 'length'`, target is `price of 'mass'` → `DimensionMismatchInUnitSlot` (same check applies to price unit slot)
- `'1 {s}'` where `s` is `string`, target is `quantity of 'mass'` → `InterpolatedTypedConstantHoleTypeMismatch` (string is not a valid hole type — rejected before dimension check)
- `'1 {u}'` where `u` is bare `unitofmeasure` field, target is `quantity of 'mass'` → no dimension check (`unitofmeasure` fields carry no dimension qualifiers — conservative accept)
- `'1 {Arg.unit}'` where `Arg` is event arg `as quantity of 'length'`, target is `quantity of 'mass'` → `DimensionMismatchInUnitSlot` (works via `TypedArgRef.DeclaredQualifiers`)

_String rejection (InterpolatedTypedConstantHoleTypeMismatch):_
- `'{s} kg'` where `s` is `string` → error (string not valid in magnitude slot)
- `'100 {s}'` where `s` is `string` → error (string not valid in unit slot)
- `'{s}'` where `s` is `string`, for any typed constant type → error (string not valid in whole-value slot)

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

3. ~~**Diagnostic code numbering:** I've used 120/121/122. The previous plan used 120 for a single code. Confirm the three-code allocation is acceptable, or suggest a different numbering range.~~ **Resolved:** `ConflictingModifiers = 120` was added to `DiagnosticCode.cs` after the original plan was written. Codes renumbered to 121/122/123 for the three interpolation diagnostics, 124 for `DimensionMismatchInUnitSlot`. The gap at 120 is intentional — it is already allocated.

---

---

## Part B — Proof Engine Qualifier Coverage

### Executive Summary

An exhaustive audit of all qualifier-proof interactions across the Precept proof engine, type checker, and operations catalog revealed **14 gaps** — 7 critical, 4 significant, 3 minor. The most severe finding: **the currency axis has near-total enforcement failure for money arithmetic and comparison operations.** `MoneyPlusMoney`, `MoneyMinusMoney`, and all 6 money comparison operators declare "same currency required" in their catalog descriptions but carry NO `QualifierCompatibilityProofRequirement`. This means `money in 'USD' + money in 'EUR'` compiles clean — a direct violation of the "invalid configurations structurally impossible" guarantee from `docs/philosophy.md`.

In contrast, the **quantity unit axis** and **price compound axis** are correctly enforced, the **temporal dimension axis** is correctly enforced via `DimensionProofRequirement` (Strategy 2), and the **numeric modifier subsumption** system works correctly with comprehensive test coverage. The architecture is sound — the S1–S5 strategy pipeline, the obligation collection machinery, and the strategy dispatch loop all function correctly. **The root cause is catalog metadata gaps**: operations were declared with textual descriptions noting requirements but the corresponding `ProofRequirements` array entries were never added.

**No structural redesign is needed.** All fixes are surgical additions to catalog metadata (`Operations.cs`), one axis-fallback fix in `ResolveQualifierOnAxis`, one new DU subtype (`QualifierChainProofRequirement`), and one new proof obligation on assignment actions. The proof engine's machinery is correct — it faithfully processes whatever obligations the catalog declares. The catalog is simply silent on money operations.

### Audit Matrix

#### Dimension Qualifiers (`quantity of 'X'`, `price per 'X'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `quantity of 'mass' + quantity of 'mass'` (same dimension, no explicit unit) | ⚠️ False Positive | Operations.cs L496: requires `QualifierAxis.Unit`. Fields with only `DeclaredQualifierMeta.Dimension` have no Unit-axis entry. `ResolveQualifierOnAxis` returns null → obligation UNRESOLVED → spurious diagnostic. |
| `quantity in 'kg' + quantity in 'kg'` (same unit) | ✅ Proved | Strategy 5 resolves both, compares via record equality → proved. |
| `quantity in 'kg' + quantity in 'lb_av'` (different unit, same dimension) | ✅ Caught | `Unit("kg", "Mass") != Unit("lb_av", "Mass")` → UNRESOLVED → diagnostic. |
| `quantity in 'kg' + quantity in 'm'` (different dimension) | ✅ Caught | `Unit("kg", "Mass") != Unit("m", "Length")` → caught. |
| Cross-field assignment `set f2 = f1` (different dimensions) | ✅ Caught | `ValidateAssignmentQualifiers` checks `DimensionCategoryMismatch`. |
| Cross-field assignment via expression `set f2 = f1 + f3` | ⚠️ Silent Gap | `ValidateAssignmentQualifiers` only extracts qualifiers from `TypedFieldRef`, `TypedArgRef`, or `TypedTypedConstant`. Binary expression results carry no qualifier provenance. |
| Static typed constant dimension validation | ✅ Caught | `QuantityValidator.Validate()` checks dimension-to-unit consistency. |
| Interpolated typed constant `.unit` dimension gap | ⚠️ Silent Gap | Known gap — tracked in Part A Slice 2 extension. |
| Guard/rule with mismatched qualifier | ✅ Caught | Operation catalog requirements fire at resolution. |
| `price + price` with matching qualifiers | ✅ Proved | Both `QualifierAxis.Unit` AND `QualifierAxis.Currency` requirements → Strategy 5 discharges both. |
| `price + price` mismatched on one axis | ✅ Caught | Each axis checked independently. |

#### Currency Qualifiers (`money in 'USD'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `money in 'USD' + money in 'USD'` | ⚠️ **Silent Gap** | Operations.cs L422-424: `MoneyPlusMoney` has NO `ProofRequirements`. |
| `money in 'USD' + money in 'EUR'` | ⚠️ **CRITICAL** | Compiles clean. Philosophy violation. |
| `money in 'USD' - money in 'EUR'` | ⚠️ **CRITICAL** | Same — `MoneyMinusMoney` has no proof requirements. |
| `money in 'USD' == money in 'EUR'` | ⚠️ **CRITICAL** | `Match: QualifierMatch.Same` has NO proof enforcement — it is a disambiguation selector only. |
| `money in 'USD' > money in 'EUR'` | ⚠️ **CRITICAL** | Same for all 6 comparison operators (L914-937). |
| `set usdField = eurField` (direct) | ✅ Caught | `ValidateAssignmentQualifiers` compares `DeclaredQualifierMeta.Currency`. |
| `set usdField = usdField + eurField` (expression) | ⚠️ Silent Gap | Binary expression result has no qualifier → passes silently. |
| `money / money` (same currency ratio) | ✅ Proved | Type checker disambiguation selects `MoneyDivideMoneySameCurrency`. |
| `money / money` (cross currency) | ✅ Caught | Type checker selects `MoneyDivideMoneyCrossCurrency` → result is `exchangerate`. |
| Static money typed constant currency mismatch | ✅ Caught | `TypedTypedConstant` extracts `CurrencyEntry` → `ValidateAssignmentQualifiers` catches mismatch. |

#### Exchange Rate Qualifiers (`exchangerate from 'USD' to 'EUR'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `exchangerate from 'USD' to 'EUR' * money in 'GBP'` | ⚠️ **CRITICAL** | No `ProofRequirements` on `ExchangeRateTimesMoney`. No from-currency chain validation. |
| `set rateField = otherRate` (different from/to) | ⚠️ Silent Gap | `ValidateAssignmentQualifiers` missing `FromCurrency`/`ToCurrency` cases. |

#### Numeric Modifiers (`nonzero`, `nonnegative`, `positive`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `a / b` where b has no modifier | ✅ Caught | Division entries carry `NumericProofRequirement` → diagnostic. |
| `a / b` where b is `nonzero` | ✅ Proved | Modifier satisfaction → Strategy 2 discharges. |
| `a / b` where b is `positive` | ✅ Proved | `positive` subsumes `nonzero` via `SatisfactionCovers`. |
| `a / b` where b is `nonnegative` | ✅ Caught | `nonnegative` does NOT subsume `nonzero` — correct. |
| `sqrt(x)` where x is `nonnegative` | ✅ Proved | `>= 0` requirement met. |
| `sqrt(x)` where x is `positive` | ✅ Proved | Subsumption: `> 0` ⊇ `>= 0`. |
| Guard `when b > 0` then `a / b` | ✅ Proved | Strategy 3 extracts guard → subsumes. |
| Guard `when b >= 0` then `a / b` | ✅ Caught | `>= 0` does not subsume `!= 0` — correct. |
| Implied modifiers | ✅ Proved | `attributeField.Modifiers.Concat(attributeField.ImpliedModifiers)` — both walked. |

#### Compound Qualifiers (`price in 'USD' per 'kg'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `price + price` matching both axes | ✅ Proved | Two `QualifierCompatibilityProofRequirement` entries — Unit + Currency. |
| `price + price` mismatched one axis | ✅ Caught | Each axis checked independently. |
| `price * quantity` — dimension chain | ⚠️ **CRITICAL** | No `ProofRequirements` on `PriceTimesQuantity`. |
| `price * period` — temporal chain | ⚠️ Silent Gap | No `ProofRequirements` on `PriceTimesPeriod`. |
| `price * decimal` scaling | ✅ Correct | No qualifier interaction needed. |

#### String Escape Hatch

| Scenario | Status | Evidence |
|----------|--------|----------|
| Bare field (no qualifier) in qualified operation | ✅ Correct | Obligation fires but cannot discharge → correct: must declare qualifiers. |
| Unqualified field in qualifier slot | ✅ Correct | No false obligation generated. |

#### Temporal Qualifiers (`period of 'date'`, `period in 'months'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `date + period of 'date'` | ✅ Proved | `DimensionProofRequirement` → Strategy 2 matches. |
| `date + period of 'time'` | ✅ Caught | Dimension mismatch → diagnostic. |
| `time + period of 'time'` | ✅ Proved | Matches. |
| `PeriodDimension.Any` in compatibility | ✅ Caught | Explicit rejection — locked decision. |
| `period in 'months'` dimension inference | ✅ Proved | `TemporalUnit.DerivedDimension` → works. |

### Gap Inventory

| ID | Category | Scenario | Status | Fix Location | Est. LOC |
|----|----------|----------|--------|--------------|----------|
| G1 | Currency | `money + money` — no currency enforcement | ⚠️ CRITICAL | Operations.cs catalog | 4 |
| G2 | Currency | `money - money` — no currency enforcement | ⚠️ CRITICAL | Operations.cs catalog | 4 |
| G3 | Currency | `money == != < > <= >= money` — no currency enforcement | ⚠️ CRITICAL | Operations.cs catalog | 12 |
| G4 | Currency Chain | `exchangerate * money` — no from-currency validation | ⚠️ CRITICAL | New `QualifierChainProofRequirement` + catalog | 30–50 |
| G5 | Dimension Chain | `price * quantity` — no dimension chain validation | ⚠️ CRITICAL | Same infrastructure as G4 + catalog entry | 4 (after G4) |
| G6 | Dimension | `quantity of 'mass' + quantity of 'mass'` — false positive | ⚠️ False Positive | ProofEngine.cs `ResolveQualifierOnAxis` — axis fallback | 15 |
| G7 | Assignment | Expression results carry no qualifier provenance | ⚠️ Silent Gap | New proof obligation on `set` actions | 50 |
| G8 | Temporal Chain | `price * period` — no temporal dimension chain validation | ⚠️ Silent Gap | Operations.cs catalog (after G4 infra) | 4–8 |
| G9 | ExchangeRate | `ValidateAssignmentQualifiers` missing `FromCurrency`/`ToCurrency` | ⚠️ Silent Gap | TypeChecker.Expressions.cs | 20 |
| G10 | Interpolation | `.unit` accessor drops dimension provenance | ⚠️ Silent Gap | Part A Slice 2 extension (already tracked) | 25 |
| G11 | Modifier Propagation | No proof that `nonneg + nonneg = nonneg` | ❓ By Design | Would require new strategy (S6+) | 80–120 |
| G12 | Price Dimension | `price of 'mass'` — same false-positive risk as G6 | ⚠️ False Positive | Subsumed by G6 fix | 0 |
| G13 | Temporal Chain | `price * duration` — no temporal unit chain validation | ⚠️ Silent Gap | Operations.cs catalog (after G4 infra) | 4–8 |
| G14 | Division Qualifier | `money / money` disambiguation — works correctly | ✅ By Design | N/A | 0 |

**Gap disposition:**
- **G10** — already tracked in Part A Slice 2. No separate slice needed.
- **G11** — by-design limitation. Modifier propagation through arithmetic is out of scope for the current proof engine. No slice.
- **G12** — subsumed by G6 fix. No separate LOC.
- **G14** — working correctly by design. No fix needed.
- **Gaps addressed by new slices:** G1, G2, G3, G4, G5, G6, G7, G8, G9, G13 (10 gaps → 6 slices).

### Implementation Slices for Proof Gaps

---

### Slice 7: Money Currency Enforcement (G1 + G2 + G3)

**Status:** Not Started  
**Goal:** Add `QualifierCompatibilityProofRequirement` on `QualifierAxis.Currency` to all 8 money arithmetic and comparison operations that currently lack currency enforcement.

**Work items:**

1. **Add proof requirements to money arithmetic** — `Operations.cs` L422 (`MoneyPlusMoney`) and L426 (`MoneyMinusMoney`): add `QualifierCompatibilityProofRequirement(ParamSubject(PMoney), ParamSubject(PMoney), QualifierAxis.Currency, "Operands must have matching currency qualifiers")` ~4 LOC
2. **Add proof requirements to money comparisons** — `Operations.cs` L914–L937 (`MoneyEqualsMoney` through `MoneyGreaterThanOrEqualMoney`): same requirement on all 6 comparison entries ~12 LOC
3. **Verify existing Strategy 5 discharges** — no new strategy code needed; `QualifierAxis.Currency` is already handled by the `ResolveQualifierOnAxis` → `QualifierCompatibility` path ~0 LOC

**Tests:**
- `money in 'USD' + money in 'USD'` → proved (same currency)
- `money in 'USD' + money in 'EUR'` → `UnprovedQualifierCompatibility` diagnostic
- `money in 'USD' - money in 'EUR'` → diagnostic
- `money in 'USD' == money in 'EUR'` → diagnostic
- `money in 'USD' > money in 'EUR'` → diagnostic
- `money in 'USD' <= money in 'USD'` → proved
- Bare `money + money` (no qualifiers) → obligation fires, cannot discharge — correct
- Regression: existing quantity/price operations still pass

**LOC estimate:** ~20  
**Risk:** Low — identical pattern to quantity operations that already work.

---

### Slice 8: Qualifier Chain Validation Infrastructure (G4 + G5)

**Status:** Not Started  
**Goal:** Introduce `QualifierChainProofRequirement` DU subtype for cross-type, cross-axis qualifier validation. Use it to enforce exchange rate × money currency chains and price × quantity dimension chains.

**Work items:**

1. **New DU subtype** — `src/Precept/Pipeline/ProofLedger.cs`: `QualifierChainProofRequirement(ProofSubject Left, QualifierAxis LeftAxis, ProofSubject Right, QualifierAxis RightAxis, string Description)` inheriting from `ProofRequirement` ~10 LOC
2. **Strategy 5 extension** — `src/Precept/Pipeline/ProofEngine.cs`: extend `TryQualifierCompatibilityProof` to handle `QualifierChainProofRequirement` by resolving LeftAxis on Left and RightAxis on Right, then comparing the resolved qualifier values ~20 LOC
3. **Catalog entry: `ExchangeRateTimesMoney`** — `Operations.cs` L621: add `QualifierChainProofRequirement(ParamSubject(PExchangeRate), QualifierAxis.FromCurrency, ParamSubject(PMoney), QualifierAxis.Currency, ...)` ~4 LOC
4. **Catalog entry: `PriceTimesQuantity`** — `Operations.cs` L595: add `QualifierChainProofRequirement(ParamSubject(PPrice), QualifierAxis.Unit, ParamSubject(PQuantity), QualifierAxis.Unit, ...)` ~4 LOC
5. **Obligation collection update** — `ProofEngine.cs` obligation walker: ensure `QualifierChainProofRequirement` is collected from operation metadata ~6 LOC
6. **MCP fire pipeline sync** — verify `LanguageTool.cs` `FirePipeline` array still matches ~0 LOC (no new stage)

**Tests:**
- `exchangerate from 'USD' to 'EUR' * money in 'USD'` → proved (from-currency matches)
- `exchangerate from 'USD' to 'EUR' * money in 'GBP'` → diagnostic (USD ≠ GBP)
- `exchangerate from 'USD' to 'EUR' * money in 'EUR'` → diagnostic (from is USD, not EUR)
- `price in 'USD' per 'kg' * quantity of 'mass'` → proved (dimension match)
- `price in 'USD' per 'kg' * quantity of 'length'` → diagnostic (mass ≠ length)
- `price in 'USD' per 'kg' * quantity in 'kg'` → proved (unit-level match → dimension inferred)
- Regression: existing operations unaffected
- Regression: quantity + quantity still works
- Bare `exchangerate * money` (no qualifiers) → obligation fires, cannot discharge
- Bare `price * quantity` (no qualifiers) → same

**LOC estimate:** ~54 (10 + 20 + 4 + 4 + 6 + 0 + overhead)  
**Risk:** Medium — new DU subtype and strategy extension, but follows established patterns.

---

### Slice 9: Dimension-Only Field False Positive Fix (G6)

**Status:** Not Started  
**Goal:** Fix `ResolveQualifierOnAxis` to fall back from `QualifierAxis.Unit` to `QualifierAxis.Dimension` when a field has only a dimension qualifier. Eliminates spurious `UnprovedQualifierCompatibility` diagnostics on `quantity of 'mass' + quantity of 'mass'`.

**Work items:**

1. **Axis fallback logic** — `src/Precept/Pipeline/ProofEngine.cs` `ResolveQualifierOnAxis` (~L898–911): when requested axis is `QualifierAxis.Unit` and no Unit qualifier exists, check for `QualifierAxis.Dimension` and return the Dimension qualifier for compatibility comparison ~15 LOC

**Tests:**
- `quantity of 'mass' + quantity of 'mass'` → proved (same dimension, no explicit unit)
- `quantity of 'mass' + quantity of 'length'` → diagnostic (dimension mismatch)
- `price of 'mass' + price of 'mass'` → proved (G12 subsumed — same fix)
- Regression: `quantity in 'kg' + quantity in 'lb_av'` still caught

**LOC estimate:** ~15  
**Risk:** Low — single method, clear fallback logic.

---

### Slice 10: Assignment Expression Qualifier Propagation (G7)

**Status:** Not Started  
**Goal:** Add proof obligation on `set` actions when the target field has qualifiers and the source is an expression (not a simple field/arg ref). The proof engine becomes the qualifier authority for expression-result assignments.

**Work items:**

1. **Assignment qualifier obligation generation** — `src/Precept/Pipeline/TypeChecker.Expressions.cs` `ValidateAssignmentQualifiers` or new call site: when the assignment source is a binary/unary expression (not `TypedFieldRef`, `TypedArgRef`, or `TypedTypedConstant`) and the target field has `DeclaredQualifiers`, generate a `QualifierCompatibilityProofRequirement` between each operand and the target field ~30 LOC
2. **Proof engine integration** — ensure generated obligations flow through existing Strategy 5 discharge path. The operands of the expression are fields with known qualifiers; Strategy 5 compares them against the target ~10 LOC
3. **Edge case handling** — nested expressions (`a + b + c`), mixed qualified/unqualified operands ~10 LOC

**Tests:**
- `set usdField = eurField + eurField` → diagnostic (EUR operands, USD target)
- `set usdField = usdField + usdField` → proved (matching currencies)
- `set usdField = eurField` (direct ref) → still caught by existing `ValidateAssignmentQualifiers`
- `set usdField = usdField * 2` (scaling) → proved (scaling preserves qualifier)
- `set massField = lengthField + lengthField` → diagnostic (dimension mismatch)
- `set bareMoneyField = eurField + eurField` → no obligation (target has no qualifiers)
- `set usdField = usdField + bareField` → obligation fires, bare side cannot discharge
- Regression: direct ref assignments still work

**LOC estimate:** ~50  
**Risk:** Medium — touches assignment validation flow, requires careful edge-case handling.

---

### Slice 11: Exchange Rate Assignment Qualifier Validation (G9)

**Status:** Not Started  
**Goal:** Add `FromCurrency` and `ToCurrency` cases to the `ValidateAssignmentQualifiers` switch statement so that exchange rate field-to-field assignments with mismatched from/to currencies produce diagnostics.

**Work items:**

1. **Add switch cases** — `src/Precept/Pipeline/TypeChecker.Expressions.cs` `ValidateAssignmentQualifiers` (~L1277–1349): add `case DeclaredQualifierMeta.FromCurrency` and `case DeclaredQualifierMeta.ToCurrency` with the same comparison logic as the existing `Currency` case ~20 LOC

**Tests:**
- `set rate1 = rate2` where `rate1` is `from 'USD' to 'EUR'` and `rate2` is `from 'USD' to 'EUR'` → no diagnostic
- `set rate1 = rate2` where `rate1` is `from 'USD' to 'EUR'` and `rate2` is `from 'GBP' to 'EUR'` → `QualifierMismatch` on from-currency
- `set rate1 = rate2` where `rate1` is `from 'USD' to 'EUR'` and `rate2` is `from 'USD' to 'GBP'` → `QualifierMismatch` on to-currency
- Regression: existing Dimension/Unit/Currency cases unaffected

**LOC estimate:** ~20  
**Risk:** Low — extending an existing switch with identical pattern.

---

### Slice 12: Temporal Chain Validation (G8 + G13)

**Status:** Not Started  
**Depends on:** Slice 8 (requires `QualifierChainProofRequirement` infrastructure)  
**Goal:** Add temporal dimension chain validation to `PriceTimesPeriod` and `PriceTimesDuration` using the infrastructure built in Slice 8.

**Work items:**

1. **Catalog entry: `PriceTimesPeriod`** — `Operations.cs` L599: add `QualifierChainProofRequirement` for temporal dimension axis ~4 LOC
2. **Catalog entry: `PriceTimesDuration`** — `Operations.cs`: add matching requirement ~4 LOC

**Tests:**
- `price in 'USD' per 'month' * period of 'date'` → proved (temporal dimension match)
- `price in 'USD' per 'month' * period of 'time'` → diagnostic (date ≠ time)
- `price in 'USD' per 'hour' * duration` → proved (time dimension match)
- Regression: existing price × period operations still compile when valid

**LOC estimate:** ~8  
**Risk:** Low — pure catalog additions using Slice 8 infrastructure.

---

### Proof Gap Slice Summary

| Slice | Gaps Covered | LOC | Tests | Dependencies |
|-------|-------------|-----|-------|--------------|
| 7 | G1, G2, G3 | ~20 | ~8 | None |
| 8 | G4, G5 | ~54 | ~10 | None |
| 9 | G6, G12 | ~15 | ~4 | None |
| 10 | G7 | ~50 | ~8 | None |
| 11 | G9 | ~20 | ~4 | None |
| 12 | G8, G13 | ~8 | ~4 | Slice 8 |
| **Total** | **10 gaps** | **~167** | **~38** | |

**Not sliced (by design or tracked elsewhere):**
- G10 — tracked in Part A Slice 2 extension (~25 LOC, 9 tests)
- G11 — by-design limitation, deferred
- G14 — working correctly, no fix needed

### Test Coverage Assessment

**Existing coverage:** The proof engine has ~173 test cases across 13 slice classes in `ProofEngineTests.cs`. Well covered: numeric obligations (S1–S4), subsumption chains, guard extraction, flow narrowing, error taint suppression, constraint influence, initial state satisfiability, forwarding facts. Partially covered: qualifier compatibility (record equality, axis values — no E2E obligation-through-discharge tests with real money/quantity expressions). Not covered: currency compatibility on arithmetic, dimension-only field compatibility, exchange rate chain validation, assignment qualifier propagation through expressions.

**New tests estimated:** ~38 tests across Slices 7–12, plus ~6 regression tests = **~44 new tests total** (including regression anchors).

### Proof Engine Architecture Assessment

The S1–S5 strategy architecture is **sound and sufficient**:

| Strategy | Handles | Status |
|----------|---------|--------|
| S1: Literal | Numeric: literal value satisfies threshold | ✅ Correct |
| S2: DeclarationAttribute | Numeric: field modifier; Dimension: temporal period; Modifier; Presence | ✅ Correct |
| S3: GuardInPath | Numeric: guard comparison; Presence: `is set` guard | ✅ Correct |
| S4: FlowNarrowing | Numeric: field-to-field guard implies result sign | ✅ Correct |
| S5: QualifierCompatibility | QualifierAxis equality between operands | ⚠️ Needs axis fallback (G6) + chain extension (G4/G5) |

**Two targeted additions needed** (both within S5's scope):

1. **Axis fallback** (Slice 9, G6): ~15 LOC in `ResolveQualifierOnAxis` to handle Dimension↔Unit equivalence.
2. **Chain validation** (Slice 8, G4/G5): new `QualifierChainProofRequirement` DU subtype + S5 extension for cross-type, cross-axis qualifier matching.

**Root cause:** This is not a proof engine bug. The operations were declared with textual descriptions noting "same currency required" but the corresponding `ProofRequirements` entries were never added. The engine faithfully processes whatever obligations the catalog declares — the catalog was simply silent on money operations. The fix belongs in the catalog, not in the engine. The engine does not need money-specific logic — it needs the catalog to declare what it already describes in prose.

### Proof Gap Dependency Order

```
Slice 7 (Money Currency)     — independent, can start immediately
Slice 8 (Chain Infra)        — independent, can start immediately
Slice 9 (Dimension Fallback) — independent, can start immediately
Slice 10 (Assignment Quals)  — independent, can start immediately
Slice 11 (ExRate Assignment)  — independent, can start immediately
Slice 12 (Temporal Chain)    — DEPENDS ON Slice 8
```

Slices 7–11 can execute in any order. Slice 12 is blocked on Slice 8's `QualifierChainProofRequirement` infrastructure.

All proof gap slices (7–12) are independent of Part A interpolation slices (1–6). The two workstreams can execute in parallel.

---

## Gates

### Part A — Interpolated Typed Constants
- [ ] Shane approves the interpolation plan (Slices 1–6)
- [ ] Open questions above are resolved (or deferred with explicit acknowledgment)

### Part B — Proof Engine Qualifier Coverage
- [ ] Shane reviews proof gap audit findings and slice assignments
- [ ] Priority 1 (Slice 7 — money currency) approved for immediate implementation
