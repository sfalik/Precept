## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, and language-server validation.
- Keeps behavioral claims tied to executable proof and records gaps as actionable test findings, not just counts.
- Historical summary (pre-2026-04-13): led broad verification for declaration guards, including parser/type-checker/runtime/LS/MCP coverage and test-matrix planning for guarded editability.

## Learnings

- When a test is added to satisfy a "count" AC, always verify it also satisfies any "correctness" sub-clauses (e.g., StateContext, message text). A test that checks `ContainSingle()` for AC #21 does NOT satisfy the "correct state context" part of that AC.
- Prior-round blockers can be marked "fixed" while still having a gap: B2 (AC #21) shipped the right test method but the assertion stopped at count=1, never checking StateContext. The AC said "with correct state context."
- Integration tests (diagnostic samples with EXPECT annotations) can cover message text gaps that unit tests leave open — but unit tests with code-only assertions are a weaker safety net. Both should be present where the design specifies message content.
- When Elaine-B1 (FormatInterval → natural language) ships, divisor-safety.precept:41 EXPECT line will need updating. Track this dependency proactively.
- Compile-time/default-data behavior must be tested explicitly whenever new guard semantics are introduced.
- Guard scope rules need separate coverage for field-scoped and arg-scoped contexts.
- Regression risk is highest where hydration, editability, and inspect/update paths share runtime machinery.
- When slice agents do their job, drift tests arrive pre-populated — audit confirms rather than creates. Slice 5 agent added C92/C93 drift entries correctly.
- Event arg constraint keyword → C93 suppression must be tested separately from event arg ensure → C93 suppression. The mechanism overlaps but the AC names them as distinct.
- `from any` expansion tests must cover each proof-scoped diagnostic independently. A null-narrowing `from any` test does NOT satisfy the divisor `from any` AC.
- Theory-based tests with `messageFragment` inline data are the strongest pattern for context-aware diagnostic messages — each row self-documents what the message should say.
- Principle #8 stance from the testing seat: compile-clean should not imply safety the checker did not actually prove. Runtime failure tests are a backstop, not the guarantee, but tighter philosophy must preserve already-proven compound patterns rather than flattening the language into trivial-only proofs.

- Compound "assume satisfiable" heuristic has a latent soundness gap: `D - D` is always zero but no C93 fires. Any future compound analysis must address this test (`Check_DivisorCompound_Subtraction_NoWarning`).
- The sample corpus has ZERO non-literal field-based divisors that lack proof — every division by a field or event arg is already constrained. Future proof techniques have no corpus gap to close for existing samples; their value is in enabling NEW patterns (inline if/then/else division, function-wrapped divisors, relational cross-field proofs).
- if/then/else in Precept is ternary expression only — narrowing applies to typing within branches, not control-flow reachability. This simplifies branch analysis compared to general PL.
- Function proofs (abs, max, min, clamp, round, sqrt) have the highest ratio of new provable patterns to implementation complexity. `max(D, 1)` as safe divisor is the single most impactful pattern.
- Interval arithmetic's biggest win in the current corpus is computed field constraint verification (e.g., proving `LineTotal nonnegative` from upstream field ranges).
- A large proof-test count can still hide guarantee holes. For PR #108, the critical misses are concentrated at the design-expansion edges: truth-based `C92` vs unresolved `C93`, the entire `C94`-`C98` family, the 64-fact / 256-node graph caps, and proof surfacing in hover/MCP.

## Recent Updates

### 2026-04-17 — Proof Technique Test Inventory (pre-implementation research)
- Read all 25 sample files, cataloged every arithmetic expression: 5 divisions, 2 modulos, 22+ additions/subtractions, 11+ multiplications, 8 function calls, 5 if/then/else ternaries, 12+ relational guard expressions.
- All sample divisions are already safe: either literal divisors or event-arg-proven. No new technique closes a gap in the existing corpus.
- Identified `Check_DivisorCompound_Subtraction_NoWarning` (`D - D`) as a latent soundness gap that any compound analysis would expose.
- Produced 40+ concrete test cases across 5 techniques with positive/negative/edge categories in actual Precept syntax.
- Output: `temp/soup-nazi-proof-test-inventory.md`

### 2026-04-17 — PR #108 Test Review (Issue #106 divisor safety)
- Reviewed full PR: 34 behavioral ACs mapped to tests. 32/34 covered, 2 blockers.
- B1: No test for event arg `positive` constraint keyword suppressing C93 (AC #12). The sqrt variant is tested but not divisor.
- B2: No test for `from any` expansion with per-state divisor proof (AC #21). Existing `from any` test is null-narrowing only.
- Warnings: guarded state ensure exclusion from divisor proof covered by mechanism but no explicit test; generic C93 message text not asserted.
- Strengths: Theory-based proof source × operator matrix, 7-variant or-pattern suite, zero disabled tests, CatalogDriftTests fully populated, code action tests go beyond core AC.
- Total: ~51 new test methods (47 Precept.Tests + 4 LS.Tests). 1463 total tests, 0 failures.

### 2026-04-11 — Guarded declaration validation sweep
- Built and verified multi-layer tests for guarded invariants, state asserts, event asserts, and guarded edit blocks, including runtime and MCP coverage.

### 2026-04-17 — Unified proof plan full test review (pre-implementation)
- Reviewed `temp/unified-proof-plan.md` §4-§8a against existing test codebase. Plan has ~166 new tests across 9 files.
- Coverage matrix (§8): All 20 input patterns mapped to planned test files. Every "✅ proves" and "💀 correctly rejected" has at least one test. No coverage gaps in the matrix proper.
- §8a unsupported patterns: Found that 4 of 17 rows need explicit "correctly rejected" tests that are NOT listed in any planned test file: row 1 (non-linear `A*B-C`), row 4 (function opacity `abs(X)-B`), row 14 (inequality-without-ordering `A!=B`), row 16 (modulo `A%B`). Filed as NON-BLOCKING finding — these are easy to add.
- Edge cases missing per file: LinearFormTests needs constant-only normalization, single-term form, `long.MaxValue` GCD stress, and construction-order equality. RationalTests needs `long.MinValue` negation overflow, multiplication overflow, division by zero. ProofContextTests needs deeply nested `Child()`, unknown field `WithAssignment`, opaque expression `IntervalOf`. TransitiveClosureTests needs disconnected graph, self-loop, mixed-scope chains.
- Regression risk: Highest risk files are PreceptTypeCheckerTests.cs (C-Nano section specifically), ConditionalExpressionTests.cs, CatalogDriftTests.cs (C92/C93 entries), DiagnosticSpanPrecisionTests.cs (C93 column tests). The ProofContext signature refactor (commit 2) touches every narrowing method — mechanical but high blast radius.
- Soundness invariant tests: -3..+3 range is acceptable first pass but narrow. Missing: decimal values (0.001, 0.1), larger magnitudes (100, 1000), explicit saturation tests.
- Workaround flagging feasibility: Assessed all 5 unsupported categories. Detection is feasible for all — `LinearForm.TryNormalize` failure reason + expression shape inspection + rule set scanning. Suggestion generation requires ~100-200 new LOC. Recommendation: follow-up PR, not this one — it improves diagnostic quality but doesn't change proof power.
- Verdict: APPROVED-WITH-CAVEATS (1 blocker, 6 non-blocking findings).

### 2026-04-17 — Slice 8: C92/C93 catalog drift + sample audit (#106)
- Verified C92 (literal zero divisor) and C93 (unproven divisor) drift test entries already present and correct in both `ConstraintTriggers` and `LineAccuracyData`.
- Audited 5 sample files: loan-application, invoice-line-item, insurance-claim, travel-reimbursement, clinic-appointment-scheduling — all compile clean, zero C92/C93 diagnostics.
- Critical validation: `travel-reimbursement.precept` with `Submit.Lodging / Submit.Days` (non-literal divisor) produces no C93 warning, confirming `BuildEventEnsureNarrowings` (Slice 4) is working correctly.
- No code changes needed. All 1290 tests pass.
