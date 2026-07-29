# Precept Builder

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Stub — error guard implemented; transformation logic not implemented; `Precept.From` throws `NotImplementedException` after `HasErrors` check |
| Source | `src/Precept/Runtime/Precept.cs` (`Precept.From` factory), `src/Precept/Runtime/PreceptBuilder.cs` (builder implementation — planned) |
| Upstream | `Compilation` (when `!HasErrors`) — carries `SemanticIndex`, `StateGraph`, `ProofLedger` |
| Downstream | All runtime operations (Create, Fire, Update, Inspect), LS preview, MCP runtime tools |

---

## Contents

- [2. Overview](#2-overview)
- [3. Responsibilities and Boundaries](#3-responsibilities-and-boundaries)
  - [In Scope](#in-scope)
  - [Out of Scope](#out-of-scope)
- [4. Right-Sizing](#4-right-sizing)
- [5. Inputs and Outputs](#5-inputs-and-outputs)
  - [Input](#input)
  - [Output](#output)
- [6. Architecture](#6-architecture)
  - [Six-Pass Transformation Pipeline](#six-pass-transformation-pipeline)
  - [Pass Dependencies and Valid Ordering](#pass-dependencies-and-valid-ordering)
- [7. Component Mechanics](#7-component-mechanics)
  - [Pass 1 — Descriptor Pass](#pass-1-descriptor-pass)
  - [Pass 2 — Slot Layout Pass](#pass-2-slot-layout-pass)
  - [Pass 3 — Dispatch Index Pass](#pass-3-dispatch-index-pass)
  - [Pass 4 — Execution Plan Pass](#pass-4-execution-plan-pass)
  - [Pass 5 — Constraint Plan Pass](#pass-5-constraint-plan-pass)
  - [Pass 6 — Constraint Influence Pass](#pass-6-constraint-influence-pass)
- [8. Dependencies and Integration Points](#8-dependencies-and-integration-points)
  - [Upstream Dependencies](#upstream-dependencies)
  - [Catalog Dependencies](#catalog-dependencies)
  - [Downstream Consumers](#downstream-consumers)
  - [Integration Contract](#integration-contract)
- [9. Failure Modes and Recovery](#9-failure-modes-and-recovery)
  - [Error Guard](#error-guard)
  - [The Certificate Gate](#the-certificate-gate)
  - [Builder Invariant Violations](#builder-invariant-violations)
  - [No Partial Builds](#no-partial-builds)
  - [Recovery Path](#recovery-path)
- [10. Contracts and Guarantees](#10-contracts-and-guarantees)
  - [Input Contract](#input-contract)
  - [Output Contract](#output-contract)
  - [Slot Index Invariants](#slot-index-invariants)
  - [Arg Slot Invariants](#arg-slot-invariants)
  - [Constraint Plan Invariants](#constraint-plan-invariants)
  - [Stateless Precept Handling](#stateless-precept-handling)
- [11. Design Rationale and Decisions](#11-design-rationale-and-decisions)
  - [Decision 1: Compile-to-Runtime Boundary is One-Way and Total](#decision-1-compile-to-runtime-boundary-is-one-way-and-total)
  - [Decision 2: Flat Opcodes Over Recursive AST Evaluation](#decision-2-flat-opcodes-over-recursive-ast-evaluation)
  - [Decision 3: Dispatch-Optimized Constraint Index](#decision-3-dispatch-optimized-constraint-index)
  - [Decision 4: Slot-Addressed Field Storage](#decision-4-slot-addressed-field-storage)
  - [Decision 5: Builder Does NOT Re-Read Catalogs for Semantic Classification](#decision-5-builder-does-not-re-read-catalogs-for-semantic-classification)
  - [Decision 6: ConstraintInfluenceMap as a Built Artifact](#decision-6-constraintinfluencemap-as-a-built-artifact)
- [12. Innovation](#12-innovation)
  - [Dispatch-Optimized Constraint Indexes](#dispatch-optimized-constraint-indexes)
  - [Flat Evaluation Plans](#flat-evaluation-plans)
  - [ConstraintInfluenceMap as First-Class Artifact](#constraintinfluencemap-as-first-class-artifact)
  - [Single-Factory Construction Pattern](#single-factory-construction-pattern)
- [13. Open Questions / Implementation Notes](#13-open-questions-implementation-notes)
  - [Open Questions](#open-questions)
  - [Implementation Status](#implementation-status)
  - [Resolved Questions](#resolved-questions)
  - [Pending Design Decisions](#pending-design-decisions)
- [14. Deliberate Exclusions](#14-deliberate-exclusions)
  - [No Semantic Re-Analysis](#no-semantic-re-analysis)
  - [No Evaluation](#no-evaluation)
  - [No Incremental Rebuild](#no-incremental-rebuild)
  - [No Optimization Passes](#no-optimization-passes)
  - [No Validation Beyond Structural Consistency](#no-validation-beyond-structural-consistency)
- [15. Cross-References](#15-cross-references)
- [16. Source Files](#16-source-files)

## 2. Overview

The Precept Builder is the **compile-to-runtime boundary** — the one-way transformation from semantic/analysis artifacts to dispatch-optimized execution structures. It transforms the `Compilation` object (carrying `SemanticIndex`, `StateGraph`, and `ProofLedger`) into the sealed `Precept` executable model.

**Pipeline Position:**

```text
Source Text → Lexer → Parser → Name Binder → Type Checker → Graph Analyzer → Proof Engine → [Precept Builder] → Evaluator
                                    ↓               ↓               ↓                ↓
                              SemanticIndex    StateGraph      ProofLedger        Precept
```

All three analysis artifacts arrive packaged as a `Compilation` object. `Precept.From(Compilation)` is the sole entry point and sole factory for the `Precept` executable model.

### Restructuring, not renaming

The runtime model is organized for execution, not for semantic analysis. **An implementer must NOT map `SemanticIndex` types 1:1 to runtime types.** The Precept Builder is a selective, restructuring transformation:

- Constraint plans are grouped by **activation anchor** (always / state-anchored / event-anchored), not by source declaration order
- Action plans are grouped by **transition row**, not by field
- Expressions are lowered to **flat opcode arrays** addressed by slot index, not preserved as recursive trees
- Descriptors are **first-class runtime identity**, not back-pointers to source declarations

The runtime model is a dispatch-optimized index, not a renamed analysis model. The evaluator becomes a plan executor that does not reason about semantics at runtime because the build step has already resolved all semantic questions.

The surveyed systems confirm this pattern: CEL's `Program` is a lowered `Interpretable` tree optimized for evaluation, not a copy of the checked AST; OPA's `Compiler` builds internal rule indexes that restructure policy for efficient top-down evaluation; XState v5 transforms machine configuration into a normalized internal model with precomputed transition maps.

**Key design identity:** The builder restructures, not renames. The runtime model is organized for execution — constraint plans grouped by activation anchor, actions grouped by transition row, fields addressed by slot index. An implementer who copies `SemanticIndex` shapes 1:1 to the runtime model is violating this identity.

**Key distinction:** The builder does NOT parse, type-check, or analyze. Those responsibilities belong to upstream stages. The builder reads already-resolved semantic facts and builds execution structures optimized for the evaluator's access patterns. Once the `Precept` is built, the analysis artifacts are no longer needed; the runtime operates entirely from the built model.

The builder executes six sequential transformation passes:

1. **Descriptor pass** — build descriptor tables from typed declarations; assign slot indexes
2. **Slot layout pass** — produce the `SlotLayout` register file specification
3. **Dispatch index pass** — build `TransitionDispatchIndex` and `ReachabilityIndex` from topology
4. **Constraint plan pass** — sort constraints into activation-anchor buckets
5. **Execution plan pass** — compile typed expressions and actions into flat opcode arrays
6. **Constraint influence pass** — build `ConstraintInfluenceMap` from `ProofLedger.ConstraintInfluenceEntries`

---

## 3. Responsibilities and Boundaries

### In Scope

| Responsibility | Description |
|---|---|
| **Descriptor table construction** | Build `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor` tables from `SemanticIndex` typed declarations |
| **Slot index assignment** | Assign sequential slot indexes to fields in declaration order; computed fields get slots |
| **Slot layout production** | Build the `SlotLayout` register file specification from field descriptors |
| **Dispatch index building** | Build `TransitionDispatchIndex` from `StateGraph` topology — pre-indexed (state, event) → rows |
| **Reachability index building** | Build `ReachabilityIndex` from `StateGraph` — pre-computed state reachability sets |
| **Constraint plan organization** | Sort `ConstraintDescriptor` entries into activation-anchor buckets (`always`, `in`, `from`, `to`, `on`) |
| **Execution plan compilation** | Compile typed expressions and action chains into flat slot-addressed opcode arrays |
| **Constraint influence map construction** | Reshape `ProofLedger.ConstraintInfluenceEntries` into query-efficient `ConstraintInfluenceMap` |

### Out of Scope

| Exclusion | Rationale |
|---|---|
| **Lexing / parsing** | The lexer and parser produce the construct manifest. The builder consumes typed semantic facts, not tokens or parse nodes. |
| **Semantic analysis** | The type checker resolves names, types, and overloads. The builder reads resolved `TypedField`, `TypedState`, `TypedTransitionRow`, etc. |
| **Graph analysis** | The graph analyzer computes topology, reachability, and dominance. The builder reads the `StateGraph` output. |
| **Proof discharge** | The proof engine generates and discharges obligations. The builder reads `ProofLedger` results. |
| **Operation execution** | The evaluator walks execution plans and checks each entering value against the constraints declared on the slot it enters. It does not evaluate the definition's rules against the finished configuration — the compiler proved every handler preserves them (`precept-language-spec.md` §0.7). The builder produces plans, not outcomes. |
| **Incremental rebuild** | The builder runs once per `Compilation`. No incremental update path exists — a source change produces a new `Compilation`, which produces a new `Precept`. |

---

## 4. Right-Sizing

The Precept Builder is scoped precisely as the transformation boundary — it is the only code path that builds the runtime model. `Precept.From` is the sole factory.

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 600–900 | ~200 descriptor construction + ~100 slot layout + ~150 dispatch index + ~150 constraint plan + ~150 execution plan compiler |
| External dependencies | 0 | Pure transformation — no I/O, no external services, no runtime calls |
| Catalog dependency | ~30% | Reads `Constraints.GetMeta(kind)` for anchor classification, `Actions.GetMeta(kind)` for action plan shapes |
| Determinism | 100% | Same `Compilation` always produces the same `Precept` |

**Why a separate builder (not inline in `Precept.From`)?**

The builder's logic is substantial — six transformation passes with distinct responsibilities. Inlining this into `Precept.From` would violate single-responsibility and make testing difficult. The builder is a pure function: `Compilation → Precept`. It can be unit-tested in isolation with synthetic `Compilation` inputs.

**Why not merge into the type checker?**

The type checker produces `SemanticIndex` — a semantic model organized for analysis (declarations by kind, expressions with types, dependency facts). The builder reorganizes this for execution (constraints by anchor, fields by slot, actions by row). These are fundamentally different access patterns. Merging them would conflate analysis and execution concerns.

**Bounded complexity:**

The builder has no algorithmic complexity beyond iteration and indexing. It does not compute fixpoints, solve constraints, or traverse graphs. The graph analyzer and proof engine handle those concerns. The builder is pure structural mapping — catalog-classified input → dispatch-optimized output.

---

## 5. Inputs and Outputs

### Input

**`Compilation`** — the pipeline's analysis output, gated by `!HasErrors`:

```csharp
public sealed record Compilation(
    TokenStream Tokens,         // lexical token stream — language server semantic-token pass reads this (CC#4)
    SemanticIndex SemanticIndex,
    StateGraph StateGraph,
    ProofLedger ProofLedger,
    ImmutableArray<Diagnostic> Diagnostics
)
{
    public bool HasErrors => Diagnostics.Any(d => d.Severity == Severity.Error);
}
```

The builder reads all three analysis artifacts:

| Artifact | What it provides |
|---|---|
| `SemanticIndex` | Typed declarations: `TypedField`, `TypedState`, `TypedEvent`, `TypedArg`, `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, `TypedAction`, `TypedExpression` |
| `StateGraph` | Topology: `GraphState`, `GraphEdge`, `ReachableStates`, `EventCoverage` |
| `ProofLedger` | Proof results: `ConstraintInfluenceEntries` |

### Output

**`Precept`** — the sealed executable model:

```csharp
public sealed class Precept
{
    // ── Descriptor tables ────────────────────────────────────────
    public IReadOnlyList<FieldDescriptor> Fields { get; }
    public IReadOnlyList<StateDescriptor> States { get; }
    public IReadOnlyList<EventDescriptor> Events { get; }
    public IReadOnlyList<ConstraintDescriptor> Constraints { get; }
    
    // ── Identity markers ─────────────────────────────────────────
    public StateDescriptor? InitialState { get; }
    public EventDescriptor? InitialEvent { get; }
    public bool IsStateless { get; }
    
    // ── Internal execution structures (not public API) ───────────
    internal SlotLayout SlotLayout { get; }
    internal TransitionDispatchIndex DispatchIndex { get; }
    internal ReachabilityIndex ReachabilityIndex { get; }
    internal ConstraintPlanIndex ConstraintPlanIndex { get; }
    internal ConstraintInfluenceMap ConstraintInfluenceMap { get; }
}
```

---

## 6. Architecture

### Six-Pass Transformation Pipeline

The builder executes six sequential passes, each producing artifacts consumed by subsequent passes:

```text
┌─────────────────────────────────────────────────────────────────────┐
│                           Compilation                                │
│              (SemanticIndex + StateGraph + ProofLedger)              │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 1: Descriptor Pass                                             │
│  • TypedField → FieldDescriptor (with SlotIndex)                     │
│  • TypedState → StateDescriptor                                      │
│  • TypedEvent → EventDescriptor (with ArgDescriptor[])               │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 2: Slot Layout Pass                                            │
│  • Compute FieldCount                                                │
│  • Identify ComputedSlots (require recompute on each transition)     │
│  • Build SlotLayout register file specification                      │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 3: Dispatch Index Pass                                         │
│  • Build TransitionDispatchIndex: (state, event) → ExecutionRow[]    │
│  • Build ReachabilityIndex: state → reachable states                 │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 4: Execution Plan Pass                                         │
│  • Compile TypedExpression trees → flat ExecutionPlan opcode arrays  │
│  • Compile TypedAction chains → ActionPlan arrays                    │
│  • Build ExecutionRow from TypedTransitionRow                        │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 5: Constraint Plan Pass                                        │
│  • Build ConstraintDescriptor with compiled ExecutionPlan            │
│  • Route into anchor buckets: always, in<S>, from<S>, to<S>, on<E>   │
│  • Build ConstraintPlanIndex                                         │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 6: Constraint Influence Pass                                   │
│  • Build ConstraintInfluenceMap from ConstraintInfluenceEntries      │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                              Precept                                 │
└─────────────────────────────────────────────────────────────────────┘
```

### Pass Dependencies and Valid Ordering

| Pass | Depends On | Produces |
|---|---|---|
| Pass 1 (Descriptor) | `SemanticIndex` | Descriptor tables, slot index assignments |
| Pass 2 (Slot Layout) | Pass 1 | `SlotLayout` |
| Pass 3 (Dispatch Index) | Pass 1, `StateGraph` | `TransitionDispatchIndex`, `ReachabilityIndex` |
| Pass 4 (Execution Plan) | Pass 1 (slot indexes) | `ExecutionPlan`, `ActionPlan`, `ExecutionRow` |
| Pass 5 (Constraint Plan) | Pass 4 (compiled expressions) | `ConstraintPlanIndex` |
| Pass 6 (Constraint Influence) | `ProofLedger` | `ConstraintInfluenceMap` |

**Valid execution order:** 1 → 2 (parallel with 3) → 4 → 5 → 6 → assemble `Precept`

Note: Pass 5 (Constraint Plan) depends on Pass 4 (Execution Plan) because `ConstraintDescriptor` requires a compiled `ExecutionPlan` for its expression — pass numbers now reflect execution order.

---

## 7. Component Mechanics

### Pass 1 — Descriptor Pass

Build descriptor tables from `SemanticIndex` typed declarations:

**Field Descriptors:**

The input `TypedField` shape is defined canonically in `type-checker.md` §7.1. Key fields for the builder:

```csharp
// Output: FieldDescriptor with assigned SlotIndex and ClrType
public sealed record FieldDescriptor(
    string Name,
    TypeKind Type,
    int SlotIndex,                              // assigned sequentially in declaration order
    ImmutableArray<ModifierKind> Modifiers,
    bool IsComputed,
    bool IsOptional,
    SourceSpan Source,
    Type ClrType                                // resolved at build time; IReadOnlyList<T> for collection-typed fields
);
```

`ClrType` is resolved from `TypeMeta.ClrType` (the base element type) plus collection wrapping for collection-typed fields. This is the valid `T` for `version.Get<T>(fieldName)`. For `list of integer`, `ClrType` is `typeof(IReadOnlyList<long>)`.

**`AccessModes` property:** `FieldDescriptor` needs an `AccessModes` property for per-state access-mode lookup (per evaluator.md §7.5). The exact shape (`ImmutableDictionary<StateDescriptor?, AccessMode>` vs. `ImmutableArray<(StateDescriptor?, AccessMode)>`) is a pending implementation decision.

Slot indexes are assigned during this pass: fields receive sequential `SlotIndex` values in declaration order, starting at 0. Computed fields get slots too — their values are recalculated by the evaluator on each transition.

**State Descriptors:**

```csharp
// Output: StateDescriptor (no slot — states are identity, not data)
public sealed record StateDescriptor(
    string Name,
    ImmutableArray<ModifierKind> Modifiers,     // initial, terminal, required, irreversible
    SourceSpan Source
);
```

**Event Descriptors:**

```csharp
// Output: EventDescriptor with ArgDescriptor array
public sealed record EventDescriptor(
    string Name,
    ImmutableArray<ArgDescriptor> Args,
    ImmutableArray<ModifierKind> Modifiers,
    bool IsInitial,
    SourceSpan Source
);

public sealed record ArgDescriptor(
    string Name,
    TypeKind Type,
    bool IsOptional,
    int SlotIndex,
    Type ClrType                                // resolved at build time — valid T for FiredArgs.Get<T>()
);
```

Arg slot indexes are assigned during this pass: each arg in an event's arg declaration list receives a sequential `SlotIndex` value in declaration order, starting at 0. This mirrors the field slot assignment invariant: the first declared arg gets index 0, the second gets index 1, and so on. `LOAD_ARG` opcodes carry arg slot indexes, not arg names — the name-to-index mapping is resolved at build time and never consulted at execution time.

**Why descriptors?** Descriptors are the runtime's vocabulary — named, typed, immutable value-objects that carry all the information needed for display, debugging, inspection, and dispatch. They are read-only structural mirrors of the semantic declarations, organized for fast lookup. The slot index is the field's address in the entity's runtime value array.

### Pass 2 — Slot Layout Pass

After all field descriptors have slots, produce a `SlotLayout`:

```csharp
public sealed record SlotLayout(
    int FieldCount,
    ImmutableArray<FieldDescriptor> Fields,      // in slot order
    ImmutableArray<int> ComputedSlots            // slots that require recompute after each transition
);
```

The slot layout is the evaluator's register file specification. Every entity instance (`Version`) is a `PreceptValue[]` slot array of size `FieldCount`. The evaluator uses slot indexes, never field names, during execution.

> **Vocabulary:** These are **field slots** — runtime storage positions in the `PreceptValue[]` working-copy array, owned by `SlotLayout`, built during `Precept.From()`. They are distinct from **construct slots** (`ParsedConstruct.Slots` / `SlotValue`), which are compile-time parse positions owned by the Constructs catalog. The two concepts do not share lifecycle, representation, or owner. When discussion crosses layers, use these terms explicitly.

**Computed slots:** Fields with `IsComputed == true` are collected into the `ComputedSlots` array. After every state transition, the evaluator walks this array and recalculates each computed field's value before the working copy is committed.

### Pass 3 — Dispatch Index Pass

Build the `TransitionDispatchIndex` and `ReachabilityIndex` from the `StateGraph`.

**TransitionDispatchIndex:**

For each (source state, event) pair, pre-build the ordered list of transition rows (in guard-evaluation order). At runtime, the evaluator looks up the pair and gets a ready-to-evaluate row list — no scanning, no filtering.

```csharp
public sealed record TransitionDispatchIndex(
    // Key: (StateDescriptor?, EventDescriptor) — null state = stateless precept or any-state wildcard
    // Value: ordered list of execution rows for this dispatch pair (guard-evaluation order)
    ImmutableDictionary<(StateDescriptor?, EventDescriptor), ImmutableArray<ExecutionRow>> Rows
);
```

**ReachabilityIndex:**

For each state, the set of states reachable from it (directly or transitively). Consumed by tooling (language server, MCP) to answer "what transitions are possible from here?"

```csharp
public sealed record ReachabilityIndex(
    ImmutableDictionary<StateDescriptor, ImmutableHashSet<StateDescriptor>> FromState
);
```

### Pass 4 — Execution Plan Pass

Compile typed action chains and typed guard/constraint expressions from the `SemanticIndex` into flat opcode arrays.

**Nothing is stamped on an opcode for a fault.** An obligation the proof engine could not discharge rejects the definition, so no `Compilation` reaching the builder carries an unproven site, and there is nothing for the builder to annotate. The compiled opcode array carries only what the evaluator computes with (`precept-language-spec.md` §0.7).

**Opcode Inventory:**

An `ExecutionPlan` is a flat array of `Opcode` values that the evaluator walks left to right. Opcodes operate on a stack (push/pop) and reference fields by slot index.

| Opcode | Stack Effect | Description |
|---|---|---|
| `LOAD_SLOT(i)` | push | Load field value at slot index `i` |
| `LOAD_ARG(argSlotIndex)` | push | Load event arg value by pre-resolved arg slot index |
| `LOAD_LIT(value)` | push | Push literal value |
| `STORE_SLOT(i)` | pop | Pop and store to slot index `i` |
| `BINARY_OP(kind)` | pop 2, push 1 | Binary operation (catalog-driven via `OperationMeta`) |
| `UNARY_OP(kind)` | pop 1, push 1 | Unary operation |
| `CALL_FUNCTION(kind, arity)` | pop N, push 1 | Function call (catalog-driven via `FunctionMeta`) |
| `MEMBER_ACCESS(accessor)` | pop 1, push 1 | Type accessor (catalog-driven via `TypeAccessor`) |
| `COLLECTION_OP(kind, slot)` | varies | Collection mutation actions (enqueue, dequeue, append, etc.) |
| `BRANCH_FALSE(offset)` | pop 1 | Short-circuit branch (for `&&`) |
| `BRANCH_TRUE(offset)` | pop 1 | Short-circuit branch (for `||`) |
| `JUMP(offset)` | — | Unconditional jump (for `?:`) |
| `RETURN` | pop 1 | End of plan, result is top of stack |
| `DUP` | pop 1, push 2 | Duplicate top of stack |
| `POP` | pop 1 | Discard top of stack |

**ExecutionPlan:**

```csharp
public sealed record ExecutionPlan(
    ImmutableArray<Opcode> Opcodes,
    TypeKind ResultType,
    int MaxStackDepth           // computed at build time; compiler enforces ≤ 32
);

public abstract record Opcode { }
public sealed record LoadSlot(int SlotIndex) : Opcode;
public sealed record LoadArg(int ArgSlotIndex) : Opcode;  // pre-resolved at build time; name resolution complete before execution
public sealed record LoadLit(PreceptValue Value) : Opcode;  // literals pre-wrapped at build time
public sealed record StoreSlot(int SlotIndex) : Opcode;
public sealed record BinaryOp(OperationKind Kind, Func<PreceptValue, PreceptValue, PreceptValue> Executor) : Opcode;
public sealed record UnaryOp(OperationKind Kind, Func<PreceptValue, PreceptValue> Executor) : Opcode;
public sealed record CallFunction(FunctionKind Kind, int Arity) : Opcode;
public sealed record MemberAccess(TypeAccessor Accessor) : Opcode;
public sealed record CollectionOp(ActionKind Kind, int SlotIndex) : Opcode;
public sealed record BranchFalse(int Offset) : Opcode;
public sealed record BranchTrue(int Offset) : Opcode;
public sealed record Jump(int Offset) : Opcode;
public sealed record Return() : Opcode;
public sealed record Dup() : Opcode;
public sealed record Pop() : Opcode;
```

**ExecutionRow:**

The compiled representation of a `TypedTransitionRow`:

```csharp
public sealed record ExecutionRow(
    StateDescriptor? SourceState,       // null for any-state rows
    EventDescriptor Event,
    ExecutionPlan? Guard,               // null if no guard
    ImmutableArray<ActionPlan> Actions,
    TransitionOutcome Outcome,
    StateDescriptor? TargetState,       // null if outcome doesn't specify target
    string? RejectReason,               // non-null iff Outcome == Reject; authored "because" text (CC#11)
    SourceSpan Source
);

public sealed record ActionPlan(
    ActionKind Kind,
    int TargetSlot,                     // field to mutate
    ExecutionPlan? Value,               // null for FieldOnly actions (clear, increment, etc.)
    int? IntoSlot                       // for CollectionInto actions (dequeue into X)
);

public enum TransitionOutcome { Transition, NoTransition, Reject }
```

**Reject reason lowering (CC#11):** `TransitionOutcome.Reject` rows carry an authored `because` message. `ExecutionRow.RejectReason` is the canonical home — `null` for non-reject rows, populated from the `because` clause text for reject rows. The evaluator reads `winningRow.RejectReason` to return the authored `Rejected(string reason)` outcome without re-reading source.

**Catalog-driven compilation:** The expression compiler reads `Operations.GetMeta(kind)`, `Functions.GetMeta(kind)`, and `Types.GetAccessor(type, name)` to emit the correct opcodes. It does not hardcode per-operation behavior — it dispatches on catalog entries. When emitting `BinaryOp` or `UnaryOp`, the builder resolves the `OperationMeta`, fetches the executor delegate from the corresponding `TypeRuntimeMeta.BinaryExecutors[localIndex]` (or `UnaryExecutors`), and embeds it directly in the opcode record. The evaluator never indexes `TypeRuntimeMeta` arrays — the delegate is already embedded.

### Pass 5 — Constraint Plan Pass

Sort constraint descriptors into activation-anchor buckets:

```csharp
public sealed record ConstraintPlanIndex(
    ImmutableArray<ConstraintDescriptor> Always,                                          // always constraints
    ImmutableDictionary<StateDescriptor, ImmutableArray<ConstraintDescriptor>> InState,   // in <state>
    ImmutableDictionary<StateDescriptor, ImmutableArray<ConstraintDescriptor>> FromState, // from <state>
    ImmutableDictionary<StateDescriptor, ImmutableArray<ConstraintDescriptor>> ToState,   // to <state>
    ImmutableDictionary<EventDescriptor, ImmutableArray<ConstraintDescriptor>> OnEvent    // on <event>
);

public sealed record ConstraintDescriptor(
    ConstraintKind Kind,
    StateDescriptor? StateAnchor,      // null for global constraints
    EventDescriptor? EventAnchor,      // null for non-event constraints
    ExecutionPlan Expression,          // compiled expression (from Pass 4)
    string? Because,
    SourceSpan Source
);
```

The evaluator never scans or filters constraints at dispatch time — it reads the pre-built bucket for its current context. The constraint plan pass uses `ConstraintMeta` catalog entries (via `Constraints.GetMeta(kind)`) to classify each constraint into its anchor family by pattern-matching on the DU subtype:

```csharp
var bucket = Constraints.GetMeta(constraint.Kind) switch
{
    ConstraintMeta.Invariant => alwaysBucket,
    ConstraintMeta.StateResident => inStateBucket,
    ConstraintMeta.StateEntry => toStateBucket,
    ConstraintMeta.StateExit => fromStateBucket,
    ConstraintMeta.EventPrecondition => onEventBucket,
    _ => throw new InvalidOperationException()
};
```

**`ConstraintMeta` DU hierarchy (CC#7 — Option B, hierarchical with `StateAnchored` grouping node):**

```csharp
public abstract record ConstraintMeta(...)
{
    public sealed record Invariant()         : ConstraintMeta(...);
    public abstract record StateAnchored()   : ConstraintMeta(...);   // grouping node
        public sealed record StateResident() : StateAnchored(...);
        public sealed record StateEntry()    : StateAnchored(...);
        public sealed record StateExit()     : StateAnchored(...);
    public sealed record EventPrecondition() : ConstraintMeta(...);
}
```

The builder's 5-way bucket switch routes to all five concrete leaf types. The `StateAnchored` abstract intermediate node is available for "is this state-scoped?" grouping checks in the type checker and proof engine (`meta is ConstraintMeta.StateAnchored`) without requiring a separate flag. The catalog is the source of truth; the builder never hardcodes the DU shape.

### Pass 6 — Constraint Influence Pass

Build the `ConstraintInfluenceMap` from `ProofLedger.ConstraintInfluence`. This is a direct reshaping of `ConstraintInfluenceEntry` records into a query-friendly map:

```csharp
public sealed record ConstraintInfluenceMap(
    // For each constraint: which fields influence its evaluation?
    ImmutableDictionary<ConstraintDescriptor, ImmutableArray<FieldDescriptor>> ConstraintToFields,
    // Inverse: for each field: which constraints does it influence?
    ImmutableDictionary<FieldDescriptor, ImmutableArray<ConstraintDescriptor>> FieldToConstraints
);
```

This enables causal reasoning: "which fields affect which constraints?" — a first-class query for AI agents and tooling.

---

## 8. Dependencies and Integration Points

### Upstream Dependencies

| Dependency | Artifact | What the builder reads |
|---|---|---|
| **Type Checker** | `SemanticIndex` | `TypedField`, `TypedState`, `TypedEvent`, `TypedArg`, `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, `TypedAction`, `TypedExpression` subtypes |
| **Graph Analyzer** | `StateGraph` | `GraphState`, `GraphEdge`, `ReachableStates`, `EventCoverage` |
| **Proof Engine** | `ProofLedger` | `ConstraintInfluenceEntries` |

### Catalog Dependencies

The builder reads catalog metadata for classification and dispatch:

| Catalog | Usage |
|---|---|
| `Constraints.GetMeta(kind)` | Route each constraint into its anchor bucket — pattern-match on `ConstraintMeta` DU subtypes |
| `Actions.GetMeta(kind)` | Determine `ActionPlan` shape via `ActionMeta.SyntaxShape` |
| `Operations.GetMeta(kind)` | Emit `BINARY_OP` opcodes via `OperationMeta` |
| `Functions.GetMeta(kind)` | Emit `CALL_FUNCTION` opcodes via `FunctionMeta` |
| `Types.GetAccessor(type, name)` | Emit `MEMBER_ACCESS` opcodes via `TypeAccessor` |
| `Modifiers.GetMeta(kind)` | Attach `ModifierKind[]` to descriptors |

### Downstream Consumers

| Consumer | What it reads from `Precept` |
|---|---|
| **Evaluator** | `SlotLayout`, `TransitionDispatchIndex`, `ConstraintPlanIndex`, `ExecutionRow`, `ExecutionPlan` |
| **Language Server** | `Fields`, `States`, `Events`, `Constraints`, `ReachabilityIndex` — for preview and inspection |
| **MCP Tools** | `Fields`, `States`, `Events`, `Constraints` — for `precept_compile` output; `Precept` instance for `precept_fire`, `precept_inspect`, `precept_update` |
| **Version (entity instance)** | `SlotLayout.FieldCount` — allocates slot array; `FieldDescriptor.SlotIndex` — accesses fields |

### Integration Contract

```csharp
// The sole entry point — no other way to construct Precept
public static Precept From(Compilation compilation)
{
    if (compilation.HasErrors)
        throw new InvalidOperationException("Cannot create a Precept from a compilation with errors.");
    
    // Six-pass transformation pipeline
    var builder = new PreceptBuilder(compilation);
    return builder.Build();
}
```

---

## 9. Failure Modes and Recovery

### Error Guard

`Precept.From` guards on `Compilation.HasErrors` — any analysis-phase error prevents builder execution:

```csharp
if (compilation.HasErrors)
    throw new InvalidOperationException("Cannot create a Precept from a compilation with errors.");
```

**Rationale:** An executable model from a broken program would be unsafe to execute. The builder does not attempt partial builds or error recovery. The correct fix is to resolve the compilation errors and retry.

### The Certificate Gate

The `HasErrors` guard is not the only condition `Precept.From` has to satisfy. `runtime-api.md` § Definition Load and the Certificate Gate says the method also runs the certificate checker over the proof record the compiler emits alongside the definition, and refuses to return a usable `Precept` when that record does not verify. That is the check standing behind the runtime's trust in the compiler, and it catches the case the `HasErrors` guard cannot: a definition that was tampered with after compilation, or that is stale with respect to the checker, and therefore carries no diagnostics to guard on.

> **Not yet built, and its failure shape is unsettled.** No certificate is emitted and no checker exists, so `Precept.From` performs no such gate today — `runtime-api.md` states this in the same words. Whether a verification failure throws or returns a structured result, whether it names the derivation step that failed to replay, and whether verification is cached across loads are all undecided. What is settled is that the failure must explain itself; an unexplained refusal is not an admissible shape.
>
> **Where the certificate arrives from is part of what is unsettled.** The `Compilation` record above carries five things and a certificate is not one of them; `runtime-api.md` describes the proof record as emitted *alongside* the definition rather than inside the compiler's analysis output. So the input contract's certificate condition is a condition on something `Precept.From` does not receive today, and whether it becomes a sixth member of `Compilation`, a separate argument, or a file the method is pointed at is decided with the rest of the shape.

Until that is decided, the rest of this section describes only the `HasErrors` guard and the builder's own invariants.

### Builder Invariant Violations

If the builder detects internal invariant violations (e.g., a `TypedTransitionRow` references a state that doesn't exist in `SemanticIndex.States`), it throws `InvalidOperationException` with diagnostic context. These indicate bugs in upstream stages — the builder assumes the `Compilation` is internally consistent.

### No Partial Builds

There is no partial `Precept`. The builder either produces a complete, valid executable model or throws. This is a deliberate design choice:

- A partial model would require every consumer to check "is this constraint index populated?" before use
- The evaluator would need null-checks on every index access
- Error handling would be scattered across the runtime instead of concentrated at the build boundary

### Recovery Path

If `Precept.From` refuses because the `Compilation` carries errors:

1. Examine the `Compilation.Diagnostics` for errors
2. Fix the source causing the errors
3. Recompile to produce a new `Compilation`
4. Retry `Precept.From`

That path does not fit a definition the certificate gate refuses, because such a definition carries no diagnostics to examine — the recovery is to recompile the definition from its source with the current compiler, not to fix errors that are not there. The precise recovery advice waits on the gate's failure shape being decided.

---

## 10. Contracts and Guarantees

### Input Contract

- `Precept.From(compilation)` requires `!compilation.HasErrors`
- The `Compilation` must be internally consistent (no dangling references, no duplicate names)
- The `SemanticIndex`, `StateGraph`, and `ProofLedger` must all be present and non-null
- The definition's proof certificate must verify — see § The Certificate Gate. A definition can satisfy all three conditions above and still be one the compiler never proved; the certificate is what distinguishes them. The condition is stated here because it belongs to the input contract; the mechanism is not built and its failure shape is not settled.

### Output Contract

- `Precept.From(compilation)` returns a complete, immutable `Precept` or throws
- The returned `Precept` is thread-safe and shareable across all entity instances
- Every `FieldDescriptor` has a unique `SlotIndex` within `[0, FieldCount)`
- Every `StateDescriptor` has a unique `Name` within the precept
- Every `EventDescriptor` has a unique `Name` within the precept
- Every `TransitionDispatchIndex` entry corresponds to a `TypedTransitionRow` in `SemanticIndex`
- Every `ConstraintDescriptor` in `ConstraintPlanIndex` corresponds to a `TypedRule` or `TypedEnsure` in `SemanticIndex`

### Slot Index Invariants

- Slot indexes are assigned sequentially in field declaration order
- `SlotLayout.FieldCount == Fields.Count`
- For all `FieldDescriptor f`: `0 <= f.SlotIndex < SlotLayout.FieldCount`
- `SlotLayout.Fields[i].SlotIndex == i` (slot-order array)

### Arg Slot Invariants

- Arg slot indexes are assigned sequentially in event arg declaration order, starting at 0
- For all `ArgDescriptor a` on event `e`: `0 <= a.SlotIndex < e.Args.Length`
- `e.Args[i].SlotIndex == i` (declaration-order array)
- Arg slot arrays are ephemeral — allocated at the Fire boundary, discarded after the call
- `LOAD_ARG(i)` opcodes carry `SlotIndex`, never arg names — all name resolution happens at build time

### Constraint Plan Invariants

- Every `ConstraintDescriptor` appears in exactly one bucket
- `Always` bucket contains only `ConstraintKind.Invariant` constraints
- `InState` buckets contain only `ConstraintKind.StateResident` constraints
- `FromState` buckets contain only `ConstraintKind.StateExit` constraints
- `ToState` buckets contain only `ConstraintKind.StateEntry` constraints
- `OnEvent` buckets contain only `ConstraintKind.EventPrecondition` constraints

**Constraint bucket routing:** The constraint-plan pass routes constraints into five anchor buckets driven by `ConstraintMeta` DU subtypes. The catalog system documents the five constraint meta shapes (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`) and the builder pattern-matches on them for routing. If catalog-system.md and the builder routing ever diverge, the catalog is the source of truth.

### Stateless Precept Handling

For stateless precepts:

- `States` is empty
- `InitialState` is null
- `IsStateless` is true
- `TransitionDispatchIndex` keys use `null` for the state component: `(null, event) → rows`

---

## 11. Design Rationale and Decisions

### Decision 1: Compile-to-Runtime Boundary is One-Way and Total

**Decision:** The builder either produces a complete, valid `Precept` or throws. No partial builds.

**Rationale:** A partial executable model would be unsafe to execute. Consumers would need defensive null-checks on every index access. The `HasErrors` guard ensures the builder never runs on broken programs — all validation happens in upstream stages.

**Alternatives rejected:**
- *Partial builds with missing features* — rejected because every consumer would need error handling for missing indexes
- *Graceful degradation* — rejected because a precept with missing constraints is semantically broken, not degraded

### Decision 2: Flat Opcodes Over Recursive AST Evaluation

**Decision:** Expression plans are compiled to flat opcode arrays (stack machine), not evaluated by walking a recursive AST at runtime.

**Rationale:**
- Cache-friendly: opcodes are contiguous in memory
- O(n) execution: one pass through the opcode array
- No stack depth limits from recursive dispatch
- Simpler evaluator implementation: a single `while` loop over opcodes
- Predictable performance: no hidden allocation, no call stack growth

**Alternatives rejected:**
- *Tree-walking interpreter* — rejected because recursive dispatch has unpredictable stack usage and poor cache locality
- *JIT compilation* — rejected as over-engineering for Precept's use case; adds complexity and startup latency

### Decision 3: Dispatch-Optimized Constraint Index

**Decision:** Constraints are sorted into anchor buckets at build time, not at dispatch time.

**Rationale:** The buckets are how the constraint catalogs are assembled (`Precept.Constraints`, `Version.ApplicableConstraints`) and how an incoming value is routed to the constraints declared on the slot it enters, without a scan. Sorting once at build time keeps both O(1) at execution time. The buckets are not walked to check the finished working copy — nothing does that (`evaluator.md` § 7.6, spec §0.7).

**Implementation:** Pattern-match on `ConstraintMeta` DU subtypes (not on `ConstraintKind` enum values directly). The DU encodes the anchor family; the builder dispatches on subtype to route into buckets.

### Decision 4: Slot-Addressed Field Storage

**Decision:** Fields are addressed by integer slot index in the runtime, not by name.

**Rationale:**
- O(1) field access: array index lookup
- Cache-local storage: entity state is a contiguous `object?[]`
- No string lookup at runtime
- Slot indexes are assigned at build time; the evaluator never resolves names

**Alternative rejected:**
- *Dictionary<string, object?>* — rejected because name-keyed lookup is O(1) amortized but has constant-factor overhead (hash computation, bucket traversal) that slot indexing avoids

### Decision 5: Builder Does NOT Re-Read Catalogs for Semantic Classification

**Decision:** All semantic decisions (what type is this field? what modifier applies? what does this action do?) were made by the type checker. The builder consumes already-resolved typed facts.

**Rationale:** Separation of concerns. The type checker owns semantic resolution; the builder owns structural transformation. No double-analysis, no risk of inconsistency between checker and builder interpretations.

**What the builder DOES read from catalogs:** Structural metadata for dispatch — `ConstraintMeta` DU subtypes for bucket routing, `ActionMeta.SyntaxShape` for action plan shapes, `OperationMeta` for opcode emission. These are not semantic decisions; they are structural classification for dispatch.

### Decision 6: ConstraintInfluenceMap as a Built Artifact

**Decision:** The proof engine computes field dependency edges for constraints; the builder reshapes them into a query-efficient map.

**Rationale:** Causal reasoning ("which fields affect which constraints?") is a first-class query for AI agents. Building the map at compile time means runtime queries are O(1) lookups, not O(constraints × fields) traversals.

**Ownership:** The proof engine computes `ConstraintInfluenceEntry` records (the raw dependency edges). The builder reshapes them into `ConstraintInfluenceMap` (the bidirectional query index). This division keeps the proof engine focused on analysis and the builder focused on indexing.

---

## 12. Innovation

### Constraint Indexes, and What They Index Now

Traditional constraint systems evaluate every constraint on every operation, and make that cheap by narrowing which ones apply. Precept does not evaluate the definition's rules per operation at all: the compiler proved before the engine existed that every handler preserves every rule, so there is no pass over the finished configuration to make cheap (`precept-language-spec.md` §0.7). What the runtime does check is each value entering from outside — an event argument, a construction input, an edited field — against the constraints declared on the slot it enters, at the moment it enters. That check is anchored to the slot, not to a state or an event.

The builder still sorts constraints into activation-anchor buckets, and the five anchors come from the language's own declaration forms:

- `always` — the constraint is declared with no anchor
- `in <State>` — declared as resident in that state
- `from <State>` — declared on leaving that state
- `to <State>` — declared on entering that state
- `on <Event>` — declared as a precondition of that event

Two uses for the buckets survive the removal of the per-operation pass, and `evaluator.md` § 7.6 states which: they assemble the definition-level and state-level constraint catalogs (`Precept.Constraints`, `Version.ApplicableConstraints`), and they route an incoming value to the constraints declared on the slot it enters. The `Always`, `FromState` and `ToState` buckets are never walked against a finished working copy.

> **Open — whether the index keeps this shape.** What remains undecided is not what the buckets are for but whether five state-and-event anchors are still the right shape for two uses that are anchored to a slot and to a catalog listing. The index could shrink to what ingress needs, or be replaced by a premise list derived from the definition's certificate. Nothing canonical answers that, and the buckets are described above as the builder produces them today. The same question reaches Pass 4 (§ 2), the `ConstraintPlanIndex` entry in § Downstream Consumers, and `catalog-system.md` § Evaluator-catalog integration pattern.

### Flat Evaluation Plans

Expressions are precomputed into flat, cache-friendly slot-addressed opcodes. The evaluator is a simple stack machine:

```csharp
var stack = new Stack<PreceptValue>();
foreach (var op in plan.Opcodes)
{
    switch (op)
    {
        case LoadSlot(var i): stack.Push(slots[i]); break;
        case LoadLit(var v): stack.Push(v); break;         // v is already PreceptValue
        case BinaryOp(var kind, var executor): 
            PreceptValue r = stack.Pop(); PreceptValue l = stack.Pop();
            stack.Push(executor(l, r));
            break;
        case Return: return stack.Pop();
    }
}
```

No recursive dispatch, no semantic reasoning at runtime. The plan encodes all decisions; the evaluator executes them.

### ConstraintInfluenceMap as First-Class Artifact

The dependency from constraints to contributing fields is a first-class runtime artifact. AI agents can query:

- "Which fields affect this constraint?" — `ConstraintToFields[constraint]`
- "Which constraints will change if I modify this field?" — `FieldToConstraints[field]`

This enables causal reasoning about constraint satisfaction without re-analyzing the constraint expressions.

### Single-Factory Construction Pattern

`Precept.From(Compilation)` is the sole factory. There is no other way to construct a `Precept`. This guarantees:

- Every `Precept` instance passed a `!HasErrors` compilation
- Every `Precept` has complete, consistent internal state
- No defensive programming required in consumers — a `Precept` is always valid

The last of those rests on both conditions the factory imposes, not on the `HasErrors` guard alone: the guard rules out a definition the compiler rejected, and the certificate gate rules out a definition the compiler never proved. Only the guard is built today (§ The Certificate Gate).

---

## 13. Open Questions / Implementation Notes

### Open Questions

An index of what this document leaves undecided. Each entry says what somebody has to decide and points at the section where the case for it is written; that discussion stays where it is, because it depends on the material around it.

- **What happens when the certificate gate refuses.** Whether a verification failure throws or returns a structured result, whether the failure names the derivation step that failed to replay, and whether verification is cached across loads are all undecided. What is settled is that the failure must explain itself. See § 9 The Certificate Gate, and § 10 Input Contract, which states the condition without the mechanism.
- **How the certificate reaches `Precept.From` at all.** The `Compilation` record does not carry one, and the proof record is described as emitted alongside the definition rather than inside the compiler's analysis output. Whether it becomes a sixth member of `Compilation`, a separate argument, or a file the method is pointed at is decided with the rest of the gate's shape. See § 9 The Certificate Gate.
- **Whether the `ConstraintPlanIndex` keeps its five-anchor shape.** Only two uses survive the removal of the per-operation constraint pass — assembling the constraint catalogs, and routing an incoming value to the constraints declared on the slot it enters — and both are anchored to a slot or to a catalog listing rather than to a state or an event. The index could shrink to what ingress needs, or be replaced by a premise list derived from the definition's certificate. The same question reaches Pass 4, the `ConstraintPlanIndex` entry in § 8 Downstream Consumers, and `catalog-system.md` § Evaluator-catalog integration pattern. See § 12 Constraint Indexes, and What They Index Now.
- **Which storage shape `FieldDescriptor.AccessModes` takes** — an immutable dictionary keyed by state, or an immutable array of state-and-mode pairs. Also written in `evaluator.md` § 7.5 Access Mode Enforcement. See § 7 Pass 1 — Descriptor Pass.
- **Whether `ConstructMeta` should carry `ModelContribution` metadata** so the assembly loop becomes fully generic, instead of the builder knowing directly that a field declaration adds a field and a transition row adds a transition. See § Pending Design Decisions below.
- **Whether the expression compiler stays a stack machine or moves to register allocation.** A stack machine is what this design specifies; the document leaves the move open against profiling evidence. See § Pending Design Decisions below.
- **Whether the `ExecutionPlan` opcode array stays a discriminated union or becomes a flat struct array with an enum discriminator.** Cache locality against allocation cost, pending profiling evidence. See § Pending Design Decisions below.
- **Whether a defect in Precept itself reaches the caller as a structured outcome or as a thrown exception.** This document is party to that question: § 9 Builder Invariant Violations states one of the two accounts that disagree, and it is not settled that the account is right. The question is filed in `fault-system.md` § Q3 and indexed in `evaluator.md` § 13 Open Questions. **The owner has parked it; his stated lean is exception, and a lean is not a ruling.**

### Implementation Status

1. **`Precept.From` stub:** Currently throws `NotImplementedException` after the `HasErrors` guard — transformation logic not implemented.

2. **Descriptor types:** `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor` shapes are defined in `docs/runtime/descriptor-types.md` but not yet implemented as C# types.

3. **Opcode types:** The `Opcode` DU and its subtypes are designed in this document but not yet implemented.

4. **Index types:** `SlotLayout`, `TransitionDispatchIndex`, `ReachabilityIndex`, `ConstraintPlanIndex`, `ConstraintInfluenceMap` are designed but not yet implemented.

### Resolved Questions

5. **Slot layout algorithm:** Field declaration order → slot index. Computed fields get slots. Declaration order provides predictable, debuggable slot assignment.

6. **ConstraintInfluenceMap ownership:** Built by the Precept Builder from `ProofLedger.ConstraintInfluenceEntries`. The proof engine computes the raw edges; the builder reshapes them into the query index.

7. **Stateless precept handling:** No `StateDescriptor` entries; `TransitionDispatchIndex` keyed on `(null, EventDescriptor)`; `IsStateless == true`.

### Pending Design Decisions

8. **`ModelContribution` metadata on `ConstructMeta`:** Should the builder expose this to make the assembly loop fully generic? Currently implicit — the builder "knows" that `FieldDeclaration` adds a field and `TransitionRow` adds a transition. This knowledge is trivially catalogable, but the value is marginal for ~12 constructs.

9. **Register-based vs. stack-based expression compilation:** Stack machines are simpler but register allocation can reduce push/pop overhead for performance-critical paths. Stack VM is the current choice; register allocation is a future optimization if profiling demands it.

10. **Opcode array shape:** The `ExecutionPlan` opcode array uses discriminated union subtypes (`LoadSlot`, `BinaryOp`, etc.). A flat struct array with an enum discriminator would improve cache locality. The DU is cleaner but has object allocation overhead per opcode. Decision pending profiling evidence.

---

## 14. Deliberate Exclusions

### No Semantic Re-Analysis

The builder consumes already-resolved semantic facts. It never re-reads catalogs for semantic classification — that was the type checker's job. The builder reads catalogs only for structural dispatch (`ConstraintMeta` DU subtype → bucket routing, `ActionMeta.SyntaxShape` → action plan shape).

### No Evaluation

Execution is the evaluator's domain. The builder produces execution plans; it does not execute them. Even default-value expressions are compiled to `ExecutionPlan`, not evaluated during build.

### No Incremental Rebuild

The builder runs once per `Compilation`, producing an immutable `Precept`. There is no incremental update path. A source change produces a new `Compilation`, which produces a new `Precept`. This simplifies reasoning about `Precept` identity — two `Precept` instances from different `Compilation` objects are never "compatible."

### No Optimization Passes

The execution plan compiler performs no optimization:

- No dead code elimination
- No constant folding
- No common subexpression elimination

The DSL's expression language is simple enough that these optimizations provide marginal benefit. The builder prioritizes correctness and simplicity over performance optimization.

### No Validation Beyond Structural Consistency

The builder does not re-validate that:

- Field types are legal
- Modifier combinations are valid
- Transition guards are boolean
- Action targets exist

These were validated by the type checker. The builder trusts the `SemanticIndex` to be semantically correct and focuses on structural transformation.

---

## 15. Cross-References

| Topic | Document |
|---|---|
| Precept executable model design | `docs/compiler-and-runtime-design.md §10` |
| SemanticIndex input shape | `docs/compiler/type-checker.md` |
| StateGraph input shape | `docs/compiler/graph-analyzer.md` |
| ProofLedger input (ConstraintInfluenceEntries) | `docs/compiler/proof-engine.md` |
| Evaluator that consumes the executable model | `docs/runtime/evaluator.md` |
| Descriptor type shapes | `docs/runtime/descriptor-types.md` |
| Catalog system architecture | `docs/language/catalog-system.md` |
| Constraint catalog and DU subtypes | `docs/language/catalog-system.md §Constraints` |

---

## 16. Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Precept.cs` | `Precept.From` factory + structural query surfaces (`Fields`, `States`, `Events`, `Constraints`) |
| `src/Precept/Runtime/PreceptBuilder.cs` | Builder implementation — six-pass transformation pipeline (planned) |
| `src/Precept/Runtime/Descriptors.cs` | `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor` (planned). The file also declares `FaultSiteDescriptor`, which no builder pass produces and which is on its way out |
| `src/Precept/Runtime/SlotLayout.cs` | `SlotLayout` register file specification (planned) |
| `src/Precept/Runtime/DispatchIndex.cs` | `TransitionDispatchIndex`, `ReachabilityIndex` (planned) |
| `src/Precept/Runtime/ConstraintPlanIndex.cs` | `ConstraintPlanIndex` with five anchor-family buckets (planned) |
| `src/Precept/Runtime/ConstraintInfluenceMap.cs` | Bidirectional constraint↔field dependency index (planned) |
| `src/Precept/Runtime/ExecutionPlan.cs` | `ExecutionPlan`, `Opcode` DU, `ExecutionRow`, `ActionPlan` (planned) |
| `src/Precept/Pipeline/SemanticIndex.cs` | `TypedField`, `TypedState`, `TypedEvent`, etc. — builder input |
| `src/Precept/Pipeline/StateGraph.cs` | `GraphState`, `GraphEdge`, etc. — builder input |
| `src/Precept/Pipeline/ProofLedger.cs` | `ConstraintInfluenceEntry` — builder input. The file also declares `FaultSiteLink` and `FaultSiteAnnotation`, and the `ProofLedger` record still carries a `FaultSiteLinks` field; no builder pass reads any of the three, and their removal has not been made |

