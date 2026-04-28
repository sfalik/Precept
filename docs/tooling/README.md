# Precept Tooling

This folder documents the three tooling surfaces that project the Precept compiler and runtime to editors, AI agents, and developers.

## Contents

| Doc | Component | Source |
|---|---|---|
| [language-server.md](language-server.md) | Language Server (LSP) | `tools/Precept.LanguageServer/` |
| [mcp.md](mcp.md) | MCP Server | `tools/Precept.Mcp/` |
| [extension.md](extension.md) | VS Code Extension | `tools/Precept.VsCode/` |

## Reading order

Read `docs/compiler-and-runtime-design.md` §§13–15 first for the architectural decisions. The docs here describe the component-level design details.

## Relationship to other docs

Tooling surfaces consume `Compilation` and `Precept` artifacts produced by the compiler and runtime. See `docs/compiler/` and `docs/runtime/` for those contracts.
