# Quantity Normalization Design — Cross-Phase Unit-Aware Comparison

**Author:** Frank (Lead Architect)
**Status:** Draft — George's technical review incorporated (APPROVED WITH CONDITIONS, all conditions accepted/addressed), pending Shane sign-off
**Date:** 2026-05-14 (George review incorporated: 2026-05-14)
**Scope:** Unit-aware normalization for compile-time (TypeChecker/ProofEngine) and runtime (Evaluator/PreceptValue) numeric comparisons on quantity, money, and price types
**Upstream:** `docs/Working/interval-proof-engine-design.md` (interval arithmetic machinery — shipped)
**Review status:** George's per-slice technical review (2026-05-14) — all findings accepted or accepted-with-modification. See §0.7 for disposition summary.

---

## Table of Contents

0. [Architectural Reassessment](#0-architectural-reassessment)
0.1. [Catalog Integration for Quantity Normalization](#01-catalog-integration-for-quantity-normalization)
0.2. [Two-Layer Value Architecture — PreceptValue as Runtime Currency](#02-two-layer-value-architecture--preceptvalue-as-runtime-currency)
0.3. [Code Sharing Between Compiler and Runtime](#03-code-sharing-between-compiler-and-runtime)
0.4. [Runtime Arg Normalization — The Intake Boundary](#04-runtime-arg-normalization--the-intake-boundary)
0.5. [Diagnostic Enforcement Alignment — Compiler/Runtime Duplication Assessment](#05-diagnostic-enforcement-alignment--compilerruntime-duplication-assessment)
0.6. [Design Resolution Summary](#06-design-resolution-summary)
1. [Problem Statement](#1-problem-statement)
2. [PreceptValue Integration Analysis](#2-preceptvalue-integration-analysis)
3. [Proposed Architecture](#3-proposed-architecture)
4. [UCUM Scale Table](#4-ucum-scale-table)
5. [Migration Path](#5-migration-path)
5.6. [Extended Slice Details — Slices 22–26](#56-extended-slice-details--slices-2226)
6. [Risks and Tradeoffs](#6-risks-and-tradeoffs)
7. [Open Questions for Shane](#7-open-questions-for-shane)

---

## §0. Architectural Reassessment

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Shane's deep-rethink request — 6 questions on pipeline placement, shared representations, and optimal seams

---

### Verdict on the Current Approach

The current design (Slices 14–21) is **directionally correct** but architecturally suboptimal in one important dimension: it normalizes in two consumer sites (TypeChecker + ProofEngine) and stores raw magnitudes on `TypedField.DeclaredMin/Max`, forcing every downstream consumer to independently normalize before comparison. This is the "normalize at every comparison" pattern the design claims to avoid in §3.1 but actually implements in §3.7.

The fix is not radical — it's a seam adjustment. The correct architecture normalizes **once**, at extraction time in the TypeChecker, and stores the normalized value alongside the original on `TypedField`. The ProofEngine, Builder, and any future consumer then read pre-normalized bounds without re-deriving them.

The design's overall shape — a `TypedConstantNormalizer` utility consuming existing UCUM infrastructure, money excluded, normalization as a comparison concern not a storage concern — is sound. What changes is *where* the normalization result lives and *how many times* it executes.

---

### Q1: Compile-on-Every-Keystroke Performance Impact

**Analysis:** The full pipeline (Lexer → Parser → NameBinder → TypeChecker → GraphAnalyzer → ProofEngine) runs on every debounced keystroke in the language server. The current design normalizes in:
1. `TypeChecker.Validation.Modifiers.cs` — once per `min`/`max` modifier per field
2. `ProofEngine.Composition.cs` — once per typed-constant expression in `IntervalOf`
3. `ProofEngine.Intervals.cs` — once per `ITuple` unwrap in `TryExtractNumericLiteralMagnitude`

For a typical precept with ~5 quantity fields and ~10 assignment expressions, this means ~15 UCUM factor applications per keystroke. Each `ApplyFactor` is 2–3 decimal multiplications — trivially cheap (nanoseconds). **Performance is not a concern.**

However, the **redundancy** is a concern for correctness maintenance. If the TypeChecker normalizes bounds but the ProofEngine also normalizes the same typed-constant when it appears in an assignment expression, the same conversion runs twice on the same parsed value. This isn't a performance problem — it's a **single-responsibility problem**. The question is: who owns the canonical normalized magnitude?

**Recommendation:** Normalize once per parsed typed-constant, cache on the semantic model. Specifically:
- The TypeChecker already calls `TryGetComparableTypedConstantValue` for bound modifiers. Extend it to store **both** `DeclaredMax` (original) and `NormalizedDeclaredMax` (normalized) on `TypedField`.
- The ProofEngine reads `NormalizedDeclaredMin/Max` directly — no re-normalization.
- For assignment-side typed constants (the `IntervalOf` path), the ProofEngine still needs to normalize the assigned expression's magnitude. This is unavoidable — the assignment's parsed value is only available at proof time. But this is a single call per expression, not redundant work.

**Net:** Two normalization sites remain (TypeChecker for bounds, ProofEngine for assignment expressions), but each normalizes exactly once per value. No caching on `SemanticIndex` beyond what `TypedField` already carries. This is the minimum viable approach.

---

### Q2: Should the Builder Be in the Compile Pipeline?

**No.** The Builder belongs in the runtime boundary, not the analysis pipeline. Here's why:

The compile pipeline (`Compiler.Compile`) runs on every keystroke for language-server diagnostics. Its output is `Compilation` — a diagnostic-bearing analysis artifact. The Builder (`Precept.From(Compilation)`) runs once when a definition is deployed/loaded for execution. These have fundamentally different lifecycles:

| Concern | Pipeline (per-keystroke) | Builder (per-deploy) |
|---------|-------------------------|---------------------|
| Latency budget | <50ms total | <500ms acceptable |
| Output | Diagnostics, semantic model | Executable model (opcodes, dispatch indexes) |
| Triggered by | Source text change | Successful compilation (no errors) |
| Incremental | Must be (debounced) | Full rebuild (no incremental path) |

The proof engine reasons about intervals and containment using **decimal arithmetic on `NumericInterval`**. The evaluator enforces constraints using **prebuilt opcode arrays** (per `evaluator.md`). These are different representations optimized for different access patterns. Forcing them to share a representation would make the proof engine dependent on the Builder (circular dependency) or make the Builder part of the analysis pipeline (violating its design identity as documented in `precept-builder.md` §2).

**The correct factoring:** The proof engine uses `NumericInterval` for static analysis. The Builder (future) reads the same `TypedField.NormalizedDeclaredMin/Max` values and encodes them into constraint plan opcodes. Both consume the same **source data** (normalized bounds on `TypedField`), but produce different **representations** optimized for their consumers. This is the right seam.

---

### Q3: Can the Proof Engine and Evaluator Share Opcodes?

**No — and they shouldn't.** The proof engine performs *abstract interpretation* (computing over intervals, not concrete values). The evaluator performs *concrete evaluation* (computing over `PreceptValue` slots). These are fundamentally different execution models:

| Dimension | Proof Engine | Evaluator |
|-----------|-------------|-----------|
| Domain | `NumericInterval` (abstract ranges) | `PreceptValue` (concrete values) |
| Operation | Interval arithmetic (union, intersection, containment) | Scalar arithmetic (add, compare, store) |
| Precision | Over-approximation (sound, not complete) | Exact |
| Completeness | May return `Unbounded` (conservative) | Must produce a definite result or fault |
| Execution frequency | Every keystroke | Every Fire/Update call |

A shared opcode representation would force one of two bad outcomes:
1. The proof engine interprets opcodes abstractly (maintaining a parallel abstract stack) — this reimplements interval arithmetic with extra indirection.
2. The proof engine and evaluator share a "plan" type but interpret it differently — this adds coupling without reducing code.

**What they CAN share:** The normalized bound values. Both the proof engine (`IntervalContainmentProofRequirement.DeclaredMax`) and the evaluator (via Builder's constraint plan compilation) need the same normalized decimal. The **shared artifact is the `TypedField` with its normalized bounds** — not an execution plan.

**Recommendation:** No shared opcode representation. The proof engine continues using `NumericInterval`. The Builder (when implemented) reads `TypedField.NormalizedDeclaredMin/Max` and emits its own constraint opcodes. The shared seam is the semantic model, not an intermediate representation.

---

### Q4: Is UCUM Logic in the Right Place?

**`src/Precept/Language/Numeric/` is correct.** The reasoning:

- `src/Precept/Language/Ucum/` owns UCUM *parsing* — taking a unit string and producing `UcumParsedUnit` with its `Scale` factor. This is a complete, self-contained concern.
- `src/Precept/Runtime/` owns runtime *execution* — `PreceptValue`, `Evaluator`, `Version`. Normalization is not a runtime concern today (evaluator is a stub) and when it becomes one, it will be the Builder's responsibility during constraint plan compilation.
- `src/Precept/Language/Numeric/` is the right home for "given a magnitude and a parsed unit, produce a normalized magnitude." It's a **consumer** of UCUM parse results applied to numeric values — a bridge between the unit system and the numeric comparison system.

The normalizer depends on `UcumParsedUnit` and `UcumExactFactor` (UCUM types) and produces `decimal` (numeric type). Placing it in `Language/Numeric/` correctly positions it as a numeric utility that happens to consume UCUM data, rather than a UCUM utility that happens to produce numeric results.

---

### Q5: Wrapping PreceptValue with QuantityValue

**Not recommended for the compile-time fix. Premature for the runtime.** Here's the analysis:

A `QuantityValue` wrapper would look like:
```csharp
public readonly record struct QuantityValue(decimal Magnitude, UcumParsedUnit Unit)
{
    public decimal NormalizedMagnitude => TypedConstantNormalizer.ApplyFactor(Magnitude, Unit.Scale);
    public bool FitsWithin(decimal normalizedMin, decimal normalizedMax) => ...;
}
```

**For compile-time (ProofEngine):** The proof engine doesn't operate on individual values — it operates on **intervals**. A `QuantityValue` wrapper would need to be `QuantityInterval` to be useful, and `NumericInterval` already does this job once magnitudes are normalized. Adding a wrapper type between the parsed tuple and `NumericInterval` adds a construction step without reducing complexity. The normalization is a single `ApplyFactor` call — wrapping it in a type doesn't improve the callsite.

**For runtime (Evaluator):** The runtime `PreceptValue` is a 32-byte struct with a union payload. Whether quantities use the reference region (pointing to a heap composite) or the decimal region (with unit on `FieldDescriptor`) is an open implementation decision per `evaluator.md`. A `QuantityValue` wrapper is premature — it would constrain the runtime storage decision before D8/R4 is designed.

**For the Builder (future):** The Builder reads `TypedField.NormalizedDeclaredMin/Max` (decimals) and emits constraint opcodes. It doesn't need a quantity-aware wrapper — it just needs the normalized number.

**Verdict:** The simpler design (normalize via `TypedConstantNormalizer.ApplyFactor`, store as `decimal`, compare as `decimal`) is correct for all three consumers. A `QuantityValue` wrapper would be an unnecessary abstraction layer that doesn't compose better — it just adds an indirection step before the same decimal comparison.

---

### Q6: The Interpolated Case — A Better Approach

The current plan (Slices 19–21) extends `InterpolatedTypedConstant` with a `UcumParsedUnit?` and adds a case to `IntervalOfNarrowed`. This is correct but can be generalized.

> **Terminology note:** This design uses `InterpolatedTypedConstant` throughout. The AST node is currently named `TypedInterpolatedTypedConstant` in the codebase — the "Typed" prefix appeared twice redundantly. The code rename to `InterpolatedTypedConstant` is a separate task.

**The insight:** Interval scaling by a UCUM factor is a **general operation on `NumericInterval`**, not a special case per AST node type. The pattern is:

```
interval_of(expr) = raw_interval(magnitude_source) × unit_scale_factor
```

This applies uniformly to:
- Static typed constants: `Point(6) × 0.45359237 = Point(2.72)`
- Interpolated typed constants: `(-∞, 2] × 453.59237 = (-∞, 907.18]`
- Any future AST node that produces a quantity from a magnitude + static unit

**Better design:** Instead of adding per-node-type normalization logic in `IntervalOfNarrowed`, factor it as:

1. `IntervalOfNarrowed` computes the **raw magnitude interval** for any expression (existing logic, unchanged).
2. A post-step checks: does this expression carry a static `UcumParsedUnit`? If so, scale the interval.
3. The static unit is resolved via a helper `TryGetStaticUnit(TypedExpression) → UcumParsedUnit?` that works for `TypedTypedConstant` (unit from parsed tuple) and `InterpolatedTypedConstant` (unit from text segments) uniformly.

```csharp
private static NumericInterval IntervalOf(TypedExpression expr, SemanticIndex semantics)
{
    var rawInterval = IntervalOfNarrowed(expr, semantics, null);
    if (rawInterval.IsUnbounded) return rawInterval;
    
    // Scale ONLY for typed constants with static UCUM units.
    // Field refs and WholeValue slots are already in the field's unit system.
    var scalingFactor = TryGetStaticScalingFactor(expr);
    if (scalingFactor is not null)
        return rawInterval.Scale(scalingFactor.Value);
    
    return rawInterval;
}

// Returns pre-computed decimal scaling factor (null = no scaling needed).
// For quantity: factor = unit.Scale (direct)
// For price: factor = 1/unit.Scale (inverse — denominator unit)
// For WholeValue slots, FieldRef, ArgRef: null (already normalized)
private static decimal? TryGetStaticScalingFactor(TypedExpression expr) => expr switch
{
    TypedTypedConstant { ParsedValue: ValueTuple<decimal, UcumParsedUnit?>(_, { } unit) } 
        => ApplyFactor(1m, unit.Scale),
    TypedTypedConstant { ParsedValue: ValueTuple<decimal, object?, UcumParsedUnit?>(_, _, { } denomUnit) }
        => ApplyFactor(1m, denomUnit.Scale.Inverse()),  // price: inverse factor
    InterpolatedTypedConstant { StaticUnit: { } unit, Slots: [{ SlotKind: Magnitude }] }
        => ApplyFactor(1m, unit.Scale),
    InterpolatedTypedConstant { StaticUnit: { } unit, Slots: [{ SlotKind: DenominatorUnit or NumeratorUnit }] }
        => null,  // dynamic unit slot — cannot scale statically
    _ => null    // FieldRef, ArgRef, WholeValue, everything else
};
```

> **§0.6 Condition 2 clarification:** The post-step is **expression-type-dispatched**, not universal. `TryGetStaticScalingFactor` returns a scaling factor ONLY for the two expression forms that carry a static UCUM unit. All other forms — `TypedFieldRef`, `TypedArgRef`, `InterpolatedTypedConstant` with `WholeValue` slot kind — return `null` (no scaling). This is critical: field-ref intervals are already in normalized units (via `GetFieldBounds` reading `NormalizedDeclaredMin/Max`), so scaling them again would double-normalize.
>
> | Expression type | Scaling action |
> |---|---|
> | `TypedTypedConstant` with quantity unit | Scale by `ApplyFactor(1m, unit.Scale)` → `decimal` |
> | `TypedTypedConstant` with price denominator unit | Scale by inverse: `ApplyFactor(1m, denomUnit.Scale.Inverse())` |
> | `InterpolatedTypedConstant` with `Magnitude` slot + static unit | Scale by `ApplyFactor(1m, unit.Scale)` |
> | `InterpolatedTypedConstant` with `WholeValue` slot | **No scaling** — already in field's declared unit system |
> | `InterpolatedTypedConstant` with dynamic unit/currency slot | **No scaling** — `null` (returns `Unbounded` from `IntervalOfNarrowed`) |
> | `TypedFieldRef` | **No scaling** — `GetFieldBounds` already reads normalized values |
> | `TypedArgRef` | **No scaling** — `ExtractArgInterval` already reads normalized values |

This eliminates the need for per-node normalization in `TryGetTypedConstantMagnitude` and centralizes all interval scaling to one place. The `IntervalOfNarrowed` method stays pure (computes raw magnitude intervals), and unit awareness is layered on top via the expression-type-dispatched `TryGetStaticScalingFactor`.

**Consequence for Slices 14–18:** The ProofEngine's `TryGetTypedConstantMagnitude` does NOT need to normalize. It returns the raw magnitude. Normalization happens in the wrapping `IntervalOf` via `TryGetStaticUnit`. This is cleaner — each function has one job.

**Consequence for Slices 19–21:** The `InterpolatedTypedConstant` still needs to carry `UcumParsedUnit?` for its static unit portion (Slice 19 is correct). But the interval scaling logic in Slice 20 becomes trivial — it's the same `rawInterval.Scale(unit.Scale)` path that handles static typed constants.

**What stays on `TypedField`:** The TypeChecker's bound extraction (`DeclaredMin/Max`) still needs normalization at extraction time — because bounds flow into `IntervalContainmentProofRequirement` as plain `decimal?` values, not as intervals-with-units. The TypeChecker stores normalized bounds; the ProofEngine scales assignment intervals. Both normalize to the same UCUM base unit. Containment check compares normalized bounds against normalized intervals.

---

### Summary of Changes from Current Plan

| Aspect | Current Plan (§3) | Revised Approach |
|--------|-------------------|-----------------|
| TypeChecker bounds | Normalize in `TryGetComparableTypedConstantValue`, store on `DeclaredMin/Max` | **Store both original AND normalized** — add `NormalizedDeclaredMin/Max` to `TypedField` |
| ProofEngine magnitude extraction | Normalize inside `TryGetTypedConstantMagnitude` | **Do NOT normalize here** — return raw magnitude |
| ProofEngine interval scaling | None (implicit in magnitude extraction) | **New: universal `IntervalOf` post-step** that scales by static unit |
| `IntervalContainmentProofRequirement` | Reads `DeclaredMin/Max` (normalized under current plan) | Reads `NormalizedDeclaredMin/Max` (explicitly named) |
| Interpolated case (Slices 19–21) | Per-node special case in `IntervalOfNarrowed` + interval scaling | **Unified** via `TryGetStaticUnit` + same `IntervalOf` post-step |
| Normalizer location | `src/Precept/Language/Numeric/` | **Unchanged** — correct placement |
| `NormalizedNumericValue` type | Proposed in §3.3 | **Simplify** — just use `decimal` for the normalized result; drop `OriginalMagnitude` and `ConversionFactor` fields (unnecessary for comparison) |
| Builder/evaluator shared opcodes | Not in current plan (correctly deferred) | **Confirmed: no shared opcodes.** Shared seam is `TypedField.NormalizedDeclaredMin/Max` |
| `QuantityValue` wrapper | Not in current plan | **Not recommended** — `decimal` is sufficient |

---

### Revised Key Types

> **Two-Layer Value Architecture:** The semantic model (`TypedField`, `IntervalContainmentProofRequirement`)
> stores normalized bounds as `decimal?` because the primary consumer is the ProofEngine's abstract interval
> arithmetic, which operates on `decimal` ranges per-keystroke. The executable model (`LoadLit` opcode)
> stores bounds as `PreceptValue` because the evaluator operates exclusively on `PreceptValue`. The Builder
> is the explicit conversion boundary: it reads `decimal` from `TypedField.NormalizedDeclaredMin/Max` and
> constructs `PreceptValue` for the `LoadLit` opcode payload. See §0.2 for the full rationale.

```csharp
// TypedField gains explicit normalized bounds (original values preserved for display).
// These are decimal? because the ProofEngine (per-keystroke hot path) does interval arithmetic
// on decimal ranges. The Builder wraps these into PreceptValue at build time (once per deploy).
public sealed record TypedField(
    // ... existing fields ...
    decimal? DeclaredMin = null,           // original authored magnitude
    decimal? DeclaredMax = null,           // original authored magnitude
    decimal? NormalizedDeclaredMin = null,  // magnitude in UCUM base units (null = no bound or no unit)
    decimal? NormalizedDeclaredMax = null,  // magnitude in UCUM base units
    // ... existing qualifier fields ...
);

// Normalizer simplifies to a single static method
public static class TypedConstantNormalizer
{
    /// Returns the magnitude normalized to UCUM base units, or the raw magnitude if no unit.
    public static decimal Normalize(decimal magnitude, UcumParsedUnit? unit);
    
    /// For price: normalizes by inverse of denominator unit.
    public static decimal NormalizePrice(decimal magnitude, UcumParsedUnit? denominatorUnit);
}

// NumericInterval gains a Scale operation
public readonly record struct NumericInterval(...)
{
    /// Scales both bounds by a decimal factor (UcumExactFactor → decimal conversion
    /// happens once in TryGetStaticScalingFactor, not here).
    public NumericInterval Scale(decimal factor) => ...;
}

// IntervalContainmentProofRequirement uses normalized bounds (decimal for interval arithmetic).
// The ProofEngine checks containment via decimal interval comparison. PreceptValue is irrelevant
// here — abstract interpretation has no concept of PreceptValue.
public sealed record IntervalContainmentProofRequirement(
    ProofSubject Subject,
    string TargetField,
    decimal? DeclaredMin,          // original (for display)
    decimal? DeclaredMax,          // original (for display)
    decimal? NormalizedMin,        // for comparison (null = use DeclaredMin as-is)
    decimal? NormalizedMax,        // for comparison (null = use DeclaredMax as-is)
    string Description
) : ProofRequirement(ProofRequirementKind.IntervalContainment, Description);

// At the Builder boundary (Pass 5), the conversion happens:
// decimal normalizedMax = typedField.NormalizedDeclaredMax.Value;
// var lit = new LoadLit(PreceptValue.FromClr(normalizedMax));  // decimal → PreceptValue wrapping
// constraintPlan.Emit(lit);  // LoadLit carries PreceptValue, not decimal
```

---

### Revised Slice Impacts

| Slice | Change from Current Plan |
|-------|------------------------|
| **14** | Simpler: `TypedConstantNormalizer` becomes two static methods returning `decimal`. Drop `NormalizedNumericValue` record. Add `NumericInterval.Scale(decimal factor)` — `UcumExactFactor → decimal` conversion happens once in `TryGetStaticScalingFactor`. |
| **15** | TypeChecker stores both `DeclaredMax` and `NormalizedDeclaredMax` on `TypedField`. `IntervalContainmentProofRequirement` creation (in `Actions.cs`) reads the normalized values. |
| **16** | Simpler: `TryGetTypedConstantMagnitude` stays UNCHANGED (returns raw magnitude). New `TryGetStaticUnit(TypedExpression)` helper + `IntervalOf` post-step does the scaling. |
| **17** | Test assertions update for the new `NormalizedDeclaredMin/Max` fields on `TypedField`. |
| **18** | Display uses `DeclaredMin/Max` (original) + qualifier labels. No de-normalization needed. |
| **19** | Unchanged: `InterpolatedTypedConstant` gains `UcumParsedUnit?` for static unit. |
| **20** | Simpler: `TryGetStaticUnit` returns the unit for both `TypedTypedConstant` and `InterpolatedTypedConstant`. The same `IntervalOf` post-step handles both. No separate `IntervalScale` method needed on `NumericInterval` — reuse `Scale`. |
| **21** | Unchanged: integration tests. |

---

### Open Questions Requiring Shane's Input

**Q7: Should `IntervalContainmentProofRequirement` carry both original and normalized bounds?**

The revised design adds `NormalizedMin/Max` alongside `DeclaredMin/Max` to the proof requirement. Alternative: drop `DeclaredMin/Max` from the requirement entirely (it's always available on `TypedField` via `TargetField` name lookup). This would keep the requirement smaller but make diagnostic rendering require a `SemanticIndex` lookup. The current diagnostic rendering code in `ProofEngine.Diagnostics.cs` and `RichHoverFactory.cs` already has `SemanticIndex` in scope — so dropping the display values from the requirement is viable. **Which do you prefer?**

**Q8: Should `NumericInterval.Scale` use `UcumExactFactor` or `decimal`?**

Using `UcumExactFactor` preserves exact rational arithmetic until the final multiplication. Using `decimal` (pre-converting the factor) is simpler but introduces the lossy step earlier. Given that the interval bounds are already `decimal` (lossy), pre-converting the factor to `decimal` before scaling loses no additional precision in practice. **Simpler API (`decimal factor`) or exact API (`UcumExactFactor`)?**

**Q9: Q2's verdict — you asked whether `DeclaredMin/Max` should store original or normalized.**

The revised design stores BOTH on `TypedField` (original in `DeclaredMin/Max`, normalized in `NormalizedDeclaredMin/Max`). This resolves Q2 definitively: we don't have to choose. The TypeChecker computes both in one pass. Diagnostic display uses original values. Proof comparison uses normalized values. The only cost is 2 extra `decimal?` fields per `TypedField` — negligible. **Do you agree with the "store both" approach, or do you want to minimize `TypedField` width?**

---

## §0.1 Catalog Integration for Quantity Normalization

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Shane's question — "How will this work with the runtime functions and opcodes that will live in the catalog?"

---

### Verdict: Normalization Is NOT a Catalog Concern

Quantity unit normalization does **not** belong in any catalog. It is implementation logic, not language surface. The catalog-system.md § Architectural Identity test is decisive:

> "Is this part of a complete description of Precept?"

A `NormalizeToBaseUnit` function would not appear in any description of the Precept language. Authors never write `normalize(weight)` in a `.precept` file. There is no `ScaleByUcumFactor` operator in the language surface. UCUM normalization is internal plumbing that makes cross-unit comparison *correct* — it is not a user-visible operation, modifier, function, or constraint form.

The fourteen catalogs describe **what the language IS** — its vocabulary, types, operations, functions, modifiers, actions, constructs, constraints, proof requirements, and failure modes. Normalization is none of these. It is a correctness mechanism inside the implementation of two existing catalog-defined concepts: the `IntervalContainment` proof requirement (catalog #11, `ProofRequirements`) and the evaluator's constraint plan execution (which consumes catalog-defined opcodes).

---

### Q1: Should Quantity Unit Normalization Be Expressed as Catalog Entries?

**No.** Here is the principle-based justification:

**The catalog test (catalog-system.md § Completeness Principle):** "If I enumerated every catalog's `All` property, would I have a complete description of Precept?" Adding a `NormalizeToBaseUnit` function to the Functions catalog would mean it appears in MCP `precept_language` output, LS completions, hover text, and AI grounding. That's wrong — normalization is not something authors invoke. It's something the system does internally to make `max '5 kg'` correctly comparable to `set weight = '6 [lb_av]'`.

**The anti-pattern test (catalog-system.md § Architectural Identity):** "Does any pipeline stage switch on a `*Kind` enum value to apply per-member behavior?" The normalization logic does NOT switch on `FunctionKind` or `OperationKind`. It switches on the *presence of a UCUM unit* on a typed-constant value. This is structural dispatch on AST shape, not member-identity dispatch on catalog entries. There is no catalog smell here.

**What normalization actually is:** A compile-time transformation applied during semantic analysis (TypeChecker stores normalized bounds) and proof obligation discharge (ProofEngine scales intervals). It produces `decimal` values that flow into existing catalog-defined infrastructure:

- `IntervalContainmentProofRequirement` (already in the ProofRequirements catalog as `ProofRequirementKind.IntervalContainment`)
- `LOAD_LIT` opcodes at build time (the Builder embeds pre-normalized decimal bounds into constraint plan literals)
- `BINARY_OP(OperationKind.CompareDecimal...)` at runtime (already in the Operations catalog)

Normalization feeds into catalog-defined operations. It is not itself a catalog-defined operation.

---

### Q2: How Does the Proof Engine's `NumericInterval.Scale` Relate to What the Evaluator Will Do?

**They are different operations on different representations consuming the same source data.**

| Dimension | Proof Engine (compile-time) | Evaluator (runtime) |
|-----------|---------------------------|---------------------|
| Operation | `NumericInterval.Scale(factor)` — abstract interval arithmetic | `BINARY_OP(CompareLessEqual)` — concrete value comparison via `Func<PreceptValue, PreceptValue, PreceptValue>` delegate |
| Operands | Interval bounds (`decimal` min/max) | `PreceptValue` slot values |
| Value representation | `decimal` — the ProofEngine's native arithmetic type | `PreceptValue` — the evaluator's exclusive value currency |
| Normalization point | ProofEngine's `IntervalOf` post-step scales raw interval by static UCUM factor | Builder reads normalized `decimal` from `TypedField`, wraps as `PreceptValue` into `LoadLit` opcode at build time |
| Result | `NumericInterval` (abstract containment check) | `bool` (concrete comparison result) |

**The evaluator never normalizes.** The Builder's Pass 5 (Constraint Plan Pass) reads `TypedField.NormalizedDeclaredMin/Max` (a `decimal?`), constructs a `PreceptValue` via `PreceptValue.FromClr(normalizedMax)`, and embeds it in a `LoadLit(PreceptValue Value)` opcode. At runtime, the evaluator executes:

```
LOAD_SLOT(weight.SlotIndex)    // push the field's current PreceptValue from slot array
MEMBER_ACCESS(.magnitude)      // extract the numeric magnitude (already in base units per storage convention)
LOAD_LIT(normalizedMaxPV)      // push the pre-normalized bound as PreceptValue (wrapping 5.0m for '5 kg')
BINARY_OP(CompareLessEqual)    // concrete comparison via catalog-resolved Func<PreceptValue, PreceptValue, PreceptValue>
```

**Critical clarification (addressing Shane's consistency concern):** `LOAD_LIT` carries `PreceptValue`, not raw `decimal`. Per `precept-builder.md` Pass 4: `public sealed record LoadLit(PreceptValue Value) : Opcode; // literals pre-wrapped at build time`. The operations library's delegates (`Func<PreceptValue, PreceptValue, PreceptValue>`) operate on the same `PreceptValue` type for both runtime-collected values and compile-time-declared bounds. **At the evaluator level, there IS representational consistency** — both args and bounds are `PreceptValue`.

The "normalization" for the runtime path happened at **build time** when the Builder read `NormalizedDeclaredMax = 5.0m` from `TypedField` and wrapped it as `PreceptValue` into `LoadLit`. The evaluator never calls `TypedConstantNormalizer` — it just executes prebuilt opcodes against concrete `PreceptValue` operands.

**The shared seam is `TypedField.NormalizedDeclaredMin/Max`** — a semantic model fact produced once by the TypeChecker, consumed by:
1. **ProofEngine** — reads the `decimal?` directly for abstract interval containment arithmetic
2. **Builder** — reads the `decimal?` and wraps into `PreceptValue` for `LoadLit` opcode embedding

This is the correct architecture per §0 Q3: "The shared artifact is the `TypedField` with its normalized bounds — not an execution plan." The Builder is the conversion boundary between the `decimal` analysis world and the `PreceptValue` runtime world.

---

### Q3: What Catalog Entries Need to Be Added or Modified?

**None.** No catalog changes are required for quantity normalization. Here is the complete analysis:

| Concern | Catalog Impact | Rationale |
|---------|---------------|-----------|
| `TypedField.NormalizedDeclaredMin/Max` | None | Semantic model, not catalog. `TypedField` is an analysis artifact on `SemanticIndex`. |
| `IntervalContainmentProofRequirement` | None | Already exists as `ProofRequirementKind.IntervalContainment` in the ProofRequirements catalog. The requirement gains `NormalizedMin/Max` fields, but this is the *proof requirement instance shape*, not catalog metadata. |
| `TypedConstantNormalizer` | None | Static utility class in `Language/Numeric/`. Not a catalog function — it's compile-time infrastructure. |
| `NumericInterval.Scale` | None | Method on the proof engine's interval type. Not catalog-facing. |
| Builder constraint plan compilation | None | The Builder already reads `TypedField` properties and emits `LOAD_LIT` opcodes. Reading `NormalizedDeclaredMax` instead of `DeclaredMax` and wrapping via `PreceptValue.FromClr(normalizedMax)` is a trivial change in Pass 5 — no new opcodes, no new catalog entries. |
| Evaluator runtime enforcement | None | Existing opcodes (`LOAD_SLOT`, `MEMBER_ACCESS`, `LOAD_LIT`, `BINARY_OP`) suffice. `LOAD_LIT` carries `PreceptValue` (not raw `decimal`). No new opcode is needed because normalization is absorbed into the `PreceptValue` literal value at build time. |

**Key insight:** The Builder's opcode inventory (§ precept-builder.md Pass 4) already contains everything needed. `LOAD_LIT(PreceptValue Value)` carries a `PreceptValue` — the Builder wraps the normalized `decimal` from `TypedField` into `PreceptValue` at build time. The evaluator's `BINARY_OP(kind)` dispatches via catalog-resolved `Func<PreceptValue, PreceptValue, PreceptValue>` delegates. No new catalog entries or opcodes are needed because normalization is **data**, not **behavior** — it produces a different literal value, not a different operation. The representational consistency Shane requires IS satisfied at the evaluator level: both bounds and runtime values are `PreceptValue`.

**What about `FieldDescriptor`?** The Builder's Pass 1 (Descriptor Pass) builds `FieldDescriptor` from `TypedField`. Whether `FieldDescriptor` carries a UCUM unit reference (for runtime re-normalization of incoming values) is a Phase 3 runtime design decision (D8/R4). For the current compile-time fix (Slices 14–21), `FieldDescriptor` does not need changes — the Builder reads normalized bounds from `TypedField` and bakes them into constraint plan literals.

---

### Q4: Is There a Catalog-Driven Design That Makes Proof Engine and Evaluator Share Normalization Through Metadata?

**No — and there shouldn't be.** Here's why this question exposes a category error:

The catalog-driven principle states: *"Pipeline stages are generic machinery that reads catalog metadata — they never maintain parallel copies or encode domain knowledge in their own logic."*

Normalization is **not domain knowledge in the catalog-system sense.** Domain knowledge (per catalog-system.md) means: "something that IS the language" — a keyword, type, operation, function, modifier, construct, or constraint form. Normalization is not any of these. It is a **correctness implementation detail** of how bound values are stored and compared.

The catalog-driven principle prevents this anti-pattern: *"The parser hardcodes that `min` is a modifier keyword instead of reading `Modifiers.All`."* That's catalog knowledge encoded in pipeline logic. Normalization has no equivalent anti-pattern — no catalog knows "this value needs UCUM scaling" because UCUM scaling is not a language concept.

**What IS the correct sharing mechanism?** The semantic model (`TypedField`). Both the ProofEngine and the Builder consume `TypedField.NormalizedDeclaredMin/Max`. Both derive their behavior from the same source of truth. But that source of truth is not catalog metadata — it's a computed property on the analysis artifact, produced by the TypeChecker from catalog-defined type rules + UCUM reference data.

**The distinction:**
- **Catalog metadata** = "what legal operations exist" (e.g., `OperationKind.CompareDecimalLessEqual` with its `IntervalTransfer` function)
- **Semantic model facts** = "what this specific definition declares" (e.g., `weight` has `NormalizedDeclaredMax = 5.0m` in base UCUM units)
- **Reference data** = "what UCUM says about kg" (e.g., `kg` has scale factor `1.0` relative to base unit `g × 10³`)

Normalization lives at the intersection of semantic model facts and reference data. It never touches the catalog layer.

**When would normalization BECOME a catalog concern?** Only if the language surface exposed a user-invocable `normalize()` function or a `@normalized` modifier — i.e., if authors could explicitly request normalization in `.precept` files. That is not the case and is not planned. Normalization is implicit: the language guarantees correct cross-unit comparison without requiring authors to think about it.

---

### Impact on Slices 14–21

**No revision required.** The current slice plan (as revised in §0) is correct as-is:

| Slice | Catalog Impact | Status |
|-------|---------------|--------|
| 14 | None — `TypedConstantNormalizer` is a static utility, not a catalog entry | Unchanged |
| 15 | None — `TypedField.NormalizedDeclaredMin/Max` is a semantic model property, not catalog metadata | Unchanged |
| 16 | None — `TryGetStaticUnit` + `IntervalOf` post-step use existing `UcumParsedUnit` reference data | Unchanged |
| 17–18 | None — tests and display formatting | Unchanged |
| 19–21 | None — interpolated case uses same infrastructure | Unchanged |

**Future Phase 3 impact (when the Builder/Evaluator are implemented):**

The Builder will read `TypedField.NormalizedDeclaredMin/Max` during Pass 5 (Constraint Plan Pass), wrap the `decimal` value into `PreceptValue` via `PreceptValue.FromClr(normalizedBound)`, and embed it in `LoadLit(PreceptValue)` opcodes in constraint expression plans. This requires:

1. Builder Pass 1 (Descriptor Pass) to propagate `NormalizedDeclaredMin/Max` onto `FieldDescriptor` (or read directly from `SemanticIndex.TypedFields`).
2. Builder Pass 5 to use normalized values when compiling bound-comparison constraint expressions.

Neither of these requires catalog changes. The Builder already reads `TypedField` properties and already emits `LoadLit(PreceptValue)` opcodes. The conversion from normalized `decimal` to `PreceptValue` is a trivial `FromClr` call — the same conversion the Builder applies to every other literal in the source.

**The one future decision:** When a `PreceptValue` for a `quantity` field is stored at runtime, will it store the magnitude in base UCUM units (pre-normalized at write time) or in authored units (requiring normalization at comparison time)? This is the Phase 3 "storage convention" decision (evaluator.md's pending internal layout question). If base units, the constraint plan is simply `LOAD_SLOT → magnitude accessor → LOAD_LIT(normalizedMax) → compare`. If authored units, the plan needs an additional normalization step. This decision doesn't affect catalogs either way — it affects `FieldDescriptor` and the Builder's expression compiler, not the language surface.

---

### Summary

| Question | Verdict |
|----------|---------|
| Should normalization be catalog entries? | **No.** Not language surface. |
| Are proof engine and evaluator doing the same operation? | **No.** Abstract interval scaling vs. concrete prebuilt-literal comparison. |
| What catalog entries need adding? | **None.** |
| Should sharing be through catalog metadata? | **No.** Sharing is through `TypedField` (semantic model), which is the correct seam. |
| Do Slices 14–21 need revision? | **No.** |

---

## §0.2 Two-Layer Value Architecture — PreceptValue as Runtime Currency

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Shane's representational consistency pushback — "A default value declared in the precept shouldn't be treated differently than a runtime value collected by an arg."

---

### Shane's Argument, Evaluated Honestly

Shane identified a real clarity gap in the previous draft. His argument:

1. Opcode delegates are typed `Func<PreceptValue, PreceptValue, PreceptValue>` — the operations library operates exclusively on `PreceptValue`.
2. `LOAD_LIT` carries `PreceptValue Value` (confirmed: `public sealed record LoadLit(PreceptValue Value) : Opcode`).
3. A bound declared at compile time (`max '5 kg'`) and a value collected at runtime (`set weight = arg`) must have the same representation when they reach the evaluator.
4. Therefore, using `decimal` in the design creates a representational inconsistency.

**Shane is right about points 1–3.** The evaluator's value currency IS `PreceptValue`. Both compile-time bounds and runtime values ARE `PreceptValue` when they participate in evaluator operations. The design already reflects this at the executable model level — `LoadLit(PreceptValue)` is the proof.

**Shane's inference (point 4) is partially wrong** — but the previous draft's language was sufficiently imprecise to make his concern legitimate. The draft said "pre-bakes normalized `decimal` bounds into `LOAD_LIT` opcodes" — which reads as if `LOAD_LIT` carries a raw `decimal`. It doesn't. It carries `PreceptValue`. The draft created a misleading impression of representational inconsistency that doesn't actually exist in the design.

---

### The Actual Architecture: Two Layers, One Truth

The design has **two layers** with a clean conversion boundary:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ANALYSIS LAYER (per-keystroke, ProofEngine hot path)                     │
│                                                                           │
│  TypedField.NormalizedDeclaredMin/Max : decimal?                          │
│  IntervalContainmentProofRequirement.NormalizedMin/Max : decimal?         │
│  NumericInterval bounds : decimal                                         │
│                                                                           │
│  WHY decimal: Abstract interval arithmetic (union, intersection,          │
│  containment, scaling) requires decimal — PreceptValue has no interval    │
│  algebra. The Operations catalog has no IntervalContainment delegate      │
│  because intervals don't exist at runtime. This layer runs on every       │
│  keystroke — it needs the most direct representation for its arithmetic.  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    Builder (conversion boundary, once per deploy)
                    reads decimal → constructs PreceptValue
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  EXECUTION LAYER (per-Fire/Update call, Evaluator hot path)               │
│                                                                           │
│  LoadLit.Value : PreceptValue                                             │
│  Version.Slots : PreceptValue[]                                           │
│  Evaluation stack : PreceptValue (stackalloc)                             │
│  BinaryOp.Executor : Func<PreceptValue, PreceptValue, PreceptValue>       │
│                                                                           │
│  WHY PreceptValue: The evaluator operates exclusively on PreceptValue.    │
│  A bound declared as `max '5 kg'` IS a PreceptValue in this layer —       │
│  indistinguishable from an arg collected at runtime. Shane's consistency  │
│  requirement IS satisfied here.                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

**The Builder is the conversion boundary.** Its Pass 5 (Constraint Plan Pass) does:
```csharp
decimal normalizedMax = typedField.NormalizedDeclaredMax!.Value;  // read from semantic model
var lit = new LoadLit(PreceptValue.FromClr(normalizedMax));       // wrap into runtime currency
constraintPlan.Emit(lit);                                         // embed in opcode array
```

After this point, the bound IS a `PreceptValue` — identical in representation to any runtime-collected arg value. The evaluator cannot distinguish them. Shane's requirement — "a default value shouldn't be treated differently than a runtime value" — is satisfied.

---

### Why NOT PreceptValue in the Semantic Model?

The alternative Shane implies — storing `TypedField.NormalizedDeclaredMin/Max` as `PreceptValue?` — would mean:

1. **ProofEngine must extract decimal on every access:** `typedField.NormalizedDeclaredMax!.ToClr<decimal>()` — adding an unwrap step to every interval arithmetic operation, on every keystroke, for zero benefit. The ProofEngine cannot USE a `PreceptValue` — it needs `decimal` for interval union/intersection/containment/scaling.

2. **TypeChecker must construct runtime values:** The TypeChecker (an analysis stage) would need to import and construct `PreceptValue` — coupling a compile-time analysis component to the runtime value type's internal layout decisions (which are still pending per evaluator.md).

3. **No consumer benefits:** The ProofEngine (primary consumer, per-keystroke) would pay an extraction penalty. The Builder (secondary consumer, once per deploy) already constructs `PreceptValue` from scalars as its fundamental job — that's what Pass 4/5 does for EVERY literal, not just bounds.

4. **Interval arithmetic has no `PreceptValue` analogue:** `NumericInterval.Scale`, `NumericInterval.Contains`, `NumericInterval.Union` — none of these operations exist on `PreceptValue`. The ProofEngine does abstract interpretation that is structurally incompatible with the concrete-value model. You cannot compute `PreceptValue.Intersect(other)` — that's not what values do.

---

### Answering Shane's Specific Questions

**Q1: Should `TypedField.NormalizedDeclaredMin/Max` be `PreceptValue?` instead of `decimal?`?**

**No.** `TypedField` is a compile-time semantic model artifact. Its primary consumer is the ProofEngine, which does `decimal`-based interval arithmetic per-keystroke. Making it `PreceptValue?` adds an extraction cost for zero benefit. The Builder converts to `PreceptValue` at build time — that's its job.

**Q2: Should `IntervalContainmentProofRequirement.NormalizedMin/Max` be `PreceptValue?` instead of `decimal?`?**

**No.** The proof requirement feeds directly into `NumericInterval.Contains(min, max)` — pure decimal arithmetic. PreceptValue would require extraction at every check with no consumer that benefits.

**Q3: Should `LOAD_LIT` in the constraint plan carry a `PreceptValue`?**

**Yes — and it already does.** `public sealed record LoadLit(PreceptValue Value) : Opcode; // literals pre-wrapped at build time`. This was never in question. The design already specifies this correctly. The previous draft's imprecise language ("pre-bakes normalized `decimal` bounds into `LOAD_LIT`") created the false impression that `LOAD_LIT` carries a raw decimal. It doesn't. It carries `PreceptValue`. Shane's concern about `LOAD_LIT` is resolved: the design IS consistent.

**Q4: What does the proof engine's interval arithmetic look like?**

It extracts decimal from the semantic model and operates on `NumericInterval` — a `decimal`-based abstract interval type. It never touches `PreceptValue`:

```csharp
// ProofEngine interval containment check — pure decimal arithmetic
var normalizedMin = typedField.NormalizedDeclaredMin;  // decimal? — read directly
var normalizedMax = typedField.NormalizedDeclaredMax;  // decimal? — read directly
var fieldBounds = NumericInterval.FromBounds(normalizedMin, normalizedMax);
var assignedInterval = IntervalOf(assignmentExpr);      // returns NumericInterval (decimal bounds)
var contained = fieldBounds.Contains(assignedInterval); // pure decimal comparison
```

`PreceptValue` is irrelevant here. The ProofEngine does abstract interpretation on ranges — it never evaluates concrete values.

**Q5: Is there a two-layer design with an extraction boundary?**

**Yes — that is exactly this design.** Layer 1: `decimal` in the analysis pipeline (semantic model + ProofEngine). Layer 2: `PreceptValue` in the execution pipeline (Builder output + Evaluator). The Builder is the extraction/conversion boundary. It reads `decimal` from Layer 1 and produces `PreceptValue` for Layer 2. This is architecturally sound because:

- Each layer uses the representation native to its operations
- The conversion happens once per deploy (not per-keystroke)
- The boundary is explicit and named (Builder Pass 5)
- After conversion, Shane's consistency requirement is fully satisfied — bounds and runtime values are indistinguishable `PreceptValue` operands

---

### Impact on Slices 14–21

**No type changes required.** The slice plan's types are correct as-is:
- `TypedField.NormalizedDeclaredMin/Max` stays `decimal?` (ProofEngine needs it)
- `IntervalContainmentProofRequirement.NormalizedMin/Max` stays `decimal?` (interval arithmetic needs it)
- `LOAD_LIT` was always `LoadLit(PreceptValue Value)` — no change needed

What changed: the EXPLANATION is now explicit about why `decimal` is correct in the analysis layer and how the Builder converts to `PreceptValue` for the execution layer. The previous draft's imprecise language created a false impression of inconsistency.

---

## §0.3 Code Sharing Between Compiler and Runtime

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Shane's question — "How will the runtime share the same code as the compiler to do unit conversions, work with default values, etc. if compiler uses decimals and runtime uses PreceptValue?"

---

### The Problem: Duplication Risk

Shane identifies a legitimate concern: if the compiler normalizes typed-constant magnitudes using one implementation (operating on `decimal`), and the runtime normalizes incoming values using a separate implementation (operating on `PreceptValue`), the project carries two implementations of the same UCUM domain logic that could drift independently.

The question is: **is this actually the situation, or does the current architecture already avoid the duplication?**

---

### Dependency Direction — The Enabling Fact

The assembly structure makes this tractable:

```
Language/  ← owns UcumParsedUnit, UcumExactFactor, TypedConstantNormalizer
    ↑                           ↑
Pipeline/  (TypeChecker,        Runtime/  (Evaluator, Builder,
 ProofEngine)                    PreceptValue)
```

Both `Pipeline` and `Runtime` depend on `Language`. Neither depends on the other. Any utility placed in `Language/` is callable from both the compiler pipeline and the runtime/builder without circular dependency.

This means **`TypedConstantNormalizer` in `Language/Numeric/` is already visible to both consumers.** No assembly restructuring is needed.

---

### Option Analysis

#### Option A: Shared Static Utility on PreceptValue

```csharp
public static class QuantityNormalizer
{
    public static PreceptValue Normalize(PreceptValue quantity, UcumParsedUnit targetUnit);
}
```

**Verdict: Rejected.**

- `PreceptValue` lives in `Runtime/`. Placing the normalizer there makes it invisible to `Pipeline/` (which doesn't reference `Runtime/`). The TypeChecker and ProofEngine cannot call it.
- Forces the ProofEngine to construct `PreceptValue` for every magnitude it normalizes (just to call the utility), then extract the `decimal` back. Pure overhead on the per-keystroke hot path.
- Couples the analysis pipeline to the runtime value type's internal layout decisions (still pending per evaluator.md).
- The ProofEngine does interval arithmetic on `decimal` — it never holds a `PreceptValue` and has no reason to.

#### Option B: Catalog-Defined Function

```csharp
// A NormalizeToBase function in the Functions catalog
Functions.GetMeta(FunctionKind.NormalizeToBase).Delegate(pv) → pv
```

**Verdict: Rejected.**

- Fails the catalog test: "Would `NormalizeToBase` appear in MCP `precept_language` output, LS completions, or a complete description of the language surface?" No. Authors never write `normalize(weight)` in `.precept` files. Normalization is implicit — it's what makes `max '5 kg'` correctly comparable to `set weight = '6 [lb_av]'` without author intervention.
- Pollutes the user-visible function catalog with implementation plumbing.
- The Builder doesn't emit `CALL_FUNC(NormalizeToBase)` opcodes — normalization is absorbed into literal values at build time. There is no runtime function call.

#### Option C: Shared UCUM Math in Language/, Builder Applies-Once (RECOMMENDED)

```csharp
// Language/Numeric/TypedConstantNormalizer.cs — the ONE implementation
public static class TypedConstantNormalizer
{
    /// Applies the UCUM scale factor to produce a magnitude in base units.
    public static decimal Normalize(decimal magnitude, UcumParsedUnit? unit)
    {
        if (unit is null) return magnitude;
        return ApplyFactor(magnitude, unit.Scale);
    }

    internal static decimal ApplyFactor(decimal magnitude, UcumExactFactor factor)
    {
        // 2-3 decimal multiplications — nanoseconds
        var scaled = magnitude * (decimal)factor.Numerator / (decimal)factor.Denominator;
        return factor.Base10Exponent switch
        {
            0 => scaled,
            > 0 => scaled * PowerOf10(factor.Base10Exponent),
            < 0 => scaled / PowerOf10(-factor.Base10Exponent),
        };
    }
}
```

**Consumers:**
1. **TypeChecker** — calls `TypedConstantNormalizer.Normalize(rawMagnitude, unit)` and stores result as `TypedField.NormalizedDeclaredMin/Max` (`decimal?`).
2. **ProofEngine** — reads pre-normalized `decimal?` from `TypedField` directly (no normalization call). For assignment expressions, the `IntervalOf` post-step calls `NumericInterval.Scale(unit.Scale)`, which internally delegates to the same `ApplyFactor` math.
3. **Builder (future)** — reads `TypedField.NormalizedDeclaredMin/Max` (`decimal?`), wraps via `PreceptValue.FromClr(normalizedMax)`, embeds in `LoadLit(PreceptValue)` opcode. The Builder calls `TypedConstantNormalizer.Normalize` only if it ever needs to normalize a value not already on `TypedField` (e.g., a default value literal in a future language feature).

**Why this works:**
- **ONE implementation** of UCUM scale math lives in `Language/Numeric/TypedConstantNormalizer`.
- Both `Pipeline/` and `Runtime/` can see it (both depend on `Language/`).
- The ProofEngine uses `decimal` output directly for interval arithmetic.
- The Builder wraps `decimal` into `PreceptValue` at the single build-time boundary — this is trivial delegation (`PreceptValue.FromClr(d)`), not a second normalization implementation.
- **No duplication.** The `PreceptValue.FromClr(decimal)` call is TYPE WRAPPING, not NORMALIZATION LOGIC. The normalization (scale math) happens once, in `TypedConstantNormalizer`. The wrapping happens once, in the Builder. These are different concerns.

#### Option D: Normalizer in Runtime Assembly

**Verdict: Rejected.**

- `Pipeline/` doesn't reference `Runtime/`. Moving the normalizer to `Runtime/` makes it invisible to the TypeChecker and ProofEngine.
- Would require a new shared assembly or dependency inversion — over-engineering for a utility that is 3 lines of decimal math.
- The current `Language/` placement already provides universal visibility without architectural cost.

---

### Shane's Concern Addressed Directly

**Q: "How will the runtime share the same code as the compiler if compiler uses decimals and runtime uses PreceptValue?"**

**A:** They already do share the same code. The normalization logic (UCUM scale math) lives in `Language/Numeric/TypedConstantNormalizer` — one implementation, callable from anywhere. What differs is the **value wrapping**, not the **conversion logic**:

| Consumer | Calls TypedConstantNormalizer? | Input type | Output type | What it does with the result |
|----------|-------------------------------|------------|-------------|------------------------------|
| TypeChecker | Yes, directly | `decimal` | `decimal` | Stores on `TypedField.NormalizedDeclaredMin/Max` |
| ProofEngine | Indirectly (reads pre-computed `decimal` from TypedField; `NumericInterval.Scale` reuses same factor math) | `decimal` | `NumericInterval` | Interval containment check |
| Builder | Reads pre-computed `decimal` from TypedField | `decimal` (from TypedField) | `PreceptValue` (via `FromClr`) | Embeds in `LoadLit` opcode |
| Evaluator | Never normalizes | — | — | Executes prebuilt opcodes with pre-normalized `PreceptValue` literals |

The "two implementations" Shane feared would exist **only if the evaluator normalized at runtime** — applying UCUM scale math to `PreceptValue` operands during `Fire`/`Update`. That would require a parallel normalizer operating on `PreceptValue` instead of `decimal`. But the architecture prevents this: **the Builder pre-applies normalization at deploy time.** By the time the evaluator sees a bound value, it's already normalized and wrapped as `PreceptValue`. The evaluator just compares two `PreceptValue` operands — it never calls conversion code.

**The drift risk Shane identifies is structurally impossible** in this architecture, because:
1. Normalization logic exists in exactly one place (`TypedConstantNormalizer`).
2. The Builder consumes the SAME normalized `decimal` values the TypeChecker computed — it reads from `TypedField`, doesn't re-derive.
3. The evaluator never normalizes — it executes prebuilt comparisons on pre-normalized literals.

There is no second implementation to drift.

---

### When Would This Change?

The architecture would need revision **only if** a future Phase 3 feature requires runtime normalization of values that weren't pre-normalized at build time. The one scenario:

**Scenario: Runtime quantity value storage in authored units.** If `PreceptValue` stores quantity magnitudes in the AUTHOR's declared unit (e.g., `6 lb_av` stored as `6.0` with unit metadata) rather than pre-normalizing to base units at write time, then every constraint check would need to normalize the stored value before comparison.

In that scenario, the normalizer would need a `PreceptValue`-accepting overload:
```csharp
// Future Phase 3 — only if storage convention is "authored units"
public static decimal NormalizeFromPreceptValue(PreceptValue quantity)
{
    var magnitude = quantity.ToClr<decimal>();  // extract
    var unit = quantity.GetUnit();              // hypothetical accessor
    return Normalize(magnitude, unit);         // SAME core logic
}
```

Even in this scenario, the core conversion logic (`ApplyFactor`) stays unchanged — the new method is a thin extractor that delegates to the same math. This is acceptable because:
- The domain logic (UCUM scale math) is still one implementation
- The new method adds extraction/delegation, not alternative math
- The drift-prone surface (factor application) remains singular

**But this scenario is unlikely.** The more natural storage convention (and the one `precept-builder.md` implies) is base-unit normalization at write time — the Builder normalizes incoming values when compiling the `set` action's expression plan. Under this convention, the evaluator STILL never normalizes, and the current architecture remains correct indefinitely.

---

### Impact on Slices 14–21

**No changes required.** The current plan already implements Option C:

- Slice 14 creates `TypedConstantNormalizer` in `Language/Numeric/` with `decimal`-based static methods — this IS the single shared implementation.
- Slice 15 has the TypeChecker call it and store normalized `decimal?` on `TypedField` — this IS the TypeChecker's consumption path.
- Slice 16 has `NumericInterval.Scale` reuse the same factor arithmetic for the ProofEngine's post-step.
- The Builder (future Phase 3) reads from `TypedField.NormalizedDeclaredMin/Max` and wraps via `PreceptValue.FromClr` — this IS type wrapping, not a second normalizer.

The plan was already correct. What was missing was the explicit explanation of WHY it avoids code duplication — which this section provides.

---

### Verdict

**Option C is the correct architecture.** UCUM scale math lives once in `Language/Numeric/TypedConstantNormalizer`, visible to all consumers via the existing dependency graph. The "code sharing" concern is resolved not by making the normalizer operate on `PreceptValue`, but by recognizing that normalization and type-wrapping are separate concerns that compose at the Builder boundary. No changes to Slices 14–21 required.

---

## §0.4 Runtime Arg Normalization — The Intake Boundary

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Shane's challenge — "How will the runtime deal with args that are not known at compile time? Won't the runtime need to dynamically convert these using the same logic as the compiler pipeline?"

---

### §0.3 Was Incomplete

Shane is correct. §0.3's conclusion — "The drift risk Shane identifies is structurally impossible in this architecture, because normalization logic exists in exactly one place (TypedConstantNormalizer). The evaluator never normalizes — it executes prebuilt opcodes." — is **only true for compile-time-known literal bounds**.

The scenario §0.3 missed:

```precept
field weight as quantity of 'mass' max '5 kg'

event SetWeight(newWeight: quantity of 'mass')
from Running on SetWeight
    -> set weight = newWeight
```

A caller fires `SetWeight` with `newWeight = '6 [lb_av]'`. The Builder can pre-normalize `max '5 kg'` into `LoadLit(PreceptValue(5.0, kg-base))`. But the Builder **cannot** pre-normalize `newWeight` — its value isn't known until Fire time.

The execution plan compiled for `set weight = newWeight` is:
```
LOAD_ARG(newWeight.SlotIndex)   // push the arg's PreceptValue (value unknown at build time)
STORE_SLOT(weight.SlotIndex)    // store into field slot
```

After action execution, the `weight` field slot contains whatever `PreceptValue` was in the arg. The constraint plan then executes:
```
LOAD_SLOT(weight.SlotIndex)     // push the quantity PreceptValue
MEMBER_ACCESS(.magnitude)       // extract the numeric magnitude
LOAD_LIT(normalizedMaxPV)       // push 5.0 (pre-normalized kg bound)
BINARY_OP(CompareLessEqual)     // compare
```

If `MEMBER_ACCESS(.magnitude)` returns the **authored** magnitude `6.0` (from `6 [lb_av]`), and the bound is `5.0` (from `5 kg` in base units), then `6.0 > 5.0` → **false constraint violation**. The exact same bug Shane identified.

**The "evaluator never normalizes" claim is still true** — normalization doesn't belong in the evaluator's opcode loop. But §0.3's corollary — "there is no second implementation to drift" — was written as if the Builder's pre-normalization of literal bounds was the complete story. It is not. Runtime arg values that carry non-base units require normalization somewhere before the constraint comparison executes.

---

### Where Runtime Arg Normalization Belongs: Normalize-on-Intake

The architectural answer is **normalize at the API ingress boundary** — the point where external values become `PreceptValue`. This is the `TypeRuntimeMeta.ReadJson` delegate (JSON lane) and the `TypeRuntime<Quantity>.FromClr` function (typed lane).

**The ingress path for a quantity arg:**

```csharp
// TypeRuntimeMeta.ReadJson for quantity type (JSON lane):
var magnitude = reader.GetDecimal();             // 6.0
var unitString = reader.GetString();             // "[lb_av]"
var parsedUnit = UcumParser.Parse(unitString);   // UcumParsedUnit with Scale factor
var normalizedMagnitude = TypedConstantNormalizer.Normalize(magnitude, parsedUnit);  // ← SAME code
return PreceptValue.FromQuantity(normalizedMagnitude, parsedUnit);
// Stores: normalized magnitude (for comparison) + original unit (for egress display)
```

```csharp
// TypeRuntime<Quantity>.FromClr (typed lane):
var normalizedMagnitude = TypedConstantNormalizer.Normalize(quantity.Magnitude, quantity.Unit);
return PreceptValue.FromQuantity(normalizedMagnitude, quantity.Unit);
```

**After intake normalization**, the constraint plan works correctly:
```
LOAD_SLOT(weight.SlotIndex)     // PreceptValue with normalized magnitude ≈ 2.72 (6 lb_av in kg-base)
MEMBER_ACCESS(.magnitude)       // extracts 2.72 (already in base units)
LOAD_LIT(5.0)                   // pre-normalized bound (5 kg in base units)
BINARY_OP(CompareLessEqual)     // 2.72 ≤ 5.0 → TRUE ✓
```

---

### Why Normalize-on-Intake, Not Normalize-at-Compare or Normalize-at-Write

Three options were analyzed:

| Option | Where | Mechanism | Verdict |
|--------|-------|-----------|---------|
| **A: Normalize-on-intake** | `TypeRuntimeMeta.ReadJson` / `TypeRuntime<Quantity>.FromClr` | Normalize magnitude to base units when constructing `PreceptValue` at Fire boundary | **SELECTED** |
| B: Normalize-at-write | Builder emits normalization opcode in action plan before `STORE_SLOT` | `LOAD_ARG → NORMALIZE_QUANTITY → STORE_SLOT` | Rejected |
| C: Normalize-at-compare | Constraint plan extracts both magnitude and unit, normalizes before comparison | Requires per-constraint normalization logic in every plan | Rejected |

**Option B rejected:** Would require a new `NORMALIZE_QUANTITY` opcode (catalog change), making the evaluator normalize (violating its "plan executor, not semantic reasoner" identity). The Builder would need to emit this opcode for every expression that references a quantity arg — coupling action plan compilation to knowledge of which args might carry non-base units.

**Option C rejected:** Pushes normalization into every constraint plan that touches a quantity field. Constraint plans would need `MEMBER_ACCESS(.magnitude) → MEMBER_ACCESS(.unit) → CALL_FUNCTION(NormalizeToBase)` sequences. This is the "normalize at every comparison" anti-pattern §0 explicitly rejected. It also requires a user-invisible `NormalizeToBase` function in the catalog — failing the catalog test (§0.1 Q1).

**Option A is correct because:**

1. **PreceptValue for quantity ALWAYS stores normalized magnitude.** This is the storage convention. Once established, every downstream consumer (constraint plans, computed field evaluation, slot reads) sees base-unit magnitudes without additional work.

2. **The evaluator remains a pure plan executor.** No semantic knowledge of units leaks into the opcode loop.

3. **The Builder's existing constraint plans work unchanged.** `LOAD_SLOT → MEMBER_ACCESS(.magnitude) → LOAD_LIT(normalizedBound) → BINARY_OP(Compare)` is correct when both sides are in base units.

4. **The normalization call is `TypedConstantNormalizer.Normalize(decimal, UcumParsedUnit?)`** — the identical method signature. No new overload. No `PreceptValue`-accepting variant. The ingress delegate extracts `decimal` magnitude + `UcumParsedUnit` from the JSON/CLR input, calls the existing normalizer, then wraps into `PreceptValue`.

5. **Egress de-normalization is trivial.** `PreceptValue` stores the original unit alongside the normalized magnitude. `TypeRuntimeMeta.WriteJson` and `TypeRuntime<Quantity>.ToClr` reverse the normalization: `TypedConstantNormalizer.Denormalize(normalizedMag, unit) → originalMag`.

---

### PreceptValue Storage Convention for Quantities (Decision — Previously "Pending Phase 3")

§0.3 deferred this as a "Phase 3 storage convention decision." Shane's challenge proves it cannot be deferred — it's a prerequisite for correct constraint evaluation on runtime args.

**Decision: Quantities store normalized (base-unit) magnitudes in PreceptValue.**

The `PreceptValue` for a quantity field carries:
- **Normalized magnitude** (decimal, in UCUM base units) — used by `MEMBER_ACCESS(.magnitude)` in constraint/expression plans
- **Original unit** (reference to `UcumParsedUnit` or UCUM code string) — used by egress for display/serialization

This parallels how the Builder already handles literal bounds: `TypedField.NormalizedDeclaredMax = 5.0m` (base units) → `LoadLit(PreceptValue.FromClr(5.0m))`. Runtime args follow the same convention — magnitudes enter as base-unit-normalized values.

**Consequence for `MEMBER_ACCESS(.magnitude)`:** This opcode extracts the normalized magnitude. It is the ONLY accessor constraint plans use for comparison. A separate `MEMBER_ACCESS(.originalMagnitude)` could exist for display contexts, but constraint plans never use it.

**Consequence for the Restore path:** `Precept.Restore(state, fields)` must also normalize quantity magnitudes when reading from persisted JSON. The same `TypeRuntimeMeta.ReadJson` delegate handles this — Restore uses the same ingress path as Fire/Update.

---

### Does This Require the "Unlikely Phase 3 Scenario" from §0.3?

**No.** §0.3 described a scenario requiring a `PreceptValue`-accepting overload:

```csharp
// §0.3's hypothetical — NOT needed
public static decimal NormalizeFromPreceptValue(PreceptValue quantity)
{
    var magnitude = quantity.ToClr<decimal>();
    var unit = quantity.GetUnit();
    return Normalize(magnitude, unit);
}
```

This was the scenario where the evaluator normalizes stored values at comparison time (Option C above — rejected). Under normalize-on-intake, this is unnecessary. The normalizer never sees a `PreceptValue` — it operates on the raw `decimal` + `UcumParsedUnit` BEFORE `PreceptValue` construction.

The call chain is:
```
External caller → Fire("SetWeight", {"newWeight": {"magnitude": 6.0, "unit": "[lb_av]"}})
    → TypeRuntimeMeta.ReadJson(reader)           // JSON lane ingress
        → decimal magnitude = 6.0
        → UcumParsedUnit unit = Parse("[lb_av]")
        → decimal normalized = TypedConstantNormalizer.Normalize(6.0, unit)  // ← existing method
        → PreceptValue.FromQuantity(normalized, unit)
    → PreceptValue enters arg slot array
    → Evaluator executes LOAD_ARG → STORE_SLOT → constraint check
```

The `TypedConstantNormalizer` call is on `decimal` — its existing signature. No new overload.

---

### Single-Implementation Claim: Updated Assessment

§0.3 stated: "Normalization logic exists in exactly one place (`TypedConstantNormalizer`)."

**This remains true.** The update is that §0.3's consumer table was incomplete:

| Consumer | Calls TypedConstantNormalizer? | When | Input |
|----------|-------------------------------|------|-------|
| TypeChecker | Yes | Per-keystroke, for literal bounds | `decimal` from parsed typed-constant |
| ProofEngine | Indirectly (reads pre-computed TypedField values; `NumericInterval.Scale` reuses factor math) | Per-keystroke | `decimal` |
| Builder | Reads pre-computed decimal from TypedField | Once per deploy | `decimal` from TypedField |
| **Ingress boundary (NEW)** | **Yes** | **Per Fire/Update/Restore call, for quantity args/fields** | **`decimal` from JSON/CLR input** |
| Evaluator | Never | — | — |

The drift risk remains structurally impossible because:
1. The normalization math is still one implementation (`TypedConstantNormalizer.Normalize`).
2. The ingress boundary calls the same method with the same signature — no forked logic.
3. The evaluator still never normalizes.

What §0.3 got wrong was the enumeration of call sites — it omitted the runtime ingress boundary. The architectural claim (single implementation, no drift) holds, but it has **four** consumers, not three.

---

### Denormalization for Egress

If `PreceptValue` stores normalized magnitudes, egress must reverse the normalization for display:

```csharp
// TypeRuntimeMeta.WriteJson for quantity type:
var normalizedMag = pv.GetMagnitude();        // normalized decimal from PreceptValue
var unit = pv.GetUnit();                       // original UcumParsedUnit
var originalMag = TypedConstantNormalizer.Denormalize(normalizedMag, unit);  // reverse scale
writer.WriteNumber("magnitude", originalMag);
writer.WriteString("unit", unit.CanonicalCode);
```

This requires a `Denormalize` method — the inverse of `Normalize`:

```csharp
public static class TypedConstantNormalizer
{
    public static decimal Normalize(decimal magnitude, UcumParsedUnit? unit);
    
    /// Reverses normalization: base-unit magnitude → original-unit magnitude.
    public static decimal Denormalize(decimal normalizedMagnitude, UcumParsedUnit? unit)
    {
        if (unit is null) return normalizedMagnitude;
        return ApplyInverseFactor(normalizedMagnitude, unit.Scale);
    }
}
```

`Denormalize` is the arithmetic inverse of `Normalize` — it divides by the factor instead of multiplying. This is a trivial addition to `TypedConstantNormalizer` and keeps all UCUM scale math in one place.

---

### Impact on Slices 14–21

Slices 14–21 address the **compile-time** normalization bug (false-positive `NumericOverflow` diagnostic). They remain correct and unchanged. The runtime ingress normalization is a **separate concern** — it ensures runtime constraint enforcement correctness, not diagnostic accuracy.

However, a new phase is needed for the runtime path:

**Phase 3 — Runtime Quantity Ingress Normalization (DEFERRED)**

> ⚠️ **NOT IMPLEMENTATION-READY (from George's review — B22).** `TypeRuntimeMeta`, `TypeRuntime<T>`, evaluator constraint execution, and quantity `PreceptValue` storage do not exist in code yet (`Runtime\PreceptValue.cs`, `Runtime\Evaluator.cs` are stubs). This slice ships ONLY after Phase 3 runtime surfaces exist. It is not numbered in the current implementation sequence.

- Implement normalize-on-intake in `TypeRuntimeMeta.ReadJson` and `TypeRuntime<Quantity>.FromClr` for quantity types.
- Add `TypedConstantNormalizer.Denormalize` for egress.
- Ensure `PreceptValue` storage convention stores normalized magnitude + original unit.
- **Dependencies:** Slice 14 (normalizer exists), Builder/Evaluator implementation (Phase 3)
1. The storage convention (normalized magnitudes in `PreceptValue`) must be known before `PreceptValue` internal layout is finalized.
2. `TypedConstantNormalizer.Denormalize` should be added alongside `Normalize` in Slice 14 to keep the API complete.
3. The ingress path design (`TypeRuntimeMeta.ReadJson` calling the existing normalizer) must be documented before Phase 3 implementation begins.

**Revision to Slice 14:** Add `TypedConstantNormalizer.Denormalize(decimal, UcumParsedUnit?)` alongside `Normalize`. Both methods are pure `decimal` math — `Denormalize` is `ApplyInverseFactor`. This keeps the normalizer complete for both directions even though egress won't be wired until Phase 3.

---

### Summary

| Question | Answer |
|----------|--------|
| Was §0.3's conclusion incomplete? | **Yes.** It was correct for compile-time literals but omitted runtime args. |
| Does the runtime need normalization? | **Yes.** At the ingress boundary, not in the evaluator. |
| Does this require a `PreceptValue`-accepting overload? | **No.** Ingress operates on raw `decimal` + `UcumParsedUnit` before constructing `PreceptValue`. |
| Where does runtime arg normalization belong? | **Normalize-on-intake** — in `TypeRuntimeMeta.ReadJson` / `TypeRuntime<Quantity>.FromClr`. |
| Does the single-implementation claim hold? | **Yes.** Same `TypedConstantNormalizer.Normalize` method, one additional call site. |
| Is a new slice needed? | **Yes.** Slice 22 (Phase 3) for ingress normalization + `Denormalize` method added to Slice 14 now. |
| Is the "Phase 3 storage convention" still deferred? | **No.** Decided: PreceptValue stores normalized magnitudes. |

---

## §0.5 Diagnostic Enforcement Alignment — Compiler/Runtime Duplication Assessment

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Shane's concern — "Did the diagnostic enforcement implementation compound the problem of duplication between the compiler and what will eventually live in the runtime?"

---

### Executive Verdict

**The diagnostic enforcement implementation did NOT compound compiler/runtime duplication.** It is architecturally clean — and in several places, it proactively *reduced* the future duplication surface by establishing catalog-mediated dispatch patterns that the runtime can directly consume. Shane's concern is legitimate in the abstract but does not apply to what was actually built.

Here is the full analysis.

---

### The Concern, Stated Precisely

When the runtime evaluator ships (Phase 3), it must enforce the same business rules the compiler checks at compile time. Shane's fear: if the diagnostic enforcement mission hardcoded rule-checking logic directly in the TypeChecker and ProofEngine — logic that the runtime will ALSO need to implement — then the project now carries two independent implementations of the same rules, with the second one yet to be written. Every enforcement rule wired during this mission could be a future drift liability.

---

### Classification of What Was Built

I traced every diagnostic enforcement slice against a three-category classification:

#### Category 1: Compile-Time-Only Rules (No Runtime Counterpart Needed)

These diagnostics enforce structural properties of the *definition* — things that are decided at compile time and can never change at runtime. The evaluator has no obligation to re-check them.

| Slice | Codes | Why Compile-Only |
|-------|-------|-----------------|
| **7** (Parser gates) | PRE0013, PRE0014, PRE0015 | Syntax rejection — invalid construct forms. The parser rejects them; the runtime never sees unparseable source. |
| **4** (EventHandler in stateful) | PRE0092 | Structural validity — a stateful precept cannot have top-level event handlers. This is a definition shape check. |
| **5A** (Ambiguous typed constant) | PRE0091 | Type resolution ambiguity in the typed-constant candidate set. Resolved entirely at compile time. |
| **9A** (Modifier constraint — closed) | N/A | Audited and closed — not viable, no code written. |
| **9B** (Typed-constant family diagnostics) | PRE0055–0058 | Temporal constant format/semantic validation. These validate the content of *authored literals* against NodaTime parse rules. No runtime input arrives as a raw temporal string that needs this same parse validation — `TypeRuntimeMeta.ReadJson` parses JSON wire formats (ISO 8601), not Precept literal syntax. |
| **8** (Scattered TypeChecker gaps) | PRE0027, PRE0035, PRE0039, PRE0042, PRE0043, PRE0044, PRE0050, PRE0067, PRE0085, PRE0105 | Definition-level structural checks: duplicate arg names, conflicting access modes, computed field with default, arg scoping violations, etc. All resolved at definition analysis time. |

**Verdict: Zero duplication risk.** These rules check the shape of the *authored definition*, not the behavior of a *running entity*. The runtime never re-validates these.

#### Category 2: Compiler Enforcement with Runtime Fault Correspondence

These diagnostics prevent conditions that COULD occur at runtime, but by design, a correctly compiled precept makes them impossible. The runtime carries a corresponding `FaultCode` as defense-in-depth, but the runtime fault is a "should never happen" assertion — not a parallel implementation of the same check.

| Slice | Codes | Corresponding FaultCode | Relationship |
|-------|-------|------------------------|-------------|
| **6** (Collection safety) | PRE0100 `IndexBoundsGuard`, PRE0104 `MissingOrderingKey` | `CollectionEmptyOnAccess` (FaultCode 9), `CollectionEmptyOnMutation` (FaultCode 10) | ProofEngine generates obligation → strategy attempts discharge → if unproved, diagnostic fires AND runtime fault is possible. The checking logic is NOT duplicated — the ProofEngine does abstract proof discharge (guard pattern matching), while the runtime would throw on the concrete illegal operation. Different concerns. |
| **6** (Staged) | PRE0099, PRE0101 | `CollectionEmptyOnAccess`, `CollectionEmptyOnMutation` | Same pattern — staged but architecturally clean. |

**Verdict: No duplication.** The `[StaticallyPreventable]` chain explicitly models this relationship. The compiler *proves absence* of the fault (abstract, symbolic). The runtime *detects presence* of the fault if it occurs despite the proof (concrete, defensive). These are structurally different operations that happen to govern the same semantic property. The enforcement implementation correctly placed the proof logic in the ProofEngine — the right location — and correctly linked to the existing FaultCode via `ProofRequirementMeta.DiagnosticCode`.

#### Category 3: Compiler Enforcement That Has Runtime Behavioral Overlap

This is the only category where duplication potential exists. These diagnostics check conditions that runtime args can also violate.

| Slice | Codes | Runtime Overlap | Assessment |
|-------|-------|----------------|-----------|
| **1** (Qualifier enforcement) | PRE0070–0074 | **YES for dynamic qualifiers.** Static qualifier enforcement (compile-time) checks `money in 'USD' + money in 'EUR'` → error. Dynamic qualifier enforcement (`money in '{CatalogCurrency}'`) is *deliberately skipped* by the TypeChecker and deferred to ProofEngine Strategy 5 / runtime. | **Not duplicated.** The TypeChecker code explicitly bails out on dynamic qualifiers (`TryGetStaticQualifiers` returns null → skip). The runtime will need its own qualifier compatibility check for dynamically-resolved qualifiers, but this is a DIFFERENT check site on DIFFERENT data — runtime concrete values vs. compile-time static qualifier analysis. The qualifier enforcement code in `TypeChecker.Expressions.cs` does NOT need to be re-implemented; it needs a PARALLEL path in the evaluator that operates on `PreceptValue` qualifier metadata. |
| **2** (Choice validation) | PRE0086, PRE0087, PRE0089 | **Partial.** `ChoiceLiteralNotInSet` validates *authored* literals against the declared domain — purely compile-time. `ChoiceArgOutsideFieldSet` validates that an event arg's declared domain is a subset of the target field's domain — also compile-time (arg domains are declared, not dynamic). | **No duplication for what was built.** Runtime choice validation (validating that a concrete arg VALUE is in the choice domain) is a different concern — it's an ingress validation at the `TypeRuntimeMeta.ReadJson` boundary (analogous to quantity ingress normalization). The choice enforcement code does not need to be replicated for this. |

**Verdict: The one area with runtime overlap (qualifier enforcement for dynamic qualifiers) was handled correctly.** The TypeChecker implementation explicitly bifurcates: static qualifiers → TypeChecker, dynamic qualifiers → deferred. This is the right architecture. No compile-time logic was written that will need a runtime clone.

---

### Catalog-Mediated Dispatch: A Proactive Win

Two enforcement slices actually *improved* the compiler/runtime alignment story:

**Slice 9B** introduced `TypedConstantFamilyMeta.FormatErrorCode` / `SemanticErrorCode` — catalog-driven diagnostic selection for typed-constant validation. This pattern means validation logic reads from metadata, not from hardcoded switch branches. When `TypeRuntimeMeta.ParseString` needs the same domain validation at runtime (for the authoring path), it can read the same `TypedConstantFamilyMeta` entries. The catalog owns the validation rules; consumers dispatch to them.

**Slice 9C** introduced `ProofRequirementMeta.DiagnosticCode` — catalog-driven proof requirement → diagnostic code mapping. This replaced per-kind hardcoded branches with metadata reads for all non-exceptional cases. The runtime fault system already has `FaultCode` members mapped to `DiagnosticCode` via `[StaticallyPreventable]`. When the evaluator needs to map a runtime fault back to its preventable diagnostic, it follows the same metadata chain. Slice 9C did not create a parallel chain — it unified the existing one.

**Slice 0** (Roslyn analyzer gates PRECEPT0027–0030) installed the standing automation that prevents future gaps. This is orthogonal to compiler/runtime duplication — it enforces that every diagnostic code has an emission site and a test. But the Gate 1 allow-list also creates an explicit inventory of what's NOT yet wired, making the scope of remaining work transparent. This is a net positive for alignment planning.

---

### The Structural Guarantee Against Duplication

The existing architecture has a structural property that makes most duplication concerns moot:

```
FaultCode (15 members)  ←→  DiagnosticCode (132 members)
    via [StaticallyPreventable]
    enforced by PRECEPT0001 + PRECEPT0002
```

Only 15 of 132 diagnostic codes have runtime fault counterparts. The enforcement mission wired ~30 codes. The vast majority are in Category 1 (compile-time-only) — they have no runtime echo at all.

The 15 `FaultCode` members represent the complete set of runtime failure modes. These are DEFENSE-IN-DEPTH faults — they fire only when the compiler's proof is incomplete or when external data bypasses the compile-time check. They are NOT re-implementations of the compiler's logic; they are concrete failure-mode detectors in the evaluator's opcode loop.

The `[StaticallyPreventable]` chain, enforced by PRECEPT0001 and PRECEPT0002, is the structural guarantee: every runtime fault maps to exactly one compile-time diagnostic, and vice versa for the preventable subset. This chain was ALREADY in place before the diagnostic enforcement mission started. The mission wired the compiler side of the chain for codes that were declared but never emitted. It did not create new runtime obligations.

---

### The One Real Concern: Ingress Validation

The genuinely load-bearing question for compiler/runtime alignment is not in the diagnostic enforcement implementation — it's in the **ingress validation** design that §0.3 and §0.4 of this document address.

When a Fire/Update/Restore call arrives with runtime args:

1. **Quantity normalization** — §0.4 resolves this via normalize-on-intake at `TypeRuntimeMeta.ReadJson`
2. **Choice domain validation** — needs an ingress check that the submitted choice value is in the field's declared domain
3. **Qualifier compatibility** — for dynamic qualifiers, needs a runtime check that the arg's qualifier matches the field's constraint

These are NOT duplication of compiler logic. They are ingress boundary validation — checking that external input conforms to the definition's contract before it enters the evaluator. The compiler validates *authored expressions*; the ingress boundary validates *submitted values*. Different data, different lifecycle, same invariant.

The correct architecture is:
- **`TypeRuntimeMeta.ReadJson`** (per-type ingress delegate) handles per-type intake validation: normalization, format validation, domain membership
- **Constraint plans** (prebuilt by the Builder) handle cross-field invariant checks post-write
- **`FaultCode`** (defense-in-depth) handles impossible-path detection in the evaluator

None of these require re-implementing the TypeChecker's or ProofEngine's diagnostic emission logic.

---

### Recommendations

1. **No remediation needed for the diagnostic enforcement implementation.** It is clean.

2. **Ingress validation design should be consolidated.** §0.4 handles quantity normalization at intake. Choice domain validation and dynamic qualifier checking need the same treatment — intake-time validation in `TypeRuntimeMeta.ReadJson` / `TypeRuntime<T>.FromClr`, not in the evaluator's opcode loop or as duplicated TypeChecker logic.

3. **Document the three-layer enforcement model explicitly.** The project has three enforcement layers that are architecturally distinct:
   - **Layer 1: Compile-time (Compiler pipeline)** — validates the *authored definition* against the language spec. Runs per-keystroke. 132 diagnostic codes.
   - **Layer 2: Ingress (TypeRuntimeMeta/TypeRuntime delegates)** — validates *submitted values* at the API boundary. Runs per-Fire/Update/Restore. Normalization + domain checks.
   - **Layer 3: Defense-in-depth (Evaluator FaultCode)** — catches impossible-path bugs. 15 fault codes, all `[StaticallyPreventable]`. Should never fire on a correctly compiled precept with correct ingress validation.

   This three-layer model should be added to `docs/runtime/runtime-api.md` or `docs/compiler-and-runtime-design.md` as a named architectural concept. It eliminates the "are we duplicating?" question by making the layer boundaries explicit.

4. **The ingress validation layer (Layer 2) should be designed as a coherent surface** — not piecemeal per-type. §0.4 designed quantity normalization for this layer. Choice domain validation and qualifier checking should follow the same pattern before Phase 3 implementation begins.

---

### Summary

| Question | Answer |
|----------|--------|
| Did diagnostic enforcement compound duplication? | **No.** |
| How many enforcement codes have runtime counterparts? | **~6 of ~30 newly wired** — and the counterparts are defense-in-depth faults, not re-implementations. |
| What's the real duplication risk? | **Ingress validation** — not the enforcement mission's scope. §0.4 addresses it for quantities; choice/qualifier need the same treatment. |
| What structural guarantee prevents duplication? | **`[StaticallyPreventable]` chain** + the three-layer enforcement model (compiler / ingress / defense-in-depth). |
| What should change? | **Name and document the three-layer model.** Design ingress validation as a coherent Layer 2 surface, not ad-hoc per-type logic. |

---

## §0.6 — Design Resolution Summary

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Resolution of the six conditions from §5.5.6 and George's §5.4 gap details.

### Resolved Conditions

| # | Condition | Resolution |
|---|-----------|------------|
| 1 | §0/§3/§7 contradiction on bounds storage | SUPERSEDED markers added to §3.6, §3.7, §7 Q2. §0's "store both" approach is authoritative. |
| 2 | `IntervalOf` post-step not scoped | Post-step is expression-type-dispatched via `TryGetStaticScalingFactor`. WholeValue slots, FieldRef, and ArgRef are excluded. §0 Q6 updated. |
| 3 | `GetFieldBounds` reads raw DeclaredMin/Max | Slice 16 updated: reads `NormalizedDeclaredMin ?? DeclaredMin` (normalized with null fallback for non-quantity types). |
| 4 | `TryGetStaticNumericValue` uses raw StaticMagnitude | Slice 16 updated: normalizes StaticMagnitude via `TryGetStaticScalingFactor` before returning trusted fact. |
| 5 | TypedArg has no normalized bounds | Decision: Option (a) — add NormalizedDeclaredMin/Max to TypedArg. Slice 15 updated with TypedArg parallel. |
| 6 | `NumericInterval.Scale` takes UcumExactFactor | Slice 14 updated: Scale(decimal factor). Factor conversion happens in TryGetStaticScalingFactor, not inside NumericInterval. |

### Scope Addition from §5.4

George's gap audit added Slices 22–26 (see §5.4). These are Phase 2 extended scope, not blocking conditions. They cover static qualifier capture, money/price interval extraction, field-default proof coverage, and event arg default resolution. See §5.6 for full slice details.

**Implementation gate status: CLEARED.** All six conditions resolved. Slices 14–21 may proceed after Shane's sign-off on this document.

---

### Condition Resolution Details

The following subsections provide the detailed rationale and specification for each resolved condition. If a condition and a slice spec appear to conflict, the resolution stated here governs.

---

### Condition 1 Resolved: §3 Supersession (→ §0 is the single authoritative description)

§0's "store both" approach is the approved design for bound storage on `TypedField`:
- `DeclaredMin/Max` — original authored magnitudes, preserved for display and diagnostics
- `NormalizedDeclaredMin/Max` — base-unit magnitudes, computed once at TypeChecker extraction time, consumed by the ProofEngine for containment comparison

§3.6 ("overwrite DeclaredMax with normalized"), §3.7 ("normalize inside TryGetTypedConstantMagnitude"), and §7 Q2 ("Option B: normalize at proof time") each describe a competing approach. All three are now marked `SUPERSEDED`. The §0 Summary of Changes table is the single authoritative description of how bounds flow through the pipeline.

### Condition 2 Resolved: `IntervalOf` Post-Step Is Expression-Type-Scoped, Not Universal

The `IntervalOf` post-step scales intervals by a UCUM factor for exactly two expression categories:

| Expression type | Action |
|---|---|
| `TypedTypedConstant` with a static unit | Scale by `unit.Scale → ApplyFactor → decimal` |
| `InterpolatedTypedConstant` with `Magnitude` slot kind + static unit text segment | Scale by same |

The post-step does **NOT** scale:

| Expression type | Reason |
|---|---|
| `InterpolatedTypedConstant` with `WholeValue` slot kind | The source value already carries quantity semantics; its interval is already in the field's declared unit system |
| `TypedFieldRef` | Interval comes from `GetFieldBounds` which reads `NormalizedDeclaredMin/Max` (already in base units after Condition 3) |
| `TypedArgRef` | Same — interval from `ExtractArgInterval` which reads `NormalizedDeclaredMin/Max` after Condition 5 |
| Everything else | No UCUM scaling context |

The helper `TryGetStaticUnitFactor(TypedExpression) → decimal?` implements this dispatch. It returns the pre-computed scale factor (as `decimal`) for the two scaled cases, and `null` for all others. See Slice 16 for the full implementation spec.

### Condition 3 Resolved: `GetFieldBounds` Reads Normalized Values

`GetFieldBounds` (`ProofEngine.Intervals.cs:131-165`) reads `TypedField.NormalizedDeclaredMin/Max` when populated (quantity fields), falling back to `DeclaredMin/Max` for non-quantity fields where normalization is N/A. This ensures field-ref intervals are in base units, making all interval comparisons unit-homogeneous. Without this, a `TypedFieldRef` to a quantity field would produce a raw-unit interval that the post-step would then incorrectly scale a second time. See updated Slice 16b spec.

### Condition 4 Resolved: `TryGetStaticNumericValue` Normalizes `StaticMagnitude`

`ProofEngine.Composition.cs:221-223` currently extracts `StaticMagnitude` raw for trusted-rule facts (Strategy 6). After normalization ships, bounds are normalized but `StaticMagnitude` remains raw — fact-comparison would produce wrong cross-unit results. The fix:

```csharp
case InterpolatedTypedConstant { StaticMagnitude: { } magnitude, StaticUnit: { } unit }:
    value = TypedConstantNormalizer.Normalize(magnitude, unit);
    return true;
case InterpolatedTypedConstant { StaticMagnitude: { } magnitude }:
    value = magnitude;   // no static unit — raw magnitude, no conversion needed
    return true;
```

This depends on `InterpolatedTypedConstant` carrying `StaticUnit` (from Slice 22). See updated Slice 16c spec.

### Condition 5 Resolved: `TypedArg` Normalized Bounds — Approach (a) Selected

**Decision: Add `NormalizedDeclaredMin/Max` to `TypedArg` (Approach a — parallel to `TypedField`).**

Event args can carry `min`/`max` bounds (e.g., `newWeight: quantity of 'mass' max '10 [lb_av]'`). `ExtractArgInterval` (`ProofEngine.Intervals.cs:114-129`) reads `arg.DeclaredMin/Max` — raw un-normalized magnitudes, the same class of bug as the field-bound issue. After Slices 14–16, field-side bound intervals are in base units, but arg-side bound intervals would remain in raw (authored) units — creating an asymmetry where field-ref intervals are normalized but arg-ref intervals are not.

**Approach (a)** adds `NormalizedDeclaredMin/Max` to `TypedArg` in the TypeChecker, parallel to `TypedField`. `ExtractArgInterval` reads the normalized values.

**Approach (b)** — normalize on-the-fly in `ExtractArgInterval` using arg qualifier metadata — would work functionally but re-derives normalization at every proof check (per-keystroke hot path). Approach (a) is architecturally consistent: the TypeChecker normalizes once at extraction time and stores the result.

**Tradeoff accepted:** `TypedArg` gains two extra `decimal?` fields (`NormalizedDeclaredMin/Max`). This is the same cost as `TypedField` — negligible. The consistency benefit outweighs the minor type width increase.

**New Slice 15b** (see §5.2) covers `TypedArg` normalization.

### Condition 6 Resolved: `NumericInterval.Scale` Takes `decimal`

`NumericInterval.Scale` takes a `decimal factor` parameter. The `UcumExactFactor → decimal` conversion happens once in `TypedConstantNormalizer.ApplyFactor`. Passing `UcumExactFactor` through to `NumericInterval` would couple the interval algebra to UCUM types unnecessarily — the interval bounds are already `decimal`, and `decimal` is the native representation at the interval arithmetic layer. See updated Slice 14 spec.

---

## §0.7 — George's Technical Review Disposition

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** George's per-slice technical review returned "APPROVED WITH CONDITIONS." This section documents Frank's disposition of each finding.

### Disposition Summary

| Finding | Verdict | Disposition |
|---------|---------|-------------|
| **G14** | feasible-with-caveats | **ACCEPT.** Sentinel-bound scaling and inverse-price math test risk noted. Design already resolves via Condition 6 (Scale takes decimal). |
| **B15** | feasible-with-caveats | **ACCEPT.** Type name corrected (`TypedArg` not `TypedEventArg`). Regression risk on `ProofEngineIntervalIntegrationTests` added to slice spec. |
| **B16** | feasible-with-caveats | **ACCEPT.** Three critical constraints added: (1) dynamic-unit forms MUST NOT fall back to raw `StaticMagnitude`; (2) P1 orthogonality explicit — PRE0116 can fire even when interval proof succeeds; (3) obligation-shape blast radius acknowledged. |
| **G17** | feasible | **ACCEPT.** WholeValue interpolation (`'{qtyField}'`) coverage added to Slice 17 test list. |
| **B18** | feasible-with-caveats | **ACCEPT WITH MODIFICATION — DISPLAY CONTRACT NOW LOCKED (see §7 Q1, 2026-05-14).** Display contract strengthened and full display specification is now complete. Q1 (decided by Shane, 2026-05-14) locked the format: primary display in the field's declared unit with de-normalization; `(computed: ...)` parenthetical shows raw normalized values. Format: `[−∞ .. 5 kg] (computed: [6 [lb_av] .. 6 [lb_av]])`. The "store both" approach provides the required raw material, and the display contract is no longer deferred. |
| **B19** | feasible-with-caveats | **ACCEPT.** Ordering note added: if Slice 22 (static qualifier capture) ships first, Slice 19's `StaticUnit` field is subsumed by the richer `StaticQualifier` payload. Implementer should not reshape the node twice. |
| **G20** | feasible-with-caveats | **ACCEPT.** Expression-type-scope constraint made more prominent in Slice 20 spec. |
| **G21** | feasible | **ACCEPT.** Conservative-case tests added (dynamic-unit holes stay unproved/unbounded). |
| **B22** | not recommended as currently planned | **ACCEPT.** Duplicate Slice 22 numbering fixed: §5.6's "static qualifier capture" retains Slice 22; §0.4's "runtime ingress normalization" moved to Phase 3 deferred section (unnumbered). Runtime slice explicitly marked not-implementation-ready. |
| **B23** | feasible-with-caveats | **ACCEPT.** "Do not duplicate WholeValue source expressions" constraint added. Regression risk noted. |
| **B24** | feasible-with-caveats | **ACCEPT.** Ordering constraint added: depends on static qualifier capture (Slice 22) first. |
| **B25** | feasible-with-caveats, under-specified | **ACCEPT WITH MODIFICATION.** Chose single path: **stronger default folding** as primary mechanism (FoldValue gains InterpolatedTypedConstant case). A dedicated default-obligation collector (NOT DynamicObligationGenerator) handles non-foldable interpolated defaults as secondary. |
| **B26** | not recommended in this track | **OVERRIDDEN (Shane direction, 2026-05-14).** Slice 26 reinstated in-track. Scoped to typed-constant defaults for quantity/money/price event args — not general expression defaults. Implementation is bounded: ~40-line `ResolveEventArgExpressions` pass modeled on `ResolveFieldExpressions`. |
| **G27** | feasible-after-shape-freeze | **ACCEPT.** Shape-freeze dependency made explicit: Slice 27 gates on Slices 16/18 settling final proof-requirement and display shapes. |

### Accepted Ordering

George's recommended ordering accepted with one modification — Slice 19 is NOT folded into Slice 22 (they solve different problems: interval extraction vs. qualifier metadata capture). The `StaticUnit` field needed by Slice 19/20 is a strict subset of Slice 22's `StaticQualifier` payload, so either ordering works, but they remain separate slices.

**Final ordering:**

```
Phase 1 (core normalization):
  14 → 15 + 15b → 16 → 17 → 18

Phase 2 (interpolated extension):
  19 → 20 → 21 → 22 → 23 → 24 → 25 → 27 → 26

Deferred (separate issues):
  Runtime ingress normalization (Phase 3 — blocked on evaluator/Builder implementation)
```

### Top Implementation Risks (from George, accepted)

1. ~~Duplicate Slice 22 numbering~~ — **RESOLVED** (renumbered).
2. `IntervalContainmentProofRequirement` shape change rippling into Actions, diagnostics, MCP, and tests — **ACKNOWLEDGED.** Mitigated by the existing "store both" design which limits the blast radius to adding fields, not changing existing ones.
3. Unsound raw-`StaticMagnitude` fallback for dynamic-unit interpolated constants — **ADDRESSED** in Slice 16 constraint (B16 finding).
4. Display drift: authored bounds vs normalized computed intervals — **ADDRESSED** via "store both" + display contract note in Slice 18.
5. Interpolated/default work depends on broader missing plumbing (`TypedArg` defaults, default-proof collection) — **ADDRESSED.** Slice 26 reinstated in-track (Shane direction); scoped to typed-constant defaults for quantity/money/price args. Slice 25 handles field defaults.

---

### 1.1 The Bug

A field declared as `field weight as quantity in 'kg' max '5 kg'` with an assignment `set weight = '6 [lb_av]'` emits a false-positive `NumericOverflow` diagnostic. The raw magnitude `6` exceeds the raw bound `5`, but after UCUM unit conversion `6 lb_av ≈ 2.72 kg`, which is well within the `5 kg` maximum.

### 1.2 Root Cause — Two Defect Sites

**Defect Site 1: TypeChecker.Validation.Modifiers.cs**

`TryExtractTypedConstantMagnitude` (line 434) extracts the raw decimal `.Item1` from the typed-constant tuple `(6m, UcumParsedUnit { CanonicalCode: "[lb_av]", Scale: ... })` and returns `6m` — ignoring `.Item2` (the `UcumParsedUnit`) entirely. This magnitude is stored as `TypedField.DeclaredMin` / `DeclaredMax` — a raw, un-normalized decimal.

```csharp
// TypeChecker.Validation.Modifiers.cs:434-452
private static bool TryExtractTypedConstantMagnitude(object? parsedValue, out decimal magnitude)
{
    // ...
    if (parsedValue is ITuple tuple && tuple.Length > 0 && tuple[0] is decimal tupleMagnitude)
    {
        magnitude = tupleMagnitude;  // ← BUG: ignores unit scale entirely
        return true;
    }
    // ...
}
```

Identically, `TryGetComparableTypedConstantValue` (line 410) calls this method and stores the raw magnitude in `ExtractedBoundValue.Magnitude`, which flows to `TypedField.DeclaredMin`/`DeclaredMax` via `TypeChecker.cs:398-399`.

**Defect Site 2: ProofEngine.Intervals.cs**

`TryGetTypedConstantMagnitude` in `ProofEngine.Composition.cs` (line 231) has the same pattern — extracts `.Item1` and discards the unit. When the ProofEngine computes `IntervalOf` for a `TypedTypedConstant`, it creates `NumericInterval.Point(6m)` instead of the correct normalized point `NumericInterval.Point(2.721...)`.

The interval containment check in `TryIntervalContainmentProof` (line 222) then compares:
- Result interval: `[6, 6]` (raw magnitude of the `'6 [lb_av]'` assignment)
- Declared max: `5m` (raw magnitude of the `'5 kg'` bound)
- `6 > 5` → containment fails → `NumericOverflow` emitted

Both comparisons assume values are in a common unit. They are not.

### 1.3 Qualifier Compatibility Check Is Orthogonal

Slice 10 of the interval design added `BoundsQualifierMismatch` (PRE0134), which checks that the qualifier axis values match (e.g., a `kg` bound against a `[lb_av]` assignment). This is a *type-level* compatibility check — it ensures the field's declared qualifier and the bound's qualifier are in the same dimensional family. It does **not** normalize magnitudes. Even if the qualifier check passes (same dimension), the magnitude comparison is still raw.

### 1.4 Impact Scope

This bug affects every typed-constant literal comparison where the value's unit differs from the field's declared unit, across all three qualified numeric types:

| Type | Example | Bug trigger |
|------|---------|-------------|
| `quantity` | `max '5 kg'` vs `set x = '6 [lb_av]'` | `6 > 5` → false overflow |
| `money` | Not affected — currencies are NOT convertible (by design) |
| `price` | `max '10 USD/kg'` vs `set x = '8 USD/[lb_av]'` | `8 > 10` is fine, but the denominator unit differs |

Money is intentionally excluded from normalization. Currency conversion is a business decision, not a unit conversion. `'100 USD'` and `'100 EUR'` are **different types** — comparing them is a type error (`BoundsQualifierMismatch`), not a normalization opportunity.

### 1.5 Interpolated Quantity Expressions — A Separate Problem

**Discovery:** The actual `samples/Test.precept` file contains an *interpolated* quantity assignment, not a static typed-constant literal:

```precept
field test as quantity of 'mass' max '5 kg'
field test2 as integer max 2 optional
...
from offState on toggle
    -> set test = '{test2} [lb_av]'
```

Line 14 (`'{test2} [lb_av]'`) produces a `InterpolatedTypedConstant` — not a `TypedTypedConstant`. The magnitude is not a static decimal but a runtime field reference (`test2`). The parser matches the `'{field} [unit]'` form against the quantity segment-form table (`MatchEmpty + MatchSpaceUnit` → `InterpolationSlotKind.Magnitude`), producing a single slot where `test2` fills the magnitude position and `[lb_av]` is the static unit suffix.

**IDE diagnostic confirms:** The language server emits `NumericOverflow` (PRE0078) on exactly this expression:
```
line 13, char 18–35: "Numeric computation exceeded the representable range on field 'test'"
```

The MCP `precept_compile` tool runs the full pipeline including the ProofEngine (`Compiler.Compile()` calls `ProofEngine.Prove()` and merges proof diagnostics into the returned `Compilation.Diagnostics`). For this specific test input, `precept_compile` reports 0 diagnostics because the interpolated expression hits a code path where the ProofEngine returns `Unbounded` (see §1.5.2) rather than emitting a false positive — the language server's incremental pipeline may evaluate the same obligation differently.

#### 1.5.1 AST Representation

```
InterpolatedTypedConstantExpression
├── Segments:
│   ├── HoleSegment(expr: FieldRef("test2"))  ← magnitude slot
│   └── TextSegment(" [lb_av]")               ← static unit suffix
└── Span: line 14, char 18–35
```

The TypeChecker resolves this to:

```
InterpolatedTypedConstant
├── Slots: [TypedInterpolationSlot(Expression: TypedFieldRef("test2"), SlotKind: Magnitude)]
├── ResultType: Quantity
├── StaticMagnitude: null  ← magnitude is NOT statically known
└── Span: line 14, char 18–35
```

`StaticMagnitude` is `null` because the magnitude comes from a field reference, not a literal. The `TryExtractStaticMagnitude` method in `TypeChecker.Expressions.TypedConstants.cs` (line 894) only extracts a static magnitude when the magnitude position contains a numeric literal in a text segment — not when it's a hole segment.

#### 1.5.2 ProofEngine Failure Path

Two strategies attempt to discharge the `IntervalContainmentProofRequirement` for this expression:

**Strategy 7 — Interval Containment (ProofEngine.Intervals.cs):**
`IntervalOfNarrowed` has no case for `InterpolatedTypedConstant`. It falls through to the `default` branch at line 79, returning `NumericInterval.Unbounded`. Since the interval is unbounded, `TryIntervalContainmentProof` (line 234) returns `false` — proof not discharged.

**Strategy 6 — Compositional Constraint Propagation (ProofEngine.Composition.cs):**
1. `FindInterpolatedAssignments` finds the `InterpolatedTypedConstant` assigned to field `test`.
2. `GetMagnitudeSlotSource` extracts the magnitude slot → `TypedFieldRef("test2")`.
3. `ResolveSourceModifiers` returns `test2`'s modifiers: `[Max, Optional]`.
4. For the `Max` modifier, `Modifiers.GetMeta(Max)` → `ValueModifierMeta` with `ProofSatisfaction.Numeric(LessThanOrEqual, DeclarationValue, SelfValue)`.
5. `SatisfactionCovers` (ProofEngine.Strategies.cs, line 199) checks whether this satisfaction covers the `NumericProofRequirement`.
6. **Critical:** At line 225, `NumericBoundSource.DeclarationValue => null` — "conservative — cannot compare without runtime value." Since the bound source is `DeclarationValue` (meaning "the value declared on the source field"), the strategy cannot resolve it to a concrete number.
7. `boundValue is null` → `return false` (line 228) → satisfaction does not cover → proof not discharged.

**Net result:** Both strategies fail. The obligation remains `Unresolved` → `NumericOverflow` diagnostic fires.

#### 1.5.3 Why the Static-Literal Fix (Slices 14–18) Does NOT Fix This

Slices 14–18 address `TryExtractTypedConstantMagnitude` and `TryGetTypedConstantMagnitude` — these extract magnitudes from `TypedTypedConstant` nodes (static literals like `'6 [lb_av]'`). The interpolated case hits entirely different code paths:

| Path | Static literal (`'6 [lb_av]'`) | Interpolated (`'{test2} [lb_av]'`) |
|------|------|------|
| Parse node | `TypedTypedConstant(ParsedValue: (6m, UcumParsedUnit))` | `InterpolatedTypedConstant(Slots: [...], StaticMagnitude: null)` |
| Interval extraction | `TryGetTypedConstantMagnitude` → `Point(6)` | Falls to `default` → `Unbounded` |
| Compositional | N/A (not interpolated) | `SatisfactionCovers` → `DeclarationValue` → null → fail |

**These are independent problems.** The static-literal normalization fix and the interpolated interval analysis fix are orthogonal. Fixing one does not fix the other.

#### 1.5.4 Correct Behavior for the Interpolated Case

For `'{test2} [lb_av]'` where `test2 as integer max 2 optional`:

1. **The magnitude interval of `test2`:** `(-∞, 2]` (from `max 2`, with no declared `min`). But `test2` is `optional` — if unset, the interpolated expression either produces a quantity with magnitude 0 or fails at runtime (see §7, Q6).

2. **The unit is statically known:** `[lb_av]` appears as a `TextSegment` in the interpolated expression. Its UCUM scale factor is `45359237/100000000` relative to gram.

3. **Correct interval analysis:** If the ProofEngine could:
   - Extract the field interval for `test2` → `(-∞, 2]`
   - Identify the static unit suffix `[lb_av]` from the text segments
   - Scale the interval: `(-∞, 2] × 453.59237 = (-∞, 907.18]` (in grams)
   - Compare against `max '5 kg'` normalized to `5000` grams
   - `907.18 ≤ 5000` → proof discharged ✅

4. **Complication — negative magnitudes:** `test2` has no `min` declared, so it could be negative. A negative quantity magnitude may itself be a semantic error, but the ProofEngine currently doesn't know that. The interval `(-∞, 907.18]` means the max is satisfied, but if a `min` check existed, the negative range could violate it.

5. **Complication — optional field:** If `test2` is unset (null), what magnitude does `'{null} [lb_av]'` produce? This is a runtime semantics question that affects whether the ProofEngine can reason about this path at all. The transition from `offState` does not have a guard requiring `test2 is set`, so the interpolation could execute with an unset field.

---

## 2. PreceptValue Integration Analysis

### 2.1 PreceptValue Today

`PreceptValue` (`src/Precept/Runtime/PreceptValue.cs:14-27`) is an abstract class hierarchy with `FromJson`/`FromClr<T>`/`ToClr<T>`/`ToJson` factory methods — all currently `throw new NotImplementedException()`. It is a stub. The evaluator doc describes the *target* as a 32-byte tagged value struct (`[StructLayout(LayoutKind.Explicit, Size = 32)]`), but the current implementation is a class hierarchy.

**Canonical layout per `evaluator.md` §PreceptValue:**

```
Byte 0:     type discriminant tag
Bytes 8–23: union payload (decimal, long, OR reference region)
Bytes 24–31: reserved / padding
```

The union payload at bytes 8–23 is **not exclusively a decimal region** — it is a three-way union of `decimal`, `long`, and a **reference region** (managed pointer). Collections already use this reference region to point to `PreceptValue[]` backing arrays (`evaluator.md` §7.4.1). The evaluator doc notes that "23 of 32 `TypeKind` members still live in the reference lane" — meaning most types, including business-domain composite types like `quantity`, are expected to use the reference region.

**Corrected finding:** The 32-byte budget *does* accommodate unit information — via the reference region, which can point to a heap object carrying both magnitude and unit (or any composite shape). The exact internal layout is explicitly a **pending implementation decision** per the canonical doc: "The exact internal field layout (tag type, union field offsets, which types use which union region) is a pending implementation decision to be settled before the opcode executor implementation pass." Two viable approaches exist:

1. **Reference-region approach:** The `quantity` tag variant uses the reference region to point to a composite heap object (e.g., `(decimal Magnitude, Unit Unit)`), similar to how collections use the reference region for backing arrays.
2. **Descriptor-metadata approach:** `PreceptValue` stores only the canonical magnitude (decimal payload); unit identity is stored on the `FieldDescriptor` (known at build time from `TypedField.DeclaredQualifiers`). The Builder normalizes incoming values to the declared unit at ingress time.

Both approaches are architecturally valid within the canonical design. The public API always materializes `Quantity` (a `readonly record struct` carrying both magnitude and unit) via `Get<Quantity>()` — the internal storage strategy is invisible to callers (`evaluator.md` Decision 14).

### 2.2 The Runtime Evaluator Today

`Evaluator.cs` (`src/Precept/Runtime/Evaluator.cs`) is a **complete stub**. All operations throw `NotImplementedException`. The evaluator has no constraint evaluation logic, no interval checks, no magnitude comparison code. The entire Fire/Update constraint pipeline documented in `docs/runtime/evaluator.md` §7.1 is design-only — no implementation exists.

**Consequence:** The runtime is not currently broken for the same reason because *the runtime does not exist*. There is no runtime path that compares quantity magnitudes today. The bug is purely in the compile-time pipeline.

### 2.3 Runtime Constraint Evaluation — Future Design

Per `docs/runtime/evaluator.md` §7.1, the evaluator's constraint evaluation uses prebuilt `ConstraintPlanIndex` buckets and `ExecutionPlan` opcode arrays. Min/max constraint enforcement at runtime will be compiled into opcode sequences by the Precept Builder, not re-derived from modifier metadata.

The evaluator doc's Fire pipeline (step 2e) shows constraint evaluation calling `EvaluateFireConstraints` on prebuilt plans. The Builder transforms `min`/`max` modifiers into constraint plans at build time. **The runtime evaluator will never call `TryExtractTypedConstantMagnitude` or `TryGetTypedConstantMagnitude`** — those are compile-time extraction functions. The Builder will have already normalized the bounds.

### 2.4 How Unit Information Flows

```
Parse time:
  '5 kg' → TypedConstantValidation.Validate() → (5m, UcumParsedUnit { Scale: 1/1*10^3 relative to g })
  '6 [lb_av]' → TypedConstantValidation.Validate() → (6m, UcumParsedUnit { Scale: 45359237/100000000 relative to g })

Type-check time:
  TypeChecker stores: DeclaredMax = 5m, DeclaredMaxBoundQualifiers = [Unit("kg")]
  ← magnitude is raw, unit metadata is stored separately as qualifier

Proof time:
  IntervalContainmentProofRequirement.DeclaredMax = 5m  ← raw magnitude, no unit
  IntervalOf('6 [lb_av]') = Point(6m)  ← raw magnitude, no unit

Runtime (future):
  PreceptValue slot holds decimal 6m (raw)
  Constraint plan opcodes compare against bound 5m (raw)
  ← same bug will exist at runtime unless Builder normalizes
```

The existing architecture already parses and stores `UcumParsedUnit` with its `Scale` factor — the UCUM infrastructure is complete. What's missing is using that scale factor to normalize magnitudes before comparison.

---

## 3. Proposed Architecture

### 3.1 Design Principle: Normalize at Extraction, Not at Comparison

The normalization should happen at the point where a typed-constant's magnitude is extracted for numeric comparison — not at every comparison site. This gives us one normalization callsite per extraction path, not N callsites scattered across TypeChecker, ProofEngine, and future Builder.

**Why not normalize the stored magnitude on TypedField?** Because `DeclaredMin`/`DeclaredMax` on `TypedField` are the *declared* values — they represent what the author wrote. Normalizing them would destroy the original declaration, making diagnostics confusing ("your max '5 kg' was exceeded" becomes "your max '5000 g' was exceeded"). We normalize for comparison purposes only.

### 3.2 Component Overview

```
src/Precept/Language/Numeric/
├── NormalizedNumericValue.cs      ← the result type
└── TypedConstantNormalizer.cs     ← the normalizer utility

Consumers:
├── TypeChecker.Validation.Modifiers.cs  (bounds extraction)
├── ProofEngine.Composition.cs           (interval extraction from typed constants)
├── ProofEngine.Intervals.cs             (field bound extraction from DeclaredMin/Max)
└── [Future] Precept Builder             (constraint plan compilation)
```

### 3.3 NormalizedNumericValue

```csharp
// src/Precept/Language/Numeric/NormalizedNumericValue.cs
namespace Precept.Language.Numeric;

/// <summary>
/// A numeric magnitude normalized to a canonical unit within its dimension.
/// Used for compile-time and build-time numeric comparisons only — not stored
/// in PreceptValue slots or serialized to MCP DTOs.
/// </summary>
public readonly record struct NormalizedNumericValue(
    decimal NormalizedMagnitude,     // magnitude expressed in the canonical unit
    decimal OriginalMagnitude,       // magnitude as declared by the author
    UcumExactFactor ConversionFactor // scale from original unit to canonical unit
);
```

**Compile-time only.** This type does not flow into `PreceptValue`. The runtime evaluator will receive pre-normalized bound values in its constraint plans (compiled by the Builder). The `NormalizedNumericValue` is consumed and discarded during analysis — it's a comparison utility, not a storage type.

**Why `UcumExactFactor` and not `decimal`?** `UcumExactFactor` uses `BigInteger` numerator/denominator with a base-10 exponent — it represents the conversion factor with exact rational precision. Converting to `decimal` for the normalized magnitude is a lossy step (128-bit decimal has finite precision). We accept this because Precept's domain is financial arithmetic where `decimal` precision (28-29 significant digits) is more than sufficient for unit conversion.

### 3.4 TypedConstantNormalizer

```csharp
// src/Precept/Language/Numeric/TypedConstantNormalizer.cs
namespace Precept.Language.Numeric;

/// <summary>
/// Normalizes typed-constant numeric values to a canonical unit within their
/// dimension, enabling cross-unit magnitude comparison.
/// </summary>
public static class TypedConstantNormalizer
{
    /// <summary>
    /// Normalizes a quantity typed-constant to its canonical (base SI) unit.
    /// Returns null if the parsed value is not a normalizable quantity.
    /// </summary>
    /// <param name="magnitude">The raw declared magnitude.</param>
    /// <param name="unit">The parsed UCUM unit, or null for plain decimals.</param>
    /// <returns>
    /// A normalized value where NormalizedMagnitude = magnitude × unit.Scale (converted to decimal),
    /// or null if the value cannot be normalized (e.g., no unit, non-numeric).
    /// </returns>
    public static NormalizedNumericValue? NormalizeQuantity(decimal magnitude, UcumParsedUnit? unit)
    {
        if (unit is null)
            return new NormalizedNumericValue(magnitude, magnitude, UcumExactFactor.One);

        var factor = unit.Scale;
        var normalizedMagnitude = ApplyFactor(magnitude, factor);
        return new NormalizedNumericValue(normalizedMagnitude, magnitude, factor);
    }

    /// <summary>
    /// Normalizes a price typed-constant's denominator unit to canonical form.
    /// The monetary numerator is not converted (currencies are not normalizable).
    /// The denominator unit is normalized so price-per-kg and price-per-lb comparisons
    /// yield correct results.
    /// </summary>
    public static NormalizedNumericValue? NormalizePrice(
        decimal magnitude, object? currency, UcumParsedUnit? denominatorUnit)
    {
        if (denominatorUnit is null)
            return new NormalizedNumericValue(magnitude, magnitude, UcumExactFactor.One);

        // For price, the denominator is inverted: price/unit means
        // a larger denominator unit = smaller effective price per base unit.
        // '10 USD/[lb_av]' vs '10 USD/kg':
        //   10 USD/lb ÷ (0.45359237 kg/lb) = 22.046 USD/kg
        var factor = denominatorUnit.Scale;
        var inverseFactor = UcumExactFactor.One.Divide(factor);
        var normalizedMagnitude = ApplyFactor(magnitude, inverseFactor);
        return new NormalizedNumericValue(normalizedMagnitude, magnitude, inverseFactor);
    }

    /// <summary>
    /// Applies a UcumExactFactor to a decimal magnitude.
    /// </summary>
    internal static decimal ApplyFactor(decimal magnitude, UcumExactFactor factor)
    {
        // factor = (Numerator / Denominator) × 10^Base10Exponent
        // Result = magnitude × Numerator / Denominator × 10^Base10Exponent
        //
        // We compute in decimal to stay within Precept's financial-arithmetic precision model.
        // UcumExactFactor stores BigInteger num/denom, which we convert to decimal for the
        // final multiplication. Overflow is possible for extreme unit scales but not for
        // any real-world UCUM unit in Precept's domain (mass, length, volume, time).
        var numDecimal = (decimal)factor.Numerator;
        var denDecimal = (decimal)factor.Denominator;
        var scaled = magnitude * numDecimal / denDecimal;

        if (factor.Base10Exponent > 0)
        {
            for (int i = 0; i < factor.Base10Exponent; i++)
                scaled *= 10m;
        }
        else if (factor.Base10Exponent < 0)
        {
            for (int i = 0; i < -factor.Base10Exponent; i++)
                scaled /= 10m;
        }

        return scaled;
    }
}
```

### 3.5 No TypeMeta.NumericNormalization DU — Correction from Prior Proposal

**Discrepancy with prior proposal:** The earlier session proposed a `TypeMeta.NumericNormalization` discriminated union with `Identity` and `UnitScaled` subtypes on `TypeMeta`. After reading the actual code, this is unnecessary and architecturally wrong.

The normalization decision is not a per-*type* decision encoded in `TypeMeta` — it's a per-*value* decision based on whether the parsed typed-constant carries a `UcumParsedUnit`. The `TypeMeta` catalog already declares `ContentValidation` (which drives parsing) and `RequiredBoundQualifierAxes` (which drives qualifier requirements). The normalization step reads the *parse result* (the tuple), not the *type metadata*. Adding a DU to `TypeMeta` would be metadata for metadata's sake — it doesn't carry information the normalizer doesn't already have from the tuple shape.

The tuple shape already tells us everything:
- `decimal` → plain numeric, no normalization
- `(decimal, UcumParsedUnit?)` → quantity, normalize via unit scale
- `(decimal, object?)` → money, no normalization (currency is not a scale axis)
- `(decimal, object?, UcumParsedUnit?)` → price, normalize via denominator unit scale (inverted)

### 3.6 Consumer Integration: TypeChecker

> ⚠️ **SUPERSEDED BY §0.** The approach below is retained for historical context only. The approved design is §0's "store both" approach: `TypedField` gains `NormalizedDeclaredMin/Max` alongside the existing `DeclaredMin/Max`. This section's approach — overwriting `DeclaredMin/Max` with normalized magnitudes — is incorrect and must NOT be implemented. `DeclaredMin/Max` must remain the original authored values for display. See §0.6 Condition 1 for the resolution.

**Change:** `TryGetComparableTypedConstantValue` (line 410) currently returns `ExtractedBoundValue(magnitude, qualifiers)` where magnitude is raw. After the fix:

```csharp
private static ExtractedBoundValue? TryGetComparableTypedConstantValue(
    string rawText, TypeKind expectedType, ImmutableArray<DeclaredQualifierMeta> declaredQualifiers)
{
    // ... existing parse logic ...
    if (!parseResult.IsValid || !TryExtractTypedConstantMagnitude(parseResult.Value, out var magnitude))
        return null;

    // NEW: Normalize magnitude to canonical unit
    var normalizedMagnitude = NormalizeParsedValueMagnitude(parseResult.Value, magnitude, expectedType);

    var qualifiers = ExtractQualifiersFromParsedValue(expectedType, parseResult.Value)
        ?? ImmutableArray<DeclaredQualifierMeta>.Empty;
    return new ExtractedBoundValue(normalizedMagnitude, qualifiers);
}

private static decimal NormalizeParsedValueMagnitude(
    object? parsedValue, decimal rawMagnitude, TypeKind expectedType) => parsedValue switch
{
    ValueTuple<decimal, UcumParsedUnit?> quantity when quantity.Item2 is { } unit
        => TypedConstantNormalizer.NormalizeQuantity(rawMagnitude, unit)?.NormalizedMagnitude ?? rawMagnitude,
    ValueTuple<decimal, object?, UcumParsedUnit?> price when price.Item3 is { } unit
        => TypedConstantNormalizer.NormalizePrice(rawMagnitude, price.Item2, unit)?.NormalizedMagnitude ?? rawMagnitude,
    _ => rawMagnitude  // money, plain decimal — no normalization
};
```

This means `TypedField.DeclaredMin` / `DeclaredMax` will store **normalized** magnitudes. The `DeclaredMinBoundQualifiers` / `DeclaredMaxBoundQualifiers` still store the *original* qualifier metadata for diagnostic display.

**Wait — this conflicts with §3.1's principle.** Let me reconcile:

The principle "normalize at extraction, not at comparison" means: normalize when extracting the decimal from the typed constant for the purpose of setting the bound. The `DeclaredMin`/`DeclaredMax` values on `TypedField` ARE the bound values used for proof comparison. If we normalize here, we're normalizing once and the ProofEngine's `IntervalContainmentProofRequirement.DeclaredMin/Max` automatically gets normalized values. This is correct — one normalization point, not scattered across consumers.

**Impact:** After this change, `TypedField.DeclaredMax` for `max '5 kg'` would be `5000m` (5 × 1000, since `kg` = 1000 × gram, and gram is the UCUM base unit for mass). For `max '5 [lb_av]'` it would be `~2267.96m` (5 × 453.59237). Both are in grams — the UCUM canonical base unit.

### 3.7 Consumer Integration: ProofEngine

> ⚠️ **SUPERSEDED BY §0.** The approach below is retained for historical context only. `TryGetTypedConstantMagnitude` is NOT modified to normalize inline. The approved design uses a `TryGetStaticUnitFactor` helper + `IntervalOf` post-step (expression-type-scoped, not universal) for all interval scaling, keeping `TryGetTypedConstantMagnitude` as a pure raw-magnitude extractor. See §0's "Consequence for Slices 14–18" table and §0.6 Condition 2 for the resolution.

**Change:** `TryGetTypedConstantMagnitude` (line 231) currently extracts `.Item1` from the tuple. After the fix, it must also normalize:

```csharp
private static bool TryGetTypedConstantMagnitude(object? parsedValue, out decimal value)
{
    switch (parsedValue)
    {
        case decimal direct:
            value = direct;
            return true;
        case int integer:
            value = integer;
            return true;
        case long whole:
            value = whole;
            return true;
        case ValueTuple<decimal, object?> money:
            value = money.Item1;  // money: no normalization (currency is not a scale axis)
            return true;
        case ValueTuple<decimal, UcumParsedUnit?> quantity:
            value = TypedConstantNormalizer.NormalizeQuantity(quantity.Item1, quantity.Item2)
                        ?.NormalizedMagnitude ?? quantity.Item1;
            return true;
        case ValueTuple<decimal, object?, UcumParsedUnit?> price:
            value = TypedConstantNormalizer.NormalizePrice(price.Item1, price.Item2, price.Item3)
                        ?.NormalizedMagnitude ?? price.Item1;
            return true;
        default:
            value = default;
            return false;
    }
}
```

**The same fix applies to `ProofEngine.Intervals.cs`** where `TryExtractNumericLiteralMagnitude` (line 84) has the `ITuple` recursive case:

```csharp
case ITuple tuple when tuple.Length > 0:
    return TryExtractNumericLiteralMagnitude(tuple[0], out magnitude);
```

This recursive unwrap also ignores units. It must be updated to normalize when the tuple shape indicates a quantity or price.

**File:** `src/Precept/Pipeline/ProofEngine.Intervals.cs`

**Change:** `GetFieldBounds` (line 131) reads `field.DeclaredMin` / `field.DeclaredMax` directly. After the TypeChecker fix (§3.6), these values are already normalized. No change needed here — the normalization flows through automatically.

### 3.8 Consumer Integration: Future Precept Builder

When the Precept Builder (Phase 3, D8/R4) compiles min/max constraints into `ConstraintPlan` opcode arrays, it will read `TypedField.DeclaredMin` / `DeclaredMax` (which are now normalized) and encode them as immediate values in constraint opcodes. The evaluator then compares `PreceptValue` slot magnitudes against these pre-normalized bounds.

**Open question (see §7):** This means runtime `PreceptValue` slots for quantities must also store normalized magnitudes, OR the Builder must emit normalization opcodes that convert the slot value before comparison. See §3.9.

### 3.9 Runtime PreceptValue Considerations

Two approaches for the future evaluator:

**Option A: Store normalized magnitudes in PreceptValue slots.**
When `set weight = '6 [lb_av]'` executes at runtime, the evaluator normalizes `6 × 0.45359237 = 2.72 kg-equivalent` and stores `2.72...` in the PreceptValue slot. All comparisons and arithmetic operate on normalized values. Display operations re-convert to the field's declared unit for output.

- Pro: All comparisons are trivially correct — same unit, same scale.
- Pro: Arithmetic between quantities of the same dimension works naturally.
- Con: Requires de-normalization for display (`ToJson`, `ToClr<T>`).
- Con: Breaks the "what you write is what you see" principle.

**Option B: Store original magnitudes; normalize at comparison time.**
PreceptValue stores `6m` (the authored value). Constraint plans include normalization opcodes that convert before comparison. Display is trivial — the stored value IS the display value.

- Pro: Preserves authored values; `ToJson()` returns `6`.
- Pro: No conversion on read.
- Con: Every constraint comparison opcode must include normalization logic.
- Con: Arithmetic between different-unit quantities requires normalization at every operation.

**Recommendation: Option A (normalize at storage) for the runtime.** The evaluator design already establishes that `PreceptValue` carries processed values, not source text. Whether the runtime stores unit information *inside* `PreceptValue` (via the reference region) or *outside* it (on the `FieldDescriptor`) is an open implementation decision — but in either case, the canonical unit is known at build time from `TypedField.DeclaredQualifiers`. Under Option A, the Builder normalizes incoming magnitudes to the canonical unit at ingress time and the evaluator's decimal comparisons are unit-homogeneous by construction. This is consistent with how typed values work in databases: the column declares the unit; the storage holds the canonical magnitude.

The field's `UcumParsedUnit` (and its `Scale`) are known at build time from `TypedField.DeclaredQualifiers`. The Builder can emit the normalization factor as part of the constraint plan or ingress conversion logic. The `Version["fieldName"]` raw-lane API returns `JsonElement` — the Builder can decide whether to return the canonical or original magnitude based on product requirements.

**This is a future decision (Phase 3).** The current bug fix is entirely compile-time and does not depend on the runtime approach.

---

## 4. UCUM Scale Table

### 4.1 Existing Infrastructure

The UCUM scale infrastructure is already complete in the codebase:

| Component | Location | Purpose |
|-----------|----------|---------|
| `UcumExactFactor` | `src/Precept/Language/Ucum/UcumExactFactor.cs` | Exact rational scale factor: `BigInteger` num/denom + base-10 exponent |
| `UcumParsedUnit` | `src/Precept/Language/Ucum/UcumParsedUnit.cs` | Parsed unit with `Scale` field of type `UcumExactFactor` |
| `UcumParser` | `src/Precept/Language/Ucum/UcumParser.cs` | Full UCUM expression parser → `UcumParsedUnit` with computed `Scale` |
| `UcumAtomCatalog` | `src/Precept/Language/Ucum/UcumAtomCatalog.cs` | Registry of UCUM base atoms with scale factors |
| `DimensionVector` | `src/Precept/Language/Ucum/DimensionVector.cs` | 7-element SI dimension vector (length, mass, time, ...) |
| `DimensionCatalog` | `src/Precept/Language/Ucum/DimensionCatalog.cs` | Named dimension aliases |

**No new scale table is needed.** The UCUM parser already computes the exact scale factor for every unit expression. `UcumParsedUnit.Scale` IS the scale table — computed lazily at parse time from the atom catalog's registered factors, prefix multipliers, and exponents.

### 4.2 Scale Factor Examples

The UCUM specification defines all conversion factors relative to base SI units:

| Unit | UCUM Code | Base Unit | Scale Factor |
|------|-----------|-----------|-------------|
| kilogram | `kg` | gram | `1000` |
| pound avoirdupois | `[lb_av]` | gram | `453.59237` |
| meter | `m` | meter | `1` |
| foot | `[ft_i]` | meter | `0.3048` |
| liter | `L` | liter | `1` |
| gallon | `[gal_us]` | liter | `3.785411784` |

The UCUM parser computes these automatically. For `kg`:
- Atom `g` has base scale `1`
- Prefix `k` has factor `10^3`
- Result: `Scale = UcumExactFactor(1, 1, 3)` → 1 × 10³ = 1000

For `[lb_av]`:
- Atom `[lb_av]` has registered scale `45359237/100000000`
- No prefix
- Result: `Scale = UcumExactFactor(45359237, 100000000, 0)` → 0.45359237

### 4.3 Precision Characteristics

`UcumExactFactor` uses `BigInteger` numerator/denominator — exact rational arithmetic with no precision loss during composition. The lossy step is `ApplyFactor` where we multiply a `decimal` magnitude by the factor. `decimal` has 28-29 significant digits — far exceeding the precision needs of any real-world quantity in Precept's domain (financial and business quantities).

**Overflow risk:** `decimal.MaxValue ≈ 7.9 × 10²⁸`. The largest plausible UCUM conversion in Precept's domain is around 10⁶ (e.g., converting metric tons to milligrams). A magnitude of 10²² × 10⁶ = 10²⁸ would overflow. This is not a realistic business scenario. We do not add special overflow handling — if a user declares `max '79228162514264337593543950335 mg'` and assigns in metric tons, they deserve the `OverflowException`.

---

## 5. Migration Path

### 5.1 Implementation Slices

These slices follow the existing interval-proof-engine-design numbering (Slices 1–13 are complete). We continue from Slice 14.

| Slice | Objective | Depends On | Agent |
|-------|-----------|------------|-------|
| **14** | `NormalizedNumericValue` + `TypedConstantNormalizer` | None (new files) | George |
| **15** | Wire TypeChecker bounds extraction to normalizer | Slice 14 | George |
| **16** | Wire ProofEngine magnitude extraction to normalizer | Slice 14 | George |
| **17** | Unit + integration + regression tests | Slices 15–16 | Soup Nazi |
| **18** | Hover/diagnostic display review (ensure diagnostics show original values, not normalized) | Slices 15–16 | Kramer |
| **27** | Doc sync — propagate normalization changes to all canonical docs | Slices 14–21 | Frank |

### 5.2 Slice Details

**Slice 14: Core normalizer types**
- Create `src/Precept/Language/Numeric/NormalizedNumericValue.cs`
- Create `src/Precept/Language/Numeric/TypedConstantNormalizer.cs`
- Unit tests for `NormalizeQuantity`, `NormalizePrice`, `ApplyFactor`
- Test cases: `kg` → identity, `[lb_av]` → 0.45359237×, `mg` → 0.001×, null unit → identity
- **§0.6 Condition 6:** `NumericInterval.Scale` takes `decimal factor`, not `UcumExactFactor`. The `UcumExactFactor → decimal` conversion happens once in `TryGetStaticScalingFactor` (via `ApplyFactor`). By the time the factor reaches `NumericInterval.Scale`, it is already `decimal`. This decouples interval algebra from UCUM types.
- **Test risk (from George's review):** Sentinel-bound scaling (∞ × factor must remain ∞) and inverse-price math are the highest-risk test areas. Ensure `NumericInterval.Scale` on unbounded/half-bounded intervals correctly preserves sentinel values.

**Slice 15: TypeChecker integration**
- Modify `TryGetComparableTypedConstantValue` in `TypeChecker.Validation.Modifiers.cs` to call `TypedConstantNormalizer.NormalizeQuantity` / `NormalizePrice` when the parse result contains a `UcumParsedUnit`
- `ExtractedBoundValue.Magnitude` now holds the normalized magnitude
- `TypedField.DeclaredMin` / `DeclaredMax` carry normalized magnitudes
- **Diagnostic display:** The `BoundsQualifierMismatch` diagnostic and the `NumericOverflow` diagnostic message use `intervalReq.DeclaredMin`/`DeclaredMax` for display. These will now show normalized values. We need to decide whether to carry the original magnitude separately for human-readable messages (see §7, Q1).

**Slice 15b: TypedArg normalization (§0.6 Condition 5)**
- **Decision: Option (a) — add `NormalizedDeclaredMin/Max` to `TypedArg`**, parallel to `TypedField`. This is architecturally consistent with `TypedField` and respects the §0 principle: normalize once at extraction time, store the result, read it everywhere.
- `TypedArg` gains `NormalizedDeclaredMin : decimal?` and `NormalizedDeclaredMax : decimal?`
- The TypeChecker normalizes arg bounds using the same `TryGetComparableTypedConstantValue` path that handles field bounds, called from the arg bound extraction logic in `PopulateEvents`
- `ExtractArgInterval` (`ProofEngine.Intervals.cs:114-129`) updated to read `arg.NormalizedDeclaredMin ?? arg.DeclaredMin` (and same for Max) — normalized when available, raw fallback for non-quantity arg types where NormalizedDeclaredMin is null
- **Tradeoff accepted:** Two extra `decimal?` fields on `TypedArg` — same negligible cost as `TypedField`. Consistency outweighs the minor type width increase.
- **Regression risk (from George's review):** `ProofEngineIntervalIntegrationTests.TypeChecker_QuantityFieldWithTypedConstantBounds_PopulatesDeclaredMaxAndQualifier` and quantity bound qualifier tests will need assertion updates to account for the new normalized fields.

**Slice 16: ProofEngine integration**
- Modify `TryGetTypedConstantMagnitude` in `ProofEngine.Composition.cs` to normalize quantity/price magnitudes
- Modify `TryExtractNumericLiteralMagnitude` in `ProofEngine.Intervals.cs` to normalize when unwrapping tuples
- After this slice, `IntervalOf('6 [lb_av]')` returns `Point(2721.55...)` instead of `Point(6)`

**GetFieldBounds fix (§0.6 Condition 3):**
- `GetFieldBounds` (`ProofEngine.Intervals.cs:131-165`) must be updated to read `field.NormalizedDeclaredMin ?? field.DeclaredMin` (and same for Max) — use normalized when available, fall back to raw for non-quantity types where `NormalizedDeclaredMin` is null.
- This prevents double-normalization: field-ref intervals will be in normalized units, and the `IntervalOf` post-step correctly skips scaling for `TypedFieldRef` (because `TryGetStaticScalingFactor` returns `null` for field refs).

**TryGetStaticNumericValue fix — Strategy 6 fact path (§0.6 Condition 4):**
- `TryGetStaticNumericValue` (`ProofEngine.Composition.cs:221-223`) must normalize `StaticMagnitude` by the static unit when one exists — same `TryGetStaticScalingFactor` helper.
- This applies `scalingFactor * StaticMagnitude` before returning the value as a trusted fact.
- Without this fix, Strategy 6 trusted facts would remain raw while all other comparison paths are normalized — producing wrong cross-unit results when the ProofEngine uses a trusted interpolated constant fact against normalized field bounds.
- **CRITICAL CONSTRAINT (from George's review — B16):** `TryGetStaticNumericValue` normalization is ONLY correct when a static scaling factor exists. Dynamic-unit forms like `'5 {u}'` / `'10 USD/{u}'` MUST NOT fall back to raw `StaticMagnitude` — that would create false proofs (raw magnitude compared against normalized bounds). When `TryGetStaticScalingFactor` returns `null` for a dynamic-unit form, `TryGetStaticNumericValue` must return `false` (no trusted fact available), not the raw magnitude.
- **P1 orthogonality (from George's review — B16):** Interval extraction is orthogonal to the new presence-obligation hole traversal (P1). After P1 ships, unguarded optional holes can still emit PRE0116 even when an interval containment proof succeeds on the same expression. These are independent proof obligations — interval satisfaction does NOT suppress presence requirements. The design must state this explicitly: a successful `IntervalContainment` discharge does not imply presence safety.

**Slice 17: Test coverage**
- **Regression test:** `max '5 kg'` with `set x = '6 [lb_av]'` should NOT emit `NumericOverflow` (6 lb ≈ 2.72 kg < 5 kg)
- **Positive test:** `max '5 kg'` with `set x = '12 [lb_av]'` SHOULD emit `NumericOverflow` (12 lb ≈ 5.44 kg > 5 kg)
- **Same-unit test:** `max '5 kg'` with `set x = '6 kg'` SHOULD emit `NumericOverflow` (unchanged behavior)
- **Price test:** `max '10 USD/kg'` with price in `USD/[lb_av]` — normalized comparison
- **Money test:** `max '100 USD'` with `set x = '200 USD'` — overflow, no normalization (currencies are not convertible)
- **Cross-dimension test:** `max '5 kg'` with `set x = '3 m'` — dimension mismatch caught by existing qualifier check, not normalization
- **WholeValue interpolation (from George's review — G17):** `max '5 kg'` with `set x = '{qtyField}'` where `qtyField` is `quantity of 'mass' max '3 kg'` — verify WholeValue form exercises the correct interval extraction path (no double-normalization, interval from source field's bounds)

**Slice 18: Display review**
- Verify hover expressions show normalized intervals (or decide to show in original units)
- Verify `NumericOverflow` diagnostic message is human-readable with normalized bounds
- May require storing both original and normalized values on `IntervalContainmentProofRequirement`
- **Display contract (from George's review — B18):** Once comparison uses normalized intervals, `ComputedInterval` becomes base-unit output while `DeclaredMin/Max` stay authored. The "store both" approach (§0) provides the raw material, but the display contract must define: (a) diagnostics show `DeclaredMin/Max` (authored values) for user-facing messages; (b) hover/preview shows `ComputedInterval` with unit labels derived from the target field's declared qualifier; (c) MCP DTO projects both raw and normalized. The full display specification is implementation-time work — the architectural constraint is that both values MUST be available at every rendering site.

### 5.3 Interpolated Quantity Slices (Independent Track)

The interpolated case (`'{field} [unit]'`) is a **separate problem** from the static-literal fix. It requires adding interval analysis for `InterpolatedTypedConstant` to the ProofEngine. These slices are independent of Slices 14–18 and can be sequenced after or in parallel.

| Slice | Objective | Depends On | Agent |
|-------|-----------|------------|-------|
| **19** | Add `InterpolatedTypedConstant` case to `IntervalOfNarrowed` | Slice 14 (normalizer) | George |
| **20** | Unit-aware interval scaling for interpolated magnitude slots | Slice 19 + Slice 16 | George |
| **21** | Integration tests for interpolated quantity overflow proofs | Slices 19–20 | Soup Nazi |
| **27** | Doc sync — propagate normalization changes to all canonical docs | Slices 14–21 | Frank |

**Slice 19: Interval analysis for interpolated typed constants**
- Add a case for `InterpolatedTypedConstant` in `IntervalOfNarrowed` (ProofEngine.Intervals.cs)
- When the interpolated expression has a single `Magnitude` slot:
  - Recurse into `IntervalOfNarrowed` on the slot's expression (e.g., `TypedFieldRef("test2")` → `ExtractFieldInterval("test2")`)
  - This produces the magnitude's interval in the source field's unit-system (raw integers for `test2`)
- When the slot is `WholeValue`, recurse on the expression directly (the whole typed value is interpolated)
- For multi-slot expressions (e.g., `'{mag} {unit}'` with both magnitude and unit interpolated), return `Unbounded` (unit is not statically known)
- **Impacts (from George's review — B19):** `Pipeline\SemanticIndex.cs` (`TypedInterpolatedTypedConstant`), `Pipeline\TypeChecker.Expressions.TypedConstants.cs` (`ResolveInterpolatedTypedConstant`)
- **Ordering note (from George's review — B19):** This slice requires a `StaticUnit: UcumParsedUnit?` field on `InterpolatedTypedConstant`. If Slice 22 (static qualifier capture) ships first, `StaticUnit` is subsumed by the richer `StaticQualifier` discriminated union — in that case, Slice 19 should consume `StaticQualifier` rather than adding a separate `StaticUnit` field. Do NOT reshape the node twice. Either: (a) implement 19 first with minimal `StaticUnit`, then Slice 22 widens to `StaticQualifier`; or (b) implement 22 first, then 19 consumes the existing payload.

**Slice 20: Unit-aware interval scaling**
- When the `TextSegment` portions of the interpolated typed constant contain a static unit suffix (e.g., ` [lb_av]`), extract the `UcumParsedUnit` from the text
- Scale the magnitude interval by the unit's UCUM factor: `interval × factor`
- This requires adding an `IntervalScale(NumericInterval interval, UcumExactFactor factor)` method to `NumericInterval`
- The bound comparison (in `IntervalContainmentProofRequirement`) must also use normalized values (per Slice 15/16)
- **Key design question:** How to extract the unit from the text segments? The TypeChecker's segment-form matching already identifies which text segments contain the unit suffix. The `InterpolatedTypedConstant` doesn't currently store the parsed `UcumParsedUnit` for static unit portions — it would need to, or the ProofEngine must re-parse the text segments.
- **CRITICAL CONSTRAINT (from George's review — G20):** The post-step MUST stay expression-type-scoped. Scale ONLY raw magnitude-producing forms: `TypedTypedConstant` with static unit, and `InterpolatedTypedConstant` with `Magnitude` slot + static unit. NEVER scale `TypedFieldRef`, `TypedArgRef`, or `WholeValue` interpolations — these already carry normalized/correct-unit intervals from their source. This is the §0 Condition 2 constraint restated at the slice level.

**Slice 21: Interpolated quantity test coverage**
- `max '5 kg'` with `set x = '{intField} [lb_av]'` where `intField max 2` → no overflow (2 lb ≈ 907g < 5000g)
- `max '5 kg'` with `set x = '{intField} [lb_av]'` where `intField max 15` → overflow (15 lb ≈ 6804g > 5000g)
- `max '5 kg'` with `set x = '{intField} [lb_av]'` where `intField` has no max → unbounded → overflow fires (conservative)
- `max '5 kg'` with `set x = '{field}'` where field is a quantity (WholeValue slot) → interval from field's bounds
- Interpolated with both magnitude and unit as holes → unbounded (cannot determine unit statically)
- **Conservative-case tests (from George's review — G21):** Explicitly test that dynamic-unit holes produce unproved/unbounded results:
  - `max '5 kg'` with `set x = '3 {unitField}'` → unbounded (dynamic unit, cannot normalize) → overflow fires conservatively
  - `max '10 USD/kg'` with `set x = '5 USD/{unitField}'` → unbounded (dynamic denominator) → overflow fires conservatively
  - Verify these do NOT produce false proofs (must not compare raw magnitude against normalized bounds)

#### Slice 27: Doc sync

**Objective:** Propagate all quantity normalization changes to canonical documentation surfaces in the same PR as Slice 21.

> **Shape-freeze dependency (from George's review — G27):** This slice gates on Slices 16 and 18 settling the final `IntervalContainmentProofRequirement` shape and display contract. Do not begin doc sync until the proof-requirement record shape and diagnostic display format are frozen. Premature doc sync creates re-work when shapes change.

**Files to modify:**

1. `docs/language/precept-language-spec.md` — §0.6 Proof Engine Design Contract, §5 Proof Engine
2. `docs/compiler/proof-engine.md` — §3.2 Obligation Collection (`IntervalContainmentProofRequirement` record), interval source table (§2.2)
3. `docs/Working/interval-proof-engine-design.md` — Implementation Tracker, §2.2 interval source table, §2.3 `IntervalContainmentProofRequirement` record, §3.2 obligation parameters
4. `docs/runtime/runtime-api.md` — Shared Types section (no `TypedField`/`TypedEventArg` documented today — but the three-layer enforcement model from §0.5 needs a home here)
5. `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` — `CompileProofObligationDto` DTO shape (code change, not doc — but affects MCP contract surface)
6. `tools/Precept.Mcp/Tools/CompileTool.cs` — projection of normalized bounds in proof obligation output

**Changes required per file:**

**1. `docs/language/precept-language-spec.md`**

- **§0.6 Proof Engine Design Contract** (line 195): Add a bullet to the proof engine contract list: "5. **Unit-aware numeric interval reasoning.** When field bounds and assignment expressions use different units within the same dimension (e.g., `max '5 kg'` with `set x = '6 [lb_av]'`), the proof engine normalizes all magnitudes to base-unit equivalents via UCUM scale factors before interval containment comparison. Normalization is applied once at the TypeChecker extraction boundary; downstream consumers read pre-normalized values." This makes the cross-unit comparison guarantee part of the language contract.
- **§5 Proof Engine** (line 1912): Add a paragraph after the existing qualifier-oriented checks list (line 1924) noting the interval containment checks now include unit-aware normalization: "Interval containment proofs for quantity and price fields normalize all magnitudes to UCUM base units before comparison, preventing false-positive `NumericOverflow` diagnostics on cross-unit assignments (e.g., `max '5 kg'` with `set field = '6 [lb_av]'` is correctly proved safe because 6 lb ≈ 2.72 kg < 5 kg)."
- **§3.6 Typed constant interpolation** (line 1452): No change needed — the existing description covers the syntax and type-checking of `{expr}` inside `'...'`. Normalization is a proof concern, not a type-checking description concern. Clean.
- **Modifier validation table** (line 1584): No change needed — `min > max` validation fires on authored values before normalization. The existing description remains correct. Clean.

**2. `docs/compiler/proof-engine.md`**

- **§3.2 Obligation Collection** (near line 155): Update the `IntervalContainmentProofRequirement` record definition to show `NormalizedDeclaredMin/Max` fields or clarify that `DeclaredMin/Max` now carry normalized values for quantity/price types. Add a note: "For quantity and price fields, `DeclaredMin` and `DeclaredMax` are UCUM-normalized to base-unit magnitudes by the TypeChecker at extraction time. Raw authored magnitudes are preserved separately on `TypedField.DeclaredMin/Max` for diagnostic display."
- **Interval source table** (if present, or add): Note that `TypedTypedConstant` (quantity/price) intervals are now normalized via `TryGetStaticScalingFactor` post-step, and `TypedFieldRef` intervals read `NormalizedDeclaredMin ?? DeclaredMin`.
- **Strategy 6 section** (line 1310): Note that `TryGetStaticNumericValue` normalizes `StaticMagnitude` by the static unit's UCUM factor before returning a trusted fact. This ensures Strategy 6 facts are in the same unit system as normalized field bounds.

**3. `docs/Working/interval-proof-engine-design.md`**

- **Implementation Tracker table** (line 13): No row additions needed (Slices 14–21 are tracked in the quantity-normalization-design doc, not here). However, add a note at the bottom of the tracker: "Slices 14–21 (unit-aware quantity normalization) extend the interval proof strategy. See `docs/Working/quantity-normalization-design.md`."
- **§2.2 Interval source table** (line 89): Add a clarifying note: "For quantity/price typed constants and field refs, magnitudes are UCUM-normalized to base units. See quantity-normalization-design.md §0/§3 for the normalization architecture."
- **§2.3 `IntervalContainmentProofRequirement`** (line 159): Add a note that `DeclaredMin`/`DeclaredMax` may be UCUM-normalized values (for quantity/price fields) rather than raw authored magnitudes, and that the raw values are preserved on `TypedField.DeclaredMin/Max` for display.
- **§3.2 Obligation parameters** (line 162): Same — annotate that these bounds are normalized for quantity/price types.

**4. `docs/runtime/runtime-api.md`**

- **New section: Three-Layer Enforcement Model** — add after "Correctness Invariant" (line 435) or as a subsection of "Design Rationale and Decisions". Content from §0.5 of the normalization design: Layer 1 (compile-time diagnostics, 132 codes), Layer 2 (ingress validation via `TypeRuntimeMeta`/`TypeRuntime`), Layer 3 (defense-in-depth evaluator faults, 15 `[StaticallyPreventable]` codes). This is a named architectural concept that the normalization design identified as needing a canonical home.
- **`TypedField` / `TypedEventArg` are NOT documented in runtime-api.md** — they are compiler-internal types, not runtime API types. No change needed for these types here. Clean.

**5. `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`** (code, not doc)

- `CompileProofObligationDto` currently projects `DeclaredMin`/`DeclaredMax` from `IntervalContainmentProofRequirement`. After normalization, these values will be normalized for quantity/price. Add `NormalizedDeclaredMin`/`NormalizedDeclaredMax` fields OR document that the existing `DeclaredMin`/`DeclaredMax` fields now carry normalized values. Decision: project both raw and normalized — add `NormalizedDeclaredMin : decimal?` and `NormalizedDeclaredMax : decimal?` to the DTO so MCP consumers can see both.

**6. `tools/Precept.Mcp/Tools/CompileTool.cs`** (code, not doc)

- Update the `ToDto` projection to populate `NormalizedDeclaredMin`/`NormalizedDeclaredMax` from the proof obligation. This is a thin DTO projection change.

**No changes needed (confirmed clean):**

- **`docs/language/catalog-system.md`** — No changes needed. The `Types` catalog entries for `quantity`, `money`, `price` describe type metadata (qualifier systems, orderable, content validation), not normalization behavior. Normalization is a proof-engine/TypeChecker concern, not a catalog concern. The catalog does not describe how magnitudes are compared — it describes what the types *are*. Clean.
- **`README.md`** — No changes needed. `README.md` contains no quantity/unit claims, no `TypedField` references, no overflow/normalization content. It describes the project at a high level. Clean.
- **`samples/`** — No sample changes needed. `samples/Test.precept` uses `'{test2} [lb_av]'` with `max '5 kg'` — this is the exact bug scenario that normalization fixes. After normalization ships, this sample will compile without false-positive `NumericOverflow`. The sample file itself doesn't need modification — the fix is in the compiler, and the sample already demonstrates the correct intended usage. `samples/inventory-item.precept` uses `[lb_av]` but has no cross-unit `min`/`max` bounds. Clean.
- **`docs/philosophy.md`** — No changes needed. Normalization does not change the category of entities Precept governs, the core guarantee, the positioning, or the constraint model. It fixes a false-positive in the existing proof system. Clean.
- **`docs/mcp/`** — Directory does not exist. MCP documentation is inline in the MCP tool source and `docs/Working/interval-proof-engine-design.md` Slice 6 notes. The MCP DTO change (items 5–6 above) is the only MCP surface affected.

**Test coverage required:**

None — this is a pure documentation slice (items 1–4). Items 5–6 are code changes to MCP DTOs that should be covered by existing MCP compile-tool tests after the DTO fields are added.

**Dependencies:** Slices 14–21 (implementation must exist before docs can accurately describe it)

**Owner:** Frank (Lead) — doc sync is a design-gate concern

### 5.4 Interpolated Types — Exhaustive Gap Audit

`TypeChecker.Expressions.TypedConstants.cs` supports interpolated typed constants for **money, quantity, price, exchange rate, currency, unit of measure, dimension, duration, and period**. The slot vocabulary is `Magnitude`, `Currency`, `Unit`, `FromCurrency`, `ToCurrency`, `WholeValue`, `NumeratorUnit`, and `DenominatorUnit`. Every one of those families resolves to the same semantic node: `InterpolatedTypedConstant`.

The key implementation fact is that `InterpolatedTypedConstant` currently retains only `Slots` plus `StaticMagnitude`. It **does not retain static qualifier-bearing text** (`USD`, `kg`, `USD/kg`, `USD/EUR`) and it does **not** expose qualifier identity for `WholeValue` holes. That makes Slices 19–21 a correct fix for the specific quantity-magnitude/static-unit overflow bug, but **not** an exhaustive fix for interpolated typed constants as a whole.

| Form | Covered by Slices 19–21? | Current behavior if not | Priority |
|---|---|---|---|
| Quantity `set`: magnitude hole + static unit text (`'{n} [lb_av]'`) | **Yes** | Today `IntervalOfNarrowed` falls through to `Unbounded`; bounded `set` emits false-positive `PRE0078`. | **HIGH** |
| Quantity `set`: whole-value hole (`'{q}'`) | **Yes (implicit in Slice 19)** | Same false-positive `PRE0078` today; there is no current `InterpolatedTypedConstant` interval path. | **HIGH** |
| Quantity `set`: dynamic unit hole(s) (`'{n} {u}'`, `'{n} {a}/{b}'`, `'0 {a}/{b}'`, `'0 {a}/each'`, `'0 each/{b}'`) | **Partially** — Slice 19 already says multi-slot stays `Unbounded` | Current engine also falls through to `Unbounded` and emits `PRE0078`; conservative when the unit is runtime-dynamic, but still imprecise. | **MEDIUM** |
| Money `set`: static currency or whole-value (`'{n} USD'`, `'{m}'`) | **No** | Bounded `set` emits false-positive `PRE0078`. Static-currency mismatch on `set`/`default` is silently accepted because static text and whole-value qualifiers are lost. | **HIGH** |
| Price `set`: static currency+unit or whole-value (`'{n} USD/kg'`, `'{p}'`) | **No** | Bounded `set` emits false-positive `PRE0078`. Static currency/unit mismatch on `set`/`default` is silently accepted. | **HIGH** |
| Price `set`: dynamic currency and/or unit holes (`'{n} {c}/{u}'`, `'0 {c}/{u}'`) | **No** | No interval path today; bounded `set` emits `PRE0078`. This should be an explicit conservative `Unbounded` path when the unit is runtime-dynamic, but the current slices do not say so. | **MEDIUM** |
| Rules / ensures with static-text or whole-value quantity / money / price constants (`q > '{n} kg'`, `m > '{other}'`, `p > '{n} USD/kg'`) | **No** | `ResolveQualifierFromInterpolatedConstant` only inspects slots. Static-text qualifiers and whole-value source qualifiers resolve as `unresolved`, so definite same-qualifier cases raise false-positive `PRE0114`. | **HIGH** |
| Field defaults with interpolated quantity / money / price constants | **No** | No interval-containment obligation is generated for defaults. Initial-state constant folding treats `InterpolatedTypedConstant` as `UnknownSentinel`, so guaranteed-bad defaults can compile clean. | **HIGH** |
| Event arg defaults with interpolated typed constants | **No** — broader prerequisite gap | `TypedArg.DefaultExpression` is never resolved today. Even obviously invalid interpolated defaults currently compile clean. | **HIGH** |
| Exchange rate / currency / unitofmeasure / dimension / duration / period interpolation | **Not meaningfully** | These families are supported syntactically, but they do not participate in the current quantity-bound proof path (`max` is inapplicable to exchange rate / duration; the others are non-numeric). Slices 19–21 do not help them. | **LOW** |

#### Gap A — Static qualifier capture is missing, and it is the largest uncovered surface

This is the highest-signal gap from the audit.

Today the semantic node keeps only slot expressions plus `StaticMagnitude`. That means all of the following are invisible downstream even when they are statically known:

- `'{n} USD'` → static currency
- `'{n} kg'` / `'{n} [lb_av]'` → static unit
- `'{n} USD/kg'` → static currency + denominator unit
- `'{n} USD/EUR'` → static from/to currencies
- `'{moneyField}'` / `'{qtyField}'` / `'{priceField}'` → whole-value source qualifiers

That single omission causes three different failure modes:

1. **False-positive qualifier diagnostics in rules/ensures** (`PRE0114`) for definite same-qualifier forms.
2. **Silent acceptance of definite qualifier mismatches** in `set` actions and field defaults (`quantity in 'm' default '{n} kg'`, `money in 'EUR' = '{n} USD'`, `price in 'EUR'/m = '{n} USD/kg'`).
3. **No reusable static metadata for non-quantity interval work** (money / price).

**Needed fix:** a new semantic payload on `InterpolatedTypedConstant` that carries resolved static qualifier data, not just `StaticMagnitude`. That payload must be consumable by both `ResolveQualifierFromInterpolatedConstant` and `ValidateAssignmentQualifiers`, and it must also expose whole-value source qualifiers by delegating to the hole expression when the form is `WholeValue`.

#### Gap B — Money and price are not covered at all by the interval slices

Money was intentionally excluded from **normalization**, but not from **interval extraction**. Those are different questions.

- **Money** still needs an interpolated interval path for `WholeValue` and `Magnitude` forms. No conversion is required; the engine just needs to recurse into the magnitude / whole-value source once qualifier compatibility is established.
- **Price** needs both the interval path and its own normalization rule. Price is structurally different from quantity: the unit lives in the denominator, so normalization is inverse-factor scaling. `'{n} USD/kg'` is not covered by the quantity slices.

Without that work, bounded money / price fields continue to raise false-positive `PRE0078` for interpolated assignments that should be statically provable.

#### Gap C — Defaults are outside the current proof path

Slices 19–21 only address the `set`-action `IntervalContainmentProofRequirement` path.

They do **not** touch:

- field defaults, where no interval-containment obligation is generated; or
- initial-state satisfiability, where `FoldValue` currently has no `InterpolatedTypedConstant` case and therefore degrades to `UnknownSentinel`.

That makes the current behavior unsafely permissive for defaults. A concrete repro from this audit was:

```precept
precept DefaultQ
field n as integer default 10
field q as quantity in 'kg' max '5 kg' default '{n} kg'
state Draft initial
in Draft ensure q <= '5 kg' because "q too large"
```

This compiles without the expected bound violation because the interpolated default is never interval-checked and the initial-state ensure cannot be folded.

#### Gap D — Event arg defaults are a broader prerequisite, not a Slice 19–21 extension

Interpolated arg defaults are not merely uncovered by the quantity design — **arg defaults are not wired at all** in the current checker. `TypedArg.DefaultExpression` is always left `null`, so even obviously invalid defaults like `default '{"oops"} kg'` compile clean.

This needs to be recorded because it blocks any claim of exhaustive interpolated-default coverage, but it should be treated as a broader event-arg-default implementation slice rather than folded into the quantity-only interval batch.

#### Explicitly safe exclusion — runtime-dynamic unit / currency forms

Some forms really should remain conservative:

- `'{n} {u}'`
- `'{n} {a}/{b}'`
- `'{n} {c}/{u}'`

When the unit or currency identity comes from a hole, the engine cannot normalize against a field bound using static information alone. For those forms, the correct compile-time answer is an explicit **`Unbounded` / not proved** path, not a guessed conversion. The current design already hints at this for quantity multi-slot forms; the audit just makes the same rule explicit for price (and for any future numeric family with dynamic qualifier-bearing holes).

#### Updated slice table

| Slice | Objective | Notes |
|---|---|---|
| **22** | Capture static interpolated qualifier metadata | Extend `InterpolatedTypedConstant` so static text contributes resolved qualifier payload (currency / unit / from-currency / to-currency) instead of being discarded after form matching. |
| **23** | Route interpolated qualifier metadata through all qualifier consumers | Update `ResolveQualifierFromInterpolatedConstant` **and** `ValidateAssignmentQualifiers` so rules/ensures stop raising false-positive `PRE0114`, and `set` / `default` stop silently accepting definite mismatches. |
| **24** | Extend interpolated interval extraction beyond quantity | Add money whole-value / magnitude paths and price whole-value / magnitude paths. Price must use denominator-aware normalization; dynamic qualifier-bearing holes stay conservative `Unbounded`. |
| **25** | Add field-default proof coverage for interpolated typed constants | Either generate default-time interval containment obligations or teach initial-state/default analysis enough about interpolated typed constants to reject guaranteed-bad defaults. |
| **26** | Event arg default resolution (companion prerequisite) | Broader than quantity normalization, but required before any claim of exhaustive interpolated-default coverage is true. |

**Bottom line:** Slices 19–21 are the right fix for the exact `'{test2} [lb_av]'` bug and its quantity whole-value neighbor, but they are **not** an exhaustive interpolated-typed-constant design. The missing surfaces are static qualifier capture, money / price interval handling, field-default proof coverage, and the broader event-arg-default hole.

---

## 6. Risks and Tradeoffs

### 6.1 Precision: decimal vs double for scale factors

**Decision: decimal.** `UcumExactFactor` → `decimal` conversion is the lossy step. `double` would give only ~15-16 significant digits; `decimal` gives 28-29. For financial and business quantities, `decimal` is the correct choice. The `UcumExactFactor.Numerator/Denominator` are `BigInteger` — the exact factor is preserved until the final multiplication.

### 6.2 Price denominator inversion

Price `'10 USD/kg'` means "10 USD per kilogram." The denominator is `kg`. When comparing against `'8 USD/[lb_av]'` (8 USD per pound), we must normalize the denominator: `8 USD / 0.45359237 kg = 17.64 USD/kg`. This is an inverted normalization — larger denominator unit → larger normalized price. `NormalizePrice` handles this with `inverseFactor = 1 / unit.Scale`.

### 6.3 Money currency mismatch

**Non-negotiable: currencies are NOT normalized.** `'100 USD'` and `'100 EUR'` are type errors, not conversion opportunities. The existing `BoundsQualifierMismatch` (PRE0134) already catches this. The normalizer returns the raw magnitude for money types.

### 6.4 Impact on MCP DTOs

`NormalizedNumericValue` is an internal comparison utility — it is NOT serialized in `precept_compile` output. The existing `proofObligations` projection in `precept_compile` shows `DeclaredMin`/`DeclaredMax` — these will now be normalized magnitudes. If MCP consumers expect to see the original authored values (e.g., `"declaredMax": 5` for `max '5 kg'`), we need to carry both original and normalized values. This is a display concern, not a correctness concern.

### 6.5 Backward compatibility: test assertion changes

Tests that assert `DeclaredMin` / `DeclaredMax` values on `TypedField` will break. For example, `ProofEngineIntervalIntegrationTests.TypeChecker_QuantityFieldWithTypedConstantBounds_PopulatesDeclaredMaxAndQualifier` (test file line ~77-91) likely asserts `DeclaredMax = 5` for `max '5 kg'`. After normalization, this becomes `DeclaredMax = 5000` (5 × 1000, since kg = 1000 g). All such tests must be updated.

### 6.6 Canonical unit as the normalization target

The UCUM parser normalizes to base SI units (gram, meter, second, etc. — not kilogram). This means `'5 kg'` normalizes to `5000` (grams), not `5`. This is mathematically correct but may surprise developers who expect kg-centric values. The normalization target is NOT configurable — it's determined by the UCUM specification's atom definitions. Consistency across all unit expressions depends on this being deterministic.

---

## 7. Open Questions for Shane

### Q1: Diagnostic display — original vs. normalized magnitudes — **LOCKED 2026-05-14**

> **STATUS: LOCKED.** Shane confirmed Option A this session (2026-05-14). This also resolves B18 — the display specification is no longer deferred.

**Locked decision:** Diagnostic display for `NumericOverflow` and related diagnostics must de-normalize computed intervals back to the field's declared unit. The locked format is:

```
error PRE0078: Numeric computation exceeded the representable range on field 'weight'
[−∞ .. 5 kg] (computed: [6 [lb_av] .. 6 [lb_av]])
```

Where:
- The primary display shows the interval in the **field's declared unit** (de-normalized from base UCUM units).
- The `(computed: ...)` parenthetical shows the raw normalized values for transparency — preserving what the author wrote alongside the declared-unit view.

**Implementation requirements:**
- `IntervalContainmentProofRequirement` must carry the target field's declared qualifier alongside the normalized bounds.
- The diagnostic renderer must include a de-normalization step to convert normalized bounds back to the declared unit before display.

**Applies to:** Slice 18 implementation, diagnostic renderer, `IntervalContainmentProofRequirement` shape.

*(Original options for reference: (a) declared unit with de-normalization, (b) raw base-unit values, (c) authored values with declared-unit primary. Option A was selected.)*

### Q2: Should DeclaredMin/DeclaredMax on TypedField store original or normalized magnitudes?

> ⚠️ **SUPERSEDED BY §0.** This question is closed. §0's "store both" approach is the approved design: `DeclaredMin/Max` retain the original authored magnitudes (for display); `NormalizedDeclaredMin/Max` carry pre-computed base-unit magnitudes (for proof comparison). Neither Option A below ("overwrite with normalized") nor Option B below ("normalize at proof time") is correct — both are superseded. See §0.6 Condition 1 for the resolution.

**(A) Store normalized** (current proposal): `DeclaredMax = 5000` for `max '5 kg'`. ProofEngine gets correct values automatically. Display needs de-normalization.

**(B) Store original, normalize at proof time**: `DeclaredMax = 5` for `max '5 kg'`. ProofEngine must normalize when creating `IntervalContainmentProofRequirement`. More work at proof time, but preserves authored values throughout the semantic model.

**Recommendation:** Option (B) — store original, normalize at proof time. This keeps the semantic model faithful to what the author declared and isolates normalization to the comparison callsites (ProofEngine obligation creation + ProofEngine interval extraction from typed constants). It also avoids the diagnostic display problem from Q1 — diagnostics can show the original values.

**This contradicts §3.6's implementation.** If Shane agrees with Option B, §3.6 changes: `TryGetComparableTypedConstantValue` stays unchanged, and the normalization moves to `IntervalContainmentProofRequirement` creation and `IntervalOf` typed-constant handling.

### Q3: Scope of this fix — compile-time only or include Builder/runtime design?

The current bug is compile-time only (evaluator is a stub). Should Slices 14–18 include runtime Builder normalization design, or should that be deferred to the Builder implementation (Phase 3, D8/R4)?

**Recommendation:** Compile-time fix only (Slices 14–18). The Builder normalization is a Phase 3 concern — it's blocked on the evaluator/executable-model design (D8/R4) which does not exist yet. Document the runtime implications here, implement them when the Builder ships.

### Q4: Should the normalizer live in `src/Precept/Language/Numeric/` or `src/Precept/Language/Ucum/`?

The normalizer depends on `UcumParsedUnit` and `UcumExactFactor`, both in `Precept.Language`. Two options:

**(A) `Language/Numeric/`** — new namespace, emphasizes that normalization is a numeric concern that applies to quantity/price, not a UCUM concern.

**(B) `Language/Ucum/`** — colocated with the types it depends on, but muddies the UCUM parser's responsibility boundary (parsing vs. normalization).

**Recommendation:** Option (A) — `Language/Numeric/`. The normalizer is a consumer of UCUM parse results, not part of the UCUM parser itself.

### Q5: For interpolated quantities, should the ProofEngine scale field intervals by the template's unit factor?

The interpolated expression `'{test2} [lb_av]'` has a magnitude interval determined by `test2`'s bounds and a unit determined by the static text segment `[lb_av]`. To discharge the proof, the ProofEngine must:
1. Compute `test2`'s interval → `(-∞, 2]` (from `max 2`)
2. Scale by `[lb_av]`'s UCUM factor → `(-∞, 907.18]` (in grams)
3. Compare against `max '5 kg'` normalized → `5000` grams

**This requires `InterpolatedTypedConstant` to carry the parsed `UcumParsedUnit` for static unit text segments.** Currently it stores only `TypedInterpolationSlot[]` — the static text portions are discarded after segment-form matching. The TypeChecker's `ResolveInterpolatedTypedConstant` would need to extract and store the `UcumParsedUnit` from the static text when a quantity's magnitude-only slot is matched.

**Recommendation:** Yes — when the unit is statically known (text segment, not a hole), the ProofEngine should scale the magnitude interval. This requires a small extension to `InterpolatedTypedConstant` to carry the resolved `UcumParsedUnit?` for static unit portions. See §5.3, Slices 19–20.

### Q6: What is the null/unset semantics for `'{test2} [lb_av]'` when test2 is not set?

`test2` is `optional` — it may be unset when the transition fires. In `from offState on toggle → set test = '{test2} [lb_av]'`, there is no guard requiring `test2 is set`. If `test2` is null:

**(A) Runtime fault:** The interpolation attempts to coerce null to a magnitude, producing a runtime fault (analogous to null-reference). This is a `PresenceViolation` — the ProofEngine should emit `UnprovedPresenceRequirement` (or similar) for unguarded use of optional fields in interpolated expressions.

**(B) Default to zero:** `'{null} [lb_av]'` → `'0 [lb_av]'`. This is a silent coercion with surprising behavior.

**(C) Existing behavior:** What does the language server actually do today? The absence of a diagnostic beyond `NumericOverflow` suggests that either the TypeChecker doesn't check for unguarded optional access inside interpolation holes, or it already suppresses it via some other path.

**Recommendation:** This is a separate presence-proof question, not a normalization question. The normalization design should document it as a prerequisite but not solve it. The ProofEngine's `PresenceProofRequirement` generation would need to cover interpolation hole expressions — this is orthogonal to the numeric interval work.

---

## Appendix A: Cross-Reference Table

| Document | Relationship |
|----------|-------------|
| `docs/Working/interval-proof-engine-design.md` | Parent design — this document extends Slice 9 (typed-constant bound extraction) with unit normalization |
| `docs/runtime/evaluator.md` | Future consumer — Builder normalization for runtime constraint plans |
| `docs/runtime/runtime-api.md` | PreceptValue API surface — impacted by normalization storage decision |
| `docs/language/catalog-system.md` | TypeMeta catalog — no changes needed (§3.5) |
| `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` | Defect site 1 — bounds extraction |
| `src/Precept/Pipeline/ProofEngine.Composition.cs` | Defect site 2 — interval extraction |
| `src/Precept/Pipeline/ProofEngine.Intervals.cs` | Defect site 2b — field bounds & literal unwrap |
| `src/Precept/Language/Ucum/UcumExactFactor.cs` | Scale factor arithmetic |
| `src/Precept/Language/Ucum/UcumParsedUnit.cs` | Parsed unit with Scale field |

## Appendix B: Worked Example

**Input:**
```precept
precept Shipping
field weight as quantity in 'kg' max '5 kg'
state Active initial
event Load
from Active on Load
  -> set weight = '6 [lb_av]'
  -> no transition
```

**Before fix (current behavior):**
1. TypeChecker: `max '5 kg'` → `DeclaredMax = 5m` (raw)
2. TypeChecker: `'6 [lb_av]'` → parsed as `(6m, UcumParsedUnit { Scale: 45359237/100000000 })`
3. ProofEngine: `IntervalOf('6 [lb_av]')` → `Point(6)` (raw magnitude)
4. ProofEngine: containment check `6 ≤ 5` → false → `NumericOverflow` ERROR ❌

**After fix (with Q2 Option B — normalize at proof time):**
1. TypeChecker: `max '5 kg'` → `DeclaredMax = 5m` (original, stored as authored)
2. TypeChecker: `'6 [lb_av]'` → parsed as `(6m, UcumParsedUnit { Scale: 45359237/100000000 })`
3. ProofEngine obligation creation: normalize `DeclaredMax`: `5 × 1000 = 5000` (5 kg in grams)
4. ProofEngine: `IntervalOf('6 [lb_av]')` → normalize: `6 × 453.59237 = 2721.55` → `Point(2721.55)`
5. ProofEngine: containment check `2721.55 ≤ 5000` → true → proof discharged ✅

---

## §5.5 — Lead Architectural Review: Interpolated Forms Exhaustive Pass

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Scope:** Independent architectural review of the full normalization design (§0–§0.5, Slices 14–22) with exhaustive coverage of interpolated typed-constant forms, proof obligation interactions, and gap analysis.

---

### §5.5.1 — Design Coverage Assessment

#### Slice 14: Core normalizer types

**Status: Complete with one revision needed.**

The `TypedConstantNormalizer` static utility in `Language/Numeric/` is well-placed — it respects the dependency graph (`Language/` visible to both `Pipeline/` and `Runtime/`). The API surface (`Normalize`, `NormalizePrice`, `Denormalize`, `ApplyFactor`) covers all three type families (quantity, price, money-excluded).

**Gap:** §0 revises Slice 14 to drop `NormalizedNumericValue` in favor of bare `decimal`, but §3.3 still defines `NormalizedNumericValue` with `OriginalMagnitude` and `ConversionFactor` fields. The design must be reconciled — §0's simplification (bare `decimal`) is correct. `NormalizedNumericValue` carries two fields (`OriginalMagnitude`, `ConversionFactor`) that no consumer ever reads after the normalized magnitude is stored. Drop it. The §0 revision takes precedence.

**Gap:** `NumericInterval.Scale(UcumExactFactor)` is proposed but Q8 (§0 Open Questions) asks whether it should take `UcumExactFactor` or `decimal`. This should be `decimal` — the interval bounds are already `decimal`, and `UcumExactFactor → decimal` conversion is done once by `ApplyFactor`. Passing `UcumExactFactor` through to `NumericInterval` couples the interval algebra to UCUM types unnecessarily.

#### Slice 15: TypeChecker bounds extraction

**Status: Internally contradictory — must pick one approach.**

§0 says: "Store BOTH original AND normalized — add `NormalizedDeclaredMin/Max` to `TypedField`." §3.6 says: "After this change, `TypedField.DeclaredMax` for `max '5 kg'` would be `5000m`." §7 Q2 says: "Recommendation: Option B — store original, normalize at proof time."

These are three mutually exclusive designs for the same slice. The design must converge.

**Architectural recommendation:** §0's "store both" approach is correct. It has the cleanest properties:
- `DeclaredMin/Max` preserves authored values for diagnostics and display.
- `NormalizedDeclaredMin/Max` pre-computes for ProofEngine consumption.
- The TypeChecker normalizes once; no downstream consumer re-derives.

This means §3.6's "overwrite DeclaredMax with normalized" and §7 Q2's "normalize at proof time" are both superseded by §0. The design doc should be explicit about this.

**Impact on obligation generation:** The `DynamicObligationGenerator` that creates `IntervalContainmentProofRequirement` currently reads `TypedField.DeclaredMin/DeclaredMax`. After §0's revision, it must read `NormalizedDeclaredMin/Max` for the bounds used in containment comparison, and retain `DeclaredMin/Max` for diagnostic display. The current `IntervalContainmentProofRequirement` record (file: `src/Precept/Language/ProofRequirement.cs:135`) has only `DeclaredMin/DeclaredMax`. It needs `NormalizedMin/Max` fields added, OR the ProofEngine must resolve normalized bounds from `TypedField` via `TargetField` lookup. The design leaves this underspecified.

#### Slice 16: ProofEngine magnitude extraction

**Status: Design contradiction between §0 and §3.7.**

§0 says: "`TryGetTypedConstantMagnitude` does NOT need to normalize. It returns the raw magnitude. Normalization happens in the wrapping `IntervalOf` via `TryGetStaticUnit`." §3.7 shows `TryGetTypedConstantMagnitude` being modified to normalize quantities and prices inline.

**Architectural recommendation:** §0's universal post-step is the superior design. Reasons:
1. Single normalization site in the interval path (`IntervalOf` post-step) vs. scattered normalization in `TryGetTypedConstantMagnitude`.
2. The post-step handles `TypedTypedConstant` and `InterpolatedTypedConstant` uniformly via `TryGetStaticUnit`.
3. `TryGetTypedConstantMagnitude` stays a pure extraction function — single responsibility.

§3.7 is superseded. The implementation should follow §0's design.

**Hidden defect in `TryExtractNumericLiteralMagnitude`:** `ProofEngine.Intervals.cs:84-103` has an `ITuple` recursive case that extracts `tuple[0]` (the raw magnitude) and discards the unit. This method is only reached for `TypedLiteral` nodes (line 25 of `IntervalOfNarrowed`), not `TypedTypedConstant` nodes (which have their own branch at line 28). For quantity typed literals, the parser does NOT produce a `TypedLiteral` with an `ITuple` value — quantities are always `TypedTypedConstant`. So this `ITuple` case may be dead code for the quantity normalization path. However, it should be verified that no other tuple-valued literal can reach it with an un-normalized magnitude. If it IS reachable, the §0 post-step would handle it (since `TryGetStaticUnit` would also need to handle `TypedLiteral` with tuple values).

#### Slice 17: Tests

**Status: Adequate scope, one missing case.**

Test plan covers the primary cross-unit comparison scenarios. Missing: **a test for interpolated quantity with `WholeValue` slot referencing a quantity field** (e.g., `'{quantityField}'` where `quantityField` is `quantity of 'mass'`). This exercises a different interval extraction path than the `Magnitude` slot case. See §5.5.2 for why this matters.

#### Slice 18: Display review

**Status: Complete — §0 resolves the display question.**

§0's "store both" approach means diagnostics display `DeclaredMin/Max` (original authored values) while proof comparison uses `NormalizedDeclaredMin/Max`. No de-normalization needed for display. §7 Q1's options are moot — the answer is (a) by construction.

#### Slice 19: `InterpolatedTypedConstant` gains `UcumParsedUnit?`

**Status: Correctly identified, underspecified.**

The design correctly identifies that the static unit portion (e.g., `[lb_av]` from text segments) must be captured on the AST node. But it does not specify:

1. **Where in the TypeChecker the unit is extracted.** The segment-form matching in `TypeChecker.Expressions.TypedConstants.cs` already identifies which text segments contain unit suffixes via `InterpolationSlotKind`. The `UcumParsedUnit?` should be resolved during `ResolveInterpolatedTypedConstant` (around line 887) by re-parsing the unit text segment through `UcumParser.Parse`. Currently, the text segments are discarded after slot-kind assignment.

2. **What field name to use on `InterpolatedTypedConstant`.** The design says "gains `UcumParsedUnit?`" but doesn't name the property. Recommend: `StaticUnit` (parallels `StaticMagnitude`).

3. **Interaction with the `Unit` slot kind.** If the expression has an `InterpolationSlotKind.Unit` hole (dynamically interpolated unit), `StaticUnit` must be null — the unit is not statically known. The design mentions returning `Unbounded` for this case in Slice 19 (multi-slot with dynamic unit), which is correct.

#### Slice 20: Unit-aware interval scaling

**Status: Correctly designed via §0's universal post-step.**

The `TryGetStaticUnit` + `IntervalOf` post-step from §0 subsumes what Slice 20 describes. No separate `IntervalScale` method is needed — `NumericInterval.Scale(decimal factor)` suffices.

**One concern:** The design assumes a single UCUM factor scales the entire interval. This is correct for quantity types (magnitude × factor). For price types, the denominator unit is inverted (§3.4 `NormalizePrice`). `TryGetStaticUnit` must return not just the `UcumParsedUnit` but also whether it's a direct or inverse factor. Alternative: `TryGetStaticUnit` returns a `decimal` scaling factor directly (pre-inverted for price denominators). This keeps the `IntervalOf` post-step unit-unaware — it just applies a decimal factor.

**Recommendation:** `TryGetStaticUnit` should return `decimal?` (the pre-computed scaling factor), not `UcumParsedUnit`. This keeps `NumericInterval` free of UCUM coupling. For quantity: `factor = unit.Scale → ApplyFactor`. For price: `factor = 1/unit.Scale → ApplyFactor`. For money: `null` (no scaling).

#### Slice 21: Integration tests

**Status: Adequate, well-scoped.**

Test cases cover the critical interpolated scenarios. No gaps.

#### Slice 27: Doc sync

**Status: New slice — added by Frank.**

Documentation propagation slice. Covers all canonical doc surfaces affected by Slices 14–21. See §5.3 for the full inventory.

#### Slice 22: Runtime quantity ingress normalization

**Status: Correctly scoped as Phase 3. One dependency noted.**

The design correctly identifies normalize-on-intake at `TypeRuntimeMeta.ReadJson`. The `Denormalize` method added to Slice 14 ensures the API is symmetric. No architectural concerns.

**Dependency risk:** The storage convention decision (normalized magnitudes in `PreceptValue`) is locked in §0.4 but the `PreceptValue` internal layout is still pending. If the layout changes in a way that doesn't accommodate stored unit metadata alongside normalized magnitude, egress denormalization (`Denormalize`) would have no unit to denormalize with. The design should document that `PreceptValue` for quantities MUST carry unit identity (either via reference-region composite or via `FieldDescriptor` metadata) — this is a hard requirement, not an open question.

---

### §5.5.2 — Interpolated Form Taxonomy

The following is an exhaustive enumeration of all interpolated typed-constant forms that can appear in the Precept DSL, derived from the `InterpolationSlotKind` enum (`SemanticIndex.cs:150-160`) and the typed-constant family definitions.

#### Form 1: Magnitude-only interpolation (quantity)

**DSL:** `'{intField} [lb_av]'` — integer or decimal field fills magnitude, static unit suffix.
**AST:** `InterpolatedTypedConstant(Slots: [TypedInterpolationSlot(TypedFieldRef, Magnitude)], ResultType: Quantity)`
**Current ProofEngine:** `IntervalOfNarrowed` falls to `default → Unbounded`. Strategy 6 (`FindInterpolatedAssignments`) finds it, `GetMagnitudeSlotSource` returns the field ref, `SatisfactionCovers` fails on `DeclarationValue → null`.
**Design proposes:** Slice 19 adds `InterpolatedTypedConstant` case to `IntervalOfNarrowed` — recurse on magnitude slot, get field interval. Slice 20 scales by static unit via `IntervalOf` post-step.
**Gap: None.** This is the primary case the design targets. Fully addressed.

#### Form 2: Magnitude-only interpolation with arg ref (quantity)

**DSL:** `'{eventArg} [kg]'` — event arg fills magnitude.
**AST:** `InterpolatedTypedConstant(Slots: [TypedInterpolationSlot(TypedArgRef, Magnitude)], ResultType: Quantity)`
**Current ProofEngine:** Same `Unbounded` path. `IntervalOfNarrowed` recurses into `TypedArgRef` → `ExtractArgInterval` (lines 114-129), which reads declared `min/max` from event arg metadata.
**Design proposes:** Same as Form 1 — the magnitude slot expression is a `TypedArgRef` instead of `TypedFieldRef`, but `IntervalOfNarrowed` already handles `TypedArgRef` at line 36. The post-step scales by static unit.
**Gap:** `ExtractArgInterval` reads `arg.DeclaredMin/Max` — these are raw (un-normalized) values for the ARG's own bounds, not quantity bounds. If the arg is declared as `newWeight: quantity of 'mass' max '10 [lb_av]'`, the arg's `DeclaredMax` would be `10` (raw), not `4535.9237` (normalized). The arg bound extraction in `ExtractArgInterval` has the SAME raw-magnitude bug as the field bound extraction. **The design does not address arg bound normalization** — it only addresses field bounds via `TypedField.NormalizedDeclaredMin/Max`. Arg metadata on `TypedArg` would need equivalent normalized bounds.

> **§0.6 Condition 5 resolves this.** `TypedArg` gains `NormalizedDeclaredMin/Max` — see Slice 15b.

#### Form 3: WholeValue interpolation (quantity)

**DSL:** `'{quantityField}'` — the entire value comes from a quantity field.
**AST:** `InterpolatedTypedConstant(Slots: [TypedInterpolationSlot(TypedFieldRef, WholeValue)], ResultType: Quantity)`
**Current ProofEngine:** Falls to `Unbounded` (no case for `InterpolatedTypedConstant`).
**Design proposes:** Slice 19 mentions `WholeValue` — "recurse on the expression directly."

**Critical gap — double normalization risk:** When the magnitude slot is `WholeValue` and the source is a quantity field, `IntervalOfNarrowed` would recurse and get `ExtractFieldInterval` → which reads `DeclaredMin/Max` (soon to be normalized, per Slice 15). The resulting interval is ALREADY in base units. If the `IntervalOf` post-step ALSO scales by a static unit (from `TryGetStaticUnit`), the interval would be double-normalized.

**Fix:** `TryGetStaticUnit` must return `null` for `WholeValue` slots (the value already carries its own unit semantics). The post-step must distinguish: "magnitude slot + static unit text" (scale by unit factor) vs. "whole value slot" (no scaling — the interval from the source field is already in the correct unit system). The design does not address this.

#### Form 4: Currency interpolation (money)

**DSL:** `'{amount} {currencyField}'` — magnitude + dynamic currency.
**AST:** `InterpolatedTypedConstant(Slots: [Slot(Magnitude), Slot(Currency)], ResultType: Money)`
**Current ProofEngine:** `Unbounded`. Strategy 6 can find it.
**Design proposes:** Money is explicitly excluded from normalization (§6.3). Correct — currencies are not convertible.
**Gap:** The `IntervalOf` post-step's `TryGetStaticUnit` would return `null` for money (no UCUM unit). But the magnitude slot interval should still be extractable. The Slice 19 `InterpolatedTypedConstant` case should still recurse on the magnitude slot even for money — the raw magnitude interval is valid for overflow checking against declared min/max bounds when both are in the same currency.

**This is a latent correctness gap**: The current `IntervalOfNarrowed` has no case for `InterpolatedTypedConstant` at all — money interpolated forms also get `Unbounded`. After Slice 19 adds the case, money magnitude intervals should work correctly (recurse on magnitude slot, no unit scaling). The design does not explicitly address this but the architecture handles it naturally if Slice 19's implementation is generic across `ResultType`.

#### Form 5: Unit interpolation (quantity)

**DSL:** `'5 {unitField}'` — static magnitude, dynamic unit.
**AST:** `InterpolatedTypedConstant(Slots: [Slot(Unit)], StaticMagnitude: 5, ResultType: Quantity)`
**Current ProofEngine:** `Unbounded`.
**Design proposes:** Slice 19 mentions "For multi-slot expressions with both magnitude and unit interpolated, return `Unbounded`." But this form has a STATIC magnitude and a DYNAMIC unit.

**Gap:** The design's `IntervalOf` post-step relies on `TryGetStaticUnit` to get the scaling factor. If the unit is dynamic (a hole, not a text segment), `TryGetStaticUnit` returns `null`. The interval becomes `Point(5)` (from `StaticMagnitude`) with no scaling — which is wrong when the unit is dynamic because we don't know what unit will be provided at runtime. Returning `Point(5)` would compare an un-scaled magnitude against normalized bounds.

**Fix:** When the unit is dynamic, the interval MUST be `Unbounded` even if the magnitude is static. The `IntervalOfNarrowed` case for `InterpolatedTypedConstant` must check: if any `Unit` or `DenominatorUnit` slot exists, AND `TryGetStaticUnit` returns null, return `Unbounded`. The design mentions this but should be explicit: **"Static magnitude + dynamic unit → Unbounded"** is the required behavior.

#### Form 6: Full interpolation (price)

**DSL:** `'{amount} {currency}/{unit}'` — magnitude and currency interpolated, static denominator unit.
**AST:** `InterpolatedTypedConstant(Slots: [Slot(Magnitude), Slot(Currency)], ResultType: Price)` with static denominator from text segment.
**Current ProofEngine:** `Unbounded`.
**Design proposes:** `NormalizePrice` handles the inverse-factor scaling. `TryGetStaticUnit` would return the inverse of the denominator unit's factor.
**Gap: None**, provided `TryGetStaticUnit` correctly identifies the denominator unit from text segments for price types and returns the pre-inverted factor.

#### Form 7: Denominator unit interpolation (price)

**DSL:** `'10 USD/{unitField}'` — dynamic denominator unit.
**AST:** `InterpolatedTypedConstant(Slots: [Slot(DenominatorUnit)], ResultType: Price)`
**Current ProofEngine:** `Unbounded`.
**Design proposes:** Not explicitly addressed.
**Gap:** Same as Form 5 — dynamic unit → `Unbounded`. `TryGetStaticUnit` returns null because the denominator unit is a hole. Correct behavior: `Unbounded`.

#### Form 8: Numerator+Denominator unit interpolation (price)

**DSL:** `'10 {numUnit}/{denUnit}'` — both units dynamic.
**AST:** `InterpolatedTypedConstant(Slots: [Slot(NumeratorUnit), Slot(DenominatorUnit)], ResultType: Price)`
**Design proposes:** Not explicitly addressed.
**Gap:** Same pattern — dynamic units → `Unbounded`. Correct by construction.

#### Form 9: Currency exchange interpolation

**DSL:** `'{from}-{to} {rate}'` — currency exchange with interpolated currencies.
**AST:** `InterpolatedTypedConstant(Slots: [Slot(FromCurrency), Slot(ToCurrency), Slot(Magnitude)], ...)`
**Design proposes:** Not explicitly addressed.
**Gap:** No UCUM unit involved (currency). No scaling. Magnitude interval from slot expression is the valid overflow check interval. After Slice 19, this should work if the implementation is generic.

#### Summary of Form Coverage

| Form | Magnitude Source | Unit Source | Design Coverage | Gap Severity |
|------|-----------------|------------|-----------------|-------------|
| 1 | Field ref (slot) | Static text | Full | — |
| 2 | Arg ref (slot) | Static text | Partial | **Medium** — arg bounds not normalized |
| 3 | WholeValue (slot) | Inherited | Partial | **High** — double normalization risk |
| 4 | Field/Arg (slot) | N/A (money) | Implicit | Low — works if Slice 19 is generic |
| 5 | Static text | Dynamic (slot) | Mentioned | **Medium** — must return Unbounded explicitly |
| 6 | Slot | Static text (price denom) | Implicit | Low — works with inverse factor |
| 7 | Static/Slot | Dynamic denom (slot) | Not addressed | Low — Unbounded by construction |
| 8 | Static/Slot | Both dynamic | Not addressed | Low — Unbounded by construction |
| 9 | Slot | N/A (currency) | Not addressed | Low — no scaling needed |

---

### §5.5.3 — Architectural Risks

#### Risk 1: Internal Design Contradiction (HIGH)

**Location:** §0 vs. §3.6 vs. §3.7 vs. §7 Q2.

§0 proposes: store both original and normalized on `TypedField`; ProofEngine uses universal `IntervalOf` post-step; `TryGetTypedConstantMagnitude` returns raw. §3.6 proposes: overwrite `DeclaredMin/Max` with normalized. §3.7 proposes: normalize inside `TryGetTypedConstantMagnitude`. §7 Q2 recommends: store original, normalize at proof time.

**Risk:** An implementer reading §3 without §0 will build the wrong thing. §0 supersedes §3 but the document doesn't say so explicitly. There is no "SUPERSEDED" marker on §3.6 and §3.7.

**Mitigation:** Add explicit supersession markers. §3.6 and §3.7 should open with: `> ⚠️ SUPERSEDED by §0. The approach below is retained for historical context but is NOT the current design.`

#### Risk 2: `StaticMagnitude` Used for Numeric Comparisons Without Normalization (MEDIUM)

**Location:** `ProofEngine.Composition.cs:221-223` — `TryGetStaticNumericValue`.

```csharp
case InterpolatedTypedConstant { StaticMagnitude: { } magnitude }:
    value = magnitude;
    return true;
```

This method extracts the raw `StaticMagnitude` (e.g., `5` from `'5 {unitField}'`) and uses it as a concrete numeric value for trusted-rule facts in Strategy 6. After normalization ships, if bounds are stored normalized but `StaticMagnitude` remains raw, the fact-comparison will compare raw against normalized — producing wrong results.

**Fix:** `TryGetStaticNumericValue` must normalize `StaticMagnitude` by the static unit when one exists. This is another site where the `TryGetStaticUnit` + normalization pattern must apply. The design does not identify this site.

#### Risk 3: Arg Bounds Not Normalized (MEDIUM)

**Location:** `ProofEngine.Intervals.cs:114-129` — `ExtractArgInterval`.

Event arg declarations can carry `min`/`max` bounds (e.g., `maxWeight: quantity of 'mass' max '10 [lb_av]'`). `ExtractArgInterval` reads `arg.DeclaredMin/Max` — these are raw magnitudes extracted by the same un-normalized path as field bounds. The design's fix normalizes field bounds via `TypedField.NormalizedDeclaredMin/Max`, but event arg metadata (`TypedArg`) has no corresponding normalized fields.

> **§0.6 Condition 5 resolves this.** `TypedArg` gains `NormalizedDeclaredMin/Max` — see Slice 15b.

**Risk:** After Slices 14-18, field-side bounds are normalized but arg-side bounds are not. An arg with `max '10 [lb_av]'` feeding into a field with `max '5 kg'` would compare `interval from arg: (-∞, 10]` against `normalized field bound: 5000g`. The arg interval should be `(-∞, 4535.9g]`.

**Mitigation:** Either (a) add `NormalizedDeclaredMin/Max` to `TypedArg` (parallel to `TypedField`), or (b) have `ExtractArgInterval` normalize on-the-fly using the arg's qualifier metadata. Option (a) is architecturally consistent with the `TypedField` approach.

> **§0.6 Condition 5 resolves this.** Option (a) selected — see Slice 15b.

#### Risk 4: `GetFieldBounds` in `ProofEngine.Intervals.cs` Uses Raw `DeclaredMin/Max` (MEDIUM)

**Location:** `ProofEngine.Intervals.cs:131-165` — `GetFieldBounds`.

This method reads `field.DeclaredMin/Max` (line 149, via `TryResolveNumericBoundValue`). After §0's revision, `DeclaredMin/Max` are original (raw) values, and `NormalizedDeclaredMin/Max` are the normalized values. `GetFieldBounds` must read the NORMALIZED values — otherwise the intervals it produces for field refs (used in guard narrowing and interval computation) will be in raw units, while the `IntervalOf` post-step adds a SECOND unit scaling to expressions that reference those field intervals.

**This is the same double-normalization risk as Form 3 (WholeValue), but generalized:** Any `TypedFieldRef` to a quantity field will have its interval computed from raw bounds, then potentially scaled by the expression's static unit. If the field is already a quantity, the bounds should be in normalized units, and no further scaling should apply unless the expression adds a unit conversion on top.

**Fix:** `GetFieldBounds` should read `NormalizedDeclaredMin/Max` when they are populated, falling back to `DeclaredMin/Max` when they are null (for non-quantity types). The `IntervalOf` post-step should only scale by the static unit when the expression is a typed constant (not a field ref — field refs' intervals are already in the field's unit system).

This is actually the most subtle point in the design. The `IntervalOf` post-step cannot be a blind "scale every expression that has a static unit" — it must only scale expressions that are producing a NEW quantity from a raw magnitude + static unit. Field refs to quantity fields are already in the field's declared unit system.

The correct rule: **The post-step scales only `TypedTypedConstant` and `InterpolatedTypedConstant` with `Magnitude` slot kind.** It does NOT scale `TypedFieldRef`, `TypedArgRef`, or `InterpolatedTypedConstant` with `WholeValue` slot kind.

#### Risk 5: Strategy 6 `SatisfactionCovers` Does Not Address Cross-Unit Comparison (LOW-MEDIUM)

**Location:** `ProofEngine.Strategies.cs:222-227`.

`SatisfactionCovers` returns `null` for `NumericBoundSource.DeclarationValue`, meaning Strategy 6 can never discharge an interpolated quantity proof where the magnitude source's modifiers use `DeclarationValue` bounds. The design documents this (§1.5.2 point 6) but does not propose a fix — it relies on Strategy 7 (interval containment) to handle the case after Slices 19-20.

**Risk:** This is acceptable IF Strategy 7 reliably discharges the obligation after Slices 19-20. But Strategy 7 runs AFTER Strategy 6 in the discharge order (line 537-540 of `ProofEngine.cs`). If Strategy 6 returns `false` (not `true`), Strategy 7 gets its chance. The concern is: Strategy 6's `TryCompositionalConstraintProof` returns `false` only if the field HAS non-interpolated assignments (`hasNonInterpolated` in `FindInterpolatedAssignments` — line 285 returns empty if any non-interpolated assignment exists). If the field has ONLY interpolated assignments, Strategy 6 iterates through all of them and returns `false` because `SatisfactionCovers` fails. Control then falls to Strategy 7 which (after Slice 19-20) can discharge the obligation.

**The architecture works** — Strategy 6 correctly declines, Strategy 7 handles it. No change needed, but the design should document this intentional handoff.

---

### §5.5.4 — Wrong Layer Assignments

#### Assignment 1: No wrong-layer issues for Slices 14-18

The normalization in the TypeChecker (bounds extraction) and ProofEngine (interval scaling) are in the correct pipeline stages. The TypeChecker owns semantic model construction (`TypedField` properties); the ProofEngine owns abstract interpretation. Normalization at extraction is a TypeChecker concern; interval scaling is a ProofEngine concern. Correct.

#### Assignment 2: `IntervalOf` post-step placement is correct but must be scoped

§0's proposed `IntervalOf` post-step applies scaling between `IntervalOfNarrowed` and the caller. This is the right layer — `IntervalOfNarrowed` is the raw interval computer, `IntervalOf` is the unit-aware wrapper. But as identified in Risk 4, the post-step must NOT blindly scale all expressions. It must be expression-type-aware:

- `TypedTypedConstant` with UCUM unit → scale
- `InterpolatedTypedConstant` with `Magnitude` slot + static unit → scale
- `InterpolatedTypedConstant` with `WholeValue` slot → DO NOT scale
- `TypedFieldRef` / `TypedArgRef` → DO NOT scale (intervals already in declared unit's system)
- Everything else → DO NOT scale

This makes the post-step less "universal" than §0 claims — it's expression-type-dispatched. But the dispatch is simple (2-3 pattern matches) and contained in one location. This is still correct layering — better than scattering normalization into each case of `IntervalOfNarrowed`.

#### Assignment 3: Slice 22 ingress normalization is correct

Normalize-on-intake at `TypeRuntimeMeta.ReadJson` is the right boundary. The evaluator stays a pure plan executor. No wrong-layer issue.

---

### §5.5.5 — Catalog Concerns

#### Concern 1: No catalog changes needed — CONFIRMED

§0.1's analysis is correct and complete. UCUM normalization is implementation infrastructure, not language surface. It fails the "Is this part of a complete description of Precept?" test. No new catalog entries, no new functions, no new opcodes, no new proof requirement kinds. The existing `ProofRequirementKind.IntervalContainment` and the existing UCUM reference data are sufficient.

#### Concern 2: `InterpolationSlotKind` is already catalog-adjacent

The `InterpolationSlotKind` enum (`SemanticIndex.cs:150-160`) with its 8 members (`Magnitude`, `Currency`, `Unit`, `FromCurrency`, `ToCurrency`, `WholeValue`, `NumeratorUnit`, `DenominatorUnit`) is not in a formal catalog, but it is a structural classification axis that pipeline stages switch on. Per the metadata-driven architecture principle: "Does any pipeline stage switch on a `*Kind` enum value to apply per-member behavior?"

The answer is: YES, after this design ships. The `IntervalOf` post-step will switch on slot kind to decide whether to scale. `GetMagnitudeSlotSource` already switches on slot kind. This is a mild catalog smell — but the number of members is small (8), the behavior is simple (scale vs. don't scale), and the classification is structural (AST shape) not domain (language surface). **No action needed now**, but if `InterpolationSlotKind` grows beyond ~12 members, it should be evaluated for catalog promotion.

#### Concern 3: `TryGetStaticUnit` dispatch is not catalog-driven — and shouldn't be

`TryGetStaticUnit` will pattern-match on AST node types (`TypedTypedConstant`, `InterpolatedTypedConstant`) to extract the static UCUM unit. This is structural dispatch on AST shapes, not member-identity dispatch on catalog entries. The AST node types are the compiler's internal representation — they are not catalog members. Correct.

---

### §5.5.6 — Verdict

**APPROVED WITH CONDITIONS.**

The design is architecturally sound in its foundational decisions: normalize-once-at-extraction, two-layer value architecture, shared UCUM math in `Language/Numeric/`, normalize-on-intake for runtime args, and no catalog changes. The interpolated-case strategy (Slices 19-21) is directionally correct.

**Conditions that must be resolved before implementation begins:**

1. **Resolve the §0 / §3 contradiction.** Add explicit `SUPERSEDED` markers to §3.6, §3.7, and §7 Q2. §0's "store both" approach is the approved design. One authoritative description, not three competing ones.

2. **Address the `IntervalOf` post-step scoping.** The post-step is NOT universal — it must only scale `TypedTypedConstant` and `InterpolatedTypedConstant` with `Magnitude`-slot-kind + static unit. Document this explicitly. The `WholeValue` slot and field-ref/arg-ref expressions must NOT be scaled.

3. **Address `GetFieldBounds` to use normalized values.** After §0's `TypedField` revision, `GetFieldBounds` must read `NormalizedDeclaredMin/Max` for quantity fields. Otherwise all field-ref intervals will be in raw units.

4. **Fix `TryGetStaticNumericValue` to normalize `StaticMagnitude`.** `ProofEngine.Composition.cs:221-223` uses raw `StaticMagnitude` for trusted facts — it must normalize by the static unit when present.

5. **Decide whether `TypedArg` needs `NormalizedDeclaredMin/Max`.** If event args can carry quantity bounds, those bounds need normalization parity with `TypedField`. Document the decision. **→ RESOLVED: Yes, Condition 5 / Slice 15b.**

6. **Simplify `NumericInterval.Scale` to take `decimal`.** Drop the `UcumExactFactor` parameter. The conversion to decimal happens once in `ApplyFactor`; the interval doesn't need to know about UCUM types.

None of these conditions are design-blocking — they are specification gaps that the implementer would hit and have to resolve ad-hoc. Closing them now prevents implementation-time architectural drift.

**What is NOT blocking:** Strategy 6's `SatisfactionCovers` returning null for `DeclarationValue` — the handoff to Strategy 7 is architecturally correct. Forms 5/7/8 returning `Unbounded` for dynamic units — conservative and correct. Slice 22's Phase 3 deferral — correctly scoped.

---

## §5.6 — Extended Slice Details — Slices 22–26

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Source:** George's §5.4 gap audit (Gaps A–D), fleshed out by Frank's resolution pass.

These slices extend the normalization design to cover interpolated typed constants beyond the core quantity case (Slices 19–21). They are **Phase 2 extended scope** — not blocking conditions for the core normalization fix, but required before any claim of exhaustive interpolated-typed-constant coverage.

---

**Slice 22: Capture static interpolated qualifier metadata**

- **Objective:** Extend `InterpolatedTypedConstant` to carry a `StaticQualifier` payload capturing resolved qualifier data from text segments.
- **Files:** `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs` (add capture during `ResolveInterpolatedTypedConstant`), `src/Precept/Language/SemanticIndex.cs` or wherever `InterpolatedTypedConstant` is defined (add `StaticQualifier` field)
- **Approach:** During form-matching in `ResolveInterpolatedTypedConstant`, when text segments contain static qualifier data (currency symbol, unit expression, from/to currency codes), parse and store as a `StaticInterpolatedQualifier` discriminated union: `StaticCurrency(string)`, `StaticUnit(UcumParsedUnit)`, `StaticCurrencyAndUnit(string, UcumParsedUnit)`, `StaticFromToCurrencies(string, string)`, `WholeValueSource(TypedExpression)` (for `WholeValue` slots — source qualifiers inferred from the source expression's type).
- **Tests:** TypeChecker tests verifying `StaticQualifier` is populated for: `'{n} USD'`, `'{n} kg'`, `'{n} USD/kg'`, `'{n} USD/EUR'`, `'{moneyField}'`, `'{qtyField}'`
- **Dependencies:** None (standalone model extension); must precede Slices 23–25

---

**Slice 23: Route static qualifier metadata through all qualifier consumers**

- **Objective:** Update `ResolveQualifierFromInterpolatedConstant` and `ValidateAssignmentQualifiers` to consume `StaticQualifier` so rules/ensures stop raising false-positive `PRE0114` and `set`/`default` stop silently accepting definite mismatches.
- **Files:** `src/Precept/Pipeline/ProofEngine.Qualifiers.cs` (`ResolveQualifierFromInterpolatedConstant`), `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs` (`ValidateAssignmentQualifiers`, `TryGetAssignmentSourceQualifiers`)
- **Approach:** `ResolveQualifierFromInterpolatedConstant` currently inspects only slots. After Slice 22, it should first check `StaticQualifier` — if present and resolved, return it directly. This eliminates the "unresolved" fallback that causes false-positive `PRE0114`. `ValidateAssignmentQualifiers` must check `StaticQualifier` against the target field's declared qualifiers — if they don't match, emit `PRE0134` (qualifier mismatch) rather than silently accepting.
- **CONSTRAINT (from George's review — B23):** Do NOT duplicate `WholeValue` source expressions into a new static payload. For `WholeValue` slots, the existing slot expression IS the qualifier source — use the existing `TypedExpression` slot to resolve qualifiers by delegating to the source expression's type metadata. Do not create a redundant copy in `StaticQualifier`.
- **Regression risk (from George's review — B23):** `ProofEngineTypedArgQualifierTests` compound-unit/PRE0114 cases, `TypeCheckerAssignmentQualifierTests`. These tests exercise the current "unresolved" path — after this slice, they should pass with resolved qualifiers instead.
- **Tests:** For each static qualifier form, verify (a) no false-positive `PRE0114` in rules/ensures, and (b) definite qualifier mismatches emit `PRE0134` in `set`/`default`.
- **Dependencies:** Slice 22

---

**Slice 24: Extend interpolated interval extraction beyond quantity**

- **Objective:** Add money whole-value/magnitude and price whole-value/magnitude paths to the `InterpolatedTypedConstant` case in `IntervalOfNarrowed`.
- **Files:** `src/Precept/Pipeline/ProofEngine.Intervals.cs` (primarily), `ProofEngine.Composition.cs` (secondarily)
- **Approach:** The Slice 19 implementation (which adds the `InterpolatedTypedConstant` case to `IntervalOfNarrowed`) handles quantity. Slice 24 extends the same case to handle:
  - Money `Magnitude` slot: recurse on magnitude slot, no unit scaling (currencies are not UCUM-normalizable)
  - Money `WholeValue` slot: recurse on whole-value source expression directly (no scaling)
  - Price `Magnitude` slot with static denominator unit: recurse on magnitude slot, apply inverse factor scaling (same as `NormalizePrice`)
  - Price `WholeValue` slot: recurse on whole-value source, no scaling
  - Dynamic qualifier holes (currency, unit, from-currency, to-currency as slots): return `Unbounded` — cannot normalize without static qualifier knowledge
- **Ordering constraint (from George's review — B24):** Money is straightforward if Slice 19 is implemented generically (not quantity-specific). Price is the tricky case because denominator normalization is inverse — requires static qualifier capture (Slice 22) to identify the static denominator unit. Dynamic qualifier-bearing holes MUST stay conservative (Unbounded). **This slice depends on Slice 22 (static qualifier capture) being complete first.**
- **Tests:** Bounded money field with interpolated money set (`'{n} USD'` and `'{m}'`); bounded price field with interpolated price set; verify false-positive `PRE0078` is eliminated.
- **Dependencies:** Slices 19, 22 (static qualifier capture must land first for price denominator identification)

---

**Slice 25: Add field-default proof coverage for interpolated typed constants**

- **Objective:** Ensure field defaults with interpolated typed constants participate in interval-containment proof coverage (currently bypassed entirely).
- **Files:** `src/Precept/Pipeline/ProofEngine.Analysis.cs` (`CheckInitialStateSatisfiability`, `FoldValue`), likely a new dedicated default-obligation collector
- **Approach — SINGLE PATH CHOSEN (from George's review — B25):**

  > **Primary mechanism: Stronger default folding.** Defaults are not action-driven, so `DynamicObligationGenerator` is the WRONG seam (it generates obligations from `set` actions in transitions). The correct approach is:
  >
  > **(a) FoldValue gains an `InterpolatedTypedConstant` case (primary).** When `StaticQualifier` and `StaticMagnitude` are both available (fully static interpolated form like `'{n} kg'` where `n` is a literal-default integer field), `FoldValue` folds to the concrete typed constant value. This enables the existing initial-state satisfiability check to detect guaranteed-bad defaults without new obligation infrastructure.
  >
  > **(b) Dedicated default-obligation collector (secondary, for non-foldable cases).** For interpolated defaults that reference runtime-variable sources (fields with no foldable default, args), a NEW collector (not `DynamicObligationGenerator`) generates `IntervalContainmentProofRequirement` with the default expression as subject. This collector runs after `FoldValue` — only defaults that could not be folded to a concrete value get obligations.
  >
  > This two-tier approach avoids generating obligations for trivially-foldable cases (wasteful) while still catching non-foldable interpolated defaults that violate bounds.

- **Regression risk (from George's review — B25):** Field default tests and initial-state satisfiability behavior. Existing tests that rely on `UnknownSentinel` degradation for interpolated defaults may need updates.
- **Tests:** Field with `max '5 kg' default '{n} kg'` where `n max 3` should compile clean; where `n max 10` should emit bound violation. Field with `max '5 kg' default '{n} [lb_av]'` where `n max 2` (≈907g < 5000g) should compile clean.
- **Dependencies:** Slices 19, 22, 23

---

**Slice 26: Event arg default resolution — typed-constant defaults for quantity/money/price args**

> **IN-TRACK (Shane direction, 2026-05-14).** Scoped to typed-constant defaults for quantity/money/price event args. Not a general expression-default feature — focused on the normalization use case where arg defaults with unit-bearing typed constants must be resolved, type-checked, and participate in proof coverage.

- **Objective:** Wire `TypedArg.DefaultExpression` resolution so event arg defaults with typed constants (e.g., `default '5 kg'`) are resolved, type-checked against the arg's declared type and bounds, and participate in interval-containment proof coverage (via Slice 25's path). Currently `TypedArg.DefaultExpression` is always `null` (hardcoded at `TypeChecker.cs:527`), meaning even obviously invalid arg defaults (e.g., `max '10 kg' default '15 kg'`) compile silently.

- **Files:**
  - `src/Precept/Pipeline/TypeChecker.cs` — new `ResolveEventArgExpressions` pass
  - `src/Precept/Pipeline/SemanticIndex.cs` — `TypedArg` (existing `DefaultExpression` slot; no type change needed)
  - `src/Precept/Pipeline/ProofEngine.Analysis.cs` — extend initial-state / arg-default satisfiability check to walk `TypedArg.DefaultExpression` (parallel to how field defaults are already checked)

- **Approach — method-level specificity:**

  1. **Add `ResolveEventArgExpressions(SymbolTable, CheckContext)` pass** (new private static method in `TypeChecker.cs`).
     - Called from `TypeCheck()` immediately after `ResolveFieldExpressions(symbols, ctx)` (line 39).
     - Iterates `ctx.Events` → each `TypedEvent.Args` → each arg's corresponding `DeclaredArg` in `symbols.Events`.
     - For each arg: extract the `default` modifier from `DeclaredArg.ParsedModifiers` (same pattern as `ResolveFieldExpressions` lines 574–576).
     - If `defaultMod?.Value is not null and not MissingExpression`:
       - Call `Resolve(defaultMod.Value, ctx, arg.ResolvedType, arg.DeclaredQualifiers)` to produce a `TypedExpression`.
       - Type-check: `IsAssignable(resolved.ResultType, arg.ResolvedType)` → emit `DiagnosticCode.TypeMismatch` if false.
       - Qualifier-check: `ValidateAssignmentQualifiers(resolved, arg.Name, arg.DeclaredQualifiers, ...)` if arg has declared qualifiers.
       - Decimal-places check: `ValidateMaxPlaces(resolved, ...)` if applicable (may require a small adapter since `ValidateMaxPlaces` currently takes `TypedField`).
       - Patch: `ctx.Events[eventIdx] = ctx.Events[eventIdx] with { Args = updatedArgs }` where the updated arg has `DefaultExpression = resolved`.
     - Scope: during arg-default resolution, the resolution context should see **no fields and no args** (arg defaults cannot reference instance state — they are declaration-time constants). Set `ctx.CurrentScope = FieldScopeMode.NoFields` or equivalent guard.

  2. **Extend `ProofEngine.Analysis.cs` initial-state satisfiability** to check resolved arg defaults against arg bounds.
     - In `CheckInitialStateSatisfiability` (or a new parallel `CheckArgDefaultSatisfiability`), iterate events → args.
     - For each arg where `DefaultExpression is not null`: apply the same FoldValue → interval containment logic used for field defaults in Slice 25.
     - Emit `IntervalContainmentProofRequirement` if the default's folded value (or interval) exceeds the arg's `DeclaredMin/Max`.
     - For quantity/money/price args: the default typed constant's magnitude must be normalized via `TryGetStaticScalingFactor` before comparison with normalized bounds (Slice 15b provides `NormalizedDeclaredMin/Max` on `TypedArg`).

  3. **No changes to `SemanticIndex.cs`** — `TypedArg.DefaultExpression` slot already exists with the correct type (`TypedExpression?`). No new fields needed.

  4. **Scope limitation:** This slice resolves ONLY typed-constant and literal default expressions (the forms relevant to quantity/money/price normalization). Interpolated defaults with field/arg refs are resolved syntactically but may produce `Unbounded` intervals at proof time — consistent with the conservative approach in Slice 25's secondary path.

- **Tests:**
  - `event Load(weight: quantity of 'mass' max '10 kg' default '15 kg')` → emits `NumericOverflow` (15 > 10 in base units).
  - `event Load(weight: quantity of 'mass' max '10 kg' default '5 kg')` → compiles clean.
  - `event Load(weight: quantity of 'mass' max '5 kg' default '6 [lb_av]')` → compiles clean (6 lb ≈ 2.72 kg < 5 kg, normalization applies).
  - `event Load(weight: quantity of 'mass' max '2 kg' default '6 [lb_av]')` → emits `NumericOverflow` (6 lb ≈ 2.72 kg > 2 kg).
  - `event Load(cost: money max '100 USD' default '50 USD')` → compiles clean.
  - `event Load(weight: quantity of 'mass' default '5 kg')` → compiles clean (no bounds = no proof obligation).
  - `event Load(weight: integer default 'hello')` → emits `TypeMismatch` (type checking works for non-quantity args too).

- **Regression risk:**
  - `ProofEngineIntervalIntegrationTests` — tests that assert no proof obligations on events with defaults may break (previously silently accepted due to null DefaultExpression).
  - `TypeCheckerTests` — tests that count diagnostics on event constructs may get additional diagnostics from newly-resolved defaults.
  - `HasError` traversal (line 1045–1052) — already walks `arg.DefaultExpression`, so error-suppression logic is ready.
  - CI validation tests in `test/Precept.LanguageServer.Tests/` that snapshot diagnostic counts.

- **Dependencies:** Slice 25 (for the proof-engine default-obligation path that this slice feeds into); Slice 15b (for `NormalizedDeclaredMin/Max` on `TypedArg` which the proof check reads).

- **Ordering:** Slice 26 comes after Slice 25 (needs the proof path) and after Slice 27 (doc sync). Final position: last implementation slice before Phase 3 deferred work.

- **Key risk:** The `ValidateMaxPlaces` helper currently takes `TypedField`. It needs a minor refactor or overload to accept `TypedArg` (or extract the common parameters: `DeclaredMin`, `DeclaredMax`, `ResolvedType`, `DeclaredQualifiers`). This is a small adapter, not a broad refactor.
