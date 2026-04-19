# Non-SMT Proof Stack — Implementation Plan for PR #108

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-04-17
**Extends:** PR #108 (feature/issue-106-divisor-safety) — Slices 1–10 already complete

---

## Updated Summary (append to existing PR Summary)

Slices 11–15 extend divisor/sqrt safety with a full non-SMT proof engine. Sequential assignment flow (Slice 11) fixes a soundness gap where intra-row mutations (`set Rate = 0 -> set X = A / Rate`) trusted stale proof state. Interval arithmetic (Slices 12–13) tracks numeric bounds through expression trees, subsuming sign analysis and enabling compound-expression divisor proofs (`A / (D * C)`, `A / abs(D)`, `sqrt(Rate * Rate)`). Relational inference (Slice 14) harvests `A > B` facts from guards/rules/ensures and proves `A - B` nonzero in divisor position. Conditional expression proof synthesis (Slice 15) computes hull intervals for `if/then/else` results. Together, these close the "compound expression → no diagnostic" gap from Principle #8 conservatism and make the `D - D` test correctly emit C93.

## Updated Why (append to existing PR Why)

The original 10 slices cover identifier-only divisor proofs. Real business formulas use compound divisors: `Amount / (Rate * Factor)`, `Score / abs(Adjustment)`, `Surplus / (Produced - Defective)`. Without expression-level proofs, these silently pass the type checker (Principle #8 conservatism) even when the divisor is provably zero. The non-SMT proof stack leverages Precept's flat execution model — no loops, no control-flow branches, no reconverging flow — to make interval arithmetic and relational inference trivially implementable as single-pass recursive walks over finite expression trees. No fixpoint, no widening, no solver.

---

## Slice 11: Sequential Assignment Flow (Soundness Fix)

**Depends on:** Slices 1–10 (complete)
**Unblocks:** Slices 12–15 (all later slices rely on correct post-mutation proof state)

### Problem

`ValidateTransitionRows()` (line ~305) computes one `setSymbols` snapshot per row from guard narrowing. All `set` assignments in that row are validated against this same snapshot. A `set Rate = 0` does NOT kill the `$positive:Rate` marker for a subsequent `set X = Amount / Rate` in the same row. Similarly, `ValidateStateActions()` (line ~410) uses `baseSymbols` immutably across all assignments.

### What to create

**New method: `ApplyAssignmentNarrowing`** in `PreceptTypeChecker.cs` (~45 lines)

```
static IReadOnlyDictionary<string, StaticValueKind> ApplyAssignmentNarrowing(
    string targetField,
    PreceptExpression rhs,
    IReadOnlyDictionary<string, StaticValueKind> symbols)
```

Pattern-match the RHS expression:
- `PreceptLiteralExpression { Value: long l }` → inject/kill `$positive:`, `$nonneg:`, `$nonzero:` based on `l`'s sign
- `PreceptLiteralExpression { Value: double d }` → same for double
- `PreceptLiteralExpression { Value: decimal m }` → same for decimal
- `PreceptLiteralExpression { Value: null }` → kill all numeric markers, reintroduce `Null` flag on the target field kind
- `PreceptIdentifierExpression` when `TryGetIdentifierKey` succeeds → copy markers from source identifier to target
- Default (compound expression): kill `$positive:target`, `$nonneg:target`, `$nonzero:target` (conservative)

### What to modify

**`ValidateTransitionRows()`** (lines ~195–230): After the `ValidateExpression` call for each `set` assignment AND after the C68 choice-literal check, call `ApplyAssignmentNarrowing` to update `setSymbols`:

```csharp
// After existing ValidateExpression + C68 check for each assignment:
setSymbols = ApplyAssignmentNarrowing(assignment.Key, assignment.Expression, setSymbols);
```

Thread the updated `setSymbols` through the assignment `foreach` loop so each subsequent assignment sees post-mutation proof state. (~5 lines changed)

**`ValidateStateActions()`** (lines ~354–380): Same pattern — after each `ValidateExpression` + C68 check, call `ApplyAssignmentNarrowing` on `baseSymbols`:

```csharp
baseSymbols = new Dictionary<string, StaticValueKind>(
    ApplyAssignmentNarrowing(assignment.Key, assignment.Expression, baseSymbols),
    StringComparer.Ordinal);
```

(~5 lines changed)

### Tests (in `PreceptTypeCheckerTests.cs`)

| Test method | Attribute | What it verifies |
|---|---|---|
| `Check_DivisorIntraRowMutation_SetZero_EmitsC93` | `[Fact]` | `set Rate = 0 -> set X = A / Rate` → C93 (was false negative) |
| `Check_DivisorIntraRowMutation_SetLiteral5_NoC93` | `[Fact]` | `set Rate = 5 -> set X = A / Rate` → clean (literal positive) |
| `Check_DivisorIntraRowMutation_SetNull_Kills` | `[Fact]` | `set Rate = null -> set X = A / Rate` → C42 or C93 |
| `Check_DivisorIntraRowMutation_SetIdentifier_Copies` | `[Fact]` | `set Rate = OtherPositive -> set X = A / Rate` → clean (marker copied) |
| `Check_DivisorIntraRowMutation_SetCompound_KillsMarkers` | `[Fact]` | `set Rate = D - D -> set X = A / Rate` → C93 (compound = markers killed) |
| `Check_DivisorStateAction_SetZero_EmitsC93` | `[Fact]` | State action: `set Rate = 0 -> set X = A / Rate` → C93 |
| `Check_DivisorStateAction_SetLiteral_NoC93` | `[Fact]` | State action: `set Rate = 3 -> set X = A / Rate` → clean |

**Regression anchor (MUST FLIP):**
- `Check_DivisorIntraRowMutation_KnownLimitation_NoC93` (line 1624) — this test currently asserts `BeEmpty()` for C93. After Slice 11, this must be **updated** to assert `ContainSingle(d => d.Constraint.Id == "C93")` because sequential flow now kills the stale `$positive:Rate` marker when `set Rate = 0` executes.

**Other regression anchors (MUST PASS UNCHANGED):**
- All existing C92/C93 tests (lines 1167–1700)
- All 25 sample `.precept` files must compile clean

### Estimated size

- ~45 new lines (`ApplyAssignmentNarrowing`)
- ~10 changed lines (wiring into assignment loops)
- 7 new tests + 1 updated test

---

## Slice 12: NumericInterval Struct + Interval Extraction from Markers

**Depends on:** Slice 11
**Unblocks:** Slice 13

### What to create

**New file: `src/Precept/Dsl/NumericInterval.cs`** (~55 lines)

```csharp
internal readonly record struct NumericInterval(
    double Lower, bool LowerInclusive,
    double Upper, bool UpperInclusive)
{
    public static readonly NumericInterval Unknown =
        new(double.NegativeInfinity, false, double.PositiveInfinity, false);
    public static readonly NumericInterval Positive =
        new(0, false, double.PositiveInfinity, false);
    public static readonly NumericInterval Nonneg =
        new(0, true, double.PositiveInfinity, false);
    public static readonly NumericInterval Zero =
        new(0, true, 0, true);

    public bool ExcludesZero =>
        Lower > 0 || Upper < 0
        || (Lower == 0 && !LowerInclusive)
        || (Upper == 0 && !UpperInclusive);

    public bool IsNonnegative => Lower > 0 || (Lower == 0 && LowerInclusive);
    public bool IsPositive => Lower > 0 || (Lower == 0 && !LowerInclusive);
    public bool IsUnknown => double.IsNegativeInfinity(Lower) && double.IsPositiveInfinity(Upper);

    // Arithmetic operations
    public static NumericInterval Add(NumericInterval a, NumericInterval b) => ...;
    public static NumericInterval Subtract(NumericInterval a, NumericInterval b) => ...;
    public static NumericInterval Multiply(NumericInterval a, NumericInterval b) => ...;
    public static NumericInterval Divide(NumericInterval a, NumericInterval b) => ...;
    public static NumericInterval Negate(NumericInterval a) => ...;
    public static NumericInterval Abs(NumericInterval a) => ...;
    public static NumericInterval Min(NumericInterval a, NumericInterval b) => ...;
    public static NumericInterval Max(NumericInterval a, NumericInterval b) => ...;
    public static NumericInterval Clamp(NumericInterval x, NumericInterval lo, NumericInterval hi) => ...;
    public static NumericInterval Hull(NumericInterval a, NumericInterval b) => ...;
}
```

Transfer rules (standard interval arithmetic):
- `Add`: `[a,b] + [c,d] = [a+c, b+d]` (inclusivity: both inclusive → inclusive)
- `Subtract`: `[a,b] - [c,d] = [a-d, b-c]`
- `Multiply`: Sign-case decomposition to avoid `0×∞=NaN` (see design doc § Layer 2 for full case table)
- `Divide`: When divisor `ExcludesZero`, standard interval division; otherwise `Unknown`
- `Negate`: `[-b, -a]` (flip inclusivity)
- `Abs`: When both nonneg → identity; when both nonpositive → negate; mixed → `[0, max(|a|, |b|)]`
- `Min`/`Max`: elementwise min/max of bounds
- `Clamp`: `[max(x.Lower, lo.Lower), min(x.Upper, hi.Upper)]`
- `Hull`: `[min(a.Lower, b.Lower), max(a.Upper, b.Upper)]` (join for `if/then/else`). When both lower bounds are equal, `LowerInclusive = a.LowerInclusive || b.LowerInclusive`; likewise for upper.

**New method in `PreceptTypeChecker.cs`: `ExtractIntervalFromMarkers`** (~25 lines)

```
static NumericInterval ExtractIntervalFromMarkers(
    string key,
    IReadOnlyDictionary<string, StaticValueKind> symbols)
```

Reads existing `$positive:key`, `$nonneg:key`, `$nonzero:key` markers and returns the tightest interval:
- `$positive` → `(0, ∞)`
- `$nonneg` → `[0, ∞)`
- `$nonneg` + `$nonzero` → `(0, ∞)` (= Positive)
- `$nonzero` alone → Unknown (can't distinguish positive from negative nonzero)
- None → Unknown

This also reads `$ival:key` markers (new marker kind injected in Slice 12b below) for fields with explicit `min N`/`max N` constraints.

**Enhancement to `Check()` (line ~108) and `TryApplyNumericComparisonNarrowing()` (line ~2340):** When processing field constraints at initial narrowing time, inject `$ival:FieldName` markers into the symbol table as serialized `NumericInterval` values. The `FieldConstraint` records already carry the data:
- `FieldConstraint.Nonnegative` → `[0, ∞)`
- `FieldConstraint.Positive` → `(0, ∞)`
- `FieldConstraint.Min(V)` → `[V, ∞)`
- `FieldConstraint.Max(V)` → `(-∞, V]`
- `FieldConstraint.Min(V1)` + `FieldConstraint.Max(V2)` → `[V1, V2]`

Implementation note: Interval markers are encoded as `$ival:{key}` in the symbol table with a sentinel `StaticValueKind` value. The actual interval is stored in a parallel `Dictionary<string, NumericInterval>` threaded alongside the symbols, OR encoded as a string-serialized form in the marker key (simpler: `$ival:{key}:{lower}:{lowerInc}:{upper}:{upperInc}`). George's recommendation of a parallel dictionary is cleaner but requires threading a second map through the entire narrowing pipeline. The string-encoding approach keeps the API surface unchanged. **Recommendation: string-encoded markers** to minimize API churn. ~20 additional lines.

### Tests

**Unit tests for `NumericInterval` arithmetic** (new test class `NumericIntervalTests.cs` in `test/Precept.Tests/`):

| Test method | Attribute | What it verifies |
|---|---|---|
| `Add_BothPositive_ReturnsPositive` | `[Fact]` | `(0,∞) + (0,∞) = (0,∞)` |
| `Subtract_SameInterval_ContainsZero` | `[Fact]` | `[1,10] - [1,10] = [-9,9]` (does NOT exclude zero) |
| `Multiply_BothPositive_ReturnsPositive` | `[Fact]` | `(0,∞) × (0,∞) = (0,∞)` |
| `Multiply_MixedSigns_FourCorner` | `[Theory]` | 6 rows covering all sign combinations |
| `Divide_DivisorExcludesZero` | `[Fact]` | `[10,100] / [2,5] = [2,50]` |
| `Divide_DivisorContainsZero_ReturnsUnknown` | `[Fact]` | `[1,10] / [-1,1] = Unknown` |
| `Abs_Nonneg_Identity` | `[Fact]` | `abs([0,∞)) = [0,∞)` |
| `Abs_Mixed_ReturnsNonneg` | `[Fact]` | `abs([-5,3]) = [0,5]` |
| `Min_BothPositive_Positive` | `[Fact]` | `min((0,∞), (0,∞)) = (0,∞)` |
| `Max_EitherPositive_Positive` | `[Fact]` | `max((-∞,∞), (0,∞)) = (0,∞)` |
| `Clamp_WithBounds` | `[Fact]` | `clamp((-∞,∞), [5,5], [100,100]) = [5,100]` |
| `Hull_PositiveAndZero_Nonneg` | `[Fact]` | `hull((0,∞), [0,0]) = [0,∞)` |
| `ExcludesZero_Positive_True` | `[Fact]` | `(0,∞).ExcludesZero == true` |
| `ExcludesZero_Nonneg_False` | `[Fact]` | `[0,∞).ExcludesZero == false` |
| `ExcludesZero_StrictlyNegative_True` | `[Fact]` | `(-∞, 0).ExcludesZero == true` (strictly negative excludes zero) |
| `ExcludesZero_OpenUpperAtZero_True` | `[Fact]` | `(-∞, 0).ExcludesZero == true` (open upper bound at zero excludes zero) |
| `ExcludesZero_BoundedPositive_True` | `[Fact]` | `[5, 100].ExcludesZero == true` (bounded positive interval excludes zero) |
| `ExtractIntervalFromMarkers_Positive` | `[Fact]` | `$positive:X` → `(0,∞)` |
| `ExtractIntervalFromMarkers_Min5Max100` | `[Fact]` | `$ival:X:5:true:100:true` → `[5,100]` |

### Estimated size

- ~55 lines (`NumericInterval.cs`)
- ~45 lines in `PreceptTypeChecker.cs` (extraction + marker injection)
- 19 new tests

---

## Slice 13: TryInferInterval + C93/C76 Integration

**Depends on:** Slice 12
**Unblocks:** Slice 14, Slice 15

### What to create

**New method in `PreceptTypeChecker.cs`: `TryInferInterval`** (~130 lines)

```
static NumericInterval TryInferInterval(
    PreceptExpression expression,
    IReadOnlyDictionary<string, StaticValueKind> symbols)
```

Recursive walk over the expression tree, returning a `NumericInterval`:

- **`PreceptLiteralExpression { Value: long l }`** → `[l, l]`
- **`PreceptLiteralExpression { Value: double d }`** → `[d, d]`
- **`PreceptLiteralExpression { Value: decimal m }`** → `[(double)m, (double)m]`
- **`PreceptIdentifierExpression`** when `TryGetIdentifierKey` succeeds → `ExtractIntervalFromMarkers(key, symbols)`
- **`PreceptParenthesizedExpression`** → recurse into `Inner`
- **`PreceptUnaryExpression { Operator: "-" }`** → `NumericInterval.Negate(recurse(operand))`
- **`PreceptBinaryExpression`** by operator:
  - `+` → `NumericInterval.Add(left, right)`
  - `-` → `NumericInterval.Subtract(left, right)`
  - `*` → `NumericInterval.Multiply(left, right)`
  - `/` → `NumericInterval.Divide(left, right)`
  - `%` → conservative: if divisor excludes zero, result ∈ `[-(|divisor.Upper| - ε), |divisor.Upper| - ε]`; otherwise `Unknown`
- **`PreceptFunctionCallExpression`** by name:
  - `abs(x)` → `NumericInterval.Abs(recurse(x))`
  - `min(a, b)` → `NumericInterval.Min(recurse(a), recurse(b))`
  - `max(a, b)` → `NumericInterval.Max(recurse(a), recurse(b))`
  - `clamp(x, lo, hi)` → `NumericInterval.Clamp(recurse(x), recurse(lo), recurse(hi))`
  - `round(x, _)` / `ceil(x)` / `floor(x)` → `floor`: `[floor(x.Lower), floor(x.Upper)]`; `ceil`: `[ceil(x.Lower), ceil(x.Upper)]`; `round`: conservative `[floor(x.Lower), ceil(x.Upper)]` (all with closed bounds)
  - `sqrt(x)` → if `x.IsNonnegative`, result ∈ `[sqrt(x.Lower), sqrt(x.Upper)]`; else `Unknown`
  - Other functions → `Unknown`
- **`PreceptConditionalExpression`** → `NumericInterval.Hull(recurse(thenBranch, narrowedSymbols), recurse(elseBranch, symbols))`
  - Must apply `ApplyNarrowing(condition, symbols, true)` for then-branch (matching existing `TryInferKind` pattern at line ~1676)
- **Default** → `Unknown`

### What to modify

**`TryInferBinaryKind()`** (lines 2082–2135): Replace the current compound-expression fall-through comment (`// Compound expressions — no diagnostic`) with interval analysis:

```csharp
// Current: compound expressions (binary, function calls, etc.) — no diagnostic (Principle #8 conservatism)
// New:
else
{
    var divisorInterval = TryInferInterval(binary.Right, symbols);
    if (!divisorInterval.ExcludesZero)
    {
        var description = divisorInterval.IsUnknown
            ? "Divisor expression has no compile-time nonzero proof."
            : $"Divisor expression interval [{divisorInterval.Lower}, {divisorInterval.Upper}] may include zero.";
        diagnostic = new PreceptValidationDiagnostic(
            DiagnosticCatalog.C93,
            $"{description} Consider restructuring into a helper field with a 'positive' constraint or 'rule != 0'.",
            0);
    }
}
```

(~15 lines replacing 1 comment line)

**`TryInferFunctionCallKind()`** C76 sqrt check (lines 1893–1917): Replace the hard-coded `isNonNeg` pattern match with interval inference:

```csharp
// Current hard-coded pattern:
//   PreceptFunctionCallExpression { Name: "abs" } => true,
//   PreceptIdentifierExpression idArg ... $nonneg or $positive
// New: fall through to interval check for non-identifier cases
var arg = fn.Arguments[argIndex];
bool isNonNeg = arg switch
{
    PreceptLiteralExpression { Value: long lval } => lval >= 0,
    PreceptLiteralExpression { Value: double dval } => dval >= 0,
    PreceptLiteralExpression { Value: decimal mval } => mval >= 0,
    _ => TryInferInterval(arg, symbols).IsNonnegative,
};
```

This subsumes both the `abs()` special case and the identifier marker lookup, because `TryInferInterval` handles both. (~10 lines replacing ~10 lines — net zero)

### Tests (in `PreceptTypeCheckerTests.cs`)

| Test method | Attribute | What it verifies |
|---|---|---|
| `Check_DivisorCompound_PositiveTimesPositive_NoC93` | `[Fact]` | `Y / (D * C)` with D, C positive → clean |
| `Check_DivisorCompound_AbsPositive_NoC93` | `[Fact]` | `Y / abs(D)` with D positive (`$positive:` marker) → clean (abs of positive interval `(0,∞)` = `(0,∞)` which excludes zero) |
| `Check_DivisorCompound_AbsUnproven_C93` | `[Fact]` | `Y / abs(D)` with D having no proof → C93 (abs(D) is nonneg but not nonzero) |
| `Check_DivisorCompound_AdditionLiteral_NoC93` | `[Fact]` | `Y / (D + 1)` with D nonneg → clean (interval `[0,∞) + [1,1] = [1,∞)` excludes zero) |
| `Check_DivisorCompound_SubtractionSelf_C93` | `[Fact]` | `Y / (D - D)` → C93 (interval `[x,x] - [x,x]` contains zero) |
| `Check_DivisorCompound_NegativeLiteral_C93` | `[Fact]` | `Y / (D - 10)` with D nonneg but unknown bound → C93 (could be zero) |
| `Check_DivisorCompound_Min5Field_NoC93` | `[Fact]` | `Y / D` with `field D as number default 5 min 5` → clean (interval `[5,∞)` excludes zero) |
| `Check_SqrtCompound_AbsInput_NoC76` | `[Fact]` | `sqrt(abs(X))` → clean (interval nonneg) |
| `Check_SqrtCompound_ProductOfNonneg_NoC76` | `[Fact]` | `sqrt(A * A)` with A positive → clean |
| `Check_SqrtCompound_Unproven_C76` | `[Fact]` | `sqrt(A - B)` with no proof on relationship → C76 |
| `Check_DivisorCompound_ClampPositive_NoC93` | `[Fact]` | `Y / clamp(D, 1, 100)` → clean (interval `[1,100]` excludes zero) |
| `Check_DivisorCompound_MaxOfPositives_NoC93` | `[Fact]` | `Y / max(A, B)` with both positive → clean |
| `Check_DivisorCompound_PostMutationInterval_NoC93` | `[Fact]` | `set Rate = 5 -> set X = A / (Rate + 1)` → clean (L1 assigns `[5,5]` to Rate, L3 computes `[5,5]+[1,1]=[6,6]` which excludes zero). Cross-layer L1+L3 integration test. |
| `Check_DivisorModuloCompound_NonnegPlusOne_NoC93` | `[Fact]` | `Y / (D % C + 1)` with D and C positive → clean (modulo result + 1 ≥ 1, excludes zero) |
| `Check_DivisorModuloCompound_Unconstrained_C93` | `[Fact]` | `Y / (D % C)` with no constraints → C93 (modulo can be zero) |
| `Check_ProvablyZeroDivisors_Theory` | `[Theory]` | Soundness regression anchor. Rows: `D - D` → C93, `D * 0` → C93, `abs(D) - abs(D)` → C93. All divisors are provably zero; engine must never miss them. |

**Regression anchors (MUST FLIP):**
- `Check_DivisorCompound_Subtraction_NoWarning` (line 1371) — currently asserts clean for `Y / (D - D)`. After Slice 13, `D - D` interval contains zero → **update to assert C93**.
- `Check_DivisorCompound_Addition_NoWarning` (line 1336) — `Y / (D + 1)` with `D` having no constraint. The interval is `Unknown + [1,1] = Unknown`. **Update**: add a `nonnegative` constraint to `D` so interval becomes `[1,∞)` and keep `BeEmpty()`. The unconstrained-addition C93 case is already covered by `Check_DivisorCompound_NegativeLiteral_C93`.
- `Check_DivisorCompound_Multiplication_NoWarning` (line 1353) — `Y / (D * C)` with no constraints. Interval `Unknown * Unknown = Unknown`. **Update**: add `positive` constraints to both D and C and keep `BeEmpty()`. The unconstrained-multiplication case is already covered by `Check_DivisorCompound_PositiveTimesPositive_NoC93` (with constraints) and the interval produces `Unknown` → C93 for unconstrained inputs.
- `Check_DivisorCompound_AbsFunction_NoWarning` (line 1388) — `Y / abs(D)` with no nonzero proof on D. `abs(Unknown) = [0,∞)` which does NOT exclude zero. **Update**: add `positive` constraint to D so the interval becomes `(0,∞)` and `abs((0,∞)) = (0,∞)` which excludes zero; keep `BeEmpty()`. Using `positive` instead of `rule D != 0` because `$nonzero:` alone maps to `Unknown` interval and `abs(Unknown) = [0,∞)` still does not exclude zero. The unproven-abs C93 case is already covered by `Check_DivisorCompound_AbsUnproven_C93`.

**Other regression anchors (MUST PASS UNCHANGED):**
- All existing identifier C93 tests (lines 1167–1320)
- All C92 literal-zero tests
- All 25 sample `.precept` files must compile clean

### Estimated size

- ~130 new lines (`TryInferInterval`)
- ~25 changed lines (C93 compound branch + C76 simplification)
- 16 new tests + 4 updated existing tests

---

## Slice 14: Relational Inference

**Depends on:** Slice 11 (sequential flow for marker correctness)
**Independent of:** Slices 12–13 (can be built in parallel, but will benefit from interval infrastructure if built after)

### Problem

`A / (A - B)` where `rule A > B` is a common business pattern (remaining balance, net quantity, surplus). Even with interval analysis, `A - B` produces `Unknown` when A and B have overlapping independent bounds. The proof requires a relational fact.

### What to create

**New marker format:** `$gt:{A}:{B}` and `$gte:{A}:{B}` markers in the symbol table, proving `A > B` and `A >= B` respectively.

### What to modify

**`TryApplyNumericComparisonNarrowing()`** (lines 2308–2370): Currently only handles `identifier <op> literal`. Add a new branch for `identifier <op> identifier`:

```csharp
// After existing leftIsId && rightIsLit / rightIsId && leftIsLit branches:
else if (leftIsId && rightIsId)
{
    var markers = new Dictionary<string, StaticValueKind>(symbols, StringComparer.Ordinal);
    bool injected = false;

    // Canonicalized: leftKey <binary.Operator> rightKey
    switch (binary.Operator)
    {
        case ">":
            markers[$"$gt:{leftKey}:{rightKey}"] = StaticValueKind.Boolean;
            injected = true;
            break;
        case ">=":
            markers[$"$gte:{leftKey}:{rightKey}"] = StaticValueKind.Boolean;
            injected = true;
            break;
        case "<":
            markers[$"$gt:{rightKey}:{leftKey}"] = StaticValueKind.Boolean;
            injected = true;
            break;
        case "<=":
            markers[$"$gte:{rightKey}:{leftKey}"] = StaticValueKind.Boolean;
            injected = true;
            break;
    }

    if (!injected) return false;
    result = markers;
    return true;
}
```

(~25 lines added)

**New method in `PreceptTypeChecker.cs`: `TryInferRelationalNonzero`** (~40 lines)

```
static bool TryInferRelationalNonzero(
    PreceptExpression divisor,
    IReadOnlyDictionary<string, StaticValueKind> symbols)
```

Pattern-matches the divisor expression for subtraction of two identifiers and checks for relational markers:

- `PreceptBinaryExpression { Operator: "-", Left: PreceptIdentifierExpression, Right: PreceptIdentifierExpression }` → extract A, B. Check `$gt:{A}:{B}` (proves `A - B > 0`) or `$gt:{B}:{A}` (proves `B - A > 0`, so `A - B < 0`, still nonzero).
- `PreceptBinaryExpression { Operator: "-", Left: PreceptIdentifierExpression, Right: PreceptIdentifierExpression }` with `$gte:{A}:{B}` → only proves `A - B >= 0`, NOT nonzero. Need `$gte` + `$nonzero` on the difference, or `$gt` for strict inequality.
- Returns `true` if divisor is provably nonzero from relational facts.

**Integration in `TryInferBinaryKind()`** C93 check: After the interval check from Slice 13, add relational fallback:

```csharp
// After interval check:
if (!divisorInterval.ExcludesZero && !TryInferRelationalNonzero(binary.Right, symbols))
{
    // emit C93
}
```

(~5 lines changed)

### Tests (in `PreceptTypeCheckerTests.cs`)

| Test method | Attribute | What it verifies |
|---|---|---|
| `Check_DivisorRelational_AMinusB_RuleGt_NoC93` | `[Fact]` | `Y / (A - B)` with `rule A > B` → clean |
| `Check_DivisorRelational_AMinusB_GuardGt_NoC93` | `[Fact]` | `when A > B -> set Y = X / (A - B)` → clean |
| `Check_DivisorRelational_AMinusB_EnsureGt_NoC93` | `[Fact]` | `in S ensure A > B` + `Y / (A - B)` → clean |
| `Check_DivisorRelational_BMinusA_RuleGt_NoC93` | `[Fact]` | `Y / (B - A)` with `rule A > B` → clean (reversed: B - A < 0, still nonzero) |
| `Check_DivisorRelational_AMinusB_RuleGte_C93` | `[Fact]` | `Y / (A - B)` with `rule A >= B` → C93 (A >= B allows A == B → difference = 0) |
| `Check_DivisorRelational_AMinusB_NoRelation_C93` | `[Fact]` | `Y / (A - B)` with no relational proof → C93 |
| `Check_DivisorRelational_AMinusB_GuardLt_NoC93` | `[Fact]` | `when B < A -> set Y = X / (A - B)` → clean (B < A ≡ A > B) |
| `Check_DivisorRelational_EventArgGt_NoC93` | `[Fact]` | `on E ensure E.A > E.B` + `Y / (E.A - E.B)` → clean |

### Estimated size

- ~25 new lines (relational marker injection in `TryApplyNumericComparisonNarrowing`)
- ~40 new lines (`TryInferRelationalNonzero`)
- ~5 changed lines (integration in C93 check)
- 8 new tests

---

## Slice 15: Conditional Expression Proof Synthesis

**Depends on:** Slice 13 (interval infrastructure, `TryInferInterval`)

### Problem

`if Rate > 0 then Amount / Rate else 0` — the then-branch is already safe (existing narrowing at line ~1676 proves `Rate > 0`). But the RESULT of the whole conditional expression has no interval proof. If used as a divisor elsewhere, or if the else-branch is `0` and this feeds into another division, the lack of result proof is a gap.

### What already works

The type checker already narrows symbols for the then-branch (`thenSymbols = ApplyNarrowing(cond.Condition, symbols, true)` at line ~1676). Division within the then-branch already benefits from guard narrowing. This slice ensures the RESULT interval of the conditional expression is synthesized correctly.

### What to verify/modify

**`TryInferInterval`** conditional case (already spec'd in Slice 13): The `PreceptConditionalExpression` case must:

1. Compute `thenInterval = TryInferInterval(cond.ThenBranch, ApplyNarrowing(cond.Condition, symbols, true))`
2. Compute `elseInterval = TryInferInterval(cond.ElseBranch, symbols)`
3. Return `NumericInterval.Hull(thenInterval, elseInterval)`

This is already included in the Slice 13 spec. Slice 15 exists to add the **dedicated test coverage** and verify the end-to-end behavior of conditional result proofs used in downstream expressions.

### Tests (in `PreceptTypeCheckerTests.cs`)

| Test method | Attribute | What it verifies |
|---|---|---|
| `Check_DivisorConditional_BothBranchesPositive_NoC93` | `[Fact]` | `Y / (if X > 0 then X else 1)` → clean (hull of `(0,∞)` and `[1,1]` = `(0,∞)`) |
| `Check_DivisorConditional_ElseZero_C93` | `[Fact]` | `Y / (if X > 0 then X else 0)` → C93 (hull includes zero) |
| `Check_DivisorConditional_ElseNegative_C93` | `[Fact]` | `Y / (if X > 0 then X else -1)` → C93 (hull of `(0,∞)` and `[-1,-1]` = `[-1,∞)` which includes zero) |
| `Check_DivisorConditional_BothBranchesNonneg_C93` | `[Fact]` | `Y / (if X > 0 then X else abs(Z))` → C93 (else branch is nonneg but could be zero) |
| `Check_SqrtConditional_BothNonneg_NoC76` | `[Fact]` | `sqrt(if X > 0 then X else 0)` → clean (hull `[0,∞)` is nonneg) |
| `Check_DivisorConditional_RelationalThenBranch_NoC93` | `[Fact]` | `Y / (if A > B then X / (A - B) else 1)` → clean (then-branch: guard narrows `A > B` so `A - B` is nonzero via relational inference; else-branch: `[1,1]`; hull excludes zero). Cross-layer L4+L5 integration test. |

### Estimated size

- ~0 new lines in production code (conditional case already in Slice 13 `TryInferInterval`)
- 6 new tests (dedicated conditional proof coverage)

---

## Updated File Inventory

| File | Slices | Create/Modify |
|---|---|---|
| `src/Precept/Dsl/PreceptTypeChecker.cs` | 11, 12, 13, 14 | Modify |
| `src/Precept/Dsl/NumericInterval.cs` | 12 | **Create** |
| `test/Precept.Tests/PreceptTypeCheckerTests.cs` | 11, 13, 14, 15 | Modify |
| `test/Precept.Tests/NumericIntervalTests.cs` | 12 | **Create** |

### Line count summary

| Slice | New lines | Changed lines | New tests |
|---|---|---|---|
| 11: Sequential flow | ~45 | ~10 | 7 + 1 updated |
| 12: Interval struct + extraction | ~100 | ~20 | 19 |
| 13: TryInferInterval + C93/C76 | ~130 | ~25 | 16 + 4 updated |
| 14: Relational inference | ~65 | ~5 | 8 |
| 15: Conditional proof synthesis | ~0 | ~0 | 6 |
| **Total** | **~340** | **~60** | **56 new + 5 updated** |

(Conservative estimate vs George's ~500 new + ~80 changed + ~100 tests. The difference is that George assumed sign analysis as a separate layer; by subsuming it into intervals, we save ~100 lines of sign-specific code. Test count is lower because interval tests cover sign cases transitively.)

---

## Tooling/MCP Sync Assessment

### Syntax highlighting
**No changes needed.** No new keywords, operators, or expression forms are added. All changes are internal to the type checker.

### Completions
**No changes needed.** No new completion contexts are introduced.

### Semantic tokens
**No changes needed.** No new token types.

### MCP tools
**No changes needed.** `precept_compile` already returns diagnostics including C92/C93/C76. The new slices change WHEN these diagnostics fire, not their shape. No DTO changes.

### Language design doc
**Update needed after implementation.** `docs/PreceptLanguageDesign.md` should document the proof techniques the type checker uses for C93/C76. This is a doc update, not a code change, and should be part of the final commit.

---

## Regression Anchors

### Tests that MUST pass unchanged (existing behavior preserved)

- All C92 literal-zero tests
- `Check_DivisorProofSource_Theory` (line 1179) — all theory rows
- `Check_DivisorGuard_PositiveProof_NoC93` (line 1206)
- `Check_DivisorGuard_NonnegAndNonzeroProof_NoC93` (line 1225)
- `Check_DivisorModuloSameRules_NoC93` (line 1244)
- `Check_DivisorNoProof_EmitsC93` (line 1268)
- `Check_DivisorGuardNonzeroLiteral_NoC93` (line 1288)
- `Check_DivisorEventArg_EnsurePositive_NoC93` (line 1407)
- `Check_DivisorEventArg_NoProof_C93` (line 1424)
- `Check_DivisorEventArg_GuardNonzero_NoC93` (line 1440)
- `Check_DivisorEventArg_PositiveConstraint_NoC93` (line 1456)
- `Check_DivisorNullable_NullGuardOnly_C93Only` (line 1491)
- `Check_DivisorComputedField_NoProof_C93` (line 1604)
- `Check_DivisorRedundantProofs_PositiveAndRule_Clean` (line 1645)
- `Check_DivisorComplementaryProofs_NonnegAndRule_Clean` (line 1663)
- `Check_C93_IsError_NotWarning` (line 1685)

### Tests that MUST FLIP (behavior intentionally changes)

| Test | Line | Current assertion | New assertion | Why |
|---|---|---|---|---|
| `Check_DivisorIntraRowMutation_KnownLimitation_NoC93` | 1624 | `BeEmpty()` | `ContainSingle(C93)` | Sequential flow kills stale marker |
| `Check_DivisorCompound_Subtraction_NoWarning` | 1371 | `BeEmpty()` | `ContainSingle(C93)` | `D - D` interval contains zero |
| `Check_DivisorCompound_Addition_NoWarning` | 1336 | `BeEmpty()` | Add `nonnegative` constraint to D; keep `BeEmpty()` | Interval `[0,∞)+[1,1]=[1,∞)` excludes zero. Unconstrained case covered by `NegativeLiteral_C93`. |
| `Check_DivisorCompound_Multiplication_NoWarning` | 1353 | `BeEmpty()` | Add `positive` constraint to D and C; keep `BeEmpty()` | Interval `(0,∞)×(0,∞)=(0,∞)` excludes zero. Unconstrained case covered by `PositiveTimesPositive_NoC93` inverse. |
| `Check_DivisorCompound_AbsFunction_NoWarning` | 1388 | `BeEmpty()` | Add `positive` constraint to D; keep `BeEmpty()` | `abs((0,∞))=(0,∞)` excludes zero. `$nonzero:` alone → `Unknown` interval → `abs(Unknown)=[0,∞)` does NOT exclude zero. Unproven case covered by `AbsUnproven_C93`. |

### Sample file validation

All 25 `.precept` files in `samples/` must compile clean after all slices. If any sample uses compound divisors that now trigger C93, the sample must be fixed (adding appropriate constraints) — the sample was silently unsound before.

---

## Dependency Graph

```
Slice 11 (Sequential flow — soundness prerequisite)
├── Slice 12 (NumericInterval struct + extraction)
│   └── Slice 13 (TryInferInterval + C93/C76 integration)
│       └── Slice 15 (Conditional proof synthesis tests)
└── Slice 14 (Relational inference — independent of 12/13)
```

**Critical path:** 11 → 12 → 13 → 15
**Parallel track:** 14 can start after 11, independent of 12/13

---

## Open Design Decisions

1. **Interval storage format:** String-encoded markers (`$ival:key:lower:lowerInc:upper:upperInc`) vs. parallel `Dictionary<string, NumericInterval>`. Recommendation: string-encoded for minimal API churn. If threading a second dictionary proves cleaner during implementation, switch.

2. ~~**How to handle existing compound tests:**~~ **RESOLVED.** The 3 `Check_DivisorCompound_*_NoWarning` tests (Addition, Multiplication, AbsFunction) are updated to add appropriate field constraints (`nonnegative` for Addition, `positive` for Multiplication and AbsFunction) and keep `BeEmpty()`. This makes them document correct interval-proof behavior. The unconstrained variants are covered by dedicated new tests. See "Tests that MUST FLIP" table for the definitive new assertions.

3. **Modulo interval:** `A % B` interval is complex. Conservative approach: if `B.ExcludesZero`, result interval is `[-(|B.Upper|-ε), |B.Upper|-ε]`; if not, `Unknown`. This is correct but coarse.
