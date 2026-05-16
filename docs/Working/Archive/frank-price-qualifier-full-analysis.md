# Price Qualifier Enforcement — Full Analysis

**By:** Frank  
**Date:** 2026-05-15T16:25:45.143-04:00  
**Status:** Analysis — governs remaining price qualifier enforcement work

---

This document consolidates and supersedes the narrower findings in:

- `docs/Working/frank-price-qualifier-shape-analysis.md`
- `docs/Working/frank-price-qualifier-enforcement-gap.md`

The declaration-side `price in` shape work is already in place. The remaining problem is the assignment-time enforcement model. Right now that model is incomplete, partially duplicated, and structurally too weak: it treats "source qualifier unknown" as silent success in multiple paths.

That is not acceptable for Precept.

---

## § 1. Assignment Source Forms Catalog

`set {price_field} = ...` accepts a general expression. In practice, qualifier-bearing price assignments fall into the forms below.

### 1.1 Core rule

For assignment enforcement, the checker must reason **per target qualifier axis**:

- **Currency axis** — `Currency`
- **Exact denominator unit axis** — `Unit`
- **Denominator dimension/category axis** — `Dimension`
- **Exchange-rate axes** — `FromCurrency`, `ToCurrency`

The current helper in `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs`:

- `ValidateAssignmentQualifiers(...)`
- `TryGetAssignmentSourceQualifiers(...)`
- `TryBuildQualifiersFromInterpolatedSlots(...)`
- `ValidateResolvedQualifiers(...)`

works with a single `ImmutableArray<DeclaredQualifierMeta>` payload and a boolean success/fail return. That shape is the root architectural problem: it cannot distinguish:

1. **resolved and matching**
2. **resolved and mismatching**
3. **required axis exists but is statically unknown**
4. **axis is irrelevant because target does not constrain it**

Those are not the same thing.

### 1.2 Forms table

