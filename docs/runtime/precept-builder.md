# Precept Builder

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Stub — error guard implemented; transformation logic not implemented; `Precept.From` throws `NotImplementedException` after `HasErrors` check |
| Source | `src/Precept/Runtime/Precept.cs` (`Precept.From` factory), `src/Precept/Runtime/PreceptBuilder.cs` (builder implementation — planned) |
| Upstream | `Compilation` (when `!HasErrors`) — carries `SemanticIndex`, `StateGraph`, `ProofLedger` |
| Downstream | All runtime operations (Create, Fire, Update, Restore, Inspect), LS preview, MCP runtime tools |

---

## 2. Overview

The Precept Builder is the **compile-to-runtime boundary** — the one-way transformation from semantic/analysis artifacts to dispatch-optimized execution structures. It transforms the `Compilation` object (carrying `SemanticIndex`, `StateGraph`, and `ProofLedger`) into the sealed `Precept` executable model.

**Pipeline Position:**

```text
Source Text → Lexer → Parser → Type Checker → Graph Analyzer → Proof Engine → [Precept Builder] → Evaluator
                                    ↓               ↓               ↓                ↓
                              SemanticIndex    StateGraph      ProofLedger        Precept
```

All three analysis artifacts arrive packaged as a `Compilation` object. `Precept.From(Compilation)` is the sole entry point and sole factory for the `Precept` executable model.

**Key design identity:** The builder restructures, not renames. The runtime model is organized for execution — constraint plans grouped by activation anchor, actions grouped by transition row, fields addressed by slot index. An implementer who copies `SemanticIndex` shapes 1:1 to the runtime model is violating this identity.

**Key distinction:** The builder does NOT parse, type-check, or analyze. Those responsibilities belong to upstream stages. The builder reads already-resolved semantic facts and builds execution structures optimized for the evaluator's access patterns. Once the `Precept` is built, the analysis artifacts are no longer needed; the runtime operates entirely from the built model.

The builder executes six sequential transformation passes:

1. **Descriptor pass** — build descriptor tables from typed declarations; assign slot indexes
2. **Slot layout pass** — produce the `SlotLayout` register file specification
3. **Dispatch index pass** — build `TransitionDispatchIndex` and `ReachabilityIndex` from topology
4. **Constraint plan pass** — sort constraints into activation-anchor buckets
5. **Execution plan pass** — compile typed expressions and actions into flat opcode arrays
6. **Fault backstop pass** — plant `FaultSiteDescriptor` entries from proof ledger links

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
| **Fault backstop planting** | Read `ProofLedger.FaultSiteLinks` and plant `FaultSiteDescriptor` entries at each site |
| **Constraint influence map construction** | Reshape `ProofLedger.ConstraintInfluenceEntries` into query-efficient `ConstraintInfluenceMap` |

### Out of Scope

| Exclusion | Rationale |
|---|---|
| **Lexing / parsing** | The lexer and parser produce the syntax tree. The builder consumes typed semantic facts, not tokens or parse nodes. |
| **Semantic analysis** | The type checker resolves names, types, and overloads. The builder reads resolved `TypedField`, `TypedState`, `TypedTransitionRow`, etc. |
| **Graph analysis** | The graph analyzer computes topology, reachability, and dominance. The builder reads the `StateGraph` output. |
| **Proof discharge** | The proof engine generates and discharges obligations. The builder reads `ProofLedger` results. |
| **Operation execution** | The evaluator walks execution plans and evaluates constraints. The builder produces plans, not outcomes. |
| **Incremental rebuild** | The builder runs once per `Compilation`. No incremental update path exists — a source change produces a new `Compilation`, which produces a new `Precept`. |

---

## 4. Right-Sizing

