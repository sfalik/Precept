# Graph Analyzer

> **Status:** Implemented.

## Contents

- [1. Overview](#1-overview)
- [2. Responsibilities and Boundaries](#2-responsibilities-and-boundaries)
  - [In Scope](#in-scope)
  - [Out of Scope](#out-of-scope)
- [3. Right-Sizing](#3-right-sizing)
- [4. Inputs and Outputs](#4-inputs-and-outputs)
  - [Input: `SemanticIndex`](#input-semanticindex)
  - [Output: `StateGraph`](#output-stategraph)
- [4a. Structural Preconditions](#4a-structural-preconditions)
- [5. Architecture](#5-architecture)
  - [5.1 Four-Phase Analysis Pipeline](#51-four-phase-analysis-pipeline)
  - [5.2 Catalog-Driven Modifier Dispatch](#52-catalog-driven-modifier-dispatch)
- [6. Component Mechanics](#6-component-mechanics)
  - [6.1 Edge Construction (Phase 1)](#61-edge-construction-phase-1)
  - [6.2 Reachability Analysis (Phase 2)](#62-reachability-analysis-phase-2)
  - [6.3 Dominator Computation (Phase 3)](#63-dominator-computation-phase-3)
  - [6.4 Structural Constraint Checks (Phase 3)](#64-structural-constraint-checks-phase-3)
  - [6.5 Event Coverage Analysis (Phase 3)](#65-event-coverage-analysis-phase-3)
  - [6.6 Proof Forwarding (Phase 4)](#66-proof-forwarding-phase-4)
- [7. Dependencies and Integration Points](#7-dependencies-and-integration-points)
  - [Upstream Dependencies](#upstream-dependencies)
  - [Downstream Consumers](#downstream-consumers)
  - [Catalog Integration](#catalog-integration)
- [8. Failure Modes and Recovery](#8-failure-modes-and-recovery)
  - [8.1 Upstream Failures](#81-upstream-failures)
  - [8.2 Structural Violations](#82-structural-violations)
  - [8.3 Invariants](#83-invariants)
- [9. Contracts and Guarantees](#9-contracts-and-guarantees)
  - [Preconditions](#preconditions)
  - [Postconditions](#postconditions)
  - [Invariants](#invariants)
- [10. Design Rationale and Decisions](#10-design-rationale-and-decisions)
  - [10.1 Catalog-Driven Modifier Semantics](#101-catalog-driven-modifier-semantics)
  - [10.2 Four-Phase Architecture](#102-four-phase-architecture)
  - [10.3 Simple Dominator Algorithm](#103-simple-dominator-algorithm)
  - [10.4 Proof Forwarding as Typed Facts](#104-proof-forwarding-as-typed-facts)
  - [10.5 Non-Short-Circuiting Analysis](#105-non-short-circuiting-analysis)
- [11. Innovation](#11-innovation)
  - [11.1 Catalog-Driven Modifier Applicability](#111-catalog-driven-modifier-applicability)
  - [11.2 Proof Forwarding Pipeline](#112-proof-forwarding-pipeline)
- [12. Open Questions / Implementation Notes](#12-open-questions--implementation-notes)
  - [Implementation Notes](#implementation-notes)
  - [Implementation Notes](#implementation-notes)
- [13. Deliberate Exclusions](#13-deliberate-exclusions)
- [14. Cross-References](#14-cross-references)
  - [Related Documentation](#related-documentation)
  - [Related Catalogs](#related-catalogs)
- [15. Source Files](#15-source-files)
- [Appendix: Diagnostic Codes](#appendix-diagnostic-codes)

## 1. Overview

The graph analyzer is the fourth pipeline stage, transforming the type checker's `SemanticIndex` into a `StateGraph` containing topology models and structural analysis facts. This stage produces no runtime values — only structural analysis results that inform subsequent proof obligations and diagnostics.

**Pipeline Position:**

```text
Source Text → Lexer → Parser → Name Binder → Type Checker → [Graph Analyzer] → Proof Engine → Runtime
                                   ↓                  ↓
                            SemanticIndex       StateGraph
```

The graph analyzer operates on fully resolved, well-typed declarations. It constructs the state-transition graph topology, partitions states by reachability, enforces catalog-driven modifier semantics (terminal, required, irreversible), and prepares proof-forwarding facts for downstream stages.

---

## 2. Responsibilities and Boundaries

### In Scope

| Responsibility | Description |
|----------------|-------------|
| **Edge construction** | Build `GraphEdge` entries from `TypedTransitionRow` declarations, resolving any-state wildcards to explicit source states |
| **Reachability analysis** | Partition states into reachable and unreachable sets via BFS from the initial state |
| **Modifier enforcement** | Apply catalog-driven structural constraints: terminal states must have no outgoing edges, irreversible states must have no back-edges, required states must dominate some terminal |
| **Dominance computation** | Compute dominator relationships to verify required-state constraints |
| **Event coverage analysis** | Determine which events are handled in each state, identifying dead events |
| **Proof forwarding** | Emit `ProofForwardingFact` entries that guide the proof engine's obligation generation |
| **Diagnostic emission** | Report `UnreachableState`, `UnhandledEvent`, and structural violations as diagnostics |

### Out of Scope

| Exclusion | Rationale |
|-----------|-----------|
| **Expression evaluation** | The graph analyzer operates on topology, not values. Expression semantics belong to the proof engine and runtime evaluator. |
| **Name resolution** | Names are already resolved by the type checker. The graph analyzer consumes `TypedState`, `TypedEvent`, and `TypedTransitionRow` entries with resolved references. |
| **Guard analysis** | Guard expressions are opaque to the graph analyzer. The proof engine handles guard satisfiability. |
| **Action effects** | Actions mutate state data at runtime. The graph analyzer sees only the structural transition topology. |
| **Runtime execution** | No event firing, no state mutation, no data binding. |

---

## 3. Right-Sizing

The graph analyzer is intentionally minimal. It produces exactly the structural facts that downstream stages require — no more.

**Why not merge into the type checker?**
The type checker validates declaration-level semantics (name binding, type compatibility, modifier applicability). Graph analysis requires the complete, resolved declaration set to reason about global properties like reachability and dominance. Separating these concerns keeps each stage focused and testable.

**Why not merge into the proof engine?**
The proof engine generates and solves proof obligations for runtime correctness. It consumes structural facts (reachability, dominance) as inputs. Bundling topology analysis into proof generation would conflate static graph properties with dynamic proof obligations.

**Bounded complexity:**
Precept targets small state machines (3–15 states typical, bounded at 50). At this scale, straightforward algorithms (BFS for reachability, iterative dominator computation) suffice. The graph analyzer does not need asymptotically optimal algorithms designed for thousands of nodes.

---

## 4. Inputs and Outputs

### Input: `SemanticIndex`

The `SemanticIndex` is the type checker's output — a fully resolved, well-typed representation of the precept definition. The canonical shape is defined in `type-checker.md` §7.1.

**Graph-relevant fields:**

| Field | Type | Purpose |
|-------|------|---------|
| `States` | `ImmutableArray<TypedState>` | State names and modifiers — nodes in the graph |
| `TransitionRows` | `ImmutableArray<TypedTransitionRow>` | Edge definitions — source state × event × target state × guard × outcome |
| `Events` | `ImmutableArray<TypedEvent>` | Event declarations — needed for state coverage analysis (§6.5) and transition completeness: which events exist vs. which have transition rows |
| `EventHandlers` | `ImmutableArray<TypedEventHandler>` | Event handler declarations — **not consumed for event coverage analysis.** Event handlers are valid only in stateless precepts (the type checker emits `EventHandlerInStatefulPrecept` if states coexist with handlers). Since the graph analyzer operates on stateful precepts, no valid `TypedEventHandler` entries exist when graph analysis runs. This field is listed for completeness; the graph analyzer does not read it. See §12 (OQ2 resolution). |

> **Note:** The following `SemanticIndex` fields are not read by the graph analyzer but pass through to the proof engine via the `SemanticIndex` reference: `Fields`, `StateHooks`.

The graph analyzer reads the topology-relevant and proof-relevant subsets of `SemanticIndex`. It does not consume `Rules`, `Ensures`, `AccessModes`, or `EditDeclarations` — those belong to the proof engine and runtime builder.

### Output: `StateGraph`

The `StateGraph` encapsulates the topology model, reachability partition, structural analysis facts, proof-forwarding facts, and diagnostics.

```csharp
public sealed record StateGraph(
    // Core topology
    ImmutableArray<GraphState> States,
    ImmutableArray<GraphEvent> Events,
    ImmutableArray<GraphEdge> Edges,
    
    // Reachability partition
    ImmutableHashSet<string> ReachableStates,
    ImmutableHashSet<string> UnreachableStates,
    
    // Structural analysis facts
    ImmutableArray<DominanceFact> Dominance,
    ImmutableArray<TerminalOutgoingViolation> TerminalViolations,
    ImmutableArray<IrreversibleBackEdgeViolation> BackEdgeViolations,
    ImmutableArray<EventCoverageEntry> EventCoverage,
    
    // Proof forwarding
    ImmutableArray<ProofForwardingFact> ProofFacts,
    
    // Diagnostics
    ImmutableArray<Diagnostic> Diagnostics
);
```

**Core Topology Types:**

```csharp
public sealed record GraphState(
    string Name,
    bool IsInitial,
    bool IsTerminal,
    bool IsRequired,
    bool IsIrreversible,
    bool IsReachable
);
```

> **✅ Resolved (CC#10) — GraphState modifier representation**
> Flat boolean flags are correct. `GraphState` is a structural analysis *output* — a record of derived conclusions, not a source model. The booleans are the analyzer's answers to structural questions derived catalog-driven from `StateModifierMeta` fields at construction time (see §5.2). `IsReachable` is topological, not modifier-derived — confirming this is a "derived facts record". `SemanticIndex.TypedState.Modifiers` already carries the raw modifier list for consumers that need it; duplicating it on `GraphState` would be redundant. Shape is final.
> *Resolved: 2026-05-06 — CC#10*

```csharp

public sealed record GraphEvent(
    string Name,
    bool IsInitial,
    ImmutableArray<string> HandledInStates  // states where this event has a transition
);

public sealed record GraphEdge(
    string FromState,
    string EventName,
    string ToState,       // resolved: self-transitions become explicit
    bool HasGuard,
    TransitionRowOutcome Outcome
);
```

**Structural Analysis Types:**

```csharp
public sealed record DominanceFact(
    string Dominator,
    string Dominated,
    int Distance          // path length in dominator tree
);

public sealed record TerminalOutgoingViolation(
    string StateName,
    ImmutableArray<GraphEdge> OutgoingEdges
);

public sealed record IrreversibleBackEdgeViolation(
    string StateName,
    GraphEdge BackEdge
);

public sealed record EventCoverageEntry(
    string EventName,
    ImmutableArray<string> HandlingStates,
    ImmutableArray<string> NonHandlingReachableStates
);
```

**Proof Forwarding Types:**

```csharp
public abstract record ProofForwardingFact;

public sealed record ReachabilityFact(
    string StateName,
    bool IsReachable,
    ImmutableArray<string>? PathFromInitial  // null if unreachable
) : ProofForwardingFact;

public sealed record DominancePathFact(
    string RequiredState,
    ImmutableArray<string> DominatedTerminals
) : ProofForwardingFact;

public sealed record EventCoverageFact(
    string EventName,
    ImmutableArray<string> UnhandledReachableStates
) : ProofForwardingFact;

public sealed record TerminalCompletenessFact(
    bool AllTerminalsReachable,
    ImmutableArray<string> UnreachableTerminals
) : ProofForwardingFact;

public sealed record DeadEndStateFact(
    ImmutableArray<string> DeadEndStates,
    int DeadEndCount
) : ProofForwardingFact;
```

#### Proof Forwarding Contract

The `ProofForwardingFact` DU defines the interface between the graph analyzer and the proof engine. Each subtype carries the structural data that the proof engine needs to instantiate proof obligations from graph-level analysis. The proof engine pattern-matches on these subtypes in Pass 1 (Obligation Instantiation) — see `docs/compiler/proof-engine.md §6` for the consumption contract.

> **Implemented.** The `ProofForwardingFact` hierarchy now lives in `src/Precept/Pipeline/StateGraph.cs` alongside the `StateGraph` topology records. The subtype set below is the current contract consumed by the proof engine.

**Fact types, data, and proof engine consumption:**

| Fact Subtype | Data Carried | Proof Engine Consumption |
|---|---|---|
| `ReachabilityFact` | `StateName`, `IsReachable`, `PathFromInitial` (path from initial state, or null if unreachable) | The proof engine uses unreachable-state facts to suppress proof obligations on transitions originating from unreachable states — those transitions can never fire, so their proof requirements are vacuously satisfied. Reachable paths feed causal diagnostic explanations. |
| `DominancePathFact` | `RequiredState`, `DominatedTerminals` (which terminal states this required state dominates) | The proof engine verifies the required-state guarantee: every execution path to a terminal passes through the required state. If `DominatedTerminals` is empty, the graph analyzer has already emitted `RequiredStateDoesNotDominateTerminal` (code 111); the proof engine records this as a structural violation in the proof ledger. |
| `EventCoverageFact` | `EventName`, `UnhandledReachableStates` (reachable states that have no transition row for this event) | The proof engine uses coverage gaps to reason about guard completeness: in states where an event is handled, are the guards sufficient to cover all possible data states? Coverage gaps are structural facts; guard satisfiability is the proof engine's domain. |
| `TerminalCompletenessFact` | `AllTerminalsReachable`, `UnreachableTerminals` (terminal states that cannot be reached from the initial state) | True when at least one terminal state exists and all terminal states are reachable from the initial state; false otherwise (including when no terminal states are declared). The proof engine uses terminal completeness to assess whether the state machine can reach completion. Unreachable terminals may indicate dead-end design or missing transitions — the proof engine records these for the proof ledger without emitting additional diagnostics (the graph analyzer already emits `UnreachableState` for each one). |
| `DeadEndStateFact` | `DeadEndStates` (reachable non-terminal states with no path to any terminal state), `DeadEndCount` | The proof engine uses dead-end facts to annotate the proof ledger with structural completeness failures. Dead-end states represent lifecycle traps — entities that enter these states can never reach completion. The graph analyzer emits `DeadEndState` (Warning, code 108) for each dead-end state. The proof engine records these facts without emitting additional diagnostics. |

**Emission rules:**

- One `ReachabilityFact` per state (reachable and unreachable).
- One `DominancePathFact` per state with `RequiresDominator = true` (from `StateModifierMeta`).
- One `EventCoverageFact` per event that has at least one `UnhandledReachableState` (events with full coverage are not forwarded — no obligation needed).
- Exactly one `TerminalCompletenessFact` per analysis run.
- Exactly one `DeadEndStateFact` per analysis run (even if `DeadEndStates` is empty — the fact records the analysis was performed).

---

## 4a. Structural Preconditions

The graph analyzer assumes the following invariants about its inputs, all established and enforced by upstream stages. These are assert-level assumptions — the graph analyzer does not re-validate them.

| Precondition | Enforced By | Rationale |
|---|---|---|
| **Exactly one state has `IsInitial = true`** (for stateful precepts) | TypeChecker — emits `MultipleInitialStates` / `NoInitialState` diagnostics | BFS traversal requires a single entry point. The graph analyzer checks for the missing-initial-state case (§8.1) as a defensive fallback, but the TypeChecker is the primary enforcer. |
| **All modifier references are fully resolved** — each `TypedState.Modifiers` array contains valid `ModifierKind` values with populated `ModifierMeta` entries in the `Modifiers` catalog | TypeChecker — resolves modifiers during symbol population (Slice 1) | The graph analyzer calls `Modifiers.GetMeta(kind)` without null-checking the result. Unresolved modifiers would cause a catalog lookup failure. |
| **All state and event names referenced in `TypedTransitionRow` resolve to declared states/events** — `FromState` (when non-null) exists in `SemanticIndex.StatesByName`, and `EventName` exists in `SemanticIndex.EventsByName` | TypeChecker — emits `UndeclaredState` / `UndeclaredEvent` diagnostics via NameBinder resolution | Edge construction (§6.1) uses state/event names as dictionary keys. Unresolved references would produce edges to nonexistent nodes. |
| **All expression nodes in transitions and rules are `TypedExpression` subtypes — no `MissingExpression` nodes propagate to this stage** | TypeChecker — converts `MissingExpression` to `TypedErrorExpression` with a diagnostic (B3 fix) | The graph analyzer does not evaluate expressions (guards are opaque), but `TypedTransitionRow.Guard` and `TypedTransitionRow.Outcome` carry `TypedExpression` nodes. `MissingExpression` would indicate an incomplete parse that the TypeChecker should have caught. |
| **`TypedTransitionRow.Outcome` is never `MalformedOutcome`** | TypeChecker — emits a diagnostic and does not produce a `TypedTransitionRow` for malformed outcomes | Edge construction assumes every transition row has a well-formed outcome with a resolvable target state (or null for self-transition). |
| **`SemanticIndex` collections are non-null** — all `ImmutableArray<T>` fields are initialized (possibly empty, never default/null) | TypeChecker — `SemanticIndex.Empty` factory initializes all fields; `CheckContext` accumulator produces non-null arrays | The graph analyzer iterates all input collections without null guards. |

---

## 5. Architecture

### 5.1 Four-Phase Analysis Pipeline

The graph analyzer executes four sequential phases, each building on prior results:

```text
┌─────────────────────────────────────────────────────────────────────┐
│                         SemanticIndex                                │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Phase 1: Edge Construction                                          │
│  • Expand any-state wildcards to explicit edges                      │
│  • Resolve self-transitions to explicit FromState == ToState         │
│  • Build adjacency structures                                        │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Phase 2: Reachability Analysis                                      │
│  • BFS from initial state                                            │
│  • Partition into ReachableStates / UnreachableStates                │
│  • Reverse-reachability from terminal states → detect dead-end states│
│  • Emit UnreachableState diagnostics                                 │
│  • Emit DeadEndState diagnostics                                     │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Phase 3: Structural Analysis                                        │
│  • Compute dominator tree (iterative algorithm)                      │
│  • Check terminal modifier constraints (no outgoing edges)           │
│  • Check irreversible modifier constraints (no back-edges)           │
│  • Check required modifier constraints (must dominate terminal)      │
│  • Compute event coverage per state                                  │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Phase 4: Proof Forwarding                                           │
│  • Emit ReachabilityFact for each state                              │
│  • Emit DominancePathFact for required states                        │
│  • Emit EventCoverageFact for events with gaps                       │
│  • Emit TerminalCompletenessFact                                     │
│  • Emit DeadEndStateFact                                             │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           StateGraph                                 │
└─────────────────────────────────────────────────────────────────────┘
```

### 5.2 Catalog-Driven Modifier Dispatch

State modifier semantics are defined in the `Modifiers` catalog, not hardcoded in the analyzer. The `StateModifierMeta` record carries the structural constraint flags:

```csharp
// From src/Precept/Language/Modifier.cs
public sealed record StateModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    bool AllowsOutgoing = true,      // terminal: false
    bool RequiresDominator = false,  // required: true
    bool PreventsBackEdge = false,   // irreversible: true
    ModifierKind[]? MutuallyExclusiveWith = null
) : ModifierMeta(Kind, Token, Description, Category);
```

**Dispatch pattern:**

```csharp
foreach (var state in index.States)
{
    foreach (var modifierKind in state.Modifiers)
    {
        var meta = Modifiers.GetMeta(modifierKind);
        if (meta is StateModifierMeta stateMeta)
        {
            if (!stateMeta.AllowsOutgoing)
                CheckNoOutgoingEdges(state, edges);
            
            if (stateMeta.PreventsBackEdge)
                CheckNoBackEdges(state, edges, reachabilityOrder);
            
            if (stateMeta.RequiresDominator)
                CheckDominatesTerminal(state, dominatorTree);
        }
    }
}
```

This pattern ensures new modifier constraints can be added to the catalog without modifying analyzer code.

---

## 6. Component Mechanics

### 6.1 Edge Construction (Phase 1)

**Input:** `ImmutableArray<TypedTransitionRow>`

**Processing:**

1. **Wildcard expansion:** For rows where `FromState` is null (any-state wildcard), generate one edge per declared state that does not have an explicit transition for that event. States are processed in declaration order for deterministic edge ordering — see §12 Implementation Note 1.

2. **Self-transition resolution:** For rows where `TargetState` is null, set `ToState = FromState`.

3. **Adjacency map:** Build `Dictionary<string, List<GraphEdge>>` for O(1) edge lookup by source state.

**Output:** `ImmutableArray<GraphEdge>`, adjacency structures.

### 6.2 Reachability Analysis (Phase 2)

**Algorithm:** Breadth-first search from the initial state.

```text
Input: adjacency map, initial state name
Output: (ReachableStates, UnreachableStates, pathMap)

1. Initialize queue with initial state
2. Initialize visited = { initial }
3. Initialize pathMap[initial] = []
4. While queue not empty:
   a. Dequeue current
   b. For each edge from current:
      i.  If edge.ToState not in visited:
          - Add to visited
          - Enqueue edge.ToState
          - pathMap[edge.ToState] = pathMap[current] + [current]
5. ReachableStates = visited
6. UnreachableStates = AllStates - visited
7. Emit Diagnostic(UnreachableState) for each unreachable state
```

**Dead-end detection (reverse-reachability from terminals):**

After forward reachability, compute reverse-reachability from all terminal states to identify dead-end states — reachable non-terminal states that have no structural path to any terminal.

```text
Input: adjacency map (reversed), ReachableStates, TerminalStates
Output: DeadEndStates

1. Build reverse adjacency map (edge.ToState → edge.FromState)
2. Initialize reverseVisited = {}
3. For each terminal state t in TerminalStates:
   a. If t not in reverseVisited:
      - BFS backward from t using reverse adjacency
      - Add all visited states to reverseVisited
4. DeadEndStates = { s ∈ ReachableStates : s ∉ TerminalStates AND s ∉ reverseVisited }
5. Emit Diagnostic(DeadEndState, Warning) for each dead-end state
```

A dead-end state is reachable from initial (it's not an unreachable state) but cannot reach any terminal (it's a lifecycle trap). This is distinct from `UnreachableState` — the state IS reachable, it just has no exit toward completion.

**Severity:** Warning, not error. Dead-end states may be intentional in designs that use certain states as permanent holds (e.g., a "Suspended" state from which entities are never expected to complete). The author can suppress the warning by adding the `terminal` modifier if the state is intended as an endpoint.

### 6.3 Dominator Computation (Phase 3)

**Algorithm:** Iterative dominator computation (Cooper, Harvey, Kennedy).

At Precept's bounded scale (≤50 states), the simple iterative algorithm is sufficient. Lengauer-Tarjan is unnecessary.

```text
Input: adjacency map, initial state, ReachableStates
Output: dominatorTree (immediate dominators), DominanceFact[]

1. dom[initial] = initial
2. For all other reachable states: dom[s] = undefined
3. changed = true
4. While changed:
   changed = false
   For each state s in reverse postorder (excluding initial):
     new_idom = first processed predecessor
     For each other predecessor p:
       if dom[p] defined:
         new_idom = intersect(new_idom, p)
     if dom[s] ≠ new_idom:
       dom[s] = new_idom
       changed = true
5. Build DominanceFact entries from dom[] tree
```

**Dominance verification for `required` modifier:**

A required state must dominate at least one terminal state. This ensures every complete execution path passes through the required state.

```text
For each state with RequiresDominator = true:
  terminalsReached = { t ∈ Terminals : state dominates t }
  if terminalsReached is empty:
    Emit Diagnostic(RequiredStateDoesNotDominateTerminal)
```

### 6.4 Structural Constraint Checks (Phase 3)

**Terminal constraint (`AllowsOutgoing = false`):**

```text
For each state where AllowsOutgoing = false:
  outgoing = edges where FromState = state
  if outgoing is not empty:
    Emit TerminalOutgoingViolation(state, outgoing)
    Emit Diagnostic(TerminalStateHasOutgoingEdges)
```

**Irreversible constraint (`PreventsBackEdge = true`):**

A back-edge is an edge from a state to an ancestor in the BFS traversal (indicating a cycle that returns to an earlier point).

```text
For each state where PreventsBackEdge = true:
  For each edge from state:
    if edge.ToState is ancestor of state in BFS tree:
      Emit IrreversibleBackEdgeViolation(state, edge)
      Emit Diagnostic(IrreversibleStateHasBackEdge)
```

### 6.5 Event Coverage Analysis (Phase 3)

Compute event coverage per state. The `EventCoverageEntry` feeds proof forwarding for dead guard sharpening — it is always computed regardless of diagnostic emission.

```text
For each event e:
  handlingStates = { s : ∃ transition row from s on event e }
  nonHandlingReachable = ReachableStates - handlingStates
  Emit EventCoverageEntry(e, handlingStates, nonHandlingReachable)

  if handlingStates is empty:
    Emit Diagnostic(UnhandledEvent, event)
```

**Diagnostic rules:**
- **Partial coverage** (some states handle, some don't) — valid, intentional authoring. No diagnostic emitted. (CC#21 ruling: violates §0.6 principle 2 to flag this.)
- **Zero coverage** (no state handles the event anywhere) — provably dead declaration. Emit `UnhandledEvent` (Warning).

> **Closed (CC#21):** The `optional` event modifier question is moot — with the old `UnhandledEvent` (partial-coverage) removed, there is nothing to suppress.

### 6.6 Proof Forwarding (Phase 4)

Package analysis results as typed facts for the proof engine:

1. **ReachabilityFact:** For each state, whether it's reachable and the path from initial (if reachable).

2. **DominancePathFact:** For each required state, which terminals it dominates.

3. **EventCoverageFact:** For each event with coverage gaps, which reachable states don't handle it.

4. **TerminalCompletenessFact:** Whether all terminal states are reachable.

5. **DeadEndStateFact:** Which reachable non-terminal states have no path to any terminal state.

---

## 7. Dependencies and Integration Points

### Upstream Dependencies

| Component | Dependency Type | Data Consumed |
|-----------|-----------------|---------------|
| Type Checker | Pipeline predecessor | `SemanticIndex` — resolved states, events, transitions |
| `Modifiers` catalog | Static metadata | `StateModifierMeta.AllowsOutgoing`, `.RequiresDominator`, `.PreventsBackEdge` |
| `Diagnostics` | Static metadata | `DiagnosticCode.UnreachableState`, `.UnhandledEvent`, `.DeadEndState` |

### Downstream Consumers

| Component | Dependency Type | Data Provided |
|-----------|-----------------|---------------|
| Proof Engine | Pipeline successor | `StateGraph` — topology, reachability, dominance, proof facts |
| Language Server | Tooling | `StateGraph` — for hover info, go-to-definition, diagnostics |
| MCP Server | Tooling | `StateGraph` — for `precept_compile` tool output |

### Catalog Integration

The graph analyzer reads from two catalogs:

1. **`Modifiers`** — via `Modifiers.GetMeta(kind)` to retrieve structural constraint flags for state modifiers.

2. **`Diagnostics`** — via `Diagnostics.Create(code, span, args)` to emit properly formatted diagnostics.

The analyzer does not write to catalogs. All catalog interaction is read-only.

---

## 8. Failure Modes and Recovery

### 8.1 Upstream Failures

**Empty or invalid `SemanticIndex`:**

If the type checker produces no states or no initial state, the graph analyzer cannot proceed meaningfully.

- **Detection:** Check `index.States.IsEmpty` or absence of initial modifier.
- **Recovery:** Return a minimal `StateGraph` with a diagnostic. Do not throw.

**Stateless precept (no states declared) — CC#26:**

A stateless precept declares no `state` blocks. This is a valid, complete configuration — not an error. The graph analyzer is **exempt from state-machine checks** for stateless precepts:

- **Initial-state reachability check:** Exempt. There is no initial state — `BFS from initial state` does not apply.
- **Unreachable-state check:** Exempt. There are no states to classify as reachable or unreachable.
- **Dead-end-state check:** Exempt. Dead-end analysis requires a state machine topology.
- **Terminal-state completeness check:** Exempt. No terminal states exist to validate.

**Detection:** `index.States.IsEmpty` — when the `SemanticIndex` has no states, the precept is stateless. The graph analyzer returns an empty-topology `StateGraph` with no diagnostics from topology checks. Event coverage analysis still runs if events are declared.

**Missing initial state (stateful precept, malformed):**

A stateful precept without an initial state has no entry point for graph traversal. This differs from a stateless precept — the author declared states but omitted the `initial` modifier.

- **Detection:** `index.States.Any()` is true, but `!index.States.Any(s => s.Modifiers.Contains(ModifierKind.Initial))`
- **Recovery:** Emit `Diagnostic(NoInitialState)` if the upstream stage did not already do so, treat all declared states as unreachable, and continue analysis so downstream tooling still receives coverage and proof-forwarding facts.

### 8.2 Structural Violations

Structural violations (terminal with outgoing edges, irreversible with back-edges, required not dominating terminal) are not failures — they are analysis results. The analyzer:

1. Records the violation in the appropriate collection (`TerminalViolations`, `BackEdgeViolations`).
2. Emits a diagnostic.
3. Continues analysis to completion.

The graph analyzer does not short-circuit on violations. All structural facts are computed regardless of individual violations.

### 8.3 Invariants

The graph analyzer maintains these invariants:

1. **Completeness:** Every state in `SemanticIndex.States` appears in either `ReachableStates` or `UnreachableStates`, never both, never neither.

2. **Deterministic output:** Given the same `SemanticIndex`, the `StateGraph` is identical across invocations (order-independent where order doesn't matter semantically).

3. **No exceptions:** The analyzer returns a `StateGraph` for any valid `SemanticIndex`. Structural problems are reported as diagnostics, not exceptions.

---

## 9. Contracts and Guarantees

### Preconditions

See §4a (Structural Preconditions) for the full precondition inventory with rationale. Summary:

| Precondition | Enforced By |
|--------------|-------------|
| Exactly one initial state (stateful precepts) | Type checker |
| All modifier references fully resolved with `ModifierMeta` populated | Type checker |
| All state/event name references in transitions resolve to declarations | Type checker (via NameBinder) |
| No `MissingExpression` nodes — all converted to `TypedErrorExpression` | Type checker |
| No `MalformedOutcome` — malformed transitions produce diagnostics, not rows | Type checker |
| `SemanticIndex` collections are non-null (possibly empty) | Type checker (`SemanticIndex.Empty` factory) |

### Postconditions

| Postcondition | Verified By |
|---------------|-------------|
| `ReachableStates ∪ UnreachableStates = AllStates` | Unit tests |
| `ReachableStates ∩ UnreachableStates = ∅` | Unit tests |
| Every `GraphEdge.FromState` and `GraphEdge.ToState` exists in `States` | Unit tests |
| Diagnostics use only registered `DiagnosticCode` values | Catalog tests |

### Invariants

| Invariant | Description |
|-----------|-------------|
| Catalog-driven dispatch | Structural constraint checks are derived from `StateModifierMeta` flags, not hardcoded per-modifier-kind |
| Deterministic ordering | Output collections use stable ordering (alphabetical by name or declaration order) |
| No side effects | The analyzer is a pure function: `SemanticIndex → StateGraph` |

---

## 10. Design Rationale and Decisions

### 10.1 Catalog-Driven Modifier Semantics

**Decision:** Structural constraint semantics (AllowsOutgoing, RequiresDominator, PreventsBackEdge) are defined in `StateModifierMeta`, not hardcoded in the analyzer.

**Rationale:** Precept follows a metadata-driven architecture. When a new state modifier is added, its structural semantics are declared in the catalog entry. The analyzer reads these flags and applies generic constraint-checking logic. This:

- Eliminates switch statements on `ModifierKind` for structural checks
- Ensures new modifiers automatically participate in graph analysis
- Keeps domain knowledge in the catalog, not scattered in pipeline code

### 10.2 Four-Phase Architecture

**Decision:** Separate edge construction, reachability, structural analysis, and proof forwarding into distinct phases.

**Rationale:** Each phase has clear inputs and outputs. Separation enables:

- Independent testing of each phase
- Clear data flow (each phase builds on prior results)
- Easier debugging (inspect intermediate state between phases)

### 10.3 Simple Dominator Algorithm

**Decision:** Use iterative dominator computation, not Lengauer-Tarjan.

**Rationale:** Precept state machines are small (3–15 states typical, bounded at 50). The iterative algorithm is O(n²) in the worst case, but at n ≤ 50, this is trivial. Lengauer-Tarjan's O(n·α(n)) complexity provides no practical benefit and adds implementation complexity.

### 10.4 Proof Forwarding as Typed Facts

**Decision:** Package analysis results as a discriminated union of `ProofForwardingFact` subtypes.

**Rationale:** The proof engine needs structured data, not string diagnostics. Typed facts enable:

- Type-safe pattern matching in the proof engine
- Clear contracts between stages
- Self-documenting data flow

### 10.5 Non-Short-Circuiting Analysis

**Decision:** The analyzer completes all phases regardless of violations.

**Rationale:** Reporting all structural issues in a single pass provides a better developer experience than stopping at the first error. The language server and MCP tools can display the complete diagnostic set.

---

## 11. Innovation

### 11.1 Catalog-Driven Modifier Applicability

Modifier applicability is catalog-driven — each `ModifierMeta` carries an `ApplicableTo` set that the graph analyzer reads to validate which constructs a modifier may appear on, and `StateModifierMeta` declares structural constraint flags (`AllowsOutgoing`, `RequiresDominator`, `PreventsBackEdge`) that the analyzer reads to determine what structural checks to apply. The graph traversal algorithms themselves (reachability, dead-state detection, liveness analysis, dominator computation) are generic machinery — they do not hardcode per-modifier behavior.

This inverts the typical pattern for modifier semantics: instead of "the analyzer knows what `terminal` means," the catalog declares "terminal means `AllowsOutgoing = false`" and the analyzer enforces "states with `AllowsOutgoing = false` must have no outgoing edges." But the BFS, dominator tree, and event coverage algorithms are standard graph algorithms — they are not catalog-derived.

### 11.2 Proof Forwarding Pipeline

Graph analysis facts are not consumed directly for diagnostics — they flow forward to the proof engine as structured facts. This separation means:

- The graph analyzer is a pure topology analyzer
- The proof engine decides which facts warrant obligations
- Future proof strategies can consume the same facts differently

---

## 12. Open Questions / Implementation Notes

### Implementation Notes

1. **Wildcard expansion ordering:** When expanding any-state wildcards, process states in declaration order to ensure deterministic edge ordering.

2. **Diagnostic source locations:** Structural violations (e.g., terminal with outgoing edges) should report the source location of both the modifier and the violating edge, enabling precise IDE diagnostics.

3. **Incremental analysis:** The current design is whole-graph analysis. Future work may introduce incremental updates for language server responsiveness, but this is not required initially.

### Implementation Notes

> **✅ Resolved — `EventCoverageEntry` stays at event-level granularity**
> `EventCoverageEntry` tracks events against all reachable states without splitting by guard presence. Guard-conditioned reachability (which specific transition rows actually fire under which guards) is the proof engine's domain via `ProofForwardingFact`. The graph analyzer cannot evaluate guards — it operates on structural facts only. Event-level coverage is sufficient for `UnhandledEvent` diagnostics and proof forwarding. Guard-split granularity would require guard evaluation, which belongs downstream.
> *Resolved: 2026-05-07 — Wave 4, team-autonomous*

> **✅ Resolved — BFS-ancestor definition is canonical for back-edges**
> A back-edge is an edge whose target state is a BFS-tree ancestor of the source state (established by traversal from the initial state). DFS back-edges are not used. BFS ancestor is the correct notion because reachability analysis and irreversibility constraints operate on the same BFS traversal order. The pseudocode already says "ancestor in the BFS tree" — that phrasing is canonical.
> *Resolved: 2026-05-07 — Wave 4, team-autonomous*

> **✅ Resolved — `GraphEvent.IsInitial` is derived from outgoing edges of the initial state**
> `GraphEvent.IsInitial = true` if and only if the event has at least one `GraphEdge` whose source state has `GraphState.IsInitial = true`. This is a purely structural derivation: the graph analyzer identifies the initial state (via `StateModifierMeta` for the `initial` modifier), then marks any event that fires from it. No event-level metadata is consulted — the derivation is entirely structural from the edge set.
> *Resolved: 2026-05-07 — Wave 4, team-autonomous*

> **✅ Resolved (OQ1) — DeadEndStateFact: separate fact type for dead-end state detection**
> Dead-end states (reachable non-terminal states with no path to any terminal state) are a distinct structural concern from terminal completeness (whether terminals are reachable from initial). `TerminalCompletenessFact` answers "can each terminal be reached?" — a forward perspective from the initial state. `DeadEndStateFact` answers "can each non-terminal reach a terminal?" — a forward perspective from each reachable state. Different questions, different data shapes, different proof consumption patterns. `DeadEndStateFact` carries `DeadEndStates` (the trapped states) and `DeadEndCount`. Detection uses reverse-reachability BFS from terminal states — any reachable non-terminal state not in the reverse-reachable set is a dead-end. The graph analyzer emits `DeadEndState` (Warning, code 108) per dead-end state. Warning severity, not error: dead-end states may be intentional permanent-hold designs; the author can suppress by adding the `terminal` modifier to declare the state as an intended endpoint.
> *Resolved: 2026-05-07 — OQ design session*

> **✅ Resolved (OQ2) — EventHandler entries do NOT count toward event coverage**
> Event handlers (`on Event -> actions`) are valid only in stateless precepts. The type checker emits `EventHandlerInStatefulPrecept` (code 92) if event handlers coexist with state declarations. The graph analyzer operates exclusively on stateful precepts — when graph analysis runs, no valid `TypedEventHandler` entries exist. Therefore, the question "should handlers count toward event coverage?" is structurally moot: the type system prevents the coexistence scenario. `EventCoverageFact` counts transition rows only. The `EventHandlers` field in `SemanticIndex` is not consumed by the graph analyzer — it is listed in §4 for completeness only. This is not a policy choice; it is a consequence of the type checker's stateless/stateful mutual exclusion rule (language spec §3.8).
> *Resolved: 2026-05-07 — OQ design session*

---

## 13. Deliberate Exclusions

| Exclusion | Rationale |
|-----------|-----------|
| **Guard satisfiability analysis** | Guards contain arbitrary expressions. Determining whether a guard can be satisfied requires the proof engine's SMT integration, not graph-level analysis. |
| **Data-flow analysis** | The graph analyzer operates on control flow (state transitions), not data flow (field values). Field analysis is the proof engine's domain. |
| **Path enumeration** | Enumerating all paths through the state machine is exponential. The analyzer computes structural properties (reachability, dominance) that summarize path behavior without enumeration. |
| **Cycle detection beyond irreversible** | General cycle detection is not a requirement. Only the `irreversible` modifier constrains cycles (via back-edge prevention). Other cycles are legal. |
| **Transition priority/ordering** | When multiple transitions match (e.g., from wildcard and explicit), priority is a runtime concern. The graph analyzer treats all matching transitions as edges. |

---

## 14. Cross-References

### Related Documentation

| Document | Relationship |
|----------|--------------|
| [Type Checker](./type-checker.md) | Upstream stage; defines `SemanticIndex` shape |
| [Proof Engine](./proof-engine.md) | Downstream stage; consumes `StateGraph` and proof facts |
| [Catalog System](../language/catalog-system.md) | Defines catalog-driven architecture and `ModifierMeta` hierarchy |
| [Precept Language Spec](../language/precept-language-spec.md) | Defines modifier semantics at the language level |
| [Compiler and Runtime Design](../compiler-and-runtime-design.md) | Pipeline overview and stage responsibilities |

### Related Catalogs

| Catalog | Usage |
|---------|-------|
| `Modifiers` | Source of `StateModifierMeta` structural constraint flags |
| `Diagnostics` | Source of diagnostic codes and message templates |

---

## 15. Source Files

| File | Purpose |
|------|---------|
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Main entry point: `GraphAnalyzer.Analyze(SemanticIndex)` |
| `src/Precept/Pipeline/StateGraph.cs` | Output type definition |
| `src/Precept/Pipeline/StateGraph.cs` | Supporting types: `GraphState`, `GraphEvent`, `GraphEdge`, analysis facts, proof-forwarding facts |
| `src/Precept/Language/Modifiers.cs` | Modifiers catalog with `StateModifierMeta` entries |
| `src/Precept/Language/Modifier.cs` | `ModifierMeta` discriminated union hierarchy |
| `test/Precept.Tests/Pipeline/GraphAnalyzerTests.cs` | Unit tests |

---

## Appendix: Diagnostic Codes

| Code | Name | Description |
|------|------|-------------|
| 32 | `NoInitialState` | No state is marked with the `initial` modifier (emitted defensively when upstream did not already diagnose) |
| 80 | `UnreachableState` | A state cannot be reached from the initial state |
| 81 | `UnhandledEvent` | An event declared in the precept has zero transition rows in any state — it can never be fired successfully |
| 108 | `DeadEndState` | A reachable non-terminal state has no structural path to any terminal state — the entity can enter this state but can never reach completion |
| 109 | `TerminalStateHasOutgoingEdges` | A terminal state has outgoing transitions to other states |
| 110 | `IrreversibleStateHasBackEdge` | An irreversible state has a transition returning to a BFS ancestor |
| 111 | `RequiredStateDoesNotDominateTerminal` | A required state does not dominate any terminal state |
