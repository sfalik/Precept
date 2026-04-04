# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. AI-native — "the contract AI can reason about."
- **Stack:** C# / .NET 10.0 (MCP server), Markdown/JSON (plugin/skills)
- **My domain:** `tools/Precept.Mcp/` (MCP server, 5 tools) and `tools/Precept.Plugin/` (Copilot agent plugin)
- **MCP tools:** `precept_language`, `precept_compile`, `precept_inspect`, `precept_fire`, `precept_update`
- **Key docs:** `docs/McpServerDesign.md` (MCP tool specs), `docs/McpServerImplementationPlan.md`
- **Thin wrapper rule:** MCP tools wrap core APIs — no domain logic in the MCP layer
- **Tests:** `test/Precept.Mcp.Tests/`
- **Distribution:** Claude Marketplace (plugin)
- **Created:** 2026-04-04

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
