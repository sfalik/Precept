# Proof Engine Design

Date: 2026-04-17

Status: **Shipped** — PR #108, Slices 11–15 (commits `8fd9746`–`adbedfa`).

### Implementation Notes (post-ship)

Fixes discovered during test authoring that refine the design:

- **`TryInferRelationalNonzero` must `StripParentheses`** before pattern-matching. Parenthesized divisors like `(A - B)` wrap the binary expression in `PreceptParenthesizedExpression`, which the `is not PreceptBinaryExpression` check rejected.
- **`NumericInterval.Multiply` zero-interval fast path.** `(0,∞) × [0,0]` produces `NaN` via IEEE 754 `∞*0`. Added early return for `[0,0]` inputs.
- **Modulo interval tightened for non-negative dividend.** When dividend is nonnegative and divisor is positive, result ∈ `[0, |B|)` (not `(-|B|, |B|)`). Enables `D % C + 1` to prove nonzero.
- **`BuildEventEnsureNarrowings` multi-field relational markers.** Relational markers like `$gt:A:B` have two field names separated by `:`. The bare→dotted translation must dot both fields independently: `$gt:A:B` → `$gt:Go.A:Go.B`.

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

**Integration point:** `PreceptTypeChecker.cs` — `ValidateTransitionRows()` (line ~195) and `ValidateStateActions()` (line ~354). After each `ValidateExpression` + C68 check, call `ApplyAssignmentNarrowing` and update the symbols dictionary.

> **Note:** Line numbers are approximate and will shift during implementation.

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
| `Multiply([a,b], [c,d])` | Sign-case decomposition (see below) |
| `Divide([a,b], [c,d])` | When `[c,d]` excludes zero: standard interval division. Otherwise: `Unknown`. |
| `Negate([a,b])` | `[-b, -a]` with flipped inclusivity |
| `Abs([a,b])` | Both nonneg → identity. Both nonpositive → negate. Mixed → `[0, max(\|a\|, \|b\|)]` |
| `Min([a,b], [c,d])` | `[min(a,c), min(b,d)]` |
| `Max([a,b], [c,d])` | `[max(a,c), max(b,d)]` |
| `Clamp(x, lo, hi)` | `[max(x.Lower, lo.Lower), min(x.Upper, hi.Upper)]` |
| `Hull([a,b], [c,d])` | `[min(a,c), max(b,d)]` — join for conditional expression synthesis. **Inclusivity for equal bounds:** when both lower bounds are equal, `LowerInclusive = a.LowerInclusive \|\| b.LowerInclusive`; likewise when both upper bounds are equal, `UpperInclusive = a.UpperInclusive \|\| b.UpperInclusive`. |

**Multiply sign-case decomposition:** Naive four-corner multiplication (`min/max` of `{a*c, a*d, b*c, b*d}`) produces `NaN` when an endpoint is zero and the other is `±∞` (because `0 × ∞` is undefined in IEEE 754). The implementation decomposes by sign combination to avoid `0 × ∞`:

| Case | Condition | Result |
|---|---|---|
| Both positive | `a ≥ 0 && c ≥ 0` | `[a*c, b*d]` |
| Both negative | `b ≤ 0 && d ≤ 0` | `[b*d, a*c]` |
| Left positive, right negative | `a ≥ 0 && d ≤ 0` | `[b*c, a*d]` |
| Left negative, right positive | `b ≤ 0 && c ≥ 0` | `[a*d, b*c]` |
| Left positive, right mixed | `a ≥ 0 && c < 0 && d > 0` | `[b*c, b*d]` |
| Left negative, right mixed | `b ≤ 0 && c < 0 && d > 0` | `[a*d, a*c]` |
| Left mixed, right positive | `a < 0 && b > 0 && c ≥ 0` | `[a*d, b*d]` |
| Left mixed, right negative | `a < 0 && b > 0 && d ≤ 0` | `[b*c, a*c]` |
| Both mixed | `a < 0 && b > 0 && c < 0 && d > 0` | `[min(a*d, b*c), max(a*c, b*d)]` |

Inclusive bounds follow: `LowerInclusive = true` when the contributing factors' relevant bounds are both inclusive. The "both mixed" case is the only one that still uses `min/max` of four products, but all four products involve finite nonzero factors (no `0 × ∞`).

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

**Min+max combination algorithm:** When `Check()` processes field constraints, it must detect and combine `min` and `max` constraints on the same field into a single `$ival:` marker rather than injecting two separate markers. Algorithm sketch: iterate over each field’s `FieldConstraint` records. Collect `min V` and `max V` values for each field. If both exist, inject a single `$ival:key:V1:true:V2:true` marker (the combined `[V1, V2]` interval). If only `min V` exists, inject `$ival:key:V:true:Infinity:false`. If only `max V` exists, inject `$ival:key:-Infinity:false:V:true`. The `nonnegative` and `positive` constraints are handled by their existing `$nonneg:` and `$positive:` markers, which `ExtractIntervalFromMarkers` already reads; they do NOT need a duplicate `$ival:` marker.

**Storage format decision:** Interval markers use string-encoded keys (`$ival:key:lower:lowerInc:upper:upperInc`) in the existing symbol table rather than a parallel `Dictionary<string, NumericInterval>`. This avoids threading a second dictionary through the entire narrowing pipeline and keeps the API surface unchanged. The tradeoff is parse overhead on extraction, which is negligible — extraction happens once per identifier per expression tree visit. **All numeric values in `$ival:` markers MUST be serialized and parsed using `CultureInfo.InvariantCulture`** to prevent locale-dependent decimal separator issues (e.g., `5.0` vs `5,0`).

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
| `floor(x)` | `[floor(x.Lower), floor(x.Upper)]` with closed bounds |
| `ceil(x)` | `[ceil(x.Lower), ceil(x.Upper)]` with closed bounds |
| `round(x, _)` | Conservative: `[floor(x.Lower), ceil(x.Upper)]` with closed bounds |
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

