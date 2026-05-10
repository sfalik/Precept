Slice 9 complete on `Precept-V2-Radical`.

- Added `tools/Precept.LanguageServer/Handlers/FoldingRangeHandler.cs` with OmniSharp 0.19.9 `IFoldingRangeHandler` using `FoldingRangeRequestParam` and `FoldingRangeRegistrationOptions`.
- Folding ranges are construct-span based only; multi-line constructs map to region folds with 0-based LSP lines.
- Added `test/Precept.LanguageServer.Tests/FoldingRangeHandlerTests.cs` covering multi-line, single-line, and empty-source cases.
- Commit: `453e690a` (`feat(ls): Slice 9 — FoldingRangeHandler`)
- Validation: IDE diagnostics clean for both new files; `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed (3737/3737).
- Remaining blocker in repo baseline: `dotnet build/test` for `Precept.LanguageServer` currently fails before LS test execution due pre-existing `SemanticTokensHandler.CreateRegistrationOptions` access-modifier mismatch.

Slice 1 complete on `Precept-V2-Radical`.

- Added `test/Precept.LanguageServer.Tests/DiagnosticProjectorTests.cs` covering error/warning severity projection, 0-based range mapping, `Source = "precept"`, and empty-diagnostic projection using real `Compiler.Compile(...)` compilations.
- Extended `test/Precept.LanguageServer.Tests/LspTestHost.cs` with publish-diagnostics capture via `WhenPublishDiagnosticsAsync(...)` and client-side `.OnPublishDiagnostics(...)` wiring.
- Added `test/Precept.LanguageServer.Tests/DiagnosticPublishIntegrationTests.cs` verifying `DidOpenTextDocument` on invalid Precept source publishes non-empty diagnostics.
- Commit: `568ab5cc` (`feat(ls): Slice 1 — diagnostic projection and publish tests`)
- Validation: `dotnet build test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed; `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed (20/20); `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed (3737/3737).

Slice 4 complete on `Precept-V2-Radical`.

- Added `tools/Precept.LanguageServer/SlotContext.cs` with token-at/before cursor resolution, catalog-aware role checks, and construct lookup for modifier-domain scoping.
- Added `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` implementing OmniSharp `ICompletionHandler` with catalog-driven top-level/type/modifier completions plus semantic state/event/field target completions.
- Added `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs` covering top-level completions, no-document incomplete results, and declared-state target suggestions; also included local client extension shims so existing LS protocol tests compile under OmniSharp 0.19.9.
- Commit: `1ec3c7d5` (`feat(ls): Slice 4 — CompletionHandler, catalog-driven completions`)
- Validation: `dotnet build tools\\Precept.LanguageServer\\Precept.LanguageServer.csproj` passed; `dotnet build test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed; `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed (20/20); `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed (3737/3737).

Slice 5 complete on `Precept-V2-Radical`.

- Added `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` implementing OmniSharp `IHoverHandler` with markdown hover content for keyword tokens plus semantic hover docs for fields, states, events, and declared event arguments.
- Added `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs` covering keyword hover, declared-field hover, whitespace/null hover, and missing-document behavior.
- Validation: clean isolated worktree using current tracked branch state passed `dotnet build tools\\Precept.LanguageServer\\Precept.LanguageServer.csproj`, `dotnet build test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj`, `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` (7/7), and `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` (3737/3737).
- Note: the shared working tree currently contains concurrent untracked LS slice files, so clean validation was run in a temporary git worktree and then removed.

Slice 2 complete on `Precept-V2-Radical`.

- Added `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` implementing OmniSharp 0.19.9 semantic tokens with catalog-driven legend generation, per-document token documents, and lexical projection from `Compilation.Tokens.Tokens`.
- Added `test/Precept.LanguageServer.Tests/SemanticTokensHandlerTests.cs` covering keyword emission, null-semantic-token skipping, and 0-based span projection; also included narrow OmniSharp client compatibility shims needed for the existing LS test host/tests under 0.19.9.
- Validation: `dotnet build tools\\Precept.LanguageServer\\Precept.LanguageServer.csproj` passed; `dotnet build test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed; `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed (20/20); `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed (3737/3737).

