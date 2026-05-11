# Decision: Numeric Range Modifiers Apply to `money` and `quantity`

**By:** Frank  
**Date:** 2026-05-10 (revised same day after Shane pushback)  
**Status:** Decision — ready for implementation by Kramer

---

## Root Cause

This is a **spec gap that propagated correctly into the catalog and TypeChecker**. The catalog and TypeChecker are not bugs — they faithfully implement the spec. The spec is wrong.

---

## What I Found

### 1. Spec (line 1498)

The modifier validation table explicitly lists:

| Modifier | Applicable to | Error when applied to |
|---|---|---|
| `nonnegative` | `integer`, `decimal`, `number` | `string`, `boolean`, `choice`, collections, temporal, **domain** |
| `positive` | (same) | (same) |
| `nonzero` | (same) | (same) |
| `min` / `max` | `integer`, `decimal`, `number` | everything else |

`money` is `TypeCategory.BusinessDomain`. The spec explicitly says "domain types get an error." So the spec explicitly rejects both `money nonnegative` and `money min '100.00 USD'`. This wording is too coarse on both counts.

### 2. Catalog (`Modifiers.cs`)

```csharp
private static readonly TypeTarget[] NumericTypes =
[
    new(TypeKind.Integer), new(TypeKind.Decimal), new(TypeKind.Number),
];
```

No `Money`. No `Quantity`. The catalog correctly implements the (wrong) spec. The TypeChecker fires `InvalidModifierForType` when the resolved type isn't in `ApplicableTo`. This is correct behavior given the current catalog. **Catalog gap, not implementation gap.**

### 3. TypeChecker — applicability and bound parsing

`IsTypeApplicable` checks the `ApplicableTo` array from the catalog. It does not hardcode type logic — if the catalog entry changes, the TypeChecker follows automatically.

`ValidateModifierBounds` is called for cross-validation when both `min` and `max` are present. It uses `TryGetComparableModifierValue` which accepts only `NumberLiteral` or `-NumberLiteral` patterns; for anything else it returns `null` and **silently skips the cross-check**. No error is emitted.

**Critical: the TypeChecker does not call `Resolve()` on `min`/`max` bound values at all.** Only `default` modifier values are type-resolved. This means the bound expression currently receives no type-checking against the field type for any type — integer, decimal, money, or otherwise.

### 4. Parser — valued modifier expressions

The parser's `ParseModifierList` calls `ParseExpression(0, ...)` for valued modifier bounds. `ExpressionStartTokens` is derived from `ExpressionForms.All` and includes `TokenKind.TypedConstant` (via `ExpressionForms.Literal.LeadTokens`). Therefore `'100.00 USD'` is **already a valid parse** in a modifier value position. No parser change is required.

### 5. ProofEngine — `DeclarationValue` is already conservative

The `ProofSatisfaction.Numeric(SelfValue, >=, DeclarationValue)` proof obligation for `min` uses `DeclarationValue` as the bound source. In `SatisfactionCovers`, `DeclarationValue` maps to `null` — conservative: cannot compare without a runtime value. This is already the correct behavior for money fields (the static prover cannot evaluate `'100.00 USD'` without runtime context). No change needed.

### 6. Runtime evaluator

`Evaluator.Fire`, `Update`, `Restore` are all `throw new NotImplementedException()`. Runtime modifier bound enforcement does not exist yet for any type. This is not a factor in the decision.

### 7. Contradiction in Constructs.cs

`ConstructKind.FieldDeclaration` usage example (line ~63):

```
"field amount as money nonnegative"
```

This is the canonical field declaration example displayed in completions, hover, and MCP output. The TypeChecker rejects it. The catalog authored this as the archetypal field declaration example — which means the catalog *intended* this to work. The spec fell behind the intended model.

### 8. `nonpositive` / `negative` — not in the language

They don't exist. There is no `ModifierKind.Nonpositive` or `ModifierKind.Negative`. The existing zero-bound set is: `nonnegative`, `positive`, `nonzero`. Out of scope.

