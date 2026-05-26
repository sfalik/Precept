# Claude Code Instructions for Precept

## Architecture

Precept is a domain integrity engine for .NET — a DSL runtime that governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible.

| Component | Path | Purpose |
|-----------|------|---------|
| Core runtime | `src/Precept/` | Lexer → parser → type checker → graph analyzer → proof engine → runtime evaluator |
| Language server | `tools/Precept.LanguageServer/` | LSP: diagnostics, completions, hover, go-to-definition, semantic tokens, preview |
| MCP server | `tools/Precept.Mcp/` | MCP tools wrapping core APIs |
| VS Code extension | `tools/Precept.VsCode/` | Extension host: syntax highlighting, preview webview, commands |
| Copilot plugin | `tools/Precept.Plugin/` | Shipped agent + skills + MCP launcher for consumers |
| Sample files | `samples/` | `.precept` files — canonical DSL usage examples |

## Documentation Map

Precept is documentation-dense. Many design decisions live in `docs/` and `research/`, not in code comments. **Read the relevant docs before making non-trivial changes** — assume the answer exists in a doc until you've verified it doesn't.

### Read first, every time

- **`docs/philosophy.md`** — Precept's core commitments. Every design and implementation decision is evaluated against these.
- **`docs/README.md`** — the doc landscape and navigation gateway. Know what exists before deciding what to read.
- **`docs/language/README.md`** — the language surface: spec, canonical types, grammar, catalog as source of truth. Precept's design decisions are language decisions; this is the primary substance.

**First time in this codebase?** Read `docs/agent-onboarding.md` once — the five organizing concepts (catalogs as language spec, the pipeline → Compilation → Precept chain, lifecycle-driven design, pointer-philosophy + doc-sync, required reads vs context-on-demand). When a term feels ambiguous, grep `docs/glossary.md`.

### Entry points

Each area has a README that catalogs its documents, status fields, and reading order. Treat these as the canonical maps before diving into individual files:

| Area | Entry point |
|---|---|
| Philosophy | `docs/philosophy.md` — core commitments; read before any design decision |
| Top-level | `docs/README.md` — doc landscape and navigation gateway |
| Architecture overview | `docs/compiler-and-runtime-design.md` — full pipeline + runtime; read before pipeline or architecture work |
| Language surface | `docs/language/README.md` — DSL spec and type system docs |
| Compiler pipeline | `docs/compiler/README.md` — pipeline stage docs and cross-cutting infrastructure |
| Runtime | `docs/runtime/README.md` — runtime API and component docs |
| Tooling | `docs/tooling/README.md` — language server, MCP server, VS Code extension |
| Contributing | `docs/contributing/` — pre-implementation checklists and reviewer protocols |
| In-flight proposals | `docs/Working/` — design proposals awaiting acceptance |
| Archive | `docs/archive/` — superseded specs; reference only, never update |

Pipeline stage docs follow a canonical 16-section template — see existing stage docs for the pattern when adding a new one.

### Doc status conventions

Every doc declares a status. Trust it for routing; verify against code when status is "Implemented":

- **Implemented / Active** — describes shipped code
- **Canonical design** — architectural reference; grounded in the implementation
- **Full** — complete reference doc; typically a stage doc following the 16-section template
- **Incremental** — grows over time as content lands (e.g., the spec adds sections per stage)
- **Design / Draft** — specification awaiting implementation
- **Partial / Partial stub** — code exists but operations are stubs or incomplete; design is locked
- **Stub** — placeholder; design not yet fully written
- **Roadmap** — forward-looking; primarily about planning rather than current state
- **Archived** — superseded; reference only, do not update

Working-doc statuses (in `docs/Working/`): `Draft`, `Draft <kind> — YYYY-MM-DD`, `Locked YYYY-MM-DD`, `Promoted to: <link>`. Status fields may carry an inline annotation; the leading category must match the taxonomy.

If a doc says "Implemented" but the code disagrees, that's drift — fix the doc in the same pass and note the drift to the user.

### Research

`research/` houses evidence and precedent that grounds the design decisions in `docs/`. Always check before fresh investigation — much of the comparator and feasibility work is already done. Subfolders: `language/`, `architecture/`, `philosophy/`, `product/`, `security/`, `archive/`.

