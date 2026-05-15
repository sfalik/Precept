# Kramer — action-chain continuation completion

- Date: 2026-05-15T18:47:10.829-04:00
- Requested by: Shane
- Status: Processed — merged into `.squad/decisions.md` on 2026-05-15T23:14:11Z.

## Context

`SlotContextResolver.TryGetActionChainContext` lost completion routing for a continuation `->` when the parser had not yet extended the enclosing action-chain construct span onto the new line. In `samples/Test.precept`, that made the second `->` fall through to the empty fallback lane.

## Decision

When `construct is null` and the current token is `Arrow`, do a bounded backward scan over recent significant tokens. If the scan finds a prior action verb and then a prior `->` at the same or deeper indentation, classify the site as `InActionVerb` instead of falling through.

## Validation

- Added regression test `Completions_ActionChainContinuationArrow_UsesActionItems`.
- `dotnet build tools\Precept.LanguageServer\Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server --nologo` succeeded.
- `dotnet test test\Precept.LanguageServer.Tests\ --nologo` succeeded with 320/320 passing.
