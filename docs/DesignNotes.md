# StateMachine Design Notes

Date: 2026-02-24

This document captures architecture decisions agreed during design review, to guide upcoming implementation work.

## Legacy FSM Status (Design Phase)

- `FiniteStateMachine` is intentionally retained during design and implementation of the new fluent `StateMachine` API.
- Purpose: provide a working reference implementation while building the new runtime.
- Scope: internal development aid only (not public, not used by external consumers).
- Policy: do not remove legacy code until the new implementation is functionally complete and validated by tests.
- Compatibility and migration concerns are out of scope for this phase.

## Template / Instance Split

- Builder stays mutable while authoring.
- `Build()` finalizes to immutable template.
- Template exposes `CreateInstance(...)`.
- Instances hold runtime mutable state/data and transition behavior.

## Builder Freeze Contract

- `Build()` may be called once per builder instance.
- Builder is frozen after `Build()`.
- Any post-build mutating fluent call throws.

## Ability to manage data along with state
- data should be immutable -- use records to enforce this
- transforms should be pure functions that take the current state and arg and return a new state and new data (if needed)
- transforms are used in the transition definition to specify how state and data should change when a transition occurs

## Rules Engine
TODO:  incorporate a mechnism for defining invariant rules that must hold true for the data and state of the machine.  
these rules should be evaluated after each transition to ensure that the machine remains in a valid state.  
if a rule is violated, the transition should be rejected and an appropriate exception should be thrown.

## Event Definitions

- Use typed event tokens captured in `On(...)`:
  - `Event<TState>`
  - `Event<TState, TArg>`
- Do not capture instance-bound delegates at build time.

## Concurrency Model

- The new fluent `StateMachine` runtime is non-thread-safe per instance.
- Concurrent calls against the same machine instance are not supported.
- Callers must provide external synchronization or serialization if cross-thread use is required.
- Transition semantics are defined for single-threaded access only.
- No stale-state revalidation contract is required in fluent APIs for this phase.

TODO: Revisit an optional thread-safe mode only after core runtime behavior is complete.

## Fluent Inspect / Fire Pattern

goals for this API:
- provide an api that leverages the deterministic nature of the statemachine for interrogating events and potential transitions before they are fired
- guide users through the correct sequence of inspection steps with a fluent API that makes it difficult to misuse or skip steps
- happy paths should be concise and readable, and error paths should provide clear information about why a transition was rejected
- for happy paths, it is ok to throw.  but for branching, the api should guide the developer to handle the various outcomes of the inspection process without needing to throw for control flow


Preserve current chain semantics:

- `Inspect(...)`
- `WithArg(...)`
- `IfAccepted(...)`
- `Fire()`
- `Else(...)`

Guard-validation gate remains mandatory before `Fire()`.

TODO: this needs to be redesigned, i'm not sold on this whole fluent inspection API
TODO: consider the use of proof tokens / receipts as a pattern to go through the steps

## Inspection Chain Representation

- Current design uses lightweight `readonly struct` wrappers for the fluent inspection chain.
- This matches the current scaffold in `Inspection.cs` and keeps design and implementation aligned during development.

Planned chain forms include:

- `Inspection<TState>` / `Inspection<TState, TArg>`
- `Defined<TState>` / `Defined<TState, TArg>`
- `Accepted<TState>` / `Accepted<TState, TArg>`
- `Rejected<TState>` / `Rejected<TState, TArg>`

TODO: Re-evaluate whether class-based chain nodes would be safer or clearer than structs. Compare copy semantics, default-value hazards, API misuse resistance, and allocation/perf impact before finalizing runtime implementation.

## Event Dispatch (Current Model)

- With single-threaded per-instance semantics, runtime does not require internal locking for correctness in this phase.
- Transition observers are invoked after transition logic is resolved for the current call.
- If a thread-safe mode is introduced later, locking/dispatch guarantees will be specified then.

## Exception Model

Use a focused exception set:

- `InvalidTransitionException`
- `GuardFailedException`

Remove legacy condition-failure exception path.

## Documentation + Tests First

- Contracts/stubs/tests/readme updated before deep implementation.
- Runtime implementation phase follows this design baseline.

## Implementation Checklist (Next)

- Implement template compilation of transition graph.
- Implement CreateInstance runtime shells.
- Implement Inspect logic (defined/accepted/reasons/target).
- Implement Fire using single-threaded per-instance semantics (no stale validation in this phase).
- Implement dataful transforms and guard chain semantics.
- Implement callback dispatch semantics for the single-threaded per-instance model.
- Unskip and expand tests incrementally.
