# Proof Engine Design

Date: 2026-04-17

Status: **Implementation in progress** — PR #108, Slices 11–15.

---

## Overview

The Precept type checker enforces divisor safety (C93) and sqrt safety (C76) at compile time. The original implementation (Slices 1–10) handles the common case: single-identifier divisors where proof markers like `$positive:`, `$nonzero:`, and `$nonneg:` are injected from field constraints, guards, rules, and ensures. An unproven identifier in divisor position emits C93.

Compound expressions — `Amount / (Rate * Factor)`, `Score / abs(Adjustment)`, `Surplus / (Produced - Defective)` — fell through with no diagnostic under Principle #8 conservatism: the compiler assumed compound expressions were satisfiable and deferred checking to the inspector at simulation time. This created a silent gap where provably-zero divisors (like `D - D`) passed compilation.

The proof engine (Slices 11–15) closes this gap. It is a non-SMT static analysis subsystem that tracks numeric bounds through expression trees using interval arithmetic, sequential assignment flow, relational inference, and conditional expression synthesis. The engine is:

- **Sound:** It never claims an expression is safe when it is not.
- **Incomplete:** It may reject expressions it cannot prove safe. This is the correct tradeoff for a DSL compiler — false negatives (missed proofs) cause author friction; false positives (wrong "safe" claims) cause runtime crashes.
- **Single-pass:** No fixpoint iteration, no widening, no solver. This is possible because of Precept's flat execution model.

## Execution Model Assumptions

The proof engine's tractability rests on three structural properties of Precept's execution model that eliminate the complexity found in general-purpose static analysis:

1. **No loops.** Precept has no iteration constructs. Expression trees are finite and acyclic. A recursive descent over any expression terminates in bounded time proportional to tree depth. There is no need for fixpoint computation or widening operators.

2. **No control-flow branches.** A transition row is a flat sequence: evaluate a guard, execute assignments left-to-right, check rules and ensures. There are no `if` statements that split execution into paths that later reconverge. Conditional *expressions* (`if/then/else`) produce a single value — both branches are type-checked, exactly one is evaluated — but they do not create control-flow divergence.

3. **No reconverging flow.** Because there are no loops or branches, there is no join point where two different proof states must be merged. Each assignment in a row sees the proof state left by all preceding assignments. This makes sequential flow analysis trivial — it is a linear walk, not a dataflow graph.

These properties mean the proof engine is a **recursive descent over finite expression trees with linear sequential context**, not an abstract interpretation framework. Standard interval arithmetic transfer rules are directly applicable without the lattice infrastructure (widen, narrow, join, meet) that general-purpose analyzers require.

## Architecture

The proof engine is organized as a five-layer stack. Each layer builds on the ones below it.

### Layer 1: Sequential Assignment Flow

**Problem solved:** Within a single transition row or state action, multiple `set` assignments execute left-to-right. Before Layer 1, all assignments in a row were validated against the same proof snapshot derived from the guard. A `set Rate = 0` did not kill the `$positive:Rate` marker for a subsequent `set X = Amount / Rate` in the same row.

**Mechanism:** `ApplyAssignmentNarrowing(targetField, rhs, symbols)` updates the proof state after each assignment by pattern-matching the right-hand side:

| RHS pattern | Marker effect |
|---|---|
| Numeric literal (sign known) | Inject/kill `$positive:`, `$nonneg:`, `$nonzero:` based on value |
| `null` literal | Kill all numeric markers; reintroduce `Null` flag |
| Identifier (markers known) | Copy source markers to target |
| Compound expression | Kill all markers on target (conservative) |

The updated proof state is threaded through the assignment `foreach` loop in both `ValidateTransitionRows()` and `ValidateStateActions()`, so each subsequent assignment sees post-mutation state.

**Integration point:** `PreceptTypeChecker.cs` — `ValidateTransitionRows()` (line ~305) and `ValidateStateActions()` (line ~410). After each `ValidateExpression` + C68 check, call `ApplyAssignmentNarrowing` and update the symbols dictionary.

### Layer 2: Interval Arithmetic

