# Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs derive behavior from metadata and shape rather than hardcoded enum identity or parallel lists.
- Public API surfaces must expose stable CLR/JSON interchange types; evaluator internals stay internal and never leak into the durable surface.
- Operation legality lives in `Language.Operations`; computation lives in `TypeRuntime` plus the runtime dispatch registry. The evaluator stays zero-knowledge.
- Identity-type work follows the dual-shape rule: enriched internal entities when metadata/lifetime demands it, lightweight API-boundary code/value shapes when callers need stable interchange.
- Collection internals are settled around universal `PreceptValue[]` backing, stride-2 pair storage, static `CollectionActions` helpers, and evaluator-owned copy-on-write.
- Collection CLR adapters are lazy at the `Version` level and eager on first materialization, not per-index lazy.
- Working docs drift quickly during heavy deliberation; canonical docs and squad records must be synchronized as soon as a decision locks.
- Investigation docs may be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Diagnostic policy follows the philosophy's "proven violations only" rule: per-state event coverage gaps are design choices, but zero-handler events across all states justify `UnhandledEvent`.
- When a working proposal becomes canonical, update every downstream contract in one pass and repoint CC references to canonical homes before archiving the proposal.
- `GraphState` is a derived-facts output record, never a source-model mirror; booleans are the right shape when the question is structural.
- `SlotContext` and `ConstructSlotKind` are different concepts; mapping between them is legitimate, aliasing them is not.
- Default-valued field additions on `readonly record struct` contracts are acceptable when they preserve existing call sites and the new data is structurally optional.

## Recent Updates

### 2026-05-07 — Wave 3 Round 1: canonical doc sweep recorded

- Closed 13 Wave 3 Round 1 markers across `docs/compiler/type-checker.md`, `docs/compiler/proof-engine.md`, and `docs/runtime/precept-builder.md`.
- `type-checker.md`: CC#9 `ConstraintIdentity` DU, CC#11 `RejectReason`, and the stale CC#1-era expression-tree note are now closed.
- `proof-engine.md`: catalog-gap #12 and #13 are closed, CC#1 / CC#5 follow-through notes are complete, and the stale initial-state OQ block is gone.
- `precept-builder.md`: CC#4 `Compilation.Tokens`, CC#11 `ExecutionRow.RejectReason`, and CC#7 `ConstraintMeta.StateAnchored` hierarchy documentation are now canonical.
- Validation remains unchanged: `dotnet build src/Precept/Precept.csproj` reports only the 3 pre-existing `SemanticIndex.cs` errors.

### 2026-05-06 — Wave 2 cross-cutting decisions fully closed

- Closed all 11 Wave 2 team-autonomous items: CC#5, CC#10, CC#13, CC#14, CC#15, CC#16, CC#17, CC#18, CC#19, CC#20, and CC#22.
- Corrected stale Wave 1 checkbox drift for CC#3, CC#4, CC#6, CC#12, CC#23, and CC#24, plus the CC#26 status row, where the status table was already authoritative.
- Propagated the locked rulings through `cross-cutting-decisions.md`, `catalog-system.md`, `graph-analyzer.md`, `evaluator.md`, `diagnostic-system.md`, `language-server.md`, `type-checker.md`, and `proof-engine.md` with a clean build reported.

### 2026-05-06 — Wave 3 Round 1: canonical doc sweep

Swept three docs to close all open question markers by propagating the locked CC decisions.

**type-checker.md:** `ConstraintFieldRefs.ConstraintIdentity` changed from `object` to `ConstraintIdentity` DU (CC#9). `string? RejectReason` added to `TypedTransitionRow` (CC#11). Stale §14 "No expression tree parsing" bullet removed (contradicted CC#1 resolution already in the doc).

**proof-engine.md:** Five OQ blocks closed — `TryLiteralProof` scope (intentional, Strategy 1 = numeric only); Strategy 3 vs Strategy 4 boundary (explicitly specified — direct subject guard vs. relational guard); initial-state satisfiability blocking note corrected (CC#1 resolved design, remaining dependency = TC implementation); corresponding stale OQ block replaced with implementation note; `FieldModifierMeta.ProofDischarges` stale OQ removed (CC#5 canonical in catalog-system.md).

**precept-builder.md:** `TokenStream Tokens` added to `Compilation` code block (CC#4). `string? RejectReason` added to `ExecutionRow` code block (CC#11). `ConstraintMeta` DU hierarchy with `StateAnchored` abstract intermediate node documented after 5-way routing switch (CC#7).

Pattern: When a doc has a code block and a prose "pending" note about the same field, fix both in one edit — the code block and the prose must be consistent. When an OQ block is stale relative to an earlier resolved note in the same doc, remove it — don't leave contradictory annotations.

### 2026-05-06 — Wave 2 cross-cutting decisions: all 11 closed

- Frank-156's UX accuracy review fed directly into the same-day Elaine correction pass; the dead zero-arg `Possible` state and the undefined-event rendering error are durably closed.
- Frank-157-1's fit assessment became the acceptance bar for CC#8: once OQ-2 and OQ-3 closed, the proposal was fit to adopt.
- Frank-158-1 applied those closures in `event-inspection-proposal.md`, resolved CC#8 in the cross-cutting register, and unblocked CC#12.
- Wave 1 facilitation opened with CC#7 first; keep the hierarchical `ConstraintMeta.StateAnchored` recommendation attached to that handoff until Shane rules.

### Historical summary through 2026-05-05

- 2026-05-04 established the execution/runtime baseline: catalogs describe legality, `TypeRuntime` plus runtime registries own computation, and the evaluator remains type-agnostic.
- Collection API design converged on CLR-friendly adapters and declared-direction storage while keeping internal storage on `PreceptValue[]`.
- Currency, unit, and dimension work converged on catalog-backed identity types with clear public/internal shape boundaries.
- Use `.squad/decisions.md` for full per-decision provenance; keep `history.md` focused on durable operating context and the newest closures.
