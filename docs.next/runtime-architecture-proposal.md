# Precept Runtime Architecture Plan

**Date:** 2026-04-22
**Status:** Pre-design planning artifact — identifies what must be designed, in what order, grounded in what evidence
**Inputs:** Language Vision (`docs.next/precept-language-vision.md`), Runtime Evaluator Architecture Survey (`research/architecture/runtime/runtime-evaluator-architecture-survey.md`), Compiler Design Plan (`docs.next/compiler-architecture-proposal.md`)

---

## Framing

This is a planning document, not a design document. Its job is to answer: *what runtime design work needs to happen, in what order, and with what grounding?* The design documents themselves come after this plan is accepted.

The language vision is the authoritative specification of what the runtime must serve. The runtime evaluator architecture survey (10 external systems across 8 dimensions) is the authoritative source of precedent. The compiler design plan establishes the pipeline boundary — the emitter's output is the runtime's input — and is treated as committed context. All claims are grounded in one of these three sources.

### What this document covers

The runtime: everything downstream of the compiler's emitter output. The executable model that the evaluator walks, the evaluator itself, entity representation, result types, constraint evaluation, working copy atomicity, fault production, and the public API surface.

### What this document intentionally excludes

- Compiler internals (covered by the compiler design plan)
- Language server integration (the LS consumes the compilation result, not the executable model)
- MCP tool shapes (MCP tools wrap runtime APIs but don't constrain their internal design)
- Persistence, serialization, or host-application integration
- The current prototype's implementation decisions

### Committed context treated as given

The compiler pipeline, its stage ordering, and its two-artifact split are committed:

- **Compilation Result** — the tooling-surface artifact (diagnostics, typed model, proof, graph). Consumed by LS and MCP. Produced on every pipeline run.
- **Executable Model** — the runtime-surface artifact (dispatch tables, slot-indexed expression trees, scope-indexed constraints). Sealed, immutable, evaluation-ready. Only produced on error-free compilation.

The fault-correspondence chain is committed: `FaultCode` ↔ `DiagnosticCode` via `[StaticallyPreventable]`, with analyzers PREC0001 and PREC0002 enforcing structural coverage. The compiler design plan's correctness invariant is committed: `inspect` MUST consume the executable model via the same evaluation path as `fire`, guaranteeing structural semantic agreement.

### Provisional stubs under evaluation

The committed stubs (`Precept`, `Version`, `Fault`) represent early shapes. This plan evaluates them against survey evidence and identifies where they may need revision. The stubs are not authoritative — the survey evidence and language vision are.

---

## 1. Runtime Shape

The runtime has four major components in dependency order, with a clear boundary between the compiler's output and the runtime's input.

### 1.1 Executable Model — the compiler/runtime boundary

The executable model is the sealed, immutable artifact the emitter produces and the evaluator consumes. The compiler design plan specifies its contents: transition dispatch table, slot-indexed expression trees, scope-indexed constraint lists, field descriptor array, topological action chains. The executable model is to the runtime what CEL's `Program` is to `Program.Eval()` — a compiled representation that an evaluator runs against entity data to produce outcomes.

The executable model is NOT the entity's data. It is the definition's compiled rules. A single executable model serves all instances of the same precept definition. This mirrors CEL's architecture: one `Program` evaluated against many `Activation` bindings.

### 1.2 Evaluator — the execution engine

The evaluator walks the executable model's expression trees against entity data to produce outcomes. The language vision's execution model properties (no loops, no branches, no reconverging flow, expression purity, finite state space) make tree-walking interpretation the natural strategy — confirmed by CEL's `Interpretable.Eval(Activation)` pattern operating under similar constraints.

The key architectural question is whether the evaluator is a static utility or an instance object. Survey evidence splits:

- **Static/functional:** CEL's `Program.Eval(activation)`, XState's `transition(machine, state, event)`, and Dhall's `eval(env, expr)` are all stateless function calls. The evaluator holds no per-invocation state.
- **Instance-based:** Drools' `KieSession`, OPA's `PreparedEvalQuery` with store transactions, and Temporal's workflow instances carry evaluation-scoped state (working memory, transaction context, replay history).

For Precept, the evaluator's per-invocation state is the working copy — a mutable scratch space for atomic mutation. This is intrinsically per-operation, not shared. The question is whether it lives inside the evaluator or is passed through it.

### 1.3 Entity representation — the data envelope

The language vision requires atomic mutation semantics: all mutations execute on a working copy; constraints are evaluated against the working copy; if all pass, the working copy is promoted; if any fail, it is discarded. "An invalid configuration never exists, even transiently."

The entity representation must capture: current state (for stateful precepts), current field data, and the precept reference (which executable model governs this entity). The provisional `Version` stub captures this as an immutable record: `Version(Precept, State, Data)`. Operations return a new `Version` — the input is never mutated.

Survey evidence strongly favors immutable snapshots for this pattern:
- XState v5's `MachineSnapshot` is immutable; `transition()` returns a new snapshot.
- CEL evaluations produce fresh results from immutable activations.
- CUE's `Value.Unify()` creates new values; inputs are never modified.

The working copy is the internal mutation mechanism hidden behind the immutable input/output interface. The caller sees `Version → Version`; internally, the evaluator clones to a working copy, mutates, validates, and either promotes or discards.

### 1.4 Public API surface — what callers touch

The language vision specifies three operations and one structural requirement:

1. **Fire** — send an event to an entity, producing a new version or an outcome explaining why not.
2. **Edit** — directly mutate an editable field, producing a new version or an outcome explaining why not.
3. **Inspect** — preview what every event would do from the current state without committing. Must have the same depth as fire: guard evaluation, exit actions, mutations, entry actions, computed field recomputation, constraint evaluation.
4. **Construction** — create an executable model from a compilation result; create an initial entity version from an executable model.

The compiler design plan's correctness invariant requires inspect and fire to share the same evaluation path, differing only in whether the working copy is promoted or discarded. This is a structural guarantee, not a testing convention.

### 1.5 Inspect/Fire relationship

The language vision is explicit: "Inspection is not a reporting layer — it is a fundamental language operation. It must have the same depth as event execution." The compiler design plan locks this further: "inspect MUST go through the full pipeline including the emitter."

Survey evidence provides two patterns:

- **XState v5's pure `transition()` function.** `transition(machine, state, event)` returns `[nextSnapshot, actions]` without side effects. `getNextSnapshot()` returns just the snapshot for preview. The same evaluation code path serves both — the difference is whether actions are executed. This is the closest structural analog to Precept's requirement.
- **CEL's exhaustive evaluation mode.** `ExhaustiveEval` forces all branches to evaluate regardless of short-circuit, producing a complete trace. The same `Interpretable` tree is walked; only the evaluation mode flag differs.

For Precept, the natural unification is: fire and inspect share the evaluation path up to and including constraint checking on the working copy. Fire promotes the working copy on success; inspect always discards it. The outcome record is identical in both cases — inspect produces the same structured result that fire would, minus the committed state change.

---

## 2. Component Inventory

### 2.1 Executable Model

The emitter's output / evaluator's input. Specified by the compiler design plan as containing: transition dispatch table keyed by `(state, event)`, slot-indexed expression trees, scope-indexed constraint lists, field descriptor array, topological action chains for computed fields, entry/exit action chains per state.

**Design questions that must be resolved:**

- What is the type-level contract between the executable model and the evaluator? What can the evaluator assume without checking? The compiler design plan's D8 (emitter contract) directly governs this — every assumption the evaluator makes about the model's structure must be guaranteed by the emitter.
- How are stateless precepts represented? They lack states and transition routing but have events, hooks, editability, rules, and fields. Does the executable model use a degenerate single-state representation, or does statelessness have its own model shape?
- How is computed field dependency ordering represented? The compiler linearizes the dependency graph topologically. Is this stored as an ordered list of slot indices, or as a more structured dependency graph that the evaluator traverses?
- Does the executable model carry proof results? The compiler design plan raises this question: proof results serve both tooling (hover diagnostics) and potentially runtime (skip redundant checks based on proven ranges). If carried, they could enable the evaluator to skip constraint checks that the proof engine has already proven always-pass — but this optimization adds coupling between the proof model and the evaluation path.
- What metadata does the executable model carry for result attribution? Constraint violation subject attribution requires knowing which fields a rule references, which state scope an ensure belongs to, and what anchor (in/to/from) applies. This metadata must survive lowering.

**Research grounding:**

- CEL's `Program` is the closest analog — a compiled expression with pre-resolved types that an evaluator walks without re-parsing. CEL preserves AST node IDs through lowering for evaluation-time correlation (`EvalState` maps IDs to observed values).
- OPA's `PreparedEvalQuery` compiles and caches the query plan. Evaluation against different inputs reuses the plan.
- The compiler design plan notes: "If slot numbers replace expression references and source identity is dropped, LS-to-evaluation correlation breaks." Node identity preservation through lowering is an explicit concern.

**Right-sizing:** The executable model is a read-only data structure. Its design complexity is in the contract specification (what invariants it guarantees), not in the data structure implementation. At Precept's scale (5–20 states, dozens of fields, hundreds of expression nodes), the dispatch table is a small map and the expression trees are shallow.

### 2.2 Evaluator

The evaluator walks the executable model's expression trees against entity data. The language vision's execution model properties make this a straightforward tree-walking interpreter: no loops means no iteration state, no branches means no join-point merging, expression purity means no side-effect tracking.

**Design questions that must be resolved:**

- Static utility vs. instance object? A static evaluator is a pure function: `Evaluate(model, state, data, event, args) → Outcome`. An instance evaluator holds configuration or cached state. Survey evidence:
  - CEL: `Program.Eval(activation)` — the `Program` is the instance, evaluation is a method call. But `Program` is the compiled model, not the evaluator. The evaluator (`Interpretable.Eval`) is stateless tree-walking.
  - XState: `transition(machine, state, event)` is a pure function. No evaluator instance.
  - Dhall: `eval(env, expr)` is a pure function. Total — always succeeds after type checking.
  - Drools: `KieSession.fireAllRules()` is deeply stateful — working memory, agenda, truth maintenance. Not applicable to Precept's pure expression model.

- What does the evaluator return? The language vision's outcome taxonomy (§ Outcomes and Semantic Verdicts) identifies 9 distinct outcomes. The evaluator must produce a result that structurally distinguishes all 9. This is a result type design question (see §2.4).

- How does the evaluator handle the action chain? Actions are sequenced — each action sees the state produced by all preceding actions. The evaluator must walk the action chain left-to-right, updating the working copy at each step, and recording proof-state invalidation on reassignment. This is a linear walk, not a graph traversal — confirmed by the language vision's "no reconverging flow" property.

- How does the evaluator handle computed fields? After mutations, computed fields must be recomputed in topological order. The executable model provides the linearized order. The evaluator walks this list, evaluating each computed field's expression against the current working copy state, and writing the result back to the working copy.

- Trust boundary with the type checker. The language vision's Principle 11 (Static Completeness): "If a precept compiles without diagnostics, it does not produce type errors at runtime." The committed fault-correspondence chain formalizes this: every `FaultCode` has `[StaticallyPreventable]` linking it to a `DiagnosticCode`. The evaluator's type checks are defensive redundancy. The question is: does the evaluator carry runtime type tags and check them, or does it trust slot types unconditionally? Survey evidence:
  - Dhall: total correspondence — evaluator has NO error paths. `eval` is a total function.
  - SPARK Ada: runtime checks are proved unnecessary by static verification and can be compiled out.
  - CEL: partial correspondence — type checker catches type faults, evaluator still handles value faults (division by zero, overflow).

  Precept sits between Dhall and CEL: the type checker catches all type errors, but value-dependent faults (division by zero despite proven-safe denominators, overflow) remain as defensive paths. The evaluator must handle these via the `FaultCode` mechanism even though they should be unreachable on well-compiled input.

**Research grounding:**

- CEL's tree-walking interpreter with `Interpretable.Eval(Activation)` is the primary structural reference. The `Activation` interface provides variable resolution — Precept's equivalent is slot-index-based field lookup in the working copy.
- XState's pure `transition()` function returning `[snapshot, actions]` is the operational reference — the evaluator is a pure function from (model, state, event) to (outcome).
- Dhall's total evaluator demonstrates what Principle 11 enables: if the type checker is complete, the evaluator's error paths are unreachable. Precept cannot achieve Dhall's totality (value-dependent faults remain), but the correspondence chain bounds the gap.

**Right-sizing:** Tree-walking interpretation over slot-indexed expression trees with no loops, no recursion, and finite depth. CEL's evaluator at ~5K lines (within its 30–40K total) is the scale reference. The evaluator is algorithmically simple; the design complexity is in the result type taxonomy and the working copy interaction.

### 2.3 Entity Representation

The entity is the data envelope: current state + current field data + reference to the governing executable model. Operations produce new entity snapshots — the input is never mutated.

**Design questions that must be resolved:**

- Immutable record vs. mutable-with-copy-on-write? The language vision requires that callers never see partially-mutated state. Survey evidence overwhelmingly favors immutable snapshots:
  - XState: `MachineSnapshot` is immutable. `transition()` returns a new snapshot.
  - CEL: activations are immutable; evaluation produces fresh results.
  - CUE: `Value.Unify()` creates new values.
  - Dhall: fully immutable; `eval` produces a new `Val`.
  
  The only stateful alternatives are Drools (`KieSession` with mutable working memory) and Temporal (replay-based mutation) — neither fits Precept's single-step atomic model.

- Slot array vs. dictionary for field storage? The compiler design plan specifies that the emitter resolves field name references to slot indices. This enables array-based storage: `object?[slotCount]` instead of `Dictionary<string, object?>`. Array access is O(1) with no hashing overhead. Survey evidence:
  - CEL: activations are map-based (string → value), because CEL operates in a dynamic schema context where field sets vary per invocation.
  - XState: context is a typed object (TypeScript record), not a map.
  - For Precept, the fixed field set per executable model makes slot arrays natural. The field count is known at compile time. Slot-indexed access eliminates string lookups in the hot path.

- What does the initial entity look like? The language vision specifies default values, initial state, and compile-time rule enforcement against defaults. The construction path must: create a field array from declared defaults, set the initial state, compute computed fields in topological order, and validate all applicable rules and entry-ensures against the initial configuration.

- Does the entity carry its own state name, or just a state index? For API consumers, state names are meaningful. For the evaluator, state indices enable O(1) dispatch table lookup. The entity likely carries both — an internal slot index for evaluation and a name for external consumption.

- Stateless entities. Stateless precepts have no state field. The entity representation must handle both stateful (`state + fields + model`) and stateless (`fields + model`) forms. Options: a single type with an optional state field, two sibling types, or a common base with specialization. The XState pattern (state machines are one `ActorLogic` implementation among several) suggests a unified interface with stateless as a degenerate case.

**Research grounding:**

- XState's `MachineSnapshot` is the primary structural reference: immutable, carries state value + context data + status + metadata.
- CEL's `Activation` interface (hierarchical, stackable variable bindings) informs the working-copy-as-overlay pattern.
- The provisional `Version` stub (`Version(Precept, State, Data)`) is structurally aligned with XState's snapshot model. Whether `Data` should be `ImmutableDictionary<string, object?>` or a slot array is the main design tension.

**Right-sizing:** An immutable record with a slot array and a state discriminator. The design complexity is in the construction path (defaults + initial validation) and the working copy interaction, not in the entity type itself.

### 2.4 Result Types (Outcome Representation)

The language vision identifies 9 semantically distinct outcomes. The runtime must structurally distinguish all 9 — not via string codes or integer discriminators, but as distinct types that callers can pattern-match against.

**Design questions that must be resolved:**

- Sealed hierarchy vs. discriminated union? C# offers two patterns:
  - **Sealed class hierarchy:** `abstract record Outcome` with sealed subtypes (`Transitioned`, `Applied`, `Rejected`, `ConstraintViolation`, etc.). Pattern-matchable via `switch` expressions. This is the idiomatic C# pattern for closed type families.
  - **Tagged union via enum + payload:** A single result type with an outcome-code enum and variant-specific data in separate properties. Less type-safe but simpler.

  Survey evidence:
  - CEL uses a three-value return `(ref.Val, *EvalDetails, error)` — infrastructure errors are separate from evaluation errors, which are separate from success. This is a layered result, not a type hierarchy.
  - XState uses `status: 'active' | 'done' | 'error'` on the snapshot — a small enum on a unified type.
  - CUE uses `Value` which may be `Bottom` — a single type that carries its error state.
  - Eiffel distinguishes contract violations by kind (precondition, postcondition, invariant) with blame assignment.

  The 9-outcome requirement exceeds what any surveyed system provides. CEL's 3 categories (success, value-error, infra-error) and XState's 3 statuses (active, done, error) are structurally simpler. Precept must design its own taxonomy.

- Per-operation or unified result type? Should `Fire`, `Edit`, and `Inspect` return the same result type, or does each operation have its own result family? The 9 outcomes are not all applicable to all operations: "Successful transition" applies to fire but not edit; "Uneditable-field failure" applies to edit but not fire. Options:
  - Unified: one `Outcome` type with all 9 variants, and each operation can produce a subset. Simpler API, more runtime variants to handle.
  - Per-operation: `FireOutcome`, `EditOutcome`, `InspectOutcome` with only their applicable variants. More type-safe, more types to design.
  
  The inspect/fire unification requirement complicates this: if inspect and fire share the evaluation path, they naturally produce the same result type.

- What does a successful outcome carry? The new entity version. What does a failure outcome carry? The violation details — including constraint violation subject attribution (semantic subjects, scope, anchor) as specified by the language vision. What does a rejection carry? The reject statement's reason. Each outcome variant has different payload requirements.

- How does the result type interact with the committed `Fault` type? The `Fault` record (`Fault(FaultCode, CodeName, Message)`) represents runtime faults produced by the evaluator's defensive paths. These are distinct from business-logic outcomes (constraint violations, rejections, unmatched events). A fault is a "this should have been caught at compile time" situation; a constraint violation is normal operation. The result type must keep these separate.

**Research grounding:**

- The compiler design plan specifies: "Outcome type hierarchy — 9 outcomes must be structurally distinguishable at the C# type level (sealed hierarchy, not string codes)."
- CEL's three-value return demonstrates the importance of separating infrastructure failures from domain-level results.
- Eiffel's blame model (client vs. supplier) maps to Precept's distinction between rejection (authored decision) and constraint violation (data truth violation) — both are "constraint failed" but they assign causality differently.
- XState's `can(event)` method returns a simple boolean for preview — far less information than Precept's inspect requires, but illustrates the demand for preview-specific result shapes.

**Right-sizing:** 9 outcome types is more than any surveyed system, but Precept's language vision makes each distinction semantically meaningful (rejection vs. constraint failure vs. unmatched vs. undefined — each requires different caller response). A sealed hierarchy with 9 leaf types is a bounded, designable surface.

### 2.5 Constraint Evaluator

The language vision distinguishes two constraint evaluation modes: collect-all (rules and ensures) and first-match (transition routing). The constraint evaluator handles the collect-all surface — evaluating all applicable constraints and collecting all violations with attribution.

**Design questions that must be resolved:**

- Collect-all evaluation order. All applicable constraints must be evaluated regardless of intermediate failures. This means: evaluate every rule whose guard (if any) is satisfied; evaluate every state ensure applicable to the target state; report all violations, not just the first. Survey evidence:
  - OPA's `deny[msg]` pattern collects all violation messages into a set — all failing constraints are collected.
  - CUE's unification accumulates all conflicts into a `Bottom` value.
  - Drools' rule network evaluates all matching patterns.
  - XState's guard model is first-match — the first satisfied guard wins. This is the WRONG model for constraint evaluation (it is the right model for transition routing).
  - Eiffel evaluates one assertion at a time and throws on the first failure — the WRONG model for collect-all.

- Constraint violation record shape. The language vision specifies semantic subject attribution with four distinct constraint kinds: event ensures (target args + event scope), rules (target fields + definition scope), state ensures (target fields + state scope with anchor: in/to/from), and transition rejections (target event as a whole). Each violation record must carry: the violated constraint's expression or identifier, the `because` message, the semantic subjects, the scope kind, and the anchor. Survey evidence:
  - SPARK Ada's proof output carries per-obligation results with source locations, prover information, and category — the most structured attribution of any surveyed system.
  - Eiffel's contract violations carry a label, class, routine, and failing expression.
  - No surveyed system provides the semantic-subject-level attribution that Precept requires. This is purpose-built.

- How are computed field references expanded? The language vision specifies: "Computed fields referenced in constraints are also targets, with transitive expansion to the concrete stored fields they depend on." If rule `Total >= 100` references computed field `Total` which depends on `Price` and `Quantity`, the violation should attribute the violation to `Price` and `Quantity` (the stored fields the user can actually change). This requires the constraint evaluator to traverse the dependency graph — or the executable model must pre-compute this transitive expansion.

- Constraint scope bucketing. The executable model pre-buckets constraints by scope: `constraintsFor[state]`, `rulesFor[event]`, etc. The constraint evaluator must know which buckets apply to a given operation. For a fire operation transitioning from state A to state B: global rules + `from A` ensures + `to B` ensures + event ensures all apply. For an edit operation in state A: global rules + `in A` ensures apply. The evaluator must compose the correct constraint set for each operation type.

**Research grounding:**

- OPA's collect-all `deny[msg]` pattern is the primary reference for collect-all constraint evaluation.
- CUE's unification-based constraint model, where `Bottom` accumulates all conflicts, demonstrates how constraint violations can be aggregated structurally.
- SPARK's per-obligation result tracking with category, severity, and source location is the reference for structured attribution.
- The compiler design plan specifies: "Constraint violation attribution record — semantic subjects + scope + anchor as structured data, consumable by diagnostics, hover, and MCP."

**Right-sizing:** The constraint evaluator is a loop over pre-bucketed constraints with working-copy evaluation. The algorithmic complexity is low (evaluate each constraint, collect results). The design complexity is in the violation record shape and the transitive attribution expansion.

### 2.6 Working Copy / Atomicity Mechanism

The language vision's atomicity guarantee: "All mutations execute on a working copy. Constraints are evaluated against the working copy after all mutations complete. If every constraint passes, the working copy is promoted. If any constraint fails, the working copy is discarded."

**Design questions that must be resolved:**

- Working copy representation. The working copy is a mutable clone of the entity's field data. Options:
  - **Full clone:** Copy the entire slot array at the start of each operation. Mutations write directly into the clone. Simple and correct. At Precept's scale (dozens of fields, each a scalar or small collection), full clone is cheap.
  - **Copy-on-write overlay:** Keep a sparse overlay of modified slots. Reads check the overlay first, then the original. More memory-efficient for large entities with few mutations, but adds indirection on every read. Survey evidence:
    - CEL's hierarchical `Activation` (`NewHierarchicalActivation(parent, child)`) is a copy-on-write overlay — child bindings shadow parent bindings.
    - OPA's store transactions provide snapshot isolation — reads within a transaction see a consistent snapshot.
  - **Slot array copy:** Since the emitter resolves fields to slot indices, the working copy is `object?[slotCount]`. At typical Precept scale (10–50 fields), `Array.Copy` is essentially free.

- Working copy lifecycle. Created at the start of each fire/edit/inspect operation. Populated with the entity's current field values. Mutated by the action chain. Evaluated against constraints. Promoted (fire/edit on success) or discarded (inspect always, fire/edit on failure). The working copy never escapes the operation boundary.

- Computed field recomputation. After the action chain completes, computed fields must be recomputed in topological order on the working copy before constraint evaluation. This ensures constraints see the final computed values. The recomputation is a linear walk over the executable model's topologically-sorted computed field list.

- Working copy for inspect. Inspect creates a working copy for each event being previewed. Since inspect evaluates every event from the current state, it creates multiple working copies (one per event) — each independent, each discarded after evaluation. No working copy crosses event boundaries.

**Research grounding:**

- CEL's hierarchical `Activation` pattern demonstrates overlay-based variable resolution in expression evaluation.
- OPA's store transactions demonstrate snapshot isolation for policy evaluation.
- XState's pure `transition()` demonstrates the pattern: compute the next state without modifying the current one. The "working copy" is implicit in the pure function — the old state is the input, the new state is the output.
- Eiffel's invariant model captures the same atomicity boundary: invariants may be temporarily violated during method execution but must hold at method boundaries. Precept's working copy is the mechanism that prevents even temporary violation visibility.

**Right-sizing:** Full slot-array clone is almost certainly the right answer at Precept's scale. Copy-on-write adds complexity for a problem that does not exist (entities are small). The design work is in specifying the lifecycle and the interaction with computed field recomputation, not in the clone mechanism.

### 2.7 Fault Production

The committed fault-correspondence chain: every `FaultCode` member has `[StaticallyPreventable(DiagnosticCode.X)]`. PREC0001 ensures every `Fail()` call takes a `FaultCode`. PREC0002 ensures every `FaultCode` has `[StaticallyPreventable]`. This guarantees: if the compiler emits no diagnostics, the evaluator's fault paths should be unreachable.

**Design questions that must be resolved:**

- What are the concrete `FaultCode` values? The language vision's totality principle identifies the value-dependent fault categories: division by zero, overflow, empty-collection accessor (`.min`/`.max`/`.peek` on empty), null dereference on nullable fields. Each needs a `FaultCode` member linked to the `DiagnosticCode` that should have prevented it.

- What happens when a fault IS produced despite a clean compile? This is the defensive redundancy scenario. Options:
  - **Throw an exception.** The fault is a bug in the compiler's proof reasoning — it should have been caught. Making it an exception treats it as a "this should never happen" assertion. Survey evidence: Temporal's `NonDeterminismException` is this pattern — a replay divergence is a bug, not a business result.
  - **Return as a fault outcome.** The fault is communicated through the normal result channel as a distinct outcome kind. Callers can handle it gracefully. Survey evidence: CEL returns errors as values in the type algebra — `types.Err` implements `ref.Val`.
  - **Both.** Record the fault for diagnostics (the compiler has a gap), AND return it as a result so the caller is not left with an unhandled exception.

  The language vision's Principle 10 (Totality) says: "Every expression evaluates to a result or a definite error." The word "definite" suggests the error is a structured result, not an exception. But the fault-correspondence chain suggests these errors should be unreachable. The tension is between totality-as-design (errors are results) and defensiveness-as-assertion (unreachable errors are bugs).

- Fault attribution. When a fault fires, what information does it carry? The committed `Fault` record has `(FaultCode, CodeName, Message)`. Should it also carry expression location (which expression caused the fault), field attribution (which fields contributed), and the `[StaticallyPreventable]` link (which diagnostic should have prevented it)?

- Fault vs. constraint violation. These are distinct categories. A fault is an evaluator-level error (division by zero, overflow) — the type checker should have prevented it. A constraint violation is a business-rule violation (rule X is not satisfied) — this is normal operation, not a bug. The result type must keep them separate. Survey evidence:
  - CEL distinguishes infrastructure error (`error` return), value error (`types.Err` in `ref.Val`), and success — three levels.
  - XState distinguishes `'active'`, `'done'`, and `'error'` — error is structural, not a guard failure.
  - Eiffel distinguishes precondition violation (client bug) from postcondition violation (supplier bug) — blame assignment.

**Research grounding:**

- SPARK Ada's fault-correspondence is the strongest surveyed reference: every Ada runtime check maps to a proof obligation. If proved, the runtime check can be removed. This is exactly the pattern `[StaticallyPreventable]` implements.
- Dhall's total evaluator demonstrates the ideal: after type checking, no faults are possible. Precept approaches but does not achieve this — value-dependent faults remain.
- CEL's error-as-value model (`types.Err` implementing `ref.Val`) demonstrates how runtime faults can be first-class values in the result, not exceptions.
- The committed `Fault` struct (`Fault(FaultCode, CodeName, Message)`) is structurally sound but may need enrichment with location and attribution data.

**Right-sizing:** The fault space is small and bounded. The `FaultCode` enum contains ~5–10 members (division by zero, overflow, empty accessor, null deref). The design complexity is in the correspondence enforcement (already committed via analyzers) and the fault-vs-violation separation in the result type.

---

## 3. Key Design Decisions

Seven design decisions that must be resolved before runtime implementation. Each is numbered R1–R7 for cross-referencing.

### R1 — Evaluator shape

**Question:** Is the evaluator a static utility class with pure functions, or an instance that holds per-operation state? What does it take in, what does it return?

**Framing:** This decision governs the runtime's concurrency model and testability. A static evaluator is trivially thread-safe and testable (pure function, no setup). An instance evaluator can encapsulate working-copy management and evaluation configuration but introduces lifecycle concerns.

**Survey grounding:**
- CEL's evaluator is structurally stateless: `Interpretable.Eval(Activation)` is a method on the compiled tree, not on an evaluator instance. The `Program` holds the compiled form; evaluation is a call against it.
- XState's pure `transition(machine, state, event)` is the purest functional model: no evaluator instance at all.
- Dhall's `eval(env, expr)` is a pure function.
- Drools' `KieSession` is deeply stateful — but Drools manages working memory with incremental pattern matching, a fundamentally different evaluation model.

Precept's execution model properties (no loops, expression purity, finite state space) favor the stateless/functional pattern. The working copy is per-operation and can be allocated inside the evaluation function.

**Dependencies:** Blocks R3 (entity representation must know who manages the working copy) and R5 (inspect/fire unification depends on whether both are methods on the same instance or parameterized calls to the same static function).

### R2 — Result type taxonomy

**Question:** What is the shape of the result type? Sealed class hierarchy with 9 leaf types? Discriminated union? Per-operation result families or unified?

**Framing:** The 9-outcome taxonomy from the language vision is a language-level commitment, not an implementation convenience. Every outcome requires different caller behavior: successful transitions return new state; constraint violations return attribution records; rejections return authored reasons; undefined surfaces are definition gaps the author should fix. The result type is the primary API consumers interact with.

**Survey grounding:**
- CEL's `(ref.Val, *EvalDetails, error)` three-value return is the closest to a layered result — but CEL only distinguishes 3-4 categories, not 9.
- XState's `MachineSnapshot` carries `status: 'active' | 'done' | 'error'` — a 3-value enum on a unified type.
- CUE's `Value` which may be `Bottom` — a 2-value distinction (valid or error).
- No surveyed system has a 9-category outcome taxonomy. This is purpose-built for Precept's semantic distinction requirements.

The compiler design plan states: "9 outcomes must be structurally distinguishable at the C# type level (sealed hierarchy, not string codes)." This locks the shape to a sealed type hierarchy.

**Dependencies:** Couples to R1 (the evaluator's return type) and R5 (inspect must return the same result shape). Couples to R6 (constraint violation records are a payload within the constraint-violation outcome variant).

### R3 — Entity representation

**Question:** Immutable record, working copy mechanism, slot array vs. dictionary. What is the shape of the entity snapshot that enters and exits every operation?

**Framing:** The entity representation is the data contract between the runtime and its callers. It must be immutable (callers never see partial mutations), carry enough information for the evaluator (state + fields + model reference), and support efficient slot-indexed access for the evaluation hot path.

**Survey grounding:**
- XState's `MachineSnapshot` is immutable, carries `value` (state) + `context` (data) + `status` + metadata. The closest structural analog.
- CEL's `Activation` is immutable and supports hierarchical composition for scoped bindings.
- CUE's `Value` is immutable; `Unify()` creates new values.
- The provisional `Version` stub uses `ImmutableDictionary<string, object?>` for data. The slot-array alternative (driven by the emitter's slot resolution) would be `object?[]` with indices assigned at compile time.

Slot array vs. dictionary is a tradeoff between internal efficiency (slot array for the evaluator) and external ergonomics (dictionary for API consumers who work with field names). A hybrid — slot array internally, name-based access methods on the public API — resolves both concerns.

**Dependencies:** Couples to R1 (the evaluator must know how to read/write entity data) and R4 (the executable model defines the slot layout). Couples to working copy design (§2.6 — the working copy is a mutable variant of the entity representation).

### R4 — Executable model contract

**Question:** What invariants does the executable model guarantee? What can the evaluator assume without checking?

**Framing:** The emitter/evaluator boundary is a trust boundary. Every invariant the emitter guarantees is an assumption the evaluator does not need to validate at runtime. The stronger the contract, the simpler and faster the evaluator. But a strong contract means the emitter must do more work and any emitter bug silently corrupts evaluation (the compiler design plan's R8 risk — emitter semantic drift).

At minimum, the contract should specify:
- Every slot index in an expression tree refers to a valid field slot.
- Every expression tree node has a resolved type tag.
- The transition dispatch table is complete: every `(state, event)` pair present in the definition has an entry.
- Computed field evaluation order is topologically sorted — no forward dependencies.
- Constraint scope indices are complete: every applicable constraint for every state/event combination is indexed.

**Survey grounding:**
- CEL's `Program` carries pre-resolved function bindings, type-checked expression trees, and cost metadata. The evaluator trusts this structure unconditionally.
- OPA's `PreparedEvalQuery` compiles the query plan. Evaluation trusts the plan.
- The compiler design plan's D8 asks exactly this question for the emitter side. The runtime's R4 is the consumer side of the same contract.

**Dependencies:** This is the primary coupling between the compiler design plan and this runtime plan. D8 (emitter contract) and R4 are two halves of the same specification. Must be co-designed. Blocks R1 (the evaluator's implementation depends on what it can assume) and R5 (inspect/fire both consume the executable model under the same contract).

### R5 — Inspect/Fire unification

**Question:** How do inspect and fire share the evaluation path while differing only in commit behavior?

**Framing:** The language vision requires that inspect has "the same depth as event execution." The compiler design plan locks: "inspect MUST go through the full pipeline including the emitter." The architectural requirement is structural: inspect and fire produce the same result because they execute the same code, not because they are tested to agree. The unification mechanism must prevent semantic drift by construction.

**Survey grounding:**
- XState v5's `transition()` function is the primary reference. `transition(machine, state, event)` is pure — it computes the next state and the action list without committing. `getNextSnapshot()` returns just the snapshot for preview. Fire and inspect differ only in whether actions are executed against the actor lifecycle. The evaluation (guard checking, state computation) is shared.
- CEL's `ExhaustiveEval` mode shares the interpretable tree with standard evaluation but changes the short-circuit behavior. The evaluation code is shared; only the mode flag differs.
- Drools' queries provide read-only fact inspection without rule firing — but queries evaluate patterns, not what-if scenarios. No structural unification between query evaluation and rule firing.

For Precept, the natural design: a shared internal function that takes (model, entity, event, args, mode) where mode ∈ {commit, preview}. Both paths: resolve transition rows → evaluate guards → select matching row → create working copy → execute action chain → recompute computed fields → evaluate constraints → produce outcome. On commit mode, a successful outcome promotes the working copy. On preview mode, the working copy is always discarded. The outcome record is identical.

**Dependencies:** Depends on R1 (evaluator shape) and R2 (result type — inspect returns the same result type as fire). Depends on R4 (both modes consume the executable model under the same contract).

### R6 — Constraint evaluation: collect-all attribution model

**Question:** How are constraint violations collected, and what is the shape of a violation record?

**Framing:** The language vision specifies four constraint kinds with distinct attribution: event ensures (args + event scope), rules (fields + definition scope), state ensures (fields + state scope with anchor), and rejections (event as a whole). Violations must carry semantic subjects, scope kind, anchor, the `because` message, and computed field transitive expansion. This attribution model exceeds anything surveyed systems provide.

**Survey grounding:**
- OPA's `deny[msg]` pattern collects all violations into a set. Each violation is a message string — no structured attribution.
- CUE's `Bottom` accumulates all conflicts from unification. The error carries source locations and constraint expressions.
- SPARK Ada's proof output carries per-obligation results with check category, source location, prover info, and severity — the most structured of any surveyed system. But SPARK's obligations are about runtime-check categories (overflow, range), not business-rule semantics.
- Eiffel's blame model (precondition = client, postcondition = supplier) is the closest to Precept's distinction between event-ensure violations (caller provided bad args — client blame) and rule violations (definition integrity — definition blame).
- No surveyed system provides semantic-subject attribution with transitive expansion through computed fields. This is Precept-specific design work.

The compiler design plan specifies: "Constraint violation attribution record — semantic subjects + scope + anchor as structured data, consumable by diagnostics, hover, and MCP." The constraint evaluator must produce records matching this specification.

**Dependencies:** Couples to R2 (violation records are the payload of the constraint-violation outcome variant) and R4 (the executable model must carry enough metadata for attribution — field names, scope tags, computed field dependency graphs).

### R7 — Fault correspondence runtime: defensive redundancy behavior

**Question:** How is the `FaultCode` → `DiagnosticCode` chain enforced at runtime, and what happens when a fault IS produced despite a clean compile?

**Framing:** The `[StaticallyPreventable]` attribute links every runtime fault to the diagnostic that should have prevented it. The PREC0001 and PREC0002 analyzers enforce structural coverage. But the question remains: what does the runtime DO when a statically-preventable fault fires? This is the gap between "should never happen" and "what if it does."

**Survey grounding:**
- SPARK Ada: when a proof obligation is proved, the runtime check is removed from the compiled executable. If the proof is wrong, the behavior is undefined — SPARK trusts the proof. This is the strongest position: proved = eliminated.
- Dhall: no runtime faults exist. `eval` is total. If a fault occurred, it would indicate a type-checker bug — but the language makes this structurally impossible.
- Temporal: `NonDeterminismException` is thrown when a replay diverges from history. This is a "should never happen" assertion that indicates a workflow code bug or version mismatch. The exception halts the workflow.
- CEL: division by zero returns `types.Err` — a value in the result, not an exception. The evaluator does not distinguish between "expected" and "unexpected" errors.

Precept's fault-correspondence chain places it between SPARK (strong static guarantee) and CEL (all faults are values). The `[StaticallyPreventable]` attribute documents the intent: the compiler should prevent these. But the runtime must handle the case where the compiler has a gap. Three options are on the table: exception (assertion failure), fault result (graceful degradation), or both (log + result).

**Dependencies:** Couples to R2 (faults need a place in the result type taxonomy — either as a distinct outcome variant or as a separate channel) and to the committed `Fault` type shape.

---

## 4. Phasing

### Phase R1 — Boundary Contracts (blocks everything)

| Design Work | Decisions Answered | Primary Survey References |
|---|---|---|
| **Executable Model Contract** (co-design with compiler D8) | R4 | CEL Program internals, OPA PreparedEvalQuery |
| **Result Type Taxonomy** | R2 | CEL three-value return, XState MachineSnapshot, CUE Value/Bottom |

These must be resolved first because every other runtime component depends on what the executable model provides (R4) and what the evaluator returns (R2). R4 is explicitly co-designed with the compiler plan's D8 — the emitter contract and the evaluator contract are two sides of the same specification.

### Phase R2 — Core Evaluation (blocks implementation)

| Design Work | Decisions Answered | Primary Survey References |
|---|---|---|
| **Evaluator Shape** | R1 | CEL Interpretable.Eval, XState transition(), Dhall eval |
| **Entity Representation** | R3 | XState MachineSnapshot, CEL Activation |
| **Inspect/Fire Unification** | R5 | XState transition/getNextSnapshot, CEL ExhaustiveEval |

Phase R2 can proceed once Phase R1 establishes the boundary contracts. R1, R3, and R5 are tightly coupled — the evaluator shape, entity representation, and inspect/fire mechanism are co-designed. They depend on R4 (what the executable model provides) and R2 (what results look like).

### Phase R3 — Constraint & Fault Surfaces (parallelizable with Phase R2)

| Design Work | Decisions Answered | Primary Survey References |
|---|---|---|
| **Constraint Evaluation & Attribution** | R6 | OPA deny[msg], CUE Bottom, SPARK proof output, Eiffel blame model |
| **Fault Correspondence Runtime** | R7 | SPARK proof-elimination, Dhall totality, CEL error-as-value |

Phase R3 can proceed in parallel with Phase R2 after Phase R1 — the constraint evaluator and fault production mechanism consume the executable model (R4) and produce results (R2), but their internal design does not depend on the evaluator shape (R1) or entity representation (R3).

### Phase Dependency Graph

```
Phase R1: R4 (Executable Model Contract) + R2 (Result Type Taxonomy)
    ↓                                          ↓
Phase R2: R1 + R3 + R5                  Phase R3: R6 + R7
(Evaluator, Entity, Inspect/Fire)       (Constraints, Faults)
```

Phases R2 and R3 are independent of each other and can proceed in parallel once Phase R1 is complete.

---

## 5. Open Questions

### Questions this plan intentionally does not answer

1. **Collection mutation semantics in the working copy.** The language vision includes `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`. How are collection mutations represented in the slot array? Are collections stored inline or by reference? Does the working copy deep-clone collections, or use structural sharing? This requires design work informed by the collection type semantics, which the language vision specifies but this plan does not evaluate in depth.

2. **Temporal and business-domain type evaluation.** The language vision specifies 8 temporal types and 7 business-domain types with operator tables, cancellation rules, and mediation semantics. The evaluator must implement these operations. The type-level design is a compiler concern (covered by the compiler plan's D3). The runtime-level question is: what backing representations do these types use at evaluation time? `NodaTime` types for temporal, `decimal` for money, custom structs for quantity/price? This is implementation design work that depends on the type system design.

3. **String interpolation evaluation.** The language vision specifies interpolation in both string literals and typed constants. The evaluator must support expression evaluation inside interpolation contexts. How is this represented in the executable model's expression trees? Is interpolation a special expression node, or is it desugared to concatenation during lowering?

4. **Nullable field semantics in expression evaluation.** The language vision specifies that `.length` on `null` produces an error and that null guards are enforced by the type checker. But what does the evaluator do if it encounters a null field in an expression? Trust the type checker (the null access is unreachable if compilation succeeded) or check defensively? This is a specific instance of the R7 (fault correspondence) question.

5. **Concurrency model.** Is the executable model safe to share across threads? (Yes — it is immutable.) Can multiple operations on different entity instances proceed concurrently? (Yes — they share the executable model but have independent working copies.) Can multiple operations on the SAME entity instance proceed concurrently? (Unclear — the immutable entity representation means each operation takes a snapshot, but concurrent operations may produce conflicting next-states. This is a host-application coordination concern, not a runtime design concern, but the runtime's API should not preclude concurrent use.)

6. **Error message formatting.** The committed `Fault` record carries `CodeName` and `Message`. The language vision requires `because` messages on all constraints. How are these messages formatted for consumers? Is there a localization mechanism? A template system for parameterized messages? This is a presentation concern but needs specification before the API is finalized.

7. **Performance characteristics of inspect.** Inspect evaluates every event from the current state. If a precept has 10 events, each with 5 transition rows, inspect evaluates 50 row guards plus all associated constraints. At Precept's scale this is likely trivial, but the working-copy-per-event cost model should be verified against realistic definitions.

8. **Edit operation scope.** The `Edit` operation validates editability and constraints. Does it go through the same evaluation path as fire (with an "edit" pseudo-event), or is it a separate path? The inspect/fire unification (R5) covers fire/inspect but does not specify edit's relationship to the shared evaluation path.

---

## 6. Stub Evaluation

The provisional stubs (`Precept`, `Version`, `Fault`) are evaluated against survey evidence.

### `Precept` class — the executable model wrapper

```csharp
public sealed class Precept
{
    private Precept() { }
    public static Precept From(CompilationResult compilation) => ...
    public Version From(string state, ImmutableDictionary<string, object?> data) => ...
}
```

**Assessment:** The two-method API conflates two concerns: `From(CompilationResult)` creates the executable model, and `From(state, data)` creates an entity instance. Survey evidence supports separating these:
- CEL: `env.Program(ast)` creates the executable; `program.Eval(activation)` is a separate call. The compiled model and entity construction are distinct operations.
- XState: `createMachine(config)` creates the machine definition; `createActor(machine, options)` creates an instance. Separate types.

The `Precept` name is reasonable — it is the compiled form of a `.precept` definition. But the class conflates model construction with instance construction. Consider whether `From(state, data)` belongs on the executable model type or on a separate factory/instance type.

Additionally, `From(string state, ImmutableDictionary<string, object?> data)` uses string state names and a string-keyed dictionary. The emitter produces slot indices and state indices. The public API may want name-based access for ergonomics, but internally the executable model works with indices. The stub does not reflect this tension.

### `Version` record — the entity snapshot

```csharp
public sealed record class Version(Precept Precept, string State, ImmutableDictionary<string, object?> Data)
{
    public Version Fire(string eventName, ImmutableDictionary<string, object?>? args = null) => ...
    public Version Edit(string field, object? value) => ...
}
```

**Assessment:** The shape is structurally aligned with XState's `MachineSnapshot` (immutable, carries state + data + model reference). However, several tensions emerge:

1. **Return type of `Fire` and `Edit`.** The stubs return `Version` — implying success. The language vision's 9-outcome taxonomy requires that fire and edit can fail (constraint violation, rejection, unmatched event, etc.). The return type should be the result type (R2), not `Version`. A successful outcome carries the new `Version`; a failure outcome carries violation details.

2. **Missing `Inspect`.** The stubs have `Fire` and `Edit` but no `Inspect`. The language vision requires inspect as a first-class operation with the same depth as fire.

3. **`ImmutableDictionary<string, object?>` for `Data`.** This is a string-keyed, untyped dictionary. The emitter produces slot-indexed fields with known types. A slot array (`object?[]`) would be more aligned with the executable model's structure. The dictionary form is more ergonomic for API consumers but misaligned with the evaluation hot path.

4. **Instance methods vs. standalone functions.** `Fire` and `Edit` as instance methods on `Version` implies the entity "does things to itself." The XState model separates concerns: `transition(machine, state, event)` is a standalone function; the snapshot is passive data. For Precept, having operations as methods on the entity snapshot is a reasonable convenience API, but the underlying implementation should be the shared evaluation function (per R5).

5. **Stateless precepts.** The stub requires `State` — but stateless precepts have no state. This needs to accommodate both forms, either via an optional state field or via a type hierarchy.

### `Fault` record — the runtime fault

```csharp
public readonly record struct Fault(FaultCode Code, string CodeName, string Message);
```

**Assessment:** The shape is minimal and correct for its purpose. The `FaultCode` enum carries the `[StaticallyPreventable]` attribute linking to compiler diagnostics. Potential enrichments identified by the plan:

1. **Expression location.** When a fault fires, which expression caused it? A span or node identifier would help diagnostics.
2. **Field attribution.** Which fields contributed to the faulting expression? For a division-by-zero fault, the divisor field is the semantic subject.
3. **Defensive-redundancy flag.** Should the fault indicate "this was supposed to be caught at compile time"? The `[StaticallyPreventable]` attribute is on the `FaultCode` enum member, but the runtime `Fault` instance does not surface this.

The `Fault` struct is adequate for the committed fault-correspondence chain but may need enrichment when the result type (R2) and attribution model (R6) are designed.

---

## 7. Summary of Survey Grounding Strength

### Decisions with strong survey grounding

- **R1 (Evaluator shape):** CEL, XState, and Dhall all converge on stateless/functional evaluation for pure-expression languages. The evidence strongly favors a static evaluator with per-operation working copies.
- **R3 (Entity representation):** XState, CEL, CUE, and Dhall all use immutable snapshots. Survey consensus is clear.
- **R5 (Inspect/Fire unification):** XState's `transition()`/`getNextSnapshot()` pair directly demonstrates the pattern. CEL's `ExhaustiveEval` provides a secondary reference. Strong support for shared evaluation with mode parameterization.

### Decisions with moderate survey grounding

- **R4 (Executable model contract):** CEL's `Program` and OPA's `PreparedEvalQuery` demonstrate the pattern (compiled model that evaluators trust). But neither specifies their contract formally — the trust is implicit. Precept must make it explicit, which is original specification work.
- **R7 (Fault correspondence runtime):** SPARK's proof-elimination pattern and Dhall's totality provide the theoretical anchors. CEL's error-as-value provides the practical alternative. But no surveyed system has Precept's specific structure: a formal attribute chain linking runtime faults to compile-time diagnostics with analyzer enforcement. The chain is committed; the runtime behavior when the chain is violated is unresolved.

### Decisions with weakest survey grounding (most contentious)

- **R2 (Result type taxonomy):** No surveyed system has 9 distinct outcomes. CEL has 3-4, XState has 3, CUE has 2. The 9-outcome requirement is derived from the language vision's semantic distinctions, which no surveyed system shares. This is purpose-built design with no direct precedent.
- **R6 (Constraint evaluation attribution):** Collect-all evaluation has precedent (OPA, CUE, Drools). But semantic-subject attribution with transitive computed-field expansion has no precedent in any surveyed system. SPARK's per-obligation tracking is the closest, but SPARK attributes by check category (overflow, range), not by business-rule semantics (which fields, which scope, which anchor).
