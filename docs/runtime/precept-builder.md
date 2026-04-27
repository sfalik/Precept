# Precept Builder

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Partial stub (error guard implemented; transformation logic not implemented; `Precept.From` throws `NotImplementedException` after `HasErrors` check) |
| Source | `src/Precept/Runtime/Precept.cs` (Precept.From factory) |
| Upstream | Compilation (when `!HasErrors`) |
| Downstream | All runtime operations (Create, Fire, Update, Restore, Inspect), LS preview, MCP runtime tools |

---

## Overview

The Precept Builder transforms analysis knowledge into the executable model (`Precept`). It is the compile-to-runtime boundary — the one-way transformation from semantic artifacts to dispatch-optimized execution structures. `Precept.From(Compilation)` is the sole entry point. The builder restructures, not renames: the runtime model is organized for execution, not for analysis.

---

## Responsibilities and Boundaries

**OWNS:** Descriptor table construction from `SemanticIndex` declarations, dispatch index building from `StateGraph`, execution plan compilation from typed action chains, constraint plan index organization by activation anchor, reachability index construction, `FaultSiteDescriptor` planting from `ProofLedger` links, `ConstraintInfluenceMap` construction.

**Does NOT OWN:** Semantic analysis (TypeChecker), graph analysis (GraphAnalyzer), proof discharge (ProofEngine), operation execution (Evaluator).

---

## Right-Sizing

The Precept Builder is the transformation boundary. It is the only code path that builds the runtime model — `Precept.From` is the sole factory. This means all runtime execution is driven by prebuilt structures; the evaluator never re-derives semantic meaning at dispatch time. The builder's scope ends at building; execution is the evaluator's domain.

---

## Inputs and Outputs

**Input:**
- `Compilation` (only when `!HasErrors`) — carries `SemanticIndex`, `StateGraph`, `ProofLedger`

**Output:**
- `Precept` — sealed executable model containing:
  - `FieldDescriptor`/`StateDescriptor`/`EventDescriptor`/`ArgDescriptor`/`ConstraintDescriptor`/`FaultSiteDescriptor` tables
  - `TransitionDispatchIndex`
  - `ConstraintPlanIndex` (five anchor families: `always`, `in`, `from`, `to`, `on`)
  - `SlotLayout`
  - `ReachabilityIndex`
  - `ConstraintInfluenceMap`
  - Execution plans (flat slot-addressed opcodes)

---

## Architecture

Sequential transformation passes — each pass depends on the output of the previous:

1. **Descriptor pass** — build `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor` tables from `SemanticIndex` declarations; assign slot indexes
2. **Slot layout pass** — produce compact `SlotLayout` from field declarations; computed fields get slots
3. **Dispatch index pass** — build `TransitionDispatchIndex` and `ReachabilityIndex` from `StateGraph` topology
4. **Constraint plan pass** — organize `ConstraintDescriptor` entries by activation anchor into precomputed `ConstraintPlanIndex` buckets
5. **Execution plan pass** — compile typed action chains into flat slot-addressed opcode arrays
6. **Fault backstop pass** — read `ProofLedger.FaultSiteLinks` and plant `FaultSiteDescriptor` entries at each site

---

## Component Mechanics

### Descriptor Construction

Descriptors are produced from `SemanticIndex` typed declarations — `TypedField` → `FieldDescriptor`, `TypedState` → `StateDescriptor`, etc. Each descriptor carries a `SlotIndex` (for fields), resolved `ModifierKind[]`, and `SourceLine` for diagnostics. Descriptors are immutable sealed records with no behavior.

### Slot Layout

Fields are assigned sequential slot indexes in declaration order. Computed fields get slots (updated by the evaluator before constraint evaluation). The `SlotLayout` is a compact register file — the evaluator's working memory for an entity instance.

### Constraint Plan Index

Constraints are pre-sorted into five anchor-family buckets: `always` (every operation), `in <state>` (state-scoped), `from <state>` (before leaving a state), `to <state>` (on entering a state), `on <event>` (event-scoped). The evaluator never scans or filters at dispatch time — it reads the precomputed bucket for the current operation context.

### Execution Plan Compilation

Typed expressions and action chains from the `SemanticIndex` are compiled into flat opcode arrays. Opcodes reference field slots by index, not by name. The execution plan for a transition row is: load args → execute action chain opcodes → (constraint evaluation is separate, via plan index).

### Fault Site Backstops

