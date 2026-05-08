# Soup-Nazi R4 Round 2 Done

- Closed the 4 remaining GraphAnalyzer R4 items in `test/Precept.Tests/GraphAnalyzerTests.cs`.
- TQ1: renamed `Analyze_RequiredState_DominatesTerminalsAndForwardsFacts` to `Analyze_RequiredState_ProducesDominanceFact` without changing its assertions.
- EC5: split the prior conflated event-coverage scenario into isolated facts for zero handlers (`Analyze_EventWithNoHandlers_EmitsUnhandledEventDiagnostic`) and partial coverage (`Analyze_EventWithPartialCoverage_EmitsNoDiagnostic`), while keeping `Analyze_EventCoverage_IsEventLevelAcrossGuardedRows` focused on guarded-row aggregation.
- EC6: added `Analyze_RejectOutcome_CreatesSelfEdge`.
- Gap 8: added `Analyze_NoTransitionOutcome_CreatesSelfEdge`.
- Validation: `dotnet test test/Precept.Tests/` passed with 3385/3385 tests green.
- `GraphAnalyzerTests.cs` now contains 20 `[Fact]` tests.
