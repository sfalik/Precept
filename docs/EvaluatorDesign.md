# Expression Evaluator Design

Date: 2026-04-19

Status: **Draft — Track B design for issue #115. Pending design review.**

Research grounding: [research/language/evaluator-architecture-survey.md](../research/language/evaluator-architecture-survey.md) — CEL, FEEL/DMN, C# spec §12.4.7, F#, Kotlin, NCalc, DynamicExpresso, EF Core, OData, NodaMoney, FsCheck precedent survey.

> This document describes the **target state** of the expression evaluator after the numeric lane integrity campaign (issue #115). Sections describing architecture, rules, and contracts are written in "to be" form — as if the target implementation is complete. The "Current State Analysis" section describes what is broken today and why.

---

## Overview

The expression evaluator is Precept's runtime computation layer. It evaluates every DSL expression — guards, rules, ensures, set assignments, computed fields, and conditional branches — against a live entity instance, producing the values that the runtime engine uses to decide whether a transition commits or rejects.

The evaluator operates over three numeric type families:

| Family | C# backing type | Semantics | DSL keyword |
|--------|-----------------|-----------|-------------|
| **Integer** | `long` | Discrete, countable, exact over the integers | `integer` |
| **Decimal** | `decimal` | Exact base-10, closed over exact arithmetic | `decimal` |
| **Number** | `double` | Approximate IEEE 754 binary64, for inherently inexact operations | `number` |

**The semantic fidelity guarantee:** Every value produced by the evaluator preserves the type identity declared or inferred for it in the DSL. A `decimal` field's value is always a C# `decimal`; it is never silently widened to `double` for comparison, storage, or arithmetic. An integer-shaped surface (`.count`, `.length`) always produces a `long`. The evaluator does not present approximation as exactness — the boundary between exact and approximate lanes is visible, explicit, and enforced at every evaluation point.

This guarantee is grounded in Precept's philosophy: *"Precept does not present approximation as exactness. If a value or operation is inherently approximate, that fact must be explicit in the contract."*

---

## Philosophy-Rooted Design Principles

The following principles govern the evaluator's design. Each traces to Precept's core philosophy commitments.

1. **Exactness is the default for business arithmetic.** The `decimal` lane exists because most business-domain calculations — pricing, tax, fees, balances, rates — require exact base-10 arithmetic. An invoice total of `$100.10` must not become `$100.09999999999999` through an internal IEEE 754 intermediary. The `decimal` backing type provides this guarantee. This is the direct realization of the philosophy's numeric exactness commitment. *(Philosophy: "Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes.")*

2. **Approximation is explicit, not hidden.** The `number` lane exists for inherently approximate operations: `sqrt`, `pow` with non-integer exponents (future), and any domain where IEEE 754 double precision is the correct model. Values in the `number` lane are honestly approximate. The contract does not pretend they are exact. *(Philosophy: "If approximation is part of the domain, the contract must say so plainly.")*

3. **Lane boundaries are type boundaries.** Moving a value from one numeric lane to another is a type-system event, not a silent coercion. Integer→decimal widening is exact and implicit. Integer→number widening is implicit (range-preserving, though precision may narrow for very large integers). Number→decimal is a **type error** unless the author explicitly bridges via `round(value, places)`, `floor(value)`, `ceil(value)`, or `truncate(value)`. Decimal→number widening is implicit (the value may lose precision — this is the author's explicit choice by using a number-typed context). *(Philosophy: "that line [between exact and approximate] must be visible in the type system and public surface.")*

4. **Prevention at the surface, not detection at depth.** The type checker rejects expressions that would silently cross lane boundaries. A `decimal` field cannot be assigned a `number` expression without explicit conversion. This prevents precision loss by construction — the invalid assignment is never evaluated. *(Philosophy: prevention, not detection.)*

5. **Integer means integer.** Surfaces that produce or consume discrete counts — `.count`, `.length`, collection indices, string slicing parameters — are typed as `integer`, not as generic `number`. The evaluator produces `long` values for these surfaces. Callers with `number` values must normalize explicitly (`floor`, `ceil`, `truncate`, `round`) before crossing into integer-shaped APIs. No silent truncation. *(Philosophy: "Precept should not advertise a broader numeric contract than the runtime meaning actually supports.")*

6. **Determinism across all lanes.** Same expression, same data, same numeric lane, same result. The evaluator does not use culture-dependent formatting, non-deterministic rounding, or platform-dependent floating-point modes. `decimal` arithmetic uses C#'s deterministic `decimal` operators. `double` arithmetic uses IEEE 754 semantics. *(Philosophy: determinism.)*

7. **Inspectability through honest types.** When the MCP `precept_inspect` or `precept_fire` tools serialize instance data, the serialized value preserves lane identity. A `decimal` field serializes as a JSON number with its exact decimal representation. An `integer` field serializes as a JSON integer. A `number` field serializes as a JSON number (IEEE 754). The consumer can distinguish lanes from the serialized output. *(Philosophy: full inspectability, nothing hidden.)*

8. **One explicit bridge, not many hidden ones.** `round(number, places) → decimal` is the sole deliberate bridge from the approximate `number` lane into the exact `decimal` lane. This bridge does not "recover" exactness from the source value — it produces a `decimal` value normalized to authored precision. No other implicit or hidden `number → decimal` paths exist. *(Locked design note, issue #115.)*

9. **Tests assert the contract, not the leak.** Test expectations must match the semantic contract: decimal-lane tests assert `decimal`-typed results, integer-shaped surface tests assert `long` results, and `number`-lane tests assert `double` results. Tests that normalize via `Convert.ToDouble()` and approximate comparisons are themselves part of the semantic drift surface and must be updated alongside the evaluator. *(Locked design note, issue #115.)*

---

## Current State Analysis

The expression evaluator exists and is functionally correct for most expression evaluation. However, it has systemic numeric lane integrity violations that compromise the semantic fidelity guarantee. These violations span the parser, model, type checker, evaluator, and runtime boundary — they are not isolated bugs but a consistent pattern of collapsing distinct numeric lanes through `double`.

### Critical Finding 1: Parser collapses decimal literals to `double`

`PreceptParser.ToNumericLiteralValue()` ([PreceptParser.cs](../src/Precept/Dsl/PreceptParser.cs#L235)) returns `long` for whole-number literals and `double` for fractional/scientific literals. A DSL literal `0.1` in a `decimal` field context becomes `double 0.1` (which is `0.1000000000000000055511151231257827021181583404541015625` in IEEE 754) at parse time. The exact base-10 value is irrecoverably lost before the evaluator ever sees it.

### Critical Finding 2: Field constraints stored as `double`

`FieldConstraint.Min(double Value)` and `FieldConstraint.Max(double Value)` ([PreceptModel.cs](../src/Precept/Dsl/PreceptModel.cs#L94)) store constraint bounds as `double`. A declaration `field Price as decimal min 0.01 max 999.99` stores its bounds as `double`, losing exact decimal representation. The runtime enforces constraints against values that have already been silently approximated.

### Critical Finding 3: Type checker maps `decimal` as `number`

`PreceptTypeChecker.MapLiteralKind()` ([PreceptTypeChecker.cs](../src/Precept/Dsl/PreceptTypeChecker.cs#L3512)) classifies C# `decimal` runtime values as `StaticValueKind.Number`, not `StaticValueKind.Decimal`. This means the type checker cannot distinguish exact decimal values from approximate number values during type inference — the decimal lane is invisible at type-check time.

### Critical Finding 4: Type checker maps `.count` and `.length` as `number`

`BuildSymbolKinds()` ([PreceptTypeChecker.cs](../src/Precept/Dsl/PreceptTypeChecker.cs#L489)) maps `Collection.count` and `Field.length` to `StaticValueKind.Number`. These are discrete integer surfaces — a collection cannot have 2.5 elements — but the type system treats them as approximate floating-point values.

### Critical Finding 5: Evaluator collapses all arithmetic through `double`

`TryToNumber()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L734)) converts every numeric type — including `decimal` — to `double`. Every binary arithmetic and comparison operator calls `TryToNumber` as its fallback path, meaning that `decimal` values silently lose precision during evaluation whenever both operands aren't matched by the `long`-specific fast path.

### Critical Finding 6: `min`/`max` compare via `double`

`ReduceComparable()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L500)) uses `TryToNumber` (which returns `double`) for all comparisons, even when all arguments are `decimal`. Two `decimal` values `0.1m` and `0.2m` are compared as `double`, producing correct results in most cases but violating the lane-preservation contract and risking edge-case divergence.

### Critical Finding 7: Collections normalize all numerics to `double`

`CollectionValue.NormalizeValue()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L917)) converts every numeric type to `double` before storage. A `set<decimal>` collection stores `double` values internally. `CollectionComparer` compares via `double`. This means `set<decimal>` has `double` ordering semantics — the declared inner type is a lie at runtime.

### Critical Finding 8: Integer surfaces produce `double`

`.count` returns `(double)collection.Count` and `.length` returns `(double)str.Length` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L63)). These are integer values cast to `double` for no semantic reason. Downstream arithmetic inherits the `double` lane instead of the `integer` lane.

### Critical Finding 9: Runtime coercion crosses lanes silently

`CoerceToDecimal()` ([PreceptRuntime.cs](../src/Precept/Dsl/PreceptRuntime.cs#L834)) accepts `double` and `float` inputs and casts them to `decimal` — silently importing approximate values into the exact lane. `CoerceToNumber()` ([PreceptRuntime.cs](../src/Precept/Dsl/PreceptRuntime.cs#L813)) collapses everything to `double`. JSON `UnwrapJsonElement()` returns `double` for all non-integer JSON numbers — a decimal field receiving JSON input `0.1` gets `double 0.1`, not `decimal 0.1m`.

### Critical Finding 10: String slicing silently truncates

`left()`, `right()`, and `mid()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L645)) accept any numeric type for their count/start/length parameters via `TryToNumber`, then silently truncate to `int` via `(int)countNum`. A call `left(Name, 3.7)` silently becomes `left(Name, 3)` — the fractional part is discarded without warning.

### Root Cause

These are not independent bugs. They share a single root cause: the evaluator was originally built with `double` as the universal numeric representation, before the three-type numeric system (`integer`/`decimal`/`number`) was fully specified. The `long` integer lane was added later (issue #29) with correct fast paths for homogeneous integer operations, but the `decimal` lane was scaffolded (issue #27) without corresponding evaluator, collection, or runtime support. The result is a system that declares three numeric lanes in the type system but collapses to two (`long` and `double`) at evaluation time.

---

## Architecture

### Numeric Type Families

The evaluator operates on three numeric type families, each backed by a distinct C# type. Values never change backing type silently — lane identity is preserved through every evaluation step.

| Family | C# type | Precision | Semantic role |
|--------|---------|-----------|---------------|
| **Integer** | `long` | Exact over ±9.2×10¹⁸ | Discrete counts, indices, ordinals. `.count`, `.length`, and all integer-declared fields. |
| **Decimal** | `decimal` | Exact base-10, 28–29 significant digits | Business arithmetic: money, rates, percentages, tax, any domain where `0.1 + 0.2 == 0.3` must be true. |
| **Number** | `double` | IEEE 754 binary64, ~15–17 significant digits | Approximate calculations: `sqrt`, scientific computation, any domain where approximate is acceptable. |

**Backing type is identity.** A value's C# runtime type determines its lane. `long` → integer lane. `decimal` → decimal lane. `double` → number lane. There is no metadata tag — the CLR type *is* the lane.

### Expression Evaluation Pipeline

Expressions flow through the evaluator in a single recursive descent:

```
AST node (PreceptExpression)
  → Evaluate() dispatches by node type
    → Literal: returns value with lane-preserving C# type
    → Identifier: retrieves value from context dictionary (lane preserved by storage)
    → Unary: evaluates operand, applies operator within operand's lane
    → Binary: evaluates both operands, dispatches to lane-appropriate operator
    → Function call: evaluates arguments, dispatches to lane-appropriate function body
    → Conditional: evaluates condition, evaluates selected branch
  → Returns EvaluationResult(Success, Value, Error)
```

The critical invariant: **`Value` in a successful `EvaluationResult` always has a C# type that matches the expression's declared or inferred lane.** If the expression is typed as `decimal`, the returned object is `decimal`. If typed as `integer`, the returned object is `long`. If typed as `number`, the returned object is `double`.

### Operator Dispatch

Binary arithmetic operators dispatch by matching both operands' runtime types, following a dispatch-table model inspired by [CEL's named-function overloads](https://github.com/google/cel-spec/blob/master/doc/langdef.md) (`_+_(int, int) → int`, `_+_(double, double) → double`). Each `(operator, leftType, rightType)` triple maps to a specific implementation and result type. Dispatch priority ensures the narrowest exact lane is preserved:

1. **Both `long`** → integer arithmetic (`long` result)
2. **Both `decimal`** → decimal arithmetic (`decimal` result)
3. **Both `double`** → number arithmetic (`double` result)
4. **Mixed integer + decimal** → widen integer to `decimal`, decimal arithmetic (`decimal` result)
5. **Mixed integer + number** → widen integer to `double`, number arithmetic (`double` result)
6. **Mixed decimal + number** → widen decimal to `double`, number arithmetic (`double` result)

Cases 1–5 mirror the C# language specification's binary numeric promotion rules (§12.4.7.3), which **forbid** mixing `decimal` with `float`/`double` in arithmetic — it is a binding-time error. Case 6 is permitted at the evaluator level only for comparison expressions that the type checker explicitly allows; arithmetic mixing of decimal and number remains a type error.

The type checker prevents case 6 in most contexts (assigning a `number` expression to a `decimal` field is a type error), but the evaluator must handle it correctly for expressions that the type checker permits (e.g., a `number`-typed function result used in a comparison with a `decimal` value).

**Division special case:** Integer division (`long / long`) produces truncated-toward-zero `long` results (C# semantics). This is the expected behavior for integer arithmetic. Authors who need exact fractional results from integer operands should declare their field as `decimal`.

### Helper/Function Evaluation

Built-in functions dispatch to lane-specific implementations. Each function has overloads registered in `FunctionRegistry` ([FunctionRegistry.cs](../src/Precept/Dsl/FunctionRegistry.cs)) with explicit input and output types. The evaluator matches the runtime type of the argument to select the correct overload body:

```
abs(long)    → long       (Math.Abs)
abs(decimal) → decimal    (Math.Abs)
abs(double)  → double     (Math.Abs)

floor(decimal) → long     (Math.Floor → cast to long)
floor(double)  → long     (Math.Floor → cast to long)

round(decimal)        → long     (1-arg: Math.Round → cast to long)
round(double)         → long     (1-arg: Math.Round → cast to long)
round(any, int places) → decimal (2-arg: Math.Round to N places → decimal)

min(long, long, ...)       → long     (comparison via long)
min(decimal, decimal, ...) → decimal  (comparison via decimal)
min(double, double, ...)   → double   (comparison via double)
```

The `round(number, places) → decimal` overload is the **explicit normalization bridge** from the approximate `number` lane into the exact `decimal` lane. This is by design — it is the sole sanctioned crossing point from approximate to exact.

### Collection Semantics

`CollectionValue` stores elements in their declared lane's C# type:

- `set<integer>`: elements stored as `long`. `SortedSet<object>` with `CollectionComparer` that compares via `long`.
- `set<decimal>`: elements stored as `decimal`. `SortedSet<object>` with `CollectionComparer` that compares via `decimal`.
- `set<number>`: elements stored as `double`. `SortedSet<object>` with `CollectionComparer` that compares via `double`.
- `set<string>`: elements stored as `string`. Ordinal comparison.

`NormalizeValue()` coerces incoming values to the collection's declared inner type:
- For `integer` inner type: coerce to `long`
- For `decimal` inner type: coerce to `decimal`
- For `number` inner type: coerce to `double`
- For `string` inner type: `ToString()`

Collection accessors:
- `.count` → `long` (integer lane)
- `.min` / `.max` on `set<decimal>` → `decimal` (preserves inner type)
- `.min` / `.max` on `set<integer>` → `long` (preserves inner type)
- `.min` / `.max` on `set<number>` → `double` (preserves inner type)
- `.peek` on `queue<T>` / `stack<T>` → inner type

### Assignment Contract

Every write path — `set` assignments in Fire transition rows, field updates in Update, and computed-field recomputation — enforces the same contract:

1. **Evaluate the RHS expression** → produces a value with a C# type
2. **Verify lane compatibility** with the target field's declared type
3. **Coerce if necessary** (integer→decimal widening only; no approximate→exact coercion)
4. **Enforce field constraints** (min, max, nonnegative, positive, maxplaces) against the lane-native value
5. **Store the value** in the target field's declared C# type

The constraint enforcement step operates on lane-native values. A `decimal` field's `min` constraint is compared via `decimal` comparison. An `integer` field's `max` constraint is compared via `long` comparison. No double intermediary.

### Runtime Coercion Rules

Values entering the runtime from external sources (JSON, C# API callers) are coerced to the target field's declared type:

| Source type | Target `integer` | Target `decimal` | Target `number` |
|-------------|------------------|-------------------|-----------------|
| `long` / `int` / `short` / `byte` | `long` (exact) | `decimal` (exact widening) | `double` (range-preserving) |
| `decimal` | Type error | `decimal` (identity) | `double` (lossy — explicit choice) |
| `double` / `float` | Type error | Type error | `double` (identity) |
| JSON integer | `long` | `decimal` | `double` |
| JSON fractional | Type error | `decimal` (via `JsonElement.GetDecimal()`) | `double` (via `JsonElement.GetDouble()`) |
| `string` (parseable) | `long.TryParse` | `decimal.TryParse` | `double.TryParse` |

**Key rule: `double` → `decimal` is a type error at the runtime boundary.** External callers must provide `decimal`-typed values for `decimal` fields. This prevents approximate values from silently entering the exact lane. The JSON deserializer uses `JsonElement.GetDecimal()` for decimal-typed fields, not `GetDouble()`.

---

## Numeric Lane Integrity Rules

This section defines the core numeric lane contracts. These are the evaluator's invariants — every operation, comparison, storage, and serialization path must satisfy them.

### Integer Lane

**Backing type:** `long` (C# Int64, 64-bit signed integer)

**Semantics:** Discrete, exact, closed over `{+, -, *, /, %}` where `/` is truncating integer division and `%` is integer modulo. Overflow throws `OverflowException` in `checked` contexts (Precept uses `checked` for safety-critical paths).

**What produces integer values:**
- Integer literals (`42`, `-7`, `0`)
- Integer-declared fields (`field Count as integer`)
- Integer-typed event arguments
- `.count` on any collection
- `.length` on string fields and string event arguments
- `floor(decimal)`, `floor(number)` → `long`
- `ceil(decimal)`, `ceil(number)` → `long`
- `truncate(decimal)`, `truncate(number)` → `long`
- `round(decimal)`, `round(number)` (1-arg) → `long`
- `abs(long)` → `long`
- `min(long, long, ...)` → `long`
- `max(long, long, ...)` → `long`
- `clamp(long, long, long)` → `long`
- `pow(long, long)` → `long`
- Homogeneous integer arithmetic (`long op long` → `long`)

**Widening rules:**
- Integer → decimal: always exact (`(decimal)longValue`). Implicit.
- Integer → number: range-preserving (`(double)longValue`). Implicit. Note: `long` values beyond ±2⁵³ may lose precision in `double` — this is the known IEEE 754 limitation, acceptable because such values are rare in business domains.

### Decimal Lane

**Backing type:** `decimal` (C# Decimal, 128-bit, 28–29 significant digits, exact base-10)

**Semantics:** Exact base-10 arithmetic, closed over `{+, -, *}`. Division (`/`) and modulo (`%`) produce exact results when the result is representable in 28–29 significant digits. The `decimal` type is the correct backing for business arithmetic: `0.1m + 0.2m == 0.3m` is `true`.

**What produces decimal values:**
- Fractional literals in decimal context (`0.1`, `3.14`, `99.99` — resolved to `decimal` by context-sensitive literal typing)
- Decimal-declared fields (`field Price as decimal`)
- Decimal-typed event arguments
- `round(any, places)` → `decimal` (the explicit normalization bridge)
- `abs(decimal)` → `decimal`
- `min(decimal, decimal, ...)` → `decimal`
- `max(decimal, decimal, ...)` → `decimal`
- `clamp(decimal, decimal, decimal)` → `decimal`
- `pow(decimal, long)` → `decimal`
- `sqrt(decimal)` → `decimal` (note: internally uses `(decimal)Math.Sqrt((double)d)` — inherently approximate but returns to decimal lane; see § Helper/Function Type Contracts for the precision note)
- Homogeneous decimal arithmetic (`decimal op decimal` → `decimal`)
- Mixed integer+decimal arithmetic (`long op decimal` → `decimal`)

**Widening rules:**
- Decimal → number: implicit (`(double)decimalValue`). **Lossy** — the value may lose precision. This widening is permitted because the author has explicitly placed the value in a `number` context, accepting the precision trade.
- Decimal → integer: type error. Use `floor()`, `ceil()`, `truncate()`, or `round()`.

**Closure guarantee:** Homogeneous decimal expressions — where all operands are `decimal` — remain in the decimal lane through every intermediate step. No intermediate value is ever computed as `double`. This is the core semantic fidelity guarantee for business arithmetic.

### Number Lane

**Backing type:** `double` (IEEE 754 binary64, 64-bit floating-point)

**Semantics:** Approximate. IEEE 754 arithmetic with all its properties: `0.1 + 0.2 ≈ 0.30000000000000004`. The `number` lane is honest about this — it does not pretend to be exact.

**What produces number values:**
- Fractional literals in number context (resolved by context-sensitive literal typing)
- Number-declared fields (`field Score as number`)
- Number-typed event arguments
- `sqrt(number)` → `number` (natively approximate — `Math.Sqrt`)
- `abs(double)` → `double`
- `min(double, double, ...)` → `double`
- `max(double, double, ...)` → `double`
- `clamp(double, double, double)` → `double`
- `pow(double, long)` → `double`
- Homogeneous number arithmetic (`double op double` → `double`)
- Mixed integer+number arithmetic (`long op double` → `double`)
- Mixed decimal+number arithmetic (`decimal op double` → `double`)

**Narrowing rules:**
- Number → decimal: **type error.** The author must use `round(value, places)` to explicitly bridge.
- Number → integer: **type error.** The author must use `floor()`, `ceil()`, `truncate()`, or `round()`.

### Cross-Lane Rules Summary

| From → To | Allowed? | Mechanism | Precision |
|-----------|----------|-----------|-----------|
| Integer → Decimal | Yes (implicit) | `(decimal)longValue` | Exact |
| Integer → Number | Yes (implicit) | `(double)longValue` | Range-preserving (±2⁵³ limit) |
| Decimal → Number | Yes (implicit) | `(double)decimalValue` | Lossy — author's explicit choice |
| Decimal → Integer | **No** | Requires `floor`/`ceil`/`truncate`/`round` | N/A |
| Number → Decimal | **No** | Requires `round(value, places)` | Normalized to authored precision |
| Number → Integer | **No** | Requires `floor`/`ceil`/`truncate`/`round` | N/A |

### Business-Domain Operator Table Contract

For the relational comparison and equality operators (`==`, `!=`, `>`, `>=`, `<`, `<=`):

| Operand types | Comparison lane | Notes |
|---------------|-----------------|-------|
| `long` vs `long` | Integer (`long` comparison) | Exact |
| `decimal` vs `decimal` | Decimal (`decimal` comparison) | Exact base-10 |
| `double` vs `double` | Number (`double` comparison) | IEEE 754 |
| `long` vs `decimal` | Decimal (widen integer) | Exact |
| `long` vs `double` | Number (widen integer) | Approximate |
| `decimal` vs `double` | Number (widen decimal) | Approximate — the `decimal` operand is the author's explicit choice to compare in the number lane |

**Design Decision (Option A — decimal scalar operand, not number):** When a decimal field is multiplied by a literal or compared to a literal, the literal resolves as `decimal` (not `number`). The expression `Price * 1.08` with `field Price as decimal` evaluates as `decimal * decimal → decimal`. The literal `1.08` is not parsed as `double` — it is resolved as `decimal` by context-sensitive literal typing (see § Context-Sensitive Literal Typing).

---

## Context-Sensitive Literal Typing

Fractional numeric literals in the DSL (e.g., `0.1`, `3.14`, `1.5`) do not have an inherent lane — their lane is determined by the expression context in which they appear.

### Resolution Rules

1. **Field assignment context:** `set Price = 0.1` where `Price` is `decimal` → literal resolves as `decimal 0.1m`. Where `Score` is `number` → literal resolves as `double 0.1`.
2. **Binary expression context:** `Price * 1.08` where `Price` is `decimal` → `1.08` resolves as `decimal`. `Score + 0.5` where `Score` is `number` → `0.5` resolves as `double`.
3. **Comparison context:** `Price > 100.00` where `Price` is `decimal` → `100.00` resolves as `decimal`. `Score > 50.0` where `Score` is `number` → `50.0` resolves as `double`.
4. **Function argument context:** `round(Score, 2)` → `Score` is number, stays number. `min(Price, 100.00)` where `Price` is `decimal` → `100.00` resolves as `decimal`.
5. **Field constraint context:** `field Price as decimal min 0.01 max 999.99` → constraint values `0.01` and `999.99` resolve as `decimal`.
6. **Default value context:** `field Price as decimal = 0.0` → default value resolves as `decimal`.

### Non-Ambiguous Inference Invariant (Design Decision 9)

**Every fractional literal resolves to exactly one numeric lane.** There is no expression context where a literal's lane is ambiguous. The inference rules are:

- If the expression context determines a lane (target field type, peer operand type, function parameter type), the literal adopts that lane.
- If no context is available (standalone literal in a type-unconstrained position), the literal defaults to `decimal` — the exact lane is the safer default for business-domain use.
- The parser stores fractional literals in a form that preserves the original text, deferring lane resolution to the type checker. At type-check time, the literal's lane is determined and the AST value is set to the appropriate C# type (`decimal` or `double`).

**Implementation mechanism:** The parser produces fractional literals as a representation that preserves the original decimal text (e.g., the string `"0.1"`). The type checker, which knows the expression context, resolves the literal to `decimal 0.1m` or `double 0.1` as appropriate. By the time the evaluator sees the literal, its `Value` property already holds the correctly-typed C# value.

---

## Helper/Function Type Contracts

Every built-in function has explicit lane contracts. The following table documents the target state.

### Numeric Functions

| Function | Input type(s) | Output type | Lane rule | Current violation |
|----------|---------------|-------------|-----------|-------------------|
| `abs(integer)` | `long` | `long` | Stays integer | None (correct today) |
| `abs(decimal)` | `decimal` | `decimal` | Stays decimal | None (correct today) |
| `abs(number)` | `double` | `double` | Stays number | None (correct today) |
| `floor(decimal)` | `decimal` | `long` | Decimal → integer (explicit rounding) | None (correct today) |
| `floor(number)` | `double` | `long` | Number → integer (explicit rounding) | None (correct today) |
| `ceil(decimal)` | `decimal` | `long` | Decimal → integer (explicit rounding) | None (correct today) |
| `ceil(number)` | `double` | `long` | Number → integer (explicit rounding) | None (correct today) |
| `truncate(decimal)` | `decimal` | `long` | Decimal → integer (explicit rounding) | None (correct today) |
| `truncate(number)` | `double` | `long` | Number → integer (explicit rounding) | None (correct today) |
| `round(integer)` | `long` | `long` | Identity (no-op) | None (correct today) |
| `round(decimal)` | `decimal` | `long` | Decimal → integer (banker's rounding) | **Yes**: current impl converts to `decimal` via `TryToDecimal` then rounds — correct but `TryToDecimal` accepts `double` input |
| `round(number)` | `double` | `long` | Number → integer (banker's rounding) | **Yes**: same `TryToDecimal` path — mixes lanes |
| `round(any, places)` | any numeric, `long` | `decimal` | **Explicit bridge**: number→decimal normalization | **Yes**: operates via `TryToDecimal` which accepts `double` — conceptually correct but should produce `decimal` natively |
| `min(integer, ...)` | `long, ...` | `long` | Stays integer | **Yes**: `ReduceComparable` uses `TryToNumber` (double) for comparison |
| `min(decimal, ...)` | `decimal, ...` | `decimal` | Stays decimal | **Yes**: same — decimal compared as double |
| `min(number, ...)` | `double, ...` | `double` | Stays number | None (already double) |
| `max(integer, ...)` | `long, ...` | `long` | Stays integer | **Yes**: same as min |
| `max(decimal, ...)` | `decimal, ...` | `decimal` | Stays decimal | **Yes**: same as min |
| `max(number, ...)` | `double, ...` | `double` | Stays number | None (already double) |
| `clamp(integer, ...)` | `long, long, long` | `long` | Stays integer | Partial — has `long` fast path but falls to double |
| `clamp(decimal, ...)` | `decimal, decimal, decimal` | `decimal` | Stays decimal | Partial — has `decimal` fast path but falls to double |
| `clamp(number, ...)` | `double, double, double` | `double` | Stays number | None |
| `pow(integer, integer)` | `long, long` | `long` | Stays integer | None (correct today) |
| `pow(decimal, integer)` | `decimal, long` | `decimal` | Stays decimal | None (correct today) |
| `pow(number, integer)` | `double, long` | `double` | Stays number | None (correct today) |
| `sqrt(decimal)` | `decimal` | `decimal` | Returns to decimal | **Precision note**: internally computes `(decimal)Math.Sqrt((double)d)` — the result is approximate, returned as `decimal`. This is a known precision limitation documented in the contract. |
| `sqrt(number)` | `double` | `double` | Stays number | None (correct today) |

### String Functions

| Function | Input type(s) | Output type | Lane rule | Current violation |
|----------|---------------|-------------|-----------|-------------------|
| `left(string, integer)` | `string, long` | `string` | Count param must be integer | **Yes**: accepts any numeric via `TryToNumber`, silently truncates |
| `right(string, integer)` | `string, long` | `string` | Count param must be integer | **Yes**: same |
| `mid(string, integer, integer)` | `string, long, long` | `string` | Start and length must be integer | **Yes**: same |
| `toLower(string)` | `string` | `string` | N/A | None |
| `toUpper(string)` | `string` | `string` | N/A | None |
| `trim(string)` | `string` | `string` | N/A | None |
| `startsWith(string, string)` | `string, string` | `boolean` | N/A | None |
| `endsWith(string, string)` | `string, string` | `boolean` | N/A | None |

---

## Collection Storage Contract

Collections preserve element type identity through storage, retrieval, comparison, and serialization.

| Collection type | Element C# type | Comparison mechanism | Ordering |
|-----------------|-----------------|----------------------|----------|
| `set<integer>` | `long` | `long.CompareTo` | Numeric ascending |
| `set<decimal>` | `decimal` | `decimal.CompareTo` | Numeric ascending (exact base-10) |
| `set<number>` | `double` | `double.CompareTo` | IEEE 754 numeric ascending |
| `set<string>` | `string` | `StringComparison.Ordinal` | Ordinal ascending |
| `set<boolean>` | `bool` | `bool.CompareTo` | `false` < `true` |
| `set<choice(...)>` | `string` | `StringComparison.Ordinal` | Ordinal ascending |
| `queue<T>` / `stack<T>` | Same as above per `T` | N/A (FIFO/LIFO, not sorted) | Insertion order |

**NormalizeValue contract:** When a value is added to a collection, `NormalizeValue` coerces it to the collection's declared inner type using the same rules as the runtime assignment contract. A `long` value added to a `set<decimal>` is widened to `decimal`. A `double` value added to a `set<decimal>` is a type error.

**CollectionComparer contract:** `CollectionComparer` dispatches comparison by element type. `decimal` elements are compared via `decimal.CompareTo`, never via `double` intermediary. This ensures that `set<decimal>` ordering is exact base-10 — `{0.1m, 0.2m, 0.3m}` sorts correctly without IEEE 754 artifacts.

**`.contains` semantics:** Element lookup uses the same `CollectionComparer`, ensuring that `mySet contains 0.1` (where `mySet` is `set<decimal>`) compares `decimal 0.1m` against `decimal` elements, not `double 0.1` against `double` elements.

---

## Assignment Contract Unification

All three write paths — **Fire** (transition row `set` assignments), **Update** (direct field edits), and **computed-field recomputation** — enforce the same contract:

### Unified Write Contract

```
1. Evaluate RHS expression → (value, type)
2. Lane compatibility check:
   a. If value type matches target field type → proceed
   b. If value is long and target is decimal → widen to decimal (exact)
   c. If value is long and target is number → widen to double
   d. If value is decimal and target is number → widen to double (lossy, author's choice)
   e. Otherwise → type error (reject the operation)
3. Constraint enforcement (on the lane-native value):
   a. nonnegative: value >= 0 (compared in target's lane)
   b. positive: value > 0 (compared in target's lane)
   c. min V: value >= V (compared in target's lane, V stored in target's lane type)
   d. max V: value <= V (compared in target's lane, V stored in target's lane type)
   e. maxplaces N: decimal places <= N (decimal lane only)
4. Store value in target field (value is now in target's C# type)
```

**Constraint storage:** `FieldConstraint.Min` and `FieldConstraint.Max` store their bound values in a type that matches the field's declared numeric type. For `decimal` fields, bounds are `decimal`. For `integer` fields, bounds are `long`. For `number` fields, bounds are `double`. This ensures constraint comparisons are lane-native — a `decimal` field's `min 0.01` is compared via `decimal.CompareTo`, not `double` comparison.

### Fire Path

In `PreceptRuntime.Fire()`, after guard evaluation selects a matching transition row, each `set` assignment in the row:
1. Evaluates the RHS expression using the current evaluation context (which includes prior assignments' results — sequential flow)
2. Runs the unified write contract above
3. Stores the result, which becomes visible to subsequent assignments in the same row

### Update Path

In `PreceptRuntime.Update()`, each field edit:
1. Takes the externally-provided value
2. Coerces to the target field type per the runtime coercion rules
3. Runs the unified write contract (steps 2–4)

### Computed-Field Path

Computed fields (`field Net as decimal = Gross - Tax`) are recomputed after every Fire or Update:
1. Evaluate the derived expression against the current field state
2. Run the unified write contract
3. Store the result

---

## Integration Points

| Integration point | Component | File | Role |
|---|---|---|---|
| Literal parsing | Parser | `PreceptParser.cs` | `ToNumericLiteralValue()` produces lane-preserving C# types. Fractional literals stored as text or `decimal`, resolved at type-check time. |
| Literal kind inference | Type checker | `PreceptTypeChecker.cs` | `MapLiteralKind()` maps `long` → `Integer`, `decimal` → `Decimal`, `double` → `Number`. Context-sensitive literal resolution determines the final type. |
| Symbol table | Type checker | `PreceptTypeChecker.cs` | `BuildSymbolKinds()` maps `.count` and `.length` to `StaticValueKind.Integer`. |
| Constraint storage | Model | `PreceptModel.cs` | `FieldConstraint.Min` and `FieldConstraint.Max` store values in lane-appropriate types. |
| Expression evaluation | Evaluator | `PreceptExpressionEvaluator.cs` | All operator dispatch, function evaluation, and collection operations preserve lane identity. |
| Runtime coercion | Runtime | `PreceptRuntime.cs` | `CoerceToDecimal()` rejects `double`/`float` inputs. `CoerceToNumber()` returns `double`. `CoerceToInteger()` returns `long`. JSON unwrapping uses `GetDecimal()` for decimal fields. |
| Fire pipeline | Runtime | `PreceptRuntime.cs` | Assignment contract enforced at each `set` in a transition row. |
| Update pipeline | Runtime | `PreceptRuntime.cs` | Same assignment contract enforced for direct field edits. |
| Computed fields | Runtime | `PreceptRuntime.cs` | Same assignment contract enforced during recomputation. |
| Proof engine | Type checker | `ProofContext.cs` | Interval arithmetic operates in the `double` (`NumericInterval`) lane per [ProofEngineDesign.md](ProofEngineDesign.md). The proof engine uses `double` for interval bounds — this is correct because proof intervals are over-approximations, and IEEE 754 widening is conservative (see ProofEngineDesign.md § Numeric Precision and IEEE 754). |
| MCP serialization | MCP tools | `tools/Precept.Mcp/Tools/` | Instance data serialization preserves lane identity. `decimal` values serialize with exact decimal representation. `long` values serialize as JSON integers. `double` values serialize as JSON numbers. |

---

## Design Decisions

### DD1: Three Distinct Numeric Type Families

**Decision:** The evaluator operates on three numeric type families — integer (`long`), decimal (`decimal`), number (`double`) — each with a distinct C# backing type, distinct semantics, and distinct lane-preservation rules.

**Rationale:** Business domains require both exact arithmetic (money, rates, tax) and approximate arithmetic (scientific calculations, scoring). A single `double`-backed numeric type cannot serve both — `0.1 + 0.2 != 0.3` in IEEE 754 is unacceptable for financial calculations. A single `decimal`-backed type is too slow and too restrictive for domains that accept approximation. The three-lane model matches C#'s own numeric type hierarchy and gives authors explicit control over precision semantics.

**External precedent:** CEL (Google Common Expression Language) uses three distinct numeric types (`int`, `uint`, `double`) with no automatic arithmetic conversions — cross-type arithmetic is a type error. C# itself (§12.4.7) defines separate predefined operators per numeric type and forbids mixing `decimal` with `float`/`double`. F# forbids all implicit numeric conversions. FEEL/DMN uses a single Decimal128 type — simpler but less expressive. Precept's three-lane model sits between CEL's strictness and C#'s promotion rules.

**Alternatives rejected:** (a) Single `double` lane (current broken state) — violates philosophy's numeric exactness commitment. (b) Single `decimal` lane (FEEL/DMN approach) — too slow for approximate-acceptable domains, cannot represent `sqrt` natively. (c) Two lanes (`integer` + `decimal` only) — forces approximate operations to produce decimal results, which are misleadingly exact-looking.

**Tradeoff accepted:** Three lanes increase operator dispatch complexity (6 binary operand combinations instead of 1). The type checker catches most cross-lane errors at compile time, limiting runtime dispatch to well-typed expressions.

### DD2: Decimal Lane Closed Over Exact Operations

**Decision:** Homogeneous `decimal` arithmetic remains `decimal`-native across all operations: arithmetic (`+`, `-`, `*`, `/`, `%`), comparison (`==`, `!=`, `>`, `>=`, `<`, `<=`), `min`, `max`, `clamp`, collection ordering, and collection membership. No intermediate `double` conversion occurs.

**Rationale:** The decimal lane's value proposition is exactness. If `decimal * decimal` internally converts to `double` for computation, the lane guarantee is violated — the user cannot trust that `Price * TaxRate` produces an exact result. Lane closure is the irreducible minimum for the decimal lane to be meaningful.

**Alternatives rejected:** Selective decimal paths (e.g., decimal arithmetic but `double` comparison) — creates inconsistency where `decimal + decimal` is exact but `decimal == decimal` is approximate.

**Tradeoff accepted:** `decimal` arithmetic is materially slower than `double` arithmetic (exact ratio varies by operation and hardware — the .NET team benchmarks decimal operations separately in `Perf.Decimal.cs`). Acceptable for a business-rule engine where correctness dominates throughput.

**Decimal overflow policy:** When a homogeneous `decimal` operation overflows `decimal` range, the evaluator throws an `OverflowException` — it does NOT silently widen to `double`. NCalc's `DecimalAsDefault` mode silently falls back to `double` infinity on decimal overflow; Precept explicitly rejects this pattern because silent widening is exactly the lane violation this design prevents.

### DD3: Integer-Shaped Surfaces Return Integer

**Decision:** `.count`, `.length`, `EventName.ArgName.length`, and all rounding/truncation functions return `long` (integer). String slicing parameters (`left`, `right`, `mid` count/start/length) require `integer` arguments.

**Rationale:** A collection count is an integer by definition — it cannot be 2.5. Modeling it as `double` introduces a false semantic claim. The philosophy requires honesty: "Precept should not advertise a broader numeric contract than the runtime meaning actually supports."

**Alternatives rejected:** Keep `.count`/`.length` as `number` and document the truncation — this normalizes the lie instead of fixing it.

**Tradeoff accepted:** Authors with `number`-typed values who want to use `left(str, count)` must explicitly convert via `floor()`, `ceil()`, or `truncate()`. This adds one function call but makes the truncation visible and intentional.

### DD4: Parser Preserves Decimal Literal Fidelity

**Decision:** The parser produces fractional literals in a form that preserves the original decimal text. The type checker resolves the literal to the appropriate C# type (`decimal` or `double`) based on expression context. `FieldConstraint.Min` and `FieldConstraint.Max` store values in the target field's lane type.

**Rationale:** If the parser converts `0.01` to `double` at parse time, the exact base-10 value is irrecoverably lost. No downstream fix can recover it. This is the root cause of the current decimal lane violations — the semantic fidelity failure occurs at the first stage of the pipeline.

**Alternatives rejected:** Parse as `decimal` always, convert to `double` in number contexts — adds an unnecessary precision step but is viable. Rejected because context-sensitive resolution is cleaner: the literal is resolved once, at the right time, in the right type.

**Tradeoff accepted:** The parser and type checker become more tightly coupled for literal resolution. The parser cannot fully type-check literals in isolation — it defers to the type checker. This is the correct separation of concerns (parsing is syntax, typing is semantics).

### DD5: `round(number, places) → decimal` as Explicit Bridge

**Decision:** `round(number, places)` is the sole sanctioned bridge from the approximate `number` lane to the exact `decimal` lane. No implicit `number → decimal` coercion paths exist.

**Rationale:** Rounding is a deliberate normalization — the author declares "I accept the approximate value and want to fix it to N decimal places." This is philosophically distinct from implicit coercion, which hides the precision loss. The bridge does not "recover" exactness — it produces a `decimal` value normalized to authored precision. The distinction must be clear in the contract.

**Alternatives rejected:** Allow implicit `number → decimal` with a warning — this weakens the lane boundary and trains authors to ignore warnings.

**Tradeoff accepted:** Authors who mix `number` and `decimal` fields must explicitly bridge. This is intentional friction — it forces awareness of the precision boundary.

### DD6: No Implicit `double` → `decimal` at Runtime Boundary

**Decision:** `CoerceToDecimal()` rejects `double` and `float` inputs. External callers must provide `decimal`-typed values for `decimal` fields. JSON deserialization uses `JsonElement.GetDecimal()` for decimal-typed fields.

**Rationale:** If the runtime accepts `double` values for `decimal` fields, the entire evaluator lane integrity is undermined at the API boundary. An external caller passing `0.1` (double) into a `decimal` field silently imports `0.1000000000000000055...` into the exact lane.

**External precedent:** Java's `new BigDecimal(double)` is the most-cited numeric precision pitfall in business software — `new BigDecimal(0.1)` produces `0.1000000000000000055511151231257827021181583404541015625`, not `0.1`. NodaMoney's `MoneyJsonConverter` preserves decimal fidelity by reading with `reader.GetDecimal()` rather than `GetDouble()`. EF Core carries type mapping metadata through translation to preserve decimal semantics even when the backend is awkward.

**Alternatives rejected:** Accept `double` and round to a configured number of places — this silently modifies the input, which violates the principle that the engine is transparent about what it does.

**Tradeoff accepted:** C# callers must explicitly pass `decimal` values (e.g., `0.1m` not `0.1`). JSON callers must ensure their serializer produces decimal-fidelity numbers. This is a breaking change for callers who currently pass `double` values.

### DD7: Proof Engine Operates in `double` Lane

**Decision:** The proof engine's `NumericInterval` continues to use `double` bounds. Interval arithmetic is an over-approximation — the slight widening from `decimal → double` cast is conservative (see [ProofEngineDesign.md](ProofEngineDesign.md) § Numeric Precision and IEEE 754).

**Rationale:** The proof engine proves properties at compile time — it needs to be sound (never claim safe when it isn't), not exact. `double` intervals are sufficient for soundness because any widening produces a larger interval, which can only cause false negatives (missed proofs), never false positives (wrong "safe" claims). Implementing a separate `decimal` interval arithmetic layer would double the proof engine's complexity for negligible precision gain.

**Alternatives rejected:** Dual-lane interval arithmetic (`decimal` intervals for decimal fields, `double` intervals for number fields) — doubles complexity, no soundness benefit.

**Tradeoff accepted:** Proof intervals for `decimal` fields may be slightly wider than the true range due to `decimal → double` conversion. This is a rare false negative, not a correctness issue.

### DD8: `sqrt(decimal) → decimal` Retains Approximate Internals

**Decision:** `sqrt(decimal)` is computed as `(decimal)Math.Sqrt((double)d)`. The result is returned as `decimal` to stay in the decimal lane, but the computation is inherently approximate due to the `double` intermediary.

**Rationale:** C# does not have a native `decimal` square root function. `Math.Sqrt` operates on `double`. The result is cast back to `decimal`, but precision loss from the `double` intermediary is inherent. This is documented in the function contract — the user knows that `sqrt` on a `decimal` is approximate.

**External precedent:** .NET generic math (`INumber<T>`, .NET 7+) explicitly separates `decimal` from IEEE 754 types — `decimal` participates in `IFloatingPoint` but NOT `IFloatingPointIeee754`. Generic `Sqrt` is defined in terms of IEEE 754 types only. This confirms that `sqrt(decimal)` is inherently a bridge operation in the .NET ecosystem, not a native decimal capability.

**Alternatives rejected:** (a) Make `sqrt(decimal)` return `number` — this would force decimal-lane users into the number lane for a common operation. (b) Implement a Newton's method `decimal` sqrt — overkill for a DSL runtime; the `double` precision (~15 significant digits) is sufficient for all practical business-domain sqrt uses.

**Tradeoff accepted:** `sqrt(decimal)` is the one function where the decimal lane's exactness guarantee is relaxed. The contract is explicit about this.

### DD9: Context-Sensitive Literal Typing with Non-Ambiguous Inference

**Decision:** Fractional literals resolve their type from expression context. Every literal resolves to exactly one type — no ambiguity. Standalone literals default to `decimal`.

**Rationale:** A literal `0.1` in `set Price = 0.1` (where `Price` is `decimal`) should produce `decimal 0.1m`, not `double 0.1`. Context-sensitive typing achieves this without requiring suffix syntax (e.g., `0.1m` vs `0.1d`). Suffix syntax was considered and rejected because it introduces PLT ceremony inappropriate for a business-rule DSL.

**This is a deliberate DSL policy choice, not host-language precedent.** C#, F#, and Kotlin all default unsuffixed fractional literals to `double` and require explicit suffixes (`m`/`M`) for `decimal`. No mainstream programming language uses context-sensitive literal typing for numeric types. However, evaluator libraries provide precedent: NCalc's `DecimalAsDefault` option and DynamicExpresso's configurable literal parsing both support evaluator-wide policies that resolve unsuffixed literals as `decimal`. Precept's approach is more precise — context-sensitive rather than global — but the pattern of "the evaluator decides, not the author's suffix" is established in production systems.

**Alternatives rejected:** (a) Always parse as `decimal`, convert in number contexts — viable but asymmetric; NCalc uses this approach with its `DecimalAsDefault` flag. (b) Require literal suffixes (`0.1m`, `0.1d`) — too much syntax for domain authors; OData uses suffixes but targets developer-facing query syntax, not business-rule authoring. (c) Always parse as `double` (current broken state) — irrecoverably destroys decimal precision.

**Tradeoff accepted:** The inference mechanism adds complexity to the type checker. The non-ambiguous invariant ensures the complexity is bounded — every literal has exactly one resolution, determined statically. Authors familiar with C# may initially expect unsuffixed `0.1` to be `double` — the DSL's context-sensitive behavior should be documented clearly in the language design doc.

---

## Test Obligations

The test suite must cover the following categories to verify lane integrity:

### Lane Preservation Tests

- **Homogeneous integer arithmetic:** `long + long → long`, `long * long → long`, `long / long → long` (truncating), `long % long → long`
- **Homogeneous decimal arithmetic:** `decimal + decimal → decimal`, `decimal * decimal → decimal`, `decimal / decimal → decimal`, `decimal % decimal → decimal`
- **Homogeneous number arithmetic:** `double + double → double`, etc.
- **Decimal closure canary:** `0.3 - 0.2 - 0.1 == 0.0` in the decimal lane — this is false in IEEE 754 but must be true in Precept's decimal lane. NCalc's `DecimalsTests.cs` uses this exact pattern.
- **Result type assertion:** Tests must assert both the C# runtime type AND the numeric value of every result. `Assert.IsType<decimal>(result)` not `Assert.Equal(0.3, Convert.ToDouble(result))`. Value-only assertions will miss lane regressions where the result is numerically close but in the wrong type. This pattern is established in NCalc's test suite (`DecimalsTests.cs`, `MathTests.cs`).

### Cross-Lane Tests

- **Integer → decimal widening:** `long + decimal → decimal`, value is exact
- **Integer → number widening:** `long + double → double`
- **Decimal → number widening:** `decimal + double → double` (explicit author choice)
- **Number → decimal error:** Assigning `double` result to `decimal` field produces type error
- **Number → integer error:** Assigning `double` result to `integer` field produces type error

### Collection Identity Tests

- **`set<decimal>` stores `decimal`:** Add `0.1m`, retrieve via `.min` → result is `decimal 0.1m`
- **`set<decimal>` comparison is exact:** `set<decimal>` containing `0.1m` and `0.2m` — `contains 0.1` returns `true` via decimal comparison
- **`set<integer>` stores `long`:** `.count` returns `long`, `.min` returns `long`

### Integer Surface Tests

- **`.count` returns `long`:** `Assert.IsType<long>(countResult)`
- **`.length` returns `long`:** `Assert.IsType<long>(lengthResult)`
- **`left()` requires integer:** `left(str, 3.7)` produces type error, not silent truncation

### Assignment Contract Tests

- **Decimal constraint enforcement uses decimal comparison:** `field Price as decimal min 0.01 max 999.99` — constraint checked via `decimal`, not `double`
- **Update path enforces same contract as Fire:** Identical constraint violation produces identical rejection

### Helper Contract Tests

- **`min(decimal, decimal)` returns `decimal`:** Not `double`
- **`max(decimal, decimal)` returns `decimal`:** Not `double`
- **`round(number, 2)` returns `decimal`:** The explicit bridge
- **`clamp(decimal, decimal, decimal)` returns `decimal`:** Stays in lane

### Runtime Boundary Tests

- **JSON `decimal` field receives `decimal`:** `JsonElement.GetDecimal()` path — validated by NodaMoney's `MoneyJsonConverter` pattern (decimal-first parse, no hidden double hop)
- **`double` input to `decimal` field rejected:** `CoerceToDecimal` type error — mirrors Java BigDecimal's `new BigDecimal(double)` footgun prevention
- **API `decimal` round-trip:** Write `decimal 0.1m`, serialize, deserialize → `decimal 0.1m`
- **Decimal overflow produces error:** `decimal.MaxValue + 1` throws `OverflowException`, does NOT silently widen to `double`

---

## Deferred / Out of Scope

The following items are explicitly excluded from issue #115:

- **New DSL syntax.** No new keywords, operators, or expression forms are introduced. The change is behavioral — the evaluator's type contracts change, but the language surface does not.
- **Proof engine decimal intervals.** The proof engine continues to operate on `double` intervals per DD7. A separate `decimal` interval arithmetic layer is not warranted.
- **Literal suffix syntax.** No `0.1m` or `0.1d` suffixes are added to the DSL. Context-sensitive literal typing is the chosen mechanism (DD9).
- **Currency or unit-of-measure types.** Issue #115 establishes the numeric lane foundation. Higher-level business types are tracked separately.
- **`sqrt(decimal)` exact implementation.** The `(decimal)Math.Sqrt((double)d)` pattern is accepted per DD8. A native `decimal` sqrt is not needed.
- **Cross-event computed-field carryover.** This is the proof engine's soundness boundary, not an evaluator concern. Tracked in ProofEngineDesign.md.
- **Backward-compatible migration tooling.** Breaking changes to the runtime API (e.g., `CoerceToDecimal` rejecting `double`) are documented but no automatic migration path is provided in this issue.
