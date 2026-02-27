# StateMachine 🚦

StateMachine is a .NET DSL-driven state/workflow engine focused on deterministic transition evaluation and persisted instance state.

## Why this project

- **Inspect before fire**: evaluate whether an event is defined/accepted before mutating state.
- **Inspect before fire**: evaluate whether a transition is not available from the current state, blocked, or enabled before mutating state.
- **Persistable runtime instances**: load/save a JSON instance with current state and instance data.
- **Explicit event arguments**: per-call arguments are separate from persisted instance data.
- **Transition-scoped data updates**: `set Key = ...` assignments run only on accepted `fire`.

## Quick Start (2 minutes)

Run the included traffic-light sample in REPL mode:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json --unicode
```

Then try:

```text
Red › events
  ├─ Advance
  ├─ Emergency
  └─ ClearEmergency

Red › data
  ├─ EmergencyReason: <null>
  ├─ LeftTurnQueued: true
  └─ VehiclesWaiting: 0

Red › inspect
  └─ Advance ──▷ FlashingGreen

Red › fire Advance
  └─ Advance ✔ ──▶ FlashingGreen

FlashingGreen › fire Advance
  └─ Advance ✔ ──▶ Green

Green › fire Advance
  └─ Advance ✔ ──▶ Yellow

Yellow › fire Advance
  └─ Advance ✔ ──▶ Red

Red › inspect Advance
  └─ Advance ⚠ | No demand detected at red

Red › fire Emergency
  └─ Emergency ⚠ | A reason is required to activate emergency mode

Red › fire Emergency '{"Reason":"Accident"}'
  └─ Emergency ✔ ──▶ FlashingRed

FlashingRed › fire Advance
  └─ Advance ✖ | no transition from FlashingRed

FlashingRed › fire ClearEmergency
  └─ ClearEmergency ✔ ──▶ Red
```

Note: you can also pass inline JSON event arguments (for example `fire Emergency '{"Reason":"Accident"}'`).
Note: output is colorized by default (success/warning/error); use `--no-color` to disable.
Note: `inspect` after the fourth `fire Advance` shows blocked because `LeftTurnQueued` was cleared and `VehiclesWaiting` is 0.

Quick script run (non-interactive):

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json --script ./traffic.script.txt --echo --unicode
```

Expected compact output shape:

```text
sm> fire Advance
[INFO] Advance: Red → FlashingGreen
sm> fire Emergency '{"Reason":"Accident"}'
[INFO] Emergency: Red → FlashingRed
```

## Core Concepts

- **Workflow definition**: immutable compiled DSL (`DslWorkflowDefinition`).
- **Instance**: persisted runtime state + data (`DslWorkflowInstance`).
- **Inspect**: side-effect free transition preview (`──▷`, ASCII fallback: `-->`) with `Undefined | Blocked | Enabled` outcome; interactive compact output phrases `Undefined` as `no transition from <State>`.
- Undefined display wording is FSM-oriented: unknown events are shown as `unknown event`, while known events unavailable from the current state are shown as `no transition from <State>`.
- **Fire**: applies state change and transition data assignments.
- **Event arguments**: optional per-call JSON object for inspect/fire.
- **Instance data**: persisted data loaded from/saved to instance JSON.

## DSL Example

The DSL is built up in stages. Each stage introduces new constructs on top of the previous one.

### Stage 1 — One-line transitions

The simplest form: a machine with three states cycling on a single event.

```text
machine TrafficLight
states Red, Green, Yellow
events Advance

from Red on Advance
  transition Green

from Green on Advance
  transition Yellow

from Yellow on Advance
  transition Red
```

### Stage 2 — Guarded transition with reject

Add an `if` guard that blocks the transition when demand is absent. The `reject` statement is the fallback if no guard passes.

```text
from Red on Advance
  if VehiclesWaiting > 0
    transition Green
  reject "No demand detected at red"
```

