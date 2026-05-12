# Typed Constants & Proof Coverage Plan

**Status:** Part A — ✅ Done (2B confirmed by audit, 2026-05-11) | Part B — Slices 7–11 ✅ Done, Slice 11B ✅ Done, Slice 12 ready to implement | Part C — C1–C4 ✅ Done | Part D — D1–D4 ✅ Done | Part E — E1 + E4 ✅ Done | E2 ✅ Done (`8785d753`) | E3 ✅ Done (`d3f5aa98`) | Part F — F1 ✅ Done | F2 ✅ Done | F3 ✅ Done | **F4 reframed** (policy exists; blocked on H1+H2), F5 blocked on H1+H2 | Part G — G1 ✅ Done (`cb4fbf57`), G2 blocked on H1+H2 | **Part H — BUG-C reframed:** syntax already works; H1 (proof engine axis fix) ready, H2 (sample update) blocked on H1

### Slice Status Tracker

| Slice | Title | Status |
|-------|-------|--------|
| **Part A — Interpolated Typed Constants** | | |
| 1 | Parser — `ParseInterpolatedTypedConstant()` | ✅ Already Implemented (confirmed by code audit, 2026-05-11) |
| 2 | Type Checker — type-grammar matching | ✅ Already Implemented (confirmed by code audit, 2026-05-11) |
| 2B | Type Checker — compound-unit interpolation (`'{A}/{B}'`) | ✅ Done (confirmed by frank-4 audit, 2026-05-11) |
| 3 | Completions — hole-filtered completions | ✅ Already Implemented (confirmed by code audit, 2026-05-11) |
| 4 | Semantic Tokens — hole expression classification | ✅ Already Implemented (confirmed by code audit, 2026-05-11) |
| 5 | Docs/MCP — spec + diagnostic codes | ✅ Already Implemented (confirmed by code audit, 2026-05-11) |
| 6 | ProofEngine — compositional constraint propagation | ✅ Already Implemented (confirmed by code audit, 2026-05-11) |
| **Part B — Proof Engine Qualifier Coverage** | | |
| 7 | Money Currency Enforcement (G1 + G2 + G3) | ✅ Already Implemented |
| 8 | Qualifier Chain Validation Infrastructure (G4 + G5) | ✅ Already Implemented |
| 9 | Dimension-Only Field False Positive Fix (G6) | ✅ Already Implemented |
| 10 | Assignment Expression Qualifier Propagation (G7) | ✅ Already Implemented |
| 11 | Exchange Rate Assignment Qualifier Validation (G9) | ✅ Already Implemented |
| 11B | Temporal Price Denominator Type System Extension (G8 + G13 prereq) | ✅ Complete (2026-05-11) |
| 12 | Temporal Chain Validation (G8 + G13) | 🔲 Not Started — **unblocked by 11B** |
| 13 | Derivation-Direction Chain Proof Analysis (G15) | ⛔ Closed — No Action Required |
| **Part C — Inventory-Item Compile Fixes** | | |
| C1 | Dimension Cancellation in TypeChecker | ✅ Done |
| C2 | Keyword-as-Member-Name: `.from`/`.to` Accessor Fix | ✅ Done |
| C3 | Compound Boolean `=`: Ensure Expression Parser Fix | ✅ Done (`4689145f`) |
| C4 | Proof Engine Interpolated Qualifier Symbolic Equality | ✅ Done (`9040a066`) — PRE0114 73→66 (partial) |
| **Part D — Test Failure Fixes (Pre-Existing)** | | |
| D1 | ConflictingModifiers Fixture Fix (TypeCheckerAssemblyTests) | ✅ Done |
| D2 | ExchangeRate `to` Keyword Qualifier Parser Fix | ✅ Done |
| D3 | LS Manifest Activation Event | ✅ Done |
| D4 | Scalar-Op Qualifier Propagation Fix (reframed) | ✅ Done (`01bc5f0e`) — 4831 tests pass (10 new) |
| **Part E — BUG-A: Inventory-Item PRE0114 Resolution** | | |
| E1 | Shared ParameterMeta Disambiguation in Qualifier Compatibility Proofs | ✅ Done (`d549b4a5`) — 7 targeted tests pass |
| E2 | Interpolated Typed Constant Qualifier Extraction | ✅ Done (`8785d753`) |
| E3 | Subexpression Qualifier Propagation (Currency + Compound Unit Numerator) | ✅ Done (`d3f5aa98`) |
| E4 | Symbolic Qualifier Equivalence for Interpolated Templates | ✅ Done (`d9464ab2`) |
| **Part F — Sample Completeness Fixes** | | |
| F1 | Sample Fix: `optional notempty` → `optional` (8 diagnostics, 6 files) | ✅ Done (no instances in any sample — verified 2026-05-12T11:08:13.750-04:00) |
| F2 | Sample Fix: `number` → `decimal` in travel-reimbursement (2 diagnostics) | ✅ Done (travel-reimbursement has zero diagnostics — verified 2026-05-12T11:08:13.750-04:00) |
| F3 | Compiler Fix: Static Typed Constant Qualifier Extraction (9 diagnostics, 4 files) | ✅ Done (compound-unit slot handling confirmed in TypeChecker.Expressions.TypedConstants.cs) |
| F4 | Compiler Fix: ExchangeRateTimesMoney Result Qualifier Policy (27 diagnostics, inventory-item) | ✅ Done — nested `Total + Rate * Amt` proof now resolves conversion results on the Currency axis |
| F5 | Verification Pass: Recompile all 30 samples, resolve any residual | 🔲 Not Started |
| **Part G — Inventory-Item Proof Coverage Completion** | | |
| G1 | Compound-Unit Interpolated Constant Qualifier Construction (RC1 bug fix in E2) | ✅ Done (`cb4fbf57`) |
| G2 | Compound Expression DivisionByZero Algebraic Proof (ReceiveShipment denominator) | 🔲 Not Started — now the only remaining ReceiveShipment proof issue (lines 214/220/225) |
| **Part H — BUG-C: Interpolated Qualifiers on Event Args** | | |
| H1 | Proof Engine CurrencyConversion Axis Translation | ✅ Done |
| H2 | Sample Update: inventory-item.precept Rate Qualifier | ✅ Done |
| H3 | Verification: Recompile inventory-item After H1 + H2 | ✅ Done — PRE0114 cleared; only PRE0083 remains at lines 214/220/225 |


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

**Status:** ✅ Already Implemented (confirmed by code audit, 2026-05-11)  
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

**Status:** ✅ Already Implemented (confirmed by code audit, 2026-05-11)  
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

### Slice 2B — Type Checker: Compound-Unit Interpolation (`'{A}/{B}'`)

**Status:** Not Started  
**Added:** 2026-05-11, based on Frank's coverage analysis of `samples/inventory-item.precept`

**Gap:** The current type-grammar table for `unitofmeasure` and `quantity` covers single-hole unit patterns but not compound-unit forms with a `/` separator — e.g., `'{StockingUnit}/{PurchaseUnit}'`. This form appears 4 times in `inventory-item.precept` (field declarations L71, L73; rules L123, L124) and will produce `InvalidInterpolatedTypedConstantForm` after Slice 2 ships unless this extension is added.

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`  
**Primary change:** Extend `ResolveInterpolatedTypedConstant()` type-grammar tables.

**New patterns to add:**

_For `unitofmeasure`:_
- `U2: H[numerator-unit] T('/') H[denominator-unit]`
  - Both holes must resolve to `unitofmeasure`
  - Text segment must be exactly `'/'`
  - Dimensional validation: if both source fields have declared dimensions, they may differ (a compound rate unit like `boxes/pallet` is dimensionally valid — it is a ratio of two independent physical dimensions)

_For `quantity`:_
- `Q5: H[magnitude] T(' ') H[numerator-unit] T('/') H[denominator-unit]`
  - First hole: integer or decimal (magnitude)
  - Second/third holes: unitofmeasure
  - Matches `'{n} {StockingUnit}/{PurchaseUnit}'`
- `Q6: H[numerator-unit] T('/') H[denominator-unit]` (whole-value rate-unit form)
  - Both holes: unitofmeasure
  - Matches `'{StockingUnit}/{PurchaseUnit}'` where the expression represents a compound unit reference rather than a quantity literal

**Slot identity additions:**
- `SlotIdentity.NumeratorUnit` — unit in numerator position of a compound-unit pattern
- `SlotIdentity.DenominatorUnit` — unit in denominator position of a compound-unit pattern

**Diagnostic behavior:**
- Both holes must be `unitofmeasure`; any other type → `InterpolatedTypedConstantHoleTypeMismatch`
- `string` in either hole → `InterpolatedTypedConstantHoleTypeMismatch` (same as all other holes)
- No cross-hole dimensional validation for compound-unit patterns — numerator and denominator dimensions are independently declared

**LOC estimate:** ~30 lines new code in the type-grammar table and slot-identity enum; no new diagnostic codes needed (reuses existing codes 121–124).

**Tests:**

_Valid compound-unit forms:_
- `'{A}/{B}'` where `A`, `B` are `unitofmeasure` → valid unitofmeasure compound
- `'{n} {A}/{B}'` where `n` is `integer`, `A`, `B` are `unitofmeasure` → valid quantity
- `'{n} {A}/{B}'` where `n` is `decimal`, `A`, `B` are `unitofmeasure` → valid quantity
- `'{A}/{B}'` where `A` is `quantity of 'length'`, `B` is `quantity of 'mass'` fields (wrong type — `quantity` not `unitofmeasure`) → `InterpolatedTypedConstantHoleTypeMismatch`

_Invalid hole types:_
- `'{s}/{B}'` where `s` is `string` → `InterpolatedTypedConstantHoleTypeMismatch`
- `'{A}/{s}'` where `s` is `string` → `InterpolatedTypedConstantHoleTypeMismatch`
- `'{n}/{B}'` where `n` is `integer` → `InterpolatedTypedConstantHoleTypeMismatch` (integer not valid in unit slot)

_Structural validity:_
- `'{A}/{B}/{C}'` for `unitofmeasure` → `InvalidInterpolatedTypedConstantForm` (no 3-hole unit pattern)
- `'{A}|{B}'` for `unitofmeasure` → `InvalidInterpolatedTypedConstantForm` (pipe is not a valid separator)

**Dependency on Slice 2:** Must be implemented after the core type-grammar matching infrastructure from Slice 2 is in place. This slice extends the grammar tables — it does not change the matching algorithm.

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

**Status:** ✅ Already Implemented (confirmed by proof audit, 2026-05-11)  
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

**Status:** ✅ Already Implemented (confirmed by proof audit, 2026-05-11)  
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

**Status:** ✅ Already Implemented (confirmed by proof audit, 2026-05-11)  
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

**Status:** ✅ Already Implemented (confirmed by proof audit, 2026-05-11)  
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

**Status:** ✅ Already Implemented (confirmed by proof audit, 2026-05-11)  
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

### Slice 11B: Temporal Price Denominator Type System Extension (G8 + G13 Prerequisite)

**Status:** ✅ Complete (2026-05-11)  
**Depends on:** ~~Slice 8~~, ~~Slice 9~~ (both already implemented — unblocked)  
**Blocks:** Slice 12  
**Goal:** Extend the type system so that price fields can carry temporal denominator metadata, enabling chain validation between price and period/duration operands.

**Architect:** Frank  
**Date:** 2026-05-11

---

#### Background: Why Slice 12 Cannot Proceed

George discovered that the original Slice 12 specification was incorrect. The plan assumed `price per 'month'` declares a temporal qualifier axis, but:

1. **No `per` keyword exists.** `TokenKind.Per` is not in the token catalog.
2. **Price uses `QS_CurrencyAndDimension`** — `in` → `QualifierAxis.Currency`, `of` → `QualifierAxis.Dimension` (physical only). No temporal axis.
3. **Period uses `QS_TemporalUnitOrDimension`** — entirely separate temporal axes (`TemporalUnit`, `TemporalDimension`).
4. **Duration has no qualifier shape** — no declared qualifiers at all.
5. **`ExtractComparableValue` has no temporal arms** — returns null for `TemporalDimension` and `TemporalUnit`.

Adding `QualifierChainProofRequirement` entries to `PriceTimesPeriod`/`PriceTimesDuration` without fixing these gaps would break existing valid operations (null comparisons → spurious diagnostics on all price × period arithmetic).

This slice provides the prerequisite type system work that makes Slice 12's catalog entries correct.

---

#### Language Surface Decision: No New Syntax

**Decision:** No `per` keyword. No `TokenKind.Per`. No new qualifier preposition.

**Rationale:** Price temporal denominators are expressible through the existing `of` qualifier — `price of 'time'` (time-dimension denominator: hours/minutes/seconds) and `price of 'date'` (date-dimension denominator: days/weeks/months/years). This parallels the physical case: `price of 'mass'` already means "denominator is in the mass dimension family." The `of` preposition means "denominator dimension category" on price — extending it to accept temporal dimensions is a natural generalization, not a new concept.

The compound literal form `price in 'USD/hours'` also carries temporal denomination in the literal value. Enabling qualifier-level temporal discrimination through `of` is orthogonal to compound literal parsing (which is a separate content-validation concern and out of scope for this slice).

**Alternatives rejected:**

- **`price per 'month'` (new preposition):** Requires a new `TokenKind.Per`, new token catalog entry, parser changes, completions, grammar updates, semantic tokens — a full language surface addition for something the existing `of` preposition already covers semantically. The `of` qualifier on price means "denominator dimension category" — temporal dimensions are a category, not a different concept. Adding `per` would create two ways to say the same thing and violate the uniform qualifier preposition model. `per` also carries "rate" semantics that overlap with price's inherent "per unit" nature, creating redundancy.
- **Generalize `Dimension` axis to bridge physical and temporal (Option B from George):** Conflates two distinct registries (UCUM physical dimensions and NodaTime temporal dimensions) under one axis. Physical dimension names (mass, length, count) and temporal dimension names (date, time) happen to be disjoint today but this is a naming coincidence, not a structural guarantee. Merging the axes would require either a polymorphic `DeclaredQualifierMeta.Dimension` that holds either a string or a `PeriodDimension`, or a union axis that must be pattern-matched everywhere. Both approaches violate the DU principle: each qualifier subtype should carry exactly the fields its consumers need.

**Tradeoff accepted:** Authors must write `price of 'time'` or `price of 'date'` to enable temporal chain validation. Unqualified temporal prices (`price in 'USD'` with a temporal literal like `'4.17 USD/hours'`) will trigger chain proof obligations that cannot discharge — the author must add the `of` qualifier. This is consistent with the physical case: `price in 'USD'` × `quantity of 'mass'` also fails without a dimension qualifier on price.

**Precedent:** The `of` preposition on `period` already accepts temporal dimension values (`period of 'date'`, `period of 'time'`). Extending it to `price` uses the same vocabulary and the same mapper logic.

---

#### Type System Design

**A. `MapDimensionQualifier` temporal extension (~10 LOC)**

In `ExtractQualifiers` (TypeChecker.cs L102), when the qualifier axis is `QualifierAxis.Dimension`, check if the containing type is `TypeKind.Price` and the value is a temporal dimension name. If so, route to `MapTemporalDimensionQualifier` instead of `MapDimensionQualifier`.

```
ExtractQualifiers switch:
  QualifierAxis.Dimension when (resolvedKind == TypeKind.Price && IsTemporalDimensionName(q.Value))
    => MapTemporalDimensionQualifier(qualifier, ctx)
  QualifierAxis.Dimension
    => MapDimensionQualifier(qualifier, ctx)
```

The `resolvedKind` is available via `qualified.InnerType.ResolvedKind` (already computed in the method). `IsTemporalDimensionName` checks for "date" or "time" — the same values `MapTemporalDimensionQualifier` already accepts.

This produces `DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Date/Time)` with `QualifierAxis.TemporalDimension` on the stored metadata. The parser-level axis (`Dimension`) and the stored metadata axis (`TemporalDimension`) differ — the axis fallback in `ResolveQualifierOnAxis` (work item C below) bridges this.

**Why type-gated:** Without the `TypeKind.Price` guard, `quantity of 'time'` would silently produce temporal metadata on a physical type. Quantities are physical measurements — temporal dimension values are invalid on quantity's `of` axis. The guard keeps the temporal acceptance scoped to price, where "denominator dimension category" semantically includes temporal.

**B. `ExtractComparableValue` temporal arms (~4 LOC)**

Add two arms to `ExtractComparableValue` in ProofEngine.cs L928:

```csharp
DeclaredQualifierMeta.TemporalUnit tu       => tu.UnitName,
DeclaredQualifierMeta.TemporalDimension td  => td.Value switch
{
    PeriodDimension.Date => "date",
    PeriodDimension.Time => "time",
    _                    => null,
},
```

`PeriodDimension.Any` returns null — it cannot satisfy a chain comparison (consistent with the existing locked decision that `PeriodDimension.Any` does not satisfy qualifier compatibility, ProofEngine.cs L888–893).

Physical dimension names (mass, length, count) and temporal dimension names (date, time) are disjoint — no collision is possible in `ChainQualifiersMatch`.

**C. `ResolveQualifierOnAxis` — Dimension→TemporalDimension fallback (~8 LOC)**

Extend the existing axis fallback logic in ProofEngine.cs L951–959. After the existing `Unit → Dimension` fallback, add:

```
if axis == QualifierAxis.Dimension:
    fall back to QualifierAxis.TemporalDimension
```

This parallels the Slice 9 pattern (Unit → Dimension fallback). When a chain requirement asks for price's `Dimension` axis and the price field has `TemporalDimension` metadata (from `price of 'time'`), the fallback finds it.

The full fallback chain becomes: `Unit → Dimension → TemporalDimension`. This means a chain requirement using `QualifierAxis.Dimension` will find either physical or temporal dimensions on the subject field. The `ExtractComparableValue` string comparison ensures cross-domain mismatches (physical vs temporal) always fail — the string values are disjoint.

**D. `ImpliedQualifiers` on `TypeMeta` — Duration implicit temporal metadata (~12 LOC)**

Add an optional `ImpliedQualifiers` property to `TypeMeta` (Type.cs L185), analogous to the existing `ImpliedModifiers`:

```csharp
DeclaredQualifierMeta[]? ImpliedQualifiers = null
// with computed property:
public DeclaredQualifierMeta[] ImpliedQualifiers { get; } = ImpliedQualifiers ?? [];
```

Set on Duration's `TypeMeta` entry in Types.cs:

```csharp
ImpliedQualifiers: [new DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Time, QualifierOrigin.Baseline)]
```

This encodes "duration is intrinsically time-dimension" as catalog metadata — no hardcoded duration logic in the proof engine.

Extend `ResolveQualifierOnAxis` to check implied qualifiers after declared qualifiers:

```
foreach (var qual in field.DeclaredQualifiers) { ... }
// After declared qualifier check, check implied:
var typeMeta = Types.GetMeta(field.ResolvedType);
foreach (var qual in typeMeta.ImpliedQualifiers) { if (qual.Axis == axis) return qual; }
```

The `QualifierOrigin.Baseline` origin distinguishes implied qualifiers from explicit/derived ones — consumers that need to differentiate can pattern-match on origin.

**Why this is correct for D15:** Duration cancels only fixed-length denominators (hours, minutes, seconds). The `PeriodDimension.Time` value maps exactly to these fixed-length units (IsCalendarBased = false in `TemporalUnits`). Date-dimension denominators (days, weeks, months, years — `PeriodDimension.Date`) are variable-length and must cancel with `period` only. The chain comparison `price of 'date' * duration` → "date" ≠ "time" → fails. Correct.

---

#### `ExtractComparableValue` Extension Detail

After this slice, `ExtractComparableValue` handles all qualifier subtypes:

| Subtype | Comparable Value | Example |
|---------|-----------------|---------|
| `Currency(code)` | `code` | `"USD"` |
| `FromCurrency(code)` | `code` | `"USD"` |
| `ToCurrency(code)` | `code` | `"EUR"` |
| `Unit(code, dim)` | `code` | `"kg"` |
| `Dimension(name)` | `name` | `"mass"` |
| `TemporalUnit(name, dim)` | `name` | `"hours"` |
| `TemporalDimension(value)` | `"date"` or `"time"` | `"time"` |
| `TemporalDimension(Any)` | `null` | — |
| `Timezone(id)` | `null` (no chain use) | — |

**Matching/subsumption rules:** Strict equality only. `per 'month'` does NOT match `per 'year'` — they are different temporal units. `of 'date'` does NOT match `of 'time'` — they are different temporal dimensions. `PeriodDimension.Any` matches nothing — it cannot satisfy chain proofs (locked decision).

---

#### Proof Chain Coverage

After this slice completes, Slice 12 adds these exact catalog entries:

**G8 — `PriceTimesPeriod`:**

```csharp
OperationKind.PriceTimesPeriod => new BinaryOperationMeta(
    kind, OperatorKind.Times, PPrice, PPeriod, TypeKind.Money,
    "Price × period → money (time-denominator cancellation)", BidirectionalLookup: true,
    ProofRequirements:
    [
        new QualifierChainProofRequirement(
            new ParamSubject(PPrice), QualifierAxis.Dimension,
            new ParamSubject(PPeriod), QualifierAxis.TemporalDimension,
            "Price denominator dimension must match period temporal dimension"),
    ]),
```

Chain logic: `ResolveQualifierOnAxis(Price, Dimension)` → fallback to `TemporalDimension` if price has `of 'time'`/`of 'date'` → `ExtractComparableValue` → "time"/"date". `ResolveQualifierOnAxis(Period, TemporalDimension)` → direct match → `ExtractComparableValue` → "time"/"date". String equality comparison via `ChainQualifiersMatch`.

**G13 — `PriceTimesDuration`:**

```csharp
OperationKind.PriceTimesDuration => new BinaryOperationMeta(
    kind, OperatorKind.Times, PPrice, PDuration, TypeKind.Money,
    "Price × duration → money (hours/min/sec cancellation)", BidirectionalLookup: true,
    ProofRequirements:
    [
        new QualifierChainProofRequirement(
            new ParamSubject(PPrice), QualifierAxis.Dimension,
            new ParamSubject(PDuration), QualifierAxis.TemporalDimension,
            "Price denominator must be time-dimension for duration cancellation"),
    ]),
