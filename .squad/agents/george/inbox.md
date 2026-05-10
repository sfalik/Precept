## Slice 3 core — ArgReference recording
- Added ArgReference record and ImmutableArray<ArgReference> ArgReferences to SemanticIndex
- Added ArgReferences list to CheckContext
- Populated at two TypedArgRef resolution sites in TypeChecker.Expressions.cs
- Added ArgReferences: ctx.ArgReferences.ToImmutableArray() in TypeChecker.cs
- 3 tests added in ArgReferenceTests.cs
- All Precept.Tests pass
- Commit: cba898b7
