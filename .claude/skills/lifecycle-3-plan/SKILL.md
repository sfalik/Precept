---
name: lifecycle-3-plan
description: Stage 3 of the engineering lifecycle — produce a phased execution plan from a locked design or audit. Triggers on — plan, execution plan, phases, "how do we ship this", roadmap, sequencing, "what's the plan to get this done". Takes a design doc (`/lifecycle-2-design` output) or an audit doc and produces a phased plan with heavyweight current+next phases, lightweight stubs for later phases, decisions-required-per-phase, exit criteria, and doc-touch obligations enumerated per phase.
---

# Precept Execution Planning

Stage 3 of the engineering lifecycle. Bridges design (Stage 2) and execution (Stage 4). Produces a phased plan that surfaces decisions as upstream gates, exit criteria as testable conditions, and doc updates as explicit per-phase sub-tasks.

## When to use

- A locked design (`/lifecycle-2-design` output) needs phasing for execution
- An audit doc (e.g., compiler-readiness review) has findings that need phased remediation
- Multiple work items need sequencing based on dependencies + decisions

## When NOT to use

- Single-step task with no phasing needed (just execute)
- Plan already exists and is current — extend or update it, don't replace

## Required output structure

A markdown file at `docs/Working/<topic>-plan-YYYY-MM-DD.md`:

```markdown
# <Topic> Plan — YYYY-MM-DD

**Status**: Draft / Active
**Companion docs**: links to design doc(s) and/or audit doc(s) this plan derives from
**Scope gate**: what this plan unlocks (e.g., "blocks runtime implementation")

## Phase summary
Table: phase #, goal, F-count (findings or work items), decisions required, effort estimate, status.

## Decisions captured
What's been decided and when. Cross-link to the Wave 0 triage doc if one exists.

## Open decisions
Categorized by which phase gates them. Each item: question, options, recommended, where to land the answer.

## Heavyweight phase blocks (current + next)
For each, all of:
- **Goal** (one-sentence outcome)
- **Findings/items in scope** (F-IDs)
- **Decisions required before kicking off** (subset of "Open decisions" above)
- **Step-by-step execution** (file paths, validation criteria)
- **Dependencies** (what must finish first)
- **Exit criteria** (testable conditions for "phase complete")
- **Estimated effort** (S/M/L/XL with day estimate)
- **Doc-update obligations** (per CLAUDE.md routing — which canonical docs land in this phase)

## Lightweight phase stubs (later phases)
For each, just goal + scope + decisions required + effort. Mark **"Status: Stub — TBD pending Phase N-1 completion and listed decisions."**

## Definition of done
Exit criteria for the overall workstream — what state must hold for "plan complete" / for the scope gate to open.

## Plan update protocol
When a phase completes / when findings emerge / when decisions get made.
```

## Behavioral guards

The skill enforces:

1. **No phase without exit criteria.** Refuses to produce a phase block without testable "phase complete" conditions.

2. **Decisions surface as gates, never buried.** If the skill detects in-line "we'll decide later" markers in execution steps, it lifts them to the Decisions section.

3. **Only N+1 phases at heavyweight detail.** Phases beyond the next one MUST be stubs. Prevents over-planning futures that earlier phases will reshape. (Heuristic: heavyweight only for phases starting within ~2 weeks.)

4. **Doc-touch enumeration per phase.** Each phase's "Doc-update obligations" section is auto-populated from the CLAUDE.md routing table based on the file paths the phase touches. Author can edit.

5. **Effort estimates required.** Refuses S/M/L without a corresponding day-band estimate to prevent uncalibrated scope.

6. **"What's already decided" log.** Forces a Decisions captured section so decisions don't get re-litigated when the plan is revisited.

## Composability

- **Input**: `--from <design-or-audit-doc>` — extracts findings, locked decisions, and acceptance criteria; pre-populates phase scope and exit criteria.
- **Output**: phased plan at `docs/Working/<topic>-plan-YYYY-MM-DD.md` — consumed by execution work in Stage 4, then by `/lifecycle-5-promote` when each phase completes.

## Anti-patterns to refuse

- All phases at the same level of detail ("plan everything now")
- Phases without exit criteria ("phase 5: do proof engine work")
- Decisions buried mid-execution ("step 3: figure out whether to use X or Y")
- No dependency graph ("just run them in parallel")
- Effort estimates without day bands ("L")

## Quick reference

| Symptom | Skill response |
|---|---|
| Phase 5 fully detailed when N=2 | Refuse; collapse to stub until N+1 |
| "Step: decide whether to use X" | Lift to Decisions section as gate |
| Exit criteria = "phase complete" | Refuse; prompt for testable condition |
| Effort = "L" with no day estimate | Refuse; ask for day band |
| No "Decisions captured" log | Add the section; populate from history |
