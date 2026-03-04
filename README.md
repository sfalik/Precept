# StateMachine 🚦

StateMachine is a .NET DSL-driven state/workflow engine focused on deterministic transition evaluation and persisted instance state.

## Why this project

- **Inspect before fire**: evaluate whether an event is defined/accepted before mutating state.
- **Inspect before fire**: evaluate whether a transition is not available from the current state, blocked, or enabled before mutating state.
- **Persistable runtime instances**: load/save a JSON instance with current state and instance 
- **Explicit event arguments**: per-call arguments are separate from persisted instance 
- **Transition-scoped data updates**: `set <Key> = ...` assignments run only on accepted `fire`.

## Current Status

- DSL parser/compiler/runtime is implemented and used by the language server for editor diagnostics and preview execution.
- VS Code extension is implemented with automatic `.sm` language-client activation plus inspector preview panels per file.
- Inspector preview now exchanges live `snapshot`/`fire`/`reset`/`inspect` requests through the language server (`stateMachine/preview/request`) instead of only local mock data. The `inspect` action re-evaluates a single event with user-supplied arguments, enabling real-time guard feedback as the user types.
- Inspector preview layout uses a single unified ELK layered layout with state-machine-tuned options (top-down direction, spline edge routing, model-order cycle breaking, feedback edges for cycles, inside self-loops, inline edge labels, DSL declaration-order node ordering), dynamic per-state node sizing, and responsive viewBox. Reject and no-transition terminal rules are excluded from the diagram graph.
- CLI host has been removed in this branch (hard cut); editor + language server are the active runtime surfaces.

## Quick Start (2 minutes)

Run the language server:

```sh
dotnet run --project tools/StateMachine.Dsl.LanguageServer
```

Run the VS Code extension locally:

```sh
cd tools/StateMachine.Dsl.VsCode
npm install
npm run compile
```

Then press `F5`, open a `.sm` file, and run `StateMachine DSL: Open Inspector Preview`.

## Core Concepts

- **Workflow definition**: immutable compiled DSL (`DslWorkflowDefinition`).
- **Instance**: persisted runtime state + data (`DslWorkflowInstance`).
- **Inspect**: side-effect free transition preview (`──▷`, ASCII fallback: `-->`) with `Undefined | Blocked | NoTransition | Enabled` outcome; interactive compact output phrases `Undefined` as `no transition from <State>`.
- Undefined display wording is FSM-oriented: unknown events are shown as `unknown event`, while known events unavailable from the current state are shown as `no transition from <State>`.
- **Fire**: applies state change and transition data assignments. `NoTransition` outcomes execute `set` assignments but do not change state.
- **Event arguments**: optional per-call JSON object for inspect/fire.
- **Instance data**: persisted data loaded from/saved to instance JSON.

## DSL Example

```text
machine TrafficLight

number VehiclesWaiting = 0
number CycleCount = 0
boolean LeftTurnQueued = false
string? EmergencyReason

state Red initial
state FlashingGreen
state Green
state Yellow
state FlashingRed

event Advance
event Emergency
  string AuthorizedBy
  string Reason
event ClearEmergency
from Red on Advance
  if LeftTurnQueued
    set LeftTurnQueued = false
    set CycleCount = CycleCount + 1
    transition FlashingGreen
  else if VehiclesWaiting > 0
    set CycleCount = CycleCount + 1
    transition Green
  else
    reject "No demand detected at red"

from FlashingGreen on Advance
  set CycleCount = CycleCount + 1
  transition Green

from Green on Advance
  set CycleCount = CycleCount + 1
  transition Yellow

from Yellow on Advance
  set CycleCount = CycleCount + 1
  transition Red

from any on Emergency
  if Emergency.AuthorizedBy != "" && Emergency.Reason != ""
    set EmergencyReason = Emergency.AuthorizedBy + ": " + Emergency.Reason
    transition FlashingRed
  else
    reject "AuthorizedBy and Reason are required to activate emergency mode"

from FlashingRed on Advance
  set CycleCount = CycleCount + 1
  no transition

from FlashingRed on ClearEmergency
  set EmergencyReason = null
  transition Red
```

The full file (with block comments) is at [`samples/trafficlight.sm`](samples/trafficlight.sm).

## Samples

Ready-to-use `.sm` files covering a range of domains and DSL features.

