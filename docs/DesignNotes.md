# StateMachine Design Notes

Date: 2026-02-24

This document captures architecture decisions agreed during design review, to guide upcoming implementation work.

## 1) Legacy FSM Status (Design Phase)

- `FiniteStateMachine` is intentionally retained during design and implementation of the new fluent `StateMachine` API.
- Purpose: provide a working reference implementation while building the new runtime.
- Scope: internal development aid only (not public, not used by external consumers).
- Policy: do not remove legacy code until the new implementation is functionally complete and validated by tests.
- Compatibility and migration concerns are out of scope for this phase.

## 2) Template / Instance Split

- Builder stays mutable while authoring.
- `Build()` finalizes to immutable template.
- Template exposes `CreateInstance(...)`.
- Instances hold runtime mutable state/data and transition behavior.

## 3) Builder Freeze Contract

- `Build()` may be called once per builder instance.
- Builder is frozen after `Build()`.
- Any post-build mutating fluent call throws.

## 4) Event Definitions

- Use typed event tokens captured in `On(...)`:
  - `Event<TState>`
  - `Event<TState, TArg>`
- Do not capture instance-bound delegates at build time.

## 5) Fluent Inspect / Fire Contract

Preserve current chain semantics:

- `Inspect(...)`
- `IfAccepted(...)`
- `Fire()`
- `Else(...)`

Guard-validation gate remains mandatory before `Fire()`.

## 6) Concurrency Model

- The new fluent `StateMachine` runtime is non-thread-safe per instance.
- Concurrent calls against the same machine instance are not supported.
- Callers must provide external synchronization or serialization if cross-thread use is required.
- Transition semantics are defined for single-threaded access only.
- No stale-state revalidation contract is required in fluent APIs for this phase.

TODO: Revisit an optional thread-safe mode only after core runtime behavior is complete.

## 7) Inspection Chain Representation

- Current design uses lightweight `readonly struct` wrappers for the fluent inspection chain.
- This matches the current scaffold in `Inspection.cs` and keeps design and implementation aligned during development.

Planned chain forms include:

- `Inspection<TState>` / `Inspection<TState, TArg>`
- `Defined<TState>` / `Defined<TState, TArg>`
- `Accepted<TState>` / `Accepted<TState, TArg>`
- `Rejected<TState>` / `Rejected<TState, TArg>`

TODO: Re-evaluate whether class-based chain nodes would be safer or clearer than structs. Compare copy semantics, default-value hazards, API misuse resistance, and allocation/perf impact before finalizing runtime implementation.

## 8) Event Dispatch (Current Model)

- With single-threaded per-instance semantics, runtime does not require internal locking for correctness in this phase.
- Transition observers are invoked after transition logic is resolved for the current call.
- If a thread-safe mode is introduced later, locking/dispatch guarantees will be specified then.

## 9) Exception Model

Use a focused exception set:

- `InvalidTransitionException`
- `GuardFailedException`

Remove legacy condition-failure exception path.

## 10) Documentation + Tests First

- Contracts/stubs/tests/readme updated before deep implementation.
- Runtime implementation phase follows this design baseline.

## Implementation Checklist (Next)

1. Implement template compilation of transition graph.
2. Implement CreateInstance runtime shells.
3. Implement Inspect logic (defined/accepted/reasons/target).
4. Implement Fire using single-threaded per-instance semantics (no stale validation in this phase).
5. Implement dataful transforms and guard chain semantics.
6. Implement callback dispatch semantics for the single-threaded per-instance model.
7. Unskip and expand tests incrementally.
