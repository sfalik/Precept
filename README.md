# StateMachine 🚦

StateMachine is a .NET DSL-driven state/workflow engine focused on deterministic transition evaluation and persisted instance state.

## Why this project

- **Inspect before fire**: evaluate whether an event is defined/accepted before mutating state.
- **Inspect before fire**: evaluate whether a transition is not available from the current state, blocked, or enabled before mutating state.
- **Persistable runtime instances**: load/save a JSON instance with current state and instance 
- **Explicit event arguments**: per-call arguments are separate from persisted instance 
- **Transition-scoped data updates**: `transform <Key> = ...` assignments run only on accepted `fire`.

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

Note: interactive REPL does not accept inline JSON event arguments for `inspect`/`fire`; use prompted values for required args.
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

number VehiclesWaiting
boolean LeftTurnQueued
string? EmergencyReason

state Red
state Green
state Yellow
state FlashingGreen
state FlashingRed

event Advance
event Emergency
  string AuthorizedBy
  string Reason
event ClearEmergency
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
  if Emergency.AuthorizedBy != "" && Emergency.Reason != ""
    transform EmergencyReason = Emergency.Reason
    transition FlashingRed
  else
    reject "AuthorizedBy and Reason are required to activate emergency mode"

from FlashingRed on Advance
  no transition

from FlashingRed on ClearEmergency
  transform EmergencyReason = null
  transition Red
```

The full file (with block comments) is at [`trafficlight.sm`](trafficlight.sm).

## DSL Syntax Reference

```text
machine <Name>
state <StateName>

event <EventName>
[ <string|number|boolean|null>[?] <ArgName> { <ArgDecl> } ]

<string|number|boolean|null>[?] <FieldName> { <FieldDecl> }

from <any|StateA[,StateB...]> on <EventName>
(
    if <Guard> [ transform <Field> = <Expr> ] ( transition <ToState> | no transition )
  | else if <Guard> [ transform <Field> = <Expr> ] ( transition <ToState> | no transition )
  | else [ transform <Field> = <Expr> ] ( transition <ToState> | reject <Reason> | no transition )
  | [ transform <Field> = <Expr> ] ( transition <ToState> | reject <Reason> | no transition )
)+

<Expr> := <Literal|<DataField>|<EventName>.<ArgName>>
<Literal> := <null|true|false|number|string>
```

Constraints:

- `()+` means one-or-more branch lines in the `from ... on ...` body.
- `if` and `else if` branches must end in either `transition <ToState>` or `no transition`.
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
  transform SubmittedAt = "2026-02-27T00:00:00Z"
  transition PendingReview
```

3) Guarded routing with fallback reject

```text
from PendingReview on Approve
  if Score >= 80
    transition Approved
  else
    reject "Score below approval threshold"
```

4) Event args copied into persisted data

