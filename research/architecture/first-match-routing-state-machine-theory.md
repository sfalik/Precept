# First-Match Guard Routing: Formal Properties and Theoretical Position

**Date:** 2026-04-19
**Author:** Frank (Lead/Architect)
**Research Angle:** State machine theory — formal semantics of Precept's first-match routing model
**Purpose:** Establish the formal classification of Precept's transition resolution algorithm, compare it to Harel Statecharts, SCXML, and XState, and give a future language designer a precise account of what changes are required to add hierarchy.

---

## Executive Summary

Precept's first-match guard routing model is a **deterministic extended finite-state machine (DEFSM) with declaration-order guard evaluation**. It is a proper subset of the flat-state fragment of SCXML's DocumentOrder algorithm, and a deliberate simplification of Harel Statecharts that eliminates hierarchy, parallel regions, and history in exchange for unconditional determinism and predictable diagnostics.

**Key finding:** First-match is not a weakness or an incomplete version of statecharts. It is a specifically chosen evaluation strategy that makes the transition table's semantics fully derivable from reading the source file top-to-bottom — no tooling, no runtime context, and no knowledge of sibling guards needed to predict behavior. The cost is real: Precept cannot express independent concurrent sub-behaviors or cross-cutting event handling without repetition. That cost is justified for the domain Precept targets (single-entity business rule enforcement), but would become a structural liability if Precept were ever extended to model composite entities with genuinely independent lifecycle dimensions.

**Verdict for future language designers:** Adding hierarchical states to Precept is not an incremental change. It requires redesigning the transition table schema, the resolution algorithm, the compiler's state model, the type checker's inheritance rules, the `from any` expansion semantics, and the grammar. Do not attempt it as a point feature.

---

## Survey Results

### 1. Precept Engine — First-Match Routing Implementation

**Source:** `docs/EngineDesign.md` §Transition Resolution; `docs/PreceptLanguageDesign.md` §First-Match Evaluation; `src/Precept/Dsl/PreceptRuntime.cs`

**Routing model:**
- The engine precomputes `_transitionRowMap: Dictionary<(State, Event), List<TransitionRow>>` at construction. All rows for a `(state, event)` pair are stored in a list in **declaration order**.
- At runtime, `ResolveTransition` iterates the list. The first row whose `when` guard evaluates to `true` (or has no guard) is selected. All subsequent rows are skipped.
- If no row matches: outcome is `NotApplicable` → surfaces as `Unmatched` to callers.
- If no rows exist for the pair: outcome is `Undefined`.

**Design principle (PreceptLanguageDesign.md §6):** "Collect-all for validation, first-match for routing. Validation (rules, ensures) reports every failure. Transition rows are evaluated top-to-bottom, first match wins. These are different problems with different evaluation strategies."

**Authoring convention:** Multiple rows for the same `(state, event)` pair model branching by placing specific guarded rows first and an unguarded catch-all (or explicit `reject`) row last. Declaration order is the author's primary control over routing priority.

**Diagnostic semantics:** `Unmatched` and `Undefined` are structurally distinct outcomes. `Unmatched` means guards exist but none passed for current data — a data-state issue, not a definition gap. `Undefined` means no routing surface exists — a design issue. Callers are expected to diagnose these differently.

---

### 2. SCXML W3C Recommendation — DocumentOrder Algorithm

**Source:** https://www.w3.org/TR/scxml/ — §3.13 Selecting and Executing Transitions; Appendix D Algorithm for SCXML Interpretation

**Routing model:**
SCXML's `selectTransitions(event)` algorithm is:
1. For each active atomic state (in document order):
2. Walk up the ancestor chain starting from the atomic state.
3. At each state, iterate transitions in document order.
4. The first transition whose `event` attribute matches and whose `cond` evaluates to `true` is selected. Break the inner loop.
5. Remove conflicting transitions (where exit sets intersect).