| # | Source form | Example | Compile-time qualifier data available | Current checker behavior | Required behavior |
|---|-------------|---------|---------------------------------------|--------------------------|-------------------|
| 1 | **Direct field ref** | `set Cost = otherPrice` | Whatever is in `TypedFieldRef.DeclaredQualifiers` | If qualifiers are non-empty, `TryGetAssignmentSourceQualifiers(...)` returns them and `ValidateResolvedQualifiers(...)` runs. If source field is bare (`price`, `money`, `quantity`, `exchangerate` with no qualifiers), the helper returns `false` and assignment silently passes. | Known mismatch must fail. Bare source flowing into constrained target must also fail — not with mismatch, but with an "unproved assignment qualifier" diagnostic. |
| 2 | **Direct arg ref** | `set Cost = PriceArg` | Whatever is in `TypedArgRef.DeclaredQualifiers` | Same as field refs. Known mismatches are caught; bare args silently pass into constrained targets. | Same as field refs. |
| 3 | **Plain typed constant literal** | `'4.17 USD/each'`, `'10 EUR'`, `'1.08 USD/EUR'` | `TypedTypedConstant.DeclaredQualifiers` for money/price/exchangerate; quantity literal validation is context-aware in `QuantityValidator.Validate(...)` | After George's fix, money/price/exchangerate typed constants with non-empty `DeclaredQualifiers` now flow through `TryGetAssignmentSourceQualifiers(...)`. Quantity literals short-circuit at the top of `ValidateAssignmentQualifiers(...)` and rely on `QuantityValidator.Validate(...)` instead. | Keep this behavior. Direct typed constants already carry definitive qualifier facts. |
| 4 | **Interpolated typed constant with fully static qualifier** | `'{n} USD'`, `'{n} kg'`, `'{n} USD/kg'`, `'{n} USD/EUR'` | `InterpolatedTypedConstant.StaticQualifier` | Correctly handled by `BuildQualifiersFromStaticInterpolated(...)` → `ValidateResolvedQualifiers(...)`. | Keep this behavior. |
| 5 | **Interpolated typed constant — whole-value hole** | `'{otherPrice}'` | The hole expression itself has qualifiers; no `StaticQualifier` is set for `WholeValue` | **Not handled.** `ResolveStaticQualifier(...)` intentionally returns `null` for `WholeValue`, and `TryBuildQualifiersFromInterpolatedSlots(...)` ignores `WholeValue`. Result: known mismatches compile clean. Verified: `field tgt as price in 'USD' of 'mass'`, `field src as price in 'EUR' of 'mass'`, `set tgt = '{src}'` compiles with 0 diagnostics. | Treat `WholeValue` as a transparent wrapper over the inner expression. If the wrapped expression is incompatible, reject exactly as the unwrapped form would reject. |
| 6 | **Interpolated typed constant — unit slot from source with known exact unit** | `'4.17 USD/{qty.unit}'` where `qty as quantity in 'box'` | Slot is `TypedMemberAccess(.unit)` over a field/arg whose `DeclaredQualifiers` include `Unit(...)` | Partially handled. `TryBuildQualifiersFromInterpolatedSlots(...)` lifts `DeclaredQualifierMeta.Unit` from the source object. This is the path George fixed for known-unit price mismatches. | Keep, but only as one case in a generalized slot resolver. |
| 7 | **Interpolated typed constant — unit slot from source with known dimension but no exact unit** | `'4.17 USD/{qty.unit}'` where `qty as quantity of 'mass'` | Source object can prove `Dimension("mass")` but not exact unit code | **Incorrectly partial.** `TryBuildQualifiersFromInterpolatedSlots(...)` only lifts `DeclaredQualifierMeta.Unit`, not `Dimension`. The dimension fact is lost. Current behavior therefore depends on accidents: `PRE0124` catches known dimension mismatches, but exact-unit targets silently pass because the checker sees no source unit. Verified: `field tgt as price in '[lb_av]'`, `field src as quantity of 'mass'`, `set tgt = '1 USD/{src.unit}'` compiles clean. | Resolve axes independently. For `Unit` targets, source exact unit is unknown → reject. For `Dimension` targets, source dimension is known and can satisfy `of 'mass'`. |
| 8 | **Interpolated typed constant — unit slot from source with no declared unit or dimension** | `'4.17 USD/{bareQty.unit}'` where `bareQty as quantity` | The slot type is `unitofmeasure`, but the source contributes no declared `Unit` or `Dimension` qualifier | **Incorrectly accepted.** `TryBuildQualifiersFromInterpolatedSlots(...)` returns `false` or returns only partial price-currency info. `ValidateResolvedQualifiers(...)` never sees the required denominator fact. `ValidateUnitSlotDimensionConsistency(...)` explicitly conservative-accepts unknown source dimensions. Verified by the user repro and by `field tgt as price of 'mass'`, `field src as quantity`, `set tgt = '1 USD/{src.unit}'` compiling with 0 diagnostics. | If the target constrains the denominator axis (`in '[lb_av]'` or `of 'mass'`), this must fail. Unknown is not good enough. |
| 9 | **Interpolated typed constant — currency slot from qualified source** | `'{n} {moneyField.currency}'`, `'{n} {priceField.currency}/kg'` | Slot is `TypedMemberAccess(.currency)` over a field/arg whose source object may already declare `Currency(...)` | **Not handled at all.** `TryBuildQualifiersFromInterpolatedSlots(...)` only understands unit slots. Verified: `field tgt as money in 'USD'`, `field src as money in 'EUR'`, `set tgt = '{1} {src.currency}'` compiles with 0 diagnostics. Verified: `field tgt as price in 'USD' of 'mass'`, `field p as price in 'EUR' of 'mass'`, `field u as quantity in 'kg'`, `set tgt = '{4} {p.currency}/{u.unit}'` compiles with 0 diagnostics. | Currency-slot source qualifiers must be resolved the same way unit-slot qualifiers are resolved. Known mismatch → `PRE0068`. Unknown required currency axis → new unproved-assignment diagnostic. |
| 10 | **Interpolated typed constant — exchange-rate from/to slots** | `'{n} {rate.from}/{rate.to}'` | `TypedMemberAccess(.from/.to)` over source with declared `FromCurrency` / `ToCurrency` | **Not handled at all.** Verified: `field tgt as exchangerate in 'USD' to 'EUR'`, `field src as exchangerate in 'GBP' to 'EUR'`, `set tgt = '{1} {src.from}/{src.to}'` compiles with 0 diagnostics. | Resolve `FromCurrency` and `ToCurrency` axes independently. |
| 11 | **Unary wrapper** | `-srcPrice` | Same qualifier facts as operand | Current `ValidateAssignmentQualifiers(...)` recurses through `TypedUnaryOp`. Known mismatches are caught. Verified: `-src` with `src as price in 'EUR' of 'mass'` into `tgt as price in 'USD' of 'mass'` emits `PRE0068`. | Keep, but use axis-state propagation rather than ad hoc recursion. |
| 12 | **Binary result — transparent scalar-preserving** | `price * 2`, `price / 2`, `money * 2`, `money / 2`, `quantity * 2`, `quantity / 2` | Result inherits the qualifier-bearing operand | Current checker recurses into children. This works for many known cases because the qualifier-bearing child is still present. | Keep conceptually, but make it explicit: these are qualifier-preserving wrappers, not child-by-child guesswork. |
| 13 | **Binary result — same-type arithmetic** | `a + b`, `a - b` | If operands are compatible, result inherits the shared qualifier; if not, type checker already reports arithmetic qualifier errors | Current checker recurses into both children. For obvious known mismatches, that often works. For unknown bare operands flowing into constrained targets, recursion falls off a cliff and silently accepts. | Result qualifier should be derived from the operation contract. If the result axis cannot be proved and the target constrains it, reject. |
| 14 | **Binary result — transformed qualifier result** | `money / quantity -> price`, `price / quantity -> price`, `rate * money -> money` | The result qualifier is not equal to either raw child; it is derived via `ResultQualifier` metadata and operand qualifiers | Current helper has special cases for `CompoundUnitCancellationRequired`, `CurrencyConversionRequired`, and `CompoundDimensionElevationRequired`. But the "unknown result" paths deliberately return `true` with empty qualifiers, which suppresses further checking. That is structurally wrong. | Never collapse "unknown derived qualifier" into silent success. Derived result unknown + constrained target must reject. |
| 15 | **Conditional expression** | `if flag then p1 else p2` | Branch qualifiers are available independently | **Not handled.** `TypedConditional` is never inspected by `ValidateAssignmentQualifiers(...)`. Verified: `if flag then src else src` with both branches `price in 'EUR' of 'mass'` assigned to `price in 'USD' of 'mass'` compiles clean. | Validate every reachable branch against the target, or synthesize a conditional result qualifier only when both branches prove the same compatible qualifier. |
| 16 | **Function call returning qualified value** | `abs(MoneyField)`, `round(MoneyField, 2)`, `abs(Qty)` | The overload metadata already declares qualifier preservation via `FunctionOverload.Match` | **No assignment-source handling.** `TypedFunctionCall` is invisible to `ValidateAssignmentQualifiers(...)`. This is not a current price form because there are no built-in price-returning functions today, but it is a live money/quantity gap. | The shared assignment qualifier resolver must understand qualifier-preserving function results. |

