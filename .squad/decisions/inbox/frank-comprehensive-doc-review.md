# Comprehensive Language/Compiler Doc Review — Flagged Issues

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-07T22:36:33-04:00

---

## 1. philosophy.md — DO NOT EDIT (6 Aspirational Claims Stated as Fact)

Per standing rule, I'm flagging these for Shane's review rather than editing philosophy.md directly.

### 1a. Runtime Operations Stated as Complete

**Lines 7, 17-20:** The four operations (CreateInstance, Inspect, Fire, Update) are described in present tense as working features. In reality:
- `Precept.Create()` → `TODO R4: implement creation pipeline`
- `Version.Fire()` → signatures exist, implementation pending D8/R4
- `Version.InspectFire()` → throws `NotImplementedException`
- `Version.Update()` → signatures exist, implementation pending D8/R4

**Recommendation:** Either (a) add a "current implementation status" callout at the top of philosophy.md, or (b) reframe the doc as a specification/vision rather than a description of current state. The philosophy's role as a *normative* document (what Precept IS) vs a *descriptive* one (what's built today) should be explicit.

### 1b. State Reachability Proof Stated as Fact

**Line 51:** "the compiler proves the business process itself is structurally sound" — GraphAnalyzer returns empty diagnostics unconditionally (`TODO Phase 3`).

### 1c. Expression Safety Proof Stated as Fact

**Line 53:** "Division by zero, arithmetic overflow, empty collection access — these are compile-time impossibilities." ProofEngine is completely unimplemented.

### 1d. Full Inspectability Stated as Fact

**Line 56:** "At any point, you can preview every possible action" — Inspect methods throw `NotImplementedException`.

### 1e. Stateless Precepts Stated as Fact

**Line 39:** "a stateless precept provides the same field declarations, rules, and constraint enforcement" — TypedEditDeclaration deferred pending D24.

### 1f. Restore Operation Not Mentioned

Code has a `Restore` operation (reconstitute persisted data with revalidation). Philosophy lists only four operations. If Restore is a first-class operation, philosophy should acknowledge it.

---

## 2. graph-analyzer.md — Design Issues (4)

### 2a. Domain Knowledge Claims Overstated

**Lines ~250-287:** Doc claims "State modifier semantics are defined in the Modifiers catalog, not hardcoded in the analyzer." But the spec hardcodes four modifier checks:
- `if (!stateMeta.AllowsOutgoing) CheckNoOutgoingEdges(...)`
- `if (stateMeta.PreventsBackEdge) CheckNoBackEdges(...)`
- `if (stateMeta.RequiresDominator) CheckDominatesTerminal(...)`
- Plus implicit reachability check

**Assessment:** The implementation pattern is correct — it reads catalog metadata to decide which checks to run. But the rhetoric overstates it. The analyzer DOES have structural knowledge of what `AllowsOutgoing`, `PreventsBackEdge`, and `RequiresDominator` mean (it provides the algorithms). Fix the claim to say: "Modifier applicability is catalog-driven; graph algorithms are generic machinery that reads metadata flags."

**Severity:** Minor wording. Can be fixed directly if Shane agrees.

### 2b. Incomplete SemanticIndex Input Specification

**Lines ~68-76:** The input table lists only States, TransitionRows, and StateHooks. But the graph analyzer also needs:
- **Events** — for event coverage analysis (§6.5)
- **Fields** — if computing constraint influence for proof forwarding

**Action needed:** Expand the input table before implementation begins.

### 2c. Proof Forwarding Contract Underspecified

**Lines ~99-104:** `StateGraph` output includes `ProofForwardingFact[]` but the doc doesn't specify what the proof engine must read from these facts. No forward reference to proof-engine.md §5.3.

**Action needed:** Add explicit contract listing what fact types the proof engine consumes.

### 2d. Missing Structural Assumptions

The doc doesn't state upfront that the graph analyzer assumes:
- Exactly one state is marked `initial` (guaranteed by TypeChecker)
- All state modifiers are resolved by the TypeChecker (no incomplete modifiers)

**Action needed:** Add a "Preconditions / Structural Invariants" section.

---

## 3. proof-engine.md — Design Issues (3)

### 3a. Proof Strategy Count Contradiction

proof-engine.md §7 header claims "Four Proof Strategies" but compiler-and-runtime-design.md §8 describes only three:
- Modifier proof (field modifier chain output bounds)
- Guard-in-path proof (guard expression establishes constraint)
- Flow narrowing (guard in same transition row narrows type state)

**Action needed:** Reconcile. What is the fourth strategy? Or is one of the three misnamed?

### 3b. Constraint Influence Sourcing Unclear

**Lines ~242-260:** Doc promises `ConstraintInfluenceEntry` records but doesn't specify their source. SemanticIndex already carries `ConstraintFieldRefs` with pre-computed field references. The proof engine should read these rather than re-walking constraint expressions.

**Action needed:** State explicitly: "Proof engine reads `SemanticIndex.ConstraintRefs` and transforms into `ConstraintInfluenceEntry`."

### 3c. Strategy Discharge Pseudocode Missing

For each of the proof strategies, the doc provides enum names and brief comments but no algorithm pseudocode. This is insufficient for implementation — George would have to design the algorithms himself, which risks architectural deviation.

**Action needed:** Add pseudocode for each strategy's discharge predicate before implementation begins.

---

## 4. catalog-system.md — Fixed Directly (See Commit)

The following were fixed in this review pass:

1. **AccessModifierMeta member count:** 4→3. `modify` is the construct verb, not a ModifierKind member. The 3 actual members are Write (editable), Read (readonly), Omit.
2. **ModifierKind total count:** 28→29. Code has 29 enum members (15+7+1+3+3).
3. **Actions table:** Was showing only 8 of 15 ActionKind members. Added all 7 new collection actions (append, appendby, insert, removeat, put, enqueueby, dequeueby) with correct ApplicableTo, SyntaxShape, and ProofRequirements.
4. **Actions ApplicableTo corrections:** `add` was `[Set]`, now `[Set, Bag]`. `remove` was `[Set]`, now `[Set, Bag, List, Lookup]`. `dequeue` was `[Queue]`, now `[Queue, QueueBy]`. `clear` expanded to include Bag, List, QueueBy. AllowedIn corrected from `EventDeclaration` only to all action contexts.

## 5. precept-grammar.md — Fixed Directly (See Commit)

1. **ExpressionFormKind count:** 13→14. The `InterpolatedString` form (enum value 14) was missing from the table and the count.