```

Chain logic: `ResolveQualifierOnAxis(Duration, TemporalDimension)` → declared qualifiers empty → implied qualifiers → `TemporalDimension(Time)` → "time". Price side same as above.

**Validation matrix:**

| Price qualifier | Period qualifier | Expected | Why |
|----------------|-----------------|----------|-----|
| `of 'time'` | `of 'time'` | ✅ Proved | "time" == "time" |
| `of 'time'` | `of 'date'` | ❌ Diagnostic | "time" ≠ "date" |
| `of 'date'` | `of 'date'` | ✅ Proved | "date" == "date" |
| `of 'date'` | `of 'time'` | ❌ Diagnostic | "date" ≠ "time" |
| `of 'mass'` | `of 'date'` | ❌ Diagnostic | "mass" ≠ "date" |
| (no qualifier) | `of 'time'` | ❌ Diagnostic | null left side |
| `of 'time'` | (no qualifier) | ❌ Diagnostic | null right side |
| `of 'time'` | duration (implied) | ✅ Proved | "time" == "time" |
| `of 'date'` | duration (implied) | ❌ Diagnostic | "date" ≠ "time" |
| `of 'mass'` | duration (implied) | ❌ Diagnostic | "mass" ≠ "time" |

---

#### Full Impact Surface

**Runtime:**

| File | Method/Area | Change | LOC |
|------|-------------|--------|-----|
| `src/Precept/Language/Type.cs` | `TypeMeta` record | Add `ImpliedQualifiers` optional parameter + computed property | ~4 |
| `src/Precept/Language/Types.cs` | Duration entry | Add `ImpliedQualifiers: [TemporalDimension(Time, Baseline)]` | ~2 |
| `src/Precept/Pipeline/TypeChecker.cs` | `ExtractQualifiers` L121 switch | Add temporal dimension routing for `Price` + `Dimension` axis | ~8 |
| `src/Precept/Pipeline/ProofEngine.cs` | `ExtractComparableValue` L928 | Add `TemporalUnit` and `TemporalDimension` arms | ~4 |
| `src/Precept/Pipeline/ProofEngine.cs` | `ResolveQualifierOnAxis` L938 | Add `Dimension → TemporalDimension` fallback + implied qualifier check | ~12 |
| **Subtotal** | | | **~30** |

No new diagnostic codes needed — chain validation uses existing `UnprovedQualifierCompatibility` diagnostic. The chain requirement descriptions provide the error message context.

**Tooling:**

| Surface | Impact | Action |
|---------|--------|--------|
| TextMate grammar | None | No new syntax — `of` is already a keyword, `'time'`/`'date'` are typed constant values |
| Completions | Minimal | Price `of` completions should include `'time'` and `'date'` alongside physical dimension names. This is a completions data change in the language server, not a structural change. ~2 LOC in dimension completion provider. |
| Hover | None | Price hover already describes `of` as denominator dimension qualifier |
| Semantic tokens | None | No new token types |

**MCP:**

| Tool | Impact | Action |
|------|--------|--------|
| `precept_types` | Minimal | `ImpliedQualifiers` may appear in type metadata output — verify DTO handles it or add mapping. ~2 LOC if needed. |
| `precept_compile` | None | Compilation output format unchanged — diagnostics flow through existing paths |
| `precept_operations` | None | Operation proof requirements already serialize through existing paths |

---

#### Work Items

1. **Add `ImpliedQualifiers` to `TypeMeta`** — `src/Precept/Language/Type.cs` L185: add optional `DeclaredQualifierMeta[]? ImpliedQualifiers = null` parameter and computed property ~4 LOC
2. **Set Duration's implied temporal dimension** — `src/Precept/Language/Types.cs` Duration entry (~L397): add `ImpliedQualifiers: [new DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Time, QualifierOrigin.Baseline)]` ~2 LOC
3. **Extend `ExtractQualifiers` for temporal dimension routing on price** — `src/Precept/Pipeline/TypeChecker.cs` L121: modify the `QualifierAxis.Dimension` arm to check `resolvedKind == TypeKind.Price && IsTemporalDimensionName(q.Value)` and route to `MapTemporalDimensionQualifier` ~8 LOC
4. **Add temporal arms to `ExtractComparableValue`** — `src/Precept/Pipeline/ProofEngine.cs` L928: add `TemporalUnit` and `TemporalDimension` pattern arms ~4 LOC
5. **Extend `ResolveQualifierOnAxis` with Dimension→TemporalDimension fallback and implied qualifier support** — `src/Precept/Pipeline/ProofEngine.cs` L938: add second fallback after Unit→Dimension, plus implied qualifier loop at end ~12 LOC

**Tests (8 scenarios):**

1. `field Rate as price in 'USD' of 'time'` → compiles clean, stores `TemporalDimension(Time)` in declared qualifiers
2. `field Rate as price in 'USD' of 'date'` → compiles clean, stores `TemporalDimension(Date)` in declared qualifiers
3. `field Rate as price in 'USD' of 'mass'` → compiles clean (unchanged physical dimension behavior)
4. `field Qty as quantity of 'time'` → emits `InvalidDimensionString` (temporal dimensions rejected on quantity — unchanged)
5. `ExtractComparableValue(TemporalUnit("hours", Time))` → `"hours"` (unit test)
6. `ExtractComparableValue(TemporalDimension(Time))` → `"time"` (unit test)
7. `ExtractComparableValue(TemporalDimension(Any))` → `null` (unit test)
8. `ResolveQualifierOnAxis(durationField, TemporalDimension)` → returns implied `TemporalDimension(Time)` (unit test)

**Regression anchors:**
- All existing price + price qualifier compatibility tests pass
- All existing price × quantity dimension chain tests pass (Slice 8, if already in)
- All existing quantity `of` / `in` qualifier tests pass (temporal routing is price-gated)
- All existing period `of 'date'` / `of 'time'` qualifier tests pass
- All existing Unit → Dimension fallback tests pass (Slice 9, if already in)

**LOC estimate:** ~30 (implementation) + ~50 (tests) = ~80 total  
**Risk:** Medium — touches TypeMeta record (public API), TypeChecker qualifier routing, and ProofEngine resolution. Changes are additive (new property, new arms, new fallbacks) with no modifications to existing code paths.

---

### Slice 12: Temporal Chain Validation (G8 + G13) — REVISED

**Status:** Not Started  
**Depends on:** ~~Slice 8~~ (already implemented), Slice 11B (requires temporal price denominator support)  
**Goal:** Add temporal dimension chain validation to `PriceTimesPeriod` and `PriceTimesDuration` using the type system extensions from Slice 11B and the chain infrastructure from Slice 8.

**Work items:**

1. **Catalog entry: `PriceTimesPeriod`** — `src/Precept/Language/Operations.cs` L615: add `QualifierChainProofRequirement(ParamSubject(PPrice), QualifierAxis.Dimension, ParamSubject(PPeriod), QualifierAxis.TemporalDimension, "Price denominator dimension must match period temporal dimension")` ~4 LOC
2. **Catalog entry: `PriceTimesDuration`** — `src/Precept/Language/Operations.cs` L619: add `QualifierChainProofRequirement(ParamSubject(PPrice), QualifierAxis.Dimension, ParamSubject(PDuration), QualifierAxis.TemporalDimension, "Price denominator must be time-dimension for duration cancellation")` ~4 LOC

**Tests:**
- `price of 'time' * period of 'time'` → proved (temporal dimension match)
- `price of 'date' * period of 'date'` → proved (temporal dimension match)
- `price of 'time' * period of 'date'` → diagnostic (time ≠ date)
- `price of 'date' * period of 'time'` → diagnostic (date ≠ time)
- `price of 'mass' * period of 'date'` → diagnostic (physical ≠ temporal)
- `price of 'time' * duration` → proved (time == time via implied qualifier)
- `price of 'date' * duration` → diagnostic (date ≠ time — D15 correct: duration only cancels fixed-length)
- `price of 'mass' * duration` → diagnostic (mass ≠ time)
- bare `price * period` (no qualifiers) → obligation fires, cannot discharge
- bare `price * duration` (no qualifiers) → obligation fires (price side null), cannot discharge
- Regression: existing `price * decimal` scaling unaffected (no chain requirement)
- Regression: existing `price ± price` qualifier compatibility unaffected

**LOC estimate:** ~8  
**Risk:** Low — pure catalog additions using Slice 8 infrastructure and Slice 11B type system support.

---

### Slice 13: Derivation-Direction Chain Proof Analysis (G15)

**Status:** Closed — No Action Required  
**Depends on:** Slice 11B (temporal infrastructure), Slice 12 (chain proof pattern)  
**Goal:** Determine whether `MoneyDivideQuantity`, `MoneyDividePeriod`, and `MoneyDivideDuration` need `QualifierChainProofRequirement` entries to enforce qualifier compatibility, mirroring the multiplication-direction chain proofs on `PriceTimesQuantity`, `PriceTimesPeriod`, and `PriceTimesDuration`.

**Architect:** Frank  
**Date:** 2026-05-11

---

#### Background: Why This Was Flagged

The spec coverage audit (frank-9) identified G15 as a gap: the multiplication-direction operations (`PriceTimesQuantity`, `PriceTimesPeriod`, `PriceTimesDuration`) carry `QualifierChainProofRequirement` entries that validate qualifier compatibility for dimensional cancellation, but the derivation-direction operations (`MoneyDivideQuantity`, `MoneyDividePeriod`, `MoneyDivideDuration`) carry only `NumericProofRequirement` (divisor non-zero). The question: should derivation-direction operations have matching chain proofs?

**Canonical authority:** `business-domain-types.md` L365–367 defines these operations:

| Operation | Result | Semantics |
|-----------|--------|-----------|
| `money / quantity` | `price` | Currency / non-currency → price derivation (L365) |
| `money / period` | `price` | Time-based price derivation (L366, D15) |
| `money / duration` | `price` | Duration-based price derivation (L367, D15) |

**Current Operations.cs state:**

| Operation | Location | Current ProofRequirements |
|-----------|----------|--------------------------|
| `MoneyDivideQuantity` | L473–480 | `NumericProofRequirement` (non-zero) only |
| `MoneyDividePeriod` | L482–489 | `NumericProofRequirement` (non-zero) only |
| `MoneyDivideDuration` | L491–498 | `NumericProofRequirement` (non-zero) only |

---

#### Analysis: Chain Proofs Are Structurally Impossible

Multiplication-direction chain proofs validate that two operands' qualifiers are compatible for **dimensional cancellation**. The proof compares qualifier values from shared or fallback-bridged axes:

```
PriceTimesQuantity:  Price.Dimension ←→ Quantity.Dimension     // both have Dimension axis
PriceTimesPeriod:    Price.Dimension ←→ Period.TemporalDimension // Dimension→TemporalDimension fallback
PriceTimesDuration:  Price.Dimension ←→ Duration.TemporalDimension // via ImpliedQualifiers
```

Derivation-direction operations have a fundamentally different qualifier topology — the operands do **not** share qualifier axes:

| Operation | Left operand axes | Right operand axes | Shared axis? |
|-----------|------------------|--------------------|-------------|
| `money ÷ quantity` | `Currency` | `Dimension`, `Unit` | ❌ None |
| `money ÷ period` | `Currency` | `TemporalDimension`, `TemporalUnit` | ❌ None |
| `money ÷ duration` | `Currency` | (implied `TemporalDimension` via 11B) | ❌ None |

Adding `QualifierChainProofRequirement` to any of these operations would cause `ResolveQualifierOnAxis` to return `null` for one side (money has no dimension axis; quantity/period/duration have no currency axis). The chain proof would **always fail**, making every derivation operation produce a spurious unresolvable obligation. This is not "needs implementation" — it is **architecturally impossible** under the current chain proof model.

**Why derivation differs from cancellation:** Cancellation validates that a pre-existing denominator dimension matches the cancelling operand's dimension (price `of 'mass'` × quantity `of 'mass'` → ✓). Derivation **creates** the relationship — the result price's denominator IS the divisor's dimension. There is no pre-existing qualifier to validate against, so there is no chain to check.

---

#### Gap Closure: Slice 10 Assignment Validation

The practical concern — "what if `money in 'USD' ÷ quantity of 'length'` is assigned to `price in 'USD' of 'mass'`?" — is already covered by Slice 10 (assignment expression qualifier propagation, **already implemented**).

`ValidateAssignmentQualifiers` (TypeChecker.Expressions.cs L1261–1268) extracts leaf operands from binary expressions and validates each leaf's qualifiers against the target field:

```
set Rate = Revenue / Qty
  → leaf Revenue (money in 'USD') checked against Rate (price in 'EUR' of 'mass')
    → Currency: 'USD' ≠ 'EUR' → QualifierMismatch diagnostic ✓
  → leaf Qty (quantity of 'length') checked against Rate (price in 'USD' of 'mass')
    → Dimension: 'length' ≠ 'mass' → DimensionCategoryMismatch diagnostic ✓
```

Every derivation-direction qualifier mismatch that could produce a semantically incoherent assignment is caught at assignment time. No operation-level chain proof adds defensive value beyond what Slice 10 already provides.

---

#### Conclusion

**G15 is a false gap.** Derivation-direction operations create qualifier relationships; they do not cancel pre-existing ones. The operands share no qualifier axes, making `QualifierChainProofRequirement` structurally impossible without introducing a new proof requirement kind. Assignment validation (Slice 10, already implemented) provides full coverage for the practical scenarios.

**No code changes. No new proof requirements. No new tests.**

**LOC estimate:** 0  
**Risk:** None — verification-only slice.

---

### Proof Gap Slice Summary

| Slice | Gaps Covered | LOC | Tests | Dependencies |
|-------|-------------|-----|-------|--------------|
| 7 | G1, G2, G3 | ~20 | ~8 | None |
| 8 | G4, G5 | ~54 | ~10 | None |
| 9 | G6, G12 | ~15 | ~4 | None |
| 10 | G7 | ~50 | ~8 | None |
| 11 | G9 | ~20 | ~4 | None |
| 11B | G8, G13 prereq | ~30 | ~8 | Slice 8, Slice 9 |
| 12 | G8, G13 | ~8 | ~12 | Slice 8, Slice 11B |
| 13 | G15 | 0 | 0 | Slice 11B, Slice 12 (analysis only) |
| **Total** | **11 gaps** | **~197** | **~54** | |

**Not sliced (by design or tracked elsewhere):**
- G10 — tracked in Part A Slice 2 extension (~25 LOC, 9 tests)
- G11 — by-design limitation, deferred
- G14 — working correctly, no fix needed
- G15 — false gap, closed by design + Slice 10 assignment validation (see Slice 13)

### Test Coverage Assessment

**Existing coverage:** The proof engine has ~173 test cases across 13 slice classes in `ProofEngineTests.cs`. Well covered: numeric obligations (S1–S4), subsumption chains, guard extraction, flow narrowing, error taint suppression, constraint influence, initial state satisfiability, forwarding facts. Partially covered: qualifier compatibility (record equality, axis values — no E2E obligation-through-discharge tests with real money/quantity expressions). Not covered: currency compatibility on arithmetic, dimension-only field compatibility, exchange rate chain validation, assignment qualifier propagation through expressions.

**New tests estimated:** ~54 tests across Slices 7–12 (including 11B), plus ~10 regression tests = **~64 new tests total** (including regression anchors).

### Proof Engine Architecture Assessment

The S1–S5 strategy architecture is **sound and sufficient**:

| Strategy | Handles | Status |
|----------|---------|--------|
| S1: Literal | Numeric: literal value satisfies threshold | ✅ Correct |
| S2: DeclarationAttribute | Numeric: field modifier; Dimension: temporal period; Modifier; Presence | ✅ Correct |
| S3: GuardInPath | Numeric: guard comparison; Presence: `is set` guard | ✅ Correct |
| S4: FlowNarrowing | Numeric: field-to-field guard implies result sign | ✅ Correct |
| S5: QualifierCompatibility | QualifierAxis equality between operands | ⚠️ Needs axis fallback (G6, 11B) + chain extension (G4/G5) + implied qualifiers (11B) |

**Three targeted additions needed** (all within S5's scope):

1. **Axis fallback** (Slice 9, G6): ~15 LOC in `ResolveQualifierOnAxis` to handle Unit→Dimension equivalence.
2. **Chain validation** (Slice 8, G4/G5): new `QualifierChainProofRequirement` DU subtype + S5 extension for cross-type, cross-axis qualifier matching.
3. **Temporal price denominator support** (Slice 11B, G8/G13 prereq): `Dimension→TemporalDimension` axis fallback, `ExtractComparableValue` temporal arms, `ImpliedQualifiers` on `TypeMeta` for duration's intrinsic time dimension, and type-gated temporal dimension acceptance on price's `of` qualifier.

**Root cause:** This is not a proof engine bug. The operations were declared with textual descriptions noting "same currency required" but the corresponding `ProofRequirements` entries were never added. The engine faithfully processes whatever obligations the catalog declares — the catalog was simply silent on money operations. The fix belongs in the catalog, not in the engine. The engine does not need money-specific logic — it needs the catalog to declare what it already describes in prose. The temporal chain gap (G8/G13) is a structural gap — the price type lacked the qualifier axis to carry temporal denominator information. Slice 11B provides the type system foundation; Slice 12 provides the catalog entries.

### Proof Gap Dependency Order

```
Slice 7 (Money Currency)     — independent, can start immediately
Slice 8 (Chain Infra)        — independent, can start immediately
Slice 9 (Dimension Fallback) — independent, can start immediately
Slice 10 (Assignment Quals)  — independent, can start immediately
Slice 11 (ExRate Assignment)  — independent, can start immediately
Slice 11B (Temporal Price)   — DEPENDS ON Slice 8, Slice 9
Slice 12 (Temporal Chain)    — DEPENDS ON Slice 8, Slice 11B
Slice 13 (Derivation Chain)  — DEPENDS ON Slice 11B, Slice 12 (analysis only — no code changes)
```

Slices 7–11 can execute in any order. Slice 11B requires Slice 8 (chain infrastructure) and Slice 9 (axis fallback pattern). Slice 12 requires Slice 8 and Slice 11B. Slice 13 is a verification-only slice — concluded G15 is a false gap (see Slice 13 analysis).

All proof gap slices (7–12, including 11B) are independent of Part A interpolation slices (1–6). The two workstreams can execute in parallel.

---

## Gates

### Part A — Interpolated Typed Constants
- [ ] Shane approves the interpolation plan (Slices 1–6)
- [ ] Open questions above are resolved (or deferred with explicit acknowledgment)

### Part B — Proof Engine Qualifier Coverage
- [ ] Shane reviews proof gap audit findings and slice assignments
- [ ] Priority 1 (Slice 7 — money currency) approved for immediate implementation

### Part C — Inventory-Item Compile Fixes
- [ ] Shane reviews C2–C4 designs and approves for implementation
- [ ] C1 (George's in-flight RC-3 work) completes and merges

### Part D — Test Failure Fixes
- [ ] D1–D4 reviewed and approved for implementation

---

## Part C — Inventory-Item Compile Fixes

**Added:** 2026-05-12  
**Architect:** Frank  
**Source:** Deep-dive analysis of `samples/inventory-item.precept` — 4 remaining compiler gaps producing ~80 diagnostics. RC-1 (parser qualifier acceptance) and RC-2 (compound-unit patterns) are tracked in Part A. These slices cover the remaining blockers.  
**Scope:** 1 TypeChecker extension (dimension cancellation), 1 parser fix (keyword-as-member-name), 1 sample fix + parser diagnostic improvement (`=` vs `==`), 1 ProofEngine extension (arg qualifier resolution for interpolated qualifiers).

All C-slices are independent of Parts A and B (except C1, which extends Part A's type-grammar work). C2–C4 can execute in any order and in parallel.

---

### Slice C1 — Dimension Cancellation in TypeChecker (`qty[A/B] × qty[B] → qty[A]`)

**Status:** 🔄 In Progress (George)  
**Added:** 2026-05-12  
**Depends on:** Slice 2B (compound-unit interpolation patterns)  
**Blocks:** None

**Gap:** The `QuantityTimesQuantity` operation in the Operations catalog (Operations.cs L570–572) is declared with the description "dimensional cancellation" but no implementation exists to compute the result dimension from operand dimensions. When `qty of 'mass/length' × qty of 'length'` is evaluated, the result should be `qty of 'mass'` — the shared denominator dimension cancels against the right operand's dimension. Without this, compound-unit arithmetic in `inventory-item.precept` (conversion factor × purchase quantity) cannot type-check with correct result dimensions.

**Files:**

| File | Change |
|------|--------|
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | Extend `CreateResolvedBinaryOp()` or `ResolveBinaryResultType()` to compute result qualifier when operation is `QuantityTimesQuantity` and both operands carry dimension qualifiers. New helper: dimension string parsing for compound `A/B` forms, cancellation logic. |
| `src/Precept/Language/Operations.cs` | Add `QualifierChainProofRequirement` or new `DimensionCancellationProofRequirement` to `QuantityTimesQuantity` operation meta to validate denominator compatibility at proof time. |
| `src/Precept/Pipeline/ProofEngine.cs` | If a new proof requirement type is added, extend `TryQualifierCompatibilityProof()` to handle dimension cancellation matching (denominator of left operand equals dimension of right operand). |
| `src/Precept/Pipeline/SemanticIndex.cs` | Possibly add `ComputedQualifier` or `CancelledDimensionQualifier` binding subtype if result qualifier identity needs to be tracked through the semantic index. |

**Tests (expected):**
- `qty of 'mass/length' × qty of 'length'` → result `qty of 'mass'` ✓
- `qty of 'length' × qty of 'length'` → result `qty of 'length²'` or structural error (design decision pending)
- `qty of 'mass' × qty of 'length'` (no cancellation possible — independent dimensions) → result dimension behavior TBD
- Reverse order: `qty of 'length' × qty of 'mass/length'` → result `qty of 'mass'` ✓
- Mismatched denominator: `qty of 'mass/length' × qty of 'mass'` → proof failure (denominator `length` ≠ `mass`)

**Downstream Impact:**
- Runtime: N/A (declaration-time only)
- Tooling: N/A — no completions/semantic-token changes; hover may show computed result dimensions
- MCP: N/A — no new tool surface

---

### Slice C2 — Keyword-as-Member-Name: `.from`/`.to` Accessor Fix

**Status:** 🔲 Not Started  
**Added:** 2026-05-12  
**Depends on:** None  
**Blocks:** None

#### Root Cause

The `exchangerate` type declares accessors named `from` and `to` (Types.cs, ExchangeRate entry, `FixedReturnAccessor("from", ...)` and `FixedReturnAccessor("to", ...)`). These names collide with keywords `TokenKind.From` (preposition: "exit-gate ensure / transition source") and `TokenKind.To` (preposition: "entry-gate ensure / transition target").

**Parse failure chain:**
1. **Lexer:** Text `from`/`to` always tokenized as `TokenKind.From`/`TokenKind.To` (keywords, not identifiers). The lexer has no context-sensitivity — `_keywordLookup` maps unconditionally.
2. **Parser member access:** `ParseMemberAccessOrMethodCall()` (Parser.Expressions.cs L318–340) checks `memberToken.Kind == TokenKind.Identifier || IsMemberNameToken(memberToken.Kind)`.
3. **`IsMemberNameToken()`** (Parser.Expressions.cs L342–343) delegates to `Parser.KeywordsValidAsMemberName.Contains(kind)`.
4. **Circular initialization:** `Parser.KeywordsValidAsMemberName` (Parser.cs L30–34) is built by filtering `Tokens.All` for `meta.IsValidAsMemberName`. But `TokenMeta.IsValidAsMemberName` (Token.cs L68) is a **computed property** that reads `Tokens.KeywordsValidAsMemberName` — creating a circular dependency. The catalog-driven checklist (item 7) says "Set `IsValidAsMemberName: true` in `Tokens.GetMeta`" — but `IsValidAsMemberName` is currently a computed property, not a constructor parameter.

**Symptom:** `Rate.from` and `Rate.to` produce PRE0009 ("expected member name") at `samples/inventory-item.precept` lines 173–174.

#### Design

The fix is **catalog-driven** per the checklist (item 7). The circular dependency must be broken by making `IsValidAsMemberName` a **constructor parameter** on `TokenMeta` instead of a computed property.

**Work:**

**A. `src/Precept/Language/Token.cs` — Make `IsValidAsMemberName` a constructor parameter (~3 LOC)**

Change `TokenMeta` record:
```csharp
// BEFORE (L68):
public bool IsValidAsMemberName => Tokens.KeywordsValidAsMemberName.Contains(Kind);

