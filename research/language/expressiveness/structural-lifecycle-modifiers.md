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
