# Catalog Gap Register Migration — Completed

**Date:** 2026-05-04
**By:** Frank (Lead/Architect)
**Status:** Complete — migration committed in 2715872

## Summary

All 43 entries from `docs/working/catalog-gap-register.md` have been processed and the source file archived to `docs/working/Archived/catalog-gap-register-migrated.md`.

## Migration Breakdown

| Status | Count |
|--------|-------|
| Pending Decision → migrated as Open Question blocks | 23 |
| Already Captured in catalog-system.md | 5 |
| Resolved in Source (docs stale, corrected) | 3 |
| Captured in cross-cutting-decisions.md | 4 |
| Out of Scope (API/tooling, not catalog metadata) | 4 |
| **Total** | **43** |

## Destination Docs (Open Question blocks placed)

The 23 Pending Decision gaps were distributed across 9 canonical docs:

| Doc | Gaps received |
|-----|--------------|
| `docs/compiler/parser.md` | #6, #7, #8 (SlotValue count mismatch, shape conflicts, disambiguation offset) |
| `docs/compiler/graph-analyzer.md` | #9 (GraphState modifier representation) |
| `docs/compiler/proof-engine.md` | #11, #13 (FaultSiteLink binding, Strategy 3/4 boundary) |
| `docs/compiler/type-checker.md` | #15, #16 (ConstraintFieldRefs type, SemanticIndex reference collections) |
| `docs/runtime/evaluator.md` | #20, #21 (ExecutionRow.RejectReason, Faulted(Fault) outcome variant) |
| `docs/compiler/diagnostic-system.md` | #39 (AdditionalLocations for multi-span diagnostics) |
| `docs/tooling/language-server.md` | #30, #31, #33, #34, #35 (TypeMeta.IsUserFacing, TypedArg.EventName, EventInspection shape, ConstructMeta.IsOutlineNode, hover strategy) |
| `docs/compiler/tooling-surface.md` | #37, #40 (TextMate pattern representation, grammar input catalog coverage) |
| `docs/language/catalog-system.md` | #41, #42, #43 (SemanticTokenModifiers, TypeAccessor DU hierarchy, ActionMeta missing properties) |

## Already-Resolved Gaps

Three gaps were found already implemented in source code but undocumented:
- **#17** `ActionMeta.SyntaxShape` — exists in `Action.cs`
- **#18** `FunctionMeta.HasCIVariant` — exists in `Function.cs`
- **#24** `ModifierMeta.ModifierCategory` — exists in `Modifier.cs` as `Category`

These were marked Resolved-in-Source and the affected stale open-question bullets removed from canonical docs.

## Gaps Confirmed Already Captured

Five gaps were already present as Open Question blocks in `docs/language/catalog-system.md` before migration (#1, #2, #3, #4, #5). No action needed; confirmed in-place.

## Out-of-Scope Entries Noted

Four entries (#12, #25, #27, #36) were API design or tooling implementation questions not constituting catalog metadata gaps. They were noted as Out of Scope in the register and not migrated to canonical docs.

## Cross-Cutting Entries

Eight entries were already tracked in `docs/working/cross-cutting-decisions.md` (#10, #14, #19, #26, #28, #29, #32, #38). Source attributions added to the register; no new migration needed.
