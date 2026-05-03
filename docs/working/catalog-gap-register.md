# Catalog Gap Register

> Single source of truth for all known catalog metadata gaps.
> Gaps are promoted from here into `docs/language/catalog-system.md` § Open Questions once decided.

## How to Use This Register

A **gap** is a place where a design doc references catalog metadata that doesn't exist, or where implementation would benefit from catalog-driven metadata that isn't yet defined. Gaps move through this lifecycle:

1. **Pending Decision** — identified but needs owner ruling before catalog addition
2. **Decided** — owner has ruled; implementation can proceed
3. **Promoted** — implemented and documented in `docs/language/catalog-system.md`

Items marked **Already Captured** are already in catalog-system.md § Open Questions. Items marked **Resolved in Source** are implemented but catalog-system.md docs are stale. Items marked **Out of Scope** are API design, naming, or tooling questions — not catalog metadata gaps.

## Status Key

| Status | Meaning |
|--------|---------|
| Already Captured | In catalog-system.md § Open Questions |
| Resolved in Source | Already implemented — catalog-system.md docs just stale |
| Pending Decision | Needs owner ruling before catalog addition |
| Out of Scope | API/naming/tooling — not a catalog metadata gap |

## Gap Register

| # | Gap | Priority | Status | Source Doc | Section | Notes |
|---|-----|----------|--------|------------|---------|-------|
| 1 | `FieldModifierMeta.ProofDischarges` | High | Already Captured | catalog-system.md | §Open Questions | Modifier-to-proof-obligation mapping for Strategy 2 |
| 2 | `ConstructMeta.ModelContribution` | Medium | Already Captured | catalog-system.md | §Open Questions | Builder dispatch on construct contribution type |
| 3 | `FieldDescriptor.AccessModes` | High | Already Captured | catalog-system.md | §Open Questions | O(1) access mode lookup by state |
| 4 | `FaultCode.AmbiguousDispatch` | Medium | Already Captured | catalog-system.md | §Open Questions | Multiple transition rows matched fault |
| 5 | `TokenMeta.HoverDescription` / `SnippetTemplate` | Medium | Already Captured | catalog-system.md | §Open Questions | Documentation strings for hover/completions |
| 6 | SlotValue Subtype Count Mismatch | Medium | Pending Decision | parser.md | line 36 | 15 vs 17 `ConstructSlotKind` members — reconcile inventory |
| 7 | SlotValue Subtype Shape Conflicts | High | Pending Decision | parser.md | line 58 | 4 slots have conflicting shapes across docs — blocking |
| 8 | `DisambiguationEntry.Offset` | Low | Pending Decision | parser.md | line 185 | peek(offset) value may vary per construct |
| 9 | GraphState Modifier Representation | Medium | Pending Decision | graph-analyzer.md | line 120 | `ImmutableArray<ModifierKind>` vs explicit booleans |
| 10 | Event "Optional" Modifier | Low | Out of Scope | graph-analyzer.md | line 407 | Language surface change, not catalog metadata |
| 11 | `FaultSiteLink.Site` to `FaultSiteDescriptor` Binding | Medium | Pending Decision | proof-engine.md | line 222 | Proof-to-runtime bridge transformation |
| 12 | `TryLiteralProof` Requirement Coverage | Low | Out of Scope | proof-engine.md | line 409 | Strategy scope question, not catalog metadata |
| 13 | Strategy 3 vs Strategy 4 Boundary | Medium | Pending Decision | proof-engine.md | line 568 | guard-in-path vs flow-narrowing boundary unclear |
| 14 | Expression Tree Design Blocking | High | Out of Scope | proof-engine.md | line 976 | Cross-doc tracking gap, not catalog metadata |
| 15 | `ConstraintFieldRefs.ConstraintIdentity` Type | Medium | Pending Decision | type-checker.md | line 528 | `object` vs typed DU alignment |
| 16 | `SemanticIndex` Reference-Tracking Collections | High | Pending Decision | type-checker.md | line 562 | Missing `References`, `FieldReferences`, etc. — tooling blocked |
| 17 | `ActionMeta.SyntaxShape` | Medium | Resolved in Source | type-checker.md | line 767 | Already in `Action.cs` — docs stale |
| 18 | `FunctionMeta.HasCIVariant` | Low | Resolved in Source | type-checker.md | line 825 | Already in `Function.cs` — docs stale |
| 19 | `Compilation.Tokens` Field | Medium | Out of Scope | precept-builder.md | line 116 | Core type shape, not catalog metadata |
| 20 | `ExecutionRow.RejectReason` Field | Medium | Pending Decision | evaluator.md | line 434 | `because` clause storage for reject transitions |
| 21 | `Faulted(Fault)` as `EventOutcome` Variant | Medium | Pending Decision | evaluator.md | line 885 | Outcome type hierarchy gap |
| 22 | `ConstraintMeta` DU Subtype Count | Medium | Pending Decision | precept-builder.md | line 608 | 4 vs 5 subtypes — shape mismatch |
| 23 | `FaultSiteDescriptor` Planting Mechanism | Medium | Pending Decision | precept-builder.md | line 467 | How evaluator locates fault sites in rows |
| 24 | `ModifierMeta.ModifierCategory` | Low | Resolved in Source | mcp.md | line 280 | Already in `Modifier.cs` — docs stale |
| 25 | `FirePipeline` Catalog | Low | Out of Scope | mcp.md | line 378 | MCP output design, not catalog metadata |
| 26 | `SemanticIndex.EnsuresByState` | Low | Out of Scope | mcp.md | line 443 | Convenience index, not catalog metadata |
| 27 | `precept_inspect` Composite View Exemption | Low | Out of Scope | mcp.md | line 509 | API design, not catalog metadata |
| 28 | `EventOutcome.mutations` Payload | Low | Out of Scope | mcp.md | line 629 | Runtime-to-MCP interface, not catalog metadata |
| 29 | Unmatched Guard Trace Enrichment | Low | Out of Scope | mcp.md | line 684 | Diagnostic enrichment, not catalog metadata |
| 30 | `TypeMeta.IsUserFacing` | Low | Pending Decision | language-server.md | line 417 | Completions use but not in catalog shape |
| 31 | `TypedArg.EventName` Back-Reference | Low | Pending Decision | language-server.md | line 543 | Navigation for hover on event args |
| 32 | `precept/inspect` vs `precept/preview` Naming | Low | Out of Scope | language-server.md | line 610 | Doc naming sync, not catalog metadata |
| 33 | `EventInspection` Shape Disagreement | Medium | Pending Decision | language-server.md | line 667 | Cross-doc shape mismatch |
| 34 | `ConstructMeta.IsOutlineNode` / `LspSymbolKind` | Medium | Pending Decision | language-server.md | line 702 | Tooling-facing catalog properties for outline |
| 35 | `TokenMeta.HoverDescription` (broader strategy) | Medium | Pending Decision | language-server.md | line 1356 | Documentation string strategy beyond partial capture |
| 36 | Grammar Generator Implementation Path | Medium | Out of Scope | tooling-surface.md | line 376 | Implementation tooling, not catalog metadata |
| 37 | Complex TextMate Pattern Representation | Medium | Pending Decision | tooling-surface.md | line 1035 | Multi-line/nested scope metadata in catalog |
| 38 | `SlotContext` vs `SlotKind` Enum Naming | Low | Out of Scope | tooling-surface.md | line 512 | Doc naming inconsistency, not catalog metadata |
| 39 | Diagnostic Related Locations | Low | Pending Decision | diagnostic-system.md | line 540 | `AdditionalLocations` for multi-span diagnostics |
| 40 | Grammar input catalog coverage | Medium | Pending Decision | tooling-surface.md | line ~144 | Grammar input catalog list may be incomplete; confirm whether `Modifiers.All` and `Actions.All` feed grammar through `Token` references |

