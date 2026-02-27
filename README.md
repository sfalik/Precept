# StateMachine 🚦

A .NET state/workflow engine project currently focused on an experimental runtime DSL.

## Current Status

### Implemented now

- Line-based DSL model/parser/runtime in `src/StateMachine/Dsl/*`
- Persistable workflow instance model (`DslWorkflowInstance`) with workflow compatibility checks
- Instance APIs in runtime (`CreateInstance`, instance-based `Inspect`, instance-based `Fire`)
- CLI tooling in `tools/StateMachine.Dsl.Cli`:
  - `validate`
  - `list`
  - `inspect`
  - `fire`
- Active tests for DSL parsing/runtime behavior in `test/StateMachine.Tests/DslWorkflowTests.cs`

### Concurrency model (current)

- `DslWorkflowDefinition` is immutable after compile and can be reused across calls.
- Instance state is modeled as data (`DslWorkflowInstance`) and updated by returning a new instance from `Fire(...)`.
- Persistence/write coordination (for instance JSON files) is caller-managed.

### Legacy runtime status

- The active implementation is the DSL runtime path only.
- Legacy fluent/runtime artifacts are not part of the current public/runtime contract.

### Not implemented yet

- DSL editor tooling (LSP / IntelliSense for VS Code / Visual Studio)
- Full workflow migration strategy between changed `.sm` definitions

### Runtime semantics (current)

- Unknown state/event/transition returns `IsDefined = false`
- Unguarded transitions are accepted immediately
- Guarded transitions are evaluated against optional runtime context
- If all guarded candidates fail, result is rejected with aggregated reasons
- Compiled workflow has deterministic `Version`; instance workflow name/version must match for instance-based inspect/fire
- CLI `inspect`/`fire` requires one of `--state` or `--instance`

## Installation

```sh
dotnet add package StateMachine
```

## Experimental DSL CLI

Example DSL file: `./trafficlight.sm`

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- validate ./trafficlight.sm
dotnet run --project tools/StateMachine.Dsl.Cli -- list ./trafficlight.sm
dotnet run --project tools/StateMachine.Dsl.Cli -- inspect ./trafficlight.sm --state Red --event Advance
dotnet run --project tools/StateMachine.Dsl.Cli -- fire ./trafficlight.sm --state Green --event Advance
dotnet run --project tools/StateMachine.Dsl.Cli -- inspect ./trafficlight.sm --instance ./traffic.instance.json --event Advance
dotnet run --project tools/StateMachine.Dsl.Cli -- fire ./trafficlight.sm --instance ./traffic.instance.json --event Advance --out-instance ./traffic.instance.json
dotnet run --project tools/StateMachine.Dsl.Cli -- inspect ./trafficlight.sm --state Red --event Advance --context "{\"CarsWaiting\": 2}"
dotnet run --project tools/StateMachine.Dsl.Cli -- fire ./trafficlight.sm --state Red --event Advance --context-file ./context.json
```

`inspect` / `fire` context options:

- `--context <json>` for inline JSON object
- `--context-file <path.json>` for JSON from file
- `--instance <path.json>` to load state/context from a persisted instance
- `--out-instance <path.json>` (fire only) to save updated instance; defaults to `--instance` path when omitted

Result exit codes are outcome-specific:

- `inspect`: `5` = not defined, `6` = rejected
- `fire`: `7` = not defined, `8` = rejected

## Sample `.sm`

```text
machine TrafficLight
state Red
state Green
event Advance
transition Red -> Green on Advance
```

Guarded variant:

```text
machine TrafficLight
state Red
state Green
event Advance
transition Red -> Green on Advance when CarsWaiting > 0
```

## Project Structure

```text
src/StateMachine/
    Dsl/
        StateMachineDslModel.cs
        StateMachineDslParser.cs
        StateMachineDslRuntime.cs

tools/StateMachine.Dsl.Cli/
    Program.cs

test/StateMachine.Tests/
    DslWorkflowTests.cs