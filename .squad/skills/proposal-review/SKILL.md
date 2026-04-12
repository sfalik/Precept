---
name: "proposal-review"
description: "How to produce and post structured proposal reviews on GitHub issues. Covers review criteria, output format, issue comment format, and integration with the design gate."
domain: "proposal review, design review, language design"
confidence: "high"
source: "earned — PR #68 review process formalization 2026-04-12; mirrors pr-review skill for the issue surface"
---

# Skill: Proposal Review — Structured Reviews on GitHub Issues

## The Problem

Proposal reviews happen in chat, get buried in Squad logs, or land as unstructured comments that don't clearly distinguish blockers from suggestions. The proposer can't tell what must change before the proposal advances, and the principal (Shane) can't see the review status without asking the coordinator.

## The Pattern

**Every proposal review is posted as a structured GitHub issue comment** via the GitHub API. Reviews follow a consistent format with clear verdicts, numbered findings, and explicit gate criteria. This mirrors the pr-review skill but targets the issue surface (no diff, no inline file comments).

## When This Applies

- Any GitHub issue that is a **proposal** (language feature, architecture change, tooling proposal, process change)
- Triggered when: "review proposal #N", "evaluate this proposal", "Frank, review the design", design review ceremony on an issue
- Does NOT apply to bug reports, task issues, or implementation PRs (use `pr-review` for PRs)

## Review Criteria

### Language Proposals (Frank — Lead/Architect)

1. **Philosophy filter** — Apply the 7 questions from `.squad/skills/dsl-philosophy-filter/SKILL.md`:
   - Domain integrity, determinism, keyword clarity, truth boundaries, locality, compile-time soundness, alias creep/AI legibility
   - Each question gets a PASS/FAIL/NA verdict

2. **Impact assessment** — All three categories MUST be covered:
   - **Runtime** — parser, type checker, evaluator, engine, diagnostics
   - **Tooling** — syntax highlighting (all positions), completions (all contexts), hover, semantic tokens
   - **MCP** — type vocabulary in `precept_language`, DTOs in `precept_compile`, serialization in fire/inspect/update
   - A proposal missing any impact category is incomplete — send it back

3. **Rationale completeness** — Every locked decision must have:
   - Why this choice (not just what)
   - Alternatives considered and rejected (with reasons)
   - Precedent from research base
   - Tradeoff accepted (known downside)

4. **Research grounding** — Does the proposal reference `research/language/` evidence? Are claims supported by the comparative studies?

5. **Acceptance criteria quality** — Are ACs behavioral (testable), specific, and complete? Do they cover error paths and edge cases?

### Test Feasibility Review (Soup Nazi — Tester)

1. **Edge case enumeration** — How many edge cases does this construct introduce?
2. **Test surface estimate** — Rough count of new test cases (parser, type checker, evaluator, runtime each)
3. **Regression risk** — Does this interact with existing constructs in ways that could break tests?
4. **Verdict** — clean / manageable / high-risk

### General Proposals (any reviewer)

1. **Scope clarity** — Is the proposal scoped tightly enough to implement?
2. **Dependencies** — Does it depend on unfinished work?
3. **Documentation impact** — Which docs need updating?

## Output Format

Reviewers MUST produce a **structured JSON object** — not plain Markdown. The coordinator writes it to a temp file and posts it as an issue comment.

```json
{
  "issueNumber": 16,
  "verdict": "APPROVED" | "BLOCKED" | "NEEDS_REVISION",
  "body": "## {emoji} {AgentName} — Proposal Review\n\n**{verdict}** — {one-line summary}\n\n### Philosophy Filter\n...\n### Impact Assessment\n...\n### Blockers\n...\n### Warnings\n...\n### What's strong\n..."
}
```

### Field Requirements

| Field | Required | Description |
|-------|----------|-------------|
| `issueNumber` | Yes | The GitHub issue number being reviewed |
| `verdict` | Yes | `APPROVED` — ready to implement; `BLOCKED` — must fix before advancing; `NEEDS_REVISION` — close but needs specific changes |
| `body` | Yes | Full review content (Markdown). Posted as the issue comment body |

### Verdict Definitions

| Verdict | Meaning | What happens next |
|---------|---------|-------------------|
| `APPROVED` | Proposal meets all gate criteria. Ready for Shane's sign-off, then implementation | Coordinator presents to Shane for approval |
| `BLOCKED` | Fundamental issues — missing impact category, philosophy violation, unsound design | Proposer must revise. Re-review required after changes |
| `NEEDS_REVISION` | Mostly sound but specific items need work — incomplete ACs, missing rationale, unclear scope | Proposer addresses items, re-review may be waived for minor fixes |

### Body Structure

```markdown
## {emoji} {AgentName} — Proposal Review

**{APPROVED|BLOCKED|NEEDS_REVISION}** — {one-line verdict}

### Philosophy Filter
| Question | Verdict | Notes |
|----------|---------|-------|
| Domain integrity | PASS | ... |
| Determinism | PASS | ... |
| ... | ... | ... |

### Impact Assessment
- **Runtime:** {covered/missing — details}
- **Tooling:** {covered/missing — details}
- **MCP:** {covered/missing — details}

### Blockers
**B1:** ...
**B2:** ...

### Warnings (non-blocking)
**W1:** ...

### What's strong
**G1:** ...
**G2:** ...

### Rationale Check
- Locked decisions with rationale: {count}
- Locked decisions missing rationale: {list}

### Test Feasibility (if Soup Nazi reviewed)
- Edge cases: {count}
- Estimated test cases: {count}
- Regression risk: clean / manageable / high-risk
```

