# StateMachine 🚦

A .NET state/workflow engine project currently focused on an experimental runtime DSL.

## Quick Start (REPL)

Use the included sample files to start an interactive session immediately:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json
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

sm> fire Advance
Defined: True
Accepted: True
NewState: Yellow

sm> state
Yellow

sm> fire Advance
Defined: True
Accepted: True
NewState: Red

sm> state
Red
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
- Guarded transitions are evaluated against optional runtime context
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

### Quick verify

Start REPL:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json
```

Run commands:

```text
sm> inspect Advance
Defined: True
Accepted: True
Target: Green

sm> fire Advance
Defined: True
Accepted: True
NewState: Green

sm> fire Advance
Defined: True
Accepted: True
NewState: Yellow

sm> fire Advance
Defined: True
Accepted: True
NewState: Red

sm> save
Instance saved: ./traffic.instance.json
```

Result exit codes are outcome-specific:

- `5`: incompatible instance/workflow
- `6`: script command failed

## Sample `.sm`

```text
machine TrafficLight
state Red
state Green
state Yellow
event Advance
transition Red -> Green on Advance
transition Green -> Yellow on Advance
transition Yellow -> Red on Advance
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