## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.
- Pushes for full-surface coverage matrices, honest red/green pressure, and regression anchors that match the real AST/runtime shape.

## Learnings

- Real static catalogs are the executable language contract; prefer tiny synthetic fixtures around them over mocked metadata.
- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Expression diagnostics often need a full parser host (rule/transition row), not a bare expression helper.
- DSL keywords are common identifier traps in tests; use non-keyword names unless the test is explicitly about keyword handling.
- Analyzer suites need partition/exclusion assertions, diagnostic severities, and direct topology checks — not just happy-path outcomes.
- ProofEngine coverage needs end-to-end strategy exercise, not enum/count assertions alone.
- Optional developer-downloaded artifacts should use discovery-time skips, not runtime exceptions.
- Slot-context routing for action chains cannot rely on parsed spans alone; `by`/`at` tail separators may need explicit token-aware coverage.
- Language-server completion regressions that depend on trigger characters should use the real trigger in the test request; otherwise the suite can miss the user-visible fallback surface.
- TextMate grammar regressions are honest when they assert the generated `tools\Precept.VsCode\syntaxes\precept.tmLanguage.json` capture order directly; declaration overrides must appear before generic `#grammarKeywords` fallback or gold scope silently swallows field syntax like `default`.
- Track 2 compiler regressions need full `Compiler.Compile` fixtures so parser, binder, and proof-stage misroutes surface together; expression-only helpers miss wrong-code failures like `Steps.at(2)` reporting `DivisionByZero` instead of collection access safety.
- Removing metadata flags must shift coverage from boolean capability checks to slot-list shape and real parse behavior; this batch deleted `SupportsPreVerbWhenGuard` assertions, rewired state/event ensure catalog expectations to 4-slot `[Target, Guard, Ensure, Because]` shapes, and added guarded/unguarded parser coverage for StateEnsure, StateAction, EventEnsure, TransitionRow, and AccessMode plus a regression anchor rejecting legacy post-verb access-mode guards.
- Agent-facing follow-up for removed construct metadata needs both sides locked: MCP JSON must prove `supportsPreVerbWhenGuard` disappeared entirely, and LS parser-context tests must prove `in Draft when IsOwner modify Amount editable` still routes guard text as expression context before `modify` hands control to the field-target slot.

## Historical Summary

- 2026-05-01 through 2026-05-08 test work established the durable posture: convert review findings `into` shipped tests, keep sample-file gates live, and use real-catalog fixtures for parser/type-checker/analyzer coverage.
- 2026-05-09 locked the ProofEngine Phase 2 matrix, the LanguageTool coverage closeout, and the LS slice-audit pattern where missing behavioral coverage becomes concrete regression anchors instead of advisory notes.
- The canonical decision ledger in `.squad/decisions.md` carries the batch-level detail; this history now keeps only the active testing baseline and most recent durable updates.

## Recent Updates

### 2026-05-10T12:45:39Z — Track 2 Slice 1 regression suite closed green
- Added `Track2PhaseAToolchainRegressionTests` coverage for wildcard state targets, broadcast field targets, collection-access diagnostics, and dual-use `min` / `max` call leaders, plus `Track2PhaseATokenCatalogTests` that lock the new `TokenMeta` fields and their exact token sets.
- The suite now fixes the intended BUG-039 red by proving `Steps.at(2)` reports `UnguardedCollectionAccess` instead of the arithmetic `DivisionByZero` family, while BUG-001, BUG-006, BUG-026, BUG-037, BUG-051, and token-shape drift all stay locked.
- Final validation closed fully green at 3824/3824 `Precept.Tests` after George's production fix landed.

### 2026-05-10T12:25:21Z — VS Code status item regression contract closed
- Added `test\Precept.VsCode.Tests` coverage that fails if extension activation stops creating/showing the language-server status item, if the click command wiring disappears, or if state updates stop keeping the item visible across starting/ready/restarting/error/stopped surfaces.
- Clarified `docs\tooling\language-server.md` and recorded the mixed validation seam: the batch closed, but a full `test\Precept.LanguageServer.Tests` rebuild is still blocked by the unrelated `DocumentSymbolHandlerTests.cs` `SourceSpan(offset: ...)` compile error.

