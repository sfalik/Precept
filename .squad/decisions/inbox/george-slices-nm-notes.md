# George — Slice N + Slice M Notes

## Summary of changes made
- Slice N: Updated type-checker bound handling so bare numeric `min`/`max` bounds on `quantity` fields no longer trigger PRE0018 type mismatch and no longer trigger PRE0133 when the field has an explicit unit qualifier (`in 'unit'`).
- Slice M: Added PRE0138 (`CountDimensionBoundsAmbiguous`) and wired bound validation to emit PRE0138 for bare numeric bounds on `quantity of 'count'` (dimension-only count) instead of PRE0133.
- Added/updated TypeChecker tests in `TypeCheckerQualifierCompatibilityTests` to cover:
  - `quantity in 'kg' min 0 max 100` => no PRE0018/PRE0133
  - `quantity of 'count' max 4` => PRE0138 and no PRE0018/PRE0133

## Diagnostic code sites and rationale
- Slice N PRE0018 suppression site: `src/Precept/Pipeline/TypeChecker.cs` in min/max bound type assignment checks.
  - Rationale: This is the direct `IsAssignable(Integer|Decimal, Quantity)` gate producing the false-positive PRE0018.
- Slice N PRE0133 suppression site: `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` in `ValidateBoundQualifierCompatibility`.
  - Rationale: This is where plain numeric bound values (`ExtractedBoundValue` with empty qualifiers) trigger PRE0133. Added an early skip for explicit-unit quantity fields to avoid risky qualifier synthesis changes.
- Slice M PRE0138 site: `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` before generic PRE0133 emission in the same compatibility path.
  - Rationale: For count-dimension-only qualifiers (`of 'count'`), bare numeric bounds are semantically ambiguous; this is the narrowest, lowest-regression interception point.

## Validation snapshot
- `dotnet build src\Precept\Precept.csproj` => **Succeeded**
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build` => **Failed (pre-existing suite state)**
  - Summary: total 5522, failed 15, succeeded 5507, skipped 0
- Focused regression check:
  - `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build --filter "FullyQualifiedName~TypeCheckerQualifierCompatibilityTests"` => **Passed**
  - Summary: total 11, failed 0, succeeded 11, skipped 0
