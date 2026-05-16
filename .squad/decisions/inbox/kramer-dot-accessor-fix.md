# Dot-accessor completion fix

**Author:** Kramer  
**Date:** 2026-05-16T18:32:32.380-04:00  
**Status:** Implemented

## Summary

- Updated `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` so `.` trigger completions check event receivers before generic receiver-type accessors.
- Added `CursorSemanticResolver.TryGetEventForDotTrigger(...)` in `tools/Precept.LanguageServer/CursorSemanticResolver.cs` to resolve `EventName.` against `Compilation.Semantics.EventsByName`.
- Event receivers now surface `TypedEvent.Args` as `Event argument` completion items; field/member access completion behavior remains intact.

## Why the handler ordering changed

On incomplete `EventName.` expressions, `TryGetReceiverTypeForDotTrigger(...)` can resolve the receiver expression as `TypeKind.Error`, which short-circuits the dot-trigger branch and produces an empty completion list. Checking `EventsByName` first preserves the intended event-arg completion path while leaving field/type accessor dispatch unchanged for non-event receivers.

## Tests

Added integration coverage in `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`:

- `Completions_DotTrigger_EventName_ShowsEventArgs`
- `Completions_DotTrigger_FieldName_ShowsAccessors`

## Validation

- `dotnet build tools\Precept.LanguageServer\Precept.LanguageServer.csproj --artifacts-path temp\dev-language-server`
- `dotnet test test\Precept.LanguageServer.Tests\`
