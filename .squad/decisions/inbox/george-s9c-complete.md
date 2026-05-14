# Slice 9C — ProofEngine Audit Complete

**Date:** 2025-07-14
**By:** George (Runtime Dev)
**Status:** Complete — refactoring applied

## Audit Results

### Emission Inventory (pre-refactoring)

| Emission Site | Method | Was Literal? | Now Catalog? |
|---|---|---|---|
| `CreateFaultSiteLink` — ModifierRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal `DiagnosticCode.UnprovedModifierRequirement` | ✅ Migrated to `ProofRequirements.GetMeta(kind).DiagnosticCode` |
| `CreateFaultSiteLink` — DimensionProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ✅ Migrated |
| `CreateFaultSiteLink` — QualifierCompatibilityProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ✅ Migrated |
| `CreateFaultSiteLink` — QualifierChainProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ✅ Migrated |
| `CreateFaultSiteLink` — PresenceProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ✅ Migrated |
| `CreateFaultSiteLink` — IntervalContainmentProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ✅ Migrated |
| `CreateFaultSiteLink` — LengthContainmentProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ✅ Migrated |
| `CreateFaultSiteLink` — CountContainmentProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ✅ Migrated |
| `CreateFaultSiteLink` — NumericProofRequirement | `ProofEngine.Diagnostics.cs` | ✅ Literal | ❌ Retained (documented exception) |
| `CreateDiagnostic` — all branches | `ProofEngine.Diagnostics.cs` | ✅ Literal | ❌ Retained (per-obligation formatting) |
| `UnsatisfiableInitialState` | `ProofEngine.cs:142` | ✅ Literal | ❌ N/A (not an obligation kind; separate initial-state check) |

### Strategy 7 (IntervalContainment) Verification

✅ `ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment).DiagnosticCode == DiagnosticCode.NumericOverflow` — confirmed by new regression test.

### Documented Exceptions (Legitimate Direct Emission)

1. **`NumericProofRequirement` in `CreateFaultSiteLink`** — Fails criterion (1): no stable 1:1 kind→diagnostic mapping. Maps to `DivisionByZero`, `SqrtOfNegative`, `UnguardedCollectionAccess`, or `UnguardedCollectionMutation` depending on the specific requirement's semantics and site context.

2. **`CreateDiagnostic` (all branches)** — Fails criterion (2): per-obligation formatting logic. Each branch constructs diagnostics with different parameter counts and type-specific message arguments (qualifier left/right operands, interval computed strings, length literal extraction, etc.). This is not uniform "obligation not met → emit diagnostic" logic.

3. **`UnsatisfiableInitialState` in `ProofEngine.cs`** — Not an obligation kind at all; it's a separate initial-state satisfiability check outside the obligation system.

## Changes Made

- **`src/Precept/Language/ProofRequirement.cs`** — Added `DiagnosticCode?` property to `ProofRequirementMeta` base record. Each subtype provides its stable diagnostic code; `Numeric` provides `null`.
- **`src/Precept/Pipeline/ProofEngine.Diagnostics.cs`** — Refactored `CreateFaultSiteLink` from per-type switch to catalog dispatch (`ProofRequirements.GetMeta(kind).DiagnosticCode`) with carve-out for `Numeric`.
- **`test/Precept.Tests/ProofRequirementCatalogTests.cs`** — Added 10 regression tests validating catalog diagnostic code mapping for all 9 kinds.
- **`docs/Working/diagnostic-enforcement.md`** — Checked off Slice 9C in §8 Tracker.

## Validation

- Full solution builds with 0 errors.
- All 5370+ existing tests pass (407 ProofEngine tests pass; 7 pre-existing failures in unrelated qualifier propagation tests confirmed identical before and after change).
- 10 new catalog tests all pass.
