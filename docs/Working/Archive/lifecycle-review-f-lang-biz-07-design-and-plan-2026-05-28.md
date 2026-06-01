# Lifecycle Review — F-LANG-BIZ-07 Design + Plan (pre-execution)

> **Archived 2026-05-31** — pre-execution review of BIZ-07 composite period basis, which fully shipped (Phase 6 complete). Historical reference; do not edit.

**Date**: 2026-05-28
**Work item**: F-LANG-BIZ-07 composite period basis — **design + plan only**, before implementation.
**Branch**: `spike/Precept-V2-Radical`
**Scope**: This is a *pre-execution* review. It verifies Stages 1–3 (research, design, plan) are sound before build effort is committed. Stages 4–5 (execute, promote) have not started by design and are not failures.

## Verdict

🟡 **Needs a plan amendment before execution — no design change, no owner decision.** The one substantive finding (P0-1) is a *pre-existing implementation gap*: `PeriodDimension.Datetime` is locked design intent (it predates this amendment) but was never built, and the plan didn't allocate a step to build it. Composite period basis depends on it, so Phase 6 must absorb it. P1-1 is pre-existing doc drift (a cheap adjacent fix). The design itself — separator, whitespace, canonicalization, layer placement, runtime-deferral, research grounding — is clean and needs no change. (An earlier draft of this report wrongly framed P0 as a new-surface owner decision; corrected after checking the design doc.)

## Stage-by-stage

### Stage 1 — Research ✅
Two Stage-1 surveys, both status `Cited`, both with methodology + sources + access dates:
- `research/language/expressiveness/period-basis-separator-survey.md` — grounds `+` over `&`.
- `research/language/expressiveness/literal-whitespace-consistency-survey.md` — grounds lenient-input + spaced-canonical.
Both cited in the D4 amendment commit (`03dfc4da`) and the plan's Companion docs. Research-cited exit (a).

### Stage 2 — Design ✅ (with a note)
The design is the **canonical D4 amendment** in `business-domain-types.md` (commit `03dfc4da`), not a `/lifecycle-2-design` Working doc. This is the correct form for amending an already-locked spec decision with owner authorization + research grounding (the spec IS the design; see `[[project-readiness-plan-purpose]]`). Four-leg present in the rewritten D4 (Rationale, Alternatives-rejected with the missed-alignment reason, Precedent citing the survey, Tradeoff). Acceptance criteria are carried in the plan's per-workstream exit criteria (testable). Doc-update enumeration present in the plan.
- **Note**: because the design lives in canonical (not a Working doc with `status: Locked` frontmatter), the skill's literal "Locked YYYY-MM-DD design doc" check is satisfied by the committed amendment + the four-leg D4, not a Working-doc status field. Judged ✅ — the owner authorized the amendment explicitly and the four legs are present.

### Stage 3 — Plan ✅ (with the P0/P1 below as content gaps)
Phase 6 detail section in `compiler-readiness-plan-2026-05-24.md` (commit `8f755f97`). Workstreams W-A–W-D each have goal, steps, exit criteria, effort band, dependencies, and doc-update obligations. Decisions surfaced as gates (D6.1 sample, D6.2 runtime-deferral), both resolved in-plan with options + recommendation. The plan format is sound; the **content** gaps are P0-1 and P1-1 below — the plan under-allocated the `'datetime'` dimension surface and inherited a doc contradiction.

### Stage 4 — Execute — not started
This is a pre-execution review. No code shipped yet, by design. Not a failure.

### Stage 5 — Promote — not started
Promotion happens after build. Not applicable yet.

### Acceptance criteria — defined and testable ✅; satisfaction deferred
The plan's per-workstream exit criteria are test-shaped (specific `precept_compile` inputs + expected diagnostics/returns). Satisfaction is verified post-build, not now.

### Stage 7 — Catalog-discipline + doc-sync (precept-reviewer) 🟡
`precept-reviewer` spawned against the design+plan artifacts. Findings folded below.

## precept-reviewer findings

### 🟠 P0-1 (downgraded from 🔴 to 🟠 after design-doc check) — pre-existing implementation gap: `PeriodDimension.Datetime` is specced but unbuilt; the plan under-allocated it
- **Reframed after checking the design doc against `03dfc4da^` (pre-amendment).** The `'datetime'` period dimension — **including `period of 'datetime'` as a declarable constraint** — is *pre-existing locked design intent*, not surface the amendment introduced:
  - The `period of` dimension table already had a `'datetime'` row marked `(new)`: "all components | `LocalDateTime.Plus(Period)` accepts all."
  - The registry table (`:787`) already listed `'datetime'` as a temporal dimension used by both `period.dimension` AND `period of '...'`.
  - Line 1226's `.dimension`-returns-`'datetime'` language pre-dates the amendment (only the `&`→`+` flip on that line is mine).
  - The amendment's line 403 (`use period of 'datetime'`) is **consistent** with this pre-existing design, not new surface.
