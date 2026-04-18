## Core Context

- Owns the core DSL/runtime: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects the fire/update/inspect pipeline contract and keeps runtime behavior aligned with docs, tests, and MCP output.
- Historical summary (pre-2026-04-13): led runtime feasibility and implementation analysis for guarded declarations, event hooks, computed fields, and verdict-modifier semantics.

## Learnings

- Runtime/design gaps should be separated cleanly from philosophy decisions; product-identity calls belong to Shane.
- Recompute-style features succeed when insertion points are explicit across Fire, Update, and Inspect.
- Documentation must describe implemented pipeline stages exactly, especially around editability, hooks, and validation order.
- DSL `ensure` statements always require a `because` clause — tests that omit it get parse failures, not type-check failures.
- When narrowing injects `$positive:` it always co-injects `$nonneg:`, so `$positive:` alone is never the only marker present. Adding `$positive:` as a C76 fallback is defensive but aligns with the C92/C93 pattern.
- The dotted-key translation for event args happens in `BuildEventEnsureNarrowings` — bare markers like `$nonneg:Val` become `$nonneg:Submit.Val`. The C76 check constructs dotted keys via `TryGetIdentifierKey`, so both ends line up.
- Transition-row `set` chains are checked against one guard-narrowed snapshot per row. Earlier assignments do not feed later assignments today, which is the root cause of the intra-row divisor/null false negatives.
- State actions share the same stale-snapshot limitation as transition rows. A sequential action-flow fix should update both sites to avoid divergent compile-time semantics.
- The compiler-side fix is local to `PreceptTypeChecker`, but editor/type-context precision is coarser: `transition-actions` currently records one scope snapshot per row, not per assignment.
- Principle #8 should be described as capability-scoped strictness, not a blanket "assume satisfiable" default: the checker already rejects missing proof in some safety domains (nullability, `sqrt`, divisor safety) while remaining conservative in others (sequential flow, collection emptiness, arithmetic transfer).
- A bounded proof engine without SMT is a natural extension of the current checker: `ApplyNarrowing`, rule/state/event ensure snapshots, and C76/C93 already form a small abstract-interpretation pipeline.
- The sample corpus puts pressure on identifier/literal arithmetic and null-guarded `.length`, not on hard algebra. Sign/nonzero propagation plus sequential `set` flow is a better first investment than intervals or solver work.
- The honest no-SMT ceiling is a bounded abstract interpreter, not general algebra: sequential flow plus sign analysis buys the most; intervals help next; relational reasoning is the expensive last mile; nonlinear formulas like amortization denominators still need helper values or a solver.
- CORRECTION: Previous feasibility analysis imported general PL static analysis complexity (loop joins, widening, fixpoints, CFG reconvergence) that does not exist in Precept. Precept has no loops, no control-flow branches, no reconverging flow. `when` guards select rows independently; `if/then/else` is an inline ternary. The "join" for conditionals is a single bitwise AND of sign facts, not a lattice merge. This dramatically simplifies all proof techniques.
- The full non-SMT proof stack (sequential flow + interval arithmetic + relational patterns) is ~500 new lines of type checker code. The previous estimate of "high complexity" for intervals and "very high" for relational reasoning was inflated by 3-5x because it included complexity that doesn't apply to Precept's execution model.
- Interval arithmetic subsumes sign analysis. Building both is redundant — intervals give you signs for free (Positive = `(0, ∞)`, Nonneg = `[0, ∞)`). The optimal build is A (sequential flow) → C (intervals) → D (relational patterns), skipping B (signs) as a separate step.
- Relational inference in Precept is pattern matching, not symbolic algebra. `A - B` in divisor position + `$gt:A:B` marker = safe. No normalization, no canonicalization, no solver. ~65 lines total.

## Recent Updates

### 2026-04-18 — Proof engine design-to-code accuracy review (PR #108)

Full review written to `.squad/decisions/inbox/george-proof-engine-review.md`.

Key findings:

**Architecture:** The five gaps (1–5) are genuinely closed in Commits 1–6. The core proof engine (`LinearForm`, `RelationalGraph`, 5-tier lookup, kill loop) is correct. The prevention guarantee holds for compound divisor patterns. The engine is sound — no false positives. `TryInferRelationalNonzero` and `InferSubtractionInterval` are correctly deleted.

**Discrepancies (13 total):**
- D1: Design doc status header says "Implemented" — FALSE. Commits 7–15 not started.
- D2: String markers (`$ival:`, `$positive:`, `$nonneg:`, `$nonzero:`, `$gt:`, `$gte:`) NOT eliminated. Still active in `PreceptTypeChecker.cs`, `ApplyAssignmentNarrowing`, `ExtractIntervalFromMarkers`, `LookupLegacyRelationalInterval`, `NumericInterval.ToMarkerKey/TryParseMarkerKey`.
- D3: `_fieldIntervals`, `_flags`, `_exprFacts` typed stores do not exist. Only `_relationalFacts` is typed.
- D4: No `GlobalProofContext`/`EventProofContext` class split. Single `ProofContext` only.
- D5: `BuildEventEnsureNarrowings` still uses string surgery — compound relational ensure narrowings are LOST (not translated to dotted form).
- D6: No `ProofAssessment` model. C92 fires only on literal 0 (not truth-based). Identifier divisor path reads markers directly, bypasses `IntervalOf`.
- D7: No `Dump()` method on `ProofContext`.
- D8: No `NumericInterval.ToNaturalLanguage()`.
- D9: `LookupLegacyRelationalInterval` (Tier 6) still present.
- D10: C94–C98 do not exist. Zero matches in `src/Precept/`.
- D11: W2 — `WithRule` doesn't GCD-normalize. Functionally recovered by tier-5 `RelationalGraph`.
- D12: W1 — `ConstantOffsetScan` `>=` + offset 0 wrong inclusivity. Unreachable, trivial fix.
- D13 (undocumented): Identifier divisor path in `TryInferBinaryKind` checks markers directly — `field D as number min 1` has `$ival:D:1:...` (ExcludesZero=true) but gets C93 because no `$positive:D` or `$nonzero:D` marker. False positive. Must be fixed in Commit 7 by unifying to `IntervalOf`/`KnowsNonzero` for all expression shapes.

**Key implementation risks:**
- Commit 7 (typed stores) must atomically unify the identifier divisor path — otherwise `min N` fields get false C93 errors.
- `TryApplyNumericComparisonNarrowing` writes BOTH markers AND LinearForm facts for `id op id` — both must be cleaned up in Commit 7.
- `BuildEventEnsureNarrowings` also loses `_relationalFacts` from compound ensures — Commit 9 must fix this.
- Commit 11 (C92 truth-based) breaks code action message parsing in `PreceptCodeActionHandler.cs` — structured metadata required in same commit.
- Commit 14 (hover) needs proof source attribution threading through `IntervalOf` — this is non-trivial new API surface.

**Philosophy verdict:** Engine is sound. Prevention guarantee holds for all compound patterns. No false-positive proof paths. Conservative false negatives (C93 on min-constrained identifiers, D13) are the only failure mode — annoying but never unsafe.

**Action required:** (a) Update design doc status header to reflect in-progress state. (b) Deliver Commits 7–15 in this PR per Shane's "no more shortcuts" directive.