### 1.3 Important current distinction

There are three different enforcement mechanisms in play today:

1. **Direct assignment qualifier comparison** — `ValidateAssignmentQualifiers(...)` / `ValidateResolvedQualifiers(...)`
2. **Typed-constant structural validation** — especially `QuantityValidator.Validate(...)`
3. **Slot-specific structural validation** — `ValidateUnitSlotDimensionConsistency(...)` → `PRE0124 DimensionMismatchInUnitSlot`

These mechanisms overlap, but they are not equivalent. `PRE0124` is a useful early structural diagnostic; it is **not** a complete substitute for assignment qualifier enforcement.

---

## § 2. Enforcement Semantics

### 2.1 The correct rule

The right answer is **not** (a) "always fail" and **not** (b) "always defer to runtime".

The correct rule is the generalized form of **(c)**:

> **For each qualifier axis constrained by the target field, the assignment must prove compatibility on that axis at compile time. If it cannot, the assignment is rejected. If the target does not constrain that axis, the source may remain unknown on that axis.**

That is the only rule consistent with Precept's prevention-first philosophy.

### 2.2 Why runtime deferral is wrong

Precept already rejects unproved arithmetic qualifier compatibility at compile time via `PRE0114 UnprovedQualifierCompatibility`. Assignment into a constrained field is not weaker than arithmetic. It is the direct mutation path that creates persisted entity state.

If the compiler allows:

- `price in '[lb_av]' <- '4.17 USD/{bareQty.unit}'`
- `money in 'USD' <- bareMoneyField`
- `exchangerate in 'USD' to 'EUR' <- '{1} {src.from}/{src.to}'`

then it is allowing a contract that may write an invalid configuration depending on runtime values. That is the exact class of hole Precept exists to close.

### 2.3 Axis-by-axis semantics

#### Currency / FromCurrency / ToCurrency

- **Resolved equal or symbolically same source** → pass
- **Resolved unequal** → `PRE0068 QualifierMismatch`
- **Unresolved while target constrains the axis** → reject with a new assignment-specific unproved-qualifier diagnostic
- **Unresolved while target does not constrain the axis** → pass