The Precept Builder is scoped precisely as the transformation boundary — it is the only code path that builds the runtime model. `Precept.From` is the sole factory.

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 600–900 | ~200 descriptor construction + ~100 slot layout + ~150 dispatch index + ~150 constraint plan + ~150 execution plan compiler + ~50 fault backstop |
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
    SemanticIndex SemanticIndex,
    StateGraph StateGraph,
    ProofLedger ProofLedger,
    ImmutableArray<Diagnostic> Diagnostics
)
{
    public bool HasErrors => Diagnostics.Any(d => d.Severity == Severity.Error);
}
```

> **Open Question (unresolved):** Should `Compilation` carry a `Tokens` field for the language server's lexical semantic token generation? Both language-server.md and tooling-surface.md reference this for Pass 1 tokens.

The builder reads all three analysis artifacts:

| Artifact | What it provides |
|---|---|
| `SemanticIndex` | Typed declarations: `TypedField`, `TypedState`, `TypedEvent`, `TypedArg`, `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, `TypedAction`, `TypedExpression` |
| `StateGraph` | Topology: `GraphState`, `GraphEdge`, `ReachableStates`, `EventCoverage` |
| `ProofLedger` | Proof results: `FaultSiteLinks`, `ConstraintInfluenceEntries` |

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
    internal ImmutableArray<FaultSiteDescriptor> FaultBackstops { get; }
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
│  Pass 5: Execution Plan Pass (runs before Pass 4)                    │
│  • Compile TypedExpression trees → flat ExecutionPlan opcode arrays  │
│  • Compile TypedAction chains → ActionPlan arrays                    │
│  • Build ExecutionRow from TypedTransitionRow                        │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 4: Constraint Plan Pass (runs after Pass 5)                    │
│  • Build ConstraintDescriptor with compiled ExecutionPlan            │
│  • Route into anchor buckets: always, in<S>, from<S>, to<S>, on<E>   │
│  • Build ConstraintPlanIndex                                         │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 6: Fault Backstop Pass                                         │
│  • Read FaultSiteLinks from ProofLedger                              │
│  • Plant FaultSiteDescriptor at each site                            │
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
| Pass 5 (Execution Plan) | Pass 1 (slot indexes) | `ExecutionPlan`, `ActionPlan`, `ExecutionRow` |
| Pass 4 (Constraint Plan) | Pass 5 (compiled expressions) | `ConstraintPlanIndex` |
| Pass 6 (Fault Backstop) | Pass 4, `ProofLedger` | `FaultSiteDescriptor[]`, `ConstraintInfluenceMap` |

**Valid execution order:** 1 → 2 (parallel with 3) → 5 → 4 → 6 → assemble `Precept`

Note: Pass 5 runs before Pass 4 because `ConstraintDescriptor` requires a compiled `ExecutionPlan` for its expression. The pass numbers reflect logical grouping, not execution order.

---

## 7. Component Mechanics

### Pass 1 — Descriptor Pass

Build descriptor tables from `SemanticIndex` typed declarations:

**Field Descriptors:**

The input `TypedField` shape is defined canonically in `type-checker.md` §7.1. Key fields for the builder:

```csharp
// Output: FieldDescriptor with assigned SlotIndex
public sealed record FieldDescriptor(
    string Name,
    TypeKind Type,
    int SlotIndex,                              // assigned sequentially in declaration order
    ImmutableArray<ModifierKind> Modifiers,
    bool IsComputed,
    bool IsOptional,
    SourceSpan Source
);
```

