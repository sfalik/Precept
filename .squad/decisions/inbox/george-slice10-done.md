# Slice 10 — D93 RequiredFieldsNeedInitialEvent: Done

**Branch:** `spike/Precept-V2-Radical`
**Commit:** `HEAD`

## Outcome

- Added `ValidateConstructionGuarantees` to emit D93 when a precept has no initial event and still exposes required non-collection, non-computed fields at construction time.
- Reused the required-field filter used by D132, but excluded fields omitted in every initial state so omit-driven draft workflows still compile until a field becomes present.
- Wired construction validation into `TypeChecker.Check` immediately after field-state validation.
- Added `TypeCheckerConstructionTests` with the requested D93 coverage and updated coupled fixtures/samples so unrelated tests remain focused on their intended behavior.

## Validation

- `dotnet test test\Precept.Tests\Precept.Tests.csproj` passed (`5127` tests).
- `samples\Test.precept` now produces D93 instead of compiling clean.
