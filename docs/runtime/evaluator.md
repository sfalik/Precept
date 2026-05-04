# Evaluator

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Stub (`Evaluator.cs` exists; `Fail(FaultCode)` routes through `Faults.Create`; all operation bodies throw `NotImplementedException`) |
| Source | `src/Precept/Runtime/Evaluator.cs` |
| Upstream | `Precept` (executable model), `Version` (entity instance) |
| Downstream | `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, `EventInspection`, `UpdateInspection` |

---

## 2. Overview

The evaluator is the **pure plan executor** — it consumes prebuilt execution structures from the `Precept` model and drives all runtime operations. It performs no semantic reasoning, no catalog lookups, and no name resolution at execution time. All routing and classification decisions were made at build time by the Precept Builder.

**Pipeline Position:**

```text
Precept (executable model) + Version (entity instance) → Evaluator → EventOutcome / UpdateOutcome / RestoreOutcome / EventInspection / UpdateInspection
```

The evaluator's inner loop is: look up a prebuilt plan, walk its opcode array, evaluate constraints from prebuilt buckets, produce a structured outcome. This makes the evaluator the smallest it can be while being correct — a tight O(n) loop with no conditional branching on language features.

**Key design identity:** The evaluator is to execution what the parser is to syntax — it mechanically applies prebuilt structures without semantic interpretation. The Precept Builder encodes all language knowledge into dispatch indexes, constraint buckets, and execution plans. The evaluator reads these structures and executes them.

**Four operations, two modes:**

| Operation | Mode | Description |
|---|---|---|
| `Create` | Commit | Construct initial entity instance |
| `Fire` | Commit | Execute event, apply mutations, transition state |
| `Update` | Commit | Apply field patch, enforce access modes and constraints |
| `Restore` | Commit | Reconstitute entity from persisted data |
| `InspectFire` | Inspect | Preview all transition rows without committing |
| `InspectUpdate` | Inspect | Preview field patch effects without committing |

Inspection and commit paths execute the **same prebuilt plans** — the only difference is disposition (report vs. enforce).

---

## 3. Responsibilities and Boundaries

### In Scope

| Responsibility | Description |
|---|---|
| **Opcode execution** | Walk flat slot-addressed opcode arrays; push/pop from evaluation stack; read/write field slots |
| **Transition row dispatch** | Look up `TransitionDispatchIndex[(state, event)]` → ordered `ExecutionRow` list; evaluate guards; select candidate |
| **Constraint evaluation** | Read `ConstraintPlanIndex` buckets; evaluate each constraint's `ExecutionPlan` against slot array; collect violations |
| **Action plan execution** | Execute `ActionPlan` arrays from `ExecutionRow`; apply mutations to working copy |
| **Access mode enforcement** | For Update: check `FieldDescriptor.AccessModes[currentState]` before accepting patch |
| **Computed field recomputation** | After mutations and before constraint evaluation: walk `SlotLayout.ComputedSlots` and re-evaluate |
| **Structured outcome production** | Produce `EventOutcome`, `UpdateOutcome`, `RestoreOutcome` discriminated unions — never throw for in-domain failures |
| **Inspection production** | Produce `EventInspection`, `UpdateInspection` with full row-by-row and constraint-by-constraint detail |
| **Fault backstop routing** | At impossible-path sites: call `Faults.Create(descriptor, context)` with the planted `FaultSiteDescriptor` |

### Out of Scope

| Exclusion | Rationale |
|---|---|
| **Semantic reasoning** | All semantic decisions were made by the type checker and encoded by the Precept Builder. The evaluator reads prebuilt structures. |
| **Catalog lookups** | The evaluator never calls `Actions.GetMeta()`, `Operations.GetMeta()`, or `Constraints.GetMeta()`. Catalog metadata is read at build time, not execution time. |
| **Name resolution** | Field names → slot indexes; event names → `EventDescriptor`; all resolved at build time. The evaluator uses descriptor identity and slot indexes. |
| **Dispatch index building** | The Precept Builder builds `TransitionDispatchIndex`, `ConstraintPlanIndex`, etc. The evaluator only reads them. |
| **Outcome type definitions** | `EventOutcome`, `UpdateOutcome`, `RestoreOutcome` are defined in their own source files — the evaluator produces instances, doesn't define the types. |
| **Plan building** | `ExecutionPlan` construction is the Precept Builder's domain. The evaluator walks plans, never builds them. |

---

## 4. Right-Sizing

The evaluator is scoped precisely to execution — the smallest component that correctly applies prebuilt plans.

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 400–600 | ~150 opcode execution + ~100 row dispatch + ~100 constraint evaluation + ~100 action execution + ~50 outcome production |
| External dependencies | 0 | Pure computation — no I/O, no external services, no network calls |
| Catalog dependency | 0% at runtime | All catalog metadata was consumed by the Precept Builder; the evaluator reads only the built `Precept` model |
| Determinism | 100% | Same `Precept` + same `Version` + same operation always produces the same outcome |

**Why not inline into `Version`?**

The evaluator is a static class with pure functions — aligned with the pipeline pattern (`Lexer.Lex`, `Parser.Parse`, `TypeChecker.Check`). `Version` is a thin façade: it holds identity (Precept + state + slots) and delegates to `Evaluator`. This separation keeps `Version` focused on identity and the evaluator focused on execution mechanics.

**Why not merge into `Precept`?**

`Precept` is the executable model — an immutable artifact that describes what operations are possible. The evaluator applies those operations to a specific `Version`. Merging them would conflate definition (what can happen) with execution (what does happen).

**Bounded complexity:**

The evaluator has no algorithmic complexity beyond iteration. It does not compute fixpoints, solve constraints, or traverse graphs. Every loop is O(n) over a prebuilt array: opcodes, rows, constraints, actions. The only conditional branching is guard evaluation and constraint checking — both read prebuilt plans.

---

## 5. Inputs and Outputs

### Primary Input: The Version Type

The `Version` type is the evaluator's primary runtime substrate — an entity instance representing a specific state and field values at a point in time:

```csharp
/// <summary>
/// Immutable snapshot of an entity at a point in time. Every operation returns
/// a new Version — the input is never mutated.
/// </summary>
public sealed record Version
{
    internal Version(Precept precept, StateDescriptor? currentState, PreceptValue[] slots)
    {
        Precept = precept;
        CurrentState = currentState;
        Slots = slots;
    }

    /// <summary>The executable model this version belongs to.</summary>
    public Precept Precept { get; }
    
    /// <summary>Current state, or null for stateless precepts.</summary>
    public StateDescriptor? CurrentState { get; }
    
