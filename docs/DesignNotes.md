# StateMachine Design Notes

Date: 2026-02-24

This document tracks current design decisions for the active implementation on this branch.

## Current Scope

Implementation focus is the DSL runtime path:

- parse `.sm` definitions
- validate semantic correctness
- execute inspection/fire against a compiled in-memory workflow definition

## Model Refactor Design (Locked)

Status: **Implemented.**

This section records all locked design decisions for the combined model restructure, `when` precondition feature, outcome renaming, and diagnostic precision improvements. Nothing here should be changed without a deliberate design decision.

---

### Model Hierarchy

The new canonical model hierarchy, in DSL-declaration order:

```
DslWorkflowModel
├── DslField              (<type> <Name> [= <default>])
│   └── DslRule           (rule <Expr> "<Reason>")
├── DslCollectionField    (set<T>|queue<T>|stack<T> <Name>)
│   └── DslRule           (rule <Expr> "<Reason>")
├── DslRule               (top-level rule <Expr> "<Reason>")
├── DslState              (state <Name> [initial])
│   └── DslRule           (rule <Expr> "<Reason>")
├── DslEvent              (event <Name>)
│   ├── DslEventArg       (<type> <Name> [= <default>])
│   └── DslRule           (rule <Expr> "<Reason>")
└── DslTransition         (from <State> on <Event> [when <Expr>])
    └── DslClause         (if|else if|else)
        ├── DslClauseOutcome
        │   ├── DslStateTransition   (transition <State>)
        │   ├── DslRejection         (reject "<Reason>")
        │   └── DslNoTransition      (no transition)
        ├── DslSetAssignment         (set <Key> = <Expr>)
        └── DslCollectionMutation    (add|remove|enqueue|...)
```

---

### Type-by-Type Decisions

#### `DslWorkflowModel` (was `DslMachine`)
- `States : IReadOnlyList<DslState>` — replaces `IReadOnlyList<string>`
- `InitialState : DslState` — typed reference into `States`, not a string; derivable but kept as a convenience property
- `Transitions : IReadOnlyList<DslTransition>` — single list; `TerminalRules` list removed
- `DataFields` renamed to `Fields : IReadOnlyList<DslField>`
- `CollectionFields : IReadOnlyList<DslCollectionField>`
- `StateRules : IReadOnlyDictionary<string, IReadOnlyList<DslRule>>` removed — rules now live on `DslState.Rules`
- `TopLevelRules : IReadOnlyList<DslRule>?` — unchanged

#### `DslState` (new)
- `Name : string`
- `Rules : IReadOnlyList<DslRule>?`
- No `IsInitial` bool — the authoritative source is `DslWorkflowModel.InitialState` (reference equality check)

#### `DslField` (renamed from `DslFieldContract`)
- Carries `Rules : IReadOnlyList<DslRule>?`
- Used only for `DslWorkflowModel.Fields` (persistent instance data)

#### `DslCollectionField` (renamed from `DslCollectionFieldContract`)
- Carries `Rules : IReadOnlyList<DslRule>?`

#### `DslEventArg` (split from `DslFieldContract`)
- Used only for `DslEvent.Args`
- No `Rules` property — event rules are on `DslEvent`, not individual args
- Properties: `Name`, `Type`, `IsNullable`, `HasDefaultValue`, `DefaultValue`

#### `DslTransition` (was: `from…on` block — reclaimed name)
- Represents the entire `from <State> on <Event> [when <Expr>]` block
- `FromState : string`
- `EventName : string`
- `Predicate : string?` — raw `when` expression text; null if no `when` clause
- `PredicateAst : DslExpression?` — parsed AST; null if no `when` clause
- `Clauses : IReadOnlyList<DslClause>`
- `SourceLine : int`
- One `DslTransition` per `(FromState, EventName)` pair — uniqueness enforced by parser

#### `DslClause`
- Represents one `if` / `else if` / `else` branch
- `Predicate : string?` — raw guard expression text; null for `else`
- `PredicateAst : DslExpression?` — parsed AST; null for `else`
- `Outcome : DslClauseOutcome`
- `SetAssignments : IReadOnlyList<DslSetAssignment>`
- `CollectionMutations : IReadOnlyList<DslCollectionMutation>?`
- `SourceLine : int`
- No `Order` property — list position is the authoritative order

#### `DslClauseOutcome` (abstract base — new)
Three concrete subtypes, all nouns:
- `DslStateTransition(TargetState : string)` — the `transition <State>` outcome
- `DslRejection(Reason : string?)` — the `reject` outcome
- `DslNoTransition` — the `no transition` outcome

#### `DslSetAssignment`
- Added: `ExpressionStartColumn : int`, `ExpressionEndColumn : int`
- `SourceLine` already present

#### `DslCollectionMutation`
- Added: `SourceLine : int`, `ExpressionStartColumn : int`, `ExpressionEndColumn : int`

