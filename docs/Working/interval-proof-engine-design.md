# Interval-Based Proof Strategy — Design Document

**Author:** Frank (Lead Architect)
**Status:** ✅ Approved — Shane sign-off complete. Implementation in progress.
**Date:** 2026-05-13
**Scope:** Compile-time overflow prevention via interval arithmetic, catalog-driven obligation generation across all constrained type families, and qualified-type bound semantics
**Review target:** Elaine (tooling/hover impact), George (implementation), Soup Nazi (test coverage)

---

## Implementation Tracker

| Slice | Objective | Agent | Status | Tests | Notes |
|-------|-----------|-------|--------|-------|-------|
| **1** | Catalog Foundation + `NumericInterval` struct | George | ✅ Done | — | Blocking Slice 2 |
| **2** | Obligation Collection + Strategy 7 Wiring (decimal) | George | ✅ Done | — | Depends on Slice 1 |
| **3** | Guard-Narrowing Integration | George | ✅ Done | — | Depends on Slice 2 |
| **4** | Function Overload Intervals | George | ✅ Done | — | Independent after Slice 2 |
| **5** | Hover Expression Display + Diagnostic Squiggle | Kramer | ✅ Done | `dotnet test test/Precept.LanguageServer.Tests/Precept.LanguageServer.Tests.csproj --filter "FullyQualifiedName~HoverHandlerIntervalTests\|FullyQualifiedName~HoverHandlerTests"` (70/70) | Interval field/expression + NumericOverflow squiggle hover shipped |
| **6** | MCP Sync Assessment + Tooling Propagation | Soup Nazi | ✅ Done | `dotnet test test/Precept.Mcp.Tests/Precept.Mcp.Tests.csproj --filter "FullyQualifiedName~CompileToolTests\|FullyQualifiedName~NewToolTests"` (38/38); `dotnet test test/Precept.Mcp.Tests/Precept.Mcp.Tests.csproj` (44/44) | `precept_compile` now projects `proofObligations` with obligation kind + `computedInterval`; `IntervalContainment` vocabulary confirmed via catalog-derived `precept_proofs` output (`precept_language` surface remains removed). |
| **7** | Catalog-Driven Obligation Generator Refactor | George | ✅ Done | `ProofEngineIntervalIntegrationTests`; `ProofEngineTests.Slice1_ObligationCollection`; `Precept.Tests` | Catalog-driven bounds extraction from modifier metadata (`ApplicableTo` + `ProofSatisfactions`) wired for obligation generation; Slice 2 anchors remain green. |
| **8** | Qualified-Type Bound Semantics | George | ✅ Done | `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~TypeCheckerModifierTests\|FullyQualifiedName~PriceExchangeRateModifierTests"` (69/69); `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~TypeChecker"` (662/662) | Added metadata-driven bound qualifier requirements (`money`/`quantity`/`price`) and `BoundsRequireQualifier` type-checker enforcement with regression coverage. |
| **9** | Typed-Constant Bound Extraction | George | ✅ Done | `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~ProofEngineIntervalIntegrationTests\|FullyQualifiedName~Track2PhaseAModifierValidationTests\|FullyQualifiedName~TypeCheckerModifierTests"` (101/101) | Typed-constant min/max extraction now populates declared bounds for money/quantity/price and persists bound qualifier metadata for Slice 10 follow-up checks. |
| **10** | Qualifier Compatibility Checks | George | ✅ Done | `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~TypeCheckerQualifierCompatibilityTests\|FullyQualifiedName~TypeCheckerModifierTests"` (11+17 pass); `dotnet test test/Precept.Tests/Precept.Tests.csproj` (5280/5280) | Added `BoundsQualifierMismatch` (PRE0134) diagnostic; per-bound qualifier compatibility check in `ValidateBoundQualifierCompatibility` enforces qualifier match when both field and typed-constant bound carry qualifiers; also enforces `BoundsRequireQualifier` when qualified field has a plain numeric bound. |
| **11** | String/Collection Constraint Obligations | George | ✅ Done | `dotnet test --filter ProofEngineStringCollectionBoundTests` (19 tests) | Slice 7; conservative literal-only strategy for string bounds |
| **12** | Presence Obligation Generation | George | ✅ Done | `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~ProofEnginePresenceTests"` (13/13) | Depends on Slice 7; parallel with Slices 8–11 |
| **13** | Type-Family Coverage Regression Suite | Soup Nazi | ✅ Done | `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~TypeFamilyCoverageTests"` (28/28) | Per-family positive + negative companion tests for all §12 matrix rows; two meta-tests (`AllConstrainableTypes_DeclaredConstraint_NeverSilentlyIgnored`, `OptionalField_InValuePosition_GeneratesPresenceObligation`); catalog-driven coverage verified |

