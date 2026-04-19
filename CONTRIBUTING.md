# Contributing to Precept

## Development Workflow

### Proposal Lifecycle

Every language or runtime change follows this flow:

```
Idea → GitHub Issue (proposal) → Research → Design Review (owner sign-off) → Implementation PR → Merge → Docs updated
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

#### 2. Research (`research/`)

Research documents live in `research/` and capture:
- Precedent surveys (how other tools solve this)
- Dead ends explored and why they were rejected
- Design philosophy and rationale

Research files are linked from the issue, not the other way around. The issue map in `research/language/README.md` connects each proposal to its research starting points.

**Research is durable.** It explains *why* decisions were made and survives across sessions. When a proposal is revised, update the research doc with the new reasoning.

**Quality bar for rationale:** `research/language/expressiveness/computed-fields.md` and the Issue #17 proposal demonstrate the expected depth — alternatives surveyed with precedent, explicit tradeoff analysis, and design philosophy grounding each locked decision.

#### 3. Design Review

Design review is a formal gate for every proposal. No implementation plan is authored until the design review ceremony completes with owner (Shane) sign-off.

Proposals follow one of two tracks:

**Track A — Standard proposal (no new canonical design):**

```
Issue → Research → Design Review Ceremony (issue comments, owner sign-off)
  → Implementation plan authored in PR body → Implement in vertical slices → Code Review → Merge
```

- Design review targets the proposal issue — decisions, acceptance criteria, scope.
- Review comments live on the issue as structured issue comments (per the proposal-review skill).
- Existing design docs (`docs/`) are updated as a final slice in the implementation PR. This rule is unchanged.

**Track B — Design-introducing proposal:**

A proposal declares Track B in the issue body when it introduces a new or substantially expanded canonical design document (a new file in `docs/` OR a major new section in an existing design doc).

```
Issue (declares "introduces canonical design: docs/Foo.md") → Research
  → Draft PR (design doc committed in "to be" form)
  → Design Review Ceremony (issue comments + inline PR comments on markdown, owner sign-off)
  → All inline review comments resolved → Implementation plan authored in PR body
  → Implement in vertical slices → Code Review → Merge
```

- Design review targets the proposal issue AND the design doc on the PR (inline review comments on the markdown diff).
- All inline PR review comments on the design doc must be resolved before the design review is considered complete.
- The design doc is the first artifact on the branch; implementation follows. Both land on `main` together when the PR merges — no future-state docs on `main` without implementing code.
- Same branch, same PR — the PR starts as a design PR and evolves into a design+implementation PR.

**Universal rules (both tracks):**

1. The implementation PR may be opened early — to carry research, design docs, or other pre-implementation artifacts — but `## Implementation Plan` stays empty (or explicitly says "Pending design review") until the gate clears.
2. Owner (Shane) signs off to mark the design review complete.
3. No implementation plan is authored, and no coding begins, until design review is complete.

#### 4. Implementation (Feature Branch + PR)

When ready to implement:

1. Create a feature branch: `feature/issue-N-short-description`
2. Open a **draft PR** immediately, linked to the issue (`Closes #N`)
3. Use the exact PR-body structure required by the repository template. Required sections:
   - `## Summary` — what changed in reviewer-facing terms
   - `## Linked Issue` — include `Closes #N`
   - `## Why` — why this PR exists, what problem it addresses, and any implementation-specific reviewer context; do **not** duplicate the full proposal rationale or alternatives from the issue/research docs
   - `## Implementation Plan` — checkbox checklist tracking vertical slices. **Note:** This section says "Pending design review" until the design review gate clears (see § 3. Design Review above). Do not author the plan until owner sign-off.
4. **Build a detailed implementation plan after design review completes.** The plan lives in the PR body's `## Implementation Plan` section. See the [Implementation Plan Quality Bar](#implementation-plan-quality-bar) below for requirements.
5. **Check off items as you complete them.** Update the PR body after each slice or logical group — not at the end. The checkbox list is a live progress tracker; it should reflect current state throughout development so reviewers and collaborators always know where things stand. Keep the `## Summary` and `## Why` sections current too if the shipped scope or reviewer context changes during implementation. Use the GitHub UI or `mcp_github_update_pull_request` to keep the PR body current.
6. Implement in vertical slices. Suggested order for cross-cutting changes:
   - Parser + model + diagnostics
   - Type checker
   - Runtime engine
   - Language server (completions, semantic tokens)
   - TextMate grammar (syntax highlighting)
   - MCP tools
   - Tests (throughout, not at the end)
   - Sample files
   - Documentation updates
7. Mark the PR as ready for review when all acceptance criteria are met.