#### `DslTerminalRule` / `DslTerminalKind` (removed)
Fully replaced by `DslClause` + `DslClauseOutcome`.

---

### `when` Precondition Feature

- Syntax: `from <State> on <Event> when <Expr>`
- `when` guard is stored as `DslTransition.Predicate` / `PredicateAst`
- When the `when` predicate evaluates to `false` at runtime, the entire block is skipped with outcome `NotApplicable`
- `if`/`else if` branches inside the block cannot use `reject` (only `else` can); this constraint is unchanged
- One `from…on` block per `(State, Event)` pair — parser enforces uniqueness with a clear error

---

### Outcome Renaming

`DslOutcomeKind` values, renamed for consistency and clarity:

| Old | New | Meaning |
|---|---|---|
| `Undefined` | `NotDefined` | Event not registered for this state |
| *(new)* | `NotApplicable` | `when` predicate false — block skipped |
| `Blocked` | `Rejected` | Explicit `reject` branch taken, or rule violation |
| `Enabled` | `Accepted` | Transition executed; state changed |
| `NoTransition` | `AcceptedInPlace` | Event accepted but no state change |

`TransitionResolutionKind` (private) already uses `NotDefined` and `Rejected` — public enum now matches.

---

### Result Record Changes

Result records implemented:
- `DslEventInspectionResult` — per-event inspect result (was `DslInspectionResult`)
- `DslInspectionResult` — new aggregate: `CurrentState`, `InstanceData`, `Events : IReadOnlyList<DslEventInspectionResult>`
- `DslFireResult` — fire result with `UpdatedInstance : DslWorkflowInstance?` (replaces old stateless form + `DslInstanceFireResult`)
- `DslCompatibilityResult` — compatibility check result (was `DslInstanceCompatibilityResult`)
- `bool IsDefined` removed from all — derivable as `Outcome != NotDefined`
- `bool IsAccepted` removed from all — derivable as `Outcome is Accepted or AcceptedInPlace`
- Factory method `NoTransition(...)` renamed to `AcceptedInPlace(...)` across the board

---

### Guard Infrastructure Removal

- `IGuardEvaluator` interface removed
- `DefaultGuardEvaluator` class removed
- `GuardEvaluationResult` record removed
- Guard expressions are no longer re-parsed at runtime — `DslClause.PredicateAst` (and `DslTransition.PredicateAst` for `when`) are evaluated directly by `DslExpressionRuntimeEvaluator`
- `DslWorkflowCompiler.Compile` no longer accepts an `IGuardEvaluator?` parameter

---

### Analyzer: `when` Null Narrowing

When processing a `DslTransition` with a non-null `Predicate`:

1. Apply `ApplyNarrowing(predicateAst, baseSymbols, assumeTrue: true)` to produce `blockSymbols`
2. Use `blockSymbols` as the starting `branchSymbols` for the clause loop (not `baseSymbols`)
3. Clause-guard negations accumulate across clauses from `blockSymbols` as before
4. The `false` path of the `when` predicate (the `NotApplicable` path) has no code to validate — no narrowing needed there

---

### Diagnostic Precision (Column-Level)

All diagnostic sites upgraded from line-level to column-precise squiggles:

| Construct | Model fields | Action |
|---|---|---|
| `DslRule` | `ExpressionStartColumn`, `ExpressionEndColumn`, `ReasonStartColumn`, `ReasonEndColumn` already present | Wire into `ValidateExpression` — add `CreateColumnDiagnostic` helper |
| `DslSetAssignment` | `ExpressionStartColumn`, `ExpressionEndColumn` (new) | Parser captures; analyzer uses instead of `FindSetLine` |
| `DslCollectionMutation` | `SourceLine`, `ExpressionStartColumn`, `ExpressionEndColumn` (all new) | Parser captures; `FindMutationLine` eliminated |
| Guard / `when` predicate | Column derivable from `Predicate` text position on `DslClause.SourceLine` / `DslTransition.SourceLine` | Parser captures start column; `FindGuardLine` eliminated |

`CreateLineDiagnostic` kept for parser-level errors where no column data is available.

