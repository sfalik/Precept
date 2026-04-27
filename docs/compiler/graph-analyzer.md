# Graph Analyzer

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Diagnostics-only stub |
| Source | `src/Precept/Pipeline/GraphAnalyzer.cs`, `src/Precept/Pipeline/StateGraph.cs` |
| Upstream | SemanticIndex (from TypeChecker), catalog metadata |
| Downstream | ProofEngine, Precept Builder, LS structural diagnostics |

---

## Overview

The graph analyzer derives lifecycle structure from semantic declarations. It transforms the resolved `SemanticIndex` into a `StateGraph` — a directed edge set plus derived structural facts (reachability, dominance, event coverage, topology violations). Its key design choice: it operates on resolved semantic identities, not syntax, because reachability and dominance require identity, not source position.

---

## Responsibilities and Boundaries

**OWNS:** Transition edge set construction, reachability computation, terminal/unreachable state detection, dominance analysis, irreversible back-edge detection, event coverage per state, ProofForwardingFacts production.

**Does NOT OWN:** Semantic resolution (TypeChecker), expression proof obligations (ProofEngine), runtime topology indexes (Precept Builder — builds execution-time indexes from graph facts).

---

## Right-Sizing

The graph analyzer is sized to structural analysis only. It does not evaluate expressions (that is the proof engine's domain) and does not plan execution (that is the builder's domain). The `StateGraph` artifact is topology + structural facts — nothing more. Modifier semantics (`initial`, `terminal`, `required`, `irreversible`) are resolved by the TypeChecker; the graph analyzer consumes already-resolved modifier facts without re-interpreting them.

---

## Inputs and Outputs

**Input:**
- `SemanticIndex` — typed state/event/transition declarations with resolved modifier facts
- Catalog metadata: Modifiers, Actions, Diagnostics

**Output:**
- `StateGraph` — adjacency set, predecessor/successor indexes, reachability partition, derived structural facts (`DominanceFact`, `TerminalOutgoingViolation`, `IrreversibleBackEdgeViolation`, `EventCoverageEntry`, `ProofForwardingFact`), diagnostics

---

## Architecture

Graph construction then structural analysis in four phases:

1. **Phase 1 — Edge set:** Build directed edge set from `TypedTransitionRow` entries in `SemanticIndex`.
2. **Phase 2 — Reachability:** Compute reachability from initial state via BFS/DFS. Partition states into reachable / unreachable sets.
3. **Phase 3 — Structural facts:** Derive dominance, coverage, and violation facts from topology.
4. **Phase 4 — Proof forwarding:** Forward structural defects as `ProofForwardingFact` entries for the proof engine.

---

## Component Mechanics

### Edge Set Construction

Each `TypedTransitionRow` contributes directed edges. `from → to` rows produce a single edge. `in` rows produce self-edges (state-scoped event declarations with no topology change). `on` rows produce edges from any reachable state to the target state. The edge set is keyed by `(TypedState, TypedEvent)` pairs — semantic identity, not string names.

### Reachability Computation

BFS from the initial state. Every state reachable from the initial state via any chain of transitions is in the reachable set. States not reachable from the initial state are in the unreachable set and produce `UnreachableState` diagnostics.

### Dominance Analysis

A state `s` dominates a terminal state `t` if every path from initial to `t` passes through `s`. Required-state modifier mandates this dominance relationship — a `required` state that does not dominate at least one terminal state is a `DominanceFact` violation.

### Event Coverage

For each reachable state, the analyzer computes which events have declared transition rows. States that have no declared rows for an event that is reachable in the lifecycle are represented as `EventCoverageEntry` entries — informational, not necessarily violations.

### Violation Detection

| Violation | Condition |
|---|---|
| `TerminalOutgoingViolation` | Terminal state has outgoing transitions |
| `IrreversibleBackEdgeViolation` | A transition re-enters an irreversible state from a downstream state |
| `UnreachableState` (diagnostic) | State not reachable from initial state |
| `DominanceFact` violation | Required state does not dominate any terminal |

