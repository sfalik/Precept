---
name: execute
description: Stage 4 of the engineering lifecycle — execute against a locked design and phased plan. Triggers on — implement, execute, ship, build this, start the work, "implement phase N", "do the next slice", "open the PR for X". Captures the execution rigor (enumerate the input space + test-first before coding, delegate well-specified work to a fresh worktree agent, adversarial diff review before commit) and the execution cadence (vertical-slice discipline, PR-body update protocol, commit conventions, post-slice doc-touch verification). Does NOT design (use /design) or plan (use /plan).
---

# Precept Execution

Stage 4 of the engineering lifecycle. Bridges plan (Stage 3) and promotion (Stage 5). The skill is execution discipline — it doesn't add new design content, but it enforces the vertical-slice cadence, PR-body-as-plan rule, doc-sync obligations, and commit-message conventions that keep execution traceable to the design and plan upstream.

## When to use

- A locked design and a phased plan are in place; ready to write code
- Starting the next vertical slice of an in-flight phase
- Opening the PR for a design / executing against a plan
- After `/plan` produces phases that need execution

## When NOT to use

- Design isn't locked yet → use `/design`
- No plan exists for non-trivial work → use `/plan` first
- The work is a small bug fix or polish with no design-and-plan upstream — just commit directly per the standard workflow in CONTRIBUTING.md
- Promoting design content to canonical docs → use `/promote`

## High & Ultra Modes

**Trigger:** `/execute high <args>` or `/execute ultra <args>` (also recognise "high-rigour"/"ultra" phrasing in the request). **Opt-in only** — these spend many sub-agents and tokens; they are never the default. Reach for them on high-stakes, hard-to-reverse, or easy-to-get-subtly-wrong work where a single pass is not enough.

Both modes run this skill as a multi-agent `Workflow` instead of a single inline pass, and add independent multiplicity + adversarial verification *on top of* this skill's normal discipline — which still fully applies (nothing below replaces the required structure, gates, or checks). Every spawned agent works fluency-first and verifies its claims against source.

- **high** — per slice: a faithful builder, then ≥2 independent adversarial verifiers each try to break the slice against its acceptance before integrate, plus mutation-test the slice's tests.
- **ultra** — per slice, **N-version**: multiple independent builders of the same slice (converge, or investigate divergence), a perspective-diverse adversarial-verifier panel, 100% mutation-kill on the slice, and the standing trust harness.

---

## Execution mode: PR mode vs. spike-branch mode

The cadence below has two modes. They differ **only** in the execution-hub artifact — the rigor, vertical-slice discipline, doc-sync, commit conventions, and review gates are identical in both.

- **PR mode (default).** Issue → feature branch → draft PR → merge to `main`. The **draft PR body is the execution hub and the plan artifact** (`Closes #N`, Implementation Plan checklist). This is the `CONTRIBUTING.md` flow; use it for any branch destined to merge to `main`.
- **Spike-branch mode.** Work on a long-lived `spike/*` branch where **no PR is opened** and commits land directly on the branch. There is no PR body, no `Closes #N`, no merge ceremony. The **execution hub and plan artifact is the plan doc** (`docs/Working/<topic>-plan-YYYY-MM-DD.md`) produced by `/plan` — its slice list is the checklist, its phase rows are the progress tracker. A GitHub issue is optional.

**Detecting the mode:** if the current branch matches `spike/*` (or the owner has said "no PR on this branch"), you are in spike-branch mode — read every "PR body" instruction below as "plan doc," skip the draft-PR step, and commit directly on the spike branch. When in doubt, ask which mode applies; don't open a PR on a spike branch without confirmation.

In both modes, slice boundaries still pause for review, and the plan/PR artifact is kept current after every slice — the rule "the plan is a live artifact, never a separate throwaway file" is mode-independent.

## Before you start

Read these before opening the PR (PR mode) / updating the plan doc (spike mode) or writing code:

**Always:**
- `docs/philosophy.md` — Precept's core commitments
- `docs/README.md` — doc landscape
- `docs/language/README.md` — language surface entry point
- The locked design doc(s) and the plan doc you're executing against
- `CONTRIBUTING.md` — the canonical workflow (issue + PR + vertical slices)

**By topic** — navigate via the README system per the change you're making (catalog change → catalog-system.md; pipeline change → relevant stage doc; etc.).

## Rigor before cadence (enumerate → test-first → gate → delegate → review)

The numbered workflow below is the execution *cadence* — slices, commits, doc-sync. This section is the *rigor* that precedes and surrounds it: the practices that keep a slice from shipping a gap. Grounded in Anthropic's Claude Code best practices ([best practices](https://code.claude.com/docs/en/best-practices), [building effective agents](https://www.anthropic.com/research/building-effective-agents)). Hard-won lesson: a slice coded from the acceptance / happy-path examples — without enumerating the full input space first — surfaces a pile of regressions mid-build instead of failing loudly up front.