**Core abstraction:** `NumericInterval` — a closed/open interval over `double` representing the range of values an expression can produce.

```csharp
internal readonly record struct NumericInterval(
    double Lower, bool LowerInclusive,
    double Upper, bool UpperInclusive)
```

**Named intervals:**

| Name | Interval | Meaning |
|---|---|---|
| `Unknown` | `(-∞, +∞)` | No information |
| `Positive` | `(0, +∞)` | Strictly positive |
| `Nonneg` | `[0, +∞)` | Non-negative (includes zero) |
| `Zero` | `[0, 0]` | Exactly zero |

**Key predicates:**

- `ExcludesZero` — `true` when the interval provably does not contain zero. This is the primary predicate for C93 suppression.
- `IsNonnegative` — `true` when the interval's lower bound is ≥ 0. This is the primary predicate for C76 suppression.

**Transfer rules** (standard interval arithmetic):

| Operation | Rule |
|---|---|
| `Add([a,b], [c,d])` | `[a+c, b+d]` |
| `Subtract([a,b], [c,d])` | `[a-d, b-c]` |
| `Multiply([a,b], [c,d])` | Four-corner: `[min(ac,ad,bc,bd), max(ac,ad,bc,bd)]` |
| `Divide([a,b], [c,d])` | When `[c,d]` excludes zero: standard interval division. Otherwise: `Unknown`. |
| `Negate([a,b])` | `[-b, -a]` with flipped inclusivity |
| `Abs([a,b])` | Both nonneg → identity. Both nonpositive → negate. Mixed → `[0, max(\|a\|, \|b\|)]` |
| `Min([a,b], [c,d])` | `[min(a,c), min(b,d)]` |
| `Max([a,b], [c,d])` | `[max(a,c), max(b,d)]` |
| `Clamp(x, lo, hi)` | `[max(x.Lower, lo.Lower), min(x.Upper, hi.Upper)]` |
| `Hull([a,b], [c,d])` | `[min(a,c), max(b,d)]` — join for conditional expression synthesis |

**Why standard interval arithmetic suffices:** Precept expressions form finite trees with no cycles. Every transfer rule produces a result interval in O(1). The recursive walk visits each node once. There is no need for lattice widening because there is no iteration that could cause unbounded interval growth. The `Unknown` interval serves as top — any operation involving `Unknown` produces `Unknown` unless the operation itself bounds the result (e.g., `abs(Unknown)` produces `[0, +∞)`).

**Interval extraction from proof markers:** `ExtractIntervalFromMarkers(key, symbols)` reads the existing string-encoded markers for an identifier and returns the tightest interval:

| Markers present | Interval |
|---|---|
| `$positive:` | `(0, +∞)` |
| `$nonneg:` | `[0, +∞)` |
| `$nonneg:` + `$nonzero:` | `(0, +∞)` |
| `$nonzero:` alone | `Unknown` (nonzero spans both positive and negative) |
| `$ival:key:lower:lowerInc:upper:upperInc` | Decoded interval from field constraints (`min N`, `max N`) |
| None | `Unknown` |

**Interval marker injection from field constraints:** At initial narrowing time (during `Check()`), field constraints are encoded as interval markers:

| Constraint | Marker interval |
|---|---|
| `nonnegative` | `[0, +∞)` |
| `positive` | `(0, +∞)` |
| `min V` | `[V, +∞)` |
| `max V` | `(-∞, V]` |
| `min V1` + `max V2` | `[V1, V2]` |

**Storage format decision:** Interval markers use string-encoded keys (`$ival:key:lower:lowerInc:upper:upperInc`) in the existing symbol table rather than a parallel `Dictionary<string, NumericInterval>`. This avoids threading a second dictionary through the entire narrowing pipeline and keeps the API surface unchanged. The tradeoff is parse overhead on extraction, which is negligible — extraction happens once per identifier per expression tree visit.

### Layer 3: Interval Inference

**Core method:** `TryInferInterval(expression, symbols)` — a recursive walk over the expression tree that returns a `NumericInterval`.

**Dispatch table:**

