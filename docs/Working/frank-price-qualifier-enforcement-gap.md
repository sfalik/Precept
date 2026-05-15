# Price Qualifier Enforcement Gap — Frank

**Date:** 2026-05-15  
**Author:** Frank  
**Status:** Bug analysis — confirmed checker gap

---

## Summary

The `price in` declaration-side shape work from `docs/Working/frank-price-qualifier-shape-analysis.md` is already wired: the type system now models polymorphic `price in` qualifiers, declaration-site `price in 'kg'` / `price in 'USD/kg'` cases resolve correctly, and invalid `in` + `of` coexistence already emits `PRE0139`.

The remaining bug is narrower and uglier: **assignment-time qualifier validation does not read qualifier metadata from plain resolved `price` typed constants**. As a result, `set test5 = '4.17 USD/box'` can flow into `field test5 as price in 'each'` with no assignment diagnostic, even though the literal already resolved with a denominator unit.

This is **not** a language-shape problem and **not** a parser problem. It is a stale assignment-source extraction seam in the type checker.

---

## 1. Prior Wave 4 Work — What Already Exists

The prior Wave 4 analysis has already landed in source:

- `src/Precept/Language/Type.cs:39-80`  
  - `QualifierAxis.PriceIn`
  - `QualifierShape.OfRequiresCurrencyIn`
- `src/Precept/Language/DeclaredQualifierMeta.cs:82-96`  
  - `DeclaredQualifierMeta.CompoundPrice`
- `src/Precept/Language/Types.cs:40-44`  
  - `price` now uses `QS_CurrencyAndDimension` with `QualifierAxis.PriceIn`
- `src/Precept/Pipeline/TypeChecker.cs:116-164`  
  - declaration-side qualifier extraction enforces `OfRequiresCurrencyIn`
- `src/Precept/Pipeline/TypeChecker.cs:167-185,365-413`  
  - `MapLiteralQualifier` dispatches `QualifierAxis.PriceIn`
  - `MapPriceInQualifier(...)` resolves `currency`, `unit`, or `currency/unit`
- `src/Precept/Language/DiagnosticCode.cs:171-175` and `src/Precept/Language/Diagnostics.cs:329-335`  
  - `PRE0139 InvalidQualifierCoexistence` already exists

So the old shape gap is closed. The bug that remains is downstream of that work.

---

## 2. Repro and Observed Scope

Confirmed repro:

```precept
field test5 as price in 'each' default '10.00 EUR/each'

state offState initial
state onState terminal

event toggle initial

from offState on toggle
    -> set test5 = '4.17 USD/box'
    -> transition onState
```

This compiles without an assignment diagnostic, but it should fail because the target field declares the counting unit `each` and the assigned price literal carries `box`.

Additional spot checks show declaration-side enforcement is active:

- `price in 'kg' of 'mass'` correctly emits `PRE0139`
- `price in 'USD/kg' of 'mass'` correctly emits `PRE0139`

So the missing enforcement is specifically on **assignment of resolved price values**, not on declaration parsing or qualifier-shape validation.

---

## 3. Where Price Typed Constants Are Resolved

The assignment value is resolved before qualifier checking.

### 3.1 Literal resolution

`src/Precept/Pipeline/TypeChecker.Expressions.cs:243-259`

When a typed constant literal is resolved with an expected target type, the checker calls:

```csharp
var result = TypedConstantValidation.Validate(cv, rawText, targetType, typedConstantContext);
```

If validation succeeds, it immediately attaches qualifier metadata:

```csharp
var declaredQualifiers = ExtractQualifiersFromParsedValue(targetType, result.Value);
return new TypedTypedConstant(targetType, rawText, result.Value, declaredQualifiers, lit.Span);
```

### 3.2 Price parsed-value qualifier extraction

`src/Precept/Pipeline/TypeChecker.Expressions.cs:346-370`

For `TypeKind.Price`, `ExtractQualifiersFromParsedValue(...)` already produces both axes from a parsed price literal:

```csharp
case TypeKind.Price:
    if (parsedValue is ValueTuple<decimal, object?, UcumParsedUnit?>(_, CurrencyEntry priceCurrency, UcumParsedUnit priceUnit))
        return
        [
            new DeclaredQualifierMeta.Currency(priceCurrency.AlphaCode),
            new DeclaredQualifierMeta.Unit(
                priceUnit.CanonicalCode,
                UnitDimensionHelper.DeriveUnitDimensionName(priceUnit)),
        ];
```

