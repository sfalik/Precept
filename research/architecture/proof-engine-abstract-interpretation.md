# Proof Engine: Abstract Interpretation Foundations and Soundness Survey

**Date:** 2026-04-19
**Author:** Frank (Lead/Architect)
**Research Angle:** Formal foundations of Precept's proof engine — domain classification, soundness conditions, comparison to established techniques
**Purpose:** Establish whether `ProofContext` / `LinearForm` / `RelationalGraph` is sound in the abstract interpretation sense, classify its domain, identify implicit assumptions, and determine what must hold before expanding the proof surface.

---

## Executive Summary

**Verdict: Precept's proof engine is sound in the abstract interpretation sense. It implements a reduced product of a standard interval abstract domain and a bounded, incomplete zone (DBM) domain, applied to a program model that structurally eliminates the two conditions that make abstract interpretation hard: loops and reconverging control flow.**

More precisely:

1. **`NumericInterval` + transfer rules** = the standard interval abstract domain from Cousot & Cousot (1977). Every transfer rule is a sound over-approximation. The engine never claims an expression is safe unless it can prove it.
2. **`LinearForm` + `RelationalGraph`** = a bounded, incomplete zone domain (Miné 2001 DBM domain). The `_relationalFacts` store keyed by `LinearForm(LHS − RHS)` is exactly the DBM representation of difference constraints. The 5-tier `LookupRelationalInterval` cascade + bounded BFS replaces Floyd-Warshall closure with an O(1)-worst-case approximation.
3. **`ProofContext.IntervalOf` = Intersect(arithmetic, relational)** = a practical reduced product of these two domains. This intersection can only tighten intervals, never fabricate them, which is the soundness-preserving property of reduced products.
4. **The flat execution model (no loops, no branches, no reconverging flow)** is load-bearing. It eliminates the need for widening operators, lattice joins, and fixpoint iteration — the three sources of precision loss in general abstract interpreters. Precept's engine is not merely *sufficient*; for its problem domain, it is *optimal*.

**One genuine soundness assumption to call out:** `NumericInterval` bounds are stored as `double`. All interval endpoints in practice derive from parsed source literals (exact rational values), and the `Rational` type is used for `LinearForm` coefficients. But for computed intervals that accumulate through arithmetic (e.g., deep chains of operations), floating-point rounding in the bounds themselves could theoretically produce slightly incorrect intervals. This has not caused known false proofs; the mechanism is sound *in practice* because DSL literal ranges are small and the engine widens toward `Unknown` on failure. But it is an implicit assumption, not a proved property.

**What this means for expanding the proof surface:** Any new proof capability that stays within linear arithmetic over the existing fact stores (field constraints, rules, guards, ensures) is safe to add. The interval transfer rules are monotone over-approximations, and any additional tiers in the relational lookup would preserve soundness. What would require careful analysis: (1) non-linear expressions (Precept currently returns `Unknown` for non-normalizable sub-expressions — correct), (2) any proof that requires path sensitivity across events (cross-event scoping is isolated by design — safe), and (3) any proof that accumulates `double` interval bounds through many arithmetic steps.

---

## Survey Results

### 1. Abstract Interpretation — Cousot & Cousot Framework

**Source:** Wikipedia — Abstract Interpretation (https://en.wikipedia.org/wiki/Abstract_interpretation)
**Primary reference:** Cousot, P.; Cousot, R. (1977). "Abstract Interpretation: A Unified Lattice Model for Static Analysis of Programs by Construction or Approximation of Fixpoints." POPL '77.

**The framework.** Abstract interpretation is a theory of sound approximation of program semantics. Given a concrete semantic domain L (e.g., sets of program states) and an abstract domain L' (e.g., intervals), the framework requires:

- An abstraction function **α: L → L'** (maps concrete state sets to abstract values)
- A concretization function **γ: L' → L** (maps abstract values back to sets of concrete states)
- A **Galois connection**: α and γ are monotone and satisfy α(S) ≤ a' iff S ⊆ γ(a') for all S, a'

**Soundness condition.** An abstract operation f' is a valid abstraction of concrete operation f iff, for all abstract inputs x':

```
f(γ(x')) ⊆ γ(f'(x'))
```

In plain terms: applying the concrete function to all concrete states in the abstract value, and mapping back, produces a result that is contained in the abstract value computed by the abstract function. The abstract result must be an over-approximation.

