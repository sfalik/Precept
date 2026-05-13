# D94 initial-state row scope

- Outcome: commit `4c567cdc` restricted `ValidateConstructionGuarantees` to analyze only initial-event rows whose `FromState` is an initial state.
- Result: `samples/Test.precept` now compiles clean, `D94_NonInitialStateRow_NotChecked` covers the regression, and `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed at `5138/5138`.