> **Open Question (unresolved):** `FieldDescriptor` needs an `AccessModes` property (per evaluator.md §7.5). What is the exact shape? `ImmutableDictionary<StateDescriptor?, AccessMode>` or `ImmutableArray<(StateDescriptor?, AccessMode)>`? Which pass builds it?

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
    bool IsOptional
);
```

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

The slot layout is the evaluator's register file specification. Every entity instance (`Version`) is a `object?[]` array of size `FieldCount`. The evaluator uses slot indexes, never field names, during execution.

**Computed slots:** Fields with `IsComputed == true` are collected into the `ComputedSlots` array. After every state transition, the evaluator walks this array and recalculates each computed field's value before constraint evaluation.

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

### Pass 5 — Execution Plan Pass

Compile typed action chains and typed guard/constraint expressions from the `SemanticIndex` into flat opcode arrays.

**Opcode Inventory:**

An `ExecutionPlan` is a flat array of `Opcode` values that the evaluator walks left to right. Opcodes operate on a stack (push/pop) and reference fields by slot index.

| Opcode | Stack Effect | Description |
|---|---|---|
| `LOAD_SLOT(i)` | push | Load field value at slot index `i` |
| `LOAD_ARG(name)` | push | Load event arg value by name |
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
    TypeKind ResultType
);

public abstract record Opcode;
public sealed record LoadSlot(int SlotIndex) : Opcode;
public sealed record LoadArg(string ArgName) : Opcode;
public sealed record LoadLit(object? Value) : Opcode;
public sealed record StoreSlot(int SlotIndex) : Opcode;
public sealed record BinaryOp(OperationKind Kind) : Opcode;
public sealed record UnaryOp(OperationKind Kind) : Opcode;
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

**Catalog-driven compilation:** The expression compiler reads `Operations.GetMeta(kind)`, `Functions.GetMeta(kind)`, and `Types.GetAccessor(type, name)` to emit the correct opcodes. It does not hardcode per-operation behavior — it dispatches on catalog entries.

### Pass 4 — Constraint Plan Pass

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
    ExecutionPlan Expression,          // compiled expression (from Pass 5)
    string? BecauseClause,
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

### Pass 6 — Fault Backstop Pass

For each `FaultSiteLink` in `ProofLedger.FaultSiteLinks`, plant a `FaultSiteDescriptor` in the relevant `ExecutionRow` or `ConstraintDescriptor` at the site identified by the link.

```csharp
public sealed record FaultSiteDescriptor(
    FaultCode Code,
    DiagnosticCode PreventedBy,         // from [StaticallyPreventable] attribute
    SourceSpan Site
);
```

> **Open Question (unresolved):** What is the actual planting mechanism for `FaultSiteDescriptor`? Neither `ExecutionRow` nor `ConstraintDescriptor` carries a fault site field, and `Precept.FaultBackstops` is a flat array. How does the evaluator know which opcode or row constitutes a fault site?

Fault backstops are defense-in-depth. A correctly-proven program never reaches them at runtime. When reached, they fire the `FaultCode`'s runtime behavior (log + graceful failure) via `Faults.Create(descriptor, context)`.

**ConstraintInfluenceMap:**

Also during this pass, build the `ConstraintInfluenceMap` from `ProofLedger.ConstraintInfluence`. This is a direct reshaping of `ConstraintInfluenceEntry` records into a query-friendly map:

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
| **Proof Engine** | `ProofLedger` | `FaultSiteLinks`, `ConstraintInfluenceEntries` |

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
| **Evaluator** | `SlotLayout`, `TransitionDispatchIndex`, `ConstraintPlanIndex`, `ExecutionRow`, `ExecutionPlan`, `FaultBackstops` |
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

### Builder Invariant Violations

If the builder detects internal invariant violations (e.g., a `TypedTransitionRow` references a state that doesn't exist in `SemanticIndex.States`), it throws `InvalidOperationException` with diagnostic context. These indicate bugs in upstream stages — the builder assumes the `Compilation` is internally consistent.

### No Partial Builds

There is no partial `Precept`. The builder either produces a complete, valid executable model or throws. This is a deliberate design choice:

- A partial model would require every consumer to check "is this constraint index populated?" before use
- The evaluator would need null-checks on every index access
- Error handling would be scattered across the runtime instead of concentrated at the build boundary

### Recovery Path

If `Precept.From` throws:

1. Examine the `Compilation.Diagnostics` for errors
2. Fix the source causing the errors
3. Recompile to produce a new `Compilation`
4. Retry `Precept.From`

---

## 10. Contracts and Guarantees

### Input Contract

- `Precept.From(compilation)` requires `!compilation.HasErrors`
- The `Compilation` must be internally consistent (no dangling references, no duplicate names)
- The `SemanticIndex`, `StateGraph`, and `ProofLedger` must all be present and non-null

### Output Contract

- `Precept.From(compilation)` returns a complete, immutable `Precept` or throws
- The returned `Precept` is thread-safe and shareable across all entity instances
- Every `FieldDescriptor` has a unique `SlotIndex` within `[0, FieldCount)`
- Every `StateDescriptor` has a unique `Name` within the precept
- Every `EventDescriptor` has a unique `Name` within the precept
- Every `TransitionDispatchIndex` entry corresponds to a `TypedTransitionRow` in `SemanticIndex`
- Every `ConstraintDescriptor` in `ConstraintPlanIndex` corresponds to a `TypedRule` or `TypedEnsure` in `SemanticIndex`
- Every `FaultSiteDescriptor` has a `FaultCode` with a `[StaticallyPreventable(DiagnosticCode)]` attribute

### Slot Index Invariants

- Slot indexes are assigned sequentially in field declaration order
- `SlotLayout.FieldCount == Fields.Count`
- For all `FieldDescriptor f`: `0 <= f.SlotIndex < SlotLayout.FieldCount`
- `SlotLayout.Fields[i].SlotIndex == i` (slot-order array)

### Constraint Plan Invariants

- Every `ConstraintDescriptor` appears in exactly one bucket
- `Always` bucket contains only `ConstraintKind.Invariant` constraints
- `InState` buckets contain only `ConstraintKind.StateResident` constraints
- `FromState` buckets contain only `ConstraintKind.StateExit` constraints
- `ToState` buckets contain only `ConstraintKind.StateEntry` constraints
- `OnEvent` buckets contain only `ConstraintKind.EventPrecondition` constraints

> **Open Question (unresolved):** The constraint bucket dispatch uses five concrete subtypes (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`), but catalog-system.md shows only four subtypes in the `ConstraintMeta` DU hierarchy. Are `StateEntry` and `StateExit` subtypes of `StateAnchored`, or separate top-level subtypes?

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

