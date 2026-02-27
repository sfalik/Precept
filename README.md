# StateMachine 🚦

StateMachine is a .NET DSL-driven state/workflow engine focused on deterministic transition evaluation and persisted instance state.

## Why this project

- **Inspect before fire**: evaluate whether an event is defined/accepted before mutating state.
- **Persistable runtime instances**: load/save a JSON instance with current state and instance data.
- **Explicit event arguments**: per-call arguments are separate from persisted instance data.
- **Transition-scoped data updates**: `set Key = ...` assignments run only on accepted `fire`.

## Quick Start (2 minutes)

Run the included traffic-light sample in REPL mode:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json
```

Then try:

```text
sm> events
Advance
Emergency
ClearEmergency

sm> inspect Advance
Defined: True
Accepted: True
Target: Green

sm> fire Advance
Defined: True
Accepted: True
NewState: Green

sm> fire Emergency '{"Reason":"Accident"}'
Defined: True
Accepted: True
NewState: FlashingRed

sm> data
{
  "CarsWaiting": 2,
  "EmergencyReason": "Accident"
}
```

Note: inline JSON event arguments should be wrapped in single quotes.

## Core Concepts

- **Workflow definition**: immutable compiled DSL (`DslWorkflowDefinition`).
- **Instance**: persisted runtime state + data (`DslWorkflowInstance`).
- **Inspect**: side-effect free transition check.
- **Fire**: applies state change and transition data assignments.
- **Event arguments**: optional per-call JSON object for inspect/fire.
- **Instance data**: persisted data loaded from/saved to instance JSON.

## DSL Example

```text
machine TrafficLight
state Red
state Green
state Yellow
state FlashingRed
event Advance
event Emergency
event ClearEmergency
transition Red -> Green on Advance when CarsWaiting > 0
transition Red -> Red on Advance when CarsWaiting == 0
transition Green -> Yellow on Advance
transition Yellow -> Red on Advance
transition Red -> FlashingRed on Emergency set EmergencyReason = arg.Reason
transition Green -> FlashingRed on Emergency set EmergencyReason = arg.Reason
transition Yellow -> FlashingRed on Emergency set EmergencyReason = arg.Reason
transition FlashingRed -> Red on ClearEmergency
```

Supported assignment forms today:

- Literal: `set CarsWaiting = 0`
- Copy from instance data: `set LastCarsWaiting = data.CarsWaiting`
- Copy from event arguments: `set EmergencyReason = arg.Reason`

## Instance JSON Example

```json
{
  "workflowName": "TrafficLight",
  "currentState": "Red",
  "lastEvent": null,
  "updatedAt": "2026-02-27T00:00:00+00:00",
  "instanceData": {
    "CarsWaiting": 2,
    "EmergencyReason": null
  }
}
```

## CLI Usage

Launch REPL:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json
```

Run script mode:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json --script ./traffic.script.txt
```

REPL commands:

- `help`
- `state`
- `events`
- `data`
- `inspect <EventName> [event-args-json]`
- `fire <EventName> [event-args-json]`
- `load <path>`
- `save [path]`
- `exit | quit`

Exit codes:

- `0`: success
- `1`: invalid usage
- `2`: input file not found
- `4`: unhandled error
- `5`: incompatible instance/workflow
- `6`: script command failed

## Current Status

Implemented now:

- Line-based DSL parser/runtime in `src/StateMachine/Dsl/*`
- Instance-first runtime APIs (`CreateInstance`, `Inspect`, `Fire`)
- Guard evaluation with rejection reasons
- Transition data assignments (`set Key = ...`) on accepted `fire`
- CLI REPL + script execution
- Active test coverage in `test/StateMachine.Tests/DslWorkflowTests.cs`

Pending:

- Editor tooling (LSP/IntelliSense)

Concurrency model (current):

- Definition is immutable and reusable.
- Instance updates are caller-managed values.
- Persistence/write coordination is caller-managed.

Legacy runtime status:

- Active public/runtime path is DSL runtime only.
- Legacy fluent/runtime artifacts are not part of the current contract.

## VS Code Setup

This workspace recommends official .NET extensions via `.vscode/extensions.json`:

- `ms-dotnettools.csdevkit`
- `ms-dotnettools.csharp`
- `ms-dotnettools.vscode-dotnet-runtime`

Press `F5` and choose `REPL (traffic sample)` to run the sample directly.

## Repository Layout

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
```