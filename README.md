# StateMachine 🚦

StateMachine is a .NET DSL-driven state/workflow engine focused on deterministic transition evaluation and persisted instance state.

## Why this project

- **Inspect before fire**: evaluate whether an event is defined/accepted before mutating state.
- **Inspect before fire**: evaluate whether a transition is not available from the current state, blocked, or enabled before mutating state.
- **Persistable runtime instances**: load/save a JSON instance with current state and instance data.
- **Explicit event arguments**: per-call arguments are separate from persisted instance data.
- **Transition-scoped data updates**: `transform data.<Key> = ...` assignments run only on accepted `fire`.

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
  ├─ Advance
  │ ├─ ──▷ FlashingGreen
  │ └─ ──✕ Green
  └─ Emergency(AuthorizedBy,Reason) ⚠ | AuthorizedBy and Reason are required to activate emergency mode
    └─ ──▷ FlashingRed

Red › fire Advance
  └─ Advance ✔ ──▶ FlashingGreen

FlashingGreen › fire Advance
  └─ Advance ✔ ──▶ Green

Green › fire Advance
  └─ Advance ✔ ──▶ Yellow

Yellow › fire Advance
  └─ Advance ✔ ──▶ Red

Red › inspect Advance
  └─ Advance
    ├─ ──▷ FlashingGreen
    └─ ──✕ Green

Red › fire Emergency
  └─ Emergency ⚠ | Event argument validation failed: required argument 'AuthorizedBy' for event 'Emergency' is missing.

Red › fire Emergency
  │ AuthorizedBy: Dispatcher
  │ Reason: Accident
  └─ Emergency ✔ ──▶ FlashingRed

FlashingRed › fire Advance
  └─ Advance ✖ | no transition from FlashingRed

FlashingRed › fire ClearEmergency
  └─ ClearEmergency ✔ ──▶ Red
```

Note: you can also pass inline JSON event arguments (for example `fire Emergency '{"AuthorizedBy":"Dispatcher","Reason":"Accident"}'`).
Note: output is colorized by default (success/warning/error); use `--no-color` to disable.
Note: `inspect` after the fourth `fire Advance` shows blocked because `data.LeftTurnQueued` was cleared and `data.VehiclesWaiting` is 0.

Quick script run (non-interactive):

```sh
dotnet run --project tools/StateMachine.Dsl.Cli -- ./trafficlight.sm --instance ./traffic.instance.json --script ./traffic.script.txt --echo --unicode
```

Expected compact output shape:

```text
sm> fire Advance
[INFO] Advance: Red → FlashingGreen
sm> fire Emergency '{"AuthorizedBy":"Dispatcher","Reason":"Accident"}'
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

```text
machine TrafficLight

state Red
state Green
state Yellow
state FlashingGreen
state FlashingRed

event Advance
event Emergency
  args
    AuthorizedBy: string
    Reason: string
event ClearEmergency

data
  VehiclesWaiting: number
  LeftTurnQueued: boolean
  EmergencyReason: string?

from Red on Advance
  if data.LeftTurnQueued
    transform data.LeftTurnQueued = false
    transition FlashingGreen
  else if data.VehiclesWaiting > 0
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
  if arg.AuthorizedBy != "" && arg.Reason != ""
    transform data.EmergencyReason = arg.Reason
    transition FlashingRed
  else
    reject "AuthorizedBy and Reason are required to activate emergency mode"

from FlashingRed on Advance
  no transition

from FlashingRed on ClearEmergency
  transform data.EmergencyReason = null
  transition Red
```

The full file (with block comments) is at [`trafficlight.sm`](trafficlight.sm).

## DSL Syntax Reference (Linear)

```text
machine <Name>
state <StateName>

event <EventName>
[ args <ArgName>: <string|number|boolean|null>[?] { <ArgDecl> } ]

data
<FieldName>: <string|number|boolean|null>[?] { <FieldDecl> }

from <any|StateA[,StateB...]> on <EventName>
(
    if <Guard> [ transform <Field|data.Field> = <Expr> ] transition <ToState>
  | else if <Guard> [ transform <Field|data.Field> = <Expr> ] transition <ToState>
  | else [ transform <Field|data.Field> = <Expr> ] ( transition <ToState> | reject <Reason> | no transition )
  | [ transform <Field|data.Field> = <Expr> ] ( transition <ToState> | reject <Reason> | no transition )
)+

<Expr> := <Literal|Identifier|arg.<ArgName>|data.<FieldName>>
<Literal> := <null|true|false|number|string>
```

Constraints:

- `()+` means one-or-more branch lines in the `from ... on ...` body.
- `if` and `else if` branches must end in `transition <ToState>`.
- `else` may end in `transition`, `reject`, or `no transition`.
- `reason "..."` is valid only on `reject`.
- Unsupported syntax: `states ...`, `events ...`, `transition A -> B on E ...`, `set ...`.

## DSL Cookbook

Practical patterns that map directly to the syntax:

1) Unconditional transition

```text
from Draft on Submit
  transition PendingReview
```