Brand research lives in `design/brand/lifecycle-1-research/`; UX/design-system research in `design/system/lifecycle-1-research/`.

**Use the `/lifecycle-1-research` skill for new investigations** — it enforces folder discipline, citation requirements, and the promote-or-cite rule. See `research/README.md` for the canonical map.

### Ignore: `.squad/`

`.squad/` is legacy state from a previous AI workflow (the Squad framework). It is **not maintained and not load-bearing** for current work. Do not read `.squad/` files for current project state, decisions, conventions, or team roster — that information has moved to `docs/`, `CLAUDE.md`, and the canonical specs. Read `.squad/` only if the user explicitly asks about historical Squad context.

## Catalog System (Non-Negotiable)

Precept uses a metadata-driven architecture. **Catalogs are the language specification in machine-readable form** — domain knowledge is declared as structured metadata, and pipeline stages, tooling, and consumers derive from it. They never maintain parallel copies or encode domain knowledge in their own logic.

The canonical catalog inventory lives in [`docs/language/catalog-system.md`](docs/language/catalog-system.md). Other docs reference catalogs by name, not by count — the enumeration is the source of truth.

This is the inverse of traditional compilers (Roslyn, GCC, TypeScript), where domain knowledge is scattered across pipeline stages and enums are internal classification axes. In Precept the catalog drives everything downstream: grammar, completions, hover, semantic tokens, MCP vocabulary, diagnostics.

**Rules:**

- **Catalog before code.** A new keyword, type, operator, modifier, or construct goes in the appropriate catalog entry first. Downstream artifacts derive from it.
- **Never maintain parallel keyword lists.** If a parser/LS/MCP consumer hardcodes what a catalog already knows, that's a violation. `Constructs.ByLeadingToken`, `DisambiguationEntry.DisambiguationTokens`, `Modifiers`, `Actions`, `Types`, `Operators` cover their respective surfaces — derive, don't duplicate.
- **Never hand-edit `tmLanguage.json`.** It's generated from `Tokens`, `Types`, and `Constructs` by the grammar generator.
- **Never switch on `*Kind` enum identity to dispatch per-member behavior.** The smell is `kind switch { FooKind.Bar => …, FooKind.Baz => … }` where each arm exists "because the language says so." That behavior belongs in catalog metadata. Switching on a DU **subtype** is correct (the subtype IS the metadata shape); switching on enum identity to apply per-member behavior is the violation.
- **Use discriminated unions for varying shapes.** Don't paper over shape differences with nullable fields on a flat record — use a DU base + sealed subtypes.

## Product Philosophy (Non-Negotiable)

`docs/philosophy.md` is the grounding document for Precept's identity — what the product is, what it governs, how it's positioned, and why. Read it before making design decisions, writing public-facing copy, or proposing language changes.

### Core principles

The durable commitments — `docs/philosophy.md` is canonical:

- **Prevention, not detection.** Invalid configurations are structurally impossible — not caught after the fact, not bypassable.
- **One file, complete rules.** Every field, rule, ensure, and transition lives in the `.precept` definition. No scattered logic across validators, handlers, or service code.
- **Data and rules are primary; states are the mechanism.** Precept governs an entity's data integrity. Lifecycle is the coordinate system, not the point. **States are optional** — stateless precepts are first-class.
- **Governance, not validation.** Rules are declarations bound to the entity, structurally enforced on every operation. Not validators called at boundaries that can be bypassed.
- **Determinism.** Same definition + same data = same outcome. Nothing hidden.
- **Honesty about approximation.** Exact and approximate behavior must be visible in the type system and public surface. Precept does not present approximation as exactness.
- **Compile-time structural checking.** Unreachable states, dead-ends, type mismatches, contradictions, unsatisfiable guards, division-by-zero, overflow — all compile-time impossibilities.
- **Primary author is the domain expert**, not the developer. The language optimizes for someone who reasons in terms of "what is this data allowed to become."

### Rules