| Expression node | Interval computation |
|---|---|
| Numeric literal | Point interval `[v, v]` |
| Identifier | `ExtractIntervalFromMarkers(key, symbols)` |
| Parenthesized | Recurse into inner expression |
| Unary `-` | `NumericInterval.Negate(recurse(operand))` |
| Binary `+` | `NumericInterval.Add(left, right)` |
| Binary `-` | `NumericInterval.Subtract(left, right)` |
| Binary `*` | `NumericInterval.Multiply(left, right)` |
| Binary `/` | `NumericInterval.Divide(left, right)` |
| Binary `%` | Conservative: if divisor excludes zero, bounded by divisor magnitude; else `Unknown` |
| `abs(x)` | `NumericInterval.Abs(recurse(x))` |
| `min(a, b)` | `NumericInterval.Min(recurse(a), recurse(b))` |
| `max(a, b)` | `NumericInterval.Max(recurse(a), recurse(b))` |
| `clamp(x, lo, hi)` | `NumericInterval.Clamp(recurse(x), recurse(lo), recurse(hi))` |
| `sqrt(x)` | If `x.IsNonnegative`: `[√Lower, √Upper]`; else `Unknown` |
| `round(x, _)` / `ceil(x)` / `floor(x)` | Same interval as `x` (rounding preserves sign and zero-exclusion) |
| Conditional `if/then/else` | See Layer 5 |
| Other | `Unknown` |

**C93 integration in `TryInferBinaryKind()`:** The current compound-expression fallthrough (`// Compound expressions — no diagnostic`) is replaced with:

```csharp
var divisorInterval = TryInferInterval(binary.Right, symbols);
if (!divisorInterval.ExcludesZero)
{
    // Also check relational inference (Layer 4) before emitting
    if (!TryInferRelationalNonzero(binary.Right, symbols))
    {
        // Emit C93 with interval-aware message
    }
}
```

**C76 integration in `TryInferFunctionCallKind()`:** The hard-coded `abs()` and identifier pattern match for sqrt safety is replaced with:

```csharp
var argInterval = TryInferInterval(arg, symbols);
bool isNonNeg = argInterval.IsNonnegative;
```

This subsumes the `abs()` special case (because `TryInferInterval` handles `abs()` via `NumericInterval.Abs`) and the identifier marker lookup (because `ExtractIntervalFromMarkers` is called for identifiers).

### Layer 4: Relational Inference

**Problem solved:** `A / (A - B)` where `rule A > B` is a common business pattern (remaining balance, net quantity, surplus). Interval arithmetic cannot prove `A - B` excludes zero when A and B have overlapping independent bounds — their relationship is lost in the interval abstraction. This requires a relational fact.

**Marker format:** `$gt:{A}:{B}` and `$gte:{A}:{B}` in the symbol table, proving `A > B` and `A >= B` respectively.

**Injection point:** `TryApplyNumericComparisonNarrowing()` (line ~2340). The existing method handles `identifier <op> literal`. A new branch handles `identifier <op> identifier`:

| Guard/rule pattern | Marker injected |
|---|---|
| `A > B` | `$gt:{A}:{B}` |
| `A >= B` | `$gte:{A}:{B}` |
| `B < A` | `$gt:{A}:{B}` (canonicalized) |
| `B <= A` | `$gte:{A}:{B}` (canonicalized) |

**Proof method:** `TryInferRelationalNonzero(divisor, symbols)` pattern-matches the divisor for `A - B` (subtraction of two identifiers) and checks:

- `$gt:{A}:{B}` → `A > B` → `A - B > 0` → nonzero ✓
- `$gt:{B}:{A}` → `B > A` → `A - B < 0` → nonzero ✓
- `$gte:{A}:{B}` → `A >= B` → `A - B >= 0` → NOT provably nonzero (allows equality)

The method returns `true` only when strict inequality is proven.

