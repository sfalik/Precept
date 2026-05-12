# Elaine — Hover Design v3 Decision Record

**Date:** 2026-05-12  
**Artifact:** `docs/Working/hover-design.md`  
**Reviewers addressed:** Frank (Lead/Architect), Kramer (LS Dev)

---

## Summary of Changes

### Blockers Fixed

1. **Rule scope (Frank B1):** Changed "Enforced in: all reachable states" → "Scope: global — enforced after every mutation". Added guarded rule variant showing "Scope: global when `<guard>`". Rules are global data truth, not state-partitioned.

2. **Field enforcement (Frank B2):** Removed `inspect` from enforcement claim. Changed to "enforced on every mutation before commit". Inspection is non-mutating preview; it does not enforce.

3. **Lead lines (Kramer B1):** Redesigned lead-line strategy. Only `rule`/`ensure` (via `because` text) and `reject` (via `RejectReason`) lead with authored rationale. All other constructs (`field`, `state`, `event`, `access`, transition rows, qualifiers) lead with type/kind metadata — the most meaningful structural fact available at compile time.

4. **Runtime metadata (Kramer B2):** Removed all evaluator/runtime pipeline source claims. Added explicit "V1 is compile-time only" section. Listed everything NOT available in V1. All templates now use only `Compilation` snapshot data.

### Notes Addressed

| Source | Note | Resolution |
|--------|------|------------|
| Frank N1 | Ensure anchor types | Four distinct scope-line patterns: residency/entry gate/exit gate/event args with examples |
| Frank N2 | Computed fields | New §1b with "Computed from:" line, suppressed writable map |
| Frank N3 | Omit declarations | New §7b covering structural absence semantics |
| Frank N4 | State modifiers | Added modifiers line + `required` state example |
| Frank N5 | Initial event | Added `initial` event example showing constructor semantics |
| Frank N6 | Typical effects expensive | Explicitly deferred to V2 |
| Kramer N1 | Field cost | Annotated data sources with cost levels |
| Kramer N2 | State cost | Annotated; noted terminal-reachable is indirect |
| Kramer N3 | Event effects high | Marked as V2 explicitly |
| Kramer N4 | Transition prose gaps | V1 shows count+category; V2 for natural text |
| Kramer N5 | Access limitations | Noted guarded-access write maps not materialized |
| Kramer N6 | Reject ordering | Removed "selected when" line from V1; deferred |
| Kramer N7 | Qualifier usage index | "Applied to X, Y" deferred to V2 |
| Kramer N8 | No tables in hover | Added VS Code constraints section; no tables in templates |
| Kramer N9 | Referenced fields bonus | Added to rule and ensure templates |
| Kramer N10 | ConstraintInfluenceEntry | Corrected; explicit notes to use this, not SemanticSubjects |

---

## V1 vs V2 Boundary

### V1 (compile-time, ship now)
- All 11 construct templates (field stored, field computed, rule, state, event, transition, ensure ×4 anchors, access, omit, reject, qualifier)
- Status badges from ProofLedger
- Referenced fields/args from ConstraintInfluenceEntry
- State modifiers, initial event marker
- Cost-annotated data sources for implementation planning

### V2 (deferred)
- Event "typical effects" summary
- Prose proof-gap text
- Qualifier "applied to" cross-references
- Event-driven mutation reach on fields
- Reject row ordering context
- Guarded access final write maps
- Runtime preview integration

---

## Design Calls Beyond Explicit Feedback

1. **Removed the "Field hover scope" open question** — resolved by deferring event-driven mutation reach to V2 per Kramer's cost assessment.
2. **Transition row proof gap in V1** shows obligation count + diagnostic category (e.g., "1 unresolved obligation (qualifier arithmetic)") rather than prose — a middle ground between the v2 aspiration and "nothing."
3. **Omit template** shows "Restored on transition to:" line — a design call to show the positive framing (where the field comes back) rather than negative (where it's missing).

---

## Status

Ready for Shane sign-off. No implementation should begin until approved.
