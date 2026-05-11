# Constraint Composition — Theory and Reference

**Research date:** 2026-04-04, expanded 2026-05-18  
**Authors:** George (Runtime Dev), Frank (Language Designer)  
**Relevance:** Formal foundations for composing constraints — predicate combinators, scope theory, desugaring semantics, and the Boolean lattice structure underlying collect-all evaluation.

**Companion document:** [constraint-composition-domain.md](../expressiveness/constraint-composition-domain.md) covers the domain-level research (precedent survey, philosophy fit, sample evidence, dead ends). This file covers the theoretical and formal grounding.

---

## Formal Concept

PLT calls this **predicate combinator design** — the practice of building complex validation rules from small, named, composable predicates rather than monolithic boolean expressions. The related theoretical framework is the **Specification pattern** (Gang of Four derivative, formalized by Evans in DDD) and the algebraic structure of **Boolean lattices** (predicates form a distributive lattice under `∧`, `∨`, `¬`).

Three composition strategies appear in well-designed constraint languages:

| Strategy | Formal Name | Example Systems | Precept Current Status |
|----------|-------------|-----------------|----------------------|
| Implicit conjunction | Collect-all AND semantics | Alloy facts, Zod `.refine()`, JML invariants | **Implemented.** Multiple `invariant` / `assert` statements are independently evaluated, all failures reported. |
| Named predicate combinators | Higher-order predicates | Alloy `pred`, Drools named rules, FluentValidation rule sets | **Absent.** No way to name a reusable boolean sub-predicate. Proposed in #8 as `rule <Name> when <Expr>`. |
| Declaration-local constraints | Type-level constraint annotations | SQL `CHECK`, Zod `.min()`, Pydantic `Field(ge=0)`, C# `[Range]` | **Absent.** Field constraints are separate `invariant` statements. Proposed in #13 as keyword suffixes. |
| Conditional constraint activation | Guarded declarations | FluentValidation `When()`, Cedar `when`, JSON Schema `if/then/else` | **Absent.** Invariants are unconditional. Proposed in #14 as `invariant <Expr> when <Guard>`. |
| Constraint propagation | Narrowing through assignment | TypeScript type narrowing, Kotlin smart casts | **Partial.** Nullable narrowing through `when` guards is implemented. Value-range narrowing is not. |

---

## Formal Foundations

### Boolean Lattice Structure

Precept's constraint evaluation operates over a Boolean lattice. Each constraint is a predicate `P: EntityState → Bool`. The lattice operations:

- **Meet (∧):** Conjunction of predicates — all must hold. This is Precept's implicit conjunction model: every `invariant` and `assert` in scope is implicitly ANDed.
- **Join (∨):** Disjunction — at least one holds. Available within single expressions via `||` but not as a structural combinator across statements.
- **Complement (¬):** Negation — currently `!`, proposed `not`.
- **Top (⊤):** Trivially true — a tautological invariant (detected at compile time).
- **Bottom (⊥):** Trivially false — a contradictory invariant (detected at compile time for literal-only expressions).

The key insight: **implicit conjunction across statements** is the natural evaluation strategy for a collect-all constraint system. Precept's architecture already embodies this — multiple `invariant` statements in a precept are structurally the meet of their predicates. Named rules and field constraints add *naming* and *co-location* to this lattice, but do not change the algebraic structure.

### Specification Pattern (Evans, DDD)

Evans formalized the Specification pattern in Domain-Driven Design (Ch. 9): a composable predicate object that answers "does this entity satisfy this business rule?" Specifications compose via `And`, `Or`, `Not` combinators, producing new specifications without mutating the originals.

Mapping to Precept:

| Specification concept | Precept equivalent |
|----------------------|-------------------|
| `IsSatisfiedBy(entity)` | Invariant/assert expression evaluation |
| `spec1.And(spec2)` | Two `invariant` statements in the same precept |
| Named specification | Proposed `rule <Name> when <Expr>` |
| Parameterized specification | **Rejected.** Precept rules are closed over fields, not parameterized. |

