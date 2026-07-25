# Contributing to Precept

## Development Workflow

## Doc Lifecycle

Every meaningful design or implementation decision moves through seven stages. The lifecycle ensures that **why-content** (rationale, alternatives, tradeoffs, precedent) is captured at decision time and preserved as the work moves from idea to maintenance. Lifecycle skills automate the transitions between stages. Six of those skills exist — `/research`, `/design`, `/plan`, `/execute`, `/promote`, `/review`. The seventh stage's `/audit` skill has not been built yet.

| Stage | Activity | Skill | Where work lives |
|---|---|---|---|
| 1 | Research / explore | `/research` | `research/` |
| 2 | Lock a design | `/design` | `docs/Working/` |
| 3 | Plan execution | `/plan` | `docs/Working/` (plan doc) |
| 4 | Execute the plan | `/execute` | code + tests + the plan document's checklist (the PR body, once the PR workflow is back in use) |
| 5 | Promote to canonical | `/promote` | canonical `docs/` updated; design moved to `docs/Working/Archive/` with cross-link |
| 6 | Review any stage artifact | `/review` | umbrella reviewer: any stage artifact (research / design / plan / code / promotion) or whole-item completion check |
| 7 | Maintain | `/audit` — not yet built | canonical `docs/` |

`/review` is the engineering-lifecycle umbrella reviewer: it dispatches a stage artifact (research / design / plan / code / promotion) to the discipline that owns it, or runs the whole-item completion check on a bare `/review <work-item>`. Its **code branch does the built-in `/review`'s PR-review job** via `/code-review` + `precept-reviewer` and intentionally **subsumes (does not invoke)** the built-in. Its **promote branch is the per-artifact reviewer for the Stage 4 → 5 transition** this section names as the most common failure mode — it verifies a single promotion landed faithfully (every enumerated canonical doc touched, `**Promoted to:**` link and archive cross-link resolve) without needing a finished work item.

### Stage 5 is mandatory

The most common failure mode is **Stage 4 → 5 transition skipped**: implementation ships, design doc gets archived, but canonical doc never receives the "why." The 2026-05-24 compiler-readiness review found 16 instances of this pattern across hover-design, constructor-semantics, diagnostic-enforcement, SemanticTokenTypes Slice 10, and 12 more from the archive scan — substantial design rationale stranded in `docs/Working/Archive/` while canonical docs lagged.

**Rule**: every design doc moved to `docs/Working/Archive/` MUST carry a top-of-file header declaring one of:
- `**Promoted to:** <canonical link>` — the why lives in the linked canonical doc
- `**Status:** Historical — superseded by <link>` — concept evolved into a different design
- `**Status:** Historical — design dropped, no canonical replacement` — design abandoned

The `/promote` skill enforces this — it refuses to archive a design doc without the header. Manual archive moves (via `mv`) bypass the skill, which is allowed but reviewer-checked.

### Pointer-philosophy applies to canonical content

After Stage 5 promotion, canonical docs should preserve "why" content and point to code for "what" content. Concretely:

- **Enumerable content** (member lists, type/field shapes, counts, file paths) → **pointer to code**: e.g., `See \`src/Precept/Language/Tokens.cs\` for member list.`
- **Conceptual content** (architecture, design rationale, why-decisions, tradeoffs) → **hand-written, preserved across promotion**

The 2026-05-24 review found `catalog-system.md` had drifted on counts (14 of 14 catalogs had at least one count discrepancy) precisely because enumerable content was duplicated in the doc. Pointer-philosophy makes that class of drift mechanically impossible.

### Four-leg rationale policy

Per the "Per-Decision Rationale (Non-Negotiable)" section in `CLAUDE.md`, locked design decisions must include four legs:
1. **Rationale** — why this choice
2. **Alternatives considered** — and rejection reasons
3. **Precedent** — research / prior art / existing pattern grounding the choice
4. **Tradeoff accepted** — known downside being taken on

**Scope of the rule**:

- **New decisions** going through `/design`: **required**. The skill refuses to mark a design "Locked" without all four legs on every decision. Author can answer "no precedent — novel choice" or "no tradeoff identified — flag for review" honestly, but cannot skip.
- **Decisions backed by Archive design docs** (Stage 4 → 5 promotion): the `/promote` skill lifts whatever depth the source provides. Pre-policy designs with Decision + Rationale only get lifted as-is with a "no further rationale recorded in source" note. **No fabrication.**
- **Existing canonical doc § Design Rationale entries** without four legs: **grandfather**. No required backfill. They do not block promotion of new work. (Flagging them as gaps is Stage 7 maintenance work; the seventh stage's `/audit` skill has not been built yet.)

The rule's purpose is to prevent future ambiguity at decision time, not to retroactively annotate shipped code. Honest grandfathering beats fabricated four-leg structure.

### Doc routing table

When implementation work touches code, the canonical docs that may need updates depend on what's touched.

**The routing table lives in `CLAUDE.md` § Documentation Sync → Where to update for which change, and only there.** This file used to reproduce it in two places; both copies drifted from the original and from each other, so both were replaced with this pointer. If a kind of change is missing a row, add the row to `CLAUDE.md` — do not start a second table here.

`/design` consults that table when populating a design doc's "Doc-update enumeration" section. `/plan` uses that enumeration to populate per-phase doc-touch obligations. `/promote` verifies those obligations at promotion time.

The skills make routing automatic — authors don't need to memorize the table, but should understand it exists so they can override when the heuristic gets a case wrong.

### Proposal Lifecycle (Stages 1-3 of the Doc Lifecycle)

When PR-and-issue workflow is in use (main branch development), the proposal lifecycle below maps onto Stages 1-3 of the Doc Lifecycle:
- Stage 1 (Research) corresponds to "Research" below
- Stage 2 (Lock a design) corresponds to "Design Review" below + the design doc in Track B
- Stage 3 (Plan execution) corresponds to "Implementation plan" in the PR body

On spike branches without PRs (the current branch is `spike/Precept-V2-Radical-reset`), the lifecycle skills (`/research`, `/design`, `/plan`, `/execute`) handle the same transitions without the GitHub gates. The discipline is the same; the enforcement mechanism differs.

Stages 4-7 (execute, promote, review, maintain) are the same on both workflows.

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

Every implementation PR must update documentation in the same pass. **Which docs, for which change, is the routing table in `CLAUDE.md` § Documentation Sync → Where to update for which change.** It covers every row this section used to list separately, including editability semantics and the proof engine diagnostics that also require a sample under `test/integrationtests/diagnostics/` (see § Diagnostic Samples below).

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
- `line`, `start`, and `end` are the exact `Line`, `Column`, and `EndColumn` values emitted by `Compiler.Compile(string source)` in `src/Precept/Compiler.cs` — there is no `PreceptCompiler.CompileFromText()`, which this file used to name

**Nothing currently checks these expectations.** As of 2026-07-25 the eight files under `test/integrationtests/diagnostics/` carry `# Demonstrates:` headers and `# EXPECT:` comments, but no test reads either — searching the test projects for `EXPECT` finds no code that parses them. The header and expectation comments are therefore a convention maintained by hand, and a sample can drift away from what the compiler actually emits without anything failing. Write them accurately anyway, and treat a sample as unverified evidence until a checking test exists. Building that test — it would compile each sample, assert every `# EXPECT:` row matches an emitted diagnostic exactly, and fail on any extra diagnostic of any severity as well as on a missing or malformed header — is outstanding work, not a description of what is there.

#### Proposal content at merge time

The table above covers which files to touch during implementation. This table is a closing checklist — where each section of the proposal must land before the issue is closed. **Nothing should exist only in a closed issue.** The issue body is a working document, not an archive.

| Proposal section | Destination at merge time |
|-----------------|--------------------------|
| Proposed syntax, behavior, examples | `docs/language/precept-language-spec.md` — syntax forms, grammar rules, operator tables, precedence, examples; the catalog entry is added first; `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` is regenerated from catalog metadata |
| Locked design decisions (the **what**) | `docs/language/precept-language-spec.md` — named decisions in the relevant section |
| Locked design decisions (the **why** / rationale / alternatives rejected) | `research/` — update or create the research doc; if rationale was only in the issue, move it here now |
| Explicit exclusions / out of scope | `docs/language/precept-language-spec.md` — named as deliberate exclusions so they aren't re-proposed later |
| Open questions resolved during implementation | Resolved decisions go to `research/`; if they changed the design, update the design doc too |
| Acceptance criteria | Verified by the test suite — tests passing *is* the living acceptance criteria; no separate doc needed |
| Implementation scope checklist | PR body — ephemeral, discarded after merge |
| Dependencies / related issues | Tracked in the issues themselves — no migration needed |

The most commonly dropped items are **deliberate exclusions** (they disappear when the issue closes) and **resolved open questions** (the resolution often stays only in an issue comment). Both are durable decisions that belong in permanent homes.

#### Implementation Plan Quality Bar

The `## Implementation Plan` in the PR body is the execution blueprint. A plan that says "implement narrowing" is useless; a plan that says "create `TryApplyNumericComparisonNarrowing` in `src/Precept/Pipeline/TypeChecker.Expressions.cs` (~30 lines), called from the narrowing path after the null-comparison case" is actionable. Name real files — the type checker is split across several `src/Precept/Pipeline/TypeChecker.*.cs` files, so locate the one you mean before writing the plan. Every plan must meet this bar before coding begins.

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

### Spike Workflow

A spike is exploratory work that validates a hypothesis or explores a design space without committing to a PR. Spikes are first-class in the Squad process — they have their own mode, ceremonies, and conventions.

**When to spike:**
- You need to validate a design approach before writing a proposal
- You're exploring a trade-off that requires actual code to understand
- You're stress-testing catalog extensibility, analyzer behavior, or parser mechanics before committing to a full implementation plan

**Spike mode rules:**
1. **No PRs.** A spike never opens a PR during the spike. All commits go directly to the spike branch.
2. **No new branches.** Work on the spike branch only. No `git checkout -b` during a spike.
3. **Branch naming.** Spike branches use the `spike/{kebab-description}` prefix (e.g., `spike/catalog-extensibility`).
4. **Ceremonies suppressed.** Design review and PR review ceremonies are suppressed during a spike. The implementation gate does not fire.
5. **Exit deliberately.** End a spike with the Spike Closeout ceremony: decide what to keep, promote findings to a proper proposal or PR, clear spike mode.

**Activating spike mode:**
- Say "let's start a spike on X" — the coordinator activates spike mode, sets `spike_mode: true` in `.squad/identity/now.md`, and records the spike question.
- Or run the Spike Kickoff ceremony manually.

**Closing out a spike:**
- Say "end the spike" or "close out the spike" — triggers the Spike Closeout ceremony.
- The closeout captures what was learned, decides what to keep, and produces a proper implementation PR or discards the spike.

**Spike vs. feature branch:**
A spike is NOT a slow-moving feature branch. If the work is ready for review, it is not a spike — open a proper PR and go through the implementation gate.

**Doc lifecycle on spike branches**:

The doc lifecycle (Stages 1-7) applies in full on spike branches. Without GitHub PRs as enforcement gates, the lifecycle skills become the primary discipline:

- `/research` — exploration in `research/`
- `/design` — lock the design with four-leg rationale; refuses to lock without
- `/plan` — phased execution plan with decisions surfaced as gates
- `/execute` — vertical-slice discipline, doc-sync per slice, catalog-first. On a spike branch it updates the plan document's checklist after each slice instead of a PR body, and it refuses to open a pull request on a `spike/*` branch
- `/promote` — lift "why" to canonical, archive with header. **Mandatory** — design docs cannot reach Archive without it (or the explicit historical-status header).
- `/review` — umbrella reviewer: reviews any stage artifact (research / design / plan / code / promotion) by dispatching to the discipline that owns it, or runs the whole-item completion check on a bare `/review <work-item>` before declaring a work item closed. The code branch does the built-in `/review`'s PR-review job via `/code-review` + `precept-reviewer` and intentionally subsumes (does not invoke) the built-in.

Reviewer prompts (the GitHub PR template equivalent) are folded into `/plan` (doc-touch obligations enumerated upfront) and `/promote` (obligations verified at promotion). The discipline lives in the skills the agent invokes, not in a manual checklist.

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
| What the DSL syntax IS | `docs/language/precept-language-spec.md` + catalogs in `src/Precept/Language/` | Permanent — tracks reality |
| What the C# API IS | `docs/runtime/runtime-api.md` | Permanent — tracks reality |
| What a feature SHOULD BE | GitHub issue body | Until implemented |
| WHY a decision was made | Issue body (per-decision rationale) + `research/` (full evidence base) | Permanent — rationale lives in both places |
| What changed, why this PR exists, and HOW to implement (summary + reviewer context + checklist) | PR body | Ephemeral — dies with the PR |
| Design doc in "to be" form (Track B) | PR branch — reaches `main` only with implementing code | Ephemeral on branch — permanent once merged |
| AI agent directives | `CLAUDE.md` governs Claude Code and is loaded in every session and sub-agent spawn; `.github/copilot-instructions.md` governs Copilot. Keep them in agreement | Permanent — updated as process evolves |

### Doc Lifecycle Path

| Stage | Path |
|---|---|
| Research (Stage 1) | `research/<subfolder>/<topic>.md` |
| Locked design (Stage 2) | `docs/Working/<topic>.md` with frontmatter `status: Locked YYYY-MM-DD` |
| Execution plan (Stage 3) | `docs/Working/<topic>-plan-YYYY-MM-DD.md` |
| Canonical (Stage 5+) | `docs/compiler/` / `docs/language/` / `docs/tooling/` / `docs/runtime/` per the routing table |
| Archived design (Stage 5 complete) | `docs/Working/Archive/<original-filename>` with `**Promoted to:**` header |
| Lifecycle review report (Stage 6) | `docs/Working/lifecycle-review-<work-item>-YYYY-MM-DD.md` |
| Audit reports (recurring Stage 7) | `docs/Working/<workstream>-review-YYYY-MM-DD.md` |

The lifecycle skills — `/research`, `/design`, `/plan`, `/execute`, `/promote`, `/review` — handle moves between these locations.

### Why not separate implementation plan docs?

Earlier in the project, implementation plans lived as standalone markdown files in `docs/` (e.g., `PreceptLanguageImplementationPlan.md`). This worked but created maintenance overhead:

- Plans went stale after implementation
- Two sources of truth: the plan doc and the actual code
- AI agents sometimes referenced outdated plan steps

The evolved process keeps the implementation checklist in the PR body (ephemeral by nature) and the durable decisions in spec docs (updated at merge time). Existing implementation plan files in `docs/` are historical artifacts that remain as reference but aren't the template for new work.

## Catalog-Driven Architecture

Precept uses a **metadata-driven architecture.** Domain knowledge is declared as structured metadata in catalogs. Pipeline stages are generic machinery that reads it. This is not a style preference — it is the architectural identity of the system.

The canonical statement of this principle is [`docs/language/catalog-system.md § Architectural Identity`](docs/language/catalog-system.md#architectural-identity-metadata-driven). Read it before making any decision about what gets cataloged, what stays bare, or how pipeline stages consume language knowledge.

The operational companion — a reviewer checklist and implementer guide — is [`docs/contributing/catalog-driven-checklist.md`](docs/contributing/catalog-driven-checklist.md). Every contributor and reviewer should internalize it.

**Core rules (non-negotiable):**

- Every new token, keyword, type, operator, action, modifier, constraint, grammar construct, diagnostic, or fault is a catalog entry. No language element exists as a bare constant, inline set, or ad-hoc condition.
- Operator precedence derives from the Operators catalog — never hardcoded inline.
- Keyword membership (lexer lookup, `KeywordsValidAsMemberName`) derives from `Tokens.All` — no parallel lists.
- Parser disambiguation derives from catalog metadata — no manual `FrozenSet<TokenKind>` or hardcoded token sets.
- No pipeline stage switches on a catalog member's enum identity to apply per-member behavior. That behavior is metadata and belongs in the catalog entry.

**When a reviewer offers a non-catalog solution alongside a catalog solution, the non-catalog option should not exist.** If the catalog can express the requirement — and it almost always can — the non-catalog approach is not an option to weigh. It is wrong. The only valid exception is a documented structural limitation of the catalog system itself, stated with explicit reasoning.

## Build & Test

Precept is built with .NET 10.0 and TypeScript.

```bash
dotnet build                        # Build everything
dotnet test                         # Run all tests (xUnit + FluentAssertions)
```

### Invariants must not depend on the build configuration (Non-Negotiable)

`Directory.Build.props` at the repo root sets portable PDB symbols everywhere, and defaults the configuration to Release **only when nothing else has already set it**:

```xml
<Project>
  <PropertyGroup>
    <Configuration Condition="'$(Configuration)' == ''">Release</Configuration>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>portable</DebugType>
  </PropertyGroup>
</Project>
```

**That default does not make the repository Release-only, and the plain `dotnet build` in this file is a Debug build.** Measured 2026-07-25:

- `dotnet build` at the repo root resolves `Precept.slnx`, and a solution build supplies its own configuration before `Directory.Build.props` is evaluated. The condition is false, the default never applies, and every project lands in `bin/Debug/` — verified against all eleven project outputs.
- `dotnet build src/Precept/Precept.csproj`, building one project with no solution involved, leaves `Configuration` unset, so the default does apply and the output lands in `bin/Release/`.
- `dotnet build -c Release` at the repo root builds the whole solution in Release. Use it when you specifically need Release output.

So a given file may be compiled either way depending on how the build was invoked, and `DEBUG` is defined in the common case. **Nothing that has to hold in production may be written so that it only runs in one configuration.**

**Forbidden in pipeline code:**

- `Debug.Assert(...)`, `Debug.Fail(...)` — these are stripped in Release. Any invariant that needs to hold in production must `throw new InvalidOperationException(...)` unconditionally.
- `#if DEBUG` blocks around invariants — same reason.
- `[Conditional("DEBUG")]` methods that guard invariants — same reason.

**Conversion pattern when retrofitting:**

```csharp
// Wrong — stripped in Release
Debug.Assert(condition, "message");

// Right — survives Release; the invariant holds in production
if (!condition)
    throw new InvalidOperationException("message");
```

**Why a hard rule and not a guideline.** Precept's identity is structural prevention. An invariant that only fires in Debug is an invariant we cannot rely on at the customer site, because customers run Release. A "Debug.Assert here" silently degrades to "no check in production" — exactly the kind of approximation Precept rejects in the language it governs. Holding ourselves to the same standard keeps the runtime honest about which checks are real.

**Diagnostic-emission corollary (D26).** Any pipeline path that produces a `TypedErrorExpression` must emit at least one Error-severity diagnostic on the same context. Returning an error-shaped typed node without a diagnostic is a silent failure that Release builds cannot catch via assertions. The right check is an unconditional throw at every error-construction site that lacks the matching diagnostic emit.

### First-time local setup

1. Run task `build`.
2. Run task `extension: install`, then reload the window.

The `extension: install` task is driven by Node and works on Windows, macOS, and Linux. It needs **Node.js + npm** and the **`code` (or `code-insiders`) CLI** on your `PATH` — the script invokes `code --install-extension <vsix>` to install the freshly packaged extension into your local profile.

- **Windows:** `code` is added to `PATH` by the VS Code installer.
- **macOS:** run `Shell Command: Install 'code' command in PATH` from the command palette.
- **Linux:** `code` is on `PATH` already if you installed VS Code via apt, dnf, snap, or AUR; otherwise add the VS Code `bin/` directory yourself.

VS Code Insiders is auto-detected from the launching terminal's environment and routed to `code-insiders` instead of `code`.

## MCP Configuration Files

**The inventory lives in `CLAUDE.md` § Development Workflow and only there** — three files define MCP servers, plus `.claude/settings.local.json`, which is not a server definition but decides which of them Claude Code enables. That section names each file, its schema, and every server it declares. This file used to carry a second, shorter copy that had drifted; it was replaced with this pointer.

The one rule worth repeating here: do not let the two development files drift into separately hand-authored contracts. `.vscode/mcp.json` and the repo-root `.mcp.json` must keep pointing `precept` at the same source-first launch script, `tools/scripts/start-precept-mcp.js`.

### Reload rules

| What you changed | Command | Reload VS Code? |
|------------------|---------|------------------|
| C# runtime or language server | `Ctrl+Shift+B` (Build task) | No |
| Catalog metadata (grammar changes) | Task: `grammar: regenerate`, then Task: `extension: install` | Yes |
| TypeScript, webview, or syntax | Task: `extension: install` | Yes |
| Agent or skill markdown | Reload Window | Yes |
| MCP server | Reload Window | Lazy rebuild on next tool call |

See [Precept Plugin README](tools/Precept.Plugin/README.md) for the local-vs-distribution operating model, worktree rules, the `.vscode/mcp.json` VS Code/workspace-local `servers` schema, the repo-root `.mcp.json` Copilot CLI `mcpServers` schema, and the plugin payload sync boundary.

### Test projects

```bash
dotnet test test/Precept.Tests/                    # Core runtime + parser + type checker + proof engine
dotnet test test/Precept.LanguageServer.Tests/     # Language server completions + diagnostics
dotnet test test/Precept.Mcp.Tests/                # MCP tool integration
dotnet test test/Precept.Analyzers.Tests/          # Roslyn analyzers in src/Precept.Analyzers
dotnet test test/Precept.MatrixTools.Tests/        # Obligation-matrix tooling in tools/Precept.MatrixTools
```

There are five test projects. `dotnet test` at the repo root runs all of them.

## Conventions

- **Test framework:** xUnit with FluentAssertions
- **Test naming:** PascalCase + `Tests` suffix
- **Branch naming:** `feature/issue-N-description`, `chore/description`, `fix/issue-N-description`
- **Commit messages:** Imperative mood, reference issue number when applicable
- **PR merge strategy:** Squash merge