---

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
- Inspector webview behavior is intentionally "click-now" oriented: for events with declared arguments, the webview sends all declared arg keys on `inspect`/`fire` (blank when the user has not typed a value yet), and after each snapshot it performs an immediate per-event inspect refresh so initial status colors match immediate click outcomes.
- Events whose current live outcome is `NotApplicable` (i.e., the `when` guard is false) are hidden from the event dock entirely. They are still inspected on every refresh cycle so they reappear automatically when the `when` condition becomes true again.
- Argument construction in the inspector webview is DSL-contract strict: user-entered values are normalized to declared scalar types, nullable unset args are sent as `null`, declared defaults are sent when untouched, and required-without-default args are sent as blank placeholders. The preview handler no longer converts blank strings to `null`; it only performs JSON-shape/type coercion.
- Inspector webview exposes a single explicit lifecycle control: `Reset` issues `reset` to create a fresh instance at the machine's initial state with DSL defaults.
- The preview endpoint is bound through a typed JSON-RPC request handler (`IJsonRpcRequestHandler<SmPreviewRequest, SmPreviewResponse>`) with the method contract declared on `SmPreviewRequest` via `[Method("stateMachine/preview/request")]` so registration is discoverable at runtime.
- Language server preview sessions are in-memory and keyed by document URI; each session keeps parsed/compiled definition and current instance state.
- The extension pushes updated snapshots to an open file panel on document change, keeping preview content aligned with current editor text.
- Extension refreshes preview snapshots on document change, save, and panel re-focus/reveal for recovery from stale panel state.
- Document-change refresh uses unsaved in-memory text from the open editor buffer; save is not required for preview updates.
- Snapshot messages are sequence-ordered in the webview so stale async responses cannot overwrite newer editor changes.
- Webview snapshot parsing accepts both camelCase and PascalCase payload keys for state/event/transition/data fields.
- In preview mode, startup recovery performs a fresh `snapshot` request (with a short retry) from the current in-memory editor text instead of requiring persisted file state.
- On webview startup, preview triggers that same snapshot-recovery path after `ready` to recover if the first host-pushed snapshot is missed.
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

### Guards vs. Rules: Routing Logic vs. Data Integrity

The DSL's guard system is per-transition: each `if`/`else if` branch evaluates a condition to decide which path fires. This is the right tool for routing logic — "if the queue is empty, reject; otherwise, dequeue and transition." But data invariants are not per-transition concerns. "Balance must not go negative" is a fact about the data that must hold after every mutation, regardless of which event or branch caused the change.

The current model forces authors to enforce data invariants as guards on every transition that modifies the field. This scales poorly. Add a new event that debits `Balance`, forget to add the guard, and the invariant is silently violated. The author's intent ("Balance must never be negative") is scattered across multiple guards rather than declared once.

Rules separate these two concerns:

- **Guards** answer "which path?" — they are routing logic, evaluated during branch selection, scoped to a single `from ... on ...` block.
- **Rules** answer "is the result valid?" — they are data integrity constraints, evaluated after all mutations commit, enforced regardless of which event or mutation path changed the data.

This separation is not merely organizational. It changes the failure model: a guard that doesn't match simply means a different branch fires (or the event is blocked). A rule that fails means the committed data would violate an invariant, so all mutations are atomically rolled back. Guards are routing; rules are protection.

Rules use the same expression grammar as guards and `set` expressions — one grammar, one evaluator, four scoped positions (field, top-level, state, event). No new operators, no new syntax. The scope restrictions are intentional: field rules can only reference their own field (cross-field constraints belong in top-level rules where the multi-field nature is visible), event rules can only reference event arguments (so they validate inputs, not state). This scope restriction is enforced at runtime by evaluating event rules against an args-only context — machine field values are never injected, so a machine field that shares a bare name with an event argument cannot shadow the argument value during event rule evaluation. See docs/RulesDesign.md for the full design.

#### Rules as prerequisite for the broader design

Rules are not an isolated feature — they are infrastructure. The editable fields feature (docs/EditableFieldsDesign.md) depends on rules existing. Without rules, direct field editing would bypass all data integrity constraints. With rules, the safety net is uniform across both mutation paths (events and editable fields). This dependency is architectural: rules must be implemented before editable fields.

### Data Editing vs. Lifecycle Events

The event pipeline (`from State on Event` → guards → `set` assignments → `transition`) handles lifecycle actions: state changes routed by business logic, with audit-relevant semantics. Every field mutation flows through an event declaration, argument definitions, and explicit `set` assignments. This three-layer structure earns its cost for lifecycle actions — it provides routing, scoping, atomicity, and reviewability.

But not every data change is a lifecycle event. Real-world entities carry data-heavy fields (notes, descriptions, contact information, tags) that need to be modified in-place without changing state. Forcing these through the event pipeline creates three layers of ceremony for a mechanical pass-through: an event that carries no routing logic, arguments that mirror the field, and `set` assignments that copy the argument to the field.

The design choice is to recognize two genuinely different mutation semantics:

- **Lifecycle actions** (events): routed by guards, may transition state, carry audit semantics
- **Data editing** (editable fields): state-scoped field modification, no routing or transitions involved

See docs/EditableFieldsDesign.md for the full design.

#### Why this is not a shortcut

The actor model maps cleanly to the DSL: private state ↔ instance data, messages ↔ events, behavior switch ↔ state transitions. In a distributed actor system, mandatory message passing is justified by concurrency protection (the "mailbox"). The DSL operates in a single-instance, single-threaded domain — the structural protection argument is weaker, and the cost of mandatory message passing for data edits is higher relative to the benefit.

