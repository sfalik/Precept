# Constraint Composition

**Research date:** 2026-04-04  
**Author:** George (Runtime Dev)  
**Relevance:** Composing multiple constraints without explicit boilerplate

---

## Formal Concept

PLT calls this **predicate combinator design** — the practice of building complex validation rules from small, named, composable predicates rather than monolithic boolean expressions. The related theoretical framework is the **Specification pattern** (Gang of Four derivative, formalized by Evans in DDD) and the algebraic structure of **Boolean lattices** (predicates form a distributive lattice under `∧`, `∨`, `¬`).

Three composition strategies appear in well-designed constraint languages:

| Strategy | Formal Name | Example Systems |
|----------|-------------|-----------------|
| Implicit conjunction | Collect-all AND semantics | Alloy facts, Zod `.refine()`, JML invariants |
| Named predicate combinators | Higher-order predicates | Alloy predicates, FluentAssertions |
| Constraint propagation | Narrowing through assignment | TypeScript type narrowing, Kotlin smart casts |

---

## Examples from Well-Designed Languages

### 1. Alloy — Implicit Conjunction in Predicates

```alloy
pred valid_loan[l: Loan] {
  l.amount > 0              // line 1
  l.applicant != none       // line 2  
  l.creditScore >= 600      // line 3
}
```

Each line is a separate constraint. The conjunction is structural (belonging to the same predicate block), not syntactic (`&&`). When `valid_loan` is checked in an instance, the analyzer reports *which line failed* — not "valid_loan is false." This is collect-all semantics: every failing constraint is reported independently.

**Key design insight:** Separating the expression of individual constraints from their evaluation strategy. The human writes one constraint per line; the evaluator conjuncts them all; the error reporter breaks them apart again.

### 2. Zod — Chainable Predicate Combinators

```typescript
const loanSchema = z.object({
  amount:      z.number().positive(),
  applicant:   z.string().min(1),
  creditScore: z.number().min(600).max(850),
  income:      z.number().nonnegative(),
}).refine(
  d => d.income >= d.existingDebt * 2,
  { message: "Debt-to-income ratio too high" }
);
```

Type-local constraints (`positive()`, `min()`, `max()`) are composed as a method chain. Cross-field constraints are composed via `.refine()`. Both are predicates; the difference is scope. When validation fails, Zod returns an array of `ZodIssue` — collect-all semantics again.

**Key design insight:** Scope segregation in combinators — scalar predicates vs. cross-field predicates are syntactically distinct, making intent visible.

---

## How This Applies to Precept

### Current state

Precept's constraint architecture already implements the correct collect-all semantics:
- Multiple `invariant` statements are checked independently; all failures are reported
- Multiple `on <Event> assert` statements per event are checked independently
- Multiple `in <State> assert` / `to <State> assert` etc. are checked independently

The **shape** is already right. The current gap is **expression-level composition** — individual constraints can only use flat boolean expressions; there's no way to name a reusable sub-predicate.

### Absent: Named Predicate Reuse

Consider the loan-application sample. The approval guard is:
```precept
from UnderReview on Approve
  when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount
```

The same combination of credit conditions might apply to multiple events. Precept has no named predicate mechanism — the expression must be duplicated. This is the single largest constraint-composition gap.

**Formal category:** Named predicate combinators — `pred IsApprovalEligible { ... }` in Alloy terms.

### Options by cost

**Option A: Named invariant references** (high semantic cost)  
Allow an invariant to be named and referenced in `when` guards. Problem: invariants are global (no arg scope), and `when` guards have event arg scope. Scoping rules make this ambiguous.

**Option B: `rule` blocks on fields** (already partially exists)  
Precept already has field-local rules (mentioned in DiagnosticCatalog). These are per-field predicate fragments. Extending them to be named and reusable across transition rows would be a genuine constraint combinator.

**Option C: Implicit constraint lifting via `require` declarations**  
A `require <Name> <BoolExpr> because "..."` form — a named, reusable constraint that can be referenced in `when` guards by name. Desugars to a full expression at the call site. This preserves flat evaluation while reducing duplication.

**Verdict:** Option C is closest to the formal combinator pattern and requires medium parser change (new `require` statement, reference resolution in guard expressions). The type checker must resolve references before expression evaluation.

### Constraint propagation (narrowing)

Precept's type checker handles nullable narrowing through `when` guards. This is a restricted form of constraint propagation: `when X != null` narrows `X` to non-nullable in subsequent expressions. This is already implemented.

**Gap:** No narrowing of *value* constraints. If `when CreditScore >= 680`, the runtime knows `CreditScore` is at least 680 in the action body, but the type checker does not exploit this for optimistic analysis. This would be a medium-high type-checker change with limited user-visible benefit in the current feature set.

---

## Implementation Cost: **Medium**

- Collect-all semantics: already implemented
- Named predicate references: medium (new AST node, scope resolution, no new runtime work)
- Constraint propagation beyond nullable narrowing: high (interval analysis extension)

---

## Semantic Risks Specific to Precept

1. **Scope leakage**: Named predicates that reference event args are only valid when that event is in scope. A predicate defined with `Submit.Amount` cannot be reused for `Approve.Amount` without parameterization — which immediately escalates from a named predicate to a parameterized predicate (higher-order), which is a major semantic expansion.

2. **Collect-all reporting**: If a named predicate is used in multiple invariants, and the predicate fails, the error must attribute to the *call site* (the invariant that included it), not the predicate body. Otherwise diagnostic locality breaks.

3. **Circular predicate definitions**: A `require` that references itself or another `require` that references it back would need cycle detection at parse/analysis time.

4. **AI readability**: Named predicates hide what a guard is actually checking. If the AI author uses a named predicate in a `when` guard, the MCP `inspect` tool must inline/expand it to make the condition auditable.

---

## Key References

- Hohpe & Woolf, *Enterprise Integration Patterns* — Specification pattern catalog
- Evans, *Domain-Driven Design* Ch. 9 — Specification pattern as named composable predicates
- Alloy Documentation — predicates as named constraint fragments
- Zod Documentation — `.refine()` and chained combinators
- Felleisen, "On the Expressive Power of Programming Languages" — what combinators actually add
