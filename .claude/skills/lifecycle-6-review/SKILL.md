---
name: lifecycle-6-review
description: Stage 6 of the engineering lifecycle — end-of-lifecycle completion review for a finished work item. Triggers on — review this work, "did we complete this properly", "is this ready to close", lifecycle review, completion check, "before we sign off on X". Verifies that all 5 earlier stages (research, design, plan, execute, promote) were processed for the given work item. One-shot per work item — distinct from `/lifecycle-7-audit` (periodic drift detection) and from `precept-reviewer` (real-time catalog/code review).
---

# Precept End-of-Lifecycle Review

Stage 6 of the engineering lifecycle. Holistic completion verification for a single work item. Catches the failure mode where a slice ships but skipped a lifecycle stage — e.g., promoted to canonical without proper design lock, or executed without a plan with exit criteria, or shipped with no acceptance criteria the team can verify against.

## When to use

- A work item is "done" by the team's informal sense; before declaring it formally complete
- Closing a slice, phase, or named workstream
- Before stamping a milestone or release marker
- After running `/lifecycle-5-promote` — natural follow-up to verify the full lifecycle ran cleanly
- User says "is this ready to sign off?" / "did we complete this properly?" / "review this work"

## When NOT to use

- Mid-execution code review (use `/code-review` or spawn `precept-reviewer` agent)
- Ongoing canonical-doc drift check (use `/lifecycle-7-audit`, Phase 9)
- Pre-work design check (use `/lifecycle-2-design` which has its own review at lock time)

## Required workflow

```
/lifecycle-6-review <work-item> [--design <design-doc>] [--plan <plan-doc>] [--strict]
```

The skill verifies the work item was processed through all 5 earlier stages:

1. **Stage 1 — Research**
   - Was research done? Check for `research/<area>/` docs cited in the design.
   - If no research found: emit ⚠️ "no research cited — intentional?" — owner judges (some work is small enough to skip research; some isn't)

2. **Stage 2 — Design**
   - Design doc exists in `docs/Working/` or `docs/Working/Archive/`
   - Status declared "Locked YYYY-MM-DD"
   - Every decision has four-leg structure (Rationale + Alternatives + Precedent + Tradeoff)
   - Acceptance criteria are testable (not prose)
   - Doc-update enumeration is present
   - 🔴 if any of these are missing

3. **Stage 3 — Plan**
   - Plan doc exists (compiler-readiness-plan, feature-plan, etc.)
   - Phases defined with exit criteria
   - Decisions surfaced as gates (not buried in execution steps)
   - Doc-touch obligations per phase enumerated
   - 🔴 if any phase the work item touches lacks exit criteria

4. **Stage 4 — Execute**
   - Code shipped (commit refs in design or plan)
   - Tests added or extended (count present in design's acceptance criteria? satisfied?)
   - Build clean (`dotnet build` 0 warnings if scoped to .NET work)
   - 🔴 if tests are missing or build is dirty

5. **Stage 5 — Promote**
   - Canonical doc(s) updated with why-content from the design (per design's doc-update enumeration)
   - Archive header on design doc: `**Promoted to:** <link>` (or explicit historical status)
   - 🔴 if any enumerated canonical doc not touched OR archive header missing

6. **Acceptance criteria**
   - From design doc, demonstrably satisfied (test refs, screenshots, MCP probes, etc.)
   - 🔴 if any acceptance criterion has no evidence of satisfaction

7. **Catalog-discipline + doc-sync spot-check**
   - Spawn `precept-reviewer` agent against all files touched by the work item
   - Fold its findings into the report
   - 🔴 if precept-reviewer surfaces P0/P1 findings

**Output**: completion report at `docs/Working/lifecycle-review-<work-item>-YYYY-MM-DD.md` with per-stage ✅/⚠️/🔴 status, summary verdict (Ready to sign off / Remediation required / Owner judgment needed), and remediation items for each ⚠️/🔴.

## Behavioral guards

The skill enforces:

1. **No "Lifecycle complete" verdict if any 🔴.** Author either remediates or invokes `--accept-debt` flag with explicit reasoning ("acceptance criterion 3 deferred to Phase N because…").

2. **⚠️ statuses surface owner-judgment questions explicitly.** Not auto-resolved. The skill produces clear prompts for owner ("Was Stage 1 research intentionally skipped here? Y/N + rationale").

3. **`--strict` mode treats ⚠️ as 🔴.** For high-stakes milestones (e.g., production release marker), forces explicit answer to every judgment question.

4. **`--accept-debt` flag is captured and recorded.** Any "remediation deferred" goes into a debt log (`docs/Working/lifecycle-debt-log.md` or per-project equivalent) so deferrals don't vanish into the void.

5. **Skill is read-only.** Produces report; doesn't apply fixes. Author uses the report to drive remediation work, then re-runs the skill.

## Composability

- **Input**: `--design <design-doc>` (recommended — anchors the review to a specific design); `--plan <plan-doc>` (optional — adds phase-scope context); `--strict` flag; `--accept-debt` flag with rationale
- **Output**: completion report at `docs/Working/lifecycle-review-<work-item>-YYYY-MM-DD.md`
- Pairs with: `/lifecycle-5-promote` (runs immediately after to verify promotion completeness); `precept-reviewer` (spawned internally for catalog/code review)

Distinct from:
- `precept-reviewer` — runs against code/diff in real-time, doesn't check process artifacts
- `/lifecycle-7-audit` — runs against all canonical docs periodically, checks for drift over time
- `/code-review`, `/security-review` — focused on code, not lifecycle process

## Anti-patterns to refuse

- Mark "complete" with 🔴 status (must remediate or explicitly accept debt)
- Skip the precept-reviewer spawn (catalog discipline must be verified)
- Skip acceptance-criteria verification (the design said this passes; the review must verify it passes)
- Accept "✅ all green" without evidence (the report should cite test files, commit refs, MCP probe outputs)

## Quick reference

| Symptom | Skill response |
|---|---|
| Design doc has no acceptance criteria | 🔴 — Stage 2 incomplete; remediate before signing off |
| Plan doesn't enumerate doc-touch | 🔴 — Stage 3 incomplete |
| Canonical doc not updated per plan's doc-touch list | 🔴 — Stage 5 incomplete; run `/lifecycle-5-promote` for the gap |
| Tests exist but acceptance criterion has no test ref | ⚠️ — surface judgment question |
| No research cited for a small bug fix | ⚠️ — likely OK; ask owner |
| `--strict` mode + ⚠️ present | Treat as 🔴 |
| User invokes `--accept-debt "reason"` | Record in debt log; downgrade 🔴 to ✅* |