Critically, editable fields are viable *because* rules exist. Rules are declarative invariants (`rule Balance >= 0 "..."`) that the runtime enforces on every mutation regardless of path. Without rules, direct editing would bypass all constraints. With rules, the safety net is uniform: the same invariants protect data whether it arrives through `Fire` or `Update`. This dependency is explicit — editable fields cannot be implemented before rules.

#### "State and data live together"

A core design principle is that the machine instance is the single source of truth for both state and associated data. If data editing requires the same ceremony as lifecycle transitions, authors are incentivized to move data-heavy fields (notes, descriptions, free-text comments) out of the machine into host-managed storage. That splits the truth — the machine knows the state, the host knows the data, and they must be kept in sync externally. Editable fields keep data in the machine while acknowledging that modifying a notes field is categorically different from approving a work order.

### Common Set Before Guards (Rejected — Future Consideration)

**Status: Rejected for now. May be revisited in the future.**

#### Motivation

When multiple branches of a guarded `from` block perform the same `set` assignment, the expression is duplicated across every branch:

```
from GoodStanding on Withdraw
    if balance - Withdraw.Amount >= 0
        set balance = balance - Withdraw.Amount   ← duplicated
        no transition
    else
        set balance = balance - Withdraw.Amount   ← duplicated
        transition Overdrawn
```

The idea is to allow `set` statements between the `from ... on` header and the first `if`, so they apply to all branches without repetition:

```
from GoodStanding on Withdraw
    set balance = balance - Withdraw.Amount        ← common set
    if balance >= 0
        no transition
    else
        transition Overdrawn
```

#### Execution-order question

Two possible semantics:

- **Set before guard evaluation:** The common set executes first, then guards see the modified data. This simplifies guards (e.g. `if balance >= 0` instead of `if balance - Withdraw.Amount >= 0`) but commits a mutation before the routing decision is made.
- **Set after guard evaluation, before the terminal:** Guards still see the original data (no semantic change to existing guards); the common set just eliminates duplication. More conservative but less intuitive — the `set` appears before the `if` textually but executes after it.

#### The `reject` problem

This is the core reason this approach is rejected for now. Consider:

```
from Overdrawn on Withdraw
    set balance = balance - Withdraw.Amount
    reject "Cannot withdraw from an overdrawn account"
```

Current semantics: `reject` means the event is not allowed and no mutation occurs. If a common set executes unconditionally, it would mutate state and then reject, contradicting the meaning of rejection. The possible resolutions are all unsatisfying:

1. **Common sets are skipped when the resolved terminal is `reject`** — intuitive (reject = nothing happened), but the "common" set is not truly common. It silently skips `reject` branches. This is a subtle gotcha: an author adding a `reject` branch to an existing block may not realize the common set no longer applies there.

2. **Common sets always execute, even before reject** — consistent, but semantically wrong. A rejected event should have no side effects.

3. **Parser error: common sets are illegal when any branch is `reject`** — honest, but limits the feature's usefulness to a narrow subset of blocks.

A mixed block with both mutation and reject branches is common in practice (e.g. the Withdraw example above has `reject` in some states), so options 1 and 3 frequently collide with real usage.

#### Alternative considered: `let` bindings

A lighter-weight DRY mechanism would be a local `let` binding that is purely computational (no state mutation):

```
from GoodStanding on Withdraw
    let newBalance = balance - Withdraw.Amount
    if newBalance >= 0
        set balance = newBalance
        no transition
    else
        set balance = newBalance
        transition Overdrawn
```

This avoids the reject problem entirely — `let` is just a named expression, not a state mutation — and eliminates the duplicated arithmetic. The `set` remains explicit in each branch, but the expression is written once. This alternative may be worth revisiting if duplication becomes a frequent authoring pain point.

### DSL Syntax Contract (Current)

Canonical linear form:

```text
# Full-line comment
state Idle initial  # Inline comment — # outside a double-quoted string literal
                    # starts a comment; everything from that # to end of line is ignored

machine <Name>
state <StateName> [initial]
[ rule <BooleanExpr> "<Reason>" ]         # state rules — indented under a state; all data fields in scope
<StateDecl> := initial

event <EventName>
[ <string|number|boolean|null>[?] <ArgName> [= <Literal>] { <ArgDecl> } ]
[ rule <BooleanExpr> "<Reason>" ]         # event rules — may only reference event args for this event

<string|number|boolean|null>[?] <FieldName> { <FieldDecl> }
<string|number|boolean|null>[?] <FieldName> [= <Literal>]
[ rule <BooleanExpr> "<Reason>" ]         # field rules — may only reference the declaring field

rule <BooleanExpr> "<Reason>"             # top-level rules — reference any data fields declared above

# Collection field declarations
set<T> <FieldName>                  # sorted unique set, always starts empty
queue<T> <FieldName>                # FIFO ordered, allows duplicates, always starts empty
stack<T> <FieldName>                # LIFO ordered, allows duplicates, always starts empty
<T> := number | string | boolean    # no nullable inner types, no nesting

from <any|StateA[,StateB...]> on <EventName> [when <BooleanExpr>]
(
    if <Guard> [ set <Field> = <Expr> ] [ <CollectionMutation> ] ( transition <ToState> | no transition )
  | else if <Guard> [ set <Field> = <Expr> ] [ <CollectionMutation> ] ( transition <ToState> | no transition )
  | else [ set <Field> = <Expr> ] [ <CollectionMutation> ] ( transition <ToState> | reject <Reason> | no transition )
  | [ set <Field> = <Expr> ] [ <CollectionMutation> ] ( transition <ToState> | reject <Reason> | no transition )
)+

<Expr> := <Literal|<FieldName>|<EventName>.<ArgName>>
        | <Collection>.count | <Collection>.min | <Collection>.max | <Collection>.peek
        | <Collection> contains <Expr>
<Literal> := <null|true|false|number|string>
<CollectionMutation> :=
    add <SetField> <Expr>
  | remove <SetField> <Expr>
  | enqueue <QueueField> <Expr>
  | dequeue <QueueField> [into <ScalarField>]
  | push <StackField> <Expr>
  | pop <StackField> [into <ScalarField>]
  | clear <CollectionField>
```

Canonical constraints:

- `()+` means one-or-more lines in a `from ... on ...` body.
- `when <BooleanExpr>` is an optional transition-level precondition on the `from ... on ...` header. If it evaluates to `false` at runtime, the entire block is skipped with outcome `NotApplicable`. The `when` guard is evaluated before any branch predicates. The language server narrows null-uncertainty symbols within the block scope when `when` is present (e.g. `when X != null` makes `X` non-nullable for all branches).
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
- Event arguments may declare literal defaults using `<ArgName> = <Literal>`.
- Non-nullable event args without a default are required — the caller must supply them when firing.
- Non-nullable event args with a default use the default when the caller omits them.
- Nullable event args are always optional; if omitted, they default to `null` or the declared default.
- Unsupported syntax: `states ...`, `events ...`, and legacy inline form `transition A -> B on E ...`.

**Rule constraints** (Status: *Implemented*):

- `rule <BooleanExpr> "<Reason>"` is the same syntax in all four positions.
- **Field rules** (indented under a scalar field declaration) scope to the declaring field only. Referencing any other identifier is a parse error.
- **Top-level rules** (unindented, between field declarations and `state` declarations) reference any data field declared above them.
- **State rules** (indented under a `state` declaration) reference any data field. Evaluated on every entry to that state, including self-transitions.
- **Event rules** (indented under an `event` declaration, after arg declarations) reference only event arguments for that event. Instance data refs are a parse error.
- Compile-time checks: literal default that violates its own field rule → error; initial state data that violates a state rule → error; literal `set` RHS that provably violates a field rule → error.
- Runtime: field and top-level rules checked after all `set`/mutation commits; state rules checked against target state. Any violation → full rollback + `Rejected` result.
- `Inspect` (discovery mode, no event args): event rules are skipped; `RequiredEventArgumentKeys` reports which args are needed.

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

### Collection Types Design Decisions

Status: **Implemented.** All collection types, mutations, properties, and the `contains` operator are functional across parser, runtime, expression evaluator, and language server.

#### Locked decisions

- Three collection types: `set<T>`, `queue<T>`, `stack<T>`.
- `map<K,V>` is out — too much language surface, breaks the declarative boundary.
- Valid inner types: `number`, `string`, `boolean`. No nullable inner types (`set<number?>` is invalid). No `set<null>`. No nesting (`set<set<number>>` is invalid).
- Generic `<T>` syntax for declarations; parser validates inner type at declaration time.
- Collections always start empty — no array literal syntax. Pre-population is done through the runtime API (`CreateInstance`), not DSL declarations.
- `set<T>` is backed by a sorted structure (`SortedSet<T>`) — this is a semantic guarantee for determinism, not an implementation detail.
- `queue<T>` is FIFO-ordered. `stack<T>` is LIFO-ordered. Both allow duplicate elements.

#### Determinism contract

For any state machine instance with state S, data D (including all scalar and collection fields), receiving event E with arguments A: f(S, D, E, A) = (S', D', O). Same inputs always produce the same outputs, regardless of execution platform, prior mutation history, or serialization round-trips.

#### Operations surface

Mutation statements (same level as `set`, `transition`, etc.):