**Why relational inference is a separate layer:** Interval arithmetic operates on individual variable bounds. Relational facts capture inter-variable constraints (`A > B`) that intervals cannot represent — the relationship between two variables is lost when each is independently bounded. This is a fundamental limitation of non-relational abstract domains (intervals are the canonical non-relational domain). Rather than upgrading to a relational domain (octagons, polyhedra) — which would be massive overkill for a DSL compiler — we harvest a targeted class of relational facts (direct comparisons between identifiers) and check them as a separate fallback after interval analysis.

### Layer 5: Conditional Expression Proof Synthesis

**Problem solved:** `if Rate > 0 then Amount / Rate else 0` — the then-branch is already safe (existing narrowing proves `Rate > 0`). But the *result* of the whole conditional expression has no interval. If used as a divisor elsewhere, the lack of a result interval is a proof gap.

**Mechanism:** The `PreceptConditionalExpression` case in `TryInferInterval` computes:

1. `thenInterval = TryInferInterval(thenBranch, ApplyNarrowing(condition, symbols, true))`
2. `elseInterval = TryInferInterval(elseBranch, symbols)`
3. `result = NumericInterval.Hull(thenInterval, elseInterval)`

**Why Hull is correct:** In Precept's execution model, exactly one branch of a conditional expression is evaluated at runtime. The result is either `thenInterval` or `elseInterval` — the proof engine must be sound for either case. Hull (the smallest interval containing both) is the correct over-approximation because there is no control-flow join where a more precise analysis could apply. There is no path-sensitivity to exploit — the conditional expression is a single value-producing node, not a branching construct.

**Example:**

```precept
field X as number positive
# if X > 5 then X else 1
# thenInterval: [5, +∞) (narrowed from X > 5)
# elseInterval: [1, 1]
# Hull: [1, +∞) — ExcludesZero = true
```

## Proof Flow

End-to-end flow for a compound divisor expression reaching `TryInferBinaryKind`:

```
1. Field constraints (positive, min N, etc.)
   └─→ Inject $positive:, $nonneg:, $nonzero:, $ival: markers into base symbols

2. Guard narrowing (when Field > 0, when Field != 0)
   └─→ Inject markers via ApplyNarrowing + TryApplyNumericComparisonNarrowing

3. Rule/ensure narrowing (rule A > B, ensure X > 0)
   └─→ Inject markers including relational $gt:/$gte: markers

4. Sequential assignment flow (set Rate = 0 → set X = A / Rate)
   └─→ ApplyAssignmentNarrowing updates/kills markers between assignments

5. Divisor expression reaches TryInferBinaryKind C93 check:
   a. Is divisor a literal zero? → C92
   b. Is divisor a single identifier? → Check $nonzero:/$positive: markers
   c. Is divisor a compound expression? → TryInferInterval:
      - Recursive walk builds interval from sub-expressions
      - Each identifier extracts interval from markers
      - Each operation applies standard transfer rule
      - Result: NumericInterval for entire divisor

6. ExcludesZero check on result interval:
   - true  → No diagnostic (divisor provably nonzero)
   - false → Fall through to relational check

7. TryInferRelationalNonzero (Layer 4 fallback):
   - Divisor is A - B with $gt:{A}:{B}? → No diagnostic
   - Otherwise → Emit C93 with interval-aware message
```

## Integration Points

| Integration point | File | Location | Change |
|---|---|---|---|
| Sequential flow wiring | `PreceptTypeChecker.cs` | `ValidateTransitionRows()` ~line 305, `ValidateStateActions()` ~line 410 | Thread `ApplyAssignmentNarrowing` through assignment loops |
| C93 compound branch | `PreceptTypeChecker.cs` | `TryInferBinaryKind()` ~line 2110 | Replace fallthrough comment with `TryInferInterval` + `TryInferRelationalNonzero` |
| C76 sqrt check | `PreceptTypeChecker.cs` | `TryInferFunctionCallKind()` ~line 1893 | Replace hard-coded pattern match with `TryInferInterval(...).IsNonnegative` |
| Interval markers | `PreceptTypeChecker.cs` | `Check()` ~line 108 | Inject `$ival:` markers from field `min`/`max` constraints |
| Relational markers | `PreceptTypeChecker.cs` | `TryApplyNumericComparisonNarrowing()` ~line 2340 | New `identifier <op> identifier` branch |
| Interval struct | `NumericInterval.cs` | New file in `src/Precept/Dsl/` | `NumericInterval` record struct + transfer rules |

