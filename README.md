# StateMachine 🚦

A .NET state/workflow engine project currently focused on an experimental runtime DSL.

## Quick Start (REPL)

Use the included sample files to start an interactive session immediately:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- repl ./trafficlight.sm --state Red --context-file ./context.json
```

Then run a few commands in the prompt:

```text
sm> events
Advance

sm> inspect Advance
Defined: True
Accepted: True
Target: Green

sm> fire Advance
Defined: True
Accepted: True
NewState: Green

sm> state
Green
```

You can also start from a persisted instance:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- repl ./trafficlight.sm --instance ./traffic.instance.json
```

## Current Status

### Implemented now

- Line-based DSL model/parser/runtime in `src/StateMachine/Dsl/*`
- Persistable workflow instance model (`DslWorkflowInstance`) with workflow-name compatibility checks
- Instance APIs in runtime (`CreateInstance`, instance-based `Inspect`, instance-based `Fire`)
- CLI tooling in `tools/StateMachine.Dsl.Cli`:
  - `validate`
  - `list`
  - `inspect`
  - `fire`
  - `repl`
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

### Runtime semantics (current)

- Unknown state/event/transition returns `IsDefined = false`
- Unguarded transitions are accepted immediately
- Guarded transitions are evaluated against optional runtime context
- If all guarded candidates fail, result is rejected with aggregated reasons
- Instance-based inspect/fire validates workflow name compatibility before evaluating transitions
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
dotnet run --project tools/StateMachine.Dsl.Cli -- repl ./trafficlight.sm --state Red --context-file ./context.json
```

The repository includes ready-to-run examples:

- `./trafficlight.sm`
- `./traffic.instance.json`
- `./context.json`

### Quick verify

Accept path (guard passes):

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- inspect ./trafficlight.sm --state Red --event Advance --context-file ./context.json
```

Expected key output:

```text
Defined: True
Accepted: True
Target: Green
```

Reject path (guard fails):

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- inspect ./trafficlight.sm --state Red --event Advance --context "{\"CarsWaiting\": 0}"
```

Expected key output:

```text
Defined: True
Accepted: False
Outcome: Rejected
```

Fire via instance and persist updated state:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- fire ./trafficlight.sm --instance ./traffic.instance.json --event Advance --out-instance ./traffic.instance.json
```

Expected key output:

```text
Accepted: True
NewState: Green
Instance saved: ./traffic.instance.json
```

`inspect` / `fire` context options:

- `--context <json>` for inline JSON object
- `--context-file <path.json>` for JSON from file
- `--instance <path.json>` to load state/context from a persisted instance
- `--out-instance <path.json>` (fire only) to save updated instance; defaults to `--instance` path when omitted

`repl` startup options:

- `--state <StateName>` optional initial state (defaults to first declared state)
- `--instance <path.json>` optional persisted instance to load as session state
- `--context <json>` / `--context-file <path.json>` optional initial session context

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

## Sample `traffic.instance.json`

```json
{
  "workflowName": "TrafficLight",
  "currentState": "Red",
  "lastEvent": null,
  "updatedAt": "2026-02-27T00:00:00+00:00",
  "contextSnapshot": {
    "CarsWaiting": 2
  }
}
```

## Sample `context.json`

```json
{
  "CarsWaiting": 2
}
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