For each `FaultSiteLink` in the `ProofLedger`, the builder plants a `FaultSiteDescriptor` carrying the `FaultCode` and `DiagnosticCode` (derived from `[StaticallyPreventable]`). These backstops exist in the executable model as defense-in-depth — a correct program never reaches them.

---

## Dependencies and Integration Points

- **Compilation** (upstream): carries all three analysis artifacts
- **SemanticIndex** (via Compilation): typed declaration inventories → descriptor tables and execution plans
- **StateGraph** (via Compilation): topology and structural facts → dispatch indexes and reachability index
- **ProofLedger** (via Compilation): FaultSiteLinks → FaultSiteDescriptor backstops; ConstraintInfluenceEntries → ConstraintInfluenceMap
- **Evaluator** (downstream): consumes the complete `Precept` executable model
- **LS preview** (downstream): reads `Precept` for inspect/preview operations when `!HasErrors`
- **MCP runtime tools** (downstream): `precept_inspect`, `precept_fire`, `precept_update` all require a built `Precept`

---

## Failure Modes and Recovery

`Precept.From` guards on `Compilation.HasErrors` — any analysis-phase error prevents builder execution. There is no partial build; the builder either produces a complete `Precept` or throws. This is by design: an executable model from a broken program would be unsafe to execute.

---

## Contracts and Guarantees

- `Precept.From(compilation)` returns a complete, immutable `Precept` or throws.
- Every `FieldDescriptor` has a unique `SlotIndex` within `[0, FieldCount)`.
- Every `TransitionDispatchIndex` entry corresponds to a `TypedTransitionRow` in `SemanticIndex`.
- Every `FaultSiteDescriptor` has a `FaultCode` with a `[StaticallyPreventable(DiagnosticCode)]` attribute.

---

## Design Rationale and Decisions

**Key design constraint:** The builder must NOT map `SemanticIndex` types 1:1 to runtime types. The runtime model is organized for execution: constraint plans are grouped by activation anchor (not declaration order); action plans are grouped by transition row (not field); the slot layout is a compact register file. An implementer who copies `SemanticIndex` shapes to the runtime model is violating this constraint.

---

## Innovation

- **Dispatch-optimized constraint indexes:** Constraints are grouped by activation anchor into precomputed buckets — the evaluator never scans or filters at dispatch time.
- **Flat evaluation plans:** Expressions are precomputed into flat, cache-friendly slot-addressed opcodes. The evaluator walks the plan array — no recursive dispatch, no semantic reasoning at runtime.
- **`ConstraintInfluenceMap` as a built artifact:** The dependency from constraints to contributing fields becomes a first-class runtime artifact, enabling AI agents to reason causally about constraint satisfaction.

---

## Open Questions / Implementation Notes

1. `Precept.From` throws `NotImplementedException` after the `HasErrors` guard — transformation logic not implemented.
2. Define the execution plan opcode set: `LOAD_ARG`, `LOAD_SLOT`, `LOAD_LIT`, `STORE_SLOT`, `CMP_GT`, `CMP_EQ`, etc. — confirm the full opcode inventory before implementing the plan compiler.
3. Define the slot layout algorithm: field declaration order → slot index? Or compiler-assigned? How are computed fields handled?
4. Confirm `ConstraintInfluenceMap` is built by the Precept Builder (from `ProofLedger.ConstraintInfluenceEntries`) or by the ProofEngine.
5. Descriptor types (`FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `FaultSiteDescriptor`) are planned but not yet implemented — see `docs/runtime/descriptor-types.md`.
6. Define how stateless precepts are handled in the builder: no `StateDescriptor` for states; `EventAvailabilityIndex` keyed on null `StateDescriptor`?

---

## Deliberate Exclusions

- **No semantic re-analysis:** The builder consumes already-resolved semantic facts, never re-reads catalogs for classification.
- **No evaluation:** Execution is the evaluator's domain.
- **No incremental rebuild:** The builder runs once per `Compilation`, producing an immutable `Precept`.

---

## Cross-References

| Topic | Document |
|---|---|
| Precept executable model design | `docs/compiler-and-runtime-design.md §10` |
| ProofLedger input (FaultSiteLinks, ConstraintInfluenceEntries) | `docs/compiler/proof-engine.md` |
| StateGraph input (topology, derived facts) | `docs/compiler/graph-analyzer.md` |
| Evaluator that consumes the executable model | `docs/runtime/evaluator.md` |
| Descriptor type shapes | `docs/runtime/descriptor-types.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Precept.cs` | `Precept.From` factory + structural query surfaces |
