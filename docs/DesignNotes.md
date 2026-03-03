# StateMachine Design Notes

Date: 2026-02-24

This document tracks current design decisions for the active implementation on this branch.

## Current Scope

Implementation focus is the DSL runtime path:

- parse `.sm` definitions
- validate semantic correctness
- execute inspection/fire against a compiled in-memory workflow definition

## Editor-First Preview Integration (Current)

- The VS Code extension now includes an inspector preview command (`StateMachine DSL: Open Inspector Preview`).
- Preview is implemented as dedicated webview panels keyed per `.sm` file URI (one panel/session per file).
- This chooses the editor-first option where each file can maintain independent preview context without active-editor multiplexing.
- Preview UI is loaded from `tools/StateMachine.Dsl.VsCode/webview/inspector-preview.html` and uses the mock-style visual shell with a live runtime bridge.
- The reference mock file remains at `tools/StateMachine.Dsl.VsCode/mockups/interactive-inspector-mockup.html`; runtime panel behavior is driven by the webview copy.
- Runtime diagram layout uses a single unified ELK layered layout computed in the extension host and attached as `snapshot.layout` (`nodes` with per-node `width`/`height`, `edges` with ELK-routed bend-point arrays, `width`, `height`).
- ELK options are state-machine-tuned: top-down direction (`elk.direction: DOWN`), spline edge routing (`elk.edgeRouting: SPLINES`), greedy cycle breaking, feedback edges for cycle handling, inside self-loops, inline edge labels, network-simplex node placement, and DSL declaration-order node/port ordering.
- Reject and no-transition terminal rules are excluded from the ELK layout graph and webview diagram edges; only real state-change transitions produce edges. Terminal rules remain in the transitions array for event discovery and evaluation logic.
- Node dimensions are computed dynamically from state name length (8.5px char width, 36px horizontal padding, 80px minimum width, 40px fixed height) and stored in each `LayoutNode`.
- No post-processing passes are applied (no stabilization, deconfliction, ingress bands, or normalization); ELK handles crossing minimization, self-loops, backward edges, and parallel edges directly.
- Runtime webview consumes ELK geometry (node positions/sizes and edge bend-point arrays) and converts edge points to smooth Catmull-Rom → cubic Bézier spline paths.
- Webview viewBox is set responsively from ELK-computed graph dimensions with 50px padding per side (minimum 600×300); no content-bounds normalization or clamping is applied.
- The preview webview now calls a custom LSP endpoint (`stateMachine/preview/request`) for `snapshot`, `fire`, `reset`, `replay`, and `inspect` actions. The `inspect` action re-evaluates a single event with caller-supplied arguments so the webview gets real-time guard status without local duplication of guard logic.
- The preview endpoint is bound through a typed JSON-RPC request handler (`IJsonRpcRequestHandler<SmPreviewRequest, SmPreviewResponse>`) with the method contract declared on `SmPreviewRequest` via `[Method("stateMachine/preview/request")]` so registration is discoverable at runtime.
- Language server preview sessions are in-memory and keyed by document URI; each session keeps parsed/compiled definition and current instance state.
- The extension pushes updated snapshots to an open file panel on document change, keeping preview content aligned with current editor text.
- Extension refreshes preview snapshots on document change, save, and panel re-focus/reveal for recovery from stale panel state.
- Document-change refresh uses unsaved in-memory text from the open editor buffer; save is not required for preview updates.
- Snapshot messages are sequence-ordered in the webview so stale async responses cannot overwrite newer editor changes.
- Webview snapshot parsing accepts both camelCase and PascalCase payload keys for state/event/transition/data fields.
- In preview mode, `Reload` performs a fresh `snapshot` request (with a short retry) from the current in-memory editor text instead of requiring persisted file state.
- On webview startup, preview triggers the same reload path after `ready` to recover if the first host-pushed snapshot is missed.
- Preview includes a replay control that runs a predefined scenario sequence through the language-server replay action.
- Replay responses include `replayMessages`; the preview renders them as a compact transcript in the event dock.
- Snapshot request failures are surfaced in the same transcript area.
- Extension packaging must include `webview/inspector-preview.html` so installed VSIX preview panels can render.
- Local extension development supports a fast watch loop (`npm run dev:watch` / task `extension: watch`) plus launch profile `Extension (StateMachine DSL) Fast Dev` to avoid VSIX repackaging on each edit.

