# CONTRIBUTING.md Updates — Draft

**Status**: Draft — for Phase 1 execution.
**Purpose**: Source-of-truth draft for the doc-lifecycle additions to `CONTRIBUTING.md`. When Phase 1 runs, these sections get inserted into `CONTRIBUTING.md` at the indicated locations.

---

## Proposed insertion: new top-level section, after `## Development Workflow` and before `### Proposal Lifecycle`

Location: between current lines 3 and 5 of `CONTRIBUTING.md`.

```markdown
## Doc Lifecycle

Every meaningful design or implementation decision moves through six stages. The lifecycle ensures that **why-content** (rationale, alternatives, tradeoffs, precedent) is captured at decision time and preserved as the work moves from idea to maintenance. Lifecycle skills automate the transitions between stages.

| Stage | Activity | Skill | Where work lives |
|---|---|---|---|
| 1 | Research / explore | `/lifecycle-1-research` | `research/` |
| 2 | Lock a design | `/lifecycle-2-design` | `docs/Working/` |
| 3 | Plan execution | `/lifecycle-3-plan` | `docs/Working/` (plan doc) |
| 4 | Execute the plan | — (engineering work) | code + tests |
| 5 | Promote to canonical | `/lifecycle-5-promote` | canonical `docs/` updated; design moved to `docs/Working/Archive/` with cross-link |
| 6 | Maintain | `/lifecycle-6-audit` (Phase 9) | canonical `docs/` |

### Stage 5 is mandatory

The most common failure mode is **Stage 4 → 5 transition skipped**: implementation ships, design doc gets archived, but canonical doc never receives the "why." The 2026-05-24 compiler-readiness review found 4+ instances of this pattern across hover-design, constructor-semantics, diagnostic-enforcement, and SemanticTokenTypes Slice 10 — substantial design rationale stranded in `docs/Working/Archive/` while canonical docs lagged.

**Rule**: every design doc moved to `docs/Working/Archive/` MUST carry a top-of-file header declaring one of:
- `**Promoted to:** <canonical link>` — the why lives in the linked canonical doc
- `**Status:** Historical — superseded by <link>` — concept evolved into a different design
- `**Status:** Historical — design dropped, no canonical replacement` — design abandoned

The `/lifecycle-5-promote` skill enforces this — it refuses to archive a design doc without the header. Manual archive moves (via `mv`) bypass the skill, which is allowed but reviewer-checked.

### Pointer-philosophy applies to canonical content

After Stage 5 promotion, canonical docs should preserve "why" content and point to code for "what" content. Concretely:

- **Enumerable content** (member lists, type/field shapes, counts, file paths) → **pointer to code**: e.g., `See \`src/Precept/Language/Tokens.cs\` for member list.`
- **Conceptual content** (architecture, design rationale, why-decisions, tradeoffs) → **hand-written, preserved across promotion**

The 2026-05-24 review found `catalog-system.md` had drifted on counts (14 of 14 catalogs had at least one count discrepancy) precisely because enumerable content was duplicated in the doc. Pointer-philosophy makes that class of drift mechanically impossible.

### Four-leg rationale policy

Per the "Per-Decision Rationale (Non-Negotiable)" section in `CLAUDE.md`, locked design decisions must include four legs:
1. **Rationale** — why this choice
2. **Alternatives considered** — and rejection reasons
3. **Precedent** — research / prior art / existing pattern grounding the choice
4. **Tradeoff accepted** — known downside being taken on

**Scope of the rule**:

- **New decisions** going through `/lifecycle-2-design`: **required**. The skill refuses to mark a design "Locked" without all four legs on every decision. Author can answer "no precedent — novel choice" or "no tradeoff identified — flag for review" honestly, but cannot skip.
- **Decisions backed by Archive design docs** (Stage 4 → 5 promotion): the `/lifecycle-5-promote` skill lifts whatever depth the source provides. Pre-policy designs with Decision + Rationale only get lifted as-is with a "no further rationale recorded in source" note. **No fabrication.**
- **Existing canonical doc § Design Rationale entries** without four legs: **grandfather**. No required backfill. `/lifecycle-6-audit` may flag these as gaps, but they don't block promotion of new work.

The rule's purpose is to prevent future ambiguity at decision time, not to retroactively annotate shipped code. Honest grandfathering beats fabricated four-leg structure.

### Doc routing table

When implementation work touches code, the canonical docs that may need updates depend on what's touched. The CLAUDE.md "Documentation Sync" section is the source of truth for routing:

| Kind of change | Update |
|---|---|
| Pipeline stage behavior | `docs/compiler/<stage>.md` |
| Runtime API | `docs/runtime/runtime-api.md` + relevant per-type doc |
| Language surface (keyword, type, operator, modifier, construct) | Catalog entry first; then `docs/language/precept-language-spec.md` + relevant type doc |
| Diagnostic added/changed | `docs/compiler/diagnostic-system.md` |
| Catalog architecture | `docs/language/catalog-system.md` |
| MCP tool surface | `docs/tooling/mcp.md` + DTO/formatter in `tools/Precept.Mcp/` |
| Language server feature | `docs/tooling/language-server.md` |
| Doc status changing (Stub → Design → Implemented) | The doc's own Status field AND any cross-referencing tables |
| README claim invalidated | `README.md` |

