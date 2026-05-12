# George — proof qualifier propagation + diagnostic label fix

## Context
- Requested by Shane on 2026-05-12 for `spike/Precept-V2-Radical`.
- Implement Frank's `frank-expression-qualifier-diagnostic.md` design note.

## Outcome
- Added `Match: QualifierMatch.Same` to `MoneyPlusMoney`, `MoneyMinusMoney`, `QuantityPlusQuantity`, `QuantityMinusQuantity`, `PricePlusPrice`, and `PriceMinusPrice` in `src/Precept/Language/Operations.cs`.
- Updated `ProofEngine.CreateDiagnostic()` to accept `SemanticIndex`, describe full expressions recursively, and attach resolved qualifier values to PRE0114 operand labels.
- Replaced proof-diagnostic placeholder fallbacks in `ProofEngine.cs` with expression descriptions, including the collection-access proof path.
- Added regression coverage in `test/Precept.Tests/ProofEngineTests.cs` for chained symbolic money subtraction, left-associated money addition, chained symbolic quantity subtraction, subexpression-aware PRE0114 messages, and catalog assertions for all six same-qualifier arithmetic operations.
- Updated `test/Precept.Tests/OperationsTests.cs` to reflect the expanded set of non-`Any` qualifier-match operations.

## Validation
- `dotnet build --nologo`
- `dotnet test test/Precept.Tests/ --no-build --nologo` → 4914/4914 passed
- `dotnet test --no-build --nologo` → 5507/5507 passed

## Notes
- I kept the PRE0114 catalog template unchanged and improved the emitted operand labels instead, so message quality improved without widening the public diagnostic-template contract.
- The worktree already had an unrelated `samples/inventory-item.precept` modification; this implementation does not depend on it and should be staged explicitly when committing.
