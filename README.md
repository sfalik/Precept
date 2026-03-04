# StateMachine 🚦

StateMachine is a .NET DSL-driven state/workflow engine focused on deterministic transition evaluation and persisted instance state.

## Why this project

StateMachine is for applications where entities move through explicit, long-lived lifecycles: support tickets, orders, approvals, onboarding flows, loan pipelines, or any process where "what state are we in and what can happen next" must be clear and consistent.

Most teams start these workflows as scattered `if`/`switch` logic across handlers and services. That works at first, then drifts: states become implicit, transition rules are duplicated, and nobody can easily answer why an action is enabled in one case and blocked in another. StateMachine exists to make that lifecycle explicit, executable, and reviewable in one place.

The design philosophy is driven by that goal, not by language novelty:

- **Predictability over cleverness**: deterministic evaluation and declaration-ordered rules mean the same input produces the same outcome every time.
- **Safe introspection over hidden side effects**: `Inspect` works because expressions are pure; you can ask "what would happen" without mutating persisted state.
- **Consistency over syntax shortcuts**: statements mutate and expressions read. This keeps the mental model small and prevents context-specific exceptions.
- **Integrity over partial success**: transitions are atomic, so branch mutations either all commit or all roll back.
- **Declarative workflow over embedded programming language**: the DSL intentionally models lifecycle rules, while complex computation stays in host code.

In practice, this library is a good fit when you need auditable workflow behavior that both developers and domain stakeholders can read. The `.sm` file becomes both runtime contract and living documentation, while persisted instances and explicit event arguments make integration straightforward for APIs, UIs, and background processing.

## Design Philosophy

StateMachine's DSL is shaped by a small number of principles that come up repeatedly when deciding what to add, what to reject, and how new features should behave.

### Expressions are pure; statements mutate

Evaluating a guard condition or a `set` right-hand side never changes state. You can evaluate `Floors.count > 0` a hundred times and the collection is untouched every time. Mutations — `add`, `remove`, `dequeue`, `pop`, `clear` — are explicit statements that only execute during an accepted `Fire`. This separation is not incidental; it is the reason `Inspect` can safely preview any transition without side effects, and it is why we chose `dequeue Q into X` (a statement) over `set X = dequeue Q` (which would have made `dequeue` an expression in one context and a statement in another).

### One rule, no exceptions

When a feature introduces a context-sensitive rule — "this keyword is an expression here but a statement there" — we look for an alternative that avoids the exception. The `into` syntax exists because we found a way to make `dequeue` one-line without bending the expression model. A shorter syntax with a caveat is worse than a slightly longer syntax with no caveats.

### Deterministic by construction

`set<T>` is backed by a sorted structure as a semantic guarantee, not an implementation detail. Iteration order, `.min`, `.max` are all deterministic regardless of insertion order. Guard evaluation follows declaration order. There are no race conditions, no nondeterministic outcomes, and no cases where running the same event with the same data could produce different results.

Null safety is part of this guarantee. A field declared as `number?` can be `null`, and using it in a comparison or arithmetic without first establishing that it is non-null is a compile-time error — the editor squiggles the line and refuses to let the ambiguity propagate to runtime. When a branch tests `if X == null → reject`, every subsequent branch in the chain knows statically that `X` is non-null: the null check is not just documentation, it is enforced by the analyzer and narrows the type for all following branches. Null uncertainty cannot silently reach a guard comparison or a data assignment.

### Atomic transitions

When a branch fires, all scalar assignments and collection mutations either commit together or roll back together. If a `dequeue` fails because the queue is empty, the entire branch is rejected — no partial mutations leak into the persisted instance. This is true even when a branch contains multiple `set` assignments and multiple collection mutations interleaved.

### Read-your-writes within a branch

Within a single branch body, mutations are immediately visible to subsequent expressions. After `add PendingFloors 5`, a later guard or `set` expression can reference `PendingFloors.count` and see the updated count, or test `PendingFloors contains 5` and get `true`. Scalar `set` assignments work the same way — `set Count = Count + 1` followed by `set DoubleCount = Count * 2` sees the incremented value. The working copy that enables atomic rollback is the same mechanism that enables read-your-writes: all mutations happen against a branch-local copy, and expressions read from that copy.