That means `'4.17 USD/box'` is already carrying the information needed to reject assignment into `price in 'each'`.

**Conclusion:** resolution is not the bug. The qualifier data exists on the `TypedTypedConstant`.

---

## 4. Where Set-Action Assignment Checking Happens

`src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs:111-145`

For `AssignAction`, the checker:

1. Resolves the target field and target qualifiers
2. Resolves the assignment expression with target-type context
3. Calls `ValidateAssignmentQualifiers(...)`

Relevant code:

```csharp
var value = Resolve(assign.Value, ctx,
    fieldType != TypeKind.Error ? fieldType : null,
    fieldQualifiers);

...

if (value is not TypedErrorExpression
    && targetFieldMeta is not null
    && !targetFieldMeta.DeclaredQualifiers.IsDefaultOrEmpty)
{
    ValidateAssignmentQualifiers(
        value,
        fieldName,
        targetFieldMeta.DeclaredQualifiers,
        assign.Value.Span,
        ctx);
}
```

This is the correct phase. The bug is downstream inside the helper it calls.

---

## 5. The Actual Gap

### 5.1 Primary bug site

`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:14-44,46-113`

`ValidateAssignmentQualifiers(...)` delegates to `TryGetAssignmentSourceQualifiers(...)`:

```csharp
if (TryGetAssignmentSourceQualifiers(value, out var sourceQualifiers))
{
    ValidateResolvedQualifiers(sourceQualifiers, fieldName, targetQualifiers, valueSpan, ctx);
    return;
}
```

The extraction helper handles:

- `TypedFieldRef`
- `TypedArgRef`
- `TypedTypedConstant` for **money only**
- several binary-op derived qualifier cases
- static interpolated qualifier cases

But it has **no branch that reads `TypedTypedConstant.DeclaredQualifiers`**, and therefore no branch that surfaces plain `price` typed constants.

The stale special-case is here:

```csharp
case TypedTypedConstant
{
    ResultType: TypeKind.Money,
    ParsedValue: ValueTuple<decimal, object?> (_, CurrencyEntry currency)
}:
    qualifiers = [new DeclaredQualifierMeta.Currency(currency.AlphaCode)];
    return true;
```

That code predates the broader `TypedTypedConstant.DeclaredQualifiers` path and never grew a `price` equivalent.

### 5.2 Why the diagnostic never fires

`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:302-425`

`ValidateResolvedQualifiers(...)` already knows how to reject mismatched target qualifiers:

- `Dimension` target: lines `316-340` → emits `DimensionCategoryMismatch`
- `Unit` target: lines `342-360` → emits `QualifierMismatch`
- `Currency` target: lines `363-381` → emits `QualifierMismatch`

For the repro, the decisive branch is the `DeclaredQualifierMeta.Unit` target branch:

```csharp
case DeclaredQualifierMeta.Unit { UnitCode: var targetUnit }:
{
    var sourceUnit = qualifiers
        .OfType<DeclaredQualifierMeta.Unit>()
        .Select(q => q.UnitCode)
        .FirstOrDefault();

    if (sourceUnit is not null
        && !string.Equals(sourceUnit, targetUnit, StringComparison.OrdinalIgnoreCase))
    {
        ctx.Diagnostics.Add(
            Diagnostics.Create(
                DiagnosticCode.QualifierMismatch,
                valueSpan,
                targetUnit,
                fieldName));
    }
}
```

This logic is already correct. It simply never runs for plain price literals because `TryGetAssignmentSourceQualifiers(...)` fails to hand it the source qualifiers.

---

## 6. Where the Fix Belongs

### Correct enforcement point

**Method:** `TryGetAssignmentSourceQualifiers(...)`  
**File:** `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs`  
**Phase:** post-resolution assignment-source qualifier extraction, immediately before `ValidateResolvedQualifiers(...)`

That is the right place because it is the shared gate used by:

- set actions
- field defaults
- min/max bounds
- computed expressions
- event-arg defaults

The check does **not** belong in:

- `MapPriceInQualifier(...)` — declaration-site qualifier parsing only
- `ExtractQualifiers(...)` — field type declaration qualifier extraction only
- parser/catalog work — already complete