### Before writing code for a slice

1. **Enumerate the full input/obligation space.** List every shape the change must handle — every source-expression form, every obligation/requirement kind, every soundness rule — and **probe real behavior** on each, not just the examples in the design. Under-enumeration is the most common cause of a mid-build regression pile.
2. **Write the failing test matrix FIRST (test-first / TDD).** Turn the enumerated space into failing tests *before* implementation — one per shape, plus one per soundness/acceptance rule. Anthropic: *"write a failing test that reproduces the issue, then fix it"*; *"provide verification criteria with example test cases."* Writing the matrix first *forces* the enumeration, so a missing shape fails loudly up front. (This sharpens § 5 Tests below: tests come *before* the code, not alongside it.)
3. **The gate (the repeat-preventer).** Code only when the slice is specified *and* tested tightly enough to brief a fresh agent **without judgment calls**. If it isn't, that's the signal to enumerate/design more — a slice needing a genuinely new mechanism gets a focused `/design` first, not a code-first attempt.

> **⚠️ Fresh-build check before trusting `precept_compile`.** The precept MCP server serves the build from when it **last spawned** (`start-precept-mcp.js` rebuilds on spawn, then runs a frozen snapshot). If `src/Precept` or `tools/Precept.Mcp` changed this session, `precept_compile` / `precept_diagnostic` are **stale** — they reflect old compiler behavior. Ask the owner to run **`/mcp reconnect precept`** (rebuilds on reconnect — no session restart needed), *then* probe. When an MCP result disagrees with a freshly-built unit test (`dotnet test`), trust the test and suspect a stale server first — don't root-cause a phantom bug. For ground-truth that's never stale, compile directly against the freshly-built core (`Compiler.Compile(source).Diagnostics`) via the test project rather than the MCP wrapper.

### Delegating implementation

4. **Delegate well-specified implementation to a fresh worktree agent.** A `general-purpose` agent in an isolated git worktree, with a tight brief: the locked design, exact file pointers, the failing test matrix to make green, the constraints (catalog discipline, no transient refs), and the verification steps. Fresh context is the lever — not a "better coder" (the model is already top-tier). A delegated agent that hits an unspecified fork stops and reports it (good) rather than debugging mid-stream; worktree isolation lets parallel slices proceed without colliding.

### Before counting a slice done

5. **Adversarial review of the diff in a fresh context — before commit.** Run an adversarial reviewer (e.g. `precept-reviewer`) that sees only the diff + criteria, not the reasoning that produced it — Anthropic's documented "fresh subagent reviews the diff" pattern. **Required for soundness-critical slices** (proof engine, type system, catalog). Tell the reviewer to flag **correctness/requirement gaps, not style** — Opus 4.8 follows "be conservative / don't nitpick" *more* faithfully than prior models, and a refute-prompted reviewer always finds *something*; chasing every NIT leads to over-engineering. Fix the real findings, then commit.

### Opus-4.8 levers
- **`effort` parameter** — tune up (`extra` / `max`) for hard or soundness-critical slices; it scopes work strictly, so the default can under-scope an open-ended task.
- **Dynamic Workflows / the `Workflow` tool** — for broad fan-outs (many-file migrations / audits), Claude writes its own orchestration over many parallel subagents; **test on a representative ~10% before the full run.**
- The model **pushes back on unsound plans and catches its own flaws** — surface gaps and design errors openly rather than papering over them; that's the model working as intended.

## Required workflow

### 1. Stand up the execution hub immediately

**PR mode:** open the draft PR immediately — it is the execution hub. **Spike-branch mode:** skip the PR; the plan doc (`docs/Working/<topic>-plan-YYYY-MM-DD.md`) is the execution hub, and its slice list / phase rows play the role the PR body plays below. Either way, the hub exists before the first slice lands.

PR body structure per `CONTRIBUTING.md` (PR mode; in spike mode the same Summary / Why / slice-checklist content lives in the plan doc, minus `Closes #N`):

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