#### Exact Unit

- **Resolved exact unit equal or symbolically same source** → pass
- **Resolved exact unit unequal** → `PRE0068 QualifierMismatch`
- **Only dimension known, exact unit unknown, target requires exact unit** → reject with the new unproved-assignment diagnostic
- **Target does not require exact unit** → source may remain unknown on exact-unit axis

Count units are not special here. `each` versus `box` is not "same count family therefore good enough." Exact counting-unit identity is required whenever the target uses `in 'each'`, `in 'box'`, etc.

#### Dimension

- **Resolved source dimension equal** → pass
- **Resolved source dimension unequal** → `PRE0069 DimensionCategoryMismatch` or `PRE0124` when the mismatch is specifically a unit-slot structural conflict
- **Source exact unit known and its derived dimension matches** → pass
- **Source dimension unknown while target constrains dimension** → reject with the new unproved-assignment diagnostic
- **Target has no dimension constraint** → pass

### 2.4 Form-by-form semantics

#### Direct field/arg refs

- Qualified source into constrained target: check directly
- Bare source into constrained target: reject
- Bare source into unconstrained target: pass

#### Plain typed constants

- If the literal carries definitive qualifier data, enforce it immediately
- Quantity literals remain context-validated at parse/resolve time; that is already the strict lane

#### Interpolated typed constants

- **Static qualifier**: enforce immediately
- **WholeValue hole**: same rule as the wrapped expression
- **Currency/unit/from/to slots**: resolve the source object's declared qualifier on that axis
- **If the slot source proves only some axes**: use those facts only for those axes; missing required axes are errors

#### Binary / unary expressions

- Unary qualifier-preserving wrappers inherit operand qualifier state
- Binary qualifier-preserving wrappers inherit the qualifier-bearing operand state
- Binary transformed-result operations must derive the **result** qualifier state, not peek at children and hope
- Unknown derived result on a required target axis is a hard compile-time rejection

#### Conditional expressions

- Both branches must satisfy the target
- If one branch mismatches and the other does not, the assignment still fails
- If both branches are compatible, pass
- If either branch cannot prove a required axis, fail

### 2.5 The user's two concrete examples

#### `set test5 = '4.17 USD/{test3.unit}'` into `field test5 as price in '[lb_av]'`

If `test3` is bare `quantity`, the source denominator unit is statically unknown.

- Target constrains **exact unit** (`[lb_av]`)
- Source cannot prove exact unit
- **Correct result: reject at compile time**

This is not a runtime-defer case.

#### `set test6 = '10.00 EUR/{test3.unit}'` into `field test6 as price of 'mass'`

If `test3` is bare `quantity`, the source denominator dimension is statically unknown.

- Target constrains **dimension** (`mass`)
- Bare `quantity` does not prove its unit is a mass unit
- **Correct result: reject at compile time**

If instead `test3 as quantity of 'mass'`, then the source proves the denominator dimension even though the exact unit code remains unknown. In that case:

- `price of 'mass'` should pass
- `price in '[lb_av]'` should still fail

That is the precise axis-by-axis rule.

### 2.6 Diagnostic semantics

`PRE0068 QualifierMismatch` is correct only for **definite static disagreement**.

It is **not** semantically correct for:

- source axis absent because source is bare
- slot source axis unknown because the underlying field is unconstrained
- derived result axis unknown after conversion/cancellation/elevation
- conditional branch axis unknown

Those are not "mismatch" cases. They are "cannot prove the assignment satisfies the target qualifier" cases.

So:

- **Definite mismatch** → existing diagnostics (`PRE0068`, `PRE0069`, `PRE0124`)
- **Unknown required axis** → **new type-stage diagnostic required**

Do **not** overload `PRE0068` for uncertainty.

---

## § 3. Gap Map

### 3.1 What is already correct

| Case | Current behavior | Status |
|------|------------------|--------|
| Direct qualified field/arg ref mismatch | `PRE0068` / `PRE0069` emitted | Correct |
| Static interpolated qualifier mismatch (`'{n} EUR'`, `'{n} g'`, `'{n} EUR/g'`, `'{n} USD/EUR'`) | Emitted through `BuildQualifiersFromStaticInterpolated(...)` | Correct |
| Known unit-slot mismatch on price exact-unit target | Emitted after George's fix | Correct |
| Known dimension mismatch in a unit slot | `PRE0124 DimensionMismatchInUnitSlot` | Correct |
| Unary wrapper over known mismatching qualified source | Current recursion catches it | Correct |
| Plain money/price/exchangerate typed constants | Now flow through `TypedTypedConstant.DeclaredQualifiers` | Correct |