    /// <summary>Slot-addressed field values. Index via FieldDescriptor.SlotIndex.</summary>
    internal PreceptValue[] Slots { get; }
}
```

The evaluator's inner loop reads from and writes to `Slots` via slot index. It never resolves field names at runtime — all name-to-slot mappings were resolved at build time. `PreceptValue` is the 32-byte tagged value struct shared across the entire evaluation pipeline. On commit success, the working copy `PreceptValue[]` is donated directly as the new `Version.Slots` — no clone (zero-copy promotion). On constraint failure, the array is returned to `ArrayPool<PreceptValue>.Shared`.

### PreceptValue: Evaluation Currency

`PreceptValue` is the unified value representation for every scalar, reference, and absent value at runtime. All field slot reads and writes, all opcode stack pushes and pops, and all event arg representations use `PreceptValue`. There is no `object?` at evaluation time.

**Why 32 bytes?** The target size is architecturally motivated by GC throughput at scale. At 100 000 Fire calls/sec, a boxed-`object?` evaluation currency projects approximately **~768 MB/s of gen-0 pressure** (each boxed value is an 8-byte reference to a 16-24 byte heap object; 40-60 live values per call). Replacing boxing with a 32-byte value struct eliminates this pressure entirely: the working copy array sits in the LOH-exempt gen-0 region and the evaluator's stack frame carries its `Span<PreceptValue>` without any heap traffic. The 32-byte size target accommodates the tag plus the largest union payload (`decimal`, which is 16 bytes) with alignment padding, while keeping the struct copy cost well inside what the JIT inlines in a register-passing calling convention.

**Hot-path memory picture (Fire, one event, Option A+G baseline):**

| Region | Slots | Size |
|--------|-------|------|
| Field slots (typical precept) | ~36–44 | 1,152–1,408 bytes |
| Ephemeral arg slots (per Fire call) | ~4–8 | 128–256 bytes |
| `stackalloc` opcode stack | 32 fixed | 1,024 bytes |
| Working copy slot array (rented) | same as field slots | 1,152–1,408 bytes |
| **Total peak stack traffic per Fire** | **~44–48 slots** | **~4,480 bytes** |

With slot-array pooling via `ArrayPool<PreceptValue>.Shared`, the GC-visible allocation per Fire call drops to the unavoidable boundary objects: the outcome record (`Transitioned` / `Applied`) and `FiredArgs` wrapper, totalling approximately **~88 bytes**. All scalar evaluation is zero-boxing.

**Struct layout — target and open design question:**

The 32-byte size and tagged-value-struct shape are locked. The exact internal field layout (tag type, union field offsets, which types use which union region) is an **open design question pending implementation decision**:

```csharp
// SHAPE LOCKED — INTERNAL LAYOUT PENDING DECISION
// The 32-byte, tagged-struct representation is the architectural baseline.
// Field offsets, tag enum, and union regions are not yet specified at implementation level.
[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct PreceptValue
{
    // Byte 0: type discriminant tag (PreceptValueTag enum — members TBD)
    // Bytes 1–7: padding / reserved
    // Bytes 8–23: union payload — exact field layout pending:
    //   - scalar region (long, bool via long, enum ordinal via long, DateOnly)
    //   - wide scalar region (decimal — 16 bytes — overlaps scalar region)
    //   - reference region (string, object, null sentinel)
    //   How decimal, DateOnly, and reference types share the union is the open question.
    // Bytes 24–31: reserved / padding to 32-byte boundary
}
```

> **Open Design Question:** The exact `StructLayout` field offsets need a concrete decision before implementation of the opcode executor. Specifically:
> 1. What is the `PreceptValueTag` enum shape (byte, int, other)?
> 2. Does `decimal` (16 bytes) overlay with the reference region, or does the struct use a parallel reference + scalar split?
> 3. Where does `DateOnly` (4 bytes) sit relative to `long`?
> 4. What is the absent/null sentinel encoding (tag value vs. zero-value check)?
> *This decision belongs in `evaluator.md` and must be made before the opcode executor implementation pass.*

### Input Summary

| Input | Source | Description |
|---|---|---|
| `Precept` | `Version.Precept` | The executable model — descriptor tables, dispatch indexes, constraint plan indexes, execution plans, fault backstops |
| `Version` | Operation input | Current state + field slot array |
| `EventDescriptor` | Fire/InspectFire | The event being fired (resolved from name at API boundary) |
| `args` | Fire/InspectFire | Event argument values keyed by `ArgDescriptor.Name` |
| `patch` | Update/InspectUpdate | Field values to apply, keyed by field name (resolved to slot index at API boundary) |
| `state + fields` | Restore | Persisted state name + field values to reconstitute |

### Output: Commit Operations

**EventOutcome** (returned by `Fire` and `Create`):

```csharp
public abstract record EventOutcome;
public sealed record Transitioned(Version Result, FiredArgs Args) : EventOutcome;  // State change succeeded
public sealed record Applied(Version Result, FiredArgs Args) : EventOutcome;       // No-transition row or stateless event succeeded
public sealed record Rejected(string Reason, FiredArgs Args) : EventOutcome;       // Authored reject row matched
public sealed record InvalidArgs(string Reason) : EventOutcome;                    // Arg validation failure
public sealed record EventConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : EventOutcome;
public sealed record Unmatched() : EventOutcome;                                   // All guards failed
public sealed record UndefinedEvent() : EventOutcome;                              // No rows for event in current state
```

**UpdateOutcome** (returned by `Update`):

```csharp
public abstract record UpdateOutcome;
public sealed record FieldWriteCommitted(Version Result) : UpdateOutcome;   // Patch applied, constraints passed
public sealed record UpdateConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : UpdateOutcome;
public sealed record AccessDenied(string FieldName, FieldAccessMode ActualMode) : UpdateOutcome;
public sealed record InvalidInput(string Reason) : UpdateOutcome;           // Type mismatch or structural error
```

**RestoreOutcome** (returned by `Restore`):

```csharp
public abstract record RestoreOutcome;
public sealed record Restored(Version Result) : RestoreOutcome;             // Constraints passed, Version ready
public sealed record RestoreConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : RestoreOutcome;
public sealed record RestoreInvalidInput(string Reason) : RestoreOutcome;   // Structural mismatch
```

### Output: Inspection Operations

**EventInspection** (returned by `InspectFire`):

```csharp
public sealed record EventInspection(
    string EventName,
    Prospect OverallProspect,                        // Certain | Possible | Impossible
    IReadOnlyList<ConstraintResult> EventEnsures,    // Event-level constraint results
    IReadOnlyList<RowInspection> Rows                // Per-row inspection detail
);
```

**RowInspection** (per transition row within `EventInspection`):

```csharp
public sealed record RowInspection(
    Prospect Prospect,                               // Would this row fire?
    RowEffect Effect,                                // TransitionTo(state) | NoTransition | Rejection(reason)
    IReadOnlyList<FieldSnapshot> ResultingFields,    // Projected field values post-mutation
    IReadOnlyList<ConstraintResult> Constraints,     // Constraint evaluation results
    Version? HypotheticalResult                      // Projected Version if this row were to fire
);

public enum Prospect { Certain = 1, Possible = 2, Impossible = 3 }
public enum ConstraintStatus { Satisfied = 1, Violated = 2, Unresolvable = 3 }
```

**UpdateInspection** (returned by `InspectUpdate`):

```csharp
public sealed record UpdateInspection(
    IReadOnlyList<FieldSnapshot> Fields,             // Projected field values post-patch
    IReadOnlyList<ConstraintResult> Constraints,     // Constraint evaluation results
    IReadOnlyList<EventInspection> Events,           // Event-prospect: what events would be available?
    Version? HypotheticalResult                      // Projected Version if patch were committed
);
```

---

## 6. Architecture

### Evaluator Structure

The evaluator is a static class with pure functions — no instance state, no per-invocation allocation beyond working copies:

```csharp
public static class Evaluator
{
    // ── Commit Operations ────────────────────────────────────────
    internal static EventOutcome Fire(Precept precept, Version version, EventDescriptor @event, FiredArgs args);
    internal static UpdateOutcome Update(Precept precept, Version version, PreceptValue[] patch);
    internal static RestoreOutcome Restore(Precept precept, StateDescriptor? state, PreceptValue[] fields);
    
    // ── Inspection Operations ────────────────────────────────────
    internal static EventInspection InspectFire(Precept precept, Version version, EventDescriptor @event, FiredArgs? args);
    internal static UpdateInspection InspectUpdate(Precept precept, Version version, PreceptValue[]? patch);
    
    // ── Fault Backstop ───────────────────────────────────────────
    internal static Fault Fail(FaultCode code, params object?[] args);
}
```

### Execution Flow Diagram

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│                              Fire(event, args)                                │
└──────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  Step 1: Dispatch Lookup                                                      │
│  TransitionDispatchIndex[(currentState, event)] → ExecutionRow[]              │
│  If empty → return UndefinedEvent()                                           │
└──────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  Step 2: Row Evaluation Loop (for each ExecutionRow)                          │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  2a. Guard Evaluation                                                   │  │
│  │  If row.Guard != null: evaluate against slots + args                    │  │
│  │  If false → skip this row                                               │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  2b. Snapshot Working Copy                                              │  │
│  │  Clone slots array for mutation isolation                               │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  2c. Action Execution                                                   │  │
│  │  For each ActionPlan in row.Actions: execute against working copy       │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  2d. Computed Field Recomputation                                       │  │
│  │  For each slot in SlotLayout.ComputedSlots: re-evaluate expression      │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  2e. Constraint Evaluation                                              │  │
│  │  always + from<current> + on<event> + to<target>                        │  │
│  │  Collect all violations; if any → row fails                             │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  If row passes all constraints → collect as candidate                         │
└──────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  Step 3: Candidate Resolution                                                 │
│  Zero candidates → return Unmatched() or EventConstraintsFailed(violations)   │
│  One candidate → commit working copy, return Transitioned/Applied             │
│  Multiple candidates → return Fault (ambiguous dispatch — impossible path)    │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Working Copy Management

The evaluator never mutates the input `Version`. The full Fire lifecycle for one event:

1. **Rent:** Rent a `PreceptValue[]` working copy from `ArrayPool<PreceptValue>.Shared`
2. **Populate:** Copy `Version.Slots` into the rented array (field slots)
3. **Load args:** The `FiredArgs` value carries the `PreceptValue[]` arg slot array materialized by `IArgBuilder` at the Fire boundary. Event args are already `PreceptValue` — no per-opcode conversion needed. `LOAD_ARG` reads from this array by pre-resolved slot index.
4. **Mutate:** Execute action plans against the working copy
5. **Recompute:** Walk `SlotLayout.ComputedSlots`; re-evaluate each computed field
6. **Evaluate:** Run constraint plans against the working copy
7. **Commit or discard:** If constraints pass, donate the working copy directly as `new Version(..., workingCopy).Slots` (zero-copy promotion — no clone); if constraints fail, return the array to `ArrayPool<PreceptValue>.Shared`

This ensures that constraint evaluation sees the post-mutation state, and that failed rows leave no side effects. On success, the working array becomes the committed `Version.Slots` — no extra allocation.

---

## 7. Component Mechanics

### 7.0 Evaluation Stack Allocation

Each expression evaluation call allocates its operand stack on the thread stack using `stackalloc` — no heap allocation per evaluation:

```csharp
internal static PreceptValue EvaluatePlan(ExecutionPlan plan, PreceptValue[] slots, FiredArgs args)
{
    Span<PreceptValue> stack = stackalloc PreceptValue[32];
    int top = 0;
    foreach (var opcode in plan.Opcodes)
        Dispatch(opcode, slots, args, stack, ref top);
    return stack[0];
}
```

**`ExecutionPlan.MaxStackDepth`** is computed at build time (Precept Builder Pass 5) and represents the maximum stack depth the plan will ever reach. The compiler enforces that `MaxStackDepth ≤ 32`; if a plan exceeds this, a compile-time diagnostic is emitted and the plan is not emitted. This makes the stack bound statically verifiable — no runtime stack overflow risk.

**Sync-only.** The evaluation API is synchronous. There are no `async`/`await` wrappers around evaluation calls. Expression evaluation must complete synchronously within the caller's thread. This is consistent with the stack-allocated operand model — `stackalloc` spans cannot cross `await` points.

| Constraint | Enforcement | Notes |
|-----------|------------|-------|
| `MaxStackDepth ≤ 32` | Compile-time diagnostic | Builder Pass 5 computes depth; TC enforces |
| No heap allocation | `stackalloc` + `ArrayPool` for working copies | Per-evaluation zero heap |
| Sync-only API | No `async` wrappers | `stackalloc` cannot cross `await` |

### 7.1 The Four Operations

#### Create

`Create` constructs the initial entity instance. Behavior depends on whether the precept declares an initial event:

> **Implementation note:** The pseudocode below uses `object?[]` for readability in showing the slot array operations. The canonical implementation uses `PreceptValue[]` throughout — all slot values are `PreceptValue`, not boxed `object?`. The logic structure is unchanged.

**With initial event:**

```csharp
EventOutcome Create(IReadOnlyDictionary<string, object?>? args)
{
    // 1. Build hollow version: default slot values + initial state
    var hollowSlots = new object?[precept.SlotLayout.FieldCount];
    for (int i = 0; i < hollowSlots.Length; i++)
        hollowSlots[i] = EvaluateDefault(precept.Fields[i]);
    
    var hollow = new Version(precept, precept.InitialState, hollowSlots);
    
    // 2. Fire the initial event atomically
    return Fire(precept, hollow, precept.InitialEvent!, args ?? EmptyArgs);
}
```

**Without initial event:**

```csharp
EventOutcome Create(IReadOnlyDictionary<string, object?>? args)
{
    // 1. Build version with defaults + initial state
    var slots = new object?[precept.SlotLayout.FieldCount];
    for (int i = 0; i < slots.Length; i++)
        slots[i] = EvaluateDefault(precept.Fields[i]);
    
    // 2. Recompute computed fields
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        slots[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, slots, args);
    
    // 3. Evaluate constraints: always + in <initial>
    var violations = EvaluateConstraintBuckets(
        precept.ConstraintPlanIndex.Always,
        precept.ConstraintPlanIndex.InState[precept.InitialState!],
        slots, args);
    
    if (violations.Count > 0)
        return new EventConstraintsFailed(violations);  // Should not happen per C100/C101
    
    return new Applied(new Version(precept, precept.InitialState, slots));
}
```

**Compiler guarantees:** The compiler enforces that precepts with required fields lacking defaults declare an initial event, and that the initial event assigns those fields. Therefore, `Create` without an initial event cannot produce `Rejected` for well-proven programs.

> **Open Question (unresolved):** The "C100/C101" compiler guarantee identifiers referenced in an earlier draft are not formally defined in type-checker.md, graph-analyzer.md, or proof-engine.md. These enforcement rules need traceable definitions in the appropriate upstream docs.

#### Fire(event, args)

```csharp
EventOutcome Fire(Precept precept, Version version, EventDescriptor @event, IReadOnlyDictionary<string, object?> args)
{
    // 1. Look up dispatch index
    if (!precept.DispatchIndex.Rows.TryGetValue((version.CurrentState, @event), out var rows))
        return new UndefinedEvent();
    
    var candidates = new List<(ExecutionRow row, object?[] workingCopy)>();
    var allViolations = new List<ConstraintViolation>();
    
    // 2. Evaluate each row
    foreach (var row in rows)
    {
        // 2a. Guard evaluation
        if (row.Guard != null && !EvaluateGuard(row.Guard, version.Slots, args))
            continue;  // Guard failed — skip row
        
        // 2b. Snapshot working copy
        var workingCopy = (object?[])version.Slots.Clone();
        
        // 2c. Execute actions
        foreach (var action in row.Actions)
            ExecuteAction(action, workingCopy, args);
        
        // 2d. Recompute computed fields
        foreach (var slot in precept.SlotLayout.ComputedSlots)
            workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, args);
        
        // 2e. Evaluate constraints
        var violations = EvaluateFireConstraints(
            precept.ConstraintPlanIndex,
            version.CurrentState,
            @event,
            row.TargetState,
            workingCopy, args);
        
        if (violations.Count == 0)
            candidates.Add((row, workingCopy));
        else
            allViolations.AddRange(violations);
    }
    
    // 3. Candidate resolution
    if (candidates.Count == 0)
    {
        if (allViolations.Count > 0)
            return new EventConstraintsFailed(allViolations);
        return new Unmatched();
    }
    
    if (candidates.Count > 1)
        return Fail(FaultCode.AmbiguousDispatch);  // Impossible in proven program

> **Open Question (unresolved):** `FaultCode.AmbiguousDispatch` is not in the current Faults catalog. It needs a catalog entry and a corresponding `DiagnosticCode` with `[StaticallyPreventable]` attribute before implementation.
    
    var (winningRow, finalSlots) = candidates[0];
    var newState = winningRow.TargetState ?? version.CurrentState;
    
    return winningRow.Outcome switch
    {
        TransitionOutcome.Transition => new Transitioned(new Version(precept, newState, finalSlots)),
        TransitionOutcome.NoTransition => new Applied(new Version(precept, version.CurrentState, finalSlots)),
        TransitionOutcome.Reject => new Rejected(winningRow.RejectReason ?? "Rejected"),
        _ => throw new InvalidOperationException()
    };
}
```

`TransitionOutcome` is the canonical type name (defined in `type-checker.md` §7.1). The evaluator uses this same type at runtime.

> **Open Question (unresolved):** `winningRow.RejectReason` references a field not defined on `ExecutionRow`. The `because` clause from a `reject` transition row needs a storage location. Should `ExecutionRow` gain a `string? RejectReason` field?
> *Source: catalog-gap-register.md #20*

```csharp
UpdateOutcome Update(Precept precept, Version version, IReadOnlyDictionary<FieldDescriptor, object?> patch)
{
    // 1. Access mode checks
    foreach (var (field, _) in patch)
    {
        var mode = GetAccessMode(field, version.CurrentState);
        if (mode != AccessMode.Writable)
            return new AccessDenied(field.Name, mode == AccessMode.ReadOnly ? FieldAccessMode.Read : FieldAccessMode.Omit);
    }
    
    // 2. Apply patch to working copy
    var workingCopy = (object?[])version.Slots.Clone();
    foreach (var (field, value) in patch)
        workingCopy[field.SlotIndex] = value;
    
    // 3. Recompute computed fields
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, EmptyArgs);
    
    // 4. Evaluate constraints: always + in <current>
    var violations = EvaluateConstraintBuckets(
        precept.ConstraintPlanIndex.Always,
        precept.ConstraintPlanIndex.InState.GetValueOrDefault(version.CurrentState),
        workingCopy, EmptyArgs);
    
    if (violations.Count > 0)
        return new UpdateConstraintsFailed(violations);
    
    return new FieldWriteCommitted(new Version(precept, version.CurrentState, workingCopy));
}
```

#### Restore(state, fields)

```csharp
RestoreOutcome Restore(Precept precept, StateDescriptor? state, IReadOnlyDictionary<FieldDescriptor, object?> fields)
{
    // 1. Build working copy with provided values
    var workingCopy = new object?[precept.SlotLayout.FieldCount];
    foreach (var (field, value) in fields)
        workingCopy[field.SlotIndex] = value;
    
    // 2. Recompute computed fields FIRST (before constraint evaluation)
    // This catches stale values in persisted snapshots
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, EmptyArgs);
    
    // 3. Evaluate constraints: always + in <current>
    // NOTE: Access modes are BYPASSED — Restore is a persistence operation
    var violations = EvaluateConstraintBuckets(
        precept.ConstraintPlanIndex.Always,
        precept.ConstraintPlanIndex.InState.GetValueOrDefault(state),
        workingCopy, EmptyArgs);
    
    if (violations.Count > 0)
        return new RestoreConstraintsFailed(violations);
    
    return new Restored(new Version(precept, state, workingCopy));
}
```

**Key distinction:** Restore bypasses access-mode checks but does NOT bypass constraint checks. Access modes are interactive controls (what can the user edit?). Constraints are correctness invariants (what must always be true?). They have different bypass rules.

### 7.2 Inspection Operations

#### InspectFire(event, args)

Returns `EventInspection` — one `RowInspection` per declared transition row for the current state/event:

```csharp
EventInspection InspectFire(Precept precept, Version version, EventDescriptor @event, IReadOnlyDictionary<string, object?>? args)
{
    if (!precept.DispatchIndex.Rows.TryGetValue((version.CurrentState, @event), out var rows))
        return new EventInspection(@event.Name, Prospect.Impossible, [], []);
    
    var rowInspections = new List<RowInspection>();
    var overallProspect = Prospect.Impossible;
    ExecutionRow? winningRow = null;
    
    foreach (var row in rows)
    {
        // Evaluate guard
        var guardResult = row.Guard == null || EvaluateGuard(row.Guard, version.Slots, args ?? EmptyArgs);
        
        if (!guardResult)
        {
            // Guard failed — row is impossible
            rowInspections.Add(new RowInspection(
                Prospect.Impossible, GetRowEffect(row), [], [], null));
            continue;
        }
        
        // Guard passed — simulate execution
        var workingCopy = (object?[])version.Slots.Clone();
        foreach (var action in row.Actions)
            ExecuteAction(action, workingCopy, args ?? EmptyArgs);
        
        foreach (var slot in precept.SlotLayout.ComputedSlots)
            workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, args ?? EmptyArgs);
        
        var violations = EvaluateFireConstraints(...);
        var constraintResults = BuildConstraintResults(violations, ...);
        var fieldSnapshots = BuildFieldSnapshots(workingCopy, precept.Fields);
        
        var prospect = violations.Count == 0 ? Prospect.Certain : Prospect.Impossible;
        
        // Track if this row would win
        if (prospect == Prospect.Certain && winningRow == null)
        {
            winningRow = row;
            if (overallProspect == Prospect.Impossible)
                overallProspect = Prospect.Certain;
        }
        else if (prospect == Prospect.Certain && winningRow != null)
        {
            // Multiple candidates — ambiguous
            overallProspect = Prospect.Possible;  // or Fault
        }
        
        var hypotheticalVersion = violations.Count == 0
            ? new Version(precept, row.TargetState ?? version.CurrentState, workingCopy)
            : null;
        
        rowInspections.Add(new RowInspection(
            prospect, GetRowEffect(row), fieldSnapshots, constraintResults, hypotheticalVersion));
    }
    
    return new EventInspection(@event.Name, overallProspect, [], rowInspections);
}
```

> **Open Question (unresolved):** Two unresolved design issues in `InspectFire`: (1) Multiple candidates handling — should inspection return `Fault` like `Fire`? (2) `EventEnsures` is hardcoded to `[]` — event-level constraints (`on<event>` bucket) are not evaluated in inspection. Should they be?

It evaluates every declared row, not just the winning candidate. Each `RowInspection` describes:
- `GuardMatched`: Did the guard pass?
- `Constraints`: Which constraints were evaluated and their results
- `ResultingFields`: Projected field values if this row were to fire
- `HypotheticalResult`: The `Version` that would result if this row committed

#### InspectUpdate(patch)

Returns `UpdateInspection` — access mode results, constraint evaluation, and event-prospect analysis:

```csharp
UpdateInspection InspectUpdate(Precept precept, Version version, IReadOnlyDictionary<FieldDescriptor, object?>? patch)
{
    var workingCopy = (object?[])version.Slots.Clone();
    
    // Apply patch if provided
    if (patch != null)
    {
        foreach (var (field, value) in patch)
            workingCopy[field.SlotIndex] = value;
    }
    
    // Recompute computed fields
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, EmptyArgs);
    
    // Evaluate constraints
    var violations = EvaluateConstraintBuckets(...);
    var constraintResults = BuildConstraintResults(violations, ...);
    var fieldSnapshots = BuildFieldSnapshots(workingCopy, precept.Fields, version.CurrentState);
    
    // Event-prospect evaluation: what events would be available post-patch?
    var hypotheticalVersion = new Version(precept, version.CurrentState, workingCopy);
    var eventInspections = new List<EventInspection>();
    foreach (var @event in precept.Events)
    {
        if (precept.DispatchIndex.Rows.ContainsKey((version.CurrentState, @event)))
            eventInspections.Add(InspectFire(precept, hypotheticalVersion, @event, null));
    }
    
    var hypotheticalResult = violations.Count == 0 ? hypotheticalVersion : null;
    
    return new UpdateInspection(fieldSnapshots, constraintResults, eventInspections, hypotheticalResult);
}
```

### 7.3 Opcode Execution Engine

The evaluator walks flat `ExecutionPlan` opcode arrays built by the Precept Builder. The execution model is a stack machine.

**Canonical signature (PreceptValue baseline — see §7.0):**

```csharp
// Canonical implementation: stackalloc PreceptValue[32], indexed top pointer.
// See §7.0 Evaluation Stack Allocation for the full implementation.
internal static PreceptValue EvaluatePlan(ExecutionPlan plan, PreceptValue[] slots, FiredArgs args)
{
    Span<PreceptValue> stack = stackalloc PreceptValue[plan.MaxStackDepth];
    int top = 0;
    foreach (var opcode in plan.Opcodes)
        Dispatch(opcode, slots, args, stack, ref top);
    return stack[0];
}
```

**Operator dispatch (catalog-owned, zero-knowledge evaluator):**

Binary and unary operator execution uses catalog-owned executor delegates indexed by `OperationKind`. The evaluator does not switch on `OperationKind` members or apply per-operator logic. It reads the pre-wired delegate:

```csharp
case BinaryOp(var kind):
    PreceptValue r = stack[--top];
    PreceptValue l = stack[--top];
    // Executor is a catalog-owned Func<PreceptValue, PreceptValue, PreceptValue>,
    // retrieved from TypeRuntime.BinaryExecutors[(int)kind] at startup.
    // The evaluator is zero-knowledge about what the operation does.
    stack[top++] = Operations.BinaryExecutors[(int)kind](l, r);
    break;