- A separate `implementation-plan.md` file. The hub artifact (PR body in PR mode; the `/plan` plan doc in spike mode) is the plan — never duplicate it into a throwaway file.
- An empty Implementation Plan post-design-review-clear. The plan is execution discipline; the section being empty signals execution has no scaffolding.
- A PR opened before the design is locked. Open the draft PR after Stage 2 completes, not before. (Spike mode opens no PR at all — don't open one on a `spike/*` branch without owner confirmation.)

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

- Check off the slice in the hub artifact — the PR-body Implementation Plan (PR mode) or the plan doc's slice checklist (spike mode)
- Update the plan doc's phase tracker if the plan tracks slice-level progress
- **Pause for review at the slice boundary** before starting the next slice — don't auto-advance (owner preference; applies in both modes)
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

1. **The hub artifact IS the implementation plan.** PR body in PR mode; the `/plan` plan doc in spike-branch mode. Either way, no separate implementation-plan.md file. Refused.
2. **No PR on a spike branch.** On a `spike/*` branch, commits land directly on the branch and the plan doc is the hub — opening a draft PR (or adding `Closes #N`) without owner confirmation is refused.
3. **Pending design review until the gate clears.** Implementation Plan section says "Pending design review" until Track A or Track B (per CONTRIBUTING.md § 3) signs off. Plans written before the gate are refused.
4. **Slices are coherent and incremental.** A slice that leaves tests red, mixes unrelated features, or skips doc-touch is refused.
5. **Catalog-first.** For language-surface or pipeline-touching slices, the catalog entry lands in the slice that introduces the feature — not in a follow-up. Pipeline code derives from catalog; if pipeline code hardcodes catalog knowledge in the slice, refused.
6. **Doc-sync in the same commit.** Affected docs (per the CLAUDE.md routing table) are updated in the slice that changes the behavior they describe. Cross-commit doc-sync (slice N changes code, slice N+1 updates docs) is refused — the description-of-current-reality drifts in slice N otherwise.
7. **Sample-edit constraint.** Sample files in `samples/` are not modified by this workstream unless the plan explicitly authorizes the edit. Stray sample changes are refused.
8. **No skipping vertical slices to amend.** When a pre-commit hook fails, fix the underlying issue and create a NEW commit; don't `--amend`. The hook failure means the commit didn't happen — `--amend` would modify the *previous* commit, potentially destroying earlier work.
9. **Enumerate + test-first before code.** No implementation begins until the slice's input/obligation space is enumerated (with real behavior probed) and the failing test matrix exists. Coding from the acceptance examples alone is refused — it is the documented cause of mid-build regression piles.
10. **Adversarial review before committing a soundness-critical slice.** Proof-engine / type-system / catalog slices get a fresh-context adversarial review of the diff (flagging correctness/requirement gaps, not style) before the commit lands.

## Composability

- **Input**: a locked design doc (`/design` output) + a phased plan (`/plan` output) + (PR mode) a GitHub issue.
- **Output**: vertical-slice commits, updated docs, green tests, and a checkable plan — landed as a merged PR (PR mode) or directly on the `spike/*` branch with the plan doc as the live tracker (spike mode). Feeds `/promote` for canonicalizing the design's content into reference docs.

## Anti-patterns to refuse

- Open a separate `implementation-plan.md` file alongside the hub (PR body / plan doc)
- Open a draft PR on a `spike/*` branch (spike mode commits directly; the plan doc is the hub)
- Begin coding before the design is locked
- Skip the Implementation Plan checklist in the hub artifact ("I'll fill it in as I go")
- Commit a slice that leaves tests red
- Land code changes in slice N and doc updates in slice N+1 ("docs are coming")
- Hand-edit `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (it's generated)
- Skip pre-commit hooks with `--no-verify`
- Modify samples without explicit plan authorization
- Amend a commit when the pre-commit hook failed (create a new commit instead)
- Start coding a slice from the acceptance examples without enumerating the full input space and writing the failing test matrix first
- Commit a soundness-critical slice (proof engine / type system / catalog) without a fresh-context adversarial review of the diff

## Quick reference

| Symptom | Skill response |
|---|---|
| Separate `implementation-plan.md` proposed | Refuse; the hub artifact (PR body / plan doc) is the plan |
| Draft PR proposed on a `spike/*` branch | Refuse without owner confirm; commit directly, plan doc is the hub |
| Code change without doc-touch in same slice | Refuse; routing-table doc updates land in the same commit |
| Pipeline code hardcoding token sets / per-member kind dispatch | Refuse; catalog-driven discipline applies in the slice that adds the feature |
| Sample edit not in plan | Refuse; sample-edit constraint applies |
| `--no-verify` to skip hooks | Refuse; fix the underlying issue |
| `--amend` after pre-commit hook failure | Refuse; create a new commit |
| Slice mixing unrelated features | Refuse; split into separate slices |
| PR body Implementation Plan empty after design gate cleared | Prompt to fill before any code lands |
| Coding from acceptance examples; no enumerated input space / failing test matrix | Refuse; enumerate + write failing tests first (guard 8) |
| Soundness-critical slice committed with no adversarial diff review | Refuse; fresh-context review (correctness, not style) before commit (guard 9) |
