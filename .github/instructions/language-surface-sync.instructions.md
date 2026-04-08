---
description: "Use when changing the Precept DSL surface, parser, runtime behavior, syntax grammar, language server completions, MCP DTOs/tools, or sample .precept files. Covers same-PR sync for docs, grammar, completions, MCP docs, tests, and samples."
name: "Language Surface Sync"
applyTo: "src/Precept/Dsl/**,tools/Precept.LanguageServer/**,tools/Precept.VsCode/syntaxes/**,tools/Precept.Mcp/Tools/**,samples/**/*.precept"
---
# Language Surface Sync

Follow `CONTRIBUTING.md` and keep language-surface changes synchronized in the same PR.

- If you change DSL syntax, parser behavior, type rules, runtime behavior, MCP outputs, or sample `.precept` files, check whether the following must also change:
  - `docs/PreceptLanguageDesign.md`
  - `docs/EditableFieldsDesign.md`
  - `docs/RuntimeApiDesign.md`
  - `docs/McpServerDesign.md`
  - `README.md`
  - `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`
  - `tools/Precept.LanguageServer/PreceptAnalyzer.cs`
  - MCP DTO/tool files under `tools/Precept.Mcp/Tools/`
  - relevant tests and samples

- If the language surface changes, verify grammar and completions in the same change.
- If runtime model or API behavior changes, verify MCP DTOs/docs in the same change.
- If you touch `.precept` samples, read at least one existing sample first and validate the changed files.
- If no sync updates are needed, say so explicitly in the PR summary or final report.

Do not leave aspirational docs. Specs in `docs/` must describe what exists after the implementation lands.