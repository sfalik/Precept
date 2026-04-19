# Atomic Working-Copy Pattern: Literature Survey

**Date:** 2026-04-19
**Author:** George (Runtime Dev)
**Research Angle:** Is Precept's atomic working-copy execution model a recognized pattern? What vocabulary exists in the literature for it?
**Purpose:** Ground `EngineDesign.md`'s execution model claims in external precedent. Provide vocabulary to explain the `PreceptEngine` Fire/Update model to any developer who has used Redux or implemented a DDD aggregate root.

---

## Executive Summary

**Key finding:** Precept's working-copy execution model — build a mutable scratchpad from an immutable instance, execute mutations, evaluate constraints, then promote the scratchpad to a new immutable value *only if all constraints pass* — is a recognized pattern with precise precedent in at least four distinct traditions: Redux (functional update), PostgreSQL MVCC (snapshot-before-commit), the Hickey Epochal Time Model ("Are We There Yet?"), and DDD aggregate invariant enforcement. No single source describes the exact combination, but together they provide the complete vocabulary.

**Verdict:** The Precept model is most precisely named a **constraint-gated functional update on an immutable value record**. It is neither raw optimistic concurrency nor full MVCC — it is closer to Hickey's "transient mutable scratchpad inside an otherwise pure function," combined with a post-mutation invariant gate borrowed from the DDD aggregate root tradition. The invalidation-never-existed property (no rollback needed because nothing was ever committed) is the structural distinguisher from all comparison systems.

---

## Survey Results

### 1. Redux — Three Principles (redux.js.org)

**Source:** https://redux.js.org/understanding/thinking-in-redux/three-principles  
**Retrieved:** 2026-04-19

Redux's three principles are: single source of truth (all state in one store), state is read-only (only actions can trigger change), and changes are made with pure functions (reducers: `(prevState, action) => nextState`).

The reducer model is the clearest parallel to Precept's working copy: a reducer takes the immutable prior state, produces a *new* state object, and never mutates the original. This is the working-copy pattern stripped down to its purest form.

**Critical difference:** Redux has no constraint gate. A reducer's output is always committed — if the reducer returns an invalid state, Redux stores it. There is no post-mutation evaluation step that can discard the produced state. Redux relies on the reducer author to produce only valid states; Precept enforces validity structurally.

**Precept relevance:** The Redux model establishes the vocabulary ("immutable prior state + pure function → next immutable state") and demonstrates that the pattern is idiomatic in modern frontend architecture. Any developer who knows Redux will recognize the shape of `Fire(instance, event) → instance | Failure`. The difference — the constraint gate — is Precept's distinguishing addition.

---

### 2. PostgreSQL MVCC (postgresql.org/docs)

**Source:** https://www.postgresql.org/docs/current/mvcc-intro.html  
**Retrieved:** 2026-04-19

PostgreSQL MVCC: "Each SQL statement sees a snapshot of data as it was some time ago, regardless of the current state of the underlying data." Readers see a consistent snapshot; writers never block readers; the database maintains multiple versions of rows simultaneously and readers use whichever version matches their transaction snapshot.

The structural parallel to Precept is the snapshot-before-write model: the working copy in `Fire` is a snapshot of the current instance data, mutated in isolation, then either promoted (commit) or discarded (abort). MVCC explicitly avoids the "partial write visible to readers" problem by keeping old versions alive until all transactions that reference them complete.

**Critical difference:** MVCC is primarily a concurrency isolation mechanism for multiple concurrent actors. Precept's working copy is not about concurrency — it is about integrity within a single call. There is no other transaction reading `updatedData` while `Fire` runs; the working copy is entirely local. But the structural property is the same: the prior state is never modified in place, the new state only becomes visible on success, and failure leaves the prior state untouched.

**Precept relevance:** MVCC provides the clearest vocabulary for the "snapshot → mutate → commit-or-discard" shape. "The committed instance was never written until all constraints passed" is MVCC commit semantics applied to integrity rather than concurrency. This vocabulary is familiar to any backend developer.

---

### 3. Fowler — Event Sourcing (martinfowler.com/eaaDev/EventSourcing.html)

**Source:** https://martinfowler.com/eaaDev/EventSourcing.html  
**Retrieved:** 2026-04-19

Event Sourcing: "The fundamental idea is ensuring every change to the state of an application is captured in an event object, and that these event objects are themselves stored in the sequence they were applied." State is derived by replaying events from an append-only log. Features: Complete Rebuild, Temporal Query, Event Replay.

"The key to Event Sourcing is that we guarantee that all changes to the domain objects are initiated by the event objects." Each event is processed sequentially; domain objects handle events by updating themselves; the event log is the authoritative record.

