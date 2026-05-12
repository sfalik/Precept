# D4 Complete — Scalar-Op Qualifier Propagation Fix

**Date:** 2026-05-11  
**Author:** George (Runtime Dev)

## What Changed

### 5 source files + 1 test file

1. **`src/Precept/Language/Operation.cs`** — Added `InheritFromQualifiedOperand` to `ResultQualifierPolicy` enum.

2. **`src/Precept/Pipeline/SemanticIndex.cs`** — Added `QualifiedOperandInherited : QualifierBinding` record to the QualifierBinding DU.

3. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`** — Extended `MapQualifierBinding()` with a new branch that returns `QualifiedOperandInherited` when the policy is `InheritFromQualifiedOperand`.

4. **`src/Precept/Language/Operations.cs`** — Set `ResultQualifierPolicy.InheritFromQualifiedOperand` on 6 scalar operations: `MoneyTimesDecimal`, `MoneyDivideDecimal`, `QuantityTimesDecimal`, `QuantityDivideDecimal`, `PriceTimesDecimal`, `PriceDivideDecimal`.

5. **`src/Precept/Pipeline/ProofEngine.cs`** — Added transitive qualifier resolution through `TypedBinaryOp` subjects in `ResolveQualifierOnAxis()`, plus two helper methods: `ResolveQualifierFromExpression()` and `ResolveFieldQualifier()`.

6. **`test/Precept.Tests/ProofEngineTests/ScalarOpQualifierPropagationTests.cs`** — 10 new tests covering all 6 scalar ops, chained ops, bidirectional order, cross-currency diagnostics, and the original repro case.

## Test Results

- **4831 tests pass** (4821 baseline + 10 new), zero failures.
- The pre-existing MCP test `Language_SyntaxReferenceMirrorsSourceAndExamplesCompile` now passes the "Money and quantity typed fields" pattern. It still fails on an unrelated pattern ("Exhaustive rejection rows") due to a missing `BuildPatternDocument` case — that's a separate issue, not D4.

## Surprises

- Divide tests initially failed because the proof engine requires `nonzero` (or equivalent) on the divisor field — `default 2` alone isn't enough. Fixed by adding `nonzero` modifier to test divisor fields. The original repro case divides by literal `100`, which the proof engine handles directly.