| File | Scenario | Key Features |
|---|---|---|
| [`samples/test.sm`](samples/test.sm) | Minimal linear chain (Start → One → Two → Three → Four → End) | Basic state/event/transition skeleton; good starting template |
| [`samples/trafficlight.sm`](samples/trafficlight.sm) | Traffic-light controller with emergency flashing mode | `from any`, data fields, nullable string, `set`, `no transition`, complex guards |
| [`samples/ecommerce.sm`](samples/ecommerce.sm) | E-commerce order lifecycle (cart → checkout → paid → shipped) | Math expressions (`Quantity * Price`), comma-separated `from`, combined guards |
| [`samples/bugtracker.sm`](samples/bugtracker.sm) | Bug/issue tracker (triage → in-progress → review → resolved → closed) | `from any` intercept, `!IsBlocked` guard, prerequisite-flag guards |
| [`samples/smarthome.sm`](samples/smarthome.sm) | Home security system (disarmed → arming delay → armed away/stay → triggered) | Boolean data fields with defaults, PIN validation, `from any on Disarm` |
| [`samples/hotel-booking.sm`](samples/hotel-booking.sm) | Hotel reservation lifecycle (available → reserved → checked-in → checked-out) | `Nights * Rate` revenue calculation, idempotent `no transition` guard |
| [`samples/package-delivery.sm`](samples/package-delivery.sm) | Parcel delivery with re-delivery and return flow | `if / else if / else` on attempt count, numeric thresholds |
| [`samples/job-application.sm`](samples/job-application.sm) | Hiring pipeline (submitted → screening → phone → technical → offer → hired) | Score-threshold routing, multi-branch pass/fail `if / else` |
| [`samples/bank-loan.sm`](samples/bank-loan.sm) | Loan origination and repayment lifecycle | Running-balance arithmetic, `||` guard, missed-payment default path |
| [`samples/subscription.sm`](samples/subscription.sm) | SaaS subscription billing (free → trial → active → past-due → suspended → cancelled) | `from any on ToggleAutoRenew`, multi-state billing guard |
| [`samples/patient-admission.sm`](samples/patient-admission.sm) | Hospital patient flow (registered → triaged → admitted → treatment → discharge → transferred) | Multiple boolean prerequisite flags, `from any on EmergencyTransfer` |
| [`samples/restaurant-order.sm`](samples/restaurant-order.sm) | Restaurant table order (seated → ordering → kitchen → served → bill → paid) | Item/total accumulation, payment-amount validation |
| [`samples/support-ticket.sm`](samples/support-ticket.sm) | Customer support ticket with escalation and reopen | `from any on CustomerReply`, reopen counter, escalation composite guard |
| [`samples/document-signing.sm`](samples/document-signing.sm) | Multi-party document signing workflow | Signature counter driving transitions, one-time expiry extension flag |
| [`samples/vending-machine.sm`](samples/vending-machine.sm) | Coin-operated vending machine | Credit accumulation, item-price guard, `from any on EnterMaintenance` |
| [`samples/elevator.sm`](samples/elevator.sm) | Elevator controller with door lifecycle and emergency halt | Sensor events (`DoorOpened`, `DoorClosed`, `FloorReached`) vs user events (`HoldDoor`, `CloseDoors`), overload reopening doors mid-close, `from any on TriggerEmergency` |
| [`samples/game-character.sm`](samples/game-character.sm) | RPG character states (alive → stunned/shielded/leveling → dead → respawning) | Shield blocking, XP threshold, nullable `string?` status message, respawn probability |

## DSL Syntax Reference

```text
# Full-line comment
state Idle initial  # Inline comment — # outside a string literal starts a comment;
                    # everything from that # to end of line is ignored

machine <Name>

state <StateName> [initial]

event <EventName>
[ <ScalarType>[?] <ArgName> [= <Literal>] { <ArgDecl> } ]

<ScalarType>[?] <FieldName>

<ScalarType>[?] <FieldName> [= <Literal>]

<ScalarType> := string | number | boolean | null

from <any|StateA[,StateB...]> on <EventName>
(
    if <GuardExpr>
      { set <Field> = <Expr> }
      ( transition <ToState> | no transition )

  | else if <GuardExpr>
      { set <Field> = <Expr> }
      ( transition <ToState> | no transition )

  | else
      { set <Field> = <Expr> }
      ( transition <ToState> | reject "<Reason>" | no transition )

  | { set <Field> = <Expr> }
    ( transition <ToState> | reject "<Reason>" | no transition )
)+

<Expr> := <Literal> | <FieldName> | <EventName>.<ArgName> | ( <Expr> ) | !<Expr> | -<Expr> | <Expr> <BinaryOp> <Expr>

<BinaryOp> := + | - | * | / | % | == | != | < | <= | > | >= | && | ||

<Literal> := null | true | false | <number> | <string>
```