- **The real gap is implementation, not design**: `PeriodDimension` (`src/Precept/Language/ProofRequirement.cs:66`) has only `Any`/`Date`/`Time` — no `Datetime`. `MapTemporalDimensionQualifier` (`TypeChecker.cs:377`) accepts only `"date"`/`"time"`. The `(new)` marker flags `'datetime'` as specced-but-unbuilt. Closing this is squarely the readiness plan's purpose (locked spec → missing implementation).
- **No owner decision.** `period of 'datetime'` is already locked; a "derived-only" option would contradict the spec. There is no A/B choice — the earlier "Owner decision" section is withdrawn.
- **The actionable finding stands**: the plan's W-A/W-B reference `PeriodDimension.Datetime` but the plan never allocated a step to *add* it, and the file-touch list omits `ProofRequirement.cs` and the `of`-parse path. Composite period basis genuinely depends on `'datetime'` (a `'days + hours'` basis spans both → `.dimension` must return `'datetime'`), so this gap must be closed as a Phase 6 prerequisite/early step.
- **Fix**: add an explicit W-A step (or a W-0 prerequisite) — add `Datetime` to `PeriodDimension`; add the `"datetime" => Datetime` arm to `MapTemporalDimensionQualifier`; add the combined-dimension computation in `MapTemporalUnitQualifier`; add `PeriodDimension.Datetime => "datetime"` to `ExtractComparableValue`/proof-markers; confirm the partition validator admits `'datetime'`. Add `ProofRequirement.cs` + the `of`-parse path to the file-touch list. Note the `PeriodDimension`-enum expansion may carry a temporal-type-system / §0.6 doc-sync obligation beyond W-A's current three docs.

### 🟠 P1-1 — `.dimension` row lists atoms the catalog doesn't have
- `business-domain-types.md:1226` lists time bases "`hours`, `minutes`, `seconds`, `milliseconds`, `nanoseconds`, `ticks`" but `TemporalUnits` (`Time/TemporalUnits.cs:28-37`) defines only `{year, month, week, day, hour, minute, second}`. W-A's atom validator (`TemporalUnits.TryGet`) would reject `milliseconds`/`nanoseconds`/`ticks` as unknown (PRE0161), contradicting the spec's own enumeration on a line the amendment edited.
- **Fix (unambiguous)**: drop `milliseconds`, `nanoseconds`, `ticks` from line 1226 to match the catalog. Sub-second atoms are not in the canonicalization order (1321), the legal-basis tables (1343-1345), or `TemporalUnits` — if ever wanted, that's separate scope.

### 🟡 P2-1 — `weeks` canonical sort position (build-verification note)
Canonical order (1321) places `weeks` between `months` and `days`, matching NodaTime's `PeriodUnits` flag order. W-A should assert this with a test (`period in 'days + weeks'` → canonical `'weeks + days'`). Spec is internally consistent; no change.

### 🟡 P2-2 — proof markers now carry spaces (build-verification note)
`$eq:X.basis:hours + minutes` is the first space-bearing marker value. Likely a non-issue (markers appear structured, not string-parsed), but W-B's narrowing test must use a genuinely composite, space-bearing value and check marker equality end-to-end.

### NIT-1 — catalog count (confirmed correct)
`catalog-system.md:288` has a Mermaid node `Diagnostics (159)` → must bump to `162`. The plan's W-A doc-obligation correctly routes here.

### NIT-2 — operator-headroom tradeoff (already accepted)
`+` is now spent in both basis and value position; a future temporal-basis combiner has no cheap distinguishing operator left. Aligns with D4's accepted tradeoff. Revisit only if a second combiner semantic is ever needed inside `'...'`.

## Owner decision — WITHDRAWN

The earlier draft of this report framed `of 'datetime'` as a new-surface owner decision (Option A derived-only vs Option B declarable). **That was an error** — checking the design doc against `03dfc4da^` showed `period of 'datetime'` is pre-existing locked design intent (the `period of` dimension table and the registry table both predate the amendment). There is no decision to make; `'datetime'` is already in the design. The gap is purely that the implementation never built `PeriodDimension.Datetime`. No consultation gate applies — this is closing a locked-spec implementation gap, not adding surface.

## Remediation before execution

1. **Amend the plan (W-A or a W-0 prerequisite)**: add an explicit step to build the pre-existing `PeriodDimension.Datetime` gap — enum member + `MapTemporalDimensionQualifier` arm + combined-dimension computation + `ExtractComparableValue`/proof-marker arm + partition-validator admission. Add `ProofRequirement.cs` and the `of`-parse path to the file-touch list. Flag the possible temporal-type-system / §0.6 doc-sync obligation from the enum expansion.
2. **Fix P1-1**: drop `milliseconds`/`nanoseconds`/`ticks` from `business-domain-types.md:1226` to match the `TemporalUnits` catalog (pre-existing drift; cheap adjacent fix).
3. **Fold P2-1, P2-2, NIT-1** into W-A/W-B/W-D as build-verification notes (no design change).

All doc/plan edits — no code yet. `business-domain-types.md:403` stays as-is (it was consistent with the pre-existing design). Re-run this review or proceed once remediated.
