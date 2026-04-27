# Result Type Taxonomy

## Status

| Property | Value |
|----------|-------|
| Doc maturity | Design |
| Implementation state | Pending — not yet implemented |
| Source | `src/Precept/Runtime/` |
| Upstream | `compiler-and-runtime-design.md`; `executable-model.md` (D8/R4) |
| Downstream | `fault-system.md`; `runtime-api.md` |

## Overview

The runtime uses three type families to represent operation results:

1. **EventOutcome** (7 variants) — returned by `Version.Fire`. One committed result.
2. **UpdateOutcome** (4 variants) — returned by `Version.Update`. One committed result.
3. **Inspection types** (EventInspection, UpdateInspection) — returned by `Version.InspectFire` and `Version.InspectUpdate`. Progressive annotated landscape.

Faults (evaluator-level errors the type checker should have prevented) throw `FaultException` — they are not in the outcome hierarchy. See `fault-system.md`.

## Responsibilities and Boundaries

**OWNS**
- The sealed result type hierarchies for commit operations: `EventOutcome` (7 variants) and `UpdateOutcome` (4 variants)
- The inspection type family: `EventInspection`, `UpdateInspection`, `RowInspection`
- Shared primitives: `FieldSnapshot`, `ConstraintResult`, `ArgInfo`
- The `Prospect` and `ConstraintStatus` enums and their propagation rules
- The `Version` API surface (`Fire`, `Update`, `InspectFire`, `InspectUpdate`)

**Does NOT OWN**
- Fault/exception handling — `FaultException` is defined in `fault-system.md`
- The executable model or evaluation pipeline implementation — see `executable-model.md`
- Constraint descriptors and catalog metadata — declared in the DSL catalog
- DSL parsing and type checking

## Right-Sizing

The result type families are scoped to exactly the operations the `Version` exposes: two commit operations (`Fire`, `Update`) and two inspection counterparts (`InspectFire`, `InspectUpdate`). Each commit operation returns its own sealed hierarchy so callers pattern-match exactly the variants their operation can produce. Inspection operations return an annotated landscape without committing state.

Faults — errors the type checker should have prevented, such as referencing an unknown field — are excluded from the outcome hierarchy and throw `FaultException` instead. This keeps outcome hierarchies to business-meaningful variants and avoids forcing every call site to handle error conditions that are programmer errors.

## Inputs and Outputs

| Direction | Type | Description |
|-----------|------|-------------|
| In | `string eventName` | Event to fire |
| In | `IReadOnlyDictionary<string, object?> args` | Event arguments for `Fire` / `InspectFire` |
| In | `IReadOnlyDictionary<string, object?> fields` | Field patch for `Update` / `InspectUpdate` |
| Out | `EventOutcome` | Sealed committed result of `Version.Fire` |
| Out | `UpdateOutcome` | Sealed committed result of `Version.Update` |
| Out | `EventInspection` | Annotated landscape from `Version.InspectFire` |
| Out | `UpdateInspection` | Annotated landscape from `Version.InspectUpdate` |

---

## EventOutcome

Sealed hierarchy. Callers pattern-match; any unhandled variant produces a compiler warning.

```csharp
public abstract record EventOutcome;
public sealed record Transitioned(Version Result) : EventOutcome;
public sealed record Applied(Version Result) : EventOutcome;
public sealed record Rejected(string Reason) : EventOutcome;
public sealed record InvalidArgs(string Reason) : EventOutcome;
public sealed record EventConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : EventOutcome;
public sealed record Unmatched() : EventOutcome;
public sealed record UndefinedEvent() : EventOutcome;
```

| Variant | Meaning | Pipeline stage | Stateless reachable? |
|---------|---------|----------------|---------------------|
| `Transitioned` | State change succeeded, new Version in target state | Stage 10 commit | No (no states) |
| `Applied` | No-transition row or stateless event succeeded, mutations committed | Stage 10 commit | Yes |
| `Rejected` | Authored `reject` row matched — business prohibition | Stage 4-5 (reject row) | Yes |
| `InvalidArgs` | Arg validation failure — wrong type, unknown key | Stage 2 (arg validation) | Yes |
| `EventConstraintsFailed` | Post-mutation constraints violated (rules, state ensures, event ensures) | Stage 9-10 | Yes |
| `Unmatched` | All guards failed (including `when` precondition) — no row matched | Stage 4-5 | Yes |
| `UndefinedEvent` | No transition rows or hooks for this event in current state | Stage 1 | Yes |