```

The `Operations` catalog pre-wires flat executor arrays at startup — one slot per `OperationKind` ordinal. The evaluator calls the delegate; it never switches on `OperationKind` itself. Same pattern for unary ops via `Operations.UnaryExecutors`.

**LOAD_ARG and event-arg slots:**

Event arguments are converted to `PreceptValue` at the Fire boundary (before the opcode loop begins), not lazily inside the loop. `IArgBuilder` materializes a `PreceptValue[]` arg slot array for the call; `LOAD_ARG(name)` resolves the arg name to a slot index at build time, so the opcode carries a slot index rather than a string:

```csharp
case LoadArg(var argSlotIndex):
    stack[top++] = args[argSlotIndex];  // args is PreceptValue[], pre-filled at Fire boundary
    break;
```

This keeps LOAD_ARG O(1) — no dictionary lookup, no string comparison.

> **Open Question (unresolved):** The LOAD_ARG opcode is currently described as `LOAD_ARG(name)` in the opcode table (string key). Locking the opcode to `LOAD_ARG(slotIndex)` (integer index, resolved at build time) is the intended design per the typed-slot baseline, but this requires `ArgDescriptor.SlotIndex` to be defined and populated by the Precept Builder. The `ArgDescriptor` shape needs updating before the evaluator implementation pass.

| Opcode | Stack Effect | Description |
|---|---|---|
| `LOAD_SLOT(i)` | push | Load field value at slot index `i` |
| `LOAD_ARG(argSlotIndex)` | push | Load event arg value by pre-resolved arg slot index (args converted to `PreceptValue[]` at Fire boundary; no dictionary lookup at execution time) |
| `LOAD_LIT(value)` | push | Push literal value (boxed) |
| `STORE_SLOT(i)` | pop | Pop and store to slot index `i` |
| `BINARY_OP(kind)` | pop 2, push 1 | Binary operation (via `OperationMeta`) |
| `UNARY_OP(kind)` | pop 1, push 1 | Unary operation |
| `CALL_FUNCTION(kind, arity)` | pop N, push 1 | Function call (via `FunctionMeta`) |
| `MEMBER_ACCESS(accessor)` | pop 1, push 1 | Type accessor (via `TypeAccessorMeta`) |
| `COLLECTION_OP(kind, slot)` | varies | Collection mutation (in-place on slot) |
| `BRANCH_FALSE(offset)` | pop 1 | Short-circuit for `&&` |
| `BRANCH_TRUE(offset)` | pop 1 | Short-circuit for `||` |
| `JUMP(offset)` | — | Unconditional (for `?:`) |
| `DUP` | pop 1, push 2 | Duplicate top of stack |
| `POP` | pop 1 | Discard top of stack |
| `RETURN` | pop 1 | End of plan; result is top |

**Key property:** O(n) execution — one pass through the opcode array. No recursion, no dynamic dispatch on expression node types, no stack depth limits.

### 7.4 Action Dispatch

Action dispatch reads `ActionMeta.SyntaxShape` from the Actions catalog. The 9 `ActionSyntaxShape` values cover all action kinds:

| SyntaxShape | Example Actions | Evaluator Behavior |
|---|---|---|
| `AssignValue` | `set` | Pop value from action plan result, `STORE_SLOT(target)` |
| `CollectionValue` | `add`, `enqueue`, `push`, `append` | Pop value, append/add to collection at target slot |
| `CollectionInto` | `dequeue`, `pop` | Remove from collection, optionally `STORE_SLOT(into)` |
| `CollectionValueBy` | `append by`, `enqueue by` | Pop value + key, add to keyed collection |
| `CollectionIntoBy` | `dequeue by` | Remove from keyed collection with optional into + by |
| `FieldOnly` | `clear` | Clear collection at slot, or set slot to null |
| `InsertAt` | `insert at` | Pop value + index, insert into list at position |
| `RemoveAtIndex` | `remove at` | Pop index, remove element at position |
| `PutKeyValue` | `put` | Pop key + value, upsert into lookup |

```csharp
void ExecuteAction(ActionPlan action, object?[] slots, IReadOnlyDictionary<string, object?> args)
{
    // SyntaxShape is resolved at build time and stored in ActionPlan
    switch (action.SyntaxShape)
    {
        case ActionSyntaxShape.AssignValue:
            var value = EvaluatePlan(action.Value!, slots, args);
            slots[action.TargetSlot] = value;
            break;
            
        case ActionSyntaxShape.CollectionValue:
            var element = EvaluatePlan(action.Value!, slots, args);
            var collection = slots[action.TargetSlot];
            AddToCollection(collection, element, action.Kind);
            break;
            
        case ActionSyntaxShape.CollectionInto:
            var removed = RemoveFromCollection(slots[action.TargetSlot], action.Kind);
            if (action.IntoSlot.HasValue)
                slots[action.IntoSlot.Value] = removed;
            break;
            
        case ActionSyntaxShape.FieldOnly:
            if (IsCollection(slots[action.TargetSlot]))
                ClearCollection(slots[action.TargetSlot]);
            else
                slots[action.TargetSlot] = null;  // optional field
            break;
            
        // ... remaining shapes
    }
}
```

### 7.5 Access Mode Enforcement

For Update operations, the evaluator checks `FieldDescriptor.AccessModes` before accepting a patch:

```csharp
AccessMode GetAccessMode(FieldDescriptor field, StateDescriptor? currentState)
{
    // AccessModes is pre-resolved by the Precept Builder
    // Key: StateDescriptor? (null = all states / stateless)
    // Value: AccessMode enum (Writable, ReadOnly, Omit)
    
    if (field.AccessModes.TryGetValue(currentState, out var mode))
        return mode;
    
    // Fall back to default (writable unless modified)
    return AccessMode.Writable;
}
```

The `AccessModes` dictionary on `FieldDescriptor` is the **resolved** per-state access mode — derived from the two-layer composition model at build time:
- Field-level baseline: `writable` modifier (default is writable)
- State-level override: `in <State> modify field` or `in <State> omit field`

The evaluator never re-derives this; it reads the pre-resolved descriptor.

> **Open Question (unresolved):** Confirm the shape of `FieldDescriptor.AccessModes`. Current design assumes `ImmutableDictionary<StateDescriptor?, AccessMode>`. Alternative: `ImmutableArray<(StateDescriptor?, AccessMode)>` for smaller field counts.

### 7.6 Constraint Evaluation

```csharp
IReadOnlyList<ConstraintViolation> EvaluateFireConstraints(
    ConstraintPlanIndex index,
    StateDescriptor? currentState,
    EventDescriptor @event,
    StateDescriptor? targetState,
    object?[] slots,
    IReadOnlyDictionary<string, object?> args)
{
    var violations = new List<ConstraintViolation>();
    
    // always constraints
    foreach (var constraint in index.Always)
        EvaluateConstraint(constraint, slots, args, violations);
    
    // from <current> constraints
    if (currentState != null && index.FromState.TryGetValue(currentState, out var fromConstraints))
        foreach (var constraint in fromConstraints)
            EvaluateConstraint(constraint, slots, args, violations);
    
    // on <event> constraints
    if (index.OnEvent.TryGetValue(@event, out var eventConstraints))
        foreach (var constraint in eventConstraints)
            EvaluateConstraint(constraint, slots, args, violations);
    
    // to <target> constraints
    if (targetState != null && index.ToState.TryGetValue(targetState, out var toConstraints))
        foreach (var constraint in toConstraints)
            EvaluateConstraint(constraint, slots, args, violations);
    
    return violations;
}