`if` branches allow an optional `transform` before `transition`. Guards do not carry `reason` — that belongs on `reject` only.

### Stage 3 — Event arguments, transform, `from any`, `no transition`

Add an emergency event that requires a payload, records data on transition, suspends the cycle, and can be cleared.

```text
machine TrafficLight
states Red, Green, Yellow, FlashingRed
events Advance, Emergency, ClearEmergency

from Red on Advance
  if VehiclesWaiting > 0
    transition Green
  reject "No demand detected at red"

from Green on Advance
  transition Yellow

from Yellow on Advance
  transition Red

from any on Emergency
  if Reason != ""
    transform EmergencyReason = Reason
    transition FlashingRed
  reject "A reason is required to activate emergency mode"

from FlashingRed on Advance
  no transition

from FlashingRed on ClearEmergency
  transform EmergencyReason = null
  transition Red
```

- `from any` matches every state; specific `from` headers take priority.
- `transform Key = Expr` runs only on accepted `fire`, never on `inspect`.
- `no transition` makes the outcome explicitly undefined — reported as `no transition from <State>`.
- Event-argument keys (`Reason`) are accessed directly in guards and transform expressions.

### Stage 4 — `else if` / `else` (full sample)

Add a protected left-turn phase (`FlashingGreen`) served ahead of general demand, cleared in one step.

```text
machine TrafficLight
states Red, Green, Yellow, FlashingGreen, FlashingRed
events Advance, Emergency, ClearEmergency

from Red on Advance
  if LeftTurnQueued
    transform LeftTurnQueued = false
    transition FlashingGreen
  else if VehiclesWaiting > 0
    transition Green
  else
    reject "No demand detected at red"

from FlashingGreen on Advance
  transition Green

from Green on Advance
  transition Yellow

from Yellow on Advance
  transition Red

from any on Emergency
  if Reason != ""
    transform EmergencyReason = Reason
    transition FlashingRed
  reject "A reason is required to activate emergency mode"

from FlashingRed on Advance
  no transition

from FlashingRed on ClearEmergency
  transform EmergencyReason = null
  transition Red
```

The full file (with block comments) is at [`trafficlight.sm`](trafficlight.sm).

Supported assignment forms:

- Literal: `transform VehiclesWaiting = 0`
- Null literal: `transform EmergencyReason = null`
- Copy from event arguments: `transform EmergencyReason = Reason`

Each `from ... on ...` block must end with an outcome statement: `transition <State>`, `reject "<message>"`, or `no transition`.
No additional statements are allowed after an outcome statement within the block.
Guarded transitions are expressed only with `if` / `else if` / `else` inside `from ... on ...` blocks.

Current DSL syntax contract:

- Block header: `from <State|State,State|any> on <Event>`
- Guarded branch: `if <Guard>` followed by an indented `transition <State>` (optional `transform` line before transition)
- Additional guarded branches: `else if <Guard>`
- Fallback branch: `else` followed by exactly one outcome path
- Outcome statements:
  - `transition <State>`
  - `reject "<message>"`
  - `no transition`
- `else if` and `else` require a preceding `if`
- `reason` is valid **only** on `reject` statements; writing `reason "..."` on an `if` or `else if` branch is a parse error
- Guarded logic belongs in branch headers (`if` / `else if`) inside `from ... on ...` blocks
- Inline guarded transitions (for example `transition A -> B on E` with trailing guard clauses) are not supported

Authoring rules (detailed):

- Indentation is structural; nested statements must be indented deeper than their parent header.
- `if` and `else if` branch bodies allow:
  - zero or one `transform <Key> = <Expr>`
  - exactly one `transition <State>`
- `if` and `else if` branches cannot end with `reject` or `no transition`; fallback outcomes belong in block-level `else` or final block outcome lines.
- `else` is optional; if present, it must be last and contain exactly one outcome path (`transition`, `reject`, or `no transition`, with optional leading `transform` for transition outcomes).
- A block may end directly with `transition <State>`, `reject "<message>"`, or `no transition` without an `else` branch.
- After the first outcome statement in a block, no further statements are valid.

