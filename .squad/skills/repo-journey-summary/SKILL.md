# Skill: Repo Journey Summary for Diverged Product Branches

**Domain:** Documentation, repository archaeology, release preparation
**Author:** J. Peterman
**Applicability:** When a repository has a long exploratory branch history and someone needs a factual "how we got here" document

---

## Pattern

When the real product line has evolved through exploratory branches, proposals, and research artifacts, a useful journey summary does **not** retell everything. It separates the record into four layers:

1. **Chronology** — the major phases in order
2. **Survivors** — what clearly persisted into the current strategy
3. **Unresolveds** — what must still be decided before consolidation or release
4. **Recommendation** — the smallest honest next-step frame

This is especially important when the current branch has **no merge base** with trunk. In that case, the summary must say plainly that consolidation is a curation problem, not standard merge hygiene.

---

## How to Apply

### 1. Build the timeline from milestones, not every commit

Use commit history to identify phase changes:

- original prototype / early experiments
- decisive redesign or rewrite
- tooling hardening
- brand / docs / distribution work

Name the pivot points. Do not drown the reader in implementation noise.

### 2. Cross-check "current" against live surfaces

For the "what survived" section, trust current source-of-truth files over exploratory notes:

- current README
- current design docs
- current brand decisions
- active branch topology

If a decision log says something is settled but the public surface still calls it temporary, report the public surface honestly.

### 3. Treat unresolved items as pre-consolidation gates

Look for unresolveds in three places:

- **branch topology** — missing merge base, sibling worktrees, orphan lines
- **working tree state** — dirty docs, pending edits, unharvested work
- **surface status** — temporary README language, draft UX surfaces, pending sign-offs

These are not footnotes. They determine whether trunking is real or ceremonial.

### 4. Write the recommendation in operational terms

End with the smallest useful recommendation, usually:

- curate, do not blindly merge;
- preserve the implemented center;
- archive exploration deliberately;
- keep public copy honest about anything still temporary.

---

## Example

**File:** `docs/HowWeGotHere.md`

Precept's current product line lives on `feature/language-redesign`, while `main` remains a concept-readme line with no merge base. The resulting summary framed trunk consolidation as a curation decision, then separated the durable center (DSL redesign, tooling, AI surface, source-of-truth docs) from the still-open items (branch strategy, dirty worktree edits, temporary hero status, draft surfaces).
