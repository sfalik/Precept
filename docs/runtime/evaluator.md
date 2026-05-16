# Evaluator

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Stub (`Evaluator.cs` exists; `Fail(FaultCode)` routes through `Faults.Create`; all operation bodies throw `NotImplementedException`) |
| Source | `src/Precept/Runtime/Evaluator.cs` |
| Upstream | `Precept` (executable model), `Version` (entity instance) |
| Downstream | `EventOutcome`, `UpdateOutcome`, `EventInspection`, `UpdateInspection` |

> [!IMPORTANT]
> **Non-Negotiable Rules — Read Before Implementing**
>
> - **`Version` is the primary runtime input** — every evaluator operation starts from an immutable snapshot of entity state at the moment of the operation. See [§Primary Input: The Version Type](#primary-input-the-version-type) and [§10. Contracts and Guarantees](#10-contracts-and-guarantees).
> - **Mutate only through a working copy** — all writes happen on a working copy and the input `Version` is never modified in place. See [§Working Copy Management](#working-copy-management) and [§7.1 The Four Operations](#71-the-four-operations).
> - **One opcode engine powers every operation** — `Create`, `Fire`, `Update`, and `Restore` all execute through the same opcode execution machinery, with inspection reusing the same plan set. See [§7.1 The Four Operations](#71-the-four-operations), [§7.2 Inspection Operations](#72-inspection-operations), and [§7.3 Opcode Execution Engine](#73-opcode-execution-engine).
> - **No diagnostics means no evaluator faults** — compiler proofs are intended to eliminate runtime fault paths, leaving evaluator faults as defense-in-depth only. See [§9. Failure Modes and Recovery](#9-failure-modes-and-recovery) and [§10. Contracts and Guarantees](#10-contracts-and-guarantees).

## Contents

- [2. Overview](#2-overview)
- [3. Responsibilities and Boundaries](#3-responsibilities-and-boundaries)
  - [In Scope](#in-scope)
  - [Out of Scope](#out-of-scope)
- [4. Right-Sizing](#4-right-sizing)
  - [Approaches Considered and Rejected](#approaches-considered-and-rejected)
- [5. Inputs and Outputs](#5-inputs-and-outputs)
  - [Primary Input: The Version Type](#primary-input-the-version-type)
  - [PreceptValue: Evaluation Currency](#preceptvalue-evaluation-currency)
  - [Input Summary](#input-summary)
  - [Output: Commit Operations](#output-commit-operations)
  - [Output: Inspection Operations](#output-inspection-operations)
- [6. Architecture](#6-architecture)
  - [Evaluator Structure](#evaluator-structure)
  - [Execution Flow Diagram](#execution-flow-diagram)
  - [Working Copy Management](#working-copy-management)
- [7. Component Mechanics](#7-component-mechanics)
  - [7.0 Evaluation Stack Allocation](#70-evaluation-stack-allocation)
  - [7.1 The Four Operations](#71-the-four-operations)
  - [7.2 Inspection Operations](#72-inspection-operations)
  - [7.3 Opcode Execution Engine](#73-opcode-execution-engine)
  - [7.4 Action Dispatch](#74-action-dispatch)
  - [7.4.1 Collection Internals](#741-collection-internals)
  - [7.5 Access Mode Enforcement](#75-access-mode-enforcement)
  - [7.6 Constraint Evaluation](#76-constraint-evaluation)
- [8. Dependencies and Integration Points](#8-dependencies-and-integration-points)
  - [Upstream Dependencies](#upstream-dependencies)
  - [Internal Dependencies (within the Precept model)](#internal-dependencies-within-the-precept-model)
  - [Downstream Consumers](#downstream-consumers)
  - [Integration Contract](#integration-contract)
  - [Business-Domain Internal Types](#business-domain-internal-types)
- [9. Failure Modes and Recovery](#9-failure-modes-and-recovery)
  - [In-Domain Failures (Expected Business Events)](#in-domain-failures-expected-business-events)
  - [Impossible-Path Failures (Defense-in-Depth)](#impossible-path-failures-defense-in-depth)
  - [Fault Production](#fault-production)
  - [Recovery Paths](#recovery-paths)
  - [No Unclassified Exceptions](#no-unclassified-exceptions)
- [10. Contracts and Guarantees](#10-contracts-and-guarantees)
  - [Input Contracts](#input-contracts)
  - [Output Guarantees](#output-guarantees)
  - [Slot Index Invariants](#slot-index-invariants)
  - [Constraint Evaluation Guarantees](#constraint-evaluation-guarantees)
  - [Inspection Guarantees](#inspection-guarantees)
- [11. Design Rationale and Decisions](#11-design-rationale-and-decisions)
  - [Decision 1: Plan Executor, Not Semantic Reasoner](#decision-1-plan-executor-not-semantic-reasoner)
  - [Decision 2: Flat Opcodes Over Recursive AST Walking](#decision-2-flat-opcodes-over-recursive-ast-walking)
  - [Decision 3: Inspection and Commit Share the Same Plans](#decision-3-inspection-and-commit-share-the-same-plans)
  - [Decision 4: Structured Outcomes, Never Exceptions](#decision-4-structured-outcomes-never-exceptions)
  - [Decision 5: FromJson Bypasses Constraint Validation](#decision-5-fromjson-bypasses-constraint-validation)
  - [Decision 6: FromJson is the Evaluator's Inverse, Not a Pipeline](#decision-6-fromjson-is-the-evaluators-inverse-not-a-pipeline)
  - [Decision 7: Collect All Violations, Don't Short-Circuit](#decision-7-collect-all-violations-dont-short-circuit)
  - [Decision 8: Single Interpreter with Diagnostic Trace — No Dual-Path](#decision-8-single-interpreter-with-diagnostic-trace--no-dual-path)
  - [Decision 9: Universal `PreceptValue[]` Backing for All 9 Collection Kinds](#decision-9-universal-preceptvalue-backing-for-all-9-collection-kinds)
  - [Decision 10: `CollectionActions` as a Static Stateless Helper Class](#decision-10-collectionactions-as-a-static-stateless-helper-class)
  - [Decision 11: Evaluator-Owned Copy-on-Write for Multi-Mutation Events](#decision-11-evaluator-owned-copy-on-write-for-multi-mutation-events)
  - [Decision 12: Embedded Delegates, Not Global Array](#decision-12-embedded-delegates-not-global-array)
  - [Decision 13: Dual-Shape API Boundary — Entity vs. Proxy Struct](#decision-13-dual-shape-api-boundary--entity-vs-proxy-struct)
  - [Decision 14: `PreceptValue` Is Strictly Internal — Never in Public API Signatures](#decision-14-preceptvalue-is-strictly-internal--never-in-public-api-signatures)
- [12. Innovation](#12-innovation)
  - [Plan Execution, Not Tree-Walking](#plan-execution-not-tree-walking)
  - [Inspection Shares Commit Plans](#inspection-shares-commit-plans)
  - [Structured Outcome Taxonomy](#structured-outcome-taxonomy)
  - [Causal Violation Explanations](#causal-violation-explanations)
  - [Computed Field Freshness in Restored Versions](#computed-field-freshness-in-restored-versions)
  - [Event-Prospect Evaluation in Update Inspection](#event-prospect-evaluation-in-update-inspection)
- [13. Implementation Notes](#13-implementation-notes)
  - [Implementation Status](#implementation-status)
  - [Resolved Design Questions](#resolved-design-questions)
- [14. Deliberate Exclusions](#14-deliberate-exclusions)
  - [No Semantic Reasoning at Dispatch Time](#no-semantic-reasoning-at-dispatch-time)
  - [No General Expression Interpreter](#no-general-expression-interpreter)
  - [No Plan Building](#no-plan-building)
  - [No Incremental Evaluation](#no-incremental-evaluation)
  - [No Concurrent Execution](#no-concurrent-execution)
  - [No Side Effects](#no-side-effects)
- [15. Cross-References](#15-cross-references)
  - [Constraint Evaluation Matrix](#constraint-evaluation-matrix)
- [16. Source Files](#16-source-files)
  - [Planned Source Files (Not Yet Implemented)](#planned-source-files-not-yet-implemented)

---

## 2. Overview

The evaluator is the **pure plan executor** — it consumes prebuilt execution structures from the `Precept` model and drives all runtime operations. It performs no semantic reasoning, no catalog lookups, and no name resolution at execution time. All routing and classification decisions were made at build time by the Precept Builder.

**Pipeline Position:**

```text
Precept (executable model) + Version (entity instance) → Evaluator → EventOutcome / UpdateOutcome / EventInspection / UpdateInspection
```

The evaluator's inner loop is: look up a prebuilt plan, walk its opcode array, evaluate constraints from prebuilt buckets, produce a structured outcome. This makes the evaluator the smallest it can be while being correct — a tight O(n) loop with no conditional branching on language features.

**Key design identity:** The evaluator is to execution what the parser is to syntax — it mechanically applies prebuilt structures without semantic interpretation. The Precept Builder encodes all language knowledge into dispatch indexes, constraint buckets, and execution plans. The evaluator reads these structures and executes them.

**Four operations, two modes:**

| Operation | Mode | Description |
|---|---|---|
| `Create` | Commit | Construct initial entity instance |
| `Fire` | Commit | Execute event, apply mutations, transition state |
| `Update` | Commit | Apply field patch, enforce access modes and constraints |
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
| **Structured outcome production** | Produce `EventOutcome`, `UpdateOutcome` discriminated unions — never throw for in-domain failures |
| **Inspection production** | Produce `EventInspection`, `UpdateInspection` with full row-by-row and constraint-by-constraint detail |
| **Fault backstop routing** | At impossible-path sites: check `opcode.FaultSite` (CC#6 `FaultSiteAnnotation?`); null = no check; non-null → `Faults.Create(annotation, context)` |

### Out of Scope

| Exclusion | Rationale |
|---|---|
| **Semantic reasoning** | All semantic decisions were made by the type checker and encoded by the Precept Builder. The evaluator reads prebuilt structures. |
| **Catalog lookups** | The evaluator never calls `Actions.GetMeta()`, `Operations.GetMeta()`, or `Constraints.GetMeta()`. Catalog metadata is read at build time, not execution time. |
| **Name resolution** | Field names → slot indexes; event names → `EventDescriptor`; all resolved at build time. The evaluator uses descriptor identity and slot indexes. |
| **Dispatch index building** | The Precept Builder builds `TransitionDispatchIndex`, `ConstraintPlanIndex`, etc. The evaluator only reads them. |
| **Outcome type definitions** | `EventOutcome`, `UpdateOutcome` are defined in their own source files — the evaluator produces instances, doesn't define the types. |
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

The evaluator is a static class with pure functions — aligned with the pipeline pattern (`Lexer.Lex`, `Parser.Parse`, `NameBinder.Bind`, `TypeChecker.Check`). `Version` is a thin façade: it holds identity (Precept + state + slots) and delegates to `Evaluator`. This separation keeps `Version` focused on identity and the evaluator focused on execution mechanics.

**Why not merge into `Precept`?**

`Precept` is the executable model — an immutable artifact that describes what operations are possible. The evaluator applies those operations to a specific `Version`. Merging them would conflate definition (what can happen) with execution (what does happen).

**Bounded complexity:**

The evaluator has no algorithmic complexity beyond iteration. It does not compute fixpoints, solve constraints, or traverse graphs. Every loop is O(n) over a prebuilt array: opcodes, rows, constraints, actions. The only conditional branching is guard evaluation and constraint checking — both read prebuilt plans.

### Approaches Considered and Rejected

**TypeBuilder rejected:** The team considered using `TypeBuilder` (a dynamic code-generation approach via `System.Reflection.Emit`) to generate per-type evaluation dispatch at startup. TypeBuilder's warm-path throughput and earlier executor validation are real advantages, but they do not survive the actual product constraints: (1) the blocking constraint is SaaS cold-start and per-definition churn — hundreds of milliseconds of compile work on upload, cache miss, or deployment is incompatible with the save-and-test loop, while A+G stays sub-millisecond to stand up; (2) inspectability is a product guarantee, not an optional debugger convenience — TypeBuilder would require a second interpreted or tracing-decorator path to recover per-step explanations that A+G already exposes naturally; (3) same-process deployment means `TypeRuntimeMeta` executor arrays are JIT-warm and fixed for the process lifetime, making TypeBuilder's warm-path throughput advantage irrelevant in practice. The builder embeds `static readonly Func<PreceptValue, PreceptValue, PreceptValue>` delegates directly in opcodes at compile time — equivalent in performance to generated code without the complexity cost.

**Upgrade seam toward compilation:** The A+G interpreter is the canonical execution path. The design deliberately leaves a seam for future compiled execution: because all dispatch is pre-indexed (slot arrays, `OperationKind` ordinals, `FiredArgs` slot indexes, prebuilt `ExecutionPlan` opcode arrays), a future compiled path could consume the same `Precept` model without requiring any changes to the evaluator's input types. The upgrade seam is the `Precept` model itself — a compiled executor would read the same `DispatchIndex`, `SlotLayout`, `ConstraintPlanIndex`, and `ExecutionPlan` structures. This was a deliberate design choice: the interpreter is not a dead-end; it is a working implementation of the same contract a compiled path would implement.

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

**Struct layout — target shape:**

The 32-byte size and tagged-value-struct shape are locked. The exact internal field layout (tag type, union field offsets, which types use which union region) is a pending implementation decision to be settled before the opcode executor implementation pass.

```csharp
// SHAPE LOCKED — INTERNAL LAYOUT PENDING DECISION
[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct PreceptValue
{
    // Byte 0: type discriminant tag
    // Bytes 8–23: union payload (decimal, long, reference region)
    // Bytes 24–31: reserved / padding
}
```

**Rejected alternative — type-per-lane storage (Option F):** A split-lane model was analyzed where scalar types occupied a compact value lane and reference types occupied a separate reference lane. This was rejected: 23 of 32 `TypeKind` members still live in the reference lane regardless of the split, and business-domain types remain cross-lane participants. Adding a wider business-value lane only recreates `PreceptValue`'s struct-copy cost without gaining the unified operation surface that makes A+G simple. A NodaTime/date-time re-analysis changed some lane membership details but did not change the verdict — the split adds routing complexity for no meaningful reduction in cross-lane operations.

### Input Summary

| Input | Source | Description |
|---|---|---|
| `Precept` | `Version.Precept` | The executable model — descriptor tables, dispatch indexes, constraint plan indexes, execution plans, fault backstops |
| `Version` | Operation input | Current state + field slot array |
| `EventDescriptor` | Fire/InspectFire | The event being fired (resolved from name at API boundary) |
| `args` | Fire/InspectFire | Event argument values keyed by `ArgDescriptor.Name` |
| `patch` | Update/InspectUpdate | Field values to apply, keyed by field name (resolved to slot index at API boundary) |

### Output: Commit Operations

**EventOutcome** (returned by `Fire` and `Create`):

```csharp
public abstract record EventOutcome
{
    // CC#23: Mutations attached directly to success variants — evaluator diffs working copy against original slots
    public sealed record Transitioned(Version Result, FiredArgs Args, ImmutableArray<FieldMutation> Mutations) : EventOutcome;  // State change succeeded
    public sealed record Applied(Version Result, FiredArgs Args, ImmutableArray<FieldMutation> Mutations) : EventOutcome;       // No-transition row or stateless event succeeded
    public sealed record Rejected(string Reason, FiredArgs Args) : EventOutcome;       // Authored reject row matched
    public sealed record InvalidArgs(string Reason) : EventOutcome;                    // Arg validation failure
    public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation> Violations) : EventOutcome;
    // CC#24: EvaluatedRows carries per-candidate TransitionInspection for tooling debug trace
    public sealed record Unmatched(ImmutableArray<TransitionInspection> EvaluatedRows) : EventOutcome;
    public sealed record UndefinedEvent() : EventOutcome;                              // No rows for event in current state
    public sealed record Faulted(Fault Fault) : EventOutcome;                          // CC#12: impossible-path failure surfaced as structured outcome
}
```

**`FieldMutation` record** (used by `Transitioned` and `Applied`):

```csharp
// CC#23: field-level diff computed by evaluator during action execution
public sealed record FieldMutation(
    string FieldName,
    JsonElement? Before,    // slot value serialized to JSON before action execution
    JsonElement? After      // slot value serialized to JSON after action execution
);
```

**UpdateOutcome** (returned by `Update`):

```csharp
public abstract record UpdateOutcome
{
    public sealed record Updated(Version Result) : UpdateOutcome;               // Patch applied, constraints passed
    public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation> Violations) : UpdateOutcome;
    public sealed record FieldNotEditable(string FieldName, FieldAccessMode ActualMode) : UpdateOutcome;
    public sealed record InvalidFields(string Reason) : UpdateOutcome;          // Type mismatch or structural error
}
```

### Output: Inspection Operations

**EventInspection** (returned by `InspectFire`):

```csharp
public sealed record EventInspection(
    string EventName,                                          // Self-describing — no name-patching needed by consumers
    Prospect OverallProspect,                                  // Certain | Possible | Impossible
    ImmutableArray<ArgDescriptor> DeclaredArgs,               // Arg contract from EventDescriptor.ArgDescriptors
    ImmutableArray<ArgError> ArgErrors,                        // Non-empty when provided args are structurally invalid
    ImmutableArray<FieldSnapshot> CurrentFields,              // Pre-mutation field state — captured once before row loop
    ImmutableArray<TransitionInspection> Transitions,         // Per-row inspection detail
    ImmutableArray<ConstraintResult> EventEnsures             // Event-level constraint results
);
```

**TransitionInspection** (per transition row within `EventInspection`):

```csharp
public sealed record TransitionInspection(
    Prospect Prospect,                                         // Would this row fire?
    RowEffect Effect,                                          // TransitionTo | NoTransition | Rejection
    string? GuardSummary,                                      // null = guard passed / no guard; populated = guard failed or ambiguous
    ImmutableArray<ConstraintResult> Constraints,             // Constraint evaluation results
    ImmutableArray<FieldSnapshot> PostFields                  // Projected field values post-mutation
);

public abstract record RowEffect
{
    public sealed record TransitionTo(string TargetState) : RowEffect;
    public sealed record NoTransition() : RowEffect;
    public sealed record Rejection(string Reason) : RowEffect;
}

public sealed record ArgError(
    string ArgName,
    string Reason);

public enum Prospect { Certain = 1, Possible = 2, Impossible = 3 }
public enum ConstraintStatus { Satisfied = 1, Violated = 2, Unresolvable = 3 }
```

**UpdateInspection** (returned by `InspectUpdate`):

```csharp
public sealed record UpdateInspection(
    ImmutableArray<FieldSnapshot> Fields,              // Projected field values post-patch
    ImmutableArray<ConstraintResult> Constraints,      // Constraint evaluation results
    ImmutableArray<EventInspection> Events             // Event-prospect: what events would be available?
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
│  Zero candidates → return Unmatched() or EventOutcome.ConstraintsFailed(violations)   │
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

**With initial event:**

```csharp
EventOutcome Create(FiredArgs? args)
{
    // 1. Build hollow version: default slot values + initial state
    var hollowSlots = new PreceptValue[precept.SlotLayout.FieldCount];
    for (int i = 0; i < hollowSlots.Length; i++)
        hollowSlots[i] = EvaluateDefault(precept.Fields[i]);
    
    var hollow = new Version(precept, precept.InitialState, hollowSlots);
    
    // 2. Fire the initial event atomically
    return Fire(precept, hollow, precept.InitialEvent!, args ?? FiredArgs.Empty);
}
```

**Without initial event:**

```csharp
EventOutcome Create()
{
    // 1. Build version with defaults + initial state
    var slots = new PreceptValue[precept.SlotLayout.FieldCount];
    for (int i = 0; i < slots.Length; i++)
        slots[i] = EvaluateDefault(precept.Fields[i]);
    
    // 2. Recompute computed fields (no event args — use FiredArgs.Empty)
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        slots[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, slots, FiredArgs.Empty);
    
    // 3. Evaluate constraints: always + in <initial>
    var violations = EvaluateConstraintBuckets(
        precept.ConstraintPlanIndex.Always,
        precept.ConstraintPlanIndex.InState[precept.InitialState!],
        slots, FiredArgs.Empty);
    
    if (violations.Count > 0)
        return new EventOutcome.ConstraintsFailed(violations);  // Should not happen per C100/C101
    
    return new EventOutcome.Applied(new Version(precept, precept.InitialState, slots));
}
```

**Compiler guarantees:** The compiler enforces that precepts with required fields lacking defaults declare an initial event, and that the initial event assigns those fields. Therefore, `Create` without an initial event cannot produce `Rejected` for well-proven programs.

**Stateless precepts (no states declared):**

For stateless precepts, `Create` omits the initial-state assignment and all state-entry evaluations:

```csharp
EventOutcome Create()  // stateless precept — no initial event
{
    // 1. Build version with defaults (no initial state — State = null)
    var slots = new PreceptValue[precept.SlotLayout.FieldCount];
    for (int i = 0; i < slots.Length; i++)
        slots[i] = EvaluateDefault(precept.Fields[i]);

    // 2. Recompute computed fields
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        slots[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, slots, FiredArgs.Empty);

    // 3. Evaluate global constraints only (no in-state bucket — null state has none)
    var violations = EvaluateConstraintBuckets(
        precept.ConstraintPlanIndex.Always,
        slots, FiredArgs.Empty);

    if (violations.Count > 0)
        return new EventOutcome.ConstraintsFailed(violations);

    return new EventOutcome.Applied(new Version(precept, state: null, slots));
}
```

With an initial event on a stateless precept, the Fire pipeline runs normally — the hollow version is built with `State = null` and passed into `Fire`. State-entry semantics are absent because `to <State> ensure` and `in <State> ensure` require a named state; with `State = null` those constraint buckets are structurally empty.

**CC#26 locked semantics (2026-05-06):**
- State-set step omitted — `Version.State = null`, no initial state to assign
- `to <State> ensure` — not evaluated (no state entered)
- `in <State> ensure` — not evaluated (no residency state)
- Omit-on-entry clearing — not applied (no state to enter)
- Arg ensures, field constraints, global rules, computed fields, working copy protocol — all run normally

#### Fire(event, args)

```csharp
EventOutcome Fire(Precept precept, Version version, EventDescriptor @event, FiredArgs args)
{
    // 1. Look up dispatch index
    if (!precept.DispatchIndex.Rows.TryGetValue((version.CurrentState, @event), out var rows))
        return new UndefinedEvent();
    
    var candidates = new List<(ExecutionRow row, PreceptValue[] workingCopy)>();
    var allViolations = new List<ConstraintViolation>();
    
    // 2. Evaluate each row
    foreach (var row in rows)
    {
        // 2a. Guard evaluation
        if (row.Guard != null && !EvaluateGuard(row.Guard, version.Slots, args))
            continue;  // Guard failed — skip row
        
        // 2b. Snapshot working copy
        var workingCopy = version.Slots.ToArray();
        
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
            return new EventOutcome.ConstraintsFailed(allViolations);
        // CC#24: attach per-candidate trace so callers can explain why no row matched
        return new EventOutcome.Unmatched(evaluatedRowTraces.ToImmutableArray());
    }
    
    if (candidates.Count > 1)
        return Fail(FaultCode.AmbiguousDispatch);  // Impossible in proven program — proof engine emits DiagnosticCode.AmbiguousDispatch (CC#13)
    
    var (winningRow, finalSlots) = candidates[0];
    var newState = winningRow.TargetState ?? version.CurrentState;
    // CC#23: compute field-level diff against pre-mutation slots before constructing outcome
    var mutations = BuildMutations(version.Slots, finalSlots, precept.Fields);
    
    return winningRow.Outcome switch
    {
        TransitionOutcome.Transition   => new EventOutcome.Transitioned(new Version(precept, newState, finalSlots), firedArgs, mutations),
        TransitionOutcome.NoTransition => new EventOutcome.Applied(new Version(precept, version.CurrentState, finalSlots), firedArgs, mutations),
        TransitionOutcome.Reject       => new EventOutcome.Rejected(winningRow.RejectReason ?? "Rejected", firedArgs),
        _                              => throw new InvalidOperationException()
    };
}
```

`TransitionOutcome` is the canonical type name (defined in `type-checker.md` §7.1). The evaluator uses this same type at runtime.

`ExecutionRow.RejectReason` carries the lowered `because` message from an authored reject row. The field is `string? RejectReason` on `ExecutionRow` — resolved by CC#11 and mirrored in `TypedTransitionRow`. The lowered message is stored at build time by the Precept Builder and read by the evaluator at runtime with no further resolution needed.

```csharp
UpdateOutcome Update(Precept precept, Version version, PreceptValue[]? patch)
{
    // 1. Access mode checks
    if (patch != null)
    {
        for (int i = 0; i < patch.Length; i++)
        {
            if (patch[i].IsAbsent) continue;
            var field = precept.Fields[i];
            var mode = GetAccessMode(field, version.CurrentState);
            if (mode != AccessMode.Writable)
                return new UpdateOutcome.FieldNotEditable(field.Name, mode == AccessMode.ReadOnly ? FieldAccessMode.Readonly : FieldAccessMode.Omit);
        }
    }
    
    // 2. Apply patch to working copy
    var workingCopy = version.Slots.ToArray();
    if (patch != null)
    {
        for (int i = 0; i < patch.Length; i++)
            if (!patch[i].IsAbsent)
                workingCopy[i] = patch[i];  // patch is slot-indexed; IsAbsent means this slot is not being updated
    }
    
    // 3. Recompute computed fields
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, FiredArgs.Empty);
    
    // 4. Evaluate constraints: always + in <current>
    var violations = EvaluateConstraintBuckets(
        precept.ConstraintPlanIndex.Always,
        precept.ConstraintPlanIndex.InState.GetValueOrDefault(version.CurrentState),
        workingCopy, FiredArgs.Empty);
    
    if (violations.Count > 0)
        return new UpdateOutcome.ConstraintsFailed(violations);
    
    return new UpdateOutcome.Updated(new Version(precept, version.CurrentState, workingCopy));
}
```

#### FromJson (Hydration — Not an Evaluator Operation)

`FromJson` is handled at the `Precept` API boundary, not by the evaluator. It parses the persistence envelope, validates the `$precept` name, resolves the `$state`, and populates the slot array from the `fields` object — then constructs `Version` directly. No constraint validation occurs. Access modes are bypassed. The evaluator is not invoked.

See `runtime-api.md` § Restoration for the full behavioral specification.

### 7.2 Inspection Operations

#### InspectFire(event, args)

Returns `EventInspection` — one `TransitionInspection` per declared transition row for the current state/event:

```csharp
EventInspection InspectFire(Precept precept, Version version, EventDescriptor @event, FiredArgs? args)
{
    var eventName = @event.Name;
    var declaredArgs = @event.ArgDescriptors;

    // ── Arg validation gate ──────────────────────────────────────
    // Args validated before evaluator is invoked. On failure, return directly
    // with Impossible prospect and populated ArgErrors — no guard/constraint evaluation.
    if (args is not null)
    {
        var argErrors = ValidateArgs(args, @event.ArgDescriptors);
        if (argErrors.Length > 0)
            return new EventInspection(eventName, Prospect.Impossible, declaredArgs, argErrors, [], [], []);
    }

    // ── Dispatch lookup ──────────────────────────────────────────
    if (!precept.DispatchIndex.Rows.TryGetValue((version.CurrentState, @event), out var rows))
        return new EventInspection(eventName, Prospect.Impossible, declaredArgs, [], [], [], []);
    
    // ── CurrentFields: captured once before row loop (fixes latent bug) ──
    // Previously FieldSnapshots was overwritten per-row; the final EventInspection
    // received only the last row's projected post-state. CurrentFields is the
    // pre-mutation snapshot, captured here, stable across all rows.
    var currentFields = BuildFieldSnapshots(version.Slots, precept.Fields, version.CurrentState);

    var transitionInspections = new List<TransitionInspection>();
    var overallProspect = Prospect.Impossible;
    ExecutionRow? winningRow = null;
    
    foreach (var row in rows)
    {
        // ── Guard evaluation ─────────────────────────────────────
        // Inspect path uses EvaluateGuardProspect (Kleene ternary), not EvaluateGuard (bool).
        // Missing args → Unknown → propagates via Kleene truth table to Possible.
        // Full args → standard binary evaluation (Certain or Impossible).
        Prospect guardProspect;
        string? guardSummary = null;

        if (row.Guard == null)
        {
            guardProspect = Prospect.Certain;
        }
        else
        {
            guardProspect = EvaluateGuardProspect(row.Guard, version.Slots, args);
            if (guardProspect == Prospect.Impossible)
                guardSummary = SummarizeGuard(row.Guard);   // Human-readable guard description
            else if (guardProspect == Prospect.Possible)
                guardSummary = SummarizeGuard(row.Guard);   // Ambiguous — show the guard expression
        }
        
        if (guardProspect == Prospect.Impossible)
        {
            // Guard failed — row is impossible
            var effect = BuildRowEffect(row);
            transitionInspections.Add(new TransitionInspection(
                Prospect.Impossible, effect, guardSummary, [], []));
            continue;
        }
        
        // Guard passed or is ambiguous — simulate execution
        var workingCopy = version.Slots.ToArray();
        foreach (var action in row.Actions)
            ExecuteAction(action, workingCopy, args ?? FiredArgs.Empty);
        
        foreach (var slot in precept.SlotLayout.ComputedSlots)
            workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, args ?? FiredArgs.Empty);
        
        var violations = EvaluateFireConstraints(...);
        var constraintResults = BuildConstraintResults(violations, ...);
        var postFields = BuildFieldSnapshots(workingCopy, precept.Fields);
        var effect = BuildRowEffect(row);
        
        var prospect = (guardProspect, violations.Count) switch
        {
            (Prospect.Certain, 0) => Prospect.Certain,
            (Prospect.Possible, 0) => Prospect.Possible,
            _ => Prospect.Impossible
        };
        
        // Track if this row would win
        if (prospect == Prospect.Certain && winningRow == null)
        {
            winningRow = row;
            if (overallProspect == Prospect.Impossible)
                overallProspect = Prospect.Certain;
        }
        else if (prospect == Prospect.Certain && winningRow != null)
        {
            overallProspect = Prospect.Possible;   // Multiple candidates — ambiguous
        }
        else if (prospect == Prospect.Possible && overallProspect == Prospect.Impossible)
        {
            overallProspect = Prospect.Possible;
        }
        
        transitionInspections.Add(new TransitionInspection(
            prospect, effect, guardSummary, constraintResults, postFields));
    }
    
    return new EventInspection(eventName, overallProspect, declaredArgs, [], currentFields, transitionInspections, []);
}

// ── RowEffect construction ───────────────────────────────────────
// Maps ExecutionRow.Outcome to the RowEffect DU:
RowEffect BuildRowEffect(ExecutionRow row) => row.Outcome switch
{
    TransitionOutcome.Transition => new RowEffect.TransitionTo(row.TargetState!.Name),
    TransitionOutcome.NoTransition => new RowEffect.NoTransition(),
    TransitionOutcome.Reject => new RowEffect.Rejection(row.RejectReason ?? ""),
};
```

**`EvaluateGuardProspect` — Kleene ternary guard evaluation for the inspect path:**

The commit path calls `EvaluateGuard(plan, slots, args)` → `bool`. The inspect path calls `EvaluateGuardProspect(plan, slots, args?)` → `Prospect`, which implements Kleene three-value logic:

- Any `LOAD_ARG` opcode for a missing arg produces `Unknown` at the Kleene level
- The stack machine propagates `Unknown` through boolean operators per the truth table documented in `result-types.md` § Kleene Propagation Rules
- The final result maps: `true` → `Certain`, `false` → `Impossible`, `Unknown` → `Possible`
- When `args` is fully provided, `EvaluateGuardProspect` produces the same binary result as `EvaluateGuard` (no `Unknown` values enter the stack)

**Bootstrap path before D8/R4 ships:** Without per-node arg-dependency sets, the evaluator implements a conservative approximation: any expression referencing any arg (detected by attempting evaluation and catching the absent-arg condition) → mark that guard expression as `Unknown → Possible`. This is conservative (may over-report `Possible`) but never wrong. When D8/R4 ships, the precise Kleene evaluation replaces the approximation automatically.

**`DeclaredArgs` population:** Read directly from `EventDescriptor.ArgDescriptors` — one array reference, zero computation. Includes both required and optional args so consumers can render a complete input form.

**`ArgError` collection path:** At the `Version.InspectFire` API boundary, arg validation runs against `EventDescriptor.ArgDescriptors` before the evaluator is invoked. If validation produces errors, the evaluator is not called; the API returns `EventInspection(EventName, Impossible, DeclaredArgs, argErrors, [], [], [])` directly. This matches the commit path where `Fire` returns `EventOutcome.InvalidArgs(reason)` on arg validation failure.

**Multiple-candidate handling:** When inspection finds more than one passing row, `overallProspect` is set to `Possible`. Inspection and commit must stay aligned — when the runtime would produce an `AmbiguousDispatch` fault, inspection reports `Possible` rather than `Certain`.

**Event-level ensures:** `EventEnsures` in `EventInspection` carries event-scoped constraint results (`on<event>` constraints). Population requires evaluating the event ensures against the post-mutation working copy — currently passes empty array until the constraint plan index is wired for inspection. **OQ-4 (pending):** whether `EventEnsures` should move inside `TransitionInspection` (per-row) or remain event-level. Pending Shane's call.

It evaluates every declared row, not just the winning candidate. Each `TransitionInspection` describes:
- `Prospect`: Did the guard pass and all constraints satisfy? (`Certain` / `Possible` / `Impossible`)
- `Effect`: The row's outcome kind — `TransitionTo(TargetState)`, `NoTransition()`, or `Rejection(Reason)`
- `GuardSummary`: Human-readable guard description when the guard failed or was ambiguous; `null` when passed or absent
- `Constraints`: Which constraints were evaluated and their results
- `PostFields`: Projected field values if this row were to fire

#### InspectUpdate(patch)

Returns `UpdateInspection` — access mode results, constraint evaluation, and event-prospect analysis:

```csharp
UpdateInspection InspectUpdate(Precept precept, Version version, PreceptValue[]? patch)
{
    var workingCopy = version.Slots.ToArray();
    
    // Apply patch if provided
    if (patch != null)
    {
        for (int i = 0; i < patch.Length; i++)
            if (!patch[i].IsAbsent)
                workingCopy[i] = patch[i];  // patch is slot-indexed; IsAbsent means this slot is not being updated
    }
    
    // Recompute computed fields
    foreach (var slot in precept.SlotLayout.ComputedSlots)
        workingCopy[slot] = EvaluatePlan(precept.Fields[slot].ComputedPlan, workingCopy, FiredArgs.Empty);
    
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
    
    return new UpdateInspection(fieldSnapshots, constraintResults, eventInspections);
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
    {
        Dispatch(opcode, slots, args, stack, ref top);

        // CC#6: Defense-in-depth fault backstop — zero overhead when FaultSite is null (proven safe)
        if (opcode.FaultSite is { } annotation && IsFaultCondition(stack, top, opcode))
            return Fail(annotation.Code, annotation.PreventedBy, annotation.Site);
    }
    return stack[0];
}
```

**Two-layer defense model (CC#6):** The first line of defense is the compile gate — the type checker and proof engine emit diagnostics for unresolved obligations, preventing the `Precept` object from being built unless the author fixes them. The second line is the runtime backstop — `FaultSiteAnnotation` on opcodes compiled from unresolved sites (force-builds, catalog evolution, or compiler bugs). For a cleanly-compiled precept, every `opcode.FaultSite` is `null`, and the null check is the only runtime cost — effectively zero overhead. See `proof-engine.md §2` for the structural elision model and `precept-builder.md §Pass 4` for the planting contract.

**Operator dispatch (opcode-embedded, zero-knowledge evaluator):**

Binary and unary operator execution uses `static readonly` executor delegates embedded directly in `BinaryOp`/`UnaryOp` opcodes at build time. The evaluator does not switch on `OperationKind` members, index any catalog array, or apply per-operator logic. It calls the embedded delegate:

```csharp
case BinaryOp(var kind, var executor):
    PreceptValue r = stack[--top];
    PreceptValue l = stack[--top];
    // Executor is a static readonly Func<> embedded in the opcode at build time.
    // The evaluator calls it directly — no catalog lookup, no switch on kind.
    // Kind is preserved on the opcode for diagnostics and trace mode.
    stack[top++] = executor(l, r);
    break;
```

Each `BinaryOp` opcode carries an embedded `Executor` delegate (`static readonly Func<PreceptValue, PreceptValue, PreceptValue>`) fetched from the corresponding `TypeRuntimeMeta.BinaryExecutors` entry at build time. The evaluator calls `opcode.Executor(l, r)` — no catalog lookup, no switch. The `Kind` field is preserved on the opcode for diagnostics, trace mode, and inspectability. Same pattern for unary ops via `UnaryOp.Executor`.

> **Delegate lifetime:** Executor delegates are `static readonly` fields on `TypeRuntime<T>` — allocated once at type initialization, immortal, GC-invisible. The evaluator holds a reference that was embedded by the builder; no heap traffic occurs on the hot path.

**LOAD_ARG and event-arg slots:**

Event arguments are converted to `PreceptValue` at the Fire boundary (before the opcode loop begins), not lazily inside the loop. `IArgBuilder` materializes a `PreceptValue[]` arg slot array for the call; `LOAD_ARG(name)` resolves the arg name to a slot index at build time, so the opcode carries a slot index rather than a string:

```csharp
case LoadArg(var argSlotIndex):
    stack[top++] = args[argSlotIndex];  // args is PreceptValue[], pre-filled at Fire boundary
    break;
```

This keeps LOAD_ARG O(1) — no dictionary lookup, no string comparison.

Pending implementation: four opcode semantics need to be locked before builder-emitted plans and any future compiled path can rely on identical execution semantics: how `LOAD_ARG` treats absent/null inputs, whether `BRANCH_FALSE` treats numeric zero as falsy or only `false`, whether `RETURN` may legally fall through additional opcodes, and whether the evaluation stack should be pooled rather than `stackalloc`-only.

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
void ExecuteAction(ActionPlan action, PreceptValue[] slots, FiredArgs args)
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

### 7.4.1 Collection Internals

This section is the **authoritative implementation reference** for collection backing, the CLR adapter types, the `CollectionActions` helper class, the copy-on-write protocol, and scalability guidance. The decisions here supersede any earlier design-doc references to Okasaki pair-of-stacks, `ImmutableDictionary`, or custom sorted structures for collection backing.

#### A. Collection Backing: `PreceptValue[]` for All 9 Kinds

**All 9 collection kinds** use `PreceptValue[]` as their internal runtime representation. A collection field occupies one slot in the evaluator's `PreceptValue[]` working copy. That slot holds a `PreceptValue` with a collection-tag variant whose reference region points to the element backing array.

**Layout conventions:**

| Collection Category | Kinds | Stride | Layout |
|---|---|---|---|
| **Single-value** | `list`, `set`, `queue`, `stack`, `log` | 1 | `[v₀, v₁, v₂, ...]` — one `PreceptValue` per element |
| **Pair** | `lookup`, `bag`, `log by P`, `queue by P` | 2 | `[k₀, v₀, k₁, v₁, ...]` — even indices = key/element, odd indices = value/frequency |

The stride is a kind-specific layout convention within the same CLR type — not a type boundary. The evaluator never dispatches on "is this a stride-1 or stride-2 array?" — it is always `PreceptValue[]`.

**`ref` helper accessors** provide named-field readability for stride-2 pairs without changing the backing type:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static ref PreceptValue Key(PreceptValue[] arr, int i) => ref arr[i * 2];

[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static ref PreceptValue Val(PreceptValue[] arr, int i) => ref arr[i * 2 + 1];
```

**Why `PreceptValue[]` for all 9 kinds:**

1. **Consistency with the slot model.** The evaluator operates on `PreceptValue[]` slot arrays. Collections are slots. Same type eliminates a type boundary.
2. **One pool.** `ArrayPool<PreceptValue>.Shared` serves both the slot working copy and all collection backing arrays. No secondary pools.
3. **Structural sharing is worthless.** Precept's versioning model is replace-the-whole-thing. No multi-version tree where shared tails save memory.
4. **Collection semantics belong in the evaluator.** The evaluator owns all mutation logic via prebuilt action plans. The backing array is dumb storage.
5. **Performance is fine at Precept's scale.** Business entity collections are small. Copying them is free relative to the evaluator's per-Fire cost.

**Stride-2 flat array beats alternatives decisively:**

| Alternative | Non-Negotiable Killer |
|---|---|
| `PreceptValue[,]` (2D rectangular) | **No `ArrayPool` support.** Cannot pool multidimensional arrays. Allocation on every mutation. |
| `PreceptValue[][]` (jagged) | N+1 heap allocations per collection. Catastrophic GC pressure. No spatial locality. |
| `(PreceptValue, PreceptValue)[]` (struct tuple) | **Second CLR type = second pool.** Breaks type uniformity. Dispatch boundary at slot layer. |

**Obsolete backing types** — the following were specified in earlier design docs and are entirely superseded. Do not implement them:

- `ImmutableLog<T>` (Okasaki pair-of-stacks)
- `ImmutableDictionary<T, int>` (bag backing)
- `ImmutableList<T>` (list backing)
- `SortedDictionary<TPriority, Queue<TElement>>` (queue-by-P backing)
- `ImmutableDictionary<K, V>` (lookup backing)
- Custom immutable sorted linked list (log-by-P backing)

All replaced by: **`PreceptValue[]` with evaluator-enforced invariants.**

#### B. CLR Adapter Types

Two internal adapter types bridge the evaluator's `PreceptValue[]` world and the public `Get<T>()` typed API. Both are lazy at the **Version level** — the adapter is constructed on first field read from a `Version`, not lazily element-by-element.

**`PreceptList<T> : IReadOnlyList<T>`**

Wraps a stride-1 `PreceptValue[]` backing array and projects each element through the CLR↔PreceptValue mapping on access. Materialization behavior:

- Adapter constructed on first `version.Get<IReadOnlyList<T>>(fieldName)` call
- Materializes to internal `T[]` on first element access (eager-on-first-read)
- After materialization, indexing is O(1)
- O(n) materialization cost per Version per field read — paid once per access series, not once per element

**`PreceptLookup<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>`**

Wraps a stride-2 `PreceptValue[]` backing array (keys at even indices, values at odd indices) and projects through the CLR↔PreceptValue mapping on access. Materialization behavior:

- Adapter constructed on first `version.Get<IReadOnlyDictionary<TKey, TValue>>(fieldName)` call
- Materializes to internal dictionary on first key access, providing O(1) key lookup semantics
- Same lazy-at-Version-level pattern as `PreceptList<T>`

> **Note:** "Lazy" means lazy at the Version level (adapter constructed on first field read), NOT lazy at the element level. Once a field read is initiated, materialization is eager — all elements are processed in one pass. This is a deliberate tradeoff that keeps the adapter simple and allocation-bounded.

Both adapter types are the bridge between internal representation and the stable public API surface (`IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`). `PreceptValue` must never appear in public API signatures.

#### C. `CollectionActions` Static Class

Collection mutation logic lives as **static methods in a `CollectionActions` class**, called directly by the evaluator's action dispatch. No class instances, no lifecycle ownership, no type boundary between the evaluator and the backing array.

```csharp
/// <summary>
/// In-place mutation helpers for collection operations. Each method receives a
/// mutable Span (evaluator-owned working copy) and returns the new logical count.
/// The evaluator owns the CoW boundary and pool lifecycle.
/// </summary>
internal static class CollectionActions
{
    // === Single-value kinds (stride 1) ===
    public static int AddToSet(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int Enqueue(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int Push(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int AppendToLog(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int InsertAt(Span<PreceptValue> backing, int count, int index, PreceptValue element) { ... }

    // === Pair kinds (stride 2) ===
    public static int PutLookup(Span<PreceptValue> backing, int count, PreceptValue key, PreceptValue value) { ... }
    public static int AddToBag(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int EnqueueByPriority(Span<PreceptValue> backing, int count, PreceptValue element, PreceptValue priority, SortDirection direction) { ... }

    // === Stride-2 ergonomic helpers ===
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PreceptValue Key(PreceptValue[] arr, int i) => ref arr[i * 2];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PreceptValue Val(PreceptValue[] arr, int i) => ref arr[i * 2 + 1];
}
```

The signature convention is **"Span in, count out"**: `CollectionActions` receives a `Span<PreceptValue>` (a mutable view of the evaluator's working array) and returns the new element count. The evaluator owns the CoW boundary — not `CollectionActions`.

**Why NOT wrapper types:**

| Concern | Why wrappers lose |
|---------|-------------------|
| **Pool lifecycle ambiguity** | Some backing arrays are pool-rented working copies; others are committed version arrays. The evaluator knows which is which. A wrapper type cannot safely manage pool returns without knowing array provenance. |
| **Catalog duplication** | Wrapper types would be behavioral clones of catalog-specified semantics hardcoded into C# classes — exactly the parallel implementation the catalog architecture prohibits. |
| **ICollectionBacking migration friction** | Wrappers become technical debt the moment `ICollectionBacking` arrives — they are either the interface implementation (then not wrappers) or gratuitous indirection. |
| **Evaluator complexity** | Wrap-construct-call-extract overhead on every mutation. Static helpers are one line: `CollectionActions.Add(span, count, element)`. |

**Design principles preserved:**

- **Evaluator owns ALL pool lifecycle decisions** — no other actor rents or returns.
- **`CollectionActions` has no state, no lifecycle, no ownership** — pure computation on caller-provided buffers.
- **Independently testable** as pure functions with zero ceremony.
- **Clean migration path** to `ICollectionBacking` — change parameter types, nothing else.

**Direction model (OQ-C3):**

Direction is "compiled in" at write time. Priority insertion methods (`EnqueueByPriority` and any log-by-priority variant) take a `SortDirection direction` parameter and insert elements in the declared sort order during the write operation itself:

- **`arr[0]` is always the logical "front" in declared order.** `Peek`, `Dequeue`, and log iteration are direction-naive — they always read from `arr[0]` forward. No adapter flip is needed at read time.
- **`FieldDescriptor.SortDirection` is informational metadata only** — it is not consulted at read time. The field records the user-declared direction for UI and tooling purposes; the ordering was baked in during insertion.
- **Rationale:** Concentrating direction ownership at the insertion site means one place is responsible for order. The alternative — always store ascending, flip index math in the CLR adapter — was rejected because the JSON serializer also needed the flip, splitting direction ownership across two places.

#### D. Copy-on-Write Protocol for Multi-Mutation Events

**The problem:** Multiple mutations to the same collection slot within one event handler:

```precept
on OrderPlaced:
    add item1 to items
    add item2 to items
    add item3 to items
```

With naive per-mutation CoW: 3 allocations, 2 immediately discarded. At scale (200-entry log appended to 5 times = 5 allocations × 200 elements) this compounds to O(N×K).

**The solution:** The evaluator's working-copy model already creates the conditions for efficient multi-mutation. The full protocol:

| Step | Actor | Action |
|------|-------|--------|
| **First mutation** to collection slot | Evaluator | Detects alias via `ReferenceEquals(currentBacking, originalSlots[slot].CollectionBacking)`. Rents working array from `ArrayPool<PreceptValue>.Shared`. Copies existing elements. |
| **All mutations** (including first) | `CollectionActions` | Mutates in-place on the `Span`. Returns new count. **Never allocates.** |
| **Subsequent mutations** to same slot | Evaluator | Detects backing is already private (not aliased). Passes directly — no clone. |
| **Commit** (success) | Evaluator | Working array in slot IS the new version's backing. Zero-copy promotion (donated to `Version`). |
| **Discard** (constraint failure) | Evaluator | Returns all working collection arrays to `ArrayPool<PreceptValue>.Shared`. |
| **Resize** (capacity exceeded) | Evaluator | Rents larger array, copies, returns old. |

**Cost model:**

| Scenario | Naive (per-mutation CoW) | Option C-2 (working-copy model) |
|----------|--------------------------|----------------------------------|
| 3 adds to empty set | 3 allocs, 0+1+2 copies | 1 alloc (capacity 3+), 3 in-place writes |
| 5 appends to 200-entry log | 5 allocs, 1,010 total copies | 1 alloc, 200 copies, 5 in-place writes |
| **1 add to set (common case)** | 1 alloc, K copies | **1 alloc, K copies (identical — zero overhead)** |

**Performance:** O(K) + O(N) for K mutations on N-element collection — not O(N×K).

**`ArrayPool` lifecycle — unambiguous:**

| Array Type | Who Rents | Who Returns | When |
|---|---|---|---|
| Committed version backing | Nobody (donated from previous `Fire`) | Nobody (GC'd with `Version`) | N/A |
| Working collection array | Evaluator (on first mutation) | Evaluator (constraint failure) OR donated (commit success) | End of row |
| Resized array | Evaluator (capacity exceeded) | Old: returned immediately. New: same lifecycle as working. | Resize point |

**Rollback on constraint failure:**

```csharp
for (int i = 0; i < workingCopy.Length; i++)
{
    if (workingCopy[i].IsCollection &&
        !ReferenceEquals(workingCopy[i].GetCollectionBacking(), originalSlots[i].GetCollectionBacking()))
    {
        ArrayPool<PreceptValue>.Shared.Return(workingCopy[i].GetCollectionBacking());
    }
}
```

**Tradeoffs accepted:**

- **Evaluator action dispatch is slightly more complex** — the `ReferenceEquals` check plus first-mutation clone adds ~5 lines per collection action dispatch path. Accepted because the alternative (O(N×K) allocations) is materially worse.
- **`CollectionActions` methods mutate their `Span` argument** — not "pure" in the strictest sense. Accepted because behavior depends only on inputs (no external state), and mutation IS the correct semantic for an in-place helper.

#### E. Scalability Guidance

**Size safety zones:**

| Threshold | Response |
|-----------|----------|
| **<500 elements** | Don't think about it. Well within acceptable bounds. |
| **500–2,000 elements** | `maxcount` should be documented as best practice. Lint warning for `log` fields without `maxcount`. |
| **>2,000 elements** | Yellow zone. Per-event copy cost is 64–160 KB. Starting to dominate the evaluator's memory budget. |
| **>10,000 elements** | Design smell. The entity needs archival, snapshotting, or external log storage. |

**The dangerous kind: `log`**

`log of T` and `log of T by P` are the **only structurally unbounded collection kinds.** Every other kind has natural drainage (`queue`/`stack` dequeue/pop), replacement semantics (`set` add/remove, `lookup` put), or explicit user intent to bound (`list`, `bag`).

> **Note:** `maxcount` should be treated as mandatory guidance for `log` fields. Without it, logs grow without bound. At 2,000 entries, the archival pattern (snapshot + external store) becomes mandatory.

**Lazy-load extensibility seam (deferred — do not implement prematurely):**

The `PreceptValue[]` representation does not lock out a future deferred-load path. The evaluator's slot indirection already provides the extensibility seam: action logic is factored per-kind, the `PreceptValue` reference region is a pointer, and the evaluator never indexes into collection backing directly. The future interface stub, for reference:

```csharp
// DEFERRED — do NOT implement. Ship PreceptValue[] first.
// The seam exists architecturally. Don't pay for it until needed.
internal interface ICollectionBacking
{
    PreceptValue[] Materialize();        // force full array (for commit)
    int Count { get; }                   // cheap for both lazy and eager
    PreceptValue ElementAt(int index);   // lazy window access
}
```

Ship `PreceptValue[]` with no abstraction layer. Introduce `ICollectionBacking` only when large-entity patterns prove the need.

---

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

**`FieldDescriptor.AccessModes` structural shape:** The evaluator assumes a per-state access-mode lookup on `FieldDescriptor`. The storage shape (dictionary vs. indexed representation) affects lookup cost, descriptor size, and emission strategy. This is a pending implementation decision for the builder/descriptor pass.

### 7.6 Constraint Evaluation

```csharp
IReadOnlyList<ConstraintViolation> EvaluateFireConstraints(
    ConstraintPlanIndex index,
    StateDescriptor? currentState,
    EventDescriptor @event,
    StateDescriptor? targetState,
    PreceptValue[] slots,
    FiredArgs args)
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
    PreceptValue[] slots,
    FiredArgs args,
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
    string? Because,                         // From constraint declaration
    ImmutableArray<FieldSnapshot> RelevantFields,  // Field values at evaluation time
    string? FailingSubexpression,            // Innermost expression that evaluated false
    JsonElement? FailingValue               // Value at the failure site
);
```

When an opcode hits a fault backstop site (identified by a non-null `FaultSiteAnnotation` on the opcode, stamped by the Precept Builder per CC#6), the evaluator produces a structured `Fault`:

```csharp
internal static Fault Fail(FaultCode code, params object?[] args)
    => Faults.Create(code, args);
```

The `Faults` catalog produces a fully-formed `Fault` with:
- `Code`: The `FaultCode` enum value
- `CodeName`: Stable string identity (e.g., `"DivisionByZero"`)
- `Message`: Pre-formatted English diagnostic message

Faults are **never thrown** — they are returned as structured outcome variants. This maintains the pattern-matchable, composable outcome model.

**`Faulted` outcome (CC#12):** Impossible-path failures are modeled as structured `Fault` values raised via `Faults.Create` and surfaced as `EventOutcome.Faulted(Fault fault)` — the 8th `EventOutcome` variant. The evaluator's `Fail()` path now returns a `Faulted` outcome rather than an unhandled exception. MCP `precept_fire` serializes this as `{ "outcome": "Faulted", "fault": { "code": ..., "codeName": ..., "message": ... } }`.

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
| `FaultSiteAnnotation` | Nullable annotation on opcode: fault code + preventing diagnostic code + source location (CC#6). Null = proven safe. |
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

The public `Version` API is dual-lane: callers supply either a `JsonElement?` (JSON lane) or an `Action<IArgBuilder>?` (CLR lane). Either is materialized into `FiredArgs` before reaching the internal evaluator. The evaluator always sees `FiredArgs` — never a dictionary or raw JSON.

```csharp
// Version façade — dual-lane ingress materializes to FiredArgs before calling Evaluator
public sealed record Version
{
    // ── JSON lane ──────────────────────────────────────────────────────────────
    public EventOutcome Fire(string eventName, JsonElement? args = null)
    {
        var @event = Precept.Events.Single(e => e.Name == eventName);
        var firedArgs = args.HasValue
            ? ArgMaterializer.FromJson(@event, args.Value)  // JsonElement → FiredArgs
            : FiredArgs.Empty;
        return Evaluator.Fire(Precept, this, @event, firedArgs);
    }
    
    public UpdateOutcome Update(JsonElement? patch = null)
    {
        var resolvedPatch = patch.HasValue
            ? PatchMaterializer.FromJson(Precept, patch.Value)  // JsonElement → PreceptValue[]
            : null;
        return Evaluator.Update(Precept, this, resolvedPatch);
    }
    
    // ── CLR lane ───────────────────────────────────────────────────────────────
    public EventOutcome Fire(string eventName, Action<IArgBuilder>? build = null)
    {
        var @event = Precept.Events.Single(e => e.Name == eventName);
        var firedArgs = build != null
            ? ArgMaterializer.FromBuilder(@event, build)  // IArgBuilder → FiredArgs
            : FiredArgs.Empty;
        return Evaluator.Fire(Precept, this, @event, firedArgs);
    }
    
    public UpdateOutcome Update(Action<IFieldBuilder>? build = null)
    {
        var resolvedPatch = build != null
            ? PatchMaterializer.FromBuilder(Precept, build)  // IFieldBuilder → PreceptValue[]
            : null;
        return Evaluator.Update(Precept, this, resolvedPatch);
    }
    
    // ── Inspect (same dual-lane pattern) ──────────────────────────────────────
    public EventInspection InspectFire(string eventName, JsonElement? args = null)
    {
        var @event = Precept.Events.Single(e => e.Name == eventName);
        var firedArgs = args.HasValue ? ArgMaterializer.FromJson(@event, args.Value) : (FiredArgs?)null;
        return Evaluator.InspectFire(Precept, this, @event, firedArgs);
    }
    
    public EventInspection InspectFire(string eventName, Action<IArgBuilder>? build = null)
    {
        var @event = Precept.Events.Single(e => e.Name == eventName);
        var firedArgs = build != null ? ArgMaterializer.FromBuilder(@event, build) : (FiredArgs?)null;
        return Evaluator.InspectFire(Precept, this, @event, firedArgs);
    }
    
    public UpdateInspection InspectUpdate(JsonElement? patch = null)
    {
        var resolvedPatch = patch.HasValue ? PatchMaterializer.FromJson(Precept, patch.Value) : null;
        return Evaluator.InspectUpdate(Precept, this, resolvedPatch);
    }
    
    public UpdateInspection InspectUpdate(Action<IFieldBuilder>? build = null)
    {
        var resolvedPatch = build != null ? PatchMaterializer.FromBuilder(Precept, build) : null;
        return Evaluator.InspectUpdate(Precept, this, resolvedPatch);
    }
}
```

**The boundary is explicit and one-way.** Public callers choose a lane (JSON or CLR typed). Materialization happens at the boundary. The internal evaluator only ever sees `FiredArgs` and `PreceptValue[]` — no dictionaries, no raw JSON, no `object?`.

---

### Business-Domain Internal Types

These are the evaluator-internal types that back the public business-domain API boundary. They live under `src/Precept/Runtime/Measures/` and are never exposed to callers — `UnitOfMeasure` and `MeasureDimension` are the lightweight proxy structs that cross the boundary (see Decision 13 in §11). `Unit`, `MeasureDimension`, and `UnitFactory` are evaluator-internal wrappers around the shared UCUM parsed-unit model used for runtime arithmetic.

#### `Unit` — evaluator-internal unit entity

```csharp
/// A unit of measure identified by its UCUM code. Evaluator-internal.
/// Consumers receive UnitOfMeasure (proxy struct) at the public API boundary.
internal sealed class Unit : IEquatable<Unit>
{
    public string   Code      { get; }  // UCUM code: "kg", "m/s2", "[lb_av]"
    public string   Name      { get; }  // Human name: "kilogram", "meter per second squared"
    public Dimension Dimension { get; } // SI base-dimension exponent vector
    public UnitTier  Tier      { get; } // Discovery tier (Common / Extended / Full / Derived)

    // Equality by UCUM code (canonical form)
    public bool Equals(Unit? other) => Code == other?.Code;
    public override int GetHashCode() => Code.GetHashCode();
}
```

#### `Dimension` — evaluator-internal SI dimension vector

```csharp
/// A product of SI base dimension exponents. Evaluator-internal.
/// Consumers receive MeasureDimension (proxy struct) at the public API boundary.
internal readonly record struct Dimension(
    int Length,       // m
    int Mass,         // kg
    int Time,         // s
    int Current,      // A
    int Temperature,  // K
    int Amount,       // mol
    int Luminosity)   // cd
{
    public static readonly Dimension Dimensionless = default;
    // Well-known constants (Mass, Length, Time, etc.) for type-checker dimensional analysis
    public bool IsCompatibleWith(Dimension other) => this == other;
}
```

#### `UnitTier` — UCUM discovery tier

```csharp
/// Controls which units are surfaced proactively by tooling.
internal enum UnitTier
{
    Common,    // Tier 1: ~150 atoms — surfaced in autocomplete and builder APIs
    Extended,  // Tier 2: ~500 atoms — valid, not proactively surfaced
    Full,      // Tier 3: ~2,600 atoms — accepted for interop, not surfaced
    Derived    // Synthesized compound expressions (not atoms from the UCUM table)
}
```

The evaluator resolves `UnitOfMeasure.Code` → `Unit` via `UnitCatalog.Get(code)` internally when dimensional analysis or conversion metadata is needed.

---

## 9. Failure Modes and Recovery

### In-Domain Failures (Expected Business Events)

All in-domain failures produce structured outcome variants — never exceptions:

| Failure | Outcome | Description |
|---|---|---|
| All guards failed | `EventOutcome.Unmatched(evaluatedRows)` | No transition row matched; per-candidate guard trace attached (CC#24) |
| Constraint violations | `EventOutcome.ConstraintsFailed(violations)` | Rows matched, but constraint(s) failed post-mutation |
| Field not editable | `UpdateOutcome.FieldNotEditable(field, mode)` | Update attempted on readonly/omit field |
| Undefined event | `EventOutcome.UndefinedEvent()` | No rows declared for event in current state |
| Authored rejection | `EventOutcome.Rejected(reason)` | A `reject` row was the winning candidate |
| Invalid args | `EventOutcome.InvalidArgs(reason)` | Arg validation failed (wrong type, missing required) |
| Invalid fields | `UpdateOutcome.InvalidFields(reason)` | Patch validation failed (type mismatch, unknown field) |
| Impossible-path fault | `EventOutcome.Faulted(fault)` | Defense-in-depth backstop; only fires when upstream pipeline has a bug (CC#12) |

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
| Ambiguous dispatch | `AmbiguousDispatch` | Proof engine exclusivity analysis (`DiagnosticCode.AmbiguousDispatch`, CC#13) |

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

**Decision:** All in-domain failures return typed outcome variants (`Rejected`, `FieldNotEditable`, `Unmatched`, etc.). The evaluator never throws exceptions for expected business events.

**Rationale:**
- **Composable results:** Callers pattern-match on outcomes; no try/catch required
- **Exhaustive handling:** Sealed outcome hierarchies + switch expressions ensure all cases are handled
- **No exception overhead:** Pattern matching is cheaper than exception handling
- **Clear semantics:** "Constraint violation" is a business event, not an exceptional condition

**Alternative rejected:** *Exception-based error handling* — rejected because it conflates "the business rule said no" with "something is broken," leading to poor error handling patterns.

### Decision 5: FromJson Bypasses Constraint Validation

**Decision:** `FromJson` bypasses both access-mode checks and constraint validation. It is a pure hydration operation — the inverse of `ToJson`.

**Rationale:**
- **Restoration is not a business transaction:** Data was committed when it satisfied the rules in effect at that time. The definition may have changed since. Constraint validation on restore would block all existing data on every schema change, making evolution impossible.
- **Access modes are interactive controls:** They govern what the user can edit, not what values are structurally valid.
- **Round-trip fidelity:** `precept.FromJson(version.ToJson())` must produce a semantically identical `Version`. Adding constraint evaluation would break this contract when the definition evolves.

**If validation is needed:** Callers who need to validate restored state should call `version.InspectFire` or `version.InspectUpdate` after restoration.

**Alternative rejected:** *Validate constraints on FromJson* — breaks round-trip fidelity; blocks valid data on every schema change; conflates "is this historically correct data?" with "does this data satisfy today's rules?"

### Decision 6: FromJson is the Evaluator's Inverse, Not a Pipeline

**Decision:** `FromJson` is handled at the `Precept` API boundary, not by the evaluator. No constraint evaluation pipeline runs.

**Rationale:**
- No evaluation is needed — `FromJson` simply parses the envelope and populates slots directly from persisted values.
- The evaluator is the pure plan executor — it is not invoked for operations that have no plans to execute.

### Decision 7: Collect All Violations, Don't Short-Circuit

**Decision:** Constraint evaluation collects all violations, not just the first one.

**Rationale:**
- **Diagnostic completeness:** Users see all problems at once, not one-at-a-time whack-a-mole
- **Better tooling support:** AI agents and UIs can present a complete picture
- **No performance cost:** Constraints are typically few; collecting all is negligible overhead

**Alternative rejected:** *Short-circuit on first violation* — rejected because it provides poor user experience and no meaningful performance benefit.

### Decision 8: Single Interpreter with Diagnostic Trace — No Dual-Path

**Decision:** There is one interpreter. The A+G stack-based opcode executor is the single runtime for ALL consumers — production Fire/Inspect/Update AND LS/MCP interactive authoring feedback.

**Rationale:**
- **Correctness guarantee:** Dual interpreters (one for production, one for tooling) must agree on semantics. When they diverge, tooling lies to the author. That is worse than no tooling.
- **Simplicity and maintainability:** A single interpreter with trace output is simpler and cheaper to maintain — there is no synchronization burden between two execution paths.
- **Trivially traceable:** The opcode loop is a flat array with O(1) per step and no recursion. Adding per-opcode trace emission is a small increment, not an architectural change.

**How trace mode works:** When a trace context is attached to an evaluation call, each opcode step emits a diagnostic record: opcode identity, operand values before and after, slot index for LOAD_SLOT/STORE_SLOT, stack depth. The trace record uses the same `PreceptValue` currency — no boxing, no secondary representation.

**Alternative explicitly rejected:** *Separate tree-walk interpreter for LS/MCP* — rejected because two interpreters that must agree is a correctness liability. Tooling that uses a different execution engine than production will eventually diverge and mislead authors.

**Open design question:** The exact trace record shape (struct? class? output channel?), how trace contexts are attached (parameter? ambient?), and how the LS/MCP consumes trace output are NOT yet decided. These are implementation seams, not architectural questions.

### Decision 9: Universal `PreceptValue[]` Backing for All 9 Collection Kinds

**Decision:** All 9 collection kinds use `PreceptValue[]` as their internal runtime representation. Stride-1 for single-value kinds; stride-2 (interleaved key/value) for pair kinds. One CLR type, one pool.

**Rationale:**
- **Type uniformity with the slot model:** Collections are slots. `PreceptValue[]` is the evaluation currency. No type boundary means no dispatch overhead at the slot layer.
- **One pool:** `ArrayPool<PreceptValue>.Shared` serves both slot working copies and collection backing arrays. A second CLR type forces a second pool, splitting lifecycle management across two allocators.
- **Structural sharing is worthless here:** Precept's versioning model is copy-on-write replace — not a persistent data structure with shared tails. Okasaki pair-of-stacks and similar functional structures carry overhead with no benefit.
- **Ergonomics solved by `ref` helpers:** Named-field readability (`Key(arr, i)`, `Val(arr, i)`) is achieved with zero-cost inlined accessor methods, not a second CLR type.

**Alternatives rejected:**
- `PreceptValue[,]` (2D rectangular): No `ArrayPool` support — allocation on every mutation.
- `PreceptValue[][]` (jagged): N+1 heap objects, no spatial locality, no pooling.
- `(PreceptValue, PreceptValue)[]` (struct tuple): Second CLR type = second pool; breaks type uniformity.
- All custom immutable collection types from earlier design docs (Okasaki log, `ImmutableList`, `SortedDictionary` queue-by-P, etc.): Predated the slot model; incompatible with `ArrayPool` CoW lifecycle.

### Decision 10: `CollectionActions` as a Static Stateless Helper Class

**Decision:** Collection mutation logic lives in `static` methods in a companion `CollectionActions` class. The signature convention is "Span in, count out." The evaluator owns the CoW boundary and pool lifecycle. `CollectionActions` has no state, no lifecycle, no ownership.

**Rationale:**
- **Consistent with the evaluator's execution model:** Guards, constraints, computed fields, and scalar assignments are all dispatched through plan executors with no wrapper types. Collections are not special enough to break the pattern.
- **Pool provenance tracking:** Some backing arrays are pool-rented working copies; others are committed version arrays. Only the evaluator knows which is which. Wrapper types cannot safely manage pool returns without that knowledge.
- **Catalog integrity:** Wrapper types that encode per-kind mutation behavior are behavioral clones of catalog-specified semantics hardcoded into C# classes — exactly the parallel implementation the catalog architecture prohibits.
- **Clean migration path:** When `ICollectionBacking` arrives, the change is parameter type substitution only. No wrapper decomposition required.

**Alternatives rejected:**
- *Wrapper types per collection kind* (`PreceptSet<T>`, `PreceptQueue<T>`, etc.): Pool lifecycle ambiguity; catalog duplication; `ICollectionBacking` migration friction; unnecessary wrap/unwrap overhead per mutation.
- *"Array in, array out" pure functions:* Rejected in favor of "Span in, count out" — the evaluator's working-copy model requires in-place mutation to avoid per-mutation allocation. Span carries the mutable view without transferring ownership.

### Decision 11: Evaluator-Owned Copy-on-Write for Multi-Mutation Events

**Decision:** The evaluator detects first mutation to a collection slot via `ReferenceEquals(currentBacking, originalSlots[slot].CollectionBacking)`, rents a working copy on first mutation only, and donates that working copy to the new `Version` on commit (zero-copy promotion). All subsequent mutations to the same slot within the same event row go in-place on the rented working array.

**Rationale:**
- **O(K) + O(N), not O(N×K):** K mutations on an N-element collection costs one allocation + N copies + K in-place writes. Naive per-mutation CoW costs K allocations + K×(N average-copy) ≈ O(N×K).
- **Zero overhead for the common case:** One mutation to one collection slot = one allocation + N copies — identical to naive CoW. The protocol adds no overhead when there is only one mutation.
- **CoW boundary belongs in the evaluator:** The `ReferenceEquals` check is 5 lines in the action dispatch path. `CollectionActions` stays zero-knowledge about array provenance. This is the correct responsibility boundary.
- **Rollback is O(slots):** On constraint failure, the evaluator walks the working copy once and returns any backing that diverged from the original. No per-kind rollback logic required.

**Tradeoffs accepted:**
- Evaluator action dispatch has ~5 additional lines per collection action dispatch path for the alias check. Accepted — this is a one-time complexity cost for an asymptotically better allocation model.
- `CollectionActions` methods mutate their `Span` argument (not "pure" in the strict sense). Accepted — behavior depends only on inputs; mutation is the correct semantic.

---

### Decision 12: Embedded Delegates, Not Global Array

**Decision:** Executor delegates are embedded directly in `BinaryOp` and `UnaryOp` opcodes at build time. There is no global `Operations.BinaryExecutors[]` array indexed by `OperationKind`.

**Rationale:**
- **Opcodes are reference types** (`sealed record`). The "flat value-type array, cache-friendly" argument for a global array is factually wrong — the evaluator already chases a heap pointer to reach every opcode.
- **Embedded path:** deref opcode → fetch delegate → call (2 steps).
- **Global array path:** deref opcode → extract Kind → index static array → fetch delegate → call (4 steps).
- Embedded has one fewer indirection with no compensating benefit from the global array.
- **Self-contained opcodes:** Everything needed to dispatch lives in the opcode. No global initialization ceremony, no mutable static, no `OperationKind`-to-index mapping.

**Delegate shape:** `static readonly Func<PreceptValue, PreceptValue, PreceptValue>` — not `unsafe delegate*`. All executor methods are static; no closures. `unsafe delegate*` saves ~150ns per event — unmeasurable at business-operation cadence, but propagates `unsafe` through `BinaryOp`, `ExecutionPlan`, and into user-facing APIs. `static readonly Func<>` costs ~48 bytes per delegate on x64; ~100 operations × 48 bytes = ~4.8 KB total, allocated once at type initialization, immortal for process lifetime. Zero per-eval allocation.

**`record struct` opcodes not pursued:** Theoretically improves cache density 4×. Practically irrelevant: Precept evaluates 5–50 opcodes per dispatch and the entire working set fits in L1 cache regardless. Do not pursue until profiling demands it.

**Alternative rejected:** *Global `Operations.BinaryExecutors[]` flat array* — eliminated because opcodes are reference types, making the cache-density argument inapplicable, and the extra indirection adds complexity with no performance gain.

### Decision 13: Dual-Shape API Boundary — Entity vs. Proxy Struct

**Decision:** At the public API boundary, expose the catalog entity directly when ALL its properties are consumer-facing. Use a lightweight proxy struct when the entity carries evaluator-internal metadata.

| Identity Type | API Shape | Why |
|---|---|---|
| `Currency` | `sealed class Currency` (direct entity) | Every property (AlphaCode, Name, Symbol, MinorUnit, NumericCode) is consumer-facing. No evaluator-internal fields. |
| Unit of Measure | `readonly record struct UnitOfMeasure(string Code)` (proxy) | The evaluator-internal `Unit` carries Tier, DimensionVector, conversion factors — machinery that must not appear on the public surface. |
| Dimension | `readonly record struct MeasureDimension(string Name)` (proxy) | The evaluator-internal `Dimension` (7-exponent SI vector) is dimensional analysis machinery. |

**Rationale:** Exposing the catalog entity directly is correct when there is no evaluator-internal state to hide — it avoids the impedance mismatch of wrapping an object inside a struct and gives callers natural access to all metadata. Using a proxy struct is correct when the entity carries evaluation metadata: the proxy exposes only the stable, consumer-meaningful part (the code/name string) while the evaluator resolves the full entity internally.

**Alternative rejected:** *Always expose catalog entities directly* — leaks evaluator-internal fields (DimensionVector, conversion factors, Tier) onto the public API, creating a dependency between the public surface and internal implementation changes.

### Decision 14: `PreceptValue` Is Strictly Internal — Never in Public API Signatures

**Decision:** `PreceptValue` must never appear in any public method signature, return type, property type, or generic constraint. The public API uses a dual-lane model: the typed lane (`version.Get<Money>("field")`) materializes the appropriate CLR type; the raw lane (`version["field"]`) returns `JsonElement`. `PreceptValue` is invisible to callers.

**Three-layer model:**

| Layer | Type | Shape | Purpose |
|---|---|---|---|
| Internal runtime slots | `PreceptValue` | 32-byte tagged struct | Opaque tagged union; all field and arg values at runtime |
| Public typed lane | `Money`, `Quantity`, `Price`, etc. | `readonly record struct` | Materialized on `Get<T>()` — no allocation on read |
| Public raw lane | `JsonElement` | CLR struct | Wire-format access; zero Precept type dependency |

**Why this is non-negotiable (four reasons):**
1. **Brittleness:** `PreceptValue`'s layout (tag encoding, union regions) is an implementation detail that will evolve. Leaking it into public signatures creates breaking-change pressure on every layout evolution.
2. **AI agent hostility:** Opaque internal types degrade agent accuracy when reasoning about APIs. `version.Get<Money>("amount")` is self-explanatory; `version.GetPreceptValue("amount")` requires the agent to know the PreceptValue tag encoding to do anything useful with the result.
3. **Generic type leakage:** Generic type parameters are the hardest leakage vector to contain. `IReadOnlyList<PreceptValue>` on a public return type pulls the internal type into every caller's compilation unit.
4. **Dual-shape invariant:** The dual-shape boundary (Decision 13) is the correctness invariant for the entire CLR API. `PreceptValue` leaking past the boundary defeats the invariant — the two-layer model collapses to one.

**Alternative rejected:** *Exposing `PreceptValue` for "advanced" callers* — once it's in a public signature, it has public API stability obligations. The design intent is that `PreceptValue` is a private implementation contract between the builder and evaluator.

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
        case BinaryOp(var kind, var executor):
            PreceptValue r = stack[--top]; PreceptValue l = stack[--top];
            stack[top++] = executor(l, r);  // delegate embedded at build time; kind retained for trace
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
    EventOutcome.Transitioned(var newVersion)      => HandleSuccess(newVersion),
    EventOutcome.ConstraintsFailed(var violations) => HandleViolations(violations),
    EventOutcome.Unmatched()                       => HandleNoMatch(),
    // Compiler warning if any case is missing
};
```

No exceptions, no error codes, no magic strings. Every failure mode is a first-class type.

### Causal Violation Explanations

`ConstraintViolation` carries structured explanation depth:

```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,         // Which constraint failed
    string? Because,                         // "because inventory cannot go negative"
    ImmutableArray<FieldSnapshot> RelevantFields,  // What were the field values?
    string? FailingSubexpression,            // "quantity - sold"
    JsonElement? FailingValue               // Value at the failure site
);
```

This enables AI agents and UIs to explain WHY the constraint failed, not just THAT it failed.

### Computed Field Freshness in Restored Versions

`FromJson` populates slots directly from persisted field values. If computed fields are stored in the persistence envelope, those values may be stale relative to the underlying data if the formula changed since the data was persisted. Callers that need fresh computed values after restoration should call `version.InspectUpdate()` to see the current computed state, or `version.Update()` with no patch to re-evaluate.

This is a deliberate tradeoff: `FromJson` is a hydration primitive, not a recomputation pipeline. Freshness enforcement is a caller concern.

### Event-Prospect Evaluation in Update Inspection

`InspectUpdate` doesn't just show constraint results — it shows what events would become available:

```csharp
var inspection = version.InspectUpdate(new { quantity = 10 });

// What events can I fire after this update?
foreach (var eventInspection in inspection.Events)
{
    Console.WriteLine($"Prospect: {eventInspection.OverallProspect}");
}
// Output:
// submit: Certain (guard now passes)
// cancel: Possible (some rows match)
// archive: Impossible (still blocked by constraint)
```

This enables progressive UIs: show users what actions become available as they fill in fields.

---

## 13. Implementation Notes

### Implementation Status

1. **All operation bodies throw `NotImplementedException`** — `Fail(FaultCode)` routing to `Faults.Create` is the only implemented path.

2. **Descriptor types are defined but pending full implementation** — `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor` exist in `Descriptors.cs`; shapes need `AccessModes` and computed plan references.

3. **Opcode types are designed but not implemented** — The `Opcode` DU and its subtypes are specified in `precept-builder.md` but no C# types exist yet.

4. **Working copy allocation strategy** — Is the slot array stack-allocated (via `stackalloc` for small precepts) or always heap-allocated? Pending implementation decision.

5. **`Version.Slots` storage representation** — `Version.Slots` is documented as `PreceptValue[]` with donation/copy-on-write semantics. The exact ownership model (raw array donation vs. immutable wrapper) is a pending implementation decision affecting snapshot semantics and zero-copy promotion reach.

6. **`FieldDescriptor.AccessModes` structural shape** — Per-state access-mode lookup storage shape (dictionary vs. indexed) affects lookup cost and descriptor size. Pending implementation decision for the builder/descriptor pass.

7. **`AmbiguousDispatch` FaultCode (CC#13)** — `FaultCode.AmbiguousDispatch` confirmed with `[StaticallyPreventable(DiagnosticCode.AmbiguousDispatch)]`. The evaluator call site `Fail(FaultCode.AmbiguousDispatch)` at the `candidates.Count > 1` branch is the correct runtime backstop; the proof engine's exclusivity analysis prevents this from firing on a cleanly-proven program.

### Resolved Design Questions

8. **Constraint evaluation order** — Constraints are evaluated in bucket order: `always` → `from<current>` → `on<event>` → `to<target>`. Within a bucket, order is declaration order (stable, debuggable).

9. **Inspection–commit plan sharing** — Confirmed: same plans, same execution. Disposition (report vs. enforce) is the only difference.

10. **Fault backstop threading (CC#6)** — Every backstop site carries a non-null `FaultSiteAnnotation` on the opcode, stamped by the builder from `ProofLedger.FaultSiteLinks`. The evaluator checks `opcode.FaultSite` after dispatch; null = proven safe, no check. Non-null triggers `Faults.Create(code, args)`, not a raw throw. For cleanly-compiled precepts, all annotations are null — zero overhead.

11. **`ConstraintViolation` full shape** — Canonical 5-field shape is the designed public contract. Implementation of `FailingSubexpression` and `FailingValue` population requires evaluator instrumentation (tracking which opcode produced the failing value). `Because` comes from `ConstraintDescriptor` metadata; `RelevantFields` is a snapshot of `version.Slots` at evaluation time. Full population is an implementation milestone, not a design question.

12. **Event-prospect evaluation in `InspectUpdate`** — Runs `InspectFire` over all events available in the current state, using the hypothetical post-patch field values via a temporary `Version` object. Consider lazy evaluation for precepts with many events.

13. **Working copy isolation** — Each candidate row evaluation must have its own working copy. Rows are evaluated in order; a failing row's mutations must not leak to subsequent rows.

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
| MCP runtime-tool status and contract doc | `docs/tooling/mcp.md §4` |

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
| `Create` (stateless, with initial event) | no | yes (initial event) | same as `Fire` — no state-entry buckets (`to`, `in`) |
| `Create` (stateless, without initial event) | no | no | `always` only — no `in <state>` bucket exists |
| `Restore` | no (bypassed) | no | `always`, `in <current>` — computed fields recomputed first |

**Key rules:**
- Restore bypasses access-mode checks but enforces constraint checks
- Inspection and commit paths execute the same prebuilt plans — disposition alone differs
- `to <State>` constraints are evaluated only during Fire, not Update or Restore

---

## 16. Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Evaluator.cs` | Evaluator implementation — static class with Fire, Update, InspectFire, InspectUpdate methods |
| `src/Precept/Runtime/Precept.cs` | Executable model — Create, InspectCreate, FromJson entry points |
| `src/Precept/Runtime/Version.cs` | Entity instance — Fire, Update, InspectFire, InspectUpdate, ToJson façade |
| `src/Precept/Runtime/EventOutcome.cs` | Fire and Create outcome DU (with nested variants) |
| `src/Precept/Runtime/UpdateOutcome.cs` | Update outcome DU (with nested variants) |
| `src/Precept/Runtime/Inspection.cs` | Inspection types (`EventInspection`, `UpdateInspection`, `TransitionInspection`, `RowEffect`, `ArgError`, `ConstraintResult`, `FieldSnapshot`) |
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