### 2026-05-10T08:25:21.928-04:00 — VS Code status bar regression anchor closed
- Extended `ExtensionManifestTests.cs` to lock the shipped VS Code language-server status item contract: activation must create, wire, and show the item, and `updateLanguageServerStatusItem` must keep it visible across starting/ready/restarting/error/stopped surfaces.
- Clarified `docs/tooling/extension.md` so the status bar contract is explicit: the left-aligned `Precept` item is always visible after activation begins and opens launch-mode details on click.
- Validation: direct source/manifest contract checks and `npm run compile` from `tools\Precept.VsCode` stayed green; full `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore` remains blocked by pre-existing `DocumentSymbolHandlerTests.cs` `SourceSpan(offset: ...)` compile errors unrelated to this status-bar coverage.

### 2026-05-10T12:15:36Z — Grammar keyword regression coverage closed
- Locked `as` and field-level `default` against the shipped TextMate grammar surface so fallback highlighting cannot silently repaint those grammar keywords gold.
- Added honest regression coverage in the language-server/TextMate grammar test layer, updated the grammar-generator override, regenerated `tools\Precept.VsCode\syntaxes\precept.tmLanguage.json`, and kept the shipped grammar as the asserted surface.
- Validation closed fully green at 4325/4325 tests for the reported batch.


### 2026-05-10T12:15:36Z — Boolean field modifier regression closed
- Tightened `CompletionHandlerTests.cs` so the boolean-field modifier surface is derived from modifier metadata, asserted as exactly `default`, `optional`, and `writable`, and guarded against numeric leaks such as `max` and `maxplaces`.
- Synced `docs\tooling\language-server.md`; targeted regression coverage and the full language-server test project both stayed green.

### 2026-05-10T05:50:00Z — Slice 25 selection-range acceptance staged
- Added `SelectionRangeHandlerTests.cs` with the two approved Slice 25 acceptance cases. The tests compile a real guard-clause fixture and derive the expected expansion chain from actual compilation artifacts: token span, enclosing guard expression span, guard-slot span, and construct span.
- Locked the multi-position contract by requesting the literal before the identifier even though it appears later in the source, so any handler that sorts results instead of preserving request order will fail.
- Validation: `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore --filter "FullyQualifiedName~SelectionRangeHandlerTests"` is intentionally red at 2/2 failures because `Precept.LanguageServer.Handlers.SelectionRangeHandler` does not exist yet.

### 2026-05-10T05:16:00Z — Slice 26 version-ordering acceptance staged
- Added `DocumentStateVersioningTests.cs` as compile-safe red coverage for the approved Slice 26 API contract: the tests reflect for `DocumentState.TryUpdate(int, Compilation, IReadOnlyDictionary<DiagnosticKey, SuggestionInfo>)` and `Version`, then assert older versions are rejected while newer versions replace both compilation and suggestions.
- Added `DidChange_OutOfOrderVersions_PublishesNewestDiagnosticsOnly` to `DiagnosticPublishIntegrationTests.cs`, locking the protocol contract that a stale `didChange` version must not emit a second diagnostics publish after a newer invalid version already won.
- Validation: targeted Slice 26 run is intentionally red at 3/7 failures — two reds for the missing `TryUpdate`/`Version` API surface and one red because `TextDocumentSyncHandler` still accepts and publishes the stale version-2 change.