For a **flat SCXML document** (no compound states, no parallel regions), this algorithm degenerates to exactly Precept's first-match: scan rows in declaration order, first passing guard wins.

**The critical difference** is the outer ancestor-walk loop. In hierarchical SCXML, if no transition matches in the atomic state, the search continues up to the parent, grandparent, and so on. Transitions in ancestor states serve as inherited defaults. Precept has no ancestor states and no fallback walk — the lookup is a direct flat table read.

**DocumentOrder priority rule:** When transitions conflict (exit sets intersect), the transition from the **more deeply nested** source state wins. Among transitions at the same depth, earlier document order wins. This is how SCXML resolves priority for hierarchical machines.

**Micro/macrostep model:** SCXML processes internal events in microsteps before accepting the next external event. Precept's engine is simpler: Fire is a single atomic operation with no internal event queue and no eventless transition loops. This eliminates an entire class of behavioral complexity.

**History states:** SCXML defines `<history>` pseudo-states (shallow and deep) that record and restore active sub-configurations. Precept has no history mechanism — returning to a state always enters its single declared form.

**Parallel states:** SCXML's `<parallel>` element makes all child states simultaneously active. Events are processed against all active atomic states concurrently (serially executed, logically parallel). Precept has no parallel regions — exactly one state is active at any time.

---

### 3. Harel Statecharts (1987) — Source Theory

**Source:** Wikipedia — UML State Machine (§UML extensions to the traditional FSM formalism); David Harel, "Statecharts: A Visual Formalism for Complex Systems," *Science of Computer Programming*, 1987.

**Core innovations over flat FSMs:**

1. **Hierarchically nested states (HSM).** A state may contain child states. Being in a substate means being simultaneously in the superstate. Events not handled in a substate propagate to the superstate — "programming by difference." This eliminates state explosion by factoring out common behavior into superstate transitions.

2. **Orthogonal regions (parallel states).** A composite state may have multiple simultaneously-active regions. Each region independently processes events. State space is additive for independent regions rather than multiplicative.

3. **History pseudo-states.** Shallow history restores the most recently active immediate child; deep history restores the full active descendant configuration. Enables "pause and resume" semantics across hierarchical regions.

4. **Entry and exit actions.** Guaranteed to execute on every entry/exit to a state regardless of transition path. Semantically analogous to constructors and destructors.

