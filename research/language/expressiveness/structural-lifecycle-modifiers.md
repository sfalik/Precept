# Structural Lifecycle Modifiers

**Research date:** 2026-04-10
**Author:** Frank (Lead/Architect & Language Designer)
**Batch:** Horizon — no open proposal
**Relevance:** Declarative modifiers on state and event declarations that constrain graph-structural roles, lifecycle boundary semantics, path obligations, and residency properties

This file is durable research, not a proposal body. It captures the full modifier design space, what adjacent systems do, which candidates fit Precept's compile-time-first philosophy, and which should be deferred or rejected.

---

## Background and Problem

### The `initial` precedent

Precept already has one structural modifier: `initial` on state declarations.

```precept
state Draft initial
```

`initial` is a **lifecycle boundary modifier** — it constrains which state an entity starts in. The compiler enforces two structural rules:

- **C8 (parse):** Only one state may be marked `initial` — duplicate initial is a hard error.
- **C13 (parse):** Exactly one state must be marked `initial` (when states are declared) — missing initial is a hard error.

`initial` has several properties that make it an instructive precedent for future modifiers:

1. **Keyword modifier on a declaration** — appears after the state name, before the comma or end-of-line.
2. **Compile-time provable** — no runtime state needed; the parser validates it from declared structure alone.
3. **Single-entity, finite-vocabulary property** — it's a property of the precept's own graph, not a cross-entity or open-ended predicate.
4. **Fills a real authoring gap** — without `initial`, the compiler couldn't enforce a unique entry point.

### The gap: declared intent vs inferred structure

The Precept analyzer (`PreceptAnalysis.cs`) already computes structural graph properties:

| Property | How detected | Diagnostic | Severity |
|---|---|---|---|
| **Unreachable states** | BFS from initial; states not in reachable set | C48 | Warning |
| **Terminal states** | States with no outgoing transition rows | (none — inferred, not diagnosed) | — |
| **Dead-end states** | Non-terminal states where all outgoing rows reject or no-transition | C50 | Warning |
| **Orphaned events** | Events declared but never referenced in transition rows | C49 | Warning |
| **Reject-only pairs** | (State, Event) pairs where every row rejects | C51 | Warning |
| **Events that never succeed** | Events where every reachable state either has no rows or all rows reject | C52 | Warning |

Terminal states are detected but never *diagnosed* — they are structurally normal. The analyzer distinguishes terminal states (no outgoing rows) from dead-end states (outgoing rows that all fail). Dead-end states get C50 because they have transition *machinery* that never succeeds — likely an authoring mistake.

But the author has no way to express **intent**. Consider:

```precept
state Closed   # No outgoing transitions — is this intentional?
state Stuck    # All outgoing transitions reject — is this a bug?
```

Without a modifier, the compiler cannot distinguish:
- **Intentionally terminal** — `Closed` is the lifecycle endpoint; no outgoing transitions is correct.
- **Accidentally dead-end** — `Stuck` was supposed to have an escape route but the author forgot or wrote guards that are always false.

A `terminal` modifier would let the author declare intent, turning C50 from a "maybe a problem" warning into an "actually a problem" error for non-terminal states, and suppressing it for states explicitly marked as endpoints.

### The broader question

Shane's question extends beyond `terminal`: what other structural modifiers exist in the design space for states and events? This research maps the full taxonomy, surveys comparable systems, and evaluates each candidate against Precept's design principles.

---

## Taxonomy of Structural Modifier Types

Structural modifiers constrain properties that are **graph-provable** — derivable from the declared states, events, and transition rows without runtime data. They fall into five categories.

### 1. Boundary modifiers — lifecycle entry and exit points

| Modifier | Applies to | Meaning | Compile-time check |
|---|---|---|---|
| `initial` (exists) | State | Entity starts here | C8: unique. C13: exactly one required |
| `terminal` (candidate) | State | Entity ends here; no outgoing transitions expected | Error if outgoing transition outcomes exist. Suppresses C50 |

Boundary modifiers mark the structural edges of the lifecycle graph. `initial` marks where instances enter; `terminal` marks where they leave. Together they form a **lifecycle frame** — the compiler can verify that every initial→terminal path is structurally sound.

### 2. Path modifiers — obligations on lifecycle traversal

| Modifier | Applies to | Meaning | Compile-time check |
|---|---|---|---|
| `required` / `milestone` (candidate) | State | Every instance must pass through this state | Error if any initial→terminal path bypasses it |

Path modifiers constrain what the lifecycle *must* look like, not just where it starts and ends. A `required` state means "the business process isn't complete unless the entity visited this state." This is an **all-paths** property — the compiler must verify that every possible initial→terminal path includes the required state.

### 3. Residency modifiers — constraints on time-in-state

| Modifier | Applies to | Meaning | Compile-time check |
|---|---|---|---|
| `transient` (candidate) | State | Entity cannot remain here; must transition out | Error if any row has `no transition` outcome; error if dead-end (all rows reject) |

Residency modifiers constrain how long an entity can stay in a state. A `transient` state must always be transited through — it's a processing waypoint, not a resting state. The compile-time check is that every (State, Event) pair has at least one row with a `transition` outcome reachable under some data condition.

### 4. Cardinality modifiers — constraints on event firing frequency

| Modifier | Applies to | Meaning | Compile-time check |
|---|---|---|---|
| `once` (candidate) | Event | Fires at most once per entity lifetime | Cannot be fully compile-time checked; requires runtime counter |

Cardinality modifiers constrain how many times something can happen. `once` means "this event may fire successfully at most once across the entire entity lifecycle." Unlike state modifiers, this is fundamentally a **runtime** property — the compiler cannot know from the graph alone whether a cycle allows re-firing.

### 5. Safety / behavior modifiers — constraints on event semantics

| Modifier | Applies to | Meaning | Compile-time check |
|---|---|---|---|
| `final` (candidate) | Event | Successful firing ends the process; must transition to terminal state | Error if any row transitions to a non-terminal state |
| `idempotent` (candidate) | Event | Repeated firing from the same state produces the same result | Partially compile-time: row must have `no transition` and no mutations that depend on current field values. Full check requires runtime comparison |

Safety modifiers constrain the behavioral properties of events. `final` is stronger than `once` — it says "not only does this fire once, it's the *last thing that happens*." `idempotent` is a promise to callers that firing the event again is safe.

---

## Comparable Systems Survey

### XState (v5)

**Final states:** XState's `type: 'final'` on a state node marks it as a terminal state. When a machine reaches a final state, the machine "is done" — it emits a `done` event and stops accepting transitions. In compound (nested) machines, a parent state listening for `done` can transition in response. XState validates at definition time that final states have no outgoing transitions.

**Tags and meta:** XState provides `tags` (string array) and `meta` (arbitrary object) on state nodes for attaching semantic metadata without behavioral effects. Tags like `"loading"` or `"error"` are used for UI state queries (`state.hasTag('loading')`). This is not a modifier system — it's unstructured annotation.

**Guards:** Named, reusable guard functions are referenced by string key. No structural modifiers on events (no `once`, `final`, or `idempotent` concept).

**Precept implications:**
- Strong precedent for `terminal` — XState's `type: 'final'` is exactly the lifecycle-boundary concept.
- XState's final states participate in parent-child completion semantics (Harel statechart completion). Precept has no hierarchy, so `terminal` is simpler: just "no outgoing transitions allowed."
- No precedent for path modifiers (`required`), residency modifiers (`transient`), or event modifiers. XState's model is permissive — it constrains structure but doesn't declare path obligations.

**URL:** https://stately.ai/docs/final-states

### AWS Step Functions

**Terminal states:** Step Functions workflows end when they reach a `Succeed`, `Fail`, or catch-all terminal state. These are explicit terminal markers in the ASL definition. The validator ensures terminal states have no `Next` field (outgoing transition).

**Timeouts:** `TimeoutSeconds` on Task states and overall workflow forces time-bounded residency. This is a *runtime* constraint, not a compile-time structural modifier — it requires a clock.

**Path properties:** Step Functions has no path-obligation concept. Branching is dynamic (Choice states), and there is no mechanism to declare that all paths must visit a specific state.

**Precept implications:**
- Validates the `terminal` concept: explicit lifecycle-end markers are standard infrastructure in hosted workflow systems.
- Step Functions' timeout model is a residency constraint (`transient` with a clock). Precept is event-driven with no time concept, so this maps to structural residency checks only.

**URL:** https://docs.aws.amazon.com/step-functions/latest/dg/concepts-amazon-states-language.html

### BPMN 2.0

