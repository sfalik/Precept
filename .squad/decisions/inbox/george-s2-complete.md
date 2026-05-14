# Slice 2 Complete — Choice Value Validation (PRE0086, PRE0087, PRE0089)

**By:** George
**Date:** 2025-07-14
**Status:** Complete

## What Was Wired

Three diagnostic emission sites added to the TypeChecker expression resolution pipeline:

1. **PRE0086 `ChoiceLiteralNotInSet`** — fires when a string literal compared to a choice-typed field is not in the field's declared domain. Case-sensitive (ordinal comparison).

2. **PRE0087 `ChoiceArgOutsideFieldSet`** — fires when an event arg with a `choice` type is assigned to a choice field, and the arg's domain contains values absent from the field's domain.

3. **PRE0089 `ChoiceRankConflict`** — fires when an event arg's choice values are all valid subset members but appear in a different order than the field's declared domain order.

## Validation Hook Placement

- **Comparison path:** `ValidateChoiceComparisonLiterals` called in `ResolveBinaryOp` immediately after `RetryChoiceComparisonLiterals` (line ~638 of `TypeChecker.Expressions.cs`). Only fires for `OperatorFamily.Comparison` operators.

- **Assignment path:** Validation block added in `ResolveAction` (`TypeChecker.Expressions.Callables.cs`) within the `AssignAction` case, after the existing qualifier validation. Handles both literal assignment (PRE0086) and arg assignment (PRE0087/PRE0089).

## Infrastructure

- Added `ChoiceDomains` dictionary to `CheckContext` (field name → domain values in declaration order). Populated during `PopulateFields`.
- Added `ArgChoiceDomains` dictionary to `CheckContext` ((eventName, argName) → domain values). Populated during `PopulateEvents`.
- `ValidateChoiceArgAgainstField` is `internal static` for testability.

## Allow-List Changes

- **Gate 1:** Removed `ChoiceLiteralNotInSet`, `ChoiceArgOutsideFieldSet`, `ChoiceRankConflict`.
- **Gate 2:** Added same three codes (cross-project analyzer cannot detect test references in Precept.Tests).

## Tests (7 added)

All in `TypeCheckerStructuralTests.cs`:
- `ChoiceField_LiteralNotInSet_EmitsChoiceLiteralNotInSet`
- `ChoiceField_ValidLiteral_NoDiagnostic`
- `ChoiceField_LiteralCaseMismatch_EmitsChoiceLiteralNotInSet`
- `ChoiceArg_ValueOutsideFieldSet_EmitsChoiceArgOutsideFieldSet`
- `ChoiceArg_ValuesSubsetOfFieldSet_NoDiagnostic`
- `ChoiceArg_RankConflictsWithField_EmitsChoiceRankConflict`
- `ChoiceArg_RankMatchesField_NoDiagnostic`

## Anomalies

- 7 pre-existing ProofEngine test failures (all `PartB_Slice7`/`PartE_E3` — currency/dimension tests) from Slice 1's TypeChecker emission sites firing earlier than ProofEngine tests expect. Not caused by Slice 2 changes.
- Pre-existing PRECEPT0028 Gate 2 errors remain unresolved (cross-project visibility limitation).
