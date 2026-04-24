# Proof Engine Design

> **Authority boundary:** This file lives in `docs/`, the repository's legacy/current reference set. Use it for the implemented v1 surface, current product reference, or historical context. If you are designing or implementing `src/Precept.Next` / the v2 clean-room pipeline, start in [docs.next/README.md](../docs.next/README.md) instead.

Date: 2026-04-18

Status: **Implemented** — Core engine and all four implementation waves shipped in PR #108. Coefficient guard soundness fix, ProofResult with real ProofAttribution, natural-language interval formatting, hover expansion (field declarations and rule/when keywords), assessment-driven code action structured metadata, MCP structured proof output, and 22 MCP proof tests all landed. Deferred items: full expression-level hover for compound sub-expressions, per-event proof scoping (Events DTO field always null — schema ready), and ensure keyword hover.

> **C99 (cross-event field invariant analysis)** is out of scope for this document. It requires fixed-point iteration, breaking the single-pass guarantee, and is tracked separately as issue #117.

---

## Overview

The proof engine is Precept's compile-time reasoning layer. It infers numeric intervals and relational facts from the `.precept` definition — field constraints, rules, guards, ensures, and assignments — and uses them to prove or disprove properties of every numeric expression before any entity instance exists. This is the mechanism that delivers the philosophy's prevention commitment for numeric integrity.

The engine's design is grounded in three philosophy commitments:

1. **Prevention before instantiation.** The proof engine fires at compile time, before any entity instance exists. Divisor-safety, sqrt-operand safety, and assignment-constraint violations are compile-time errors — the invalid configuration never reaches runtime. This is the direct realization of the prevention commitment: invalid configurations are structurally impossible, not caught at runtime.

2. **Full inspectability.** Proof results are not private compiler state. Every proven range and its source attribution — the field constraints, rules, and guards that contributed — surfaces through hover displays, diagnostic messages, and MCP structured proof output. The author sees what the engine proved, what it could not prove, and why. Inspectability is architectural, not optional.

3. **Determinism.** Same definition produces the same proof outcome. The proof engine uses interval arithmetic and bounded relational closure — no non-deterministic solvers, no timing-dependent analysis, no stochastic reasoning. When the engine cannot prove safety, it says so explicitly. The author is never confronted with an unexplainable verdict.

The engine serves three purposes:

1. **Safety enforcement.** Prove that divisors are nonzero (C92/C93), sqrt operands are non-negative (C76), and assignments satisfy field constraints (C94). Interval transfer rules propagate proof information through all 10 built-in numeric functions (`abs`, `min`, `max`, `clamp`, `sqrt`, `floor`, `ceil`, `round`, `truncate`, `pow`) so that proof-backed diagnostics fire correctly when these functions appear in expressions. These are compile-time errors or warnings — the invalid configuration never reaches runtime.
2. **Structural integrity analysis.** Detect contradictory rules (C95), vacuous rules (C96), dead guards (C97), and vacuous guards (C98). These are authoring-quality diagnostics — they surface definition problems that would silently degrade the entity's behavior.
3. **Author-facing inspectability.** Surface proven ranges and source attribution through hover displays, diagnostic messages, and MCP proof output — in natural language, never compiler internals. The author sees what the engine proved, what it could not prove, and why.

All proof-backed diagnostics route through a shared assessment model that classifies outcomes by proof result (contradiction, unresolved obligation, proven safe), not by syntax shape. This ensures consistent behavior across diagnostics, hover, and MCP output.

### Architecture Summary

The engine is organized as three composing types with a single integration query:

- **`ProofContext`** — the typed proof state container. Holds field intervals, relational facts, expression facts, and sign flags in structured, typed stores. Scoped: `GlobalProofContext` (immutable, definition-wide) and `EventProofContext` (per-event child, structurally isolated).
- **`LinearForm`** — canonical normalizer for linear expressions with `Rational` coefficients. Keys the relational fact store. Reduces compound expressions like `Total - Tax - Fee` to a single canonical form for lookup.
- **`RelationalGraph`** — bounded BFS transitive closure over relational facts. Derives ordering relationships not directly stored (e.g., `A > C` from `rule A > B` and `rule B > C`). Hard-capped: 64 facts, depth 4, 256 visited nodes.

The composing query `ProofContext.IntervalOf(expr)` is the single integration point for all proof consumers. It returns a `ProofResult` containing a `NumericInterval` (the proven range) and a `ProofAttribution` (the source rules and constraints that contributed to the proof). Every safety check, integrity analysis, and inspectability surface calls `IntervalOf`.

### Properties

- **Sound.** The engine never claims an expression is safe when it is not. Every code path returns a provably correct interval or conservatively widens toward `Unknown`.
- **Incomplete.** The engine may reject expressions it cannot prove safe. This is the correct tradeoff for a DSL compiler — false negatives (missed proofs) cause author friction; false positives (wrong "safe" claims) cause runtime crashes.
- **Single-pass.** No fixpoint computation, no widening, no solver. This is possible because of Precept's flat execution model: no loops, no control-flow branches, no reconverging flow.
- **Deterministic.** Same definition produces the same proof outcome. No non-deterministic solvers, no timing-dependent analysis.
- **Bounded.** All traversals and closures have hard caps. Worst-case cost is predictable and proportional to definition size, not expression complexity.

---

## Philosophy-Rooted Design Principles

The following principles govern the proof engine's design, implementation, and evolution. They are grounded in Precept's core philosophy — prevention, one-file completeness, inspectability, determinism, and compile-time structural checking — and were confirmed through team design review of the unified proof engine architecture. They are not aspirational; they are constraints that every proof-engine decision must satisfy.

1. **Prevention before instantiation.** The proof engine proves safety at compile time, before any entity instance exists. This is the direct realization of the philosophy's prevention commitment: invalid configurations are structurally impossible, not caught at runtime. A proof that fires only when an instance is constructed is detection; a proof that fires when the definition is compiled is prevention.

2. **The flat execution model is the load-bearing insight.** Precept has no loops, no control-flow branches, and no reconverging flow. This makes interval arithmetic *optimal*, not merely sufficient — it eliminates fixpoint computation, widening operators, and lattice joins by construction. The absence of widening is a feature: in general-purpose analyzers, widening is the primary source of precision loss. The engine's tractability is not an engineering tradeoff; it is a structural consequence of the DSL's execution model.

3. **Soundness over completeness.** Every code path returns a provably correct interval or `Unknown`. No path fabricates a tighter interval than the evidence justifies. False negatives (missed proofs) cause author friction — the author must supply additional constraints. False positives (wrong "safe" claims) cause runtime crashes. The engine always chooses the safe direction: widen toward `Unknown`, never narrow without proof.

4. **One file, complete proof facts.** All proof facts — field constraints, rules, guards, ensures, assignments — derive from the `.precept` definition. No external oracle, no hidden configuration, no side channel. The proof engine's knowledge boundary is the file boundary. This is a direct consequence of the one-file-complete-rules philosophy commitment.

5. **Deterministic proof outcomes.** Same definition produces the same proof result. No non-deterministic solvers, no timing-dependent analysis, no stochastic reasoning. This is why SMT solvers are rejected even when they could prove more: a solver that returns different results on different runs, or whose proof witness is opaque, violates both determinism and inspectability.

6. **Inspectability is architectural, not optional.** The proof engine's reasoning must be visible through hover displays, MCP tools, and structured proof dumps. Inspectability ships *with* the engine, not after it — prevention without inspectability improves safety but does not cash the philosophy promise that "nothing is hidden." The author must be able to see what the engine proved, what it could not prove, and why.

7. **Natural language, not compiler internals.** Users see proven ranges and source attribution ("1 to 100, inclusive — from: rule Rate >= 1, rule Rate <= 100"), not proof mechanics (`LinearForm`, `RelationalGraph`, interval notation). This is enforced by architecture: the data-flow pipeline from proof assessment to user-facing display has no path where internal type names can surface. Convention-based hiding is fragile; architectural separation is durable.

8. **Truth-based diagnostic classification.** The proof engine classifies outcomes into three categories: *proved dangerous* (the compiler can demonstrate a violation), *proved safe* (the compiler can demonstrate correctness), and *unresolved* (the compiler cannot determine either). These categories map to distinct author actions: fix a proven violation, rely on proven safety, or supply additional constraints to help the compiler. Syntax-shape pattern matching is replaced by proof-outcome classification through a shared assessment model.

9. **Proven violations only.** The engine reports what is definitively broken, not what might be broken. Flagging possible violations turns the compiler into a nag that trains authors to ignore warnings. Flagging only proven violations makes it a trusted guide — when it speaks, it is right. This is the difference between a compiler that helps and one that erodes trust.

10. **Opaque solvers are rejected on principle.** SMT/Z3 is excluded because opaque proof witnesses violate the inspectability commitment. The constraint surface handled by the proof engine is decidable with interval arithmetic and bounded relational closure alone. When the engine cannot prove safety, it says so explicitly — the author is never confronted with an unexplainable verdict. Proof legibility extends to AI agents: proof witnesses must be structured data, not opaque solver traces.

11. **Proof flows as structured data, not parsed prose.** The shared assessment model is the contract center for all consumers: diagnostics, hover, MCP tools, and AI agents. Diagnostic message text is a rendering of the structured assessment, never a contract. Tooling integrations consume the assessment model directly — message parsing is explicitly rejected as an integration mechanism. This is how inspectability becomes real in the editor and in agent tooling.

12. **Design claims are locked by tests.** Every proof-engine guarantee must have corresponding test coverage. Boundary conditions — fact-store limits, traversal depths, visited-node caps — are correctness boundaries, not implementation trivia. Assessment-model coverage is distinct from scenario coverage: the model's classification logic must be tested directly, not only through scenarios that happen to pass through it. A design claim without a test is not a guarantee.

13. **General integrity reasoning, not special-case patterns.** The proof engine reasons about numeric integrity across the entire definition — field constraints, rules, assignments, expressions — not as a collection of special-case checkers for specific syntactic patterns. Broadening from divisor safety to general integrity reasoning transforms the engine from a targeted checker into a general authoring aid. Every new proof capability should follow this principle: prove properties of the domain model, not properties of a syntax shape.

---

## Research Foundations

The ProofContext + LinearForm + RelationalGraph architecture sits on well-established formal ground.

**Academic grounding.** This design is a simplified **Zone abstract domain** in the Cousot & Cousot (1977) abstract interpretation framework. Specifically: an interval domain combined with a bounded relational graph in a reduced product — nearly identical to Miné's zone/octagon domains (2001/2006). The bounded BFS transitive closure is a simplified **difference-bound matrix (DBM)** without the full Floyd-Warshall closure. Barrett & Tinelli (2018) SMT surveys confirm that **QF_LRA** (quantifier-free linear real arithmetic) is the closest formal fragment; this approach is a sound under-approximation of that fragment.

**Reference implementations.** Three reference implementations inform the build:

- **CodeContracts / Clousot** (MIT, archived, github.com/microsoft/CodeContracts) — contains a "Pentagons" domain that is the architectural blueprint: interval domain + lightweight relational domain (upper bounds) in a reduced product. This is exactly the architecture chosen here.
- **Boogie IntervalDomain.cs** (MIT, active, github.com/boogie-org/boogie) — 1,730 lines of production C# interval domain code. Uses `BigInteger?` for bounds. Best living C# reference for interval domain implementation.
- **Crab / IKOS / Apron** — C++ algorithm references for zone domain and DBM closure.

**SMT/Z3 evaluation.** Z3/CVC5 was unanimously rejected during design review for PR #108. The reasons: 50–65 MB of native dependencies, non-deterministic solving times, opaque proof witnesses that violate Principle #12 (AI legibility), and API-surface complexity — all for a constraint surface that is decidable with interval arithmetic alone. Reaffirmed at unified engine design review.

**Library survey conclusion.** No reusable .NET library exists for interval arithmetic, LinearForm normalization, or DBM/zone domains. The three new types (`Rational`, `LinearForm`, `RelationalGraph`) are built from scratch, informed by the references above.

---

## Execution Model Assumptions

The proof engine's tractability rests on three structural properties of Precept's execution model that eliminate the complexity found in general-purpose static analysis:

1. **No loops.** Precept has no iteration constructs. Expression trees are finite and acyclic. A recursive descent over any expression terminates in bounded time proportional to tree depth. There is no need for fixpoint computation or widening operators.

2. **No control-flow branches.** A transition row is a flat sequence: evaluate a guard, execute assignments left-to-right, check rules and ensures. There are no `if` statements that split execution into paths that later reconverge. Conditional *expressions* (`if/then/else`) produce a single value — both branches are type-checked, exactly one is evaluated — but they do not create control-flow divergence.

3. **No reconverging flow.** Because there are no loops or branches, there is no join point where two different proof states must be merged. Each assignment in a row sees the proof state left by all preceding assignments. This makes sequential flow analysis trivial — it is a linear walk, not a dataflow graph.

These properties mean the proof engine is a **recursive descent over finite expression trees with linear sequential context**, not an abstract interpretation framework. Standard interval arithmetic transfer rules are directly applicable without the lattice infrastructure (widen, narrow, join, meet) that general-purpose analyzers require.

---

## Architecture

The proof engine is organized as three composing types — `ProofContext`, `LinearForm`, and `RelationalGraph` — with a single composing query `ProofContext.IntervalOf(expr)` as the integration point. All five consultation sites in the type checker that previously called separate methods (`TryInferInterval`, `TryInferRelationalNonzero`, `ExtractIntervalFromMarkers`, `InferSubtractionInterval`, the C-Nano special case) now call `IntervalOf`. The internal dispatch handles interval arithmetic, relational lookup, forward-inferred facts, and transitive closure in one place.

### ProofContext

`ProofContext` is the typed proof state container. It replaces the flat `IReadOnlyDictionary<string, StaticValueKind>` + six string-encoded marker conventions with structured, typed storage.

**Fact stores:**

