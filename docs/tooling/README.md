# tooling/ — Language Server, MCP Server, VS Code Extension

The three tooling surfaces that project Precept's compiler and runtime to editors, AI agents, and developers. Each is a separate component with its own design doc here; each consumes the compiler's `Compilation` and the runtime's `Precept` artifacts but otherwise operates independently.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [language-server.md](language-server.md) | LSP server (`tools/Precept.LanguageServer/`) — diagnostics, semantic tokens, completions, hover, definition/references/rename, signature help, symbols, selection ranges, code actions, preview panel | Full |
| [mcp.md](mcp.md) | MCP server (`tools/Precept.Mcp/`) — live tool surface for AI agents (compile, plus catalog/reference tools); planned runtime tools ship with v1 | Canonical design |
| [extension.md](extension.md) | VS Code extension (`tools/Precept.VsCode/`) — extension host, status bar, commands, dev file watcher, preview panel scaffold | Stub |

## Reading Order

For an agent new to the tooling area:

1. [`../compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) §§13–15 — the architectural decisions that ground every tooling surface (TextMate grammar generation, MCP integration, LS integration). Read this first; the per-component docs below assume this context.
2. [mcp.md](mcp.md) — the live AI-agent surface; this is the most mature tooling doc and the cleanest example of "honest about what's live vs. planned."
3. [language-server.md](language-server.md) — the LSP server design. Section routing in 7.x covers each LSP feature.
4. [extension.md](extension.md) — the VS Code host. Stubbier than the other two; relevant when working on extension UI or commands.

## Relationship to Other Docs

- [`../compiler/README.md`](../compiler/README.md) — Tooling consumes `Compilation` artifacts produced by the compiler pipeline. Diagnostic IDs flow from [`../compiler/diagnostic-system.md`](../compiler/diagnostic-system.md) into MCP `precept_compile` output and LSP diagnostics.
- [`../runtime/README.md`](../runtime/README.md) — Tooling consumes `Precept` / `Version` runtime artifacts. The LSP preview panel and the planned MCP runtime tools (`precept_fire`, `precept_create`, `precept_update`, `precept_inspect`) depend on the runtime shipping with v1.
- [`../language/catalog-system.md`](../language/catalog-system.md) — All tooling derives vocabulary from catalogs. The TextMate grammar generator (`tools/Precept.GrammarGen/`) reads catalogs to produce `precept.tmLanguage.json`; LSP completions and hover query the same catalogs; the MCP catalog-reference tools (`precept_syntax`, `precept_types`, `precept_operations`, `precept_domains`, `precept_proofs`, `precept_patterns`, `precept_diagnostic`) are catalog projections.
- [`../compiler/tooling-surface.md`](../compiler/tooling-surface.md) — The TextMate grammar generation and semantic-token two-pass design. Lives in `compiler/` because it's a compile-time artifact, but read alongside the LS doc.
- [`../compiler/grammar-generator.md`](../compiler/grammar-generator.md) — Algorithm and pattern templates for `tools/Precept.GrammarGen/`.

## Cross-cutting concerns

- **Catalog discipline.** Tooling **never** maintains parallel keyword lists. If LS code, MCP code, or the grammar generator hardcodes what a catalog already knows, that's a violation. See [`../language/catalog-system.md § Architectural Identity`](../language/catalog-system.md) and [`../contributing/catalog-driven-checklist.md`](../contributing/catalog-driven-checklist.md).
- **Live vs. planned.** Each tooling doc is honest about which features ship today vs. design-only. mcp.md models this well (`§ Live tool surface` vs `§ Planned or absent tools`); language-server.md does the same in its Implementation Status section. When updating a tooling doc after shipping new features, move items from Planned → Live in the same commit.
- **MCP / LS / grammar update triangle.** Language-surface changes propagate to three places: MCP tool output (`tools/Precept.Mcp/CatalogFormatters.cs` and any affected tool projections), LS completions/hover/semantic-tokens, and the TextMate grammar (regenerated via `tools/Precept.GrammarGen/`). Confirm all three after any catalog change; the [routing table in CLAUDE.md](../../CLAUDE.md) names this triangle as a non-negotiable sync obligation.
- **Authoring audience for tooling output.** Diagnostic messages, hover text, completion descriptions, and MCP tool output target the **domain expert**, not the developer. See [`../philosophy.md § Who authors a precept`](../philosophy.md) — this constrains LS and MCP design decisions about message wording and surface complexity.
