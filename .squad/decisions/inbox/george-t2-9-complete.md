# George t2-9 — TypeChecker catalog-derived operator typing

## Completed in
- Implementation commit: `b7868d60`

## What changed
- `TypeChecker.Expressions` now resolves operator result types from `OperatorMeta.ResultType` / `ResultTypePolicy`, covering boolean operators, lookup `for`, `contains`, and arithmetic result projection from the operations catalog.
- Choice literals in comparison position now contextual-type against `choice` operands, and quantifier bindings preserve the case-insensitive qualifier of their collection element type.
- Modifier validation now emits `RedundantModifier` for subsumed constraints, enforces bound-pair ordering, and keyed `queue` / `log` field types now resolve to `QueueBy` / `LogBy` so `.peekby` works.
- Added regression coverage in `OperatorTypingTests` and `ModifierValidationTests`, plus updated existing proof/type-checker/catalog tests for the new behavior.

## Validation
- `dotnet build .\\src\\Precept\\Precept.csproj --nologo --no-restore`
- `dotnet test .\\test\\Precept.Tests\\Precept.Tests.csproj --nologo`

## Shared-tree note
- Left unrelated untracked working docs (`docs\\Working\\frank-grammar-*.md`) untouched.
