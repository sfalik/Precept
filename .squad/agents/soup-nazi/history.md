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
- Temporal type proposals create most of their risk in the type-checker and runtime boundary layers, not in raw arithmetic alone; the dangerous paths are context-dependent quantity resolution, DST mediation, null/default interactions, and field-constraint desugaring.
- Design-doc review must flag spec contradictions as test blockers immediately: interpolation expression scope, diagnostic severity, and unresolved nullable/default semantics all prevent writing stable assertions.
- Collection and ordering interactions need explicit temporal coverage because set semantics depend on comparability; date/time/instant/duration/datetime are set-safe, while period/timezone/zoneddatetime must fail with teachable diagnostics.

## Recent Updates

### 2026-04-17 — PR #108 Test Review (Issue #106 divisor safety)
- Reviewed full PR: 34 behavioral ACs mapped to tests. 32/34 covered, 2 blockers.
- B1: No test for event arg `positive` constraint keyword suppressing C93 (AC #12). The sqrt variant is tested but not divisor.
- B2: No test for `from any` expansion with per-state divisor proof (AC #21). Existing `from any` test is null-narrowing only.
- Warnings: guarded state ensure exclusion from divisor proof covered by mechanism but no explicit test; generic C93 message text not asserted.
- Strengths: Theory-based proof source × operator matrix, 7-variant or-pattern suite, zero disabled tests, CatalogDriftTests fully populated, code action tests go beyond core AC.
- Total: ~51 new test methods (47 Precept.Tests + 4 LS.Tests). 1463 total tests, 0 failures.

### 2026-04-11 — Guarded declaration validation sweep
- Built and verified multi-layer tests for guarded invariants, state asserts, event asserts, and guarded edit blocks, including runtime and MCP coverage.

### 2026-04-17 — Slice 8: C92/C93 catalog drift + sample audit (#106)
- Verified C92 (literal zero divisor) and C93 (unproven divisor) drift test entries already present and correct in both `ConstraintTriggers` and `LineAccuracyData`.
- Audited 5 sample files: loan-application, invoice-line-item, insurance-claim, travel-reimbursement, clinic-appointment-scheduling — all compile clean, zero C92/C93 diagnostics.
- Critical validation: `travel-reimbursement.precept` with `Submit.Lodging / Submit.Days` (non-literal divisor) produces no C93 warning, confirming `BuildEventEnsureNarrowings` (Slice 4) is working correctly.
- No code changes needed. All 1290 tests pass.

### 2026-04-18 — CurrencyQuantityUomDesign.md test strategy review
- Reviewed full design doc for 7 business-domain types: `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`.
- Produced structured review in `.squad/decisions/inbox/soup-nazi-currency-test-review.md`.
- Found 6 acceptance criteria gaps (AC-GAP-1 through AC-GAP-6). Two are test blockers: AC-GAP-2 (maxplaces default contradiction) and Issue #115 (decimal precision bug).
- Issue #115 is a hard test-blocker: ALL decimal-backed arithmetic tests (money, quantity, price, exchangerate) are unstable until it is fixed. Must be gated or explicitly documented.
- Identified 10 regression risks (X1–X10). X1 and X10 (CatalogDrift ConstraintTriggers and LineAccuracyData) will fail the moment new diagnostic codes are assigned — drift tests need entries before any implementation PR can be green.
- E5 (chained compound quantity cancellation) and E6 (commensurable arithmetic result unit after conversion) are the two implementation edge cases most likely to be wrong on first try.
- Hardest single edge case: E4 (duration cancellation with `days` denominator, D15 boundary) — most likely to appear in real DSL and be silently accepted if the compiler only checks denominator unit name and not fixed-length flag.
- Discrete equality narrowing (T9) tests must be written BEFORE null-narrowing tests are touched. Regression risk X9 (AccidentalNullNarrowingInterference) is non-trivial.
- Total estimated new tests: ~310 (220 Precept.Tests + 60 LS.Tests + 30 Mcp.Tests).
- Key file paths: `docs/CurrencyQuantityUomDesign.md`, `test/Precept.Tests/PreceptDecimalTypeTests.cs` (regression anchor), `test/Precept.Tests/CatalogDriftTests.cs` (drift anchor), `test/Precept.LanguageServer.Tests/PreceptAnalyzerNullNarrowingTests.cs` (X9 anchor).

### 2026-04-18 — Edge case analysis: period vs duration in compound-type arithmetic
- Completed 10-scenario stress-test of D15 (period-only cancellation) vs Frank's proposal (duration also cancels).
- Findings written to `.squad/decisions/inbox/soup-nazi-duration-edge-cases.md`.
- 6 of 10 scenarios are 🔴 correctness breaks. 4 of the 6 are not edge cases — they are mainstream business patterns (fractional-hour payroll, instant-subtraction billing, DST fall-back wages, partial-day daily-rate pricing).
- The DST fall-back scenario (Scenario 3) is the most severe: the period path silently underpays overnight workers by one hour on fall-back night — a potential FLSA violation. The period path is wrong there, not just inconvenient.
- Issue #115 (decimal→double bug) is a live dependency: any duration-cancellation implementation that routes through `duration.totalHours` (number/double) will inject double precision artifacts into decimal-backed money results on day one.
- The "both paths exist" confusion (Scenario 9) is the subtlest risk: both period and duration can produce the same money value 363 days/year and silently diverge on 2 DST nights with no compiler warning.
- Fixed-length asymmetry (Scenario 10): hours cancel, weeks don't, for non-obvious NodaTime reasons. This will confuse domain authors writing rental contracts.

### 2026-04-18 — Currency review batch consolidation

- Frank and George converged on the same structural gate: Issue #95 is feasible only after the `maxplaces` contract, accessor surface, compound serialization form, and Issue #115 are resolved.
- The batch strengthened the testing priority order: lock the duration/days boundary and chained cancellation semantics before implementing the broad matrix.
- Newman kept the MCP question intentionally narrow: string versus object transport for compound values. Test planning should assume one canonical form, not both.
- Uncle Leo's High-severity conditions mean validator/normalization order and Issue #115 framing need to land in the design doc before implementation tests should be treated as durable.