Constraints:

- `()+` means one-or-more branch lines in a `from ... on ...` body.
- Exactly one `state` declaration must include `initial`.
- `event` declarations are optional. A machine with no events is syntactically valid but behaviorally inert; the language server emits a hint when no events are declared.
- `if` and `else if` branches must end in exactly one outcome: `transition <ToState>` or `no transition`.
- `else` (or unguarded body) must end in exactly one outcome: `transition`, `reject`, or `no transition`.
- After an `if`/`else if` chain, a fallback **must** use `else`; a bare block-level outcome after a chain is a parse error.
- `set <Field> = <Expr>` is valid only inside a `from ... on ...` branch body.
- Multiple `set` lines are allowed and execute in declaration order with read-your-writes on the fire path.
- `set` is allowed with `no transition` in all branch contexts; assignments execute on fire but state does not change.
- `reason "..."` is valid only on `reject`.
- Event arguments and persisted data fields are scalar-only (`string|number|boolean|null`, optional `?`).
- Event arguments may declare literal defaults using `<ArgName> = <Literal>`.
- Non-nullable event args without a default are required — the caller must supply them when firing.
- Non-nullable event args with a default use the default when the caller omits them.
- Nullable event args are always optional; if omitted, they default to `null` or the declared default.
- Top-level data fields may declare literal defaults using `<Field> = <Literal>`.
- Defaults are applied when creating instances and can be overridden by caller-supplied instance data.
- Non-nullable top-level data fields must declare defaults.
- Unsupported syntax: `states ...`, `events ...`, and legacy inline form `transition A -> B on E ...`.

## DSL Cookbook

Practical patterns that map directly to the syntax:

1) Mandatory initial state declaration

```text
state Draft initial
state PendingReview
state Approved
```

2) Unconditional transition

```text
from Draft on Submit
  transition PendingReview
```

3) Transition with one field assignment (`set`)

```text
from Draft on Submit
  set SubmittedAt = "2026-02-27T00:00:00Z"
  transition PendingReview
```

4) Guarded routing with `else if` and fallback `reject`

```text
from PendingReview on Approve
  if Score >= 80
    set RiskTier = "Low"
    transition Approved
  else if Score >= 70
    set RiskTier = "Medium"
    transition Approved
  else
    reject "Score below approval threshold"
```

5) Event args + expression assignment

```text
event Escalate
  string Actor
  string Reason

from PendingReview on Escalate
  if Escalate.Actor != "" && Escalate.Reason != ""
    set EscalationReason = Escalate.Actor + ": " + Escalate.Reason
    transition Escalated
  else
    reject "Actor and Reason are required"
```

6) Event args with defaults

```text
event Submit
  string Reason
  number Priority = 1
  string? Note

from Draft on Submit
  set LastPriority = Submit.Priority
  set LastNote = Submit.Note
  transition PendingReview
```

`Reason` is required (non-nullable, no default). `Priority` defaults to `1` if omitted. `Note` is nullable and defaults to `null` if omitted.

7) Multi-source state list (explicit sources)

```text
from Draft,PendingReview on Cancel
  set CancelledAt = "2026-02-27T00:00:00Z"
  transition Cancelled
```

8) Global handler with `from any`

```text
event Archive

from any on Archive
  transition Archived
```

9) Explicitly disable an event in one state

```text
from Archived on Submit
  no transition
```

10) Null-safe numeric guard + read-your-writes multi-set

```text
number? RetryCount
number Attempts = 0
string AuditMessage = ""

from Active on Retry
  if RetryCount != null && RetryCount % 2 == 0
    set RetryCount = RetryCount + 1
    set Attempts = RetryCount
    set AuditMessage = "Retry #" + Attempts
    transition Active
  else
    reject "RetryCount is unavailable"
```

