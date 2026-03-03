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

Canonical constraints:

- `()+` means one-or-more lines in a `from ... on ...` body.
- `if` and `else if` must end with `transition <State>` or `no transition`.
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
  - `transition <State>` or `no transition` (required for `if`/`else if` branch bodies)
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
  - require exactly one outcome: `transition <State>` or `no transition`
  - do not allow `reject`
- `else` branch body:
  - may include optional `transform <Key> = <Expr>`
  - must end in exactly one outcome statement: `transition <State>`, `reject "<message>"`, or `no transition`

#### Evaluation model

- Branches are evaluated in declaration order.
- First matching guarded branch wins.
- If no guard matches, configured block outcome is applied.
- Transition transforms execute only on accepted fire-path transitions.

### Transform/Expression Design Decisions (Locked)

Status:

- The following choices are design-locked for the next transform/expression expansion.
- Phase 1 parser/model foundation is implemented: transform expressions parse into a DSL expression AST and transitions retain ordered transform-assignment lists.
- Phase 2 shared evaluator integration is implemented: guards and transform expressions evaluate through the shared AST-based expression evaluator.
- Phase 3 runtime execution is implemented: transform assignments evaluate in declaration order on a working copy (read-your-writes) and commit atomically.

Locked choices:

- Execution model is atomic batch per selected branch: all transform assignments in that branch must succeed or none are committed.
- Branches support multiple `transform` statements; each `transform` remains single-assignment (`transform <Key> = <Expr>`).
- In-branch evaluation is read-your-writes using a working copy in declaration order.
- Assignment typing is strict fail-fast with no implicit coercion.
- Guard and transform expressions use one shared expression semantics model; only expected result type differs (guard => `boolean`, transform => assigned field contract type).

Expression scope (B-v1):

- Operands: literals, data fields, event args (`<EventName>.<ArgName>`), and parentheses.
- Operators: `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!`.
- String concatenation is strong concat only: `+` is valid for strings only when both operands are strings.

Null handling (B-v1):

- `==`/`!=` support explicit null checks.
- `<`, `<=`, `>`, `>=` with null are invalid.
- Arithmetic (`+`, `-`, `*`, `/`, `%`) with null is invalid.
- Boolean operators with null operands are invalid.
- String concat with null operand is invalid.
- Any invalid expression evaluation fails the fire path for that branch and, due to atomic batch semantics, commits no transform assignments.

Examples:

```text
# valid (guard + arithmetic + modulo + concat)
if RetryCount != null && RetryCount % 2 == 0
  transform RetryCount = RetryCount + 1
  transform AuditMessage = "Retry #" + RetryCount
  transition Retrying

# invalid (strict typing / null handling)
transform RetryCount = "1"           # string -> number (type mismatch)
transform AuditMessage = "Reason: " + Emergency.Reason   # invalid when Emergency.Reason is null
```

Implementation checklist (B-v1):

- Parser/model
  - Replace single assignment storage (`DataAssignmentKey`/`DataAssignmentExpression`) with ordered transform-assignment list per transition outcome.
  - Parse multiple `transform` lines per branch and preserve declaration order.
  - Extend expression grammar to support `()`, `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!`.
- Runtime
  - Implement one shared expression evaluator used by both guards and transforms.
  - Implement strict type matrix and null rules from this document (no implicit coercion).
  - Implement strong string concat (`+`) only for string+string.
  - Implement short-circuit evaluation for `&&` and `||`.
  - Evaluate transform assignments in branch order on a working copy (read-your-writes), then commit atomically. ✅
- Inspect/fire behavior
  - Ensure inspect uses the same guard semantics as fire.
  - Ensure transform evaluation errors reject fire and commit no transform assignments.
- LSP/editor tooling
  - Update diagnostics for operator/type/null violations. ✅
  - Update completion/snippets to include new operator-aware expression authoring patterns. ✅
- Tests
  - Add parser tests for operator precedence/associativity and multi-transform parsing.
  - Add runtime tests for atomic batch rollback, read-your-writes, strict typing, strong concat, null-handling failures, and short-circuit behavior.
  - Add inspect/fire parity tests for shared guard semantics.
- Documentation
  - Update README examples to include at least one valid modulo/concat expression and one null-safe guard pattern.

### Explicit Declarations (Current)

The DSL supports explicit contracts while keeping declaration style consistent:

- `state <Name>` remains one declaration per line.
- `event <Name>` remains one declaration per line, with optional indented argument declarations.
- Typed field declarations at top-level define persisted instance-data fields.

Form:

```text
machine <MachineName>

state <StateName>
state <StateName>

event <EventName>
event <EventName>
  <ScalarType>[?] <ArgName>

<ScalarType>[?] <FieldName>
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
- Guards/transforms support explicit scoped references (`<EventName>.<ArgKey>`, `<Key>`).

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

- Language-server expression diagnostics are currently best-effort static checks and do not perform full guard-flow null narrowing.
- Extension packaging/publishing workflow

## Test Status

- Active tests include `test/StateMachine.Tests/DslWorkflowTests.cs`, `test/StateMachine.Tests/DslExpressionParserTests.cs`, `test/StateMachine.Tests/DslExpressionParserEdgeCaseTests.cs`, `test/StateMachine.Tests/DslTransformParsingTests.cs`, and `test/StateMachine.Tests/DslExpressionRuntimeEvaluatorBehaviorTests.cs`.
- Guard/expression test coverage includes: boolean guards, comparisons, string/null equality, numeric runtime type coercion, unsupported-expression rejection, reason aggregation, expression AST parsing precedence/invalid syntax diagnostics, lexer edge cases, transform-branch parsing constraints, and runtime evaluator operator/short-circuit behavior.

## Next Steps

1. Add packaging/publishing automation for the VS Code extension client.
2. Improve language-server expression analysis with deeper flow-aware null/type narrowing to reduce false-positive/false-negative diagnostics.

## Guard Evaluation + Event-Argument Model (Current)

- Runtime uses `IGuardEvaluator` with a default implementation (`DefaultGuardEvaluator`).
- `DslWorkflowCompiler.Compile(...)` accepts an optional custom evaluator.
- `Inspect(...)` and `Fire(...)` accept optional event arguments (`IReadOnlyDictionary<string, object?>`).
- Interactive REPL `inspect`/`fire` do not accept inline JSON event-argument payloads.
- REPL supports `symbols test` to print an ASCII/Unicode compatibility matrix for terminal/font diagnostics.
- REPL supports `clear` to clear the terminal screen.
- REPL supports interactive `Tab` completion for top-level commands, event names (`inspect`/`fire`), `style theme` names, and `symbols` subcommands.
- REPL supports `Up`/`Down` command history navigation and `Right Arrow` inline completion acceptance in interactive mode.
- REPL supports inline type-ahead hints while typing (current-token completion preview).
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
- REPL `fire` prompts for each required event arg with type-aware labels when the event is defined from the current state.
- Interactive fire prompting performs scalar type coercion/validation against arg contracts and re-prompts on invalid input.
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
- Transition DSL supports `transform <Key> = <expr>` where `<Key>` may be bare or `<Key>`.
- Assignment expressions support literals and scoped references (`<EventName>.<ArgKey>`, `<Key>`).
- Event declarations support optional indented argument declarations with scalar type contracts.
- Top-level typed declarations define persisted instance-data scalar contracts.
- Inline typed event arguments (`event Name(Type)`) are rejected; use indented event argument declarations instead.
- CLI emits non-zero exit codes for incompatible instances and script command failures.
- Runtime supports persisted instance creation and instance-based `Inspect(...)` / `Fire(...)`.
- `tools/StateMachine.Dsl.LanguageServer` provides LSP stdio diagnostics/completion MVP for `.sm` files.
- LSP diagnostics run parser/compiler validation on document open/change/save and map parser `Line N:` failures to line-scoped diagnostics.
- LSP completion includes DSL keywords plus contextual state/event suggestions (`from`, `transition`, `on`).
- LSP completion includes contextual guard suggestions for `if`/`else if` (data fields, operators/literals, and current-event argument references).
- LSP completion includes contextual transform suggestions (data-field target names before `=`, expression suggestions after `=`).
- LSP completion includes event-argument member suggestions after `<EventName>.` in guard/transform expressions.
- LSP completion includes snippet-style templates for common branch/outcome patterns (`from ... on ...`, `if/else if/else`, `transition`, `reject`, `no transition`, and `transform`).
- LSP semantic tokens provide role-aware highlighting for keywords, state/event symbols, variable identifiers, strings, numbers, operators, and comments.
- `tools/StateMachine.Dsl.VsCode` provides a VS Code client MVP that auto-starts the language server for `.sm` files.
- VS Code client startup resolves the language-server project relative to extension location and does not require a workspace folder in Extension Development Host.
- VS Code client startup for locally installed VSIX resolves the language-server project from current workspace folder paths (repo root or `tools/StateMachine.Dsl.VsCode`) with extension-path fallback for Extension Development Host.
- VS Code client contributes TextMate grammar-based syntax highlighting for `.sm` files.
- VS Code client supports local-only VSIX packaging via `npm run package:local` in `tools/StateMachine.Dsl.VsCode`.
- Local VSIX packaging includes language-client runtime dependencies so activation works after install.
- VS Code client supports a local package+install loop via `npm run loop:local` in `tools/StateMachine.Dsl.VsCode`.
- VS Code client activates from `workspaceContains:**/*.sm` plus language contribution activation and writes startup diagnostics to the `StateMachine DSL` output channel.
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

