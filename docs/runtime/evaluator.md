# Evaluator

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Partial stub (`Evaluator.cs` exists; `Fail(FaultCode)` routes through `Faults.Create`; all operation bodies are stubs throwing `NotImplementedException`) |
| Source | `src/Precept/Runtime/Evaluator.cs` |
| Upstream | Precept (executable model), Version (current entity snapshot) |
| Downstream | `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, `EventInspection`, `UpdateInspection` |

---

## Overview

The evaluator is the plan executor. It consumes the executable model built by the Precept Builder and drives all four runtime operations: Create, Restore, Fire, Update. The evaluator never reasons about semantics — all semantic questions were resolved at build time. It walks prebuilt execution plans, looks up prebuilt constraint buckets, and produces structured outcomes.

---

## Responsibilities and Boundaries

**OWNS:** Plan execution (walking flat slot-addressed opcodes), constraint evaluation from prebuilt plan indexes, access-mode enforcement for Update, row dispatch for Fire, structured outcome production, fault backstop routing via `Faults.Create`.

**Does NOT OWN:** Semantic resolution, dispatch index building (Precept Builder), outcome type definitions (they are in `EventOutcome.cs`/`UpdateOutcome.cs`/`RestoreOutcome.cs`), inspection shape rendering.

---

## Right-Sizing

The evaluator is sized to execution only. No semantic reasoning, no catalog lookups at dispatch time, no re-analysis. All routing decisions were made by the Precept Builder. The evaluator's inner loop is: look up a prebuilt plan, walk its opcode array, evaluate constraints from prebuilt buckets, produce an outcome. This makes the evaluator the smallest it can be while being correct.

---

## Inputs and Outputs

**Input:**
- `Precept` — the executable model (descriptor tables, dispatch indexes, constraint plan indexes, execution plans, fault backstops)
- `Version` — current state (as `StateDescriptor?`) and field slot array
- Event/arg descriptors (for Fire), field patch (for Update)

**Output (commit):**
- `EventOutcome` — Fired | Rejected (with structured violations) | Faulted
- `UpdateOutcome` — Updated | Rejected | AccessDenied | Faulted
- `RestoreOutcome` — Restored | Rejected | Faulted

**Output (inspect):**
- `EventInspection` — preview of every declared transition row from current state
- `UpdateInspection` — preview of current field access + constraint evaluation + event-prospect
- `RowInspection` — detailed result for a single transition row

---

## Execution Model

The evaluator is a shared plan executor — all four operations route through the same constraint evaluation infrastructure. The key design insight: inspection and commit paths execute the SAME prebuilt plans; the only difference is disposition (report vs. enforce).

```
Fire(event, args):
  1. Look up transition rows from TransitionDispatchIndex[current state, event]
  2. For each matching row:
     a. Evaluate guard (if any) against slot array
     b. Evaluate constraint plans: always + from<current> + on<event> + to<target>
     c. If all constraints pass: collect as candidate row
  3. If zero candidates → Rejected (no matching row)
  4. If one candidate → execute action plan, produce new Version, return Fired
  5. If multiple candidates → Faulted (ambiguous dispatch — impossible path in valid program)