| Statement | `set<T>` | `queue<T>` | `stack<T>` | Behavior |
|---|---|---|---|---|
| `add <Collection> <Expr>` | Yes | — | — | Insert; no-op if duplicate (idempotent) |
| `remove <Collection> <Expr>` | Yes | — | — | Remove by value; no-op if absent (idempotent) |
| `enqueue <Collection> <Expr>` | — | Yes | — | Append to back; always succeeds |
| `dequeue <Collection>` | — | Yes | — | Remove from front; fails if empty |
| `dequeue <Collection> into <Field>` | — | Yes | — | Copy front element to scalar field, then remove; fails if empty |
| `push <Collection> <Expr>` | — | — | Yes | Add to top; always succeeds |
| `pop <Collection>` | — | — | Yes | Remove from top; fails if empty |
| `pop <Collection> into <Field>` | — | — | Yes | Copy top element to scalar field, then remove; fails if empty |
| `clear <Collection>` | Yes | Yes | Yes | Remove all elements (idempotent) |

Properties (dot-access, no parentheses):

| Property | Types | Returns | Valid in guards? | Valid in `set` RHS? |
|---|---|---|---|---|
| `count` | All | number | Yes | Yes |
| `min` | `set<T>` | T | No (fails if empty) | Yes |
| `max` | `set<T>` | T | No (fails if empty) | Yes |
| `peek` | `queue<T>`, `stack<T>` | T | No (fails if empty) | Yes |

Operators:

| Expression | Types | Returns | Valid in guards? | Valid in `set` RHS? |
|---|---|---|---|---|
| `<Collection> contains <Expr>` | All | boolean | Yes | Yes |

**No function-call syntax.** The DSL surface uses properties (dot-access) and operators (infix keywords) only.

#### Element-returning expressions restricted to `set` RHS

Properties that return an element (`min`, `max`, `peek`) are valid only on the right side of `set` assignments, not in guard expressions. Reasoning: element-returning operations can fail (empty collection), and a failing guard is semantically ambiguous (does the branch not match, or is it an error?). In `set` RHS, failure is unambiguous: the branch body fails, atomic rollback occurs.

Correct pattern:
```text
if PendingFloors.count > 0
    set TargetFloor = PendingFloors.min
    transition Moving
```

#### Failure semantics

- Writes are lenient: `add` of a duplicate, `remove` of a missing value, and `clear` on an empty collection are no-ops.
- Reads are strict: `dequeue`/`pop` on an empty collection and `min`/`max`/`peek` on an empty collection fail the branch. Due to atomic batch semantics, all mutations in that branch are rolled back.

#### Read-your-writes

Collection mutations within a branch body participate in the working copy, consistent with scalar `set` semantics. After `add PendingFloors 5`, subsequent references to `PendingFloors.count` or `PendingFloors contains 5` in the same branch body reflect the addition.

#### Serialization

- `set<T>`: serializes as a sorted JSON array; deserialization reconstructs sorted order, duplicates silently dropped.
- `queue<T>`: serializes as an ordered JSON array (front = index 0).
- `stack<T>`: serializes as an ordered JSON array (top = last index).
- Round-trip fidelity guaranteed: serialize → deserialize → serialize produces identical output.

#### Cross-collection operations

No direct collection-to-collection operations. Use a scalar intermediary with `into`:
```text
dequeue ApprovalChain into NextApprover
push CompletedApprovers NextApprover
```

#### `dequeue`/`pop` statements with optional `into`

`dequeue` and `pop` are mutation statements, not expressions. The optional `into <ScalarField>` clause copies the front/top element into a declared scalar data field before removing it. Without `into`, the element is simply discarded. The `into` target must be a declared scalar (non-collection) data field whose type matches the collection's element type. This keeps mutation at statement level while providing a concise way to capture the removed element without a separate `set` + `peek` step.

#### Decision pending: directional set queries (`above`/`below`)

Sorted sets naturally support directional nearest-neighbor queries (e.g., "smallest element greater than X"). This enables patterns like elevator SCAN algorithms. Multiple syntax options were explored:

1. `above`/`below` as infix filter operators with `.min`/`.max` on the result: `(PendingFloors above CurrentFloor).min`
2. Comparison operators as set filters: `(PendingFloors > CurrentFloor).min`
3. Trailing modifier on property: `PendingFloors.min above CurrentFloor`
4. Slice syntax: `PendingFloors[> CurrentFloor].min`
5. Named views: `view FloorsAbove = PendingFloors > CurrentFloor`
6. Several other options considered.

All options introduce syntax cost (new keywords, filter expressions, or parameterized properties). The core concern is that element-returning filtered queries either need function-call syntax (which the DSL avoids) or introduce new expression composition patterns.

**Current decision**: deferred. `min`/`max` cover the majority of real use cases. The directional query pattern can be revisited once real usage patterns emerge. For now, authors can approximate directional behavior using `min`/`max` with a `MovingUp` boolean to select the appropriate branch.

### Set/Expression Design Decisions (Locked — Scalars)

Status:

- The following choices are design-locked for the scalar set/expression system.
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
  <ScalarType>[?] <ArgName> [= <Literal>]

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

