# Nullable Propagation in Computed / Derived Fields

> **Issue**: #17 (Computed Fields — shipped in PR #82)
> **Type**: Feasibility & suitability analysis — NOT a proposal
> **Date**: 2025-07-18

## Executive Summary

This study surveys how 10 languages, type systems, and DSLs handle nullable propagation in computed or derived values. The central question: **should a computed field automatically become nullable when any of its inputs are nullable, propagating `null` through the expression?**

Precept currently forbids nullable references in computed field expressions (C83) and declares that computed fields always produce a value (C81). This research examines external precedent to assess whether relaxing these constraints would be feasible and suitable for Precept's design principles.

---

## 1. External Precedent Survey

### 1.1 SQL (PostgreSQL, SQL Server, SQLite)

**Approach**: Implicit propagation via three-valued logic.

SQL is the canonical example of implicit null propagation. Any arithmetic or string operation involving NULL yields NULL: `NULL + 5 = NULL`, `NULL || 'text' = NULL`. This extends to generated/computed columns — a `GENERATED ALWAYS AS (Price * Quantity)` column becomes NULL when either input is NULL.

- **COALESCE(a, b, ...)** returns the first non-null argument — the standard fallback mechanism.
- **NULLIF(a, b)** returns NULL if a = b, otherwise a.
- Comparison: `NULL = NULL` is not `TRUE` but `UNKNOWN`; `10 >= NULL` is `UNKNOWN`.
- SQL Server computed columns: "If any column that is referenced in a computed column can be NULL, the computed column itself can return NULL."

**Key insight**: SQL chose implicit propagation 40+ years ago and has lived with its consequences. The resulting "NULL trap" (where NULLs silently corrupt aggregations, joins, and predicates) is one of the most cited sources of bugs in relational programming.

### 1.2 C# (Nullable Value Types, Nullable References)

**Approach**: Lifted operators (implicit propagation for value types) + explicit propagation operators.

C#'s `Nullable<T>` uses **lifted operators**: `int? + int?` → `int?`, returning null if either operand is null. This is compiler-generated — the same `+` operator works transparently on nullable and non-nullable types.

