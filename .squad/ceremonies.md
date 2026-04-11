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

**Agenda:**
1. Review the task and requirements
2. **Impact analysis** — walk through Runtime, Tooling, and MCP impact categories (see `language-surface-sync.instructions.md`). Implementing devs flag gaps.
3. Agree on interfaces and contracts between components
4. Identify risks and edge cases
5. Assign action items

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