### 2026-04-17 — Stateless event handlers (`on EventName`) feasibility evaluation
- Verdict: **feasible**. All 7 implementation layers have clear insertion points. Total size: M.
- Parser: new `StatelessTransitionRowParser` combinator after `EventEnsureDecl.Try()`. No ambiguity — `ensure` token disambiguates.
- Model: `PreceptTransitionRow.FromState` must become `string?`. ~5 callsite null-guards. Stateless rows store `null`.
- Type checker: null-safe group key in `ValidateTransitionRows`. One new diagnostic: `transition <State>` not valid in stateless `on` block.
- Analysis: C49 must become conditional — suppress for events that have a stateless row handler.
- Engine: `Fire()` and `Inspect()` hard-wall on `IsStateless` must be replaced with branching paths. `_transitionRowMap` key type → `(string?, string)`. `ResolveTransition` signature → `string?`.
- Philosophy flag: `docs/philosophy.md` frames Precept as a "state machine/lifecycle" engine. Stateless event handlers expand that identity. Frank's call — not a technical blocker.
- Design decision needed: explicit `-> no transition` required (Option A) vs. implicit default. Recommend starting with explicit for parser consistency.
- Key files: `PreceptParser.cs` (Statement union, new combinator), `PreceptModel.cs` (FromState nullable), `PreceptTypeChecker.cs` (ValidateTransitionRows null-guarding), `PreceptRuntime.cs` (Fire/Inspect branching), `PreceptAnalysis.cs` (C49 conditional).



### 2026-04-17 — Issue #106 Slice 6: sqrt C76 rework with unified narrowing + dotted key fix
- Verified the C76 `$nonneg:` proof lookup already handled dotted event-arg keys (inline ternary, not broken as initially suspected).
- Refactored the C76 identifier check to use `TryGetIdentifierKey(idArg, out var idKey)` for consistency with the rest of the narrowing infrastructure.
- Added `$positive:` as alternate C76 proof (defensive, matches the C92/C93 divisor check pattern). `positive` implies nonneg, so this is sound.
- Updated C76 message in `DiagnosticCatalog.cs` and the instance message in `PreceptTypeChecker.cs` to mention `rule`, state/event `ensure`, and guard as proof sources (not just `nonnegative` constraint).
- 5 new tests: rule proof, state ensure proof, guard proof, dotted event-arg with event ensure (no C76), dotted event-arg without proof (C76 emitted with `Submit.Val` in message).
- All 1290 Precept.Tests + 169 LS tests pass. Catalog drift tests unaffected (fragment `"non-negative"` still matches).

### 2026-04-17 — Issue #106 Slice 3: unified rule-based proof extraction
- Replaced the bespoke `$nonneg:` constraint-inspection loop in `Check()` with unified rule-based proof iteration through `ApplyNarrowing`.
- The old loop directly inspected `FieldConstraint.Nonnegative`, `Positive`, and `Min { Value: >= 0 }` properties. The new approach iterates `model.Rules` (unguarded only) and delegates to `ApplyNarrowing(rule.Expression, dataFieldKinds, assumeTrue: true)`.
- This works because constraints desugar to synthetic rules at parse time (e.g., `positive` → `rule Field > 0`), so iterating rules automatically picks up constraint proofs.
- Guarded rules excluded via `.Where(r => r.WhenGuard is null)` — a guarded rule's fact only holds when its guard is true, so injecting it unconditionally would be unsound.
- Had to widen `dataFieldKinds` declaration from `var` (inferred `Dictionary<>`) to explicit `IReadOnlyDictionary<>` since `ApplyNarrowing` returns the readonly interface.
- DSL gotcha: `rule` statements require a `because` clause, and `when` guards must reference boolean fields (not state names). Also, duplicate unguarded `from S on E` rows are caught at parse time — use separate events to avoid conflicts in tests.
- New test: `Check_GuardedRule_ExcludedFromProofIteration_SqrtStillC76` — verifies guarded `rule D >= 0 when IsActive` does NOT suppress C76 on `sqrt(D)`.
- All 1498 tests pass (1237 Precept + 92 MCP + 169 LS).

### 2026-04-17 — Issue #106 Slice 2: or-pattern null-guard decomposition
- Implemented `TryDecomposeNullOrPattern` in `PreceptTypeChecker.cs` — recognizes `Field == null or Field > 0` patterns from `MaybeNullGuard` desugars and extracts numeric proof markers from the non-null branch.
- Handles both orderings (null-check first or numeric first), reversed null literal position (`null == Field`), compound `and` patterns (`Field == null or (Field >= 0 and Field < 100)`), and same-field identity checks to prevent unsound cross-field decomposition.
- Wired into `ApplyNarrowing()` `or` branch under `assumeTrue: true`, with C42-dependency soundness comment.
- Nullable field syntax is `number nullable`, not `number?` — caught by test parse failures.
- Nullable fields hit C77 (null-argument) before C76 (non-negative proof) when no null-guard is present. Tests for reject cases assert either C76 or C77.
- 7 new tests, all 1236 tests pass (+ 92 MCP + 169 LS = full green).

