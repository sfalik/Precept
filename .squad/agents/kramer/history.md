## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- Incomplete-code routing is safest when it reuses semantic spans plus neighboring significant tokens instead of trusting parser recovery spans alone.
- Cursor positions parked at the next token boundary belong to the preceding separator for member-access and default-value completions.
- Shared helpers (`LanguageServerComposition`, `SemanticExpressionLocator`, `SymbolNavigation`, `OutlineSymbolProjector`) are the durable way to keep the shipped host and test harness aligned.
- Versioned document updates must reject stale recompiles while preserving the unversioned fallback path for clients that omit version data.
- Modifier completions should derive from `ValueModifierMeta.ApplicableTo` and declaration-site legality so the completion surface stays catalog-truthful.
- Visible editor-color drift can come from VS Code fallback/theme ordering even when catalog metadata and semantic tokens are already correct.

## Historical Summary

- Early May 2026 tooling work established the catalog-driven language-server baseline: Phase 1 handler wiring, semantic-token color publication, trigger-character and `set` context fixes, and Slice 13/14 completion routing over semantic context instead of LS-local lists.
- Later 2026-05-10 slices closed the shared navigation and symbol stack (`SymbolNavigation`, rename, document/workspace symbols), document version ordering, completion item quality, hover completion, and typed-constant editor polish, all backed by language-server regression coverage.
- The canonical decision ledger in `.squad/decisions.md` carries the batch-level detail; this history keeps only the durable tooling baseline and newest live updates.

## Recent Updates

### 2026-05-10T12:25:21Z — Status-bar activation contract recorded
- Kept both shipped VS Code activation paths, `workspaceContains:**/*.precept` and `onLanguage:precept`, so the status bar and language server still activate in single-file and no-workspace sessions.
- Durable tooling rule: activation coverage is part of the user-visible tooling surface; repo-workspace activation alone is too narrow for Precept authoring.


### 2026-05-10T12:15:36Z — Grammar keyword fallback color fix recorded
- Kramer closed the `as`/`default` gold drift by fixing the VS Code fallback TextMate color rule in `tools\Precept.VsCode\package.json` instead of changing language-server semantic token classification.
- Validation stayed green: `SemanticTokensHandlerTests` passed 150/150, and `npm run compile` succeeded in `tools\Precept.VsCode`.

### 2026-05-10T12:25:21Z — Extension activation keeps status-bar support in single-file sessions
- The VS Code extension must retain both `workspaceContains:**/*.precept` and `onLanguage:precept` activation events so the status bar item and language server still appear in no-workspace or single-file authoring sessions.
- This is a tooling-activation durability rule, not a catalog or language-surface change.

### 2026-05-10T12:15:36Z — Boolean field modifier completion filtering landed
- `CompletionHandler` now filters field modifiers through modifier metadata and declaration-site legality, so boolean fields offer only `default`, `optional`, and `writable` instead of leaking numeric-only items like `max` and `maxplaces`.
- `CompletionHandlerTests.cs` now locks the exact boolean-valid surface, and the full language-server test project passed 150/150 after the fix.

### 2026-05-10T07:15:00Z — Slice 18 hover surface completion landed
- `HoverHandler` now reuses `SemanticExpressionLocator` so hover can project catalog-driven details for function calls, typed constants, accessors, and semantic expression sites without LS-local symbol mirrors.
- Validation stayed green: targeted hover coverage passed 15/15, then the full language-server test project passed 141/141.

### 2026-05-10T06:55:00Z — Slice 17 completion item quality landed
- Completion items now carry snippet insert text, markdown documentation, and stable sort grouping while keeping semantic symbols ahead of catalog entries.
- Validation stayed green: `CompletionHandlerTests` passed 23/23, then the full language-server test project passed 135/135.

### 2026-05-10T06:33:00Z — Slice 26 document version ordering landed
- `DocumentState` now stores versioned snapshots and rejects stale `TryUpdate(...)` calls; `TextDocumentSyncHandler` suppresses stale diagnostic publishes while retaining the unversioned fallback path.
- Targeted Slice 26 coverage passed 7/7, then the full language-server test project passed 115/115.

### 2026-05-10T05:25:00Z — Slice 20 shared symbol navigation landed
- `SymbolNavigation` now centralizes declaration/reference lookup for fields, states, events, and event args, and the server capability surface registers references plus document highlights through the shared composition path.
- The durable event-arg rule is still explicit: qualified arg references can share the broad semantic site for navigation/highlights, but rename must trim edits back to the identifier token.

### 2026-05-10T08:25:21.928-04:00 — Status bar activation gap closed
- The VS Code extension manifest now activates on onLanguage:precept as well as workspaceContains:**/*.precept, so the language-server status item appears in single-file and no-workspace editor sessions instead of only in workspaces that already contain .precept files.
- Added manifest coverage in ExtensionManifestTests to lock both activation paths, and updated docs\tooling\extension.md to document the single-file activation behavior.

