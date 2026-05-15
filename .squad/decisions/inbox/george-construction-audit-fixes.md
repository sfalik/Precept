# George — construction audit follow-up

**Date:** 2026-05-15T18:51:51.086-04:00

- **Status:** Processed — merged into `.squad/decisions.md` on 2026-05-15T23:14:11Z.

## Decision

Ship the four follow-up fixes from Frank's construction audit as one runtime slice:

1. Treat wildcard `from any` initial rows as covering every initial state they apply to during construction-guarantee validation.
2. Add `PRE0143 MaterializedFieldSelfReference` for the first assignment that materializes an omitted required field on entry and reads that field directly or through a computed dependency before any value exists.
3. Make `PRE0142 UninitializedFieldReadInInitialAssignment` walk `SecondaryExpression` as well as the primary input expression.
4. Add `PRE0144 UninitializedCrossFieldReadInInitialAssignment` for ordered cross-field undefined reads inside initial-event action chains.

## Why

The runtime guarantee here is “no path reads a value before the language has structurally established one.” The audit exposed two blind spots in construction (`from any` coverage and cross-field order) and one blind spot in omit→present materialization (transitive self-reference through computed helpers). These are correctness bugs, not message-polish bugs.

## Files touched

- `src/Precept/Pipeline/TypeChecker.Validation.FieldState.cs`
- `src/Precept/Language/DiagnosticCode.cs`
- `src/Precept/Language/Diagnostics.cs`
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs`
- `test/Precept.Tests/TypeChecker/TypeCheckerConstructionTests.cs`
- `test/Precept.Tests/TypeChecker/TypeCheckerFieldStateTests.cs`
- `test/Precept.Tests/DiagnosticsTests.cs`
- `test/Precept.Tests/SampleFieldStateRegressionTests.cs`
- `docs/compiler/diagnostic-system.md`
- `docs/language/precept-language-spec.md`
- `docs/Working/diagnostic-enforcement.md`
- `.squad/agents/george/history.md`

## Validation

- `dotnet build src\Precept\Precept.csproj --no-restore -v minimal` ✅
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build --filter "FullyQualifiedName~TypeCheckerConstructionTests|FullyQualifiedName~TypeCheckerFieldStateTests|FullyQualifiedName~DiagnosticsTests|FullyQualifiedName~SampleFieldStateRegressionTests" -v minimal` ✅ (690 passed)
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build --filter "FullyQualifiedName~D144_InitialEvent_CrossFieldReadBeforeFirstAssignment_FiresAtReadSite|FullyQualifiedName~D143_OmitToNonOmit_RequiredField_IndirectComputedSelfReference_Fires|FullyQualifiedName~D94_WildcardInitialRow_AssignsRequiredField_NoDiagnostic" -v minimal` ✅ (3 passed)
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build -v minimal` ⚠️ current workspace baseline still ends with 10 unrelated failing proof/qualifier tests after the new slice passes its focused coverage.