void EvaluateConstraint(
    ConstraintDescriptor constraint,
    object?[] slots,
    IReadOnlyDictionary<string, object?> args,
    List<ConstraintViolation> violations)
{
    var result = EvaluatePlan(constraint.Expression, slots, args);
    
    if (result is not true)
    {
        violations.Add(new ConstraintViolation(
            constraint,
            BuildFieldNames(constraint.ReferencedFields)));
    }
}
```

**ConstraintViolation shape:**

```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    string? BecauseClause,                   // From constraint declaration
    ImmutableArray<FieldSnapshot> RelevantFields,  // Field values at evaluation time
    string? FailingSubexpression,            // Innermost expression that evaluated false
    object? FailingValue                     // Value that caused failure
);
```

When an opcode hits a fault backstop site (identified by a `FaultSiteDescriptor` planted by the Precept Builder), the evaluator produces a structured `Fault`:

```csharp
internal static Fault Fail(FaultCode code, params object?[] args)
    => Faults.Create(code, args);
```

The `Faults` catalog produces a fully-formed `Fault` with:
- `Code`: The `FaultCode` enum value
- `CodeName`: Stable string identity (e.g., `"DivisionByZero"`)
- `Message`: Pre-formatted English diagnostic message

Faults are **never thrown** — they are returned as structured outcome variants. This maintains the pattern-matchable, composable outcome model.

> **Open Question (unresolved):** `Fail` returns a `Fault`, but `Fault` is not currently a subtype of `EventOutcome`. Should `Faulted(Fault)` be added as an `EventOutcome` variant?
> *Source: catalog-gap-register.md #21*

---

## 8. Dependencies and Integration Points

### Upstream Dependencies

| Dependency | Artifact | What the Evaluator Reads |
|---|---|---|
| **Precept Builder** | `Precept` | The complete executable model: `SlotLayout`, `TransitionDispatchIndex`, `ConstraintPlanIndex`, `ExecutionRow[]`, `FaultBackstops` |
| **Version** | Slot array + state | Current field values via `Version.Slots`, current state via `Version.CurrentState` |

### Internal Dependencies (within the Precept model)

| Structure | Purpose |
|---|---|
| `SlotLayout` | Field count, computed slot list, slot-to-field mapping |
| `TransitionDispatchIndex` | `(state, event) → ExecutionRow[]` dispatch table |
| `ConstraintPlanIndex` | Constraint buckets: `Always`, `InState`, `FromState`, `ToState`, `OnEvent` |
| `ExecutionRow` | Guard plan, action plans, target state, outcome kind |
| `ExecutionPlan` | Flat opcode array for expressions |
| `ActionPlan` | Action kind, target slot, value plan, into slot |
| `FaultSiteDescriptor` | Fault code + preventing diagnostic code + source location |
| `ConstraintDescriptor` | Constraint kind, anchors, expression plan, because clause |
| `FieldDescriptor` | Slot index, type, modifiers, access modes, computed plan |

### Downstream Consumers

| Consumer | How It Uses the Evaluator |
|---|---|
| **Version API** | `Version.Fire`, `Version.Update`, `Version.InspectFire`, `Version.InspectUpdate` delegate to `Evaluator.*` |
| **Precept API** | `Precept.Create`, `Precept.InspectCreate`, `Precept.Restore` delegate to evaluator internals |
| **MCP Tools** | `precept_fire` → `Version.Fire`, `precept_update` → `Version.Update`, `precept_inspect` → `Version.InspectFire`/`InspectUpdate` |
| **Language Server** | Preview rendering consumes inspection outputs |

### Integration Contract

```csharp
// Version façade delegates to Evaluator
public sealed record Version
{
    public EventOutcome Fire(string eventName, IReadOnlyDictionary<string, object?> args)
    {
        var @event = Precept.Events.Single(e => e.Name == eventName);
        return Evaluator.Fire(Precept, this, @event, args);
    }
    
