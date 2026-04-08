---
name: "merged-pr-verification"
description: "Verify whether a requested PR action is already complete before recreating or mutating branch state."
domain: "github-workflow"
confidence: "high"
source: "earned"
tools:
  - name: "gh"
    description: "Inspects GitHub pull request and branch state."
    when: "When a user asks to raise, complete, or merge a PR on a retained or long-lived branch."
---

## Context
Use this when a branch may already have an associated PR or merge history. Retained branches are the trap: the branch can still exist while the PR is already merged and `origin/main` already contains the merge commit.

## Patterns
1. Fetch before concluding anything.
2. Use `gh pr status` to discover the branch PR, then `gh pr view <number>` to confirm title, state, merge commit, merged timestamp, and base/head refs.
3. Treat `origin/main` as the authoritative merge-state reference, not local `main`.
4. If the PR is already merged, report the actual merge result and preserve the branch unless the user explicitly orders deletion.

## Examples
- `chore/upgrade-squad-latest` remained open by directive, but `gh pr view 36` showed the branch had already been merged into `main` with merge commit `49cab38`.
- The right action was to verify the merge and summarize branch state, not to manufacture a second PR flow.

## Anti-Patterns
- Assuming a surviving branch means the PR is still open.
- Recreating a PR for a diff that GitHub already merged.
- Using stale local `main` as proof that merge work remains.
- Deleting the source branch when branch retention is part of the standing contract.