```

---

## Operation Dispatch and Constraint Mechanics

### Plan Execution

Opcodes are walked in sequence. Each opcode either reads from the slot array, loads a literal, performs a comparison or arithmetic operation, or stores to the slot array. No recursion; no dynamic dispatch. The execution plan for a transition row was precomputed by the Precept Builder from typed action chains.

### Constraint Evaluation

The evaluator reads prebuilt constraint buckets from `ConstraintPlanIndex`. For each constraint in the applicable bucket, it evaluates the constraint expression against the current (and projected) slot array. A failing constraint produces a structured `ConstraintViolation` carrying the failing expression text, evaluated field values, and guard context.

### Access-Mode Enforcement (Update)

Update checks `FieldDescriptor.AccessMode` for the current state before applying the patch. A field with `readonly` access in the current state produces an `AccessDenied` violation, not a constraint violation.

### Fault Backstop Routing

The evaluator calls `Faults.Create(FaultSiteDescriptor, context)` for every impossible-path backstop site. The `FaultSiteDescriptor` is read from the `Precept` executable model — it was planted by the Precept Builder. No backstop call is allowed without a descriptor.

### Restore with Recomputation-First

Restore recomputes computed fields BEFORE constraint evaluation. This catches stale values in persisted snapshots — a snapshot with a computed field value that differs from what the constraint expression would compute is caught at restore time.

---

## Dependencies and Integration Points

- **Precept** (upstream): executable model — all prebuilt structures
- **Version** (upstream): current state descriptor + field slot array
- **EventOutcome / UpdateOutcome / RestoreOutcome** (downstream): structured outcome types
- **EventInspection / UpdateInspection / RowInspection** (downstream): inspection types
- **MCP tools** (downstream): `precept_fire`, `precept_update`, `precept_inspect` all invoke the evaluator

---

## Failure Modes and Recovery

All failures are classified. In-domain failures (constraint violations, access denials, no matching row) produce structured `Rejected` outcomes — not exceptions. Impossible-path failures (ambiguous dispatch, invariant breaches) produce `Fault` via `Faults.Create(FaultSiteDescriptor, ...)`. No unclassified exceptions are thrown from the evaluator surface.

---

## Contracts and Guarantees

- A valid `Precept` with a valid `Version` never produces `Faulted` outcomes for in-domain operations.
- Inspection and commit paths execute the same constraint plans — inspection cannot disagree with commit.
- Restore with a valid persisted snapshot produces `Restored` or `Rejected` — never `Faulted`.
- Every `Fault` produced by the evaluator carries a resolved `FaultSiteDescriptor`.

---

## Design Rationale and Decisions

The "plan executor" identity is the core design decision: the evaluator has no knowledge of the language, catalogs, or semantic rules. All language knowledge was baked into the executable model by the Precept Builder. This makes the evaluator the simplest correct implementation — a tight inner loop with no conditional branching on language features.

---

## Innovation

- **Plan execution, not tree-walking:** The evaluator walks a flat opcode array, not a recursive AST. This makes evaluation predictable-time, cache-friendly, and trivially inspectable.
- **Inspection shares commit plans:** No second evaluator, no separate code paths. The inspection API previews every transition with full constraint evaluation before committing.
- **Structured outcomes taxonomy:** No exceptions, no error codes, no untyped failures. Every operation result is pattern-matchable.
- **Causal violation explanations:** `ConstraintViolation` carries structured explanation depth — failing expression, evaluated field values, guard context, failing sub-expression.
- **Restore with recomputation-first constraint evaluation:** Restore recomputes computed fields BEFORE constraint evaluation, catching stale values.

---

## Constraint Evaluation Matrix

Every operation evaluates constraints through the same prebuilt plan indexes. Access-mode checks and row dispatch are independent of constraint evaluation.

| Operation | Access-mode checks | Row dispatch | Constraint plans evaluated |
|---|---|---|---|
| `Fire` | no | yes | `always`, `from <current>`, `on <event>`, `to <target>` |
| `InspectFire` | no | yes | same as `Fire` |
| `Update` | yes | no | `always`, `in <current>` |
| `InspectUpdate` | yes | no | same as `Update`, plus event-prospect evaluation over hypothetical state |
| `Create` with initial event | no | yes (initial event) | `always`, plus initial-event fire-path plans |
| `Create` without initial event | no | no | `always`, in `<initial>` |
| `Restore` | no | no | `always`, in `<current>` — computed fields recomputed first |

**Key rule:** Restore bypasses access-mode checks and row dispatch but does NOT bypass constraint evaluation.
**Key rule:** Inspection and commit paths execute the same prebuilt plans — disposition alone differs (report vs. enforce).

---

## Open Questions / Implementation Notes

1. All operation bodies throw `NotImplementedException` — `Fail(FaultCode)` routing to `Faults.Create` is the only implemented path.
2. `ConstraintViolation` full shape is not yet implemented: currently only `ConstraintDescriptor` + `IReadOnlyList<string> FieldNames`. Full shape needs evaluated field values (`{field: value}` pairs), guard context (if guarded), and failing sub-expression. This is the designed contract; implementation is blocked on the evaluator.
3. Define the opcode execution model: how are scratch slots allocated? Is the slot array on the stack or heap-allocated per operation?
4. Confirm Restore recomputation-first ordering: is this a hard constraint in the evaluator, or enforced by the execution plan shape?
5. `InspectUpdate` event-prospect evaluation: confirm this runs Fire inspection over hypothetical post-patch state — does it require a temporary `Version` or a different code path?
6. Confirm fault backstop threading: every backstop site must carry a resolved `FaultSiteDescriptor` from the `Precept` model linked to its `FaultCode`.

---

## Deliberate Exclusions

- **No semantic reasoning at dispatch time:** All routing decisions are pre-built. The evaluator reads indexes, not catalogs.
- **No general expression interpreter:** The evaluator executes prebuilt opcode plans, not a general expression AST.
- **No plan building:** `ExecutionPlan` construction is the Precept Builder's domain.

---

## Cross-References

| Topic | Document |
|---|---|
| Full runtime surface design | `docs/compiler-and-runtime-design.md §11` |
| Precept Builder that builds the executable model | `docs/runtime/precept-builder.md` |
| Descriptor types used in outcomes and inspections | `docs/runtime/descriptor-types.md` |
| Outcome and inspection type shapes | `docs/runtime/result-types.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Evaluator.cs` | Evaluator implementation — all four operation bodies |
| `src/Precept/Runtime/EventOutcome.cs` | Fire and Create outcome types |
| `src/Precept/Runtime/UpdateOutcome.cs` | Update outcome types |
| `src/Precept/Runtime/RestoreOutcome.cs` | Restore outcome types |
| `src/Precept/Runtime/Inspection.cs` | Inspection types (`EventInspection`, `UpdateInspection`, `RowInspection`) |
| `src/Precept/Runtime/SharedTypes.cs` | `ConstraintViolation`, `ConstraintResult` |