### Preview Fire-Animation Ordering (Deferred)

The current fire flow is fire-then-animate: the client sends the `fire` request to the server immediately on button click, receives the response (with updated snapshot), then plays the dot animation as a visual flourish and applies the snapshot in `onComplete`.

An alternative flow is animate-then-fire: the animation plays first using the target state already known from `inspect`/`snapshot` status, and only when the dot reaches the target does the client send `fire` to the server and apply the resulting snapshot.

**Current (fire-then-animate):**
- Pros: error feedback is immediate (no wasted animation on a failed fire); snapshot is already available when animation ends so commit is synchronous in `onComplete`.
- Cons: the animation is decorative — the state has already committed server-side before the user sees the dot move.

**Alternative (animate-then-fire):**
- Pros: better matches an exploration-oriented preview — the user watches the transition unfold and then the commit follows, making the animation feel like the event is happening rather than merely replaying a done deal.
- Cons: if the server rejects after the dot arrives (edge case), the UI must handle failure post-animation (show error, keep old state); `onComplete` becomes async; animation target must come from `currentEventStatuses[event].targetState` rather than from the fire response.

Both flows are structurally simple — the difference is ~10-15 lines rearranged in `fireCurrentSelection`. The rest of the webview is unaffected.

Decision: keeping fire-then-animate for now; revisit when preview UX stabilises.

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
state <StateName> [initial]
<StateDecl> := initial

event <EventName>
[ <string|number|boolean|null>[?] <ArgName> { <ArgDecl> } ]

<string|number|boolean|null>[?] <FieldName> { <FieldDecl> }
<string|number|boolean|null>[?] <FieldName> [= <Literal>]

from <any|StateA[,StateB...]> on <EventName>
(
    if <Guard> [ set <Field> = <Expr> ] ( transition <ToState> | no transition )
  | else if <Guard> [ set <Field> = <Expr> ] ( transition <ToState> | no transition )
  | else [ set <Field> = <Expr> ] ( transition <ToState> | reject <Reason> | no transition )
  | [ set <Field> = <Expr> ] ( transition <ToState> | reject <Reason> | no transition )
)+

