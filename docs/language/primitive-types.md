# Primitive Types

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Designed — type checker implementation pending |
| Scope | Six primitive types: `string`, `integer`, `decimal`, `number`, `boolean`, `choice`; numeric lanes, conversion rules, built-in functions |
| Related | [Temporal Type System](temporal-type-system.md) · [Business-Domain Types](business-domain-types.md) · [Literal System](../compiler/literal-system.md) |

---

## Overview

Precept has six primitive types. Each has a fixed backing type, a defined operator surface, and explicit constraints. There is no implicit coercion between non-numeric types, no truthy/falsy conversion, and no user-defined types.

| Type | Backing | Purpose | Example |
|------|---------|---------|---------|
| `string` | UTF-16 text | Names, descriptions, identifiers | `"hello"` |
| `integer` | exact whole number | Counts, indices, discrete quantities | `42` |
| `decimal` | exact base-10 fractional | Financial, rate, tax — exactness required | `3.14` |
| `number` | IEEE 754 double | Scientific, scoring — approximation acceptable | `1.5e2` |
| `boolean` | true/false | Guards, conditions, flags | `true` |
| `choice` | finite string set | Enumerations with optional ordering | `"draft"` |

---

## `string`

**Declaration:**

```precept
field Name as string
field Email as string notempty
field Code as string minlength 3 maxlength 10
field Notes as string optional
```

**Operators:**

| Expression | Result | Notes |
|---|---|---|
| `string + string` | `string` | Concatenation. Both operands must be `string` — no implicit coercion from other types. |
| `string == string` | `boolean` | Ordinal, case-sensitive. |
| `string != string` | `boolean` | Ordinal, case-sensitive. |
| `string ~= string` | `boolean` | Ordinal, case-insensitive (`OrdinalIgnoreCase`). |
| `string !~ string` | `boolean` | Ordinal, case-insensitive not-equals. |

Relational comparison (`<`, `>`, `<=`, `>=`) is not available on `string`. Arithmetic (`-`, `*`, `/`, `%`) is a type error. Logical operators are a type error.

**Member access:**

| Member | Returns | Notes |
|---|---|---|
| `.length` | `integer` | Code-unit count. Requires presence guard (`is set`) for optional fields. |

**Constraints:** `notempty`, `minlength N`, `maxlength N`, `optional`, `default "..."`.

**String interpolation:** `"Hello, {Name}"` — each `{expr}` is type-checked independently. Any scalar type is coercible to string inside `"..."` interpolation. Collections are a type error.

**`~string` — case-insensitive collection inner type:**

`~string` is not a standalone field type — it is only valid as the inner type of a collection:

```precept
field Tags   as set of string    # ordinal — "Apple" ≠ "apple", both can coexist
field Labels as set of ~string   # OrdinalIgnoreCase — "Apple" and "apple" are the same element
```

The `~` prefix selects `StringComparer.OrdinalIgnoreCase` as the collection's comparer, which governs membership, deduplication, and ordering consistently. `set of ~string` supports `.min`/`.max` (OrdinalIgnoreCase ordering is deterministic). `field Name as ~string` is a type error (`CaseInsensitiveStringOnNonCollection`).

---

## `integer`

**Declaration:**

```precept
field Count as integer
field Quantity as integer nonnegative default 0
field Priority as integer min 1 max 10
```

**Backing:** Exact whole number. No fractional part. Overflow is a type error.

**Operators:**

| Expression | Result | Notes |
|---|---|---|
| `integer + integer` | `integer` | Stays in lane. Checked overflow. |
| `integer - integer` | `integer` | |
| `integer * integer` | `integer` | |
| `integer / integer` | `integer` | Integer division (truncates). Divisor safety applies. |
| `integer % integer` | `integer` | Remainder. |
| `-integer` | `integer` | Negation. |
| `integer == integer` | `boolean` | |
| `integer < integer` | `boolean` | Orderable. |

