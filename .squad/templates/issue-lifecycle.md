# Issue Lifecycle — Repo Connection & PR Flow

Reference for connecting Squad to a repository and managing the issue→branch→PR→merge lifecycle.

## Repo Connection Format

When connecting Squad to an issue tracker, store the connection in `.squad/team.md`:

```markdown
## Issue Source

**Repository:** {owner}/{repo}  
**Connected:** {date}  
**Platform:** {GitHub | Azure DevOps | Planner}  
**Filters:**
- Labels: `{label-filter}`
- Project: `{project-name}` (ADO/Planner only)
- Plan: `{plan-id}` (Planner only)
```

**Detection triggers:**
- User says "connect to {repo}"
- User says "monitor {repo} for issues"
- Ralph is activated without an issue source

## Platform-Specific Issue States

Each platform tracks issue lifecycle differently. Squad normalizes these into a common board state.

### GitHub

| GitHub State | GitHub API Fields | Squad Board State |
|--------------|-------------------|-------------------|
| Open, not yet triaged | `state: open`, `squad` label only, no `squad:{member}` label | `Backlog` |
| Open, owner assigned, no branch yet | `state: open`, `squad:{member}` present, no linked PR | `Ready` |
| Open, branch exists or active implementation underway | `state: open`, linked branch exists | `In Progress` |
| Open, PR exists or active review/decision is underway | `state: open`, PR exists | `In Review` |
| Closed | `state: closed` | `Done` |

**Issue labels used by Squad:**
- `squad` — Issue is in Squad backlog
- `squad:{member}` — Assigned to specific agent
- `squad:chore` — Autonomous chore pickup gate; Ralph routes to best-fit member, applies `squad:{member}` label, and preserves review ceremony
- `priority:p{N}` — Priority level (0=critical, 1=high, 2=medium, 3=low)
- `blocked` — Work cannot advance until an external blocker clears
- `deferred` — Work is intentionally parked outside the current active plan

**Branch naming convention:**
```
squad/{issue-number}-{kebab-case-slug}
```
Example: `squad/42-fix-login-validation`

### Azure DevOps

| ADO State | Squad Board State |
|-----------|-------------------|
| New | `Backlog` |
| Active, no branch | `Ready` |
| Active, branch exists | `In Progress` |
| Active, PR opened | `In Review` |
| Resolved | `Done` |
| Closed | `Done` |

**Work item tags used by Squad:**
- `squad` — Work item is in Squad backlog
- `squad:{member}` — Assigned to specific agent

**Branch naming convention:**
```
squad/{work-item-id}-{kebab-case-slug}
```
Example: `squad/1234-add-auth-module`

### Microsoft Planner

Planner does not have native Git integration. Squad uses Planner for task tracking and GitHub/ADO for code management.

| Planner Status | Squad Board State |
|----------------|-------------------|
| Not Started | `Backlog` |
| Ready to start | `Ready` |
| In Progress, no PR | `In Progress` |
| In Progress, PR opened | `In Review` |
| Completed | `Done` |

**Planner→Git workflow:**
1. Task created in Planner bucket
2. Agent reads task from Planner
3. Agent creates branch in GitHub/ADO repo
4. Agent opens PR referencing Planner task ID in description
5. Agent marks task as "Completed" when PR merges

## Issue → Branch → PR → Merge Lifecycle

### 1. Issue Assignment (Triage)

**Trigger:** Ralph detects a backlog issue that still needs triage, or the user manually assigns work.

**Actions:**
1. Read `.squad/routing.md` to determine which agent should handle the issue
2. Apply `squad:{member}` (GitHub) or the equivalent ownership tag (ADO)
3. Transition the board status to `Backlog` or `Ready`, depending on whether the issue is actionable immediately
4. Optionally spawn agent immediately if issue is high-priority

**Issue read command:**
```bash
# GitHub
gh issue view {number} --json number,title,body,labels,assignees

# Azure DevOps
az boards work-item show --id {id} --output json
```

### 2. Branch Creation (Start Work)

**Trigger:** Agent accepts issue assignment and begins work.

**Actions:**
1. Ensure working on latest base branch (usually `main` or `dev`)
2. Create feature branch using Squad naming convention
3. Transition issue to `In Progress`

**Branch creation commands:**

**Standard (single-agent, no parallelism):**
```bash
git checkout main && git pull && git checkout -b squad/{issue-number}-{slug}
```

**Worktree (parallel multi-agent):**
```bash
git worktree add ../worktrees/{issue-number} -b squad/{issue-number}-{slug}
cd ../worktrees/{issue-number}
```

