# George t2-11 — ProofEngine catalog-derived proof obligations

## Completed in
- Implementation commit: `004e68be`

## What changed
- Collection non-empty obligations now project by proof-site shape: member access stays `UnguardedCollectionAccess`, while `pop`/`dequeue` action sites emit `UnguardedCollectionMutation` and map to `CollectionEmptyOnMutation`.
- Unguarded mutation diagnostics now bind to the actual collection field name instead of falling back to `<unknown>`.
- Added regression coverage for guarded and `notempty` collection mutations plus `sqrt(abs(x))` remaining clean through the catalog-driven function return proof path.

## Validation
- `dotnet build .\\src\\Precept\\Precept.csproj --nologo --no-restore`
- `dotnet test .\\test\\Precept.Tests\\Precept.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~CollectionMutationProofTests|FullyQualifiedName~FunctionReturnProofTests"`

## Shared-tree note
- A full `dotnet test .\\test\\Precept.Tests\\Precept.Tests.csproj --nologo --no-restore` run in the shared workspace is currently red in unrelated `NameBinder\\StateWildcardTests` because concurrent uncommitted changes exist in `src\\Precept\\Pipeline\\NameBinder.cs`, `src\\Precept\\Pipeline\\TypeChecker.cs`, and matching NameBinder test files. No proof-engine overlap.