`/lifecycle-2-design` consults this table when populating a design doc's "Doc-update enumeration" section. `/lifecycle-3-plan` uses that enumeration to populate per-phase doc-touch obligations. `/lifecycle-5-promote` verifies those obligations at promotion time.

The skills make routing automatic — authors don't need to memorize the table, but should understand it exists so they can override when the heuristic gets a case wrong.
```

---

## Proposed update: existing "Proposal Lifecycle" section

Location: existing `### Proposal Lifecycle` section starting at line 5 of `CONTRIBUTING.md`.

**Recommended change**: reframe this section as Stage 1-3 of the broader lifecycle. Keep all existing content about issues, research, design review, etc. — but add a note at the top:

```markdown
### Proposal Lifecycle (Stages 1-3 of the Doc Lifecycle)

When PR-and-issue workflow is in use (main branch development), the proposal lifecycle below maps onto Stages 1-3 of the Doc Lifecycle:
- Stage 1 (Research) corresponds to "Research" below
- Stage 2 (Lock a design) corresponds to "Design Review" below + the design doc in Track B
- Stage 3 (Plan execution) corresponds to "Implementation plan" in the PR body

On spike branches without PRs (current `spike/Precept-V2-Radical` workflow), the lifecycle skills (`/lifecycle-1-research`, `/lifecycle-2-design`, `/lifecycle-3-plan`) handle the same transitions without the GitHub gates. The discipline is the same; the enforcement mechanism differs.

Stages 4-6 (execute, promote, maintain) are the same on both workflows.

[existing content continues...]
```

---

## Proposed update: existing "Spike Workflow" section

Location: existing `### Spike Workflow` section at line 214 of `CONTRIBUTING.md`.

**Recommended addition** at the end of the existing spike workflow section:

```markdown
**Doc lifecycle on spike branches**:

The doc lifecycle (Stages 1-6) applies in full on spike branches. Without GitHub PRs as enforcement gates, the lifecycle skills become the primary discipline:

- `/lifecycle-1-research` — exploration in `research/`
- `/lifecycle-2-design` — lock the design with four-leg rationale; refuses to lock without
- `/lifecycle-3-plan` — phased execution plan with decisions surfaced as gates
- (execute the plan)
- `/lifecycle-5-promote` — lift "why" to canonical, archive with header. **Mandatory** — design docs cannot reach Archive without it (or the explicit historical-status header).

Reviewer prompts (the GitHub PR template equivalent) are folded into `/lifecycle-3-plan` (doc-touch obligations enumerated upfront) and `/lifecycle-5-promote` (obligations verified at promotion). The discipline lives in the skills the agent invokes, not in a manual checklist.
```

---

## Proposed update: existing "Where Things Live" section

Location: existing `## Where Things Live` section at line 264 of `CONTRIBUTING.md`.

**Recommended addition** at the end of the existing section:

```markdown
### Doc Lifecycle Path

| Stage | Path |
|---|---|
| Research (Stage 1) | `research/<subfolder>/<topic>.md` |
| Locked design (Stage 2) | `docs/Working/<topic>.md` with frontmatter `status: Locked YYYY-MM-DD` |
| Execution plan (Stage 3) | `docs/Working/<topic>-plan-YYYY-MM-DD.md` |
| Canonical (Stage 5+) | `docs/compiler/` / `docs/language/` / `docs/tooling/` / `docs/runtime/` per the routing table |
| Archived design (Stage 5 complete) | `docs/Working/Archive/<original-filename>` with `**Promoted to:**` header |
| Audit reports (recurring Stage 6) | `docs/Working/<workstream>-review-YYYY-MM-DD.md` |

The `/lifecycle-*` skills handle moves between these locations.
```

---

## Implementation notes for Phase 1

When Phase 1 executes the CONTRIBUTING.md updates:

1. **Insertion order**: add the new "## Doc Lifecycle" section first (before existing Proposal Lifecycle), then update the references in existing sections.
2. **Verification**: after applying, grep `CONTRIBUTING.md` for `/lifecycle-` references and verify all 5 skill names appear (research, design, plan, promote, audit).
3. **Cross-link**: also add a "**See also**: `CONTRIBUTING.md` § Doc Lifecycle" note near the top of `CLAUDE.md`'s "Documentation Sync (Non-Negotiable)" section so the lifecycle policy is discoverable from both directions.
4. **No removal of existing content**: the existing CONTRIBUTING.md content stays. The lifecycle framework is additive — it gives the existing workflow structure, not a replacement.

Effort: S (~30-45 min) — mostly cut-and-paste into the right locations, plus the CLAUDE.md cross-link.