## Summary by Status

| Status | Count |
|--------|-------|
| Already Captured | 5 |
| Resolved in Source | 3 |
| Pending Decision | 20 |
| Out of Scope | 12 |
| **Total** | **40** |

## High-Priority Gaps (Blocking)

These require owner decision before dependent implementation can proceed:

### #7 — SlotValue Subtype Shape Conflicts (Pending Decision)

Four slot subtypes have conflicting shapes between parser.md and type-checker.md: `TypeExpressionSlot`, `ModifierListSlot`, `AccessModeSlot`, `BecauseClauseSlot`. The parser and type checker cannot align until canonical shapes are resolved. **Decision needed:** Which document's shapes are authoritative?

### #16 — SemanticIndex Reference-Tracking Collections (Pending Decision)

`SemanticIndex` lacks `References`, `FieldReferences`, `StateReferences`, `EventReferences` collections that tooling-surface.md and language-server.md require for semantic token Pass 2. **Decision needed:** Add reference-site arrays to `SemanticIndex`, or define an alternative mechanism?

### #1 — FieldModifierMeta.ProofDischarges (Already Captured)

Already in catalog-system.md § Open Questions with proposed shape. Blocks proof engine Strategy 2 implementation. **Status:** Designed, awaiting implementation.

### #3 — FieldDescriptor.AccessModes (Already Captured)

Already in catalog-system.md § Open Questions with proposed shape. Enables O(1) access mode lookup in evaluator. **Status:** Designed, awaiting implementation.

## Resolved / Already Done

The following items are already implemented in source code. Update `docs/language/catalog-system.md` to remove them from Open Questions or mark as implemented:

| # | Gap | Source File | Action |
|---|-----|-------------|--------|
| 17 | `ActionMeta.SyntaxShape` | `src/Precept/Language/Action.cs` | Remove from Open Questions — already has `SyntaxShape` property |
| 18 | `FunctionMeta.HasCIVariant` | `src/Precept/Language/Function.cs` | Remove from Open Questions — already has `HasCIVariant` and `CIVariantOf` |
| 24 | `ModifierMeta.ModifierCategory` | `src/Precept/Language/Modifier.cs` | Remove from Open Questions — already has `Category` property of type `ModifierCategory` |