    public UpdateOutcome Update(IReadOnlyDictionary<string, object?> fields)
    {
        var patch = ResolvePatch(fields);  // name → FieldDescriptor
        return Evaluator.Update(Precept, this, patch);
    }
    
    public EventInspection InspectFire(string eventName, IReadOnlyDictionary<string, object?>? args = null)
    {
        var @event = Precept.Events.Single(e => e.Name == eventName);
        return Evaluator.InspectFire(Precept, this, @event, args);
    }
    
    public UpdateInspection InspectUpdate(IReadOnlyDictionary<string, object?>? fields = null)
    {
        var patch = fields != null ? ResolvePatch(fields) : null;
        return Evaluator.InspectUpdate(Precept, this, patch);
    }
}
```

---

## 9. Failure Modes and Recovery

### In-Domain Failures (Expected Business Events)

All in-domain failures produce structured outcome variants — never exceptions:

| Failure | Outcome | Description |
|---|---|---|
| All guards failed | `Unmatched()` | No transition row matched; event was not valid for current state |
| Constraint violations | `EventConstraintsFailed(violations)` | Rows matched, but constraint(s) failed post-mutation |
| Access denied | `AccessDenied(field, mode)` | Update attempted on readonly/omit field |
| Undefined event | `UndefinedEvent()` | No rows declared for event in current state |
| Authored rejection | `Rejected(reason)` | A `reject` row was the winning candidate |
| Invalid args | `InvalidArgs(reason)` | Arg validation failed (wrong type, missing required) |
| Invalid input | `InvalidInput(reason)` | Patch validation failed (type mismatch, unknown field) |
| Restore mismatch | `RestoreInvalidInput(reason)` | Persisted data doesn't match definition |

These are **not errors** — they are expected business outcomes. Callers pattern-match on the outcome type to determine the appropriate response.

### Impossible-Path Failures (Defense-in-Depth)

Impossible-path failures indicate bugs in upstream stages (compiler/builder) or corrupted data:

| Failure | FaultCode | Should Be Prevented By |
|---|---|---|
| Division by zero | `DivisionByZero` | Guard `when Divisor != 0` or `nonzero` modifier |
| Sqrt of negative | `SqrtOfNegative` | Guard `when Value >= 0` or `nonnegative` modifier |
| Type mismatch at runtime | `TypeMismatch` | Static type checking |
| Undeclared field reference | `UndeclaredField` | Static name resolution |
| Null in non-nullable context | `UnexpectedNull` | `optional` modifier or null guard |
| Invalid member access | `InvalidMemberAccess` | Static type checking |
| Function arity mismatch | `FunctionArityMismatch` | Static signature checking |
| Collection empty on access | `CollectionEmptyOnAccess` | Guard `when F.count > 0` |
| Collection empty on mutation | `CollectionEmptyOnMutation` | Guard `when F.count > 0` |
| Ambiguous dispatch | (not yet in FaultCode) | Proof engine exclusivity analysis |

Every `FaultCode` carries a `[StaticallyPreventable(DiagnosticCode)]` attribute linking it to the compiler diagnostic that should have caught it:

```csharp
public enum FaultCode
{
    [StaticallyPreventable(DiagnosticCode.DivisionByZero)]
    DivisionByZero = 1,
    
