# Ceremonies

> Team meetings that happen before or after work. Each squad configures their own.

## Design Review

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | before |
| **Condition** | multi-agent task involving 2+ agents modifying shared systems, OR any language-surface change (new type, keyword, operator, constraint), OR any proposal (Track A or Track B — see `CONTRIBUTING.md` § 3. Design Review) |
| **Facilitator** | lead |
| **Participants** | all-relevant (implementing devs MUST attend for language-surface changes) |
| **Time budget** | focused |
| **Enabled** | ✅ yes |
| **Completion gate** | Owner (Shane) sign-off. No implementation plan is authored until this gate clears. |

**Track A (standard proposal — no new canonical design):**
- Design review targets the proposal issue — decisions, acceptance criteria, scope.
- Review comments are posted as structured issue comments (per `proposal-review` skill).

**Track B (design-introducing proposal — PR contains a new or substantially expanded canonical design doc):**
- Design review targets the proposal issue AND the design doc on the PR.
- Reviewers post structured issue comments on the proposal AND inline PR review comments on the markdown diff.
- All inline PR review comments must be resolved before the ceremony is considered complete.
- The proposer declares Track B in the issue body.

**Output:** Follow `.squad/skills/proposal-review/SKILL.md` for structured output format and posting workflow. Every reviewer produces structured JSON; the coordinator posts each review as a GitHub issue comment on the proposal issue. For Track B, reviewers also post inline PR comments on the design doc diff.

**Agenda:**
1. Review the task and requirements
2. **Impact analysis** — walk through Runtime, Tooling, and MCP impact categories (see `language-surface-sync.instructions.md`). Implementing devs flag gaps.
3. Agree on interfaces and contracts between components
4. Identify risks and edge cases
5. Assign action items
6. **Post reviews** — coordinator posts each reviewer's structured comment to the GitHub issue
7. **(Track B only)** Verify all inline PR review comments on the design doc are resolved

---

## External Research

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | before (Design Review) |
| **Condition** | Track B proposal, OR any proposal where design decisions lack external precedent evidence, OR user requests "research" |
| **Facilitator** | lead |
| **Participants** | 2+ agents with complementary research angles (e.g., architecture + implementation, brand + PM) |
| **Time budget** | focused |
| **Enabled** | ✅ yes |
| **Completion gate** | Research file(s) committed to `research/`, referenced from the proposal issue |

**Methodology:** Follow `.squad/skills/external-research/SKILL.md` for research angles, source quality, output format, and the commit-to-repo requirement.

**Agent assignment:**
- Decompose the research question into 3–5 complementary angles
- Assign each angle to the agent best equipped for it (see skill for mapping)
- Spawn researchers in parallel — no data dependencies between angles

**Output:**
1. Research file(s) in `research/{domain}/` with cited sources, DD mapping, and summary table
2. Issue comment on the proposal linking to the research file(s)
3. Open questions flagged for the Design Review agenda

**Sequencing:** This ceremony runs BEFORE Design Review. Research findings are inputs to the review — reviewers cite them when evaluating design decisions. If research surfaces challenges to design decisions, the proposer updates the design doc before the review ceremony begins.

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

### Coordinator pre-flight (BLOCKING — do these BEFORE spawning reviewers)

1. **Read `.squad/skills/pr-review/SKILL.md`** — this defines the structured JSON format, the posting workflow via `squad-review.js`, and the full review conversation lifecycle (initial review → dev fix → re-review). Do not spawn reviewers without reading it.
2. **Spawn reviewers as full agents (not Explore)** — reviewers need tool access to read files with accurate line numbers. Never use `agentName: "Explore"` for reviews.
3. **Include in each reviewer's prompt:** their charter identity, the linked issue's acceptance criteria, the structured JSON output format from the skill file, and the instruction to produce a JSON review file (not chat-only analysis).
4. **After collecting results:** post reviews to the PR via `node tools/scripts/squad-review.js <pr-number> <review-file.json>`. Reviews must be durable on the PR, not buried in chat.

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
