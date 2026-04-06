# Squad Team

> Precept

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Frank | Lead/Architect & Language Designer | `.squad/agents/frank/charter.md` | 🟢 Active |
| George | Runtime Dev | `.squad/agents/george/charter.md` | 🟢 Active |
| Kramer | Tooling Dev | `.squad/agents/kramer/charter.md` | 🟢 Active |
| Newman | MCP/AI Dev | `.squad/agents/newman/charter.md` | 🟢 Active |
| Soup Nazi | Tester | `.squad/agents/soup-nazi/charter.md` | 🟢 Active |
| Uncle Leo | Code Reviewer | `.squad/agents/uncle-leo/charter.md` | 🟢 Active |
| J. Peterman | Brand/DevRel | `.squad/agents/j-peterman/charter.md` | 🟢 Active |
| Steinbrenner | PM | `.squad/agents/steinbrenner/charter.md` | 🟢 Active |
| Elaine | UX Designer | `.squad/agents/elaine/charter.md` | 🟢 Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 🟢 Active |
| Ralph | Work Monitor | — | 🔄 Monitor |

## Human Members

| Name | Role | Badge | Status | Notes |
|------|------|-------|--------|-------|
| Shane | Reviewer | 👤 Human | 🟢 Active | Final human review/sign-off when the team explicitly routes work for review. |

## Project Context

- **Project:** Precept — a domain integrity engine for .NET. Binds entity state, data, and business rules into a single executable contract via a declarative DSL. Makes invalid states structurally impossible.
- **Stack:** C# / .NET 10.0 (core runtime, language server, MCP server), TypeScript (VS Code extension), xUnit + FluentAssertions (tests), LSP (language server protocol)
- **Components:** Core DSL runtime (`src/Precept/`), Language Server (`tools/Precept.LanguageServer/`), MCP Server (`tools/Precept.Mcp/`), VS Code Extension (`tools/Precept.VsCode/`), Copilot Plugin (`tools/Precept.Plugin/`)
- **Distribution:** NuGet (core library), VS Code Marketplace (extension), Claude Marketplace (plugin)
- **Owner:** shane
- **Universe:** Seinfeld
- **Created:** 2026-04-04

## Issue Source

- **Provider:** GitHub
- **Repository:** `sfalik/Precept`
- **Connected:** 2026-04-05T19:00:10Z
- **Filters:** Open issues and open pull requests