### Exact condition

For any assignment source expression that resolves to a `TypedTypedConstant` and already has non-empty `DeclaredQualifiers`, pass those qualifiers through.

Then existing target-side validation does the rest:

- target `price in 'each'` → target has `DeclaredQualifierMeta.Unit("each", "count")`
- source `'4.17 USD/box'` → source has `DeclaredQualifierMeta.Currency("USD")` and `DeclaredQualifierMeta.Unit("box", "count")`
- `ValidateResolvedQualifiers(...)` compares `sourceUnit` (`box`) to `targetUnit` (`each`)
- mismatch emits `PRE0068`

Architecturally, the minimal correct fix is to stop re-deriving per-type cases in `TryGetAssignmentSourceQualifiers(...)` and trust the already-resolved `TypedTypedConstant.DeclaredQualifiers` payload.

---

## 7. Diagnostic Decision

### Existing diagnostic applies

No new diagnostic code is needed.

Use the existing assignment mismatch diagnostic:

- `src/Precept/Language/DiagnosticCode.cs:143` → `QualifierMismatch = 68`
- `src/Precept/Language/Diagnostics.cs:602-606`

Current message:

> `Value does not match the '{0}' qualifier on field '{1}'`

That is the correct bucket for this bug.

For the repro, the emitted text should be:

> `Value does not match the 'each' qualifier on field 'test5'`

That is already how the `Unit` branch formats the mismatch: it passes `targetUnit` and `fieldName` into `PRE0068`.

### Existing diagnostics that are *not* the answer

- `PRE0139 InvalidQualifierCoexistence` is declaration-shape validation for invalid `price in ... of ...` combinations.
- `DimensionCategoryMismatch` is for dimension/category disagreement, not explicit unit-qualifier disagreement.

This bug is a straight assignment qualifier mismatch.

---

## 8. Tests Needed

Add the regression coverage under:

- `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs`

That file already owns assignment-time qualifier enforcement and already contains price-interpolated mismatch coverage.

### Required tests

1. **Set action — typed constant count-unit mismatch on price**  
   Suggested name:  
   `SetPriceField_FromTypedConstantCountUnitMismatch_EmitsQualifierMismatch`

   Scenario:
   ```precept
   field Cost as price in 'each' default '1 USD/each' writable
   ...
   -> set Cost = '4 USD/box'
   ```

   Expected: `PRE0068 QualifierMismatch`

2. **Set action — typed constant count-unit match on price**  
   Suggested name:  
   `SetPriceField_FromTypedConstantCountUnitMatch_NoDiagnostic`

   Scenario:
   ```precept
   field Cost as price in 'each' default '1 USD/each' writable
   ...
   -> set Cost = '4 USD/each'
   ```

   Expected: clean compile

3. **Set action — typed constant currency mismatch on dimension-qualified price**  
   Suggested name:  
   `SetPriceField_FromTypedConstantCurrencyMismatch_EmitsQualifierMismatch`

   Scenario:
   ```precept
   field Cost as price in 'USD' of 'mass' default '1 USD/kg' writable
   ...
   -> set Cost = '4 EUR/g'
   ```

   Expected: `PRE0068 QualifierMismatch`  
   Reason: proves plain price literals now surface currency qualifiers into assignment validation, not just unit qualifiers.

### Optional but worthwhile follow-up

Because the helper is shared beyond set actions, add one non-set regression (for example a field default or event-arg default using a plain price typed constant) if the implementation touches the generic `TypedTypedConstant` path. Not required for the bug report, but it would lock the shared seam instead of only the set-action surface.

---

## 9. Final Judgment

The missing enforcement is in **assignment-source qualifier extraction**, specifically `TryGetAssignmentSourceQualifiers(...)` failing to surface qualifier metadata from resolved plain `price` typed constants.

The fix belongs in the type checker's shared assignment-validation path, not in catalog metadata, parser logic, or a new diagnostic.

**Use existing `PRE0068 QualifierMismatch`.** The source literal already knows it is `USD/box`; the checker is simply failing to look.

---

## § Interpolated Unit Slot Gap

This is the interpolated-slot variant of the same seam, and it lands on a different typed expression shape than the plain literal case.

### Repro shape