**Where the analogy is strong:** Precept's `Fire` operation is structurally event-driven — a named event with arguments triggers a pipeline that produces a new state. The event is the unit of change, and the event name has routing semantics (transition table lookup). Precept could sit underneath an event sourcing system: `Fire` produces a new `PreceptInstance` that could be stored as the materialized state for each event in the log.

**Where the analogy breaks:** Event Sourcing is a *persistence model* — its primary claim is about how state is stored and reconstructed. Precept's working-copy model is an *execution integrity model* — its primary claim is about what can become the next state. These are orthogonal. An Event Sourcing system has no constraint gate; Precept has no event log. They solve different problems and can be composed.

**Precept relevance:** Event Sourcing provides vocabulary for explaining why events are the right unit of change (`Fire` names the triggering event; `LastEvent` is recorded on `PreceptInstance`). The "processing selection logic" vs. "processing domain logic" distinction in Fowler maps neatly to Precept's separation of transition-row guard evaluation (selection) from row mutations and constraint evaluation (domain logic).

---

### 4. Fowler — DDD Aggregate (martinfowler.com/bliki/DDD_Aggregate.html)

**Source:** https://martinfowler.com/bliki/DDD_Aggregate.html  
**Retrieved:** 2026-04-19

DDD Aggregate: "A cluster of domain objects that can be treated as a single unit... An aggregate will have one of its component objects be the aggregate root. Any references from outside the aggregate should only go to the aggregate root. The root can thus ensure the integrity of the aggregate as a whole."

Microsoft's DDD validation guidance (from the same tradition) makes the invariant connection explicit: "In DDD, validation rules can be thought as invariants. The main responsibility of an aggregate is to enforce invariants across state changes for all the entities within that aggregate." Entities must always be valid; the aggregate root is the enforcement point. The Microsoft doc also illustrates the *failure mode* Precept avoids: a `SetAddress` method that partially mutates then throws leaves the object in an invalid intermediate state, because it writes before it checks.

**Where Precept matches:** `PreceptEngine` plays the role of an aggregate root — it is the only path to a new `PreceptInstance`; it enforces invariants (rules + ensures) on every state change; external callers cannot mutate instance data directly. The DDD principle "domain entities should always be valid entities" is Precept's structural guarantee: a caller who holds a `PreceptInstance` holds a value that passed all constraints at the time it was produced.

**Where Precept differs:** A DDD aggregate root is a domain object — it *is* the data, and it enforces its own rules through exception-throwing behavior methods. `PreceptEngine` is an *external engine* that governs a data record (`PreceptInstance`). The separation is deliberate: the engine is immutable and can be shared across millions of instances; the instance is a plain value record. DDD aggregates are typically mutable objects with in-place mutation and throw-on-violation; Precept's model produces a new value on success and returns a typed failure on violation, with no object mutation at any point.

**Precept relevance:** The DDD vocabulary ("aggregate root," "invariant enforcement," "consistency boundary") is the closest existing vocabulary to what `PreceptEngine` does in the .NET world. Framing Precept as "a declarative aggregate root engine that enforces invariants through a compile-time DSL rather than hand-written behavior methods" makes it immediately legible to DDD practitioners.

---

### 5. Rich Hickey — "Are We There Yet?" (JVM Language Summit 2009)

**Source:** https://github.com/matthiasn/talk-transcripts/blob/master/Hickey_Rich/AreWeThereYet.md  
**Retrieved:** 2026-04-19

The talk introduces the **Epochal Time Model**: identity is a derived concept we superimpose on a causally-related series of immutable values. "Actual entities are immutable. When you have a new thing, it's a function in that pure functional sense of the past." State = "a snapshot. This entity has this value at this point-in-time. That's state. The concept of mutable state makes no sense."

The model: an identity is governed by an atomic succession of values `v₀ → v₁ → v₂ ...` where each transition is produced by a pure function `F(vₙ, args) → vₙ₊₁`. The function is atomic — its internals are imperceptible. Hickey explicitly sanctions mutable internals inside F: "If no one can ever see what happens inside F — in other words, if it's going to take something immutable and produce something immutable and everything else about this is atomic — then nobody cares what happens inside F. You can do whatever you want."

He also introduces MVCC in the context of STM: "The key attribute of multiversion concurrency control is that readers don't impede writers. The perception doesn't impede process."