#### 5. Documentation Sync (Same PR — Non-Negotiable)

Every implementation PR must update documentation in the same pass:

| What changed | Update |
|-------------|--------|
| New keyword, operator, or syntax | `docs/PreceptLanguageDesign.md` + `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` |
| New or changed API behavior | `docs/RuntimeApiDesign.md` |
| New editability semantics | `docs/EditableFieldsDesign.md` |
| New MCP tool behavior | `docs/McpServerDesign.md` |
| Feature claims in README | `README.md` |
| New or changed proof engine diagnostic (C76, C92–C98, future) | `test/integrationtests/diagnostics/` — add or update a `.precept` sample that demonstrates the diagnostic scenario. See § Diagnostic Samples below. |

**Design docs track what EXISTS in the runtime, not what's planned.** They are updated at implementation time, never before. **Exception — Track B proposals:** For Track B proposals, the design doc is committed on the branch in "to be" form as the first artifact. It only reaches `main` alongside the implementing code. This is an exception to the general rule — Track B docs describe the target state but are gated behind the same PR as the implementation that realizes them.

**Docs are a final slice, not interleaved.** Update documentation at the end of the implementation — after runtime, tooling, and tests are complete — but still in the same PR. Tests get the "throughout, not at the end" treatment; docs get the "final slice, same PR" treatment.

#### 6. Diagnostic Samples (Same PR — Non-Negotiable)

The `test/integrationtests/diagnostics/` folder contains `.precept` files that demonstrate the proof engine's diagnostic scenarios. These are **user-facing reference samples** — not test fixtures. They show authors what the proof engine catches, what messages it produces, and how to fix the code.

**Maintenance rule:** When a PR adds, changes, or removes a proof engine diagnostic (C76, C92–C98, and any future proof-backed diagnostics), the same PR must add or update the corresponding sample in `test/integrationtests/diagnostics/`. This is part of the documentation sync, not a separate phase.

**Sample file conventions:**

| Convention | Rule |
|-----------|------|
| **Naming** | `{scenario-slug}.precept` — descriptive, kebab-case (e.g., `divisor-safety.precept`, `contradictory-rules.precept`) |
| **Structure** | Each file is a self-contained precept demonstrating one diagnostic family or closely related diagnostics |
| **Comments** | Use `//` comments to explain what the proof engine proves, what diagnostic fires, and why |
| **Both sides** | Show both the triggering pattern (diagnostic fires) AND the fixed version (diagnostic resolved) in the same file where practical |
| **Attribution** | Comment at top: which diagnostics the file demonstrates (e.g., `# Demonstrates: C92, C93 — divisor safety`) |

**When to add a new sample vs. update an existing one:**
- New diagnostic family (e.g., C94 assignment constraints) → new file
- Refinement to existing diagnostic (e.g., better C93 message) → update existing file
- New proof composition pattern (e.g., conditional + relational) → new file if it demonstrates a distinct author scenario

**Evolution:** As the proof engine grows (collection reasoning, string constraints, cross-field analysis), new samples should be added to cover those scenarios. The `test/integrationtests/diagnostics/` folder is a living catalog of what the engine can prove.

**Expectation contract:** Every emitted diagnostic in a diagnostic sample must have an adjacent `# EXPECT:` comment that declares the full assertion contract:

```text
# EXPECT: C94 | severity=error | match=exact | message=Assignment to 'Score' is provably outside the field's constraint range. Expression produces 200 to 600 (inclusive), but field requires 0 to 100 (inclusive). | line=19 | start=39 | end=52
```

- `code` is the human-facing diagnostic family (`C76`, `C92`, etc.)
- `severity` is `error`, `warning`, or `hint`
- `match` is `exact` or `contains`
- `message` is the required visible diagnostic text; prefer `match=exact` and use `contains` only when the visible surface intentionally includes dynamic context that would make exact matching brittle
- `line`, `start`, and `end` are the exact `Line`, `Column`, and `EndColumn` values emitted by `PreceptCompiler.CompileFromText()`

**Drift prevention:** Every diagnostic sample is backed by a test in `test/Precept.Tests/DiagnosticSampleDriftTests.cs`. The test reads the sample's `# Demonstrates:` header and `# EXPECT:` comments, compiles the file, and asserts the expectations match the emitted diagnostics exactly. No extra diagnostics of any severity are allowed — not just no unexpected errors. A discovery test fails if any sample file lacks the header or malformed expectation metadata. When adding a new sample, no manual test wiring is needed — the theory test auto-discovers `test/integrationtests/diagnostics/*.precept` files.

#### Proposal content at merge time