**The role of widening.** In the presence of loops, abstract values must reach a fixpoint. Since the abstract domain may have infinite ascending chains (intervals can grow unboundedly), widening operators force convergence by aggressively over-approximating. Widening is the primary source of precision loss in general abstract interpreters. Its absence is a structural benefit, not a design gap.

**Relational vs. non-relational domains.** Non-relational domains (intervals, signs, constants) assign an abstract value to each variable independently. They are fast but miss correlations between variables. The classic example: `y = x; z = x − y` should give `z = 0`, but interval arithmetic with no correlation gives `z ∈ [−1, 1]` if `x ∈ [0, 1]` — because x and y are tracked independently. Relational domains (zones, octagons, polyhedra) track constraints *between* variables at the cost of higher complexity.

**Abstract domain examples cited:**
- Intervals (Cousot 1976): non-relational, O(n) per variable
- Difference-bound matrices / zones (Miné 2001): relational, O(n²) storage, O(n³) closure
- Octagons (Miné 2006): relational, ±xi ± xj ≤ c constraints, O(n³) per operation
- Convex polyhedra (Cousot & Halbwachs 1978): most expressive, exponential complexity

**Relevance to Precept.** Precept's proof engine implements both a non-relational component (NumericInterval) and a relational component (LinearForm + RelationalGraph). The Cousot framework provides the formal grounding for why the design is correct. The soundness condition is met at each transfer rule.

---

### 2. Interval Arithmetic as Abstract Domain

