## Slice 3 core — ArgReference recording
- Added ArgReference record and ImmutableArray<ArgReference> ArgReferences to SemanticIndex
- Added ArgReferences list to CheckContext
- Populated at two TypedArgRef resolution sites in TypeChecker.Expressions.cs
- Added ArgReferences: ctx.ArgReferences.ToImmutableArray() in TypeChecker.cs
- 3 tests added in ArgReferenceTests.cs
- All Precept.Tests pass
- Commit: cba898b7

## 2026-05-17T09:00:00Z — Slice E complete
- PRE0115 initial-state satisfiability now skips default-value checking for precepts with construction handlers, matching the Pattern A materialization semantics.
- Added narrow proof regressions for construction-row exemption vs. non-construction PRE0115 emission.
- `slice-docs-samples` is now unblocked: both slice-8b and slice-E are done.
