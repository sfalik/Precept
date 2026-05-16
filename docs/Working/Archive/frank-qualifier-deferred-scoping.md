# Qualifier Deferred Items — Implementation Scoping

**By:** Frank  
**Date:** 2026-05-15T18:09:58-04:00  
**Status:** Scoping spec — governs implementation of three items deferred from PRE0141 enforcement work  
**Prerequisite:** Assignment qualifier enforcement (PRE0141) is shipped and Frank-approved.

---

## Item 1: ProofEngine.Qualifiers.cs Unification (W2)

### Diagnosis

Two independent qualifier-resolution subsystems exist:

1. **TypeChecker path** — `TypeChecker.Expressions.AssignmentQualifiers.cs`, method `ResolveAssignmentQualifierAxis(TypedExpression, QualifierAxis) → ResolvedQualifierAxis`. Returns the tri-state `Resolved/Unknown/Absent` model via `QualifierResolutionKind`. This is the new axis-aware resolver that shipped with PRE0141.

2. **ProofEngine path** — `ProofEngine.Qualifiers.cs`, method `ResolveQualifierFromExpression(TypedExpression, QualifierAxis, SemanticIndex) → DeclaredQualifierMeta?`. Returns nullable — `null` means "cannot resolve," which conflates Unknown and Absent. This is the pre-existing resolver used by Strategy 5 (qualifier compatibility proofs for binary operations, PRE0114).

**Where they agree:**
- Both handle `TypedFieldRef`, `TypedArgRef`, `TypedTypedConstant`, `InterpolatedTypedConstant`, `TypedBinaryOp` with `ResultQualifier` subtypes.
- Both follow the same axis-fallback chains (`Unit→Dimension`, `Dimension→TemporalDimension`, `CompoundPrice→Currency/Unit/Dimension` projection).
- Both handle the same `ResultQualifier` DU subtypes: `SameQualifierRequired`, `QualifiedOperandInherited`, `CurrencyConversionRequired`, `CompoundUnitCancellationRequired`, `CompoundDimensionElevationRequired`.

**Where they drift:**

| Concern | TypeChecker path | ProofEngine path |
|---------|------------------|------------------|
| Return type | `ResolvedQualifierAxis` (tri-state + qualifier) | `DeclaredQualifierMeta?` (nullable, no Unknown/Absent distinction) |
| Field resolution | Via `TypedFieldRef.DeclaredQualifiers` directly | Via `SemanticIndex.FieldsByName` lookup + implied qualifiers from `Types.GetMeta(...)` |
| Implied qualifiers | Not consulted | Consulted (line 350–356: `typeMeta.ImpliedQualifiers`) |
| Function calls | Not handled — falls through to `Absent` | Not handled — falls through to `null` |
| `TranslateCurrencyAxis` | Returns `ResolvedQualifierAxis` with ToCurrency→Currency translation built into `ResolveCurrencyConversionAxis` | Has separate `TranslateCurrencyAxis(DeclaredQualifierMeta?)` helper |
| Compound cancellation | Uses `TryMatchCompoundUnitCancellation` with full structural matching | Uses `TryResolveCompoundCancellationUnit` with string-based compound value extraction |
| Interpolated constants | Delegates to `ResolveInterpolatedQualifierAxis` with slot-kind routing and `StaticQualifier` awareness | Delegates to `ResolveQualifierFromInterpolatedConstant` with parallel but structurally different slot logic |

**The correctness risk:**

The ProofEngine path does NOT distinguish "source has no qualifier on this axis because it's unconstrained" from "source cannot be resolved." Both are `null`. This means the ProofEngine's `QualifiersAreCompatible` method treats *both* as proof failure (returns `false`). That is currently safe for proof obligations — failing to prove compatibility means the proof obligation stays unproven, which is conservative. But:

1. If new proof strategies are added that depend on knowing whether a qualifier is genuinely absent (irrelevant axis) versus unknown (underspecified source), the conflation will cause false negatives.
2. The implied-qualifier lookup in ProofEngine (lines 350–356) exists there but NOT in the TypeChecker path. This means `duration`'s implied `TemporalDimension(Time)` is visible to proof obligations but invisible to assignment validation. This is a **latent correctness divergence** — it doesn't bite today because no assignment target constrains `TemporalDimension` on duration fields, but it will if temporal constraints become assignable.
3. The compound-cancellation logic uses different structural approaches: TypeChecker does full `TryMatchCompoundUnitCancellation` with UCUM parsing; ProofEngine does string-based `ExtractCompoundValue` + slash-splitting. These can disagree on edge cases.