## Project Status Lifecycle

Proposal reviews drive status transitions on the GitHub Projects V2 board. The coordinator signals Scribe to execute these transitions at the right moments.

| Moment | Status Transition | Coordinator Action |
|--------|-------------------|--------------------|
| Design review ceremony starts | → `In Review` | Include `STATUS_TRANSITION: In Review` in Scribe's spawn manifest alongside the review payloads |
| All reviewers approve + Shane signs off | → `Ready` | Include `STATUS_TRANSITION: Ready` in Scribe's post-review spawn manifest |

If the proposal is `BLOCKED` or `NEEDS_REVISION`, status stays at `In Review` until the re-review completes with approval.

The coordinator MUST include the issue number and the target status in Scribe's spawn manifest. Scribe executes the transition per its § Project Status Sync Responsibilities.

## How to Post

**Scribe posts all proposal reviews.** The coordinator quality-checks review content, then passes pre-validated payloads to Scribe in the spawn manifest. Scribe writes each to a temp file and posts via GitHub API. See Scribe charter § Review Posting Responsibilities.

### Coordinator's role:
1. Collect structured JSON from each reviewer agent.
2. Validate: correct format, no hallucinated findings, verdicts are justified.
3. Pass validated review bodies to Scribe in the spawn manifest.
4. **Present a synthesis summary to the user** (see § Coordinator Post-Review Summary below).

### Scribe's role:
1. Write each review body to `temp/{agent-name}-proposal-review.md`.
2. Post: `gh issue comment <issue-number> --body-file temp/<agent>-proposal-review.md` (or `mcp_github_add_issue_comment`).
3. Log posted URL in orchestration log.

Unlike PR reviews, issue comments don't have `APPROVE` / `REQUEST_CHANGES` events. The verdict is conveyed in the structured body. To make the verdict machine-scannable, the first line after the heading MUST be:

```
**APPROVED** — ...
**BLOCKED** — ...
**NEEDS_REVISION** — ...
```

## Coordinator Post-Review Summary

After all reviews are collected (and before or alongside Scribe posting), the coordinator presents a **synthesis summary** to the user. This is the user-facing deliverable — not the raw reviews.

### Required sections:

1. **Verdict table** — each reviewer's verdict in one glanceable table
2. **Where the team agrees** — themes, positions, or assessments that appeared across multiple reviews
3. **Where they differ** — any disagreements, divergent risk assessments, or items one reviewer flagged that others didn't
4. **Consolidated action items** — deduplicated list of blockers and amendments, with source attribution
5. **Bottom line** — one-sentence overall recommendation

### Example format:

```markdown
## Design Review Summary — Issue #N: {Title}

### Verdicts
| Reviewer | Verdict |
|---|---|
| Frank | APPROVED |
| George | feasible-with-caveats |

### Where the team agrees
- ...

### Where they differ
- ...

### Action items (deduplicated)
| # | Item | Raised by |
|---|---|---|
| 1 | ... | Soup Nazi, Steinbrenner |

### Bottom line
...
```

The summary is chat-only — it is NOT posted to the issue. The structured individual reviews on the issue are the durable record; the summary is for the user's immediate decision-making.

## Spawn Prompt Addition

When the coordinator spawns a proposal reviewer, include this in the prompt:

```
PROPOSAL REVIEW OUTPUT FORMAT: You MUST output a JSON object with this structure:
{
  "issueNumber": <number>,
  "verdict": "APPROVED" | "BLOCKED" | "NEEDS_REVISION",
  "body": "## {emoji} {YourName} — Proposal Review\n\n**{verdict}** — ..."
}

Apply the philosophy filter (7 questions from .squad/skills/dsl-philosophy-filter/SKILL.md).
Verify all 3 impact categories (Runtime, Tooling, MCP) are covered.
Check rationale completeness for every locked decision.
Read .squad/skills/proposal-review/SKILL.md for the full review format spec.
```

## Coordinator Post-Review Workflow

After collecting reviewer output:

1. Parse the JSON from the agent's response
2. Extract the `body` field
3. Write to `temp/{agent-name}-proposal-review.md`
4. Post: `gh issue comment {issueNumber} --body-file temp/{agent-name}-proposal-review.md`
5. Report verdict to the user
6. If `APPROVED` → present to Shane for sign-off
7. If `BLOCKED` or `NEEDS_REVISION` → route back to the proposer with specific items to address

## Relationship to Other Skills

| Skill | Relationship |
|-------|-------------|
| `pr-review` | Sibling — same structured approach, different surface (PRs vs. issues) |
| `dsl-philosophy-filter` | Input — the 7 philosophy questions are a required section of language proposal reviews |
| `proposal-gate-analysis` | Downstream — after a proposal is APPROVED and reviewed, the PM uses gate analysis to identify minimum sign-off decisions before execution |
| `constraint-holder-review-gate` | Complementary — the constraint-holder must also review the post-implementation artifact |
| `language-design` | Input — research location and design doc references used during review |

## History

- **2026-04-12:** Created alongside `pr-review` skill during PR #68 review process formalization. Addresses the gap where proposal reviews were informal chat responses rather than structured, trackable issue comments.
