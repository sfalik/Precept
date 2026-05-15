# Soup Nazi — N3/N4 qualifier follow-up tests

- **When:** 2026-05-15T19:02:24.3919248-04:00
- **By:** Soup Nazi
- **Requested by:** Shane
- **Status:** Processed — merged into `.squad/decisions.md` on 2026-05-15T23:14:11Z.

## Decision

Add direct regression anchors for Frank's remaining N3/N4 qualifier follow-up gaps instead of relying on indirect coverage.

## What changed

- Added a direct TypeChecker assignment-resolver test proving bare `duration` references resolve the implied `TemporalDimension(Time)` axis as `Resolved`, while the real `duration` assignment still compiles clean.
- Added paired synthetic compound-cancellation resolver tests in `TypeCheckerAssignmentQualifierTests` and `ProofEngineTypedArgQualifierTests` so both subsystems now assert the shared `QualifierUnitHelpers` path derives `m` / `length` from `m/s` cancelled by `s`.
- Added function-call qualifier preservation coverage for `min(money, money)`, `max(quantity, quantity)`, and `round(money, places)`, including clean matching assignments plus qualifier-mismatch targets.

## Validation

- Baseline before edits: `dotnet test test\Precept.Tests\ --no-restore --nologo -v q` reported `5655 passed / 9 failed / 5664 total`.
- Final closeout validation after the follow-up arc finished: `dotnet test test\Precept.Tests\ --no-restore --nologo -v q` reported `5689 passed / 10 failed / 5699 total`.
- All 26 new N3/N4 tests pass.
- A transient detached HEAD artifact that surfaced as a duplicate variable in `FieldState.cs` blocked an intermediate rebuild, but it was resolved before the final run.