2) Transition with data update

```text
from Draft on Submit
  transform data.SubmittedAt = "2026-02-27T00:00:00Z"
  transition PendingReview
```

3) Guarded routing with fallback reject

```text
from PendingReview on Approve
  if data.Score >= 80
    transition Approved
  else
    reject "Score below approval threshold"
```

4) Event args copied into persisted data

```text
event Escalate
  args
    Reason: string

from PendingReview on Escalate
  if arg.Reason != ""
    transform data.EscalationReason = arg.Reason
    transition Escalated
  else
    reject "Reason is required"
```

5) Multi-source handler with `from any`

```text
event Archive

from any on Archive
  transition Archived
```

6) Explicitly disable an event in a state

```text
from Archived on Submit
  no transition
```

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
- `clear`
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
`style preview` prints a compact scenario matrix in your terminal using the current theme.
`style preview all` prints the same compact scenario matrix for all built-in themes in one run.
Style previews render a realistic timeline transcript using the same compact line structures as live REPL output and now exercise the full compact outcome surface (inspect-all/single, multi-target reachable/unreachable child arrows, blocked guard-linked child previews, fire success, undefined unknown-event and no-transition cases, argument prompts, and truncation behavior).
`style theme list` shows available themes: `mono-accent`, `muted`, `nord-crisp`, `tokyo-night`, `github-dark`, `solarized-modern`, `dracula`, `rose-pine`, and `everforest`.
`style theme <name>` applies a theme immediately for the current REPL session.
Default theme at startup is `mono-accent`.
`inspect` without an event name evaluates all workflow events and lists callable plus guarded events from the current state.
Inspect preview is eager: if current data (and any provided args) is sufficient to resolve a concrete transition, inspect shows the concrete preview target.
When more than one transition target is defined for an event from the current state, inspect keeps the resolved target on the event line and renders alternate targets as child lines with an unreachable marker (`──✕`, ASCII: `--X`).
When required args are missing and guard logic depends on those missing args, inspect treats the outcome as ambiguous and renders child target lines (`├─`/`└─`) with hollow preview arrows (`──▷`) for possible transition targets.
For ambiguous inspect results with a terminal `reject`, the reject reason is shown on the event line.
In interactive REPL mode, `fire <EventName>` prompts for each required event key individually if no inline event arguments are provided.
In interactive REPL mode, `data` renders a readable key-value list by default; use output json to emit JSON.
Interactive REPL uses compact output only.
Interactive REPL exits cleanly on stdin EOF (for example, after piped input completes).
Interactive compact output is intentionally concise and omits repeated command/event labels.
Interactive compact mode uses a timeline-style prompt (`Red ›`) and branch prefixes (`├─`/`└─`) with semantic transition arrows: preview uses `──▷` (ASCII: `-->`) and committed success uses `──▶` (ASCII: `==>`).
Compact color roles are consistent across inspect/fire/style-preview: events use event color, states use state color, status markers (`✔`/`⚠`/`✖`) use success/warn/error colors, reachable preview arrows (`──▷`/`-->`) and committed fire arrows (`──▶`/`==>`) use success color, and unreachable child arrows (`──✕`/`--X`) use error color while target state labels remain state-colored.
Structural timeline glyphs and indentation (`├─`, `└─`, `│`) are rendered in neutral/meta color so only semantic tokens carry status/event/state coloring.
When a guarded event is blocked but still lists candidate targets, those child preview arrows (`──▷`/`-->`) use warning color to indicate guard-linked conditional reachability.
Single-event `inspect <EventName>` preview output is visually highlighted from inspect-all rows to emphasize direct command results.
Interactive compact inspect lists use natural spacing (no fixed event-column padding) to avoid wide gaps with long event signatures.
Interactive compact inspect/fire outcome rows stay on one line; long status text is truncated with `...` to preserve timeline/arrow alignment.
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

## Status

Implemented now:

- Line-based DSL parser/runtime in `src/StateMachine/Dsl/*`
- Explicit `data` contracts and event `args` contracts (scalar-only)
- Instance-first runtime APIs (`CreateInstance`, `Inspect`, `Fire`)
- Guard evaluation with rejection reasons
- Transition data assignments (`transform data.<Key> = ...`) on accepted `fire`
- CLI REPL + script execution
- Active test coverage in `test/StateMachine.Tests/DslWorkflowTests.cs` and `test/StateMachine.Tests/CliRenderingTests.cs`

Pending:

- Editor tooling (LSP/IntelliSense)

Design-phase compatibility policy:

- Backward compatibility is not required at this time while DSL design is still actively evolving.
- Syntax-level breaking changes are expected during this phase if they improve language clarity.

Concurrency model:

- Definition is immutable and reusable.
- Instance updates are caller-managed values.
- Persistence/write coordination is caller-managed.

Legacy runtime status:

- Active public/runtime path is DSL runtime only.
- Legacy fluent/runtime artifacts are not part of the active contract.

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
  CliRenderingTests.cs
    DslWorkflowTests.cs
```