**`Rejected` vs `InvalidArgs`:** `Rejected` is a business decision authored in the precept (`-> reject "reason"`). `InvalidArgs` is a caller error — the args don't match the event's declared contract. Parallel with UpdateOutcome's `InvalidInput`. DDD's "business rejection vs. invalid input" distinction.

**`EventConstraintsFailed` scope:** Covers ALL post-fire constraints: global rules, state ensures (`in`/`to`/`from`), AND event ensures. The `Event` prefix disambiguates from `UpdateConstraintsFailed`, not scope-limits to event-level constraints.

---

## UpdateOutcome

```csharp
public abstract record UpdateOutcome;
public sealed record FieldWriteCommitted(Version Result) : UpdateOutcome;
public sealed record UpdateConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : UpdateOutcome;
public sealed record AccessDenied(string FieldName, FieldAccessMode ActualMode) : UpdateOutcome;
public sealed record InvalidInput(string Reason) : UpdateOutcome;
```

| Variant | Meaning | Pipeline stage |
|---------|---------|----------------|
| `FieldWriteCommitted` | Patch applied, constraints passed, new Version committed | Commit |
| `UpdateConstraintsFailed` | Patch applied to working copy but constraints violated | Rule/ensure evaluation |
| `AccessDenied` | Field is not `write`-accessible in current state | Access mode check |
| `InvalidInput` | Type mismatch or structurally invalid patch | Type check |

---

## Inspection Types

### Progressive Evaluation Model

The evaluator runs the full fire or update pipeline as far as available data allows, annotating each decision point with certainty:

- **Guards** with arg-dependent terms that cannot be resolved remain `Possible`.
- **Set assignments** referencing missing args produce `IsResolved = false` on the resulting FieldSnapshot.
- **Constraints** evaluated against an unresolvable working copy slot produce `ConstraintStatus.Unresolvable`.
- **Derived fields** depending on unresolvable inputs are transitively marked unresolvable.

### Enums

```csharp
// Row-level certainty in first-match routing
public enum Prospect { Certain, Possible, Impossible }

// Per-constraint evaluation result under partial information
public enum ConstraintStatus { Satisfied, Violated, Unresolvable }
```

**Prospect propagation rules (first-match):**
- A `Certain` row makes all subsequent rows `Impossible`.
- An `Impossible` row opens up the next row (may become Certain or Possible).
- A `Possible` row leaves subsequent rows at most `Possible`.

**Guard evaluation under partial args uses Kleene three-value logic:**

| Left | Op | Right | Result |
|------|----|-------|--------|
| false | and | Unknown | Impossible (short-circuit) |
| true | and | Unknown | Possible |
| true | or | Unknown | Certain (short-circuit) |
| false | or | Unknown | Possible |
| Unknown | not | — | Possible |

Where "Unknown" = the expression references an arg not yet provided.

### EventInspection

```csharp
public sealed record EventInspection(
    string EventName,
    Prospect OverallProspect,
    IReadOnlyList<ConstraintResult> EventEnsures,
    IReadOnlyList<RowInspection> Rows);
```

- `OverallProspect` — reduced from Rows: if any row is `Certain` or `Possible`, the event is enabled.
- `Rows` — empty list = undefined event (no rows/hooks for this event in current state).
- `EventEnsures` — arg-scoped ensures, individually annotated.

Returned by `Version.InspectFire(eventName, args?)`. Also nested inside `UpdateInspection.Events`.

### UpdateInspection

```csharp
public sealed record UpdateInspection(
    IReadOnlyList<FieldSnapshot> Fields,
    IReadOnlyList<ConstraintResult> Constraints,
    IReadOnlyList<EventInspection> Events,
    Version? HypotheticalResult);
```

- `Fields` — ALL non-omitted fields with post-patch values and recomputed derived fields.
- `Constraints` — rules evaluated against the hypothetical post-patch state.
- `Events` — full EventInspection for every event defined in the current state, evaluated against the hypothetical field state. Undefined events for other states are not included.
- `HypotheticalResult` — non-null when the patch is complete and all constraints pass.

Returned by `Version.InspectUpdate(fields?)`. When called with no patch, returns the landscape against current field values.

### RowInspection

```csharp
public sealed record RowInspection(
    Prospect Prospect,
    RowEffect Effect,
    IReadOnlyList<FieldSnapshot> ResultingFields,
    IReadOnlyList<ConstraintResult> Constraints,
    Version? HypotheticalResult);
```

- `ResultingFields` — ALL non-omitted fields in the TARGET state, post-mutation + recomputation. Fields whose access mode changes upon transition reflect the target state's mode.
- `HypotheticalResult` — non-null only when `Prospect == Certain` and the full pipeline resolves successfully.

### Row Effects

