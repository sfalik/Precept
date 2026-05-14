# Decision: Slice 6 — Collection Safety Extensions (PRE0100, PRE0104)

**Date:** 2025-01-27  
**Author:** George (Runtime Dev)  
**Status:** Complete (partial — PRE0099/0101 blocked on catalog)

## Summary

Wired PRE0100 (IndexBoundsGuard) and PRE0104 (MissingOrderingKey) from the diagnostic enforcement plan Slice 6. Built infrastructure for PRE0099/0101 (KeyPresenceProofRequirement, GuardHasContainsCheck strategy) but cannot fully wire them until Lookup type gets key-access accessor and put action in the catalog.

## What Was Done

1. **PRE0104 (MissingOrderingKey):** Added RequiredTraits validation in TypeChecker.Expressions.Callables.cs. When `.min`/`.max` is called on a collection whose element type lacks the Orderable trait, PRE0104 fires. Applied in both `ResolveMemberAccess` and `ResolveMethodCall` paths.

2. **PRE0100 (IndexBoundsGuard):** Refined diagnostic routing in ProofEngine.Diagnostics.cs. The existing `count > 0` proof obligation on `.at()` now routes to PRE0100 (IndexBoundsGuard) instead of the generic PRE0063 (UnguardedCollectionAccess), giving users a more specific diagnostic.

3. **PRE0099/0101 Infrastructure:** Added `KeyPresenceProofRequirement` record, `ProofRequirementKind.KeyPresence`, meta/satisfaction types, `ContainsGuardConstraint`, and `GuardHasContainsCheck` strategy. Ready for use when Lookup accessors/actions are added.

4. **Allow-list updates:** Moved IndexBoundsGuard and MissingOrderingKey from Gate 1 → emitting (removed from Gate 1). KeyPresenceSafety and KeyUniquenessGuard remain in Gate 1 (no emission site until Lookup catalog expansion).

5. **Tests:** 10 new tests in `TypeCheckerCollectionSafetyTests.cs` covering PRE0104 (string/boolean → emit, integer/decimal → clean) and PRE0100 routing.

## Blocked Items

- **PRE0099 (KeyPresenceSafety):** Requires a key-access accessor on TypeKind.Lookup (currently only has `.count`).
- **PRE0101 (KeyUniquenessGuard):** Requires a "put" action for Lookup type (currently `Add` only targets Set/Bag).

These will unblock when the Lookup type catalog is expanded with key-access operations.

## Files Modified

- `src/Precept/Language/ProofRequirementKind.cs` — added KeyPresence = 10
- `src/Precept/Language/ProofRequirement.cs` — KeyPresenceProofRequirement, meta, satisfaction
- `src/Precept/Language/ProofRequirements.cs` — GetMeta case
- `src/Precept/Pipeline/ProofEngine.cs` — ContainsGuardConstraint record
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` — GuardHasContainsCheck, WalkForContains
- `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` — routing for PRE0100, KeyPresence dispatch
- `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs` — PRE0104 RequiredTraits check
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — allow-list updates
- `test/Precept.Tests/TypeChecker/TypeCheckerCollectionSafetyTests.cs` — 10 new tests
- `test/Precept.Tests/ProofRequirementCatalogTests.cs` — updated counts for KeyPresence
- `test/Precept.Tests/Track2PhaseAToolchainRegressionTests.cs` — updated .at() expectation
- `docs/Working/diagnostic-enforcement.md` — tracker checkboxes
