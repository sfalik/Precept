# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible. Declarative `.precept` files compile to executable runtime contracts.
- **Stack:** C# / .NET 10.0 (core runtime, language server, MCP server), TypeScript (VS Code extension), xUnit + FluentAssertions
- **Components:** `src/Precept/` (core DSL pipeline), `tools/Precept.LanguageServer/`, `tools/Precept.Mcp/`, `tools/Precept.VsCode/`, `tools/Precept.Plugin/`
- **Key docs:** `docs/PreceptLanguageDesign.md` (DSL spec), `docs/RuntimeApiDesign.md` (public API), `docs/RulesDesign.md` (constraints), `docs/McpServerDesign.md` (MCP tools)
- **Distribution:** NuGet, VS Code Marketplace, Claude Marketplace
- **Created:** 2026-04-04

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