```csharp
public abstract record RowEffect;
public sealed record TransitionTo(string TargetState) : RowEffect;
public sealed record NoTransition() : RowEffect;
public sealed record Rejection(string Reason) : RowEffect;
```

### Shared Primitives

```csharp
public sealed record FieldSnapshot(
    string FieldName,
    FieldAccessMode Mode,
    string FieldType,
    bool IsResolved,
    object? Value);

public sealed record ConstraintResult(
    ConstraintDescriptor Constraint,
    IReadOnlyList<string> FieldNames,
    ConstraintStatus Status);

public sealed record ArgInfo(
    string Name,
    string Type);
```

**`FieldSnapshot.IsResolved`:** `false` when the post-mutation value could not be computed because a required arg dependency is missing. `Value` is meaningless when `IsResolved = false`. Prevents ambiguity between genuinely-null optional fields (`IsResolved = true, Value = null`) and stuck assignments.

**`ConstraintResult.Constraint`:** References the `ConstraintDescriptor` that was evaluated — callers can access kind, scope, anchor, guard, and `because` rationale through the descriptor. This is Tier 3 of the three-tier constraint exposure model (see `runtime-api.md` § Constraint Exposure Model).

**`ConstraintResult.FieldNames`:** Identifies which fields a constraint relates to. Enables per-field inline validation in the UI. Empty for entity-level constraints with no specific field attribution. Computed field references are transitively expanded to the stored fields the user can actually change.

---

## Version API Surface

```csharp
public sealed record Version
{
    public Precept Precept { get; }
    public string? State { get; }
    public object? this[string fieldName] { get; }
    public IReadOnlyList<FieldAccessInfo> FieldAccess { get; }
    public IReadOnlyList<string> AvailableEvents { get; }
    public IReadOnlyList<ArgInfo> RequiredArgs(string eventName);
    public IReadOnlyList<ConstraintDescriptor> ApplicableConstraints { get; }

    // Commit
    EventOutcome    Fire(string eventName, IReadOnlyDictionary<string, object?> args);
    UpdateOutcome   Update(IReadOnlyDictionary<string, object?> fields);

    // Inspect
    EventInspection   InspectFire(string eventName, IReadOnlyDictionary<string, object?>? args = null);
    UpdateInspection  InspectUpdate(IReadOnlyDictionary<string, object?>? fields = null);
}

public sealed record FieldAccessInfo(string FieldName, FieldAccessMode Mode, string FieldType, object? CurrentValue);
public enum FieldAccessMode { Read, Write }
```

**Four methods, consistent naming:** Every commit verb (`Fire`, `Update`) has an `Inspect___` counterpart. Fire/Update require complete input and return one committed outcome. InspectFire/InspectUpdate accept optional partial input and return an annotated landscape.

**Why InspectFire/InspectUpdate are separate:** Fire and Update are mutually exclusive commit operations — field patches and event args produce a new Version through fundamentally different pipelines (row matching + transition vs. access-mode check + constraint evaluation). A unified `Inspect(fields?, event?, args?)` would force callers to disentangle which combination was evaluated and would conflate two non-overlapping input/output shapes.

**Why Update, not Edit:** Renamed from `Edit` to avoid confusion with `edit` declarations in the DSL (per-state field access blocks). `Update` describes what the caller does to field values; `edit` describes what the precept author declares.

**Always full landscape:** Both inspection methods return the full annotated result. At DSL scale (10–50 fields, 5–15 events) the full pipeline is sub-millisecond. A lightweight mode can be added later without changing existing signatures.

---

## Design Rationale and Decisions

### Survey Grounding

| System | Relevant finding | How it informs R2 |
|--------|-----------------|-------------------|
| Temporal Workflow | `WorkflowResult<T>`, `NonDeterminismException` for system errors | Faults throw (not return). Business outcomes are typed results. |
| XState v5 | 4 snapshot statuses, `transition()` returns same `State` type | Preview/commit return type parity. Inspection returns same structural depth. |
| Rust `Result<T,E>` | Exhaustive match required by compiler | Sealed hierarchy with no catch-all — every variant handled. |
| F# discriminated unions | Exhaustive match, per-case payload | Same pattern in C# via sealed records. |
| K8s dry-run | Same response type as real operation, `managedFields` annotation | Commit and inspection share structural vocabulary. |
| Terraform plan | Progressive resolution — known values vs. `(known after apply)` | `FieldSnapshot.IsResolved` and `ConstraintStatus.Unresolvable` follow this pattern. |
| OPA `deny[msg]` | Collect-all violation set | ConstraintResult lists, not short-circuit. |
| DDD per-command results | Each command returns its own result type | Two families (EventOutcome, UpdateOutcome), not unified. |
| gRPC status codes | FAILED_PRECONDITION / INVALID_ARGUMENT / ABORTED | Maps to Rejected / InvalidArgs / Unmatched. |

