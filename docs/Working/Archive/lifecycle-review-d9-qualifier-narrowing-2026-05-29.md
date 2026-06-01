# Lifecycle-6 Completion Review — D9 Open-Field Qualifier Narrowing

> **Archived 2026-05-31** — completed lifecycle-6 review of the (shipped) D9 work. Historical reference; do not edit.

**Date**: 2026-05-29
**Work item**: D9 open-field qualifier narrowing (Phase 6) — slices S1, S2a, S2b, S3 + the (0) temporal-denominator price-parsing prerequisite.
**Branch**: `spike/Precept-V2-Radical` (spike mode — no PR; plan doc is the hub).
**Design**: `d9-qualifier-narrowing-design.md` · **Plan**: `d9-qualifier-narrowing-plan-2026-05-29.md` · **Research**: `d9-qualifier-narrowing-analysis-2026-05-28.md`

## Verdict: ✅ Ready to sign off — after the in-pass remediation below, with one explicitly-accepted debt.

All five lifecycle stages were processed. The catalog/doc-sync spot-check (precept-reviewer, fresh context) found catalog discipline clean, the `$eq:` fiction fully purged, and the sample legitimate. The two P1 doc-sync gaps it surfaced, plus the promote-tail and acceptance-criterion items, were **remediated in this close-out pass**. One acceptance-criterion clause is deferred to W-C (recorded).

## Per-stage

| Stage | Status | Evidence |
|---|---|---|
| 1 — Research | ✅ | `d9-qualifier-narrowing-analysis-2026-05-28.md` (3 lenses: requirements/soundness/architecture); cited by the design's `sources-consulted`. |
| 2 — Design | ✅ | Locked 2026-05-29; 7 decisions each with four-leg rationale; doc-update enumeration present; acceptance criteria testable. *One criterion over-specified — see Acceptance.* |
| 3 — Plan | ✅ | Plan hub with per-slice rows, exit criteria, decisions-as-gates, doc-touch obligations, per-slice ✅ + commit hashes. |
| 4 — Execute | ✅ | Commits `4e5f837c` `fc10db45` `f662aad3` `5d071ffa` `5949de4b` `04bf5f0e` `75b83980` `01ed818f` `04933c31`; `dotnet test` 6567 green; `dotnet build` 0 new warnings; per-slice adversarial reviews (S1/S3/(0)) clean. |
| 5 — Promote | ✅ (remediated) | § D9 rewritten to the real mechanism (`5949de4b`); design now carries a `promoted-to:` header; analysis marked historical; the two P1 doc-sync gaps fixed (below). |
| Acceptance | ✅ / 1 debt | Most criteria satisfied; criterion #4's cancellation clause deferred to W-C. |
| Catalog/doc-sync | ✅ | precept-reviewer: catalog clean, fiction purged, sample clean; 2 P1 → remediated. |

## Findings & remediation (all closed in this pass)

1. **P1 — diagnostic-system.md missing the narrowed-vs-required wording** (design obligated it). → **Fixed**: documented the `PRE0141` detail clause and the `(from guard)` operand suffix on `PRE0114`/open-period `PRE0113`.
2. **P1 — catalog-system.md count contradiction** ("13 members" vs an adjacent "(10 members)" + a 10-node mermaid diagram). → **Fixed**: count → 13; added `IndexBounds`, `DimensionalProduct`, `AssignmentQualifier` nodes/edges to the diagram.
3. **Promote-tail — design doc lacked an archive/promoted marker.** → **Fixed**: `promoted-to:` header added; analysis doc status → historical.
4. **Acceptance criterion #4 conflated narrowing with cancellation.** The criterion read "`.basis == 'hours'` … enables `price in 'USD/hours' × period`; composite does not cancel a single-unit denominator." D9/S3 delivered the **D14 basis-assignment** subset discharge; the **cancellation** half (dimension-level `price × period`, and composite single-basis rejection) is W-C. → **Fixed**: criterion corrected to scope D9 to assignment narrowing; cancellation explicitly deferred.

## Accepted debt (recorded)

- **Acceptance criterion #4 — `.basis` cancellation half → deferred to W-C.** `period in 'hours'` does not yet cancel `price in 'USD/hours'` (over-rejects), and composite single-basis rejection (D15) is unbuilt. This is **basis-aware cancellation = W-C**, not D9 narrowing. Durable record: W-C's exit criteria in `compiler-readiness-plan-2026-05-24.md` (both the rejection and the newly-added acceptance side) + the pinned test `PricePerHours_TimesPeriodInHours_NotYetProven` (flips RED→GREEN when W-C lands). No separate debt-log entry needed — the obligation lives in W-C's gate.

## NIT (non-blocking)

- `GetApplicableAssignmentQualifierAxes` is a hardcoded type→axes map (pre-existing); the `Period → TemporalUnit` arm is defensible (`.basis` deliberately carries `ReturnsQualifier: None`, so the axis isn't trivially catalog-derivable). Future catalog-derivation pass.

## Sign-off

D9 open-field qualifier narrowing is **complete and lifecycle-clean**. Next build effort: **W-C** (basis-aware cancellation), which carries the one deferred acceptance clause.
