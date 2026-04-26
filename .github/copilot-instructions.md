# Copilot Instructions for Precept

## Architecture

Precept is a domain integrity engine for .NET — a DSL runtime that governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible.

| Component | Path | Purpose |
|-----------|------|---------|
| Core runtime | `src/Precept/` | Lexer → parser → type checker → graph analyzer → proof engine → runtime evaluator |
| Language server | `tools/Precept.LanguageServer/` | LSP: diagnostics, completions, hover, go-to-definition, semantic tokens, preview |
| MCP server | `tools/Precept.Mcp/` | 5 MCP tools wrapping core APIs (see below) |
| VS Code extension | `tools/Precept.VsCode/` | Extension host: syntax highlighting, preview webview, commands |
| Copilot plugin | `tools/Precept.Plugin/` | Agent definition + 2 skills + MCP launcher |
| Sample files | `samples/` | 20 `.precept` files — canonical DSL usage examples |

Key design docs: `docs/philosophy.md` (product philosophy), `docs/language/precept-language-spec.md` (DSL semantics), `docs/runtime/runtime-api.md` (C# API), `docs/language/catalog-system.md` (metadata registries). See `docs/` for the full set.

## Metadata-Driven Architecture (Non-Negotiable)

Precept uses a **metadata-driven architecture.** Domain knowledge is declared as structured metadata in catalogs. Pipeline stages are generic machinery that reads it. This is not the traditional compiler model — it is the inverse.

In traditional compilers (Roslyn, GCC, TypeScript), domain knowledge is scattered across pipeline stage implementations and enums are internal classification axes. In Precept, **catalogs are the language specification in machine-readable form.** Pipeline stages, tooling, and consumers derive from catalog metadata — they never maintain parallel copies or encode domain knowledge in their own logic.

When making design decisions, reason from language surface outward:

- **"Is this part of a complete description of Precept?"** If yes, it gets cataloged — regardless of how small the enum is, how internal it looks, or whether existing compilers would treat it as a bare implementation detail.
- **"Do consumers hardcode per-member knowledge that should be metadata?"** If the type checker, graph analyzer, evaluator, or runtime switches on enum values to apply per-member behavior, that behavior is domain knowledge and belongs in metadata.
- **"Do members need different metadata shapes?"** Use a discriminated union (abstract record base + sealed subtypes). Each subtype carries exactly the fields its consumers need. Do not use flat records with inapplicable nullable fields — use a DU instead.

The canonical design doc for the catalog system is `docs/language/catalog-system.md`. Read its § Architectural Identity before making decisions about what gets cataloged, what stays bare, or how metadata is shaped.

## Build & Test

```bash
# Build everything
dotnet build

# Build language server only (default build task — Ctrl+Shift+B)
dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server

# Run all tests (xUnit + FluentAssertions, ~2000 tests across 3 projects)
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

**VS Code tasks** (Run Task menu): `build`, `extension: install`, `extension: uninstall`, `plugin: sync payload`.

## Development Workflow

- **Runtime / language server changes** → edit `src/Precept/` or `tools/Precept.LanguageServer/` → run Build task → extension auto-detects new build, no reload needed.
- **Extension UI / grammar / TypeScript** → edit `tools/Precept.VsCode/` → run `extension: install` task → reload window.
- **MCP server** → edit `tools/Precept.Mcp/` → keep the workspace-owned `.vscode/mcp.json` `servers.precept` entry pointed at `tools/scripts/start-precept-mcp.js` → reload window → rebuild happens lazily on next tool invocation from source.
- **Plugin (agents/skills markdown)** → edit workspace-native copies in `.github/agents/` and `.github/skills/` → reload window → changes appear immediately. Run `plugin: sync payload` only when updating the shipped plugin payload under `tools/Precept.Plugin/` for explicit validation.

Keep `tools/Precept.Plugin/.mcp.json` in shipped `dotnet tool run precept-mcp` form. That plugin file uses its own `mcpServers` payload schema. Use `.vscode/mcp.json` for repo-local MCP development with the VS Code `servers` schema, `.github/agents/` and `.github/skills/` as the workspace-native customization source, and treat plugin/distribution-shaped validation as explicit validation, not the default inner loop.

## Issue Implementation Workflow

For issue-based implementation work:

- Read `CONTRIBUTING.md` before starting and treat it as the canonical workflow for issue work.
- Open or reuse the linked **draft PR** immediately and treat it as the execution hub for the issue.
- Use the exact PR-body structure required by `CONTRIBUTING.md` and the repository PR template: `## Summary`, `## Linked Issue` (with `Closes #N`), `## Why`, and `## Implementation Plan`.
- Keep the `## Summary` and `## Why` sections current so reviewers can see what changed and why without reconstructing it from the diff.
- **Design review gate:** No implementation plan is authored until the design review ceremony completes with owner sign-off. The `## Implementation Plan` section says \"Pending design review\" until the gate clears. For Track B proposals (those introducing a new canonical design doc), all inline PR review comments on the design doc must also be resolved. See `CONTRIBUTING.md` § 3. Design Review for full Track A / Track B details.
- **Build a detailed implementation plan after design review completes.** The plan lives in the PR body's `## Implementation Plan` section and must meet the quality bar defined in `CONTRIBUTING.md` § Implementation Plan Quality Bar: vertical slices with method-level specificity, exact file paths, tests per slice, regression anchors, dependency ordering, file inventory, and tooling/MCP sync assessment.
- Work in vertical slices. After each completed slice, commit, push, and update the PR-body summary/checklist before continuing.
- Do **not** create a separate implementation-plan markdown file; the PR body is the ephemeral plan artifact for this repo.

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

## Product Philosophy (Non-Negotiable)

`docs/philosophy.md` is the grounding document for Precept's identity — what the product is, what it governs, how it's positioned, and why. Read it before making design decisions, writing public-facing copy, or proposing language changes.

**Do not edit `docs/philosophy.md` without explicit owner approval.** Philosophy changes require deliberation — they are never auto-synced, never incidental updates, and never bundled into implementation PRs without discussion.

When any of the following change, **flag the potential philosophy gap to the user** — do not resolve it yourself:

- The category of entities Precept can govern (e.g. stateless precepts shipping)
- The core guarantee (prevention, determinism, inspectability)
- The positioning relative to adjacent tools
- The constraint model or operation surface

If the runtime can do something the philosophy doesn't describe, or the philosophy claims something the runtime can't do, **surface the gap and wait for direction.** The philosophy governs the product — the product does not silently rewrite the philosophy.

## Documentation Sync (Non-Negotiable)

When making any code, interface, test, or behavior change, keep documentation in sync in the same edit pass. Unless explicitly told not to, include documentation synchronization as part of every relevant code change.

### Source of Truth

- `README.md` — public project narrative and usage guide. Must track real implementation: update API names, behavioral semantics, examples, and feature claims on every meaningful change. Never leave aspirational claims as if implemented.
- `docs/` — canonical technical design decision records, architecture notes, implementation plans, research, and project philosophy.
- `design/brand/` — canonical source for brand identity, brand spec, and brand-level semantic meaning.
- `design/system/` — canonical source for reusable product-facing visual-system guidance and surface specs.
- Legacy files (`README-legacy.md`, `docs/DesignNotes-legacy.md`) — archived, do not update.

Keep updates focused and factual. If uncertain whether a claim is implemented, verify from code/tests first.

### Design Asset Boundaries

- Use `design/brand/` for identity and brand meaning.
- Use `design/system/` for reusable visual-system rules and surface specs.
- Use `docs/` for project philosophy, explanatory documentation, and technical documentation.
- Durable design prototypes belong in `design/prototypes/`.
- Hot, code-near prototypes may temporarily live near their owning tool surface, but should be promoted into `design/prototypes/` once they become durable design references.

## Language Surface Propagation (Non-Negotiable)

In v2, the TextMate grammar (`tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`), completions, semantic tokens, and MCP vocabulary are **generated from catalog metadata** — they are not hand-edited. The catalog is the single source of truth.

When making language surface changes:

- **Add to the catalog.** A new keyword, type, operator, or construct belongs in the appropriate catalog entry in `src/Precept/`. All downstream artifacts — grammar, completions, hover, MCP output — derive from it.
- **Do not hand-edit `tmLanguage.json`.** It is a build output. The grammar generator reads the `Tokens`, `Types`, and `Constructs` catalogs and emits the file.
- **Do not maintain parallel keyword lists** in tooling code. If a consumer switches on catalog members or hardcodes per-member behavior, that behavior belongs in catalog metadata instead.

See `docs/language/catalog-system.md` — specifically § Architectural Identity and the consumer table — for which catalogs drive which artifacts.

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

## Proposal Philosophy Capture (Non-Negotiable)

See [CONTRIBUTING.md](/CONTRIBUTING.md) for the full proposal lifecycle and where each artifact lives.

Language proposals (GitHub issues) must include the design philosophy and rationale — not just the syntax and acceptance criteria. When a proposal is revised or a new feature is decided through design discussion:

1. **Capture the reasoning in `research/`** — research evidence, precedent surveys, dead ends explored, and why alternatives were rejected. This is the durable record that explains *why*.
2. **Reference research from the proposal issue** — the issue body should link to the research file(s) that ground its decisions.
3. **Update the issue map** in `research/language/README.md` — connect each proposal to its research starting points.
4. **Design doc updates happen at implementation time** — `docs/PreceptLanguageDesign.md` tracks what EXISTS in the runtime. Proposals describe what's PLANNED. The design doc is updated in the same PR that implements the feature, not before.

### Per-Decision Rationale Requirement

When writing or reviewing proposals, ensure every locked design decision includes:

- **Rationale** — why this choice, not just what it is
- **Alternatives considered and rejected** — with reasons for rejection
- **Precedent from the research base** — the evidence that grounds the decision
- **Tradeoff accepted** — the known downside the team is deliberately taking on

A proposal that states WHAT without WHY is incomplete. Flag it for rationale before it advances.

This ensures philosophy and rationale survive across sessions and make their way back into the design doc when features ship.

## Design Option Responses

When providing design-option responses, include concrete usage examples to illustrate the implementation and clarify the context of the options presented.