### Lenient writes, strict reads

`add` of a duplicate value is a no-op. `remove` of a missing value is a no-op. `clear` on an empty collection is a no-op. But `dequeue` on an empty queue, `pop` on an empty stack, and `.min`/`.max`/`.peek` on an empty collection all fail the branch. The asymmetry is intentional: writes should be safe to issue without precondition checks, but reads that assume data exists should fail loudly when that assumption is wrong.

### Inspect before fire

Every transition can be evaluated read-only (`Inspect`) before committing (`Fire`). This enables UIs to show which events are defined, which are blocked by guards, and which are enabled — all without touching persisted state. The inspect/fire split is the reason expressions must stay pure: if evaluating a guard could mutate a collection, inspection would no longer be safe.

### The declarative boundary

The DSL is not a general-purpose language, and that is a feature. `map<K,V>`, function-call syntax, loops, collection nesting, and user-defined functions are deliberately excluded. When a workflow needs logic beyond what the DSL expresses, that logic belongs in the host application — not in a more complex DSL. Every feature proposal is weighed against the cost of moving the DSL closer to a programming language, and the answer is usually "no."

## Current Status

- DSL parser/compiler/runtime is implemented and used by the language server for editor diagnostics and preview execution.
- VS Code extension is implemented with automatic `.sm` language-client activation plus inspector preview panels per file.
- Inspector preview now exchanges live `snapshot`/`fire`/`reset`/`inspect` requests through the language server (`stateMachine/preview/request`) instead of only local mock data. The `inspect` action re-evaluates a single event with user-supplied arguments, enabling real-time guard feedback as the user types.
- Inspector preview layout uses a single unified ELK layered layout with state-machine-tuned options (top-down direction, spline edge routing, model-order cycle breaking, feedback edges for cycles, inside self-loops, inline edge labels, DSL declaration-order node ordering), dynamic per-state node sizing, and responsive viewBox. Reject and no-transition terminal rules are excluded from the diagram graph.
- CLI host has been removed in this branch (hard cut); editor + language server are the active runtime surfaces.
- **Collection types** (`set<T>`, `queue<T>`, `stack<T>`) are implemented with full parser, runtime, and language-server support. Declarations, mutations (`add`/`remove`/`enqueue`/`dequeue`/`push`/`pop`/`clear`), guard properties (`.count`/`.min`/`.max`/`.peek`), and the `contains` operator are all functional. Directional set queries (`above`/`below`) remain deferred pending real usage data.
- **Language-server null-flow diagnostics** perform both intra-expression narrowing (`&&`/`||`/`!` within a single guard) and cross-branch narrowing: when guarded branches form an `if`/`else if`/`else` chain, prior guard negations are accumulated so later branches see a progressively narrowed type environment (e.g. after `if X == null → no transition`, the `else if` and `else` branches see `X` as non-nullable). Collection mutation value expressions (`add`, `enqueue`, `push`) are type-checked against the collection's inner type with the same narrowed symbols as `set` assignments; `dequeue`/`pop into` target types are validated against the collection's inner type.

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

# Collection field declarations
set<T> <FieldName>                  # sorted unique set, always starts empty
queue<T> <FieldName>                # FIFO ordered, allows duplicates, always starts empty
stack<T> <FieldName>                # LIFO ordered, allows duplicates, always starts empty
<T> := number | string | boolean    # no nullable inner types, no nesting

from <any|StateA[,StateB...]> on <EventName>
(
    if <GuardExpr>
      { set <Field> = <Expr> }
      { <CollectionMutation> }
      ( transition <ToState> | no transition )

  | else if <GuardExpr>
      { set <Field> = <Expr> }
      { <CollectionMutation> }
      ( transition <ToState> | no transition )

  | else
      { set <Field> = <Expr> }
      { <CollectionMutation> }
      ( transition <ToState> | reject "<Reason>" | no transition )

  | { set <Field> = <Expr> }
    { <CollectionMutation> }
    ( transition <ToState> | reject "<Reason>" | no transition )
)+

<Expr> := <Literal> | <FieldName> | <EventName>.<ArgName> | ( <Expr> ) | !<Expr> | -<Expr> | <Expr> <BinaryOp> <Expr>
        | <Collection>.count | <Collection>.min | <Collection>.max | <Collection>.peek
        | <Collection> contains <Expr>

