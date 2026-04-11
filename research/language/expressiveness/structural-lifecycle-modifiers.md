# Structural Lifecycle Modifiers

**Research date:** 2026-04-10
**Author:** Frank (Lead/Architect & Language Designer)
**Batch:** Horizon — no open proposal
**Relevance:** Declarative modifiers on state, event, and field declarations that constrain graph-structural roles, lifecycle boundary semantics, path obligations, residency properties, mutation control, and tooling presentation

This file is durable research, not a proposal body. It captures the full modifier design space across states, events, and fields; what adjacent systems do; which candidates fit Precept's compile-time-first philosophy; and which should be deferred or rejected.

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

`initial` also demonstrates that a modifier can fill multiple roles simultaneously:

- **Structural constraint:** C8 and C13 enforce uniqueness and presence.
- **Intent declaration:** the author states "this is the entry point," and the compiler cross-checks.
- **Tooling directive:** the diagram marks the start state with a distinctive visual treatment.
- **Analysis enabler:** BFS reachability analysis requires a known root — `initial` provides it.

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

Terminal states are detected but never *diagnosed* — they are structurally normal. The analyzer distinguishes terminal states (no outgoing rows) from dead-end states (outgoing rows that all fail). Dead-end states get C50 because they have transition machinery that never succeeds — likely an authoring mistake.

Without modifiers, the compiler cannot distinguish:
- **Intentionally terminal** — `Closed` is the lifecycle endpoint; no outgoing transitions is correct.
- **Accidentally dead-end** — `Stuck` was supposed to have an escape route but the author forgot or wrote guards that are always false.

A `terminal` modifier lets the author declare intent, making C50 a stronger diagnostic for non-terminal states and suppressing it for declared endpoints.

### Modifier roles

A modifier on a declaration is any keyword that adds a **provable structural property** to the entity. "Provable" means verifiable from declared structure — guards aside — at compile time. A modifier can fill any of these roles:

1. **Structural constraint** — the compiler errors if declared structure violates the property (e.g., `terminal` errors on outgoing transitions).
2. **Intent declaration** — the author states what they intend, and the compiler cross-checks (e.g., `entry` asserts the event only fires from the initial state).
3. **Tooling directive** — the modifier changes how IntelliSense, preview, diagrams, or MCP present the entity (e.g., `sensitive` causes value masking).
4. **Analysis enabler** — the modifier unlocks new compiler checks that are meaningless without the declaration (e.g., `required` enables bypass-path detection).
5. **Feature gate** — the modifier enables or restricts other language features for that entity (e.g., `derived` disables `set` targeting).

Most strong candidates serve multiple roles simultaneously. The existing `nullable` and `default` field modifiers show that non-constraint roles (type contract, initialization) are already established in the modifier system.

**Scope rule:** A modifier must be either (a) compile-time verifiable from declared structure, or (b) trigger concrete tooling behavior where tooling needs an explicit author signal. Pure documentation annotations with no structural check and no tooling action belong in comments or a future annotation mechanism, not the keyword modifier position.

### The broader question

The `initial` precedent and the `terminal` gap are the visible tip of a larger design space. Structural modifiers exist across all three Precept declaration entity types — states, events, and fields. Each entity type has graph-structural or mutation-lifecycle properties that are compile-time provable, and for each, declared modifiers enable stronger diagnostics, clearer authoring intent, and better tooling presentation.

This research maps the full taxonomy, surveys comparable systems, and evaluates each candidate against Precept's design principles.

---

## Taxonomy of Structural Modifier Types

Structural modifiers constrain properties derivable from the declared states, events, transition rows, and field declarations without runtime data. The space spans all three entity types.

### State modifiers

States have graph-topology properties: where the entity enters, where it exits, which states it must visit, how it behaves while there.

| Category | Modifier | Meaning | Compile-time check |
|---|---|---|---|
| **Boundary** | `initial` (exists) | Lifecycle entry | C8: unique; C13: required |
| **Boundary** | `terminal` (candidate) | Lifecycle exit; no outgoing transitions | Error if outgoing `transition` rows exist |
| **Path** | `required` / `milestone` (candidate) | All initial→terminal paths must visit | Dominator analysis on transition graph |
| **Residency** | `transient` (candidate) | Must have a successful outgoing transition | Error if no row with `transition` outcome |
| **Safety** | `guarded` (candidate) | All incoming transitions must have guards | Error if any incoming row lacks `when` |
| **Semantic** | `error` / `failure` (candidate) | Represents a failure outcome | Limited — primarily tooling |
| **Semantic** | `resting` (candidate) | Expected steady state | Must have at least one `no transition` outcome |
| **Semantic** | `decision` (candidate) | Branching point | Must have 2+ distinct outgoing event types |
| **Structural** | `absorbing` (candidate) | Event handler that never transitions out | Must have rows; no `transition` outcomes allowed |
| **Structural** | `convergent` (candidate) | Reachable from 2+ distinct source states | Count incoming source states |

### Event modifiers

Events have transition-row-structure properties: where they fire, what outcomes they produce, which states they target, whether they create reversible paths.

| Category | Modifier | Meaning | Compile-time check |
|---|---|---|---|
| **Scope** | `entry` (candidate) | Fires only from initial state | Error if non-initial source state has rows |
| **Scope** | `isolated` (candidate) | Fires from exactly one state | Error if 2+ distinct source states |
| **Scope** | `universal` (candidate) | Fires from every reachable non-terminal state | Error if any reachable state lacks rows |
| **Outcome** | `advancing` (candidate) | Every success is a state transition | Error if any row has `no transition` outcome |
| **Outcome** | `settling` (candidate) | Every success is `no transition` | Error if any row has `transition` outcome |
| **Outcome** | `completing` (candidate) | Transitions only to `terminal` states | Error if any target state is not `terminal` |
| **Structural** | `irreversible` (candidate) | No path from target back to source | Graph reverse-reachability check |
| **Structural** | `symmetric` (candidate) | A return path exists for every transition | Graph adjacency check for reverse edges |
| **Guard** | `guarded` (candidate) | Every row must have `when` clause | Error if any row lacks `when` |
| **Guard** | `total` (candidate) | Every state handling event has non-reject rows | Inversion of C51 — upgrade to error |
| **[Rejected]** | `once` | Fires at most once | Requires runtime counter |
| **[Rejected]** | `idempotent` | Repeated firing yields same result | Mutation commutativity undecidable |

### Field modifiers

Fields have write-lifecycle and value-shape properties: when they can be set, whether they grow, whether they hold sensitive data.

| Category | Modifier | Meaning | Compile-time check |
|---|---|---|---|
| **Type contract** | `nullable` (exists) | Value may be null | Type-level |
| **Type contract** | `default` (exists) | Initial value at creation | Initialization |
| **Write constraint** | `writeonce` (candidate) | Set at most once across the lifecycle | Row-partition analysis |
| **Write constraint** | `immutable` (candidate) | Never mutated; `default` value only | Total mutation ban |
| **Write constraint** | `sealed after <State>` (candidate) | No mutation after named state is entered | Reachability + row analysis |
| **Value shape** | `monotonic` (candidate) | Only increases (numbers) / only grows (collections) | Verb check (collections); expression analysis (scalars) |
| **Lifecycle** | `identity` (candidate) | Identifying data: `writeonce` + tooling weight | Same as `writeonce` + `edit` exclusion |
| **Computation** | `derived` (candidate) | Computed from other fields; no direct writes | Error on any `set`/`edit` targeting |
| **Tooling** | `sensitive` (candidate) | Contains PII/PHI; mask in tool output | None (tooling directive) |
| **Tooling** | `audit` (candidate) | Mutations tracked for compliance | None (tooling metadata) |

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

