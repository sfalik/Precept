# George t2-10 — Name binder catalog-derived resolution complete

## Commit
- `def91dbb` — `feat(t2-10): name binder — catalog-derived name resolution`

## What shipped
- `NameBinder` now treats token-catalog wildcards and broadcasts as non-name lookups via `TokenMeta.IsStateWildcard` / `IsFieldBroadcast`, so `any` and `all` no longer fall through to undeclared-state/field diagnostics.
- Computed fields bind after a declaration-order-stable topological sort. Non-cyclic forward references resolve regardless of declaration order; cyclic sets emit `CircularComputedField`.
- The repo's second state-target normalization pass lives in `TypeChecker.cs`, so Slice 10 also taught that pass to honor wildcard state anchors (`to any`, `in any`, `from any`) instead of re-emitting PRE0028 after the binder succeeds.

## Validation
- `dotnet build src\Precept\Precept.csproj`
- `dotnet test test\Precept.Tests\Precept.Tests.csproj`
- Result: 3,911 / 3,911 passing.