### Review Feedback Incorporated

This design incorporates findings from three concurrent reviews (2026-04-22):

| Reviewer | Verdict | Key contributions |
|----------|---------|-------------------|
| Frank (Architect) | CHANGES_NEEDED → resolved | InvalidArgs variant, Rejected scope documentation, survey citation corrections |
| George (Runtime) | FEASIBLE_WITH_CAVEATS | FieldSnapshot null ambiguity (→ IsResolved), arg-dependency sets, transitive unresolvability, Kleene logic |
| Elaine (UX) | UX_CONCERNS | ConstraintResult field attribution, OverallProspect on EventInspection, ArgInfo, IsResolved |

---

## Innovation

No surveyed system has progressive annotation with per-constraint certainty, per-row prospect, and field-level attribution. The inspection model is purpose-built for Precept's progressive evaluation requirement — evaluating pipelines as far as available data allows, annotating each decision point with certainty, and surfacing a complete landscape without requiring complete inputs.

---

## Open Questions / Implementation Notes

These are deferred to the executable model contract (D8/R4) and evaluator design doc, but recorded here as binding requirements from R2:

### Arg-dependency sets (D8/R4)

Each expression node in the executable model must carry a dependency annotation indicating which event arguments appear in its subtree. This is a lowering-time tree walk during `Precept.From()`, not a runtime check. The type checker already resolves all identifier references — computing arg-dependency sets is a straightforward extension.

Without this, the evaluator cannot distinguish field-only guards (fully resolvable) from arg-dependent guards (possibly resolvable) without attempting evaluation and catching failures.

### Transitive unresolvability propagation

When a `set` assignment references a missing arg, the working copy slot is marked unresolvable (not null). Derived fields computed from unresolvable inputs are transitively unresolvable. The recomputation loop must check for unresolvable inputs and propagate accordingly — otherwise constraints evaluated against the working copy will receive wrong values and produce spurious violations.

The working copy data structure must carry an `IsResolved` flag per slot, mirroring the output `FieldSnapshot.IsResolved`.

### Kleene three-value guard evaluation

Guard evaluation under partial args must use Kleene three-value logic. Comparisons referencing a missing arg produce Unknown. Boolean operators propagate Unknown according to the truth table above. The evaluator must not throw or fault when encountering an unresolvable subexpression — it must propagate the Unknown signal through the expression tree and map the final result to a `Prospect` value.

---

## Deliberate Exclusions

| Concern | Disposition |
|---------|-------------|
| Lightweight inspection mode (E5) | Always return full landscape. At DSL scale (10–50 fields, 5–15 events) the full pipeline is sub-millisecond — premature optimization would add API surface for no measurable gain. A lightweight mode can be added later without changing existing signatures. |
| `Possible` conflating data-insufficient and logically-uncertain (E6) | Three-value Prospect is sufficient. A fourth value (`ArgDependent`) was considered and rejected: the UI already knows which args are missing via `RequiredArgs` and can infer arg-absence from context. A fourth value would complicate pattern matching at every consumer for a signal that's redundant with information already on the Version surface. |
| `Version.Precept` contract (E7) | Separate design decision. Not part of R2. |
| Migration distance from prototype (G8) | Full API break acknowledged. Implementation planning concern. |

---

## Cross-References

- `compiler-and-runtime-design.md` — compiler/runtime architecture overview
- `executable-model.md` (D8/R4) — arg-dependency sets and evaluator contract
- `fault-system.md` — `FaultException`; evaluator-level faults excluded from outcome hierarchy
- `runtime-api.md` — full `Version` API spec; § Constraint Exposure Model (three-tier model, Tier 3 = `ConstraintResult.Constraint`)

---

## Source Files

`src/Precept/Runtime/` — stub; not yet implemented. Expected types:

- `EventOutcome.cs` — sealed record hierarchy
- `UpdateOutcome.cs` — sealed record hierarchy
- `EventInspection.cs` — inspection result type
- `UpdateInspection.cs` — inspection result type
- `RowInspection.cs` — row-level inspection
- `FieldSnapshot.cs` — shared primitive
- `ConstraintResult.cs` — shared primitive
- `ArgInfo.cs` — shared primitive
- `Prospect.cs` — enum
- `ConstraintStatus.cs` — enum
- `RowEffect.cs` — discriminated union
- `Version.cs` — runtime Version record