<BinaryOp> := + | - | * | / | % | == | != | < | <= | > | >= | && | ||

<CollectionMutation> :=
    add <SetField> <Expr>
  | remove <SetField> <Expr>
  | enqueue <QueueField> <Expr>
  | dequeue <QueueField> [into <ScalarField>]
  | push <StackField> <Expr>
  | pop <StackField> [into <ScalarField>]
  | clear <CollectionField>

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
- Collection mutation statements (`add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`) are valid only inside `from ... on ...` branch bodies, at the same level as `set`. Each mutation verb is valid only on its matching collection kind (parser rejects mismatches).
- `dequeue <QueueField> into <ScalarField>` and `pop <StackField> into <ScalarField>` atomically read the front/top element into a scalar data field and remove it from the collection. The `into` target must be a declared scalar data field whose type matches the collection's inner type. Empty collection → branch failure + atomic rollback.
- Collection properties: `.count` (all types, returns number, valid in guards and `set` RHS), `.min`/`.max` (`set<T>` only, returns element, `set` RHS only), `.peek` (`queue<T>`/`stack<T>` only, returns element, `set` RHS only).
- `contains` is an infix boolean operator: `<Collection> contains <Expr>` (valid in guards and `set` RHS).
- Element-returning properties (`.min`, `.max`, `.peek`) fail on empty collections, triggering atomic branch rollback.
- Collection mutations are lenient for writes (`add` duplicate → no-op, `remove` missing → no-op) but strict for reads (`dequeue`/`pop` on empty → branch failure and rollback).
- No function-call syntax. No nullable inner types. No collection nesting. No array literals. No `map<K,V>`.
- Unsupported syntax: `states ...`, `events ...`, and legacy inline form `transition A -> B on E ...`.

### Null Safety

Appending `?` to a type name declares a nullable field or event argument.

```text
number? RetryCount
string? LastError
```

Using a nullable field in any operator, comparison, or assignment that requires a concrete type is a **compile-time error** — the language server squiggles the exact offending line. This prevents null-related surprises from reaching the runtime.

**Three patterns for handling nullable fields before use:**

1. Inline with `&&` — test and use in the same guard:

```text
if RetryCount != null && RetryCount > 0
  set Attempts = RetryCount
  transition Retry
else
  reject "RetryCount unavailable"
```

The analyzer understands that the right-hand side of `&&` is only reached when the left side is true, so `RetryCount` is treated as non-nullable for the `RetryCount > 0` test and for the `set` assignment.

2. Inline with `||` — short-circuit the null case:

```text
if RetryCount == null || RetryCount > 0
  transition Retry
else
  reject "Retry limit reached"
```

The right-hand side of `||` is only reached when the left side is false (i.e. `RetryCount != null`), so the comparison is valid.

3. Early-exit pattern across branches — reject the null case first, then use freely:

```text
from Active on Retry
  if RetryCount == null
    reject "RetryCount unavailable"
  else if RetryCount > 0
    set Attempts = RetryCount
    transition Active
  else
    reject "No retries remaining"
```

Because the first branch rejects when `RetryCount == null`, the analyzer knows every subsequent `else if` and `else` branch can only be reached when `RetryCount` is non-null. Using `RetryCount` in `RetryCount > 0` or in `set Attempts = RetryCount` requires no additional null check. This cross-branch narrowing is enforced statically — you get the same squiggle you would get in the single-guard case if you mistakenly skip the null check.

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

13) Collection: sorted set with add/remove/min

```text
set<number> PendingFloors

from Idle on RequestFloor
  if RequestFloor.Floor >= 1 && RequestFloor.Floor <= TotalFloors
    add PendingFloors RequestFloor.Floor
    transition Moving
  else
    reject "Invalid floor"

from Moving on FloorReached
  remove PendingFloors CurrentFloor
  set CurrentFloor = TargetFloor
  transition DoorsOpen

from DoorsOpen on CloseDoors
  if !Overloaded && PendingFloors.count > 0
    set TargetFloor = PendingFloors.min
    transition Moving
  else if !Overloaded
    transition Idle
  else
    reject "Cannot close doors while overloaded"
```

14) Collection: queue with enqueue/dequeue/peek

