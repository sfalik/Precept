---
name: lifecycle-4-execute
description: Stage 4 of the engineering lifecycle — execute against a locked design and phased plan. Triggers on — implement, execute, ship, build this, start the work, "implement phase N", "do the next slice", "open the PR for X". Captures vertical-slice discipline, PR-body update protocol, commit conventions, and post-slice doc-touch verification. Does NOT design (use /lifecycle-2-design) or plan (use /lifecycle-3-plan).
---

# Precept Execution

Stage 4 of the engineering lifecycle. Bridges plan (Stage 3) and promotion (Stage 5). The skill is execution discipline — it doesn't add new design content, but it enforces the vertical-slice cadence, PR-body-as-plan rule, doc-sync obligations, and commit-message conventions that keep execution traceable to the design and plan upstream.

## When to use

- A locked design and a phased plan are in place; ready to write code
- Starting the next vertical slice of an in-flight phase
- Opening the PR for a design / executing against a plan
- After `/lifecycle-3-plan` produces phases that need execution

## When NOT to use

- Design isn't locked yet → use `/lifecycle-2-design`
- No plan exists for non-trivial work → use `/lifecycle-3-plan` first
- The work is a small bug fix or polish with no design-and-plan upstream — just commit directly per the standard workflow in CONTRIBUTING.md
- Promoting design content to canonical docs → use `/lifecycle-5-promote`

## Before you start

Read these before opening the PR or writing code:

**Always:**
- `docs/philosophy.md` — Precept's core commitments
- `docs/README.md` — doc landscape
- `docs/language/README.md` — language surface entry point
- The locked design doc(s) and the plan doc you're executing against
- `CONTRIBUTING.md` — the canonical workflow (issue + PR + vertical slices)

**By topic** — navigate via the README system per the change you're making (catalog change → catalog-system.md; pipeline change → relevant stage doc; etc.).

## Required workflow

### 1. Open the draft PR immediately

The PR is the execution hub. Body structure per `CONTRIBUTING.md`:

```markdown
## Summary
<1-3 sentences on what this PR delivers>

## Linked Issue
Closes #N

## Why
<paragraph tying the work back to the design's Goal/Acceptance Criteria>

## Implementation Plan

<Until design review gate clears (Track A or Track B per CONTRIBUTING.md § 3),
this section says "Pending design review.">

<After gate clears: enumerate vertical slices, in execution order.
Each slice is a single coherent commit; the list is also the execution checklist.>
```

**Anti-patterns:**

- A separate `implementation-plan.md` file. The PR body is the plan artifact — never duplicate.
- An empty Implementation Plan post-design-review-clear. The plan is execution discipline; the section being empty signals execution has no scaffolding.
- A PR opened before the design is locked. Open the draft PR after Stage 2 completes, not before.

### 2. Vertical slices

Each commit / slice must be:

- **Coherent** — one logical change. Catalog entry + parser dispatch + type checker update + tests + doc-touch for one feature is one slice; two unrelated features is two slices.
- **Incremental** — tests pass after each slice (`dotnet test` green). A slice that leaves the tree red is a process violation.
- **Doc-synced** — per CLAUDE.md's routing table, the docs that describe the changed code are updated in the same commit. Stale "Implemented" claims are drift.
- **Catalog-first** — for language-surface or pipeline changes, the catalog entry lands first (in the slice that introduces the feature). Pipeline code derives from the catalog; if the slice adds pipeline code that hardcodes what a catalog should know, that's a catalog discipline violation.

After each slice: commit, push, update the PR-body checklist.

### 3. Commit messages

Format (mirrors recent commits in this repo):

```
chore(<area>): <short summary, lowercase>

<paragraph: what changed and why this slice exists in the phased plan>

<bullets: specific files / behaviors changed, with finding IDs if applicable>

Co-Authored-By: <model + version> <noreply@anthropic.com>
```

Areas observed in this repo: `phase-N`, `phase-N-followup`, `docs`, `lifecycle`, `archive`, `plan`. Pick the one that matches what the slice delivers.

**Anti-patterns:**

- Commit message says "fix" or "update" with no context. The why is the body of the message, not just the title.
- Body claims completeness the slice doesn't deliver. Honesty is the cost of incremental discipline — if a slice is partial, say so and name the gap.

### 4. Doc-sync per slice

For each slice, scan the CLAUDE.md routing table and verify the affected docs are updated. The reminder triangle:

- **Runtime**: parser / type checker / evaluator / diagnostics — the stage doc, diagnostic-system.md
- **Tooling**: LS completions / hover / semantic tokens, MCP vocabulary / DTOs, TextMate grammar (regenerated, not hand-edited)
- **MCP**: tool DTOs, `CatalogFormatters.cs`, `docs/tooling/mcp.md`

A slice that touches a non-negotiable surface (language surface, public API, diagnostic codes, MCP vocabulary, catalog member names) without doc updates in the same commit is a doc-sync violation.

### 5. Tests

- xUnit + FluentAssertions
- `PascalCase` + `Tests` suffix on test classes
- `[Fact]` / `[Theory]` attributes
- Every new diagnostic or feature gets a scenario test demonstrating it
- Sample files in `samples/` are NOT modified by this workstream unless the plan explicitly authorizes a sample edit (per CLAUDE.md's sample-edit constraint)

### 6. Post-slice update protocol

After each slice lands:

- Check off the slice in the PR-body Implementation Plan
- Update the plan doc's phase tracker if the plan tracks slice-level progress
- Update `docs/Working/bugs.md` if the slice closes a bug entry
- Note any decisions surfaced during execution (per the plan's "Open decisions" section)

### 7. Phase completion

A phase is complete when:

- All slices in the phase's Implementation Plan are checked off
- All phase-level exit criteria from the plan doc are satisfied (typically: `dotnet build` clean, `dotnet test` green, MCP probe battery returns expected outcomes)
- All phase-level doc-touch obligations are landed
- The plan doc's phase row is marked ✅ Complete with the commit hash

## Behavioral guards

The skill enforces:

1. **The PR body IS the implementation plan.** No separate implementation-plan.md file. Refused.
2. **Pending design review until the gate clears.** Implementation Plan section says "Pending design review" until Track A or Track B (per CONTRIBUTING.md § 3) signs off. Plans written before the gate are refused.
3. **Slices are coherent and incremental.** A slice that leaves tests red, mixes unrelated features, or skips doc-touch is refused.
4. **Catalog-first.** For language-surface or pipeline-touching slices, the catalog entry lands in the slice that introduces the feature — not in a follow-up. Pipeline code derives from catalog; if pipeline code hardcodes catalog knowledge in the slice, refused.
5. **Doc-sync in the same commit.** Affected docs (per the CLAUDE.md routing table) are updated in the slice that changes the behavior they describe. Cross-commit doc-sync (slice N changes code, slice N+1 updates docs) is refused — the description-of-current-reality drifts in slice N otherwise.
6. **Sample-edit constraint.** Sample files in `samples/` are not modified by this workstream unless the plan explicitly authorizes the edit. Stray sample changes are refused.
7. **No skipping vertical slices to amend.** When a pre-commit hook fails, fix the underlying issue and create a NEW commit; don't `--amend`. The hook failure means the commit didn't happen — `--amend` would modify the *previous* commit, potentially destroying earlier work.

## Composability

- **Input**: a locked design doc (`/lifecycle-2-design` output) + a phased plan (`/lifecycle-3-plan` output) + a GitHub issue.
- **Output**: a merged PR with vertical-slice commits, updated docs, green tests, and a checkable Implementation Plan in the PR body. Feeds `/lifecycle-5-promote` for canonicalizing the design's content into reference docs.

## Anti-patterns to refuse

- Open a separate `implementation-plan.md` file alongside the PR
- Begin coding before the design is locked
- Skip the PR-body Implementation Plan ("I'll fill it in as I go")
- Commit a slice that leaves tests red
- Land code changes in slice N and doc updates in slice N+1 ("docs are coming")
- Hand-edit `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (it's generated)
- Skip pre-commit hooks with `--no-verify`
- Modify samples without explicit plan authorization
- Amend a commit when the pre-commit hook failed (create a new commit instead)

## Quick reference

| Symptom | Skill response |
|---|---|
| Separate `implementation-plan.md` proposed | Refuse; PR body is the plan artifact |
| Code change without doc-touch in same slice | Refuse; routing-table doc updates land in the same commit |
| Pipeline code hardcoding token sets / per-member kind dispatch | Refuse; catalog-driven discipline applies in the slice that adds the feature |
| Sample edit not in plan | Refuse; sample-edit constraint applies |
| `--no-verify` to skip hooks | Refuse; fix the underlying issue |
| `--amend` after pre-commit hook failure | Refuse; create a new commit |
| Slice mixing unrelated features | Refuse; split into separate slices |
| PR body Implementation Plan empty after design gate cleared | Prompt to fill before any code lands |