**Do not edit `docs/philosophy.md` without explicit owner approval.** Philosophy changes require deliberation — they are never auto-synced, never incidental updates, and never bundled into implementation PRs without discussion.

When any of the following change, **flag the potential philosophy gap to the user** — do not resolve it yourself:

- The category of entities Precept can govern (e.g. stateless precepts shipping)
- The core guarantee (prevention, determinism, inspectability)
- The positioning relative to adjacent tools
- The constraint model or operation surface

If the runtime can do something the philosophy doesn't describe, or the philosophy claims something the runtime can't do, **surface the gap and wait for direction.** The philosophy governs the product — the product does not silently rewrite the philosophy.

## Documentation Sync (Non-Negotiable)

**See also**: `CONTRIBUTING.md` § Doc Lifecycle — the 7-stage lifecycle and the lifecycle skills (`/lifecycle-1-research` through `/lifecycle-7-audit`) that automate doc-sync at each transition. The routing table below is consumed by `/lifecycle-2-design` (populates doc-update enumeration in design docs) and `/lifecycle-5-promote` (verifies obligations at promotion).

When making any code, interface, test, or behavior change, keep documentation in sync in the same edit pass. Unless explicitly told not to, include documentation synchronization as part of every relevant code change. Keep updates focused and factual; if uncertain whether a claim is implemented, verify from code/tests first.

### Where to update for which change

| Kind of change | Update |
|---|---|
| Pipeline stage behavior | `docs/compiler/<stage>.md` § Implementation State / § Open Questions |
| Runtime API | `docs/runtime/runtime-api.md` + affected `descriptor-types.md` / `result-types.md` / `fault-system.md` / `precept-builder.md` / `evaluator.md` |
| Language surface (new keyword/type/operator/modifier/construct) | Add catalog entry first; then update `docs/language/precept-language-spec.md` + relevant type doc (`primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md`) |
| Diagnostic added/changed | `docs/compiler/diagnostic-system.md` |
| MCP tool surface | `docs/tooling/mcp.md` + relevant DTO/formatter in `tools/Precept.Mcp/` |
| Language server feature | `docs/tooling/language-server.md` |
| Doc status changing (Stub → Design → Implemented) | Update the doc's own Status field AND any cross-referencing tables (e.g., `docs/compiler/README.md`) |
| README claim invalidated | `README.md` — never let aspirational claims sit as if implemented |

### Source of Truth

- `README.md` — public project narrative and usage guide; must track real implementation. Never leave aspirational claims as if implemented.
- `docs/` — canonical technical design decision records, architecture notes, project philosophy. Per-area READMEs are the canonical maps.
- `research/` — evidence and precedent; cite, don't duplicate. See `/lifecycle-1-research` skill.
- `design/brand/` — brand identity and brand-level semantic meaning.
- `design/system/` — reusable product-facing visual-system guidance and surface specs.
- `design/prototypes/` — durable design prototypes. Hot, code-near prototypes may live near their owning tool surface but should be promoted here when durable.
- `docs/archive/` holds superseded specs; reference only, never update.

### Transient vs Canonical references (Non-Negotiable)

**Code comments must reference only canonical, stable material.** Project-state references (the current task, workstream labels, phase numbers, finding IDs, design-doc Decision numbers, working-doc paths, audit dates) rot as the codebase evolves and belong in commit messages / PR descriptions, not in code that survives the project state.

**Canonical (OK to reference from code)**:
- `docs/language/*.md`, `docs/compiler/*.md`, `docs/runtime/*.md`, `docs/tooling/*.md` — canonical specs and per-stage docs
- `docs/philosophy.md` — locked philosophy
- `CLAUDE.md` — load-bearing project rules
- Catalog and source files (e.g. `Modifiers.cs`, `Tokens.cs`, `Operations.cs`)
- Spec line numbers when stable (e.g. `precept-language-spec.md:1662`)

**Transient (do NOT reference from code)**:
- `docs/Working/` — in-flight design proposals; promote-or-archive lifecycle means content moves
- `bugs.md` — tracking surface; entries move from Active to Fixed and eventually archive
- Design-doc internal structure (`Decision 2`, `D-3`, `Option A/B/C`)
- Workstream labels (`W-A`, `W-G`), phase labels (`Phase 4`), slice labels (`Slice 8`, `Slice 12`)
- Finding IDs (`F-LANG-COLL-06`, `F-LANG-BIZ-10`)
- Audit/review references (`precept-reviewer audit (2026-MM-DD)`), date stamps