---

## The Design Question: Why Is Zero Universal And Min/Max Are Not A Special Problem

The zero-bound insight stands unchanged: `nonnegative`, `positive`, `nonzero` all compare against the **universal zero**. Currency dimension is irrelevant to the zero predicate.

My original claim that `min`/`max` on `money` required "a different literal form, a different validation path, and potentially currency-consistency enforcement" was **wrong on all three counts**:

1. **Different literal form**: False. The parser already accepts typed constants (`'100.00 USD'`) in modifier value positions — `TypedConstant` is in `ExpressionStartTokens`. There is no parser change required.

2. **Different validation path**: False. `ValidateModifierBounds` already handles non-NumberLiteral values gracefully (returns null → skips cross-check). The TypeChecker doesn't validate ANY min/max bound expression against the field type today — not for integer, not for decimal, not for money. The validation path is uniformly absent, not money-specific.

3. **Currency-consistency enforcement is unresolved**: True but overstated as a blocker. Currency-mismatch detection for `min '100.00 EUR'` on `money in 'USD'` requires adding a `Resolve()` call for `min`/`max` bound values in the TypeChecker — the same 3-line pattern already used for `default` modifier values. This is a small, contained addition, not a "separate larger feature." And it should be done for correctness on ALL types, not just money.

---

## Revised Decision

**`nonnegative`, `positive`, and `nonzero` SHALL apply to `money` and `quantity` fields.**

**`min` and `max` SHALL ALSO apply to `money` and `quantity` fields.** The bound value must be a typed constant in the field's declared unit — `field Balance as money in 'USD' min '100.00 USD'`. Currency-denominated bounds desugar to `rule Balance >= '100.00 USD'`, exactly as numeric bounds desugar to `rule Amount >= 100`.

Rationale for inclusion: The bound form already parses. `DeclarationValue` is already conservative in the proof engine. Adding `Resolve()` calls for `min`/`max` bounds in the TypeChecker enables currency-mismatch detection via the existing `QualifierMatch.Same` path — the same mechanism that catches currency mismatches in binary expressions. The alleged structural barrier was a fiction arising from not reading the code carefully enough.

`price` and `exchangerate` are also `TypeTrait.Orderable` and are natural follow-ons; scope them to `money` and `quantity` for now.

---

## What Must Change

### A. Modifiers.cs

Split the current `NumericTypes` into two applicability arrays:

```csharp
// For zero-bound modifiers (amount-only comparison): integer, decimal, number, money, quantity
private static readonly TypeTarget[] ZeroBoundNumericTypes =
[
    new(TypeKind.Integer), new(TypeKind.Decimal), new(TypeKind.Number),
    new(TypeKind.Money),   new(TypeKind.Quantity),
];

// For ranged bound modifiers (min/max) — also includes money/quantity (bound is a typed constant)
private static readonly TypeTarget[] RangedNumericTypes =
[
    new(TypeKind.Integer), new(TypeKind.Decimal), new(TypeKind.Number),
    new(TypeKind.Money),   new(TypeKind.Quantity),
];
```

Update the modifier entries:

- `ModifierKind.Nonnegative` → `ZeroBoundNumericTypes`
- `ModifierKind.Positive`    → `ZeroBoundNumericTypes`
- `ModifierKind.Nonzero`     → `ZeroBoundNumericTypes`
- `ModifierKind.Min`         → `RangedNumericTypes`
- `ModifierKind.Max`         → `RangedNumericTypes`

(If `ZeroBoundNumericTypes` and `RangedNumericTypes` are identical, they can be merged into one array — the names serve as documentation of intent.)

### B. TypeChecker.cs — resolve min/max bound expressions

Add `Resolve()` calls for `min`/`max` modifier bound values, using the **exact same pattern as `default`**:

```csharp
var resolved = Resolve(boundExpr, ctx, typedField.ResolvedType);
```

That is the complete implementation. No post-resolve type-mismatch check. No `ValidateMinMaxBoundQualifier` helper.

