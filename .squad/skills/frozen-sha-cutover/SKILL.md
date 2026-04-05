---
name: "frozen-sha-cutover"
description: "Before trunk decisions on unrelated or fast-moving branches, freeze the exact candidate SHA and review that immutable surface instead of the branch name"
domain: "git workflow, consolidation, release management, code review"
confidence: "high"
source: "earned - return-to-main safety review 2026-04-05"
---

# Skill: Frozen SHA Before Cutover

## The Problem

When a branch has no merge base with `main`, or when the repo is being used in a shared non-isolated worktree, a review against the branch name is unstable. The branch can move while people are still discussing strategy. That makes any approval ambiguous.

## The Pattern

**Freeze the exact candidate commit before anyone approves trunk work.**

Do not approve:
- "`main` should take `feature/x`"
- "just force-push the branch"
- "looks good to merge"

Approve only:
- "`main` may take commit `<sha>` using strategy `<named cutover pattern>`"

## When to Apply

- No merge base between candidate branch and `main`
- The delta is effectively a repo import/replacement
- The branch is ahead of origin or otherwise locally mutable
- The environment is shared and another actor can move the branch during review

## Procedure

1. Record the exact candidate SHA.
2. Verify the branch does not move during review. If it moves, restart from the new SHA.
3. Separate the landing into artifact classes:
   - product code/tests
   - public contracts/docs
   - automation/process/config
   - collateral/brand assets
4. Choose the cutover strategy against the frozen SHA, not the branch name.
5. Preserve the original branch for archaeology if needed, but curate trunk history separately.

## Why This Works

- Prevents approvals from becoming stale mid-review
- Forces reviewers to name the real landing unit
- Exposes when a "merge" is actually a repository cutover
- Keeps trunk decisions auditable