    [StaticallyPreventable(DiagnosticCode.SqrtOfNegative)]
    SqrtOfNegative = 2,
    
    // ...
}
```

### Fault Production

```csharp
internal static Fault Fail(FaultCode code, params object?[] args)
    => Faults.Create(code, args);
```

`Faults.Create` looks up `FaultMeta` from the catalog and produces a fully-formed `Fault`:

```csharp
public readonly record struct Fault(
    FaultCode Code,
    string CodeName,     // nameof-derived stable identity
    string Message       // Pre-formatted diagnostic message
);
```

### Recovery Paths

| Failure Type | Recovery |
|---|---|
| In-domain failure | Caller handles the outcome appropriately (display error, retry, etc.) |
| Impossible-path fault | Log the fault with `FaultCode` and `CodeName`; investigate compiler/builder bug; do not retry |

### No Unclassified Exceptions

The evaluator does not throw exceptions for in-domain operations. Every failure path either:
- Produces a structured outcome variant (for expected failures), or
- Produces a `Fault` via `Faults.Create` (for impossible paths)

The only exception paths are truly exceptional: out-of-memory, corrupted `Precept` model, etc. These are unrecoverable and should not be caught.

---

## 10. Contracts and Guarantees

### Input Contracts

| Contract | Enforcement |
|---|---|
| `Precept` must be non-null and complete | `Precept.From` guarantees completeness; null is a caller bug |
| `Version.Precept` must match the evaluator's `Precept` | Verified at API boundary |
| `Version.Slots.Length == Precept.SlotLayout.FieldCount` | Invariant maintained by `Version` construction |
| Event descriptor must exist in `Precept.Events` | Lookup at API boundary; `UndefinedEvent` if missing |
| Arg types must match `ArgDescriptor.Type` | Validated; `InvalidArgs` if mismatch |
| Patch field types must match `FieldDescriptor.Type` | Validated; `InvalidInput` if mismatch |

### Output Guarantees

| Guarantee | Rationale |
|---|---|
| **Determinism** | Same `Precept` + same `Version` + same operation + same inputs → same outcome. No randomness, no external state. |
| **Immutability preservation** | Input `Version` is never mutated. Output `Version` (in success outcomes) is a fresh instance. |
| **Fault–diagnostic correspondence** | Every `Fault` produced by the evaluator carries a `FaultCode` with a `[StaticallyPreventable]` attribute. If the compiler emits no errors, the evaluator should never fault. |
| **Inspection–commit agreement** | `InspectFire` and `Fire` execute the same plans. A row that `InspectFire` marks `Prospect.Certain` will be the winning row when `Fire` is called with the same inputs. |
| **Constraint completeness** | All applicable constraints are evaluated. The evaluator does not short-circuit on first violation (it collects all violations for diagnostic completeness). |
| **Access mode bypass for Restore** | `Restore` bypasses access-mode checks but enforces constraint checks. |

### Slot Index Invariants

| Invariant | Description |
|---|---|
| Slot index range | `0 <= FieldDescriptor.SlotIndex < SlotLayout.FieldCount` |
| Unique slot assignment | No two `FieldDescriptor` entries share a `SlotIndex` |
| Slot order matches field order | `SlotLayout.Fields[i].SlotIndex == i` |
| Computed slots are a subset | Every slot in `SlotLayout.ComputedSlots` is a valid slot index |

### Constraint Evaluation Guarantees

| Guarantee | Description |
|---|---|
| Bucket completeness | Every `ConstraintDescriptor` appears in exactly one bucket |
| Correct bucket routing | `Always` contains only `Invariant`; `InState` contains only `StateResident`; etc. |
| No constraint skipping | All constraints in applicable buckets are evaluated, regardless of prior failures |
| Post-mutation evaluation | Constraints are evaluated against the working copy AFTER action execution and computed field recomputation |

### Inspection Guarantees

| Guarantee | Description |
|---|---|
| All rows inspected | `InspectFire` evaluates every declared row, not just the first match |
| Consistent projections | `RowInspection.ResultingFields` shows the exact values that would exist post-commit |
| Hypothetical correctness | `RowInspection.HypotheticalResult` is a valid `Version` if and only if all constraints pass |
| Event-prospect accuracy | `UpdateInspection.Events` reflects event availability against the hypothetical post-patch state |

---

## 11. Design Rationale and Decisions

### Decision 1: Plan Executor, Not Semantic Reasoner

**Decision:** The evaluator contains zero language knowledge. All routing, classification, and constraint-bucket decisions are prebuilt by the Precept Builder.

**Rationale:**
- Eliminates the evaluator as a location for semantic drift
- Language changes require only catalog + builder changes; the evaluator is untouched
- Reduces evaluator complexity to pure execution mechanics
- Enables confident testing: if the builder produces correct plans, the evaluator applies them correctly

**Alternative rejected:** *Inline semantic reasoning* — rejected because it duplicates language knowledge between the type checker, builder, and evaluator, creating three locations where semantic bugs could occur.

### Decision 2: Flat Opcodes Over Recursive AST Walking

**Decision:** Expression plans are compiled to flat opcode arrays (stack machine), not evaluated by walking a recursive AST.

**Rationale:**
- **Cache-friendly:** Opcodes are contiguous in memory
- **O(n) execution:** One pass through the opcode array
- **No stack depth limits:** Deeply nested expressions don't cause stack overflow
- **Trivially inspectable:** The opcode array can be logged/traced opcode-by-opcode
- **Predictable performance:** No hidden allocation, no call stack growth

**Alternative rejected:** *Tree-walking interpreter* — rejected because recursive dispatch has unpredictable stack usage, poor cache locality, and harder debugging.

**Alternative rejected:** *JIT compilation* — rejected as over-engineering for Precept's use case; adds complexity and startup latency for marginal runtime gains on simple expressions.

### Decision 3: Inspection and Commit Share the Same Plans

**Decision:** `InspectFire` and `Fire` execute the same `ExecutionPlan` and `ConstraintPlanIndex` structures. There is no second evaluator or second code path for inspection.

**Rationale:**
- **Inspection cannot disagree with commit:** If they share code paths, they cannot diverge
- **Single source of truth:** One constraint evaluation implementation, tested once
- **Simpler testing:** Test `Fire`; inspection correctness follows automatically
- **Progressive disclosure:** Inspection is the same computation with different disposition (report vs. enforce)

**Alternative rejected:** *Separate inspection evaluator* — rejected because maintaining two evaluators that must agree is error-prone and wasteful.

### Decision 4: Structured Outcomes, Never Exceptions

**Decision:** All in-domain failures return typed outcome variants (`Rejected`, `AccessDenied`, `Unmatched`, etc.). The evaluator never throws exceptions for expected business events.

**Rationale:**
- **Composable results:** Callers pattern-match on outcomes; no try/catch required
- **Exhaustive handling:** Sealed outcome hierarchies + switch expressions ensure all cases are handled
- **No exception overhead:** Pattern matching is cheaper than exception handling
- **Clear semantics:** "Constraint violation" is a business event, not an exceptional condition

**Alternative rejected:** *Exception-based error handling* — rejected because it conflates "the business rule said no" with "something is broken," leading to poor error handling patterns.

### Decision 5: Restore Bypasses Access Modes, Not Constraints

**Decision:** `Restore` bypasses access-mode checks but enforces constraint checks.

**Rationale:**
- **Access modes are interactive controls:** They govern what the user can edit, not what values are valid
- **Constraints are correctness invariants:** They define what must always be true
- **Different bypass rules for good reason:** A persisted snapshot should be restorable if its values are correct, even if those values are in fields the user couldn't edit interactively
- **Schema evolution:** If access modes change between versions, old snapshots should still restore correctly

**Alternative rejected:** *Bypass all checks for Restore* — rejected because constraints are correctness invariants; violating them means the data is invalid, not just inaccessible.

### Decision 6: Computed Field Recomputation Before Constraint Evaluation in Restore

**Decision:** `Restore` recomputes computed fields BEFORE evaluating constraints.

**Rationale:**
- **Stale values in persisted snapshots:** A snapshot might contain a computed field value that was correct when saved but is stale relative to the underlying fields
- **Constraint evaluation needs current values:** Constraints that reference computed fields must see the recomputed values, not stale ones
- **Graceful version migration:** If a computed field's formula changes, old snapshots with stale values should be restorable if the underlying data is valid

**Example:** A snapshot with `total = 100` but `items.sum = 95` should restore successfully (the constraint sees the recomputed `total = 95`), not fail with a stale constraint violation.

### Decision 7: Collect All Violations, Don't Short-Circuit

**Decision:** Constraint evaluation collects all violations, not just the first one.

**Rationale:**
- **Diagnostic completeness:** Users see all problems at once, not one-at-a-time whack-a-mole
- **Better tooling support:** AI agents and UIs can present a complete picture
- **No performance cost:** Constraints are typically few; collecting all is negligible overhead

**Alternative rejected:** *Short-circuit on first violation* — rejected because it provides poor user experience and no meaningful performance benefit.

---

## 12. Innovation

### Plan Execution, Not Tree-Walking

Traditional expression evaluators walk recursive ASTs. Precept compiles expressions to flat, cache-friendly slot-addressed opcodes with a `stackalloc` operand stack and `PreceptValue`-typed currency:

```csharp
// Traditional tree-walking — object? everywhere, unbounded recursion, heap boxing
object? Evaluate(Expr node) => node switch
{
    BinaryExpr(var l, var op, var r) => ApplyOp(op, Evaluate(l), Evaluate(r)),
    FieldRef(var name) => fields[name],
    Literal(var v) => v,
    // ... recursive calls, stack depth grows with expression depth, boxing on every value
};