```text
event Escalate
  string Reason
from PendingReview on Escalate
  if Escalate.Reason != ""
    transform EscalationReason = Escalate.Reason
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
- `Tab` for command/event/theme completion
- `Up/Down` history, `Right Arrow` accept-inline completion
- `state`
- `events`
- `data`
- `inspect [EventName]`
- `fire <EventName>`
- `load <path>`
- `save [path]`
- `exit | quit`

`symbols test` prints a compact ASCII/Unicode compatibility matrix so you can choose the best symbol mode for your current terminal/font.
`style preview` prints a compact scenario matrix in your terminal using the current theme.
`style preview all` prints the same compact scenario matrix for all built-in themes in one run.
Style previews render a realistic timeline transcript using the same compact line structures as live REPL output and now exercise the full compact outcome surface (inspect-all/single, multi-target reachable/unreachable child arrows, blocked guard-linked child previews, fire success, undefined unknown-event and no-transition cases, argument prompts, and truncation behavior).
`style theme list` shows available themes: `mono-accent`, `muted`, `nord-crisp`, `tokyo-night`, `github-dark`, `solarized-modern`, `dracula`, `rose-pine`, `everforest`, `catppuccin-mocha`, `one-dark-pro`, `gruvbox-dark`, `material-ocean`, `night-owl`, `palenight`, `cobalt2`, `ayu-mirage`, `horizon-dark`, `kanagawa-wave`, `synthwave-84`, `monokai-pro`, `sepia-soft`, `forest-night`, `iceberg`, `carbon`, `neon-mint`, `ember`, `lavender-mist`, `slate-blue`, and `slate-blue-vivid`.
`style theme <name>` applies a theme immediately for the current REPL session.
Interactive REPL supports `Tab` completion for commands, event names (`inspect`/`fire`), `style theme` names, and `symbols` subcommands.
Interactive REPL also supports `Up`/`Down` command history navigation and `Right Arrow` to accept the current inline completion.
Interactive REPL type-ahead shows inline completion hints for the token you are currently typing.
Default theme at startup is `slate-blue-vivid`.
`inspect` without an event name evaluates all workflow events and lists callable plus guarded events from the current state.
Inspect preview is eager: if current data (and any provided args) is sufficient to resolve a concrete transition, inspect shows the concrete preview target.
When more than one transition target is defined for an event from the current state, inspect keeps the resolved target on the event line and renders alternate targets as child lines with an unreachable marker (`──✕`, ASCII: `--X`).
When required args are missing and guard logic depends on those missing args, inspect treats the outcome as ambiguous and renders child target lines (`├─`/`└─`) with hollow preview arrows (`──▷`) for possible transition targets.
For ambiguous inspect results with a terminal `reject`, the reject reason is shown on the event line.
In interactive REPL mode, `fire <EventName>` prompts for each required event arg (with type hints like `[string]`, `[number]`, `[boolean]`).
Interactive fire prompts apply input coercion for declared scalar arg types and re-prompt until a valid value is entered.
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

## Language Server (MVP)

Project:

- `tools/StateMachine.Dsl.LanguageServer`

Run manually over stdio:

```sh
dotnet run --project tools/StateMachine.Dsl.LanguageServer
```

Current MVP capabilities:

- Parses/compiles `.sm` documents on open/change/save and publishes diagnostics.
- Converts parser `Line N:` failures into line-scoped LSP error diagnostics.
- Provides completion items for DSL keywords.
- Provides contextual completion for known `state` names (`from`, `transition`) and known `event` names (`on`).
- Provides contextual guard completion for `if`/`else if` lines (data fields, operators/literals, and current-event argument references).
- Provides contextual transform completion for `transform` lines (data-field targets before `=`, expression suggestions after `=`).
- Provides event-argument member completion after `<EventName>.` in guard and transform expressions.
- Provides snippet-style completions for common branch/outcome patterns (`from ... on ...`, `if/else if/else`, `transition`, `reject`, `no transition`, and `transform`).
- Provides semantic token highlighting for declarations/usages (keywords, state/event symbols, variables, strings, numbers, operators, comments).

Notes:

- Server transport is stdio and is ready for editor-client wiring.
- This repository now includes a local VS Code client extension for automatic `.sm` activation.

## VS Code Client Extension (MVP)

Project:

- `tools/StateMachine.Dsl.VsCode`

Setup:

```sh
cd tools/StateMachine.Dsl.VsCode
npm install
npm run compile
```

Local package (no publishing/CI):

```sh
cd tools/StateMachine.Dsl.VsCode
npm run package:local
code --install-extension .\state-machine-dsl-vscode-0.0.1.vsix
```

One-command local update loop (package + install):

```sh
cd tools/StateMachine.Dsl.VsCode
npm run loop:local
```

Then in VS Code run: `Developer: Reload Window`.

In-IDE trigger (no terminal typing):

- `Terminal: Run Task` → `extension: loop local install`

Run/debug in VS Code:

- Press `F5` and select `Extension (StateMachine DSL)`.
- Open a `.sm` file in the Extension Development Host to auto-start the language server client.
- Client startup in Extension Development Host does not require opening a workspace folder first.
- For locally installed VSIX testing, open either the repository root (`StateMachine`) or `tools/StateMachine.Dsl.VsCode` so the extension can resolve `tools/StateMachine.Dsl.LanguageServer/StateMachine.Dsl.LanguageServer.csproj`.
- `.sm` files include TextMate syntax highlighting (keywords, declarations, strings, numbers, operators, comments).
- Semantic highlighting is enabled (`semanticHighlighting`) and enriched by language-server semantic tokens.
- Local packaging uses `npm run package:local` and emits a `.vsix` in `tools/StateMachine.Dsl.VsCode`.
- Fast local iteration uses `npm run loop:local` to package and force-install the latest VSIX.
- Local VSIX packaging includes language-client runtime dependencies (`node_modules`) required for extension activation.

Troubleshooting completion/diagnostics:

- Open `View: Output` and select `StateMachine DSL` to inspect language-client startup logs and server path resolution.

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
- Explicit `data` contracts and event argument contracts (scalar-only)
- Instance-first runtime APIs (`CreateInstance`, `Inspect`, `Fire`)
- Guard evaluation with rejection reasons
- Transition data assignments (`transform <Key> = ...`) on accepted `fire`
- Transform parser/model foundation for B-v1: transitions now carry ordered transform-assignment lists and transform expressions parse into an expression AST.
- Shared AST expression evaluator now drives guard evaluation and transform expression execution.
- CLI REPL + script execution
- Active test coverage in `test/StateMachine.Tests/DslWorkflowTests.cs` and `test/StateMachine.Tests/CliRenderingTests.cs`
- Active parser/runtime coverage also includes expression AST parsing/edge-case diagnostics, transform parsing coverage, and runtime evaluator operator/short-circuit behavior in `test/StateMachine.Tests/DslExpressionParserTests.cs`, `test/StateMachine.Tests/DslExpressionParserEdgeCaseTests.cs`, `test/StateMachine.Tests/DslTransformParsingTests.cs`, and `test/StateMachine.Tests/DslExpressionRuntimeEvaluatorBehaviorTests.cs`.
- Language server MVP in `tools/StateMachine.Dsl.LanguageServer` (stdio diagnostics + completion)
- Language server MVP in `tools/StateMachine.Dsl.LanguageServer` (stdio diagnostics + completion + semantic tokens)
- VS Code client MVP in `tools/StateMachine.Dsl.VsCode` (auto-start for `.sm` files)
- VS Code client contributes TextMate syntax highlighting for `.sm` files

Pending:

- Extension packaging/publishing workflow
- Transform/expression expansion (B-v1) is design-locked but not yet implemented in runtime/parser.

## Transform/Expression Roadmap (Design-Locked, Pending)

The following decisions are locked for the next transform/expression iteration and are documented here for clarity. Current runtime behavior remains the source of truth until this work is implemented.

Current progress:

- Phase 1 (parser/model foundation) is implemented.
- Phase 2 (shared expression evaluator integration) is implemented for guards and transform expression evaluation.
- Runtime still applies only one assignment during fire-path data update (current behavior uses the last transform assignment for backward compatibility) until atomic multi-transform execution lands.

- Atomic batch per selected branch: all transform assignments in a branch commit together or none commit.
- Multiple `transform` lines per branch are supported; each `transform` remains a single assignment.
- In-branch transform evaluation is read-your-writes in declaration order.
- Strict fail-fast typing (no implicit coercion).
- Guard/transform consistency: shared expression semantics; guards must evaluate to `boolean`, transforms must match target field contract type.
- Planned B-v1 expression scope includes operators `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!`.
- Planned B-v1 string `+` is strong concat only (both operands must be strings).
- Planned B-v1 null handling: null checks via `==`/`!=` are allowed; arithmetic/ordered comparisons/boolean ops/concat with null are invalid.

Example target behavior after implementation:

```text
from Active on Retry
  if RetryCount != null && RetryCount % 2 == 0
    transform RetryCount = RetryCount + 1
    transform AuditMessage = "Retry #" + RetryCount
    transition Active
  else
    reject "RetryCount is unavailable"
```

  Implementation checklist (B-v1):

  - Parser/model: support multiple ordered `transform` assignments per selected branch and operator-aware expression parsing.
  - Runtime: shared guard/transform expression evaluator with strict type/null semantics, strong string concat, short-circuit boolean logic, and atomic batch commit.
  - Inspect/fire parity: same guard evaluation semantics in inspect and fire; transform expression failures reject fire and commit no transform changes.
  - Tooling: language-server diagnostics/completion updates for operator/type/null rules and expression authoring.
  - Tests: precedence/associativity, read-your-writes, atomic rollback, strict typing, null behavior, strong concat, and inspect/fire parity coverage.

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

tools/StateMachine.Dsl.LanguageServer/
    Program.cs

tools/StateMachine.Dsl.VsCode/
  src/extension.ts

test/StateMachine.Tests/
  CliRenderingTests.cs
    DslWorkflowTests.cs
```