11) Guarded `no transition` with `set` (valid in all branch contexts)

```text
from Active on Pause
  if HasPendingWork
    set PauseAttempts = PauseAttempts + 1
    no transition
  else
    transition Paused
```

12) Unguarded `no transition` with `set`

```text
from FlashingRed on Advance
  set CycleCount = CycleCount + 1
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
    "CycleCount": 0,
    "VehiclesWaiting": 0,
    "LeftTurnQueued": true,
    "EmergencyReason": null
  }
}
```

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
- Provides contextual set completion for `set` lines (data-field targets before `=`, expression suggestions after `=`).
- Provides event-argument member completion after `<EventName>.` in guard and set expressions.
- Provides snippet-style completions for common branch/outcome patterns (`from ... on ...`, `if/else if/else`, `transition`, `reject`, `no transition`, and `set`).
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

Fast inner-loop (no VSIX package/install):

- Start TypeScript watch once:

```sh
cd tools/StateMachine.Dsl.VsCode
npm run dev:watch
```

- In VS Code, press `F5` and select `Extension (StateMachine DSL) Fast Dev`.
- Keep watch running while iterating; reload the Extension Development Host window to pick up changes quickly.

In-IDE trigger (no terminal typing):

- `Terminal: Run Task` → `extension: loop local install`
- `Terminal: Run Task` → `extension: watch`

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
- Local VSIX packaging also includes webview assets under `webview/` required by inspector preview.
- Inspector preview can be opened from command palette via `StateMachine DSL: Open Inspector Preview` while editing a `.sm` file.
- Preview opens as dedicated webview panels per `.sm` file (independent panel/session per file URI).
- Preview panels request live snapshots from the language server and refresh from unsaved in-memory `.sm` edits as the document changes.
- Preview also refreshes on `.sm` save and when an existing panel is re-focused/revealed.
- Snapshot updates are sequence-ordered so stale async responses cannot overwrite newer in-memory edits.
- Preview webview accepts both camelCase and PascalCase snapshot payload keys for robust host/runtime compatibility.
- `Reload` requests a fresh live snapshot (with a short retry) from the current in-memory editor buffer; it does not require saving first.
- Preview event actions (`fire`, `reset`, `replay`) are executed via the language server runtime session for that file.
- The language server binds preview requests with a typed JSON-RPC request handler and declares `[Method("stateMachine/preview/request")]` on `SmPreviewRequest` to ensure runtime method registration.

Troubleshooting completion/diagnostics:

- Open `View: Output` and select `StateMachine DSL` to inspect language-client startup logs and server path resolution.

### Inspector Preview UI (Current)