**Alloy:** Alloy specifies structural properties as first-order relational constraints. Reachability is expressed as transitive closure: `all s: State | s in InitialState.*transitions` (all states are reachable). Terminal states are states with no outgoing transitions: `no s.transitions`. Alloy's Analyzer checks these by bounded model finding.

**Precept implications:**
- TLA+ and Alloy show that every candidate modifier maps to a precisely defined formal property. This is reassuring — the concepts are well-understood in formal methods.
- The key distinction: TLA+ and Alloy verify properties over *all possible execution traces* (model checking / bounded model finding). Precept's compile-time analysis operates over the *declared graph structure* (BFS/DFS on transition rows). The two are related but not identical — Precept's graph analysis is an overapproximation.
- This means compile-time provability of path modifiers (`required`) depends on whether the check considers `when` guards or treats all edges as traversable. The sound approach: treat all edges as traversable for structural checks (overapproximate reachability), which means structural guarantees cannot account for guard-dependent path selection.

**URLs:** https://lamport.azurewebeb.net/tla/tla.html, https://alloytools.org/documentation.html

### Temporal.io

**Workflow completion:** A Temporal workflow completes when the workflow function returns. There is no explicit "terminal state" declaration — the function's return is the terminal event.

**Activity lifecycle:** Temporal activities have built-in heartbeat timeout and schedule-to-close timeout — runtime-enforced residency constraints. Activities that fail to heartbeat are cancelled and potentially retried.

**Once semantics:** Temporal provides exactly-once execution guarantees for activities through its event-sourced replay mechanism. This is infrastructure-level `once`, not a language-level declaration.

**Precept implications:**
- Temporal's completion model is procedural (function return), not declarative (state marker). It doesn't provide a direct analogue for `terminal`.
- Temporal's activity timeouts are the closest analogue to `transient`, but they are runtime-enforced with a clock. Precept's event-driven model can only check structural residency (no `no transition` outcomes), not time-bounded residency.
- Temporal's exactly-once guarantee confirms that `once` is an infrastructure concern, not a declaration concern. A precept that needs `once` semantics needs runtime tracking.

**URL:** https://docs.temporal.io/workflows

### UML State Machines

**Final states:** UML defines a `FinalState` pseudostate — a filled circle inside a circle. Reaching a FinalState completes the enclosing region (or the entire state machine for the top-level region). UML requires that FinalState has no outgoing transitions.

**Initial pseudostate:** The filled-circle `InitialPseudostate` marks the default entry point for a region. Exactly one per region. This is the direct ancestor of Precept's `initial` keyword.

**History pseudostates:** Shallow and deep history pseudostates remember the last active substate of a composite state. These require hierarchical state machines — not applicable to Precept's flat model.

**Junction and choice pseudostates:** Junction (static) and choice (dynamic) pseudostates implement branching. Junction is resolved at compile time; choice is resolved at runtime based on guards. Precept's `when` guard model is equivalent to choice pseudostates.

**Precept implications:**
- UML's `FinalState` is the canonical formal precedent for Precept's `terminal`. The semantics are well-defined: no outgoing transitions, triggers completion.
- UML's `InitialPseudostate` directly maps to Precept's `initial` — Precept's implementation is already UML-aligned.
- UML has no path-obligation, residency, or cardinality concepts on states/events. State machine theory historically separates behavioral specification (the machine) from property verification (temporal logic, model checking). Modifiers like `required` and `transient` come from the verification side, not the machine specification side.

**URL:** https://www.omg.org/spec/UML/2.5.1/

### Cedar / OPA — Policy Systems

**Cedar:** Cedar policies are `permit` or `forbid` statements with optional `when`/`unless` guards. There is no lifecycle, no state machine, and no structural modifier concept.

**OPA / Rego:** Rego rules compose via conjunction. There is no explicit "terminal rule" or lifecycle concept.

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
| **Completing (event)** | — | — | Terminate End Event | — | — | — | — | — |
| **Idempotent (event)** | — | — | — | — | — | Activity retry safety | — | — |

**Key finding:** Terminal/final states are the only modifier concept with broad cross-system precedent. Every workflow and state machine system surveyed — XState, Step Functions, BPMN, UML — has explicit terminal state support. Path modifiers, residency modifiers, and event modifiers have at best partial precedent, and that precedent usually involves runtime enforcement or formal verification, not compile-time declaration.

The event-level structural modifiers in this research (`entry`, `advancing`, `settling`, `isolated`) are not well-represented in any surveyed system. This is not because they are unsound — they are fully provable from declared row structure — but because most workflow systems do not model events as structured declarations with inspectable row properties. Precept's event-row model creates a modifier surface that has no direct analogue elsewhere.

---

## Cross-Cutting Findings

### Provability spans all entity types equivalently

The modifier space is provable across all three entity types, each drawing on a different provability source:

| Entity type | Compile-time provability source | Strongest candidates |
|---|---|---|
| **States** | Graph topology (reachability, degree, domination, incoming/outgoing rows) | `terminal`, `guarded` (incoming), `required`, `error` |
| **Events** | Transition row structure (source states, outcome shapes, target states, reverse edges) | `entry`, `advancing`, `settling`, `isolated` |
| **Fields** | Mutation analysis (which rows set which fields, reachability of setting rows, collection verb check) | `writeonce`, `sealed after`, `monotonic` (collections) |

Events specifically have rich structural properties derivable from declared transition rows — not from firing history or execution traces. The boundary between "provable" and "not provable" falls between row-shape properties (`advancing`, `settling`) and firing-history properties (`once`, `idempotent`). The former are compile-time structural; the latter require runtime state.

### Four distinct modifier roles require a clear scope position

| Role | Example modifiers | Compile check? | Tooling impact? | Runtime impact? |
|---|---|---|---|---|
| **Structural constraint** | `terminal`, `advancing`, `settling`, `writeonce`, `sealed after` | Yes | Indirect | None |
| **Intent declaration** | `entry`, `isolated`, `resting`, `decision` | Cross-check | Visual styling | None |
| **Tooling directive** | `sensitive`, `audit`, `error` | None | Primary purpose | Metadata only |
| **Feature gate** | `derived` | Yes (disables `set`/`edit`) | Completions filtered | New eval pass |

`initial` already functions as a hybrid across all four roles simultaneously. The existing `nullable` and `default` field modifiers confirm that non-constraint roles are already part of the modifier system. Based on this precedent, the scope rule is: **a modifier must be either structurally verifiable or tooling-actionable**. Modifiers with no compile-time check AND no concrete tooling behavior belong in comments or future annotations. `sensitive` qualifies (MCP masking, preview masking). `audit` is borderline — it depends on host-application integration and should be evaluated as a host-application concern unless first-class platform tooling behavior is defined.

### Modifier pairs form a declarative lifecycle vocabulary

The strongest candidates form natural pairs and composites that together describe the lifecycle's structural properties:

| Pair / chain | What it declares |
|---|---|
| `initial` + `terminal` | Lifecycle boundary frame — where the entity starts and ends |
| `entry` + `completing` | Event boundary frame — which events open and close the lifecycle |
| `advancing` + `settling` | Event outcome dichotomy — state-changing vs data-accumulating events |
| `isolated` + `universal` | Event scope spectrum — single-state to all-state |
| `writeonce` + `sealed after` | Field mutability lifecycle — when fields are first set and when they freeze |
| `guarded` (state) + `to assert` | Entry safety layers — guards before entry, asserts after |
| `terminal` + `error` | Endpoint taxonomy — success endpoints vs failure endpoints |