The table above covers which files to touch during implementation. This table is a closing checklist — where each section of the proposal must land before the issue is closed. **Nothing should exist only in a closed issue.** The issue body is a working document, not an archive.

| Proposal section | Destination at merge time |
|-----------------|--------------------------|
| Proposed syntax, behavior, examples | `docs/PreceptLanguageDesign.md` — syntax forms, grammar rules, operator tables, precedence, examples; also `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` for any new keywords or syntax forms |
| Locked design decisions (the **what**) | `docs/PreceptLanguageDesign.md` — named decisions in the relevant section |
| Locked design decisions (the **why** / rationale / alternatives rejected) | `research/` — update or create the research doc; if rationale was only in the issue, move it here now |
| Explicit exclusions / out of scope | `docs/PreceptLanguageDesign.md` — named as deliberate exclusions so they aren't re-proposed later |
| Open questions resolved during implementation | Resolved decisions go to `research/`; if they changed the design, update the design doc too |
| Acceptance criteria | Verified by the test suite — tests passing *is* the living acceptance criteria; no separate doc needed |
| Implementation scope checklist | PR body — ephemeral, discarded after merge |
| Dependencies / related issues | Tracked in the issues themselves — no migration needed |

The most commonly dropped items are **deliberate exclusions** (they disappear when the issue closes) and **resolved open questions** (the resolution often stays only in an issue comment). Both are durable decisions that belong in permanent homes.

#### Implementation Plan Quality Bar

The `## Implementation Plan` in the PR body is the execution blueprint. A plan that says "implement narrowing" is useless; a plan that says "create `TryApplyNumericComparisonNarrowing` in `PreceptTypeChecker.cs` (~30 lines), wire into `ApplyNarrowing` after the null-comparison branch at line 2152" is actionable. Every plan must meet this bar before coding begins.

**Required elements per slice:**

| Element | Why |
|---------|-----|
| **Create vs. Modify** | Distinguish new methods/classes from changes to existing ones. Name each method, the file it lives in, and approximate size. |
| **Exact file paths** | Every slice lists the files it touches. No ambiguity about where changes land. |
| **Method-level specificity** | Name the methods to create or modify. Reference line numbers or structural landmarks (e.g., "after the null-comparison branch in `ApplyNarrowing`") when modifying existing code. |
| **Tests per slice** | Each slice specifies its test methods — names, assertion style (`[Fact]` vs `[Theory]` with row counts), and what each test verifies. Tests are part of the slice, not a separate phase. |
| **Regression anchors** | For slices that replace or refactor existing behavior, list the exact existing test method names that must pass unchanged. |
| **Dependency ordering** | State which slices must precede others and why. A reviewer should be able to read the ordering constraints and understand the critical path. |

**Required plan-level elements:**

| Element | Why |
|---------|-----|
| **File inventory table** | A single table mapping every file to the slices that touch it. Reviewers use this to scope their review. |
| **Tooling/MCP sync assessment** | Explicit statement per category (syntax highlighting, completions, semantic tokens, MCP) — either "changes needed" with specifics or "no changes needed" with reasoning. |

**How to build a plan (process):**

1. **Read the full issue body AND all comments.** Implementation notes, scope additions, test requirements, and ordering constraints often appear in comments — not the body. Missing a comment means missing scope.
2. **Explore the codebase** before planning. Locate the exact files, methods, and line numbers involved. A plan built on assumptions about code structure will be wrong.
3. **Organize as vertical slices** — each slice is independently testable and delivers a coherent unit of behavior. Slices are not "parser, then type checker, then tests" — they are "feature X end-to-end including its tests."
4. **Include the dependency graph.** If Slice 2 depends on Slice 1's infrastructure, say so explicitly. If slices can be parallelized, note that too.

**Quality bar exemplar:** PR #108 (`feat: compile-time divisor safety via unified narrowing`) demonstrates the expected depth — 9 vertical slices with method-level specificity, exact file paths, ~56 edge-case tests mapped to slices, 16 named regression anchors, dependency ordering, and a file inventory table.

**A plan that fails this bar is incomplete.** Send it back for detail before coding begins — just as a proposal without rationale is sent back for rationale.

#### 7. Merge and Close

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
| WHY a decision was made | Issue body (per-decision rationale) + `research/` (full evidence base) | Permanent — rationale lives in both places |
| What changed, why this PR exists, and HOW to implement (summary + reviewer context + checklist) | PR body | Ephemeral — dies with the PR |
| Design doc in "to be" form (Track B) | PR branch — reaches `main` only with implementing code | Ephemeral on branch — permanent once merged |
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
