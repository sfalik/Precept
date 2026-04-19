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

## Recent Updates

### 2026-04-17 — Issue #106 Slice 6: sqrt C76 rework with unified narrowing + dotted key fix
- Verified the C76 `$nonneg:` proof lookup already handled dotted event-arg keys (inline ternary, not broken as initially suspected).
- Refactored the C76 identifier check to use `TryGetIdentifierKey(idArg, out var idKey)` for consistency with the rest of the narrowing infrastructure.
- Added `$positive:` as alternate C76 proof (defensive, matches the C92/C93 divisor check pattern). `positive` implies nonneg, so this is sound.
- Updated C76 message in `DiagnosticCatalog.cs` and the instance message in `PreceptTypeChecker.cs` to mention `rule`, state/event `ensure`, and guard as proof sources (not just `nonnegative` constraint).
- 5 new tests: rule proof, state ensure proof, guard proof, dotted event-arg with event ensure (no C76), dotted event-arg without proof (C76 emitted with `Submit.Val` in message).
- All 1290 Precept.Tests + 169 LS tests pass. Catalog drift tests unaffected (fragment `"non-negative"` still matches).
### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### 2026-04-10 - Issue #31 Slices 1-4 + Samples (keyword logical operators)
- **Token names found:** `And`, `Or`, `Not` — already existed in `PreceptToken` enum with old `[TokenSymbol("&&")]`, `[TokenSymbol("||")]`, `[TokenSymbol("!")]`. Changed to `[TokenSymbol("and")]`, `[TokenSymbol("or")]`, `[TokenSymbol("not")]`. Both `TokenCategory.Operator` attributes were correct; no category changes needed.
- **Tokenizer protection:** `requireDelimiters: true` on keyword registration (step 7 in `Build()`) is the mechanism that prevents `android` from matching `And` + `roid`. The operator entries (`&&`, `||`, `!`) were in steps 4-5 (plain span/character matches without delimiters) — removing them from those sections was sufficient, since `And`/`Or`/`Not` are now registered as keywords via the keyword loop.
- **Surprises — none.** The branch already had all five source-file changes prepared (PreceptToken.cs, PreceptTokenizer.cs, PreceptParser.cs, PreceptTypeChecker.cs, PreceptExpressionEvaluator.cs) plus sample file changes. The work was complete; it only needed a build verification and commit.
- **ApplyNarrowing location:** `PreceptTypeChecker.cs` around line 889 (method body starts after `StripParentheses` call). The pattern-matched `"!"` → `"not"` update was in the `PreceptUnaryExpression { Operator: "not" } unary` destructure; `"&&"` → `"and"` and `"||"` → `"or"` were `binary.Operator ==` string comparisons in the same method. Both were already updated on the branch.
- **`CatalogDriftTests.cs` change:** C4 test used `PreceptParser.ParseExpression("&&")` to trigger parse-expression failure path; updated to `"and"` since the old `&&` is no longer a valid token.
- **Build:** 0 errors, 0 warnings on full solution (`dotnet build`). Committed as `83497aa` on `squad/31-keyword-logical-operators`.
- **Key pattern:** When a branch has pre-staged diffs, always run `git status` + `git diff` to inventory existing work before touching files — avoids double-applying changes.

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