- Runtime preview uses `tools/StateMachine.Dsl.VsCode/webview/inspector-preview.html`, which is the production webview sourcing all state from language-server snapshots. The file contains no hardcoded demo data and requires the VS Code extension host to function.
- Mock reference remains intact at `tools/StateMachine.Dsl.VsCode/mockups/interactive-inspector-mockup.html` and is not used as the runtime source file.
- State graph/events/data visuals follow the mock-style presentation while `snapshot`/`fire`/`reset`/`replay` requests execute against the real `.sm` runtime session.
- Runtime layout uses a single unified ELK layered graph computed in the extension host and attached as `snapshot.layout` (`nodes` with per-node `width`/`height`, `edges` with ELK-routed bend-point arrays, `width`, `height`).
- ELK options are state-machine-tuned: top-down direction, spline edge routing, model-order cycle breaking, feedback edges for cycle handling, inside self-loops, inline edge labels, network-simplex node placement, and DSL declaration-order node/port ordering.
- Reject and no-transition terminal rules are excluded from the layout graph and diagram edges; only real state-change transitions are rendered.
- Node dimensions are computed dynamically from state name length (8.5px char width, 36px padding, 80px minimum width, 40px fixed height).
- Edge paths use Catmull-Rom to cubic Bézier spline conversion for smooth curves from ELK bend points.
- Webview viewBox is set responsively from ELK-computed graph dimensions with 50px padding per side (minimum 600×300).
- If ELK layout data is missing or empty, the diagram shows an error message instead of attempting a fallback layout.
- Supported runtime actions are `snapshot` (reload), `fire`, `reset`, and `replay` via `stateMachine/preview/request`.
- `Reload` requests a fresh runtime snapshot from the current in-memory editor text.
- Activity log lines are sourced from runtime responses (including replay messages and snapshot failures).

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
- Transition data assignments (`set <Key> = ...`) on accepted `fire`
- Set parser/model foundation for B-v1: transitions now carry ordered set-assignment lists and set expressions parse into an expression AST.
- Shared AST expression evaluator now drives guard evaluation and set expression execution.
- Atomic ordered multi-set execution is implemented on fire-path updates with read-your-writes and all-or-nothing commit semantics.
- Active test coverage in `test/StateMachine.Tests/DslWorkflowTests.cs`
- Active parser/runtime coverage also includes expression AST parsing/edge-case diagnostics, set parsing coverage, and runtime evaluator operator/short-circuit behavior in `test/StateMachine.Tests/DslExpressionParserTests.cs`, `test/StateMachine.Tests/DslExpressionParserEdgeCaseTests.cs`, `test/StateMachine.Tests/DslSetParsingTests.cs`, and `test/StateMachine.Tests/DslExpressionRuntimeEvaluatorBehaviorTests.cs`.
- Language-server analyzer coverage now includes null-flow narrowing diagnostics tests in `test/StateMachine.Dsl.LanguageServer.Tests/SmDslAnalyzerNullNarrowingTests.cs`.
- Language server MVP in `tools/StateMachine.Dsl.LanguageServer` (stdio diagnostics + completion)
- Language server MVP in `tools/StateMachine.Dsl.LanguageServer` (stdio diagnostics + completion + semantic tokens)
- Language server semantic diagnostics now validate expression operator/type compatibility, set-target type compatibility, and null-flow narrowing for explicit null checks in `&&`/`||` guard paths.
- Language server completion now includes operator-aware suggestions in guard and set-expression contexts.
- VS Code client MVP in `tools/StateMachine.Dsl.VsCode` (auto-start for `.sm` files)
- VS Code client contributes TextMate syntax highlighting for `.sm` files

Pending:

- Extension packaging/publishing workflow

## Set/Expression Roadmap (Design-Locked)

The following decisions are locked for set/expression behavior and are documented here for clarity.

Current progress:

- Phase 1 (parser/model foundation) is implemented.
- Phase 2 (shared expression evaluator integration) is implemented for guards and set expression evaluation.
- Phase 3 (atomic ordered multi-set execution with read-your-writes) is implemented.

- Atomic batch per selected branch: all set assignments in a branch commit together or none commit.
- Multiple `set` lines per branch are supported; each `set` remains a single assignment.
- In-branch set evaluation is read-your-writes in declaration order.
- Strict fail-fast typing (no implicit coercion).
- Guard/set consistency: shared expression semantics; guards must evaluate to `boolean`, sets must match target field contract type.
- Planned B-v1 expression scope includes operators `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!`.
- Planned B-v1 string `+` is strong concat only (both operands must be strings).
- Planned B-v1 null handling: null checks via `==`/`!=` are allowed; arithmetic/ordered comparisons/boolean ops/concat with null are invalid.

Example behavior:

```text
from Active on Retry
  if RetryCount != null && RetryCount % 2 == 0
    set RetryCount = RetryCount + 1
    set AuditMessage = "Retry #" + RetryCount
    transition Active
  else
    reject "RetryCount is unavailable"
```

  Implementation checklist (B-v1):

  - Parser/model: support multiple ordered `set` assignments per selected branch and operator-aware expression parsing.
  - Runtime: shared guard/set expression evaluator with strict type/null semantics, strong string concat, short-circuit boolean logic, and atomic batch commit.
  - Inspect/fire parity: same guard evaluation semantics in inspect and fire; set expression failures reject fire and commit no set changes.
  - Tooling: continue iterating language-server diagnostics/completion precision for advanced multi-branch null-flow scenarios and richer expression authoring hints.
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

Press `F5` and choose `Extension (StateMachine DSL)` to run the extension host.

## Repository Layout

```text
src/StateMachine/
    Dsl/
        StateMachineDslModel.cs
        StateMachineDslParser.cs
        StateMachineDslRuntime.cs

tools/StateMachine.Dsl.LanguageServer/
    Program.cs

tools/StateMachine.Dsl.VsCode/
  src/extension.ts

test/StateMachine.Tests/
    DslWorkflowTests.cs
```
