# Graph Analyzer

> **Status:** Design — implementation pending.

## 1. Overview

The graph analyzer is the fourth pipeline stage, transforming the type checker's `SemanticIndex` into a `StateGraph` containing topology models and structural analysis facts. This stage produces no runtime values — only structural analysis results that inform subsequent proof obligations and diagnostics.

**Pipeline Position:**

```text
Source Text → Lexer → Parser → Type Checker → [Graph Analyzer] → Proof Engine → Runtime
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

| Field | Purpose |
|-------|---------|
| `States` | `ImmutableArray<TypedState>` — state names and modifiers |
| `TransitionRows` | `ImmutableArray<TypedTransitionRow>` — edge definitions |
| `StateHooks` | `ImmutableArray<TypedStateHook>` — entry/exit hooks (stub — no downstream consumer yet) |

The graph analyzer reads only the topology-relevant subset of `SemanticIndex`. It does not consume `Fields`, `Rules`, `Ensures`, `AccessModes`, or `EventHandlers` — those belong to the proof engine and runtime builder.

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

> **Open Question:** `GraphState.Modifiers` representation
> `GraphState` currently materializes four modifier-derived booleans, but the doc never settles whether the graph should instead carry the original modifier set and derive flags from catalog metadata. `IsReachable` is topological output, so the unresolved part is specifically how modifier facts are represented.
> *Flagged: 2026-05-04*
> *Source: catalog-gap-register.md #9*

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
    TransitionOutcome Outcome
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
```

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
│  • Emit UnreachableState diagnostics                                 │
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

1. **Wildcard expansion:** For rows where `FromState` is null (any-state wildcard), generate one edge per declared state that does not have an explicit transition for that event.

> **Open Question:** Wildcard expansion ordering
> Wildcard transition expansion does not yet say whether it iterates all declared states or only states already proven reachable. The phase contract needs one rule because expansion scope changes graph shape, event coverage, and the runtime rows the builder will later compile.
> *Flagged: 2026-05-04*

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

Determine which events are handled in which states, identifying gaps.

```text
For each event e:
  handlingStates = { s : ∃ edge from s on event e }
  nonHandlingReachable = ReachableStates - handlingStates
  Emit EventCoverageEntry(e, handlingStates, nonHandlingReachable)
  
  if nonHandlingReachable is not empty:
    Emit Diagnostic(UnhandledEvent, state, event) for each gap
```

> **Open Question (unresolved):** Should there be a mechanism to suppress `UnhandledEvent` diagnostics for certain events (e.g., an "optional" modifier on events)? Currently no such mechanism exists in the language spec.

### 6.6 Proof Forwarding (Phase 4)

Package analysis results as typed facts for the proof engine:

1. **ReachabilityFact:** For each state, whether it's reachable and the path from initial (if reachable).

2. **DominancePathFact:** For each required state, which terminals it dominates.

3. **EventCoverageFact:** For each event with coverage gaps, which reachable states don't handle it.

4. **TerminalCompletenessFact:** Whether all terminal states are reachable.

---

## 7. Dependencies and Integration Points

### Upstream Dependencies

| Component | Dependency Type | Data Consumed |
|-----------|-----------------|---------------|
| Type Checker | Pipeline predecessor | `SemanticIndex` — resolved states, events, transitions |
| `Modifiers` catalog | Static metadata | `StateModifierMeta.AllowsOutgoing`, `.RequiresDominator`, `.PreventsBackEdge` |
| `Diagnostics` | Static metadata | `DiagnosticCode.UnreachableState`, `.UnhandledEvent` |

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

**Missing initial state:**

A precept without an initial state has no entry point for graph traversal.

- **Detection:** `!index.States.Any(s => s.Modifiers.Contains(ModifierKind.Initial))`
- **Recovery:** Emit `Diagnostic(NoInitialState)`, return empty reachability sets.

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

| Precondition | Enforced By |
|--------------|-------------|
| `SemanticIndex` is well-formed (no null collections) | Type checker |
| State and event names are non-null, non-empty | Type checker |
| Transition references resolve to declared states/events | Type checker |

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

### 11.1 Catalog-Derived Constraint Checking

Unlike traditional compilers where modifier semantics are embedded in analysis code, Precept's graph analyzer derives constraint checks from catalog metadata. The `StateModifierMeta` record declares the structural properties; the analyzer reads them and applies generic checks.

This inverts the typical pattern: instead of "the analyzer knows what `terminal` means," the catalog declares "terminal means AllowsOutgoing = false" and the analyzer enforces "states with AllowsOutgoing = false must have no outgoing edges."

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

### To Be Resolved During Implementation

> **Open Question:** `EventCoverageEntry` granularity
> The graph analyzer does not yet settle whether guarded and unguarded transition coverage stay separate or collapse into one coarser event-coverage entry. That choice affects diagnostics, tooling summaries, and whether guard-conditioned reachability remains visible downstream.
> *Flagged: 2026-05-04*

> **Open Question:** Back-edge definition
> The current text mixes a BFS-ancestor notion of back-edge with classic DFS back-edge terminology. The graph contract needs one definition because cycle detection and `irreversible` reasoning change depending on which graph-theory meaning is canonical.
> *Flagged: 2026-05-04*

> **Open Question:** `GraphEvent.IsInitial` derivation
> `GraphEvent` exposes `IsInitial`, but the doc never defines whether that flag comes from initial-state reachability, explicit event metadata, or creation-only routing. The analyzer needs one derivation rule before event facts can be treated as structural output.
> *Flagged: 2026-05-04*

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
| `src/Precept/Pipeline/GraphTypes.cs` | Supporting types: `GraphState`, `GraphEvent`, `GraphEdge`, analysis facts |
| `src/Precept/Language/Modifiers.cs` | Modifiers catalog with `StateModifierMeta` entries |
| `src/Precept/Language/Modifier.cs` | `ModifierMeta` discriminated union hierarchy |
| `test/Precept.Tests/Pipeline/GraphAnalyzerTests.cs` | Unit tests |

---

## Appendix: Diagnostic Codes

| Code | Name | Description |
|------|------|-------------|
| 80 | `UnreachableState` | A state cannot be reached from the initial state |
| 81 | `UnhandledEvent` | An event has no transition in one or more reachable states |
| TBD | `TerminalStateHasOutgoingEdges` | A terminal state has outgoing transitions |
| TBD | `IrreversibleStateHasBackEdge` | An irreversible state has a transition returning to an ancestor |
| TBD | `RequiredStateDoesNotDominateTerminal` | A required state does not dominate any terminal state |
| TBD | `NoInitialState` | No state is marked with the `initial` modifier |