```precept
field test3 as quantity in 'box' max 6 default '1 box'
field test5 as price in 'each' default '1 USD/each' writable
...
-> set test5 = '4.17 USD/{test3.unit}'
```

With the required-field noise removed, this still compiles clean. It should emit `PRE0068` because `test3.unit` is statically `box`, so the assigned value is effectively `price in 'box'` flowing into `price in 'each'`.

### What expression type it resolves to

`'4.17 USD/{test3.unit}'` does **not** resolve to `TypedTypedConstant`.

It matches **Price form P4** in `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:556-560`:

```csharp
// P4: "4.17 USD/" H[unit]
new([MatchNumericSpaceCurrencySlash, MatchEmpty], [InterpolationSlotKind.Unit]),
```

So `ResolveInterpolatedTypedConstant(...)` (`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:792-917`) returns:

- `InterpolatedTypedConstant`
- `ResultType = TypeKind.Price`
- one slot: `TypedInterpolationSlot(Expression = <resolved hole>, SlotKind = InterpolationSlotKind.Unit)`
- `StaticMagnitude = 4.17`
- `StaticQualifier = null`

The hole expression itself resolves through `ResolveMemberAccess(...)` (`src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs:790-867`) to:

- `TypedMemberAccess`
- `ResultType = TypeKind.UnitOfMeasure`
- `ResolvedAccessor = FixedReturnAccessor("unit", TypeKind.UnitOfMeasure, ..., ReturnsQualifier: QualifierAxis.Unit)`
- `Object = TypedFieldRef("test3", ..., DeclaredQualifiers = [Unit("box", "count")])`

So the qualifier data is present indirectly: the slot contains a `.unit` accessor over a field ref whose declared unit is already known.

### Why `TryGetAssignmentSourceQualifiers(...)` misses it

The gap is in `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:46-109`.

The only interpolated branch is the static-only branch:

```csharp
case InterpolatedTypedConstant { StaticQualifier: { } staticQual }:
    qualifiers = BuildQualifiersFromStaticInterpolated(staticQual);
    return !qualifiers.IsDefaultOrEmpty;
```

This repro does **not** satisfy that pattern, because `StaticQualifier` is null.

`ResolveStaticQualifier(...)` (`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:946-983`) leaves it null for two concrete reasons:

1. `hasUnitSlot` is true, so the unit-only static branch is excluded.
2. The concatenated static text is effectively `"4.17 USD/"`, and `TryExtractCurrencyAndUnit(...)` (`:1015-1037`) rejects trailing-slash text (`slashIndex == trimmed.Length - 1`).

So `TryGetAssignmentSourceQualifiers(...)` extracts **nothing** from this expression. It never drills into `InterpolatedTypedConstant.Slots`, never looks through `TypedMemberAccess.Object`, and therefore never surfaces the underlying `DeclaredQualifierMeta.Unit("box", "count")` from `test3`.

That means `ValidateResolvedQualifiers(...)` never receives a source unit, so the existing `DeclaredQualifierMeta.Unit` mismatch branch (`:338-356`) has nothing to compare and emits no `PRE0068`.

### Exact fix requirement

The fix belongs in **`TryGetAssignmentSourceQualifiers(...)`** in `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs`.

Add a non-static interpolated path for `InterpolatedTypedConstant` that inspects slot-based qualifier sources, specifically this shape:

- `InterpolatedTypedConstant { ResultType: TypeKind.Price, StaticQualifier: null, Slots: ... }`
- a slot with `SlotKind == InterpolationSlotKind.Unit`
- whose `Expression` is `TypedMemberAccess { ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: QualifierAxis.Unit }, Object: TypedFieldRef|TypedArgRef }`

For that shape, lift the underlying field/arg unit qualifier into the returned source qualifier set (and, for full parity, merge any statically-known currency text when present).

The checker already has the exact structural pattern for this member-access form in `ValidateUnitSlotDimensionConsistency(...)` (`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:1064-1094`). The assignment fix should mirror that seam instead of inventing a new parser- or catalog-level rule.

Once `TryGetAssignmentSourceQualifiers(...)` returns `DeclaredQualifierMeta.Unit("box", "count")` for this interpolated expression, the existing `ValidateResolvedQualifiers(...)` logic will emit the correct `PRE0068` against target `price in 'each'` with no new diagnostic code.
