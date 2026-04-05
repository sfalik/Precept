# Expression Evaluation

**Research date:** 2026-04-04  
**Author:** George (Runtime Dev)  
**Relevance:** Principled ways to expand Precept's expression system while keeping semantics decidable and AI-readable

---

## Formal Concept

Precept's expression language is a **many-sorted first-order decidable fragment** — it has three base sorts (number, string, boolean), typed identifiers, a fixed set of operations per sort, and no quantifiers or recursion. This is a conservative, intentional design choice.

PLT provides a well-understood taxonomy for expanding such systems:

| Expansion Type | Formal Category | Decidability Risk |
|----------------|-----------------|-------------------|
| Additional comparison operators | Same fragment, new symbols | None |
| Boolean connectives | Boolean algebra extension | None |
| String predicates (length, prefix) | Regular language predicates | None if pattern-free |
| String pattern matching (regex) | Omega-regular expressions | Decidable but expensive |
| Arithmetic over reals | Presburger arithmetic | Decidable |
| Quantified constraints (`all x in S`) | First-order logic over sets | Decidable over finite domains |
| User-defined functions | Lambda calculus fragment | Undecidable in general |

Precept's current expression set sits in the lowest-risk zone: standard Boolean algebra over three sorts with arithmetic, comparison, and membership. Every proposed addition should be evaluated against this taxonomy.

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

## Implementation Cost Summary

| Addition | Parser Cost | Evaluator Cost | Type-checker Cost | Risk |
|----------|------------|---------------|------------------|------|
| `Field.length` accessor | Low | Low | Low | None |
| `startsWith` / `endsWith` operators | Low | Low | Low | None |
| `matches` (literal regex only) | Medium | Medium | Medium | ReDoS if open |
| `sum` collection accessor | Low | Low | Medium (no interval analysis) | None |
| Quantified expressions | High | High | High | Scope leakage |
| Nullable collection support + `?.` | Medium | Medium | Medium | Type model change |

---

## Semantic Risks Specific to Precept

1. **Constraint subject extraction**: `ExpressionSubjects.Extract()` walks the AST to find field/arg references for violation targets. New operator forms (dotted string accessors, new binary operators) must be handled in this walker or violation targets will be misattributed.

2. **Type coercion for new types**: The evaluator handles `JsonElement` unwrapping for external data. New operators must handle JSON string inputs correctly — `string.Length` works on CLR strings but may need a guard against `JsonElement` values.

3. **Operator precedence placement**: New operators must be inserted at the correct precedence level in the existing chain (Unary → Multiplicative → Additive → Comparison → AND → OR). String operators (`startsWith`, `endsWith`) belong at the Comparison level (same as `contains`).

4. **AI prompt fidelity**: If a new operator is added to the parser but not to the `precept_language` MCP tool response, AI agents will not know to use it. Parser, evaluator, type-checker, MCP tool, and language server completions must all be updated together.

---

## Key References

- Pierce, *Types and Programming Languages* Ch. 8 — typed arithmetic expressions
- Veanes et al., "Symbolic Finite Automata" (CACM 2021) — decidable Boolean algebras over string predicates
- Kozen, "On the Complexity of Reasoning in Kleene Algebra" — decidability of regular expression logic
- TypeScript Handbook, "Narrowing" — flow-sensitive type narrowing as a checker extension
- Z3 SMT solver documentation — Presburger arithmetic decision procedures
