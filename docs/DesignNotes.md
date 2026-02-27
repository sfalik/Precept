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
  - instance-first `repl`
  - script execution mode via `--script`

## Current Runtime Semantics

- Undefined state/event/transition resolves to `IsDefined = false`
- Unguarded transitions are accepted and return a target/new state
- Guarded transitions are evaluated at runtime against optional event arguments; when provided, they are used for that call without mutating persisted instance data
- Transition data assignments are evaluated/applied only during accepted `Fire(...)` calls
- If one guarded transition evaluates `true`, inspection/fire is accepted and returns target/new state
- If all guarded transitions evaluate `false`, inspection/fire is rejected with aggregated guard-failure reasons
- Instance-based inspect/fire validates workflow name compatibility before evaluating transitions
- CLI is instance-first; startup requires loading a persisted instance file

## Concurrency Model (Current)

- `DslWorkflowDefinition` is immutable after compile.
- Runtime does not maintain hidden mutable process state; state progresses through returned `DslWorkflowInstance` values.
- Any coordination for concurrently reading/writing persisted instance files is outside runtime scope and must be handled by the caller.

## Known Gaps

- Guard language is intentionally minimal in this phase (`Identifier`, `!Identifier`, and simple comparisons)
- Editor tooling (LSP and IntelliSense integration)

## Test Status

- Active tests: `test/StateMachine.Tests/DslWorkflowTests.cs`
- Guard test coverage includes: boolean guards, comparisons, string/null equality, numeric runtime type coercion, unsupported-expression rejection, and reason aggregation.

## Next Steps

1. Expand guard language/features or swap in a richer evaluator implementation.
2. Implement LSP-backed diagnostics/completion for `.sm` files.

## Guard Evaluation + Event-Argument Model (Current)

- Runtime uses `IGuardEvaluator` with a default implementation (`DefaultGuardEvaluator`).
- `DslWorkflowCompiler.Compile(...)` accepts an optional custom evaluator.
- `Inspect(...)` and `Fire(...)` accept optional event arguments (`IReadOnlyDictionary<string, object?>`).
- REPL commands support transient per-command JSON event-argument overrides.
- Transition DSL supports `set <Key> = <expr>` where `<expr>` can be a literal, `data.<Key>`, or `arg.<Key>`.
- CLI emits non-zero exit codes for incompatible instances and script command failures.
- Runtime supports persisted instance creation and instance-based `Inspect(...)` / `Fire(...)`.
- CLI supports `--instance` at startup and REPL-level `load`/`save` for instance file management.
- Repository root includes runnable examples: `trafficlight.sm`, `traffic.instance.json`, `traffic.script.txt`.
- CLI includes interactive REPL and non-interactive script execution using the same command set.

Supported default guard forms:

- `IsEnabled`
- `!IsEnabled`
- `CarsWaiting > 0`
- `CarsWaiting >= 3`
- `Mode == "Manual"`

Unsupported/invalid guards are treated as failed with descriptive reasons.