**Precept mapping is near-perfect:**
- `PreceptInstance` = Hickey's immutable value at a point-in-time
- `Fire(instance, event, args)` = Hickey's pure function F producing the next value
- The working copy (`updatedData`, `workingCollections`) = the mutable internal scratchpad inside F that no one outside can see
- `Inspect` = Hickey's "perception" — observers not in the timeline; they can look at any value without impeding the mutation process
- "The invalid configuration never existed" = Hickey's model: if F decides not to publish a new value (constraint failure), the old value simply remains; there was never a published bad state

**Precept relevance:** Hickey's vocabulary is the most complete theoretical grounding for why Precept's model is correct, not just idiomatic. "Identity is a derived construct we superimpose on a succession of valid values" is precisely the mental model for a `PreceptInstance`'s lifecycle. The working copy is philosophically justified: it's the internal mechanism of F, invisible to all observers, producing the next immutable value or producing nothing.

---

### 6. Microsoft — DDD Domain Model Layer Validations (.NET microservices guide)

**Source:** https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations  
**Retrieved:** 2026-04-19

The Microsoft .NET microservices guide on DDD domain model validation addresses the same problem space. It describes the anti-pattern Precept avoids: a method that partially modifies an entity then throws, leaving the entity in an invalid intermediate state. It then recommends the Notification pattern (collect all violations, return them together) rather than early exception-throwing.

Validation should occur in entity constructors or behavior methods; the Specification pattern can make rules composable; the Notification pattern returns a collection of errors rather than a single exception.

**Direct mapping:** Precept's `ConstraintFailure` outcome — returning the full set of violations, not just the first — implements the Notification pattern at the engine level. The engine collects all `CollectConstraintViolations` results and returns them in a single typed outcome, with per-field violation messages. This is the Notification pattern without the caller needing to implement it; the engine provides it structurally.

The guide also observes that in DDD, "validation rules can be thought as invariants" — identical to Precept's terminology in `EngineDesign.md` and `philosophy.md`.

**Precept relevance:** This source is the most directly actionable for `.NET` developers reading Precept documentation. The pattern it describes as "best practice" is what Precept provides automatically. The anti-pattern it warns against (partial mutation + throw) is structurally impossible in Precept.

---

## Synthesis: Pattern Classification

The Precept working-copy model combines elements from several recognized patterns. No single term in the existing literature covers it exactly. The correct classification depends on which audience is being addressed:

**For functional programming / Redux audience:**
The model is a **constraint-validated pure function producing an immutable successor value**. `Fire` is a reducer with a constraint gate: `(PreceptInstance, event, args) → PreceptInstance | ConstraintFailure | Rejected | ...`. The working copy is the mutable scratchpad inside an otherwise pure function — explicitly sanctioned by the Hickey formulation.

**For database / backend audience:**
The model is a **snapshot-isolated atomic transaction with an integrity gate instead of a conflict detector**. The working copy is the transaction's private write buffer; the constraint evaluation is the commit condition; "discard on failure" is the abort path. The key departure from MVCC: the gate is domain invariant evaluation, not concurrent-write conflict detection.

**For DDD / .NET audience:**
The model is a **declarative aggregate root engine with a post-mutation invariant gate and rollback-free failure**. `PreceptEngine` is the aggregate root; `rules` and `ensures` are the invariants; the Notification pattern (full violation collection) is the failure surface; `PreceptInstance` is the always-valid aggregate snapshot.

**Formal name for EngineDesign.md:** **Constraint-gated functional update.** A functional update (old immutable value + function → new immutable value) with the addition of a constraint gate that can suppress the new value from being published. The gate runs against the candidate next value before it is promoted to the public record.

**Is it optimistic concurrency?** Partially. The "assume success, validate at commit" shape is optimistic in spirit. But optimistic concurrency is about *conflict detection* between concurrent writers; Precept's gate is about *invariant evaluation* with no concurrent writer consideration. The term "optimistic" can be used to describe the execution order (mutate first, check after) but carries wrong connotations about what is being detected.

---

## Properties

These are the formal properties of Precept's constraint-gated functional update model. They are guaranteed by the implementation, not aspirational:

**P1 — Atomicity.** A Fire or Update operation either fully succeeds (all mutations committed, new `PreceptInstance` returned) or fully fails (no mutations visible, original `PreceptInstance` unchanged). There is no partial commit — no state where some mutations are observable and others are not.

**P2 — Caller isolation.** The caller's `PreceptInstance` is never modified at any step of the operation, including during mutation execution and constraint evaluation. The caller can hold references to any number of historical `PreceptInstance` values from the same engine; none are invalidated by subsequent operations.

**P3 — Invalidation-never-existed (rollback-free failure).** On constraint failure, the working copy is discarded; there is nothing to roll back. The invalid configuration never existed in the public record — it was only ever a private scratchpad. This is structurally stronger than "write-then-rollback" patterns, which have a window where the invalid state exists and requires compensating mutations.