---

## Dependencies and Integration Points

- **SemanticIndex** (upstream): typed transition rows, typed state modifier facts, typed event declarations
- **Catalogs** (upstream): Modifiers (for `initial`, `terminal`, `required`, `irreversible` semantics), Diagnostics
- **ProofEngine** (downstream): consumes `ProofForwardingFact` entries from structural violations
- **Precept Builder** (downstream): builds runtime dispatch indexes and reachability index from `StateGraph` topology
- **LS structural diagnostics** (downstream): surfaces unreachable states and topology violations in the editor

---

## Failure Modes and Recovery

The graph analyzer accumulates all diagnostics without aborting. If no initial state is declared, the edge set is empty and reachability produces an empty reachable set with a diagnostic. Topology violations produce diagnostics and structural fact entries; downstream stages (proof, builder) treat them as signals to suppress derivative work or plant fault backstops.

---

## Contracts and Guarantees

- Every state in `SemanticIndex` appears in exactly one of: reachable set, unreachable set.
- Every `TypedTransitionRow` in `SemanticIndex` contributes at least one edge to the edge set.
- `ProofForwardingFact` entries are produced for all structural violations that have proof-level implications.

---

## Design Rationale and Decisions

TBD — design rationale section to be populated during implementation. Key decisions include: choice of graph algorithm for dominance (see Open Questions), definition of "unreachable" in the presence of `on` anchor rows.

---

## Innovation

- **Reachability as a first-class design artifact:** Graph analysis produces reachable/unreachable state sets, structural validity facts, and runtime indexes — not just a pass/fail check. These facts flow into proof obligations and runtime precomputation.
- **Lifecycle soundness at compile time:** Unreachable states, terminal outgoing-edge violations, required-state dominance violations, and irreversible back-edges are caught before any instance exists. No state machine library in this category provides this level of static lifecycle verification.
- **Structural cycle and dominance detection:** The graph analyzer reasons about structural properties (dominance, predecessor/successor relationships, event coverage per state) that would otherwise require runtime observation to discover.

---

## Open Questions / Implementation Notes

1. `GraphAnalyzer.Analyze` throws `NotImplementedException` — implementation not started beyond stub wiring.
2. Confirm graph algorithm for dominance computation: standard immediate dominator algorithm, or Lengauer-Tarjan? At Precept's scale (bounded state count), a simple post-dominator DFS is sufficient; Lengauer-Tarjan is not needed.
3. Confirm `EventCoverageEntry` definition: what does "uncovered" mean exactly — event declared but no row in this state, or event reachable but no row here?
4. Confirm `ProofForwardingFact` shapes before implementing the proof engine — the proof engine depends on these.
5. Confirm whether `StateGraph` needs a predecessor index at runtime (Precept Builder uses it) or only at graph-analysis time.

---

## Deliberate Exclusions

- **No expression evaluation or proof:** Constraint satisfiability and operation safety are the proof engine's domain.
- **No execution planning:** Runtime dispatch indexes are built by the Precept Builder from graph facts.
- **No modifier re-resolution:** Modifier semantics (`initial`, `terminal`, `required`, `irreversible`) are resolved by the TypeChecker; the graph analyzer consumes already-resolved modifier facts.

---

## Cross-References

| Topic | Document |
|---|---|
| Full StateGraph design | `docs/compiler-and-runtime-design.md §7` |
| SemanticIndex input contract | `docs/compiler/type-checker.md` |
| ProofForwardingFacts consumer | `docs/compiler/proof-engine.md` |
| Runtime index builder from StateGraph | `docs/runtime/precept-builder.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Graph analyzer implementation — `GraphAnalyzer` static class with `Analyze(SemanticIndex)` entry point |
| `src/Precept/Pipeline/StateGraph.cs` | `StateGraph` — topology and derived structural facts artifact |
