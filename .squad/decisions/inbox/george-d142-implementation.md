# George — D94/D142 implementation notes

**Date:** 2026-05-15T18:30:16-04:00

- **Status:** Processed — merged into `.squad/decisions.md` on 2026-05-15T23:14:11Z.

## What shipped

- Closed the stateless D94 blind spot by making construction validation inspect stateless initial handlers (and null-from-state rows when present) instead of returning early.
- Added `PRE0142 / UninitializedFieldReadInInitialAssignment` so the type checker now rejects `set X = X + ...` on an initial event when `X` has no default and no prior assignment in that action chain.
- Wired the new validation immediately after construction guarantees in the type-checker pipeline.
- Added runtime/diagnostic metadata, construction tests, diagnostics metadata coverage, and typed-constant test helpers so unrelated typed-constant fixtures stop failing on construction diagnostics they are not about.
- Added the new emitted diagnostic to the Gate 2 allow-list because the analyzer cannot see cross-project test references in `test/Precept.Tests/`.

## Validation

- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --filter "FullyQualifiedName~DiagnosticsTests|FullyQualifiedName~TypeCheckerConstructionTests|FullyQualifiedName~TypeCheckerTypedConstantTests" -v minimal` ✅
- `dotnet build src\Precept\Precept.csproj --no-restore -v minimal` ✅
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore -v minimal` ❌ still ends at the branch baseline: 9 unrelated failures in existing proof/quantity suites (`ProofEngineTests`, `ProofEngineTypedArgQualifierTests`, `TypeCheckerQuantityNormalizationTests`).
