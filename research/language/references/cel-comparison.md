# Precept vs CEL (Common Expression Language) — Language-Level Comparison

**Date:** 2026-04-10
**Source:** Frank (Lead/Architect & Language Designer) analysis, grounded in [CEL language spec](https://github.com/google/cel-spec/blob/master/doc/langdef.md), [CEL overview](https://cel.dev/overview/cel-overview), `docs/PreceptLanguageDesign.md`, and the expression-expansion research library.
**Proposals informed:** #9, #10, #15, #16, #31

---

## Why CEL Is a Relevant Comparator

CEL and Precept occupy different positions on the same spectrum — non-Turing-complete declarative languages with safety guarantees. CEL is an embeddable expression evaluator for policy/config (Google infrastructure — access control, routing, security policies). Precept is a domain integrity engine. Both are side-effect-free, terminating, and strongly-typed. Their contrasting design choices illuminate what matters at each position.

---

## Summary Table

| Dimension | CEL | Precept |
|-----------|-----|---------|
| Category | Embeddable expression evaluator | Domain integrity engine |
| Expression surface | Rich (ternary, comprehensions, regex, functions) | Minimal — expanding via proposals #9-16 |
| Type system | Gradual typing; int/uint/double/bytes/protobuf | Static; number/string/boolean |
| Collection model | list/map with iteration macros | set/queue/stack with fixed accessors + mutation verbs |
| Safety bound | Terminating, but exponential via macros | Terminating, strictly linear |
| Extension | Open (host-provided custom functions) | Closed (fixed vocabulary) |
| Logical semantics | Commutative (non-deterministic evaluation order) | Left-to-right short-circuit (deterministic) |
| Error model | Propagation + absorption in logical operators | Structured enumerated outcomes + mandatory `because` reasons |
| Embedding | Leaf expression evaluator (Go/Java/C++) | Entity governance engine (.NET) |
| Lifecycle | None | Full state machine + stateless entity support |
| Compile-time | Optional type-checking | Mandatory type-checking + structural analysis |

---

## Expression Model

### Operator Comparison

| Category | CEL | Precept |
|----------|-----|---------|
| Arithmetic | `+`, `-`, `*`, `/`, `%` | `+`, `-`, `*`, `/`, `%` |
| Comparison | `==`, `!=`, `<`, `<=`, `>=`, `>`, `in` | `==`, `!=`, `<`, `<=`, `>=`, `>`, `contains` |
| Logical | `&&`, `||`, `!` (commutative semantics) | `&&`, `||`, `!` → `and`, `or`, `not` (#31) (deterministic short-circuit) |
| Membership | `in` (lists and maps) | `contains` (set, queue, stack) |
| Conditional | `? :` (ternary) | None (proposal #9 targets `if...then...else`) |
| String concat | `+` | `+` (already implemented) |
| Index access | `e[i]`, `e[key]` | Not supported |

### Comprehension Macros

CEL provides `e.all(x, p)`, `e.exists(x, p)`, `e.exists_one(x, p)`, `e.map(x, t)`, `e.filter(x, p)`. These are macro-expanded comprehensions that introduce bound variables and iterate over collections. They are the **only avenue for exponential behavior** (acknowledged by the CEL spec itself).

Precept has no iteration. Collections are queried through fixed accessors (`.count`, `.min`, `.max`, `.peek`, `contains`) and mutated through explicit verbs (`add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`).

---

## Type System

| Dimension | CEL | Precept |
|-----------|-----|---------|
| Typing discipline | Dynamic at runtime; optional type-check before execution | Fully static; mandatory compile-time type-checking |
| Scalar types | `int` (i64), `uint` (u64), `double` (f64), `bool`, `string`, `bytes`, `null_type` | `number`, `boolean`, `string` |
| Numeric model | Three distinct types; no implicit conversion; heterogeneous numeric equality at runtime | Single `number`; no conversion needed |
| Collection types | `list(A)`, `map(K, V)` — parameterized, nestable | `set of T`, `queue of T`, `stack of T` — scalar inner types only |
| Nullability | `null_type` is a value; any variable can be null | Explicit `nullable` annotation; compiler enforces null-flow narrowing (C42) |
| Type conversion | Explicit: `int()`, `uint()`, `double()`, `string()`, `bool()` | None |
| Abstract types | `timestamp`, `duration` (protobuf well-known types) | None |
| Type reification | `type(x)` returns the type as a first-class value | No runtime type introspection |

Precept's single `number` eliminates mixed-numeric errors. Its explicit nullability is stricter — `field X as number default 0` *cannot be null*, enforced structurally.

---

## Logical Operator Semantics (Critical Difference)

**CEL uses commutative (non-deterministic) evaluation for `&&` and `||`.** The spec: "If one operand determines the result, the other may or may not be evaluated. Errors in the non-determining operand are ignored." So `false && error` = `false`, and `error && false` also = `false`. Evaluation order is **undefined**.

**Precept uses deterministic left-to-right short-circuit.** `X && Y` evaluates X; if false, Y is never evaluated. Same as C#, Python, SQL.

This matters for null-narrowing. `when Score != null && Score >= 680` only type-checks in Precept because the compiler *knows* the right operand is only reached when `Score` is non-null. Under CEL's commutative semantics, this pattern is unsound — the runtime might evaluate `Score >= 680` first.

CEL's commutative semantics trade determinism for partial-data tolerance. Precept's Principle #1 makes the opposite trade.

---

## String Operations

| Operation | CEL | Precept |
|-----------|-----|---------|
| Equality | `==`, `!=` | `==`, `!=` |
| Contains | `string.contains(sub)` | Not yet (#15) |
| Starts/ends with | `startsWith()`, `endsWith()` | Not yet (#16) |
| Regex | `matches()` (RE2, linear-time) | Excluded (decidability risk) |
| Length | `size(string)` / `string.size()` | Not yet (#10) |
| Concatenation | `+` | `+` (already implemented) |

Proposals #10, #15, #16 would match CEL's built-in surface minus regex.

---

## Safety Guarantees

| Property | CEL | Precept |
|----------|-----|---------|
| Side-effect-free | Yes | Yes (expressions); engine mutates via governed action verbs |
| Termination | Yes — macros are the only avenue for computation | Yes — no loops, no macros, no recursion |
| Complexity (no macros) | O(P × I) | Strictly linear in definition size |
| Complexity (with macros) | **Exponential** — nested comprehension macros | N/A |
| Regex | RE2 (linear-time guarantee) | Excluded entirely |
| Determinism | Logical operators non-deterministic | Fully deterministic (Principle #1) |

---

## Extension Model

CEL is designed for extension — host applications register custom functions, add types, extend overloads. This is a first-class design goal.

Precept allows no extension. The vocabulary is fixed (~50 keywords, ~23 symbols). This is what makes compile-time verification, deterministic evaluation, and complete IntelliSense possible.

---

## What Precept Has That CEL Doesn't

The primary differentiator is governed integrity — structural prevention on every operation, not just the state machine. CEL evaluates an expression against a presented context; Precept ensures that no operation can produce an entity configuration that violates its declared rules.

- State machines (`state`, `from...on...transition`)
- Lifecycle-scoped constraints (`in`/`to`/`from` assert)
- Field declarations with types, defaults, nullability
- Mandatory constraint reasons (`because "..."`)
- Transition/mutation pipeline with rollback
- Editable field declarations (`in State edit Field`)
- Inspect API (non-mutating complete preview of all possible actions)
- Compile-time structural analysis (unreachable states, contradictions)
- Structured enumerated outcomes (`Transition`, `Rejected`, `ConstraintFailure`, etc.)

---

## What CEL Has That Precept Doesn't

- Ternary conditional expression (Precept: proposal #9 — `if...then...else` keyword form)
- Comprehension macros — `all`, `exists`, `filter`, `map` (Precept: deliberate exclusion)
- Regex matching via RE2 (Precept: deliberate exclusion)
- Extension functions (Precept: deliberate exclusion — closed vocabulary)
- Protobuf integration, field presence testing via `has(e.f)`
- Explicit type conversion functions
- Timestamp and duration types
- List/map literals, index access
- Multi-language runtimes (Go, Java, C++)

---

## What Precept Can Learn from CEL

1. **Conditional expressions are essential.** CEL's ternary is its most-used construct beyond basic comparisons. Validates proposal #9's priority.

2. **A small, fixed string-function surface is sufficient.** CEL's built-in `contains`, `startsWith`, `endsWith`, `size` — exactly what proposals #10/#15/#16 target — is enough for real policy evaluation without regex.

3. **Commutative logical operators are a cautionary tale.** CEL's non-deterministic `&&`/`||` is a constant source of user confusion. Google mitigates this with documentation and `?:` rewrite guidance. Precept's deterministic short-circuit + keyword migration (#31) is the right call.

4. **`has()` is not needed.** CEL's `has(e.f)` handles structurally absent protobuf fields. Precept's explicit nullability + mandatory defaults means presence ambiguity never arises — `Field != null` is the complete equivalent.

5. **Comprehension macros validate collection-query demand but Precept should not adopt them.** CEL's exponential worst-case cost is the price. If quantification becomes necessary, use fixed-form constructs with bounded complexity.

---

## Where Precept Should Stay Different

1. **No comprehension iteration** — eliminates exponential-cost pathway
2. **No extension functions** — preserves closed-world compile-time analysis
3. **No regex** — zero sample-corpus demand; decidability risk
4. **No gradual typing** — mandatory type-checking is a product feature (Principle #8)
5. **No runtime type introspection** — every type is statically known
6. **Deterministic logical operators** — Principle #1 is non-negotiable

---

## Tensions Surfaced

1. **Temporal types.** CEL has `timestamp`/`duration`. Precept's sample corpus has no temporal fields — but insurance claims have deadlines, loan applications have expirations, work orders have SLA windows.
2. **Map/dictionary collections.** CEL has `map(K, V)`. More complex entity governance (fee schedules, configuration objects) might need key-value structures.
3. **String concatenation with nullable operands.** Both languages support string `+`. Precept's type checker rejects `+` when either operand is `string nullable` — classified as L12 (moderate gap) in the expression audit.