// AFTER:
bool IsValidAsMemberName = false    // constructor parameter with default
```

This breaks the circular dependency. `IsValidAsMemberName` becomes catalog metadata set at definition time, not a runtime computation.

**B. `src/Precept/Language/Tokens.cs` — Derive `IsValidAsMemberName` from type accessor metadata (~15 LOC)**

`Tokens.KeywordsValidAsMemberName` (L505–513) already computes the correct set from `Types.All.SelectMany(meta => meta.Accessors)`. The initialization needs to be restructured:

1. Compute the keyword-as-member-name set **first** (eagerly, before `GetMeta` calls need it).
2. In `GetMeta` for `TokenKind.From` and `TokenKind.To`, set `IsValidAsMemberName: true`.

The cleanest approach: keep `Tokens.KeywordsValidAsMemberName` derived from `Types.All` accessor names (the canonical source), but **also** set `IsValidAsMemberName` on each affected `TokenMeta` entry. This means `GetMeta` entries for `From` and `To` gain `IsValidAsMemberName: true`.

The set `Tokens.KeywordsValidAsMemberName` becomes a **validation/documentation artifact** (confirming which tokens are marked), while the `Parser.KeywordsValidAsMemberName` set drives parser behavior from the per-token metadata.

**Alternatively** (simpler, preferred): Remove `Tokens.KeywordsValidAsMemberName` entirely. Change `Parser.KeywordsValidAsMemberName` to compute directly from type accessors at parser init:
```csharp
public static FrozenSet<TokenKind> KeywordsValidAsMemberName { get; } =
    Types.All
        .SelectMany(meta => meta.Accessors)
        .Select(a => a.Name)
        .Distinct(StringComparer.Ordinal)
        .Select(name => Tokens.Keywords.TryGetValue(name, out var kind) ? kind : (TokenKind?)null)
        .Where(kind => kind is not null)
        .Select(kind => kind!.Value)
        .ToFrozenSet();
```

This eliminates the circular dependency entirely: `Parser.KeywordsValidAsMemberName` derives from `Types.All` (accessor names) → `Tokens.Keywords` (text → TokenKind mapping). No dependency on `TokenMeta.IsValidAsMemberName`. The `TokenMeta.IsValidAsMemberName` property then becomes either (a) removed, or (b) set as a parameter in `GetMeta` and used only for non-parser consumers.

**C. No Lexer changes.** The lexer remains context-free. Keywords stay as keywords. The parser handles context-sensitivity.

**D. No Parser changes beyond the set derivation.** `IsMemberNameToken()` and `ParseMemberAccessOrMethodCall()` already work correctly — they just need the set to contain `From` and `To`.

**Files:**

| File | Change | LOC |
|------|--------|-----|
| `src/Precept/Language/Token.cs` | Make `IsValidAsMemberName` a constructor parameter (or remove it) | ~3 |
| `src/Precept/Language/Tokens.cs` | Remove `KeywordsValidAsMemberName` (move derivation to Parser) or keep as validation set | ~5 |
| `src/Precept/Pipeline/Parser.cs` | Update `KeywordsValidAsMemberName` derivation to use Types.All accessor names directly | ~10 |
| `samples/inventory-item.precept` | Lines 173–174 should now compile (no sample change needed) | 0 |

**LOC estimate:** ~18 lines changed

**Tests:**

_Parser tests (new):_
- `Rate.from` where `Rate` is `exchangerate` → parses as `MemberAccessExpression` with member name `"from"` ✓
- `Rate.to` where `Rate` is `exchangerate` → parses as `MemberAccessExpression` with member name `"to"` ✓
- `transition Active from Inactive` → `from` still parses as keyword (not confused by member-name allowance) ✓
- `in Listed ensure ... from SomeState` → `from` in non-member-access position remains a keyword ✓
- `SomeField.amount` → non-keyword accessor still works ✓

_Type checker tests (new):_
- `Rate.from` on `exchangerate` field → resolves to `TypeKind.Currency` ✓
- `Rate.to` on `exchangerate` field → resolves to `TypeKind.Currency` ✓
- `Rate.from = SupplierCurrency` in ensure → valid currency equality comparison ✓

_Regression tests:_
- All existing `from`/`to` keyword usages (transition sources, ensure exit-gates) remain valid
- `KeywordsValidAsMemberName` set contains exactly `{TokenKind.From, TokenKind.To}` (currently — may grow as more types add keyword-named accessors)

**Downstream Impact:**
- Runtime: N/A — parser change only
- Tooling: Completions after `.` on exchangerate fields should already include `from`/`to` (they derive from accessor metadata). Verify no regression.
- MCP: N/A — no tool surface changes. The `precept_language` type accessor output already lists `from`/`to`.

---

### Slice C3 — Compound Boolean `=`: Ensure Expression Parser Fix

**Status:** 🔲 Not Started  
**Added:** 2026-05-12  
**Depends on:** None  
**Blocks:** None

#### Root Cause

This is a **sample file bug, not a compiler bug.** The analysis is definitive:

In `ensure QuantityOnHand > '0 {StockingUnit}' and AverageCost > '0 {CatalogCurrency}/{StockingUnit}' or QuantityOnHand = '0 {StockingUnit}'` (inventory-item.precept L150, L156):

1. Precept uses `=` exclusively for **assignment** (`set Field = Value`). `TokenKind.Assign` is not registered as a binary operator in the `Operators` catalog.
2. Precept uses `==` for **equality comparison** (`OperatorKind.Equals`, `TokenKind.DoubleEquals`, precedence 30 in Operators.cs).
3. When the Pratt parser encounters `=` in an expression context, `GetLedBindingPower(TokenKind.Assign)` returns `(-1, -1)` — "not an operator." The parser stops the led loop and returns the partial expression `QuantityOnHand` without consuming the `=` or the right operand.
4. The orphaned `= '0 {StockingUnit}'` tokens violate slot termination, producing PRE0009.

**This is correct parser behavior.** The language design intentionally separates assignment (`=`) from equality (`==`) to prevent ambiguity between action and expression contexts. The sample file is wrong — it should use `==`.

#### Design

**Two changes:**

**A. Fix the sample file (2 lines)**

`samples/inventory-item.precept` lines 150 and 156: change `= '0 {StockingUnit}'` to `== '0 {StockingUnit}'`.

**B. Add a targeted diagnostic hint when `=` appears in expression context (~15 LOC)**

When the parser encounters `TokenKind.Assign` in expression position (inside `ParseExpression`'s led loop, after `GetLedBindingPower` returns `(-1, -1)`), emit a **hint diagnostic** suggesting `==`:

**File:** `src/Precept/Pipeline/Parser.Expressions.cs`  
**Method:** `ParseExpression()` (L20–34) or new helper called from the led-loop exit path

After the binding-power check fails and before breaking:
```csharp
// In the led loop, when ledBp < 0:
if (Peek().Kind == TokenKind.Assign)
{
    _diagnostics.Add(Language.Diagnostics.Create(
        DiagnosticCode.AssignmentInExpressionContext, Peek().Span));
    // Do NOT consume the token — let normal error recovery handle it
}
```

**File:** `src/Precept/Language/DiagnosticCode.cs`  
Add new code: `AssignmentInExpressionContext` (next available code number)

**File:** `src/Precept/Language/Diagnostics.cs` (or wherever diagnostic metadata lives)  
Message: `"Use '==' for comparison. '=' is the assignment operator (used in 'set' actions)."`  
Severity: `Error` (not a hint — this is always wrong in expression context)

**Files:**

| File | Change | LOC |
|------|--------|-----|
| `samples/inventory-item.precept` | L150, L156: `= '0 {StockingUnit}'` → `== '0 {StockingUnit}'` | 2 |
| `src/Precept/Pipeline/Parser.Expressions.cs` | Add `TokenKind.Assign` check in led-loop exit path | ~8 |
| `src/Precept/Language/DiagnosticCode.cs` | Add `AssignmentInExpressionContext` code | 1 |
| `src/Precept/Language/Diagnostics.cs` | Add diagnostic metadata entry with message and severity | ~5 |

**LOC estimate:** ~16 lines changed

**Tests:**

_Parser tests (new):_
- `ensure A = B` → emits `AssignmentInExpressionContext` diagnostic with span on `=` ✓
- `ensure A == B` → no diagnostic, valid comparison ✓
- `ensure A > B and C = D` → emits `AssignmentInExpressionContext` on the `=` ✓
- `set Field = Value` → no diagnostic (action context, not expression context) ✓

_Regression tests:_
- `set X = Y` in action position — still valid, no spurious diagnostic
- All existing ensure expressions with `==` — unchanged behavior
- All existing `=` in action slots — unchanged behavior

**Downstream Impact:**
- Runtime: N/A — diagnostic addition only
- Tooling: Language server surfaces the new diagnostic code automatically (diagnostics are catalog-derived). Quick-fix action (suggest `==` replacement) is a possible follow-up but out of scope.
- MCP: New diagnostic code auto-surfaces through `precept_language` diagnostic lookup. No DTO changes.

---

### Slice C4 — Proof Engine Interpolated Qualifier Symbolic Equality (BUG-A)

**Status:** 🔲 Not Started  
**Added:** 2026-05-12  
**Depends on:** None (RC-1 already implemented — parser accepts interpolated qualifiers)  
**Blocks:** None

#### Root Cause

73+ PRE0114 (`UnprovedQualifierCompatibility`) errors in `inventory-item.precept` trace to a single gap: **`ResolveQualifierOnAxis()` in ProofEngine.cs cannot resolve qualifiers from event arguments (`TypedArgRef`).** It only resolves qualifiers from fields (`TypedFieldRef`).

**The failure chain:**

1. **TypeChecker produces correct qualifier metadata.** For `qty as quantity of '{StockingUnit.dimension}'`, `MapInterpolatedQualifier()` (TypeChecker.cs L154–184) creates `DeclaredQualifierMeta.Dimension("{StockingUnit.dimension}")` — the template string preserves the interpolated expression structure. `TypedArgRef` carries these qualifiers in its `DeclaredQualifiers` property (SemanticIndex.cs L29–35).

2. **ProofEngine resolves the subject to `TypedArgRef`.** `ResolveSubject()` (ProofEngine.cs L254–273) correctly maps a `ParamSubject` in a binary operation to the `TypedArgRef` via `ResolveParamInBinaryOp()`.

3. **`GetFieldName()` fails on `TypedArgRef`.** (ProofEngine.cs L315–323) The switch only handles `TypedFieldRef` and `TypedMemberAccess { Object: TypedFieldRef }`. `TypedArgRef` falls through to `_ => null`.

4. **`ResolveQualifierOnAxis()` returns null.** (ProofEngine.cs L945–988) Because `GetFieldName()` returned null (L948–949), the method returns null immediately — no qualifier lookup attempted.

5. **`TryQualifierCompatibilityProof()` fails.** (ProofEngine.cs L878–911) When either side resolves to null, the proof cannot discharge. The obligation remains unresolved, producing PRE0114.

**Key insight:** The qualifier values are **already structurally equal** (both sides carry `Dimension("{StockingUnit.dimension}")` as a string template). The bug is not in qualifier comparison — it's that the engine never gets to compare them because it can't look up arg qualifiers.

**Existing precedent:** `ResolveSourceModifiers()` (ProofEngine.cs L1101–1121) **already handles `TypedArgRef`** for modifier resolution — it pattern-matches on `TypedArgRef`, looks up the event by name, and iterates args. The qualifier resolution path needs the same treatment.

#### Design

**A. Extend `GetFieldName()` to handle `TypedArgRef` (~2 LOC)**

**File:** `src/Precept/Pipeline/ProofEngine.cs` L315–323

```csharp
private static string? GetFieldName(TypedExpression? resolved)
{
    return resolved switch
    {
        TypedFieldRef fieldRef => fieldRef.FieldName,
        TypedMemberAccess { Object: TypedFieldRef fieldRef } => fieldRef.FieldName,
        TypedArgRef argRef => argRef.ArgName,      // ← NEW
        _ => null
    };
}
```

This gives the proof engine a name to use for arg references in diagnostic messages (replacing `<unknown>` with the actual arg name).

**B. Extend `ResolveQualifierOnAxis()` to resolve arg qualifiers (~20 LOC)**

**File:** `src/Precept/Pipeline/ProofEngine.cs` L945–988

After the existing field lookup path (L950), add an arg qualifier resolution path. The structure mirrors `ResolveSourceModifiers()`:

```csharp
private static DeclaredQualifierMeta? ResolveQualifierOnAxis(
    ProofSubject subject, QualifierAxis axis, TypedExpression site, SemanticIndex semantics)
{
    var resolved = ResolveSubject(subject, site);
    
    // ── Path 1: Direct qualifier extraction from TypedArgRef ──
    // TypedArgRef carries its qualifiers directly (set by TypeChecker from
    // MapInterpolatedQualifier). No semantic index lookup needed.
    if (resolved is TypedArgRef argRef && argRef.DeclaredQualifiers is { IsDefaultOrEmpty: false } argQuals)
    {
        foreach (var qual in argQuals)
        {
            if (qual.Axis == axis)
                return qual;
        }
        // Apply same axis fallbacks as field path (Unit→Dimension, Dimension→TemporalDimension)
        if (axis == QualifierAxis.Unit)
        {
            foreach (var qual in argQuals)
                if (qual.Axis == QualifierAxis.Dimension) return qual;
        }
        if (axis == QualifierAxis.Dimension)
        {
            foreach (var qual in argQuals)
                if (qual.Axis == QualifierAxis.TemporalDimension) return qual;
        }
        return null;
    }
    
    // ── Path 2: Existing field lookup path (unchanged) ──
    var fieldName = GetFieldName(resolved);
    if (fieldName is null) return null;
    if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return null;
    // ... existing field qualifier resolution + fallbacks + implied qualifiers ...
}
```

**Why direct extraction instead of semantic index lookup:** `TypedArgRef` already carries `DeclaredQualifiers` (set by the TypeChecker at resolution time). Looking up qualifiers through the semantic index (`EventsByName → Args → DeclaredQualifiers`) would work but is unnecessarily indirect — the qualifiers are already on the expression node. Direct extraction is simpler, more efficient, and consistent with how `TypedArgRef` is used elsewhere in the TypeChecker (e.g., `TypeChecker.Expressions.TypedConstants.cs` L55 pattern-matches `TypedArgRef { DeclaredQualifiers: { } argQualifiers }`).

**C. Qualifier equality for interpolated values: string comparison is sufficient**

Two interpolated qualifier expressions `'{StockingUnit.dimension}'` produce identical `DeclaredQualifierMeta.Dimension("{StockingUnit.dimension}")` records. C# record equality (`leftQualifier == rightQualifier`) compares all fields — `DimensionName`, `Origin`, `Preposition`, and `ProofSatisfactions`. The string comparison on `DimensionName` is exact: `"{StockingUnit.dimension}" == "{StockingUnit.dimension}"`.

For `ExtractComparableValue()`, `Dimension("{StockingUnit.dimension}")` → returns `"{StockingUnit.dimension}"` — the template string itself. `ChainQualifiersMatch` uses `string.Equals(..., Ordinal)`, so two structurally identical interpolated qualifiers compare equal.

**No new subtype or comparison logic is needed.** The existing `DeclaredQualifierMeta` DU subtypes and comparison infrastructure handle interpolated qualifiers correctly — once the qualifiers are actually resolved.

**D. What "symbolic equality" means for interpolated qualifiers**

Two interpolated qualifier expressions are symbolically equal when their **template strings are identical.** The template string is produced by `DescribeInterpolatedQualifier()` (TypeChecker.cs L186–204), which reconstructs the expression as `{FieldName.AccessorName}` or `{FieldName}`. This is a **structural identity** — it compares the names in the source text, not the runtime values.

- `'{StockingUnit.dimension}'` on field A and `'{StockingUnit.dimension}'` on field B → **equal** (same template string)
- `'{StockingUnit.dimension}'` and `'{PurchaseUnit.dimension}'` → **not equal** (different template strings, even if both happen to reference the same dimension at runtime)
- `'{StockingUnit.dimension}'` and `'mass'` (literal) → **not equal** (interpolated template ≠ literal value, even if StockingUnit happens to be in the mass dimension)

This is the correct behavior. Precept's proof engine is **conservative**: it proves what is structurally guaranteed, not what might be true at runtime. If an author needs to assert that two different fields have the same dimension, they must use the same interpolation source or a literal.

**Files:**

| File | Change | LOC |
|------|--------|-----|
| `src/Precept/Pipeline/ProofEngine.cs` L315–323 | Add `TypedArgRef argRef => argRef.ArgName` to `GetFieldName()` switch | ~2 |
| `src/Precept/Pipeline/ProofEngine.cs` L945–988 | Add `TypedArgRef` direct qualifier extraction path before field lookup, with axis fallbacks | ~20 |

**LOC estimate:** ~22 lines changed

**Tests:**

_Proof engine tests (new) — all in `test/Precept.Tests/ProofEngine/ProofEngineTests.cs`:_

- **Arg qualifier compatibility — same interpolated qualifier:**
  `event E(x as quantity of '{F.dimension}')` + `field F as quantity of '{F.dimension}'` + `on E ensure E.x > F` → proof discharges ✓ (both carry `Dimension("{F.dimension}")`)

- **Arg qualifier compatibility — different interpolated qualifier:**
  `event E(x as quantity of '{A.dimension}')` + `field B as quantity of '{C.dimension}'` + `on E ensure E.x > B` → PRE0114 ✓ (template strings differ)

- **Arg qualifier compatibility — literal vs interpolated:**
  `event E(x as quantity of '{F.dimension}')` + `field G as quantity of 'mass'` + `on E ensure E.x > G` → PRE0114 ✓ (interpolated template ≠ literal value)

- **Arg qualifier compatibility — interpolated on both args:**
  `event E(x as quantity of '{F.dimension}', y as quantity of '{F.dimension}')` + binary op involving both → proof discharges ✓

- **Arg currency qualifier:**
  `event E(p as price in '{Curr}')` + `field Curr as currency` + `on E ensure E.p > TotalCost` where `TotalCost as price in '{Curr}'` → proof discharges ✓

- **Arg with axis fallback (Unit→Dimension):**
  `event E(q as quantity of '{F.dimension}')` + `field G as quantity in 'kg'` (unit with dimension 'mass') + comparison → axis fallback resolves; proof outcome depends on whether `"{F.dimension}"` equals `"mass"` (it won't — correctly PRE0114)

- **GetFieldName diagnostic message:**
  When proof fails on an arg reference, diagnostic message should show `"x"` (arg name) not `"<unknown>"`

_Regression tests:_
- All existing proof engine tests pass (field-only qualifier resolution unchanged)
- `ResolveSourceModifiers` for `TypedArgRef` — existing modifier resolution still works
- Field qualifier resolution path (Path 2) is structurally unchanged — verify no regression in field-only comparisons

**Downstream Impact:**
- Runtime: N/A — proof engine change only (compile-time)
- Tooling: Better diagnostic messages (arg names instead of `<unknown>`). No semantic-token or completion changes.
- MCP: N/A — no tool surface changes. Proof obligations are internal.

---

### Part C Dependency Order

```
C1 (Dimension Cancellation) — depends on Slice 2B (compound-unit forms)
C2 (Keyword Member Name)    — independent
C3 (Ensure Equals)          — independent
C4 (Arg Qualifier Proof)    — independent
```

C2, C3, and C4 can execute in any order, in parallel, and in parallel with Parts A/B/D. C1 depends on Slice 2B and is already in progress (George).

---

## Part D — Test Failure Fixes (Pre-Existing)

**Added:** 2026-05-11  
**Architect:** Frank  
**Source:** `test-results/failure-diagnosis.txt` — 30 pre-existing failures across the test suite, none caused by recent George/RC work.  
**Scope:** 4 root-cause groups (B1–B4) requiring surgical test/fixture/config fixes. No weakening of validation logic. No pipeline changes.

All D-slices are independent of Parts A, B, and C. They can execute in any order and in parallel with other workstreams.

---

### Slice D1 — B1: ConflictingModifiers Fixture Fix (TypeCheckerAssemblyTests)

**Status:** 🔲 Not Started  
**Owner:** George  
**Depends on:** None  
**Fixes:** 23 failures in `TypeCheckerAssemblyTests` + 1 in `Integration_LoanApplication_FullSample` = **24 total**

**Root Cause Analysis:**

The `Modifiers` catalog correctly declares `Optional` and `Notempty` as mutually exclusive:

- `Modifiers.cs` L61: `Optional` → `MutuallyExclusiveWith: [ModifierKind.Notempty]`
- `Modifiers.cs` L131: `Notempty` → `MutuallyExclusiveWith: [ModifierKind.Optional]`

The modifier validation in `TypeChecker.Validation.cs` L121–123 correctly emits `DiagnosticCode.ConflictingModifiers` when both appear on the same field/arg. This validation is semantically correct: `optional` permits absence while `notempty` asserts content — a logical contradiction. The `TypeCheckerModifierTests` already contain positive tests proving this validation works as designed (`Field_OptionalAndNotempty_EmitsConflictingModifiers`, `EventArg_OptionalAndNotempty_EmitsConflictingModifiers`, `Field_CollectionOptionalAndNotempty_EmitsConflictingModifiers`).

The problem: two shared test DSL fixtures contain `optional notempty` on event args, which was never valid once modifier conflict validation shipped. The fixtures predate the validation tightening.

**Design Decision:** The validation is correct — we do NOT weaken it. The fixtures must be updated to remove the contradiction.

**Semantic intent of the original fixture:** The fixture author intended "the Note arg is optional, but if provided, must not be empty." This intent is valid but cannot be expressed via contradictory modifiers. In Precept, the correct pattern is: make the arg `optional` (presence semantics) and add an event ensure to validate non-emptiness when present. However, for a test fixture whose purpose is to exercise SemanticIndex completeness and assembly output — not modifier semantics — the simplest correct fix is to drop `notempty` and keep `optional`. The fixture's tests never inspect the Note arg's modifier list; they inspect the assembled SemanticIndex structure.

**Files:**

| File | Change |
|------|--------|
| `test/Precept.Tests/TypeChecker/TypeCheckerAssemblyTests.cs` L55 | `Note as string optional notempty` → `Note as string optional` |
| `test/Precept.Tests/TypeChecker/TypeCheckerAssemblyTests.cs` L502 | `Note as string optional notempty` → `Note as string optional` (LoanApplication integration test) |

**Work:**

1. In the `FullPrecept` constant (L55), change the `Approve` event arg declaration from `Note as string optional notempty` to `Note as string optional`.
2. In the `Integration_LoanApplication_FullSample` inline DSL (L502), apply the same change.
3. No other test files require changes — the `ParserCoverageGapTests` uses `optional notempty` in parser-level tests that don't invoke type checking, so they remain valid.

**Tests:** All 24 previously-failing tests should now pass. No new tests needed — the existing `TypeCheckerModifierTests` already validate that `ConflictingModifiers` fires correctly for `optional notempty`.

**Downstream Impact:**
- Runtime: N/A — no runtime changes
- Tooling: N/A — no language server changes
- MCP: N/A — no MCP changes

---

### Slice D2 — B2: ExchangeRate `to` Keyword Qualifier Parser Fix

**Status:** 🔲 Not Started  
**Owner:** George  
**Depends on:** None  
**Fixes:** 3 failures in `TypeCheckerAssignmentQualifierTests`

**Root Cause Analysis:**

The 3 failing exchange rate tests use the syntax `field rate1 as exchangerate from 'USD' to 'EUR'`. However, the canonical qualifier shape for `exchangerate` is:

```
QS_ExchangeRate = new([
    new(TokenKind.In,   QualifierAxis.FromCurrency),
    new(TokenKind.To,   QualifierAxis.ToCurrency),
]);
```

The correct DSL syntax is `exchangerate in 'USD' to 'EUR'`, not `exchangerate from 'USD' to 'EUR'`. The `Types` catalog confirms this at `Types.cs` L604: `UsageExample: "field FxRate as exchangerate in 'USD' to 'EUR'"`.

**Why it worked before RC-1:** Before RC-1 tightened parser qualifier position handling, the `from` keyword in `field rate1 as exchangerate from 'USD' to 'EUR'` was being consumed differently — likely as an unrecognized token that the parser skipped past, allowing the `to` qualifier to still match its slot. After RC-1, `from` is now immediately consumed as a construct leader (transition row `from State on Event`), which pulls the parser into transition parsing and causes the `to` keyword to fail with `ExpectedToken`.

**Why these tests use `from` instead of `in`:** The tests were written based on an intuitive reading of exchange rate semantics ("from USD to EUR") rather than the actual qualifier shape. The qualifier shape uses `in` for the source currency — consistent with all other domain types (`money in 'USD'`, `quantity in 'kg'`). The `from` preposition on `exchangerate` is a member accessor (`.from` returns the source currency value), not a qualifier preposition.

**Design Decision:** Fix the test DSL to use the canonical qualifier syntax. This is NOT a parser bug — the parser correctly follows the qualifier shape. The tests had the wrong syntax.

**Interaction with Rec 2 (`.from`/`.to` member access disambiguation):** None. Rec 2 concerns member accessor tokens (`.from`, `.to`) in expressions. This fix concerns qualifier preposition tokens (`in`, `to`) in field type declarations. Different parser paths, different token positions, no shared disambiguation logic.

**Files:**

| File | Change |
|------|--------|
| `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` L22 | `exchangerate from 'USD' to 'EUR'` → `exchangerate in 'USD' to 'EUR'` |
| `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` L23 | same |
| `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` L40 | same |
| `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` L41 | same |
| `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` L58 | same |
| `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` L59 | same |

**Work:**

In all 3 exchange rate test methods (`ExchangeRate_Assignment_MatchingFromTo_NoDiagnostic`, `ExchangeRate_Assignment_MismatchedFromCurrency_Diagnostic`, `ExchangeRate_Assignment_MismatchedToCurrency_Diagnostic`), replace `from 'XXX' to 'YYY'` with `in 'XXX' to 'YYY'` in every `field ... as exchangerate` declaration. Six line changes total across the three tests.

**Tests:** The 3 previously-failing tests should now pass with their original assertions intact:
- `MatchingFromTo_NoDiagnostic` — expects clean compile → will compile clean with correct syntax
- `MismatchedFromCurrency_Diagnostic` — expects `QualifierMismatch` → will now reach qualifier validation and produce the expected mismatch
- `MismatchedToCurrency_Diagnostic` — expects `QualifierMismatch` → same

**Downstream Impact:**
- Runtime: N/A — no runtime changes
- Tooling: N/A — no language server changes
- MCP: N/A — no MCP changes

---

### Slice D3 — B3: LS Manifest Activation Event

**Status:** 🔲 Not Started  
**Owner:** Kramer (language server)  
**Depends on:** None  
**Fixes:** 1 failure in `Precept.LanguageServer.Tests` — `ExtensionManifestTests.PackageManifest_Activates_WhenAPreceptDocumentOpens`

**Root Cause Analysis:**

The test at `test/Precept.LanguageServer.Tests/ExtensionManifestTests.cs` L40 asserts:

```csharp
activationEvents.Should().Contain("onLanguage:precept");
activationEvents.Should().Contain("workspaceContains:**/*.precept");
```

The current `tools/Precept.VsCode/package.json` L13–15 only declares:

```json
"activationEvents": [
    "workspaceContains:**/*.precept"
]
```

The `onLanguage:precept` entry is missing. This is a pre-existing config gap — the extension activates when a workspace contains `.precept` files, but NOT when a `.precept` document is opened directly (e.g., `code path/to/file.precept` in a non-workspace context).

**Design Decision:** Add `"onLanguage:precept"` to the `activationEvents` array. This is the standard VS Code pattern for language extensions — both workspace-level and document-level activation should be present.

**Files:**

| File | Change |
|------|--------|
| `tools/Precept.VsCode/package.json` L13–15 | Add `"onLanguage:precept"` to `activationEvents` array |

**Work:**

Change the `activationEvents` array from:

```json
"activationEvents": [
    "workspaceContains:**/*.precept"
]
```

to:

```json
"activationEvents": [
    "onLanguage:precept",
    "workspaceContains:**/*.precept"
]
```

One line addition. The `onLanguage:precept` entry triggers activation when any document with the `precept` language ID is opened, matching the language registration in `contributes.languages[0].id = "precept"` (L19). The `workspaceContains` entry remains as a fallback for workspace-level activation before any document is opened.

**Tests:** The 1 previously-failing test (`PackageManifest_Activates_WhenAPreceptDocumentOpens`) should now pass.

**Downstream Impact:**
- Runtime: N/A
- Tooling: The extension will now activate on single-file opens in non-workspace contexts — this is correct behavior, not a side effect
- MCP: N/A

---

### Slice D4 (Reframed) — Scalar-Op Qualifier Propagation Fix

**Status:** 🔲 Not Started — design complete  
**Owner:** George (core runtime)  
**Depends on:** None  
**Fixes:** 1 failure in `Precept.Mcp.Tests` — `LanguageToolTests.Language_SyntaxReferenceMirrorsSourceAndExamplesCompile`

**Naming Decision:** Kept as D4, not renamed to C5. Rationale: Part C is scoped to "inventory-item compile fixes." This fix does NOT resolve any of the remaining 66 PRE0114 in `inventory-item.precept` — those are all from BUG-A (arg qualifier resolution, C4). Part D is "pre-existing test failure fixes." This fixes a pre-existing test failure. The fact that the fix reaches into the compiler doesn't change its classification — it makes it a D-series item whose root cause runs deeper than originally assumed. Creating a new E-series for one item is over-engineering the classification.

#### Root Cause Analysis (Reframed)

The original D4 premise was wrong. `default '0.00 USD/kg'` on a `price` field **already compiles** — the runtime supports compound-unit defaults. The actual failure is in the `FinalCost` computed expression in the same snippet.

**Failing test:** `test/Precept.Mcp.Tests/LanguageToolTests.cs` L380–385 → `Language_SyntaxReferenceMirrorsSourceAndExamplesCompile`

**Failing snippet:** The "Money and quantity typed fields" `CommonPattern` at `src/Precept/Language/SyntaxReference.cs` L244–257:

```precept
precept ShipmentOrder
field Weight as quantity of 'mass' default '0 kg'
field UnitPrice as price in 'USD' of 'mass' default '0.00 USD/kg'
field TotalCost as money in 'USD' <- Weight * UnitPrice
field DiscountPercent as decimal default 0 nonnegative max 100 maxplaces 2
field FinalCost as money in 'USD' <- TotalCost - (TotalCost * DiscountPercent / 100)
rule DiscountPercent <= 100 because "Discount percent cannot exceed 100%"
```

**Exact error:** `PRE0114 | Error | Operands '<unknown>' and '<unknown>' have incompatible Currency qualifiers in field 'FinalCost' computed expression`

**Minimal repro (confirmed by direct compile probe):**

```precept
precept Test
field TotalCost as money in 'USD' default '10 USD'
field DiscountPercent as decimal default 0
field FinalCost as money in 'USD' <- TotalCost - (TotalCost * DiscountPercent / 100)
```

Same PRE0114. Removing `FinalCost` makes the snippet compile clean.

#### The Bug: Qualifier-Bearing Scalar Ops Drop Qualifiers

Six operations in `src/Precept/Language/Operations.cs` produce a qualifier-bearing result type but carry **no `ResultQualifierPolicy`** and **no `QualifierMatch`**:

| OperationKind | Line | Signature | Missing |
|---|---|---|---|
| `MoneyTimesDecimal` | L440 | `money × decimal → money` | No qualifier propagation |
| `MoneyDivideDecimal` | L444 | `money ÷ decimal → money` | No qualifier propagation |
| `QuantityTimesDecimal` | L519 | `quantity × decimal → quantity` | No qualifier propagation |
| `QuantityDivideDecimal` | L523 | `quantity ÷ decimal → quantity` | No qualifier propagation |
| `PriceTimesDecimal` | L636 | `price × decimal → price` | No qualifier propagation |
| `PriceDivideDecimal` | L640 | `price ÷ decimal → price` | No qualifier propagation |

All six have `ResultQualifierPolicy: ResultQualifierPolicy.None` (default) and `QualifierMatch: QualifierMatch.Any` (default).

**Consequence:** In `MapQualifierBinding()` (TypeChecker.Expressions.cs L666–677), these operations produce `ResultQualifier = null` on the `TypedBinaryOp`. When that subexpression appears as an operand in an outer operation (e.g., `MoneySubtractMoney` with `QualifierMatch.Same`), the proof engine cannot resolve its qualifier:

1. `TotalCost * DiscountPercent` → resolves as `MoneyTimesDecimal` → `TypedBinaryOp(Money, ResultQualifier: null)`
2. `result / 100` → resolves as `MoneyDivideDecimal` → `TypedBinaryOp(Money, ResultQualifier: null)`
3. `TotalCost - result` → resolves as `MoneySubtractMoney` with `QualifierMatch.Same` → `SameQualifierRequired`
4. Proof engine calls `ResolveQualifierOnAxis()` on the right operand (the inner `TypedBinaryOp`)
5. `GetFieldName(TypedBinaryOp)` → returns `null` (line 322: `_ => null`)
6. `ResolveQualifierOnAxis()` returns `null` → proof fails → PRE0114

**The model to follow:** `QuantityTimesQuantity` (L570–573) already carries `ResultQualifierPolicy: ResultQualifierPolicy.CompoundUnitCancellation`. The type checker and proof engine already handle this via `CompoundUnitCancellationRequired` in `MapQualifierBinding()`.

#### Design

The fix has three layers: catalog metadata, type checker, and proof engine.

**1. New `ResultQualifierPolicy` enum value**

**File:** `src/Precept/Language/Operation.cs` L25–30

Add a new policy value:

```csharp
public enum ResultQualifierPolicy
{
    [Precept.AllowZeroDefault]
    None,
    CompoundUnitCancellation,
    InheritFromQualifiedOperand,
}
```

**Semantics:** The result inherits ALL qualifiers from whichever operand carries qualifiers (the typed operand, not the scalar). For `money × decimal`, the `money` operand's currency qualifier flows to the result. For `price × decimal`, the `price` operand's currency AND dimension qualifiers flow.

**Why not `QualifierMatch.Same`?** `Same` means "both operands share the same qualifier and the result inherits it." Scalar operations have only ONE qualifier-bearing operand — the other is `decimal`, which has no qualifiers. `Same` is semantically wrong and would fail the qualifier compatibility proof (decimal has no currency to match).

**2. New `QualifierBinding` subtype**

**File:** `src/Precept/Pipeline/SemanticIndex.cs` L196–205

Add after `CompoundUnitCancellationRequired`:

```csharp
/// <summary>
/// Result inherits qualifiers from the qualifier-bearing operand in a scalar operation.
/// The non-qualifier-bearing operand (e.g., decimal) is transparent to qualifier flow.
/// </summary>
public sealed record QualifiedOperandInherited : QualifierBinding;
```

**3. Update `MapQualifierBinding()`**

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` L666–677

