---
name: "issue-workflow-normalization"
description: "How to standardize GitHub issue workflow by separating routing, taxonomy, board status, and closure semantics"
domain: "workflow, issue-triage, project-operations"
confidence: "high"
source: "earned (2026-05-15 standardized issue workflow recommendation)"
tools:
  - name: "view"
    description: "Read routing docs, lifecycle templates, and existing workflow guidance before proposing changes"
    when: "Always inspect current issue-process docs before recommending a new workflow model"
  - name: "github"
    description: "Review real issues/projects when validating how the workflow will map to GitHub"
    when: "Use when the recommendation needs to align with actual GitHub project states or issue inventory"
---

## Context

Use this when a repo's issue process has become muddled because labels are doing too many jobs at once. The common failure mode is predictable: labels start encoding ownership, issue type, workflow state, and decision outcomes simultaneously. That produces duplicate truth and eventually contradictory truth.

## Patterns

1. **Separate concerns brutally.**
   - Routing labels answer **who owns this**
   - Taxonomy labels answer **what kind of issue is this**
   - Project status answers **where is it in the workflow**
   - Open/closed answers **is it still live**

2. **Keep routing labels minimal.**
   - Team inbox label (`squad`)
   - Exactly one member-owner label (`squad:{member}`)
   - If a label does not change who picks up the work, it is not routing

3. **Make proposals just another issue type.**
   - `proposal` is taxonomy, not lifecycle
   - Waiting for sign-off belongs in board status (`In Review`), not in `needs-decision`
   - Approved/rejected/deferred outcomes belong in the closing comment and closure, not in special labels

4. **Use a compact board model.**
   - `Backlog`
   - `Ready`
   - `In Progress`
   - `In Review`
   - `Done`

   Keep `blocked` and `deferred` as exception labels, not board columns.

5. **Close for terminal meaning only.**
   - Implemented
   - Rejected
   - Deferred / not now
   - Duplicate
   - Superseded

## Examples

### Minimal label model

- Routing: `squad`, `squad:frank`
- Taxonomy: `proposal`, `language`
- Board status: `In Review`
- Issue state: `open`

That combination cleanly means: "Frank owns a language proposal that is awaiting review/decision."

### Proposal after decision

- Labels remain taxonomy only (`proposal`, `language`)
- Issue is **closed**
- Final comment records:
  - Decision
  - Rationale
  - Follow-on issue links, if any

No `decided` label required. The board and the issue state already did the work.

## Anti-Patterns

- ❌ Status labels that duplicate project status (`needs-decision`, `ready-to-merge`, `in-progress`)
- ❌ Proposal-only lifecycle tracks that diverge from bug/feature/doc issues
- ❌ Multiple active owner labels on one issue
- ❌ Using open proposal issues as long-lived implementation trackers after the decision is already made
- ❌ Closing issues without a final decision/resolution comment