Resolution semantics:

- Branches are evaluated in source order.
- The first guard that evaluates `true` is selected and its transition is applied.
- If no guarded branch is selected, the block outcome decides behavior:
  - `reject` => inspect/fire result is blocked with the configured reason
  - `no transition` => result is undefined
  - terminal `transition` => unconditional enabled transition

Unsupported in transforms:

- `arg.<Key>` and `data.<Key>` references are rejected during parse/compile.

Reserved literals in transform expressions: `true`, `false`, and `null`.
Event-arg keys with those exact names are reserved and cannot be referenced as transform identifiers.

## Instance JSON Example

```json
{
  "workflowName": "TrafficLight",
  "currentState": "Red",
  "lastEvent": null,
  "updatedAt": "2026-02-27T00:00:00+00:00",
  "instanceData": {
    "VehiclesWaiting": 0,
    "LeftTurnQueued": true,
    "EmergencyReason": null
  }
}
```

## CLI Usage

Launch REPL:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json --unicode
```

Run script mode:

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json --script ./traffic.script.txt --unicode
```

CLI output options:

- `--echo` (echo script commands in script mode)
- `--no-color` (disable ANSI coloring)
- symbols default to `auto` detection (uses Unicode if terminal signals support; otherwise ASCII)
- `--unicode` (force Unicode symbols in compact mode)
- `--ascii` (force ASCII symbols in compact mode)

Notes:

- Interactive REPL is compact-only.
- Script mode emits log-style output lines (`INFO`/`WARN`/`ERROR`).

REPL commands:

- `help`
- `symbols [auto|ascii|unicode|test]`
- `style preview [all]`
- `style theme <name|list>`
- `state`
- `events`
- `data`
- `inspect [EventName] [event-args-json]`
- `fire <EventName> [event-args-json]`
- `load <path>`
- `save [path]`
- `exit | quit`

`symbols test` prints a compact ASCII/Unicode compatibility matrix so you can choose the best symbol mode for your current terminal/font.
`style preview` prints a style sample in your terminal using compact styling and the current theme.
`style preview all` prints the same compact sample for all built-in themes in one run.
Style previews render a realistic timeline transcript using traffic-light labels (for example, `Red`, `Advance`, `Green`) and include preview/success/error/warning paths so branch alignment and transition flow are easy to evaluate.
`style theme list` shows available themes: `muted`, `nord-crisp`, `tokyo-night`, `github-dark`, `solarized-modern`, and `mono-accent`.
`style theme <name>` applies a theme immediately for the current REPL session.
Default theme at startup is `github-dark`.
`inspect` without an event name evaluates all workflow events and lists only those callable from the current state.
In interactive REPL mode, `fire <EventName>` prompts for each required event key individually if no inline event arguments are provided.
In interactive REPL mode, `data` renders a readable key-value list by default; use output json to emit JSON.
Interactive REPL uses compact output only.
Interactive compact output is intentionally concise and omits repeated command/event labels.
Interactive compact mode uses a timeline-style prompt (`Red ›`) and branch prefixes (`├─`/`└─`) with semantic transition arrows: preview uses `──▷` (ASCII: `-->`) and committed success uses `──▶` (ASCII: `==>`).
Single-event `inspect <EventName>` preview output is visually highlighted from inspect-all rows to emphasize direct command results.
Interactive compact inspect lists align event names for faster visual scanning.
Interactive compact `events` output aligns event-name columns for visual consistency.
Interactive compact non-prompt lines (including argument prompts like `│  Reason: value`) are rendered as timeline children beneath each prompt.
Verbose mode renders structured table/panel output for inspect/fire details and callable-event listings.

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

Design-phase compatibility policy (current):

- Backward compatibility is not required at this time while DSL design is still actively evolving.
- Syntax-level breaking changes are expected during this phase if they improve language clarity.

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