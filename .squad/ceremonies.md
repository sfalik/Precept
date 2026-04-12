# Ceremonies

> Team meetings that happen before or after work. Each squad configures their own.

## Design Review

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | before |
| **Condition** | multi-agent task involving 2+ agents modifying shared systems, OR any language-surface change (new type, keyword, operator, constraint) |
| **Facilitator** | lead |
| **Participants** | all-relevant (implementing devs MUST attend for language-surface changes) |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Output:** Follow `.squad/skills/proposal-review/SKILL.md` for structured output format and posting workflow. Every reviewer produces structured JSON; the coordinator posts each review as a GitHub issue comment on the proposal issue. Reviews are durable on the issue, not buried in chat.

**Agenda:**
1. Review the task and requirements
2. **Impact analysis** — walk through Runtime, Tooling, and MCP impact categories (see `language-surface-sync.instructions.md`). Implementing devs flag gaps.
3. Agree on interfaces and contracts between components
4. Identify risks and edge cases
5. Assign action items
6. **Post reviews** — coordinator posts each reviewer's structured comment to the GitHub issue

---

## PR Review

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | before |
| **Condition** | squad PR marked ready for review (draft → ready, or new non-draft PR from a squad branch) |
| **Facilitator** | lead |
| **Participants** | Lead (architecture, code quality, doc sync), Tester (AC-to-test coverage, edge cases) |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Output:** Follow `.squad/skills/pr-review/SKILL.md` for structured review format and posting workflow. Every reviewer produces structured JSON; Scribe posts each review via `squad-reviewer[bot]`. Reviews are durable on the PR, not buried in chat.

**Agenda:**
1. Read the linked issue's acceptance criteria
2. **Code review** — Lead reviews architecture, doc accuracy, grammar/completions sync, diagnostic correctness, dead code
3. **Test review** — Tester verifies AC-to-test matrix (every behavioral criterion has a test), spot-checks test quality, scans for disabled tests
4. **Synthesis** — coordinator presents summary to user: verdicts, agreements, disagreements, consolidated action items
5. **Post reviews** — Scribe posts each reviewer's structured review to the PR via squad-review.js
6. **Status sync** — if reviews approve and PR merges, Scribe transitions the linked issue to `Done` on the project board

---

## Retrospective

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | after |
| **Condition** | build failure, test failure, or reviewer rejection |
| **Facilitator** | lead |
| **Participants** | all-involved |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**
1. What happened? (facts only)
2. Root cause analysis
3. What should change?
4. Action items for next iteration
