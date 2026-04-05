# Zod / Valibot — DSL Expressiveness Research

**Studied:** 2026-05-01  
**Source:** https://zod.dev/ + web research on refinements and superRefine  
**Relevance:** Zod is the dominant TypeScript schema validation DSL. Its chainable constraint model is the reference point for how front-end and full-stack developers expect to express field-level rules. Valibot mirrors its patterns with a tree-shaking-friendly architecture.

---

## What Zod Does Well

### 1. Co-located schema + constraints (single declaration)

```typescript
const UserSchema = z.object({
  name: z.string().min(1, "Name is required").max(100, "Name too long"),
  age: z.number().int().positive("Age must be positive"),
  email: z.string().email("Invalid email format").optional(),
});
```

The field type and all its constraints are declared together in one expression. Reading left to right: type → constraints → optional.

### 2. Cross-field constraints via `.refine()` / `.superRefine()`

```typescript
const LoanSchema = z.object({
  requestedAmount: z.number().positive(),
  approvedAmount: z.number().nonnegative(),
}).refine(
  data => data.approvedAmount <= data.requestedAmount,
  { message: "Approved cannot exceed requested", path: ["approvedAmount"] }
);
```

Cross-field rules attach to the *object* schema. The `path` option targets the error at the specific field.

### 3. Conditional schemas via `.discriminatedUnion()`

```typescript
const PaymentSchema = z.discriminatedUnion("method", [
  z.object({ method: z.literal("card"), cardNumber: z.string() }),
  z.object({ method: z.literal("bank"), accountNumber: z.string() }),
]);
```

When the shape of valid data depends on a discriminant field, Zod has first-class support for it.

---

## Equivalent Precept DSL

### Field declarations + constraints

```precept
field Name as string nullable
field Age as number default 0
field Email as string nullable

invariant Name != null && Name != "" because "Name is required"
invariant Name.length <= 100 because "Name too long"  # hypothetical — string length not in current DSL
invariant Age > 0 because "Age must be positive"
```

Cross-field constraint:

```precept
field RequestedAmount as number default 0
field ApprovedAmount as number default 0

invariant ApprovedAmount >= 0 because "Approved amount cannot be negative"
invariant ApprovedAmount <= RequestedAmount because "Approved cannot exceed requested"
```

Conditional schema (discriminated by a field value) — Precept's state model:

```precept
state CardPayment initial
state BankPayment

field PaymentMethod as string nullable
field CardNumber as string nullable
field AccountNumber as string nullable

in CardPayment assert CardNumber != null because "Card payment requires a card number"
in BankPayment assert AccountNumber != null because "Bank payment requires an account number"
```

---

## Gap Analysis

### What's equal
Cross-field invariants are roughly equivalent in statement count and arguably more readable in Precept:

```typescript
// Zod — 3 lines, one method call, path annotation
.refine(d => d.approvedAmount <= d.requestedAmount, 
  { message: "...", path: ["approvedAmount"] })
```

```precept
# Precept — 1 line, no path annotation needed (field names in message)
invariant ApprovedAmount <= RequestedAmount because "Approved cannot exceed requested"
```

**Precept wins on concision for cross-field invariants.** Zod needs a `.refine()` wrapper with an options object; Precept's `invariant` is direct.

### Where Precept is more verbose — GAP 1: Inline field constraints

**The most significant expressiveness gap.** Zod declares type + constraints in a single expression:

```typescript
name: z.string().min(1).max(100)   // 1 declaration + 2 constraints = 1 line
```

Precept requires three separate statements:

```precept
field Name as string nullable         # 1 — type declaration
invariant Name != null because "..."  # 2 — non-null constraint  
invariant Name != "" because "..."    # 3 — non-empty constraint
# (no string length constraint exists in current DSL)
```

For a 5-field entity with 2 constraints per field, Zod = 5 lines; Precept = 15+ lines. This is a **real statement-count gap for data-entry entities**.

**Root cause:** Precept deliberately separates field declaration (structural) from invariant (behavioral). This is correct for complex business rules, but creates verbosity for common, simple constraints like `> 0`, `!= ""`, and `<= 100`.

**Language lacks a construct.** Zod's inline chaining has no Precept equivalent for non-null and range checks co-located with the field declaration.

### Where Precept is more verbose — GAP 2: No string-length or pattern constraints

Zod has `.min(n)`, `.max(n)`, `.email()`, `.url()`, `.regex()` on string fields. Precept has no string length accessor (`.length` is not in the DSL), no format validators, and no regex matching. These require `on ... assert` event-level checks with manual string comparison, which is more verbose and doesn't cover invariant (always-hold) cases.

**Language lacks a construct** for string-length constraints entirely.

### Where Precept is richer
Precept's state-scoped `in <State> assert` is a more expressive version of Zod's discriminated union:

- Zod requires a separate schema variant per discriminant value.
- Precept's `in <State> assert` applies constraints automatically based on current state without duplicating the full schema.

**Precept wins on state-conditional validation.**

---

## Related GitHub proposal issues

- **#13 — Field-level range/basic constraints**: primary compactness proposal for the co-located simple-constraint pressure documented here.
- **#10 — String `.length` accessor**: narrow expression-surface fix for the missing string-size checks highlighted above.

The proposal bodies live in GitHub. This file remains the evidence base for why schema-style co-location feels attractive, where that conflicts with Precept's keyword-anchored design, and why state-scoped validation is still one of Precept's genuine advantages.

---

## Takeaway for Hero Sample

The hero sample should avoid fields with many simple constraints — it will look verbose compared to Zod. Prefer fields where the invariants are *business rules*, not format rules (e.g., `ApprovedAmount <= RequestedAmount` rather than `Name.length > 0`). Business-rule invariants are where Precept's expressiveness is *equal to or better than* Zod, not behind it.