### 3.2 Current silent gaps — verified

| Case | Verified current result | Required result |
|------|-------------------------|-----------------|
| Bare `money` field into `money in 'USD'` | Compiles clean | Reject — source currency unproved |
| Bare `price` field into `price in 'USD'` | Compiles clean | Reject — source currency/unit or dimension unproved |
| Whole-value interpolation `'{src}'` where `src` is mismatched qualified `price` | Compiles clean | `PRE0068` / `PRE0069` as appropriate |
| Conditional `if flag then src else src` where branches are mismatched qualified `price` or `money` | Compiles clean | Reject |
| Currency-slot interpolation from known mismatching money source | Compiles clean | `PRE0068` |
| Exchange-rate `from/to` slot interpolation from known mismatching source | Compiles clean | `PRE0068` |
| Price currency-slot interpolation from known mismatching source | Compiles clean | `PRE0068` |
| Exact-unit price target with source dimension-only unit slot (`quantity of 'mass'`) | Compiles clean | Reject — exact unit unproved |
| Dimension-only price target with bare quantity unit slot | Compiles clean | Reject — dimension unproved |
| Derived binary result with unknown qualifier on required axis (`rate * money`, `money / quantity`, `price / quantity`) | Silent because helper returns `true` with empty qualifiers in some paths | Reject |

### 3.3 Current partial gaps

#### Gap A — slot handling is axis-incomplete

`TryBuildQualifiersFromInterpolatedSlots(...)` only understands:

- static price currency text before `/`
- `InterpolationSlotKind.Unit`

It does **not** understand:

- `Currency`
- `FromCurrency`
- `ToCurrency`
- `WholeValue`
- dimension-only information from a unit-slot source when there is no exact `Unit(...)`

That is why the current price fix is only a slice, not the model.

#### Gap B — boolean extraction API collapses unknown into silence

`TryGetAssignmentSourceQualifiers(...)` returns `bool` plus a partial qualifier array.

That API cannot represent:

- "currency known, unit unknown"
- "dimension known, exact unit unknown"
- "axis required but unresolved"

So the implementation silently drops facts it cannot fully represent.

#### Gap C — direct bare-source refs are structurally unguarded

`TypedFieldRef` / `TypedArgRef` branches only fire when `DeclaredQualifiers` is non-empty.

That means:

- `set usdMoney = bareMoney`
- `set lbPrice = barePrice`
- `set usdEur = bareRate`

all pass today if the source declaration is unconstrained.

That is a prevention failure.

#### Gap D — transformed-result binary paths suppress enforcement on uncertainty

Two explicit cases in `TryGetAssignmentSourceQualifiers(...)` are wrong by construction:

- `CurrencyConversionRequired` returns `true` with `qualifiers = []` when `ToCurrency` is unknown
- `CompoundDimensionElevationRequired` returns `true` with `qualifiers = []` when result qualifiers cannot be resolved

That suppresses recursive fallback and also suppresses diagnostics.

The checker is explicitly choosing silence where the model requires rejection.

#### Gap E — conditional and whole-value wrappers are invisible

- `TypedConditional` is never inspected
- `InterpolatedTypedConstant` with `WholeValue` slot is never inspected

Both forms preserve or select existing qualifier-bearing values, but the assignment checker currently treats them as if they have no qualifier semantics.

### 3.4 Price-specific gap summary

For `price`, the current enforcement is therefore:

- **good** on direct known refs, static qualifiers, known exact unit-slot mismatches
- **bad** on whole-value interpolation, conditional selection, currency slots, bare-source refs, unknown unit-slot sources, and unknown derived-result qualifiers

That is not a complete enforcement model. It is a patchwork.

---

## § 4. Architectural Recommendation

### 4.1 The fix must be architectural, not another patch branch

Do **not** add one more special case to `TryBuildQualifiersFromInterpolatedSlots(...)` and call it done. That path is already the wrong abstraction.

The correct fix is to replace the current assignment-source extraction seam with a **shared expression-qualifier resolver** that operates per axis and can represent uncertainty explicitly.

### 4.2 New internal contract

Add a shared internal model in `src/Precept/Pipeline/` for assignment/proof qualifier resolution, conceptually like:

```csharp
internal enum QualifierResolutionKind
{
    Resolved,
    Unknown,
    Absent
}

internal sealed record ResolvedQualifierAxis(
    QualifierAxis Axis,
    QualifierResolutionKind Kind,
    DeclaredQualifierMeta? Qualifier,
    string? Reason = null);
```

The exact type name can vary. The important point is this:

> The resolver must distinguish **Resolved** from **Unknown**. A partial qualifier array cannot do that.

### 4.3 Where the logic should live

#### Primary implementation site

`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs`

Replace/refactor:

- `ValidateAssignmentQualifiers(...)`
- `TryGetAssignmentSourceQualifiers(...)`
- `TryBuildQualifiersFromInterpolatedSlots(...)`
- `TryGetUnitSlotSourceQualifiers(...)`
- `ValidateResolvedQualifiers(...)`

#### Shared logic target

`src/Precept/Pipeline/ProofEngine.Qualifiers.cs`

This file already has a richer `ResolveQualifierFromExpression(...)` than the assignment path. The two subsystems are drifting.

That duplication is a design smell. The qualifier-resolution logic should be shared, with proof-specific comparison layered on top.

### 4.4 What the resolver must understand

#### A. Direct expressions

- `TypedFieldRef`
- `TypedArgRef`
- `TypedTypedConstant`

These are straightforward: pull from `DeclaredQualifiers`, but if the required target axis is missing because the source declaration is bare, that is **Unknown on a constrained axis**, not success.

#### B. Interpolated typed constants

Handle all slot kinds, not just units:

- `WholeValue`
- `Currency`
- `Unit`
- `FromCurrency`
- `ToCurrency`
- `NumeratorUnit`
- `DenominatorUnit`

Generalize `TryGetUnitSlotSourceQualifiers(...)` into an axis-aware slot-source resolver that can inspect:

- `TypedMemberAccess { ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier = ... } }`
- the member-access object (`TypedFieldRef` / `TypedArgRef`)
- exact qualifier or dimension facts already present on the source object

Critical detail:

- `qty.unit` sourced from `quantity of 'mass'` should resolve:
  - `Dimension` = resolved (`mass`)
  - `Unit` = unknown
- `qty.unit` sourced from bare `quantity` should resolve:
  - `Dimension` = unknown
  - `Unit` = unknown

That is the missing precision.

#### C. Unary wrappers

- `TypedUnaryOp` should transparently inherit the operand's per-axis resolution state

#### D. Binary expressions

Use operation/result metadata, not ad hoc child recursion.

Cases:

- **same-qualifier result** (`price + price`, `money + money`, etc.)
- **qualified-operand inherited** (`price * decimal`, `money / decimal`)
- **currency conversion** (`rate * money`)
- **compound cancellation / elevation** (`money / quantity`, `price / quantity`, `quantity * quantity`)

For the transformed-result cases, remove the current "return empty qualifiers and suppress validation" behavior. Unknown derived result on a required target axis must become a diagnostic.

#### E. Conditional expressions

Two acceptable strategies:

1. **Branch validation strategy** — validate `ThenBranch` and `ElseBranch` independently against the target
2. **Conditional-result strategy** — synthesize a result qualifier state only if both branches resolve compatible states

I recommend **branch validation** in the type checker. It is simpler and honest.

#### F. Function calls

For current scope this matters for money/quantity, not price, but the shared resolver should still cover it.

Read `FunctionOverload.Match` / existing qualifier-preservation metadata and treat:

- `abs(x)` as qualifier-preserving
- `round(x, places)` as qualifier-preserving
- `min/max/clamp` as requiring same compatible qualifiers across arguments and returning that qualifier

### 4.5 How validation should work once resolution is explicit

For each target qualifier in `targetQualifiers`:

1. Resolve the source axis state
2. Branch on state:

- **Resolved + incompatible**
  - `Currency` / `Unit` / `FromCurrency` / `ToCurrency` → `PRE0068 QualifierMismatch`
  - `Dimension` → `PRE0069 DimensionCategoryMismatch`
- **Resolved + compatible**
  - accept
- **Unknown + target constrains axis**
  - emit new assignment-specific unproved-qualifier diagnostic
- **Absent + target does not constrain axis**
  - ignore

The current `ValidateResolvedQualifiers(...)` cannot do this because it only sees a partial list of resolved qualifiers and treats missing source axes as silence.

That method must be replaced or radically rewritten.

### 4.6 Diagnostics

#### Keep existing diagnostics for definite failures