**Widening:** `integer` implicitly widens to `decimal` (lossless) and `number` (lossless within safe integer range). See [Numeric Lane Rules](#numeric-lane-rules).

**Constraints:** `nonnegative`, `positive`, `nonzero`, `min N`, `max N`, `optional`, `default N`.

**Surfaces that produce `integer`:** `.count` (collections), `.length` (string), `floor()`, `ceil()`, `truncate()`, `round()` (no places).

---

## `decimal`

**Declaration:**

```precept
field Price as decimal nonnegative
field TaxRate as decimal min 0 max 1 maxplaces 4
field Balance as decimal default 0
```

**Backing:** Exact base-10 fractional representation. No floating-point artifacts. `0.1 + 0.2 == 0.3` is true.

**Operators:**

| Expression | Result | Notes |
|---|---|---|
| `decimal + decimal` | `decimal` | Stays in lane. Overflow is an error. |
| `decimal - decimal` | `decimal` | |
| `decimal * decimal` | `decimal` | |
| `decimal / decimal` | `decimal` | Full 28-digit precision. Divisor safety applies. |
| `decimal % decimal` | `decimal` | |
| `-decimal` | `decimal` | Negation. |
| `decimal == decimal` | `boolean` | Exact comparison. |
| `decimal < decimal` | `boolean` | Orderable. |

**Widening:** `decimal` does **not** implicitly widen to `number`. See [Numeric Lane Rules](#numeric-lane-rules).

**Constraints:** `nonnegative`, `positive`, `nonzero`, `min N`, `max N`, `maxplaces N`, `optional`, `default N`.

**`maxplaces`:** Validation constraint, not auto-rounding. `field X as decimal maxplaces 2` — assigning `1.999` is a constraint violation. Only applicable to `decimal`.

**Business-domain scalar role:** All seven business-domain types (`money`, `quantity`, `price`, `exchangerate`, etc.) use `decimal` as their magnitude backing. `decimal` is the scalar operand for business-domain arithmetic — `money * decimal → money`. See [Business-Domain Types](business-domain-types.md) for the D12 contract.

---

## `number`

**Declaration:**

```precept
field Score as number
field Latitude as number min -90 max 90
field Coefficient as number default 1.0
```

**Backing:** IEEE 754 double-precision floating point. Approximate — `0.1 + 0.2 ≠ 0.3`.

**Operators:**

| Expression | Result | Notes |
|---|---|---|
| `number + number` | `number` | Stays in lane. |
| `number - number` | `number` | |
| `number * number` | `number` | |
| `number / number` | `number` | Divisor safety applies. |
| `number % number` | `number` | |
| `-number` | `number` | Negation. |
| `number == number` | `boolean` | IEEE 754 comparison. |
| `number < number` | `boolean` | Orderable. |

**Widening:** `number` is the terminal lane. Nothing widens to or from `number` implicitly (except `integer → number`). See [Numeric Lane Rules](#numeric-lane-rules).

**Constraints:** `nonnegative`, `positive`, `nonzero`, `min N`, `max N`, `optional`, `default N`.

**Number-lane-only functions:** `sqrt()` lives exclusively in the `number` lane. Authors with `decimal` values reach it via `sqrt(approximate(value))`. Future functions (`log`, `sin`, `cos`, `exp`) will also be number-lane-only.

**Not a scalar for business-domain types.** `number` is rejected as a scalar operand for `money`, `quantity`, `price`, and `exchangerate` — the type checker emits a teachable diagnostic. See the D12 widening ceiling in [Business-Domain Types](business-domain-types.md).

---

## `boolean`

**Declaration:**

```precept
field IsActive as boolean default true
field HasApproval as boolean optional
```

**Backing:** `true` or `false`. No truthy/falsy coercion — `0`, `""`, and `null` are not boolean.

**Operators:**

| Expression | Result | Notes |
|---|---|---|
| `boolean and boolean` | `boolean` | Short-circuit. |
| `boolean or boolean` | `boolean` | Short-circuit. |
| `not boolean` | `boolean` | |
| `boolean == boolean` | `boolean` | |
| `boolean != boolean` | `boolean` | |

Relational comparison (`<`, `>`, `<=`, `>=`) is a type error. Arithmetic is a type error.

**Constraints:** `optional`, `default true/false`.

**Role in the language:** Every `when`, `if`, `rule`, and `ensure` expression must be `boolean`-typed. No implicit coercion from other types.

---

## `choice`

**Declaration:**

```precept
field Status as choice of string("draft", "active", "closed") default "draft"
field Priority as choice of string("low", "medium", "high") ordered default "low"
```

**Backing:** String value constrained to a declared finite set. Stored and serialized as the string value.

**Variants:**

| Variant | Declared with | Comparison | Use case |
|---|---|---|---|
| Unordered | `choice of T(...)` | `==`, `!=` only | Status, category — no ranking |
| Ordered | `choice of T(...) ordered` | `==`, `!=`, `<`, `<=`, `>`, `>=` | Priority, severity — rank by declaration order |

**Operators:**

| Expression | Result | Notes |
|---|---|---|
| `choice == choice` | `boolean` | Ordinal, case-sensitive. |
| `choice != choice` | `boolean` | |
| `choice < choice` (ordered only) | `boolean` | Declaration-position rank. First value is lowest. |

Arithmetic is a type error. Logical operators are a type error.

**Ordinal rank is field-local.** Comparison is valid only between a choice field and a literal from its own declared set. Comparing two choice fields — even with the same member set — is a compile-time error.

**Literal validation:** Every literal assigned to or compared against a choice field must be a member of that field's declared set. Non-member literals are compile-time errors, in both assignment and comparison positions.

**Constraints:** `ordered`, `optional`, `default "..."`.

---

## Numeric Lane Rules

> **This section is the single source of truth for numeric type conversions.** All other documents cross-reference here rather than restating the rules. When a conversion rule changes, update this section — other docs link, not duplicate.

### The three lanes

`integer`, `decimal`, and `number` are distinct semantic lanes, not interchangeable representations of "a number."

| Lane | Backing | Semantics | Use case |
|------|---------|-----------|----------|
| `integer` | exact whole number | Discrete counts, indices, positions | `.count`, `.length`, integer arithmetic |
| `decimal` | exact base-10 fractional | Financial, rate, tax — exactness required | Money, percentages, business arithmetic |
| `number` | IEEE 754 double | Approximate computation, scientific, scoring | Calculations where approximation is acceptable |

### Lane rules

1. **Homogeneous arithmetic stays in lane.** `integer + integer → integer`, `decimal * decimal → decimal`, `number + number → number`. No silent lane crossing.
2. **`integer` widens losslessly.** `integer` may widen to `decimal` (exact) or `number` (exact within safe integer range) implicitly in any context.
3. **`decimal` and `number` never mix implicitly.** Every operation that combines `decimal` and `number` — arithmetic, assignment, comparison, function arguments, default values — is a **type error**. The author must use an explicit bridge function. No exceptions.
4. **`integer`-shaped surfaces produce `integer`.** `.count`, `.length`, and rounding functions (`floor`, `ceil`, `truncate`, `round` with no places) always return `integer`.
5. **No implicit narrowing.** Moving toward a more restrictive lane (`number → decimal`, `number → integer`, `decimal → integer`) always requires an explicit bridge function.

### Complete conversion map

**Implicit (no bridge needed):**

| Direction | Mechanism | Why lossless |
|---|---|---|
| `integer → decimal` | Implicit widening | Every integer is exactly representable in base-10 |
| `integer → number` | Implicit widening (direct) | Every integer within safe range is exactly representable as IEEE 754 double |

**Explicit bridge required:**

| Direction | Bridge function | Output type | Why explicit |
|---|---|---|---|
| `decimal → number` | `approximate(value)` | `number` | Lossy — IEEE 754 can't exactly represent all base-10 values (`0.1 + 0.2 ≠ 0.3`) |
| `number → decimal` | `round(value, places)` | `decimal` | Lossy — must choose precision; IEEE 754 representation artifacts become visible |
| `number → integer` | `floor(value)` | `integer` | Truncation — must choose rounding mode |
| | `ceil(value)` | `integer` | |
| | `truncate(value)` | `integer` | |
| | `round(value)` | `integer` | Banker's rounding |
| `decimal → integer` | `floor(value)` | `integer` | Same four functions — fractional part is discarded |
| | `ceil(value)` | `integer` | |
| | `truncate(value)` | `integer` | |
| | `round(value)` | `integer` | Banker's rounding |

**Blocked (type error, no conversion path exists):**

| Expression | Why blocked |
|---|---|
| `decimal + number` | Implicit lane mixing. Use `approximate(decimalExpr) + numberExpr` or `decimalExpr + round(numberExpr, N)` |
| `decimal == number` | Cross-lane equality is semantically dangerous — IEEE 754 represents `0.1 + 0.2` as `0.30000000000000004`, so equality silently returns `false` against exact `0.3`. Bridge one operand first. |
| `decimal > number` | Same — bridge one operand to choose which lane the comparison lives in |
| `set numberField = decimalExpr` | Lossy assignment. Use `set numberField = approximate(decimalExpr)` |
| `set decimalField = numberExpr` | Lossy assignment. Use `set decimalField = round(numberExpr, N)` |
| `number` as scalar for business-domain types | Business-domain operator tables only accept `decimal` scalars (D12). Change the field to `as decimal`. |

### Context-by-context matrix

| Context | `integer → decimal` | `integer → number` | `decimal → number` | `number → decimal` |
|---------|---------------------|---------------------|---------------------|---------------------|
| Assignment | ✓ implicit | ✓ implicit | ✗ requires `approximate()` | ✗ requires `round(v, N)` |
| Binary arithmetic | ✓ implicit | ✓ implicit | ✗ type error | ✗ type error |
| Comparison | ✓ implicit | ✓ implicit | ✗ type error | ✗ type error |
| Function arguments | ✓ implicit | ✓ implicit | ✗ requires `approximate()` | ✗ requires `round(v, N)` |
| Default values | ✓ implicit | ✓ implicit | ✗ requires `approximate()` | ✗ requires `round(v, N)` |

### Why no comparison exception?

Cross-lane equality (`decimal == number`) is semantically dangerous. IEEE 754 represents `0.1 + 0.2` as `0.30000000000000004`. Comparing that to exact `decimal` `0.3` returns `false` — the exact kind of silent wrongness the lane separation is designed to prevent. Ordering operators are marginally safer but still lossy at boundaries. Requiring a bridge forces the author to choose which lane the comparison lives in — consistent with the principle that **every lossy lane crossing is explicit**.

### Widening ceiling for business-domain types

The `integer → decimal` widening applies when scalars interact with `money`, `quantity`, `price`, and `exchangerate` operators — `Amount * 2` works because `integer` widens to `decimal` losslessly.

The `decimal → number` bridge does **not** apply. `number` is permanently excluded from business-domain operator tables (D12). `money * number`, `quantity * number`, `price * number`, and `exchangerate * number` are all type errors. The type checker emits a teachable diagnostic directing the author to change the scalar field to `as decimal`. See [Business-Domain Types](business-domain-types.md) § Backing Standards.

---

## Context-Sensitive Literal Resolution

Numeric literals do not carry an inherent lane. Context determines the type.

| Literal form | Valid target types | Resolution rule |
|---|---|---|
| Whole number (`42`) | `integer`, `decimal`, `number` | Context determines. If target is `integer`, resolves as `integer`. If `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. |
| Fractional (`3.14`) | `decimal`, `number` | If target is `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. Type error if target is `integer`. |
| Exponent (`1.5e2`) | `number` only | Always `number`. Type error if target is `integer` or `decimal`. |

**Context sources** (in priority order):

1. **Assignment target:** `set Price = 0.1` where `Price` is `decimal` → resolves as `decimal`.
2. **Binary operator peer:** `Price * 1.08` where `Price` is `decimal` → `1.08` resolves as `decimal`.
3. **Function argument position:** `min(Price, 100.00)` where `Price` is `decimal` → `100.00` resolves as `decimal`.
4. **Field constraint value:** `field Price as decimal min 0.01` → resolves as `decimal`.
5. **Default value:** `field Price as decimal default 0.0` → resolves as `decimal`.
6. **Comparison peer:** `Score > 50.0` where `Score` is `number` → `50.0` resolves as `number`.

**No literal suffix syntax.** No `0.1m`, `0.1d`, or similar markers. Context is the sole resolution mechanism.

**No implicit fallback.** If a numeric literal appears with no type context, the type checker emits a diagnostic and assigns `ErrorType`. Every fractional literal resolves to exactly one lane — no ambiguity. The non-ambiguity invariant guarantees exactly one resolution per literal per context.

**Business-domain co-operand rule:** When the co-operand or assignment target is a `decimal`-backed business type (`money`, `quantity`, `price`, `exchangerate`), the literal is born as `decimal`. When the context is `number`, the literal is born as `number`. See [Business-Domain Types](business-domain-types.md) § Scalar Literal Type Resolution.

---

## Type Operator Surface Summary

| Type | `==` `!=` | `~=` `!~` | `<` `>` `<=` `>=` | `+` `-` `*` `/` `%` | `and` `or` `not` | `.length` / `.count` |
|---|---|---|---|---|---|---|
| `string` | ✓ ordinal | ✓ ordinal ignore-case | ✗ | `+` only (concat) | ✗ | `.length → integer` |
| `integer` | ✓ | ✗ type error | ✓ | ✓ (stays integer) | ✗ | — |
| `decimal` | ✓ exact | ✗ type error | ✓ exact | ✓ (stays decimal) | ✗ | — |
| `number` | ✓ IEEE 754 | ✗ type error | ✓ IEEE 754 | ✓ (stays number) | ✗ | — |
| `boolean` | ✓ | ✗ type error | ✗ | ✗ | ✓ short-circuit | — |
| `choice` (unordered) | ✓ ordinal | ✗ type error | ✗ | ✗ | ✗ | — |
| `choice` (ordered) | ✓ ordinal | ✗ type error | ✓ rank | ✗ | ✗ | — |

**No truthy/falsy coercion.** `0` and `""` are not boolean. Conditions require `boolean`-typed expressions.

**No mixed-type string concatenation.** `string + number` is a type error. Use string interpolation: `"{Name}: {Count}"`.

---

## Case-Insensitive Comparison

Two operators provide case-insensitive string comparison using ordinal case folding (`StringComparison.OrdinalIgnoreCase` in .NET).

| Operator | Operand types | Result | Semantics |
|---|---|---|---|
| `~=` | `string` × `string` | `boolean` | Case-insensitive equals |
| `!~` | `string` × `string` | `boolean` | Case-insensitive not-equals |

**String-only.** Both operators are type errors on non-string operands. Case has no meaning outside strings.

**Ordinal, not culture-aware.** No locale rules, no Turkish-I problem, no surprises. The .NET runtime uses `StringComparison.OrdinalIgnoreCase` — the standard for programmatic case-insensitive comparison.

**Same precedence as `==` / `!=`.** The `~` prefix selects case-insensitive mode; the comparison semantics are otherwise identical.

**Negation alternatives.** `!~` and `not (x ~= y)` are equivalent. Authors may use whichever they find more readable. The `=` in `~=` disambiguates equality from the reserved `~` prefix — the same role `=` plays in `==` vs `=`. Once `!` is present, the context is unambiguously a test, so the trailing `=` is unnecessary — just as `!=` doesn't need `!==`. `not (x ~= y)` uses keyword negation with explicit parentheses — the parens are required because `not` binds tighter than comparison operators.

### Usage examples

```
// Guard — case-insensitive equality (the common case)
from Draft on Submit
  when Department ~= "Engineering"
  when Priority !~ "low"

// Equivalent using keyword negation
from Draft on Submit
  when Department ~= "Engineering"
  when not (Priority ~= "low")

// Conditional expression
set Tier = if Category ~= "premium" then "Gold" else "Standard"
```

### Design rationale

1. **Domain-expert ergonomics.** The primary `.precept` author is a business analyst. In business domains — names, emails, addresses, department names — case-insensitive comparison is the common case, not the exception. Making the common case require `toLower(x) == toLower(y)` forces the domain expert to reason about string transformations to express what they consider an obvious comparison.
2. **Explicitness over mechanism.** `~=` declares intent ("this comparison is case-insensitive") rather than mechanism (`toLower` + `==`). This parallels `approximate()` — the numeric bridge declares that a lossy crossing is happening rather than making the author perform the conversion manually.
3. **`!=` stays.** The `!=` operator was a deliberate, researched decision (see `research/language/expressiveness/conditional-logic-strategy.md`). Symbols for comparison, keywords for logic. `!~` follows the same pattern — `!` negates within the comparison family. The `=` in `~=` disambiguates equality from the reserved `~` prefix (just as `==` disambiguates from `=`); once `!` is present the context is unambiguously a test, so the trailing `=` drops — matching the `!=` / `!==` precedent.
4. **No cascade.** Only `~=` and `!~` ship. No `~<`, `~>`, `~startsWith`. Case-insensitive ordering is rare; `toLower()` covers it. The operator surface stays tight.
5. **Ordinal is the default.** `OrdinalIgnoreCase` is the .NET ecosystem standard for programmatic comparison. No ambiguity about which folding to use.
6. **No precedent for a dedicated CI operator exists** in any surveyed rule engine, validation framework, or expression language (see `research/language/expressiveness/case-insensitive-implementation-survey.md`). Precept adds one because its target audience is different — domain experts, not programmers — and the function-based idiom optimizes for the wrong author.

---

## Constraint Catalog

| Constraint | Applicable to | Meaning |
|---|---|---|
| `optional` | any field type | Field may be unset; requires `is set` guard before use |
| `default V` | any field type | Initial value when no event has set it |
| `nonnegative` | `integer`, `decimal`, `number` | Value ≥ 0 |
| `positive` | `integer`, `decimal`, `number` | Value > 0 (subsumes `nonnegative`) |
| `nonzero` | `integer`, `decimal`, `number` | Value ≠ 0 |
| `min N` | `integer`, `decimal`, `number` | Value ≥ N |
| `max N` | `integer`, `decimal`, `number` | Value ≤ N |
| `maxplaces N` | `decimal` only | At most N decimal places. Validation constraint, not auto-rounding. |
| `notempty` | `string` only | `.length > 0` |
| `minlength N` | `string` only | `.length ≥ N` |
| `maxlength N` | `string` only | `.length ≤ N` |
| `ordered` | `choice` only | Enables ordinal comparison by declaration position |

**Constraint validation rules:**

| Check | Diagnostic |
|---|---|
| `min` > `max` on same field | `InvalidModifierBounds` |
| `minlength` > `maxlength` | `InvalidModifierBounds` |
| Negative count/length/places | `InvalidModifierValue` |
| `maxplaces` not integer | `InvalidModifierValue` |
| Duplicate modifier | `DuplicateModifier` |
| `nonnegative` + `positive` | `RedundantModifier` (warning — `positive` subsumes) |
| Constraint on wrong type | `InvalidModifier` (e.g., `maxplaces` on `number`) |

---

## Built-in Functions (Primitive)

> Functions operating on temporal or business-domain types are documented in their respective type docs. This section covers functions applicable to primitive types only.

### Numeric functions

| Function | Signature | Return type | Notes |
|---|---|---|---|
| `min(a, b)` | `(numeric, numeric) → numeric` | Common numeric type | |
| `max(a, b)` | `(numeric, numeric) → numeric` | Common numeric type | |
| `abs(value)` | `(numeric) → numeric` | Same type as input | Type-preserving: `integer → integer`, `decimal → decimal`, `number → number` |
| `clamp(value, lo, hi)` | `(numeric, numeric, numeric) → numeric` | Common numeric type | |
| `pow(base, exp)` | `(numeric, integer) → numeric` | Same type as `base` | `exp` must be non-negative for integer lane |
| `sqrt(value)` | `(number) → number` | `number` | **Number-lane only.** `sqrt(decimal)` is a type error — use `sqrt(approximate(value))`. Proof engine checks non-negativity. |

### Rounding functions

| Function | Signature | Return type | Notes |
|---|---|---|---|
| `floor(value)` | `(decimal\|number) → integer` | `integer` | Round toward −∞ |
| `ceil(value)` | `(decimal\|number) → integer` | `integer` | Round toward +∞ |
| `truncate(value)` | `(decimal\|number) → integer` | `integer` | Truncate toward zero |
| `round(value)` | `(decimal\|number) → integer` | `integer` | Banker's rounding |
| `round(value, places)` | `(numeric, integer) → decimal` | `decimal` | `places` must be ≥ 0. **Explicit bridge: `number → decimal`.** |

### Bridge functions

| Function | Signature | Return type | Notes |
|---|---|---|---|
| `approximate(value)` | `(decimal) → number` | `number` | **Explicit bridge: `decimal → number`.** Makes precision loss visible. |
| `round(value, places)` | `(numeric, integer) → decimal` | `decimal` | **Explicit bridge: `number → decimal`.** Also serves as general rounding. |

These two functions — plus the rounding family for `→ integer` — are the **only** mechanisms that cross lane boundaries. No cast syntax. No implicit coercion.

### Edge-value behavior

Rounding and bridge functions must define behavior at boundaries:

| Scenario | `floor` | `ceil` | `truncate` | `round` (banker's) |
|----------|---------|--------|------------|---------------------|
| `2.5` | `2` | `3` | `2` | `2` (banker's: round-half-to-even) |
| `-2.5` | `-3` | `-2` | `-2` | `-2` (banker's: round-half-to-even) |
| `3.5` | `3` | `4` | `3` | `4` (banker's: round-half-to-even) |
| `0.0` | `0` | `0` | `0` | `0` |

**`round(value, places)` with large `places`:** If `places` exceeds the value's actual precision, the result is unchanged (no zero-padding artifacts). `places` must be ≥ 0 — negative places are a compile-time error.

**`approximate(value)` precision characteristics:** Converts `decimal` to IEEE 754 `double`. Values with more than ~15-17 significant digits lose trailing precision. This is the known cost of the bridge — the function name "approximate" makes this explicit.

**Non-finite `number` inputs:** IEEE 754 `double` can represent `+∞`, `-∞`, and `NaN`. Precept's `number` lane inherits these representations. Behavior when non-finite values reach rounding functions:

| Input | `floor` / `ceil` / `truncate` / `round` | `round(value, places)` |
|-------|------------------------------------------|------------------------|
| `+∞` | Runtime fault — integer overflow | Runtime fault — cannot round infinity |
| `-∞` | Runtime fault — integer overflow | Runtime fault — cannot round infinity |
| `NaN` | Runtime fault — NaN is not a number | Runtime fault — NaN is not a number |

Non-finite values cannot reach `decimal`-lane rounding (since `decimal` has no infinity/NaN representation). They can only occur in the `number` lane. The proof engine may be able to prove non-finiteness is impossible for specific expressions, but the runtime must handle it defensively.

**Integer conversion overflow:** `floor(1e20)` produces a value outside `integer` range. This is a runtime fault — the value exceeds `long.MaxValue` / `long.MinValue`. The same applies to `ceil`, `truncate`, and `round` on very large `number` or `decimal` values. Statically preventable when the proof engine can bound the expression range.

### String functions

| Function | Signature | Return type | Notes |
|---|---|---|---|
| `trim(s)` | `(string) → string` | `string` | Remove leading and trailing whitespace |
| `startsWith(s, prefix)` | `(string, string) → boolean` | `boolean` | Case-sensitive prefix test |
| `endsWith(s, suffix)` | `(string, string) → boolean` | `boolean` | Case-sensitive suffix test |
| `toLower(s)` | `(string) → string` | `string` | Lowercase (invariant culture) |
| `toUpper(s)` | `(string) → string` | `string` | Uppercase (invariant culture) |
| `left(s, n)` | `(string, integer) → string` | `string` | Leftmost N code units (clamped to string length) |
| `right(s, n)` | `(string, integer) → string` | `string` | Rightmost N code units (clamped to string length) |
| `mid(s, start, length)` | `(string, integer, integer) → string` | `string` | Substring, 1-indexed (clamped); `start` and `length` must be positive `integer` |

### Function lane integrity rule

A function keeps its `decimal` overload if and only if the mathematical operation is **closed over finite decimals** — meaning decimal input always produces a result exactly representable as a finite decimal.

- **Closed:** `abs`, `min`, `max`, `clamp`, `pow` (integer exponent), `round`, `floor`, `ceil`, `truncate` — these all have `decimal` overloads.
- **Not closed:** `sqrt`, and future functions (`log`, `sin`, `cos`, `exp`) — these live exclusively in the `number` lane. Authors reach them via `approximate()`.

---

## Open Questions / Implementation Notes

_TBD — open questions will be captured here as the type checker and evaluator implementation progresses._

---

## Cross-References

This document is the canonical source for primitive type rules. Other documents reference — not restate — these rules:

| Document | What it references | Link pattern |
|---|---|---|
| [Language Spec](precept-language-spec.md) | §3.1 type inventory, §3.2 widening, §3.6 operator typing | Brief summary + "See [Primitive Types](primitive-types.md)" |
| [Type Checker](../compiler/type-checker.md) | §4.2 widening, §4.2a numeric lanes | Brief summary + "See [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules)" |
| [Language Vision](../archive/language-design/precept-language-vision.md) | Archived — numeric lane system, choice type, operator contracts | Brief summary + link |
| [Business-Domain Types](business-domain-types.md) | D12 scalar operand contract, widening ceiling | References this doc for the conversion map |
| [Temporal Type System](temporal-type-system.md) | Numeric backing for duration scaling | References this doc for `number`-lane duration arithmetic |
