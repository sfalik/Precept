# StateMachine Design Notes

Date: 2026-02-24

This document tracks current design decisions for the active implementation on this branch.

## Current Scope

Implementation focus is the DSL runtime path:

- parse `.sm` definitions
- validate semantic correctness
- execute inspection/fire against a compiled in-memory workflow definition

### Design-Phase Compatibility Policy

- Backwards compatibility is not required at this time.
- The DSL is in active design phase; syntax and behavior may change to improve clarity and consistency.
- The current canonical guarded-branch syntax is `from ... on ...` with `if` / `else if` / `else`.
- Legacy guard keywords are removed from the DSL syntax surface.

### DSL Design Intent

- Readability-first: rules should read top-to-bottom as a deterministic decision flow.
- Single obvious style: one canonical guarded-branch form for authoring and tooling.
- Deterministic outcomes: each `from ... on ...` block resolves to exactly one result (`Enabled`, `Blocked`, or `Undefined`).
- Strict structure over permissiveness: parser rejects ambiguous or mixed-shape statements early with explicit errors.
- Outcome-first clarity: fallback behavior is explicit (`reject` or `no transition`) rather than implicit failure.
- Tooling-friendly grammar: indentation + fixed keywords provide stable anchors for completion, diagnostics, and formatting.

### DSL Syntax Contract (Current)

Canonical linear form:

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

Canonical constraints:

- `()+` means one-or-more lines in a `from ... on ...` body.
- `if` and `else if` must end with `transition <State>`.
- `else` may end with `transition`, `reject`, or `no transition`.
- `reason "..."` is valid only on `reject`.
- Unsupported syntax: `states ...`, `events ...`, `transition A -> B on E ...`, `set ...`.

Block-authoring equivalent (same semantics):

- Block header: `from <State|State,State|any> on <Event>`
- Guarded branch header: `if <Guard>`
- Additional guarded branch headers: `else if <Guard>`
- Optional fallback header: `else`
- Allowed branch body statements:
  - `transform <Key> = <Expr>` (optional)
  - `transition <State>` (required for `if`/`else if` branch bodies)
- Allowed block outcome statements:
  - `transition <State>`
  - `reject "<message>"`
  - `no transition`
- Validation rules:
  - Every `from ... on ...` block must end with an outcome statement.
  - No statements are allowed after an outcome statement.
  - `else if` and `else` require a preceding `if` in the same block.
  - `reason "<message>"` is valid **only** on `reject` statements; writing it on `if` or `else if` is a parse error.
  - Inline guarded transitions (`transition A -> B on E` with trailing guard clauses) are invalid.

#### Branch/body constraints

- `if` and `else if` branch bodies:
  - allow optional `transform <Key> = <Expr>`
  - require exactly one `transition <State>`
  - do not allow `reject` or `no transition`
- `else` branch body:
  - may include optional `transform <Key> = <Expr>`
  - must end in exactly one outcome statement: `transition <State>`, `reject "<message>"`, or `no transition`

#### Evaluation model

- Branches are evaluated in declaration order.
- First matching guarded branch wins.
- If no guard matches, configured block outcome is applied.
- Transition transforms execute only on accepted fire-path transitions.

### Explicit Declarations (Current)

The DSL supports explicit contracts while keeping declaration style consistent:

- `state <Name>` remains one declaration per line.
- `event <Name>` remains one declaration per line, with optional indented `args` body.
- `data` block declares persisted instance-data fields.

Form:

```text
machine <MachineName>

state <StateName>
state <StateName>

event <EventName>
event <EventName>
  args
    <ArgName>: <ScalarType>[?]

data
  <FieldName>: <ScalarType>[?]
```

Scalar-only type set (flat model):

- `string`
- `number`
- `boolean`
- `null`

Validation constraints:

- Event args and data fields are scalar-only; nested object/array values are invalid.
- Unknown event-arg keys and unknown data keys are rejected.
- `Type?` means nullable; non-nullable values reject `null`.
- Guards/transforms support explicit references (`arg.<Key>`, `data.<Key>`).

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