**P4 — Honest simulation (observer not in timeline).** `Inspect` executes the complete Fire pipeline on a working copy but does not promote the result. Because the working copy is private to the call, `Inspect` cannot produce any observable effect, making the "no commitment" guarantee architectural rather than disciplinary. Predictions are always honest because they run the real pipeline.

**P5 — Determinism.** Given the same compiled engine, the same `PreceptInstance`, the same event name, and the same event arguments, Fire always produces the same outcome. The evaluator is expression-isolated (no side effects, no external observations); the working copy captures all mutable state relevant to the operation; no external state can change the result between two calls with identical inputs.

**P6 — Notification-pattern violation surface.** On constraint failure, all violated rules and ensures are collected before returning. Callers receive the full violation set, not just the first. No partial evaluation is aborted early on the first failure.

---

## Comparison Table

| Dimension | Precept (PreceptEngine) | Redux | PostgreSQL MVCC | DDD Aggregate Root |
|---|---|---|---|---|
| **Prior state mutated** | Never — always a working copy | Never — reducer returns new object | Never — snapshot isolation | Typically yes — in-place mutation |
| **Working copy** | Explicit: `updatedData` dict + cloned `workingCollections` | Implicit: reducer builds new object literal | Implicit: transaction's private write buffer | Usually absent — entity is the mutable target |
| **Constraint gate** | Explicit: all `rules` + `ensures` must pass | None — any reducer output is committed | Conflict detection only (no domain invariants) | Ad-hoc: throw in behavior method, possibly after partial mutation |
| **On failure** | Discard working copy; return typed outcome with full violation set | N/A (pure reducers don't fail) | Rollback transaction | Exception thrown; entity may be in partial-mutation state |
| **Caller sees prior state on failure** | Yes — always (P2) | N/A | Yes | Not guaranteed — depends on exception timing |
| **Simulate without commit** | Yes — `Inspect` runs full pipeline, skips promotion | No (time-travel via state snapshots is different) | `SELECT ... FOR SHARE` / read-only transactions | No standard equivalent |
| **Immutability of produced value** | Yes — `PreceptInstance` is a sealed record | Yes — new state object returned from reducer | N/A (MVCC version chain, not returned) | No — aggregate root is typically a mutable object |
| **Outcome vocabulary** | 6 typed outcomes with diagnostic distinctions | Single return value (new state) | Success / conflict / deadlock | Success / exception |
| **Invariant definition location** | Compiled DSL (`rules`, `ensures`) | None — ad-hoc in reducer body | Database constraints (schema-level) | Hand-written in aggregate root behavior methods |
| **Rollback needed on failure** | No — working copy is discarded | N/A | Yes — transaction rollback | Sometimes — depends on how far mutation progressed |

### What's shared across all four

All four traditions converge on the same fundamental insight: **the prior state must be protected from partially-applied mutations**. Redux, MVCC, and Hickey all achieve this through immutability of the prior value; DDD aggregate design achieves it (imperfectly) through exception-throwing on the first violation. Precept achieves it through the working copy pattern with an explicit constraint gate, combining the immutability guarantee of Redux/Hickey with the domain invariant semantics of DDD.

### What is distinctly Precept's

1. The **constraint gate is post-mutation, not pre-mutation**: invariants are evaluated against the fully mutated working copy, including computed field values that depend on the mutations. This means invariants can reference derived state (e.g., a rule that checks a computed sum) rather than just input values.

2. The **typed outcome taxonomy** (6 outcomes with diagnostic distinctions) is absent from all comparison systems. Redux returns a new state. MVCC returns success or conflict. DDD aggregates throw or succeed. Precept returns `Rejected` vs `ConstraintFailure` vs `Unmatched` vs `Undefined` — because callers need to respond differently to each.

3. **Inspect as first-class operation**: full pipeline simulation without commitment is not available in any of the comparison systems as a structural guarantee. Precept's `Inspect` is honest (runs the real pipeline) and free (working copy private to call, guaranteed no side effects).

---

## Implications for EngineDesign.md

The following claims should be expressible in `EngineDesign.md` with this research as grounding. Documented Assumptions A3 and A4 are specifically addressed.

**On §Atomic Execution Model:**

The "rollback-free discard" property should be named explicitly: this is stronger than transactional rollback. Rollback requires the system to know what was written and undo it; discard requires nothing because the working copy was never promoted. The term "invalidation-never-existed" (P3 above) is the precise claim.

The model can be stated in Hickey's vocabulary for readers who know it: "Fire and Update are instances of the Epochal Time Model's pure function F. The working copy is the transient mutable internal of F, invisible to all observers. Constraint failure means F produces no successor value; the identity (the entity) remains at its prior value."

**On §Philosophy-Rooted Design Principle 3:**

The principle currently states "atomic execution with rollback-free discard." This should note that this is the structural mechanism for "invalid configurations structurally impossible" — not just at the compile-time gate, but at every runtime mutation. The working copy model extends the compile-time prevention guarantee into the run-time phase: no mutation can produce an observable invalid state, because observable requires promotion, and promotion requires passing the constraint gate.

**On Documented Assumption A3 (read-your-writes):**

The "read-your-writes" semantics within a row (later assignments can observe earlier assignments in the same row) is bounded within a single atomic working copy operation. This is safe precisely because of the working copy model: all reads-of-own-writes occur against the private scratchpad; no observer can see the intermediate states. If this caused a constraint failure, the entire working copy would be discarded anyway. The transactionality of the operation is what makes sequential mutation semantics safe to expose.

**On Documented Assumption A4 (hydrate/dehydrate dual format):**

The internal format (collections as typed `CollectionValue` objects) is another instance of the same principle: it is the private representation inside F. Callers only ever see the public format (`List<object>`). The hydrate/dehydrate boundary enforces that internal representation never leaks to the public record — structurally analogous to MVCC's separation between internal row versions and what a SELECT returns.

**Claims the doc should make using this vocabulary:**

1. "`Fire` and `Update` implement a constraint-gated functional update: an immutable predecessor value plus a pure function (with a mutable internal scratchpad) produces either an immutable successor value or a typed failure. This is the Epochal Time Model applied to governed entity lifecycle."

2. "The working copy is never observable by callers. Constraint evaluation runs against the working copy, not the committed record. A constraint failure means the working copy is discarded — there is nothing to roll back and no window where an invalid state existed in the public record."

3. "This model is structurally distinct from the DDD aggregate pattern where behavior methods partially mutate the aggregate then throw on validation failure (the Microsoft anti-pattern: `SetAddress` that writes `line1` and `city` before validating `state`). Precept never partially writes an observable record."

4. "Inspect is the direct realization of the Hickey principle that observers are not in the timeline. It runs the full pipeline on a private working copy and returns the predicted outcome. The guarantee is architectural: there is no mechanism by which `Inspect` can affect the original instance, because the working copy is local to the call."

---

## References

1. **Redux — Three Principles.** https://redux.js.org/understanding/thinking-in-redux/three-principles  
   Principles: single source of truth, state is read-only, pure functions (reducers). Establishes the `(prevState, action) → nextState` vocabulary.

2. **PostgreSQL — MVCC Introduction.** https://www.postgresql.org/docs/current/mvcc-intro.html  
   MVCC snapshot-before-commit model. Vocabulary for "each statement sees a snapshot as it was, not the current state of underlying data."

3. **Martin Fowler — Event Sourcing.** https://martinfowler.com/eaaDev/EventSourcing.html  
   Event-as-unit-of-change model. "Processing selection logic" (guard evaluation) vs. "processing domain logic" (mutations). Relevant for why Fire names an event and records it on the instance.

4. **Martin Fowler — DDD Aggregate.** https://martinfowler.com/bliki/DDD_Aggregate.html  
   Aggregate root as integrity enforcement boundary. "The root can thus ensure the integrity of the aggregate as a whole."

5. **Rich Hickey — "Are We There Yet?" (JVM Language Summit 2009).** Transcript: https://github.com/matthiasn/talk-transcripts/blob/master/Hickey_Rich/AreWeThereYet.md  
   Epochal Time Model. Immutable values as points-in-time; identity as a derived concept; pure function F producing the next value; "if no one can see what happens inside F, you can do whatever you want inside F." The single most complete theoretical grounding for Precept's model.

6. **Microsoft — Design validations in the domain model layer.** https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations  
   DDD invariant enforcement in .NET. The `SetAddress` anti-pattern (partial mutation + throw) explicitly shows what Precept's working copy model avoids. Notification pattern as the recommended violation surface.

7. **Precept Engine Design.** `docs/EngineDesign.md`  
   §Atomic Execution Model, §Fire, §Update, §Philosophy-Rooted Design Principles, §Documented Assumptions A3 and A4.

8. **Precept Architecture Design.** `docs/ArchitectureDesign.md`  
   §Run-Time Phase — four operations (CreateInstance, Inspect, Fire, Update), their commit semantics, and the architecture position of PreceptEngine as the boundary between compile-time and run-time phases.