**Guard ordering.** The UML specification (which formalizes Harel's work) **deliberately does not stipulate evaluation order** for multiple transitions with the same triggering event. The burden is placed on the designer to make guards non-overlapping. This is in explicit contrast to Precept's first-match, which defines a total ordering and eliminates ambiguity by fiat.

**Priority.** In UML/Harel machines, substate transitions take priority over superstate transitions. Among transitions at the same nesting level, the specification leaves evaluation order undefined for multiple guards on the same event.

**Flat FSM limitation.** Harel specifically observed that flat FSMs suffer state and transition explosion: the number of states and transitions grows faster than the complexity of the modeled system because common behavior must be repeated in every state. Hierarchy was designed as the solution.

---

### 4. FSM Formal Definition — Determinism Conditions

**Source:** Wikipedia — Finite-State Machine (§Mathematical model, §Classification/Determinism)

**Formal DFA definition:** A deterministic finite automaton is a quintuple $(Σ, S, s_0, δ, F)$ where:
- $Σ$ is the input alphabet
- $S$ is a finite non-empty set of states
- $s_0 \in S$ is the initial state
- $δ: S \times Σ → S$ is the state-transition function (total or partial)
- $F \subseteq S$ is the set of accepting/final states

**Determinism condition:** $δ$ is a function, not a relation — for each $(s, e)$ pair, at most one next state is defined. If $δ$ is partial, undefined pairs produce an error (equivalent to Precept's `Undefined` outcome).

**Non-deterministic FSMs** allow $δ: S \times Σ → \mathcal{P}(S)$ — multiple next states for the same input. Any NFA can be converted to an equivalent DFA by the powerset construction, typically at exponential state cost.

**Extended state machines.** When guard conditions are added (evaluated against extended state variables), the formal definition expands: $δ: S \times Σ \times D → S$ where $D$ is the data state. If guards can overlap (multiple transitions enabled simultaneously), the machine is technically non-deterministic in the extended sense. First-match evaluation is one canonical strategy for recovering determinism from a potentially non-deterministic guard table: the first matching row is the unique outcome.

**Precept's determinism:** Precept's extended state transition function is $δ(currentState, event, instanceData) → outcome$ — a function, not a relation. First-match guard evaluation ensures this: for any valid input triple, at most one row is selected and the outcome is unique. The `Unmatched` outcome is the partial-function "undefined" case, explicitly surfaced rather than silently ignored.

---

### 5. XState (v5) — Modern Statecharts Implementation

**Source:** https://stately.ai/docs/guards; https://stately.ai/docs/transitions

**Guard evaluation model:**
XState's multi-guard syntax uses an explicit ordered array:
```javascript
on: {
  'feedback.provide': [
    { guard: 'sentimentGood', target: 'thanks' },  // checked first
    { guard: 'sentimentBad', target: 'form' },       // checked second
    { target: 'form' }                               // default: no guard
  ]
}
```
"Each transition will be tested in order, and the first transition whose `guard` evaluates to `true` will be taken." This is structurally identical to Precept's first-match, within a single state.

**Hierarchy overlay:** XState is a full statechart implementation. Transition selection begins at the deepest active child state and propagates up: "Start on the deepest active state nodes. If the transition is enabled, select it. If not, go up to the parent state node and repeat." The first-match within a state is nested inside this hierarchical traversal.

**Parallel states:** XState supports `type: 'parallel'` states with multiple simultaneously-active regions. Multiple transitions fire in parallel regions. Precept has no equivalent.

**Wildcard transitions:** XState's `*` wildcard matches any event. Combined with hierarchy, wildcards on parent states become catch-all handlers for entire subtrees. Precept's `from any` is a compile-time expansion (not a runtime wildcard), producing one row per state in the table.

**Comparison to Precept:** Within a single flat state, XState's guard array and Precept's first-match rows are semantically equivalent. The divergence is everything else: XState's machine is the full Harel model with hierarchy, orthogonal regions, and dynamic context; Precept's machine is a flat, field-driven DEFSM with a static state set and a rule engine.

---

## Synthesis: First-Match in the Formal Literature

### Classification

Precept's routing model is a **deterministic extended finite-state machine with declaration-order guard evaluation (DEFSM-DO)**. Formally:

- **State space:** Finite, flat (no nesting), declared at compile time.
- **Extended state:** The `InstanceData` dictionary — evaluated by guard expressions at runtime.
- **Transition function:** $δ(s, e, d) = $ first row in $rows(s, e)$ whose `when(d)$ evaluates to $true$, or `Unmatched` if none.
- **Determinism guarantee:** Provided unconditionally by the linear scan. No two rows can simultaneously win.

This puts Precept in the following position relative to the literature:

| Model | Where Precept stands |
|---|---|
| Classical DFA | Precept is an extended DFA (data-driven guards) |
| Harel Statecharts | Precept is the flat-state, single-region fragment only |
| SCXML flat profile | Structurally equivalent for single-level states |
| UML Extended State Machine | Equivalent, but with defined guard ordering (UML leaves it undefined) |

### The Flat Fragment of SCXML is Precept

SCXML's `selectTransitions` algorithm, restricted to a document with no `<parallel>` elements and no compound states (all states are atomic), degenerates to:

```
for each transition in state.transitions sorted by document order:
    if transition.event matches AND transition.cond is true:
        take transition
        break
```

This is exactly `ResolveTransition`. The algorithms are isomorphic in the flat case. The only difference is Precept's clean separation of `Undefined` (no rows) from `Unmatched` (rows exist, none passed) as first-class outcomes, whereas SCXML simply produces no transition in both cases.

### Where Precept Diverges Formally

1. **No ancestor fallback.** SCXML searches ancestors if no transition fires in the current state. Precept stops at the flat row list. There is no inherited behavior from a "parent" state — because there are no parent states.

2. **Atomic guard evaluation context.** Precept builds a snapshot evaluation context before beginning the row scan. SCXML can evaluate guards dynamically as events trigger internal transitions during a macrostep. Precept has no internal event queue and no macrostep loop.

3. **No eventless transitions.** SCXML's `selectEventlessTransitions` loop fires transitions with no `event` attribute whenever conditions are true. Precept has no equivalent — all transitions are event-triggered.

4. **`from any` is syntactic sugar, not a runtime wildcard.** Precept expands `from any on E` to one row per declared state at parse time. The runtime table has no wildcard concept — each row is a concrete `(state, event)` pair.

---

## What First-Match Buys

### 1. Determinism by Construction

A Precept machine is always deterministic regardless of how guards are written. Even if guards overlap (e.g., Row 1: `when Score >= 80`, Row 2: `when Score >= 60`), the outcome is unambiguous: whichever row appears first in the source file wins for any input that satisfies both. Authors do not need to prove mutual exclusion — they only need to order rows by priority.

UML/Harel imposes the opposite burden: guards must be non-overlapping, and the spec explicitly refuses to define evaluation order. In practice, every UML tool implementation picks an order, but that order is not guaranteed by the standard. Precept's guarantee is unconditional and source-derivable.

### 2. Declarative Fallthrough

The "guarded rows + unguarded catch-all" pattern is idiomatic Precept:

```precept
from Submitted on Cancel when Cancel.reason == "fraud"
    -> set Balance = 0 -> transition Canceled
from Submitted on Cancel
    -> reject "Only fraud cancellation allowed from Submitted"
```

The catch-all row (no `when`) always fires if all specific rows fail. This pattern directly encodes the author's intent: specific conditions first, general fallback last. No additional syntax is needed.

### 3. Predictable `Unmatched` Diagnostics

`Unmatched` is a first-class outcome with a well-defined meaning: rows exist for this `(state, event)` pair, but no guard passed for the current data state. This is distinct from `Undefined` (no rows exist). The caller can deliver actionable feedback: "this action is not available right now because [condition X] is not met." Without first-match's explicit guard evaluation and termination, this distinction would be harder to surface cleanly.

### 4. Source-Identical Behavior

Because first-match uses declaration order, two engineers reading the same `.precept` file will derive identical routing behavior by inspection. No tool support is needed. The transition table is the code — readable, reviewable, diffs-able. This is a direct consequence of having a single, well-defined ordering strategy rather than leaving it implementation-defined.

### 5. No Conflict Resolution Infrastructure

SCXML's `removeConflictingTransitions` procedure handles the case where parallel regions both select transitions with overlapping exit sets, applying a complex priority algorithm. Precept has none of this because: (a) no parallel regions → no concurrent transition selection, (b) no hierarchical states → no overlapping exit sets. The entire conflict-resolution subsystem is structurally unnecessary.

---

## What First-Match Costs

### 1. No Transition Inheritance (The Big One)

In Harel/SCXML, if a substate does not handle an event, the event propagates to the superstate. This means common behavior can be placed once on a parent state and inherited by all children. Precept has no equivalent. A transition available in N states requires N rows (or `from any`, which expands to N rows at compile time). For large state sets, this creates repetition.

**Quantified:** A precept with 10 states where `Cancel` always means the same thing requires 10 `from X on Cancel` rows. A statechart would handle this with one `on Cancel` handler on the root state. The Harel model's "programming by difference" — substates only define what differs from their superstate — is simply unavailable.

### 2. No Parallel States

Precept cannot model entities with genuinely independent concurrent sub-behaviors in the same state machine. An insurance claim that simultaneously tracks a `PaymentStatus` lifecycle and a `DocumentStatus` lifecycle must either (a) represent the Cartesian product of states explicitly (e.g., `PendingPayment_DocumentsPending`, `PendingPayment_DocumentsComplete`, etc.) or (b) use two separate precepts with external coordination. The state explosion problem Harel solved with orthogonal regions is unsolved in Precept's model.

This is **justified** for the current domain: Precept governs single entities with a single lifecycle dimension. But if a future use case requires an entity with two truly independent sub-lifecycles (e.g., a subscription with an independent billing state), the flat model will force state explosion or model splitting.

### 3. No History

Returning to a state in Precept always returns to the state's "default" form — whatever the initial or `to` entry actions establish. There is no mechanism to resume a prior sub-configuration. This is not a limitation in the current model (Precept has no substates to resume), but it would become a gap if hierarchy were added.

### 4. `from any` is an Approximation, Not a Wildcard

`from any on E` expands to one row per declared state. This means:
- Adding a new state requires auditing all `from any` usage to verify it should apply to the new state.
- The expansion produces N rows in the runtime table, which is transparent to the engine but visible in tooling (e.g., the preview inspector shows all expanded rows).
- In statecharts, a root-level wildcard transition covers all present and future substates automatically. Precept's expansion is static and brittle to state additions.

### 5. Cross-Cutting Behavior Requires Repetition or Pattern Discipline

In SCXML/XState, behavior that should apply regardless of current substate is placed on a parent (or root) state and inherited automatically. In Precept, the author must use `from any` and accept the expansion, or repeat the row. There is no structural mechanism for "this event means the same thing in all states." `from any` achieves the effect, but it is syntactic sugar over repetition — not a semantic inheritance mechanism.

---

## Comparison Table

| Property | Precept | Harel Statecharts (1987) | SCXML (W3C 2015) | XState v5 |
|---|---|---|---|---|
| **Routing model** | First-match, declaration order | Depth-first, then document order in same level | DocumentOrder (depth-first ancestor walk) | Depth-first ancestor walk; first-match in guard arrays |
| **Guard ordering** | Defined: top-to-bottom declaration order | Undefined by spec (designer's burden) | Defined: document order within a state, depth priority between levels | Defined: array index order within a state |
| **Hierarchy** | None (flat states) | Full (compound states, arbitrary nesting) | Full (compound states, arbitrary nesting) | Full |
| **Parallel states** | None | Orthogonal regions | `<parallel>` element | `type: 'parallel'` |
| **History** | None | Shallow and deep history pseudo-states | `<history>` element | `type: 'history'` |
| **Inherited transitions** | None (`from any` is compile-time expansion) | Yes (superstate handles unmatched events) | Yes (ancestor walk in `selectTransitions`) | Yes (parent-to-child fallback) |
| **Eventless transitions** | None | Via epsilon/completion transitions | `<transition>` without `event` attribute | `always:` transitions |
| **Wildcard events** | None (compile-time expansion only) | Via UML trigger matching semantics | `*` event descriptor | `'*'` event type |
| **Determinism guarantee** | Unconditional (by construction) | Requires non-overlapping guards (by convention) | Defined (document order resolves ties) | Defined (array order + depth priority) |
| **"Unmatched" diagnostic** | Explicit `Unmatched` outcome | N/A (event silently discarded if unhandled) | No transition taken | No transition taken |
| **`Undefined` vs `Unmatched`** | Structurally distinct outcomes | Not distinguished | Not distinguished | Not distinguished |
| **Conflict resolution** | Unnecessary (no parallel regions) | Priority by depth and document order | `removeConflictingTransitions` algorithm | Automatic by hierarchy |
| **Internal event queue** | None | Implementation-defined | `internalQueue` with macrostep loop | Actor-based (internal events) |
| **State representation** | Single active state (string) | Active state configuration (set of states) | Configuration (ordered set of states) | State value (nested object) |

---

## Implications for ArchitectureDesign.md and PreceptLanguageDesign.md

### For ArchitectureDesign.md

The engine section should note explicitly that `PreceptEngine` implements a **flat DEFSM with declaration-order guard evaluation**, and that this is a deliberately scoped subset of the Harel statechart formalism. The architectural scope claim ("Precept governs single-entity lifecycle with a single active state") should be grounded in this formal positioning. ArchitectureDesign.md currently may implicitly assume the flat model without naming its theoretical basis — naming it gives future contributors a precise handle for extension proposals.

### For PreceptLanguageDesign.md

The First-Match Evaluation section (§ currently at approximately L1303) should cite this research as the grounding for the claim that first-match provides determinism by construction, and note that the alternative (undefined guard ordering, as in UML) was explicitly rejected. The documentation already correctly describes the mechanics; it does not currently explain *why* this model was chosen over alternatives, or what the formal properties are. Adding a two-paragraph rationale note would close that gap.

### What Adding Hierarchy Would Actually Require

For the record, a future proposal to add hierarchical states would need to change:

1. **Grammar:** States declarable inside other states (`state Active { state Reviewing { ... } }`). `initial` on compound states.
2. **Parser:** Nested state block parsing. State name resolution that respects nesting scope.
3. **Type checker:** State identity must become a path (e.g., `Active.Reviewing`) rather than a flat name. `from any` expansion must walk the state hierarchy. Transition conflict detection across nesting levels.
4. **Compiler / `_transitionRowMap`:** Must change from flat `(State, Event)` keys to support hierarchical lookup — ancestor walk on miss.
5. **`ResolveTransition`:** Must implement the ancestor-walk loop (equivalent to SCXML's `selectTransitions` inner `for s in [state].append(getProperAncestors(state, null))`).
6. **`PreceptInstance.CurrentState`:** Must change from a string to a state configuration (path or set). The immutable instance record model is compatible, but serialization changes.
7. **Entry/exit actions:** Must apply LCCA logic (exit states up to Least Common Compound Ancestor, enter states down to target). Currently entry/exit anchors are flat.
8. **`from any` expansion:** Must be removed or redesigned — in a hierarchy, `from any` would collide with inherited transitions from parent states.
9. **Diagnostics (`Undefined`, `Unmatched`):** Would need re-scoping. An event handled by an ancestor is not `Undefined`.
10. **Language server, preview inspector, diagram layout:** All visual representations assume flat state sets.

This is not an additive change. It is a ground-up redesign of the state model with pervasive effects across every layer of the stack.

---

## References

1. **Precept EngineDesign.md** — §Fire, §Transition Resolution, §Outcome Taxonomy. `c:\...\docs\EngineDesign.md`
2. **Precept PreceptLanguageDesign.md** — §6 (Design Principles), §First-Match Evaluation, §When Guards. `c:\...\docs\PreceptLanguageDesign.md`
3. **SCXML W3C Recommendation** (2015) — §3.13 Selecting and Executing Transitions; Appendix D Algorithm for SCXML Interpretation. https://www.w3.org/TR/scxml/
4. **Harel, David** (1987) — "Statecharts: A Visual Formalism for Complex Systems." *Science of Computer Programming*, pp. 231–274.
5. **UML State Machine** — Wikipedia article summarizing OMG UML 2.5.1, §StateMachines. https://en.wikipedia.org/wiki/UML_state_machine
6. **Finite-State Machine** — Wikipedia, §Mathematical model, §Classification/Determinism. https://en.wikipedia.org/wiki/Finite-state_machine
7. **XState v5 Guards** — Stately/XState documentation, §Multiple guarded transitions. https://stately.ai/docs/guards
8. **XState v5 Transitions** — Stately/XState documentation, §Selecting transitions. https://stately.ai/docs/transitions
9. **Hopcroft, J.; Motwani, R.; Ullman, J.** (2006) — *Introduction to Automata Theory, Languages, and Computation*, 3rd ed. Addison-Wesley.
