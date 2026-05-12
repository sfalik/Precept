# George C4 Done — TypedArgRef qualifier resolution

**Date:** 2026-05-11T23:29:22.2031046-04:00  
**Scope:** C4 / BUG-A in `ProofEngine`

## Decision

Implement the narrow ProofEngine fix for direct event-argument qualifier resolution:

1. `GetFieldName(TypedExpression?)` now maps `TypedArgRef` to `ArgName` so proof diagnostics can name direct event args.
2. `ResolveQualifierOnAxis()` now checks `TypedArgRef.DeclaredQualifiers` before falling back to `semantics.FieldsByName`, including the existing Unit→Dimension and Dimension→TemporalDimension fallback behavior.

## Validation

- `dotnet build src\Precept\Precept.csproj --nologo` ✅
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo --filter ProofEngineTypedArgQualifierTests` ✅
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo` ⚠️ one pre-existing unrelated failure remains in `ParserSlice8Tests.Parser_Bug031_InterpolatedRejectAndBecause_CompilesClean` because `AlwaysRejecting` is now emitted as a warning.
- `samples/inventory-item.precept` PRE0114 count dropped from 73 to 66.

## Notes

The focused C4 fix closes the direct `TypedArgRef` blind spot, but the remaining sample PRE0114s still involve composite operand subtrees that resolve to `<unknown>` in diagnostics. Those require follow-up proof-expression/result-qualifier work, not additional direct arg lookup changes.