The critical departure: Evans' specifications are runtime objects composed via method calls. Precept's rules are compile-time definitions expanded at each call site. This preserves Precept's compile-time-first, deterministic evaluation model.

### Scope Theory for Constraint Languages

Constraint scopes in Precept form a hierarchy:

```
Entity scope (all fields)
├── Field-local scope (one field + literals)
├── Cross-field scope (all fields + collection accessors)
├── Event-arg scope (one event's arguments only)
└── Mixed scope (fields + event args — transition `when` guards only)
```

Each constraint form has a fixed scope:

| Form | Scope | Rationale |
|------|-------|-----------|
| Field constraint suffix | Field-local | Declaration-co-located — about this field only |
| `invariant` | Cross-field | Entity-level data integrity |
| `in/to/from <State> assert` | Cross-field | State-conditional data integrity |
| `on <Event> assert` | Event-arg | Input validation — transient data only |
| `rule <Name> when <Expr>` | Cross-field | Reusable data predicate — must be event-independent for reuse |
| `when` guard (transition row) | Mixed | Routing decision — needs both entity state and event input |

**The scope boundary is the key design constraint.** Named rules are cross-field, not mixed-scope. This means a named rule cannot reference event arguments — and therefore cannot be used in `on <Event> assert`. This is not a limitation but a deliberate scope-separation decision. If a named rule could reference `Approve.Amount`, it would only be valid when `Approve` is in scope, defeating the reuse purpose.

### Desugaring Theory

Field constraint suffixes and named rules both desugar to existing constructs:

**Field constraints → invariants:**
```
field Amount as number default 0 nonnegative
  ≡ field Amount as number default 0
    invariant Amount >= 0 because "Amount must be non-negative"
```

**Named rules → expression expansion:**
```
rule LoanEligible when DocumentsVerified && CreditScore >= 680
from UnderReview on Approve when LoanEligible && Approve.Amount <= RequestedAmount
  ≡ from UnderReview on Approve when (DocumentsVerified && CreditScore >= 680) && Approve.Amount <= RequestedAmount
```

**Conditional invariants → guarded evaluation:**
```
invariant Amount > 100 when IsPremium because "..."
  ≡ (at evaluation time) if IsPremium then check(Amount > 100) else skip
```

The desugaring is syntactic — no new runtime primitives. This is a key design property: the composition layer adds expressiveness at the authoring level while preserving the existing evaluation engine unchanged.

**Diagnostic fidelity requirement:** Desugared constraints must attribute errors to the *original* authoring construct (the field suffix, the invariant call site, the rule definition), not the desugared form. This is the standard "resugaring" requirement in language design (Pombrio & Krishnamurthi, "Resugaring: Lifting Evaluation Sequences through Syntactic Sugar").

---

## Examples from Well-Designed Languages

### 1. Alloy — Implicit Conjunction and Named Predicates

```alloy
pred valid_loan[l: Loan] {
  l.amount > 0              // line 1
  l.applicant != none       // line 2  
  l.creditScore >= 600      // line 3
}

fact { all l: Loan | valid_loan[l] }
```

Each line in the predicate body is a separate constraint. The conjunction is structural (belonging to the same predicate block), not syntactic (`&&`). When `valid_loan` is checked in an instance, the analyzer reports *which line failed* — not "valid_loan is false." This is collect-all semantics: every failing constraint is reported independently.

**Key design insights:**
1. Separating the expression of individual constraints from their evaluation strategy. The human writes one constraint per line; the evaluator conjuncts them all; the error reporter breaks them apart again.
2. Named predicates (`pred`) are the primary composition mechanism — they are how Alloy scales from simple models to complex specifications.
3. Predicates can take parameters (`l: Loan`), but Precept deliberately rejects parameterization to stay declarative.