> **Note:** Worktree support is in progress (#525). Current implementation uses standard checkout.

### 3. Implementation & Commit

**Actions:**
1. Agent makes code changes
2. Updates the draft PR body after each completed slice or logical group so the summary, why, and implementation checklist stay current
3. Commits reference the issue number
4. Pushes branch to remote

**Slice loop:**

For active implementation work, repeat this loop throughout the PR instead of waiting until the end:

1. Finish one vertical slice or logical group
2. Update `## Summary` / `## Why` if the shipped scope or reviewer context changed, then check off the corresponding implementation-plan items in the draft PR body
3. Commit the completed slice
4. Push the branch

**Commit message format:**
```
{type}({scope}): {description} (#{issue-number})

{detailed explanation if needed}

{breaking change notice if applicable}

Closes #{issue-number}

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

**Commit types:** `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`, `style`, `build`, `ci`

**Push command:**
```bash
git push -u origin squad/{issue-number}-{slug}
```

### 4. PR Creation

**Trigger:** Agent completes implementation and is ready for review.

**Actions:**
1. Open PR from feature branch to base branch
2. Reference the issue in the `## Linked Issue` section with `Closes #{issue-number}`
3. Seed the PR body with `## Summary`, `## Linked Issue`, `## Why`, and `## Implementation Plan` if they are not already present
4. Apply labels if needed
5. Transition issue to `In Review`

**PR creation commands:**

**GitHub:**
```bash
gh pr create --title "{title}" \
  --body "Closes #{issue-number}\n\n{description}" \
  --head squad/{issue-number}-{slug} \
  --base main
```

**Azure DevOps:**
```bash
az repos pr create --title "{title}" \
  --description "Closes #{work-item-id}\n\n{description}" \
  --source-branch squad/{work-item-id}-{slug} \
  --target-branch main
```

**PR description template:**
```markdown
## Summary
- {what changed}

## Linked Issue
- Closes #{issue-number}

## Why
- {why this change exists / what problem it addresses / reviewer context}

## Implementation Plan
- [ ] Slice 1 — ...
- [ ] Slice 2 — ...

{If working as a squad member:}
Working as {member} ({role})

{If needs human review:}
⚠️ This task was flagged as "needs review" — please have a squad member review before merging.
```

### 5. PR Review & Updates

**Review states:**
- **Approved** → stays `In Review` until merged
- **Changes requested** → stays `In Review` while revisions are in flight
- **CI failure** → stays `In Review`; use `blocked` only when a non-routine blocker needs explicit tracking

**When changes are requested:**
1. Agent addresses feedback
2. Commits fixes to the same branch
3. Pushes updates
4. Requests re-review

**Update workflow:**
```bash
# Make changes
git add .
git commit -m "fix: address review feedback"
git push
```

**Re-request review (GitHub):**
```bash
gh pr ready {pr-number}
```

### 6. PR Merge

**Trigger:** PR is approved and CI passes.

**Merge strategies:**

**GitHub (merge commit):**
```bash
gh pr merge {pr-number} --merge --delete-branch
```

**GitHub (squash):**
```bash
gh pr merge {pr-number} --squash --delete-branch
```

**Azure DevOps:**
```bash
az repos pr update --id {pr-id} --status completed --delete-source-branch true
```

**Post-merge actions:**
1. Issue automatically closes (if "Closes #{number}" is in PR description)
2. Feature branch is deleted
3. Squad board state transitions to `Done`
4. Worktree cleanup (if worktree was used — #525)

### 7. Cleanup

**Standard workflow cleanup:**
```bash
git checkout main
git pull
git branch -d squad/{issue-number}-{slug}
```

**Worktree cleanup (future, #525):**
```bash
cd {original-cwd}
git worktree remove ../worktrees/{issue-number}
```

## Spawn Prompt Additions for Issue Work

When spawning an agent to work on an issue, include this context block:

```markdown
## ISSUE CONTEXT

**Issue:** #{number} — {title}  
**Platform:** {GitHub | Azure DevOps | Planner}  
**Repository:** {owner}/{repo}  
**Assigned to:** {member}

**Description:**
{issue body}

**Labels/Tags:**
{labels}

**Acceptance Criteria:**
{criteria if present in issue}

**Branch:** `squad/{issue-number}-{slug}`

**Your task:**
{specific directive to the agent}

**After completing work:**
1. Commit with message referencing issue number
2. Push branch
3. Open PR using:
   ```
   gh pr create --title "{title}" --body-file {tempfile-with-summary-linked-issue-why-and-plan} --head squad/{issue-number}-{slug} --base {base-branch}
   ```
4. Report PR URL to coordinator
```

## Ralph's Role in Issue Lifecycle

Ralph (the work monitor) continuously checks issue and PR state:

1. **Triage:** Detects backlog issues that still need triage, assigns `squad:{member}` labels
2. **Spawn:** Launches agents for `Ready` issues
3. **Monitor:** Tracks board flow (`Backlog -> Ready -> In Progress -> In Review -> Done`)
4. **Merge:** Automatically merges approved PRs
5. **Cleanup:** Marks issues as `Done` when PRs merge

**Ralph's work-check cycle:**
```
Scan → Categorize → Dispatch → Watch → Report → Loop
```

See `.squad/templates/ralph-reference.md` for Ralph's full lifecycle.

## PR Review Handling

### Automated Approval (CI-only projects)

If the project has no human reviewers configured:
1. PR opens
2. CI runs
3. If CI passes, Ralph auto-merges
4. Issue closes

### Human Review Required

If the project requires human approval:
1. PR opens
2. Human reviewer is notified (GitHub/ADO notifications)
3. Reviewer approves or requests changes
4. If approved + CI passes, Ralph merges
5. If changes requested, agent addresses feedback

### Squad Member Review

If the issue was assigned to a squad member and they authored the PR:
1. Another squad member reviews (conflict of interest avoidance)
2. Original author is locked out from re-working rejected code (rejection lockout)
3. Reviewer can approve edits or reject outright

## Common Issue Lifecycle Patterns

### Pattern 1: Quick Fix (Single Agent, No Review)
```
Issue created → Backlog triage → Ready → Branch created → Code fixed →
PR opened → CI passes → Auto-merged → Issue closed
```

### Pattern 2: Feature Development (Human Review)
```
Issue created → Backlog triage → Ready → Branch created → Feature implemented →
PR opened → Human reviews → Changes requested → Agent fixes → 
Re-reviewed → Approved → Merged → Issue closed
```

### Pattern 3: Research-Then-Implement
```
Issue created → Backlog triage → Research tracked in Backlog/In Review as needed →
Research documented → Research PR merged → Implementation issue created →
Implementation agent spawned → Feature built → PR merged
```

### Pattern 4: Parallel Multi-Agent (Future, #525)
```
Epic issue created → Decomposed into sub-issues → Each sub-issue assigned → 
Multiple agents work in parallel worktrees → PRs opened concurrently → 
All PRs reviewed → All PRs merged → Epic closed
```

## Anti-Patterns

- ❌ Creating branches without linking to an issue
- ❌ Committing without issue reference in message
- ❌ Opening PRs without "Closes #{number}" in description
- ❌ Merging PRs before CI passes
- ❌ Leaving feature branches undeleted after merge
- ❌ Using `checkout -b` when parallel agents are active (causes working directory conflicts)
- ❌ Manually transitioning issue states — let the platform and Squad automation handle it
- ❌ Skipping the branch naming convention — breaks Ralph's tracking logic

## GitHub Projects V2 — Status Sync

Scribe owns all project-board status transitions. The coordinator tells Scribe WHAT to transition; Scribe executes the update mechanically.

### Board Reference (Precept Language Improvements — Project #2)

| Field | Value |
|-------|-------|
| Project number | `2` |
| Project ID | `PVT_kwHOAQyRK84BTxO4` |
| Owner | `sfalik` |
| Status field ID | `PVTSSF_lAHOAQyRK84BTxO4zhA94Fw` |

### Status Options

| Status | Option ID | Trigger |
|--------|-----------|---------|
| Backlog | `86bc9793` | Issue triaged and added to board |
| In Review | `6c1e3216` | Design review ceremony starts |
| Ready | `732ec1dd` | Design approved / all reviewers approve + Shane signs off |
| In Progress | `f5aee879` | Implementation branch created or draft PR opened |
| Done | `f79a3ae0` | PR merged and issue closed |

### Update Commands

**Look up item ID for an issue:**
```bash
gh project item-list 2 --owner sfalik --format json --limit 50
# Match content.number to find the PVTI_... item ID
```

**Set status:**
```bash
gh project item-edit --project-id PVT_kwHOAQyRK84BTxO4 \
  --id <item-id> \
  --field-id PVTSSF_lAHOAQyRK84BTxO4zhA94Fw \
  --single-select-option-id <status-option-id>
```

### Coordinator-to-Scribe Handoff

Include in Scribe's spawn manifest when a status transition is needed:

```
STATUS_TRANSITION:
  issueNumber: {N}
  targetStatus: "In Review" | "Ready" | "In Progress" | "Done"
```

Scribe looks up the item ID, maps the target status to the option ID, and executes `gh project item-edit`. See Scribe charter § Project Status Sync Responsibilities.

### PR Linking

Every implementation PR MUST contain `Closes #N` in the body. This:
- Creates a GitHub-tracked link (populates "Linked pull requests" on the board)
- Auto-closes the issue when the PR merges
- If missing, Scribe adds it during PR stewardship

## Migration Notes

**v0.8.x → v0.9.x (Worktree Support):**
- `checkout -b` → `git worktree add` for parallel agents
- Worktree cleanup added to post-merge flow
- `TEAM_ROOT` passing to agents to support worktree-aware state resolution

This template will be updated as worktree lifecycle support lands in #525.