| Store | Key | Value | Scope |
|---|---|---|---|
| `_fieldIntervals` | `FieldKey` | `NumericInterval` | Field-constraint → global; guard/ensure/assignment-derived → event-local |
| `_relationalFacts` | `LinearForm` (LHS - RHS) | `RelationalFact` (`>` or `>=`, scope) | Rule-derived → global; guard-derived → event-local |
| `_exprFacts` | `LinearForm` | `NumericInterval` | Always event-local |
| `_flags` | `FieldKey` | `NumericFlags` | Same as source fact |

**Primary query methods:**

- `IntervalOf(PreceptExpression expr)` — composing query. Returns a `ProofResult` containing a `NumericInterval` and a `ProofAttribution` (source rules/constraints that contributed to the interval). Calls interval arithmetic, relational lookup, and transitive closure in one pass. This is the single integration point for all C92, C93, C76, C94, C97, and C98 checks.

**`IntervalOf` query dispatch order.** The composing query runs two phases and intersects their results:

1. **Interval arithmetic** (`TryInferInterval`). Recursive descent over the expression tree. Literal nodes produce singleton intervals. Identifier leaves are resolved by: (a) `_fieldIntervals` direct lookup, (b) `_flags` projection (`Positive` → `(0,+∞)`, `Nonneg` → `[0,+∞)`, etc.). Binary and unary nodes apply standard transfer rules. Function calls dispatch to `Abs`, `Min`, `Max`, `Clamp`, `Sqrt`, `Floor`, `Ceil`, `Round`, `Truncate`, and `Pow` (see function transfer rules below). Conditional `if/then/else` applies `Hull` over both branches with guard narrowing (see § Conditional Composition). This phase produces the **arithmetic interval**.
2. **Relational refinement** (`LookupRelationalInterval`). Attempted only when `LinearForm.TryNormalize(expr)` succeeds. If the normalized form has empty terms (pure constant, e.g. `A - A`), returns an exact singleton directly — no relational lookup needed. Otherwise, runs the 5-tier cascade (see § Relational Fact Store and 5-Tier Lookup). `_exprFacts` are consulted during this phase for expression-level facts stored by `WithAssignment`. This phase produces the **relational interval**.

The final result is `Intersect(arithmetic, relational)` — the tightest interval supported by both sources. When either phase returns `Unknown`, the intersection is the other phase's result. When both return `Unknown`, the final result is `Unknown`.
- `SignOf(PreceptExpression expr)` — returns `Positive / Nonneg / Nonzero / Unknown` derived from `IntervalOf`.
- `KnowsNonzero(PreceptExpression expr)` — `IntervalOf(...).Interval.ExcludesZero`.
- `KnowsNonnegative(PreceptExpression expr)` — `IntervalOf(...).Interval.IsNonnegative`.

### ProofResult and ProofAttribution

`IntervalOf` returns a `ProofResult` — a pair of `(NumericInterval Interval, ProofAttribution Attribution)`. The attribution tracks which rules, field constraints, and guards contributed to deriving the interval, enabling hover "from:" lines and MCP source references without a separate reconstruction pass.

```csharp
internal readonly record struct ProofResult(
    NumericInterval Interval,
    ProofAttribution Attribution);

internal sealed class ProofAttribution
{
    public IReadOnlyList<string> Sources { get; }  // e.g. "rule Rate >= 1", "field constraint positive"
    public static ProofAttribution None { get; }   // no attribution (Unknown interval)
    public ProofAttribution Merge(ProofAttribution other);  // combine sources from intersected intervals
}
```

**Design decision (D2, team review 2026-04-18): Eager tracking.** Attribution is collected during `IntervalOf` traversal, not reconstructed lazily at hover time. Every `With*` mutation records its source. `NumericInterval` arithmetic (`Add`, `Subtract`, `Intersect`, etc.) merges attributions from both operands. This adds ~1 field per method signature but gives all consumers — hover, diagnostics, MCP — access to attribution with zero additional cost.

**Tradeoff accepted:** Every `IntervalOf` call pays the attribution collection cost, even when no consumer reads it. The cost is proportional to the number of contributing facts (typically 1–3), not the expression tree size. Acceptable for a DSL compiler.

**Multi-source display format:** When multiple sources contribute to a single interval, the "from:" line renders them as a comma-separated list, each source as its DSL declaration text: `from: rule Rate >= 1, rule Rate <= 100`. Field constraints render as `field constraint {kind}` (e.g., `field constraint positive`). If the source list exceeds 5 entries, truncate to the first 4 and append "and N more" (e.g., `from: rule A >= 1, rule A <= 100, rule A != 50, rule A != 75, and 3 more`). This format is shared by hover, diagnostics, and MCP display.

**Mutation methods:**

- `WithAssignment(FieldKey target, PreceptExpression rhs)` — closes Gap 3. Calls `IntervalOf(rhs)` and stores the result as `_fieldIntervals[target]`. When `rhs` is normalizable, also stores an equality fact in `_exprFacts` (`LinearForm(target) - LinearForm(rhs) ∈ [0,0]`). Includes the reassignment kill loop: when `target` is assigned, every `_relationalFacts` entry whose `LinearForm` contains `target`, every `_exprFacts` entry whose `LinearForm` contains `target`, and every `_flags` entry on `target` is cleared atomically before the new fact is stored.
- `WithGuard(PreceptExpression condition, bool branch)` — calls `TryApplyNumericComparisonNarrowing` to inject interval and relational narrowings from a guard condition.
- `WithRule(PreceptExpression lhs, RelationKind rel, PreceptExpression rhs)` — stores a `RelationalFact` keyed by `LinearForm(lhs) - LinearForm(rhs)` when both sides are normalizable. Closes Gap 2 at injection time.
- `Child()` — returns an `EventProofContext` parented to this context (see § Proof State Lifecycle and Scope).
- `Dump()` — structured snapshot of all fact stores, consumed by hover, MCP `precept_compile` output, and debug display.

**Sequential flow (Gap 1 close).** `WithAssignment` is called after each `set` assignment in `ValidateTransitionRows()` and `ValidateStateActions()`, threading the updated proof state through the row. Each subsequent assignment sees post-mutation state — `set Rate = 0` kills `$positive:Rate` before a subsequent `set X = Amount / Rate` in the same row sees the Rate interval.

### Rational Type

`readonly record struct Rational(long Numerator, long Denominator) : INumber<Rational>, ISignedNumber<Rational>`

The `Rational` type provides exact arithmetic for `LinearForm` coefficients. It is a .NET 10 native type (~230 LOC, zero external dependencies). `ISignedNumber<Rational>` provides `NegativeOne`, used in unary-minus normalization and display logic.

**Why not `double`.** `double` coefficients risk FP-induced false positives in normalization: `(A/10)*3` and `3*A/10` may produce different bit patterns, causing them to hash to different `LinearForm` keys even though they represent the same expression. Any such divergence is a fabricated false "no match" in the relational store — sound (no false proof), but erodes proving power unpredictably.

**Why not `BigInteger` or NuGet.** `BigInteger`-backed rational (~150 LOC) was considered. tompazourek/Rationals (MIT, 1M+ downloads) targets .NET Standard 1.3 / .NET 6 — works on .NET 10 but not native, no `INumber<T>` generic math. c-ohle/RationalNumerics: 52 stars, single contributor, stale — overkill for the use case. **All coefficients originate from source literals (parser-bounded)**, so `long/long` representation is sufficient. Parser produces `long` for integers and `double` for decimals; `double`→`Rational` conversion is exact for finite decimals via `decimal` intermediary.

**Construction.** GCD normalization at construction ensures canonical form for dictionary key equality (`3/6` and `1/2` produce the same `Rational`). Zero denominator throws at construction.

**Arithmetic safety.** Uses `checked` long arithmetic. `TryNormalize` catches `OverflowException` and returns `null` — the caller falls back to interval arithmetic, which is always sound. Cross-GCD pre-reduction before multiplication (`gcd(a.Num, b.Den)`, `gcd(b.Num, a.Den)`, reduce, then multiply) prevents overflow in common cases. `CompareTo` uses `Int128`-based cross-multiplication to avoid overflow when comparing fractions with large numerators.

**Decimal literal conversion.** `Rational.FromDecimalLiteral("0.1")` → `Rational(1, 10)`. Round-trip: `(double)Rational(1, 10)` = `0.1d`. Exact by construction.

### LinearForm Normalization

`LinearForm` is the canonical form for sums of field references with `Rational` coefficients. It is a `sealed class` (not a record) with content-based `IEquatable<LinearForm>` — `ImmutableSortedDictionary` has reference equality, so record-generated equality would be incorrect. It is a parallel form to the AST — the parser AST is never mutated.

**Grammar.** A `LinearForm` is:
```
Terms: ImmutableSortedDictionary<string, Rational>   (field name, possibly dotted → coefficient)
Constant: Rational                                    (additive constant)
```

Zero-coefficient terms are dropped at construction. `A - A` produces empty terms and constant `0`. The string sort order is deterministic, ensuring canonical form for dictionary key equality and `GetHashCode` consistency.

**`TryNormalize(PreceptExpression expr) → LinearForm?`** Recursive depth-bounded normalization:

| Expression node | Normalization |
|---|---|
| Integer / decimal literal | `LinearForm` with empty terms, constant = literal value |
| Field reference | Single-term `LinearForm`: `{field → 1}`, constant = 0 |
| Parenthesized | Recurse (does not consume depth budget) |
| Unary `-` | `LinearForm.Negate(recurse(operand))` |
| Binary `+` | `LinearForm.Add(left, right)` |
| Binary `-` | `LinearForm.Subtract(left, right)` |
| `field * constant` or `constant * field` | `LinearForm.ScaleByConstant(field, constant)` |
| `field / constant` | `LinearForm.ScaleByConstant(field, Rational(1, constant))` |
| Function calls, `*` of two non-constants, etc. | `null` (non-normalizable) |

**Depth bound.** `TryNormalize` tracks a recursion depth counter initialized to `8`. Binary `+`, `-`, unary `-`, and function calls each decrement the counter. Parenthesized expression unwrapping does not consume depth budget — only operations do. On counter reaching zero, returns `null`. This guarantees termination and bounds per-expression work.

**Scalar-multiple normalization.** Before any relational lookup, `IntervalOf` GCD-normalizes the queried `LinearForm`: all coefficients are divided by their GCD. This means `+3·A + (-3)·B` reduces to `+1·A + (-1)·B` before matching against stored facts. `Y / (k*A - k*B)` with `rule A > B` therefore proves directly without requiring the author to factor the constant.

**Soundness rationale.** `Rational` coefficients ensure that two expressions that evaluate to the same linear function under any variable assignment produce the same `LinearForm` key. FP cancellation cannot fabricate a match or miss a match. The depth bound and `null`-on-overflow semantics mean `TryNormalize` either returns a provably correct form or declines — it never returns an incorrect form.

### Relational Fact Store and 5-Tier Lookup

`ProofContext._relationalFacts: Dictionary<LinearForm, RelationalFact>` stores ordering relationships between expressions. Each fact is keyed by `LinearForm(LHS) − LinearForm(RHS)` and carries a `RelationKind` (`>` strict or `>=` non-strict) and a scope (global from `rule`, or event-local from `when` guards).

**Fact injection.** `WithRule(lhs, rel, rhs)` normalizes both sides and stores `RelationalFact` keyed by `LinearForm(lhs) - LinearForm(rhs)` — compound expressions on either side are first-class (closes Gap 2). `WithGuard(condition, branch)` extracts comparisons and stores event-local narrowing facts.

**`LookupRelationalInterval(form)`** runs 5 tiers in order; first non-Unknown result wins:

| Tier | Description | Example |
|---|---|---|
| 1. Direct | Exact key match in `_relationalFacts` | `A−B` matches stored `A−B > 0` → `(0,+∞)` |
| 2. GCD-normalized | Divide all coefficients by their GCD; retry | `3A−3B` → `A−B`; matches stored `A−B > 0` → `(0,+∞)` |
| 3. Negated | Negate the form + GCD-normalize; if found, negate the returned interval | Query `B−A`; stored `A−B > 0`; returns `(-∞, 0)` — proves the expression is negative |
| 4. Constant-offset scan | Scan facts sharing variable terms but differing only in constant; `c > 0` + `>` ⇒ `(c,+∞)` | `A−B+1` vs stored `A−B > 0`; offset `c=1` → `(1,+∞)` (closes Gap 1) |
| 5. Transitive closure | `RelationalGraph.Query(form)` — bounded BFS, depth ≤ 4, facts ≤ 64, nodes ≤ 256 | `A−C` with `A>B`, `B>C` → 2-hop chain derives `A>C` (closes Gap 4) |

All tiers operate on typed `RelationalFact` records in `_relationalFacts`. The legacy string-marker fallback (tier 6 in the original implementation) is deleted — no proof path produces string markers.

### Transitive Closure — RelationalGraph

`RelationalGraph` performs bounded BFS over the `_relationalFacts` store to derive facts not directly stored.

**Algorithm.** Lazy BFS over the relational graph: nodes are `LinearForm`s appearing as either LHS or RHS of any stored relational fact; edges are `>`/`>=` facts. On a query for `IntervalOf(expr)` where no direct fact matches the normalized form, `RelationalGraph.Query(form)` walks outward from the normalized form looking for a chain of facts that implies the target.

**Strict/non-strict matrix.** Encoded as a static table:

| Composition | Result |
|---|---|
| `>` · `>` | `>` |
| `>=` · `>` | `>` |
| `>` · `>=` | `>` |
| `>=` · `>=` | `>=` (does NOT yield `>`) |

`>=` · `>=` correctly does NOT prove nonzero (example: `A >= B`, `B >= A` allows `A = B`).

**Hard caps.** Maximum 64 facts per scope, maximum depth 4, maximum 256 visited nodes per query. On any cap hit, the query returns no derived fact — the caller continues with whatever interval arithmetic already produced. Cap hit is a sound false negative, never a false positive.

**Self-contradiction handling.** Cycle detection during BFS: if the traversal derives `A > A` (or any form whose interval would be empty under the derivation), the derived fact is silently dropped and traversal continues. Self-contradictions surface in the inspector and runtime; the type checker is not the place to enforce rule consistency.

