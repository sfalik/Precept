# Expression Evaluation

**Research date:** 2026-04-04 (original); 2026-04-08 (expanded)  
**Authors:** George (Runtime Dev), Frank (Lead/Architect)  
**Relevance:** Principled ways to expand Precept's expression system while keeping semantics decidable and AI-readable. This is the formal/theory companion to the domain-first research in [expression-expansion-domain.md](../expressiveness/expression-expansion-domain.md).

---

## Formal Concept

Precept's expression language is a **many-sorted first-order decidable fragment** — it has three base sorts (number, string, boolean), typed identifiers, a fixed set of operations per sort, and no quantifiers or recursion. This is a conservative, intentional design choice.

PLT provides a well-understood taxonomy for expanding such systems:

| Expansion Type | Formal Category | Decidability Risk | Precept proposals |
|----------------|-----------------|-------------------|-------------------|
| Additional comparison operators | Same fragment, new symbols | None | — (already complete) |
| Boolean connectives | Boolean algebra extension | None | #31 (`and`/`or`/`not` — token swap only) |
| Conditional value selection | Total function, case split | None | #9 (`if...then...else`) |
| String predicates (length, prefix, substring) | Regular language predicates | None if pattern-free | #10 (`.length`), #15 (`.contains()`), #16 (`startsWith`, `endsWith`) |
| String pattern matching (regex) | Omega-regular expressions | Decidable but expensive (ReDoS risk) | **Excluded** — no proposal |
| Bounded built-in functions | Closed-form total functions | None (deterministic, no recursion) | #16 (`abs`, `round`, `min`, `max`, etc.) |
| Arithmetic over reals | Presburger arithmetic | Decidable | — (already present) |
| Quantified constraints (`all x in S`) | First-order logic over sets | Decidable over finite domains | **Deferred** — high grammar cost |
| User-defined functions | Lambda calculus fragment | Undecidable in general | **Permanently excluded** |

Precept's current expression set sits in the lowest-risk zone: standard Boolean algebra over three sorts with arithmetic, comparison, and membership. Every proposed addition should be evaluated against this taxonomy.

### Why the proposed expansions stay decidable