Slice 7 complete on `Precept-V2-Radical`.

- Added `tools/Precept.LanguageServer/LevenshteinDistance.cs` with case-insensitive iterative DP Levenshtein computation.
- Added `tools/Precept.LanguageServer/DiagnosticEnricher.cs` with diagnostic projection, suggestion-pool construction from `SuggestionSource`, alphabetical tiebreaking, and a distance-0 guard that suppresses alternate suggestions when a normalized exact match exists.
- Added `test/Precept.LanguageServer.Tests/DiagnosticEnricherTests.cs` covering closest-match enrichment, no-match cutoff, alphabetical tiebreaking, empty pools, and normalized exact-match suppression using real `Compiler.Compile(...)` inputs.
- Validation: `dotnet build tools\\Precept.LanguageServer\\Precept.LanguageServer.csproj` passed; `dotnet build test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed; `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed (27/27); `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed (3740/3740).

Slice 6 complete on `Precept-V2-Radical`.

- Added `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs` implementing OmniSharp go-to-definition for field/state/event/argument reference spans via `Compilation.Semantics` and `DiagnosticProjector.ToRange(...)`.
- Added `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs` implementing catalog-driven document outline generation from `ConstructManifest.Constructs` using `Meta.IsOutlineNode` / `Meta.OutlineSymbolTag` with slot-based name extraction.
- Added `test/Precept.LanguageServer.Tests/DefinitionHandlerTests.cs` covering field, state, event, argument, and empty-result definition lookups through the `internal static` entrypoint.
- Added `test/Precept.LanguageServer.Tests/DocumentSymbolHandlerTests.cs` covering outline-node emission plus Property/Enum/Function symbol-kind mapping.
- Validation: `dotnet build tools\\Precept.LanguageServer\\Precept.LanguageServer.csproj` passed; `dotnet build test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed; `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed (36/36); `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed (3740/3740).
- Commit: `e144d92e` (`feat(ls): Slice 6 — DefinitionHandler and DocumentSymbolHandler`)

Slice 3 overlay complete on `Precept-V2-Radical`.

- Updated `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` to extend the semantic-token legend with `property`, `enum`, `function`, and `parameter` overlays sourced from `Compilation.Semantics` / `SemanticIndex`, including `ArgReferences`.
- Added `ProjectIdentifierTokens(...)` plus `AddIdentifierTokens(...)`; `Tokenize(...)` now merges lexical and identifier projections in source order before pushing so OmniSharp delta encoding stays valid when overlays start earlier than the last lexical token.
- Added `test/Precept.LanguageServer.Tests/SemanticTokensHandlerTests.cs` coverage for legend identifier types, field declaration property tokens, event-arg declaration parameter tokens, and arg-reference parameter tokens.
- Validation: `dotnet build tools\\Precept.LanguageServer\\Precept.LanguageServer.csproj` passed; `dotnet build test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed; `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` passed (40/40); `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` passed (3740/3740).
- Commit: `75b52baa` (`feat(ls): Slice 3 overlay — SemanticTokensHandler identifier overlays (Pass 2)`)

## Slice 8: CodeActionHandler — 2026-05-09 21:03:40-04:00
- Files created: tools\\Precept.LanguageServer\\Handlers\\CodeActionHandler.cs; test\\Precept.LanguageServer.Tests\\CodeActionHandlerTests.cs
- Files modified: tools\\Precept.LanguageServer\\DocumentState.cs; tools\\Precept.LanguageServer\\Handlers\\TextDocumentSyncHandler.cs; tools\\Precept.LanguageServer\\Program.cs; test\\Precept.LanguageServer.Tests\\LspTestHost.cs; tools\\Precept.VsCode\\package.json; tools\\Precept.VsCode\\src\\extension.ts
- Tests: 3802/3802 passing (Precept.LanguageServer.Tests 62/62, Precept.Tests 3740/3740)
- Commit: c752af087c43f68db53f8e5578d4e444603468a8