### 2026-05-10T04:36:29Z — Slice 13 slot-context acceptance closed
- Expanded `SlotContextResolverTests.cs` to the full approved 13-case matrix: action-chain arrow/verb/``into``, `=`/`by`/`at`, guard/compute/ensure/rule expressions, event-arg `default`, field-modifier `default`, and inner collection type after `of`.
- Locked the production seam: `enqueue ... by ...` still leaves `ActionChainSlot.Span` truncated before `by`, so LS slot-context routing must treat post-span `by`/`at` tokens as action-chain expression positions.
- Validation stayed green at both levels: filtered slot-context coverage passed 13/13 and `test\Precept.LanguageServer.Tests` passed 88/88.

### 2026-05-10T03:13:51Z — Toolchain bug test-strategy verdict merged
- The 52-bug gap audit is now a durable team decision: keep the real static catalogs as the executable language contract and build small synthetic stage fixtures around them instead of mocking metadata.
- Priority coverage layers are now explicit — MCP definition-surface matrices, parser routing/disambiguation tests from `Constructs.Entries`, keyword-collision/accessor tests from real catalog names, TypeChecker catalog-consumer tests, and hook-specific pipeline tests.

### 2026-05-09T14:14:17Z — LanguageTool coverage review closed
- Reviewed `LanguageToolTests.cs` against `McpServerDesign.md`, converted the gap list `into` shipped tests, and closed the batch green at 19/19 for `test/Precept.Mcp.Tests`.
- Remaining `dotnet test --no-build -q -m:1 /nr:false` failures stayed isolated to the pre-existing language-server baseline and did not implicate `LanguageTool`.

### 2026-05-09T04:35:00Z — ProofEngine Phase 2 suite validated
- The 158-test `ProofEngineTests.cs` matrix for S1-S13 stayed fully green after George's follow-up fixes.
- The recorded red cases flushed out forwarding-fact suppression drift and the Strategy 2 null-guard bug before branch closeout.

### 2026-05-10T00:55:06.1578637-04:00 — Slice 14 completion coverage closed
- Added five Slice 14 acceptance tests in `CompletionHandlerTests.cs`: action verbs from `Actions.All`, action-chain field-targets after both direct verbs and `into`, expression completions for fields/event args/functions/boolean literals, `TypeMeta.Accessors` member access, and argument-default parity with expression completions.
- Coverage finding: completion routing at token boundaries must treat a cursor parked at the start of the next token as belonging to the preceding separator (`.` / `default`), or member-access and arg-default requests collapse to the wrong completion surface. The boundary fix is now locked by the new member-access + arg-default tests.
- Validation: `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore` -> 94/94 green.

### 2026-05-10T05:05:00Z — Slice 20 navigation coverage staged
- Landed red Slice 20 acceptance tests in `ReferencesHandlerTests.cs` and `DocumentHighlightHandlerTests.cs`, plus `SymbolNavigationHandlerTestHelpers.cs` so the suite already asserts declaration + all-site coverage for fields, states, events, and arguments as soon as Kramer lands the handlers.
- Locked the event-argument edge: qualified arg references must round-trip through the full `Event.Arg` semantic site span, not just the trailing arg identifier, because both references and highlights query that wider site in the new tests.
- Validation: baseline full suite was green at 4265/4265; after staging Slice 20 coverage, `dotnet test` is intentionally red only on the six new Slice 20 tests (4271 total, 4265 green, 6 red pending handler implementation).

### 2026-05-10T05:18:00Z — Slice 23 document-symbol acceptance staged
- Added the four approved `DocumentSymbolHandlerTests` acceptance cases for field/state/event/precept selection ranges, all asserting the identifier-facing selection span instead of the handler's current construct-keyword span.
- Important seam: for states, the approved Slice 23 contract is the current semantic `TypedState.NameSpan`, which today still includes trailing state modifiers (`initial`, etc.); the test locks that exact runtime contract rather than inventing a narrower token-only span.
- Validation: targeted `DocumentSymbolHandlerTests` now run 8 total with 4 legacy greens and 4 intentional reds; full `test\Precept.LanguageServer.Tests` is red only on those four new Slice 23 tests (104 total, 100 green, 4 red pending handler work).