The five open proposals (#9, #10, #15, #16, #31) all fall within the **None** risk column:

1. **Conditional expressions** (`if C then T else E`) are total case splits: `C` is boolean, both branches produce a typed value, exactly one is selected. This is syntactic sugar over an existing evaluation-time branch — the evaluator already selects between transition rows via guards. Conditional expressions move the branch from the row level to the value level. No new decidability concern.

2. **String predicates** (`.length`, `.contains()`, `startsWith()`, `endsWith()`) are regular-language predicates as classified by Veanes et al. (2021). They compose with Boolean operators without leaving the decidable fragment. The constraint: arguments must be compile-time string literals (not variables or expressions) to keep the predicate alphabet finite.

3. **Bounded built-in functions** (`abs`, `round`, `floor`, `ceil`, `min`, `max`) are closed-form total functions over the `number` sort. They do not introduce recursion, iteration, or unbounded computation. Each function maps a finite numeric input to a deterministic numeric output. The type checker validates arity and argument types statically.

4. **Keyword logical operators** (`and`/`or`/`not`) are a token swap for `&&`/`||`/`!`. Identical Boolean algebra semantics. No decidability change whatsoever.

---

## Examples from Well-Designed Languages

### 1. Alloy — Typed Relational Expressions

Alloy expressions are typed over signatures (sets/relations). The expression language supports:
- Relational join: `a.b` (navigation)
- Set operators: `+` (union), `&` (intersection), `-` (difference), `in` (subset), `=` (equality)
- Cardinality: `#s` (count)
- Integer arithmetic over bounded integers (Alloy Int, decidable)

**Design principle:** Every operator has a well-defined sort. The type checker rejects `a.b` unless `a` is a relation with codomain compatible with `b`. Sort-checked operators are the safest way to expand expression systems.

### 2. Kotlin — Smart Cast and Type Narrowing

```kotlin
fun process(value: String?) {
  if (value != null && value.length > 3) {
    println(value.uppercase())  // value is String (non-null) here
  }
}
```

After `value != null`, Kotlin's flow-sensitive type checker narrows `value` to `String` — the type changes within the branch. The expression language doesn't change; the type-checker's treatment of the expression result does.

**Design principle:** Type narrowing is an expression-checker extension, not an expression-grammar extension. It adds static guarantees without changing the surface language.

### 3. TypeScript — Discriminated Union Narrowing

```typescript
type Status = "pending" | "approved" | "rejected";
function handle(status: Status) {
  if (status === "approved") {
    // status: "approved" — narrowed to literal type
  }
}
```

Comparison with a string literal narrows the type to the literal value within the branch. This is pure static analysis — the expression system (`===`) is unchanged.

**Design principle:** String literal comparison operators are the safe entry point for string predicate expansion. No new syntax; richer static guarantees.

---

## Precept's Current Expression System

The current expression grammar (from `PreceptParser.cs` analysis):

| Level | Operators |
|-------|-----------|
| Atoms | NumberLiteral, StringLiteral, BoolLiteral, NullLiteral, Identifier, DottedIdentifier, ParenExpr |
| Unary | `!` (boolean not), `-` (numeric negation) |
| Multiplicative | `*`, `/`, `%` |
| Additive | `+`, `-` |
| Comparison | `==`, `!=`, `>`, `>=`, `<`, `<=`, `contains` |
| Logical AND | `&&` |
| Logical OR | `\|\|` |

Collection property accessors: `.count`, `.min`, `.max`, `.peek`

**What's already here that's underutilized:**  
`%` (modulo) is in the grammar but rarely appears in samples. Arithmetic is fully operational. The comparison set is complete for numbers. `contains` handles set membership.

**What's conspicuously absent:**

1. **String predicates**: No `startsWith`, `endsWith`, `matches`, `length` — string comparison is limited to `==`, `!=`. The only non-null string test in samples is `Name != ""` (which is `!=` comparison, already present).

2. **Null-safe navigation**: `Field?.property` doesn't exist. Nullable collection fields require explicit null guards before accessing `.count`.

3. **String interpolation**: No format expressions. Not needed for constraint evaluation.

4. **Arithmetic aggregate over collections**: `Tags.count` works. `Tags.min`, `Tags.max` work for numeric inner types. There's no `sum` accessor.

---

## Principled Expansion Paths

### Path 1: String Predicates (Low Cost, Low Risk)

**Candidates:**
- `Field.length` — string length accessor (analogous to `.count` for collections)
- `Field startsWith "prefix"` — new binary operator
- `Field endsWith "suffix"` — new binary operator
- `Field matches "pattern"` — regex matching (medium risk, see below)

**Type rules:**
- `Field.length`: `string → number` — adds a new dotted accessor
- `startsWith`/`endsWith`: `string × string → boolean` — same grammar level as `contains`
- `matches`: `string × string_literal → boolean` — the argument must be a regex literal to keep decidability

**Risk analysis:**
- `length`, `startsWith`, `endsWith`: No decidability risk. These are regular language predicates — the symbolic automaton framework confirms they are decidable when composed with Boolean operators. **Implementation cost: Low.** Add new dotted accessor to the evaluator and type-checker; add tokens for `startsWith`/`endsWith`.
- `matches` with unrestricted patterns: Risk of catastrophic backtracking (ReDoS) if the pattern is user-supplied. Must restrict to compile-time literal patterns only, and consider a safe regex subset (no backreferences). **Implementation cost: Medium. Recommend deferring.**

**Impact on samples:**

Many string validations currently use `Field != ""`. With `length > 0` as an alias, expressiveness doesn't change. The main value of `startsWith`/`endsWith` is in domain-specific patterns:
```precept
on SubmitApplication assert Applicant startsWith "USR-" because "Must be a valid user ID format"
```

### Path 2: Boolean Short-Circuit Completeness (Already Present)

`&&` and `||` already short-circuit. `!` (negation) is available. De Morgan's laws are available to the author. **No expansion needed here.**

### Path 3: Comparison Operator Completeness (Already Present)

All six comparison operators are present. **No expansion needed.**

### Path 4: Null-Safe Accessor for Collections

Current pattern:
```precept
when Tags != null && Tags.count > 0
```

Nullable collection fields don't exist in the current type system (collections default to empty, not null). This is a non-issue for the current type model.

**If nullable collections were added:** The `?.` operator (`Tags?.count ?? 0`) would be needed to avoid null-access errors. This is a medium-cost type system extension. **Not needed until nullable collections are proposed.**

### Path 5: Arithmetic Aggregate Accessors (Medium Cost, Medium Risk)

`sum` over a numeric set/queue/stack:
```precept
invariant LineItems.sum <= CreditLimit because "Total cannot exceed credit limit"
```

**Risk:** `sum` is not representable as interval analysis — the analyzer cannot statically bound it from field declarations alone. It would appear as a non-reducible expression, assumed satisfiable (current behavior for cross-field expressions). Runtime evaluation is straightforward. **Implementation cost: Low (runtime) + Medium (type-checker).**

### Path 6: Quantified Expressions (High Cost)

```precept
invariant all Tag in Tags : Tag.length > 0 because "Tags cannot be empty strings"
```

This is first-order quantification over a collection. It is decidable over finite domains (the collection is bounded at runtime), but adds significant parser complexity and a new evaluation strategy (universally quantified predicates must evaluate over each element). **Cost: High. Out of scope for near-term.**

---

## Decidability Guarantee

Precept's current expression set is **decidable** because:
1. No recursion or user-defined functions
2. Arithmetic is bounded by the numeric literals used in the precept (Presburger arithmetic over bounded integers)
3. `contains` is a membership check on a finite set
4. Collection accessors return scalar values that participate in existing scalar expressions

Any proposed addition must maintain this guarantee. The safe test:
> **An expression is safe if every subexpression has a type and every type has a finite domain analysis algorithm.**

String predicates with literal arguments pass. Regex with unrestricted patterns fail. Quantifiers over collections conditionally pass (finite collections at runtime, but not statically bounded).

---

## AI-Readability Constraint

Precept's design principle 12 requires AI authoring to be reliable. This constrains expression expansion differently than decidability:

- New operators should have **natural language keywords** where possible: `startsWith` over `sw`, `contains` over `in`. Keywords are tokenized and recognized by the language server; symbols are not.
- New operator semantics should be **monotone**: adding `startsWith "X"` to a guard makes it more restrictive, never less. Non-monotone operators (negation of complex predicates) are hard for AI authors to reason about.
- New accessors should follow the **existing dotted pattern**: `Field.length`, not a postfix operator or a global function call.

---

## Vocabulary Taxonomy — Formal Framework

The expression expansion introduces four distinct vocabulary categories. Each has different formal properties, different grammar integration costs, and different extension rules. This taxonomy is the authoritative classification for any future expression-surface discussion.

### 1. Operators (Binary and Unary)

**Formal category:** Infix/prefix symbols or keywords at a defined precedence level in the expression grammar.

**Grammar integration:** Operators are parsed as part of the precedence-climbing algorithm. Adding a new operator means adding a token and a precedence level.

**Existing inventory:**

| Level | Operators | Sort constraints |
|---|---|---|
| Unary (7) | `!` / `not`, unary `-` | Boolean, Number respectively |
| Multiplicative (6) | `*`, `/`, `%` | Number × Number → Number |
| Additive (5) | `+`, `-` | Number × Number → Number; String × String → String (concatenation) |
| Comparison (4) | `==`, `!=`, `>`, `>=`, `<`, `<=`, `contains` | Various; `contains` is Collection × Inner → Boolean |
| Logical AND (2) | `&&` / `and` | Boolean × Boolean → Boolean |
| Logical OR (1) | `||` / `or` | Boolean × Boolean → Boolean |

**Extension criteria:** An operation should be an operator when it is (a) binary or unary, (b) has natural precedence placement relative to existing operators, and (c) reads fluently inline without parenthesized arguments.

### 2. Accessors (Dotted Property Reads)

**Formal category:** Parameterless property reads on typed identifiers, parsed as `Identifier.Member` atoms.

**Grammar integration:** Accessors are resolved during atom parsing. The parser produces a `PreceptIdentifierExpression` with `Member` set. No new AST node needed.

**Decidability property:** Each accessor is a **total function from its receiver type to a scalar type** with a fixed, known signature. The type checker injects them into the symbol table at compile time.

| Accessor | Receiver | Return sort | Totality |
|---|---|---|---|
| `.count` | `set<T>`, `queue<T>`, `stack<T>` | `number` | Total (empty → 0) |
| `.min`, `.max` | `set<T>` (numeric inner) | inner type | Partial (empty set) |
| `.peek` | `queue<T>`, `stack<T>` | inner type | Partial (empty collection) |
| `.length` (proposed) | `string` | `number` | Total (empty string → 0) |

**Extension criteria:** An operation should be an accessor when it is (a) parameterless, (b) scoped to a specific receiver type, and (c) returns a scalar value. The accessor surface should remain small and discoverable — a handful of well-known properties per type.

### 3. Methods (Dotted Calls with Arguments)

**Formal category:** Call expressions with a typed receiver and parenthesized arguments, parsed as `Identifier.method(args)`.

**Grammar integration:** Methods require the parser to recognize `.Identifier(` as a method-call form on a preceding expression. This is a new grammar form not present in the current parser — it extends the atom/postfix parsing to handle method calls on typed receivers.

**Decidability property:** Methods are **total functions from (receiver type × argument types) to a result type**, with the same decidability as accessors provided arguments are compile-time constants or typed expressions within the decidable fragment.

| Method | Receiver | Arguments | Return sort | Proposed |
|---|---|---|---|---|
| `.contains(sub)` | `string` | `string` (literal) | `boolean` | #15 |

**Extension criteria:** An operation should be a method when it is (a) scoped to a specific receiver type, (b) requires at least one argument, and (c) reads naturally as a property of the receiver rather than a free-standing computation.

### 4. Functions (Prefix Calls)

**Formal category:** Named function calls with parenthesized arguments, parsed as `functionName(arg1, arg2, ...)`.

**Grammar integration:** Functions require a new `PreceptFunctionCallExpression(Name, Arguments)` AST node and a static function registry in the type checker. The parser inserts a function-call atom alternative before `DottedIdentifier` in the `Atom` combinator, using lookahead on `(` to distinguish `Name(` from `Name` followed by other tokens.

**Decidability property:** All functions in the registry are **closed-form total functions over the base sorts**. No recursion, no iteration, no unbounded computation. Each function maps a fixed number of typed inputs to a deterministic typed output. The registry is finite and known at compile time.

| Function | Signature | Decidability | Proposed |
|---|---|---|---|
| `abs(x)` | `number → number` | Total, closed-form | #16 |
| `floor(x)` | `number → number` | Total, closed-form | #16 |
| `ceil(x)` | `number → number` | Total, closed-form | #16 |
| `round(x)` | `number → number` | Total, closed-form | #16 |
| `min(a, b)` | `number × number → number` | Total, closed-form | #16 |
| `max(a, b)` | `number × number → number` | Total, closed-form | #16 |
| `startsWith(s, p)` | `string × string → boolean` | Regular predicate | #16 |
| `endsWith(s, p)` | `string × string → boolean` | Regular predicate | #16 |
| `toLower(s)` | `string → string` | Total, closed-form | #16 |
| `toUpper(s)` | `string → string` | Total, closed-form | #16 |
| `trim(s)` | `string → string` | Total, closed-form | #16 |

**Extension criteria:** An operation should be a function when it is (a) not scoped to a single receiver type, or (b) takes multiple arguments of different types, or (c) would read unnaturally as a method or accessor.

### Boundary enforcement

The four categories form a strict hierarchy of grammar cost and extension risk:

```
Operators < Accessors < Methods < Functions
(existing)   (1 new)    (1 new form)  (new AST node)
```

Each new category should be justified by the precedent survey and sample-corpus evidence before introduction. The domain research document ([expression-expansion-domain.md](../expressiveness/expression-expansion-domain.md)) establishes that all four categories are needed for the proposed expression expansion.

---

## Conditional Expression Semantics

`if C then T else E` is a **total expression** — it always produces a value.

### Type rules

```
Γ ⊢ C : boolean    Γ ⊢ T : τ    Γ ⊢ E : τ
─────────────────────────────────────────────
         Γ ⊢ if C then T else E : τ
```

Both branches must produce the same type `τ`. The condition must be boolean. The result type is `τ`.

### Null narrowing in branches

If the condition performs a null check (`X != null`), the `then` branch sees `X` narrowed to non-null. This extends the existing `&&` narrowing model:

```
Γ ⊢ X : τ | null    C = (X != null)
──────────────────────────────────────
Γ, [X : τ] ⊢ T : σ    Γ ⊢ E : σ
──────────────────────────────────────
Γ ⊢ if X != null then T else E : σ
```

### Evaluation semantics

Short-circuit: evaluate `C`; if true, evaluate and return `T`; otherwise evaluate and return `E`. The unevaluated branch has no effect. Both branches are type-checked regardless of which is evaluated at runtime.

### Precedence

`if...then...else` has the lowest precedence in the expression grammar (below `or`/`||`). Nesting requires parentheses: `if A then (if B then 1 else 2) else 3`.

---

## Function-Call Semantics in Constrained Expression Languages

### Static dispatch model

Precept's function registry uses **name + arity** for dispatch. The type checker looks up the function by name, validates arity, checks argument types against the signature, and produces the return type. There is no dynamic dispatch, no overload resolution by subtyping, and no polymorphism.

For functions that will support multiple numeric types (when `integer` and `decimal` ship), overload resolution selects the **most-specific signature** — `abs(integer)` is preferred over `abs(number)` when the argument is statically known to be integer. This is standard overload resolution by specificity, not ad-hoc polymorphism.

### Compile-time constant constraints

Some functions impose compile-time constraints on arguments: `round(decimal, N)` requires `N` to be a non-negative integer literal. The type checker validates this constraint during compilation. The rationale: if `N` were a variable, the scale of the result would be unknown at compile time, defeating static type inference.

### Totality and error handling

All functions in the registry are total over their declared domain:

- `abs(x)` is total for all `number` values.
- `round(x)` is total using banker's rounding (`MidpointRounding.ToEven`).
- `min(a, b)` and `max(a, b)` are total for all pairs.

If a nullable argument bypasses the type checker (should not happen), the evaluator produces a runtime error with a clear message rather than propagating null. This is a defensive guard, not a design choice — the type checker should reject nullable arguments statically.

### Formal precedent

FEEL (DMN) provides the closest formal model: a fixed function library with named functions, static type checking, and deterministic evaluation. FEEL's function library is larger (50+ functions) but shares the same architectural properties:
- No user-defined functions
- No higher-order functions
- All functions are total over their declared types
- The evaluator dispatches by name and arity

Cedar (AWS) provides the conservative counter-model: only 4 extension functions (`ip()`, `decimal()`, `isIpv4()`, `isIpv6()`), all type-constructors rather than computation. Cedar's position is that computation belongs in the policy evaluation engine, not in the policy language. Precept's position is different: `set` expressions compute values, which justifies computation functions.

---

## String Predicate Theory

String predicates in Precept's expression language fall into two categories from symbolic automata theory (Veanes et al., 2021):

### Character-count predicates (`.length`)

`.length` maps a string to a natural number. Combined with arithmetic comparison (`>=`, `<=`), this produces **length constraints** — a well-studied class in string constraint solving. Length constraints are decidable and compose freely with Boolean operators.

The key property: `.length` is **independent of string content**. It constrains *how much* data, not *what* data. This makes it the safest string predicate to add.

### Substring predicates (`.contains()`, `startsWith()`, `endsWith()`)

These are **regular-language membership tests** when the argument is a compile-time string literal. `s.contains("@")` is equivalent to testing membership in the regular language `Σ*@Σ*`. `startsWith(s, "USR-")` tests membership in `USR-Σ*`. These compose with Boolean operators without leaving the decidable fragment.

The constraint: if the argument were a variable (another field), the predicate would become a **string equation** (`s contains t` where both are variables), which is decidable but significantly more complex (word equations, Makanin's algorithm). Precept's v1 restriction to literal arguments avoids this entirely.

### Pattern matching (regex) — excluded

Unrestricted regex matching falls into the **omega-regular** category. While decidable in theory, it introduces ReDoS risk (catastrophic backtracking) when patterns are not carefully constrained. FEEL and Cedar both omit regex. Precept follows their lead: substring predicates with literal arguments cover the documented sample-corpus needs without regex risk.

---

## Null Safety in the Expanded Expression Surface

### Current model

Precept tracks nullability via `StaticValueKind.Null` flags in the type checker. The `&&` operator provides null narrowing: in `X != null && X.count > 0`, the right operand sees `X` narrowed to non-null.

### Extension for new constructs

The null safety model extends uniformly to all new expression forms:

| Construct | Null input behavior | Type checker action |
|---|---|---|
| `if C then T else E` | `C` may narrow nullables in `then` branch | Apply narrowing context to `T` |
| `Field.length` | Nullable `Field` → compile error | Reject unless narrowed (`Field != null && Field.length > 0`) |
| `Field.contains(sub)` | Nullable `Field` → compile error | Reject unless narrowed |
| `abs(x)` | Nullable `x` → compile error | Reject — function signatures require non-null |
| `round(x)` | Nullable `x` → compile error | Reject |
| `startsWith(s, p)` | Nullable `s` → compile error | Reject unless narrowed |

The policy is **conservative rejection**: nullable inputs are always compile errors unless the author has narrowed first. No implicit coercion to default values. This is consistent with the existing collection accessor model (collection fields default to empty, never null) and the computed-fields research contract (#3 — nullable accessor safety).

### Null-coalescing (`??`) — future

When a null-coalescing operator is proposed, it will integrate with this model as a **narrowing operator**: `X ?? default` narrows `X` from `τ | null` to `τ` by providing a fallback value. The result type strips the `Null` bit. This interacts with the type checker's `StaticValueKind` flag system — the result kind is `leftKind & ~StaticValueKind.Null`.

---

## Implementation Cost Summary

| Addition | Parser Cost | Evaluator Cost | Type-checker Cost | Risk | Proposal |
|----------|------------|---------------|------------------|------|----------|
| `and`/`or`/`not` keywords | Low (token swap) | None | None | None | #31 |
| `Field.length` accessor | Low | Low | Low | None | #10 |
| `if...then...else` expression | Medium (new atom form) | Low | Medium (branch type unification) | Low | #9 |
| `.contains(sub)` method | Medium (new method-call form) | Low | Low | Low | #15 |
| `abs`/`round`/`min`/`max` functions | Medium (new AST node) | Low | Medium (function registry) | Low | #16 |
| `startsWith`/`endsWith` functions | Included in #16 | Low | Low | None | #16 |
| `toLower`/`toUpper`/`trim` functions | Included in #16 | Low | Low | None | #16 |
| `matches` (literal regex only) | Medium | Medium | Medium | ReDoS if open | **Excluded** |
| `sum` collection accessor | Low | Low | Medium (no interval analysis) | None | Deferred |
| Quantified expressions | High | High | High | Scope leakage | Deferred |
| Nullable collection support + `?.` | Medium | Medium | Medium | Type model change | Deferred |

---

## Semantic Risks Specific to Precept

1. **Constraint subject extraction**: `ExpressionSubjects.Extract()` walks the AST to find field/arg references for violation targets. New expression forms — conditional expressions, method calls, function calls — must be handled in this walker or violation targets will be misattributed. The conditional expression is the most important case: both branches may reference different fields, and the walker must traverse both.

2. **Type coercion for new types**: The evaluator handles `JsonElement` unwrapping for external data. New operators, accessors, methods, and functions must handle JSON string inputs correctly — `string.Length` works on CLR strings but may need a guard against `JsonElement` values that haven't been unwrapped.

3. **Operator precedence placement**: The `if...then...else` expression has the lowest precedence (below `or`). Function calls and method calls parse at atom level (highest precedence). `and`/`or`/`not` inherit the existing `&&`/`||`/`!` precedence levels. No precedence conflicts arise.

4. **AI prompt fidelity**: Every new expression construct must appear simultaneously in the parser, evaluator, type checker, `precept_language` MCP tool response, and language server completions. If any one is missing, AI agents will not know the construct exists. The function registry should be automatically reflected in the `precept_language` tool output.

5. **Function-call ambiguity**: The parser must distinguish `Name(` (function call) from `Name !=` or `Name ==` (identifier in comparison). The Superpower `.Try()` composition handles this: the function-call atom alternative uses lookahead on `(` and is inserted before the identifier alternative. `Name` followed by anything other than `(` falls through to the identifier parse. This is unambiguous because field names and function names occupy different namespaces (function names are a fixed static set; field names are declared per-precept).

---

## Key References

- Pierce, *Types and Programming Languages* Ch. 8 — typed arithmetic expressions; Ch. 11 — simple extensions (let, sequencing, conditionals as expressions)
- Veanes et al., "Symbolic Finite Automata" (CACM 2021) — decidable Boolean algebras over string predicates; composition of character-class and string-length predicates
- Kozen, "On the Complexity of Reasoning in Kleene Algebra" — decidability of regular expression logic; relevance to string pattern matching exclusion
- TypeScript Handbook, "Narrowing" — flow-sensitive type narrowing as a checker extension; model for Precept's `&&` narrowing and conditional expression branch narrowing
- Z3 SMT solver documentation — Presburger arithmetic decision procedures; string constraint theory
- OMG DMN 1.4 Specification — FEEL function library as formal precedent for bounded built-in functions in a business-rule DSL
- Cedar Language Specification — counter-precedent: minimal expression surface for authorization policy; deliberate omission of math functions and conditionals
- Makanin, "The Problem of Solvability of Equations in a Free Semigroup" (1977) — decidability of word equations; justification for restricting `.contains()` arguments to literals