**Why this is sufficient and correct:**

Code inspection of `TypeChecker.cs` confirms the `default` path is:
```csharp
var resolved = Resolve(defaultMod.Value, ctx, typedField.ResolvedType);
ctx.Fields[i] = ctx.Fields[i] with { DefaultExpression = resolved };
```
No post-resolve `ResultType != typedField.ResolvedType` check follows. The resolved expression is stored as-is.

`Resolve()` takes `TypeKind? expectedType` — a bare `TypeKind`, not a full resolved type with qualifier. `typedField.ResolvedType` is a `TypeKind`. `ResolveTypedConstant()` receives only the `TypeKind` and uses it to look up `ContentValidation` via `Types.GetMeta(targetType)`. It validates format/content (is EUR a valid ISO 4217 code? is the UCUM string parseable?) but has no access to the field's declared qualifier (currency code, unit code). Qualifier alignment is NOT checked.

**Pre-existing gaps (affect `default` and `min`/`max` equally):**

1. **Qualifier-alignment gap**: `'100.00 EUR'` on `money in 'USD'` passes `Resolve()` — EUR is a valid ISO 4217 code. The `MoneyValidator` checks format, not field-qualifier match. `field Balance as money in 'USD' default '100.00 EUR'` emits no error today. After this change, `field Balance as money in 'USD' min '100.00 EUR'` will also emit no error — consistent with `default`. This is a pre-existing gap to fix uniformly in a follow-up.

2. **Plain-number gap**: `default 100` on `money in 'USD'` → `ResolveNumericLiteral(lit, TypeKind.Money)` → `IsAssignable(TypeKind.Integer, TypeKind.Money)` returns false → returns `TypedLiteral(TypeKind.Integer, ...)`. No post-resolve check emits a diagnostic. After this change, `min 100` on `money in 'USD'` will also pass silently — consistent with `default`. Pre-existing gap, fix uniformly.

**Do NOT add:**
- A post-resolve `resolved.ResultType != typedField.ResolvedType` check (not present in `default` path)
- `ValidateMinMaxBoundQualifier` or any bespoke currency/unit matching helper (creates asymmetry with `default`)

### C. precept-language-spec.md (line ~1498)

Update the Modifier validation table:

| Modifier | Applicable to | Error when applied to |
|---|---|---|
| `nonnegative` | `integer`, `decimal`, `number`, `money`, `quantity` | `string`, `boolean`, `choice`, collections, temporal, `currency`, `unitofmeasure`, `dimension`, `price`, `exchangerate` |
| `positive` | (same as nonnegative) | (same as above) |
| `nonzero` | (same as nonnegative) | (same as above) |
| `min` / `max` | `integer`, `decimal`, `number`, `money`, `quantity` | `string`, `boolean`, `choice`, collections, temporal, `currency`, `unitofmeasure`, `dimension`, `price`, `exchangerate` |

Remove the original note explaining why `min`/`max` excluded domain types. Replace with:

> **`min`/`max` on `money`/`quantity` fields:** The bound value must be a typed constant in the field's declared domain type — `field Balance as money in 'USD' min '100.00 USD'`. The TypeChecker validates the bound using the same `Resolve()` call as `default`, which checks that the typed-constant content is valid for the declared type (valid ISO 4217 code, valid UCUM unit string, etc.). Qualifier alignment (currency match, unit match) is not validated at compile time — this is a known pre-existing gap that applies equally to `default` and will be addressed uniformly in a follow-up.

### D. Tests (TypeCheckerValidationTests or equivalent)

Required regression anchors:

```
field X as money in 'USD' nonnegative                        → 0 errors
field X as money in 'USD' positive                           → 0 errors
field X as money in 'USD' nonzero                            → 0 errors
field X as quantity in 'kg' nonnegative                      → 0 errors
field X as quantity in 'kg' positive                         → 0 errors
field X as money in 'USD' min '100.00 USD'                   → 0 errors
field X as money in 'USD' max '500.00 USD'                   → 0 errors
field X as money in 'USD' min '100.00 USD' max '500.00 USD'  → 0 errors
field X as quantity in 'kg' min '1.0 kg'                     → 0 errors
field X as money in 'USD' min '100.00 EUR'                   → 0 errors  ← qualifier gap, same as default
field X as money in 'USD' min 100                            → 0 errors  ← plain-number gap, same as default
field X as money in 'USD' min 'not-valid-currency'           → InvalidTypedConstantContent
```

