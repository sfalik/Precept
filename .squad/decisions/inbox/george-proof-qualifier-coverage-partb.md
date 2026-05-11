# Decision: Proof Engine Qualifier Coverage — Part B (Slices 7+8+9)

**Agent:** George (Runtime Dev)
**Date:** 2025-07-11
**Status:** Complete

## Summary

Closed three critical philosophy violations in the proof engine's qualifier coverage:

1. **Slice 7 — Money Currency Enforcement:** Added `QualifierCompatibilityProofRequirement` on `QualifierAxis.Currency` to all 8 money operations (MoneyPlusMoney, MoneyMinusMoney, and 6 comparison ops). These operations already had textual descriptions saying "same currency required" and `QualifierMatch.Same` for type-checker disambiguation, but had NO proof requirements — meaning the proof engine had zero enforcement.

2. **Slice 8 — Qualifier Chain Infrastructure:** Introduced `QualifierChainProofRequirement`, a new DU subtype of `ProofRequirement` with dual-subject, dual-axis design for cross-type qualifier validation. Added chain requirements to `ExchangeRateTimesMoney` (FromCurrency↔Currency) and `PriceTimesQuantity` (Dimension↔Dimension). Extended `TryQualifierCompatibilityProof` with chain comparison logic using `ExtractComparableValue` for cross-axis string matching.

3. **Slice 9 — Dimension Fallback:** Added Unit→Dimension fallback in `ResolveQualifierOnAxis` so that dimension-only fields (`quantity of 'mass'`) can satisfy Unit-axis proof requirements.

## Design Decisions

- **QualifierChainProofRequirement has dual axes** (LeftAxis + RightAxis) unlike QualifierCompatibilityProofRequirement which has a single shared Axis. This is necessary because chain validation crosses different qualifier axes (e.g., FromCurrency↔Currency).
- **Chain diagnostics reuse `DiagnosticCode.UnprovedQualifierCompatibility`** (PRE0114) — no new diagnostic code was needed since the error semantics are the same.
- **`ProofRequirementKind.QualifierChain = 6`** is the new enum value.
- **PriceTimesQuantity uses Dimension axis** (not Unit) for both sides, since Price declares a Dimension qualifier, not a Unit qualifier.
- **if/else pattern** used instead of switch for the `TryQualifierCompatibilityProof` method to avoid PRECEPT0025 (no wildcards) analyzer violations on the `[CatalogDU]` type.

## Files Changed

- `src/Precept/Language/Operations.cs` — ProofRequirements on 10 operations (8 money + 2 chain)
- `src/Precept/Language/ProofRequirement.cs` — QualifierChainProofRequirement record + QualifierChain meta
- `src/Precept/Language/ProofRequirementKind.cs` — QualifierChain = 6
- `src/Precept/Language/ProofRequirements.cs` — QualifierChain arm in GetMeta
- `src/Precept/Pipeline/ProofEngine.cs` — Chain handling + dimension fallback + diagnostic/fault arms
- `tools/Precept.Mcp/Tools/LanguageTool.cs` — Chain rendering in RenderProofRequirement
- `tools/Precept.Mcp/Tools/ProofsTool.cs` — QualifierChain in dual-subject check
- `test/Precept.Tests/ProofEngineTests.cs` — 19 new tests in PartB_Slice7/8/9 classes
- `test/Precept.Tests/ProofRequirementCatalogTests.cs` — Updated count and DU tests for 6th kind

## Test Results

- 193 ProofEngine tests pass (174 existing + 19 new)
- 17 ProofRequirementCatalog tests pass
- 26 pre-existing failures in TypeChecker tests (unrelated — from concurrent agent work)