### 2026-04-17 — Issue #106 Slice 1: numeric narrowing infrastructure
- Implemented `FlipComparisonOperator`, `TryGetNumericLiteral`, and `TryApplyNumericComparisonNarrowing` in `PreceptTypeChecker.cs`.
- Wired numeric comparison narrowing into `ApplyNarrowing()` after the existing null-comparison branch.
- Proof markers injected: `$positive:`, `$nonneg:`, `$nonzero:` keyed as `StaticValueKind.Boolean` (matching existing `$nonneg:` convention from the bespoke constraint loop).
- Existing `$nonneg:` markers use `StaticValueKind.Boolean`, not `NonNull` (which doesn't exist in the enum). The C76 check only does `ContainsKey`, so the value type doesn't matter — but consistency matters.
- Tested via sqrt() C76 interaction: if narrowing correctly injects `$nonneg:`, sqrt() inside the guard doesn't emit C76. This validates the infrastructure without needing C92/C93 (Slice 5).
- All 1229 existing tests pass with the new code. 6 new tests added.
- Key file paths: `src/Precept/Dsl/PreceptTypeChecker.cs` (~L2130–2280 for new methods), `test/Precept.Tests/PreceptTypeCheckerTests.cs` (bottom of file for new tests).

### 2026-04-17 — Unified proof plan full design review
- **Verdict: APPROVED-WITH-CAVEATS** on `temp/unified-proof-plan.md`.
- §8a completeness: found 2 missing patterns — (1) scalar-distributed differences `k*A - k*B` not provable from `rule A > B` (workaround: factor as `k*(A-B)`), (2) `pow`/`truncate` function opacity not listed alongside `abs`/`min`/`sqrt` in the unsupported table.
- Found 2 patterns missing from §8 coverage matrix that the plan DOES prove but doesn't claim: `rule A > -B` proving `Y/(A+B)` and `rule A+B > C` proving `Y/(A+B-C)`. Both validate via LinearForm normalization.
- Rational `long/long` overflow: reachable within depth-8 bound via 3 multiplications of large constants (e.g., `A * 10^9 * 10^9 * 10`). **[CAVEAT]** Plan must specify `checked` arithmetic in `Rational.Multiply` with null-fallback in `TryNormalize` on `OverflowException`. Also recommend cross-GCD pre-reduction before multiplication to prevent overflow in common cases.
- `INumber<Rational>`: over-engineering for current usage (~50+ interface members vs ~20 actually needed). Non-blocking — recommend implementing operators directly, add interface later if generic math needed.
- `ProofContext.Dump()` unspecced: confirmed low priority, agree.
- LinearForm depth bound (8): verified parens do NOT blow the budget in practice (7-level paren nesting compiles clean today). Plan should clarify that `PreceptParenthesizedExpression` unwrapping does not decrement depth counter.
- CodeContracts Pentagons: confirms architectural validity. Our lazy composition (query-time reduced product) is sufficient for divisor-safety — no backward propagation from relational → interval needed. No patterns to adopt.
- Confidence on §8a completeness: MEDIUM-HIGH. Systematically checked all 11 expression forms, all rule/guard/ensure contexts, cross-namespace patterns, negation, scaling, depth stress. The 2 missing patterns are low-frequency with simple workarounds.

### 2026-04-12 — Issue #17 computed fields feasibility
- Confirmed computed fields are feasible with additive parser/model/runtime work and a single recomputation helper inserted before constraint evaluation.

### 2026-04-11 — Issue #14 documentation sync
- Synced language, runtime, editability, MCP, and README docs for declaration `when` guards after implementation.