```csharp
private static QualifierBinding? MapQualifierBinding(BinaryOperationMeta meta)
{
    if (meta.ResultQualifierPolicy == ResultQualifierPolicy.CompoundUnitCancellation)
        return new CompoundUnitCancellationRequired();

    if (meta.ResultQualifierPolicy == ResultQualifierPolicy.InheritFromQualifiedOperand)
        return new QualifiedOperandInherited();

    return meta.Match switch
    {
        QualifierMatch.Same      => new SameQualifierRequired(),
        QualifierMatch.Different => null,
        _                        => null,
    };
}
```

**4. Set policy on six scalar operations**

**File:** `src/Precept/Language/Operations.cs`

For each of the six affected operations, add `ResultQualifierPolicy: ResultQualifierPolicy.InheritFromQualifiedOperand`:

| OperationKind | Current | Change |
|---|---|---|
| `MoneyTimesDecimal` (L440–442) | No policy | Add `ResultQualifierPolicy: ResultQualifierPolicy.InheritFromQualifiedOperand` |
| `MoneyDivideDecimal` (L444–451) | No policy | Same |
| `QuantityTimesDecimal` (L519–521) | No policy | Same |
| `QuantityDivideDecimal` (L523–530) | No policy | Same |
| `PriceTimesDecimal` (L636–638) | No policy | Same |
| `PriceDivideDecimal` (L640–647) | No policy | Same |

**5. Extend `ResolveQualifierOnAxis()` to handle `TypedBinaryOp` subjects**

**File:** `src/Precept/Pipeline/ProofEngine.cs` L946–1010

After the `TypedArgRef` handling (L950–956) and before the `GetFieldName` path (L977), add a new path for `TypedBinaryOp`:

```csharp
// ── Path: Transitive qualifier resolution through binary operations ──
// When the resolved subject is a TypedBinaryOp (a subexpression), its
// ResultQualifier tells us how to derive the result's qualifiers.
if (resolved is TypedBinaryOp binOp && binOp.ResultQualifier is not null)
{
    switch (binOp.ResultQualifier)
    {
        case SameQualifierRequired:
            // Both operands share the qualifier; recurse into either side.
            // Left is the canonical choice (right would yield the same value).
            return ResolveQualifierFromExpression(binOp.Left, axis, semantics);

        case QualifiedOperandInherited:
            // Exactly one operand is qualifier-bearing. Find it by checking
            // which operand's type is the same as the result type (the typed
            // operand), not the scalar.
            var qualifiedOperand = binOp.Left.ResultType == binOp.ResultType
                ? binOp.Left : binOp.Right;
            return ResolveQualifierFromExpression(qualifiedOperand, axis, semantics);

        case CompoundUnitCancellationRequired:
            // Compound cancellation produces a new qualifier computed from
            // both operands — not inherited from either. Cannot resolve
            // transitively without the cancellation algorithm. Return null
            // and let the proof engine handle it through existing paths.
            return null;
    }
}
```

**6. New helper: `ResolveQualifierFromExpression()`**

**File:** `src/Precept/Pipeline/ProofEngine.cs`

```csharp
/// <summary>
/// Resolve a qualifier on an axis from an arbitrary typed expression.
/// Handles field refs, arg refs, and recursive binary ops.
/// </summary>
private static DeclaredQualifierMeta? ResolveQualifierFromExpression(
    TypedExpression expr, QualifierAxis axis, SemanticIndex semantics)
{
    switch (expr)
    {
        case TypedArgRef { DeclaredQualifiers: { IsDefaultOrEmpty: false } argQuals }:
            foreach (var q in argQuals)
                if (q.Axis == axis) return q;
            if (axis == QualifierAxis.Unit)
                foreach (var q in argQuals)
                    if (q.Axis == QualifierAxis.Dimension) return q;
            if (axis == QualifierAxis.Dimension)
                foreach (var q in argQuals)
                    if (q.Axis == QualifierAxis.TemporalDimension) return q;
            return null;

        case TypedFieldRef fieldRef:
            return ResolveFieldQualifier(fieldRef.FieldName, axis, semantics);

        case TypedMemberAccess { Object: TypedFieldRef fieldRef }:
            return ResolveFieldQualifier(fieldRef.FieldName, axis, semantics);

        case TypedBinaryOp binOp when binOp.ResultQualifier is not null:
            // Recursive case: nested scalar ops, e.g., (a * b) * c
            return binOp.ResultQualifier switch
            {
                SameQualifierRequired =>
                    ResolveQualifierFromExpression(binOp.Left, axis, semantics),
                QualifiedOperandInherited =>
                    ResolveQualifierFromExpression(
                        binOp.Left.ResultType == binOp.ResultType ? binOp.Left : binOp.Right,
                        axis, semantics),
                _ => null,
            };

        default:
            return null;
    }
}

/// <summary>Look up a field's qualifier on a specific axis (with standard fallbacks).</summary>
private static DeclaredQualifierMeta? ResolveFieldQualifier(
    string fieldName, QualifierAxis axis, SemanticIndex semantics)
{
    if (!semantics.FieldsByName.TryGetValue(fieldName, out var field))
        return null;

    foreach (var q in field.DeclaredQualifiers)
        if (q.Axis == axis) return q;

    if (axis == QualifierAxis.Unit)
        foreach (var q in field.DeclaredQualifiers)
            if (q.Axis == QualifierAxis.Dimension) return q;

    if (axis == QualifierAxis.Dimension)
        foreach (var q in field.DeclaredQualifiers)
            if (q.Axis == QualifierAxis.TemporalDimension) return q;

    var typeMeta = Types.GetMeta(field.ResolvedType);
    foreach (var q in typeMeta.ImpliedQualifiers)
        if (q.Axis == axis) return q;

    return null;
}
```

**Note:** `ResolveFieldQualifier` extracts and deduplicates the field-lookup logic already present in `ResolveQualifierOnAxis`. The existing method can be refactored to delegate to `ResolveFieldQualifier` for the field path — this is an optional cleanup, not a requirement.

#### Impact on `inventory-item.precept`

**None.** The remaining 66 PRE0114 in `inventory-item.precept` are all from BUG-A (arg qualifier resolution via interpolated qualifiers on event arguments). All arithmetic in that file uses typed-operand operations (`quantity × quantity`, `money + money`, `money ÷ quantity`, `price × quantity`, `exchangerate × money`). None involve scalar decimal scaling. C4 is the fix for inventory-item PRE0114, not D4.

#### Test Cases

**A. Direct fix verification (the failing test):**

1. `Language_SyntaxReferenceMirrorsSourceAndExamplesCompile` — currently failing. After fix, the "Money and quantity typed fields" snippet compiles clean because `TotalCost * DiscountPercent` preserves the USD currency qualifier through the subexpression.

**B. New targeted unit tests** (suggested file: `test/Precept.Tests/Pipeline/ScalarOpQualifierPropagationTests.cs`):

| Test | Expression | Expected |
|---|---|---|
| `MoneyTimesDecimal_PreservesQualifier` | `field A as money in 'USD' default '10 USD'` / `field B as decimal default 2` / `field C as money in 'USD' <- A * B` | Clean compile |
| `MoneyDivideDecimal_PreservesQualifier` | Same with `A / B` | Clean compile |
| `MoneyScaledSubtraction_PreservesQualifier` | `field C as money in 'USD' <- A - (A * B / 100)` | Clean compile (the repro case) |
| `QuantityTimesDecimal_PreservesQualifier` | `field Q as quantity of 'mass' default '1 kg'` / `field S as decimal default 2` / `field R as quantity of 'mass' <- Q * S` | Clean compile |
| `QuantityDivideDecimal_PreservesQualifier` | Same with `Q / S` | Clean compile |
| `PriceTimesDecimal_PreservesQualifier` | `field P as price in 'USD' of 'mass'` / `field S as decimal default 2` / `field R as price in 'USD' of 'mass' <- P * S` | Clean compile |
| `PriceDivideDecimal_PreservesQualifier` | Same with `P / S` | Clean compile |
| `ChainedScalarOps_PreservesQualifier` | `field C as money in 'USD' <- A * B * B` (nested scalar ops) | Clean compile |
| `CrossCurrencyScalarResult_Diagnostic` | `field A as money in 'USD'` / `field B as decimal` / `field C as money in 'EUR' <- A * B` | PRE0114 (correct: USD ≠ EUR) |
| `BidirectionalScalarOrder` | `field C as money in 'USD' <- B * A` (decimal on left) | Clean compile (BidirectionalLookup) |

**C. Regression anchors:**

- All existing `MoneySubtractMoney`, `MoneyAddMoney`, `QuantityAddQuantity` tests should continue passing — `SameQualifierRequired` is unchanged.
- All existing `CompoundUnitCancellation` tests — the new code path does NOT touch this binding type.
- All existing `Language_SyntaxReferenceMirrorsSourceAndExamplesCompile` OTHER patterns — only the "Money and quantity" pattern was failing.

#### Regression Risk

**Low.** The changes are:
1. Additive enum value — no existing code paths affected
2. New `QualifierBinding` subtype — existing code doesn't pattern-match it, so it falls through safely
3. `MapQualifierBinding` gets a new branch before the existing switch — existing branches unchanged
4. `ResolveQualifierOnAxis` gets a new path for `TypedBinaryOp` — currently returns `null` for all `TypedBinaryOp` subjects, so any change is strictly an improvement (null → resolved qualifier)
5. The six operation metadata changes add a named parameter that was previously defaulted — the only behavioral change is the `MapQualifierBinding` branch producing a non-null binding instead of null

The one non-trivial risk is that `SameQualifierRequired` on `TypedBinaryOp` subjects now recurses into operands instead of returning null. This is correct behavior (the qualifier IS there), but if there are any tests that expect PRE0114 on nested same-qualifier operations, those tests would start passing. That is a bug fix, not a regression.

#### Files Changed