- Undefined state/event/transition resolves to outcome `Undefined`
- CLI display for `Undefined` distinguishes unknown event (`unknown event`) from no available edge on a known event (`no transition from <State>`).
- Unguarded transitions resolve to outcome `Enabled` and return a target/new state
- Guarded transitions are evaluated at runtime against optional event arguments; if provided, they are used for that call without mutating persisted instance data
- `from ... on ...` blocks support ordered `if`/`else if`/`else` branches and end with an outcome statement: `transition <State>`, `reject "<message>"`, or `no transition`.
- Statements are not allowed after an outcome statement in a block.
- Transition data assignments are evaluated/applied only during `Enabled` `Fire(...)` calls
- If one guarded transition evaluates `true`, inspection/fire is `Enabled` and returns target/new state
- If all guarded transitions evaluate `false`, terminal `reject` returns `Blocked` with the configured reason; terminal `no transition` returns `Undefined`
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
- REPL supports `symbols test` to print an ASCII/Unicode compatibility matrix for terminal/font diagnostics.
- REPL supports `clear` to clear the terminal screen.
- REPL supports `style preview` (current theme) and `style preview all` (all themes) using compact timeline styling.
- Style preview samples use a realistic traffic-light transcript (`Red`, `Advance`, `Green`, etc.) and now cover the full compact scenario matrix: inspect-all and inspect-single rows, multi-target reachable/unreachable child arrows, blocked guard-linked child previews, fire success, undefined unknown-event/no-transition rows, argument prompts, and truncation.
- REPL supports `style theme <name|list>` to switch among built-in palettes during a session.
- Built-in palettes now include `mono-accent`, `muted`, `nord-crisp`, `tokyo-night`, `github-dark`, `solarized-modern`, `dracula`, `rose-pine`, `everforest`, `catppuccin-mocha`, `one-dark-pro`, `gruvbox-dark`, `material-ocean`, `night-owl`, `palenight`, `cobalt2`, `ayu-mirage`, `horizon-dark`, `kanagawa-wave`, `synthwave-84`, `monokai-pro`, `sepia-soft`, `forest-night`, `iceberg`, `carbon`, `neon-mint`, `ember`, `lavender-mist`, `slate-blue`, and `slate-blue-vivid`.
- REPL default startup theme is `slate-blue-vivid`.
- REPL `inspect` supports optional event name; without one it inspects all events and reports callable plus guarded transitions for the current state.
- Inspect preview is eager: if available instance data (plus supplied event args, if any) is sufficient to resolve a concrete branch, preview shows that concrete target.
- When multiple targets are defined for an event, inspect shows the event on the parent line and renders all possible targets on child lines; the currently reachable target uses preview arrow (`──▷`) and alternates use unreachable marker (`──✕`, ASCII: `--X`).
- Missing required args only force ambiguous preview when guard logic references the missing arg(s).
- Ambiguous inspect rendering uses timeline rows: the event prints once and possible targets are rendered underneath as child lines using `├─`/`└─` with hollow preview arrows (`──▷` / `-->`).
- If an ambiguous inspect result has a terminal `reject`, warning/reason remains on the event line.
- REPL `fire` prompts for each required event arg key directly if no inline JSON args are supplied and the event is defined from the current state.
- REPL `data` renders readable key-value output in interactive mode unless output mode is json.
- Interactive REPL uses `compact` only.
- Interactive REPL exits cleanly on stdin EOF (for example, when piped input is exhausted).
- Script/non-interactive output is log-oriented (`INFO`/`WARN`/`ERROR`) for command traceability.
- REPL compact output in interactive mode omits repeated command/event labels to reduce noise.
- REPL compact mode uses a timeline-style prompt (`Red ›`) with branch prefixes (`├─`/`└─`) and semantic transition arrows: preview (`inspect`) uses `──▷` (ASCII fallback: `-->`), and committed success (`fire`) uses `──▶` (ASCII fallback: `==>`).
- Compact color semantics are role-based: event labels use event color; state labels use state color; status markers use success/warn/error colors; reachable preview arrows (`──▷`/`-->`) are success-colored; committed fire arrows are success-colored; unreachable child arrows (`──✕`/`--X`) are error-colored while target state labels remain state-colored.
- Structural timeline glyphs/indentation (`├─`, `└─`, `│`) are rendered in neutral/meta color rather than event/state colors.
- For blocked guard outcomes that still show candidate child targets, child preview arrows (`──▷`/`-->`) use warning color to indicate conditionally reachable paths gated by guard data/args.
- Single-event `inspect <EventName>` preview output is visually differentiated from inspect-all rows to make direct command results stand out.
- Interactive compact `inspect`/`fire` result lines include the event name before the status marker (for example, `NotAnEvent ✖ | unknown event`, `Advance ⚠ | No cars waiting`, `Advance ✔ ──▶ Green`, `ClearEmergency ✖ | no transition from Red`).
- Interactive argument prompts in compact mode follow the same style (for example, `│  Reason: value`).
- Interactive commands that display events/states use the same timeline rendering, including `events`, `state`, and inspect callable lines.
- Compact inspect callable lists use natural spacing (no fixed event-column padding) to avoid wide gaps with long event signatures.
- Compact interactive inspect/fire outcome rows are single-line; long status text is truncated with `...` to preserve timeline arrow alignment.
- Compact `events` output also aligns event-name columns to match inspect-list rhythm.
- Compact interactive non-prompt lines (including argument prompts) are rendered as timeline children beneath each prompt.
- Interactive inspect callable output lists only event/state lines (no separate "callable events" banner).
- REPL verbose mode renders structured table/panel views for inspect/fire details and callable-event listings.
- Symbol rendering supports `auto|ascii|unicode`; auto mode prefers Unicode only if runtime terminal heuristics indicate support.
- Transition DSL supports `transform <Key> = <expr>` where `<Key>` may be bare or `data.<Key>`.
- Assignment expressions support literals, bare event-argument keys, and scoped references (`arg.<Key>`, `data.<Key>`).
- Event declarations support optional indented `args` blocks with scalar type contracts.
- `data` block declarations define persisted instance-data scalar contracts.
- Inline typed event arguments (`event Name(Type)`) are rejected; use event `args` blocks instead.
- CLI emits non-zero exit codes for incompatible instances and script command failures.
- Runtime supports persisted instance creation and instance-based `Inspect(...)` / `Fire(...)`.
- CLI supports `--instance` at startup and REPL-level `load`/`save` for instance file management.
- Repository root includes runnable examples: `trafficlight.sm`, `traffic.instance.json`, `traffic.script.txt`.
- CLI includes interactive REPL and non-interactive script execution using the same command set.
- Test suite includes CLI transcript rendering regression coverage for compact inspect/fire timeline scenarios (Unicode and ASCII symbol modes).

Supported default guard forms:

- `IsEnabled`
- `!IsEnabled`
- `CarsWaiting > 0`
- `CarsWaiting >= 3`
- `Mode == "Manual"`

Unsupported/invalid guards are treated as failed with descriptive reasons.