**Alloy documentation:** [https://alloytools.org/documentation.html](https://alloytools.org/documentation.html)

### 2. Zod — Scope-Segregated Predicate Combinators

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

**Key design insight:** Scope segregation in combinators — scalar predicates vs. cross-field predicates are syntactically distinct, making intent visible. Precept's design maps directly: field constraint suffixes are the Zod `.min()` equivalent; `invariant` is the `.refine()` equivalent.

**Zod documentation:** [https://zod.dev/](https://zod.dev/)

### 3. FluentValidation — Conditional Blocks and Named Rule Sets

```csharp
public class LoanValidator : AbstractValidator<Loan>
{
    public LoanValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CreditScore).InclusiveBetween(300, 850);
        
        When(x => x.IsPremium, () =>
        {
            RuleFor(x => x.MinAmount).GreaterThan(5000);
            RuleFor(x => x.CreditCard).NotNull();
        });
        
        RuleSet("Approval", () =>
        {
            RuleFor(x => x.DocumentsVerified).Equal(true);
            RuleFor(x => x.Income).GreaterThan(x => x.Debt * 2);
        });
    }
}
```

**Key design insights:**
1. `When` blocks apply one condition to multiple rules — the direct precedent for Precept's `invariant ... when`.
2. `RuleSet` groups rules under a name — a form of named constraint composition.
3. Property-chained constraints (`.GreaterThan(0)`) co-locate bounds with the property — the precedent for field constraint suffixes.

**FluentValidation documentation:** [https://docs.fluentvalidation.net/en/latest/](https://docs.fluentvalidation.net/en/latest/)

### 4. Drools — Named Rules with Structural Guards

```drools
rule "Loan Eligibility"
when
    $loan : Loan(
        documentsVerified == true,
        creditScore >= 680,
        annualIncome >= existingDebt * 2
    )
then
    $loan.setEligible(true);
end
```

**Key design insight:** Every rule has a name (`"Loan Eligibility"`). The name is the primary reference mechanism. When rules conflict or fire, the engine reports the rule by name. This is exactly the transparency model Precept's named rules should follow — the name is visible at authoring time, the expanded predicate is visible at inspection time.

**Drools documentation:** [https://docs.drools.org/latest/drools-docs/docs-website/drools/language-reference/](https://docs.drools.org/latest/drools-docs/docs-website/drools/language-reference/)

### 5. OCL — Let-Bound Sub-Expressions

```ocl
context Loan
inv eligibility:
  let isAffordable = self.income >= self.debt * 2 in
  let hasGoodCredit = self.creditScore >= 680 in
  isAffordable and hasGoodCredit and self.documentsVerified
```

**Key design insight:** `let` bindings give names to sub-expressions within an invariant body. This is lighter than a full named predicate — no standalone declaration, just inline naming. Precept's named rules chose the standalone declaration form for better discoverability and reuse across statements.

**OCL specification:** [https://www.omg.org/spec/OCL/](https://www.omg.org/spec/OCL/)

### 6. FHIR — Keyed Constraints with Expressions and Human Descriptions

```json
{
  "key": "ele-1",
  "severity": "error",
  "human": "All FHIR elements must have a @value or children",
  "expression": "hasValue() or (children().count() > id.count())",
  "xpath": "@value|f:*|h:div"
}
```

**Key design insight:** Every constraint has a `key` (name), a `human` (reason text), and an `expression`. This triple — name, reason, predicate — is exactly the structure Precept needs for named rules: `rule <Name> when <Expr>` with a `because` at the call site.

**FHIR conformance documentation:** [https://hl7.org/fhir/conformance-rules.html](https://hl7.org/fhir/conformance-rules.html)

---

## How This Applies to Precept

### Current state

Precept's constraint architecture already implements the correct collect-all semantics:
- Multiple `invariant` statements are checked independently; all failures are reported
- Multiple `on <Event> assert` statements per event are checked independently
- Multiple `in <State> assert` / `to <State> assert` etc. are checked independently

The **shape** is already right. The current gaps are:
1. **Expression-level composition** — no way to name a reusable sub-predicate (#8)
2. **Declaration-level co-location** — no way to attach basic constraints to field declarations (#13)
3. **Conditional applicability** — no way to make an invariant apply only when a field condition holds (#14)

### Absent: Named Predicate Reuse

Consider the loan-application sample. The approval guard is:
```precept
from UnderReview on Approve
  when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount
```

The same combination of credit conditions might apply to multiple events. Precept has no named predicate mechanism — the expression must be duplicated. This is the single largest constraint-composition gap.

**Formal category:** Named predicate combinators — `pred IsApprovalEligible { ... }` in Alloy terms, `rule "Loan Eligibility"` in Drools terms, `ele-1` in FHIR terms.

### Resolved Design: `rule` Declarations

The decision has converged on `rule <Name> when <BoolExpr>` — a top-level, named, field-scoped, flat boolean predicate. This corresponds to:

| Formal concept | Precept implementation |
|---------------|----------------------|
| Alloy `pred` | `rule` (but no parameters — closed over fields) |
| Drools named rule | `rule` (same naming pattern, flat body) |
| FluentValidation `RuleSet` | `rule` references in `invariant` / `assert` (grouping via naming, not block scoping) |
| OCL `let` | `rule` at top level (not inline — better discoverability) |
| Evans Specification | `rule` (compile-time resolution, not runtime object composition) |

**Scope resolution:** Rules resolve to cross-field scope — all declared fields, collection accessors, no event args. References are identifiers in boolean expression positions. The type checker resolves before expression evaluation.

**No stacking:** No `rule A when B && RuleC`. Each rule stands alone. This is a deliberate restriction that keeps the language flat and every rule independently readable. Combining rules happens at the *use site*: `when RuleA && RuleB`.

### Constraint propagation (narrowing)

Precept's type checker handles nullable narrowing through `when` guards. This is a restricted form of constraint propagation: `when X != null` narrows `X` to non-nullable in subsequent expressions. This is already implemented.

**Gap:** No narrowing of *value* constraints. If `when CreditScore >= 680`, the runtime knows `CreditScore` is at least 680 in the action body, but the type checker does not exploit this for optimistic analysis. This would be a medium-high type-checker change with limited user-visible benefit in the current feature set — classified as Batch 3 (static reasoning expansion) in the research batch plan.

---

## Composition Strategy Comparison

| Strategy | Formal Name | Precept Form | Scope | Evaluation | Error Attribution |
|----------|-------------|--------------|-------|------------|-------------------|
| Declaration-local | Type-level annotation | `field ... nonnegative` | Field-local | Desugared to invariant; collect-all | Field + constraint keyword |
| Implicit conjunction | Lattice meet | Multiple `invariant` statements | Cross-field | Collect-all; all failures reported | Each invariant independently |
| Named predicate | Predicate combinator | `rule <Name> when <Expr>` | Cross-field | Expanded at call site; collect-all | Call site (invariant/guard), not definition |
| Conditional activation | Guarded declaration | `invariant ... when <Guard>` | Cross-field (guard is field-only) | Guard evaluated first; if false, skip | Guard evaluation + constraint evaluation |
| Scope-segregated | Scope hierarchy | `invariant` vs `on Event assert` | Cross-field vs event-arg | Independent evaluation per scope | Scope-appropriate targets |

---

## Implementation Cost Summary

| Feature | Parser | Type Checker | Runtime | Language Server | Grammar | Tests |
|---------|--------|-------------|---------|-----------------|---------|-------|
| Field constraint suffixes (#13) | Medium (new tokens, `.Many()` combinator) | Medium (type-constraint compatibility, default validation) | Low (desugared to existing invariants) | Medium (completions, diagnostics) | Low (keyword additions) | Medium |
| Conditional guards (#14) | Low (optional `when` clause) | Low (field-scope guard validation) | Low (guard check before invariant eval) | Low (completions, hover) | Low (pattern addition) | Medium |
| Named rules (#8) | Medium (new declaration, reference resolution) | Medium (scope validation, boolean type check) | Low (expanded at compile time) | Medium (completions, go-to-definition, hover expansion) | Low (keyword + identifier) | Medium |
| Value narrowing | — | High (interval analysis) | — | — | — | High |

---

## Semantic Risks Specific to Precept

1. **Scope leakage**: Named predicates that reference event args are only valid when that event is in scope. A predicate defined with `Submit.Amount` cannot be reused for `Approve.Amount` without parameterization — which immediately escalates from a named predicate to a parameterized predicate (higher-order), which is a major semantic expansion. **Resolution:** Named rules are field-scoped only; event-arg references are a compile-time error.

2. **Collect-all reporting**: If a named predicate is used in multiple invariants, and the predicate fails, the error must attribute to the *call site* (the invariant that included it), not the predicate body. Otherwise diagnostic locality breaks. **Resolution:** Expansion at each call site; violation targets the call-site invariant.

3. **Circular predicate definitions**: A rule that references another rule would need cycle detection. **Resolution:** No rule-to-rule references — each rule stands alone. Cycle detection is trivially satisfied.

4. **AI readability**: Named predicates hide what a guard is actually checking. If the AI author uses a named predicate in a `when` guard, the MCP `inspect` tool must inline/expand it to make the condition auditable. **Resolution:** Named rules are transparent abstractions — expanded in all consumer-facing outputs.

5. **Desugaring diagnostic fidelity**: Field constraint violations must trace back to the field declaration, not the desugared `invariant`. This is the resugaring problem (Pombrio & Krishnamurthi). **Resolution:** Constraint violations carry source-location metadata pointing to the original constraint suffix, not the desugared invariant.

6. **Suffix grammar extensibility**: Each new type (#25–#29) may bring type-specific constraint keywords. The constraint zone must accommodate growth without breaking existing parses or creating keyword collisions. **Resolution:** Constraint keywords are a self-identifying closed set per type; parser validates keyword-type compatibility at parse time (C56).

---

## Key References

- Evans, *Domain-Driven Design* Ch. 9 — Specification pattern as named composable predicates
- Hohpe & Woolf, *Enterprise Integration Patterns* — Specification pattern catalog
- Pombrio & Krishnamurthi, "Resugaring: Lifting Evaluation Sequences through Syntactic Sugar" — diagnostic fidelity for desugared constructs
- Alloy documentation — predicates as named constraint fragments: [https://alloytools.org/documentation.html](https://alloytools.org/documentation.html)
- Zod documentation — `.refine()` and chained combinators: [https://zod.dev/](https://zod.dev/)
- FluentValidation documentation — `When` blocks, rule sets, property chains: [https://docs.fluentvalidation.net/en/latest/](https://docs.fluentvalidation.net/en/latest/)
- Drools documentation — named rules with structural guards: [https://docs.drools.org/latest/drools-docs/docs-website/drools/language-reference/](https://docs.drools.org/latest/drools-docs/docs-website/drools/language-reference/)
- Cedar documentation — `when`/`unless` guards on policies: [https://docs.cedarpolicy.com/policies/syntax-policy.html](https://docs.cedarpolicy.com/policies/syntax-policy.html)
- FHIR conformance rules — keyed constraints with expressions: [https://hl7.org/fhir/conformance-rules.html](https://hl7.org/fhir/conformance-rules.html)
- OCL specification — invariants, let-bindings, implies: [https://www.omg.org/spec/OCL/](https://www.omg.org/spec/OCL/)
- JSON Schema — `if/then/else` conditional subschemas: [https://json-schema.org/understanding-json-schema/](https://json-schema.org/understanding-json-schema/)
- DMN specification — decision tables, FEEL expressions: [https://www.omg.org/spec/DMN/](https://www.omg.org/spec/DMN/)
- Felleisen, "On the Expressive Power of Programming Languages" — what combinators actually add
