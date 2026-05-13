# Numeric Overflow Prevention Design Analysis

**Author:** Frank (Analysis Agent)  
**Status:** Design recommendation — interval arithmetic chosen as implementation path  
**Scope:** Overflow prevention for `decimal`, `number`, and bounded `integer` types in Precept  
**Date:** 2026-05-13

---

## Executive Summary

After comprehensive architectural analysis of Precept's numeric type system, this document presents eight viable overflow prevention strategies and recommends **interval arithmetic** as the path forward for compile-time bound guarantee and prevention of numeric overflow. This recommendation is conditional on pervasive annotation (`@bounds`), but mathematically sound and sufficient for Precept's financial domain focus.

---

## Problem Statement

Precept's numeric type system currently faces an overflow vulnerability:

- **`integer`** = `System.Numerics.BigInteger` (unbounded, prevents arithmetic overflow)
- **`decimal`** = fixed-point ~10^28 (bounded, overflow silently possible, runtime-faulted)
- **`number`** = IEEE 754 (bounded, overflow silently possible, runtime-faulted)

### Critical Gaps

1. **Undocumented BigInteger design:** No rationale documented for why `integer` is unbounded when `decimal` and `number` are bounded.
2. **Missing overflow proof:** The proof engine has six strategies (Literal, Declaration Attribute, Guard-in-Path, Flow-Narrowing, Qualifier Compatibility, Compositional Constraint), **none of which handle arithmetic result-range checking**. Arithmetic operations (+, −, ×) carry **zero ProofRequirements**.
3. **Diagnostic gap:** `NumericOverflow` diagnostic exists, marked `[StaticallyPreventable]`, but has no corresponding proof strategy.
4. **Type representable bounds not cataloged:** The bounds of each numeric type (e.g., decimal's ~10^28 limit) are not in the Operations or Type catalogs, making them unavailable to the proof engine.
5. **Semantic confusion:** Field-bound violations (`OutOfRange`) and type-representable violations (`NumericOverflow`) are conflated; the system treats them as a single concern but they require different prevention strategies.

### Historical Context

Precept's v1 runtime (April 2026) **did implement interval arithmetic** with full transfer rules for +, −, ×, ÷, √, Pow and IEEE 754 saturation semantics. The implementation included:
- `NumericInterval` struct
- `LinearForm` canonical fact key matching
- `ProofContext` with transitive closure BFS (MaxFacts=64, MaxDepth=4, MaxVisited=256)
- 255 tests proving correctness

This implementation was **deleted in commit b0c09eb5** ("clean room isolation" for radical branch architecture pivot). The radical branch chose a different tradeoff: use BigInteger for unbounded `integer` to prevent arithmetic overflow entirely, and accept bounded-type overflow as a runtime behavior for `decimal` and `number`.

---

## Analysis: Eight Overflow Prevention Strategies

| # | Strategy | Mechanism | Pros | Cons | Complexity | Domain Fit |
|---|----------|-----------|------|------|-----------|-----------|
| 1 | **BigInteger for all** | Unbounded representation | Prevents all arithmetic overflow | Memory overhead; performance drag; doesn't solve bound violations | Low | Poor — defeats purpose of bounded types |
| 2 | **Interval arithmetic** ← **RECOMMENDED** | Compile-time bound composition | Guarantee at compile time; fast custom solver; conditional on annotation | Annotation burden; doesn't prevent, shifts to constraints | Medium | **Excellent** — financial domains annotate bounds anyway |
| 3 | **SMT solver (Z3, CVC4)** | Automatic constraint solving | Fully automatic; handles complex arithmetic | Slow; timeout risk; false negatives; overkill for linear arithmetic | High | Poor — financial operations are mostly linear |
| 4 | **Checked arithmetic + narrowing** | Explicit overflow checks + type narrowing | Compile-time checks; static safety | Restrictive (many operations must be rejected); verbose | Medium | Fair — too many valid operations rejected |
| 5 | **Qualified types** | `NonOverflow<T>` nominal types | Lightweight; composable; gradual adoption | Less expressive; doesn't capture bounds; per-type overhead | Low | Fair — orthogonal to other strategies |
| 6 | **Homogeneous numeric hierarchy** | Single unifying type (e.g., `number` covers all) | Unified semantics; runtime-safe | Loss of semantic distinction; no static guarantees | High | Poor — contradicts Precept philosophy |
| 7 | **Saturation semantics** | Overflow clips to type bounds | Silent safety | Hides errors; user confusion; wrong semantics for financial | Low | **Very poor** — financial domain needs exact values |
| 8 | **Promote-on-overflow** | Auto-promote to larger type or string | Preserves exact value | Non-deterministic; adds complexity; unclear failure modes | Medium-High | Poor — implicit conversions violate Precept guarantee |

**Recommendation: Strategy 2 (Interval Arithmetic)** combines tractability (custom solver sufficient), soundness (proven in v1 runtime), and domain fit (financial operations are mostly linear arithmetic with declared bounds).

---

## Interval Arithmetic: Mathematical Foundation

### How It Works

**Interval arithmetic** represents numeric values as ranges [min, max] and composes intervals through arithmetic operations:

```
[a_min, a_max] + [b_min, b_max] = [a_min + b_min, a_max + b_max]
[a_min, a_max] × [b_min, b_max] = [min(a_min×b_min, a_min×b_max, a_max×b_min, a_max×b_max),
                                     max(a_min×b_min, a_min×b_max, a_max×b_min, a_max×b_max)]
```

**Proof obligation:** For field assignment `field x: decimal max 10`, if an expression computes a result, the proof engine must verify that the result interval fits within the declared bound:
```
interval(expr) ⊆ [0, 10]  ?  ✓ PROVED  :  emit ERROR (or fallback to runtime check)
```

### Proof Mechanism

1. **Constraint declaration:** User annotates fields with bounds:
   ```precept
   field balance: decimal @bounds(min: 0m, max: 999999999.99m)
   field amount: decimal @bounds(min: 0m, max: 50000m)
   ```

2. **Interval extraction:** Type checker extracts declared bounds and creates intervals:
   ```
   balance ∈ [0, 999999999.99]
   amount ∈ [0, 50000]
   ```

3. **Interval composition:** For expression `balance - amount`, compute result interval:
   ```
   [0, 999999999.99] - [0, 50000] = [0 - 50000, 999999999.99 - 0]
                                   = [-50000, 999999999.99]
   ```

4. **Proof check:** Does result fit the assignment target's bound?
   ```
   balance = balance - amount
   target interval: [0, 999999999.99]
   result interval: [-50000, 999999999.99]
   
   [-50000, 999999999.99] ⊆ [0, 999999999.99] ?  ✗ FALSE
   → Emit ERROR: "Subtraction may produce negative balance; add guard or constraint"
   ```

5. **Unconstrained operands:** If an operand has no bound:
   ```
   balance ∈ [0, 999999999.99]
   input: decimal (unconstrained → [decimal.MinValue, decimal.MaxValue])
   
   result = balance + input ∈ [decimal.MinValue, decimal.MaxValue]
   → Proof fails (result interval too wide)
   → Fallback: INSERT RUNTIME CHECK or emit HARD ERROR
   ```

### Why This Works for Precept

- **Conditional but sound:** Overflow is mathematically guaranteed NOT to occur if all operands have declared bounds.
- **Custom solver sufficient:** Financial arithmetic is mostly linear (±, ×) and straightforward rounding. No SMT solver needed.
- **Annotation burden is acceptable:** Precept fields *already require* `max` constraints in most cases. Adding an explicit `@bounds` annotation is one extra line per field.
- **Two-layer defense:** Compile-time proof + runtime checks for edge cases that can't be proved.
- **Gradual adoption:** Fields without `@bounds` fall back to runtime checks; system remains safe.

---

## Interval Arithmetic Key Insights

### Doesn't Prevent Overflow; Shifts Burden to Constraints

**Critical distinction:** Interval arithmetic doesn't magically prevent overflow. It **shifts the burden from the runtime to the constraint declaration phase**.

- **Without bounds:** `x = a + b` where `a` and `b` are unconstrained decimals → can overflow the type.
- **With bounds:** `a ≤ 50000` and `b ≤ 50000` → result is guaranteed ≤ 100000 → fits within decimal's range (if the operation is chosen properly).

If operands are unconstrained, unconstrained results expose the full representable range. Interval arithmetic proves safety **only when constraints are pervasive**.

### Runtime Fallback

Unprovable operations don't fail at compile time (with careful configuration):

1. **Option A (Hard error):** Proof fails → emit ERROR diagnostic → block compilation. User must add guards or constraints.
2. **Option B (Runtime check):** Proof fails → emit WARN diagnostic → insert runtime checked arithmetic → continue compilation. At runtime, if overflow occurs, the evaluator raises `NumericOverflow` fault.

**Recommendation: Use Option A initially** (hard error) to enforce sound constraints. Relax to Option B only after the system has proven sound and users demand more flexibility.

### Interval Arithmetic Can Solve Overflow

**Frank's verdict:** YES — mathematically sound, but conditional on pervasive annotation.

Interval arithmetic was proven in v1 runtime with 255 tests. The reason it was deleted wasn't because it doesn't work; it was a deliberate architectural choice (radical branch pivoted to BigInteger for `integer` to avoid the annotation burden on users).

Given Precept's commitment to "make invalid states structurally impossible," interval arithmetic is the right tradeoff: more annotation upfront, but absolute guarantee at compile time.

---

## Design Space: Alternative Strategies

### Why Not SMT Solver?

SMT solvers (Z3, CVC4) can automatically discharge arbitrary arithmetic constraints. **They are overkill for Precept:**

- **Financial operations are mostly linear:** Precept targets financial domain where operations are addition, subtraction, multiplication by constants, percentage calculations. Linear arithmetic solvers are fast and sufficient.
- **Timeout risk:** SMT solvers can time out on complex constraints. Precept's determinism guarantee makes timeouts unacceptable.
- **Complex to integrate:** Adds external dependency, build complexity, and distribution burden.
- **Slower:** Custom interval solver is O(n) in constraint count; SMT solver is exponential in worst case.

**Fallback strategy:** If future use cases (e.g., complex derivatives pricing, non-linear constraints) demand SMT capability, Z3 can be added as an optional upgrade path. Start with custom solver.

### Why Not Qualified Types?

Qualified types (e.g., `NonOverflow<decimal>`) add a nominal type layer for "proven-safe" values:

```csharp
type NonOverflow<T> = T when ProvenNotToOverflow
```

**Pros:**
- Lightweight, composable, orthogonal to other strategies
- Can be adopted gradually alongside interval arithmetic

**Cons:**
- Doesn't capture bounds themselves (only "no overflow" flag)
- Requires API discipline to avoid mixing qualified and unqualified types
- Can be combined with interval arithmetic but doesn't replace it

**Role:** Qualified types are a **complementary strategy**, not a replacement. They can be added to Precept later as an optimization (e.g., `x: NonOverflow<decimal>` bypasses some interval checks).

### Why Not Saturation Semantics?

Saturation (overflow → type bounds, e.g., overflow to `decimal.MaxValue`) silently clips values:

**Fatal flaw for financial domain:** In financial systems, losing precision is worse than knowing the operation failed. An unnoticed rounding to `decimal.MaxValue` can corrupt account state. Precept's philosophy is "make invalid states structurally impossible" — saturation does the opposite (silently makes them possible).

---

## Implementation Architecture: 3-Tier Approach

### Tier 1 — Minimal Working Implementation (Weeks 1–2)

**Scope:** Bounded `integer` type with linear arithmetic intervals.

**Components:**

1. **`Interval` record:**
   ```csharp
   public record Interval(decimal Min, decimal Max)
   {
       public bool IsEmpty => Min > Max;
       public bool IsUnbounded => Min == decimal.MinValue && Max == decimal.MaxValue;
       
       // Arithmetic operations
       public Interval Add(Interval other) => new(Min + other.Min, Max + other.Max);
       public Interval Subtract(Interval other) => new(Min - other.Max, Max - other.Min);
       public Interval Multiply(Interval other) => ... // 4-case logic
       
       // Proof check
       public bool Contains(Interval other) => other.Min >= Min && other.Max <= Max;
   }
   ```

2. **Bounds annotation parsing:**
   - Add `@bounds(min, max)` annotation to field declarations
   - Parse and store in `PreceptField.Bounds` (nullable `Interval`)
   - Type checker validates `min` and `max` are constant expressions

3. **Proof obligation generation:**
   - During assignment type checking, extract field's declared interval
   - Compute result interval for expression
   - Check containment
   - If fails: emit `NumericOverflowOnAssignment` ERROR diagnostic

4. **Runtime fallback:**
   - Insert `checked` arithmetic around unprovable assignments
   - If overflow occurs at runtime, evaluator raises `NumericOverflow` fault

5. **Unit tests:** 20–30 tests covering:
   - Interval containment (positive and negative cases)
   - Arithmetic operations (linear operations only)
   - Proof success and failure paths
   - Undeclared bounds handling (fallback to runtime checks)

### Tier 2 — Extend to `decimal` and Division (Weeks 3–4)

**Scope:** Bounded `decimal` type, division, and rounding semantics.

**Additional Components:**

1. **Division interval logic:**
   ```csharp
   public Interval Divide(Interval other)
   {
       // Handle division by zero
       if (other.Min <= 0 && other.Max >= 0) return Interval.Unbounded;
       
       // Compute all four corner products
       var cases = new[] {
           Min / other.Min, Min / other.Max, Max / other.Min, Max / other.Max
       };
       return new(cases.Min(), cases.Max());
   }
   ```

2. **Rounding semantics:**
   - Precept's fixed-point decimal uses truncation or banker's rounding (implementation-dependent)
   - Interval logic must account for rounding direction

3. **Constraint generation from derived fields:**
   - If field `B` depends on field `A` (e.g., `B := A * 0.01` for percentage), extract bounds
   - Example: `A ∈ [0, 10000]`, operation `× 0.01` → `B ∈ [0, 100]`

4. **Financial computation tests:**
   - Percentage calculations (tax, fee)
   - Currency conversions (with fixed exchange rates)
   - Compound interest (with bounded iterations)

### Tier 3 — Computed Bounds and Inference (Weeks 5–6)

**Scope:** Bounds referencing other fields, bounds inference for derived fields.

**Additional Components:**

1. **Cross-field bounds:**
   ```precept
   field total: decimal @bounds(min: 0m, max: {sum of all line items})
   ```
   - Allow `max` to reference other field values
   - Require all referenced fields to be constant or previously computed
   - Solve system of constraint equations

2. **Bounds inference:**
   - For computed fields, infer bounds from deriving expression
   - Example: `discountAmount := total × discountRate` with `discountRate ∈ [0, 0.5]` → infer `discountAmount` bounds

3. **Integration with guards and invariants:**
   - Guards that narrow intervals (e.g., `require x > 100` narrows `x`'s lower bound)
   - Invariants that constrain multiple fields (e.g., `balance ≥ debt + interest`)

---

## Integration Points

### 1. Type Checker

**Phase:** New phase after type checking, before code generation.

```
TypeChecker output (well-typed AST)
    ↓
BoundsValidator (interval arithmetic)
    ↓
Emit diagnostics (overflow proofs)
    ↓
GraphAnalyzer (unchanged)
```

### 2. Operations Catalog

**Update required:** Add `Interval`-based bounds metadata to Operations catalog entries for +, −, ×, ÷, √, Pow:

```csharp
public record OperationMeta
{
    public IntervalTransfer? IntervalSemantics { get; init; }
    // ... existing fields
}

public delegate Interval IntervalTransfer(Interval[] operands);
```

### 3. Diagnostics

**New diagnostic code:** `NumericOverflowOnAssignment` (ERROR, StaticallyPreventable)

```csharp
DivisionByZero (ERROR) — already exists
SqrtOfNegative (ERROR) — already exists
NumericOverflow (ERROR, StaticallyPreventable) — new, for type-level overflow
NumericOverflowOnAssignment (ERROR, StaticallyPreventable) — new, for field-bound overflow via arithmetic
```

### 4. Language Server

**Updates:**

- Hover on field with `@bounds`: show inferred interval
- Hover on expression: show computed result interval
- Diagnostic in-line on risky assignments: "Expression interval [−50000, 999999999.99] exceeds field bound [0, 999999999.99]"
- Completions: suggest `@bounds(min: 0m, max: ...)` when defining bounded fields

### 5. Runtime

**Fallback checked arithmetic:** For unprovable operations, insert runtime checks.

```csharp
// Proof fails → insert runtime check
decimal result = checked(a + b);  // throws OverflowException if overflow occurs
```

---

## Phasing Strategy: 3-Wave Timeline

### Wave 1: Bounded `integer` (Weeks 1–2)

**Goal:** Minimal working system for `integer` type with simple linear arithmetic.

- Implement `Interval` record and arithmetic
- Parse `@bounds` annotation
- Add proof obligation generation (Tier 1)
- Add 20–30 unit tests
- **Success criterion:** Proofs discharge correctly for `integer` fields with declared bounds; unprovable operations emit ERROR

### Wave 2: Bounded `decimal` (Weeks 3–4)

**Goal:** Extend to `decimal` type, handle division, support financial operations.

- Implement division and rounding logic
- Add constraint generation from derived fields
- Add financial computation tests
- **Success criterion:** Percentage calculations, currency operations prove correctly or emit appropriate errors

### Wave 3: Computed Bounds (Weeks 5–6)

**Goal:** Advanced features: cross-field bounds, inference, guards, invariants.

- Implement cross-field bounds references
- Implement bounds inference
- Integrate with guards and invariants
- Update samples and documentation
- **Success criterion:** Complex financial workflows (e.g., discount calculation with inferred bounds) work end-to-end

**Total effort:** 6 weeks for minimal working implementation.

---

## Concrete Example: LineItem with Bounds

```precept
precept LineItem
  state Active

  field itemPrice: decimal @bounds(min: 0m, max: 100000m)
  field quantity: decimal @bounds(min: 1m, max: 1000m)
  field lineTotal: decimal @bounds(min: 0m, max: 100000000m)

  on Init
    require itemPrice >= 0m
    require quantity >= 1m
    set lineTotal = 0m
  
  on SetItemPrice(price: decimal)
    require price >= 0m
    require price <= 100000m
    set itemPrice = price
    // Result interval: [0, 100000] × [1, 1000] = [0, 100000000] ✓ fits in lineTotal
    set lineTotal = itemPrice * quantity
  
  on ApplyDiscount(discountPercent: decimal)
    require discountPercent >= 0m
    require discountPercent <= 1m
    // Result interval: [0, 100000000] × [0, 1] = [0, 100000000] ✓ fits in lineTotal
    set lineTotal = lineTotal * (1m - discountPercent)
```

**Proof outcomes:**
- ✅ `SetItemPrice`: Result interval [0, 100000000] ⊆ lineTotal bounds [0, 100000000] — PROVED
- ✅ `ApplyDiscount`: Result interval [0, 100000000] ⊆ lineTotal bounds [0, 100000000] — PROVED
- ⚠️ If user removes `require discountPercent <= 1m`: Result interval becomes [decimal.MinValue, decimal.MaxValue] — FAILS proof → emit ERROR

---

## Critical Design Decisions

1. **Annotation burden is acceptable:** Precept fields typically need bounds anyway (`max 10`, etc.). `@bounds` is additive, not disruptive.

2. **Hard error on unprovable operations (initially):** Proof failure → ERROR diagnostic → blocks compilation. This enforces sound constraints. After the system is proven sound, relax to runtime checks.

3. **No SMT solver in MVP:** Custom interval solver is sufficient for financial arithmetic. Z3 can be added as an optional feature later.

4. **Interval arithmetic handles arithmetic overflow only:** Field-bound violations (`OutOfRange`) are handled separately by existing proof engine. Don't conflate.

5. **Unconstrained operands are unsafe:** If an operand has no `@bounds`, its interval is unbounded (decimal.MinValue to decimal.MaxValue). Operations on unbounded values produce unbounded results → proof fails. This is correct behavior; it forces users to declare bounds.

---

## Fallback Strategies (If Constraints Arise)

### If Annotation Burden Proves Unacceptable

**Option 1: Qualified types**
- Add `NonOverflow<T>` nominal type for "proven-safe" values
- Allows mixing safe and unsafe values gracefully
- Can coexist with interval arithmetic

**Option 2: Inferred bounds from invariants**
- If field `x` appears only in transition guards like `require x < 100`, infer `x ∈ [0, 100)`
- Reduces need for explicit `@bounds`
- Complexity: guard analysis is expensive; only for high-value cases

### If Performance Issues Arise

**Option 1: Proof caching**
- Cache interval computations for expression trees
- Amortize cost across multiple assignments to the same expression

**Option 2: Lazy proof deferral**
- Defer proof obligations to graph analyzer phase (after state machine structure is known)
- May enable better constraint simplification

### If Linear Arithmetic Insufficient

**Option 1: SMT solver integration (Z3)**
- For non-linear constraints (e.g., derivatives pricing, polynomial models)
- Requires Z3 as external dependency
- Significant integration work

**Option 2: Qualified types + manual proof**
- Users mark complex operations as `@proven` after manual review
- Bypasses automatic checking for trusted code paths

---

## Documentation Updates Required

1. **`docs/language/primitive-types.md`**
   - Document BigInteger design choice and rationale
   - Explain `@bounds` annotation syntax
   - Provide overflow prevention examples

2. **`docs/language/numeric-overflow-design.md`** (new)
   - This document (design and architecture)

3. **`docs/runtime/interval-arithmetic.md`** (new)
   - Implementation details for maintainers
   - Interval arithmetic algorithm description
   - Rounding semantics reference

4. **Samples**
   - Add `samples/interval-arithmetic-financial.precept` with LineItem and similar examples
   - Show bounded and unbounded scenarios

5. **Language server completions**
   - Add `@bounds` to field annotation completions
   - Update hover information for fields with bounds

---

## Success Metrics

1. **Compilation gates overflow:** No `decimal` field can exceed its declared bounds at compile time (or with explicit fallback to runtime check).

2. **Constraint coverage:** 100% of financial operations in sample domain have explicit `@bounds` declarations.

3. **No silent overflow:** If overflow occurs (field assigned value exceeding bounds), the system either proves it impossible or fails loudly (ERROR diagnostic or runtime `NumericOverflow` fault).

4. **Performance acceptable:** Interval arithmetic adds <5% to type-checking time (for typical precepts with ~20 fields).

5. **User experience:** New `@bounds` annotation feels natural and is documented clearly; no confusion with existing `max` constraints.

---

## Conclusion

**Interval arithmetic is the right choice for Precept's numeric overflow problem.** It combines:

- **Soundness:** Mathematically proven (v1 runtime reference implementation, 255 tests)
- **Tractability:** Custom solver sufficient for financial domain
- **Gradualism:** Unconstrained fields fall back to runtime checks; adoption can be phased
- **Domain fit:** Financial operations are mostly linear; annotation burden aligns with existing practices

The 6-week phasing strategy (Wave 1: integer, Wave 2: decimal, Wave 3: computed bounds) reduces risk and allows early feedback from financial domain experts.

**Next steps:**
1. Design review and owner sign-off
2. Create GitHub issues per wave
3. Implement Wave 1 (bounded `integer`)
4. Collect feedback and adjust as needed