| File | Change Type | Lines |
|------|-------------|-------|
| `src/Precept/Language/Operation.cs` | Add enum value | ~1 LOC |
| `src/Precept/Pipeline/SemanticIndex.cs` | Add QualifierBinding subtype | ~4 LOC |
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | New branch in `MapQualifierBinding` | ~3 LOC |
| `src/Precept/Language/Operations.cs` | Add `ResultQualifierPolicy` to 6 operations | ~6 LOC (one named param each) |
| `src/Precept/Pipeline/ProofEngine.cs` | New `TypedBinaryOp` path + helper methods | ~60 LOC |
| `test/Precept.Tests/Pipeline/ScalarOpQualifierPropagationTests.cs` | New test file | ~150 LOC |

#### Downstream Impact

- **Runtime:** Core fix — catalog metadata + type checker + proof engine. All three layers change.
- **Tooling:** N/A — no language server, completions, or semantic token changes. The new `QualifierBinding` subtype is internal to the pipeline.
- **MCP:** The `SyntaxReference` snippet is NOT modified. The snippet becomes correct because the compiler now handles it correctly — the example stays as-is, including `default '0.00 USD/kg'` on `UnitPrice` and the `FinalCost` computed expression.

---

### Part D Dependency Order

```
D1 (ConflictingModifiers)   — ✅ Done
D2 (ExchangeRate Qualifiers) — ✅ Done
D3 (LS Manifest)             — ✅ Done
D4 (Scalar-Op Qualifier)     — independent of D1–D3 and all C-slices
```

D4 is the only remaining D-series item. It is independent of all other plan parts. No other slice depends on it. It unblocks no downstream work except the test itself.

---

## Part E — BUG-A: Inventory-Item PRE0114 Resolution

**Added:** 2026-05-12
**Depends on:** C4 ✅ Done, D4 ✅ Done
**Blocks:** inventory-item.precept clean compile (modulo the 8 TypeMismatch sample design issues)

### Problem Statement

After C4 (TypedArgRef qualifier resolution) and D4 (scalar-op qualifier propagation), `inventory-item.precept` still produces **66 PRE0114 (`UnprovedQualifierCompatibility`)** errors and **8 PRE0018 (`TypeMismatch`)** errors. The original "BUG-A" label was too coarse — investigation reveals **4 distinct root causes** that interact and must be fixed in sequence.

### Error Census (66 PRE0114)

| Location | Axis | Count | Expression Pattern |
|----------|------|-------|--------------------|
| Rules (L123–133) | Unit (7), Currency (7 incl. compound) | 13 | `field >= interpolated_typed_constant` |
| State ensures (L146–156) | Unit (8), Currency (4) | 12 | `field > constant`, `field * field >= field`, complex boolean |
| Event ensures (L169–207) | Unit (9), Currency (2) | 11 | `arg > constant`, `arg.accessor == field` |
| Transition actions (L229–322) | Unit (9), Currency (15), Dimension↔Dimension chain (6) | 30 | `field +/- subexpression`, compound unit arithmetic |
| **Total** | | **66** | |

### 8 TypeMismatch Errors (Sample Design Issue — Separate)

The ReceiveShipment transitions contain a **grouping bug in the DSL source**, not a compiler bug. The expression:
```
ReceiveShipment.Rate * (ReceiveShipment.SupplierUnitCost * ReceiveShipment.PurchaseQty * StockingUnitsPerPurchaseUnit)
```
parses left-associatively as `((SupplierUnitCost * PurchaseQty) * StockingUnitsPerPurchaseUnit)`, giving `(price × quantity → money) × quantity → TypeMismatch`. The intended grouping `SupplierUnitCost * (PurchaseQty * StockingUnitsPerPurchaseUnit)` yields `price × (quantity × quantity → quantity) → money` which type-checks. The sample needs inner parentheses. This affects L228, L230, L234, L236, L239, L241 (the `TotalInventoryCost` and `AverageCost` set actions in ReceiveShipment transitions). The `set QuantityOnHand` actions (L229, L235, L240) don't involve price multiplication and type-check fine.

**Action:** Fix parenthesization in `samples/inventory-item.precept` as part of E3 or as a standalone cleanup. Not a compiler change.

### Root Cause Analysis

#### RC-1: Shared ParameterMeta Ambiguity in Subject Resolution (Foundational)

**Affects:** All 61 `QualifierCompatibilityProofRequirement` errors (29 Currency + 32 Unit)

**Mechanism:** Every `QualifierCompatibilityProofRequirement` for same-type operations uses the same static `ParameterMeta` instance for both `LeftSubject` and `RightSubject`:

```csharp
// Operations.cs L961 — both subjects wrap the same PMoney instance
new QualifierCompatibilityProofRequirement(
    new ParamSubject(PMoney), new ParamSubject(PMoney),
    QualifierAxis.Currency, "...")
```

In `ResolveParamInBinaryOp()` (ProofEngine.cs L275–287), `ReferenceEquals` matches the shared `PMoney` against `bom.Rhs` first (by design, to handle divisor proofs), so **both subjects always resolve to `bin.Right`**. The left operand is never examined.

**Consequence:**
- When `bin.Right` has a resolvable qualifier (e.g., a field with static `in 'USD'`), both sides get the same qualifier → proof **accidentally passes** (false positive risk — cross-currency arithmetic would not be caught)
- When `bin.Right` is a typed constant or subexpression with no resolution path, both sides get null → proof fails (the 61 errors we see)
- Diagnostic messages show `<unknown>` for BOTH operands because `GetFieldName()` can't resolve either (both point to the unresolvable right operand)

**Evidence:** The existing test `Money_plus_money_same_currency_proved` passes only because both sides accidentally resolve to `bin.Right` (F2 with 'USD'), which happens to give the correct answer. If the test had different currencies on F1 vs F2, the proof would pass incorrectly. The `Bare_money_plus_bare_money_obligation_fires` test works correctly by accident because the bare right operand returns null for both sides.

#### RC-2: No Qualifier Resolution Path for TypedInterpolatedTypedConstant

**Affects:** 36 errors (all rules, state ensures, event ensures involving `field/arg >= '...'` comparisons)

**Mechanism:** `ResolveQualifierOnAxis()` and `ResolveQualifierFromExpression()` handle `TypedFieldRef`, `TypedArgRef`, `TypedBinaryOp`, and `TypedMemberAccess`, but have no case for `TypedInterpolatedTypedConstant`. When an interpolated typed constant like `'0 {StockingUnit}'` is the comparison operand, the engine returns null.

The qualifier information IS present in the `TypedInterpolatedTypedConstant.Slots` — each `TypedInterpolationSlot` has an `InterpolationSlotKind` (`Currency`, `Unit`, `NumeratorUnit`, `DenominatorUnit`, etc.) and an `Expression` (typically a `TypedFieldRef` pointing to the qualifier source field). The proof engine just doesn't know how to extract it.

#### RC-3: Missing Qualifier Propagation Through Compound and Cross-Type Operations

**Affects:** 30 transition action errors (15 Currency, 9 Unit, 6 Dimension↔Dimension chain)

**Sub-cause 3a — CompoundUnitCancellationRequired returns null on all axes:**

`ResolveQualifierOnAxis()` (ProofEngine.cs L992–993) returns hard null for `CompoundUnitCancellationRequired` on EVERY axis. But compound unit cancellation only "cancels" the dimension/unit axis — currency is orthogonal and should propagate. For `price × quantity → money`, the result's currency is the price's currency. The engine should find it by recursing into the currency-bearing operand.

The 15 Currency transition errors all involve `quantity × price → money` (PriceTimesQuantity via BidirectionalLookup) where the result feeds into a `money + money` or `money - money` operation. The currency from the price operand (AverageCost or ListPrice) should propagate through.

**Sub-cause 3b — Operations with null ResultQualifier:**

`PriceTimesQuantity` (Operations.cs L610) and `ExchangeRateTimesMoney` (L656) produce qualified results (money with currency) but have no `ResultQualifierPolicy` and no `Match` parameter → `MapQualifierBinding()` returns null → `TypedBinaryOp.ResultQualifier` is null → the binary-op path in `ResolveQualifierOnAxis` is skipped entirely.

These operations need either:
- A `ResultQualifierPolicy` (for PriceTimesQuantity: the currency comes from the price operand)
- A null-ResultQualifier fallback in the proof engine that tries operands

**Sub-cause 3c — Compound unit numerator extraction for Unit/Dimension axis:**

The 9 Unit errors and 6 Dimension↔Dimension chain errors involve expressions like `QuantityOnHand + PurchaseQty * StockingUnitsPerPurchaseUnit`. The compound cancellation result's unit is structurally derivable — for `quantity(of PurchaseUnit) × quantity(in StockingUnit/PurchaseUnit)`, the PurchaseUnit denominator cancels, leaving StockingUnit. But extracting the result unit requires parsing the compound unit qualifier string `{StockingUnit}/{PurchaseUnit}` to find the numerator.

#### RC-4: Cross-Axis Symbolic Qualifier Comparison

**Affects:** Residual errors after RC-1/2/3 fixes. Exact count TBD but potentially all 36 typed-constant errors would fail comparison without this.

**Mechanism:** Even with perfect qualifier extraction from both sides, the comparison `leftQualifier == rightQualifier` (record equality) fails when the two sides produce different `DeclaredQualifierMeta` subtypes encoding the same logical constraint:

- Field `QuantityOnHand` has `of '{StockingUnit.dimension}'` → `DeclaredQualifierMeta.Dimension("{StockingUnit.dimension}")`
- Typed constant `'0 {StockingUnit}'` has Unit slot → would produce `DeclaredQualifierMeta.Unit("{StockingUnit}")`
- Record equality: `Dimension("{StockingUnit.dimension}") ≠ Unit("{StockingUnit}")` → proof fails

Both qualifiers derive from `StockingUnit`. A unit implies its own dimension. The proof engine needs **symbolic equivalence**: extract the source field from interpolated template strings (e.g., `"{StockingUnit.dimension}"` → `StockingUnit`, `"{StockingUnit}"` → `StockingUnit`) and compare the source fields.

### Implementation Slices

#### Slice E1 — Shared ParameterMeta Disambiguation (RC-1)

**Status:** ✅ Done (`d549b4a5`) — 7 targeted tests pass
**Depends on:** None
**Blocks:** E2, E3, E4 (foundational — without this, qualification resolution gives wrong operand)

##### Root Cause

`ResolveParamInBinaryOp()` uses `ReferenceEquals(param, bom.Rhs)` which always matches first for same-type operations because `bom.Lhs` and `bom.Rhs` are the same static `ParameterMeta` instance. Both `QualifierCompatibilityProofRequirement` subjects resolve to `bin.Right`.

##### Design

**Bypass `ResolveSubject` for QualifierCompatibilityProofRequirement — resolve operands directly from the site.**

The `QualifierCompatibilityProofRequirement` semantics are: "the two operands of this binary operation must have compatible qualifiers on the specified axis." The left/right subject distinction is an artifact of the `ParamSubject` representation — the requirement always means "left operand vs. right operand." We can resolve directly from the obligation site.

**File:** `src/Precept/Pipeline/ProofEngine.cs` — method `TryQualifierCompatibilityProof()`

```csharp
private static bool TryQualifierCompatibilityProof(ProofObligation obligation, SemanticIndex semantics)
{
    if (obligation.Requirement is QualifierCompatibilityProofRequirement qcReq)
    {
        // ── Direct operand access (E1) ──
        // Same-type operations use shared ParameterMeta, making
        // ResolveSubject ambiguous. Access operands directly from the site.
        if (obligation.Site is not TypedBinaryOp binOp)
            return false;

        var leftQualifier = ResolveQualifierFromExpression(binOp.Left, qcReq.Axis, semantics);
        var rightQualifier = ResolveQualifierFromExpression(binOp.Right, qcReq.Axis, semantics);

        if (leftQualifier is null || rightQualifier is null)
            return false;

        // PeriodDimension.Any guard (unchanged)
        if (qcReq.Axis == QualifierAxis.TemporalDimension)
        {
            if (leftQualifier is DeclaredQualifierMeta.TemporalDimension { Value: PeriodDimension.Any }
                || rightQualifier is DeclaredQualifierMeta.TemporalDimension { Value: PeriodDimension.Any })
                return false;
        }

        return leftQualifier == rightQualifier;
    }

    // QualifierChainProofRequirement path unchanged (uses different param types, no ambiguity)
    if (obligation.Requirement is QualifierChainProofRequirement chainReq)
    {
        // ... existing code unchanged ...
    }

    return false;
}
```

Also update `CreateDiagnostic` for `QualifierCompatibilityProofRequirement` to extract operand names directly from the site expression:

```csharp
case QualifierCompatibilityProofRequirement qcReq:
    var leftName = obligation.Site is TypedBinaryOp qcBin
        ? GetFieldName(qcBin.Left) ?? "<expression>"
        : GetFieldName(qcReq.LeftSubject, obligation.Site) ?? "<unknown>";
    var rightName = obligation.Site is TypedBinaryOp qcBin2
        ? GetFieldName(qcBin2.Right) ?? "<expression>"
        : GetFieldName(qcReq.RightSubject, obligation.Site) ?? "<unknown>";
    return Diagnostics.Create(DiagnosticCode.UnprovedQualifierCompatibility, obligation.Site.Span,
        leftName, rightName, qcReq.Axis.ToString(), $" in {contextDesc}");
```

##### Test Cases

| Test | Input | Expected |
|------|-------|----------|
| `Same_currency_fields_proved` (existing) | `F1 in 'USD' + F2 in 'USD'` | Proved (was accidentally correct, now correct for right reason) |
| `Cross_currency_fields_now_detected` (NEW) | `F1 in 'USD' + F2 in 'EUR'` | Unresolved, PRE0114 diagnostic |
| `Bare_money_obligation_fires` (existing) | `F1 bare + F2 bare` | Unresolved (unchanged) |
| `Operand_names_in_diagnostics` (NEW) | `F1 in 'USD' + F2 in 'EUR'` | Message contains "F1" and "F2" (not `<unknown>`) |
| `Quantity_same_dimension_proved` (NEW) | `Q1 of 'mass' + Q2 of 'mass'` | Proved |
| `Quantity_different_dimension_detected` (NEW) | `Q1 of 'mass' + Q2 of 'length'` | Unresolved |
| `Price_same_qualifiers_proved` (NEW) | `P1 in 'USD' of 'mass' + P2 in 'USD' of 'mass'` | Proved |

**Files changed:** `src/Precept/Pipeline/ProofEngine.cs` (~30 LOC)
**Test file:** `test/Precept.Tests/ProofEngineTests.cs` (add to existing QualifierCompatibility class, ~80 LOC)
**Regression risk:** Low. This fixes a silent false positive bug (cross-qualifier same-type operations were accidentally passing). Existing tests pass because they use same-qualifier operands. The new `Cross_currency_fields_now_detected` test validates the fix catches real mismatches.

##### Impact on Inventory-Item PRE0114

**0 errors resolved** by E1 alone. E1 makes resolution correct (left operand → left qualifier, right operand → right qualifier) but the right operands still have no resolution path. E1 is a prerequisite for E2/E3/E4 to function correctly.

---

#### Slice E2 — Interpolated Typed Constant Qualifier Extraction (RC-2)

**Status:** 🔲 Not Started
**Depends on:** E1
**Blocks:** None (E4 is needed alongside for symbolic comparison to match)

##### Root Cause

`ResolveQualifierFromExpression()` has no case for `TypedInterpolatedTypedConstant`. The qualifier information is in the slots but the engine can't extract it.

##### Design

**Add a `TypedInterpolatedTypedConstant` case to `ResolveQualifierFromExpression()`** that maps `InterpolationSlotKind` to `QualifierAxis` and creates a `DeclaredQualifierMeta` from the slot expression.

**File:** `src/Precept/Pipeline/ProofEngine.cs` — method `ResolveQualifierFromExpression()`

```csharp
case TypedInterpolatedTypedConstant itc:
    return ResolveQualifierFromInterpolatedConstant(itc, axis);
```

New helper:

```csharp
private static DeclaredQualifierMeta? ResolveQualifierFromInterpolatedConstant(
    TypedInterpolatedTypedConstant itc, QualifierAxis axis)
{
    // Map InterpolationSlotKind → QualifierAxis
    InterpolationSlotKind? targetSlot = axis switch
    {
        QualifierAxis.Currency => InterpolationSlotKind.Currency,
        QualifierAxis.Unit => InterpolationSlotKind.Unit,
        QualifierAxis.Dimension => InterpolationSlotKind.Unit, // fallback: unit implies dimension
        QualifierAxis.FromCurrency => InterpolationSlotKind.FromCurrency,
        QualifierAxis.ToCurrency => InterpolationSlotKind.ToCurrency,
        _ => null,
    };

    if (targetSlot is null) return null;

    foreach (var slot in itc.Slots)
    {
        if (slot.SlotKind == targetSlot)
            return CreateQualifierFromSlotExpression(slot.Expression, axis);
    }

    // For compound units ('0 {X}/{Y}'), Currency axis → NumeratorUnit slot
    if (axis == QualifierAxis.Currency)
    {
        foreach (var slot in itc.Slots)
            if (slot.SlotKind == InterpolationSlotKind.NumeratorUnit)
                return CreateQualifierFromSlotExpression(slot.Expression, axis);
    }

    // For compound units, Dimension/Unit axis → DenominatorUnit slot
    if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)
    {
        foreach (var slot in itc.Slots)
            if (slot.SlotKind == InterpolationSlotKind.DenominatorUnit)
                return CreateQualifierFromSlotExpression(slot.Expression, axis);
    }

    return null;
}

private static DeclaredQualifierMeta? CreateQualifierFromSlotExpression(
    TypedExpression expr, QualifierAxis axis)
{
    var fieldName = expr switch
    {
        TypedFieldRef f => f.FieldName,
        TypedArgRef a => a.ArgName,
        _ => null,
    };
    if (fieldName is null) return null;

    // Create an interpolated qualifier template matching the source field
    return axis switch
    {
        QualifierAxis.Currency => new DeclaredQualifierMeta.Currency($"{{{fieldName}}}"),
        QualifierAxis.Unit => new DeclaredQualifierMeta.Unit($"{{{fieldName}}}"),
        QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension($"{{{fieldName}}}"),
        QualifierAxis.FromCurrency => new DeclaredQualifierMeta.FromCurrency($"{{{fieldName}}}"),
        QualifierAxis.ToCurrency => new DeclaredQualifierMeta.ToCurrency($"{{{fieldName}}}"),
        _ => null,
    };
}
```

##### Test Cases

| Test | Input | Expected |
|------|-------|----------|
| `Quantity_field_gte_interpolated_constant` | `Q of 'mass' >= '0 kg'` (static constant) | Proved |
| `Money_field_gte_interpolated_constant` | `M in 'USD' >= '0.00 USD'` (static constant) | Proved |
| `Price_field_gt_compound_constant` | `P in 'USD' of 'mass' > '0 USD/kg'` | Proved (both axes) |
| `Cross_currency_constant_detected` | `M in 'USD' >= '0.00 EUR'` | Unresolved (correct: USD ≠ EUR) |

**Files changed:** `src/Precept/Pipeline/ProofEngine.cs` (~50 LOC)
**Test file:** `test/Precept.Tests/ProofEngineTests.cs` (~60 LOC)

##### Impact on Inventory-Item PRE0114

**Depends on E4 for full effect.** E2 extracts qualifiers from typed constants, but for interpolated constants like `'0 {StockingUnit}'`, the extracted qualifier `Unit("{StockingUnit}")` won't match the field's `Dimension("{StockingUnit.dimension}")` without symbolic comparison (E4). E2 + E4 together resolve **36 errors** (all rules, state ensures, event ensures).

---

#### Slice E3 — Subexpression Qualifier Propagation (RC-3)

**Status:** 🔲 Not Started
**Depends on:** E1
**Blocks:** None

##### Root Cause

Three sub-causes prevent qualifier resolution through subexpressions:
1. `CompoundUnitCancellationRequired` returns null on all axes (ProofEngine.cs L992-993)
2. `PriceTimesQuantity` has null `ResultQualifier` (no binding declared)
3. Compound unit numerator extraction not implemented

##### Design

**Part A — Currency propagation through compound operations (~15 LOC)**

For `CompoundUnitCancellationRequired`, currency is orthogonal to the cancelled dimension — propagate it by trying both operands:

**File:** `src/Precept/Pipeline/ProofEngine.cs` — methods `ResolveQualifierOnAxis()` and `ResolveQualifierFromExpression()`

```csharp
case CompoundUnitCancellationRequired:
    // Currency propagates through compound unit cancellation (orthogonal to dimension)
    if (axis == QualifierAxis.Currency || axis == QualifierAxis.FromCurrency || axis == QualifierAxis.ToCurrency)
        return ResolveQualifierFromExpression(binOp.Left, axis, semantics)
            ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics);
    return null; // Unit/Dimension axes: result is a derived dimension (see Part C)
```

**Part B — ResultQualifier for PriceTimesQuantity (~5 LOC per operation)**

Add `ResultQualifierPolicy: ResultQualifierPolicy.CompoundUnitCancellation` to `PriceTimesQuantity` in the operations catalog. This is semantically correct — the dimension cancels (price's denominator matches quantity's dimension), and currency propagates from the price operand. The existing `CompoundUnitCancellationRequired` binding in `ResolveQualifierOnAxis` + Part A currency propagation handles it.

**File:** `src/Precept/Language/Operations.cs` — `PriceTimesQuantity` entry (L610)

```csharp
OperationKind.PriceTimesQuantity => new BinaryOperationMeta(
    kind, OperatorKind.Times, PPrice, PQuantity, TypeKind.Money,
    "Price × quantity → money (dimensional cancellation)", BidirectionalLookup: true,
    ResultQualifierPolicy: ResultQualifierPolicy.CompoundUnitCancellation,  // ← ADD
    ProofRequirements:
    [
        new QualifierChainProofRequirement(...),
    ]),
```

Similarly for `PriceTimesPeriod` (L620) and `PriceTimesDuration` (L630) if they appear in expressions that need currency propagation.

**Part C — Compound unit numerator extraction for Unit/Dimension (~30 LOC)**

For compound unit cancellation results on the Unit/Dimension axis, extract the numerator from the compound qualifier string:

```csharp
case CompoundUnitCancellationRequired:
    if (axis == QualifierAxis.Currency || ...)
        // Part A (above)
    // Unit/Dimension: try compound unit numerator extraction
    return TryResolveCompoundCancellationUnit(binOp, axis, semantics);
```

New helper:

```csharp
private static DeclaredQualifierMeta? TryResolveCompoundCancellationUnit(
    TypedBinaryOp binOp, QualifierAxis axis, SemanticIndex semantics)
{
    // Find the operand with a compound qualifier (contains '/')
    var leftQ = ResolveQualifierFromExpression(binOp.Left, axis, semantics);
    var rightQ = ResolveQualifierFromExpression(binOp.Right, axis, semantics);

    // Try to find a compound unit and extract numerator
    var compoundValue = ExtractCompoundValue(leftQ) ?? ExtractCompoundValue(rightQ);
    if (compoundValue is null) return null;

    var slashIdx = compoundValue.IndexOf('/');
    if (slashIdx < 0) return null;

    var numerator = compoundValue[..slashIdx].Trim();

    return axis switch
    {
        QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(numerator),
        QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(numerator),
        _ => null,
    };
}

private static string? ExtractCompoundValue(DeclaredQualifierMeta? qualifier) => qualifier switch
{
    DeclaredQualifierMeta.Unit { UnitCode: var code } when code.Contains('/') => code,
    DeclaredQualifierMeta.Dimension { DimensionName: var name } when name.Contains('/') => name,
    _ => null,
};
```