## Soundness Guarantees

### What the engine proves

- **Identifier divisors:** Provably nonzero via `$positive:`, `$nonzero:`, or (`$nonneg:` + `$nonzero:`) markers. No change from Slices 1–10.
- **Compound divisors:** Provably nonzero via interval arithmetic — the result interval's `ExcludesZero` predicate.
- **Relational divisors:** `A - B` provably nonzero when strict inequality `A > B` or `B > A` is proven from guards, rules, or ensures.
- **Conditional expression results:** Provably nonzero when the hull of both branch intervals excludes zero.
- **Sqrt arguments:** Provably non-negative via interval arithmetic — the argument interval's `IsNonnegative` predicate.
- **Sequential flow:** Post-mutation proof state correctly reflects assignment effects. A `set Rate = 0` kills the nonzero proof for subsequent uses of `Rate` in the same row.

### What the engine conservatively rejects

These patterns emit C93 even though a human could verify them safe:

- **Inter-event reasoning:** `on SetRate ensure Rate > 0` does not prove `Rate > 0` in a different event's transition row. Each event's proof context is independent.
- **Aliased fields:** `set Backup = Rate` followed by `X / Backup` — compound RHS kills markers (Layer 1 conservatism). The engine does not track that `Backup` holds the value of `Rate`.
- **Deeply nested conditionals:** The engine handles one level of `if/then/else` via Hull. Nested conditionals compose correctly (Hull of Hull) but the resulting interval may be very wide, leading to inconclusive proofs.
- **Non-linear relational patterns:** `rule A * B > 0` does not produce a relational marker. Only direct `identifier <op> identifier` comparisons are harvested.
- **Modulo with variable divisor:** `A % B` where B's magnitude is not bounded by known constraints produces `Unknown`, even if the author knows B is bounded.

### The right tradeoff