**Optional debug mode.** `--proof-saturate` flag materializes all transitive facts up front (eager mode) for test inspection. Production mode is always lazy.

### Proof State Lifecycle and Scope

**GlobalProofContext (per Check).** Built once per `Check()` invocation. Contains:

- Field constraint intervals (from `nonnegative`, `positive`, `min N`, `max N`).
- Global relational facts from `rule` declarations (always present, all events see them).
- `Choices` and field-declared flags.
- Derived flags from global rules.

**EventProofContext (per event/state, child of global).** Constructed per event/state as `globalCtx.Child()`. Contains:

- Event-arg intervals.
- Guard-narrowed intervals and relational facts.
- Ensure-derived narrowings.
- Sequential assignment effects (`WithAssignment` results).
- All derived facts from Gap 3 (`_exprFacts` and computed-field intervals from `WithAssignment`).

**Cross-event isolation.** `EventProofContext.Mutations` are never promoted on context flush. Derived facts from event E1's transition row cannot influence event E2's `EventProofContext` — E2's context is built fresh from the same `GlobalProofContext` parent. Cross-event fact carryover requires explicit `ensure` clauses, which already produce facts via `BuildEventEnsureNarrowings`.

**`BuildStateEnsureNarrowings`.** Built once per `Check()`, alongside event-ensure narrowings. Processes unconditional `in {State} ensure` clauses (`EnsureAnchor.In`, no `when` guard), grouped by state name. Each group starts from a fresh `ProofContext` seeded with the global data-field kinds and applies `ApplyNarrowing` for each ensure expression. The result is a `IReadOnlyDictionary<string, ProofContext>` — one entry per state that has at least one `in`-anchored ensure. Consumed by both `ValidateTransitionRows` (merged into the `EventProofContext` for transitions whose source state has ensures) and `ValidateStateActions` (merged into the proof context for `initial` and `on enter` actions in states with ensures). State ensures operate on data-field names directly — no bare→dotted translation is needed because state-ensure narrowings apply to the same field namespace as global rules.

**`BuildEventEnsureNarrowings` rewrite.** The bare→dotted field-name translation is now a structural transform on typed `RelationalFact` records: `facts.Select(f => f.Rekey(bare → dotted))`. The string-surgery bug class from PR #108 (`$gt:A:B` → `$gt:Go.A:Go.B` having to split on `:` and dot both parts) is eliminated by construction — the key is a `LinearForm` with typed `FieldKey` terms, and dotting is a map over terms.

### NumericInterval

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
| `Modulo([a,b], [c,d])` | When `[c,d]` excludes zero: `(-\|d\|, \|d\|)`. **Tighter when dividend is nonneg and divisor is positive:** `[0, \|d\|)`. Otherwise: `Unknown`. |
| `Negate([a,b])` | `[-b, -a]` with flipped inclusivity |
| `Abs([a,b])` | Both nonneg → identity. Both nonpositive → negate. Mixed → `[0, max(\|a\|, \|b\|)]` |
| `Min([a,b], [c,d])` | `[min(a,c), min(b,d)]` |
| `Max([a,b], [c,d])` | `[max(a,c), max(b,d)]` |
| `Clamp(x, lo, hi)` | `[max(x.Lower, lo.Lower), min(x.Upper, hi.Upper)]` |
| `Sqrt([a,b])` | When nonneg: `[√a, √b]` preserving inclusivity. Otherwise: `Unknown`. |
| `Floor([a,b])` | `[⌊a⌋, ⌊b⌋]` with both bounds inclusive. Sound but may over-approximate — integer results always land on or inside the floored bounds. |
| `Ceil([a,b])` | `[⌈a⌉, ⌈b⌉]` with both bounds inclusive. Same conservative inclusivity as `Floor`. |
| `Round([a,b])` | `[⌊a⌋, ⌈b⌉]` with both bounds inclusive. Widest safe approximation — `round` can go either direction. |
| `Truncate([a,b])` | Truncation toward zero: positive values floor, negative values ceil. `tLo = a >= 0 ? floor(a) : ceil(a)`, `tHi = b >= 0 ? floor(b) : ceil(b)`, result `[min(tLo,tHi), max(tLo,tHi)]` with both bounds inclusive. Returns `Unknown` when `IsUnknown`. |
| `Pow([a,b], [c,d])` | When exponent is a constant integer: even exponent on nonneg base → `[a^n, b^n]`; even exponent on nonpositive base → `[b^n, a^n]`; even exponent on mixed base → `[0, max(a^n, b^n)]` (lower inclusive when base can be zero and base lacks the Nonzero/Positive flag). Non-integer or non-constant exponent → `Unknown`. |
| `Hull([a,b], [c,d])` | `[min(a,c), max(b,d)]` — join for conditional expression synthesis. **Inclusivity for equal bounds:** when both lower bounds are equal, `LowerInclusive = a.LowerInclusive \|\| b.LowerInclusive`; likewise when both upper bounds are equal, `UpperInclusive = a.UpperInclusive \|\| b.UpperInclusive`. *(Covered by existing `NumericIntervalTests.cs` Hull tests — no new test obligation.)* |

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

**Interval extraction from proof state:** `ProofContext.IntervalOf(identifier)` reads `_fieldIntervals[key]` for the field key, then applies flag projection:

| Flags / stored interval | Interval returned |
|---|---|
| `Positive` flag | `(0, +∞)` |
| `Nonneg` flag | `[0, +∞)` |
| `Nonneg` + `Nonzero` flags | `(0, +∞)` |
| `Nonzero` flag alone | `Unknown` (nonzero spans both positive and negative) |
| Stored interval | Decoded interval from field constraints (`min N`, `max N`) |
| None | `Unknown` |

**Interval injection from field constraints:** At `GlobalProofContext` build time (during `Check()`), field constraints are encoded as intervals in `_fieldIntervals`:

| Constraint | Stored interval |
|---|---|
| `nonnegative` | `[0, +∞)` |
| `positive` | `(0, +∞)` |
| `min V` | `[V, +∞)` |
| `max V` | `(-∞, V]` |
| `min V1` + `max V2` | `[V1, V2]` |

**Min+max combination:** When `Check()` processes field constraints, `min` and `max` constraints on the same field are combined into a single stored interval. If only `min V` exists: `[V, +∞)`. If only `max V` exists: `(-∞, V]`. Both: `[V1, V2]`. The `nonnegative` and `positive` constraints are handled by their flag entries; they do NOT need a duplicate interval entry.

**Note on storage.** Previous implementation used string-encoded `$ival:key:lower:lowerInc:upper:upperInc` markers in the symbol table. This is superseded by typed `_fieldIntervals` storage in `ProofContext`. See Design Decision #1 (Superseded).

### Conditional Expression Proof Synthesis

**Problem solved:** `if Rate > 0 then Amount / Rate else 0` — the then-branch is safe (narrowing proves `Rate > 0`). The result of the whole conditional expression needs an interval so it can be used as a sub-expression in a larger divisor.

**Mechanism.** The `PreceptConditionalExpression` case in `IntervalOf` computes:

1. `thenInterval = IntervalOf(thenBranch, childCtx.WithGuard(condition, true))`
2. `elseInterval = IntervalOf(elseBranch, childCtx.WithGuard(condition, false))`
3. `result = NumericInterval.Hull(thenInterval, elseInterval)`

**Why Hull is correct:** In Precept's execution model, exactly one branch of a conditional expression is evaluated at runtime. The result is either `thenInterval` or `elseInterval`. Hull (the smallest interval containing both) is the correct over-approximation. There is no post-conditional code path where path-sensitivity could tighten the result — the conditional is a single value-producing node, not a branching construct.

**Example:**
```precept
field X as number positive
# if X > 5 then X else 1
# thenInterval: [5, +∞)   (from guard narrowing X > 5)
# elseInterval: [1, 1]
# Hull: [1, +∞)  — ExcludesZero = true
```

---

## IntervalOf Query Path

`ProofContext.IntervalOf(expr)` is the composing query at the center of the unified architecture. At each node, it:

1. **Computes the interval-arithmetic result** via `NumericInterval` transfer rules (for all operators and functions).
2. **Attempts `LinearForm.TryNormalize(expr)`** (bounded depth 8).
3. **If normalization succeeds:** calls `LookupRelationalInterval(normalizedForm)` — the 5-tier relational lookup described in § Relational Fact Store. Intersects the returned interval with the arithmetic result.
4. **Checks `_exprFacts`** for any stored equality or interval fact matching the normalized form. If found, intersects.
5. **Returns the intersection** of all information gathered. Each intersection can only narrow (tighten) the interval — never widen it — so the result is always sound.

For identifiers, `IntervalOf` reads `_fieldIntervals[key]` directly (no normalization needed). For compound expressions, the normalization + relational lookup gives the relational inference; the arithmetic walk gives the interval arithmetic; the intersection gives the tightest provable bound.

---

## Proof Flow

End-to-end flow for a divisor expression reaching the proof-diagnostic assessment:

```
1. GlobalProofContext built once per Check():
   └─→ Field constraint intervals stored in _fieldIntervals
   └─→ Rule relational facts stored in _relationalFacts (keyed by LinearForm)
   └─→ Field flags (positive, nonneg, nonzero) stored in _flags

2. EventProofContext built per event/state as globalCtx.Child():
   └─→ Guard narrowing: WithGuard() adds event-local intervals + relational facts
   └─→ Ensure narrowing: BuildEventEnsureNarrowings (typed RelationalFact map)

3. Sequential assignment walk in ValidateTransitionRows() / ValidateStateActions():
   └─→ After each assignment: WithAssignment(target, rhs)
       ├─→ IntervalOf(rhs) → stores interval in _fieldIntervals[target]
       ├─→ TryNormalize(rhs) succeeds → stores equality fact in _exprFacts
       └─→ Reassignment kill loop: clear prior facts mentioning target field

4. Divisor expression reaches proof-diagnostic assessment:
   a. ctx.IntervalOf(divisor) → result interval
   b. Assessment classifies:
      - Interval is exactly [0, 0] (or provably zero by any path) → C92 (contradiction)
      - Interval.ExcludesZero → no diagnostic (proven safe)
      - Otherwise → C93 (unresolved safety obligation)

5. Same assessment model for sqrt (C76), assignment constraint (C94),
   rule enforcement (C95/C96), and guard enforcement (C97/C98):
   - Proven violation → Error (C94, C95) or Warning (C96, C97, C98)
   - Proven safe → no diagnostic
   - Unknown → no diagnostic (Principle #8 conservatism)
```

---

## Integration Points

| Integration point | File | Location | Role |
|---|---|---|---|
| GlobalProofContext construction | `PreceptTypeChecker.cs` | `Check()` ~line 89 | Build global fact stores from field constraints and rules |
| EventProofContext construction | `PreceptTypeChecker.cs` | `ValidateTransitionRows()` ~line 195, `ValidateStateActions()` ~line 354 | `globalCtx.Child()` per event/state |
| Sequential flow wiring | `PreceptTypeChecker.cs` | `ValidateTransitionRows()`, `ValidateStateActions()` | Call `WithAssignment` after each assignment |
| C93 compound branch | `PreceptTypeChecker.cs` | `TryInferBinaryKind()` ~line 1921 | `ctx.KnowsNonzero(divisor)` |
| C76 sqrt check | `PreceptTypeChecker.cs` | `TryInferFunctionCallKind()` ~line 1722 | `ctx.KnowsNonnegative(arg)` |
| Relational fact injection | `PreceptTypeChecker.cs` | `TryApplyNumericComparisonNarrowing()` | `ctx.WithRule(lhs, rel, rhs)` |
| Ensure narrowing rewrite | `PreceptTypeChecker.cs` | `BuildEventEnsureNarrowings()` | Typed `RelationalFact` map over dotted keys |
| NumericInterval struct | `NumericInterval.cs` | `src/Precept/Dsl/` | Unchanged; gains `IntersectMany` helper |
| ProofContext | `ProofContext.cs` | `src/Precept/Dsl/` | New: central proof state container |
| LinearForm | `LinearForm.cs` | `src/Precept/Dsl/` | New: canonical normalizer |
| Rational | `Rational.cs` | `src/Precept/Dsl/` | New: exact arithmetic for coefficients |
| RelationalGraph | `RelationalGraph.cs` | `src/Precept/Dsl/` | New: bounded transitive closure |
| MCP proof snapshot | `CompileTool.cs` | `tools/Precept.Mcp/Tools/` | Structured `ProofSnapshot` DTO under `proof` key |
| Hover integration | `PreceptHover.cs` | `tools/Precept.LanguageServer/` | `ctx.IntervalOf(expr)` returns `ProofResult` for hover content |

> **Note:** Line numbers are approximate and will shift during implementation.

### MCP Proof DTO Schema (D6, team review 2026-04-18)

The MCP `precept_compile` output includes a `proof` key containing a structured `ProofSnapshot` DTO — NOT raw `Dump()` output. `Dump()` is a debug-only method. The MCP DTO is purpose-built for AI agent consumption.

```json
{
  "proof": {
    "global": {
      "fieldIntervals": {
        "Rate": {
          "interval": { "lower": 1, "lowerInclusive": true, "upper": 100, "upperInclusive": true },
          "display": "1 to 100 (inclusive)",
          "sources": ["rule Rate >= 1", "rule Rate <= 100"]
        },
        "Tax": {
          "interval": { "lower": 0, "lowerInclusive": false, "upper": null, "upperInclusive": false },
          "display": "always greater than 0",
          "sources": ["field constraint positive"]
        }
      },
      "relationalFacts": [
        {
          "subject": "Amount - Tax",
          "requirement": "Nonzero",
          "outcome": "Proven",
          "display": "always greater than 0",
          "sources": ["rule Amount > Tax"],
          "scope": "global"
        }
      ]
    },
    "events": {
      "Submit": {
        "assessments": [
          {
            "subject": "Gross - Tax",
            "requirement": "Nonzero",
            "outcome": "Proven",
            "interval": { "lower": 0, "lowerInclusive": false, "upper": null, "upperInclusive": false },
            "display": "always greater than 0",
            "sources": ["rule Gross > Tax"],
            "scope": "event"
          }
        ]
      }
    }
  }
}
```

