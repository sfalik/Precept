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

Every locked design decision in the proposal must include explicit rationale:
- **Why this choice** — the reasoning, not just the outcome
- **Alternatives rejected** — what else was considered and why it lost
- **Precedent** — what research or prior art grounds the decision
- **Tradeoff accepted** — what downside the team is deliberately taking on

A proposal that states WHAT without WHY is incomplete. Send it back for rationale before it moves to Ready.

Create the issue with labels `proposal` + `language` (and optionally `dsl-expressiveness` or `dsl-compactness`). Add it to the **Precept Language Improvements** project. Assign a wave milestone.

#### 2. Research (`docs/research/`)

Research documents live in `docs/research/` and capture:
- Precedent surveys (how other tools solve this)
- Dead ends explored and why they were rejected
- Design philosophy and rationale

Research files are linked from the issue, not the other way around. The issue map in `research/language/README.md` connects each proposal to its research starting points.

**Research is durable.** It explains *why* decisions were made and survives across sessions. When a proposal is revised, update the research doc with the new reasoning.

**Quality bar for rationale:** `research/language/expressiveness/computed-fields.md` and the Issue #17 proposal demonstrate the expected depth — alternatives surveyed with precedent, explicit tradeoff analysis, and design philosophy grounding each locked decision.

#### 3. Implementation (Feature Branch + PR)

When ready to implement:

1. Create a feature branch: `feature/issue-N-short-description`
2. Open a **draft PR** immediately, linked to the issue (`Closes #N`)
3. Create an implementation plan in the body of the PR, including checkmarks to track progress. This is ephemeral; it doesn't need to outlive the PR.
4. **Check off items as you complete them.** Update the PR body after each slice or logical group — not at the end. The checkbox list is a live progress tracker; it should reflect current state throughout development so reviewers and collaborators always know where things stand. Use the GitHub UI or `mcp_github_update_pull_request` to check off completed items.
5. Implement in vertical slices. Suggested order for cross-cutting changes:
   - Parser + model + diagnostics
   - Type checker
   - Runtime engine
   - Language server (completions, semantic tokens)
   - TextMate grammar (syntax highlighting)
   - MCP tools
   - Tests (throughout, not at the end)
   - Sample files
   - Documentation updates
6. Mark the PR as ready for review when all acceptance criteria are met.

#### 4. Documentation Sync (Same PR — Non-Negotiable)

Every implementation PR must update documentation in the same pass:

| What changed | Update |
|-------------|--------|
| New keyword, operator, or syntax | `docs/PreceptLanguageDesign.md` + `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` |
| New or changed API behavior | `docs/RuntimeApiDesign.md` |
| New editability semantics | `docs/EditableFieldsDesign.md` |
| New MCP tool behavior | `docs/McpServerDesign.md` |
| Feature claims in README | `README.md` |

**Design docs track what EXISTS in the runtime, not what's planned.** They are updated at implementation time, never before.

**Docs are a final slice, not interleaved.** Update documentation at the end of the implementation — after runtime, tooling, and tests are complete — but still in the same PR. Tests get the "throughout, not at the end" treatment; docs get the "final slice, same PR" treatment.

#### Proposal content at merge time

The table above covers which files to touch during implementation. This table is a closing checklist — where each section of the proposal must land before the issue is closed. **Nothing should exist only in a closed issue.** The issue body is a working document, not an archive.

| Proposal section | Destination at merge time |
|-----------------|--------------------------|
| Proposed syntax, behavior, examples | `docs/PreceptLanguageDesign.md` — syntax forms, grammar rules, operator tables, precedence, examples; also `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` for any new keywords or syntax forms |
| Locked design decisions (the **what**) | `docs/PreceptLanguageDesign.md` — named decisions in the relevant section |
| Locked design decisions (the **why** / rationale / alternatives rejected) | `docs/research/` — update or create the research doc; if rationale was only in the issue, move it here now |
| Explicit exclusions / out of scope | `docs/PreceptLanguageDesign.md` — named as deliberate exclusions so they aren't re-proposed later |
| Open questions resolved during implementation | Resolved decisions go to `docs/research/`; if they changed the design, update the design doc too |
| Acceptance criteria | Verified by the test suite — tests passing *is* the living acceptance criteria; no separate doc needed |
| Implementation scope checklist | PR body — ephemeral, discarded after merge |
| Dependencies / related issues | Tracked in the issues themselves — no migration needed |

The most commonly dropped items are **deliberate exclusions** (they disappear when the issue closes) and **resolved open questions** (the resolution often stays only in an issue comment). Both are durable decisions that belong in permanent homes.

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
| WHY a decision was made | Issue body (per-decision rationale) + `docs/research/` (full evidence base) | Permanent — rationale lives in both places |
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
