# Soup Nazi — N3/N4 qualifier follow-up tests

- **When:** 2026-05-15T19:02:24.3919248-04:00
- **By:** Soup Nazi
- **Requested by:** Shane
- **Status:** Tests added; full project rebuild remains blocked by pre-existing test-project compile errors.

## Decision

Add direct regression anchors for Frank's remaining N3/N4 qualifier follow-up gaps instead of relying on indirect coverage.

## What changed

- Added a direct TypeChecker assignment-resolver test proving bare `duration` references resolve the implied `TemporalDimension(Time)` axis as `Resolved`, while the real `duration` assignment still compiles clean.
- Added paired synthetic compound-cancellation resolver tests in `TypeCheckerAssignmentQualifierTests` and `ProofEngineTypedArgQualifierTests` so both subsystems now assert the shared `QualifierUnitHelpers` path derives `m` / `length` from `m/s` cancelled by `s`.
- Added function-call qualifier preservation coverage for `min(money, money)`, `max(quantity, quantity)`, and `round(money, places)`, including clean matching assignments plus qualifier-mismatch targets.

## Validation

- Baseline before edits: `dotnet test test\Precept.Tests\ --no-restore --nologo -v q` reported `5655 passed / 9 failed / 5664 total`.
- Post-edit rebuilds are blocked by existing project issues outside this slice:
  - analyzer-gated rebuild errors in `src\Precept\Language\Diagnostics.cs` (`PRECEPT0007`, `PRECEPT0028`) unless suppressed for inspection
  - compile errors in `test\Precept.Tests\TypeChecker\TypeCheckerFieldStateTests.cs` (`AssertSingleD143` / `AssertNoD143` missing)
- IDE diagnostics for the two edited test files are clean.