- `PRE0068 QualifierMismatch`
- `PRE0069 DimensionCategoryMismatch`
- `PRE0124 DimensionMismatchInUnitSlot`

#### Do not use `PRE0134`

`PRE0134 BoundsQualifierMismatch` is bounds-only. It is unrelated to assignment.

#### Do not overload `PRE0068` for uncertainty

Unknown is not mismatch.

#### Do not reuse `PRE0114` directly

`PRE0114 UnprovedQualifierCompatibility` is a proof-stage operand-pair diagnostic. Assignment enforcement is a type-checker surface and needs its own diagnostic.

#### New diagnostic required

Add a new **type-stage** error, conceptually:

- **Code name:** `UnprovedAssignmentQualifierCompatibility`
- **Message shape:** `Cannot prove the value's '{0}' qualifier satisfies field '{1}'`

Examples:

- `Cannot prove the value's 'currency' qualifier satisfies field 'Total'`
- `Cannot prove the value's 'unit' qualifier satisfies field 'Cost'`
- `Cannot prove the value's 'dimension' qualifier satisfies field 'test6'`

The exact PRE number should be assigned in `src/Precept/Language/DiagnosticCode.cs` when implemented. The important design decision is: **new code required; PRE0068 is not sufficient for unknown-source cases.**

### 4.7 Conservative vs permissive

The checker should be:

- **conservative when the target constrains an axis**
- **permissive only when the target leaves that axis unconstrained**

That is the entire model.

Examples:

| Source | Target | Result |
|--------|--------|--------|
| bare `quantity` via `.unit` | `price in '[lb_av]'` | Reject — exact unit unproved |
| `quantity of 'mass'` via `.unit` | `price of 'mass'` | Accept — dimension proved |
| `quantity of 'mass'` via `.unit` | `price in '[lb_av]'` | Reject — exact unit unproved |
| bare `money` | `money in 'USD'` | Reject — currency unproved |
| bare `money` | bare `money` | Accept |
| `money in '{CatalogCurrency}'` | `money in '{CatalogCurrency}'` | Accept — symbolic equality |

---

## § 5. Analogous Gaps

Yes. The problem is bigger than price.

### 5.1 Money

#### Current analogous gaps

- Bare `money` source into `money in 'USD'`
- Whole-value interpolation: `set tgt = '{srcMoney}'`
- Currency-slot interpolation: `set tgt = '{n} {srcMoney.currency}'`
- Conditional selection of money values
- Function-call wrappers: `abs(EurMoney)`, `round(EurMoney, 2)` assigned to `money in 'USD'`

#### Why this exists

- `MoneyValidator` is not context-aware the way `QuantityValidator` is
- `TryBuildQualifiersFromInterpolatedSlots(...)` has no currency-slot logic
- `TypedFunctionCall` is invisible to the assignment checker

### 5.2 Quantity

#### Current analogous gaps

- Bare `quantity` source into `quantity of 'mass'` or `quantity in 'kg'`
- Whole-value interpolation: `'{srcQty}'`
- Unit-slot from unconstrained source into constrained target — today explicitly conservative-accepted in the `PRE0124` path
- Conditional selection of quantities
- Function-call wrappers: `abs(Qty)`, `round(Qty, 2)`, `min/max/clamp`

#### Important nuance

Plain quantity typed constants are already stricter because `QuantityValidator.Validate(...)` receives the target's declared qualifiers. The gap is not the literal lane; it is the **expression lane** around refs, wrappers, and interpolation.

### 5.3 ExchangeRate

#### Current analogous gaps

- Bare `exchangerate` source into `exchangerate in 'USD' to 'EUR'`
- Whole-value interpolation: `'{srcRate}'`
- From/to-slot interpolation: `'{n} {src.from}/{src.to}'`
- Conditional selection of exchange rates

### 5.4 Price

Price combines all of the above and adds denominator-axis complexity:

- direct bare refs
- whole-value interpolation
- currency slots
- unit slots
- mixed static+dynamic qualifier forms
- derived result forms (`money / quantity`, `price / quantity`)

### 5.5 Conclusion on analogous gaps

The shared `TryGetAssignmentSourceQualifiers(...)` path is not a price-only bug. Price is simply where the weakness is easiest to see because price has two independent qualifier axes.

If George fixes only `price + unit-slot-from-bare-quantity`, the model will remain broken.

---

## § 6. Implementation Scope

This is what George needs to fix, in order.

### Priority 1 — Lock the semantics and add the right diagnostic