- `StateMachine.Dsl.DslWorkflowModel` parse tree (was `DslMachine`)
- `StateMachine.Dsl.DslWorkflowParser` — `DslWorkflowParser.Parse(text)` returns `DslWorkflowModel` (was `StateMachineDslParser`)
- `StateMachine.Dsl.DslWorkflowCompiler` — `Compile(DslWorkflowModel)` returns `DslWorkflowEngine`
- `StateMachine.Dsl.DslWorkflowEngine` — immutable compiled engine (was `DslWorkflowDefinition`)
- `StateMachine.Dsl.DslWorkflowInstance` persisted instance model
- Result types: `DslEventInspectionResult`, `DslInspectionResult` (aggregate), `DslFireResult`, `DslCompatibilityResult`
- `tools/StateMachine.Dsl.LanguageServer` (LSP diagnostics/completion/semantic tokens + preview request handler)
- `tools/StateMachine.Dsl.VsCode` (language client + inspector preview webview)

## Current Runtime Semantics

- Undefined state/event/transition resolves to outcome `NotDefined`
- Unguarded transitions resolve to outcome `Accepted` and return a target/new state
- Guarded transitions are evaluated at runtime against optional event arguments; if provided, they are used for that call without mutating persisted instance data
- `from ... on ...` blocks support ordered `if`/`else if`/`else` branches and end with an outcome statement: `transition <State>`, `reject "<message>"`, or `no transition`.
- Statements are not allowed after an outcome statement in a block.
- Transition data assignments are evaluated/applied only during `Accepted` or `AcceptedInPlace` `Fire(...)` calls
- If a `when` predicate evaluates to `false`, the entire block is skipped with outcome `NotApplicable`
- If one guarded transition evaluates `true`, inspection/fire is `Accepted` and returns target/new state
- If all guarded transitions evaluate `false`, terminal `reject` returns `Rejected` with the configured reason; terminal `no transition` returns `AcceptedInPlace` (state unchanged, `set` assignments execute on fire)
- Instance-based inspect/fire validates workflow name compatibility before evaluating transitions
- `Inspect(instance)` returns a `DslInspectionResult` aggregate with `CurrentState`, clean `InstanceData`, and `Events` — all events evaluated in one call
- `Fire(instance, eventName)` returns `DslFireResult` with `UpdatedInstance : DslWorkflowInstance?`; on failure the instance is unchanged (full rollback)
- `CoerceEventArguments(eventName, args?)` coerces raw JSON/untyped values to the correct scalar types
- `CheckCompatibility(instance)` validates schema compatibility and runs all current rules

## Concurrency Model (Current)

- `DslWorkflowEngine` is immutable after compile.
- Runtime does not maintain hidden mutable process state; state progresses through returned `DslWorkflowInstance` values.
- Any coordination for concurrently reading/writing persisted instance files is outside runtime scope and must be handled by the caller.

## Known Gaps

- Extension packaging/publishing workflow

## Test Status

- Active tests include `test/StateMachine.Tests/DslWorkflowTests.cs`, `test/StateMachine.Tests/DslExpressionParserTests.cs`, `test/StateMachine.Tests/DslExpressionParserEdgeCaseTests.cs`, `test/StateMachine.Tests/DslSetParsingTests.cs`, `test/StateMachine.Tests/DslExpressionRuntimeEvaluatorBehaviorTests.cs`, `test/StateMachine.Tests/DslCollectionTests.cs`, `test/StateMachine.Tests/DslRulesTests.cs`, `test/StateMachine.Dsl.LanguageServer.Tests/SmDslAnalyzerNullNarrowingTests.cs`, and `test/StateMachine.Dsl.LanguageServer.Tests/SmDslAnalyzerCollectionMutationTests.cs`.
- Guard/expression test coverage includes: boolean guards, comparisons, string/null equality, numeric runtime type coercion, unsupported-expression rejection, reason aggregation, expression AST parsing precedence/invalid syntax diagnostics, lexer edge cases, set-branch parsing constraints, and runtime evaluator operator/short-circuit behavior.
- Language-server null-narrowing coverage includes: single-expression `&&`/`||` narrowing, cross-branch narrowing across ordered if/else-if/else chains, set-assignment validation under narrowed types, and collection mutation value type checking.

## Next Steps

1. Add packaging/publishing automation for the VS Code extension client.
2. Editable fields (docs/EditableFieldsDesign.md) — prerequisite rules feature is now implemented; editable fields are the logical next feature.

## Guard Evaluation + Event-Argument Model (Current)

