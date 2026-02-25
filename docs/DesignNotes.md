# StateMachine Design Notes

Date: 2026-02-24

This document captures architecture decisions agreed during design review, to guide upcoming implementation work.

## 1) Legacy Removal

- Remove legacy finite-state implementation and tests.
- No compatibility window is required.
- Maintain one canonical architecture only.

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

- Use typed event definitions captured in `On(...)`:
  - `EventDef`
  - `EventDef<TArg>`
- Do not capture instance-bound delegates at build time.

## 5) Fluent Inspect / Fire Contract

Preserve current chain semantics:

- `Inspect(...)`
- `IfAccepted(...)`
- `Fire()`
- `Else(...)`

Guard-validation gate remains mandatory before `Fire()`.

## 6) Stale Detection

- Data-ful inspections capture both:
  - observed data record reference
  - observed state
- Data-less inspections capture:
  - observed state
- `Fire()` validates against current instance snapshot and throws `StaleStateException` on mismatch.

## 7) Inspection Chain Types

- Use classes, not structs.
- Avoid default-value silent behavior.
- Fail fast on invalid chain usage.

## 8) Locking + Event Dispatch

- Perform transition state/data mutation under lock.
- Build transition event args under lock.
- Release lock before invoking observers.

## 9) Exception Model

Use a focused exception set:

- `InvalidTransitionException`
- `GuardFailedException`
- `StaleStateException`

Remove legacy condition-failure exception path.

## 10) Documentation + Tests First

- Contracts/stubs/tests/readme updated before deep implementation.
- Runtime implementation phase follows this design baseline.

## Implementation Checklist (Next)

1. Implement template compilation of transition graph.
2. Implement CreateInstance runtime shells.
3. Implement Inspect logic (defined/accepted/reasons/target).
4. Implement Fire with stale validation.
5. Implement dataful transforms and guard chain semantics.
6. Implement callback dispatch outside lock.
7. Unskip and expand tests incrementally.