A DSL compiler serves domain authors, not PL researchers. The cost of a false positive (claiming safe when it isn't) is a runtime crash — catastrophic in a business rules engine. The cost of a false negative (rejecting a safe expression) is author friction — the author adds a constraint or restructures the expression. The engine is calibrated for zero false positives at the cost of some false negatives. Authors who hit a false negative have clear remediation: add a field constraint (`positive`, `min 1`), a rule (`rule X != 0`), or restructure into a guarded expression (`when X != 0 -> ...`).

## Design Decisions

### 1. String-Encoded Interval Markers

**Decision:** Interval data from field constraints (`min N`, `max N`) is stored as string-encoded markers (`$ival:key:lower:lowerInc:upper:upperInc`) in the existing `IReadOnlyDictionary<string, StaticValueKind>` symbol table.

**Alternative rejected:** Parallel `Dictionary<string, NumericInterval>` threaded alongside the symbol table through all narrowing methods.

**Rationale:** The symbol table is already threaded through every narrowing and validation method. Adding a second dictionary would require changing the signature of ~15 methods and every call site. String-encoded markers keep the API surface unchanged. The parse overhead on extraction (one `string.Split` per identifier per expression visit) is negligible compared to the expression validation work already being done.

**Tradeoff accepted:** String encoding is less type-safe than a dedicated dictionary. A malformed marker string would silently produce `Unknown` rather than a compile error. This is acceptable because marker injection and extraction are co-located in the same file and covered by unit tests.

### 2. Subsuming Sign Analysis into Intervals

**Decision:** There is no separate "sign analysis" layer. Sign information is a special case of interval bounds — `Positive` is `(0, +∞)`, `Nonneg` is `[0, +∞)`, `Nonzero` is the complement of `{0}` (not directly representable as a single interval, but handled via the existing marker check as a fallback).

**Alternative rejected:** Dedicated sign domain (Positive / Negative / Nonneg / Nonpositive / Zero / Nonzero / Unknown) with its own transfer rules, running as a parallel analysis alongside intervals.

**Rationale:** A sign domain duplicates information already captured by interval bounds. Every sign transfer rule is a special case of the corresponding interval transfer rule. Maintaining two parallel analyses adds ~100 lines of code with no additional proving power — intervals subsume everything sign analysis can do, plus they handle bounded ranges (`min 5`, `clamp(x, 1, 100)`) that sign analysis cannot.

**Tradeoff accepted:** `Nonzero` (the union `(-∞, 0) ∪ (0, +∞)`) is not representable as a single interval. The engine falls back to the existing `$nonzero:` marker check for this case. This is not a regression — Slices 1–10 already handle `$nonzero:` markers for identifier divisors.

### 3. Hull for Conditional Expressions

**Decision:** The result interval of `if/then/else` is the hull (smallest enclosing interval) of the then-branch and else-branch intervals.

**Alternative considered:** Path-sensitive analysis that tracks which branch was taken and narrows accordingly.

**Rationale:** Precept's conditional expressions are value-producing nodes, not control-flow constructs. There is no post-conditional code path where the branch choice matters — the result is a single value used in the enclosing expression. Hull is sound (it contains both possible results) and optimal for this use case (no precision is lost because there is no subsequent narrowing opportunity). Path-sensitive analysis would add complexity with no benefit.

### 4. Relational Markers as a Separate Layer

**Decision:** `$gt:{A}:{B}` and `$gte:{A}:{B}` markers are harvested from guards/rules/ensures and checked in `TryInferRelationalNonzero` as a fallback after interval analysis fails.

**Alternative considered:** Encoding relational information into the interval domain itself (e.g., constraining `A - B` to a positive interval when `A > B` is known).

**Rationale:** Intervals are a non-relational abstract domain by definition — they track bounds per variable, not relationships between variables. Encoding `A > B` as a constraint on `A - B` would require tracking synthetic expressions in the symbol table, which is architecturally invasive. The separate relational layer is surgically targeted: it handles the specific pattern (`A - B` in divisor position with a known `A > B` fact) that intervals cannot and does so in ~40 lines. The scope is deliberately narrow — only direct `identifier <op> identifier` comparisons, not arbitrary relational constraints.

**Tradeoff accepted:** The relational layer only handles subtraction of two identifiers. `A / (A - B - C)` with `rule A > B + C` is not recognized. This covers the dominant business pattern (pairwise comparison) and avoids the complexity of general relational reasoning.

## Limitations and Future Work

### Deliberate exclusions

- **Inter-event proof propagation:** Each event's transition rows are validated with their own proof context. A `positive` constraint or rule applies everywhere, but guard narrowing in one event does not carry to another. This is correct — events are independent entry points.

- **Alias tracking through assignments:** `set Backup = Rate` does not propagate Rate's markers to Backup when the RHS is a compound expression (Layer 1 conservatism). Identifier-to-identifier copies DO propagate markers. Full alias tracking would require must-alias analysis, which is disproportionate for a DSL compiler.

- **Non-linear arithmetic:** Multiplication of two unbounded variables produces `Unknown` bounds (the four-corner rule with `±∞` extremes). This is correct but coarse. Proving `X * X > 0` from `X != 0` would require squaring analysis — a special case not yet implemented.

- **Disjunctive intervals:** `Nonzero` is `(-∞, 0) ∪ (0, +∞)` — a union of two intervals. The engine uses a single interval representation and cannot express this directly. The existing `$nonzero:` marker system covers the identifier case; compound expressions involving `nonzero` variables may lose precision.

### Potential future enhancements

- **`$nonzero:` interval encoding:** Represent `nonzero` as a flag on `NumericInterval` rather than a separate marker, enabling compound expressions involving nonzero variables to benefit from interval arithmetic.
- **Must-alias tracking for identifier copies:** When `set A = B` is detected, copy all markers from B to A in `ApplyAssignmentNarrowing`. Already implemented for simple identifier RHS; could be extended to `abs(B)`, `max(B, literal)`, etc.
- **Product sign analysis:** `X * Y` is nonzero when both X and Y are nonzero. Currently only proven when both intervals exclude zero; could also check `$nonzero:` markers for each factor.
