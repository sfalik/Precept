# Decision: PRECEPT0024 Anti-Mirroring Enforcement Implemented

**Author:** Newman (MCP/AI Dev)
**Date:** 2026-05-07
**Status:** Done

## Context

OQ1 in `docs/compiler/type-checker.md` §13 locked the decision that `.Syntax` back-pointers on `Typed*` records must only be accessed inside `TypeChecker`. GraphAnalyzer, ProofEngine, and Builder must consume typed semantic data — never parse-tree back-pointers. The enforcement mechanism was specified as a Roslyn analyzer.

## Decision

Implemented `PRECEPT0024` as a Roslyn analyzer in `src/Precept.Analyzers/Precept0024AntiMirroringEnforcement.cs`.

- **Diagnostic ID:** PRECEPT0024
- **Severity:** Error
- **Mechanism:** `RegisterOperationAction` on `OperationKind.PropertyReference`
- **Guard:** Fires when `.Syntax` is accessed on any of 10 guarded `Typed*` record types (`TypedField`, `TypedState`, `TypedEvent`, `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, `TypedAccessMode`, `TypedStateHook`, `TypedEventHandler`, `TypedEditDeclaration`) outside the `TypeChecker` class in `Precept.Pipeline` namespace.
- **Allowed:** Access inside `TypeChecker` (including nested types). Test code uses `#pragma warning disable PRECEPT0024` where needed.
- **Type resolution:** Uses `IPropertyReferenceOperation` with namespace-qualified type checks to avoid false positives on unrelated types.

## Tests

8 tests in `test/Precept.Analyzers.Tests/Precept0024Tests.cs`:
- 4 true positives: GraphAnalyzer, ProofEngine, Builder, lambda-in-non-TypeChecker
- 4 true negatives: inside TypeChecker, non-guarded type, non-Syntax property, nested class in TypeChecker

## Impact

- Closes OQ1 from type-checker.md §13.
- No MCP surface changes required — this is a compile-time enforcement mechanism only.