**Note on ExchangeRateTimesMoney:** This operation's result currency is the exchange rate's `to` currency, which is fundamentally different from "propagate from an operand." The proof engine would need a new qualifier binding type (`ResultQualifierPolicy.InheritFromOperandAxis` with axis parameters) or rely on the event ensures (`Rate.to == CatalogCurrency`) as runtime constraints. This is **deferred** — the ReceiveShipment expressions are already TypeMismatch-tainted (by the sample grouping bug), so no PRE0114 errors from exchange rate chains appear in the current count.

##### Test Cases

| Test | Input | Expected |
|------|-------|----------|
| `PriceTimesQuantity_currency_propagates` (NEW) | `set M = P_usd * Q` where M is `money in 'USD'` | Proved (currency from price) |
| `CompoundUnit_cancellation_currency_propagates` (NEW) | `set M = Q1 * Q2 * P` with compound units | Proved (currency from price through chain) |
| `CompoundUnit_numerator_unit_extracted` (NEW) | `set Q = Q_a * Q_compound` where Q_compound is `in '{X}/{Y}'` | Proved (numerator extracted) |
| `Existing_compound_cancellation_tests` | All existing | Pass (regression) |

**Files changed:** `src/Precept/Pipeline/ProofEngine.cs` (~60 LOC), `src/Precept/Language/Operations.cs` (~3 LOC)
**Test file:** `test/Precept.Tests/ProofEngineTests.cs` (~80 LOC)

##### Impact on Inventory-Item PRE0114

E1 + E3 resolves **30 transition action errors**:
- 15 Currency errors (all FulfillOrder, ReturnOrder, RecordShrinkage transitions) — via currency propagation through PriceTimesQuantity
- 9 Unit errors (ReceiveShipment, FulfillOrder, ReturnOrder quantity arithmetic) — via compound unit numerator extraction
- 6 Dimension↔Dimension chain errors (FulfillOrder, ReturnOrder cost calculations) — via compound unit numerator providing dimension for chain proofs

---

#### Slice E4 — Symbolic Qualifier Equivalence for Interpolated Templates (RC-4)

**Status:** ✅ Done (`d9464ab2`)
**Depends on:** E1
**Blocks:** E2, E3 (full effect)

##### Root Cause

Record equality `leftQualifier == rightQualifier` fails when both sides reference the same logical constraint through different `DeclaredQualifierMeta` subtypes:
- `Dimension("{StockingUnit.dimension}")` (from field `of '{StockingUnit.dimension}'`)
- `Unit("{StockingUnit}")` (from typed constant slot `{StockingUnit}`)

Both derive from `StockingUnit`. A unit implies its own dimension. The proof engine needs symbolic comparison.

##### Design

**Replace `leftQualifier == rightQualifier` with `QualifiersSymbolicallyEqual(left, right)` in `TryQualifierCompatibilityProof` and the chain qualifier comparison.**

**File:** `src/Precept/Pipeline/ProofEngine.cs`

```csharp
/// <summary>
/// Compares two qualifier values for symbolic equivalence.
/// Two interpolated qualifiers are equivalent if they derive from the same source field,
/// even when they differ in axis subtype (e.g., Dimension("{X.dimension}") ≈ Unit("{X}")).
/// </summary>
private static bool QualifiersSymbolicallyEqual(DeclaredQualifierMeta left, DeclaredQualifierMeta right)
{
    // Record equality first (covers static qualifiers like Currency("USD") == Currency("USD"))
    if (left == right) return true;

    // Interpolated template comparison: extract source field and compare
    var leftSource = ExtractSourceField(left);
    var rightSource = ExtractSourceField(right);
    return leftSource is not null && rightSource is not null
        && string.Equals(leftSource, rightSource, StringComparison.Ordinal);
}

/// <summary>
/// Extracts the source field name from an interpolated qualifier template.
/// "{CatalogCurrency}" → "CatalogCurrency"
/// "{StockingUnit.dimension}" → "StockingUnit"
/// Static values (no braces) → null (use record equality instead)
/// </summary>
private static string? ExtractSourceField(DeclaredQualifierMeta qualifier)
{
    var raw = qualifier switch
    {
        DeclaredQualifierMeta.Currency { CurrencyCode: var v } => v,
        DeclaredQualifierMeta.Unit { UnitCode: var v } => v,
        DeclaredQualifierMeta.Dimension { DimensionName: var v } => v,
        DeclaredQualifierMeta.FromCurrency { CurrencyCode: var v } => v,
        DeclaredQualifierMeta.ToCurrency { CurrencyCode: var v } => v,
        DeclaredQualifierMeta.TemporalUnit { UnitName: var v } => v,
        _ => null,
    };

    if (raw is null || !raw.StartsWith('{') || !raw.EndsWith('}')) return null;

    var inner = raw[1..^1]; // Strip braces
    var dotIdx = inner.IndexOf('.');
    return dotIdx >= 0 ? inner[..dotIdx] : inner; // "X.dimension" → "X"
}
```

Update the comparison in `TryQualifierCompatibilityProof` (E1 code):
```csharp
// Was: return leftQualifier == rightQualifier;
return QualifiersSymbolicallyEqual(leftQualifier, rightQualifier);
```

Update the chain qualifier comparison in `ChainQualifiersMatch()`:
```csharp
private static bool ChainQualifiersMatch(DeclaredQualifierMeta left, DeclaredQualifierMeta right)
{
    // Existing cross-axis string comparison
    var leftValue = ExtractComparableValue(left);
    var rightValue = ExtractComparableValue(right);
    if (leftValue is not null && rightValue is not null
        && string.Equals(leftValue, rightValue, StringComparison.Ordinal))
        return true;

    // Symbolic equivalence for interpolated qualifiers
    return QualifiersSymbolicallyEqual(left, right);
}
```

##### Test Cases

| Test | Left Qualifier | Right Qualifier | Expected |
|------|---------------|-----------------|----------|
| `Same_template_equal` | `Dimension("{X.dimension}")` | `Dimension("{X.dimension}")` | true (record equality) |
| `Cross_axis_symbolic_equal` | `Dimension("{X.dimension}")` | `Unit("{X}")` | true (same source "X") |
| `Different_source_not_equal` | `Dimension("{X.dimension}")` | `Unit("{Y}")` | false |
| `Static_qualifiers_unchanged` | `Currency("USD")` | `Currency("USD")` | true (record equality) |
| `Static_vs_interpolated_not_equal` | `Currency("USD")` | `Currency("{C}")` | false (static has no source) |
| `Compound_numerator_symbolic` | `Dimension("{StockingUnit.dimension}")` | `Unit("{StockingUnit}")` from numerator extraction | true |

**Files changed:** `src/Precept/Pipeline/ProofEngine.cs` (~40 LOC)
**Test file:** `test/Precept.Tests/ProofEngineTests.cs` (~60 LOC)

##### Impact on Inventory-Item PRE0114

E4 enables E2's extracted qualifiers to match field qualifiers across axis types. Without E4, E2 would extract qualifiers from typed constants but the comparison would fail for interpolated fields (which use dimension templates like `{StockingUnit.dimension}`) vs typed constant units (which produce `{StockingUnit}`).

E1 + E2 + E4 together resolve **36 rule/ensure/event-ensure errors**.
E1 + E3 + E4 together resolve all **30 transition errors**.
All four slices together: **66 errors → 0 PRE0114**.

---

### Part E Dependency Order

```
E1 (Shared ParameterMeta Fix)     — foundational, no dependencies
  ├→ E2 (Typed Constant Extraction) — depends on E1
  │    └→ E4 (Symbolic Comparison)  — depends on E2, co-required for typed constant proofs
  └→ E3 (Subexpression Propagation) — depends on E1
```

**Recommended implementation order:** E1 → E4 → E2 → E3

Rationale: E1 is foundational. E4 is pure comparison logic with no extraction dependency (can be tested with synthetic qualifier values). E2 needs E4 for its proofs to match. E3 is independent of E2/E4 and can be done in parallel after E1.

### Residual Work After Part E

1. **Sample parenthesization fix** — `samples/inventory-item.precept` ReceiveShipment transitions need inner parentheses to fix the 8 TypeMismatch errors. Not a compiler change.
2. **ExchangeRate result currency binding** — a new `ResultQualifierPolicy` for `ExchangeRateTimesMoney` that declares "result currency = exchange rate TO currency." Needed when the sample grouping is fixed and exchange rate chains no longer produce TypeMismatch. Can be a follow-up E5 slice or deferred.
3. **Cross-qualifier false positive audit** — E1 exposes that same-type operations with mismatched qualifiers were silently passing. Audit sample files and tests for latent cross-qualifier bugs that were hidden by the shared ParameterMeta issue.

### File Inventory

| File | E1 | E2 | E3 | E4 | Change Type |
|------|----|----|----|----|-------------|
| `src/Precept/Pipeline/ProofEngine.cs` | ✓ | ✓ | ✓ | ✓ | Core fix — proof resolution + comparison |
| `src/Precept/Language/Operations.cs` | | | ✓ | | Add ResultQualifierPolicy to PriceTimesQuantity |
| `test/Precept.Tests/ProofEngineTests.cs` | ✓ | ✓ | ✓ | ✓ | New tests in existing qualifier class |
| `test/Precept.Tests/ProofEngineTypedArgQualifierTests.cs` | | | | | Update baseline assertion (66 → 0) |
| `samples/inventory-item.precept` | | | ✓* | | Fix ReceiveShipment parenthesization (sample fix) |

\* The sample fix is separate from the compiler work — no diagnostic code changes.

---

## Part F — Sample Completeness Fixes

**Goal:** All 30 `.precept` sample files compile with zero diagnostics.

**Scope:** 46 diagnostics across 10 files, traced to 4 root causes.

**Architect:** Frank (frank-sample-completeness analysis, 2026-05-12)

**Design decisions required:** Q1 (ExchangeRateTimesMoney policy), Q2 (TypedLiteral node shape) — filed in `.squad/decisions/inbox/frank-sample-errors-design-questions.md`

**Complete error inventory:** `.squad/decisions/inbox/frank-sample-errors-analysis.md`

---

### F1 — Sample Fix: `optional notempty` → `optional`

**Type:** Sample fix
**Diagnostics cleared:** 8 × PRE0120 (ConflictingModifiers)
**Effort:** Small
**Dependencies:** None
**Owner:** Any team member

**Root cause:** The `notempty` modifier requires a value to be non-empty when present. The `optional` modifier allows a field/arg to be absent. These are mutually exclusive — if a value is absent, the `notempty` check is meaningless. The type checker correctly rejects this combination.

**Fix:** In each file, change `optional notempty` to just `optional` on the affected event arg declarations. The author's intent is that the argument is optional — when provided, string validation handles content requirements at the application layer, not the contract level.

**Affected files and lines:**

| File | Line | Current | Fix |
|------|------|---------|-----|
| `samples/it-helpdesk-ticket.precept` | 32 | `Note as string optional notempty` | `Note as string optional` |
| `samples/library-book-checkout.precept` | 46 | `Condition as string optional notempty` | `Condition as string optional` |
| `samples/library-book-checkout.precept` | 47 | (second arg) `optional notempty` | `optional` |
| `samples/loan-application.precept` | 44 | `Note as string optional notempty` | `Note as string optional` |
| `samples/maintenance-work-order.precept` | 53 | `Reason as string optional notempty` | `Reason as string optional` |
| `samples/refund-request.precept` | 32 | `Note as string optional notempty` | `Note as string optional` |
| `samples/refund-request.precept` | 37 | `Note as string optional notempty` | `Note as string optional` |
| `samples/travel-reimbursement.precept` | 40 | `Note as string optional notempty` | `Note as string optional` |

**Validation:** After edit, each file's PRE0120 count drops to 0. No other diagnostics should change (the modifier fix is independent of qualifier/type errors).

---

### F2 — Sample Fix: `number` → `decimal` Type Mismatch in travel-reimbursement

**Type:** Sample fix
**Diagnostics cleared:** 2 × PRE0018 (TypeMismatch)
**Effort:** Small
**Dependencies:** None
**Owner:** Any team member

**Root cause:** `travel-reimbursement.precept` declares fields `LodgingTotal` and `MealsTotal` as `decimal` (L10–11), but the `Submit` event declares args `Lodging` and `Meals` as `number` (L30–31). The transition `set LodgingTotal = Submit.Lodging` assigns `number` to `decimal` — a type mismatch. The types are not interchangeable in Precept's type system.

**Fix:** Change the `Submit` event arg types from `number` to `decimal`:

```
// Before (L30-31):
Lodging as number,
Meals as number,

// After:
Lodging as decimal,
Meals as decimal,
```

**Affected file:** `samples/travel-reimbursement.precept` lines 30–31

**Validation:** PRE0018 count drops from 2 to 0. The `RequestedTotal` computed field (`LodgingTotal + MealsTotal + MileageTotal`) remains valid since all three are `decimal`. No cascading changes needed — the ensures on `Submit.Lodging` and `Submit.Meals` remain valid for `decimal`.

---

### F3 — Compiler Fix: Static Typed Constant Qualifier Extraction

**Type:** Compiler fix (proof engine)
**Diagnostics cleared:** 9 × PRE0114 (UnprovedQualifierCompatibility) across 4 files
**Effort:** Medium
**Dependencies:** Design decision Q2 (TypedLiteral node shape)
**Owner:** George

**Root cause:** The proof engine cannot extract qualifiers from static typed constant literals in comparison expressions. When evaluating `MonthlyIncome > '0.00 USD'`, the proof engine resolves `MonthlyIncome`'s `Currency(USD)` qualifier from the field declaration but returns `null` for the typed constant `'0.00 USD'` — because `ResolveQualifierFromExpression()` has no branch for `TypedLiteral` nodes.

**Affected files (samples cleared by this fix):**
- `samples/apartment-rental-application.precept` — 2 errors cleared
- `samples/hiring-pipeline.precept` — 1 error cleared
- `samples/insurance-claim.precept` — 1 error cleared
- `samples/loan-application.precept` — 5 errors cleared

**Implementation:**

**Step 1 — Extend `TypedLiteral` node** (pending Q2 decision):

```csharp
// In SemanticIndex.cs — add DeclaredQualifiers to TypedLiteral
public sealed record TypedLiteral(
    TypeKind ResultType,
    object? Value,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers,  // NEW — populated for typed constants only
    SourceSpan Span
) : TypedExpression(ResultType, Span);
```

**Step 2 — Populate qualifiers in type checker:**

In `TypeChecker.Expressions.cs`, wherever `TypedLiteral` is created for a typed constant resolution (the branch that handles `TokenKind.TypedConstant`), extract qualifiers from the `TypedConstantParseResult` (which already carries `DeclaredQualifiers`) and pass them to the `TypedLiteral` constructor.

Method: `ResolveLiteral()` in `TypeChecker.Expressions.cs` — the `TypedConstant` branch that creates the `TypedLiteral` node for static typed constants.

**Step 3 — Add proof engine branch:**

In `ProofEngine.cs`, `ResolveQualifierFromExpression()` (line ~1100), add a branch before the `default` case:

```csharp
case TypedLiteral { DeclaredQualifiers: { IsDefaultOrEmpty: false } litQuals }:
    foreach (var q in litQuals)
        if (q.Axis == axis) return q;
    if (axis == QualifierAxis.Unit)
        foreach (var q in litQuals)
            if (q.Axis == QualifierAxis.Dimension) return q;
    if (axis == QualifierAxis.Dimension)
        foreach (var q in litQuals)
            if (q.Axis == QualifierAxis.TemporalDimension) return q;
    return null;
```

This mirrors the existing `TypedArgRef` branch at line 1105.

**Step 4 — Also update `ResolveQualifierOnAxis()`** (line ~995):

Add the same `TypedLiteral` branch after the `TypedArgRef` block. The two resolution methods serve different proof paths (compatibility vs chain), and both need the new branch.

**Tests:**