<Expr> := <Literal|<FieldName>|<EventName>.<ArgName>>
<Literal> := <null|true|false|number|string>
```

Canonical constraints:

- `()+` means one-or-more lines in a `from ... on ...` body.
- Exactly one `state` declaration must include `initial`.
- `event` declarations are optional. A machine with no events is syntactically valid. The language server emits a `Hint` diagnostic on the `machine` line when no events are declared, as such a machine cannot respond to any input.
- `if` and `else if` must end with `transition <State>` or `no transition`.
- `else` may end with `transition`, `reject`, or `no transition`.
- After an `if`/`else if` chain, a fallback **must** use `else`; a bare block-level outcome after a chain is a parse error.
- `set` is allowed with `no transition` in all branch contexts; assignments execute on fire but state does not change.
- `no transition` without any `set` is permitted (including in unguarded block-level position), even though it is a pure no-op with no observable effect on state or data. The rationale is that `no transition` carries semantic intent — the event is explicitly *acknowledged* (`IsDefined=true`, `IsAccepted=true`) rather than undefined or rejected — and the guard-branch case (`if Hold / no transition`) is a meaningful "accept but stay" pattern that does not require data changes. **Future review candidate:** Consider warning (via the language server analyzer) when an unguarded (block-level) `no transition` has no `set` assignments, since that specific form is always a no-op and likely unintentional.
- `reason "..."` is valid only on `reject`.
- Top-level data fields may declare literal defaults using `<Field> = <Literal>`.
- Defaults are applied when creating instances and can be overridden by caller-supplied instance data.
- Non-nullable top-level data fields must declare defaults.
- Unsupported syntax: `states ...`, `events ...`, and legacy inline form `transition A -> B on E ...`.

Block-authoring equivalent (same semantics):

- Block header: `from <State|State,State|any> on <Event>`
- Guarded branch header: `if <Guard>`
- Additional guarded branch headers: `else if <Guard>`
- Optional fallback header: `else`
- Allowed branch body statements:
  - `set <Key> = <Expr>` (optional)
  - `transition <State>` or `no transition` (required for `if`/`else if` branch bodies)
- Allowed block outcome statements:
  - `transition <State>`
  - `reject "<message>"`
  - `no transition`
- Validation rules:
  - Every `from ... on ...` block must end with an outcome statement.
  - No statements are allowed after an outcome statement.
  - `else if` and `else` require a preceding `if` in the same block.
  - After an `if`/`else if` chain, any block-level outcome statement requires `else`; bare fallbacks without `else` are a parse error.
  - `reason "<message>"` is valid **only** on `reject` statements; writing it on `if` or `else if` is a parse error.
  - Inline guarded transitions (`transition A -> B on E` with trailing guard clauses) are invalid.

#### Branch/body constraints

- `if` and `else if` branch bodies:
  - allow optional `set <Key> = <Expr>`
  - require exactly one outcome: `transition <State>` or `no transition`
  - do not allow `reject`
- `else` branch body:
  - may include optional `set <Key> = <Expr>`
  - must end in exactly one outcome statement: `transition <State>`, `reject "<message>"`, or `no transition`

#### Evaluation model

- Branches are evaluated in declaration order.
- First matching guarded branch wins.
- If no guard matches, configured block outcome is applied.
- Transition sets execute only on accepted fire-path transitions.

### Set/Expression Design Decisions (Locked)

Status:

- The following choices are design-locked for the next set/expression expansion.
- Phase 1 parser/model foundation is implemented: set expressions parse into a DSL expression AST and transitions retain ordered set-assignment lists.
- Phase 2 shared evaluator integration is implemented: guards and set expressions evaluate through the shared AST-based expression evaluator.
- Phase 3 runtime execution is implemented: set assignments evaluate in declaration order on a working copy (read-your-writes) and commit atomically.

Locked choices:

- Execution model is atomic batch per selected branch: all set assignments in that branch must succeed or none are committed.
- Branches support multiple `set` statements; each `set` remains single-assignment (`set <Key> = <Expr>`).
- In-branch evaluation is read-your-writes using a working copy in declaration order.
- Assignment typing is strict fail-fast with no implicit coercion.
- Guard and set expressions use one shared expression semantics model; only expected result type differs (guard => `boolean`, set => assigned field contract type).

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
- Any invalid expression evaluation fails the fire path for that branch and, due to atomic batch semantics, commits no set assignments.

Examples:

```text
# valid (guard + arithmetic + modulo + concat)
if RetryCount != null && RetryCount % 2 == 0
  set RetryCount = RetryCount + 1
  set AuditMessage = "Retry #" + RetryCount
  transition Retrying

