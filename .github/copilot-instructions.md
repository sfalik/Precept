# Copilot Instructions for Precept

## Use the MCP Tools First

This project ships a Precept MCP server with 6 tools. **Use them as your primary research tools** before reading source code or making assumptions about the DSL:

- **`precept_language`** — returns the complete DSL vocabulary (all keywords by category, operators with precedence, expression scopes, constraints, fire pipeline stages, outcome kinds). Call this first when you need to understand what the language supports.
- **`precept_schema(path)`** — returns the full typed structure of a `.precept` file (fields with types/defaults/nullability, events with args, states, transitions with guard branches). Use this to understand any specific precept file.
- **`precept_validate(path)`** — parse + compile with structured diagnostics. Use this instead of `dotnet build` to check a precept file.
- **`precept_audit(path)`** — BFS reachability analysis: unreachable states, dead ends, terminal states, orphaned events. Use this to assess structural quality.
- **`precept_inspect(path, state, data)`** — read-only fire preview from any state+data snapshot. Use this to understand runtime behavior.
- **`precept_run(path, events)`** — step-by-step event execution. Use this to trace through a workflow.

When analyzing, designing, or debugging anything related to the Precept DSL, start with the MCP tools to get authoritative data, then read source code only for implementation details the tools don't cover.

## DSL Sample Files (.precept)

`.precept` files are DSL source files interpreted directly by the runtime — they are **not** compiled by the C# build pipeline. Never run `dotnet build` or `dotnet run` to validate a `.precept` file.

To check a `.precept` file for errors:
1. Use the `get_errors` tool on the `.precept` file path — this reads the VS Code Problems panel, which is populated by the DSL language server.
2. Cross-check the file against the sample files in `samples/`.

Do not invoke any terminal command to validate `.precept` files.

## Documentation Sync Is Mandatory

When making any code, interface, test, or behavior change, keep documentation in sync in the same edit pass.

### Source of Truth

- `README.md` is the public project narrative and usage guide.
- Design documents in `docs/` are canonical design decision records.
- Legacy documentation (`README-legacy.md`, `docs/DesignNotes-legacy.md`) is archived and must not be updated.

## README Must Track Real Implementation

On every meaningful change, review `README.md` and update impacted sections, including:

- API names/signatures and type names
- Behavioral semantics (especially inspect/fire outcomes and exceptions)
- Examples/snippets that reference changed APIs
- Feature claims that no longer match current code
- Sample files that are affected by changes

Do not leave aspirational claims as if implemented. If behavior is planned but not implemented, mark it clearly as design-phase or pending.

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
- When `ConstructCatalog` or `ConstraintCatalog` records gain or lose properties, verify `LanguageTool.cs` serialization still matches `McpServerDesign.md § precept_language` output format.
- When the fire pipeline stages change, update the static `FirePipeline` array in `LanguageTool.cs`.
- The MCP tools are **thin wrappers** — never duplicate domain logic. If a tool method exceeds ~30 lines of non-serialization code, the logic probably belongs in `src/Precept/`.

## Scope Discipline

- Keep doc updates focused and factual.
- Prefer minimal, accurate wording over broad marketing language.
- If uncertain whether a claim is implemented, verify from code/tests first.

## Design Option Responses

When providing design-option responses, include concrete usage examples to illustrate the implementation and clarify the context of the options presented.

## DSL Authoring (Non-Negotiable)

Before writing or editing any `.precept` file or any DSL snippet, read at least one representative sample file from `samples/` to confirm current syntax conventions. Do not rely on memory or inference — read first, then write.

## Deliverable Expectation

Unless explicitly told not to, include documentation synchronization as part of every relevant code change.