- **Null-conditional `?.`**: `obj?.Property` short-circuits to null if `obj` is null. Chains naturally: `a?.b?.c`.
- **Null-coalescing `??`**: `value ?? fallback` provides a non-null default.
- **Nullable reference types** (C# 8+): static flow analysis tracks maybe-null vs. not-null states through control flow. Compiler warns when you dereference a maybe-null reference without a null check.
- Comparison operators on `Nullable<T>`: `10 >= null` is `false` (not UNKNOWN as in SQL), `null == null` is `true`.

**Key insight**: C# has two parallel systems — implicit lifted-operator propagation for value types, and explicit flow-analysis for reference types. The lifted-operator model is closest to what Precept would need.

### 1.3 TypeScript / JavaScript

**Approach**: Explicit propagation operators + control-flow narrowing.

TypeScript does not automatically propagate `undefined`/`null` through arithmetic. `null + 5` is a type error in strict mode. Instead:

- **Optional chaining `?.`**: `obj?.prop` returns `undefined` if `obj` is nullish. Short-circuits the entire chain.
- **Nullish coalescing `??`**: `value ?? fallback` returns `fallback` only for `null`/`undefined` (not `0` or `""`).
- **Type narrowing**: `if (x !== null)` narrows `x` from `T | null` to `T` within the block. Discriminated unions, `typeof` guards, and equality checks all narrow.

**Key insight**: TypeScript deliberately rejected implicit propagation. The type system forces developers to handle nullability explicitly at each step, which is verbose but eliminates surprise-null bugs.

### 1.4 Kotlin

**Approach**: Type-system-enforced explicit propagation.

Kotlin's null safety is baked into the type system: `String` never holds null, `String?` may. The compiler rejects method calls on nullable types without explicit null handling.

- **Safe call `?.`**: `str?.length` returns `Int?` — null if `str` is null.
- **Elvis operator `?:`**: `str?.length ?: 0` provides a fallback.
- **Non-null assertion `!!`**: `str!!.length` throws NPE if null — opt-in unsafety.
- **Smart casts**: After a null check in an `if`, the variable is automatically narrowed to non-null.

**Key insight**: Kotlin makes nullability a first-class type distinction and forces explicit propagation at every step. No operation silently introduces or propagates null.

### 1.5 Swift

**Approach**: Optionals as explicit wrapper types + optional chaining.

Swift optionals (`Int?`) are a distinct type from `Int`. You cannot use an `Int?` where an `Int` is expected without unwrapping.

- **Optional chaining `?.`**: `person?.address?.city` returns `String?`. Multiple levels of chaining don't add more optionality (no `String??`).
- **Optional binding `if let`**: `if let value = optional { /* use value */ }` — cleanly unwraps.
- **Nil-coalescing `??`**: `optional ?? default` provides fallback.
- **Force unwrap `!`**: `optional!` crashes on nil — explicit opt-in to unsafety.

**Key insight**: Swift's computed properties on classes/structs can reference optionals through chaining, but the result type must be declared. There is no implicit propagation through arithmetic — `Int? + Int` is a compile error.

### 1.6 Haskell / ML (Maybe Monad)

**Approach**: Fully explicit monadic propagation.

Haskell's `Maybe a = Nothing | Just a` is structurally equivalent to nullable types, but propagation is always explicit:

- **Functor `fmap`**: `fmap (+1) (Just 5)` → `Just 6`; `fmap (+1) Nothing` → `Nothing`. Maps a function over the inner value, propagating Nothing.
- **Monad bind `>>=`**: Chains computations. If any step returns `Nothing`, the entire chain short-circuits to `Nothing`.
- **`maybe` function**: `maybe defaultVal f maybeVal` — deconstructor with fallback.
- **do-notation**: Syntactic sugar for monadic chaining, making multi-step Maybe computations readable.

**Key insight**: Haskell proves that explicit propagation can be ergonomic with the right syntax (do-notation, `<$>`, `<*>`). But it requires a fundamentally different programming model — monadic composition rather than direct expressions.

### 1.7 Excel (Formulas and Error Propagation)

**Approach**: Implicit error/blank propagation with explicit error-trapping functions.

Excel is arguably the closest UX precedent to Precept's "formula as definition" model. Empty/blank cells participate in formulas:

- Blank cells are treated as `0` in numeric contexts and `""` in string contexts — they do NOT propagate as null.
- **Error propagation** is implicit: if any input to a formula produces an error (#N/A, #VALUE!, #REF!, #DIV/0!, #NUM!, #NAME?, #NULL!), the error propagates through the entire formula.
- **IFERROR(value, fallback)**: Catches any error type and returns fallback. This is the standard guard.
- **IFNA(value, fallback)**: Catches only #N/A errors.
- Array formulas: IFERROR wraps the entire array expression; errors in individual elements get the fallback value.

**Key insight**: Excel distinguishes between "blank" (absent data, coerced to a default) and "error" (invalid computation, propagated). Precept's `null` is closer to "blank" — a field that hasn't been assigned yet. Excel's approach of coercing blanks to domain-appropriate defaults rather than propagating them is worth noting.

### 1.8 GraphQL (Null Propagation in Resolver Chains)

**Approach**: Default-nullable with explicit non-null annotation (`!`) + upward null propagation.

GraphQL types are nullable by default. The `!` suffix marks a type as non-null: `String!` guarantees a value.

- **Null propagation rule**: If a non-null field resolves to null (due to a resolver error), the null propagates upward to the nearest nullable parent field. If that parent is also non-null, it propagates further.
- **Bubble-up**: If all fields from root to error source are non-null, the entire `"data"` entry becomes null.
- The `!` annotation is the schema author's **guarantee** — "I promise this will never be null." Breaking that promise triggers error propagation.
- Lists interact with non-null: `[Int!]` (nullable list of non-null ints) vs. `[Int]!` (non-null list of nullable ints) vs. `[Int!]!` (non-null list of non-null ints).

**Key insight**: GraphQL's approach inverts the typical pattern — fields are nullable by default, and non-null is the special annotation. Null propagation serves as an error-handling mechanism, not a data-flow mechanism. The non-null annotation creates a contract that, when violated, causes cascading nullification.

### 1.9 Zod / Valibot (Schema Validation Libraries)

**Approach**: Explicit wrappers with no implicit propagation through transforms.

Zod and Valibot are TypeScript schema validation libraries:

- **Zod**: `z.nullable(z.string())` creates a schema accepting `string | null`. `z.optional()` for `undefined`. `z.nullish()` for both. Transforms (`.transform()`) and pipes (`.pipe()`) do not automatically propagate nullability — if input can be null, the transform function receives null and must handle it.
- **Valibot**: `v.nullable(v.string())` accepts `string | null`. `v.optional()` for `undefined`. Default values via second argument: `v.optional(v.string(), "default")` changes output type from `string | undefined` to `string`. Dependent defaults via `v.transform()` in pipes.
- Neither library has a concept of "computed/derived schemas" that infer nullability from inputs. Nullability is always declared explicitly on each schema.

**Key insight**: Modern validation DSLs treat nullability as an explicit per-field declaration, never inferring it from expression context. This mirrors the "explicit is better than implicit" philosophy.

### 1.10 Datalog / Datomic (Derived Attributes with Missing Data)

**Approach**: Closed-world absence — no null concept; missing data simply doesn't match.

Datomic Datalog has no null values at all. Instead:

- Attributes either have values or don't. A query clause `[?e :artist/startYear ?year]` simply doesn't match entities without a `:artist/startYear`.
- **`missing?` predicate**: `[(missing? $ ?artist :artist/startYear)]` explicitly tests for the absence of an attribute. Returns true if the entity has no value for the attribute.
- **`get-else`**: `[(get-else $ ?artist :artist/startYear "N/A") ?year]` returns a default value when an attribute is missing.
- **Rules (derived "attributes")**: A rule like `[(track-info ?artist ?name ?duration) ...]` only produces results where all clauses match. If any entity lacks a required attribute, that entity is silently excluded from results — no null is produced.

**Key insight**: Datalog's approach is the most radically different — it eliminates null entirely by treating absence as "no binding" rather than "null binding." Derived values (rules) cannot produce null; they either produce a result or produce nothing. This is the purest expression of "computed fields always produce a value."

---

## 2. Taxonomy of Approaches

The surveyed systems fall into four categories:

### 2.1 Implicit Propagation
**Systems**: SQL, C# lifted operators, Excel (errors)

Null/error propagates automatically through expressions without any explicit syntax. `null + 5` = `null`. The computed result's nullability is inferred from inputs.

**Pros**: Zero ceremony — expressions "just work" with nullable inputs.
**Cons**: Silent corruption. NULLs spread invisibly, especially through aggregations and comparisons. SQL's 40-year track record demonstrates the maintenance cost.

### 2.2 Explicit Propagation Operators
**Systems**: C# (`?.`, `??`), TypeScript (`?.`, `??`), Kotlin (`?.`, `?:`, `!!`), Swift (`?.`, `??`, `if let`)

The type system tracks nullability, and the developer must use explicit operators to propagate or unwrap. Arithmetic on nullable types without unwrapping is a compile error.

**Pros**: No surprise nulls. Every propagation point is visible in code.
**Cons**: Verbose. Requires operator syntax that may not fit every DSL's aesthetics.

### 2.3 Monadic / Compositional Propagation
**Systems**: Haskell (`Maybe`/`fmap`/`>>=`)

Propagation happens through explicit monadic composition. Syntactically heavier than operator-based approaches but theoretically sound.

**Pros**: Mathematical elegance. Composable.
**Cons**: High learning curve. Requires fundamentally different expression model.

### 2.4 Absence Eliminates Rather Than Propagates
**Systems**: Datalog/Datomic, GraphQL (in a different way)

Missing data doesn't produce null results — it eliminates the computation entirely. Derived values either exist fully or don't exist at all.

**Pros**: No null values in the system. Computed results are always valid.
**Cons**: Limits expressiveness — can't represent "this field exists but has no value."

---

## 3. Precept-Specific Analysis

### 3.1 Current Constraints Recap

| Constraint | Description |
|-----------|-------------|
| C80 | Computed field and `default` are mutually exclusive |
| C81 | Computed field cannot be `nullable` — "Computed fields always produce a value" |
| C82 | Multi-name declarations not allowed for computed fields |
| C83 | Nullable field references rejected in computed expressions |
| C84 | Event arguments rejected in computed expressions |
| C85 | Unsafe collection accessors rejected |
| C86 | Circular dependencies are compile errors |
| C87 | Computed fields cannot appear in `edit` blocks |
| C88 | Computed fields cannot be `set` targets |

### 3.2 Alignment with Design Principles

Precept's philosophy document identifies several principles relevant to this analysis:

1. **"English-ish but not English"**: Any nullable propagation syntax must feel natural in Precept's declarative idiom. Operators like `?.` and `??` are foreign to Precept's current vocabulary.

2. **"Minimal ceremony"**: Implicit propagation (SQL-style) would have zero ceremony. Explicit propagation would add new syntax and concepts to a deliberately minimal language.

3. **"Deterministic inspectability"**: Every field value must be explainable. If `Total -> Price * Quantity` silently becomes null because Price is null, the inspectability story becomes "Total is null because Price is null because..." — which is traceable but potentially surprising to domain users.

4. **Principle 11: "`->` means results in"**: The arrow carries a strong semantic promise — this expression *produces* a value. A nullable computed field weakens this promise to "this expression *might produce* a value."

5. **"Prevention, not detection"**: C81 and C83 together prevent null from entering computed fields. This is a prevention strategy. Nullable propagation would shift to detection (via inspect/constraints).

### 3.3 Interaction with Invariants and Guards

If computed fields could be nullable, several downstream effects emerge:

- **Invariants**: An invariant like `assert Total > 0` would need to handle `Total` being null. Currently, this is impossible by construction. With nullable computed fields, every invariant referencing a nullable computed field would need implicit or explicit null handling.
- **Guards**: A guard like `when Total > 100` faces the same issue. What does `null > 100` evaluate to? SQL says UNKNOWN (falsy). C# says false. Precept would need to decide once and define it clearly.
- **Other computed fields**: If `A -> B + 1` and `B` is a nullable computed field, A would also need to propagate nullability. This creates transitive nullable chains.
- **Edit declarations**: Currently C87 prevents computed fields in `edit` blocks. If they're nullable, does `edit` need to accommodate displaying "no value"? The UX surface expands.

### 3.4 Type Checker Impact

Introducing nullable computed fields would require:

1. **Nullable type inference**: The type checker must determine when a computed expression could evaluate to null based on its input fields.
2. **Operator semantics for null**: Define what `null + 5`, `null > 0`, `null and true`, etc. evaluate to. Precept would need a complete null-algebra specification.
3. **Constraint evaluation changes**: Every C-series constraint that references field types would need to account for nullable computed fields.
4. **Transitive closure**: If computed field A references computed field B, and B could be nullable, A inherits potential nullability. The type checker must compute transitive nullable closures across the dependency graph.

### 3.5 Expression Evaluator Impact

The expression evaluator currently assumes computed field expressions always produce a value. Changes needed:

1. Every binary operator (`+`, `-`, `*`, `/`, `>`, `<`, `>=`, `<=`, `=`, `!=`, `and`, `or`) needs null-handling semantics.
2. The `if/else` expression needs null-aware branch selection — what happens if the condition is null?
3. Collection accessors (`.count`) are already safe, but if collections themselves could be nullable...
4. String concatenation with null — is `"hello" + null` an error, `"hello"`, or `"hellonull"`?

### 3.6 MCP Serialization Impact

The MCP tools would need to:
- Report nullable-computed differently from non-nullable-computed in `precept_compile` output.
- Handle null values in `precept_inspect` and `precept_fire` output for computed fields.
- The `precept_language` tool would need updated constraint catalogs.

---

## 4. Feasibility Assessment

### 4.1 Type Checker: **Feasible with Significant Effort**
Nullable type inference through expressions is well-understood (C#, Kotlin, Swift all do it). But Precept's type system is deliberately minimal. Adding a nullable-propagation inference pass is tractable but increases type-checker complexity substantially.

### 4.2 Expression Evaluator: **Feasible with Clear Design Choices**
The null-algebra must be fully specified. SQL's three-valued logic is one option. C#'s approach (comparisons return false, arithmetic returns null) is another. Either is implementable, but the choice has far-reaching implications for guard and invariant semantics.

### 4.3 Invariant/Guard Pipeline: **Feasible but Changes Semantics**
The current guarantee that all guard and invariant expressions evaluate against fully non-null computed fields would be lost. Every guard/invariant evaluation would need null-handling. This is the highest-impact change.

### 4.4 MCP Serialization: **Low Impact**
MCP DTOs already handle nullable field values. Extending to nullable computed fields is straightforward.

---

## 5. Suitability Verdict

### **CONDITIONALLY SUITABLE**

Nullable propagation in computed fields is **technically feasible** but runs against several of Precept's strongest design commitments:

1. **It weakens the `->` guarantee**. The current "computed fields always produce a value" invariant (C81) is one of Precept's cleanest guarantees. Introducing nullable computed fields creates a two-tier system: computed fields that always produce values and computed fields that might not.

2. **It opposes the Datalog/prevention model**. Precept's closest philosophical neighbors (Datalog, prevention-oriented systems) eliminate null from derived values entirely. SQL's implicit propagation model, while the most ergonomic, is also the most criticized in practice.

3. **The expression surface must expand**. Precept would need null-handling operators (`??`, `?.`, or equivalent) or implicit propagation semantics. Both add complexity to a deliberately small language.

4. **Invariant/guard semantics change non-locally**. A nullable computed field affects every constraint that references it. This is a systemic impact, not a local change.

**However**, there are legitimate domain modeling scenarios where a computed field should reflect "this value cannot be determined yet because its inputs are incomplete." The question is whether Precept should handle this at the computed-field level or at the field-level (making the input fields non-nullable with appropriate defaults or state guards).

### Conditions for Suitability

If this were to proceed, it would be most suitable under these conditions:

1. **Opt-in, not default**: Keep C81 (computed fields always produce a value) as the default. Introduce nullable computed fields as an explicit opt-in, e.g. `field Total as nullable number -> Price * Quantity`.
2. **Explicit fallback required**: Require a fallback expression rather than silent propagation, e.g. `field Total as number -> (Price * Quantity) ?? 0`.
3. **Type-checker-enforced**: If a computed expression references a nullable field, the type checker should require either the output to be declared nullable or a fallback to be provided.
4. **Defined null algebra**: Full specification of what every operator does with null operands, documented as a clear table rather than implicit behavior.

---

## 6. Recommended Next Steps

1. **Survey real domain use cases**: Identify 3-5 concrete business entities where a computed field legitimately needs to reflect input incompleteness, not just absent data.
2. **Evaluate the fallback-expression alternative**: Instead of nullable propagation, consider allowing computed fields to reference nullable fields *only when* a fallback expression (`??` or similar) is provided, preserving C81.
3. **If proceeding**: Write a formal proposal (GitHub issue) with the conditions above, the null-algebra specification, and the impact analysis from this research.
4. **If not proceeding**: Document the decision rationale in the existing computed-fields research file — the current C81+C83 constraints are a deliberate design choice aligned with Datalog precedent and prevention philosophy.

---

## References

| System | Sources |
|--------|---------|
| SQL | PostgreSQL generated columns docs, SQL Server computed columns docs, SQLite CREATE TABLE docs, PostgreSQL conditional function docs |
| C# | Nullable value types docs, null-conditional operator docs, nullable reference types docs |
| TypeScript/JS | TypeScript narrowing handbook, MDN optional chaining, MDN nullish coalescing |
| Kotlin | Kotlin null-safety docs, delegated properties docs |
| Swift | Swift optional chaining docs, Swift basics/optionals docs |
| Haskell | HaskellWiki Maybe, Hackage Data.Maybe |
| Excel | Microsoft IFERROR function docs, Microsoft formula error-avoidance guide |
| GraphQL | GraphQL spec §6.4.4 Handling Field Errors, graphql.org schema/types docs |
| Zod/Valibot | Zod docs (nullable, optional, transforms), Valibot optionals guide |
| Datalog/Datomic | Datomic query data reference (get-else, missing?, rules) |
