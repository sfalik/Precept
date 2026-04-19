# Expression Evaluator Design

Date: 2026-04-19

Status: **Draft — Track B design for Evaluator redesign: semantic fidelity and lane integrity (issue #115). Pending design review.**

Research grounding: [research/language/evaluator-architecture-survey.md](../research/language/evaluator-architecture-survey.md) — CEL, FEEL/DMN, C# spec §12.4.7, F#, Kotlin, NCalc, DynamicExpresso, EF Core, OData, NodaMoney, FsCheck precedent survey.

> This document specifies the expression evaluator as designed for the Evaluator redesign: semantic fidelity and lane integrity (issue #115). Architecture, rules, and contracts are written as target-state specifications. Appendix A documents the gap between the current implementation and this specification.

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

---

## Design Principles

The following principles govern the evaluator's design. They are organized from the most fundamental (those that apply to all types and all expression positions) to the most specific (numeric lane integrity). Each traces to Precept's core philosophy commitments.

### Foundational Guarantees

These principles apply to every expression the evaluator processes, regardless of type family.

1. **The evaluator is the prevention engine.** Every guard, rule, and ensure is an expression that the evaluator resolves to a boolean. That boolean is the gate: `false` means the operation is rejected or the row is skipped. The evaluator does not suggest, warn, or log — it prevents. If a guard evaluates to `false`, the transition row does not fire. If a rule evaluates to `false` after mutation, the entire transition rolls back. The evaluator is what makes "invalid configurations structurally impossible" true at runtime, not just at compile time. *(Philosophy: "Prevention, not detection.")*

2. **Same expression, same data, same result. Always.** The evaluator is fully deterministic. No culture-dependent formatting, no platform-dependent floating-point modes, no observable side effects, no external state. String operations use invariant culture. String comparisons use ordinal semantics. Numeric operations use C#'s deterministic operators. Two evaluations of the same expression against the same entity data produce the same value, on any machine, at any time. *(Philosophy: "The engine is deterministic — same definition, same data, same outcome. Nothing is hidden.")*

3. **Expressions are pure.** Expression evaluation cannot mutate entity state, trigger side effects, or observe anything outside the evaluation context (current field values and event arguments). A guard evaluation does not change the entity. A rule evaluation does not write to a log. A `set` RHS computes a value but does not assign it — the runtime engine performs assignment after the evaluator returns. This purity is what makes Inspect safe: evaluating every guard and rule for every possible event touches nothing. It is also what makes rollback possible: if post-mutation validation fails, nothing outside the entity's proposed state was affected. *(Philosophy: "Full inspectability — preview every possible action without executing anything.")*

4. **Every expression evaluates to a result or a definite error.** There is no undefined behavior, no silent failure, no partial evaluation. The evaluator returns a value or an explicit error message. Division by zero is an error, not `Infinity`. `.peek` on an empty collection is an error, not `null`. `.length` on a null string is an error, not `0`. Silent production of `NaN`, `Infinity`, or unexpected `null` is a bug — these values corrupt downstream evaluations without any visible failure signal. *(Philosophy: "Nothing is hidden.")*

5. **Short-circuit is a semantic guarantee, not an optimization.** `and` evaluates left-to-right; if the left operand is `false`, the right operand is never evaluated. `or` evaluates left-to-right; if the left operand is `true`, the right operand is never evaluated. This enables the safe guard idiom: `Name != null and Name.length >= 2` never evaluates `.length` on null. Short-circuit makes the evaluator's own totality guarantee composable — authors can chain preconditions without nesting, and the evaluator guarantees that guarded sub-expressions are only reached when their preconditions hold. *(Philosophy: totality + safe guard patterns.)*

6. **Operators are defined for their declared types. Nothing else.** `boolean + boolean` is an error. `string > string` is an error. `string * number` is an error. `"42" == 42` is an error. The type checker catches all of these at compile time — a precept that compiles without diagnostics will not produce type errors at runtime (see Principle 18). The evaluator's runtime type checks are defensive assertions (belt-and-suspenders), not the primary enforcement layer. No implicit coercion across type families — the evaluator does not convert `string` to `number` for comparison, does not treat `0` as `false`, does not auto-convert between incompatible families. If a value reaches an operator with an incompatible type, the evaluator rejects the operation explicitly — but this indicates a bug in the type checker, not expected runtime behavior. *(Philosophy: "Prevention at the surface, not detection at depth.")*

7. **Evaluation results are honest and serializable.** Within the runtime, evaluation results preserve type identity — a `decimal` field holds `decimal`, an `integer` field holds `long`, a `number` field holds `double`. A `string` function always returns `string` (or `boolean` for predicates). The evaluator never presents an approximate value as exact or an exact value as approximate. Serialized output uses standard JSON — consumers recover lane identity from the precept definition, not from the serialized value (see Principle 14, DD17). *(Philosophy: "Full inspectability. The engine exposes the complete reasoning.")*

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

14. **Inspectability through honest types.** The runtime preserves lane identity — a `decimal` stays `decimal`, an `integer` stays `long`, a `number` stays `double` throughout every evaluation step. Serialized output uses standard JSON — no typed wrappers, no lane metadata. JSON numbers lose lane identity; consumers recover lane information from the precept definition (`precept_compile` output). Data outside Precept is outside Precept's guarantees. *(Philosophy: full inspectability within the runtime. Standard mechanisms for serialization — see DD17.)*

15. **Two explicit bridges, not many hidden ones.** `round(number, places) → decimal` is the deliberate bridge from the approximate `number` lane into the exact `decimal` lane. `approximate(decimal) → number` is the deliberate bridge from the exact `decimal` lane into the approximate `number` lane. These bridges are symmetric in intent — `round` says "normalize this to N places," `approximate` says "approximate this value." No other implicit or hidden cross-lane paths exist for arithmetic or assignment contexts. *(Locked design note, issue #115. Updated by DD11.)*

16. **Tests assert the contract, not the leak.** Test expectations must match the semantic contract: decimal-lane tests assert `decimal`-typed results, integer-shaped surface tests assert `long` results, and `number`-lane tests assert `double` results. Tests that normalize via `Convert.ToDouble()` and approximate comparisons are themselves part of the semantic drift surface and must be updated alongside the evaluator. *(Locked design note, issue #115.)*

17. **Function lane integrity rule.** A function keeps its decimal overload if and only if the mathematical operation is closed over finite decimals — meaning: decimal input always produces a result exactly representable as a finite decimal. All current functions except `sqrt` satisfy this. `sqrt` is inherently approximate — `sqrt(x)` is irrational for most inputs — so `sqrt` lives exclusively in the number lane. Future functions (`log`, `sin`, `cos`, `exp`, non-integer `pow`) are inherently approximate and would likewise live exclusively in the number lane. The author reaches them via `approximate()`. *(Locked design note, DD16, issue #115.)*

18. **Static completeness.** If a precept compiles without diagnostics, it will not produce type errors at runtime. The type checker and proof engine together must statically reject every expression that could produce a type error — they are the primary enforcement layer. The evaluator's runtime type checks are defensive redundancy (belt-and-suspenders), not the safety net. If a type error reaches the evaluator at runtime, that is a bug in the type checker, not expected evaluator behavior. The only legitimate runtime failures are: overflow (checked integer arithmetic — DD19), `DecimalPow` edge cases (division by zero, decimal overflow — DD14), and guard/rule business logic producing `false`. This principle reframes the contract throughout: the type checker does not catch "most" type errors — it catches ALL of them. *(Locked design note, issue #115.)*

---

## Numeric Type System

The evaluator operates on three numeric type families, each backed by a distinct C# type. Values never change backing type silently — lane identity is preserved through every evaluation step. *(Established in DD1.)*

| Family | C# type | Precision | Semantic role |
|--------|---------|-----------|---------------|
| **Integer** | `long` | Exact over ±9.2×10¹⁸ | Discrete counts, indices, ordinals. `.count`, `.length`, and all integer-declared fields. |
| **Decimal** | `decimal` | Exact base-10, 28–29 significant digits | Business arithmetic: money, rates, percentages, tax, any domain where `0.1 + 0.2 == 0.3` must be true. |
| **Number** | `double` | IEEE 754 binary64, ~15–17 significant digits | Approximate calculations: `sqrt`, scientific computation, any domain where approximate is acceptable. |

**Backing type is identity.** A value's C# runtime type determines its lane. `long` → integer lane. `decimal` → decimal lane. `double` → number lane. There is no metadata tag — the CLR type *is* the lane.

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

**Closure guarantee:** Homogeneous decimal expressions — where all operands are `decimal` — remain in the decimal lane through every intermediate step. No intermediate value is ever computed as `double`. This is the core semantic fidelity guarantee for business arithmetic. *(Established in DD2.)*

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

### Cross-Lane Rules

| From → To | Allowed? | Mechanism | Precision |
|-----------|----------|-----------|-----------|
| Integer → Decimal | Yes (implicit) | `(decimal)longValue` | Exact |
| Integer → Number | Yes (implicit) | `(double)longValue` | Range-preserving (±2⁵³ limit) |
| Decimal → Number | **Context-dependent** | Comparisons: implicit (result is `boolean`). Arithmetic/assignment: requires `approximate()` | Lossy — explicit bridge required for non-comparison contexts |
| Decimal → Integer | **No** | Requires `floor`/`ceil`/`truncate`/`round` | N/A |
| Number → Decimal | **No** | Requires `round(value, places)` | Normalized to authored precision |
| Number → Integer | **No** | Requires `floor`/`ceil`/`truncate`/`round` | N/A |

### Business-Domain Comparison Contract

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

## Operator Dispatch

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

---

## Bridge Functions

Two explicit bridges connect the numeric lanes, plus four rounding bridges that normalize numeric values to integers. No other implicit or hidden cross-lane paths exist for arithmetic or assignment contexts. *(Established in DD5 and DD11.)*

| Direction | Bridge | Reads as |
|-----------|--------|----------|
| number → decimal | `round(value, places)` | "Round this to N places" |
| decimal → number | `approximate(value)` | "Approximate this value" |
| decimal/number → integer | `floor` / `ceil` / `truncate` / `round` (1-arg) | Named by rounding behavior |

### `approximate(decimal) → number`

`approximate(decimal) → number` is the **explicit approximation bridge** from the exact `decimal` lane into the approximate `number` lane. *(Established in DD11.)* The function is named after business intent — what it does to the value — not after the type system. `approximate()` reads as a verb describing the semantic effect: "approximate this value."

### `round(value, places) → decimal`

`round(number, places) → decimal` is the **explicit normalization bridge** from the approximate `number` lane into the exact `decimal` lane. *(Established in DD5.)* The bridge does not "recover" exactness — it produces a `decimal` value normalized to authored precision. The distinction must be clear in the contract.

### Integer Bridges: `floor`, `ceil`, `truncate`, `round` (1-arg)

These functions bridge from `decimal` or `number` into the `integer` lane:

```
floor(decimal) → long     (Math.Floor → cast to long)
floor(double)  → long     (Math.Floor → cast to long)
ceil(decimal)  → long     (ceiling)
ceil(double)   → long     (ceiling)
truncate(decimal) → long  (toward zero)
truncate(double)  → long  (toward zero)
round(decimal) → long     (1-arg: banker's rounding → cast to long)
round(double)  → long     (1-arg: banker's rounding → cast to long)
```

These are symmetric in intent with the lane-crossing bridges: `round` says "normalize this to N places" (2-arg) or "round to nearest integer" (1-arg), `approximate` says "approximate this value." **Author pattern:** `set adjusted = round(pow(approximate(Price), Rate), 2)` — the author explicitly bridges to number, computes the power, and bridges back to decimal with explicit precision.

---

## Function Contracts

Built-in functions dispatch to lane-specific implementations. Each function has overloads registered in `FunctionRegistry` ([FunctionRegistry.cs](../src/Precept/Dsl/FunctionRegistry.cs)) with explicit input and output types. The evaluator matches the runtime type of the argument to select the correct overload body.

A function keeps its decimal overload if and only if the mathematical operation is closed over finite decimals — meaning: decimal input always produces a result exactly representable as a finite decimal. *(Function lane integrity rule — Principle 17, established in DD16.)*

### Concise Reference

```
abs(long)    → long       (Math.Abs)
abs(decimal) → decimal    (Math.Abs)
abs(double)  → double     (Math.Abs)

min(long, long, ...)       → long     (comparison via long)
min(decimal, decimal, ...) → decimal  (comparison via decimal)
min(double, double, ...)   → double   (comparison via double)

approximate(decimal) → double  (explicit bridge: decimal → number)
sqrt(double)         → double  (Math.Sqrt — number lane only)
```

### Numeric Functions

| Function | Input type(s) | Output type | Lane rule |
|----------|---------------|-------------|-----------|
| `abs(integer)` | `long` | `long` | Stays integer |
| `abs(decimal)` | `decimal` | `decimal` | Stays decimal |
| `abs(number)` | `double` | `double` | Stays number |
| `floor(decimal)` | `decimal` | `long` | Decimal → integer (explicit rounding) |
| `floor(number)` | `double` | `long` | Number → integer (explicit rounding) |
| `ceil(decimal)` | `decimal` | `long` | Decimal → integer (explicit rounding) |
| `ceil(number)` | `double` | `long` | Number → integer (explicit rounding) |
| `truncate(decimal)` | `decimal` | `long` | Decimal → integer (explicit rounding) |
| `truncate(number)` | `double` | `long` | Number → integer (explicit rounding) |
| `round(integer)` | `long` | `long` | Identity (no-op) |
| `round(decimal)` | `decimal` | `long` | Decimal → integer (banker's rounding) |
| `round(number)` | `double` | `long` | Number → integer (banker's rounding) |
| `round(any, places)` | any numeric, `long` | `decimal` | **Explicit bridge**: number→decimal normalization *(DD5)* |
| `min(integer, ...)` | `long, ...` | `long` | Stays integer |
| `min(decimal, ...)` | `decimal, ...` | `decimal` | Stays decimal |
| `min(number, ...)` | `double, ...` | `double` | Stays number |
| `max(integer, ...)` | `long, ...` | `long` | Stays integer |
| `max(decimal, ...)` | `decimal, ...` | `decimal` | Stays decimal |
| `max(number, ...)` | `double, ...` | `double` | Stays number |
| `clamp(integer, ...)` | `long, long, long` | `long` | Stays integer |
| `clamp(decimal, ...)` | `decimal, decimal, decimal` | `decimal` | Stays decimal |
| `clamp(number, ...)` | `double, double, double` | `double` | Stays number |
| `pow(integer, integer)` | `long, long` | `long` | Stays integer |
| `pow(decimal, integer)` | `decimal, long` | `decimal` | Stays decimal |
| `pow(number, integer)` | `double, long` | `double` | Stays number |
| `sqrt(number)` | `double` | `double` | Number lane only *(DD10, DD16)* |
| `approximate(decimal)` | `decimal` | `double` | **Explicit bridge**: decimal→number *(DD11)* |

### String Functions

| Function | Input type(s) | Output type | Lane rule |
|----------|---------------|-------------|-----------|
| `left(string, integer)` | `string, long` | `string` | Count param must be integer *(DD3)* |
| `right(string, integer)` | `string, long` | `string` | Count param must be integer *(DD3)* |
| `mid(string, integer, integer)` | `string, long, long` | `string` | Start and length must be integer *(DD3)* |
| `toLower(string)` | `string` | `string` | N/A |
| `toUpper(string)` | `string` | `string` | N/A |
| `trim(string)` | `string` | `string` | N/A |
| `startsWith(string, string)` | `string, string` | `boolean` | N/A |
| `endsWith(string, string)` | `string, string` | `boolean` | N/A |

---

## String / Boolean / Choice / Conditional / Contains Evaluation

### String Evaluation Contract

#### Backing Type

**`string`** — immutable, UTF-16, ordinal semantics. The evaluator treats string values as opaque character sequences. No locale-aware collation is applied at any evaluation point.

#### String Concatenation

The `+` operator is overloaded for strings. When both operands are `string`, `+` produces a new `string` via standard .NET concatenation. Mixed-type concatenation (string + number, string + boolean) is a type error — the evaluator does not implicitly coerce non-string operands to string.

#### String Equality

`==` and `!=` on string operands use `Object.Equals`, which for strings performs ordinal (byte-by-byte) comparison. This is case-sensitive: `"Draft" != "draft"`.

String is NOT relationally comparable — `>`, `>=`, `<`, `<=` on string operands are type errors (they fall through to the numeric dispatch path, which fails). Only equality operators are defined for plain strings. Relational comparison on string-backed values is available only for `choice` fields with the `ordered` constraint (see § Choice Evaluation Contract).

#### `.length` Accessor

`Field.length` returns the UTF-16 code unit count of the string value as `long` (integer lane). *(Established in DD3.)* This matches .NET's `string.Length` and is O(1). Characters outside the Basic Multilingual Plane (e.g., emoji) count as 2 code units: `"💀".length == 2`.

The `.length` accessor is also available on event argument dotted forms: `EventName.ArgName.length`.

Integer-typed `.length` ensures that downstream expressions like `Name.length >= 2` compare via `long`, and that `left(str, Name.length)` passes an integer argument as required.

#### Null Handling

`.length` on a `null` value produces an evaluation error (`"Field.length failed: field is null."`), not a silent `0`. The type checker enforces a null guard for nullable string fields — authors must narrow to non-null before accessing `.length`.

If a string function receives a non-string argument where a string is expected, the evaluator produces an evaluation error (e.g., `"toLower() requires a string argument."`). `null` is not coerced to empty string.

#### String Functions

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

#### String Slicing Parameter Type Contract

`left`, `right`, and `mid` count/start/length parameters require `integer` (per DD3). Authors with `number`-typed values must explicitly convert via `floor()`, `ceil()`, or `truncate()` before passing them as slicing parameters.

#### `contains` on Strings

The `contains` operator is NOT supported for substring testing on strings. `contains` is defined only for collection membership (see § Contains Operator). Substring testing uses `startsWith` and `endsWith`. This is a deliberate surface constraint — a `contains` function for substring search may be added in a future language revision but is not part of the current contract.

### Boolean Evaluation Contract

#### Backing Type

**`bool`** — C# `System.Boolean`. Boolean values are `true` or `false`. There is no truthy/falsy coercion — the evaluator does not treat `0`, `""`, or `null` as boolean.

#### Logical Operators

| Operator | Form | Semantics |
|----------|------|-----------|
| `and` | `Expr and Expr` | Short-circuit conjunction. Evaluates the left operand first; if `false`, returns `false` without evaluating the right operand. Both operands must be `bool` — non-boolean produces an evaluation error. |
| `or` | `Expr or Expr` | Short-circuit disjunction. Evaluates the left operand first; if `true`, returns `true` without evaluating the right operand. Both operands must be `bool`. |
| `not` | `not Expr` | Unary negation. The operand must be `bool`. Returns the logical complement. |

**Short-circuit guarantee:** `and` and `or` are evaluated lazily. The right operand is evaluated only if the left operand does not determine the result. This is semantically significant — a right-side expression that would produce an evaluation error is never reached if the left side short-circuits. This enables guard patterns like `Name != null and Name.length >= 2`.

#### Equality

`==` and `!=` on boolean operands use `Object.Equals`. `true == true` is `true`; `true == false` is `false`.

#### Relational Operators

Boolean is NOT comparable. `>`, `>=`, `<`, `<=` on boolean operands are type errors. The evaluator's comparison dispatch first checks for `long`, then attempts `TryToNumber` (which does not accept `bool`), then checks for ordered choice — boolean matches none of these, producing an evaluation error.

#### Where Booleans Appear

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

### Choice Evaluation Contract

#### Backing Type

**`string`** — choice values are stored as `string` at runtime. The `choice("A", "B", "C")` declaration constrains the value to a member of the declared set, but the underlying representation is a plain string. This means choice fields participate in string-typed storage, serialization, and collection membership — but they carry additional semantics that the evaluator enforces.

#### Equality

`==` and `!=` on choice values use `Object.Equals`, which for the `string` backing type performs ordinal (case-sensitive) comparison. `"Draft" != "draft"`.

Choice equality does not require the `ordered` constraint — all choice fields support `==` and `!=` regardless of ordering.

#### Ordered Choice Comparison

Relational operators (`>`, `>=`, `<`, `<=`) are valid on choice fields **only** when the field carries the `ordered` constraint. Comparison uses declaration-position ordinal index from the field's `ChoiceValues` list.

**Mechanism:** When the evaluator encounters a relational operator and both operands are `string`, it calls `TryGetChoiceOrdinals` to attempt ordered choice resolution:

1. The left-side expression must be a `PreceptIdentifierExpression` (a simple identifier, not a dotted form).
2. The identifier must resolve to a `PreceptField` in the `fieldContracts` dictionary.
3. The field must have `Type == PreceptScalarType.Choice` and `IsOrdered == true`.
4. The field must have a non-empty `ChoiceValues` list.
5. Both the left and right operand values (as strings) are looked up in the `ChoiceValues` list by ordinal (`StringComparison.Ordinal`) to find their position indices.
6. The position indices are compared using the requested relational operator.

**Example:** Given `field Priority as choice("low", "medium", "high") ordered`, the ordinal positions are: `"low"` → 0, `"medium"` → 1, `"high"` → 2. The expression `Priority > "low"` evaluates to `true` when `Priority` is `"medium"` or `"high"`.

#### Error Cases

- **Value not in ordered set:** If either operand's string value is not found in the `ChoiceValues` list, the evaluator produces an evaluation error: `"'value' is not a member of the ordered choice set."` This is a hard error, not a silent `false`. **Compile-time validation (DD22):** The type checker validates that string literals used in ordered choice comparisons (e.g., `Priority > "Urgent"`) are members of the declared choice set. This extends the existing C68 pattern (which validates choice members in `set` assignments) to comparison contexts. When the literal is a non-member, the type checker rejects the expression at compile time — the runtime error path is defensive redundancy only (Principle 18).
- **Unordered choice with relational operator:** If the field lacks the `ordered` constraint, `TryGetChoiceOrdinals` returns `false`, and the evaluator falls through to the numeric comparison path, which fails with `"operator '>' requires numeric operands."` The type checker prevents this at compile time; the evaluator's check is defensive redundancy (see Principle 18).
- **Cross-field ordered comparison:** Ordinal rank is field-local. Comparing two different ordered choice fields is meaningless because their orderings are independent.

#### Arithmetic on Choice

Choice values are NOT numeric. Arithmetic operators (`+`, `-`, `*`, `/`, `%`) on choice values are type errors — the string backing does not participate in numeric dispatch, and string `+` requires both operands to be `string` (which choice values satisfy syntactically, but the type checker rejects arithmetic on choice-typed fields at compile time).

#### Choice in Collections

Choice values in `set<choice(...)>` collections are stored as `string` and compared via ordinal string comparison (not declaration-position ordering). The `ordered` constraint affects only relational operator evaluation, not collection sort order.

### Conditional Expression Evaluation

#### Evaluation Model

Conditional expressions follow the form:

```
if Condition then ThenBranch else ElseBranch
```

The evaluator processes a conditional expression in three steps:

1. **Evaluate the condition.** The `Condition` sub-expression is evaluated. If evaluation fails, the error propagates immediately.
2. **Type-check the condition result.** The result must be `bool`. If the condition evaluates to a non-boolean value, the evaluator produces an evaluation error: `"conditional expression condition must be a boolean."` There is no truthy/falsy coercion.
3. **Evaluate the selected branch.** If the condition is `true`, only `ThenBranch` is evaluated. If `false`, only `ElseBranch` is evaluated. The unselected branch is never evaluated — this is a short-circuit guarantee, not an optimization.

#### Return Type

The return type of a conditional expression is the type of the selected branch. The `ThenBranch` and `ElseBranch` may have different types — the type checker validates compatibility at compile time (both branches must be assignable to the target context's type), but the evaluator returns whichever branch's result is produced at runtime without further coercion.

#### Nesting

Conditional expressions nest arbitrarily. The `ThenBranch` or `ElseBranch` may itself be a conditional expression:

```
if Score >= 90 then "high" else if Score >= 50 then "medium" else "low"
```

Each nested conditional follows the same three-step evaluation model. Nesting depth is limited only by the expression AST — there is no artificial depth limit.

#### Short-Circuit Significance

The short-circuit guarantee is semantically meaningful, not just a performance concern. An expression like:

```
if Count > 0 then Total / Count else 0
```

relies on the `else` branch NOT evaluating `Total / Count` when `Count` is 0. The evaluator guarantees this — a division-by-zero error is never produced when the condition directs evaluation to the safe branch.

#### Field Contracts Propagation

The `fieldContracts` dictionary is propagated through conditional expression evaluation. Both branches have access to field contracts for ordered choice resolution and other contract-dependent evaluation (see § Choice Evaluation Contract).

### Contains Operator

#### Collection `contains`

The `contains` operator tests collection membership. Its evaluation follows a strict structural contract:

1. **Left side must be a collection identifier.** The left operand must be a `PreceptIdentifierExpression` with no member accessor (no dotted form). If the left side is not a simple identifier, the evaluator produces an error: `"'contains' requires a collection field on the left side."`
2. **Left side must resolve to a `CollectionValue`.** The identifier is looked up in the context via the `__collection__` key prefix. If no collection is found, the evaluator produces an error: `"'<name>' is not a collection field."`
3. **Right side is any expression.** The right operand is evaluated as a normal expression. If evaluation fails, the error propagates.
4. **Membership test.** The evaluated right-side value is tested against the collection via `CollectionValue.Contains()`, which delegates to `CollectionComparer` for type-aware comparison.

**Return type:** `bool` — `true` if the collection contains the value, `false` otherwise.

#### Comparison Semantics

`CollectionValue.Contains()` normalizes the test value through `NormalizeValue` and compares using `CollectionComparer`, which dispatches by element type. For element comparison semantics per collection inner type, see § Collection Storage Contract.

#### String `contains` (NOT Supported)

The `contains` operator is defined **only** for collection membership. It is NOT available as a substring test on string values. An expression like `Name contains "smith"` where `Name` is a `string` field will fail: the evaluator checks for a collection on the left side, finds none, and produces an error.

Substring testing is supported via `startsWith(str, prefix)` and `endsWith(str, suffix)`. A dedicated substring `contains` function is not part of the current language surface.

---

## Collection Storage Contract

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

**Emptiness guard requirement (DD21):** `.min`, `.max`, and `.peek` require a conditional guard — the type checker rejects bare accessor use outside a conditional expression whose condition tests `.count > 0`. The idiomatic pattern is: `if Items.count > 0 then Items.min else 0`. This ensures that compiled precepts never produce a runtime error from accessing an empty collection (Principle 18 — static completeness). See DD21 for rationale and alternatives considered.

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

## Assignment & Coercion

### Unified Write Contract

Every write path — `set` assignments in Fire transition rows, field updates in Update, and computed-field recomputation — enforces the same contract:

1. **Evaluate the RHS expression** → produces a value with a C# type
2. **Verify lane compatibility** with the target field's declared type
3. **Coerce if necessary** (integer→decimal widening only; no approximate→exact coercion)
4. **Enforce field constraints** (min, max, nonnegative, positive, maxplaces) against the lane-native value
5. **Store the value** in the target field's declared C# type

The constraint enforcement step operates on lane-native values. A `decimal` field's `min` constraint is compared via `decimal` comparison. An `integer` field's `max` constraint is compared via `long` comparison. No double intermediary.

#### Detailed Lane Compatibility Rules

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

**Key rule: `double` → `decimal` is a type error at the runtime boundary.** *(Established in DD6.)* External callers must provide `decimal`-typed values for `decimal` fields. This prevents approximate values from silently entering the exact lane. The JSON deserializer uses `JsonElement.GetDecimal()` for decimal-typed fields, not `GetDouble()`.

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
| MCP serialization | MCP tools | `tools/Precept.Mcp/Tools/` | Instance data serialization uses standard JSON. Lane identity is runtime-scoped — consumers recover lane info from the precept definition (DD17, DD18). |

---

## Deferred / Out of Scope

The following items are explicitly excluded from the Evaluator redesign: semantic fidelity and lane integrity (issue #115):

- **New DSL syntax (beyond `approximate()`).** The `approximate()` function (DD11) is the sole new language surface addition. No new keywords, operators, or expression forms beyond this are introduced.
- **Proof engine decimal intervals.** The proof engine continues to operate on `double` intervals per DD7. A separate `decimal` interval arithmetic layer is not warranted.
- **Literal suffix syntax.** No `0.1m` or `0.1d` suffixes are added to the DSL. Context-sensitive literal typing is the chosen mechanism (DD9).
- **Currency or unit-of-measure types.** The Evaluator redesign: semantic fidelity and lane integrity (issue #115) establishes the numeric lane foundation. Higher-level business types are tracked separately.
- **`sqrt(decimal)` overload.** Removed per DD10. The `sqrt` function is number-only. Authors use `sqrt(approximate(value))` for decimal inputs.
- **Cross-event computed-field carryover.** This is the proof engine's soundness boundary, not an evaluator concern. Tracked in ProofEngineDesign.md.
- **Backward-compatible migration tooling.** Breaking changes to the runtime API (e.g., `CoerceToDecimal` rejecting `double`) are documented but no automatic migration path is provided in this issue.

---

## Appendix A: Gap Analysis

The expression evaluator exists and is functionally correct for most expression evaluation. However, it has systemic numeric lane integrity violations that compromise the semantic fidelity guarantee. These violations span the parser, model, type checker, evaluator, and runtime boundary — they are not isolated bugs but a consistent pattern of collapsing distinct numeric lanes through `double`.

### Critical Finding 1: Parser collapses decimal literals to `double`

*(Violates § Numeric Type System — Decimal Lane and § Context-Sensitive Literal Typing.)*

`PreceptParser.ToNumericLiteralValue()` ([PreceptParser.cs](../src/Precept/Dsl/PreceptParser.cs#L235)) returns `long` for whole-number literals and `double` for fractional/scientific literals. A DSL literal `0.1` in a `decimal` field context becomes `double 0.1` (which is `0.1000000000000000055511151231257827021181583404541015625` in IEEE 754) at parse time. The exact base-10 value is irrecoverably lost before the evaluator ever sees it.

### Critical Finding 2: Field constraints stored as `double`

*(Violates § Assignment & Coercion — Constraint storage.)*

`FieldConstraint.Min(double Value)` and `FieldConstraint.Max(double Value)` ([PreceptModel.cs](../src/Precept/Dsl/PreceptModel.cs#L94)) store constraint bounds as `double`. A declaration `field Price as decimal min 0.01 max 999.99` stores its bounds as `double`, losing exact decimal representation. The runtime enforces constraints against values that have already been silently approximated.

### Critical Finding 3: Type checker maps `decimal` as `number`

*(Violates § Numeric Type System — Decimal Lane and § Integration Points — Literal kind inference.)*

`PreceptTypeChecker.MapLiteralKind()` ([PreceptTypeChecker.Helpers.cs](../src/Precept/Dsl/PreceptTypeChecker.Helpers.cs#L58)) classifies C# `decimal` runtime values as `StaticValueKind.Number`, not `StaticValueKind.Decimal`. This means the type checker cannot distinguish exact decimal values from approximate number values during type inference — the decimal lane is invisible at type-check time.

### Critical Finding 4: Type checker maps `.count` and `.length` as `number`

*(Violates § Numeric Type System — Integer Lane and § Integration Points — Symbol table.)*

`BuildSymbolKinds()` ([PreceptTypeChecker.Helpers.cs](../src/Precept/Dsl/PreceptTypeChecker.Helpers.cs#L352)) maps `Collection.count` and `Field.length` to `StaticValueKind.Number`. These are discrete integer surfaces — a collection cannot have 2.5 elements — but the type system treats them as approximate floating-point values.

### Critical Finding 5: Evaluator collapses all arithmetic through `double`

*(Violates § Operator Dispatch — Cases 2 and 4, and § Numeric Type System — Decimal Lane closure guarantee.)*

`TryToNumber()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L734)) converts every numeric type — including `decimal` — to `double`. Every binary arithmetic and comparison operator calls `TryToNumber` as its fallback path, meaning that `decimal` values silently lose precision during evaluation whenever both operands aren't matched by the `long`-specific fast path.

### Critical Finding 6: `min`/`max` compare via `double`

*(Violates § Function Contracts — Numeric Functions (`min`/`max` decimal overloads).)*

`ReduceComparable()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L500)) uses `TryToNumber` (which returns `double`) for all comparisons, even when all arguments are `decimal`. Two `decimal` values `0.1m` and `0.2m` are compared as `double`, producing correct results in most cases but violating the lane-preservation contract and risking edge-case divergence.

### Critical Finding 7: Collections normalize all numerics to `double`

*(Violates § Collection Storage Contract — NormalizeValue contract, CollectionComparer contract, and `.contains` semantics.)*

`CollectionValue.NormalizeValue()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L917)) converts every numeric type to `double` before storage. A `set<decimal>` collection stores `double` values internally. `CollectionComparer` compares via `double`. This means `set<decimal>` has `double` ordering semantics — the declared inner type is a lie at runtime. Consequently, `set<decimal> contains 0.1` compares `double 0.1` against `double`-stored elements, not `decimal 0.1m` against `decimal` elements.

### Critical Finding 8: Integer surfaces produce `double`

*(Violates § Numeric Type System — Integer Lane and § String / Boolean / Choice / Conditional / Contains Evaluation — `.length` Accessor.)*

`.count` returns `(double)collection.Count` and `.length` returns `(double)str.Length` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L63)). These are integer values cast to `double` for no semantic reason. Downstream arithmetic inherits the `double` lane instead of the `integer` lane. The same violation applies to the three-level dotted form (`EventName.ArgName.length`).

### Critical Finding 9: Runtime coercion crosses lanes silently

*(Violates § Assignment & Coercion — Runtime Coercion Rules.)*

`CoerceToDecimal()` ([PreceptRuntime.cs](../src/Precept/Dsl/PreceptRuntime.cs#L834)) accepts `double` and `float` inputs and casts them to `decimal` — silently importing approximate values into the exact lane. `CoerceToNumber()` ([PreceptRuntime.cs](../src/Precept/Dsl/PreceptRuntime.cs#L813)) collapses everything to `double`. JSON `UnwrapJsonElement()` returns `double` for all non-integer JSON numbers — a decimal field receiving JSON input `0.1` gets `double 0.1`, not `decimal 0.1m`.

### Critical Finding 10: String slicing silently truncates

*(Violates § Function Contracts — String Functions (`left`/`right`/`mid`) and § String / Boolean / Choice / Conditional / Contains Evaluation — String Slicing Parameter Type Contract.)*

`left()`, `right()`, and `mid()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L645)) accept any numeric type for their count/start/length parameters via `TryToNumber`, then silently truncate to `int` via `(int)countNum`. A call `left(Name, 3.7)` silently becomes `left(Name, 3)` — the fractional part is discarded without warning.

### Critical Finding 11: Unary minus collapses `decimal` to `double`

*(Violates § Operator Dispatch — unary operator lane preservation.)*

`EvaluateUnary()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L99)) checks for `long` first, then falls through to `TryToNumber`, which converts `decimal` to `double`. The expression `-Price` where `Price` is `decimal` produces a `double` result, silently leaving the exact lane. This is the same class of bug as the arithmetic operator violations (Critical Finding 5) but applied to the unary negation operator.

### Critical Finding 12: `DecimalPow` throws on zero base with negative exponent

*(Violates § Design Principles — Principle 4 (Totality).)*

`DecimalPow(0m, -1)` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L580)) computes `1m / DecimalPow(0m, 1)` which triggers `DivideByZeroException`. Unlike every other division path in the evaluator (which guards against zero divisors and returns `EvaluationResult.Fail`), this path throws an unhandled exception. This violates the totality guarantee (Principle 4) — the evaluator must return a definite error, not crash.

### Critical Finding 13: `DecimalPow` throws on large-exponent overflow

*(Violates § Design Principles — Principle 4 (Totality).)*

`DecimalPow(10m, 29)` and similar large-exponent cases throw an unhandled `OverflowException` when the result exceeds `decimal.MaxValue`. Like Critical Finding 12, this is a totality violation (Principle 4) — the evaluator must catch the overflow and return `EvaluationResult.Fail` with a descriptive error, not propagate the exception. *(Identified in DD14.)*

### Critical Finding 14: `min`/`max` decimal comparison routes through `double`

*(Violates § Function Contracts — Numeric Functions (`min`/`max` decimal overloads).)*

`ReduceComparable()` ([PreceptExpressionEvaluator.cs](../src/Precept/Dsl/PreceptExpressionEvaluator.cs#L500)) uses `TryToNumber` (which returns `double`) for all comparisons in `min`/`max`, even when all arguments are `decimal`. Two `decimal` values `0.1m` and `0.2m` are compared as `double`, producing correct results in most cases but violating the lane-preservation contract and risking edge-case divergence for values near `decimal`'s precision limits. The fix is a decimal fast-path using native `decimal.CompareTo`, mirroring the existing `long` fast-path. *(Identified in DD13.)*

### Critical Finding 15: Unary minus rejects decimal in type checker

*(Gap G2 — type checker too strict for unary minus on decimal.)*

The type checker rejects unary minus applied to `decimal`-typed expressions, but the evaluator correctly handles it by collapsing through `TryToNumber`. The type checker should accept unary minus for all three numeric lanes. This is a trivial fix in `TypeInference.cs` — add `decimal` to the accepted operand types for unary minus. *(Resolves Gap G2.)*

### Critical Finding 16: Collection `.min`/`.max`/`.peek` on empty collection produces runtime Fail

*(Gap G1 — empty collection accessor not caught by type checker.)*

Accessing `.min`, `.max`, or `.peek` on an empty collection produces `EvaluationResult.Fail` at runtime. The type checker does not statically prevent this — the emptiness condition depends on runtime state. Resolved by DD21: the type checker now requires a conditional guard (`if Collection.count > 0 then Collection.min else fallback`), making bare accessor use a compile-time error. *(Resolves Gap G1 via DD21.)*

### Critical Finding 17: Ordered choice comparison with non-member literal produces runtime Fail

*(Gap G4 — ordered choice literal not validated at compile time.)*

An ordered choice comparison like `when Priority > "Bogus"` compiles without diagnostics but fails at runtime with `"'Bogus' is not a member of the ordered choice set."` The type checker validates choice members in `set` assignments (C68) but not in comparison contexts. Resolved by DD22: the type checker now validates string literals in ordered choice comparisons against the declared choice set. *(Resolves Gap G4 via DD22.)*

### Root Cause

These are not independent bugs. They share a single root cause: the evaluator was originally built with `double` as the universal numeric representation, before the three-type numeric system (`integer`/`decimal`/`number`) was fully specified. The `long` integer lane was added later (issue #29) with correct fast paths for homogeneous integer operations, but the `decimal` lane was scaffolded (issue #27) without corresponding evaluator, collection, or runtime support. The result is a system that declares three numeric lanes in the type system but collapses to two (`long` and `double`) at evaluation time.

### Function Contract Implementation Status

The following table documents the implementation status of each function contract against the specification in § Function Contracts. Entries marked "None (correct today)" are already conformant.

#### Numeric Functions

| Function | Input type(s) | Output type | Lane rule | Implementation status |
|----------|---------------|-------------|-----------|----------------------|
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
| `round(decimal)` | `decimal` | `long` | Decimal → integer (banker's rounding) | **Violation**: current impl converts to `decimal` via `TryToDecimal` then rounds — correct but `TryToDecimal` accepts `double` input |
| `round(number)` | `double` | `long` | Number → integer (banker's rounding) | **Violation**: same `TryToDecimal` path — mixes lanes |
| `round(any, places)` | any numeric, `long` | `decimal` | **Explicit bridge**: number→decimal normalization | **Violation**: operates via `TryToDecimal` which accepts `double` — conceptually correct but should produce `decimal` natively |
| `min(integer, ...)` | `long, ...` | `long` | Stays integer | **Violation**: `ReduceComparable` uses `TryToNumber` (double) for comparison (CF6, CF14) |
| `min(decimal, ...)` | `decimal, ...` | `decimal` | Stays decimal | **Violation**: same — decimal compared as double (CF6, CF14) |
| `min(number, ...)` | `double, ...` | `double` | Stays number | None (already double) |
| `max(integer, ...)` | `long, ...` | `long` | Stays integer | **Violation**: same as min (CF6, CF14) |
| `max(decimal, ...)` | `decimal, ...` | `decimal` | Stays decimal | **Violation**: same as min (CF6, CF14) |
| `max(number, ...)` | `double, ...` | `double` | Stays number | None (already double) |
| `clamp(integer, ...)` | `long, long, long` | `long` | Stays integer | Partial — has `long` fast path but falls to double |
| `clamp(decimal, ...)` | `decimal, decimal, decimal` | `decimal` | Stays decimal | Partial — has `decimal` fast path but falls to double |
| `clamp(number, ...)` | `double, double, double` | `double` | Stays number | None |
| `pow(integer, integer)` | `long, long` | `long` | Stays integer | None (correct today) |
| `pow(decimal, integer)` | `decimal, long` | `decimal` | Stays decimal | None (correct today) |
| `pow(number, integer)` | `double, long` | `double` | Stays number | None (correct today) |
| `sqrt(number)` | `double` | `double` | Stays number | None (correct today) |
| `approximate(decimal)` | `decimal` | `double` | **Explicit bridge**: decimal→number | New function (DD11) |

#### String Functions

| Function | Input type(s) | Output type | Lane rule | Implementation status |
|----------|---------------|-------------|-----------|----------------------|
| `left(string, integer)` | `string, long` | `string` | Count param must be integer | **Violation**: accepts any numeric via `TryToNumber`, silently truncates (CF10) |
| `right(string, integer)` | `string, long` | `string` | Count param must be integer | **Violation**: same (CF10) |
| `mid(string, integer, integer)` | `string, long, long` | `string` | Start and length must be integer | **Violation**: same (CF10) |
| `toLower(string)` | `string` | `string` | N/A | None |
| `toUpper(string)` | `string` | `string` | N/A | None |
| `trim(string)` | `string` | `string` | N/A | None |
| `startsWith(string, string)` | `string, string` | `boolean` | N/A | None |
| `endsWith(string, string)` | `string, string` | `boolean` | N/A | None |

---

## Appendix B: Design Decision Log

### DD1: Three Distinct Numeric Type Families

**Decision:** The evaluator operates on three numeric type families — integer (`long`), decimal (`decimal`), number (`double`) — each with a distinct C# backing type, distinct semantics, and distinct lane-preservation rules.

**Rationale:** Business domains require both exact arithmetic (money, rates, tax) and approximate arithmetic (scientific calculations, scoring). A single `double`-backed numeric type cannot serve both — `0.1 + 0.2 != 0.3` in IEEE 754 is unacceptable for financial calculations. A single `decimal`-backed type is too slow and too restrictive for domains that accept approximation. The three-lane model matches C#'s own numeric type hierarchy and gives authors explicit control over precision semantics.

**External precedent:** CEL (Google Common Expression Language) uses three distinct numeric types (`int`, `uint`, `double`) with no automatic arithmetic conversions — cross-type arithmetic is a type error. C# itself (§12.4.7) defines separate predefined operators per numeric type and forbids mixing `decimal` with `float`/`double`. F# forbids all implicit numeric conversions. FEEL/DMN uses a single Decimal128 type — simpler but less expressive. Precept's three-lane model sits between CEL's strictness and C#'s promotion rules.

**Alternatives rejected:** (a) Single `double` lane (current broken state) — violates philosophy's numeric exactness commitment. (b) Single `decimal` lane (FEEL/DMN approach) — too slow for approximate-acceptable domains, cannot represent `sqrt` natively. (c) Two lanes (`integer` + `decimal` only) — forces approximate operations to produce decimal results, which are misleadingly exact-looking.

**Tradeoff accepted:** Three lanes increase operator dispatch complexity (6 binary operand combinations instead of 1). The type checker catches all cross-lane errors at compile time (see Principle 18), limiting runtime dispatch to well-typed expressions.

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

**Totality note:** This is a correctness fix, not a new feature. The lane integrity contract already requires decimal-native comparison (see § Function Contracts); the current implementation fails to honor it.

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

### DD17: Runtime-Scoped Lane Identity

**Decision:** Lane identity is preserved within the CLR runtime — `decimal` stays `decimal`, `number` stays `double`, `integer` stays `long` throughout every evaluation step. Serialized output uses standard JSON — no typed wrappers, no lane metadata. Consumers recover lane information from the precept definition (`precept_compile` output).

**Rationale:** "Don't reinvent serialization, use standard mechanisms." Data outside Precept is outside Precept's guarantees. The runtime is the integrity boundary — once values cross into JSON, they are plain JSON values. Lane recovery is a metadata lookup against the precept definition, not a property of the serialized value.

**Narrows Principle 14.** The previous formulation implied consumers could distinguish lanes from the serialized output alone. The corrected formulation: the runtime preserves lane identity; serialization uses standard formats.

**External precedent:** Every major serialization format (JSON, Protocol Buffers, MessagePack) collapses numeric types. JSON has one `number` type. Typed-value wrappers (e.g., `{"type": "decimal", "value": "0.1"}`) are non-standard and poorly supported by downstream tooling.

**Alternatives rejected:** Typed-value wrappers in MCP output — overengineered, non-standard, creates a custom serialization format that every consumer must understand.

**Tradeoff accepted:** JSON numbers lose lane identity. A `decimal` field value `0.1` and a `number` field value `0.1` are indistinguishable in JSON output. Consumers must cross-reference the precept definition to recover lane semantics.

### DD18: MCP Boundary Uses Standard JSON

**Decision:** `JsonConvert.cs` converting JSON numbers to `double` is acceptable — this is standard JSON behavior. MCP input is "outside Precept" — the runtime coerces incoming values to the declared field type on ingestion. No schema-aware JSON parsing is needed at the MCP boundary.

**Rationale:** Same as DD17 — standard mechanisms, Precept's guarantees are runtime-scoped. The MCP boundary is an ingestion point, not an integrity boundary. The runtime's coercion rules (§ Assignment & Coercion — Runtime Coercion Rules) enforce lane-correct storage after ingestion.

**Resolves:** MCP input lane collapse concerns. A `double` arriving from JSON for a `decimal` field is coerced by the runtime's existing type coercion contract — the MCP layer does not need to perform schema-aware parsing.

**Tradeoff accepted:** MCP consumers sending values for `decimal` fields via JSON may experience the standard JSON numeric precision limitations. The runtime coercion contract handles the boundary.

### DD19: Checked Integer Arithmetic

**Decision:** All integer arithmetic in the evaluator uses `checked` contexts. Overflow produces `EvaluationResult.Fail` — a definite error, not an unhandled exception or silent wraparound.

**Scope:** Covers the `IntegerPow` multiplication loop, rounding-to-`long` casts in `floor`/`ceil`/`truncate`/`round`, and any future integer arithmetic path.

**Rationale:** Totality guarantee (Principle 4) — every expression evaluates to a result or a definite error. Silent integer overflow in a domain integrity engine is unacceptable — it produces silently corrupted values that downstream guards and rules operate on as if valid. The `long` range (±9.2×10¹⁸) covers all realistic business-domain integer values; overflow indicates a logic error or adversarial input, not a normal condition.

**Performance:** `checked` arithmetic adds approximately one CPU instruction per integer operation (an overflow-flag test). Negligible in a business-rule engine where expression evaluation is not the throughput bottleneck.

**Alternatives rejected:** Unchecked arithmetic (current behavior for some paths) — silent overflow corruption in a domain integrity engine is worse than the ~1 instruction overhead.

**Tradeoff accepted:** ~1 CPU instruction overhead per integer operation. Expressions that would silently overflow now produce `EvaluationResult.Fail` instead of a silently wrong value.

### DD20: Integer Pow Restricts to Non-Negative Exponents

**Decision:** `pow(integer, integer)` requires exponent ≥ 0. The type checker rejects negative integer literal exponents. The proof engine must verify non-negative when the exponent is a field reference.

**Rationale:** "Thinly wrap math — don't invent integer behavior for operations that produce non-integer results." `pow(2, -1) = 0.5` is not an integer. Rather than inventing a truncation convention (`pow(2, -1) → 0`) or returning a different type, the type system rejects the operation outright. Authors who need `x⁻ⁿ` should use the decimal or number lane: `pow(approximate(x), -n)` or `pow(x_as_decimal, -n)`.

**External precedent:** Python `int ** -1` → `float` (lane-crosses); Haskell `(^)` restricts to non-negative exponents (type error for negative); SQL `POWER()` returns `float` regardless of input types. Precept follows Haskell's approach — the operation is undefined for the integer type rather than silently crossing lanes.

**Proof engine obligation:** When the exponent is a field reference (not a literal), the proof engine must verify that the field's value range is non-negative. If the proof engine cannot prove non-negative, a diagnostic is emitted.

**Alternatives rejected:** Truncated integer division (`pow(2, -1) → 0`) — surprising and inconsistent. Most developers expect `2⁻¹ = 0.5`, not `0`. Returning `0` sends mixed signals about what the operation means.

**Tradeoff accepted:** Authors cannot compute negative-exponent powers in the integer lane. They must explicitly cross to decimal or number, which makes the non-integer result visible in the type system.

### DD21: Collection Accessor Emptiness — Blanket Prohibition

**Decision:** `.min`, `.max`, and `.peek` require a conditional guard: `if Collection.count > 0 then Collection.min else fallback`. The type checker rejects bare `.min`/`.max`/`.peek` use outside a conditional expression whose condition tests `.count > 0`.

**Rationale:** Principle 18 (static completeness) — no runtime errors from compiled precepts. Collection emptiness is statically unprovable today: the proof engine cannot determine whether a collection is guaranteed non-empty, because collection population depends on runtime event history. The conditional guard pattern makes the emptiness check explicit in the source code, eliminating the runtime failure path for compiled precepts.

**Diagnostic:** New diagnostic (extend C85 or new code) in `TryInferKind` for identifier expressions with `.min`/`.max`/`.peek` member access. The diagnostic fires when the accessor appears outside a conditional expression whose condition tests the collection's `.count > 0`.

**Implementation:** The type checker inspects the AST context of `.min`/`.max`/`.peek` accessor expressions. If the accessor is the `ThenBranch` of a conditional expression whose `Condition` is a comparison of the same collection's `.count` against `0` (using `>`), the accessor is permitted. Otherwise, a diagnostic is emitted.

**Alternative rejected:** Count-aware proof engine extension — extending the proof engine to track collection cardinality intervals, enabling bare `.min`/`.max`/`.peek` when the collection is provably non-empty. This approach is tractable but deferred: filed as issue #131 for future investigation. The blanket prohibition is the conservative starting point.

**Tradeoff accepted:** More verbose syntax — `if Items.count > 0 then Items.min else 0` instead of bare `Items.min`. The verbosity makes the emptiness assumption explicit, which is both safe and readable. The conditional pattern is standard in the sample corpus (see `computed-tax-net.precept`, `fee-schedule.precept`).

### DD22: Choice Literal Validation in Comparisons

**Decision:** The type checker validates that string literals used in ordered choice comparisons (`Priority > "Urgent"`) are members of the declared choice set. Invalid literals produce a compile-time diagnostic.

**Rationale:** Principle 18 (static completeness) — `when Priority > "Bogus"` currently compiles without diagnostics but fails at runtime with `"'Bogus' is not a member of the ordered choice set."` This violates the static completeness guarantee: a compiled precept should not produce type errors at runtime.

**Extends:** The existing C68 diagnostic pattern, which validates choice members in `set` assignments. DD22 extends the same validation to comparison contexts.

**Implementation:** `TryInferBinaryKind` needs access to field metadata (specifically `PreceptField.ChoiceValues`) when one operand is an identifier referencing a choice field and the other is a string literal. The type checker resolves the field, checks `IsOrdered`, and validates the literal against `ChoiceValues`. If the literal is not a member, a diagnostic is emitted.

**Context enrichment:** The type inference path for binary expressions currently operates on `StaticValueKind` without field metadata. DD22 requires threading field metadata (choice value lists) into the type inference context so that `TryInferBinaryKind` can perform membership validation. This is a moderate implementation cost — the type checker must carry field context through binary expression inference.

**Alternative rejected:** Leave as runtime error — this directly violates Principle 18. The runtime error path becomes defensive redundancy only after DD22.

**Tradeoff accepted:** Moderate implementation cost (context enrichment in type inference). The type checker must carry more context through binary expression analysis, but the payoff is a complete static guarantee: every compiled choice comparison uses valid members only.

### DD23: EvalFailCode Enum — Internal, Classified with Attributes

**Decision:** Introduce an internal `EvalFailCode` enum that catalogs every evaluator failure mode. Each member is classified with `[StaticallyPreventable("CXX")]` (linked to `DiagnosticCatalog`) or `[LegitimatelyDynamic]`. The enum is internal — invisible to API consumers and MCP output.

**Rationale:** Free-form `Fail(string)` calls have no structural identity — George's audit (Appendix D) required manual source-text scanning to discover and classify 72 Fail sites. The enum provides stable identity, makes classification a first-class architectural concept, and enables automated sentinel tests. The `[StaticallyPreventable("CXX")]` attribute creates a bi-directional link: evaluator failure → compiler diagnostic.

**Alternatives rejected:** (a) Public enum visible in `EvaluationResult` — premature API surface commitment. (b) External classification file — decouples classification from code, easier to drift. (c) Sentinel tests only — detects drift reactively but does not prevent it.

**Tradeoff accepted:** ~75 existing Fail call sites must be updated to pass an `EvalFailCode` member. Mechanical but non-trivial.

**See:** Appendix F § F.2 for full specification.

### DD24: Function Evaluation Is Registry-Driven

**Decision:** Extend `FunctionOverload` with a `Func<object?[], EvaluationResult>? Evaluator` delegate. The evaluator's `EvaluateFunction` dispatches through the registry instead of a hand-coded switch. Every registered overload must have a non-null `Evaluator` delegate.

**Rationale:** The `FunctionRegistry` already provides a declarative contract that the type checker reads — but the evaluator ignores it and reimplements every function body independently. This is the archetype of compiler↔evaluator drift. George's gap G5 (`pow(integer, negative)`) is the canonical example: the registry declared `RequiresNonNegativeProof`, but the evaluator's switch arm did not enforce it.

**Alternatives rejected:** (a) Keep hand-coded switch with sentinel tests — detects drift but does not prevent it. (b) Code-generate the switch from the registry — adds build complexity, makes debugging harder.

**Tradeoff accepted:** Evaluation delegates in a static registry replace direct pattern-matching. Performance delta is negligible. Debugging goes through delegates instead of named methods — mitigation: each delegate references a named static method.

**See:** Appendix F § F.3 for full specification.

### DD25: Operator Dispatch Is Registry-Driven

**Decision:** Introduce an `OperatorRegistry` that declares the full operator × type-family matrix — legal combinations, result types, widening rules, and evaluation delegates. Both the type checker and evaluator consume this registry as their single source of truth.

**Rationale:** The type checker and evaluator independently implement the same semantic rules for operator dispatch (~200 LOC binary, ~30 LOC unary). George's gaps G2 (unary minus rejects decimal in type checker) and G4 (ordered choice comparison) both stem from this independent implementation. A shared registry makes disagreement structurally impossible.

**Alternatives rejected:** (a) Registry for functions only, sentinels for operators — sentinels need to enumerate the full matrix anyway. (b) Dispatch table without evaluation delegates — provides agreement on legality but not evaluation behavior.

**Tradeoff accepted:** The operator registry is more complex than the function registry due to widening, lane promotion, and comparison-vs-arithmetic distinction. The operator set is stable (11 operators), so per-entry drift-prevention value is lower than functions — but the type-family dimension is where drift occurs.

**See:** Appendix F § F.4 for full specification.

### DD26: Roslyn Analyzer as Default Structural Enforcement (11 Rules)

**Decision:** A single `Precept.Analyzers` project containing 11 Roslyn analyzer rules (PREC001–PREC011) is the default structural enforcement mechanism for the codebase. Rules cover evaluator failure classification, exhaustive switch coverage, token metadata completeness, and cross-catalog consistency. Four existing reflection-based drift tests are replaced and deleted.

**Rationale:** DD23 makes failure classification possible; the Roslyn analyzer makes structural discipline mandatory and immediate. The shift-left benefit — from test-time CI discovery to edit-time IDE red squiggles — applies to all structural invariants enforceable from the syntax tree, not just `EvaluationResult.Fail` calls.

**Alternatives rejected:** (a) Source-text scanning in tests — fragile, regex-based, test-time only. (b) Convention enforcement via code review — the 7 gaps prove this is insufficient. (c) Source generator — generators produce code, not diagnostics. (d) Keep reflection tests alongside analyzer — parallel enforcement provides no value.

**Tradeoff accepted:** Adds a `Precept.Analyzers` project with 11 rules. Build-time analyzer loading has negligible performance impact. Reflection-based drift tests remain only for C#↔JSON boundaries (e.g., FunctionRegistry ↔ TextMate grammar) where Roslyn cannot reach.

**See:** Appendix F § F.5 for the full rule catalog.

---

## Appendix C: Test Obligations

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

## Appendix D: Compiler↔Evaluator Conformance Audit

This appendix documents the results of the compiler↔evaluator conformance audit — a systematic review of all evaluator error paths to verify that Principle 18 (static completeness) holds: every statically preventable runtime error has a corresponding type-checker or proof-engine rule that prevents it at compile time.

### Audit Scope

The audit examined all 72 distinct evaluator error paths — every `EvaluationResult.Fail(...)` return site and every unguarded exception path in `PreceptExpressionEvaluator.cs`. Each path was classified as either:

- **Statically preventable** — the type checker or proof engine can reject the input at compile time.
- **Legitimately dynamic** — the error depends on runtime state that cannot be determined statically (overflow, DecimalPow edge cases, empty collections with conditional guard).

### Coverage Summary

| Category | Count | Status |
|----------|-------|--------|
| Fully covered by type checker | 38 | No action needed — type checker already rejects |
| Gaps needing type-checker changes | 2 | G2, G5 — trivial fixes |
| Gaps needing evaluator hardening only | 3 | G6, G7, unchecked arithmetic — defense-in-depth |
| Gaps resolved by new design decisions | 2 | G1 (DD21), G4 (DD22) |
| **Total error paths audited** | **72** | |

**After the 7 gaps are resolved:** Every statically preventable evaluator `Fail` has a matching compiler rule. The only runtime failures from compiled precepts will be legitimately dynamic: integer overflow (DD19), `DecimalPow` edge cases (DD14), and guard/rule business logic producing `false`.

### Gap Detail

#### G1: Collection accessor on empty collection → DD21

**Trigger:** `.min`, `.max`, or `.peek` on a collection with zero elements.

**Location:** `EvaluateIdentifier()` → member accessor dispatch for `.min`/`.max`/`.peek`.

**Fix:** DD21 — the type checker requires a conditional guard (`if Collection.count > 0 then Collection.min else fallback`). Bare accessor use is now a compile-time error. The evaluator's runtime check remains as defensive redundancy.

#### G2: Unary minus rejects decimal in type checker

**Trigger:** Unary minus applied to a `decimal`-typed expression. The type checker rejects it; the evaluator would handle it correctly (via `TryToNumber` collapse).

**Location:** `TryInferKind()` for unary expressions in `PreceptTypeChecker.cs`.

**Fix:** Trivial — add `StaticValueKind.Decimal` to the accepted operand types for unary minus in the type checker. Also see CF15.

#### G3: Division/modulo by zero

**Trigger:** `x / 0` or `x % 0` where the divisor is a literal or field value equal to zero.

**Status:** Already covered by C92 (literal zero divisor) and C93 (proof-engine divisor safety). NaN/Infinity from extreme floating-point values is a DD19 carve-out (legitimate runtime). No gap.

#### G4: Ordered choice comparison with non-member literal → DD22

**Trigger:** `when Priority > "Bogus"` where `"Bogus"` is not in the declared choice set.

**Location:** `TryGetChoiceOrdinals()` → ordinal lookup failure in evaluator.

**Fix:** DD22 — the type checker validates string literals in ordered choice comparisons against the declared choice set at compile time. The evaluator's runtime check remains as defensive redundancy.

#### G5: `pow(integer, negative)` not rejected by type checker

**Trigger:** `pow(Count, -2)` where both arguments are integer-typed and the exponent is negative.

**Location:** `FunctionRegistry` → `IntegerPow` body. The evaluator computes `1/pow(x,|n|)` which truncates to 0 for most inputs.

**Fix:** Trivial — DD20 compliance. Add a type-checker rule that rejects negative integer literal exponents for `pow(integer, integer)`. When the exponent is a field reference, the proof engine must verify ≥ 0.

#### G6: `sqrt()` missing NaN guard

**Trigger:** `sqrt(x)` where `x` is negative. `Math.Sqrt(-1.0)` returns `double.NaN`, which then propagates silently.

**Location:** `FunctionRegistry` → `sqrt(double)` body.

**Fix:** Evaluator hardening — add a guard that returns `EvaluationResult.Fail` when the argument is negative. The proof engine already enforces non-negativity for `sqrt` arguments (C94); this is defense-in-depth only.

#### G7: `floor`/`ceil`/`truncate`/`round` overflow on cast to `long`

**Trigger:** `floor(1e18)` where the rounded result exceeds `long.MaxValue` or is below `long.MinValue`.

**Location:** `FunctionRegistry` → rounding function bodies that cast `double`/`decimal` to `long`.

**Fix:** DD19 scope — use `checked` casts in all rounding-to-`long` paths. Overflow produces `EvaluationResult.Fail` (a legitimate dynamic failure, like integer arithmetic overflow).

### Classification

**Statically preventable** (must have compiler rules — drift = bug):
- All 38 already-covered error paths
- G1 (DD21), G2 (type-checker fix), G4 (DD22), G5 (DD20 compliance)

**Legitimately dynamic** (runtime failures that cannot be statically prevented):
- Integer arithmetic overflow (DD19 — checked arithmetic)
- `DecimalPow` edge cases: division by zero with zero base + negative exponent, decimal range overflow (DD14)
- `floor`/`ceil`/`truncate`/`round` overflow on `long` cast (G7 — DD19 scope)
- `sqrt` NaN guard (G6 — defense-in-depth behind proof engine's C94)
- Guard/rule business logic producing `false` (by design — this is the prevention engine)

The full error-path catalog is maintained in the conformance test suite (see Appendix E) rather than reproduced here. The test suite enforces the classification: every statically preventable `Fail` path is paired with a compiler-rejection test, and every legitimate dynamic failure is paired with an `AllowedDynamicState` marker.

---

## Appendix E: Conformance Test Architecture

This appendix specifies the conformance test architecture that enforces Principle 18 (static completeness). The test suite systematically verifies that the type checker and evaluator agree on every construct — if the type checker accepts an expression, the evaluator must evaluate it without type errors; if the type checker rejects an expression, the evaluator must also reject it when the type checker is bypassed.

### Scope

The test architecture addresses two classes of evaluator failures:

1. **Statically preventable** — errors that the type checker or proof engine should catch at compile time. If a statically preventable error reaches the evaluator at runtime, it indicates drift between the compiler and evaluator. These are bugs.
2. **Legitimately dynamic** — errors that depend on runtime state: integer overflow (DD19), `DecimalPow` edge cases (DD14), empty collections behind conditional guards (DD21), and guard/rule business logic producing `false`. These are expected runtime behaviors, not drift.

### Seven Conformance Test Categories

#### 1. Registry-Derived Function Conformance

Auto-generated test matrix from `FunctionRegistry`. For every registered function overload:
- **Positive:** Compile succeeds AND evaluate succeeds — assert CLR result type matches declared output type and value is correct.
- **Negative:** Compile rejects when argument types don't match any registered overload.
- **Bypass:** Evaluator also rejects when type checker is skipped — the evaluator's own type dispatch produces `Fail`, not an unhandled exception.

New overloads added to `FunctionRegistry` auto-require tests — the anti-drift sentinel (see below) detects untested overloads.

#### 2. Operator Dispatch and Lane Matrix

Operator × type-family matrix covering all 6 binary operand combinations (`long×long`, `decimal×decimal`, `double×double`, `long×decimal`, `long×double`, `decimal×double`) for all 5 arithmetic operators (`+`, `-`, `*`, `/`, `%`) and all 6 comparison operators (`==`, `!=`, `>`, `>=`, `<`, `<=`):
- **Positive:** Each legal combination compiles and evaluates with correct result type and value.
- **Negative:** Each illegal combination (e.g., `decimal + double`) is rejected by the type checker.
- **Bypass:** Evaluator also rejects illegal combinations via its own dispatch.

#### 3. Accessor and Collection Conformance

Tests for `.count`, `.length`, `.min`, `.max`, `.peek`, and `contains`:
- **`.count` and `.length` return `long`** — assert `typeof(long)`, not `typeof(double)`.
- **`.min`/`.max` preserve inner type** — `set<decimal>.min` returns `decimal`, not `double`.
- **`.min`/`.max`/`.peek` require conditional guard** — bare use is rejected by type checker (DD21).
- **`contains` uses lane-native comparison** — `set<decimal> contains 0.1` compares via `decimal`, not `double`.

#### 4. Proof-Obligation Conformance

Tests for proof-engine obligations: divisor safety (C92/C93), `sqrt` non-negativity (C94), `pow` non-negative exponent (DD20), and assignment interval validation:
- **Positive:** Expression with provably safe operands compiles and evaluates.
- **Negative:** Expression with unprovable operands emits the correct diagnostic.
- **Bypass:** Evaluator also rejects (defense-in-depth) when proof is skipped.

#### 5. Assignment and Runtime-Boundary Conformance

Tests for the three write paths — Fire (`set` assignments), Update (direct field edits), and computed-field recomputation:
- **Lane compatibility:** Each write path enforces the same lane rules (§ Assignment & Coercion).
- **Constraint enforcement:** Constraints are checked in the target field's lane-native type.
- **Coercion rules:** Runtime boundary coercion follows the documented table (§ Runtime Coercion Rules).

#### 6. Evaluator-Failure Parity

Every statically preventable `EvaluationResult.Fail` return site in the evaluator is mapped to a compiler rule:
- For each `Fail` site, a test asserts that the type checker emits the corresponding diagnostic.
- Sites that are legitimately dynamic are mapped to `AllowedDynamicState` markers instead.
- The sentinel (see below) detects unmapped `Fail` sites — a new `Fail` return added to the evaluator without a matching compiler rule or `AllowedDynamicState` marker fails the test suite.

#### 7. Regression Anchors

Named tests for every Appendix A critical finding and every design decision with behavioral impact. These tests encode the specific bugs and decisions that shaped the current design — they cannot be silently deleted or renamed without failing the anchor registry sentinel.

### Dual-Path Testing Pattern

Every construct is tested from three sides:

1. **Positive path:** Compile succeeds AND evaluate succeeds. Assert both the CLR type (`Assert.IsType<decimal>()`) and the value (`Assert.Equal(expected, actual)`) of the result. Value-only assertions miss lane regressions where the result is numerically close but in the wrong type (Principle 16).

2. **Negative path:** Compile rejects with the expected diagnostic code and message. Assert that the diagnostic is correct — wrong code or wrong message is a failure.

3. **Bypass path:** Skip the type checker and invoke the evaluator directly. Assert that the evaluator also rejects the input with `EvaluationResult.Fail`. This is the defense-in-depth guarantee — even if the type checker has a bug, the evaluator does not silently produce a wrong-typed result.

### Anti-Drift Sentinels

Four sentinel mechanisms detect drift before it reaches production:

#### Function Registry Completeness

Reflects over `FunctionRegistry` at test time. For every registered overload `(functionName, inputTypes, outputType)`, asserts that a corresponding conformance test exists. A new overload added without a test fails the sentinel.

#### Operator Manifest Completeness

Maintains an explicit operator × type-family matrix. Adding a new operator or a new type family without updating the matrix fails the sentinel.

#### Evaluator Failure Classification

Every `EvaluationResult.Fail` call site passes an `EvalFailCode` member (enforced by the Roslyn analyzer at build time — see Appendix F § F.5). Each `EvalFailCode` member must be classified with `[StaticallyPreventable("CXX")]` or `[LegitimatelyDynamic]`. The EvalFailCode sentinel test (Appendix F § F.7) reflects over the enum and asserts the mapping:
- `[StaticallyPreventable("CXX")]` members must have a matching `DiagnosticCatalog` entry and a conformance test proving the type checker emits that diagnostic.
- `[LegitimatelyDynamic]` members are accepted — no compiler rule is expected.

An unclassified `EvalFailCode` member or a bare-string `Fail` call (without `EvalFailCode`) fails the build.

#### Regression Anchor Registry

Maintains a list of named regression anchor tests. Deleting or renaming an anchor test without updating the registry fails the sentinel. This prevents silent removal of tests that encode critical findings and design decisions.

### Regression Anchors Table

The following named tests must exist in the conformance test suite. Each anchors a specific critical finding or design decision:

| Anchor name | What it tests | Origin |
|-------------|---------------|--------|
| `DecimalClosure_SubtractionCanary` | `0.3m - 0.2m - 0.1m == 0.0m` in decimal lane | CF5, DD2 |
| `CountReturnsInteger` | `.count` returns `long`, not `double` | CF8, DD3 |
| `LengthReturnsInteger` | `.length` returns `long`, not `double` | CF8, DD3 |
| `SqrtDecimalOverloadRemoved` | `sqrt(decimal)` is a compile error | DD10, DD16 |
| `ApproximateFunctionExists` | `approximate(decimal) → double` compiles and evaluates | DD11 |
| `PowIntegerNegativeExponentRejects` | `pow(integer, -1)` is a compile error | DD20 |
| `DecimalPlusNumberTypeError` | `decimal + number` is a compile error | DD12 |
| `MinDecimalReturnsDecimal` | `min(decimal, decimal)` returns `decimal`, not `double` | CF14, DD13 |
| `MaxDecimalReturnsDecimal` | `max(decimal, decimal)` returns `decimal`, not `double` | CF14, DD13 |
| `RoundBridgeReturnsDecimal` | `round(number, 2)` returns `decimal` | DD5 |
| `CollectionAccessorRequiresGuard` | Bare `.min`/`.max`/`.peek` is a compile error | DD21, CF16 |
| `ChoiceLiteralValidatedInComparison` | `Priority > "Bogus"` is a compile error | DD22, CF17 |
| `CheckedIntegerOverflow` | Integer overflow produces `Fail`, not silent wraparound | DD19 |
| `DecimalPowZeroBaseNegativeExponent` | `pow(0m, -1)` produces `Fail`, not exception | DD14, CF12 |
| `UnaryMinusDecimalAccepted` | `-Price` where `Price` is decimal compiles and evaluates | CF15, G2 |
| `EvalFailCodeSentinel_AllStaticallyPreventableHaveCompilerRule` | Every `[StaticallyPreventable]` EvalFailCode member maps to a DiagnosticCatalog entry | DD23, Appendix F |
| `FunctionRegistryEvaluatorCompleteness` | Every `FunctionRegistry` overload has a non-null `Evaluator` delegate | DD24, Appendix F |
| `OperatorRegistryCompleteness` | Every `OperatorRegistry` entry has an evaluation delegate | DD25, Appendix F |
| `RoslynAnalyzer_PREC001_BareStringFail` | Roslyn analyzer flags `EvaluationResult.Fail` calls without `EvalFailCode` | DD26, Appendix F |
| `RoslynAnalyzer_PREC002_ExhaustiveExpressionSwitch` | Roslyn analyzer flags non-exhaustive switch over `PreceptExpression` subtypes | DD26, Appendix F |
| `FunctionRegistry_GrammarSync` | `FunctionRegistry.FunctionNames` matches TextMate grammar `functionCall` pattern | Appendix F |

---

## Appendix F: Compiler↔Evaluator Conformance Architecture

This appendix specifies the structural mechanisms that enforce Principle 18 (static completeness) at the architecture level. The goal: make compiler↔evaluator drift a build error, not a review finding. Where Appendix D documents the audit that found 7 gaps in 72 error paths, and Appendix E defines the test architecture that detects drift after the fact, this appendix defines the structural enforcement that prevents drift by construction.

### F.1 Motivation

Principle 18 makes a hard promise: if a precept compiles without diagnostics, it will not produce type errors at runtime. George's conformance audit (Appendix D) found 7 gaps across 72 evaluator error paths — a 90% coverage rate arrived at through heroic manual effort. The audit proved that the compiler and evaluator can drift, and that the drift is invisible without systematic review.

The structural risk has three dimensions:

1. **Independent authoring.** The evaluator dispatches on runtime CLR types (`is long`, `is decimal`, `is string`) and returns `EvaluationResult.Fail(string)`. The type checker dispatches on `StaticValueKind` flags and emits diagnostics via `DiagnosticCatalog`. No compile-time or structural relationship ties these two error surfaces together.

2. **Free-form failure identity.** Every `EvaluationResult.Fail("operator '+' requires ...")` is an ad-hoc string with no catalog identity. There is no enum, no diagnostic code, and no way for a test to discover "this Fail site exists" without source-text scanning or reflection.

3. **Growth vector.** New language features add evaluator paths first — parser support, then evaluator handling, then type-checker rules, then tests. Steps 3 and 4 are the ones that get missed under pressure, and the evaluator silently becomes the safety net instead of defense-in-depth.

The conformance test architecture in Appendix E detects drift reactively — sentinel tests discover new Fail sites and demand classification. This appendix defines three structural mechanisms that prevent drift proactively:

- **EvalFailCode catalog** — gives every Fail site a stable identity and a declared classification (Phase 1).
- **Registry-driven function evaluation** — eliminates function-level drift by construction (Phase 2).
- **Operator registry** — eliminates operator-level drift by construction (Phase 3).

A fourth mechanism — a **Roslyn analyzer** — enforces classification discipline at build time, making it impossible to add an unclassified Fail site without a build warning.

All four mechanisms ship together in issue #115.

### F.2 EvalFailCode Catalog

#### Design

Every `EvaluationResult.Fail` call site in the evaluator passes an `EvalFailCode` enum member that identifies the failure mode. The enum is internal — invisible to consumers, used only for structural enforcement within the compiler/evaluator boundary.

```csharp
internal enum EvalFailCode
{
    // ── Statically preventable ──────────────────────────────────
    // These MUST have a matching type-checker or proof-engine rule.
    // The [StaticallyPreventable] attribute links to DiagnosticCatalog.

    [StaticallyPreventable("C38")] OperatorTypeMismatch,
    [StaticallyPreventable("C41")] FunctionArgTypeMismatch,
    [StaticallyPreventable("C78")] ConditionalConditionNotBoolean,
    [StaticallyPreventable("C68")] ChoiceLiteralNotInSet,          // DD22
    [StaticallyPreventable("C85")] CollectionAccessorWithoutGuard, // DD21
    [StaticallyPreventable]        UnsupportedUnaryOperator,
    [StaticallyPreventable]        UnsupportedBinaryOperator,
    [StaticallyPreventable]        UnknownFunction,
    [StaticallyPreventable]        UnknownCollectionProperty,
    // ... every statically preventable Fail site gets a member

    // ── Legitimately dynamic ────────────────────────────────────
    // These depend on runtime state — no compiler rule can prevent them.

    [LegitimatelyDynamic] IntegerOverflow,           // DD19
    [LegitimatelyDynamic] DecimalPowEdgeCase,        // DD14
    [LegitimatelyDynamic] DivisionByZero_RuntimeValue,
    [LegitimatelyDynamic] CollectionEmpty_BehindGuard,
    [LegitimatelyDynamic] SqrtNegative_BehindProof,
    [LegitimatelyDynamic] RoundingCastOverflow,      // DD19 scope (G7)
}
```

#### Attributes

Two custom attributes classify each enum member:

- **`[StaticallyPreventable("CXX")]`** — links the evaluator failure to its `DiagnosticCatalog` counterpart. The string argument is the constraint ID (e.g., `"C38"`). When the diagnostic code is not yet assigned (new constructs), the parameterless `[StaticallyPreventable]` overload marks the member as requiring a compiler rule — the sentinel test will fail until one is linked.

- **`[LegitimatelyDynamic]`** — declares that the failure depends on runtime state and has no static counterpart. The sentinel test skips these members.

Both attributes are internal, applied only to `EvalFailCode` members.

#### Fail Signature

The `EvaluationResult.Fail` factory method gains an `EvalFailCode` parameter:

```csharp
internal sealed record EvaluationResult(bool Success, object? Value, EvalFailCode? FailCode, string? Error)
{
    internal static EvaluationResult Ok(object? value) => new(true, value, null, null);
    internal static EvaluationResult Fail(EvalFailCode code, string? detail = null)
        => new(false, null, code, detail);
}
```

The `FailCode` is internal — it does not appear in MCP output, API responses, or any consumer-facing surface. The `Error` string remains for human-readable diagnostics. The enum carries the structural identity; the string carries the context.

#### How New Fail Sites Are Added

When a developer adds a new `EvaluationResult.Fail(...)` call:

1. They must pass an `EvalFailCode` member — the Roslyn analyzer (§ F.5) flags bare string Fail calls.
2. They must add the corresponding enum member to `EvalFailCode`.
3. They must classify it with `[StaticallyPreventable("CXX")]` or `[LegitimatelyDynamic]`.
4. If `[StaticallyPreventable]`, the sentinel test (§ F.7) fails until a matching compiler diagnostic exists and is tested.

This makes classification a conscious architectural decision at the point of authoring, not a post-hoc audit finding.

### F.3 Registry-Driven Function Evaluation

#### Problem

The `FunctionRegistry` is declarative — it specifies function names, overload signatures, accepted types, return types, and argument constraints. The type checker reads this registry. The evaluator does NOT — it has a parallel hand-coded switch in `EvaluateFunction` that independently implements every function body. This is the archetype of the drift problem: two implementations of the same contract, one declarative and one imperative, with no enforcement that they agree.

George's audit identified G5 (`pow(integer, negative)`) as a direct consequence: the registry declares `RequiresNonNegativeProof` on the exponent parameter, but the evaluator's hand-coded `IntegerPow` body does not enforce it — the constraint lives in one implementation but not the other.

#### Design

Extend `FunctionOverload` with an evaluation delegate:

```csharp
internal sealed record FunctionOverload(
    FunctionParameter[] Parameters,
    StaticValueKind ReturnType,
    int? MinArity = null,
    Func<object?[], EvaluationResult>? Evaluator = null);
```

The `Evaluator` delegate receives the evaluated arguments (already type-checked by the registry's parameter declarations) and returns an `EvaluationResult`. Every overload in `FunctionRegistry` supplies its evaluation delegate at registration time:

```csharp
Register(new FunctionDefinition("abs", "Returns the absolute value.",
[
    new([new("value", StaticValueKind.Integer)], StaticValueKind.Integer,
        Evaluator: args => EvaluationResult.Ok(Math.Abs((long)args[0]!))),
    new([new("value", StaticValueKind.Decimal)], StaticValueKind.Decimal,
        Evaluator: args => EvaluationResult.Ok(Math.Abs((decimal)args[0]!))),
    new([new("value", StaticValueKind.Number)], StaticValueKind.Number,
        Evaluator: args => EvaluationResult.Ok(Math.Abs((double)args[0]!))),
]));
```

#### How EvaluateFunction Changes

The evaluator's `EvaluateFunction` method transforms from a hand-coded name switch to a registry lookup:

1. **Look up the function** in `FunctionRegistry` by name. If not found → `EvaluationResult.Fail(EvalFailCode.UnknownFunction, ...)`.
2. **Match the overload** by comparing the runtime argument types against the registry's parameter declarations. If no match → `EvaluationResult.Fail(EvalFailCode.FunctionArgTypeMismatch, ...)`.
3. **Invoke the delegate** on the matched overload: `overload.Evaluator!(evaluatedArgs)`.

The hand-coded function switch — currently ~18 arms dispatching `abs`, `floor`, `ceil`, `round`, `truncate`, `min`, `max`, `clamp`, `pow`, `sqrt`, `approximate`, `toLower`, `toUpper`, `trim`, `startsWith`, `endsWith`, `left`, `right`, `mid` — is deleted entirely. Argument-count checks, argument-type checks, and function-body dispatch all flow through the registry.

#### Why This Eliminates Function-Level Drift

Adding a new function to `FunctionRegistry` requires specifying both the type signature (for the type checker) and the evaluation delegate (for the evaluator) in a single registration call. There is no second location to update, no parallel switch arm to add, and no way for the type checker and evaluator to disagree about which functions exist or what types they accept. The registry is the single source of truth.

The sentinel test (Function Registry Completeness, Appendix E) enforces that every overload has a non-null `Evaluator` delegate. An overload registered with `Evaluator: null` fails the sentinel — type-checking-only registrations without evaluation paths are structurally impossible.

### F.4 Operator Registry

#### Problem

Operator dispatch is the largest surface area in the evaluator — 5 arithmetic operators × 6 type-family combinations × {result type, widening rule, evaluation body} = a dispatch matrix that the evaluator implements imperatively and the type checker implements independently via `TryInferBinaryKind`. Both implementations encode the same semantic rules (which operator × type combinations are legal, what the result type is, what widening applies), but they share no data structure. Drift between them produced George's gaps G2 (unary minus rejects decimal) and G4 (ordered choice comparison).

#### Design

A new `OperatorRegistry` declares the full operator × type-family matrix in one place:

```csharp
internal static class OperatorRegistry
{
    internal sealed record OperatorEntry(
        string Operator,
        StaticValueKind LeftType,
        StaticValueKind RightType,
        StaticValueKind ResultType,
        Func<object, object, EvaluationResult> Evaluate);

    internal sealed record UnaryEntry(
        string Operator,
        StaticValueKind OperandType,
        StaticValueKind ResultType,
        Func<object, EvaluationResult> Evaluate);
}
```

Each entry declares:
- The operator symbol (`+`, `-`, `*`, `/`, `%`, `==`, `!=`, `>`, `>=`, `<`, `<=`).
- The accepted operand type families (e.g., `Integer`, `Decimal`, `Number`).
- The result type (e.g., `Integer` for `long + long`, `Boolean` for `decimal < double`).
- The evaluation delegate.

#### Widening Rules Encoded Declaratively

The operator × type-family matrix encodes widening explicitly rather than imperatively:

| Left | Right | Arithmetic result | Comparison result | Widening |
|------|-------|-------------------|-------------------|----------|
| `Integer` | `Integer` | `Integer` | `Boolean` | None |
| `Decimal` | `Decimal` | `Decimal` | `Boolean` | None |
| `Number` | `Number` | `Number` | `Boolean` | None |
| `Integer` | `Decimal` | `Decimal` | `Boolean` | Integer → Decimal |
| `Integer` | `Number` | `Number` | `Boolean` | Integer → Number |
| `Decimal` | `Number` | **Type error** | `Boolean` | Decimal → Number (comparison only) |

The registry contains entries for each legal combination. Illegal combinations (e.g., `Decimal + Number`) have no entry — both the type checker and evaluator consult the same registry to determine legality.

For unary operators, the registry declares:

| Operator | Operand | Result | Evaluation |
|----------|---------|--------|------------|
| `-` | `Integer` | `Integer` | `-(long)x` |
| `-` | `Decimal` | `Decimal` | `-(decimal)x` |
| `-` | `Number` | `Number` | `-(double)x` |
| `not` | `Boolean` | `Boolean` | `!(bool)x` |

#### Why Operators Are More Complex Than Functions

The operator registry is architecturally more complex than the function registry for three reasons:

1. **Widening.** Functions accept explicit parameter types — `abs(decimal)` takes a `decimal`. Operators accept pairs where one operand may be widened: `long + decimal` widens the `long` to `decimal`. The registry must encode the widening rule per entry, and the evaluator must apply the widening before invoking the delegate.

2. **Lane promotion.** Comparison operators produce `boolean` regardless of operand lane, so `decimal < double` is legal (the decimal is widened to double for comparison, and the result is `boolean` — no numeric lane contamination). Arithmetic operators produce a numeric result in the wider lane, so `decimal + double` is illegal (the result would be an ambiguously-laned numeric value). The registry must encode this distinction: some operator × type combinations are legal for comparisons but illegal for arithmetic.

3. **String and choice dispatch.** `+` is overloaded for string concatenation. `==`/`!=` work on strings and choices. Relational operators work on ordered choices via declaration-position ordinal comparison. These are structurally distinct from numeric dispatch and require their own registry entries with different evaluation delegates.

#### How the Type Checker and Evaluator Both Consume the Registry

The type checker calls `OperatorRegistry.TryGetResultType(operator, leftKind, rightKind)` to determine whether an operator × type combination is legal and what its result type is. The evaluator calls `OperatorRegistry.TryGetEntry(operator, leftType, rightType)` to find the evaluation delegate. Both consult the same data structure — if an entry exists, the combination is legal and evaluable; if no entry exists, both reject it.

### F.5 Roslyn Analyzer

#### Purpose

The Roslyn analyzer is the **default structural enforcement mechanism** for the Precept codebase. It operates at build time, discovering violations during `dotnet build` and surfacing them as IDE red squiggles during editing. Roslyn replaces reflection-based drift tests wherever possible — reflection tests remain only for cross-boundary enforcement that Roslyn cannot reach (e.g., C#↔JSON grammar sync).

The analyzer enforces 11 rules across four categories: evaluator failure classification, exhaustive switch coverage, token metadata completeness, and cross-catalog consistency.

#### Rule Catalog

| Rule ID | Severity | What It Enforces | Replaces |
|---------|----------|-----------------|----------|
| PREC001 | Warning | `EvaluationResult.Fail()` must pass an `EvalFailCode` member — no bare-string overload | New |
| PREC002 | Warning | Switch over `PreceptExpression` subtypes must be exhaustive (TypeChecker + Evaluator) — `default` arm does not count | Convention only |
| PREC003 | Warning | Every `PreceptToken` enum member must have both `[TokenCategory]` and `[Description]` attributes | `AllTokens_HaveCategoryAndDescription` reflection test |
| PREC004 | Warning | Keyword/operator `PreceptToken` members must have `[TokenSymbol]` attribute | `KeywordAndOperatorTokens_HaveSymbol` reflection test |
| PREC005 | Warning | No duplicate `[TokenSymbol]` values across `PreceptToken` enum members | New |
| PREC006 | Warning | Switch over `EvalFailCode` must be exhaustive — `default` arm does not count | New |
| PREC007 | Warning | Switch over `FunctionArgConstraint` must be exhaustive — `default` arm does not count | Convention only |
| PREC008 | Warning | Switch over `StaticValueKind` must be exhaustive — `default` arm does not count | Convention only |
| PREC009 | Warning | Every `[StaticallyPreventable]` `EvalFailCode` member must have a matching `DiagnosticCatalog` code | Sentinel test |
| PREC010 | Warning | `// SYNC:` comments must reference valid `DiagnosticCatalog` codes | `SyncComments_MatchDiagnosticCatalog` reflection test |
| PREC011 | Warning | Declaration record types (e.g., `PreceptField`, `PreceptState`, `PreceptEvent`) must have an `int SourceLine` property | `ModelRecords_HaveSourceLine` reflection test |

All diagnostics are warnings. CI treats warnings-as-errors for the `Precept` project, so violations fail the pipeline. This allows incremental development locally (add the construct, then fix the diagnostic) while preventing unresolved violations from merging.

#### Rule Categories

**Evaluator failure classification (PREC001).** Discovers `EvaluationResult.Fail(...)` call sites and enforces that every call passes an `EvalFailCode` member. A call to `EvaluationResult.Fail(string)` (the old bare-string signature) triggers the diagnostic.

**Exhaustive switch coverage (PREC002, PREC006–PREC008).** These four rules share a common Roslyn pattern: enumerate all subtypes of a sealed type hierarchy (for `PreceptExpression`) or all members of an enum (for `EvalFailCode`, `FunctionArgConstraint`, `StaticValueKind`), find all switch sites over that type, and verify every subtype/member has an explicit case arm. The `default` arm is NOT counted as coverage — explicit arms are required. This catches the category of bug where a new expression form, failure code, or constraint kind is added but switch sites are not updated.

**Token metadata completeness (PREC003–PREC005).** Enforces that the `PreceptToken` enum — the source of truth for syntax highlighting, semantic tokens, and the `precept_language` MCP tool — is fully annotated. PREC003 requires `[TokenCategory]` + `[Description]` on every member. PREC004 requires `[TokenSymbol]` on keyword/operator members. PREC005 catches duplicate symbol values that would create ambiguous tokenization.

**Cross-catalog consistency (PREC009–PREC011).** PREC009 enforces the contract between `EvalFailCode` and `DiagnosticCatalog` — every `[StaticallyPreventable("CXX")]` member must have a matching catalog entry. PREC010 validates `// SYNC:` comments that cross-reference diagnostic codes. PREC011 enforces the structural contract that all declaration record types carry source-line provenance for diagnostic reporting.

#### Reflection Test Retirement

The following reflection-based `CatalogDriftTests` are replaced by Roslyn analyzer rules and will be deleted once the analyzer ships:

| Reflection Test | Replaced By |
|----------------|-------------|
| `AllTokens_HaveCategoryAndDescription` | PREC003 |
| `KeywordAndOperatorTokens_HaveSymbol` | PREC004 |
| `SyncComments_MatchDiagnosticCatalog` | PREC010 |
| `ModelRecords_HaveSourceLine` | PREC011 |

No parallel enforcement — the reflection tests are deleted, not kept alongside the analyzer rules. The shift-left benefit: reflection tests discover problems at test-time in CI (minutes); the Roslyn analyzer discovers them at edit-time in the IDE (instant red squiggles) and enforces at build-time.

#### Integration

All 11 rules live in a single `Precept.Analyzers` project, referenced by the `Precept` project as internal tooling (not a NuGet package):

```xml
<ProjectReference Include="..\Precept.Analyzers\Precept.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

The analyzer runs during every `dotnet build` invocation. It requires no manual invocation, no source-text scanning, and no reflection — it operates on the Roslyn syntax tree directly, which is robust against formatting changes, comment changes, and refactoring.

#### Why Roslyn Analyzer, Not Source Scanning

Appendix E's Evaluator Failure Classification sentinel originally contemplated source-text scanning (regex over C# source) or static enumeration as discovery mechanisms. The Roslyn analyzer supersedes both:

| Mechanism | Fragility | Build integration | False positives |
|-----------|-----------|-------------------|-----------------|
| Source-text scanning (regex) | High — breaks on formatting, comments, string literals containing "Fail" | Test-time only — runs in CI but not during local builds | Moderate — regex cannot distinguish `Fail(string)` from `Fail(EvalFailCode, string)` reliably |
| Static enumeration (manual list) | Medium — requires updating the list when adding Fail sites | Test-time only | None — but misses new sites by definition |
| Reflection-based drift tests | Low — operates on compiled types | Test-time only — runs in CI but not during local builds | None — but still reactive, not proactive |
| **Roslyn analyzer** | **None — operates on typed syntax tree** | **Build-time — runs during `dotnet build` and in IDE** | **None — inspects actual syntax and symbol tables** |

The Roslyn analyzer turns structural enforcement from a reactive test-time check into proactive build-time and edit-time enforcement.

### F.6 Design Decisions

#### DD23: EvalFailCode Enum — Internal, Classified with Attributes

**Decision:** Introduce an internal `EvalFailCode` enum that catalogs every evaluator failure mode. Each member is classified with `[StaticallyPreventable("CXX")]` (linked to `DiagnosticCatalog`) or `[LegitimatelyDynamic]`. The enum is internal — invisible to API consumers and MCP output.

**Rationale:** Free-form `Fail(string)` calls have no structural identity. George's audit required manual source-text scanning to discover and classify 72 Fail sites. The enum provides the stable identity that free-form strings lack, makes classification a first-class architectural concept, and enables automated sentinel tests.

**Alternatives rejected:**
- (a) Public enum visible in `EvaluationResult` — exposes internal classification to consumers, adds API surface commitment before the classification is stable. Premature.
- (b) External classification file (JSON/YAML mapping Fail messages to codes) — decouples the classification from the code, making it easier to drift. The enum-on-the-call-site pattern co-locates the classification with the failure point.
- (c) No enum, sentinel tests only (Option C from the proposal) — detects drift reactively but does not prevent it. Free-form strings remain, and two Fail sites can have the same message with different conditions that the sentinel cannot distinguish.

**Tradeoff accepted:** Every existing Fail call site (~75 sites) must be updated to pass an `EvalFailCode` member. This is mechanical but non-trivial. The enum grows with every new construct — manageable because the growth rate matches the evaluator's surface area growth.

#### DD24: Function Evaluation Is Registry-Driven

**Decision:** Extend `FunctionOverload` with a `Func<object?[], EvaluationResult>? Evaluator` delegate. The evaluator's `EvaluateFunction` method dispatches through the registry instead of a hand-coded switch. Every registered overload must have a non-null `Evaluator` delegate.

**Rationale:** The `FunctionRegistry` already provides a declarative contract for function signatures — the type checker reads it, but the evaluator ignores it and reimplements every function body in a parallel switch. This is the archetype of the drift problem. Extending the registry with evaluation delegates closes the loop: one registration, two consumers, zero divergence. George's gap G5 (`pow(integer, negative)`) is the canonical example — the registry declared `RequiresNonNegativeProof`, but the evaluator's independent switch arm did not enforce it.

**Alternatives rejected:**
- (a) Keep the hand-coded switch and add sentinel tests — detects drift but does not prevent it. New functions would still require two independent implementations.
- (b) Code-generate the evaluator switch from the registry — adds a build step, makes debugging harder, and the generated code still has no structural link back to the registry entry.

**Tradeoff accepted:** Evaluation delegates in a static registry may complicate step-through debugging — the call stack goes through a delegate instead of a named method. Mitigation: each delegate can be a named static method referenced by the registry, preserving debuggability. The hand-coded evaluator's optimization (direct pattern-matching on `is long`) is replaced by registry lookup + delegate invocation — the performance delta is negligible for a business-rule engine.

#### DD25: Operator Dispatch Is Registry-Driven

**Decision:** Introduce an `OperatorRegistry` that declares the full operator × type-family matrix: legal operand combinations, result types, widening rules, and evaluation delegates. Both the type checker and evaluator consume this registry as their single source of truth for operator semantics.

**Rationale:** Operator dispatch is the largest surface area in the evaluator (~200 LOC of binary dispatch, ~30 LOC of unary dispatch). The type checker and evaluator independently implement the same semantic rules — which combinations are legal, what widening applies, what the result type is. George's gaps G2 (unary minus rejects decimal in type checker but evaluator handles it) and G4 (ordered choice comparison not validated) both stem from this independent implementation. A shared registry makes disagreement structurally impossible.

**Alternatives rejected:**
- (a) Registry for functions only, sentinels for operators — the operator set is stable (11 operators), but the type-family matrix grows with each new type. Sentinels would need to enumerate the full matrix anyway; a registry is the same information in a consumable form.
- (b) Operator dispatch table as a static data structure without evaluation delegates — provides type-checker/evaluator agreement on legality but not on evaluation behavior. Evaluation delegates close the full loop.

**Tradeoff accepted:** The operator registry is more complex than the function registry due to widening, lane promotion, and the comparison-vs-arithmetic distinction. Implementation cost is higher. The operator set changes rarely (11 operators, stable since project inception), so the registry's drift-prevention value is lower per-entry than the function registry's. The value is in the type-family dimension: adding a new numeric type or changing widening rules requires updating one registry, not two independent dispatch implementations.

#### DD26: Roslyn Analyzer as Default Structural Enforcement

**Decision:** A single `Precept.Analyzers` project containing 11 Roslyn analyzer rules (PREC001–PREC011) enforces structural discipline across the codebase at build time: evaluator failure classification, exhaustive switch coverage over sealed type hierarchies and enums, token metadata completeness, and cross-catalog consistency. Roslyn is the default enforcement mechanism — reflection-based drift tests are replaced wherever Roslyn can reach.

**Rationale:** DD23 makes failure classification possible; the Roslyn analyzer makes structural discipline mandatory and immediate. The original DD26 scope (2 evaluator-specific rules) addressed only `EvaluationResult.Fail` sites. The expanded scope recognizes that the same shift-left benefit — from test-time discovery to edit-time red squiggles — applies to all structural invariants enforceable from the syntax tree: exhaustive switch coverage, token metadata, cross-catalog references, and declaration-record contracts. Four existing reflection-based `CatalogDriftTests` (`AllTokens_HaveCategoryAndDescription`, `KeywordAndOperatorTokens_HaveSymbol`, `SyncComments_MatchDiagnosticCatalog`, `ModelRecords_HaveSourceLine`) are replaced by analyzer rules (PREC003, PREC004, PREC010, PREC011) and deleted — no parallel enforcement.

**Alternatives rejected:**
- (a) Source-text scanning in tests — fragile, regex-based, test-time only.
- (b) Convention enforcement via code review — the 7 gaps prove this is insufficient.
- (c) Source generator instead of analyzer — generators produce code, not diagnostics.
- (d) Keep reflection tests alongside the analyzer — parallel enforcement provides no value and creates maintenance burden. The analyzer is strictly superior for everything it can reach.
- (e) Separate analyzer projects per category — unnecessary complexity. The 11 rules share infrastructure (symbol resolution, attribute inspection) and the combined project is small.

**Tradeoff accepted:** Adds a `Precept.Analyzers` project with 11 rules to the solution. This is a build dependency — analyzer assemblies are loaded during compilation. The performance impact is negligible: each rule is narrowly scoped to specific syntax patterns. Analyzer tests (verifying that each rule correctly flags violations and passes clean code) add to the test surface — 11 rules × (positive + negative) minimum. Reflection-based drift tests remain only for the C#↔JSON boundary (e.g., FunctionRegistry ↔ TextMate grammar sync) where Roslyn cannot reach.

**See:** Appendix F § F.5 for the full rule catalog and integration details.

### F.7 Conformance Test Obligations

The architecture defined in this appendix requires structural tests beyond the conformance test categories in Appendix E.

#### EvalFailCode Sentinel Test

Reflects over the `EvalFailCode` enum at test time:

1. For every member with `[StaticallyPreventable("CXX")]`: assert that `DiagnosticCatalog` contains constraint `CXX`, and that a conformance test exists proving the type checker emits that diagnostic for the condition the enum member describes.
2. For every member with `[StaticallyPreventable]` (no code): fail — the member is declared as statically preventable but has no linked diagnostic. This forces the developer to either assign a diagnostic code or reclassify as `[LegitimatelyDynamic]`.
3. For every member with `[LegitimatelyDynamic]`: pass — no compiler rule is expected. Optionally assert that a defense-in-depth test exists (evaluator produces `Fail`, not an unhandled exception).
4. For every member with neither attribute: fail — unclassified members are not allowed.

#### Function Registry Evaluator Completeness

Reflects over `FunctionRegistry.AllFunctions` at test time. For every overload in every function definition: assert that `overload.Evaluator` is not null. A null delegate means the overload has a type-checking signature but no evaluation path — a structural hole.

#### Operator Registry Completeness

Reflects over `OperatorRegistry` at test time. For every declared `OperatorEntry` and `UnaryEntry`: assert that the evaluation delegate is not null and that a conformance test exists exercising that entry. An entry without a test means the registry declares a legal combination that has never been verified.

#### Roslyn Analyzer Tests

Standard Roslyn analyzer test infrastructure (`Microsoft.CodeAnalysis.Testing`). Each of the 11 rules (PREC001–PREC011) requires at minimum a positive test (clean code produces no diagnostic) and a negative test (violating code produces the expected diagnostic).

**PREC001 — Bare-string Fail:**
- **Positive:** `Fail(EvalFailCode.SomeCode, "detail")` produces no diagnostic.
- **Negative:** `Fail("bare string")` produces PREC001.

**PREC002 — Exhaustive PreceptExpression switch:**
- **Positive:** Switch with explicit arms for every `PreceptExpression` subtype produces no diagnostic.
- **Negative:** Switch missing one subtype (with only a `default` arm) produces PREC002.

**PREC003 — Token category and description:**
- **Positive:** `PreceptToken` member with both `[TokenCategory]` and `[Description]` produces no diagnostic.
- **Negative:** Member missing either attribute produces PREC003.

**PREC004 — Token symbol on keywords/operators:**
- **Positive:** Keyword `PreceptToken` member with `[TokenSymbol]` produces no diagnostic.
- **Negative:** Keyword member missing `[TokenSymbol]` produces PREC004.

**PREC005 — Duplicate token symbols:**
- **Positive:** All `[TokenSymbol]` values are unique — no diagnostic.
- **Negative:** Two members with `[TokenSymbol("if")]` produces PREC005.

**PREC006 — Exhaustive EvalFailCode switch:**
- **Positive:** Switch with explicit arms for every `EvalFailCode` member produces no diagnostic.
- **Negative:** Switch missing one member (relying on `default`) produces PREC006.

**PREC007 — Exhaustive FunctionArgConstraint switch:**
- **Positive:** Switch with explicit arms for every `FunctionArgConstraint` member produces no diagnostic.
- **Negative:** Missing member produces PREC007.

**PREC008 — Exhaustive StaticValueKind switch:**
- **Positive:** Switch with explicit arms for every `StaticValueKind` member produces no diagnostic.
- **Negative:** Missing member produces PREC008.

**PREC009 — StaticallyPreventable ↔ DiagnosticCatalog:**
- **Positive:** `[StaticallyPreventable("C41")]` member where `DiagnosticCatalog` contains C41 produces no diagnostic.
- **Negative:** `[StaticallyPreventable("C99")]` where C99 does not exist in the catalog produces PREC009.

**PREC010 — SYNC comment references:**
- **Positive:** `// SYNC: C41` where C41 exists in `DiagnosticCatalog` produces no diagnostic.
- **Negative:** `// SYNC: C999` where C999 does not exist produces PREC010.

**PREC011 — Declaration record SourceLine:**
- **Positive:** Declaration record type with `int SourceLine` property produces no diagnostic.
- **Negative:** Declaration record type missing `SourceLine` produces PREC011.

#### FunctionRegistry ↔ Grammar Drift Test

A reflection-based drift test that compares `FunctionRegistry.FunctionNames` against the TextMate grammar's `functionCall` regex pattern in `precept.tmLanguage.json`. This test covers the C#↔JSON boundary that the Roslyn analyzer cannot reach — the grammar file is not C# source. Every function name registered in the C# `FunctionRegistry` must appear as an alternative in the grammar's `functionCall` pattern, and vice versa.

This is a companion to the Roslyn analyzer, not a replacement. It validates the one cross-boundary contract where Roslyn has no jurisdiction.

#### Reflection Test Retirement

The following existing reflection-based tests in `CatalogDriftTests` are deleted once the corresponding Roslyn analyzer rules ship. No parallel enforcement — the analyzer supersedes them:

| Deleted Reflection Test | Replaced By |
|------------------------|-------------|
| `AllTokens_HaveCategoryAndDescription` | PREC003 |
| `KeywordAndOperatorTokens_HaveSymbol` | PREC004 |
| `SyncComments_MatchDiagnosticCatalog` | PREC010 |
| `ModelRecords_HaveSourceLine` | PREC011 |

### F.8 How This Catches George's 7 Gaps

The following walkthrough shows how the architecture defined in this appendix would have auto-detected each gap identified in the Appendix D conformance audit — without a manual audit.

#### G1: Collection accessor on empty collection

**Detection path:** The evaluator's `.min`/`.max`/`.peek` empty-collection Fail sites are classified as `EvalFailCode.CollectionAccessorEmpty` with `[StaticallyPreventable("C85")]`. The sentinel test (§ F.7) asserts that diagnostic C85 exists and that the type checker rejects bare accessor use. Before DD21 introduced the conditional guard requirement, C85 did not exist → sentinel fails → gap surfaced.

#### G2: Unary minus rejects decimal in type checker

**Detection path:** The evaluator's unary minus Fail site for non-numeric operands is `EvalFailCode.UnsupportedUnaryOperator` with `[StaticallyPreventable]`. Under the operator registry (§ F.4), unary minus on `Decimal` has an explicit entry — the type checker reads the registry and accepts it. Before the registry, the type checker independently rejected decimal → the positive-path conformance test (compile `-(decimal)` and evaluate) fails → gap surfaced.

#### G3: Division by zero (already covered)

**Detection path:** The evaluator's division-by-zero Fail site is `EvalFailCode.DivisionByZero_RuntimeValue` with `[LegitimatelyDynamic]`. The sentinel skips it — no compiler rule expected. Separately, C92/C93 cover literal-zero and proof-engine divisor safety at compile time. No gap.

#### G4: Ordered choice non-member literal

**Detection path:** The evaluator's `TryGetChoiceOrdinals` Fail site for non-member values is `EvalFailCode.ChoiceLiteralNotInSet` with `[StaticallyPreventable("C68")]`. The sentinel asserts that C68 covers comparison contexts (not just `set` assignment contexts). Before DD22 extended C68 to comparisons, the sentinel's conformance test (compile `Priority > "Bogus"` and expect rejection) fails → gap surfaced.

#### G5: `pow(integer, negative)` not rejected

**Detection path:** Under registry-driven function evaluation (§ F.3), `pow(integer, integer)` is registered with `RequiresNonNegativeProof` on the exponent parameter. The type checker reads this constraint and rejects negative literal exponents. The evaluator dispatches through the registry delegate, which never receives a negative exponent for integer pow. Before the registry extension, the evaluator's hand-coded `IntegerPow` accepted negative exponents independently → the bypass-path conformance test (skip type checker, evaluate `pow(2, -1)`) reveals divergence → gap surfaced.

#### G6: `sqrt()` missing NaN guard

**Detection path:** Under registry-driven function evaluation (§ F.3), the `sqrt` evaluation delegate includes the NaN guard:

```csharp
Evaluator: args =>
{
    var v = (double)args[0]!;
    if (v < 0) return EvaluationResult.Fail(EvalFailCode.SqrtNegative_BehindProof, "sqrt requires non-negative argument.");
    return EvaluationResult.Ok(Math.Sqrt(v));
}
```

The guard is co-located with the registration — there is no separate method to forget to harden. `EvalFailCode.SqrtNegative_BehindProof` is classified as `[LegitimatelyDynamic]` (the proof engine's C94 covers the static case). The sentinel accepts it.

#### G7: Rounding overflow on `long` cast

**Detection path:** Under registry-driven function evaluation (§ F.3), every rounding function's evaluation delegate uses `checked` casts:

```csharp
Evaluator: args =>
{
    var v = (double)args[0]!;
    try { return EvaluationResult.Ok(checked((long)Math.Floor(v))); }
    catch (OverflowException) { return EvaluationResult.Fail(EvalFailCode.RoundingCastOverflow, "floor result exceeds integer range."); }
}
```

The overflow guard is co-located with the delegate — there is no separate function body where the `checked` cast could be omitted. `EvalFailCode.RoundingCastOverflow` is classified as `[LegitimatelyDynamic]` (DD19 scope). The sentinel accepts it.

#### Summary

| Gap | EvalFailCode member | Classification | Detection mechanism |
|-----|---------------------|----------------|---------------------|
| G1 | `CollectionAccessorEmpty` | `[StaticallyPreventable("C85")]` | Sentinel demands C85 |
| G2 | `UnsupportedUnaryOperator` | `[StaticallyPreventable]` | Operator registry + positive-path test |
| G3 | `DivisionByZero_RuntimeValue` | `[LegitimatelyDynamic]` | No gap — already covered |
| G4 | `ChoiceLiteralNotInSet` | `[StaticallyPreventable("C68")]` | Sentinel demands C68 in comparisons |
| G5 | `FunctionArgTypeMismatch` | `[StaticallyPreventable("C41")]` | Registry-driven — structurally impossible |
| G6 | `SqrtNegative_BehindProof` | `[LegitimatelyDynamic]` | Co-located delegate hardening |
| G7 | `RoundingCastOverflow` | `[LegitimatelyDynamic]` | Co-located delegate hardening |