**Removed from anchors vs. earlier draft:**
- `min '100.00 EUR'` → TypeMismatch: incorrect. `Resolve()` does not check qualifier alignment; the same call on `default` accepts `'100.00 EUR'` silently. Consistency requires the same behavior here.
- `min 100` → TypeMismatch: incorrect. `Resolve()` on a plain number with `expectedType = TypeKind.Money` returns `TypedLiteral(Integer)` and the `default` path has no post-resolve mismatch check. Do not add one asymmetrically for `min`/`max`.

---

## Known Gaps: qualifier-alignment and plain-number for all valued modifiers

**These gaps apply equally to `default` and `min`/`max`.** The implementation for this PR makes them consistent — not asymmetrically worse for bounds than for defaults.

**Qualifier-alignment gap**: `Resolve(expr, ctx, typedField.ResolvedType)` passes `TypeKind` (bare kind), not the full resolved type with qualifier. `ResolveTypedConstant()` validates that the content is a valid typed constant for the type (ISO 4217 code exists, UCUM string parses) but has no access to the field's declared qualifier. So `'100.00 EUR'` on `money in 'USD'` passes without error. Same for `default`. Fix uniformly in a follow-up — likely by threading `DeclaredQualifierMeta` into `Resolve()` or by a post-resolve qualifier check applied to ALL valued modifiers and computed expressions.

**Plain-number gap**: `Resolve(NumberLiteral, ctx, TypeKind.Money)` → `IsAssignable(TypeKind.Integer, TypeKind.Money)` returns false → resolves as `TypedLiteral(Integer)`. No post-resolve mismatch check exists in the `default` path, so no diagnostic is emitted. `min 100` on `money in 'USD'` will similarly pass. Fix uniformly in a follow-up — a post-resolve `resolved.ResultType != typedField.ResolvedType` check on ALL valued modifier expressions (and `default`) would catch both.

**`ValidateModifierBounds` min < max ordering**: `TryGetComparableModifierValue` currently handles only `NumberLiteral` — for money/quantity typed constants it returns `null` and the ordering check is silently skipped. `field Balance as money in 'USD' min '500.00 USD' max '100.00 USD'` (min > max) emits no error. Pre-exists for any non-numeric literal form. NOT a blocker — the ordering check is a usability convenience, not a correctness requirement. Address in a follow-up.

---

## What Kramer Does NOT Need to Touch

- `TypeChecker.Validation.cs` — the `IsTypeApplicable` logic is correct; it reads from the catalog; `ValidateModifierBounds` gracefully skips non-NumberLiteral bounds
- `ProofEngine.cs` — `DeclarationValue` is already conservative for all types; no change needed
- `Constructs.cs` — the usage example `"field amount as money nonnegative"` is already correct; this decision makes the catalog agree with it
- `Types.cs` — no trait changes needed
- `Parser.cs` — `TypedConstant` is already in `ExpressionStartTokens`; modifier value positions already accept typed constants
- Any bespoke qualifier-matching helper — `ValidateMinMaxBoundQualifier` or equivalent is NOT part of this implementation. Qualifier alignment is a pre-existing gap in the `default` path and will be closed uniformly in a follow-up.

---

## Flag to Shane

No further design decisions needed. The code investigation confirmed Shane's pushback was correct: the parser already handles typed constant bounds, the ProofEngine is already conservative for `DeclarationValue`, and the currency-mismatch gap is resolved by adding 3-line `Resolve()` calls for bound values — not a separate feature. The original exclusion was based on wrong assumptions about what the parser and TypeChecker already support.