`BUG-NNN` cites are a deliberate exception: the bugs.md convention pairs each inline cite with a planned removal (the workaround comes out when the bug closes). Add a new `BUG-NNN` cite only when there's a matching workaround in the code or sample.

**Rewriting rule**: if a comment had load-bearing WHY content mixed with transient refs, preserve the WHY in terms of the language/architecture; drop the project-task scaffolding.

- Bad: `// Slice 8 wires PRE0048 emission for action-applicability mismatches per F-LANG-COLL-08`
- Good: `// PRE0048 emission for action-applicability mismatches`
- Bad: `// TODO(W-G): widen return per COLL-02/03 design D-3 Option A (docs/Working/...).`
- Good: `// TODO: return TypedElementType? so accessor return-type propagation can flow qualifier metadata.`

This rule supplements the generic "don't reference the current task" comment-discipline rule with the project-specific list of which surfaces are stable vs transient.

### When research is involved

When locking a decision that started as research:
- Reference the research file from the consuming proposal/decision/spec.
- Update the issue map in `research/language/README.md` (or the relevant subfolder README) so the research connects forward.
- **Do not let research stand alone as policy.** Promote conclusions to a spec or decision; archive what didn't ship. (See `/lifecycle-1-research` skill — Promote-or-Cite rule.)

## DSL Authoring (Non-Negotiable)

Delegate `.precept` file work to the **`precept-author` sub-agent** (`.claude/agents/precept-author.md`). It carries the precept MCP tools and the canonical authoring/debugging workflows — spawn it rather than authoring DSL inline.

For inline snippets (a single line in an explanation, a short example in a comment), still: read at least one representative sample file from `samples/` first. Do not rely on memory or inference — read first, then write.

## Language Surface Design (Non-Negotiable)

New language surface — syntax, keywords, types, operators, modifiers, constructs, expression forms — must go through `/lifecycle-2-design`. Never propose or settle on a specific syntax approach in direct chat.

If a user asks about syntax options, discuss tradeoffs briefly but **do not propose a specific design inline**. Route to the skill: "Let's run `/lifecycle-2-design` to work through this properly." A suggestion made in chat is brainstorming; it must not harden into a decision without the four-leg rationale, Language Design Grounding (broader field, not just Precept-internal), and Architecture Grounding the skill enforces.

The risk: a casual inline suggestion — made without reading the spec, comparable systems, or the catalog — can become "the" design simply by being the first thing written down. The `/lifecycle-2-design` skill exists precisely to prevent this.

## Per-Decision Rationale (Non-Negotiable)

Locked design decisions — in proposals, design docs, or research conclusions — must include all four:

- **Rationale** — why this choice, not just what it is
- **Alternatives considered and rejected** — with reasons for rejection
- **Precedent** — the evidence that grounds the decision
- **Tradeoff accepted** — the known downside being taken on

A decision that states WHAT without WHY is incomplete. Flag it before it advances.

For research that grounds decisions, use the `/lifecycle-1-research` skill — it codifies the methodology, folder discipline, and promote-or-cite rule. See `CONTRIBUTING.md` for the proposal lifecycle (research → issue → decision → spec).

## Build & Test

```bash
# Build everything
dotnet build

# Build language server only (default build task — Ctrl+Shift+B)
dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server

# Run all tests (xUnit + FluentAssertions, ~3600 tests across 4 projects)
dotnet test

# Run a single test project
dotnet test test/Precept.Tests/
dotnet test test/Precept.LanguageServer.Tests/
dotnet test test/Precept.Mcp.Tests/
dotnet test test/Precept.Analyzers.Tests/

# VS Code extension (from tools/Precept.VsCode/)
npm run compile        # Build TypeScript
npm run watch          # Watch mode
npm run loop:local     # Package + install locally (also a VS Code task)
```

**VS Code tasks** (Run Task menu): `build`, `extension: install`, `extension: uninstall`, `plugin: sync payload`.