**Files:**

- `src/Precept/Language/DiagnosticCode.cs`
- `src/Precept/Language/Diagnostics.cs`

**Work:**

1. Keep `PRE0068` for definite mismatch only
2. Add a new type-stage diagnostic for "required assignment qualifier axis is unproved"
3. Do **not** reuse `PRE0134`
4. Do **not** overload `PRE0068`

### Priority 2 — Replace the assignment extraction seam

**Primary file:**

- `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs`

**Work:**

1. Replace `TryGetAssignmentSourceQualifiers(...)` with an axis-aware resolver that can represent `Resolved` / `Unknown` / `Absent`
2. Rewrite `ValidateResolvedQualifiers(...)` accordingly
3. Remove the current "empty qualifier array means success" behavior
4. Stop returning `true` with `qualifiers = []` in the `CurrencyConversionRequired` / `CompoundDimensionElevationRequired` unknown-result paths

### Priority 3 — Cover every price source form

**Files:**

- `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs`
- possibly shared helper extracted alongside `src/Precept/Pipeline/ProofEngine.Qualifiers.cs`

**Work:**

1. Direct refs, including bare-source rejection
2. Whole-value interpolation
3. Unit-slot interpolation
4. Currency-slot interpolation
5. Mixed currency+unit interpolation
6. Conditional selection
7. Derived-result binary expressions

### Priority 4 — Unify type-checker and proof-engine qualifier resolution

**Files:**

- `src/Precept/Pipeline/ProofEngine.Qualifiers.cs`
- shared pipeline helper extracted under `src/Precept/Pipeline/`

**Work:**

1. Remove the drift between assignment-time and proof-time qualifier resolution
2. Share the slot/member-access/typed-constant/direct-ref logic
3. Extend both surfaces consistently when adding new expression forms

### Priority 5 — Extend the same fix to money / quantity / exchangerate

**Files:**

- `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs`
- `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs`
- tests listed below

**Work:**

1. Money currency-slot and whole-value gaps
2. Quantity whole-value / bare-source / function-result gaps
3. Exchange-rate from/to-slot and whole-value gaps
4. Function-call qualifier-preserving result handling for money/quantity

### Priority 6 — Regression matrix

**Primary test file:**

- `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs`

**Additional files as needed:**

- `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs`
- `test/Precept.Tests/ProofEngineTypedArgQualifierTests.cs` (only if shared resolver changes proof behavior)

**Required new test groups:**

1. **Price — direct bare-source rejection**
   - `SetQualifiedPriceField_FromBarePriceField_EmitsUnprovedAssignmentQualifierCompatibility`
2. **Price — whole-value interpolation**
   - `SetQualifiedPriceField_FromWholeValueInterpolatedMismatchedPrice_EmitsQualifierMismatch`
3. **Price — conditional selection**
   - `SetQualifiedPriceField_FromConditionalMismatchedBranches_EmitsQualifierMismatch`
4. **Price — dimension-only source vs exact-unit target**
   - `SetExactUnitPriceField_FromDimensionOnlyUnitSlot_EmitsUnprovedAssignmentQualifierCompatibility`
5. **Price — bare quantity source vs dimension target**
   - `SetDimensionQualifiedPriceField_FromBareQuantityUnitSlot_EmitsUnprovedAssignmentQualifierCompatibility`
6. **Money — currency slot from known source**
   - `SetUsdMoneyField_FromInterpolatedCurrencySlotEurSource_EmitsQualifierMismatch`
7. **Money — bare source**
   - `SetUsdMoneyField_FromBareMoneyField_EmitsUnprovedAssignmentQualifierCompatibility`
8. **ExchangeRate — from/to slots**
   - `SetUsdEurRate_FromInterpolatedFromToSlots_GbpEurSource_EmitsQualifierMismatch`
9. **Quantity — whole-value interpolation / bare source / function wrappers**
10. **Shared surfaces**
    - set actions
    - field defaults
    - computed fields
    - event-arg defaults
    - min/max assignment-style validation where applicable

### Final priority judgment

If George does only this:

- patch `TryBuildQualifiersFromInterpolatedSlots(...)` for bare `test3.unit`

then he will fix one symptom and preserve the broken model.

If he does this instead:

- replace the boolean/partial-array assignment qualifier seam with explicit per-axis resolution
- add the missing unproved-assignment diagnostic
- close the wrapper + slot + bare-source forms across all qualified types

then the entire qualifier assignment model becomes coherent.

That is the work.
