# Contributing to Precept

## Development Workflow

### Proposal Lifecycle

Every language or runtime change follows this flow:

```
Idea → GitHub Issue (proposal) → Research → Implementation PR → Merge → Docs updated
```

#### 1. Proposal (GitHub Issue)

The **GitHub issue is the canonical proposal**. It contains:
- Summary and motivation
- Proposed syntax or API changes
- Design decisions with rationale
- Acceptance criteria
- Implementation scope (t-shirt sizes per layer)

Create the issue with labels `proposal` + `language` (and optionally `dsl-expressiveness` or `dsl-compactness`). Add it to the **Precept Language Improvements** project. Assign a wave milestone.

#### 2. Research (`docs/research/`)

Research documents live in `docs/research/` and capture:
- Precedent surveys (how other tools solve this)
- Dead ends explored and why they were rejected
- Design philosophy and rationale

Research files are linked from the issue, not the other way around. The issue map in `docs/research/language/README.md` connects each proposal to its research starting points.

**Research is durable.** It explains *why* decisions were made and survives across sessions. When a proposal is revised, update the research doc with the new reasoning.

#### 3. Implementation (Feature Branch + PR)

When ready to implement:

1. Create a feature branch: `feature/issue-N-short-description`
2. Open a **draft PR** immediately, linked to the issue (`Closes #N`)
3. The **PR body is the implementation plan** — a checklist of work items. This is ephemeral; it doesn't need to outlive the PR.
4. Implement in vertical slices. Suggested order for cross-cutting changes:
   - Parser + model + diagnostics
   - Type checker
   - Runtime engine
   - Language server (completions, semantic tokens)
   - TextMate grammar (syntax highlighting)
   - MCP tools
   - Tests (throughout, not at the end)
   - Sample files
   - Documentation updates
5. Mark the PR as ready for review when all acceptance criteria are met.

#### 4. Documentation Sync (Same PR — Non-Negotiable)

Every implementation PR must update documentation in the same pass:

| What changed | Update |
|-------------|--------|
| New keyword, operator, or syntax | `docs/PreceptLanguageDesign.md` |
| New or changed API behavior | `docs/RuntimeApiDesign.md` |
| New editability semantics | `docs/EditableFieldsDesign.md` |
| New MCP tool behavior | `docs/McpServerDesign.md` |
| New keyword or syntax form | `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` |
| Feature claims in README | `README.md` |

**Design docs track what EXISTS in the runtime, not what's planned.** They are updated at implementation time, never before.

#### 5. Merge and Close

After review approval:
- Squash merge into `main`
- Issue auto-closes via `Closes #N` in the PR
- Project board item moves to Done

### Project Board States

The **Precept Language Improvements** project tracks proposals through:

| State | Meaning |
|-------|---------|
| Backlog | Proposal exists but isn't scheduled |
| Ready | Design is complete, acceptance criteria are clear, ready to implement |
| In Progress | Active feature branch and draft/open PR |
| In Review | PR is ready for review |
| Done | Merged |

### Wave Milestones

Language proposals are assigned to wave milestones that reflect priority and dependency order. Each wave has a theme and unlocks a set of authoring capabilities. Issues within a wave can often be worked in parallel unless there's an explicit dependency noted in the issue.

## Where Things Live

| Content | Location | Durability |
|---------|----------|------------|
| What the DSL syntax IS | `docs/PreceptLanguageDesign.md` | Permanent — tracks reality |
| What the C# API IS | `docs/RuntimeApiDesign.md` | Permanent — tracks reality |
| What a feature SHOULD BE | GitHub issue body | Until implemented |
| WHY a decision was made | `docs/research/` | Permanent — rationale archive |
| HOW to implement (checklist) | PR body | Ephemeral — dies with the PR |
| AI agent directives | `.github/copilot-instructions.md` | Permanent — updated as process evolves |

### Why not separate implementation plan docs?

Earlier in the project, implementation plans lived as standalone markdown files in `docs/` (e.g., `PreceptLanguageImplementationPlan.md`). This worked but created maintenance overhead:

- Plans went stale after implementation
- Two sources of truth: the plan doc and the actual code
- AI agents sometimes referenced outdated plan steps

The evolved process keeps the implementation checklist in the PR body (ephemeral by nature) and the durable decisions in spec docs (updated at merge time). Existing implementation plan files in `docs/` are historical artifacts that remain as reference but aren't the template for new work.

## Build & Test

Precept is built with .NET 10.0 and TypeScript.

```bash
dotnet build                        # Build everything
dotnet test                         # Run all tests (xUnit + FluentAssertions)
```

### First-time local setup

1. Run task `build`.
2. Run task `extension: install`, then reload the window.

If you previously used an older local plugin-registration flow, remove any stale `chat.pluginLocations` entry that points at `tools/Precept.Plugin/`. The current local model uses workspace-native `.github/agents/`, `.github/skills/`, and `.vscode/mcp.json` instead.

### Reload rules

| What you changed | Command | Reload VS Code? |
|------------------|---------|------------------|
| C# runtime or language server | `Ctrl+Shift+B` (Build task) | No |
| TypeScript, webview, or syntax | Task: `extension: install` | Yes |
| Agent or skill markdown | Reload Window | Yes |
| MCP server | Reload Window | Lazy rebuild on next tool call |

See [ArtifactOperatingModelDesign.md](docs/ArtifactOperatingModelDesign.md) for the local-vs-distribution operating model, worktree rules, the workspace `.vscode/mcp.json` `servers` schema, and the plugin payload sync boundary.

### Test projects

```bash
dotnet test test/Precept.Tests/                    # Core runtime + parser + type checker
dotnet test test/Precept.LanguageServer.Tests/     # Language server completions + diagnostics
dotnet test test/Precept.Mcp.Tests/                # MCP tool integration
```

## Conventions

- **Test framework:** xUnit with FluentAssertions
- **Test naming:** PascalCase + `Tests` suffix
- **Branch naming:** `feature/issue-N-description`, `chore/description`, `fix/issue-N-description`
- **Commit messages:** Imperative mood, reference issue number when applicable
- **PR merge strategy:** Squash merge
