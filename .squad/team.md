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

- **Project:** Precept — a domain integrity engine for .NET. A single declarative contract for modelling business entities, governing how their data evolves under business rules across a lifecycle. Makes invalid configurations structurally impossible.
- **Stack:** C# / .NET 10.0 (core runtime, language server, MCP server), TypeScript (VS Code extension), xUnit + FluentAssertions (tests), LSP (language server protocol)
- **Components:** Core DSL runtime (`src/Precept/`), Language Server (`tools/Precept.LanguageServer/`), MCP Server (`tools/Precept.Mcp/`), VS Code Extension (`tools/Precept.VsCode/`), Copilot Plugin (`tools/Precept.Plugin/`)
- **Design Structure:** `design/brand/` is the source of truth for brand identity and canonical meaning. `design/system/` is the source of truth for reusable product-facing visual-system guidance and surface specs. `design/prototypes/` holds durable design prototypes. `docs/` remains the source of truth for technical and explanatory documentation.
- **Philosophy:** `docs/philosophy.md` — grounding document for product identity, positioning, and conceptual hierarchy. All agents must read before design decisions, positioning work, or language proposals. Keep in sync with the implementation.
- **Distribution:** NuGet (core library), VS Code Marketplace (extension), Claude Marketplace (plugin)
- **Owner:** shane
- **Universe:** Seinfeld
- **Created:** 2026-04-04

## Ownership Boundaries

- **Peterman owns `design/brand/`** — brand identity, philosophy, voice, mark logic, typography intent, and canonical brand meaning.
- **Elaine owns `design/system/`** — semantic visual system work, reusable product-surface guidance, and canonical surface specs.
- **Elaine creates and maintains the two canonical design HTML artifacts** — `design/brand/brand-spec.html` and `design/system/foundations/semantic-visual-system.html` are Elaine-owned design executions and should be treated as a visually coordinated pair.
- **Peterman remains brand owner for the brand artifact** — he supplies brand meaning, research, semantic guidance, and review input to Elaine for `design/brand/brand-spec.html` and for any design-system choices that affect brand meaning.
- **Frank governs boundary decisions** — promotions between local surface rules, reusable system rules, and brand-level meaning.
- **`docs/` is not a design bucket** — it holds technical design docs, implementation plans, research, and explanatory material rather than canonical visual-system rules.
- **Shared review is required** when a change affects both brand meaning and reusable product-surface guidance.

## Issue Source

- **Provider:** GitHub
- **Repository:** `sfalik/Precept`
- **Connected:** 2026-04-05T19:00:10Z
- **Filters:** Open issues and open pull requests
