# SKILL: Unrelated-History Trunk Return

**Domain:** Release management / Git consolidation
**Author:** Steinbrenner
**Applicability:** A feature branch must return to trunk, but trunk and feature have no merge base

---

## Pattern

When the active branch and trunk have unrelated histories, do **not** force the old history onto trunk with a merge or heroic rebase. Treat the active branch as a **source tree**, create a fresh integration branch from trunk, and re-land the desired state as curated commits.

---

## How to Apply

1. Freeze the source branch.
2. Stash or otherwise quarantine any uncommitted local work before consolidation starts.
3. Determine whether side branches/worktrees are independent or already ancestors of the source branch.
4. If they are ancestors, keep them as references only; do not merge them separately.
5. Create a new integration branch from trunk.
6. Transplant work in buckets:
   - implementation and tests
   - essential docs
   - README/brand/process material
7. Validate after the implementation bucket lands, before bringing over strategy-facing docs.
8. Merge the curated integration branch to trunk through review.
9. Only then retire old branches and worktrees.

---

## Why This Works

- It keeps trunk history reviewable.
- It prevents exploratory commits and process exhaust from polluting the authoritative line.
- It forces every uncommitted edit to be handled intentionally.
- It avoids creating a one-time merge artifact that future maintainers have to explain forever.

---

## Anti-Patterns

- Merging unrelated histories directly into trunk
- Rebasing a long exploratory branch onto a tiny trunk just to preserve chronology
- Letting dirty working-tree docs hitchhike into the consolidation branch
- Merging ancestor side branches separately even though the source branch already contains them