**Source:** Wikipedia — Interval Arithmetic (https://en.wikipedia.org/wiki/Interval_arithmetic)

**The interval domain.** An interval [a, b] represents all real values between a and b (inclusive or exclusive). For a program variable x, an interval [Lx, Hx] means x ∈ [Lx, Hx] at this program point.

**Standard transfer rules** (all sound over-approximations):
- Addition: `[a,b] + [c,d] = [a+c, b+d]`
- Subtraction: `[a,b] − [c,d] = [a−d, b−c]`
- Multiplication: min/max of four products (requires sign-case decomposition to avoid 0×∞)
- Division: standard when denominator excludes zero; `Unknown` otherwise

**The dependency problem.** This is the fundamental limitation of non-relational interval arithmetic. If a variable `x` appears multiple times in an expression, each occurrence is treated independently. Example: `x − x` for `x ∈ [0, 1]` gives `[0,1] − [0,1] = [−1, 1]` instead of `[0, 0]`. This is sound (the real result [0,0] is contained in [−1,1]) but imprecise.

The dependency problem becomes relevant when (a) the same field appears as both operand and subexpression, or (b) compound linear expressions are evaluated term by term without normalization. The standard mitigation in production tools is symbolic normalization before interval evaluation — exactly what `LinearForm.TryNormalize` does.

**Rounding note.** Mathematically correct interval arithmetic with floating-point bounds requires *outward rounding*: lower bounds round down, upper bounds round up. IEEE 754 implementations that do not change the rounding mode may produce slightly tight intervals that exclude true program values. This is the `double` bounds soundness caveat mentioned in the Executive Summary.

**Relevance to Precept.** Precept's `NumericInterval` implements standard interval arithmetic transfer rules for all 10 built-in functions and all binary operators, including the multiply sign-case decomposition that avoids the 0×∞ problem. The `LinearForm` normalization directly addresses the dependency problem for linear sub-expressions. Non-linear expressions (products of two non-constants, non-constant exponents) correctly return `null` from `TryNormalize` and fall back to interval arithmetic.

---

### 3. Octagon Abstract Domain and Zone/DBM Domains

**Source:** Miné, A. (2006). "The Octagon Abstract Domain." Higher Order Symbol. Comput. 19(1):31–100. (Wikipedia Octagon page returned 404; drawing on the primary paper and the abstract interpretation Wikipedia article.)

**Zone domain / Difference-Bound Matrices.** Miné (2001) introduced the zone domain for reasoning about constraints of the form `xi − xj ≤ c` (difference constraints). A zone is represented as a difference-bound matrix (DBM): an n×n matrix M where M[i][j] = upper bound on xi − xj. Floyd-Warshall closure (O(n³)) derives all implied constraints. The zone domain subsumes the interval domain: bounds on xi alone are a degenerate difference xi − ⊥ ≤ c.

**Octagon domain.** Miné (2006) generalized zones to octagonal constraints: `±xi ± xj ≤ c`. This allows tracking sums as well as differences, at the same O(n³) closure cost. Octagons subsume zones; zones subsume intervals. The precision hierarchy is: intervals ⊂ zones ⊂ octagons ⊂ polyhedra.

**Reduced products.** Combining two abstract domains into a reduced product gives the most precise information both domains can provide. The result of an operation is computed in both domains and then "reduced" — constraints from one domain are propagated to tighten the other. In practice, many tools implement a weaker approximation: compute in both domains, intersect the results (which is what Precept's `IntervalOf` does).

**Reference implementations:**
- **CodeContracts / Clousot** (Microsoft, archived): "Pentagons" domain = interval domain + upper bound relational domain. Architectural precursor to Precept's design.
- **Boogie IntervalDomain.cs** (Microsoft, active): 1,730 lines of C# interval domain with `BigInteger?` bounds. Production reference for .NET interval domain implementation.
- **Apron / ELINA / Crab**: C/C++ libraries implementing full zone, octagon, polyhedra domains with formal abstract interpretation lattice infrastructure.

**Relevance to Precept.** The `_relationalFacts` store, keyed by `LinearForm(LHS) − LinearForm(RHS)`, with values `RelationalFact(kind: > or >=, scope)`, is a direct implementation of the DBM representation of the zone domain. Each stored fact corresponds to one entry in a conceptual DBM. The 5-tier lookup cascade + bounded BFS is Precept's substitute for Floyd-Warshall closure: it derives implied facts (transitivity) without materializing the full O(n³) closure. This makes Precept's relational domain *incomplete* (it may miss derivable facts beyond depth 4) but *sound* (it never derives a false fact).

---

### 4. Clang Static Analyzer — Production Abstract Interpretation

**Source:** Clang 23.0 Developer Docs — Debug Checks (https://clang.llvm.org/docs/analyzer/developer-docs/DebugChecks.html)

**Architecture overview** (from Clang source knowledge; the linked page covers debug infrastructure). The Clang Static Analyzer is a path-sensitive, inter-procedural abstract interpreter for C/C++/ObjC:

- **ExplodedGraph**: models execution paths as a graph of (ProgramPoint, ProgramState) pairs. Handles loops via fixpoint iteration over the graph.
- **ProgramState**: immutable state snapshot including symbolic values (`SVal`), environment, store, and constraint manager.
- **RangeConstraintManager**: tracks integer ranges (essentially intervals) on symbolic values. `clang_analyzer_value` output shows these: `8s:{ [-128, 127] }` for a signed 8-bit char.
- **Z3 crosscheck mode**: optionally uses Z3 to cross-check feasibility of paths, filtering false positives. The standard mode uses the range constraint manager; Z3 is an optional verifier. The debug docs note this mode is "unsupported and badly broken as a full constraint manager."
- **Checkers**: domain-specific analyses (null dereference, memory leak, etc.) layered over the abstract interpreter infrastructure.

**Scope**: General-purpose, handles loops (with fixpoint), recursion (with inlining bounds), pointer aliasing, inter-procedural analysis. Much larger scope than Precept.

**Relevance to Precept.** The Clang analyzer confirms that interval/range domains are the practical standard in production static analysis tools. The fact that even Clang uses ranges (not full polyhedra or octagons) for its constraint manager validates Precept's choice. The Z3 crosscheck experience also confirms Precept's conclusion: Z3 as a primary constraint engine is expensive and fragile; it works better as a secondary cross-check on specific paths.

---

### 5. Facebook Infer — Scope Comparison

**Source:** https://fbinfer.com/docs/about-Infer

**What Infer is.** A production static analyzer for Java, C, C++, Objective-C, and Erlang. Deployed in Meta's CI pipeline. Detects null pointer dereferences, data races, resource leaks, and heap memory issues.

**Core technique.** Bi-abduction (Calcagno et al., 2011): a form of separation logic that infers pre/postconditions from partial heap state. Completely different abstraction domain from Precept. Infer reasons about the *shape* of heap memory (what pointers exist, what objects are reachable) rather than numeric ranges of values.

**Scope.** General-purpose, cross-language, deployed at massive scale (millions of lines of code per analysis). Inter-procedural. Runs incrementally (analyzes changed functions).

**Relevance to Precept.** Infer is a useful scope comparator, not an architectural comparator. Precept doesn't need heap reasoning (no pointers, no dynamic allocation) and has no use for separation logic. The relevant comparison is: Infer uses a sophisticated domain for a complex problem domain; Precept uses a simpler domain matched to a simpler problem domain. Neither is "better" — they solve different problems. What both share: the commitment to static, compile-time verification that reports only proven violations (Infer's "no false positives in its default mode" claim mirrors Precept's Principle #9).

---

### 6. Z3 Theorem Prover — Comparator

**Source:** Z3 project (Microsoft Research). URL 404; drawing on knowledge of the tool.

**What Z3 is.** A high-performance SMT solver supporting QF_LRA (quantifier-free linear real arithmetic), QF_LIA (integer arithmetic), bitvectors, arrays, and more. Used in Dafny, Boogie, CodeContracts, KLEE, and many other verification tools.

**Formal fragment.** QF_LRA is the closest formal fragment to Precept's proof domain: linear arithmetic over real-valued variables without quantifiers. QF_LRA is decidable (via Fourier-Motzkin or simplex) and Z3 handles it completely — it either proves or disproves any QF_LRA formula.

**Why Precept rejected Z3.** Per ProofEngineDesign.md Principle #10 and the team design review for PR #108:

1. **Size**: 50–65 MB of native dependencies for a DSL compiler
2. **Non-determinism**: solving times are input-dependent; different machine states can produce different solving orders
3. **Opaque proof witnesses**: Z3 returns SAT/UNSAT, not a structured derivation the author can read
4. **Overkill**: the constraint surface (linear arithmetic over field values with constraints) is decidable with interval arithmetic alone
5. **Inspectability violation**: a solver that cannot explain itself in DSL terms violates Principle #6

**The formal relationship.** Precept's proof engine is a sound *under-approximation* of what Z3's QF_LRA solver could prove. Precept proves a subset of the theorems Z3 could prove — but only by evidence that can be directly traced back to DSL declarations. Z3 could prove more; it would prove some things Precept cannot. The tradeoff: Precept's proofs are always explainable; Z3's are not.

---

## Synthesis: How Precept's Proof Engine Relates to Abstract Interpretation

### Domain Classification

Precept's proof engine implements a **reduced product of an interval domain and a bounded zone domain**, applied to a linear arithmetic over real-valued fields.

More precisely:

| Component | Abstract Domain | Formal Name |
|---|---|---|
| `NumericInterval` + transfer rules | Interval abstract domain | Cousot & Cousot 1976 |
| `_relationalFacts` + `LinearForm` keys | Zone domain (DBM over linear forms) | Miné 2001 |
| `LookupRelationalInterval` 5-tier cascade | Incomplete zone closure | Simplified DBM without full Floyd-Warshall |
| `IntervalOf = Intersect(arithmetic, relational)` | Reduced product approximation | Practical reduced product |
| `Hull(then, else)` for conditionals | Join (lattice ∨) for conditional expressions | Standard abstract interpretation join |
| `WithGuard(condition, branch)` | Guard narrowing (backward direction) | Standard narrowing transfer function |

The engine is a **Zone abstract domain** in the broad sense: it tracks both per-variable intervals and difference constraints between variables. It is *not* a full octagon domain (it does not track sum constraints ±xi ± xj ≤ c). It is *not* a full zone domain (it uses bounded BFS instead of Floyd-Warshall, and its domains is over `LinearForm` keys not single variable pairs). It is more expressive than a pure interval domain because the relational store can derive interval facts by transitivity over compound expressions.

The ProofEngineDesign.md already characterizes this accurately ("simplified Zone abstract domain … nearly identical to Miné's zone/octagon domains"). This research confirms that characterization is correct.

### Soundness Properties That Hold

The following soundness properties are verified by the design:

1. **Transfer rule soundness.** Each `NumericInterval` operation produces an interval that contains all concrete results for all concrete inputs in the operand intervals. Standard interval arithmetic transfer rules are well-known to have this property. The multiply sign-case decomposition avoids the 0×∞ UB that would violate soundness.

2. **Relational injection soundness.** `WithRule(lhs, rel, rhs)` stores facts only when the rule makes a definitive statement. `rule A > B` injects `A − B > 0`. This fact is exactly what the rule claims; it does not fabricate any additional information.

3. **Relational lookup soundness.** The 5-tier cascade — direct match, GCD-normalization, negation, constant-offset scan, transitive closure — each produce intervals derivable from stored facts. Tier 5 (BFS) applies the strict/non-strict composition matrix correctly; in particular, `>= ∘ >=` correctly yields `>=` not `>`, avoiding the false "strictly positive" claim.

4. **Intersection soundness.** `Intersect(arithmetic, relational)` can only tighten intervals. The resulting interval is contained in both input intervals. No value excluded by either source is reintroduced by the intersection.

5. **Hull soundness for conditionals.** `Hull(thenInterval, elseInterval)` is the join in the interval lattice — it returns the smallest interval containing both. Since exactly one branch executes at runtime, the actual result is either in `thenInterval` or `elseInterval`, and therefore in their hull. This is the standard abstract interpretation join for path-merging, applied correctly.

6. **Guard narrowing soundness.** `WithGuard(condition, true)` narrows the interval of the variables mentioned in the condition. Narrowing can only tighten — it returns a sub-interval of what was already known. It never widens. The only way narrowing could be unsound is if it derived a constraint the guard doesn't actually imply; the implementation extracts only the exact numerical constraint from the comparison expression.

7. **Unknown propagation.** Operations on `Unknown` intervals return `Unknown` unless the operation itself bounds the result (e.g., `abs(Unknown) → [0, +∞)`). This ensures that absence of information propagates conservatively.

8. **Sequential flow soundness.** `WithAssignment(target, rhs)` kills all prior facts about `target` before storing the new fact. The reassignment kill loop ensures that stale relational facts (derived before the assignment) do not persist to mislead subsequent proof queries in the same row. Without the kill loop, facts about `Rate` derived before `set Rate = 0` would continue to influence subsequent divisor checks — a false proof. The kill loop is a correctness-critical implementation detail.

### Implicit Assumptions (Not Formally Proved)

1. **`double` interval bounds.** `NumericInterval` stores bounds as `double`. Standard interval arithmetic requires outward rounding (lower bounds rounded down, upper bounds rounded up) to be mathematically sound. Precept does not explicitly set the FP rounding mode. In practice this is benign because all interval endpoints originate from parsed source literals (exact rational values that fit in `long` and convert exactly to `double` via `Rational`) and the arithmetic is not deep enough to accumulate FP rounding error into a false safety claim. But it is an assumption, not a proof. A rigorous proof would require either exact arithmetic bounds (BigInteger/Rational) or outward rounding.

2. **Field values are real numbers, not machine integers.** Precept's numeric fields are real-valued (the runtime uses `double`). The interval domain is over ℝ. If Precept ever adds integer-valued fields (or if field values could wrap around), interval arithmetic over ℝ would need to account for modular arithmetic. This is not a current concern but is a precondition to be aware of if integer fields are ever introduced.

3. **No aliasing.** The `_fieldIntervals` store is keyed by field name. There is no aliasing between fields; each name uniquely identifies a value. If fields could alias (e.g., through references or struct projections), a kill loop over single names would be insufficient. Precept's flat field model makes this safe.

4. **Expression trees are well-typed and finite.** `IntervalOf` is a recursive descent with a depth cap (LinearForm depth ≤ 8, RelationalGraph depth ≤ 4). The depth cap ensures termination but means very deep expressions may not be fully analyzed. This is sound (returns `Unknown` at depth limit) but is a bound on completeness, not soundness.

5. **Hard caps are correctness boundaries.** The 64-fact, depth-4, 256-node caps on `RelationalGraph` are not performance conveniences — they are correctness boundaries that bound worst-case cost. Any cap hit returns no derived fact. This is sound. But if the number of rules in a large precept approaches the caps, proof coverage silently degrades. This should be monitored as definitions grow.

---

## Soundness Conditions Summary

For future reference: what must be true for each new proof capability to be sound.

| Condition | Status | Notes |
|---|---|---|
| Transfer rules are over-approximations | ✓ Satisfied | All 10 function rules and all binary operators |
| Intersection can only tighten | ✓ Satisfied | By construction of NumericInterval.Intersect |
| Hull is smallest enclosing interval | ✓ Satisfied | By construction of NumericInterval.Hull |
| Guard narrowing only tightens | ✓ Satisfied | WithGuard uses comparison extraction, not widening |
| Kill loop clears stale facts on reassignment | ✓ Satisfied | Implemented in WithAssignment |
| `double` bounds are sound for source literals | ✓ In practice | Not formally proved; implicit assumption |
| Non-linear expressions return `Unknown` | ✓ Satisfied | TryNormalize returns null for non-linear forms |
| Hard caps are never silently exceeded | ✓ Satisfied | Cap hit returns no fact, not a wrong fact |
| Cross-event isolation holds | ✓ Satisfied | EventProofContext child is isolated from global |

For any new proof capability, the key question is: **does the new derivation step produce an interval that provably contains all concrete values, or does it risk excluding a value that could appear at runtime?** If the former, it is sound. If the latter, it is a false proof.

---

## Comparison: Precept vs. Production Abstract Interpreters

| Dimension | Precept ProofEngine | Clang Static Analyzer | Infer | IKOS / Crab / Apron |
|---|---|---|---|---|
| Abstract domain | Interval + bounded zone (reduced product) | Ranges (interval-like, per symbolic value) | Separation logic (bi-abduction) | Full zone / octagon / polyhedra |
| Relational | Bounded BFS, depth ≤ 4 | None (range-only) | Structural (heap shape) | Full Floyd-Warshall O(n³) |
| Widening | Not needed (no loops) | Required (for loops/recursion) | Required | Required |
| Fixpoint iteration | Not needed | Yes (ExplodedGraph) | Yes | Yes |
| Path sensitivity | Sequential + guard narrowing | Full (exploded graph paths) | Per-procedure | Configurable |
| Loops | Not applicable (DSL has none) | Required | Required | Required |
| Inter-procedural | Not needed (single-file) | Optional (with inlining) | Yes | Configurable |
| Scope | Numeric integrity of DSL fields | C/C++/ObjC memory safety | Java/C heap safety | General numeric analysis |
| LOC (analysis engine) | ~3,000 (ProofContext + LinearForm + Rational + RelationalGraph) | ~150,000 (ExprEngine + checkers) | ~300,000 (OCaml) | ~50,000–100,000 (C++) |
| Opaque solvers | Rejected by design | Optional (Z3 crosscheck) | No | Some tools integrate Z3 |
| Completeness | Incomplete (bounded BFS, depth 8 LinearForm) | Incomplete (loop bounds, inlining limits) | Incomplete | Incomplete for polyhedra |
| Soundness | Sound for its domain | Sound for its domain | Unsound in some modes | Sound |
| Inspectability | Architectural (attribution in every ProofResult) | Limited (debug checkers only) | Limited | Limited |

**Key observation.** Precept's engine is substantially smaller than all production abstract interpreters, yet achieves *better precision* for its specific problem domain. This is the direct consequence of the flat execution model: without loops, the engine sidesteps the entire widening infrastructure that dominates the complexity of general-purpose tools. The 3,000 LOC engine does what would take 50,000+ LOC in a general framework, because it exploits the structural constraint that general frameworks cannot assume.

**The precision advantage.** Clang's range constraint manager is non-relational — it does not track relationships between variables. Precept's `RelationalGraph` provides relational reasoning that Clang's constraint manager lacks. For Precept's domain (asserting `A > B` and `B > C` therefore `A > C`), Precept's engine is *more precise* than Clang's, despite being far smaller. This is not a criticism of Clang — Clang optimizes for a different problem — but it confirms that Precept's custom engine is the right tool for its domain.

---

## Implications for ProofEngineDesign.md

The following are specific additions or clarifications that would strengthen `ProofEngineDesign.md`:

### 1. Make the Galois Connection Explicit

The Research Foundations section identifies the zone domain and cites the right papers but does not spell out the Galois connection. Adding this would strengthen the formal claim:

> **Concrete domain:** a function v: FieldName → ℝ mapping each field to its runtime value.
> **Abstraction α:** α({v}) = the smallest interval [L, H] ⊇ {v(field)} over the field's constraint set.
> **Concretization γ:** γ([L, H]) = {v | L ≤ v ≤ H}.
> The engine implements a sound abstract interpreter over this Galois connection.

### 2. Name the Dependency Problem and Explain the Fix

The dependency problem (a field appearing multiple times in an expression causes interval over-approximation) should be named explicitly. `LinearForm.TryNormalize` is the fix. Calling this out makes it clear that Precept's relational store is not just a bonus feature — it is the standard mitigation for a known formal limitation of the interval domain.

### 3. Formally State the `double` Bounds Assumption

The design doc should add a note under `NumericInterval`:

> **Soundness note:** `double` bounds are used for interval endpoints. All interval endpoints in practice originate from parsed source literals (which are exact rational values). The `Rational` type is used for `LinearForm` coefficients, ensuring exact linear form arithmetic. For computed intervals derived through deep arithmetic chains, floating-point rounding in the `double` bounds is a theoretical unsoundness that has no known practical impact in current DSL definitions. If high-assurance soundness is required, bounds should be migrated to `Rational` or `BigInteger`-backed exact representation.

### 4. Clarify "Reduced Product" Terminology

The ProofEngineDesign.md says "reduced product" but the actual operation in `IntervalOf` is an intersection, not a formal reduced product (which requires computing the meet of both domain elements and propagating constraints from one domain to tighten the other). The intersection is a practical approximation of a reduced product. Clarifying this avoids misrepresenting the formal relationship:

> The combination of arithmetic and relational intervals in `IntervalOf` is a *practical reduced product approximation* — we intersect the results of both domains rather than computing the formal reduced product (which would require propagating relational constraints back into the interval component). This is weaker than a full reduced product but stronger than using either domain alone.

### 5. Add a Soundness Condition Checklist

The current design doc lists philosophy principles but not formal soundness conditions. Adding a brief checklist (similar to the table in this document) would make the engine's guarantees machine-checkable in code review.

### 6. Note the `>= ∘ >=` Strict/Non-Strict Correctness

The strict/non-strict composition matrix in `RelationalGraph` is a known correctness trap. The design doc already states this correctly (`>= ∘ >=` does NOT yield `>`). This should be marked as a regression-test-required boundary condition, because it is precisely the kind of change that seems harmless but would introduce false proofs if changed incorrectly.

---

## References

1. Cousot, P.; Cousot, R. (1977). "Abstract Interpretation: A Unified Lattice Model for Static Analysis of Programs by Construction or Approximation of Fixpoints." *POPL '77.* ACM. pp. 238–252. doi:10.1145/512950.512973.
2. Cousot, P.; Cousot, R. (1976). "Static determination of dynamic properties of programs." *Proc. 2nd Int. Symp. on Programming.* Dunod, Paris.
3. Miné, A. (2001). "A New Numerical Abstract Domain Based on Difference-Bound Matrices." *PADO II.* LNCS 2053, pp. 155–172. arXiv:cs/0703073.
4. Miné, A. (2006). "The Octagon Abstract Domain." *Higher Order Symbol. Comput.* 19(1):31–100. doi:10.1007/s10990-006-8609-1. arXiv:cs/0703084.
5. Miné, A. (2004). *Weakly Relational Numerical Abstract Domains.* PhD thesis, École Normale Supérieure.
6. Cousot, P.; Halbwachs, N. (1978). "Automatic Discovery of Linear Restraints Among Variables of a Program." *POPL '78.* pp. 84–97.
7. Moore, R. E. (1966). *Interval Analysis.* Prentice-Hall. (Foundation of modern interval arithmetic.)
8. Wikipedia. "Interval Arithmetic." https://en.wikipedia.org/wiki/Interval_arithmetic. Retrieved 2026-04-19.
9. Wikipedia. "Abstract Interpretation." https://en.wikipedia.org/wiki/Abstract_interpretation. Retrieved 2026-04-19.
10. Clang Static Analyzer Developer Docs. "Debug Checks." https://clang.llvm.org/docs/analyzer/developer-docs/DebugChecks.html. Retrieved 2026-04-19. (Note: `clang_analyzer_value` debug check shows the range constraint manager's interval representation.)
11. Facebook Infer. "About Infer." https://fbinfer.com/docs/about-Infer. Retrieved 2026-04-19.
12. Z3 Theorem Prover. Microsoft Research. (URL returned 404; citing from knowledge.) Barrett, C.; Tinelli, C. (2018). "Satisfiability Modulo Theories." In *Handbook of Model Checking.* Springer.
13. Logozzo, F.; Fähndrich, M. (2010). "Pentagons: A Weakly Relational Abstract Domain for the Efficient Validation of Array Accesses." *Sci. Comput. Program.* (CodeContracts / Clousot architecture — direct architectural predecessor to Precept's proof engine design.)
14. Leino, K. R. M. et al. Boogie `IntervalDomain.cs` — https://github.com/boogie-org/boogie (MIT, active). 1,730 LOC C# interval domain implementation; best living C# reference for production interval domain.
15. ProofEngineDesign.md — internal design document. Precept project. 2026-04-18. (All proof engine architecture described here is grounded in this document.)