**Injection point:** `TryApplyNumericComparisonNarrowing()` (line ~2308). The existing method handles `identifier <op> literal`. A new branch handles `identifier <op> identifier`:

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
| Sequential flow wiring | `PreceptTypeChecker.cs` | `ValidateTransitionRows()` ~line 195, `ValidateStateActions()` ~line 354 | Thread `ApplyAssignmentNarrowing` through assignment loops |
| C93 compound branch | `PreceptTypeChecker.cs` | `TryInferBinaryKind()` ~line 1921 | Replace fallthrough comment with `TryInferInterval` + `TryInferRelationalNonzero` |
| C76 sqrt check | `PreceptTypeChecker.cs` | `TryInferFunctionCallKind()` ~line 1722 | Replace hard-coded pattern match with `TryInferInterval(...).IsNonnegative` |
| Interval markers | `PreceptTypeChecker.cs` | `Check()` ~line 89 | Inject `$ival:` markers from field `min`/`max` constraints |
| Relational markers | `PreceptTypeChecker.cs` | `TryApplyNumericComparisonNarrowing()` ~line 2308 | New `identifier <op> identifier` branch |
| Interval struct | `NumericInterval.cs` | New file in `src/Precept/Dsl/` | `NumericInterval` record struct + transfer rules |

> **Note:** Line numbers are approximate references to the current codebase and will shift during implementation.

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

## Numeric Precision and IEEE 754

The proof engine operates on `double` (IEEE 754 binary64) internally. This introduces three corner cases that are deliberately handled for soundness:

- **Overflow saturates to ±∞.** When an arithmetic operation produces a result beyond `double.MaxValue`, IEEE 754 rounds to `+∞` or `-∞`. This is sound — an interval containing `+∞` or `-∞` is a valid over-approximation. No false "safe" claims result from overflow.
- **NaN inputs produce Unknown-equivalent behavior.** If a `NaN` value enters the interval system (e.g., from a malformed literal or an impossible operation), the affected interval is treated as `Unknown`. The `ExcludesZero` and `IsNonnegative` predicates return `false` for any interval containing `NaN`, which is conservative — the engine rejects rather than falsely approves.
- **Decimal→double cast is a slight widening.** Precept field values use `decimal` at runtime, but interval arithmetic uses `double`. The cast from `decimal` to `double` may widen the value slightly (e.g., `0.1m` → `0.1d` ≈ `0.100000000000000005...`). This is soundness-preserving — a slightly wider interval never causes a false "safe" claim. It may very rarely cause a false "unsafe" claim for values extremely close to zero, which is acceptable given the engine's conservative design.

## Limitations and Future Work

### Deliberate exclusions

- **Inter-event proof propagation:** Each event's transition rows are validated with their own proof context. A `positive` constraint or rule applies everywhere, but guard narrowing in one event does not carry to another. This is correct — events are independent entry points.

- **Alias tracking through assignments:** `set Backup = Rate` does not propagate Rate's markers to Backup when the RHS is a compound expression (Layer 1 conservatism). Identifier-to-identifier copies DO propagate markers. Full alias tracking would require must-alias analysis, which is disproportionate for a DSL compiler.

- **Non-linear arithmetic:** Multiplication of two unbounded variables produces `Unknown` bounds (the four-corner rule with `±∞` extremes). This is correct but coarse. Proving `X * X > 0` from `X != 0` would require squaring analysis — a special case not yet implemented.

- **Disjunctive intervals:** `Nonzero` is `(-∞, 0) ∪ (0, +∞)` — a union of two intervals. The engine uses a single interval representation and cannot express this directly. The existing `$nonzero:` marker system covers the identifier case; compound expressions involving `nonzero` variables may lose precision.

### Potential precision enhancements

These enhance existing analysis precision without adding new enforcement categories:

- **`$nonzero:` interval encoding:** Represent `nonzero` as a flag on `NumericInterval` rather than a separate marker, enabling compound expressions involving nonzero variables to benefit from interval arithmetic.
- **Must-alias tracking for identifier copies:** When `set A = B` is detected, copy all markers from B to A in `ApplyAssignmentNarrowing`. Already implemented for simple identifier RHS; could be extended to `abs(B)`, `max(B, literal)`, etc.
- **Product sign analysis:** `X * Y` is nonzero when both X and Y are nonzero. Currently only proven when both intervals exclude zero; could also check `$nonzero:` markers for each factor.

For the comprehensive enforcement catalog — assignment constraint checking, dead rule/guard detection, transition reachability sharpening, and cross-event invariant analysis — see **§ Comprehensive Compile-Time Enforcement** below.

---

## Optimality Assessment

**Verdict: The layered, non-SMT interval+relational design is the correct architecture for Precept's constraint surface. No structural revision needed.**

### Rationale

1. **Precept's execution model eliminates abstract-interpretation overhead.** No loops, no branches, no reconverging flow (§ Execution Model Assumptions). Single-pass interval arithmetic is not just sufficient — it is *optimal*. Widening, narrowing, fixpoint iteration, and lattice joins are structurally unnecessary because there are no program points where multiple execution paths converge. (Principle #1: deterministic, inspectable model.)