These pairings suggest that modifiers are not isolated keywords but elements of a **declarative lifecycle vocabulary** — a way for the author to describe the lifecycle's structural properties without reading transition-row-level rules.

### The `advancing`/`settling` split is the highest-leverage event modifier finding

Any stateful workflow contains two fundamentally different kinds of events:
- **Advancing events** that move the lifecycle forward: Submit, Approve, Deny, FundLoan, PayClaim
- **Settling events** that accumulate data within the current state: VerifyDocuments, LogRepairStep, RequestDocument, AddInterviewer

This is the event-side manifestation of Principle #5 (data truth vs movement truth). The distinction is invisible in the current DSL unless you read every transition row. Making it visible at the declaration level serves:
- **Author comprehension:** scanning event declarations tells you the lifecycle's routing structure
- **AI authoring:** AI knows whether to generate `transition` or `no transition` outcomes
- **Diagram rendering:** settling events can be visually suppressed or grouped separately
- **IntelliSense:** `transition` is not suggested as an outcome when authoring rows for a settling event

---

## Interaction with Existing Diagnostics

### Summary table

| Diagnostic | Current behavior | With `terminal` | With `required` | With `transient` |
|---|---|---|---|---|
| **C48** (unreachable) | Warning: state unreachable from initial | **No change.** Terminal + unreachable = definition problem | **No change.** Required + unreachable = contradictory declaration | **No change.** |
| **C50** (dead-end) | Warning: non-terminal state has outgoing rows that all reject/no-transition | **Suppressed for terminal states.** **Strengthened for others** — "mark with `terminal` if intentional" | No direct change | **Upgraded to error** for transient states: transient dead-end is a proven contradiction |
| **C51** (reject-only pair) | Warning: every row for (State, Event) rejects | No direct change | No change | No change |
| **C52** (event never succeeds) | Warning: event can never succeed from any reachable state | No direct change | No change | No change |
| **New diagnostic** | — | Error: terminal state has outgoing `transition` rows | Error: required state bypassed on initial→terminal path | Error: transient state has no successful outgoing transition |

### C50 interaction with `terminal`

With `terminal`:
1. States marked `terminal` are validated to have **no outgoing `transition` outcomes** (new error diagnostic).
2. States marked `terminal` are excluded from C50 analysis.
3. Non-terminal dead-end states (C50) now get stronger diagnostic language: "State 'X' appears to be a dead end. If it is intentionally terminal, mark it with `terminal`. Otherwise, fix the outgoing transitions."

### C48 + terminal: unreachable terminal

A state that is both `terminal` and unreachable (C48) is a definition smell — the author declared a lifecycle endpoint that no execution path can reach. The simpler approach: let C48 stand as-is. The author sees both pieces of information. Compound diagnostics add interaction complexity with marginal benefit.

### Potential new diagnostics

| Code | Trigger | Severity | Message pattern |
|---|---|---|---|
| **C56** (tentative) | Terminal state has outgoing `transition` outcome | Error | "State '{State}' is marked terminal but has a transition to '{Target}' — terminal states may not have outgoing transitions" |
| **C57** (tentative) | Required state bypassed on initial→terminal path | Error | "State '{State}' is marked required but path {Initial}→…→{Terminal} bypasses it" |
| **C58** (tentative) | Transient state has no successful outgoing transition | Error | "State '{State}' is marked transient but has no outgoing transition that succeeds" |

---

## Recommendation Tiers

### Tier 1 — Strong Candidates (propose)

| Modifier | Entity | Provability | Key value |
|---|---|---|---|
| `terminal` | State | Full | Lifecycle boundary; `initial` parallel; C50 interaction |
| `guarded` | State | Full | Entry safety; prevents unguarded high-value state entry |
| `entry` | Event | Full | Intake event pattern; structural corollary of `initial` |
| `advancing` | Event | Full | State-changing intent; routing vs mutation distinction |
| `settling` | Event | Full | Data-only intent; complement to `advancing` |
| `isolated` | Event | Full | Single-state scope; phase-specific action pattern |
| `writeonce` | Field | Partial (overapprox) | Intake-data immutability; permanent-record semantics |
| `sealed after <State>` | Field | Full | Lifecycle-phase immutability; freeze-point pattern |

### Tier 2 — Interesting but Complex (explore further)

| Modifier | Entity | Provability | Key blocker |
|---|---|---|---|
| `required` / `milestone` | State | Partial (dominator + overapprox) | Guard-dependent false positives; diagnostic clarity; needs `terminal` first |
| `transient` | State | Partial | Weaker-than-expected guarantee; no time model |
| `error` / `failure` | State | Limited | Primarily semantic/tooling; limited compile leverage |
| `resting` | State | Partial (self-loop) | Primarily semantic/tooling |
| `decision` | State | Full | Derivable from transition rows; primarily intent/tooling |
| `irreversible` | Event | Graph (overapprox) | Guard-dependent; most useful for non-terminal targets |
| `universal` | Event | Full | Overlap with `from any` mechanism |
| `completing` | Event | Full (requires `terminal`) | Dependency chain; cross-row constraint |
| `guarded` | Event | Full | Conflicts with idiomatic reject-fallback pattern; reject-row exemption design needed |
| `total` | Event | Full (C51 inversion) | Incremental over existing C51 warning |
| `identity` | Field | Same as `writeonce` | Superset of `writeonce`; subsumption design question |
| `monotonic` | Field | Full (collections) / Partial (scalars) | Split provability; start with collections |
| `sensitive` | Field | None (tooling-only) | Requires philosophical position on tooling-directive modifiers |
| `immutable` | Field | Full | Narrow use case; const-vs-field design question |
| `derived` | Field | Full (mutation ban) | Right entry point but deferred to proposal #17 |

### Tier 3 — Reject or Defer

| Modifier | Entity | Reason |
|---|---|---|
| `once` | Event | Requires runtime counter; achievable via boolean field + guard |
| `idempotent` | Event | Undecidable in general; better as explicit guard patterns |
| `symmetric` | Event | Niche domain applicability; most workflows are directional |
| `convergent` | State | Limited utility; `terminal` + `error` covers the common case |
| `absorbing` | State | Expressible as `terminal` + event handling; separate keyword marginal |
| `audit` | Field | Tooling metadata best as host-application concern |

---

## Implementation Sequence

If the project decides to implement modifiers, the recommended order is:

1. **`terminal`** — Tier 1, lowest risk, highest certainty. Ships alone. Unblocks `completing`, `required`, and `sealed after`.
2. **`entry` + `advancing` + `settling` + `isolated`** — Tier 1 event modifiers. Ship as a cohort (they form a coherent vocabulary). Low implementation cost (row-shape checks, no runtime changes).
3. **`writeonce`** — Tier 1 field modifier. Ships independently. Requires row mutation analysis.
4. **`sealed after <State>`** — Tier 1 field modifier. Ships after `terminal`. Requires reachability + row analysis.
5. **`guarded` (state)** — Tier 1 state modifier. Ships independently. Incoming-row scan.
6. **Tier 2 candidates** — evaluated after Tier 1 feedback from real usage.

---

## Where This Fits in the Research Taxonomy

This research creates a new domain in the language research taxonomy. It is not a natural extension of any existing domain:

- **Not constraint composition (#8, #13, #14)** — constraint composition is about value constraints (invariants, field-level bounds, named rules). Structural modifiers constrain graph topology, not field values.
- **Not static reasoning expansion** — static reasoning is about proving properties from existing declarations (contradiction detection, satisfiability). Structural modifiers *add new declarations* that create new provable properties.
- **Not entity-modeling surface (#17, #22)** — entity modeling is about what data an entity has (fields, computed values, statelessness). Structural modifiers are about what the lifecycle graph looks like.
- **Closest relative: state machine expressiveness** — `state-machine-expressiveness.md` covers what graph features Precept could gain (hierarchy, parallelism). Structural modifiers are a different axis — they annotate existing graph features to enable stronger compile-time reasoning.

**Recommended taxonomy placement:** Domain "Structural lifecycle modifiers" in the expressiveness research folder, with `state-machine-expressiveness.md` and `static-reasoning-expansion.md` as theory/reference companions.

| Domain | Open proposals | Primary grounding | Theory / reference companion | Notes |
|---|---|---|---|---|
| Structural lifecycle modifiers | — | [expressiveness/structural-lifecycle-modifiers.md](./structural-lifecycle-modifiers.md) | [references/state-machine-expressiveness.md](../references/state-machine-expressiveness.md), [references/static-reasoning-expansion.md](../references/static-reasoning-expansion.md) | Horizon domain. `terminal` is the strong candidate. |

---

## Dead Ends and Rejected Directions

### State-type system (rejected)

**Idea:** Instead of keyword modifiers, make state categories a type system — `state Closed as terminal`, `state Processing as transient`, with a fixed set of state types that carry behavioral contracts.

**Why rejected:** This turns states into typed entities with distinct behavioral contracts per type. The parser and type checker must track state types and enforce different rules per type. Architecturally invasive and breaks the current flat model where all states are structurally identical (just names). Keyword modifiers add information without changing the fundamental model. Types change the model.

### Event lifecycle annotations (rejected)

**Idea:** Full event lifecycle annotations — `event Approve preconditions [R1, R2]`, `event Close postconditions [in terminal]`.

**Why rejected:** This is pre/postcondition specification on events, equivalent to attaching Hoare-logic contracts. Precept already has this: `on Event assert ...` is the precondition; state asserts and invariants are the postconditions. Adding more annotation layers duplicates existing constructs.

### Temporal properties (deferred indefinitely)

**Idea:** Time-bounded residency (`state Processing timeout 30m`), deadline states, SLA modifiers.

**Why deferred:** Precept is event-driven with no clock model. Time-bounded properties require a runtime clock, a polling mechanism for timeout detection, and a new category of system-generated events (timeout events). This is a fundamental model extension, not a modifier. Workflow orchestrators (Temporal, Step Functions) handle time; Precept handles data integrity.

### Hierarchical state modifiers (deferred indefinitely)

**Idea:** Modifiers that reference other states — `state Review parent of TechnicalReview, LegalReview`.

**Why deferred:** Precept's flat state model is a deliberate design choice (Principle #9, tooling-friendly; `state-machine-expressiveness.md` analysis: hierarchical states are HIGH cost). Modifiers that reference other states introduce hierarchy through the back door.

### `absorbing` / `sink` as merged into `terminal` (reconsidered)

**Earlier reasoning:** `absorbing` was initially considered identical to `terminal` ("terminal state with event handling"). On further analysis, `absorbing` (rows exist, none transition) is structurally distinct from `terminal` (no outgoing rows at all). The concept is valid but marginal — a `terminal` state can already have `no transition` and `reject` rows, making it effectively absorbing. Current resolution: Tier 3, pending evidence of real demand for the visual distinction in tooling.

---

## Open Questions for Proposal Phase

If `terminal` advances to a proposal, these questions need answers:

1. **Interaction with `edit` declarations:** Can a terminal state have `in Terminal edit` declarations? (Likely yes — a closed entity might still allow annotation updates.)

2. **Interaction with state entry/exit actions:** Can a terminal state have `to Terminal -> set ClosedDate = ...` entry actions? (Likely yes — entry actions run when transitioning *into* the state.)

3. **Grammar extension:** Does `terminal` go in the same modifier position as `initial` (after state name, before comma)? If a state can be both `initial` and `terminal` (edge case: single-state precept), what's the declaration order?

4. **Stateless precepts:** Should `terminal` be rejected in stateless precepts (no states to modify)? (Yes — trivially, since there are no state declarations.)

5. **C50 message upgrade:** Should C50's message text change when `terminal` is available in the language? ("Mark this state as `terminal` if this is intentional" as guidance.)

6. **`sealed after` with multiple states:** Can a field be sealed after multiple states? E.g., `sealed after Approved, Denied` — meaning the field freezes when either state is entered. Consistent with existing multi-state syntax in `in S1, S2 assert ...`.

7. **`guarded` (state) + `edit` interaction:** If a state is `guarded` (all incoming transitions require guards), does `edit` bypass this? Likely no — direct `edit` is a different operation from transition rows. Clarify whether `guarded` applies to transitions only.

8. **`advancing` + `reject` row semantics:** `advancing` allows `reject` rows (the event is refused, but when it succeeds, it advances). Confirm this is the right semantics before proposal.

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
- `src/Precept/Dsl/DiagnosticCatalog.cs` — C48, C49, C50, C51, C52 diagnostics
- `research/language/references/state-machine-expressiveness.md` — existing theory companion
- `research/language/references/static-reasoning-expansion.md` — adjacent static-reasoning research
- `research/language/expressiveness/constraint-composition-domain.md` — Frank's #13 keyword-vs-predicate taxonomy
- `samples/insurance-claim.precept` — usage illustration source for `settling`, `advancing`, `writeonce`, `sealed after`
- `samples/hiring-pipeline.precept` — usage illustration source for `isolated`, `settling`, `decision`, `monotonic`
- `samples/loan-application.precept` — usage illustration source for `entry`, `guarded` state, `writeonce`
- `samples/warranty-repair-request.precept` — usage illustration source for `resting`, `advancing`, `settling`
- `samples/refund-request.precept` — usage illustration source for `advancing`, `sealed after`
- `samples/customer-profile.precept` — stateless precept; confirms field modifiers are not state-machine-specific

---

## Detailed Modifier Analysis

For each candidate modifier, the evaluation covers: design principle alignment, compile-time provability, runtime implications, and fit verdict.

### State Modifiers

#### `terminal` — State Boundary Modifier

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
- If state `S` is marked `terminal`, error if any `from S on E ... -> transition T` row exists.
- `no transition` outcomes are allowed on terminal states.
- `reject` outcomes are allowed (the event is received but refused — the entity stays terminal).
- `set` mutations in `no transition` or `reject` rows are allowed — a terminal state can still update data.

**Runtime implications:** None. `terminal` is compile-time-only. No new runtime state, no changes to the fire pipeline.

**Consistency with `initial`:**

| Aspect | `initial` | `terminal` |
|---|---|---|
| Keyword position | After state name | After state name |
| Cardinality | Exactly one required (C13) | Zero or more allowed |
| Compile-time check | C8: duplicate; C13: missing | Error if outgoing transitions exist |
| Runtime effect | Sets `InitialState` on definition | None |
| Parser representation | `InitialFlags` boolean array | Would add `TerminalFlags` boolean array (or similar) |

**Cardinality:** `initial` is required-unique (exactly one). `terminal` should be optional-many — a precept may have zero terminal states (a cyclic workflow) or multiple (several possible endpoints). BPMN, UML, and XState all allow multiple terminal states.

**Interaction with C50 (dead-end states):**
- Declared `terminal` states are excluded from C50 analysis — they have no outgoing transitions by definition.
- Non-terminal states with all-failing outgoing rows still get C50 — now strengthened: "State 'X' appears to be a dead end. If it is intentionally terminal, mark it with `terminal`. Otherwise, fix the outgoing transitions."

**Interaction with C48 (unreachable states):** No change. A terminal state that is unreachable still gets C48. Being terminal doesn't exempt a state from reachability — an unreachable terminal is a definition problem.

**Design system interaction:** The visual treatment already assigns terminal states a distinct color (`#C4B5FD`). A `terminal` modifier aligns the authoring surface with the visual surface — what the diagram shows, the DSL declares.

**Usage illustration:** In a lifecycle like InsuranceClaim, states such as `Paid`, `Denied`, and `Withdrawn` are the natural lifecycle endpoints. Marking them `terminal` declares what the diagram already shows and what the author intends — while giving the compiler the information it needs to flag any future outgoing transition as an error.

**Fit verdict: Tier 1.** Lowest-risk, highest-clarity modifier in the entire design space.

---

#### `required` / `milestone` — State Path Modifier

**Syntax:**
```precept
state UnderReview required
state Assessment milestone
```

**Compile-time provability:** Partially provable with caveats.

The check: enumerate all initial→terminal paths using dominator analysis. Error if the `required` state is not a dominator of all terminal states — i.e., if any path from initial to any terminal state bypasses the required state. Dominator analysis is O(V + E) using Lengauer-Tarjan — tractable for Precept's graph sizes.

Complications:
1. **Guard-dependent paths:** Guards are ignored (overapproximation — all edges treated as traversable). The check may flag paths that guard logic makes impossible at runtime. The diagnostic must communicate: "structurally, a bypass path exists."
2. **Requires `terminal`:** Required states must be on every initial→terminal path, so terminal states must be identifiable. `terminal` must ship first.

**Keyword choice — `required` vs `milestone`:** `required` is precise ("must visit") but collides with the common English use of "required" for mandatory fields. `milestone` is evocative but softer. For Precept's domain audience, `required` is clearer despite the textual collision — the collision is contextual.

**Usage illustration:** In InsuranceClaim, an `Assessment` state that every claim must pass through before Approval embodies the `required` concept — no claim goes from Draft to Approved without an assessment. The domain need is real: business process integrity often demands proof that a mandatory review step was not skipped.

**Fit verdict: Tier 2.** Real domain value. Tractable dominator-based analysis. Blocked by: guard-dependent false positives weakening the guarantee; `terminal` must exist first; keyword choice needs resolution.

---

#### `transient` — State Residency Modifier

**Syntax:**
```precept
state Processing transient
```

**Compile-time provability:** Partially provable.

The structural check: error if any (State, Event) pair has only `no transition` or `reject` outcomes, or if no outgoing rows exist at all. This confirms a successful outgoing transition structurally exists.

However, the compiler cannot guarantee that any outgoing transition's `when` guard will evaluate to `true` at runtime. `transient` cannot mean "the entity will leave" — only "a successful transition exists structurally." This is a semantic promise gap: users reading `transient` may expect time-bounded enforcement that Precept cannot deliver without a clock model.

**Usage illustration:** A `Processing` state in WarrantyRepairRequest where the entity is expected to move quickly through diagnostic analysis. The structural check ensures at least one outgoing transition exists; the intent declaration communicates "this is not a resting state."

**Fit verdict: Tier 2.** The structural dead-end check has value, but C50 already covers the strongest version. `transient` adds marginal value over C50 with significant semantic-promise risk. Defer until `terminal` ships and real usage shows demand.

---

#### `guarded` — State Safety Modifier

**Syntax:**
```precept
state Approved guarded
```

**What it means:** Every `from S on E -> transition Approved` row must have a `when` clause. The state should only be entered under explicitly declared conditions — unconditional entry is an error.

**Compile-time provability:** Fully provable — scan all incoming transition rows, error if any lacks `when`.

**Runtime implications:** None.

**Interaction with `to State assert`:** `guarded` prevents unguarded routing pre-entry. `to Approved assert ...` validates data post-entry. They are complementary layers.

**Usage illustration:** In LoanApplication, entry to `Approved` requires a compound guard (`DocumentsVerified and CreditScore >= 680`). With `state Approved guarded`, the compiler catches any future unguarded row routing to Approved — preventing the most dangerous lifecycle error: reaching a high-value state without evaluating the business rules that justify the transition.

**Fit verdict: Tier 1.** Fully compile-time provable. High safety value. Prevents unguarded routing to business-critical states.

---

#### `error` / `failure` — State Semantic Modifier

**Syntax:**
```precept
state Denied error
state Rejected error
```

**What it means:** The state represents a failure outcome. Recovery transitions from this state (if any) are "escape" routes.

**Compile-time provability:** Limited. If `error` + `terminal`: validate no outgoing transitions. If `error` + non-terminal: strengthen C50 — a non-terminal error state with no escape route is a stronger diagnostic than a generic dead-end.

**What it enables:** Primarily tooling — diagram colors error states in red/amber; preview shows failure icon; MCP `inspect` labels events from error states as "recovery." AI tools can distinguish "the entity failed" from "the entity completed."

**Interaction with `terminal`:** Orthogonal. A state can be `terminal` (lifecycle end), `error` (failure outcome), both, or neither. `state Denied terminal error` marks a failed terminal. `state Retry error` (without `terminal`) marks a recoverable error state.

**Usage illustration:** In InsuranceClaim, `Denied` is a failure terminal. In HiringPipeline, `Rejected` appears at several lifecycle branches. Distinguishing failure endpoints from success endpoints is a universal business need — BPMN distinguishes Error End Events from normal End Events; Step Functions has explicit `Fail` states.

**Fit verdict: Tier 2.** Limited structural check. Primary value is semantic annotation and tooling.

---

#### `resting` — State Residency Intent Modifier

**Syntax:**
```precept
state Active resting
state InRepair resting
```

**What it means:** The entity is expected to spend significant time in this state, accumulating data via in-state events. Structural opposite of `transient`.

**Compile-time check:** Must have at least one `no transition` outcome from some (state, event) pair. Error if every outgoing row transitions to a different state.

**Usage illustration:** In WarrantyRepairRequest, `InRepair` is a resting state — `LogRepairStep` and `UndoLastStep` fire within it repeatedly without changing state. In InsuranceClaim, `UnderReview` fires `RequestDocument` and `ReceiveDocument` as data-collecting resting events. Resting states are the lifecycle's data-collection phases.

**Fit verdict: Tier 2.** Real structural check (self-loop requirement). Primary value is semantic and tooling.

---

#### `decision` — State Branching Modifier

**Syntax:**
```precept
state UnderReview decision
state Decision decision
```

**What it means:** Multiple distinct events from this state produce `transition` outcomes to different target states. Business judgment is applied here.

**Compile-time check:** At least two distinct events must have `transition` outcomes from this state. Error if fewer than two.

**Usage illustration:** In InsuranceClaim, `UnderReview` branches to either Approve or Deny. In HiringPipeline, `Decision` branches to ExtendOffer or RejectCandidate. Decision states concentrate guard logic and business rules.

**Fit verdict: Tier 2.** Fully compile-time provable. Primary value is intent declaration and tooling (BPMN-style diamond rendering). Somewhat derivable from visible transition rows; the modifier adds declared intent that protects against accidental reduction to a single-path state.

---

#### `absorbing` — State Structural Modifier

**Syntax:**
```precept
state Closed absorbing
```

**What it means:** The state has event-handling rows but no `transition` outcomes — the entity accepts events but never changes state. Distinguished from `terminal`, which has no outgoing rows at all.

**Compile-time check:** Must have at least one row; no row may have a `transition` outcome.

**Usage illustration:** A `Closed` state that accepts `AddAuditNote` events (with `no transition` outcome) while never routing to another state. Post-closure data collection — audit notes after claim closure, feedback after hiring — is a common business need.

**Fit verdict: Tier 3.** `terminal` with in-terminal event handling already expresses this. The semantic distinction ("truly done" vs "done but still active") may not warrant a separate keyword unless tooling treats them differently.

---

#### `convergent` — State Structural Modifier

**Syntax:**
```precept
state UnderReview convergent
```

**What it means:** The state is reachable from two or more distinct source states — a merge point in the lifecycle.

**Compile-time check:** Count distinct source states with `-> transition <State>` outcomes. Error if fewer than two.

**Usage illustration:** In HiringPipeline, `Rejected` receives incoming transitions from Screening, InterviewLoop, and Decision.

**Fit verdict: Tier 3.** Fully compile-time provable but limited utility. Most convergence occurs at terminal failure states, where `terminal` + `error` already communicates the meaning. Defer.

---

### Event Modifiers

The modifier space for events is grounded in **transition row structure** — which states the event fires from, which outcomes its rows produce, which states it targets, and whether reverse paths exist in the graph. These are fully compile-time provable properties, as strong as state modifiers. The boundary between provable and not-provable falls between row-shape properties (`advancing`, `settling`) and firing-history properties (`once`, `idempotent`). The former are compile-time structural; the latter require runtime state.

#### `entry` — Event Scope Modifier

**Syntax:**
```precept
event Submit entry with Applicant as string, Amount as number
```

**What it means:** This event's rows only appear with the initial state as source. It is the lifecycle's intake event — the one that populates initial data and moves the entity out of its starting state.

**Compile-time check:** Error if any `from S on Submit` row exists where `S` is not the initial state. `from any` expansion would include the initial state but also other states — this violates `entry`.

**Runtime implications:** None.

`entry` is the natural event-side complement to the `initial` state modifier: `initial` marks where the lifecycle starts; `entry` marks the event through which it starts. If the lifecycle has a unique entry point, there is logically a corresponding event responsible for populating initial data and leaving that state.

**Usage illustration:** In a lifecycle like InsuranceClaim, `Submit` fires only from `Draft` — initializing claimant data and transitioning out of the starting state. The intake-event pattern is a structural corollary of having a single entry point.

**Fit verdict: Tier 1.** Fully compile-time provable. Captures a structurally fundamental pattern. Zero runtime cost. Clean structural parallel to `initial`.

---

#### `advancing` — Event Outcome Modifier

**Syntax:**
```precept
event Approve advancing with Amount as number
```

**What it means:** When this event fires successfully (not rejected), it always changes the entity's state. No `no transition` outcomes allowed — this is a routing event, not a data-collection event.

**Compile-time check:** Error if any row for this event has a `no transition` outcome. `reject` outcomes are allowed.

**Runtime implications:** None.

**What it enables:**
- Compiler catches accidental `no transition` rows on routing events.
- Diagrams draw advancing events as state-changing arrows exclusively.
- IntelliSense suggests `transition` outcomes when authoring rows for advancing events.
- AI generating rows for an advancing event knows to always produce `transition` outcomes.

**Usage illustration:** In InsuranceClaim, `Submit`, `Approve`, `Deny`, and `PayClaim` always transition to a new state when they succeed. The distinction between routing actions and data-collection actions is a fundamental structural split — some events exist to move the process forward, others to accumulate data within the current state. This split is the event-side manifestation of Principle #5 (data truth vs movement truth).

**Fit verdict: Tier 1.** Fully compile-time provable. Captures the fundamental distinction between lifecycle movement and data accumulation. Zero runtime cost. Clean complement to `settling`.

---

#### `settling` — Event Outcome Modifier

**Syntax:**
```precept
event LogRepairStep settling with StepName as string
event VerifyDocuments settling
```

**What it means:** When this event fires successfully, it never changes state. The event exists purely for in-state data mutation — updating fields, modifying collections, recording information without advancing the lifecycle.

**Compile-time check:** Error if any row for this event has a `transition` outcome. Only `no transition` and `reject` outcomes allowed.

**Runtime implications:** None.

**What it enables:**
- Compiler catches accidental `transition` rows on data-only events.
- Diagrams can suppress settling events from state-to-state arrows or render them as self-loops. In complex precepts, settling events create visual clutter in state diagrams; `settling` lets the renderer suppress or de-emphasize them.
- IntelliSense suggests `no transition` outcomes when authoring rows for settling events.

**Usage illustration:** In WarrantyRepairRequest, `LogRepairStep` always stays in-state while recording data. In InsuranceClaim, `RequestDocument` always produces `no transition` + collection mutation. These events exist purely to accumulate or update information within the current lifecycle phase.

**Fit verdict: Tier 1.** Fully compile-time provable. Structural complement to `advancing`. Together, `advancing` and `settling` partition the event space into "changes state" and "doesn't change state," making the lifecycle's routing structure visible at the declaration level without reading transition rows.

---

#### `isolated` — Event Scope Modifier

**Syntax:**
```precept
event FundLoan isolated
event StartRepair isolated
```

**What it means:** This event's rows all originate from the same single state. The event belongs to exactly one lifecycle phase.

**Compile-time check:** Collect all source states referenced in `from S on Event` rows. Error if more than one distinct source state appears.

**Runtime implications:** None.

**What it enables:**
- Compiler catches architectural drift — if someone adds `from UnderReview on FundLoan`, the compiler errors because FundLoan belongs to the Approved phase.
- Preview groups isolated events under their single source state.
- IntelliSense can auto-fill the `from` state when authoring rows for an isolated event.

**Usage illustration:** In LoanApplication, `FundLoan` fires only from `Approved` and `VerifyDocuments` fires only from `UnderReview`. In WarrantyRepairRequest, `LogRepairStep` fires only from `InRepair`. Single-state events are a natural consequence of lifecycle design: most events are meaningful only in a specific phase.

**Fit verdict: Tier 1.** Fully compile-time provable. Prevents architectural drift. Clean complement to `universal` — together they form a scope spectrum: `isolated` (one state) → unmodified (some states) → `universal` (all states).

---

#### `irreversible` — Event Structural Modifier

**Syntax:**
```precept
event Approve irreversible with Amount as number
```

**What it means:** Once this event fires successfully, the entity can never return to the source state. The transition is a one-way door.

**Compile-time check:** For every `from S on Event -> transition T` row, verify that `S` is not reachable from `T` in the transition graph (reverse-reachability check). Treats all edges as traversable (overapproximation). Error if any structural return path exists.

**Runtime implications:** None.

**What it enables:**
- Catches lifecycle design errors where the author intended a one-way transition but accidentally created a cycle.
- Diagrams draw irreversible transitions with special arrow styling.
- Preview labels transitions as "point of no return."

**Usage illustration:** In LoanApplication, `FundLoan` transitions from Approved → Funded with no return path. In InsuranceClaim, `PayClaim` moves from Approved → Paid irreversibly. Events that transition to terminal states are trivially irreversible by construction — `irreversible` is most useful for non-terminal one-way transitions. The compiler could hint: "This event is already irreversible because all targets are terminal."

**Fit verdict: Tier 2.** Graph-provable with overapproximation. Domain value is real — one-way transitions are a well-understood business concept. The guarantee is structural, not absolute.

---

#### `universal` — Event Scope Modifier

**Syntax:**
```precept
event Cancel universal with Reason as string nullable default null
```

**What it means:** Every reachable non-terminal state has at least one transition row for this event.

**Compile-time check:** For every reachable state that is not marked `terminal`, error if no `from S on Cancel` row exists (direct or via `from any` expansion).

**Interaction with `from any`:** If a `universal` event uses `from any on Event`, the check is automatically satisfied. `universal` adds value when the author uses individual `from` rows and might miss a state — especially when new states are added to the precept later.

**Usage illustration:** A `Cancel` event intended to be available from every non-terminal state. The `from any on Event` shorthand provides the transition-level mechanism; `universal` adds the intent-level declaration that the compiler enforces even when individual `from` rows are used — and flags immediately when a new state is added that lacks a row for the universal event.

**Fit verdict: Tier 2.** Fully compile-time provable. Useful as a safety net when new states are added. Somewhat overlaps with `from any` mechanism — the modifier adds intent-level enforcement on top.

---

#### `completing` — Event Outcome Modifier

**Syntax:**
```precept
event PayClaim completing
event FundLoan completing
```

**What it means:** Every successful transition outcome for this event targets a `terminal` state. This is the lifecycle-closing event.

**Compile-time check:** For every `from S on Event -> transition T`, error if `T` is not marked `terminal`. `no transition` outcomes are not allowed (if the intent is to END the lifecycle, staying put contradicts it). Requires `terminal` to exist in the language.

**What it enables:**
- Catches rows where a completing event accidentally transitions to an intermediate state.
- Creates a declarative lifecycle frame: `initial` → ... → `terminal`, with `entry` opening and `completing` closing.

**Usage illustration:** In LoanApplication, `FundLoan` transitions to Funded (terminal). In InsuranceClaim, `PayClaim` transitions to Paid (terminal). Lifecycle-closing events form a natural complement to lifecycle-opening ones (`entry`).

**Fit verdict: Tier 2.** Fully compile-time provable contingent on `terminal` existing. A rename of the first-consideration `final` concept — `completing` avoids collision with `final` in C#/Java semantics. The cross-row constraint concern remains valid, but is mitigated because completing events are typically single-source-state events with one terminal target.

---

#### `guarded` (event) — Event Guard Modifier

**Syntax:**
```precept
event Approve guarded with Amount as number
```

**What it means:** Every row for this event must have a `when` clause. No unguarded fallback rows allowed.

**Compile-time check:** Error if any `from S on Approve` row lacks a `when` clause.

**Design question:** Should `reject`-only rows be exempt? If yes, the modifier means "every row that can succeed must have a guard" — preserving the guarded+reject-fallback pattern. This is likely the right semantics.

**Usage illustration:** In LoanApplication, `Approve` has a compound guard (`DocumentsVerified and CreditScore >= 680`). If someone added an unguarded `from SomeState on Approve -> transition Approved` row, it would bypass the business rules. `guarded` prevents this.

**Fit verdict: Tier 2.** Fully compile-time provable. Real safety value for high-stakes events. The reject-fallback design question must be resolved before proposal.

---

#### `total` — Event Coverage Modifier

**Syntax:**
```precept
event RecordInterviewFeedback total
```

**What it means:** For every state where this event has transition rows, at least one row can succeed (produce `transition` or `no transition`). The event is never hopeless in any state that handles it.

**Compile-time check:** For every (state, event) pair, error if all rows have `reject` outcome. This is the inverse of C51 — `total` upgrades C51 from a warning to an error for this specific event.

**Interaction with `guarded`:** `total` + `guarded` is a strong combination — "every row has a guard, and in every state, at least one guarded path can succeed."

**Usage illustration:** In InsuranceClaim, `ReceiveDocument` from UnderReview has both a success row and a reject fallback. With `total`, the compiler enforces that at least one non-reject path exists in every state that handles the event.

**Fit verdict: Tier 2.** Fully provable — the C51 check inverted. Upgrades an existing warning to an author-declared requirement. Value is real but incremental over the existing C51 warning.

---

#### `symmetric` — Event Structural Modifier

**Syntax:**
```precept
event Transfer symmetric
```

**What it means:** For every `from A on Transfer -> transition B` row, some event exists with a `from B on E -> transition A` row. The transition is bidirectional.

**Compile-time check:** For every `from A on Transfer -> transition B`, check the graph adjacency map for a `from B on <anything> -> transition A` row.

**Usage illustration:** Escalation workflows (Open ↔ Escalated), assignment workflows (AssignedA ↔ AssignedB), review loops where a rejected application can be resubmitted.

**Fit verdict: Tier 3.** Fully compile-time provable but niche. Most business workflows are directionally structured. Defer — revisit if demand appears in real-world usage.

---

#### `once` — [Rejected]

**Why rejected:** Cannot be compile-time proven for general graphs with cycles. Requires runtime counter state on `PreceptInstance`. Achievable today via boolean field + `when` guard, which is explicit, visible, and consistent with the existing language model.

**Alternative:** `field IsApproved as boolean default false` + `from UnderReview on Approve when not IsApproved -> set IsApproved = true`.

---

#### `idempotent` — [Rejected]

**Why rejected:** True idempotency requires mutation commutativity analysis and guard stability proofs — undecidable in general. Narrow compile-time check (constant assignments + no transition outcome) covers only trivial cases. Better expressed as explicit guard patterns in the DSL.

---

### Field Modifiers

Fields currently have two modifiers: `nullable` and `default`. New field modifiers address the field's **lifecycle contract** — how its value evolves across the entity's lifecycle as events fire and states change.

#### `writeonce` — Field Write Constraint

**Syntax:**
```precept
field ClaimantName as string nullable writeonce
field OrderId as string nullable writeonce
```

**What it means:** After the field's value changes from its default/null to a concrete value, no subsequent transition row may set it again. The field captures identifying data that is permanent once established.

**Compile-time check:** Overapproximation: partition the graph into pre-set states (reachable from initial without passing through a row that sets the field) and post-set states (reachable from any row that sets the field). Error if any row in a post-set state sets the field again.

For the common case — field set only in initial-state rows — the check is trivially satisfied. The compiler catches the dominant violation pattern without requiring full path analysis.

**Runtime implications:** Minimal. A per-field "has been set" flag is needed only for cyclic graphs where the setting row could potentially fire twice.

**Usage illustration:** In InsuranceClaim, `ClaimantName` is set in `from Draft on Submit` and never modified thereafter. In LoanApplication, `ApplicantName` and `RequestedAmount` are established at submission and become the permanent record. Identifying data established at intake is conceptually non-revisable.

**Fit verdict: Tier 1.** Captures a structurally fundamental field lifecycle pattern: intake data is established once and becomes the permanent record. Compile-time check covers the dominant case. Minimal runtime overhead.

---

#### `sealed after <State>` — Field Lifecycle Constraint

**Syntax:**
```precept
field ApprovedAmount as number default 0 sealed after Approved
field ClaimantName as string nullable sealed after Submitted
```

**What it means:** Once the entity enters the named state (or any state reachable from it), the field can no longer be mutated. Freely writable in earlier phases, locked in later phases.

**Compile-time check:** Build the set of states reachable from the sealing state — reusing existing reachability infrastructure computed for C48. Error if any `set` action targets this field in a row whose source state is in the reachable set. Error if any `edit` declaration for a sealed state includes this field.

**Runtime implications:** None. Fully compile-time.

**Grammar consideration:** Introduces a modifier clause — `sealed after <StateName>` — rather than a boolean keyword. The parser must handle `field ... sealed after <Identifier>`.

**Design question:** Can a field be sealed after multiple states? E.g., `sealed after Approved, Denied` — meaning the field freezes when either state is entered. Consistent with existing multi-state syntax in `in S1, S2 assert ...`.

**Usage illustration:** In InsuranceClaim, `ApprovedAmount` is set in `from UnderReview on Approve` and should never change after entering Approved. Once a decision is committed — approved amount, offer salary, assessed value — that number becomes the entity's permanent record. Lifecycle freeze points are a universal concept in business process progression.

**Fit verdict: Tier 1.** Fully compile-time provable. High domain value. Reuses existing reachability infrastructure. Grammar extension is well-motivated.

---

#### `monotonic` — Field Value Shape Modifier

**Syntax:**
```precept
field FeedbackCount as number default 0 monotonic
field MissingDocuments as set of string monotonic
```

**What it means:**
- For **number** fields: value can only increase or stay the same. No `set` may produce a value less than the current value.
- For **collection** fields: items can only be added, never removed. `add`, `enqueue`, `push` allowed; `remove`, `dequeue`, `pop`, `clear` are errors.

**Compile-time check:**
- **Collections (full provability):** Error if any `remove`, `dequeue`, `pop`, or `clear` action targets this field. Fully compile-time verb check.
- **Scalars (partial provability):** Error if any `set X = <provably-decreasing expression>`. Pass on unknowns.

**Runtime implications:** None for collections. Full scalar enforcement requires a runtime comparison.

**Usage illustration:** In HiringPipeline, `FeedbackCount` only increases. A `CompletedDocuments` set that only ever grows is another natural example. Accumulation fields represent business progress and are conceptually non-reversible.

**Fit verdict: Tier 2.** Split provability. The collection case is compelling and fully provable. The scalar case requires new expression analysis. **Recommend:** If implemented, start with collections only and extend to scalars later.

---

#### `identity` — Field Lifecycle Modifier

**Syntax:**
```precept
field OrderId as string nullable identity
field ClaimantName as string nullable identity
```

**What it means:** The field is a key identifier for the entity: like `writeonce`, but with tooling prominence — identity fields appear in instance summaries, preview headers, and MCP inspect descriptions.

**Compile-time check:** Same as `writeonce` (error if set more than once). Additional: identity fields are automatically excluded from `edit` declarations.

**Design question:** Should `identity` subsume `writeonce` (the stronger form absorbs the weaker)? Subsumption is simpler and avoids redundant modifier combinations.

**Usage illustration:** In InsuranceClaim, `ClaimantName` and `ClaimAmount` serve as the entity's natural-language identity — the fields that distinguish one claim from another in human communication.

**Fit verdict: Tier 2.** Superset of `writeonce` + tooling annotations. The structural core is `writeonce`; `identity` layers tooling prominence on top. Use subsumption design — `identity` implies `writeonce`.

---

#### `sensitive` — Field Tooling Directive

**Syntax:**
```precept
field SocialSecurityNumber as string nullable sensitive
field CustomerEmail as string nullable sensitive
```

**What it means:** The field contains PII, PHI, or other sensitive data. No structural constraint on DSL usage — the modifier changes how every consumer presents the field's value.

**Compile-time check:** None. Pure tooling directive.

**What it enables:**
- `precept_inspect` and `precept_fire` mask sensitive field values in output. This prevents sensitive data from appearing in AI tool outputs, chat logs, and MCP response caches.
- Preview webview shows sensitive fields as masked by default, with an explicit reveal toggle.
- Language server hover shows a warning notice.

Principle #12 (AI is first-class consumer) means AI tools read precept data — `sensitive` gives the tooling layer an explicit signal to mask values before sending to AI models or chat histories. The design precedent: `nullable` and `default` are not "structural constraints" in the compile-verification sense — they are type-contract modifiers with tooling implications. `sensitive` extends this precedent to data-handling directives.

**Usage illustration:** A CustomerProfile precept with `Email` and `Phone` fields, or an InsuranceClaim with claimant contact data. The need for PII/PHI marking is universal in any system handling personal data.

**Fit verdict: Tier 2.** No compile-time structural check, but strong tooling value and clear Principle #12 alignment. Fit depends on the project's position on tooling-directive modifiers (see Cross-Cutting Findings).

---

#### `immutable` — Field Write Constraint

**Syntax:**
```precept
field PolicyVersion as string default "v2.1" immutable
field CreatedAt as string default "2024-01-01" immutable
```

**What it means:** The field is set at declaration time (via `default`) and cannot be changed by any transition row or `edit` declaration. A configuration constant embedded in instance data.

**Compile-time check:** Error if any `set ImmutableField = ...` appears in any transition row. Error if any `edit` declaration includes the field. Fully provable — total mutation ban.

**Runtime implications:** None. The compiler prevents all mutations.

**Design question:** `immutable` is simpler than a separate `const` declaration form. Trade-off: simplicity (reuse `field` + modifier) vs separation (constants vs evolving data). `immutable` is the default recommendation.

**Usage illustration:** A precept with `field PolicyVersion as string default "v2.1" immutable` embeds a configuration constant — set at authoring time, never modified at runtime. Real-world entities carry fixed metadata: regulatory jurisdiction codes, product category identifiers, policy version strings.

**Fit verdict: Tier 2.** Fully compile-time provable. Narrow use case but clean semantics. Simpler than a new `const` surface.

---

#### `derived` — Field Computation Modifier

**Syntax:**
```precept
field DebtToIncomeRatio as number derived = ExistingDebt / AnnualIncome
field TotalScore as number derived = CreditScore * 0.4 + FeedbackCount * 10
```

**What it means:** The field's value is always computed from other fields. No `set` action may target it. No `edit` declaration may include it. The value is re-evaluated whenever its inputs change.

**Compile-time check:** Error if any transition row contains `set DerivedField = ...`. Error if any `edit` declaration includes a derived field. If the inline expression form is used, type-check the expression and detect circular dependencies.

**Runtime implications:** Significant. New evaluation pass in the fire pipeline (after `set` mutations, before invariant checks), topological ordering of derived fields, circular dependency detection.

**Interaction with proposal #17:** `derived` is the right language-surface entry point for computed fields. The modifier keyword + inline expression syntax defines the DSL surface; computation evaluation is the runtime implementation.

**Usage illustration:** In LoanApplication, a `DebtToIncomeRatio` derived from `ExistingDebt / AnnualIncome` would replace the current inline guard expression. Computed values are well-established in domain modeling.

**Fit verdict: Tier 2 concept; Deferred to #17.** The `derived` modifier is the right design entry point. Runtime implications are substantial. Include keyword design in #17's scope; defer implementation to that proposal's timeline. As a modifier-only keyword without inline expressions, it means only "no `set` or `edit` targeting" — a weak standalone guarantee.

---

#### `audit` — Field Tooling Directive

**Syntax:**
```precept
field ApprovedAmount as number default 0 audit
field DecisionNote as string nullable audit
```

**What it means:** Every mutation to this field should be tracked — who changed it, when, from what value to what value. A metadata annotation for compliance.

**Compile-time check:** None. The modifier is metadata.

**Runtime implications:** The fire pipeline already records mutations in `FireResult`. `audit` would tag changes explicitly, enabling host application audit logging. No new infrastructure in the core runtime — just a flag on `PreceptField`.

**Usage illustration:** In a regulated deployment of InsuranceClaim, `ApprovedAmount` and `DecisionNote` would benefit from audit tracking. Field-level change tracking is a standard compliance requirement in finance, healthcare, and insurance.

**Fit verdict: Tier 3.** No compile-time structural check. Better positioned as a host-application concern than a DSL keyword. `sensitive` is a stronger entry into the tooling-directive modifier space because it has first-class platform behavior (MCP masking, preview).

---

