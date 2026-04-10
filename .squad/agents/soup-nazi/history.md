## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, and language-server validation.
- Keeps behavioral claims tied to executable proof and records gaps as actionable test findings, not just counts.
- Historical summary (pre-2026-04-13): led broad verification for declaration guards, including parser/type-checker/runtime/LS/MCP coverage and test-matrix planning for guarded editability.

## Learnings

- Compile-time/default-data behavior must be tested explicitly whenever new guard semantics are introduced.
- Guard scope rules need separate coverage for field-scoped and arg-scoped contexts.
- Regression risk is highest where hydration, editability, and inspect/update paths share runtime machinery.
- When slice agents do their job, drift tests arrive pre-populated — audit confirms rather than creates. Slice 5 agent added C92/C93 drift entries correctly.
- Event arg constraint keyword → C93 suppression must be tested separately from event arg ensure → C93 suppression. The mechanism overlaps but the AC names them as distinct.
- `from any` expansion tests must cover each proof-scoped diagnostic independently. A null-narrowing `from any` test does NOT satisfy the divisor `from any` AC.
- Theory-based tests with `messageFragment` inline data are the strongest pattern for context-aware diagnostic messages — each row self-documents what the message should say.

## Recent Updates

### 2026-04-17 — PR #108 Test Review (Issue #106 divisor safety)
- Reviewed full PR: 34 behavioral ACs mapped to tests. 32/34 covered, 2 blockers.
- B1: No test for event arg `positive` constraint keyword suppressing C93 (AC #12). The sqrt variant is tested but not divisor.
- B2: No test for `from any` expansion with per-state divisor proof (AC #21). Existing `from any` test is null-narrowing only.
- Warnings: guarded state ensure exclusion from divisor proof covered by mechanism but no explicit test; generic C93 message text not asserted.
- Strengths: Theory-based proof source × operator matrix, 7-variant or-pattern suite, zero disabled tests, CatalogDriftTests fully populated, code action tests go beyond core AC.
- Total: ~51 new test methods (47 Precept.Tests + 4 LS.Tests). 1463 total tests, 0 failures.
### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### 2026-04-10 - Issue #31 Slice 7: keyword logical operator tests
- Updated 9 existing test files (8 in `test/Precept.Tests/`, 1 in `test/Precept.LanguageServer.Tests/`) to replace DSL symbols `&&` → `and`, `||` → `or`, `!` → `not` in all `.precept` string literals and operator assertions.
- Created new `test/Precept.Tests/PreceptKeywordLogicalOperatorTests.cs` covering: basic keyword parsing (not/and/or), precedence validation (not > and > or), null narrowing through `not (Field == null)`, `!=` operator unaffected, old symbols `&&`/`||`/`!` produce parse errors, compound expression parse/evaluate, invariant context (or/and).
- George's runtime changes (parser, tokenizer, type checker, evaluator, samples) were already on the branch — tests written against the finished spec.
- Key learning: always check if the partner's runtime changes are already on branch before assuming tests will fail. The old-symbol-produces-error tests and keyword tests may pass immediately if George's work is complete.

### 2026-04-11 — Guarded declaration validation sweep
- Built and verified multi-layer tests for guarded invariants, state asserts, event asserts, and guarded edit blocks, including runtime and MCP coverage.

### 2026-04-17 — Slice 8: C92/C93 catalog drift + sample audit (#106)
- Verified C92 (literal zero divisor) and C93 (unproven divisor) drift test entries already present and correct in both `ConstraintTriggers` and `LineAccuracyData`.
- Audited 5 sample files: loan-application, invoice-line-item, insurance-claim, travel-reimbursement, clinic-appointment-scheduling — all compile clean, zero C92/C93 diagnostics.
- Critical validation: `travel-reimbursement.precept` with `Submit.Lodging / Submit.Days` (non-literal divisor) produces no C93 warning, confirming `BuildEventEnsureNarrowings` (Slice 4) is working correctly.
- No code changes needed. All 1290 tests pass.