### Spec

**Scope decision: PARTIAL ALIGNMENT — full unification is premature.**

Full unification would mean making both subsystems call the same resolver. That requires either:
- (a) Making `ResolveAssignmentQualifierAxis` take `SemanticIndex` (currently it doesn't need it because `TypedFieldRef.DeclaredQualifiers` is already populated), or
- (b) Stripping `SemanticIndex` dependency from the ProofEngine path (would lose implied-qualifier lookup and field-by-name resolution, which are needed for proof subjects that resolve through `ResolveFieldQualifier`).

Neither is free, and both subsystems serve different contracts: assignment validation needs the tri-state to emit different diagnostics; proof validation needs binary-operand comparison with symbolic equality. The comparison logic (`QualifiersAreCompatible`, `QualifiersSymbolicallyEqual`, `ChainQualifiersMatch`) is proof-specific and has no assignment analog.

**What MUST be aligned now (preventing correctness divergence):**

1. **Implied-qualifier parity.** Add implied-qualifier lookup to `ResolveDirectQualifierAxis` in `TypeChecker.Expressions.AssignmentQualifiers.cs`. After checking `DeclaredQualifiers`, if no qualifier found on the requested axis and the result type carries implied qualifiers via `Types.GetMeta(resultType).ImpliedQualifiers`, check those. Return `Resolved` if found, fall through to existing `Unknown/Absent` logic if not.

   - **File:** `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs`
   - **Method:** `ResolveDirectQualifierAxis`
   - **Change:** After line 160 (the `return IsAssignmentQualifierAxisApplicable...` fallback), insert implied-qualifier check:
     ```csharp
     var typeMeta = Types.GetMeta(resultType);
     foreach (var implied in typeMeta.ImpliedQualifiers)
     {
         if (ProjectQualifierForAxis(implied, axis) is { } projected)
             return new(axis, QualifierResolutionKind.Resolved, projected);
     }
     ```
   - This goes *between* the declared-qualifier check and the `Unknown/Absent` fallback.

2. **Compound cancellation alignment.** The TypeChecker's `TryMatchCompoundUnitCancellation` uses UCUM-aware `TrySplitCompoundUnit` and `TryDeriveUnitDimensionName`. The ProofEngine's `TryResolveCompoundCancellationUnit` uses raw string-splitting via `ExtractCompoundValue`. Make ProofEngine call `TryMatchCompoundUnitCancellation` (it's already in the same compilation unit via the `TypeChecker` partial class — ProofEngine would need to either call it directly or duplicate).

   - **File:** `src/Precept/Pipeline/ProofEngine.Qualifiers.cs`
   - **Method:** `TryResolveCompoundCancellationUnit`
   - **Change:** Replace the string-split extraction with a call to the TypeChecker's UCUM-aware helper. Since `TryMatchCompoundUnitCancellation` is `private static` on `TypeChecker`, expose it as `internal static` or extract the compound-unit split logic into a shared helper in `UnitDimensionHelper`.
   - **Preferred approach:** Add `UnitDimensionHelper.TrySplitCompoundUnit(string unitCode, out string numerator, out string denominator)` as a public static method (if not already there) and use it in both sites.

3. **Do NOT unify the resolver entry points.** `ResolveAssignmentQualifierAxis` and `ResolveQualifierFromExpression` remain separate methods with separate return types. The proof path's nullable return is correct for its consumers; forcing tri-state onto proof consumers would add complexity with no benefit.

4. **Do NOT add `TypedFunctionCall` handling to ProofEngine.Qualifiers.cs in this item.** That's Item 3's scope. The ProofEngine doesn't generate function-call proof obligations for qualifier compatibility today.

### Tests Required

1. **Implied-qualifier assignment test:** `field dur as duration`, `set dur = otherDuration` where `otherDuration` is bare `duration` — verify no false PRE0141 on `TemporalDimension` axis if/when temporal constraints become assignable. For now: verify the resolver returns `Resolved` with `TemporalDimension(Time)` for a bare `duration` field ref.
2. **Compound-cancellation consistency test:** `quantity of 'length/time'` divided by `quantity of 'time'` — verify both TypeChecker and ProofEngine resolve the result dimension to `length`.
3. **Negative: No behavior regression on existing PRE0114 proof tests.** Run the full `ProofEngineTests` suite as regression anchor.

### Scope Decision

**PARTIAL** — Align implied qualifiers and compound-cancellation structural helpers. Full entry-point unification is deferred because the two subsystems serve genuinely different contracts (assignment diagnostics vs. proof disposition) and full unification would require either breaking the TypeChecker's `SemanticIndex`-free design or stripping the ProofEngine's implied-qualifier/field-lookup capability.

---

## Item 2: Quantity Type Gaps

### Diagnosis

The assignment qualifier enforcement (PRE0141) explicitly short-circuits quantity typed constants at line 31–32 of `ValidateAssignmentQualifiers`:

```csharp
if (value is TypedTypedConstant { ResultType: TypeKind.Quantity })
    return;
```

This bypass exists because plain quantity literals are validated by `QuantityValidator.Validate(...)`, which is context-aware: it receives `TypedConstantContext.DeclaredQualifiers` and checks dimension/unit compatibility at parse time. That literal-lane coverage is correct and should stay.

**But the bypass also means that quantity expressions OTHER than plain literals — refs, interpolation, binary expressions, function calls, conditionals — are processed by the shared resolver.** And the shared resolver DOES handle quantity. The short-circuit only affects `TypedTypedConstant` with `ResultType == Quantity`.

So the question is: which quantity expression forms actually have gaps?

#### Gap A: Bare `quantity` ref into constrained target

**Input:** `field src as quantity` (no qualifier), `field tgt as quantity of 'mass'`, `set tgt = src`  
**Current behavior:** `ResolveAssignmentQualifierAxis` on `TypedFieldRef` → `ResolveDirectQualifierAxis` → DeclaredQualifiers is empty → `IsAssignmentQualifierAxisApplicable(Quantity, Dimension)` returns `true` → returns `Unknown`. `ValidateResolvedQualifierAxes` emits `PRE0141 UnprovedAssignmentQualifierCompatibility` on the Dimension axis.  
**Verdict: ALREADY HANDLED by the shipped resolver.** PRE0141 fires correctly.

Same for `quantity in 'kg'` targets: bare source → `Unknown` on Unit axis → PRE0141.

#### Gap B: Whole-value interpolation

**Input:** `field src as quantity in 'g'`, `field tgt as quantity in 'kg'`, `set tgt = '{src}'`  
**Current behavior:** `ResolveInterpolatedQualifierAxis` → detects `WholeValue` slot → delegates to `ResolveAssignmentQualifierAxis(slot.Expression, axis)` → resolves through the `TypedFieldRef` path → gets `Unit("g", "mass")` → `Resolved`. `ValidateResolvedQualifierAxes` checks `ResolvedQualifierSatisfiesTarget(Unit("g","mass"), Unit("kg","mass"))` → **this compares unit codes `g` vs `kg`** → mismatch → `PRE0068`.  
**Verdict: ALREADY HANDLED.** Whole-value propagation was implemented as part of the PRE0141 work.

For bare `quantity` whole-value: `'{bareQty}'` → WholeValue slot → `ResolveAssignmentQualifierAxis` on bare field → `Unknown` → PRE0141. Also handled.

#### Gap C: Unit slots in interpolation

**Input:** `field tgt as quantity in 'kg'`, `field src as quantity` (bare), `set tgt = '{42} {src.unit}'`  
**Current behavior:** `ResolveInterpolatedQualifierAxis` → no WholeValue → `axis == Unit` → `ResolveInterpolatedSlotAxis(interpolated, InterpolationSlotKind.Unit, axis)` → `ResolveSlotSourceQualifierAxis` → checks `TypedMemberAccess.ResolvedAccessor` is `FixedReturnAccessor` with `ReturnsQualifier` → resolves source field's qualifier on Unit axis → bare source → no declared qualifier → returns `Absent`.  
**BUT:** The `SlotAccessorCanResolveAxis` check and `ResolveSlotSourceQualifierAxis` logic returns `Absent` when the source has no qualifier, not `Unknown`. This means the assignment validation sees `Absent` on the Unit axis and **skips it** — the target's Unit constraint is silently unsatisfied.

**Wait.** Let me re-read. `ValidateResolvedQualifierAxes` iterates over `targetQualifiers` and finds the resolution for each. If `resolution.Kind == Absent`, there is no case arm for it — it falls through the switch silently. So `Absent` on a constrained axis is indeed silent.

**HOWEVER:** The `Absent` path is correct semantically when the source expression genuinely cannot carry qualifiers on that axis (e.g., an integer assigned to a quantity field — that's a type error caught elsewhere). For interpolated slots where the source IS a quantity-typed field but has no declared qualifiers, returning `Absent` is **wrong** — that should be `Unknown`, because the source type CAN carry the qualifier but doesn't declare it.

**This is a real gap.** The `ResolveSlotSourceQualifierAxis` method returns `Absent` instead of routing through `IsAssignmentQualifierAxisApplicable` to distinguish `Unknown` from `Absent`.

**Input 2:** `field tgt as quantity of 'mass'`, `field src as quantity of 'length'`, `set tgt = '{42} {src.unit}'`  
**Current behavior:** Source has `Dimension("length")`, target requires `Dimension("mass")`. The slot resolver resolves `Dimension("length")` → `Resolved`. The target's Dimension qualifier comparison fires → `ResolvedQualifierSatisfiesTarget(Dimension("length"), Dimension("mass"))` → mismatch → `PRE0068` (or `PRE0069`).  
**Verdict: Known mismatch is handled.**

**Summary for C:** Known qualifier → handled. Bare source (no qualifier but type-applicable) → **GAP**. Returns `Absent` instead of `Unknown`.

#### Gap D: Binary-result paths

**Input:** `field tgt as quantity of 'mass'`, `field a as quantity`, `set tgt = a * 2`  
**Current behavior:** `TypedBinaryOp` with `ResultQualifier = QualifiedOperandInherited`. `ResolveBinaryQualifierAxis` → `ResolveAssignmentQualifierAxis` on `a` → `Unknown` (bare quantity on Dimension axis). Returns `Unknown` to the top-level validator → PRE0141.  
**Verdict: HANDLED.**

**Input 2:** `quantity / quantity → number` — no qualifier on result. Not applicable.  
**Verdict: N/A — quantity division produces number, not quantity.**

#### Gap E: Conditional branches

**Input:** `field tgt as quantity of 'mass'`, `field a as quantity of 'mass'`, `field b as quantity of 'length'`, `set tgt = if flag then a else b`  
**Current behavior:** `ValidateAssignmentQualifiers` detects `TypedConditional` at line 34 → recurses into both branches independently → each branch is validated against `targetQualifiers` → `a` resolves `Dimension("mass")` → passes; `b` resolves `Dimension("length")` → mismatch → `PRE0068` or `PRE0069`.  
**Verdict: HANDLED.**

**Input 2:** `set tgt = if flag then bareQty else qualifiedQty` → bare branch → `Unknown` → PRE0141 on that branch.  
**Verdict: HANDLED.**

### Spec

**Only one real gap: interpolation unit-slot resolution returns `Absent` instead of `Unknown` for bare-but-type-applicable sources.**

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs`  
**Method:** `ResolveSlotSourceQualifierAxis`

**Current logic (lines 278–300):**
```csharp
private static ResolvedQualifierAxis ResolveSlotSourceQualifierAxis(
    TypedExpression holeExpression,
    QualifierAxis axis,
    out string sourceName)
{
    sourceName = GetQualifierSourceName(holeExpression);

    if (holeExpression is not TypedMemberAccess { ... })
    {
        return new(axis, QualifierResolutionKind.Absent, null);  // ← HERE
    }

    return source switch
    {
        TypedFieldRef fieldRef => ResolveDirectQualifierAxis(fieldRef.ResultType, fieldRef.DeclaredQualifiers, axis),
        TypedArgRef argRef => ResolveDirectQualifierAxis(argRef.ResultType, argRef.DeclaredQualifiers, axis),
        _ => new(axis, QualifierResolutionKind.Absent, null),  // ← AND HERE
    };
}
```

**Fix:** The early-return `Absent` for non-MemberAccess expressions is correct (non-accessor slots can't carry qualifiers). BUT: the `source switch` default arm `_ => Absent` should distinguish whether the source type could carry qualifiers on this axis. More importantly, the `ResolveDirectQualifierAxis` call already handles this correctly — when the source field has no declared qualifier but the type supports the axis, it returns `Unknown`. So the `FieldRef` and `ArgRef` arms are actually correct.

**The real gap** is the early-return when `holeExpression is not TypedMemberAccess` OR when `SlotAccessorCanResolveAxis` returns false. In those cases, if the slot expression's result type COULD carry qualifiers on this axis, returning `Absent` is wrong.

**Change:**
```csharp
if (holeExpression is not TypedMemberAccess
    {
        Object: var source,
        ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: var returnsQualifier }
    }
    || !SlotAccessorCanResolveAxis(returnsQualifier, axis))
{
    // If the hole expression's type could carry qualifiers on this axis,
    // return Unknown (not Absent) — the qualifier is applicable but unresolved.
    var holeType = holeExpression switch
    {
        TypedMemberAccess ma => ma.Object switch
        {
            TypedFieldRef fr => fr.ResultType,
            TypedArgRef ar => ar.ResultType,
            _ => holeExpression.ResultType,
        },
        _ => holeExpression.ResultType,
    };
    return IsAssignmentQualifierAxisApplicable(holeType, axis)
        ? new(axis, QualifierResolutionKind.Unknown, null)
        : new(axis, QualifierResolutionKind.Absent, null);
}
```

**Additionally:** The `QuantityValidator.Validate(...)` in `src/Precept/Language/QuantityValidator.cs` is NOT affected by this fix. It handles plain typed constants only and its dimension/unit checking is correct for that scope. The `ValidateAssignmentQualifiers` short-circuit for `TypedTypedConstant { ResultType: Quantity }` should STAY — `QuantityValidator` is the right handler for literal validation.

**No new diagnostic code needed.** PRE0141 (`UnprovedAssignmentQualifierCompatibility`) is the correct diagnostic for unknown-source-into-constrained-target quantity assignments. No quantity-specific code is warranted.

### Tests Required

1. **Gap C regression test:** `field tgt as quantity of 'mass'`, `field src as quantity` (bare), `set tgt = '{42} {src.unit}'` — must emit PRE0141 on dimension axis. Currently silently accepted.
2. **Gap C exact-unit test:** `field tgt as quantity in 'kg'`, `field src as quantity` (bare), `set tgt = '{42} {src.unit}'` — must emit PRE0141 on unit axis.
3. **Gap C known-mismatch anchor:** `field tgt as quantity of 'mass'`, `field src as quantity of 'length'`, `set tgt = '{42} {src.unit}'` — must emit PRE0069 or PRE0068 (dimension mismatch). Verify this still works after the fix.
4. **Negative regression:** `field tgt as quantity of 'mass'`, `field src as quantity of 'mass'`, `set tgt = '{42} {src.unit}'` — must compile clean. Verify no false positive.
5. **All existing `TypeCheckerExpressionTests` and `ProofEngineTests` as regression anchors.**

### Scope Decision

**FULL** — One surgical fix in `ResolveSlotSourceQualifierAxis`. No design review gate needed. The semantic model is already established (PRE0141 semantics); this is a coverage extension to a form that was missed.

---

## Item 3: Function-Call Qualifier Preservation (W1)

### Diagnosis

`TypedFunctionCall` is invisible to both qualifier resolvers:

1. **TypeChecker assignment path:** `ResolveAssignmentQualifierAxis` has no `case TypedFunctionCall` arm. It falls through to the `default` arm (line 142–143) and returns `Absent`. This means `set moneyField = abs(otherMoney)` silently passes all qualifier checks even when `moneyField` constrains currency.

2. **ProofEngine path:** `ResolveQualifierFromExpression` has no `case TypedFunctionCall` arm. Falls through to `default` (line 450–451) and returns `null`. This means qualifier compatibility proofs for binary expressions involving function-call subexpressions fail to prove — which is conservative but unnecessarily so.

3. **TypedFunctionCall record** (SemanticIndex.cs, line 65–71) carries `FunctionKind` and `Arguments` but does NOT carry qualifier information. There is no `DeclaredQualifiers` property and no `QualifierMatch` indicator on the typed node.

4. **The catalog already declares qualifier intent.** `FunctionOverload.Match` is `QualifierMatch.Same` on `min/max/clamp/abs/round(value,places)` overloads for `Money` and `Quantity` types. `ValidateFunctionQualifierCompatibility` in `TypeChecker.Expressions.Callables.cs` (line 705–718) already reads this metadata and emits `PRE0068` when arguments have known mismatched qualifiers. But this validation is **input-facing only** — it validates that arguments agree. It does NOT propagate the resolved qualifier onto the function call result for downstream consumption.

**Functions that preserve qualifiers (output = input qualifier):**

| Function | Money overload | Quantity overload | Qualifier behavior |
|----------|---------------|-------------------|-------------------|
| `abs` | ✓ Match=Same | ✓ Match=Same | Output qualifier = input qualifier |
| `min` | ✓ Match=Same | ✓ Match=Same | Output qualifier = shared input qualifier |
| `max` | ✓ Match=Same | ✓ Match=Same | Output qualifier = shared input qualifier |
| `clamp` | ✓ Match=Same | ✓ Match=Same | Output qualifier = shared input qualifier (value arg) |
| `round(v,p)` | ✓ Match=Same | ✓ Match=Same | Output qualifier = value arg qualifier |

**Functions that do NOT preserve qualifiers (type-changing):**

| Function | Qualifier behavior |
|----------|-------------------|
| `floor/ceil/truncate/round(v)` | decimal/number → integer — no qualified types |
| `approximate` | decimal → number — no qualified types |
| `sqrt` | number → number — no qualified types |
| `pow` | number, integer → number — no qualified types |
| `len/contains/starts_with/ends_with/trim/upper/lower/substring/replace` | String functions — no qualified types |
| `days/hours/minutes/seconds` | Temporal functions — no qualified types |
| `year/month/day_of_month` etc. | Temporal functions — no qualified types |

**Conclusion:** Only overloads with `QualifierMatch.Same` are qualifier-preserving, and these are exclusively the `Money` and `Quantity` overloads of `abs`, `min`, `max`, `clamp`, and `round(v,p)`.

### Spec

**Two changes required:**

#### Change 1: Add qualifier propagation to `TypedFunctionCall`

**File:** `src/Precept/Pipeline/SemanticIndex.cs`  
**Record:** `TypedFunctionCall`

Add a `DeclaredQualifiers` property:

```csharp
public sealed record TypedFunctionCall(
    TypeKind ResultType,
    FunctionKind ResolvedFunction,
    ImmutableArray<TypedExpression> Arguments,
    ImmutableArray<ProofRequirement> ProofRequirements,
    ImmutableArray<DeclaredQualifierMeta>? ResultQualifiers,
    SourceSpan Span
) : TypedExpression(ResultType, Span);
```

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs`  
**Method:** `CreateTypedFunctionCall`

Populate `ResultQualifiers` based on `overload.Match`:

```csharp
private static TypedExpression CreateTypedFunctionCall(
    FunctionKind kind,
    FunctionOverload overload,
    ImmutableArray<TypedExpression> args,
    SourceSpan span,
    CheckContext ctx)
{
    ValidateFunctionQualifierCompatibility(overload, args, span, ctx);

    ImmutableArray<DeclaredQualifierMeta>? resultQualifiers = null;
    if (overload.Match == QualifierMatch.Same && args.Length > 0)
    {
        // Qualifier-preserving: inherit from first argument
        resultQualifiers = TryGetStaticQualifiers(args[0]);
    }

    return new TypedFunctionCall(
        overload.ReturnType,
        kind,
        args,
        overload.ProofRequirements.ToImmutableArray(),
        resultQualifiers,
        span);
}
```

`TryGetStaticQualifiers` already exists in the same file — it extracts `DeclaredQualifiers` from field refs, arg refs, typed constants, and interpolated constants. This is the correct source for propagation.

#### Change 2: Add `TypedFunctionCall` handling to both resolvers

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs`  
**Method:** `ResolveAssignmentQualifierAxis`

Add case arm:

```csharp
case TypedFunctionCall functionCall:
    return ResolveDirectQualifierAxis(
        functionCall.ResultType,
        functionCall.ResultQualifiers,
        axis);
```

This correctly:
- Returns `Resolved` with the qualifier when the first arg has known qualifiers
- Returns `Unknown` when the first arg is a bare qualified type (via `IsAssignmentQualifierAxisApplicable`)
- Returns `Absent` when the result type doesn't support qualifiers on this axis

**File:** `src/Precept/Pipeline/ProofEngine.Qualifiers.cs`  
**Method:** `ResolveQualifierFromExpression`

Add case arm before `default`:

```csharp
case TypedFunctionCall { ResultQualifiers: { IsDefaultOrEmpty: false } funcQuals }:
    foreach (var q in funcQuals)
        if (q.Axis == axis) return q;
    if (axis == QualifierAxis.Unit)
        foreach (var q in funcQuals)
            if (q.Axis == QualifierAxis.Dimension) return q;
    if (axis == QualifierAxis.Dimension)
        foreach (var q in funcQuals)
            if (q.Axis == QualifierAxis.TemporalDimension) return q;
    foreach (var q in funcQuals)
    {
        var projected = TryProjectCompoundPrice(q, axis);
        if (projected is not null) return projected;
    }
    return null;
```

This mirrors the existing `TypedArgRef` and `TypedTypedConstant` patterns already in the method.

#### No new catalog metadata needed

`FunctionOverload.Match` already carries the qualifier-preservation intent. Adding a separate `QualifierPreservation` field to `FunctionMeta` would be redundant — `Match == QualifierMatch.Same` IS the declaration. The resolver reads `Match` at `CreateTypedFunctionCall` time and propagates the result. No catalog change.

#### No new `FunctionMeta` property needed

The existing `QualifierMatch` enum on `FunctionOverload` is sufficient. The `Same` variant already means "all qualified-type arguments must share qualifiers and the result inherits them." No ambiguity.

### Tests Required

1. **Known-qualifier preservation:** `field tgt as money in 'USD'`, `field src as money in 'USD'`, `set tgt = abs(src)` — must compile clean.
2. **Known mismatch through function:** `field tgt as money in 'USD'`, `field src as money in 'EUR'`, `set tgt = abs(src)` — must emit PRE0068 on currency axis.
3. **Unknown qualifier through function:** `field tgt as money in 'USD'`, `field src as money` (bare), `set tgt = abs(src)` — must emit PRE0141 on currency axis.
4. **Quantity preservation:** `field tgt as quantity of 'mass'`, `field src as quantity of 'mass'`, `set tgt = abs(src)` — must compile clean.
5. **Quantity mismatch:** `field tgt as quantity of 'mass'`, `field src as quantity of 'length'`, `set tgt = abs(src)` — must emit PRE0069 or PRE0068.
6. **Multi-arg qualifier preservation:** `field tgt as money in 'USD'`, `field a as money in 'USD'`, `field b as money in 'USD'`, `set tgt = min(a, b)` — must compile clean.
7. **Multi-arg mismatch:** `set tgt = min(usdMoney, eurMoney)` into `money in 'USD'` — existing `ValidateFunctionQualifierCompatibility` should catch the argument mismatch; verify the result also doesn't silently pass assignment validation.
8. **round(money, places) preservation:** `set tgt = round(src, 2)` where `src as money in 'USD'`, `tgt as money in 'USD'` — must compile clean.
9. **Non-qualifier function not affected:** `set intField = floor(decField)` — no qualifier propagation expected, no diagnostic.
10. **ProofEngine regression:** All existing `ProofEngineTests` must pass. Function calls in binary expressions (`abs(qty) + qty`) should now have qualifier data available for proof obligations.

### Scope Decision

**FULL** — No design review gate needed. The semantic model is established: `QualifierMatch.Same` is already the catalog declaration; `ValidateFunctionQualifierCompatibility` already enforces input agreement. This work completes the output side of the contract by propagating the resolved qualifier through `TypedFunctionCall.ResultQualifiers` and teaching both resolvers to read it. The catalog metadata is already correct. The change is mechanical.

---

## Implementation Order

1. **Item 3 (Function-Call)** — Self-contained. Changes `TypedFunctionCall`, `CreateTypedFunctionCall`, both resolvers. No dependencies on Items 1 or 2.
2. **Item 2 (Quantity Gaps)** — Self-contained. One fix in `ResolveSlotSourceQualifierAxis`. No dependencies.
3. **Item 1 (ProofEngine Alignment)** — Partial alignment. Implied-qualifier parity and compound-cancellation helper extraction. Can be done independently but is lowest priority because the correctness risk is latent, not active.

Items 1 and 2 have no ordering dependency on each other. Item 3 has no dependency on either. All three can be parallelized if desired.
