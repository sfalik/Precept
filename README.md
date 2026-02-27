# StateMachine 🚦

A .NET state/workflow engine project currently focused on an experimental runtime DSL.

## Quick Start

Use the included sample files to start an interactive session immediately:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json
```

Note: when passing inline event arguments in REPL commands, wrap the JSON with single quotes (for example: `fire Emergency '{"Reason":"Accident"}'`).

Then run a few commands in the prompt:

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

sm> state
FlashingRed
```

This startup mode always begins from a persisted instance.

## Current Status

### Implemented now

- Line-based DSL model/parser/runtime in `src/StateMachine/Dsl/*`
- Persistable workflow instance model (`DslWorkflowInstance`) with workflow-name compatibility checks
- Instance APIs in runtime (`CreateInstance`, instance-based `Inspect`, instance-based `Fire`)
- CLI tooling in `tools/StateMachine.Dsl.Cli`:
  - `repl` (interactive)
  - `script` mode (`--script`) for non-interactive command files
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
- Guarded transitions are evaluated against optional event arguments; when provided, they are used for that call without mutating persisted instance data
- Transition data assignments (`set Key = ...`) are applied only on accepted `fire` calls
- If all guarded candidates fail, result is rejected with aggregated reasons
- Instance-based inspect/fire validates workflow name compatibility before evaluating transitions
- CLI startup is instance-first: `--instance` is required

## Installation

```sh
dotnet add package StateMachine
```

## VS Code (official extensions)

This workspace is configured for Microsoft's official .NET tooling:

- `ms-dotnettools.csdevkit`
- `ms-dotnettools.csharp`
- `ms-dotnettools.vscode-dotnet-runtime`

When opened in VS Code, the workspace recommends these extensions via `.vscode/extensions.json`.
The default solution is preconfigured in `.vscode/settings.json` as `dotnet.defaultSolution = "StateMachine.slnx"`.

Press `F5` and choose `REPL (traffic sample)` to build and launch the CLI in REPL mode with:

- `./trafficlight.sm`
- `--instance ./traffic.instance.json`

## Experimental DSL CLI

Launch REPL with an instance:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json
```

Run a script (file of REPL commands):

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json --script ./traffic.script.txt
```

The repository includes ready-to-run examples:

- `./trafficlight.sm`
- `./traffic.instance.json`

Result exit codes are outcome-specific:

- `5`: incompatible instance/workflow
- `6`: script command failed

## Sample `.sm`

This sample demonstrates:

- Guarded transitions using numeric event-argument/data checks
- Multiple events (`Advance`, `Emergency`, `ClearEmergency`)
- An emergency override state (`FlashingRed`) reachable from normal flow states
- Transition data updates using `set <Key> = <expr>` during `fire`
- Event-argument-aware behavior that can be inspected/fired from REPL or script mode

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

## Sample `traffic.instance.json`

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