2. **The constraint surface is entirely interval-compatible.** Every numeric constraint (`nonnegative`, `positive`, `min N`, `max N`) maps directly to a closed/open interval. Collection constraints (`mincount`, `maxcount`) and string constraints (`minlength`, `maxlength`) map to integer count intervals over `.count` and `.length` accessors. No constraint in the DSL today requires relational or disjunctive reasoning that intervals cannot express.

3. **SMT is overkill.** Z3/CVC5 would bring 50–65 MB of native dependencies, non-deterministic solving times, opaque proof witnesses, and API-surface complexity — all for a constraint surface that is decidable with interval arithmetic alone. This violates Principle #9 (tooling drives syntax — structured diagnostics, not opaque solver output) and Principle #12 (AI legibility — proof witnesses must be inspectable).

4. **A fuller abstract-interpretation lattice is disproportionate.** Octagons (O(n²) per variable) or polyhedra (exponential worst case) would handle multi-variable relationships but at ~2,000+ lines of implementation for a DSL compiler serving business domain authors. The targeted relational layer (Layer 4) handles the dominant inter-variable pattern (`A - B` with `A > B`) in ~65 lines — 97% of the benefit at 3% of the cost.

5. **A simpler pattern-based approach is too weak.** Pattern matching (recognize `abs()`, `max()`, etc.) misses compound expressions. The interval system proves things like `clamp(D, 1, 100)` excludes zero, `Score + Amount ∈ [1, ∞)` from constraint-derived intervals, and `if X > 0 then X else 1 ∈ (0, ∞)` via Hull — no pattern matcher can match this generality.

6. **Philosophy alignment is direct.** The design serves Principle #1 (deterministic — same input → same proof), Principle #8 (sound compile-time-first — never false-positive, conservative on unknowns), Principle #9 (tooling drives syntax — structured diagnostics with interval witnesses for hover/preview), and Principle #12 (AI legibility — proof witnesses are data, not opaque solver traces).

### Alternatives considered and rejected

| Alternative | Why rejected |
|---|---|
| Constraint propagation graph | Adds node-based structure with no benefit over direct interval computation — Precept expressions are trees, not constraint networks |
| Symbolic execution | Collapses to interval arithmetic when there are no branches — Precept's model makes symbolic execution degenerate |
| SMT-backed verification | Disproportionate dependency cost; opaque proofs violate Principle #12; non-deterministic timing |
| Octagon / polyhedra domains | O(n²)/exponential cost for multi-variable relations; Layer 4's targeted relational inference covers the dominant pattern |
| Property testing / fuzzing | Complementary at test time, not a compile-time analysis technique |

### Architectural smells

None detected. Each layer has a single responsibility, layers compose vertically (higher layers call lower ones), and the overall architecture is a single-pass recursive descent over finite expression trees with linear sequential context. The marker-based proof state is a natural fit for the existing symbol table infrastructure.

---

## Comprehensive Compile-Time Enforcement

The proof engine's interval infrastructure (Layers 2–5) enables enforcement beyond C93 (divisor safety) and C76 (sqrt safety). This section catalogs every construct with compile-time-checkable semantics and specifies how the proof engine reasons about each.

### Governing Policy: Proven Violation Only

All enforcements follow a single policy, grounded in Principles #1 and #8:

| Proof outcome | Action |
|---|---|
| **Proven violation** (expression interval entirely outside constraint interval) | Diagnostic (error or warning per construct) |
| **Possible violation** (partial overlap — some values safe, some not) | No diagnostic — the runtime invariant system handles runtime-data-dependent cases cleanly |
| **Proven safe** (expression interval entirely within constraint interval) | No diagnostic |
| **Unknown** (interval is Unknown or analysis is inconclusive) | No diagnostic (Principle #8 conservatism) |

The engine NEVER fires a diagnostic on code that might be safe at runtime. The runtime invariant system exists precisely to handle the "possible violation" cases — warning about its normal operation would be noise, not signal.

### Enforcement Summary Table

| Construct | Enforcement | Diag | Sev | Algorithm | Phase |
|---|---|---|---|---|---|
| `set Field = expr` (numeric) | Expr interval provably outside field constraint interval | C94 | Error | L2–L3 interval containment | Follow-up A |
| `to/from State -> set ...` | Same for state actions | C94 | Error | Same | Follow-up A |
| Computed `field -> expr` | Formula interval provably violates computed field constraint | C94 | Error | Same | Follow-up A |
| `rule expr because "..."` | Rule predicate contradicts field constraints (unsatisfiable) | C95 | Error | L2 interval intersection | Follow-up B |
| `rule expr because "..."` | Rule predicate always true given constraints (vacuous) | C96 | Warning | L2 interval containment | Follow-up B |
| `when guard` (row/edit/ensure) | Guard provably always false | C97 | Warning | L2–L4 proof evaluation | Follow-up C |
| `when guard` (row/edit/ensure) | Guard provably always true | C98 | Warning | L2–L4 proof evaluation | Follow-up C |
| Transition rows via C97 | Dead row sharpens C50/C51 | C50/C51 | Warning (existing) | Via C97 | Follow-up C |
| State reachability via C97 | All incoming rows dead sharpens C48 | C48 | Warning (existing) | Via C97 | Follow-up D |
| Cross-event field invariant | Field always in range across all reachable states | C99 | Info | State-graph fixed-point | Follow-up E |
| Boolean guard refinement | Boolean field narrowing to {true}/{false} | — | — | Existing narrowing | Already shipped |
| Choice assignment | Literal not in choice set | C68 | Error (existing) | Existing | Already shipped |
| Divisor safety | Divisor provably zero / unproven nonzero | C92/C93 | Error | L1–L5 | PR #108 |
| Sqrt safety | Argument provably negative / unproven non-negative | C76 | Error | L2–L3 | PR #108 |

### C94: Assignment Constraint Enforcement

**What:** Every `set Field = expr` where the target field has numeric constraints (`nonnegative`, `positive`, `min N`, `max N`). Covers transition row bodies, state actions, and computed field formulas.

**Algorithm:**

1. At each assignment site, compute the target field's **constraint interval** by combining all declared constraints:

| Constraint(s) | Constraint interval |
|---|---|
| `nonnegative` | `[0, +∞)` |
| `positive` | `(0, +∞)` |
| `min N` | `[N, +∞)` |
| `max N` | `(-∞, N]` |
| `min N` + `max M` | `[N, M]` |
| `positive` + `max M` | `(0, M]` |
| Multiple constraints | Intersection of all individual intervals |

2. Compute `exprInterval = TryInferInterval(expr, symbols)` using L2–L3.

3. Check: `!NumericInterval.Intersects(exprInterval, constraintInterval)` → **C94 error**.

**New method on `NumericInterval`:**

```
static bool Intersects(NumericInterval a, NumericInterval b):
    if a.Upper < b.Lower then false
    if a.Lower > b.Upper then false
    if a.Upper == b.Lower && !(a.UpperInclusive && b.LowerInclusive) then false
    if a.Lower == b.Upper && !(a.LowerInclusive && b.UpperInclusive) then false
    else true
```

**New class method on `NumericInterval`:**

```
static NumericInterval FromConstraints(IReadOnlyList<FieldConstraint> constraints):
    lower = -∞, lowerInc = false
    upper = +∞, upperInc = false
    for each constraint:
        Nonnegative  → lower = max(lower, 0), lowerInc |= (lower == 0)
        Positive     → lower = max(lower, 0), lowerInc = false when lower == 0
        Min(V)       → lower = max(lower, V), lowerInc = true when lower == V
        Max(V)       → upper = min(upper, V), upperInc = true when upper == V
    return NumericInterval(lower, lowerInc, upper, upperInc)
```

**Severity: Error.** A proven violation means the runtime invariant will ALWAYS reject the operation — the assignment can never succeed. This is dead code (the transition row will always be rolled back). Principle #8: if the checker proves a contradiction, block it.

**Marker format:** No new markers. Uses existing `$ival:` markers from Layer 2 for expression intervals, plus the new `FromConstraints` method for field constraint intervals.

**False positive policy:** C94 fires ONLY when expression interval and constraint interval have zero overlap. `set Score = Score + 1` with `max 100` → `[1, 101]` ∩ `(-∞, 100]` = `[1, 100]` (non-empty) → no diagnostic. The runtime handles the `Score = 100` edge case.

**Nullable interaction:** For nullable fields with constraints, the constraint only applies to non-null values (constraints desugar with null guards: `Field == null or Field >= N`). C94 checks only apply when the RHS expression is provably non-null (no `Null` in `StaticValueKind`). If the expression might be null, no C94 check — the null case is valid by the `nullable` declaration.

**What triggers C94:**

| Expression | Constraint | Interval | Constraint interval | Diagnosis |
|---|---|---|---|---|
| `set Score = 200` | `max 100` | `[200, 200]` | `(-∞, 100]` | C94 ✓ |
| `set Rate = -1` | `nonnegative` | `[-1, -1]` | `[0, +∞)` | C94 ✓ |
| `set Rate = 0` | `positive` | `[0, 0]` | `(0, +∞)` | C94 ✓ |
| `set Count = 0` | `min 1` | `[0, 0]` | `[1, +∞)` | C94 ✓ |
| `set Score = max(Score, 101)` | `max 100` | `[101, +∞)` | `(-∞, 100]` | C94 ✓ |
| `set Rate = Score * -1` | `positive`, Score ∈ `[0, 100]` | `[-100, 0]` | `(0, +∞)` | C94 ✓ |

**What does NOT trigger C94:**

| Expression | Constraint | Interval | Constraint interval | Why safe |
|---|---|---|---|---|
| `set Score = Score + 1` | `max 100` | `[1, 101]` | `(-∞, 100]` | Overlap `[1, 100]` |
| `set Score = X` | `max 100`, X Unknown | `(-∞, +∞)` | `(-∞, 100]` | Unknown always overlaps |
| `set Score = Go.Amount` | `max 100`, Amount `min 1` | `[1, +∞)` | `(-∞, 100]` | Overlap `[1, 100]` |

**Diagnostic message:** `"Assignment to '{field}' provably violates the '{constraint}' constraint. Expression range [{lo}, {hi}] is entirely outside the required range [{cLo}, {cHi}]."`

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C94_SetLiteralExceedsMax_Error` | `set Score = 200` with `max 100` |
| `Check_C94_SetNegativeWithNonneg_Error` | `set Rate = -1` with `nonnegative` |
| `Check_C94_SetZeroWithPositive_Error` | `set Rate = 0` with `positive` |
| `Check_C94_SetBelowMin_Error` | `set Count = 0` with `min 1` |
| `Check_C94_SetCompoundExceedsMax_Error` | `set Score = max(Score, 101)` with `max 100` |
| `Check_C94_SetPartialOverlap_NoDiagnostic` | `set Score = Score + 1` with `max 100` → clean |
| `Check_C94_SetUnknown_NoDiagnostic` | `set Score = X + Y` (unknowns) → clean |
| `Check_C94_StateAction_Error` | `to Done -> set Rate = -1` with `positive` |
| `Check_C94_ComputedField_Error` | `field X as number -> -1 nonnegative` |
| `Check_C94_NullableField_NullRHS_NoDiagnostic` | `set NullableRate = null` with `positive` → clean |
| `Check_C94_EventArgRange_Error` | `set Score = Go.Amount * 1000` with Score `max 100`, Amount `min 1` |
| `Check_C94_CombinedMinMax_Error` | `set Score = 200` with `min 0 max 100` |

### C95: Contradictory Rule

**What:** A non-synthetic rule whose predicate is provably unsatisfiable given the field's declared constraints.

**Algorithm:**

1. For each rule, check if the rule's `isSynthetic` flag is false and the expression is a simple single-field comparison (`Field <op> Literal`).
2. Extract the **satisfying interval** — the range of field values that make the predicate true:

| Predicate | Satisfying interval |
|---|---|
| `Field > N` | `(N, +∞)` |
| `Field >= N` | `[N, +∞)` |
| `Field < N` | `(-∞, N)` |
| `Field <= N` | `(-∞, N]` |
| `Field == N` | `[N, N]` |
| `Field != N` | `(-∞, N) ∪ (N, +∞)` — not representable as single interval; skip |

3. Compute the field's constraint interval from declared constraints only (not from other rules — avoids circularity).
4. If `!Intersects(satisfyingInterval, constraintInterval)` → **C95 error**.

**Severity: Error.** A contradictory rule means no valid state can satisfy all rules simultaneously. Every mutation will be rejected. The precept is structurally broken.

**Example:** `field Score as number min 10` + `rule Score < 5 because "..."`. Constraint `[10, +∞)`, satisfying `(-∞, 5)`, no intersection → C95.

**Scope limitation:** Only simple single-field comparisons (`Field <op> Literal`). Cross-field rules (`rule A > B`) and complex expressions (`rule X + Y > 0`) are unanalyzed — no diagnostic. The `!= N` case is also skipped because the satisfying set is disjunctive and not representable as a single interval.

**Diagnostic message:** `"Rule '{expression}' contradicts the '{constraint}' constraint on field '{field}'. No valid value satisfies both — every operation will be rejected."`

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C95_MinVsLessThan_Error` | `min 10` + `rule X < 5` |
| `Check_C95_PositiveVsLteZero_Error` | `positive` + `rule X <= 0` |
| `Check_C95_MaxVsGreaterThan_Error` | `max 100` + `rule X > 100` |
| `Check_C95_MinMaxVsOutOfRange_Error` | `min 0 max 100` + `rule X > 200` |
| `Check_C95_CrossFieldRule_NoDiagnostic` | `rule A > B` → no diagnostic (cross-field) |
| `Check_C95_SyntheticRule_Excluded` | Synthetic rule from constraint → no C95 |

### C96: Vacuous Rule

**What:** A non-synthetic rule whose predicate is provably always true given the field's declared constraints.

**Algorithm:**

1. Same extraction as C95 — simple single-field comparison, satisfying interval, constraint interval.
2. Check containment: if `constraintInterval ⊆ satisfyingInterval` → **C96 warning**.

**Containment check:**

```
Contains(outer, inner):
    if inner.Lower < outer.Lower then false
    if inner.Upper > outer.Upper then false
    if inner.Lower == outer.Lower && inner.LowerInclusive && !outer.LowerInclusive then false
    if inner.Upper == outer.Upper && inner.UpperInclusive && !outer.UpperInclusive then false
    else true
```

`Contains(satisfyingInterval, constraintInterval)` → C96.

**Severity: Warning.** The rule isn't wrong — it's unnecessary. The constraint already guarantees the condition. May indicate a misunderstanding of the constraint system, or may be kept intentionally for documentation. Principle #8 conservatism: warn but don't block.

**Example:** `field Score as number min 0 max 100` + `rule Score >= 0 because "..."`. Constraint `[0, 100]`, satisfying `[0, +∞)`, `[0, 100] ⊆ [0, +∞)` → C96.

**Note:** Synthetic rules (generated by constraint desugaring) are excluded from C96 analysis. A `nonnegative` constraint generating `rule X >= 0` is not redundant with itself.

**Diagnostic message:** `"Rule '{expression}' is always satisfied by the '{constraint}' constraint on field '{field}'. Consider removing the redundant rule."`

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C96_NonnegVsGte0_Warning` | `nonnegative` + `rule X >= 0` |
| `Check_C96_MinVsGteLower_Warning` | `min 5` + `rule X >= 0` |
| `Check_C96_MaxVsLteHigher_Warning` | `max 100` + `rule X <= 200` |
| `Check_C96_SyntheticRule_Excluded` | Synthetic rule from constraint → no C96 |
| `Check_C96_NotVacuous_NoDiagnostic` | `min 0 max 100` + `rule X >= 50` → no C96 (constraint not ⊆ satisfying) |

### C97: Dead Guard

**What:** A `when` guard on a transition row, edit declaration, or ensure whose condition is provably always false given the proof state at the point of evaluation.

**Algorithm:**

1. At each `when` guard, extract the guard's satisfying interval for each field it references (same technique as C95 — simple single-field comparison extraction).
2. Look up the field's constraint interval from the current proof state (base proof + rules + state ensures, as narrowed by `ApplyNarrowing`).
3. If `!Intersects(guardSatisfyingInterval, fieldConstraintInterval)` for any field → **C97 warning**.

**Extended to relational guards:** When the proof state contains `$gt:A:B` (from a rule `A > B`) and the guard is `when A <= B`, the guard contradicts the relational fact → C97. This uses Layer 4 markers.

**Severity: Warning.** Dead guards create unreachable code — similar to existing C48 (unreachable state), which is also a warning. The code doesn't cause runtime failure; it's just unused. Principle #8: warn on proven dead code, don't error on it unless it prevents some operation from ever succeeding.

**Example:** `field Score as number max 100` + `when Score > 100 -> ...`. Guard satisfying `(100, +∞)`, constraint `(-∞, 100]`, no intersection → C97.

**Diagnostic message:** `"Guard 'when {expr}' is provably always false — this row/declaration is unreachable. Field '{field}' is constrained to [{lo}, {hi}] but the guard requires [{gLo}, {gHi}]."`

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C97_GuardExceedsMax_Warning` | `when Score > 100` with `max 100` |
| `Check_C97_GuardBelowMin_Warning` | `when Count < 0` with `nonnegative` |
| `Check_C97_GuardContradictRule_Warning` | `rule A > B` + `when A <= B` |
| `Check_C97_ComplexGuard_NoDiagnostic` | `when A + B > 0` → no diagnostic (complex) |
| `Check_C97_EditGuardDead_Warning` | `in State when X > 100 edit Field` with `max 100` |
| `Check_C97_EnsureGuardDead_Warning` | `in State ensure X > 0 when X < 0` with `nonnegative` |

### C98: Vacuous Guard

**What:** A `when` guard whose condition is provably always true given the proof state.

**Algorithm:**

1. Same extraction as C97 — guard satisfying interval, field constraint interval.
2. Check: `Contains(guardSatisfyingInterval, fieldConstraintInterval)` → **C98 warning**.

**Severity: Warning.** The guard adds no information — removing `when` would not change behavior. Not a bug, but may indicate unnecessary complexity.

**Example:** `field Rate as number positive` + `when Rate > 0 -> ...`. Guard satisfying `(0, +∞)`, constraint `(0, +∞)`, constraint ⊆ satisfying → C98.

**Diagnostic message:** `"Guard 'when {expr}' is provably always true — the condition has no effect. Field '{field}' already satisfies this via its '{constraint}' constraint."`

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C98_PositiveVsGt0_Warning` | `when Rate > 0` with `positive` |
| `Check_C98_NonnegVsGte0_Warning` | `when Score >= 0` with `nonnegative` |
| `Check_C98_MinVsGteLower_Warning` | `when Count >= 1` with `min 1` |
| `Check_C98_NotVacuous_NoDiagnostic` | `when Score > 50` with `min 0 max 100` → no C98 |

### Transition Reachability Sharpening

The proof engine sharpens three existing diagnostics by combining dead guard detection (C97) with existing state-graph analysis:

**C50 sharpening (dead-end state):** If all transition rows leaving a state have provably-dead guards (all C97), the state has no viable exits. C50 fires even though rows syntactically exist — they are all unreachable.

**C51 sharpening (always-rejecting):** If all non-reject rows for a `(State, Event)` pair have dead guards, only reject rows remain viable. C51 fires.

**C48 sharpening (unreachable state):** If all transition rows targeting a state have dead guards (on the source rows), no path can reach it. C48 fires even though transition rows exist.

These are NOT new diagnostics — they are sharper triggers for existing ones, using interval analysis instead of purely syntactic analysis.

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C50_Sharpened_AllGuardsDead_Warning` | State with rows but all guards are C97 |
| `Check_C51_Sharpened_NonRejectRowsDead_Warning` | Only dead-guard non-reject rows |
| `Check_C48_Sharpened_IncomingRowsDead_Warning` | All incoming transitions have dead guards |

### C99: Cross-Event Field Invariant Analysis (Opt-In)

**What:** Proves that a field maintains a constraint across ALL reachable states and transitions, enabling downstream code to rely on the invariant without per-expression proof.

**Example:** `field Score as number min 0 max 100` — if no transition row assigns a value outside `[0, 100]` (all assignments provably safe via C94 analysis), then Score's interval is `[0, 100]` in every reachable state. The proof engine can inject this as a tighter interval for downstream checks.

**Algorithm:**

1. Build the state graph (states as nodes, transition rows as edges).
2. For each field with numeric constraints, initialize the field's interval to the constraint interval.
3. For each transition row, compute the post-assignment interval for the field using `TryInferInterval` on the RHS expression (if assigned) or carry the incoming interval (if not assigned).
4. At each state, compute the join (Hull) of all incoming edge intervals.
5. Iterate until fixed point (field intervals stop changing).
6. If the fixed-point interval for a field at a state is tighter than `Unknown`, inject it as a proof marker for that state's context.

**Complexity:** O(|states| × |fields| × |transitions|) per iteration. Terminates because field constraint intervals provide finite bounds — intervals cannot grow beyond the constraint, and Hull is monotone (only widens). The number of iterations is bounded by the number of distinct interval values, which is finite given fixed constraint bounds.

**Gating:** This analysis is **opt-in only**. It does not run by default. It is the only enforcement in this design that requires fixed-point iteration, breaking the single-pass guarantee. For small precepts (5–10 states), it is fast. For large precepts with complex state graphs, it could add noticeable compile time.

**Severity: Info.** Informational diagnostic — tells the author a proven invariant holds. Does not block compilation.

**Diagnostic message:** `"Field '{field}' maintains the invariant [{lo}, {hi}] across all reachable states."`

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C99_FieldInvariant_AllPathsSafe_Info` | Score always in [0, 100] across all transitions |
| `Check_C99_FieldInvariant_OnePathViolates_NoC99` | One transition can violate → no C99 |
| `Check_C99_OptIn_DefaultOff` | Analysis doesn't run by default |

### Collection, String, Boolean, and Choice Reasoning

**Collection count intervals:** `mincount N` and `maxcount N` define integer intervals for `.count`. After `add`, count ∈ `[prev.count, prev.count + 1]` (sets may not increase for duplicates); after `clear`, count = 0; after `enqueue`/`push`, count = prev.count + 1; after `dequeue`/`pop`, count = prev.count - 1.

**Design decision: NOT enforced.** Collection mutations are data-dependent (add a value from an event arg). Count evolution depends on runtime membership, which the proof engine cannot track without element-level set analysis. The runtime invariant system enforces `mincount`/`maxcount` after each mutation. The cost of element-membership tracking is disproportionate.

**String length intervals:** `minlength N` and `maxlength N` define integer intervals for `.length`. String functions with bounded output exist (`left(s, N)` → length ≤ N), but the dominant pattern is literal assignment or event-arg assignment where length depends on runtime input.

**Design decision: NOT enforced.** String operations in Precept are limited, and the dominant patterns involve runtime-dependent string values. Literal string assignments are already checked at parse time (C59 for defaults). No new enforcement adds meaningful value.

**Boolean reasoning:** Already tracked through the narrowing system as `{true, false}` / `{true}` / `{false}`. Guards referencing boolean fields refine correctly: `when IsPremium` → `IsPremium = true`. No new proof engine work needed.

**Choice/enum reasoning:** C68 already checks literal assignment to choice fields at compile time. Choices are discrete sets, not intervals — the proof engine's interval infrastructure does not add value. No new enforcement needed.

---

## Enforcement Soundness Guarantees

The proof engine's no-false-positive guarantee extends to all new enforcements:

### What the engine proves (complete list)

**Existing (PR #108):**
- **C92:** Divisor is literal zero — proven contradiction.
- **C93:** Divisor has no compile-time nonzero proof — unproven identity/compound expression.
- **C76:** Sqrt argument has no compile-time non-negative proof.
- **Sequential flow:** Post-mutation proof state correctly reflects assignment effects.
- **Relational facts:** `A - B` provably nonzero when strict inequality `A > B` is proven.
- **Conditional results:** Provably nonzero when Hull of both branch intervals excludes zero.

**New (follow-up):**
- **C94:** Assignment expression interval provably outside field constraint interval. The `Intersects` predicate is sound — it returns false only when there is provably zero overlap between the two intervals.
- **C95:** Rule predicate satisfying interval provably disjoint from field constraint interval. Uses the same `Intersects` predicate.
- **C96:** Field constraint interval provably contained within rule satisfying interval. The `Contains` predicate is sound — it returns true only when every value in the inner interval is also in the outer interval.
- **C97/C98:** Guard satisfying interval provably disjoint from / contains the field constraint interval. Same predicates as C95/C96 applied to guard expressions.
- **C99:** Cross-event field interval provably bounded after fixed-point convergence. Sound because Hull is an over-approximation (only widens) and iteration terminates at a fixed point.

### What the engine conservatively rejects (updated)

All items from § Soundness Guarantees above remain unchanged, plus:

- **Partial-overlap constraint violations.** `set Score = Score + Amount` where the result interval partially exceeds `max 100` is not flagged. The runtime handles it.
- **Cross-field rule contradictions.** `rule A > B` with constrained A and B is not analyzed for satisfiability.
- **Complex rule/guard expressions.** `rule X + Y > 0` or `when A * B > 5` are not analyzed — only simple single-field comparisons.
- **`!= N` predicates in rules.** The satisfying set `(-∞, N) ∪ (N, +∞)` is disjunctive and not representable as a single interval. Skipped.

---

## New Design Decisions

### 5. Proven-Violation-Only Policy for Constraint Enforcement

**Decision:** C94 fires only when expression interval and constraint interval have NO overlap. "Possible violation" (partial overlap) produces no diagnostic.

**Alternative rejected:** Warning on possible violations (expression range extends beyond constraint).

**Rationale:** Precept's runtime invariant system is designed to catch constraint violations at execution time. Flagging every assignment that MIGHT produce an out-of-range value would flood authors with warnings on correct code. An assignment like `set Score = Score + Amount` with `max 100` INTENTIONALLY relies on the runtime constraint to reject the `Score = 100, Amount > 0` case. Warning about the intended design of the constraint system would be noise.

**Precedent:** Rust's const-evaluation in match exhaustiveness only flags provably exhaustive/unreachable patterns, not "might be" patterns. TypeScript's narrowing errors on provable contradictions, not possible ones.

**Tradeoff accepted:** `set Score = Score + 100` (which exceeds max when Score > 0) is not flagged because `[100, +∞)` ∩ `(-∞, 100]` = `{100}` (non-empty, single point). The runtime catches the actual violation. The engine only catches the case where NO value in the expression range could satisfy the constraint.

### 6. Simple Single-Field Scope for Dead Rule/Guard Analysis

**Decision:** C95/C96/C97/C98 only analyze simple single-field comparisons (`Field <op> Literal`). Cross-field and complex expressions are unanalyzed.

**Alternative rejected:** Full constraint satisfaction for arbitrary boolean expressions.

**Rationale:** General constraint satisfaction is disproportionate to the benefit. Simple single-field comparisons cover the dominant patterns (field constraints vs rules/guards that reference the same field with a literal bound). Cross-field analysis would require relational intervals or an SMT solver — both violate the design's non-SMT, single-pass architecture. (Principle #8: the checker doesn't guess.)

**Tradeoff accepted:** `rule A + B < 5` with `A min 3` and `B min 3` is not flagged even though it's contradictory (`A + B >= 6 > 5`). The analysis doesn't compose arithmetic intervals across rule predicates.

### 7. Cross-Event Invariant Analysis is Opt-In

**Decision:** C99 requires explicit opt-in. Does not run by default.

**Alternative rejected:** Always-on cross-event analysis.

**Rationale:** Cross-event analysis is the only enforcement requiring fixed-point iteration, breaking the single-pass guarantee. Performance impact is proportional to state-graph complexity. The fast-by-default experience is a product commitment — opt-in preserves it.

**Tradeoff accepted:** Authors don't get cross-event invariant information unless they explicitly enable it. Most won't need it.

### 8. Error vs Warning Severity Split

**Decision:** Proven violations that make code structurally dead → Error. Proven redundancies or unnecessary constructs → Warning.

| Diagnostic | Condition | Severity | Rationale |
|---|---|---|---|
| C94 | Assignment always violates constraint | Error | Runtime will always reject — dead code |
| C95 | Rule always unsatisfiable | Error | Global integrity failure — nothing works |
| C96 | Rule always true | Warning | Not harmful, just unnecessary |
| C97 | Guard always false | Warning | Unreachable code, not harmful |
| C98 | Guard always true | Warning | Unnecessary condition, not harmful |
| C99 | Cross-event invariant holds | Info | Informational, not a problem |

**Rationale:** Errors block compilation (Principle #8: prevention). Warnings inform the author. The line is drawn at "will this code ever succeed?" — if the answer is provably no, it's an error. If it's "this code is redundant but not broken," it's a warning.

### 9. No New DSL Constructs Required

**Decision:** All enforcements apply to constructs that EXIST in the DSL today. No new keywords, modifiers, or syntax forms are needed.

**Observation:** The constraint surface (`nonnegative`, `positive`, `min N`, `max N`, `notempty`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered`) is comprehensive for the current type system. Two gaps noted:

- **`nonzero` modifier:** Would enable `$nonzero:` as a first-class constraint, improving interval precision for divisor safety. Already filed as issue #111. Not required for any enforcement in this design.
- **`length` constraint on strings (integer bound on `.length`):** Would enable C94 for string assignments. Does not exist today. PROPOSAL NOTE: if string-length enforcement is wanted, a `length` constraint keyword would be needed. This is a separate language proposal, not part of this design.

---

## Phasing

### PR #108 — Current Scope (Unchanged)

PR #108 ships the proof engine infrastructure: Layers 1–5, C93 compound-expression enforcement, C76 interval-based sqrt safety, and the `NumericInterval` foundation. Slices 11–15 as designed in `temp/proof-stack-implementation-plan.md`.

**No new enforcement diagnostics land on PR #108.**

**Rationale:** PR #108 is already +340 new lines, +56 tests. The proof stack infrastructure must be correct and well-tested before building enforcement on top of it. Shipping infrastructure and enforcement separately enables clean bisection if regressions occur, and each follow-up issue has a focused, reviewable scope.

### Follow-up A: Assignment Constraint Enforcement (C94)

**Title:** Compile-time assignment constraint enforcement via interval analysis
**Scope:** C94 diagnostic, `NumericInterval.Intersects`, `NumericInterval.FromConstraints`, constraint interval checks at all assignment sites (transition rows, state actions, computed fields), ~12 tests.
**Size:** ~80 new lines + ~15 changed + 12 tests.
**Depends on:** PR #108 (interval infrastructure).
**Rationale:** Natural first consumer of the interval infrastructure. High value — catches provably-dead assignments that currently pass silently to runtime rejection.