**DTO contract:**
- `interval` — machine-readable `NumericInterval` fields for programmatic consumption
- `display` — `ToNaturalLanguage()` output for human/AI display (same formatter as hover and diagnostics)
- `sources` — `ProofAttribution.Sources` array (same data as hover "from:" lines)
- `scope` — `"global"` or `"event"` — which proof context level the fact lives at

---

## Soundness Guarantees

### Fact category invariants

**`interval` (`NumericInterval` on a field key)**
- *Derivation:* From field constraint declarations, event-arg constraints, guard narrowing, ensure narrowing, and `WithAssignment` calling `IntervalOf(rhs)`.
- *Scope:* Field-constraint intervals → global. Event-arg, guard, ensure, assignment-derived → event-local.
- *Invalidation:* On reassignment to the same field key.
- *Soundness:* All arithmetic via `NumericInterval` (saturation handled). FP edge case: `A > B` does NOT imply `A - 1 > B` in IEEE 754 — but this is never derived symbolically; it is always computed via `NumericInterval.Subtract(IntervalOf(A), Singleton(1))`, which produces `(B + ε, +∞) - [1,1] = (B - 1 + ε, +∞)`, correctly NOT proving `> B`. Saturation: `B + C` near `MaxValue` produces `[..., +∞)`, which is sound.

**`$positive` / `$nonneg` / `$nonzero` flags**
- *Derivation:* From interval projection (interval ⊆ (0,+∞) ⇒ Positive flag). From relational facts. From explicit declarations.
- *Scope:* Same as the source.
- *Invalidation:* When the underlying interval or relational fact is invalidated.
- *Soundness:* Pure intersection/projection over `NumericInterval`. Cannot fabricate.

**`$gt` / `$gte` relational facts (keyed by LinearForm)**
- *Derivation:* From `rule` comparisons `LHS op RHS` where both sides normalize. From `when` guard comparisons. From transitive closure (depth-bounded BFS).
- *Scope:* Rule-derived → global. Guard-derived → event-local. Transitive-derived → scope of weakest source (if any source is event-local, the derived fact is event-local).
- *Invalidation:* Direct facts: when a field appearing in either side of the LinearForm is reassigned. Transitive-derived: lazy — re-derived on next query, never cached across mutations.
- *Soundness:* LinearForm uses exact `Rational` (`long/long`) coefficients — no FP cancellation can fabricate a relational fact. Strict/non-strict matrix is explicit. Self-contradictions dropped silently. Transitive closure depth-bounded — termination guaranteed.

**`_exprFacts` (derived facts about compound expressions)**
- *Derivation:* Equality facts from `WithAssignment` (`LinearForm(target) - LinearForm(rhs) ∈ [0,0]`) when `rhs` is normalizable. Interval facts about non-trivial LinearForms from rule narrowing.
- *Scope:* **Always event-local.** Computed-field state never crosses event boundaries.
- *Invalidation:* When any field appearing in the LinearForm is reassigned, the entire entry is dropped (kill loop scans `LinearForm.Terms`).
- *Soundness:* LinearForm equality is used ONLY to substitute one normalized form for another in queries — never to perform symbolic arithmetic. The substitution preserves soundness because `NumericInterval` of either form bounds the same set of concrete runtime values.

**Transitive-derived relational facts**
- *Derivation:* Bounded BFS over `_relationalFacts` graph with explicit strict/non-strict matrix.
- *Scope:* Inherited from source facts (most-restrictive wins).
- *Invalidation:* Lazy — never cached across mutations.
- *Soundness:* Transitivity of `>` and `>=` is exact under IEEE 754 (no arithmetic involved, only fact composition). The strict/non-strict matrix is the only correctness hazard — it is encoded as a small static table reviewed exhaustively in tests.

### What the engine proves

- **Identifier divisors:** Provably nonzero via `Positive`, `Nonzero`, or (`Nonneg` + `Nonzero`) flags. Unchanged from PR #108.
- **Compound divisors:** Provably nonzero via `IntervalOf(expr).ExcludesZero`.
- **Relational divisors:** `A - B` and general `LinearForm`-shaped expressions provably nonzero when a matching strict relational fact exists in `_relationalFacts`, directly or via transitive closure.
- **Computed-field intermediaries:** `set Net = Gross-Tax` followed by `Amount/Net` proves when `IntervalOf(Gross-Tax)` excludes zero — the computed-field interval is stored by `WithAssignment` and available for subsequent divisor checks in the same row.
- **Conditional expression results:** Provably nonzero when Hull of both branch intervals excludes zero.
- **Sqrt arguments:** Provably non-negative via `IntervalOf(arg).IsNonnegative`.
- **Sequential flow:** Post-mutation proof state correctly reflects assignment effects. `set Rate = 0` kills the Positive fact for subsequent uses of `Rate` in the same row.

### What the engine conservatively rejects

These patterns emit C93 even though a human could verify them safe:

- **Cross-event derived facts:** Computed-field intervals derived in event E1 do not carry to event E2. Cross-event carryover requires explicit `ensure` clauses. (This is the soundness load-bearing wall — it never moves.)
- **Non-linear relational patterns:** `rule A * B > 0` does not produce a relational fact. Only expressions normalizable to LinearForm are stored.
- **Function-call opacity:** `abs(X)`, `min(A,B)`, and other built-in function calls are opaque to LinearForm. Function results contribute via `NumericInterval` only.
- **Inequality without ordering:** `rule A != B` does not enter the relational store. Only `>` and `>=` comparisons produce relational facts.
- **Transitive chains beyond depth 4.** The BFS depth cap produces conservative false negatives, never false positives.
- **Deeply nested conditionals:** Hull composes correctly (Hull of Hull) but the resulting interval may be very wide.

### The right tradeoff