# invalid (strict typing / null handling)
set RetryCount = "1"           # string -> number (type mismatch)
set AuditMessage = "Reason: " + Emergency.Reason   # invalid when Emergency.Reason is null
```

Implementation checklist (B-v1):

- Parser/model
  - Replace single assignment storage (`DataAssignmentKey`/`DataAssignmentExpression`) with ordered set-assignment list per transition outcome.
  - Parse multiple `set` lines per branch and preserve declaration order.
  - Extend expression grammar to support `()`, `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!`.
- Runtime
  - Implement one shared expression evaluator used by both guards and sets.
  - Implement strict type matrix and null rules from this document (no implicit coercion).
  - Implement strong string concat (`+`) only for string+string.
  - Implement short-circuit evaluation for `&&` and `||`.
  - Evaluate set assignments in branch order on a working copy (read-your-writes), then commit atomically. ✅
- Inspect/fire behavior
  - Ensure inspect uses the same guard semantics as fire.
  - Ensure set evaluation errors reject fire and commit no set assignments.
- LSP/editor tooling
  - Update diagnostics for operator/type/null violations. ✅
  - Update completion/snippets to include new operator-aware expression authoring patterns. ✅
- Tests
  - Add parser tests for operator precedence/associativity and multi-set parsing.
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
- Guards/sets support explicit scoped references (`<EventName>.<ArgKey>`, `<Key>`).

## Implemented Components

- `StateMachine.Dsl.DslMachine` model
- `StateMachine.Dsl.StateMachineDslParser`
- `StateMachine.Dsl.DslWorkflowCompiler`
- `StateMachine.Dsl.DslWorkflowDefinition`
- `StateMachine.Dsl.DslWorkflowInstance` persisted instance model
- Instance result/compatibility types: `DslInstanceCompatibilityResult`, `DslInstanceFireResult`
- `tools/StateMachine.Dsl.LanguageServer` (LSP diagnostics/completion/semantic tokens + preview request handler)
- `tools/StateMachine.Dsl.VsCode` (language client + inspector preview webview)

## Current Runtime Semantics

- Undefined state/event/transition resolves to outcome `Undefined`
- Unguarded transitions resolve to outcome `Enabled` and return a target/new state
- Guarded transitions are evaluated at runtime against optional event arguments; if provided, they are used for that call without mutating persisted instance data
- `from ... on ...` blocks support ordered `if`/`else if`/`else` branches and end with an outcome statement: `transition <State>`, `reject "<message>"`, or `no transition`.
- Statements are not allowed after an outcome statement in a block.
- Transition data assignments are evaluated/applied only during `Enabled` or `NoTransition` `Fire(...)` calls
- If one guarded transition evaluates `true`, inspection/fire is `Enabled` and returns target/new state
- If all guarded transitions evaluate `false`, terminal `reject` returns `Blocked` with the configured reason; terminal `no transition` returns `NoTransition` (`IsDefined = true`, state unchanged, `set` assignments execute on fire)
- Instance-based inspect/fire validates workflow name compatibility before evaluating transitions

## Concurrency Model (Current)

- `DslWorkflowDefinition` is immutable after compile.
- Runtime does not maintain hidden mutable process state; state progresses through returned `DslWorkflowInstance` values.
- Any coordination for concurrently reading/writing persisted instance files is outside runtime scope and must be handled by the caller.

## Known Gaps

- Language-server expression diagnostics now perform null-flow narrowing for explicit null checks in `&&`/`||` paths, but still do not perform full cross-branch flow analysis.
- Extension packaging/publishing workflow

## Test Status

- Active tests include `test/StateMachine.Tests/DslWorkflowTests.cs`, `test/StateMachine.Tests/DslExpressionParserTests.cs`, `test/StateMachine.Tests/DslExpressionParserEdgeCaseTests.cs`, `test/StateMachine.Tests/DslSetParsingTests.cs`, `test/StateMachine.Tests/DslExpressionRuntimeEvaluatorBehaviorTests.cs`, and `test/StateMachine.Dsl.LanguageServer.Tests/SmDslAnalyzerNullNarrowingTests.cs`.
- Guard/expression test coverage includes: boolean guards, comparisons, string/null equality, numeric runtime type coercion, unsupported-expression rejection, reason aggregation, expression AST parsing precedence/invalid syntax diagnostics, lexer edge cases, set-branch parsing constraints, and runtime evaluator operator/short-circuit behavior.

## Next Steps

1. Add packaging/publishing automation for the VS Code extension client.
2. Improve language-server expression analysis with deeper cross-branch null/type narrowing to reduce false-positive/false-negative diagnostics.

## Guard Evaluation + Event-Argument Model (Current)

- Runtime uses `IGuardEvaluator` with a default implementation (`DefaultGuardEvaluator`).
- `DslWorkflowCompiler.Compile(...)` accepts an optional custom evaluator.
- `Inspect(...)` and `Fire(...)` accept optional event arguments (`IReadOnlyDictionary<string, object?>`).
- Inspect preview is eager: if available instance data (plus supplied event args, if any) is sufficient to resolve a concrete branch, preview shows that concrete target.
- When multiple targets are defined for an event, inspect shows the event on the parent line and renders all possible targets on child lines; the currently reachable target uses preview arrow (`──▷`) and alternates use unreachable marker (`──✕`, ASCII: `--X`).
- Missing required args only force ambiguous preview when guard logic references the missing arg(s).
- Ambiguous inspect rendering uses timeline rows: the event prints once and possible targets are rendered underneath as child lines using `├─`/`└─` with hollow preview arrows (`──▷` / `-->`).
- If an ambiguous inspect result has a terminal `reject`, warning/reason remains on the event line.
- Transition DSL supports `set <Key> = <expr>` where `<Key>` may be bare or `<Key>`.
- Assignment expressions support literals and scoped references (`<EventName>.<ArgKey>`, `<Key>`).
- Event declarations support optional indented argument declarations with scalar type contracts.
- Top-level typed declarations define persisted instance-data scalar contracts.
- Inline typed event arguments (`event Name(Type)`) are rejected; use indented event argument declarations instead.
- Runtime supports persisted instance creation and instance-based `Inspect(...)` / `Fire(...)`.
- `tools/StateMachine.Dsl.LanguageServer` provides LSP stdio diagnostics/completion MVP for `.sm` files.
- LSP diagnostics run parser/compiler validation on document open/change/save and map parser `Line N:` failures to line-scoped diagnostics.
- LSP completion includes DSL keywords plus contextual state/event suggestions (`from`, `transition`, `on`).
- LSP completion includes contextual guard suggestions for `if`/`else if` (data fields, operators/literals, and current-event argument references).
- LSP completion includes contextual set suggestions (data-field target names before `=`, expression suggestions after `=`).
- LSP completion includes event-argument member suggestions after `<EventName>.` in guard/set expressions.
- LSP completion includes snippet-style templates for common branch/outcome patterns (`from ... on ...`, `if/else if/else`, `transition`, `reject`, `no transition`, and `set`).
- LSP semantic tokens provide role-aware highlighting for keywords, state/event symbols, variable identifiers, strings, numbers, operators, and comments.
- `tools/StateMachine.Dsl.VsCode` provides a VS Code client MVP that auto-starts the language server for `.sm` files.
- VS Code client startup resolves the language-server project relative to extension location and does not require a workspace folder in Extension Development Host.
- VS Code client startup for locally installed VSIX resolves the language-server project from current workspace folder paths (repo root or `tools/StateMachine.Dsl.VsCode`) with extension-path fallback for Extension Development Host.
- VS Code client contributes TextMate grammar-based syntax highlighting for `.sm` files.
- VS Code client supports local-only VSIX packaging via `npm run package:local` in `tools/StateMachine.Dsl.VsCode`.
- Local VSIX packaging includes language-client runtime dependencies so activation works after install.
- VS Code client supports a local package+install loop via `npm run loop:local` in `tools/StateMachine.Dsl.VsCode`.
- VS Code client activates from `workspaceContains:**/*.sm` plus language contribution activation and writes startup diagnostics to the `StateMachine DSL` output channel.
- Repository root includes runnable DSL examples such as `trafficlight.sm`.

Supported default guard forms:

- `IsEnabled`
- `!IsEnabled`
- `CarsWaiting > 0`
- `CarsWaiting >= 3`
- `Mode == "Manual"`

Unsupported/invalid guards are treated as failed with descriptive reasons.