### Follow-up B: Dead Rule Detection (C95, C96)

**Title:** Compile-time dead rule detection — contradictory and vacuous rules
**Scope:** C95/C96 diagnostics, satisfying interval extraction from rule predicates, comparison against field constraint intervals, synthetic-rule exclusion, ~8 tests.
**Size:** ~60 new lines + ~10 changed + 8 tests.
**Depends on:** Follow-up A (constraint interval extraction pattern).
**Rationale:** Catches structural precept errors (C95) and unnecessary rules (C96). Low implementation cost — reuses interval comparison from C94.

### Follow-up C: Dead Guard Detection (C97, C98) + Transition Sharpening

**Title:** Compile-time dead/vacuous guard detection and transition reachability sharpening
**Scope:** C97/C98 diagnostics, guard interval analysis, sharpened C48/C50/C51 triggers, ~12 tests.
**Size:** ~80 new lines + ~20 changed + 12 tests.
**Depends on:** Follow-up A and B (interval extraction patterns).
**Rationale:** Extends dead-code detection to transition routing. High value for complex precepts where guard interactions with constraints create unreachable rows.

### Follow-up D: State Reachability Sharpening

**Title:** Proof-engine-aware state reachability analysis
**Scope:** Integrate C97 results into existing C48 analysis, ~3 tests.
**Size:** ~20 new lines + ~10 changed + 3 tests.
**Depends on:** Follow-up C (C97 infrastructure).
**Rationale:** Small, focused extension. Leverages C97 to sharpen the existing state-graph analysis.

### Follow-up E: Cross-Event Field Invariant Analysis (C99)

**Title:** Opt-in cross-event field invariant analysis via state-graph fixed-point
**Scope:** C99 diagnostic, state-graph iteration, opt-in gating, ~6 tests.
**Size:** ~150 new lines + ~10 changed + 6 tests.
**Depends on:** PR #108 (interval infrastructure). Independent of A–D.
**Rationale:** The only enforcement requiring fixed-point iteration. Separate research and design review recommended before implementation. Highest cost, lowest urgency.