```text
queue<string> ApprovalChain
string LastApprover = ""

from Submitted on AssignApprover
  enqueue ApprovalChain Approver.Name
  no transition

from AwaitingApproval on Approve
  if ApprovalChain.count > 1
    dequeue ApprovalChain into LastApprover
    no transition
  else
    dequeue ApprovalChain into LastApprover
    transition Approved
```

15) Collection: stack with push/pop/peek

```text
stack<string> BreadcrumbTrail
string CurrentRoom = ""

from Exploring on EnterRoom
  push BreadcrumbTrail CurrentRoom
  set CurrentRoom = EnterRoom.RoomName
  transition Exploring

from Exploring on Backtrack
  if BreadcrumbTrail.count > 0
    pop BreadcrumbTrail into CurrentRoom
    no transition
  else
    reject "No rooms to backtrack to"
```

16) Collection: contains operator in guards

```text
from Idle on RequestFloor
  if PendingFloors contains RequestFloor.Floor
    reject "Floor already requested"
  else
    add PendingFloors RequestFloor.Floor
    transition Moving
```

17) Collection: cross-collection transfer via scalar intermediary

```text
queue<string> PendingReviewers
stack<string> CompletedReviewers
string Reviewer = ""

from AwaitingReview on CompleteReview
  if PendingReviewers.count > 1
    dequeue PendingReviewers into Reviewer
    push CompletedReviewers Reviewer
    no transition
  else
    dequeue PendingReviewers into Reviewer
    push CompletedReviewers Reviewer
    transition FullyReviewed
```

18) Nullable field — inline null check with `&&`

```text
number? Score
string RiskTier = "Unknown"

from Pending on Evaluate
  if Score != null && Score >= 80
    set RiskTier = "Low"
    transition Approved
  else
    reject "Score unavailable or below threshold"
```

The editor squiggles `Score >= 80` if you remove the `Score != null &&` prefix, because `Score` is declared nullable and the `>=` operator requires a non-null number. The null check in the same `&&` clause narrows `Score` to non-nullable for the right-hand side.

19) Nullable field — early-exit null rejection, then use freely in else branches

```text
number? RetryCount
number Attempts = 0

from Active on Retry
  if RetryCount == null
    reject "RetryCount unavailable"
  else if RetryCount > 0
    set Attempts = RetryCount
    transition Active
  else
    reject "No retries remaining"
```

After the first branch rejects on `null`, all following `else if` and `else` branches are statically known to execute only when `RetryCount` is non-null. `RetryCount > 0` and `set Attempts = RetryCount` require no additional null guard — the analyzer enforces this across the branch chain. Removing the `if RetryCount == null` branch causes the editor to squiggle `RetryCount > 0` in the next branch.

```text
queue<string> ApprovalChain
string LastApprover = ""

from Submitted on AssignApprover
  enqueue ApprovalChain Approver.Name
  no transition

from AwaitingApproval on Approve
  if ApprovalChain.count > 1
    dequeue ApprovalChain into LastApprover
    no transition
  else
    dequeue ApprovalChain into LastApprover
    transition Approved
```

15) Collection: stack with push/pop/peek

```text
stack<string> BreadcrumbTrail
string CurrentRoom = ""

from Exploring on EnterRoom
  push BreadcrumbTrail CurrentRoom
  set CurrentRoom = EnterRoom.RoomName
  transition Exploring

from Exploring on Backtrack
  if BreadcrumbTrail.count > 0
    pop BreadcrumbTrail into CurrentRoom
    no transition
  else
    reject "No rooms to backtrack to"
```

16) Collection: contains operator in guards

```text
from Idle on RequestFloor
  if PendingFloors contains RequestFloor.Floor
    reject "Floor already requested"
  else
    add PendingFloors RequestFloor.Floor
    transition Moving
```

17) Collection: cross-collection transfer via scalar intermediary

```text
queue<string> PendingReviewers
stack<string> CompletedReviewers
string Reviewer = ""

from AwaitingReview on CompleteReview
  if PendingReviewers.count > 1
    dequeue PendingReviewers into Reviewer
    push CompletedReviewers Reviewer
    no transition
  else
    dequeue PendingReviewers into Reviewer
    push CompletedReviewers Reviewer
    transition FullyReviewed
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
