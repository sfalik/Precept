# Kramer — ActionSyntaxShape enrollment in PRECEPT0019

## What I split
- Refactored `src/Precept/Pipeline/Parser.cs` so `ParseActionByShape(ActionMeta meta, SourceSpan actionStartSpan)` is now a thin dispatcher.
- Moved each existing `ActionSyntaxShape` switch arm into its own annotated handler method.
- Added `[HandlesCatalogExhaustively(typeof(ActionSyntaxShape))]` to `ParserState` alongside the existing class-level coverage attributes.
- Confirmed `ActionSyntaxShape` enum members match the 9 parser cases exactly; no missing or extra switch arms were found.

## Final handler method names
- `ParseAssignValueAction`
- `ParseCollectionValueAction`
- `ParseCollectionIntoAction`
- `ParseFieldOnlyAction`
- `ParseCollectionValueByAction`
- `ParseInsertAtAction`
- `ParseRemoveAtIndexAction`
- `ParsePutKeyValueAction`
- `ParseCollectionIntoByAction`

## Default arm decision
- Kept the `default:` recovery arm returning `MalformedAction`.
- Reason: this preserves the parser's prior fallback behavior exactly and avoids introducing a behavior change in a refactor-only slice, even though PRECEPT0019 should make the path unreachable in normal catalog-driven operation.

## Verification notes
- Clean verifier worktree (`precept-architecture-kramer-verify`):
  - `dotnet build` succeeded, but not clean: 2 pre-existing `VSTHRD200` warnings in `tools/Precept.LanguageServer/LanguageServerStubs.cs`.
  - `dotnet test test/Precept.Analyzers.Tests/Precept.Analyzers.Tests.csproj` passed: 272/272.
  - `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~Precept.Tests.ActionsTests|FullyQualifiedName~Precept.Tests.Parser.ActionChainTests"` passed: 64/64.
  - Full `dotnet test test/Precept.Tests/Precept.Tests.csproj` did not stay green in that clean verifier baseline: 3611 total, 3609 passed, 2 failed in unrelated `TokensTests` assertions.
- Current shared workspace:
  - Root `dotnet build` is blocked by unrelated in-progress changes outside this slice (LanguageServer and analyzer compile failures already present in the working tree), so the requested clean 0-warning/0-error root validation could not be reproduced safely without disturbing other users' work.