**Initiated:** 2026-05-13T19:19:55Z
**Updated by:** Scribe (live status updates as agents complete slices)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Core Concepts](#2-core-concepts)
3. [Proof Engine Integration](#3-proof-engine-integration)
4. [Constraint Model](#4-constraint-model)
5. [Edge Cases](#5-edge-cases)
6. [Hover Display Compatibility](#6-hover-display-compatibility)
7. [Philosophy Alignment](#7-philosophy-alignment)
8. [Implementation Plan](#8-implementation-plan)
9. [Test Strategy](#9-test-strategy)
10. [Catalog-Driven Obligation Architecture](#10-catalog-driven-obligation-architecture)
11. [Qualified-Type Bound Semantics](#11-qualified-type-bound-semantics)
12. [Type-Family Coverage Matrix](#12-type-family-coverage-matrix)

---

## 1. Executive Summary

Precept's prevention guarantee requires that **arithmetic overflow is structurally impossible** — not caught at runtime, structurally impossible. The compiler must prove it, just as it proves division-by-zero impossible. Today it cannot: the proof engine's six strategies (Literal, Declaration Attribute, Guard-in-Path, Flow-Narrowing, Qualifier Compatibility, Compositional Constraint) handle scalar-threshold proofs, dimensional compatibility, and composite constraint composition but carry **zero proof machinery for arithmetic result-range checking**. `NumericOverflow` is marked `[StaticallyPreventable]` in the Faults catalog with no corresponding prevention path. That is a lie the product cannot afford to tell.

**This design closes that gap.** The interval-based proof strategy (Strategy 7: `IntervalContainment`) teaches the proof engine to:

1. Extract declared numeric bounds from existing `min`/`max` field modifiers.
2. Propagate those bounds through arithmetic expression trees using interval transfer functions declared in the `Operations` catalog — not hardcoded in the engine.
3. Check that the computed result interval fits within the assignment target's declared bounds.
4. Discharge `IntervalContainmentProofRequirement` obligations when containment is proven; leave them unresolved (emitting `NumericOverflow` ERROR) when it is not.

**Why this strategy satisfies Precept's prevention guarantee:** If the compiler can prove that every arithmetic expression that assigns to a bounded field produces a result interval contained within that field's bounds, then no representable-type overflow can occur on that field. This is a structural proof at definition time — the same category as division-by-zero prevention. The guarantee holds for every field that carries declared bounds. For unbounded fields, an unresolved obligation blocks compilation, forcing the author to either declare bounds or restructure the expression. The invariant: **a definition that compiles without diagnostics has no unproven overflow obligations.**

**Why interval arithmetic, not an SMT solver or saturation semantics:** Precept's domain (financial arithmetic) is primarily linear. Interval arithmetic is O(n) in expression depth, sound for addition/subtraction/multiplication/division, and was already proven correct in Precept's v1 runtime (255 tests, deleted in commit `b0c09eb5` during architectural pivot). Saturation semantics hide errors, violating Precept's honesty principle. SMT solvers add an external dependency, introduce timeout risk (incompatible with determinism), and are overkill for linear arithmetic. The custom solver is sufficient and has prior proof of correctness.

---

## 2. Core Concepts

### 2.1 Interval Semantics

An interval `[min, max]` represents the set of all possible values a numeric expression can produce. Intervals compose through arithmetic:

```
[a, b] + [c, d]  = [a + c,  b + d]
[a, b] - [c, d]  = [a - d,  b - c]      ← note swap on subtraction
[a, b] × [c, d]  = [min of corners, max of corners]   ← 4-corner case
[a, b] ÷ [c, d]  = unbounded if [c, d] contains 0; else 4-corner case
```

Two special values:
- **`Unbounded` interval** — `[decimal.MinValue, decimal.MaxValue]`. Assigned to any field or expression whose bounds are not declared. Operations on unbounded intervals propagate unbounded results.
- **`Empty` interval** — `[max, min]` where max < min. Signals a logically impossible range (e.g., produced when guard constraints over-constrain). Empty intervals trivially satisfy containment (no value can exist outside them) — treated as proved.

### 2.2 Interval Propagation Through the Expression Tree

The proof engine traverses a typed expression tree bottom-up, computing an interval for each node:

| Expression kind | Interval computation |
|---|---|
| `TypedLiteral` (numeric) | `[value, value]` — point interval |
| `TypedFieldRef` (with declared bounds) | `[field.Min, field.Max]` |
| `TypedFieldRef` (no declared bounds) | `Unbounded` |
| `TypedEventArgRef` (with declared bounds) | `[arg.Min, arg.Max]` |
| `TypedEventArgRef` (no declared bounds) | `Unbounded` |
| `TypedBinaryOp` | Transfer function from `BinaryOperationMeta.IntervalTransfer` |
| `TypedFunctionCall` | Transfer function from `FunctionOverload.IntervalTransfer` (null = unbounded) |
| `TypedConditional` | `union([then-interval, else-interval])` |

**Critical rule: unbounded propagates.** Any operation on an unbounded operand produces an unbounded result. This is intentional — it forces the proof to fail unless the author has declared bounds at the leaves.

### 2.3 Proof Obligation: `IntervalContainment`

An `IntervalContainmentProofRequirement` is generated for every `set` assignment whose target field has declared bounds (`min` and/or `max` modifiers). The proof obligation says:

> "Prove that the result interval of `<expression>` is contained within `[target.Min, target.Max]`."

**Containment check:** interval `R` is contained in bounds `B` iff `R.Min >= B.Min && R.Max <= B.Max`.

**One-sided bounds:** If a field declares only `min X` (no max), the containment check is one-sided: `R.Min >= X`. Similarly for `max Y` only: `R.Max <= Y`. If neither bound is declared, no obligation is generated.

**Proof disposition:**
- `Proved` — computed interval is contained in bounds.
- `Unresolved` — computed interval exceeds bounds OR contains unbounded (the result potentially overflows). Emits `NumericOverflow` diagnostic with `Severity.Error`, message identifying which bound is violated and the computed interval.

---

## 3. Proof Engine Integration

### 3.1 Pipeline Position

The proof engine runs after type checking and graph analysis. This order is unchanged. What changes: the proof engine now carries an additional pass — **Strategy 7: IntervalContainment** — which runs after the existing five strategies on any unresolved `IntervalContainmentProofRequirement` obligations.

```
Lexer → Parser → TypeChecker → GraphAnalyzer → ProofEngine → Compiler output
                                                     ↑
                                         Strategy 7 runs here, on
                                         IntervalContainmentProofRequirement
                                         obligations collected from set-actions
```

The ProofEngine pipeline, with new components bolded:

```
1. Obligation collection pass
   - collect existing obligations (Numeric, Presence, Dimension, QualifierCompatibility, etc.)
   - NEW: collect IntervalContainmentProofRequirement for every set-action on bounded targets

2. Strategy resolution (for each unresolved obligation):
   - S1: Literal
   - S2: DeclarationAttribute
   - S3: GuardInPath
   - S4: FlowNarrowing
   - S5: QualifierCompatibility
   - S6: CompositionalConstraint
   - NEW S7: IntervalContainment (only for IntervalContainmentProofRequirement)

3. Emit diagnostics for remaining Unresolved obligations
```

### 3.2 Obligation Collection: Where `IntervalContainmentProofRequirement` Originates

**Source:** The `set` action in the `Actions` catalog carries a `ProofRequirementKind.IntervalContainment` entry in its `ProofRequirements` list. The obligation collection pass reads this and, for each `TypedSetAction` whose target field has at least one bound modifier, creates an `IntervalContainmentProofRequirement` obligation.

**Catalog-driven design (non-negotiable):** The obligation is not hardcoded in the proof engine against the string `"set"`. The `ActionMeta` for `set` declares the requirement; the obligation collector reads `ActionMeta.ProofRequirements` — the same machinery used for division and sqrt obligations. This is consistent with the catalog-driven architecture.

**Obligation parameters:**
```csharp
public sealed record IntervalContainmentProofRequirement(
    ProofSubject  Subject,        // the RHS expression being assigned
    string        TargetField,    // the target field name
    decimal?      DeclaredMin,    // from target's min modifier, null if absent
    decimal?      DeclaredMax,    // from target's max modifier, null if absent
    string        Description
) : ProofRequirement(ProofRequirementKind.IntervalContainment, Description);
```

The bounds (`DeclaredMin`, `DeclaredMax`) are extracted from the target field's `ValueModifierMeta.ProofSatisfactions` at collection time. Specifically: walk the target field's modifiers; for any `ValueModifierMeta` whose `ProofSatisfactions` include a `ProofSatisfaction.Numeric` with `Comparison = GreaterThanOrEqual`, use that bound as `DeclaredMin`. For `LessThanOrEqual`, use as `DeclaredMax`.

**No new syntax.** Existing `min X` and `max X` field modifier declarations are the bound source. Authors who already declare bounds get interval proofs at no additional annotation cost.

### 3.3 Strategy 7: Interval Containment Solver

**New file:** `src/Precept/Pipeline/ProofEngine.Intervals.cs`

```
ProofEngine.Intervals.cs responsibilities:
  - NumericInterval struct (Min, Max, IsUnbounded, IsEmpty, arithmetic operations)
  - IntervalOf(TypedExpression, SemanticIndex) → NumericInterval
  - TryIntervalContainmentProof(ProofObligation, SemanticIndex) → bool
```

**`IntervalOf` traversal (catalog-driven):**
- Reads `BinaryOperationMeta.IntervalTransfer` delegate for each binary op — the catalog declares how the operation propagates intervals. The engine calls it. No hardcoded per-operator arithmetic in the engine.
- Falls back to `Unbounded` if no transfer function is declared (safe default).

**New field on `BinaryOperationMeta`:**
```csharp
public sealed record BinaryOperationMeta(
    // ... existing fields ...
    IntervalTransferFn? IntervalTransfer = null
)
```

```csharp
public delegate NumericInterval IntervalTransferFn(NumericInterval left, NumericInterval right);
```

Transfer functions are declared as `static` methods in `Operations.cs` per arithmetic operator, then referenced in each `BinaryOperationMeta` entry. Example:

```csharp
// In Operations.cs — transfer functions (static, pure)
private static NumericInterval AddTransfer(NumericInterval l, NumericInterval r)
    => l.IsUnbounded || r.IsUnbounded ? NumericInterval.Unbounded
     : new(l.Min + r.Min, l.Max + r.Max);

private static NumericInterval SubtractTransfer(NumericInterval l, NumericInterval r)
    => l.IsUnbounded || r.IsUnbounded ? NumericInterval.Unbounded
     : new(l.Min - r.Max, l.Max - r.Min);   // note swap

private static NumericInterval MultiplyTransfer(NumericInterval l, NumericInterval r)
{
    if (l.IsUnbounded || r.IsUnbounded) return NumericInterval.Unbounded;
    var corners = new[] { l.Min * r.Min, l.Min * r.Max, l.Max * r.Min, l.Max * r.Max };
    return new(corners.Min(), corners.Max());
}

private static NumericInterval DivideTransfer(NumericInterval l, NumericInterval r)
{
    if (l.IsUnbounded || r.IsUnbounded) return NumericInterval.Unbounded;
    // Division by interval containing zero → unbounded (division-by-zero separately caught)
    if (r.Min <= 0m && r.Max >= 0m) return NumericInterval.Unbounded;
    var corners = new[] { l.Min / r.Min, l.Min / r.Max, l.Max / r.Min, l.Max / r.Max };
    return new(corners.Min(), corners.Max());
}
```

**Strategy 7 implementation:**
```csharp
private static bool TryIntervalContainmentProof(
    ProofObligation obligation, SemanticIndex semantics)
{
    if (obligation.Requirement is not IntervalContainmentProofRequirement intervalReq)
        return false;

    var resultInterval = IntervalOf(obligation.Site, semantics);

    // One-sided or two-sided containment check
    if (intervalReq.DeclaredMin.HasValue && resultInterval.Min < intervalReq.DeclaredMin.Value)
        return false;   // lower bound violated
    if (intervalReq.DeclaredMax.HasValue && resultInterval.Max > intervalReq.DeclaredMax.Value)
        return false;   // upper bound violated

    return true;
}
```

### 3.4 `ProofStrategy` Enum Extension

```csharp
public enum ProofStrategy
{
    Literal                 = 1,
    DeclarationAttribute    = 2,
    GuardInPath             = 3,
    FlowNarrowing           = 4,
    QualifierCompatibility  = 5,
    CompositionalConstraint = 6,
    IntervalContainment     = 7,    // NEW
}
```

### 3.5 Catalog Additions Summary

| Catalog | Change |
|---|---|
| `ProofRequirementKind` | Add `IntervalContainment = 7` |
| `ProofRequirement` | Add `IntervalContainmentProofRequirement` record |
| `ProofRequirementMeta` | Add `IntervalContainment` DU subtype |
| `ProofSatisfaction` | Add `IntervalContainment` subtype (positive carrier) |
| `ProofRequirements.GetMeta` | Add exhaustive switch arm for `IntervalContainment` |
| `BinaryOperationMeta` | Add `IntervalTransfer` delegate field (nullable) |
| `Operations.cs` | Populate `IntervalTransfer` for `+`, `−`, `×`, `÷` entries on `decimal`/`number`/`integer` |
| `Actions.cs` | Add `ProofRequirementKind.IntervalContainment` to `set`'s proof requirements |
| `ProofStrategy` | Add `IntervalContainment = 7` |
| `Diagnostics` | Verify `NumericOverflow` (already `[StaticallyPreventable]`) is wired as this strategy's emitted code |

---

## 4. Constraint Model

### 4.1 How `min`/`max` Field Modifiers Feed Interval Propagation

The interval engine reads bounds from the existing `ValueModifierMeta.ProofSatisfactions` on a field's declared modifiers. The extraction logic:

```csharp
private static (decimal? min, decimal? max) ExtractBounds(TypedField field)
{
    decimal? min = null, max = null;
    foreach (var modifier in field.Modifiers.Concat(field.ImpliedModifiers))
    {
        var meta = Modifiers.GetMeta(modifier);
        if (meta is not ValueModifierMeta vmm) continue;
        foreach (var sat in vmm.ProofSatisfactions)
        {
            if (sat is ProofSatisfaction.Numeric { Comparison: OperatorKind.GreaterThanOrEqual } ns
                && ns.Bound is NumericBoundSource.Constant c)
                min = c.Value;
            if (sat is ProofSatisfaction.Numeric { Comparison: OperatorKind.LessThanOrEqual } ns2
                && ns2.Bound is NumericBoundSource.Constant c2)
                max = c2.Value;
        }
    }
    return (min, max);
}
```

**Design rationale:** This re-uses the existing modifier satisfaction machinery. No new field on `TypedField` is needed in the initial slice. Bounds are derived from catalog metadata at proof-obligation-collection time.

### 4.2 Event Argument Bounds

Event arguments can also carry bounds via the same `min`/`max` modifier mechanism (if the DSL supports modifier annotations on event args). In the initial implementation, event arg bounds are treated as unbounded unless the arg has declared modifiers. Guard-in-Path proofs (Strategy 3) already handle guards like `require amount >= 0`, which narrows the interval from the guard context. The interval engine's integration with guard narrowing is described in Edge Cases (§5.4).

### 4.3 `integer` Type

`integer` = `System.Numerics.BigInteger` — mathematically unbounded. The `IntervalContainment` obligation is **never generated** for `integer` fields: BigInteger cannot overflow by type definition. Interval proofs on `integer` expressions are only relevant if the target is a bounded type (e.g., `decimal`). This is expressed in the obligation collection pass: skip obligation generation when the target field's resolved type is `TypeKind.Integer`.

### 4.4 Type Representable Bounds vs. Field Bounds

There are two distinct overflow concerns:

1. **Type representable overflow** — the arithmetic result exceeds `decimal.MaxValue` (~7.9 × 10²⁸). This is a catastrophic failure.
2. **Field bound overflow** — the result exceeds the author-declared `max` on the field (e.g., `max 999999`). This is a business-rule violation.

The interval strategy handles **both** via the same containment check. When no field bound is declared, the interval engine uses the type's representable range as the implicit bound. This is the mechanism by which *type-level* overflow is caught: an unbounded field still has an implicit `[decimal.MinValue, decimal.MaxValue]` bound, and if an expression can exceed it, the containment check fails.

**However**, for the initial implementation, type-representable bounds are declared as implicit bounds only if the author explicitly opts in via `max` or `min` (or both). An unbounded field with no declared bounds emits no interval obligation — this matches gradual adoption. See §5.1 for the phasing rationale.

---

## 5. Edge Cases

### 5.1 Unbounded Fields (No Declared Bounds)

**Behavior:** If a field has no `min` or `max` modifier, no `IntervalContainmentProofRequirement` is generated for assignments to that field. The existing runtime fault path (`NumericOverflow` fault) remains as the safety net.

**Rationale:** Forcing a hard error on all unbounded fields in the initial implementation would break every existing precept definition. Gradual adoption is required. The design opts for: bounded fields get compile-time proof; unbounded fields retain runtime fallback. Future work can introduce a definition-level pragma (`require-bounds` or similar) to force interval obligations on all numeric fields.

**Philosophy check:** This does NOT violate the prevention guarantee. The guarantee is: "a definition that compiles without diagnostics has no unproven proof obligations." If no obligation is generated, no obligation is unresolved. The runtime fault path is still present — the guarantee weakens only for authors who choose not to declare bounds. The strengthened guarantee ("overflow structurally impossible") applies fully to any field with declared bounds.

### 5.2 Optional Fields

Optional fields (`field x: decimal?`) may be unset. The interval engine treats an optional field's interval as `[field.Min, field.Max]` when bounds are declared, identical to a required field. The presence-check obligation (Strategy 2) separately ensures the field is proven set before any arithmetic access. If the presence proof fails, the expression containing the field access is already flagged — the interval obligation is secondary.

**Ordering:** The obligation collector generates both the presence obligation (if applicable) and the interval obligation. The interval obligation is resolved or left unresolved independently. This is correct: even if presence is proved, the interval must also be proved.

### 5.3 Computed Fields (`compute` expressions)

Computed fields are derived from an expression at definition time. Their interval is computed from the derivation expression. If the derivation expression's interval fits within the computed field's declared bounds, the obligation is proved. If not, the obligation is unresolved.

**Special case: computed fields with no declared max.** If the computed field has no declared bound, no obligation is generated. The author can add bounds to the computed field, or the compiler will infer bounds in a future tier (bounds inference is deferred — see §8 Implementation Plan, Slice 4).

### 5.4 Guard-Narrowing Integration

Existing Strategy 3 (Guard-in-Path) already extracts `GuardConstraint` records from guard expressions. These can narrow a field's interval:

- `when amount >= 100m` → narrows amount's interval lower bound to `max(declared.Min, 100m)`
- `when amount <= 5000m` → narrows amount's interval upper bound to `min(declared.Max, 5000m)`

**Integration approach (deferred to Slice 3):** In the initial implementation (Slice 1), the interval engine does not apply guard narrowing — it uses declared bounds from modifiers only. This means expressions that are only safe when guards narrow the domain may produce unresolved obligations that a later slice will prove. Slice 3 adds guard-narrowing: the interval engine calls `NarrowWithGuard(interval, guardConstraints)` before composing the expression tree.

**Correctness invariant:** Not applying guard narrowing is conservative (more obligations left unresolved), never unsound (no false proofs). This is the correct ordering: start conservative, relax with each slice.

### 5.5 Cross-Field Bounds (Future Work, Not in Initial Design)

An expression like `total = sum(lineItems, price)` where total's max is conceptually derived from line item count × price max — this is not handled in the initial implementation. Cross-field bounds require solving a system of constraint equations and are deferred to a future tier. For now, such expressions result in unresolved obligations unless the author declares an explicit numeric bound on `total`.

### 5.6 Negation and Absolute Value

Unary negation on a bounded interval: `negate([a, b]) = [-b, -a]`. This is handled as a unary transfer function. The `Operations` catalog's entries for unary negation receive an `IntervalTransfer` delegate.

Absolute value (if available as a function): `abs([a, b])`:
- If `a >= 0`: `[a, b]` unchanged
- If `b <= 0`: `[-b, -a]`
- Mixed: `[0, max(-a, b)]`

This is handled in `FunctionOverload.IntervalTransfer`.

### 5.7 `number` (IEEE 754) vs. `decimal` (Fixed-Point) Rounding

IEEE 754 `number` type has rounding during arithmetic. The interval engine for `number` must widen intervals by one ULP (unit in the last place) at each multiplication/division to account for rounding error. This is a `number`-specific widening applied in the transfer function: the `Operations` catalog declares distinct transfer functions for `number` vs `decimal` operations.

**Initial implementation:** Apply widening only for multiplication and division on `number`. Addition and subtraction on IEEE 754 double are exact for most financial ranges (no widening needed if both operands are within `±10^15`). This approximation is conservative.

---

## 6. Hover Display Compatibility

The hover design (V7, `docs/working/hover-design.md`) is the proof display contract. Interval arithmetic **extends** it — it does not introduce new card kinds or replace existing badges. Every interval proof result is expressed using the existing badge vocabulary.

**Interval notation:** All intervals use bracket notation with two-dot separator: `[lo .. hi]`
- `[0 .. 1 000]` — fully bounded
- `[0 .. +∞]` — lower-bounded only
- `[−∞ .. 0]` — upper-bounded only
- `[−∞ .. +∞]` — unbounded (no declared or inferred limit)

Use thin space as thousands separator in intervals longer than four digits (`999 999.99` not `999999.99`).

### 6.1 Field with Declared Bounds (Proven Safe)

Field has declared `min`/`max`; all assignments proven to stay within the bound.

```md
✅ Proven · `balance` stays within `[0 .. 999 999 999.99]`
⚖️ Declared: `min 0 max 999 999 999.99` · `CatalogCurrency`
Governed by: 2 rules · 1 ensure
```

**Reading:** Line 1 delivers the verdict. Line 2 cites the bound source and links the qualifier. Line 3 provides governance context (unchanged from standard field card).

**Data sources:** `ProofLedger.Obligations` filtered by `IntervalContainmentProofRequirement` targeting this field with `Disposition == Proved`; `IntervalContainmentProofRequirement.DeclaredMin/Max` for the bounds.

### 6.2 Field with Declared Bounds (Gap — Assignment Overflows)

Field has declared bounds but a computed assignment produces a result interval that escapes them.

```md
⚠️ Gap · `balance` assignment may leave `[0 .. 999 999 999.99]`
🔬 `balance − amount` → `[−50 000 .. 999 999 999.99]` · lower bound unsafe
`amount` has no lower bound · add guard `amount ≤ balance` or bound `amount`
```

**Reading:** Line 1 names the field and the violated range. Line 2 shows the arithmetic: operand intervals yield the result, flagging which side is unsafe. Line 3 is the actionable repair hint.

**Expanded view (lines 4–5, when proof is the user's question):**

```md
⚠️ Gap · `balance` assignment may leave `[0 .. 999 999 999.99]`
🔬 `balance − amount` → `[−50 000 .. 999 999 999.99]` · lower bound unsafe
`amount` has no lower bound · add guard `amount ≤ balance` or bound `amount`
🔬 `balance ∈ [0 .. 999 999 999.99]` (declared)
🔬 `amount ∈ [−∞ .. +∞]` (no bounds declared) · subtraction expands lower to `−∞`
```

**Data sources:** `ProofObligation` (kind `IntervalContainment`, `Disposition != Proved`) matched to the field; `ComputedInterval` metadata carrying operand intervals; `ProofObligation.Context` identifies the failing assignment expression.

### 6.3 Computed Expression with Interval Result (Proven Safe)

Arithmetic expression with fully bounded operands; result fits target.

```md
✅ Proven · `principal + interest` result `[0 .. 50 000]` fits `loanBalance`
🔬 `[0 .. 45 000]` + `[0 .. 5 000]` → `[0 .. 50 000]`
Target declared: `max 50 000` · proven safe
```

**Reading:** Line 1 gives the verdict, names the expression, shows the result interval, and names the target field. Line 2 shows the propagation. Line 3 confirms the target bound and proof success.

**Data sources:** `TypedBinaryOp` expression span; `ProofObligation.Disposition == Proved`; `ComputedInterval` on the obligation carrying the result interval and declared target bounds.

### 6.4 Computed Expression with Interval (Gap — Overflow Risk)

Arithmetic expression where the result interval exceeds the target field's bound.

```md
⚠️ Gap · `invoice + surcharge` may exceed `[0 .. 999 999.99]`
🔬 `[0 .. 999 990.00]` + `[0 .. 100.00]` → `[0 .. 1 000 090.00]` · upper bound unsafe
`surcharge` max exceeds available headroom in `invoice` · tighten `surcharge` bounds
```

**Reading:** Line 1 names the expression and the violated bound. Line 2 shows the full arithmetic with the computed result, flagging which end is unsafe. Line 3 gives the repair direction — operand-specific hint.

**Expanded view (when detailed arithmetic is needed):**

```md
⚠️ Gap · `invoice + surcharge` may exceed `[0 .. 999 999.99]`
🔬 `[0 .. 999 990.00]` + `[0 .. 100.00]` → `[0 .. 1 000 090.00]` · upper bound unsafe
`surcharge` max exceeds available headroom in `invoice` · tighten `surcharge` bounds
🔬 `invoice ∈ [0 .. 999 990.00]` (declared) · headroom: `9.99`
🔬 `surcharge ∈ [0 .. 100.00]` (declared) · exceeds headroom by `90.01`
```

**Data sources:** `TypedBinaryOp` expression span; `ProofObligation.Disposition != Proved`; `ComputedInterval` carrying operand intervals and bound violation details.

### 6.5 Unbounded Field (Implicit `[−∞ .. +∞]`)

A numeric field with no declared bounds. The interval solver cannot make any safe claims about expressions that include it.

```md
⚠️ Gap · `adjustment` has no declared bounds
🔬 Interval: `[−∞ .. +∞]` · arithmetic with this field can't be proven safe
Declare `min` / `max` to enable interval proof · fallback: runtime overflow check
```

**Reading:** Line 1 states the gap plainly — no bounds, no proof. Line 2 shows what the solver sees: unbounded input propagates unbounded output. Line 3 offers the fix path and names the fallback behavior.

**Routing note:** An unbounded field hover always shows `⚠️ Gap`, not `⚡ Enforced`, even though a runtime check exists. The hover reflects the static proof status, not the runtime safety net.

**Data sources:** Field has no `min`/`max` modifiers; `ProofObligation.Disposition != Proved` due to unbounded operand; `ComputedInterval == null` or `IsUnbounded`.

### 6.6 Optional Field (Interval Carries Presence Uncertainty)

An optional field where the interval applies only when the field is present.

```md
⚠️ Gap · `discount` is optional · interval applies only when present
🔬 When present: `[0 .. 50.00]` · when absent: no value, no interval
`totalPrice − discount` requires presence guard before arithmetic
```

**Reading:** Line 1 states the gap type: presence uncertainty and its interval implication. Line 2 shows the dual-path interval. Line 3 names the specific expression at risk and what's needed.

**Routing note:** This card variant combines an interval gap with a presence gap. If both `PRE0116` (presence not confirmed) and an interval gap fire on the same expression, the diagnostic squiggle card wins (routing rule 1). This template applies when no diagnostic has fired but the hover is on the field declaration itself.

**Data sources:** `TypedField.IsOptional == true`; `ProofObligation.Disposition != Proved` with optional-field origin; presence proof not confirmed on all uses.

### 6.7 Diagnostic Squiggle — Numeric Overflow

When `NumericOverflow` (error) is emitted by Strategy 7, the existing diagnostic squiggle hover template applies:

```md
⚠️ `PRE0XXX` · Arithmetic result may overflow declared bounds
🔬 `balance = balance - amount`
Result interval [−50 000 .. 999 999 999.99] · declared [0 .. 999 999 999.99]
Lower bound violated: result.Min = −50 000
```

**Integration point:** `ConstraintInfluenceEntry` and `ProofLedger.Obligations` provide the data. No new hover card kind is required. The 🔬 line gains an interval sub-line — a format extension within the existing card template.

**Routing:** Proof diagnostic span wins (routing rule 1). Interval overflow diagnostics are proof diagnostics and route accordingly.

**Data sources:** `Diagnostic.Code == NumericOverflow`; matching `ProofObligation` with `ComputedInterval` and bound violation details.

### 6.8 Badge Usage for Intervals

No new icons are introduced. The existing vocabulary extends naturally:

| Badge | Interval role |
|-------|---------------|
| ✅ | Result interval fits the target bound — proof is complete |
| ⚠️ | Result interval escapes the target bound, or operand is unbounded |
| 🔬 | Arithmetic chain, propagation step, or interval reasoning |
| ⚖️ | Declared bounds on a field (bounds are a comparison contract: `x ≥ min AND x ≤ max`) |

`⚖️` already means "currency, unit, or comparison contract." Declared numeric bounds are a comparison contract.

### 6.9 Compactness Rules for Intervals

The V7 compactness contract applies unchanged: **design for 3 lines, use lines 4–5 only when proof is the user's actual question.**

| Scenario | Default lines | Expanded lines |
|----------|--------------|----------------|
| Declared bounds, proven | 3 | Not applicable |
| Declared bounds, gap | 3 | 5 (when expression detail matters) |
| Computed expression, proven | 3 | Not applicable |
| Computed expression, gap | 3 | 5 (when overflow arithmetic matters) |
| Unbounded field | 3 | Not applicable |
| Optional field | 3 | Not applicable |

**Line 1 always carries the verdict:** `✅ Proven` or `⚠️ Gap` — never buried.

**The repair hint belongs on line 3** for gap cases, not in an expanded-only view. Users need to know what to do without expanding. Expanded view adds the mathematical detail, not the instruction.

**Numbers in intervals:** Use actual numeric values, not code variable names. `[0 .. 50 000]` not `[minBalance .. maxBalance]`. The hover is showing the evaluated fact.

### 6.10 Routing Rules Extension

These additions layer on top of the existing routing rules in V7 § 4. Priority ordering is unchanged.

1. **Proof diagnostic span wins** — interval-overflow diagnostics (`NumericOverflow`) are proof diagnostics and win at rule 1.
2. **Smallest proof-bearing `TypedBinaryOp` wins** — if the cursor is on an arithmetic expression with an interval result, the interval proof-expression hover fires at rule 2.
3. **Field declaration hovers** — when the cursor is on a field with declared bounds and no expression is in scope, the standard field card shows bound information inline (Template 6.1 or 6.5).
4. **Unbounded field vs. bounded field** — both show on the field declaration hover; the unbounded template (6.5) fires whenever `Interval == null` or `Interval.IsUnbounded`.
5. **Optional field interval** — fires only on the field declaration itself when no expression hover is active and presence is uncertain. Does not replace the diagnostic squiggle hover when `PRE0116` is active.

### 6.11 V1 Boundary for Interval Hovers

**Available in V1 (when interval proof ships):**
- `ProofLedger.Obligations` with kind `IntervalContainmentProofRequirement` (declared `Interval` on the field)
- Proof result: whether the interval obligation was satisfied or not (`Disposition`)
- The specific gap type (unbounded operand vs. result overflow vs. presence uncertainty)
- Operand intervals for the failing expression
- Templates 6.1–6.6 in compact form (3 lines)

**Not available in V1:**
- Step-by-step propagation chains (expanded view, lines 4–5) — requires the solver to expose its intermediate steps, not just the final result. Deferred to V2/Slice 3.
- Cross-field bounds expressions (Tier 3 inference) — deferred to future work.
- Guard-narrowed interval display (guard narrows `x`'s lower bound mid-path) — deferred to Slice 3.
- Per-operand gap breakdown when more than two operands contribute to overflow — deferred to V2 as part of expanded propagation.

**V1 hover behavior:** Cards show Templates 6.1–6.6 (compact, 3-line). Expanded propagation chains (Elaine's § 5 Expanded view) are a V2 surface, contingent on `ProofEngine` exposing intermediate intervals.

### 6.12 What Must NOT Change

- Badge vocabulary (`✅`, `⚡`, `⚠️`, `🔬`) — unchanged.
- Routing rules (§4 of hover-design.md) — proof diagnostic span wins; smallest proof-bearing `TypedBinaryOp` wins next. `IntervalContainment` proof obligations follow these exact same routing rules.
- V1 boundary — interval hover data is available in V1 if `ProofLedger.Obligations` carries the resolved/unresolved interval obligations. No new runtime surface is needed.

**Kramer (language server dev) has this as the implementation spec. Elaine's design review is complete.**

---

## 7. Philosophy Alignment

### 7.1 Prevention, Not Detection

The philosophy states: "Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities."

Interval-based proof makes this true for overflow on bounded fields. For unbounded fields, the runtime fallback remains — which is honest, because those fields are not structurally constrained and overflow is not proven impossible. The philosophy's absolute claim is satisfied for bounded fields; unbounded fields remain in a weaker (runtime-caught) regime until authors add bounds.

**The product must not claim "overflow is impossible" globally until every numeric field is bounded.** The hover display (§6) must distinguish `✅ Proven` (bounded field, interval proof discharged) from `⚡ Enforced` (unbounded field, runtime check). The distinction is inspectable — Precept's determinism guarantee holds.

### 7.2 Determinism

Interval arithmetic is deterministic: same definition, same bounds, same computed intervals. The strategy produces a definitive `Proved`/`Unresolved` verdict per obligation, no probabilistic reasoning, no timeouts. This is exactly what Precept requires.

### 7.3 Inspectability

Every interval proof result is visible in `ProofLedger.Obligations`. The computed interval (both the result interval and the bounds it was checked against) is stored in the obligation record and projected to the hover card. Nothing is hidden. Authors can inspect exactly why an obligation was proved or left unresolved — with the interval values that drove the decision.

**Obligation metadata enrichment:** `IntervalContainmentProofRequirement` carries `DeclaredMin`/`DeclaredMax`. The proof engine, on Strategy 7 resolution, enriches the `ProofObligation` with a new field: `ComputedInterval` (a nullable `NumericInterval`). This is stored on `ProofObligation` and projected to the language server.

### 7.4 One-File Completeness

No new file is required for interval bounds. The bounds are declared on the field using existing `min`/`max` modifiers — exactly where they belong in Precept's one-file contract model.

### 7.5 AI Legibility

The `IntervalContainmentProofRequirement` record carries `TargetField`, `DeclaredMin`, `DeclaredMax`, and `Description` — all structured, all machine-consumable. The `ComputedInterval` on `ProofObligation` is a flat struct. MCP tool consumers (`precept_compile`) can read the interval proof status from the proof ledger without any contextual human knowledge.

---

## 8. Implementation Plan

> **Design review gate:** No implementation starts until Shane signs off on this document. The implementation plan below is presented for planning review only.

### 8.1 File Inventory

| File | Status | Role |
|---|---|---|
| `src/Precept/Language/ProofRequirementKind.cs` | Modify | Add `IntervalContainment = 7` |
| `src/Precept/Language/ProofRequirement.cs` | Modify | Add `IntervalContainmentProofRequirement` record + `ProofSatisfaction.IntervalContainment` |
| `src/Precept/Language/ProofRequirements.cs` | Modify | Add exhaustive switch arm + `All` member |
| `src/Precept/Language/Operations.cs` | Modify | Add `IntervalTransfer` delegate + populate for `+`, `−`, `×`, `÷`, unary negate on decimal/number |
| `src/Precept/Language/Operation.cs` (or equivalent) | Modify | Add `IntervalTransferFn?` field to `BinaryOperationMeta` |
| `src/Precept/Language/Actions.cs` | Modify | Add `ProofRequirementKind.IntervalContainment` to `set` action's proof requirements |
| `src/Precept/Pipeline/ProofLedger.cs` | Modify | Add `ComputedInterval` (nullable) to `ProofObligation`; add `ProofStrategy.IntervalContainment = 7` |
| `src/Precept/Pipeline/ProofEngine.cs` | Modify | Hook Strategy 7 into strategy resolution loop |
| `src/Precept/Pipeline/ProofEngine.Intervals.cs` | **Create** | `NumericInterval` struct + `IntervalOf` traversal + `TryIntervalContainmentProof` |
| `src/Precept/Pipeline/ProofEngine.Strategies.cs` | Modify | Add guard-narrowing integration (Slice 3) |
| `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` | Modify | Extend proof expression hover with interval sub-line (Slice 5) |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | Assess | Verify `precept_compile` interval obligation projection |
| `test/Precept.Tests/ProofEngineIntervalTests.cs` | **Create** | All interval unit tests |
| `test/Precept.LanguageServer.Tests/HoverHandlerIntervalTests.cs` | **Create** | Interval hover regression tests |
| `src/Precept/Language/Diagnostics.cs` (or `DiagnosticCode.cs`) | Modify | Add `BoundsRequireQualifier`, `BoundsQualifierMismatch` codes (Slices 8, 10) |
| `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` | Modify | Extend `TryGetComparableModifierValue` for typed constants (Slice 9); qualifier validation (Slices 8, 10) |
| `test/Precept.Tests/TypeFamilyCoverageTests.cs` | **Create** | Cross-type-family constraint coverage regression (Slice 13) |
| `src/Precept/Pipeline/ProofEngine.cs` | Modify | Extend `WalkExpression` / `WalkActions` to inject `PresenceProofRequirement` for optional field refs (Slice 12) |
| `src/Precept/Language/FaultCode.cs` | Modify | Redirect `FaultCode.UnexpectedNull` `[StaticallyPreventable]` from PRE0019 to PRE0116 (Slice 12) |
| `test/Precept.Tests/ProofEnginePresenceTests.cs` | **Create** | Presence obligation generation + discharge tests (Slice 12) |

### 8.2 Vertical Slices

> **Dependency order:** Slice 1 → Slice 2 → Slice 3 → Slice 4 (independent) → Slice 5 → Slice 6

---

#### Slice 1 — Catalog Foundation + `NumericInterval` struct

**Objective:** Add `IntervalContainment` to the proof requirement catalog; implement the `NumericInterval` struct and basic arithmetic. No proof engine wiring yet.

**Files:**
- `src/Precept/Language/ProofRequirementKind.cs` — add `IntervalContainment = 7`
- `src/Precept/Language/ProofRequirement.cs` — add `IntervalContainmentProofRequirement`, `ProofSatisfaction.IntervalContainment`
- `src/Precept/Language/ProofRequirements.cs` — add switch arm, `All` entry
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` (CREATE) — `NumericInterval` struct with: `Min`, `Max`, `IsUnbounded`, `IsEmpty`, `Add`, `Subtract`, `Multiply`, `Divide`, `Negate`, `Contains(other)`, `Union(other)`
- `src/Precept/Pipeline/ProofLedger.cs` — add `ProofStrategy.IntervalContainment = 7`

**Tests (new file: `ProofEngineIntervalTests.cs`):**
- `NumericInterval_Add_PositivePositive_ReturnsSum`
- `NumericInterval_Add_PositiveNegative_ReturnsCorrectBounds`
- `NumericInterval_Subtract_SwapsMaxMin`
- `NumericInterval_Multiply_FourCornerCases` (4 sign combinations)
- `NumericInterval_Divide_DivisorContainsZero_ReturnsUnbounded`
- `NumericInterval_Divide_AllPositive_ReturnsCorrectBounds`
- `NumericInterval_Contains_ProperSubset_ReturnsTrue`
- `NumericInterval_Contains_ExceedsMax_ReturnsFalse`
- `NumericInterval_Contains_BelowMin_ReturnsFalse`
- `NumericInterval_Unbounded_PropagatesThrough_Add`
- `NumericInterval_Unbounded_PropagatesThrough_Multiply`
- `NumericInterval_Empty_ContainsAnything_ReturnsTrue`
- `NumericInterval_PointInterval_ContainsItself`

**Regression anchors:** All existing `ProofRequirements` tests must pass unchanged. CS8509 exhaustive switch enforcement catches missing arms at build time.

**Completion gate:** `dotnet build` clean; all new unit tests pass; no changes to `ProofEngine.cs` dispatch yet.

---

#### Slice 2 — Obligation Collection + Strategy 7 Wiring (decimal only)

**Objective:** Wire the proof engine to generate `IntervalContainmentProofRequirement` obligations for `set` actions on bounded `decimal` fields; implement Strategy 7 dispatch; emit `NumericOverflow` on unresolved.

**Files:**
- `src/Precept/Language/Operation.cs` — add `IntervalTransferFn? IntervalTransfer` to `BinaryOperationMeta`
- `src/Precept/Language/Operations.cs` — add static transfer functions (`AddTransfer`, `SubtractTransfer`, `MultiplyTransfer`, `DivideTransfer`) and populate in `decimal` operation entries
- `src/Precept/Language/Actions.cs` — add `ProofRequirementKind.IntervalContainment` to `set`'s proof requirements
- `src/Precept/Pipeline/ProofEngine.cs` — add strategy 7 dispatch in resolution loop
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` — add `IntervalOf(TypedExpression, SemanticIndex)` traversal + `TryIntervalContainmentProof`
- `src/Precept/Pipeline/ProofLedger.cs` — add `NumericInterval? ComputedInterval` to `ProofObligation`

**Method-level specificity for `IntervalOf`:**
```csharp
private static NumericInterval IntervalOf(TypedExpression expr, SemanticIndex semantics)
    // TypedLiteral(decimal) → [value, value]
    // TypedLiteral(integer) → [value, value]
    // TypedFieldRef → ExtractFieldInterval(fieldName, semantics)
    // TypedEventArgRef → ExtractArgInterval(argName, semantics)
    // TypedBinaryOp → apply BinaryOperationMeta.IntervalTransfer(left, right)
    //                  or Unbounded if IntervalTransfer is null
    // TypedFunctionCall → Unbounded (FunctionOverload.IntervalTransfer deferred to Slice 4)
    // TypedConditional → Union of then/else intervals
    // _ → Unbounded (safe default)
```

**Tests:**
- `IntervalOf_Literal_ReturnsPointInterval`
- `IntervalOf_BoundedField_ReturnsDeclaredBounds`
- `IntervalOf_UnboundedField_ReturnsUnbounded`
- `IntervalOf_Add_TwoBoundedFields_ReturnsSum`
- `IntervalOf_Subtract_CanGoNegative_ReturnsNegativeBound`
- `IntervalOf_Multiply_BothBounded_ReturnsFourCorner`
- `IntervalContainment_BothBoundsDeclairedAndFit_Proved`
- `IntervalContainment_MaxExceeded_EmitsNumericOverflow`
- `IntervalContainment_MinViolated_EmitsNumericOverflow`
- `IntervalContainment_OnlyMaxDeclared_ChecksMaxOnly`
- `IntervalContainment_OnlyMinDeclared_ChecksMinOnly`
- `IntervalContainment_NoBoundsDeclared_NoObligationGenerated`
- `IntervalContainment_UnboundedFieldRef_EmitsNumericOverflow`
- `IntervalContainment_IntegerTarget_NoObligationGenerated`

**Regression anchors:**
- All existing `ProofEngineTests.cs` tests pass — obligation collection for existing `Numeric`/`Presence`/`QualifierCompatibility` obligations must be unaffected
- Existing `NumericProofRequirement` (threshold-style) obligations are unaffected by Strategy 7
- `DivisionByZero` proof still works (separate obligation, separate strategy)

**Completion gate:** `dotnet test test/Precept.Tests/` clean; `NumericOverflow` is now emitted on provably-overflowing bounded-field assignments in decimal operations.

---

#### Slice 3 — Guard-Narrowing Integration

**Objective:** Extend `IntervalOf` to narrow field intervals using guard constraints extracted from the current obligation context. Fields with guards like `require x >= 100m` get their interval lower-bound raised to `max(declared.Min, 100m)` within that context.

**Files:**
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` — add `NarrowedIntervalOf(TypedExpression, SemanticIndex, ObligationContext)` that calls `ExtractGuardBranches` (already in Strategies.cs), narrows field intervals per guard constraint, then delegates to `IntervalOf`

**Method-level specificity:**
```csharp
private static NumericInterval NarrowedIntervalOf(
    TypedExpression expr, SemanticIndex semantics, ObligationContext ctx)
{
    // Extract guard constraints from ctx (TransitionRowContext, StateHookContext)
    // Build narrowed interval environment: fieldName → narrowed NumericInterval
    // Call IntervalOf substituting narrowed intervals for field refs
}
```

**Tests:**
- `NarrowedIntervalOf_GuardNarrowsLowerBound_FitAfterNarrowing_Proved`
- `NarrowedIntervalOf_GuardNarrowsUpperBound_FitAfterNarrowing_Proved`
- `NarrowedIntervalOf_GuardInsufficientToNarrow_StillFails_Unresolved`
- `NarrowedIntervalOf_NoGuard_FallsBackToDeclarationBounds`
- `NarrowedIntervalOf_DisjunctiveGuard_AllBranchesMustProve` (Strategy 3 parity)

**Regression anchors:**
- Guard-in-Path proof (Strategy 3) still fires for `Numeric` threshold obligations — Slice 3 only adds narrowing to the interval path, does not alter Strategy 3 logic

**Completion gate:** Example from `LineItem.precept`: `require discountPercent <= 1m` guard narrows discount interval; `lineTotal = lineTotal * (1 - discountPercent)` obligation proves.

---

#### Slice 4 — `number` Type + Unary Negation + Function Intervals

**Objective:** Extend interval transfer to `number` (IEEE 754) operations (with ULP widening for multiply/divide) and unary negation; add `IntervalTransferFn` to `FunctionOverload` for key builtins.

**Files:**
- `src/Precept/Language/Operations.cs` — add `number`-specific transfer functions with ULP widening for `×`/`÷`
- `src/Precept/Language/Operation.cs` — add unary-op transfer function surface (or extend existing unary-op metadata)
- `src/Precept/Language/Functions.cs` (or equivalent) — add `IntervalTransferFn? IntervalTransfer` to `FunctionOverload`; populate for `abs`, `round`, `floor`, `ceiling`, `min`, `max` built-ins
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` — extend `IntervalOf` to handle `TypedFunctionCall` via `FunctionOverload.IntervalTransfer`

**Tests:**
- `IntervalOf_Number_MultiplyWithUlpWidening_WiderThanDecimal`
- `IntervalOf_UnaryNegate_SwapsSignedBounds`
- `IntervalOf_Abs_MixedSignInterval_CorrectPositiveBounds`
- `IntervalOf_Round_PreservesDecimalBounds`
- `IntervalOf_Min_TwoIntervals_ReturnsIntersectLower`
- `IntervalOf_Max_TwoIntervals_ReturnsUnionUpper`
- `IntervalOf_UnknownFunction_ReturnsUnbounded` (safe default)

**Regression anchors:** `decimal` proofs from Slice 2 unaffected; `number` fields with bounds now get proofs.

---

#### Slice 5 — Language Server Hover Extension

**Objective:** Extend `RichHoverFactory` to render interval proof results on field hovers and proof expression hovers. Extends V7 hover — no new card kinds.

**Files:**
- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` — extend `CreateFieldHover` and `CreateProofExpressionHover` with interval evidence lines
- `tools/Precept.LanguageServer/` — ensure `ProofLedger.Obligations` `ComputedInterval` is accessible to hover factory

**Elaine coordination required:** Exact card format (§6 of this document) must be signed off before implementation.

**Tests (`HoverHandlerIntervalTests.cs`):** (≥15 required — see §9.1 F for the full breakdown)

*Template coverage (one per template variant, 4.1–4.6):*
- `FieldHover_BoundedField_ProvedInterval_Template41_ShowsProvenBadge` — template 4.1: bounded field, interval proved
- `FieldHover_BoundedField_UnresolvedInterval_Template42_ShowsGapBadge` — template 4.2: bounded field, interval unresolved
- `FieldHover_UnboundedField_Template43_ShowsEnforcedBadge` — template 4.3: no interval line (no bounds declared)
- `ProofExpressionHover_ProvedInterval_Template44_ShowsIntervalSubLine` — template 4.4: proof-expression hover, proved
- `ProofExpressionHover_UnresolvedInterval_Template45_ShowsViolatedBound` — template 4.5: proof-expression hover, unresolved
- `FieldHover_OptionalBoundedField_Template46_ShowsCombinedGap` — template 4.6: optional + bounded field combined gap

*Routing priority:*
- `DiagnosticSquiggle_NumericOverflow_BeatsFieldHover_RoutingPriority` — squiggle hover wins over field hover when both applicable
- `FieldHover_NoDiagnostic_RoutesToFieldCard_NotSquiggle` — clean field routes to field card, not diagnostic card

*Interval notation format:*
- `FieldHover_IntervalNotation_UsesSquareBracketDotDotFormat` — rendered string matches `[0 .. 999 999 999.99]`
- `FieldHover_IntervalNotation_ThinSpaceThousandsSeparator` — thousands separator rendered as thin space (U+2009), not comma

*Badge distinction (declared vs. inferred):*
- `FieldHover_DeclaredBounds_ShowsScalesBadge` — `⚖️` badge when bounds come from declared `min`/`max`
- `FieldHover_InferredInterval_ShowsMicroscopeBadge` — `🔬` badge when interval is engine-computed, not author-declared

*V1 boundary:*
- `FieldHover_V1_ExpandedView_DoesNotAppear` — expanded view (5-line cap) absent in V1; confirms V2 gate

*Diagnostic squiggle:*
- `DiagnosticSquiggle_NumericOverflow_ShowsIntervalLine` — full diagnostic card includes interval value sub-line
- `DiagnosticSquiggle_NumericOverflow_IdentifiesViolatedBound` — upper vs. lower bound violation spelled out separately

**Regression anchors:** All existing `HoverHandlerTests.cs` tests — qualifier hover, state B4 graph narrative, presence hover — must pass unchanged.

---

#### Slice 6 — MCP Sync Assessment + Tooling Propagation

**Objective:** Verify `precept_compile` exposes interval proof obligations; update `LanguageTool.cs` if DTOs need changes.

**Files:**
- `tools/Precept.Mcp/Tools/LanguageTool.cs` — assess `ProofObligation` DTO projection; if `ComputedInterval` is not serialized, add it
- `tools/Precept.Mcp/Tools/` — verify `IntervalContainment` obligation kind appears in `precept_compile` output

**Assessment criteria:**
- Does `precept_compile` include `ProofObligation` records in its output? If yes, do they now include the `IntervalContainment` kind? If the output omits obligations, this is a DTO gap that should be logged as a separate issue.
- Does `precept_language` enumerate `ProofRequirementKind.IntervalContainment` in its vocabulary output? (It derives from `ProofRequirements.All` — automatic if the catalog entry is added correctly.)

**Tests (`Precept.Mcp.Tests/`):**
- `LanguageTool_ProofRequirements_IncludesIntervalContainment`
- `CompileTool_BoundedFieldOverflow_ObligationKindIsIntervalContainment`

**Regression anchors:** Existing MCP `precept_language` vocabulary output unchanged except for new `IntervalContainment` member.

---

#### Slice 7 — Catalog-Driven Obligation Generator Refactor

**Objective:** Replace hardcoded `TypeKind` checks in obligation collection with catalog-driven generation. After this slice, any field with declared constraint modifiers that carry `ProofSatisfactions` entries generates proof obligations regardless of its `TypeKind`.

**Files:**
- `src/Precept/Pipeline/ProofEngine.cs` (lines 283–298) — replace `if (type == TypeKind.Decimal || type == TypeKind.Number)` with modifier-metadata-driven collection: iterate field modifiers, check for `ProofSatisfaction.Numeric` entries, generate `IntervalContainmentProofRequirement` when found
- `src/Precept/Language/Actions.cs` — verify `set` action's `ProofRequirements` list drives collection generically (remove any residual type-specific filtering)

**Method-level specificity:**
```csharp
// In ProofEngine.cs obligation collection:
// BEFORE (hardcoded):
//   if (targetField.ResolvedType.Kind is TypeKind.Decimal or TypeKind.Number)
//       if (min.HasValue || max.HasValue) → emit obligation
//
// AFTER (catalog-driven):
//   foreach (var modifier in targetField.Modifiers)
//       var meta = Modifiers.GetMeta(modifier);
//       if (meta is ValueModifierMeta vmm
//           && vmm.ProofSatisfactions.OfType<ProofSatisfaction.Numeric>().Any())
//           → extract bounds from ProofSatisfactions → emit obligation
```

**Tests:**
- `ObligationCollection_MoneyFieldWithBounds_GeneratesIntervalObligation`
- `ObligationCollection_QuantityFieldWithBounds_GeneratesIntervalObligation`
- `ObligationCollection_PriceFieldWithBounds_GeneratesIntervalObligation`
- `ObligationCollection_ExchangeRateFieldWithBounds_GeneratesIntervalObligation`
- `ObligationCollection_DecimalFieldWithBounds_StillGeneratesObligation` (regression)
- `ObligationCollection_NumberFieldWithBounds_StillGeneratesObligation` (regression)
- `ObligationCollection_IntegerField_NoObligationGenerated` (regression — BigInteger cannot overflow)
- `ObligationCollection_FieldWithNoConstraintModifiers_NoObligation`

**Acceptance criteria:**
- Zero `TypeKind` switches remain in obligation collection path
- All existing Slice 2 tests pass unchanged
- New tests confirm obligation generation for `money`, `quantity`, `price`, `exchangerate`

**Regression anchors:** All Slice 2 tests (`IntervalContainmentStrategyTests.cs`), all existing proof engine tests.

**Completion gate:** `dotnet test test/Precept.Tests/` clean; `money`/`quantity`/`price`/`exchangerate` fields with declared `min`/`max` generate `IntervalContainmentProofRequirement`.

---

#### Slice 8 — Qualified-Type Bound Semantics (Required Qualifiers)

**Objective:** Enforce that `min`/`max` on qualified types (`money`, `quantity`, `price`) require a matching qualifier context (`in`/`of`). Emit `BoundsRequireQualifier` diagnostic when bounds are declared without the required qualifier.

**Files:**
- `src/Precept/Language/Diagnostics.cs` (or `DiagnosticCode.cs`) — add `BoundsRequireQualifier` diagnostic code
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` — add validation: when field type is a qualified type and `min`/`max` modifier is present, verify the field has the required qualifier modifier (`in` for money/price, `in` or `of` for quantity)
- `src/Precept/Language/DiagnosticCatalog.cs` (or equivalent) — register diagnostic metadata

**Tests:**
- `BoundsRequireQualifier_MoneyWithoutIn_EmitsDiagnostic`
- `BoundsRequireQualifier_MoneyWithIn_NoDiagnostic`
- `BoundsRequireQualifier_QuantityWithoutInOrOf_EmitsDiagnostic`
- `BoundsRequireQualifier_QuantityWithIn_NoDiagnostic`
- `BoundsRequireQualifier_QuantityWithOf_NoDiagnostic`
- `BoundsRequireQualifier_PriceWithoutIn_EmitsDiagnostic`
- `BoundsRequireQualifier_PriceWithIn_NoDiagnostic`
- `BoundsRequireQualifier_DecimalWithBoundsNoQualifier_NoDiagnostic` (regression — decimal doesn't require qualifier)
- `BoundsRequireQualifier_ExchangeRateWithBounds_NoDiagnostic` (exchangerate qualifiers have different semantics — verify)

**Acceptance criteria:**
- `field cost as money max 100000` emits `BoundsRequireQualifier` error
- `field cost as money in 'USD' max '100000 USD'` compiles clean
- No false positives on `decimal`/`number` fields

**Completion gate:** `dotnet test` clean; all new diagnostics fire on invalid qualifier-less bounds.

---

#### Slice 9 — Typed-Constant Bound Extraction

**Objective:** Extend `TryGetComparableModifierValue` to extract numeric values from typed constants (e.g., `'5 kg'` → `5`, `'100 USD'` → `100`), populating `DeclaredMin`/`DeclaredMax` for interval proof obligations.

**Files:**
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` — extend `TryGetComparableModifierValue` (lines 211–226) to handle typed-constant expressions:
  1. Detect single-quoted literal expressions
  2. Use existing typed-constant resolution pipeline to parse
  3. Extract numeric component as `decimal`
  4. Store extracted qualifier for compatibility check (Slice 10)
- `src/Precept/Pipeline/TypeChecker.cs` — ensure `DeclaredMin`/`DeclaredMax` are populated from typed-constant extraction results (lines 378–383)

**Method-level specificity:**
```csharp
// In TryGetComparableModifierValue:
// Existing: handles NumberLiteral, UnaryMinus+NumberLiteral
// NEW: handle TypedConstantExpression:
//   1. Resolve via ResolveTypedConstant(expression)
//   2. If resolved, extract numeric component (e.g., '5 kg' → 5m)
//   3. Return the numeric value as the comparable value
//   4. Store qualifier ('kg') on a new ExtractedBoundQualifier field
```

**Tests:**
- `TryGetComparableModifierValue_TypedConstantWithUnit_ExtractsNumericValue` (`'5 kg'` → `5`)
- `TryGetComparableModifierValue_TypedConstantWithCurrency_ExtractsNumericValue` (`'100 USD'` → `100`)
- `TryGetComparableModifierValue_TypedConstantNegative_ExtractsNegativeValue` (`'-50 USD'` → `-50`)
- `TryGetComparableModifierValue_TypedConstantWithDecimals_ExtractsDecimalValue` (`'99.99 USD'` → `99.99`)
- `TryGetComparableModifierValue_PlainNumberLiteral_StillWorks` (regression)
- `TryGetComparableModifierValue_UnaryMinusNumberLiteral_StillWorks` (regression)
- `TryGetComparableModifierValue_InvalidTypedConstant_ReturnsNull`
- `IntervalObligation_MoneyFieldWithTypedConstantBounds_GeneratesCorrectBounds`
- `IntervalObligation_QuantityFieldWithTypedConstantBounds_GeneratesCorrectBounds`

**Acceptance criteria:**
- `field weight as quantity in 'kg' max '5 kg'` → `DeclaredMax = 5`, interval obligation generated
- `field cost as money in 'USD' min '0 USD' max '100000 USD'` → `DeclaredMin = 0, DeclaredMax = 100000`
- Plain numeric bounds (`min 0 max 100`) still work (regression)

**Completion gate:** `dotnet test` clean; typed-constant bounds produce non-null `DeclaredMin`/`DeclaredMax`.

---

#### Slice 10 — Qualifier Compatibility Checks

**Objective:** Enforce that when both a field and its bound carry qualifiers, those qualifiers must be compatible. Emit `BoundsQualifierMismatch` on mismatch.

**Files:**
- `src/Precept/Language/Diagnostics.cs` (or `DiagnosticCode.cs`) — add `BoundsQualifierMismatch` diagnostic code
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` — after typed-constant extraction (Slice 9), compare the bound's extracted qualifier against the field's declared qualifier. Emit `BoundsQualifierMismatch` if they differ.
- `src/Precept/Language/DiagnosticCatalog.cs` (or equivalent) — register diagnostic metadata

**Tests:**
- `BoundsQualifierMismatch_FieldUSD_BoundEUR_EmitsDiagnostic`
- `BoundsQualifierMismatch_FieldKg_BoundLb_EmitsDiagnostic`
- `BoundsQualifierMismatch_FieldUSD_BoundUSD_NoDiagnostic`
- `BoundsQualifierMismatch_FieldKg_BoundKg_NoDiagnostic`
- `BoundsQualifierMismatch_DecimalFieldNoQualifier_PlainNumericBound_NoDiagnostic` (regression)
- `BoundsQualifierMismatch_FieldHasQualifier_BoundIsPlainNumeric_EmitsBoundsRequireQualifier` (delegates to Slice 8 diagnostic)

**Acceptance criteria:**
- `field cost as money in 'USD' max '100 EUR'` emits `BoundsQualifierMismatch`
- `field cost as money in 'USD' max '100 USD'` compiles clean
- Qualifier comparison is case-sensitive and exact (no currency conversion)

**Completion gate:** `dotnet test` clean; qualifier mismatches diagnosed.

---

#### Slice 11 — String/Collection Constraint Obligation Coverage

**Objective:** Extend obligation generation to `string` (`minlength`/`maxlength`) and `collection` (`mincount`/`maxcount`) constraint modifiers. These use analogous but distinct obligation types (`LengthContainment`, `CountContainment`) because the proof semantics differ from numeric interval arithmetic.

**Files:**
- `src/Precept/Language/ProofRequirementKind.cs` — add `LengthContainment` and `CountContainment` (if separate obligation kinds are warranted; alternatively, generalize `IntervalContainment` to cover length/count with a discriminator)
- `src/Precept/Language/ProofRequirement.cs` — add obligation records for length/count containment
- `src/Precept/Pipeline/ProofEngine.cs` — extend obligation collection: when a `set` target field has `minlength`/`maxlength` or `mincount`/`maxcount` modifiers with `ProofSatisfactions`, generate the corresponding obligation
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` (or new `ProofEngine.Lengths.cs`) — length/count containment strategy

**Tests:**
- `StringField_MaxLength_SetToLongerLiteral_EmitsDiagnostic`
- `StringField_MaxLength_SetToShorterLiteral_Proved`
- `StringField_MinLength_SetToEmptyLiteral_EmitsDiagnostic`
- `StringField_NoBoundsModifiers_NoObligation`
- `CollectionField_MaxCount_AddBeyondLimit_EmitsDiagnostic` (if add semantics are provable)
- `CollectionField_MinCount_RemoveToEmpty_EmitsDiagnostic` (if remove semantics are provable)
- `CollectionField_NoBoundsModifiers_NoObligation`

**Note:** String/collection proofs are inherently more limited than numeric interval proofs — many operations are not statically provable (e.g., concatenation length depends on runtime values). The initial coverage should handle:
- Literal assignments where the value length/count is known at compile time
- Leave non-literal assignments as unresolved (conservative, sound)

**Acceptance criteria:**
- Declared `maxlength`/`minlength` on a string field generates an obligation on `set` actions
- Literal string assignments are checked against declared length bounds
- Collection constraints generate obligations where provable

**Completion gate:** `dotnet test` clean; no string/collection constraint is silently ignored.

---

#### Slice 12 — Presence Obligation Generation

**Objective:** Enhance `ProofEngine.WalkExpression` (and/or `WalkActions`) to inject `PresenceProofRequirement` when a `TypedFieldRef` references a field with `IsOptional == true` (checked via `SemanticIndex.FieldsByName`, where `field.Presence is DeclaredPresenceMeta.Optional`) in a value position. The existing discharge infrastructure (Strategy 2 via `DeclaredPresenceMeta.Guaranteed` + `ProofSatisfaction.Presence`, Strategy 3 via `when X is set` guard-in-path, and Strategy 5 via qualifier compatibility) plus PRE0116 `UnprovedPresenceRequirement` diagnostic emission already handle the downstream path — this slice only generates the obligations that feed into it.

**Why this belongs here:** Same architectural pattern as all other obligation generation slices — teach the expression walker to generate a new class of obligations from catalog/semantic metadata, then existing discharge + diagnostic infrastructure handles the rest. Zero new diagnostic codes needed. Zero new strategies needed. Zero new ProofRequirement subtypes needed. The `PresenceProofRequirement` type, `ProofSatisfaction.Presence`, Strategy 2/3 presence arms, and PRE0116 emission are ALL already fully plumbed — they just receive zero obligations today.

**Value positions (obligation triggers):**
1. Binary operation operand (`TypedBinaryOp.Left` or `.Right`)
2. Function argument (`TypedFunctionCall.Arguments[i]`)
3. Set action source expression (the value being assigned)
4. Member access receiver (`TypedMemberAccess.Object`)
5. Rule/ensure condition (field ref in a boolean expression)
6. Interpolation hole (`TypedHoleSegment.Expression`)

**Implementation approach:**
- In `WalkExpression`, when visiting a `TypedFieldRef`, look up the field in `SemanticIndex.FieldsByName`. If `field.Presence is DeclaredPresenceMeta.Optional`, emit a `new PresenceProofRequirement(new SelfSubject(), "Field '{fieldName}' is optional and may be absent")`.
- The obligation context (`ObligationContext`) is already passed through the walk — no changes needed.
- The walker already visits all value-position expression forms; the new check is a leaf-node addition at the `TypedFieldRef` case (currently a no-op in the switch).

**Files:**
- `src/Precept/Pipeline/ProofEngine.cs` — extend `WalkExpression` to add a `case TypedFieldRef fieldRef:` arm (or extend the existing default) that checks optionality and emits `PresenceProofRequirement`. Requires passing `SemanticIndex` through `WalkExpression` (it currently only receives `ObligationContext`).
- `src/Precept/Language/FaultCode.cs` — change `[StaticallyPreventable(DiagnosticCode.NullInNonNullableContext)]` on `FaultCode.UnexpectedNull` to `[StaticallyPreventable(DiagnosticCode.UnprovedPresenceRequirement)]`. This correctly links the runtime fault to its compile-time prevention path (PRE0116, not PRE0019).
- `test/Precept.Tests/ProofEnginePresenceTests.cs` (CREATE) — presence obligation generation tests.

**Tests:**
- `OptionalField_InSetAction_WithoutGuard_GeneratesPRE0116` — an optional field used as a value source in a `set` action without a `when X is set` guard → PRE0116 diagnostic emitted.
- `OptionalField_InSetAction_WithWhenGuard_NoDiagnostic` — same field inside `when X is set { set Y to X }` → Strategy 3 discharges the obligation, no diagnostic.
- `OptionalField_InBinaryOp_WithoutGuard_GeneratesPRE0116` — optional field as operand in `X + Y` without guard → PRE0116.
- `OptionalField_InFunctionArg_WithoutGuard_GeneratesPRE0116` — optional field as function argument → PRE0116.
- `OptionalField_InMemberAccess_WithoutGuard_GeneratesPRE0116` — optional field as `.count` receiver → PRE0116.
- `OptionalField_InRuleCondition_WithoutGuard_GeneratesPRE0116` — optional field in `rule` condition → PRE0116.
- `OptionalField_InInterpolationHole_WithoutGuard_GeneratesPRE0116` — optional field in `"Total: {optionalField}"` → PRE0116.
- `RequiredField_InValuePosition_NoObligation` — non-optional field → no presence obligation generated.
- `OptionalField_GuardedByDeclaration_Strategy2Discharges` — field with `DeclaredPresenceMeta.Guaranteed` (via `required` modifier or equivalent) → Strategy 2 discharges.
- `FaultCode_UnexpectedNull_PointsToPRE0116` — verify `StaticallyPreventableAttribute` on `FaultCode.UnexpectedNull` now references `DiagnosticCode.UnprovedPresenceRequirement`.

**Acceptance criteria:**
- `new PresenceProofRequirement(...)` is constructed in production code for every optional field ref in a value position.
- Guarded access (`when X is set { ... }`) produces zero PRE0116 diagnostics.
- Unguarded access produces PRE0116.
- `FaultCode.UnexpectedNull` `[StaticallyPreventable]` attribute references `DiagnosticCode.UnprovedPresenceRequirement`.

**Completion gate:** `dotnet test` clean; at least one positive and one negative presence obligation test passing.

**Dependencies:** Depends on Slice 7 (catalog-driven obligation generator refactor — already done). Independent of Slices 8–11; can run in parallel.

---

#### Slice 13 — Type-Family Coverage Regression Suite

**Objective:** Comprehensive cross-type-family regression suite ensuring every type family's constraints are covered per the matrix in §12. This is the final validation gate for the catalog-driven obligation architecture.

**Files:**
- `test/Precept.Tests/TypeFamilyCoverageTests.cs` (CREATE) — one test class with per-family test methods

**Tests (one per row in the §12 coverage matrix):**
- `DecimalField_WithBounds_GeneratesIntervalObligation`
- `NumberField_WithBounds_GeneratesIntervalObligation`
- `IntegerField_WithBounds_NoObligation` (BigInteger exemption)
- `MoneyField_WithQualifiedBounds_GeneratesIntervalObligation`
- `QuantityField_WithQualifiedBounds_GeneratesIntervalObligation`
- `PriceField_WithQualifiedBounds_GeneratesIntervalObligation`
- `ExchangeRateField_WithBounds_GeneratesIntervalObligation`
- `StringField_WithLengthBounds_GeneratesLengthObligation`
- `CollectionField_WithCountBounds_GeneratesCountObligation`
- `TemporalField_NoBoundsDeclared_NoObligation`
- **Negative companion per positive test** — field without bounds, same type, no obligation generated

**Meta-test:**
- `AllConstrainableTypes_DeclaredConstraint_NeverSilentlyIgnored` — parameterized test iterating over every type in the `Types` catalog that has `min`/`max`/`minlength`/`maxlength`/`mincount`/`maxcount` in its `ApplicableModifiers`; verifies that a field with the constraint and a `set` action produces either an obligation or a diagnostic.
- `OptionalField_InValuePosition_GeneratesPresenceObligation` — verifies that optional field references in value positions generate `PresenceProofRequirement` obligations.

**Acceptance criteria:**
- 100% coverage of the §12 matrix (including presence checking row)
- No type family has a declared constraint that produces neither an obligation nor a diagnostic
- No optional field in a value position escapes without a presence obligation
- All existing tests pass (full regression)

**Completion gate:** `dotnet test` clean; meta-test `AllConstrainableTypes_DeclaredConstraint_NeverSilentlyIgnored` passes.

**Dependencies:** Depends on Slices 8–12.

---

### 8.3 Dependency Ordering

```
Slice 1 (catalog + NumericInterval)
    ↓
Slice 2 (obligation collection + Strategy 7 wiring — decimal)
    ↓
Slice 3 (guard narrowing)     Slice 4 (number type + functions) [parallel]
    ↓                              ↓
Slice 5 (hover extension — after Elaine design review)
    ↓
Slice 6 (MCP sync)
    ↓
Slice 7 (catalog-driven obligation generator refactor)
    ↓
Slice 8 (qualified-type bound semantics)     Slice 11 (string/collection constraints)  Slice 12 (presence obligations) [all parallel]
    ↓                                             ↓                                         ↓
Slice 9 (typed-constant extraction)               ↓                                         ↓
    ↓                                             ↓                                         ↓
Slice 10 (qualifier compatibility checks)         ↓                                         ↓
    ↓                                             ↓                                         ↓
    └──────────────→ Slice 13 (type-family coverage regression) ←──────────────────────────┘
```

Slice 4 is independent of Slice 3 and can run in parallel once Slice 2 is complete.
Slices 8–10 are sequential (each depends on the prior). Slices 11 and 12 are independent of Slices 8–10 and each other, and can run in parallel after Slice 7. Slice 13 depends on all of Slices 8–12.

### 8.4 Tooling/MCP Sync Assessment

| Surface | Assessment | Action required |
|---|---|---|
| `precept_language` (MCP) | `ProofRequirements.All` auto-derives — `IntervalContainment` will appear when catalog entry is added | **No action needed** — derives automatically |
| `precept_compile` (MCP) | Must verify `ProofObligation` DTO includes `ComputedInterval` | **Assess in Slice 6** |
| Hover (LS) | Extend `RichHoverFactory` — see Slice 5 | **Implement in Slice 5, Elaine review required** |
| TextMate grammar | No new tokens — `min`/`max` modifiers already highlighted | **No action needed** |
| Completions (LS) | No new keywords | **No action needed** |
| Semantic tokens (LS) | No new token categories | **No action needed** |

### 8.5 Breaking Change Assessment

- **No breaking changes to the public `Precept` / `Version` runtime API.** Interval proofs are compile-time only.
- **`ProofObligation` record adds `ComputedInterval` field** — if MCP consumers read `ProofObligation` DTOs directly, this is an additive (non-breaking) change. Verify in Slice 6.
- **`ProofLedger` adds `ComputedInterval` to `ProofObligation`** — internal pipeline type, no public surface. Internal record addition is non-breaking.
- **`BinaryOperationMeta` adds `IntervalTransfer` field** — additive change to internal catalog record. Non-breaking; defaults to null (no transfer function = unbounded, safe).
- **Existing precept definitions with bounded fields may now get new `NumericOverflow` errors** — this IS a semantic breaking change for authors who relied on the runtime fault path. This is intentional and correct — the compiler is now enforcing what it always should have. Authors will need to add guards or adjust expressions. **Document this clearly in release notes.**

---

## 9. Test Strategy

### 9.1 Unit Test Breakdown by Proof-Obligation Type

#### Fixture Strategy — `ParseAndGetSemanticIndex` Helper

Sections B, C, and D require a `SemanticIndex` — a non-trivial object produced by a full compilation pass. Tests in these sections are **unit tests that drive a real parse** on a minimal inline precept string, not integration tests over sample files. All traversal, strategy, and guard-narrowing tests use the following helper, declared once in `test/Precept.Tests/IntervalTestHelpers.cs`:

```csharp
/// <summary>
/// Parses the given precept source, runs the full TypeChecker pass, and returns
/// the SemanticIndex. Throws if the source has unexpected parse or type errors —
/// caller is responsible for providing structurally valid precept text.
/// </summary>
internal static SemanticIndex ParseAndGetSemanticIndex(string preceptSource)
{
    var result = Precept.Compile(preceptSource);
    return result.SemanticIndex
        ?? throw new InvalidOperationException(
               "Compilation produced no SemanticIndex — check precept source for errors.");
}
```

**Why a real parse, not a stub:** `SemanticIndex` has enough internal invariants (resolved field types, modifier proof satisfactions, event handler structure) that a hand-constructed stub would either duplicate that logic or silently omit invariants the interval engine relies on. A real parse on a minimal string is the right fixture — it is still a unit test because the precept under test is defined inline in the test file, not on disk.

**Canonical inline precept constants** (defined in `IntervalTestHelpers.cs` or per-test-file preamble):

```csharp
internal const string BoundedDecimalPrecept = @"
precept BoundedBalance {
    field balance: decimal min 0 max 999999999.99
    event Deposit(amount: decimal min 0 max 50000)
    Active -> Active on Deposit: set balance to balance + amount
}";

internal const string GuardedBoundedPrecept = @"
precept GuardedTransfer {
    field balance: decimal min 0 max 999999999.99
    event Transfer(amount: decimal)
    Active -> Active on Transfer
        require amount >= 1
        require amount <= balance:
        set balance to balance - amount
}";

internal const string MultiSetPrecept = @"
precept MultiFieldUpdate {
    field principal: decimal min 0 max 1000000
    field interest: decimal min 0 max 50000
    field total: decimal min 0 max 1050000
    event Accrue(rate: decimal min 0 max 0.2)
    Open -> Open on Accrue:
        set principal to principal
        set interest to principal * rate
        set total to principal + interest
}";
```

**Usage pattern** for sections B/C/D tests: call `ParseAndGetSemanticIndex(...)`, extract the typed expression under test via a helper like `GetSetActionExpression(semantics, fieldName)`, then pass to the function under test. No test in sections B/C/D depends on external `.precept` files.

---

#### A. `NumericInterval` struct unit tests (no proof engine involvement)

| Test category | Count | Location |
|---|---|---|
| Arithmetic operations (add, subtract, multiply, divide) × sign combinations | 20 | `NumericIntervalArithmeticTests.cs` |
| Unbounded propagation through all operations | 8 | same |
| Empty interval handling | 4 | same |
| Point interval (literal values) | 4 | same |
| `Contains` — subset, equal, exceeds-max, below-min | 6 | same |
| `Union` (for conditional expressions) | 4 | same |
| Unary negation + abs value | 4 | same |

**Total: ~50 struct-level unit tests**

#### B. `IntervalOf` traversal tests

> All tests in this section use `ParseAndGetSemanticIndex` to obtain the `SemanticIndex` fixture. See the fixture strategy above.

| Test category | Count | Location |
|---|---|---|
| Field ref — with bounds | 4 | `IntervalOfTraversalTests.cs` |
| Field ref — without bounds | 2 | same |
| Event arg ref — with/without bounds | 4 | same |
| Binary op composition (decimal) | 10 | same |
| Binary op composition (number, with ULP widening) | 6 | same |
| Function call — known functions | 6 | same |
| Function call — unknown function (unbounded fallback) | 2 | same |
| Conditional expression (then/else union) | 4 | same |
| Nested expression tree (multi-level) | 4 | same |

**Total: ~42 traversal tests**

#### C. Strategy 7 (interval containment) proof obligation tests

> All tests in this section use `ParseAndGetSemanticIndex` to obtain the `SemanticIndex` fixture. See the fixture strategy above.

| Test category | Count | Location |
|---|---|---|
| Obligation generated when target has `max` | 2 | `IntervalContainmentStrategyTests.cs` |
| Obligation generated when target has `min` | 2 | same |
| Obligation generated when target has both | 2 | same |
| No obligation generated when unbounded target | 2 | same |
| No obligation generated for `integer` target | 2 | same |
| Proved: result within both bounds | 6 | same |
| Unresolved: result exceeds max bound | 6 | same |
| Unresolved: result below min bound | 6 | same |
| Unresolved: result contains unbounded | 4 | same |
| `NumericOverflow` diagnostic emitted on unresolved | 4 | same |
| Diagnostic message — upper bound violation format | 2 | same |
| Diagnostic message — lower bound violation format | 2 | same |
| Diagnostic message — includes field name and interval values | 2 | same |

**Total: ~42 proof strategy tests** (up from 38; 5 additional message-content tests)

#### D. Guard-narrowing integration tests

> All tests in this section use `ParseAndGetSemanticIndex` to obtain the `SemanticIndex` fixture (specifically `GuardedBoundedPrecept`). See the fixture strategy above.

| Test category | Count | Location |
|---|---|---|
| Guard narrows lower bound → proof succeeds | 4 | `GuardNarrowingTests.cs` |
| Guard narrows upper bound → proof succeeds | 4 | same |
| Guard insufficient — proof still fails | 4 | same |
| Disjunctive guard — all branches must prove | 4 | same |
| No guard present — falls back to declaration bounds | 2 | same |

**Total: ~18 guard-narrowing tests**

#### E. Negative tests (proofs must NOT fire on valid expressions)

Per the quality bar established in diagnostic coverage enforcement: every positive test (proof fails) must have a companion negative test (proof succeeds on equivalent safe expression).

| Test category | Count | Location |
|---|---|---|
| Companion `CheckExpectingClean` for every `CheckExpectingError` above | 1:1 ratio | Respective partitioned test file |

**Minimum: 30 negative tests** (one per positive case in section C)

#### F. Hover display tests

> These tests live in `test/Precept.LanguageServer.Tests/HoverHandlerIntervalTests.cs`. See §8.2 Slice 5 for the full test list. **Minimum: 15 tests.**

The hover test set must cover: all 6 template variants (4.1–4.6 including combined-gap template 4.6), routing priority (squiggle beats field hover), interval notation format (`[lo .. hi]` with thin-space thousands separator), badge distinction (`⚖️` declared vs. `🔬` inferred), and the V1 boundary (no expanded view). See §8.2 Slice 5 for the named test list at the required depth.

**Minimum: 15 hover tests** (expanded from the original 6)

### 9.2 Integration Test Scenarios

These test full precept definitions through compilation, verifying the proof engine emits (or does not emit) `NumericOverflow` correctly end-to-end. **Do not reference `.precept` sample files** — sample files may not have the right field types or action patterns for these scenarios. All scenarios use inline precept strings defined as named constants at the top of the integration test class.

**Location:** `test/Precept.Tests/ProofEngineIntervalIntegrationTests.cs`

**Inline precept constants** (declare at class level):

```csharp
// Scenario 1 & 2 — bounded decimal field with set actions
private const string BoundedLineItemPrecept = @"
precept LineItemCalc {
    field unitPrice: decimal min 0 max 10000
    field quantity: decimal min 1 max 1000
    field discountRate: decimal min 0 max 0.5
    field lineTotal: decimal min 0 max 10000000
    event Calculate(newQty: decimal min 1 max 1000)
    Draft -> Draft on Calculate:
        set quantity to newQty
        set lineTotal to unitPrice * newQty * (1 - discountRate)
}";

// Scenario 2 variant — discount rate unbounded (no max)
private const string UnboundedDiscountPrecept = @"
precept LineItemCalcBad {
    field unitPrice: decimal min 0 max 10000
    field quantity: decimal min 1 max 1000
    field discountRate: decimal    field lineTotal: decimal min 0 max 10000000
    event Calculate(newQty: decimal min 1 max 1000)
    Draft -> Draft on Calculate:
        set quantity to newQty
        set lineTotal to unitPrice * newQty * (1 - discountRate)
}";

// Scenarios 3 & 4 — guarded bounded fields
private const string GuardedAmountPrecept = @"
precept LoanCalc {
    field balance: decimal min 0 max 1000000
    field payment: decimal min 0 max 10000
    event MakePayment(amount: decimal)
    Active -> Active on MakePayment
        require amount >= 1
        require amount <= balance:
        set balance to balance - amount
        set payment to amount
}";

// Scenario 4 variant — bounds too tight
private const string TooTightBoundsPrecept = @"
precept LoanCalcBad {
    field balance: decimal min 0 max 1000000
    field total: decimal min 0 max 500000
    event AddFees(fee: decimal min 0 max 600000)
    Active -> Active on AddFees:
        set total to balance + fee
}";
```

| Scenario | Precept source | Expected outcome |
|---|---|---|
| Bounded `decimal` field, valid arithmetic | `BoundedLineItemPrecept` | No diagnostics — all interval obligations proved |
| Bounded field, unbounded operand | `UnboundedDiscountPrecept` | `NumericOverflow` on `lineTotal` assignment |
| Guarded bounded field, guards sufficient | `GuardedAmountPrecept` | All interval obligations proved (after Slice 3) |
| Bounds too tight for possible sum | `TooTightBoundsPrecept` | `NumericOverflow` on `total` assignment |
| Computed field, inferable bounds (all operands bounded) | Inline precept: computed field `<-` expression with all bounded operands | No diagnostics |
| Computed field, one unbounded operand | Inline precept: computed field `<-` with one unbounded operand | `NumericOverflow` |
| `integer` field — no interval obligation | Inline precept: `integer` field + `set` action | No interval diagnostics |
| `decimal` field, no bounds declared | Inline precept: `decimal` field, no `min`/`max` | No interval diagnostics (no obligation generated) |
| `number` field with bounds, ULP widening applies | Inline precept: `number` field + bounds + multiply | Wider interval vs. `decimal`; may prove or not depending on bounds margin |
| Previously-clean definition with no bounded fields | Inline precept: no `min`/`max` on any field | Regression: no new diagnostics vs. pre-Slice-2 baseline |

### 9.3 Edge Case Coverage

| Edge case | Test |
|---|---|
| Division where divisor interval contains zero | Unbounded result → proof fails; `DivisionByZero` already emits separately |
| Deeply nested expression (5+ levels) | Interval propagates correctly through all levels |
| Conditional expression with branches of different intervals | Union of branch intervals used for containment check |
| Field that appears on both sides of assignment (`balance = balance - amount`) | `IntervalOf(balance)` uses declared bounds, not current assignment target |
| Optional field in arithmetic expression | Presence proof is separate; interval uses declared bounds for optional field |
| Field with only `min` declared (no `max`) | One-sided check: only `result.Min >= declared.Min` |
| Field with only `max` declared (no `min`) | One-sided check: only `result.Max <= declared.Max` |
| Empty interval result (logically impossible range) | Trivially proved — empty interval satisfies any containment |
| Negation of bounded interval | `[-max, -min]` — verified to contain within negated bounds |
| Multiplication by zero literal | Result interval `[0, 0]` — contained in any non-empty bounds |
| **Multiple `set` actions on bounded fields in a single transition row** | Each `set` action generates an independent `IntervalContainmentProofRequirement`. Three `set` actions targeting three distinct bounded fields produce three independent obligations. Obligations share the same `TransitionRowContext` but are discharged independently — one may prove while others remain unresolved. Use `MultiSetPrecept` (from §9.1 fixture constants) to verify three obligations are collected and each is discharged correctly. |
| **Currency/unit qualifier + interval co-occurrence (S5 × S7)** | A `decimal` field with a currency qualifier (`as USD`) and declared `min`/`max` bounds may trigger both a `QualifierCompatibility` (S5) obligation and an `IntervalContainment` (S7) obligation on the same `set` expression. **Routing decision:** these are orthogonal obligations and are discharged independently. S5 checks dimensional compatibility; S7 checks numeric containment. Both may be unresolved simultaneously, emitting two diagnostics on the same span — `QualifierMismatch` (S5) and `NumericOverflow` (S7). Neither suppresses the other. An S5 unresolved obligation does NOT block S7 from running — the proof engine evaluates all applicable strategies for each obligation regardless of other obligations' outcomes. **Test:** `CurrencyField_QualifierMismatchAndIntervalViolation_BothDiagnosticsEmitted` — a precept with a `decimal as USD` field with bounds where an expression assigns a non-USD value that also overflows must emit both diagnostics. Companion negative test: `CurrencyField_MatchingQualifierAndFitInterval_NoDiagnostics`. |
| **Computed field with declared `max`, expression interval exceeds it** | A `compute` field (`field x: decimal max 100 <- expr`) where `IntervalOf(expr)` returns an interval that exceeds `max 100` must generate and fail an `IntervalContainment` obligation. The obligation is collected from `FieldExpressionContext` (not `TransitionRowContext`), so the obligation collector's computed-field walk (§3.2) must produce the obligation. **Test:** `ComputedField_DeclaredMaxExceeded_EmitsNumericOverflow` + companion `ComputedField_IntervalWithinMax_Proved`. |

### 9.4 Regression Anchors (named)

The following existing behaviors must not change:

1. **`DivisionByZero` proof** — `decimal ÷ 0` and `decimal ÷ unguarded-field` still emit `DivisionByZero` via existing Numeric proof requirements
2. **`SqrtOfNegative` proof** — sqrt of unbounded field still emits, unaffected
3. **`QualifierCompatibility` proof** — currency/unit checks on `+`/`−` unaffected
4. **`PresenceProof`** — optional field access guards unaffected
5. **Guard-in-Path Strategy 3** — existing `TryGuardInPathProof` for Numeric threshold obligations unaffected
6. **`ProofLedger.Obligations` count** — Numeric threshold obligations still generated and resolved; adding `IntervalContainment` obligations increases count but does not replace existing ones. For the `BoundedLineItemPrecept` test precept, the exact delta (number of new `IntervalContainment` obligations) must be asserted as a named count in the regression test — e.g., `result.Obligations.Count(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment).Should().Be(2)`. "Count increases" alone is not specific enough.
7. **Stateless precept compilation** — `FieldExpressionContext` obligations on stateless precepts unaffected
8. **Initial-state satisfiability** — `CheckInitialStateSatisfiability` in `ProofEngine.Analysis.cs` unaffected
9. **MCP `precept_language` vocabulary output** — `ProofRequirements.All` gains one member (from 6 to 7); existing six members unchanged
10. **Hover card format for qualifier proof** — `⚖️` currency/unit cards unaffected
11. **`ProofRequirementKind` catalog count assertion** — `ProofRequirements.All.Length.Should().Be(7)` must be added to `ProofRequirementsTests.cs` (or equivalent catalog count test) when `IntervalContainment = 7` is added. This is the explicit tripwire that catches missing catalog registrations at build time.

---

## 10. Catalog-Driven Obligation Architecture

> **Added:** 2026-05-13 — incorporates accepted improvements from Frank's catalog-obligation audit and bounds-qualifier audit.

### 10.1 Problem Statement

The initial interval proof engine (Slices 1–4) proved the concept for `decimal`/`number` fields but hardcoded obligation generation against specific `TypeKind` values. This violates Precept's catalog-driven architecture: the obligation collector switches on `TypeKind.Decimal` and `TypeKind.Number` to decide whether to emit `IntervalContainmentProofRequirement`, silently skipping `integer`, `money`, `quantity`, `price`, `exchangerate`, `string` (for `minlength`/`maxlength`), and `collection` (for `mincount`/`maxcount`).

The consequence: authors can declare constraints on these types via existing `min`/`max`/`minlength`/`maxlength`/`mincount`/`maxcount` modifiers, the catalog accepts them, but the proof engine generates no obligations — the constraints are silently ignored at compile time. This is a structural integrity gap.

### 10.2 Rationale

Precept's non-negotiable design principle: **catalog declares behavior; pipeline consumes metadata.** The obligation generator must derive which types require interval/bounds obligations from modifier metadata (`ApplicableTypes`, `ProofSatisfactions`), not from a hardcoded type list in the engine.

**Evidence anchors:**
- `src/Precept/Language/Actions.cs` — hardcoded type filtering in interval-containment obligation generation
- `src/Precept/Language/Modifiers.cs` — `ApplicableTypes` and `ProofSatisfactions` already declare which types support which constraint modifiers
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` — the discharge side already reads `ProofSatisfactions` broadly; the collection side must match

### 10.3 Target Architecture

The obligation collector must:

1. For each `set` action (or computed-field expression), inspect the target field's declared modifiers.
2. For each modifier that carries `ProofSatisfactions` with a `Numeric` entry (comparisons like `>=`, `<=`), generate an `IntervalContainmentProofRequirement` — regardless of the field's `TypeKind`.
3. For non-numeric constraint modifiers (`minlength`, `maxlength`, `mincount`, `maxcount`), generate analogous constraint proof obligations (see §12 for coverage matrix).
4. The `TypeKind` is irrelevant to obligation *generation*. The catalog's modifier metadata determines whether a constraint exists; the obligation generator reads that metadata.

**What this eliminates:** Every `TypeKind` switch/check in the obligation collection path. The only remaining type-specific logic is in the *discharge* strategies (transfer functions differ per type), which is correct — transfer functions are declared per-operation in `Operations.cs`.

### 10.4 Updated Enforcement Model

| Stage | Current (hardcoded) | Target (catalog-driven) |
|---|---|---|
| Obligation collection | `if (type == decimal \|\| type == number)` | `if (field.Modifiers.Any(m => m.ProofSatisfactions.OfType<Numeric>().Any()))` |
| Bound extraction | `TryGetComparableModifierValue` (numeric literals only) | Extended to handle typed constants and qualified bounds (§11) |
| Discharge strategy | Strategy 7 reads `BinaryOperationMeta.IntervalTransfer` | Unchanged — already catalog-driven |
| Diagnostic emission | `NumericOverflow` on unresolved | Unchanged; additional diagnostics for qualifier issues (§11.3) |

### 10.5 New Diagnostics

| Diagnostic name | Stage | Trigger | Severity |
|---|---|---|---|
| `BoundsRequireQualifier` | Type checker | `min`/`max` declared on `money`/`quantity`/`price` without required `in`/`of` qualifier | Error |
| `BoundsQualifierMismatch` | Type checker | Bound qualifier (e.g., `'5 kg'`) conflicts with field qualifier (e.g., `in 'lb'`) | Error |
| `UnextractableBound` | Type checker | Bound expression cannot be converted to a comparable value (transitional — to be removed as extraction improves) | Warning |

---

## 11. Qualified-Type Bound Semantics

### 11.1 Problem Statement

For qualified types (`money`, `quantity`, `price`), declaring `min`/`max` without a matching qualifier context is semantically undefined. `field test as quantity max '5 kg'` compiles without diagnostics today, but:

1. `quantity` without `in`/`of` has no comparison basis — what unit is `max` measured in?
2. Typed constants like `'5 kg'` are not extracted into comparable bound values by `TryGetComparableModifierValue`, so `DeclaredMin`/`DeclaredMax` remain `null` and no interval obligation is emitted.
3. The bound is accepted but silently ignored — violating the principle that declared constraints must never be silently ignored.

### 11.2 Required Qualifier Context

| Type | Required qualifier for bounds | Example (legal) | Example (illegal) |
|---|---|---|---|
| `money` | `in` (currency) | `field cost as money in 'USD' min '0 USD' max '100000 USD'` | `field cost as money max 100000` |
| `quantity` | `in` or `of` (unit) | `field weight as quantity in 'kg' max '5 kg'` | `field weight as quantity max '5 kg'` |
| `price` | `in` (currency) | `field rate as price in 'USD' min '0 USD'` | `field rate as price min 0` |
| `decimal` | None required | `field balance as decimal min 0 max 999999` | — |
| `number` | None required | `field ratio as number min 0 max 1` | — |

### 11.3 Typed-Constant Bound Extraction

**Current gap:** `TryGetComparableModifierValue` (at `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:211-226`) handles only `NumberLiteral` and unary-minus number literals. Typed constants return `null`.

**Target:** Extend `TryGetComparableModifierValue` to:

1. Recognize typed-constant expressions (single-quoted literals like `'5 kg'`, `'100 USD'`).
2. Parse the numeric component from the typed constant using the existing typed-constant resolution pipeline.
3. Extract the qualifier component (unit/currency) from the typed constant.
4. Populate `DeclaredMin`/`DeclaredMax` with the numeric value.
5. Store the extracted qualifier for compatibility checking (§11.4).

**Files affected:**
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` — extend `TryGetComparableModifierValue`
- `src/Precept/Pipeline/TypeChecker.cs` — `DeclaredMin`/`DeclaredMax` population path

### 11.4 Qualifier Compatibility Checks

When a field has a qualifier (`in 'USD'`) and a bound has a qualifier (`max '100 EUR'`), the qualifiers must be compatible:

- **Match:** Field `in 'USD'`, bound `'100 USD'` → extract numeric value `100`, qualifiers match.
- **Mismatch:** Field `in 'USD'`, bound `'100 EUR'` → emit `BoundsQualifierMismatch` diagnostic.
- **Missing field qualifier:** Field has no `in`/`of`, bound has qualifier → emit `BoundsRequireQualifier`.
- **Missing bound qualifier:** Field has `in 'kg'`, bound is bare numeric `5` → this is ambiguous for qualified types. Emit `BoundsRequireQualifier` (the bound must specify its unit).

**Implementation location:** Type-checker validation pass, immediately after modifier extraction, before proof obligation collection.

---

## 12. Type-Family Coverage Matrix

This matrix documents the constraint-obligation coverage target across all type families. The principle: **any declared constraint must produce a compile-time proof obligation or a diagnostic explaining why it cannot.**

| Type family | Constraint modifiers | Obligation type | Current status | Target |
|---|---|---|---|---|
| `decimal` | `min`, `max` | `IntervalContainment` | ✅ Covered (Slices 1–2) | Maintained |
| `number` | `min`, `max` | `IntervalContainment` (with ULP widening) | ✅ Covered (Slice 4) | Maintained |
| `integer` | `min`, `max` | None (BigInteger cannot overflow) | ✅ Correct skip | Maintained |
| `money` | `min`, `max` | `IntervalContainment` | ❌ Silently skipped | Covered via catalog-driven generator (Slice 7) + qualifier semantics (Slices 8–10) |
| `quantity` | `min`, `max` | `IntervalContainment` | ❌ Silently skipped | Covered via catalog-driven generator (Slice 7) + qualifier semantics (Slices 8–10) |
| `price` | `min`, `max` | `IntervalContainment` | ❌ Silently skipped | Covered via catalog-driven generator (Slice 7) + qualifier semantics (Slices 8–10) |
| `exchangerate` | `min`, `max` | `IntervalContainment` | ❌ Silently skipped | Covered via catalog-driven generator (Slice 7) |
| `string` | `minlength`, `maxlength` | `LengthContainment` (new) | ❌ No obligation generated | Covered (Slice 11) |
| `collection` | `mincount`, `maxcount` | `CountContainment` (new) | ❌ No obligation generated | Covered (Slice 11) |
| `optional` (any type) | `optional` modifier | `Presence` | ❌ No obligation generated (`PresenceProofRequirement` never constructed) | Covered (Slice 12) |
| `temporal` (`date`, `time`, `datetime`, `instant`) | No `min`/`max` declared in catalog | None | ✅ No gap (no constraint declared) | No action needed |

### 12.1 Principle: No Silent Constraint Ignoring

If the catalog allows a constraint modifier on a type (i.e., the modifier's `ApplicableTypes` includes that type), then either:

1. A proof obligation is generated for assignments to fields with that constraint, **or**
2. A diagnostic is emitted explaining why the constraint cannot be enforced (e.g., `BoundsRequireQualifier`).

There must be no third option where the constraint is declared, accepted by the parser and type checker, and then silently ignored by the proof engine. This applies equally to presence constraints: an `optional` field referenced in a value position must generate a `PresenceProofRequirement`, or the proof engine is silently ignoring a declared constraint.

---

## Appendix A — Why Not `@bounds` Annotation Syntax

The prior design analysis (`overflow-prevention-design-analysis.md`) proposed a new `@bounds(min, max)` annotation. This design rejects that proposal for three reasons:

1. **Language surface already has `min`/`max`** — Precept fields already support `min X` and `max X` modifier syntax. Introducing `@bounds(min, max)` would create two parallel ways to declare the same thing. One-file completeness and keyword-anchored syntax are design principles that prohibit this.
2. **`@bounds` doesn't exist in the Tokens catalog** — adding it would require a new token kind, a new modifier kind, new parser rules, new completions, and grammar changes. The interval engine gets all the information it needs from existing modifiers.
3. **Gradual adoption without new syntax** — authors who already use `min`/`max` get interval proofs for free. Authors who don't, don't. No migration path needed.

## Appendix B — Why the Proof Is Sound (Not Approximate)

Interval arithmetic with correct transfer rules is **complete for linear arithmetic**: if the result interval is contained within the bounds, no overflow can occur. This is not an approximation — it is a mathematical proof. The only conservatism is:

- Unbounded fields produce unbounded intervals → containment fails → obligation unresolved. This is correct conservatism: if bounds are unknown, overflow cannot be proved impossible.
- IEEE 754 ULP widening for `number` type → intervals are slightly wider than mathematically exact → some valid proofs may fail. This is correct: better to produce a false negative than a false proof. Authors can tighten bounds slightly to compensate.

The strategy is **sound**: it never proves a false positive. If Strategy 7 discharges an obligation as `Proved`, the mathematical interval containment holds, and overflow is genuinely impossible given the declared bounds.

---

*Document ready for design review. Shane sign-off required before any implementation begins. Elaine review required on §6 (hover) before Slice 5. George review required on §8 (implementation plan) before any slice begins. Soup Nazi review required on §9 (test strategy) before Slice 2 completion gate. Sections 10–12 and Slices 7–12 added 2026-05-13 to incorporate catalog-driven obligation generation, qualified-type bound semantics, and type-family coverage improvements.*


