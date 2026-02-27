# StateMachine Design Notes

Date: 2026-02-24

This document tracks current design decisions for the active implementation on this branch.

## Current Scope

Implementation focus is the DSL runtime path:

- parse `.sm` definitions
- validate semantic correctness
- execute inspection/fire against a compiled in-memory workflow definition

## Implemented Components

- `StateMachine.Dsl.DslMachine` model
- `StateMachine.Dsl.StateMachineDslParser`
- `StateMachine.Dsl.DslWorkflowCompiler`
- `StateMachine.Dsl.DslWorkflowDefinition`
- `StateMachine.Dsl.DslWorkflowInstance` persisted instance model
- Instance result/compatibility types: `DslInstanceCompatibilityResult`, `DslInstanceFireResult`
- CLI commands in `tools/StateMachine.Dsl.Cli`:
  - `validate`
  - `list`
  - `inspect`
  - `fire`

## Current Runtime Semantics

- Undefined state/event/transition resolves to `IsDefined = false`
- Unguarded transitions are accepted and return a target/new state
- Guarded transitions are evaluated at runtime against an optional context payload
- If one guarded transition evaluates `true`, inspection/fire is accepted and returns target/new state
- If all guarded transitions evaluate `false`, inspection/fire is rejected with aggregated guard-failure reasons
- Compiled workflow definitions expose deterministic `Version` values
- Instance-based inspect/fire validates workflow name + version compatibility before evaluating transitions

## Concurrency Model (Current)

- `DslWorkflowDefinition` is immutable after compile.
- Runtime does not maintain hidden mutable process state; state progresses through returned `DslWorkflowInstance` values.
- Any coordination for concurrently reading/writing persisted instance files is outside runtime scope and must be handled by the caller.

## Known Gaps

- Guard language is intentionally minimal in this phase (`Identifier`, `!Identifier`, and simple comparisons)
- Version migration strategy for persisted instances across evolved `.sm` definitions
- Editor tooling (LSP and IntelliSense integration)

## Test Status

- Active tests: `test/StateMachine.Tests/DslWorkflowTests.cs`
- Guard test coverage includes: boolean guards, comparisons, string/null equality, numeric runtime type coercion, unsupported-expression rejection, and reason aggregation.

## Next Steps

1. Expand guard language/features or swap in a richer evaluator implementation.
2. Add explicit migration hooks/policies for incompatible persisted instance versions.
3. Implement LSP-backed diagnostics/completion for `.sm` files.

## Guard Evaluation + Context Model (Current)

- Runtime uses `IGuardEvaluator` with a default implementation (`DefaultGuardEvaluator`).
- `DslWorkflowCompiler.Compile(...)` accepts an optional custom evaluator.
- `Inspect(...)` and `Fire(...)` accept optional context (`IReadOnlyDictionary<string, object?>`).
- CLI supports `--context` (inline JSON) and `--context-file` (JSON file path).
- CLI emits distinct non-zero exit codes for `inspect`/`fire` not-defined vs rejected outcomes.
- Runtime supports persisted instance creation and instance-based `Inspect(...)` / `Fire(...)`.
- CLI supports `--instance` and `--out-instance` for loading/saving workflow instances.
- CLI requires either `--state` or `--instance` for `inspect`/`fire`.

Supported default guard forms:

- `IsEnabled`
- `!IsEnabled`
- `CarsWaiting > 0`
- `CarsWaiting >= 3`
- `Mode == "Manual"`

Unsupported/invalid guards are treated as failed with descriptive reasons.