## Development Workflow

- **Runtime / language server changes** → edit `src/Precept/` or `tools/Precept.LanguageServer/` → run Build task → extension auto-detects new build, no reload needed.
- **Extension UI / grammar / TypeScript** → edit `tools/Precept.VsCode/` → run `extension: install` task → reload window.
- **MCP server** → edit `tools/Precept.Mcp/` → reload window → rebuild happens lazily on next tool invocation from source.
- **Claude Code agents / skills** → edit `.claude/agents/` and `.claude/skills/` → changes appear on next session or sub-agent spawn.
- **Shipped Copilot plugin (agents/skills markdown)** → edit workspace-native copies in `.github/agents/` and `.github/skills/` → reload window → changes appear immediately. Run `plugin: sync payload` only when updating the shipped plugin payload under `tools/Precept.Plugin/` for explicit validation. *(The shipped plugin targets Copilot consumers of Precept — it's a product artifact, not your dev tooling.)*

Four distinct MCP config surfaces exist and must stay distinct:

- **`.vscode/mcp.json`** — VS Code/workspace-local source-first config. Uses the VS Code `servers` schema. Points `servers.precept` at `tools/scripts/start-precept-mcp.js`.
- **`.mcp.json` (repo root)** — repo-local config shared by Claude Code and Copilot CLI. Uses the `mcpServers` schema. Points `mcpServers.precept` at the same `tools/scripts/start-precept-mcp.js`. Claude Code enables it via `.claude/settings.local.json` (`enabledMcpjsonServers: ["precept"]`); Copilot CLI uses it natively.
- **`.claude/settings.local.json`** — Claude Code's per-user, per-project settings (enabled MCP servers, permissions). Not the MCP server definition itself — that lives in `.mcp.json`.
- **`tools/Precept.Plugin/.mcp.json`** — shipped/distribution payload. Uses `mcpServers` with `dotnet tool run precept-mcp`. Do not use this surface for local development.

## Use the MCP Tools First

This project ships a Precept MCP server. **Use its tools as your primary research surface** before reading source code or making assumptions about the DSL. Call `precept_ping` to confirm connectivity, then discover what's available from your session's tool list. Fall back to source code only for implementation details the tools don't cover.

## MCP Tool Sync

The MCP server tools in `tools/Precept.Mcp/Tools/` are **thin wrappers** around core APIs. If a tool method exceeds ~30 lines of non-serialization code, the logic belongs in `src/Precept/`.

- Core model / compile-result changes → check `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` and any affected tool projections
- Catalog / diagnostic / domain metadata changes → verify `tools/Precept.Mcp/CatalogFormatters.cs` and `docs/tooling/mcp.md` still match

## Issue Implementation Workflow

Read `CONTRIBUTING.md` for the full workflow. Load-bearing rules:

- Open the draft PR immediately; it's the execution hub. Body structure: `## Summary`, `## Linked Issue` (`Closes #N`), `## Why`, `## Implementation Plan`.
- **Design review gate.** `## Implementation Plan` stays "Pending design review" until the review ceremony completes with owner sign-off. Track B (introducing a new canonical design doc) also requires all inline review comments on the design doc resolved. See `CONTRIBUTING.md` § 3 for full Track A / Track B details.
- Work in vertical slices — commit, push, and update the PR-body summary/checklist after each.
- The PR body **is** the implementation plan. Never create a separate implementation-plan markdown file.

## DSL Sample Files (.precept)

`.precept` files are interpreted by the runtime — **not** compiled by the C# build pipeline. Never run `dotnet build` or `dotnet run` to validate a `.precept` file.

To check a `.precept` file for errors:
1. Use the `precept_compile` MCP tool on the `.precept` file contents — it returns diagnostics from the full compiler pipeline.
2. Cross-check against sample files in `samples/`.

## Test Conventions

- Framework: **xUnit** with **FluentAssertions**
- Naming: `PascalCase` + `Tests` suffix (e.g. `PreceptParserTests.cs`, `PreceptRuntimeTests.cs`)
- Tests typically use `[Fact]` and `[Theory]` attributes
