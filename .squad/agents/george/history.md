## Core Context

- Owns the core DSL/runtime: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects the fire/update/inspect pipeline contract and keeps runtime behavior aligned with docs, tests, and MCP output.
- Historical summary (pre-2026-04-13): led runtime feasibility and implementation analysis for guarded declarations, event hooks, computed fields, and verdict-modifier semantics.

## Learnings

- Runtime/design gaps should be separated cleanly from philosophy decisions; product-identity calls belong to Shane.
- Recompute-style features succeed when insertion points are explicit across Fire, Update, and Inspect.
- Documentation must describe implemented pipeline stages exactly, especially around editability, hooks, and validation order.

## Recent Updates

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

### 2026-04-12 — Issue #17 computed fields feasibility
- Confirmed computed fields are feasible with additive parser/model/runtime work and a single recomputation helper inserted before constraint evaluation.

### 2026-04-11 — Issue #14 documentation sync
- Synced language, runtime, editability, MCP, and README docs for declaration `when` guards after implementation.
