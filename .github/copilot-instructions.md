# Copilot Instructions for Precept

## Architecture

Precept is a domain integrity engine for .NET — a DSL runtime that binds an entity's state, data, and business rules into a single executable contract.

| Component | Path | Purpose |
|-----------|------|---------|
| Core runtime | `src/Precept/Dsl/` | Parser → type checker → expression evaluator → runtime engine |
| Language server | `tools/Precept.LanguageServer/` | LSP: diagnostics, completions, hover, go-to-definition, semantic tokens, preview |
| MCP server | `tools/Precept.Mcp/` | 5 MCP tools wrapping core APIs (see below) |
| VS Code extension | `tools/Precept.VsCode/` | Extension host: syntax highlighting, preview webview, commands |
| Copilot plugin | `tools/Precept.Plugin/` | Agent definition + 2 skills + MCP launcher |
| Sample files | `samples/` | 20 `.precept` files — canonical DSL usage examples |

Key design docs: `docs/PreceptLanguageDesign.md` (DSL semantics), `docs/RuntimeApiDesign.md` (C# API), `docs/McpServerDesign.md` (MCP tool specs), `docs/CatalogInfrastructureDesign.md` (metadata registries). See `docs/` for the full set.

## Build & Test

```bash
# Build everything
dotnet build

# Build language server only (default build task — Ctrl+Shift+B)
dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server

# Run all tests (xUnit + FluentAssertions, 666 tests across 3 projects)
dotnet test

# Run a single test project
dotnet test test/Precept.Tests/
dotnet test test/Precept.LanguageServer.Tests/
dotnet test test/Precept.Mcp.Tests/

# VS Code extension (from tools/Precept.VsCode/)
npm run compile        # Build TypeScript
npm run watch          # Watch mode
npm run loop:local     # Package + install locally (also a VS Code task)
```

**VS Code tasks** (Run Task menu): `build`, `extension: install`, `extension: uninstall`, `plugin: enable`, `plugin: disable`.

## Development Workflow

- **Runtime / language server changes** → edit `src/Precept/` or `tools/Precept.LanguageServer/` → run Build task → extension auto-detects new build, no reload needed.
- **Extension UI / grammar / TypeScript** → edit `tools/Precept.VsCode/` → run `extension: install` task → reload window.
- **MCP server** → edit `tools/Precept.Mcp/` → reload window → rebuild happens lazily on next tool invocation.
- **Plugin (agents/skills markdown)** → edit `tools/Precept.Plugin/` → reload window → changes appear immediately.

## Use the MCP Tools First

This project ships a Precept MCP server with 5 tools. **Use them as your primary research tools** before reading source code or making assumptions about the DSL:

| Tool | Purpose |
|------|---------|
| `precept_language` | Complete DSL vocabulary (keywords, operators, scopes, constraints, pipeline stages) |
| `precept_compile(text)` | Parse, type-check, analyze; returns typed structure + diagnostics |
| `precept_inspect(text, currentState, data, eventArgs?)` | Read-only preview of what each event would do |
| `precept_fire(text, currentState, event, data?, args?)` | Single-event execution for step-by-step tracing |
| `precept_update(text, currentState, data, fields)` | Direct field editing to test `edit` declarations and constraints |

Start with MCP tools for authoritative data, then read source code only for implementation details the tools don't cover.

## DSL Sample Files (.precept)

`.precept` files are interpreted by the runtime — **not** compiled by the C# build pipeline. Never run `dotnet build` or `dotnet run` to validate a `.precept` file.

To check a `.precept` file for errors:
1. Use the `get_errors` tool on the `.precept` file path (reads the VS Code Problems panel, populated by the language server).
2. Cross-check against sample files in `samples/`.

## Documentation Sync (Non-Negotiable)

When making any code, interface, test, or behavior change, keep documentation in sync in the same edit pass. Unless explicitly told not to, include documentation synchronization as part of every relevant code change.

### Source of Truth

- `README.md` — public project narrative and usage guide. Must track real implementation: update API names, behavioral semantics, examples, and feature claims on every meaningful change. Never leave aspirational claims as if implemented.
- `docs/` — canonical design decision records.
- Legacy files (`README-legacy.md`, `docs/DesignNotes-legacy.md`) — archived, do not update.

Keep updates focused and factual. If uncertain whether a claim is implemented, verify from code/tests first.

## Syntax Highlighting Grammar Sync (Non-Negotiable)

The TextMate grammar at `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` must stay in sync with the DSL parser at `src/Precept/Dsl/PreceptParser.cs`.

When any of the following change, update the grammar file in the same pass:

- New keywords added (control, action, type, or collection)
- New statement or declaration forms (e.g. a new block type like `edit`)
- New expression constructs or operators
- New collection type kinds or inner types
- Changes to identifier naming rules

### Grammar Sync Checklist

For every new or changed DSL construct, verify the grammar covers:

1. **Declaration form** — does the keyword appear at the start of a line? Add/update a named declaration pattern with capture groups for the keyword and following identifier.
2. **Keyword** — is it a control keyword (`if/else/from/on/state/event/precept/initial`) or action keyword (`set/transition/reject/rule/add/remove/…`)? Add to the correct `controlKeywords` or `actionKeywords` alternation.
3. **Type token** — is it a type name (`string/number/boolean`) or collection type (`set<T>/queue<T>/stack<T>`)? Add to `typeKeywords`.
4. **Operator** — is it a new operator symbol? Add to `operators` in priority order (multi-char before single-char).
5. **Identifier references** — identifiers in expression positions are caught by the `identifierReference` catch-all; no change needed unless a new dotted form (like `EventName.ArgName`) is introduced, in which case add a dedicated pattern before `identifierReference`.
6. **Pattern ordering** — specific patterns (declarations, dotted refs) must appear before general ones (type keywords, identifier catch-all). Verify the top-level `patterns` array order is still correct after changes.

## Intellisense Sync (Non-Negotiable)

The completions in `tools/Precept.LanguageServer/PreceptAnalyzer.cs` must stay in sync with the DSL parser whenever the language surface changes. Semantic tokens in `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` are driven by `PreceptTokenMeta.GetCategory()` via a `SemanticTypeMap` — new token types are picked up automatically from `[TokenCategory]` attributes on the `PreceptToken` enum.

When any of the following change, update the analyzer in the same pass:

- New keywords, operators, or type names
- New statement forms or block types with their own context
- New expression positions where identifiers or operators can appear
- New dotted accessor forms (e.g. `Collection.count`, `EventName.ArgName`)
- New collection kinds or inner types

### Completions Sync Checklist (`PreceptAnalyzer.cs`)

For every new or changed DSL construct:

1. **Keyword in `KeywordItems`** — is the new keyword visible in the global fallback list? Add it.
2. **Context-specific trigger** — does the keyword start or appear within a specific line position (e.g. after `from … on`, after `set =`, at the start of a block body)? Add a regex branch to `GetCompletions` that detects that position and returns the correct item set.
3. **Identifier scope** — are field names, event names, arg names, or state names valid completions in the new context? Reuse `BuildGuardCompletions` or `BuildExpressionCompletions` as appropriate, or build a new dedicated helper.
4. **Dotted member access** — if the new construct allows `Identifier.member` access, add it to the dot-trigger branch and the member suggestion list.
5. **Snippets** — if the construct has a required structure, add a snippet to the relevant snippet list.

### Semantic Tokens Note

Semantic tokens are now catalog-driven via `PreceptTokenMeta.GetCategory()`. When adding a new token kind to the `PreceptToken` enum, apply the appropriate `[TokenCategory]` attribute — the semantic tokens handler picks it up automatically. No manual handler edits needed for standard keyword/type/operator additions.

## MCP Tool Sync

The MCP server tools in `tools/Precept.Mcp/Tools/` are thin wrappers around core APIs. When core types or behavior change, check whether MCP DTOs need updates:

- When core model types change (`PreceptDefinition`, `PreceptField`, `PreceptState`, `PreceptEvent`, `PreceptTransitionRow`, etc.), check whether MCP tool DTOs in `tools/Precept.Mcp/Tools/` need corresponding updates.
- When `ConstructCatalog` or `DiagnosticCatalog` records gain or lose properties, verify `LanguageTool.cs` serialization still matches `McpServerDesign.md § precept_language` output format.
- When the fire pipeline stages change, update the static `FirePipeline` array in `LanguageTool.cs`.
- The MCP tools are **thin wrappers** — never duplicate domain logic. If a tool method exceeds ~30 lines of non-serialization code, the logic probably belongs in `src/Precept/`.

## Test Conventions

- Framework: **xUnit** with **FluentAssertions**
- Naming: `PascalCase` + `Tests` suffix (e.g. `PreceptParserTests.cs`, `PreceptRuntimeTests.cs`)
- Tests typically use `[Fact]` and `[Theory]` attributes

## DSL Authoring (Non-Negotiable)

Before writing or editing any `.precept` file or any DSL snippet, read at least one representative sample file from `samples/` to confirm current syntax conventions. Do not rely on memory or inference — read first, then write.

## Design Option Responses

When providing design-option responses, include concrete usage examples to illustrate the implementation and clarify the context of the options presented.