A DSL compiler serves domain authors, not PL researchers. The cost of a false positive (claiming safe when it isn't) is a runtime crash — catastrophic in a business rules engine. The cost of a false negative (rejecting a safe expression) is author friction — the author adds a constraint or restructures the expression. The engine is calibrated for zero false positives at the cost of some false negatives. Authors who hit a false negative have clear remediation paths — see § Unsupported Patterns.

---

## Coverage Matrix

22 input patterns. For each: behavior before the unified PR (baseline = PR #108 + C-Nano `6fbb315`) → behavior after.

Legend: ✅ proves (no diagnostic) · ❌ false negative (C93 emitted but expression is provably safe) · 💀 correctly rejected (C93 emitted, expression is unsafe) · ⚠️ false positive (no diagnostic but expression is unsafe — must never happen).

| # | Pattern | Before | After | Gap closed |
|---|---------|--------|-------|------------|
| 1 | `Y / (A - B)` with `rule A > B` | ✅ (C-Nano) | ✅ | regression anchor |
| 2 | `Y / ((A + 1) - B)` with `rule A > B` | ❌ | ✅ | 1 |
| 3 | `Y / (A - (B + C))` with `rule A > B + C` | ❌ | ✅ | 1 + 2 |
| 4 | `Y / (Total - Tax - Fee)` with `rule Total > Tax + Fee` | ❌ | ✅ | 1 + 2 |
| 5 | `Y / (A - B - C)` with `rule A > B`, `rule A > C`, `rule A - B > C` | ❌ | ✅ | 1 + 2 |
| 6 | `set Net = Gross - Tax; check Amount / Net` with `rule Gross > Tax` | ❌ | ✅ | 3 |
| 7 | `set A = X + Y; set B = A - 1; check Z / B` with `rule X + Y > 1` | ❌ | ✅ | 3 + 2 |
| 8 | `set Net = Gross - Tax; set Net = 0; check Amount / Net` (reassignment) | 💀 | 💀 | regression: kill loop |
| 9 | `Y / (A - C)` with `rule A > B`, `rule B > C` (transitive 2-step) | ❌ | ✅ | 4 |
| 10 | `Y / (A - D)` with `rule A > B`, `rule B > C`, `rule C > D` (3-step) | ❌ | ✅ | 4 |
| 11 | `Y / (A - C)` with `rule A >= B`, `rule B > C` (mixed strict, derives `>`) | ❌ | ✅ | 4 |
| 12 | `Y / (A - C)` with `rule A >= B`, `rule B >= C` (both weak, derives `>=` only) | 💀 | 💀 | regression + soundness anchor |
| 13 | `Y / (A - B)` with `rule A > B`, `rule B > A` (self-contradiction) | 💀 | 💀 | regression: silent drop |
| 14 | Event E1: `set Net = Gross - Tax`. Event E2: `check Amount / Net`. No field-declared positivity. | 💀 | 💀 | regression: cross-event isolation |
| 15 | Event E1: derived `Net > 0`. Event E2 (sibling): `check Amount / Net`. | 💀 | 💀 | regression: sibling isolation |
| 16 | `Y / D` where `D` is a field with declared `> 0` constraint | ✅ | ✅ | regression anchor |
| 17 | `Y / abs(X)` where `X` is `nonzero` | ✅ | ✅ | regression anchor |
| 18 | `Y / (Rate * Factor)` where both are `positive` | ✅ | ✅ | regression anchor |
| 19 | `Y / (D - D)` (provably zero) | 💀 | 💀 | regression anchor |
| 20 | `Y / (if A > B then A - B else 1)` with `rule A > B` | ❌ | ✅ | falls out of gap 1 (conditional branch uses composing IntervalOf) |
| 21 | `Y / (A + B)` with `rule A > -B` | ❌ | ✅ | falls out of gap 2 (LinearForm normalization of negated operand rule) |
| 22 | `Y / (A + B - C)` with `rule A + B > C` | ❌ | ✅ | falls out of gap 2 (compound-expression rule) |

**Coverage delta:** 12 patterns now prove that did not before (#2, #3, #4, #5, #6, #7, #9, #10, #11, #20, #21, #22). 0 false positives introduced. 0 regressions.

---

## Unsupported Patterns

Each pattern below is expressible in the DSL but the proof engine cannot prove it safe. Author workarounds are validated against the DSL language reference.

**DSL facts that shaped this table:**
- `rule` supports compound expressions on both sides (`rule A * 2 <= B because "..."`).
- `on Event ensure` is event-arg-only — cannot reference fields (PRECEPT016).
- `in State ensure` is the cross-event field workaround — validated to suppress C93 in receiving-state events.
- `to State ensure` does NOT suppress C93 in the receiving event — use `in`, not `to`.
- `when A > B` guard suppresses C93 for `A - B` divisors; `when A != B` and `when A - B > 0` do NOT.
- `rule D != 0` / `field D as number positive` suppress C93 on bare `D` but not after `set D = expr` — after the unified plan, the global `positive` constraint in `GlobalProofContext` survives assignment (the event-local kill loop does not touch global facts).

| # | Unsupported pattern | Why the proof fails | Author workaround |
|---|---|---|---|
| 1 | `Y / (A * B - C)` with `rule A * B > C` | LHS `A * B` is non-linear — does not normalize to LinearForm | Introduce intermediate: `set Product = A * B` with `rule Product > C because "..."`. Global rule `Product > C` survives; gap 3 forward-infers the interval. |
| 2 | `Y / (A * 2 - B)` with `rule A * 2 > B` | Scalar multiplication `A * 2` IS linear — coefficient `2` is a constant that LinearForm handles | *(works — no workaround needed. Scalar-constant multiplication normalizes correctly.)* |
| 3 | `Y / (A / 2 - B)` with `rule A > 2 * B` | Division by constant produces rational coefficient `1/2` — LinearForm handles this | *(works — no workaround needed. Division by constant normalizes via Rational(1,2).)* |
| 4 | `Y / (abs(X) - B)` with `rule abs(X) > B` | Function calls are opaque to LinearForm — `abs(X)` does not normalize | Introduce intermediate: `set AbsX = abs(X)` with `rule AbsX > B because "..."`. |
| 5 | `Y / (min(A, B) - C)` with `rule min(A, B) > C` | Same — function results do not normalize | Same pattern: `set MinAB = min(A, B)` with `rule MinAB > C because "..."`. |
| 6 | `Y / sqrt(A - B)` with `rule A > B` | sqrt argument check uses `KnowsNonnegative`; sqrt result inherits strict positivity when argument is strictly positive | *(works — validated. `rule A > B` proves `A - B > 0`, sqrt of strictly positive is strictly positive. No C93.)* |
| 7 | `Y / D` where `D = if cond then A else B` and branches are nonzero for different reasons | Conditional hull requires both branches to produce a common provable flag | Ensure both branches share the same provable property: `rule A > 0 because "..."` and `rule B > 0 because "..."` — both branches positive, hull preserves positivity. |
| 8 | `Y / (A - C)` with `rule A >= B` and `rule B >= C` (both weak) | Strict/non-strict matrix: `>= · >= ⇒ >=` only. `A = B = C` satisfies both rules AND makes divisor zero. **Correctly rejected.** | Strengthen at least one rule to strict: `rule A > B` or `rule B > C`. Transitive closure then derives `A > C`. |
| 9 | `set Net = Gross - Tax` in event E1; `Y / Net` in event E2 | Computed-field facts are event-local — the soundness wall prevents cross-event leakage | Three workarounds: (a) `field Net as number positive` — global constraint. (b) `in Step2 ensure Net > 0 because "..."` — suppresses C93 in events from Step2. (c) Repeat `set Net = Gross - Tax` in E2's chain so gap 3 forward-infers within that event. |
| 10 | `Y / D` where `D` is assigned from a function call: `set D = compute(X)` where function is known positive | No function-summary analysis — opaque to LinearForm and NumericInterval | `field D as number positive` — global constraint survives assignment. Runtime rejects any assignment that violates the constraint. |
| 11 | Transitive chain longer than 4 hops | Depth cap = 4 on BFS (sound false negative) | Add a shortcut rule: `rule A > F because "..."`. Direct `rule` always works. |
| 12 | Relational graph with > 64 facts in scope | Fact-count cap = 64 (sound bounded saturation) | Unlikely in realistic precepts. Move some rules to state-scoped `in State ensure` to reduce global fact count. |
| 13 | `Y / (A + B)` where both are `nonneg` but at least one might be zero | Sum of non-negatives is non-negative, not positive | Strengthen one summand: `field A as number positive`. Or add `rule A + B > 0 because "..."` directly. |
| 14 | `Y / (A - B)` where the only fact is `rule A != B` | `!=` does not enter the relational store — only `>` and `>=` | Split into guarded branches: `when A > B -> set Y = Y / (A - B)` and `when B > A -> set Y = Y / (B - A)`. Or add `rule A > B because "..."` if ordering is known. |
| 15 | `Y / (A - B)` where `A` is an event arg and `B` is a state field with cross-namespace rule | Cross-namespace relational facts work when the rule references dotted forms | *(works — no workaround needed. The bare→dotted rewriter handles the event-arg side structurally.)* |
| 16 | `Y / (A % B)` where `A` and `B` are positive | `A % B` can be zero even when both operands are positive (`6 % 3 = 0`). **Correctly rejected.** | `(A % B) + 1` is provably positive via interval arithmetic `[0, ∞) + 1 = [1, ∞)`. Validated. |
| 17 | `rule A > B`, `rule B > A` (self-contradiction) then `Y / (A - B)` | Cycle detected, derived fact silently dropped, C93 emitted | Fix the rules — they are inconsistent. No valid concrete state can satisfy both. |
| 18 | `Y / (k*A - k*B)` with `rule A > B` and constant `k > 0` | Scalar multiple of a stored relational fact | *(works — no workaround needed. Scalar-multiple GCD normalization reduces `+k·A + (-k)·B` to `+1·A + (-1)·B` before relational lookup.)* |
| 19 | `Y / (pow(A - B, 2))` where `A > B` | `pow` and `truncate` contribute via `NumericInterval` interval arithmetic (not LinearForm normalization), so the interval result propagates directly. `pow(A-B, 2)` where `A-B ∈ (0,+∞)` yields `(0,+∞)` — no C93. `truncate(X)` where `X has min 1` yields `[1,+∞)` — no C93. The limitation applies only when the *argument* is itself not provable, not to the function call per se. | Ensure the argument interval excludes zero before the function call. No workaround needed when the argument is provably positive. |

**Unsupported categories summary:**
1. **Non-linear expressions** (rows 1, 4, 5) → hoist into intermediate fields with global `rule` constraints.
2. **Function call opacity** (rows 4, 5, 10) → promote function results to named fields with `positive` constraint or `rule`.
3. **Cross-event derived facts** (row 9) → declare `field` constraint, use `in State ensure`, or re-derive in the consuming event.
4. **Correctly rejected patterns** (rows 8, 16, 17) → true unsafeties or contradictions. Fix the logic or rules.
5. **Bound limits** (rows 11, 12) → add shortcut rules or scope rules tighter.
6. **Inequality without ordering** (row 14) → split into ordered guard branches or add ordering rule.

---

## Risk Register

| Risk | Severity | Mitigation |
|------|----------|------------|
| LinearForm `Rational` arithmetic bug → false-positive proof | **High soundness** | Property tests (associativity/commutativity/distributivity over scalar). `ProofEngineSoundnessInvariantTests` enumerates concrete value set `{-100, -1, -0.5, 0, 0.5, 1, 100}` for free variables and asserts `IntervalOf` result contains every realized value. Includes saturation boundary test with `long.MaxValue`-scale coefficients. Debug-build assertion. |
| FP edge case: `A > B` does NOT imply `A - 1 > B` exploited via symbolic shortcut | **High soundness** | Hard rule in `ProofContext`: all numeric narrowing flows through `NumericInterval` arithmetic. LinearForm equality is used ONLY for fact-key matching, never for symbolic value computation. Property tests cover saturation near MaxValue. |
| Cross-event leakage of derived facts | **High soundness** | Structural: `EventProofContext.Mutations` not promoted on context flush. `BuildEventEnsureNarrowings` consumes only declared `ensure` clauses. Explicit cross-event isolation tests in `ProofEngineComputedFieldTests` and `ProofContextScopeTests`. |
| Reassignment fails to invalidate derived facts → stale proof | **High soundness** | C-Nano kill loop ported verbatim into `WithAssignment`, generalized to scan `LinearForm.Terms`. `ProofContextTests.cs` covers (a) relational-fact invalidation (every prior C-Nano scenario mirrored verbatim) and (b) equality-fact invalidation (new in this PR: `WithAssignment` + `_exprFacts` kill loop scanning `LinearForm.Terms`). |
| Transitive closure exhausts on adversarial precepts | **Medium perf** | Hard caps: 64 facts, depth 4, 256 visited nodes. Cap hit returns Unknown (sound false negative). Stress cases tested. |
| Strict/non-strict combination matrix bug → fabricates `>` from two `>=` | **Medium soundness** | Encoded as a small static table in `RelationalGraph.cs`. Exhaustive tests cover all four combinations. |
| Self-contradiction in relational graph throws or fabricates | **Medium correctness** | Cycle detection in BFS; silent drop on contradiction. Test asserts no throw and no fabrication. |
| LinearForm normalization runs at every `IntervalOf` call → perf regression | **Low perf** | Depth bound (8). Cached on AST node (one allocation per expression, reused across queries). Short-circuit on provably non-normalizable shapes. Budget: no >5% compile-time regression across all 24 sample files. |
| Inspectability regression — proof state harder to dump | **Low UX** | `ProofContext.Dump()` produces a structured snapshot consumed by hover, MCP `precept_compile`, and debugger display. Strictly an improvement over scattered marker strings. |
| `BuildEventEnsureNarrowings` migration breaks bare→dotted rewriter | **Medium correctness** | Typed `RelationalFact` records make dotting a structural transform. The PR #108 multi-field bug class is eliminated by construction. Regression tests in `ProofContextScopeTests`. |
| AST consumers (hover, semantic tokens, MCP DTOs, `ReconstituteExpr`) break | **Low** | LinearForm is parallel to the AST, never replaces it. Diagnostic source spans remain bound to original `PreceptExpression`. AST consumers see no change. |

---

## Numeric Precision and IEEE 754

The proof engine operates on `double` (IEEE 754 binary64) for `NumericInterval` bounds. This introduces three corner cases that are deliberately handled for soundness:

- **Overflow saturates to ±∞.** When an arithmetic operation produces a result beyond `double.MaxValue`, IEEE 754 rounds to `+∞` or `-∞`. This is sound — an interval containing `+∞` or `-∞` is a valid over-approximation. No false "safe" claims result from overflow.
- **NaN inputs produce Unknown-equivalent behavior.** If a `NaN` value enters the interval system (e.g., from a malformed literal or an impossible operation), the affected interval is treated as `Unknown`. The `ExcludesZero` and `IsNonnegative` predicates return `false` for any interval containing `NaN`, which is conservative — the engine rejects rather than falsely approves.
- **Decimal→double cast is a slight widening.** Precept field values use `decimal` at runtime, but interval arithmetic uses `double`. The cast from `decimal` to `double` may widen the value slightly (e.g., `0.1m` → `0.1d` ≈ `0.100000000000000005...`). This is soundness-preserving — a slightly wider interval never causes a false "safe" claim. It may very rarely cause a false "unsafe" claim for values extremely close to zero, which is acceptable given the engine's conservative design.

**`LinearForm` / `Rational` are immune to these issues.** `Rational` uses `long/long` with GCD normalization and `checked` arithmetic. Overflow in `TryNormalize` returns `null` and falls back to interval arithmetic (sound). LinearForm equality for fact-key matching never involves floating-point computation.

---

## Limitations and Future Work

### Deliberate exclusions

- **Cross-event derived fact carryover.** Always unsound for derived facts — event semantics demand fresh state. Cross-event facts MUST come from declared `ensure` clauses via the existing path. This is the soundness load-bearing wall; it never moves.
- **Alias tracking through computed assignments.** `set Backup = Rate` propagates whatever `IntervalOf(Rate)` reveals. Full alias tracking through arbitrary expressions would require must-alias analysis disproportionate to the divisor-safety theme.
- **Non-linear arithmetic.** `A * B` is not linear; representing it requires polynomial normalization (Gröbner-style). Falls back to existing `NumericInterval` arithmetic, which handles `nonzero × nonzero = nonzero` patterns through the flag pipeline.
- **Function calls in LinearForm.** `abs(X)`, `min(A,B)`, etc. produce non-linear shapes. Function results contribute via `NumericInterval` only.
- **SMT, octagons, polyhedra, fixpoint over expression trees.** All rejected. Reaffirmed at unified engine design review.

### Potential precision enhancements

These enhance existing analysis precision without adding new enforcement categories:

- **`nonzero` modifier as first-class constraint:** Would enable `Nonzero` as a flag that survives compound expressions better. Already filed as issue #111.
- **Product sign analysis:** `X * Y` is nonzero when both X and Y are nonzero. Currently only proven when both intervals exclude zero; could also check `Nonzero` flags for each factor.
- **`NumericInterval` bounds representation.** `NumericInterval` stores bounds as `double`. All current endpoint values originate from parsed source literals, where the `decimal`→`double` cast is exact or a known slight widening — soundness is maintained in practice. It is not formally proved for accumulated or computed bounds (multi-hop interval derivation chains). Before expanding the proof surface to include cross-event interval carryover, derived-field interval chaining, or new accumulated-bound operations, the formal precision contract should be documented. Reference: `research/architecture/proof-engine-abstract-interpretation.md` and the `## Numeric Precision and IEEE 754` section above.

For the comprehensive enforcement catalog — assignment constraint checking, dead rule/guard detection, and transition reachability sharpening — see **§ Comprehensive Compile-Time Enforcement** below.

---

## Optimality Assessment

**Verdict: The unified ProofContext + LinearForm + RelationalGraph architecture is the correct architecture for Precept's constraint surface. No structural revision needed.**

### Rationale

1. **Precept's execution model eliminates abstract-interpretation overhead.** No loops, no branches, no reconverging flow (§ Execution Model Assumptions). Single-pass interval arithmetic is not just sufficient — it is *optimal*. Widening, narrowing, fixpoint iteration, and lattice joins are structurally unnecessary.

2. **The constraint surface is entirely interval-compatible.** Every numeric constraint (`nonnegative`, `positive`, `min N`, `max N`) maps directly to a closed/open interval. Collection constraints (`mincount`, `maxcount`) and string constraints (`minlength`, `maxlength`) map to integer count intervals. No constraint in the DSL today requires relational or disjunctive reasoning that intervals cannot express.

3. **SMT is overkill.** Z3/CVC5 would bring 50–65 MB of native dependencies, non-deterministic solving times, opaque proof witnesses, and API-surface complexity — all for a constraint surface that is decidable with interval arithmetic alone. This violates Principle #9 (tooling drives syntax — structured diagnostics, not opaque solver output) and Principle #12 (AI legibility — proof witnesses must be inspectable).

4. **A fuller abstract-interpretation lattice is disproportionate.** Octagons (O(n²) per variable) or polyhedra (exponential worst case) would handle multi-variable relationships but at ~2,000+ lines of implementation for a DSL compiler serving business domain authors. The targeted relational layer (`_relationalFacts` keyed by LinearForm + bounded RelationalGraph BFS) handles the dominant inter-variable pattern in ~360 lines — 97% of the benefit at 3% of the cost. The architecture is the CodeContracts "Pentagons" blueprint — interval domain + lightweight relational domain — the proven point on the cost/power curve for this problem class.

5. **A simpler pattern-based approach is too weak.** Pattern matching misses compound expressions. The unified engine proves things like `clamp(D, 1, 100)` excludes zero, `Score + Amount ∈ [1, ∞)` from constraint-derived intervals, and `if X > 0 then X else 1 ∈ (0, ∞)` via Hull — no pattern matcher can match this generality. Moreover, each new proof shape under the old approach required its own bespoke method (C-Nano illustrated this explicitly). LinearForm closes three gaps simultaneously because it is the typed, exact vocabulary that ad-hoc string markers were approximating.

6. **Philosophy alignment is direct.** The design serves Principle #1 (deterministic — same input → same proof), Principle #8 (sound compile-time-first — never false-positive, conservative on unknowns), Principle #9 (tooling drives syntax — structured diagnostics with interval witnesses for hover/preview), and Principle #12 (AI legibility — proof witnesses are data, not opaque solver traces).

### Alternatives considered and rejected

| Alternative | Why rejected |
|---|---|
| Extended marker conventions with bounded helpers (C-Nano trajectory) | Cannot close gaps 1+2 together without inventing a parallel canonical-expression vocabulary as marker keys. LinearForm *is* that vocabulary, typed and exact instead of string-encoded and ad hoc. |
| AST normalization in the parser | Breaks `ReconstituteExpr`, source spans, semantic tokens, hover, and MCP DTOs. LinearForm as a parallel form avoids this. |
| Constraint propagation graph | Adds node-based structure with no benefit over direct interval computation — Precept expressions are trees, not constraint networks. |
| Symbolic execution | Collapses to interval arithmetic when there are no branches — Precept's model makes symbolic execution degenerate. |
| SMT-backed verification | Disproportionate dependency cost; opaque proofs violate Principle #12; non-deterministic timing. |
| Octagon / polyhedra domains | O(n²)/exponential cost for multi-variable relations; the targeted relational inference covers the dominant pattern. |

---

## Design Decisions

### 1. String-Encoded Interval Markers — SUPERSEDED

**Decision (original, PR #108):** Interval data from field constraints was stored as string-encoded markers (`$ival:key:lower:lowerInc:upper:upperInc`) in the existing `IReadOnlyDictionary<string, StaticValueKind>` symbol table.

**Status: SUPERSEDED** by the unified engine's typed `ProofContext._fieldIntervals` (`Dictionary<FieldKey, NumericInterval>`).

**Why superseded.** The string-marker approach was correctly motivated at PR #108 time (avoided threading a second dictionary through ~15 methods). The unified engine's `ProofContext` parameter threading replaces the symbol table entirely, making typed storage the natural choice. The string-encoding and `CultureInfo.InvariantCulture` parsing overhead are eliminated. The bug surface (malformed marker strings silently producing `Unknown`) is eliminated by construction.

**New decision (unified engine).** `ProofContext` is the single proof state parameter threaded through all narrowing and validation methods. Interval data is stored in `_fieldIntervals: Dictionary<FieldKey, NumericInterval>`. Relational facts are stored in `_relationalFacts: Dictionary<LinearForm, RelationalFact>`. All string-marker conventions (`$positive:`, `$nonneg:`, `$nonzero:`, `$ival:`, `$gt:`, `$gte:`) are replaced by typed storage.

**Tradeoff.** Refactoring `PreceptTypeChecker.cs` to replace ~15 method signatures is the price of the unified model. The risk is mitigated by Commit 2 of the implementation plan being a purely mechanical signature refactor (no behavior change) with full test coverage before behavior changes land.

### 2. Subsuming Sign Analysis into Intervals

**Decision:** There is no separate "sign analysis" layer. Sign information is a special case of interval bounds — `Positive` is `(0, +∞)`, `Nonneg` is `[0, +∞)`, `Nonzero` is the complement of `{0}` (handled via flag fallback).

**Alternative rejected:** Dedicated sign domain (Positive / Negative / Nonneg / Nonpositive / Zero / Nonzero / Unknown) with its own transfer rules, running as a parallel analysis alongside intervals.

**Rationale:** A sign domain duplicates information already captured by interval bounds. Every sign transfer rule is a special case of the corresponding interval transfer rule. Maintaining two parallel analyses adds ~100 lines of code with no additional proving power — intervals subsume everything sign analysis can do, plus they handle bounded ranges (`min 5`, `clamp(x, 1, 100)`) that sign analysis cannot.

**Tradeoff accepted:** `Nonzero` (the union `(-∞, 0) ∪ (0, +∞)`) is not representable as a single interval. The engine falls back to the `Nonzero` flag for this case. This is not a regression — PR #108 already handled `Nonzero`-flagged identifier divisors.

### 3. Hull for Conditional Expressions

**Decision:** The result interval of `if/then/else` is the hull (smallest enclosing interval) of the then-branch and else-branch intervals, with each branch evaluated under the narrowed proof context from the guard.

**Alternative considered:** Path-sensitive analysis that tracks which branch was taken and narrows accordingly.

**Rationale:** Precept's conditional expressions are value-producing nodes, not control-flow constructs. There is no post-conditional code path where the branch choice matters — the result is a single value used in the enclosing expression. Hull is sound (it contains both possible results) and optimal for this use case. Path-sensitive analysis would add complexity with no benefit.

### 4. Relational Markers as a Separate Layer — SUPERSEDED

**Decision (original, PR #108):** `$gt:{A}:{B}` and `$gte:{A}:{B}` markers were harvested from guards/rules/ensures and checked in `TryInferRelationalNonzero` as a fallback after interval analysis.

**Status: SUPERSEDED** by unified `LinearForm`-keyed relational facts in `ProofContext._relationalFacts`.

**Why superseded.** The old approach only handled `id OP id` patterns — `A - B` where both sides were bare identifiers. Any compound expression on either side fell through. The new approach stores relational facts keyed by the `LinearForm` of `LHS - RHS`, handling any normalizable expression on either side. `TryInferRelationalNonzero` is deleted.

**New decision (unified engine).** Relational facts are stored as `RelationalFact` records in `_relationalFacts: Dictionary<LinearForm, RelationalFact>`. The fact key is `LinearForm(lhs) - LinearForm(rhs)`. `WithRule(lhs, rel, rhs)` calls `TryNormalize` on both sides; if either is non-normalizable, the fact is dropped (sound — false negative, not false positive). `IntervalOf` queries the store by normalizing the divisor expression and looking for a key that matches (or matches after scalar-multiple GCD reduction). This single query handles direct matches, compound operands, and — via `RelationalGraph` — transitive chains.

### 5. Proven-Violation-Only Policy for Constraint Enforcement

**Decision:** C94 fires only when expression interval and constraint interval have NO overlap. "Possible violation" (partial overlap) produces no diagnostic.

**Alternative rejected:** Warning on possible violations (expression range extends beyond constraint).

**Rationale:** Precept's runtime invariant system is designed to catch constraint violations at execution time. Flagging every assignment that MIGHT produce an out-of-range value would flood authors with warnings on correct code. An assignment like `set Score = Score + Amount` with `max 100` INTENTIONALLY relies on the runtime constraint to reject the `Score = 100, Amount > 0` case.

**Precedent:** Rust's const-evaluation in match exhaustiveness only flags provably exhaustive/unreachable patterns, not "might be" patterns. TypeScript's narrowing errors on provable contradictions, not possible ones.

**Tradeoff accepted:** `set Score = Score + 100` (which exceeds max when Score > 0) is not flagged because `[100, +∞)` ∩ `(-∞, 100]` = `{100}` (non-empty). The runtime catches the actual violation. The engine only catches the case where NO value in the expression range could satisfy the constraint.

### 6. Simple Single-Field Scope for Dead Rule/Guard Analysis

**Decision:** C95/C96/C97/C98 only analyze simple single-field comparisons (`Field <op> Literal`). Cross-field and complex expressions are unanalyzed.

**Alternative rejected:** Full constraint satisfaction for arbitrary boolean expressions.

**Rationale:** General constraint satisfaction is disproportionate to the benefit. Simple single-field comparisons cover the dominant patterns. Cross-field analysis would require relational intervals or an SMT solver — both violate the non-SMT, single-pass architecture.

**Tradeoff accepted:** `rule A + B < 5` with `A min 3` and `B min 3` is not flagged even though it's contradictory.

### 7. Error vs Warning Severity Split

**Decision:** Proven violations that make code structurally dead → Error. Proven redundancies or unnecessary constructs → Warning.

| Diagnostic | Condition | Severity | Rationale |
|---|---|---|---|
| C94 | Assignment always violates constraint | Error | Runtime will always reject — dead code |
| C95 | Rule always unsatisfiable | Error | Global integrity failure — nothing works |
| C96 | Rule always true | Warning | Not harmful, just unnecessary |
| C97 | Guard always false | Warning | Unreachable code, not harmful |
| C98 | Guard always true | Warning | Unnecessary condition, not harmful |

### 8. No New DSL Constructs Required

**Decision:** All enforcements apply to constructs that exist in the DSL today. No new keywords, modifiers, or syntax forms are needed.

**Observation:** Two gaps noted:
- **`nonzero` modifier:** Would enable `Nonzero` as a first-class constraint, improving interval precision for divisor safety. Already filed as issue #111. Not required for any enforcement in this design.
- **`length` constraint on strings:** Would enable C94 for string assignments. Separate language proposal, not part of this design.

---

## Proof-Diagnostic Assessment Model

All proof-backed diagnostics route through a shared assessment model. This eliminates ad hoc message branching and ensures the compiler tells a consistent truth-based story across `sqrt()`, division, modulo, assignment constraints, rules, and guards.

### Assessment Structure

Each proof-backed check produces a `ProofAssessment` with:

| Field | Type | Purpose |
|---|---|---|
| `Requirement` | enum | What the check requires: `Nonzero` (divisor), `Nonnegative` (sqrt), `InConstraintRange` (C94), `Satisfiable` (C95), `NotVacuous` (C96), `Reachable` (C97), `NotTautological` (C98) |
| `Outcome` | enum | `Contradiction` (provably violated), `Proven` (provably satisfied), `Unresolved` (cannot determine), `Unknown` (no information) |
| `StrongestFact` | `NumericInterval?` | The tightest interval the engine could derive for the subject expression |
| `Evidence` | `string?` | Source attribution — which rule, constraint, or guard produced the fact |
| `Scope` | enum | `Global` (from field constraints/rules) or `EventLocal` (from guards/ensures/assignments) |

### Outcome-to-Diagnostic Mapping

| Requirement | Outcome | Diagnostic | Severity |
|---|---|---|---|
| `Nonzero` | `Contradiction` (interval is exactly `[0,0]` or provably zero) | **C92** | Error |
| `Nonzero` | `Unresolved` (zero not excluded) | **C93** | Error |
| `Nonzero` | `Proven` (interval excludes zero) | — | — |
| `Nonnegative` | `Contradiction` (interval is provably negative) | **C76** | Error |
| `Nonnegative` | `Unresolved` (non-negative not proven) | **C76** | Error |
| `Nonnegative` | `Proven` (interval is non-negative) | — | — |
| `InConstraintRange` | `Contradiction` (no overlap) | **C94** | Error |
| `Satisfiable` | `Contradiction` (no overlap) | **C95** | Error |
| `NotVacuous` | `Contradiction` (constraint ⊆ satisfying) | **C96** | Warning |
| `Reachable` | `Contradiction` (no overlap) | **C97** | Warning |
| `NotTautological` | `Contradiction` (constraint ⊆ satisfying) | **C98** | Warning |

### Truth-Based C92

C92 fires on any **provably-zero** divisor — not just literal zero. Sources of provably-zero:

- Literal `0` in source
- Identifier with `IntervalOf(id)` returning `[0, 0]`
- Assignment-derived: `set X = 0; Y / X` — `WithAssignment` stores `_fieldIntervals[X] = [0,0]`
- Expression-derived: `IntervalOf(A - A)` via `LinearForm` constant form returning `[0, 0]`

**Rationale:** The old C92 (literal-zero-only) was a syntax check, not a proof. The unified engine already knows when an expression is provably zero — routing that knowledge to a distinct diagnostic code is free and more informative to the author.

### Truth-Based C93

C93 means **unresolved safety obligation**: the compiler cannot prove the divisor is nonzero, and the interval does not exclude zero. This is materially distinct from C92 (contradiction — provably zero). The old C93 mixed both cases under "unproven divisor"; the new model splits them cleanly.

### Diagnostic Rendering

The shared assessment model feeds a single proof-diagnostic renderer that:

1. Selects the diagnostic code from the outcome-to-diagnostic mapping above
2. Formats the message using `NumericInterval.ToNaturalLanguage()` (Elaine UX spec — no interval notation, no compiler internals)
3. Attaches source attribution from `ProofAttribution` (eager tracking from `IntervalOf`)
4. Emits structured metadata on every diagnostic for tooling consumers (code actions, hover, MCP)

**Diagnostic surfaces (R11).** All proof-backed diagnostics originate as `PreceptDiagnostic` instances from `PreceptCompiler.CompileFromText()`. There is one pipeline, not three. The same `PreceptDiagnostic` list is consumed identically by:

- **Language server** — maps each diagnostic to an LSP `Diagnostic` (squiggle, severity, message). File: `tools/Precept.LanguageServer/PreceptDiagnosticsPublisher.cs`.
- **MCP `precept_compile`** — serializes each diagnostic to the `diagnostics[]` array in the JSON response. File: `tools/Precept.Mcp/Tools/CompileTool.cs`.
- **CLI** (future) — will render the same list to console output.

When this document says "the compiler reports C93," it means a `PreceptDiagnostic` with code `C93` is added to the compilation result. All surfaces display it — no surface has its own diagnostic generation logic.

Code actions consume the structured assessment metadata — they do NOT parse diagnostic message text.

### Structured Diagnostic Metadata (D7, team review 2026-04-18)

Every proof-backed diagnostic carries structured metadata alongside the human-readable message:

| Field | Type | Purpose |
|---|---|---|
| `AssessmentOutcome` | enum | `Contradiction` / `Proven` / `Unresolved` / `Unknown` |
| `Subject` | `string` | The expression or field under proof (e.g. `"Rate"`, `"Gross - Tax"`) |
| `Requirement` | enum | What the check requires (same as `ProofAssessment.Requirement`) |
| `StrongestFact` | `NumericInterval?` | Tightest interval derived |
| `Attribution` | `ProofAttribution` | Source rules/constraints — from eager `IntervalOf` tracking |

This metadata is consumed by:
- **Code actions** — extract subject and requirement without regex parsing
- **Hover** — append proof section using `StrongestFact.ToNaturalLanguage()` + `Attribution.Sources`
- **MCP** — project into the proof DTO for AI agent consumption

### Natural-Language Interval Formatting (D3+D4, team review 2026-04-18)

`NumericInterval.ToNaturalLanguage()` converts intervals to user-facing phrasing. **This single formatter is used by all three surfaces: hover, diagnostics, and MCP display.** No surface uses interval notation.

| Interval | Natural Language |
|---|---|
| `(0, +∞)` | "always greater than 0" |
| `[0, +∞)` | "0 or greater" |
| `[1, 100]` | "1 to 100 (inclusive)" |
| `(1, 100)` | "between 1 and 100 (exclusive)" |
| `[1, 100)` | "1 to less than 100" |
| `(1, 100]` | "greater than 1 to 100" |
| `[5, 5]` | "exactly 5" |
| `[0, 0]` | "exactly 0" |
| `(-∞, 0]` | "0 or less" |
| `(-∞, 0)` | "always less than 0" |
| `(-∞, +∞)` | *(show nothing — no proof = no section)* |
| `[N, +∞)` | "N or greater" |
| `(-∞, N]` | "N or less" |
| `(N, +∞)` | "always greater than N" |
| `(-∞, N)` | "always less than N" |

**ExcludesZero annotation:** When the interval excludes zero AND the context is a divisor/nonzero check, append ", never zero". Example: `[1, 100]` → "1 to 100 (inclusive), never zero".

**Diagnostic message format:** All proof-backed diagnostics use `ToNaturalLanguage()` instead of interval notation. Example C94 message:
> "Assignment to 'Score' provably violates the 'max 100' constraint. Expression produces a value that is always greater than 100 — required range is 0 to 100 (inclusive)."

### Hover Trigger Conditions

The language server appends a proof section to hover content for the following AST node positions:

1. **Field declarations** with numeric type (`field F as number`) — hover shows the field's proven interval from global proof state (field constraints + rules).
2. **Numeric expressions** in `set` assignment RHS — hover shows the expression's `IntervalOf` result under the event proof context at that assignment point.
3. **Numeric expressions** in `rule` declarations — hover shows the expression's proven interval.
4. **Numeric expressions** in `when` guard conditions — hover shows the expression's proven interval (under the guard's proof context).

**Suppression rules:**
- Show nothing when `IntervalOf` returns `Unknown`.
- Show nothing when the interval is `(-∞, +∞)` — an unbounded interval carries no useful information.
- Show nothing for non-numeric expressions (string, boolean, choice fields).

**Rule and guard declaration hover (R10):** Hovering on a `rule` or `when` declaration keyword (not the expression inside it) shows what the declaration *contributes* to the proof state, framed as effect rather than range:

- **`rule` declaration hover:** "This rule proves {field} is {natural-language interval}." with `from:` self-referencing the rule. Example: hovering on `rule Rate >= 1` → "This rule proves Rate is 1 or greater." If the rule is vacuous (C96) or contradictory (C95), show the diagnostic instead.
- **`when` guard hover:** "This guard narrows {field} to {natural-language interval} in this branch." Example: hovering on `when Score > 0` → "This guard narrows Score to always greater than 0 in this branch." If the guard is dead (C97) or vacuous (C98), show the diagnostic instead.
- **`ensure` declaration hover:** Same pattern as `rule` — "This ensure proves {field} is {natural-language interval} in state {State}."

This is distinct from expression hover (which shows the *result* of proof composition) — declaration hover shows the *input* a single declaration contributes to the proof state.

**Format:** Separator `---` between existing hover content and proof section. Section label "Proven safe:". Source attribution "from:" line. See § Natural-Language Interval Formatting for the display phrasing. See PR #108 Commit 14 for the full Elaine-reviewed UX spec.

---

## Conditional Expression Proof Composition

Conditional expressions (`if/then/else`) compose with the proof engine through **guard-narrowed branching + Hull**:

1. The guard condition narrows the proof state for the then-branch via `WithGuard(condition, true)`.
2. The negated guard narrows the proof state for the else-branch via `WithGuard(condition, false)`. **This includes injecting the negated relational fact** — e.g., `if A > B` injects `B >= A` into the else-branch context. (D5, team review 2026-04-18: explicit else-branch negated guard narrowing is a Commit 12 acceptance criterion.)
3. Each branch is evaluated under its narrowed context, producing a `NumericInterval`.
4. The result interval is the **Hull** (smallest enclosing interval) of both branch intervals.

### Conditional + Relational Composition

When a conditional guard is a relational comparison (e.g., `if A > B`), the guard narrowing injects a relational fact into the branch's proof state. This means:

- **Then-branch** of `if A > B then A - B else ...` sees `A > B` as a relational fact → `IntervalOf(A - B)` returns `(0, +∞)`.
- **Else-branch** of `if A > B then ... else B - A` sees `B >= A` as a relational fact → `IntervalOf(B - A)` returns `[0, +∞)`.

The Hull of both branches determines the result interval. If both branches exclude zero, the conditional result excludes zero and no C93 fires.

**Key test patterns for composition closure:**

| Pattern | Then interval | Else interval | Hull | C93? |
|---|---|---|---|---|
| `if A > B then A - B else 1` | `(0, +∞)` | `[1, 1]` | `(0, +∞)` | No |
| `if A > B then A - B else B - A` | `(0, +∞)` | `[0, +∞)` | `[0, +∞)` | **Yes** — else branch includes zero when `A == B` |
| `if A > B then A - B else 0` | `(0, +∞)` | `[0, 0]` | `[0, +∞)` | **Yes** — hull includes zero |
| `if A > 0 then A else 1` | `(0, +∞)` | `[1, 1]` | `(0, +∞)` | No |
| `if A > B then A - B else B - A + 1` | `(0, +∞)` | `[1, +∞)` | `(0, +∞)` | No |

**Soundness:** Hull is always sound — it over-approximates. If the Hull includes zero, C93 fires (conservative). If the Hull excludes zero, both branches genuinely exclude zero under their respective guard-narrowed contexts.

---

## Comprehensive Compile-Time Enforcement

The proof engine's interval infrastructure enables enforcement beyond C93 (divisor safety) and C76 (sqrt safety). This section catalogs every construct with compile-time-checkable semantics and specifies how the proof engine reasons about each.

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

| Construct | Enforcement | Diag | Sev | Algorithm |
|---|---|---|---|---|
| `set Field = expr` (numeric) | Expr interval provably outside field constraint interval | C94 | Error | IntervalOf + containment |
| `to/from State -> set ...` | Same for state actions | C94 | Error | Same |
| Computed `field -> expr` | Formula interval provably violates computed field constraint | C94 | Error | Same |
| `rule expr because "..."` | Rule predicate contradicts field constraints (unsatisfiable) | C95 | Error | Interval intersection |
| `rule expr because "..."` | Rule predicate always true given constraints (vacuous) | C96 | Warning | Interval containment |
| `when guard` (row/edit/ensure) | Guard provably always false | C97 | Warning | IntervalOf proof evaluation |
| `when guard` (row/edit/ensure) | Guard provably always true | C98 | Warning | IntervalOf proof evaluation |
| Transition rows via C97 | Dead row sharpens C50/C51 | C50/C51 | Warning (existing) | Via C97 |
| State reachability via C97 | All incoming rows dead sharpens C48 | C48 | Warning (existing) | Via C97 |
| Boolean guard refinement | Boolean field narrowing to {true}/{false} | — | — | Existing narrowing |
| Choice assignment | Literal not in choice set | C68 | Error (existing) | Existing |
| Divisor safety | Divisor provably zero / unproven nonzero | C92/C93 | Error | Unified engine |
| Sqrt safety | Argument provably negative / unproven non-negative | C76 | Error | Unified engine |

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

2. Compute `exprInterval = ctx.IntervalOf(expr)` using the unified engine.
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

**Nullable interaction:** C94 checks only apply when the RHS expression is provably non-null (no `Null` in `StaticValueKind`). If the expression might be null, no C94 check — the null case is valid by the `nullable` declaration.

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

**Diagnostic message:** `"Assignment to '{field}' provably violates the '{constraint}' constraint. Expression produces a value that is {exprInterval.ToNaturalLanguage()} — required range is {constraintInterval.ToNaturalLanguage()}."`

**Remediation guidance (S4):** The author must either adjust the expression to produce values within the constraint range, or widen the field constraint if the intended range was wrong. For computed fields, verify the formula against all possible input combinations.

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

**Scope limitation:** Only simple single-field comparisons. Cross-field rules (`rule A > B`) and complex expressions (`rule X + Y > 0`) are unanalyzed.

**Diagnostic message:** `"Rule '{expression}' contradicts the '{constraint}' constraint on field '{field}'. Field values are {constraintInterval.ToNaturalLanguage()} but the rule requires {satisfyingInterval.ToNaturalLanguage()} — no valid value satisfies both."`

**Remediation guidance (S4):** The author must remove or adjust one of the conflicting declarations — either relax the field constraint or change the rule predicate so a valid range exists. A contradictory rule means no mutation can ever succeed.

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C95_ContradictoryRule_EmitsC95` | Contradictory rule emits C95 warning |
| `Check_C95_SatisfiableRule_NoC95` | Satisfiable rule emits no diagnostic |
| `Check_C95_RuleAtExactBoundary_NoC95` | Rule at exact boundary emits no diagnostic |
| `Check_C95_LiteralOnLeftSide_EmitsC95` | Literal-on-left contradictory rule emits C95 |

### C96: Vacuous Rule

**What:** A non-synthetic rule whose predicate is provably always true given the field's declared constraints.

**Algorithm:** Same extraction as C95. Check containment: if `constraintInterval ⊆ satisfyingInterval` → **C96 warning**.

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

**Severity: Warning.** The rule isn't wrong — it's unnecessary. Principle #8 conservatism: warn but don't block.

**Example:** `field Score as number min 0 max 100` + `rule Score >= 0 because "..."`. Constraint `[0, 100]`, satisfying `[0, +∞)`, `[0, 100] ⊆ [0, +∞)` → C96.

**Note:** Synthetic rules (generated by constraint desugaring) are excluded from C96 analysis.

**Diagnostic message:** `"Rule '{expression}' is always satisfied — field '{field}' is already constrained to {constraintInterval.ToNaturalLanguage()}, which always satisfies this rule. Consider removing the redundant rule."`

**Remediation guidance (S4):** Remove the redundant rule — the field constraint already guarantees the property. If the rule was intentional documentation, consider using a `because` message on the field constraint instead.

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C96_NonnegVsGte0_Warning` | `nonnegative` + `rule X >= 0` |
| `Check_C96_MinVsGteLower_Warning` | `min 5` + `rule X >= 0` |
| `Check_C96_MaxVsLteHigher_Warning` | `max 100` + `rule X <= 200` |
| `Check_C96_SyntheticRule_Excluded` | Synthetic rule from constraint → no C96 |
| `Check_C96_NotVacuous_NoDiagnostic` | `min 0 max 100` + `rule X >= 50` → no C96 |

### C97: Dead Guard

**What:** A `when` guard on a transition row, edit declaration, or ensure whose condition is provably always false given the proof state at the point of evaluation.

**Algorithm:** At each `when` guard, extract the guard's satisfying interval for each field it references (simple single-field comparison extraction). Look up the field's constraint interval from the current proof state. If `!Intersects(guardSatisfyingInterval, fieldConstraintInterval)` for any field → **C97 warning**.

**Extended to relational guards:** When the proof state contains a relational fact `A > B` and the guard is `when A <= B`, the guard contradicts the relational fact → C97.

**Severity: Warning.** Dead guards create unreachable code — similar to existing C48 (unreachable state), also a warning.

**Example:** `field Score as number max 100` + `when Score > 100 -> ...`. Guard satisfying `(100, +∞)`, constraint `(-∞, 100]`, no intersection → C97.

**Diagnostic message:** `"Guard 'when {expr}' is provably always false — this row/declaration is unreachable. Field '{field}' is constrained to {constraintInterval.ToNaturalLanguage()} but the guard requires {guardInterval.ToNaturalLanguage()}."`

**Remediation guidance (S4):** Remove the unreachable row or adjust the guard condition to match a reachable range. If the guard was protecting against a condition that constraints already prevent, the guard is unnecessary.

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

**Algorithm:** Same extraction as C97. Check: `Contains(guardSatisfyingInterval, fieldConstraintInterval)` → **C98 warning**.

**Severity: Warning.** The guard adds no information — removing `when` would not change behavior.

**Example:** `field Rate as number positive` + `when Rate > 0 -> ...`. Guard satisfying `(0, +∞)`, constraint `(0, +∞)`, constraint ⊆ satisfying → C98.

**Diagnostic message:** `"Guard 'when {expr}' is provably always true — the condition has no effect. Field '{field}' is already constrained to {constraintInterval.ToNaturalLanguage()}, which always satisfies this guard."`

**Remediation guidance (S4):** Remove the unnecessary guard — the constraint already guarantees the condition. If the guard was intended to document intent, consider using a `because` message on the relevant constraint or rule instead.

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C98_PositiveVsGt0_Warning` | `when Rate > 0` with `positive` |
| `Check_C98_NonnegVsGte0_Warning` | `when Score >= 0` with `nonnegative` |
| `Check_C98_MinVsGteLower_Warning` | `when Count >= 1` with `min 1` |
| `Check_C98_NotVacuous_NoDiagnostic` | `when Score > 50` with `min 0 max 100` → no C98 |

### Transition Reachability Sharpening

The proof engine sharpens three existing diagnostics by combining dead guard detection (C97) with existing state-graph analysis:

**C50 sharpening (dead-end state):** If all transition rows leaving a state have provably-dead guards (all C97), the state has no viable exits. C50 fires even though rows syntactically exist.

**C51 sharpening (always-rejecting):** If all non-reject rows for a `(State, Event)` pair have dead guards, only reject rows remain viable. C51 fires.

**C48 sharpening (unreachable state):** If all transition rows targeting a state have dead guards, no path can reach it. C48 fires even though transition rows exist.

These are NOT new diagnostics — they are sharper triggers for existing ones, using interval analysis instead of purely syntactic analysis.

**Test obligations:**

| Test | Verifies |
|---|---|
| `Check_C50_Sharpened_AllGuardsDead_Warning` | State with rows but all guards are C97 |
| `Check_C51_Sharpened_NonRejectRowsDead_Warning` | Only dead-guard non-reject rows |
| `Check_C48_Sharpened_IncomingRowsDead_Warning` | All incoming transitions have dead guards |

### Collection, String, Boolean, and Choice Reasoning

**Collection count intervals:** `mincount N` and `maxcount N` define integer intervals for `.count`. After `add`, count ∈ `[prev.count, prev.count + 1]`; after `clear`, count = 0; after `enqueue`/`push`, count = prev.count + 1; after `dequeue`/`pop`, count = prev.count - 1.

**Design decision: NOT enforced.** Collection mutations are data-dependent. Count evolution depends on runtime membership, which the proof engine cannot track without element-level set analysis. The runtime invariant system enforces `mincount`/`maxcount` after each mutation.

**String length intervals:** **NOT enforced.** String operations in Precept are limited, and the dominant patterns involve runtime-dependent string values.

**Boolean reasoning:** Already tracked through the narrowing system as `{true, false}` / `{true}` / `{false}`. No new proof engine work needed.

**Choice/enum reasoning:** C68 already checks literal assignment to choice fields at compile time. No new enforcement needed.

---

## Enforcement Soundness Guarantees

The proof engine's no-false-positive guarantee extends to all new enforcements:

### What the engine proves (complete list)

- **C92:** Divisor is provably zero — proven contradiction. Fires on any provably-zero divisor (literal, identifier, assignment-derived, exact-zero expression), not just literal zero.
- **C93:** Divisor has no compile-time nonzero proof — unresolved safety obligation. Zero still possible or nonzero proof absent, but not provably zero.
- **C76:** Sqrt argument has no compile-time non-negative proof. Provably-negative cases map to contradiction; unresolved cases map to unresolved obligation.
- **C94:** Assignment expression interval provably outside field constraint interval. The `Intersects` predicate is sound — it returns false only when there is provably zero overlap.
- **C95:** Rule predicate satisfying interval provably disjoint from field constraint interval.
- **C96:** Field constraint interval provably contained within rule satisfying interval.
- **C97/C98:** Guard satisfying interval provably disjoint from / contains the field constraint interval.
- **Sequential flow:** Post-mutation proof state correctly reflects assignment effects.
- **Relational facts:** LinearForm-shaped expressions provably nonzero when a matching strict relational fact exists (directly or transitively).
- **Computed-field intermediaries:** Intervals derived from `WithAssignment` are available to subsequent divisor checks in the same row.
- **Conditional results:** Provably nonzero when Hull of both branch intervals excludes zero.

### What the engine conservatively rejects (updated)

- **Partial-overlap constraint violations.** `set Score = Score + Amount` where the result interval partially exceeds `max 100` is not flagged. The runtime handles it.
- **Cross-field rule contradictions.** `rule A > B` with constrained A and B is not analyzed for satisfiability.
- **Complex rule/guard expressions.** `rule X + Y > 0` or `when A * B > 5` are not analyzed — only simple single-field comparisons.
- **`!= N` predicates in rules.** The satisfying set `(-∞, N) ∪ (N, +∞)` is disjunctive and not representable as a single interval. Skipped.

---

## Test Obligations

**Baseline (branch at design review):** 1469 tests. (The plan's original 1364 baseline predated the DiagnosticSpanPrecisionTests (28 tests) and C-Nano commits that landed before the unified engine PR.)

**Post-PR target:** ~1740+ tests (1469 + ~270 new).

| File | New tests | Coverage summary |
|------|-----------|-----------------|
| `LinearFormTests.cs` | 40 | Normalization of `+`, `-`, unary `-`, parens, literals, identifiers; commutative equality; decimal literal handling via `Rational`; depth-bound termination at 8; non-normalizable → null; algebra (Add/Subtract/Negate); property tests for associativity/commutativity/distributivity over scalar; constant-only form; single-term form; zero-coefficient cancellation (`A - A` → empty terms, constant 0); `long.MaxValue` GCD stress. |
| `RationalTests.cs` | 23 | Construction; GCD normalization; `INumber<Rational>` compliance; arithmetic; equality and GetHashCode consistency; zero denominator throws; decimal literal round-trip; `long.MinValue` negation overflow; multiplication overflow; division by zero throws. |
| `ProofContextTests.cs` | 33 | Query API contracts (`IntervalOf`, `SignOf`, `KnowsNonzero`, `KnowsNonnegative`); copy-on-write semantics; `Child()` parenting; `Dump()` shape; **reassignment kill loop — covers (a) relational-fact invalidation (every C-Nano scenario mirrored verbatim) AND (b) equality-fact invalidation (new in this PR: `WithAssignment` + `_exprFacts` kill loop scanning `LinearForm.Terms`)**; `IntervalOf` on fully opaque expression → Unknown (never throws). |
| `ProofEngineCompoundDivisorTests.cs` | 17 | Gap 1: `(A+1)-B` with `rule A>B`; `A-(B+C)` with `rule A>B+C`; `Total-Tax-Fee` with `rule Total>Tax+Fee`; mixed compound shapes; regression: bare `A-B` (ex-C-Nano); scalar-multiple: `Y / (3*A - 3*B)` with `rule A > B` proves; negative-k does NOT prove. **Includes `Check_ConditionalHullIncludesZero_C93`: `Y / (if A > B then A - B else 0)` — hull is `[0, +∞)` → C93 fires correctly.** |
| `ProofEngineSumOnRhsTests.cs` | 12 | Gap 2: `rule Total > Tax + Fee` proves `Amount/(Total-Tax-Fee)`; `rule A >= B + C`; mixed-sign summands; function-call summands fall back gracefully (no crash, conservative). |
| `ProofEngineComputedFieldTests.cs` | 18 | Gap 3: `set Net = Gross-Tax; check Amount/Net` with `rule Gross>Tax`; multi-step chains (`set A = X+Y; set B = A-1; check Z/B`); reassignment invalidates derived equality fact; non-normalizable RHS falls back conservatively; cross-event isolation soundness anchor. |
| `ProofEngineTransitiveClosureTests.cs` | 17 | Gap 4: 2-step / 3-step / 4-step chains; strict/non-strict matrix exhaustive (all four combinations); cycle/self-contradiction silently dropped; depth cap honored; fact-count cap honored; disconnected graph. |
| `ProofContextScopeTests.cs` | 12 | Gap 5: global facts visible in event scope; event facts NOT visible in sibling event; ensure-derived facts cross via `BuildEventEnsureNarrowings`; rule narrowings remain global; bare→dotted rewriter regression. **Explicitly covers all three ensure variants: `on Event ensure` (arg-only — cannot reference fields); `in State ensure` (suppresses C93 in receiving-state events); `to State ensure` (does NOT suppress C93 in the receiving event — use `in`, not `to`).** |
| `ProofEngineSoundnessInvariantTests.cs` | 15 | Concrete value enumeration over `{-100, -1, -0.5, 0, 0.5, 1, 100}` for free variables; saturation boundary test with `long.MaxValue`-scale coefficients; FP edge cases. |
| `ProofEngineUnsupportedPatternTests.cs` | 4 | Soundness anchors: C93 correctly fires on non-linear divisors, function-opaque divisors, inequality-without-ordering, and modulo. |
| `ProofDiagnosticAssessmentTests.cs` | ~15 | Shared assessment model: contradiction vs unresolved vs proven across C76/C92/C93; truth-based C92 fires on provably-zero identifiers and expressions (not just literal zero); C93 restricted to unresolved obligation; code actions consume structured metadata (not message parsing). |
| `ProofEngineConstraintEnforcementTests.cs` | ~12 | C94: set literal exceeds max, set negative with nonneg, set zero with positive, set below min, compound exceeds max, partial overlap no diagnostic, unknown no diagnostic, state action, computed field, nullable field, event arg range, combined min/max. |
| `ProofEngineRuleEnforcementTests.cs` | ~8 | C95: min vs less-than, positive vs lte-zero, max vs greater-than; C96: nonneg vs gte-0, min vs gte-lower, max vs lte-higher; synthetic rule exclusion. |
| `ProofEngineGuardEnforcementTests.cs` | ~12 | C97: guard exceeds max, guard below min, guard contradicts rule, complex guard no diagnostic, edit guard dead, ensure guard dead; C98: positive vs gt-0, nonneg vs gte-0, min vs gte-lower, not vacuous no diagnostic. |
| `ProofEngineReachabilityTests.cs` | ~3 | C50 sharpened (all guards dead), C51 sharpened (non-reject rows dead), C48 sharpened (incoming rows dead). |
| `ProofEngineConditionalCompositionTests.cs` | ~5 | Conditional + relational composition: both branches exclude zero (no C93), else branch includes zero (C93), guard-narrowed relational fact propagates into then-branch IntervalOf. |
| **Total new** | **~270+** | |

**Hull equal-bound inclusivity** (`LowerInclusive = a.LowerInclusive || b.LowerInclusive` when bounds are equal) is existing behavior covered by `NumericIntervalTests.cs` Hull tests. No new test obligation for this PR.

### Performance Acceptance Targets (D8, team review 2026-04-18)

| Target | Budget | Measurement |
|---|---|---|
| Compile-time regression | ≤5% across all 25 sample files | Benchmark `samples/*.precept` before/after using language server timing output |
| Hover latency | ≤50ms for proof section | Measure `IntervalOf` + `ToNaturalLanguage()` + attribution on representative expressions |

Pre-merge manual check. Candidate for CI automation, not in scope for this PR.

### Cross-Surface Consistency (D9, team review 2026-04-18)

Hover, diagnostics, and MCP MUST produce the same proof facts for the same expression in the same proof context. This is structurally guaranteed by the shared assessment model:

- All three surfaces call `IntervalOf()` on the same `ProofContext` → same `ProofResult`
- All three surfaces format via `ToNaturalLanguage()` → same phrasing
- All three surfaces read `ProofAttribution.Sources` → same "from:" lines

**Acceptance criterion:** At least 1 sample file (created in Commit 15) must demonstrate an expression where the hover proof section, the diagnostic message, and the MCP `proof` key all display consistent interval + attribution data. This is verified by inspection during Commit 15, not by automated test.

### Diagnostic Samples (S5, team review 2026-04-18)

The `test/integrationtests/diagnostics/` folder is a living catalog of proof engine scenarios — user-facing reference files that show authors what the engine catches, what messages it produces, and how to fix the code. Each file is self-contained, demonstrates both the triggering pattern and the remediation, and uses comments to explain the proof engine's reasoning.

**Maintenance rule:** When a PR adds, changes, or removes a proof-backed diagnostic, the same PR must add or update the corresponding sample in `test/integrationtests/diagnostics/`. This is codified in `CONTRIBUTING.md` § Diagnostic Samples.

**Initial catalog (Commit 15):**

| Sample file | Diagnostics | Scenario |
|---|---|---|
| `divisor-safety.precept` | C92, C93 | Literal zero, identifier without proof, compound expression, three C93 message variants, remediation paths |
| `sqrt-safety.precept` | C76 | Negative argument, unproven non-negative, remediation via `nonnegative` |
| `assignment-constraints.precept` | C94 | Provably outside constraint interval, partial overlap (no diagnostic), remediation |
| `contradictory-rules.precept` | C95 | Rule vs field constraint contradiction |
| `vacuous-rules.precept` | C96 | Rule always true given constraints |
| `dead-guards.precept` | C97 | Guard always false, reachability sharpening impact |
| `vacuous-guards.precept` | C98 | Guard always true |
| `conditional-composition.precept` | C92, C93 | Guard-narrowed branches, relational narrowing, Hull semantics |

**Drift prevention tests:** Each diagnostic sample has a corresponding `[Theory]` test case in `DiagnosticSampleDriftTests.cs` that compiles the file via `PreceptCompiler.CompileFromText()` and asserts:

1. **Header coverage** — the sample's `# Demonstrates:` header exactly matches the distinct diagnostic codes declared by its `# EXPECT:` annotations.
2. **Exact diagnostic contract** — every `# EXPECT:` annotation declares the code, severity, message match mode, visible message text, and emitted `Line` / `Column` / `EndColumn` values for one diagnostic.
3. **No extras at any severity** — the actual compilation result must match the declared expectation set exactly. No undeclared errors, warnings, or hints are allowed.

The `# Demonstrates:` header remains the summary contract, but the line-by-line `# EXPECT:` annotations are the precise executable contract. This keeps the sample file itself as the single source of truth for what the author should see in the editor, including squiggle placement.

This mirrors the existing `SampleFile_ParsesAndCompilesClean` pattern for regular samples, but inverts the assertion: regular samples must compile clean, diagnostic samples must produce their declared diagnostics.

---

## Implementation Notes (PR #108)

Implementation history and archaeology for the unified proof engine, PR #108 (`feature/issue-106-divisor-safety`). This section preserves the evolutionary context — what was superseded, what was deleted, and what gaps were closed — for future contributors.

### Superseded Approaches

- **C-Nano subsumed.** The C-Nano `InferSubtractionInterval` special case (`6fbb315`) is deleted. Every C-Nano test scenario passes via `ProofContext.IntervalOf` + `LinearForm` — the unified path is strictly more powerful.
- **`TryInferRelationalNonzero` deleted.** The bespoke relational pattern-matcher is subsumed by `ProofContext.KnowsNonzero`, which calls `IntervalOf`.
- **`InferSubtractionInterval` deleted.** The bespoke subtraction shape-matcher is subsumed by `IntervalOf`'s general relational lookup path.

### Gap Closure

The original type checker handled single-identifier divisors. Compound expressions fell through with no diagnostic under Principle #8 conservatism. Five gaps were identified and closed:

1. **Compound subtraction operands** (`(A+1)-B`, `Total-Tax-Fee`): closed by `LinearForm.TryNormalize` reducing the divisor to a canonical form; constant-offset scan matches the stored relational fact.
2. **Sum-on-RHS rules** (`rule Total > Tax + Fee`): closed by `WithRule` calling `TryNormalize` on both sides, storing facts keyed by `LinearForm(lhs) − LinearForm(rhs)`.
3. **Computed-field intermediaries** (`set Net = Gross-Tax; Amount/Net`): closed by `WithAssignment` calling `IntervalOf(rhs)` and storing the result in `_fieldIntervals[target]`.
4. **Transitive closure** (`rule A>B`, `rule B>C` → `A-C`): closed by `RelationalGraph` bounded BFS.
5. **Cross-event scope leakage**: closed structurally — `EventProofContext.Mutations` never promoted to `GlobalProofContext`.

### Migration Notes

- **All string markers eliminated.** `$ival:`, `$positive:`, `$nonneg:`, `$nonzero:`, `$gt:`, `$gte:` marker conventions replaced by typed stores. `_symbols` retained only for non-proof symbol/type duties.
- **`BuildEventEnsureNarrowings` rewritten.** Bare→dotted string surgery replaced with structural `Rekey()` transform on typed `RelationalFact` records, eliminating the multi-field marker bug class (`$gt:A:B` → `$gt:Go.A:Go.B`) by construction.
- **Scope split.** Single `ProofContext` class replaced by `GlobalProofContext` (immutable) / `EventProofContext` (per-event child via `Child()`), making scope isolation a type-level guarantee.

### Archaeology

- `TryInferRelationalNonzero` required `StripParentheses` — parenthesized divisors like `(A - B)` wrapped in `PreceptParenthesizedExpression` failed the `is not PreceptBinaryExpression` check. Irrelevant after deletion, but `StripParentheses` remains in other consumers.
- `NumericInterval.Multiply` zero-interval fast path: `(0,∞) × [0,0]` produces `NaN` via IEEE 754 `∞*0`. Added early return for `[0,0]` inputs (still present).
- Modulo interval tightened: non-negative dividend + positive divisor → result ∈ `[0, |B|)`.
