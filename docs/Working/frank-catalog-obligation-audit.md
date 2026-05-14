# Frank audit: catalog-driven obligation generation

## Core fix direction

Replace type hardcoding with a catalog-driven obligation generator based on modifier metadata (`ApplicableTypes`, `ProofSatisfactions`) so any declared constraint on any supported type gets compile-time proof obligations automatically.

## Prior findings snapshot

- Current set-action obligation generation is hardcoded to `decimal`/`number` and skips `integer` and business-qualified numeric families.
- `money`, `quantity`, `price` and `exchangerate` constraints can be declared but are not consistently enforced at compile time through obligation emission.
- `string` (`minlength`/`maxlength`) and collection (`mincount`/`maxcount`) constraints also need obligation-generation coverage on mutation paths.
- Temporal types currently do not declare `min`/`max`, so there is no temporal bounds obligation gap in the same sense.

## Evidence anchors from prior audit

- `src/Precept/Language/Actions.cs` — hardcoded type filtering in interval-containment obligation generation.
- `src/Precept/Language/Modifiers.cs` — constraint applicability and proof metadata coverage.
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` — discharge side already reads `ProofSatisfactions` broadly.

## Recommended remediation shape

1. Derive obligation emission from field modifiers and catalog metadata, not `TypeKind` hardcoded checks.
2. Emit obligations for all declared constraints that have proof semantics in metadata.
3. Add integration tests per constrained family (numeric, business, string, collection) to ensure declared constraints cannot be silently ignored.
4. Keep architecture metadata-driven: catalog declares behavior; pipeline consumes metadata.