// Precept's flat opcode execution — PreceptValue, stackalloc, catalog-delegate dispatch
Span<PreceptValue> stack = stackalloc PreceptValue[plan.MaxStackDepth];
int top = 0;
foreach (var op in plan.Opcodes)
{
    switch (op)
    {
        case LoadSlot(var i): stack[top++] = slots[i]; break;
        case BinaryOp(var kind):
            PreceptValue r = stack[--top]; PreceptValue l = stack[--top];
            stack[top++] = Operations.BinaryExecutors[(int)kind](l, r);  // catalog-owned, zero-knowledge
            break;
        // ... O(n) iteration, no recursion, no boxing, no heap traffic
    }
}
```

This makes evaluation predictable-time, cache-friendly, and trivially traceable.

### Inspection Shares Commit Plans

Most systems have separate "preview" and "execute" paths. Precept's inspection API runs the **same plans** as the commit path — only disposition differs:

| Mode | Guard | Actions | Constraints | Disposition |
|---|---|---|---|---|
| Commit | Evaluate | Execute | Evaluate | Fail/succeed |
| Inspect | Evaluate | Execute (on copy) | Evaluate | Report all |

This guarantees that inspection cannot disagree with commit. A row that inspection marks `Certain` will be the winning row when committed.

### Structured Outcome Taxonomy

Every operation returns a discriminated union outcome. Callers pattern-match; exhaustiveness is compiler-enforced:

```csharp
var outcome = version.Fire("submit", args);
var result = outcome switch
{
    Transitioned(var newVersion) => HandleSuccess(newVersion),
    EventConstraintsFailed(var violations) => HandleViolations(violations),
    Unmatched() => HandleNoMatch(),
    // Compiler warning if any case is missing
};
```

No exceptions, no error codes, no magic strings. Every failure mode is a first-class type.

### Causal Violation Explanations

`ConstraintViolation` carries structured explanation depth:

```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,         // Which constraint failed
    string? BecauseClause,                   // "because inventory cannot go negative"
    ImmutableArray<FieldSnapshot> RelevantFields,  // What were the field values?
    string? FailingSubexpression,            // "quantity - sold"
    object? FailingValue                     // "-5"
);
```

This enables AI agents and UIs to explain WHY the constraint failed, not just THAT it failed.

### Recomputation-First Restore Semantics

Restore recomputes computed fields BEFORE constraint evaluation:

```text
Traditional restore:
  1. Load persisted fields (including stale computed values)
  2. Evaluate constraints (may fail on stale computed values)
  ❌ Snapshot with stale computed field → constraint violation

Precept restore:
  1. Load persisted fields
  2. Recompute computed fields (fresh values from underlying data)
  3. Evaluate constraints (sees correct computed values)
  ✅ Snapshot with stale computed field → restored successfully
```

This enables graceful schema evolution: if a computed field's formula changes, old snapshots restore correctly as long as the underlying data is valid.

### Event-Prospect Evaluation in Update Inspection

`InspectUpdate` doesn't just show constraint results — it shows what events would become available:

```csharp
var inspection = version.InspectUpdate(new { quantity = 10 });

