# Conditional Invariant Survey

Reference: How production constraint systems handle conditional constraints.

## Summary

**Key finding:** Production systems avoid negation and conditional invariants. Use positive guards (`when`/`unless`), positive rows (DMN), or separate constraint clauses instead.

## Detailed Survey

### FluentValidation (C# Validation Framework)

```csharp
RuleFor(x => x.CardNumber)
  .CreditCard().When(x => x.IsPremium).WithMessage("Premium requires valid card");

RuleFor(x => x.Age)
  .GreaterThan(18).Unless(x => x.HasParental).WithMessage("...");
```

**Pattern:** `When(condition)` / `Unless(condition)` as row-level guards. Multiple rules for the same field, each guarded. Positive condition; no negation in the guard itself.

**Status:** Flagship pattern. Widely copied.

---

### Drools (JBoss Business Rules Engine)

```
rule "Check premium discount"
when
  $order : Order( isPremium == true )
  $item : Item( price > 100 )
then
  $order.applyDiscount(10);
end
```

**Pattern:** `when` structural guard in rule head. Conditions are positive. Multiple rules for same fact pattern.

**Status:** Foundational to Drools. `when` is the rule composition operator.

---

### Cedar (AWS Authorization Language)

```cedar
permit(principal, action, resource)
when {
  principal has department &&
  principal.department == resource.department &&
  action == Action::"read"
};
```

**Pattern:** `when` guards in policy effect. Positive conditions.

**Status:** Modern. Designed for expressiveness without negation burden.

---

### DMN (Decision Model and Notation — OMG Standard)

Decision tables with multiple rows:
```
  Input1    | Input2  | Output
  ----------|---------|----------
  "premium" | > 100   | "approve"
  "standard"| > 1000  | "approve"
  -         | -       | "deny"
```

**Pattern:** Each row is a positive condition set. No `else` or fallback; rows ordered; first match wins. No negation in column headers.

**Status:** OMG standard. Enterprise-grade. No conditional invariants.

---

### JSON Schema (Draft 7+)

```json
{
  "if": { "properties": { "credit_score": { "minimum": 680 } } },
  "then": { "properties": { "max_loan": { "minimum": 100000 } } },
  "else": { "properties": { "max_loan": { "minimum": 10000 } } }
}
```

**Pattern:** `if/then/else` for conditional schema. **Added in Draft 7 specifically to avoid negation.**

**Status:** Widely used. Validates the `if/then/else` design for value conditions.

---

### Zod (TypeScript Validation)

```typescript
z.object({
  email: z.string().email().refine(val => isVerified(val), {
    message: "Email must be verified"
  }).when(context.isPremium, ...)
})
```

**Pattern:** `refine().when()` — guard-based activation of custom rules.

**Status:** Modern, functional style. Guards are positive.

---

### OCL (Object Constraint Language — OMG)

```ocl
context Order
inv: self.isPremium implies self.minAmount > 5000
inv: self.isPremium implies self.deliveryDays < 5
```

**Pattern:** `implies` operator. `A implies B` ≡ `not A or B`.

**Drawback:** The negated case is non-intuitive. Double-negative rules are hard to read.

**Status:** Formal logic. Used in UML. Not fluent for business DSLs.

---

### Alloy (Formal Specification Language — MIT)

```alloy
fact { all o: Order | o.isPremium => o.minAmount > 0 }
fact { all o: Order | not o.isRefunded }
```

**Pattern:** `=>` implication, `not` keyword. Formal register.

**Key insight:** Uses `not` keyword, not `!`, for readability in specifications.

**Status:** Academic/formal verification. Not a DSL.

---

### CSS (Cascading Style Sheets)

```css
.card { color: blue; }
.card.premium { color: gold; }
.card:not(.premium) { color: gray; }
```

**Pattern:** Separate positive rules. Negation via `:not()` pseudo-class (modern, but verbose). Default: write positive rules.

**Status:** Web standard. Designers avoid negation; it's syntactically heavy.

---

### SQL CHECK Constraints

```sql
CREATE TABLE orders (
  amount DECIMAL,
  is_premium BOOLEAN,
  CHECK (amount > 0),
  CHECK (is_premium = true OR amount < 1000)
);
```

**Pattern:** Independent constraints. No conditional relationship. If you need conditional validation, split into two tables or use application logic.

**Lesson:** SQL deliberately avoids complex conditional constraints.

## Lessons for Precept

1. **Positive guards, not negation.** Use `when IsPremium` not `when not IsPremium`.
2. **Multiple rows, first-match.** DMN, Drools, FluentValidation all use row selection instead of conditional constraints.
3. **Avoid formal implication notation.** `implies` is powerful for math but wrong register for business DSLs.
4. **Recognize the root cause.** Boolean fields for categories (Premium/Standard) are the anti-pattern. Introduce choice types to eliminate negation at the modeling level.
5. **`when`/`unless` guards scale.** Every system that moved away from formal implication uses positive guards.