**End events:** BPMN distinguishes Start Events, Intermediate Events, and End Events as first-class structural categories. End Events are the lifecycle-termination primitive — they are explicitly drawn and typed (None, Message, Error, Terminate, etc.). A process must have at least one End Event (analogous to Precept's C13 for `initial`).

**Intermediate events:** BPMN's intermediate events (Timer, Message, Signal, etc.) can be attached to activity boundaries or placed inline in sequence flows. Boundary events are a structural annotation on activities — declaring what interrupts are valid at that point in the process.

**Gateways and path obligations:** BPMN's Parallel Gateway (AND-split/join) enforces that all parallel paths must complete before the process can continue past the join. This is a structural path-completion requirement — the closest BPMN analogue to a `required` modifier, though it applies to path convergence, not individual activity visitation.

**Precept implications:**
- Very strong precedent for explicit `terminal` (End Event) and `initial` (Start Event) as structural categories. BPMN treats these as fundamentally different node types, not just annotations.
- BPMN's boundary events and intermediate events show that event-level structural modifiers exist in workflow modeling, but they require Harel-style hierarchy (boundary events attach to activities, which are nested containers). Precept's flat model doesn't support this.
- Path obligations in BPMN are expressed through gateway topology, not state annotations. `required` as a state modifier has no direct BPMN analogue — it would be expressed as a mandatory activity in a sequential sub-process.

**URL:** https://www.omg.org/spec/BPMN/2.0.2/

### Stateless (.NET)

**No built-in modifiers:** Stateless has no `final`, `initial`, or modifier concept on states or triggers. The API is purely programmatic — `machine.Configure(State.Closed).Permit(...)`. There is no compile-time validation of graph properties.

**Emergent patterns:** Stateless users implement terminal semantics by convention: a state with no configured `Permit` calls acts as a terminal state. Some users add a `CanFire(trigger)` check that returns false for all triggers from terminal states — a runtime guard, not a declared modifier.

**Precept implications:**
- The absence of modifiers in Stateless is itself informative: as a .NET library consumed by developers, Stateless assumes the programmer manages graph semantics. Precept targets a broader audience (domain experts reviewing rules) and makes a prevention-first promise. Declared modifiers fill a gap that Stateless leaves to convention.

**URL:** https://github.com/dotnet-state-machine/stateless

### TLA+ / Alloy — Formal Specification

**TLA+:** Temporal properties in TLA+ are specified as formulas over execution traces. Liveness properties (`<>P` — "eventually P") express path obligations: "every execution trace must eventually reach state P." Safety properties (`[]P` — "always P") express invariants. TLC (the model checker) verifies these by exhaustive state enumeration.

A `terminal` state in TLA+ is expressed as `Spec => <>Terminated` — "the specification implies eventual termination." A `required` state is `Spec => <>InReview` — "every trace visits InReview." A `transient` state is `Spec => [](InProcessing => <>~InProcessing)` — "whenever in Processing, eventually not in Processing."

**Alloy:** Alloy specifies structural properties as first-order relational constraints. Reachability is expressed as transitive closure: `all s: State | s in InitialState.*transitions` (all states are reachable). Terminal states are states with no outgoing transitions: `no s.transitions`. Required states are expressed as path membership: `all p: Path | RequiredState in p.visited`. Alloy's Analyzer checks these by bounded model finding.

**Precept implications:**
- TLA+ and Alloy show that every candidate modifier maps to a precisely defined formal property. This is reassuring — the concepts are well-understood in formal methods.
- The key distinction: TLA+ and Alloy verify properties over *all possible execution traces* (model checking / bounded model finding). Precept's compile-time analysis operates over the *declared graph structure* (BFS/DFS on transition rows). The two are related but not identical — Precept's graph analysis is an overapproximation. A `when` guard might make a transition unreachable in practice, but the graph still shows the edge.
- This means compile-time provability of path modifiers (`required`) depends on whether the check considers `when` guards or treats all edges as traversable. The sound approach: treat all edges as traversable for structural checks (overapproximate reachability), which means `required` checks provide a structural guarantee but cannot account for guard-dependent path selection.

**URLs:** https://lamport.azurewebeb.net/tla/tla.html, https://alloytools.org/documentation.html

### Temporal.io

**Workflow completion:** A Temporal workflow completes when the workflow function returns. There is no explicit "terminal state" declaration — the function's return is the terminal event. Workflow completion triggers child cancellation and triggers the parent's `ChildWorkflowExecutionCompleted` event.

**Activity lifecycle:** Temporal activities have built-in heartbeat timeout and schedule-to-close timeout — runtime-enforced residency constraints. Activities that fail to heartbeat are cancelled and potentially retried.

**Once semantics:** Temporal provides exactly-once execution guarantees for activities through its event-sourced replay mechanism. This is infrastructure-level `once`, not a language-level declaration.

**Precept implications:**
- Temporal's completion model is procedural (function return), not declarative (state marker). It doesn't provide a direct analogue for `terminal`.
- Temporal's activity timeouts are the closest analogue to `transient`, but they are runtime-enforced with a clock. Precept's event-driven model can only check structural residency (no `no transition` outcomes), not time-bounded residency.
- Temporal's exactly-once guarantee confirms that `once` is an infrastructure concern, not a declaration concern. A precept that needs `once` semantics needs runtime tracking.

**URL:** https://docs.temporal.io/workflows

### UML State Machines

**Final states:** UML defines a `FinalState` pseudostate — a filled circle inside a circle. Reaching a FinalState completes the enclosing region (or the entire state machine for the top-level region). Like XState's `type: 'final'`, this triggers parent-level completion events. UML requires that FinalState has no outgoing transitions.

**Initial pseudostate:** The filled-circle `InitialPseudostate` marks the default entry point for a region. Exactly one per region. This is the direct ancestor of Precept's `initial` keyword.

**History pseudostates:** Shallow and deep history pseudostates remember the last active substate of a composite state. These require hierarchical state machines — not applicable to Precept's flat model.

**Junction and choice pseudostates:** Junction (static) and choice (dynamic) pseudostates implement branching. Junction is resolved at compile time; choice is resolved at runtime based on guards. Precept's `when` guard model is equivalent to choice pseudostates.

**Precept implications:**
- UML's `FinalState` is the canonical formal precedent for Precept's `terminal`. The semantics are well-defined: no outgoing transitions, triggers completion.
- UML's `InitialPseudostate` directly maps to Precept's `initial` — Precept's implementation is already UML-aligned.
- UML has no path-obligation, residency, or cardinality concepts on states/events. State machine theory historically separates behavioral specification (the machine) from property verification (temporal logic, model checking). Modifiers like `required` and `transient` come from the verification side, not the machine specification side.

**URL:** https://www.omg.org/spec/UML/2.5.1/

### Cedar / OPA — Policy Systems

**Cedar:** Cedar policies are `permit` or `forbid` statements with optional `when`/`unless` guards. There is no lifecycle, no state machine, and no structural modifier concept. Policy termination is determined by the evaluation engine (first-match or default-deny), not by policy authors.

**OPA / Rego:** Rego rules compose via conjunction. There is no explicit "terminal rule" or lifecycle concept. Policy evaluation terminates when all applicable rules have been evaluated.

**Precept implications:**
- Policy systems have no analogue for structural modifiers because they have no lifecycle graph. The concepts are orthogonal.
- This confirms that structural modifiers are a *state-machine-specific* concern, not a general constraint-system concern.

**URLs:** https://docs.cedarpolicy.com/, https://www.openpolicyagent.org/docs/latest/

---

## Cross-System Pattern Summary

| Modifier concept | XState | Step Functions | BPMN | Stateless | TLA+/Alloy | Temporal | UML | Cedar/OPA |
|---|---|---|---|---|---|---|---|---|
| **Terminal / final state** | `type: 'final'` | `Succeed`/`Fail` | End Event | Convention only | `<>Terminated` | Function return | `FinalState` | N/A |
| **Initial state** | `initial:` | `StartAt` | Start Event | `initialState` | `Init` | Workflow entry | `InitialPseudostate` | N/A |
| **Required / milestone** | — | — | Parallel gateway join | — | `<>InState` | — | — | — |
| **Transient / pass-through** | — | `Pass` state | — | — | `[]( In => <>~In )` | Activity timeout | — | — |
| **Once (event cardinality)** | — | — | — | — | Count constraint | Exactly-once infra | — | — |
| **Final (event)** | — | — | Terminate End Event | — | — | — | — | — |
| **Idempotent (event)** | — | — | — | — | — | Activity retry safety | — | — |

**Key finding:** Terminal/final states are the only modifier concept with broad cross-system precedent. Every workflow and state machine system surveyed — XState, Step Functions, BPMN, UML — has explicit terminal state support. Path modifiers, residency modifiers, and event modifiers have at best partial precedent, and that precedent usually involves runtime enforcement or formal verification, not compile-time declaration.

---

## Precept Fit Analysis

For each candidate modifier, I evaluate against Precept's design principles (numbered per `PreceptLanguageDesign.md`), compile-time provability, runtime implications, and consistency with the `initial` pattern.

### `terminal` — State Boundary Modifier

**Syntax:**
```precept
state Closed terminal
state Draft initial, UnderReview, Approved, Closed terminal
```

**Design principle alignment:**

| Principle | Fit | Notes |
|---|---|---|
| #1 (Deterministic, inspectable) | Strong | Terminal is a static graph property — no hidden state |
| #2 (English-ish, not English) | Strong | `terminal` reads naturally: "state Closed terminal" |
| #5 (Data truth vs movement truth) | Strong | Terminal constrains movement truth (lifecycle boundary), same category as `initial` |
| #8 (Compile-time-first) | Strong | Fully provable from declared structure: state has outgoing transition rows → error |
| #9 (Tooling drives syntax) | Strong | Keyword modifier, same position as `initial`, predictable IntelliSense |
| #12 (AI is first-class consumer) | Strong | Keyword-anchored, deterministic parse, clear semantics |
| #13 (Keywords for domain) | Strong | `terminal` is a domain concept (lifecycle endpoint), not math |

**Compile-time provability:** Fully provable. The check is:
- If state `S` is marked `terminal`, error if any `from S on E ... -> transition T` row exists (outgoing transition to another state).
- `no transition` outcomes are allowed on terminal states (the entity stays in the terminal state).
- `reject` outcomes are allowed (the event is received but refused — the entity stays terminal).
- `set` mutations in `no transition` or `reject` rows are allowed — a terminal state can still update data.

The check is structurally identical to how the analyzer already identifies terminal states: `!statesWithOutgoing.Contains(state)`. The difference is that `terminal` makes this a *declared* property with error-on-violation semantics rather than an *inferred* property with no diagnostic.

**Runtime implications:** None. `terminal` is a compile-time-only property. No new runtime state, no new fields on `PreceptInstance`, no changes to the fire pipeline.

**Consistency with `initial`:** Excellent parallel.

| Aspect | `initial` | `terminal` |
|---|---|---|
| Keyword position | After state name | After state name |
| Cardinality | Exactly one required (C13) | Zero or more allowed |
| Compile-time check | C8: duplicate; C13: missing | Error if outgoing transitions exist |
| Runtime effect | Sets `InitialState` on definition | None |
| Parser representation | `InitialFlags` boolean array | Would add `TerminalFlags` boolean array (or similar) |

**Cardinality difference:** `initial` is required-unique (exactly one). `terminal` should be optional-many — a precept may have zero terminal states (a cyclic workflow) or multiple (several possible endpoints). This is the natural split: entry is singular, exits are plural. BPMN, UML, and XState all allow multiple terminal states.

**Interaction with C50 (dead-end states):** A state marked `terminal` would be *excluded* from C50 analysis. Currently, C50 fires on "non-terminal state has outgoing rows but none can reach another state." With `terminal`:
- Declared `terminal` states are never C50 candidates (they have no outgoing transitions by definition).
- Non-terminal states with all-failing outgoing rows still get C50 — the warning is now stronger evidence of a real problem.

**Interaction with C48 (unreachable states):** No change. A terminal state that is unreachable from initial would still get C48. Being terminal doesn't exempt a state from reachability — an unreachable terminal is a definition problem.

**Design system interaction:** The visual treatment already assigns terminal states a distinct color (`#C4B5FD`). A declared `terminal` modifier would align the authoring surface with the visual surface — what the diagram shows, the DSL declares.

### `required` / `milestone` — State Path Modifier

**Syntax options:**
```precept
state UnderReview required
# or
state UnderReview milestone
```

**Design principle alignment:**

| Principle | Fit | Notes |
|---|---|---|
| #1 (Deterministic, inspectable) | Moderate | Requires all-paths analysis; result depends on graph structure |
| #8 (Compile-time-first) | Moderate | Provable from graph structure but computationally expensive for complex graphs; `when` guards make the analysis approximate |
| #9 (Tooling drives syntax) | Moderate | Same keyword-modifier position as `initial`/`terminal`, but diagnostic messages must explain multi-path violations clearly |
| #12 (AI is first-class consumer) | Moderate | Clear semantics but AI would need to understand path enumeration to author correctly |

**Compile-time provability:** Partially provable with caveats.

The check: enumerate all paths from initial to all terminal states. Error if any path bypasses the `required` state. This is a graph traversal problem — decidable and polynomial for DAGs, but potentially exponential for cyclic graphs (which Precept allows — a state can transition back to a previous state).

Complications:
1. **Cycle handling:** A cyclic graph has infinitely many paths. The analysis must check whether *every finite path from initial to terminal* visits the required state. This is equivalent to checking that the required state is a *dominator* of all terminal states in the graph (every path from initial to any terminal state passes through it). Dominator analysis is O(V + E) using Lengauer-Tarjan — tractable.
2. **Guard-dependent paths:** A `when` guard might make a transition unreachable in practice, creating a graph edge that is structurally present but never taken. The sound approach: treat all edges as traversable (overapproximate reachability). This means the compiler might report "path bypasses required state" for a path that guards make impossible. This is conservative (no false negatives) but may produce false positives.
3. **Dynamic routing:** If a precept uses data-dependent guards to route through different states, the compiler sees all edges and may flag paths that are data-impossible. The `required` guarantee becomes: "barring guard-dependent routing, every structural path visits this state."

**Runtime implications:** None if the check is purely compile-time. However, a runtime "has this instance visited the required state?" flag would enable a runtime API check (e.g., `instance.HasVisited("UnderReview")`). This is additional runtime state — a per-state boolean flag.

**Keyword choice — `required` vs `milestone`:**
- `required` is more precise ("must visit") but collides with the common English use of "required" for mandatory fields.
- `milestone` is evocative but has softer semantics — a milestone in project management is a checkpoint, not necessarily a mandatory pass-through.
- For Precept's domain audience, `required` is clearer despite the collision. The collision is contextual — `required` on a state means "required visitation," not "required value."

**Precept fit verdict:** Interesting but complex. The dominator-based check is tractable, but guard-dependent false positives weaken the guarantee. The value is real — domain experts want to say "every claim must pass through Assessment" — but the implementation needs careful design of the diagnostic message to communicate what is guaranteed and what is approximate.

### `transient` — State Residency Modifier

**Syntax:**
```precept
state Processing transient
```

**Design principle alignment:**

| Principle | Fit | Notes |
|---|---|---|
| #1 (Deterministic, inspectable) | Strong | Structural check on transition row outcomes |
| #8 (Compile-time-first) | Moderate | Can check that at least one outgoing row has `transition` outcome, but cannot guarantee the guard will evaluate to true |
| #5 (Data truth vs movement truth) | Strong | `transient` constrains movement truth |

**Compile-time provability:** Partially provable.

The structural check: for a `transient` state, error if:
- Any outgoing (State, Event) pair has *only* `no transition` or `reject` outcomes (no successful transition exists).
- No outgoing transition rows exist at all (the state is accidentally terminal).

But the compiler cannot guarantee that at least one outgoing transition's `when` guard will evaluate to `true` at runtime. A transient state might still be a runtime dead-end if all guards happen to be false for the current data.

**Runtime implications:** A strict `transient` guarantee (the entity *will* leave this state) requires either:
- Runtime enforcement — reject any operation that would leave the entity in a transient state without transitioning (but this breaks the current pipeline, which allows `no transition`).
- Time-bounded enforcement — force a transition within a timeout (but Precept has no time model).

Without runtime enforcement, `transient` is a weaker guarantee: "a successful outgoing transition structurally exists" but not "the entity will definitely leave."

**Precept fit verdict:** Moderate. The compile-time check has value (catching accidentally terminal processing states) but the runtime gap undermines the promise. If `transient` means "the author intends this to be temporary," that's useful for documentation and diagnostics. If it means "the engine guarantees the entity will leave," that's undeliverable without a time model.

### `final` — Event Boundary Modifier

**Syntax:**
```precept
event Close final
```

**Design principle alignment:**

| Principle | Fit | Notes |
|---|---|---|
| #8 (Compile-time-first) | Moderate | Can check that all transition outcomes target terminal states. Requires `terminal` to be defined first |
| #7 (Self-contained rows) | Weak | `final` on the event creates an implicit constraint that affects all rows for that event across all states — breaks row independence |

**Compile-time provability:** Depends on `terminal` existing first. The check: for every `from S on FinalEvent ... -> transition T`, `T` must be marked `terminal`. This creates a dependency chain: `final` events reference `terminal` states.

**Runtime implications:** None beyond what `terminal` provides. If `final` event fires and transitions to a terminal state, the entity is at its lifecycle endpoint — same as without the modifier.

**Precept fit verdict:** Weak-to-moderate. `final` is derivable from `terminal` + convention. If all rows for `Close` transition to `terminal` states, the event is effectively final. A modifier saves one inference step but adds a keyword for what is already structurally visible. Moreover, `final` on an event constrains all rows for that event across all source states — this is a cross-row constraint that breaks Principle #7 (self-contained rows).

### `once` — Event Cardinality Modifier

**Syntax:**
```precept
event Approve once
```

**Design principle alignment:**

| Principle | Fit | Notes |
|---|---|---|
| #1 (Deterministic, inspectable) | Weak | Requires runtime counter; the inspector must track how many times an event has fired |
| #8 (Compile-time-first) | Weak | Cannot be proven from graph structure alone; requires runtime enforcement |
| #12 (AI legibility) | Moderate | Clear intent, but runtime tracking is hidden state |

**Compile-time provability:** Not provable from graph structure. A cyclic graph could re-enter a state from which the `once` event was previously fired. The compiler can detect *some* cases: if the event appears only in states that transition to terminal states, it structurally fires at most once. But in general, `once` requires runtime tracking.

**Runtime implications:** Significant.
- New per-event counter on `PreceptInstance` (or a set of "events already fired").
- Fire pipeline adds a new check: if event has `once` modifier and has been previously fired, outcome is `Rejected` (or a new outcome kind?).
- The counter must persist across serialization/deserialization cycles.
- The inspect result must include "this event has already been fired" information.

**Precept fit verdict:** Weak. `once` is a behavioral property that requires runtime state. It conflicts with Precept's compile-time-first philosophy and introduces hidden state that the author cannot see in the `.precept` file. The same guarantee can be expressed structurally today: if `Approve` should fire only from `UnderReview` and transitions to `Approved` (a terminal or forward state), the graph ensures single execution. When the graph doesn't ensure it, a boolean field (`IsApproved`) with a `when` guard achieves the same result with explicit, visible state.

### `idempotent` — Event Safety Modifier

**Syntax:**
```precept
event Acknowledge idempotent
```

**Design principle alignment:**

| Principle | Fit | Notes |
|---|---|---|
| #1 (Deterministic, inspectable) | Weak | Idempotency is a property of the mutation set + guard set; proving it from the AST is complex |
| #8 (Compile-time-first) | Weak | Requires reasoning about mutation commutativity and guard stability |

**Compile-time provability:** Extremely limited. A truly idempotent event must satisfy: firing it twice from the same state with the same data produces the same result. This requires proving that:
1. The `when` guard evaluates the same way after the mutations as before (stability).
2. The mutations are idempotent (e.g., `set X = 5` is idempotent; `set X = X + 1` is not).
3. The transition outcome is the same (easy if `no transition`; harder if `transition T` because re-firing from state T may have different rows).

The compiler could check a *subset*: if all mutations are constant assignments (`set X = literal`) and the outcome is `no transition`, the event is trivially idempotent. But for realistic events with computed mutations or state transitions, idempotency is undecidable without full data-dependent reasoning.

**Runtime implications:** If enforced, requires either:
- Runtime de-duplication via event log (track fired events and skip re-fires).
- Runtime comparison (apply mutations, compare result to current state, skip if no change).

Both add significant runtime complexity.

**Precept fit verdict:** Reject. Idempotency is a behavioral property that the compiler cannot generally prove and that requires significant runtime infrastructure. It is better expressed as a design convention documented in comments, or as explicit guard patterns in the DSL (`when IsAcknowledged == false`).

---

## State vs Event Asymmetry

The analysis reveals a clear structural asymmetry between state modifiers and event modifiers.

### Why state modifiers are stronger candidates

**State modifiers constrain graph-structural roles.** A state's role in the lifecycle (boundary, waypoint, milestone) is determined by its position in the transition graph — which transitions point TO it, which transitions point FROM it, and which states it connects. This is fully visible in the declared structure.

**State properties are provable from declared transitions.** The compiler already computes these properties (reachability, terminality, dead-end-ness). A state modifier asserts what the compiler can verify: the declared intent matches the computed structure.

**State modifiers follow the `initial` precedent.** `initial` proves the pattern works: a keyword modifier on a state declaration, validated at compile time from the declared graph, with no runtime implications. `terminal`, `required`, and `transient` all follow the same pattern — they are assertions about graph-structural properties that the compiler can check.

### Why event modifiers are weaker candidates

**Event modifiers constrain behavioral properties.** Whether an event fires once, fires last, or is idempotent depends on the entity's *data state* at firing time and the *history of prior fires* — not just the graph structure. These are properties of execution traces, not graph topology.

**Event properties require cross-entity or runtime reasoning.** `once` needs a runtime counter. `idempotent` needs mutation commutativity analysis. `final` needs `terminal` states to be defined first and constrains all rows for the event across all source states. These are either runtime-dependent or create implicit cross-row dependencies.

**Event modifiers break row independence (Principle #7).** A modifier on an event creates an invisible constraint that applies to every transition row where that event appears. The row author cannot read the constraint from the row — they must look up the event declaration. This weakens the self-contained row property that is fundamental to Precept's readability model.

### Frank's #13 taxonomy

From the constraint composition domain research, the taxonomy distinguishes:
- **Keywords for single-entity, finite-vocabulary properties** — properties of the precept's own declared structure that can be enumerated and verified at compile time. Example: `initial`, `terminal`, `nullable`, `default`.
- **Predicates for cross-entity, open-ended logic** — properties that depend on runtime data, external state, or unbounded computation. Example: `once` (requires runtime counter), `idempotent` (requires mutation analysis).

State modifiers fall cleanly into the keyword category. Event modifiers largely fall into the predicate category — they express properties that are better served by explicit guard logic than by structural keywords.

---

## Recommendation Tiers

### Tier 1 — Strong Candidates

**`terminal` on states**

- **Why:** Compile-time provable, clear `initial` parallel, fills a real gap (declared intent vs inferred structure), zero runtime cost, suppresses false-positive C50 warnings, aligns with universal cross-system precedent (XState, BPMN, UML, Step Functions).
- **Compile-time check:** Error if any outgoing `transition T` row exists for a terminal state.
- **Allowed on terminal states:** `no transition` rows, `reject` rows, `set` mutations, entry/exit actions.
- **Cardinality:** Zero or more terminal states per precept.
- **Implementation scope:** Parser extension (modifier position after state name, same as `initial`). New C-code in `DiagnosticCatalog`. Analysis update to exclude terminal states from C50 and to report terminal-state violations.
- **Recommendation:** Strong candidate for proposal. Lowest-risk, highest-clarity modifier.

### Tier 2 — Interesting but Complex

**`required` / `milestone` on states**

- **Why:** Real domain value (business must-visit states), tractable dominator-based check, but guard-dependent false positives weaken the guarantee.
- **Blocking questions:** (1) How to communicate the overapproximation to authors — "required" is a strong word for a structural guarantee that `when` guards can circumvent at runtime. (2) Keyword choice — `required` collides with field-required semantics; `milestone` is softer. (3) Requires `terminal` to exist first (required states must be on every initial→terminal path, so terminal states must be identifiable).
- **Recommendation:** Defer until `terminal` ships and the diagnostic message design can be validated with real users.

**`transient` on states**

- **Why:** Catches structural dead-ends in processing states, but the guarantee is weaker than the word implies (cannot guarantee the entity *will* leave, only that a successful transition *exists*).
- **Blocking questions:** (1) What does the compiler actually promise? "A transition row with a `transition` outcome exists" is much weaker than "the entity will not remain here." (2) Runtime expectation gap — users who read `transient` may expect time-bounded enforcement. Precept has no time model.
- **Recommendation:** Defer. The current C50 diagnostic covers the strongest version of this check (all-reject/no-transition dead-end detection). `transient` adds marginal value over C50 with significant semantic-promise risk.

### Tier 3 — Reject or Defer

**`final` on events**

- **Why rejected:** Derivable from `terminal` + row inspection. Creates cross-row constraint that breaks Principle #7 (self-contained rows). Adds a keyword for a property that is already visible from the transition table.
- **Alternative:** Author convention: "events named Close/Complete/Finalize transition to terminal states."

**`once` on events**

- **Why rejected:** Cannot be compile-time proven for general graphs. Requires runtime counter state. Achievable today via boolean field + `when` guard, which is explicit, visible, and consistent with the existing language model.
- **Alternative:** `field IsApproved as boolean default false` + `from UnderReview on Approve when not IsApproved` + `-> set IsApproved = true`.

**`idempotent` on events**

- **Why rejected:** Undecidable in general. Narrow compile-time check (constant assignments + no transition) covers only trivial cases. Runtime enforcement adds significant complexity. Better expressed as a design convention.
- **Alternative:** Author writes explicit guard: `when State == "Acknowledged"` on the acknowledgment row, with `no transition` outcome. The row is self-documenting.

---

## Interaction with Existing Diagnostics

### Summary table

| Diagnostic | Current behavior | With `terminal` | With `required` | With `transient` |
|---|---|---|---|---|
| **C48** (unreachable) | Warning: state unreachable from initial | **No change.** Terminal + unreachable = definition problem (how can the entity end there if it can't reach it?) | **No change.** Required + unreachable = definition error (contradictory: required but unreachable) | **No change.** Transient + unreachable = definition problem |
| **C50** (dead-end) | Warning: non-terminal state has outgoing rows that all reject/no-transition | **Suppressed for terminal states** (they have no outgoing rows by definition). **Strengthened for others** — if a non-terminal state is a dead-end, and `terminal` exists in the language, the author clearly didn't intend it as an endpoint | No direct change | **Upgraded to error** for transient states: a transient dead-end is a proven contradiction (transient = "must leave" + dead-end = "can't leave") |
| **C51** (reject-only pair) | Warning: every row for (State, Event) rejects | No direct change (reject rows are allowed on terminal states, but C51 fires on specific state+event pairs) | No change | No change |
| **C52** (event never succeeds) | Warning: event can never succeed from any reachable state | No direct change | No change | No change |
| **New diagnostic** | — | Error: terminal state has outgoing `transition` rows | Error: required state is bypassed on at least one initial→terminal path | Error: transient state has no outgoing row with `transition` outcome |

### C50 interaction detail

The C50 interaction with `terminal` deserves deeper analysis because it's the primary diagnostic improvement:

**Current C50 logic** (`PreceptAnalysis.cs`):
```csharp
var terminalStates = allStateNames
    .Where(state => !statesWithOutgoing.Contains(state))
    .ToArray();

var deadEndStates = allStateNames
    .Where(state => !terminalStates.Contains(state, StringComparer.Ordinal))
    .Where(/* has outgoing rows */)
    .Where(/* all rows reject or no-transition */)
    .ToArray();
```

C50 already excludes terminal states (states with no outgoing rows). The issue is that a state could have outgoing rows that all fail — making it a *behavioral* dead-end — but the author intended it as terminal and simply hasn't cleaned up the dead rows.

With `terminal`:
1. States marked `terminal` are validated to have **no outgoing `transition` outcomes** (new error diagnostic).
2. States marked `terminal` are excluded from C50 analysis (same as inferred terminal states today).
3. Non-terminal states that are dead-ends (C50) now have stronger diagnostic language: "State 'X' appears to be a dead end. If it is intentionally terminal, mark it with `terminal`. Otherwise, fix the outgoing transitions."

### C48 interaction — unreachable terminal contradiction

A state that is both `terminal` and unreachable (C48) is a strong definition smell — the author declared a lifecycle endpoint that no execution path can reach. This could be:
- **New compound diagnostic:** "Terminal state 'X' is unreachable from initial — no entity can ever end here." Higher severity than C48 alone.
- **Or:** Let C48 stand as-is (warning). The author sees both pieces of information: it's unreachable, and it's terminal.

The simpler approach (let C48 stand) is architecturally cleaner and avoids diagnostic interaction complexity.

### Potential new diagnostics

| Code | Trigger | Severity | Message pattern |
|---|---|---|---|
| **C56** (tentative) | Terminal state has outgoing `transition` outcome | Error | "State '{State}' is marked terminal but has a transition to '{Target}' — terminal states may not have outgoing transitions" |
| **C57** (tentative) | Required state bypassed on initial→terminal path | Error | "State '{State}' is marked required but path {Initial}→…→{Terminal} bypasses it" |
| **C58** (tentative) | Transient state has no successful outgoing transition | Error | "State '{State}' is marked transient but has no outgoing transition that succeeds" |

---

## Where This Fits in the Research Taxonomy

This research creates a **new domain** in the language research taxonomy. It is not a natural extension of any existing domain:

- **Not constraint composition (#8, #13, #14)** — constraint composition is about value constraints (invariants, field-level bounds, named rules). Structural modifiers constrain graph topology, not field values.
- **Not static reasoning expansion** — static reasoning is about proving properties from existing declarations (contradiction detection, satisfiability). Structural modifiers *add new declarations* that create new provable properties.
- **Not entity-modeling surface (#17, #22)** — entity modeling is about what data an entity has (fields, computed values, statelessness). Structural modifiers are about what the lifecycle graph looks like.
- **Closest relative: state machine expressiveness** — the existing `state-machine-expressiveness.md` reference covers what graph features Precept could gain (hierarchy, parallelism). Structural modifiers are a different axis — they don't add new graph features, they add *annotations on existing graph features* that enable stronger compile-time reasoning.

**Recommended taxonomy placement:** New domain "Structural lifecycle modifiers" in the expressiveness research folder, with `state-machine-expressiveness.md` and `static-reasoning-expansion.md` as theory/reference companions.

| Domain | Open proposals | Primary grounding | Theory / reference companion | Notes |
|---|---|---|---|---|
| Structural lifecycle modifiers | — | [expressiveness/structural-lifecycle-modifiers.md](./structural-lifecycle-modifiers.md) | [references/state-machine-expressiveness.md](../references/state-machine-expressiveness.md), [references/static-reasoning-expansion.md](../references/static-reasoning-expansion.md) | Horizon domain. `terminal` is the strong candidate. |

---

## Dead Ends and Rejected Directions

### State-type system (rejected)

**Idea:** Instead of keyword modifiers, make state categories a type system — `state Closed as terminal`, `state Processing as transient`, with a fixed set of state types that carry behavioral contracts.

**Why rejected:** This turns states into typed entities with distinct behavioral contracts per type. The parser and type checker must now track state types and enforce different rules per type. This is architecturally invasive and breaks the current flat model where all states are structurally identical (just names). Keyword modifiers add information without changing the fundamental model. Types change the model.

### Event lifecycle annotations (rejected)

**Idea:** Full event lifecycle annotations — `event Approve preconditions [R1, R2]`, `event Close postconditions [in terminal]`.

**Why rejected:** This is general pre/postcondition specification on events, equivalent to attaching Hoare-logic contracts. Precept already has this: `on Event assert ...` is the precondition; state asserts and invariants are the postconditions. Adding more annotation layers duplicates existing constructs.

### Temporal properties (deferred indefinitely)

**Idea:** Time-bounded residency (`state Processing timeout 30m`), deadline states, SLA modifiers.

**Why rejected for Precept:** Precept is event-driven with no clock model. Time-bounded properties require a runtime clock, a polling mechanism for timeout detection, and a new category of "system-generated events" (timeout events). This is a fundamental model extension, not a modifier. Workflow orchestrators (Temporal, Step Functions) handle time; Precept handles data integrity.

### Hierarchical state modifiers (deferred indefinitely)

**Idea:** Modifiers that reference other states — `state Review parent of TechnicalReview, LegalReview`.

**Why rejected for Precept:** Precept's flat state model is a deliberate design choice (Principle #9, tooling-friendly; `state-machine-expressiveness.md` analysis: hierarchical states are HIGH cost). Modifiers that reference other states introduce hierarchy through the back door.

### `absorbing` / `sink` state modifier (merged into `terminal`)

**Idea:** A state that accepts events but never transitions — an event sink.

**Why merged:** This is a subset of `terminal`. A terminal state with `reject` or `no transition` rows for specific events is already an absorbing state. No separate keyword needed.

---

## Open Questions for Proposal Phase

If `terminal` advances to a proposal, these questions need answers:

1. **Interaction with `edit` declarations:** Can a terminal state have `in Terminal edit` declarations? (Likely yes — a closed entity might still allow annotation updates.)

2. **Interaction with state entry/exit actions:** Can a terminal state have `to Terminal -> set ClosedDate = ...` entry actions? (Likely yes — entry actions run when transitioning *into* the state.)

3. **Grammar extension:** Does `terminal` go in the same modifier position as `initial` (after state name, before comma)? If a state can be both `initial` and `terminal` (edge case: single-state precept), what's the declaration order?

4. **Stateless precepts:** Should `terminal` be rejected in stateless precepts (no states to modify)? (Yes — trivially, since there are no state declarations.)

5. **C50 message upgrade:** Should C50's message text change when `terminal` is available? ("Mark this state as `terminal` if this is intentional" as guidance.)

---

## Key References

- Harel, "Statecharts: A visual formalism for complex systems" (1987)
- UML Superstructure Spec, §14.2.3 FinalState, §14.2.3 InitialPseudostate
- XState v5 — final states: https://stately.ai/docs/final-states
- BPMN 2.0 — end events: https://www.omg.org/spec/BPMN/2.0.2/
- AWS Step Functions — states: https://docs.aws.amazon.com/step-functions/latest/dg/concepts-amazon-states-language.html
- Temporal workflows: https://docs.temporal.io/workflows
- Lengauer & Tarjan, "A Fast Algorithm for Finding Dominators in a Flowgraph" (1979) — dominator analysis for `required`
- Lamport, *Specifying Systems* (2002) — temporal properties (liveness, safety) for formal modifier semantics
- `docs/PreceptLanguageDesign.md` — design principles numbered #1–#13
- `src/Precept/Dsl/PreceptAnalysis.cs` — existing terminal/dead-end/unreachable analysis
- `src/Precept/Dsl/DiagnosticCatalog.cs` — C48, C49, C50, C51, C52 constraints
- `research/language/references/state-machine-expressiveness.md` — existing theory companion
- `research/language/references/static-reasoning-expansion.md` — adjacent static-reasoning research
- `research/language/expressiveness/constraint-composition-domain.md` — Frank's #13 keyword-vs-predicate taxonomy

---

## Second Pass — Expanded Modifier Space

**Date:** 2026-04-10
**Trigger:** Shane's review of first pass — requested deeper exploration of event modifiers, new coverage of field modifiers, and additional state modifiers. Key framing shift: modifiers are not just constraints. A modifier can declare a property that changes compiler analysis, tooling presentation, runtime behavior, documents authorial intent, or enables/disables other features.

### Context Shift: From Constraint to Declaration

The first pass evaluated modifiers primarily as *constraints* — structural rules the compiler enforces. Shane's feedback expands the frame. A modifier on a declaration is any keyword that adds a **provable structural property** to the entity, where "provable" means "somewhat verifiable from declared structure, guards aside." The property can serve any of these roles:

1. **Constraint** — the compiler errors if declared structure violates the property (e.g., `terminal` errors on outgoing transitions).
2. **Intent declaration** — the author states what they intend, and the compiler cross-checks against structure (e.g., `entry` asserts the event only fires from Initial).
3. **Tooling directive** — the modifier changes how IntelliSense, preview, diagrams, or MCP present the entity (e.g., `sensitive` causes masking).
4. **Analysis enabler** — the modifier unlocks new compiler checks that are meaningless without the declaration (e.g., `required` enables bypass-path detection).
5. **Feature gate** — the modifier enables or restricts other language features for that entity (e.g., a `computed` field disables `set` targeting).

The `initial` precedent serves roles 1 (C8: unique, C13: required), 2 (author declares "this is the entry point"), 3 (diagram marks the start state), and 4 (enables reachability analysis — without initial, BFS has no root).

This second pass evaluates candidates across all five roles, not just role 1.

---

### Event Modifiers — Beyond Constraints

The first pass concluded that event modifiers are weak because they "constrain behavioral properties" that require runtime state. That conclusion was too narrow — it evaluated events as behavior-constrained entities rather than as **structural participants in the lifecycle graph**. Events have graph-provable structural properties beyond their firing behavior:

- **Source scope** — which states does the event fire from?
- **Outcome shape** — does it always transition, never transition, or both?
- **Target scope** — where does it route to?
- **Reversibility** — can the entity return to the source after this event?
- **Coverage** — is the event handled everywhere, or only in specific states?

These are properties of the **declared transition rows**, not of runtime execution traces. They are provable from the same graph structure the analyzer already computes.

#### 1. `entry` — Event fires only from the initial state

**Syntax:**
```precept
event Submit entry with Applicant as string, Amount as number
```

**What it means:** This event's transition rows only appear with the initial state as source. It is the lifecycle's intake event — the one that populates initial data and moves the entity out of its starting state.

**Compile-time check:** Error if any `from S on Submit` row exists where `S` is not the initial state (accounting for `from any` expansion — `from any` would include the initial state but also other states, which violates `entry`).

**Runtime implications:** None. Purely a compile-time and tooling property.

**What it enables:**
- **Compiler:** Catches misplaced transition rows — if someone adds `from UnderReview on Submit`, the compiler errors because Submit is marked `entry`.
- **Tooling:** IntelliSense only suggests `entry` events when authoring rows from the initial state. Preview and diagram highlight the intake event distinctly. MCP `inspect` can label `entry` events as "intake."
- **AI authoring:** An AI generating transition rows for an `entry` event knows to always use `from <InitialState> on <Event>`.

**Design principle served:** #8 (compile-time first — structural check), #4 (locality — the event declaration tells you its role), #12 (AI legibility — the keyword communicates intent).

**Grounding in samples:** In InsuranceClaim, `Submit` fires only from `Draft`. In LoanApplication, `Submit` fires only from `Draft`. In HiringPipeline, `SubmitApplication` fires only from `Draft`. In RefundRequest, `Submit` fires only from `Draft`. In WarrantyRepairRequest, `Submit` fires only from `Draft`. Every sample has exactly one intake event, and it always fires only from the initial state. This is the most consistent structural pattern across all 24 samples.

**Fit verdict: Strong.** Fully compile-time provable. Captures the single most common event pattern in the sample corpus. Zero runtime cost. Clear `initial` parallel (initial marks the entry state; `entry` marks the entry event).

---

#### 2. `advancing` — Every successful outcome is a state transition

**Syntax:**
```precept
event Approve advancing with Amount as number
```

**What it means:** When this event fires successfully (not rejected, not unmatched), it always changes the entity's state. No `no transition` outcomes are allowed — the event's purpose is to move the lifecycle forward.

**Compile-time check:** Error if any row for this event has a `no transition` outcome. `reject` outcomes are allowed (the event can be refused, but if it succeeds, it must advance).

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches rows where the author forgot to include a `transition` outcome on a routing event.
- **Tooling:** Diagram draws advancing events as state-changing arrows exclusively. Inspector labels them as "will change state if successful." Preview can show "this event always advances the lifecycle."
- **Authoring intent:** Distinguishes routing events (Approve, Deny, Submit) from data-mutation events (LogRepairStep, VerifyDocuments). This is a fundamental domain distinction — some events exist to move the process forward, others exist to accumulate or update data within a state.

**Design principle served:** #5 (data truth vs movement truth — `advancing` explicitly marks an event as movement truth), #8 (compile-time first).

**Grounding in samples:** InsuranceClaim: `Submit` (always transitions), `AssignAdjuster` (always transitions from Submitted to UnderReview), `Approve` (always transitions — the reject row is a separate concern), `Deny` (always transitions), `PayClaim` (always transitions). LoanApplication: `Submit`, `Approve`, `Decline`, `FundLoan` — all advancing. The non-advancing events: `VerifyDocuments` (no transition), `RequestDocument` (no transition), `ReceiveDocument` (sometimes no transition, sometimes reject). The advancing/settling split maps directly to the domain distinction between "routing actions" and "data-collection actions."

**Fit verdict: Strong.** Fully compile-time provable from row outcomes. Captures a real structural distinction visible in every sample. Zero runtime cost. Clean complement to `settling` (below).

---

#### 3. `settling` — Every successful outcome is `no transition`

**Syntax:**
```precept
event LogRepairStep settling with StepName as string
event VerifyDocuments settling
```

**What it means:** When this event fires successfully, it never changes state. The event exists purely for in-state data mutation — updating fields, modifying collections, or recording information without advancing the lifecycle.

**Compile-time check:** Error if any row for this event has a `transition` outcome. Only `no transition` and `reject` outcomes are allowed.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches rows where the author accidentally added a `transition` to a data-only event.
- **Tooling:** Diagram can omit settling events from state-to-state arrows (they don't change state) or show them as self-loops. Inspector labels them as "data-only — state unchanged." Preview groups settling and advancing events separately.
- **Diagram simplification:** In complex precepts, settling events create visual clutter in state diagrams because they appear as self-loops. A `settling` modifier lets the diagram renderer suppress or de-emphasize them.

**Design principle served:** #5 (data truth — `settling` events are purely about data, not movement), #9 (tooling drives syntax — the modifier enables better diagram rendering).

**Grounding in samples:** InsuranceClaim: `RequestDocument` (always `no transition` + collection mutation), `ReceiveDocument` (always `no transition` when successful + `reject` when not). WarrantyRepairRequest: `LogRepairStep` (always `no transition`), `UndoLastStep` (always `no transition` when successful + `reject` when stack empty). HiringPipeline: `AddInterviewer` (always `no transition`). LoanApplication: `VerifyDocuments` (always `no transition`). Every sample with data-collection patterns has settling events.

**Fit verdict: Strong.** Fully compile-time provable. Structural complement to `advancing`. Together, `advancing` and `settling` partition the event space into "changes state" and "doesn't change state," with unmodified events allowed to do both. Clean from a language design perspective — the pair is a complete classification.

---

#### 4. `irreversible` — No path back to the source state after firing

**Syntax:**
```precept
event Approve irreversible with Amount as number
```

**What it means:** Once this event fires successfully, the entity can never return to the state it was in before the event fired. The transition is a one-way door.

**Compile-time check:** For every `from S on Approve -> transition T` row, verify that `S` is not reachable from `T` in the transition graph. The analyzer builds the adjacency set from transition rows, then checks reverse reachability. Error if any target state `T` has a path back to source state `S`.

This check treats all edges as traversable (guards ignored — same overapproximation as `required`). A path that is guard-impossible still flags the error. The diagnostic must communicate: "structurally, a return path exists."

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches lifecycle design errors where the author intended a one-way transition but accidentally created a cycle. Example: if Approved can somehow transition back to UnderReview, `irreversible` on Approve catches this.
- **Tooling:** Diagram draws irreversible transitions with special arrow styling (solid, one-way). Preview can label transitions as "point of no return."
- **Domain reasoning:** Irreversibility is a core business concept. In loan processing, once a loan is Funded, you can't go back to Approved. In hiring, once an offer is Accepted, you can't go back to Decision. The modifier captures this.

**Design principle served:** #1 (deterministic, inspectable — irreversibility is a visible lifecycle property), #8 (compile-time first — provable from graph structure with overapproximation).

**Grounding in samples:** LoanApplication: `FundLoan` (Approved → Funded, no path back). InsuranceClaim: `PayClaim` (Approved → Paid, no path back). `Deny` in every sample transitions to a terminal state — trivially irreversible. Many events that transition to terminal states are irreversible by construction (no outgoing transitions from terminal means no return path).

**Fit verdict: Moderate.** Graph-provable with overapproximation. The guarantee is structural, not absolute — guards can make the reverse path impossible even without the modifier. But the domain value is real. Interaction with `terminal` is noteworthy: every transition to a terminal state is trivially irreversible, so the modifier is most useful for non-terminal transitions. The compiler could hint: "This event is already irreversible because all targets are terminal" to avoid redundant modifiers.

---

#### 5. `universal` — Event is handled from every reachable state

**Syntax:**
```precept
event Cancel universal with Reason as string nullable default null
```

**What it means:** Every reachable non-terminal state has at least one transition row for this event. The event is a "global action" — always available regardless of where the entity is in its lifecycle.

**Compile-time check:** For every reachable state that is not marked `terminal`, error if no `from S on Cancel` row exists (direct or via `from any` expansion). Terminal states are excluded because they have no outgoing transitions by definition.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches states where the author forgot to handle a universal event. If Cancel should be available everywhere but the author didn't write a `from InRepair on Cancel` row (and didn't use `from any`), the compiler flags it.
- **Tooling:** Preview can show universal events in a special section ("always available"). Diagram can draw them as a separate annotation layer rather than cluttering every state with the same event.
- **AI authoring:** AI generating transition rows for a `universal` event knows it must create coverage for every state.

**Design principle served:** #8 (compile-time first — provable from transition rows and reachable state set), #12 (AI legibility).

**Grounding in samples:** No current sample uses `from any on Cancel` for a truly universal event, but the pattern is implicit. In RefundRequest, `Cancel` is only available from Submitted — not universal. More commonly, events like a hypothetical `AddNote` or `Audit` might be universal. The `from any on Event` shorthand already supports this at the transition level, but `universal` as a modifier adds an intent declaration that the compiler enforces even if the author forgets `from any`.

**Interaction with `from any`:** If a `universal` event uses `from any on Event`, the check is automatically satisfied for all states. The modifier adds value when the author uses individual `from` rows and might forget a state. It also adds value when new states are added — the compiler would immediately flag the new state as missing a row for the universal event.

**Fit verdict: Moderate.** Fully compile-time provable. Useful as a safety net when new states are added (the compiler tells you which universal events need new rows). But the current `from any` syntax already provides the mechanism — the modifier adds an intent-level check that `from any` doesn't (you CAN use `from any` without declaring universal intent, and you CAN declare universal intent without using `from any`).

---

#### 6. `isolated` — Event fires from exactly one state

**Syntax:**
```precept
event FundLoan isolated
event StartRepair isolated
```

**What it means:** This event's transition rows all originate from the same single state. The event belongs to exactly one phase of the lifecycle.

**Compile-time check:** Collect all source states referenced in `from S on Event` rows (expanding `from any` to all states). Error if more than one distinct source state appears.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches architectural drift — if someone adds a new `from UnderReview on FundLoan` row to an event that was designed for the Approved state only, the compiler flags it.
- **Tooling:** Preview can group isolated events under their single source state, making the lifecycle structure more legible. IntelliSense can auto-fill the `from` state when authoring rows for an isolated event.
- **Lifecycle clarity:** In domain terms, `isolated` events are "state-specific actions." They belong to a particular phase of the process and don't make sense elsewhere.

**Design principle served:** #4 (locality of reference — the event is local to one state's concern), #8 (compile-time first).

**Grounding in samples:** LoanApplication: `FundLoan` fires only from Approved, `VerifyDocuments` fires only from UnderReview. WarrantyRepairRequest: `StartRepair` fires only from Approved, `ConfirmReturn` fires only from ReadyToReturn, `LogRepairStep` fires only from InRepair. Most events in the samples are isolated — they fire from exactly one state. The exceptions are events like `RejectCandidate` in HiringPipeline (fires from Screening, InterviewLoop, Decision) and `RequestDocument` in InsuranceClaim (fires from Submitted and UnderReview).

**Fit verdict: Strong.** Fully compile-time provable. Captures the dominant pattern — most events in the sample corpus are structurally isolated. Clean complement to `universal` — together they form a scope spectrum: `isolated` (one state) → unmodified (some states) → `universal` (all states). Zero runtime cost.

---

#### 7. `guarded` — Every row for this event has a `when` clause

**Syntax:**
```precept
event Approve guarded with Amount as number
```

**What it means:** The event's availability is always conditional. No unguarded (catch-all) row exists — every row must explicitly declare its precondition.

**Compile-time check:** Error if any `from S on Approve` row lacks a `when` clause.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Prevents accidentally adding an unguarded fallback row for an event that should always be conditioned. This is a safety catch for critical events where unconditional firing would be a business logic error.
- **Tooling:** Inspector labels guarded events as "conditional — always requires precondition." IntelliSense prompts the author to add a `when` clause when authoring rows for guarded events.
- **Interaction with C25 (unreachable rows):** A guarded event can never have an unreachable row caused by a preceding unguarded row (because no unguarded row exists). C25 is structurally prevented for guarded events.

**Design principle served:** #8 (compile-time first), #6 (collect-all for validation — guarded events force explicit guard authoring, making the first-match routing explicit).

**Grounding in samples:** LoanApplication: `Approve` has a complex guard (`when DocumentsVerified and CreditScore >= 680 and ...`). If someone added a simple unguarded `from SomeState on Approve -> transition Approved` row, it would bypass the business rules. `guarded` prevents this. InsuranceClaim: `Approve` is guarded on `PoliceReportRequired` and `ClaimAmount`. HiringPipeline: `ExtendOffer` is guarded on `FeedbackCount >= 2`.

**Fit verdict: Moderate.** Fully compile-time provable. Real safety value for high-stakes events where unconditional firing would be a business error. However, the current pattern (guarded row + unguarded reject fallback) is idiomatic in the samples — the reject fallback provides a user-facing error message. `guarded` would disallow this pattern unless reject rows are exempt. **Design question:** Should reject-only rows (rows whose outcome is `reject`) be exempt from the `guarded` check? If yes, the modifier means "every row that can succeed must have a guard" — which preserves the guarded+reject-fallback pattern and is arguably the right semantics.

---

#### 8. `total` — Every state that handles this event has at least one non-reject outcome

**Syntax:**
```precept
event RecordInterviewFeedback total
```

**What it means:** For every state where this event has transition rows, at least one row can succeed (produce `transition` or `no transition`). The event is never hopeless in any state that handles it — if the event is available, it can potentially succeed.

**Compile-time check:** For every (state, event) pair, check that at least one row has a non-reject outcome. Error if any state has only `reject` rows for this event. This is the inverse of C51 (reject-only pairs) — `total` upgrades C51 from a warning to an error for this event.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Strengthens C51 for specific events. C51 is a general warning ("this state/event pair always rejects — is that intentional?"). `total` says "no, it's never intentional for this event — rejecting from every row in any state means the transition rows are wrong."
- **Tooling:** Inspector can label total events as "always potentially successful."
- **Interaction with `guarded`:** `total` + `guarded` is a strong combination — "every row has a guard, and in every state, at least one guarded path can succeed." This captures complex events like Approve that have guards but should always have a success path.

**Design principle served:** #8 (compile-time first), #6 (collect-all vs first-match — `total` ensures the first-match routing has at least one valid match per state).

**Grounding in samples:** InsuranceClaim: `ReceiveDocument` from UnderReview has both a success row (`when MissingDocuments contains ...`) and a reject fallback. From the author's perspective, the event should always be able to succeed in UnderReview if the document is valid. `total` would enforce that at least one non-reject row exists.

**Fit verdict: Moderate.** Fully compile-time provable (it's literally the C51 check inverted). Upgrades an existing warning to an error for specific events. The value is real but incremental — C51 already catches the problem as a warning. `total` makes it an author-declared requirement.

---

#### 9. `completing` — Event always transitions to a terminal state

**Syntax:**
```precept
event PayClaim completing
event FundLoan completing
```

**What it means:** Every successful transition outcome for this event targets a `terminal` state. This is the event that closes the lifecycle.

**Compile-time check:** For every `from S on PayClaim -> transition T`, error if `T` is not marked `terminal`. Requires `terminal` to exist in the language. `no transition` outcomes are not allowed on a completing event (the event's purpose is to END the lifecycle — staying put contradicts that).

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches rows where a completing event accidentally transitions to an intermediate state.
- **Tooling:** Diagram draws completing events as arrows to the terminal boundary. Preview labels them as "lifecycle-ending."
- **Dependency:** Requires `terminal` modifier on states. This creates a modifier ecosystem — `completing` events reference `terminal` states, forming a declarative lifecycle frame: `initial` → ... → `terminal` with `entry` opening and `completing` closing.

**Design principle served:** #5 (movement truth — completing is a lifecycle-boundary concept), #8 (compile-time first).

**Grounding in samples:** LoanApplication: `FundLoan` transitions to Funded (would be terminal). InsuranceClaim: `PayClaim` transitions to Paid (would be terminal). WarrantyRepairRequest: `ConfirmReturn` transitions to Closed (would be terminal). The pattern is consistent.

**Fit verdict: Moderate.** Fully compile-time provable contingent on `terminal` existing. This is a revisit of the first pass's `final` — renamed to `completing` for clarity (avoids collision with `final` in C#/Java semantics, and `completing` reads better: "event PayClaim completing"). The first pass rejected `final` partly because it creates a cross-row constraint breaking Principle #7. This remains a valid concern — `completing` constrains all rows for the event. Mitigated by the fact that `completing` events are typically single-row (one state → one terminal state), so the cross-row burden is light. **Upgraded from Tier 3 to Tier 2** given the rename, the dependency on `terminal`, and the lighter cross-row burden for typical usage.

---

#### 10. `symmetric` — For every A→B transition, a B→A path exists

**Syntax:**
```precept
event Transfer symmetric
```

**What it means:** For every `from A on Transfer -> transition B` row, there exists some event `E` with a `from B on E -> transition A` row. The transition is bidirectional — what you can do, you can undo (possibly via a different event).

**Compile-time check:** For every `from A on Transfer -> transition B`, check the graph adjacency map for a `from B on <anything> -> transition A` row. Error if no reverse edge exists. Note: the reverse doesn't need to use the same event — just any event.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches lifecycle design errors where the author intended a reversible path but forgot the return route.
- **Tooling:** Diagram draws bidirectional arrows for symmetric transitions.
- **Domain reasoning:** Symmetry is meaningful in processes where entities can move between equivalent states: transfer between departments, escalate/de-escalate priority, assign/reassign.

**Design principle served:** #1 (inspectable — symmetry is a visible graph property), #8 (compile-time first).

**Grounding in samples:** Few current samples exhibit symmetric transitions. The pattern would appear in escalation workflows (Open ↔ Escalated), assignment workflows (AssignedA ↔ AssignedB), or approval loops where a rejected application can be resubmitted. The current samples are mostly forward-flowing pipelines.

**Fit verdict: Weak.** Fully compile-time provable but limited grounding in the sample corpus. The concept is well-defined but niche. Most business workflows are directional, not bidirectional. **Recommend: Defer** — revisit if demand appears in real-world usage.

---

### Event Modifier Summary

| Modifier | Provability | Runtime cost | Role | Fit |
|---|---|---|---|---|
| `entry` | Full | None | Intent + constraint | **Strong** |
| `advancing` | Full | None | Intent + constraint | **Strong** |
| `settling` | Full | None | Intent + constraint + tooling | **Strong** |
| `irreversible` | Graph (overapprox) | None | Intent + constraint | **Moderate** |
| `universal` | Full | None | Constraint + safety net | **Moderate** |
| `isolated` | Full | None | Intent + constraint | **Strong** |
| `guarded` | Full | None | Constraint + safety | **Moderate** |
| `total` | Full (C51 inversion) | None | Constraint (C51 upgrade) | **Moderate** |
| `completing` | Full (requires `terminal`) | None | Intent + constraint | **Moderate** |
| `symmetric` | Full | None | Constraint | **Weak** |

**Key insight from the second pass:** The first pass's conclusion that "event modifiers constrain behavioral properties requiring runtime state" was wrong. That conclusion was driven by evaluating `once` and `idempotent` — modifiers about *firing history* and *mutation commutativity*. The expanded analysis shows that events have rich **graph-structural** properties (source scope, outcome shape, target scope, reversibility, coverage) that are fully compile-time provable from declared transition rows. The structural event modifiers (`entry`, `advancing`, `settling`, `isolated`) are as strong as state modifiers — they constrain declared structure, not runtime behavior.

The fundamental error in the first pass was conflating "event modifier" with "event behavior modifier." The strong candidates are *event structure modifiers* — properties of the declared transition rows, not of execution traces.

---

### Field Modifiers

Fields currently have two modifiers: `nullable` (allows null values) and `default` (specifies initial value). Both are **type-contract** modifiers — they constrain the field's initial state and value space. New field modifiers should address the field's **lifecycle contract** — how the field's value evolves across the entity's lifecycle as events fire and states change.

Field lifecycle properties derivable from declared structure:

- **Write frequency** — how many transition rows target this field with `set`?
- **Write scope** — which states can mutate this field?
- **Write direction** — does the value only go up, or can it decrease?
- **Collection direction** — does the collection only grow, or can items be removed?
- **Sensitivity** — does the field contain data requiring special handling?
- **Derivation** — is the field always calculated from other fields?

#### 1. `writeonce` — Field can be set at most once across the lifecycle

**Syntax:**
```precept
field ClaimantName as string nullable writeonce
field OrderId as string nullable writeonce
```

**What it means:** After the field's value changes from its default/null to a concrete value, no subsequent transition row may set it again. The field captures a datum that is permanent once established — names, IDs, decisions, timestamps.

**Compile-time check:** The full check requires path analysis (ensure no execution path sets the field more than once), which is complex for cyclic graphs. A sound overapproximation: partition the graph into "pre-set" states (reachable from initial without passing through a row that sets the field) and "post-set" states (reachable from any row that sets the field). Error if any row in a post-set state sets the field again.

For the common case: if the field is only set in rows from the initial state (e.g., `from Draft on Submit -> set ClaimantName = ...`), and no other row sets it, the check is trivially satisfied. The compiler can catch the simple violations without full path analysis and overapproximate the complex ones.

**Runtime implications:** For full enforcement, a per-field "has been set" flag would be needed. But the compile-time check catches the structural violations — a runtime flag is only needed for cycles where the same row fires twice.

**What it enables:**
- **Compiler:** Error if two rows on different states both set the same `writeonce` field (structural over-write detection). Warning if a single row sets a writeonce field and the row's source state is reachable more than once (cycle detection).
- **Tooling:** Preview marks writeonce fields with a lock icon after they're set. IntelliSense doesn't suggest writeonce fields in `set` actions from non-initial states.
- **Domain reasoning:** Many fields in the samples are set exactly once: ClaimantName (Submit), ApplicantName (Submit), OrderId (Submit), CandidateName (SubmitApplication). The pattern is pervasive.

**Design principle served:** #8 (compile-time first — partial provability from row analysis), #1 (deterministic — the field's value is fixed after first write).

**Grounding in samples:** InsuranceClaim: `ClaimantName` is set only in `from Draft on Submit`. LoanApplication: `ApplicantName`, `CreditScore`, `AnnualIncome`, `ExistingDebt` — all set only in Submit. HiringPipeline: `CandidateName`, `RoleName`, `RecruiterName` — set only in SubmitApplication. RefundRequest: `OrderId`, `CustomerEmail` — set only in Submit. The pattern is universal across samples: intake-phase data fields are set once and never changed.

**Fit verdict: Strong.** Captures the most common field lifecycle pattern in the corpus. Compile-time check catches the dominant case (field set in one state, never set in another). Runtime flag needed only for edge cases (cyclic graphs re-entering the setting state). Minimal syntax extension — single keyword in the modifier position.

---

#### 2. `identity` — Field is part of the entity's identity; immutable after first write

**Syntax:**
```precept
field OrderId as string nullable identity
field SerialNumber as string nullable identity
```

**What it means:** The field is a key identifier for the entity. It must be set exactly once (like `writeonce`) AND carries additional semantic weight: tooling treats it as the entity's label, MCP uses it in summary descriptions, and the preview shows it prominently.

**Compile-time check:** Same as `writeonce` (error if set more than once on any path). Additional: identity fields are automatically excluded from `edit` declarations — they cannot be directly edited by the host application.

**Runtime implications:** Same as `writeonce`.

**What it enables beyond `writeonce`:**
- **Tooling:** Preview shows identity fields in the instance header (e.g., "LoanApplication: John Smith / $50,000"). MCP `inspect` result includes identity fields as a summary. Diagram annotations can label states with identity field values.
- **Edit interaction:** `identity` fields are automatically excluded from `edit` declarations. `in Submitted edit all` would not include identity fields. This is a compile-time constraint that prevents the host app from overwriting the entity's identity.
- **AI legibility:** AI tools can use identity fields to refer to instances in natural language ("the claim filed by Jane Doe" rather than "the instance in state UnderReview").

**Design principle served:** #12 (AI legibility — identity fields make entities describable), #4 (locality — the field declaration tells you its role), #8 (compile-time — writeonce check + edit exclusion).

**Grounding in samples:** InsuranceClaim: `ClaimantName`, `ClaimAmount` serve as identity. LoanApplication: `ApplicantName`, `RequestedAmount`. HiringPipeline: `CandidateName`, `RoleName`. WarrantyRepairRequest: `CustomerName`, `ProductName`, `SerialNumber`. Every sample has 1-3 fields that function as the entity's natural language identity.

**Fit verdict: Moderate.** Superset of `writeonce` + semantic/tooling annotations. The `writeonce` compile-time check is the structural core; `identity` adds a tooling role. If `writeonce` ships independently, `identity` could be layered on top. But there's a design question: should modifier stacking be allowed (`field X as string nullable writeonce identity`)? Or is `identity` a stronger modifier that subsumes `writeonce`? The subsumption approach is simpler.

---

#### 3. `monotonic` — Value can only increase (numbers) or collection can only grow

**Syntax:**
```precept
field FeedbackCount as number default 0 monotonic
field MissingDocuments as set of string monotonic
```

**What it means:**
- For **number** fields: the value can only increase or stay the same. No `set` action may produce a value less than the current value.
- For **collection** fields: items can only be added, never removed. `add`, `enqueue`, `push` are allowed; `remove`, `dequeue`, `pop`, `clear` are errors.

**Compile-time check:**
- **Collections (full provability):** Error if any `remove`, `dequeue`, `pop`, or `clear` action targets this field. This is a simple verb check — fully compile-time provable from declared actions.
- **Scalars (partial provability):** Error if any `set FeedbackCount = <expr>` where the expression is provably decreasing. Simple cases: `set X = X - 1` is provably decreasing; `set X = X + 1` is provably increasing; `set X = Y` is unknown. The compiler can flag definitely-decreasing expressions and pass on unknowns.

**Runtime implications:** For scalars, full monotonicity enforcement requires a runtime check (compare new value to current value). For collections, no runtime overhead — the compile-time verb check is complete.

**What it enables:**
- **Compiler (collections):** Error on `remove`/`pop`/`dequeue`/`clear` targeting a monotonic collection. This is the strongest compile-time guarantee.
- **Compiler (scalars):** Error on provably-decreasing mutations. Warning on ambiguous mutations.
- **Domain reasoning:** Monotonic fields represent accumulations — feedback counts, approval amounts, document collections. They capture the business rule "this number only goes up" or "this collection only grows."

**Design principle served:** #8 (compile-time first — full for collections, partial for scalars), #1 (deterministic — monotonicity is a visible data property).

**Grounding in samples:** HiringPipeline: `FeedbackCount` only increases (`set FeedbackCount = FeedbackCount + 1`). InsuranceClaim's `MissingDocuments` is NOT monotonic — documents are removed when received. But a hypothetical `CompletedDocuments` set would be monotonic. LoanApplication has no monotonic collections. The pattern exists but is less pervasive than `writeonce`.

**Fit verdict: Moderate.** Split provability: full for collections (verb check), partial for scalars (expression analysis). The collection case is compelling — `add`-only sets are a real pattern. The scalar case requires new expression analysis infrastructure. **Recommend:** If implemented, start with collections only (full provability) and extend to scalars later.

---

#### 4. `sensitive` — Field contains data requiring special handling

**Syntax:**
```precept
field SocialSecurityNumber as string nullable sensitive
field DateOfBirth as string nullable sensitive
field CustomerEmail as string nullable sensitive
```

**What it means:** The field contains personally identifiable information (PII), protected health information (PHI), or other sensitive data. No structural constraint on how the field is used in the DSL — the modifier is a metadata annotation that changes how every consumer presents the field's value.

**Compile-time check:** None. This is not a structural constraint.

**Runtime implications:** The runtime itself is unchanged. The modifier is carried on `PreceptField` as a flag and consumed by tooling.

**What it enables:**
- **MCP tools:** `precept_inspect` and `precept_fire` mask sensitive field values in their output (`"CustomerEmail": "****"`). This prevents sensitive data from appearing in AI tool outputs, chat logs, and MCP response caches.
- **Preview webview:** Sensitive fields display as masked by default, with an explicit reveal toggle.
- **Language server:** Hover info on a sensitive field shows a "⚠ Sensitive data" notice.
- **AI interaction safety:** Principle #12 (AI is first-class consumer) means AI tools read and write precept data. `sensitive` gives the tooling layer a signal to mask data before sending it to AI models, chat histories, or log files. This is a real data-protection concern.

**Design principle served:** #12 (AI-first — explicit signal for AI-facing tooling to mask data), #9 (tooling drives syntax — the keyword exists because tooling needs the signal).

**Grounding in samples:** CustomerProfile: `Email`, `Phone` would benefit from `sensitive`. InsuranceClaim: PII is light, but in a real deployment, claimant contact info would be sensitive. The samples don't currently mark PII because there's no mechanism to do so.

**Fit verdict: Moderate.** No compile-time structural check, which weakens the fit against Principle #8. But the tooling value is real and the design principle alignment with #12 is strong. This is the first modifier in the analysis that is purely a **tooling directive** rather than a structural constraint — it expands the modifier concept beyond compile-time checks. If the project accepts tooling-directive modifiers as valid, `sensitive` is a strong entry. If the project restricts modifiers to compile-time provable properties, `sensitive` should be a separate mechanism (perhaps a field annotation rather than a modifier keyword).

---

#### 5. `derived` — Field is computed from other fields; never directly set

**Syntax:**
```precept
field DebtToIncomeRatio as number derived = ExistingDebt / AnnualIncome
field TotalScore as number derived = CreditScore * 0.4 + FeedbackCount * 10
```

*Or, without inlined expression (modifier only, expression provided elsewhere):*
```precept
field DebtToIncomeRatio as number derived
```

**What it means:** The field's value is always computed from other fields. No `set` action may target it. No `edit` declaration may include it. The field is read-only to both transition rows and the host application — its value is re-evaluated whenever its inputs change.

**Compile-time check:** Error if any transition row contains `set DerivedField = ...`. Error if any `edit` declaration includes a derived field. If the inline expression form is used, error if the expression references undeclared fields or has type mismatches.

**Runtime implications:** Significant. The runtime must re-evaluate derived field expressions after every mutation. This means:
- A new evaluation pass in the fire pipeline (after `set` mutations, before invariant checks).
- Derived fields must be topologically ordered (no circular dependencies).
- The expression evaluator must handle derived-field expressions as first-class computed properties.

**What it enables:**
- **Compiler:** `set` and `edit` targeting a derived field is a hard error. Circular dependency detection.
- **Tooling:** Preview shows derived fields with a "computed" indicator. IntelliSense excludes derived fields from `set` completions. Hover shows the computation expression.
- **Language simplification:** Currently, "computed" values require manual `set` synchronization — if `CreditScore` changes, the author must remember to re-compute `DebtToIncomeRatio`. `derived` eliminates this synchronization burden.

**Design principle served:** #8 (compile-time — strong compile-time checks against direct mutation), #1 (deterministic — derived values are always consistent with inputs).

**Interaction with proposal #17:** This is a natural entry point for Issue #17 (computed fields). The `derived` modifier keyword + inline expression syntax defines the language surface; the computation evaluation is the runtime implementation. Shipping `derived` as a modifier-only keyword (without inline expressions) is possible but less useful — it would just mean "don't set this field," which is a weaker guarantee.

**Grounding in samples:** LoanApplication: `DebtToIncomeRatio` = `ExistingDebt / AnnualIncome` could be a derived field (currently the guard inlines this as `AnnualIncome >= ExistingDebt * 2`). HiringPipeline: a hypothetical `InterviewProgressPct` = `(TotalInterviewers - PendingInterviewers.count) / TotalInterviewers`. The samples don't use derived fields because the mechanism doesn't exist yet, but the domain use cases are clear.

**Fit verdict: Moderate-to-Strong as a concept; Defer as implementation.** The `derived` modifier is the right language-surface entry point for computed fields. But the runtime implications are substantial — new evaluation pass, topological ordering, expression scope expansion. The modifier keyword design should be included in proposal #17's scope, not shipped independently. As a standalone modifier (without computation expressions), it only means "no `set` or `edit` targeting" — equivalent to a convention the author enforces manually. **Recommend:** Include `derived` keyword design in #17 proposal; defer implementation to that proposal's timeline.

---

#### 6. `immutable` — Field cannot be mutated in any transition row

**Syntax:**
```precept
field PolicyVersion as string default "v2.1" immutable
field CreatedAt as string default "2024-01-01" immutable
```

**What it means:** The field is set at declaration time (via `default`) and cannot be changed by any transition row or edit declaration. It is a configuration constant embedded in the precept instance.

**Compile-time check:** Error if any `set ImmutableField = ...` appears in any transition row. Error if any `edit` declaration includes the field. Fully compile-time provable — it's a complete ban on mutation.

**Runtime implications:** None. The field exists on the instance data with its default value and is never changed. The runtime doesn't need a special code path — the compiler prevents all mutations.

**What it enables:**
- **Compiler:** Total mutation ban — no `set`, no `edit`, no collection verbs. Simpler than `writeonce` (which allows one write) — `immutable` allows zero writes.
- **Tooling:** Preview shows immutable fields as constants. IntelliSense excludes them from `set` and `edit` completions entirely.
- **Domain reasoning:** Configuration constants — policy versions, regulatory categories, product types — that are fixed at entity creation and never change.

**Design principle served:** #8 (compile-time — fully provable, zero mutations to scan), #1 (deterministic — the value never changes).

**Grounding in samples:** No current sample has true constants, but real-world precepts would: policy version strings, regulatory jurisdiction codes, product category identifiers. These are fields that the domain expert sets at precept authoring time (via `default`), not at instance runtime.

**Fit verdict: Moderate.** Fully compile-time provable. Clean semantics. But the use case is narrow — it's essentially a named constant embedded in instance data. The question is whether constants belong as fields at all, or whether a separate `const` declaration would be cleaner. `immutable` is the simpler approach (no new declaration form), but it mixes "data that never changes" with "data that evolves" in the field namespace. Trade-off: simplicity (reuse `field` + modifier) vs clarity (new `const` form). **Recommend:** If implemented, use the modifier form (simpler) and let the `const` alternative be explored separately.

---

#### 7. `sealed after <State>` — Field becomes immutable when entity enters a state

**Syntax:**
```precept
field ApprovedAmount as number default 0 sealed after Approved
field ClaimantName as string nullable sealed after Submitted
```

**What it means:** Once the entity enters the named state (or any state reachable from it), the field can no longer be mutated. The field is freely writable in earlier lifecycle phases and locked in later phases.

**Compile-time check:** Build the set of states reachable from the sealing state (including the sealing state itself). Error if any `set` action targets this field in a transition row whose source state is in the reachable set. Error if any `edit` declaration for a state in the reachable set includes this field.

**Runtime implications:** None. The check is fully compile-time — the compiler uses the same reachability analysis it already computes for C48.

**What it enables:**
- **Compiler:** Catches late-stage mutations of fields that should be frozen. Example: `ApprovedAmount` should not change after the claim is Approved — `sealed after Approved` prevents a rogue transition row from overwriting it.
- **Tooling:** Preview shows field lock icons in states at or after the sealing point. IntelliSense excludes sealed fields from `set` and `edit` completions in sealed states.
- **Domain reasoning:** Many fields have a natural "freeze point" in the lifecycle. ClaimantName freezes after Submitted. ApprovedAmount freezes after Approved. OfferAmount freezes after OfferExtended. The freeze point is a real business rule — "once we've committed to this number, it cannot change."

**Design principle served:** #8 (compile-time — fully provable from reachability + row analysis), #5 (data truth — the field's mutability is lifecycle-phase-dependent, which is a data-meets-movement concern).

**Grounding in samples:** InsuranceClaim: `ApprovedAmount` is set in `from UnderReview on Approve -> set ApprovedAmount = ...` and never set again after transitioning to Approved. `sealed after Approved` would enforce this. LoanApplication: `ApprovedAmount` is set in `from UnderReview on Approve` and never set after Approved. WarrantyRepairRequest: `ApprovalNote` is set at Submitted→Approved and never changed. The pattern is pervasive — most fields have a natural lifecycle freeze point.

**Grammar consideration:** This introduces a new modifier form: `sealed after <StateName>`. Unlike `nullable` or `writeonce` (standalone keywords), `sealed after` takes a state reference. The parser would need to handle `field ... sealed after <Identifier>` as a modifier clause. This is more complex than a boolean keyword modifier but follows the same modifier-position convention.

**Fit verdict: Strong.** Fully compile-time provable, high domain value, captures a pervasive pattern. The grammar extension is more complex than a single keyword but well-motivated. The compile-time check reuses existing reachability infrastructure. **Key design question:** Can a field be sealed after multiple states? E.g., `sealed after Approved, Denied` — meaning the field freezes when the entity enters either state. This would use comma-separated state names, consistent with existing multi-state syntax in `in S1, S2 assert ...`.

---

#### 8. `audit` — Every mutation to this field is tracked

**Syntax:**
```precept
field ApprovedAmount as number default 0 audit
field DecisionNote as string nullable audit
```

**What it means:** The host application and tooling should track every change to this field — who changed it, when, from what value to what value. This is a metadata annotation for compliance, not a structural constraint.

**Compile-time check:** None. The modifier is metadata.

**Runtime implications:** The runtime fire pipeline already records mutations (the `FireResult` includes field changes). `audit` would extend this:
- The `FireResult` (or a separate audit log) would explicitly tag changes to `audit` fields.
- The MCP `fire` and `inspect` results would surface audit-flagged field changes with before/after values.
- The host application could use the `audit` flag to trigger external audit logging.

**What it enables:**
- **MCP tools:** `precept_fire` output highlights changes to audit fields: `"auditChanges": [{ "field": "ApprovedAmount", "from": 0, "to": 15000 }]`.
- **Preview:** Audit fields show a change history in the field detail panel.
- **Host application:** The `PreceptField.IsAudit` flag lets the host app conditionally log changes to regulated fields.
- **Compliance:** In regulated industries (finance, healthcare, insurance), field-level change tracking is a compliance requirement. The modifier lets the precept author declare which fields are compliance-sensitive.

**Design principle served:** #12 (AI legibility — AI tools know which fields are compliance-tracked), #9 (tooling drives syntax — the keyword exists because tooling needs the signal).

**Grounding in samples:** InsuranceClaim: `ApprovedAmount`, `DecisionNote` would benefit from audit tracking in a real deployment. LoanApplication: `ApprovedAmount`, `CreditScore` (did it change during processing?). The samples don't use it because the mechanism doesn't exist, but the domain need is clear in regulated verticals.

**Fit verdict: Weak-to-Moderate.** No compile-time structural check. The value is in runtime/tooling metadata, not in language-level enforcement. Same concern as `sensitive` — is a tooling-directive modifier within scope for the language, or should it be a separate mechanism? If the project accepts tooling modifiers, `audit` is a reasonable candidate. If not, it's a host-application concern that belongs outside the DSL.

---

### Field Modifier Summary

| Modifier | Provability | Runtime cost | Role | Fit |
|---|---|---|---|---|
| `writeonce` | Partial (row analysis + overapprox) | Minimal (cycle edge case) | Constraint + intent | **Strong** |
| `identity` | Same as writeonce + edit exclusion | Same as writeonce | Intent + tooling | **Moderate** |
| `monotonic` | Full (collections) / Partial (scalars) | None (collections) / Minimal (scalars) | Constraint | **Moderate** |
| `sensitive` | None | None (tooling-only) | Tooling directive | **Moderate** |
| `derived` | Full (mutation ban) | Significant (eval pass) | Constraint + feature gate | **Deferred (#17)** |
| `immutable` | Full (total mutation ban) | None | Constraint | **Moderate** |
| `sealed after <State>` | Full (reachability + row) | None | Constraint + intent | **Strong** |
| `audit` | None | Minimal (metadata) | Tooling directive | **Weak-to-Moderate** |

---

### Additional State Modifiers

Beyond `terminal` (Tier 1), `required` (Tier 2), and `transient` (Tier 2) from the first pass.

#### 1. `error` / `failure` — State represents an error or failure mode

**Syntax:**
```precept
state Denied error
state Stuck error
```

**What it means:** The state represents a failure outcome in the lifecycle. The entity ended up here because something went wrong — a denial, a cancellation, a policy violation. Events FROM an error state are "recovery" events (if any exist).

**Compile-time check:**
- If `error` and `terminal`: no additional check needed (the error is a final outcome).
- If `error` and not `terminal`: the compiler can warn if no outgoing transition exists (the state is a dead-end error with no recovery path — likely a missing escape route or should be marked `terminal`).
- If `error` and has outgoing transitions: the transitions are "recovery" events. Tooling can label them accordingly.

**Runtime implications:** None. Purely a compile-time and tooling property.

**What it enables:**
- **Compiler:** In combination with `terminal`: no new check. With non-terminal error states: strengthens C50 (dead-end) detection — a non-terminal error state that is also a dead-end is a stronger diagnostic than a generic dead-end.
- **Tooling:** Diagram colors error states in red/amber. Preview shows them with a failure icon. MCP `inspect` labels events from error states as "recovery." The design system already assigns a distinct color to terminal states — `error` would add a second semantic color category.
- **AI authoring:** AI tools can distinguish "the entity failed" from "the entity completed" — both are terminal, but they carry different business meaning.

**Design principle served:** #12 (AI legibility — `error` communicates business meaning), #9 (tooling — enables visual distinction in diagrams and preview).

**Grounding in samples:** InsuranceClaim: `Denied` is an error/failure state. HiringPipeline: `Rejected` is a failure state. LoanApplication: `Declined` is a failure state. RefundRequest: `Declined` is a failure state. Every sample has 1-2 failure endpoints alongside success endpoints (Paid, Hired, Funded, Refunded).

**Interaction with `terminal`:** `error` and `terminal` are orthogonal — a state can be `terminal` (lifecycle end), `error` (failure outcome), both (failed terminal), or neither. `state Denied terminal error` marks a state as "lifecycle endpoint and failure outcome." `state Retry error` (without terminal) marks a recoverable error state.

**Fit verdict: Moderate.** Limited compile-time leverage (mostly a tooling/semantic annotation). But the **domain semantic value** is clear — every sample has error states, and distinguishing failure endpoints from success endpoints is a universal business need. If the project accepts semantic annotation modifiers (same category as `sensitive`), `error` is one of the strongest candidates.

---

#### 2. `resting` — Entity is expected to remain in this state; must have self-loop structure

**Syntax:**
```precept
state Active resting
state InRepair resting
```

**What it means:** The entity is expected to spend significant time in this state, receiving events that mutate data without changing state. Opposite of `transient` — where `transient` means "pass through quickly," `resting` means "this is a normal operating state."

**Compile-time check:** A `resting` state must have at least one `no transition` outcome from some (state, event) pair. Error if every outgoing transition row transitions to a different state (the state has no self-loop behavior, contradicting the "resting" claim).

Additionally, the compiler can check that the state has `edit` declarations (resting states are where direct field editing typically happens).

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches a mislabeled resting state that has no self-loop behavior.
- **Tooling:** Diagram emphasizes resting states as "steady states" — the places where entities spend most of their life. Preview can show "entity is settled" vs "entity is in transit."
- **Domain reasoning:** In workflows, most entities spend 90% of their lifecycle in 1-2 resting states (Active, InProgress, InRepair). Declaring this intent makes the lifecycle structure more legible.

**Design principle served:** #8 (compile-time — provable self-loop check), #1 (inspectable — resting states are the lifecycle's "normal" modes).

**Grounding in samples:** InsuranceClaim: `UnderReview` is a resting state (events fire within it: RequestDocument, ReceiveDocument). WarrantyRepairRequest: `InRepair` is a resting state (LogRepairStep, UndoLastStep fire within it). HiringPipeline: `InterviewLoop` is resting (RecordInterviewFeedback fires within it). The resting pattern is present in every sample with in-state data collection.

**Fit verdict: Moderate.** The compile-time check (must have at least one self-loop) is real but lightweight. The main value is semantic/tooling — `resting` communicates intent and enables diagram optimization. Stronger than `checkpoint` (next) because it has a verifiable structural property.

---

#### 3. `decision` — State with multiple competing transition outcomes

**Syntax:**
```precept
state UnderReview decision
state Decision decision
```

**What it means:** Multiple distinct events can transition the entity OUT of this state to different target states. The state is a branching point — a decision gate where the outcome depends on which event fires.

**Compile-time check:** For a `decision` state, at least two distinct events must have `transition` outcomes. Error if fewer than two distinct events produce `transition` outcomes from this state.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches mislabeled decision states (only one outgoing transition event means it's not really a decision — it's a pipeline stage).
- **Tooling:** Diagram can style decision states as diamond shapes (BPMN convention). Preview shows "decision pending" with the competing options laid out. MCP `inspect` from a decision state highlights the branching events.
- **Domain reasoning:** Decision states are where business judgment is applied — approve/deny, extend offer/reject candidate, fund/decline. They are architecturally important because they're where guards and business rules concentrate.

**Design principle served:** #8 (compile-time — provable from outgoing row analysis), #9 (tooling — enables BPMN-style diamond rendering).

**Grounding in samples:** InsuranceClaim: `UnderReview` → Approve or Deny (decision). HiringPipeline: `Decision` → ExtendOffer or RejectCandidate (decision). LoanApplication: `UnderReview` → Approve or Decline (decision). The pattern is in every sample — every non-trivial workflow has at least one decision state.

**Fit verdict: Moderate.** Fully compile-time provable. Real tooling value (diamond shapes, decision highlighting). The concern: `decision` is somewhat redundant with what's already visible from transition rows. It doesn't enable new analysis that the compiler couldn't do by just counting outgoing events. The value is in declared intent + tooling presentation.

---

#### 4. `guarded` — Every incoming transition to this state has a `when` clause

**Syntax:**
```precept
state Approved guarded
```

**What it means:** Every transition row that targets this state (`-> transition Approved`) must have a `when` guard. The state should only be entered under explicitly declared conditions — unconditional entry is an error.

**Compile-time check:** Scan all `from S on E ... -> transition Approved` rows. Error if any lacks a `when` clause. This is the state-analog of event-level `guarded` — but scoped to incoming transitions rather than outgoing rows.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches unguarded entry into high-value states. Example: `Approved` should never be reached without checking business rules. If someone writes `from UnderReview on Approve -> transition Approved` without a guard, the compiler errors.
- **Safety net:** This is the single strongest compile-time safety check for business-critical states. The guard ensures that the business-rule evaluation happened before entry.
- **Interaction with `to State assert`:** A `to Approved assert ...` entry gate catches invalid data post-entry. `guarded` ensures the guard happens pre-entry. They are complementary: `guarded` prevents unguarded routing; `to assert` validates post-mutation data.

**Design principle served:** #8 (compile-time — fully provable from row analysis), #5 (data truth vs movement truth — `guarded` ensures movement is conditioned on data).

**Grounding in samples:** LoanApplication: `from UnderReview on Approve when DocumentsVerified and CreditScore >= 680 and ...`. The Approve row IS guarded. With `state Approved guarded`, the compiler would verify this and catch any future unguarded Approve rows. InsuranceClaim: similarly, Approve into Approved has a compound guard.

**Fit verdict: Strong.** Fully compile-time provable. High safety value — prevents the most dangerous kind of lifecycle error (reaching a high-value state without checking conditions). Complements `to State assert` instead of competing with it. Clean semantics.

---

#### 5. `absorbing` — State accepts events but never transitions out

**Syntax:**
```precept
state Closed absorbing
```

**What it means:** The state handles events (data mutations, reject responses) but the entity never leaves. Distinguished from `terminal` in two ways:
- `terminal` = lifecycle endpoint with no outgoing transition rows at all.
- `absorbing` = active event handler that happens to never route elsewhere. Rows exist, but all have `no transition` or `reject` outcomes — no `transition` outcome targets another state.

**Compile-time check:** Error if any row for this state has a `transition` outcome. `no transition` and `reject` are required for all rows. Additionally, at least one `no transition` or `reject` row must exist (otherwise the state is terminal, not absorbing — use `terminal`).

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Validates that the state has event handling but no escape route. Catches accidentally added `transition` rows on a state meant to be a data-collecting endpoint.
- **Tooling:** Diagram shows absorbing states differently from terminal states — they have incoming arrows (events handled) but no outgoing arrows. Preview shows "active endpoint — accepts events but lifecycle complete."
- **Domain example:** A `Closed` state that allows adding audit notes, adjusting final amounts, or recording post-closure information — but the entity never transitions to another state.

**Design principle served:** #8 (compile-time — row outcome check), #1 (inspectable — the distinction between "truly done" and "done but still active" is visible).

**Grounding in samples:** Current samples don't have absorbing states — their terminal states (Paid, Hired, Funded, Closed) have no event handling. But in real-world deployments, post-closure data collection is common: adding audit notes after claim closure, recording feedback after hiring, annotating shipped orders.

**Fit verdict: Moderate.** Fully compile-time provable. Clean structural semantics. But the first pass already noted that terminal states CAN have `no transition` and `reject` rows — so `absorbing` could be expressed as `terminal` with event handling. The question is whether the distinction between "terminal with rows" and "terminal without rows" is worth a separate keyword. If the tooling treats them differently (absorbing states show as active endpoints in diagrams; terminal states show as dead endpoints), the keyword earns its place.

---

#### 6. `convergent` — State is reachable from multiple other states

**Syntax:**
```precept
state UnderReview convergent
```

**What it means:** The state has incoming transitions from two or more distinct source states. It's a merge point in the lifecycle — multiple paths converge here.

**Compile-time check:** Count the distinct source states that have `transition <State>` outcomes targeting this state. Error if fewer than two distinct source states exist.

**Runtime implications:** None.

**What it enables:**
- **Compiler:** Catches mislabeled convergent states.
- **Tooling:** Diagram can highlight convergence points with a BPMN-style merge icon or thicker border. This is architecturally useful for lifecycle understanding — convergent states are where multiple paths rejoin.
- **Interaction with `decision`:** `decision` is a divergent state (multiple paths out); `convergent` is a convergent state (multiple paths in). Together they describe the lifecycle's branching and merging structure.

**Design principle served:** #8 (compile-time), #9 (tooling — enables visual merge-point indicators).

**Grounding in samples:** InsuranceClaim: `UnderReview` receives transitions from Submitted (via AssignAdjuster — wait, only from Submitted). Actually, most states in the samples have only ONE incoming source. The exception pattern is rare in current samples. HiringPipeline: `Rejected` has incoming from Screening, InterviewLoop, and Decision — it IS convergent. But Rejected is a terminal sink, not a processing state.

**Fit verdict: Weak.** Fully compile-time provable but limited utility. Convergent points in the samples are mostly terminal states (Rejected, Declined, Denied) where multiple paths lead to the same failure endpoint. For these, `terminal` + `error` already communicates the meaning. Convergent non-terminal states (multiple paths into a processing state) are less common in the sample corpus. **Recommend: Defer** — revisit if lifecycle complexity increases.

---

### Additional State Modifier Summary

| Modifier | Provability | Runtime cost | Role | Fit |
|---|---|---|---|---|
| `error` / `failure` | Limited (combo with terminal/C50) | None | Semantic + tooling | **Moderate** |
| `resting` | Partial (self-loop check) | None | Intent + tooling | **Moderate** |
| `decision` | Full (outgoing event count) | None | Intent + tooling | **Moderate** |
| `guarded` (on state) | Full (incoming row scan) | None | Constraint + safety | **Strong** |
| `absorbing` | Full (row outcome check) | None | Constraint + tooling | **Moderate** |
| `convergent` | Full (incoming source count) | None | Tooling | **Weak** |

---

### Cross-Cutting Observations

#### 1. The provability spectrum spans entity types differently than expected

The first pass concluded: "state modifiers are provable; event modifiers are not." The second pass disproves this. The correct statement is:

| Entity type | Compile-time provability source | Strongest candidates |
|---|---|---|
| **States** | Graph topology (reachability, degree, domination) | `terminal`, `guarded` (incoming), `required`, `error` |
| **Events** | Transition row structure (source states, outcome shapes, target states) | `entry`, `advancing`, `settling`, `isolated` |
| **Fields** | Mutation analysis (which rows set which fields, reachability of setting rows) | `writeonce`, `sealed after`, `monotonic` (collections) |

All three entity types have modifiers that are fully or partially compile-time provable. The first pass was biased by evaluating the wrong event modifiers — `once` and `idempotent` are behavioral (firing-history) properties, which truly require runtime state. But `entry`, `advancing`, and `settling` are structural (row-shape) properties, which are as provable as state modifiers.

#### 2. Four distinct modifier roles emerge

| Role | Example modifiers | Compile check? | Tooling impact? | Runtime impact? |
|---|---|---|---|---|
| **Structural constraint** | `terminal`, `advancing`, `settling`, `writeonce`, `sealed after` | Yes | Indirect | None |
| **Intent declaration** | `entry`, `isolated`, `resting`, `decision` | Cross-check | Visual styling | None |
| **Tooling directive** | `sensitive`, `audit`, `error` | None | Primary purpose | Metadata only |
| **Feature gate** | `derived` | Yes (disables `set`/`edit`) | Completions filtered | New eval pass |

The language needs a position on which roles are in-scope for keyword modifiers. The safest position: **structural constraints and intent declarations are modifiers; tooling directives and feature gates may require a different mechanism** (e.g., annotations, metadata blocks). But `initial` is already a hybrid — it's a structural constraint AND a tooling directive (diagram marks the start state). So the precedent supports hybrid modifiers.

#### 3. Modifier pairing creates a declarative lifecycle frame

The strongest candidates form natural pairs and chains:

| Pair / chain | What it declares |
|---|---|
| `initial` + `terminal` | Lifecycle boundary frame — where the entity starts and ends |
| `entry` + `completing` | Event boundary frame — which events open and close the lifecycle |
| `advancing` + `settling` | Event outcome dichotomy — state-changing vs data-mutating events |
| `isolated` + `universal` | Event scope spectrum — single-state vs all-state events |
| `writeonce` + `sealed after` | Field mutability lifecycle — when fields are first set and when they freeze |
| `guarded` (state) + `to assert` | Entry safety layers — guards before entry, asserts after entry |
| `terminal` + `error` | Endpoint taxonomy — success endpoints vs failure endpoints |

These pairings suggest that modifiers are not isolated keywords but elements of a **declarative lifecycle vocabulary** — a way for the author to describe the lifecycle's structural properties without writing transition-row-level rules.

#### 4. The `advancing`/`settling` split is the most valuable discovery

Of all candidates across all three entity types, the `advancing`/`settling` dichotomy may be the most impactful for language usability. Every sample shows two kinds of events:

- **Advancing events** that move the lifecycle forward: Submit, Approve, Deny, PayClaim, FundLoan
- **Settling events** that accumulate data within a state: VerifyDocuments, LogRepairStep, AddInterviewer, RequestDocument

This is a **fundamental domain distinction** — it separates "routing" from "data collection." The distinction is invisible in the current DSL unless you read every transition row. Modifiers make it visible at the declaration level, which serves:
- **Author comprehension** — scanning event declarations tells you the lifecycle's structure without reading transition rows
- **AI authoring** — AI knows whether to generate `transition` or `no transition` outcomes
- **Diagram rendering** — settling events can be visually suppressed or grouped separately
- **IntelliSense** — when authoring rows for a settling event, `transition` is not suggested as an outcome

#### 5. Tooling-directive modifiers need a philosophical position

Two candidates — `sensitive` and `audit` — have no compile-time structural check. They are pure tooling metadata. The project needs to decide:

**Option A — Modifiers are strictly compile-time-provable properties.** `sensitive` and `audit` must use a different mechanism (annotations, metadata blocks, or host-app-level configuration). The keyword modifier position is reserved for structural constraints and intent declarations.

**Option B — Modifiers are any keyword-declared property, including tooling directives.** `sensitive` and `audit` join `nullable` and `default` as modifiers. The precedent: `nullable` is not a compile-time constraint (the compiler doesn't prevent null values), it's a type-contract modifier. `default` is not a compile-time constraint, it's an initialization directive. So the modifier system already includes non-structural keywords.

**Option C — Hybrid: modifiers are either structurally verifiable OR tooling-actionable.** This includes `sensitive` (tooling masks it) and `audit` (tooling tracks it) because they trigger concrete tooling behavior. It excludes pure documentation annotations that tooling doesn't act on.

The first pass implicitly assumed Option A. Shane's feedback ("modifiers are not just constraints") suggests Option B or C. **Frank's recommendation: Option C** — a modifier must either be compile-time verifiable or trigger concrete tooling behavior. Pure documentation (like a hypothetical `@deprecated` tag) should use comments or a future annotation mechanism, not keyword modifiers.

---

### Updated Recommendation Tiers

Incorporating the second pass analysis with original first-pass candidates.

#### Tier 1 — Strong Candidates (propose)

| Modifier | Entity | Provability | Key value |
|---|---|---|---|
| `terminal` | State | Full | Lifecycle boundary; `initial` parallel; C50 interaction |
| `entry` | Event | Full | Intake event pattern; most consistent sample pattern |
| `advancing` | Event | Full | State-changing intent; routing vs mutation distinction |
| `settling` | Event | Full | Data-only intent; complement to `advancing` |
| `isolated` | Event | Full | Single-state scope; dominant event pattern |
| `writeonce` | Field | Partial (overapprox) | Intake-data immutability; pervasive sample pattern |
| `sealed after <State>` | Field | Full | Lifecycle-phase immutability; freeze-point pattern |
| `guarded` | State | Full | Entry safety; prevents unguarded high-value state entry |

#### Tier 2 — Interesting but Complex (explore further)

| Modifier | Entity | Provability | Key blocker |
|---|---|---|---|
| `required` / `milestone` | State | Partial (dominator + overapprox) | Guard-dependent false positives; diagnostic clarity |
| `transient` | State | Partial | Weaker-than-expected guarantee; no time model |
| `irreversible` | Event | Graph (overapprox) | Guard-dependent; most useful for non-terminal targets |
| `universal` | Event | Full | Overlap with `from any` mechanism |
| `completing` | Event | Full (requires `terminal`) | Dependency chain; cross-row constraint |
| `total` | Event | Full (C51 inversion) | Incremental over existing C51 warning |
| `error` / `failure` | State | Limited | Primarily semantic/tooling; limited compile leverage |
| `resting` | State | Partial (self-loop) | Primarily semantic/tooling |
| `decision` | State | Full | Derivable from transition rows |
| `identity` | Field | Same as `writeonce` + extras | Superset of `writeonce`; design stacking question |
| `monotonic` | Field | Full (collections) / Partial (scalars) | Split provability; start with collections |
| `sensitive` | Field | None (tooling-only) | Requires philosophical position on tooling modifiers |
| `immutable` | Field | Full | Narrow use case; const-vs-field design question |

#### Tier 3 — Reject or Defer

| Modifier | Entity | Reason |
|---|---|---|
| `once` | Event | Requires runtime counter; achievable via boolean field + guard |
| `idempotent` | Event | Undecidable; requires runtime infrastructure |
| `symmetric` | Event | Limited domain grounding; niche pattern |
| `convergent` | State | Limited utility; mostly terminal states |
| `absorbing` | State | Expressible as terminal + event handling; separate keyword marginal |
| `guarded` (event) | Event | Conflicts with idiomatic reject-fallback pattern |
| `derived` | Field | Right entry point but deferred to proposal #17 |
| `audit` | Field | Tooling metadata; better as host-app concern |

---

### Prioritized Implementation Sequence

If the project decides to implement modifiers, the recommended order is:

1. **`terminal`** — Tier 1, lowest risk, highest certainty. Ships alone. Unblocks `completing` and `required`.
2. **`entry` + `advancing` + `settling` + `isolated`** — Tier 1 event modifiers. Ship as a cohort (they form a coherent vocabulary). Low implementation cost (row-shape checks, no runtime changes).
3. **`writeonce`** — Tier 1 field modifier. Ships independently. Requires row mutation analysis.
4. **`sealed after <State>`** — Tier 1 field modifier. Ships after `terminal` (the sealing-point concept is clearest when terminal states exist). Requires reachability + row analysis.
5. **`guarded` (state)** — Tier 1 state modifier. Ships independently. Incoming-row scan.
6. **Tier 2 candidates** — evaluated after Tier 1 feedback from real usage.

---

### Key References (Second Pass)

- All first-pass references remain applicable.
- `samples/insurance-claim.precept` — exemplar for `settling` (RequestDocument, ReceiveDocument), `advancing` (Submit, Approve), `writeonce` (ClaimantName), `sealed after` (ApprovedAmount after Approved).
- `samples/hiring-pipeline.precept` — exemplar for `isolated` (most events), `settling` (AddInterviewer), `decision` (Decision state), `monotonic` (FeedbackCount).
- `samples/loan-application.precept` — exemplar for `entry` (Submit from Draft only), `guarded` state (Approved requires compound guard), `writeonce` (ApplicantName, CreditScore).
- `samples/warranty-repair-request.precept` — exemplar for `resting` (InRepair with settling events), `advancing` (Submit, StartRepair), `settling` (LogRepairStep, UndoLastStep).
- `samples/refund-request.precept` — exemplar for `advancing` (Submit, Approve, Decline), `sealed after` (ApprovedAmount after AwaitingReturn).
- `samples/customer-profile.precept` — stateless precept; modifier concepts like `writeonce`, `sensitive`, and `identity` apply to stateless entities as well, confirming that field modifiers are not state-machine-specific.
