# State Machine Expressiveness

**Research date:** 2026-04-04  
**Author:** George (Runtime Dev)  
**Relevance:** What PLT and statechart theory offer beyond Precept's current `from/on/transition` model

This reference covers Precept's state machine capabilities. The philosophy also supports stateless entities with governed integrity but no state machine — those are outside this document's scope.

---

## Formal Concept

PLT distinguishes three generations of state machine formalism:

| Generation | Name | Core Additions |
|------------|------|---------------|
| 1 | Finite Automaton (FA) | States + transitions; no data |
| 2 | Extended FA (EFA/Mealy/Moore) | States + transitions + output actions; data guards |
| 3 | Statecharts (Harel, 1987) | Hierarchy, orthogonality, broadcast events, history |

Precept's state-machine component operates in **Generation 2** — it has states, transitions, guards, and output actions (mutations). This is the lifecycle-aware layer within a broader domain integrity engine that also governs stateless entities through the same field declarations, invariants, and constraint enforcement. Generation 3 features offer expressiveness, but each comes with semantic cost.

---

## What xstate/Statecharts Offer That Precept Doesn't

### 1. Hierarchical (Nested) States

**What it is:** A state can contain substates. Entering the parent implicitly enters the initial substate. A transition from the parent applies to all substates without repetition.

**xstate example:**
```js
const lightMachine = createMachine({
  states: {
    red: {
      initial: 'walk',
      states: {
        walk: { on: { PED_TIMER: 'wait' } },
        wait: { on: { PED_TIMER: 'stop' } },
        stop: {}
      },
      on: { TIMER: 'green' } // applies to walk, wait, AND stop
    },
    green: { on: { TIMER: 'yellow' } },
    yellow: { on: { TIMER: 'red' } }
  }
});
```

**Value:** Eliminates repeated transitions. `TIMER → green` applies to all substates of `red` without three rows. In Precept terms, this would express: "from any substate of UnderReview on Cancel → transition Cancelled" without listing each substate explicitly.

**Precept equivalent (partial):** `from any on Cancel` covers all states, not just substates of a parent. True hierarchical grouping has no Precept equivalent.

**Semantic cost for Precept:**
- Flat model assumption violated: the runtime builds lookup maps by `(State, Event)` pairs; hierarchy requires multi-level lookup with inheritance semantics
- Type-checker scope changes: state names are currently a flat set; hierarchy requires tree scope
- Diagnostic clarity: error messages must attribute to the right level of the hierarchy
- AI authoring: flat linear files are easy; tree-structured files require consistent indentation or new delimiters — breaks the "tooling-friendly flat structure" design principle
- **Cost category: HIGH — architecturally invasive**

### 2. Parallel (Orthogonal) Regions

**What it is:** A state can contain multiple independent sub-machines running simultaneously. Each region has its own state; the combined state is the Cartesian product.

**xstate example:**
```js
const playerMachine = createMachine({
  type: 'parallel',
  states: {
    playback: { initial: 'paused', states: { paused: {}, playing: {} } },
    volume:   { initial: 'normal', states: { normal: {}, muted: {} } }
  }
});
```

**Value:** Eliminates state explosion. N independent aspects with K values each require K^N states flat; parallel regions keep it N×K.

**Precept relevance:** Domain precepts rarely need parallel regions. Insurance claim status is one thing; it doesn't have a separate independent sub-machine running in parallel. If parallel regions were needed, it would signal that two separate entities should be two separate `.precept` files (composition at the domain level, not the state level).

**Cost category: HIGH — not applicable to Precept's domain model; composition belongs at the entity level**

### 3. History States

**What it is:** A pseudo-state that remembers the last active substate of a region, so that re-entering the parent resumes from where it left off rather than the initial substate.

**Precept relevance:** Precept already stores all data explicitly (fields). "History" in the statechart sense is a runtime variable tracking the last substate — Precept models this as a `nullable string` field with a transition that sets it. This is fine for current domain complexity.

**Cost category: LOW — expressible today via explicit field pattern; not needed as a language primitive**

### 4. After/Timeout Transitions

**What it is:** Automatic transitions that fire after a specified elapsed time in a state.

**Precept relevance:** Not applicable — Precept has no time concept; it's event-driven, not time-driven.

---

## What Precept's `from/on/transition` Model Handles Well

The current model is well-suited for:
- **Linear workflows** (Draft → UnderReview → Approved → Funded): clean row-per-path structure
- **Conditional branching** (`when` guards): first-match covers most business logic
- **Guard + mutation + outcome** in one row: self-contained rows are readable and AI-generatable
- **Multi-state shorthand** (`from any`, `from Open, InProgress`): reduces repetition without adding hierarchy

The design principle "self-contained rows" is *better* than statechart hierarchy for domain documents, because every path is individually readable without context from a parent node.

---

## What the Model Could Gain at Low Cost

### A. Wildcard Event Notation

Current: `from Open on Cancel → ...` and `from InProgress on Cancel → ...` (separate rows)  
Option: `from any on Cancel → ...` already exists. What's missing is `from Open, InProgress on Cancel, Suspend → ...` — multi-event in the `on` clause.

This is a flat desugaring (2×2 = 4 rows internally), consistent with existing multi-state expansion. **Cost: LOW**

### B. Default Catch-All Row

```precept
from any on any -> reject "Event not applicable in this state"
```

Currently, `from any on <Event>` must name a specific event. A wildcard `on any` that acts as a final catch-all row would reduce the need for empty `from S on E -> reject "..."` rows. This is analogous to a `default:` clause in a switch statement — a derived form that desugars to one row per `(S, E)` pair that has no other matching row.

**Semantic risk:** Would silently mask `Undefined` outcomes that currently signal authoring omissions. Needs clear documentation that it converts `Undefined → Rejected` and suppresses reachability warnings. **Cost: LOW-MEDIUM**

---

## Implementation Cost Summary

| Feature | Cost | Recommendation |
|---------|------|----------------|
| Multi-event `on` clause | Low | Viable for Phase 2 |
| Catch-all `on any` row | Low-Medium | Viable with semantic warning |
| Hierarchical states | High | Out of scope for Precept's model |
| Parallel regions | High | Not applicable to domain model |
| History states | Low (expressible today) | No new language primitive needed |
| After/timeout transitions | N/A | Outside Precept's event model |

---

## Semantic Risks Specific to Precept

1. **First-match semantics + multi-event**: If `from Open on Cancel, Suspend` desugars to two rows, their relative position to other `from Open on Cancel` rows must be preserved. Expansion must interleave correctly, not append to end.

2. **Catch-all masking**: A `from any on any` row added to the bottom would suppress all `Undefined` outcomes, including ones the author hasn't written transitions for yet. This could silence important C44–C48 analysis warnings.

3. **Orthogonality of new features**: xstate's Guard + Action + Target architecture is more composable than it looks — their `assign`, `sendTo`, and `raise` actions introduce reactive/side-effect semantics that Precept explicitly forbids. Every xstate feature must be evaluated against Precept's determinism requirement.

---

## Key References

- Harel, "Statecharts: A visual formalism for complex systems" (1987) — original statechart paper
- xstate v5 Documentation: https://xstate.js.org/docs/ — hierarchical and parallel states
- stately.ai parallel states: https://stately.ai/docs/parallel-states
- Sipser, *Introduction to the Theory of Computation* Ch. 1 — finite automata fundamentals
- Pierce, *TAPL* Ch. 24 — type systems for process calculi