- Add `MoneyFieldVsStaticTypedConstant_QualifierProved` test — `field X as money in 'USD'` + `ensure X > '0.00 USD'` → 0 PRE0114.
- Add `QuantityFieldVsStaticTypedConstant_QualifierProved` test — same pattern for quantity.
- Add `PriceFieldVsStaticTypedConstant_QualifierProved` test — `price in 'USD' of 'mass'` vs `'0 USD/kg'`.
- Add `CrossCurrencyStaticConstant_StillRejected` test — `field X as money in 'USD'` + `ensure X > '0.00 EUR'` → PRE0114 still fires (currencies don't match).

**Regression:** All existing proof engine tests must pass unchanged. The new qualifier extraction only adds resolution paths — it never removes them.

---

### F4 — ExchangeRateTimesMoney Result Qualifier Policy (Reframed)

**Type:** ~~Compiler fix~~ → **Already implemented; blocked on BUG-C**
**Diagnostics affected:** All ReceiveShipment qualifier diagnostics in `inventory-item.precept` (Currency <unresolved>, FromCurrency↔Currency chain failures)
**Status:** ✅ Policy exists — remaining diagnostics are a **data problem** (missing event arg qualifier), not a policy problem

**Reframing (2026-05-12 root-cause review):**

The original F4 description assumed the `CurrencyConversion` `ResultQualifierPolicy` and its proof engine handler were missing. **They are not.** Both exist and work correctly:

- `Operations.cs:678` — `ExchangeRateTimesMoney` already has `ResultQualifierPolicy: ResultQualifierPolicy.CurrencyConversion`
- `ProofEngine.cs:1194-1202` — `CurrencyConversionRequired` handler correctly resolves Currency axis by reading the exchangerate operand's `ToCurrency`
- `ProofEngine.cs:1315-1321` — Same handler in `ResolveQualifierFromExpression`
- `Operations.cs:681-683` — `QualifierChainProofRequirement` for `FromCurrency↔Currency` exists

**The actual problem:** `Rate as exchangerate` on the ReceiveShipment event (line 156) has **no qualifier declaration**. The `CurrencyConversion` handler calls `ResolveQualifierFromExpression(rateOperand, QualifierAxis.ToCurrency, semantics)` → resolves to the Rate arg → Rate arg has no `ToCurrency` qualifier metadata → returns null → result Currency is `<unresolved>`. Independently, the `FromCurrency↔Currency` chain proof resolves Rate's `FromCurrency` → null → chain proof fails.

**Why it can't be fixed without BUG-C:** The Rate arg needs `in '{SupplierCurrency}' to '{CatalogCurrency}'` — interpolated qualifiers on event args. This is BUG-C. The sample's runtime workaround (`ensure Rate.from == SupplierCurrency`, `ensure Rate.to == CatalogCurrency`) cannot be used by the proof engine for compile-time qualifier resolution — ensures are runtime guards, not qualifier declarations.

**When BUG-C ships:** The existing `CurrencyConversion` policy and proof engine handler handle it automatically. No additional compiler work is needed for F4 beyond BUG-C itself. Rate's `ToCurrency` qualifier will resolve to `{CatalogCurrency}`, the result Currency becomes `{CatalogCurrency}`, and assignment to `TotalInventoryCost (Currency: '{CatalogCurrency}')` passes.

**Design decision Q1 is resolved:** The question was "which policy approach for ExchangeRateTimesMoney?" — Answer: `CurrencyConversion`, and it's already implemented. Q1 is closed.

**Previous F4 content is superseded.** Steps 1–6 of the old F4 described work that already exists in the codebase.

---

### F5 — Verification Pass: Recompile All Samples

**Type:** Verification
**Diagnostics cleared:** Any residual after F1–F4
**Effort:** Small (diagnosis) to Medium (if additional fixes needed)
**Dependencies:** F1 + F2 + F3 + F4 all complete
**Owner:** Any team member

**Process:**

1. Recompile all 30 `.precept` sample files.
2. Confirm 20 clean files remain clean (no regressions).
3. Confirm F1 target files (6 files, 8 PRE0120) now compile clean.
4. Confirm F2 target file (travel-reimbursement, 2 PRE0018) now compiles clean.
5. Confirm F3 target files (4 files, 9 PRE0114) now compile clean.
6. Confirm F4 target file (inventory-item, 27 errors) — count remaining diagnostics.
7. Any residual diagnostics get individually diagnosed and resolved as F5a, F5b, etc.

**Expected residual areas (may need attention after F4):**

- `inventory-item.precept` L127/128 — compound-unit interpolated quantity qualifier matching in rule comparisons. If `ResolveQualifierFromExpression` for `TypedInterpolatedTypedConstant` doesn't handle compound-unit `NumeratorUnit`/`DenominatorUnit` slots correctly for the Unit axis, these may persist.
- `inventory-item.precept` L108 — GrossProfit computed field subtraction chain with interpolated `'{CatalogCurrency}'` qualifiers. Depends on whether E4 symbolic equivalence handles multi-hop binary operation chains.
- `inventory-item.precept` L144/151 — price ÷ quantity comparison with interpolated dimension qualifiers. May require compound-unit cancellation improvements.

---

### Dependency Graph

```
F1 (sample: optional notempty)      — independent, immediate
F2 (sample: number/decimal)         — independent, immediate
F3 (compiler: static TC qualifier)  — needs Q2 decision
F4 (ExchangeRate policy)            — ✅ policy exists; remaining work = BUG-C (event arg interpolated qualifiers)
F5 (verification)                   — after F1 + F2 + F3 + G1 + BUG-C
G1 (compound-unit typed constant)   — independent, immediate
G2 (compound expr DivisionByZero)   — after G1 + BUG-C
```

**Recommended execution order:** F1 → F2 → G1 (immediate, unblocked) → F3 (after Q2) → BUG-C → G2 → F5

F1, F2, and G1 can all be done immediately in parallel. G1 is the highest-value item: it resolves 4 diagnostics on inventory-item directly and unblocks downstream DivisionByZero reasoning.

### File Inventory

| File | F1 | F2 | F3 | F4 | F5 | G1 | G2 | Change Type |
|------|----|----|----|----|-----|----|----|-------------|
| `samples/it-helpdesk-ticket.precept` | ✓ | | | | | | | Sample fix |
| `samples/library-book-checkout.precept` | ✓ | | | | | | | Sample fix |
| `samples/loan-application.precept` | ✓ | | ✓ | | | | | Sample fix + cleared by F3 |
| `samples/maintenance-work-order.precept` | ✓ | | | | | | | Sample fix |
| `samples/refund-request.precept` | ✓ | | | | | | | Sample fix |
| `samples/travel-reimbursement.precept` | ✓ | ✓ | | | | | | Sample fix |
| `samples/apartment-rental-application.precept` | | | ✓ | | | | | Cleared by F3 |
| `samples/hiring-pipeline.precept` | | | ✓ | | | | | Cleared by F3 |
| `samples/insurance-claim.precept` | | | ✓ | | | | | Cleared by F3 |
| `samples/inventory-item.precept` | | | | — | ✓ | ✓ | ✓ | Cleared by G1 + BUG-C + G2 |
| `src/Precept/Pipeline/SemanticIndex.cs` | | | ✓ | — | | | | TypedLiteral DeclaredQualifiers |
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | | | ✓ | — | | | | Populate TypedLiteral qualifiers |
| `src/Precept/Pipeline/ProofEngine.cs` | | | ✓ | — | | ✓ | ✓ | ResolveQualifierFromInterpolatedConstant fix (G1) + compound DivisionByZero (G2) |
| `test/Precept.Tests/ProofEngineTests.cs` | | | ✓ | — | | ✓ | ✓ | New tests |

---

## Part G — Inventory-Item Proof Coverage Completion

**Added:** 2026-05-12
**Goal:** Resolve all remaining diagnostics on `samples/inventory-item.precept`
**Architect:** Frank (exhaustive root-cause review, 2026-05-12)

### Current Diagnostic Census (2026-05-12)

| Lines | Code | Count | Root Cause | Slice |
|-------|------|-------|------------|-------|
| 122–123 | PRE0114 (UnprovedQualifierCompatibility, Unit) | 2 | RC1: compound-unit typed constant qualifier construction | **G1** |
| 137, 142 | PRE0083 (DivisionByZero) | 2 | Cascading from RC1: rule L124 can't prove `StockingUnitsPerSaleUnit > 0` | **G1** (cascade) |
| 215–229 (×3 rows) | PRE0114 (UnprovedQualifierCompatibility, Currency) | ~6 | RC2: `Rate as exchangerate` has no qualifier → CurrencyConversion result `<unresolved>` | **F4 → BUG-C** |
| 215–229 (×3 rows) | PRE0114 (UnprovedQualifierCompatibility, FromCurrency↔Currency) | ~3–6 | RC2: `Rate` has no `FromCurrency` → chain proof fails | **F4 → BUG-C** |
| 215–229 (×3 rows) | PRE0083 (DivisionByZero) | 3 | RC3: compound expression `(QuantityOnHand + PurchaseQty × StockingUnitsPerPurchaseUnit)` can't be proved non-zero | **G2** |

**Total:** ~16–19 diagnostics across 3 root causes.

---

### Root Cause Analysis

#### RC1 — Compound-Unit Interpolated Constant Qualifier Construction Bug

**Slice:** G1
**Severity:** Blocking — cascades to DivisionByZero on lines 137/142
**Type:** ProofEngine bug (defect in shipped E2 code)

**What is failing:** Rules on lines 122–123 compare compound-unit quantity fields against compound-unit interpolated typed constants:
```
rule StockingUnitsPerPurchaseUnit > '0 {StockingUnit}/{PurchaseUnit}'
```
The field `StockingUnitsPerPurchaseUnit` has qualifier `Unit: '{StockingUnit}/{PurchaseUnit}'`. The typed constant `'0 {StockingUnit}/{PurchaseUnit}'` is parsed with `NumeratorUnit = StockingUnit` and `DenominatorUnit = PurchaseUnit` slots.

**Why it fails:** `ResolveQualifierFromInterpolatedConstant()` in `ProofEngine.cs` (line 1338) handles the Unit axis by first searching for a `InterpolationSlotKind.Unit` slot (no match for compound-unit constants — they use NumeratorUnit/DenominatorUnit instead). It then falls through to the DenominatorUnit-only fallback (line 1369–1376), returning `Unit("{PurchaseUnit}", "{PurchaseUnit}")`. The proof then compares:
- Field: `Unit("{StockingUnit}/{PurchaseUnit}")`
- Typed constant: `Unit("{PurchaseUnit}")`
→ **Mismatch.**

The function never checks for the NumeratorUnit+DenominatorUnit compound pair on the Unit axis. It was designed (E2) for simple unit slots and compound-unit fallback to DenominatorUnit alone — but compound-unit constants need both slots composed into `{numerator}/{denominator}`.

**Cascade:** Lines 137 and 142 have `ListPrice / StockingUnitsPerSaleUnit >= AverageCost`. The division generates a `NumericProofRequirement(NotEquals, 0)` on `StockingUnitsPerSaleUnit`. The proof engine looks for evidence from rules/ensures. Rule L124 (`StockingUnitsPerSaleUnit > '0 {StockingUnit}/{SaleUnit}'`) would establish `> 0` which subsumes `!= 0`. But because L124 itself has an unresolved qualifier obligation (same RC1 bug on line 123), the engine can't trust it as evidence. After RC1 fix, L124 compiles clean → `StockingUnitsPerSaleUnit > 0` becomes available → DivisionByZero on L137/L142 clears.

---

#### RC2 — Unqualified ExchangeRate Event Arg (BUG-C Dependency)

**Slice:** F4 (reframed)
**Severity:** Blocking — affects all ReceiveShipment transition diagnostics
**Type:** Data problem (missing qualifier), not a policy problem
**Blocked by:** BUG-C (interpolated qualifiers on event args)

**What is failing:** The `ReceiveShipment` event declares `Rate as exchangerate` (line 156) with no qualifier. The sample intends `in '{SupplierCurrency}' to '{CatalogCurrency}'` but this syntax requires BUG-C.

**Two independent proof failures from the same root cause:**

1. **Currency `<unresolved>` (PRE0114):** The `CurrencyConversionRequired` handler resolves result Currency by calling `ResolveQualifierFromExpression(rateOperand, QualifierAxis.ToCurrency, ...)`. Rate arg has no `ToCurrency` → returns null → result `Currency: <unresolved>` → assignment to `TotalInventoryCost (Currency: '{CatalogCurrency}')` fails qualifier compatibility. Same cascade on `AverageCost`.

2. **FromCurrency↔Currency chain (PRE0114):** The `QualifierChainProofRequirement` on `ExchangeRateTimesMoney` checks `Rate.FromCurrency == money_expr.Currency`. Rate has no `FromCurrency` → left side null → chain proof fails.

**Cannot be fixed without BUG-C:** The proof engine correctly resolves qualifiers from event arg declarations. The problem is the declaration has none. Runtime `ensure` guards (`Rate.from == SupplierCurrency`, `Rate.to == CatalogCurrency`) are not qualifier metadata — they're runtime constraints. Teaching the proof engine to infer qualifiers from ensures would be fragile, non-general, and architecturally wrong.

**When BUG-C ships:** `Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}'` → Rate gets `FromCurrency("{SupplierCurrency}")` and `ToCurrency("{CatalogCurrency}")`. The existing `CurrencyConversion` handler resolves `ToCurrency` → `{CatalogCurrency}` → result `Currency: '{CatalogCurrency}'` → assignment passes. The chain proof resolves `FromCurrency("{SupplierCurrency}")` vs `money_expr.Currency` → requires the money expression's currency to match SupplierCurrency (which it does — `SupplierUnitCost in '{SupplierCurrency}'`). The runtime `ensure` guards (lines 159–162) become compile-time guarantees and can be removed.

---

#### RC3 — Compound Expression DivisionByZero (Algebraic Proof Gap)

**Slice:** G2
**Severity:** Low (co-located with RC2 errors; would surface only after BUG-C + G1 land)
**Type:** Proof engine limitation — missing compositional algebraic reasoning

**What is failing:** The AverageCost computation divides by a compound expression:
```
(QuantityOnHand + ReceiveShipment.PurchaseQty * StockingUnitsPerPurchaseUnit)
```
The proof engine's `NumericProofRequirement(NotEquals, 0)` resolves the subject to this compound expression. `GetFieldName()` returns null for compound expressions (they're not field references), so `TryCompositionalConstraintProof()`, `TryModifierProof()`, `TryGuardProof()`, and `TryRuleOrEnsureProof()` all decline.

**What the engine would need to prove:** Given:
- `QuantityOnHand >= 0` (from rule L114)
- `PurchaseQty > 0` (from event ensure L157)
- `StockingUnitsPerPurchaseUnit > 0` (from rule L123, after G1 fix)

Therefore: `PurchaseQty * StockingUnitsPerPurchaseUnit > 0`, and `QuantityOnHand + positive > 0`, so the denominator is provably positive.

This requires **algebraic compositional reasoning** that the proof engine doesn't currently support: `nonneg + positive > 0`. The engine would need to:
1. Decompose the compound expression into its constituent operations
2. Recursively resolve numeric bounds on each sub-expression
3. Apply algebraic rules: `a >= 0 ∧ b > 0 → a + b > 0`, `a > 0 ∧ b > 0 → a × b > 0`

**Dependency:** Blocked on G1 (to prove `StockingUnitsPerPurchaseUnit > 0`) AND BUG-C (to establish `PurchaseQty > 0` from the event ensure, which may already work). Even if G1 lands, RC3 diagnostics would remain but be hidden by RC2's qualifier failures on the same lines. RC3 only becomes the last-standing issue after BUG-C ships.

---

### G1 — Compound-Unit Typed Constant Qualifier Construction

**Type:** Compiler fix (proof engine)
**Diagnostics cleared:** 4 — Lines 122, 123 (2 × PRE0114) + Lines 137, 142 (2 × PRE0083 DivisionByZero, cascade)
**Effort:** Small
**Dependencies:** None — **immediately actionable**
**Owner:** George

#### Root Cause

`ResolveQualifierFromInterpolatedConstant()` in `ProofEngine.cs` (line 1338) handles Unit/Dimension axis lookup for interpolated typed constants. When the constant has `InterpolationSlotKind.NumeratorUnit` and `InterpolationSlotKind.DenominatorUnit` slots (compound-unit constant like `'0 {StockingUnit}/{PurchaseUnit}'`), the function skips the NumeratorUnit and returns only the DenominatorUnit — producing `Unit("{PurchaseUnit}")` instead of the correct `Unit("{StockingUnit}/{PurchaseUnit}")`.

#### Fix

**File:** `src/Precept/Pipeline/ProofEngine.cs` — method `ResolveQualifierFromInterpolatedConstant()` (line 1338)

**Before (lines 1369–1376):**
```csharp
if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)
{
    foreach (var slot in itc.Slots)
    {
        if (slot.SlotKind == InterpolationSlotKind.DenominatorUnit)
            return CreateQualifierFromSlotExpression(slot.Expression, axis);
    }
}
```

**After:** Insert a compound-unit pair check BEFORE the DenominatorUnit-only fallback:

```csharp
if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)
{
    // Compound-unit pair: NumeratorUnit + DenominatorUnit → construct '{num}/{denom}'
    TypedExpression? numeratorExpr = null;
    TypedExpression? denominatorExpr = null;
    foreach (var slot in itc.Slots)
    {
        if (slot.SlotKind == InterpolationSlotKind.NumeratorUnit)
            numeratorExpr = slot.Expression;
        else if (slot.SlotKind == InterpolationSlotKind.DenominatorUnit)
            denominatorExpr = slot.Expression;
    }

    if (numeratorExpr is not null && denominatorExpr is not null)
    {
        var numName = GetSlotFieldName(numeratorExpr);
        var denomName = GetSlotFieldName(denominatorExpr);
        if (numName is not null && denomName is not null)
        {
            var compoundUnit = $"{{{numName}}}/{{{denomName}}}";
            return axis switch
            {
                QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(
                    compoundUnit, $"{{{numName}.dimension}}/{{{denomName}.dimension}}",
                    SourceFieldName: numName),
                QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(
                    $"{{{numName}.dimension}}/{{{denomName}.dimension}}",
                    SourceFieldName: numName),
                _ => null,
            };
        }
    }

    // Single DenominatorUnit fallback (non-compound case)
    foreach (var slot in itc.Slots)
    {
        if (slot.SlotKind == InterpolationSlotKind.DenominatorUnit)
            return CreateQualifierFromSlotExpression(slot.Expression, axis);
    }
}
```

Where `GetSlotFieldName` is a helper that extracts the field/arg name:
```csharp
private static string? GetSlotFieldName(TypedExpression expr) => expr switch
{
    TypedFieldRef f => f.FieldName,
    TypedArgRef a => a.ArgName,
    _ => null,
};
```

Note: `GetSlotFieldName` may already exist as part of `CreateQualifierFromSlotExpression` — if so, extract and reuse.

#### Test Cases

| Test | Input | Expected |
|------|-------|----------|
| `CompoundUnit_FieldVsInterpolatedConstant_SameQualifier` | `field X as quantity in '{A}/{B}'` + `rule X > '0 {A}/{B}'` | 0 PRE0114 (Unit qualifiers match: `{A}/{B}` == `{A}/{B}`) |
| `CompoundUnit_DifferentNumerator_Detected` | `field X as quantity in '{A}/{B}'` + `rule X > '0 {C}/{B}'` | PRE0114 fires (correct: `{A}/{B}` ≠ `{C}/{B}`) |
| `CompoundUnit_RuleProvesPositive_DivisionSafe` | `field X as quantity in '{A}/{B}'` + `rule X > '0 {A}/{B}'` + `ensure Y / X >= Z` | 0 PRE0083 (DivisionByZero cleared by rule evidence) |
| `SimpleUnit_DenominatorFallback_Unchanged` | `field X as price in 'USD' of 'mass'` + `ensure X >= '0 USD/kg'` | Existing behavior unchanged (no regression) |

**Files changed:** `src/Precept/Pipeline/ProofEngine.cs` (~25 LOC)
**Test file:** `test/Precept.Tests/ProofEngineTests.cs` (~60 LOC)
**Regression risk:** Low. The new compound-pair check runs before the existing DenominatorUnit fallback. The fallback path is unchanged. Existing tests for simple-unit typed constants are unaffected.

#### Impact on Inventory-Item

After G1:
- Lines 122–123: **Resolved** — compound-unit qualifier `{StockingUnit}/{PurchaseUnit}` constructed correctly
- Lines 137, 142: **Resolved** (cascade) — rule L124 now proves `StockingUnitsPerSaleUnit > 0` → DivisionByZero clears
- ReceiveShipment lines: **No change** — still blocked by RC2 (BUG-C)

**Net: 4 diagnostics cleared, remaining ~12–15 all BUG-C dependent.**

---

### G2 — Compound Expression DivisionByZero Algebraic Proof

**Type:** Compiler enhancement (proof engine — new algebraic reasoning)
**Diagnostics cleared:** 3 × PRE0083 (DivisionByZero on ReceiveShipment AverageCost denominator)
**Effort:** Medium–Large
**Dependencies:** G1 (prerequisite for StockingUnitsPerPurchaseUnit > 0) + BUG-C (prerequisite for PurchaseQty > 0 from event ensure; also, these diagnostics are currently masked by RC2 qualifier failures on the same lines)
**Owner:** George

#### Root Cause

The AverageCost computation's divisor `(QuantityOnHand + PurchaseQty * StockingUnitsPerPurchaseUnit)` is a compound expression. The proof engine's DivisionByZero proof strategy resolves the divisor subject and calls `GetFieldName()` — which returns null for expressions that aren't field references. All existing proof strategies (`TryModifierProof`, `TryGuardProof`, `TryRuleOrEnsureProof`, `TryCompositionalConstraintProof`) then decline because they require a resolvable field name.

#### Design

**File:** `src/Precept/Pipeline/ProofEngine.cs` — new method `TryAlgebraicNonZeroProof()`

Add a new proof strategy that decomposes binary operation expressions and recursively verifies numeric bounds:

```csharp
private static bool TryAlgebraicNonZeroProof(ProofObligation obligation, SemanticIndex semantics)
{
    if (obligation.Requirement is not NumericProofRequirement
        { Comparison: OperatorKind.NotEquals, Threshold: 0m })
        return false;

    var subject = ResolveSubject(obligation.Requirement.Subject, obligation.Site);
    if (subject is null) return false;

    return TryProvePositive(subject, obligation, semantics);
}
```

Core recursive helper:
```csharp
private static bool TryProvePositive(TypedExpression expr, ProofObligation ctx, SemanticIndex semantics)
{
    // Base case: field/arg reference — check rules, ensures, modifiers for > 0 or >= 0
    if (expr is TypedFieldRef or TypedArgRef)
    {
        var fieldName = GetFieldName(expr);
        // ... check rules/ensures/modifiers for numeric bounds
    }

    // Recursive case: binary operation
    if (expr is TypedBinaryOp binOp)
    {
        // Addition: a >= 0 ∧ b > 0 → a + b > 0 (or symmetric)
        if (IsAdditionOp(binOp.ResolvedOp))
        {
            return (TryProveNonnegative(binOp.Left, ...) && TryProvePositive(binOp.Right, ...))
                || (TryProvePositive(binOp.Left, ...) && TryProveNonnegative(binOp.Right, ...));
        }

        // Multiplication: a > 0 ∧ b > 0 → a × b > 0
        if (IsMultiplicationOp(binOp.ResolvedOp))
        {
            return TryProvePositive(binOp.Left, ...) && TryProvePositive(binOp.Right, ...);
        }
    }

    return false;
}
```

**Scope guard:** This reasoning is strictly for DivisionByZero proofs (`NotEquals 0`) and only triggers when simpler strategies fail. The recursion depth is bounded by AST depth (max ~5–6 levels in any real expression). No risk of infinite recursion.

**Note:** This is a significant proof engine enhancement. The exact implementation should be designed carefully with recursion limits and conservative fallbacks. The approach above is the conceptual design — implementation details (helper extraction, bounds tracking, caching) are left to the implementer.

#### Test Cases

| Test | Input | Expected |
|------|-------|----------|
| `Addition_NonnegPlusPositive_ProvesNonZero` | `field A >= 0` + `field B > 0` + `ensure X / (A + B) != err` | 0 PRE0083 |
| `Multiplication_PositiveTimesPositive_ProvesNonZero` | `field A > 0` + `field B > 0` + `ensure X / (A * B) != err` | 0 PRE0083 |
| `Addition_BothNonneg_StillFails` | `field A >= 0` + `field B >= 0` + `ensure X / (A + B) != err` | PRE0083 fires (correct: 0 + 0 = 0) |
| `NestedCompound_ReceiveShipmentPattern` | Pattern matching inventory-item: `QoH >= 0`, `PQ > 0`, `SUPP > 0` → `QoH + PQ * SUPP` | 0 PRE0083 |

**Files changed:** `src/Precept/Pipeline/ProofEngine.cs` (~80–120 LOC)
**Test file:** `test/Precept.Tests/ProofEngineTests.cs` (~80 LOC)

#### Impact on Inventory-Item

After G1 + BUG-C + G2:
- Lines 215–229 DivisionByZero: **Resolved** (3 × PRE0083 cleared)
- Combined with BUG-C clearing the Currency/FromCurrency errors: **all ReceiveShipment diagnostics resolved**

---

### Diagnostic-to-Slice Mapping (inventory-item.precept)

Every live diagnostic traces to exactly one slice:

| Line(s) | Diagnostic | Root Cause | Slice | Status |
|----------|-----------|------------|-------|--------|
| 122 | PRE0114 Unit mismatch on compound-unit rule | RC1: compound qualifier construction | **G1** | ✅ Fixed by G1 (`cb4fbf57`) |
| 123 | PRE0114 Unit mismatch on compound-unit rule | RC1: compound qualifier construction | **G1** | ✅ Fixed by G1 (`cb4fbf57`) |
| 137 | PRE0083 DivisionByZero (StockingUnitsPerSaleUnit) | Cascade from RC1 (rule can't prove > 0) | **G1** | ✅ Fixed by G1 (`cb4fbf57`) |
| 142 | PRE0083 DivisionByZero (StockingUnitsPerSaleUnit) | Cascade from RC1 (rule can't prove > 0) | **G1** | ✅ Fixed by G1 (`cb4fbf57`) |
| 215–219 | PRE0114 Currency `<unresolved>` (×2 per row) | RC2: Rate has no qualifier | **BUG-C** | ⏳ Blocked |
| 215–219 | PRE0114 FromCurrency↔Currency chain failure | RC2: Rate has no FromCurrency | **BUG-C** | ⏳ Blocked |
| 218 | PRE0083 DivisionByZero (compound denominator) | RC3: algebraic proof gap | **G2** | ⏳ Blocked on G1+BUG-C |
| 221–225 | PRE0114 Currency `<unresolved>` (×2 per row) | RC2: Rate has no qualifier | **BUG-C** | ⏳ Blocked |
| 221–225 | PRE0114 FromCurrency↔Currency chain failure | RC2: Rate has no FromCurrency | **BUG-C** | ⏳ Blocked |
| 224 | PRE0083 DivisionByZero (compound denominator) | RC3: algebraic proof gap | **G2** | ⏳ Blocked on G1+BUG-C |
| 226–230 | PRE0114 Currency `<unresolved>` (×2 per row) | RC2: Rate has no qualifier | **BUG-C** | ⏳ Blocked |
| 226–230 | PRE0114 FromCurrency↔Currency chain failure | RC2: Rate has no FromCurrency | **BUG-C** | ⏳ Blocked |
| 229 | PRE0083 DivisionByZero (compound denominator) | RC3: algebraic proof gap | **G2** | ⏳ Blocked on G1+BUG-C |

### Part G Dependency Graph

```
G1 (compound-unit qualifier)     — no dependencies, immediately actionable
  ↓ clears L122-123 (PRE0114) + L137,142 (PRE0083 cascade)
  ↓ prerequisite for G2

BUG-C (event arg interpolated qualifiers) — external dependency
  ↓ clears all ReceiveShipment PRE0114 (Currency + FromCurrency↔Currency)
  ↓ prerequisite for G2

G2 (algebraic DivisionByZero)    — depends on G1 + BUG-C
  ↓ clears L218, L224, L229 (PRE0083)
```

**After G1 alone:** 4 diagnostics resolved, ~12–15 remaining (all BUG-C blocked).
**After G1 + BUG-C:** ~12 diagnostics resolved, 3 remaining (all G2).
**After G1 + BUG-C + G2:** All diagnostics resolved. inventory-item.precept compiles clean.

---

## Part H — BUG-C: Interpolated Qualifiers on Event Args

**Added:** 2026-05-12
**Architect:** Frank (exhaustive syntax/type-checker/proof-engine audit, 2026-05-12)
**Unblocks:** F4 (ExchangeRateTimesMoney result qualifier), G2 (compound DivisionByZero), F5 (verification pass)
**Hero sample:** `samples/inventory-item.precept` — line 156: `Rate as exchangerate` (bare, no qualifier)

### The Gap

BUG-C was originally described as "event args cannot carry interpolated qualifier annotations." This is the language gap that prevents the proof engine from knowing what currencies an exchange rate converts between — without that metadata, the `CurrencyConversion` result qualifier policy cannot resolve the result's currency, and the `FromCurrency↔Currency` chain proof cannot verify dimensional correctness.

The concrete broken case in `inventory-item.precept`:

```precept
# Current (broken — no qualifier metadata):
event ReceiveShipment(
    PurchaseQty as quantity of '{PurchaseUnit.dimension}' default '0 {PurchaseUnit}',
    SupplierUnitCost as price in '{SupplierCurrency}' of '{StockingUnit.dimension}',
    Rate as exchangerate)  # ← BUG-C: bare exchangerate

# Desired (with qualifier metadata):
event ReceiveShipment(
    PurchaseQty as quantity of '{PurchaseUnit.dimension}' default '0 {PurchaseUnit}',
    SupplierUnitCost as price in '{SupplierCurrency}' of '{StockingUnit.dimension}',
    Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}')
```

Without qualifiers on Rate, the proof engine returns null for `ToCurrency` and `FromCurrency`, cascading into ~12 PRE0114 diagnostics across all ReceiveShipment transitions (lines 215–229).

### Impact Analysis: What BUG-C Blocks

| Slice | How BUG-C Blocks It |
|-------|-------------------|
| F4 (ExchangeRateTimesMoney policy) | Policy exists and works — but Rate has no `ToCurrency` → `CurrencyConversion` handler returns null → result Currency `<unresolved>` → assignment fails |
| G2 (DivisionByZero algebraic proof) | G2's diagnostics are co-located with RC2's qualifier failures; G2 only becomes last-standing after BUG-C clears RC2 |
| F5 (verification pass) | Cannot verify inventory-item compiles clean until all RC2 diagnostics clear |

---

### Critical Finding: Syntax Already Implemented

**The interpolated qualifier syntax on event args already works.** Exhaustive audit (2026-05-12) confirmed:

1. **Parser** — `ParseTypeReference()` calls `TryParseQualifiers()` (Parser.cs:622), which iterates the type's `QualifierShape.Slots` and handles both `TypedConstant` (literal) and `TypedConstantStart` (interpolated) tokens. This runs INSIDE `ParseArgumentList()` at line 788 via `ParseTypeReference(asToken.Span)` — event arg qualifiers are parsed identically to field qualifiers.

2. **Type Checker** — `PopulateEvents()` (TypeChecker.cs:462) calls `ExtractQualifiers(arg.Type, ctx)` at line 481, which dispatches to `MapInterpolatedQualifier()` for interpolated qualifiers. This produces `DeclaredQualifierMeta.FromCurrency` and `DeclaredQualifierMeta.ToCurrency` with correct `SourceFieldName` (e.g., `"SupplierCurrency"`, `"CatalogCurrency"`). The same code path used for field qualifiers — no event-arg-specific logic needed.

3. **Proof Engine — simple cases** — `ResolveQualifierFromExpression()` (ProofEngine.cs:1280) handles `TypedArgRef` with `DeclaredQualifiers` correctly. The `CurrencyConversionRequired` handler (ProofEngine.cs:1327) resolves result Currency by reading the exchange rate's `ToCurrency`. Direct assignment `set Cost = Rate * Amt` compiles clean.

**Verification:** The following precept compiles with 0 diagnostics:
```precept
precept TestExchangeRate
field SupplierCurrency as currency default 'USD'
field CatalogCurrency as currency default 'EUR'
field Cost as money in '{CatalogCurrency}' default '0.00 {CatalogCurrency}'
state Draft initial
state Done terminal
event Convert(Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}',
              Amount as money in '{SupplierCurrency}')
from Draft on Convert
    -> set Cost = Convert.Rate * Convert.Amount
    -> transition Done
```

The syntax, parsing, type checking, and basic proof resolution are all operational. **BUG-C is not a parser or type-checker feature gap — it is a sample update plus a residual proof engine bug.**

---

### Residual Bug: CurrencyConversion Qualifier Resolution in Nested Binary Expressions

While direct assignment from a currency-converted expression works, the **accumulation pattern** — the actual pattern used in `inventory-item.precept` — fails:

```precept
# FAILS with PRE0114: "Operands 'Total' and '<expression>' have incompatible Currency qualifiers"
-> set Total = Total + (Rate * Amt)

# WORKS: splitting across two set actions
-> set LocalCost = Rate * Amt    # CurrencyConversion resolves correctly
-> set Total = Total + LocalCost  # SameQualifier compares two Currency metas
```

**Root cause analysis:**

The expression `Total + (Rate * Amt)` generates a `QualifierCompatibilityProofRequirement(Currency)` from `MoneyPlusMoney`'s `SameQualifierRequired` binding. The proof engine resolves:

- **Left operand** (`Total`): `ResolveQualifierFromExpression` → `ResolveFieldQualifier` → returns `Currency("{CatalogCurrency}", SourceFieldName: "CatalogCurrency")`
- **Right operand** (`Rate * Amt`): `ResolveQualifierFromExpression` → `CurrencyConversionRequired` handler → resolves `QualifierAxis.ToCurrency` from Rate arg → returns `ToCurrency("{CatalogCurrency}", SourceFieldName: "CatalogCurrency")`

The comparison receives a `Currency` meta on the left and a `ToCurrency` meta on the right. `QualifiersSymbolicallyEqual` should compare `SourceFieldName` values ("CatalogCurrency" == "CatalogCurrency" → true). However, the proof fails empirically. The exact failure point requires live debugging — the code analysis shows the comparison SHOULD succeed, which means there's a subtle issue in the resolution or comparison path that static analysis alone cannot pinpoint.

**This bug is pre-existing** — it also affects literal qualifiers (`in 'EUR' to 'USD'`), confirming it is not specific to interpolated qualifiers on event args. It is a general proof engine limitation with `CurrencyConversionRequired` result qualifiers when nested inside a parent `SameQualifierRequired` binary op.

**Note:** The `ValidateAssignmentQualifiers` type-checker path (TypeChecker.Expressions.TypedConstants.cs:72) handles `CurrencyConversionRequired` correctly by explicitly creating a `Currency` meta from the `ToCurrency` value (line 86). The proof engine's `ResolveQualifierFromExpression` does NOT perform this axis translation — it returns the raw `ToCurrency` meta. This axis mismatch is the most likely root cause.

---

### A. Syntax Decision

**Chosen syntax:** `Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}'`

This is **not a new syntax** — it is the existing qualifier annotation syntax applied to event args, which the parser already supports. The decision is to confirm and document that this syntax is correct and operational.

**Rationale:**
- Mirrors the existing field qualifier syntax: `field X as exchangerate in '{A}' to '{B}'` → `arg X as exchangerate in '{A}' to '{B}'`
- Uses the same preposition tokens (`in`, `to`) driven by the `QualifierShape` metadata in `Types.cs` — `QS_ExchangeRate = [(In, FromCurrency), (To, ToCurrency)]`
- Interpolated expressions (`'{FieldName}'`) reference precept fields whose runtime values supply the qualifier binding — consistent with how field qualifiers work (Part A, Slice 2)
- No grammar changes required — the existing `TryParseQualifiers` method handles all qualifier-bearing types uniformly

**Alternatives rejected:**

1. **Explicit `.from`/`.to` accessor ensures as qualifier source** — `ensure Rate.from == SupplierCurrency` as a compile-time qualifier. Rejected: ensures are runtime guards, not qualifier metadata. Teaching the proof engine to infer qualifiers from ensures would be fragile, non-general, and architecturally wrong. The philosophy doc says "make invalid configurations structurally impossible" — qualifier declarations must be static metadata.

2. **New `qualified` keyword** — `Rate as exchangerate qualified(from: '{SupplierCurrency}', to: '{CatalogCurrency}')`. Rejected: introduces unnecessary syntax divergence from field qualifiers. The preposition-based qualifier syntax (`in ... to ...`) is already established and parsed. No justification for a second syntax.

3. **Bare field reference without interpolation braces** — `Rate as exchangerate in SupplierCurrency to CatalogCurrency`. Rejected: ambiguity with literal qualifier values (is `USD` a field name or a currency code?). The `'{...}'` interpolation syntax makes the field reference explicit and is consistent with typed constant interpolation (Part A).

---

### B. Grammar / Parser Impact

**No changes required.**

- `ParseArgumentList()` (Parser.cs:772) already calls `ParseTypeReference()` at line 788
- `ParseTypeReference()` calls `TryParseQualifiers()` at line 467 for non-collection, non-choice types
- `TryParseQualifiers()` iterates `typeMeta.QualifierShape.Slots`, consuming `in`/`to`/`of` prepositions and their typed constant values (literal or interpolated)
- The `exchangerate` type's `QualifierShape` (`QS_ExchangeRate` in Types.cs:46) defines `[(In, FromCurrency), (To, ToCurrency)]` — both slots are handled by the existing parser loop
- Interpolated values (`'{...}'`) are tokenized as `TypedConstantStart` by the lexer, triggering `ParseInterpolatedTypedConstant()` at Parser.cs:650
- The resulting `QualifiedTypeReference` wrapping `SimpleTypeReference(exchangerate)` with `InterpolatedParsedQualifier` nodes is preserved in `ArgumentSyntax.Type`

**AST nodes:** No new nodes. `ArgumentSyntax` already carries `ParsedTypeReference Type` which can be `QualifiedTypeReference`. No AST shape change.

**Token disambiguation:** `in` and `to` serve as qualifier prepositions within `TryParseQualifiers` (consumed before the modifier loop in `ParseArgumentList` at line 791). The modifier loop checks `Modifiers.ByValueToken` which does NOT contain `in`/`to` as value modifiers (they are anchor modifiers, applicable only in `ModifierList` slots on `ensure`/`in`/`to`/`from` constructs). No disambiguation issue.

---

### C. Type Checker Impact

**No changes required.**

- `PopulateEvents()` (TypeChecker.cs:462) already calls `ExtractQualifiers(arg.Type, ctx)` at line 481
- `ExtractQualifiers()` handles `QualifiedTypeReference` with both `LiteralParsedQualifier` and `InterpolatedParsedQualifier`
- `MapInterpolatedQualifier()` (TypeChecker.cs:154) resolves the interpolated expression against the expected type (`Currency` for `FromCurrency`/`ToCurrency` axes), extracts the `SourceFieldName`, and produces the correct `DeclaredQualifierMeta` subtype
- The resulting `TypedArg.DeclaredQualifiers` carries `[FromCurrency("{SupplierCurrency}", SourceFieldName: "SupplierCurrency"), ToCurrency("{CatalogCurrency}", SourceFieldName: "CatalogCurrency")]`
- `TypedArgRef` (SemanticIndex.cs:29) already carries `ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers` — no shape change needed

**Diagnostics:** No new diagnostic codes needed. Existing diagnostics cover all validation:
- `ExpectedToken` if qualifier value is missing after preposition
- `TypeMismatch` if interpolated expression resolves to wrong type (e.g., `'{StringField}'` where `currency` expected)
- `UnprovedQualifierCompatibility` (PRE0114) if qualifier chain fails at proof time

---

### D. Proof Engine Impact

**One fix required: CurrencyConversion qualifier axis translation in `ResolveQualifierFromExpression`.**

The proof engine's `ResolveQualifierFromExpression` (ProofEngine.cs:1327) handles `CurrencyConversionRequired` by resolving the exchange rate's `ToCurrency` qualifier. However, it returns the raw `ToCurrency` meta (axis=ToCurrency) when the caller asked for `Currency` axis. This works for assignment validation (the type checker's `ValidateAssignmentQualifiers` handles it separately at TypeChecker.Expressions.TypedConstants.cs:72-93), but fails for proof engine qualifier compatibility checks where `Currency` and `ToCurrency` are compared as different-axis qualifiers.

**The fix — axis translation:**

In `ResolveQualifierFromExpression` (ProofEngine.cs:1327-1333), after resolving the ToCurrency from the exchange rate, translate it to a Currency meta:

```csharp
// BEFORE (returns ToCurrency meta — wrong axis for Currency comparisons):
CurrencyConversionRequired =>
    axis == QualifierAxis.Currency
        ? ResolveQualifierFromExpression(
            binOp.Left.ResultType == TypeKind.ExchangeRate ? binOp.Left : binOp.Right,
            QualifierAxis.ToCurrency, semantics)
        : null,

// AFTER (translate ToCurrency → Currency for result axis consistency):
CurrencyConversionRequired =>
    axis == QualifierAxis.Currency
        ? TranslateToCurrencyResult(
            ResolveQualifierFromExpression(
                binOp.Left.ResultType == TypeKind.ExchangeRate ? binOp.Left : binOp.Right,
                QualifierAxis.ToCurrency, semantics))
        : null,
```

Where `TranslateToCurrencyResult` converts a `ToCurrency` meta to a `Currency` meta preserving the value and `SourceFieldName`:

```csharp
private static DeclaredQualifierMeta? TranslateToCurrencyResult(DeclaredQualifierMeta? resolved)
{
    if (resolved is DeclaredQualifierMeta.ToCurrency tc)
        return new DeclaredQualifierMeta.Currency(tc.CurrencyCode,
            Origin: QualifierOrigin.Derived,
            SourceFieldName: tc.SourceFieldName);
    return resolved;
}
```

**Also apply the same translation in `ResolveQualifierOnAxis`** (ProofEngine.cs:1194-1202) which has the identical `CurrencyConversionRequired` handler pattern.

**Interaction with `QualifierChainProofRequirement`:** The `ExchangeRateTimesMoney` operation also generates a `QualifierChainProofRequirement(FromCurrency, Currency)` checking `Rate.FromCurrency == Money.Currency`. This uses `ResolveQualifierOnAxis` which resolves subjects on DIFFERENT axes (left=FromCurrency, right=Currency) and compares via `ChainQualifiersMatch` → `ExtractComparableValue`. The `ExtractComparableValue` function (ProofEngine.cs:1106) already handles `FromCurrency` and `ToCurrency` subtypes correctly. No change needed for chain proofs.

**Interaction with `ExtractQualifierSourcePath`:** The `ExtractQualifierSourcePath` function (ProofEngine.cs:1080) only handles `Currency`, `Unit`, `Dimension`, and `TemporalUnit` subtypes — `FromCurrency` and `ToCurrency` fall through to `_ => null`. This is a secondary gap (affects literal-qualifier fallback comparison only, not interpolated qualifiers which use `SourceFieldName`). The fix should add:

```csharp
DeclaredQualifierMeta.FromCurrency { CurrencyCode: var value } => value,
DeclaredQualifierMeta.ToCurrency { CurrencyCode: var value } => value,
```

---

### E. Runtime / Evaluator Impact

**No changes required.**

At event-fire time, the caller provides a value for `Rate`. The runtime already validates that the provided value is of the correct type (`exchangerate`). Qualifier annotations are compile-time metadata — they constrain the proof engine's static analysis. At runtime, the exchange rate value carries its own `from`/`to` currencies intrinsically (accessible via `.from` and `.to` accessors). The qualifier annotation tells the COMPILER which currencies to expect — the runtime value already contains them.

The existing `ensure Rate.from == SupplierCurrency` and `ensure Rate.to == CatalogCurrency` runtime guards (inventory-item.precept lines 159–162) remain valid as defense-in-depth but become redundant once the qualifier annotation makes these constraints provable at compile time. The sample should mark them as optional or remove them.

---

### F. Tooling Impact

**Syntax highlighting:** No changes. The TextMate grammar is generated from catalog metadata. The qualifier prepositions (`in`, `to`) and interpolation braces (`{`, `}`) are already covered by the grammar rules for qualifier annotations and typed constant interpolation.

**Completions:** No changes. Completions inside `'{...}'` holes in qualifier position already suggest field names of the appropriate type (currency fields for `FromCurrency`/`ToCurrency` axes). This is driven by the existing completion infrastructure for interpolated typed constants (Part A, Slice 3).

**Hover:** The language server's hover handler already displays qualifier information from `TypedArg.DeclaredQualifiers`. Once the sample file adds qualifiers to Rate, hover on `Rate` in event args will show `FromCurrency: {SupplierCurrency}, ToCurrency: {CatalogCurrency}`.

**MCP:** The `precept_compile` tool's `TypedArg` DTO already includes qualifier information (inherited from the core `TypedArg` record). No DTO changes needed. Existing compile output will show correct qualifier metadata once the sample is updated.

---

### G. Scope — What BUG-C Does NOT Cover

1. **New parser features** — No grammar changes, no new tokens, no new AST nodes. The syntax is already implemented.
2. **Type checker changes** — No new type-checking logic. `ExtractQualifiers` already handles event arg qualifiers identically to field qualifiers.
3. **New diagnostic codes** — No new codes. Existing codes cover all validation scenarios.
4. **Runtime validation changes** — Qualifier annotations are compile-time metadata. No runtime API changes.
5. **Tooling changes** — No grammar generator, completion, hover, or MCP changes.
6. **Other sample files** — Only `inventory-item.precept` is affected. Other samples that use event arg qualifiers (e.g., `PurchaseQty as quantity of '{PurchaseUnit.dimension}'`) already work correctly.
7. **The G2 algebraic DivisionByZero proof** — G2 is a separate proof engine enhancement for compound expression non-zero reasoning. BUG-C unblocks G2 by clearing RC2 qualifier failures that mask RC3 DivisionByZero diagnostics, but G2's implementation is independent.

---

### Implementation Slices

#### H1 — Proof Engine CurrencyConversion Axis Translation

**Type:** Bug fix (proof engine)
**Status:** ✅ Done
**Diagnostics cleared:** Proof-side currency-axis mismatches for the ReceiveShipment accumulation shape; together with H2, the sample's PRE0114 diagnostics on lines 212, 214, 218, 220, 223, and 225 are gone
**Effort:** Small
**Dependencies:** None
**Owner:** George

##### Root Cause

`ResolveQualifierFromExpression` in ProofEngine.cs (line 1327) handles `CurrencyConversionRequired` by resolving the exchange rate operand's `ToCurrency` qualifier. It returns the raw `ToCurrency` meta (axis=ToCurrency) when the caller requested `Currency` axis. This creates a cross-axis comparison (`Currency` vs `ToCurrency`) that `QualifiersAreCompatible` fails to match via record equality or structural comparison, even when both qualifiers reference the same source field.

The type checker's `ValidateAssignmentQualifiers` (TypeChecker.Expressions.TypedConstants.cs:72-93) correctly handles this by creating a new `Currency` meta from the `ToCurrency` value. The proof engine lacks this translation.

##### Fix

**File:** `src/Precept/Pipeline/ProofEngine.cs`

**Implemented changes:**

1. `ResolveQualifierFromExpression` now translates a resolved `ToCurrency` qualifier into a `Currency` qualifier when the caller requested the Currency axis, preserving proof satisfactions and `SourceFieldName` so nested binary-op comparisons stay on the same axis.
2. `ExtractQualifierSourcePath` now handles `FromCurrency` and `ToCurrency`, so legacy literal fallback comparison works for exchange-rate qualifiers too.

##### Test Cases

| Test | Input | Expected |
|------|-------|----------|
| `CurrencyConversion_DirectAssignment_StillWorks` | `set Cost = Rate * Amt` where Rate is `exchangerate in '{A}' to '{B}'`, Cost is `money in '{B}'` | 0 PRE0114 (regression guard) |
| `CurrencyConversion_Accumulation_QualifierResolved` | `set Total = Total + Rate * Amt` where both Total and Rate.ToCurrency reference `{CatalogCurrency}` | 0 PRE0114 (the key fix) |
| `CurrencyConversion_LiteralQualifier_Accumulation` | `set Total = Total + Rate * Amt` with literal `in 'EUR' to 'USD'`, Total `in 'USD'` | 0 PRE0114 |
| `CurrencyConversion_Mismatch_StillDetected` | `set Total = Total + Rate * Amt` where Total is `in '{CurrA}'` but Rate is `to '{CurrB}'` (different fields) | PRE0114 fires (correct: currencies don't match) |
| `CurrencyConversion_NestedCompound_InventoryPattern` | Full inventory-item accumulation: `Total + Rate * (UnitCost * (Qty * ConversionFactor))` | 0 PRE0114 (verifies deep nesting) |

**Files changed:** `src/Precept/Pipeline/ProofEngine.cs` (~20 LOC)
**Test file:** `test/Precept.Tests/ProofEngineTests.cs` (~60 LOC)
**Regression risk:** Low. The axis translation only affects `CurrencyConversionRequired` results on the `Currency` axis. All existing tests that pass through this handler use direct assignment (validated by the type checker), not nested binary ops (validated by the proof engine). The translation is additive — it converts `ToCurrency` → `Currency` which is strictly more comparable.

---

#### H2 — Sample Update: inventory-item.precept Rate Qualifier

**Type:** Sample fix
**Status:** ✅ Done
**Diagnostics cleared:** The ReceiveShipment PRE0114 sites on lines 212, 214, 218, 220, 223, and 225
**Effort:** Trivial
**Dependencies:** H1
**Owner:** George

##### Change

**File:** `samples/inventory-item.precept` — line 156

**Before:**
```precept
    Rate as exchangerate)
```

**After:**
```precept
    Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}')
```

##### Secondary Changes

1. **Removed redundant runtime ensures** (former lines 159–162): the `Rate.from` / `Rate.to` checks are now carried by the event-arg qualifier itself.
2. **Scoped sample edit only**: this pass intentionally left the rest of `inventory-item.precept` untouched.

##### Test

Compiling the updated sample with the current runtime leaves only the three ReceiveShipment denominator PRE0083 diagnostics on lines 214, 220, and 225. The qualifier-related PRE0114 diagnostics are gone.

---

#### H3 — Verification: Recompile inventory-item After H1 + H2

**Type:** Verification
**Status:** ✅ Done
**Dependencies:** H1 + H2
**Owner:** George

`samples/inventory-item.precept` now recompiles with the qualifier class cleared. The remaining diagnostics are only:

- PRE0083 at line 214
- PRE0083 at line 220
- PRE0083 at line 225

Those are the existing ReceiveShipment denominator proofs tracked by G2.

---

### Part H Dependency Graph

```
H1 (proof engine axis translation) — no dependencies, immediately actionable
  ↓ fixes CurrencyConversion qualifier resolution in nested binary ops
  ↓ prerequisite for H2 (sample update would fail without proof fix)

H2 (sample update: Rate qualifier)  — depends on H1
  ↓ adds in '{SupplierCurrency}' to '{CatalogCurrency}' to Rate arg
  ↓ clears all RC2 diagnostics (Currency <unresolved> + FromCurrency↔Currency chain)

H3 (verification)                   — depends on H1 + H2
  ↓ confirms diagnostic clearance on inventory-item.precept

G2 (algebraic DivisionByZero)       — depends on G1 + H1 + H2 (replaces G1 + BUG-C)
  ↓ clears RC3 diagnostics (3 × PRE0083)
```

**After H1 alone:** The proof engine fix is generic — any nested `exchangerate × money` accumulation now resolves onto the Currency axis correctly.
**After H1 + H2:** The sample's ReceiveShipment qualifier failures are gone; only RC3 remains (3 × PRE0083 at lines 214/220/225).
**After H1 + H2 + G1 + G2:** All diagnostics on inventory-item.precept resolved. The sample compiles clean.

**Completed execution order:** H1 → H2 → H3.
