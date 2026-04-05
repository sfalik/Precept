---
name: "unified-issue-workflow"
description: "How to run one GitHub issue lifecycle across proposals, bugs, features, chores, UX, docs, and research without proposal-specific status labels"
domain: "project-management, issue-triage, roadmap"
confidence: "high"
source: "earned (2026-04-05 issue workflow standardization for Precept)"
tools:
  - name: "github"
    description: "Inspect issue labels, state, and routing patterns before recommending workflow changes"
    when: "When standardizing repo issue operations or migrating label/state models"
---

## Context

Use this when a repo has drifted into issue-type-specific workflows and wants one operating model that works for proposals, bugs, features, chores, UX work, docs, and research.

The core move is to separate three concerns:

1. **workflow state** — where the work is in the lifecycle
2. **taxonomy** — what kind of work it is and what domain it belongs to
3. **exceptions** — why an otherwise normal issue is paused or parked

## Patterns

### 1. One shared lifecycle

Standardize on:

`Backlog -> Ready -> In Progress -> In Review -> Done`

That lifecycle is generic enough to fit every issue type.

### 2. Labels are for taxonomy, not routine status

Keep labels for durable categories:

- exactly one issue type label
- one primary domain label
- one owner/routing label
- optional long-lived slice/theme labels

Do not use labels for ordinary workflow steps once the board is the primary status surface.

### 3. Keep only exception labels with unique semantics

Two labels usually earn their keep:

- `blocked` — work is active in principle but cannot advance
- `deferred` — work is intentionally parked and should be searchable later

These are exceptions, not normal stages.

### 4. Define "Ready" tightly

An issue is `Ready` only when it has:

- a clear outcome or decision question
- enough context to act
- an owner
- a priority call
- no unresolved blocker

### 5. Enforce one-pass triage

Before an issue leaves `Backlog`, assign:

- type
- domain
- owner
- priority
- next action
- workflow state

## Examples

### Example label stack

- `feature`
- `runtime`
- `squad:george`
- optional slice label like `release:v0.10`

### Example proposal under the shared workflow

- type: `proposal`
- domain: `language`
- owner: `squad:frank`
- status: `Ready` while awaiting active review
- status: `In Review` once PM/architecture are discussing it
- `Done` when the decision comment is recorded and the issue is closed

### Example deferred rule

- apply `deferred`
- close the issue
- add a comment with the cut reason and return trigger
- reopen the same issue if it comes back into scope

## Anti-Patterns

- Applying multiple issue type labels to one issue
- Reintroducing `ready`/`review`/proposal-state labels after moving workflow to the board
- Using `blocked` or `deferred` without a reason and resume trigger
- Letting issues sit in `In Progress` when nobody is actively working them