**Rationale:** Eliminates O(constraints) scan per operation. The evaluator reads the pre-built bucket — O(1) lookup, O(bucket size) evaluation. For a typical precept with 20 constraints across 5 states, this reduces per-operation overhead significantly.

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

### Dispatch-Optimized Constraint Indexes

Traditional constraint systems evaluate all constraints on every operation. Precept pre-sorts constraints into activation-anchor buckets:

- `always` — evaluated on every operation
- `in <State>` — evaluated only when in that state
- `from <State>` — evaluated only when leaving that state
- `to <State>` — evaluated only when entering that state
- `on <Event>` — evaluated only when firing that event

The evaluator never scans or filters — it reads the pre-built bucket for its context. For a precept with 50 constraints, a typical Fire operation might evaluate only 5–10 (the `always` bucket plus the relevant state/event buckets).

### Flat Evaluation Plans

Expressions are precomputed into flat, cache-friendly slot-addressed opcodes. The evaluator is a simple stack machine:

```csharp
var stack = new Stack<object?>();
foreach (var op in plan.Opcodes)
{
    switch (op)
    {
        case LoadSlot(var i): stack.Push(slots[i]); break;
        case LoadLit(var v): stack.Push(v); break;
        case BinaryOp(var kind): 
            var r = stack.Pop(); var l = stack.Pop();
            stack.Push(Execute(kind, l, r));
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

---

## 13. Open Questions / Implementation Notes

### Implementation Status

1. **`Precept.From` stub:** Currently throws `NotImplementedException` after the `HasErrors` guard — transformation logic not implemented.

2. **Descriptor types:** `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `FaultSiteDescriptor` shapes are defined in `docs/runtime/descriptor-types.md` but not yet implemented as C# types.

3. **Opcode types:** The `Opcode` DU and its subtypes are designed in this document but not yet implemented.

4. **Index types:** `SlotLayout`, `TransitionDispatchIndex`, `ReachabilityIndex`, `ConstraintPlanIndex`, `ConstraintInfluenceMap` are designed but not yet implemented.

### Resolved Questions

5. **Slot layout algorithm:** Field declaration order → slot index. Computed fields get slots. This is resolved — declaration order provides predictable, debuggable slot assignment.

6. **ConstraintInfluenceMap ownership:** Built by the Precept Builder from `ProofLedger.ConstraintInfluenceEntries`. The proof engine computes the raw edges; the builder reshapes them into the query index.

7. **Stateless precept handling:** No `StateDescriptor` entries; `TransitionDispatchIndex` keyed on `(null, EventDescriptor)`; `IsStateless == true`.

### Pending Design Decisions

> **Open Question (unresolved):** Should the builder expose `ModelContribution` metadata on `ConstructMeta` to make the assembly loop fully generic? Currently implicit — the builder "knows" that `FieldDeclaration` adds a field and `TransitionRow` adds a transition. This knowledge is trivially catalogable, but the value is marginal for ~12 constructs.

> **Open Question (unresolved):** Expression compilation currently targets a stack-based VM. Should register-based allocation be considered for performance-critical paths? Stack machines are simpler but register allocation can reduce push/pop overhead.

> **Open Question (unresolved):** The `ExecutionPlan` opcode array uses discriminated union subtypes (`LoadSlot`, `BinaryOp`, etc.). Should this be a flat struct array with an enum discriminator for better cache locality? The DU is cleaner but has object allocation overhead.

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
| ProofLedger input (FaultSiteLinks, ConstraintInfluenceEntries) | `docs/compiler/proof-engine.md` |
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
| `src/Precept/Runtime/Descriptors.cs` | `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `FaultSiteDescriptor` (planned) |
| `src/Precept/Runtime/SlotLayout.cs` | `SlotLayout` register file specification (planned) |
| `src/Precept/Runtime/DispatchIndex.cs` | `TransitionDispatchIndex`, `ReachabilityIndex` (planned) |
| `src/Precept/Runtime/ConstraintPlanIndex.cs` | `ConstraintPlanIndex` with five anchor-family buckets (planned) |
| `src/Precept/Runtime/ConstraintInfluenceMap.cs` | Bidirectional constraint↔field dependency index (planned) |
| `src/Precept/Runtime/ExecutionPlan.cs` | `ExecutionPlan`, `Opcode` DU, `ExecutionRow`, `ActionPlan` (planned) |
| `src/Precept/Pipeline/SemanticIndex.cs` | `TypedField`, `TypedState`, `TypedEvent`, etc. — builder input |
| `src/Precept/Pipeline/StateGraph.cs` | `GraphState`, `GraphEdge`, etc. — builder input |
| `src/Precept/Pipeline/ProofLedger.cs` | `FaultSiteLink`, `ConstraintInfluenceEntry` — builder input |

