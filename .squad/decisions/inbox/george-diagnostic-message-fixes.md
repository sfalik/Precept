# George — Diagnostic Message Fixes

**Author:** George (Runtime Dev)
**Date:** 2026-05-12
**Requested by:** Shane
**Source:** Elaine's diagnostic audit

---

## Applied

- Updated the approved templates in `src/Precept/Language/Diagnostics.cs` for:
  - `UnprovedModifierRequirement`
  - `UnprovedDimensionRequirement`
  - `UnprovedPresenceRequirement`
  - `InvalidInterpolatedTypedConstantForm`
  - `InterpolatedTypedConstantHoleTypeMismatch`
  - `DimensionMismatchInUnitSlot`
  - `CollectionOperationOnScalar`
  - `FunctionArgConstraintViolation`
  - `InvalidTypedConstantContent`
- Reworked proof-context formatting in `src/Precept/Pipeline/ProofEngine.cs` so transition-row usage renders as `on event 'E' from state 'S'` and proof-usage suffixes render as `(used ...)` instead of chaining `in ... in ...`.
- Removed the hardcoded `"unknown"` argument from `UnprovedDimensionRequirement` emission and switched the message to the approved author-facing guidance.
- Fixed `InvalidTypedConstantContent` emission in `src/Precept/Pipeline/TypeChecker.Expressions.cs` so `{1}` is the target type display name (`currency`, `duration`, etc.), matching the revised template.
- Added regression coverage for the revised templates and emitted message text in `DiagnosticsTests`, `ProofEngineTests`, and `TypeCheckerTypedConstantTests`.
- Synced the proof-engine diagnostic table in `docs/compiler/proof-engine.md`.

## Skipped / constrained

- The `RuleIdentity` sub-fix was skipped. `RuleIdentity` currently only carries `RuleIndex` (`src/Precept/Pipeline/SemanticIndex.cs`) and has no `BecauseText` or equivalent author-facing label to surface.
- I did not edit `docs/Working/typed-constants-and-proof-coverage-plan.md` because it was already dirty in the shared workspace; I updated the canonical compiler doc instead.

## Validation

- `dotnet build src\Precept\Precept.csproj --nologo` ✅
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --filter "FullyQualifiedName~DiagnosticsTests|FullyQualifiedName~ProofEngineTests|FullyQualifiedName~TypeCheckerTypedConstantTests" --nologo` ✅ (818/818)
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --nologo` ⚠️ still fails on the pre-existing `ProofEngineTypedArgQualifierTests.InventoryItem_Sample_PRE0114_Count_Drops_Below_Baseline` workspace issue; current run: 4840 passed / 1 failed / 4841 total.

## Notes

- The full-suite failure was already present before this work; my changes are message/template only and do not alter qualifier-proof counting logic.