// What events can I fire after this update?
foreach (var eventInspection in inspection.Events)
{
    Console.WriteLine($"{eventInspection.EventName}: {eventInspection.OverallProspect}");
}
// Output:
// submit: Certain (guard now passes)
// cancel: Possible (some rows match)
// archive: Impossible (still blocked by constraint)
```

This enables progressive UIs: show users what actions become available as they fill in fields.

---

## 13. Open Questions / Implementation Notes

### Implementation Status

1. **All operation bodies throw `NotImplementedException`** — `Fail(FaultCode)` routing to `Faults.Create` is the only implemented path.

2. **Descriptor types are defined but pending full implementation** — `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor` exist in `Descriptors.cs`; shapes need `AccessModes` and computed plan references.

3. **Opcode types are designed but not implemented** — The `Opcode` DU and its subtypes are specified in `precept-builder.md` but no C# types exist yet.

4. **Working copy allocation strategy undefined** — Is the slot array stack-allocated (via `stackalloc` for small precepts) or always heap-allocated?

### Resolved Design Questions

5. **Constraint evaluation order** — Constraints are evaluated in bucket order: `always` → `from<current>` → `on<event>` → `to<target>`. Within a bucket, order is declaration order (stable, debuggable).

6. **Restore recomputation ordering** — Confirmed: recompute computed fields BEFORE constraint evaluation. This is a hard constraint in the evaluator, not just an execution plan detail.

7. **Inspection–commit plan sharing** — Confirmed: same plans, same execution. Disposition (report vs. enforce) is the only difference.

8. **Fault backstop threading** — Every backstop site must carry a resolved `FaultSiteDescriptor` from the `Precept` model. The evaluator calls `Faults.Create(code, args)`, not a raw throw.

### Pending Design Questions

> **Open Question (unresolved):** Should `Version.Slots` be `ImmutableArray<object?>` or `object?[]`? `ImmutableArray` provides snapshot semantics but has allocation overhead for copy-on-write. `object?[]` with explicit cloning may be more efficient but requires discipline.

> **Open Question (unresolved):** Confirm the shape of `FieldDescriptor.AccessModes`. Current design assumes `ImmutableDictionary<StateDescriptor?, AccessMode>`. Alternative: flat array `ImmutableArray<(StateDescriptor?, AccessMode)>` for smaller field counts.

> **Open Question (unresolved):** `InspectUpdate` event-prospect evaluation runs `InspectFire` over the hypothetical post-patch state. Should this use a temporary `Version` object, or operate directly on the working copy? Using a `Version` is cleaner but allocates; operating on the working copy requires parameter surgery.

> **Open Question (unresolved):** The evaluator currently has no `AmbiguousDispatch` `FaultCode`. This impossible-path failure (multiple candidates in first-match) needs a fault code with `[StaticallyPreventable]` linking to the proof engine's exclusivity analysis.

> **Open Question (unresolved):** Should the evaluator cache `ActionMeta.SyntaxShape` lookups, or are they fast enough to call per-action? The catalog lookup is O(1) via switch, but calling it N times per transition may be measurable for action-heavy events.

### Implementation Notes

5. **`ConstraintViolation` full shape not yet implemented** — Currently only `ConstraintDescriptor` + `IReadOnlyList<string> FieldNames`. Full shape needs:
   - Evaluated field values (`FieldSnapshot[]`)
   - Guard context (if the constraint was guarded)
   - Failing sub-expression text
   - Failing value that caused the failure
   
   This is the designed contract; implementation is blocked on the evaluator.

6. **Event-prospect evaluation in `InspectUpdate`** — Runs `InspectFire` over all events available in the current state, using the hypothetical post-patch field values. This may be expensive for precepts with many events; consider lazy evaluation.

7. **Working copy isolation** — Each candidate row evaluation must have its own working copy. Rows are evaluated in order; a failing row's mutations must not leak to subsequent rows.

---

## 14. Deliberate Exclusions

### No Semantic Reasoning at Dispatch Time

The evaluator reads prebuilt indexes, not catalogs. It does not:
- Call `Actions.GetMeta()` to determine how to execute an action (uses `ActionPlan.Kind` + prebuilt shape)
- Call `Operations.GetMeta()` to determine how to apply an operator (uses `BinaryOp(kind)` opcode)
- Call `Constraints.GetMeta()` to classify constraints (buckets were built by Precept Builder)
- Call `Types.GetAccessor()` to resolve member access (uses `MemberAccess(accessor)` opcode)

All catalog metadata was consumed at build time. The evaluator is catalog-free at runtime.

### No General Expression Interpreter

The evaluator does not interpret arbitrary expressions. It executes prebuilt `ExecutionPlan` opcode arrays:
- No expression parsing
- No type resolution
- No operator overload resolution
- No function lookup

If you need to evaluate an arbitrary expression, you must compile it first (via the full pipeline) and then execute the resulting `ExecutionPlan`.

### No Plan Building

`ExecutionPlan` construction is the Precept Builder's domain. The evaluator:
- Receives plans, does not build them
- Does not modify plans
- Does not generate opcodes

This separation ensures the evaluator is stateless and pure — it transforms `(Precept, Version, args)` → `Outcome` with no side effects.

### No Incremental Evaluation

The evaluator does not support:
- Incremental constraint evaluation (only changed fields)
- Memoization across operations
- Cached intermediate results

Every operation evaluates all applicable constraints from scratch. The constraint set is typically small enough that incremental evaluation provides no benefit and adds complexity.

### No Concurrent Execution

The evaluator is single-threaded by design:
- No parallel constraint evaluation
- No concurrent row evaluation
- No async execution

Operations are fast (O(fields × constraints)); parallelism would add overhead without benefit. If concurrency is needed, it happens at a higher level (multiple Versions processed in parallel).

### No Side Effects

The evaluator is pure:
- No I/O (logging, network, file system)
- No external service calls
- No global state mutation
- No randomness

The same inputs always produce the same outputs. This enables confident testing and reproducible behavior.

---

## 15. Cross-References

| Topic | Document |
|---|---|
| Precept Builder (produces executable model) | `docs/runtime/precept-builder.md` |
| Executable model structure | `docs/runtime/precept-builder.md §6–7` |
| Opcode inventory and ExecutionPlan design | `docs/runtime/precept-builder.md §7.5` |
| Constraint anchor classification | `docs/runtime/precept-builder.md §7.4` |
| Descriptor type shapes | `docs/runtime/descriptor-types.md` |
| Outcome and inspection type shapes | `docs/runtime/result-types.md` |
| Runtime API surface design | `docs/runtime/runtime-api.md` |
| Catalog system architecture | `docs/language/catalog-system.md` |
| Constraint catalog and DU subtypes | `docs/language/catalog-system.md §Constraints` |
| Actions catalog and SyntaxShape | `docs/language/catalog-system.md §Actions` |
| Fault–diagnostic correspondence | `docs/compiler/proof-engine.md §Fault Backstops` |
| MCP tools that invoke the evaluator | `docs/mcp/McpServerDesign.md §precept_fire, §precept_update, §precept_inspect` |

### Constraint Evaluation Matrix

This matrix summarizes which constraint buckets and access-mode checks apply to each operation:

| Operation | Access-mode checks | Row dispatch | Constraint plans evaluated |
|---|---|---|---|
| `Fire` | no | yes | `always`, `from <current>`, `on <event>`, `to <target>` |
| `InspectFire` | no | yes (all rows) | same as `Fire`, but evaluated for every row |
| `Update` | yes | no | `always`, `in <current>` |
| `InspectUpdate` | yes (reported) | no | same as `Update`, plus event-prospect over hypothetical state |
| `Create` with initial event | no | yes (initial event) | same as `Fire` for the initial event |
| `Create` without initial event | no | no | `always`, `in <initial state>` |
| `Restore` | no (bypassed) | no | `always`, `in <current>` — computed fields recomputed first |

**Key rules:**
- Restore bypasses access-mode checks but enforces constraint checks
- Inspection and commit paths execute the same prebuilt plans — disposition alone differs
- `to <State>` constraints are evaluated only during Fire, not Update or Restore

---

## 16. Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Evaluator.cs` | Evaluator implementation — static class with Fire, Update, Restore, InspectFire, InspectUpdate methods |
| `src/Precept/Runtime/Precept.cs` | Executable model — Create, InspectCreate, Restore entry points |
| `src/Precept/Runtime/Version.cs` | Entity instance — Fire, Update, InspectFire, InspectUpdate façade |
| `src/Precept/Runtime/EventOutcome.cs` | Fire and Create outcome DU |
| `src/Precept/Runtime/UpdateOutcome.cs` | Update outcome DU |
| `src/Precept/Runtime/RestoreOutcome.cs` | Restore outcome DU |
| `src/Precept/Runtime/Inspection.cs` | Inspection types (`EventInspection`, `UpdateInspection`, `RowInspection`, `ConstraintResult`, `FieldSnapshot`) |
| `src/Precept/Runtime/SharedTypes.cs` | `ConstraintViolation`, `ConstraintDescriptor`, `FieldAccessInfo`, `FieldAccessMode` |
| `src/Precept/Runtime/Descriptors.cs` | `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `FaultSiteDescriptor` |
| `src/Precept/Language/FaultCode.cs` | `FaultCode` enum with `[StaticallyPreventable]` attributes |
| `src/Precept/Language/Faults.cs` | `Faults.Create()` factory and `FaultMeta` catalog |
| `src/Precept/Language/Fault.cs` | `Fault` record struct |
| `src/Precept/Language/Actions.cs` | `ActionMeta` and `ActionSyntaxShape` — action dispatch classification |
| `src/Precept/Language/Constraints.cs` | `ConstraintMeta` DU — constraint anchor classification |
| `src/Precept/Language/Constraint.cs` | `ConstraintMeta` base and subtypes (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`) |

### Planned Source Files (Not Yet Implemented)

| File | Purpose |
|---|---|
| `src/Precept/Runtime/ExecutionPlan.cs` | `ExecutionPlan`, `Opcode` DU, `ExecutionRow`, `ActionPlan` |
| `src/Precept/Runtime/SlotLayout.cs` | `SlotLayout` register file specification |
| `src/Precept/Runtime/DispatchIndex.cs` | `TransitionDispatchIndex`, `ReachabilityIndex` |
| `src/Precept/Runtime/ConstraintPlanIndex.cs` | `ConstraintPlanIndex` with five anchor-family buckets |
| `src/Precept/Runtime/ConstraintInfluenceMap.cs` | Bidirectional constraint↔field dependency index |
