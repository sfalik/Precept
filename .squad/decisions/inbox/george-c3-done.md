# George C3 done

## Decision
- `samples/inventory-item.precept` was wrong: the ensure expressions meant equality, not assignment. The sample now uses `==` on the four affected ensure lines.
- The compiler behavior stays unchanged semantically: `=` remains invalid in expression context. Instead, the parser now emits `AssignmentInExpressionContext` with an explicit `use '=='` message and recovers by consuming the right-hand side.

## Why
- The previous failure mode surfaced as a confusing downstream parse error on `because`, which hid the real mistake.
- This is a usability fix plus sample cleanup, not a grammar change.

## Validation
- `dotnet build src\Precept\Precept.csproj --nologo`
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~Precept.Tests.Parser.ParserExpressionTests.Negative_AssignmentInEnsureExpression_EmitsAssignmentInExpressionContext_AndRecoversBecauseClause|FullyQualifiedName~Precept.Tests.DiagnosticsTests.ParseStageCodes_AllHaveParseStage"`
- Compiling `samples/inventory-item.precept` through `Precept.Compiler` shows no `ExpectedToken` or `AssignmentInExpressionContext` diagnostics on sample ensure lines 150, 156, 173, and 174.

## Notes
- The full `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo` run still fails on two pre-existing unrelated tests: `ParserSlice8Tests.Parser_Bug031_InterpolatedRejectAndBecause_CompilesClean` and `ProofEngineTypedArgQualifierTests.InventoryItem_Sample_Has_No_PRE0114_Diagnostics`.