- `IGuardEvaluator`, `DefaultGuardEvaluator`, and `GuardEvaluationResult` are removed — guard ASTs are evaluated directly by `DslExpressionRuntimeEvaluator`.
- `DslWorkflowCompiler.Compile(DslWorkflowModel)` takes no optional evaluator parameter.
- `Inspect(instance, eventName, args?)` and `Fire(instance, eventName, args?)` accept optional event arguments (`IReadOnlyDictionary<string, object?>`).
- `Inspect(instance)` aggregate evaluates all events against the current instance in one call.
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
- `DslTransition` and `DslTerminalRule` model records carry a `SourceLine` property (0 = unknown) recorded by the parser at the `from … on …` header line. `ValidateReferences` uses these to emit `Line N:` prefixes on all reference errors (unknown state/event/field), enabling the language server to squiggle the exact offending `from … on …` line rather than always falling back to line 0.
- `DslTransition` also carries `TargetLine` (the `transition <State>` inner line), used by `ValidateReferences` to point unknown-target-state errors at the correct line inside the block rather than the header.
- `DslSetAssignment` carries `SourceLine` (the `set <Key> = <Expr>` line), used by both `ValidateReferences` (unknown field) and the analyzer's `FindSetLine` fallback so set-expression squiggles land on the correct line.
- The parser tracks `firstContentLineNumber` and `lastStateLineNumber` during the main parse loop; post-loop errors (missing `machine`, no states, no initial marker) now include a `Line N:` prefix so the language server places the diagnostic on the first relevant line instead of 0:0.
- Duplicate unguarded outcome validation now points to the second (duplicate) rule's `SourceLine` rather than having no line context.
- `SmDslAnalyzer.GetSemanticDiagnostics` now resets the guard/set text-search cursor per-rule to `rule.SourceLine - 1` instead of advancing monotonically from 0, fixing cases where interleaved or reversed rules caused the search to overshoot the actual guard/set line.
- `FindGuardLine` and `FindSetLine` now accept a `fallbackLine` parameter; on a text-match miss they return the fallback (the `from … on …` header line) instead of line 0.
- `SmDslAnalyzer.GetSemanticDiagnostics` groups all transitions and terminal rules by `(FromState, EventName)` and processes each group in `Order`-sorted sequence, accumulating negations of prior guards into a running `branchSymbols` dictionary. This provides cross-branch null narrowing: after `if X == null → no transition`, the `else if` and `else` branches see `X` as non-nullable, eliminating false-positive diagnostics on numeric comparisons or `set` assignments that follow a null-guard branch.
- Collection mutation value expressions (`add`, `remove`, `enqueue`, `push`) are type-checked in the same `GetSemanticDiagnostics` pass as `set` assignments, using the branch's narrowed symbol table. `dequeue`/`pop into` target field types are validated against the collection's inner type. Each mutation error is squiggled at the actual mutation line using `FindMutationLine` (modelled after `FindGuardLine`/`FindSetLine`), not at the `from … on …` header.
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
- The TextMate grammar is at `tools/StateMachine.Dsl.VsCode/syntaxes/state-machine-dsl.tmLanguage.json`. It must be kept in sync with the DSL parser whenever keywords, statement forms, operators, or type names change. Patterns are ordered: declarations → dotted event-arg refs → field declarations → control keywords → type keywords → action keywords → operators → boolean/null constants → numbers → identifier catch-all → strings. Multi-character operators must appear before single-character operators in the operators block.
- Completions are implemented in `tools/StateMachine.Dsl.LanguageServer/SmDslAnalyzer.cs` (`GetCompletions`, `KeywordItems`, context-specific branches, snippet lists). It must be kept in sync with the DSL parser: every new keyword goes into `KeywordItems`; every new statement position that has a distinct valid completion set needs a regex branch in `GetCompletions`.
- Semantic tokens are implemented in `tools/StateMachine.Dsl.LanguageServer/SmSemanticTokensHandler.cs` (`KeywordTokens`, `HighlightNamedSymbols`, `OperatorRegex`, `ExpressionLineRegex`). It must be kept in sync with the DSL parser: every new keyword goes into `KeywordTokens`; new declaration header forms go into `HighlightNamedSymbols`; new operators go into `OperatorRegex` (multi-char first); new expression-containing line prefixes go into `ExpressionLineRegex`.
- Apply the Grammar Sync Checklist and Intellisense Sync Checklist from `.github/copilot-instructions.md` for every DSL change.
- VS Code client supports local-only VSIX packaging via `npm run package:local` in `tools/StateMachine.Dsl.VsCode`.
- Local VSIX packaging includes language-client runtime dependencies so activation works after install.
- VS Code client supports a local package+install loop via `npm run loop:local` in `tools/StateMachine.Dsl.VsCode`.
- VS Code client activates from `workspaceContains:**/*.sm` plus language contribution activation and writes startup diagnostics to the `StateMachine DSL` output channel.
- Repository includes runnable DSL examples such as `samples/trafficlight.sm`.

Supported default guard forms:

- `IsEnabled`
- `!IsEnabled`
- `CarsWaiting > 0`
- `CarsWaiting >= 3`
- `Mode == "Manual"`

Unsupported/invalid guards are treated as failed with descriptive reasons.

