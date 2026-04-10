# Orchestration Log Entry

### 2026-04-08T23:55:00Z — Soup Nazi charter + language-proposal template update (Coordinator direct)

| Field | Value |
|-------|-------|
| **Agent routed** | Coordinator (direct) |
| **Why chosen** | Charter updates and template edits are coordination-layer work. The Coordinator owns `.squad/agents/*/charter.md` maintenance and GitHub issue template configuration; no spawn needed. |
| **Spawn count** | 0 (direct execution) |

#### Action 1 — Soup Nazi charter: MCP Regression Testing skill section
| Field | Value |
|-------|-------|
| **File modified** | `.squad/agents/soup-nazi/charter.md` |
| **What changed** | Added `## MCP Regression Testing` section between `## How I Work` and `## DSL Feature Input`. Section includes: authoring rules (5 hard-won corrections from exploratory execution), Round 1 compile surface coverage requirements, Round 2 runtime path coverage requirements (all 7 outcome kinds), Round 3 stateless end-to-end, Round 4 diagnostic edge cases. |
| **Why** | Exploratory rounds 1+2 surfaced 5 test-plan authoring errors. Baking these into the charter ensures future regression rounds authored by any agent start from correct syntax knowledge rather than rediscovering parse failures. |

#### Action 2 — Language proposal template: regression checklist item
| Field | Value |
|-------|-------|
| **File modified** | `.github/ISSUE_TEMPLATE/language-proposal.yml` |
| **What changed** | Added checklist item to `proposal_checklist`: "MCP regression testing (all 4 rounds) completed and passed after implementation — verify before closing the PR." |
| **Why** | The MCP regression pass is now a named, repeatable skill. Making it a required checklist item on language proposals closes the feedback loop: every language feature that ships through a proposal must also pass the full 4-round MCP regression before merging. |
