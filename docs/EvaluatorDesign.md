# Expression Evaluator Design

Date: 2026-04-19

Status: **Draft — Track B design for issue #115. Pending design review.**

Research grounding: [research/language/evaluator-architecture-survey.md](../research/language/evaluator-architecture-survey.md) — CEL, FEEL/DMN, C# spec §12.4.7, F#, Kotlin, NCalc, DynamicExpresso, EF Core, OData, NodaMoney, FsCheck precedent survey.

> This document describes the **target state** of the expression evaluator after the numeric lane integrity campaign (issue #115). Sections describing architecture, rules, and contracts are written in "to be" form — as if the target implementation is complete. The "Current State Analysis" section describes what is broken today and why.

---

## Overview

The expression evaluator is Precept's runtime computation layer — the mechanism through which every philosophy commitment becomes operational. It evaluates every DSL expression — guards, rules, ensures, set assignments, computed fields, and conditional branches — against a live entity instance, producing the values that the runtime engine uses to decide whether an operation commits or rejects.

The evaluator provides eight interlocking guarantees that together realize Precept's core philosophy:

| Guarantee | What it means | Philosophy root |
|-----------|---------------|-----------------|
| **Prevention** | Guard, rule, and ensure evaluations produce the boolean gates that structurally prevent invalid configurations | "Invalid configurations cannot exist" |
| **Determinism** | Same expression + same data = same result, across machines, cultures, and time | "The engine is deterministic — same definition, same data, same outcome" |
| **Inspectability** | Every evaluation result is honest, complete, and serializable — powering Inspect and preview without committing anything | "Full inspectability. Nothing is hidden." |
| **Totality** | Every well-typed expression produces a definite value or a definite error — no undefined behavior, no silent NaN/Infinity/null | "Nothing is hidden" |
| **Type contract** | Operators are defined only for their declared operand types — no implicit coercion across type families | "Prevention at the surface, not detection at depth" |
| **Semantic fidelity** | Every value preserves the type identity declared for it — a decimal stays a decimal, an integer stays an integer, lane boundaries are explicit | "Precept does not present approximation as exactness" |
| **Short-circuit correctness** | `and`/`or` short-circuit left-to-right as a semantic guarantee, enabling safe null-guard patterns | Totality + safe guard idioms |
| **Expression isolation** | Expressions are pure — no mutation, no side effects, no observation outside the evaluation context | Foundation of Inspect and rollback |

The evaluator operates over five type families:

| Family | C# backing type | Semantics | DSL keyword |
|--------|-----------------|-----------|-------------|
| **Integer** | `long` | Discrete, countable, exact over the integers | `integer` |
| **Decimal** | `decimal` | Exact base-10, closed over exact arithmetic | `decimal` |
| **Number** | `double` | Approximate IEEE 754 binary64, for inherently inexact operations | `number` |
| **String** | `string` | Immutable text, ordinal comparison | `string` |
| **Boolean** | `bool` | Logical truth value | `boolean` |

A sixth "type" — **Choice** — is backed by `string` at runtime but carries additional semantics: its value is constrained to a declared member set, and the `ordered` constraint enables declaration-position ordinal comparison.

The evaluator supports the following expression forms:

| Form | Examples |
|------|----------|
| **Arithmetic** | `+`, `-`, `*`, `/`, `%` on numeric operands; `+` on strings (concatenation) |
| **Comparison** | `==`, `!=`, `>`, `>=`, `<`, `<=` — dispatch varies by operand type family |
| **Logical** | `and`, `or` (short-circuit), `not` (unary) |
| **Conditional** | `if Condition then ThenBranch else ElseBranch` |
| **Contains** | `Collection contains Expr` — collection membership test |
| **Function call** | `abs`, `floor`, `ceil`, `round`, `truncate`, `min`, `max`, `clamp`, `pow`, `sqrt`, `approximate`, `toLower`, `toUpper`, `trim`, `startsWith`, `endsWith`, `left`, `right`, `mid` |
| **Accessor** | `.count`, `.length`, `.min`, `.max`, `.peek` |

These guarantees are not independent — they form a dependency chain. Expression isolation enables inspectability. Totality enables prevention (a guard that fails to evaluate can't gate anything). Determinism enables inspectability (Inspect is trustworthy only if the preview matches the actual fire). Semantic fidelity enables type contracts (lane boundaries are meaningless if values silently cross them). Together, they realize the philosophy's promise: the engine is deterministic, nothing is hidden, and invalid configurations cannot exist.

---

## Philosophy-Rooted Design Principles

The following principles govern the evaluator's design. They are organized from the most fundamental (those that apply to all types and all expression positions) to the most specific (numeric lane integrity). Each traces to Precept's core philosophy commitments.

### Foundational Guarantees

These principles apply to every expression the evaluator processes, regardless of type family.

1. **The evaluator is the prevention engine.** Every guard, rule, and ensure is an expression that the evaluator resolves to a boolean. That boolean is the gate: `false` means the operation is rejected or the row is skipped. The evaluator does not suggest, warn, or log — it prevents. If a guard evaluates to `false`, the transition row does not fire. If a rule evaluates to `false` after mutation, the entire transition rolls back. The evaluator is what makes "invalid configurations structurally impossible" true at runtime, not just at compile time. *(Philosophy: "Prevention, not detection.")*

2. **Same expression, same data, same result. Always.** The evaluator is fully deterministic. No culture-dependent formatting, no platform-dependent floating-point modes, no observable side effects, no external state. String operations use invariant culture. String comparisons use ordinal semantics. Numeric operations use C#'s deterministic operators. Two evaluations of the same expression against the same entity data produce the same value, on any machine, at any time. *(Philosophy: "The engine is deterministic — same definition, same data, same outcome. Nothing is hidden.")*

3. **Expressions are pure.** Expression evaluation cannot mutate entity state, trigger side effects, or observe anything outside the evaluation context (current field values and event arguments). A guard evaluation does not change the entity. A rule evaluation does not write to a log. A `set` RHS computes a value but does not assign it — the runtime engine performs assignment after the evaluator returns. This purity is what makes Inspect safe: evaluating every guard and rule for every possible event touches nothing. It is also what makes rollback possible: if post-mutation validation fails, nothing outside the entity's proposed state was affected. *(Philosophy: "Full inspectability — preview every possible action without executing anything.")*

4. **Every expression evaluates to a result or a definite error.** There is no undefined behavior, no silent failure, no partial evaluation. The evaluator returns a value or an explicit error message. Division by zero is an error, not `Infinity`. `.peek` on an empty collection is an error, not `null`. `.length` on a null string is an error, not `0`. Silent production of `NaN`, `Infinity`, or unexpected `null` is a bug — these values corrupt downstream evaluations without any visible failure signal. *(Philosophy: "Nothing is hidden.")*

5. **Short-circuit is a semantic guarantee, not an optimization.** `and` evaluates left-to-right; if the left operand is `false`, the right operand is never evaluated. `or` evaluates left-to-right; if the left operand is `true`, the right operand is never evaluated. This enables the safe guard idiom: `Name != null and Name.length >= 2` never evaluates `.length` on null. Short-circuit makes the evaluator's own totality guarantee composable — authors can chain preconditions without nesting, and the evaluator guarantees that guarded sub-expressions are only reached when their preconditions hold. *(Philosophy: totality + safe guard patterns.)*

6. **Operators are defined for their declared types. Nothing else.** `boolean + boolean` is an error. `string > string` is an error. `string * number` is an error. `"42" == 42` is an error. The type checker catches most of these at compile time; the evaluator is the runtime safety net. No implicit coercion across type families — the evaluator does not convert `string` to `number` for comparison, does not treat `0` as `false`, does not auto-convert between incompatible families. If a value reaches an operator with an incompatible type, the evaluator rejects the operation explicitly. *(Philosophy: "Prevention at the surface, not detection at depth.")*

7. **Evaluation results are honest and serializable.** When Inspect, MCP tools, or the preview panel present evaluation results, the serialized form preserves type identity. A `decimal` field serializes with its exact decimal representation. An `integer` field serializes as a JSON integer. A `number` field serializes as a JSON number (IEEE 754). A `string` function always returns `string` (or `boolean` for predicates). The consumer can distinguish types from the serialized output. The evaluator never presents an approximate value as exact or an exact value as approximate. *(Philosophy: "Full inspectability. The engine exposes the complete reasoning.")*

### Numeric Integrity

These principles govern the evaluator's three numeric type families: integer (`long`), decimal (`decimal`), and number (`double`). They are the specific realization of the foundational guarantees applied to Precept's numeric type system.

8. **Exactness is the default for business arithmetic.** The `decimal` lane exists because most business-domain calculations — pricing, tax, fees, balances, rates — require exact base-10 arithmetic. An invoice total of `$100.10` must not become `$100.09999999999999` through an internal IEEE 754 intermediary. The `decimal` backing type provides this guarantee. This is the direct realization of the philosophy's numeric exactness commitment. *(Philosophy: "Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes.")*

9. **Approximation is explicit, not hidden.** The `number` lane exists for inherently approximate operations: `sqrt`, `pow` with non-integer exponents (future), and any domain where IEEE 754 double precision is the correct model. Values in the `number` lane are honestly approximate. The contract does not pretend they are exact. *(Philosophy: "If approximation is part of the domain, the contract must say so plainly.")*

10. **Lane boundaries are type boundaries.** Moving a value from one numeric lane to another is a type-system event, not a silent coercion. Integer→decimal widening is exact and implicit. Integer→number widening is implicit (range-preserving, though precision may narrow for very large integers). Number→decimal is a **type error** unless the author explicitly bridges via `round(value, places)`, `floor(value)`, `ceil(value)`, or `truncate(value)`. Decimal→number crossing is **context-dependent** — the mechanism varies by expression position:

    | Context | Decimal → Number | Mechanism |
    |---------|-----------------|----------|
    | Function argument | Requires `approximate()` | Explicit bridge |
    | Arithmetic operator | Type error | Type checker blocks |
    | Comparison operator | Implicit (already allowed) | Result is `boolean`, no lane contamination |
    | Assignment to number field | Requires `approximate()` | Explicit bridge |

    The key insight: comparisons produce `boolean`, not a stored numeric value, so implicit decimal→number widening in comparisons does not contaminate either lane. Arithmetic and assignment produce stored numeric values, so the author must explicitly acknowledge the precision loss via `approximate()`. *(Philosophy: "that line [between exact and approximate] must be visible in the type system and public surface.")* *(Refined in DD15, issue #115.)*

11. **Prevention at the surface, not detection at depth.** The type checker rejects expressions that would silently cross lane boundaries. A `decimal` field cannot be assigned a `number` expression without explicit conversion. This prevents precision loss by construction — the invalid assignment is never evaluated. *(Philosophy: prevention, not detection.)*

12. **Integer means integer.** Surfaces that produce or consume discrete counts — `.count`, `.length`, collection indices, string slicing parameters — are typed as `integer`, not as generic `number`. The evaluator produces `long` values for these surfaces. Callers with `number` values must normalize explicitly (`floor`, `ceil`, `truncate`, `round`) before crossing into integer-shaped APIs. No silent truncation. *(Philosophy: "Precept should not advertise a broader numeric contract than the runtime meaning actually supports.")*

13. **Determinism across all lanes.** Same expression, same data, same numeric lane, same result. The evaluator does not use culture-dependent formatting, non-deterministic rounding, or platform-dependent floating-point modes. `decimal` arithmetic uses C#'s deterministic `decimal` operators. `double` arithmetic uses IEEE 754 semantics. *(Philosophy: determinism.)*

14. **Inspectability through honest types.** When the MCP `precept_inspect` or `precept_fire` tools serialize instance data, the serialized value preserves lane identity. A `decimal` field serializes as a JSON number with its exact decimal representation. An `integer` field serializes as a JSON integer. A `number` field serializes as a JSON number (IEEE 754). The consumer can distinguish lanes from the serialized output. *(Philosophy: full inspectability, nothing hidden.)*

15. **Two explicit bridges, not many hidden ones.** `round(number, places) → decimal` is the deliberate bridge from the approximate `number` lane into the exact `decimal` lane. `approximate(decimal) → number` is the deliberate bridge from the exact `decimal` lane into the approximate `number` lane. These bridges are symmetric in intent — `round` says "normalize this to N places," `approximate` says "approximate this value." No other implicit or hidden cross-lane paths exist for arithmetic or assignment contexts. *(Locked design note, issue #115. Updated by DD11.)*

16. **Tests assert the contract, not the leak.** Test expectations must match the semantic contract: decimal-lane tests assert `decimal`-typed results, integer-shaped surface tests assert `long` results, and `number`-lane tests assert `double` results. Tests that normalize via `Convert.ToDouble()` and approximate comparisons are themselves part of the semantic drift surface and must be updated alongside the evaluator. *(Locked design note, issue #115.)*

17. **Function lane integrity rule.** A function keeps its decimal overload if and only if the mathematical operation is closed over finite decimals — meaning: decimal input always produces a result exactly representable as a finite decimal. All current functions except `sqrt` satisfy this. `sqrt` is inherently approximate — `sqrt(x)` is irrational for most inputs — so `sqrt` lives exclusively in the number lane. Future functions (`log`, `sin`, `cos`, `exp`, non-integer `pow`) are inherently approximate and would likewise live exclusively in the number lane. The author reaches them via `approximate()`. *(Locked design note, DD16, issue #115.)*

---

## Current State Analysis

The expression evaluator exists and is functionally correct for most expression evaluation. However, it has systemic numeric lane integrity violations that compromise the semantic fidelity guarantee. These violations span the parser, model, type checker, evaluator, and runtime boundary — they are not isolated bugs but a consistent pattern of collapsing distinct numeric lanes through `double`.

### Critical Finding 1: Parser collapses decimal literals to `double`

`PreceptParser.ToNumericLiteralValue()` ([PreceptParser.cs](../src/Precept/Dsl/PreceptParser.cs#L235)) returns `long` for whole-number literals and `double` for fractional/scientific literals. A DSL literal `0.1` in a `decimal` field context becomes `double 0.1` (which is `0.1000000000000000055511151231257827021181583404541015625` in IEEE 754) at parse time. The exact base-10 value is irrecoverably lost before the evaluator ever sees it.

### Critical Finding 2: Field constraints stored as `double`

`FieldConstraint.Min(double Value)` and `FieldConstraint.Max(double Value)` ([PreceptModel.cs](../src/Precept/Dsl/PreceptModel.cs#L94)) store constraint bounds as `double`. A declaration `field Price as decimal min 0.01 max 999.99` stores its bounds as `double`, losing exact decimal representation. The runtime enforces constraints against values that have already been silently approximated.

### Critical Finding 3: Type checker maps `decimal` as `number`

`PreceptTypeChecker.MapLiteralKind()` ([PreceptTypeChecker.Helpers.cs](../src/Precept/Dsl/PreceptTypeChecker.Helpers.cs#L58)) classifies C# `decimal` runtime values as `StaticValueKind.Number`, not `StaticValueKind.Decimal`. This means the type checker cannot distinguish exact decimal values from approximate number values during type inference — the decimal lane is invisible at type-check time.

### Critical Finding 4: Type checker maps `.count` and `.length` as `number`

`BuildSymbolKinds()` ([PreceptTypeChecker.Helpers.cs](../src/Precept/Dsl/PreceptTypeChecker.Helpers.cs#L352)) maps `Collection.count` and `Field.length` to `StaticValueKind.Number`. These are discrete integer surfaces — a collection cannot have 2.5 elements — but the type system treats them as approximate floating-point values.

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

### Critical Finding 11: Unary minus collapses `decimal` to `double`

`EvaluateUnary()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L99)) checks for `long` first, then falls through to `TryToNumber`, which converts `decimal` to `double`. The expression `-Price` where `Price` is `decimal` produces a `double` result, silently leaving the exact lane. This is the same class of bug as the arithmetic operator violations (Critical Finding 5) but applied to the unary negation operator.

### Critical Finding 12: `DecimalPow` throws on zero base with negative exponent

`DecimalPow(0m, -1)` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L580)) computes `1m / DecimalPow(0m, 1)` which triggers `DivideByZeroException`. Unlike every other division path in the evaluator (which guards against zero divisors and returns `EvaluationResult.Fail`), this path throws an unhandled exception. This violates the totality guarantee (Principle 4) — the evaluator must return a definite error, not crash.

### Critical Finding 13: `DecimalPow` throws on large-exponent overflow

`DecimalPow(10m, 29)` and similar large-exponent cases throw an unhandled `OverflowException` when the result exceeds `decimal.MaxValue`. Like Critical Finding 12, this is a totality violation (Principle 4) — the evaluator must catch the overflow and return `EvaluationResult.Fail` with a descriptive error, not propagate the exception. *(Identified in DD14.)*

### Critical Finding 14: `min`/`max` decimal comparison routes through `double`

`ReduceComparable()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L500)) uses `TryToNumber` (which returns `double`) for all comparisons in `min`/`max`, even when all arguments are `decimal`. Two `decimal` values `0.1m` and `0.2m` are compared as `double`, producing correct results in most cases but violating the lane-preservation contract and risking edge-case divergence for values near `decimal`'s precision limits. The fix is a decimal fast-path using native `decimal.CompareTo`, mirroring the existing `long` fast-path. *(Identified in DD13.)*

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
6. **Mixed decimal + number** → **type error for arithmetic** (the type checker blocks `decimal + number`, `decimal * number`, etc.). For **comparison operators** (`==`, `!=`, `<`, `<=`, `>`, `>=`), the decimal operand is widened to `double` and compared in the number lane — this is permitted because comparisons produce `boolean`, not a stored numeric value, so no lane contamination occurs. The author must use `approximate()` to explicitly cross into the number lane for arithmetic or assignment.

Cases 1–5 mirror the C# language specification's binary numeric promotion rules (§12.4.7.3), which **forbid** mixing `decimal` with `float`/`double` in arithmetic — it is a binding-time error. Case 6 follows the same prohibition for arithmetic but permits comparisons, which produce `boolean` and do not store a numeric value in either lane. *(Aligned with DD12 and DD15.)*

The type checker enforces the arithmetic prohibition. The evaluator handles the comparison case correctly when the type checker permits it (e.g., a `number`-typed function result compared with a `decimal` value).

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

approximate(decimal) → double  (explicit bridge: decimal → number)
sqrt(double)         → double  (Math.Sqrt — number lane only)
```

Two explicit bridges connect the lanes:
- `round(number, places) → decimal` is the **explicit normalization bridge** from the approximate `number` lane into the exact `decimal` lane.
- `approximate(decimal) → number` is the **explicit approximation bridge** from the exact `decimal` lane into the approximate `number` lane.

These are symmetric in intent: `round` says "normalize this to N places," `approximate` says "approximate this value." Author pattern: `set adjusted = round(pow(approximate(Price), Rate), 2)`.

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
| `decimal` | Type error | `decimal` (identity) | Type error (requires `approximate()` per DD12/DD15) |
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
- Homogeneous decimal arithmetic (`decimal op decimal` → `decimal`)
- Mixed integer+decimal arithmetic (`long op decimal` → `decimal`)

**Widening rules:**
- Decimal → number: **context-dependent** (see Principle 10). Implicit for comparisons (result is `boolean`, no lane contamination). Requires `approximate()` for arithmetic operands or assignment to number fields. The type checker enforces this.
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
- `approximate(decimal)` → `number` (explicit bridge from exact to approximate lane)
- `abs(double)` → `double`
- `min(double, double, ...)` → `double`
- `max(double, double, ...)` → `double`
- `clamp(double, double, double)` → `double`
- `pow(double, long)` → `double`
- Homogeneous number arithmetic (`double op double` → `double`)
- Mixed integer+number arithmetic (`long op double` → `double`)

**Narrowing rules:**
- Number → decimal: **type error.** The author must use `round(value, places)` to explicitly bridge.
- Number → integer: **type error.** The author must use `floor()`, `ceil()`, `truncate()`, or `round()`.

### Cross-Lane Rules Summary

| From → To | Allowed? | Mechanism | Precision |
|-----------|----------|-----------|-----------|
| Integer → Decimal | Yes (implicit) | `(decimal)longValue` | Exact |
| Integer → Number | Yes (implicit) | `(double)longValue` | Range-preserving (±2⁵³ limit) |
| Decimal → Number | **Context-dependent** | Comparisons: implicit (result is `boolean`). Arithmetic/assignment: requires `approximate()` | Lossy — explicit bridge required for non-comparison contexts |
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
| `decimal` vs `double` | Number (widen decimal) | Approximate — comparisons are the one context where implicit decimal→number widening is permitted (DD15), because the result is `boolean` with no lane contamination |

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
| `sqrt(number)` | `double` | `double` | Stays number | None (correct today) |
| `approximate(decimal)` | `decimal` | `double` | **Explicit bridge**: decimal→number | New function (DD11) |

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

## String Evaluation Contract

### Backing Type

**`string`** — immutable, UTF-16, ordinal semantics. The evaluator treats string values as opaque character sequences. No locale-aware collation is applied at any evaluation point.

### String Concatenation

The `+` operator is overloaded for strings. When both operands are `string`, `+` produces a new `string` via standard .NET concatenation. Mixed-type concatenation (string + number, string + boolean) is a type error — the evaluator does not implicitly coerce non-string operands to string.

### String Equality

`==` and `!=` on string operands use `Object.Equals`, which for strings performs ordinal (byte-by-byte) comparison. This is case-sensitive: `"Draft" != "draft"`.

String is NOT relationally comparable — `>`, `>=`, `<`, `<=` on string operands are type errors (they fall through to the numeric dispatch path, which fails). Only equality operators are defined for plain strings. Relational comparison on string-backed values is available only for `choice` fields with the `ordered` constraint (see § Choice Evaluation Contract).

### `.length` Accessor

`Field.length` returns the UTF-16 code unit count of the string value. This matches .NET's `string.Length` and is O(1). Characters outside the Basic Multilingual Plane (e.g., emoji) count as 2 code units: `"💀".length == 2`.

The `.length` accessor is also available on event argument dotted forms: `EventName.ArgName.length`.

**Target type:** `long` (integer lane, per DD3). Integer-typed `.length` ensures that downstream expressions like `Name.length >= 2` compare via `long`, and that `left(str, Name.length)` passes an integer argument as required.

**Current violation:** `.length` returns `(double)str.Length` — a `double`-typed value. This places string length in the `number` lane instead of the `integer` lane. The same violation applies to the three-level dotted form (`EventName.ArgName.length`). See Current State Analysis, Critical Finding 8.

### Null Handling

`.length` on a `null` value produces an evaluation error (`"Field.length failed: field is null."`), not a silent `0`. The type checker enforces a null guard for nullable string fields — authors must narrow to non-null before accessing `.length`.

If a string function receives a non-string argument where a string is expected, the evaluator produces an evaluation error (e.g., `"toLower() requires a string argument."`). `null` is not coerced to empty string.

### String Functions

| Function | Signature | Return type | Semantics | Culture/comparison |
|----------|-----------|-------------|-----------|-------------------|
| `toLower(string)` | `string → string` | `string` | Lowercase conversion | Invariant culture (`ToLowerInvariant`) |
| `toUpper(string)` | `string → string` | `string` | Uppercase conversion | Invariant culture (`ToUpperInvariant`) |
| `trim(string)` | `string → string` | `string` | Remove leading/trailing whitespace | Unicode whitespace (`String.Trim`) |
| `startsWith(string, string)` | `string, string → boolean` | `bool` | Prefix test | Ordinal comparison (`StringComparison.Ordinal`) |
| `endsWith(string, string)` | `string, string → boolean` | `bool` | Suffix test | Ordinal comparison (`StringComparison.Ordinal`) |
| `left(string, integer)` | `string, long → string` | `string` | First N code units | Count clamped to `[0, string.Length]` |
| `right(string, integer)` | `string, long → string` | `string` | Last N code units | Count clamped to `[0, string.Length]` |
| `mid(string, integer, integer)` | `string, long, long → string` | `string` | Substring from 1-based start, length N | Start and length clamped; out-of-range start returns `""` |

**Culture determinism:** `toLower` and `toUpper` use invariant culture to ensure deterministic results across platforms. No locale-sensitive casing is performed. `startsWith` and `endsWith` use ordinal comparison — no Unicode normalization, no locale-dependent collation.

**`mid` indexing:** The `start` parameter is **1-based** (first character is position 1). Internally, the evaluator subtracts 1 to convert to the 0-based .NET index.

### String Slicing Parameter Type Contract

`left`, `right`, and `mid` count/start/length parameters require `integer` (per DD3). Authors with `number`-typed values must explicitly convert via `floor()`, `ceil()`, or `truncate()` before passing them as slicing parameters.

**Current violation:** `left()`, `right()`, and `mid()` accept any numeric type via `TryToNumber`, then silently truncate to `int` via `(int)countNum`. A call `left(Name, 3.7)` silently becomes `left(Name, 3)` — the fractional part is discarded without warning. See Current State Analysis, Critical Finding 10.

### `contains` on Strings

The `contains` operator is NOT supported for substring testing on strings. `contains` is defined only for collection membership (see § Contains Operator). Substring testing uses `startsWith` and `endsWith`. This is a deliberate surface constraint — a `contains` function for substring search may be added in a future language revision but is not part of the current contract.

---

## Boolean Evaluation Contract

### Backing Type

**`bool`** — C# `System.Boolean`. Boolean values are `true` or `false`. There is no truthy/falsy coercion — the evaluator does not treat `0`, `""`, or `null` as boolean.

### Logical Operators

| Operator | Form | Semantics |
|----------|------|-----------|
| `and` | `Expr and Expr` | Short-circuit conjunction. Evaluates the left operand first; if `false`, returns `false` without evaluating the right operand. Both operands must be `bool` — non-boolean produces an evaluation error. |
| `or` | `Expr or Expr` | Short-circuit disjunction. Evaluates the left operand first; if `true`, returns `true` without evaluating the right operand. Both operands must be `bool`. |
| `not` | `not Expr` | Unary negation. The operand must be `bool`. Returns the logical complement. |

**Short-circuit guarantee:** `and` and `or` are evaluated lazily. The right operand is evaluated only if the left operand does not determine the result. This is semantically significant — a right-side expression that would produce an evaluation error is never reached if the left side short-circuits. This enables guard patterns like `Name != null and Name.length >= 2`.

### Equality

`==` and `!=` on boolean operands use `Object.Equals`. `true == true` is `true`; `true == false` is `false`.

### Relational Operators

Boolean is NOT comparable. `>`, `>=`, `<`, `<=` on boolean operands are type errors. The evaluator's comparison dispatch first checks for `long`, then attempts `TryToNumber` (which does not accept `bool`), then checks for ordered choice — boolean matches none of these, producing an evaluation error.

### Where Booleans Appear

Boolean values are the result type for:
- Guard conditions (`when` clauses in transition rows)
- `if` conditions in conditional expressions
- `rule` and `ensure` constraint expressions
- Comparison operators (`==`, `!=`, `>`, `>=`, `<`, `<=`) — all return `bool`
- Logical operators (`and`, `or`, `not`) — all return `bool`
- `contains` operator — returns `bool`
- `startsWith` and `endsWith` functions — return `bool`

Boolean values are NOT valid operands for:
- Arithmetic operators (`+`, `-`, `*`, `/`, `%`) — type error
- String concatenation — type error (no implicit `ToString`)
- Numeric functions (`abs`, `floor`, `round`, etc.) — type error

---

## Choice Evaluation Contract

### Backing Type

**`string`** — choice values are stored as `string` at runtime. The `choice("A", "B", "C")` declaration constrains the value to a member of the declared set, but the underlying representation is a plain string. This means choice fields participate in string-typed storage, serialization, and collection membership — but they carry additional semantics that the evaluator enforces.

### Equality

`==` and `!=` on choice values use `Object.Equals`, which for the `string` backing type performs ordinal (case-sensitive) comparison. `"Draft" != "draft"`.

Choice equality does not require the `ordered` constraint — all choice fields support `==` and `!=` regardless of ordering.

### Ordered Choice Comparison

Relational operators (`>`, `>=`, `<`, `<=`) are valid on choice fields **only** when the field carries the `ordered` constraint. Comparison uses declaration-position ordinal index from the field's `ChoiceValues` list.

**Mechanism:** When the evaluator encounters a relational operator and both operands are `string`, it calls `TryGetChoiceOrdinals` to attempt ordered choice resolution:

1. The left-side expression must be a `PreceptIdentifierExpression` (a simple identifier, not a dotted form).
2. The identifier must resolve to a `PreceptField` in the `fieldContracts` dictionary.
3. The field must have `Type == PreceptScalarType.Choice` and `IsOrdered == true`.
4. The field must have a non-empty `ChoiceValues` list.
5. Both the left and right operand values (as strings) are looked up in the `ChoiceValues` list by ordinal (`StringComparison.Ordinal`) to find their position indices.
6. The position indices are compared using the requested relational operator.

**Example:** Given `field Priority as choice("low", "medium", "high") ordered`, the ordinal positions are: `"low"` → 0, `"medium"` → 1, `"high"` → 2. The expression `Priority > "low"` evaluates to `true` when `Priority` is `"medium"` or `"high"`.

### Error Cases

- **Value not in ordered set:** If either operand's string value is not found in the `ChoiceValues` list, the evaluator produces an evaluation error: `"'value' is not a member of the ordered choice set."` This is a hard error, not a silent `false`.
- **Unordered choice with relational operator:** If the field lacks the `ordered` constraint, `TryGetChoiceOrdinals` returns `false`, and the evaluator falls through to the numeric comparison path, which fails with `"operator '>' requires numeric operands."` The type checker prevents this at compile time, but the evaluator enforces it as a safety net.
- **Cross-field ordered comparison:** Ordinal rank is field-local. Comparing two different ordered choice fields is meaningless because their orderings are independent.

### Arithmetic on Choice

Choice values are NOT numeric. Arithmetic operators (`+`, `-`, `*`, `/`, `%`) on choice values are type errors — the string backing does not participate in numeric dispatch, and string `+` requires both operands to be `string` (which choice values satisfy syntactically, but the type checker rejects arithmetic on choice-typed fields at compile time).

### Choice in Collections

Choice values in `set<choice(...)>` collections are stored as `string` and compared via ordinal string comparison (not declaration-position ordering). The `ordered` constraint affects only relational operator evaluation, not collection sort order.

---

## Conditional Expression Evaluation

### Evaluation Model

Conditional expressions follow the form:

```
if Condition then ThenBranch else ElseBranch
```

The evaluator processes a conditional expression in three steps:

1. **Evaluate the condition.** The `Condition` sub-expression is evaluated. If evaluation fails, the error propagates immediately.
2. **Type-check the condition result.** The result must be `bool`. If the condition evaluates to a non-boolean value, the evaluator produces an evaluation error: `"conditional expression condition must be a boolean."` There is no truthy/falsy coercion.
3. **Evaluate the selected branch.** If the condition is `true`, only `ThenBranch` is evaluated. If `false`, only `ElseBranch` is evaluated. The unselected branch is never evaluated — this is a short-circuit guarantee, not an optimization.

### Return Type

The return type of a conditional expression is the type of the selected branch. The `ThenBranch` and `ElseBranch` may have different types — the type checker validates compatibility at compile time (both branches must be assignable to the target context's type), but the evaluator returns whichever branch's result is produced at runtime without further coercion.

### Nesting

Conditional expressions nest arbitrarily. The `ThenBranch` or `ElseBranch` may itself be a conditional expression:

```
if Score >= 90 then "high" else if Score >= 50 then "medium" else "low"
```

Each nested conditional follows the same three-step evaluation model. Nesting depth is limited only by the expression AST — there is no artificial depth limit.

### Short-Circuit Significance

The short-circuit guarantee is semantically meaningful, not just a performance concern. An expression like:

```
if Count > 0 then Total / Count else 0
```

relies on the `else` branch NOT evaluating `Total / Count` when `Count` is 0. The evaluator guarantees this — a division-by-zero error is never produced when the condition directs evaluation to the safe branch.

### Field Contracts Propagation

The `fieldContracts` dictionary is propagated through conditional expression evaluation. Both branches have access to field contracts for ordered choice resolution and other contract-dependent evaluation (see § Choice Evaluation Contract).

---

## Contains Operator

### Collection `contains`

The `contains` operator tests collection membership. Its evaluation follows a strict structural contract:

1. **Left side must be a collection identifier.** The left operand must be a `PreceptIdentifierExpression` with no member accessor (no dotted form). If the left side is not a simple identifier, the evaluator produces an error: `"'contains' requires a collection field on the left side."`
2. **Left side must resolve to a `CollectionValue`.** The identifier is looked up in the context via the `__collection__` key prefix. If no collection is found, the evaluator produces an error: `"'<name>' is not a collection field."`
3. **Right side is any expression.** The right operand is evaluated as a normal expression. If evaluation fails, the error propagates.
4. **Membership test.** The evaluated right-side value is tested against the collection via `CollectionValue.Contains()`, which delegates to `CollectionComparer` for type-aware comparison.

**Return type:** `bool` — `true` if the collection contains the value, `false` otherwise.

### Comparison Semantics

`CollectionValue.Contains()` normalizes the test value through `NormalizeValue` and compares using `CollectionComparer`, which dispatches by element type. For element comparison semantics per collection inner type, see § Collection Storage Contract.

**Current violation:** `NormalizeValue` converts all numerics to `double` via `Convert.ToDouble`. This means `set<decimal> contains 0.1` compares `double 0.1` against `double`-stored elements, not `decimal 0.1m` against `decimal` elements. See Current State Analysis, Critical Finding 7.

### String `contains` (NOT Supported)

The `contains` operator is defined **only** for collection membership. It is NOT available as a substring test on string values. An expression like `Name contains "smith"` where `Name` is a `string` field will fail: the evaluator checks for a collection on the left side, finds none, and produces an error.

Substring testing is supported via `startsWith(str, prefix)` and `endsWith(str, suffix)`. A dedicated substring `contains` function is not part of the current language surface.

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
   d. If value is decimal and target is number → type error (author must use `approximate()` to bridge explicitly, per DD12/DD15)
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
| Literal kind inference | Type checker | `PreceptTypeChecker.Helpers.cs` | `MapLiteralKind()` maps `long` → `Integer`, `decimal` → `Decimal`, `double` → `Number`. Context-sensitive literal resolution determines the final type. |
| Symbol table | Type checker | `PreceptTypeChecker.Helpers.cs` | `BuildSymbolKinds()` maps `.count` and `.length` to `StaticValueKind.Integer`. |
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

**Decision:** `round(number, places)` is the sole sanctioned bridge from the approximate `number` lane to the exact `decimal` lane. The symmetric bridge in the other direction is `approximate(decimal) → number` (see DD11). No implicit coercion paths exist for arithmetic or assignment contexts.

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

### DD8: `sqrt(decimal) → decimal` Retains Approximate Internals *(Superseded by DD10)*

**Status: Superseded.** DD10 removes the `sqrt(decimal) → decimal` overload entirely. The rationale below is retained for historical context — it documents the intermediate position that DD10 replaces.

**Original decision:** `sqrt(decimal)` is computed as `(decimal)Math.Sqrt((double)d)`. The result is returned as `decimal` to stay in the decimal lane, but the computation is inherently approximate due to the `double` intermediary.

**Original rationale:** C# does not have a native `decimal` square root function. `Math.Sqrt` operates on `double`. The result is cast back to `decimal`, but precision loss from the `double` intermediary is inherent. This is documented in the function contract — the user knows that `sqrt` on a `decimal` is approximate.

**Why superseded:** DD10 recognized that returning an approximate result as `decimal` pretends approximation is exactness — violating the semantic fidelity guarantee. The correct design is to remove the decimal overload entirely and require authors to use `sqrt(approximate(value))` when they need a square root of a decimal value.

**External precedent:** .NET generic math (`INumber<T>`, .NET 7+) explicitly separates `decimal` from IEEE 754 types — `decimal` participates in `IFloatingPoint` but NOT `IFloatingPointIeee754`. Generic `Sqrt` is defined in terms of IEEE 754 types only. This confirms that `sqrt(decimal)` is inherently a bridge operation in the .NET ecosystem, not a native decimal capability.

**Alternatives rejected:** (a) Make `sqrt(decimal)` return `number` — this would force decimal-lane users into the number lane for a common operation. (b) Implement a Newton's method `decimal` sqrt — overkill for a DSL runtime; the `double` precision (~15 significant digits) is sufficient for all practical business-domain sqrt uses.

**Tradeoff accepted:** `sqrt(decimal)` is the one function where the decimal lane's exactness guarantee is relaxed. The contract is explicit about this.

### DD9: Context-Sensitive Literal Typing with Non-Ambiguous Inference

**Decision:** Fractional literals resolve their type from expression context. Every literal resolves to exactly one type — no ambiguity. Standalone literals default to `decimal`.

**Rationale:** A literal `0.1` in `set Price = 0.1` (where `Price` is `decimal`) should produce `decimal 0.1m`, not `double 0.1`. Context-sensitive typing achieves this without requiring suffix syntax (e.g., `0.1m` vs `0.1d`). Suffix syntax was considered and rejected because it introduces PLT ceremony inappropriate for a business-rule DSL.

**This is a deliberate DSL policy choice, not host-language precedent.** C#, F#, and Kotlin all default unsuffixed fractional literals to `double` and require explicit suffixes (`m`/`M`) for `decimal`. No mainstream programming language uses context-sensitive literal typing for numeric types. However, evaluator libraries provide precedent: NCalc's `DecimalAsDefault` option and DynamicExpresso's configurable literal parsing both support evaluator-wide policies that resolve unsuffixed literals as `decimal`. Precept's approach is more precise — context-sensitive rather than global — but the pattern of "the evaluator decides, not the author's suffix" is established in production systems.

**Alternatives rejected:** (a) Always parse as `decimal`, convert in number contexts — viable but asymmetric; NCalc uses this approach with its `DecimalAsDefault` flag. (b) Require literal suffixes (`0.1m`, `0.1d`) — too much syntax for domain authors; OData uses suffixes but targets developer-facing query syntax, not business-rule authoring. (c) Always parse as `double` (current broken state) — irrecoverably destroys decimal precision.

**Tradeoff accepted:** The inference mechanism adds complexity to the type checker. The non-ambiguous invariant ensures the complexity is bounded — every literal has exactly one resolution, determined statically. Authors familiar with C# may initially expect unsuffixed `0.1` to be `double` — the DSL's context-sensitive behavior should be documented clearly in the language design doc.

### DD10: Remove `sqrt(decimal) → decimal` Overload

**Decision:** The `sqrt(decimal) → decimal` overload is removed. Only `sqrt(number) → number` remains. Authors who need the square root of a decimal value use `sqrt(approximate(value))`.

**Rationale:** `sqrt` is inherently approximate — `sqrt(x)` is irrational for most inputs. The decimal overload (which internally computed `(decimal)Math.Sqrt((double)d)`) pretended approximation was exactness by returning a `decimal`-typed result for an approximate computation. This violates the semantic fidelity guarantee (Principle 7) and the function lane integrity rule (Principle 17). Removing the overload makes the approximation honest — `sqrt` lives exclusively in the number lane, and the author must explicitly cross into it via `approximate()`.

**Supersedes:** DD8 (`sqrt(decimal) → decimal` Retains Approximate Internals).

**Author pattern:** `set adjusted = round(sqrt(approximate(Price)), 2)` — the author explicitly bridges to number, computes the sqrt, and bridges back to decimal with explicit precision.

**Tradeoff accepted:** Authors with decimal fields who need `sqrt` now write `sqrt(approximate(x))` instead of `sqrt(x)`. This adds one function call but eliminates the silent precision lie.

### DD11: `approximate(value)` — Explicit Bridge from Decimal to Number

**Decision:** New DSL function: `approximate(decimal) → number`. This is the explicit bridge from the exact `decimal` lane into the approximate `number` lane.

**Rationale:** Named after business intent — what it does to the value — not after the type system. `approximate()` reads as a verb describing the semantic effect: "approximate this value." Symmetric with `round(value, places)` going the other direction: "round this to N places."

| Direction | Bridge | Reads as |
|-----------|--------|----------|
| number → decimal | `round(value, places)` | "Round this to N places" |
| decimal → number | `approximate(value)` | "Approximate this value" |
| decimal/number → integer | `floor` / `ceil` / `truncate` / `round` | Named by rounding behavior |

**Author pattern:** `set adjusted = round(pow(approximate(Price), Rate), 2)`

**Alternatives rejected:** (a) `toNumber(value)` — names the type system mechanism, not the business intent. (b) `widen(value)` — too abstract; doesn't communicate what happens to the value. (c) Implicit widening — hides the precision loss.

**Tradeoff accepted:** A new function name to learn. The name is self-documenting and appears at every decimal→number boundary, making precision loss visible in the source code.

### DD12: Decimal + Number Arithmetic Is a Type Error

**Decision:** Case 6 in operator dispatch stays as-is: decimal + number arithmetic is blocked by the type checker. The author must use `approximate()` to cross explicitly. Comparisons (`<`, `<=`, `>`, `>=`, `==`, `!=`) remain allowed — they produce `boolean`, not a stored numeric value.

**Rationale:** Arithmetic operators produce a numeric result that would be stored in a field. If `decimal * number` silently produced a `double`, the author's decimal field would silently receive an approximate value. Comparisons produce `boolean` — no numeric value is stored, so no lane is contaminated.

**Alignment:** This is consistent with C# §12.4.7.3 which forbids mixing `decimal` with `float`/`double` in arithmetic. Precept extends the same principle to its type system.

**Tradeoff accepted:** Authors who want `Price * Rate` where `Price` is decimal and `Rate` is number must write `approximate(Price) * Rate` or convert `Rate` to decimal first. This friction is intentional.

### DD13: Fix `min`/`max` Decimal Comparison Path

**Decision:** `ReduceComparable` must add a decimal fast-path using native `decimal` comparison. The current implementation routes decimal values through `double` for comparison.

**Critical Finding:** `ReduceComparable()` uses `TryToNumber` (which returns `double`) for all comparisons, even when all arguments are `decimal`. Two `decimal` values `0.1m` and `0.2m` are compared as `double`, producing correct results in most cases but violating the lane-preservation contract and risking edge-case divergence for values near `decimal`'s precision limits.

**Implementation fix:** Add a `decimal` branch in `ReduceComparable` that detects when all arguments are `decimal` and compares via native `decimal.CompareTo`, bypassing the `TryToNumber` → `double` path entirely. This mirrors the existing `long` fast-path pattern.

**Totality note:** This is a correctness fix, not a new feature. The lane integrity contract already requires decimal-native comparison (see § Helper/Function Type Contracts); the current implementation fails to honor it.

### DD14: Guard `DecimalPow` Edge Cases

**Decision:** Guard `DecimalPow` against two edge cases that currently violate the totality guarantee (Principle 4):

1. **`pow(0, -N)`** — currently computes `1m / DecimalPow(0m, N)` which triggers an unhandled `DivideByZeroException`. Must return `EvaluationResult.Fail` with an error message instead.
2. **`pow(10, 29)` and similar large-exponent cases** — currently throws an unhandled `OverflowException` when the result exceeds `decimal.MaxValue`. Must catch `OverflowException` and return `EvaluationResult.Fail` with an error message instead.

**Critical Finding:** Both cases are totality violations. The evaluator's contract (Principle 4) requires that every expression evaluates to a result or a definite error — not an unhandled exception. The existing Critical Finding 12 documents the `DivideByZeroException` case; this decision extends the fix to also cover `OverflowException`.

**Implementation fix:** Wrap the `DecimalPow` computation in a try-catch that handles both `DivideByZeroException` and `OverflowException`, returning `EvaluationResult.Fail` with descriptive error messages (e.g., `"pow(0, -N) is undefined: division by zero"` and `"pow result exceeds decimal range"`).

### DD15: Principle 10 Refinement — Resolve the Contradiction

**Decision:** Replace the blanket "Decimal→number widening is implicit" claim in Principle 10 with a context-specific rule table.

**Rationale:** The prior Principle 10 stated "Decimal→number widening is implicit." This directly contradicted Case 6 in operator dispatch, which blocks decimal+number arithmetic as a type error. The contradiction existed because comparisons and arithmetic were conflated — comparisons produce `boolean` (no lane contamination), while arithmetic produces stored numeric values (lane contamination).

**Refined rule:**

| Context | Decimal → Number | Mechanism |
|---------|-----------------|-----------|
| Function argument | Requires `approximate()` | Explicit bridge |
| Arithmetic operator | Type error | Type checker blocks |
| Comparison operator | Implicit (already allowed) | Result is `boolean`, no lane contamination |
| Assignment to number field | Requires `approximate()` | Explicit bridge |

**Tradeoff accepted:** The rule is now context-dependent rather than a simple "implicit/explicit" binary. This adds nuance but eliminates the contradiction.

### DD16: Function Lane Integrity Rule

**Decision:** Establish a governing rule for which functions get decimal overloads:

> A function keeps its decimal overload if and only if the mathematical operation is closed over finite decimals — meaning: decimal input always produces a result exactly representable as a finite decimal.

**Current function audit:**
- **Satisfy the rule (keep decimal overload):** `abs`, `floor`, `ceil`, `truncate`, `round`, `min`, `max`, `clamp`, `pow(decimal, integer)` — all produce exact decimal results from decimal inputs.
- **Violate the rule (number-only):** `sqrt` — `sqrt(x)` is irrational for most inputs. Removed per DD10.
- **Future functions (number-only by this rule):** `log`, `sin`, `cos`, `exp`, non-integer `pow` — all inherently approximate. They would live exclusively in the number lane. Authors reach them via `approximate()`.

**Rationale:** This rule makes the decimal/number boundary principled rather than ad-hoc. Instead of evaluating each new function individually, the rule provides a clear test: "Is the operation closed over finite decimals?" If yes, decimal overload. If no, number-only. The `approximate()` bridge provides the escape hatch.

**Tradeoff accepted:** Some useful operations on decimal values (like `sqrt`) require an extra `approximate()` call. This is the correct trade — the alternative is pretending approximation is exactness.

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
- **Decimal + number arithmetic error:** `decimal + double` produces type error (DD12)
- **Decimal → number via `approximate()`:** `approximate(decimal)` produces `double`
- **Decimal vs number comparison allowed:** `decimal < double` produces `boolean` (implicit widening for comparisons, DD15)
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

- **New DSL syntax (beyond `approximate()`).** The `approximate()` function (DD11) is the sole new language surface addition. No new keywords, operators, or expression forms beyond this are introduced.
- **Proof engine decimal intervals.** The proof engine continues to operate on `double` intervals per DD7. A separate `decimal` interval arithmetic layer is not warranted.
- **Literal suffix syntax.** No `0.1m` or `0.1d` suffixes are added to the DSL. Context-sensitive literal typing is the chosen mechanism (DD9).
- **Currency or unit-of-measure types.** Issue #115 establishes the numeric lane foundation. Higher-level business types are tracked separately.
- **`sqrt(decimal)` overload.** Removed per DD10. The `sqrt` function is number-only. Authors use `sqrt(approximate(value))` for decimal inputs.
- **Cross-event computed-field carryover.** This is the proof engine's soundness boundary, not an evaluator concern. Tracked in ProofEngineDesign.md.
- **Backward-compatible migration tooling.** Breaking changes to the runtime API (e.g., `CoerceToDecimal` rejecting `double`) are documented but no automatic migration path is provided in this issue.
